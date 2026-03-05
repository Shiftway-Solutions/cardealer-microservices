# 🔄 Componente Vehicle360Viewer

> **Tiempo estimado:** 90 minutos
> **Prerrequisitos:** Framer Motion, hooks de API, MediaService
> **Referencia Backend:** Vehicle360ProcessingService, Video360Service, BackgroundRemovalService

---

## 📋 OBJETIVO

Implementar el sistema completo de visualización 360° de vehículos:

- Visor interactivo con drag/touch para rotar
- Uploader de video para dealers
- Hook para gestión de estado y procesamiento
- Estados: Loading, Processing, Ready, Error

---

## 🎨 ARQUITECTURA DEL SISTEMA 360°

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                            FLUJO DE PROCESAMIENTO                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1. DEALER SUBE VIDEO                                                       │
│     ├── Archivo MP4/MOV/AVI/WebM (máx 500MB, 60 segundos)                  │
│     └── Video del vehículo girando 360°                                    │
│                                                                             │
│  2. BACKEND PROCESA                                                         │
│     ├── Vehicle360ProcessingService (Orquestador)                          │
│     ├── Video360Service → Extrae 6 frames (0°, 60°, 120°, 180°, 240°, 300°)│
│     ├── BackgroundRemovalService → Remueve fondos de cada frame           │
│     └── MediaService → Almacena en S3/CDN                                  │
│                                                                             │
│  3. FRONTEND MUESTRA                                                        │
│     ├── Vehicle360Viewer → Visor interactivo                               │
│     ├── Drag to rotate / Touch support                                     │
│     ├── Auto-rotate, Zoom, Fullscreen                                      │
│     └── Thumbnails de los 6 ángulos                                        │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔧 PASO 1: Types y Interfaces

```typescript
// filepath: src/types/vehicle360.ts

/** Frame individual de la vista 360° */
export interface Vehicle360Frame {
  index: number;
  angle: number;
  name: string;
  imageUrl: string;
  thumbnailUrl: string;
  hasTransparentBackground: boolean;
}

/** Vista 360° completa de un vehículo */
export interface Vehicle360View {
  vehicleId: string;
  isReady: boolean;
  totalFrames: number;
  primaryImageUrl: string;
  frames: Vehicle360Frame[];
  config: Vehicle360Config;
  createdAt: string;
  updatedAt: string;
}

/** Configuración del visor 360° */
export interface Vehicle360Config {
  autoRotate: boolean;
  autoRotateSpeed: number;
  allowDrag: boolean;
  allowZoom: boolean;
  showThumbnails: boolean;
  showAngleIndicator: boolean;
  preloadAll: boolean;
  hasTransparentBackground: boolean;
}

/** Estado de un job de procesamiento */
export type ProcessingStatus =
  | "Pending"
  | "Queued"
  | "Uploading"
  | "VideoUploaded"
  | "ExtractingFrames"
  | "FramesExtracted"
  | "RemovingBackgrounds"
  | "Completed"
  | "Failed";

/** Job de procesamiento 360° */
export interface ProcessingJob {
  jobId: string;
  vehicleId: string;
  status: ProcessingStatus;
  progress: number;
  currentStep: string;
  errorMessage?: string;
  estimatedTimeRemaining?: number;
  createdAt: string;
  updatedAt: string;
}

/** Request para iniciar procesamiento */
export interface StartProcessingRequest {
  vehicleId: string;
  videoUrl?: string;
  options?: {
    removeBackground?: boolean;
    video360Provider?:
      | "ApyHub"
      | "FfmpegApi"
      | "Cloudinary"
      | "Imgix"
      | "Shotstack";
    backgroundRemovalProvider?:
      | "ClipDrop"
      | "RemoveBg"
      | "Photoroom"
      | "Slazzer";
  };
}

/** Response del procesamiento */
export interface StartProcessingResponse {
  jobId: string;
  status: ProcessingStatus;
  queuePosition: number;
  estimatedWaitSeconds: number;
}
```

---

## 🔧 PASO 2: Service de API

```typescript
// filepath: src/lib/services/vehicle360Service.ts

import { api } from "@/lib/api";
import type {
  Vehicle360View,
  ProcessingJob,
  StartProcessingRequest,
  StartProcessingResponse,
} from "@/types/vehicle360";

const BASE_URL = "/api/vehicle360processing";

export const vehicle360Service = {
  /**
   * Obtiene la vista 360° de un vehículo
   */
  async getView(vehicleId: string): Promise<Vehicle360View | null> {
    try {
      const response = await api.get<Vehicle360View>(
        `${BASE_URL}/viewer/${vehicleId}`,
      );
      return response.data;
    } catch (error: any) {
      if (error.response?.status === 404) {
        return null;
      }
      throw error;
    }
  },

  /**
   * Verifica si un vehículo tiene vista 360° disponible
   */
  async hasView(vehicleId: string): Promise<boolean> {
    try {
      const response = await api.head(`${BASE_URL}/viewer/${vehicleId}`);
      return response.status === 200;
    } catch {
      return false;
    }
  },

  /**
   * Inicia el procesamiento de un video 360°
   */
  async startProcessing(
    request: StartProcessingRequest,
  ): Promise<StartProcessingResponse> {
    const response = await api.post<StartProcessingResponse>(
      `${BASE_URL}/process`,
      request,
    );
    return response.data;
  },

  /**
   * Sube un video y comienza el procesamiento
   */
  async uploadAndProcess(
    vehicleId: string,
    videoFile: File,
    options?: StartProcessingRequest["options"],
  ): Promise<StartProcessingResponse> {
    const formData = new FormData();
    formData.append("video", videoFile);
    formData.append("vehicleId", vehicleId);

    if (options?.removeBackground !== undefined) {
      formData.append("removeBackground", String(options.removeBackground));
    }

    const response = await api.post<StartProcessingResponse>(
      `${BASE_URL}/upload-and-process`,
      formData,
      {
        headers: { "Content-Type": "multipart/form-data" },
        timeout: 120000, // 2 minutos para upload
      },
    );
    return response.data;
  },

  /**
   * Obtiene el estado de un job de procesamiento
   */
  async getJobStatus(jobId: string): Promise<ProcessingJob> {
    const response = await api.get<ProcessingJob>(`${BASE_URL}/jobs/${jobId}`);
    return response.data;
  },

  /**
   * Cancela un job de procesamiento
   */
  async cancelJob(jobId: string): Promise<void> {
    await api.delete(`${BASE_URL}/jobs/${jobId}`);
  },

  /**
   * Elimina la vista 360° de un vehículo
   */
  async deleteView(vehicleId: string): Promise<void> {
    await api.delete(`${BASE_URL}/viewer/${vehicleId}`);
  },
};
```

---

## 🔧 PASO 3: Hook useVehicle360

```typescript
// filepath: src/lib/hooks/useVehicle360.ts

"use client";

import * as React from "react";
import { vehicle360Service } from "@/lib/services/vehicle360Service";
import type {
  Vehicle360View,
  ProcessingJob,
  StartProcessingRequest,
} from "@/types/vehicle360";

interface UseVehicle360Options {
  /** Cargar automáticamente al montar */
  autoFetch?: boolean;
  /** Intervalo de polling para jobs (ms) */
  pollInterval?: number;
}

interface UseVehicle360Return {
  /** Vista 360° del vehículo */
  view: Vehicle360View | null;
  /** Job de procesamiento actual */
  job: ProcessingJob | null;
  /** Está cargando la vista */
  isLoading: boolean;
  /** Está procesando un video */
  isProcessing: boolean;
  /** Progreso del procesamiento (0-100) */
  progress: number;
  /** Mensaje de error */
  error: string | null;
  /** Iniciar procesamiento con URL de video */
  startProcessing: (
    videoUrl: string,
    options?: StartProcessingRequest["options"],
  ) => Promise<void>;
  /** Subir video y procesar */
  uploadAndProcess: (videoFile: File) => Promise<void>;
  /** Cancelar procesamiento */
  cancelProcessing: () => Promise<void>;
  /** Recargar vista */
  refetch: () => Promise<void>;
}

export function useVehicle360(
  vehicleId: string,
  options: UseVehicle360Options = {},
): UseVehicle360Return {
  const { autoFetch = true, pollInterval = 2000 } = options;

  const [view, setView] = React.useState<Vehicle360View | null>(null);
  const [job, setJob] = React.useState<ProcessingJob | null>(null);
  const [isLoading, setIsLoading] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);

  const isProcessing =
    job?.status === "Pending" ||
    job?.status === "Queued" ||
    job?.status === "Uploading" ||
    job?.status === "ExtractingFrames" ||
    job?.status === "RemovingBackgrounds";

  const progress = job?.progress ?? 0;

  // Fetch vista existente
  const fetchView = React.useCallback(async () => {
    if (!vehicleId) return;

    setIsLoading(true);
    setError(null);

    try {
      const data = await vehicle360Service.getView(vehicleId);
      setView(data);
    } catch (err) {
      setError("Error al cargar la vista 360°");
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  }, [vehicleId]);

  // Iniciar procesamiento con URL
  const startProcessing = React.useCallback(
    async (
      videoUrl: string,
      processingOptions?: StartProcessingRequest["options"],
    ) => {
      if (!vehicleId) return;

      setError(null);

      try {
        const newJob = await vehicle360Service.startProcessing({
          vehicleId,
          videoUrl,
          options: {
            removeBackground: true,
            video360Provider: "FfmpegApi",
            backgroundRemovalProvider: "ClipDrop",
            ...processingOptions,
          },
        });
        setJob({
          jobId: newJob.jobId,
          vehicleId,
          status: newJob.status,
          progress: 0,
          currentStep: "Iniciando...",
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
        });
      } catch (err) {
        setError("Error al iniciar el procesamiento");
        console.error(err);
      }
    },
    [vehicleId],
  );

  // Subir video y procesar
  const uploadAndProcess = React.useCallback(
    async (videoFile: File) => {
      if (!vehicleId) return;

      setError(null);

      try {
        const newJob = await vehicle360Service.uploadAndProcess(
          vehicleId,
          videoFile,
          { removeBackground: true },
        );
        setJob({
          jobId: newJob.jobId,
          vehicleId,
          status: newJob.status,
          progress: 0,
          currentStep: "Subiendo video...",
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
        });
      } catch (err) {
        setError("Error al subir y procesar el video");
        console.error(err);
      }
    },
    [vehicleId],
  );

  // Cancelar procesamiento
  const cancelProcessing = React.useCallback(async () => {
    if (!job?.jobId) return;

    try {
      await vehicle360Service.cancelJob(job.jobId);
      setJob(null);
    } catch (err) {
      setError("Error al cancelar el procesamiento");
      console.error(err);
    }
  }, [job?.jobId]);

  // Auto-fetch al montar
  React.useEffect(() => {
    if (autoFetch) {
      fetchView();
    }
  }, [autoFetch, fetchView]);

  // Poll job status mientras está procesando
  React.useEffect(() => {
    if (!job || !isProcessing) return;

    const intervalId = setInterval(async () => {
      try {
        const updatedJob = await vehicle360Service.getJobStatus(job.jobId);
        setJob(updatedJob);

        if (updatedJob.status === "Completed") {
          // Refetch la vista cuando termine
          await fetchView();
        } else if (updatedJob.status === "Failed") {
          setError(updatedJob.errorMessage || "Error en el procesamiento");
        }
      } catch (err) {
        console.error("Error polling job status:", err);
      }
    }, pollInterval);

    return () => clearInterval(intervalId);
  }, [job, isProcessing, pollInterval, fetchView]);

  return {
    view,
    job,
    isLoading,
    isProcessing,
    progress,
    error,
    startProcessing,
    uploadAndProcess,
    cancelProcessing,
    refetch: fetchView,
  };
}
```

---

## 🔧 PASO 4: Componente Vehicle360Viewer

```typescript
// filepath: src/components/vehicles/Vehicle360Viewer.tsx

"use client";

import * as React from "react";
import Image from "next/image";
import { motion, AnimatePresence } from "framer-motion";
import {
  Play,
  Pause,
  ZoomIn,
  ZoomOut,
  Maximize2,
  Minimize2,
  RotateCcw,
  Hand,
  Loader2,
  AlertCircle,
  Camera,
} from "lucide-react";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";
import { useVehicle360 } from "@/lib/hooks/useVehicle360";
import type { Vehicle360Frame } from "@/types/vehicle360";

interface Vehicle360ViewerProps {
  vehicleId: string;
  className?: string;
  /** Mostrar thumbnails de los frames */
  showThumbnails?: boolean;
  /** Mostrar controles de reproducción */
  showControls?: boolean;
  /** Frame inicial (0-5) */
  initialFrame?: number;
  /** Callback cuando cambia el frame */
  onFrameChange?: (frame: Vehicle360Frame) => void;
  /** Altura del visor */
  height?: number | string;
}

export function Vehicle360Viewer({
  vehicleId,
  className,
  showThumbnails = true,
  showControls = true,
  initialFrame = 0,
  onFrameChange,
  height = 400,
}: Vehicle360ViewerProps) {
  const { view, isLoading, error } = useVehicle360(vehicleId);

  const [currentFrameIndex, setCurrentFrameIndex] = React.useState(initialFrame);
  const [isAutoRotating, setIsAutoRotating] = React.useState(false);
  const [isDragging, setIsDragging] = React.useState(false);
  const [zoomLevel, setZoomLevel] = React.useState(1);
  const [isFullscreen, setIsFullscreen] = React.useState(false);
  const [imagesLoaded, setImagesLoaded] = React.useState(false);

  const containerRef = React.useRef<HTMLDivElement>(null);
  const dragStartX = React.useRef(0);
  const lastFrameIndex = React.useRef(0);

  // Preload all images
  React.useEffect(() => {
    if (!view?.frames.length) return;

    let loadedCount = 0;
    const totalImages = view.frames.length;

    view.frames.forEach((frame) => {
      const img = new window.Image();
      img.src = frame.imageUrl;
      img.onload = () => {
        loadedCount++;
        if (loadedCount === totalImages) {
          setImagesLoaded(true);
        }
      };
    });
  }, [view]);

  // Auto-rotate
  React.useEffect(() => {
    if (!isAutoRotating || !view) return;

    const speed = view.config.autoRotateSpeed || 3000;
    const interval = setInterval(() => {
      setCurrentFrameIndex((prev) => (prev + 1) % view.frames.length);
    }, speed / view.frames.length);

    return () => clearInterval(interval);
  }, [isAutoRotating, view]);

  // Notify on frame change
  React.useEffect(() => {
    if (view && onFrameChange) {
      onFrameChange(view.frames[currentFrameIndex]);
    }
  }, [currentFrameIndex, view, onFrameChange]);

  // Drag handlers (mouse)
  const handleMouseDown = React.useCallback(
    (e: React.MouseEvent) => {
      if (!view?.config.allowDrag) return;

      setIsDragging(true);
      setIsAutoRotating(false);
      dragStartX.current = e.clientX;
      lastFrameIndex.current = currentFrameIndex;
    },
    [view, currentFrameIndex]
  );

  const handleMouseMove = React.useCallback(
    (e: React.MouseEvent) => {
      if (!isDragging || !view) return;

      const deltaX = e.clientX - dragStartX.current;
      const frameWidth = containerRef.current?.clientWidth || 400;
      const sensitivity = 0.5; // Ajustar sensibilidad
      const framesPerPixel = (view.frames.length / frameWidth) * sensitivity;
      const frameDelta = Math.round(deltaX * framesPerPixel);

      let newIndex = (lastFrameIndex.current + frameDelta) % view.frames.length;
      if (newIndex < 0) newIndex += view.frames.length;

      setCurrentFrameIndex(newIndex);
    },
    [isDragging, view]
  );

  const handleMouseUp = React.useCallback(() => {
    setIsDragging(false);
  }, []);

  // Touch handlers (mobile)
  const handleTouchStart = React.useCallback(
    (e: React.TouchEvent) => {
      if (!view?.config.allowDrag) return;

      setIsDragging(true);
      setIsAutoRotating(false);
      dragStartX.current = e.touches[0].clientX;
      lastFrameIndex.current = currentFrameIndex;
    },
    [view, currentFrameIndex]
  );

  const handleTouchMove = React.useCallback(
    (e: React.TouchEvent) => {
      if (!isDragging || !view) return;

      const deltaX = e.touches[0].clientX - dragStartX.current;
      const frameWidth = containerRef.current?.clientWidth || 400;
      const sensitivity = 0.5;
      const framesPerPixel = (view.frames.length / frameWidth) * sensitivity;
      const frameDelta = Math.round(deltaX * framesPerPixel);

      let newIndex = (lastFrameIndex.current + frameDelta) % view.frames.length;
      if (newIndex < 0) newIndex += view.frames.length;

      setCurrentFrameIndex(newIndex);
    },
    [isDragging, view]
  );

  // Zoom handlers
  const handleZoomIn = () => {
    if (view?.config.allowZoom) {
      setZoomLevel((prev) => Math.min(prev + 0.25, 3));
    }
  };

  const handleZoomOut = () => {
    if (view?.config.allowZoom) {
      setZoomLevel((prev) => Math.max(prev - 0.25, 1));
    }
  };

  const handleResetZoom = () => {
    setZoomLevel(1);
  };

  // Fullscreen
  const toggleFullscreen = async () => {
    if (!containerRef.current) return;

    if (!isFullscreen) {
      await containerRef.current.requestFullscreen?.();
      setIsFullscreen(true);
    } else {
      await document.exitFullscreen?.();
      setIsFullscreen(false);
    }
  };

  // Listen for fullscreen change
  React.useEffect(() => {
    const handleFullscreenChange = () => {
      setIsFullscreen(!!document.fullscreenElement);
    };

    document.addEventListener("fullscreenchange", handleFullscreenChange);
    return () => {
      document.removeEventListener("fullscreenchange", handleFullscreenChange);
    };
  }, []);

  // Loading state
  if (isLoading) {
    return (
      <div
        className={cn(
          "flex flex-col items-center justify-center bg-gray-100 rounded-xl",
          className
        )}
        style={{ height }}
      >
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
        <p className="mt-2 text-sm text-gray-500">Cargando vista 360°...</p>
      </div>
    );
  }

  // Error state
  if (error) {
    return (
      <div
        className={cn(
          "flex flex-col items-center justify-center bg-red-50 rounded-xl",
          className
        )}
        style={{ height }}
      >
        <AlertCircle className="h-8 w-8 text-red-500" />
        <p className="mt-2 text-sm text-red-600">{error}</p>
      </div>
    );
  }

  // No view available
  if (!view || !view.isReady) {
    return (
      <div
        className={cn(
          "flex flex-col items-center justify-center bg-gray-50 rounded-xl border-2 border-dashed border-gray-200",
          className
        )}
        style={{ height }}
      >
        <Camera className="h-12 w-12 text-gray-300" />
        <p className="mt-2 text-sm text-gray-500">
          Vista 360° no disponible
        </p>
      </div>
    );
  }

  const currentFrame = view.frames[currentFrameIndex];

  return (
    <div
      ref={containerRef}
      className={cn(
        "relative bg-gradient-to-b from-gray-100 to-gray-200 rounded-xl overflow-hidden select-none",
        isFullscreen && "fixed inset-0 z-50 rounded-none",
        className
      )}
      style={{ height: isFullscreen ? "100vh" : height }}
    >
      {/* Main image container */}
      <div
        className="relative w-full h-full flex items-center justify-center"
        onMouseDown={handleMouseDown}
        onMouseMove={handleMouseMove}
        onMouseUp={handleMouseUp}
        onMouseLeave={handleMouseUp}
        onTouchStart={handleTouchStart}
        onTouchMove={handleTouchMove}
        onTouchEnd={handleMouseUp}
        style={{ cursor: isDragging ? "grabbing" : "grab" }}
      >
        {/* Preloader */}
        {!imagesLoaded && (
          <div className="absolute inset-0 flex items-center justify-center bg-gray-100">
            <Loader2 className="h-6 w-6 animate-spin text-primary" />
          </div>
        )}

        {/* Current frame image */}
        <AnimatePresence mode="wait">
          <motion.div
            key={currentFrameIndex}
            initial={{ opacity: 0.8 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0.8 }}
            transition={{ duration: 0.1 }}
            className="relative w-full h-full"
          >
            <Image
              src={currentFrame.imageUrl}
              alt={`Vista 360° - ${currentFrame.name}`}
              fill
              className="object-contain transition-transform duration-100"
              style={{ transform: `scale(${zoomLevel})` }}
              draggable={false}
              priority
            />
          </motion.div>
        </AnimatePresence>

        {/* Angle indicator */}
        {view.config.showAngleIndicator && (
          <div className="absolute top-4 left-4 bg-black/60 text-white text-xs px-3 py-1.5 rounded-full backdrop-blur-sm">
            {currentFrame.angle}° - {currentFrame.name}
          </div>
        )}

        {/* Drag hint */}
        {!isDragging && !isAutoRotating && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            className="absolute bottom-20 left-1/2 -translate-x-1/2 flex items-center gap-2 bg-black/60 text-white text-xs px-4 py-2 rounded-full backdrop-blur-sm"
          >
            <Hand className="h-4 w-4" />
            <span>Arrastra para rotar</span>
          </motion.div>
        )}
      </div>

      {/* Controls */}
      {showControls && (
        <div className="absolute bottom-4 left-1/2 -translate-x-1/2 flex items-center gap-2 bg-white/90 backdrop-blur-sm rounded-full px-4 py-2 shadow-lg">
          {/* Frame dots */}
          <div className="flex items-center gap-1.5 mr-2 pr-2 border-r">
            {view.frames.map((frame, index) => (
              <button
                key={index}
                className={cn(
                  "w-2.5 h-2.5 rounded-full transition-all",
                  index === currentFrameIndex
                    ? "bg-primary scale-125"
                    : "bg-gray-300 hover:bg-gray-400"
                )}
                onClick={() => {
                  setCurrentFrameIndex(index);
                  setIsAutoRotating(false);
                }}
                aria-label={`Ver ${frame.name}`}
              />
            ))}
          </div>

          {/* Play/Pause */}
          <Button
            variant="ghost"
            size="sm"
            className="h-8 w-8 p-0"
            onClick={() => setIsAutoRotating(!isAutoRotating)}
            aria-label={isAutoRotating ? "Pausar" : "Reproducir"}
          >
            {isAutoRotating ? (
              <Pause className="h-4 w-4" />
            ) : (
              <Play className="h-4 w-4" />
            )}
          </Button>

          {/* Zoom controls */}
          {view.config.allowZoom && (
            <>
              <Button
                variant="ghost"
                size="sm"
                className="h-8 w-8 p-0"
                onClick={handleZoomOut}
                disabled={zoomLevel <= 1}
                aria-label="Alejar"
              >
                <ZoomOut className="h-4 w-4" />
              </Button>

              {zoomLevel > 1 && (
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-8 w-8 p-0"
                  onClick={handleResetZoom}
                  aria-label="Restablecer zoom"
                >
                  <RotateCcw className="h-4 w-4" />
                </Button>
              )}

              <Button
                variant="ghost"
                size="sm"
                className="h-8 w-8 p-0"
                onClick={handleZoomIn}
                disabled={zoomLevel >= 3}
                aria-label="Acercar"
              >
                <ZoomIn className="h-4 w-4" />
              </Button>
            </>
          )}

          {/* Fullscreen */}
          <Button
            variant="ghost"
            size="sm"
            className="h-8 w-8 p-0"
            onClick={toggleFullscreen}
            aria-label={isFullscreen ? "Salir de pantalla completa" : "Pantalla completa"}
          >
            {isFullscreen ? (
              <Minimize2 className="h-4 w-4" />
            ) : (
              <Maximize2 className="h-4 w-4" />
            )}
          </Button>
        </div>
      )}

      {/* Thumbnails */}
      {showThumbnails && (
        <div className="absolute top-4 right-4 flex flex-col gap-1.5">
          {view.frames.map((frame, index) => (
            <button
              key={index}
              className={cn(
                "relative w-12 h-12 rounded-lg overflow-hidden border-2 transition-all",
                index === currentFrameIndex
                  ? "border-primary ring-2 ring-primary/30"
                  : "border-white/50 hover:border-white"
              )}
              onClick={() => {
                setCurrentFrameIndex(index);
                setIsAutoRotating(false);
              }}
            >
              <Image
                src={frame.thumbnailUrl || frame.imageUrl}
                alt={frame.name}
                fill
                className="object-cover"
              />
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
```

---

## 🔧 PASO 5: Componente Vehicle360Uploader (Para Dealers)

```typescript
// filepath: src/components/vehicles/Vehicle360Uploader.tsx

"use client";

import * as React from "react";
import { motion, AnimatePresence } from "framer-motion";
import {
  Upload,
  Video,
  CheckCircle,
  AlertCircle,
  Loader2,
  X,
  RefreshCw,
  Trash2,
  Play,
} from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Progress } from "@/components/ui/Progress";
import { cn, formatFileSize } from "@/lib/utils";
import { useVehicle360 } from "@/lib/hooks/useVehicle360";
import { Vehicle360Viewer } from "./Vehicle360Viewer";

interface Vehicle360UploaderProps {
  vehicleId: string;
  className?: string;
  /** Callback cuando se completa el procesamiento */
  onComplete?: () => void;
  /** Callback cuando hay error */
  onError?: (error: string) => void;
}

const ACCEPTED_FORMATS = ["video/mp4", "video/quicktime", "video/avi", "video/webm"];
const MAX_FILE_SIZE = 500 * 1024 * 1024; // 500MB
const MAX_DURATION = 60; // 60 segundos

export function Vehicle360Uploader({
  vehicleId,
  className,
  onComplete,
  onError,
}: Vehicle360UploaderProps) {
  const {
    view,
    job,
    isLoading,
    isProcessing,
    progress,
    error,
    uploadAndProcess,
    cancelProcessing,
    refetch,
  } = useVehicle360(vehicleId);

  const [selectedFile, setSelectedFile] = React.useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = React.useState<string | null>(null);
  const [validationError, setValidationError] = React.useState<string | null>(null);
  const [isDragOver, setIsDragOver] = React.useState(false);

  const inputRef = React.useRef<HTMLInputElement>(null);

  // Cleanup preview URL on unmount
  React.useEffect(() => {
    return () => {
      if (previewUrl) {
        URL.revokeObjectURL(previewUrl);
      }
    };
  }, [previewUrl]);

  // Handle completion
  React.useEffect(() => {
    if (view?.isReady && onComplete) {
      onComplete();
    }
  }, [view?.isReady, onComplete]);

  // Handle errors
  React.useEffect(() => {
    if (error && onError) {
      onError(error);
    }
  }, [error, onError]);

  const validateFile = async (file: File): Promise<string | null> => {
    // Check format
    if (!ACCEPTED_FORMATS.includes(file.type)) {
      return "Formato no soportado. Usa MP4, MOV, AVI o WebM.";
    }

    // Check size
    if (file.size > MAX_FILE_SIZE) {
      return `El archivo es muy grande. Máximo ${formatFileSize(MAX_FILE_SIZE)}.`;
    }

    // Check duration (create temp video element)
    return new Promise((resolve) => {
      const video = document.createElement("video");
      video.preload = "metadata";

      video.onloadedmetadata = () => {
        URL.revokeObjectURL(video.src);
        if (video.duration > MAX_DURATION) {
          resolve(`El video es muy largo. Máximo ${MAX_DURATION} segundos.`);
        } else {
          resolve(null);
        }
      };

      video.onerror = () => {
        URL.revokeObjectURL(video.src);
        resolve("No se pudo leer el video. Verifica que no esté corrupto.");
      };

      video.src = URL.createObjectURL(file);
    });
  };

  const handleFileSelect = async (file: File) => {
    setValidationError(null);

    const validationResult = await validateFile(file);
    if (validationResult) {
      setValidationError(validationResult);
      return;
    }

    setSelectedFile(file);
    setPreviewUrl(URL.createObjectURL(file));
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      handleFileSelect(file);
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(false);

    const file = e.dataTransfer.files?.[0];
    if (file) {
      handleFileSelect(file);
    }
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(true);
  };

  const handleDragLeave = () => {
    setIsDragOver(false);
  };

  const handleUpload = async () => {
    if (!selectedFile) return;
    await uploadAndProcess(selectedFile);
  };

  const handleClear = () => {
    setSelectedFile(null);
    if (previewUrl) {
      URL.revokeObjectURL(previewUrl);
      setPreviewUrl(null);
    }
    setValidationError(null);
    if (inputRef.current) {
      inputRef.current.value = "";
    }
  };

  const getStatusMessage = (): string => {
    if (!job) return "";

    switch (job.status) {
      case "Pending":
      case "Queued":
        return "En cola de procesamiento...";
      case "Uploading":
        return "Subiendo video...";
      case "VideoUploaded":
        return "Video subido, iniciando procesamiento...";
      case "ExtractingFrames":
        return "Extrayendo frames del video...";
      case "FramesExtracted":
        return "Frames extraídos, removiendo fondos...";
      case "RemovingBackgrounds":
        return `Procesando imágenes (${progress}%)...`;
      case "Completed":
        return "¡Procesamiento completado!";
      case "Failed":
        return job.errorMessage || "Error en el procesamiento";
      default:
        return "Procesando...";
    }
  };

  // Si ya tiene vista 360° disponible
  if (view?.isReady && !isProcessing) {
    return (
      <div className={cn("space-y-4", className)}>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2 text-green-600">
            <CheckCircle className="h-5 w-5" />
            <span className="font-medium">Vista 360° disponible</span>
          </div>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => inputRef.current?.click()}
            >
              <RefreshCw className="h-4 w-4 mr-2" />
              Reemplazar
            </Button>
          </div>
        </div>

        <Vehicle360Viewer vehicleId={vehicleId} height={300} />

        <input
          ref={inputRef}
          type="file"
          accept="video/*"
          className="hidden"
          onChange={handleInputChange}
        />
      </div>
    );
  }

  // Si está procesando
  if (isProcessing) {
    return (
      <div className={cn("space-y-4", className)}>
        <div className="bg-blue-50 border border-blue-200 rounded-xl p-6">
          <div className="flex items-center gap-3 mb-4">
            <Loader2 className="h-5 w-5 animate-spin text-blue-600" />
            <span className="font-medium text-blue-900">
              Procesando video 360°
            </span>
          </div>

          <Progress value={progress} className="h-2 mb-2" />

          <div className="flex items-center justify-between text-sm">
            <span className="text-blue-700">{getStatusMessage()}</span>
            <span className="text-blue-600 font-medium">{progress}%</span>
          </div>

          <p className="text-xs text-blue-600 mt-4">
            Esto puede tomar 2-5 minutos. Puedes continuar editando mientras procesamos.
          </p>

          <Button
            variant="outline"
            size="sm"
            onClick={cancelProcessing}
            className="mt-4"
          >
            <X className="h-4 w-4 mr-2" />
            Cancelar
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className={cn("space-y-4", className)}>
      {/* Dropzone */}
      {!selectedFile && (
        <div
          className={cn(
            "relative border-2 border-dashed rounded-xl p-8 text-center transition-colors",
            isDragOver
              ? "border-primary bg-primary/5"
              : "border-gray-200 hover:border-gray-300",
            validationError && "border-red-300 bg-red-50"
          )}
          onDrop={handleDrop}
          onDragOver={handleDragOver}
          onDragLeave={handleDragLeave}
        >
          <input
            ref={inputRef}
            type="file"
            accept="video/*"
            className="hidden"
            onChange={handleInputChange}
          />

          <Video className="h-12 w-12 mx-auto text-gray-400 mb-4" />

          <h3 className="font-semibold text-gray-900 mb-1">
            Subir Video 360°
          </h3>

          <p className="text-sm text-gray-500 mb-4">
            Arrastra un video aquí o haz clic para seleccionar
          </p>

          <Button onClick={() => inputRef.current?.click()}>
            <Upload className="h-4 w-4 mr-2" />
            Seleccionar Video
          </Button>

          <p className="text-xs text-gray-400 mt-4">
            MP4, MOV, AVI, WebM • Máx. 500MB • Máx. 60 segundos
          </p>

          {validationError && (
            <div className="mt-4 flex items-center justify-center gap-2 text-red-600">
              <AlertCircle className="h-4 w-4" />
              <span className="text-sm">{validationError}</span>
            </div>
          )}
        </div>
      )}

      {/* Preview */}
      {selectedFile && previewUrl && !isProcessing && (
        <div className="space-y-4">
          <div className="relative rounded-xl overflow-hidden bg-black">
            <video
              src={previewUrl}
              controls
              className="w-full max-h-[300px] object-contain"
            />

            <button
              className="absolute top-2 right-2 bg-black/60 text-white p-1.5 rounded-full hover:bg-black/80"
              onClick={handleClear}
            >
              <X className="h-4 w-4" />
            </button>
          </div>

          <div className="flex items-center justify-between bg-gray-50 rounded-lg p-3">
            <div className="flex items-center gap-3">
              <Video className="h-5 w-5 text-gray-400" />
              <div>
                <p className="font-medium text-sm text-gray-900">
                  {selectedFile.name}
                </p>
                <p className="text-xs text-gray-500">
                  {formatFileSize(selectedFile.size)}
                </p>
              </div>
            </div>

            <div className="flex gap-2">
              <Button variant="outline" size="sm" onClick={handleClear}>
                <Trash2 className="h-4 w-4 mr-2" />
                Eliminar
              </Button>
              <Button size="sm" onClick={handleUpload}>
                <Play className="h-4 w-4 mr-2" />
                Procesar Video
              </Button>
            </div>
          </div>

          <div className="bg-blue-50 border border-blue-100 rounded-lg p-4">
            <h4 className="font-medium text-blue-900 mb-2">
              ¿Qué sucederá?
            </h4>
            <ul className="text-sm text-blue-700 space-y-1">
              <li>• Extraeremos 6 fotos del video (cada 60°)</li>
              <li>• Removeremos el fondo de cada imagen</li>
              <li>• Crearemos una vista 360° interactiva</li>
              <li>• Tiempo estimado: 2-5 minutos</li>
            </ul>
          </div>
        </div>
      )}

      {/* Error */}
      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 flex items-start gap-3">
          <AlertCircle className="h-5 w-5 text-red-500 flex-shrink-0 mt-0.5" />
          <div>
            <p className="font-medium text-red-900">Error en el procesamiento</p>
            <p className="text-sm text-red-700">{error}</p>
            <Button
              variant="outline"
              size="sm"
              onClick={refetch}
              className="mt-2"
            >
              <RefreshCw className="h-4 w-4 mr-2" />
              Reintentar
            </Button>
          </div>
        </div>
      )}

      {/* Tips */}
      <div className="bg-gray-50 rounded-lg p-4">
        <h4 className="font-medium text-gray-900 mb-2">
          💡 Tips para un buen video 360°
        </h4>
        <ul className="text-sm text-gray-600 space-y-1">
          <li>• Graba con el vehículo sobre una plataforma giratoria</li>
          <li>• Usa buena iluminación, preferiblemente exterior</li>
          <li>• Mantén la cámara estable y a la altura del vehículo</li>
          <li>• Una vuelta completa y lenta (15-30 segundos)</li>
          <li>• Fondo limpio para mejor remoción automática</li>
        </ul>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 6: Interior360Viewer (Vista Interior)

```typescript
// filepath: src/components/vehicles/Interior360Viewer.tsx

"use client";

import * as React from "react";
import Image from "next/image";
import { motion } from "framer-motion";
import {
  Loader2,
  AlertCircle,
  ChevronLeft,
  ChevronRight,
  Maximize2,
  Eye,
} from "lucide-react";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

interface InteriorHotspot {
  id: string;
  x: number;
  y: number;
  label: string;
  description?: string;
}

interface InteriorFrame {
  id: string;
  name: string;
  imageUrl: string;
  hotspots?: InteriorHotspot[];
}

interface Interior360ViewerProps {
  vehicleId: string;
  className?: string;
  frames?: InteriorFrame[];
  isLoading?: boolean;
  error?: string | null;
}

// Frames predefinidos del interior
const DEFAULT_INTERIOR_FRAMES: InteriorFrame[] = [
  { id: "dashboard", name: "Tablero", imageUrl: "" },
  { id: "front-seats", name: "Asientos Delanteros", imageUrl: "" },
  { id: "rear-seats", name: "Asientos Traseros", imageUrl: "" },
  { id: "trunk", name: "Maletero", imageUrl: "" },
  { id: "center-console", name: "Consola Central", imageUrl: "" },
];

export function Interior360Viewer({
  vehicleId,
  className,
  frames = DEFAULT_INTERIOR_FRAMES,
  isLoading = false,
  error = null,
}: Interior360ViewerProps) {
  const [currentIndex, setCurrentIndex] = React.useState(0);
  const [activeHotspot, setActiveHotspot] = React.useState<InteriorHotspot | null>(null);
  const [isFullscreen, setIsFullscreen] = React.useState(false);

  const containerRef = React.useRef<HTMLDivElement>(null);

  const currentFrame = frames[currentIndex];
  const hasFrames = frames.length > 0 && frames[0].imageUrl;

  const goToPrevious = () => {
    setCurrentIndex((prev) => (prev === 0 ? frames.length - 1 : prev - 1));
    setActiveHotspot(null);
  };

  const goToNext = () => {
    setCurrentIndex((prev) => (prev === frames.length - 1 ? 0 : prev + 1));
    setActiveHotspot(null);
  };

  const toggleFullscreen = async () => {
    if (!containerRef.current) return;

    if (!isFullscreen) {
      await containerRef.current.requestFullscreen?.();
      setIsFullscreen(true);
    } else {
      await document.exitFullscreen?.();
      setIsFullscreen(false);
    }
  };

  if (isLoading) {
    return (
      <div
        className={cn(
          "flex flex-col items-center justify-center bg-gray-100 rounded-xl h-[300px]",
          className
        )}
      >
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
        <p className="mt-2 text-sm text-gray-500">Cargando interior...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div
        className={cn(
          "flex flex-col items-center justify-center bg-red-50 rounded-xl h-[300px]",
          className
        )}
      >
        <AlertCircle className="h-8 w-8 text-red-500" />
        <p className="mt-2 text-sm text-red-600">{error}</p>
      </div>
    );
  }

  if (!hasFrames) {
    return (
      <div
        className={cn(
          "flex flex-col items-center justify-center bg-gray-50 rounded-xl border-2 border-dashed border-gray-200 h-[300px]",
          className
        )}
      >
        <Eye className="h-12 w-12 text-gray-300" />
        <p className="mt-2 text-sm text-gray-500">
          Vista interior no disponible
        </p>
      </div>
    );
  }

  return (
    <div
      ref={containerRef}
      className={cn(
        "relative bg-gray-900 rounded-xl overflow-hidden",
        isFullscreen && "fixed inset-0 z-50 rounded-none",
        className
      )}
    >
      {/* Main image */}
      <div className="relative h-[300px] lg:h-[400px]">
        <Image
          src={currentFrame.imageUrl}
          alt={`Interior - ${currentFrame.name}`}
          fill
          className="object-cover"
        />

        {/* Hotspots */}
        {currentFrame.hotspots?.map((hotspot) => (
          <motion.button
            key={hotspot.id}
            className="absolute w-8 h-8 -ml-4 -mt-4"
            style={{ left: `${hotspot.x}%`, top: `${hotspot.y}%` }}
            onClick={() => setActiveHotspot(activeHotspot?.id === hotspot.id ? null : hotspot)}
            whileHover={{ scale: 1.2 }}
            whileTap={{ scale: 0.9 }}
          >
            <span className="absolute inset-0 bg-primary/80 rounded-full animate-ping" />
            <span className="relative flex items-center justify-center w-full h-full bg-primary text-white rounded-full text-xs font-bold shadow-lg">
              +
            </span>
          </motion.button>
        ))}

        {/* Hotspot tooltip */}
        {activeHotspot && (
          <motion.div
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            className="absolute bottom-20 left-1/2 -translate-x-1/2 bg-white rounded-lg shadow-xl p-4 max-w-xs"
          >
            <h4 className="font-semibold text-gray-900">{activeHotspot.label}</h4>
            {activeHotspot.description && (
              <p className="text-sm text-gray-600 mt-1">{activeHotspot.description}</p>
            )}
          </motion.div>
        )}

        {/* Navigation arrows */}
        <button
          className="absolute left-4 top-1/2 -translate-y-1/2 bg-black/50 hover:bg-black/70 text-white p-2 rounded-full transition-colors"
          onClick={goToPrevious}
        >
          <ChevronLeft className="h-6 w-6" />
        </button>

        <button
          className="absolute right-4 top-1/2 -translate-y-1/2 bg-black/50 hover:bg-black/70 text-white p-2 rounded-full transition-colors"
          onClick={goToNext}
        >
          <ChevronRight className="h-6 w-6" />
        </button>

        {/* Frame label */}
        <div className="absolute top-4 left-4 bg-black/60 text-white text-sm px-3 py-1.5 rounded-full backdrop-blur-sm">
          {currentFrame.name}
        </div>

        {/* Fullscreen button */}
        <button
          className="absolute top-4 right-4 bg-black/60 hover:bg-black/80 text-white p-2 rounded-full transition-colors"
          onClick={toggleFullscreen}
        >
          <Maximize2 className="h-4 w-4" />
        </button>
      </div>

      {/* Thumbnails */}
      <div className="flex gap-2 p-3 bg-gray-800 overflow-x-auto">
        {frames.map((frame, index) => (
          <button
            key={frame.id}
            className={cn(
              "relative flex-shrink-0 w-16 h-12 rounded-lg overflow-hidden border-2 transition-all",
              index === currentIndex
                ? "border-primary ring-2 ring-primary/30"
                : "border-transparent hover:border-white/50"
            )}
            onClick={() => {
              setCurrentIndex(index);
              setActiveHotspot(null);
            }}
          >
            <Image
              src={frame.imageUrl}
              alt={frame.name}
              fill
              className="object-cover"
            />
          </button>
        ))}
      </div>
    </div>
  );
}
```

---

## 📊 RESUMEN DE COMPONENTES

| Componente           | Archivo                           | Función                             |
| -------------------- | --------------------------------- | ----------------------------------- |
| `Vehicle360Viewer`   | `vehicles/Vehicle360Viewer.tsx`   | Visor interactivo exterior 360°     |
| `Vehicle360Uploader` | `vehicles/Vehicle360Uploader.tsx` | Subida y procesamiento para dealers |
| `Interior360Viewer`  | `vehicles/Interior360Viewer.tsx`  | Visor de interior con hotspots      |
| `useVehicle360`      | `hooks/useVehicle360.ts`          | Hook con state y API calls          |
| `vehicle360Service`  | `services/vehicle360Service.ts`   | Service para endpoints              |

---

## 🔗 INTEGRACIÓN EN OTRAS PÁGINAS

### En VehicleDetailPage

```tsx
import { Vehicle360Viewer } from "@/components/vehicles/Vehicle360Viewer";
import { Interior360Viewer } from "@/components/vehicles/Interior360Viewer";

// En el componente VehicleTabs, agregar tab "360°"
<Tabs defaultValue="description">
  <TabsList>
    <TabsTrigger value="description">Descripción</TabsTrigger>
    <TabsTrigger value="specs">Especificaciones</TabsTrigger>
    <TabsTrigger value="features">Características</TabsTrigger>
    <TabsTrigger value="360">Vista 360°</TabsTrigger>
  </TabsList>

  <TabsContent value="360">
    <div className="space-y-6">
      <Vehicle360Viewer vehicleId={vehicle.id} height={500} />
      <Interior360Viewer vehicleId={vehicle.id} />
    </div>
  </TabsContent>
</Tabs>;
```

### En PublishForm (Dealers)

```tsx
import { Vehicle360Uploader } from "@/components/vehicles/Vehicle360Uploader";

// Agregar paso "Video 360°" después de "Fotos"
const STEPS = [
  { title: "Información básica", component: BasicInfoStep },
  { title: "Detalles", component: DetailsStep },
  { title: "Fotos", component: PhotosStep },
  { title: "Video 360°", component: Video360Step }, // ← NUEVO
  { title: "Precio", component: PricingStep },
  { title: "Revisar", component: PreviewStep },
];

// Componente Video360Step
function Video360Step({ vehicleId }: { vehicleId: string }) {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-lg font-semibold mb-2">Vista 360° (Opcional)</h2>
        <p className="text-gray-600">
          Agrega una vista 360° para que los compradores vean tu vehículo desde
          todos los ángulos.
        </p>
      </div>

      <Vehicle360Uploader vehicleId={vehicleId} />
    </div>
  );
}
```

---

## ✅ CHECKLIST DE IMPLEMENTACIÓN

- [ ] Crear tipos en `src/types/vehicle360.ts`
- [ ] Crear service `src/lib/services/vehicle360Service.ts`
- [ ] Crear hook `src/lib/hooks/useVehicle360.ts`
- [ ] Crear `Vehicle360Viewer` component
- [ ] Crear `Vehicle360Uploader` component
- [ ] Crear `Interior360Viewer` component
- [ ] Integrar en `VehicleTabs` (detalle)
- [ ] Agregar paso en `PublishForm` (dealers)
- [ ] Probar drag/touch en desktop y mobile
- [ ] Verificar fullscreen funciona
- [ ] Test de loading/error states

---

**Siguiente:** [04-PAGINAS/18-vehicle-360-page.md](../04-PAGINAS/18-vehicle-360-page.md)
