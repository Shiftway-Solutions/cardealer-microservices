/**
 * Vehicle Detail Client Component
 * Handles interactive parts of the vehicle detail page
 */

'use client';

import * as React from 'react';
import { Phone, MessageCircle } from 'lucide-react';
import { Breadcrumbs } from '@/components/ui/breadcrumbs';
import {
  VehicleGallery,
  VehicleHeader,
  SellerCard,
  VehicleTabs,
  SimilarVehicles,
} from '@/components/vehicle-detail';
import { VehicleCardSkeleton } from '@/components/ui/vehicle-card';
import { ReviewsSection } from '@/components/reviews';
import FeaturedVehicles from '@/components/advertising/featured-vehicles';
import { NativeBannerAd } from '@/components/advertising/native-ads';
import { ReportPurchaseModal } from '@/components/okla-score/report-purchase-modal';
import { useReportPurchase } from '@/hooks/use-report-purchase';
import { formatPrice } from '@/lib/format';
import { trackContactDealer, trackVehicleView } from '@/lib/retargeting-pixels';
import type { Vehicle } from '@/types';

// Extended type to access seller nested object
interface VehicleWithSeller extends Vehicle {
  seller?: {
    id: string;
    name: string;
    phone?: string;
  };
}

interface VehicleDetailClientProps {
  vehicle: Vehicle;
}

// ─── Sticky Mobile CTA Bar ──────────────────────────────────────────────────
// Always-visible bottom bar on mobile with price + WhatsApp + Call.
// Addresses SEM CRITICAL-4: CTA must be above fold on landing pages.
// z-40 to stay below cookie consent (z-[9998]) and modals.
function MobileStickyCtaBar({ vehicle }: { vehicle: Vehicle }) {
  const vws = vehicle as VehicleWithSeller;
  const phone = vws.seller?.phone;

  /** Normalize DR phone for WhatsApp (same logic as seller-card.tsx) */
  const getWhatsAppPhone = React.useCallback(() => {
    let p = phone?.replace(/\D/g, '') || '';
    if (p.length === 10 && /^(809|829|849)/.test(p)) {
      p = `1${p}`;
    } else if (p.length === 7) {
      p = `1809${p}`;
    }
    return p;
  }, [phone]);

  const handleCall = React.useCallback(() => {
    if (!phone) return;
    try {
      trackContactDealer({
        vehicleId: vehicle.id,
        title: `${vehicle.year} ${vehicle.make} ${vehicle.model}`,
        price: vehicle.price,
        dealerId: vehicle.sellerId,
        contactMethod: 'call',
      });
    } catch {
      /* silently fail */
    }
    // CWV P1-5 FIX: Navigate immediately — analytics fires via non-blocking sendBeacon/queueMicrotask
    // Removed 150ms setTimeout that was adding latency to INP
    window.location.href = `tel:${phone}`;
  }, [phone, vehicle]);

  const handleWhatsApp = React.useCallback(() => {
    const waPhone = getWhatsAppPhone();
    if (!waPhone) return;
    const msg = encodeURIComponent(
      `Hola, me interesa el ${vehicle.year} ${vehicle.make} ${vehicle.model} que vi en OKLA.`
    );
    try {
      trackContactDealer({
        vehicleId: vehicle.id,
        title: `${vehicle.year} ${vehicle.make} ${vehicle.model}`,
        price: vehicle.price,
        dealerId: vehicle.sellerId,
        contactMethod: 'whatsapp',
      });
    } catch {
      /* silently fail */
    }
    // P1-01 FIX: 100ms delay before navigation so pixel network requests
    // (FB Lead, Google Ads conversion) can complete before mobile Safari
    // suspends the tab. 100ms is within INP click budget (200ms).
    setTimeout(() => {
      window.open(`https://wa.me/${waPhone}?text=${msg}`, '_blank');
    }, 100);
  }, [vehicle, getWhatsAppPhone]);

  // Don't render if there's no phone at all
  if (!phone) return null;

  return (
    <div className="fixed inset-x-0 bottom-0 z-40 border-t border-slate-200 bg-white/95 backdrop-blur-md lg:hidden dark:border-slate-700 dark:bg-slate-900/95">
      <div className="mx-auto flex max-w-screen-xl items-center justify-between gap-2 px-3 py-2.5 sm:px-4">
        {/* Price */}
        <div className="min-w-0 flex-shrink">
          <p className="truncate text-lg font-bold text-slate-900 dark:text-white">
            {formatPrice(vehicle.price, vehicle.currency)}
          </p>
          {vehicle.isNegotiable && (
            <p className="text-xs text-slate-500 dark:text-slate-400">Negociable</p>
          )}
        </div>

        {/* Action buttons */}
        <div className="flex flex-shrink-0 gap-2">
          <button
            onClick={handleWhatsApp}
            className="inline-flex items-center gap-1.5 rounded-lg bg-green-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-green-700 active:bg-green-800"
            aria-label="Contactar por WhatsApp"
          >
            <MessageCircle className="h-4 w-4" />
            <span>WhatsApp</span>
          </button>
          <button
            onClick={handleCall}
            className="inline-flex items-center gap-1.5 rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-blue-700 active:bg-blue-800"
            aria-label="Llamar al vendedor"
          >
            <Phone className="h-4 w-4" />
            <span>Llamar</span>
          </button>
        </div>
      </div>
    </div>
  );
}

export function VehicleDetailClient({ vehicle }: VehicleDetailClientProps) {
  const title = `${vehicle.year} ${vehicle.make} ${vehicle.model}`;
  const [showPurchaseModal, setShowPurchaseModal] = React.useState(false);
  const { isPurchased, markPurchased } = useReportPurchase(vehicle.id);

  // Open purchase modal for OKLA Score report
  const handlePurchaseReportClick = React.useCallback(() => {
    if (isPurchased) return; // Already purchased — report is unlocked
    setShowPurchaseModal(true);
  }, [isPurchased]);

  // Called after successful payment
  const handlePurchaseComplete = React.useCallback(
    (purchaseId: string, buyerEmail: string) => {
      markPurchased(purchaseId, buyerEmail);
    },
    [markPurchased]
  );

  // REMARKETING FIX: Fire vehicle view pixels (FB ViewContent + Google view_item + TikTok ViewContent).
  // This creates remarketing audiences of "users who viewed this vehicle" on all platforms.
  // Must fire once per page load — NOT on re-renders.
  React.useEffect(() => {
    trackVehicleView({
      vehicleId: vehicle.id,
      title,
      make: vehicle.make,
      model: vehicle.model,
      year: vehicle.year,
      price: vehicle.price,
      condition: vehicle.condition,
      image: vehicle.images?.[0]?.url,
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [vehicle.id]);

  return (
    <div className="min-h-screen bg-slate-50 pb-[72px] lg:pb-0 dark:bg-slate-950">
      {/* Breadcrumbs */}
      <div className="border-border sticky top-16 z-20 border-b bg-white/95 backdrop-blur-sm lg:top-[72px] dark:bg-slate-900/95">
        <div className="mx-auto max-w-screen-xl px-4 py-2.5 sm:px-6">
          <Breadcrumbs items={[{ label: 'Vehículos', href: '/vehiculos' }, { label: title }]} />
        </div>
      </div>

      {/* Main content */}
      <div className="mx-auto max-w-screen-xl px-3 py-4 sm:px-6 lg:py-6">
        <div className="grid gap-4 sm:gap-5 lg:grid-cols-[1fr_320px]">
          {/* Left column - Gallery and Details */}
          <div className="space-y-4 lg:col-span-1">
            {/* Gallery */}
            <VehicleGallery
              images={vehicle.images ?? []}
              title={title}
              has360View={vehicle.has360View}
              hasVideo={vehicle.hasVideo}
              vehicleMake={vehicle.make}
              vehicleBodyStyle={vehicle.bodyType}
              vehicleId={vehicle.id}
            />

            {/* Mobile: Header (hidden on desktop) */}
            <div className="lg:hidden">
              <VehicleHeader vehicle={vehicle} onPurchaseReportClick={handlePurchaseReportClick} />
            </div>

            {/* Mobile: Seller Card */}
            <div className="lg:hidden">
              <SellerCard vehicle={vehicle} />
            </div>

            {/* Tabs - Description, Specs, Features */}
            <VehicleTabs vehicle={vehicle} />
          </div>

          {/* Right column - Sticky sidebar */}
          <div className="hidden lg:block">
            <div className="sticky top-[112px] space-y-4 lg:top-[120px]">
              {/* Header with price */}
              <VehicleHeader vehicle={vehicle} onPurchaseReportClick={handlePurchaseReportClick} />

              {/* Seller info */}
              <SellerCard vehicle={vehicle} />
            </div>
          </div>
        </div>

        {/* Similar vehicles */}
        <div className="mt-8">
          <React.Suspense
            fallback={
              <div>
                <h2 className="text-foreground mb-4 text-xl font-bold">Vehículos similares</h2>
                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
                  {Array.from({ length: 6 }).map((_, i) => (
                    <VehicleCardSkeleton key={i} />
                  ))}
                </div>
              </div>
            }
          >
            <SimilarVehicles vehicleId={vehicle.id} />
          </React.Suspense>
        </div>

        {/* ── Advertising: Dealer CTA Banner ──────────────────────────────── */}
        <div className="mt-8">
          <NativeBannerAd
            title="¿Buscas más opciones? Explora vehículos destacados"
            subtitle="Descubre ofertas exclusivas de dealers verificados en todo el país."
            ctaText="Ver Destacados"
            ctaUrl="/vehiculos?sortBy=featured"
            backgroundGradient="from-blue-600 to-indigo-700"
            impressionToken={`detail-banner-${vehicle.id}`}
          />
        </div>

        {/* ── Advertising: Vehículos Destacados (paid spots) ──────────────── */}
        <div className="mt-8">
          <FeaturedVehicles
            title="⭐ Vehículos Destacados"
            placementType="FeaturedSpot"
            maxItems={4}
            columns={4}
          />
        </div>

        {/* ── Advertising: Vehículos Premium (paid spots) ─────────────────── */}
        <div className="mt-8">
          <FeaturedVehicles
            title="💎 Vehículos Premium"
            placementType="PremiumSpot"
            maxItems={4}
            columns={4}
          />
        </div>

        {/* Ley 358-05 — Advertising disclosure */}
        <div className="mt-4">
          <p className="text-muted-foreground border-t pt-3 text-center text-[11px] leading-relaxed">
            Los espacios de vehículos destacados y premium constituyen contenido publicitario
            pagado. Precios de referencia, no incluyen ITBIS ni gastos de traspaso. Ley 358-05.
          </p>
        </div>

        {/* Reviews — secondary section, below the fold */}
        <div className="border-border mt-10 border-t pt-8">
          <ReviewsSection
            targetId={vehicle.sellerId}
            targetType={vehicle.sellerType === 'dealer' ? 'dealer' : 'seller'}
            vehicleId={vehicle.id}
            vehicleTitle={title}
          />
        </div>
      </div>

      {/* ── Sticky Mobile CTA Bar ────────────────────────────────────────── */}
      <MobileStickyCtaBar vehicle={vehicle} />

      {/* ── OKLA Score Report Purchase Modal ─────────────────────────────── */}
      <ReportPurchaseModal
        open={showPurchaseModal}
        onClose={() => setShowPurchaseModal(false)}
        vehicleId={vehicle.id}
        vehicleTitle={title}
        onPurchaseComplete={handlePurchaseComplete}
      />
    </div>
  );
}

export default VehicleDetailClient;
