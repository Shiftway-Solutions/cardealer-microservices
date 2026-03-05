---
title: "90. Video Tour y Test Pages"
priority: P0
estimated_time: ""
dependencies: []
apis: []
status: complete
last_updated: "2026-01-30"
---

# 90. Video Tour y Test Pages

## Descripción General

Documentación de la página de video tour y las páginas de testing para desarrollo:

- **VideoTourPage.tsx** - Reproductor de video tour de vehículos (Premium Q2 2026)
- **CreateListingTestPage.tsx** - Testing de creación de listings con límites
- **PlansComparisonTestPage.tsx** - Testing de comparación de planes
- **DealerAnalyticsTestPage.tsx** - Testing de analytics con feature gates
- **DealerAnalyticsPage.example.tsx** - Ejemplo de implementación de analytics

## Arquitectura

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      VIDEO TOUR & TEST PAGES                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                      VideoTourPage                                   │   │
│  │  ┌─────────────────────────────────────────────────────────────┐    │   │
│  │  │               Video Player (HTML5)                          │    │   │
│  │  │  ┌───────────────────────────────────────────────────────┐  │    │   │
│  │  │  │           Video Content                               │  │    │   │
│  │  │  │                                                       │  │    │   │
│  │  │  └───────────────────────────────────────────────────────┘  │    │   │
│  │  │  ┌─────────────────────────────────────────────────────────┐│    │   │
│  │  │  │ ▶️ 1:23 / 4:05 ━━━━━━━━○━━━━━━━ 🔊 ⚙️ ⛶ │Controls       ││    │   │
│  │  │  └─────────────────────────────────────────────────────────┘│    │   │
│  │  └─────────────────────────────────────────────────────────────┘    │   │
│  │  ┌─────────────────────────────────────────────────────────────┐    │   │
│  │  │  Chapters: [Exterior] [Interior] [Motor] [Maletero]         │    │   │
│  │  └─────────────────────────────────────────────────────────────┘    │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    Test Pages (Development)                          │   │
│  │  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐   │   │
│  │  │CreateListingTest │  │PlansComparison   │  │DealerAnalytics   │   │   │
│  │  │- Limit Banner    │  │- Plan Cards      │  │- UpgradePrompt   │   │   │
│  │  │- Progress Bar    │  │- Feature Table   │  │- Stats Grid      │   │   │
│  │  │- Form Fields     │  │- Current Access  │  │- Market Analysis │   │   │
│  │  └──────────────────┘  └──────────────────┘  └──────────────────┘   │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 1. VideoTourPage.tsx

### Ubicación

`frontend/web/src/pages/vehicles/VideoTourPage.tsx`

### Descripción

Reproductor de video tour de vehículos con capítulos, controles avanzados y hotspots temporales. Feature premium planificada para Q2 2026.

### Ruta

`/vehicles/:slug/video-tour`

### Características

- **Video Player** - HTML5 video con controles personalizados
- **Chapters** - Navegación por secciones del video
- **Playback Controls** - Play/pause, seek, volume
- **Playback Speed** - 0.5x, 1x, 1.5x, 2x
- **Quality Selector** - Auto, 1080p, 720p, 480p
- **Fullscreen** - Toggle pantalla completa
- **Auto-hide Controls** - Ocultar controles después de inactividad
- **Dealer Info** - Información del vendedor con badge verificado
- **Share & Favorite** - Compartir y guardar como favorito

### Líneas de Código

~614 líneas

### Dependencias

```typescript
import { useState, useRef, useEffect } from "react";
import { useParams, Link } from "react-router-dom";
import {
  FiPlay,
  FiPause,
  FiVolume2,
  FiVolumeX,
  FiMaximize2,
  FiMinimize2,
  FiArrowLeft,
  FiSkipBack,
  FiSkipForward,
  FiSettings,
  FiShare2,
  FiHeart,
  FiMessageCircle,
  FiClock,
  FiUser,
  FiCheck,
} from "react-icons/fi";
import MainLayout from "../../layouts/MainLayout";
```

### TypeScript Types

```typescript
interface Chapter {
  id: string;
  title: string;
  startTime: number;
  endTime: number;
  thumbnail: string;
}

interface VideoData {
  id: string;
  url: string;
  poster: string;
  duration: number;
  views: number;
  uploadedAt: string;
  chapters: Chapter[];
  dealer: {
    name: string;
    avatar: string;
    verified: boolean;
  };
}
```

### Estado del Componente

```typescript
const videoRef = useRef<HTMLVideoElement>(null);
const containerRef = useRef<HTMLDivElement>(null);
const progressRef = useRef<HTMLDivElement>(null);

const [isPlaying, setIsPlaying] = useState(false);
const [isMuted, setIsMuted] = useState(false);
const [volume, setVolume] = useState(1);
const [currentTime, setCurrentTime] = useState(0);
const [duration, setDuration] = useState(0);
const [isFullscreen, setIsFullscreen] = useState(false);
const [showControls, setShowControls] = useState(true);
const [activeChapter, setActiveChapter] = useState<Chapter | null>(null);
const [showSettings, setShowSettings] = useState(false);
const [playbackSpeed, setPlaybackSpeed] = useState(1);
const [quality, setQuality] = useState<"auto" | "1080p" | "720p" | "480p">(
  "auto",
);
const [isFavorite, setIsFavorite] = useState(false);
const [showChapters, setShowChapters] = useState(true);
```

### Chapters Mock Data

```typescript
const chapters: Chapter[] = [
  {
    id: "c1",
    title: "Exterior frontal",
    startTime: 0,
    endTime: 45,
    thumbnail: "...",
  },
  {
    id: "c2",
    title: "Lateral derecho",
    startTime: 45,
    endTime: 90,
    thumbnail: "...",
  },
  {
    id: "c3",
    title: "Parte trasera",
    startTime: 90,
    endTime: 120,
    thumbnail: "...",
  },
  {
    id: "c4",
    title: "Lateral izquierdo",
    startTime: 120,
    endTime: 150,
    thumbnail: "...",
  },
  {
    id: "c5",
    title: "Interior - Dashboard",
    startTime: 150,
    endTime: 195,
    thumbnail: "...",
  },
  {
    id: "c6",
    title: "Interior - Asientos traseros",
    startTime: 195,
    endTime: 225,
    thumbnail: "...",
  },
  {
    id: "c7",
    title: "Motor y maletero",
    startTime: 225,
    endTime: 245,
    thumbnail: "...",
  },
];
```

### Helpers

```typescript
// Format time as MM:SS
const formatTime = (seconds: number) => {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins}:${secs.toString().padStart(2, "0")}`;
};

// Auto-hide controls after inactivity
useEffect(() => {
  let timeout: NodeJS.Timeout;
  const handleMouseMove = () => {
    setShowControls(true);
    clearTimeout(timeout);
    if (isPlaying) {
      timeout = setTimeout(() => setShowControls(false), 3000);
    }
  };
  container?.addEventListener("mousemove", handleMouseMove);
  return () => container?.removeEventListener("mousemove", handleMouseMove);
}, [isPlaying]);

// Update active chapter based on current time
useEffect(() => {
  const chapter = videoData.chapters.find(
    (c) => currentTime >= c.startTime && currentTime < c.endTime,
  );
  setActiveChapter(chapter || null);
}, [currentTime]);
```

---

## 2. CreateListingTestPage.tsx

### Ubicación

`frontend/web/src/pages/dealer/CreateListingTestPage.tsx`

### Descripción

Página de testing para crear listings con control de límites según el plan del dealer.

### Ruta

`/dealer/test/create-listing`

### Características

- **Limit Banner** - Muestra cuando el dealer alcanzó su límite
- **Progress Bar** - Uso actual vs máximo del plan
- **Plan Info Card** - Muestra plan actual y features
- **Form** - Formulario de listing (disabled si límite alcanzado)
- **Warning** - Alerta cuando uso >80%

### Líneas de Código

~243 líneas

### Dependencias

```typescript
import { useState } from "react";
import { useAuthStore } from "../../store/authStore";
import { useDealerFeatures } from "../../hooks/useDealerFeatures";
import { LimitReachedBanner } from "../../components/dealer/UpgradePrompt";
```

### Hook useDealerFeatures

```typescript
const { hasReachedLimit, getUsageProgress, currentPlan, usage, limits } =
  useDealerFeatures(user?.subscription);

const isAtLimit = hasReachedLimit("listings");
const progress = getUsageProgress("listings");
```

### Progress Bar Colors

```typescript
// Color según porcentaje de uso
const getProgressColor = (progress: number) => {
  if (progress > 90) return "bg-red-500";
  if (progress > 70) return "bg-yellow-500";
  return "bg-blue-500";
};
```

---

## 3. PlansComparisonTestPage.tsx

### Ubicación

`frontend/web/src/pages/dealer/PlansComparisonTestPage.tsx`

### Descripción

Página de testing para ver comparación de planes y features disponibles.

### Ruta

`/dealer/test/plans`

### Características

- **Pricing Cards** - 4 planes (FREE, BASIC, PRO, ENTERPRISE)
- **Current Plan Badge** - Indica plan actual del usuario
- **Feature Table** - Tabla detallada de todas las features
- **Your Access** - Grid mostrando acceso actual

### Líneas de Código

~251 líneas

### Dependencias

```typescript
import { useAuthStore } from "../../store/authStore";
import {
  useDealerFeatures,
  FEATURE_NAMES,
  DEALER_PLAN_PRICING,
} from "../../hooks/useDealerFeatures";
import { DEALER_PLAN_LIMITS, DealerPlan } from "@/shared/types";
import type { DealerPlanFeatures } from "@/shared/types";
```

### Plan Configuration

```typescript
const plans = [
  DealerPlan.FREE,
  DealerPlan.BASIC,
  DealerPlan.PRO,
  DealerPlan.ENTERPRISE,
];

// Pricing example
const DEALER_PLAN_PRICING = {
  [DealerPlan.FREE]: { price: 0, available: true },
  [DealerPlan.BASIC]: { price: 29, available: true },
  [DealerPlan.PRO]: { price: 79, available: true },
  [DealerPlan.ENTERPRISE]: { price: 199, available: false }, // Coming soon
};
```

---

## 4. DealerAnalyticsTestPage.tsx

### Ubicación

`frontend/web/src/pages/dealer/DealerAnalyticsTestPage.tsx`

### Descripción

Página de testing para Analytics Dashboard con feature gates según el plan.

### Ruta

`/dealer/test/analytics`

### Características

- **UpgradePrompt** - Mostrar si no tiene acceso a analytics (FREE)
- **Stats Cards** - 4 métricas principales (BASIC+)
- **Market Analysis** - Solo para PRO+ (feature-gated)
- **Top Listings** - Lista de vehículos con mejor rendimiento
- **Plan Info** - Información del plan actual

### Líneas de Código

~175 líneas

### Dependencias

```typescript
import { useAuthStore } from "../../store/authStore";
import { useDealerFeatures } from "../../hooks/useDealerFeatures";
import { UpgradePrompt } from "../../components/dealer/UpgradePrompt";
```

### Feature Gates

```typescript
const { canAccess, currentPlan, limits } = useDealerFeatures(user?.subscription);

// Gate para analytics básicos
if (!canAccess('analyticsAccess')) {
  return <UpgradePrompt feature="analyticsAccess" currentPlan={currentPlan} />;
}

// Gate para market analysis (PRO+)
{!canAccess('marketPriceAnalysis') ? (
  <UpgradePrompt feature="marketPriceAnalysis" currentPlan={currentPlan} />
) : (
  <MarketAnalysisSection />
)}
```

---

## 5. DealerAnalyticsPage.example.tsx

### Ubicación

`frontend/web/src/pages/dealer/DealerAnalyticsPage.example.tsx`

### Descripción

Ejemplo de implementación de página de analytics con todos los patrones de feature gates.

### Exports

```typescript
export const DealerAnalyticsPage = () => { ... }
export const CreateListingPage = () => { ... }
```

### Características

- **DealerAnalyticsPage** - Ejemplo completo de analytics con gates
- **CreateListingPage** - Ejemplo de form con límites
- **UpgradePrompt** - Uso del componente de upgrade
- **LimitReachedBanner** - Banner cuando límite alcanzado
- **Progress Bar** - Barra de progreso de uso

### Líneas de Código

~192 líneas

### Patrón de Feature Gates

```typescript
// 1. Check access antes de renderizar
if (!canAccess('analyticsAccess')) {
  return <UpgradePrompt feature="analyticsAccess" />;
}

// 2. Gate secciones específicas
{!canAccess('marketPriceAnalysis') ? (
  <UpgradePrompt feature="marketPriceAnalysis" />
) : (
  <MarketPriceCards />
)}

// 3. Disable form si límite alcanzado
<form className={isAtLimit ? 'opacity-50 pointer-events-none' : ''}>
```

---

## API Endpoints

### VideoTourPage

```
GET /api/vehicles/{slug}/video-tour
Response: VideoData

POST /api/vehicles/{slug}/video-tour/views
(Increment view count)

POST /api/vehicles/{slug}/favorite
DELETE /api/vehicles/{slug}/favorite
```

### Test Pages

```
GET /api/dealers/{dealerId}/subscription
Response: DealerSubscription

GET /api/dealers/{dealerId}/usage
Response: { currentListings: number, currentImages: number, ... }
```

---

## Servicios y Hooks Relacionados

| Hook/Service          | Uso                            |
| --------------------- | ------------------------------ |
| `useDealerFeatures`   | Feature gates y límites        |
| `useAuthStore`        | Estado de autenticación        |
| `DEALER_PLAN_LIMITS`  | Constantes de límites por plan |
| `FEATURE_NAMES`       | Nombres legibles de features   |
| `DEALER_PLAN_PRICING` | Precios de planes              |

---

## Checklist de Validación

### VideoTourPage

- [ ] Video carga y reproduce
- [ ] Play/pause funciona
- [ ] Seek por progress bar funciona
- [ ] Volume control funciona
- [ ] Mute toggle funciona
- [ ] Fullscreen toggle funciona
- [ ] Chapters clickeables saltan al tiempo correcto
- [ ] Active chapter se actualiza automáticamente
- [ ] Settings menu abre/cierra
- [ ] Playback speed cambia
- [ ] Quality selector funciona
- [ ] Auto-hide controls después de 3s
- [ ] Favorite toggle funciona
- [ ] Dealer info muestra verificado

### CreateListingTestPage

- [ ] LimitReachedBanner aparece cuando límite alcanzado
- [ ] Progress bar muestra porcentaje correcto
- [ ] Color de barra cambia según uso (>70%, >90%)
- [ ] Warning aparece cuando >80%
- [ ] Plan info card muestra datos correctos
- [ ] Form disabled cuando límite alcanzado
- [ ] Submit bloqueado si límite

### PlansComparisonTestPage

- [ ] 4 plan cards se muestran
- [ ] Current plan tiene badge
- [ ] Feature table renderiza todas las features
- [ ] Checkmarks y X correctos por plan
- [ ] Coming soon en Enterprise
- [ ] Your access grid muestra acceso actual

### DealerAnalyticsTestPage

- [ ] UpgradePrompt aparece para FREE plan
- [ ] Stats cards se muestran para BASIC+
- [ ] Market analysis gated para PRO+
- [ ] Top listings se muestran
- [ ] Plan badge correcto

---

## Notas de Implementación

1. **VideoTourPage**: Feature premium para Q2 2026, actualmente usa mock data
2. **Test Pages**: Solo para desarrollo, no expuestas en navegación de producción
3. **useDealerFeatures**: Hook centralizado para feature gates
4. **Plan Limits**: Definidos en `@/shared/types` como constantes
5. **Mobile**: VideoTourPage necesita controles touch-friendly
6. **Keyboard**: VideoTourPage soportar atajos (space, arrows)
7. **Analytics Tracking**: Trackear uso de features por plan

---

## 🔧 Hooks y Servicios

### useDealerFeatures Hook

```typescript
// filepath: src/hooks/useDealerFeatures.ts
import { useMemo } from "react";
import { useAuthStore } from "@/stores/authStore";

export type DealerPlan = "FREE" | "STARTER" | "PRO" | "ENTERPRISE";

export interface DealerPlanLimits {
  maxListings: number;
  maxImagesPerListing: number;
  hasVideoTour: boolean;
  hasAnalytics: boolean;
  hasAdvancedAnalytics: boolean;
  hasCRM: boolean;
  hasAPIAccess: boolean;
  hasPrioritySupport: boolean;
  hasWhiteLabel: boolean;
}

export const DEALER_PLAN_LIMITS: Record<DealerPlan, DealerPlanLimits> = {
  FREE: {
    maxListings: 3,
    maxImagesPerListing: 5,
    hasVideoTour: false,
    hasAnalytics: false,
    hasAdvancedAnalytics: false,
    hasCRM: false,
    hasAPIAccess: false,
    hasPrioritySupport: false,
    hasWhiteLabel: false,
  },
  STARTER: {
    maxListings: 15,
    maxImagesPerListing: 10,
    hasVideoTour: false,
    hasAnalytics: true,
    hasAdvancedAnalytics: false,
    hasCRM: false,
    hasAPIAccess: false,
    hasPrioritySupport: false,
    hasWhiteLabel: false,
  },
  PRO: {
    maxListings: 50,
    maxImagesPerListing: 20,
    hasVideoTour: true,
    hasAnalytics: true,
    hasAdvancedAnalytics: true,
    hasCRM: true,
    hasAPIAccess: false,
    hasPrioritySupport: true,
    hasWhiteLabel: false,
  },
  ENTERPRISE: {
    maxListings: Infinity,
    maxImagesPerListing: 50,
    hasVideoTour: true,
    hasAnalytics: true,
    hasAdvancedAnalytics: true,
    hasCRM: true,
    hasAPIAccess: true,
    hasPrioritySupport: true,
    hasWhiteLabel: true,
  },
};

export function useDealerFeatures() {
  const { user, dealer } = useAuthStore();

  const plan: DealerPlan = dealer?.subscription?.plan || "FREE";
  const limits = DEALER_PLAN_LIMITS[plan];

  const usage = useMemo(
    () => ({
      currentListings: dealer?.stats?.activeListings || 0,
      maxListings: limits.maxListings,
      usagePercentage:
        limits.maxListings === Infinity
          ? 0
          : ((dealer?.stats?.activeListings || 0) / limits.maxListings) * 100,
      isAtLimit: (dealer?.stats?.activeListings || 0) >= limits.maxListings,
      isNearLimit:
        (dealer?.stats?.activeListings || 0) / limits.maxListings > 0.8,
    }),
    [dealer, limits],
  );

  const hasFeature = (feature: keyof DealerPlanLimits): boolean => {
    return !!limits[feature];
  };

  const canUpgrade = plan !== "ENTERPRISE";
  const nextPlan: DealerPlan | null = {
    FREE: "STARTER",
    STARTER: "PRO",
    PRO: "ENTERPRISE",
    ENTERPRISE: null,
  }[plan];

  return {
    plan,
    limits,
    usage,
    hasFeature,
    canUpgrade,
    nextPlan,
    isDealer: !!dealer,
    dealerId: dealer?.id,
  };
}
```

### useVideoPlayer Hook

```typescript
// filepath: src/hooks/useVideoPlayer.ts
import { useState, useRef, useCallback, useEffect } from "react";

interface UseVideoPlayerOptions {
  autoPlay?: boolean;
  onEnded?: () => void;
  onError?: (error: Error) => void;
}

interface VideoChapter {
  id: string;
  title: string;
  timestamp: number;
}

export function useVideoPlayer(options: UseVideoPlayerOptions = {}) {
  const videoRef = useRef<HTMLVideoElement>(null);
  const [isPlaying, setIsPlaying] = useState(false);
  const [isMuted, setIsMuted] = useState(false);
  const [volume, setVolume] = useState(1);
  const [currentTime, setCurrentTime] = useState(0);
  const [duration, setDuration] = useState(0);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [playbackRate, setPlaybackRate] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);
  const [activeChapter, setActiveChapter] = useState<string | null>(null);

  const play = useCallback(() => {
    videoRef.current?.play();
    setIsPlaying(true);
  }, []);

  const pause = useCallback(() => {
    videoRef.current?.pause();
    setIsPlaying(false);
  }, []);

  const toggle = useCallback(() => {
    if (isPlaying) {
      pause();
    } else {
      play();
    }
  }, [isPlaying, play, pause]);

  const seek = useCallback((time: number) => {
    if (videoRef.current) {
      videoRef.current.currentTime = time;
      setCurrentTime(time);
    }
  }, []);

  const seekToChapter = useCallback(
    (chapter: VideoChapter) => {
      seek(chapter.timestamp);
      setActiveChapter(chapter.id);
    },
    [seek],
  );

  const setVolumeLevel = useCallback((level: number) => {
    if (videoRef.current) {
      videoRef.current.volume = level;
      setVolume(level);
      setIsMuted(level === 0);
    }
  }, []);

  const toggleMute = useCallback(() => {
    if (videoRef.current) {
      videoRef.current.muted = !isMuted;
      setIsMuted(!isMuted);
    }
  }, [isMuted]);

  const toggleFullscreen = useCallback(async () => {
    const container = videoRef.current?.parentElement;
    if (!container) return;

    if (!document.fullscreenElement) {
      await container.requestFullscreen();
      setIsFullscreen(true);
    } else {
      await document.exitFullscreen();
      setIsFullscreen(false);
    }
  }, []);

  const setSpeed = useCallback((speed: number) => {
    if (videoRef.current) {
      videoRef.current.playbackRate = speed;
      setPlaybackRate(speed);
    }
  }, []);

  // Update chapter based on current time
  const updateChapter = useCallback(
    (chapters: VideoChapter[]) => {
      const current = chapters
        .filter((c) => c.timestamp <= currentTime)
        .sort((a, b) => b.timestamp - a.timestamp)[0];

      if (current && current.id !== activeChapter) {
        setActiveChapter(current.id);
      }
    },
    [currentTime, activeChapter],
  );

  // Keyboard shortcuts
  useEffect(() => {
    const handleKeydown = (e: KeyboardEvent) => {
      if (document.activeElement?.tagName === "INPUT") return;

      switch (e.code) {
        case "Space":
          e.preventDefault();
          toggle();
          break;
        case "ArrowLeft":
          seek(Math.max(0, currentTime - 10));
          break;
        case "ArrowRight":
          seek(Math.min(duration, currentTime + 10));
          break;
        case "ArrowUp":
          setVolumeLevel(Math.min(1, volume + 0.1));
          break;
        case "ArrowDown":
          setVolumeLevel(Math.max(0, volume - 0.1));
          break;
        case "KeyM":
          toggleMute();
          break;
        case "KeyF":
          toggleFullscreen();
          break;
      }
    };

    window.addEventListener("keydown", handleKeydown);
    return () => window.removeEventListener("keydown", handleKeydown);
  }, [
    toggle,
    seek,
    currentTime,
    duration,
    volume,
    setVolumeLevel,
    toggleMute,
    toggleFullscreen,
  ]);

  return {
    videoRef,
    isPlaying,
    isMuted,
    volume,
    currentTime,
    duration,
    isFullscreen,
    playbackRate,
    isLoading,
    error,
    activeChapter,
    play,
    pause,
    toggle,
    seek,
    seekToChapter,
    setVolumeLevel,
    toggleMute,
    toggleFullscreen,
    setSpeed,
    updateChapter,
    setCurrentTime,
    setDuration,
    setIsLoading,
    setError,
  };
}
```

---

## 🎨 Estados de UI

### Loading State

```typescript
export function VideoTourSkeleton() {
  return (
    <div className="max-w-4xl mx-auto">
      {/* Video Skeleton */}
      <div className="aspect-video bg-gray-200 rounded-xl animate-pulse relative">
        <div className="absolute inset-0 flex items-center justify-center">
          <div className="w-16 h-16 bg-gray-300 rounded-full" />
        </div>
        {/* Progress bar */}
        <div className="absolute bottom-0 left-0 right-0 p-4">
          <Skeleton className="h-1 w-full rounded" />
        </div>
      </div>

      {/* Info Skeleton */}
      <div className="mt-4 space-y-3">
        <Skeleton className="h-6 w-3/4" />
        <Skeleton className="h-4 w-1/2" />
      </div>

      {/* Chapters Skeleton */}
      <div className="mt-6 grid grid-cols-4 gap-2">
        {[1, 2, 3, 4].map((i) => (
          <Skeleton key={i} className="h-20 rounded-lg" />
        ))}
      </div>
    </div>
  );
}
```

### Error State

```typescript
export function VideoTourError({ onRetry }: { onRetry: () => void }) {
  return (
    <div className="aspect-video bg-gray-900 rounded-xl flex items-center justify-center">
      <div className="text-center text-white">
        <AlertCircle size={48} className="mx-auto mb-4 text-red-400" />
        <h3 className="text-lg font-semibold mb-2">Error al cargar video</h3>
        <p className="text-gray-400 mb-4 max-w-sm">
          No pudimos cargar el video tour. Por favor intenta de nuevo.
        </p>
        <Button variant="outline" onClick={onRetry} className="text-white border-white">
          <RefreshCw size={16} className="mr-2" />
          Reintentar
        </Button>
      </div>
    </div>
  );
}
```

### Feature Gated State

```typescript
export function VideoTourGated() {
  return (
    <div className="aspect-video bg-gradient-to-br from-gray-900 to-gray-800 rounded-xl flex items-center justify-center relative overflow-hidden">
      {/* Blurred preview */}
      <div className="absolute inset-0 bg-gray-800 opacity-80" />

      {/* Upgrade prompt */}
      <div className="relative text-center text-white p-8">
        <Lock size={48} className="mx-auto mb-4 text-blue-400" />
        <h3 className="text-xl font-bold mb-2">Video Tour Premium</h3>
        <p className="text-gray-300 mb-6 max-w-md">
          Los video tours están disponibles en el plan PRO.
          Actualiza para ofrecer una mejor experiencia a tus compradores.
        </p>
        <Link href="/dealer/planes">
          <Button size="lg">
            <Zap size={16} className="mr-2" />
            Actualizar a PRO
          </Button>
        </Link>
      </div>
    </div>
  );
}
```

---

## 🧪 Testing E2E

```typescript
// filepath: e2e/components/video-tour.spec.ts
import { test, expect } from "@playwright/test";

test.describe("VideoTourPage", () => {
  test("should load and display video player", async ({ page }) => {
    await page.goto("/vehiculos/toyota-corolla-2024/video-tour");

    await expect(page.getByTestId("video-player")).toBeVisible();
  });

  test("should play/pause on click", async ({ page }) => {
    await page.goto("/vehiculos/toyota-corolla-2024/video-tour");

    const playButton = page.getByTestId("play-button");
    await playButton.click();

    // Video should be playing
    const video = page.locator("video");
    await expect(video).not.toHaveAttribute("paused");

    // Click again to pause
    await playButton.click();
    await expect(video).toHaveAttribute("paused");
  });

  test("should navigate chapters on click", async ({ page }) => {
    await page.goto("/vehiculos/toyota-corolla-2024/video-tour");

    const chapter = page.getByTestId("chapter-interior");
    await chapter.click();

    // Should highlight active chapter
    await expect(chapter).toHaveClass(/active/);
  });

  test("should toggle fullscreen", async ({ page }) => {
    await page.goto("/vehiculos/toyota-corolla-2024/video-tour");

    const fullscreenButton = page.getByTestId("fullscreen-button");
    await fullscreenButton.click();

    // Check fullscreen state
    const isFullscreen = await page.evaluate(
      () => !!document.fullscreenElement,
    );
    expect(isFullscreen).toBe(true);
  });

  test("should change playback speed", async ({ page }) => {
    await page.goto("/vehiculos/toyota-corolla-2024/video-tour");

    await page.getByTestId("settings-button").click();
    await page.getByRole("option", { name: "1.5x" }).click();

    const video = page.locator("video");
    await expect(video).toHaveAttribute("playbackrate", "1.5");
  });

  test("should work with keyboard shortcuts", async ({ page }) => {
    await page.goto("/vehiculos/toyota-corolla-2024/video-tour");

    // Space to play
    await page.keyboard.press("Space");

    // M to mute
    await page.keyboard.press("m");
    const video = page.locator("video");
    await expect(video).toHaveAttribute("muted");
  });

  test("should show gated state for FREE plan", async ({ page }) => {
    // Login as FREE dealer
    await page.goto("/login");
    await page.fill('[name="email"]', "free-dealer@test.com");
    await page.fill('[name="password"]', "test123");
    await page.click('button[type="submit"]');

    await page.goto("/dealer/crear-listing");
    await page.getByTestId("video-tour-section").click();

    await expect(page.getByText("Video Tour Premium")).toBeVisible();
    await expect(page.getByText("Actualizar a PRO")).toBeVisible();
  });
});

test.describe("CreateListingTestPage - Limits", () => {
  test("should show limit warning when near limit", async ({ page }) => {
    // Login as STARTER dealer at 80% capacity
    await page.goto("/test/create-listing?mockUsage=80");

    await expect(page.getByTestId("limit-warning")).toBeVisible();
    await expect(page.getByText(/Estás cerca del límite/)).toBeVisible();
  });

  test("should block form when at limit", async ({ page }) => {
    await page.goto("/test/create-listing?mockUsage=100");

    await expect(page.getByTestId("limit-reached-banner")).toBeVisible();
    await expect(page.getByRole("button", { name: "Publicar" })).toBeDisabled();
  });
});

test.describe("PlansComparisonTestPage", () => {
  test("should display all 4 plans", async ({ page }) => {
    await page.goto("/test/plans-comparison");

    await expect(page.getByText("FREE")).toBeVisible();
    await expect(page.getByText("STARTER")).toBeVisible();
    await expect(page.getByText("PRO")).toBeVisible();
    await expect(page.getByText("ENTERPRISE")).toBeVisible();
  });

  test("should highlight current plan", async ({ page }) => {
    await page.goto("/test/plans-comparison?currentPlan=STARTER");

    const starterCard = page.getByTestId("plan-card-STARTER");
    await expect(starterCard.getByText("Tu plan actual")).toBeVisible();
  });

  test("should show feature comparison table", async ({ page }) => {
    await page.goto("/test/plans-comparison");

    await expect(page.getByTestId("feature-table")).toBeVisible();
    await expect(page.getByText("Video Tour")).toBeVisible();
    await expect(page.getByText("Analytics Avanzado")).toBeVisible();
  });
});
```

---

## 📊 Analytics Events

```typescript
// filepath: src/lib/analytics/videoTourEvents.ts
import { analytics } from "@/lib/analytics";

export const videoTourEvents = {
  // Video playback
  videoPlay: (vehicleId: string, chapterId?: string) => {
    analytics.track("video_tour_play", { vehicleId, chapterId });
  },

  videoPause: (vehicleId: string, currentTime: number) => {
    analytics.track("video_tour_pause", { vehicleId, currentTime });
  },

  videoComplete: (vehicleId: string, totalDuration: number) => {
    analytics.track("video_tour_complete", { vehicleId, totalDuration });
  },

  // Chapter navigation
  chapterClick: (vehicleId: string, chapterId: string) => {
    analytics.track("video_tour_chapter_clicked", { vehicleId, chapterId });
  },

  // Controls
  toggleFullscreen: (vehicleId: string, isFullscreen: boolean) => {
    analytics.track("video_tour_fullscreen_toggle", {
      vehicleId,
      isFullscreen,
    });
  },

  changeSpeed: (vehicleId: string, speed: number) => {
    analytics.track("video_tour_speed_change", { vehicleId, speed });
  },

  // Engagement
  videoWatchTime: (
    vehicleId: string,
    watchTime: number,
    totalDuration: number,
  ) => {
    analytics.track("video_tour_watch_time", {
      vehicleId,
      watchTime,
      totalDuration,
      completionRate: (watchTime / totalDuration) * 100,
    });
  },

  // Feature gating
  featureGateHit: (feature: string, currentPlan: string) => {
    analytics.track("feature_gate_hit", { feature, currentPlan });
  },

  upgradePromptShown: (feature: string, currentPlan: string) => {
    analytics.track("upgrade_prompt_shown", { feature, currentPlan });
  },

  upgradePromptClicked: (
    feature: string,
    currentPlan: string,
    targetPlan: string,
  ) => {
    analytics.track("upgrade_prompt_clicked", {
      feature,
      currentPlan,
      targetPlan,
    });
  },
};
```

---

## ✅ Checklist de Implementación Completo

### VideoTourPage

- [ ] Video player con controles personalizados
- [ ] Play/pause, seek, volume controls
- [ ] Fullscreen toggle
- [ ] Playback speed selector
- [ ] Chapter navigation
- [ ] Active chapter highlight
- [ ] Auto-hide controls
- [ ] Keyboard shortcuts
- [ ] Loading state (skeleton)
- [ ] Error state con retry
- [ ] Mobile touch controls
- [ ] Analytics tracking

### Test Pages

- [ ] CreateListingTestPage con limit warnings
- [ ] PlansComparisonTestPage con feature table
- [ ] DealerAnalyticsTestPage con feature gates
- [ ] Mock data para todos los planes

### Hooks & Services

- [ ] useDealerFeatures hook
- [ ] useVideoPlayer hook
- [ ] DEALER_PLAN_LIMITS constants
- [ ] Feature gate utilities

### Feature Gating

- [ ] VideoTourGated component
- [ ] LimitReachedBanner component
- [ ] UpgradePrompt component
- [ ] hasFeature() checks en pages

---

## 🧪 TESTS E2E (Playwright)

```typescript
import { test, expect } from "@playwright/test";
import { loginAsDealer } from "../helpers/auth";

test.describe("Video Tour & Test Pages", () => {
  test("VideoTourPage debe mostrar reproductor con controles", async ({
    page,
  }) => {
    await page.goto("/vehicles/honda-accord-2024/video-tour");
    await expect(page.getByTestId("video-tour-page")).toBeVisible();
    await expect(page.getByTestId("video-player")).toBeVisible();
    await expect(page.getByTestId("play-pause-button")).toBeVisible();
    await expect(page.getByTestId("volume-control")).toBeVisible();
    await expect(page.getByTestId("fullscreen-button")).toBeVisible();
  });

  test("VideoTourPage debe mostrar progreso y tiempo", async ({ page }) => {
    await page.goto("/vehicles/honda-accord-2024/video-tour");
    await expect(page.getByTestId("video-progress")).toBeVisible();
    await expect(page.getByTestId("video-time")).toBeVisible();
  });

  test("CreateListingTestPage debe validar límites de plan", async ({
    page,
  }) => {
    await loginAsDealer(page);
    await page.goto("/test/create-listing");
    await expect(page.getByTestId("listing-limit-indicator")).toBeVisible();
    await expect(page.getByTestId("current-listings")).toBeVisible();
    await expect(page.getByTestId("max-listings")).toBeVisible();
  });

  test("PlansComparisonTestPage debe mostrar todos los planes", async ({
    page,
  }) => {
    await page.goto("/test/plans-comparison");
    await expect(page.getByTestId("plan-starter")).toBeVisible();
    await expect(page.getByTestId("plan-pro")).toBeVisible();
    await expect(page.getByTestId("plan-enterprise")).toBeVisible();
  });

  test("DealerAnalyticsTestPage debe mostrar feature gates", async ({
    page,
  }) => {
    await loginAsDealer(page);
    await page.goto("/test/dealer-analytics");
    await expect(page.getByTestId("analytics-test-page")).toBeVisible();
    await expect(page.getByTestId("feature-gate-status")).toBeVisible();
  });

  test("LimitReachedBanner debe mostrarse cuando se alcanza límite", async ({
    page,
  }) => {
    await loginAsDealer(page);
    await page.goto("/test/limit-reached");
    await expect(page.getByTestId("limit-reached-banner")).toBeVisible();
    await expect(
      page.getByRole("button", { name: /mejorar plan/i }),
    ).toBeVisible();
  });

  test("UpgradePrompt debe mostrar beneficios del upgrade", async ({
    page,
  }) => {
    await loginAsDealer(page);
    await page.goto("/test/upgrade-prompt");
    await expect(page.getByTestId("upgrade-prompt")).toBeVisible();
    await expect(page.getByTestId("upgrade-benefits")).toBeVisible();
  });
});
```

---

## ➡️ SIGUIENTE PASO

Continuar con: [06-shared-utils.md](./06-event-tracking-sdk.md)
