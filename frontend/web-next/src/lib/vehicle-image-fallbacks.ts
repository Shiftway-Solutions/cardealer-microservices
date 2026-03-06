/**
 * Vehicle Image Fallback System
 *
 * Provides high-quality Unsplash photos as fallback images when vehicle
 * photos stored in S3 are unavailable, expired, or are placeholder files.
 *
 * Images are sourced from Unsplash (allowed in next.config.ts) and matched
 * by vehicle make, model, body style, or generic vehicle type.
 */

// Unsplash vehicle photos organized by make (most popular in DR market)
const makeImages: Record<string, string[]> = {
  toyota: [
    'https://images.unsplash.com/photo-1619767886558-efdc259cde1a?w=800&q=75',
    'https://images.unsplash.com/photo-1621007947382-bb3c3994e3fb?w=800&q=75',
    'https://images.unsplash.com/photo-1568844293986-8d0400f4f36d?w=800&q=75',
  ],
  honda: [
    'https://images.unsplash.com/photo-1606611013016-969c19ba27c5?w=800&q=75',
    'https://images.unsplash.com/photo-1583121274602-3e2820c69888?w=800&q=75',
    'https://images.unsplash.com/photo-1605559424843-9e4c228bf1c2?w=800&q=75',
  ],
  hyundai: [
    'https://images.unsplash.com/photo-1609521263047-f8f205293f24?w=800&q=75',
    'https://images.unsplash.com/photo-1625231334168-32354e3b4f4b?w=800&q=75',
    'https://images.unsplash.com/photo-1617814076367-b759c7d7e738?w=800&q=75',
  ],
  kia: [
    'https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=800&q=75',
    'https://images.unsplash.com/photo-1542362567-b07e54358753?w=800&q=75',
    'https://images.unsplash.com/photo-1503376780353-7e6692767b70?w=800&q=75',
  ],
  nissan: [
    'https://images.unsplash.com/photo-1533473359331-2969f3c6b787?w=800&q=75',
    'https://images.unsplash.com/photo-1494976388531-d1058494cdd8?w=800&q=75',
    'https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?w=800&q=75',
  ],
  'mercedes-benz': [
    'https://images.unsplash.com/photo-1618843479313-40f8afb4b4d8?w=800&q=75',
    'https://images.unsplash.com/photo-1617814076367-b759c7d7e738?w=800&q=75',
    'https://images.unsplash.com/photo-1616422285623-13ff0162193c?w=800&q=75',
  ],
  bmw: [
    'https://images.unsplash.com/photo-1555215695-3004980ad54e?w=800&q=75',
    'https://images.unsplash.com/photo-1556189250-72ba954cfc2b?w=800&q=75',
    'https://images.unsplash.com/photo-1580273916550-e323be2ae537?w=800&q=75',
  ],
  audi: [
    'https://images.unsplash.com/photo-1606664515524-ed2f786a0bd6?w=800&q=75',
    'https://images.unsplash.com/photo-1603584173870-7f23fdae1b7a?w=800&q=75',
    'https://images.unsplash.com/photo-1549317661-bd32c8ce0db2?w=800&q=75',
  ],
  lexus: [
    'https://images.unsplash.com/photo-1583121274602-3e2820c69888?w=800&q=75',
    'https://images.unsplash.com/photo-1605559424843-9e4c228bf1c2?w=800&q=75',
    'https://images.unsplash.com/photo-1542362567-b07e54358753?w=800&q=75',
  ],
  porsche: [
    'https://images.unsplash.com/photo-1503376780353-7e6692767b70?w=800&q=75',
    'https://images.unsplash.com/photo-1494976388531-d1058494cdd8?w=800&q=75',
    'https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?w=800&q=75',
  ],
  ford: [
    'https://images.unsplash.com/photo-1551830820-330a71b99659?w=800&q=75',
    'https://images.unsplash.com/photo-1533473359331-2969f3c6b787?w=800&q=75',
    'https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=800&q=75',
  ],
  chevrolet: [
    'https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=800&q=75',
    'https://images.unsplash.com/photo-1580273916550-e323be2ae537?w=800&q=75',
    'https://images.unsplash.com/photo-1533473359331-2969f3c6b787?w=800&q=75',
  ],
  volkswagen: [
    'https://images.unsplash.com/photo-1609521263047-f8f205293f24?w=800&q=75',
    'https://images.unsplash.com/photo-1617814076367-b759c7d7e738?w=800&q=75',
    'https://images.unsplash.com/photo-1606611013016-969c19ba27c5?w=800&q=75',
  ],
  tesla: [
    'https://images.unsplash.com/photo-1560958089-b8a1929cea89?w=800&q=75',
    'https://images.unsplash.com/photo-1536700503339-1e4b06520771?w=800&q=75',
    'https://images.unsplash.com/photo-1554744512-d6c603f27c54?w=800&q=75',
  ],
  mazda: [
    'https://images.unsplash.com/photo-1580273916550-e323be2ae537?w=800&q=75',
    'https://images.unsplash.com/photo-1549317661-bd32c8ce0db2?w=800&q=75',
    'https://images.unsplash.com/photo-1583121274602-3e2820c69888?w=800&q=75',
  ],
};

// Body style fallback images
const bodyStyleImages: Record<string, string[]> = {
  suv: [
    'https://images.unsplash.com/photo-1619767886558-efdc259cde1a?w=800&q=75',
    'https://images.unsplash.com/photo-1609521263047-f8f205293f24?w=800&q=75',
    'https://images.unsplash.com/photo-1606611013016-969c19ba27c5?w=800&q=75',
  ],
  crossover: [
    'https://images.unsplash.com/photo-1625231334168-32354e3b4f4b?w=800&q=75',
    'https://images.unsplash.com/photo-1617814076367-b759c7d7e738?w=800&q=75',
    'https://images.unsplash.com/photo-1621007947382-bb3c3994e3fb?w=800&q=75',
  ],
  sedan: [
    'https://images.unsplash.com/photo-1549317661-bd32c8ce0db2?w=800&q=75',
    'https://images.unsplash.com/photo-1553440569-bcc63803a83d?w=800&q=75',
    'https://images.unsplash.com/photo-1580273916550-e323be2ae537?w=800&q=75',
  ],
  hatchback: [
    'https://images.unsplash.com/photo-1609521263047-f8f205293f24?w=800&q=75',
    'https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=800&q=75',
    'https://images.unsplash.com/photo-1606611013016-969c19ba27c5?w=800&q=75',
  ],
  pickup: [
    'https://images.unsplash.com/photo-1533473359331-2969f3c6b787?w=800&q=75',
    'https://images.unsplash.com/photo-1551830820-330a71b99659?w=800&q=75',
    'https://images.unsplash.com/photo-1494976388531-d1058494cdd8?w=800&q=75',
  ],
  coupe: [
    'https://images.unsplash.com/photo-1503376780353-7e6692767b70?w=800&q=75',
    'https://images.unsplash.com/photo-1555215695-3004980ad54e?w=800&q=75',
    'https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=800&q=75',
  ],
  sport: [
    'https://images.unsplash.com/photo-1503376780353-7e6692767b70?w=800&q=75',
    'https://images.unsplash.com/photo-1494976388531-d1058494cdd8?w=800&q=75',
    'https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?w=800&q=75',
  ],
  convertible: [
    'https://images.unsplash.com/photo-1542362567-b07e54358753?w=800&q=75',
    'https://images.unsplash.com/photo-1556189250-72ba954cfc2b?w=800&q=75',
    'https://images.unsplash.com/photo-1605559424843-9e4c228bf1c2?w=800&q=75',
  ],
  van: [
    'https://images.unsplash.com/photo-1533473359331-2969f3c6b787?w=800&q=75',
    'https://images.unsplash.com/photo-1609521263047-f8f205293f24?w=800&q=75',
    'https://images.unsplash.com/photo-1568844293986-8d0400f4f36d?w=800&q=75',
  ],
  minivan: [
    'https://images.unsplash.com/photo-1568844293986-8d0400f4f36d?w=800&q=75',
    'https://images.unsplash.com/photo-1619767886558-efdc259cde1a?w=800&q=75',
    'https://images.unsplash.com/photo-1621007947382-bb3c3994e3fb?w=800&q=75',
  ],
};

// Generic vehicle images as last resort
const genericImages = [
  'https://images.unsplash.com/photo-1494976388531-d1058494cdd8?w=800&q=75',
  'https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?w=800&q=75',
  'https://images.unsplash.com/photo-1583121274602-3e2820c69888?w=800&q=75',
];

/**
 * Simple hash for consistent image selection based on vehicle ID
 */
function simpleHash(str: string): number {
  let hash = 0;
  for (let i = 0; i < str.length; i++) {
    const char = str.charCodeAt(i);
    hash = (hash << 5) - hash + char;
    hash = hash & hash; // Convert to 32-bit integer
  }
  return Math.abs(hash);
}

/**
 * Get a fallback Unsplash image URL for a vehicle based on its properties.
 * Uses deterministic selection so the same vehicle always gets the same image.
 *
 * @param vehicleId - Vehicle ID for consistent image selection
 * @param make - Vehicle make (e.g., "Toyota")
 * @param bodyStyle - Body style (e.g., "SUV", "Sedan")
 * @param index - Optional index for variety within a list
 */
export function getVehicleFallbackImage(
  vehicleId: string,
  make?: string,
  bodyStyle?: string,
  index?: number
): string {
  const hash = simpleHash(vehicleId || String(index || 0));

  // Try make-specific images first
  if (make) {
    const makeLower = make.toLowerCase();
    const makeImgs = makeImages[makeLower];
    if (makeImgs?.length) {
      return makeImgs[hash % makeImgs.length];
    }
  }

  // Then body style images
  if (bodyStyle) {
    const styleLower = bodyStyle.toLowerCase();
    const styleImgs = bodyStyleImages[styleLower];
    if (styleImgs?.length) {
      return styleImgs[hash % styleImgs.length];
    }
  }

  // Generic fallback
  return genericImages[hash % genericImages.length];
}

/**
 * Get up to 3 fallback images for gallery display.
 */
export function getVehicleFallbackImages(
  vehicleId: string,
  make?: string,
  bodyStyle?: string
): string[] {
  // Try make images first
  if (make) {
    const makeLower = make.toLowerCase();
    const makeImgs = makeImages[makeLower];
    if (makeImgs?.length) return makeImgs.slice(0, 3);
  }

  // Then body style
  if (bodyStyle) {
    const styleLower = bodyStyle.toLowerCase();
    const styleImgs = bodyStyleImages[styleLower];
    if (styleImgs?.length) return styleImgs.slice(0, 3);
  }

  return genericImages.slice(0, 3);
}

/**
 * Check if a URL is likely a valid image (not a placeholder or broken path).
 * Returns false for relative paths, blob URLs, or known placeholder paths.
 */
export function isValidImageUrl(url: string | undefined | null): boolean {
  if (!url) return false;
  if (url === '/placeholder-car.jpg') return false;
  if (url.startsWith('blob:')) return false;
  if (url.startsWith('/images/demo/')) return false;
  // Must be an absolute HTTP(S) URL
  return url.startsWith('http://') || url.startsWith('https://');
}
