/**
 * useAdvertising — React hooks for the OKLA Advertising System
 *
 * TanStack Query hooks for campaigns, rotation, homepage config, and reports.
 */

'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  createCampaign,
  getCampaignById,
  getCampaignsByOwner,
  pauseCampaign,
  resumeCampaign,
  cancelCampaign,
  recordImpression,
  recordClick,
  getHomepageRotation,
  getRotationConfig,
  updateRotationConfig,
  refreshRotation,
  getCategories,
  updateCategory,
  getBrands,
  updateBrand,
  getCampaignReport,
  getPlatformReport,
  getOwnerReport,
  getPricingEstimate,
} from '@/services/advertising';
import type {
  AdPlacementType,
  AdvertiserReport,
  CampaignStatus,
  CreateCampaignRequest,
  UpdateCategoryRequest,
  UpdateBrandRequest,
  RecordImpressionRequest,
  RecordClickRequest,
  RotationConfig,
} from '@/types/advertising';

// ============================================================
// QUERY KEY FACTORY
// ============================================================

export const advertisingKeys = {
  all: ['advertising'] as const,

  // Campaigns
  campaigns: () => [...advertisingKeys.all, 'campaigns'] as const,
  campaignDetail: (id: string) => [...advertisingKeys.campaigns(), id] as const,
  campaignsByOwner: (ownerId: string, ownerType: string, status?: CampaignStatus) =>
    [...advertisingKeys.campaigns(), 'owner', ownerId, ownerType, status] as const,

  // Rotation
  rotation: () => [...advertisingKeys.all, 'rotation'] as const,
  rotationSection: (section: AdPlacementType) => [...advertisingKeys.rotation(), section] as const,
  rotationConfig: (section: AdPlacementType) =>
    [...advertisingKeys.rotation(), 'config', section] as const,

  // Homepage config
  homepage: () => [...advertisingKeys.all, 'homepage'] as const,
  categories: (includeHidden?: boolean) =>
    [...advertisingKeys.homepage(), 'categories', includeHidden] as const,
  brands: (includeHidden?: boolean) =>
    [...advertisingKeys.homepage(), 'brands', includeHidden] as const,

  // Reports
  reports: () => [...advertisingKeys.all, 'reports'] as const,
  campaignReport: (id: string, daysBack: number) =>
    [...advertisingKeys.reports(), 'campaign', id, daysBack] as const,
  platformReport: (daysBack: number) =>
    [...advertisingKeys.reports(), 'platform', daysBack] as const,
  ownerReport: (ownerId: string, ownerType: string, daysBack: number) =>
    [...advertisingKeys.reports(), 'owner', ownerId, ownerType, daysBack] as const,
  advertiserReport: (ownerId: string, period: string) =>
    [...advertisingKeys.reports(), 'advertiser', ownerId, period] as const,

  // Pricing
  pricing: (type: AdPlacementType) => [...advertisingKeys.all, 'pricing', type] as const,
};

// ============================================================
// CAMPAIGN HOOKS
// ============================================================

export function useCampaign(id: string) {
  return useQuery({
    queryKey: advertisingKeys.campaignDetail(id),
    queryFn: () => getCampaignById(id),
    enabled: !!id,
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
}

export function useCampaignsByOwner(
  ownerId: string,
  ownerType: string = 'Individual',
  status?: CampaignStatus,
  page: number = 1,
  pageSize: number = 20
) {
  return useQuery({
    queryKey: [...advertisingKeys.campaignsByOwner(ownerId, ownerType, status), page, pageSize],
    queryFn: () => getCampaignsByOwner(ownerId, ownerType, status, page, pageSize),
    enabled: !!ownerId,
    staleTime: 60 * 1000, // 1 minute
  });
}

export function useCreateCampaign() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateCampaignRequest) => createCampaign(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: advertisingKeys.campaigns() });
    },
  });
}

export function usePauseCampaign() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => pauseCampaign(id),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: advertisingKeys.campaignDetail(id) });
      queryClient.invalidateQueries({ queryKey: advertisingKeys.campaigns() });
    },
  });
}

export function useResumeCampaign() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => resumeCampaign(id),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: advertisingKeys.campaignDetail(id) });
      queryClient.invalidateQueries({ queryKey: advertisingKeys.campaigns() });
    },
  });
}

export function useCancelCampaign() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => cancelCampaign(id),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: advertisingKeys.campaignDetail(id) });
      queryClient.invalidateQueries({ queryKey: advertisingKeys.campaigns() });
    },
  });
}

// ============================================================
// TRACKING HOOKS
// ============================================================

export function useRecordImpression() {
  return useMutation({
    mutationFn: (data: RecordImpressionRequest) => recordImpression(data),
  });
}

export function useRecordClick() {
  return useMutation({
    mutationFn: (data: RecordClickRequest) => recordClick(data),
  });
}

// ============================================================
// ROTATION HOOKS
// ============================================================

export function useHomepageRotation(section: AdPlacementType) {
  return useQuery({
    queryKey: advertisingKeys.rotationSection(section),
    queryFn: () => getHomepageRotation(section),
    staleTime: 5 * 60 * 1000, // 5 minutes (matches backend cache)
    retry: 2, // 2 retries before silently hiding the section
  });
}

export function useRotationConfig(section: AdPlacementType) {
  return useQuery({
    queryKey: advertisingKeys.rotationConfig(section),
    queryFn: () => getRotationConfig(section),
    staleTime: 10 * 60 * 1000,
  });
}

export function useUpdateRotationConfig() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: Partial<RotationConfig> & { section: AdPlacementType }) =>
      updateRotationConfig(data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({
        queryKey: advertisingKeys.rotationConfig(variables.section),
      });
      queryClient.invalidateQueries({
        queryKey: advertisingKeys.rotationSection(variables.section),
      });
    },
  });
}

export function useRefreshRotation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (section?: AdPlacementType) => refreshRotation(section),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: advertisingKeys.rotation() });
    },
  });
}

// ============================================================
// HOMEPAGE CONFIG HOOKS
// ============================================================

export function useCategories(includeHidden: boolean = false) {
  return useQuery({
    queryKey: advertisingKeys.categories(includeHidden),
    queryFn: () => getCategories(includeHidden),
    staleTime: 24 * 60 * 60 * 1000, // 24 hours (rarely changes)
  });
}

export function useUpdateCategory() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateCategoryRequest) => updateCategory(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: advertisingKeys.homepage() });
    },
  });
}

export function useBrands(includeHidden: boolean = false) {
  return useQuery({
    queryKey: advertisingKeys.brands(includeHidden),
    queryFn: () => getBrands(includeHidden),
    staleTime: 24 * 60 * 60 * 1000, // 24 hours
  });
}

export function useUpdateBrand() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateBrandRequest) => updateBrand(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: advertisingKeys.homepage() });
    },
  });
}

// ============================================================
// REPORT HOOKS
// ============================================================

export function useCampaignReport(campaignId: string, daysBack: number = 30) {
  return useQuery({
    queryKey: advertisingKeys.campaignReport(campaignId, daysBack),
    queryFn: () => getCampaignReport(campaignId, daysBack),
    enabled: !!campaignId,
    staleTime: 5 * 60 * 1000,
  });
}

export function usePlatformReport(daysBack: number = 30) {
  return useQuery({
    queryKey: advertisingKeys.platformReport(daysBack),
    queryFn: () => getPlatformReport(daysBack),
    staleTime: 5 * 60 * 1000,
  });
}

export function useOwnerReport(
  ownerId: string,
  ownerType: string = 'Individual',
  daysBack: number = 30
) {
  return useQuery({
    queryKey: advertisingKeys.ownerReport(ownerId, ownerType, daysBack),
    queryFn: () => getOwnerReport(ownerId, ownerType, daysBack),
    enabled: !!ownerId,
    staleTime: 5 * 60 * 1000,
  });
}

/**
 * Fetch the enhanced advertiser report (with CPC, CPM, CPL, budget utilization,
 * period comparison, and per-campaign breakdown) from the BFF route.
 */
export function useAdvertiserReport(ownerId: string, period: '7d' | '30d' | '90d' = '7d') {
  return useQuery<AdvertiserReport>({
    queryKey: advertisingKeys.advertiserReport(ownerId, period),
    queryFn: async () => {
      const res = await fetch(
        `/api/advertising/advertiser-report?ownerId=${ownerId}&period=${period}`
      );
      const data = await res.json();
      if (!data.success) throw new Error(data.error ?? 'Error loading report');
      return data.data as AdvertiserReport;
    },
    enabled: !!ownerId,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

// ============================================================
// PRICING HOOKS
// ============================================================

export function usePricingEstimate(placementType: AdPlacementType) {
  return useQuery({
    queryKey: advertisingKeys.pricing(placementType),
    queryFn: () => getPricingEstimate(placementType),
    staleTime: 60 * 60 * 1000, // 1 hour (pricing rarely changes)
  });
}
