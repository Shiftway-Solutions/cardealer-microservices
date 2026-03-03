/**
 * Vehicle Gallery Component
 * Full-featured image gallery with lightbox, thumbnails, and navigation
 */

'use client';

import * as React from 'react';
import Image from 'next/image';
import { ChevronLeft, ChevronRight, Expand, X, Play, View } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';
import type { VehicleImage } from '@/types';

interface VehicleGalleryProps {
  images: VehicleImage[];
  title: string;
  has360View?: boolean;
  hasVideo?: boolean;
  className?: string;
}

export function VehicleGallery({
  images,
  title,
  has360View,
  hasVideo,
  className,
}: VehicleGalleryProps) {
  const [currentIndex, setCurrentIndex] = React.useState(0);
  const [isLightboxOpen, setIsLightboxOpen] = React.useState(false);
  const lightboxRef = React.useRef<HTMLDivElement>(null);
  const previousFocusRef = React.useRef<HTMLElement | null>(null);

  // Touch swipe state
  const touchStartRef = React.useRef<number | null>(null);
  const touchEndRef = React.useRef<number | null>(null);
  const minSwipeDistance = 50;

  // Sort images by order, primary first
  const sortedImages = React.useMemo(() => {
    return [...images].sort((a, b) => {
      if (a.isPrimary) return -1;
      if (b.isPrimary) return 1;
      return a.order - b.order;
    });
  }, [images]);

  const currentImage = sortedImages[currentIndex];
  const totalImages = sortedImages.length;

  const goToNext = React.useCallback(() => {
    setCurrentIndex(prev => (prev + 1) % totalImages);
  }, [totalImages]);

  const goToPrevious = React.useCallback(() => {
    setCurrentIndex(prev => (prev - 1 + totalImages) % totalImages);
  }, [totalImages]);

  const goToIndex = React.useCallback((index: number) => {
    setCurrentIndex(index);
  }, []);

  // Touch swipe handlers
  const handleTouchStart = React.useCallback((e: React.TouchEvent) => {
    touchEndRef.current = null;
    touchStartRef.current = e.targetTouches[0].clientX;
  }, []);

  const handleTouchMove = React.useCallback((e: React.TouchEvent) => {
    touchEndRef.current = e.targetTouches[0].clientX;
  }, []);

  const handleTouchEnd = React.useCallback(() => {
    if (!touchStartRef.current || !touchEndRef.current) return;
    const distance = touchStartRef.current - touchEndRef.current;
    if (Math.abs(distance) >= minSwipeDistance) {
      if (distance > 0) goToNext();
      else goToPrevious();
    }
    touchStartRef.current = null;
    touchEndRef.current = null;
  }, [goToNext, goToPrevious]);

  // Keyboard navigation
  React.useEffect(() => {
    if (!isLightboxOpen) return;

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'ArrowRight') goToNext();
      if (e.key === 'ArrowLeft') goToPrevious();
      if (e.key === 'Escape') setIsLightboxOpen(false);
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isLightboxOpen, goToNext, goToPrevious]);

  // Body scroll lock when lightbox is open
  React.useEffect(() => {
    if (isLightboxOpen) {
      const scrollbarWidth = window.innerWidth - document.documentElement.clientWidth;
      document.body.style.overflow = 'hidden';
      document.body.style.paddingRight = `${scrollbarWidth}px`;
    } else {
      document.body.style.overflow = '';
      document.body.style.paddingRight = '';
    }
    return () => {
      document.body.style.overflow = '';
      document.body.style.paddingRight = '';
    };
  }, [isLightboxOpen]);

  // Focus trap for lightbox
  React.useEffect(() => {
    if (isLightboxOpen) {
      previousFocusRef.current = document.activeElement as HTMLElement;
      // Focus lightbox container
      requestAnimationFrame(() => lightboxRef.current?.focus());
    } else if (previousFocusRef.current) {
      previousFocusRef.current.focus();
      previousFocusRef.current = null;
    }
  }, [isLightboxOpen]);

  if (totalImages === 0) {
    return (
      <div
        className={cn(
          'border-border flex aspect-[16/10] items-center justify-center rounded-2xl border bg-gradient-to-br from-slate-100 via-slate-50 to-slate-200 dark:from-slate-800 dark:via-slate-900 dark:to-slate-700',
          className
        )}
      >
        <div className="flex flex-col items-center gap-2">
          <svg
            className="h-16 w-16 text-slate-300 dark:text-slate-600"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={1.5}
              d="M8.25 18.75a1.5 1.5 0 01-3 0m3 0a1.5 1.5 0 00-3 0m3 0h6m-9 0H3.375a1.125 1.125 0 01-1.125-1.125V14.25m17.25 4.5a1.5 1.5 0 01-3 0m3 0a1.5 1.5 0 00-3 0m3 0h1.125c.621 0 1.129-.504 1.09-1.124a17.902 17.902 0 00-3.213-9.193 2.056 2.056 0 00-1.58-.86H14.25M16.5 18.75h-2.25m0-11.177v-.958c0-.568-.422-1.048-.987-1.106a48.554 48.554 0 00-10.026 0 1.106 1.106 0 00-.987 1.106v7.635m12-6.677v6.677m0 4.5v-4.5m0 0h-12"
            />
          </svg>
          <p className="text-sm font-medium text-slate-400 dark:text-slate-500">
            Sin imágenes disponibles
          </p>
        </div>
      </div>
    );
  }

  return (
    <>
      <div
        className={cn(
          'border-border overflow-hidden rounded-2xl border bg-white dark:bg-slate-900',
          className
        )}
      >
        {/* Main Image */}
        <div
          className="group relative aspect-[16/10]"
          onTouchStart={handleTouchStart}
          onTouchMove={handleTouchMove}
          onTouchEnd={handleTouchEnd}
        >
          <Image
            src={currentImage?.url || '/placeholder-car.jpg'}
            alt={currentImage?.alt || title}
            fill
            priority
            sizes="(max-width: 768px) 100vw, (max-width: 1200px) 66vw, 800px"
            className="object-cover"
          />

          {/* Navigation Arrows */}
          {totalImages > 1 && (
            <>
              <button
                onClick={goToPrevious}
                className="absolute top-1/2 left-3 flex h-11 w-11 -translate-y-1/2 items-center justify-center rounded-full bg-white/90 shadow-lg transition-opacity hover:bg-white md:opacity-0 md:group-hover:opacity-100"
                aria-label="Imagen anterior"
              >
                <ChevronLeft className="h-6 w-6" />
              </button>
              <button
                onClick={goToNext}
                className="absolute top-1/2 right-3 flex h-11 w-11 -translate-y-1/2 items-center justify-center rounded-full bg-white/90 shadow-lg transition-opacity hover:bg-white md:opacity-0 md:group-hover:opacity-100"
                aria-label="Siguiente imagen"
              >
                <ChevronRight className="h-6 w-6" />
              </button>
            </>
          )}

          {/* Expand Button */}
          <button
            onClick={() => setIsLightboxOpen(true)}
            className="absolute top-3 right-3 flex h-11 w-11 items-center justify-center rounded-full bg-black/60 text-white transition-opacity hover:bg-black/80 md:opacity-0 md:group-hover:opacity-100"
            aria-label="Ver pantalla completa"
          >
            <Expand className="h-5 w-5" />
          </button>

          {/* Image Counter */}
          <div className="absolute bottom-3 left-3 rounded-full bg-black/60 px-3 py-1.5 text-sm font-medium text-white">
            {currentIndex + 1} / {totalImages}
          </div>

          {/* Special Features Badges */}
          <div className="absolute right-3 bottom-3 flex gap-2">
            {has360View && (
              <Badge variant="secondary" className="gap-1 border-0 bg-black/60 text-white">
                <View className="h-3 w-3" />
                360°
              </Badge>
            )}
            {hasVideo && (
              <Badge variant="secondary" className="gap-1 border-0 bg-black/60 text-white">
                <Play className="h-3 w-3" />
                Video
              </Badge>
            )}
          </div>
        </div>

        {/* Thumbnail Strip */}
        {totalImages > 1 && (
          <div className="border-border border-t p-3">
            <div className="scrollbar-hide flex gap-2 overflow-x-auto">
              {sortedImages.map((image, index) => (
                <button
                  key={image.id}
                  onClick={() => goToIndex(index)}
                  className={cn(
                    'relative h-14 w-20 flex-shrink-0 overflow-hidden rounded-lg border-2 transition-all',
                    index === currentIndex
                      ? 'border-[#00A870] ring-2 ring-[#00A870]/20'
                      : 'border-transparent hover:border-slate-300'
                  )}
                  aria-label={`Ver imagen ${index + 1}`}
                >
                  <Image
                    src={image.thumbnailUrl || image.url}
                    alt={image.alt || `${title} - Imagen ${index + 1}`}
                    fill
                    sizes="64px"
                    className="object-cover"
                  />
                </button>
              ))}
            </div>
          </div>
        )}
      </div>

      {/* Lightbox */}
      {isLightboxOpen && (
        <div
          ref={lightboxRef}
          role="dialog"
          aria-modal="true"
          aria-label={`Galería de imágenes: ${title}`}
          tabIndex={-1}
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/95 outline-none"
          onClick={() => setIsLightboxOpen(false)}
          onTouchStart={handleTouchStart}
          onTouchMove={handleTouchMove}
          onTouchEnd={handleTouchEnd}
        >
          {/* Close Button */}
          <button
            onClick={() => setIsLightboxOpen(false)}
            className="absolute top-4 right-4 z-10 flex h-12 w-12 items-center justify-center rounded-full bg-white/10 text-white transition-colors hover:bg-white/20"
            aria-label="Cerrar"
          >
            <X className="h-6 w-6" />
          </button>

          {/* Navigation */}
          {totalImages > 1 && (
            <>
              <button
                onClick={e => {
                  e.stopPropagation();
                  goToPrevious();
                }}
                className="absolute top-1/2 left-4 flex h-12 w-12 -translate-y-1/2 items-center justify-center rounded-full bg-white/10 text-white transition-colors hover:bg-white/20"
                aria-label="Imagen anterior"
              >
                <ChevronLeft className="h-8 w-8" />
              </button>
              <button
                onClick={e => {
                  e.stopPropagation();
                  goToNext();
                }}
                className="absolute top-1/2 right-4 flex h-12 w-12 -translate-y-1/2 items-center justify-center rounded-full bg-white/10 text-white transition-colors hover:bg-white/20"
                aria-label="Siguiente imagen"
              >
                <ChevronRight className="h-8 w-8" />
              </button>
            </>
          )}

          {/* Main Image */}
          <div
            className="relative h-full max-h-[90vh] w-full max-w-6xl p-4"
            onClick={e => e.stopPropagation()}
          >
            <Image
              src={currentImage?.url || '/placeholder-car.jpg'}
              alt={currentImage?.alt || title}
              fill
              sizes="100vw"
              className="object-contain"
            />
          </div>

          {/* Counter */}
          <div className="absolute bottom-4 left-1/2 -translate-x-1/2 rounded-full bg-white/10 px-4 py-2 text-sm text-white">
            {currentIndex + 1} / {totalImages}
          </div>
        </div>
      )}
    </>
  );
}

export default VehicleGallery;
