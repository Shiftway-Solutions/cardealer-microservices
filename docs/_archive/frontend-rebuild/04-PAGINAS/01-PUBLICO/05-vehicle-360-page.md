---
title: "Página Vista 360° de Vehículo"
priority: P0
estimated_time: "45 minutos"
dependencies: []
apis: []
status: complete
last_updated: "2026-01-30"
---

# 🔄 Página Vista 360° de Vehículo

> **Tiempo estimado:** 45 minutos
> **Prerrequisitos:** Vehicle360Viewer, Interior360Viewer, useVehicle360
> **Ruta:** `/vehiculos/:slug/360`

---

## 📋 OBJETIVO

Implementar página dedicada para la vista 360° interactiva:

- Vista exterior 360° a pantalla completa
- Vista interior con navegación
- Controles avanzados (zoom, auto-rotate, fullscreen)
- Navegación entre exterior e interior
- SEO optimizado
- Compartir en redes sociales

---

## 🎨 ESTRUCTURA DE LA PÁGINA

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ HEADER                                                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│ BREADCRUMB: Inicio > Vehículos > Toyota Camry 2024 > Vista 360°             │
├─────────────────────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │                                                                         │ │
│ │                                                                         │ │
│ │                         VEHICLE 360° VIEWER                             │ │
│ │                         (Exterior / Interior)                           │ │
│ │                         ◄── Drag to rotate ──►                          │ │
│ │                                                                         │ │
│ │                                                                         │ │
│ │   ○ ○ ○ ● ○ ○    [▶️ Auto] [🔍+ Zoom] [⛶ Fullscreen]                    │ │
│ │                                                                         │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────────────────────┤
│ TABS: [Exterior 360°] [Interior]                                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌────────────────────────────────────────────┬───────────────────────────┐ │
│  │  VEHICLE INFO (Left)                       │  ACTIONS (Right)          │ │
│  │  ────────────────────                      │  ─────────────────        │ │
│  │  🚗 2024 Toyota Camry SE                   │  [❤️ Guardar]             │ │
│  │  📍 Santo Domingo                          │  [📤 Compartir]           │ │
│  │  💰 RD$ 1,850,000                          │  [📞 Contactar]           │ │
│  │                                            │                           │ │
│  │  [Ver Detalles Completos →]                │  [💬 WhatsApp]            │ │
│  └────────────────────────────────────────────┴───────────────────────────┘ │
├─────────────────────────────────────────────────────────────────────────────┤
│ VEHÍCULOS SIMILARES CON 360°                                                │
│ [Card] [Card] [Card] [Card]                                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│ FOOTER                                                                       │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔧 PASO 1: Página Principal

```typescript
// filepath: src/app/(main)/vehiculos/[slug]/360/page.tsx

import { Metadata } from "next";
import { notFound } from "next/navigation";
import { Suspense } from "react";
import { Vehicle360PageContent } from "@/components/vehicle-360/Vehicle360PageContent";
import { vehicleService } from "@/lib/services/vehicleService";
import { vehicle360Service } from "@/lib/services/vehicle360Service";
import { formatPrice } from "@/lib/utils";

interface Vehicle360PageProps {
  params: Promise<{ slug: string }>;
}

// Generate metadata for SEO
export async function generateMetadata({
  params,
}: Vehicle360PageProps): Promise<Metadata> {
  const { slug } = await params;

  try {
    const vehicle = await vehicleService.getBySlug(slug);

    const title = `Vista 360° - ${vehicle.year} ${vehicle.make} ${vehicle.model} | OKLA`;
    const description = `Explora el ${vehicle.year} ${vehicle.make} ${vehicle.model} en vista 360° interactiva. Rota el vehículo, ve el interior y todos los detalles. ${formatPrice(vehicle.price)}.`;

    return {
      title,
      description,
      openGraph: {
        title,
        description,
        images: vehicle.images?.[0]?.url ? [vehicle.images[0].url] : [],
        type: "website",
      },
      twitter: {
        card: "summary_large_image",
        title,
        description,
        images: vehicle.images?.[0]?.url ? [vehicle.images[0].url] : [],
      },
    };
  } catch {
    return {
      title: "Vista 360° no disponible | OKLA",
    };
  }
}

export default async function Vehicle360Page({ params }: Vehicle360PageProps) {
  const { slug } = await params;

  let vehicle;
  let has360View = false;

  try {
    vehicle = await vehicleService.getBySlug(slug);
    has360View = await vehicle360Service.hasView(vehicle.id);
  } catch {
    notFound();
  }

  // Si no tiene vista 360°, redirigir al detalle
  if (!has360View) {
    notFound();
  }

  return (
    <Suspense fallback={<Vehicle360Skeleton />}>
      <Vehicle360PageContent vehicle={vehicle} />
    </Suspense>
  );
}

function Vehicle360Skeleton() {
  return (
    <div className="min-h-screen bg-gray-900 animate-pulse">
      <div className="h-12 bg-gray-800" />
      <div className="h-[60vh] bg-gray-800" />
      <div className="container py-6">
        <div className="h-32 bg-gray-800 rounded-xl" />
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 2: Componente Principal de Contenido

```typescript
// filepath: src/components/vehicle-360/Vehicle360PageContent.tsx

"use client";

import * as React from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { motion, AnimatePresence } from "framer-motion";
import {
  ArrowLeft,
  Heart,
  Share2,
  Phone,
  MessageCircle,
  ExternalLink,
  MapPin,
  Calendar,
  Gauge,
  Car,
  Sofa,
} from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/Tabs";
import { Breadcrumbs } from "@/components/ui/Breadcrumbs";
import { PriceDisplay } from "@/components/ui/PriceDisplay";
import { Vehicle360Viewer } from "@/components/vehicles/Vehicle360Viewer";
import { Interior360Viewer } from "@/components/vehicles/Interior360Viewer";
import { SimilarVehicles360 } from "./SimilarVehicles360";
import { useFavorites } from "@/lib/hooks/useFavorites";
import { showToast } from "@/lib/toast";
import { cn, formatNumber } from "@/lib/utils";
import type { Vehicle } from "@/types";

interface Vehicle360PageContentProps {
  vehicle: Vehicle;
}

export function Vehicle360PageContent({ vehicle }: Vehicle360PageContentProps) {
  const router = useRouter();
  const { isFavorite, toggleFavorite } = useFavorites();
  const [activeTab, setActiveTab] = React.useState<"exterior" | "interior">("exterior");

  const isFav = isFavorite(vehicle.id);
  const title = `${vehicle.year} ${vehicle.make} ${vehicle.model}`;

  const handleShare = async () => {
    const url = window.location.href;
    const text = `Vista 360° - ${title}`;

    if (navigator.share) {
      try {
        await navigator.share({ title: text, url });
      } catch {
        // User cancelled
      }
    } else {
      await navigator.clipboard.writeText(url);
      showToast.success("Enlace copiado", "El enlace se copió al portapapeles");
    }
  };

  const handleFavorite = () => {
    toggleFavorite(vehicle.id);
    showToast.success(
      isFav ? "Eliminado de favoritos" : "Agregado a favoritos",
      isFav
        ? "El vehículo fue eliminado de tu lista"
        : "El vehículo fue agregado a tu lista"
    );
  };

  const handleCall = () => {
    window.location.href = `tel:${vehicle.seller?.phone}`;
  };

  const handleWhatsApp = () => {
    const message = encodeURIComponent(
      `Hola, me interesa el ${title} que vi en OKLA (vista 360°). ¿Está disponible?`
    );
    window.open(
      `https://wa.me/${vehicle.seller?.phone?.replace(/\D/g, "")}?text=${message}`,
      "_blank"
    );
  };

  return (
    <div className="min-h-screen bg-gray-900">
      {/* Header */}
      <div className="bg-gray-800 border-b border-gray-700">
        <div className="container py-3">
          <div className="flex items-center justify-between">
            <Breadcrumbs
              items={[
                { label: "Inicio", href: "/" },
                { label: "Vehículos", href: "/vehiculos" },
                { label: title, href: `/vehiculos/${vehicle.slug}` },
                { label: "Vista 360°" },
              ]}
              className="text-gray-400"
            />

            <Button
              variant="ghost"
              size="sm"
              onClick={() => router.back()}
              className="text-gray-400 hover:text-white"
            >
              <ArrowLeft className="h-4 w-4 mr-2" />
              Volver
            </Button>
          </div>
        </div>
      </div>

      {/* Viewer Tabs */}
      <div className="bg-gray-900">
        <div className="container">
          <Tabs
            value={activeTab}
            onValueChange={(v) => setActiveTab(v as "exterior" | "interior")}
            className="w-full"
          >
            {/* Tab Controls */}
            <div className="flex items-center justify-center py-4 border-b border-gray-800">
              <TabsList className="bg-gray-800">
                <TabsTrigger
                  value="exterior"
                  className="data-[state=active]:bg-primary data-[state=active]:text-white"
                >
                  <Car className="h-4 w-4 mr-2" />
                  Exterior 360°
                </TabsTrigger>
                <TabsTrigger
                  value="interior"
                  className="data-[state=active]:bg-primary data-[state=active]:text-white"
                >
                  <Sofa className="h-4 w-4 mr-2" />
                  Interior
                </TabsTrigger>
              </TabsList>
            </div>

            {/* Viewer Content */}
            <AnimatePresence mode="wait">
              <TabsContent value="exterior" className="mt-0">
                <motion.div
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  exit={{ opacity: 0 }}
                >
                  <Vehicle360Viewer
                    vehicleId={vehicle.id}
                    height="60vh"
                    showThumbnails={true}
                    showControls={true}
                    className="w-full"
                  />
                </motion.div>
              </TabsContent>

              <TabsContent value="interior" className="mt-0">
                <motion.div
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  exit={{ opacity: 0 }}
                  className="min-h-[60vh] flex items-center justify-center"
                >
                  <Interior360Viewer
                    vehicleId={vehicle.id}
                    className="w-full max-w-5xl mx-auto"
                  />
                </motion.div>
              </TabsContent>
            </AnimatePresence>
          </Tabs>
        </div>
      </div>

      {/* Vehicle Info + Actions */}
      <div className="bg-gray-50 border-t">
        <div className="container py-6">
          <div className="flex flex-col lg:flex-row gap-6">
            {/* Vehicle Info */}
            <div className="flex-1">
              <div className="flex flex-wrap items-center gap-2 mb-2">
                {vehicle.condition === "new" && (
                  <Badge variant="new">Nuevo</Badge>
                )}
                {vehicle.isCertified && (
                  <Badge variant="certified">Certificado</Badge>
                )}
                {vehicle.sellerType === "dealer" && vehicle.isVerified && (
                  <Badge variant="dealer">Dealer Verificado</Badge>
                )}
              </div>

              <h1 className="text-2xl lg:text-3xl font-bold text-gray-900 mb-2">
                {title}
              </h1>

              {vehicle.trim && (
                <p className="text-lg text-gray-600 mb-4">{vehicle.trim}</p>
              )}

              <div className="flex flex-wrap items-center gap-4 text-sm text-gray-600 mb-4">
                <div className="flex items-center gap-1">
                  <Calendar size={16} className="text-gray-400" />
                  <span>{vehicle.year}</span>
                </div>
                {vehicle.mileage !== undefined && (
                  <div className="flex items-center gap-1">
                    <Gauge size={16} className="text-gray-400" />
                    <span>{formatNumber(vehicle.mileage)} km</span>
                  </div>
                )}
                <div className="flex items-center gap-1">
                  <MapPin size={16} className="text-gray-400" />
                  <span>{vehicle.city}</span>
                </div>
              </div>

              <PriceDisplay
                price={vehicle.price}
                originalPrice={vehicle.originalPrice}
                size="lg"
              />

              <Link
                href={`/vehiculos/${vehicle.slug}`}
                className="inline-flex items-center gap-2 text-primary hover:underline mt-4"
              >
                Ver detalles completos
                <ExternalLink size={16} />
              </Link>
            </div>

            {/* Actions */}
            <div className="lg:w-80">
              <div className="bg-white rounded-xl p-6 shadow-sm border">
                <h3 className="font-semibold text-gray-900 mb-4">
                  ¿Te interesa este vehículo?
                </h3>

                <div className="space-y-3">
                  <Button
                    className="w-full"
                    onClick={handleWhatsApp}
                  >
                    <MessageCircle className="h-4 w-4 mr-2" />
                    Escribir por WhatsApp
                  </Button>

                  <Button
                    variant="outline"
                    className="w-full"
                    onClick={handleCall}
                  >
                    <Phone className="h-4 w-4 mr-2" />
                    Llamar ahora
                  </Button>

                  <div className="flex gap-2">
                    <Button
                      variant={isFav ? "default" : "outline"}
                      className="flex-1"
                      onClick={handleFavorite}
                    >
                      <Heart
                        size={16}
                        className={cn(isFav && "fill-current")}
                      />
                      <span className="ml-2">
                        {isFav ? "Guardado" : "Guardar"}
                      </span>
                    </Button>

                    <Button variant="outline" onClick={handleShare}>
                      <Share2 size={16} />
                    </Button>
                  </div>
                </div>

                {/* Seller info */}
                {vehicle.seller && (
                  <div className="mt-6 pt-4 border-t">
                    <p className="text-sm text-gray-500 mb-1">Vendido por</p>
                    <p className="font-medium text-gray-900">
                      {vehicle.seller.name || vehicle.seller.businessName}
                    </p>
                    {vehicle.sellerType === "dealer" && (
                      <Link
                        href={`/dealers/${vehicle.seller.id}`}
                        className="text-sm text-primary hover:underline"
                      >
                        Ver perfil del dealer
                      </Link>
                    )}
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Similar Vehicles with 360° */}
      <div className="bg-white border-t">
        <div className="container py-12">
          <h2 className="text-2xl font-bold text-gray-900 mb-6">
            Más vehículos con Vista 360°
          </h2>
          <SimilarVehicles360
            currentVehicleId={vehicle.id}
            makeId={vehicle.makeId}
            limit={4}
          />
        </div>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 3: Vehículos Similares con 360°

```typescript
// filepath: src/components/vehicle-360/SimilarVehicles360.tsx

"use client";

import * as React from "react";
import Link from "next/link";
import Image from "next/image";
import { motion } from "framer-motion";
import { RotateCcw, MapPin, Calendar } from "lucide-react";
import { Badge } from "@/components/ui/Badge";
import { PriceDisplay } from "@/components/ui/PriceDisplay";
import { cn, formatNumber } from "@/lib/utils";
import { vehicleService } from "@/lib/services/vehicleService";
import type { Vehicle } from "@/types";

interface SimilarVehicles360Props {
  currentVehicleId: string;
  makeId?: string;
  limit?: number;
  className?: string;
}

export function SimilarVehicles360({
  currentVehicleId,
  makeId,
  limit = 4,
  className,
}: SimilarVehicles360Props) {
  const [vehicles, setVehicles] = React.useState<Vehicle[]>([]);
  const [isLoading, setIsLoading] = React.useState(true);

  React.useEffect(() => {
    async function fetchSimilar() {
      try {
        // Fetch vehículos con vista 360° disponible
        const response = await vehicleService.search({
          has360View: true,
          makeId,
          excludeId: currentVehicleId,
          limit,
          sortBy: "featured",
        });
        setVehicles(response.items);
      } catch (error) {
        console.error("Error fetching similar vehicles:", error);
      } finally {
        setIsLoading(false);
      }
    }

    fetchSimilar();
  }, [currentVehicleId, makeId, limit]);

  if (isLoading) {
    return (
      <div className={cn("grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6", className)}>
        {Array.from({ length: limit }).map((_, i) => (
          <div key={i} className="animate-pulse">
            <div className="bg-gray-200 rounded-xl h-48" />
            <div className="mt-3 space-y-2">
              <div className="h-4 bg-gray-200 rounded w-3/4" />
              <div className="h-4 bg-gray-200 rounded w-1/2" />
            </div>
          </div>
        ))}
      </div>
    );
  }

  if (vehicles.length === 0) {
    return (
      <div className={cn("text-center py-12 text-gray-500", className)}>
        <RotateCcw className="h-12 w-12 mx-auto text-gray-300 mb-3" />
        <p>No hay más vehículos con vista 360° disponibles</p>
      </div>
    );
  }

  return (
    <div className={cn("grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6", className)}>
      {vehicles.map((vehicle, index) => (
        <motion.div
          key={vehicle.id}
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: index * 0.1 }}
        >
          <Link
            href={`/vehiculos/${vehicle.slug}/360`}
            className="group block bg-white rounded-xl overflow-hidden border shadow-sm hover:shadow-lg transition-shadow"
          >
            {/* Image */}
            <div className="relative aspect-[4/3] overflow-hidden bg-gray-100">
              <Image
                src={vehicle.images?.[0]?.url || "/placeholder-vehicle.jpg"}
                alt={`${vehicle.year} ${vehicle.make} ${vehicle.model}`}
                fill
                className="object-cover group-hover:scale-105 transition-transform duration-300"
              />

              {/* 360° Badge */}
              <div className="absolute top-2 left-2">
                <Badge className="bg-blue-600 text-white gap-1">
                  <RotateCcw className="h-3 w-3" />
                  360°
                </Badge>
              </div>
            </div>

            {/* Content */}
            <div className="p-4">
              <h3 className="font-semibold text-gray-900 group-hover:text-primary transition-colors">
                {vehicle.year} {vehicle.make} {vehicle.model}
              </h3>

              {vehicle.trim && (
                <p className="text-sm text-gray-500 truncate">{vehicle.trim}</p>
              )}

              <div className="flex items-center gap-3 mt-2 text-xs text-gray-500">
                <span className="flex items-center gap-1">
                  <Calendar size={12} />
                  {vehicle.year}
                </span>
                {vehicle.mileage !== undefined && (
                  <span className="flex items-center gap-1">
                    {formatNumber(vehicle.mileage)} km
                  </span>
                )}
                <span className="flex items-center gap-1">
                  <MapPin size={12} />
                  {vehicle.city}
                </span>
              </div>

              <div className="mt-3">
                <PriceDisplay price={vehicle.price} size="sm" />
              </div>
            </div>
          </Link>
        </motion.div>
      ))}
    </div>
  );
}
```

---

## 🔧 PASO 4: Componente de Link 360° para usar en VehicleCard

```typescript
// filepath: src/components/vehicles/Vehicle360Badge.tsx

"use client";

import * as React from "react";
import Link from "next/link";
import { motion } from "framer-motion";
import { RotateCcw } from "lucide-react";
import { cn } from "@/lib/utils";

interface Vehicle360BadgeProps {
  slug: string;
  className?: string;
  variant?: "badge" | "button";
}

export function Vehicle360Badge({
  slug,
  className,
  variant = "badge",
}: Vehicle360BadgeProps) {
  if (variant === "button") {
    return (
      <Link
        href={`/vehiculos/${slug}/360`}
        className={cn(
          "inline-flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg",
          "hover:bg-blue-700 transition-colors text-sm font-medium",
          className
        )}
      >
        <motion.div
          animate={{ rotate: 360 }}
          transition={{ duration: 3, repeat: Infinity, ease: "linear" }}
        >
          <RotateCcw className="h-4 w-4" />
        </motion.div>
        Ver en 360°
      </Link>
    );
  }

  return (
    <Link
      href={`/vehiculos/${slug}/360`}
      className={cn(
        "inline-flex items-center gap-1 px-2 py-1 bg-blue-600 text-white rounded-full",
        "hover:bg-blue-700 transition-colors text-xs font-medium",
        className
      )}
      onClick={(e) => e.stopPropagation()}
    >
      <motion.div
        animate={{ rotate: 360 }}
        transition={{ duration: 3, repeat: Infinity, ease: "linear" }}
      >
        <RotateCcw className="h-3 w-3" />
      </motion.div>
      360°
    </Link>
  );
}
```

---

## 🔧 PASO 5: Actualizar VehicleCard para mostrar badge 360°

```typescript
// En src/components/vehicles/VehicleCard.tsx agregar:

import { Vehicle360Badge } from "./Vehicle360Badge";

// Dentro del componente, agregar en la imagen:
<div className="relative aspect-[4/3] overflow-hidden">
  <Image src={...} alt={...} fill className="object-cover" />

  {/* Badge 360° si el vehículo tiene vista disponible */}
  {vehicle.has360View && (
    <div className="absolute top-2 left-2">
      <Vehicle360Badge slug={vehicle.slug} />
    </div>
  )}
</div>
```

---

## 📱 RESPONSIVE DESIGN

### Desktop (≥1024px)

- Viewer ocupa 60vh de altura
- Info y acciones lado a lado
- Thumbnails visibles

### Tablet (768-1023px)

- Viewer ocupa 50vh
- Info y acciones stack vertical
- Thumbnails más pequeños

### Mobile (<768px)

- Viewer ocupa 40vh
- Todo en columna
- Controles compactos
- Touch gestures optimizados

---

## 🎨 ESTILOS ESPECÍFICOS

```css
/* Para el viewer en modo oscuro */
.vehicle-360-page {
  --viewer-bg: theme("colors.gray.900");
  --viewer-controls-bg: rgba(255, 255, 255, 0.9);
  --viewer-text: theme("colors.gray.100");
}

/* Animación del badge 360° */
@keyframes rotate360 {
  from {
    transform: rotate(0deg);
  }
  to {
    transform: rotate(360deg);
  }
}

.badge-360-icon {
  animation: rotate360 3s linear infinite;
}
```

---

## ✅ CHECKLIST DE IMPLEMENTACIÓN

- [ ] Crear página `/vehiculos/[slug]/360/page.tsx`
- [ ] Crear `Vehicle360PageContent` component
- [ ] Crear `SimilarVehicles360` component
- [ ] Crear `Vehicle360Badge` component
- [ ] Actualizar `VehicleCard` para mostrar badge
- [ ] Verificar SEO metadata
- [ ] Probar navegación entre exterior/interior
- [ ] Probar compartir en redes sociales
- [ ] Verificar responsive en mobile
- [ ] Probar fullscreen mode

---

## � PASO 8: Video Streaming HLS Optimizado (P2 - Performance) 🎬

```typescript
// filepath: src/components/vehicles/OptimizedVideoPlayer.tsx
"use client";

import * as React from "react";
import Plyr from "plyr-react";
import Hls from "hls.js";
import { Loader2, AlertCircle, Wifi, WifiOff } from "lucide-react";
import { cn } from "@/lib/utils";
import "plyr-react/plyr.css";

interface OptimizedVideoPlayerProps {
  src: string; // HLS manifest URL (.m3u8)
  poster?: string;
  className?: string;
  onReady?: () => void;
  onError?: (error: Error) => void;
}

export function OptimizedVideoPlayer({
  src,
  poster,
  className,
  onReady,
  onError,
}: OptimizedVideoPlayerProps) {
  const videoRef = React.useRef<HTMLVideoElement>(null);
  const hlsRef = React.useRef<Hls | null>(null);
  const [isLoading, setIsLoading] = React.useState(true);
  const [error, setError] = React.useState<string | null>(null);
  const [quality, setQuality] = React.useState<string>("auto");
  const [buffering, setBuffering] = React.useState(false);

  React.useEffect(() => {
    const video = videoRef.current;
    if (!video) return;

    // Check if HLS is supported
    if (Hls.isSupported()) {
      const hls = new Hls({
        // Enable adaptive bitrate streaming
        enableWorker: true,
        lowLatencyMode: true,
        // Buffer configuration
        maxBufferLength: 30, // seconds
        maxMaxBufferLength: 60,
        // Quality selection
        startLevel: -1, // Auto start quality
        capLevelToPlayerSize: true, // Limit quality to player size
        // Network optimization
        manifestLoadingTimeOut: 10000,
        manifestLoadingMaxRetry: 3,
        levelLoadingTimeOut: 10000,
      });

      hlsRef.current = hls;

      // Load HLS manifest
      hls.loadSource(src);
      hls.attachMedia(video);

      // Events
      hls.on(Hls.Events.MANIFEST_PARSED, () => {
        setIsLoading(false);
        if (onReady) onReady();

        // Log available qualities
        const levels = hls.levels.map((level) => ({
          height: level.height,
          bitrate: level.bitrate,
          label: `${level.height}p`,
        }));
        console.log("Available qualities:", levels);
      });

      hls.on(Hls.Events.ERROR, (event, data) => {
        console.error("HLS error:", data);
        if (data.fatal) {
          switch (data.type) {
            case Hls.ErrorTypes.NETWORK_ERROR:
              setError("Error de red. Verifica tu conexión.");
              hls.startLoad();
              break;
            case Hls.ErrorTypes.MEDIA_ERROR:
              setError("Error de reproducción. Intentando recuperar...");
              hls.recoverMediaError();
              break;
            default:
              setError("Error al cargar el video.");
              if (onError) onError(new Error(data.details));
              break;
          }
        }
      });

      hls.on(Hls.Events.LEVEL_SWITCHED, (event, data) => {
        const level = hls.levels[data.level];
        setQuality(`${level.height}p`);
      });

      // Buffer events
      video.addEventListener("waiting", () => setBuffering(true));
      video.addEventListener("canplay", () => setBuffering(false));

      return () => {
        hls.destroy();
        video.removeEventListener("waiting", () => setBuffering(true));
        video.removeEventListener("canplay", () => setBuffering(false));
      };
    } else if (video.canPlayType("application/vnd.apple.mpegurl")) {
      // Native HLS support (Safari)
      video.src = src;
      video.addEventListener("loadedmetadata", () => {
        setIsLoading(false);
        if (onReady) onReady();
      });
      video.addEventListener("error", () => {
        setError("Error al cargar el video.");
      });
    } else {
      setError("Tu navegador no soporta streaming HLS.");
    }
  }, [src, onReady, onError]);

  // Manual quality selection
  const changeQuality = (levelIndex: number) => {
    if (hlsRef.current) {
      hlsRef.current.currentLevel = levelIndex;
    }
  };

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center h-full bg-gray-900 text-white p-8">
        <AlertCircle className="h-12 w-12 text-red-500 mb-4" />
        <p className="text-lg font-medium">{error}</p>
        <button
          onClick={() => {
            setError(null);
            setIsLoading(true);
            if (hlsRef.current) {
              hlsRef.current.startLoad();
            }
          }}
          className="mt-4 px-4 py-2 bg-white text-gray-900 rounded-lg hover:bg-gray-100"
        >
          Reintentar
        </button>
      </div>
    );
  }

  return (
    <div className={cn("relative", className)}>
      {isLoading && (
        <div className="absolute inset-0 flex items-center justify-center bg-gray-900">
          <Loader2 className="h-8 w-8 text-white animate-spin" />
        </div>
      )}

      {buffering && (
        <div className="absolute top-4 right-4 z-10 bg-black/70 text-white px-3 py-1.5 rounded-full text-sm flex items-center gap-2">
          <Loader2 className="h-4 w-4 animate-spin" />
          Cargando...
        </div>
      )}

      {/* Quality indicator */}
      {quality !== "auto" && (
        <div className="absolute top-4 left-4 z-10 bg-black/70 text-white px-2 py-1 rounded text-xs flex items-center gap-1">
          <Wifi className="h-3 w-3" />
          {quality}
        </div>
      )}

      <Plyr
        ref={videoRef as any}
        source={{
          type: "video",
          sources: [{ src, type: "application/x-mpegURL" }],
          poster,
        }}
        options={{
          controls: [
            "play-large",
            "play",
            "progress",
            "current-time",
            "duration",
            "mute",
            "volume",
            "settings",
            "fullscreen",
          ],
          settings: ["quality", "speed"],
          quality: {
            default: 720,
            options: [360, 720, 1080],
          },
          speed: {
            selected: 1,
            options: [0.5, 0.75, 1, 1.25, 1.5, 2],
          },
        }}
      />
    </div>
  );
}
```

**Instalación de dependencias:**

```bash
pnpm add plyr-react hls.js
pnpm add -D @types/hls.js
```

**Uso en VideoTourPage:**

```typescript
import { OptimizedVideoPlayer } from "@/components/vehicles/OptimizedVideoPlayer";

export function VideoTourPage({ vehicle }) {
  return (
    <OptimizedVideoPlayer
      src={vehicle.videoHlsUrl} // URL del manifest .m3u8
      poster={vehicle.thumbnailUrl}
      className="w-full aspect-video"
      onReady={() => console.log("Video ready")}
      onError={(error) => console.error("Video error:", error)}
    />
  );
}
```

---

## �🔗 RUTAS RELACIONADAS

| Ruta                     | Componente        | Descripción            |
| ------------------------ | ----------------- | ---------------------- |
| `/vehiculos/:slug`       | VehicleDetailPage | Detalle normal         |
| `/vehiculos/:slug/360`   | Vehicle360Page    | Vista 360° dedicada    |
| `/vehiculos/:slug/video` | VideoTourPage     | Tour en video (futuro) |

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/vehicle-360.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Vehicle 360° View Page", () => {
  const vehicleSlug = "toyota-camry-2023-santo-domingo";

  test("debe cargar visor 360° completo", async ({ page }) => {
    await page.goto(`/vehiculos/${vehicleSlug}/360`);

    await expect(page.getByTestId("viewer-360")).toBeVisible();
    await expect(page.getByTestId("hotspot-markers")).toBeVisible();
  });

  test("debe permitir rotación con drag", async ({ page }) => {
    await page.goto(`/vehiculos/${vehicleSlug}/360`);

    const viewer = page.getByTestId("viewer-360");
    const box = await viewer.boundingBox();

    await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
    await page.mouse.down();
    await page.mouse.move(box.x + box.width / 2 + 100, box.y + box.height / 2);
    await page.mouse.up();

    // Verificar que la imagen cambió (frame diferente)
    await expect(page.getByTestId("current-frame")).not.toHaveAttribute(
      "data-frame",
      "0",
    );
  });

  test("debe mostrar hotspots informativos", async ({ page }) => {
    await page.goto(`/vehiculos/${vehicleSlug}/360`);

    await page.getByTestId("hotspot-engine").click();
    await expect(page.getByTestId("hotspot-tooltip")).toBeVisible();
    await expect(page.getByTestId("hotspot-tooltip")).toContainText(/motor/i);
  });

  test("debe soportar zoom in/out", async ({ page }) => {
    await page.goto(`/vehiculos/${vehicleSlug}/360`);

    await page.getByRole("button", { name: /zoom in/i }).click();
    await expect(page.getByTestId("viewer-360")).toHaveAttribute(
      "data-zoom",
      "2",
    );

    await page.getByRole("button", { name: /zoom out/i }).click();
    await expect(page.getByTestId("viewer-360")).toHaveAttribute(
      "data-zoom",
      "1",
    );
  });

  test("debe tener autoplay toggle", async ({ page }) => {
    await page.goto(`/vehiculos/${vehicleSlug}/360`);

    await page.getByRole("button", { name: /autoplay/i }).click();
    await expect(page.getByTestId("viewer-360")).toHaveAttribute(
      "data-autoplay",
      "true",
    );
  });
});
```

---

**Anterior:** [03-COMPONENTES/06-vehicle-360-viewer.md](../03-COMPONENTES/06-vehicle-360-viewer.md)
**Siguiente:** [05-API-INTEGRATION/05-vehicle-360-api.md](../05-API-INTEGRATION/05-vehicle-360-api.md)
