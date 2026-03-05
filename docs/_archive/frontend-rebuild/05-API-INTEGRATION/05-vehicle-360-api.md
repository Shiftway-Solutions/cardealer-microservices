# 🔌 API Integration - Vehicle 360° Processing

> **Tiempo estimado:** 30 minutos
> **Prerrequisitos:** Axios/Fetch configurado, Auth context
> **Backend:** Vehicle360ProcessingService, Video360Service, BackgroundRemovalService

---

## 📋 OBJETIVO

Documentar la integración completa con el backend de procesamiento 360°:

- Endpoints disponibles
- Request/Response types
- Manejo de errores
- Polling de estado
- Upload de videos

---

## 🔌 ENDPOINTS DEL BACKEND

### Base URL

```
Desarrollo: http://localhost:18443/api/vehicle360processing
Producción: https://api.okla.com.do/api/vehicle360processing
```

### Endpoints Disponibles

| Método   | Endpoint              | Descripción                            | Auth     |
| -------- | --------------------- | -------------------------------------- | -------- |
| `GET`    | `/viewer/{vehicleId}` | Obtener vista 360° de un vehículo      | ❌       |
| `HEAD`   | `/viewer/{vehicleId}` | Verificar si existe vista 360°         | ❌       |
| `POST`   | `/process`            | Iniciar procesamiento con URL de video | ✅       |
| `POST`   | `/upload-and-process` | Subir video y procesar                 | ✅       |
| `GET`    | `/jobs/{jobId}`       | Obtener estado de un job               | ✅       |
| `DELETE` | `/jobs/{jobId}`       | Cancelar un job                        | ✅       |
| `DELETE` | `/viewer/{vehicleId}` | Eliminar vista 360°                    | ✅       |
| `GET`    | `/statistics`         | Estadísticas de procesamiento (admin)  | ✅ Admin |

---

## 🔧 PASO 1: Configuración de API Client

```typescript
// filepath: src/lib/api.ts

import axios, { AxiosInstance, AxiosError } from "axios";
import { getSession } from "next-auth/react";

const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_URL || "http://localhost:18443";

// Crear instancia de axios
export const api: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
  headers: {
    "Content-Type": "application/json",
  },
});

// Interceptor para agregar token JWT
api.interceptors.request.use(async (config) => {
  const session = await getSession();

  if (session?.accessToken) {
    config.headers.Authorization = `Bearer ${session.accessToken}`;
  }

  return config;
});

// Interceptor para manejo de errores
api.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    // Manejar errores específicos
    if (error.response?.status === 401) {
      // Token expirado - redirigir a login
      window.location.href = "/login?expired=true";
    }

    if (error.response?.status === 429) {
      // Rate limit
      console.error("Rate limit exceeded");
    }

    return Promise.reject(error);
  },
);
```

---

## 🔧 PASO 2: Types para Vehicle 360°

```typescript
// filepath: src/types/vehicle360.ts

// ============================================
// ENUMS
// ============================================

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

export type Video360Provider =
  | "ApyHub"
  | "FfmpegApi"
  | "Cloudinary"
  | "Imgix"
  | "Shotstack";

export type BackgroundRemovalProvider =
  | "LocalMl"
  | "ClipDrop"
  | "RemoveBg"
  | "Photoroom"
  | "Slazzer"
  | "RemovalAi";

// ============================================
// RESPONSE TYPES
// ============================================

/** Frame individual de la vista 360° */
export interface Vehicle360Frame {
  index: number;
  angle: number;
  name: string;
  imageUrl: string;
  thumbnailUrl: string;
  hasTransparentBackground: boolean;
}

/** Configuración del visor */
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

/** Vista 360° completa */
export interface Vehicle360View {
  vehicleId: string;
  isReady: boolean;
  totalFrames: number;
  primaryImageUrl: string;
  frames: Vehicle360Frame[];
  config: Vehicle360Config;
  processingDurationMs?: number;
  createdAt: string;
  updatedAt: string;
}

/** Job de procesamiento */
export interface ProcessingJob {
  jobId: string;
  vehicleId: string;
  status: ProcessingStatus;
  progress: number;
  currentStep: string;
  queuePosition?: number;
  estimatedTimeRemaining?: number;
  errorMessage?: string;
  errorCode?: string;
  createdAt: string;
  updatedAt: string;
  completedAt?: string;
}

// ============================================
// REQUEST TYPES
// ============================================

/** Opciones de procesamiento */
export interface ProcessingOptions {
  removeBackground?: boolean;
  video360Provider?: Video360Provider;
  backgroundRemovalProvider?: BackgroundRemovalProvider;
  frameCount?: number;
  outputFormat?: "png" | "jpeg" | "webp";
  quality?: number;
}

/** Request para iniciar procesamiento */
export interface StartProcessingRequest {
  vehicleId: string;
  videoUrl?: string;
  options?: ProcessingOptions;
}

/** Response de iniciar procesamiento */
export interface StartProcessingResponse {
  jobId: string;
  status: ProcessingStatus;
  queuePosition: number;
  estimatedWaitSeconds: number;
}

/** Estadísticas de procesamiento (admin) */
export interface ProcessingStatistics {
  totalJobsProcessed: number;
  totalJobsToday: number;
  averageProcessingTimeMs: number;
  successRate: number;
  activeJobs: number;
  queuedJobs: number;
  failedJobsToday: number;
  providerUsage: Record<string, number>;
}

// ============================================
// ERROR TYPES
// ============================================

export interface ApiError {
  code: string;
  message: string;
  details?: Record<string, unknown>;
}

export type Vehicle360ErrorCode =
  | "VIDEO_TOO_LARGE"
  | "VIDEO_TOO_LONG"
  | "INVALID_FORMAT"
  | "PROCESSING_FAILED"
  | "PROVIDER_ERROR"
  | "QUOTA_EXCEEDED"
  | "NOT_FOUND"
  | "UNAUTHORIZED"
  | "RATE_LIMITED";
```

---

## 🔧 PASO 3: Service Completo

```typescript
// filepath: src/lib/services/vehicle360Service.ts

import { api } from "@/lib/api";
import type {
  Vehicle360View,
  ProcessingJob,
  StartProcessingRequest,
  StartProcessingResponse,
  ProcessingOptions,
  ProcessingStatistics,
  Vehicle360ErrorCode,
} from "@/types/vehicle360";

const BASE_URL = "/api/vehicle360processing";

// Errores personalizados
export class Vehicle360Error extends Error {
  constructor(
    public code: Vehicle360ErrorCode,
    message: string,
    public details?: Record<string, unknown>,
  ) {
    super(message);
    this.name = "Vehicle360Error";
  }
}

export const vehicle360Service = {
  // ============================================
  // VIEWER ENDPOINTS
  // ============================================

  /**
   * Obtiene la vista 360° de un vehículo
   * @throws Vehicle360Error si no existe o hay error
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
      throw this.handleError(error);
    }
  },

  /**
   * Verifica si un vehículo tiene vista 360° disponible
   * Usa HEAD request para eficiencia
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
   * Elimina la vista 360° de un vehículo
   * @requires Auth (owner o admin)
   */
  async deleteView(vehicleId: string): Promise<void> {
    try {
      await api.delete(`${BASE_URL}/viewer/${vehicleId}`);
    } catch (error: any) {
      throw this.handleError(error);
    }
  },

  // ============================================
  // PROCESSING ENDPOINTS
  // ============================================

  /**
   * Inicia el procesamiento de un video 360° con URL
   * @requires Auth (owner)
   */
  async startProcessing(
    request: StartProcessingRequest,
  ): Promise<StartProcessingResponse> {
    try {
      const response = await api.post<StartProcessingResponse>(
        `${BASE_URL}/process`,
        {
          vehicleId: request.vehicleId,
          videoUrl: request.videoUrl,
          removeBackground: request.options?.removeBackground ?? true,
          video360Provider: request.options?.video360Provider ?? "FfmpegApi",
          backgroundRemovalProvider:
            request.options?.backgroundRemovalProvider ?? "ClipDrop",
          frameCount: request.options?.frameCount ?? 6,
          outputFormat: request.options?.outputFormat ?? "png",
        },
      );
      return response.data;
    } catch (error: any) {
      throw this.handleError(error);
    }
  },

  /**
   * Sube un video y comienza el procesamiento
   * @requires Auth (owner)
   */
  async uploadAndProcess(
    vehicleId: string,
    videoFile: File,
    options?: ProcessingOptions,
  ): Promise<StartProcessingResponse> {
    try {
      const formData = new FormData();
      formData.append("video", videoFile);
      formData.append("vehicleId", vehicleId);

      // Agregar opciones
      if (options?.removeBackground !== undefined) {
        formData.append("removeBackground", String(options.removeBackground));
      }
      if (options?.video360Provider) {
        formData.append("video360Provider", options.video360Provider);
      }
      if (options?.backgroundRemovalProvider) {
        formData.append(
          "backgroundRemovalProvider",
          options.backgroundRemovalProvider,
        );
      }
      if (options?.frameCount) {
        formData.append("frameCount", String(options.frameCount));
      }
      if (options?.outputFormat) {
        formData.append("outputFormat", options.outputFormat);
      }

      const response = await api.post<StartProcessingResponse>(
        `${BASE_URL}/upload-and-process`,
        formData,
        {
          headers: { "Content-Type": "multipart/form-data" },
          timeout: 120000, // 2 minutos para upload
          onUploadProgress: (progressEvent) => {
            const percent = progressEvent.total
              ? Math.round((progressEvent.loaded * 100) / progressEvent.total)
              : 0;
            console.log(`Upload progress: ${percent}%`);
          },
        },
      );
      return response.data;
    } catch (error: any) {
      throw this.handleError(error);
    }
  },

  // ============================================
  // JOB MANAGEMENT
  // ============================================

  /**
   * Obtiene el estado de un job de procesamiento
   */
  async getJobStatus(jobId: string): Promise<ProcessingJob> {
    try {
      const response = await api.get<ProcessingJob>(
        `${BASE_URL}/jobs/${jobId}`,
      );
      return response.data;
    } catch (error: any) {
      throw this.handleError(error);
    }
  },

  /**
   * Cancela un job de procesamiento
   */
  async cancelJob(jobId: string): Promise<void> {
    try {
      await api.delete(`${BASE_URL}/jobs/${jobId}`);
    } catch (error: any) {
      throw this.handleError(error);
    }
  },

  /**
   * Hace polling del estado de un job hasta que termine
   * @param onProgress Callback con el progreso actual
   */
  async waitForCompletion(
    jobId: string,
    options?: {
      pollInterval?: number;
      timeout?: number;
      onProgress?: (job: ProcessingJob) => void;
    },
  ): Promise<ProcessingJob> {
    const { pollInterval = 2000, timeout = 600000, onProgress } = options || {};

    const startTime = Date.now();

    return new Promise((resolve, reject) => {
      const poll = async () => {
        try {
          // Check timeout
          if (Date.now() - startTime > timeout) {
            reject(
              new Vehicle360Error(
                "PROCESSING_FAILED",
                "El procesamiento tardó demasiado tiempo",
              ),
            );
            return;
          }

          const job = await this.getJobStatus(jobId);

          if (onProgress) {
            onProgress(job);
          }

          if (job.status === "Completed") {
            resolve(job);
          } else if (job.status === "Failed") {
            reject(
              new Vehicle360Error(
                "PROCESSING_FAILED",
                job.errorMessage || "Error en el procesamiento",
              ),
            );
          } else {
            // Continuar polling
            setTimeout(poll, pollInterval);
          }
        } catch (error) {
          reject(error);
        }
      };

      poll();
    });
  },

  // ============================================
  // ADMIN ENDPOINTS
  // ============================================

  /**
   * Obtiene estadísticas de procesamiento
   * @requires Auth (admin)
   */
  async getStatistics(): Promise<ProcessingStatistics> {
    try {
      const response = await api.get<ProcessingStatistics>(
        `${BASE_URL}/statistics`,
      );
      return response.data;
    } catch (error: any) {
      throw this.handleError(error);
    }
  },

  // ============================================
  // HELPERS
  // ============================================

  /**
   * Mapea errores de API a errores tipados
   */
  handleError(error: any): Vehicle360Error {
    const status = error.response?.status;
    const data = error.response?.data;

    switch (status) {
      case 400:
        if (data?.code === "VIDEO_TOO_LARGE") {
          return new Vehicle360Error(
            "VIDEO_TOO_LARGE",
            "El video es muy grande. Máximo 500MB.",
          );
        }
        if (data?.code === "VIDEO_TOO_LONG") {
          return new Vehicle360Error(
            "VIDEO_TOO_LONG",
            "El video es muy largo. Máximo 60 segundos.",
          );
        }
        if (data?.code === "INVALID_FORMAT") {
          return new Vehicle360Error(
            "INVALID_FORMAT",
            "Formato de video no soportado. Usa MP4, MOV, AVI o WebM.",
          );
        }
        return new Vehicle360Error(
          "PROCESSING_FAILED",
          data?.message || "Error en la solicitud",
        );

      case 401:
        return new Vehicle360Error(
          "UNAUTHORIZED",
          "No tienes permiso para realizar esta acción",
        );

      case 404:
        return new Vehicle360Error(
          "NOT_FOUND",
          "No se encontró el recurso solicitado",
        );

      case 429:
        return new Vehicle360Error(
          "RATE_LIMITED",
          "Has excedido el límite de solicitudes. Intenta más tarde.",
        );

      case 500:
      case 502:
      case 503:
        return new Vehicle360Error(
          "PROVIDER_ERROR",
          "Error del servidor. Intenta más tarde.",
        );

      default:
        return new Vehicle360Error(
          "PROCESSING_FAILED",
          error.message || "Error desconocido",
        );
    }
  },

  /**
   * Valida un archivo de video antes de subirlo
   */
  validateVideoFile(file: File): { valid: boolean; error?: string } {
    const ACCEPTED_FORMATS = [
      "video/mp4",
      "video/quicktime",
      "video/avi",
      "video/webm",
      "video/x-msvideo",
    ];
    const MAX_SIZE = 500 * 1024 * 1024; // 500MB

    if (!ACCEPTED_FORMATS.includes(file.type)) {
      return {
        valid: false,
        error: "Formato no soportado. Usa MP4, MOV, AVI o WebM.",
      };
    }

    if (file.size > MAX_SIZE) {
      return {
        valid: false,
        error: "El archivo es muy grande. Máximo 500MB.",
      };
    }

    return { valid: true };
  },

  /**
   * Obtiene la duración de un video (async)
   */
  async getVideoDuration(file: File): Promise<number> {
    return new Promise((resolve, reject) => {
      const video = document.createElement("video");
      video.preload = "metadata";

      video.onloadedmetadata = () => {
        URL.revokeObjectURL(video.src);
        resolve(video.duration);
      };

      video.onerror = () => {
        URL.revokeObjectURL(video.src);
        reject(new Error("No se pudo leer el video"));
      };

      video.src = URL.createObjectURL(file);
    });
  },
};
```

---

## 🔧 PASO 4: React Query Hooks (Opcional)

```typescript
// filepath: src/lib/hooks/useVehicle360Query.ts

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { vehicle360Service } from "@/lib/services/vehicle360Service";
import type { StartProcessingRequest } from "@/types/vehicle360";

// Keys para React Query
export const vehicle360Keys = {
  all: ["vehicle360"] as const,
  views: () => [...vehicle360Keys.all, "views"] as const,
  view: (vehicleId: string) => [...vehicle360Keys.views(), vehicleId] as const,
  jobs: () => [...vehicle360Keys.all, "jobs"] as const,
  job: (jobId: string) => [...vehicle360Keys.jobs(), jobId] as const,
};

/**
 * Hook para obtener vista 360° con React Query
 */
export function useVehicle360View(vehicleId: string) {
  return useQuery({
    queryKey: vehicle360Keys.view(vehicleId),
    queryFn: () => vehicle360Service.getView(vehicleId),
    enabled: !!vehicleId,
    staleTime: 5 * 60 * 1000, // 5 minutos
    retry: false,
  });
}

/**
 * Hook para verificar si existe vista 360°
 */
export function useHasVehicle360View(vehicleId: string) {
  return useQuery({
    queryKey: [...vehicle360Keys.view(vehicleId), "exists"],
    queryFn: () => vehicle360Service.hasView(vehicleId),
    enabled: !!vehicleId,
    staleTime: 5 * 60 * 1000,
  });
}

/**
 * Hook para iniciar procesamiento
 */
export function useStartProcessing() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: StartProcessingRequest) =>
      vehicle360Service.startProcessing(request),
    onSuccess: (data, variables) => {
      // Invalidar la vista cuando se inicie procesamiento
      queryClient.invalidateQueries({
        queryKey: vehicle360Keys.view(variables.vehicleId),
      });
    },
  });
}

/**
 * Hook para subir y procesar video
 */
export function useUploadAndProcess() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ vehicleId, file }: { vehicleId: string; file: File }) =>
      vehicle360Service.uploadAndProcess(vehicleId, file),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({
        queryKey: vehicle360Keys.view(variables.vehicleId),
      });
    },
  });
}

/**
 * Hook para obtener estado de job con polling
 */
export function useProcessingJob(jobId: string | null) {
  return useQuery({
    queryKey: vehicle360Keys.job(jobId!),
    queryFn: () => vehicle360Service.getJobStatus(jobId!),
    enabled: !!jobId,
    refetchInterval: (query) => {
      const data = query.state.data;
      // Polling mientras está procesando
      if (
        data?.status === "Pending" ||
        data?.status === "Queued" ||
        data?.status === "Uploading" ||
        data?.status === "ExtractingFrames" ||
        data?.status === "RemovingBackgrounds"
      ) {
        return 2000; // Poll cada 2 segundos
      }
      return false; // Stop polling
    },
  });
}

/**
 * Hook para eliminar vista 360°
 */
export function useDeleteVehicle360View() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (vehicleId: string) => vehicle360Service.deleteView(vehicleId),
    onSuccess: (_, vehicleId) => {
      queryClient.invalidateQueries({
        queryKey: vehicle360Keys.view(vehicleId),
      });
    },
  });
}
```

---

## 📊 FLUJO DE PROCESAMIENTO

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        FLUJO DE PROCESAMIENTO 360°                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1. VALIDAR VIDEO (Frontend)                                                │
│     ├── Formato: MP4, MOV, AVI, WebM                                       │
│     ├── Tamaño: < 500MB                                                    │
│     └── Duración: < 60 segundos                                            │
│                                                                             │
│  2. SUBIR VIDEO                                                             │
│     POST /api/vehicle360processing/upload-and-process                      │
│     ├── FormData: video, vehicleId, options                               │
│     └── Response: { jobId, status: "Queued", estimatedWaitSeconds }       │
│                                                                             │
│  3. POLLING DE ESTADO                                                       │
│     GET /api/vehicle360processing/jobs/{jobId}                             │
│     ├── Status: Queued → Uploading → ExtractingFrames → Completed          │
│     ├── Progress: 0% → 25% → 50% → 75% → 100%                             │
│     └── Retry cada 2 segundos hasta Completed o Failed                     │
│                                                                             │
│  4. OBTENER VISTA                                                           │
│     GET /api/vehicle360processing/viewer/{vehicleId}                       │
│     └── Response: { frames: [...], config: {...}, isReady: true }          │
│                                                                             │
│  5. RENDERIZAR EN FRONTEND                                                  │
│     └── Vehicle360Viewer muestra los 6 frames interactivos                 │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## ⏱️ TIEMPOS DE PROCESAMIENTO

| Etapa                   | Tiempo Estimado | Timeout    |
| ----------------------- | --------------- | ---------- |
| Upload a S3             | 5-30s           | 120s       |
| Extracción de frames    | 30-120s         | 300s       |
| Remoción de fondos (×6) | 60-180s         | 180s/frame |
| **Total**               | **2-5 minutos** | 10 min     |

---

## 🔒 SEGURIDAD Y RATE LIMITING

### Headers de Autenticación

```typescript
Authorization: Bearer {jwt_token}
```

### Rate Limits

| Endpoint                   | Límite       | Ventana  |
| -------------------------- | ------------ | -------- |
| `POST /upload-and-process` | 10 requests  | 1 hora   |
| `POST /process`            | 20 requests  | 1 hora   |
| `GET /jobs/{id}`           | 100 requests | 1 minuto |
| `GET /viewer/{id}`         | Sin límite   | -        |

---

## 🎯 MANEJO DE ERRORES

```typescript
try {
  await vehicle360Service.uploadAndProcess(vehicleId, file);
} catch (error) {
  if (error instanceof Vehicle360Error) {
    switch (error.code) {
      case "VIDEO_TOO_LARGE":
        showToast.error("Video muy grande", "El máximo es 500MB");
        break;
      case "VIDEO_TOO_LONG":
        showToast.error("Video muy largo", "El máximo es 60 segundos");
        break;
      case "INVALID_FORMAT":
        showToast.error("Formato inválido", "Usa MP4, MOV, AVI o WebM");
        break;
      case "RATE_LIMITED":
        showToast.error("Límite excedido", "Intenta en 1 hora");
        break;
      case "PROCESSING_FAILED":
        showToast.error("Error", error.message);
        break;
      default:
        showToast.error("Error", "Algo salió mal. Intenta de nuevo.");
    }
  }
}
```

---

## ✅ CHECKLIST DE IMPLEMENTACIÓN

- [ ] Configurar `api.ts` con interceptors
- [ ] Crear types en `src/types/vehicle360.ts`
- [ ] Crear `vehicle360Service.ts`
- [ ] Crear hooks de React Query (opcional)
- [ ] Integrar con `useVehicle360` hook
- [ ] Manejar errores con toast
- [ ] Probar upload de video grande
- [ ] Probar polling de estado
- [ ] Verificar rate limiting

---

**Anterior:** [04-PAGINAS/18-vehicle-360-page.md](../04-PAGINAS/18-vehicle-360-page.md)
**Siguiente:** [03-detalle-vehiculo.md - Actualización Tab 360°](../04-PAGINAS/03-detalle-vehiculo.md)
