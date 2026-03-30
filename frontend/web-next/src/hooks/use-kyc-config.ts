/**
 * KYC Configuration Hook
 *
 * Fetches platform KYC bypass settings from the ConfigurationService
 * via the server-side /api/kyc-config route.
 *
 * These settings control whether individual sellers and/or dealers
 * can operate on the platform without completing KYC verification.
 *
 * Usage:
 *   const { config, isLoading } = useKycConfig();
 *   if (config.bypassForIndividualSeller) { ... }
 */

'use client';

import { useState, useEffect, useCallback } from 'react';

// =============================================================================
// TYPES
// =============================================================================

export interface KYCBypassConfig {
  /** Whether individual sellers can skip KYC to publish vehicles */
  bypassForIndividualSeller: boolean;
  /** Whether dealers can skip KYC to publish vehicles */
  bypassForDealer: boolean;
  /** Whether KYC is required for individual sellers to publish */
  requireKycIndividualSeller: boolean;
  /** Whether KYC is required for dealers to publish */
  requireKycDealer: boolean;
  /** Whether sales can happen without seller KYC validation */
  allowSaleWithoutKyc: boolean;
}

// Default configuration — KYC bypassed in development when ConfigurationService unavailable
const DEFAULT_CONFIG: KYCBypassConfig = {
  bypassForIndividualSeller: process.env.NODE_ENV === 'development',
  bypassForDealer: process.env.NODE_ENV === 'development',
  requireKycIndividualSeller: process.env.NODE_ENV !== 'development',
  requireKycDealer: process.env.NODE_ENV !== 'development',
  allowSaleWithoutKyc: false,
};

// Module-level cache shared across all hook instances
let _cachedConfig: KYCBypassConfig | null = null;
let _cacheTimestamp = 0;
const CACHE_TTL_MS = 60_000; // 60 seconds
let _pendingFetch: Promise<KYCBypassConfig> | null = null;

// =============================================================================
// FETCH FUNCTION
// =============================================================================

async function fetchKycConfig(): Promise<KYCBypassConfig> {
  const now = Date.now();

  // Return cached data if fresh
  if (_cachedConfig && now - _cacheTimestamp < CACHE_TTL_MS) {
    return _cachedConfig;
  }

  // Deduplicate concurrent fetches
  if (_pendingFetch) {
    return _pendingFetch;
  }

  _pendingFetch = (async () => {
    try {
      const response = await fetch('/api/kyc-config', {
        signal: AbortSignal.timeout(5000),
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch KYC config: ${response.status}`);
      }

      const data: KYCBypassConfig = await response.json();

      // Update cache
      _cachedConfig = data;
      _cacheTimestamp = Date.now();

      return data;
    } catch (error) {
      console.warn('[useKycConfig] Failed to fetch, using defaults:', error);
      return _cachedConfig || DEFAULT_CONFIG;
    } finally {
      _pendingFetch = null;
    }
  })();

  return _pendingFetch;
}

// =============================================================================
// HELPER
// =============================================================================

/**
 * Determines if KYC is bypassed for a given account type.
 *
 * @param config - The KYC bypass configuration
 * @param accountType - The user's account type
 * @returns true if the user can skip KYC verification
 */
export function isKycBypassedForAccountType(
  config: KYCBypassConfig,
  accountType?: string
): boolean {
  if (!accountType) return false;

  // Individual sellers (buyer who wants to sell, or explicit seller)
  if (accountType === 'seller' || accountType === 'buyer') {
    return (
      config.bypassForIndividualSeller ||
      !config.requireKycIndividualSeller ||
      config.allowSaleWithoutKyc
    );
  }

  // Dealers and dealer employees
  if (accountType === 'dealer' || accountType === 'dealer_employee') {
    return config.bypassForDealer || !config.requireKycDealer || config.allowSaleWithoutKyc;
  }

  // Admins and platform employees don't need KYC
  if (accountType === 'admin' || accountType === 'platform_employee') {
    return true;
  }

  return false;
}

// =============================================================================
// HOOK
// =============================================================================

export function useKycConfig() {
  const [config, setConfig] = useState<KYCBypassConfig>(_cachedConfig || DEFAULT_CONFIG);
  const [isLoading, setIsLoading] = useState(!_cachedConfig);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      _cacheTimestamp = 0;
      const data = await fetchKycConfig();
      setConfig(data);
    } catch {
      setError('Error al cargar configuración KYC');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    let mounted = true;

    async function loadConfig() {
      try {
        const data = await fetchKycConfig();
        if (mounted) {
          setConfig(data);
          setIsLoading(false);
        }
      } catch {
        if (mounted) {
          setError('Error al cargar configuración KYC');
          setIsLoading(false);
        }
      }
    }

    loadConfig();

    return () => {
      mounted = false;
    };
  }, []);

  return { config, isLoading, error, refresh };
}

export default useKycConfig;
