'use client';

import { useCallback, useMemo } from 'react';
import { useDropzone, type FileRejection } from 'react-dropzone';
import { Upload, Camera, Loader2 } from 'lucide-react';
import { toast } from 'sonner';

// ============================================================
// TYPES
// ============================================================

interface PhotoDropzoneProps {
  onFilesAccepted: (files: File[]) => void;
  currentCount: number;
  maxPhotos: number;
  minPhotos: number;
  isUploading?: boolean;
  className?: string;
}

// ============================================================
// CONSTANTS
// ============================================================

const MAX_FILE_SIZE = 15 * 1024 * 1024; // 15MB
const ACCEPTED_TYPES = {
  'image/jpeg': ['.jpg', '.jpeg'],
  'image/png': ['.png'],
  'image/webp': ['.webp'],
};

// ============================================================
// COMPONENT
// ============================================================

export function PhotoDropzone({
  onFilesAccepted,
  currentCount,
  maxPhotos,
  minPhotos,
  isUploading = false,
  className = '',
}: PhotoDropzoneProps) {
  const remaining = maxPhotos - currentCount;
  const photosNeeded = Math.max(0, minPhotos - currentCount);

  const onDrop = useCallback(
    (acceptedFiles: File[], rejections: FileRejection[]) => {
      // Handle rejections
      for (const rejection of rejections) {
        const errors = rejection.errors.map(e => {
          switch (e.code) {
            case 'file-too-large':
              return 'Archivo demasiado grande (máximo 15MB)';
            case 'file-invalid-type':
              return 'Formato no soportado. Usa JPG, PNG o WebP';
            default:
              return e.message;
          }
        });
        toast.error(`${rejection.file.name}: ${errors.join(', ')}`);
      }

      if (acceptedFiles.length === 0) return;

      // Limit to remaining slots
      const filesToAdd = acceptedFiles.slice(0, remaining);
      if (filesToAdd.length < acceptedFiles.length) {
        toast.warning(
          `Solo se agregaron ${filesToAdd.length} de ${acceptedFiles.length} fotos (máximo ${maxPhotos})`
        );
      }

      onFilesAccepted(filesToAdd);
    },
    [remaining, maxPhotos, onFilesAccepted]
  );

  const { getRootProps, getInputProps, isDragActive, isDragReject } = useDropzone({
    onDrop,
    accept: ACCEPTED_TYPES,
    maxSize: MAX_FILE_SIZE,
    multiple: true,
    disabled: remaining <= 0 || isUploading,
  });

  const isMobile = useMemo(() => {
    if (typeof navigator === 'undefined') return false;
    return /iPhone|iPad|iPod|Android/i.test(navigator.userAgent);
  }, []);

  const borderClass = isDragReject
    ? 'border-red-500 bg-red-50'
    : isDragActive
      ? 'border-emerald-500 bg-emerald-50'
      : 'border-gray-300 hover:border-emerald-400 hover:bg-gray-50';

  const isDisabled = remaining <= 0 || isUploading;

  return (
    <div
      {...getRootProps()}
      className={`cursor-pointer rounded-xl border-2 border-dashed p-8 text-center transition-all ${borderClass} ${isDisabled ? 'cursor-not-allowed opacity-50' : ''} ${isDragActive ? 'scale-[1.02]' : ''} ${className}`}
    >
      <input
        {...getInputProps()}
        // On mobile, allow camera capture
        {...(isMobile ? { capture: 'environment' as const } : {})}
      />

      {isUploading ? (
        <div className="flex flex-col items-center gap-2">
          <Loader2 className="h-10 w-10 animate-spin text-emerald-500" />
          <p className="text-sm text-gray-600">Subiendo fotos...</p>
        </div>
      ) : isDragReject ? (
        <div className="flex flex-col items-center gap-2">
          <div className="flex h-14 w-14 items-center justify-center rounded-full bg-red-100">
            <Upload className="h-6 w-6 text-red-400" />
          </div>
          <p className="text-sm font-medium text-red-600">Tipo de archivo no permitido</p>
          <p className="text-xs text-red-500">Solo JPEG, PNG y WebP</p>
        </div>
      ) : isDragActive ? (
        <div className="flex flex-col items-center gap-2">
          <div className="flex h-14 w-14 animate-pulse items-center justify-center rounded-full bg-emerald-100">
            <Upload className="h-6 w-6 text-emerald-600" />
          </div>
          <p className="text-sm font-medium text-emerald-700">¡Suelta las fotos aquí!</p>
        </div>
      ) : (
        <div className="flex flex-col items-center gap-3">
          <div className="flex h-14 w-14 items-center justify-center rounded-full bg-gray-100">
            <Upload className="h-6 w-6 text-gray-400" />
          </div>
          <div>
            <p className="text-sm font-medium text-gray-700">
              Arrastra tus fotos aquí o{' '}
              <span className="text-emerald-600">haz clic para seleccionar</span>
            </p>
            <p className="mt-1 text-xs text-gray-500">JPEG, PNG, WebP · Máximo 15MB por foto</p>
            <p className="mt-0.5 text-xs text-gray-500">
              {currentCount}/{maxPhotos} fotos ·{' '}
              {photosNeeded > 0 ? `Faltan ${photosNeeded} mínimo` : 'Requisito mínimo completado ✓'}
            </p>
          </div>

          {/* Mobile camera button */}
          {isMobile && (
            <button
              type="button"
              className="mt-1 flex items-center gap-1.5 rounded-lg bg-emerald-50 px-4 py-2 text-sm font-medium text-emerald-700 transition-colors hover:bg-emerald-100"
              onClick={e => e.stopPropagation()}
            >
              <Camera className="h-4 w-4" />
              Tomar foto con cámara
            </button>
          )}

          {/* Tip */}
          <p className="mt-1 text-xs text-gray-400">
            💡 Las publicaciones con {maxPhotos >= 8 ? '8+' : `${maxPhotos}`} fotos reciben 3 veces más contactos
          </p>
        </div>
      )}
    </div>
  );
}
