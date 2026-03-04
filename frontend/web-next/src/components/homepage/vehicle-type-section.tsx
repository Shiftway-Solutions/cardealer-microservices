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
import { useQuery } from '@tanstack/react-query';
import Link from 'next/link';
import Image from 'next/image';
import { ArrowRight } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { apiClient } from '@/lib/api-client';

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

function formatPrice(price: number, currency = 'DOP') {
  return currency === 'USD'
    ? `US$${price.toLocaleString('en-US')}`
    : `RD$${price.toLocaleString('es-DO')}`;
}

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
  const primaryImage =
    vehicle.images
      ?.filter(img => img.url && !img.url.startsWith('blob:'))
      .sort((a, b) => (a.isPrimary ? -1 : b.isPrimary ? 1 : a.sortOrder - b.sortOrder))[0]?.url ||
    '/placeholder-car.jpg';

  const location = [vehicle.city, vehicle.state].filter(Boolean).join(', ') || 'R.D.';

  return (
    <Link href={buildVehicleSlug(vehicle)} className="group block">
      <Card className="overflow-hidden border-0 shadow-md transition-shadow hover:shadow-xl">
        <div className="bg-muted relative aspect-[16/10]">
          <Image
            src={primaryImage}
            alt={vehicle.title || `${vehicle.year} ${vehicle.make} ${vehicle.model}`}
            fill
            className="object-cover transition-transform duration-300 group-hover:scale-105"
            sizes="(max-width: 640px) 100vw, (max-width: 1024px) 50vw, 25vw"
          />
          {vehicle.isFeatured && (
            <Badge className="absolute top-2 left-2 border-0 bg-amber-500 text-white">
              ⭐ Destacado
            </Badge>
          )}
          {vehicle.isPremium && !vehicle.isFeatured && (
            <Badge className="absolute top-2 left-2 border-0 bg-gradient-to-r from-purple-500 to-pink-500 text-white">
              💎 Premium
            </Badge>
          )}
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
      <div className="bg-muted aspect-[16/10]" />
      <CardContent className="space-y-2 p-4">
        <div className="bg-muted h-4 w-3/4 rounded" />
        <div className="bg-muted h-5 w-1/2 rounded" />
        <div className="bg-muted h-3 w-2/3 rounded" />
      </CardContent>
    </Card>
  );
}

// ─────────────────────────────────────────────
// Main Component
// ─────────────────────────────────────────────

interface VehicleTypeSectionProps extends VehicleTypeSectionConfig {
  maxItems?: number;
}

export default function VehicleTypeSection({
  filterType = 'bodyStyle',
  filterValue,
  title,
  subtitle,
  icon,
  viewAllHref,
  accentColor = 'blue',
  maxItems = 8,
}: VehicleTypeSectionProps) {
  const colors = accentClasses[accentColor] || accentClasses.blue;

  const { data, isLoading } = useQuery<VehiclesApiResponse>({
    queryKey: ['vehicles-by-type', filterType, filterValue, maxItems],
    queryFn: async () => {
      const param = filterType === 'fuelType' ? 'fuelType' : 'bodyStyle';
      const res = await apiClient.get<VehiclesApiResponse>(
        `/api/vehicles?${param}=${encodeURIComponent(filterValue)}&limit=${maxItems}&sortBy=featured`
      );
      return res.data;
    },
    staleTime: 5 * 60 * 1000, // 5 min
    retry: 1,
  });

  const vehicles = data?.vehicles?.slice(0, maxItems) ?? [];

  // Loading skeleton
  if (isLoading) {
    return (
      <section className="py-8">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="mb-6 flex items-center justify-between">
            <h2 className="text-2xl font-bold">
              {icon} {title}
            </h2>
          </div>
          <div className="grid grid-cols-2 gap-4 md:grid-cols-3 lg:grid-cols-4">
            {Array.from({ length: 4 }).map((_, i) => (
              <SkeletonCard key={i} />
            ))}
          </div>
        </div>
      </section>
    );
  }

  // Don't render empty sections
  if (!vehicles.length) return null;

  return (
    <section className="py-8">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="mb-6 flex items-center justify-between">
          <div>
            <h2 className="text-foreground text-2xl leading-tight font-bold tracking-tight">
              {icon && <span className="mr-2">{icon}</span>}
              {title}
            </h2>
            {subtitle && (
              <p className="text-muted-foreground mt-1 text-sm leading-relaxed">{subtitle}</p>
            )}
          </div>
          <Link href={viewAllHref}>
            <Button variant="outline" className={`group ${colors.viewAll}`}>
              Ver todos
              <ArrowRight className="ml-1 h-4 w-4 transition-transform group-hover:translate-x-1" />
            </Button>
          </Link>
        </div>

        {/* Grid — same card size as FeaturedVehicles */}
        <div className="grid grid-cols-2 gap-4 md:grid-cols-3 lg:grid-cols-4">
          {vehicles.map(vehicle => (
            <VehicleCard key={vehicle.id} vehicle={vehicle} accentColor={accentColor} />
          ))}
        </div>
      </div>
    </section>
  );
}
