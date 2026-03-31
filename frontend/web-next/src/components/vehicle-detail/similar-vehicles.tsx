/**
 * Similar Vehicles Component
 * Shows 4-6 related vehicles based on same segment (body type), ±20% price, ±2 years.
 * Uses TanStack Query for caching, deduplication, and automatic retry.
 * Tracks impressions (IntersectionObserver) and clicks for product analytics.
 */

'use client';

import * as React from 'react';
import Link from 'next/link';
import { ChevronRight } from 'lucide-react';
import { useQuery } from '@tanstack/react-query';
import { VehicleCard, VehicleCardSkeleton } from '@/components/ui/vehicle-card';
import { cn } from '@/lib/utils';
import { vehicleService } from '@/services/vehicles';
import type { VehicleCardData } from '@/types';

interface SimilarVehiclesProps {
  vehicleId: string;
  /** Number of vehicles to show (4-6). Default: 6. */
  limit?: number;
  variant?: 'default' | 'compact';
  className?: string;
}

const MIN_RESULTS = 2; // Show widget with as few as 2 similar vehicles (small inventory)

export function SimilarVehicles({
  vehicleId,
  limit = 6,
  variant = 'default',
  className,
}: SimilarVehiclesProps) {
  const sectionRef = React.useRef<HTMLDivElement>(null);
  const impressionFired = React.useRef(false);

  const { data: vehicles = [], isLoading } = useQuery<VehicleCardData[]>({
    queryKey: ['similar-vehicles', vehicleId, limit],
    queryFn: () => vehicleService.getSimilar(vehicleId, limit),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
    retry: 2,
    enabled: !!vehicleId,
  });

  // ── Analytics: impression tracking via IntersectionObserver ──────────────
  React.useEffect(() => {
    if (vehicles.length < MIN_RESULTS || impressionFired.current) return;
    const el = sectionRef.current;
    if (!el) return;

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting && !impressionFired.current) {
          impressionFired.current = true;
          window.gtag?.('event', 'similar_vehicles_impression', {
            source_vehicle_id: vehicleId,
            count: vehicles.length,
          });
          observer.disconnect();
        }
      },
      { threshold: 0.25 }
    );
    observer.observe(el);
    return () => observer.disconnect();
  }, [vehicles, vehicleId]);

  // ── Analytics: click handler ────────────────────────────────────────────
  const handleSimilarClick = React.useCallback(
    (clickedId: string, position: number) => {
      window.gtag?.('event', 'similar_vehicle_click', {
        source_vehicle_id: vehicleId,
        clicked_vehicle_id: clickedId,
        position,
      });
    },
    [vehicleId]
  );

  if (isLoading) {
    return (
      <div className={className}>
        <div className="mb-4 flex items-center justify-between">
          <h2
            className={cn(
              variant === 'compact'
                ? 'text-sm font-semibold tracking-wide text-gray-700 uppercase'
                : 'text-foreground text-xl font-bold'
            )}
          >
            Vehículos similares
          </h2>
        </div>
        <div
          className={cn(
            variant === 'compact'
              ? 'grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6'
              : 'grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-3'
          )}
        >
          {Array.from({ length: Math.min(limit, 3) }).map((_, i) => (
            <VehicleCardSkeleton key={i} variant={variant} />
          ))}
        </div>
      </div>
    );
  }

  if (vehicles.length < MIN_RESULTS) {
    return null;
  }

  return (
    <div ref={sectionRef} className={className}>
      <div className="mb-4 flex items-center justify-between">
        <h2
          className={cn(
            variant === 'compact'
              ? 'text-sm font-semibold tracking-wide text-gray-700 uppercase'
              : 'text-foreground text-xl font-bold'
          )}
        >
          Vehículos similares
        </h2>
        <Link
          href="/vehiculos"
          className="text-primary flex items-center gap-1 text-sm hover:underline"
        >
          Ver todos
          <ChevronRight className="h-4 w-4" />
        </Link>
      </div>
      <div
        className={cn(
          variant === 'compact'
            ? 'grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6'
            : 'grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-3'
        )}
      >
        {vehicles.map((vehicle, index) => (
          <div key={vehicle.id} onClick={() => handleSimilarClick(vehicle.id, index + 1)}>
            <VehicleCard vehicle={vehicle} variant={variant} />
          </div>
        ))}
      </div>
    </div>
  );
}

export default SimilarVehicles;
