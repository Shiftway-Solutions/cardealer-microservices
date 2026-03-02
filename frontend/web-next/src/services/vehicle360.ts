/**
 * Vehicle 360° Service — API client for Video360Service.
 * Handles video upload, processing, and viewer data for 360° vehicle views.
 * Pipeline: Video → Local FFmpeg (frame extraction) → remove.bg (bg removal) → S3.
 */

import { apiClient } from '@/lib/api-client';

// ============================================================
// TYPES
// ============================================================

/**
 * Backend ProcessingStatus enum values (serialized as strings via JsonStringEnumConverter).
 */
export type Vehicle360Status =
  | 'Pending'
  | 'Uploading'
  | 'Processing'
  | 'Downloading'
  | 'Completed'
  | 'Failed'
  | 'Cancelled'
  | 'Retrying';

export interface ProcessingJob {
  jobId: string;
  vehicleId?: string;
  /** Status as string (JsonStringEnumConverter). */
  status: Vehicle360Status;
  frameCount?: number;
  /** Extracted frame image URLs (mapped from backend frames[].imageUrl). */
  frameUrls?: string[];
  createdAt: string;
  completedAt?: string;
  errorMessage?: string;
}

/** Raw backend Video360JobResponse before mapping */
interface BackendJobResponse {
  jobId: string;
  vehicleId?: string;
  status: Vehicle360Status;
  totalFrames: number;
  frames: { imageUrl: string; thumbnailUrl?: string; index: number }[];
  errorMessage?: string;
  createdAt: string;
  completedAt?: string;
}

function mapJobResponse(raw: BackendJobResponse): ProcessingJob {
  return {
    jobId: raw.jobId,
    vehicleId: raw.vehicleId,
    status: raw.status,
    frameCount: raw.totalFrames,
    frameUrls: raw.frames?.map(f => f.imageUrl).filter(Boolean),
    createdAt: raw.createdAt,
    completedAt: raw.completedAt,
    errorMessage: raw.errorMessage,
  };
}

export interface ProcessingOptions {
  frameCount?: number;
  width?: number;
  height?: number;
  format?: string;
  removeBackground?: boolean;
  autoCorrectExposure?: boolean;
  backgroundColor?: string;
}

export interface ViewerData {
  id: string;
  type: 'photos' | 'video';
  status: string;
  frameCount: number;
  frameUrls: string[];
  thumbnailUrl?: string;
  config: ViewerConfig;
}

export interface ViewerConfig {
  initialFrame: number;
  autoRotate: boolean;
  autoRotateSpeed: number;
  allowZoom: boolean;
  maxZoom: number;
  showControls: boolean;
  invertDrag: boolean;
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// ============================================================
// API FUNCTIONS
// ============================================================

/**
 * Upload a video for 360° processing via multipart/form-data.
 * vehicleId, frameCount, imageFormat and quality are passed as query params.
 */
export async function uploadVideo(
  file: File,
  vehicleId: string,
  onProgress?: (progress: number) => void,
  frameCount = 36,
  imageFormat: 'Jpeg' | 'Png' | 'WebP' = 'Jpeg',
  quality: 'Low' | 'Medium' | 'High' | 'Ultra' = 'High'
): Promise<ProcessingJob> {
  const formData = new FormData();
  formData.append('file', file);

  const params = new URLSearchParams({
    vehicleId,
    frameCount: String(frameCount),
    imageFormat,
    quality,
  });

  const response = await apiClient.post<BackendJobResponse>(
    `/api/vehicle360/jobs/upload?${params.toString()}`,
    formData,
    {
      headers: { 'Content-Type': 'multipart/form-data' },
      timeout: 300000, // 5 min timeout for video upload + processing
      onUploadProgress: e => {
        if (onProgress && e.total) {
          onProgress(Math.round((e.loaded * 100) / e.total));
        }
      },
    }
  );
  return mapJobResponse(response.data);
}

/**
 * Process a video already uploaded to S3.
 */
export async function processVideo(
  storageUrl: string,
  vehicleId: string,
  options?: ProcessingOptions
): Promise<ProcessingJob> {
  const response = await apiClient.post<ProcessingJob>('/api/vehicle360/process', {
    storageUrl,
    vehicleId,
    ...options,
  });
  return response.data;
}

/**
 * Get the status / result of a processing job (same endpoint for status polling and final result).
 */
export async function getJobStatus(jobId: string): Promise<ProcessingJob> {
  const response = await apiClient.get<BackendJobResponse>(`/api/vehicle360/jobs/${jobId}/status`);
  return mapJobResponse(response.data);
}

/**
 * Get the full result of a completed processing job.
 */
export async function getJobResult(jobId: string): Promise<ProcessingJob> {
  const response = await apiClient.get<BackendJobResponse>(`/api/vehicle360/jobs/${jobId}/result`);
  return mapJobResponse(response.data);
}

/**
 * Retry a failed processing job (max 3 retries).
 */
export async function retryJob(jobId: string): Promise<ProcessingJob> {
  const response = await apiClient.post<ProcessingJob>(`/api/vehicle360/jobs/${jobId}/retry`);
  return response.data;
}

/**
 * Cancel an in-progress processing job.
 */
export async function cancelJob(jobId: string): Promise<void> {
  await apiClient.post(`/api/vehicle360/jobs/${jobId}/cancel`);
}

/**
 * Get viewer data for a vehicle's 360° view (used by the viewer component).
 */
export async function getViewerData(vehicleId: string): Promise<ViewerData> {
  const response = await apiClient.get<ViewerData>(`/api/vehicle360/viewer/${vehicleId}`);
  return response.data;
}

/**
 * Get processing job history for a vehicle.
 */
export async function getVehicleJobs(vehicleId: string): Promise<ProcessingJob[]> {
  const response = await apiClient.get<ProcessingJob[]>(
    `/api/vehicle360/vehicles/${vehicleId}/jobs`
  );
  return response.data;
}

/**
 * Get user's processing jobs (paginated).
 */
export async function getUserJobs(
  userId: string,
  page = 1,
  pageSize = 10
): Promise<PaginatedResult<ProcessingJob>> {
  const response = await apiClient.get<PaginatedResult<ProcessingJob>>(
    `/api/vehicle360/user/${userId}/jobs`,
    { params: { page, pageSize } }
  );
  return response.data;
}

// ============================================================
// SERVICE OBJECT
// ============================================================

export const vehicle360Service = {
  uploadVideo,
  processVideo,
  getJobStatus,
  getJobResult,
  retryJob,
  cancelJob,
  getViewerData,
  getVehicleJobs,
  getUserJobs,
};

export default vehicle360Service;
