/**
 * Plan Access Hook
 *
 * Unified hook to check plan-based feature access for both sellers and dealers.
 * Provides feature gating utilities and plan comparison helpers.
 *
 * Usage:
 *   const { canAccess, currentPlan, planLabel, upgradeUrl } = usePlanAccess();
 *   if (!canAccess('analytics')) showUpgradePrompt();
 */

'use client';

import { useMemo } from 'react';
import { useAuth } from '@/hooks/use-auth';
import { useCurrentDealer } from '@/hooks/use-dealers';
import { DealerPlan, DEALER_PLAN_LIMITS, SellerPlan, SELLER_PLAN_LIMITS } from '@/lib/plan-config';

// =============================================================================
// TYPES
// =============================================================================

export type PlanFeatureKey =
  // Dealer features
  | 'analytics'
  | 'marketPriceAnalysis'
  | 'bulkUpload'
  | 'featuredListings'
  | 'leadManagement'
  | 'emailAutomation'
  | 'customBranding'
  | 'apiAccess'
  | 'prioritySupport'
  | 'whatsappIntegration'
  | 'chatAgentWeb'
  | 'autoScheduling'
  | 'canExportAnalytics'
  | 'videoTour'
  | 'view360Available'
  | 'pricingAgent'
  // Seller features
  | 'searchPriority'
  | 'verifiedBadge'
  | 'detailedStats'
  | 'boostAvailable'
  | 'socialSharing'
  | 'priceDropAlerts';

export interface PlanAccessResult {
  /** User's account type: 'seller' | 'dealer' | other */
  accountType: string | undefined;
  /** Whether user is a seller */
  isSeller: boolean;
  /** Whether user is a dealer */
  isDealer: boolean;
  /** Current plan ID */
  currentPlan: string;
  /** Human-readable plan label */
  planLabel: string;
  /** URL to subscription/upgrade page */
  upgradeUrl: string;
  /** Check if user can access a specific feature */
  canAccess: (feature: PlanFeatureKey) => boolean;
  /** Get the minimum plan needed for a feature */
  minimumPlanFor: (feature: PlanFeatureKey) => string;
  /** Maximum listings allowed */
  maxListings: number;
  /** Maximum images per vehicle */
  maxImages: number;
  /** Featured listings per month */
  featuredPerMonth: number;
  /** Whether data is still loading */
  isLoading: boolean;
}

// =============================================================================
// PLAN LABELS
// =============================================================================

const DEALER_PLAN_LABELS: Record<string, string> = {
  none: 'Sin Plan',
  libre: 'LIBRE',
  visible: 'VISIBLE',
  starter: 'STARTER',
  pro: 'PRO',
  elite: 'ÉLITE',
  enterprise: 'ENTERPRISE',
};

const SELLER_PLAN_LABELS: Record<string, string> = {
  libre_seller: 'Libre',
  estandar: 'Estándar',
  verificado: 'Verificado',
};

// =============================================================================
// FEATURE MAPPING (Dealer)
// =============================================================================

function dealerCanAccess(plan: string, feature: PlanFeatureKey): boolean {
  const dp = (plan as DealerPlan) || DealerPlan.LIBRE;
  const limits = DEALER_PLAN_LIMITS[dp] ?? DEALER_PLAN_LIMITS[DealerPlan.LIBRE];

  const map: Record<string, boolean> = {
    analytics: limits.analyticsAccess,
    marketPriceAnalysis: limits.marketPriceAnalysis,
    bulkUpload: limits.bulkUpload,
    featuredListings: limits.featuredListings > 0,
    leadManagement: limits.leadManagement,
    emailAutomation: limits.emailAutomation,
    customBranding: limits.customBranding,
    apiAccess: limits.apiAccess,
    prioritySupport: limits.prioritySupport,
    whatsappIntegration: limits.whatsappIntegration,
    chatAgentWeb: limits.chatAgentWeb !== 0,
    autoScheduling: limits.autoScheduling,
    canExportAnalytics: limits.canExportAnalytics,
    videoTour: limits.videoTour,
    view360Available: limits.view360Available,
    pricingAgent: limits.pricingAgentMonthly !== 0,
    // Seller features mapped for dealers (always available for pro+)
    searchPriority: dp !== DealerPlan.LIBRE,
    verifiedBadge: dp !== DealerPlan.LIBRE,
    detailedStats: limits.analyticsAccess,
    boostAvailable: dp !== DealerPlan.LIBRE,
    socialSharing: true,
    priceDropAlerts:
      dp === DealerPlan.PRO || dp === DealerPlan.ELITE || dp === DealerPlan.ENTERPRISE,
  };

  return map[feature] ?? false;
}

function dealerMinPlanFor(feature: PlanFeatureKey): string {
  const planOrder: DealerPlan[] = [
    DealerPlan.LIBRE,
    DealerPlan.VISIBLE,
    DealerPlan.STARTER,
    DealerPlan.PRO,
    DealerPlan.ELITE,
    DealerPlan.ENTERPRISE,
  ];

  for (const plan of planOrder) {
    if (dealerCanAccess(plan, feature)) {
      return DEALER_PLAN_LABELS[plan] ?? plan;
    }
  }
  return 'ÉLITE';
}

// =============================================================================
// FEATURE MAPPING (Seller)
// =============================================================================

function sellerCanAccess(plan: string, feature: PlanFeatureKey): boolean {
  const sp = (plan as SellerPlan) || SellerPlan.LIBRE;
  const limits = SELLER_PLAN_LIMITS[sp] ?? SELLER_PLAN_LIMITS[SellerPlan.LIBRE];

  const map: Record<string, boolean> = {
    analytics: limits.analyticsAccess,
    searchPriority: limits.searchPriority,
    verifiedBadge: limits.verifiedBadge,
    featuredListings: limits.featuredListings > 0,
    detailedStats: limits.detailedStats,
    boostAvailable: limits.boostAvailable,
    socialSharing: limits.socialSharing,
    priceDropAlerts: limits.priceDropAlerts,
    whatsappIntegration: limits.whatsappContact,
    // Dealer-specific features — never available for sellers
    marketPriceAnalysis: false,
    bulkUpload: false,
    leadManagement: false,
    emailAutomation: false,
    customBranding: false,
    apiAccess: false,
    prioritySupport: sp === SellerPlan.VERIFICADO,
  };

  return map[feature] ?? false;
}

function sellerMinPlanFor(feature: PlanFeatureKey): string {
  const planOrder: SellerPlan[] = [SellerPlan.LIBRE, SellerPlan.ESTANDAR, SellerPlan.VERIFICADO];

  for (const plan of planOrder) {
    if (sellerCanAccess(plan, feature)) {
      return SELLER_PLAN_LABELS[plan] ?? plan;
    }
  }
  return 'PRO';
}

// =============================================================================
// HOOK
// =============================================================================

export function usePlanAccess(): PlanAccessResult {
  const { user, isLoading: authLoading } = useAuth();
  const { data: dealer, isLoading: dealerLoading } = useCurrentDealer();

  const accountType = user?.accountType;
  const isSeller = accountType === 'seller';
  const isDealer = accountType === 'dealer' || accountType === 'dealer_employee';

  return useMemo(() => {
    if (isDealer) {
      const plan = dealer?.plan ?? 'libre';
      const limits =
        DEALER_PLAN_LIMITS[(plan as DealerPlan) ?? DealerPlan.LIBRE] ??
        DEALER_PLAN_LIMITS[DealerPlan.LIBRE];

      return {
        accountType,
        isSeller: false,
        isDealer: true,
        currentPlan: plan,
        planLabel: DEALER_PLAN_LABELS[plan] ?? plan,
        upgradeUrl: '/cuenta/upgrade',
        canAccess: (f: PlanFeatureKey) => dealerCanAccess(plan, f),
        minimumPlanFor: dealerMinPlanFor,
        maxListings: limits.maxListings,
        maxImages: limits.maxImages,
        featuredPerMonth: limits.featuredListings,
        isLoading: authLoading || dealerLoading,
      };
    }

    if (isSeller) {
      // For sellers, we derive plan from user metadata
      // Default to 'libre_seller' if not set
      const plan = (user as unknown as Record<string, unknown>)?.sellerPlan ?? 'libre_seller';
      const planKey = plan as SellerPlan;
      const limits = SELLER_PLAN_LIMITS[planKey] ?? SELLER_PLAN_LIMITS[SellerPlan.LIBRE];

      return {
        accountType,
        isSeller: true,
        isDealer: false,
        currentPlan: plan as string,
        planLabel: SELLER_PLAN_LABELS[plan as string] ?? 'Libre',
        upgradeUrl: '/cuenta/upgrade',
        canAccess: (f: PlanFeatureKey) => sellerCanAccess(plan as string, f),
        minimumPlanFor: sellerMinPlanFor,
        maxListings: limits.maxListings,
        maxImages: limits.maxImages,
        featuredPerMonth: limits.featuredListings,
        isLoading: authLoading,
      };
    }

    // Default (buyer or unauthenticated)
    return {
      accountType,
      isSeller: false,
      isDealer: false,
      currentPlan: 'none',
      planLabel: 'Sin Plan',
      upgradeUrl: '/vender',
      canAccess: () => false,
      minimumPlanFor: () => 'N/A',
      maxListings: 0,
      maxImages: 0,
      featuredPerMonth: 0,
      isLoading: authLoading,
    };
  }, [accountType, isSeller, isDealer, dealer?.plan, user, authLoading, dealerLoading]);
}
