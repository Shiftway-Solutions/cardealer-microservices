# 🚗 Componentes de Vehículos

> **Tiempo estimado:** 90 minutos
> **Prerrequisitos:** Design tokens, componentes base, tipos TypeScript

---

## 📋 OBJETIVO

Implementar todos los componentes relacionados con vehículos:

- VehicleCard con variantes
- VehicleGrid responsive
- VehicleGallery con lightbox
- VehicleSpecs
- VehicleComparison
- SimilarVehicles

---

## 🔧 PASO 1: VehicleCard Completo

### Card Principal

```typescript
// filepath: src/components/vehicles/VehicleCard.tsx
"use client";

import * as React from "react";
import Image from "next/image";
import Link from "next/link";
import { Heart, MapPin, Gauge, Calendar, Check, Eye } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { PriceDisplay } from "@/components/ui/PriceDisplay";
import { cn, formatNumber } from "@/lib/utils";
import type { VehicleSummary } from "@/types";

interface VehicleCardProps {
  vehicle: VehicleSummary;
  isFavorited?: boolean;
  onFavoriteClick?: (vehicleId: string) => void;
  variant?: "default" | "horizontal" | "compact";
  showViews?: boolean;
  priority?: boolean;
  className?: string;
}

export function VehicleCard({
  vehicle,
  isFavorited = false,
  onFavoriteClick,
  variant = "default",
  showViews = false,
  priority = false,
  className,
}: VehicleCardProps) {
  const handleFavoriteClick = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    onFavoriteClick?.(vehicle.id);
  };

  const title = `${vehicle.year} ${vehicle.make} ${vehicle.model}`;
  const isDealer = vehicle.sellerType === "dealer";
  const isNew = vehicle.condition === "new";

  if (variant === "horizontal") {
    return (
      <VehicleCardHorizontal
        vehicle={vehicle}
        title={title}
        isFavorited={isFavorited}
        onFavoriteClick={handleFavoriteClick}
        isDealer={isDealer}
        isNew={isNew}
        showViews={showViews}
        className={className}
      />
    );
  }

  if (variant === "compact") {
    return (
      <VehicleCardCompact
        vehicle={vehicle}
        title={title}
        className={className}
      />
    );
  }

  return (
    <article
      className={cn(
        "group relative bg-white rounded-xl border border-gray-200 overflow-hidden",
        "transition-all duration-300 hover:shadow-lg hover:border-gray-300",
        "focus-within:ring-2 focus-within:ring-primary-500 focus-within:ring-offset-2",
        className
      )}
    >
      {/* Image Container */}
      <Link
        href={`/vehiculos/${vehicle.slug}`}
        className="block relative aspect-[4/3] overflow-hidden"
      >
        <Image
          src={vehicle.primaryImage || "/images/vehicle-placeholder.jpg"}
          alt={title}
          fill
          sizes="(max-width: 640px) 100vw, (max-width: 1024px) 50vw, 33vw"
          className="object-cover transition-transform duration-500 group-hover:scale-105"
          priority={priority}
        />

        {/* Overlay Gradient */}
        <div className="absolute inset-0 bg-gradient-to-t from-black/40 via-transparent to-transparent opacity-0 group-hover:opacity-100 transition-opacity" />

        {/* Badges */}
        <div className="absolute top-3 left-3 flex flex-wrap gap-2">
          {isNew && (
            <Badge variant="success" size="sm">
              Nuevo
            </Badge>
          )}
          {isDealer && vehicle.isVerified && (
            <Badge variant="primary" size="sm" className="gap-1">
              <Check size={12} />
              Verificado
            </Badge>
          )}
        </div>

        {/* Favorite Button */}
        {onFavoriteClick && (
          <Button
            variant="ghost"
            size="icon"
            onClick={handleFavoriteClick}
            className={cn(
              "absolute top-3 right-3 h-9 w-9 rounded-full bg-white/90 backdrop-blur-sm shadow-sm",
              "hover:bg-white hover:scale-110",
              "transition-all duration-200"
            )}
            aria-label={
              isFavorited ? "Quitar de favoritos" : "Agregar a favoritos"
            }
          >
            <Heart
              size={18}
              className={cn(
                "transition-colors",
                isFavorited
                  ? "fill-red-500 text-red-500"
                  : "text-gray-600"
              )}
            />
          </Button>
        )}

        {/* Views Count */}
        {showViews && vehicle.views !== undefined && (
          <div className="absolute bottom-3 left-3 flex items-center gap-1 text-white text-xs bg-black/50 rounded-full px-2 py-1">
            <Eye size={12} />
            {formatNumber(vehicle.views)}
          </div>
        )}
      </Link>

      {/* Content */}
      <div className="p-4">
        {/* Title */}
        <Link href={`/vehiculos/${vehicle.slug}`}>
          <h3 className="font-semibold text-gray-900 line-clamp-1 group-hover:text-primary-600 transition-colors">
            {title}
          </h3>
        </Link>

        {/* Subtitle - Trim if available */}
        {vehicle.trim && (
          <p className="text-sm text-gray-500 line-clamp-1 mt-0.5">
            {vehicle.trim}
          </p>
        )}

        {/* Price */}
        <div className="mt-3">
          <PriceDisplay
            price={vehicle.price}
            originalPrice={vehicle.originalPrice}
            size="lg"
          />
        </div>

        {/* Specs Row */}
        <div className="flex items-center gap-4 mt-3 text-sm text-gray-600">
          {vehicle.mileage !== undefined && (
            <div className="flex items-center gap-1">
              <Gauge size={14} className="text-gray-400" />
              <span>{formatNumber(vehicle.mileage)} km</span>
            </div>
          )}
          <div className="flex items-center gap-1">
            <Calendar size={14} className="text-gray-400" />
            <span>{vehicle.year}</span>
          </div>
        </div>

        {/* Location */}
        <div className="flex items-center gap-1 mt-2 text-sm text-gray-500">
          <MapPin size={14} className="text-gray-400" />
          <span>{vehicle.city}</span>
        </div>
      </div>
    </article>
  );
}

// Horizontal Variant
interface HorizontalProps {
  vehicle: VehicleSummary;
  title: string;
  isFavorited: boolean;
  onFavoriteClick: (e: React.MouseEvent) => void;
  isDealer: boolean;
  isNew: boolean;
  showViews: boolean;
  className?: string;
}

function VehicleCardHorizontal({
  vehicle,
  title,
  isFavorited,
  onFavoriteClick,
  isDealer,
  isNew,
  showViews,
  className,
}: HorizontalProps) {
  return (
    <article
      className={cn(
        "group relative bg-white rounded-xl border border-gray-200 overflow-hidden",
        "flex flex-col sm:flex-row",
        "transition-all duration-300 hover:shadow-lg",
        className
      )}
    >
      {/* Image */}
      <Link
        href={`/vehiculos/${vehicle.slug}`}
        className="relative w-full sm:w-72 aspect-[4/3] sm:aspect-auto sm:h-48 flex-shrink-0"
      >
        <Image
          src={vehicle.primaryImage || "/images/vehicle-placeholder.jpg"}
          alt={title}
          fill
          sizes="(max-width: 640px) 100vw, 288px"
          className="object-cover"
        />

        {/* Badges */}
        <div className="absolute top-3 left-3 flex gap-2">
          {isNew && <Badge variant="success" size="sm">Nuevo</Badge>}
          {isDealer && vehicle.isVerified && (
            <Badge variant="primary" size="sm">Verificado</Badge>
          )}
        </div>
      </Link>

      {/* Content */}
      <div className="flex-1 p-4 flex flex-col justify-between">
        <div>
          <Link href={`/vehiculos/${vehicle.slug}`}>
            <h3 className="font-semibold text-lg text-gray-900 group-hover:text-primary-600 transition-colors">
              {title}
            </h3>
          </Link>

          <div className="flex flex-wrap gap-x-4 gap-y-1 mt-2 text-sm text-gray-600">
            {vehicle.mileage !== undefined && (
              <span>{formatNumber(vehicle.mileage)} km</span>
            )}
            <span>{vehicle.year}</span>
            <span className="flex items-center gap-1">
              <MapPin size={14} />
              {vehicle.city}
            </span>
          </div>
        </div>

        <div className="flex items-center justify-between mt-4">
          <PriceDisplay price={vehicle.price} size="lg" />

          <Button
            variant="ghost"
            size="icon"
            onClick={onFavoriteClick}
            aria-label={isFavorited ? "Quitar de favoritos" : "Agregar a favoritos"}
          >
            <Heart
              size={20}
              className={isFavorited ? "fill-red-500 text-red-500" : "text-gray-400"}
            />
          </Button>
        </div>
      </div>
    </article>
  );
}

// Compact Variant
interface CompactProps {
  vehicle: VehicleSummary;
  title: string;
  className?: string;
}

function VehicleCardCompact({ vehicle, title, className }: CompactProps) {
  return (
    <Link
      href={`/vehiculos/${vehicle.slug}`}
      className={cn(
        "group flex gap-3 p-3 bg-white rounded-lg border border-gray-200",
        "hover:border-gray-300 hover:shadow-sm transition-all",
        className
      )}
    >
      <div className="relative w-24 h-18 rounded-md overflow-hidden flex-shrink-0">
        <Image
          src={vehicle.primaryImage || "/images/vehicle-placeholder.jpg"}
          alt={title}
          fill
          sizes="96px"
          className="object-cover"
        />
      </div>

      <div className="flex-1 min-w-0">
        <h4 className="font-medium text-gray-900 line-clamp-1 text-sm group-hover:text-primary-600">
          {title}
        </h4>
        <p className="text-xs text-gray-500 mt-0.5">
          {formatNumber(vehicle.mileage ?? 0)} km • {vehicle.city}
        </p>
        <p className="font-semibold text-primary-600 mt-1">
          RD$ {formatNumber(vehicle.price)}
        </p>
      </div>
    </Link>
  );
}
```

### Skeleton

```typescript
// filepath: src/components/vehicles/VehicleCardSkeleton.tsx
import { cn } from "@/lib/utils";

interface VehicleCardSkeletonProps {
  variant?: "default" | "horizontal" | "compact";
  className?: string;
}

export function VehicleCardSkeleton({
  variant = "default",
  className,
}: VehicleCardSkeletonProps) {
  if (variant === "horizontal") {
    return (
      <div
        className={cn(
          "flex flex-col sm:flex-row bg-white rounded-xl border border-gray-200 overflow-hidden animate-pulse",
          className
        )}
      >
        <div className="w-full sm:w-72 aspect-[4/3] sm:aspect-auto sm:h-48 bg-gray-200" />
        <div className="flex-1 p-4 space-y-3">
          <div className="h-6 w-3/4 bg-gray-200 rounded" />
          <div className="h-4 w-1/2 bg-gray-200 rounded" />
          <div className="h-8 w-1/3 bg-gray-200 rounded mt-4" />
        </div>
      </div>
    );
  }

  if (variant === "compact") {
    return (
      <div
        className={cn(
          "flex gap-3 p-3 bg-white rounded-lg border border-gray-200 animate-pulse",
          className
        )}
      >
        <div className="w-24 h-18 bg-gray-200 rounded-md" />
        <div className="flex-1 space-y-2">
          <div className="h-4 w-3/4 bg-gray-200 rounded" />
          <div className="h-3 w-1/2 bg-gray-200 rounded" />
          <div className="h-5 w-1/3 bg-gray-200 rounded" />
        </div>
      </div>
    );
  }

  return (
    <div
      className={cn(
        "bg-white rounded-xl border border-gray-200 overflow-hidden animate-pulse",
        className
      )}
    >
      <div className="aspect-[4/3] bg-gray-200" />
      <div className="p-4 space-y-3">
        <div className="h-5 w-3/4 bg-gray-200 rounded" />
        <div className="h-7 w-1/2 bg-gray-200 rounded" />
        <div className="flex gap-4">
          <div className="h-4 w-20 bg-gray-200 rounded" />
          <div className="h-4 w-16 bg-gray-200 rounded" />
        </div>
        <div className="h-4 w-24 bg-gray-200 rounded" />
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 2: VehicleGrid

```typescript
// filepath: src/components/vehicles/VehicleGrid.tsx
"use client";

import * as React from "react";
import { VehicleCard } from "./VehicleCard";
import { VehicleCardSkeleton } from "./VehicleCardSkeleton";
import { EmptyState } from "@/components/ui/EmptyState";
import { cn } from "@/lib/utils";
import type { VehicleSummary } from "@/types";

interface VehicleGridProps {
  vehicles: VehicleSummary[];
  isLoading?: boolean;
  skeletonCount?: number;
  favorites?: Set<string>;
  onFavoriteClick?: (vehicleId: string) => void;
  variant?: "default" | "horizontal";
  columns?: 2 | 3 | 4;
  emptyMessage?: string;
  emptyDescription?: string;
  className?: string;
}

export function VehicleGrid({
  vehicles,
  isLoading = false,
  skeletonCount = 6,
  favorites = new Set(),
  onFavoriteClick,
  variant = "default",
  columns = 3,
  emptyMessage = "No se encontraron vehículos",
  emptyDescription = "Intenta ajustar los filtros de búsqueda",
  className,
}: VehicleGridProps) {
  const gridClasses = cn(
    "grid gap-6",
    variant === "horizontal"
      ? "grid-cols-1"
      : {
          "grid-cols-1 sm:grid-cols-2": columns === 2,
          "grid-cols-1 sm:grid-cols-2 lg:grid-cols-3": columns === 3,
          "grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4": columns === 4,
        },
    className
  );

  // Loading state
  if (isLoading) {
    return (
      <div className={gridClasses}>
        {Array.from({ length: skeletonCount }).map((_, i) => (
          <VehicleCardSkeleton key={i} variant={variant} />
        ))}
      </div>
    );
  }

  // Empty state
  if (vehicles.length === 0) {
    return (
      <EmptyState
        variant="search"
        title={emptyMessage}
        description={emptyDescription}
        className="py-12"
      />
    );
  }

  return (
    <div className={gridClasses}>
      {vehicles.map((vehicle, index) => (
        <VehicleCard
          key={vehicle.id}
          vehicle={vehicle}
          variant={variant}
          isFavorited={favorites.has(vehicle.id)}
          onFavoriteClick={onFavoriteClick}
          priority={index < 3} // Prioritize first 3 images
        />
      ))}
    </div>
  );
}
```

---

## 🔧 PASO 3: VehicleGallery

```typescript
// filepath: src/components/vehicles/VehicleGallery.tsx
"use client";

import * as React from "react";
import Image from "next/image";
import { Dialog, DialogContent, DialogTitle } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { ChevronLeft, ChevronRight, X, ZoomIn, Expand } from "lucide-react";
import { cn } from "@/lib/utils";
import type { VehicleImage } from "@/types";
import { VisuallyHidden } from "@radix-ui/react-visually-hidden";

interface VehicleGalleryProps {
  images: VehicleImage[];
  title: string;
  className?: string;
}

export function VehicleGallery({
  images,
  title,
  className,
}: VehicleGalleryProps) {
  const [selectedIndex, setSelectedIndex] = React.useState(0);
  const [isLightboxOpen, setIsLightboxOpen] = React.useState(false);

  const selectedImage = images[selectedIndex];
  const hasMultipleImages = images.length > 1;

  const goToNext = React.useCallback(() => {
    setSelectedIndex((prev) => (prev + 1) % images.length);
  }, [images.length]);

  const goToPrev = React.useCallback(() => {
    setSelectedIndex((prev) => (prev - 1 + images.length) % images.length);
  }, [images.length]);

  // Keyboard navigation
  React.useEffect(() => {
    if (!isLightboxOpen) return;

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === "ArrowRight") goToNext();
      if (e.key === "ArrowLeft") goToPrev();
      if (e.key === "Escape") setIsLightboxOpen(false);
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [isLightboxOpen, goToNext, goToPrev]);

  if (images.length === 0) {
    return (
      <div className={cn("aspect-[16/10] bg-gray-100 rounded-xl flex items-center justify-center", className)}>
        <p className="text-gray-500">Sin imágenes disponibles</p>
      </div>
    );
  }

  return (
    <div className={cn("space-y-4", className)}>
      {/* Main Image */}
      <div className="relative aspect-[16/10] rounded-xl overflow-hidden bg-gray-100 group">
        <Image
          src={selectedImage.url}
          alt={selectedImage.alt || `${title} - Imagen ${selectedIndex + 1}`}
          fill
          sizes="(max-width: 768px) 100vw, 60vw"
          className="object-cover"
          priority
        />

        {/* Zoom Button */}
        <Button
          variant="secondary"
          size="icon"
          onClick={() => setIsLightboxOpen(true)}
          className="absolute top-4 right-4 opacity-0 group-hover:opacity-100 transition-opacity"
          aria-label="Ver imagen en pantalla completa"
        >
          <Expand size={18} />
        </Button>

        {/* Navigation Arrows */}
        {hasMultipleImages && (
          <>
            <Button
              variant="secondary"
              size="icon"
              onClick={goToPrev}
              className="absolute left-4 top-1/2 -translate-y-1/2 opacity-0 group-hover:opacity-100 transition-opacity"
              aria-label="Imagen anterior"
            >
              <ChevronLeft size={24} />
            </Button>
            <Button
              variant="secondary"
              size="icon"
              onClick={goToNext}
              className="absolute right-4 top-1/2 -translate-y-1/2 opacity-0 group-hover:opacity-100 transition-opacity"
              aria-label="Siguiente imagen"
            >
              <ChevronRight size={24} />
            </Button>
          </>
        )}

        {/* Image Counter */}
        {hasMultipleImages && (
          <div className="absolute bottom-4 left-1/2 -translate-x-1/2 bg-black/60 text-white px-3 py-1 rounded-full text-sm">
            {selectedIndex + 1} / {images.length}
          </div>
        )}
      </div>

      {/* Thumbnails */}
      {hasMultipleImages && (
        <div className="flex gap-2 overflow-x-auto pb-2 scrollbar-thin">
          {images.map((image, index) => (
            <button
              key={image.id}
              onClick={() => setSelectedIndex(index)}
              className={cn(
                "relative w-20 h-16 flex-shrink-0 rounded-lg overflow-hidden",
                "ring-2 ring-offset-2 transition-all",
                selectedIndex === index
                  ? "ring-primary-500"
                  : "ring-transparent hover:ring-gray-300"
              )}
              aria-label={`Ver imagen ${index + 1}`}
              aria-current={selectedIndex === index}
            >
              <Image
                src={image.thumbnailUrl || image.url}
                alt=""
                fill
                sizes="80px"
                className="object-cover"
              />
            </button>
          ))}
        </div>
      )}

      {/* Lightbox */}
      <Dialog open={isLightboxOpen} onOpenChange={setIsLightboxOpen}>
        <DialogContent className="max-w-[95vw] max-h-[95vh] p-0 bg-black/95">
          <VisuallyHidden>
            <DialogTitle>{title} - Galería de imágenes</DialogTitle>
          </VisuallyHidden>

          {/* Close Button */}
          <Button
            variant="ghost"
            size="icon"
            onClick={() => setIsLightboxOpen(false)}
            className="absolute top-4 right-4 z-50 text-white hover:bg-white/20"
            aria-label="Cerrar galería"
          >
            <X size={24} />
          </Button>

          {/* Main Image */}
          <div className="relative w-full h-[80vh] flex items-center justify-center">
            <Image
              src={selectedImage.url}
              alt={selectedImage.alt || title}
              fill
              sizes="95vw"
              className="object-contain"
            />
          </div>

          {/* Navigation */}
          {hasMultipleImages && (
            <>
              <Button
                variant="ghost"
                size="icon"
                onClick={goToPrev}
                className="absolute left-4 top-1/2 -translate-y-1/2 text-white hover:bg-white/20 h-12 w-12"
                aria-label="Imagen anterior"
              >
                <ChevronLeft size={32} />
              </Button>
              <Button
                variant="ghost"
                size="icon"
                onClick={goToNext}
                className="absolute right-4 top-1/2 -translate-y-1/2 text-white hover:bg-white/20 h-12 w-12"
                aria-label="Siguiente imagen"
              >
                <ChevronRight size={32} />
              </Button>
            </>
          )}

          {/* Thumbnail Strip */}
          {hasMultipleImages && (
            <div className="absolute bottom-4 left-1/2 -translate-x-1/2 flex gap-2 bg-black/60 p-2 rounded-lg">
              {images.map((image, index) => (
                <button
                  key={image.id}
                  onClick={() => setSelectedIndex(index)}
                  className={cn(
                    "relative w-16 h-12 rounded overflow-hidden",
                    "ring-2 transition-all",
                    selectedIndex === index
                      ? "ring-white"
                      : "ring-transparent opacity-60 hover:opacity-100"
                  )}
                >
                  <Image
                    src={image.thumbnailUrl || image.url}
                    alt=""
                    fill
                    sizes="64px"
                    className="object-cover"
                  />
                </button>
              ))}
            </div>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
```

---

## 🔧 PASO 4: VehicleSpecs

```typescript
// filepath: src/components/vehicles/VehicleSpecs.tsx
import {
  Gauge,
  Fuel,
  Cog,
  Calendar,
  Palette,
  Car,
  Users,
  DoorOpen,
  Zap,
  Shield,
  Hash,
} from "lucide-react";
import { formatNumber } from "@/lib/utils";
import type { Vehicle } from "@/types";

interface VehicleSpecsProps {
  vehicle: Vehicle;
  variant?: "grid" | "list";
  className?: string;
}

const FUEL_TYPE_LABELS: Record<string, string> = {
  gasoline: "Gasolina",
  diesel: "Diésel",
  electric: "Eléctrico",
  hybrid: "Híbrido",
  plugin_hybrid: "Híbrido Enchufable",
  lpg: "GLP",
};

const TRANSMISSION_LABELS: Record<string, string> = {
  automatic: "Automático",
  manual: "Manual",
  cvt: "CVT",
  dct: "Doble Embrague",
};

const DRIVETRAIN_LABELS: Record<string, string> = {
  fwd: "Tracción Delantera",
  rwd: "Tracción Trasera",
  awd: "Tracción Total (AWD)",
  "4wd": "4x4",
};

export function VehicleSpecs({
  vehicle,
  variant = "grid",
  className,
}: VehicleSpecsProps) {
  const specs = [
    {
      icon: Gauge,
      label: "Kilometraje",
      value: vehicle.mileage !== undefined
        ? `${formatNumber(vehicle.mileage)} km`
        : null,
    },
    {
      icon: Calendar,
      label: "Año",
      value: vehicle.year.toString(),
    },
    {
      icon: Fuel,
      label: "Combustible",
      value: vehicle.fuelType
        ? FUEL_TYPE_LABELS[vehicle.fuelType] || vehicle.fuelType
        : null,
    },
    {
      icon: Cog,
      label: "Transmisión",
      value: vehicle.transmission
        ? TRANSMISSION_LABELS[vehicle.transmission] || vehicle.transmission
        : null,
    },
    {
      icon: Car,
      label: "Tracción",
      value: vehicle.drivetrain
        ? DRIVETRAIN_LABELS[vehicle.drivetrain] || vehicle.drivetrain
        : null,
    },
    {
      icon: Zap,
      label: "Motor",
      value: vehicle.engineSize
        ? `${vehicle.engineSize}L ${vehicle.cylinders ? `${vehicle.cylinders} cil.` : ""}`
        : null,
    },
    {
      icon: Zap,
      label: "Potencia",
      value: vehicle.horsepower ? `${vehicle.horsepower} HP` : null,
    },
    {
      icon: Palette,
      label: "Color Exterior",
      value: vehicle.exteriorColor,
    },
    {
      icon: Palette,
      label: "Color Interior",
      value: vehicle.interiorColor,
    },
    {
      icon: DoorOpen,
      label: "Puertas",
      value: vehicle.doors?.toString(),
    },
    {
      icon: Users,
      label: "Asientos",
      value: vehicle.seats?.toString(),
    },
    {
      icon: Hash,
      label: "VIN",
      value: vehicle.vin,
    },
  ].filter((spec) => spec.value);

  if (variant === "list") {
    return (
      <div className={className}>
        <h3 className="text-lg font-semibold text-gray-900 mb-4">
          Especificaciones
        </h3>
        <dl className="divide-y divide-gray-200">
          {specs.map((spec) => (
            <div
              key={spec.label}
              className="py-3 flex items-center justify-between"
            >
              <dt className="flex items-center gap-2 text-gray-600">
                <spec.icon size={18} className="text-gray-400" />
                {spec.label}
              </dt>
              <dd className="font-medium text-gray-900">{spec.value}</dd>
            </div>
          ))}
        </dl>
      </div>
    );
  }

  return (
    <div className={className}>
      <h3 className="text-lg font-semibold text-gray-900 mb-4">
        Especificaciones
      </h3>
      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
        {specs.map((spec) => (
          <div
            key={spec.label}
            className="flex items-start gap-3 p-3 bg-gray-50 rounded-lg"
          >
            <spec.icon size={20} className="text-primary-600 flex-shrink-0 mt-0.5" />
            <div>
              <dt className="text-sm text-gray-500">{spec.label}</dt>
              <dd className="font-medium text-gray-900">{spec.value}</dd>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 5: VehicleFeatures

```typescript
// filepath: src/components/vehicles/VehicleFeatures.tsx
import { Check, Shield } from "lucide-react";
import { cn } from "@/lib/utils";

interface VehicleFeaturesProps {
  features: string[];
  safetyFeatures?: string[];
  className?: string;
}

export function VehicleFeatures({
  features,
  safetyFeatures = [],
  className,
}: VehicleFeaturesProps) {
  if (features.length === 0 && safetyFeatures.length === 0) {
    return null;
  }

  return (
    <div className={cn("space-y-6", className)}>
      {/* Comfort & Convenience Features */}
      {features.length > 0 && (
        <div>
          <h3 className="text-lg font-semibold text-gray-900 mb-4">
            Características
          </h3>
          <ul className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
            {features.map((feature) => (
              <li
                key={feature}
                className="flex items-center gap-2 text-gray-700"
              >
                <Check size={16} className="text-green-600 flex-shrink-0" />
                {feature}
              </li>
            ))}
          </ul>
        </div>
      )}

      {/* Safety Features */}
      {safetyFeatures.length > 0 && (
        <div>
          <h3 className="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
            <Shield size={20} className="text-blue-600" />
            Seguridad
          </h3>
          <ul className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
            {safetyFeatures.map((feature) => (
              <li
                key={feature}
                className="flex items-center gap-2 text-gray-700"
              >
                <Check size={16} className="text-blue-600 flex-shrink-0" />
                {feature}
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}
```

---

## 🔧 PASO 6: SimilarVehicles

```typescript
// filepath: src/components/vehicles/SimilarVehicles.tsx
"use client";

import { VehicleCard } from "./VehicleCard";
import { VehicleCardSkeleton } from "./VehicleCardSkeleton";
import { useSimilarVehicles } from "@/lib/hooks/useVehicles";
import { cn } from "@/lib/utils";

interface SimilarVehiclesProps {
  vehicleId: string;
  makeId?: string;
  priceRange?: number;
  limit?: number;
  className?: string;
}

export function SimilarVehicles({
  vehicleId,
  makeId,
  priceRange = 500000,
  limit = 4,
  className,
}: SimilarVehiclesProps) {
  const { data, isLoading } = useSimilarVehicles(vehicleId, {
    makeId,
    priceRange,
    limit,
  });

  if (isLoading) {
    return (
      <section className={className}>
        <h2 className="text-xl font-semibold text-gray-900 mb-6">
          Vehículos Similares
        </h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
          {Array.from({ length: limit }).map((_, i) => (
            <VehicleCardSkeleton key={i} />
          ))}
        </div>
      </section>
    );
  }

  if (!data?.data || data.data.length === 0) {
    return null;
  }

  return (
    <section className={className}>
      <h2 className="text-xl font-semibold text-gray-900 mb-6">
        Vehículos Similares
      </h2>
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
        {data.data.map((vehicle) => (
          <VehicleCard key={vehicle.id} vehicle={vehicle} />
        ))}
      </div>
    </section>
  );
}
```

---

## 🔧 PASO 7: RecentlyViewed

```typescript
// filepath: src/components/vehicles/RecentlyViewed.tsx
"use client";

import * as React from "react";
import { VehicleCard } from "./VehicleCard";
import type { VehicleSummary } from "@/types";

const STORAGE_KEY = "okla_recently_viewed";
const MAX_ITEMS = 10;

export function useRecentlyViewed() {
  const [vehicles, setVehicles] = React.useState<VehicleSummary[]>([]);

  // Load from localStorage on mount
  React.useEffect(() => {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored) {
      try {
        setVehicles(JSON.parse(stored));
      } catch {
        localStorage.removeItem(STORAGE_KEY);
      }
    }
  }, []);

  const addVehicle = React.useCallback((vehicle: VehicleSummary) => {
    setVehicles((prev) => {
      // Remove if already exists
      const filtered = prev.filter((v) => v.id !== vehicle.id);
      // Add to beginning
      const updated = [vehicle, ...filtered].slice(0, MAX_ITEMS);
      // Save to localStorage
      localStorage.setItem(STORAGE_KEY, JSON.stringify(updated));
      return updated;
    });
  }, []);

  const clearAll = React.useCallback(() => {
    setVehicles([]);
    localStorage.removeItem(STORAGE_KEY);
  }, []);

  return { vehicles, addVehicle, clearAll };
}

interface RecentlyViewedProps {
  vehicles: VehicleSummary[];
  onClear?: () => void;
  className?: string;
}

export function RecentlyViewed({
  vehicles,
  onClear,
  className,
}: RecentlyViewedProps) {
  if (vehicles.length === 0) {
    return null;
  }

  return (
    <section className={className}>
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-xl font-semibold text-gray-900">
          Vistos Recientemente
        </h2>
        {onClear && (
          <button
            onClick={onClear}
            className="text-sm text-gray-500 hover:text-gray-700"
          >
            Limpiar historial
          </button>
        )}
      </div>
      <div className="flex gap-4 overflow-x-auto pb-4 scrollbar-thin">
        {vehicles.map((vehicle) => (
          <div key={vehicle.id} className="flex-shrink-0 w-72">
            <VehicleCard vehicle={vehicle} />
          </div>
        ))}
      </div>
    </section>
  );
}
```

---

## ✅ VALIDACIÓN

### Tests

```typescript
// filepath: __tests__/components/vehicles/VehicleCard.test.tsx
import { describe, it, expect, vi } from "vitest";
import { screen } from "@testing-library/react";
import { render } from "@tests/utils/test-utils";
import { VehicleCard } from "@/components/vehicles/VehicleCard";
import { createVehicleSummary } from "@tests/utils/factories";

describe("VehicleCard", () => {
  const vehicle = createVehicleSummary({
    title: "Toyota Camry SE 2024",
    price: 1850000,
    year: 2024,
    make: "Toyota",
    model: "Camry",
  });

  it("renders vehicle info correctly", () => {
    render(<VehicleCard vehicle={vehicle} />);

    expect(screen.getByText("2024 Toyota Camry")).toBeInTheDocument();
    expect(screen.getByText(/1,850,000/)).toBeInTheDocument();
  });

  it("shows verified badge for dealers", () => {
    render(
      <VehicleCard
        vehicle={{ ...vehicle, sellerType: "dealer", isVerified: true }}
      />
    );

    expect(screen.getByText("Verificado")).toBeInTheDocument();
  });

  it("calls onFavoriteClick when favorite is clicked", async () => {
    const handleFavorite = vi.fn();
    const { user } = render(
      <VehicleCard vehicle={vehicle} onFavoriteClick={handleFavorite} />
    );

    await user.click(screen.getByRole("button", { name: /favoritos/i }));

    expect(handleFavorite).toHaveBeenCalledWith(vehicle.id);
  });
});
```

### Ejecutar

```bash
pnpm test components/vehicles
```

---

## 📊 RESUMEN

| Componente          | Archivo                            | Función                |
| ------------------- | ---------------------------------- | ---------------------- |
| VehicleCard         | `vehicles/VehicleCard.tsx`         | Card con 3 variantes   |
| VehicleCardSkeleton | `vehicles/VehicleCardSkeleton.tsx` | Loading state          |
| VehicleGrid         | `vehicles/VehicleGrid.tsx`         | Grid responsive        |
| VehicleGallery      | `vehicles/VehicleGallery.tsx`      | Galería + Lightbox     |
| VehicleSpecs        | `vehicles/VehicleSpecs.tsx`        | Especificaciones       |
| VehicleFeatures     | `vehicles/VehicleFeatures.tsx`     | Lista de features      |
| SimilarVehicles     | `vehicles/SimilarVehicles.tsx`     | Vehículos relacionados |
| RecentlyViewed      | `vehicles/RecentlyViewed.tsx`      | Historial local        |

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/04-PAGINAS/01-home.md`
