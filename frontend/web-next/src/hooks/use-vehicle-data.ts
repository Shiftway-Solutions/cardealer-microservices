/**
 * React Query hooks for Vehicle Data (History, Specs, Market Pricing)
 *
 * INTEGRATION FIX: The VehicleDataController had 11 endpoints with zero
 * frontend hooks. These hooks enable:
 * - D1 VIN History display on listing pages
 * - Market price analysis for buyers
 * - Vehicle spec enrichment
 * - AI price recommendation for sellers
 */

import { useQuery, useMutation } from '@tanstack/react-query';
import {
  getVehicleHistory,
  getVehicleHistorySummary,
  isHistoryAvailable,
  getVehicleSpecs,
  getVehicleTrims,
  decodeVinSpecs,
  getMarketPrice,
  getComparableListings,
  getMarketTrend,
  getPriceRecommendation,
  getProviderStatus,
  type VehicleHistoryReport,
  type VehicleHistorySummary,
  type VehicleSpecification,
  type TrimInfo,
  type MarketPriceAnalysis,
  type MarketListing,
  type MarketTrend,
  type PriceRecommendation,
  type ProviderStatus,
} from '@/services/vehicle-data';

// ============================================================================
// Vehicle History Hooks
// ============================================================================

/** Get full vehicle history report by VIN */
export function useVehicleHistory(vin: string | undefined) {
  return useQuery<VehicleHistoryReport>({
    queryKey: ['vehicle-history', vin],
    queryFn: () => getVehicleHistory(vin!),
    enabled: !!vin && vin.length === 17,
    staleTime: 1000 * 60 * 60, // 1 hour — history doesn't change often
    gcTime: 1000 * 60 * 60 * 24, // 24 hours
  });
}

/** Get quick history summary */
export function useVehicleHistorySummary(vin: string | undefined) {
  return useQuery<VehicleHistorySummary>({
    queryKey: ['vehicle-history-summary', vin],
    queryFn: () => getVehicleHistorySummary(vin!),
    enabled: !!vin && vin.length === 17,
    staleTime: 1000 * 60 * 60,
  });
}

/** Check if history report is available */
export function useHistoryAvailability(vin: string | undefined) {
  return useQuery<boolean>({
    queryKey: ['vehicle-history-available', vin],
    queryFn: () => isHistoryAvailable(vin!),
    enabled: !!vin && vin.length === 17,
    staleTime: 1000 * 60 * 30, // 30 min
  });
}

// ============================================================================
// Vehicle Specs Hooks
// ============================================================================

/** Get vehicle specs by make/model/year */
export function useVehicleSpecs(
  make: string | undefined,
  model: string | undefined,
  year: number | undefined
) {
  return useQuery<VehicleSpecification>({
    queryKey: ['vehicle-specs', make, model, year],
    queryFn: () => getVehicleSpecs(make!, model!, year!),
    enabled: !!make && !!model && !!year,
    staleTime: 1000 * 60 * 60 * 24 * 7, // 1 week — specs don't change
  });
}

/** Get available trims */
export function useVehicleTrims(
  make: string | undefined,
  model: string | undefined,
  year: number | undefined
) {
  return useQuery<TrimInfo[]>({
    queryKey: ['vehicle-trims', make, model, year],
    queryFn: () => getVehicleTrims(make!, model!, year!),
    enabled: !!make && !!model && !!year,
    staleTime: 1000 * 60 * 60 * 24 * 7,
  });
}

/** Decode VIN to get specs */
export function useVinSpecsDecode(vin: string | undefined) {
  return useQuery<VehicleSpecification>({
    queryKey: ['vehicle-specs-vin', vin],
    queryFn: () => decodeVinSpecs(vin!),
    enabled: !!vin && vin.length === 17,
    staleTime: 1000 * 60 * 60 * 24,
  });
}

// ============================================================================
// Market Price Hooks
// ============================================================================

/** Get market price analysis */
export function useMarketPrice(
  make: string | undefined,
  model: string | undefined,
  year: number | undefined,
  options?: { mileage?: number; condition?: string }
) {
  return useQuery<MarketPriceAnalysis>({
    queryKey: ['market-price', make, model, year, options?.mileage, options?.condition],
    queryFn: () => getMarketPrice(make!, model!, year!, options?.mileage, options?.condition),
    enabled: !!make && !!model && !!year,
    staleTime: 1000 * 60 * 60 * 4, // 4 hours — prices change throughout the day
  });
}

/** Get comparable listings */
export function useComparableListings(
  make: string | undefined,
  model: string | undefined,
  year: number | undefined,
  maxResults = 10
) {
  return useQuery<MarketListing[]>({
    queryKey: ['comparable-listings', make, model, year, maxResults],
    queryFn: () => getComparableListings(make!, model!, year!, maxResults),
    enabled: !!make && !!model && !!year,
    staleTime: 1000 * 60 * 60 * 2,
  });
}

/** Get market price trends */
export function useMarketTrend(make: string | undefined, model: string | undefined, months = 12) {
  return useQuery<MarketTrend>({
    queryKey: ['market-trend', make, model, months],
    queryFn: () => getMarketTrend(make!, model!, months),
    enabled: !!make && !!model,
    staleTime: 1000 * 60 * 60 * 24, // 1 day
  });
}

/** Get AI price recommendation (mutation — requires user input) */
export function usePriceRecommendation() {
  return useMutation<
    PriceRecommendation,
    Error,
    {
      make: string;
      model: string;
      year: number;
      mileage: number;
      condition: string;
      province?: string;
    }
  >({
    mutationFn: params => getPriceRecommendation(params),
  });
}

/** Get external provider status */
export function useProviderStatus() {
  return useQuery<ProviderStatus[]>({
    queryKey: ['provider-status'],
    queryFn: () => getProviderStatus(),
    staleTime: 1000 * 60 * 5, // 5 min
  });
}
