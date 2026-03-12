#!/usr/bin/env python3
"""
Remove Background V4 - Professional Quality
============================================
Two modes:
1. remove.bg API (identical results to their service)
2. BRIA RMBG 2.0 (free, local, very high quality)

Usage:
    python remove_background_v4.py --mode api --api-key YOUR_KEY
    python remove_background_v4.py --mode local
"""

import os
import sys
import time
import argparse
import requests
from pathlib import Path
from PIL import Image, ImageFilter, ImageEnhance
import numpy as np

# Directories
INPUT_DIR = Path("./input")
OUTPUT_DIR = Path("./output_v4")

def setup_directories():
    """Create output directories."""
    OUTPUT_DIR.mkdir(exist_ok=True)
    (OUTPUT_DIR / "transparent").mkdir(exist_ok=True)
    (OUTPUT_DIR / "white_bg").mkdir(exist_ok=True)
    (OUTPUT_DIR / "shadow").mkdir(exist_ok=True)

def remove_bg_api(image_path: Path, api_key: str) -> Image.Image:
    """
    Use remove.bg API for professional results.
    Free tier: 50 images/month
    Get API key at: https://www.remove.bg/api
    """
    print(f"  → Using remove.bg API...")
    
    with open(image_path, 'rb') as f:
        response = requests.post(
            'https://api.remove.bg/v1.0/removebg',
            files={'image_file': f},
            data={
                'size': 'full',  # full resolution
                'type': 'car',   # optimized for vehicles
                'format': 'png',
                'crop': 'false',
                'add_shadow': 'false',  # we'll add our own
            },
            headers={'X-Api-Key': api_key},
            timeout=60
        )
    
    if response.status_code == 200:
        # Convert response to PIL Image
        from io import BytesIO
        return Image.open(BytesIO(response.content)).convert('RGBA')
    else:
        error = response.json().get('errors', [{'title': 'Unknown error'}])
        raise Exception(f"remove.bg API error: {error[0].get('title', 'Unknown')}")

def remove_bg_local(image_path: Path) -> Image.Image:
    """
    Use BRIA RMBG 2.0 for high-quality local processing.
    This model is specifically trained for professional background removal.
    """
    print(f"  → Using BRIA RMBG 2.0 (local)...")
    
    try:
        from transformers import AutoModelForImageSegmentation
        import torch
        from torchvision import transforms
    except ImportError:
        print("\n⚠️  Installing required packages for BRIA RMBG 2.0...")
        os.system(f"{sys.executable} -m pip install transformers torch torchvision --quiet")
        from transformers import AutoModelForImageSegmentation
        import torch
        from torchvision import transforms
    
    # Load model (downloads ~176MB on first run)
    device = "mps" if torch.backends.mps.is_available() else "cuda" if torch.cuda.is_available() else "cpu"
    print(f"  → Device: {device}")
    
    model = AutoModelForImageSegmentation.from_pretrained(
        "briaai/RMBG-2.0",
        trust_remote_code=True
    )
    model.to(device)
    model.eval()
    
    # Prepare image
    image = Image.open(image_path).convert('RGB')
    original_size = image.size
    
    # Transform for model
    transform = transforms.Compose([
        transforms.Resize((1024, 1024)),
        transforms.ToTensor(),
        transforms.Normalize([0.485, 0.456, 0.406], [0.229, 0.224, 0.225])
    ])
    
    input_tensor = transform(image).unsqueeze(0).to(device)
    
    # Get mask
    with torch.no_grad():
        output = model(input_tensor)
    
    # Process output - handle different output formats
    if isinstance(output, tuple):
        mask = output[0]
    elif isinstance(output, dict):
        mask = output.get('masks', output.get('pred', list(output.values())[0]))
    else:
        mask = output
    
    # Convert to numpy
    if hasattr(mask, 'sigmoid'):
        mask = mask.sigmoid()
    mask = mask.squeeze().cpu().numpy()
    
    # Resize mask to original size
    mask_image = Image.fromarray((mask * 255).astype(np.uint8))
    mask_image = mask_image.resize(original_size, Image.LANCZOS)
    
    # Apply mask to original image
    result = Image.open(image_path).convert('RGBA')
    result.putalpha(mask_image)
    
    return result

def remove_bg_rembg_birefnet(image_path: Path) -> Image.Image:
    """
    Fallback to rembg with BiRefNet if BRIA fails.
    """
    print(f"  → Using rembg BiRefNet (fallback)...")
    
    from rembg import remove, new_session
    
    session = new_session("birefnet-general")
    
    with open(image_path, 'rb') as f:
        input_data = f.read()
    
    output_data = remove(
        input_data,
        session=session,
        alpha_matting=True,
        alpha_matting_foreground_threshold=240,
        alpha_matting_background_threshold=10,
        alpha_matting_erode_size=10
    )
    
    from io import BytesIO
    return Image.open(BytesIO(output_data)).convert('RGBA')

def refine_edges(image: Image.Image) -> Image.Image:
    """
    Professional edge refinement similar to remove.bg
    """
    print(f"  → Refining edges...")
    
    # Get alpha channel
    r, g, b, a = image.split()
    alpha = np.array(a)
    
    # 1. Smooth the edges with Gaussian blur
    alpha_pil = Image.fromarray(alpha)
    alpha_smooth = alpha_pil.filter(ImageFilter.GaussianBlur(radius=0.5))
    alpha = np.array(alpha_smooth)
    
    # 2. Increase contrast to sharpen edges while keeping smoothness
    alpha_pil = Image.fromarray(alpha)
    enhancer = ImageEnhance.Contrast(alpha_pil)
    alpha_pil = enhancer.enhance(1.2)
    alpha = np.array(alpha_pil)
    
    # 3. Feather edges slightly
    from scipy.ndimage import gaussian_filter
    edge_mask = np.logical_and(alpha > 10, alpha < 245)
    alpha_float = alpha.astype(float)
    alpha_float[edge_mask] = gaussian_filter(alpha_float, sigma=0.8)[edge_mask]
    alpha = np.clip(alpha_float, 0, 255).astype(np.uint8)
    
    # Apply refined alpha
    result = image.copy()
    result.putalpha(Image.fromarray(alpha))
    
    return result

def add_professional_shadow(image: Image.Image) -> Image.Image:
    """
    Add a subtle contact shadow like remove.bg
    """
    print(f"  → Adding professional shadow...")
    
    # Get dimensions
    width, height = image.size
    
    # Create larger canvas for shadow
    padding = 50
    new_width = width + padding * 2
    new_height = height + padding * 2
    
    # Create white background
    result = Image.new('RGBA', (new_width, new_height), (255, 255, 255, 255))
    
    # Get alpha channel for shadow base
    alpha = np.array(image.split()[3])
    
    # Find bottom of vehicle for shadow placement
    rows_with_content = np.where(alpha.max(axis=1) > 128)[0]
    if len(rows_with_content) > 0:
        bottom_y = rows_with_content[-1]
    else:
        bottom_y = height - 1
    
    # Create shadow mask (only at the bottom)
    shadow_height = 30
    shadow = Image.new('L', (width, height), 0)
    shadow_array = np.zeros((height, width), dtype=np.uint8)
    
    # Create gradient shadow at bottom
    for y in range(max(0, bottom_y - shadow_height), min(height, bottom_y + 5)):
        cols_with_content = np.where(alpha[y] > 128)[0]
        if len(cols_with_content) > 0:
            left_x = cols_with_content[0]
            right_x = cols_with_content[-1]
            
            # Shadow intensity based on distance from bottom
            dist_from_bottom = abs(y - bottom_y)
            intensity = max(0, 60 - dist_from_bottom * 3)
            
            # Add shadow with horizontal falloff
            for x in range(left_x, right_x + 1):
                edge_dist = min(x - left_x, right_x - x)
                edge_falloff = min(1.0, edge_dist / 20)
                shadow_array[y, x] = int(intensity * edge_falloff)
    
    shadow = Image.fromarray(shadow_array)
    shadow = shadow.filter(ImageFilter.GaussianBlur(radius=8))
    
    # Apply shadow to result
    shadow_layer = Image.new('RGBA', (width, height), (0, 0, 0, 0))
    shadow_layer.putalpha(shadow)
    
    # Paste shadow slightly below vehicle
    result.paste(Image.new('RGB', (width, height), (0, 0, 0)), 
                 (padding, padding + 3), shadow)
    
    # Paste vehicle on top
    result.paste(image, (padding, padding), image)
    
    return result

def create_white_background(image: Image.Image) -> Image.Image:
    """
    Create version with clean white background.
    """
    white_bg = Image.new('RGBA', image.size, (255, 255, 255, 255))
    white_bg.paste(image, (0, 0), image)
    return white_bg.convert('RGB')

def process_image(image_path: Path, mode: str, api_key: str = None) -> dict:
    """
    Process a single image with the specified mode.
    """
    results = {}
    
    try:
        # Step 1: Remove background
        if mode == 'api' and api_key:
            result = remove_bg_api(image_path, api_key)
        elif mode == 'local':
            try:
                result = remove_bg_local(image_path)
            except Exception as e:
                print(f"  ⚠️ BRIA failed: {e}")
                print(f"  → Falling back to BiRefNet...")
                result = remove_bg_rembg_birefnet(image_path)
        else:
            result = remove_bg_rembg_birefnet(image_path)
        
        # Step 2: Refine edges (only for local processing)
        if mode != 'api':
            result = refine_edges(result)
        
        # Step 3: Save transparent version
        stem = image_path.stem
        transparent_path = OUTPUT_DIR / "transparent" / f"{stem}_transparent.png"
        result.save(transparent_path, 'PNG', optimize=True)
        results['transparent'] = transparent_path
        
        # Step 4: Save white background version
        white_bg = create_white_background(result)
        white_path = OUTPUT_DIR / "white_bg" / f"{stem}_white.png"
        white_bg.save(white_path, 'PNG', optimize=True)
        results['white_bg'] = white_path
        
        # Step 5: Save version with professional shadow
        shadow_version = add_professional_shadow(result)
        shadow_path = OUTPUT_DIR / "shadow" / f"{stem}_shadow.png"
        shadow_version.save(shadow_path, 'PNG', optimize=True)
        results['shadow'] = shadow_path
        
        results['success'] = True
        
    except Exception as e:
        results['success'] = False
        results['error'] = str(e)
    
    return results

def main():
    parser = argparse.ArgumentParser(description='Professional Background Removal V4')
    parser.add_argument('--mode', choices=['api', 'local', 'fallback'], default='local',
                       help='api: use remove.bg API, local: use BRIA RMBG 2.0, fallback: use rembg BiRefNet')
    parser.add_argument('--api-key', type=str, default=os.environ.get('REMOVEBG_API_KEY'),
                       help='remove.bg API key (or set REMOVEBG_API_KEY env var)')
    args = parser.parse_args()
    
    print("=" * 60)
    print("🚗 Professional Background Removal V4")
    print("=" * 60)
    
    if args.mode == 'api':
        if not args.api_key:
            print("\n❌ API mode requires --api-key or REMOVEBG_API_KEY env var")
            print("   Get your free API key at: https://www.remove.bg/api")
            print("\n   Or use local mode: python remove_background_v4.py --mode local")
            sys.exit(1)
        print(f"Mode: remove.bg API (professional cloud processing)")
    elif args.mode == 'local':
        print(f"Mode: BRIA RMBG 2.0 (high-quality local processing)")
    else:
        print(f"Mode: rembg BiRefNet (fallback)")
    
    print(f"Input: {INPUT_DIR}")
    print(f"Output: {OUTPUT_DIR}")
    print("=" * 60)
    
    # Setup
    setup_directories()
    
    # Get images
    extensions = ['*.jpg', '*.jpeg', '*.png', '*.webp', '*.bmp']
    images = []
    for ext in extensions:
        images.extend(INPUT_DIR.glob(ext))
    
    if not images:
        print(f"\n❌ No images found in {INPUT_DIR}")
        sys.exit(1)
    
    print(f"\n📁 Found {len(images)} images to process\n")
    
    # Process each image
    start_time = time.time()
    successful = 0
    failed = 0
    
    for i, image_path in enumerate(sorted(images), 1):
        print(f"[{i}/{len(images)}] Processing: {image_path.name}")
        img_start = time.time()
        
        results = process_image(image_path, args.mode, args.api_key)
        
        img_time = time.time() - img_start
        
        if results['success']:
            print(f"  ✅ Done in {img_time:.1f}s")
            successful += 1
        else:
            print(f"  ❌ Failed: {results.get('error', 'Unknown error')}")
            failed += 1
        print()
    
    # Summary
    total_time = time.time() - start_time
    print("=" * 60)
    print("📊 SUMMARY")
    print("=" * 60)
    print(f"✅ Successful: {successful}")
    print(f"❌ Failed: {failed}")
    print(f"⏱️  Total time: {total_time:.1f}s")
    print(f"⏱️  Average: {total_time/len(images):.1f}s per image")
    print(f"\n📂 Output folders:")
    print(f"   • Transparent: {OUTPUT_DIR}/transparent/")
    print(f"   • White BG:    {OUTPUT_DIR}/white_bg/")
    print(f"   • With Shadow: {OUTPUT_DIR}/shadow/")
    print("=" * 60)

if __name__ == "__main__":
    main()
