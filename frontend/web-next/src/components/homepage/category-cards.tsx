/**
 * Professional Category Cards
 *
 * Visual category navigation with hover effects and gradients
 * Inspired by premium automotive marketplaces
 */

'use client';

import { useState } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { ArrowRight, TrendingUp } from 'lucide-react';
import { cn } from '@/lib/utils';
import { useInView } from '@/hooks/use-in-view';

// =============================================================================
// TYPES
// =============================================================================

export interface CategoryCard {
  id: string;
  name: string;
  slug: string;
  description: string;
  vehicleCount: number;
  imageUrl: string;
  gradient: string;
  trending?: boolean;
}

interface CategoryCardsProps {
  categories?: CategoryCard[];
  className?: string;
}

// =============================================================================
// DEFAULT CATEGORIES
// =============================================================================

export const DEFAULT_CATEGORIES: CategoryCard[] = [
  {
    id: '1',
    name: 'SUV',
    slug: 'suv',
    description: 'Versatilidad y espacio para toda la familia',
    vehicleCount: 1250,
    imageUrl: '', // Use gradient fallback - no image needed
    gradient: 'from-blue-600 to-blue-800',
    trending: true,
  },
  {
    id: '2',
    name: 'Sedán',
    slug: 'sedan',
    description: 'Elegancia y eficiencia para el día a día',
    vehicleCount: 980,
    imageUrl: '', // Use gradient fallback
    gradient: 'from-primary to-primary/90',
  },
  {
    id: '3',
    name: 'Camioneta',
    slug: 'camioneta',
    description: 'Potencia y capacidad de carga',
    vehicleCount: 650,
    imageUrl: '', // Use gradient fallback
    gradient: 'from-amber-600 to-amber-800',
  },
  {
    id: '4',
    name: 'Deportivo',
    slug: 'deportivo',
    description: 'Rendimiento y adrenalina pura',
    vehicleCount: 320,
    imageUrl: '', // Use gradient fallback
    gradient: 'from-red-600 to-red-800',
  },
  {
    id: '5',
    name: 'Eléctrico',
    slug: 'electrico',
    description: 'El futuro de la movilidad sostenible',
    vehicleCount: 180,
    imageUrl: '', // Use gradient fallback
    gradient: 'from-cyan-500 to-blue-600',
    trending: true,
  },
  {
    id: '6',
    name: 'Híbrido',
    slug: 'hibrido',
    description: 'Lo mejor de dos mundos',
    vehicleCount: 420,
    imageUrl: '', // Use gradient fallback
    gradient: 'from-green-500 to-primary/90',
  },
];

// =============================================================================
// SINGLE CATEGORY CARD
// =============================================================================

function SingleCategoryCard({ category, index }: { category: CategoryCard; index: number }) {
  const [isHovered, setIsHovered] = useState(false);
  const [imageError, setImageError] = useState(false);
  const { ref, inView } = useInView({ rootMargin: '-50px' });

  return (
    <div
      ref={ref}
      className={cn(
        'transition-all duration-500',
        inView ? 'translate-y-0 opacity-100' : 'translate-y-5 opacity-0'
      )}
      style={{ transitionDelay: `${index * 100}ms` }}
    >
      <Link
        href={`/vehiculos?bodyType=${category.slug}`}
        className="group block"
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
      >
        <div className="relative h-72 overflow-hidden rounded-2xl shadow-lg transition-all duration-500 hover:shadow-2xl md:h-80">
          {/* Background Image or Gradient */}
          {!imageError && category.imageUrl ? (
            <Image
              src={category.imageUrl}
              alt={category.name}
              fill
              sizes="(max-width: 768px) 100vw, (max-width: 1200px) 50vw, 33vw"
              className={cn(
                'object-cover transition-transform duration-700',
                isHovered ? 'scale-110' : 'scale-100'
              )}
              onError={() => setImageError(true)}
            />
          ) : (
            <div className={cn('absolute inset-0 bg-gradient-to-br', category.gradient)} />
          )}

          {/* Overlay Gradient */}
          <div
            className={cn(
              'absolute inset-0 bg-gradient-to-t from-black/80 via-black/40 to-transparent',
              'transition-opacity duration-500',
              isHovered ? 'opacity-90' : 'opacity-70'
            )}
          />

          {/* Trending Badge */}
          {category.trending && (
            <div className="absolute top-4 left-4">
              <div className="animate-scale-in flex items-center gap-1.5 rounded-full bg-gradient-to-r from-orange-500 to-red-500 px-3 py-1.5 text-xs font-semibold tracking-wide text-white shadow-lg">
                <TrendingUp className="h-3.5 w-3.5" />
                Trending
              </div>
            </div>
          )}

          {/* Content */}
          <div className="absolute inset-0 flex flex-col justify-end p-6">
            {/* Category Name */}
            <h3
              className={cn(
                'mb-2 text-2xl leading-tight font-bold tracking-tight text-white transition-transform duration-300 md:text-3xl',
                isHovered ? '-translate-y-2' : 'translate-y-0'
              )}
            >
              {category.name}
            </h3>

            {/* Description - Shows on hover */}
            <p
              className={cn(
                'mb-3 line-clamp-2 text-sm leading-relaxed text-white/85 transition-all duration-300',
                isHovered ? 'translate-y-0 opacity-100' : 'translate-y-2 opacity-0'
              )}
            >
              {category.description}
            </p>

            {/* Vehicle Count & CTA */}
            <div className="flex items-center justify-between">
              <span className="text-sm font-medium text-white/80">
                {category.vehicleCount.toLocaleString()} vehículos
              </span>

              <div
                className={cn(
                  'text-primary flex items-center gap-2 text-sm font-semibold transition-transform duration-300',
                  isHovered ? 'translate-x-1' : 'translate-x-0'
                )}
              >
                <span>Explorar</span>
                <ArrowRight
                  className={cn(
                    'h-4 w-4 transition-transform duration-300',
                    isHovered ? 'translate-x-1' : ''
                  )}
                />
              </div>
            </div>
          </div>

          {/* Decorative Border Glow on Hover */}
          <div
            className="pointer-events-none absolute inset-0 rounded-2xl transition-shadow duration-300"
            style={{
              boxShadow: isHovered
                ? 'inset 0 0 0 2px rgba(0, 168, 112, 0.5)'
                : 'inset 0 0 0 0px rgba(0, 168, 112, 0)',
            }}
          />
        </div>
      </Link>
    </div>
  );
}

// =============================================================================
// MAIN COMPONENT
// =============================================================================

export function CategoryCards({ categories = DEFAULT_CATEGORIES, className }: CategoryCardsProps) {
  return (
    <div
      className={cn(
        'grid grid-cols-2 gap-4 sm:gap-6 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4',
        className
      )}
    >
      {categories.map((category, index) => (
        <SingleCategoryCard key={category.id} category={category} index={index} />
      ))}
    </div>
  );
}

// =============================================================================
// FEATURED CATEGORY (LARGER)
// =============================================================================

interface FeaturedCategoryProps {
  category: CategoryCard;
  className?: string;
}

export function FeaturedCategory({ category, className }: FeaturedCategoryProps) {
  const [isHovered, setIsHovered] = useState(false);
  const [imageError, setImageError] = useState(false);

  return (
    <Link
      href={`/vehiculos?bodyType=${category.slug}`}
      className={cn('group block', className)}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
    >
      <div className="relative h-96 overflow-hidden rounded-3xl shadow-xl transition-all duration-500 hover:shadow-2xl md:h-[500px]">
        {/* Background */}
        {!imageError && category.imageUrl ? (
          <Image
            src={category.imageUrl}
            alt={category.name}
            fill
            sizes="100vw"
            className={cn(
              'object-cover transition-transform duration-1000',
              isHovered ? 'scale-105' : 'scale-100'
            )}
            onError={() => setImageError(true)}
          />
        ) : (
          <div className={cn('absolute inset-0 bg-gradient-to-br', category.gradient)} />
        )}

        {/* Dark Overlay */}
        <div className="absolute inset-0 bg-gradient-to-r from-black/70 via-black/40 to-transparent" />

        {/* Content */}
        <div className="absolute inset-0 flex max-w-xl flex-col justify-center p-8 md:p-12">
          {category.trending && (
            <div className="mb-4 flex w-fit items-center gap-1.5 rounded-full bg-gradient-to-r from-orange-500 to-red-500 px-3 py-1.5 text-xs font-semibold text-white shadow-lg">
              <TrendingUp className="h-3.5 w-3.5" />
              Lo más buscado
            </div>
          )}

          <h2 className="mb-4 text-4xl font-bold text-white md:text-5xl lg:text-6xl">
            {category.name}
          </h2>

          <p className="mb-6 text-lg text-white/80">{category.description}</p>

          <div className="flex items-center gap-4">
            <button className="bg-primary text-primary-foreground shadow-primary/30 hover:bg-primary/90 flex items-center gap-2 rounded-xl px-6 py-3 font-semibold shadow-lg transition-all duration-200 hover:scale-[1.02] active:scale-[0.98]">
              Explorar {category.vehicleCount.toLocaleString()} vehículos
              <ArrowRight className="h-5 w-5" />
            </button>
          </div>
        </div>
      </div>
    </Link>
  );
}

export default CategoryCards;
