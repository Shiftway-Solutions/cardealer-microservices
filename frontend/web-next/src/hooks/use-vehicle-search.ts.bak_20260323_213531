/**
 * useVehicleSearch Hook
 *
 * Manages vehicle search state with URL synchronization
 *
 * Features:
 * - Syncs filters with URL search params
 * - Integrates with Zustand store for persistent client state (undo, drafts, recent searches)
 * - Debounced updates to avoid excessive URL changes
 * - Provides type-safe filter interface
 * - Integrates with React Query for data fetching
 */

'use client';

import * as React from 'react';
import { useRouter, useSearchParams, usePathname } from 'next/navigation';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { searchVehicles } from '@/services/vehicles';
import { useSearchStore, type SearchFilters } from '@/stores/search-store';
import type { VehicleSearchParams } from '@/types';

// =============================================================================
// TYPES
// =============================================================================

/** Re-export SearchFilters from the Zustand store as VehicleSearchFilters for backward compatibility */
export type VehicleSearchFilters = SearchFilters;

export interface VehicleSearchResult {
  id: string;
  slug: string;
  title: string;
  make: string;
  model: string;
  year: number;
  price: number;
  mileage: number;
  transmission: string;
  fuelType: string;
  bodyType: string;
  location: string;
  imageUrl: string;
  dealRating?: 'great' | 'good' | 'fair' | 'high';
  isVerified?: boolean;
  isFeatured?: boolean;
}

export interface VehicleSearchResponse {
  vehicles: VehicleSearchResult[];
  total: number;
  page: number;
  totalPages: number;
  facets?: {
    makes: { value: string; count: number }[];
    bodyTypes: { value: string; count: number }[];
    provinces: { value: string; count: number }[];
    fuelTypes: { value: string; count: number }[];
    transmissions: { value: string; count: number }[];
  };
}

export interface UseVehicleSearchOptions {
  /** Initial filters */
  initialFilters?: VehicleSearchFilters;
  /** Debounce delay in ms */
  debounceMs?: number;
  /** Whether to sync with URL */
  syncUrl?: boolean;
  /** Whether to enable the query */
  enabled?: boolean;
}

export interface UseVehicleSearchReturn {
  /** Current filters */
  filters: VehicleSearchFilters;
  /** Set a single filter */
  setFilter: <K extends keyof VehicleSearchFilters>(key: K, value: VehicleSearchFilters[K]) => void;
  /** Set multiple filters at once */
  setFilters: (filters: Partial<VehicleSearchFilters>) => void;
  /** Clear all filters */
  clearFilters: () => void;
  /** Clear a specific filter */
  clearFilter: (key: keyof VehicleSearchFilters) => void;
  /** Search results */
  results: VehicleSearchResponse | undefined;
  /** Whether search is loading */
  isLoading: boolean;
  /** Whether search is fetching */
  isFetching: boolean;
  /** Error if any */
  error: Error | null;
  /** Refetch results */
  refetch: () => void;
  /** Active filter count */
  activeFilterCount: number;
}

// =============================================================================
// HELPERS
// =============================================================================

const defaultFilters: VehicleSearchFilters = {
  page: 1,
  limit: 24,
  sortBy: 'relevance',
};

function parseSearchParams(searchParams: URLSearchParams): VehicleSearchFilters {
  const filters: VehicleSearchFilters = { ...defaultFilters };

  // Text
  const query = searchParams.get('q');
  if (query) filters.query = query;

  // Basic
  const make = searchParams.get('make');
  if (make) filters.make = make;

  const model = searchParams.get('model');
  if (model) filters.model = model;

  const yearMin = searchParams.get('year_min');
  if (yearMin) filters.yearMin = parseInt(yearMin, 10);

  const yearMax = searchParams.get('year_max');
  if (yearMax) filters.yearMax = parseInt(yearMax, 10);

  const priceMin = searchParams.get('price_min');
  if (priceMin) filters.priceMin = parseInt(priceMin, 10);

  const priceMax = searchParams.get('price_max');
  if (priceMax) filters.priceMax = parseInt(priceMax, 10);

  // Additional
  const mileageMax = searchParams.get('mileage_max');
  if (mileageMax) filters.mileageMax = parseInt(mileageMax, 10);

  const bodyType = searchParams.get('body_type');
  if (bodyType) filters.bodyType = bodyType;

  const transmission = searchParams.get('transmission');
  if (transmission) filters.transmission = transmission as VehicleSearchFilters['transmission'];

  const fuelType = searchParams.get('fuel_type');
  if (fuelType) filters.fuelType = fuelType as VehicleSearchFilters['fuelType'];

  const drivetrain = searchParams.get('drivetrain');
  if (drivetrain) filters.drivetrain = drivetrain as VehicleSearchFilters['drivetrain'];

  const condition = searchParams.get('condition');
  if (condition) filters.condition = condition as VehicleSearchFilters['condition'];

  // Location
  const province = searchParams.get('province');
  if (province) filters.province = province;

  const city = searchParams.get('city');
  if (city) filters.city = city;

  // Other
  const dealRating = searchParams.get('deal_rating');
  if (dealRating) filters.dealRating = dealRating as VehicleSearchFilters['dealRating'];

  const sellerType = searchParams.get('seller_type');
  if (sellerType) filters.sellerType = sellerType as VehicleSearchFilters['sellerType'];

  const isCertified = searchParams.get('is_certified');
  if (isCertified) filters.isCertified = isCertified === 'true';

  const hasCleanTitle = searchParams.get('has_clean_title');
  if (hasCleanTitle) filters.hasCleanTitle = hasCleanTitle === 'true';

  const color = searchParams.get('color');
  if (color) filters.color = color;

  // Features (comma-separated)
  const features = searchParams.get('features');
  if (features) filters.features = features.split(',');

  // Extended DR-market filters
  const seats = searchParams.get('seats');
  if (seats) filters.seats = parseInt(seats, 10);

  const cylinders = searchParams.get('cylinders');
  if (cylinders) filters.cylinders = parseInt(cylinders, 10);

  const interiorColor = searchParams.get('interior_color');
  if (interiorColor) filters.interiorColor = interiorColor;

  // NOTE: `page` is intentionally NOT read from URL params.
  // This is an infinite-scroll view — page state is always reset to 1 on load.
  // Reading page from URL caused the bug where ?page=7 showed only 5/149 results.

  const limit = searchParams.get('limit');
  if (limit) filters.limit = parseInt(limit, 10);

  // Sorting
  const sortBy = searchParams.get('sort');
  if (sortBy) filters.sortBy = sortBy as VehicleSearchFilters['sortBy'];

  return filters;
}

function filtersToSearchParams(filters: VehicleSearchFilters): URLSearchParams {
  const params = new URLSearchParams();

  if (filters.query) params.set('q', filters.query);
  if (filters.make) params.set('make', filters.make);
  if (filters.model) params.set('model', filters.model);
  if (filters.yearMin) params.set('year_min', filters.yearMin.toString());
  if (filters.yearMax) params.set('year_max', filters.yearMax.toString());
  if (filters.priceMin) params.set('price_min', filters.priceMin.toString());
  if (filters.priceMax) params.set('price_max', filters.priceMax.toString());
  if (filters.mileageMax) params.set('mileage_max', filters.mileageMax.toString());
  if (filters.bodyType) params.set('body_type', filters.bodyType);
  if (filters.transmission) params.set('transmission', filters.transmission);
  if (filters.fuelType) params.set('fuel_type', filters.fuelType);
  if (filters.drivetrain) params.set('drivetrain', filters.drivetrain);
  if (filters.condition) params.set('condition', filters.condition);
  if (filters.province) params.set('province', filters.province);
  if (filters.city) params.set('city', filters.city);
  if (filters.dealRating) params.set('deal_rating', filters.dealRating);
  if (filters.sellerType) params.set('seller_type', filters.sellerType);
  if (filters.isCertified) params.set('is_certified', 'true');
  if (filters.hasCleanTitle) params.set('has_clean_title', 'true');
  if (filters.color) params.set('color', filters.color);
  if (filters.features?.length) params.set('features', filters.features.join(','));
  // Extended DR-market filters
  if (filters.seats) params.set('seats', filters.seats.toString());
  if (filters.cylinders) params.set('cylinders', filters.cylinders.toString());
  if (filters.interiorColor) params.set('interior_color', filters.interiorColor);
  // NOTE: `page` is intentionally NOT serialized to the URL.
  // This page is an infinite-scroll view — the page counter is internal state.
  // Writing page=7 to the URL causes: (a) users sharing/bookmarking broken URLs
  // that only show a partial page of results, (b) 149 total shown but only 5 visible.
  if (filters.limit && filters.limit !== 24) params.set('limit', filters.limit.toString());
  if (filters.sortBy && filters.sortBy !== 'relevance') params.set('sort', filters.sortBy);

  return params;
}

function countActiveFilters(filters: VehicleSearchFilters): number {
  let count = 0;

  if (filters.query) count++;
  if (filters.make) count++;
  if (filters.model) count++;
  if (filters.yearMin || filters.yearMax) count++;
  if (filters.priceMin || filters.priceMax) count++;
  if (filters.mileageMax) count++;
  if (filters.bodyType) count++;
  if (filters.transmission) count++;
  if (filters.fuelType) count++;
  if (filters.drivetrain) count++;
  if (filters.condition) count++;
  if (filters.province) count++;
  if (filters.city) count++;
  if (filters.dealRating) count++;
  if (filters.sellerType) count++;
  if (filters.isCertified) count++; // 'Con garantía del vendedor'
  if (filters.hasCleanTitle) count++;
  if (filters.color) count++;
  if (filters.features?.length) count++;
  // Extended DR-market filters
  if (filters.seats) count++;
  if (filters.cylinders) count++;
  if (filters.interiorColor) count++;

  return count;
}

// Map hook sort values to backend SortBy + SortDescending
function mapSortParams(sortBy?: string): { sortBy?: string; sortOrder?: 'asc' | 'desc' } {
  switch (sortBy) {
    case 'price_asc':
      return { sortBy: 'Price', sortOrder: 'asc' };
    case 'price_desc':
      return { sortBy: 'Price', sortOrder: 'desc' };
    case 'year_desc':
      return { sortBy: 'Year', sortOrder: 'desc' };
    case 'year_asc':
      return { sortBy: 'Year', sortOrder: 'asc' };
    case 'mileage_asc':
      return { sortBy: 'Mileage', sortOrder: 'asc' };
    case 'newest':
      return { sortBy: 'PublishedAt', sortOrder: 'desc' };
    default:
      return {};
  }
}

// Real API call using vehicles service
async function fetchVehicles(filters: VehicleSearchFilters): Promise<VehicleSearchResponse> {
  const sort = mapSortParams(filters.sortBy);

  const params: VehicleSearchParams = {
    q: filters.query,
    make: filters.make,
    model: filters.model,
    yearMin: filters.yearMin,
    yearMax: filters.yearMax,
    priceMin: filters.priceMin,
    priceMax: filters.priceMax,
    mileageMax: filters.mileageMax,
    bodyType: filters.bodyType as VehicleSearchParams['bodyType'],
    transmission: filters.transmission,
    fuelType: filters.fuelType,
    drivetrain: filters.drivetrain,
    condition: filters.condition,
    province: filters.province,
    city: filters.city,
    sellerType: filters.sellerType,
    color: filters.color,
    isCertified: filters.isCertified,
    hasCleanTitle: filters.hasCleanTitle,
    features: filters.features,
    // Extended DR-market filters
    seats: filters.seats,
    cylinders: filters.cylinders,
    interiorColor: filters.interiorColor,
    page: filters.page || 1,
    pageSize: filters.limit || 24,
    sortBy: sort.sortBy,
    sortOrder: sort.sortOrder,
  };

  try {
    const result = await searchVehicles(params);

    // Transform PaginatedResponse<VehicleCardData> to VehicleSearchResponse
    const vehicles: VehicleSearchResult[] = result.items.map(item => ({
      id: item.id,
      slug: item.slug,
      title: `${item.year} ${item.make} ${item.model}${item.trim ? ' ' + item.trim : ''}`,
      make: item.make,
      model: item.model,
      year: item.year,
      price: item.price,
      mileage: item.mileage,
      transmission: item.transmission,
      fuelType: item.fuelType,
      bodyType: '',
      location: item.location || '',
      imageUrl: item.imageUrl || '/placeholder-car.jpg',
      dealRating: item.dealRating as VehicleSearchResult['dealRating'],
      isVerified: item.isCertified,
      isFeatured: false,
    }));

    return {
      vehicles,
      total: result.pagination.totalItems,
      page: result.pagination.page,
      totalPages: result.pagination.totalPages,
    };
  } catch (error) {
    console.error('Error fetching vehicles:', error);
    // Return empty results on error
    return {
      vehicles: [],
      total: 0,
      page: filters.page || 1,
      totalPages: 0,
    };
  }
}

// =============================================================================
// HOOK
// =============================================================================

export function useVehicleSearch(options: UseVehicleSearchOptions = {}): UseVehicleSearchReturn {
  const { initialFilters, debounceMs = 300, syncUrl = true, enabled = true } = options;

  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const queryClient = useQueryClient();

  // ── Zustand store integration ─────────────────────────────────
  const storeFilters = useSearchStore(s => s.filters);
  const storeSetFilter = useSearchStore(s => s.setFilter);
  const storeSetFilters = useSearchStore(s => s.setFilters);
  const storeClearAllFilters = useSearchStore(s => s.clearAllFilters);
  const storeClearFilter = useSearchStore(s => s.clearFilter);
  const storeAddRecentSearch = useSearchStore(s => s.addRecentSearch);
  const storeSetIsSearching = useSearchStore(s => s.setIsSearching);

  // Flag to track if we've initialized from URL (only on first mount)
  const hasInitialized = React.useRef(false);

  // Initialize from URL params on mount (takes priority over persisted store state)
  React.useEffect(() => {
    if (hasInitialized.current) return;
    hasInitialized.current = true;

    if (syncUrl && searchParams) {
      const urlFilters = parseSearchParams(searchParams);
      // Merge URL params with initial filters (URL wins)
      const mergedFilters = { ...defaultFilters, ...initialFilters, ...urlFilters };
      // Only update store if URL had meaningful filters
      const urlHasFilters = searchParams.toString().length > 0;
      if (urlHasFilters) {
        storeSetFilters(mergedFilters);
      }
    } else if (initialFilters) {
      storeSetFilters({ ...defaultFilters, ...initialFilters });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Use store filters as the source of truth
  const filters = storeFilters;

  // Debounced filters for URL sync and data fetching
  const [debouncedFilters, setDebouncedFilters] = React.useState(filters);

  // Debounce effect
  React.useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedFilters(filters);
    }, debounceMs);

    return () => clearTimeout(timer);
  }, [filters, debounceMs]);

  // Sync URL with debounced filters
  React.useEffect(() => {
    if (!syncUrl) return;

    const params = filtersToSearchParams(debouncedFilters);
    const queryString = params.toString();
    const newUrl = queryString ? `${pathname}?${queryString}` : pathname;

    router.replace(newUrl, { scroll: false });
  }, [debouncedFilters, pathname, router, syncUrl]);

  // React Query for data fetching
  const {
    data: results,
    isLoading,
    isFetching,
    error,
    refetch,
  } = useQuery({
    queryKey: ['vehicles', 'search', debouncedFilters],
    queryFn: () => fetchVehicles(debouncedFilters),
    enabled,
    staleTime: 1000 * 60 * 5, // 5 minutes
    gcTime: 1000 * 60 * 15, // 15 minutes — keep search results in cache longer
    placeholderData: previousData => previousData,
  });

  // Sync searching state to store
  React.useEffect(() => {
    storeSetIsSearching(isFetching);
  }, [isFetching, storeSetIsSearching]);

  // Track recent searches when results come back
  React.useEffect(() => {
    if (!results || results.total === 0) return;
    if (!filters.query && !filters.make) return; // Only track meaningful searches

    const label = filters.query
      ? filters.query
      : filters.make
        ? `${filters.make}${filters.model ? ' ' + filters.model : ''}`
        : '';

    if (label) {
      storeAddRecentSearch({
        filters: { ...filters },
        label,
        resultCount: results.total,
      });
    }
    // Only run when debounced results change, not on every filter change
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [results?.total, debouncedFilters]);

  // Prefetch next page when current results load
  React.useEffect(() => {
    if (!results || results.page >= results.totalPages) return;

    const nextPageFilters = { ...debouncedFilters, page: (debouncedFilters.page || 1) + 1 };
    queryClient.prefetchQuery({
      queryKey: ['vehicles', 'search', nextPageFilters],
      queryFn: () => fetchVehicles(nextPageFilters),
      staleTime: 1000 * 60 * 5,
    });
  }, [results, debouncedFilters, queryClient]);

  // Delegate to store actions (maintains backward-compatible API)
  const setFilter = React.useCallback(
    <K extends keyof VehicleSearchFilters>(key: K, value: VehicleSearchFilters[K]) => {
      storeSetFilter(key, value);
    },
    [storeSetFilter]
  );

  const setFilters = React.useCallback(
    (newFilters: Partial<VehicleSearchFilters>) => {
      storeSetFilters(newFilters);
    },
    [storeSetFilters]
  );

  const clearFilters = React.useCallback(() => {
    storeClearAllFilters();
  }, [storeClearAllFilters]);

  const clearFilter = React.useCallback(
    (key: keyof VehicleSearchFilters) => {
      storeClearFilter(key);
    },
    [storeClearFilter]
  );

  // Count active filters
  const activeFilterCount = React.useMemo(() => countActiveFilters(filters), [filters]);

  return {
    filters,
    setFilter,
    setFilters,
    clearFilters,
    clearFilter,
    results,
    isLoading,
    isFetching,
    error: error as Error | null,
    refetch,
    activeFilterCount,
  };
}

export default useVehicleSearch;
