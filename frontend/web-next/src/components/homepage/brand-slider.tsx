/**
 * Professional Brand Slider
 *
 * Animated brand logos carousel with hover effects
 * Shows popular car makes with their logos
 */

'use client';

import Image from 'next/image';
import Link from 'next/link';
import { cn } from '@/lib/utils';
import { useInView } from '@/hooks/use-in-view';

// =============================================================================
// TYPES
// =============================================================================

interface Brand {
  id: string;
  name: string;
  slug: string;
  logoUrl?: string;
  vehicleCount?: number;
}

interface BrandSliderProps {
  brands?: Brand[];
  autoScroll?: boolean;
  scrollSpeed?: number;
  className?: string;
}

// =============================================================================
// DEFAULT BRANDS
// =============================================================================

const DEFAULT_BRANDS: Brand[] = [
  { id: '1', name: 'Toyota', slug: 'toyota', vehicleCount: 2500 },
  { id: '2', name: 'Honda', slug: 'honda', vehicleCount: 1800 },
  { id: '3', name: 'Hyundai', slug: 'hyundai', vehicleCount: 1600 },
  { id: '4', name: 'Kia', slug: 'kia', vehicleCount: 1400 },
  { id: '5', name: 'Nissan', slug: 'nissan', vehicleCount: 1200 },
  { id: '6', name: 'Mazda', slug: 'mazda', vehicleCount: 800 },
  { id: '7', name: 'Ford', slug: 'ford', vehicleCount: 950 },
  { id: '8', name: 'Chevrolet', slug: 'chevrolet', vehicleCount: 750 },
  { id: '9', name: 'BMW', slug: 'bmw', vehicleCount: 320 },
  { id: '10', name: 'Mercedes-Benz', slug: 'mercedes-benz', vehicleCount: 280 },
  { id: '11', name: 'Audi', slug: 'audi', vehicleCount: 220 },
  { id: '12', name: 'Volkswagen', slug: 'volkswagen', vehicleCount: 400 },
];

// =============================================================================
// BRAND LOGO PLACEHOLDER (SVG)
// =============================================================================

function BrandLogoPlaceholder({ name }: { name: string }) {
  // Get first 2 letters of brand name
  const initials = name.substring(0, 2).toUpperCase();

  return (
    <div className="from-muted to-muted/80 flex h-full w-full items-center justify-center rounded-xl bg-gradient-to-br">
      <span className="text-muted-foreground text-2xl font-bold">{initials}</span>
    </div>
  );
}

// =============================================================================
// SINGLE BRAND CARD
// =============================================================================

function BrandCard({ brand }: { brand: Brand }) {
  return (
    <Link href={`/vehiculos?make=${brand.slug}`} className="group flex-shrink-0">
      <div className="border-border bg-card hover:border-primary/30 relative h-24 w-32 overflow-hidden rounded-2xl border shadow-md transition-all duration-300 hover:-translate-y-1 hover:scale-105 hover:shadow-xl active:scale-[0.98] md:h-28 md:w-40">
        {/* Logo Container */}
        <div className="absolute inset-2 flex items-center justify-center">
          {brand.logoUrl ? (
            <Image
              src={brand.logoUrl}
              alt={brand.name}
              fill
              sizes="160px"
              className="object-contain p-4 grayscale transition-all duration-300 group-hover:grayscale-0"
            />
          ) : (
            <BrandLogoPlaceholder name={brand.name} />
          )}
        </div>

        {/* Hover Overlay with Name */}
        <div className="absolute inset-0 flex items-end justify-center bg-gradient-to-t from-black/70 to-transparent pb-2 opacity-0 transition-opacity duration-300 group-hover:opacity-100">
          <span className="text-sm font-semibold text-white">{brand.name}</span>
        </div>

        {/* Vehicle Count Badge */}
        {brand.vehicleCount && (
          <div className="absolute top-1 right-1 opacity-0 transition-opacity duration-300 group-hover:opacity-100">
            <span className="bg-primary/10 text-primary rounded-full px-1.5 py-0.5 text-[10px] font-medium">
              {brand.vehicleCount.toLocaleString()}
            </span>
          </div>
        )}
      </div>
    </Link>
  );
}

// =============================================================================
// INFINITE SCROLL ANIMATION
// =============================================================================

function InfiniteScroll({
  brands,
  direction = 'left',
  speed = 30,
}: {
  brands: Brand[];
  direction?: 'left' | 'right';
  speed?: number;
}) {
  // Duplicate brands for seamless infinite scroll
  const duplicatedBrands = [...brands, ...brands];

  return (
    <div
      className="animate-scroll flex gap-4 md:gap-6"
      style={
        {
          '--scroll-direction': direction === 'left' ? '0' : '-100%',
          '--scroll-distance': direction === 'left' ? '-50%' : '0',
          '--scroll-duration': `${speed}s`,
        } as React.CSSProperties
      }
    >
      {duplicatedBrands.map((brand, index) => (
        <BrandCard key={`${brand.id}-${index}`} brand={brand} />
      ))}
    </div>
  );
}

// =============================================================================
// MAIN COMPONENT
// =============================================================================

export function BrandSlider({
  brands = DEFAULT_BRANDS,
  autoScroll = true,
  scrollSpeed = 30,
  className,
}: BrandSliderProps) {
  return (
    <div className={cn('relative overflow-hidden py-4', className)}>
      {/* Gradient Masks */}
      <div className="from-background pointer-events-none absolute top-0 bottom-0 left-0 z-10 w-20 bg-gradient-to-r to-transparent md:w-40" />
      <div className="from-background pointer-events-none absolute top-0 right-0 bottom-0 z-10 w-20 bg-gradient-to-l to-transparent md:w-40" />

      {/* Scrolling Container */}
      <div className="flex gap-4 overflow-hidden md:gap-6">
        {autoScroll ? (
          <InfiniteScroll brands={brands} speed={scrollSpeed} />
        ) : (
          <div className="scrollbar-hide flex gap-4 overflow-x-auto px-4 md:gap-6">
            {brands.map(brand => (
              <BrandCard key={brand.id} brand={brand} />
            ))}
          </div>
        )}
      </div>

      {/* CSS for infinite scroll animation */}
      <style jsx global>{`
        @keyframes scroll {
          0% {
            transform: translateX(0);
          }
          100% {
            transform: translateX(-50%);
          }
        }

        .animate-scroll {
          animation: scroll var(--scroll-duration, 30s) linear infinite;
        }

        .animate-scroll:hover {
          animation-play-state: paused;
        }

        .scrollbar-hide::-webkit-scrollbar {
          display: none;
        }

        .scrollbar-hide {
          -ms-overflow-style: none;
          scrollbar-width: none;
        }
      `}</style>
    </div>
  );
}

// =============================================================================
// BRAND GRID (ALTERNATIVE LAYOUT)
// =============================================================================

interface BrandGridProps {
  brands?: Brand[];
  columns?: 4 | 6 | 8;
  className?: string;
}

export function BrandGrid({
  brands = DEFAULT_BRANDS.slice(0, 8),
  columns = 4,
  className,
}: BrandGridProps) {
  const columnClasses = {
    4: 'grid-cols-2 md:grid-cols-4',
    6: 'grid-cols-3 md:grid-cols-6',
    8: 'grid-cols-4 md:grid-cols-8',
  };

  const { ref, inView } = useInView();

  return (
    <div ref={ref} className={cn(`grid ${columnClasses[columns]} gap-4`, className)}>
      {brands.map((brand, index) => (
        <div
          key={brand.id}
          className={cn(
            'transition-all duration-500',
            inView ? 'translate-y-0 opacity-100' : 'translate-y-5 opacity-0'
          )}
          style={{ transitionDelay: `${index * 50}ms` }}
        >
          <Link
            href={`/vehiculos?make=${brand.slug}`}
            className="group border-border bg-card hover:border-primary/30 flex flex-col items-center gap-3 rounded-2xl border p-4 transition-all duration-300 hover:shadow-lg md:p-6"
          >
            {/* Logo */}
            <div className="relative h-16 w-16 md:h-20 md:w-20">
              {brand.logoUrl ? (
                <Image
                  src={brand.logoUrl}
                  alt={brand.name}
                  fill
                  sizes="80px"
                  className="object-contain grayscale transition-all duration-300 group-hover:grayscale-0"
                />
              ) : (
                <BrandLogoPlaceholder name={brand.name} />
              )}
            </div>

            {/* Name */}
            <span className="text-foreground group-hover:text-primary text-sm font-semibold transition-colors">
              {brand.name}
            </span>

            {/* Vehicle Count */}
            {brand.vehicleCount && (
              <span className="text-muted-foreground text-xs">
                {brand.vehicleCount.toLocaleString()} vehículos
              </span>
            )}
          </Link>
        </div>
      ))}
    </div>
  );
}

export default BrandSlider;
