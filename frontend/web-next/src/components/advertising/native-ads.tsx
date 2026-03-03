'use client';

// ============================================================================
// OKLA Native Ad Components
// Sponsored vehicle cards that look like normal listings with subtle badges.
// "Que se vean como publicaciones normales... elegante"
// ============================================================================

import React, { useCallback, useEffect, useRef, useState } from 'react';
import Image from 'next/image';
import Link from 'next/link';
import { Heart, MapPin, Camera, Fuel, Gauge, Settings2, BadgeCheck, Sparkles, TrendingUp } from 'lucide-react';
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
      className: 'bg-gradient-to-r from-violet-50/90 to-purple-50/90 text-purple-700 border-purple-200/60',
    },
  };

  const config = configs[tier];
  const Icon = config.icon;
  const isSmall = size === 'sm';

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 rounded-full border backdrop-blur-sm font-medium',
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
          }).catch(() => {/* silent */});
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
    }).catch(() => {/* silent */});
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
          <Card className="overflow-hidden border-0 shadow-sm hover:shadow-md transition-all duration-300 bg-white">
            <div className="relative aspect-[4/3]">
              <Image
                src={imgError ? fallbackImage : (vehicle.imageUrl || fallbackImage)}
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
              <p className="font-semibold text-sm text-slate-900 truncate">
                {vehicle.year} {vehicle.make} {vehicle.model}
              </p>
              <p className="text-base font-bold text-emerald-600 mt-1">
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
          <Card className="overflow-hidden border-0 shadow-sm hover:shadow-md transition-all duration-300 bg-white">
            <div className="flex">
              <div className="relative w-48 flex-shrink-0">
                <Image
                  src={imgError ? fallbackImage : (vehicle.imageUrl || fallbackImage)}
                  alt={`${vehicle.year} ${vehicle.make} ${vehicle.model}`}
                  fill
                  className="object-cover"
                  onError={() => setImgError(true)}
                />
                <div className="absolute top-2 left-2">
                  <SponsoredBadge tier={vehicle.sponsorTier} />
                </div>
              </div>
              <CardContent className="p-4 flex-1 flex flex-col justify-between">
                <div>
                  <h3 className="font-semibold text-slate-900">
                    {vehicle.year} {vehicle.make} {vehicle.model}
                    {vehicle.trim && <span className="text-slate-500 font-normal ml-1">{vehicle.trim}</span>}
                  </h3>
                  <div className="flex items-center gap-3 mt-2 text-xs text-slate-500">
                    <span className="flex items-center gap-1"><Gauge className="h-3 w-3" />{formatMileage(vehicle.mileage)}</span>
                    <span className="flex items-center gap-1"><Settings2 className="h-3 w-3" />{vehicle.transmission}</span>
                    <span className="flex items-center gap-1"><Fuel className="h-3 w-3" />{vehicle.fuelType}</span>
                  </div>
                </div>
                <div className="flex items-end justify-between mt-2">
                  <div>
                    <p className="text-lg font-bold text-emerald-600">
                      {formatPrice(vehicle.price, vehicle.currency)}
                    </p>
                  </div>
                  <span className="text-xs text-slate-400 flex items-center gap-1">
                    <MapPin className="h-3 w-3" />{vehicle.location}
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
        <Card className="overflow-hidden border-0 shadow-sm hover:shadow-lg transition-all duration-300 bg-white group-hover:-translate-y-0.5">
          {/* Image Container */}
          <div className="relative aspect-[16/10] overflow-hidden">
            <Image
              src={imgError ? fallbackImage : (vehicle.imageUrl || fallbackImage)}
              alt={`${vehicle.year} ${vehicle.make} ${vehicle.model}`}
              fill
              className="object-cover group-hover:scale-105 transition-transform duration-500"
              onError={() => setImgError(true)}
              priority={priority}
            />

            {/* Gradient overlay */}
            <div className="absolute inset-0 bg-gradient-to-t from-black/30 via-transparent to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-300" />

            {/* Top badges row */}
            <div className="absolute top-2.5 left-2.5 right-2.5 flex items-start justify-between">
              <div className="flex items-center gap-1.5">
                <SponsoredBadge tier={vehicle.sponsorTier} />
                {vehicle.isVerified && (
                  <span className="inline-flex items-center gap-0.5 rounded-full bg-emerald-500/90 text-white px-2 py-0.5 text-[10px] font-medium backdrop-blur-sm">
                    <BadgeCheck className="h-2.5 w-2.5" />
                    Verificado
                  </span>
                )}
              </div>
              {showFavoriteButton && (
                <button
                  type="button"
                  onClick={(e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    setIsFavorite(!isFavorite);
                  }}
                  className="rounded-full bg-white/90 p-1.5 shadow-sm backdrop-blur-sm hover:bg-white transition-colors"
                >
                  <Heart
                    className={cn('h-4 w-4 transition-colors', isFavorite ? 'fill-red-500 text-red-500' : 'text-slate-600')}
                  />
                </button>
              )}
            </div>

            {/* Photo count badge */}
            {vehicle.photoCount && vehicle.photoCount > 0 && (
              <div className="absolute bottom-2.5 left-2.5">
                <span className="inline-flex items-center gap-1 rounded-md bg-black/60 text-white px-2 py-0.5 text-[10px] font-medium backdrop-blur-sm">
                  <Camera className="h-2.5 w-2.5" />
                  {vehicle.photoCount}
                </span>
              </div>
            )}

            {/* Dealer badge */}
            {vehicle.dealerName && (
              <div className="absolute bottom-2.5 right-2.5">
                <span className="inline-flex items-center gap-1 rounded-md bg-blue-600/90 text-white px-2 py-0.5 text-[10px] font-medium backdrop-blur-sm">
                  Dealer
                </span>
              </div>
            )}
          </div>

          {/* Card Content */}
          <CardContent className="p-3.5">
            {/* Title */}
            <h3 className="font-semibold text-[15px] text-slate-900 leading-tight truncate">
              {vehicle.year} {vehicle.make} {vehicle.model}
              {vehicle.trim && (
                <span className="text-slate-500 font-normal ml-1 text-sm">{vehicle.trim}</span>
              )}
            </h3>

            {/* Specs row */}
            <div className="flex items-center gap-2.5 mt-2 text-xs text-slate-500">
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
            <div className="flex items-center gap-1 mt-2 text-xs text-slate-500">
              <MapPin className="h-3 w-3 text-slate-400" />
              {vehicle.location}
            </div>

            {/* Price */}
            <div className="flex items-end justify-between mt-3 pt-3 border-t border-slate-100">
              <div>
                <p className="text-lg font-bold text-emerald-600 leading-none">
                  {formatPrice(vehicle.price, vehicle.currency)}
                </p>
                {vehicle.monthlyPayment && (
                  <p className="text-[11px] text-slate-400 mt-0.5">
                    ~{formatPrice(vehicle.monthlyPayment, vehicle.currency)}/mes
                  </p>
                )}
              </div>
              {vehicle.dealerRating && (
                <div className="flex items-center gap-1 text-xs">
                  <span className="text-amber-500">★</span>
                  <span className="text-slate-600 font-medium">{vehicle.dealerRating}</span>
                </div>
              )}
            </div>

            {/* CTA on hover */}
            <div className="mt-3 opacity-0 group-hover:opacity-100 transition-opacity duration-300">
              <div className="w-full py-2 rounded-lg bg-emerald-600 text-white text-center text-sm font-medium hover:bg-emerald-700 transition-colors">
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
        <div className="flex items-center justify-between mb-4">
          <div>
            <h2 className="text-lg font-semibold text-slate-900">{title}</h2>
            {subtitle && <p className="text-sm text-slate-500 mt-0.5">{subtitle}</p>}
          </div>
          <SponsoredBadge tier="sponsored" size="md" />
        </div>
      )}

      {variant === 'scroll' ? (
        <div className="flex gap-4 overflow-x-auto pb-4 -mx-4 px-4 snap-x snap-mandatory scrollbar-hide">
          {vehicles.map((v) => (
            <div key={v.id} className="flex-shrink-0 w-[280px] snap-start">
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
          {vehicles.map((v) => (
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

export function InlineAdSlot({
  vehicles,
  className,
  onImpression,
  onClick,
}: InlineAdSlotProps) {
  if (!vehicles.length) return null;

  // Render a single sponsored card that blends into the grid
  return (
    <>
      {vehicles.map((v) => (
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

export function SidebarAdUnit({
  vehicles,
  className,
  onImpression,
  onClick,
}: SidebarAdUnitProps) {
  if (!vehicles.length) return null;

  return (
    <div className={cn('space-y-3', className)}>
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-medium text-slate-700">Recomendados</h3>
        <SponsoredBadge tier="sponsored" />
      </div>
      {vehicles.map((v) => (
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
          }).catch(() => {/* silent */});
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
        <div className="absolute top-0 right-0 w-64 h-64 bg-white/5 rounded-full -translate-y-1/2 translate-x-1/2" />
        <div className="absolute bottom-0 left-1/2 w-48 h-48 bg-white/5 rounded-full translate-y-1/2" />

        <div className="relative z-10 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
          <div>
            <h3 className="text-xl sm:text-2xl font-bold text-white">{title}</h3>
            <p className="text-sm sm:text-base text-white/80 mt-1 max-w-md">{subtitle}</p>
          </div>
          <Link
            href={ctaUrl}
            className="inline-flex items-center px-6 py-3 rounded-xl bg-white text-emerald-700 font-semibold text-sm hover:bg-white/90 transition-colors shadow-lg shadow-black/10 whitespace-nowrap"
          >
            {ctaText}
          </Link>
        </div>

        {/* Subtle sponsor indicator */}
        <div className="absolute bottom-2 right-3">
          <span className="text-[9px] text-white/40 font-medium">Patrocinado</span>
        </div>
      </div>
    </div>
  );
}
