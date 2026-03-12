/**
 * React Query hooks for Image Health Dashboard
 * Provides data fetching and mutations for admin image health monitoring
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getImageHealthDashboard,
  triggerImageHealthScan,
  flagListingForAttention,
  type FlagListingRequest,
} from '@/services/image-health';

// =============================================================================
// QUERY KEYS
// =============================================================================

export const imageHealthKeys = {
  all: ['image-health'] as const,
  dashboard: () => [...imageHealthKeys.all, 'dashboard'] as const,
};

// =============================================================================
// HOOKS
// =============================================================================

/**
 * Fetches image health dashboard data with 30s auto-refresh.
 */
export function useImageHealthDashboard() {
  return useQuery({
    queryKey: imageHealthKeys.dashboard(),
    queryFn: getImageHealthDashboard,
    staleTime: 20_000,
    refetchInterval: 30_000,
    refetchIntervalInBackground: false,
  });
}

/**
 * Mutation to trigger a manual image health scan.
 */
export function useTriggerScan() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: triggerImageHealthScan,
    onSuccess: () => {
      // Invalidate dashboard data after triggering scan
      // The scan takes a few minutes, so data will update on next refetch
      setTimeout(() => {
        queryClient.invalidateQueries({ queryKey: imageHealthKeys.dashboard() });
      }, 60_000);
    },
  });
}

/**
 * Mutation to flag a listing as "requires dealer attention".
 */
export function useFlagListing() {
  return useMutation({
    mutationFn: (request: FlagListingRequest) => flagListingForAttention(request),
  });
}
