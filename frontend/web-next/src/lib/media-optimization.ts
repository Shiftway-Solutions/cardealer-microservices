/**
 * Media Optimization Utilities for Dominican Republic
 *
 * Optimizes images and videos for users with slow internet connections.
 * Dominican Republic context: significant population on 3G/slow 4G.
 *
 * Strategies:
 * 1. Progressive image loading (blur-up technique)
 * 2. WebP/AVIF format with fallback
 * 3. Responsive srcset for different screen sizes
 * 4. Lazy loading with Intersection Observer
 * 5. Connection-aware quality selection
 * 6. Client-side image compression before upload
 * 7. Low-data mode for slow connections
 */

// ============================================================
// CONNECTION DETECTION
// ============================================================

export type ConnectionQuality = 'fast' | 'moderate' | 'slow' | 'offline';

interface NetworkInfo {
  effectiveType: '4g' | '3g' | '2g' | 'slow-2g';
  downlink: number; // Mbps
  rtt: number; // ms
  saveData: boolean;
}

/**
 * Detect network quality using Navigator.connection API
 * Falls back to 'moderate' if API unavailable
 */
export function getConnectionQuality(): ConnectionQuality {
  if (typeof navigator === 'undefined') return 'moderate';
  if (!navigator.onLine) return 'offline';

  const conn = (navigator as Navigator & { connection?: NetworkInfo }).connection;
  if (!conn) return 'moderate';

  if (conn.saveData) return 'slow';

  switch (conn.effectiveType) {
    case '4g':
      return conn.downlink >= 5 ? 'fast' : 'moderate';
    case '3g':
      return 'slow';
    case '2g':
    case 'slow-2g':
      return 'slow';
    default:
      return 'moderate';
  }
}

/**
 * Check if user has Save Data mode enabled
 */
export function isSaveDataEnabled(): boolean {
  if (typeof navigator === 'undefined') return false;
  const conn = (navigator as Navigator & { connection?: NetworkInfo }).connection;
  return conn?.saveData ?? false;
}

// ============================================================
// IMAGE QUALITY SETTINGS PER CONNECTION
// ============================================================

export interface ImageQualitySettings {
  quality: number; // 1-100
  maxWidth: number;
  maxHeight: number;
  format: 'webp' | 'jpeg' | 'avif';
  enableBlurPlaceholder: boolean;
  placeholderSize: number; // px for blur-up
}

const QUALITY_PRESETS: Record<ConnectionQuality, ImageQualitySettings> = {
  fast: {
    quality: 85,
    maxWidth: 1920,
    maxHeight: 1440,
    format: 'webp',
    enableBlurPlaceholder: false,
    placeholderSize: 0,
  },
  moderate: {
    quality: 75,
    maxWidth: 1280,
    maxHeight: 960,
    format: 'webp',
    enableBlurPlaceholder: true,
    placeholderSize: 20,
  },
  slow: {
    quality: 55,
    maxWidth: 800,
    maxHeight: 600,
    format: 'webp',
    enableBlurPlaceholder: true,
    placeholderSize: 10,
  },
  offline: {
    quality: 40,
    maxWidth: 640,
    maxHeight: 480,
    format: 'jpeg',
    enableBlurPlaceholder: true,
    placeholderSize: 8,
  },
};

/**
 * Get optimal image quality settings based on current connection
 */
export function getImageQualitySettings(override?: ConnectionQuality): ImageQualitySettings {
  const quality = override || getConnectionQuality();
  return QUALITY_PRESETS[quality];
}

// ============================================================
// CLIENT-SIDE IMAGE COMPRESSION
// ============================================================

interface CompressOptions {
  maxWidth?: number;
  maxHeight?: number;
  quality?: number; // 0-1
  format?: 'image/webp' | 'image/jpeg';
  maxSizeKB?: number;
}

/**
 * Compress an image file client-side before uploading.
 * Uses Canvas API for resize + quality reduction.
 *
 * @param file - Original image File
 * @param options - Compression options
 * @returns Compressed File
 */
export async function compressImage(file: File, options: CompressOptions = {}): Promise<File> {
  const {
    maxWidth = 1600,
    maxHeight = 1200,
    quality = 0.8,
    format = 'image/webp',
    maxSizeKB = 500,
  } = options;

  // Skip if already small enough
  if (file.size <= maxSizeKB * 1024) return file;

  return new Promise((resolve, reject) => {
    const img = new window.Image();
    const reader = new FileReader();

    reader.onload = e => {
      img.onload = () => {
        // Calculate new dimensions
        let { width, height } = img;
        if (width > maxWidth) {
          height = Math.round((height * maxWidth) / width);
          width = maxWidth;
        }
        if (height > maxHeight) {
          width = Math.round((width * maxHeight) / height);
          height = maxHeight;
        }

        // Draw to canvas
        const canvas = document.createElement('canvas');
        canvas.width = width;
        canvas.height = height;
        const ctx = canvas.getContext('2d');
        if (!ctx) {
          resolve(file);
          return;
        }

        ctx.drawImage(img, 0, 0, width, height);

        // Convert to blob
        canvas.toBlob(
          blob => {
            if (!blob) {
              resolve(file);
              return;
            }

            // If still too large, reduce quality further
            if (blob.size > maxSizeKB * 1024 && quality > 0.3) {
              compressImage(file, {
                ...options,
                quality: quality - 0.15,
              })
                .then(resolve)
                .catch(reject);
              return;
            }

            const ext = format === 'image/webp' ? '.webp' : '.jpg';
            const name = file.name.replace(/\.[^.]+$/, ext);
            resolve(new File([blob], name, { type: format }));
          },
          format,
          quality
        );
      };
      img.onerror = () => resolve(file);
      img.src = e.target?.result as string;
    };
    reader.onerror = () => resolve(file);
    reader.readAsDataURL(file);
  });
}

/**
 * Compress image with connection-aware quality settings
 */
export async function compressImageAdaptive(file: File): Promise<File> {
  const settings = getImageQualitySettings();
  return compressImage(file, {
    maxWidth: settings.maxWidth,
    maxHeight: settings.maxHeight,
    quality: settings.quality / 100,
    format: settings.format === 'jpeg' ? 'image/jpeg' : 'image/webp',
    maxSizeKB: settings.quality > 70 ? 800 : 400,
  });
}

// ============================================================
// BLUR PLACEHOLDER GENERATOR
// ============================================================

/**
 * Generate a tiny blur-up placeholder from an image
 * Used for progressive loading: show blurred preview while full image loads
 */
export async function generateBlurPlaceholder(file: File, size: number = 16): Promise<string> {
  return new Promise(resolve => {
    const img = new window.Image();
    const reader = new FileReader();

    reader.onload = e => {
      img.onload = () => {
        const canvas = document.createElement('canvas');
        canvas.width = size;
        canvas.height = Math.round((size * img.height) / img.width);
        const ctx = canvas.getContext('2d');
        if (!ctx) {
          resolve('');
          return;
        }
        ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
        resolve(canvas.toDataURL('image/jpeg', 0.3));
      };
      img.onerror = () => resolve('');
      img.src = e.target?.result as string;
    };
    reader.onerror = () => resolve('');
    reader.readAsDataURL(file);
  });
}

// ============================================================
// RESPONSIVE IMAGE URL BUILDER
// ============================================================

/**
 * Build responsive srcSet for Next.js Image or plain <img>
 * Generates URLs for different widths using the media service's
 * resize endpoint (if available) or Next.js Image Optimization
 */
export function buildResponsiveSrcSet(
  baseUrl: string,
  widths: number[] = [320, 640, 960, 1280, 1920]
): string {
  // If using Next.js Image component, this is handled automatically
  // For external URLs, build srcset manually
  if (baseUrl.includes('/_next/image')) return '';

  return widths
    .map(w => {
      // Use DO Spaces CDN transform if available
      if (baseUrl.includes('digitaloceanspaces.com')) {
        return `${baseUrl}?w=${w}&q=75 ${w}w`;
      }
      return `${baseUrl} ${w}w`;
    })
    .join(', ');
}

// ============================================================
// VIDEO OPTIMIZATION
// ============================================================

export interface VideoQualitySettings {
  maxResolution: '480p' | '720p' | '1080p';
  maxBitrateKbps: number;
  preload: 'none' | 'metadata' | 'auto';
  autoplay: boolean;
}

const VIDEO_QUALITY_PRESETS: Record<ConnectionQuality, VideoQualitySettings> = {
  fast: {
    maxResolution: '1080p',
    maxBitrateKbps: 4000,
    preload: 'metadata',
    autoplay: true,
  },
  moderate: {
    maxResolution: '720p',
    maxBitrateKbps: 2000,
    preload: 'metadata',
    autoplay: false,
  },
  slow: {
    maxResolution: '480p',
    maxBitrateKbps: 800,
    preload: 'none',
    autoplay: false,
  },
  offline: {
    maxResolution: '480p',
    maxBitrateKbps: 400,
    preload: 'none',
    autoplay: false,
  },
};

/**
 * Get optimal video quality settings based on connection
 */
export function getVideoQualitySettings(override?: ConnectionQuality): VideoQualitySettings {
  const quality = override || getConnectionQuality();
  return VIDEO_QUALITY_PRESETS[quality];
}

// ============================================================
// BATCH UPLOAD WITH PROGRESS
// ============================================================

export interface UploadProgress {
  fileId: string;
  fileName: string;
  progress: number; // 0-100
  status: 'pending' | 'compressing' | 'uploading' | 'done' | 'error';
  originalSize: number;
  compressedSize?: number;
}

/**
 * Upload multiple images with adaptive compression and progress tracking
 */
export async function uploadImagesAdaptive(
  files: File[],
  uploadUrl: string,
  onProgress: (progress: UploadProgress[]) => void
): Promise<string[]> {
  const progress: UploadProgress[] = files.map((f, i) => ({
    fileId: `file-${i}`,
    fileName: f.name,
    progress: 0,
    status: 'pending',
    originalSize: f.size,
  }));
  onProgress([...progress]);

  const urls: string[] = [];

  for (let i = 0; i < files.length; i++) {
    const file = files[i];

    // Compress
    progress[i].status = 'compressing';
    progress[i].progress = 10;
    onProgress([...progress]);

    const compressed = await compressImageAdaptive(file);
    progress[i].compressedSize = compressed.size;
    progress[i].progress = 30;
    progress[i].status = 'uploading';
    onProgress([...progress]);

    // Upload
    try {
      const formData = new FormData();
      formData.append('file', compressed);

      const res = await fetch(uploadUrl, {
        method: 'POST',
        body: formData,
      });

      if (res.ok) {
        const data = await res.json();
        urls.push(data.url || data.data?.url || '');
        progress[i].progress = 100;
        progress[i].status = 'done';
      } else {
        progress[i].status = 'error';
      }
    } catch {
      progress[i].status = 'error';
    }

    onProgress([...progress]);
  }

  return urls;
}

// ============================================================
// ANALYSIS REPORT
// ============================================================

/**
 * Generate a network analysis report for debugging
 */
export function getNetworkAnalysis(): {
  quality: ConnectionQuality;
  imageSettings: ImageQualitySettings;
  videoSettings: VideoQualitySettings;
  saveData: boolean;
  recommendations: string[];
} {
  const quality = getConnectionQuality();
  const recommendations: string[] = [];

  switch (quality) {
    case 'slow':
      recommendations.push(
        'Imágenes serán comprimidas al 55% de calidad',
        'Videos no se reproducirán automáticamente',
        'Se usará resolución de 800x600 max',
        'Placeholders difuminados mientras cargan las imágenes',
        'Considere activar el modo de datos reducidos en su navegador'
      );
      break;
    case 'moderate':
      recommendations.push(
        'Imágenes en calidad media (75%)',
        'Resolución optimizada a 1280x960',
        'Carga progresiva habilitada'
      );
      break;
    case 'fast':
      recommendations.push(
        'Calidad completa de imágenes (85%)',
        'Resolución hasta 1920px',
        'Videos con reproducción automática'
      );
      break;
    case 'offline':
      recommendations.push(
        'Sin conexión — usando caché del navegador',
        'Las imágenes nuevas no cargarán'
      );
      break;
  }

  return {
    quality,
    imageSettings: getImageQualitySettings(),
    videoSettings: getVideoQualitySettings(),
    saveData: isSaveDataEnabled(),
    recommendations,
  };
}
