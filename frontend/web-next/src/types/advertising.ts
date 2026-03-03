// =============================================================================
// OKLA - Advertising System TypeScript Types
// =============================================================================

// --- Enums ---
export type AdPlacementType = 'FeaturedSpot' | 'PremiumSpot';
export type CampaignStatus =
  | 'PendingPayment'
  | 'Active'
  | 'Paused'
  | 'Cancelled'
  | 'Completed'
  | 'Expired';
export type CampaignPricingModel = 'PerView' | 'PerClick' | 'PerDay' | 'FixedMonthly' | 'FlatFee';
export type RotationAlgorithmType =
  | 'WeightedRandom'
  | 'RoundRobin'
  | 'CTROptimized'
  | 'BudgetPriority';

// --- Campaign ---
export interface AdCampaign {
  id: string;
  ownerId: string;
  ownerType: string;
  vehicleId: string;
  placementType: AdPlacementType;
  pricingModel: CampaignPricingModel;
  pricePerUnit: number;
  totalBudget: number;
  remainingBudget: number;
  status: CampaignStatus;
  qualityScore: number;
  totalViews: number;
  totalClicks: number;
  ctr: number;
  startDate: string;
  endDate: string;
  createdAt: string;
  updatedAt: string;
}

export interface AdCampaignSummary {
  id: string;
  vehicleId: string;
  placementType: AdPlacementType;
  status: CampaignStatus;
  totalBudget: number;
  remainingBudget: number;
  totalViews: number;
  totalClicks: number;
  ctr: number;
  startDate: string;
  endDate: string;
}

export interface CreateCampaignRequest {
  name?: string;
  ownerId: string;
  ownerType: string;
  vehicleId?: string;
  vehicleIds?: string[];
  placementType: AdPlacementType;
  pricingModel: CampaignPricingModel;
  totalBudget: number;
  dailyBudget?: number;
  bidAmount?: number;
  startDate: string;
  endDate: string;
}

// --- Rotation ---
export interface RotationConfig {
  id: string;
  section: AdPlacementType;
  algorithm: RotationAlgorithmType;
  maxSlots: number;
  rotationIntervalMinutes: number;
  minQualityScore: number;
  isActive: boolean;
}

export interface HomepageRotation {
  section: AdPlacementType;
  items: RotatedVehicle[];
  generatedAt: string;
  nextRotationAt: string;
}

export interface RotatedVehicle {
  vehicleId: string;
  campaignId: string;
  position: number;
  qualityScore: number;
  title?: string;
  slug?: string;
  imageUrl?: string;
  price?: number;
  currency?: string;
  location?: string;
  isFeatured?: boolean;
  isPremium?: boolean;
}

// --- Homepage Config ---
export interface CategoryImageConfig {
  id: string;
  categoryKey: string;
  displayName: string;
  imageUrl: string;
  description: string;
  href: string;
  accentColor: string;
  displayOrder: number;
  isActive: boolean;
  isTrending: boolean;
}

export interface BrandConfig {
  id: string;
  brandKey: string;
  displayName: string;
  logoUrl: string;
  vehicleCount: number;
  displayOrder: number;
  isActive: boolean;
}

export interface UpdateCategoryRequest {
  categoryKey: string;
  displayName?: string;
  imageUrl?: string;
  description?: string;
  href?: string;
  accentColor?: string;
  displayOrder?: number;
  isActive?: boolean;
  isTrending?: boolean;
}

export interface UpdateBrandRequest {
  brandKey: string;
  displayName?: string;
  logoUrl?: string;
  vehicleCount?: number;
  displayOrder?: number;
  isActive?: boolean;
}

// --- Reports ---
export interface CampaignReport {
  campaignId: string;
  totalViews: number;
  totalClicks: number;
  ctr: number;
  totalSpent: number;
  remainingBudget: number;
  dailyData: DailyDataPoint[];
}

export interface OwnerReport {
  ownerId: string;
  ownerType: string;
  activeCampaigns: number;
  totalCampaigns: number;
  totalImpressions: number;
  totalClicks: number;
  overallCtr: number;
  totalSpent: number;
  dailyImpressions: DailyDataPoint[];
  dailyClicks: DailyDataPoint[];
}

export interface PlatformReport {
  totalActiveCampaigns: number;
  totalRevenue: number;
  averageCtr: number;
  totalImpressions: number;
  totalClicks: number;
  dailyData: DailyDataPoint[];
}

export interface DailyDataPoint {
  date: string;
  views: number;
  clicks: number;
  spent: number;
}

// --- Pricing ---
export interface PricingEstimate {
  placementType: AdPlacementType;
  pricingModels: PricingModelOption[];
}

export interface PricingModelOption {
  model: CampaignPricingModel;
  pricePerUnit: number;
  currency: string;
  description: string;
  estimatedDailyViews: number;
}

// --- Tracking ---
export interface RecordImpressionRequest {
  campaignId: string;
  vehicleId: string;
  section: AdPlacementType;
  viewerUserId?: string;
  viewerIp?: string;
}

export interface RecordClickRequest {
  campaignId: string;
  vehicleId: string;
  section: AdPlacementType;
  clickerUserId?: string;
  clickerIp?: string;
  destinationUrl?: string;
}
