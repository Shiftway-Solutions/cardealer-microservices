/**
 * React Query hooks for Vehicles
 * Provides data fetching and mutations for vehicle operations
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  vehicleService,
  getVehiclesByDealer,
  getVehiclesByIds,
  createVehicle,
  publishVehicle,
  updateVehicle,
  deleteVehicle,
  getMakes,
  getModelsByMake,
  getBodyTypes,
  getFuelTypes,
  getTransmissions,
  getColors,
  getProvinces,
  getFeatures,
  decodeVinSmart,
  checkVinExists,
  decodeVinBatch,
  type CreateVehicleRequest,
  type UpdateVehicleRequest,
} from '@/services/vehicles';
import {
  getPriceSuggestion,
  type PriceSuggestionRequest,
} from '@/services/vehicle-intelligence';
import type { VehicleSearchParams } from '@/types';

// =============================================================================
// QUERY KEYS
// =============================================================================

export const vehicleKeys = {
  all: ['vehicles'] as const,
  lists: () => [...vehicleKeys.all, 'list'] as const,
  list: (params: VehicleSearchParams) => [...vehicleKeys.lists(), params] as const,
  details: () => [...vehicleKeys.all, 'detail'] as const,
  detail: (id: string) => [...vehicleKeys.details(), id] as const,
  detailBySlug: (slug: string) => [...vehicleKeys.details(), 'slug', slug] as const,
  similar: (vehicleId: string) => [...vehicleKeys.all, 'similar', vehicleId] as const,
  featured: () => [...vehicleKeys.all, 'featured'] as const,
  byDealer: (dealerId: string) => [...vehicleKeys.all, 'dealer', dealerId] as const,
  byIds: (ids: string[]) => [...vehicleKeys.all, 'batch', ...ids] as const,
};

// =============================================================================
// QUERY HOOKS
// =============================================================================

/**
 * Get vehicle by slug
 */
export function useVehicleBySlug(slug: string) {
  return useQuery({
    queryKey: vehicleKeys.detailBySlug(slug),
    queryFn: () => vehicleService.getBySlug(slug),
    enabled: !!slug,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Get vehicle by ID
 */
export function useVehicle(id: string) {
  return useQuery({
    queryKey: vehicleKeys.detail(id),
    queryFn: () => vehicleService.getById(id),
    enabled: !!id,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * List vehicles with filters (simple query wrapper).
 * For the full search hook with URL sync and debounce, use useVehicleSearch from use-vehicle-search.ts.
 */
export function useVehicleList(params: VehicleSearchParams, options?: { enabled?: boolean }) {
  return useQuery({
    queryKey: vehicleKeys.list(params),
    queryFn: () => vehicleService.search(params),
    enabled: options?.enabled !== false,
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
}

/** @deprecated Use useVehicleList instead */
export const useVehicleSearch = useVehicleList;

/**
 * Get similar vehicles
 */
export function useSimilarVehicles(vehicleId: string, limit: number = 4) {
  return useQuery({
    queryKey: vehicleKeys.similar(vehicleId),
    queryFn: () => vehicleService.getSimilar(vehicleId, limit),
    enabled: !!vehicleId,
    staleTime: 10 * 60 * 1000, // 10 minutes
  });
}

/**
 * Get featured vehicles
 */
export function useFeaturedVehicles(limit: number = 8) {
  return useQuery({
    queryKey: vehicleKeys.featured(),
    queryFn: () => vehicleService.getFeatured(limit),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Get vehicles by dealer ID
 */
export function useVehiclesByDealer(
  dealerId: string,
  params: { page?: number; pageSize?: number; status?: string } = {}
) {
  return useQuery({
    queryKey: [...vehicleKeys.byDealer(dealerId), params],
    queryFn: () => getVehiclesByDealer(dealerId, params),
    enabled: !!dealerId,
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
}

/**
 * Get vehicles by IDs (for comparison)
 */
export function useVehiclesByIds(ids: string[]) {
  return useQuery({
    queryKey: vehicleKeys.byIds(ids),
    queryFn: () => getVehiclesByIds(ids),
    enabled: ids.length > 0,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

// =============================================================================
// MUTATION HOOKS
// =============================================================================

/**
 * Track vehicle view
 */
export function useTrackVehicleView() {
  return useMutation({
    mutationFn: (vehicleId: string) => vehicleService.trackView(vehicleId),
  });
}

/**
 * Create a new vehicle
 */
export function useCreateVehicle() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateVehicleRequest) => createVehicle(data),
    onSuccess: () => {
      // Invalidate vehicle lists to refetch
      queryClient.invalidateQueries({ queryKey: vehicleKeys.lists() });
      queryClient.invalidateQueries({ queryKey: vehicleKeys.featured() });
    },
  });
}

/**
 * Publish a vehicle (Draft → Active)
 */
export function usePublishVehicle() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => publishVehicle(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: vehicleKeys.lists() });
      queryClient.invalidateQueries({ queryKey: vehicleKeys.featured() });
    },
  });
}

/**
 * Update an existing vehicle
 */
export function useUpdateVehicle() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateVehicleRequest }) =>
      updateVehicle(id, data),
    onSuccess: (updatedVehicle, { id }) => {
      // Update the cache with new data
      queryClient.setQueryData(vehicleKeys.detail(id), updatedVehicle);
      // Invalidate lists
      queryClient.invalidateQueries({ queryKey: vehicleKeys.lists() });
    },
  });
}

/**
 * Delete a vehicle
 */
export function useDeleteVehicle() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => deleteVehicle(id),
    onSuccess: (_, id) => {
      // Remove from cache
      queryClient.removeQueries({ queryKey: vehicleKeys.detail(id) });
      // Invalidate lists
      queryClient.invalidateQueries({ queryKey: vehicleKeys.lists() });
    },
  });
}

// =============================================================================
// CATALOG HOOKS
// =============================================================================

export const catalogKeys = {
  all: ['catalog'] as const,
  makes: () => [...catalogKeys.all, 'makes'] as const,
  models: (makeId: string) => [...catalogKeys.all, 'models', makeId] as const,
  bodyTypes: () => [...catalogKeys.all, 'bodyTypes'] as const,
  fuelTypes: () => [...catalogKeys.all, 'fuelTypes'] as const,
  transmissions: () => [...catalogKeys.all, 'transmissions'] as const,
  colors: () => [...catalogKeys.all, 'colors'] as const,
  provinces: () => [...catalogKeys.all, 'provinces'] as const,
  features: () => [...catalogKeys.all, 'features'] as const,
};

/**
 * Get all vehicle makes
 */
export function useMakes() {
  return useQuery({
    queryKey: catalogKeys.makes(),
    queryFn: getMakes,
    staleTime: 24 * 60 * 60 * 1000, // 24 hours - catalog data rarely changes
  });
}

/**
 * Get models for a specific make
 */
export function useModelsByMake(makeId: string) {
  return useQuery({
    queryKey: catalogKeys.models(makeId),
    queryFn: () => getModelsByMake(makeId),
    enabled: !!makeId,
    staleTime: 24 * 60 * 60 * 1000, // 24 hours
  });
}

/**
 * Get body types
 */
export function useBodyTypes() {
  return useQuery({
    queryKey: catalogKeys.bodyTypes(),
    queryFn: getBodyTypes,
    staleTime: 24 * 60 * 60 * 1000, // 24 hours
  });
}

/**
 * Get fuel types
 */
export function useFuelTypes() {
  return useQuery({
    queryKey: catalogKeys.fuelTypes(),
    queryFn: getFuelTypes,
    staleTime: 24 * 60 * 60 * 1000, // 24 hours
  });
}

/**
 * Get transmissions
 */
export function useTransmissions() {
  return useQuery({
    queryKey: catalogKeys.transmissions(),
    queryFn: getTransmissions,
    staleTime: 24 * 60 * 60 * 1000, // 24 hours
  });
}

/**
 * Get colors
 */
export function useColors() {
  return useQuery({
    queryKey: catalogKeys.colors(),
    queryFn: getColors,
    staleTime: 24 * 60 * 60 * 1000, // 24 hours
  });
}

/**
 * Get provinces
 */
export function useProvinces() {
  return useQuery({
    queryKey: catalogKeys.provinces(),
    queryFn: getProvinces,
    staleTime: 24 * 60 * 60 * 1000, // 24 hours
  });
}

/**
 * Get features by category
 */
export function useFeatures() {
  return useQuery({
    queryKey: catalogKeys.features(),
    queryFn: getFeatures,
    staleTime: 24 * 60 * 60 * 1000, // 24 hours
  });
}

// =============================================================================
// VIN DECODE HOOKS
// =============================================================================

export const vinKeys = {
  all: ['vin'] as const,
  decode: (vin: string) => [...vinKeys.all, 'decode', vin] as const,
  exists: (vin: string) => [...vinKeys.all, 'exists', vin] as const,
  priceSuggestion: (specs: PriceSuggestionRequest) => [...vinKeys.all, 'price', specs] as const,
};

/**
 * Smart VIN decode with catalog matching and duplicate check
 */
export function useDecodeVin(vin: string, options?: { enabled?: boolean }) {
  return useQuery({
    queryKey: vinKeys.decode(vin),
    queryFn: () => decodeVinSmart(vin),
    enabled: options?.enabled !== false && vin.length === 17,
    staleTime: 5 * 60 * 1000, // 5 minutes
    retry: 1,
  });
}

/**
 * Check if VIN exists (fast, for real-time validation)
 */
export function useCheckVinExists(vin: string, options?: { enabled?: boolean }) {
  return useQuery({
    queryKey: vinKeys.exists(vin),
    queryFn: () => checkVinExists(vin),
    enabled: options?.enabled !== false && vin.length === 17,
    staleTime: 30 * 1000, // 30 seconds
  });
}

/**
 * Batch VIN decode (mutation for dealers)
 */
export function useDecodeVinBatch() {
  return useMutation({
    mutationFn: (vins: string[]) => decodeVinBatch(vins),
  });
}

/**
 * Get price suggestion for a vehicle
 */
export function useEstimatePrice(specs: PriceSuggestionRequest, options?: { enabled?: boolean }) {
  return useQuery({
    queryKey: vinKeys.priceSuggestion(specs),
    queryFn: () => getPriceSuggestion(specs),
    enabled: options?.enabled !== false && !!specs.make && !!specs.model && !!specs.year,
    staleTime: 10 * 60 * 1000, // 10 minutes
    retry: 1,
  });
}
