/**
 * Upload Queue Manager — Manages parallel uploads with retry, progress tracking,
 * and connection speed adaptation.
 */

import { compressImage, shouldCompress, type CompressionResult } from './image-compressor';

// ============================================================
// TYPES
// ============================================================

export type FileUploadStatus =
  | 'pending'
  | 'compressing'
  | 'uploading'
  | 'uploaded'
  | 'processing'
  | 'error';

export interface QueuedFile {
  id: string;
  file: File;
  status: FileUploadStatus;
  progress: number;
  retryCount: number;
  error?: string;
  result?: UploadResult;
  compressionResult?: CompressionResult;
  previewUrl: string;
}

export interface UploadResult {
  mediaId: string;
  url: string;
  thumbnailUrl?: string;
  width: number;
  height: number;
  fileSize: number;
  contentType: string;
}

export interface UploadQueueCallbacks {
  onFileProgress: (fileId: string, progress: number) => void;
  onFileStatusChange: (fileId: string, status: FileUploadStatus, error?: string) => void;
  onFileComplete: (fileId: string, result: UploadResult) => void;
  onFileError: (fileId: string, error: string, retryCount: number) => void;
  onQueueProgress: (completed: number, total: number) => void;
  onCompressionResult: (fileId: string, result: CompressionResult) => void;
}

export interface QueueStatus {
  total: number;
  pending: number;
  uploading: number;
  completed: number;
  failed: number;
  progress: number; // 0-100 overall
}

type UploadFunction = (file: File, onProgress: (progress: number) => void) => Promise<UploadResult>;

// ============================================================
// UPLOAD QUEUE MANAGER
// ============================================================

export class UploadQueueManager {
  private queue: Map<string, QueuedFile> = new Map();
  private activeUploads = 0;
  private maxConcurrent = 3;
  private retryAttempts = 3;
  private retryDelayMs = 1000;
  private compressBeforeUpload = true;
  private isPaused = false;
  private callbacks: UploadQueueCallbacks;
  private uploadFn: UploadFunction;
  private nextId = 0;

  constructor(
    uploadFn: UploadFunction,
    callbacks: UploadQueueCallbacks,
    options?: {
      maxConcurrent?: number;
      retryAttempts?: number;
      compressBeforeUpload?: boolean;
    }
  ) {
    this.uploadFn = uploadFn;
    this.callbacks = callbacks;
    this.maxConcurrent = options?.maxConcurrent ?? 3;
    this.retryAttempts = options?.retryAttempts ?? 3;
    this.compressBeforeUpload = options?.compressBeforeUpload ?? true;
  }

  /**
   * Add files to the upload queue. Returns generated IDs.
   */
  addFiles(files: File[]): string[] {
    const ids: string[] = [];

    for (const file of files) {
      const id = `upload_${Date.now()}_${this.nextId++}`;
      const previewUrl = URL.createObjectURL(file);

      const queuedFile: QueuedFile = {
        id,
        file,
        status: 'pending',
        progress: 0,
        retryCount: 0,
        previewUrl,
      };

      this.queue.set(id, queuedFile);
      ids.push(id);
    }

    // Auto-start processing
    this.processQueue();
    return ids;
  }

  /**
   * Pause all uploads. Active uploads continue but no new ones start.
   */
  pause(): void {
    this.isPaused = true;
  }

  /**
   * Resume processing the queue.
   */
  resume(): void {
    this.isPaused = false;
    this.processQueue();
  }

  /**
   * Cancel a specific file upload.
   */
  cancel(fileId: string): void {
    const qf = this.queue.get(fileId);
    if (qf) {
      URL.revokeObjectURL(qf.previewUrl);
      this.queue.delete(fileId);
      this.emitQueueProgress();
    }
  }

  /**
   * Cancel all uploads and clear the queue.
   */
  cancelAll(): void {
    for (const qf of this.queue.values()) {
      URL.revokeObjectURL(qf.previewUrl);
    }
    this.queue.clear();
    this.activeUploads = 0;
  }

  /**
   * Retry a failed upload.
   */
  retry(fileId: string): void {
    const qf = this.queue.get(fileId);
    if (qf && qf.status === 'error') {
      qf.status = 'pending';
      qf.progress = 0;
      qf.error = undefined;
      qf.retryCount = 0;
      this.callbacks.onFileStatusChange(fileId, 'pending');
      this.processQueue();
    }
  }

  /**
   * Get current queue status.
   */
  getStatus(): QueueStatus {
    let pending = 0,
      uploading = 0,
      completed = 0,
      failed = 0;
    for (const qf of this.queue.values()) {
      switch (qf.status) {
        case 'pending':
        case 'compressing':
          pending++;
          break;
        case 'uploading':
          uploading++;
          break;
        case 'uploaded':
        case 'processing':
          completed++;
          break;
        case 'error':
          failed++;
          break;
      }
    }
    const total = this.queue.size;
    const progress = total > 0 ? Math.round((completed / total) * 100) : 0;
    return { total, pending, uploading, completed, failed, progress };
  }

  /**
   * Get all queued files as an array.
   */
  getFiles(): QueuedFile[] {
    return Array.from(this.queue.values());
  }

  /**
   * Get a specific file's status.
   */
  getFileStatus(fileId: string): QueuedFile | undefined {
    return this.queue.get(fileId);
  }

  /**
   * Adjust concurrency based on detected connection speed.
   */
  adjustConcurrency(speed: 'slow' | 'medium' | 'fast'): void {
    switch (speed) {
      case 'slow':
        this.maxConcurrent = 1;
        break;
      case 'medium':
        this.maxConcurrent = 2;
        break;
      case 'fast':
        this.maxConcurrent = 3;
        break;
    }
  }

  /**
   * Detect connection speed using Network Information API or fallback.
   */
  detectConnectionSpeed(): 'slow' | 'medium' | 'fast' {
    if (typeof navigator !== 'undefined' && 'connection' in navigator) {
      const conn = (navigator as NavigatorWithConnection).connection;
      if (conn) {
        const effectiveType = conn.effectiveType;
        if (effectiveType === 'slow-2g' || effectiveType === '2g') return 'slow';
        if (effectiveType === '3g') return 'medium';
        return 'fast';
      }
    }
    return 'medium'; // Default assumption
  }

  /**
   * Clean up all preview URLs when the manager is no longer needed.
   */
  destroy(): void {
    for (const qf of this.queue.values()) {
      URL.revokeObjectURL(qf.previewUrl);
    }
    this.queue.clear();
  }

  // ────────────────────────────────────────────
  // Private methods
  // ────────────────────────────────────────────

  private async processQueue(): Promise<void> {
    if (this.isPaused) return;

    // Find pending files
    const pendingFiles = Array.from(this.queue.values()).filter(qf => qf.status === 'pending');

    // Start uploads up to max concurrent
    while (this.activeUploads < this.maxConcurrent && pendingFiles.length > 0) {
      const qf = pendingFiles.shift()!;
      this.activeUploads++;
      this.processFile(qf).finally(() => {
        this.activeUploads--;
        this.processQueue(); // Process next in queue
      });
    }
  }

  private async processFile(qf: QueuedFile): Promise<void> {
    let fileToUpload = qf.file;

    // Compression step
    if (this.compressBeforeUpload && shouldCompress(qf.file)) {
      qf.status = 'compressing';
      this.callbacks.onFileStatusChange(qf.id, 'compressing');

      try {
        const compressionResult = await compressImage(qf.file, {
          onProgress: p => {
            qf.progress = Math.round(p * 0.3); // Compression is 0-30% of total
            this.callbacks.onFileProgress(qf.id, qf.progress);
          },
        });

        qf.compressionResult = compressionResult;
        this.callbacks.onCompressionResult(qf.id, compressionResult);
        fileToUpload = compressionResult.compressed;
      } catch {
        // Compression failed — use original file
        fileToUpload = qf.file;
      }
    }

    // Upload step
    qf.status = 'uploading';
    this.callbacks.onFileStatusChange(qf.id, 'uploading');

    try {
      const result = await this.uploadFn(fileToUpload, progress => {
        // Upload progress is 30-100% of total (or 0-100% if no compression)
        const base = qf.compressionResult?.wasCompressed ? 30 : 0;
        qf.progress = base + Math.round((progress * (100 - base)) / 100);
        this.callbacks.onFileProgress(qf.id, qf.progress);
      });

      qf.status = 'uploaded';
      qf.progress = 100;
      qf.result = result;
      this.callbacks.onFileStatusChange(qf.id, 'uploaded');
      this.callbacks.onFileComplete(qf.id, result);
      this.emitQueueProgress();
    } catch (error) {
      await this.handleUploadError(qf, fileToUpload, error);
    }
  }

  private async handleUploadError(qf: QueuedFile, file: File, error: unknown): Promise<void> {
    qf.retryCount++;
    const errorMessage = error instanceof Error ? error.message : 'Error de carga desconocido';

    if (qf.retryCount <= this.retryAttempts) {
      // Retry with exponential backoff
      const delay = this.retryDelayMs * Math.pow(2, qf.retryCount - 1);
      this.callbacks.onFileError(
        qf.id,
        `Reintentando (${qf.retryCount}/${this.retryAttempts})...`,
        qf.retryCount
      );

      await new Promise(resolve => setTimeout(resolve, delay));

      // Retry upload
      qf.status = 'uploading';
      this.callbacks.onFileStatusChange(qf.id, 'uploading');

      try {
        const result = await this.uploadFn(file, progress => {
          qf.progress = Math.round(progress);
          this.callbacks.onFileProgress(qf.id, qf.progress);
        });

        qf.status = 'uploaded';
        qf.progress = 100;
        qf.result = result;
        this.callbacks.onFileStatusChange(qf.id, 'uploaded');
        this.callbacks.onFileComplete(qf.id, result);
        this.emitQueueProgress();
      } catch (retryError) {
        await this.handleUploadError(qf, file, retryError);
      }
    } else {
      // Max retries exceeded
      qf.status = 'error';
      qf.error = errorMessage;
      this.callbacks.onFileStatusChange(qf.id, 'error', errorMessage);
      this.callbacks.onFileError(qf.id, errorMessage, qf.retryCount);
      this.emitQueueProgress();
    }
  }

  private emitQueueProgress(): void {
    const status = this.getStatus();
    this.callbacks.onQueueProgress(status.completed, status.total);
  }
}

// ============================================================
// NAVIGATOR TYPE EXTENSION
// ============================================================

interface NavigatorWithConnection {
  connection?: {
    effectiveType: 'slow-2g' | '2g' | '3g' | '4g';
    downlink?: number;
    rtt?: number;
    saveData?: boolean;
  };
}
