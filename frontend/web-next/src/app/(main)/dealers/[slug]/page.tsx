/**
 * Dealer Profile Page — Server Component
 *
 * SSR metadata + JSON-LD for Google Rich Results + ISR caching.
 * Fetches dealer data server-side for accurate SEO metadata.
 * The client component handles all interactivity (tabs, reviews, etc.)
 *
 * SEO: generateMetadata fetches real dealer data → canonical, OG, Twitter card.
 * Structured Data: AutoDealer + BreadcrumbList JSON-LD rendered server-side.
 *
 * Route: /dealers/[slug]
 */

import { Suspense } from 'react';
import type { Metadata } from 'next';
import DealerProfileClient from './dealer-profile-client';
import { Skeleton } from '@/components/ui/skeleton';
import {
  JsonLd,
  generateDealerMetadata,
  generateDealerJsonLd,
  generateBreadcrumbJsonLd,
} from '@/lib/seo';
import type { DealerSEO } from '@/lib/seo';

// ISR: Revalidate every 5 minutes
export const revalidate = 300;

// Allow dynamic slugs
export const dynamicParams = true;

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || 'https://okla.com.do';

interface DealerPageProps {
  params: Promise<{ slug: string }>;
}

/**
 * Server-side fetch of dealer data for SEO metadata + JSON-LD.
 * Uses INTERNAL_API_URL for direct service-to-service calls (BFF pattern).
 */
async function getDealerBySlug(slug: string) {
  try {
    const apiUrl =
      process.env.INTERNAL_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:18443';
    const response = await fetch(`${apiUrl}/api/dealers/slug/${slug}`, {
      next: { revalidate: 300 },
    });

    if (!response.ok) return null;
    return await response.json();
  } catch {
    return null;
  }
}

export async function generateMetadata({ params }: DealerPageProps): Promise<Metadata> {
  const { slug } = await params;

  // Try to fetch real dealer data for accurate metadata
  const dealer = await getDealerBySlug(slug);

  if (dealer) {
    const dealerSEO: DealerSEO = {
      id: dealer.id,
      slug,
      name: dealer.businessName,
      description: dealer.description,
      logo: dealer.logoUrl,
      coverImage: dealer.bannerUrl,
      address: dealer.address || '',
      city: dealer.city || '',
      province: dealer.state || '',
      phone: dealer.phone,
      email: dealer.email,
      website: dealer.website,
      rating: dealer.averageRating > 0 ? Number(dealer.averageRating) : undefined,
      reviewCount: dealer.totalReviews > 0 ? dealer.totalReviews : undefined,
      vehicleCount: dealer.activeListings,
    };

    return generateDealerMetadata(dealerSEO);
  }

  // Fallback: derive name from slug when API is unavailable
  const dealerName = slug
    .split('-')
    .map((word: string) => word.charAt(0).toUpperCase() + word.slice(1))
    .join(' ');

  return {
    title: `${dealerName}`,
    description: `Visita el perfil de ${dealerName} en OKLA. Encuentra su inventario de vehículos, reseñas y ubicación en República Dominicana.`,
    alternates: {
      canonical: `${SITE_URL}/dealers/${slug}`,
    },
    openGraph: {
      title: `${dealerName} | OKLA Dealers`,
      description: `Perfil del concesionario ${dealerName} en OKLA.`,
      type: 'website',
      siteName: 'OKLA',
      locale: 'es_DO',
    },
    twitter: {
      card: 'summary_large_image',
      title: `${dealerName} | OKLA Dealers`,
      description: `Perfil del concesionario ${dealerName} en OKLA.`,
    },
  };
}

/**
 * Build server-side JSON-LD structured data for Google Rich Results.
 * AutoDealer schema + BreadcrumbList — visible to crawlers without JS execution.
 */
function DealerJsonLd({ dealer, slug }: { dealer: Record<string, unknown> | null; slug: string }) {
  if (!dealer) return null;

  const dealerSEO: DealerSEO = {
    id: dealer.id as string,
    slug,
    name: dealer.businessName as string,
    description: dealer.description as string | undefined,
    logo: dealer.logoUrl as string | undefined,
    coverImage: dealer.bannerUrl as string | undefined,
    address: (dealer.address as string) || '',
    city: (dealer.city as string) || '',
    province: (dealer.state as string) || '',
    phone: dealer.phone as string | undefined,
    email: dealer.email as string | undefined,
    rating: (dealer.averageRating as number) > 0 ? Number(dealer.averageRating) : undefined,
    reviewCount: (dealer.totalReviews as number) > 0 ? (dealer.totalReviews as number) : undefined,
    vehicleCount: dealer.activeListings as number,
  };

  const breadcrumbJsonLd = generateBreadcrumbJsonLd([
    { name: 'Inicio', url: '/' },
    { name: 'Dealers', url: '/dealers' },
    { name: dealer.businessName as string, url: `/dealers/${slug}` },
  ]);

  return (
    <>
      <JsonLd data={generateDealerJsonLd(dealerSEO)} />
      <JsonLd data={breadcrumbJsonLd} />
    </>
  );
}

function DealerProfileSkeleton() {
  return (
    <div className="bg-muted/50 min-h-screen">
      <Skeleton className="h-48 w-full md:h-64 lg:h-80" />
      <div className="relative z-10 container mx-auto -mt-16 px-4">
        <div className="bg-card rounded-xl p-6 shadow-lg">
          <div className="flex flex-col gap-6 md:flex-row">
            <Skeleton className="-mt-16 h-24 w-24 rounded-xl md:-mt-20 md:h-32 md:w-32" />
            <div className="flex-1 space-y-3">
              <Skeleton className="h-8 w-64" />
              <Skeleton className="h-4 w-96" />
              <div className="flex gap-4">
                <Skeleton className="h-4 w-24" />
                <Skeleton className="h-4 w-24" />
                <Skeleton className="h-4 w-24" />
              </div>
            </div>
          </div>
        </div>
      </div>
      <div className="container mx-auto px-4 py-8">
        <div className="grid grid-cols-1 gap-8 lg:grid-cols-3">
          <div className="space-y-4 lg:col-span-2">
            <Skeleton className="h-10 w-64" />
            <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
              {[1, 2, 3, 4].map(i => (
                <Skeleton key={i} className="h-64 w-full" />
              ))}
            </div>
          </div>
          <div className="space-y-6">
            <Skeleton className="h-48 w-full" />
            <Skeleton className="h-32 w-full" />
          </div>
        </div>
      </div>
    </div>
  );
}

export default async function DealerProfilePage({ params }: DealerPageProps) {
  const { slug } = await params;

  // Fetch dealer data server-side for JSON-LD (shared cache with generateMetadata via ISR)
  const dealer = await getDealerBySlug(slug);

  return (
    <>
      {/* Server-rendered JSON-LD — visible to crawlers without JS */}
      <DealerJsonLd dealer={dealer} slug={slug} />

      <Suspense fallback={<DealerProfileSkeleton />}>
        <DealerProfileClient params={params} />
      </Suspense>
    </>
  );
}
