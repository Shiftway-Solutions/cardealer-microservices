'use client';

// ============================================================================
// OKLA Ad Serving Hooks
// Hooks for fetching and managing sponsored vehicles in the platform
// ============================================================================

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useCallback, useRef } from 'react';
import type { SponsoredVehicle, AdSlotPosition, AdSlotConfig } from '@/types/ads';
import { AD_SLOT_CONFIGS } from '@/types/ads';
import { generateSponsoredVehiclesForSlot } from '@/lib/ad-engine';

// Query key factory
const adKeys = {
  all: ['ads'] as const,
  sponsored: (slot: AdSlotPosition) => [...adKeys.all, 'sponsored', slot] as const,
  search: (query: string) => [...adKeys.all, 'search', query] as const,
  dashboard: (dealerId: string) => [...adKeys.all, 'dashboard', dealerId] as const,
};

/**
 * Fetch sponsored vehicles for a specific ad slot.
 * In production, this calls the ad server API.
 * Currently uses the local ad engine for demo.
 */
async function fetchSponsoredVehicles(
  slot: AdSlotPosition,
  params?: {
    searchQuery?: string;
    count?: number;
    region?: string;
    make?: string;
  }
): Promise<SponsoredVehicle[]> {
  try {
    // Try to fetch from the ad server API
    const queryParams = new URLSearchParams();
    queryParams.set('slot', slot);
    if (params?.searchQuery) queryParams.set('query', params.searchQuery);
    if (params?.count) queryParams.set('count', String(params.count));
    if (params?.region) queryParams.set('region', params.region);
    if (params?.make) queryParams.set('make', params.make);

    const res = await fetch(`/api/advertising/sponsored?${queryParams.toString()}`);
    if (res.ok) {
      const data = await res.json();
      if (data.data && Array.isArray(data.data) && data.data.length > 0) {
        return data.data;
      }
    }
  } catch {
    // Fall back to local demo data
  }

  // Use demo data from ad engine
  return generateSponsoredVehiclesForSlot(slot, params?.count);
}

/**
 * Hook to get sponsored vehicles for a specific ad slot.
 */
export function useSponsoredVehicles(
  slot: AdSlotPosition,
  options?: {
    searchQuery?: string;
    count?: number;
    region?: string;
    make?: string;
    enabled?: boolean;
  }
) {
  return useQuery({
    queryKey: [...adKeys.sponsored(slot), options?.searchQuery, options?.count],
    queryFn: () =>
      fetchSponsoredVehicles(slot, {
        searchQuery: options?.searchQuery,
        count: options?.count,
        region: options?.region,
        make: options?.make,
      }),
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes
    enabled: options?.enabled !== false,
    refetchOnWindowFocus: false,
  });
}

/**
 * Hook to get sponsored search results (for /vehiculos page).
 * Returns both top-position and inline sponsored vehicles.
 */
export function useSponsoredSearch(searchQuery?: string) {
  const topResults = useSponsoredVehicles('search_top', {
    searchQuery,
    count: 3,
    enabled: true,
  });

  const inlineResults = useSponsoredVehicles('search_inline', {
    searchQuery,
    count: 4,
    enabled: true,
  });

  return {
    topSponsored: topResults.data ?? [],
    inlineSponsored: inlineResults.data ?? [],
    isLoading: topResults.isLoading || inlineResults.isLoading,
  };
}

/**
 * Hook to get all homepage sponsored vehicles across different slots.
 */
export function useHomepageAds() {
  const heroAds = useSponsoredVehicles('homepage_hero', { count: 5 });
  const gridAds = useSponsoredVehicles('homepage_featured_grid', { count: 3 });
  const recommendedAds = useSponsoredVehicles('homepage_recommended', { count: 6 });
  const categoryAds = useSponsoredVehicles('homepage_category', { count: 2 });

  return {
    heroSponsored: heroAds.data ?? [],
    gridSponsored: gridAds.data ?? [],
    recommendedSponsored: recommendedAds.data ?? [],
    categorySponsored: categoryAds.data ?? [],
    isLoading: heroAds.isLoading,
  };
}

/**
 * Hook for ad click tracking.
 */
export function useAdClickTracking() {
  const _queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (params: {
      vehicleId: string;
      campaignId: string;
      slotPosition: AdSlotPosition;
      auctionPosition: number;
    }) => {
      await fetch('/api/advertising/tracking/click', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          ...params,
          timestamp: Date.now(),
        }),
      });
    },
    onSuccess: () => {
      // Silently succeed
    },
  });
}

/**
 * Hook for frequency capping — tracks ad views per user session.
 */
export function useFrequencyCap() {
  const viewCounts = useRef<Map<string, { daily: number; weekly: number }>>(new Map());

  const recordView = useCallback((dealerId: string) => {
    const current = viewCounts.current.get(dealerId) ?? { daily: 0, weekly: 0 };
    viewCounts.current.set(dealerId, {
      daily: current.daily + 1,
      weekly: current.weekly + 1,
    });
  }, []);

  const canShow = useCallback((dealerId: string, maxDaily = 4, maxWeekly = 12) => {
    const current = viewCounts.current.get(dealerId);
    if (!current) return true;
    return current.daily < maxDaily && current.weekly < maxWeekly;
  }, []);

  return { recordView, canShow };
}

/**
 * Hook to interleave sponsored vehicles into organic results.
 * Places sponsored items at strategic positions without being intrusive.
 */
export function useInterleavedResults<T extends { id: string }>(
  organicResults: T[],
  sponsoredVehicles: SponsoredVehicle[],
  options?: {
    /** Positions to insert sponsored results (0-indexed) */
    insertPositions?: number[];
    /** Max sponsored items to show */
    maxSponsored?: number;
  }
) {
  const insertPositions = options?.insertPositions ?? [0, 1, 2, 8, 15, 23];
  const maxSponsored = options?.maxSponsored ?? sponsoredVehicles.length;

  const interleaved: Array<
    { type: 'organic'; data: T } | { type: 'sponsored'; data: SponsoredVehicle }
  > = [];

  let sponsoredIndex = 0;
  let organicIndex = 0;

  for (
    let i = 0;
    organicIndex < organicResults.length ||
    sponsoredIndex < Math.min(maxSponsored, sponsoredVehicles.length);
    i++
  ) {
    if (
      insertPositions.includes(i) &&
      sponsoredIndex < Math.min(maxSponsored, sponsoredVehicles.length)
    ) {
      interleaved.push({ type: 'sponsored', data: sponsoredVehicles[sponsoredIndex] });
      sponsoredIndex++;
    } else if (organicIndex < organicResults.length) {
      interleaved.push({ type: 'organic', data: organicResults[organicIndex] });
      organicIndex++;
    }
  }

  return interleaved;
}

/**
 * Get the slot configuration for an ad position.
 */
export function getSlotConfig(position: AdSlotPosition): AdSlotConfig | undefined {
  return AD_SLOT_CONFIGS.find(c => c.position === position);
}
