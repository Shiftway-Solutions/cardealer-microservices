/**
 * React Query hooks for Vehicle 360° operations.
 * Provides mutations for uploads/processing and queries with polling for status tracking.
 */

import { useMutation, useQuery } from '@tanstack/react-query';
import {
  uploadVideo,
  processVideo,
  getJobStatus,
  getJobResult,
  retryJob,
  cancelJob,
  getViewerData,
  getVehicleJobs,
  type ProcessingJob,
  type ProcessingOptions,
  type ViewerData,
} from '@/services/vehicle360';

/**
 * Mutation: Upload a video for 360° processing.
 */
export function useUpload360Video() {
  return useMutation<
    ProcessingJob,
    Error,
    {
      file: File;
      vehicleId: string;
      onProgress?: (p: number) => void;
      frameCount?: number;
      imageFormat?: 'Jpeg' | 'Png' | 'WebP';
      quality?: 'Low' | 'Medium' | 'High' | 'Ultra';
    }
  >({
    mutationFn: ({ file, vehicleId, onProgress, frameCount, imageFormat, quality }) =>
      uploadVideo(file, vehicleId, onProgress, frameCount, imageFormat, quality),
  });
}

/**
 * Mutation: Process a video already in S3.
 */
export function useProcess360Video() {
  return useMutation<
    ProcessingJob,
    Error,
    { storageUrl: string; vehicleId: string; options?: ProcessingOptions }
  >({
    mutationFn: ({ storageUrl, vehicleId, options }) =>
      processVideo(storageUrl, vehicleId, options),
  });
}

/**
 * Query: Poll for 360° processing job status.
 * Stops polling when status is 'Completed', 'Failed', or 'Cancelled'.
 */
export function use360JobStatus(jobId: string | null) {
  return useQuery<ProcessingJob>({
    queryKey: ['360-job-status', jobId],
    queryFn: () => getJobStatus(jobId!),
    enabled: !!jobId,
    refetchInterval: query => {
      const status = query.state.data?.status;
      if (status === 'Completed' || status === 'Failed' || status === 'Cancelled') {
        return false;
      }
      return 5000; // Poll every 5 seconds
    },
  });
}

/**
 * Query: Get the full result of a completed 360° job.
 */
export function use360JobResult(jobId: string | null, isCompleted: boolean) {
  return useQuery<ProcessingJob>({
    queryKey: ['360-job-result', jobId],
    queryFn: () => getJobResult(jobId!),
    enabled: !!jobId && isCompleted,
  });
}

/**
 * Query: Get viewer data for rendering the 360° viewer component.
 */
export function use360ViewerData(vehicleId: string | null) {
  return useQuery<ViewerData>({
    queryKey: ['360-viewer-data', vehicleId],
    queryFn: () => getViewerData(vehicleId!),
    enabled: !!vehicleId,
    staleTime: 5 * 60 * 1000, // Cache for 5 minutes
  });
}

/**
 * Query: Get 360° processing job history for a vehicle.
 */
export function use360VehicleJobs(vehicleId: string | null) {
  return useQuery<ProcessingJob[]>({
    queryKey: ['360-vehicle-jobs', vehicleId],
    queryFn: () => getVehicleJobs(vehicleId!),
    enabled: !!vehicleId,
  });
}

/**
 * Mutation: Retry a failed 360° job.
 */
export function useRetry360Job() {
  return useMutation<ProcessingJob, Error, string>({
    mutationFn: (jobId: string) => retryJob(jobId),
  });
}

/**
 * Mutation: Cancel a 360° processing job.
 */
export function useCancel360Job() {
  return useMutation<void, Error, string>({
    mutationFn: (jobId: string) => cancelJob(jobId),
  });
}

// Re-exports
export type { ProcessingJob, ProcessingOptions, ViewerData };
