// ============================================================================
// OKLA Advertising System Types
// Based on the OKLA Scientific Advertising Algorithm Study v1.0
// GSP Auction + Quality Score + Multi-Dimensional Targeting
// ============================================================================

// ---------------------------------------------------------------------------
// Ad Slot Positions & Types
// ---------------------------------------------------------------------------

/** Physical positions where ads can appear in the platform */
export type AdSlotPosition =
  | 'homepage_hero'              // Hero carousel on homepage
  | 'homepage_featured_grid'     // Within "Más Vehículos" grid
  | 'homepage_recommended'       // "Recomendados para ti" section
  | 'homepage_category'          // Inside category sections
  | 'homepage_banner'            // Banner between sections
  | 'search_top'                 // Top 1-3 sponsored results in /vehiculos
  | 'search_inline'              // Inline sponsored within search results
  | 'search_sidebar'             // Sidebar ad in /vehiculos
  | 'detail_related'             // Related vehicles on detail page
  | 'detail_banner';             // Banner on vehicle detail page

/** Revenue model for each slot */
export type AdPricingModel = 'CPC' | 'CPM' | 'CPA' | 'CPL' | 'FLAT';

/** Visual format of the ad */
export type AdFormat =
  | 'native_vehicle_card'        // Looks like a normal vehicle listing
  | 'native_dealer_card'         // Looks like a dealer card
  | 'banner_leaderboard'         // 728x90
  | 'banner_medium_rectangle'    // 300x250
  | 'banner_billboard'           // 970x250
  | 'banner_interstitial'        // Full screen mobile
  | 'native_inline'              // Text/image inline with content
  | 'video_preroll';             // Video before content

// ---------------------------------------------------------------------------
// Quality Score System (Section 2.3 of study)
// ---------------------------------------------------------------------------

/** Quality Score components (1-10 scale each) */
export interface QualityScoreComponents {
  /** Expected CTR based on historical performance (weight: 35%) */
  expectedCtr: number;
  /** Relevance of ad to search/context (weight: 40%) */
  adRelevance: number;
  /** Post-click experience quality (weight: 25%) */
  landingExperience: number;
}

/** Computed Quality Score */
export interface QualityScore {
  components: QualityScoreComponents;
  /** Final score 1-10 */
  score: number;
  /** Last computation timestamp */
  computedAt: string;
}

// ---------------------------------------------------------------------------
// Ad Auction System (Section 2.2 of study)
// ---------------------------------------------------------------------------

/** A bid from a dealer for an ad slot */
export interface AdBid {
  id: string;
  campaignId: string;
  dealerId: string;
  dealerName: string;
  /** Maximum CPC the dealer is willing to pay (RD$) */
  maxCpcBid: number;
  /** Quality Score of this ad */
  qualityScore: QualityScore;
  /** Context multiplier based on targeting match */
  contextMultiplier: number;
  /** Computed: maxCpcBid × qualityScore × contextMultiplier */
  adRank: number;
  /** Vehicle being promoted */
  vehicleId: string;
}

/** Result of a GSP auction for a single slot */
export interface AuctionResult {
  /** Winning bid */
  winnerId: string;
  /** Position awarded (1 = top) */
  position: number;
  /** Actual price paid (second-price + RD$1) */
  actualPricePaid: number;
  /** The ad rank score */
  adRank: number;
  /** Quality score of winner */
  qualityScore: number;
  /** The vehicle to display */
  vehicleId: string;
  /** The dealer info */
  dealerId: string;
  dealerName: string;
  /** Campaign reference */
  campaignId: string;
}

/** Full auction for a set of slots */
export interface SlotAuction {
  slotPosition: AdSlotPosition;
  /** Number of positions available in this slot */
  availablePositions: number;
  /** Results ordered by position */
  results: AuctionResult[];
  /** Timestamp of auction */
  auctionTimestamp: string;
  /** Floor price for this slot */
  floorPrice: number;
}

// ---------------------------------------------------------------------------
// Ad Campaign (extends existing advertising types)
// ---------------------------------------------------------------------------

export type AdCampaignStatus =
  | 'draft'
  | 'pending_review'
  | 'active'
  | 'paused'
  | 'completed'
  | 'cancelled';

export type AdCampaignObjective =
  | 'traffic'          // Drive clicks to listings
  | 'leads'            // Generate contact leads
  | 'awareness'        // Brand awareness (impressions)
  | 'conversions';     // Drive sales

export interface AdCampaignBudget {
  /** Daily budget limit (RD$) */
  dailyBudget: number;
  /** Total campaign budget (RD$) */
  totalBudget: number;
  /** Amount spent so far (RD$) */
  spent: number;
  /** Remaining budget (RD$) */
  remaining: number;
}

export interface AdCampaignTargeting {
  /** Target regions */
  regions?: string[];
  /** Target vehicle makes */
  makes?: string[];
  /** Target vehicle models */
  models?: string[];
  /** Target year range */
  yearRange?: { min: number; max: number };
  /** Target price range (RD$) */
  priceRange?: { min: number; max: number };
  /** Target buyer intent score range */
  intentScoreRange?: { min: number; max: number };
  /** Keywords for sponsored search */
  keywords?: string[];
  /** Device targeting */
  devices?: ('mobile' | 'desktop' | 'tablet')[];
  /** Day/hour targeting */
  schedule?: AdSchedule[];
}

export interface AdSchedule {
  dayOfWeek: number; // 0=Sunday
  startHour: number; // 0-23
  endHour: number;   // 0-23
  bidMultiplier: number; // e.g., 1.3 = +30%
}

export interface AdCampaignFull {
  id: string;
  dealerId: string;
  dealerName: string;
  name: string;
  objective: AdCampaignObjective;
  status: AdCampaignStatus;
  /** Slots this campaign is bidding on */
  targetSlots: AdSlotPosition[];
  /** Ad format */
  format: AdFormat;
  /** Pricing model */
  pricingModel: AdPricingModel;
  /** Max bid per unit (CPC/CPM/etc.) */
  maxBid: number;
  /** Budget configuration */
  budget: AdCampaignBudget;
  /** Targeting configuration */
  targeting: AdCampaignTargeting;
  /** Vehicles in this campaign */
  vehicleIds: string[];
  /** Quality Score */
  qualityScore?: QualityScore;
  /** Performance metrics */
  metrics: AdCampaignMetrics;
  /** Dates */
  startDate: string;
  endDate?: string;
  createdAt: string;
  updatedAt: string;
}

export interface AdCampaignMetrics {
  impressions: number;
  clicks: number;
  ctr: number;
  leads: number;
  conversions: number;
  spent: number;
  cpc: number;
  cpl: number;
  roas: number;
}

// ---------------------------------------------------------------------------
// Purchase Intent Score (Section 3.2 of study)
// ---------------------------------------------------------------------------

export type BuyerIntentLevel =
  | 'cold'       // PIS 0-19,  CPC multiplier 0.3-0.6x
  | 'browsing'   // PIS 20-39, CPC multiplier 0.7-0.9x
  | 'interested' // PIS 40-59, CPC multiplier 1.0-1.3x
  | 'warm'       // PIS 60-79, CPC multiplier 1.4-1.9x
  | 'hot';       // PIS 80-100, CPC multiplier 2.0-2.5x

export interface PurchaseIntentScore {
  score: number;       // 0-100
  level: BuyerIntentLevel;
  cpcMultiplier: number;
  lastUpdated: string;
}

// ---------------------------------------------------------------------------
// Frequency Capping (Section 4.3 of study)
// ---------------------------------------------------------------------------

export interface FrequencyCap {
  adType: 'sponsored_search' | 'display_banner' | 'featured_listing' | 'push_notification' | 'retargeting';
  maxPerDay: number;
  maxPerWeek: number;
}

/** Default frequency caps from the study */
export const DEFAULT_FREQUENCY_CAPS: FrequencyCap[] = [
  { adType: 'sponsored_search',   maxPerDay: 3,  maxPerWeek: 8  },
  { adType: 'display_banner',     maxPerDay: 5,  maxPerWeek: 15 },
  { adType: 'featured_listing',   maxPerDay: 4,  maxPerWeek: 12 },
  { adType: 'push_notification',  maxPerDay: 1,  maxPerWeek: 2  },
  { adType: 'retargeting',        maxPerDay: 6,  maxPerWeek: 18 },
];

// ---------------------------------------------------------------------------
// Pacing (Section 4.2 of study)
// ---------------------------------------------------------------------------

/** Hour weight for budget pacing */
export interface HourWeight {
  hour: number;           // 0-23
  trafficWeight: number;  // % of daily traffic
  conversionWeight: number; // % of daily conversions
  cpcMultiplier: number;  // CPC adjustment
}

/** Default hour weights from the study */
export const DEFAULT_HOUR_WEIGHTS: HourWeight[] = [
  { hour: 0,  trafficWeight: 0.01, conversionWeight: 0.01, cpcMultiplier: 0.6 },
  { hour: 1,  trafficWeight: 0.005, conversionWeight: 0.005, cpcMultiplier: 0.6 },
  { hour: 2,  trafficWeight: 0.005, conversionWeight: 0.005, cpcMultiplier: 0.6 },
  { hour: 3,  trafficWeight: 0.005, conversionWeight: 0.005, cpcMultiplier: 0.6 },
  { hour: 4,  trafficWeight: 0.005, conversionWeight: 0.005, cpcMultiplier: 0.6 },
  { hour: 5,  trafficWeight: 0.005, conversionWeight: 0.005, cpcMultiplier: 0.6 },
  { hour: 6,  trafficWeight: 0.025, conversionWeight: 0.015, cpcMultiplier: 0.7 },
  { hour: 7,  trafficWeight: 0.025, conversionWeight: 0.015, cpcMultiplier: 0.7 },
  { hour: 8,  trafficWeight: 0.04, conversionWeight: 0.03, cpcMultiplier: 0.9 },
  { hour: 9,  trafficWeight: 0.04, conversionWeight: 0.03, cpcMultiplier: 0.9 },
  { hour: 10, trafficWeight: 0.075, conversionWeight: 0.06, cpcMultiplier: 1.0 },
  { hour: 11, trafficWeight: 0.075, conversionWeight: 0.06, cpcMultiplier: 1.0 },
  { hour: 12, trafficWeight: 0.09, conversionWeight: 0.075, cpcMultiplier: 1.1 },
  { hour: 13, trafficWeight: 0.09, conversionWeight: 0.075, cpcMultiplier: 1.1 },
  { hour: 14, trafficWeight: 0.07, conversionWeight: 0.06, cpcMultiplier: 1.0 },
  { hour: 15, trafficWeight: 0.07, conversionWeight: 0.06, cpcMultiplier: 1.0 },
  { hour: 16, trafficWeight: 0.07, conversionWeight: 0.06, cpcMultiplier: 1.0 },
  { hour: 17, trafficWeight: 0.10, conversionWeight: 0.11, cpcMultiplier: 1.3 },
  { hour: 18, trafficWeight: 0.10, conversionWeight: 0.11, cpcMultiplier: 1.3 },
  { hour: 19, trafficWeight: 0.10, conversionWeight: 0.11, cpcMultiplier: 1.3 },
  { hour: 20, trafficWeight: 0.08, conversionWeight: 0.125, cpcMultiplier: 1.4 },
  { hour: 21, trafficWeight: 0.08, conversionWeight: 0.125, cpcMultiplier: 1.4 },
  { hour: 22, trafficWeight: 0.02, conversionWeight: 0.025, cpcMultiplier: 0.6 },
  { hour: 23, trafficWeight: 0.01, conversionWeight: 0.01, cpcMultiplier: 0.6 },
];

// ---------------------------------------------------------------------------
// Price Floors (Section 4.1 of study)
// ---------------------------------------------------------------------------

export interface AdSlotConfig {
  position: AdSlotPosition;
  pricingModel: AdPricingModel;
  /** Minimum price floor (RD$) */
  cpmFloor?: number;
  cpcFloor?: number;
  /** Estimated daily inventory */
  estimatedDailyInventory: number;
  /** Target eCPM (RD$) */
  targetEcpm: number;
  /** Max ads in this position */
  maxAds: number;
  /** Ad format for this slot */
  format: AdFormat;
}

/** Default slot configurations from the study */
export const AD_SLOT_CONFIGS: AdSlotConfig[] = [
  {
    position: 'search_top',
    pricingModel: 'CPC',
    cpcFloor: 180,
    estimatedDailyInventory: 5000,
    targetEcpm: 4500,
    maxAds: 3,
    format: 'native_vehicle_card',
  },
  {
    position: 'search_inline',
    pricingModel: 'CPC',
    cpcFloor: 80,
    estimatedDailyInventory: 8000,
    targetEcpm: 2800,
    maxAds: 4,
    format: 'native_vehicle_card',
  },
  {
    position: 'homepage_hero',
    pricingModel: 'CPM',
    cpmFloor: 3500,
    estimatedDailyInventory: 500,
    targetEcpm: 3500,
    maxAds: 5,
    format: 'native_vehicle_card',
  },
  {
    position: 'homepage_featured_grid',
    pricingModel: 'CPC',
    cpcFloor: 60,
    estimatedDailyInventory: 3000,
    targetEcpm: 2100,
    maxAds: 3,
    format: 'native_vehicle_card',
  },
  {
    position: 'homepage_recommended',
    pricingModel: 'CPC',
    cpcFloor: 60,
    estimatedDailyInventory: 3000,
    targetEcpm: 2100,
    maxAds: 6,
    format: 'native_vehicle_card',
  },
  {
    position: 'homepage_category',
    pricingModel: 'CPC',
    cpcFloor: 60,
    estimatedDailyInventory: 4000,
    targetEcpm: 1800,
    maxAds: 2,
    format: 'native_vehicle_card',
  },
  {
    position: 'homepage_banner',
    pricingModel: 'CPM',
    cpmFloor: 1200,
    estimatedDailyInventory: 15000,
    targetEcpm: 1200,
    maxAds: 1,
    format: 'banner_leaderboard',
  },
  {
    position: 'search_sidebar',
    pricingModel: 'CPM',
    cpmFloor: 1200,
    estimatedDailyInventory: 15000,
    targetEcpm: 1200,
    maxAds: 2,
    format: 'banner_medium_rectangle',
  },
  {
    position: 'detail_related',
    pricingModel: 'CPC',
    cpcFloor: 60,
    estimatedDailyInventory: 25000,
    targetEcpm: 1800,
    maxAds: 4,
    format: 'native_vehicle_card',
  },
  {
    position: 'detail_banner',
    pricingModel: 'CPM',
    cpmFloor: 1800,
    estimatedDailyInventory: 25000,
    targetEcpm: 1800,
    maxAds: 1,
    format: 'banner_medium_rectangle',
  },
];

// ---------------------------------------------------------------------------
// Brand Multipliers (Section F1 of study)
// ---------------------------------------------------------------------------

/** Premium brand CPC multipliers */
export const BRAND_MULTIPLIERS: Record<string, number> = {
  // Premium brands (+40%)
  'BMW': 1.4,
  'Mercedes-Benz': 1.4,
  'Lexus': 1.4,
  'Audi': 1.4,
  'Porsche': 1.4,
  'Land Rover': 1.4,
  // Popular brands (+20%)
  'Toyota': 1.2,
  'Honda': 1.2,
  'Hyundai': 1.2,
  // Popular models (+15-30%) — applied at vehicle level
  // Default
  '_default': 1.0,
};

/** Popular model CPC multipliers */
export const MODEL_MULTIPLIERS: Record<string, number> = {
  'CR-V': 1.3,
  'Corolla': 1.3,
  'RAV4': 1.3,
  'Civic': 1.25,
  'Camry': 1.25,
  'Tucson': 1.2,
  'Santa Fe': 1.2,
  'Hilux': 1.2,
  '_default': 1.0,
};

// ---------------------------------------------------------------------------
// Sponsored Vehicle (what gets rendered)
// ---------------------------------------------------------------------------

/** A vehicle card enriched with ad metadata for rendering */
export interface SponsoredVehicle {
  /** Original vehicle data */
  id: string;
  slug: string;
  make: string;
  model: string;
  year: number;
  price: number;
  currency: 'DOP' | 'USD';
  mileage: number;
  transmission: string;
  fuelType: string;
  imageUrl: string;
  location: string;
  dealerName: string;
  dealerRating?: number;
  photoCount?: number;
  isVerified?: boolean;
  trim?: string;
  monthlyPayment?: number;
  /** Ad metadata */
  campaignId: string;
  adBidId: string;
  slotPosition: AdSlotPosition;
  /** 'sponsored' | 'featured' | 'premium' */
  sponsorTier: 'sponsored' | 'featured' | 'premium';
  /** Position in the auction result (1 = top) */
  auctionPosition: number;
  /** Actual CPC the dealer pays if clicked */
  actualCpc: number;
  /** Impression tracking token */
  impressionToken: string;
  /** Click tracking URL */
  clickTrackingUrl: string;
}

// ---------------------------------------------------------------------------
// Ad Performance for dealer dashboard
// ---------------------------------------------------------------------------

export interface AdPerformanceSummary {
  totalCampaigns: number;
  activeCampaigns: number;
  totalSpent: number;
  totalImpressions: number;
  totalClicks: number;
  totalLeads: number;
  averageCtr: number;
  averageCpc: number;
  averageCpl: number;
  estimatedRoas: number;
  /** Daily breakdown */
  dailyMetrics: DailyAdMetric[];
}

export interface DailyAdMetric {
  date: string;
  impressions: number;
  clicks: number;
  leads: number;
  spent: number;
  ctr: number;
  cpc: number;
}

// ---------------------------------------------------------------------------
// ROI Calculator (Section 6.2 of study)
// ---------------------------------------------------------------------------

export interface RoiCalculation {
  monthlyBudget: number;
  estimatedImpressions: number;
  estimatedClicks: number;
  estimatedCpc: number;
  estimatedLeads: number;
  estimatedCpl: number;
  estimatedSales: number;
  estimatedCac: number;
  averageMarginPerSale: number;
  totalRevenue: number;
  roi: number; // percentage
  returnPerDollar: number;
}

// ---------------------------------------------------------------------------
// Anti-Fraud (Section 8 of study)
// ---------------------------------------------------------------------------

export interface InvalidTrafficScore {
  /** Bot probability 0-1 (weight: 40%) */
  botProbability: number;
  /** Anomaly pattern 0-1 (weight: 35%) */
  anomalyPattern: number;
  /** Device fingerprint risk 0-1 (weight: 25%) */
  deviceFingerprintRisk: number;
  /** Composite IVT score 0-1 */
  score: number;
  /** If score > 0.70, traffic is invalid */
  isInvalid: boolean;
}
