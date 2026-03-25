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
  STARTER: 'starter',
  PRO: 'pro',
  ELITE: 'elite',
  ENTERPRISE: 'enterprise',
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
  badgeType:
    | 'none'
    | 'verified'
    | 'verified-plus'
    | 'verified-gold'
    | 'verified-premium'
    | 'enterprise';
  chatAgentWeb: number; // conversations/month, -1 = unlimited
  chatAgentWhatsApp: number;
  chatAgentOverageCostUsd: number; // USD per conversation above monthly limit (0 = no overage)
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
  [DealerPlan.STARTER]: 59,
  [DealerPlan.PRO]: 99,
  [DealerPlan.ELITE]: 349,
  [DealerPlan.ENTERPRISE]: 599,
};

export const DEALER_PLAN_LIMITS: Record<DealerPlan, DealerPlanFeatures> = {
  [DealerPlan.LIBRE]: {
    maxListings: 999999,
    maxImages: 5,
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
    chatAgentOverageCostUsd: 0,
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
    maxImages: 10,
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
    chatAgentOverageCostUsd: 0,
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
  [DealerPlan.STARTER]: {
    maxListings: 999999,
    maxImages: 12,
    analyticsAccess: true,
    marketPriceAnalysis: false,
    bulkUpload: true,
    featuredListings: 5,
    leadManagement: true,
    emailAutomation: false,
    customBranding: false,
    apiAccess: false,
    prioritySupport: false,
    whatsappIntegration: false,
    searchPriority: 'high',
    monthlyOklaCoinsCredits: 30,
    badgeType: 'verified-plus',
    chatAgentWeb: 100,
    chatAgentWhatsApp: 100,
    chatAgentOverageCostUsd: 0.1,
    autoScheduling: false,
    whatsAppReminders: false,
    pricingAgentFree: 0,
    pricingAgentMonthly: 10,
    pricingAgentPdf: false,
    dashboardLevel: 'basic',
    canExportAnalytics: false,
    videoTour: false,
    maxVideos: 1,
    view360Available: false,
  },
  [DealerPlan.PRO]: {
    maxListings: 999999,
    maxImages: 15,
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
    chatAgentOverageCostUsd: 0.08,
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
    maxImages: 20,
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
    chatAgentWeb: 2000,
    chatAgentWhatsApp: 2000,
    chatAgentOverageCostUsd: 0.06,
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
  [DealerPlan.ENTERPRISE]: {
    maxListings: 999999,
    maxImages: 20,
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
    searchPriority: 'top',
    monthlyOklaCoinsCredits: 300,
    badgeType: 'enterprise',
    chatAgentWeb: -1,
    chatAgentWhatsApp: -1,
    chatAgentOverageCostUsd: 0,
    autoScheduling: true,
    whatsAppReminders: true,
    pricingAgentFree: 0,
    pricingAgentMonthly: -1,
    pricingAgentPdf: true,
    dashboardLevel: 'complete',
    canExportAnalytics: true,
    videoTour: true,
    maxVideos: -1,
    view360Available: true,
  },
};

// =============================================================================
// SELLER PLANS
// =============================================================================

export const SellerPlan = {
  LIBRE: 'libre_seller',
  ESTANDAR: 'estandar',
  VERIFICADO: 'verificado',
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

export const SELLER_PLAN_PRICES: Record<SellerPlan, number> = {
  [SellerPlan.LIBRE]: 0,
  [SellerPlan.ESTANDAR]: 9.99, // USD per listing (one-time)
  [SellerPlan.VERIFICADO]: 34.99, // USD/mes
};

export const SELLER_PLAN_LIMITS: Record<SellerPlan, SellerPlanFeatures> = {
  [SellerPlan.LIBRE]: {
    maxListings: 1,
    maxImages: 5,
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
  [SellerPlan.ESTANDAR]: {
    maxListings: 1, // 1 por pago (por listing)
    maxImages: 10,
    listingDuration: 60,
    analyticsAccess: false,
    searchPriority: true,
    verifiedBadge: true,
    featuredListings: 0,
    whatsappContact: true,
    detailedStats: false,
    boostAvailable: true,
    socialSharing: true,
    priceDropAlerts: false,
    maxVideos: 0,
    view360Available: false,
  },
  [SellerPlan.VERIFICADO]: {
    maxListings: 3,
    maxImages: 12,
    listingDuration: 90,
    analyticsAccess: true,
    searchPriority: true,
    verifiedBadge: true,
    featuredListings: 0,
    whatsappContact: true,
    detailedStats: true,
    boostAvailable: true,
    socialSharing: true,
    priceDropAlerts: true,
    maxVideos: 0,
    view360Available: true,
  },
};
