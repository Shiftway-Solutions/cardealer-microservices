/**
 * Alerts Hook - React hook for price alerts and saved searches
 *
 * Provides:
 * - Price alert management
 * - Saved search management
 * - Alert statistics
 */

'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  alertService,
  type PriceAlert,
  type SavedSearch,
  type CreatePriceAlertRequest,
  type UpdatePriceAlertRequest,
  type CreateSavedSearchRequest,
  type UpdateSavedSearchRequest,
  type AlertStats,
} from '@/services/alerts';
import { useAuth } from './use-auth';

// =============================================================================
// QUERY KEYS
// =============================================================================

export const alertKeys = {
  all: ['alerts'] as const,
  // Price alerts
  priceAlerts: () => [...alertKeys.all, 'price'] as const,
  priceAlertsList: (params: object) => [...alertKeys.priceAlerts(), 'list', params] as const,
  priceAlertDetail: (id: string) => [...alertKeys.priceAlerts(), 'detail', id] as const,
  priceAlertForVehicle: (vehicleId: string) =>
    [...alertKeys.priceAlerts(), 'vehicle', vehicleId] as const,
  priceHistory: (vehicleId: string) => [...alertKeys.priceAlerts(), 'history', vehicleId] as const,
  // Saved searches
  savedSearches: () => [...alertKeys.all, 'searches'] as const,
  savedSearchesList: (params: object) => [...alertKeys.savedSearches(), 'list', params] as const,
  savedSearchDetail: (id: string) => [...alertKeys.savedSearches(), 'detail', id] as const,
  savedSearchMatches: (id: string) => [...alertKeys.savedSearches(), 'matches', id] as const,
  // Stats
  stats: () => [...alertKeys.all, 'stats'] as const,
};

// =============================================================================
// PRICE ALERT HOOKS
// =============================================================================

/**
 * Get user's price alerts
 */
export function usePriceAlerts(
  params: { isActive?: boolean; page?: number; pageSize?: number } = {}
) {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: alertKeys.priceAlertsList(params),
    queryFn: () => alertService.getPriceAlerts(params),
    enabled: isAuthenticated,
    staleTime: 2 * 60 * 1000,
  });
}

/**
 * Get price alert by ID
 */
export function usePriceAlert(id: string | undefined) {
  return useQuery({
    queryKey: alertKeys.priceAlertDetail(id || ''),
    queryFn: () => alertService.getPriceAlertById(id!),
    enabled: !!id,
    staleTime: 5 * 60 * 1000,
  });
}

/**
 * Get price alert for a specific vehicle
 */
export function usePriceAlertForVehicle(vehicleId: string | undefined) {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: alertKeys.priceAlertForVehicle(vehicleId || ''),
    queryFn: () => alertService.getPriceAlertForVehicle(vehicleId!),
    enabled: !!vehicleId && isAuthenticated,
    staleTime: 5 * 60 * 1000,
  });
}

/**
 * Get price history for a vehicle
 */
export function useVehiclePriceHistory(vehicleId: string | undefined) {
  return useQuery({
    queryKey: alertKeys.priceHistory(vehicleId || ''),
    queryFn: () => alertService.getVehiclePriceHistory(vehicleId!),
    enabled: !!vehicleId,
    staleTime: 10 * 60 * 1000,
  });
}

/**
 * Create price alert mutation
 */
export function useCreatePriceAlert() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreatePriceAlertRequest) => alertService.createPriceAlert(data),
    onSuccess: newAlert => {
      queryClient.invalidateQueries({ queryKey: alertKeys.priceAlerts() });
      queryClient.setQueryData(alertKeys.priceAlertDetail(newAlert.id), newAlert);
      queryClient.setQueryData(alertKeys.priceAlertForVehicle(newAlert.vehicleId), newAlert);
      queryClient.invalidateQueries({ queryKey: alertKeys.stats() });
    },
  });
}

/**
 * Update price alert mutation
 */
export function useUpdatePriceAlert() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdatePriceAlertRequest }) =>
      alertService.updatePriceAlert(id, data),
    onSuccess: updated => {
      queryClient.setQueryData(alertKeys.priceAlertDetail(updated.id), updated);
      queryClient.setQueryData(alertKeys.priceAlertForVehicle(updated.vehicleId), updated);
      queryClient.invalidateQueries({ queryKey: alertKeys.priceAlerts() });
    },
  });
}

/**
 * Delete price alert mutation
 */
export function useDeletePriceAlert() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => alertService.deletePriceAlert(id),
    onSuccess: (_, id) => {
      queryClient.removeQueries({ queryKey: alertKeys.priceAlertDetail(id) });
      queryClient.invalidateQueries({ queryKey: alertKeys.priceAlerts() });
      queryClient.invalidateQueries({ queryKey: alertKeys.stats() });
    },
  });
}

/**
 * Toggle price alert mutation
 */
export function useTogglePriceAlert() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => alertService.togglePriceAlert(id),
    onSuccess: updated => {
      queryClient.setQueryData(alertKeys.priceAlertDetail(updated.id), updated);
      queryClient.invalidateQueries({ queryKey: alertKeys.priceAlerts() });
      queryClient.invalidateQueries({ queryKey: alertKeys.stats() });
    },
  });
}

// =============================================================================
// SAVED SEARCH HOOKS
// =============================================================================

/**
 * Get user's saved searches
 */
export function useSavedSearches(
  params: { isActive?: boolean; page?: number; pageSize?: number } = {}
) {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: alertKeys.savedSearchesList(params),
    queryFn: () => alertService.getSavedSearches(params),
    enabled: isAuthenticated,
    staleTime: 2 * 60 * 1000,
    // Don't retry on 401/403 — these indicate auth issues, not transient failures
    retry: (failureCount, error) => {
      const status = (error as { response?: { status?: number } })?.response?.status;
      if (status === 401 || status === 403) return false;
      return failureCount < 2;
    },
  });
}

/**
 * Get saved search by ID
 */
export function useSavedSearch(id: string | undefined) {
  return useQuery({
    queryKey: alertKeys.savedSearchDetail(id || ''),
    queryFn: () => alertService.getSavedSearchById(id!),
    enabled: !!id,
    staleTime: 5 * 60 * 1000,
  });
}

/**
 * Get saved search matches
 */
export function useSavedSearchMatches(
  id: string | undefined,
  params: { page?: number; pageSize?: number } = {}
) {
  return useQuery({
    queryKey: alertKeys.savedSearchMatches(id || ''),
    queryFn: () => alertService.getSavedSearchMatches(id!, params),
    enabled: !!id,
    staleTime: 5 * 60 * 1000,
  });
}

/**
 * Create saved search mutation
 */
export function useCreateSavedSearch() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateSavedSearchRequest) => alertService.createSavedSearch(data),
    onSuccess: newSearch => {
      queryClient.invalidateQueries({ queryKey: alertKeys.savedSearches() });
      queryClient.setQueryData(alertKeys.savedSearchDetail(newSearch.id), newSearch);
      queryClient.invalidateQueries({ queryKey: alertKeys.stats() });
    },
  });
}

/**
 * Update saved search mutation
 */
export function useUpdateSavedSearch() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateSavedSearchRequest }) =>
      alertService.updateSavedSearch(id, data),
    onSuccess: updated => {
      queryClient.setQueryData(alertKeys.savedSearchDetail(updated.id), updated);
      queryClient.invalidateQueries({ queryKey: alertKeys.savedSearches() });
    },
  });
}

/**
 * Delete saved search mutation
 */
export function useDeleteSavedSearch() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => alertService.deleteSavedSearch(id),
    onSuccess: (_, id) => {
      queryClient.removeQueries({ queryKey: alertKeys.savedSearchDetail(id) });
      queryClient.invalidateQueries({ queryKey: alertKeys.savedSearches() });
      queryClient.invalidateQueries({ queryKey: alertKeys.stats() });
    },
  });
}

/**
 * Toggle saved search mutation
 */
export function useToggleSavedSearch() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => alertService.toggleSavedSearch(id),
    onSuccess: updated => {
      queryClient.setQueryData(alertKeys.savedSearchDetail(updated.id), updated);
      queryClient.invalidateQueries({ queryKey: alertKeys.savedSearches() });
      queryClient.invalidateQueries({ queryKey: alertKeys.stats() });
    },
  });
}

/**
 * Mark matches as seen mutation
 */
export function useMarkMatchesAsSeen() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => alertService.markMatchesAsSeen(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: alertKeys.savedSearchDetail(id) });
      queryClient.invalidateQueries({ queryKey: alertKeys.savedSearchMatches(id) });
      queryClient.invalidateQueries({ queryKey: alertKeys.stats() });
    },
  });
}

/**
 * Run saved search mutation
 */
export function useRunSavedSearch() {
  return useMutation({
    mutationFn: (id: string) => alertService.runSavedSearch(id),
  });
}

// =============================================================================
// STATS HOOK
// =============================================================================

/**
 * Get alert statistics
 */
export function useAlertStats() {
  const { isAuthenticated } = useAuth();

  return useQuery({
    queryKey: alertKeys.stats(),
    queryFn: () => alertService.getAlertStats(),
    enabled: isAuthenticated,
    staleTime: 1 * 60 * 1000, // 1 minute
    // Don't retry on 401/403 — these indicate auth/service issues, not transient failures
    retry: (failureCount, error) => {
      const status = (error as { response?: { status?: number } })?.response?.status;
      if (status === 401 || status === 403) return false;
      return failureCount < 2;
    },
  });
}

// =============================================================================
// UTILITY FUNCTIONS (re-exported from service)
// =============================================================================

export const {
  formatPriceChange,
  formatPercentageChange,
  getChangeColor,
  formatNotifyFrequency,
  buildSearchDescription,
} = alertService;

// =============================================================================
// EXPORTS
// =============================================================================

export type {
  PriceAlert,
  SavedSearch,
  AlertStats,
  CreatePriceAlertRequest,
  UpdatePriceAlertRequest,
  CreateSavedSearchRequest,
  UpdateSavedSearchRequest,
};
