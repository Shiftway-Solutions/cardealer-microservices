#!/usr/bin/env python3
"""
Local Batch Processor for SAM2 Worker
Processes all images in /app/input and saves to /app/output

Usage: python3 process_local_batch.py

Improvements:
- Mask refinement (removes artifacts, fills holes, smooths edges)
- Alpha matting for smooth edge transitions
- Better fallback handling
"""

import logging
import os
import sys
from pathlib import Path

import cv2
import numpy as np
import torch
from PIL import Image

# Import mask refinement module
try:
    from mask_refinement import AlphaMatting, MaskRefinement
except ImportError:
    # Add current directory to path
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    from mask_refinement import AlphaMatting, MaskRefinement

logging.basicConfig(level=logging.INFO, format="%(asctime)s - %(levelname)s - %(message)s")
logger = logging.getLogger(__name__)

INPUT_DIR = os.getenv("INPUT_DIR", "/app/input")
OUTPUT_DIR = os.getenv("OUTPUT_DIR", "/app/output")
DEVICE = os.getenv("DEVICE", "cuda" if torch.cuda.is_available() else "cpu")


class VehicleDetector:
    """YOLO-based vehicle detector"""

    VEHICLE_CLASSES = {2: "car", 3: "motorcycle", 5: "bus", 7: "truck"}

    def __init__(self, device: str = "cpu"):
        self.device = device
        self.model = None
        self._load_model()

    def _load_model(self):
        try:
            from ultralytics import YOLO

            original_load = torch.load

            def patched_load(*args, **kwargs):
                kwargs["weights_only"] = False
                return original_load(*args, **kwargs)

            torch.load = patched_load
            self.model = YOLO("yolov8n.pt")
            torch.load = original_load
            logger.info("✅ YOLO vehicle detector loaded")
        except Exception as e:
            logger.warning(f"⚠️ Failed to load YOLO: {e}")
            self.model = None

    def detect_vehicle(self, image: np.ndarray):
        if self.model is None:
            return None

        try:
            results = self.model(image, device=self.device, verbose=False)
            best_box = None
            best_area = 0

            for result in results:
                for box in result.boxes:
                    class_id = int(box.cls[0])
                    if class_id in self.VEHICLE_CLASSES:
                        x1, y1, x2, y2 = box.xyxy[0].tolist()
                        conf = float(box.conf[0])
                        if conf > 0.3:
                            area = (x2 - x1) * (y2 - y1)
                            if area > best_area:
                                best_area = area
                                best_box = np.array([x1, y1, x2, y2])
            return best_box
        except Exception as e:
            logger.error(f"Detection failed: {e}")
            return None


class SAM2Processor:
    """SAM2-based vehicle segmentation with refinement"""

    def __init__(self, device: str = "cpu"):
        self.device = device
        self.model = None
        self.predictor = None
        self._load_model()
        self.vehicle_detector = VehicleDetector(device)

        # Professional mask refinement - clean edges, minimal artifacts
        self.mask_refiner = MaskRefinement(
            min_area_ratio=0.03,
            max_area_ratio=0.95,
            edge_feather_radius=3,  # Small - just for antialiasing
            morphology_kernel_size=5,
            enable_antialiasing=True,
        )

        # Professional alpha matting: 3-5px transition zone (industry standard)
        self.alpha_matter = AlphaMatting(feather_radius=4)

    def _load_model(self):
        """Load SAM2 model"""
        model_path = "/models/sam2_hiera_base_plus.pt"

        if os.path.exists(model_path):
            try:
                from sam2.build_sam import build_sam2
                from sam2.sam2_image_predictor import SAM2ImagePredictor

                logger.info(f"Loading SAM2 model from {model_path}...")
                self.model = build_sam2(config_file="sam2_hiera_b+.yaml", ckpt_path=model_path, device=self.device)
                self.predictor = SAM2ImagePredictor(self.model)
                logger.info("✅ SAM2 model loaded successfully")
            except Exception as e:
                logger.warning(f"⚠️ Failed to load SAM2: {e}, using fallback")
                self.model = None
        else:
            logger.warning(f"⚠️ Model not found at {model_path}, using fallback")

    def segment_vehicle(self, image: np.ndarray):
        """Segment vehicle from image with refinement, return mask"""
        # Get vehicle bounding box from YOLO
        bbox = self.vehicle_detector.detect_vehicle(image)
        raw_mask = None

        if self.predictor is not None and bbox is not None:
            try:
                # Add ASYMMETRIC padding - more on bottom for wheels
                h, w = image.shape[:2]
                x1, y1, x2, y2 = bbox
                box_width = x2 - x1
                box_height = y2 - y1

                # Asymmetric padding: more on sides and bottom
                pad_left = box_width * 0.15  # 15% left
                pad_right = box_width * 0.15  # 15% right
                pad_top = box_height * 0.10  # 10% top (less needed)
                pad_bottom = box_height * 0.25  # 25% bottom (wheels, shadows)

                padded_bbox = np.array(
                    [max(0, x1 - pad_left), max(0, y1 - pad_top), min(w, x2 + pad_right), min(h, y2 + pad_bottom)]
                )

                logger.info(
                    f"  📦 BBox padding: L={pad_left:.0f}, R={pad_right:.0f}, T={pad_top:.0f}, B={pad_bottom:.0f}"
                )

                self.predictor.set_image(image)

                # Create prompt points inside the vehicle (center and key areas)
                # These positive points tell SAM2 "this is what I want to segment"
                center_x = (x1 + x2) / 2
                center_y = (y1 + y2) / 2

                # Multiple points for better coverage:
                # - Center of vehicle
                # - Upper center (hood/roof)
                # - Left and right sides
                point_coords = np.array(
                    [
                        [center_x, center_y],  # Center
                        [center_x, y1 + box_height * 0.3],  # Upper (hood area)
                        [center_x, y1 + box_height * 0.7],  # Lower (body)
                        [x1 + box_width * 0.25, center_y],  # Left side
                        [x2 - box_width * 0.25, center_y],  # Right side
                    ]
                )

                # All positive points (1 = foreground, 0 = background)
                point_labels = np.array([1, 1, 1, 1, 1])

                logger.info(f"  📍 Using {len(point_coords)} prompt points for better segmentation")

                masks, scores, _ = self.predictor.predict(
                    point_coords=point_coords, point_labels=point_labels, box=padded_bbox, multimask_output=True
                )
                # Use best mask
                best_idx = np.argmax(scores)
                raw_mask = masks[best_idx]
                logger.info(f"SAM2 segmentation complete, score: {scores[best_idx]:.3f}")

            except Exception as e:
                logger.error(f"SAM2 segmentation failed: {e}")
                raw_mask = None

        # Fallback: use bounding box as mask
        if raw_mask is None and bbox is not None:
            h, w = image.shape[:2]
            raw_mask = np.zeros((h, w), dtype=bool)
            x1, y1, x2, y2 = map(int, bbox)
            raw_mask[y1:y2, x1:x2] = True
            logger.info("Using bounding box as fallback mask")

        # Last resort: center region
        if raw_mask is None:
            h, w = image.shape[:2]
            raw_mask = np.zeros((h, w), dtype=bool)
            margin_x = int(w * 0.1)
            margin_y = int(h * 0.1)
            raw_mask[margin_y : h - margin_y, margin_x : w - margin_x] = True
            logger.info("Using center region as fallback mask")

        # Convert to uint8 for refinement
        mask_uint8 = raw_mask.astype(np.uint8) * 255

        # Apply mask refinement
        refined_mask, metadata = self.mask_refiner.refine_mask(mask_uint8, image, bbox)
        logger.info(f"Mask refined: {metadata['refinement_applied']}")

        # Return as boolean for compatibility
        return refined_mask > 127

    def _add_drop_shadow(
        self,
        image: np.ndarray,
        mask: np.ndarray,
        shadow_color: tuple = (200, 200, 200),
        shadow_blur: int = 25,
        shadow_offset_y: int = 15,
        shadow_opacity: float = 0.4,
    ) -> np.ndarray:
        """
        Add a professional drop shadow below the vehicle.
        Creates a natural ground shadow effect.

        Args:
            image: RGB image with white background
            mask: Binary mask of the vehicle
            shadow_color: Shadow color (R, G, B)
            shadow_blur: Gaussian blur radius for shadow softness
            shadow_offset_y: Vertical offset (positive = down)
            shadow_opacity: Shadow transparency (0-1)
        """
        h, w = image.shape[:2]
        result = image.copy()

        # Normalize mask to 0-1
        mask_norm = mask.astype(np.float32) / 255.0 if mask.max() > 1 else mask.astype(np.float32)

        # Find the bottom of the vehicle for ground plane
        mask_binary = mask_norm > 0.5
        rows_with_vehicle = np.any(mask_binary, axis=1)
        if not np.any(rows_with_vehicle):
            return image

        vehicle_bottom = np.max(np.where(rows_with_vehicle)[0])

        # Create shadow mask (compressed vertically to simulate ground plane perspective)
        shadow_mask = np.zeros((h, w), dtype=np.float32)

        # Copy lower portion of vehicle mask for shadow
        # Only use bottom 30% of vehicle for shadow base
        vehicle_height = np.sum(rows_with_vehicle)
        shadow_start_row = vehicle_bottom - int(vehicle_height * 0.3)

        for row in range(shadow_start_row, vehicle_bottom):
            if row >= h:
                continue
            # Compress shadow vertically (perspective effect)
            progress = (row - shadow_start_row) / max(1, (vehicle_bottom - shadow_start_row))
            shadow_row_idx = min(h - 1, vehicle_bottom + int(shadow_offset_y * (1 + progress * 0.5)))

            # Copy and fade the shadow
            shadow_mask[shadow_row_idx, :] = np.maximum(
                shadow_mask[shadow_row_idx, :], mask_norm[row, :] * progress * shadow_opacity
            )

        # Stretch shadow horizontally (perspective)
        for row in range(vehicle_bottom, min(h, vehicle_bottom + shadow_offset_y + shadow_blur)):
            if np.any(shadow_mask[row] > 0):
                # Find shadow bounds
                cols = np.where(shadow_mask[row] > 0)[0]
                if len(cols) > 0:
                    center = (cols[0] + cols[-1]) // 2
                    # Expand horizontally
                    stretch_factor = 1.1 + 0.02 * (row - vehicle_bottom)
                    new_left = int(center - (center - cols[0]) * stretch_factor)
                    new_right = int(center + (cols[-1] - center) * stretch_factor)
                    new_left = max(0, new_left)
                    new_right = min(w - 1, new_right)

                    # Create stretched shadow line
                    old_shadow = shadow_mask[row, cols[0] : cols[-1] + 1].copy()
                    shadow_mask[row, :] = 0
                    if len(old_shadow) > 0 and (new_right - new_left) > 0:
                        stretched = cv2.resize(old_shadow.reshape(1, -1), (new_right - new_left + 1, 1)).flatten()
                        shadow_mask[row, new_left : new_left + len(stretched)] = stretched

        # Apply Gaussian blur for soft shadow edges
        shadow_mask = cv2.GaussianBlur(shadow_mask, (shadow_blur * 2 + 1, shadow_blur * 2 + 1), 0)

        # Clip shadow to not overlap with vehicle
        shadow_mask[mask_norm > 0.5] = 0

        # Apply shadow to white background areas only
        for c in range(3):
            # Darken white areas based on shadow
            shadow_effect = (255 - shadow_color[c]) * shadow_mask
            result[:, :, c] = np.clip(result[:, :, c].astype(np.float32) - shadow_effect, 0, 255).astype(np.uint8)

        logger.info(f"  🌑 Added drop shadow (blur={shadow_blur}, offset={shadow_offset_y})")
        return result

    def process_image(self, image_path: Path, output_dir: Path):
        """Process single image and save results with improved quality"""
        logger.info(f"\n🔄 Processing: {image_path.name}")

        # Load image
        pil_image = Image.open(image_path).convert("RGB")
        image_array = np.array(pil_image)

        # Get segmentation mask (already refined)
        mask = self.segment_vehicle(image_array)

        # Ensure mask is boolean
        if mask.dtype != bool:
            mask = mask > 127 if mask.max() > 1 else mask.astype(bool)

        base_name = image_path.stem

        # 1. Background Removal with Alpha Matting (smooth edges)
        # Create alpha matte for smooth transitions
        mask_uint8 = mask.astype(np.uint8) * 255
        alpha_matte = self.alpha_matter.create_alpha_matte(mask_uint8, image_array)

        # Create white background
        h, w = image_array.shape[:2]
        white_bg = np.full((h, w, 3), 255, dtype=np.uint8)

        # Alpha composite for smooth edges
        bg_removed_array = self.alpha_matter.apply_alpha_composite(image_array, white_bg, alpha_matte)

        # Skip artificial drop shadow - looks unprofessional
        # Just use the clean alpha-composited image
        bg_removed = Image.fromarray(bg_removed_array)
        bg_output = output_dir / f"{base_name}_bg_removed.png"
        bg_removed.save(bg_output, quality=95)
        logger.info(f"  ✅ Saved: {bg_output.name} (professional alpha matting)")

        # 2. Segmentation Mask (refined binary)
        mask_image = Image.fromarray(mask_uint8)
        seg_output = output_dir / f"{base_name}_segmented.png"
        mask_image.save(seg_output)
        logger.info(f"  ✅ Saved: {seg_output.name} (refined mask)")

        # 3. Alpha Matte (for debugging/quality check)
        alpha_output = output_dir / f"{base_name}_alpha.png"
        Image.fromarray(alpha_matte).save(alpha_output)
        logger.info(f"  ✅ Saved: {alpha_output.name} (alpha matte)")

        return True


def main():
    print("=" * 60)
    print("🖼️  SAM2 Local Batch Processor")
    print("=" * 60)
    print(f"📁 Input:  {INPUT_DIR}")
    print(f"📁 Output: {OUTPUT_DIR}")
    print(f"🔧 Device: {DEVICE}")
    print("=" * 60)

    input_path = Path(INPUT_DIR)
    output_path = Path(OUTPUT_DIR)
    output_path.mkdir(parents=True, exist_ok=True)

    # Find images
    images = list(input_path.glob("*.jpg")) + list(input_path.glob("*.jpeg")) + list(input_path.glob("*.png"))

    if not images:
        logger.error(f"❌ No images found in {INPUT_DIR}")
        return 1

    logger.info(f"📷 Found {len(images)} images")

    # Initialize processor
    processor = SAM2Processor(DEVICE)

    # Process each image
    success = 0
    for img_path in images:
        try:
            if processor.process_image(img_path, output_path):
                success += 1
        except Exception as e:
            logger.error(f"❌ Failed to process {img_path.name}: {e}")

    print("\n" + "=" * 60)
    print(f"✅ Processed {success}/{len(images)} images")
    print(f"📁 Output saved to: {OUTPUT_DIR}")
    print("=" * 60)

    return 0


if __name__ == "__main__":
    sys.exit(main())
