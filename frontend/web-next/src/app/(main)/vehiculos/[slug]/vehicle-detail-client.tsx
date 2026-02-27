/**
 * Vehicle Detail Client Component — Premium conversion-focused layout
 *
 * UX Architecture (research-backed):
 * - Gallery + Header/Contact in 2-col layout (desktop)
 * - Quick specs bar for fast scanning (Baymard: key specs = #1 info users check)
 * - Internal chat as primary CTA (keeps users on platform)
 * - Reviews collapsible Amazon-style (secondary to listing)
 * - Mobile: single-col + sticky contact footer
 * - Registration prompts strategically placed for non-auth users
 */

'use client';

import * as React from 'react';
import Link from 'next/link';
import {
  ChevronDown,
  ChevronUp,
  Star,
  Fuel,
  Gauge,
  Settings,
  Palette,
  Zap,
  Lock,
} from 'lucide-react';
import { Breadcrumbs } from '@/components/ui/breadcrumbs';
import { Button } from '@/components/ui/button';
import {
  VehicleGallery,
  VehicleHeader,
  VehicleTabs,
  SimilarVehicles,
} from '@/components/vehicle-detail';
import { SellerContactCard } from '@/components/vehicle-detail/seller-contact-card';
import { VehicleCardSkeleton } from '@/components/ui/vehicle-card';
import { ReviewsSection } from '@/components/reviews';
import { useAuth } from '@/hooks/use-auth';
import { cn, formatNumber } from '@/lib/utils';
import type { Vehicle } from '@/types';

interface VehicleDetailClientProps {
  vehicle: Vehicle;
}

/* ─── Quick Specs Bar (horizontal, scannable) ─── */

function QuickSpecsBar({ vehicle }: { vehicle: Vehicle }) {
  const specs = [
    {
      icon: Gauge,
      label: 'Kilometraje',
      value: vehicle.mileage !== undefined ? `${formatNumber(vehicle.mileage)} km` : null,
    },
    {
      icon: Settings,
      label: 'Transmisión',
      value:
        vehicle.transmission === 'automatic'
          ? 'Automática'
          : vehicle.transmission === 'manual'
            ? 'Manual'
            : vehicle.transmission
              ? 'CVT'
              : null,
    },
    {
      icon: Fuel,
      label: 'Combustible',
      value:
        vehicle.fuelType === 'gasoline'
          ? 'Gasolina'
          : vehicle.fuelType === 'diesel'
            ? 'Diesel'
            : vehicle.fuelType === 'hybrid'
              ? 'Híbrido'
              : vehicle.fuelType === 'electric'
                ? 'Eléctrico'
                : null,
    },
    {
      icon: Palette,
      label: 'Color',
      value: vehicle.exteriorColor || null,
    },
    {
      icon: Settings,
      label: 'Tracción',
      value:
        vehicle.drivetrain === '4wd'
          ? '4x4'
          : vehicle.drivetrain === 'awd'
            ? 'AWD'
            : vehicle.drivetrain
              ? '2WD'
              : null,
    },
  ].filter(s => s.value);

  if (specs.length === 0) return null;

  return (
    <div className="scrollbar-hide flex gap-2 overflow-x-auto pb-1">
      {specs.map((spec, i) => (
        <div
          key={i}
          className="flex flex-shrink-0 items-center gap-2 rounded-full border border-gray-200 bg-white px-4 py-2 text-sm"
        >
          <spec.icon className="h-4 w-4 text-gray-400" />
          <span className="text-gray-500">{spec.label}:</span>
          <span className="font-medium text-gray-900">{spec.value}</span>
        </div>
      ))}
    </div>
  );
}

/* ─── Collapsible Reviews (Amazon-style) ─── */

function CollapsibleReviews({ vehicle, title }: { vehicle: Vehicle; title: string }) {
  const [isExpanded, setIsExpanded] = React.useState(false);

  return (
    <div className="overflow-hidden rounded-xl bg-white shadow-sm">
      <button
        onClick={() => setIsExpanded(!isExpanded)}
        className="flex w-full items-center justify-between px-6 py-4 text-left transition-colors hover:bg-gray-50"
      >
        <div className="flex items-center gap-2.5">
          <Star className="h-5 w-5 text-amber-500" />
          <h2 className="text-base font-semibold text-gray-900">Reseñas del vendedor</h2>
        </div>
        <div className="flex items-center gap-2">
          <span className="text-xs text-gray-400">{isExpanded ? 'Ocultar' : 'Ver reseñas'}</span>
          {isExpanded ? (
            <ChevronUp className="h-4 w-4 text-gray-400" />
          ) : (
            <ChevronDown className="h-4 w-4 text-gray-400" />
          )}
        </div>
      </button>

      <div
        className={cn(
          'transition-all duration-300 ease-in-out',
          isExpanded ? 'max-h-[2000px] opacity-100' : 'max-h-0 overflow-hidden opacity-0'
        )}
      >
        <div className="border-t border-gray-100 px-6 pt-2 pb-6">
          <ReviewsSection
            targetId={vehicle.sellerId}
            targetType={vehicle.sellerType === 'dealer' ? 'dealer' : 'seller'}
            vehicleId={vehicle.id}
            vehicleTitle={title}
          />
        </div>
      </div>
    </div>
  );
}

/* ─── Mobile Contact Sticky Footer ─── */

function MobileContactFooter({ vehicle }: { vehicle: Vehicle }) {
  const { isAuthenticated } = useAuth();
  const vehicleTitle = `${vehicle.year} ${vehicle.make} ${vehicle.model}`;

  const handleWhatsApp = () => {
    const message = encodeURIComponent(
      `Hola, me interesa el ${vehicleTitle} que vi en OKLA. ¿Está disponible?`
    );
    const vehicleData = vehicle as unknown as Record<string, unknown>;
    const seller = vehicleData.seller as { phone?: string } | undefined;
    const phone = seller?.phone?.replace(/\D/g, '');
    if (phone) {
      window.open(`https://wa.me/1${phone}?text=${message}`, '_blank');
    }
  };

  const chatHref = isAuthenticated
    ? `/mensajes/nuevo?vehicleId=${vehicle.id}&sellerId=${vehicle.sellerId}`
    : `/registro?redirect=/vehiculos/${vehicle.slug}`;

  return (
    <div className="fixed inset-x-0 bottom-0 z-40 border-t border-gray-200 bg-white/95 px-4 py-3 backdrop-blur-md lg:hidden">
      <div className="flex gap-2">
        <Button
          asChild
          className="flex-1 gap-2 bg-emerald-600 py-5 font-semibold text-white hover:bg-emerald-700"
        >
          <Link href={chatHref}>
            {isAuthenticated ? (
              <>
                <svg viewBox="0 0 24 24" className="h-4 w-4 fill-current">
                  <path d="M20 2H4c-1.1 0-2 .9-2 2v18l4-4h14c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2zm0 14H6l-2 2V4h16v12z" />
                </svg>
                Chatear
              </>
            ) : (
              <>
                <Lock className="h-4 w-4" />
                Regístrate para chatear
              </>
            )}
          </Link>
        </Button>
        <Button
          onClick={handleWhatsApp}
          variant="outline"
          className="gap-2 border-gray-300 py-5 font-semibold"
        >
          <svg viewBox="0 0 24 24" className="h-5 w-5 fill-[#25D366]">
            <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413z" />
          </svg>
        </Button>
      </div>
    </div>
  );
}

/* ─── Registration Banner (strategic placement for non-auth users) ─── */

function RegistrationBanner({ vehicleSlug }: { vehicleSlug: string }) {
  const { isAuthenticated } = useAuth();
  if (isAuthenticated) return null;

  return (
    <div className="overflow-hidden rounded-xl bg-gradient-to-r from-emerald-600 to-teal-600 p-6 text-white shadow-lg shadow-emerald-200/30">
      <div className="flex flex-col items-center gap-4 sm:flex-row">
        <div className="flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-full bg-white/20">
          <Zap className="h-6 w-6" />
        </div>
        <div className="flex-1 text-center sm:text-left">
          <h3 className="text-base font-semibold">
            Crea tu cuenta gratis y obtén ventajas exclusivas
          </h3>
          <p className="mt-1 text-sm text-emerald-100">
            Guarda favoritos • Recibe alertas de precios • Chatea con vendedores • Historial de
            búsquedas
          </p>
        </div>
        <div className="flex gap-2">
          <Button asChild className="bg-white font-semibold text-emerald-700 hover:bg-emerald-50">
            <Link href={`/registro?redirect=/vehiculos/${vehicleSlug}`}>Crear cuenta gratis</Link>
          </Button>
        </div>
      </div>
    </div>
  );
}

/* ═══════════════════════════════════════════════════════
   MAIN COMPONENT
   ═══════════════════════════════════════════════════════ */

export function VehicleDetailClient({ vehicle }: VehicleDetailClientProps) {
  const title = `${vehicle.year} ${vehicle.make} ${vehicle.model}`;

  return (
    <div className="min-h-screen bg-gray-50/80 pb-24 lg:pb-0">
      {/* Breadcrumbs — compact */}
      <div className="border-b border-gray-200 bg-white">
        <div className="container py-2.5">
          <Breadcrumbs items={[{ label: 'Vehículos', href: '/vehiculos' }, { label: title }]} />
        </div>
      </div>

      {/* ─── Main Grid ─── */}
      <div className="container py-5 lg:py-6">
        <div className="grid gap-5 lg:grid-cols-[1fr_380px]">
          {/* ─── LEFT COLUMN ─── */}
          <div className="space-y-5">
            {/* Gallery */}
            <VehicleGallery
              images={vehicle.images ?? []}
              title={title}
              has360View={vehicle.has360View}
              hasVideo={vehicle.hasVideo}
              className="shadow-sm"
            />

            {/* Mobile: Header + Contact (hidden on desktop) */}
            <div className="space-y-4 lg:hidden">
              <VehicleHeader vehicle={vehicle} />
              <SellerContactCard vehicle={vehicle} />
            </div>

            {/* Quick Specs Bar */}
            <QuickSpecsBar vehicle={vehicle} />

            {/* Tabs — Description, Specs, Features */}
            <VehicleTabs vehicle={vehicle} />

            {/* Registration Banner (non-auth users only) */}
            <RegistrationBanner vehicleSlug={vehicle.slug} />

            {/* Reviews — Amazon-style collapsible */}
            <CollapsibleReviews vehicle={vehicle} title={title} />
          </div>

          {/* ─── RIGHT COLUMN (Desktop Sticky Sidebar) ─── */}
          <div className="hidden lg:block">
            <div className="sticky top-20 space-y-5">
              <VehicleHeader vehicle={vehicle} />
              <SellerContactCard vehicle={vehicle} />
            </div>
          </div>
        </div>

        {/* ─── Similar Vehicles ─── */}
        <div className="mt-10">
          <React.Suspense
            fallback={
              <div>
                <h2 className="mb-6 text-lg font-bold text-gray-900">Vehículos similares</h2>
                <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-4">
                  {Array.from({ length: 4 }).map((_, i) => (
                    <VehicleCardSkeleton key={i} />
                  ))}
                </div>
              </div>
            }
          >
            <SimilarVehicles vehicleId={vehicle.id} limit={4} />
          </React.Suspense>
        </div>
      </div>

      {/* ─── Mobile Sticky Contact Footer ─── */}
      <MobileContactFooter vehicle={vehicle} />
    </div>
  );
}

export default VehicleDetailClient;
