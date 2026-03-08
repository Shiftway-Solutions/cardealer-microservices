/**
 * Vehicle Data Service
 *
 * Frontend service for vehicle history, specs, and market pricing.
 * Connects to the VehicleDataController in VehiclesSaleService.
 *
 * INTEGRATION FIX: These 11 endpoints existed in backend but had ZERO frontend integration.
 * This service enables D1 VIN History (25% of OKLA Score), market price analysis,
 * and vehicle spec enrichment for listings.
 */

import { apiClient } from '@/lib/api-client';

// ============================================================================
// Types — Vehicle History (CARFAX/VinAudit)
// ============================================================================

export interface VehicleHistoryReport {
  vin: string;
  provider: string;
  ownerCount: number;
  titleStatus: string;
  hasAccidents: boolean;
  accidentCount: number;
  hasFloodDamage: boolean;
  hasFireDamage: boolean;
  isStolen: boolean;
  hasOpenRecalls: boolean;
  recallCount: number;
  serviceHistory: ServiceRecord[];
  ownershipHistory: OwnershipRecord[];
  titleHistory: TitleRecord[];
  lastReportedMileage: number;
  lastReportedMileageDate: string;
  odometerRollback: boolean;
  reportUrl: string;
  generatedAt: string;
}

export interface ServiceRecord {
  date: string;
  description: string;
  mileage: number;
  location: string;
}

export interface OwnershipRecord {
  ownerNumber: number;
  purchaseDate: string;
  saleDate?: string;
  state: string;
  usageType: string;
}

export interface TitleRecord {
  date: string;
  type: string;
  state: string;
  mileage: number;
}

export interface VehicleHistorySummary {
  vin: string;
  ownerCount: number;
  titleStatus: string;
  accidentCount: number;
  hasOpenRecalls: boolean;
  lastReportedMileage: number;
  odometerRollback: boolean;
  provider: string;
}

// ============================================================================
// Types — Vehicle Specs (Edmunds)
// ============================================================================

export interface VehicleSpecification {
  make: string;
  model: string;
  year: number;
  trim?: string;
  bodyType: string;
  engine: EngineSpecs;
  transmission: TransmissionSpecs;
  fuelEconomy: FuelEconomySpecs;
  dimensions: DimensionSpecs;
  performance: PerformanceSpecs;
  safety: SafetySpecs;
  msrpUsd: number;
  provider: string;
}

export interface EngineSpecs {
  type: string;
  cylinders: number;
  displacementL: number;
  horsePower: number;
  torqueNm: number;
  fuelType: string;
}

export interface TransmissionSpecs {
  type: string;
  speeds: number;
  drivetrain: string;
}

export interface FuelEconomySpecs {
  cityMpg: number;
  highwayMpg: number;
  combinedMpg: number;
  fuelTankGallons: number;
}

export interface DimensionSpecs {
  lengthIn: number;
  widthIn: number;
  heightIn: number;
  wheelbaseIn: number;
  curbWeightLbs: number;
  cargoVolumesCuFt: number;
}

export interface PerformanceSpecs {
  zeroToSixtySec: number;
  topSpeedMph: number;
  towingCapacityLbs: number;
}

export interface SafetySpecs {
  nhtsaOverallRating: number;
  nhtsaFrontalRating: number;
  nhtsaSideRating: number;
  nhtsaRolloverRating: number;
  airbags: number;
  hasABS: boolean;
  hasESC: boolean;
  hasBackupCamera: boolean;
  hasBlindSpotMonitoring: boolean;
}

export interface TrimInfo {
  name: string;
  year: number;
  msrpUsd: number;
  features: string[];
}

// ============================================================================
// Types — Market Price (MarketCheck)
// FIX B4: Aligned field names with backend C# MarketPriceAnalysis record.
// Backend returns DOP (currency field indicates), not USD.
// ============================================================================

export interface MarketPriceAnalysis {
  make: string;
  model: string;
  year: number;
  trim?: string | null;
  averagePrice: number; // In currency specified by `currency` field (DOP)
  medianPrice: number;
  minPrice: number;
  maxPrice: number;
  currency: string; // "DOP" | "USD"
  sampleSize: number;
  priceAboveMarket: number | null;
  marketPosition: string; // "Below Market" | "At Market" | "Above Market"
  depreciationRate: number | null;
  analyzedAt: string;
  provider: string;
}

// FIX B4: Aligned with backend MarketListing record
export interface MarketListing {
  listingId: string;
  source: string; // "OKLA" | "Facebook" | "CoroMotors" | "SuperCarros"
  make: string;
  model: string;
  year: number;
  trim?: string | null;
  price: number;
  currency: string;
  mileageKm?: number | null;
  condition?: string | null;
  province?: string | null;
  dealerName?: string | null;
  listedDate: string;
  listingUrl?: string | null;
}

// FIX B4: Aligned with backend VehiclesSaleService PriceRecommendation record
export interface PriceRecommendation {
  recommendedPrice: number;
  quickSalePrice: number; // 5-10% below recommended
  premiumPrice: number; // 5-10% above recommended
  currency: string;
  explanation: string;
  daysToSellEstimate: number;
  confidenceScore: number; // 0-1
  provider: string;
}

// FIX B4: Aligned with backend VehiclesSaleService MarketTrend record
export interface MarketTrend {
  make: string;
  model: string;
  year?: number | null;
  priceHistory: PriceDataPoint[];
  trendDirection: number; // +1.5 = 1.5% monthly increase, -2.3 = 2.3% decline
  trendLabel: string; // "Rising" | "Stable" | "Declining"
  totalListingsAnalyzed: number;
  provider: string;
}

// FIX B4: Aligned with backend PriceDataPoint record
export interface PriceDataPoint {
  month: string;
  averagePrice: number;
  medianPrice: number;
  listingCount: number;
}

// FIX B4: Aligned with backend VehicleDataController anonymous type
export interface ProviderStatus {
  provider: string;
  status: string; // "Active" | "Inactive" etc.
  note: string;
}

// ============================================================================
// API Functions — Vehicle History
// ============================================================================

/**
 * Get full vehicle history report by VIN
 * Requires authentication. Returns CARFAX/VinAudit data.
 */
export async function getVehicleHistory(vin: string): Promise<VehicleHistoryReport> {
  const response = await apiClient.get<VehicleHistoryReport>(`/api/vehicle-data/history/${vin}`);
  return response.data;
}

/**
 * Get quick vehicle history summary (lighter response)
 */
export async function getVehicleHistorySummary(vin: string): Promise<VehicleHistorySummary> {
  const response = await apiClient.get<VehicleHistorySummary>(
    `/api/vehicle-data/history/${vin}/summary`
  );
  return response.data;
}

/**
 * Check if a history report is available for a VIN
 */
export async function isHistoryAvailable(vin: string): Promise<boolean> {
  const response = await apiClient.get<{ available: boolean }>(
    `/api/vehicle-data/history/${vin}/available`
  );
  return response.data.available;
}

// ============================================================================
// API Functions — Vehicle Specs
// ============================================================================

/**
 * Get vehicle specs by make/model/year
 */
export async function getVehicleSpecs(
  make: string,
  model: string,
  year: number
): Promise<VehicleSpecification> {
  const response = await apiClient.get<VehicleSpecification>(
    `/api/vehicle-data/specs/${encodeURIComponent(make)}/${encodeURIComponent(model)}/${year}`
  );
  return response.data;
}

/**
 * Get available trims for make/model/year
 */
export async function getVehicleTrims(
  make: string,
  model: string,
  year: number
): Promise<TrimInfo[]> {
  const response = await apiClient.get<TrimInfo[]>(
    `/api/vehicle-data/specs/${encodeURIComponent(make)}/${encodeURIComponent(model)}/${year}/trims`
  );
  return response.data;
}

/**
 * Decode a VIN to get vehicle specs
 */
export async function decodeVinSpecs(vin: string): Promise<VehicleSpecification> {
  const response = await apiClient.get<VehicleSpecification>(
    `/api/vehicle-data/specs/decode/${vin}`
  );
  return response.data;
}

// ============================================================================
// API Functions — Market Pricing
// ============================================================================

/**
 * Get market price analysis for a vehicle
 */
export async function getMarketPrice(
  make: string,
  model: string,
  year: number,
  mileage?: number,
  condition?: string
): Promise<MarketPriceAnalysis> {
  let url = `/api/vehicle-data/market-price/${encodeURIComponent(make)}/${encodeURIComponent(model)}/${year}`;
  const params = new URLSearchParams();
  if (mileage !== undefined) params.set('mileage', mileage.toString());
  if (condition) params.set('condition', condition);
  if (params.toString()) url += `?${params.toString()}`;

  const response = await apiClient.get<MarketPriceAnalysis>(url);
  return response.data;
}

/**
 * Get comparable listings in the market
 */
export async function getComparableListings(
  make: string,
  model: string,
  year: number,
  maxResults: number = 10
): Promise<MarketListing[]> {
  const response = await apiClient.get<MarketListing[]>(
    `/api/vehicle-data/market-price/${encodeURIComponent(make)}/${encodeURIComponent(model)}/${year}/comparables?maxResults=${maxResults}`
  );
  return response.data;
}

/**
 * Get market price trends for a vehicle
 */
export async function getMarketTrend(
  make: string,
  model: string,
  months: number = 12
): Promise<MarketTrend> {
  const response = await apiClient.get<MarketTrend>(
    `/api/vehicle-data/market-price/${encodeURIComponent(make)}/${encodeURIComponent(model)}/trend?months=${months}`
  );
  return response.data;
}

/**
 * Get AI-powered price recommendation for a vehicle
 */
export async function getPriceRecommendation(params: {
  make: string;
  model: string;
  year: number;
  mileage: number;
  condition: string;
  province?: string;
}): Promise<PriceRecommendation> {
  const response = await apiClient.post<PriceRecommendation>(
    '/api/vehicle-data/market-price/recommendation',
    params
  );
  return response.data;
}

/**
 * Get status of all external data providers
 */
export async function getProviderStatus(): Promise<ProviderStatus[]> {
  const response = await apiClient.get<ProviderStatus[]>('/api/vehicle-data/providers/status');
  return response.data;
}
