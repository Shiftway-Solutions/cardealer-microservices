/**
 * FeaturedListingGrid Component
 *
 * Grid layout for featured vehicle listings
 * Used on homepage for "Destacados" section
 */

'use client';

import Link from 'next/link';
import Image from 'next/image';
import { MapPin, Gauge, Calendar, Heart } from 'lucide-react';
import { useState } from 'react';
import { cn, formatCurrency, formatMileage } from '@/lib/utils';
import { Badge } from '@/components/ui/badge';
import type { Vehicle } from '@/services/homepage-sections';

interface FeaturedListingGridProps {
  vehicles: Vehicle[];
  columns?: 2 | 3 | 4;
  maxItems?: number;
  className?: string;
}

const columnClasses = {
  2: 'grid-cols-1 md:grid-cols-2',
  3: 'grid-cols-1 md:grid-cols-2 lg:grid-cols-3',
  4: 'grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4',
};

export default function FeaturedListingGrid({
  vehicles,
  columns = 3,
  maxItems,
  className = '',
}: FeaturedListingGridProps) {
  const displayVehicles = maxItems ? vehicles.slice(0, maxItems) : vehicles;

  // Generate vehicle URL (mirrors backend GenerateSlug: {year}-{make}-{model}-{shortId8})
  const generateVehicleUrl = (vehicle: Vehicle) => {
    const slug = `${vehicle.year}-${vehicle.make}-${vehicle.model}`
      .toLowerCase()
      .replace(/\s+/g, '-');
    const shortId = (vehicle.id || '').replace(/-/g, '').slice(0, 8).toLowerCase();
    return `/vehiculos/${slug}-${shortId}`;
  };

  return (
    <div className={cn(`grid ${columnClasses[columns]} gap-4 md:gap-6`, className)}>
      {displayVehicles.map((vehicle, index) => (
        <FeaturedListingCard
          key={vehicle.id}
          vehicle={vehicle}
          vehicleUrl={generateVehicleUrl(vehicle)}
          priority={index < 4}
          index={index}
        />
      ))}
    </div>
  );
}

// Card component
interface FeaturedListingCardProps {
  vehicle: Vehicle;
  vehicleUrl: string;
  priority?: boolean;
  index: number;
}

function FeaturedListingCard({
  vehicle,
  vehicleUrl,
  priority = false,
  index,
}: FeaturedListingCardProps) {
  const [isFavorite, setIsFavorite] = useState(false);

  // Render badge based on tier
  const renderBadge = () => {
    if (!vehicle.tier || vehicle.tier === 'basic') return null;

    switch (vehicle.tier) {
      case 'enterprise':
        return (
          <Badge className="border-0 bg-gradient-to-r from-amber-500 to-orange-600 text-xs text-white">
            Top Dealer
          </Badge>
        );
      case 'premium':
        return (
          <Badge className="border-0 bg-gradient-to-r from-purple-500 to-indigo-600 text-xs text-white">
            Premium
          </Badge>
        );
      case 'featured':
        return (
          <Badge className="from-primary border-0 bg-gradient-to-r to-teal-600 text-xs text-white">
            Destacado
          </Badge>
        );
      default:
        return null;
    }
  };

  return (
    <div className="animate-slide-up" style={{ animationDelay: `${index * 50}ms` }}>
      <div className="border-border bg-card overflow-hidden rounded-xl border shadow-md transition-shadow duration-300 hover:shadow-xl">
        <Link href={vehicleUrl} className="group block">
          {/* Image Container */}
          <div className="relative aspect-[4/3] overflow-hidden">
            <Image
              src={vehicle.images[0] || '/placeholder-car.jpg'}
              alt={`${vehicle.year} ${vehicle.make} ${vehicle.model}`}
              fill
              sizes="(max-width: 640px) 100vw, (max-width: 1024px) 50vw, 33vw"
              className="object-cover transition-transform duration-300 group-hover:scale-105"
              priority={priority}
            />

            {/* Badge Overlay - Top Right */}
            {renderBadge() && (
              <div className="absolute top-2 right-2 sm:top-3 sm:right-3">{renderBadge()}</div>
            )}

            {/* Favorite Button - Top Left */}
            <button
              onClick={e => {
                e.preventDefault();
                setIsFavorite(!isFavorite);
              }}
              className="absolute top-2 left-2 touch-manipulation rounded-full bg-white/90 p-1.5 shadow-md transition-colors hover:bg-white active:bg-white sm:top-3 sm:left-3 sm:p-2"
              aria-label={isFavorite ? 'Remove from favorites' : 'Add to favorites'}
            >
              <Heart
                size={18}
                className="sm:h-5 sm:w-5"
                fill={isFavorite ? '#ef4444' : 'none'}
                stroke={isFavorite ? '#ef4444' : '#6b7280'}
              />
            </button>

            {/* Condition Badge - Bottom Left */}
            {vehicle.condition === 'New' && (
              <div className="absolute bottom-2 left-2 sm:bottom-3 sm:left-3">
                <span className="bg-primary rounded-full px-2 py-1 text-xs font-semibold text-white sm:px-3">
                  Nuevo
                </span>
              </div>
            )}
            {vehicle.condition === 'Certified Pre-Owned' && (
              <div className="absolute bottom-2 left-2 sm:bottom-3 sm:left-3">
                <span className="bg-primary rounded-full px-2 py-1 text-xs font-semibold text-white sm:px-3">
                  Certificado
                </span>
              </div>
            )}
          </div>

          {/* Content */}
          <div className="p-3 sm:p-4">
            {/* Title & Price */}
            <div className="mb-2 sm:mb-3">
              <h3 className="group-hover:text-primary text-foreground line-clamp-1 text-base leading-snug font-bold tracking-tight transition-colors sm:text-lg">
                {vehicle.year} {vehicle.make} {vehicle.model}
              </h3>
              <p className="text-primary mt-1 text-xl font-bold tracking-tight sm:text-2xl">
                {formatCurrency(vehicle.price)}
              </p>
            </div>

            {/* Key Details */}
            <div className="text-muted-foreground grid grid-cols-3 gap-2 text-xs sm:gap-3 sm:text-sm">
              <div className="flex items-center gap-1">
                <Gauge size={14} className="text-muted-foreground/70 flex-shrink-0 sm:h-4 sm:w-4" />
                <span className="truncate font-medium">{formatMileage(vehicle.mileage)}</span>
              </div>
              <div className="flex items-center gap-1">
                <Calendar
                  size={14}
                  className="text-muted-foreground/70 flex-shrink-0 sm:h-4 sm:w-4"
                />
                <span className="font-medium">{vehicle.year}</span>
              </div>
              <div className="flex items-center gap-1">
                <MapPin
                  size={14}
                  className="text-muted-foreground/70 flex-shrink-0 sm:h-4 sm:w-4"
                />
                <span className="truncate font-medium">
                  {vehicle.location?.split(',')[0] || 'RD'}
                </span>
              </div>
            </div>
          </div>
        </Link>
      </div>
    </div>
  );
}
