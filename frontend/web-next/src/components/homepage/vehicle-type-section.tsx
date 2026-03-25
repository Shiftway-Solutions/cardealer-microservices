/**
 * VehicleTypeSection Component
 *
 * Fetches and displays a vehicle grid section filtered by body style.
 * Uses the same large-card layout as the paid FeaturedVehicles section
 * so all homepage sections have a consistent look and feel.
 *
 * Admin-managed: section visibility and vehicle assignment are controlled
 * via the admin portal → VehiclesSaleService homepage sections.
 */

'use client';

import type { ReactNode } from 'react';
import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import Link from 'next/link';
import Image from 'next/image';
import { ArrowRight, Car, Megaphone } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { apiClient } from '@/lib/api-client';
import { getVehicleFallbackImage } from '@/lib/vehicle-image-fallbacks';
import { formatPrice } from '@/lib/format';
import { normalizeLocationName } from '@/lib/utils';
import { useResponsiveMaxItems } from '@/hooks/use-responsive-max-items';

// ─────────────────────────────────────────────
// Types
// ─────────────────────────────────────────────

interface VehicleItem {
  id: string;
  title: string;
  make: string;
  model: string;
  year: number;
  price: number;
  currency: string;
  mileage: number;
  mileageUnit: string;
  bodyStyle: string;
  fuelType: string;
  transmission: string;
  city: string;
  state: string;
  isFeatured: boolean;
  isPremium: boolean;
  images: Array<{ url: string; sortOrder: number; isPrimary: boolean }>;
}

interface VehiclesApiResponse {
  vehicles: VehicleItem[];
  totalCount: number;
}

export interface VehicleTypeSectionConfig {
  /** API filter type — 'bodyStyle' for body type, 'fuelType' for hybrid/electric */
  filterType?: 'bodyStyle' | 'fuelType';
  /** API filter value: e.g. "SUV", "Sedan", "Hybrid", "Electric" */
  filterValue: string;
  title: string;
  subtitle?: string;
  icon?: ReactNode;
  viewAllHref: string; // e.g. /vehiculos?bodyType=suv
  accentColor?: string; // Tailwind color token: "blue", "emerald", "amber", etc.
}

// ─────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────

function buildVehicleSlug(vehicle: VehicleItem) {
  const slug = `${vehicle.year}-${vehicle.make}-${vehicle.model}`
    .toLowerCase()
    .replace(/\s+/g, '-');
  const shortId = vehicle.id.replace(/-/g, '').slice(0, 8).toLowerCase();
  return `/vehiculos/${slug}-${shortId}`;
}

// Maps accent color names → Tailwind classes
const accentClasses: Record<string, { badge: string; price: string; viewAll: string }> = {
  blue: {
    badge: 'bg-blue-500',
    price: 'text-blue-600',
    viewAll: 'border-blue-500 text-blue-600 hover:bg-blue-50',
  },
  emerald: {
    badge: 'bg-emerald-500',
    price: 'text-emerald-600',
    viewAll: 'border-emerald-500 text-emerald-600 hover:bg-emerald-50',
  },
  amber: {
    badge: 'bg-amber-500',
    price: 'text-amber-600',
    viewAll: 'border-amber-500 text-amber-600 hover:bg-amber-50',
  },
  orange: {
    badge: 'bg-orange-500',
    price: 'text-orange-600',
    viewAll: 'border-orange-500 text-orange-600 hover:bg-orange-50',
  },
  purple: {
    badge: 'bg-purple-500',
    price: 'text-purple-600',
    viewAll: 'border-purple-500 text-purple-600 hover:bg-purple-50',
  },
  teal: {
    badge: 'bg-teal-500',
    price: 'text-teal-600',
    viewAll: 'border-teal-500 text-teal-600 hover:bg-teal-50',
  },
  rose: {
    badge: 'bg-rose-500',
    price: 'text-rose-600',
    viewAll: 'border-rose-500 text-rose-600 hover:bg-rose-50',
  },
  indigo: {
    badge: 'bg-indigo-500',
    price: 'text-indigo-600',
    viewAll: 'border-indigo-500 text-indigo-600 hover:bg-indigo-50',
  },
  sky: {
    badge: 'bg-sky-500',
    price: 'text-sky-600',
    viewAll: 'border-sky-500 text-sky-600 hover:bg-sky-50',
  },
  violet: {
    badge: 'bg-violet-500',
    price: 'text-violet-600',
    viewAll: 'border-violet-500 text-violet-600 hover:bg-violet-50',
  },
  pink: {
    badge: 'bg-pink-500',
    price: 'text-pink-600',
    viewAll: 'border-pink-500 text-pink-600 hover:bg-pink-50',
  },
  slate: {
    badge: 'bg-slate-500',
    price: 'text-slate-600',
    viewAll: 'border-slate-500 text-slate-600 hover:bg-slate-50',
  },
  yellow: {
    badge: 'bg-yellow-500',
    price: 'text-yellow-600',
    viewAll: 'border-yellow-500 text-yellow-600 hover:bg-yellow-50',
  },
};

// Gradient bg + icon color for empty placeholder circles (dealer-card style)
const accentIconClasses: Record<string, { bg: string; icon: string }> = {
  blue: {
    bg: 'bg-gradient-to-br from-blue-100 to-blue-200 dark:from-blue-900/30 dark:to-blue-800/30',
    icon: 'text-blue-500 dark:text-blue-400',
  },
  sky: {
    bg: 'bg-gradient-to-br from-sky-100 to-sky-200 dark:from-sky-900/30 dark:to-sky-800/30',
    icon: 'text-sky-500 dark:text-sky-400',
  },
  emerald: {
    bg: 'bg-gradient-to-br from-emerald-100 to-teal-100 dark:from-emerald-900/30 dark:to-teal-900/30',
    icon: 'text-emerald-600 dark:text-emerald-400',
  },
  violet: {
    bg: 'bg-gradient-to-br from-violet-100 to-purple-100 dark:from-violet-900/30 dark:to-purple-900/30',
    icon: 'text-violet-600 dark:text-violet-400',
  },
  amber: {
    bg: 'bg-gradient-to-br from-amber-100 to-orange-100 dark:from-amber-900/30 dark:to-orange-900/30',
    icon: 'text-amber-600 dark:text-amber-400',
  },
  orange: {
    bg: 'bg-gradient-to-br from-orange-100 to-amber-100 dark:from-orange-900/30 dark:to-amber-900/30',
    icon: 'text-orange-600 dark:text-orange-400',
  },
  rose: {
    bg: 'bg-gradient-to-br from-rose-100 to-pink-100 dark:from-rose-900/30 dark:to-pink-900/30',
    icon: 'text-rose-600 dark:text-rose-400',
  },
  pink: {
    bg: 'bg-gradient-to-br from-pink-100 to-rose-100 dark:from-pink-900/30 dark:to-rose-900/30',
    icon: 'text-pink-600 dark:text-pink-400',
  },
  slate: {
    bg: 'bg-gradient-to-br from-slate-100 to-gray-100 dark:from-slate-800/50 dark:to-gray-800/50',
    icon: 'text-slate-500 dark:text-slate-400',
  },
  yellow: {
    bg: 'bg-gradient-to-br from-yellow-100 to-amber-100 dark:from-yellow-900/30 dark:to-amber-900/30',
    icon: 'text-yellow-600 dark:text-yellow-400',
  },
  teal: {
    bg: 'bg-gradient-to-br from-teal-100 to-emerald-100 dark:from-teal-900/30 dark:to-emerald-900/30',
    icon: 'text-teal-600 dark:text-teal-400',
  },
  indigo: {
    bg: 'bg-gradient-to-br from-indigo-100 to-blue-100 dark:from-indigo-900/30 dark:to-blue-900/30',
    icon: 'text-indigo-600 dark:text-indigo-400',
  },
  purple: {
    bg: 'bg-gradient-to-br from-purple-100 to-violet-100 dark:from-purple-900/30 dark:to-violet-900/30',
    icon: 'text-purple-600 dark:text-purple-400',
  },
};

// ─────────────────────────────────────────────
// Card Component
// ─────────────────────────────────────────────

function VehicleCard({
  vehicle,
  accentColor = 'blue',
}: {
  vehicle: VehicleItem;
  accentColor?: string;
}) {
  const colors = accentClasses[accentColor] || accentClasses.blue;
  const [imageError, setImageError] = useState(false);

  const s3Image =
    vehicle.images
      ?.filter(img => img.url && !img.url.startsWith('blob:'))
      .sort((a, b) => (a.isPrimary ? -1 : b.isPrimary ? 1 : a.sortOrder - b.sortOrder))[0]?.url ||
    '';

  // Use fallback Unsplash image when S3 image is invalid or fails to load
  const fallbackImage = getVehicleFallbackImage(vehicle.id, vehicle.make, vehicle.bodyStyle);
  const primaryImage = imageError || !s3Image ? fallbackImage : s3Image;

  const location =
    [normalizeLocationName(vehicle.city), normalizeLocationName(vehicle.state)]
      .filter(Boolean)
      .join(', ') || 'R.D.';

  return (
    <Link href={buildVehicleSlug(vehicle)} className="group block h-full">
      <Card className="border-border hover:border-primary/50 flex h-full flex-col overflow-hidden border shadow-md transition-all duration-200 hover:-translate-y-1 hover:shadow-xl">
        <div className="bg-muted relative aspect-[4/3]">
          <Image
            src={primaryImage}
            alt={vehicle.title || `${vehicle.year} ${vehicle.make} ${vehicle.model}`}
            fill
            className="object-cover transition-transform duration-300 group-hover:scale-105"
            sizes="(max-width: 640px) 100vw, (max-width: 1024px) 50vw, 25vw"
            quality={75}
            loading="lazy"
            placeholder="blur"
            blurDataURL="data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iNDAwIiBoZWlnaHQ9IjMwMCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48cmVjdCB3aWR0aD0iMTAwJSIgaGVpZ2h0PSIxMDAlIiBmaWxsPSIjZTJlOGYwIi8+PC9zdmc+"
            onError={() => setImageError(true)}
          />
          {/* Ley 358-05 Art. 88 — Identificación de publicidad */}
          <span
            className="absolute right-2 bottom-2 rounded-full bg-black/50 px-2 py-0.5 text-[10px] text-white/80 backdrop-blur-sm"
            aria-label="Contenido publicitario pagado por anunciantes"
          >
            Publicidad
          </span>
        </div>
        <CardContent className="p-4">
          <h3 className="group-hover:text-primary line-clamp-1 text-sm font-semibold transition-colors">
            {vehicle.title || `${vehicle.year} ${vehicle.make} ${vehicle.model}`}
          </h3>
          <p className={`mt-1 text-lg font-bold ${colors.price}`}>
            {formatPrice(vehicle.price, vehicle.currency)}
          </p>
          <p className="text-muted-foreground mt-1 text-xs">
            {vehicle.mileage?.toLocaleString() ?? '—'}{' '}
            {vehicle.mileageUnit === 'Miles' ? 'mi' : 'km'} · {location}
          </p>
        </CardContent>
      </Card>
    </Link>
  );
}

// ─────────────────────────────────────────────
// Skeleton
// ─────────────────────────────────────────────

function SkeletonCard() {
  return (
    <Card className="animate-pulse overflow-hidden border-0 shadow-md">
      <div className="bg-muted aspect-[4/3]" />
      <CardContent className="space-y-2 p-4">
        <div className="bg-muted h-4 w-3/4 rounded" />
        <div className="bg-muted h-5 w-1/2 rounded" />
        <div className="bg-muted h-3 w-2/3 rounded" />
      </CardContent>
    </Card>
  );
}

function EmptyVehicleCard({
  icon,
  accentColor = 'blue',
}: {
  icon?: ReactNode;
  accentColor?: string;
}) {
  const iconClasses = accentIconClasses[accentColor] || accentIconClasses.blue;
  return (
    <Card className="flex h-full flex-col overflow-hidden border-2 border-dashed border-slate-200 bg-slate-50/50 dark:border-slate-700 dark:bg-slate-800/20">
      <div className="flex aspect-[4/3] items-center justify-center bg-slate-50 dark:bg-slate-800/30">
        <div
          className={`flex h-16 w-16 items-center justify-center rounded-2xl transition-transform duration-300 ${iconClasses.bg}`}
        >
          {/* Use section icon, forcing size via !important so it overrides the source className */}
          <span className={`[&_svg]:!h-8 [&_svg]:!w-8 ${iconClasses.icon}`}>
            {icon ?? <Car className="h-8 w-8" />}
          </span>
        </div>
      </div>
      <CardContent className="flex-1 p-4">
        <div className="bg-muted h-4 w-3/4 rounded" />
        <div className="bg-muted mt-2 h-5 w-1/2 rounded" />
        <div className="bg-muted mt-2 h-3 w-2/3 rounded" />
      </CardContent>
    </Card>
  );
}

// ─────────────────────────────────────────────
// Main Component
// ─────────────────────────────────────────────

interface VehicleTypeSectionProps extends VehicleTypeSectionConfig {
  /** Flat item count used when maxItemsResponsive is not set. Default: 10. */
  maxItems?: number;
  /**
   * Responsive item counts per breakpoint.
   * When provided, overrides maxItems.
   * Defaults: mobile=10, tablet=9, desktop=8, xl=10
   * (chosen so each breakpoint fills complete rows in the 2/3/4/5-col grid)
   */
  maxItemsResponsive?: {
    mobile: number;
    tablet: number;
    desktop: number;
    xl: number;
  };
}

export default function VehicleTypeSection({
  filterType = 'bodyStyle',
  filterValue,
  title,
  subtitle,
  icon,
  viewAllHref,
  accentColor = 'blue',
  maxItems = 10,
  maxItemsResponsive,
}: VehicleTypeSectionProps) {
  const colors = accentClasses[accentColor] || accentClasses.blue;

  // Responsive count: default grid is 2/3/4/5 cols → complete rows at each bp
  const effectiveMaxItems = useResponsiveMaxItems(
    maxItemsResponsive?.mobile ?? maxItems,
    maxItemsResponsive?.tablet ?? maxItems,
    maxItemsResponsive?.desktop ?? maxItems,
    maxItemsResponsive?.xl ?? maxItems
  );

  const { data, isLoading } = useQuery<VehiclesApiResponse>({
    queryKey: ['vehicles-by-type', filterType, filterValue, effectiveMaxItems],
    queryFn: async () => {
      const param = filterType === 'fuelType' ? 'fuelType' : 'bodyStyle';
      const res = await apiClient.get<VehiclesApiResponse>(
        `/api/vehicles?${param}=${encodeURIComponent(filterValue)}&limit=${effectiveMaxItems}&sortBy=featured`
      );
      return res.data;
    },
    staleTime: 5 * 60 * 1000, // 5 min
    retry: 1,
  });

  // FIX P0-005+P0-007: Exclude E2E test vehicles and deduplicate by make+model+year
  const vehicles = useMemo(() => {
    const items = data?.vehicles?.slice(0, effectiveMaxItems) ?? [];
    const seenIds = new Set<string>();
    const seenVehicles = new Set<string>();
    return items.filter((v: VehicleItem) => {
      // P0-005: Exclude E2E test vehicles that may leak from backend
      const title = (v.title || '').toUpperCase();
      if (title.includes('E2E') || title.includes('DO NOT BUY')) return false;
      // Deduplicate by ID
      if (seenIds.has(v.id)) return false;
      seenIds.add(v.id);
      // P0-007: Deduplicate by make+model+year to prevent duplicate listings
      const vehicleKey = `${v.make}-${v.model}-${v.year}`.toLowerCase();
      if (seenVehicles.has(vehicleKey)) return false;
      seenVehicles.add(vehicleKey);
      return true;
    });
  }, [data?.vehicles, effectiveMaxItems]);

  // Loading skeleton
  if (isLoading) {
    return (
      <section className="py-8">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 2xl:max-w-[1600px]">
          <div className="mb-6 flex items-center justify-between">
            <h2 className="text-2xl font-bold">
              {icon} {title}
            </h2>
          </div>
          <div className="grid grid-cols-2 gap-3 sm:gap-4 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
            {Array.from({ length: effectiveMaxItems }).map((_, i) => (
              <SkeletonCard key={i} />
            ))}
          </div>
        </div>
      </section>
    );
  }

  // Hide section entirely when no vehicles exist for this type
  if (!vehicles.length) {
    return null;
  }

  return (
    <section className="py-8">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 2xl:max-w-[1600px]">
        {/* Header */}
        <div className="mb-6 flex items-center justify-between">
          <div>
            <h2 className="text-foreground text-xl leading-tight font-bold tracking-tight sm:text-2xl">
              {icon && <span className="mr-2">{icon}</span>}
              {title}
            </h2>
            {subtitle && (
              <p className="text-muted-foreground mt-1 text-xs leading-relaxed sm:text-sm">
                {subtitle}
              </p>
            )}
            {/* Ley 358-05 Art. 88 — Identificación de espacio publicitario */}
            <span
              title="Contenido publicitario pagado por anunciantes. Ley 358-05."
              aria-label="Espacio publicitario pagado"
              className="mt-1.5 inline-flex items-center gap-1 rounded-full border border-slate-200/60 bg-slate-50/90 px-2.5 py-0.5 text-[11px] font-medium text-slate-600 dark:border-slate-700/60 dark:bg-slate-800/20 dark:text-slate-400"
            >
              <Megaphone className="h-2.5 w-2.5" />
              Publicidad
            </span>
          </div>
          <Link href={viewAllHref}>
            <Button
              variant="outline"
              size="sm"
              className={`group text-xs sm:text-sm ${colors.viewAll}`}
            >
              <span className="hidden sm:inline">Ver todos</span>
              <span className="sm:hidden">Ver</span>
              <ArrowRight className="ml-1 h-4 w-4 transition-transform group-hover:translate-x-1" />
            </Button>
          </Link>
        </div>
        {/* Grid — same card size as FeaturedVehicles */}
        <div className="grid grid-cols-2 gap-3 sm:gap-4 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
          {vehicles.map(vehicle => (
            <VehicleCard key={vehicle.id} vehicle={vehicle} accentColor={accentColor} />
          ))}
        </div>
        {/* Ley 358-05 Art. 84 — Transparencia en precios */}
        <p className="text-muted-foreground mt-3 text-center text-[10px] leading-relaxed">
          *Precios de referencia publicados por anunciantes. No incluyen ITBIS, traspaso ni otros
          cargos. Sujetos a verificación. Ley 358-05.
        </p>{' '}
      </div>
    </section>
  );
}
