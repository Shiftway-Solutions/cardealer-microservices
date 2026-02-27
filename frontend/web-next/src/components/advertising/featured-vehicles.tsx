'use client';

import { useEffect, useMemo, useRef } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
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
    <Link
      href={vehicleHref}
      onClick={handleClick}
      className="group block"
    >
      <Card className="overflow-hidden border-0 shadow-md transition-shadow hover:shadow-lg">
        <div className="bg-muted relative aspect-[16/10]">
          {vehicle.imageUrl ? (
            <Image
              src={vehicle.imageUrl}
              alt={vehicle.title || 'Vehículo'}
              fill
              className="object-cover transition-transform duration-300 group-hover:scale-105"
              sizes="(max-width: 768px) 100vw, (max-width: 1200px) 50vw, 33vw"
            />
          ) : (
            <div className="text-muted-foreground flex h-full items-center justify-center">🚗</div>
          )}
          {vehicle.isPremium && (
            <Badge className="absolute top-2 left-2 border-0 bg-gradient-to-r from-amber-500 to-orange-500 text-white">
              💎 Premium
            </Badge>
          )}
          {vehicle.isFeatured && !vehicle.isPremium && (
            <Badge className="absolute top-2 left-2" variant="secondary">
              ⭐ Destacado
            </Badge>
          )}
        </div>
        <CardContent className="p-4">
          <h3 className="group-hover:text-primary line-clamp-1 text-sm font-semibold transition-colors">
            {vehicle.title || 'Vehículo'}
          </h3>
          <p className="text-primary mt-1 text-lg font-bold">
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
  maxItems = 8,
}: FeaturedVehiclesProps) {
  const { data: rotation, isLoading } = useHomepageRotation(placementType);

  if (isLoading) {
    return (
      <section className="py-8">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <h2 className="mb-6 text-2xl font-bold">{title}</h2>
          <div className="grid grid-cols-2 gap-4 md:grid-cols-3 lg:grid-cols-4">
            {Array.from({ length: maxItems }).map((_, i) => (
              <Card key={i} className="animate-pulse">
                <div className="bg-muted aspect-[16/10]" />
                <CardContent className="space-y-2 p-4">
                  <div className="bg-muted h-4 w-3/4 rounded" />
                  <div className="bg-muted h-5 w-1/2 rounded" />
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      </section>
    );
  }

  const vehicles = rotation?.items?.slice(0, maxItems) || [];

  if (vehicles.length === 0) return null;

  return (
    <section className="py-8">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="mb-6 flex items-center justify-between">
          <h2 className="text-2xl font-bold">{title}</h2>
          <Link href="/buscar" className="text-primary text-sm hover:underline">
            Ver todos →
          </Link>
        </div>
        <div className="grid grid-cols-2 gap-4 md:grid-cols-3 lg:grid-cols-4">
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
