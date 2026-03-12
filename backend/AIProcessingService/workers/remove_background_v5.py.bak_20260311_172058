#!/usr/bin/env python3
"""
Remove Background V5 - SAM 2 (Segment Anything Model 2)
========================================================
Uses Meta's SAM 2 - the most accurate open-source segmentation model.

Usage:
    python remove_background_v5.py
"""

import os
import sys
import time
from pathlib import Path
from PIL import Image, ImageFilter, ImageEnhance
import numpy as np

# Directories
INPUT_DIR = Path("./input")
OUTPUT_DIR = Path("./output_v5")

def setup_directories():
    """Create output directories."""
    OUTPUT_DIR.mkdir(exist_ok=True)
    (OUTPUT_DIR / "transparent").mkdir(exist_ok=True)
    (OUTPUT_DIR / "white_bg").mkdir(exist_ok=True)
    (OUTPUT_DIR / "shadow").mkdir(exist_ok=True)

def install_sam2():
    """Install SAM 2 dependencies."""
    print("📦 Installing SAM 2 dependencies...")
    os.system(f"{sys.executable} -m pip install torch torchvision --quiet")
    os.system(f"{sys.executable} -m pip install git+https://github.com/facebookresearch/segment-anything-2.git --quiet")

def remove_bg_sam2(image_path: Path) -> Image.Image:
    """
    Use SAM 2 for high-quality segmentation.
    """
    print(f"  → Loading SAM 2...")
    
    try:
        import torch
        from sam2.build_sam import build_sam2
        from sam2.sam2_image_predictor import SAM2ImagePredictor
    except ImportError:
        install_sam2()
        import torch
        from sam2.build_sam import build_sam2
        from sam2.sam2_image_predictor import SAM2ImagePredictor
    
    # Device
    device = "mps" if torch.backends.mps.is_available() else "cuda" if torch.cuda.is_available() else "cpu"
    print(f"  → Device: {device}")
    
    # Load model
    sam2_checkpoint = Path.home() / ".cache" / "sam2" / "sam2_hiera_large.pt"
    model_cfg = "sam2_hiera_l.yaml"
    
    if not sam2_checkpoint.exists():
        print(f"  → Downloading SAM 2 model (~2.4GB)...")
        sam2_checkpoint.parent.mkdir(parents=True, exist_ok=True)
        import urllib.request
        urllib.request.urlretrieve(
            "https://dl.fbaipublicfiles.com/segment_anything_2/072824/sam2_hiera_large.pt",
            sam2_checkpoint
        )
    
    sam2_model = build_sam2(model_cfg, sam2_checkpoint, device=device)
    predictor = SAM2ImagePredictor(sam2_model)
    
    # Load image
    image = Image.open(image_path).convert('RGB')
    image_np = np.array(image)
    
    predictor.set_image(image_np)
    
    # Auto-detect vehicle using center point
    h, w = image_np.shape[:2]
    # Use multiple points to capture the whole vehicle
    points = np.array([
        [w // 2, h // 2],      # center
        [w // 4, h // 2],      # left
        [3 * w // 4, h // 2],  # right
        [w // 2, h // 3],      # top
        [w // 2, 2 * h // 3],  # bottom
    ])
    labels = np.array([1, 1, 1, 1, 1])  # all foreground
    
    masks, scores, _ = predictor.predict(
        point_coords=points,
        point_labels=labels,
        multimask_output=True
    )
    
    # Use highest scoring mask
    best_idx = np.argmax(scores)
    mask = masks[best_idx]
    
    # Convert to alpha
    alpha = (mask * 255).astype(np.uint8)
    alpha_pil = Image.fromarray(alpha)
    
    # Apply to image
    result = image.convert('RGBA')
    result.putalpha(alpha_pil)
    
    return result

def remove_bg_rembg_dis(image_path: Path) -> Image.Image:
    """
    Use rembg with DIS (Dichotomous Image Segmentation) model.
    This is more accurate than BiRefNet for vehicles.
    """
    print(f"  → Using DIS model (high accuracy)...")
    
    from rembg import remove, new_session
    
    # Try DIS model first (better for vehicles)
    try:
        session = new_session("isnet-general-use")
    except:
        session = new_session("birefnet-general")
    
    with open(image_path, 'rb') as f:
        input_data = f.read()
    
    output_data = remove(
        input_data,
        session=session,
        alpha_matting=True,
        alpha_matting_foreground_threshold=250,  # More aggressive foreground
        alpha_matting_background_threshold=5,    # More aggressive background removal
        alpha_matting_erode_size=5,              # Less erosion to preserve edges
    )
    
    from io import BytesIO
    return Image.open(BytesIO(output_data)).convert('RGBA')

def remove_bg_combined(image_path: Path) -> Image.Image:
    """
    Combined approach: Use multiple models and merge masks.
    This gives the best results by combining strengths of each model.
    """
    print(f"  → Using combined multi-model approach...")
    
    from rembg import remove, new_session
    from io import BytesIO
    
    # Get masks from multiple models
    masks = []
    
    # Model 1: BiRefNet (good edges)
    print(f"    • BiRefNet pass...")
    session1 = new_session("birefnet-general")
    with open(image_path, 'rb') as f:
        data1 = f.read()
    result1 = remove(data1, session=session1)
    img1 = Image.open(BytesIO(result1)).convert('RGBA')
    masks.append(np.array(img1.split()[3]))
    
    # Model 2: U2Net (good detection)
    print(f"    • U2Net pass...")
    session2 = new_session("u2net")
    with open(image_path, 'rb') as f:
        data2 = f.read()
    result2 = remove(data2, session=session2)
    img2 = Image.open(BytesIO(result2)).convert('RGBA')
    masks.append(np.array(img2.split()[3]))
    
    # Model 3: ISNet (good for general objects)
    print(f"    • ISNet pass...")
    try:
        session3 = new_session("isnet-general-use")
        with open(image_path, 'rb') as f:
            data3 = f.read()
        result3 = remove(data3, session=session3)
        img3 = Image.open(BytesIO(result3)).convert('RGBA')
        masks.append(np.array(img3.split()[3]))
    except:
        pass  # Skip if not available
    
    # Combine masks (union - if ANY model says foreground, keep it)
    print(f"    • Combining masks...")
    combined_mask = np.zeros_like(masks[0], dtype=np.float32)
    for m in masks:
        combined_mask = np.maximum(combined_mask, m.astype(np.float32))
    
    # Smooth the combined mask
    combined_pil = Image.fromarray(combined_mask.astype(np.uint8))
    combined_pil = combined_pil.filter(ImageFilter.GaussianBlur(radius=0.5))
    
    # Apply to original image
    original = Image.open(image_path).convert('RGBA')
    original.putalpha(combined_pil)
    
    return original

def refine_edges_pro(image: Image.Image) -> Image.Image:
    """
    Professional edge refinement with multiple passes.
    """
    print(f"  → Professional edge refinement...")
    
    from scipy.ndimage import gaussian_filter, binary_fill_holes, binary_dilation
    
    r, g, b, a = image.split()
    alpha = np.array(a).astype(np.float32)
    
    # 1. Fill any holes in the mask
    binary_mask = alpha > 128
    binary_mask = binary_fill_holes(binary_mask)
    
    # 2. Slight dilation to recover cut edges
    binary_mask = binary_dilation(binary_mask, iterations=1)
    
    # 3. Create smooth gradient at edges
    alpha_smooth = gaussian_filter(binary_mask.astype(np.float32) * 255, sigma=0.8)
    
    # 4. Blend original alpha with smoothed version at edges only
    edge_region = (alpha > 10) & (alpha < 245)
    final_alpha = alpha.copy()
    final_alpha[edge_region] = alpha_smooth[edge_region]
    
    # 5. Ensure solid core (no semi-transparent vehicle)
    core_region = alpha > 200
    final_alpha[core_region] = 255
    
    # Apply
    result = image.copy()
    result.putalpha(Image.fromarray(np.clip(final_alpha, 0, 255).astype(np.uint8)))
    
    return result

def add_professional_shadow(image: Image.Image) -> Image.Image:
    """
    Add a subtle contact shadow like remove.bg
    """
    print(f"  → Adding professional shadow...")
    
    width, height = image.size
    padding = 50
    new_width = width + padding * 2
    new_height = height + padding * 2
    
    result = Image.new('RGBA', (new_width, new_height), (255, 255, 255, 255))
    
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
    
    result.paste(Image.new('RGB', (width, height), (0, 0, 0)), 
                 (padding, padding + 3), shadow)
    result.paste(image, (padding, padding), image)
    
    return result

def create_white_background(image: Image.Image) -> Image.Image:
    white_bg = Image.new('RGBA', image.size, (255, 255, 255, 255))
    white_bg.paste(image, (0, 0), image)
    return white_bg.convert('RGB')

def process_image(image_path: Path, use_combined: bool = True) -> dict:
    """Process a single image."""
    results = {}
    
    try:
        # Remove background with combined approach
        if use_combined:
            result = remove_bg_combined(image_path)
        else:
            result = remove_bg_rembg_dis(image_path)
        
        # Refine edges
        result = refine_edges_pro(result)
        
        # Save outputs
        stem = image_path.stem
        
        transparent_path = OUTPUT_DIR / "transparent" / f"{stem}_transparent.png"
        result.save(transparent_path, 'PNG', optimize=True)
        results['transparent'] = transparent_path
        
        white_bg = create_white_background(result)
        white_path = OUTPUT_DIR / "white_bg" / f"{stem}_white.png"
        white_bg.save(white_path, 'PNG', optimize=True)
        results['white_bg'] = white_path
        
        shadow_version = add_professional_shadow(result)
        shadow_path = OUTPUT_DIR / "shadow" / f"{stem}_shadow.png"
        shadow_version.save(shadow_path, 'PNG', optimize=True)
        results['shadow'] = shadow_path
        
        results['success'] = True
        
    except Exception as e:
        results['success'] = False
        results['error'] = str(e)
        import traceback
        traceback.print_exc()
    
    return results

def main():
    print("=" * 60)
    print("🚗 Professional Background Removal V5")
    print("   Multi-Model Combined Approach")
    print("=" * 60)
    print(f"Input: {INPUT_DIR}")
    print(f"Output: {OUTPUT_DIR}")
    print("=" * 60)
    
    setup_directories()
    
    extensions = ['*.jpg', '*.jpeg', '*.png', '*.webp', '*.bmp']
    images = []
    for ext in extensions:
        images.extend(INPUT_DIR.glob(ext))
    
    if not images:
        print(f"\n❌ No images found in {INPUT_DIR}")
        sys.exit(1)
    
    print(f"\n📁 Found {len(images)} images to process")
    print("   Using: BiRefNet + U2Net + ISNet (combined)")
    print()
    
    start_time = time.time()
    successful = 0
    failed = 0
    
    for i, image_path in enumerate(sorted(images), 1):
        print(f"[{i}/{len(images)}] Processing: {image_path.name}")
        img_start = time.time()
        
        results = process_image(image_path)
        
        img_time = time.time() - img_start
        
        if results['success']:
            print(f"  ✅ Done in {img_time:.1f}s")
            successful += 1
        else:
            print(f"  ❌ Failed: {results.get('error', 'Unknown error')}")
            failed += 1
        print()
    
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
