/**
 * Vehicle Detail Page
 *
 * Shows complete details of a single vehicle including:
 * - Image gallery with lightbox
 * - Vehicle specs and features
 * - Seller information and contact
 * - Similar vehicles
 *
 * Route: /vehiculos/[slug]
 *
 * Uses ISR (Incremental Static Regeneration) for performance:
 * - Pages are statically generated at build time
 * - Revalidated every 5 minutes
 * - On-demand revalidation when vehicle is updated
 */

import { Metadata } from 'next';
import { notFound } from 'next/navigation';
import { Suspense } from 'react';
import { VehicleDetailClient } from './vehicle-detail-client';
import { vehicleService } from '@/services/vehicles';
import { formatCurrency } from '@/lib/utils';
import {
  JsonLd,
  generateVehicleJsonLd,
  generateBreadcrumbJsonLd,
  generateFAQJsonLd,
  VehicleSEO,
} from '@/lib/seo';

// ISR Configuration: Revalidate every 5 minutes (300 seconds)
export const revalidate = 300;

// Dynamic params configuration
export const dynamicParams = true;

// SEO FIX: Pre-generate ALL beta vehicles at build time for faster Googlebot crawling.
// For the beta launch (1,500 listings), we pre-render all of them to maximize
// crawl budget efficiency and ensure instant TTFB on first Googlebot visit.
// After initial indexing wave, reduce to ~500 and rely on on-demand ISR for the rest.
// New vehicles still use on-demand ISR (dynamicParams: true).
export async function generateStaticParams(): Promise<Array<{ slug: string }>> {
  try {
    const apiUrl =
      process.env.INTERNAL_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:18443';
    const response = await fetch(`${apiUrl}/api/vehicles/sitemap`, {
      next: { revalidate: 900 },
    });
    if (!response.ok) return [];
    const data = await response.json();
    const items = Array.isArray(data) ? data : (data?.items ?? []);
    // Pre-generate all beta vehicles (up to 2000) for launch indexing coverage
    return items.slice(0, 2000).map((v: { slug: string }) => ({ slug: v.slug }));
  } catch {
    return [];
  }
}

interface VehiclePageProps {
  params: Promise<{ slug: string }>;
}

// Generate metadata for SEO
export async function generateMetadata({ params }: VehiclePageProps): Promise<Metadata> {
  const { slug } = await params;

  try {
    const vehicle = await vehicleService.getBySlug(slug);

    const priceFormatted = formatCurrency(vehicle.price);
    const title = `${vehicle.year} ${vehicle.make} ${vehicle.model} ${vehicle.trim || ''} - ${priceFormatted} | OKLA`;
    const mileage = vehicle.mileage ?? 0;
    const conditionLabel = vehicle.condition === 'new' && mileage <= 1000 ? 'Nuevo' : 'Usado';
    const description = `${conditionLabel} ${vehicle.year} ${vehicle.make} ${vehicle.model} en ${vehicle.location.city} por ${priceFormatted}. ${vehicle.mileage?.toLocaleString() || 0} km. ¡Contáctanos hoy para agendar una prueba de manejo! Ver fotos, especificaciones y financiamiento en OKLA.`;
    const _imageUrl = vehicle.images?.[0]?.url;

    const siteUrl = process.env.NEXT_PUBLIC_SITE_URL || 'https://okla.com.do';
    const vehicleUrl = `${siteUrl}/vehiculos/${slug}`;

    // INDEXATION FIX C3: Only active vehicles should be indexed by Google.
    // Draft, pending, rejected, sold, and inactive vehicles get noindex.
    const isIndexable = vehicle.status === 'active';

    // SEO FIX: Extract vehicle data for updatedAt/createdAt
    const vehicleData = vehicle as unknown as Record<string, unknown>;
    const updatedAt = (vehicleData.updatedAt as string) || undefined;
    const createdAt = (vehicleData.createdAt as string) || undefined;

    // SEO FIX: Keywords meta with brand/model/year/condition/location for Bing/Yandex
    const keywords = [
      vehicle.make,
      vehicle.model,
      `${vehicle.year}`,
      conditionLabel.toLowerCase(),
      'venta',
      'comprar',
      vehicle.location?.city || '',
      vehicle.location?.province || '',
      vehicle.fuelType || '',
      vehicle.transmission || '',
      'OKLA',
      'República Dominicana',
    ].filter(Boolean);

    return {
      title,
      description,
      keywords,
      alternates: {
        canonical: vehicleUrl,
      },
      // INDEXATION FIX C3: Prevent non-active vehicles from being indexed
      robots: isIndexable
        ? { index: true, follow: true, 'max-image-preview': 'large' as const, 'max-snippet': -1 }
        : { index: false, follow: false },
      openGraph: {
        title,
        description,
        url: vehicleUrl,
        // SEO FIX: Use 'article' type for product pages (richer than 'website')
        type: 'article',
        siteName: 'OKLA',
        locale: 'es_DO',
        // SEO FIX: publishedTime + modifiedTime for Google freshness signals
        ...(createdAt && { publishedTime: createdAt }),
        ...(updatedAt && { modifiedTime: updatedAt }),
      },
      twitter: {
        card: 'summary_large_image',
        title,
        description,
        site: '@okla',
        // Twitter image is generated dynamically by twitter-image.tsx
      },
      other: {
        'og:price:amount': vehicle.price.toString(),
        'og:price:currency': vehicle.currency || 'DOP',
        // SEO FIX: og:updated_time for Facebook crawler freshness
        ...(updatedAt && { 'og:updated_time': updatedAt }),
        // SEO FIX: product:availability for social commerce
        'product:availability': 'in stock',
        'product:condition': vehicle.condition === 'new' ? 'new' : 'used',
        'product:price:amount': vehicle.price.toString(),
        'product:price:currency': vehicle.currency || 'DOP',
      },
    };
  } catch {
    return {
      title: 'Vehículo no encontrado | OKLA',
      description: 'El vehículo que buscas no está disponible.',
    };
  }
}

// Server component for initial data fetching
export default async function VehiclePage({ params }: VehiclePageProps) {
  const { slug } = await params;

  let vehicle;
  try {
    vehicle = await vehicleService.getBySlug(slug);
  } catch (error) {
    console.error('Error fetching vehicle:', error);
    notFound();
  }

  if (!vehicle) {
    notFound();
  }

  // Track view only for Active vehicles (fire and forget)
  if (vehicle.status === 'active') {
    vehicleService.trackView(vehicle.id).catch(() => {
      // Silently fail - view tracking is non-critical
    });
  }

  // Prepare SEO data for JSON-LD
  // Cast to unknown first then to Record for flexible property access
  const vehicleData = vehicle as unknown as Record<string, unknown>;
  const sellerData = vehicleData.seller as { name?: string; type?: string } | undefined;
  const locationData = vehicle.location;

  const vehicleSEO: VehicleSEO = {
    id: vehicle.id,
    slug: slug,
    title: `${vehicle.year} ${vehicle.make} ${vehicle.model}`,
    make: vehicle.make,
    model: vehicle.model,
    year: vehicle.year,
    price: vehicle.price,
    currency: vehicle.currency || 'DOP',
    mileage: vehicle.mileage,
    fuelType: vehicle.fuelType,
    transmission: vehicle.transmission,
    color: vehicle.exteriorColor,
    condition: vehicle.condition as 'new' | 'used',
    description: vehicle.description,
    images: vehicle.images?.map(img => img.url) || [],
    sellerName: sellerData?.name,
    sellerType: (sellerData?.type || vehicle.sellerType) as 'dealer' | 'seller' | undefined,
    location: {
      city: locationData?.city || '',
      province: locationData?.province || '',
      country: 'DO',
    },
    availability: 'InStock',
    createdAt: (vehicleData.createdAt as string) || undefined,
    updatedAt: (vehicleData.updatedAt as string) || undefined,
    // SEO AUDIT FIX: Pass new JSON-LD fields for Google Rich Results
    vin: (vehicleData.vin as string) || undefined,
    bodyType: (vehicleData.bodyType as string) || undefined,
    numberOfDoors: (vehicleData.numberOfDoors as number) || undefined,
    driveWheelConfiguration: (vehicleData.driveWheelConfiguration as string) || undefined,
    engineSize: (vehicleData.engineSize as string) || undefined,
    // OKLA Score for Google structured data
    oklaScore: (vehicleData.oklaScore as number) || undefined,
  };

  // Breadcrumb data
  const breadcrumbs = [
    { name: 'Inicio', url: '/' },
    { name: 'Vehículos', url: '/vehiculos' },
    { name: vehicle.make, url: `/vehiculos?make=${vehicle.make.toLowerCase()}` },
    { name: `${vehicle.year} ${vehicle.make} ${vehicle.model}`, url: `/vehiculos/${slug}` },
  ];

  // SEO FIX: FAQ schema for Google Rich Results — auto-generated per vehicle
  const priceFormatted = formatCurrency(vehicle.price);
  const conditionLabel = vehicle.condition === 'new' ? 'Nuevo' : 'Usado';
  const vehicleFAQs = [
    {
      question: `¿Cuánto cuesta el ${vehicle.year} ${vehicle.make} ${vehicle.model}?`,
      answer: `El precio del ${vehicle.year} ${vehicle.make} ${vehicle.model} ${conditionLabel.toLowerCase()} es ${priceFormatted} (${vehicle.currency || 'DOP'}). Disponible en ${vehicle.location?.city || 'República Dominicana'}. Contáctanos en OKLA para opciones de financiamiento.`,
    },
    {
      question: `¿Dónde puedo ver el ${vehicle.year} ${vehicle.make} ${vehicle.model}?`,
      answer: `Este vehículo está disponible en ${vehicle.location?.city || 'República Dominicana'}${vehicle.location?.province ? `, ${vehicle.location.province}` : ''}. Agenda una cita de prueba de manejo directamente desde OKLA.`,
    },
    {
      question: `¿El ${vehicle.year} ${vehicle.make} ${vehicle.model} tiene garantía?`,
      answer: `Contacta al vendedor en OKLA para conocer las condiciones de garantía de este ${vehicle.year} ${vehicle.make} ${vehicle.model}. Los vehículos verificados en OKLA pasan por inspección de calidad.`,
    },
    {
      question: `¿Cuántos kilómetros tiene el ${vehicle.year} ${vehicle.make} ${vehicle.model}?`,
      answer: vehicle.mileage
        ? `Este ${vehicle.year} ${vehicle.make} ${vehicle.model} tiene ${vehicle.mileage.toLocaleString()} km registrados. Historial completo disponible en OKLA.`
        : `Consulta el kilometraje actualizado contactando al vendedor en OKLA.`,
    },
  ];

  // Status banner config for non-active vehicles
  const statusBanners: Record<string, { bg: string; text: string; message: string } | undefined> = {
    pending: {
      bg: 'bg-yellow-50 border-yellow-300',
      text: 'text-yellow-800',
      message:
        '⏳ Este anuncio está pendiente de revisión por nuestro equipo. No es visible al público aún.',
    },
    draft: {
      bg: 'bg-gray-50 border-gray-300',
      text: 'text-gray-700',
      message: '📝 Este es un borrador. Envíalo a revisión para que sea publicado.',
    },
    rejected: {
      bg: 'bg-red-50 border-red-300',
      text: 'text-red-800',
      message:
        '❌ Este anuncio fue rechazado. Revisa los comentarios del equipo y corrige los problemas antes de re-enviarlo.',
    },
    paused: {
      bg: 'bg-gray-50 border-gray-300',
      text: 'text-gray-600',
      message: '⏸️ Este anuncio está pausado y no es visible al público.',
    },
    sold: {
      bg: 'bg-blue-50 border-blue-300',
      text: 'text-blue-800',
      message: '✅ Este vehículo ha sido marcado como vendido.',
    },
  };
  const banner = statusBanners[vehicle.status];

  return (
    <>
      {/* Structured Data */}
      <JsonLd data={generateVehicleJsonLd(vehicleSEO)} />
      <JsonLd data={generateBreadcrumbJsonLd(breadcrumbs)} />
      {/* SEO FIX: FAQ schema for Google Rich Results — unique per listing */}
      <JsonLd data={generateFAQJsonLd(vehicleFAQs)} />

      {/* Status banner for non-active vehicles (visible only to the owner) */}
      {banner && (
        <div className={`border-b ${banner.bg}`}>
          <div className="container py-3">
            <p className={`text-sm font-medium ${banner.text}`}>{banner.message}</p>
          </div>
        </div>
      )}

      <Suspense fallback={<VehicleDetailSkeleton />}>
        <VehicleDetailClient vehicle={vehicle} />
      </Suspense>
    </>
  );
}

// Loading skeleton
function VehicleDetailSkeleton() {
  return (
    <div className="bg-muted/50 min-h-screen animate-pulse">
      {/* Breadcrumb skeleton */}
      <div className="border-border bg-card border-b">
        <div className="container py-3">
          <div className="bg-muted h-4 w-64 rounded"></div>
        </div>
      </div>

      {/* Main content skeleton */}
      <div className="container py-6 lg:py-8">
        <div className="grid gap-8 lg:grid-cols-3">
          {/* Left column */}
          <div className="space-y-6 lg:col-span-2">
            {/* Gallery skeleton */}
            <div className="bg-card rounded-xl p-4">
              <div className="bg-muted aspect-[16/10] rounded-lg"></div>
              <div className="mt-3 flex gap-2">
                {[1, 2, 3, 4, 5].map(i => (
                  <div key={i} className="bg-muted h-12 w-16 flex-shrink-0 rounded-lg"></div>
                ))}
              </div>
            </div>

            {/* Tabs skeleton */}
            <div className="bg-card rounded-xl p-6">
              <div className="border-border flex gap-4 border-b pb-3">
                <div className="bg-muted h-6 w-24 rounded"></div>
                <div className="bg-muted h-6 w-32 rounded"></div>
                <div className="bg-muted h-6 w-28 rounded"></div>
              </div>
              <div className="mt-4 space-y-3">
                <div className="bg-muted h-4 w-full rounded"></div>
                <div className="bg-muted h-4 w-3/4 rounded"></div>
                <div className="bg-muted h-4 w-5/6 rounded"></div>
              </div>
            </div>
          </div>

          {/* Right column */}
          <div className="hidden space-y-6 lg:block">
            {/* Header skeleton */}
            <div className="bg-card rounded-xl p-6">
              <div className="bg-muted mb-2 h-8 w-48 rounded"></div>
              <div className="bg-muted mb-4 h-6 w-32 rounded"></div>
              <div className="bg-muted mt-6 h-10 w-full rounded"></div>
            </div>

            {/* Seller skeleton */}
            <div className="bg-card rounded-xl p-6">
              <div className="flex gap-4">
                <div className="bg-muted h-14 w-14 rounded-full"></div>
                <div className="flex-1">
                  <div className="bg-muted mb-2 h-5 w-32 rounded"></div>
                  <div className="bg-muted h-4 w-24 rounded"></div>
                </div>
              </div>
              <div className="mt-6 space-y-3">
                <div className="bg-muted h-10 w-full rounded"></div>
                <div className="bg-muted h-10 w-full rounded"></div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
