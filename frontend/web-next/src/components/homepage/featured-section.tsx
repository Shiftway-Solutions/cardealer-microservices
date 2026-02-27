/**
 * FeaturedSection Component
 *
 * Horizontal scrollable section for homepage vehicle categories
 * Each category shows vehicles with custom accent colors
 */

'use client';

import { useRef, useState, useEffect } from 'react';
import { ChevronLeft, ChevronRight, ArrowRight } from 'lucide-react';
import Link from 'next/link';
import Image from 'next/image';
import { cn, formatCurrency, formatMileage } from '@/lib/utils';
import { Button } from '@/components/ui/button';

// Color mappings for accent colors
const accentColorClasses: Record<string, { bg: string; text: string; border: string }> = {
  blue: { bg: 'bg-blue-500', text: 'text-blue-600', border: 'border-blue-500' },
  green: { bg: 'bg-primary/100', text: 'text-primary', border: 'border-primary' },
  amber: { bg: 'bg-amber-500', text: 'text-amber-600', border: 'border-amber-500' },
  red: { bg: 'bg-red-500', text: 'text-red-600', border: 'border-red-500' },
  purple: { bg: 'bg-purple-500', text: 'text-purple-600', border: 'border-purple-500' },
  indigo: { bg: 'bg-indigo-500', text: 'text-indigo-600', border: 'border-indigo-500' },
  pink: { bg: 'bg-pink-500', text: 'text-pink-600', border: 'border-pink-500' },
  teal: { bg: 'bg-teal-500', text: 'text-teal-600', border: 'border-teal-500' },
  orange: { bg: 'bg-orange-500', text: 'text-orange-600', border: 'border-orange-500' },
};

export interface FeaturedListingItem {
  id: string;
  title: string;
  price: number;
  mileage: number;
  location: string;
  imageUrl: string;
  category: string;
  year: number;
  make: string;
  model: string;
  fuelType: string;
  transmission: string;
}

interface FeaturedSectionProps {
  title: string;
  subtitle?: string;
  listings: FeaturedListingItem[];
  viewAllHref?: string;
  accentColor?: string;
  className?: string;
}

export default function FeaturedSection({
  title,
  subtitle,
  listings,
  viewAllHref = '/vehiculos',
  accentColor = 'blue',
  className = '',
}: FeaturedSectionProps) {
  const scrollContainerRef = useRef<HTMLDivElement>(null);
  const [canScrollLeft, setCanScrollLeft] = useState(false);
  const [canScrollRight, setCanScrollRight] = useState(true);

  const colors = accentColorClasses[accentColor] || accentColorClasses.blue;

  const checkScrollPosition = () => {
    if (!scrollContainerRef.current) return;

    const { scrollLeft, scrollWidth, clientWidth } = scrollContainerRef.current;
    setCanScrollLeft(scrollLeft > 0);
    setCanScrollRight(scrollLeft < scrollWidth - clientWidth - 10);
  };

  useEffect(() => {
    checkScrollPosition();
    window.addEventListener('resize', checkScrollPosition);
    return () => window.removeEventListener('resize', checkScrollPosition);
  }, [listings]);

  const scroll = (direction: 'left' | 'right') => {
    if (!scrollContainerRef.current) return;

    const scrollAmount = 320; // Card width + gap
    const newScrollLeft =
      scrollContainerRef.current.scrollLeft + (direction === 'left' ? -scrollAmount : scrollAmount);

    scrollContainerRef.current.scrollTo({
      left: newScrollLeft,
      behavior: 'smooth',
    });
  };

  // Generate vehicle URL (mirrors backend GenerateSlug: {year}-{make}-{model}-{shortId8})
  const generateVehicleUrl = (item: FeaturedListingItem) => {
    const slug = `${item.year}-${item.make}-${item.model}`.toLowerCase().replace(/\s+/g, '-');
    const shortId = (item.id || '').replace(/-/g, '').slice(0, 8).toLowerCase();
    return `/vehiculos/${slug}-${shortId}`;
  };

  if (listings.length === 0) return null;

  return (
    <section className={cn('bg-background py-10 lg:py-12', className)}>
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="mb-6 flex items-center justify-between">
          <div>
            <div className="flex items-center gap-3">
              <div className={cn('h-8 w-1 rounded-full', colors.bg)} />
              <h2 className="text-foreground text-2xl leading-tight font-bold tracking-tight md:text-3xl">
                {title}
              </h2>
            </div>
            {subtitle && (
              <p className="text-muted-foreground mt-2 ml-4 leading-relaxed">{subtitle}</p>
            )}
          </div>

          <div className="flex items-center gap-2">
            {/* Navigation Arrows */}
            <div className="mr-4 hidden items-center gap-2 md:flex">
              <button
                onClick={() => scroll('left')}
                disabled={!canScrollLeft}
                className={cn(
                  'rounded-full border p-2 transition-colors',
                  canScrollLeft
                    ? 'border-border hover:border-muted-foreground hover:bg-accent'
                    : 'border-border/50 text-muted-foreground/50 cursor-not-allowed'
                )}
                aria-label="Scroll left"
              >
                <ChevronLeft className="h-5 w-5" />
              </button>
              <button
                onClick={() => scroll('right')}
                disabled={!canScrollRight}
                className={cn(
                  'rounded-full border p-2 transition-colors',
                  canScrollRight
                    ? 'border-border hover:border-muted-foreground hover:bg-accent'
                    : 'border-border/50 text-muted-foreground/50 cursor-not-allowed'
                )}
                aria-label="Scroll right"
              >
                <ChevronRight className="h-5 w-5" />
              </button>
            </div>

            {/* View All Link */}
            <Link href={viewAllHref}>
              <Button variant="outline" className={cn('group', colors.text, colors.border)}>
                Ver todos
                <ArrowRight className="ml-1 h-4 w-4 transition-transform group-hover:translate-x-1" />
              </Button>
            </Link>
          </div>
        </div>

        {/* Scrollable Cards Container */}
        <div
          ref={scrollContainerRef}
          onScroll={checkScrollPosition}
          className="scrollbar-hide flex snap-x snap-mandatory gap-4 overflow-x-auto pb-4"
          style={{ scrollbarWidth: 'none', msOverflowStyle: 'none' }}
        >
          {listings.map((item, index) => (
            <div
              key={item.id}
              className="animate-slide-up w-[280px] flex-shrink-0 snap-start sm:w-[300px]"
              style={{ animationDelay: `${index * 50}ms` }}
            >
              <Link href={generateVehicleUrl(item)} className="group block">
                <div className="border-border bg-card overflow-hidden rounded-xl border shadow-md transition-shadow duration-300 hover:shadow-xl">
                  {/* Image */}
                  <div className="relative aspect-[4/3] overflow-hidden">
                    <Image
                      src={item.imageUrl || '/placeholder-car.jpg'}
                      alt={item.title}
                      fill
                      sizes="300px"
                      className="object-cover transition-transform duration-300 group-hover:scale-105"
                    />
                    {/* Category Badge */}
                    <div
                      className={cn(
                        'absolute top-3 right-3 rounded-full px-2 py-1 text-xs font-semibold text-white',
                        colors.bg
                      )}
                    >
                      {item.category}
                    </div>
                  </div>

                  {/* Content */}
                  <div className="p-4">
                    <h3 className="group-hover:text-primary text-foreground mb-1 line-clamp-1 leading-snug font-bold tracking-tight transition-colors">
                      {item.title}
                    </h3>
                    <p className={cn('mb-2 text-xl font-bold tracking-tight', colors.text)}>
                      {formatCurrency(item.price)}
                    </p>

                    <div className="text-muted-foreground flex items-center gap-3 text-sm font-medium">
                      <span>{formatMileage(item.mileage)}</span>
                      <span className="text-muted-foreground/50">•</span>
                      <span>{item.transmission}</span>
                    </div>

                    <div className="text-muted-foreground/80 mt-2 flex items-center gap-1 text-sm">
                      <span className="truncate">{item.location}</span>
                    </div>
                  </div>
                </div>
              </Link>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
