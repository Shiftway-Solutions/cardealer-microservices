/**
 * Orphan Image Cleanup Service
 * API client for admin orphan image management
 * Connects via API Gateway → MediaService /api/media/orphan-cleanup/*
 */

import { apiClient } from '@/lib/api-client';

// ============================================================
// TYPES
// ============================================================

export interface OrphanFileSample {
  storageKey: string;
  sizeBytes: number;
  lastModified: string;
  cdnUrl: string;
  listingId: string;
}

export interface OrphanReport {
  generatedAt: string;
  bucketName: string;
  totalS3Objects: number;
  totalDbKeys: number;
  orphanCount: number;
  totalSizeBytes: number;
  totalSizeGb: number;
  scanDurationSeconds: number;
  sampleOrphans: OrphanFileSample[];
}

export interface OrphanCleanupResult {
  completedAt: string;
  deletedCount: number;
  failedCount: number;
  freedBytes: number;
  freedGb: number;
  durationSeconds: number;
}

export interface OrphanCleanupStatus {
  hasPendingReport: boolean;
  pendingReport: OrphanReport | null;
  lastResult: OrphanCleanupResult | null;
}

export interface ApproveResponse {
  message: string;
  orphanCount: number;
  approvedAt: string;
}

export interface DismissResponse {
  message: string;
}

// ============================================================
// API FUNCTIONS
// ============================================================

/**
 * Fetches the current orphan cleanup status: pending report, last result, or idle.
 */
export async function getOrphanCleanupStatus(): Promise<OrphanCleanupStatus> {
  const response = await apiClient.get<OrphanCleanupStatus>(
    '/api/media/orphan-cleanup/status'
  );
  return response.data;
}

/**
 * Approves the pending orphan cleanup. Deletion starts within 30s.
 */
export async function approveOrphanCleanup(): Promise<ApproveResponse> {
  const response = await apiClient.post<ApproveResponse>(
    '/api/media/orphan-cleanup/approve'
  );
  return response.data;
}

/**
 * Dismisses the pending report without executing deletion.
 */
export async function dismissOrphanReport(): Promise<DismissResponse> {
  const response = await apiClient.post<DismissResponse>(
    '/api/media/orphan-cleanup/dismiss'
  );
  return response.data;
}
