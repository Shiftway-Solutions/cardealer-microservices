/**
 * Public Pricing API Route
 *
 * Server-side proxy to ConfigurationService for platform pricing.
 * This route does NOT require authentication - prices are public data.
 * Caches pricing data for 60 seconds to reduce backend calls.
 *
 * GET /api/pricing → Returns platform pricing configuration
 */

import { NextResponse } from 'next/server';

// BFF pattern: server-side API routes use INTERNAL_API_URL for direct internal Gateway calls
const API_URL =
  process.env.INTERNAL_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:18443';
// In production, ALWAYS route through the Gateway. Direct microservice access is
// only allowed in development via explicit CONFIGURATION_SERVICE_URL env var.
const DIRECT_CONFIG_URL =
  process.env.NODE_ENV === 'development' ? process.env.CONFIGURATION_SERVICE_URL || null : null;

// Simple in-memory cache
let cachedPricing: PlatformPricing | null = null;
let cacheTimestamp = 0;
const CACHE_TTL_MS = 60_000; // 60 seconds

export interface PlatformPricing {
  // Publicaciones
  basicListing: number;
  featuredListing: number;
  premiumListing: number;
  sellerPremiumPrice: number;
  individualListingPrice: number;
  // Planes Dealer (4 niveles)
  dealerLibre: number;
  dealerVisible: number;
  dealerPro: number;
  dealerElite: number;
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
  proMaxPhotos: number;
  eliteMaxPhotos: number;
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

// Default pricing fallback
const DEFAULT_PRICING: PlatformPricing = {
  // Publicaciones
  basicListing: 0,
  featuredListing: 1499,
  premiumListing: 2999,
  sellerPremiumPrice: 1699,
  individualListingPrice: 1699,
  // Planes Dealer (DOP mensuales) - LIBRE/VISIBLE/PRO/ÉLITE
  dealerLibre: 0,
  dealerVisible: 1699,
  dealerPro: 5199,
  dealerElite: 11599,
  // Planes Seller Individual (DOP mensuales) - GRATIS/PREMIUM/PRO
  sellerGratis: 0,
  sellerPremium: 1699,
  sellerProPlan: 3499,
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
  freeMaxPhotos: 10,
  visibleMaxPhotos: 25,
  proMaxPhotos: 40,
  eliteMaxPhotos: 50,
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

export async function GET() {
  try {
    // Return cached data if fresh
    const now = Date.now();
    if (cachedPricing && now - cacheTimestamp < CACHE_TTL_MS) {
      return NextResponse.json(cachedPricing, {
        headers: {
          'Cache-Control': 'public, max-age=60, s-maxage=60',
          'X-Cache': 'HIT',
        },
      });
    }

    // In production, always go through Gateway. Direct is dev-only optimization.
    let pricing: PlatformPricing | null = null;

    if (DIRECT_CONFIG_URL) {
      try {
        const directResponse = await fetch(`${DIRECT_CONFIG_URL}/api/public/pricing`, {
          next: { revalidate: 60 },
          signal: AbortSignal.timeout(5000),
        });

        if (directResponse.ok) {
          pricing = await directResponse.json();
        }
      } catch {
        console.warn('[Pricing API] Direct ConfigurationService unavailable, trying Gateway...');
      }
    }

    // Fallback: try via Gateway (public endpoint, no auth)
    if (!pricing) {
      try {
        const gatewayResponse = await fetch(`${API_URL}/api/public/pricing`, {
          next: { revalidate: 60 },
          signal: AbortSignal.timeout(5000),
        });

        if (gatewayResponse.ok) {
          pricing = await gatewayResponse.json();
        }
      } catch {
        console.warn('[Pricing API] Gateway also unavailable, using defaults');
      }
    }

    // Use fetched pricing or defaults
    const result = pricing || DEFAULT_PRICING;

    // Update cache
    cachedPricing = result;
    cacheTimestamp = now;

    return NextResponse.json(result, {
      headers: {
        'Cache-Control': 'public, max-age=60, s-maxage=60',
        'X-Cache': pricing ? 'MISS' : 'DEFAULT',
      },
    });
  } catch (error) {
    console.error('[Pricing API] Error:', error);
    return NextResponse.json(DEFAULT_PRICING, {
      headers: {
        'Cache-Control': 'public, max-age=30',
        'X-Cache': 'ERROR',
      },
    });
  }
}
