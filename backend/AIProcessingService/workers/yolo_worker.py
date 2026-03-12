"""
YOLO Worker - Object Detection for License Plates
Uses YOLOv8 for detecting and blurring license plates

Features:
- License plate detection
- Automatic plate blurring
- Multi-plate support
- Bounding box extraction
"""

import asyncio
import io
import json
import logging
import os
from dataclasses import dataclass
from typing import List, Optional

import aio_pika
import boto3
import httpx
import numpy as np
from botocore.client import Config
from PIL import Image, ImageFilter

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# ========== Configuration ==========
RABBITMQ_HOST = os.getenv("RABBITMQ_HOST", "rabbitmq")
RABBITMQ_USER = os.getenv("RABBITMQ_USER", "guest")
RABBITMQ_PASS = os.getenv("RABBITMQ_PASS", "guest")
QUEUE_NAME = "ai-processing-detection"

S3_ENDPOINT = os.getenv("S3_ENDPOINT", "https://s3.amazonaws.com")
S3_BUCKET = os.getenv("S3_BUCKET", "okla-processed-images")
S3_ACCESS_KEY = os.getenv("S3_ACCESS_KEY", "")
S3_SECRET_KEY = os.getenv("S3_SECRET_KEY", "")
S3_REGION = os.getenv("S3_REGION", "us-east-1")

MODEL_PATH = os.getenv("MODEL_PATH", "/models/yolov8x.pt")
PLATE_MODEL_PATH = os.getenv("PLATE_MODEL_PATH", "/models/license_plate_detector.pt")
DEVICE = os.getenv("DEVICE", "0")  # CUDA device ID

API_CALLBACK_URL = os.getenv("API_CALLBACK_URL", "http://aiprocessingservice:8080")


@dataclass
class BoundingBox:
    x1: float
    y1: float
    x2: float
    y2: float
    confidence: float
    class_name: str


@dataclass
class DetectionMessage:
    job_id: str
    vehicle_id: str
    image_url: str
    blur_plates: bool = True


@dataclass
class DetectionResult:
    job_id: str
    success: bool
    plates_detected: int = 0
    plates: List[dict] = None
    blurred_url: Optional[str] = None
    error_message: Optional[str] = None
    processing_time_ms: int = 0


class YOLOProcessor:
    """YOLOv8-based object detection processor"""

    def __init__(self, model_path: str, plate_model_path: str, device: str = "0"):
        self.device = device
        self.model = None
        self.plate_model = None
        self._load_models(model_path, plate_model_path)

    def _load_models(self, model_path: str, plate_model_path: str):
        """Load YOLO models"""
        logger.info(f"Loading YOLO models on device {self.device}")

        try:
            from ultralytics import YOLO

            # General object detection model
            if os.path.exists(model_path):
                self.model = YOLO(model_path)
                logger.info(f"Loaded general YOLO model: {model_path}")
            else:
                self.model = YOLO("yolov8x.pt")
                logger.info("Loaded default YOLOv8x model")

            # License plate specific model (if available)
            if os.path.exists(plate_model_path):
                self.plate_model = YOLO(plate_model_path)
                logger.info(f"Loaded plate detection model: {plate_model_path}")
            else:
                self.plate_model = None
                logger.warning("No plate-specific model, using general detection")

        except ImportError:
            logger.warning("Ultralytics not installed, using mock")
            self.model = None

    def detect_plates(self, image: np.ndarray) -> List[BoundingBox]:
        """
        Detect license plates in image

        Returns list of bounding boxes for detected plates
        """
        if self.model is None:
            # Mock detection for development
            logger.warning("Using mock plate detection")
            return []

        boxes = []

        # Use plate-specific model if available
        model = self.plate_model if self.plate_model else self.model

        # Run detection
        results = model(image, device=self.device, verbose=False)

        for result in results:
            for box in result.boxes:
                class_id = int(box.cls[0])
                class_name = model.names[class_id]

                # Check if it's a license plate (or vehicle for general model)
                if self.plate_model or class_name in ["license_plate", "number_plate", "plate"]:
                    x1, y1, x2, y2 = box.xyxy[0].tolist()
                    conf = float(box.conf[0])

                    if conf > 0.5:  # Confidence threshold
                        boxes.append(BoundingBox(x1=x1, y1=y1, x2=x2, y2=y2, confidence=conf, class_name=class_name))

        # If using general model, detect vehicles and look for plates in typical locations
        if not self.plate_model:
            boxes = self._find_plates_heuristic(image, results)

        return boxes

    def _find_plates_heuristic(self, image: np.ndarray, results) -> List[BoundingBox]:
        """
        Heuristic plate detection based on car detection
        Looks for plate-like regions in typical plate locations
        """
        boxes = []
        h, w = image.shape[:2]

        for result in results:
            for box in result.boxes:
                class_name = self.model.names[int(box.cls[0])]

                if class_name in ["car", "truck", "bus", "motorcycle"]:
                    x1, y1, x2, y2 = box.xyxy[0].tolist()
                    conf = float(box.conf[0])

                    # Estimate plate locations (front and rear)
                    car_width = x2 - x1
                    car_height = y2 - y1

                    # Front plate (lower center of vehicle)
                    plate_w = car_width * 0.25
                    plate_h = car_height * 0.08
                    plate_x = x1 + (car_width - plate_w) / 2
                    plate_y = y2 - plate_h - (car_height * 0.05)

                    boxes.append(
                        BoundingBox(
                            x1=plate_x,
                            y1=plate_y,
                            x2=plate_x + plate_w,
                            y2=plate_y + plate_h,
                            confidence=conf * 0.7,  # Lower confidence for heuristic
                            class_name="estimated_plate",
                        )
                    )

        return boxes

    def blur_plates(self, image: Image.Image, boxes: List[BoundingBox], blur_radius: int = 30) -> Image.Image:
        """
        Blur detected license plates in image
        """
        result = image.copy()

        for box in boxes:
            # Extract plate region
            x1, y1, x2, y2 = int(box.x1), int(box.y1), int(box.x2), int(box.y2)

            # Ensure bounds are within image
            x1 = max(0, x1)
            y1 = max(0, y1)
            x2 = min(image.width, x2)
            y2 = min(image.height, y2)

            if x2 > x1 and y2 > y1:
                # Crop, blur, and paste back
                plate_region = result.crop((x1, y1, x2, y2))
                blurred = plate_region.filter(ImageFilter.GaussianBlur(radius=blur_radius))
                result.paste(blurred, (x1, y1))

        return result


class S3Client:
    """S3 storage client"""

    def __init__(self):
        self.client = boto3.client(
            "s3",
            endpoint_url=S3_ENDPOINT if "amazonaws" not in S3_ENDPOINT else None,
            aws_access_key_id=S3_ACCESS_KEY,
            aws_secret_access_key=S3_SECRET_KEY,
            region_name=S3_REGION,
            config=Config(signature_version="s3v4"),
        )
        self.bucket = S3_BUCKET

    async def download_image(self, url: str) -> Image.Image:
        """Download image from URL"""
        async with httpx.AsyncClient() as client:
            response = await client.get(url)
            response.raise_for_status()
        return Image.open(io.BytesIO(response.content)).convert("RGB")

    async def upload_image(self, image: Image.Image, key: str, format: str = "WEBP", quality: int = 90) -> str:
        """Upload image to S3"""
        buffer = io.BytesIO()
        image.save(buffer, format=format, quality=quality)
        buffer.seek(0)

        content_type = {"WEBP": "image/webp", "PNG": "image/png", "JPEG": "image/jpeg"}.get(format, "image/webp")

        loop = asyncio.get_event_loop()
        await loop.run_in_executor(
            None,
            lambda: self.client.put_object(
                Bucket=self.bucket, Key=key, Body=buffer.getvalue(), ContentType=content_type, ACL="public-read"
            ),
        )

        return f"https://{self.bucket}.s3.{S3_REGION}.amazonaws.com/{key}"


class YOLOWorker:
    """Main worker for YOLO detection"""

    def __init__(self):
        self.processor = YOLOProcessor(MODEL_PATH, PLATE_MODEL_PATH, DEVICE)
        self.s3 = S3Client()
        self.connection = None
        self.channel = None

    async def connect(self):
        """Connect to RabbitMQ"""
        self.connection = await aio_pika.connect_robust(f"amqp://{RABBITMQ_USER}:{RABBITMQ_PASS}@{RABBITMQ_HOST}/")
        self.channel = await self.connection.channel()
        await self.channel.set_qos(prefetch_count=1)

        queue = await self.channel.declare_queue(QUEUE_NAME, durable=True)
        await queue.consume(self.process_message)

        logger.info(f"YOLO Worker connected on queue: {QUEUE_NAME}")

    async def process_message(self, message: aio_pika.IncomingMessage):
        """Process detection message"""
        import time

        start_time = time.time()

        async with message.process():
            try:
                data = json.loads(message.body.decode())

                # MassTransit sends extra envelope fields - extract only what we need
                expected_fields = {"job_id", "vehicle_id", "image_url", "blur_plates"}

                # Check if message is wrapped in MassTransit envelope
                if "message" in data and isinstance(data["message"], dict):
                    filtered_data = {k: v for k, v in data["message"].items() if k in expected_fields}
                else:
                    filtered_data = {k: v for k, v in data.items() if k in expected_fields}

                # Default blur_plates to True if not present
                if "blur_plates" not in filtered_data:
                    filtered_data["blur_plates"] = True

                msg = DetectionMessage(**filtered_data)

                logger.info(f"Processing detection for job {msg.job_id}")

                # Download image
                image = await self.s3.download_image(msg.image_url)
                image_np = np.array(image)

                # Detect plates
                plates = self.processor.detect_plates(image_np)

                blurred_url = None

                # Blur plates if requested
                if msg.blur_plates and plates:
                    blurred = self.processor.blur_plates(image, plates)

                    # Upload blurred image
                    key = f"blurred/{msg.vehicle_id}/{msg.job_id}.webp"
                    blurred_url = await self.s3.upload_image(blurred, key)

                processing_time_ms = int((time.time() - start_time) * 1000)

                result = DetectionResult(
                    job_id=msg.job_id,
                    success=True,
                    plates_detected=len(plates),
                    plates=[
                        {
                            "x1": p.x1,
                            "y1": p.y1,
                            "x2": p.x2,
                            "y2": p.y2,
                            "confidence": p.confidence,
                            "class": p.class_name,
                        }
                        for p in plates
                    ],
                    blurred_url=blurred_url,
                    processing_time_ms=processing_time_ms,
                )

                await self._report_result(result)

                logger.info(f"Job {msg.job_id}: detected {len(plates)} plates in {processing_time_ms}ms")

            except Exception as e:
                logger.error(f"Error in detection: {e}")

                await self._report_result(
                    DetectionResult(
                        job_id=data.get("job_id", "unknown"),
                        success=False,
                        error_message=str(e),
                        processing_time_ms=int((time.time() - start_time) * 1000),
                    )
                )

    async def _report_result(self, result: DetectionResult):
        """Report result to API"""
        try:
            async with httpx.AsyncClient() as client:
                await client.post(
                    f"{API_CALLBACK_URL}/api/aiprocessing/detection-callback",
                    json={
                        "job_id": result.job_id,
                        "success": result.success,
                        "plates_detected": result.plates_detected,
                        "plates": result.plates,
                        "blurred_url": result.blurred_url,
                        "error_message": result.error_message,
                        "processing_time_ms": result.processing_time_ms,
                    },
                    timeout=30,
                )
        except Exception as e:
            logger.error(f"Failed to report: {e}")

    async def run(self):
        """Run the worker"""
        await self.connect()

        try:
            await asyncio.Future()
        finally:
            if self.connection:
                await self.connection.close()


async def main():
    logger.info("Starting YOLO Worker...")
    logger.info(f"Device: {DEVICE}")
    logger.info(f"Model: {MODEL_PATH}")

    worker = YOLOWorker()
    await worker.run()


if __name__ == "__main__":
    asyncio.run(main())
