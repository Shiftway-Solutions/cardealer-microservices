"""
SAM2 Worker - Vehicle Segmentation & Background Replacement
Uses Meta's Segment Anything Model 2 for precise vehicle isolation

Features:
- Vehicle segmentation with point/box prompts
- Background removal (transparent PNG)
- Background replacement with presets
- Shadow generation
- Batch processing support
"""

import asyncio
import io
import json
import logging
import os
from dataclasses import dataclass
from enum import Enum
from typing import Optional, Tuple

import aio_pika
import httpx
import numpy as np
import torch
from PIL import Image

# Import mask refinement module
from mask_refinement import AlphaMatting, MaskRefinement

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class VehicleDetector:
    """YOLO-based vehicle detector to provide bounding boxes for SAM2"""

    # Vehicle class IDs in COCO dataset
    VEHICLE_CLASSES = {2: "car", 3: "motorcycle", 5: "bus", 7: "truck"}

    def __init__(self, device: str = "cpu"):
        self.device = device
        self.model = None
        self._load_model()

    def _load_model(self):
        """Load YOLO model for vehicle detection"""
        try:
            import torch
            from ultralytics import YOLO

            # Fix for PyTorch 2.6+ weights_only issue
            original_load = torch.load

            def patched_load(*args, **kwargs):
                kwargs["weights_only"] = False
                return original_load(*args, **kwargs)

            torch.load = patched_load

            # Use YOLOv8n for fast detection (we just need bounding box)
            self.model = YOLO("yolov8n.pt")

            # Restore original torch.load
            torch.load = original_load

            logger.info("YOLO vehicle detector loaded successfully")
        except ImportError:
            logger.warning("Ultralytics not installed, vehicle detection disabled")
            self.model = None
        except Exception as e:
            logger.warning(f"Failed to load YOLO: {e}")
            self.model = None

    def detect_vehicle(self, image: np.ndarray) -> Optional[np.ndarray]:
        """
        Detect the main vehicle in the image and return its bounding box.

        Args:
            image: Input image as numpy array (RGB)

        Returns:
            Bounding box as numpy array [x1, y1, x2, y2] or None if no vehicle found
        """
        if self.model is None:
            logger.warning("YOLO not available, cannot detect vehicle bounding box")
            return None

        try:
            # Run detection
            results = self.model(image, device=self.device, verbose=False)

            best_box = None
            best_area = 0

            for result in results:
                for box in result.boxes:
                    class_id = int(box.cls[0])

                    # Check if it's a vehicle class
                    if class_id in self.VEHICLE_CLASSES:
                        x1, y1, x2, y2 = box.xyxy[0].tolist()
                        conf = float(box.conf[0])

                        # Only consider high confidence detections
                        if conf > 0.3:
                            area = (x2 - x1) * (y2 - y1)

                            # Select largest vehicle (main subject)
                            if area > best_area:
                                best_area = area
                                best_box = np.array([x1, y1, x2, y2])
                                logger.info(
                                    f"Detected {self.VEHICLE_CLASSES[class_id]} with confidence {conf:.2f}, area {area:.0f}"
                                )

            if best_box is not None:
                logger.info(f"Selected vehicle bounding box: {best_box}")
            else:
                logger.warning("No vehicle detected in image")

            return best_box

        except Exception as e:
            logger.error(f"Vehicle detection failed: {e}")
            return None


# ========== Configuration ==========
RABBITMQ_HOST = os.getenv("RABBITMQ_HOST", "rabbitmq")
RABBITMQ_USER = os.getenv("RABBITMQ_USER", "guest")
RABBITMQ_PASS = os.getenv("RABBITMQ_PASS", "guest")
QUEUE_NAME = "ai-processing-segmentation"

# MediaService for file uploads (preferred over direct S3)
MEDIA_SERVICE_URL = os.getenv("MEDIA_SERVICE_URL", "http://mediaservice:80")

# Legacy S3 config (fallback if MediaService not available)
S3_ENDPOINT = os.getenv("S3_ENDPOINT", "https://s3.amazonaws.com")
S3_BUCKET = os.getenv("S3_BUCKET", "okla-processed-images")
S3_ACCESS_KEY = os.getenv("S3_ACCESS_KEY", "")
S3_SECRET_KEY = os.getenv("S3_SECRET_KEY", "")
S3_REGION = os.getenv("S3_REGION", "us-east-1")

MODEL_PATH = os.getenv("MODEL_PATH", "/models/sam2_hiera_base_plus.pt")
DEVICE = os.getenv("DEVICE", "cuda" if torch.cuda.is_available() else "cpu")

API_CALLBACK_URL = os.getenv("API_CALLBACK_URL", "http://aiprocessingservice:8080")


class ProcessingType(Enum):
    SEGMENTATION = "Segmentation"
    VEHICLE_SEGMENTATION = "VehicleSegmentation"
    BACKGROUND_REMOVAL = "BackgroundRemoval"
    BACKGROUND_REPLACEMENT = "BackgroundReplacement"
    FULL_PIPELINE = "FullPipeline"  # Segmentation + Background replacement


@dataclass
class ProcessingMessage:
    job_id: str
    vehicle_id: str
    user_id: str
    image_url: str
    processing_type: str
    options: dict


@dataclass
class ProcessingResult:
    job_id: str
    success: bool
    processed_url: Optional[str] = None
    mask_url: Optional[str] = None
    error_message: Optional[str] = None
    processing_time_ms: int = 0
    metadata: Optional[dict] = None


class SAM2Processor:
    """SAM2-based image segmentation processor with mask refinement"""

    def __init__(self, model_path: str, device: str = "cuda"):
        self.device = device
        self.model = None
        self.predictor = None
        self.vehicle_detector = VehicleDetector(device=device)  # Add YOLO detector
        self.mask_refiner = MaskRefinement(
            min_area_ratio=0.05,
            max_area_ratio=0.95,
            edge_feather_radius=3,
            morphology_kernel_size=5,
            enable_antialiasing=True,
        )
        self.alpha_matter = AlphaMatting(feather_radius=5)
        self._load_model(model_path)

    def _load_model(self, model_path: str):
        """Load SAM2 model"""
        logger.info(f"Loading SAM2 model from {model_path} on {self.device}")

        # Check if model file exists
        import os

        if not os.path.exists(model_path):
            logger.warning(f"SAM2 model not found at {model_path}, using mock mode for development")
            logger.warning("To download the model, run:")
            logger.warning(
                "  wget https://dl.fbaipublicfiles.com/segment_anything_2/072824/sam2_hiera_base_plus.pt -O /models/sam2_hiera_base.pt"
            )
            self.model = None
            self.predictor = None
            return

        try:
            # Import SAM2 - adjust import based on your sam2 installation
            from sam2.build_sam import build_sam2
            from sam2.sam2_image_predictor import SAM2ImagePredictor

            # Determine config based on model filename
            model_name = os.path.basename(model_path).lower()
            if "large" in model_name or "_l" in model_name:
                config = "sam2_hiera_l.yaml"
            elif "base_plus" in model_name or "base" in model_name:
                config = "sam2_hiera_b+.yaml"
            elif "small" in model_name or "_s" in model_name:
                config = "sam2_hiera_s.yaml"
            elif "tiny" in model_name or "_t" in model_name:
                config = "sam2_hiera_t.yaml"
            else:
                config = "sam2_hiera_b+.yaml"  # Default to base+

            logger.info(f"Using SAM2 config: {config}")

            # Load model
            self.model = build_sam2(config, model_path, device=self.device)
            self.predictor = SAM2ImagePredictor(self.model)
            logger.info("SAM2 model loaded successfully")

        except ImportError:
            logger.warning("SAM2 not installed, using mock for development")
            self.model = None
            self.predictor = None
        except Exception as e:
            logger.warning(f"Failed to load SAM2 model: {e}, using mock mode")
            self.model = None
            self.predictor = None

    def segment_vehicle(  # noqa: C901
        self,
        image: np.ndarray,
        point_coords: Optional[np.ndarray] = None,
        point_labels: Optional[np.ndarray] = None,
        box: Optional[np.ndarray] = None,
        refine_mask: bool = True,
        num_iterations: int = 2,
    ) -> Tuple[np.ndarray, float]:
        """
        Segment vehicle from image with mask refinement

        Args:
            image: Input image as numpy array (RGB)
            point_coords: Optional point prompts [[x, y], ...]
            point_labels: Labels for points (1=foreground, 0=background)
            box: Optional bounding box [x1, y1, x2, y2]
            refine_mask: Whether to apply post-processing refinement
            num_iterations: Number of SAM2 refinement iterations

        Returns:
            Tuple of (mask, confidence_score)
        """
        detected_box = None

        if self.predictor is None:
            # Mock segmentation for development
            logger.warning("Using mock segmentation")
            h, w = image.shape[:2]
            mask = np.zeros((h, w), dtype=np.uint8)
            # Create simple ellipse mask in center
            center_x, center_y = w // 2, h // 2
            for y in range(h):
                for x in range(w):
                    if ((x - center_x) / (w * 0.4)) ** 2 + ((y - center_y) / (h * 0.3)) ** 2 < 1:
                        mask[y, x] = 255
            return mask, 0.95

        # Set image for predictor
        self.predictor.set_image(image)

        # IMPROVED: Use YOLO to detect vehicle bounding box if no prompts provided
        if point_coords is None and box is None:
            logger.info("No prompts provided, using YOLO to detect vehicle bounding box...")
            detected_box = self.vehicle_detector.detect_vehicle(image)

            if detected_box is not None:
                # Add padding to bounding box for better segmentation
                h, w = image.shape[:2]
                x1, y1, x2, y2 = detected_box
                pad_x = (x2 - x1) * 0.05  # 5% padding
                pad_y = (y2 - y1) * 0.05
                detected_box = np.array(
                    [max(0, x1 - pad_x), max(0, y1 - pad_y), min(w, x2 + pad_x), min(h, y2 + pad_y)]
                )
                box = detected_box
                logger.info(f"Using YOLO-detected vehicle box with padding: {box}")
            else:
                # Fallback: use multiple points across the image center area
                h, w = image.shape[:2]
                logger.warning("No vehicle detected, using center grid fallback")
                center_x, center_y = w // 2, h // 2
                # Use multiple points across the center area
                point_coords = np.array(
                    [
                        [center_x, center_y],  # Center
                        [center_x - w // 6, center_y],  # Left of center
                        [center_x + w // 6, center_y],  # Right of center
                        [center_x, center_y - h // 6],  # Above center
                        [center_x, center_y + h // 6],  # Below center
                    ]
                )
                point_labels = np.array([1, 1, 1, 1, 1])  # All foreground

        # Initial prediction
        if box is not None:
            logger.info(f"Running SAM2 with bounding box prompt: {box}")
            masks, scores, _ = self.predictor.predict(
                point_coords=None, point_labels=None, box=box, multimask_output=True
            )
        else:
            logger.info(f"Running SAM2 with {len(point_coords)} point prompts")
            masks, scores, _ = self.predictor.predict(
                point_coords=point_coords, point_labels=point_labels, box=None, multimask_output=True
            )

        # Select best mask
        best_idx = np.argmax(scores)
        best_mask = masks[best_idx]
        confidence = float(scores[best_idx])

        # Iterative refinement: use mask to generate better prompts
        for iteration in range(1, num_iterations):
            logger.info(f"SAM2 refinement iteration {iteration + 1}/{num_iterations}")

            # Find center of current mask for point prompt
            mask_coords = np.where(best_mask)
            if len(mask_coords[0]) > 0:
                center_y = int(np.mean(mask_coords[0]))
                center_x = int(np.mean(mask_coords[1]))

                # Also get extremes for better coverage
                min_y, max_y = mask_coords[0].min(), mask_coords[0].max()
                min_x, max_x = mask_coords[1].min(), mask_coords[1].max()

                refinement_points = np.array(
                    [
                        [center_x, center_y],  # Center of mask
                        [(min_x + center_x) // 2, center_y],  # Left middle
                        [(max_x + center_x) // 2, center_y],  # Right middle
                    ]
                )
                refinement_labels = np.array([1, 1, 1])

                # Use current mask's bbox as box prompt too
                mask_bbox = np.array([min_x, min_y, max_x, max_y])

                masks, scores, _ = self.predictor.predict(
                    point_coords=refinement_points,
                    point_labels=refinement_labels,
                    box=mask_bbox,
                    multimask_output=True,
                    mask_input=best_mask[None, :, :].astype(np.float32),  # Use previous mask as input
                )

                new_best_idx = np.argmax(scores)
                if scores[new_best_idx] > confidence:
                    best_mask = masks[new_best_idx]
                    confidence = float(scores[new_best_idx])
                    logger.info(f"Iteration {iteration + 1} improved confidence to {confidence:.3f}")

        logger.info(
            f"SAM2 segmentation complete. Confidence: {confidence:.3f}, mask coverage: {best_mask.sum() / best_mask.size * 100:.1f}%"
        )

        # Convert to uint8
        mask_uint8 = (best_mask * 255).astype(np.uint8)

        # Apply mask refinement (post-processing)
        if refine_mask:
            logger.info("Applying mask refinement...")
            mask_uint8, metadata = self.mask_refiner.refine_mask(
                mask_uint8, image=image, bbox=detected_box if detected_box is not None else box
            )
            logger.info(f"Refinement: {metadata['refinement_applied']}")

        return mask_uint8, confidence

    def remove_background(
        self,
        image: np.ndarray,
        mask: np.ndarray,
        background_color: Tuple[int, int, int] = (255, 255, 255),
        use_alpha_matte: bool = True,
        feather_radius: int = 5,
    ) -> Image.Image:
        """
        Remove background with smooth alpha blending for professional results.

        Args:
            image: Original RGB image
            mask: Binary mask (0 = background, 255 = foreground/vehicle)
            background_color: RGB color to use for background (default: white)
            use_alpha_matte: Whether to use smooth alpha transitions (recommended)
            feather_radius: Radius for edge feathering

        Returns:
            PIL Image in RGB mode with colored background
        """
        if use_alpha_matte:
            # Create smooth alpha matte for professional edges
            alpha = self.alpha_matter.create_alpha_matte(mask, image)

            # Create solid color background
            h, w = image.shape[:2]
            background = np.full((h, w, 3), background_color, dtype=np.uint8)

            # Alpha composite
            result = self.alpha_matter.apply_alpha_composite(image, background, alpha)

            # Count affected pixels for logging
            alpha_binary = alpha > 127
            foreground_pixels = alpha_binary.sum()
            background_pixels = (~alpha_binary).sum()

            logger.info(
                f"Alpha matte applied: {foreground_pixels} fg pixels, {background_pixels} bg pixels, feather radius: {feather_radius}"
            )

            return Image.fromarray(result, "RGB")
        else:
            # Simple hard-edge replacement (legacy behavior)
            mask_binary = (mask > 127).astype(np.uint8)
            result = image.copy()
            background_pixels = mask_binary == 0
            result[background_pixels] = np.array(background_color, dtype=np.uint8)

            logger.info(
                f"Hard-edge background removal: {background_pixels.sum()} pixels replaced with color {background_color}"
            )

            return Image.fromarray(result, "RGB")

    def replace_background(
        self,
        image: np.ndarray,
        mask: np.ndarray,
        background: np.ndarray,
        shadow_intensity: float = 0.3,
        shadow_offset: Tuple[int, int] = (10, 10),
        use_alpha_matte: bool = True,
    ) -> Image.Image:
        """
        Replace background with new background image using smooth alpha blending

        Args:
            image: Original image (RGB)
            mask: Vehicle mask
            background: Background image (RGB)
            shadow_intensity: Shadow darkness (0-1)
            shadow_offset: Shadow offset (x, y) in pixels
            use_alpha_matte: Whether to use smooth alpha transitions
        """
        h, w = image.shape[:2]

        # Resize background to match image
        bg_pil = Image.fromarray(background).resize((w, h), Image.LANCZOS)
        background_resized = np.array(bg_pil)

        # Create shadow if requested
        if shadow_intensity > 0:
            shadow = self._create_shadow(mask, shadow_intensity, shadow_offset)
            # Apply shadow to background
            background_resized = self._apply_shadow(background_resized, shadow)

        if use_alpha_matte:
            # Create smooth alpha matte for professional blending
            alpha = self.alpha_matter.create_alpha_matte(mask, image)
            result = self.alpha_matter.apply_alpha_composite(image, background_resized, alpha)
        else:
            # Legacy hard-edge composite
            mask_normalized = mask.astype(np.float32) / 255.0
            mask_3d = np.stack([mask_normalized] * 3, axis=-1)
            result = (image * mask_3d + background_resized * (1 - mask_3d)).astype(np.uint8)

        return Image.fromarray(result, "RGB")

    def _create_shadow(self, mask: np.ndarray, intensity: float, offset: Tuple[int, int]) -> np.ndarray:
        """Create shadow from mask"""
        from scipy.ndimage import gaussian_filter, shift

        # Shift mask to create shadow position
        shadow = shift(mask.astype(np.float32), (offset[1], offset[0]), mode="constant", cval=0)

        # Blur shadow
        shadow = gaussian_filter(shadow, sigma=15)

        # Normalize and apply intensity
        shadow = (shadow / shadow.max()) * intensity if shadow.max() > 0 else shadow

        return shadow

    def _apply_shadow(self, background: np.ndarray, shadow: np.ndarray) -> np.ndarray:
        """Apply shadow to background"""
        shadow_3d = np.stack([shadow] * 3, axis=-1)
        darkened = background * (1 - shadow_3d)
        return darkened.astype(np.uint8)


class MediaServiceClient:
    """Client for uploading files via MediaService API"""

    def __init__(self):
        self.media_service_url = MEDIA_SERVICE_URL
        self.local_path = "/app/processed-images"  # Folder to be mounted as volume
        self.use_local_storage = os.getenv("USE_LOCAL_STORAGE", "true").lower() == "true"
        os.makedirs(self.local_path, exist_ok=True)
        logger.info(f"MediaService URL: {self.media_service_url}")
        logger.info(f"Using local storage: {self.use_local_storage}, path: {self.local_path}")

    async def download_image(self, url: str) -> np.ndarray:
        """Download image from URL and return as numpy array"""
        async with httpx.AsyncClient(timeout=60) as client:
            response = await client.get(url)
            response.raise_for_status()

        image = Image.open(io.BytesIO(response.content))
        return np.array(image.convert("RGB"))

    async def upload_image(
        self,
        image: Image.Image,
        key: str,
        format: str = "WEBP",
        quality: int = 90,
        entity_type: str = "Vehicle",
        entity_id: str = "",
    ) -> str:
        """Upload image via MediaService API and return public URL"""
        # Normalize format
        format = format.upper()
        if format == "JPG":
            format = "JPEG"
        if format not in ["WEBP", "PNG", "JPEG"]:
            format = "WEBP"

        # Convert image to bytes
        buffer = io.BytesIO()
        if format == "JPEG" and image.mode in ("RGBA", "LA", "P"):
            image = image.convert("RGB")

        save_kwargs = {"format": format}
        if format in ["WEBP", "JPEG"]:
            save_kwargs["quality"] = quality

        image.save(buffer, **save_kwargs)
        buffer.seek(0)

        # Determine content type and extension
        content_type = {"WEBP": "image/webp", "PNG": "image/png", "JPEG": "image/jpeg"}.get(format, "image/webp")
        ext = {"WEBP": "webp", "PNG": "png", "JPEG": "jpg"}.get(format, "webp")
        filename = f"{key.replace('/', '_')}.{ext}"

        # Use local storage for development
        if self.use_local_storage:
            local_file = os.path.join(self.local_path, filename)
            with open(local_file, "wb") as f:
                buffer.seek(0)
                f.write(buffer.read())
            # Return URL that AIProcessingService can serve
            local_url = f"http://aiprocessingservice:8080/api/aiprocessing/images/{filename}"
            logger.info(f"Saved image locally: {local_file} -> {local_url}")
            return local_url

        # Try to upload via MediaService
        try:
            async with httpx.AsyncClient(timeout=120) as client:
                files = {"file": (filename, buffer.getvalue(), content_type)}
                # MediaService expects 'folder' parameter for the simple upload endpoint
                data = {"folder": f"ai-processed/{entity_type.lower()}"}

                response = await client.post(f"{self.media_service_url}/api/media/upload/image", files=files, data=data)

                if response.status_code in [200, 201]:
                    result = response.json()
                    public_url = result.get("url", result.get("publicUrl", ""))
                    logger.info(f"Uploaded via MediaService: {public_url}")
                    return public_url
                else:
                    logger.warning(f"MediaService returned {response.status_code}: {response.text}")
                    raise Exception(f"MediaService error: {response.status_code}")

        except Exception as e:
            logger.warning(f"MediaService upload failed: {e}, saving locally")
            # Fallback to local storage
            local_file = os.path.join(self.local_path, filename)
            with open(local_file, "wb") as f:
                buffer.seek(0)
                f.write(buffer.read())
            logger.info(f"Saved image locally: {local_file}")
            return f"file://{local_file}"


class SAM2Worker:
    """Main worker class for SAM2 processing"""

    def __init__(self):
        self.processor = SAM2Processor(MODEL_PATH, DEVICE)
        self.storage = MediaServiceClient()
        self.connection = None
        self.channel = None

    async def connect(self):
        """Connect to RabbitMQ"""
        self.connection = await aio_pika.connect_robust(f"amqp://{RABBITMQ_USER}:{RABBITMQ_PASS}@{RABBITMQ_HOST}/")
        self.channel = await self.connection.channel()
        await self.channel.set_qos(prefetch_count=1)

        # Declare queue
        queue = await self.channel.declare_queue(QUEUE_NAME, durable=True)

        # Start consuming
        await queue.consume(self.process_message)

        logger.info(f"SAM2 Worker connected and listening on queue: {QUEUE_NAME}")

    async def process_message(self, message: aio_pika.IncomingMessage):
        """Process a single message from the queue"""
        import time

        start_time = time.time()

        async with message.process():
            try:
                # Parse message
                data = json.loads(message.body.decode())

                # MassTransit sends extra envelope fields - extract only what we need
                # Expected fields: job_id, vehicle_id, user_id, image_url, processing_type, options
                expected_fields = {"job_id", "vehicle_id", "user_id", "image_url", "processing_type", "options"}

                # Check if message is wrapped in MassTransit envelope
                if "message" in data and isinstance(data["message"], dict):
                    # MassTransit envelope format: {message: {...}, messageId: ..., messageType: ...}
                    filtered_data = {k: v for k, v in data["message"].items() if k in expected_fields}
                else:
                    # Direct message format
                    filtered_data = {k: v for k, v in data.items() if k in expected_fields}

                # Ensure options exists
                if "options" not in filtered_data:
                    filtered_data["options"] = {}

                msg = ProcessingMessage(**filtered_data)

                logger.info(f"Processing job {msg.job_id}: {msg.processing_type}")

                # Download image
                image = await self.storage.download_image(msg.image_url)

                # Get processing options
                options = msg.options or {}

                # Perform segmentation
                mask, confidence = self.processor.segment_vehicle(image)

                # Process based on type
                processing_type = ProcessingType(msg.processing_type)

                if (
                    processing_type == ProcessingType.SEGMENTATION
                    or processing_type == ProcessingType.VEHICLE_SEGMENTATION
                ):
                    # Just return mask as grayscale
                    result_image = Image.fromarray(mask, "L")
                    format = "PNG"

                elif processing_type == ProcessingType.BACKGROUND_REMOVAL:
                    # Return image with removed background (replaced with white)
                    result_image = self.processor.remove_background(image, mask, background_color=(255, 255, 255))
                    format = "PNG"

                elif (
                    processing_type == ProcessingType.BACKGROUND_REPLACEMENT
                    or processing_type == ProcessingType.FULL_PIPELINE
                ):
                    # Get background - default to white studio
                    bg_code = options.get("background_code", options.get("background_id", "white_studio"))
                    background = await self._get_background(bg_code)

                    shadow_intensity = options.get("shadow_intensity", 0.3)

                    result_image = self.processor.replace_background(
                        image, mask, background, shadow_intensity=shadow_intensity
                    )
                    format = options.get("output_format", "WEBP").upper()
                else:
                    raise ValueError(f"Unknown processing type: {msg.processing_type}")

                # Upload results
                quality = options.get("quality", 90)

                processed_key = f"processed_{msg.vehicle_id}_{msg.job_id}"
                processed_url = await self.storage.upload_image(
                    result_image,
                    processed_key,
                    format=format,
                    quality=quality,
                    entity_type="Vehicle",
                    entity_id=msg.vehicle_id,
                )

                mask_key = f"mask_{msg.vehicle_id}_{msg.job_id}"
                mask_url = await self.storage.upload_image(
                    Image.fromarray(mask, "L"), mask_key, format="PNG", entity_type="Vehicle", entity_id=msg.vehicle_id
                )

                # Calculate processing time
                processing_time_ms = int((time.time() - start_time) * 1000)

                # Report success
                result = ProcessingResult(
                    job_id=msg.job_id,
                    success=True,
                    processed_url=processed_url,
                    mask_url=mask_url,
                    processing_time_ms=processing_time_ms,
                    metadata={
                        "confidence": confidence,
                        "model": "SAM2-Hiera-Large",
                        "device": DEVICE,
                        "format": format,
                    },
                )

                await self._report_result(result)

                logger.info(f"Job {msg.job_id} completed in {processing_time_ms}ms")

            except Exception as e:
                logger.error(f"Error processing job: {e}")

                result = ProcessingResult(
                    job_id=data.get("job_id", "unknown"),
                    success=False,
                    error_message=str(e),
                    processing_time_ms=int((time.time() - start_time) * 1000),
                )

                await self._report_result(result)

    async def _get_background(self, code: str) -> np.ndarray:
        """Get background image by code"""
        # Predefined solid color backgrounds
        backgrounds = {
            "white_studio": np.full((1080, 1920, 3), 255, dtype=np.uint8),
            "gray_showroom": np.full((1080, 1920, 3), 229, dtype=np.uint8),
            "dark_studio": np.full((1080, 1920, 3), 26, dtype=np.uint8),
            "outdoor_day": np.array([135, 206, 235], dtype=np.uint8)[None, None, :]
            .repeat(1080, axis=0)
            .repeat(1920, axis=1),
        }

        if code in backgrounds:
            return backgrounds[code]

        # Try to fetch from MediaService or S3
        try:
            url = f"https://{S3_BUCKET}.s3.{S3_REGION}.amazonaws.com/backgrounds/{code}.jpg"
            return await self.storage.download_image(url)
        except Exception:
            logger.warning(f"Background {code} not found, using white")
            return backgrounds["white_studio"]

    async def _report_result(self, result: ProcessingResult):
        """Report processing result back to API"""
        try:
            async with httpx.AsyncClient() as client:
                await client.post(
                    f"{API_CALLBACK_URL}/api/aiprocessing/callback",
                    json={
                        "job_id": result.job_id,
                        "success": result.success,
                        "processed_url": result.processed_url,
                        "mask_url": result.mask_url,
                        "error_message": result.error_message,
                        "processing_time_ms": result.processing_time_ms,
                        "metadata": result.metadata,
                    },
                    timeout=30,
                )
        except Exception as e:
            logger.error(f"Failed to report result: {e}")

    async def run(self):
        """Run the worker"""
        await self.connect()

        # Keep running
        try:
            await asyncio.Future()  # Run forever
        finally:
            if self.connection:
                await self.connection.close()


async def main():
    """Main entry point"""
    logger.info("Starting SAM2 Worker...")
    logger.info(f"Device: {DEVICE}")
    logger.info(f"Model: {MODEL_PATH}")
    logger.info(f"RabbitMQ: {RABBITMQ_HOST}")

    worker = SAM2Worker()
    await worker.run()


if __name__ == "__main__":
    asyncio.run(main())
