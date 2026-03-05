---
title: "75 - Páginas de Media de Vehículos (360°, Video Tour, Mapa)"
priority: P0
estimated_time: ""
dependencies: []
apis: ["VehiclesSaleService"]
status: partial
last_updated: "2026-01-30"
---

# 75 - Páginas de Media de Vehículos (360°, Video Tour, Mapa)

> **Módulo**: Media360ViewerPage, VideoTourPage, MapViewPage  
> **Ubicación**: `frontend/web/src/pages/vehicles/`  
> **Última actualización**: Enero 2026

---

## 📐 Arquitectura General

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    VEHICLE MEDIA PAGES                                  │
│                                                                         │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │                  Media360ViewerPage                            │    │
│  │  /vehicles/:slug/360 (visor 360° interactivo)                  │    │
│  │                                                                 │    │
│  │  ┌─────────────────────────────────────────────────────────┐  │    │
│  │  │              360° Viewer Canvas                         │  │    │
│  │  │  - Drag to rotate                                       │  │    │
│  │  │  - Auto-rotate toggle                                   │  │    │
│  │  │  - Zoom controls                                        │  │    │
│  │  │  - Fullscreen mode                                      │  │    │
│  │  │  - Hotspots interactivos                                │  │    │
│  │  └─────────────────────────────────────────────────────────┘  │    │
│  │  ┌─────────────────┐  ┌───────────────────────────────────┐  │    │
│  │  │ Controls        │  │ Info Panel                        │  │    │
│  │  │ ⟳ Auto-rotate   │  │ Vehicle: Toyota Camry 2023        │  │    │
│  │  │ 🔍 Zoom +/-     │  │ Frame: 15/36 (150°)               │  │    │
│  │  │ ⛶ Fullscreen    │  │ View: Exterior                    │  │    │
│  │  │ 📍 Hotspots     │  └───────────────────────────────────┘  │    │
│  │  └─────────────────┘                                          │    │
│  │  Modes: Embed (Spyne iframe) | Custom (extracted frames)      │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                         │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │                    VideoTourPage                               │    │
│  │  /vehicles/:slug/video-tour (video walkaround)                 │    │
│  │                                                                 │    │
│  │  ┌─────────────────────────────────────────────────────────┐  │    │
│  │  │              Video Player                               │  │    │
│  │  │  - Custom controls (play, pause, seek)                  │  │    │
│  │  │  - Chapter navigation                                   │  │    │
│  │  │  - Playback speed (0.5x, 1x, 1.5x, 2x)                  │  │    │
│  │  │  - Quality selector (480p, 720p, 1080p, auto)           │  │    │
│  │  │  - Fullscreen mode                                      │  │    │
│  │  └─────────────────────────────────────────────────────────┘  │    │
│  │  ┌─────────────────────────────────────────────────────────┐  │    │
│  │  │ Chapters Sidebar                                        │  │    │
│  │  │ ▸ Exterior frontal (0:00 - 0:45)                       │  │    │
│  │  │ ▸ Lateral derecho (0:45 - 1:30)                        │  │    │
│  │  │ ▸ Parte trasera (1:30 - 2:00)                          │  │    │
│  │  │ ▸ Interior - Dashboard (2:30 - 3:15)                   │  │    │
│  │  │ ▸ Motor y maletero (3:45 - 4:05)                       │  │    │
│  │  └─────────────────────────────────────────────────────────┘  │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                         │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │                      MapViewPage                               │    │
│  │  /vehicles/map (mapa de dealers con inventario)                │    │
│  │                                                                 │    │
│  │  ┌─────────────────┐  ┌───────────────────────────────────┐  │    │
│  │  │ Filters Panel   │  │ Google Map                        │  │    │
│  │  │ ☑ Verified only │  │  🏪 Dealer markers                │  │    │
│  │  │ Distance: 50km  │  │  📍 User location                 │  │    │
│  │  └─────────────────┘  │  Cluster on zoom out              │  │    │
│  │                       └───────────────────────────────────┘  │    │
│  │  ┌─────────────────────────────────────────────────────────┐  │    │
│  │  │ Dealer Card (on marker click)                           │  │    │
│  │  │ AutoMax RD ✓ Verified                                   │  │    │
│  │  │ ⭐ 4.8/5 (245 reviews) | 📍 2.5 km                       │  │    │
│  │  │ 15 vehículos disponibles                                │  │    │
│  │  │ [📞 Llamar] [📱 WhatsApp] [🔗 Compartir]                │  │    │
│  │  └─────────────────────────────────────────────────────────┘  │    │
│  └────────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 🔧 Tipos TypeScript

### Media360ViewerPage Types

```typescript
// Modo de visor
type ViewerMode = "embed" | "custom" | "loading" | "error";

// Datos del spin 360°
interface Video360SpinData {
  spinId: string;
  vehicleId: string;
  status: "Pending" | "Processing" | "Completed" | "Failed";
  spinViewerUrl?: string; // URL de iframe embed (Spyne)
  spinEmbedCode?: string; // HTML embed code
  extractedFrameUrls: string[]; // URLs de frames extraídos
  extractedFrameCount: number; // Total de frames (típico: 36)
  thumbnailUrl?: string;
  progressPercent: number; // 0-100
  errorMessage?: string;
}

// Hotspot interactivo
interface Hotspot {
  id: string;
  x: number; // Posición X en %
  y: number; // Posición Y en %
  degrees: number; // Ángulo donde aparece (0-360)
  label: string;
  description: string;
  type: "feature" | "damage" | "upgrade" | "info";
}

// Datos del API vehicle360Service
interface Vehicle360ViewerData {
  viewId: string;
  vehicleId: string;
  status: "Pending" | "Processing" | "Completed" | "Failed";
  spinViewerUrl?: string;
  spinEmbedCode?: string;
  extractedFrameUrls: string[];
  extractedFrameCount: number;
  thumbnailUrl?: string;
  progressPercent: number;
  errorMessage?: string;
}

interface JobStatusResponse {
  jobId: string;
  isComplete: boolean;
  isFailed: boolean;
  progress: {
    percentage: number;
    currentStep: string;
  };
  errorMessage?: string;
}
```

### VideoTourPage Types

```typescript
// Capítulo del video
interface Chapter {
  id: string;
  title: string;
  startTime: number; // Segundos
  endTime: number; // Segundos
  thumbnail: string;
}

// Datos del video
interface VideoData {
  id: string;
  url: string;
  poster: string; // Thumbnail
  duration: number; // Segundos
  views: number;
  uploadedAt: string; // ISO timestamp
  chapters: Chapter[];
  dealer: {
    name: string;
    avatar: string;
    verified: boolean;
  };
}

// Calidad de video
type VideoQuality = "auto" | "1080p" | "720p" | "480p";
```

### MapViewPage Types

```typescript
// Ubicación de dealer
interface DealerLocation {
  id: string;
  name: string;
  latitude: number;
  longitude: number;
  address: string;
  city: string;
  phone: string;
  whatsapp?: string;
  isVerified: boolean;
  rating: number;
  reviewCount: number;
  vehicleCount: number;
  vehicles: MapVehicle[];
  logoUrl?: string;
  operatingHours?: string;
}

// Vehículo en mapa
interface MapVehicle {
  id: string;
  title: string;
  price: number;
  imageUrl: string;
  year: number;
}

// Filtros del mapa
interface DealerFilters {
  verified: boolean; // Solo verificados
  maxDistance: number; // En km (0 = sin límite)
}
```

---

## 🧩 Componentes Principales

### Media360ViewerPage

```
frontend/web/src/pages/vehicles/Media360ViewerPage.tsx (799 líneas)
│
├── State
│   ├── viewerMode ('embed' | 'custom' | 'loading' | 'error')
│   ├── spinData (Video360SpinData)
│   ├── loadedFrames (Set<number>)
│   ├── preloadProgress (0-100)
│   ├── currentFrame (0 to totalFrames-1)
│   ├── isAutoRotating
│   ├── autoRotateSpeed (ms per frame)
│   ├── zoom (1-3)
│   ├── isFullscreen
│   ├── isDragging
│   ├── showHotspots
│   ├── activeHotspot
│   └── useEmbedViewer
│
├── API Integration
│   ├── getVehicleViewer(vehicleId) - Obtener datos 360°
│   ├── getJobStatus(spinId) - Polling de estado
│   └── mapViewerDataToSpinData() - Transform DTO
│
├── Features
│   ├── Drag-to-rotate interaction
│   ├── Auto-rotate with configurable speed
│   ├── Zoom in/out (1x, 2x, 3x)
│   ├── Fullscreen mode
│   ├── Hotspots interactivos
│   ├── Progress bar durante carga
│   ├── Frame preloading
│   └── Dual mode: Embed (Spyne) vs Custom (frames)
│
└── Render
    ├── Loading state (spinner + progress)
    ├── Error state
    ├── Embed mode (iframe)
    └── Custom mode
        ├── Image canvas
        ├── Control bar
        ├── Hotspot overlays
        └── Info panel
```

### VideoTourPage

```
frontend/web/src/pages/vehicles/VideoTourPage.tsx (614 líneas)
│
├── State
│   ├── isPlaying
│   ├── isMuted
│   ├── volume (0-1)
│   ├── currentTime
│   ├── duration
│   ├── isFullscreen
│   ├── showControls
│   ├── activeChapter
│   ├── showSettings
│   ├── playbackSpeed (0.5, 1, 1.5, 2)
│   ├── quality ('auto' | '1080p' | '720p' | '480p')
│   ├── isFavorite
│   └── showChapters
│
├── Refs
│   ├── videoRef (HTMLVideoElement)
│   ├── containerRef
│   └── progressRef
│
├── Features
│   ├── Custom video controls
│   ├── Chapter navigation
│   ├── Auto-hide controls on inactivity
│   ├── Active chapter detection
│   ├── Playback speed selector
│   ├── Quality selector
│   ├── Keyboard shortcuts
│   ├── Volume control
│   ├── Progress bar with preview
│   └── Share and favorite actions
│
├── Chapters (ejemplo)
│   ├── Exterior frontal (0:00 - 0:45)
│   ├── Lateral derecho (0:45 - 1:30)
│   ├── Parte trasera (1:30 - 2:00)
│   ├── Lateral izquierdo (2:00 - 2:30)
│   ├── Interior - Dashboard (2:30 - 3:15)
│   ├── Interior - Asientos traseros (3:15 - 3:45)
│   └── Motor y maletero (3:45 - 4:05)
│
└── Render
    ├── Video container
    │   ├── <video> element
    │   ├── Play/Pause overlay
    │   └── Custom controls bar
    ├── Progress bar with chapters
    ├── Chapters sidebar (collapsible)
    └── Settings dropdown
```

### MapViewPage

```
frontend/web/src/pages/vehicles/MapViewPage.tsx (613 líneas)
│
├── State
│   ├── dealers (DealerLocation[])
│   ├── isLoadingDealers
│   ├── dealersError
│   ├── selectedDealer
│   ├── hoveredDealer
│   ├── showFilters
│   ├── showShareMenu
│   ├── imageStartIndex
│   ├── filters (DealerFilters)
│   └── userLocation
│
├── Google Maps
│   ├── useJsApiLoader (API key from env)
│   ├── GoogleMap component
│   ├── OverlayView for custom markers
│   └── Custom dealer markers
│
├── Features
│   ├── Load dealers from VehiclesSaleService
│   ├── Distance calculation (Haversine formula)
│   ├── Filter by verified only
│   ├── Filter by max distance
│   ├── Click marker to show dealer card
│   ├── Share dealer (copy link, WhatsApp)
│   ├── Navigate to dealer profile
│   ├── Image carousel in dealer card
│   └── URL state persistence (dealer, page)
│
├── Filters
│   ├── Verified only toggle
│   └── Max distance slider (0-100km)
│
└── Render
    ├── Loading state
    ├── Error state
    ├── Filter panel (collapsible)
    ├── Google Map
    │   ├── Dealer markers
    │   └── User location marker
    └── Selected dealer card
        ├── Logo + Name + Verified badge
        ├── Rating + Review count
        ├── Vehicle count
        ├── Distance from user
        ├── Vehicle carousel
        └── Action buttons (Call, WhatsApp, Share)
```

---

## 🌐 API Services

### vehicle360Service.ts

```typescript
// services/vehicle360Service.ts
import api from "@/lib/api";

// Response types
export interface Vehicle360ViewerData {
  viewId: string;
  vehicleId: string;
  status: "Pending" | "Processing" | "Completed" | "Failed";
  spinViewerUrl?: string;
  spinEmbedCode?: string;
  extractedFrameUrls: string[];
  extractedFrameCount: number;
  thumbnailUrl?: string;
  progressPercent: number;
  errorMessage?: string;
}

export interface JobStatusResponse {
  jobId: string;
  isComplete: boolean;
  isFailed: boolean;
  progress: {
    percentage: number;
    currentStep: string;
  };
  errorMessage?: string;
}

// GET /api/vehicles/:vehicleId/360
export const getVehicleViewer = async (
  vehicleId: string,
): Promise<Vehicle360ViewerData> => {
  const { data } = await api.get(`/vehicles/${vehicleId}/360`);
  return data;
};

// GET /api/jobs/:jobId/status
export const getJobStatus = async (
  jobId: string,
): Promise<JobStatusResponse> => {
  const { data } = await api.get(`/jobs/${jobId}/status`);
  return data;
};

// POST /api/vehicles/:vehicleId/360/create (dealer only)
export const create360Spin = async (
  vehicleId: string,
  videoFile: File,
): Promise<void> => {
  const formData = new FormData();
  formData.append("video", videoFile);
  await api.post(`/vehicles/${vehicleId}/360/create`, formData, {
    headers: { "Content-Type": "multipart/form-data" },
  });
};
```

### dealerService.ts

```typescript
// services/dealerService.ts
import api from "@/lib/api";

export interface DealerLocation {
  id: string;
  name: string;
  latitude: number;
  longitude: number;
  address: string;
  city: string;
  phone: string;
  whatsapp?: string;
  isVerified: boolean;
  rating: number;
  reviewCount: number;
  vehicleCount: number;
  vehicles: Array<{
    id: string;
    title: string;
    price: number;
    imageUrl: string;
    year: number;
  }>;
  logoUrl?: string;
  operatingHours?: string;
}

// GET /api/dealers/with-vehicles (para MapViewPage)
export const getDealersWithVehicles = async (): Promise<DealerLocation[]> => {
  const { data } = await api.get("/dealers/with-vehicles");
  return data;
};
```

---

## 🔗 Configuración de Entorno

```typescript
// config/env.ts
export const integrationConfig = {
  googleMapsKey: import.meta.env.VITE_GOOGLE_MAPS_API_KEY || "YOUR_API_KEY",
};
```

---

## 🛣️ Rutas

```typescript
// App.tsx
<Route path="/vehicles/:slug/360" element={<Media360ViewerPage />} />
<Route path="/vehicles/:slug/video-tour" element={<VideoTourPage />} />
<Route path="/vehicles/map" element={<MapViewPage />} />
```

---

## 🎮 Controles e Interacciones

### Media360ViewerPage - Controles

| Control            | Acción                     |
| ------------------ | -------------------------- |
| Drag (mouse/touch) | Rotar vehículo             |
| ⟳ Auto-rotate      | Toggle rotación automática |
| 🔍+ / 🔍-          | Zoom in/out (1x → 2x → 3x) |
| ⛶ Fullscreen       | Modo pantalla completa     |
| 📍 Hotspots        | Toggle mostrar hotspots    |
| Click hotspot      | Mostrar tooltip con info   |

### VideoTourPage - Controles

| Control       | Acción            |
| ------------- | ----------------- |
| Space         | Play/Pause        |
| ← / →         | Seek -10s / +10s  |
| ↑ / ↓         | Volumen +/-       |
| M             | Mute/Unmute       |
| F             | Fullscreen        |
| Click chapter | Saltar a capítulo |
| Drag progress | Seek to position  |

### MapViewPage - Interacciones

| Interacción       | Resultado                  |
| ----------------- | -------------------------- |
| Click marker      | Mostrar dealer card        |
| Drag map          | Pan                        |
| Scroll/Pinch      | Zoom                       |
| Click "Llamar"    | tel: link                  |
| Click "WhatsApp"  | wa.me link                 |
| Click "Compartir" | Copy link / WhatsApp share |

---

## 📦 Dependencias

```json
{
  "@react-google-maps/api": "^2.x",
  "framer-motion": "^10.x",
  "react-icons": "^4.x"
}
```

---

## 🌍 Internacionalización

```json
// locales/es/vehicles.json
{
  "360viewer": {
    "title": "Vista 360°",
    "autoRotate": "Rotación automática",
    "zoom": "Zoom",
    "fullscreen": "Pantalla completa",
    "hotspots": "Puntos de interés",
    "loading": "Cargando vista 360°...",
    "processing": "Procesando... {{progress}}%",
    "error": "Error al cargar la vista 360°"
  },
  "videoTour": {
    "title": "Video Tour",
    "chapters": "Capítulos",
    "settings": "Configuración",
    "speed": "Velocidad",
    "quality": "Calidad",
    "views": "{{count}} visualizaciones"
  },
  "map": {
    "title": "Mapa de Dealers",
    "loading": "Cargando mapa...",
    "loadingDealers": "Cargando dealers...",
    "verified": "Verificado",
    "distance": "{{distance}}",
    "vehicles": "{{count}} vehículos",
    "call": "Llamar",
    "whatsapp": "WhatsApp",
    "share": "Compartir",
    "filters": {
      "verifiedOnly": "Solo verificados",
      "maxDistance": "Distancia máxima"
    }
  }
}
```

---

## 🔐 Configuración Google Maps

### .env

```env
VITE_GOOGLE_MAPS_API_KEY=AIzaSy...
```

### APIs requeridas en Google Cloud Console

- Maps JavaScript API
- Places API (opcional, para autocompletado)
- Geocoding API (opcional)

---

## ✅ Checklist de Validación

### Media360ViewerPage

- [ ] Carga datos del API /api/vehicles/:id/360
- [ ] Muestra progreso durante procesamiento
- [ ] Polling de status si está en Processing
- [ ] Modo embed funciona (iframe)
- [ ] Modo custom funciona (frames)
- [ ] Drag to rotate suave
- [ ] Auto-rotate funcional
- [ ] Zoom in/out 1x-3x
- [ ] Fullscreen funciona
- [ ] Hotspots aparecen en ángulos correctos
- [ ] Preload de frames con progress
- [ ] Error handling

### VideoTourPage

- [ ] Video se reproduce correctamente
- [ ] Controles custom funcionan
- [ ] Play/Pause, seek, volumen
- [ ] Capítulos navegables
- [ ] Active chapter highlighting
- [ ] Playback speed funciona (0.5x-2x)
- [ ] Quality selector
- [ ] Fullscreen funciona
- [ ] Auto-hide controls
- [ ] Keyboard shortcuts

### MapViewPage

- [ ] Google Maps carga correctamente
- [ ] Markers de dealers visibles
- [ ] Click en marker muestra card
- [ ] Filtro "verificados" funciona
- [ ] Filtro "distancia" funciona
- [ ] Cálculo de distancia correcto
- [ ] Botón llamar funciona (tel:)
- [ ] Botón WhatsApp funciona (wa.me)
- [ ] Compartir copia link
- [ ] Compartir WhatsApp funciona
- [ ] State en URL (dealer, page)
- [ ] Loading y error states

---

## 🧪 TESTS E2E (Playwright)

```typescript
import { test, expect } from "@playwright/test";

test.describe("Vehicle Media Pages", () => {
  test("Media360ViewerPage debe mostrar visor 360 interactivo", async ({
    page,
  }) => {
    await page.goto("/vehicles/toyota-camry-2024/360");
    await expect(page.getByTestId("media-360-page")).toBeVisible();
    await expect(page.getByTestId("viewer-360-canvas")).toBeVisible();
    await expect(page.getByTestId("rotate-controls")).toBeVisible();
  });

  test("Media360ViewerPage debe permitir rotación con mouse", async ({
    page,
  }) => {
    await page.goto("/vehicles/toyota-camry-2024/360");
    const canvas = page.getByTestId("viewer-360-canvas");
    await canvas.hover();
    await page.mouse.down();
    await page.mouse.move(100, 0);
    await page.mouse.up();
    await expect(page.getByTestId("frame-indicator")).not.toHaveText("1/36");
  });

  test("Media360ViewerPage debe tener modo auto-rotate", async ({ page }) => {
    await page.goto("/vehicles/toyota-camry-2024/360");
    await page.getByTestId("auto-rotate-toggle").click();
    await expect(page.getByTestId("auto-rotate-indicator")).toBeVisible();
  });

  test("VideoTourPage debe reproducir video walkaround", async ({ page }) => {
    await page.goto("/vehicles/toyota-camry-2024/video-tour");
    await expect(page.getByTestId("video-tour-page")).toBeVisible();
    await expect(page.getByTestId("video-player")).toBeVisible();
    await page.getByTestId("play-button").click();
    await expect(page.getByTestId("video-playing")).toBeVisible();
  });

  test("VideoTourPage debe mostrar capítulos navegables", async ({ page }) => {
    await page.goto("/vehicles/toyota-camry-2024/video-tour");
    await expect(page.getByTestId("video-chapters")).toBeVisible();
    await page.getByTestId("chapter-interior").click();
    await expect(page.getByTestId("current-chapter")).toHaveText(/interior/i);
  });

  test("MapViewPage debe mostrar mapa con dealers", async ({ page }) => {
    await page.goto("/vehicles/toyota-camry-2024/map");
    await expect(page.getByTestId("map-view-page")).toBeVisible();
    await expect(page.getByTestId("google-map")).toBeVisible();
    await expect(page.getByTestId("dealer-marker").first()).toBeVisible();
  });

  test("MapViewPage debe mostrar card al hacer clic en marker", async ({
    page,
  }) => {
    await page.goto("/vehicles/toyota-camry-2024/map");
    await page.getByTestId("dealer-marker").first().click();
    await expect(page.getByTestId("dealer-info-card")).toBeVisible();
    await expect(page.getByTestId("call-dealer-button")).toBeVisible();
    await expect(page.getByTestId("whatsapp-dealer-button")).toBeVisible();
  });
});
```

---

## 📚 Documentación Relacionada

- [74-vehicle-detail-browse-pages.md](../01-PUBLICO/03-detalle-vehiculo.md) - VehicleDetailPage
- [68-common-components.md](./01-common-components.md) - Componentes compartidos
- [57-dealer-inventory-management.md](../05-DEALER/02-dealer-inventario.md) - Subida de media 360°
