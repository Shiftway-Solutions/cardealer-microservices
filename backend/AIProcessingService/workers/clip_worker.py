"""
CLIP Worker - Zero-Shot Image Classification
Uses OpenAI's CLIP for image classification and angle detection

Features:
- Vehicle type classification (Exterior/Interior/Engine/Trunk/Wheel/Misc)
- Angle detection (Front, Side, Rear, Front-Left, etc.)
- Damage detection
- Quality assessment
"""

import asyncio
import io
import json
import logging
import os
from dataclasses import dataclass
from typing import Dict, List, Optional, Tuple

import aio_pika
import httpx
import numpy as np
import torch
from PIL import Image

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# ========== Configuration ==========
RABBITMQ_HOST = os.getenv("RABBITMQ_HOST", "rabbitmq")
RABBITMQ_USER = os.getenv("RABBITMQ_USER", "guest")
RABBITMQ_PASS = os.getenv("RABBITMQ_PASS", "guest")
QUEUE_NAME = "ai-processing-classification"

MODEL_NAME = os.getenv("CLIP_MODEL", "ViT-L/14@336px")
DEVICE = os.getenv("DEVICE", "cuda" if torch.cuda.is_available() else "cpu")

API_CALLBACK_URL = os.getenv("API_CALLBACK_URL", "http://aiprocessingservice:8080")


@dataclass
class ClassificationMessage:
    job_id: str
    vehicle_id: str
    image_url: str


@dataclass
class ClassificationResult:
    job_id: str
    success: bool
    image_type: Optional[str] = None
    image_type_confidence: float = 0.0
    angle: Optional[str] = None
    angle_confidence: float = 0.0
    quality_score: float = 0.0
    has_damage: bool = False
    damage_confidence: float = 0.0
    all_predictions: Optional[Dict] = None
    error_message: Optional[str] = None


# Classification labels
IMAGE_TYPE_LABELS = [
    "exterior photo of a car showing the outside body",
    "interior photo of a car showing the cabin and seats",
    "photo of a car engine bay",
    "photo of a car trunk or cargo area",
    "close-up photo of a car wheel or tire",
    "photo of a car dashboard and controls",
    "photo of car detail or miscellaneous part",
]

IMAGE_TYPE_MAP = {0: "Exterior", 1: "Interior", 2: "Engine", 3: "Trunk", 4: "Wheel", 5: "Dashboard", 6: "Misc"}

ANGLE_LABELS = [
    "front view of a car",
    "side view of a car",
    "rear view of a car",
    "front-left angle view of a car",
    "front-right angle view of a car",
    "rear-left angle view of a car",
    "rear-right angle view of a car",
    "top view of a car",
    "close-up detail shot",
]

ANGLE_MAP = {
    0: "Front",
    1: "Side",
    2: "Rear",
    3: "FrontLeft",
    4: "FrontRight",
    5: "RearLeft",
    6: "RearRight",
    7: "Top",
    8: "Detail",
}

DAMAGE_LABELS = [
    "a car with no visible damage, clean and intact",
    "a car with visible damage, dents, scratches or broken parts",
]

QUALITY_LABELS = [
    "a very low quality, blurry, dark photo",
    "a low quality photo with poor lighting",
    "an average quality photo",
    "a good quality, clear photo",
    "a professional, high quality, well-lit photo",
]


class CLIPProcessor:
    """CLIP-based image classification processor"""

    def __init__(self, model_name: str, device: str = "cuda"):
        self.device = device
        self.model = None
        self.preprocess = None
        self._load_model(model_name)

    def _load_model(self, model_name: str):
        """Load CLIP model"""
        logger.info(f"Loading CLIP model {model_name} on {self.device}")

        try:
            import clip

            self.model, self.preprocess = clip.load(model_name, device=self.device)
            self.model.eval()

            logger.info("CLIP model loaded successfully")

        except ImportError:
            logger.warning("CLIP not installed, using mock for development")
            self.model = None

    def _encode_text(self, texts: List[str]) -> torch.Tensor:
        """Encode text labels to embeddings"""
        import clip

        tokens = clip.tokenize(texts).to(self.device)
        with torch.no_grad():
            text_features = self.model.encode_text(tokens)
        return text_features / text_features.norm(dim=-1, keepdim=True)

    def _encode_image(self, image: Image.Image) -> torch.Tensor:
        """Encode image to embedding"""
        image_input = self.preprocess(image).unsqueeze(0).to(self.device)
        with torch.no_grad():
            image_features = self.model.encode_image(image_input)
        return image_features / image_features.norm(dim=-1, keepdim=True)

    def classify(self, image: Image.Image, labels: List[str]) -> Tuple[int, float, List[float]]:
        """
        Classify image against text labels

        Returns:
            Tuple of (best_index, confidence, all_probabilities)
        """
        if self.model is None:
            # Mock classification for development
            probs = [1.0 / len(labels)] * len(labels)
            best_idx = 0
            return best_idx, probs[best_idx], probs

        # Encode image and text
        image_features = self._encode_image(image)
        text_features = self._encode_text(labels)

        # Calculate similarity
        similarity = (100.0 * image_features @ text_features.T).softmax(dim=-1)
        probs = similarity[0].cpu().numpy().tolist()

        best_idx = int(np.argmax(probs))
        confidence = probs[best_idx]

        return best_idx, confidence, probs

    def classify_vehicle_image(self, image: Image.Image) -> Dict:
        """
        Full classification of a vehicle image

        Returns dict with:
        - image_type: Exterior/Interior/Engine/etc.
        - angle: Front/Side/Rear/etc.
        - quality_score: 0-100
        - has_damage: bool
        """
        # Image type classification
        type_idx, type_conf, type_probs = self.classify(image, IMAGE_TYPE_LABELS)
        image_type = IMAGE_TYPE_MAP[type_idx]

        # Angle detection (only for exterior images)
        if image_type == "Exterior":
            angle_idx, angle_conf, angle_probs = self.classify(image, ANGLE_LABELS)
            angle = ANGLE_MAP[angle_idx]
        else:
            angle = None
            angle_conf = 0.0
            angle_probs = []

        # Quality assessment
        quality_idx, quality_conf, quality_probs = self.classify(image, QUALITY_LABELS)
        quality_score = (quality_idx + 1) * 20  # 20, 40, 60, 80, 100

        # Damage detection (only for exterior)
        if image_type == "Exterior":
            damage_idx, damage_conf, damage_probs = self.classify(image, DAMAGE_LABELS)
            has_damage = damage_idx == 1
        else:
            has_damage = False
            damage_conf = 0.0

        return {
            "image_type": image_type,
            "image_type_confidence": type_conf,
            "image_type_all": dict(zip([IMAGE_TYPE_MAP[i] for i in range(len(IMAGE_TYPE_LABELS))], type_probs)),
            "angle": angle,
            "angle_confidence": angle_conf,
            "angle_all": (
                dict(zip([ANGLE_MAP[i] for i in range(len(ANGLE_LABELS))], angle_probs)) if angle_probs else {}
            ),
            "quality_score": quality_score,
            "quality_confidence": quality_conf,
            "has_damage": has_damage,
            "damage_confidence": damage_conf,
        }


class CLIPWorker:
    """Main worker class for CLIP classification"""

    def __init__(self):
        self.processor = CLIPProcessor(MODEL_NAME, DEVICE)
        self.connection = None
        self.channel = None

    async def connect(self):
        """Connect to RabbitMQ"""
        self.connection = await aio_pika.connect_robust(f"amqp://{RABBITMQ_USER}:{RABBITMQ_PASS}@{RABBITMQ_HOST}/")
        self.channel = await self.connection.channel()
        await self.channel.set_qos(prefetch_count=1)

        queue = await self.channel.declare_queue(QUEUE_NAME, durable=True)
        await queue.consume(self.process_message)

        logger.info(f"CLIP Worker connected and listening on queue: {QUEUE_NAME}")

    async def download_image(self, url: str) -> Image.Image:
        """Download image from URL"""
        async with httpx.AsyncClient() as client:
            response = await client.get(url)
            response.raise_for_status()
        return Image.open(io.BytesIO(response.content)).convert("RGB")

    async def process_message(self, message: aio_pika.IncomingMessage):
        """Process classification message"""
        async with message.process():
            try:
                data = json.loads(message.body.decode())

                # MassTransit sends extra envelope fields - extract only what we need
                expected_fields = {"job_id", "vehicle_id", "image_url"}

                # Check if message is wrapped in MassTransit envelope
                if "message" in data and isinstance(data["message"], dict):
                    filtered_data = {k: v for k, v in data["message"].items() if k in expected_fields}
                else:
                    filtered_data = {k: v for k, v in data.items() if k in expected_fields}

                msg = ClassificationMessage(**filtered_data)

                logger.info(f"Classifying image for job {msg.job_id}")

                # Download image
                image = await self.download_image(msg.image_url)

                # Classify
                result = self.processor.classify_vehicle_image(image)

                # Report result
                classification_result = ClassificationResult(
                    job_id=msg.job_id,
                    success=True,
                    image_type=result["image_type"],
                    image_type_confidence=result["image_type_confidence"],
                    angle=result["angle"],
                    angle_confidence=result["angle_confidence"],
                    quality_score=result["quality_score"],
                    has_damage=result["has_damage"],
                    damage_confidence=result["damage_confidence"],
                    all_predictions=result,
                )

                await self._report_result(classification_result)

                logger.info(f"Job {msg.job_id} classified: {result['image_type']} ({result['angle']})")

            except Exception as e:
                logger.error(f"Error classifying: {e}")

                await self._report_result(
                    ClassificationResult(job_id=data.get("job_id", "unknown"), success=False, error_message=str(e))
                )

    async def _report_result(self, result: ClassificationResult):
        """Report result back to API"""
        try:
            async with httpx.AsyncClient() as client:
                await client.post(
                    f"{API_CALLBACK_URL}/api/aiprocessing/classification-callback",
                    json={
                        "job_id": result.job_id,
                        "success": result.success,
                        "image_type": result.image_type,
                        "image_type_confidence": result.image_type_confidence,
                        "angle": result.angle,
                        "angle_confidence": result.angle_confidence,
                        "quality_score": result.quality_score,
                        "has_damage": result.has_damage,
                        "damage_confidence": result.damage_confidence,
                        "all_predictions": result.all_predictions,
                        "error_message": result.error_message,
                    },
                    timeout=30,
                )
        except Exception as e:
            logger.error(f"Failed to report result: {e}")

    async def run(self):
        """Run the worker"""
        await self.connect()

        try:
            await asyncio.Future()
        finally:
            if self.connection:
                await self.connection.close()


async def main():
    logger.info("Starting CLIP Worker...")
    logger.info(f"Device: {DEVICE}")
    logger.info(f"Model: {MODEL_NAME}")

    worker = CLIPWorker()
    await worker.run()


if __name__ == "__main__":
    asyncio.run(main())
