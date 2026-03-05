# 📸 PROMPT: Sistema Inteligente de Fotos y Vista 360° — OKLA Platform

> **Objetivo:** Implementar un sistema de subida de fotos de vehículos de alta eficiencia con compresión client-side, uploads paralelos, procesamiento automático de imágenes, remoción de fondos via pipeline interno (BackgroundRemovalService con ClipDrop/Slazzer), vista interactiva 360° desde video (Vehicle360ProcessingService + Video360Service/FFmpeg-API), y una experiencia de usuario que supere cualquier marketplace de vehículos en República Dominicana.

---

## 📋 CONTEXTO DEL PROYECTO

OKLA es un marketplace de compra/venta de vehículos en **República Dominicana**. Arquitectura de **microservicios .NET 8** con **Clean Architecture**, frontend **Next.js 14 App Router**, desplegado en **Digital Ocean Kubernetes**.

### Stack Relevante

- **Backend:** .NET 8, PostgreSQL 16, RabbitMQ 3.12, Redis 7, Ocelot Gateway
- **Frontend:** Next.js 14 + TypeScript + App Router, pnpm, shadcn/ui, React Query (TanStack Query)
- **Patrones:** CQRS (MediatR), Repository Pattern, Result Pattern, Domain Events via RabbitMQ
- **Almacenamiento:** AWS S3 (bucket: `okla-images-2026`, region: `us-east-2`)
- **Procesamiento:** SixLabors.ImageSharp (resize, thumbnails, optimización)
- **Seguridad:** JWT Bearer, FluentValidation, CSRF, sanitización de inputs

---

## 🏗️ ESTADO ACTUAL DEL CÓDIGO (Lo que ya existe)

### Microservicios Involucrados

| Servicio                        | Puerto Dev | Estado           | Responsabilidad                                                                                                                               | Usa MediatR |
| ------------------------------- | ---------- | ---------------- | --------------------------------------------------------------------------------------------------------------------------------------------- | ----------- |
| **MediaService**                | 15105      | ✅ Producción    | Upload genérico a S3, thumbnails, variantes, pre-signed URLs                                                                                  | ✅ Sí       |
| **VehiclesSaleService**         | 15104      | ✅ Producción    | CRUD vehículos, entidad `VehicleImage` con FK a Vehicle                                                                                       | ❌ No       |
| **Vehicle360ProcessingService** | -          | ✅ Existe        | Orquestador interno: extrae frames de video → remove bg → sube resultados. Coordina MediaService + Video360Service + BackgroundRemovalService | ✅ Sí       |
| **Video360Service**             | -          | ⚠️ \_DESCARTADOS | Extracción de frames de videos. Multi-provider: FFmpeg-API (default), ApyHub, Cloudinary, Imgix, Shotstack                                    | ✅ Sí       |
| **BackgroundRemovalService**    | -          | ✅ Existe        | Remoción de fondos multi-provider: ClipDrop (default), Slazzer, Photoroom, Remove.bg. Strategy Pattern con fallback chain                     | ✅ Sí       |

> ⚠️ **NOTA:** Video360Service está en `backend/_DESCARTADOS/` pero su interfaz HTTP es utilizada por Vehicle360ProcessingService. Debe restaurarse o re-implementarse.

### MediaService — Arquitectura Actual

#### Entidades de Dominio (TPH — Table Per Hierarchy)

```
MediaAsset (base, tabla: media_assets)
├── ImageMedia    → Width, Height, Hash, IsPrimary, AltText
├── VideoMedia    → Duration, Codecs, FrameRate, HLS streams
└── DocumentMedia → PageCount, Author, IsSearchable

MediaVariant → Variantes por tamaño (thumb/small/medium/large)
              → Width, Height, Quality, Format, Url, Size
```

#### Endpoints Existentes

| Método   | Ruta                              | Auth    | Descripción                                                    |
| -------- | --------------------------------- | ------- | -------------------------------------------------------------- |
| `POST`   | `/api/media/upload`               | ✅ JWT  | Upload genérico (100MB max)                                    |
| `POST`   | `/api/media/upload/image`         | ✅ JWT  | Upload imagen (50MB max) con category                          |
| `POST`   | `/api/media/upload/init`          | ✅ JWT  | Iniciar upload pre-signed (crea MediaAsset + retorna S3 URL)   |
| `POST`   | `/api/media/upload/{id}/finalize` | ✅ JWT  | Finalizar upload pre-signed (verifica en S3 → marca Processed) |
| `GET`    | `/api/media/{id}`                 | 🔓 Anon | Obtener media con variantes                                    |
| `GET`    | `/api/media/{id}/url`             | ✅ JWT  | Pre-signed URL fresca (1h)                                     |
| `DELETE` | `/api/media/{id}`                 | ✅ JWT  | Eliminar de S3 + BD                                            |

#### Procesamiento de Imágenes (ImageSharp)

```csharp
// IImageProcessor — ya implementado en ImageSharpProcessor.cs
ProcessImage(stream, variants[]) → genera múltiples tamaños
CreateThumbnail(stream, width, height) → JPEG quality 80
Optimize(stream) → comprime manteniendo calidad
GetImageInfo(stream) → dimensiones, formato, EXIF
ValidateImage(stream) → integridad de archivo
```

Variantes por defecto: `thumb` (200×200), `small` (400×400), `medium` (800×800), `large` (1200×1200)

#### ⚠️ Workers de Procesamiento — VACÍOS (NO implementados)

```
MediaService.Workers/
├── Handlers/
│   ├── ImageProcessingHandler.cs    ← VACÍO (solo scaffold)
│   ├── VideoTranscodingHandler.cs   ← VACÍO
│   └── MediaCleanupHandler.cs       ← VACÍO
├── Services/
│   ├── ImageProcessingWorker.cs     ← VACÍO
│   ├── VideoTranscodingWorker.cs    ← VACÍO
│   └── DocumentProcessingWorker.cs  ← VACÍO
```

Los domain events (`MediaUploadedEvent`, `MediaProcessedEvent`) se publican pero **ningún consumer los procesa**. Las variantes (thumbnails, tamaños) NO se generan automáticamente tras el upload.

#### Almacenamiento S3

```json
{
  "Storage": {
    "Provider": "S3",
    "S3": {
      "BucketName": "okla-images-2026",
      "Region": "us-east-2",
      "PreSignedUrlExpirationMinutes": 60
    }
  }
}
```

Path pattern: `{ownerId}/{mediaType}/{filename}`

#### Messaging (RabbitMQ)

| Exchange         | Tipo   | Colas                                                |
| ---------------- | ------ | ---------------------------------------------------- |
| `media.events`   | Topic  | `media.uploaded`, `media.processed`, `media.deleted` |
| `media.commands` | Direct | `media.process`                                      |

### VehiclesSaleService — Relación Vehicle ↔ Images

#### Entidad `VehicleImage`

```csharp
public class VehicleImage
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }        // Multi-tenant
    public Guid VehicleId { get; set; }        // FK → Vehicle
    public string Url { get; set; }            // URL full-size (required, max 500)
    public string? ThumbnailUrl { get; set; }  // URL thumbnail (max 500)
    public string? AltText { get; set; }       // Caption
    public ImageType ImageType { get; set; }   // Exterior, Interior, Engine, Damage, Documents, Other
    public int SortOrder { get; set; }         // Orden de display
    public bool IsPrimary { get; set; }        // Imagen principal del listing
    public long? FileSize { get; set; }        // Bytes
    public string? ContentType { get; set; }   // MIME type
    public int? Width { get; set; }
    public int? Height { get; set; }
}

public enum ImageType { Exterior, Interior, Engine, Damage, Documents, Other }
```

#### Flujo Actual de Creación

```
Frontend                    MediaService                VehiclesSaleService
   │                            │                            │
   │  POST /api/media/upload    │                            │
   │ ────────────────────────▶  │                            │
   │   (multipart/form-data)    │                            │
   │                            │  → S3 upload               │
   │  ◀──── { url } ────────── │                            │
   │                            │                            │
   │  POST /api/vehicles        │                            │
   │ ─────────────────────────────────────────────────────▶  │
   │   { Images: ["url1", "url2", ...] }                     │
   │                            │    → Creates VehicleImage  │
   │                            │      rows con las URLs     │
```

**⚠️ Problemas del flujo actual:**

1. Thumbnails se generan por **manipulación de string en la URL** (`/800/` → `/200/`) — NO procesamiento real
2. Uploads son **secuenciales** (uno por uno) en el frontend
3. **No hay compresión client-side** — las fotos del celular (3-8MB) se suben tal cual
4. **No hay procesamiento automático** post-upload (workers vacíos)
5. **No hay validación de calidad** de imagen (blur, baja resolución, etc.)
6. Las variantes (thumb/small/medium/large) **nunca se generan** automáticamente

### Video360Service — Extracción de Frames (Multi-Provider)

> ⚠️ Actualmente en `backend/_DESCARTADOS/Video360Service/` — debe restaurarse al directorio principal.

El servicio extrae frames individuales de un video walk-around del vehículo. Usa **Strategy Pattern** con múltiples proveedores y fallback automático.

#### Proveedores de Extracción de Frames

| Proveedor         | Costo/Vehículo | Prioridad   | Descripción                        |
| ----------------- | -------------- | ----------- | ---------------------------------- |
| **FFmpeg-API** ⭐ | $0.011         | 1 (DEFAULT) | API REST sobre FFmpeg, más estable |
| **ApyHub**        | $0.009         | 2           | Más barato, menor estabilidad      |
| **Cloudinary**    | $0.012         | 3           | Buena calidad, API robusta         |
| **Imgix**         | $0.018         | 4           | Premium, alto costo                |
| **Shotstack**     | $0.050         | 5           | Premium, funciones avanzadas       |

#### Fallback Chain

```
FFmpeg-API (default)
  → si falla 3x → ApyHub
    → si falla 3x → Cloudinary
      → si falla 3x → Imgix
        → si falla 3x → Shotstack
          → si falla 3x → ERROR definitivo
```

#### Configuración por defecto

```csharp
FrameCount: 6               // Frames a extraer
SmartFrameSelection: true    // Selección inteligente de posiciones
Width: 1920, Height: 1080   // Resolución de salida
Format: "png"               // PNG para transparencia en background removal
AutoCorrectExposure: true
GenerateThumbnails: true
```

#### Ángulos de los 6 Frames por Defecto

| Frame | Ángulo | Vista            |
| ----- | ------ | ---------------- |
| 1     | 0°     | Frente           |
| 2     | 60°    | Frente-Derecha   |
| 3     | 120°   | Atrás-Derecha    |
| 4     | 180°   | Atrás            |
| 5     | 240°   | Atrás-Izquierda  |
| 6     | 300°   | Frente-Izquierda |

#### Resiliencia (Polly)

- **Retry:** 3 intentos con backoff exponencial (2s, 4s, 8s)
- **Circuit Breaker:** 5 fallos → circuito abierto 30-60s → intento parcial
- **Timeout optimista:** 300s (videos grandes toman tiempo)

### BackgroundRemovalService — Remoción de Fondos (Multi-Provider)

Servicio completamente implementado con **Strategy Pattern** para elegir proveedor de remoción de fondos. 55 archivos, Clean Architecture.

#### Proveedores de Background Removal

| Proveedor       | Costo/Imagen | Prioridad   | Calidad    | Descripción                                 |
| --------------- | ------------ | ----------- | ---------- | ------------------------------------------- |
| **ClipDrop** ⭐ | $0.05        | 1 (DEFAULT) | ⭐⭐⭐⭐   | Stability AI, buena relación precio/calidad |
| **Slazzer**     | $0.02        | 2           | ⭐⭐⭐     | Más barato, calidad aceptable               |
| **Photoroom**   | $0.05        | 3           | ⭐⭐⭐⭐   | Similar a ClipDrop                          |
| **Remove.bg**   | $0.20        | 4           | ⭐⭐⭐⭐⭐ | Mejor calidad, más caro                     |

#### Fallback Chain

```
ClipDrop (default)
  → si falla 3x → Remove.bg
    → si falla 3x → Photoroom
      → si falla 3x → Slazzer
        → si falla 3x → ERROR definitivo
```

#### Endpoints BackgroundRemovalService

| Método | Ruta                                 | Descripción                        |
| ------ | ------------------------------------ | ---------------------------------- |
| `POST` | `/api/backgroundremoval/remove`      | Remover fondo de una imagen        |
| `POST` | `/api/backgroundremoval/batch`       | Batch de hasta 10 imágenes         |
| `GET`  | `/api/backgroundremoval/{id}/status` | Estado del procesamiento           |
| `GET`  | `/api/backgroundremoval/{id}/result` | Resultado con URL de imagen limpia |

#### Almacenamiento S3

- **Bucket:** `cardealer-media` (us-east-1) — Bucket separado de MediaService
- **Path:** `background-removal/{jobId}/{filename}`

#### Resiliencia (Polly)

- **Retry:** 3 intentos con backoff exponencial (2s, 4s, 8s)
- **Circuit Breaker:** 5 fallos → circuito abierto 30-60s
- **Timeout optimista:** 180s

### Vehicle360ProcessingService — Orquestador del Pipeline

El orquestador coordina todo el flujo de procesamiento 360°. **NO procesa nada directamente** — delega a los servicios especializados.

#### Pipeline de Procesamiento Interno

```
Pending → Queued → Processing → UploadingVideo → VideoUploaded
  → ExtractingFrames → FramesExtracted → RemovingBackgrounds
  → UploadingResults → Completed
                    ↘ Failed (retry hasta 3x)
                    ↘ Cancelled
```

#### Endpoints

| Método | Ruta                                        | Descripción                            |
| ------ | ------------------------------------------- | -------------------------------------- |
| `POST` | `/api/vehicle360/upload`                    | Subir video (500MB max)                |
| `POST` | `/api/vehicle360/process`                   | Procesar video ya en S3                |
| `GET`  | `/api/vehicle360/jobs/{id}/status`          | Estado del job                         |
| `GET`  | `/api/vehicle360/jobs/{id}`                 | Detalle completo                       |
| `GET`  | `/api/vehicle360/jobs/{id}/result`          | Resultado con frames                   |
| `POST` | `/api/vehicle360/jobs/{id}/retry`           | Reintentar (max 3)                     |
| `POST` | `/api/vehicle360/jobs/{id}/cancel`          | Cancelar                               |
| `GET`  | `/api/vehicle360/viewer/{vehicleId}`        | **Datos para viewer 360° en frontend** |
| `GET`  | `/api/vehicle360/vehicles/{vehicleId}/jobs` | Historial de jobs                      |
| `GET`  | `/api/vehicle360/user/{userId}/jobs`        | Jobs del usuario (paginado)            |

#### Dependencias Internas (Polly resilience: retry + circuit breaker + timeout)

| Servicio                        | Timeout | Función                               |
| ------------------------------- | ------- | ------------------------------------- |
| **MediaServiceHttpClient**      | 120s    | Upload video y frames procesados a S3 |
| **Video360ServiceHttpClient**   | 300s    | Extracción de frames del video        |
| **BackgroundRemovalHttpClient** | 180s    | Remoción de fondo de cada frame       |

#### Configuración Default de Procesamiento

```csharp
Width: 1920, Height: 1080
Format: "png" (transparencia)
FrameCount: 6
SmartFrameSelection: true
AutoCorrectExposure: true
GenerateThumbnails: true
BackgroundColor: "#FFFFFF"
```

### 💰 Costos del Pipeline 360° por Vehículo

El pipeline usa dos servicios externos (frame extraction + background removal). Aquí están los **3 tiers de costo**:

#### Tier Budget — $0.129/vehículo

| Servicio               | Proveedor | Costo Unitario | Unidades | Subtotal   |
| ---------------------- | --------- | -------------- | -------- | ---------- |
| Frame Extraction       | ApyHub    | $0.009         | 1 video  | $0.009     |
| Background Removal     | Slazzer   | $0.02          | 6 frames | $0.120     |
| **Total por vehículo** |           |                |          | **$0.129** |

#### Tier Recomendado — $0.311/vehículo ⭐

| Servicio               | Proveedor  | Costo Unitario | Unidades | Subtotal   |
| ---------------------- | ---------- | -------------- | -------- | ---------- |
| Frame Extraction       | FFmpeg-API | $0.011         | 1 video  | $0.011     |
| Background Removal     | ClipDrop   | $0.05          | 6 frames | $0.300     |
| **Total por vehículo** |            |                |          | **$0.311** |

#### Tier Premium — $1.25/vehículo

| Servicio               | Proveedor | Costo Unitario | Unidades | Subtotal   |
| ---------------------- | --------- | -------------- | -------- | ---------- |
| Frame Extraction       | Shotstack | $0.05          | 1 video  | $0.050     |
| Background Removal     | Remove.bg | $0.20          | 6 frames | $1.200     |
| **Total por vehículo** |           |                |          | **$1.250** |

#### Proyección Mensual (100 vehículos/mes)

| Tier        | Por Vehículo | 100 vehículos/mes | 500 vehículos/mes |
| ----------- | ------------ | ----------------- | ----------------- |
| Budget      | $0.129       | $12.90/mes        | $64.50/mes        |
| Recomendado | $0.311       | $31.10/mes        | $155.50/mes       |
| Premium     | $1.250       | $125.00/mes       | $625.00/mes       |

> 📊 **Recomendación:** Usar **Tier Recomendado** (FFmpeg-API + ClipDrop) como default. Ofrece la mejor relación calidad/precio. El costo de ~$0.31 por vehículo se absorbe con el plan Dealer ($49-299/mes).

### Control de Acceso por Tipo de Cuenta — Pipeline 360° y Background Removal

| Feature                           | Individual (Gratis) | Dealer (Suscripción)    |
| --------------------------------- | ------------------- | ----------------------- |
| Background removal (fondo blanco) | ✅ (1 gratis)       | ✅ Ilimitado            |
| Background removal batch          | ❌                  | ✅ (suscripción activa) |
| 360° desde video                  | ❌                  | ✅ (suscripción activa) |
| 360° desde fotos                  | ❌                  | ✅ (suscripción activa) |

### Frontend — Estado Actual de Fotos

#### `services/media.ts`

```typescript
uploadImage(file, options?) → POST /api/media/upload/image (multipart)
uploadMultipleImages(files) → secuencial (loop uno por uno)
uploadFile(file) → POST /api/media/upload (genérico)
deleteMedia(publicId) → DELETE /api/media/{id}
validateImageFile(file) → JPEG/PNG/GIF/WebP, max 10MB
validateVideoFile(file) → MP4/WebM/MOV, max 500MB
```

#### `hooks/use-media.ts`

```typescript
useUploadImage(); // mutation: un archivo
useUploadMultipleImages(); // mutation: múltiples (secuencial)
useUploadFile(); // mutation: genérico
useDeleteMedia(); // mutation: eliminar
```

#### Componente `photo-upload-step.tsx` (Smart Publish Wizard)

- Drag & drop zone con estado visual
- Click para seleccionar archivos
- Validación: JPEG/PNG/WebP, max 10MB
- Individual: min 3, max 20 fotos
- Dealer: min 5, max 50 fotos
- Grid con:
  - Marcar foto principal (estrella)
  - Eliminar foto (X)
  - Reordenar con drag nativo HTML
  - Badge "PRINCIPAL" en la foto primaria
  - Card "Agregar más"
- Usa `URL.createObjectURL()` para previews locales
- **Upload real ocurre al momento de publicar** (no durante la selección)

#### Componente `photo-guide.tsx`

8 ángulos recomendados: Frontal, Trasera, Lateral Izq, Lateral Der, ¾ Frontal-Izq, ¾ Frontal-Der, Interior, Tablero

#### ⚠️ Lo que NO existe en el frontend

| Funcionalidad                    | Estado                                              |
| -------------------------------- | --------------------------------------------------- |
| Compresión client-side           | ❌ No existe                                        |
| Uploads paralelos                | ❌ No (secuencial)                                  |
| Cropping/edición                 | ❌ No existe                                        |
| Visor 360°                       | ❌ No existe (a pesar de que hay servicios backend) |
| Background removal (UI)          | ❌ No existe                                        |
| Lightbox/gallery viewer          | ❌ No existe                                        |
| Progreso individual por foto     | ⚠️ Parcial (solo en legacy)                         |
| Validación de calidad de imagen  | ❌ No existe                                        |
| Upload pre-signed (directo a S3) | ❌ No se usa (siempre va por MediaService)          |

### Gateway — Rutas Existentes para Media/Fotos/360°

```
/api/media/*              → mediaservice (puerto 8080 en K8s)
/api/vehicle360/*         → vehicle360processingservice
/api/backgroundremoval/*  → backgroundremovalservice
/api/vehicles/*           → vehiclessaleservice
```

Timeouts configurados: 180s para background removal, 300s para Vehicle360 (video procesamiento), 60s para media upload, 30s default.

---

## 🎯 REQUERIMIENTOS DE IMPLEMENTACIÓN

### Objetivo Principal

Crear un **sistema de fotos de vehículos de clase mundial** que:

1. Sea **extremadamente rápido** — compresión client-side + uploads paralelos + pre-signed URLs directas a S3
2. Ofrezca **procesamiento automático** — thumbnails, variantes, optimización sin intervención del usuario
3. Integre **remoción de fondos con pipeline interno** — BackgroundRemovalService (ClipDrop/Slazzer) para fotos limpias con un click
4. Soporte **vista 360° interactiva** — desde video del celular (Video360Service/FFmpeg-API extrae frames + BackgroundRemovalService limpia fondos)
5. Guíe al usuario para obtener **fotos de calidad profesional** — guía inteligente, validación de calidad
6. Funcione impecablemente en **móvil** — el 70%+ de los usuarios suben fotos desde el celular
7. Sea **resiliente** — retry automático, recuperación de uploads fallidos, progreso persistente
8. Sea **económicamente eficiente** — pipeline interno a ~$0.31/vehículo (Tier Recomendado) vs servicios premium a $1.25+

### Diagrama de Arquitectura Objetivo

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                            FRONTEND (Next.js 14)                             │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐     │
│  │  PhotoUploadManager (nuevo componente principal)                     │     │
│  │                                                                     │     │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────────┐   │     │
│  │  │ 📷 Fotos │  │ 🎬 Video │  │ 📱 360°  │  │ 🪄 Remover      │   │     │
│  │  │ Estándar │  │ Walk-    │  │ desde    │  │ Fondo           │   │     │
│  │  │          │  │ around   │  │ fotos    │  │ (post-upload)   │   │     │
│  │  └────┬─────┘  └────┬─────┘  └────┬─────┘  └──────┬───────────┘   │     │
│  │       │              │             │                │               │     │
│  │       ▼              │             │                │               │     │
│  │  ┌──────────────┐    │             │                │               │     │
│  │  │ Compresión   │    │             │                │               │     │
│  │  │ client-side  │    │             │                │               │     │
│  │  │ (browser-    │    │             │                │               │     │
│  │  │  image-comp) │    │             │                │               │     │
│  │  └──────┬───────┘    │             │                │               │     │
│  │         │            │             │                │               │     │
│  │         ▼            │             │                │               │     │
│  │  ┌──────────────┐    │             │                │               │     │
│  │  │ Upload       │    │             │                │               │     │
│  │  │ Paralelo     │    │             │                │               │     │
│  │  │ (3 concurr.) │    │             │                │               │     │
│  │  │ Pre-signed   │    │             │                │               │     │
│  │  │ URLs → S3    │    │             │                │               │     │
│  │  └──────┬───────┘    │             │                │               │     │
│  │         │            │             │                │               │     │
│  └─────────┼────────────┼─────────────┼────────────────┼───────────────┘     │
│            │            │             │                │                      │
└────────────┼────────────┼─────────────┼────────────────┼──────────────────────┘
             │            │             │                │
             ▼            ▼             ▼                ▼
┌────────────────┐ ┌─────────────────┐ ┌──────────────────────────────────┐
│  MediaService  │ │ Vehicle360      │ │ BackgroundRemovalService         │
│                │ │ Processing      │ │                                  │
│ • Init upload  │ │ Service         │ │ • POST /remove (1 imagen)       │
│ • Finalize     │ │ (Orquestador)   │ │ • POST /batch (hasta 10)        │
│ • Process      │ │                 │ │ • GET /{id}/status               │
│   variants     │ │ • Upload video  │ │ • GET /{id}/result               │
│ • Generate     │ │ • Coordina:     │ │                                  │
│   thumbnails   │ │   Video360Svc   │ │ Providers:                       │
│                │ │   + BgRemoval   │ │ ClipDrop → Remove.bg →           │
│                │ │   + MediaSvc    │ │ Photoroom → Slazzer              │
└───────┬────────┘ │ • GET viewer    │ └──────────────────────────────────┘
        │          │   data          │
        ▼          └──────┬──────────┘
┌────────────┐            │         ┌─────────────────────┐
│   AWS S3   │ ◀──────────┤         │  Video360Service    │
│  (bucket:  │            └────────▶│  (FFmpeg-API)       │
│  okla-     │                      │                     │
│  images-   │                      │  • Extraer 6 frames │
│  2026)     │                      │  • Smart selection   │
└────────────┘                      │  • Multi-provider    │
                                    └─────────────────────┘
```

---

## 📐 ESPECIFICACIONES TÉCNICAS DETALLADAS

### 1. Backend — Implementar Workers de Procesamiento en MediaService

#### 1.1 Implementar `ImageProcessingHandler` (actualmente vacío)

El handler debe escuchar la cola `media.process` de RabbitMQ y:

1. **Recibir** `ProcessMediaCommand` con el `MediaAssetId`
2. **Descargar** la imagen original de S3
3. **Generar variantes** usando `ImageSharpProcessor`:
   - `thumb`: 200×200, JPEG quality 80, crop center
   - `small`: 400×400, JPEG quality 85, max fit
   - `medium`: 800×800, JPEG quality 85, max fit
   - `large`: 1200×1200, JPEG quality 90, max fit
   - `webp`: 800×800, WebP quality 80 (formato moderno)
4. **Subir** cada variante a S3 con path: `{ownerId}/variants/{variantType}/{filename}`
5. **Crear** registros `MediaVariant` en BD con URLs de cada variante
6. **Actualizar** `MediaAsset.Status` a `Processed`
7. **Publicar** `MediaProcessedEvent` con URLs de todas las variantes
8. **Manejar errores** con retry (3 intentos) y DLQ si falla definitivamente

**¿Por qué es crítico?** Actualmente los thumbnails se "generan" manipulando strings de URL, lo cual no funciona realmente. Necesitamos variantes reales almacenadas en S3 para:

- Carga rápida de listados (thumbnails reales de 200×200)
- Responsive images (`srcset` con múltiples tamaños)
- Ahorro de ancho de banda (no descargar imágenes de 5MB para un thumbnail)

#### 1.2 Implementar `MediaCleanupHandler` (actualmente vacío)

Worker que periódicamente (cron cada 24h):

1. **Buscar** `MediaAsset` con `Status = Uploaded` y `CreatedAt < 48h ago` (uploads sin finalizar)
2. **Eliminar** de S3 y marcar como `Failed` en BD
3. **Buscar** `MediaAsset` sin `VehicleImage` asociado y con `CreatedAt < 7 días` (huérfanos)
4. **Notificar** al admin si hay más de 100 huérfanos (posible leak)
5. **Registrar** métricas de limpieza

#### 1.3 Nuevo Endpoint: Upload Optimizado para Vehículos

```
POST /api/media/upload/vehicle-image
```

Endpoint especializado para fotos de vehículos que:

1. Acepta `multipart/form-data` con campos adicionales:
   - `file` (required) — la imagen
   - `vehicleId` (optional, Guid) — si ya existe el vehículo
   - `imageType` (optional) — Exterior, Interior, Engine, etc.
   - `sortOrder` (optional, int)
   - `isPrimary` (optional, bool)
   - `compress` (optional, bool, default true) — comprimir server-side si no se hizo client-side
2. Valida que sea imagen válida (magic bytes, extensión, tamaño max 15MB)
3. Si `compress = true`: optimiza con ImageSharp (max 1920px ancho, JPEG quality 85)
4. Sube a S3 con path: `vehicles/{vehicleId}/{imageType}/{filename}`
5. **Dispara procesamiento asíncrono** de variantes vía RabbitMQ
6. Retorna inmediatamente con la URL original + promesa de variantes

**Response:**

```csharp
public record VehicleImageUploadResponse
{
    public string MediaId { get; init; }           // ID del MediaAsset
    public string OriginalUrl { get; init; }       // URL de la imagen original
    public string? ThumbnailUrl { get; init; }     // URL del thumbnail (si ya se generó inline)
    public long FileSize { get; init; }            // Tamaño final en bytes
    public int Width { get; init; }                // Ancho en px
    public int Height { get; init; }               // Alto en px
    public string ContentType { get; init; }       // MIME type
    public string ProcessingStatus { get; init; }  // "Queued" | "Processing" | "Completed"
}
```

#### 1.4 Nuevo Endpoint: Upload Batch para Vehículos

```
POST /api/media/upload/vehicle-images/batch
```

Acepta hasta **10 imágenes** en un solo request (multipart con múltiples files):

1. Valida todas las imágenes primero (falla rápido si alguna es inválida)
2. Procesa en paralelo (max 4 concurrentes)
3. Retorna array de `VehicleImageUploadResponse` con status individual por imagen
4. Las que fallan no bloquean las exitosas

#### 1.5 Nuevo Endpoint: Pre-signed URLs Batch

```
POST /api/media/upload/presigned-urls
Body: {
    "files": [
        { "filename": "foto1.jpg", "contentType": "image/jpeg", "size": 2500000 },
        { "filename": "foto2.jpg", "contentType": "image/jpeg", "size": 1800000 }
    ],
    "vehicleId": "optional-guid",
    "category": "vehicles"
}
```

Para el **flujo de upload directo a S3** (más eficiente):

1. Valida content types y tamaños
2. Crea `MediaAsset` por cada archivo (status: `Uploaded`)
3. Genera pre-signed PUT URLs de S3 (expiración: 15 min)
4. Retorna array de `{ mediaId, presignedUrl, expiresAt }`
5. Frontend sube directamente a S3 usando las URLs
6. Frontend llama a `POST /api/media/upload/{id}/finalize` por cada una
7. Finalize dispara procesamiento de variantes

**¿Por qué pre-signed URLs?** Elimina el MediaService como cuello de botella. Las fotos van directo del browser a S3, reduciendo latencia y carga del servidor.

#### 1.6 Nuevo Endpoint: Validación de Calidad de Imagen

```
POST /api/media/validate/quality
```

Acepta una imagen y retorna análisis de calidad:

```csharp
public record ImageQualityResult
{
    public double OverallScore { get; init; }      // 0-100
    public bool IsBlurry { get; init; }            // Detección de blur (Laplacian variance)
    public bool IsTooDark { get; init; }           // Histograma < umbral
    public bool IsTooBright { get; init; }         // Histograma > umbral
    public bool IsTooSmall { get; init; }          // Resolución < 640×480
    public bool HasExifOrientation { get; init; }  // Necesita rotación
    public int Width { get; init; }
    public int Height { get; init; }
    public double AspectRatio { get; init; }
    public string[] Warnings { get; init; }        // Mensajes legibles
    public string[] Suggestions { get; init; }     // Tips para mejorar
}
```

Usar **ImageSharp** para:

- **Blur detection**: Calcular Laplacian variance del grayscale — si < umbral → borrosa
- **Exposición**: Analizar histograma de luminancia — detectar sub/sobre-exposición
- **Resolución**: Verificar dimensiones mínimas (640×480 para vehículos)
- **Aspecto**: Advertir si la relación de aspecto es inusual para fotos de vehículos

### 2. Backend — Integrar 360° con el Flujo de Publicación

#### 2.1 Nuevo Endpoint en VehiclesSaleService: Asociar 360° a Vehículo

```
POST /api/vehicles/{vehicleId}/360-view
Body: {
    "type": "photos" | "video",
    "sourceJobId": "guid",
    "viewerDataUrl": "string",
    "thumbnailUrl": "string",
    "frameCount": 6,
    "isActive": true
}
```

#### 2.2 Nueva Entidad en VehiclesSaleService: `Vehicle360View`

```csharp
public class Vehicle360View
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }         // FK → Vehicle
    public Vehicle Vehicle { get; set; }
    public string Type { get; set; }            // "photos" | "video"
    public string? SourceJobId { get; set; }    // ID del job en Vehicle360ProcessingService
    public string ViewerDataUrl { get; set; }   // URL JSON con datos del viewer
    public string? ThumbnailUrl { get; set; }   // Preview estático
    public int FrameCount { get; set; }         // Cantidad de frames
    public List<string> FrameUrls { get; set; } // URLs de cada frame (JSONB)
    public string Status { get; set; }          // "Processing" | "Ready" | "Failed"
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
```

Agregar `Vehicle360View? View360` como navigation property en la entidad `Vehicle`.

#### 2.3 Endpoint para Obtener Datos del Viewer 360°

```
GET /api/vehicles/{vehicleId}/360-view
```

Retorna los datos necesarios para renderizar el viewer 360° en el frontend:

```csharp
public record Vehicle360ViewResponse
{
    public Guid Id { get; init; }
    public string Type { get; init; }
    public string Status { get; init; }
    public int FrameCount { get; init; }
    public List<string> FrameUrls { get; init; }  // URLs pre-signed de cada frame
    public string? ThumbnailUrl { get; init; }
    public ViewerConfig Config { get; init; }      // Configuración del viewer
}

public record ViewerConfig
{
    public int InitialFrame { get; init; } = 0;
    public bool AutoRotate { get; init; } = true;
    public int AutoRotateSpeed { get; init; } = 3; // RPM
    public bool AllowZoom { get; init; } = true;
    public double MaxZoom { get; init; } = 3.0;
    public bool ShowControls { get; init; } = true;
    public bool InvertDrag { get; init; } = false;
}
```

### 3. Frontend — Sistema Completo de Fotos

#### 3.1 Paquetes npm a Instalar

```bash
pnpm add browser-image-compression   # Compresión client-side (lossy/lossless)
pnpm add react-dropzone               # Drag & drop robusto con validación
pnpm add @dnd-kit/core @dnd-kit/sortable @dnd-kit/utilities  # Reordenamiento drag-and-drop
pnpm add react-image-crop             # Cropping de imágenes
pnpm add yet-another-react-lightbox   # Lightbox/gallery viewer
```

> Nota: NO se necesita visor panorámico. Un visor 360° de vehículos es un **carrusel de imágenes controlable por drag** (no un panorama esférico).

#### 3.2 Nuevos Componentes — Estructura de Archivos

```
src/components/vehicles/photos/
├── photo-upload-manager.tsx      # Componente principal orquestador
├── photo-dropzone.tsx            # Zona de drag & drop con validación
├── photo-grid.tsx                # Grid de fotos con reordenamiento DnD Kit
├── photo-card.tsx                # Card individual con acciones (primaria, eliminar, crop)
├── photo-category-guide.tsx      # Guía visual de categorías/ángulos
├── photo-quality-indicator.tsx   # Indicador de calidad por foto
├── photo-upload-progress.tsx     # Progreso de upload individual y global
├── photo-crop-modal.tsx          # Modal para cropping antes de subir
├── photo-enhance-modal.tsx       # Modal para remoción de fondo (before/after)
├── photo-lightbox.tsx            # Lightbox para ver fotos en grande
├── upload-queue-manager.ts       # Lógica de cola de uploads paralelos
├── image-compressor.ts           # Wrapper de browser-image-compression
└── index.ts

src/components/vehicles/viewer-360/
├── viewer-360.tsx                # Componente principal del visor 360°
├── viewer-360-controls.tsx       # Controles (play/pause, zoom, fullscreen)
├── viewer-360-capture-guide.tsx  # Guía para capturar fotos/video para 360°
├── viewer-360-from-photos.tsx    # Flujo: seleccionar fotos → generar 360° (pipeline interno)
├── viewer-360-from-video.tsx     # Flujo: grabar/subir video → generar 360° (pipeline interno)
├── viewer-360-thumbnail.tsx      # Preview compacto del 360° en listings
├── viewer-360-processing.tsx     # Estado de procesamiento con progreso
└── index.ts

src/components/vehicles/background-removal/
├── bg-remove-button.tsx          # Botón "🪄 Remover fondo"
├── bg-before-after.tsx           # Slider de comparación antes/después
├── bg-batch-remove.tsx           # Remover fondo de todas las fotos a la vez
├── bg-processing-status.tsx      # Estado de procesamiento background removal
└── index.ts
```

#### 3.3 `photo-upload-manager.tsx` — Orquestador Principal

Componente que gestiona todo el flujo de fotos. Props:

```typescript
interface PhotoUploadManagerProps {
  mode: "individual" | "dealer";
  vehicleId?: string; // Si ya existe el vehículo
  initialImages?: UploadedImage[]; // Imágenes existentes (edición)
  onImagesChange: (images: UploadedImage[]) => void;
  onUploadComplete?: () => void;
  maxPhotos?: number; // Override del máximo
  minPhotos?: number; // Override del mínimo
  showBackgroundRemoval?: boolean; // Mostrar botón de remoción de fondo
  show360Option?: boolean; // Mostrar opción 360°
  className?: string;
}
```

**Tabs/secciones del componente:**

```
┌─────────────────────────────────────────────────────────────────────┐
│  📸 Fotos del Vehículo                                              │
│                                                                      │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────────────┐ │
│  │ 📷 Fotos       │  │ 🔄 Vista 360°  │  │ 📊 Progreso (3/8)     │ │
│  │ Estándar       │  │ (opcional)      │  │ ████████░░░ 62%       │ │
│  └────────┬───────┘  └────────┬───────┘  └────────────────────────┘ │
│           │                   │                                      │
│  ─────────┴───────────────────┘                                      │
│                                                                      │
│  [Contenido del tab activo]                                          │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │  🪄 Remover fondo de fotos  │  Dealers con suscripción / $X   │ │
│  └─────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
```

**Funcionalidades clave:**

1. **Dos modos de upload**: inmediato (al seleccionar foto) o diferido (al publicar)
2. **Cola de uploads paralelos**: max 3 simultáneos para no saturar la red
3. **Compresión automática**: fotos > 2MB se comprimen antes de subir
4. **Categorización inteligente**: sugerir categoría basándose en el orden (1ª = frontal, etc.)
5. **Progreso global**: barra general + status individual por foto
6. **Auto-retry**: si un upload falla, reintentar hasta 3 veces con backoff
7. **Recuperación de sesión**: si el usuario cierra y vuelve, los uploads pendientes se retoman

#### 3.4 `photo-dropzone.tsx` — Zona de Drop Inteligente

Usa `react-dropzone` para una experiencia robusta:

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                  │
│          ┌──────────────────────────────────────┐               │
│          │       📷                              │               │
│          │                                       │               │
│          │  Arrastra tus fotos aquí              │               │
│          │  o haz clic para seleccionar          │               │
│          │                                       │               │
│          │  JPEG, PNG, WebP • Max 15MB por foto  │               │
│          │  Mínimo 3 fotos • Máximo 20 fotos     │               │
│          │                                       │               │
│          │  📱 ¿Desde el celular?                │               │
│          │  [Tomar foto con cámara]              │               │
│          └──────────────────────────────────────┘               │
│                                                                  │
│  💡 Tip: Las publicaciones con 8+ fotos reciben 3x más vistas  │
└─────────────────────────────────────────────────────────────────┘
```

**Funcionalidades:**

- `accept`: `image/jpeg, image/png, image/webp`
- `maxSize`: 15MB (ya que se comprimirá client-side)
- `multiple`: true
- Detectar cámara en móvil → ofrecer opción directa de cámara (`capture="environment"`)
- Estados visuales: idle, hover (drag over), active (archivos sobre la zona), rejected (tipo inválido)
- Animación de pulse cuando está esperando fotos
- Contador de fotos restantes: "Puedes agregar 12 fotos más"

#### 3.5 `photo-grid.tsx` — Grid Reordenable

Usa `@dnd-kit/sortable` para reordenamiento suave:

```
┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐
│ ⭐ 1     │  │    2     │  │    3     │  │    4     │
│ PRINCIPAL│  │          │  │          │  │          │
│ [foto]   │  │ [foto]   │  │ [foto]   │  │ [foto]   │
│          │  │          │  │          │  │          │
│ 📊 95/100│  │ 📊 87/100│  │ 📊 72/100│  │ ⏳ 45%  │
│ ✏️ ✂️ 🗑 │  │ ✏️ ✂️ 🗑 │  │ ✏️ ✂️ 🗑 │  │    ⟳    │
└──────────┘  └──────────┘  └──────────┘  └──────────┘

┌──────────┐  ┌──────────┐  ┌──────────┐
│    5     │  │    6     │  │  + Add   │
│          │  │ ❌ Error │  │          │
│ [foto]   │  │ [foto]   │  │   más    │
│          │  │ Reintentar│  │  fotos   │
│ 📊 91/100│  │ [Retry]  │  │          │
│ ✏️ ✂️ 🗑 │  │    🗑    │  │          │
└──────────┘  └──────────┘  └──────────┘
```

**Funcionalidades:**

- Drag & drop con @dnd-kit (accesible, touch-friendly, con animación)
- Indicador de posición durante el drag (ghost + placeholder)
- Acciones por foto: Marcar principal (⭐), Crop (✂️), Eliminar (🗑), Ver grande (🔍)
- Badge de calidad por foto (score 0-100 del endpoint de validación)
- Estados: uploading (barra de progreso), uploaded (✅), error (❌ con retry), processing (⏳)
- Grid responsive: 4 columnas desktop, 3 tablet, 2 móvil

#### 3.6 `photo-card.tsx` — Card Individual

Cada foto muestra:

```typescript
interface PhotoCardProps {
  image: UploadedImage;
  index: number;
  isPrimary: boolean;
  uploadStatus:
    | "pending"
    | "compressing"
    | "uploading"
    | "uploaded"
    | "processing"
    | "error";
  uploadProgress: number; // 0-100
  qualityScore?: number; // 0-100
  qualityWarnings?: string[]; // ["Imagen borrosa", "Baja resolución"]
  onSetPrimary: () => void;
  onDelete: () => void;
  onCrop: () => void;
  onViewFull: () => void;
  onRetry?: () => void;
}
```

**Estados visuales del card:**

| Estado        | Visual                                | Acciones                      |
| ------------- | ------------------------------------- | ----------------------------- |
| `pending`     | Thumbnail + "En cola"                 | Cancelar                      |
| `compressing` | Thumbnail + "Comprimiendo..." + barra | Cancelar                      |
| `uploading`   | Thumbnail + barra de progreso %       | Cancelar                      |
| `uploaded`    | Thumbnail + ✅ verde                  | Primaria, Crop, Eliminar, Ver |
| `processing`  | Thumbnail + ⏳ "Generando variantes"  | -                             |
| `error`       | Thumbnail + ❌ rojo + mensaje         | Reintentar, Eliminar          |

#### 3.7 `photo-category-guide.tsx` — Guía Inteligente de Categorías

Mejorar la guía existente (`photo-guide.tsx`) con:

```
┌─────────────────────────────────────────────────────────────────┐
│  📋 Guía de Fotos — Sigue este orden para mejores resultados   │
│                                                                  │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐          │
│  │ Frontal │  │ Trasera │  │ Lateral │  │ Lateral │          │
│  │  [icon] │  │  [icon] │  │  Izq    │  │  Der    │          │
│  │  ✅/⬜  │  │  ✅/⬜  │  │  [icon] │  │  [icon] │          │
│  │         │  │         │  │  ✅/⬜  │  │  ✅/⬜  │          │
│  └─────────┘  └─────────┘  └─────────┘  └─────────┘          │
│                                                                  │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐          │
│  │  ¾      │  │  ¾      │  │Interior│  │Tablero │          │
│  │ Fr-Izq  │  │ Fr-Der  │  │  [icon] │  │  [icon] │          │
│  │  [icon] │  │  [icon] │  │  ✅/⬜  │  │  ✅/⬜  │          │
│  │  ✅/⬜  │  │  ✅/⬜  │  │         │  │         │          │
│  └─────────┘  └─────────┘  └─────────┘  └─────────┘          │
│                                                                  │
│  Opcionales:                                                     │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐          │
│  │ Motor  │  │ Llantas │  │Detalles │  │ Daños  │          │
│  │  [icon] │  │  [icon] │  │  [icon] │  │  [icon] │          │
│  │  ✅/⬜  │  │  ✅/⬜  │  │  ✅/⬜  │  │  ✅/⬜  │          │
│  └─────────┘  └─────────┘  └─────────┘  └─────────┘          │
│                                                                  │
│  ✅ 6/8 fotos esenciales completadas                            │
│  💡 Agregar Motor y Llantas aumenta la confianza del comprador  │
└─────────────────────────────────────────────────────────────────┘
```

**Tips contextuales por categoría:**

- **Frontal:** "Foto de frente centrada, de día, a la altura de las luces"
- **Interior:** "Muestra los asientos, consola central y estado general"
- **Motor:** "Genera confianza — los compradores quieren ver el motor"
- **Daños:** "La honestidad genera más consultas — muestra cualquier imperfección"

Cada slot de categoría puede servir como **target de drop** — si el usuario arrastra una foto sobre la categoría "Motor", se asigna automáticamente como `ImageType.Engine`.

#### 3.8 `image-compressor.ts` — Compresión Client-Side

```typescript
import imageCompression from "browser-image-compression";

interface CompressionOptions {
  maxSizeMB: number; // Target size (default: 1.5)
  maxWidthOrHeight: number; // Max dimension (default: 2048)
  useWebWorker: boolean; // Offload to Web Worker (default: true)
  preserveExif: boolean; // Keep EXIF orientation (default: true)
  fileType: string; // Output format (default: original)
  onProgress?: (progress: number) => void;
}

export async function compressImage(
  file: File,
  options?: Partial<CompressionOptions>,
): Promise<{ compressed: File; savings: number; originalSize: number }>;

export function shouldCompress(file: File): boolean;
// Retorna true si file.size > 2MB

export function getCompressionStats(
  original: File,
  compressed: File,
): {
  originalSizeKB: number;
  compressedSizeKB: number;
  savingsPercent: number;
  wasCompressed: boolean;
};
```

**Configuración por contexto:**

| Contexto          | maxSizeMB | maxWidthOrHeight | Razón                              |
| ----------------- | --------- | ---------------- | ---------------------------------- |
| Foto estándar     | 1.5       | 2048             | Balance calidad/velocidad          |
| Foto dealer       | 2.0       | 2560             | Dealers necesitan mejor calidad    |
| Thumbnail preview | 0.3       | 800              | Solo para preview rápido           |
| 360° frames       | 1.0       | 1920             | Muchos frames, necesita ser ligero |

**Beneficios medibles:**

- Foto típica de celular: 5-8MB → 1-2MB (60-80% reducción)
- Upload 3x más rápido en conexiones móviles dominicanas (3G/4G)
- Menor costo de almacenamiento S3

#### 3.9 `upload-queue-manager.ts` — Cola de Uploads Paralelos

```typescript
interface UploadQueueConfig {
  maxConcurrent: number; // 3 simultáneos (ajustable por conexión)
  retryAttempts: number; // 3 reintentos
  retryDelayMs: number; // 1000 * attempt (backoff exponencial)
  usePresignedUrls: boolean; // true = directo a S3 (más rápido)
  compressBeforeUpload: boolean; // true = comprimir client-side primero
  onProgress: (fileId: string, progress: number) => void;
  onComplete: (fileId: string, result: UploadResult) => void;
  onError: (fileId: string, error: Error, retryCount: number) => void;
  onQueueProgress: (completed: number, total: number) => void;
}

class UploadQueueManager {
  // Agregar archivos a la cola
  addFiles(files: File[]): string[]; // Retorna IDs

  // Control de la cola
  start(): void;
  pause(): void;
  resume(): void;
  cancel(fileId: string): void;
  cancelAll(): void;
  retry(fileId: string): void;

  // Estado
  getStatus(): QueueStatus;
  getFileStatus(fileId: string): FileUploadStatus;

  // Detección de red
  detectConnectionSpeed(): Promise<"slow" | "medium" | "fast">;
  adjustConcurrency(speed: string): void; // slow=1, medium=2, fast=3
}
```

**Flujo de upload por archivo:**

```
File seleccionado
  → shouldCompress(file)? → compressImage(file) → file comprimido
  → usePresignedUrls?
    → SÍ: GET presigned URL → PUT directo a S3 → POST finalize
    → NO: POST /api/media/upload/vehicle-image (multipart)
  → onComplete → actualizar UI
  → onError → retry con backoff → 3 intentos → marcar como error
```

**Detección de velocidad de conexión:**

- Usar `navigator.connection` (Network Information API) si disponible
- Fallback: medir tiempo de un request pequeño
- Ajustar concurrencia: 3G → 1 concurrent, 4G → 2, WiFi → 3

#### 3.10 `photo-crop-modal.tsx` — Crop Antes de Subir

Modal con `react-image-crop`:

```
┌─────────────────────────────────────────────┐
│  ✂️ Recortar Imagen                    [✕]  │
│                                              │
│  ┌───────────────────────────────────────┐   │
│  │                                       │   │
│  │        [Imagen con handles de crop]   │   │
│  │        ┌─────────────────┐            │   │
│  │        │   Área de crop  │            │   │
│  │        │   (arrastrable) │            │   │
│  │        └─────────────────┘            │   │
│  │                                       │   │
│  └───────────────────────────────────────┘   │
│                                              │
│  Aspecto: [Libre ▾]  [4:3]  [16:9]  [1:1]   │
│  Rotación: [↺ -90°]  [↻ +90°]               │
│                                              │
│  [Cancelar]              [Aplicar Recorte]   │
└─────────────────────────────────────────────┘
```

- Aspect ratios predefinidos: Libre, 4:3 (estándar auto), 16:9 (wide), 1:1 (cuadrado)
- Rotación en incrementos de 90°
- Preview del resultado antes de aplicar
- El crop genera un nuevo File (canvas → blob) que reemplaza al original

#### 3.11 `photo-lightbox.tsx` — Visualización en Grande

Usa `yet-another-react-lightbox`:

- Abrir al hacer click en cualquier foto del grid
- Navegación con flechas, swipe en móvil
- Zoom con pinch (móvil) o scroll (desktop)
- Contador: "3 de 8 fotos"
- Thumbnails en la parte inferior
- Botón de fullscreen

### 4. Frontend — Vista 360° Interactiva

#### 4.1 `viewer-360.tsx` — Componente Principal del Visor

Un visor 360° de vehículos **NO es un panorama esférico** — es un **carrusel controlable por drag** que muestra el vehículo desde múltiples ángulos. Se implementa con imágenes secuenciales (frames) que cambian según la posición del cursor/dedo.

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                  │
│                    [Imagen del vehículo]                         │
│                    (cambia al arrastrar)                         │
│                                                                  │
│  ◀── Arrastra para rotar ──▶                                    │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ ● ● ● ● ● ● ● ●|● ● ● ● ● ● ● ● ● ● ● ● ● ● ● ● ● │    │
│  │          ▲ Frame actual                                  │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
│  [▶ Auto-rotar]  [🔍 Zoom]  [⛶ Pantalla completa]              │
└─────────────────────────────────────────────────────────────────┘
```

**Implementación técnica (Canvas + preloaded images):**

```typescript
interface Viewer360Props {
  frames: string[]; // URLs de las imágenes (6-36 frames)
  initialFrame?: number; // Frame inicial (0)
  autoRotate?: boolean; // Auto-rotación (true)
  autoRotateSpeed?: number; // RPM (3)
  allowZoom?: boolean; // Zoom habilitado (true)
  maxZoom?: number; // Zoom máximo (3x)
  invertDrag?: boolean; // Invertir dirección del drag
  showControls?: boolean; // Mostrar controles (true)
  width?: number | string; // Ancho del viewer
  height?: number | string; // Alto del viewer
  thumbnailUrl?: string; // Imagen de carga mientras precarga
  onFrameChange?: (frame: number) => void;
  onInteractionStart?: () => void;
  onInteractionEnd?: () => void;
  className?: string;
}
```

**Lógica core:**

1. **Precargar frames**: Al montar, cargar todas las imágenes en `Image()` objects
2. **Mostrar thumbnail** mientras se precargan
3. **Barra de progreso** de precarga: "Cargando vista 360°... 45%"
4. **Mouse/Touch drag**: Calcular delta X → mapear a frame index
   - `frameIndex = Math.floor((deltaX / containerWidth) * totalFrames) % totalFrames`
5. **Auto-rotación**: `requestAnimationFrame` loop que avanza frames
   - Se detiene cuando el usuario interactúa
   - Se reanuda tras 3 segundos de inactividad
6. **Zoom**: CSS transform scale + pan con drag
7. **Momentum/inercia**: Al soltar el drag, continuar brevemente con desaceleración
8. **Responsive**: Redimensionar canvas al tamaño del contenedor

**Consideraciones de rendimiento:**

- Precargar frames en batches (primero 1 de cada 4, luego los intermedios) → **progressive loading**
- Usar `<canvas>` para rendering (más eficiente que cambiar `<img src>`)
- `will-change: transform` para GPU acceleration en zoom
- En móvil: reducir frames mostrados si hay lentitud (detectar FPS < 30)
- Cache frames en `Map<number, HTMLImageElement>` para acceso O(1)

#### 4.2 `viewer-360-capture-guide.tsx` — Guía de Captura

```
┌─────────────────────────────────────────────────────────────────┐
│  🔄 Crear Vista 360° de tu Vehículo                            │
│                                                                  │
│  Elige tu método preferido:                                      │
│                                                                  │
│  ┌──────────────────────────┐  ┌──────────────────────────┐    │
│  │ 📷 Desde Fotos           │  │ 🎬 Desde Video            │    │
│  │                          │  │                           │    │
│  │ Toma 8-36 fotos          │  │ Graba un video de         │    │
│  │ alrededor del vehículo   │  │ 30-60 segundos            │    │
│  │                          │  │ caminando alrededor        │    │
│  │ Mejor calidad            │  │ del vehículo              │    │
│  │ Más control              │  │                           │    │
│  │ ~5 min                   │  │ Más rápido y fácil        │    │
│  │                          │  │ ~2 min                    │    │
│  │ [Seleccionar fotos]      │  │ [Grabar/Subir video]      │    │
│  └──────────────────────────┘  └──────────────────────────┘    │
│                                                                  │
│  📐 Tips para mejores resultados:                                │
│  • Estaciona en un área abierta y bien iluminada                │
│  • Mantén la misma distancia al vehículo en cada posición       │
│  • Muévete en sentido horario, uniformemente                    │
│  • Evita que personas u objetos bloqueen la vista               │
│  • Mantén la cámara a la altura de los faros                    │
│                                                                  │
│  [Diagrama animado mostrando posiciones de captura]              │
└─────────────────────────────────────────────────────────────────┘
```

#### 4.3 `viewer-360-from-photos.tsx` — Flujo de 360° desde Fotos

Envía las fotos al pipeline interno (Vehicle360ProcessingService → BackgroundRemovalService):

1. Fotos se suben a MediaService (S3)
2. Se envía request a `POST /api/vehicle360/process` con las URLs
3. Vehicle360ProcessingService coordina la remoción de fondos de cada frame
4. Polling de status hasta completar
5. Al completar: viewer 360° disponible

```
┌─────────────────────────────────────────────────────────────────┐
│  📷 Vista 360° desde Fotos                                      │
│                                                                  │
│  Paso 1: Selecciona las fotos (mínimo 8, ideal 24-36)           │
│                                                                  │
│  ┌───────────────────────────────────────────────────────┐      │
│  │  [grid de fotos seleccionadas, numeradas 1-N]         │      │
│  │  Cada posición muestra el ángulo esperado              │      │
│  │                                                        │      │
│  │  1(0°) 2(45°) 3(90°) 4(135°) 5(180°) 6(225°) ...     │      │
│  │                                                        │      │
│  │  Las fotos se pueden reordenar arrastrando             │      │
│  └───────────────────────────────────────────────────────┘      │
│                                                                  │
│  Paso 2: Configuración                                           │
│  ☑ Remover fondo (background blanco limpio)                      │
│  ☑ Corregir exposición automáticamente                           │
│                                                                  │
│  💰 Costo estimado: ~$0.30 (6 frames × $0.05 ClipDrop)          │
│                                                                  │
│  [Generar Vista 360°]                                            │
└─────────────────────────────────────────────────────────────────┘
```

#### 4.4 `viewer-360-from-video.tsx` — Flujo de 360° desde Video

```
┌─────────────────────────────────────────────────────────────────┐
│  🎬 Vista 360° desde Video                                      │
│                                                                  │
│  Paso 1: Graba o sube un video                                   │
│                                                                  │
│  ┌───────────────────────────────────────────────────────┐      │
│  │                                                        │      │
│  │  📱 En móvil: [Grabar Video]                           │      │
│  │       Cámara se abre con guía de grabación             │      │
│  │       "Camina lentamente alrededor del vehículo"       │      │
│  │       Indicador de ángulo recorrido (0° → 360°)        │      │
│  │                                                        │      │
│  │  💻 En desktop: [Subir Video]                          │      │
│  │       Drag & drop o seleccionar archivo                │      │
│  │       MP4/MOV, 30-90 segundos, max 500MB               │      │
│  │                                                        │      │
│  └───────────────────────────────────────────────────────┘      │
│                                                                  │
│  Paso 2: Preview del video                                       │
│  [Video player con preview]                                      │
│  Duración: 45s ✅  |  Tamaño: 120MB ✅  |  Calidad: HD ✅      │
│                                                                  │
│  Paso 3: Configuración                                           │
│  Frames a extraer: [6 ▾] (más frames = rotación más suave)      │
│  ☑ Remover fondo                                                 │
│  ☑ Corrección de exposición                                      │
│                                                                  │
│  💰 Costo estimado: ~$0.31 ($0.011 FFmpeg-API + 6 × $0.05)      │
│                                                                  │
│  [Procesar Video → 360°]                                         │
│                                                                  │
│  ⏳ Procesando... (esto puede tomar 2-5 minutos)                │
│  ████████████░░░░░░░░ 55%                                        │
│  Extrayendo frames: 4/6                                          │
└─────────────────────────────────────────────────────────────────┘
```

**Flujo técnico:**

1. Usuario graba/sube video
2. Upload del video a `POST /api/vehicle360/upload` (pipeline interno)
3. Vehicle360ProcessingService orquesta:
   - Video360Service (FFmpeg-API) extrae 6 frames del video
   - BackgroundRemovalService (ClipDrop) remueve el fondo de cada frame
   - MediaService sube los frames procesados a S3
4. Polling de status cada 5 segundos via `GET /api/vehicle360/jobs/{id}/status`
5. Mostrar progreso con etapas: "Subiendo video" → "Extrayendo frames" → "Removiendo fondos" → "Subiendo resultados"
6. Al completar: mostrar preview del 360° con `viewer-360.tsx`
7. Botón "Agregar al vehículo" → `POST /api/vehicles/{vehicleId}/360-view`

#### 4.5 `viewer-360-thumbnail.tsx` — Preview en Listings

Para el detalle del vehículo y cards de listing, mostrar un indicador de que tiene 360°:

```
┌────────────────────────┐
│  [Foto del vehículo]   │
│                        │
│  ┌──────────────────┐  │
│  │ 🔄 Ver en 360°   │  │  ← Badge clickeable sobre la foto
│  └──────────────────┘  │
│                        │
└────────────────────────┘
```

Al hacer click, se abre el viewer-360 en un modal o reemplaza el carrusel de fotos.

#### 4.6 `viewer-360-processing.tsx` — Estado de Procesamiento

```
┌─────────────────────────────────────────────────────────────────┐
│  🔄 Procesando Vista 360°                                       │
│                                                                  │
│  ████████████████░░░░░░░░░░░░░░░░ 45%                           │
│                                                                  │
│  Etapa actual: Extrayendo frames del video                       │
│                                                                  │
│  ✅ Video subido                                                 │
│  ✅ Video validado                                               │
│  ⏳ Extrayendo frames (4/6) — FFmpeg-API                        │
│  ○ Removiendo fondos — ClipDrop                                  │
│  ○ Subiendo resultados a S3                                      │
│  ○ Generando vista interactiva                                   │
│                                                                  │
│  ⏱️ Tiempo estimado restante: ~3 minutos                        │
│                                                                  │
│  💡 Puedes continuar con el resto de la publicación              │
│     mientras se procesa la vista 360°                            │
│                                                                  │
│  [Cancelar procesamiento]                                        │
└─────────────────────────────────────────────────────────────────┘
```

**Importante:** El procesamiento 360° es **asíncrono** y NO bloquea la publicación del vehículo. El usuario puede publicar sin esperar que el 360° esté listo. Cuando termine, se asocia automáticamente al vehículo.

### 5. Frontend — Remoción de Fondos con Pipeline Interno

#### 5.1 `bg-remove-button.tsx`

```
[🪄 Remover fondo]
```

- Visible para dealers con suscripción activa (verificar con `useCurrentDealer()`)
- Para individuales: 1 foto gratis, luego mostrar como upsell "Fondo profesional desde $X por foto"
- Al hacer click, envía la imagen a `POST /api/backgroundremoval/remove`
- Muestra spinner mientras procesa (típicamente 5-15 segundos)
- Al completar, muestra `bg-before-after.tsx` para comparar

#### 5.2 `bg-before-after.tsx` — Comparación Slider

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                  │
│  ANTES              |              DESPUÉS                       │
│                     |                                            │
│  [Foto original     | [Foto con fondo                           │
│   con fondo de      |  blanco limpio,                           │
│   garaje/calle]     |  profesional]                             │
│                     |                                            │
│        ◄── Arrastra para comparar ──►                            │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

Implementar con CSS `clip-path` y drag handle. El usuario arrastra el divisor para comparar el antes y después.

#### 5.3 `bg-batch-remove.tsx`

Para procesamiento batch de todas las fotos:

```
┌─────────────────────────────────────────────────────────────────┐
│  🪄 Remover Fondo de Todas las Fotos                            │
│                                                                  │
│  Proveedor: ClipDrop (recomendado)                              │
│  Costo estimado: 6 fotos × $0.05 = $0.30                        │
│                                                                  │
│  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐       │
│  │foto1 │ │foto2 │ │foto3 │ │foto4 │ │foto5 │ │foto6 │       │
│  │ ✅   │ │ ⏳   │ │ ⏳   │ │ ○    │ │ ○    │ │ ○    │       │
│  └──────┘ └──────┘ └──────┘ └──────┘ └──────┘ └──────┘       │
│                                                                  │
│  Procesando: 2 de 6 fotos                                        │
│  ████████████░░░░░░░░░░░░░░░░ 33%                               │
│                                                                  │
│  [Cancelar]                        [Remover Fondos]              │
└─────────────────────────────────────────────────────────────────┘
```

#### 5.4 `bg-processing-status.tsx`

Estado de procesamiento para una imagen individual:

```
┌───────────────────────────────────────┐
│  🪄 Removiendo fondo...               │
│  ████████████████░░░░░ 75%            │
│  Proveedor: ClipDrop                   │
│  ⏱️ ~5 segundos restantes             │
└───────────────────────────────────────┘
```

- Si ClipDrop falla, muestra: "Reintentando con proveedor alternativo..." (fallback chain automático en backend)
- Polly resilience en backend maneja los reintentos transparentemente

### 6. Frontend — Services y Hooks Nuevos

#### 6.1 Agregar a `services/media.ts`

```typescript
// Pre-signed URLs para upload directo a S3
getPresignedUrls(files: FileInfo[], vehicleId?: string): Promise<PresignedUrlResponse[]>

// Upload optimizado para fotos de vehículos
uploadVehicleImage(file: File, options: VehicleImageUploadOptions): Promise<VehicleImageUploadResponse>

// Upload batch
uploadVehicleImagesBatch(files: File[]): Promise<VehicleImageUploadResponse[]>

// Validación de calidad
validateImageQuality(file: File): Promise<ImageQualityResult>

// Finalizar upload pre-signed
finalizeUpload(mediaId: string): Promise<void>
```

#### 6.2 Nuevo `services/background-removal.ts`

```typescript
// Remover fondo de una imagen
removeBackground(imageUrl: string): Promise<BackgroundRemovalJob>

// Batch de remoción de fondos (hasta 10 imágenes)
removeBackgroundBatch(imageUrls: string[]): Promise<BackgroundRemovalJob>

// Consultar estado del procesamiento
getRemovalStatus(jobId: string): Promise<BackgroundRemovalStatus>

// Obtener resultado con URL de imagen procesada
getRemovalResult(jobId: string): Promise<BackgroundRemovalResult>
```

**Tipos:**

```typescript
interface BackgroundRemovalJob {
  jobId: string;
  status: "pending" | "processing" | "completed" | "failed";
  provider: string; // "clipdrop" | "slazzer" | "photoroom" | "removebg"
  estimatedTimeSeconds: number;
}

interface BackgroundRemovalResult {
  jobId: string;
  originalUrl: string;
  processedUrl: string; // URL de imagen con fondo removido
  provider: string;
  processingTimeMs: number;
}
```

#### 6.3 Nuevo `services/vehicle360.ts`

```typescript
// Upload y procesamiento de video (servicio interno)
uploadVideo(file: File, vehicleId: string): Promise<ProcessingJob>
processVideo(storageUrl: string, vehicleId: string, options: ProcessingOptions): Promise<ProcessingJob>
getJobStatus(jobId: string): Promise<JobStatus>
getJobResult(jobId: string): Promise<ProcessingResult>
retryJob(jobId: string): Promise<ProcessingJob>
cancelJob(jobId: string): Promise<void>

// Datos del viewer
getViewerData(vehicleId: string): Promise<ViewerData>

// Historial
getVehicleJobs(vehicleId: string): Promise<ProcessingJob[]>
getUserJobs(userId: string, page: number): Promise<PaginatedResult<ProcessingJob>>

// Health check de servicios dependientes
checkHealth(): Promise<HealthStatus>
```

#### 6.4 Nuevos Hooks

```typescript
// hooks/use-media.ts — agregar
useUploadVehicleImage(); // mutation
useUploadVehicleImagesBatch(); // mutation
useGetPresignedUrls(); // mutation
useValidateImageQuality(file); // query (enabled cuando hay file)

// hooks/use-background-removal.ts — nuevo
useRemoveBackground(); // mutation
useBatchRemoveBackground(); // mutation
useRemovalStatus(jobId); // query con polling
useRemovalResult(jobId); // query (enabled cuando status=completed)

// hooks/use-vehicle360.ts — nuevo
useUpload360Video(); // mutation
useProcess360Video(); // mutation
use360JobStatus(jobId); // query con polling (refetchInterval: 5s)
use360JobResult(jobId); // query (enabled cuando status=Completed)
use360ViewerData(vehicleId); // query
use360VehicleJobs(vehicleId); // query
useRetry360Job(); // mutation
useCancel360Job(); // mutation
```

**Polling con React Query:**

```typescript
use360JobStatus(jobId, {
  refetchInterval: (data) =>
    data?.status === "Completed" || data?.status === "Failed"
      ? false // Detener polling
      : 5000, // Cada 5 segundos
});

useRemovalStatus(jobId, {
  refetchInterval: (data) =>
    data?.status === "completed" || data?.status === "failed" ? false : 3000, // Cada 3 segundos (background removal es más rápido)
});
```

### 7. Integración con el Smart Publish Wizard

#### 7.1 Modificar `photo-upload-step.tsx` existente

El componente actual debe ser reemplazado por `PhotoUploadManager` que incluye:

1. **Tab "Fotos Estándar"** — El flujo mejorado con compresión, uploads paralelos, categorización
2. **Tab "Vista 360°"** — Capture guide + flujos desde fotos/video (pipeline interno)
3. **Barra de Background Removal** — Solo visible para dealers, con botón "🪄 Remover fondo"

#### 7.2 Modificar `review-step.tsx` — Mostrar 360° en Preview

Si el vehículo tiene una vista 360°, el paso de revisión debe mostrar:

- El viewer-360 interactivo
- Badge "🔄 Vista 360° incluida" en el listing quality score
- Si está procesando: "Vista 360° procesando — se agregará automáticamente cuando esté lista"

#### 7.3 Modificar `listing-quality-score.tsx` — Puntaje Actualizado

| Criterio                           | Puntos  | Detalle                        |
| ---------------------------------- | ------- | ------------------------------ |
| Tiene fotos                        | 20/100  | Base (min 3)                   |
| 8+ fotos con categorías            | +15/100 | Todas las categorías cubiertas |
| Fotos de alta calidad (score > 70) | +10/100 | Promedio de quality score      |
| Vista 360° incluida                | +10/100 | Bonus por 360°                 |
| Fotos con fondo removido           | +5/100  | Bonus por background removal   |
| Descripción completa               | +15/100 | > 150 caracteres               |
| Precio en rango de mercado         | +10/100 | Dentro del rango sugerido      |
| VIN verificado                     | +10/100 | VIN decodificado               |
| Información completa               | +5/100  | Todos los campos llenos        |

### 8. Detalle del Vehículo — Mostrar 360° en la Página Pública

#### 8.1 Modificar página de detalle `/vehiculos/[slug]/page.tsx`

En el carrusel de imágenes del detalle del vehículo:

```
┌─────────────────────────────────────────────────────────────────┐
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐         │
│  │ 📷 Fotos (8) │  │ 🔄 360° (1)  │  │ 🎬 Video (1) │         │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘         │
│         │                 │                  │                  │
│  ───────┴─────────────────┘                  │                  │
│                                              │                  │
│  [Si tab "360°" activo:]                                        │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │                                                          │  │
│  │         [Viewer 360° interactivo full-width]             │  │
│  │                                                          │  │
│  │  ◀── Arrastra para rotar ──▶                             │  │
│  │                                                          │  │
│  │  [▶ Auto]  [🔍 Zoom]  [⛶ Full]                          │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                  │
│  [Si tab "Fotos" activo:]                                        │
│  [Carrusel normal de fotos con lightbox]                         │
│                                                                  │
│  [Si tab "Video" activo:]                                        │
│  [Video player]                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔒 SEGURIDAD — Obligatorio

### Backend

- `.NoSqlInjection()` y `.NoXss()` en TODOS los validators de strings nuevos
- **Magic bytes validation** en MediaService (ya existe, verificar que se mantiene)
- **Extension blocklist** actualizada (bloquear SVG, HTML, JS, PHP, EXE)
- **Content-type whitelist**: solo `image/jpeg`, `image/png`, `image/webp`, `video/mp4`, `video/webm`, `video/quicktime`
- **Rate limiting** en upload endpoints: 20 req/min para uploads, 5 req/min para batch
- **Tamaño máximo**: 15MB por imagen, 500MB por video
- **Pre-signed URL expiration**: 15 minutos (mínimo necesario)
- **Audit logging**: registrar cada upload, delete, y procesamiento de background removal
- **Ownership validation**: solo el owner puede eliminar/modificar sus media

### Frontend

- **Validación client-side ANTES de upload**: tipo, tamaño, dimensiones
- Usar `sanitizeFilename()` antes de enviar nombres de archivo
- Usar `csrfFetch()` para TODAS las mutaciones (POST, PUT, DELETE)
- **No renderizar URLs de usuario** sin `sanitizeUrl()`
- **No renderizar alt text** sin `escapeHtml()`
- **Validar File type** por magic bytes (no solo extensión): `file.slice(0, 4)` → verificar headers
- **Canvas taint protection**: no exportar canvas con imágenes cross-origin sin CORS

---

## 📱 RESPONSIVE & UX

### Mobile-first

- **Cámara directa**: En móvil, el botón principal debe ser "Tomar foto" (no "Seleccionar archivo")
- **Upload al tomar**: Cada foto se sube inmediatamente al tomarla (no esperar a seleccionar todas)
- **Feedback háptico**: Vibración sutil al completar upload (si soportado: `navigator.vibrate`)
- **Progreso visible**: La barra de progreso debe ser sticky durante scroll
- **Touch drag**: Reordenamiento de fotos con long-press + drag (no solo desktop drag)
- **Grid adaptativo**: 2 columnas en móvil, 3 en tablet, 4 en desktop
- **Viewer 360° touch**: Swipe horizontal para rotar, pinch para zoom

### Feedback y Micro-interacciones

- **Drop zone**: Animación de pulse cuando hay archivos sobre ella
- **Upload completado**: Animación de check verde (✅) con lottie/framer-motion
- **Error de upload**: Shake animation + color rojo
- **Reordenamiento**: Smooth animation con @dnd-kit
- **Compresión**: Mostrar ahorro "Comprimida: 5.2MB → 1.4MB (73% ahorrado)"
- **Quality score**: Animación de gauge/meter
- **360° rotation**: Inercia suave al soltar el drag
- **Background removal**: Slider before/after con animación de reveal

### Accesibilidad

- **ARIA labels** en todos los botones: "Marcar como foto principal", "Eliminar foto 3 de 8"
- **Keyboard navigation**: Tab entre fotos, Enter para acciones, Delete para eliminar
- **Screen reader**: Anunciar progreso de upload, errores, completados
- **Alt text automático**: Generar alt text basado en categoría: "Vista frontal del 2024 Toyota Camry"
- **Focus management**: Después de eliminar una foto, focus va a la siguiente
- **Reduced motion**: Respetar `prefers-reduced-motion` para animaciones

### Idioma

Toda la UI en **español** (República Dominicana). Ejemplos de textos:

- "Arrastra tus fotos aquí o haz clic para seleccionar"
- "Comprimiendo imagen... 5.2MB → 1.4MB"
- "Subiendo foto 3 de 8..."
- "Vista 360° procesando — esto puede tomar unos minutos"
- "🪄 Remover fondo de las fotos"
- "Las publicaciones con 8+ fotos reciben 3 veces más contactos"
- "Fondo removido exitosamente — $0.05 por foto"

---

## 📁 ARCHIVOS A CREAR/MODIFICAR

### Nuevos Archivos — Backend

```
backend/MediaService/MediaService.Workers/Handlers/
  └── ImageProcessingHandler.cs         # IMPLEMENTAR (actualmente vacío)
  └── MediaCleanupHandler.cs            # IMPLEMENTAR (actualmente vacío)

backend/MediaService/MediaService.Api/Controllers/
  └── (Modificar MediaController.cs — agregar vehicle-image upload, presigned-urls batch, quality validation)

backend/MediaService/MediaService.Application/Features/Media/Commands/
  └── UploadVehicleImage/               # Nuevo command
  └── UploadVehicleImagesBatch/         # Nuevo command
  └── GetPresignedUrlsBatch/            # Nuevo command
  └── ValidateImageQuality/             # Nuevo command/query

backend/VehiclesSaleService/.../
  └── (Crear entidad Vehicle360View.cs)
  └── (Modificar Vehicle.cs — agregar navigation property View360)
  └── (Modificar/Crear endpoints para asociar 360° al vehículo)
  └── (Crear migración de BD)

backend/Video360Service/                # ⚠️ RESTAURAR desde backend/_DESCARTADOS/Video360Service/
  └── (Verificar que los endpoints que espera Vehicle360ProcessingService existen)
  └── (Verificar configuración de providers: FFmpeg-API, ApyHub, etc.)

backend/BackgroundRemovalService/       # Ya existe (55 archivos)
  └── (Verificar endpoints: /remove, /batch, /{id}/status, /{id}/result)
  └── (Verificar configuración de providers: ClipDrop, Slazzer, etc.)
  └── (Verificar API keys en configuración)
```

### Nuevos Archivos — Frontend

```
frontend/web-next/src/components/vehicles/photos/
├── photo-upload-manager.tsx
├── photo-dropzone.tsx
├── photo-grid.tsx
├── photo-card.tsx
├── photo-category-guide.tsx
├── photo-quality-indicator.tsx
├── photo-upload-progress.tsx
├── photo-crop-modal.tsx
├── photo-enhance-modal.tsx
├── photo-lightbox.tsx
├── upload-queue-manager.ts
├── image-compressor.ts
└── index.ts

frontend/web-next/src/components/vehicles/viewer-360/
├── viewer-360.tsx
├── viewer-360-controls.tsx
├── viewer-360-capture-guide.tsx
├── viewer-360-from-photos.tsx
├── viewer-360-from-video.tsx
├── viewer-360-thumbnail.tsx
├── viewer-360-processing.tsx
└── index.ts

frontend/web-next/src/components/vehicles/background-removal/
├── bg-remove-button.tsx
├── bg-before-after.tsx
├── bg-batch-remove.tsx
├── bg-processing-status.tsx
└── index.ts

frontend/web-next/src/services/
├── background-removal.ts          # NUEVO — API client para BackgroundRemovalService
├── vehicle360.ts                   # NUEVO — API client para Vehicle360ProcessingService
└── media.ts                        # MODIFICAR — agregar vehicle-image endpoints

frontend/web-next/src/hooks/
├── use-background-removal.ts       # NUEVO — React Query hooks para background removal
├── use-vehicle360.ts               # NUEVO — React Query hooks para 360°
└── use-media.ts                    # MODIFICAR — agregar nuevos hooks
```

### Paquetes npm a Instalar (pnpm)

```bash
pnpm add browser-image-compression    # Compresión client-side de imágenes
pnpm add react-dropzone               # Drag & drop robusto
pnpm add @dnd-kit/core @dnd-kit/sortable @dnd-kit/utilities  # DnD reordenamiento
pnpm add react-image-crop             # Cropping de imágenes
pnpm add yet-another-react-lightbox   # Lightbox para ver fotos en grande
```

> Notas:
>
> - `react-webcam` ya está instalado (se usa en KYC y VIN scanner)
> - NO se necesita `pannellum` ni `three.js` — el viewer 360° se implementa con canvas + imágenes secuenciales
> - NO se necesita `ffmpeg.wasm` — la extracción de frames la hace el backend (Video360Service)

### Gateway — Rutas a Verificar/Agregar

```
/api/media/*              → mediaservice               (ya configurado)
/api/vehicle360/*         → vehicle360processingservice  (ya configurado, timeout 300s)
/api/backgroundremoval/*  → backgroundremovalservice     (AGREGAR si no existe, timeout 180s)
/api/vehicles/*           → vehiclessaleservice          (ya configurado)
```

⚠️ Verificar que los timeouts son adecuados:

- `/api/media/upload/*` → 60s (para uploads grandes)
- `/api/backgroundremoval/*` → 180s (procesamiento de remoción de fondo)
- `/api/vehicle360/upload` → 300s (video upload grande + procesamiento)

---

## ✅ CRITERIOS DE ACEPTACIÓN

### Funcionales — Fotos Estándar

- [ ] Las fotos se comprimen client-side antes de subir (fotos > 2MB)
- [ ] Los uploads son paralelos (max 3 simultáneos)
- [ ] Cada foto muestra progreso individual y hay progreso global
- [ ] Las fotos se pueden reordenar arrastrando (desktop y mobile)
- [ ] Se puede marcar una foto como principal
- [ ] Se puede eliminar una foto individual
- [ ] Se puede recortar una foto antes de subir
- [ ] Se puede ver una foto en lightbox (tamaño completo)
- [ ] La guía de categorías muestra qué ángulos faltan
- [ ] Si un upload falla, se puede reintentar individualmente
- [ ] Los uploads fallidos no bloquean los exitosos
- [ ] Se muestran sugerencias de calidad por foto (borrosa, oscura, etc.)
- [ ] Las variantes (thumbnails) se generan automáticamente después del upload
- [ ] En móvil, el botón principal es "Tomar foto con cámara"
- [ ] El sistema detecta la velocidad de conexión y ajusta la concurrencia

### Funcionales — Vista 360°

- [ ] El usuario puede crear un 360° desde fotos múltiples (mínimo 8)
- [ ] El usuario puede crear un 360° subiendo un video walk-around
- [ ] El procesamiento 360° es asíncrono y muestra progreso con etapas (frames, bg removal, upload)
- [ ] El procesamiento 360° NO bloquea la publicación del vehículo
- [ ] El pipeline usa Video360Service (FFmpeg-API) para extracción de frames
- [ ] El pipeline usa BackgroundRemovalService (ClipDrop) para remoción de fondos
- [ ] El viewer 360° permite rotar arrastrando horizontalmente
- [ ] El viewer 360° tiene auto-rotación que se pausa al interactuar
- [ ] El viewer 360° soporta zoom
- [ ] El viewer 360° soporta fullscreen
- [ ] El viewer 360° funciona con touch en móvil (swipe + pinch zoom)
- [ ] Los frames del 360° se precargan progresivamente
- [ ] En el detalle del vehículo público, se muestra el tab "360°" si existe
- [ ] Badge "🔄 360°" visible en cards de listing que tienen vista 360°

### Funcionales — Remoción de Fondos (Pipeline Interno)

- [ ] Los dealers con suscripción pueden remover fondos con un click
- [ ] Se muestra slider before/after para comparar resultado
- [ ] El batch processing muestra progreso individual por foto
- [ ] Los individuales tienen 1 remoción gratis, luego ven pricing
- [ ] El fallback chain funciona transparentemente (si ClipDrop falla, usa Remove.bg, etc.)
- [ ] Se muestra el costo estimado antes de procesar ($0.05/foto con ClipDrop)
- [ ] El procesamiento toma típicamente 5-15 segundos por imagen

### No Funcionales

- [ ] Un upload típico (foto de 5MB con celular) toma menos de 3 segundos (incluyendo compresión)
- [ ] El viewer 360° carga en menos de 5 segundos (progressive loading)
- [ ] El viewer 360° mantiene 30+ FPS al rotar
- [ ] Los thumbnails se generan en menos de 10 segundos después del upload
- [ ] Todos los inputs están sanitizados
- [ ] CSRF protection en todas las mutaciones
- [ ] Audit logging de uploads y procesamiento
- [ ] Los textos están en español
- [ ] WCAG 2.1 AA compliance
- [ ] Funciona en Chrome, Safari, Firefox (últimas 2 versiones)
- [ ] Funciona en iOS Safari y Chrome Android
- [ ] Costo del pipeline 360° ≤ $0.35/vehículo (Tier Recomendado)

### Testing

- [ ] Tests unitarios para image-compressor.ts
- [ ] Tests unitarios para upload-queue-manager.ts
- [ ] Tests de componentes para photo-dropzone, photo-grid, photo-card
- [ ] Tests de componentes para viewer-360
- [ ] Tests de componentes para bg-before-after, bg-batch-remove
- [ ] Tests de integración para el flujo completo de upload
- [ ] Tests backend para ImageProcessingHandler
- [ ] Tests backend para el endpoint de quality validation
- [ ] Tests backend para BackgroundRemovalService (mock providers)

---

## 🚀 ORDEN DE IMPLEMENTACIÓN SUGERIDO

### Fase 1 — Backend Core: Procesamiento de Imágenes (Prioridad CRÍTICA)

1. Implementar `ImageProcessingHandler` en MediaService.Workers (genera variantes reales)
2. Implementar `MediaCleanupHandler` en MediaService.Workers (limpieza de huérfanos)
3. Endpoint `POST /api/media/upload/vehicle-image` (upload optimizado)
4. Endpoint `POST /api/media/upload/presigned-urls` (batch pre-signed URLs)
5. Endpoint `POST /api/media/validate/quality` (validación de calidad)

### Fase 2 — Frontend Core: Upload Mejorado (Prioridad ALTA)

6. `image-compressor.ts` — compresión client-side
7. `upload-queue-manager.ts` — cola paralela con retry
8. `photo-dropzone.tsx` — zona de drop con react-dropzone
9. `photo-card.tsx` — card individual con estados
10. `photo-grid.tsx` — grid reordenable con @dnd-kit
11. `photo-upload-manager.tsx` — orquestador principal
12. Integrar con Smart Publish Wizard (reemplazar photo-upload-step actual)

### Fase 3 — Frontend: UX Polish (Prioridad ALTA)

13. `photo-category-guide.tsx` — guía mejorada con drag targets
14. `photo-quality-indicator.tsx` — indicador de calidad por foto
15. `photo-crop-modal.tsx` — crop antes de subir
16. `photo-lightbox.tsx` — ver fotos en grande
17. `photo-upload-progress.tsx` — progreso global sticky

### Fase 4 — Backend 360°: Entidad, Endpoints y Servicios (Prioridad MEDIA)

18. Restaurar Video360Service desde `_DESCARTADOS` → verificar endpoints y providers
19. Verificar BackgroundRemovalService: endpoints, providers, API keys
20. Verificar Vehicle360ProcessingService: orquestador, HTTP clients, pipeline
21. Crear entidad `Vehicle360View` en VehiclesSaleService
22. Endpoints CRUD para 360° en VehiclesSaleService
23. Agregar rutas al Gateway para BackgroundRemovalService (si no existe)

### Fase 5 — Frontend 360°: Viewer y Captura (Prioridad MEDIA)

24. `viewer-360.tsx` — componente principal del visor
25. `viewer-360-controls.tsx` — controles interactivos
26. `viewer-360-capture-guide.tsx` — guía de captura
27. `viewer-360-from-photos.tsx` — flujo desde fotos (pipeline interno)
28. `viewer-360-from-video.tsx` — flujo desde video (pipeline interno)
29. `viewer-360-thumbnail.tsx` — preview en listings
30. `viewer-360-processing.tsx` — estado de procesamiento con etapas
31. Integrar viewer en página de detalle del vehículo

### Fase 6 — Frontend: Remoción de Fondos (Prioridad MEDIA)

32. `services/background-removal.ts` y `hooks/use-background-removal.ts`
33. `bg-remove-button.tsx` — botón con verificación de cuenta
34. `bg-before-after.tsx` — slider comparación
35. `bg-batch-remove.tsx` — batch processing con costo estimado
36. `bg-processing-status.tsx` — estado con fallback info

### Fase 7 — Polish y Analytics (Prioridad BAJA)

37. Event tracking: upload_photo, upload_360, bg_remove, quality_score
38. Actualizar listing-quality-score con puntos de fotos, 360° y bg removal
39. Tests completos (frontend + backend)
40. Optimización de rendimiento del viewer 360°

---

## ⚠️ NOTAS IMPORTANTES

1. **Package manager:** Usar SIEMPRE `pnpm` (NO npm, NO yarn)
2. **Workers vacíos:** `MediaService.Workers` tiene handlers vacíos que DEBEN implementarse — sin esto no hay thumbnails reales
3. **Pipeline interno único:** Se usa SOLO el pipeline interno (Vehicle360ProcessingService → Video360Service + BackgroundRemovalService). NO hay integración con APIs externas monolíticas tipo Spyne
4. **Video360Service en \_DESCARTADOS:** El servicio está en `backend/_DESCARTADOS/Video360Service/` — debe restaurarse al directorio principal de backend. Verificar que los endpoints que Vehicle360ProcessingService espera existen
5. **Providers configurables:** Tanto Video360Service como BackgroundRemovalService soportan múltiples providers con fallback automático. El tier por defecto es "Recomendado" (FFmpeg-API + ClipDrop = ~$0.31/vehículo)
6. **Upload pre-signed:** La opción más eficiente es pre-signed URLs (browser → S3 directo), pero requiere CORS configurado en el bucket S3
7. **Compresión obligatoria:** En RD muchos usuarios tienen conexiones lentas (3G/4G) — la compresión client-side es CRÍTICA
8. **Canvas para 360°:** NO usar librería de panoramas (Pannellum/Three.js) — un 360° de vehículos es simplemente un carrusel controlable por drag con imágenes secuenciales
9. **Background removal es asíncrono:** El procesamiento típicamente toma 5-15 segundos — usar polling o webhooks
10. **VehiclesSaleService NO usa MediatR** — lógica directa en controllers
11. **MediaService SÍ usa MediatR** — usar Commands/Queries
12. **Vehicle360ProcessingService SÍ usa MediatR** — usar Commands/Queries
13. **BackgroundRemovalService SÍ usa Strategy Pattern** — providers intercambiables
14. **Puerto K8s:** Todos los servicios usan 8080 en Kubernetes
15. **BFF Pattern:** Frontend accede a API vía rewrites, NO directamente
16. **Idioma:** Toda la UI en español (RD)
17. **Verificar PROBLEMS** (Ctrl+Shift+M) después de cada cambio
18. **Costos 360°:** Pipeline interno ~$0.31/vehículo (Tier Recomendado). Budget: $0.13. Premium: $1.25. Documentar para decisión del negocio
19. **API Keys necesarias:** Configurar en Kubernetes Secrets:
    - `CLIPDROP_API_KEY` — para BackgroundRemovalService (default provider)
    - `SLAZZER_API_KEY` — para BackgroundRemovalService (fallback)
    - `FFMPEG_API_KEY` — para Video360Service (default provider)

---

_Documento diseñado para OKLA Platform — Febrero 2026_
_Microservicios: MediaService, Vehicle360ProcessingService, Video360Service, BackgroundRemovalService, VehiclesSaleService_
