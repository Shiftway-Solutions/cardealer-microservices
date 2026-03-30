/**
 * KYC Configuration API Route
 *
 * Server-side proxy to ConfigurationService for KYC bypass settings.
 * This route does NOT require authentication - KYC config is needed
 * by the frontend to determine if verification gates should be shown.
 * Caches data for 60 seconds to reduce backend calls.
 *
 * GET /api/kyc-config → Returns KYC bypass configuration
 */

import { NextResponse } from 'next/server';

// BFF pattern: server-side API routes use INTERNAL_API_URL for direct internal Gateway calls
const API_URL =
  process.env.INTERNAL_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:18443';
// In production, ALWAYS route through the Gateway. Direct microservice access is
// only allowed in development via explicit CONFIGURATION_SERVICE_URL env var.
const DIRECT_CONFIG_URL =
  process.env.NODE_ENV === 'development' ? process.env.CONFIGURATION_SERVICE_URL || null : null;

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

// Default configuration — KYC bypassed in development when ConfigurationService is unavailable
const DEFAULT_CONFIG: KYCBypassConfig = {
  bypassForIndividualSeller: process.env.NODE_ENV === 'development',
  bypassForDealer: process.env.NODE_ENV === 'development',
  requireKycIndividualSeller: process.env.NODE_ENV !== 'development',
  requireKycDealer: process.env.NODE_ENV !== 'development',
  allowSaleWithoutKyc: false,
};

// Simple in-memory cache
let cachedConfig: KYCBypassConfig | null = null;
let cacheTimestamp = 0;
const CACHE_TTL_MS = 60_000; // 60 seconds

/**
 * Parse configuration items from the backend into KYCBypassConfig
 */
function parseConfigItems(items: Array<{ key: string; value: string }>): Partial<KYCBypassConfig> {
  const result: Partial<KYCBypassConfig> = {};

  for (const item of items) {
    switch (item.key) {
      case 'kyc.bypass_for_individual_seller':
        result.bypassForIndividualSeller = item.value === 'true';
        break;
      case 'kyc.bypass_for_dealer':
        result.bypassForDealer = item.value === 'true';
        break;
      case 'vehicles.require_kyc_individual_seller':
        result.requireKycIndividualSeller = item.value === 'true';
        break;
      case 'vehicles.require_kyc_dealer':
        result.requireKycDealer = item.value === 'true';
        break;
      case 'vehicles.allow_sale_without_kyc':
        result.allowSaleWithoutKyc = item.value === 'true';
        break;
    }
  }

  return result;
}

export async function GET() {
  try {
    // Return cached data if fresh
    const now = Date.now();
    if (cachedConfig && now - cacheTimestamp < CACHE_TTL_MS) {
      return NextResponse.json(cachedConfig, {
        headers: {
          'Cache-Control': 'public, max-age=60, s-maxage=60',
          'X-Cache': 'HIT',
        },
      });
    }

    let config: KYCBypassConfig | null = null;

    // Try direct connection to ConfigurationService (development only)
    if (DIRECT_CONFIG_URL) {
      try {
        const keys = [
          'kyc.bypass_for_individual_seller',
          'kyc.bypass_for_dealer',
          'vehicles.require_kyc_individual_seller',
          'vehicles.require_kyc_dealer',
          'vehicles.allow_sale_without_kyc',
        ];

        const directResponse = await fetch(
          `${DIRECT_CONFIG_URL}/api/configurations?environment=Development`,
          {
            next: { revalidate: 60 },
            signal: AbortSignal.timeout(5000),
          }
        );

        if (directResponse.ok) {
          const allConfigs: Array<{ key: string; value: string }> = await directResponse.json();
          const relevantConfigs = allConfigs.filter(c => keys.includes(c.key));
          const parsed = parseConfigItems(relevantConfigs);
          config = { ...DEFAULT_CONFIG, ...parsed };
        }
      } catch {
        console.warn('[KYC Config API] Direct ConfigurationService unavailable, trying Gateway...');
      }
    }

    // Fallback: try via Gateway
    if (!config) {
      try {
        const gatewayResponse = await fetch(
          `${API_URL}/api/configurations?environment=Development`,
          {
            next: { revalidate: 60 },
            signal: AbortSignal.timeout(5000),
          }
        );

        if (gatewayResponse.ok) {
          const allConfigs: Array<{ key: string; value: string }> = await gatewayResponse.json();
          const keys = [
            'kyc.bypass_for_individual_seller',
            'kyc.bypass_for_dealer',
            'vehicles.require_kyc_individual_seller',
            'vehicles.require_kyc_dealer',
            'vehicles.allow_sale_without_kyc',
          ];
          const relevantConfigs = allConfigs.filter(c => keys.includes(c.key));
          const parsed = parseConfigItems(relevantConfigs);
          config = { ...DEFAULT_CONFIG, ...parsed };
        }
      } catch {
        console.warn('[KYC Config API] Gateway also unavailable, using defaults');
      }
    }

    // Use fetched config or defaults
    const result = config || DEFAULT_CONFIG;

    // Update cache
    cachedConfig = result;
    cacheTimestamp = now;

    return NextResponse.json(result, {
      headers: {
        'Cache-Control': 'public, max-age=60, s-maxage=60',
        'X-Cache': config ? 'MISS' : 'DEFAULT',
      },
    });
  } catch (error) {
    console.error('[KYC Config API] Error:', error);
    return NextResponse.json(DEFAULT_CONFIG, {
      headers: {
        'Cache-Control': 'public, max-age=30',
        'X-Cache': 'ERROR',
      },
    });
  }
}
