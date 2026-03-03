// =============================================================================
// OKLA Score™ — TypeScript Type System
// =============================================================================
// Vehicle scoring system (0–1,000) with 7 dimensions for the Dominican Republic
// used-vehicle market. Protects buyers from fraud, inflated prices, and hidden
// damage history from US auctions (COPART/IAAI).
// =============================================================================

// --- Score Levels ---

export type OklaScoreLevel = 'excellent' | 'good' | 'regular' | 'deficient' | 'critical';

export interface OklaScoreLevelInfo {
  level: OklaScoreLevel;
  label: string;
  labelEs: string;
  color: string; // tailwind color key
  emoji: string;
  min: number;
  max: number;
  recommendation: string;
  recommendationEs: string;
}

export const OKLA_SCORE_LEVELS: OklaScoreLevelInfo[] = [
  {
    level: 'excellent',
    label: 'Excellent',
    labelEs: 'Excelente',
    color: 'emerald',
    emoji: '🟢',
    min: 850,
    max: 1000,
    recommendation: 'BUY',
    recommendationEs: 'COMPRAR',
  },
  {
    level: 'good',
    label: 'Good',
    labelEs: 'Bueno',
    color: 'blue',
    emoji: '🔵',
    min: 700,
    max: 849,
    recommendation: 'BUY',
    recommendationEs: 'COMPRAR',
  },
  {
    level: 'regular',
    label: 'Regular',
    labelEs: 'Regular',
    color: 'amber',
    emoji: '🟡',
    min: 550,
    max: 699,
    recommendation: 'WITH CAUTION',
    recommendationEs: 'CON CAUTELA',
  },
  {
    level: 'deficient',
    label: 'Deficient',
    labelEs: 'Deficiente',
    color: 'red',
    emoji: '🔴',
    min: 400,
    max: 549,
    recommendation: 'CAUTION',
    recommendationEs: 'PRECAUCIÓN',
  },
  {
    level: 'critical',
    label: 'Critical',
    labelEs: 'Crítico',
    color: 'slate',
    emoji: '⚫',
    min: 0,
    max: 399,
    recommendation: 'DO NOT BUY',
    recommendationEs: 'NO COMPRAR',
  },
];

// --- VIN & Vehicle Data ---

export interface VinDecodeResult {
  vin: string;
  make: string;
  model: string;
  year: number;
  trim?: string;
  bodyType?: string;
  engineType?: string;
  engineCylinders?: number;
  displacementL?: number;
  fuelType?: string;
  transmission?: string;
  drivetrain?: string; // FWD, RWD, AWD, 4WD
  plantCountry?: string;
  plantCity?: string;
  doors?: number;
  gvwr?: string;
  vehicleType?: string;
  errorCode?: string;
  errorText?: string;
}

export interface VinHistoryReport {
  vin: string;
  titleType: TitleType;
  totalOwners: number;
  accidentCount: number;
  accidentSeverity: AccidentSeverity;
  hasFloodDamage: boolean;
  hasFrameDamage: boolean;
  hasHailDamage: boolean;
  isLemonBuyback: boolean;
  isRentalFleet: boolean;
  isStolenOrCloned: boolean;
  odometerReadings: OdometerReading[];
  serviceRecords: number;
  lastReportedMileage?: number;
  lastReportedDate?: string;
}

export type TitleType = 'Clean' | 'Salvage' | 'Rebuilt' | 'Flood' | 'Junk' | 'Unknown';

export type AccidentSeverity = 'None' | 'Minor' | 'Moderate' | 'Severe' | 'Total';

export interface OdometerReading {
  date: string;
  mileage: number;
  source: string;
}

// --- NHTSA Data ---

export interface NhtsaSafetyRating {
  overallRating: number; // 1-5 stars
  frontalCrashRating?: number;
  sideCrashRating?: number;
  rolloverRating?: number;
}

export interface NhtsaRecall {
  campaignNumber: string;
  component: string;
  summary: string;
  consequence: string;
  remedy: string;
  reportReceivedDate: string;
  isResolved?: boolean;
}

export interface NhtsaComplaintSummary {
  totalComplaints: number;
  componentBreakdown: Record<string, number>;
}

// --- Score Dimensions (D1–D7) ---

export interface DimensionScore {
  dimension: ScoreDimension;
  label: string;
  labelEs: string;
  weight: number; // percentage (e.g., 25 for 25%)
  maxPoints: number;
  rawScore: number; // actual points earned
  weightedScore: number; // rawScore × (weight/100) scaled to 1000
  factors: ScoreFactor[];
}

export type ScoreDimension = 'D1' | 'D2' | 'D3' | 'D4' | 'D5' | 'D6' | 'D7';

export interface ScoreFactor {
  name: string;
  nameEs: string;
  impact: number; // positive = bonus, negative = penalty
  description: string;
  descriptionEs: string;
  source?: string;
}

export const DIMENSION_CONFIG: Record<
  ScoreDimension,
  {
    label: string;
    labelEs: string;
    weight: number;
    maxPoints: number;
    icon: string;
  }
> = {
  D1: {
    label: 'VIN History (US)',
    labelEs: 'Historial en EE.UU.',
    weight: 25,
    maxPoints: 250,
    icon: '🔍',
  },
  D2: {
    label: 'Mechanical Condition',
    labelEs: 'Condición Mecánica',
    weight: 20,
    maxPoints: 200,
    icon: '⚙️',
  },
  D3: {
    label: 'Mileage / Odometer',
    labelEs: 'Kilometraje / Odómetro',
    weight: 18,
    maxPoints: 180,
    icon: '📊',
  },
  D4: {
    label: 'Price vs Market',
    labelEs: 'Precio vs. Mercado',
    weight: 17,
    maxPoints: 170,
    icon: '💰',
  },
  D5: {
    label: 'Safety & Recalls',
    labelEs: 'Seguridad y Recalls',
    weight: 10,
    maxPoints: 100,
    icon: '🛡️',
  },
  D6: {
    label: 'Depreciation & Year',
    labelEs: 'Depreciación y Año',
    weight: 6,
    maxPoints: 60,
    icon: '📅',
  },
  D7: {
    label: 'Seller Reputation',
    labelEs: 'Reputación del Vendedor',
    weight: 4,
    maxPoints: 40,
    icon: '⭐',
  },
};

// --- Full Score Report ---

export interface OklaScoreReport {
  id: string;
  vin: string;
  vehicleId?: string;
  score: number; // 0–1,000
  level: OklaScoreLevel;
  dimensions: DimensionScore[];
  priceAnalysis: PriceAnalysis;
  alerts: ScoreAlert[];
  vinDecode: VinDecodeResult;
  safetyRating?: NhtsaSafetyRating;
  recalls: NhtsaRecall[];
  generatedAt: string;
  expiresAt: string;
  version: string;
}

export interface PriceAnalysis {
  listedPriceDOP: number;
  fairPriceDOP: number;
  fairPriceUSD: number;
  priceDiffPercent: number; // negative = below market (good), positive = above
  priceVerdict: PriceVerdict;
  exchangeRate: number; // USD → DOP
  sources: PriceSource[];
}

export type PriceVerdict =
  | 'excellent_deal' // ≤ -15%
  | 'good_price' // -15% to -5%
  | 'fair_price' // ±5%
  | 'expensive' // +5% to +15%
  | 'very_expensive' // +15% to +30%
  | 'abusive_price'; // > +30%

export interface PriceSource {
  name: string;
  averagePrice: number;
  currency: string;
  weight: number; // percentage weight in calculation
  sampleSize: number;
  lastUpdated: string;
}

export interface ScoreAlert {
  severity: 'critical' | 'warning' | 'info';
  code: string;
  title: string;
  titleEs: string;
  description: string;
  descriptionEs: string;
  dimension: ScoreDimension;
}

// --- Score Badge for Listings ---

export type OklaBadgeType =
  | 'certified_excellence' // 850–1000
  | 'verified' // 700–849
  | 'in_evaluation' // 550–699
  | 'no_verification'; // < 550

export interface OklaBadge {
  type: OklaBadgeType;
  score: number;
  level: OklaScoreLevel;
  labelEs: string;
  shortLabelEs: string;
}

// --- API Request/Response ---

export interface ScoreLookupRequest {
  vin: string;
  listedPriceDOP?: number;
  declaredMileage?: number;
  mileageUnit?: 'miles' | 'km';
  sellerId?: string;
  sellerType?: 'dealer' | 'individual';
}

export interface ScoreLookupResponse {
  success: boolean;
  data?: OklaScoreReport;
  error?: string;
  cached?: boolean;
}

// --- Helper Functions ---

export function getScoreLevel(score: number): OklaScoreLevelInfo {
  return (
    OKLA_SCORE_LEVELS.find(l => score >= l.min && score <= l.max) ||
    OKLA_SCORE_LEVELS[OKLA_SCORE_LEVELS.length - 1]
  );
}

export function getScoreBadge(score: number): OklaBadge {
  const level = getScoreLevel(score);
  if (score >= 850) {
    return {
      type: 'certified_excellence',
      score,
      level: level.level,
      labelEs: 'OKLA Certified Excellence',
      shortLabelEs: 'Certificado',
    };
  }
  if (score >= 700) {
    return {
      type: 'verified',
      score,
      level: level.level,
      labelEs: 'OKLA Verified',
      shortLabelEs: 'Verificado',
    };
  }
  if (score >= 550) {
    return {
      type: 'in_evaluation',
      score,
      level: level.level,
      labelEs: 'En Evaluación',
      shortLabelEs: 'En Evaluación',
    };
  }
  return {
    type: 'no_verification',
    score,
    level: level.level,
    labelEs: 'Sin Verificación OKLA',
    shortLabelEs: 'Sin Verificar',
  };
}

export function getScoreColor(score: number): string {
  if (score >= 850) return 'emerald';
  if (score >= 700) return 'blue';
  if (score >= 550) return 'amber';
  if (score >= 400) return 'red';
  return 'slate';
}
