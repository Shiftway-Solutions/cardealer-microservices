/**
 * Plans Catalog API Route — /api/plans
 *
 * Serves the full subscription plan catalog (features + dynamic pricing).
 * Prices are always loaded from /api/pricing (which proxies to AdminService).
 * Feature definitions are stored here and can be overridden via POST (admin).
 *
 * GET  /api/plans                  → full catalog { dealer: [], seller: [] }
 * GET  /api/plans?audience=seller  → seller plans only
 * GET  /api/plans?audience=dealer  → dealer plans only
 * POST /api/plans                  → admin: save plan feature overrides
 */

import { NextRequest, NextResponse } from 'next/server';

const API_URL =
  process.env.INTERNAL_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:18443';

// =============================================================================
// TYPES
// =============================================================================

export interface PlanFeature {
  label: string;
  value?: string;
  included: boolean;
}

export interface PublicPlan {
  key: string;
  name: string;
  description: string;
  /** Price in the billing currency (DOP for dealers, USD-equivalent for sellers) */
  monthlyPrice: number;
  /** For seller Estándar: price per listing. For others: monthly */
  annualPrice: number;
  currency: 'DOP' | 'USD';
  billingType: 'monthly' | 'per_listing' | 'free';
  audience: 'dealer' | 'seller';
  tier: number;
  isPopular: boolean;
  badge: string | null;
  badgeColor?: string;
  features: string[];
  highlightFeatures: PlanFeature[];
}

export interface PlansCatalog {
  dealer: PublicPlan[];
  seller: PublicPlan[];
  /** ISO timestamp of when this catalog was last updated */
  updatedAt: string;
}

// =============================================================================
// IN-MEMORY FEATURE OVERRIDE STORE (admin-configurable)
// =============================================================================
// Admin can POST to /api/plans to update feature descriptions per plan.
// Prices always come live from /api/pricing (AdminService-backed).

let _featureOverrides: Record<string, string[]> | null = null;
let _catalogCacheTimestamp = 0;
const CATALOG_CACHE_TTL_MS = 60_000;
let _cachedCatalog: PlansCatalog | null = null;

// =============================================================================
// DEFAULT FEATURE LISTS
// =============================================================================

// v5.0 Dealer feature lists
const DEFAULT_DEALER_FEATURES: Record<string, string[]> = {
  libre: [
    'Publicaciones ilimitadas',
    'Hasta 5 fotos por vehículo',
    'Posición estándar en búsquedas',
    '1 valoración PricingAgent IA gratis',
  ],
  visible: [
    'Publicaciones ilimitadas',
    'Hasta 10 fotos por vehículo',
    'Prioridad media en búsquedas',
    '3 publicaciones destacadas/mes',
    '🔵 Badge Dealer Verificado OKLA',
    '$15 OKLA Coins/mes',
    '5 valoraciones PricingAgent IA/mes',
    'Dashboard Analytics básico',
    'Soporte email',
    '✅ Garantía: 10 consultas en 30 días o mes 2 gratis',
  ],
  starter: [
    'Publicaciones ilimitadas',
    'Hasta 12 fotos por vehículo',
    'Alta prioridad en búsquedas',
    '5 publicaciones destacadas/mes',
    '🔵 Badge Verificado+',
    '$30 OKLA Coins/mes',
    'ChatAgent Web 100 conv/mes',
    'ChatAgent WhatsApp 100 conv/mes',
    'Overage $0.10/conv adicional',
    '10 valoraciones PricingAgent IA/mes',
    'Soporte email — respuesta 48h',
  ],
  pro: [
    'Publicaciones ilimitadas',
    'Hasta 15 fotos por vehículo',
    'Alta prioridad en búsquedas',
    '10 publicaciones destacadas/mes',
    '🥇 Badge Verificado Dorado',
    '$45 OKLA Coins/mes',
    'ChatAgent Web 300 conv/mes',
    'ChatAgent WhatsApp 300 conv/mes',
    'Overage $0.08/conv adicional',
    'Agendamiento automático',
    'PricingAgent IA ilimitado',
    'Dashboard Analytics avanzado',
    'Soporte email — 24h respuesta',
  ],
  elite: [
    'Publicaciones ilimitadas',
    'Hasta 20 fotos + video tour',
    'Top prioridad en búsquedas',
    '25 publicaciones destacadas/mes',
    '💎 Badge Verificado Premium',
    '$120 OKLA Coins/mes',
    'ChatAgent Web 5,000 conv/mes',
    'ChatAgent WhatsApp 5,000 conv/mes',
    'Overage $0.06/conv adicional',
    'Agendamiento + recordatorios WhatsApp',
    'PricingAgent IA ilimitado + PDF',
    'Dashboard Analytics completo + exportar',
    'Gerente de cuenta dedicado',
  ],
  enterprise: [
    'Publicaciones ilimitadas',
    'Hasta 20 fotos + video tour',
    '#1 garantizado en posición de búsqueda',
    '50 publicaciones destacadas/mes',
    '👑 Badge Enterprise',
    '$300 OKLA Coins/mes',
    'ChatAgent IA SIN LÍMITE',
    'Agendamiento + CRM + recordatorios WhatsApp',
    'PricingAgent IA ilimitado + PDF + API',
    'Acceso completo a API OKLA',
    'Dashboard + API + reportes personalizados',
    'Empleados ilimitados',
    'SLA garantizado + Soporte 24/7',
  ],
};

// v5.0 Seller feature lists
const DEFAULT_SELLER_FEATURES: Record<string, string[]> = {
  libre: [
    '1 publicación activa',
    'Hasta 5 fotos por vehículo',
    'Duración: 30 días',
    '⬇ Posición al fondo en búsquedas',
    '⚪ Sin badge de verificación',
    'KYC: solo email',
  ],
  estandar: [
    '1 publicación por pago',
    'Hasta 10 fotos por vehículo',
    'Duración: 60 días',
    '⬆ Posición media (bajo dealers)',
    '🔵 Badge Vendedor OKLA',
    'KYC: email + teléfono verificados',
    'Renovación de listing: $6.99',
    '1 valoración PricingAgent IA por listing',
  ],
  verificado: [
    '3 publicaciones simultáneas',
    'Hasta 12 fotos por vehículo',
    'Duración: 90 días',
    '📈 Alta posición (bajo dealers VISIBLE)',
    '✅ Badge Vendedor Verificado',
    'KYC completo: cédula + selfie + teléfono',
    'Renovación de listing incluida',
    '2 valoraciones PricingAgent IA/mes',
    'Analytics básico de tus publicaciones',
  ],
};

// =============================================================================
// CATALOG BUILDER
// =============================================================================

interface PricingData {
  dealerLibre: number;
  dealerVisible: number;
  dealerStarter: number;
  dealerPro: number;
  dealerElite: number;
  dealerEnterprise: number;
  sellerGratis: number;
  sellerEstandar: number;
  sellerVerificado: number;
}

function buildCatalog(pricing: PricingData): PlansCatalog {
  const feats = _featureOverrides ?? {};

  const dealerPlans: PublicPlan[] = [
    {
      key: 'libre',
      name: 'Libre',
      description: 'Para empezar sin costo',
      monthlyPrice: pricing.dealerLibre,
      annualPrice: pricing.dealerLibre,
      currency: 'DOP',
      billingType: 'free',
      audience: 'dealer',
      tier: 0,
      isPopular: false,
      badge: null,
      features: feats['dealer_libre'] ?? DEFAULT_DEALER_FEATURES['libre'],
      highlightFeatures: [],
    },
    {
      key: 'visible',
      name: 'Visible',
      description: 'Más visibilidad y primeros clientes',
      monthlyPrice: pricing.dealerVisible,
      annualPrice: Math.round(pricing.dealerVisible * 10),
      currency: 'DOP',
      billingType: 'monthly',
      audience: 'dealer',
      tier: 1,
      isPopular: false,
      badge: null,
      features: feats['dealer_visible'] ?? DEFAULT_DEALER_FEATURES['visible'],
      highlightFeatures: [],
    },
    {
      key: 'starter',
      name: 'Starter',
      description: 'Primer paso con ChatAgent IA',
      monthlyPrice: pricing.dealerStarter,
      annualPrice: Math.round(pricing.dealerStarter * 10),
      currency: 'DOP',
      billingType: 'monthly',
      audience: 'dealer',
      tier: 2,
      isPopular: false,
      badge: null,
      features: feats['dealer_starter'] ?? DEFAULT_DEALER_FEATURES['starter'],
      highlightFeatures: [],
    },
    {
      key: 'pro',
      name: 'Pro',
      description: 'Herramientas avanzadas de venta',
      monthlyPrice: pricing.dealerPro,
      annualPrice: Math.round(pricing.dealerPro * 10),
      currency: 'DOP',
      billingType: 'monthly',
      audience: 'dealer',
      tier: 3,
      isPopular: true,
      badge: 'MÁS POPULAR',
      badgeColor: 'emerald',
      features: feats['dealer_pro'] ?? DEFAULT_DEALER_FEATURES['pro'],
      highlightFeatures: [],
    },
    {
      key: 'elite',
      name: 'Élite',
      description: 'Para grandes concesionarios',
      monthlyPrice: pricing.dealerElite,
      annualPrice: Math.round(pricing.dealerElite * 10),
      currency: 'DOP',
      billingType: 'monthly',
      audience: 'dealer',
      tier: 4,
      isPopular: false,
      badge: 'RECOMENDADO',
      badgeColor: 'amber',
      features: feats['dealer_elite'] ?? DEFAULT_DEALER_FEATURES['elite'],
      highlightFeatures: [],
    },
    {
      key: 'enterprise',
      name: 'Enterprise',
      description: 'Grupos automotrices y franquicias',
      monthlyPrice: pricing.dealerEnterprise,
      annualPrice: Math.round(pricing.dealerEnterprise * 10),
      currency: 'DOP',
      billingType: 'monthly',
      audience: 'dealer',
      tier: 5,
      isPopular: false,
      badge: null,
      features: feats['dealer_enterprise'] ?? DEFAULT_DEALER_FEATURES['enterprise'],
      highlightFeatures: [],
    },
  ];

  const sellerPlans: PublicPlan[] = [
    {
      key: 'libre_seller',
      name: 'Libre',
      description: 'Para vender de forma ocasional',
      monthlyPrice: pricing.sellerGratis,
      annualPrice: pricing.sellerGratis,
      currency: 'DOP',
      billingType: 'free',
      audience: 'seller',
      tier: 0,
      isPopular: false,
      badge: null,
      features: feats['seller_libre'] ?? DEFAULT_SELLER_FEATURES['libre'],
      highlightFeatures: [],
    },
    {
      key: 'estandar',
      name: 'Estándar',
      description: 'Publica y vende más rápido',
      monthlyPrice: pricing.sellerEstandar,
      annualPrice: pricing.sellerEstandar,
      currency: 'DOP',
      billingType: 'per_listing',
      audience: 'seller',
      tier: 1,
      isPopular: false,
      badge: null,
      features: feats['seller_estandar'] ?? DEFAULT_SELLER_FEATURES['estandar'],
      highlightFeatures: [],
    },
    {
      key: 'verificado',
      name: 'Verificado',
      description: 'Máxima visibilidad y confianza',
      monthlyPrice: pricing.sellerVerificado,
      annualPrice: pricing.sellerVerificado,
      currency: 'DOP',
      billingType: 'monthly',
      audience: 'seller',
      tier: 2,
      isPopular: true,
      badge: 'MÁS POPULAR',
      badgeColor: 'emerald',
      features: feats['seller_verificado'] ?? DEFAULT_SELLER_FEATURES['verificado'],
      highlightFeatures: [],
    },
  ];

  return {
    dealer: dealerPlans,
    seller: sellerPlans,
    updatedAt: new Date().toISOString(),
  };
}

// =============================================================================
// GET — fetch plan catalog
// =============================================================================

export async function GET(request: NextRequest) {
  try {
    const now = Date.now();
    const audience = request.nextUrl.searchParams.get('audience');

    // Return cached catalog if fresh
    if (_cachedCatalog && now - _catalogCacheTimestamp < CATALOG_CACHE_TTL_MS) {
      const result =
        audience === 'seller'
          ? { seller: _cachedCatalog.seller, updatedAt: _cachedCatalog.updatedAt }
          : audience === 'dealer'
            ? { dealer: _cachedCatalog.dealer, updatedAt: _cachedCatalog.updatedAt }
            : _cachedCatalog;

      return NextResponse.json(result, {
        headers: { 'Cache-Control': 'public, max-age=60', 'X-Cache': 'HIT' },
      });
    }

    // Fetch live pricing from the BFF /api/pricing route (which proxies to AdminService)
    let pricing: PricingData | null = null;
    try {
      const pricingRes = await fetch(`${API_URL}/api/public/pricing`, {
        next: { revalidate: 60 },
        signal: AbortSignal.timeout(5000),
      });
      if (pricingRes.ok) {
        pricing = await pricingRes.json();
      }
    } catch {
      // Fall through to defaults
    }

    const safePricing: PricingData = {
      // v5.0 prices in DOP (USD × 58)
      dealerLibre: pricing?.dealerLibre ?? 0,
      dealerVisible: pricing?.dealerVisible ?? 1682,
      dealerStarter: pricing?.dealerStarter ?? 3422,
      dealerPro: pricing?.dealerPro ?? 5742,
      dealerElite: pricing?.dealerElite ?? 20242,
      dealerEnterprise: pricing?.dealerEnterprise ?? 34742,
      sellerGratis: pricing?.sellerGratis ?? 0,
      sellerEstandar: pricing?.sellerEstandar ?? 579,
      sellerVerificado: pricing?.sellerVerificado ?? 2029,
    };

    const catalog = buildCatalog(safePricing);
    _cachedCatalog = catalog;
    _catalogCacheTimestamp = now;

    const result =
      audience === 'seller'
        ? { seller: catalog.seller, updatedAt: catalog.updatedAt }
        : audience === 'dealer'
          ? { dealer: catalog.dealer, updatedAt: catalog.updatedAt }
          : catalog;

    return NextResponse.json(result, {
      headers: { 'Cache-Control': 'public, max-age=60', 'X-Cache': 'MISS' },
    });
  } catch (error) {
    console.error('[Plans API] GET error:', error);
    return NextResponse.json({ error: 'Failed to load plans' }, { status: 500 });
  }
}

// =============================================================================
// POST — admin saves plan feature overrides
// =============================================================================

export async function POST(request: NextRequest) {
  try {
    const body = await request.json();

    // Merge feature overrides
    if (body.featureOverrides && typeof body.featureOverrides === 'object') {
      _featureOverrides = { ...(_featureOverrides ?? {}), ...body.featureOverrides };
    }

    // Invalidate catalog cache so next GET rebuilds with new features
    _catalogCacheTimestamp = 0;
    _cachedCatalog = null;

    return NextResponse.json(
      { success: true, message: 'Plan features updated. Catalog cache invalidated.' },
      { status: 200 }
    );
  } catch (error) {
    console.error('[Plans API] POST error:', error);
    return NextResponse.json({ error: 'Failed to save plan overrides' }, { status: 500 });
  }
}
