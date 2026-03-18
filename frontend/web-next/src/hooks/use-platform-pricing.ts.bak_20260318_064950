/**
 * Platform Pricing Hook
 *
 * Provides dynamic pricing from ConfigurationService.
 * Fetches pricing through the Next.js /api/pricing route (server-cached).
 * Falls back to defaults if the service is unavailable.
 *
 * Usage:
 *   const { pricing, isLoading, error, formatPrice, calculateWithTax } = usePlatformPricing();
 *   const total = calculateWithTax(pricing.featuredListing);
 */

'use client';

import { useState, useEffect, useCallback, useMemo } from 'react';

// =============================================================================
// TYPES
// =============================================================================

export interface PlatformPricing {
  // Publicaciones
  basicListing: number;
  featuredListing: number;
  premiumListing: number;
  sellerPremiumPrice: number;
  individualListingPrice: number;
  // Planes Dealer (6 niveles)
  dealerLibre: number;
  dealerVisible: number;
  dealerStarter: number;
  dealerPro: number;
  dealerElite: number;
  dealerEnterprise: number;
  // Planes Seller Individual (3 niveles)
  sellerGratis: number;
  sellerPremium: number;
  sellerProPlan: number;
  // Boosts
  boostBasicPrice: number;
  boostBasicDays: number;
  boostProPrice: number;
  boostProDays: number;
  boostPremiumPrice: number;
  boostPremiumDays: number;
  // Duraciones
  basicListingDays: number;
  individualListingDays: number;
  // Límites por plan (fotos)
  freeMaxPhotos: number;
  visibleMaxPhotos: number;
  starterMaxPhotos: number;
  proMaxPhotos: number;
  eliteMaxPhotos: number;
  enterpriseMaxPhotos: number;
  // Comisiones
  platformCommission: number;
  itbisPercentage: number;
  currency: string;
  // Early Bird
  earlyBirdDiscount: number;
  earlyBirdDeadline: string;
  earlyBirdFreeMonths: number;
  // Trial
  stripeTrialDays: number;
}

// Default pricing (fallback when service is unavailable)
const DEFAULT_PRICING: PlatformPricing = {
  // Publicaciones
  basicListing: 0,
  featuredListing: 1499,
  premiumListing: 2999,
  sellerPremiumPrice: 1699,
  individualListingPrice: 1699,
  // Planes Dealer (DOP mensuales) - LIBRE/VISIBLE/STARTER/PRO/ÉLITE/ENTERPRISE
  dealerLibre: 0,
  dealerVisible: 1699,
  dealerStarter: 3499,
  dealerPro: 5799,
  dealerElite: 20299,
  dealerEnterprise: 34999,
  // Planes Seller Individual (DOP) - LIBRE(gratis)/ESTÁNDAR(579/listing)/VERIFICADO(1999/mes)
  sellerGratis: 0,
  sellerPremium: 579,
  sellerProPlan: 1999,
  // Boosts
  boostBasicPrice: 499,
  boostBasicDays: 3,
  boostProPrice: 999,
  boostProDays: 7,
  boostPremiumPrice: 1999,
  boostPremiumDays: 14,
  // Duraciones
  basicListingDays: 30,
  individualListingDays: 45,
  // Límites por plan (fotos por vehículo)
  freeMaxPhotos: 5,
  visibleMaxPhotos: 10,
  starterMaxPhotos: 12,
  proMaxPhotos: 15,
  eliteMaxPhotos: 20,
  enterpriseMaxPhotos: 20,
  // Comisiones
  platformCommission: 2.5,
  itbisPercentage: 18,
  currency: 'DOP',
  // Early Bird
  earlyBirdDiscount: 25,
  earlyBirdDeadline: '2026-12-31',
  earlyBirdFreeMonths: 2,
  // Trial
  stripeTrialDays: 14,
};

// Module-level cache shared across all hook instances
let _cachedPricing: PlatformPricing | null = null;
let _cacheTimestamp = 0;
const CACHE_TTL_MS = 60_000; // 60 seconds
let _pendingFetch: Promise<PlatformPricing> | null = null;

// =============================================================================
// FETCH FUNCTION
// =============================================================================

async function fetchPlatformPricing(): Promise<PlatformPricing> {
  const now = Date.now();

  // Return cached data if fresh
  if (_cachedPricing && now - _cacheTimestamp < CACHE_TTL_MS) {
    return _cachedPricing;
  }

  // Deduplicate concurrent fetches
  if (_pendingFetch) {
    return _pendingFetch;
  }

  _pendingFetch = (async () => {
    try {
      const response = await fetch('/api/pricing', {
        signal: AbortSignal.timeout(5000),
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch pricing: ${response.status}`);
      }

      const data: PlatformPricing = await response.json();

      // Update cache
      _cachedPricing = data;
      _cacheTimestamp = Date.now();

      return data;
    } catch (error) {
      console.warn('[usePlatformPricing] Failed to fetch, using defaults:', error);
      return _cachedPricing || DEFAULT_PRICING;
    } finally {
      _pendingFetch = null;
    }
  })();

  return _pendingFetch;
}

// =============================================================================
// HOOK
// =============================================================================

export function usePlatformPricing() {
  const [pricing, setPricing] = useState<PlatformPricing>(_cachedPricing || DEFAULT_PRICING);
  const [isLoading, setIsLoading] = useState(!_cachedPricing);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      // Invalidate cache to force refetch
      _cacheTimestamp = 0;
      const data = await fetchPlatformPricing();
      setPricing(data);
    } catch (err) {
      setError('Error al cargar precios');
      console.error('[usePlatformPricing] refresh error:', err);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    let mounted = true;

    async function loadPricing() {
      try {
        const data = await fetchPlatformPricing();
        if (mounted) {
          setPricing(data);
          setIsLoading(false);
        }
      } catch {
        if (mounted) {
          setError('Error al cargar precios');
          setIsLoading(false);
        }
      }
    }

    loadPricing();

    return () => {
      mounted = false;
    };
  }, []);

  // Helper: Format price with currency symbol
  const formatPrice = useCallback(
    (amount: number, currencyOverride?: string) => {
      const curr = currencyOverride || pricing.currency;
      if (curr === 'USD') {
        return `$${amount.toLocaleString('en-US')}`;
      }
      return `RD$${amount.toLocaleString('es-DO')}`;
    },
    [pricing.currency]
  );

  // Helper: Calculate ITBIS tax
  const calculateTax = useCallback(
    (subtotal: number): number => {
      return Math.round(subtotal * (pricing.itbisPercentage / 100));
    },
    [pricing.itbisPercentage]
  );

  // Helper: Calculate total with ITBIS
  const calculateWithTax = useCallback(
    (subtotal: number): number => {
      return subtotal + calculateTax(subtotal);
    },
    [calculateTax]
  );

  // Memoized product pricing map (for checkout integration)
  const productPricing = useMemo(
    () => ({
      'boost-basic': {
        price: pricing.featuredListing,
        currency: pricing.currency,
      },
      'boost-premium': {
        price: pricing.premiumListing,
        currency: pricing.currency,
      },
      'dealer-libre': {
        price: pricing.dealerLibre,
        currency: pricing.currency,
      },
      'dealer-visible': {
        price: pricing.dealerVisible,
        currency: pricing.currency,
      },
      'dealer-pro': {
        price: pricing.dealerPro,
        currency: pricing.currency,
      },
      'dealer-elite': {
        price: pricing.dealerElite,
        currency: pricing.currency,
      },
      'listing-single': {
        price: pricing.individualListingPrice,
        currency: 'USD' as string,
      },
    }),
    [pricing]
  );

  return {
    pricing,
    isLoading,
    error,
    refresh,
    formatPrice,
    calculateTax,
    calculateWithTax,
    productPricing,
    itbisPercentage: pricing.itbisPercentage,
    currency: pricing.currency,
  };
}

// =============================================================================
// SERVER-SIDE HELPER (for Server Components)
// =============================================================================

/**
 * Fetch pricing on the server side (for Server Components / getServerSideProps)
 */
export async function getServerPricing(): Promise<PlatformPricing> {
  // BFF pattern: server-side uses INTERNAL_API_URL for direct internal Gateway calls
  const API_URL =
    process.env.INTERNAL_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:18443';
  const DIRECT_CONFIG_URL =
    process.env.NODE_ENV === 'development' ? process.env.CONFIGURATION_SERVICE_URL || null : null;

  // Try direct (dev only)
  if (DIRECT_CONFIG_URL) {
    try {
      const response = await fetch(`${DIRECT_CONFIG_URL}/api/public/pricing`, {
        next: { revalidate: 60 },
        signal: AbortSignal.timeout(5000),
      });
      if (response.ok) return await response.json();
    } catch {
      console.warn('[getServerPricing] Direct ConfigurationService unavailable, trying Gateway...');
    }
  }

  // Always try Gateway
  try {
    const response = await fetch(`${API_URL}/api/public/pricing`, {
      next: { revalidate: 60 },
      signal: AbortSignal.timeout(5000),
    });
    if (response.ok) return await response.json();
  } catch {
    console.warn('[getServerPricing] Gateway unavailable, using defaults');
  }

  return DEFAULT_PRICING;
}

export { DEFAULT_PRICING };
