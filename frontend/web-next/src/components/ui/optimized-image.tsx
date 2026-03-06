/**
 * Optimized Image Component
 *
 * Wrapper around next/image with:
 * - Blur placeholder generation
 * - Lazy loading by default
 * - Responsive sizes
 * - CDN integration for external images
 * - Error fallback handling
 */

'use client';

import Image, { ImageProps } from 'next/image';
import { useState, useCallback, useEffect } from 'react';
import { cn } from '@/lib/utils';
import { getConnectionSpeed, getOptimalQuality } from '@/lib/image-utils';

// =============================================================================
// TYPES
// =============================================================================

interface OptimizedImageProps extends Omit<ImageProps, 'src'> {
  src: string | null | undefined;
  fallbackSrc?: string;
  aspectRatio?: 'auto' | 'square' | '4/3' | '16/9' | '3/2' | '21/9';
  showSkeleton?: boolean;
  enableZoom?: boolean;
}

// =============================================================================
// CONSTANTS
// =============================================================================

const FALLBACK_IMAGE = '/placeholder-car.jpg';
const CDN_URL = process.env.NEXT_PUBLIC_CDN_URL || '';

// Blur data URL - a tiny blurred placeholder
const BLUR_DATA_URL =
  'data:image/jpeg;base64,/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAYEBQYFBAYGBQYHBwYIChAKCgkJChQODwwQFxQYGBcUFhYaHSUfGhsjHBYWICwgIyYnKSopGR8tMC0oMCUoKSj/2wBDAQcHBwoIChMKChMoGhYaKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCj/wAARCAAIAAoDASIAAhEBAxEB/8QAFgABAQEAAAAAAAAAAAAAAAAAAAUH/8QAIhAAAgEDBAMBAAAAAAAAAAAAAQIDAAQRBQYSIRMxQVH/xAAUAQEAAAAAAAAAAAAAAAAAAAAA/8QAFBEBAAAAAAAAAAAAAAAAAAAAAP/aAAwDAQACEQMRAD8Aw/S9KuLm3WYsscbEhWYE5A+0q/tlN/L/2Q==';

// Aspect ratio classes
const aspectRatioClasses: Record<string, string> = {
  auto: '',
  square: 'aspect-square',
  '4/3': 'aspect-[4/3]',
  '16/9': 'aspect-video',
  '3/2': 'aspect-[3/2]',
  '21/9': 'aspect-[21/9]',
};

// =============================================================================
// HELPERS
// =============================================================================

/**
 * Get the optimized image URL
 * - If it's a relative URL, use as-is
 * - If it's an external URL and we have CDN, proxy through CDN
 * - Otherwise use the original URL
 */
function getOptimizedSrc(src: string): string {
  if (!src) return FALLBACK_IMAGE;

  // Already a relative path
  if (src.startsWith('/')) return src;

  // If we have a CDN configured and it's an external URL
  if (CDN_URL && src.startsWith('http')) {
    // Convert to CDN URL for optimization
    return `${CDN_URL}/_next/image?url=${encodeURIComponent(src)}&w=1200&q=75`;
  }

  return src;
}

/**
 * Generate responsive sizes string based on usage
 */
function getResponsiveSizes(_width?: number | string): string {
  // Default responsive sizes for vehicle cards
  return '(max-width: 640px) 100vw, (max-width: 768px) 50vw, (max-width: 1024px) 33vw, 25vw';
}

// =============================================================================
// COMPONENT
// =============================================================================

export function OptimizedImage({
  src,
  alt,
  fallbackSrc = FALLBACK_IMAGE,
  aspectRatio = 'auto',
  showSkeleton = true,
  enableZoom = false,
  className,
  fill,
  width,
  height,
  priority = false,
  quality,
  ...props
}: OptimizedImageProps & { quality?: number }) {
  const [isLoading, setIsLoading] = useState(true);
  const [hasError, setHasError] = useState(false);
  const [adaptiveQuality, setAdaptiveQuality] = useState(
    () => quality ?? getOptimalQuality(getConnectionSpeed())
  );

  // Subscribe to connection changes — only update quality when connection type changes
  useEffect(() => {
    if (quality) return; // explicit quality takes precedence

    const conn =
      typeof navigator !== 'undefined'
        ? navigator.connection || navigator.mozConnection || navigator.webkitConnection
        : null;
    if (conn?.addEventListener) {
      const handleChange = () => setAdaptiveQuality(getOptimalQuality(getConnectionSpeed()));
      conn.addEventListener('change', handleChange);
      return () => conn.removeEventListener?.('change', handleChange);
    }
  }, [quality]);

  const handleLoad = useCallback(() => {
    setIsLoading(false);
  }, []);

  const handleError = useCallback(() => {
    setHasError(true);
    setIsLoading(false);
  }, []);

  const imageSrc = hasError ? fallbackSrc : getOptimizedSrc(src || fallbackSrc);

  return (
    <div
      className={cn(
        'bg-muted relative overflow-hidden',
        aspectRatioClasses[aspectRatio],
        enableZoom && 'group cursor-zoom-in',
        className
      )}
    >
      {/* Loading skeleton */}
      {showSkeleton && isLoading && (
        <div className="from-muted via-muted-foreground/10 to-muted absolute inset-0 animate-pulse bg-gradient-to-r" />
      )}

      <Image
        src={imageSrc}
        alt={alt}
        fill={fill ?? !width}
        width={fill ? undefined : width}
        height={fill ? undefined : height}
        sizes={getResponsiveSizes(width)}
        quality={adaptiveQuality}
        placeholder="blur"
        blurDataURL={BLUR_DATA_URL}
        loading={priority ? 'eager' : 'lazy'}
        priority={priority}
        onLoad={handleLoad}
        onError={handleError}
        className={cn(
          'object-cover transition-all duration-300',
          isLoading && 'opacity-0',
          !isLoading && 'opacity-100',
          enableZoom && 'group-hover:scale-105'
        )}
        {...props}
      />
    </div>
  );
}

// =============================================================================
// SPECIALIZED VARIANTS
// =============================================================================

/**
 * Vehicle thumbnail - optimized for cards
 */
export function VehicleThumbnail({
  src,
  alt,
  className,
  priority = false,
}: {
  src: string | null | undefined;
  alt: string;
  className?: string;
  priority?: boolean;
}) {
  return (
    <OptimizedImage
      src={src}
      alt={alt}
      aspectRatio="4/3"
      fill
      priority={priority}
      className={cn('rounded-t-lg', className)}
      fallbackSrc="/images/placeholder-vehicle.jpg"
    />
  );
}

/**
 * Avatar image - circular with fallback
 */
export function AvatarImage({
  src,
  alt,
  size = 40,
  className,
}: {
  src: string | null | undefined;
  alt: string;
  size?: number;
  className?: string;
}) {
  return (
    <OptimizedImage
      src={src}
      alt={alt}
      width={size}
      height={size}
      aspectRatio="square"
      className={cn('rounded-full', className)}
      fallbackSrc="/images/avatar-placeholder.png"
    />
  );
}

/**
 * Hero image - full width with priority loading
 */
export function HeroImage({
  src,
  alt,
  className,
}: {
  src: string | null | undefined;
  alt: string;
  className?: string;
}) {
  return (
    <OptimizedImage
      src={src}
      alt={alt}
      aspectRatio="21/9"
      fill
      priority
      className={className}
      enableZoom
    />
  );
}

/**
 * Gallery image - with zoom capability
 */
export function GalleryImage({
  src,
  alt,
  className,
  onClick,
}: {
  src: string | null | undefined;
  alt: string;
  className?: string;
  onClick?: () => void;
}) {
  return (
    <div onClick={onClick} className={cn('cursor-pointer', className)}>
      <OptimizedImage src={src} alt={alt} aspectRatio="4/3" fill enableZoom />
    </div>
  );
}

export default OptimizedImage;
