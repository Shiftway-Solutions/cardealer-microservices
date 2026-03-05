---
title: "67 - Common Components"
priority: P0
estimated_time: ""
dependencies: []
apis: []
status: complete
last_updated: "2026-01-30"
---

# 📋 67 - Common Components

**Objetivo:** Componentes reutilizables de UI: LocalizedContent, ImageDropZone, ConfirmDialog, ErrorBoundary, LanguageSwitcher.

**Prioridad:** P0 (Crítica)  
**Complejidad:** 🟡 Media  
**Dependencias:** react-i18next, Sentry, S3/MediaService

---

## 🎨 WIREFRAME - COMPONENTES COMUNES

### ImageDropZone

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                                                                         │ │
│  │                                                                         │ │
│  │          ┌──────────────────────────────────────────────┐               │ │
│  │          │                                              │               │ │
│  │          │     📷  Arrastra imágenes aquí              │               │ │
│  │          │         o haz clic para seleccionar         │               │ │
│  │          │                                              │               │ │
│  │          │     Formatos: JPG, PNG, WebP                │               │ │
│  │          │     Máximo: 10MB por imagen                 │               │ │
│  │          │                                              │               │ │
│  │          └──────────────────────────────────────────────┘               │ │
│  │                                                                         │ │
│  │  Imágenes subidas (3):                                                  │ │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐                               │ │
│  │  │ 📷 img1  │  │ 📷 img2  │  │ 📷 img3  │                               │ │
│  │  │ ████████ │  │ ████████ │  │ ████████ │                               │ │
│  │  │ ████████ │  │ ████████ │  │ ████████ │                               │ │
│  │  │    [✕]   │  │    [✕]   │  │    [✕]   │                               │ │
│  │  └──────────┘  └──────────┘  └──────────┘                               │ │
│  │                                                                         │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### ConfirmDialog

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     (Overlay oscuro - fondo difuminado)                     │
│                                                                              │
│          ┌──────────────────────────────────────────────────────┐            │
│          │                                                      │            │
│          │  ⚠️  ¿Estás seguro?                                  │            │
│          │                                                      │            │
│          │  Esta acción no se puede deshacer. El vehículo       │            │
│          │  será eliminado permanentemente del sistema.         │            │
│          │                                                      │            │
│          │              [Cancelar]     [🗑️ Eliminar]            │            │
│          │                              (rojo)                  │            │
│          └──────────────────────────────────────────────────────┘            │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### ErrorBoundary

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                              │
│          ┌──────────────────────────────────────────────────────┐            │
│          │                                                      │            │
│          │              ⚠️  Algo salió mal                      │            │
│          │                                                      │            │
│          │  Lo sentimos, ocurrió un error inesperado.           │            │
│          │  Por favor intenta recargar la página.               │            │
│          │                                                      │            │
│          │  [📋 Copiar detalles del error]                      │            │
│          │                                                      │            │
│          │              [🔄 Reintentar]                         │            │
│          │                                                      │            │
│          └──────────────────────────────────────────────────────┘            │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### LanguageSwitcher

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                              │
│  Navbar:  [...otros elementos...]    🌐 ES ▼                                │
│                                     ┌──────────┐                             │
│                                     │ 🇪🇸 Español│◀── seleccionado           │
│                                     │ 🇺🇸 English│                            │
│                                     └──────────┘                             │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 📋 TABLA DE CONTENIDOS

1. [LocalizedContent](#-localizedcontent)
2. [ImageDropZone](#-imagedropzone)
3. [ConfirmDialog](#-confirmdialog)
4. [ErrorBoundary](#-errorboundary)
5. [LanguageSwitcher](#-languageswitcher)

---

## 🌐 LOCALIZEDCONTENT

**Ubicación:** `src/components/common/LocalizedContent.tsx`

Muestra contenido multilingüe con badge opcional indicando el idioma original.

```typescript
// src/components/common/LocalizedContent.tsx
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { FiGlobe } from 'react-icons/fi';
import { cn } from '@/lib/utils';

export interface MultiLangText {
  es?: string;
  en?: string;
  originalLang?: 'es' | 'en';
}

export interface LocalizedContentProps {
  content: MultiLangText | string;
  showBadge?: boolean;
  badgePosition?: 'inline' | 'top-right';
  lineClamp?: number;
  className?: string;
  as?: 'p' | 'span' | 'div' | 'h1' | 'h2' | 'h3' | 'h4';
}

export function LocalizedContent({
  content,
  showBadge = true,
  badgePosition = 'inline',
  lineClamp,
  className,
  as: Component = 'span',
}: LocalizedContentProps) {
  const { i18n } = useTranslation();
  const currentLang = i18n.language as 'es' | 'en';

  const { text, isTranslated } = useMemo(() => {
    // Simple string
    if (typeof content === 'string') {
      return { text: content, isTranslated: false };
    }

    // MultiLangText
    const localizedText = content[currentLang] || content.es || content.en || '';
    const originalLang = content.originalLang || 'es';
    const isTranslated = originalLang !== currentLang && !!content[currentLang];

    return { text: localizedText, isTranslated };
  }, [content, currentLang]);

  const lineClampClass = lineClamp ? `line-clamp-${lineClamp}` : '';

  // Badge para contenido traducido
  const LanguageBadge = () => {
    if (!showBadge || !isTranslated) return null;

    return (
      <span
        className={cn(
          'inline-flex items-center gap-1 text-xs text-gray-500 bg-gray-100 rounded px-1.5 py-0.5',
          badgePosition === 'inline' && 'ml-2'
        )}
        title="Este contenido ha sido traducido automáticamente"
      >
        <FiGlobe size={10} />
        <span>Traducido</span>
      </span>
    );
  };

  if (badgePosition === 'top-right') {
    return (
      <div className="relative">
        {showBadge && isTranslated && (
          <div className="absolute -top-1 -right-1">
            <LanguageBadge />
          </div>
        )}
        <Component className={cn(lineClampClass, className)}>{text}</Component>
      </div>
    );
  }

  return (
    <Component className={cn('inline', lineClampClass, className)}>
      {text}
      <LanguageBadge />
    </Component>
  );
}
```

### Props

| Prop            | Tipo                                | Default      | Descripción                   |
| --------------- | ----------------------------------- | ------------ | ----------------------------- |
| `content`       | `MultiLangText \| string`           | **required** | Contenido a mostrar           |
| `showBadge`     | `boolean`                           | `true`       | Mostrar badge si es traducido |
| `badgePosition` | `'inline' \| 'top-right'`           | `'inline'`   | Posición del badge            |
| `lineClamp`     | `number`                            | -            | Limitar líneas de texto       |
| `className`     | `string`                            | -            | Clases CSS adicionales        |
| `as`            | `'p' \| 'span' \| 'div' \| 'h1'...` | `'span'`     | Elemento HTML a renderizar    |

---

## 📷 IMAGEDROPZONE

**Ubicación:** `src/components/common/ImageDropZone.tsx`

Componente drag & drop para subir imágenes con progreso y validación.

```typescript
// src/components/common/ImageDropZone.tsx
import { useState, useCallback, useRef } from 'react';
import { FiUploadCloud, FiX, FiImage, FiAlertCircle, FiCheck } from 'react-icons/fi';
import { cn } from '@/lib/utils';
import { useUploadMultipleImages } from '@/hooks/useUploadMultipleImages';
import { useImageValidation } from '@/hooks/useImageValidation';

export interface ImagePreview {
  id: string;
  file?: File;
  url: string;
  name: string;
  size: number;
  progress: number;
  status: 'pending' | 'uploading' | 'success' | 'error';
  error?: string;
}

export interface ImageDropZoneProps {
  maxFiles?: number;
  maxSizeMB?: number;
  folder?: string;
  autoUpload?: boolean;
  acceptedFormats?: string[];
  onImagesChange?: (images: ImagePreview[]) => void;
  onUploadComplete?: (urls: string[]) => void;
  className?: string;
  disabled?: boolean;
  initialImages?: string[];
}

export function ImageDropZone({
  maxFiles = 10,
  maxSizeMB = 5,
  folder = 'vehicles',
  autoUpload = true,
  acceptedFormats = ['image/jpeg', 'image/png', 'image/webp'],
  onImagesChange,
  onUploadComplete,
  className,
  disabled = false,
  initialImages = [],
}: ImageDropZoneProps) {
  const [images, setImages] = useState<ImagePreview[]>(() =>
    initialImages.map((url, i) => ({
      id: `initial-${i}`,
      url,
      name: `Imagen ${i + 1}`,
      size: 0,
      progress: 100,
      status: 'success' as const,
    }))
  );
  const [isDragging, setIsDragging] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  const { validateImage } = useImageValidation({ maxSizeMB, acceptedFormats });
  const { uploadImages } = useUploadMultipleImages({ folder });

  // Handle file drop
  const handleDrop = useCallback(
    async (e: React.DragEvent) => {
      e.preventDefault();
      setIsDragging(false);

      if (disabled) return;

      const files = Array.from(e.dataTransfer.files);
      await processFiles(files);
    },
    [disabled]
  );

  // Handle file select
  const handleFileSelect = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(e.target.files || []);
    await processFiles(files);
    if (inputRef.current) inputRef.current.value = '';
  };

  // Process and validate files
  const processFiles = async (files: File[]) => {
    const remainingSlots = maxFiles - images.length;
    const filesToProcess = files.slice(0, remainingSlots);

    const newPreviews: ImagePreview[] = [];

    for (const file of filesToProcess) {
      const validation = validateImage(file);
      const id = `${Date.now()}-${Math.random()}`;

      if (!validation.valid) {
        newPreviews.push({
          id,
          file,
          url: URL.createObjectURL(file),
          name: file.name,
          size: file.size,
          progress: 0,
          status: 'error',
          error: validation.error,
        });
      } else {
        newPreviews.push({
          id,
          file,
          url: URL.createObjectURL(file),
          name: file.name,
          size: file.size,
          progress: 0,
          status: 'pending',
        });
      }
    }

    const updatedImages = [...images, ...newPreviews];
    setImages(updatedImages);
    onImagesChange?.(updatedImages);

    // Auto upload valid images
    if (autoUpload) {
      const validPreviews = newPreviews.filter((p) => p.status === 'pending');
      if (validPreviews.length > 0) {
        await handleUpload(validPreviews, updatedImages);
      }
    }
  };

  // Upload images to server
  const handleUpload = async (toUpload: ImagePreview[], currentImages: ImagePreview[]) => {
    const filesToUpload = toUpload.map((p) => p.file!);

    try {
      // Update status to uploading
      setImages((prev) =>
        prev.map((img) =>
          toUpload.find((u) => u.id === img.id) ? { ...img, status: 'uploading' as const } : img
        )
      );

      const results = await uploadImages(filesToUpload, {
        onProgress: (progress, index) => {
          setImages((prev) =>
            prev.map((img) =>
              img.id === toUpload[index]?.id ? { ...img, progress } : img
            )
          );
        },
      });

      // Update with success/error
      setImages((prev) =>
        prev.map((img) => {
          const uploadIndex = toUpload.findIndex((u) => u.id === img.id);
          if (uploadIndex === -1) return img;

          const result = results[uploadIndex];
          if (result.success) {
            return {
              ...img,
              url: result.url,
              progress: 100,
              status: 'success' as const,
            };
          }
          return {
            ...img,
            status: 'error' as const,
            error: result.error,
          };
        })
      );

      // Callback with successful URLs
      const successfulUrls = results.filter((r) => r.success).map((r) => r.url);
      if (successfulUrls.length > 0) {
        onUploadComplete?.(successfulUrls);
      }
    } catch (error) {
      console.error('Upload failed:', error);
    }
  };

  // Remove image
  const removeImage = (id: string) => {
    const image = images.find((img) => img.id === id);
    if (image?.url.startsWith('blob:')) {
      URL.revokeObjectURL(image.url);
    }
    const updated = images.filter((img) => img.id !== id);
    setImages(updated);
    onImagesChange?.(updated);
  };

  // Format file size
  const formatSize = (bytes: number): string => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`;
  };

  const canAddMore = images.length < maxFiles;

  return (
    <div className={cn('space-y-4', className)}>
      {/* Drop Zone */}
      {canAddMore && (
        <div
          onDragOver={(e) => {
            e.preventDefault();
            setIsDragging(true);
          }}
          onDragLeave={() => setIsDragging(false)}
          onDrop={handleDrop}
          onClick={() => inputRef.current?.click()}
          className={cn(
            'border-2 border-dashed rounded-xl p-8 text-center cursor-pointer transition-colors',
            isDragging
              ? 'border-primary-500 bg-primary-50'
              : 'border-gray-300 hover:border-primary-400 hover:bg-gray-50',
            disabled && 'opacity-50 cursor-not-allowed'
          )}
        >
          <input
            ref={inputRef}
            type="file"
            accept={acceptedFormats.join(',')}
            multiple
            onChange={handleFileSelect}
            className="hidden"
            disabled={disabled}
          />

          <FiUploadCloud
            size={48}
            className={cn(
              'mx-auto mb-4',
              isDragging ? 'text-primary-500' : 'text-gray-400'
            )}
          />

          <p className="text-lg font-medium text-gray-700 mb-2">
            Arrastra imágenes aquí o haz clic para seleccionar
          </p>
          <p className="text-sm text-gray-500">
            Máximo {maxFiles} imágenes • Hasta {maxSizeMB}MB cada una • JPG, PNG, WebP
          </p>
          <p className="text-sm text-gray-400 mt-2">
            {images.length} de {maxFiles} imágenes
          </p>
        </div>
      )}

      {/* Preview Grid */}
      {images.length > 0 && (
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4">
          {images.map((image, index) => (
            <div
              key={image.id}
              className="relative group rounded-lg overflow-hidden border border-gray-200"
            >
              <img
                src={image.url}
                alt={image.name}
                className="w-full h-32 object-cover"
              />

              {/* Primary badge */}
              {index === 0 && (
                <span className="absolute top-2 left-2 bg-primary-500 text-white text-xs px-2 py-0.5 rounded">
                  Principal
                </span>
              )}

              {/* Status overlay */}
              {image.status === 'uploading' && (
                <div className="absolute inset-0 bg-black/50 flex items-center justify-center">
                  <div className="text-center text-white">
                    <div className="w-10 h-10 border-3 border-white border-t-transparent rounded-full animate-spin mx-auto mb-2" />
                    <span className="text-sm">{image.progress}%</span>
                  </div>
                </div>
              )}

              {image.status === 'error' && (
                <div className="absolute inset-0 bg-red-500/80 flex items-center justify-center">
                  <div className="text-center text-white px-2">
                    <FiAlertCircle size={24} className="mx-auto mb-1" />
                    <span className="text-xs">{image.error || 'Error'}</span>
                  </div>
                </div>
              )}

              {image.status === 'success' && (
                <div className="absolute top-2 right-2 bg-green-500 text-white rounded-full p-1">
                  <FiCheck size={12} />
                </div>
              )}

              {/* Remove button */}
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  removeImage(image.id);
                }}
                className="absolute top-2 right-2 bg-red-500 text-white rounded-full p-1 opacity-0 group-hover:opacity-100 transition-opacity"
                title="Eliminar imagen"
              >
                <FiX size={16} />
              </button>

              {/* File info */}
              <div className="absolute bottom-0 left-0 right-0 bg-gradient-to-t from-black/60 to-transparent p-2">
                <p className="text-white text-xs truncate">{image.name}</p>
                {image.size > 0 && (
                  <p className="text-white/70 text-xs">{formatSize(image.size)}</p>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
```

### Props

| Prop               | Tipo                               | Default                         | Descripción                 |
| ------------------ | ---------------------------------- | ------------------------------- | --------------------------- |
| `maxFiles`         | `number`                           | `10`                            | Máximo de archivos          |
| `maxSizeMB`        | `number`                           | `5`                             | Tamaño máximo por archivo   |
| `folder`           | `string`                           | `'vehicles'`                    | Carpeta de destino en S3    |
| `autoUpload`       | `boolean`                          | `true`                          | Subir automáticamente       |
| `acceptedFormats`  | `string[]`                         | `['image/jpeg','image/png'...]` | Formatos aceptados          |
| `onImagesChange`   | `(images: ImagePreview[]) => void` | -                               | Callback en cambio          |
| `onUploadComplete` | `(urls: string[]) => void`         | -                               | Callback cuando terminan    |
| `initialImages`    | `string[]`                         | `[]`                            | URLs de imágenes existentes |
| `disabled`         | `boolean`                          | `false`                         | Deshabilitar interacción    |

---

## ⚠️ CONFIRMDIALOG

**Ubicación:** `src/components/common/ConfirmDialog.tsx`

Modal de confirmación con variantes de estilo.

```typescript
// src/components/common/ConfirmDialog.tsx
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog';
import Button from '@/components/atoms/Button';
import { FiAlertTriangle, FiAlertCircle, FiCheckCircle, FiInfo } from 'react-icons/fi';
import { cn } from '@/lib/utils';

export type ConfirmDialogVariant = 'danger' | 'warning' | 'info' | 'success';

export interface ConfirmDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  description?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  onConfirm: () => void | Promise<void>;
  onCancel?: () => void;
  variant?: ConfirmDialogVariant;
  isLoading?: boolean;
  infoText?: string;
}

const variantStyles: Record<
  ConfirmDialogVariant,
  {
    icon: React.ElementType;
    iconBg: string;
    iconColor: string;
    confirmButton: string;
  }
> = {
  danger: {
    icon: FiAlertTriangle,
    iconBg: 'bg-red-100',
    iconColor: 'text-red-600',
    confirmButton: 'bg-red-600 hover:bg-red-700 text-white',
  },
  warning: {
    icon: FiAlertCircle,
    iconBg: 'bg-yellow-100',
    iconColor: 'text-yellow-600',
    confirmButton: 'bg-yellow-600 hover:bg-yellow-700 text-white',
  },
  info: {
    icon: FiInfo,
    iconBg: 'bg-blue-100',
    iconColor: 'text-blue-600',
    confirmButton: 'bg-blue-600 hover:bg-blue-700 text-white',
  },
  success: {
    icon: FiCheckCircle,
    iconBg: 'bg-green-100',
    iconColor: 'text-green-600',
    confirmButton: 'bg-green-600 hover:bg-green-700 text-white',
  },
};

export function ConfirmDialog({
  open,
  onOpenChange,
  title,
  description,
  confirmLabel = 'Confirmar',
  cancelLabel = 'Cancelar',
  onConfirm,
  onCancel,
  variant = 'danger',
  isLoading = false,
  infoText,
}: ConfirmDialogProps) {
  const styles = variantStyles[variant];
  const Icon = styles.icon;

  const handleConfirm = async () => {
    await onConfirm();
    onOpenChange(false);
  };

  const handleCancel = () => {
    onCancel?.();
    onOpenChange(false);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <div className="flex items-start gap-4">
            <div
              className={cn(
                'flex-shrink-0 w-12 h-12 rounded-full flex items-center justify-center',
                styles.iconBg
              )}
            >
              <Icon size={24} className={styles.iconColor} />
            </div>
            <div className="flex-1 min-w-0">
              <DialogTitle className="text-lg font-semibold text-gray-900">
                {title}
              </DialogTitle>
              {description && (
                <DialogDescription className="mt-2 text-sm text-gray-600">
                  {description}
                </DialogDescription>
              )}
            </div>
          </div>
        </DialogHeader>

        {infoText && (
          <div className="bg-gray-50 rounded-lg p-3 text-sm text-gray-600 mt-4">
            {infoText}
          </div>
        )}

        <DialogFooter className="mt-6 flex gap-3 sm:justify-end">
          <Button
            type="button"
            variant="outline"
            onClick={handleCancel}
            disabled={isLoading}
          >
            {cancelLabel}
          </Button>
          <Button
            type="button"
            className={styles.confirmButton}
            onClick={handleConfirm}
            disabled={isLoading}
            isLoading={isLoading}
          >
            {confirmLabel}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
```

### Props

| Prop           | Tipo                          | Default       | Descripción                 |
| -------------- | ----------------------------- | ------------- | --------------------------- |
| `open`         | `boolean`                     | **required**  | Estado de visibilidad       |
| `onOpenChange` | `(open: boolean) => void`     | **required**  | Callback cambio visibilidad |
| `title`        | `string`                      | **required**  | Título del modal            |
| `description`  | `string`                      | -             | Descripción/mensaje         |
| `confirmLabel` | `string`                      | `'Confirmar'` | Texto botón confirmar       |
| `cancelLabel`  | `string`                      | `'Cancelar'`  | Texto botón cancelar        |
| `onConfirm`    | `() => void \| Promise<void>` | **required**  | Callback al confirmar       |
| `onCancel`     | `() => void`                  | -             | Callback al cancelar        |
| `variant`      | `ConfirmDialogVariant`        | `'danger'`    | Estilo visual               |
| `isLoading`    | `boolean`                     | `false`       | Estado de carga             |
| `infoText`     | `string`                      | -             | Información adicional       |

---

## 🛡️ ERRORBOUNDARY

**Ubicación:** `src/components/common/ErrorBoundary.tsx`

Captura errores de React y reporta a Sentry.

```typescript
// src/components/common/ErrorBoundary.tsx
import { Component, type ReactNode, type ErrorInfo } from 'react';
import * as Sentry from '@sentry/react';
import { FiAlertOctagon, FiRefreshCw, FiHome } from 'react-icons/fi';
import Button from '@/components/atoms/Button';

interface ErrorBoundaryProps {
  children: ReactNode;
  fallback?: ReactNode;
  onError?: (error: Error, errorInfo: ErrorInfo) => void;
  showDialog?: boolean;
}

interface ErrorBoundaryState {
  hasError: boolean;
  error: Error | null;
  errorId: string | null;
}

// Default fallback UI
function DefaultFallback({
  error,
  errorId,
  onReset,
}: {
  error: Error | null;
  errorId: string | null;
  onReset: () => void;
}) {
  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <div className="max-w-md w-full text-center">
        <div className="w-20 h-20 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-6">
          <FiAlertOctagon size={40} className="text-red-600" />
        </div>

        <h1 className="text-2xl font-bold text-gray-900 mb-4">
          Algo salió mal
        </h1>

        <p className="text-gray-600 mb-6">
          Ocurrió un error inesperado. Nuestro equipo ha sido notificado y estamos trabajando para solucionarlo.
        </p>

        {errorId && (
          <p className="text-sm text-gray-500 mb-6 font-mono bg-gray-100 rounded px-3 py-2">
            Error ID: {errorId}
          </p>
        )}

        {process.env.NODE_ENV === 'development' && error && (
          <details className="text-left mb-6 bg-red-50 rounded-lg p-4">
            <summary className="cursor-pointer text-sm font-medium text-red-800 mb-2">
              Detalles del error (solo en desarrollo)
            </summary>
            <pre className="text-xs text-red-700 overflow-auto max-h-48 mt-2">
              {error.message}
              {'\n\n'}
              {error.stack}
            </pre>
          </details>
        )}

        <div className="flex gap-4 justify-center">
          <Button variant="outline" onClick={onReset} leftIcon={<FiRefreshCw />}>
            Reintentar
          </Button>
          <Button
            onClick={() => (window.location.href = '/')}
            leftIcon={<FiHome />}
          >
            Ir al Inicio
          </Button>
        </div>
      </div>
    </div>
  );
}

export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = {
      hasError: false,
      error: null,
      errorId: null,
    };
  }

  static getDerivedStateFromError(error: Error): Partial<ErrorBoundaryState> {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    // Add breadcrumb
    Sentry.addBreadcrumb({
      category: 'error-boundary',
      message: 'React error boundary caught error',
      level: 'error',
      data: {
        componentStack: errorInfo.componentStack,
      },
    });

    // Capture error and get event ID
    const eventId = Sentry.captureException(error, {
      extra: {
        componentStack: errorInfo.componentStack,
      },
    });

    this.setState({ errorId: eventId });

    // Custom error handler
    this.props.onError?.(error, errorInfo);

    // Show Sentry feedback dialog
    if (this.props.showDialog) {
      Sentry.showReportDialog({ eventId });
    }

    // Log to console in development
    if (process.env.NODE_ENV === 'development') {
      console.error('ErrorBoundary caught an error:', error);
      console.error('Component stack:', errorInfo.componentStack);
    }
  }

  handleReset = () => {
    this.setState({ hasError: false, error: null, errorId: null });
  };

  render() {
    if (this.state.hasError) {
      if (this.props.fallback) {
        return this.props.fallback;
      }

      return (
        <DefaultFallback
          error={this.state.error}
          errorId={this.state.errorId}
          onReset={this.handleReset}
        />
      );
    }

    return this.props.children;
  }
}

// HOC for wrapping components
export function withErrorBoundary<P extends object>(
  WrappedComponent: React.ComponentType<P>,
  fallback?: ReactNode
) {
  return function WithErrorBoundary(props: P) {
    return (
      <ErrorBoundary fallback={fallback}>
        <WrappedComponent {...props} />
      </ErrorBoundary>
    );
  };
}
```

### Props

| Prop         | Tipo                                      | Default      | Descripción                  |
| ------------ | ----------------------------------------- | ------------ | ---------------------------- |
| `children`   | `ReactNode`                               | **required** | Contenido a proteger         |
| `fallback`   | `ReactNode`                               | -            | UI alternativa en error      |
| `onError`    | `(error: Error, info: ErrorInfo) => void` | -            | Callback cuando ocurre error |
| `showDialog` | `boolean`                                 | `false`      | Mostrar dialog de Sentry     |

---

## 🌍 LANGUAGESWITCHER

**Ubicación:** `src/components/common/LanguageSwitcher.tsx`

Toggle para cambiar entre ES/EN.

```typescript
// src/components/common/LanguageSwitcher.tsx
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { FiGlobe, FiCheck, FiChevronDown } from 'react-icons/fi';
import { cn } from '@/lib/utils';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';

type SupportedLanguage = 'es' | 'en';

interface LanguageSwitcherProps {
  variant?: 'dropdown' | 'inline' | 'minimal';
  className?: string;
  showLabel?: boolean;
  showFlag?: boolean;
}

const supportedLanguages: SupportedLanguage[] = ['es', 'en'];

const languageLabels: Record<SupportedLanguage, string> = {
  es: 'Español',
  en: 'English',
};

const languageFlags: Record<SupportedLanguage, string> = {
  es: '🇩🇴', // Dominican Republic
  en: '🇺🇸', // United States
};

export function LanguageSwitcher({
  variant = 'dropdown',
  className,
  showLabel = true,
  showFlag = true,
}: LanguageSwitcherProps) {
  const { i18n } = useTranslation();
  const currentLang = i18n.language as SupportedLanguage;

  const changeLanguage = (lang: SupportedLanguage) => {
    i18n.changeLanguage(lang);
    localStorage.setItem('preferred-language', lang);
    document.documentElement.lang = lang;
  };

  // Dropdown variant
  if (variant === 'dropdown') {
    return (
      <DropdownMenu>
        <DropdownMenuTrigger
          className={cn(
            'flex items-center gap-2 px-3 py-2 rounded-lg border border-gray-200 hover:bg-gray-50 transition-colors text-sm',
            className
          )}
        >
          {showFlag && <span>{languageFlags[currentLang]}</span>}
          {showLabel && <span>{languageLabels[currentLang]}</span>}
          <FiChevronDown size={14} className="text-gray-400" />
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="min-w-[140px]">
          {supportedLanguages.map((lang) => (
            <DropdownMenuItem
              key={lang}
              onClick={() => changeLanguage(lang)}
              className={cn(
                'flex items-center justify-between cursor-pointer',
                lang === currentLang && 'bg-primary-50'
              )}
            >
              <span className="flex items-center gap-2">
                {showFlag && <span>{languageFlags[lang]}</span>}
                {languageLabels[lang]}
              </span>
              {lang === currentLang && (
                <FiCheck size={14} className="text-primary-600" />
              )}
            </DropdownMenuItem>
          ))}
        </DropdownMenuContent>
      </DropdownMenu>
    );
  }

  // Inline variant (side by side buttons)
  if (variant === 'inline') {
    return (
      <div className={cn('flex items-center gap-1 bg-gray-100 rounded-lg p-1', className)}>
        {supportedLanguages.map((lang) => (
          <button
            key={lang}
            onClick={() => changeLanguage(lang)}
            className={cn(
              'px-3 py-1.5 rounded-md text-sm font-medium transition-all',
              lang === currentLang
                ? 'bg-white text-primary-600 shadow-sm'
                : 'text-gray-600 hover:text-gray-900'
            )}
          >
            {showFlag && <span className="mr-1">{languageFlags[lang]}</span>}
            {lang.toUpperCase()}
          </button>
        ))}
      </div>
    );
  }

  // Minimal variant (just icon)
  return (
    <button
      onClick={() => {
        const nextLang = currentLang === 'es' ? 'en' : 'es';
        changeLanguage(nextLang);
      }}
      className={cn(
        'flex items-center justify-center w-10 h-10 rounded-full hover:bg-gray-100 transition-colors',
        className
      )}
      title={`Cambiar a ${languageLabels[currentLang === 'es' ? 'en' : 'es']}`}
    >
      <FiGlobe size={20} className="text-gray-600" />
    </button>
  );
}
```

### Props

| Prop        | Tipo                                  | Default      | Descripción               |
| ----------- | ------------------------------------- | ------------ | ------------------------- |
| `variant`   | `'dropdown' \| 'inline' \| 'minimal'` | `'dropdown'` | Estilo visual             |
| `className` | `string`                              | -            | Clases CSS adicionales    |
| `showLabel` | `boolean`                             | `true`       | Mostrar nombre del idioma |
| `showFlag`  | `boolean`                             | `true`       | Mostrar emoji de bandera  |

---

## ✅ VALIDACIÓN

### LocalizedContent

- [ ] Muestra texto en idioma actual
- [ ] Badge "Traducido" si es traducción
- [ ] Posición inline y top-right funcionan
- [ ] lineClamp limita líneas
- [ ] Renderiza como elemento correcto (p, span, div, h1...)

### ImageDropZone

- [ ] Drag & drop funciona
- [ ] Click abre selector de archivos
- [ ] Validación de tamaño y formato
- [ ] Preview de imágenes
- [ ] Progreso de upload visible
- [ ] Estados: pending, uploading, success, error
- [ ] Botón X elimina imagen
- [ ] Badge "Principal" en primera imagen
- [ ] Límite de archivos respetado

### ConfirmDialog

- [ ] 4 variantes visuales funcionan
- [ ] Icono y colores correctos por variante
- [ ] Botón confirmar con loading state
- [ ] Botón cancelar funciona
- [ ] infoText se muestra si existe

### ErrorBoundary

- [ ] Captura errores de React
- [ ] Reporta a Sentry con eventId
- [ ] Muestra UI de fallback
- [ ] Botón "Reintentar" resetea error
- [ ] Botón "Ir al Inicio" navega a /
- [ ] Detalles técnicos solo en desarrollo
- [ ] showDialog abre Sentry feedback

### LanguageSwitcher

- [ ] Dropdown con lista de idiomas
- [ ] Inline con botones ES/EN
- [ ] Minimal con solo icono
- [ ] Persiste en localStorage
- [ ] Actualiza document.lang

---

## 🧪 TESTS E2E (Playwright)

```typescript
import { test, expect } from "@playwright/test";

test.describe("Common Components", () => {
  test("ImageDropZone debe permitir arrastrar y soltar imágenes", async ({
    page,
  }) => {
    await page.goto("/sell/create");
    await expect(page.getByTestId("image-dropzone")).toBeVisible();
    await expect(page.getByText(/arrastra imágenes/i)).toBeVisible();
  });

  test("ImageDropZone debe mostrar preview de imágenes subidas", async ({
    page,
  }) => {
    await page.goto("/sell/create");
    const dropzone = page.getByTestId("image-dropzone");
    await dropzone.setInputFiles("tests/fixtures/test-car.jpg");
    await expect(page.getByTestId("image-preview").first()).toBeVisible();
  });

  test("ConfirmDialog debe mostrarse y responder a acciones", async ({
    page,
  }) => {
    await page.goto("/favorites");
    await page.getByTestId("delete-favorite-button").first().click();
    await expect(page.getByTestId("confirm-dialog")).toBeVisible();
    await expect(
      page.getByRole("button", { name: /confirmar/i }),
    ).toBeVisible();
    await expect(page.getByRole("button", { name: /cancelar/i })).toBeVisible();
  });

  test("ErrorBoundary debe mostrar UI de fallback en error", async ({
    page,
  }) => {
    await page.goto("/test-error-boundary");
    await expect(page.getByTestId("error-fallback")).toBeVisible();
    await expect(
      page.getByRole("button", { name: /reintentar/i }),
    ).toBeVisible();
    await expect(
      page.getByRole("link", { name: /ir al inicio/i }),
    ).toBeVisible();
  });

  test("LanguageSwitcher debe cambiar idioma y persistir", async ({ page }) => {
    await page.goto("/");
    await page.getByTestId("language-switcher").click();
    await page.getByTestId("lang-en").click();
    await expect(page).toHaveAttribute("lang", "en", { timeout: 2000 });
    await page.reload();
    await expect(page).toHaveAttribute("lang", "en");
  });

  test("LocalizedContent debe mostrar contenido traducido", async ({
    page,
  }) => {
    await page.goto("/");
    await page.getByTestId("language-switcher").click();
    await page.getByTestId("lang-en").click();
    await expect(page.getByText(/find your vehicle/i)).toBeVisible();
  });
});
```

---

_Última actualización: Enero 2026_
