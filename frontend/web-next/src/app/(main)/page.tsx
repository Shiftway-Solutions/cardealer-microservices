/**
 * OKLA Homepage — Server Component
 *
 * Fetches homepage data server-side for fast First Contentful Paint.
 * Interactive components are rendered as client islands via HomepageClient.
 *
 * Performance: SSR sends fully-rendered HTML — critical for users in
 * Dominican Republic on mobile connections (~10-15 Mbps).
 */

import { Suspense } from 'react';
import { getInternalApiUrl } from '@/lib/api-url';
import { transformSection, type HomepageSectionDto } from '@/services/homepage-sections';
import HomepageClient from './homepage-client';
import { LoadingSection } from '@/components/homepage';

// =============================================
// SERVER-SIDE DATA FETCHING
// =============================================

async function getHomepageSections() {
  try {
    const apiUrl = getInternalApiUrl();
    const res = await fetch(`${apiUrl}/api/homepagesections/homepage`, {
      next: { revalidate: 300 }, // ISR: revalidate every 5 minutes
      headers: { Accept: 'application/json' },
    });

    if (!res.ok) {
      console.error(`[Homepage] Failed to fetch sections: ${res.status}`);
      return [];
    }

    const data: HomepageSectionDto[] = await res.json();
    return data.map(transformSection);
  } catch (error) {
    console.error('[Homepage] Error fetching sections:', error);
    return [];
  }
}

/**
 * Fallback: fetch featured vehicles directly when curated sections are empty.
 * Returns raw vehicle DTOs that HomepageClient will transform.
 */
async function getFeaturedVehiclesFallback() {
  try {
    const apiUrl = getInternalApiUrl();
    const res = await fetch(`${apiUrl}/api/vehicles/featured?limit=12`, {
      next: { revalidate: 300 },
      headers: { Accept: 'application/json' },
    });

    if (!res.ok) {
      console.error(`[Homepage] Failed to fetch featured vehicles: ${res.status}`);
      return [];
    }

    const data = await res.json();
    // Handle both ApiResponse<T> wrapper and raw array
    return Array.isArray(data) ? data : data.data ?? [];
  } catch (error) {
    console.error('[Homepage] Error fetching featured vehicles:', error);
    return [];
  }
}

// =============================================
// PAGE COMPONENT (Server Component)
// =============================================

export default async function HomePage() {
  const sections = await getHomepageSections();

  // When curated sections have no vehicles, fallback to featured vehicles API
  const hasVehicles = sections.some(s => s.vehicles.length > 0);
  const fallbackVehicles = hasVehicles ? [] : await getFeaturedVehiclesFallback();

  return (
    <Suspense fallback={<LoadingSection />}>
      <HomepageClient sections={sections} fallbackVehicles={fallbackVehicles} />
    </Suspense>
  );
}
