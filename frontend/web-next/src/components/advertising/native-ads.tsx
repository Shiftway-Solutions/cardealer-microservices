'use client';

// ============================================================================
// OKLA Native Ad Components
// Sponsored vehicle cards that use the same design as regular listings.
// "Que se vean como publicaciones normales... elegante"
// ============================================================================

import React, { useCallback, useEffect, useRef } from 'react';
import Link from 'next/link';
import { TrendingUp, Sparkles } from 'lucide-react';
import { cn } from '@/lib/utils';
import { VehicleCard } from '@/components/ui/vehicle-card';
import type { VehicleCardData } from '@/types';
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
      label: 'Anuncio Destacado',
      icon: Sparkles,
      className: 'bg-amber-50/90 text-amber-700 border-amber-200/60',
    },
    premium: {
      label: 'Anuncio Premium',
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
      title="Contenido publicitario pagado por anunciantes"
      aria-label="Publicidad pagada"
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
// Helper: map SponsoredVehicle → VehicleCardData for the shared VehicleCard
// ---------------------------------------------------------------------------

function sponsoredToCardData(vehicle: SponsoredVehicle): VehicleCardData {
  return {
    id: vehicle.id,
    slug: vehicle.slug,
    make: vehicle.make,
    model: vehicle.model,
    year: vehicle.year,
    price: vehicle.price,
    currency: vehicle.currency,
    mileage: vehicle.mileage,
    transmission: vehicle.transmission,
    fuelType: vehicle.fuelType,
    imageUrl: vehicle.imageUrl,
    location: vehicle.location,
    dealerName: vehicle.dealerName,
    dealerRating: vehicle.dealerRating,
    photoCount: vehicle.photoCount,
    isVerified: vehicle.isVerified,
    trim: vehicle.trim,
    monthlyPayment: vehicle.monthlyPayment,
  };
}

// ---------------------------------------------------------------------------
// SponsoredVehicleCard — Uses the same VehicleCard as organic results.
// Ad tracking (impressions + clicks) is handled transparently by the wrapper.
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

  useAdImpression(cardRef, vehicle.impressionToken, onImpression);

  const handleClick = useCallback(() => {
    onClick?.(vehicle);
    // Fire-and-forget click tracking
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

  return (
    <div ref={cardRef} className={cn('relative', className)} onClick={handleClick}>
      <VehicleCard
        vehicle={sponsoredToCardData(vehicle)}
        variant={variant}
        showDealRating={false}
        showFavoriteButton={showFavoriteButton}
        priority={priority}
      />
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

        {/* Sponsor disclosure — visible per Ley 358-05 */}
        <div className="absolute top-3 right-3">
          <span
            title="Contenido publicitario pagado por anunciantes"
            aria-label="Publicidad pagada"
            className="inline-flex items-center gap-1 rounded-full border border-white/30 bg-black/20 px-2 py-0.5 text-[11px] font-medium text-white/80 backdrop-blur-sm"
          >
            <TrendingUp className="h-2.5 w-2.5" />
            Publicidad
          </span>
        </div>

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
      </div>
    </div>
  );
}
