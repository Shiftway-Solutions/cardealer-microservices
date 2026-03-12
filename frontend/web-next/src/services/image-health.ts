/**
 * Image Health Dashboard Service
 * API client for admin image health monitoring
 * Connects via API Gateway → MediaService /api/media/image-health/*
 */

import { apiClient } from '@/lib/api-client';

// ============================================================
// TYPES
// ============================================================

export interface ImageHealthSummary {
  totalActiveImages: number;
  brokenImages: number;
  healthyImages: number;
  healthPercentage: number;
  /** "green" (≥99%), "yellow" (95-99%), "red" (<95%) */
  healthStatus: 'green' | 'yellow' | 'red';
  lastScanTime: string | null;
  totalStorageBytes: number;
  totalStorageGb: number;
  estimatedMonthlyCostUsd: number;
}

export interface BrokenListing {
  ownerId: string;
  dealerId: string;
  totalImages: number;
  brokenCount: number;
  brokenPercentage: number;
  lastDetectedAt: string | null;
}

export interface ImageHealthDashboardData {
  summary: ImageHealthSummary;
  topBrokenListings: BrokenListing[];
}

export interface FlagListingRequest {
  ownerId: string;
  dealerId: string;
  reason?: string;
}

export interface FlagListingResponse {
  message: string;
  ownerId: string;
  flaggedAt: string;
}

// ============================================================
// API FUNCTIONS
// ============================================================

/**
 * Fetches the complete image health dashboard data (summary + top 20 broken listings).
 */
export async function getImageHealthDashboard(): Promise<ImageHealthDashboardData> {
  const response = await apiClient.get<ImageHealthDashboardData>(
    '/api/media/image-health/dashboard'
  );
  return response.data;
}

/**
 * Triggers a manual re-verification of all image URLs.
 * The job starts within 30 seconds of the trigger.
 */
export async function triggerImageHealthScan(): Promise<{ message: string }> {
  const response = await apiClient.post<{ message: string }>(
    '/api/media/image-health/trigger-scan'
  );
  return response.data;
}

/**
 * Flags a listing as "requires dealer attention" with optional reason.
 */
export async function flagListingForAttention(
  request: FlagListingRequest
): Promise<FlagListingResponse> {
  const response = await apiClient.post<FlagListingResponse>(
    '/api/media/image-health/flag-listing',
    request
  );
  return response.data;
}
