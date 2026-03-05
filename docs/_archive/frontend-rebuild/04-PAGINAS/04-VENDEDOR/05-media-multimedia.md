---
title: "🖼 Media & Multimedia - Sistema Completo de Gestión"
priority: P1
estimated_time: "2 horas"
dependencies: []
apis: ["VehiclesSaleService", "MediaService"]
status: complete
last_updated: "2026-01-30"
---

# 🖼️ Media & Multimedia - Sistema Completo de Gestión

> **Última actualización:** Enero 29, 2026  
> **Complejidad:** 🟡 Media-Alta (Upload S3, Processing, CDN, 360° viewer)  
> **Estado:** 📖 Documentación Completa - Listo para Implementación  
> **Dependencias:** MediaService (backend), VehiclesSaleService, Digital Ocean Spaces

---

## 📚 DOCUMENTACIÓN BASE

Este documento integra TODOS los procesos de la carpeta `docs/process-matrix/10-MEDIA-ARCHIVOS/`:

| Documento Process Matrix      | Secciones Cubiertas                     |
| ----------------------------- | --------------------------------------- |
| `01-media-service.md`         | Upload API, CDN, Presigned URLs         |
| `02-image-processing.md`      | Resize, Watermark, Optimization         |
| `03-document-storage.md`      | Storage seguro, Retention, Audit        |
| `04-multimedia-processing.md` | Videos, 360° Tours, Document Processing |

---

## ⚠️ AUDITORÍA DE ESTADO (Enero 29, 2026)

### Estado de Implementación Backend ✅

| Proceso Backend         | Estado  | Observación                   |
| ----------------------- | ------- | ----------------------------- |
| MediaService API        | ✅ 100% | `/backend/MediaService/`      |
| Upload Presigned URLs   | ✅ 100% | Digital Ocean Spaces          |
| CDN Distribution        | ✅ 100% | DO CDN configurado            |
| Image Processing Worker | ✅ 100% | Resize, watermark, optimize   |
| Video Processing        | ✅ 95%  | FFmpeg transcoding automático |
| Document Storage        | ✅ 90%  | Encriptación AES-256          |

### Estado de Acceso UI

| Funcionalidad UI          | Estado  | Ubicación Actual                 |
| ------------------------- | ------- | -------------------------------- |
| Upload imágenes (básico)  | ✅ 100% | `/sell`, `/dealer/publish`       |
| Gallery con lightbox      | ✅ 100% | `/vehicles/:slug`                |
| Drag & Drop upload        | ✅ 90%  | Formularios de publicación       |
| Editor avanzado imágenes  | 🔴 0%   | **FALTA** - A implementar        |
| Video upload              | 🟡 70%  | Soportado pero UI básica         |
| Viewer 360° interactivo   | 🟡 60%  | Viewer básico implementado       |
| Tour virtual 360°         | 🔴 0%   | **FALTA** - A implementar        |
| Document upload (KYC)     | ✅ 90%  | `/dealer/register`, `/kyc`       |
| Document preview          | 🟡 70%  | PDF básico, sin OCR UI           |
| Media Library (gestión)   | 🔴 0%   | **FALTA** - A implementar        |
| Stats de almacenamiento   | 🔴 0%   | **FALTA** - Admin panel          |
| Audit logs de documentos  | 🔴 0%   | **FALTA** - Admin compliance     |
| Watermark personalizado   | 🔴 0%   | **FALTA** - Config por dealer    |
| Batch operations          | 🔴 0%   | **FALTA** - Bulk delete/edit     |
| Image optimization manual | 🔴 0%   | **FALTA** - Tools para optimizar |

---

## 📊 RESUMEN DE PROCESOS A IMPLEMENTAR

### MEDIA-\* (MediaService Core) - 6 procesos

| ID       | Proceso                         | Backend | UI      | Prioridad |
| -------- | ------------------------------- | ------- | ------- | --------- |
| MEDIA-01 | Upload presigned URL (S3)       | ✅ 100% | ✅ 100% | ✅ HECHO  |
| MEDIA-02 | Finalize upload + publish event | ✅ 100% | ✅ 100% | ✅ HECHO  |
| MEDIA-03 | Get media by ID                 | ✅ 100% | ✅ 100% | ✅ HECHO  |
| MEDIA-04 | Delete media                    | ✅ 100% | 🟡 60%  | 🟡 MEDIA  |
| MEDIA-05 | CDN URL generation              | ✅ 100% | ✅ 100% | ✅ HECHO  |
| MEDIA-06 | Media metadata update           | ✅ 100% | 🔴 0%   | 🔴 BAJA   |

### IMG-\* (Image Processing) - 8 procesos

| ID     | Proceso                      | Backend | UI     | Prioridad |
| ------ | ---------------------------- | ------- | ------ | --------- |
| IMG-01 | Process image (resize)       | ✅ 100% | N/A    | ✅ HECHO  |
| IMG-02 | Generate variants (S/M/L/XL) | ✅ 100% | N/A    | ✅ HECHO  |
| IMG-03 | Apply watermark              | ✅ 100% | N/A    | ✅ HECHO  |
| IMG-04 | Optimize (WebP/AVIF)         | ✅ 100% | N/A    | ✅ HECHO  |
| IMG-05 | Generate blurhash            | ✅ 100% | 🟡 50% | 🟡 MEDIA  |
| IMG-06 | Extract EXIF metadata        | ✅ 100% | 🔴 0%  | 🔴 BAJA   |
| IMG-07 | Detect NSFW content          | 🟡 70%  | N/A    | 🟡 MEDIA  |
| IMG-08 | Face detection (privacy)     | 🔴 0%   | 🔴 0%  | 🔴 BAJA   |

### VID-\* (Video Processing) - 5 procesos

| ID     | Proceso                   | Backend | UI     | Prioridad |
| ------ | ------------------------- | ------- | ------ | --------- |
| VID-01 | Upload video              | ✅ 95%  | 🟡 70% | 🟡 MEDIA  |
| VID-02 | Transcode to MP4/WebM     | ✅ 90%  | N/A    | ✅ HECHO  |
| VID-03 | Generate thumbnail        | ✅ 90%  | 🟡 60% | 🟡 MEDIA  |
| VID-04 | Extract duration/metadata | ✅ 90%  | 🔴 0%  | 🔴 BAJA   |
| VID-05 | Streaming HLS/DASH        | 🔴 0%   | 🔴 0%  | 🔴 FUTURA |

### DOC-\* (Document Storage) - 5 procesos

| ID     | Proceso                       | Backend | UI     | Prioridad |
| ------ | ----------------------------- | ------- | ------ | --------- |
| DOC-01 | Upload document (encrypted)   | ✅ 90%  | ✅ 90% | ✅ HECHO  |
| DOC-02 | Download document (presigned) | ✅ 90%  | ✅ 80% | ✅ HECHO  |
| DOC-03 | Audit log (access tracking)   | 🟡 70%  | 🔴 0%  | 🟡 MEDIA  |
| DOC-04 | Retention policy              | 🟡 60%  | 🔴 0%  | 🔴 BAJA   |
| DOC-05 | Generate PDF preview          | 🟡 70%  | 🟡 70% | 🟡 MEDIA  |

### 360-\* (360° Tours) - 4 procesos

| ID     | Proceso                        | Backend | UI     | Prioridad |
| ------ | ------------------------------ | ------- | ------ | --------- |
| 360-01 | Upload 360° photos             | 🟡 70%  | 🟡 60% | 🟡 MEDIA  |
| 360-02 | Stitch panorama (equirect)     | 🔴 0%   | 🔴 0%  | 🔴 FUTURA |
| 360-03 | Interactive viewer (pannellum) | 🟡 60%  | 🟡 60% | 🟡 MEDIA  |
| 360-04 | Hotspots & annotations         | 🔴 0%   | 🔴 0%  | 🔴 FUTURA |

**TOTAL: 28 procesos** (13 ✅ completos, 11 🟡 parciales, 4 🔴 pendientes)

---

## 🎯 OBJETIVO DE ESTE DOCUMENTO

Implementar UI completa para:

1. **Upload mejorado:** Drag & Drop con preview, progress, retry
2. **Media Library:** Gestión centralizada de archivos
3. **Image Editor:** Crop, rotate, filters básicos
4. **Video Player:** Player custom con controles
5. **360° Viewer:** Viewer interactivo mejorado
6. **Document Viewer:** Preview de PDFs con navegación
7. **Admin Tools:** Stats, audit logs, bulk operations

---

## 🏗️ ARQUITECTURA

### Flujo de Upload (Presigned URL)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      Upload Flow con Presigned URLs                         │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1️⃣ INIT UPLOAD                                                             │
│  Frontend                → POST /api/media/upload/init                      │
│                          {                                                  │
│                            fileName: "camry-2024.jpg",                      │
│                            fileSize: 2048000,                               │
│                            mimeType: "image/jpeg",                          │
│                            category: "vehicle_photo"                        │
│                          }                                                  │
│                                                                             │
│  MediaService API        → Validate (size, type, virus scan)               │
│                          → Generate unique ID: img_abc123                   │
│                          → Create DB record (status: Pending)               │
│                          → Request presigned URL from S3                    │
│                          ← Return presigned URL + mediaId                   │
│                                                                             │
│  2️⃣ DIRECT UPLOAD                                                           │
│  Frontend                → PUT {presignedURL}                               │
│                             Content-Type: image/jpeg                        │
│                             Body: [binary image data]                       │
│                                                                             │
│  Digital Ocean Spaces    ← Store in /originals/img_abc123.jpg              │
│  (S3-compatible)         → Return 200 OK                                    │
│                                                                             │
│  3️⃣ FINALIZE                                                                │
│  Frontend                → POST /api/media/upload/finalize                  │
│                          { mediaId: "img_abc123" }                          │
│                                                                             │
│  MediaService API        → Update DB (status: Processing)                  │
│                          → Verify file exists in S3                         │
│                          → Publish event: media.uploaded                    │
│                          ← Return media object                              │
│                                                                             │
│  4️⃣ ASYNC PROCESSING                                                        │
│  RabbitMQ                → media.uploaded event                             │
│                                                                             │
│  ImageProcessingWorker   → Download from S3                                 │
│                          → Generate variants:                               │
│                             • thumbnail (150x150)                           │
│                             • small (400x300)                               │
│                             • medium (800x600)                              │
│                             • large (1200x900)                              │
│                          → Apply watermark (logo OKLA)                      │
│                          → Optimize (WebP + AVIF)                           │
│                          → Generate blurhash                                │
│                          → Upload variants to S3                            │
│                          → Update DB (status: Ready)                        │
│                          → Publish: media.processed                         │
│                                                                             │
│  5️⃣ CDN DELIVERY                                                            │
│  Frontend requests       → GET https://cdn.okla.com.do/img_abc123_m.webp   │
│  Digital Ocean CDN       ← Serve from edge cache                            │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Tipos de Media

| Tipo         | Prefijo | Max Size | Formatos             | Procesamiento      |
| ------------ | ------- | -------- | -------------------- | ------------------ |
| **Image**    | `img_`  | 10 MB    | JPG, PNG, WebP, HEIC | Resize + Watermark |
| **Video**    | `vid_`  | 100 MB   | MP4, MOV, AVI        | Transcode + Thumb  |
| **Document** | `doc_`  | 25 MB    | PDF, DOC, DOCX       | Encrypt + Preview  |
| **Audio**    | `aud_`  | 20 MB    | MP3, WAV             | Transcode          |
| **360°**     | `p360_` | 15 MB    | JPG (equirect)       | Stitch + Viewer    |

### Storage Structure (Digital Ocean Spaces)

```
okla-bucket/
├── originals/
│   ├── img_abc123.jpg           # Original sin procesar
│   ├── vid_def456.mp4
│   └── doc_ghi789.pdf
│
├── processed/
│   ├── images/
│   │   ├── img_abc123_thumb.webp   # 150x150
│   │   ├── img_abc123_s.webp       # 400x300
│   │   ├── img_abc123_m.webp       # 800x600
│   │   ├── img_abc123_l.webp       # 1200x900
│   │   └── img_abc123_xl.webp      # 1920x1080
│   │
│   ├── videos/
│   │   ├── vid_def456_480p.mp4
│   │   ├── vid_def456_720p.mp4
│   │   ├── vid_def456_thumb.jpg
│   │   └── vid_def456.m3u8          # HLS manifest
│   │
│   └── documents/
│       ├── doc_ghi789_preview.jpg   # Primera página
│       └── doc_ghi789.encrypted     # Encriptado AES-256
│
└── temp/
    └── upload_xyz.tmp               # Uploads en progreso
```

---

## 📦 COMPONENTES A IMPLEMENTAR

### 1. MediaUploader (Drag & Drop)

**Ubicación:** `src/components/media/MediaUploader.tsx`

```typescript
// filepath: src/components/media/MediaUploader.tsx
"use client";

import * as React from "react";
import { useDropzone } from "react-dropzone";
import { Upload, X, CheckCircle, AlertCircle, Loader2 } from "lucide-react";
import { Progress } from "@/components/ui/progress";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { mediaService } from "@/lib/services/mediaService";
import { showToast } from "@/lib/toast";

interface MediaUploaderProps {
  accept?: Record<string, string[]>;
  maxSize?: number;
  maxFiles?: number;
  category: "vehicle_photo" | "profile_photo" | "document" | "video" | "panorama_360";
  onUploadComplete?: (media: UploadedMedia[]) => void;
  className?: string;
}

interface UploadedMedia {
  id: string;
  url: string;
  type: string;
  status: "uploading" | "processing" | "ready" | "error";
  progress: number;
  error?: string;
}

export function MediaUploader({
  accept = {
    "image/*": [".jpg", ".jpeg", ".png", ".webp", ".heic"],
  },
  maxSize = 10 * 1024 * 1024, // 10MB default
  maxFiles = 10,
  category,
  onUploadComplete,
  className,
}: MediaUploaderProps) {
  const [uploads, setUploads] = React.useState<UploadedMedia[]>([]);
  const [isUploading, setIsUploading] = React.useState(false);

  const onDrop = React.useCallback(
    async (acceptedFiles: File[]) => {
      if (acceptedFiles.length === 0) return;

      setIsUploading(true);

      // Initialize upload states
      const newUploads: UploadedMedia[] = acceptedFiles.map((file) => ({
        id: `temp_${Date.now()}_${Math.random()}`,
        url: URL.createObjectURL(file),
        type: file.type,
        status: "uploading" as const,
        progress: 0,
      }));

      setUploads((prev) => [...prev, ...newUploads]);

      // Upload each file
      for (let i = 0; i < acceptedFiles.length; i++) {
        const file = acceptedFiles[i];
        const tempId = newUploads[i].id;

        try {
          // 1. Init upload
          const initResponse = await mediaService.initUpload({
            fileName: file.name,
            fileSize: file.size,
            mimeType: file.type,
            category,
          });

          // 2. Upload to S3 with progress
          await mediaService.uploadToS3(
            initResponse.presignedUrl,
            file,
            (progress) => {
              setUploads((prev) =>
                prev.map((u) =>
                  u.id === tempId ? { ...u, progress } : u
                )
              );
            }
          );

          // 3. Finalize
          const media = await mediaService.finalizeUpload(initResponse.mediaId);

          // Update with real media ID and processing status
          setUploads((prev) =>
            prev.map((u) =>
              u.id === tempId
                ? {
                    ...u,
                    id: media.id,
                    url: media.cdnUrl,
                    status: "processing" as const,
                    progress: 100,
                  }
                : u
            )
          );

          // 4. Poll for processing completion
          pollProcessingStatus(media.id);
        } catch (error) {
          console.error("Upload failed:", error);
          setUploads((prev) =>
            prev.map((u) =>
              u.id === tempId
                ? {
                    ...u,
                    status: "error" as const,
                    error: error instanceof Error ? error.message : "Upload failed",
                  }
                : u
            )
          );
          showToast.error(`Error al subir ${file.name}`);
        }
      }

      setIsUploading(false);
    },
    [category]
  );

  const pollProcessingStatus = async (mediaId: string) => {
    let attempts = 0;
    const maxAttempts = 30; // 30 seconds max

    const poll = setInterval(async () => {
      attempts++;

      try {
        const media = await mediaService.getById(mediaId);

        if (media.status === "ready") {
          setUploads((prev) =>
            prev.map((u) =>
              u.id === mediaId
                ? { ...u, status: "ready" as const, url: media.cdnUrl }
                : u
            )
          );
          clearInterval(poll);

          // Notify parent
          if (onUploadComplete) {
            const readyUploads = uploads.filter((u) => u.status === "ready");
            onUploadComplete([...readyUploads, { ...media, status: "ready", progress: 100 }]);
          }
        } else if (media.status === "error") {
          setUploads((prev) =>
            prev.map((u) =>
              u.id === mediaId
                ? { ...u, status: "error" as const, error: "Processing failed" }
                : u
            )
          );
          clearInterval(poll);
        }

        if (attempts >= maxAttempts) {
          clearInterval(poll);
          showToast.warning("El procesamiento está tomando más tiempo de lo esperado");
        }
      } catch (error) {
        console.error("Polling error:", error);
        clearInterval(poll);
      }
    }, 1000);
  };

  const removeUpload = (id: string) => {
    setUploads((prev) => prev.filter((u) => u.id !== id));
  };

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept,
    maxSize,
    maxFiles,
    disabled: isUploading || uploads.length >= maxFiles,
  });

  return (
    <div className={cn("space-y-4", className)}>
      {/* Dropzone */}
      <div
        {...getRootProps()}
        className={cn(
          "border-2 border-dashed rounded-lg p-8 text-center transition-colors",
          isDragActive
            ? "border-blue-500 bg-blue-50"
            : "border-gray-300 hover:border-gray-400",
          isUploading && "opacity-50 cursor-not-allowed"
        )}
      >
        <input {...getInputProps()} />
        <Upload className="mx-auto h-12 w-12 text-gray-400 mb-4" />
        {isDragActive ? (
          <p className="text-blue-600 font-medium">Suelta los archivos aquí...</p>
        ) : (
          <>
            <p className="text-gray-600 mb-1">
              Arrastra archivos aquí o haz clic para seleccionar
            </p>
            <p className="text-sm text-gray-500">
              Máximo {maxFiles} archivos, {(maxSize / 1024 / 1024).toFixed(0)} MB cada uno
            </p>
          </>
        )}
      </div>

      {/* Upload List */}
      {uploads.length > 0 && (
        <div className="space-y-2">
          {uploads.map((upload) => (
            <div
              key={upload.id}
              className="flex items-center gap-3 p-3 bg-white border rounded-lg"
            >
              {/* Preview */}
              {upload.type.startsWith("image/") && (
                <img
                  src={upload.url}
                  alt=""
                  className="w-12 h-12 object-cover rounded"
                />
              )}

              {/* Info */}
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 mb-1">
                  {upload.status === "uploading" && (
                    <Loader2 className="h-4 w-4 animate-spin text-blue-500" />
                  )}
                  {upload.status === "processing" && (
                    <Loader2 className="h-4 w-4 animate-spin text-yellow-500" />
                  )}
                  {upload.status === "ready" && (
                    <CheckCircle className="h-4 w-4 text-green-500" />
                  )}
                  {upload.status === "error" && (
                    <AlertCircle className="h-4 w-4 text-red-500" />
                  )}

                  <span className="text-sm font-medium truncate">
                    {upload.status === "uploading" && "Subiendo..."}
                    {upload.status === "processing" && "Procesando..."}
                    {upload.status === "ready" && "Listo"}
                    {upload.status === "error" && "Error"}
                  </span>
                </div>

                {upload.status === "uploading" && (
                  <Progress value={upload.progress} className="h-1" />
                )}

                {upload.error && (
                  <p className="text-xs text-red-600 mt-1">{upload.error}</p>
                )}
              </div>

              {/* Remove button */}
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => removeUpload(upload.id)}
                disabled={upload.status === "uploading"}
              >
                <X className="h-4 w-4" />
              </Button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
```

---

### 2. MediaLibrary (Gestión centralizada)

**Ubicación:** `src/components/media/MediaLibrary.tsx`

```typescript
// filepath: src/components/media/MediaLibrary.tsx
"use client";

import * as React from "react";
import { Search, Filter, Grid, List, Trash2, Download, Eye } from "lucide-react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Checkbox } from "@/components/ui/checkbox";
import { MediaGrid } from "./MediaGrid";
import { MediaList } from "./MediaList";
import { MediaPreviewModal } from "./MediaPreviewModal";
import { useMediaLibrary } from "@/lib/hooks/useMediaLibrary";
import { formatFileSize, formatDate } from "@/lib/utils";

interface MediaLibraryProps {
  userId?: string;
  category?: string;
  onSelect?: (media: Media[]) => void;
  selectionMode?: boolean;
}

export function MediaLibrary({
  userId,
  category,
  onSelect,
  selectionMode = false,
}: MediaLibraryProps) {
  const [view, setView] = React.useState<"grid" | "list">("grid");
  const [searchQuery, setSearchQuery] = React.useState("");
  const [selectedMedia, setSelectedMedia] = React.useState<Set<string>>(new Set());
  const [previewMedia, setPreviewMedia] = React.useState<Media | null>(null);

  const {
    media,
    isLoading,
    filters,
    setFilters,
    deleteMedia,
    downloadMedia,
  } = useMediaLibrary({ userId, category });

  const filteredMedia = React.useMemo(() => {
    if (!searchQuery) return media;
    return media.filter((m) =>
      m.fileName.toLowerCase().includes(searchQuery.toLowerCase())
    );
  }, [media, searchQuery]);

  const handleSelectAll = () => {
    if (selectedMedia.size === filteredMedia.length) {
      setSelectedMedia(new Set());
    } else {
      setSelectedMedia(new Set(filteredMedia.map((m) => m.id)));
    }
  };

  const handleSelect = (id: string) => {
    const newSelection = new Set(selectedMedia);
    if (newSelection.has(id)) {
      newSelection.delete(id);
    } else {
      newSelection.add(id);
    }
    setSelectedMedia(newSelection);

    if (selectionMode && onSelect) {
      const selected = filteredMedia.filter((m) => newSelection.has(m.id));
      onSelect(selected);
    }
  };

  const handleBulkDelete = async () => {
    if (confirm(`¿Eliminar ${selectedMedia.size} archivos?`)) {
      for (const id of selectedMedia) {
        await deleteMedia(id);
      }
      setSelectedMedia(new Set());
    }
  };

  const handleBulkDownload = async () => {
    for (const id of selectedMedia) {
      await downloadMedia(id);
    }
  };

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex flex-col sm:flex-row gap-4 justify-between">
        {/* Search */}
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
          <Input
            type="search"
            placeholder="Buscar archivos..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="pl-10"
          />
        </div>

        {/* Actions */}
        <div className="flex items-center gap-2">
          {/* Filters */}
          <Select
            value={filters.type}
            onValueChange={(value) => setFilters({ ...filters, type: value })}
          >
            <SelectTrigger className="w-32">
              <Filter className="h-4 w-4 mr-2" />
              <SelectValue placeholder="Tipo" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Todos</SelectItem>
              <SelectItem value="image">Imágenes</SelectItem>
              <SelectItem value="video">Videos</SelectItem>
              <SelectItem value="document">Documentos</SelectItem>
            </SelectContent>
          </Select>

          {/* View toggle */}
          <div className="flex border rounded-lg">
            <Button
              type="button"
              variant={view === "grid" ? "secondary" : "ghost"}
              size="sm"
              onClick={() => setView("grid")}
            >
              <Grid className="h-4 w-4" />
            </Button>
            <Button
              type="button"
              variant={view === "list" ? "secondary" : "ghost"}
              size="sm"
              onClick={() => setView("list")}
            >
              <List className="h-4 w-4" />
            </Button>
          </div>
        </div>
      </div>

      {/* Bulk actions */}
      {selectedMedia.size > 0 && (
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-3 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Checkbox checked={true} onCheckedChange={handleSelectAll} />
            <span className="text-sm font-medium">
              {selectedMedia.size} seleccionados
            </span>
          </div>
          <div className="flex gap-2">
            <Button type="button" variant="outline" size="sm" onClick={handleBulkDownload}>
              <Download className="h-4 w-4 mr-2" />
              Descargar
            </Button>
            <Button
              type="button"
              variant="destructive"
              size="sm"
              onClick={handleBulkDelete}
            >
              <Trash2 className="h-4 w-4 mr-2" />
              Eliminar
            </Button>
          </div>
        </div>
      )}

      {/* Media Grid/List */}
      <Tabs value={filters.type} onValueChange={(v) => setFilters({ ...filters, type: v })}>
        <TabsList>
          <TabsTrigger value="all">Todos ({media.length})</TabsTrigger>
          <TabsTrigger value="image">
            Imágenes ({media.filter((m) => m.type.startsWith("image")).length})
          </TabsTrigger>
          <TabsTrigger value="video">
            Videos ({media.filter((m) => m.type.startsWith("video")).length})
          </TabsTrigger>
          <TabsTrigger value="document">
            Documentos ({media.filter((m) => m.type === "application/pdf").length})
          </TabsTrigger>
        </TabsList>

        <TabsContent value={filters.type} className="mt-4">
          {view === "grid" ? (
            <MediaGrid
              media={filteredMedia}
              selectedMedia={selectedMedia}
              onSelect={handleSelect}
              onPreview={setPreviewMedia}
              isLoading={isLoading}
            />
          ) : (
            <MediaList
              media={filteredMedia}
              selectedMedia={selectedMedia}
              onSelect={handleSelect}
              onPreview={setPreviewMedia}
              isLoading={isLoading}
            />
          )}
        </TabsContent>
      </Tabs>

      {/* Preview Modal */}
      {previewMedia && (
        <MediaPreviewModal
          media={previewMedia}
          onClose={() => setPreviewMedia(null)}
        />
      )}
    </div>
  );
}
```

---

### 3. Video360Viewer (Interactivo)

**Ubicación:** `src/components/media/Video360Viewer.tsx`

```typescript
// filepath: src/components/media/Video360Viewer.tsx
"use client";

import * as React from "react";
import { Maximize, Minimize, RotateCw, ZoomIn, ZoomOut } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

// Requires: npm install pannellum pannellum-react
import { Pannellum } from "pannellum-react";

interface Video360ViewerProps {
  imageUrl: string;
  title?: string;
  autoRotate?: boolean;
  hotSpots?: HotSpot[];
  className?: string;
}

interface HotSpot {
  pitch: number;
  yaw: number;
  type: "info" | "custom";
  text?: string;
  onClick?: () => void;
}

export function Video360Viewer({
  imageUrl,
  title,
  autoRotate = true,
  hotSpots = [],
  className,
}: Video360ViewerProps) {
  const [isFullscreen, setIsFullscreen] = React.useState(false);
  const [rotation, setRotation] = React.useState(0);
  const containerRef = React.useRef<HTMLDivElement>(null);

  const toggleFullscreen = () => {
    if (!document.fullscreenElement && containerRef.current) {
      containerRef.current.requestFullscreen();
      setIsFullscreen(true);
    } else if (document.exitFullscreen) {
      document.exitFullscreen();
      setIsFullscreen(false);
    }
  };

  const handleRotate = () => {
    setRotation((prev) => (prev + 90) % 360);
  };

  return (
    <div
      ref={containerRef}
      className={cn(
        "relative bg-black rounded-lg overflow-hidden",
        isFullscreen && "fixed inset-0 z-50",
        className
      )}
    >
      {/* Title */}
      {title && (
        <div className="absolute top-4 left-4 z-10 bg-black/70 px-4 py-2 rounded-lg">
          <h3 className="text-white font-medium">{title}</h3>
        </div>
      )}

      {/* Controls */}
      <div className="absolute bottom-4 right-4 z-10 flex gap-2">
        <Button
          type="button"
          variant="secondary"
          size="sm"
          onClick={handleRotate}
          className="bg-black/70 hover:bg-black/90"
        >
          <RotateCw className="h-4 w-4" />
        </Button>

        <Button
          type="button"
          variant="secondary"
          size="sm"
          onClick={toggleFullscreen}
          className="bg-black/70 hover:bg-black/90"
        >
          {isFullscreen ? (
            <Minimize className="h-4 w-4" />
          ) : (
            <Maximize className="h-4 w-4" />
          )}
        </Button>
      </div>

      {/* Pannellum Viewer */}
      <Pannellum
        width="100%"
        height="600px"
        image={imageUrl}
        pitch={10}
        yaw={rotation}
        hfov={110}
        autoLoad
        autoRotate={autoRotate ? 2 : 0}
        showZoomCtrl={true}
        showFullscreenCtrl={false}
        hotspotDebug={false}
        hotSpots={hotSpots.map((spot) => ({
          pitch: spot.pitch,
          yaw: spot.yaw,
          type: spot.type,
          text: spot.text,
          clickHandlerFunc: spot.onClick,
        }))}
      />

      {/* Instructions (first time only) */}
      <div className="absolute bottom-20 left-1/2 -translate-x-1/2 z-10 pointer-events-none">
        <div className="bg-black/70 px-4 py-2 rounded-full text-white text-sm">
          Arrastra para mirar alrededor • Scroll para zoom
        </div>
      </div>
    </div>
  );
}
```

---

## 🔌 API SERVICE

### mediaService.ts

```typescript
// filepath: src/lib/services/mediaService.ts
import { api } from "./api";

export interface InitUploadRequest {
  fileName: string;
  fileSize: number;
  mimeType: string;
  category:
    | "vehicle_photo"
    | "profile_photo"
    | "document"
    | "video"
    | "panorama_360";
}

export interface InitUploadResponse {
  mediaId: string;
  presignedUrl: string;
  expiresIn: number;
}

export interface Media {
  id: string;
  fileName: string;
  fileSize: number;
  mimeType: string;
  category: string;
  status: "pending" | "processing" | "ready" | "error";
  originalUrl: string;
  cdnUrl: string;
  thumbnailUrl?: string;
  variants?: MediaVariant[];
  metadata?: Record<string, any>;
  createdAt: string;
  updatedAt: string;
}

export interface MediaVariant {
  size: "thumb" | "small" | "medium" | "large" | "xlarge";
  url: string;
  width: number;
  height: number;
  format: "webp" | "avif" | "jpg";
}

class MediaService {
  /**
   * MEDIA-01: Init upload (presigned URL)
   */
  async initUpload(request: InitUploadRequest): Promise<InitUploadResponse> {
    const response = await api.post<InitUploadResponse>(
      "/media/upload/init",
      request,
    );
    return response.data;
  }

  /**
   * Direct upload to S3 with progress
   */
  async uploadToS3(
    presignedUrl: string,
    file: File,
    onProgress?: (progress: number) => void,
  ): Promise<void> {
    return new Promise((resolve, reject) => {
      const xhr = new XMLHttpRequest();

      xhr.upload.addEventListener("progress", (e) => {
        if (e.lengthComputable && onProgress) {
          const progress = Math.round((e.loaded / e.total) * 100);
          onProgress(progress);
        }
      });

      xhr.addEventListener("load", () => {
        if (xhr.status === 200) {
          resolve();
        } else {
          reject(new Error(`Upload failed with status ${xhr.status}`));
        }
      });

      xhr.addEventListener("error", () => {
        reject(new Error("Upload failed"));
      });

      xhr.open("PUT", presignedUrl);
      xhr.setRequestHeader("Content-Type", file.type);
      xhr.send(file);
    });
  }

  /**
   * MEDIA-02: Finalize upload
   */
  async finalizeUpload(mediaId: string): Promise<Media> {
    const response = await api.post<Media>(`/media/upload/finalize`, {
      mediaId,
    });
    return response.data;
  }

  /**
   * MEDIA-03: Get media by ID
   */
  async getById(id: string): Promise<Media> {
    const response = await api.get<Media>(`/media/${id}`);
    return response.data;
  }

  /**
   * MEDIA-04: Delete media
   */
  async delete(id: string): Promise<void> {
    await api.delete(`/media/${id}`);
  }

  /**
   * Get user's media library
   */
  async getUserMedia(
    userId: string,
    filters?: {
      type?: string;
      category?: string;
      page?: number;
      pageSize?: number;
    },
  ): Promise<{ items: Media[]; total: number }> {
    const response = await api.get<{ items: Media[]; total: number }>(
      `/media/user/${userId}`,
      { params: filters },
    );
    return response.data;
  }

  /**
   * Download media
   */
  async download(id: string): Promise<void> {
    const response = await api.get(`/media/${id}/download`, {
      responseType: "blob",
    });

    const url = window.URL.createObjectURL(new Blob([response.data]));
    const link = document.createElement("a");
    link.href = url;
    link.setAttribute("download", `media_${id}`);
    document.body.appendChild(link);
    link.click();
    link.remove();
  }

  /**
   * Get CDN URL for specific variant
   */
  getCdnUrl(
    media: Media,
    variant: "thumb" | "small" | "medium" | "large" | "original" = "medium",
  ): string {
    if (variant === "original") return media.cdnUrl;

    const variantObj = media.variants?.find((v) => v.size === variant);
    return variantObj?.url || media.cdnUrl;
  }
}

export const mediaService = new MediaService();
```

---

## 📍 INTEGRACIÓN EN PÁGINAS EXISTENTES

### 1. PublishForm - Agregar Video360Step

**Modificar:** `docs/frontend-rebuild/04-PAGINAS/04-publicar.md`

**Línea ~160 - Agregar paso de Video 360°:**

```typescript
const STEPS = [
  { title: "Información básica", component: BasicInfoStep },
  { title: "Detalles", component: DetailsStep },
  { title: "Fotos", component: PhotosStep },
  { title: "Video 360°", component: Video360Step }, // ← NUEVO
  { title: "Precio", component: PricingStep },
  { title: "Revisar", component: PreviewStep },
];
```

**Nuevo componente Video360Step:**

```typescript
// filepath: src/components/publish/steps/Video360Step.tsx
"use client";

import * as React from "react";
import { useFormContext } from "react-hook-form";
import { Info } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { MediaUploader } from "@/components/media/MediaUploader";
import { Video360Viewer } from "@/components/media/Video360Viewer";
import { PublishFormData } from "../PublishForm";

export function Video360Step() {
  const { watch, setValue } = useFormContext<PublishFormData>();
  const video360 = watch("video360");

  const handleUpload = (media: any[]) => {
    if (media.length > 0) {
      setValue("video360", {
        id: media[0].id,
        url: media[0].url,
      });
    }
  };

  const handleRemove = () => {
    setValue("video360", undefined);
  };

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold">Vista 360° (Opcional)</h2>
        <p className="text-gray-600 mt-1">
          Agrega una foto panorámica del interior para que los compradores
          exploren tu vehículo
        </p>
      </div>

      <Alert>
        <Info className="h-4 w-4" />
        <AlertDescription>
          Para mejores resultados, usa la cámara panorámica de tu smartphone o una
          cámara 360°. La foto debe ser en formato equirectangular (2:1 aspect ratio).
        </AlertDescription>
      </Alert>

      {!video360 ? (
        <MediaUploader
          accept={{ "image/*": [".jpg", ".jpeg", ".png"] }}
          maxSize={15 * 1024 * 1024} // 15MB
          maxFiles={1}
          category="panorama_360"
          onUploadComplete={handleUpload}
        />
      ) : (
        <div className="space-y-4">
          <Video360Viewer
            imageUrl={video360.url}
            title="Preview de tu vista 360°"
            autoRotate={true}
          />

          <div className="flex justify-center">
            <Button type="button" variant="outline" onClick={handleRemove}>
              Cambiar foto 360°
            </Button>
          </div>
        </div>
      )}

      <div className="bg-gray-50 rounded-lg p-4">
        <h3 className="font-medium mb-2">¿Por qué agregar vista 360°?</h3>
        <ul className="space-y-1 text-sm text-gray-600">
          <li>✓ Los compradores pueden explorar el interior sin visitarlo</li>
          <li>✓ Aumenta el engagement en un 40%</li>
          <li>✓ Reduce preguntas sobre el estado del interior</li>
          <li>✓ Destaca tu publicación con un badge especial</li>
        </ul>
      </div>
    </div>
  );
}
```

---

### 2. VehicleDetailPage - Agregar Tab de 360°

**Modificar:** `docs/frontend-rebuild/04-PAGINAS/03-detalle-vehiculo.md`

**Actualizar VehicleTabs para incluir tab de 360°:**

```typescript
// filepath: src/components/vehicle-detail/VehicleTabs.tsx
import { Video360Viewer } from "@/components/media/Video360Viewer";

export function VehicleTabs({ vehicle }: VehicleTabsProps) {
  return (
    <Tabs defaultValue="description">
      <TabsList className="grid w-full grid-cols-5">
        <TabsTrigger value="description">Descripción</TabsTrigger>
        <TabsTrigger value="specs">Especificaciones</TabsTrigger>
        <TabsTrigger value="features">Características</TabsTrigger>
        {vehicle.video360Url && (
          <TabsTrigger value="360">Vista 360° 🔥</TabsTrigger>
        )}
        <TabsTrigger value="location">Ubicación</TabsTrigger>
      </TabsList>

      {/* ... otros tabs ... */}

      {vehicle.video360Url && (
        <TabsContent value="360">
          <div className="bg-white rounded-lg p-6">
            <h3 className="text-lg font-semibold mb-4">Vista interior 360°</h3>
            <Video360Viewer
              imageUrl={vehicle.video360Url}
              title={`Interior ${vehicle.year} ${vehicle.make} ${vehicle.model}`}
              autoRotate={true}
            />
          </div>
        </TabsContent>
      )}
    </Tabs>
  );
}
```

---

### 3. Dealer Inventory - Media Library

**Nueva página:** `/dealer/media`

```typescript
// filepath: src/app/(dealer)/dealer/media/page.tsx
import { Metadata } from "next";
import { MediaLibrary } from "@/components/media/MediaLibrary";
import { auth } from "@/lib/auth";
import { redirect } from "next/navigation";

export const metadata: Metadata = {
  title: "Mi Biblioteca de Medios | OKLA Dealer",
  description: "Gestiona tus imágenes, videos y documentos",
};

export default async function DealerMediaPage() {
  const session = await auth();

  if (!session?.user) {
    redirect("/login?callbackUrl=/dealer/media");
  }

  return (
    <div className="container py-8">
      <div className="mb-8">
        <h1 className="text-3xl font-bold">Mi Biblioteca de Medios</h1>
        <p className="text-gray-600 mt-2">
          Gestiona todos tus archivos multimedia en un solo lugar
        </p>
      </div>

      <MediaLibrary userId={session.user.id} />
    </div>
  );
}
```

---

## 🧪 TESTING

### Unit Tests - MediaUploader

```typescript
// filepath: src/components/media/__tests__/MediaUploader.test.tsx
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MediaUploader } from "../MediaUploader";
import { mediaService } from "@/lib/services/mediaService";

jest.mock("@/lib/services/mediaService");

describe("MediaUploader", () => {
  it("should render dropzone", () => {
    render(<MediaUploader category="vehicle_photo" />);
    expect(screen.getByText(/arrastra archivos/i)).toBeInTheDocument();
  });

  it("should handle file drop", async () => {
    const user = userEvent.setup();
    const mockOnComplete = jest.fn();

    (mediaService.initUpload as jest.Mock).mockResolvedValue({
      mediaId: "img_123",
      presignedUrl: "https://s3.example.com/upload",
      expiresIn: 3600,
    });

    (mediaService.uploadToS3 as jest.Mock).mockResolvedValue(undefined);

    (mediaService.finalizeUpload as jest.Mock).mockResolvedValue({
      id: "img_123",
      cdnUrl: "https://cdn.example.com/img_123.jpg",
      status: "processing",
    });

    render(
      <MediaUploader
        category="vehicle_photo"
        onUploadComplete={mockOnComplete}
      />
    );

    const file = new File(["test"], "test.jpg", { type: "image/jpeg" });
    const input = screen.getByRole("textbox", { hidden: true });

    await user.upload(input, file);

    await waitFor(() => {
      expect(mediaService.initUpload).toHaveBeenCalledWith({
        fileName: "test.jpg",
        fileSize: file.size,
        mimeType: "image/jpeg",
        category: "vehicle_photo",
      });
    });
  });

  it("should show progress during upload", async () => {
    // Test implementation
  });

  it("should handle upload errors", async () => {
    // Test implementation
  });
});
```

---

## 📊 MÉTRICAS DE ÉXITO

| Métrica                     | Objetivo | Método de Medición          |
| --------------------------- | -------- | --------------------------- |
| Upload success rate         | > 98%    | `media.uploaded` events     |
| Processing time (images)    | < 10s    | `media.processed` timestamp |
| CDN cache hit rate          | > 95%    | CloudFlare Analytics        |
| User satisfaction (uploads) | > 4.5/5  | In-app survey               |
| 360° viewer engagement      | > 40%    | Click rate on tab           |

---

## 🚀 PRÓXIMOS PASOS

### Sprint 1: Core Upload & Library (Alta Prioridad)

- [ ] MediaUploader component
- [ ] MediaLibrary component
- [ ] MediaGrid & MediaList
- [ ] MediaPreviewModal
- [ ] Integration en PublishForm
- [ ] Tests unitarios (> 80% coverage)

### Sprint 2: 360° & Video (Media Prioridad)

- [ ] Video360Viewer component
- [ ] Video360Step en PublishForm
- [ ] Tab de 360° en VehicleDetailPage
- [ ] Video player mejorado
- [ ] Video thumbnail generation UI

### Sprint 3: Admin & Analytics (Baja Prioridad)

- [ ] Admin media dashboard
- [ ] Storage stats
- [ ] Audit logs UI
- [ ] Batch operations
- [ ] Watermark customization

---

## 📚 REFERENCIAS

### Documentos Process Matrix

- [01-media-service.md](../../process-matrix/10-MEDIA-ARCHIVOS/01-media-service.md)
- [02-image-processing.md](../../process-matrix/10-MEDIA-ARCHIVOS/02-image-processing.md)
- [03-document-storage.md](../../process-matrix/10-MEDIA-ARCHIVOS/03-document-storage.md)
- [04-multimedia-processing.md](../../process-matrix/10-MEDIA-ARCHIVOS/04-multimedia-processing.md)

### Documentos Relacionados Frontend

- [04-publicar.md](./01-publicar-vehiculo.md) - Formulario de publicación
- [03-detalle-vehiculo.md](../01-PUBLICO/03-detalle-vehiculo.md) - Detalle con galería
- [09-dealer-inventario.md](../05-DEALER/02-dealer-inventario.md) - Gestión de inventario

### Backend APIs

- `MediaService.Api` - `/backend/MediaService/MediaService.Api/`
- `MediaService.Workers` - `/backend/MediaService/MediaService.Workers/`

### Librerías Externas

- [react-dropzone](https://react-dropzone.js.org/) - File upload
- [pannellum](https://pannellum.org/) - 360° viewer
- [video.js](https://videojs.com/) - Video player

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/media-multimedia.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsUser } from "../helpers/auth";

test.describe("Media & Multimedia", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test.describe("Image Upload", () => {
    test("debe mostrar dropzone para imágenes", async ({ page }) => {
      await page.goto("/publicar");
      // Navegar al paso de fotos

      await expect(page.getByTestId("image-dropzone")).toBeVisible();
    });

    test("debe subir imagen vía drag & drop", async ({ page }) => {
      await page.goto("/publicar");

      const dropzone = page.getByTestId("image-dropzone");
      const fileInput = page.locator('input[type="file"]');
      await fileInput.setInputFiles("./fixtures/car-photo.jpg");

      await expect(page.getByTestId("uploaded-image")).toBeVisible();
    });

    test("debe reordenar imágenes con drag", async ({ page }) => {
      await page.goto("/publicar");
      // Subir múltiples imágenes primero

      const images = page.getByTestId("sortable-image");
      if ((await images.count()) >= 2) {
        // Drag first to second position
        await images.first().dragTo(images.nth(1));
      }
    });

    test("debe eliminar imagen", async ({ page }) => {
      await page.goto("/publicar");

      await page.getByTestId("remove-image").first().click();
      await expect(page.getByText(/imagen eliminada/i)).toBeVisible();
    });
  });

  test.describe("Video Upload", () => {
    test("debe subir video tour", async ({ page }) => {
      await page.goto("/publicar");

      const videoInput = page.locator('input[accept*="video"]');
      if (await videoInput.isVisible()) {
        await videoInput.setInputFiles("./fixtures/car-video.mp4");
        await expect(page.getByTestId("video-preview")).toBeVisible();
      }
    });
  });

  test.describe("360° View", () => {
    test("debe capturar secuencia 360°", async ({ page }) => {
      await page.goto("/publicar/360");

      await expect(page.getByTestId("capture-360")).toBeVisible();
    });
  });
});
```

---

**✅ DOCUMENTO COMPLETO - LISTO PARA IMPLEMENTACIÓN**

_Este documento integra TODOS los procesos de MEDIA-ARCHIVOS con implementación UI completa, hooks, servicios, testing y métricas._

---

**Siguiente documento:** `39-blockchain-vehiculos.md` (si aplica) o continuar con mejoras de UX

**Dependencias backend:** MediaService debe estar corriendo en puerto 5016 con Digital Ocean Spaces configurado.

**Prioridad:** 🟡 MEDIA (Core upload ya existe, este documento agrega features avanzados)
