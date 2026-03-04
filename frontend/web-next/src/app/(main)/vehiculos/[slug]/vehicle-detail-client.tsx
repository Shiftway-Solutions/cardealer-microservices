/**
 * Vehicle Detail Client Component
 * Handles interactive parts of the vehicle detail page
 */

'use client';

import * as React from 'react';
import { Breadcrumbs } from '@/components/ui/breadcrumbs';
import { VehicleGallery, VehicleHeader, SellerCard, VehicleTabs, SimilarVehicles } from '@/components/vehicle-detail';
import { VehicleCardSkeleton } from '@/components/ui/vehicle-card';
import { ReviewsSection } from '@/components/reviews';
import type { Vehicle } from '@/types';

interface VehicleDetailClientProps {
  vehicle: Vehicle;
}

export function VehicleDetailClient({ vehicle }: VehicleDetailClientProps) {
  const title = `${vehicle.year} ${vehicle.make} ${vehicle.model}`;

  return (
    <div className="min-h-screen bg-slate-50 dark:bg-slate-950">
      {/* Breadcrumbs */}
      <div className="border-border sticky top-0 z-20 border-b bg-white/95 backdrop-blur-sm dark:bg-slate-900/95">
        <div className="mx-auto max-w-screen-xl px-4 py-2.5 sm:px-6">
          <Breadcrumbs items={[{ label: 'Vehículos', href: '/vehiculos' }, { label: title }]} />
        </div>
      </div>

      {/* Main content */}
      <div className="mx-auto max-w-screen-xl px-4 py-5 sm:px-6 lg:py-6">
        <div className="grid gap-5 lg:grid-cols-[1fr_320px]">
          {/* Left column - Gallery and Details */}
          <div className="space-y-4 lg:col-span-1">
            {/* Gallery */}
            <VehicleGallery
              images={vehicle.images ?? []}
              title={title}
              has360View={vehicle.has360View}
              hasVideo={vehicle.hasVideo}
            />

            {/* Mobile: Header (hidden on desktop) */}
            <div className="lg:hidden">
              <VehicleHeader vehicle={vehicle} />
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
            <div className="sticky top-[53px] space-y-4">
              {/* Header with price */}
              <VehicleHeader vehicle={vehicle} />

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
                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
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

    </div>
  );
}

export default VehicleDetailClient;
