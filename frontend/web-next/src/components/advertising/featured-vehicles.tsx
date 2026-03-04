'use client';

import { useEffect, useMemo, useRef } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { ArrowRight } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useHomepageRotation, useRecordImpression, useRecordClick } from '@/hooks/use-advertising';
import type { RotatedVehicle } from '@/types/advertising';

function formatPrice(price: number, currency: string = 'DOP') {
  if (currency === 'DOP') {
    return `RD$${price.toLocaleString('es-DO')}`;
  }
  return `US$${price.toLocaleString('en-US')}`;
}

function FeaturedVehicleCard({
  vehicle,
  placementType,
}: {
  vehicle: RotatedVehicle;
  placementType: 'FeaturedSpot' | 'PremiumSpot';
}) {
  const impressionRecorded = useRef(false);
  const recordImpression = useRecordImpression();
  const recordClick = useRecordClick();

  // Record impression once when visible
  useEffect(() => {
    if (!impressionRecorded.current && vehicle.campaignId) {
      impressionRecorded.current = true;
      recordImpression.mutate({
        campaignId: vehicle.campaignId,
        vehicleId: vehicle.vehicleId,
        section: placementType,
      });
    }
  }, [vehicle.campaignId, vehicle.vehicleId, placementType, recordImpression]);

  const handleClick = () => {
    if (vehicle.campaignId) {
      recordClick.mutate({
        campaignId: vehicle.campaignId,
        vehicleId: vehicle.vehicleId,
        section: placementType,
      });
    }
  };

  // Build a proper slug when the API doesn't provide one
  const vehicleHref = useMemo(() => {
    if (vehicle.slug) return `/vehiculos/${vehicle.slug}`;
    // Generate slug matching backend format: {year}-{make}-{model}-{shortId8}
    const title = (vehicle.title || '').toLowerCase().replace(/\s+/g, '-');
    const shortId = (vehicle.vehicleId || '').replace(/-/g, '').slice(0, 8).toLowerCase();
    return `/vehiculos/${title}-${shortId}`;
  }, [vehicle]);

  return (
    <Link href={vehicleHref} onClick={handleClick} className="group block h-full">
      <Card className="flex h-full flex-col overflow-hidden border-0 shadow-md transition-all hover:-translate-y-0.5 hover:shadow-xl">
        {/* Larger aspect ratio than regular cards — "bigger than all others" */}
        <div className="bg-muted relative" style={{ aspectRatio: '4/3' }}>
          {vehicle.imageUrl ? (
            <Image
              src={vehicle.imageUrl}
              alt={vehicle.title || 'Vehículo'}
              fill
              className="object-cover transition-transform duration-300 group-hover:scale-105"
              sizes="(max-width: 768px) 100vw, (max-width: 1200px) 50vw, 33vw"
            />
          ) : (
            <div className="flex h-full items-center justify-center bg-slate-100 dark:bg-slate-800">
              <span className="text-5xl text-slate-300">🚗</span>
            </div>
          )}
          {vehicle.isPremium && (
            <Badge className="absolute top-2 left-2 border-0 bg-gradient-to-r from-purple-500 to-pink-500 text-white">
              💎 Premium
            </Badge>
          )}
          {vehicle.isFeatured && !vehicle.isPremium && (
            <Badge className="absolute top-2 left-2 border-0 bg-amber-500 text-white">
              ⭐ Destacado
            </Badge>
          )}
          {/* Sponsored disclosure */}
          <span className="absolute right-2 bottom-2 rounded-full bg-black/50 px-2 py-0.5 text-[10px] text-white/80 backdrop-blur-sm">
            Patrocinado
          </span>
        </div>
        <CardContent className="p-4">
          <h3 className="group-hover:text-primary line-clamp-1 text-base font-semibold transition-colors">
            {vehicle.title || 'Vehículo'}
          </h3>
          <p className="text-primary mt-1 text-xl font-bold">
            {vehicle.price ? formatPrice(vehicle.price, vehicle.currency) : ''}
          </p>
          {vehicle.location && (
            <p className="text-muted-foreground mt-1 text-xs">📍 {vehicle.location}</p>
          )}
        </CardContent>
      </Card>
    </Link>
  );
}

interface FeaturedVehiclesProps {
  title?: string;
  placementType?: 'FeaturedSpot' | 'PremiumSpot';
  maxItems?: number;
}

export default function FeaturedVehicles({
  title = 'Vehículos Destacados',
  placementType = 'FeaturedSpot',
  maxItems = 6,
}: FeaturedVehiclesProps) {
  const { data: rotation, isLoading } = useHomepageRotation(placementType);

  const accentColor =
    placementType === 'PremiumSpot'
      ? { price: 'text-purple-600', border: 'border-purple-500 text-purple-600 hover:bg-purple-50' }
      : { price: 'text-amber-600', border: 'border-amber-500 text-amber-600 hover:bg-amber-50' };

  const viewAllHref =
    placementType === 'PremiumSpot' ? '/vehiculos?sortBy=price_desc' : '/vehiculos?sortBy=newest';

  if (isLoading) {
    return (
      <section className="py-8">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="mb-6 flex items-center justify-between">
            <h2 className="text-2xl font-bold">{title}</h2>
          </div>
          {/* 3-col grid for bigger cards */}
          <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {Array.from({ length: maxItems }).map((_, i) => (
              <Card key={i} className="animate-pulse overflow-hidden border-0 shadow-md">
                <div className="bg-muted" style={{ aspectRatio: '4/3' }} />
                <CardContent className="space-y-2 p-4">
                  <div className="bg-muted h-4 w-3/4 rounded" />
                  <div className="bg-muted h-6 w-1/2 rounded" />
                  <div className="bg-muted h-3 w-2/3 rounded" />
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      </section>
    );
  }

  // Only show vehicles that have been enriched (have title + image + price)
  const vehicles = (rotation?.items || [])
    .filter(v => v.title && v.imageUrl && v.price)
    .slice(0, maxItems);

  // Hide the entire section if there are no real vehicles
  if (!vehicles.length) return null;

  return (
    <section className="py-8">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="mb-6 flex items-center justify-between">
          <h2 className="text-foreground text-2xl leading-tight font-bold tracking-tight">
            {title}
          </h2>
          <Link href={viewAllHref}>
            <Button variant="outline" className={`group ${accentColor.border}`}>
              Ver todos
              <ArrowRight className="ml-1 h-4 w-4 transition-transform group-hover:translate-x-1" />
            </Button>
          </Link>
        </div>
        {/* 3-col grid: cards bigger than the 4-col VehicleTypeSection grids */}
        <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
          {vehicles.map(vehicle => (
            <FeaturedVehicleCard
              key={vehicle.vehicleId}
              vehicle={vehicle}
              placementType={placementType}
            />
          ))}
        </div>
      </div>
    </section>
  );
}
