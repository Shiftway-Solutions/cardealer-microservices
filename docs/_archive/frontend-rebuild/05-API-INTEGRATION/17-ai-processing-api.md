# 🤖 17 - AI Processing API

**Servicio:** AIProcessingService  
**Puerto:** 8080  
**Base Path:** `/api/ai`, `/api/aiprocessing`  
**Autenticación:** ✅ Parcial (algunos públicos)

---

## 📖 Descripción

Servicio de procesamiento de imágenes con IA. Permite:

- Remover/reemplazar fondos de imágenes de vehículos
- Generar vistas 360° desde video o imágenes
- Analizar imágenes para clasificación automática
- Procesar en batch para dealers

---

## 🎯 Endpoints Disponibles

| #   | Método | Endpoint                              | Auth     | Descripción                     |
| --- | ------ | ------------------------------------- | -------- | ------------------------------- |
| 1   | `POST` | `/api/ai/process`                     | ❌       | Procesar una imagen             |
| 2   | `POST` | `/api/ai/process/batch`               | ✅       | Procesar múltiples imágenes     |
| 3   | `POST` | `/api/ai/spin360/generate`            | ✅       | Generar vista 360°              |
| 4   | `GET`  | `/api/ai/jobs/{jobId}`                | ❌       | Estado de un job                |
| 5   | `POST` | `/api/ai/jobs/{jobId}/cancel`         | ✅       | Cancelar job                    |
| 6   | `POST` | `/api/ai/jobs/{jobId}/retry`          | ✅       | Reintentar job fallido          |
| 7   | `POST` | `/api/ai/analyze`                     | ✅       | Analizar imagen (clasificar)    |
| 8   | `GET`  | `/api/ai/backgrounds`                 | ✅       | Listar fondos disponibles       |
| 9   | `GET`  | `/api/ai/stats/queue`                 | ✅ Admin | Estadísticas de cola            |
| 10  | `GET`  | `/api/ai/vehicles/{vehicleId}/images` | ❌       | Imágenes procesadas de vehículo |

---

## 📝 Detalle de Endpoints

### 1. POST `/api/ai/process` - Procesar Imagen

**Request:**

```json
{
  "vehicleId": "vehicle-123",
  "imageUrl": "https://s3.../original.jpg",
  "type": "BackgroundRemoval",
  "options": {
    "backgroundId": "showroom-white",
    "outputFormat": "webp",
    "quality": 90,
    "preserveShadow": true
  }
}
```

**Response 200:**

```json
{
  "jobId": "job-456",
  "status": "Queued",
  "estimatedTimeSeconds": 30,
  "position": 5
}
```

---

### 3. POST `/api/ai/spin360/generate` - Generar 360°

**Request:**

```json
{
  "vehicleId": "vehicle-123",
  "sourceType": "Video",
  "videoUrl": "https://s3.../walkthrough.mp4",
  "frameCount": 36,
  "options": {
    "removeBackground": true,
    "backgroundId": "studio-gray",
    "autoRotate": true,
    "rotationSpeed": 5
  }
}
```

**Response 200:**

```json
{
  "jobId": "spin-789",
  "status": "Processing",
  "estimatedTimeSeconds": 120
}
```

---

### 4. GET `/api/ai/jobs/{jobId}` - Estado del Job

**Response 200:**

```json
{
  "jobId": "job-456",
  "status": "Completed",
  "progress": 100,
  "result": {
    "processedImageUrl": "https://s3.../processed.webp",
    "maskUrl": "https://s3.../mask.png",
    "thumbnailUrl": "https://s3.../thumb.webp"
  },
  "processingTimeMs": 2340,
  "createdAt": "2026-01-30T10:00:00Z",
  "completedAt": "2026-01-30T10:00:03Z"
}
```

**Estados posibles:**

- `Queued` - En cola
- `Processing` - Procesando
- `Completed` - Completado
- `Failed` - Fallido
- `Cancelled` - Cancelado

---

### 8. GET `/api/ai/backgrounds` - Fondos Disponibles

**Response 200:**

```json
{
  "backgrounds": [
    {
      "id": "showroom-white",
      "name": "Showroom Blanco",
      "category": "Indoor",
      "thumbnailUrl": "https://s3.../bg-thumb-1.jpg",
      "isPremium": false
    },
    {
      "id": "studio-gradient",
      "name": "Estudio Gradiente",
      "category": "Studio",
      "thumbnailUrl": "https://s3.../bg-thumb-2.jpg",
      "isPremium": true
    }
  ]
}
```

---

## 🔧 TypeScript Types

```typescript
// ============================================================================
// PROCESSING TYPES
// ============================================================================

export type ProcessingType =
  | "BackgroundRemoval"
  | "BackgroundReplacement"
  | "Enhancement"
  | "Segmentation"
  | "Classification";

export type JobStatus =
  | "Queued"
  | "Processing"
  | "Completed"
  | "Failed"
  | "Cancelled";

export interface ProcessImageRequest {
  vehicleId: string;
  imageUrl: string;
  type: ProcessingType;
  options?: ProcessingOptions;
}

export interface ProcessingOptions {
  backgroundId?: string;
  outputFormat?: "webp" | "png" | "jpeg";
  quality?: number;
  preserveShadow?: boolean;
  enhanceColors?: boolean;
}

export interface ProcessImageResponse {
  jobId: string;
  status: JobStatus;
  estimatedTimeSeconds?: number;
  position?: number;
}

// ============================================================================
// BATCH PROCESSING
// ============================================================================

export interface ProcessBatchRequest {
  vehicleId: string;
  imageUrls: string[];
  type: ProcessingType;
  options?: ProcessingOptions;
}

export interface BatchProcessResponse {
  batchId: string;
  jobs: { imageUrl: string; jobId: string }[];
  totalJobs: number;
  estimatedTimeSeconds: number;
}

// ============================================================================
// 360° SPIN
// ============================================================================

export type SourceType = "Video" | "Images";

export interface Generate360Request {
  vehicleId: string;
  sourceType: SourceType;
  videoUrl?: string;
  imageUrls?: string[];
  frameCount?: number;
  options?: Spin360Options;
}

export interface Spin360Options {
  removeBackground?: boolean;
  backgroundId?: string;
  autoRotate?: boolean;
  rotationSpeed?: number;
}

// ============================================================================
// JOB STATUS
// ============================================================================

export interface JobStatusResponse {
  jobId: string;
  status: JobStatus;
  progress: number;
  result?: ProcessingResult;
  error?: string;
  processingTimeMs?: number;
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
}

export interface ProcessingResult {
  processedImageUrl: string;
  maskUrl?: string;
  thumbnailUrl?: string;
  metadata?: Record<string, unknown>;
}

// ============================================================================
// BACKGROUNDS
// ============================================================================

export interface Background {
  id: string;
  name: string;
  category: string;
  thumbnailUrl: string;
  fullUrl?: string;
  isPremium: boolean;
}

// ============================================================================
// QUEUE STATS (Admin)
// ============================================================================

export interface QueueStats {
  pending: number;
  processing: number;
  completed24h: number;
  failed24h: number;
  averageProcessingTimeMs: number;
  workerStatus: { workerId: string; status: string; currentJob?: string }[];
}
```

---

## 📡 Service Layer

```typescript
// src/services/aiProcessingService.ts
import { apiClient } from "./api-client";
import type {
  ProcessImageRequest,
  ProcessImageResponse,
  ProcessBatchRequest,
  BatchProcessResponse,
  Generate360Request,
  JobStatusResponse,
  Background,
  QueueStats,
} from "@/types/ai-processing";

class AIProcessingService {
  // ============================================================================
  // SINGLE IMAGE PROCESSING
  // ============================================================================

  async processImage(
    request: ProcessImageRequest,
  ): Promise<ProcessImageResponse> {
    const response = await apiClient.post<ProcessImageResponse>(
      "/api/ai/process",
      request,
    );
    return response.data;
  }

  async processBatch(
    request: ProcessBatchRequest,
  ): Promise<BatchProcessResponse> {
    const response = await apiClient.post<BatchProcessResponse>(
      "/api/ai/process/batch",
      request,
    );
    return response.data;
  }

  // ============================================================================
  // 360° SPIN
  // ============================================================================

  async generate360Spin(
    request: Generate360Request,
  ): Promise<ProcessImageResponse> {
    const response = await apiClient.post<ProcessImageResponse>(
      "/api/ai/spin360/generate",
      request,
    );
    return response.data;
  }

  // ============================================================================
  // JOB MANAGEMENT
  // ============================================================================

  async getJobStatus(jobId: string): Promise<JobStatusResponse> {
    const response = await apiClient.get<JobStatusResponse>(
      `/api/ai/jobs/${jobId}`,
    );
    return response.data;
  }

  async cancelJob(jobId: string): Promise<void> {
    await apiClient.post(`/api/ai/jobs/${jobId}/cancel`);
  }

  async retryJob(jobId: string): Promise<void> {
    await apiClient.post(`/api/ai/jobs/${jobId}/retry`);
  }

  // ============================================================================
  // BACKGROUNDS & ANALYSIS
  // ============================================================================

  async getBackgrounds(): Promise<Background[]> {
    const response = await apiClient.get<{ backgrounds: Background[] }>(
      "/api/ai/backgrounds",
    );
    return response.data.backgrounds;
  }

  async analyzeImage(imageUrl: string): Promise<any> {
    const response = await apiClient.post("/api/ai/analyze", { imageUrl });
    return response.data;
  }

  // ============================================================================
  // VEHICLE IMAGES
  // ============================================================================

  async getVehicleProcessedImages(
    vehicleId: string,
  ): Promise<ProcessingResult[]> {
    const response = await apiClient.get<{ images: ProcessingResult[] }>(
      `/api/ai/vehicles/${vehicleId}/images`,
    );
    return response.data.images;
  }

  // ============================================================================
  // ADMIN
  // ============================================================================

  async getQueueStats(): Promise<QueueStats> {
    const response = await apiClient.get<QueueStats>("/api/ai/stats/queue");
    return response.data;
  }
}

export const aiProcessingService = new AIProcessingService();
```

---

## 🎣 React Query Hooks

```typescript
// src/hooks/useAIProcessing.ts
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { aiProcessingService } from "@/services/aiProcessingService";
import type {
  ProcessImageRequest,
  Generate360Request,
} from "@/types/ai-processing";

export const aiKeys = {
  all: ["ai"] as const,
  jobs: () => [...aiKeys.all, "jobs"] as const,
  job: (id: string) => [...aiKeys.jobs(), id] as const,
  backgrounds: () => [...aiKeys.all, "backgrounds"] as const,
  vehicleImages: (vehicleId: string) =>
    [...aiKeys.all, "vehicle", vehicleId] as const,
  queueStats: () => [...aiKeys.all, "queue-stats"] as const,
};

// ============================================================================
// PROCESSING MUTATIONS
// ============================================================================

export function useProcessImage() {
  return useMutation({
    mutationFn: (request: ProcessImageRequest) =>
      aiProcessingService.processImage(request),
  });
}

export function useProcessBatch() {
  return useMutation({
    mutationFn: aiProcessingService.processBatch,
  });
}

export function useGenerate360Spin() {
  return useMutation({
    mutationFn: (request: Generate360Request) =>
      aiProcessingService.generate360Spin(request),
  });
}

// ============================================================================
// JOB STATUS
// ============================================================================

export function useJobStatus(jobId: string, options?: { enabled?: boolean }) {
  return useQuery({
    queryKey: aiKeys.job(jobId),
    queryFn: () => aiProcessingService.getJobStatus(jobId),
    enabled: !!jobId && options?.enabled !== false,
    refetchInterval: (data) => {
      // Poll every 2s while processing
      if (data?.status === "Queued" || data?.status === "Processing") {
        return 2000;
      }
      return false;
    },
  });
}

export function useCancelJob() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (jobId: string) => aiProcessingService.cancelJob(jobId),
    onSuccess: (_, jobId) => {
      queryClient.invalidateQueries({ queryKey: aiKeys.job(jobId) });
    },
  });
}

export function useRetryJob() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (jobId: string) => aiProcessingService.retryJob(jobId),
    onSuccess: (_, jobId) => {
      queryClient.invalidateQueries({ queryKey: aiKeys.job(jobId) });
    },
  });
}

// ============================================================================
// BACKGROUNDS
// ============================================================================

export function useBackgrounds() {
  return useQuery({
    queryKey: aiKeys.backgrounds(),
    queryFn: () => aiProcessingService.getBackgrounds(),
    staleTime: 1000 * 60 * 60, // 1 hour - backgrounds don't change often
  });
}

// ============================================================================
// VEHICLE IMAGES
// ============================================================================

export function useVehicleProcessedImages(vehicleId: string) {
  return useQuery({
    queryKey: aiKeys.vehicleImages(vehicleId),
    queryFn: () => aiProcessingService.getVehicleProcessedImages(vehicleId),
    enabled: !!vehicleId,
  });
}
```

---

## 🧩 Componente de Ejemplo

```typescript
// src/components/ai/ImageProcessor.tsx
import { useState } from "react";
import { useProcessImage, useJobStatus, useBackgrounds } from "@/hooks/useAIProcessing";

export const ImageProcessor = ({ vehicleId, imageUrl }: Props) => {
  const [jobId, setJobId] = useState<string | null>(null);
  const [selectedBackground, setSelectedBackground] = useState("showroom-white");

  const { data: backgrounds } = useBackgrounds();
  const processMutation = useProcessImage();
  const { data: jobStatus } = useJobStatus(jobId || "", { enabled: !!jobId });

  const handleProcess = async () => {
    const result = await processMutation.mutateAsync({
      vehicleId,
      imageUrl,
      type: "BackgroundReplacement",
      options: {
        backgroundId: selectedBackground,
        outputFormat: "webp",
        quality: 90,
      },
    });
    setJobId(result.jobId);
  };

  return (
    <div className="space-y-4">
      {/* Original Image */}
      <div className="relative">
        <img src={imageUrl} alt="Original" className="rounded-lg" />
      </div>

      {/* Background Selector */}
      <div className="flex gap-2 overflow-x-auto">
        {backgrounds?.map((bg) => (
          <button
            key={bg.id}
            onClick={() => setSelectedBackground(bg.id)}
            className={`p-1 rounded-lg border-2 ${
              selectedBackground === bg.id ? "border-blue-500" : "border-transparent"
            }`}
          >
            <img src={bg.thumbnailUrl} alt={bg.name} className="w-16 h-16 rounded" />
          </button>
        ))}
      </div>

      {/* Process Button */}
      <button
        onClick={handleProcess}
        disabled={processMutation.isPending}
        className="btn btn-primary w-full"
      >
        {processMutation.isPending ? "Enviando..." : "Procesar Imagen"}
      </button>

      {/* Job Status */}
      {jobStatus && (
        <div className="bg-gray-100 p-4 rounded-lg">
          <div className="flex justify-between mb-2">
            <span>Estado: {jobStatus.status}</span>
            <span>{jobStatus.progress}%</span>
          </div>
          <div className="w-full bg-gray-200 rounded-full h-2">
            <div
              className="bg-blue-500 h-2 rounded-full transition-all"
              style={{ width: `${jobStatus.progress}%` }}
            />
          </div>

          {jobStatus.status === "Completed" && jobStatus.result && (
            <div className="mt-4">
              <img
                src={jobStatus.result.processedImageUrl}
                alt="Procesada"
                className="rounded-lg"
              />
            </div>
          )}
        </div>
      )}
    </div>
  );
};
```

---

## 🎉 Resumen

✅ **10 Endpoints documentados**  
✅ **TypeScript Types** (Processing, Jobs, Backgrounds, 360°)  
✅ **Service Layer** (10 métodos)  
✅ **React Query Hooks** (9 hooks con polling automático)  
✅ **Componente ejemplo** (ImageProcessor con backgrounds)

---

_Última actualización: Enero 30, 2026_
