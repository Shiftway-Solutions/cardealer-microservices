/**
 * Plan Configuration
 *
 * Plan enums, features, and limits for sellers and dealers.
 * Used by usePlanAccess hook and PlanGate components.
 */

// =============================================================================
// DEALER PLANS
// =============================================================================

export const DealerPlan = {
  LIBRE: 'libre',
  VISIBLE: 'visible',
  PRO: 'pro',
  ELITE: 'elite',
} as const;
export type DealerPlan = (typeof DealerPlan)[keyof typeof DealerPlan];

export interface DealerPlanFeatures {
  maxListings: number;
  maxImages: number;
  analyticsAccess: boolean;
  marketPriceAnalysis: boolean;
  bulkUpload: boolean;
  featuredListings: number;
  leadManagement: boolean;
  emailAutomation: boolean;
  customBranding: boolean;
  apiAccess: boolean;
  prioritySupport: boolean;
  whatsappIntegration: boolean;
}

export const DEALER_PLAN_LIMITS: Record<DealerPlan, DealerPlanFeatures> = {
  [DealerPlan.LIBRE]: {
    maxListings: 999999,
    maxImages: 10,
    analyticsAccess: false,
    marketPriceAnalysis: false,
    bulkUpload: false,
    featuredListings: 0,
    leadManagement: false,
    emailAutomation: false,
    customBranding: false,
    apiAccess: false,
    prioritySupport: false,
    whatsappIntegration: false,
  },
  [DealerPlan.VISIBLE]: {
    maxListings: 999999,
    maxImages: 25,
    analyticsAccess: true,
    marketPriceAnalysis: false,
    bulkUpload: true,
    featuredListings: 3,
    leadManagement: true,
    emailAutomation: false,
    customBranding: false,
    apiAccess: false,
    prioritySupport: false,
    whatsappIntegration: false,
  },
  [DealerPlan.PRO]: {
    maxListings: 999999,
    maxImages: 40,
    analyticsAccess: true,
    marketPriceAnalysis: true,
    bulkUpload: true,
    featuredListings: 10,
    leadManagement: true,
    emailAutomation: true,
    customBranding: true,
    apiAccess: false,
    prioritySupport: true,
    whatsappIntegration: true,
  },
  [DealerPlan.ELITE]: {
    maxListings: 999999,
    maxImages: 50,
    analyticsAccess: true,
    marketPriceAnalysis: true,
    bulkUpload: true,
    featuredListings: 50,
    leadManagement: true,
    emailAutomation: true,
    customBranding: true,
    apiAccess: true,
    prioritySupport: true,
    whatsappIntegration: true,
  },
};

// =============================================================================
// SELLER PLANS
// =============================================================================

export const SellerPlan = {
  GRATIS: 'gratis',
  PREMIUM: 'premium',
  PRO: 'pro',
} as const;
export type SellerPlan = (typeof SellerPlan)[keyof typeof SellerPlan];

export interface SellerPlanFeatures {
  maxListings: number;
  maxImages: number;
  listingDuration: number;
  analyticsAccess: boolean;
  searchPriority: boolean;
  verifiedBadge: boolean;
  featuredListings: number;
  whatsappContact: boolean;
  detailedStats: boolean;
  boostAvailable: boolean;
  socialSharing: boolean;
  priceDropAlerts: boolean;
}

export const SELLER_PLAN_LIMITS: Record<SellerPlan, SellerPlanFeatures> = {
  [SellerPlan.GRATIS]: {
    maxListings: 1,
    maxImages: 10,
    listingDuration: 30,
    analyticsAccess: false,
    searchPriority: false,
    verifiedBadge: false,
    featuredListings: 0,
    whatsappContact: true,
    detailedStats: false,
    boostAvailable: false,
    socialSharing: false,
    priceDropAlerts: false,
  },
  [SellerPlan.PREMIUM]: {
    maxListings: 5,
    maxImages: 30,
    listingDuration: 0,
    analyticsAccess: true,
    searchPriority: true,
    verifiedBadge: true,
    featuredListings: 2,
    whatsappContact: true,
    detailedStats: true,
    boostAvailable: true,
    socialSharing: true,
    priceDropAlerts: false,
  },
  [SellerPlan.PRO]: {
    maxListings: 15,
    maxImages: 50,
    listingDuration: 0,
    analyticsAccess: true,
    searchPriority: true,
    verifiedBadge: true,
    featuredListings: 5,
    whatsappContact: true,
    detailedStats: true,
    boostAvailable: true,
    socialSharing: true,
    priceDropAlerts: true,
  },
};
