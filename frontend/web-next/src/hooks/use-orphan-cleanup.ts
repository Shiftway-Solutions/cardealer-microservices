/**
 * React Query hooks for Orphan Image Cleanup
 * Provides data fetching and mutations for admin orphan cleanup management
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getOrphanCleanupStatus,
  approveOrphanCleanup,
  dismissOrphanReport,
} from '@/services/orphan-cleanup';

// =============================================================================
// QUERY KEYS
// =============================================================================

export const orphanCleanupKeys = {
  all: ['orphan-cleanup'] as const,
  status: () => [...orphanCleanupKeys.all, 'status'] as const,
};

// =============================================================================
// HOOKS
// =============================================================================

/**
 * Fetches orphan cleanup status with 30s auto-refresh.
 */
export function useOrphanCleanupStatus() {
  return useQuery({
    queryKey: orphanCleanupKeys.status(),
    queryFn: getOrphanCleanupStatus,
    staleTime: 20_000,
    refetchInterval: 30_000,
    refetchIntervalInBackground: false,
  });
}

/**
 * Mutation to approve pending orphan cleanup.
 */
export function useApproveOrphanCleanup() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: approveOrphanCleanup,
    onSuccess: () => {
      // Re-fetch status — it will show the report was consumed
      queryClient.invalidateQueries({ queryKey: orphanCleanupKeys.status() });
    },
  });
}

/**
 * Mutation to dismiss the pending orphan report.
 */
export function useDismissOrphanReport() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: dismissOrphanReport,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: orphanCleanupKeys.status() });
    },
  });
}
