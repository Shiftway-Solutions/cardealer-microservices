/**
 * Vehicle Intelligence Hooks
 *
 * React Query hooks for VehicleIntelligenceService API
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getPriceSuggestion,
  createPriceAnalysis,
  getLatestPriceAnalysis,
  getPriceAnalysisById,
  getDemandByCategory,
  getMarketAnalysis,
  getMarketAnalysisDashboard,
  getDemandPrediction,
  type PriceSuggestionRequest,
  type CreatePriceAnalysisRequest,
  type PriceAnalysis,
} from '@/services/vehicle-intelligence';

// ============================================================================
// Query Keys
// ============================================================================

export const vehicleIntelligenceKeys = {
  all: ['vehicle-intelligence'] as const,
  pricing: () => [...vehicleIntelligenceKeys.all, 'pricing'] as const,
  priceAnalysis: (vehicleId: string) =>
    [...vehicleIntelligenceKeys.pricing(), 'analysis', vehicleId] as const,
  priceAnalysisById: (id: string) => [...vehicleIntelligenceKeys.pricing(), 'id', id] as const,
  demand: () => [...vehicleIntelligenceKeys.all, 'demand'] as const,
  demandByCategory: () => [...vehicleIntelligenceKeys.demand(), 'categories'] as const,
  demandPrediction: (make: string, model: string, year: number) =>
    [...vehicleIntelligenceKeys.demand(), 'prediction', make, model, year] as const,
  market: () => [...vehicleIntelligenceKeys.all, 'market'] as const,
  marketAnalysis: (make: string, model: string, year: number) =>
    [...vehicleIntelligenceKeys.market(), 'analysis', make, model, year] as const,
  marketDashboard: (filters?: Record<string, unknown>) =>
    [...vehicleIntelligenceKeys.market(), 'dashboard', filters] as const,
};

// ============================================================================
// Pricing Hooks
// ============================================================================

/**
 * Get price suggestion for publishing wizard
 */
export function usePriceSuggestion() {
  return useMutation({
    mutationFn: (data: PriceSuggestionRequest) => getPriceSuggestion(data),
  });
}

/**
 * Create full price analysis
 */
export function useCreatePriceAnalysis() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreatePriceAnalysisRequest) => createPriceAnalysis(data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: vehicleIntelligenceKeys.priceAnalysis(variables.vehicleId),
      });
    },
  });
}

/**
 * Get latest price analysis for a vehicle
 */
export function useLatestPriceAnalysis(vehicleId: string) {
  return useQuery({
    queryKey: vehicleIntelligenceKeys.priceAnalysis(vehicleId),
    queryFn: () => getLatestPriceAnalysis(vehicleId),
    enabled: !!vehicleId,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Get price analysis by ID
 */
export function usePriceAnalysis(id: string) {
  return useQuery({
    queryKey: vehicleIntelligenceKeys.priceAnalysisById(id),
    queryFn: () => getPriceAnalysisById(id),
    enabled: !!id,
  });
}

// ============================================================================
// Demand Hooks
// ============================================================================

/**
 * Get demand by category
 */
export function useDemandByCategory() {
  return useQuery({
    queryKey: vehicleIntelligenceKeys.demandByCategory(),
    queryFn: getDemandByCategory,
    staleTime: 10 * 60 * 1000, // 10 minutes
  });
}

/**
 * Get demand prediction for specific vehicle
 */
export function useDemandPrediction(make: string, model: string, year: number) {
  return useQuery({
    queryKey: vehicleIntelligenceKeys.demandPrediction(make, model, year),
    queryFn: () => getDemandPrediction(make, model, year),
    enabled: !!make && !!model && !!year,
    staleTime: 30 * 60 * 1000, // 30 minutes
  });
}

// ============================================================================
// Market Analysis Hooks
// ============================================================================

/**
 * Get market analysis for specific make/model/year
 */
export function useMarketAnalysis(make: string, model: string, year: number) {
  return useQuery({
    queryKey: vehicleIntelligenceKeys.marketAnalysis(make, model, year),
    queryFn: () => getMarketAnalysis(make, model, year),
    enabled: !!make && !!model && !!year,
    staleTime: 15 * 60 * 1000, // 15 minutes
  });
}

/**
 * Get market analysis dashboard data
 */
export function useMarketAnalysisDashboard(filters?: {
  make?: string;
  bodyType?: string;
  minYear?: number;
  maxYear?: number;
}) {
  return useQuery({
    queryKey: vehicleIntelligenceKeys.marketDashboard(filters),
    queryFn: () => getMarketAnalysisDashboard(filters),
    staleTime: 10 * 60 * 1000, // 10 minutes
  });
}

// ============================================================================
// Combined Hooks
// ============================================================================

/**
 * Get all pricing intelligence for dealer inventory
 */
export function useDealerPricingIntelligence(vehicleIds: string[]) {
  const queries = vehicleIds.map(id => ({
    queryKey: vehicleIntelligenceKeys.priceAnalysis(id),
    queryFn: () => getLatestPriceAnalysis(id),
    enabled: !!id,
  }));

  // This would need to use useQueries from tanstack-query
  // For now, return a simple structure
  return {
    isLoading: false,
    data: [] as (PriceAnalysis | null)[],
  };
}
