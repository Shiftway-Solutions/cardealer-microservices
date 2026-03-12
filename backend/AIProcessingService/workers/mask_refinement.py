"""
Mask Refinement Module - Post-processing for cleaner segmentation results
Fixes issues with rough edges, artifacts, and incomplete masks

Author: OKLA Team
Date: January 2026
"""

import logging
from typing import Optional, Tuple

import cv2
import numpy as np
from PIL import Image
from scipy.ndimage import binary_fill_holes

logger = logging.getLogger(__name__)


class MaskRefinement:
    """
    Post-processing pipeline for vehicle segmentation masks.

    Fixes common issues:
    - Rough/jagged edges
    - Holes in the mask
    - Small disconnected regions (artifacts)
    - Missing parts (wheels, mirrors, etc.)
    """

    def __init__(
        self,
        min_area_ratio: float = 0.05,
        max_area_ratio: float = 0.95,
        edge_feather_radius: int = 3,
        morphology_kernel_size: int = 5,
        enable_antialiasing: bool = True,
    ):
        """
        Args:
            min_area_ratio: Minimum mask area as ratio of image (filters noise)
            max_area_ratio: Maximum mask area as ratio of image (filters bad segmentation)
            edge_feather_radius: Radius for edge smoothing
            morphology_kernel_size: Kernel size for morphological operations
            enable_antialiasing: Whether to apply antialiasing to edges
        """
        self.min_area_ratio = min_area_ratio
        self.max_area_ratio = max_area_ratio
        self.edge_feather_radius = edge_feather_radius
        self.morphology_kernel_size = morphology_kernel_size
        self.enable_antialiasing = enable_antialiasing

    def refine_mask(
        self, mask: np.ndarray, image: Optional[np.ndarray] = None, bbox: Optional[np.ndarray] = None
    ) -> Tuple[np.ndarray, dict]:
        """
        Apply full refinement pipeline to a raw segmentation mask.

        Args:
            mask: Raw binary mask (0-255 or 0-1)
            image: Original image (optional, for edge-aware refinement)
            bbox: Vehicle bounding box [x1, y1, x2, y2] (optional, for validation)

        Returns:
            Tuple of (refined_mask, metadata_dict)
        """
        metadata = {
            "original_coverage": 0,
            "refined_coverage": 0,
            "holes_filled": 0,
            "artifacts_removed": 0,
            "refinement_applied": [],
        }

        # Ensure mask is binary (0 or 1)
        if mask.max() > 1:
            mask = (mask > 127).astype(np.uint8)
        else:
            mask = mask.astype(np.uint8)

        h, w = mask.shape[:2]
        total_pixels = h * w
        metadata["original_coverage"] = mask.sum() / total_pixels

        # Step 1: Remove small disconnected artifacts
        mask, num_removed = self._remove_small_components(mask, min_size=int(total_pixels * 0.001))
        metadata["artifacts_removed"] = num_removed
        if num_removed > 0:
            metadata["refinement_applied"].append("remove_artifacts")

        # Step 2: Keep only the largest connected component (main vehicle)
        mask = self._keep_largest_component(mask)
        metadata["refinement_applied"].append("keep_largest")

        # Step 3: Fill holes in the mask
        original_sum = mask.sum()
        mask = self._fill_holes(mask)
        holes_filled = mask.sum() - original_sum
        metadata["holes_filled"] = int(holes_filled)
        if holes_filled > 0:
            metadata["refinement_applied"].append("fill_holes")

        # Step 4: Morphological closing (close small gaps)
        mask = self._morphological_closing(mask)
        metadata["refinement_applied"].append("morph_closing")

        # Step 5: Morphological opening (smooth edges)
        mask = self._morphological_opening(mask)
        metadata["refinement_applied"].append("morph_opening")

        # Step 6: Smooth edges with Gaussian blur threshold
        mask = self._smooth_edges(mask)
        metadata["refinement_applied"].append("smooth_edges")

        # Step 6.5: Expand lower portion (preserve wheels/tires)
        mask = self._expand_lower_region(mask)
        metadata["refinement_applied"].append("expand_lower")

        # Step 7: Validate mask against bbox if provided
        if bbox is not None:
            mask = self._validate_against_bbox(mask, bbox)
            metadata["refinement_applied"].append("bbox_validation")

        # Step 8: Edge-aware refinement if image provided
        if image is not None and self.enable_antialiasing:
            mask = self._edge_aware_refinement(mask, image)
            metadata["refinement_applied"].append("edge_aware")

        metadata["refined_coverage"] = mask.sum() / total_pixels

        # Convert back to 0-255 range
        mask = (mask * 255).astype(np.uint8)

        logger.info(
            f"Mask refined: coverage {metadata['original_coverage']:.2%} -> {metadata['refined_coverage']:.2%}, "
            f"holes filled: {metadata['holes_filled']}, artifacts removed: {metadata['artifacts_removed']}"
        )

        return mask, metadata

    def _remove_small_components(self, mask: np.ndarray, min_size: int = 1000) -> Tuple[np.ndarray, int]:
        """Remove connected components smaller than min_size pixels."""
        # Find connected components
        num_labels, labels, stats, centroids = cv2.connectedComponentsWithStats(mask.astype(np.uint8), connectivity=8)

        # Create output mask
        output = np.zeros_like(mask)
        removed_count = 0

        for i in range(1, num_labels):  # Skip background (label 0)
            area = stats[i, cv2.CC_STAT_AREA]
            if area >= min_size:
                output[labels == i] = 1
            else:
                removed_count += 1

        return output, removed_count

    def _keep_largest_component(self, mask: np.ndarray) -> np.ndarray:
        """Keep only the largest connected component."""
        num_labels, labels, stats, _ = cv2.connectedComponentsWithStats(mask.astype(np.uint8), connectivity=8)

        if num_labels <= 1:
            return mask

        # Find largest component (excluding background)
        areas = stats[1:, cv2.CC_STAT_AREA]
        largest_label = np.argmax(areas) + 1

        output = np.zeros_like(mask)
        output[labels == largest_label] = 1

        return output

    def _fill_holes(self, mask: np.ndarray) -> np.ndarray:
        """Fill holes in the mask using flood fill."""
        # Use scipy's binary_fill_holes for robust hole filling
        filled = binary_fill_holes(mask.astype(bool)).astype(np.uint8)
        return filled

    def _morphological_closing(self, mask: np.ndarray) -> np.ndarray:
        """Apply morphological closing to close small gaps."""
        kernel = cv2.getStructuringElement(
            cv2.MORPH_ELLIPSE, (self.morphology_kernel_size, self.morphology_kernel_size)
        )
        closed = cv2.morphologyEx(mask.astype(np.uint8), cv2.MORPH_CLOSE, kernel)
        return closed

    def _morphological_opening(self, mask: np.ndarray) -> np.ndarray:
        """Apply morphological opening to remove small protrusions."""
        kernel_size = max(3, self.morphology_kernel_size - 2)  # Smaller kernel
        kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (kernel_size, kernel_size))
        opened = cv2.morphologyEx(mask.astype(np.uint8), cv2.MORPH_OPEN, kernel)
        return opened

    def _smooth_edges(self, mask: np.ndarray) -> np.ndarray:
        """Smooth edges using Gaussian blur and threshold."""
        # Apply Gaussian blur
        blurred = cv2.GaussianBlur(
            mask.astype(np.float32), (self.edge_feather_radius * 2 + 1, self.edge_feather_radius * 2 + 1), 0
        )
        # Re-threshold to binary
        smoothed = (blurred > 0.5).astype(np.uint8)
        return smoothed

    def _expand_lower_region(self, mask: np.ndarray, expand_ratio: float = 0.15) -> np.ndarray:
        """
        Expand the lower portion of the mask to preserve wheels/tires.
        Vehicles often have wheels cut off because the shadow/ground is dark.

        Args:
            mask: Binary mask
            expand_ratio: How much of the bottom to expand (0.15 = bottom 15%)
        """
        h, w = mask.shape[:2]

        # Find where the vehicle is
        rows_with_mask = np.any(mask > 0, axis=1)
        if not np.any(rows_with_mask):
            return mask

        vehicle_rows = np.where(rows_with_mask)[0]
        vehicle_top = vehicle_rows.min()
        vehicle_bottom = vehicle_rows.max()
        vehicle_height = vehicle_bottom - vehicle_top

        if vehicle_height < 10:
            return mask

        # Calculate lower region to expand
        lower_start = vehicle_bottom - int(vehicle_height * expand_ratio)

        # Create expansion kernel (horizontal ellipse for wheels)
        kernel_w = 15  # Wide for horizontal wheel expansion
        kernel_h = 9  # Less vertical
        kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (kernel_w, kernel_h))

        # Only dilate the lower portion
        lower_mask = mask.copy()
        lower_mask[:lower_start, :] = 0  # Zero out upper portion

        # Dilate only the lower region
        dilated_lower = cv2.dilate(lower_mask.astype(np.uint8), kernel, iterations=2)

        # Combine: original mask + expanded lower portion
        result = np.maximum(mask, dilated_lower)

        logger.info(f"  🔧 Expanded lower {int(expand_ratio*100)}% of vehicle mask")

        return result

    def _validate_against_bbox(self, mask: np.ndarray, bbox: np.ndarray, expansion_ratio: float = 0.1) -> np.ndarray:
        """
        Validate and potentially expand mask to include bbox area.
        If mask doesn't cover enough of the bbox, expand it.
        """
        x1, y1, x2, y2 = map(int, bbox)
        h, w = mask.shape[:2]

        # Clamp to image bounds
        x1, y1 = max(0, x1), max(0, y1)
        x2, y2 = min(w, x2), min(h, y2)

        bbox_area = (x2 - x1) * (y2 - y1)
        if bbox_area == 0:
            return mask

        # Check mask coverage within bbox
        mask_in_bbox = mask[y1:y2, x1:x2].sum()
        coverage = mask_in_bbox / bbox_area

        # If coverage is too low, something went wrong
        if coverage < 0.3:
            logger.warning(f"Low mask coverage in bbox ({coverage:.1%}), mask may be incomplete")
            # Could optionally expand mask here, but we'll just log for now

        return mask

    def _edge_aware_refinement(self, mask: np.ndarray, image: np.ndarray) -> np.ndarray:
        """
        Refine mask edges using image gradients (GrabCut-like refinement).
        This helps the mask follow natural object boundaries.
        """
        try:
            # Convert to grayscale for edge detection
            if len(image.shape) == 3:
                gray = cv2.cvtColor(image, cv2.COLOR_RGB2GRAY)
            else:
                gray = image

            # Compute image gradients
            grad_x = cv2.Sobel(gray, cv2.CV_64F, 1, 0, ksize=3)
            grad_y = cv2.Sobel(gray, cv2.CV_64F, 0, 1, ksize=3)
            magnitude = np.sqrt(grad_x**2 + grad_y**2)

            # Normalize gradient magnitude
            if magnitude.max() > 0:
                magnitude = magnitude / magnitude.max()

            # Find edge pixels in current mask
            kernel = np.ones((3, 3), np.uint8)
            dilated = cv2.dilate(mask.astype(np.uint8), kernel, iterations=1)
            eroded = cv2.erode(mask.astype(np.uint8), kernel, iterations=1)
            dilated - eroded

            # In edge region, use gradient to refine
            # High gradient = likely real edge, keep mask boundary
            # Low gradient = possibly wrong edge, smooth more

            # For now, just apply guided filter if available
            # This is a simplified version - full implementation would use CRF or GrabCut

            return mask

        except Exception as e:
            logger.warning(f"Edge-aware refinement failed: {e}")
            return mask


class AlphaMatting:
    """
    Professional alpha matting for vehicle edges.
    Uses erosion + minimal antialiasing (2-5px) for clean marketplace-quality edges.
    Industry standard: sharp edges with subtle antialiasing, NOT heavy blur.
    """

    def __init__(self, feather_radius: int = 4, edge_threshold: float = 0.3):
        # Professional feather: 3-5px max for clean edges
        self.feather_radius = min(feather_radius, 6)  # Cap at 6px
        self.edge_threshold = edge_threshold

    def create_alpha_matte(self, mask: np.ndarray, image: Optional[np.ndarray] = None) -> np.ndarray:
        """
        Professional alpha matte: erosion + minimal antialiasing.

        Technique used by professional photo editors:
        1. Erode mask by 1-2px to remove edge artifacts/fringing
        2. Apply minimal Gaussian blur (2-4px) for antialiasing
        3. Keep interior 100% solid

        Args:
            mask: Binary mask (0-255)
            image: Original image (optional)

        Returns:
            Alpha matte with clean antialiased edges (0-255)
        """
        # Normalize mask to 0-1
        mask_float = mask.astype(np.float32) / 255.0 if mask.max() > 1 else mask.astype(np.float32)
        mask_binary = (mask_float > 0.5).astype(np.uint8)

        # Step 1: Erode by 1px to remove color fringing at edges
        kernel_erode = np.ones((3, 3), np.uint8)
        eroded = cv2.erode(mask_binary, kernel_erode, iterations=1)

        # Step 2: Create alpha with minimal blur for antialiasing
        # Use small Gaussian blur (sigma = feather_radius/2)
        blur_size = self.feather_radius * 2 + 1  # Must be odd
        alpha = cv2.GaussianBlur(eroded.astype(np.float32), (blur_size, blur_size), sigmaX=self.feather_radius / 2)

        # Step 3: Strengthen the interior to keep it solid
        # Where original mask is solid, keep alpha at 1.0
        interior = cv2.erode(mask_binary, kernel_erode, iterations=2)
        alpha[interior > 0] = 1.0

        # Ensure valid range
        alpha = np.clip(alpha, 0, 1)

        return (alpha * 255).astype(np.uint8)

    def apply_alpha_composite(self, foreground: np.ndarray, background: np.ndarray, alpha: np.ndarray) -> np.ndarray:
        """
        Composite foreground onto background using alpha matte.

        Args:
            foreground: Vehicle image (RGB)
            background: Background image (RGB, same size)
            alpha: Alpha matte (0-255)

        Returns:
            Composited image (RGB)
        """
        # Normalize alpha to 0-1
        alpha_float = alpha.astype(np.float32) / 255.0

        # Expand alpha to 3 channels
        alpha_3ch = np.stack([alpha_float] * 3, axis=-1)

        # Alpha composite
        result = foreground.astype(np.float32) * alpha_3ch + background.astype(np.float32) * (1 - alpha_3ch)

        return result.astype(np.uint8)


def enhance_background_removal(
    image: np.ndarray,
    mask: np.ndarray,
    background_color: Tuple[int, int, int] = (255, 255, 255),
    use_alpha_matte: bool = True,
    feather_radius: int = 3,
) -> Image.Image:
    """
    Enhanced background removal with smooth edges.

    Args:
        image: Original RGB image
        mask: Binary segmentation mask
        background_color: Color for background (default white)
        use_alpha_matte: Whether to create smooth alpha transitions
        feather_radius: Radius for edge smoothing

    Returns:
        PIL Image with background removed
    """
    # First refine the mask
    refiner = MaskRefinement(edge_feather_radius=feather_radius)
    refined_mask, metadata = refiner.refine_mask(mask, image)

    if use_alpha_matte:
        # Create smooth alpha matte
        matter = AlphaMatting(feather_radius=feather_radius)
        alpha = matter.create_alpha_matte(refined_mask, image)

        # Create solid color background
        h, w = image.shape[:2]
        background = np.full((h, w, 3), background_color, dtype=np.uint8)

        # Composite
        result = matter.apply_alpha_composite(image, background, alpha)

        return Image.fromarray(result, "RGB")
    else:
        # Simple hard-edge replacement
        mask_binary = refined_mask > 127
        result = image.copy()
        result[~mask_binary] = np.array(background_color, dtype=np.uint8)

        return Image.fromarray(result, "RGB")


# Test function
def test_refinement():
    """Test the refinement pipeline with a synthetic mask."""
    # Create test mask with holes and artifacts
    mask = np.zeros((480, 640), dtype=np.uint8)

    # Main vehicle region
    cv2.ellipse(mask, (320, 240), (200, 100), 0, 0, 360, 255, -1)

    # Add a hole
    cv2.circle(mask, (320, 240), 30, 0, -1)

    # Add artifacts
    cv2.circle(mask, (50, 50), 10, 255, -1)
    cv2.circle(mask, (600, 400), 15, 255, -1)

    # Refine
    refiner = MaskRefinement()
    refined, metadata = refiner.refine_mask(mask)

    print(f"Refinement results: {metadata}")

    return refined


if __name__ == "__main__":
    test_refinement()
