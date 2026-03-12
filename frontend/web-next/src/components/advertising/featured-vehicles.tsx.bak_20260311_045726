'use client';

import { useEffect, useMemo, useRef } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { ArrowRight, Car, Megaphone } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useHomepageRotation, useRecordImpression, useRecordClick } from '@/hooks/use-advertising';
import type { RotatedVehicle } from '@/types/advertising';
import { formatPrice } from '@/lib/format';

function FeaturedVehicleCard({
  vehicle,
  placementType,
  priority = false,
}: {
  vehicle: RotatedVehicle;
  placementType: 'FeaturedSpot' | 'PremiumSpot';
  priority?: boolean;
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
              quality={75}
              loading={priority ? 'eager' : 'lazy'}
              priority={priority}
              placeholder="blur"
              blurDataURL="data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNDAwIiBoZWlnaHQ9IjMwMCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48cmVjdCB3aWR0aD0iMTAwJSIgaGVpZ2h0PSIxMDAlIiBmaWxsPSIjZTJlOGYwIi8+PC9zdmc+"
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

// Placeholder card shown when no paid vehicles are active for this placement
function EmptyFeaturedSlot({ placementType }: { placementType: 'FeaturedSpot' | 'PremiumSpot' }) {
  const href = placementType === 'PremiumSpot' ? '/dealers' : '/vender/publicidad';
  const isPremium = placementType === 'PremiumSpot';
  return (
    <Link href={href} className="group block h-full">
      <Card className="flex h-full flex-col overflow-hidden border-2 border-dashed border-slate-200 bg-slate-50/50 transition-all hover:border-emerald-400 hover:shadow-md dark:border-slate-700 dark:bg-slate-800/20">
        <div
          className="flex items-center justify-center bg-slate-50 dark:bg-slate-800/30"
          style={{ aspectRatio: '4/3' }}
        >
          <div
            className={`flex h-20 w-20 items-center justify-center rounded-2xl transition-transform duration-300 group-hover:scale-110 ${
              isPremium
                ? 'bg-gradient-to-br from-purple-100 to-violet-100 dark:from-purple-900/30 dark:to-violet-900/30'
                : 'bg-gradient-to-br from-amber-100 to-orange-100 dark:from-amber-900/30 dark:to-orange-900/30'
            }`}
          >
            <Car
              className={`h-10 w-10 ${
                isPremium
                  ? 'text-purple-400 dark:text-purple-300'
                  : 'text-amber-400 dark:text-amber-300'
              }`}
            />
          </div>
        </div>
        <CardContent className="flex-1 p-4">
          <p className="text-sm font-semibold text-slate-400 transition-colors group-hover:text-emerald-600">
            Sé el primero aquí
          </p>
          <p className="mt-0.5 text-xs text-slate-300">Destaca tu vehículo →</p>
        </CardContent>
      </Card>
    </Link>
  );
}

interface FeaturedVehiclesProps {
  title?: string;
  placementType?: 'FeaturedSpot' | 'PremiumSpot';
  maxItems?: number;
  /** Number of columns in the grid. 4 = bigger/premium section; 3 = standard. */
  columns?: 3 | 4;
}

export default function FeaturedVehicles({
  title = 'Vehículos Destacados',
  placementType = 'FeaturedSpot',
  maxItems = 6,
  columns = 3,
}: FeaturedVehiclesProps) {
  const { data: rotation, isLoading } = useHomepageRotation(placementType);

  const accentColor =
    placementType === 'PremiumSpot'
      ? { price: 'text-purple-600', border: 'border-purple-500 text-purple-600 hover:bg-purple-50' }
      : { price: 'text-amber-600', border: 'border-amber-500 text-amber-600 hover:bg-amber-50' };

  const viewAllHref =
    placementType === 'PremiumSpot' ? '/vehiculos?sortBy=price_desc' : '/vehiculos?sortBy=newest';

  const gridClass =
    columns === 4
      ? 'grid grid-cols-2 gap-4 sm:gap-5 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5'
      : 'grid grid-cols-2 gap-4 sm:gap-5 md:grid-cols-3 lg:grid-cols-3 xl:grid-cols-4';

  // Complies with Ley 358-05 — must disclose paid/sponsored content
  const sponsoredBadgeClass =
    placementType === 'PremiumSpot'
      ? 'mt-1.5 inline-flex items-center gap-1 rounded-full border border-purple-200/60 bg-purple-50/90 px-2.5 py-0.5 text-[11px] font-medium text-purple-700 dark:border-purple-800/60 dark:bg-purple-900/20 dark:text-purple-400'
      : 'mt-1.5 inline-flex items-center gap-1 rounded-full border border-amber-200/60 bg-amber-50/90 px-2.5 py-0.5 text-[11px] font-medium text-amber-700 dark:border-amber-800/60 dark:bg-amber-900/20 dark:text-amber-400';

  if (isLoading) {
    return (
      <section className="py-8">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 2xl:max-w-[1600px]">
          <div className="mb-6 flex items-center justify-between">
            <div>
              <h2 className="text-2xl font-bold">{title}</h2>
              <span
                title="Contenido publicitario pagado por anunciantes"
                aria-label="Espacio publicitario patrocinado"
                className={sponsoredBadgeClass}
              >
                <Megaphone className="h-2.5 w-2.5" />
                Espacio Patrocinado
              </span>
            </div>
          </div>
          <div className={gridClass}>
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

  // Slots to fill to complete the last row
  const fillCount = vehicles.length > 0 ? (columns - (vehicles.length % columns)) % columns : 0;

  // When no paid vehicles are active — show placeholder slots so the section is always visible
  if (!vehicles.length) {
    return (
      <section className="py-8">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 2xl:max-w-[1600px]">
          <div className="mb-6 flex items-center justify-between">
            <div>
              <h2 className="text-foreground text-2xl leading-tight font-bold tracking-tight">
                {title}
              </h2>
              <span
                title="Contenido publicitario pagado por anunciantes"
                aria-label="Espacio publicitario patrocinado"
                className={sponsoredBadgeClass}
              >
                <Megaphone className="h-2.5 w-2.5" />
                Espacio Patrocinado
              </span>
            </div>
            <Link href={viewAllHref}>
              <Button variant="outline" className={`group ${accentColor.border}`}>
                Ver todos
                <ArrowRight className="ml-1 h-4 w-4 transition-transform group-hover:translate-x-1" />
              </Button>
            </Link>
          </div>
          <div className={gridClass}>
            {Array.from({ length: maxItems }).map((_, i) => (
              <EmptyFeaturedSlot key={i} placementType={placementType} />
            ))}
          </div>
        </div>
      </section>
    );
  }

  return (
    <section className="py-8">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 2xl:max-w-[1600px]">
        <div className="mb-6 flex items-center justify-between">
          <div>
            <h2 className="text-foreground text-2xl leading-tight font-bold tracking-tight">
              {title}
            </h2>
            <span
              title="Contenido publicitario pagado por anunciantes"
              aria-label="Espacio publicitario patrocinado"
              className={sponsoredBadgeClass}
            >
              <Megaphone className="h-2.5 w-2.5" />
              Espacio Patrocinado
            </span>
          </div>
          <Link href={viewAllHref}>
            <Button variant="outline" className={`group ${accentColor.border}`}>
              Ver todos
              <ArrowRight className="ml-1 h-4 w-4 transition-transform group-hover:translate-x-1" />
            </Button>
          </Link>
        </div>
        <div className={gridClass}>
          {vehicles.map((vehicle, index) => (
            <FeaturedVehicleCard
              key={vehicle.vehicleId}
              vehicle={vehicle}
              placementType={placementType}
              priority={index < 2}
            />
          ))}
          {fillCount > 0 &&
            Array.from({ length: fillCount }).map((_, i) => (
              <EmptyFeaturedSlot key={`fill-${i}`} placementType={placementType} />
            ))}
        </div>
      </div>
    </section>
  );
}
