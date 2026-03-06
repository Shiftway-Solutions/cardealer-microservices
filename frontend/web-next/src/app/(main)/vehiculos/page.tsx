/**
 * Vehicles Listing Page — Server Component
 *
 * Fetches initial vehicle data server-side for SEO and fast First Contentful Paint.
 * Delegates interactivity (filters, pagination, search) to VehiculosClient.
 *
 * Performance: SSR sends fully-rendered HTML with initial vehicle data.
 * SEO: Crawlers see the full vehicle listing without JS.
 *
 * Route: /vehiculos
 */

import { Suspense } from 'react';
import type { Metadata } from 'next';
import VehiculosClient from './vehiculos-client';
import { JsonLd, generateBreadcrumbJsonLd, generateItemListJsonLd } from '@/lib/seo';
import { searchVehicles } from '@/services/vehicles';
import type { VehicleCardData } from '@/types';

// ISR: Revalidate every 2 minutes for fresh listings
export const revalidate = 120;

export const metadata: Metadata = {
  title: 'Vehículos en Venta | OKLA',
  description:
    'Encuentra los mejores vehículos en venta en República Dominicana. Miles de opciones nuevas y usadas verificadas en OKLA.',
  openGraph: {
    title: 'Vehículos en Venta | OKLA',
    description:
      'Encuentra los mejores vehículos en venta en República Dominicana. Miles de opciones nuevas y usadas.',
    type: 'website',
    siteName: 'OKLA',
    locale: 'es_DO',
    images: [
      {
        url: '/opengraph-image',
        width: 1200,
        height: 630,
        alt: 'OKLA — Vehículos en venta en República Dominicana',
      },
    ],
  },
  twitter: {
    card: 'summary_large_image',
    title: 'Vehículos en Venta | OKLA',
    description: 'Miles de vehículos nuevos y usados verificados en República Dominicana.',
    images: ['/opengraph-image'],
  },
};

function VehiculosLoadingFallback() {
  return (
    <div className="bg-muted/50 min-h-screen">
      <div className="border-border bg-card border-b">
        <div className="mx-auto max-w-7xl px-4 py-4 sm:px-6 lg:px-8">
          <div className="bg-muted h-10 w-full max-w-xl animate-pulse rounded-lg" />
          <div className="mt-4 flex gap-3">
            {[1, 2, 3, 4].map(i => (
              <div key={i} className="bg-muted h-9 w-[150px] animate-pulse rounded-lg" />
            ))}
          </div>
        </div>
      </div>
      <div className="mx-auto max-w-7xl px-4 py-6 sm:px-6 lg:px-8">
        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 9 }).map((_, i) => (
            <div key={i} className="overflow-hidden rounded-xl border">
              <div className="bg-muted aspect-[16/10] w-full animate-pulse" />
              <div className="space-y-2 p-4">
                <div className="bg-muted h-5 w-3/4 animate-pulse rounded" />
                <div className="bg-muted h-4 w-1/2 animate-pulse rounded" />
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

export default async function VehiculosPage() {
  // SSR fetch first page for JSON-LD structured data (graceful degradation)
  let vehicles: VehicleCardData[] = [];
  try {
    const result = await searchVehicles({ page: 1, pageSize: 10 });
    vehicles = result.items;
  } catch {
    // API unavailable at build time — JSON-LD omitted, page still renders
  }

  const breadcrumbJsonLd = generateBreadcrumbJsonLd([
    { name: 'Inicio', url: '/' },
    { name: 'Vehículos en Venta', url: '/vehiculos' },
  ]);

  const itemListJsonLd =
    vehicles.length > 0
      ? generateItemListJsonLd(
          vehicles.map(v => ({
            name: `${v.year} ${v.make} ${v.model}`,
            url: `/vehiculos/${v.slug}`,
            image: v.imageUrl,
            price: v.price,
            currency: v.currency || 'DOP',
          }))
        )
      : null;

  return (
    <>
      <JsonLd data={breadcrumbJsonLd} />
      {itemListJsonLd && <JsonLd data={itemListJsonLd} />}
      <Suspense fallback={<VehiculosLoadingFallback />}>
        <VehiculosClient />
      </Suspense>
    </>
  );
}
