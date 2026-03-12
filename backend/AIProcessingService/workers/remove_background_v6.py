#!/usr/bin/env python3
"""
Remove Background V6 - Multi-Model with Object Isolation
=========================================================
Fixes V5 issue: removes stray objects not connected to the vehicle.

Strategy:
1. Multi-model voting (2 of 3 must agree = foreground)
2. Keep only the largest connected component (the vehicle)
3. Remove isolated islands and artifacts

Usage:
    python remove_background_v6.py
"""

import sys
import time
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter
from scipy.ndimage import binary_dilation, binary_erosion, binary_fill_holes, label

# Directories
INPUT_DIR = Path("./input")
OUTPUT_DIR = Path("./output_v6")


def setup_directories():
    """Create output directories."""
    OUTPUT_DIR.mkdir(exist_ok=True)
    (OUTPUT_DIR / "transparent").mkdir(exist_ok=True)
    (OUTPUT_DIR / "white_bg").mkdir(exist_ok=True)
    (OUTPUT_DIR / "shadow").mkdir(exist_ok=True)


def get_masks_from_models(image_path: Path) -> list:
    """
    Get masks from multiple models.
    Returns list of binary masks (numpy arrays).
    """
    from io import BytesIO

    from rembg import new_session, remove

    masks = []

    # Model 1: BiRefNet (good edges)
    print("    • BiRefNet...")
    try:
        session1 = new_session("birefnet-general")
        with open(image_path, "rb") as f:
            result1 = remove(f.read(), session=session1)
        img1 = Image.open(BytesIO(result1)).convert("RGBA")
        masks.append(np.array(img1.split()[3]) > 128)
    except Exception as e:
        print(f"      ⚠️ BiRefNet failed: {e}")

    # Model 2: U2Net (good detection)
    print("    • U2Net...")
    try:
        session2 = new_session("u2net")
        with open(image_path, "rb") as f:
            result2 = remove(f.read(), session=session2)
        img2 = Image.open(BytesIO(result2)).convert("RGBA")
        masks.append(np.array(img2.split()[3]) > 128)
    except Exception as e:
        print(f"      ⚠️ U2Net failed: {e}")

    # Model 3: ISNet (general objects)
    print("    • ISNet...")
    try:
        session3 = new_session("isnet-general-use")
        with open(image_path, "rb") as f:
            result3 = remove(f.read(), session=session3)
        img3 = Image.open(BytesIO(result3)).convert("RGBA")
        masks.append(np.array(img3.split()[3]) > 128)
    except Exception as e:
        print(f"      ⚠️ ISNet failed: {e}")

    return masks


def majority_voting(masks: list) -> np.ndarray:
    """
    Combine masks using majority voting.
    A pixel is foreground only if majority of models agree.
    """
    if len(masks) == 0:
        raise ValueError("No masks to combine")

    # Stack masks and count votes
    stacked = np.stack(masks, axis=0)
    vote_count = np.sum(stacked, axis=0)

    # Majority = more than half
    threshold = len(masks) / 2
    combined = vote_count > threshold

    return combined


def keep_largest_component(mask: np.ndarray) -> np.ndarray:
    """
    Keep only the largest connected component (the vehicle).
    This removes stray objects not connected to the main subject.
    """
    print("    • Keeping largest component...")

    # Label connected components
    labeled, num_features = label(mask)

    if num_features == 0:
        return mask

    if num_features == 1:
        return mask

    # Find the largest component
    component_sizes = []
    for i in range(1, num_features + 1):
        size = np.sum(labeled == i)
        component_sizes.append((i, size))

    # Sort by size (largest first)
    component_sizes.sort(key=lambda x: x[1], reverse=True)

    # Keep only the largest
    largest_label = component_sizes[0][0]
    result = labeled == largest_label

    # Report what was removed
    removed_count = num_features - 1
    if removed_count > 0:
        removed_pixels = sum(size for label_id, size in component_sizes[1:])
        print(f"      ✂️ Removed {removed_count} stray objects ({removed_pixels:,} pixels)")

    return result


def remove_small_islands(mask: np.ndarray, min_size: int = 500) -> np.ndarray:
    """
    Remove small isolated regions that are likely artifacts.
    """
    labeled, num_features = label(mask)

    result = np.zeros_like(mask)
    removed = 0

    for i in range(1, num_features + 1):
        component = labeled == i
        size = np.sum(component)

        if size >= min_size:
            result = result | component
        else:
            removed += 1

    if removed > 0:
        print(f"      ✂️ Removed {removed} small islands (< {min_size} pixels)")

    return result


def fill_holes_in_vehicle(mask: np.ndarray) -> np.ndarray:
    """
    Fill holes inside the vehicle (windows, wheels, etc should stay solid).
    """
    return binary_fill_holes(mask)


def smooth_edges(mask: np.ndarray) -> np.ndarray:
    """
    Smooth the edges of the mask for cleaner cutout.
    """
    # Slight dilation then erosion to smooth jagged edges
    mask = binary_dilation(mask, iterations=1)
    mask = binary_erosion(mask, iterations=1)
    return mask


def create_soft_alpha(mask: np.ndarray) -> np.ndarray:
    """
    Create a soft alpha channel with anti-aliased edges.
    """
    from scipy.ndimage import distance_transform_edt, gaussian_filter

    # Convert to float
    alpha = mask.astype(np.float32) * 255

    # Create distance from edge
    dist_inside = distance_transform_edt(mask)
    dist_outside = distance_transform_edt(~mask)

    # Create gradient at edges (within 2 pixels of edge)
    edge_width = 2
    edge_inside = (dist_inside > 0) & (dist_inside <= edge_width)
    edge_outside = (dist_outside > 0) & (dist_outside <= edge_width)

    # Soften edges
    alpha[edge_inside] = 200 + (dist_inside[edge_inside] / edge_width) * 55
    alpha[edge_outside] = (1 - dist_outside[edge_outside] / edge_width) * 100

    # Apply slight blur for anti-aliasing
    alpha = gaussian_filter(alpha, sigma=0.5)

    return np.clip(alpha, 0, 255).astype(np.uint8)


def remove_bg_v6(image_path: Path) -> Image.Image:
    """
    Main background removal with V6 improvements.
    """
    print("  → Getting masks from 3 models...")
    masks = get_masks_from_models(image_path)

    if len(masks) == 0:
        raise ValueError("All models failed")

    print(f"  → Combining with majority voting ({len(masks)} models)...")
    combined = majority_voting(masks)

    print("  → Isolating vehicle...")
    # Remove small islands first
    combined = remove_small_islands(combined, min_size=500)

    # Keep only largest connected component (the vehicle)
    combined = keep_largest_component(combined)

    # Fill any holes
    combined = fill_holes_in_vehicle(combined)

    # Smooth edges
    combined = smooth_edges(combined)

    print("  → Creating soft alpha edges...")
    alpha = create_soft_alpha(combined)

    # Apply to original image
    original = Image.open(image_path).convert("RGBA")
    original.putalpha(Image.fromarray(alpha))

    return original


def add_professional_shadow(image: Image.Image) -> Image.Image:
    """Add a subtle contact shadow."""
    width, height = image.size
    padding = 50
    new_width = width + padding * 2
    new_height = height + padding * 2

    result = Image.new("RGBA", (new_width, new_height), (255, 255, 255, 255))

    alpha = np.array(image.split()[3])

    rows_with_content = np.where(alpha.max(axis=1) > 128)[0]
    if len(rows_with_content) > 0:
        bottom_y = rows_with_content[-1]
    else:
        bottom_y = height - 1

    shadow_height = 30
    shadow_array = np.zeros((height, width), dtype=np.uint8)

    for y in range(max(0, bottom_y - shadow_height), min(height, bottom_y + 5)):
        cols_with_content = np.where(alpha[y] > 128)[0]
        if len(cols_with_content) > 0:
            left_x = cols_with_content[0]
            right_x = cols_with_content[-1]

            dist_from_bottom = abs(y - bottom_y)
            intensity = max(0, 60 - dist_from_bottom * 3)

            for x in range(left_x, right_x + 1):
                edge_dist = min(x - left_x, right_x - x)
                edge_falloff = min(1.0, edge_dist / 20)
                shadow_array[y, x] = int(intensity * edge_falloff)

    shadow = Image.fromarray(shadow_array)
    shadow = shadow.filter(ImageFilter.GaussianBlur(radius=8))

    result.paste(Image.new("RGB", (width, height), (0, 0, 0)), (padding, padding + 3), shadow)
    result.paste(image, (padding, padding), image)

    return result


def create_white_background(image: Image.Image) -> Image.Image:
    white_bg = Image.new("RGBA", image.size, (255, 255, 255, 255))
    white_bg.paste(image, (0, 0), image)
    return white_bg.convert("RGB")


def process_image(image_path: Path) -> dict:
    """Process a single image."""
    results = {}

    try:
        result = remove_bg_v6(image_path)

        # Save outputs
        stem = image_path.stem

        transparent_path = OUTPUT_DIR / "transparent" / f"{stem}_transparent.png"
        result.save(transparent_path, "PNG", optimize=True)
        results["transparent"] = transparent_path

        white_bg = create_white_background(result)
        white_path = OUTPUT_DIR / "white_bg" / f"{stem}_white.png"
        white_bg.save(white_path, "PNG", optimize=True)
        results["white_bg"] = white_path

        print("  → Adding shadow...")
        shadow_version = add_professional_shadow(result)
        shadow_path = OUTPUT_DIR / "shadow" / f"{stem}_shadow.png"
        shadow_version.save(shadow_path, "PNG", optimize=True)
        results["shadow"] = shadow_path

        results["success"] = True

    except Exception as e:
        results["success"] = False
        results["error"] = str(e)
        import traceback

        traceback.print_exc()

    return results


def main():
    print("=" * 60)
    print("🚗 Professional Background Removal V6")
    print("   Multi-Model + Vehicle Isolation")
    print("=" * 60)
    print(f"Input: {INPUT_DIR}")
    print(f"Output: {OUTPUT_DIR}")
    print()
    print("Improvements over V5:")
    print("  ✓ Majority voting (2/3 models must agree)")
    print("  ✓ Keep only largest object (the vehicle)")
    print("  ✓ Remove stray background objects")
    print("  ✓ Soft anti-aliased edges")
    print("=" * 60)

    setup_directories()

    extensions = ["*.jpg", "*.jpeg", "*.png", "*.webp", "*.bmp"]
    images = []
    for ext in extensions:
        images.extend(INPUT_DIR.glob(ext))

    if not images:
        print(f"\n❌ No images found in {INPUT_DIR}")
        sys.exit(1)

    print(f"\n📁 Found {len(images)} images to process\n")

    start_time = time.time()
    successful = 0
    failed = 0

    for i, image_path in enumerate(sorted(images), 1):
        print(f"[{i}/{len(images)}] Processing: {image_path.name}")
        img_start = time.time()

        results = process_image(image_path)

        img_time = time.time() - img_start

        if results["success"]:
            print(f"  ✅ Done in {img_time:.1f}s\n")
            successful += 1
        else:
            print(f"  ❌ Failed: {results.get('error', 'Unknown error')}\n")
            failed += 1

    total_time = time.time() - start_time
    print("=" * 60)
    print("📊 SUMMARY")
    print("=" * 60)
    print(f"✅ Successful: {successful}")
    print(f"❌ Failed: {failed}")
    print(f"⏱️  Total time: {total_time:.1f}s")
    print(f"⏱️  Average: {total_time/len(images):.1f}s per image")
    print(f"\n📂 Output: {OUTPUT_DIR}/")
    print("=" * 60)


if __name__ == "__main__":
    main()
