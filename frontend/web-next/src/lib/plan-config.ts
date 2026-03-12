/**
 * Plan Configuration — OKLA v2
 *
 * Plan enums, features, and limits for sellers and dealers.
 * Updated with full OKLA monetization features: visibility tiers,
 * OKLA Coins credits, ChatAgent, PricingAgent, badges, analytics.
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
  // OKLA v2 fields
  searchPriority: 'standard' | 'medium' | 'high' | 'top';
  monthlyOklaCoinsCredits: number;
  badgeType: 'none' | 'verified' | 'verified-gold' | 'verified-premium';
  chatAgentWeb: number; // conversations/month, -1 = unlimited
  chatAgentWhatsApp: number;
  autoScheduling: boolean;
  whatsAppReminders: boolean;
  pricingAgentFree: number;
  pricingAgentMonthly: number; // -1 = unlimited
  pricingAgentPdf: boolean;
  dashboardLevel: 'none' | 'basic' | 'advanced' | 'complete';
  canExportAnalytics: boolean;
  videoTour: boolean;
  maxVideos: number;
  view360Available: boolean;
}

export const DEALER_PLAN_PRICES: Record<DealerPlan, number> = {
  [DealerPlan.LIBRE]: 0,
  [DealerPlan.VISIBLE]: 29,
  [DealerPlan.PRO]: 89,
  [DealerPlan.ELITE]: 199,
};

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
    searchPriority: 'standard',
    monthlyOklaCoinsCredits: 0,
    badgeType: 'none',
    chatAgentWeb: 0,
    chatAgentWhatsApp: 0,
    autoScheduling: false,
    whatsAppReminders: false,
    pricingAgentFree: 1,
    pricingAgentMonthly: 0,
    pricingAgentPdf: false,
    dashboardLevel: 'none',
    canExportAnalytics: false,
    videoTour: false,
    maxVideos: 0,
    view360Available: false,
  },
  [DealerPlan.VISIBLE]: {
    maxListings: 999999,
    maxImages: 20,
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
    searchPriority: 'medium',
    monthlyOklaCoinsCredits: 15,
    badgeType: 'verified',
    chatAgentWeb: 0,
    chatAgentWhatsApp: 0,
    autoScheduling: false,
    whatsAppReminders: false,
    pricingAgentFree: 0,
    pricingAgentMonthly: 5,
    pricingAgentPdf: false,
    dashboardLevel: 'basic',
    canExportAnalytics: false,
    videoTour: false,
    maxVideos: 1,
    view360Available: false,
  },
  [DealerPlan.PRO]: {
    maxListings: 999999,
    maxImages: 30,
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
    searchPriority: 'high',
    monthlyOklaCoinsCredits: 45,
    badgeType: 'verified-gold',
    chatAgentWeb: 500,
    chatAgentWhatsApp: 500,
    autoScheduling: true,
    whatsAppReminders: false,
    pricingAgentFree: 0,
    pricingAgentMonthly: -1,
    pricingAgentPdf: false,
    dashboardLevel: 'advanced',
    canExportAnalytics: false,
    videoTour: false,
    maxVideos: 2,
    view360Available: true,
  },
  [DealerPlan.ELITE]: {
    maxListings: 999999,
    maxImages: 40,
    analyticsAccess: true,
    marketPriceAnalysis: true,
    bulkUpload: true,
    featuredListings: 25,
    leadManagement: true,
    emailAutomation: true,
    customBranding: true,
    apiAccess: true,
    prioritySupport: true,
    whatsappIntegration: true,
    searchPriority: 'top',
    monthlyOklaCoinsCredits: 120,
    badgeType: 'verified-premium',
    chatAgentWeb: -1,
    chatAgentWhatsApp: -1,
    autoScheduling: true,
    whatsAppReminders: true,
    pricingAgentFree: 0,
    pricingAgentMonthly: -1,
    pricingAgentPdf: true,
    dashboardLevel: 'complete',
    canExportAnalytics: true,
    videoTour: true,
    maxVideos: 5,
    view360Available: true,
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
  maxVideos: number;
  view360Available: boolean;
}

export const SELLER_PLAN_LIMITS: Record<SellerPlan, SellerPlanFeatures> = {
  [SellerPlan.GRATIS]: {
    maxListings: 1,
    maxImages: 15,
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
    maxVideos: 0,
    view360Available: false,
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
    maxVideos: 1,
    view360Available: true,
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
    maxVideos: 3,
    view360Available: true,
  },
};
