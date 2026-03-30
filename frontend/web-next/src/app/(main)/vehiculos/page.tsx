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
  title: 'Vehículos en Venta',
  description:
    'Encuentra los mejores vehículos en venta en República Dominicana. Miles de opciones nuevas y usadas verificadas en OKLA. ¡Busca, compara y contacta vendedores hoy!',
  keywords: [
    'vehículos en venta',
    'carros usados',
    'autos nuevos',
    'República Dominicana',
    'comprar carro',
    'OKLA',
    'marketplace automotriz',
    'Toyota',
    'Honda',
    'Hyundai',
  ],
  alternates: {
    canonical: 'https://okla.com.do/vehiculos',
  },
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
    description:
      'Miles de vehículos nuevos y usados verificados en República Dominicana. ¡Encuentra tu próximo carro en OKLA!',
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
  let totalPages = 1;
  try {
    const result = await searchVehicles({ page: 1, pageSize: 10 });
    vehicles = result.items;
    totalPages =
      result.pagination?.totalPages ?? Math.ceil((result.pagination?.totalItems ?? 0) / 10);
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

  // INDEXATION FIX C4: Generate pagination links so Googlebot can discover
  // vehicles beyond page 1. The UI uses infinite scroll (client JS), but
  // crawlers that don't execute JS need explicit <a> links to paginated URLs.
  const siteUrl = process.env.NEXT_PUBLIC_SITE_URL || 'https://okla.com.do';
  const maxPaginationLinks = Math.min(totalPages, 100); // Cap at 100 pages for sanity

  return (
    <>
      <JsonLd data={breadcrumbJsonLd} />
      {itemListJsonLd && <JsonLd data={itemListJsonLd} />}
      <Suspense fallback={<VehiculosLoadingFallback />}>
        <VehiculosClient />
      </Suspense>

      {/* INDEXATION FIX C4: Server-rendered pagination links for crawlers.
          These are hidden from visual users via noscript + sr-only but
          provide Googlebot with a crawl path to all paginated vehicle pages. */}
      <noscript>
        <nav aria-label="Paginación de vehículos">
          {Array.from({ length: maxPaginationLinks }, (_, i) => i + 1).map(page => (
            <a
              key={page}
              href={`${siteUrl}/vehiculos?page=${page}`}
              style={{ display: 'inline-block', padding: '4px 8px', margin: '2px' }}
            >
              Página {page}
            </a>
          ))}
        </nav>
      </noscript>

      {/* Also provide crawl-visible links (sr-only but in the DOM for Googlebot JS rendering) */}
      <nav aria-label="Páginas de vehículos" className="sr-only">
        {Array.from({ length: maxPaginationLinks }, (_, i) => i + 1).map(page => (
          <a key={page} href={`/vehiculos?page=${page}`}>
            Vehículos página {page}
          </a>
        ))}
      </nav>
    </>
  );
}
