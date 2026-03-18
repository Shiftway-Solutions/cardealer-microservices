/**
 * Dealer Settings Hooks
 *
 * React Query hooks for dealer settings operations
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getDealerSettings,
  updateNotificationSettings,
  updateSecuritySettings,
  enableTwoFactor,
  disableTwoFactor,
  changePassword,
  type NotificationSettings,
  type SecuritySettings,
} from '@/services/dealer-settings';

// ============================================================================
// Query Keys
// ============================================================================

export const dealerSettingsKeys = {
  all: ['dealer-settings'] as const,
  settings: (dealerId: string) => [...dealerSettingsKeys.all, dealerId] as const,
  notifications: (dealerId: string) =>
    [...dealerSettingsKeys.settings(dealerId), 'notifications'] as const,
  security: (dealerId: string) => [...dealerSettingsKeys.settings(dealerId), 'security'] as const,
};

// ============================================================================
// Query Hooks
// ============================================================================

/**
 * Get dealer settings
 */
export function useDealerSettings(dealerId: string) {
  return useQuery({
    queryKey: dealerSettingsKeys.settings(dealerId),
    queryFn: () => getDealerSettings(dealerId),
    enabled: !!dealerId,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

// ============================================================================
// Mutation Hooks
// ============================================================================

/**
 * Update notification settings
 */
export function useUpdateNotificationSettings(dealerId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (settings: NotificationSettings) => updateNotificationSettings(dealerId, settings),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: dealerSettingsKeys.settings(dealerId),
      });
    },
  });
}

/**
 * Update security settings
 */
export function useUpdateSecuritySettings(dealerId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (settings: Partial<SecuritySettings>) => updateSecuritySettings(dealerId, settings),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: dealerSettingsKeys.settings(dealerId),
      });
    },
  });
}

/**
 * Enable two-factor authentication
 */
export function useEnableTwoFactor(dealerId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => enableTwoFactor(dealerId),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: dealerSettingsKeys.settings(dealerId),
      });
    },
  });
}

/**
 * Disable two-factor authentication
 */
export function useDisableTwoFactor(dealerId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (code: string) => disableTwoFactor(dealerId, code),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: dealerSettingsKeys.settings(dealerId),
      });
    },
  });
}

/**
 * Change password
 */
export function useChangePassword(dealerId: string) {
  return useMutation({
    mutationFn: ({
      currentPassword,
      newPassword,
    }: {
      currentPassword: string;
      newPassword: string;
    }) => changePassword(dealerId, currentPassword, newPassword),
  });
}
