'use client';

// ============================================================================
// OKLA Native Ad Components
// Sponsored vehicle cards that look like normal listings with subtle badges.
// "Que se vean como publicaciones normales... elegante"
// ============================================================================

import React, { useCallback, useEffect, useRef, useState } from 'react';
import Image from 'next/image';
import Link from 'next/link';
import {
  Heart,
  MapPin,
  Camera,
  Fuel,
  Gauge,
  Settings2,
  BadgeCheck,
  Sparkles,
  TrendingUp,
} from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { cn } from '@/lib/utils';
import type { SponsoredVehicle, AdSlotPosition } from '@/types/ads';

// ---------------------------------------------------------------------------
// Sponsored Badge — Subtle "Patrocinado" indicator
// ---------------------------------------------------------------------------

interface SponsoredBadgeProps {
  tier: 'sponsored' | 'featured' | 'premium';
  className?: string;
  size?: 'sm' | 'md';
}

export function SponsoredBadge({ tier, className, size = 'sm' }: SponsoredBadgeProps) {
  const configs = {
    sponsored: {
      label: 'Patrocinado',
      icon: TrendingUp,
      className: 'bg-slate-100/90 text-slate-600 border-slate-200/60',
    },
    featured: {
      label: 'Destacado',
      icon: Sparkles,
      className: 'bg-amber-50/90 text-amber-700 border-amber-200/60',
    },
    premium: {
      label: 'Premium',
      icon: Sparkles,
      className:
        'bg-gradient-to-r from-violet-50/90 to-purple-50/90 text-purple-700 border-purple-200/60',
    },
  };

  const config = configs[tier];
  const Icon = config.icon;
  const isSmall = size === 'sm';

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 rounded-full border font-medium backdrop-blur-sm',
        isSmall ? 'px-2 py-0.5 text-[10px]' : 'px-2.5 py-1 text-xs',
        config.className,
        className
      )}
    >
      <Icon className={isSmall ? 'h-2.5 w-2.5' : 'h-3 w-3'} />
      {config.label}
    </span>
  );
}

// ---------------------------------------------------------------------------
// Ad Impression Tracker (lightweight)
// ---------------------------------------------------------------------------

function useAdImpression(
  ref: React.RefObject<HTMLElement | null>,
  impressionToken: string,
  onImpression?: (token: string) => void
) {
  const tracked = useRef(false);

  useEffect(() => {
    if (!ref.current || tracked.current) return;

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting && !tracked.current) {
          tracked.current = true;
          onImpression?.(impressionToken);
          // Fire-and-forget impression
          fetch('/api/advertising/tracking/impression', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ impressionToken, timestamp: Date.now() }),
          }).catch(() => {
            /* silent */
          });
          observer.disconnect();
        }
      },
      { threshold: 0.5 }
    );

    observer.observe(ref.current);
    return () => observer.disconnect();
  }, [ref, impressionToken, onImpression]);
}

// ---------------------------------------------------------------------------
// SponsoredVehicleCard — Native ad that looks like a normal vehicle card
// ---------------------------------------------------------------------------

interface SponsoredVehicleCardProps {
  vehicle: SponsoredVehicle;
  variant?: 'default' | 'compact' | 'horizontal';
  className?: string;
  onImpression?: (token: string) => void;
  onClick?: (vehicle: SponsoredVehicle) => void;
  priority?: boolean;
  showFavoriteButton?: boolean;
}

export function SponsoredVehicleCard({
  vehicle,
  variant = 'default',
  className,
  onImpression,
  onClick,
  priority = false,
  showFavoriteButton = true,
}: SponsoredVehicleCardProps) {
  const cardRef = useRef<HTMLDivElement>(null);
  const [isFavorite, setIsFavorite] = useState(false);
  const [imgError, setImgError] = useState(false);

  useAdImpression(cardRef, vehicle.impressionToken, onImpression);

  const handleClick = useCallback(() => {
    onClick?.(vehicle);
    // Track click
    fetch(vehicle.clickTrackingUrl, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        vehicleId: vehicle.id,
        campaignId: vehicle.campaignId,
        position: vehicle.auctionPosition,
        timestamp: Date.now(),
      }),
    }).catch(() => {
      /* silent */
    });
  }, [vehicle, onClick]);

  const formatPrice = (price: number, currency: string) => {
    if (currency === 'USD') {
      return `US$${price.toLocaleString('en-US')}`;
    }
    return `RD$${price.toLocaleString('es-DO')}`;
  };

  const formatMileage = (km: number) => {
    if (km >= 1000) return `${(km / 1000).toFixed(km >= 10000 ? 0 : 1)}k km`;
    return `${km.toLocaleString()} km`;
  };

  const vehicleUrl = `/vehiculos/${vehicle.slug}`;
  const fallbackImage = '/images/vehicle-placeholder.svg';

  if (variant === 'compact') {
    return (
      <div ref={cardRef} className={cn('group', className)}>
        <Link href={vehicleUrl} onClick={handleClick} className="block">
          <Card className="overflow-hidden border-0 bg-white shadow-sm transition-all duration-300 hover:shadow-md">
            <div className="relative aspect-[4/3]">
              <Image
                src={imgError ? fallbackImage : vehicle.imageUrl || fallbackImage}
                alt={`${vehicle.year} ${vehicle.make} ${vehicle.model}`}
                fill
                className="object-cover"
                onError={() => setImgError(true)}
                priority={priority}
              />
              <div className="absolute top-2 left-2">
                <SponsoredBadge tier={vehicle.sponsorTier} />
              </div>
            </div>
            <CardContent className="p-3">
              <p className="truncate text-sm font-semibold text-slate-900">
                {vehicle.year} {vehicle.make} {vehicle.model}
              </p>
              <p className="mt-1 text-base font-bold text-emerald-600">
                {formatPrice(vehicle.price, vehicle.currency)}
              </p>
            </CardContent>
          </Card>
        </Link>
      </div>
    );
  }

  if (variant === 'horizontal') {
    return (
      <div ref={cardRef} className={cn('group', className)}>
        <Link href={vehicleUrl} onClick={handleClick} className="block">
          <Card className="overflow-hidden border-0 bg-white shadow-sm transition-all duration-300 hover:shadow-md">
            <div className="flex">
              <div className="relative w-48 flex-shrink-0">
                <Image
                  src={imgError ? fallbackImage : vehicle.imageUrl || fallbackImage}
                  alt={`${vehicle.year} ${vehicle.make} ${vehicle.model}`}
                  fill
                  className="object-cover"
                  onError={() => setImgError(true)}
                />
                <div className="absolute top-2 left-2">
                  <SponsoredBadge tier={vehicle.sponsorTier} />
                </div>
              </div>
              <CardContent className="flex flex-1 flex-col justify-between p-4">
                <div>
                  <h3 className="font-semibold text-slate-900">
                    {vehicle.year} {vehicle.make} {vehicle.model}
                    {vehicle.trim && (
                      <span className="ml-1 font-normal text-slate-500">{vehicle.trim}</span>
                    )}
                  </h3>
                  <div className="mt-2 flex items-center gap-3 text-xs text-slate-500">
                    <span className="flex items-center gap-1">
                      <Gauge className="h-3 w-3" />
                      {formatMileage(vehicle.mileage)}
                    </span>
                    <span className="flex items-center gap-1">
                      <Settings2 className="h-3 w-3" />
                      {vehicle.transmission}
                    </span>
                    <span className="flex items-center gap-1">
                      <Fuel className="h-3 w-3" />
                      {vehicle.fuelType}
                    </span>
                  </div>
                </div>
                <div className="mt-2 flex items-end justify-between">
                  <div>
                    <p className="text-lg font-bold text-emerald-600">
                      {formatPrice(vehicle.price, vehicle.currency)}
                    </p>
                  </div>
                  <span className="flex items-center gap-1 text-xs text-slate-400">
                    <MapPin className="h-3 w-3" />
                    {vehicle.location}
                  </span>
                </div>
              </CardContent>
            </div>
          </Card>
        </Link>
      </div>
    );
  }

  // Default variant — matches the existing VehicleCard design exactly
  return (
    <div ref={cardRef} className={cn('group', className)}>
      <Link href={vehicleUrl} onClick={handleClick} className="block">
        <Card className="overflow-hidden border-0 bg-white shadow-sm transition-all duration-300 group-hover:-translate-y-0.5 hover:shadow-lg">
          {/* Image Container */}
          <div className="relative aspect-[16/10] overflow-hidden">
            <Image
              src={imgError ? fallbackImage : vehicle.imageUrl || fallbackImage}
              alt={`${vehicle.year} ${vehicle.make} ${vehicle.model}`}
              fill
              className="object-cover transition-transform duration-500 group-hover:scale-105"
              onError={() => setImgError(true)}
              priority={priority}
            />

            {/* Gradient overlay */}
            <div className="absolute inset-0 bg-gradient-to-t from-black/30 via-transparent to-transparent opacity-0 transition-opacity duration-300 group-hover:opacity-100" />

            {/* Top badges row */}
            <div className="absolute top-2.5 right-2.5 left-2.5 flex items-start justify-between">
              <div className="flex items-center gap-1.5">
                <SponsoredBadge tier={vehicle.sponsorTier} />
                {vehicle.isVerified && (
                  <span className="inline-flex items-center gap-0.5 rounded-full bg-emerald-500/90 px-2 py-0.5 text-[10px] font-medium text-white backdrop-blur-sm">
                    <BadgeCheck className="h-2.5 w-2.5" />
                    Verificado
                  </span>
                )}
              </div>
              {showFavoriteButton && (
                <button
                  type="button"
                  onClick={e => {
                    e.preventDefault();
                    e.stopPropagation();
                    setIsFavorite(!isFavorite);
                  }}
                  className="rounded-full bg-white/90 p-1.5 shadow-sm backdrop-blur-sm transition-colors hover:bg-white"
                >
                  <Heart
                    className={cn(
                      'h-4 w-4 transition-colors',
                      isFavorite ? 'fill-red-500 text-red-500' : 'text-slate-600'
                    )}
                  />
                </button>
              )}
            </div>

            {/* Photo count badge */}
            {vehicle.photoCount && vehicle.photoCount > 0 && (
              <div className="absolute bottom-2.5 left-2.5">
                <span className="inline-flex items-center gap-1 rounded-md bg-black/60 px-2 py-0.5 text-[10px] font-medium text-white backdrop-blur-sm">
                  <Camera className="h-2.5 w-2.5" />
                  {vehicle.photoCount}
                </span>
              </div>
            )}

            {/* Dealer badge */}
            {vehicle.dealerName && (
              <div className="absolute right-2.5 bottom-2.5">
                <span className="inline-flex items-center gap-1 rounded-md bg-blue-600/90 px-2 py-0.5 text-[10px] font-medium text-white backdrop-blur-sm">
                  Dealer
                </span>
              </div>
            )}
          </div>

          {/* Card Content */}
          <CardContent className="p-3.5">
            {/* Title */}
            <h3 className="truncate text-[15px] leading-tight font-semibold text-slate-900">
              {vehicle.year} {vehicle.make} {vehicle.model}
              {vehicle.trim && (
                <span className="ml-1 text-sm font-normal text-slate-500">{vehicle.trim}</span>
              )}
            </h3>

            {/* Specs row */}
            <div className="mt-2 flex items-center gap-2.5 text-xs text-slate-500">
              <span className="flex items-center gap-1">
                <Gauge className="h-3.5 w-3.5 text-slate-400" />
                {formatMileage(vehicle.mileage)}
              </span>
              <span className="text-slate-300">·</span>
              <span className="flex items-center gap-1">
                <Settings2 className="h-3.5 w-3.5 text-slate-400" />
                {vehicle.transmission}
              </span>
              <span className="text-slate-300">·</span>
              <span className="flex items-center gap-1">
                <Fuel className="h-3.5 w-3.5 text-slate-400" />
                {vehicle.fuelType}
              </span>
            </div>

            {/* Location */}
            <div className="mt-2 flex items-center gap-1 text-xs text-slate-500">
              <MapPin className="h-3 w-3 text-slate-400" />
              {vehicle.location}
            </div>

            {/* Price */}
            <div className="mt-3 flex items-end justify-between border-t border-slate-100 pt-3">
              <div>
                <p className="text-lg leading-none font-bold text-emerald-600">
                  {formatPrice(vehicle.price, vehicle.currency)}
                </p>
                {vehicle.monthlyPayment && (
                  <p className="mt-0.5 text-[11px] text-slate-400">
                    ~{formatPrice(vehicle.monthlyPayment, vehicle.currency)}/mes
                  </p>
                )}
              </div>
              {vehicle.dealerRating && (
                <div className="flex items-center gap-1 text-xs">
                  <span className="text-amber-500">★</span>
                  <span className="font-medium text-slate-600">{vehicle.dealerRating}</span>
                </div>
              )}
            </div>

            {/* CTA on hover */}
            <div className="mt-3 opacity-0 transition-opacity duration-300 group-hover:opacity-100">
              <div className="w-full rounded-lg bg-emerald-600 py-2 text-center text-sm font-medium text-white transition-colors hover:bg-emerald-700">
                Contactar vendedor
              </div>
            </div>
          </CardContent>
        </Card>
      </Link>
    </div>
  );
}

// ---------------------------------------------------------------------------
// SponsoredSection — A section of sponsored vehicles with header
// ---------------------------------------------------------------------------

interface SponsoredSectionProps {
  title?: string;
  subtitle?: string;
  vehicles: SponsoredVehicle[];
  variant?: 'grid' | 'scroll' | 'inline';
  columns?: 2 | 3 | 4;
  cardVariant?: 'default' | 'compact';
  className?: string;
  showHeader?: boolean;
  onImpression?: (token: string) => void;
  onClick?: (vehicle: SponsoredVehicle) => void;
}

export function SponsoredSection({
  title,
  subtitle,
  vehicles,
  variant = 'grid',
  columns = 4,
  cardVariant = 'default',
  className,
  showHeader = true,
  onImpression,
  onClick,
}: SponsoredSectionProps) {
  if (!vehicles.length) return null;

  const gridCols = {
    2: 'grid-cols-1 sm:grid-cols-2',
    3: 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-3',
    4: 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4',
  };

  return (
    <section className={cn('relative', className)}>
      {showHeader && title && (
        <div className="mb-4 flex items-center justify-between">
          <div>
            <h2 className="text-lg font-semibold text-slate-900">{title}</h2>
            {subtitle && <p className="mt-0.5 text-sm text-slate-500">{subtitle}</p>}
          </div>
          <SponsoredBadge tier="sponsored" size="md" />
        </div>
      )}

      {variant === 'scroll' ? (
        <div className="scrollbar-hide -mx-4 flex snap-x snap-mandatory gap-4 overflow-x-auto px-4 pb-4">
          {vehicles.map(v => (
            <div key={v.id} className="w-[280px] flex-shrink-0 snap-start">
              <SponsoredVehicleCard
                vehicle={v}
                variant={cardVariant}
                onImpression={onImpression}
                onClick={onClick}
              />
            </div>
          ))}
        </div>
      ) : variant === 'inline' ? (
        <div className="flex flex-col gap-3">
          {vehicles.map(v => (
            <SponsoredVehicleCard
              key={v.id}
              vehicle={v}
              variant="horizontal"
              onImpression={onImpression}
              onClick={onClick}
            />
          ))}
        </div>
      ) : (
        <div className={cn('grid gap-4', gridCols[columns])}>
          {vehicles.map((v, i) => (
            <SponsoredVehicleCard
              key={v.id}
              vehicle={v}
              variant={cardVariant}
              priority={i < 2}
              onImpression={onImpression}
              onClick={onClick}
            />
          ))}
        </div>
      )}
    </section>
  );
}

// ---------------------------------------------------------------------------
// InlineAdSlot — Blends into search results grid
// ---------------------------------------------------------------------------

interface InlineAdSlotProps {
  position: AdSlotPosition;
  vehicles: SponsoredVehicle[];
  className?: string;
  onImpression?: (token: string) => void;
  onClick?: (vehicle: SponsoredVehicle) => void;
}

export function InlineAdSlot({ vehicles, className, onImpression, onClick }: InlineAdSlotProps) {
  if (!vehicles.length) return null;

  // Render a single sponsored card that blends into the grid
  return (
    <>
      {vehicles.map(v => (
        <SponsoredVehicleCard
          key={v.id}
          vehicle={v}
          variant="default"
          className={className}
          onImpression={onImpression}
          onClick={onClick}
        />
      ))}
    </>
  );
}

// ---------------------------------------------------------------------------
// SidebarAdUnit — For sidebar placement in search results
// ---------------------------------------------------------------------------

interface SidebarAdUnitProps {
  vehicles: SponsoredVehicle[];
  className?: string;
  onImpression?: (token: string) => void;
  onClick?: (vehicle: SponsoredVehicle) => void;
}

export function SidebarAdUnit({ vehicles, className, onImpression, onClick }: SidebarAdUnitProps) {
  if (!vehicles.length) return null;

  return (
    <div className={cn('space-y-3', className)}>
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-medium text-slate-700">Recomendados</h3>
        <SponsoredBadge tier="sponsored" />
      </div>
      {vehicles.map(v => (
        <SponsoredVehicleCard
          key={v.id}
          vehicle={v}
          variant="compact"
          onImpression={onImpression}
          onClick={onClick}
          showFavoriteButton={false}
        />
      ))}
    </div>
  );
}

// ---------------------------------------------------------------------------
// NativeBannerAd — Elegant banner that blends with content
// ---------------------------------------------------------------------------

interface NativeBannerAdProps {
  title: string;
  subtitle: string;
  ctaText: string;
  ctaUrl: string;
  backgroundGradient?: string;
  imageUrl?: string;
  className?: string;
  impressionToken?: string;
}

export function NativeBannerAd({
  title,
  subtitle,
  ctaText,
  ctaUrl,
  backgroundGradient = 'from-emerald-600 to-teal-700',
  className,
  impressionToken,
}: NativeBannerAdProps) {
  const bannerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!bannerRef.current || !impressionToken) return;

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) {
          fetch('/api/advertising/tracking/impression', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ impressionToken, type: 'banner', timestamp: Date.now() }),
          }).catch(() => {
            /* silent */
          });
          observer.disconnect();
        }
      },
      { threshold: 0.5 }
    );

    observer.observe(bannerRef.current);
    return () => observer.disconnect();
  }, [impressionToken]);

  return (
    <div ref={bannerRef} className={cn('relative overflow-hidden rounded-2xl', className)}>
      <div className={cn('bg-gradient-to-r p-6 sm:p-8', backgroundGradient)}>
        {/* Decorative elements */}
        <div className="absolute top-0 right-0 h-64 w-64 translate-x-1/2 -translate-y-1/2 rounded-full bg-white/5" />
        <div className="absolute bottom-0 left-1/2 h-48 w-48 translate-y-1/2 rounded-full bg-white/5" />

        <div className="relative z-10 flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-center">
          <div>
            <h3 className="text-xl font-bold text-white sm:text-2xl">{title}</h3>
            <p className="mt-1 max-w-md text-sm text-white/80 sm:text-base">{subtitle}</p>
          </div>
          <Link
            href={ctaUrl}
            className="inline-flex items-center rounded-xl bg-white px-6 py-3 text-sm font-semibold whitespace-nowrap text-emerald-700 shadow-lg shadow-black/10 transition-colors hover:bg-white/90"
          >
            {ctaText}
          </Link>
        </div>

        {/* Subtle sponsor indicator */}
        <div className="absolute right-3 bottom-2">
          <span className="text-[9px] font-medium text-white/40">Patrocinado</span>
        </div>
      </div>
    </div>
  );
}
