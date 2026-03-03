// ============================================================================
// OKLA Ad Engine — GSP Auction Algorithm with Quality Score
// Implements: Generalized Second-Price Auction (Vickrey)
// Based on the OKLA Scientific Advertising Algorithm Study v1.0
// ============================================================================

import type {
  AdBid,
  AuctionResult,
  SlotAuction,
  QualityScore,
  QualityScoreComponents,
  AdSlotPosition,
  SponsoredVehicle,
  PurchaseIntentScore,
  BuyerIntentLevel,
  InvalidTrafficScore,
  RoiCalculation,
  FrequencyCap,
} from '@/types/ads';

import {
  AD_SLOT_CONFIGS,
  BRAND_MULTIPLIERS,
  DEFAULT_HOUR_WEIGHTS,
  DEFAULT_FREQUENCY_CAPS,
} from '@/types/ads';

// ---------------------------------------------------------------------------
// Quality Score Computation (Section 2.3)
// ---------------------------------------------------------------------------

/**
 * Compute the Quality Score for an ad.
 * QS = (CTR_expected × 0.35) + (Ad_Relevance × 0.40) + (Landing_Experience × 0.25)
 * Each component normalized to 1-10 scale.
 */
export function computeQualityScore(components: QualityScoreComponents): QualityScore {
  const score = Math.min(10, Math.max(1,
    (components.expectedCtr * 0.35) +
    (components.adRelevance * 0.40) +
    (components.landingExperience * 0.25)
  ));

  return {
    components,
    score: Math.round(score * 10) / 10,
    computedAt: new Date().toISOString(),
  };
}

/**
 * Estimate Expected CTR based on vehicle attributes and dealer history.
 * Returns a score from 1-10.
 */
export function estimateExpectedCtr(params: {
  dealerHistoricalCtr?: number;  // e.g., 0.05 = 5%
  vehicleMake?: string;
  vehicleYear?: number;
  photoCount?: number;
  hasPrice?: boolean;
}): number {
  let score = 5; // baseline

  // Dealer historical CTR (normalized)
  if (params.dealerHistoricalCtr !== undefined) {
    const ctrNormalized = Math.min(params.dealerHistoricalCtr / 0.10, 1); // 10% CTR = max
    score += ctrNormalized * 3; // up to +3 points
  }

  // Popular makes get higher CTR
  const makeMultiplier = params.vehicleMake
    ? (BRAND_MULTIPLIERS[params.vehicleMake] || 1.0)
    : 1.0;
  score += (makeMultiplier - 1) * 2;

  // Recent vehicles get higher CTR
  if (params.vehicleYear) {
    const currentYear = new Date().getFullYear();
    const age = currentYear - params.vehicleYear;
    if (age <= 3) score += 1;
    else if (age >= 10) score -= 1;
  }

  // More photos = higher CTR
  if (params.photoCount) {
    if (params.photoCount >= 15) score += 1;
    else if (params.photoCount < 5) score -= 1;
  }

  // Having a price is critical
  if (params.hasPrice === false) score -= 2;

  return Math.min(10, Math.max(1, Math.round(score * 10) / 10));
}

/**
 * Calculate Ad Relevance score based on keyword/context matching.
 * Returns a score from 1-10.
 */
export function calculateAdRelevance(params: {
  searchQuery?: string;
  vehicleMake: string;
  vehicleModel: string;
  vehicleYear: number;
  vehicleKeywords?: string[];
  targetKeywords?: string[];
}): number {
  if (!params.searchQuery) return 6; // default for non-search contexts

  const query = params.searchQuery.toLowerCase();
  let score = 3; // baseline for a match

  // Check brand match
  if (query.includes(params.vehicleMake.toLowerCase())) score += 2;
  // Check model match
  if (query.includes(params.vehicleModel.toLowerCase())) score += 2;
  // Check year match
  if (query.includes(String(params.vehicleYear))) score += 1;

  // Check keyword matches
  const keywords = [...(params.vehicleKeywords || []), ...(params.targetKeywords || [])];
  for (const kw of keywords) {
    if (query.includes(kw.toLowerCase())) {
      score += 0.5;
    }
  }

  return Math.min(10, Math.max(1, Math.round(score * 10) / 10));
}

/**
 * Calculate Landing Experience score based on listing completeness.
 * Returns a score from 1-10.
 */
export function calculateLandingExperience(params: {
  photoCount?: number;
  hasPrice?: boolean;
  hasDescription?: boolean;
  dealerResponseTime?: number;  // hours
  isVerified?: boolean;
  dealerRating?: number;        // 1-5
}): number {
  let score = 5;

  // Photo quality
  if (params.photoCount) {
    if (params.photoCount >= 20) score += 2;
    else if (params.photoCount >= 10) score += 1;
    else if (params.photoCount < 3) score -= 2;
  }

  // Price transparency
  if (params.hasPrice !== false) score += 0.5;
  else score -= 1;

  // Description completeness
  if (params.hasDescription) score += 0.5;

  // Dealer responsiveness
  if (params.dealerResponseTime !== undefined) {
    if (params.dealerResponseTime <= 1) score += 1.5;
    else if (params.dealerResponseTime <= 4) score += 0.5;
    else if (params.dealerResponseTime > 24) score -= 1;
  }

  // Verification
  if (params.isVerified) score += 0.5;

  // Dealer rating
  if (params.dealerRating) {
    score += (params.dealerRating - 3) * 0.5; // 5★ = +1, 1★ = -1
  }

  return Math.min(10, Math.max(1, Math.round(score * 10) / 10));
}

// ---------------------------------------------------------------------------
// Context Multiplier (Section 3.1)
// ---------------------------------------------------------------------------

/**
 * Calculate context multiplier based on targeting match quality.
 */
export function calculateContextMultiplier(params: {
  /** Hour of day (0-23) */
  hour?: number;
  /** Is mobile device */
  isMobile?: boolean;
  /** Purchase Intent Score */
  pis?: PurchaseIntentScore;
  /** Geographic match quality (0-1) */
  geoMatchScore?: number;
  /** Brand match */
  brandMatch?: boolean;
}): number {
  let multiplier = 1.0;

  // Hour weight
  if (params.hour !== undefined) {
    const hourWeight = DEFAULT_HOUR_WEIGHTS.find(h => h.hour === params.hour);
    if (hourWeight) {
      multiplier *= hourWeight.cpcMultiplier;
    }
  }

  // Mobile has slightly higher CTR for vehicle ads
  if (params.isMobile) multiplier *= 1.05;

  // PIS multiplier
  if (params.pis) {
    multiplier *= params.pis.cpcMultiplier;
  }

  // Geo match boosts relevance
  if (params.geoMatchScore !== undefined) {
    multiplier *= (0.8 + params.geoMatchScore * 0.4); // 0.8 to 1.2
  }

  // Brand match boost
  if (params.brandMatch) multiplier *= 1.1;

  return Math.round(multiplier * 100) / 100;
}

// ---------------------------------------------------------------------------
// GSP Auction Algorithm (Section 2.2)
// ---------------------------------------------------------------------------

/**
 * Run a Generalized Second-Price (GSP) auction for a slot.
 *
 * Ad Rank = Max_CPC_bid × Quality_Score × Context_Multiplier
 * Price_Paid = (Ad_Rank_next / Quality_Score_winner) + RD$1
 *
 * @param bids - All eligible bids for this slot
 * @param slotPosition - The ad slot being auctioned
 * @returns Auction results ordered by position
 */
export function runGspAuction(
  bids: AdBid[],
  slotPosition: AdSlotPosition
): SlotAuction {
  const slotConfig = AD_SLOT_CONFIGS.find(c => c.position === slotPosition);
  const maxPositions = slotConfig?.maxAds ?? 3;
  const floorPrice = slotConfig?.cpcFloor ?? slotConfig?.cpmFloor ?? 50;

  // Calculate Ad Rank for each bid
  const rankedBids = bids.map(bid => ({
    ...bid,
    adRank: bid.maxCpcBid * bid.qualityScore.score * bid.contextMultiplier,
  }));

  // Sort by Ad Rank (descending)
  rankedBids.sort((a, b) => b.adRank - a.adRank);

  // Filter out bids below floor price
  const eligibleBids = rankedBids.filter(bid => bid.maxCpcBid >= floorPrice);

  // Award positions (up to maxPositions)
  const results: AuctionResult[] = [];
  const numWinners = Math.min(eligibleBids.length, maxPositions);

  for (let i = 0; i < numWinners; i++) {
    const winner = eligibleBids[i];
    let actualPrice: number;

    if (i < eligibleBids.length - 1) {
      // Second-price: pay minimum to beat the next ad
      const nextAdRank = eligibleBids[i + 1].adRank;
      actualPrice = Math.ceil(nextAdRank / winner.qualityScore.score) + 1;
    } else {
      // Last position pays floor price + RD$1
      actualPrice = floorPrice + 1;
    }

    // Cap at max bid
    actualPrice = Math.min(actualPrice, winner.maxCpcBid);

    results.push({
      winnerId: winner.id,
      position: i + 1,
      actualPricePaid: actualPrice,
      adRank: winner.adRank,
      qualityScore: winner.qualityScore.score,
      vehicleId: winner.vehicleId,
      dealerId: winner.dealerId,
      dealerName: winner.dealerName,
      campaignId: winner.campaignId,
    });
  }

  return {
    slotPosition,
    availablePositions: maxPositions,
    results,
    auctionTimestamp: new Date().toISOString(),
    floorPrice,
  };
}

// ---------------------------------------------------------------------------
// Purchase Intent Score (Section 3.2)
// ---------------------------------------------------------------------------

/** Behavior event weights for PIS calculation */
const PIS_EVENT_WEIGHTS: Record<string, { weight: number; decayDays: number }> = {
  'active_search':         { weight: 8,  decayDays: 7 },
  'view_detail_60s':       { weight: 5,  decayDays: 5 },
  'view_okla_score':       { weight: 6,  decayDays: 10 },
  'contact_whatsapp':      { weight: 12, decayDays: 3 },
  'fill_test_drive_form':  { weight: 15, decayDays: 2 },
  'compare_vehicles':      { weight: 7,  decayDays: 7 },
  'view_score_section':    { weight: 4,  decayDays: 14 },
  'session_return_48_72h': { weight: 3,  decayDays: 5 },
};

/**
 * Calculate Purchase Intent Score for a user based on behavior events.
 */
export function calculatePurchaseIntentScore(events: Array<{
  type: string;
  timestamp: string;
  count?: number;
}>): PurchaseIntentScore {
  const now = Date.now();
  let totalScore = 0;

  for (const event of events) {
    const config = PIS_EVENT_WEIGHTS[event.type];
    if (!config) continue;

    const eventTime = new Date(event.timestamp).getTime();
    const daysSince = (now - eventTime) / (1000 * 60 * 60 * 24);

    // Exponential decay
    const decayFactor = Math.exp(-daysSince / config.decayDays);
    const count = event.count ?? 1;

    totalScore += config.weight * count * decayFactor;
  }

  // Clamp to 0-100
  const score = Math.min(100, Math.max(0, Math.round(totalScore)));

  // Determine level and CPC multiplier
  let level: BuyerIntentLevel;
  let cpcMultiplier: number;

  if (score >= 80) {
    level = 'hot';
    cpcMultiplier = 2.0 + ((score - 80) / 20) * 0.5; // 2.0-2.5
  } else if (score >= 60) {
    level = 'warm';
    cpcMultiplier = 1.4 + ((score - 60) / 20) * 0.5; // 1.4-1.9
  } else if (score >= 40) {
    level = 'interested';
    cpcMultiplier = 1.0 + ((score - 40) / 20) * 0.3; // 1.0-1.3
  } else if (score >= 20) {
    level = 'browsing';
    cpcMultiplier = 0.7 + ((score - 20) / 20) * 0.2; // 0.7-0.9
  } else {
    level = 'cold';
    cpcMultiplier = 0.3 + (score / 20) * 0.3; // 0.3-0.6
  }

  return {
    score,
    level,
    cpcMultiplier: Math.round(cpcMultiplier * 100) / 100,
    lastUpdated: new Date().toISOString(),
  };
}

// ---------------------------------------------------------------------------
// Pacing Algorithm (Section 4.2)
// ---------------------------------------------------------------------------

/**
 * Calculate pacing rate for budget distribution.
 * Pacing_Rate(t) = Budget_Remaining / Time_Remaining × Hour_Weight(t)
 */
export function calculatePacingRate(params: {
  budgetRemaining: number;
  hoursRemaining: number;
  currentHour: number;
}): number {
  if (params.hoursRemaining <= 0 || params.budgetRemaining <= 0) return 0;

  const hourWeight = DEFAULT_HOUR_WEIGHTS.find(h => h.hour === params.currentHour);
  const weight = hourWeight?.conversionWeight ?? (1 / 24);

  // Normalize weight relative to remaining hours
  const remainingWeightSum = DEFAULT_HOUR_WEIGHTS
    .filter(h => h.hour >= params.currentHour)
    .reduce((sum, h) => sum + h.conversionWeight, 0) || 1;

  const normalizedWeight = weight / remainingWeightSum;
  return params.budgetRemaining * normalizedWeight;
}

/**
 * Check if a campaign should participate in the current auction based on pacing.
 */
export function shouldParticipateInAuction(params: {
  dailyBudget: number;
  spentToday: number;
  currentHour: number;
}): boolean {
  const remaining = params.dailyBudget - params.spentToday;
  if (remaining <= 0) return false;

  const _hoursRemaining = 24 - params.currentHour;
  const targetSpendByNow = params.dailyBudget * (params.currentHour / 24);

  // Allow if under-paced or within 10% of target
  return params.spentToday <= targetSpendByNow * 1.1;
}

// ---------------------------------------------------------------------------
// Frequency Capping (Section 4.3)
// ---------------------------------------------------------------------------

/**
 * Check if an ad should be shown based on frequency caps.
 */
export function isWithinFrequencyCap(params: {
  adType: FrequencyCap['adType'];
  dailyImpressions: number;
  weeklyImpressions: number;
}): boolean {
  const cap = DEFAULT_FREQUENCY_CAPS.find(c => c.adType === params.adType);
  if (!cap) return true;

  return (
    params.dailyImpressions < cap.maxPerDay &&
    params.weeklyImpressions < cap.maxPerWeek
  );
}

// ---------------------------------------------------------------------------
// Invalid Traffic Detection (Section 8)
// ---------------------------------------------------------------------------

/**
 * Calculate Invalid Traffic Score.
 * IVT_Score = (Bot_Probability × 0.4) + (Anomaly_Pattern × 0.35) + (Device_Fingerprint_Risk × 0.25)
 * If IVT_Score > 0.70, traffic is invalid.
 */
export function calculateIvtScore(params: {
  botProbability: number;    // 0-1
  anomalyPattern: number;    // 0-1
  deviceFingerprintRisk: number; // 0-1
}): InvalidTrafficScore {
  const score =
    params.botProbability * 0.4 +
    params.anomalyPattern * 0.35 +
    params.deviceFingerprintRisk * 0.25;

  return {
    botProbability: params.botProbability,
    anomalyPattern: params.anomalyPattern,
    deviceFingerprintRisk: params.deviceFingerprintRisk,
    score: Math.round(score * 100) / 100,
    isInvalid: score > 0.70,
  };
}

// ---------------------------------------------------------------------------
// ROI Calculator (Section 6.2)
// ---------------------------------------------------------------------------

/**
 * Calculate estimated ROI for a dealer's ad campaign.
 */
export function calculateRoi(params: {
  monthlyBudget: number;
  estimatedCtr?: number;     // default 5%
  estimatedLeadRate?: number; // default 12%
  estimatedConvRate?: number; // default 35%
  averageMargin?: number;    // default RD$50,000
}): RoiCalculation {
  const ctr = params.estimatedCtr ?? 0.05;
  const leadRate = params.estimatedLeadRate ?? 0.12;
  const convRate = params.estimatedConvRate ?? 0.35;
  const margin = params.averageMargin ?? 50000;

  // Estimate impressions (assuming avg CPC of RD$120)
  const avgCpc = 120;
  const clicks = Math.floor(params.monthlyBudget / avgCpc);
  const impressions = Math.floor(clicks / ctr);
  const leads = Math.floor(clicks * leadRate);
  const sales = Math.max(1, Math.floor(leads * convRate));

  const cpl = leads > 0 ? params.monthlyBudget / leads : 0;
  const cac = sales > 0 ? params.monthlyBudget / sales : 0;
  const totalRevenue = sales * margin;
  const roi = params.monthlyBudget > 0
    ? ((totalRevenue - params.monthlyBudget) / params.monthlyBudget) * 100
    : 0;
  const returnPerDollar = params.monthlyBudget > 0
    ? totalRevenue / params.monthlyBudget
    : 0;

  return {
    monthlyBudget: params.monthlyBudget,
    estimatedImpressions: impressions,
    estimatedClicks: clicks,
    estimatedCpc: avgCpc,
    estimatedLeads: leads,
    estimatedCpl: Math.round(cpl),
    estimatedSales: sales,
    estimatedCac: Math.round(cac),
    averageMarginPerSale: margin,
    totalRevenue,
    roi: Math.round(roi),
    returnPerDollar: Math.round(returnPerDollar * 10) / 10,
  };
}

// ---------------------------------------------------------------------------
// Demo/Simulation — Generate sponsored vehicles for display
// ---------------------------------------------------------------------------

/**
 * Generate simulated sponsored vehicles for a given slot.
 * In production, this would call the backend ad server.
 * For now, it creates mock data that looks realistic.
 */
export function generateSponsoredVehiclesForSlot(
  slotPosition: AdSlotPosition,
  count?: number
): SponsoredVehicle[] {
  const slotConfig = AD_SLOT_CONFIGS.find(c => c.position === slotPosition);
  const maxAds = count ?? slotConfig?.maxAds ?? 3;

  const DEMO_SPONSORED: SponsoredVehicle[] = [
    {
      id: 'sp-001',
      slug: 'toyota-rav4-2023-limited',
      make: 'Toyota',
      model: 'RAV4',
      year: 2023,
      price: 2850000,
      currency: 'DOP',
      mileage: 12500,
      transmission: 'Automática',
      fuelType: 'Gasolina',
      imageUrl: '/images/vehicles/placeholder-suv.jpg',
      location: 'Santo Domingo Este',
      dealerName: 'AutoElite RD',
      dealerRating: 4.8,
      photoCount: 24,
      isVerified: true,
      trim: 'Limited AWD',
      monthlyPayment: 48500,
      campaignId: 'camp-001',
      adBidId: 'bid-001',
      slotPosition,
      sponsorTier: 'premium',
      auctionPosition: 1,
      actualCpc: 142,
      impressionToken: `imp-${Date.now()}-001`,
      clickTrackingUrl: '/api/ads/click?id=sp-001',
    },
    {
      id: 'sp-002',
      slug: 'honda-cr-v-2022-touring',
      make: 'Honda',
      model: 'CR-V',
      year: 2022,
      price: 2450000,
      currency: 'DOP',
      mileage: 18000,
      transmission: 'Automática',
      fuelType: 'Gasolina',
      imageUrl: '/images/vehicles/placeholder-suv-2.jpg',
      location: 'Santiago',
      dealerName: 'MegaCars DR',
      dealerRating: 4.5,
      photoCount: 18,
      isVerified: true,
      trim: 'Touring',
      monthlyPayment: 41200,
      campaignId: 'camp-002',
      adBidId: 'bid-002',
      slotPosition,
      sponsorTier: 'featured',
      auctionPosition: 2,
      actualCpc: 98,
      impressionToken: `imp-${Date.now()}-002`,
      clickTrackingUrl: '/api/ads/click?id=sp-002',
    },
    {
      id: 'sp-003',
      slug: 'hyundai-tucson-2023-sel',
      make: 'Hyundai',
      model: 'Tucson',
      year: 2023,
      price: 2150000,
      currency: 'DOP',
      mileage: 8500,
      transmission: 'Automática',
      fuelType: 'Gasolina',
      imageUrl: '/images/vehicles/placeholder-suv-3.jpg',
      location: 'Santo Domingo Norte',
      dealerName: 'HyundaiPlus RD',
      dealerRating: 4.6,
      photoCount: 22,
      isVerified: true,
      trim: 'SEL',
      monthlyPayment: 36800,
      campaignId: 'camp-003',
      adBidId: 'bid-003',
      slotPosition,
      sponsorTier: 'sponsored',
      auctionPosition: 3,
      actualCpc: 85,
      impressionToken: `imp-${Date.now()}-003`,
      clickTrackingUrl: '/api/ads/click?id=sp-003',
    },
    {
      id: 'sp-004',
      slug: 'toyota-corolla-2024-le',
      make: 'Toyota',
      model: 'Corolla',
      year: 2024,
      price: 1650000,
      currency: 'DOP',
      mileage: 3200,
      transmission: 'Automática',
      fuelType: 'Gasolina',
      imageUrl: '/images/vehicles/placeholder-sedan.jpg',
      location: 'Punta Cana',
      dealerName: 'Toyota Caribe',
      dealerRating: 4.9,
      photoCount: 30,
      isVerified: true,
      trim: 'LE CVT',
      monthlyPayment: 28500,
      campaignId: 'camp-004',
      adBidId: 'bid-004',
      slotPosition,
      sponsorTier: 'premium',
      auctionPosition: 4,
      actualCpc: 155,
      impressionToken: `imp-${Date.now()}-004`,
      clickTrackingUrl: '/api/ads/click?id=sp-004',
    },
    {
      id: 'sp-005',
      slug: 'kia-sportage-2023-ex',
      make: 'Kia',
      model: 'Sportage',
      year: 2023,
      price: 2050000,
      currency: 'DOP',
      mileage: 14200,
      transmission: 'Automática',
      fuelType: 'Gasolina',
      imageUrl: '/images/vehicles/placeholder-suv-4.jpg',
      location: 'La Romana',
      dealerName: 'Kia Premium RD',
      dealerRating: 4.4,
      photoCount: 16,
      isVerified: true,
      trim: 'EX',
      monthlyPayment: 35200,
      campaignId: 'camp-005',
      adBidId: 'bid-005',
      slotPosition,
      sponsorTier: 'featured',
      auctionPosition: 5,
      actualCpc: 92,
      impressionToken: `imp-${Date.now()}-005`,
      clickTrackingUrl: '/api/ads/click?id=sp-005',
    },
    {
      id: 'sp-006',
      slug: 'mercedes-benz-glc-300-2022',
      make: 'Mercedes-Benz',
      model: 'GLC 300',
      year: 2022,
      price: 4250000,
      currency: 'DOP',
      mileage: 22000,
      transmission: 'Automática',
      fuelType: 'Gasolina',
      imageUrl: '/images/vehicles/placeholder-luxury.jpg',
      location: 'Distrito Nacional',
      dealerName: 'AutoLujo RD',
      dealerRating: 4.9,
      photoCount: 35,
      isVerified: true,
      trim: '4MATIC',
      monthlyPayment: 72500,
      campaignId: 'camp-006',
      adBidId: 'bid-006',
      slotPosition,
      sponsorTier: 'premium',
      auctionPosition: 6,
      actualCpc: 210,
      impressionToken: `imp-${Date.now()}-006`,
      clickTrackingUrl: '/api/ads/click?id=sp-006',
    },
  ];

  return DEMO_SPONSORED.slice(0, maxAds).map(v => ({
    ...v,
    slotPosition,
    impressionToken: `imp-${Date.now()}-${v.id}`,
  }));
}
