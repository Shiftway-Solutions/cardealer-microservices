/**
 * DealerPromoSection
 *
 * Paid promotional section for verified OKLA dealers.
 * Shows dealer logo cards in a prominent grid layout.
 * Empty slots are shown as branded placeholder frames.
 *
 * This is a **sponsored / paid** placement — dealers who want
 * visibility on the homepage buy a slot here.
 */

'use client';

import Image from 'next/image';
import Link from 'next/link';
import { Building2, Star, ChevronRight } from 'lucide-react';
import { cn } from '@/lib/utils';

// =============================================================================
// TYPES
// =============================================================================

export interface DealerPromoItem {
  id: string;
  name: string;
  slug: string;
  logoUrl?: string;
  vehicleCount?: number;
  isActive?: boolean;
  portalSlug?: string;
}

interface DealerPromoSectionProps {
  dealers?: DealerPromoItem[];
  /** Total number of slots to display (filled + empty placeholders). Default 8. */
  totalSlots?: number;
  className?: string;
}

// =============================================================================
// CONSTANTS
// =============================================================================

/** Min slots always rendered so the grid looks full even with few dealers */
const DEFAULT_SLOTS = 8;

// =============================================================================
// SINGLE DEALER CARD
// =============================================================================

function DealerCard({ dealer }: { dealer: DealerPromoItem }) {
  const href = dealer.portalSlug ? `/portal/${dealer.portalSlug}` : `/dealers?id=${dealer.id}`;

  return (
    <Link
      href={href}
      className="group dark:bg-card relative flex flex-col items-center justify-between overflow-hidden rounded-2xl border-2 border-emerald-200 bg-white p-4 shadow-sm transition-all duration-300 hover:-translate-y-1 hover:border-emerald-400 hover:shadow-lg dark:border-emerald-800 dark:hover:border-emerald-600"
    >
      {/* Sponsored badge */}
      <div className="absolute top-2 right-2 flex items-center gap-1 rounded-full bg-emerald-50 px-2 py-0.5 text-[10px] font-semibold text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400">
        <Star className="h-2.5 w-2.5 fill-current" />
        Verificado
      </div>

      {/* Logo */}
      <div className="relative mb-3 flex h-20 w-full items-center justify-center">
        {dealer.logoUrl ? (
          <Image
            src={dealer.logoUrl}
            alt={dealer.name}
            fill
            sizes="(max-width: 640px) 80px, 120px"
            className="object-contain px-2 grayscale transition-all duration-300 group-hover:grayscale-0"
          />
        ) : (
          <div className="flex h-16 w-16 items-center justify-center rounded-xl bg-gradient-to-br from-emerald-100 to-teal-100 dark:from-emerald-900/30 dark:to-teal-900/30">
            <Building2 className="h-7 w-7 text-emerald-600 dark:text-emerald-400" />
          </div>
        )}
      </div>

      {/* Name */}
      <p className="text-center text-sm leading-tight font-semibold text-gray-800 dark:text-gray-100">
        {dealer.name}
      </p>

      {/* Vehicle count */}
      {dealer.vehicleCount !== undefined && dealer.vehicleCount > 0 && (
        <p className="text-muted-foreground mt-1 text-xs">
          {dealer.vehicleCount.toLocaleString()} vehículos
        </p>
      )}

      {/* CTA */}
      <div className="mt-3 flex items-center gap-1 text-xs font-semibold text-emerald-600 opacity-0 transition-opacity duration-300 group-hover:opacity-100 dark:text-emerald-400">
        Ver inventario
        <ChevronRight className="h-3.5 w-3.5" />
      </div>

      {/* Bottom accent bar */}
      <div className="absolute right-0 bottom-0 left-0 h-0.5 scale-x-0 bg-gradient-to-r from-emerald-400 to-teal-500 transition-transform duration-300 group-hover:scale-x-100" />
    </Link>
  );
}

// =============================================================================
// EMPTY PLACEHOLDER SLOT
// =============================================================================

function EmptySlot({ index }: { index: number }) {
  return (
    <Link
      href="/dealers"
      className={cn(
        'group flex flex-col items-center justify-center rounded-2xl border-2 border-dashed p-6 text-center transition-all duration-300 hover:border-emerald-400 hover:bg-emerald-50/50 dark:hover:bg-emerald-900/10',
        index % 2 === 0
          ? 'border-gray-200 dark:border-gray-700'
          : 'border-gray-100 dark:border-gray-800'
      )}
    >
      <div className="mb-2 flex h-10 w-10 items-center justify-center rounded-xl border-2 border-dashed border-gray-300 transition-colors group-hover:border-emerald-400 dark:border-gray-600">
        <Building2 className="h-5 w-5 text-gray-300 transition-colors group-hover:text-emerald-500 dark:text-gray-600" />
      </div>
      <p className="text-xs leading-tight font-medium text-gray-400 transition-colors group-hover:text-emerald-600 dark:text-gray-500 dark:group-hover:text-emerald-400">
        Tu marca aquí
      </p>
      <p className="mt-0.5 text-[10px] text-gray-300 group-hover:text-emerald-500 dark:text-gray-600">
        Espacio disponible
      </p>
    </Link>
  );
}

// =============================================================================
// MAIN COMPONENT
// =============================================================================

export function DealerPromoSection({
  dealers = [],
  totalSlots = DEFAULT_SLOTS,
  className,
}: DealerPromoSectionProps) {
  const realSlots = dealers.slice(0, totalSlots);
  const emptyCount = Math.max(0, totalSlots - realSlots.length);

  return (
    <section className={cn('bg-card py-12 lg:py-16', className)}>
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 2xl:max-w-[1600px]">
        {/* Header */}
        <div className="mb-8 text-center">
          {/* Paid badge */}
          <span className="mb-3 inline-flex items-center gap-1.5 rounded-full bg-emerald-50 px-4 py-1.5 text-xs font-semibold text-emerald-700 ring-1 ring-emerald-200 dark:bg-emerald-900/20 dark:text-emerald-400 dark:ring-emerald-800">
            <Star className="h-3 w-3 fill-current" />
            Espacio Patrocinado
          </span>
          <h2 className="text-foreground mb-2 text-2xl font-bold tracking-tight lg:text-3xl">
            Concesionarios en OKLA
          </h2>
          <p className="text-muted-foreground mx-auto max-w-lg text-sm leading-relaxed">
            Los mejores concesionarios verificados de República Dominicana. Espacio exclusivo para
            dealers certificados que quieren destacar su marca.
          </p>
        </div>

        {/* Dealer Grid */}
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-4 xl:grid-cols-5">
          {realSlots.map(dealer => (
            <DealerCard key={dealer.id} dealer={dealer} />
          ))}
          {Array.from({ length: emptyCount }).map((_, i) => (
            <EmptySlot key={`empty-${i}`} index={i} />
          ))}
        </div>

        {/* Footer CTA */}
        <div className="mt-8 text-center">
          <Link
            href="/dealers"
            className="inline-flex items-center gap-2 rounded-full border border-emerald-200 bg-emerald-50 px-6 py-2.5 text-sm font-semibold text-emerald-700 transition-all hover:border-emerald-400 hover:bg-emerald-100 dark:border-emerald-800 dark:bg-emerald-900/20 dark:text-emerald-400 dark:hover:bg-emerald-900/40"
          >
            ¿Eres concesionario? Promociona tu marca
            <ChevronRight className="h-4 w-4" />
          </Link>
        </div>
      </div>
    </section>
  );
}

export default DealerPromoSection;
