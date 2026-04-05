'use client';

import { useState, useCallback, useRef, useEffect } from 'react';
import { toast } from 'sonner';
import { Camera, RotateCcw, Sparkles } from 'lucide-react';
import { PhotoDropzone } from './photo-dropzone';
import { PhotoGrid } from './photo-grid';
import { type PhotoItem, type PhotoStatus } from './photo-card';
import { PhotoCropModal } from './photo-crop-modal';
import { PhotoLightbox } from './photo-lightbox';
import { PhotoUploadProgress } from './photo-upload-progress';
import { PhotoCategoryGuide, PHOTO_ANGLES } from './photo-category-guide';
import { type CompressionPreset } from './image-compressor';
import {
  UploadQueueManager,
  type UploadResult,
  type UploadQueueCallbacks,
} from './upload-queue-manager';
import { uploadVehicleImage, uploadImage } from '@/services/media';
import { DEALER_PLAN_LIMITS, SELLER_PLAN_LIMITS, DealerPlan, SellerPlan } from '@/lib/plan-config';

// ============================================================
// TYPES
// ============================================================

export interface PhotoUploadManagerProps {
  /** Current list of images from parent */
  initialPhotos?: PhotoItem[];
  /** Called when photos change (add, remove, reorder, status) */
  onPhotosChange: (photos: PhotoItem[]) => void;
  /** Vehicle ID (if editing existing) */
  vehicleId?: string;
  /** Account type affects limits */
  accountType: 'individual' | 'dealer';
  /** Dealer plan for plan-based image limits */
  dealerPlan?: DealerPlan;
  /** Seller plan for plan-based image limits */
  sellerPlan?: SellerPlan;
  /** Show 360° tab */
  show360Tab?: boolean;
  /** Show background removal (dealer only) */
  showBgRemoval?: boolean;
  /** Compression preset */
  compressionPreset?: CompressionPreset;
  className?: string;
}

type TabId = 'photos' | '360';

// ============================================================
// CONSTANTS
// ============================================================

/** Fallback limits when no plan is provided */
const FALLBACK_LIMITS = {
  individual: { min: 3, max: 10 },
  dealer: { min: 5, max: 10 },
};

/** Derive photo limits from plan configuration */
function getPlanPhotoLimits(
  accountType: 'individual' | 'dealer',
  dealerPlan?: DealerPlan,
  sellerPlan?: SellerPlan
): { min: number; max: number } {
  if (accountType === 'dealer') {
    const plan = dealerPlan || DealerPlan.LIBRE;
    const maxImages = DEALER_PLAN_LIMITS[plan]?.maxImages ?? FALLBACK_LIMITS.dealer.max;
    return { min: FALLBACK_LIMITS.dealer.min, max: maxImages };
  } else {
    const plan = sellerPlan || SellerPlan.LIBRE;
    const maxImages = SELLER_PLAN_LIMITS[plan]?.maxImages ?? FALLBACK_LIMITS.individual.max;
    return { min: FALLBACK_LIMITS.individual.min, max: maxImages };
  }
}

// ============================================================
// HELPERS
// ============================================================

function generateId(): string {
  return `photo_${Date.now()}_${Math.random().toString(36).slice(2, 8)}`;
}

function getImageDimensions(file: File): Promise<{ width: number; height: number }> {
  return new Promise(resolve => {
    const img = new Image();
    img.onload = () => {
      resolve({ width: img.naturalWidth, height: img.naturalHeight });
      URL.revokeObjectURL(img.src);
    };
    img.onerror = () => {
      resolve({ width: 0, height: 0 });
      URL.revokeObjectURL(img.src);
    };
    img.src = URL.createObjectURL(file);
  });
}

// ============================================================
// COMPONENT
// ============================================================

export function PhotoUploadManager({
  initialPhotos = [],
  onPhotosChange,
  vehicleId,
  accountType,
  dealerPlan,
  sellerPlan,
  show360Tab = false,
  showBgRemoval = false,
  compressionPreset = 'standard',
  className = '',
}: PhotoUploadManagerProps) {
  // ─── State ────────────────────────────────────────────────
  const [photos, setPhotos] = useState<PhotoItem[]>(initialPhotos);
  const [activeTab, setActiveTab] = useState<TabId>('photos');
  const [isUploading, setIsUploading] = useState(false);
  const [isPaused, setIsPaused] = useState(false);

  // Crop
  const [cropPhotoId, setCropPhotoId] = useState<string | null>(null);

  // Lightbox
  const [lightboxOpen, setLightboxOpen] = useState(false);
  const [lightboxIndex, setLightboxIndex] = useState(0);

  // Upload queue ref
  const queueRef = useRef<UploadQueueManager | null>(null);
  const dropzoneRef = useRef<HTMLDivElement>(null);

  const { min, max } = getPlanPhotoLimits(accountType, dealerPlan, sellerPlan);

  // ─── Upload Queue Setup ───────────────────────────────────
  useEffect(() => {
    // Upload function that the queue manager will call for each file
    const uploadFn = async (
      file: File,
      onProgress: (progress: number) => void
    ): Promise<UploadResult> => {
      if (vehicleId) {
        const result = await uploadVehicleImage(
          { file, vehicleId, imageType: 'exterior', compress: true },
          p => onProgress(p.percentage)
        );
        return {
          mediaId: result.mediaId,
          url: result.originalUrl,
          thumbnailUrl: result.thumbnailUrl,
          width: result.width,
          height: result.height,
          fileSize: result.fileSize,
          contentType: file.type,
        };
      } else {
        const result = await uploadImage(file, 'vehicles', p => onProgress(p.percentage));
        return {
          mediaId: result.publicId,
          url: result.url,
          width: result.width,
          height: result.height,
          fileSize: result.size,
          contentType: result.format,
        };
      }
    };

    // Callbacks
    const callbacks: UploadQueueCallbacks = {
      onFileProgress: (fileId: string, progress: number) => {
        setPhotos(prev => prev.map(p => (p.id === fileId ? { ...p, progress } : p)));
      },
      onFileStatusChange: (fileId: string, status: string) => {
        setPhotos(prev =>
          prev.map(p => (p.id === fileId ? { ...p, status: status as PhotoStatus } : p))
        );
      },
      onFileComplete: (fileId: string, result: UploadResult) => {
        setPhotos(prev =>
          prev.map(p =>
            p.id === fileId
              ? {
                  ...p,
                  status: 'uploaded' as PhotoStatus,
                  progress: 100,
                  mediaId: result.mediaId,
                  thumbnailUrl: result.thumbnailUrl || p.url,
                  compressedSize: result.fileSize,
                }
              : p
          )
        );
      },
      onFileError: (fileId: string, error: string) => {
        setPhotos(prev =>
          prev.map(p =>
            p.id === fileId
              ? {
                  ...p,
                  status: 'error' as PhotoStatus,
                  errorMessage: error,
                  progress: 0,
                }
              : p
          )
        );
      },
      onQueueProgress: (completed: number, total: number) => {
        const status = queueRef.current?.getStatus();
        // BUG S35-B1 FIX: include failed files in "done" check.
        // Previously only fired when completed===total, which meant all-error
        // scenarios (completed=0, total=N) never cleared isUploading.
        const allDone = status
          ? status.total > 0 && status.completed + status.failed >= status.total
          : completed === total && total > 0;
        if (allDone) {
          setIsUploading(false);
          if (status && status.failed > 0) {
            toast.warning(`${status.completed} fotos subidas, ${status.failed} con error`);
          } else {
            toast.success(`${completed} fotos subidas correctamente`);
          }
        }
      },
      onCompressionResult: () => {
        /* tracked via status change */
      },
    };

    queueRef.current = new UploadQueueManager(uploadFn, callbacks, {
      maxConcurrent: 3,
      compressBeforeUpload: true,
    });

    return () => {
      queueRef.current?.cancelAll();
    };
  }, [vehicleId, compressionPreset]);

  // ─── Sync photos to parent ────────────────────────────────
  useEffect(() => {
    onPhotosChange(photos);
  }, [photos, onPhotosChange]);

  // ─── Handlers ─────────────────────────────────────────────

  const handleFilesAccepted = useCallback(
    async (files: File[]) => {
      const newPhotos: PhotoItem[] = [];

      for (const file of files) {
        const id = generateId();
        const url = URL.createObjectURL(file);
        const dims = await getImageDimensions(file);

        newPhotos.push({
          id,
          file,
          url,
          order: photos.length + newPhotos.length,
          isPrimary: photos.length === 0 && newPhotos.length === 0,
          status: 'pending',
          progress: 0,
          originalSize: file.size,
          dimensions: dims.width > 0 ? dims : undefined,
        });
      }

      setPhotos(prev => [...prev, ...newPhotos]);

      // Start uploading
      if (queueRef.current && newPhotos.length > 0) {
        setIsUploading(true);
        const rawFiles = newPhotos.map(p => p.file);
        const queueIds = queueRef.current.addFiles(rawFiles);

        // Map queue IDs back to our photo items so callbacks match
        setPhotos(prev =>
          prev.map(p => {
            const photoIdx = newPhotos.findIndex(np => np.id === p.id);
            if (photoIdx >= 0 && queueIds[photoIdx]) {
              return { ...p, id: queueIds[photoIdx] };
            }
            return p;
          })
        );
      }
    },
    [photos.length]
  );

  const handleReorder = useCallback((reordered: PhotoItem[]) => {
    setPhotos(reordered);
  }, []);

  const handleSetPrimary = useCallback((id: string) => {
    setPhotos(prev =>
      prev.map(p => ({
        ...p,
        isPrimary: p.id === id,
      }))
    );
  }, []);

  const handleRemove = useCallback(
    (id: string) => {
      setPhotos(prev => {
        const filtered = prev.filter(p => p.id !== id);
        // If removed photo was primary, set first as primary
        if (filtered.length > 0 && !filtered.some(p => p.isPrimary)) {
          filtered[0].isPrimary = true;
        }
        // Reindex orders
        return filtered.map((p, i) => ({ ...p, order: i }));
      });

      // Revoke object URL to free memory
      const photo = photos.find(p => p.id === id);
      if (photo?.url?.startsWith('blob:')) {
        URL.revokeObjectURL(photo.url);
      }
    },
    [photos]
  );

  const handleCrop = useCallback((id: string) => {
    setCropPhotoId(id);
  }, []);

  const handleCropConfirm = useCallback(
    (croppedFile: File) => {
      if (!cropPhotoId) return;

      const newUrl = URL.createObjectURL(croppedFile);

      setPhotos(prev =>
        prev.map(p =>
          p.id === cropPhotoId
            ? {
                ...p,
                file: croppedFile,
                url: newUrl,
                status: 'pending' as PhotoStatus,
                progress: 0,
                compressedSize: croppedFile.size,
              }
            : p
        )
      );

      // Re-upload the cropped file
      if (queueRef.current) {
        setIsUploading(true);
        const ids = queueRef.current.addFiles([croppedFile]);
        // Update photo ID to match the new queue entry
        if (ids[0]) {
          setPhotos(prev => prev.map(p => (p.id === cropPhotoId ? { ...p, id: ids[0] } : p)));
        }
      }

      setCropPhotoId(null);
      toast.success('Foto recortada correctamente');
    },
    [cropPhotoId]
  );

  const handleView = useCallback(
    (id: string) => {
      const index = photos.findIndex(p => p.id === id);
      if (index >= 0) {
        setLightboxIndex(index);
        setLightboxOpen(true);
      }
    },
    [photos]
  );

  const handleRetry = useCallback(
    (id: string) => {
      const photo = photos.find(p => p.id === id);
      if (!photo || !queueRef.current) return;

      setPhotos(prev =>
        prev.map(p =>
          p.id === id
            ? { ...p, status: 'pending' as PhotoStatus, progress: 0, errorMessage: undefined }
            : p
        )
      );

      setIsUploading(true);
      queueRef.current.retry(id);
    },
    [photos]
  );

  const handlePause = useCallback(() => {
    queueRef.current?.pause();
    setIsPaused(true);
  }, []);

  const handleResume = useCallback(() => {
    queueRef.current?.resume();
    setIsPaused(false);
  }, []);

  const handleCancel = useCallback(() => {
    queueRef.current?.cancelAll();
    setIsUploading(false);
    setIsPaused(false);
    // Mark all pending/uploading as error
    setPhotos(prev =>
      prev.map(p =>
        p.status === 'pending' || p.status === 'uploading' || p.status === 'compressing'
          ? { ...p, status: 'error' as PhotoStatus, errorMessage: 'Cancelado' }
          : p
      )
    );
  }, []);

  const handleAddMore = useCallback(() => {
    dropzoneRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, []);

  // ─── Derived state ───────────────────────────────────────
  const uploadedAngles = photos
    .filter(p => p.alt && PHOTO_ANGLES.some(a => a.id === p.alt))
    .map(p => p.alt!);

  const cropPhoto = photos.find(p => p.id === cropPhotoId);

  const photosCount = photos.length;
  const hasEnoughPhotos = photosCount >= min;

  // ─── Render ───────────────────────────────────────────────
  return (
    <div className={`space-y-4 ${className}`}>
      {/* Tab selector (only if 360° enabled) */}
      {show360Tab && (
        <div className="flex gap-1 rounded-lg bg-gray-100 p-1">
          <button
            type="button"
            onClick={() => setActiveTab('photos')}
            className={`flex-1 rounded-md px-4 py-2 text-sm font-medium transition-colors ${
              activeTab === 'photos'
                ? 'bg-white text-gray-900 shadow-sm'
                : 'text-gray-500 hover:text-gray-700'
            }`}
          >
            <Camera className="mr-1.5 inline h-4 w-4" />
            Fotos Estándar
          </button>
          <button
            type="button"
            onClick={() => setActiveTab('360')}
            className={`flex-1 rounded-md px-4 py-2 text-sm font-medium transition-colors ${
              activeTab === '360'
                ? 'bg-white text-gray-900 shadow-sm'
                : 'text-gray-500 hover:text-gray-700'
            }`}
          >
            <RotateCcw className="mr-1.5 inline h-4 w-4" />
            Vista 360°
          </button>
        </div>
      )}

      {/* Standard Photos Tab */}
      {activeTab === 'photos' && (
        <div className="space-y-4">
          {/* Header info */}
          <div className="flex items-center justify-between">
            <div>
              <h3 className="text-base font-semibold text-gray-900">Fotos del vehículo</h3>
              <p className="text-sm text-gray-500">
                Mínimo {min}, máximo {max} fotos ·{' '}
                {photosCount > 0 ? `${photosCount} seleccionadas` : 'Ninguna seleccionada'}
              </p>
            </div>
            {!hasEnoughPhotos && photosCount > 0 && (
              <span className="rounded-full bg-amber-50 px-2.5 py-1 text-xs font-medium text-amber-600">
                Faltan {min - photosCount} fotos
              </span>
            )}
            {hasEnoughPhotos && (
              <span className="rounded-full bg-emerald-50 px-2.5 py-1 text-xs font-medium text-emerald-600">
                ✓ Mínimo alcanzado
              </span>
            )}
          </div>

          {/* Category guide (compact when photos exist) */}
          <PhotoCategoryGuide uploadedAngles={uploadedAngles} compact={photosCount > 0} />

          {/* Dropzone */}
          <div ref={dropzoneRef}>
            <PhotoDropzone
              onFilesAccepted={handleFilesAccepted}
              currentCount={photosCount}
              maxPhotos={max}
              minPhotos={min}
              isUploading={isUploading}
            />
          </div>

          {/* Photo grid */}
          {photosCount > 0 && (
            <PhotoGrid
              photos={photos}
              onReorder={handleReorder}
              onSetPrimary={handleSetPrimary}
              onRemove={handleRemove}
              onCrop={handleCrop}
              onView={handleView}
              onRetry={handleRetry}
              onAddMore={handleAddMore}
              maxPhotos={max}
            />
          )}

          {/* Background removal CTA (dealer only) */}
          {showBgRemoval && photosCount > 0 && (
            <div className="flex items-center gap-3 rounded-xl border border-purple-200 bg-purple-50 px-4 py-3">
              <Sparkles className="h-5 w-5 text-purple-500" />
              <div className="flex-1">
                <p className="text-sm font-medium text-purple-900">
                  Eliminar fondo automáticamente
                </p>
                <p className="text-xs text-purple-600">
                  Disponible para cuentas dealer. Mejora la apariencia de tus fotos con fondo blanco
                  profesional.
                </p>
              </div>
              <button
                type="button"
                className="rounded-lg bg-purple-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-purple-700"
              >
                Aplicar
              </button>
            </div>
          )}

          {/* Upload progress (sticky bottom) */}
          <PhotoUploadProgress
            photos={photos}
            isUploading={isUploading}
            isPaused={isPaused}
            onPause={handlePause}
            onResume={handleResume}
            onCancel={handleCancel}
          />
        </div>
      )}

      {/* 360° Tab (placeholder - components created separately) */}
      {activeTab === '360' && (
        <div className="flex flex-col items-center justify-center rounded-xl border-2 border-dashed border-gray-300 p-12 text-center">
          <RotateCcw className="h-12 w-12 text-gray-300" />
          <h4 className="mt-4 text-lg font-semibold text-gray-700">Vista 360° del vehículo</h4>
          <p className="mt-1 text-sm text-gray-500">
            Sube un video o múltiples fotos para crear una experiencia interactiva 360°
          </p>
          <p className="mt-4 text-xs text-gray-400">Componente de captura 360° se renderiza aquí</p>
        </div>
      )}

      {/* Crop Modal */}
      {cropPhoto && (
        <PhotoCropModal
          isOpen={!!cropPhotoId}
          imageUrl={cropPhoto.url}
          onClose={() => setCropPhotoId(null)}
          onConfirm={handleCropConfirm}
          fileName={cropPhoto.file.name}
        />
      )}

      {/* Lightbox */}
      <PhotoLightbox
        photos={photos}
        isOpen={lightboxOpen}
        initialIndex={lightboxIndex}
        onClose={() => setLightboxOpen(false)}
      />
    </div>
  );
}
