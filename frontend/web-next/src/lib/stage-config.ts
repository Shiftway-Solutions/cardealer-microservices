/**
 * OKLA Platform Stage Configuration
 *
 * Controls which features are available based on the current platform stage.
 * All features are configurable by stage to allow gradual rollout.
 *
 * Stage 1: Desarrollo — Free APIs only, basic scoring, core marketplace
 * Stage 2: Beta — Paid APIs, full scoring, payment integration
 * Stage 3: Crecimiento — Badges, PDF reports, advanced analytics
 * Stage 4: Escala — Mandatory scoring, public API, ERP features
 */

export type OklaStage = 1 | 2 | 3 | 4;

export interface StageFeatures {
  // OKLA Score
  oklaScore: {
    enabled: boolean;
    dimensions: ('D1' | 'D2' | 'D3' | 'D4' | 'D5' | 'D6' | 'D7')[];
    vinAudit: boolean;
    marketCheck: boolean;
    kbb: boolean;
    carfax: boolean;
    bcrdExchangeRate: boolean;
    badgeOnListings: boolean;
    pdfReports: boolean;
    mandatoryForPublish: boolean;
    publicApi: boolean;
    scoreHistory: boolean;
  };

  // Media
  media: {
    photoUpload: boolean;
    videoUpload: boolean;
    vista360: boolean;
    video360: boolean;
    maxPhotos: number;
    maxVideoSizeMb: number;
    backgroundRemoval: boolean;
  };

  // Advertising
  advertising: {
    featuredSpots: boolean;
    premiumSpots: boolean;
    bannerAds: boolean;
    dealerShowcase: boolean;
    nativeAds: boolean;
    retargeting: boolean;
    roiCalculator: boolean;
  };

  // Sales
  sales: {
    saleClosedTracking: boolean;
    buyerConfirmation: boolean;
    transactionEntity: boolean;
    fraudDetection: boolean;
    conversionMetrics: boolean;
    marketIntelligence: boolean;
  };

  // Dealer ERP
  dealerErp: {
    inventory: boolean;
    crm: boolean;
    invoicing: boolean;
    ncfIntegration: boolean;
    taxCompliance: boolean;
    staffManagement: boolean;
    accounting: boolean;
  };

  // Platform
  platform: {
    chatAgent: boolean;
    searchAgent: boolean;
    recommendations: boolean;
    priceAlerts: boolean;
    priceHistory: boolean;
    vehicleComparison: boolean;
    financing: boolean;
    insurance: boolean;
    testDriveBooking: boolean;
    referralProgram: boolean;
    oklaCoins: boolean;
    mobileApp: boolean;
  };

  // Infrastructure
  infrastructure: {
    dgiiIntegration: boolean;
    taxCalculator: boolean;
    bcrdApi: boolean;
    multiCluster: boolean;
    readReplicas: boolean;
    cdnOptimization: boolean;
  };
}

/**
 * Returns the feature configuration for a given platform stage
 */
export function getStageFeatures(stage: OklaStage): StageFeatures {
  switch (stage) {
    case 1:
      return {
        oklaScore: {
          enabled: true,
          dimensions: ['D1', 'D3', 'D5', 'D6'],
          vinAudit: false,
          marketCheck: false,
          kbb: false,
          carfax: false,
          bcrdExchangeRate: false,
          badgeOnListings: false,
          pdfReports: false,
          mandatoryForPublish: false,
          publicApi: false,
          scoreHistory: false,
        },
        media: {
          photoUpload: true,
          videoUpload: true,
          vista360: false,
          video360: false,
          maxPhotos: 20,
          maxVideoSizeMb: 50,
          backgroundRemoval: true,
        },
        advertising: {
          featuredSpots: true,
          premiumSpots: true,
          bannerAds: true,
          dealerShowcase: true,
          nativeAds: true,
          retargeting: false,
          roiCalculator: false,
        },
        sales: {
          saleClosedTracking: true,
          buyerConfirmation: false,
          transactionEntity: true,
          fraudDetection: false,
          conversionMetrics: true,
          marketIntelligence: false,
        },
        dealerErp: {
          inventory: true,
          crm: false,
          invoicing: false,
          ncfIntegration: false,
          taxCompliance: false,
          staffManagement: true,
          accounting: false,
        },
        platform: {
          chatAgent: true,
          searchAgent: true,
          recommendations: true,
          priceAlerts: false,
          priceHistory: false,
          vehicleComparison: true,
          financing: false,
          insurance: false,
          testDriveBooking: false,
          referralProgram: false,
          oklaCoins: false,
          mobileApp: false,
        },
        infrastructure: {
          dgiiIntegration: false,
          taxCalculator: false,
          bcrdApi: false,
          multiCluster: false,
          readReplicas: false,
          cdnOptimization: false,
        },
      };

    case 2:
      return {
        oklaScore: {
          enabled: true,
          dimensions: ['D1', 'D2', 'D3', 'D4', 'D5', 'D6', 'D7'],
          vinAudit: true,
          marketCheck: false,
          kbb: false,
          carfax: false,
          bcrdExchangeRate: true,
          badgeOnListings: false,
          pdfReports: false,
          mandatoryForPublish: false,
          publicApi: false,
          scoreHistory: false,
        },
        media: {
          photoUpload: true,
          videoUpload: true,
          vista360: true,
          video360: false,
          maxPhotos: 30,
          maxVideoSizeMb: 100,
          backgroundRemoval: true,
        },
        advertising: {
          featuredSpots: true,
          premiumSpots: true,
          bannerAds: true,
          dealerShowcase: true,
          nativeAds: true,
          retargeting: true,
          roiCalculator: true,
        },
        sales: {
          saleClosedTracking: true,
          buyerConfirmation: true,
          transactionEntity: true,
          fraudDetection: true,
          conversionMetrics: true,
          marketIntelligence: false,
        },
        dealerErp: {
          inventory: true,
          crm: true,
          invoicing: false,
          ncfIntegration: false,
          taxCompliance: false,
          staffManagement: true,
          accounting: false,
        },
        platform: {
          chatAgent: true,
          searchAgent: true,
          recommendations: true,
          priceAlerts: true,
          priceHistory: true,
          vehicleComparison: true,
          financing: false,
          insurance: false,
          testDriveBooking: true,
          referralProgram: false,
          oklaCoins: false,
          mobileApp: false,
        },
        infrastructure: {
          dgiiIntegration: true,
          taxCalculator: true,
          bcrdApi: true,
          multiCluster: false,
          readReplicas: false,
          cdnOptimization: true,
        },
      };

    case 3:
      return {
        oklaScore: {
          enabled: true,
          dimensions: ['D1', 'D2', 'D3', 'D4', 'D5', 'D6', 'D7'],
          vinAudit: true,
          marketCheck: true,
          kbb: true,
          carfax: false,
          bcrdExchangeRate: true,
          badgeOnListings: true,
          pdfReports: true,
          mandatoryForPublish: false,
          publicApi: false,
          scoreHistory: true,
        },
        media: {
          photoUpload: true,
          videoUpload: true,
          vista360: true,
          video360: true,
          maxPhotos: 50,
          maxVideoSizeMb: 200,
          backgroundRemoval: true,
        },
        advertising: {
          featuredSpots: true,
          premiumSpots: true,
          bannerAds: true,
          dealerShowcase: true,
          nativeAds: true,
          retargeting: true,
          roiCalculator: true,
        },
        sales: {
          saleClosedTracking: true,
          buyerConfirmation: true,
          transactionEntity: true,
          fraudDetection: true,
          conversionMetrics: true,
          marketIntelligence: true,
        },
        dealerErp: {
          inventory: true,
          crm: true,
          invoicing: true,
          ncfIntegration: false,
          taxCompliance: false,
          staffManagement: true,
          accounting: true,
        },
        platform: {
          chatAgent: true,
          searchAgent: true,
          recommendations: true,
          priceAlerts: true,
          priceHistory: true,
          vehicleComparison: true,
          financing: true,
          insurance: true,
          testDriveBooking: true,
          referralProgram: true,
          oklaCoins: true,
          mobileApp: false,
        },
        infrastructure: {
          dgiiIntegration: true,
          taxCalculator: true,
          bcrdApi: true,
          multiCluster: false,
          readReplicas: true,
          cdnOptimization: true,
        },
      };

    case 4:
      return {
        oklaScore: {
          enabled: true,
          dimensions: ['D1', 'D2', 'D3', 'D4', 'D5', 'D6', 'D7'],
          vinAudit: true,
          marketCheck: true,
          kbb: true,
          carfax: true,
          bcrdExchangeRate: true,
          badgeOnListings: true,
          pdfReports: true,
          mandatoryForPublish: true,
          publicApi: true,
          scoreHistory: true,
        },
        media: {
          photoUpload: true,
          videoUpload: true,
          vista360: true,
          video360: true,
          maxPhotos: 100,
          maxVideoSizeMb: 500,
          backgroundRemoval: true,
        },
        advertising: {
          featuredSpots: true,
          premiumSpots: true,
          bannerAds: true,
          dealerShowcase: true,
          nativeAds: true,
          retargeting: true,
          roiCalculator: true,
        },
        sales: {
          saleClosedTracking: true,
          buyerConfirmation: true,
          transactionEntity: true,
          fraudDetection: true,
          conversionMetrics: true,
          marketIntelligence: true,
        },
        dealerErp: {
          inventory: true,
          crm: true,
          invoicing: true,
          ncfIntegration: true,
          taxCompliance: true,
          staffManagement: true,
          accounting: true,
        },
        platform: {
          chatAgent: true,
          searchAgent: true,
          recommendations: true,
          priceAlerts: true,
          priceHistory: true,
          vehicleComparison: true,
          financing: true,
          insurance: true,
          testDriveBooking: true,
          referralProgram: true,
          oklaCoins: true,
          mobileApp: true,
        },
        infrastructure: {
          dgiiIntegration: true,
          taxCalculator: true,
          bcrdApi: true,
          multiCluster: true,
          readReplicas: true,
          cdnOptimization: true,
        },
      };
  }
}

/**
 * Gets the current platform stage from environment
 */
export function getCurrentStage(): OklaStage {
  const envStage = process.env.NEXT_PUBLIC_OKLA_STAGE;
  const stage = envStage ? parseInt(envStage, 10) : 1;
  if (stage >= 1 && stage <= 4) return stage as OklaStage;
  return 1;
}

/**
 * Gets the current OKLA Score phase from environment
 */
export function getCurrentScorePhase(): OklaStage {
  const envPhase = process.env.NEXT_PUBLIC_OKLA_SCORE_PHASE;
  const phase = envPhase ? parseInt(envPhase, 10) : 1;
  if (phase >= 1 && phase <= 4) return phase as OklaStage;
  return 1;
}

/**
 * Gets feature configuration for the current platform stage
 */
export function getCurrentStageFeatures(): StageFeatures {
  return getStageFeatures(getCurrentStage());
}

/**
 * Checks if a specific feature is enabled for the current stage
 */
export function isFeatureEnabled(category: keyof StageFeatures, feature: string): boolean {
  const features = getCurrentStageFeatures();
  const categoryFeatures = features[category] as Record<string, unknown>;
  return Boolean(categoryFeatures[feature]);
}

/**
 * Stage metadata for display purposes
 */
export const STAGE_META = {
  1: {
    name: 'Desarrollo',
    nameEs: 'Etapa 1 — Desarrollo',
    description: 'Fase de desarrollo. Solo APIs gratuitas, funcionalidad básica.',
    color: 'emerald',
    costRange: '$0 - $100/mes',
  },
  2: {
    name: 'Beta',
    nameEs: 'Etapa 2 — Beta',
    description: 'APIs pagadas activas, scoring completo, integración de pagos.',
    color: 'blue',
    costRange: '$100 - $280/mes',
  },
  3: {
    name: 'Crecimiento',
    nameEs: 'Etapa 3 — Crecimiento',
    description: 'Badges, reportes PDF, analytics avanzados, ERP básico.',
    color: 'purple',
    costRange: '$280 - $700/mes',
  },
  4: {
    name: 'Escala',
    nameEs: 'Etapa 4 — Escala',
    description: 'Score obligatorio, API pública, ERP completo, multi-cluster.',
    color: 'amber',
    costRange: '$700 - $5,000/mes',
  },
} as const;
