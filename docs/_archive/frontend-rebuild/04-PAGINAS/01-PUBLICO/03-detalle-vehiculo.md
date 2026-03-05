---
title: "Página de Detalle de Vehículo"
priority: P0
estimated_time: "90 minutos"
dependencies: []
apis: []
status: complete
last_updated: "2026-01-30"
---

# 🚗 Página de Detalle de Vehículo

> **Tiempo estimado:** 90 minutos
> **Prerrequisitos:** VehicleGallery, VehicleSpecs, VehicleFeatures, API hooks

---

## ✅ INTEGRACIÓN CON SERVICIOS DE CONFIANZA

Este documento complementa:

- [process-matrix/15-CONFIANZA-SEGURIDAD/02-garantia-inspeccion.md](../../process-matrix/15-CONFIANZA-SEGURIDAD/02-garantia-inspeccion.md) - **Garantía e Inspección** ⭐
- [process-matrix/15-CONFIANZA-SEGURIDAD/03-devolucion-cancelacion.md](../../process-matrix/15-CONFIANZA-SEGURIDAD/03-devolucion-cancelacion.md) - **Devoluciones** ⭐
- [process-matrix/15-CONFIANZA-SEGURIDAD/04-disputas-mediacion.md](../../process-matrix/15-CONFIANZA-SEGURIDAD/04-disputas-mediacion.md) - **Disputas** ⭐
- [process-matrix/15-CONFIANZA-SEGURIDAD/05-okla-certified.md](../../process-matrix/15-CONFIANZA-SEGURIDAD/05-okla-certified.md) - **OKLA Certified** ⭐

**Estado:** ✅ WarrantyService 100% BE | 🔴 ReturnService 0% | ✅ DisputeService 80% BE | 🔴 CertificationService 0%

### Servicios de Confianza

| Servicio             | Puerto | Función                    | Estado              |
| -------------------- | ------ | -------------------------- | ------------------- |
| WarrantyService      | 5083   | Garantías e inspección     | ✅ 100% BE + 40% UI |
| DisputeService       | 5089   | Resolución de disputas     | ✅ 80% BE + 0% UI   |
| TrustService         | 5082   | Devoluciones (planificado) | 🔴 0%               |
| CertificationService | 5092   | OKLA Certified             | 🔴 0% (Fase 2)      |

### Badges de Confianza a Mostrar

```tsx
// En VehicleDetailPage, mostrar badges si aplica:
{
  vehicle.hasCertification && (
    <Badge variant="success">
      <ShieldCheck className="w-4 h-4" />
      OKLA Certified
    </Badge>
  );
}
{
  vehicle.hasWarranty && (
    <Badge variant="blue">
      <Shield className="w-4 h-4" />
      Garantía 7 días
    </Badge>
  );
}
{
  vehicle.inspectionScore >= 85 && (
    <Badge variant="purple">
      <CheckCircle className="w-4 h-4" />
      Inspección Certificada {vehicle.inspectionScore}/100
    </Badge>
  );
}
```

### Endpoints Relacionados

| Método | Endpoint                              | Descripción                       | Servicio        |
| ------ | ------------------------------------- | --------------------------------- | --------------- |
| `POST` | `/api/warranty/request-inspection`    | Solicitar inspección del vehículo | WarrantyService |
| `GET`  | `/api/warranty/certified/{vehicleId}` | Ver certificación OKLA            | WarrantyService |
| `POST` | `/api/warranty/claims`                | Reclamar garantía                 | WarrantyService |
| `POST` | `/api/disputes`                       | Abrir disputa sobre vehículo      | DisputeService  |
| `POST` | `/api/trust/returns`                  | Solicitar devolución (7 días)     | TrustService    |

---

## 📋 OBJETIVO

Implementar la página de detalle completa:

- Galería de imágenes con lightbox
- Información del vehículo
- **Badges de confianza (Certificado, Garantía, Inspección)** ⭐
- Especificaciones técnicas
- Características
- Información del vendedor
- **Botones de acción (Inspección, Disputa)** ⭐
- Formulario de contacto
- Vehículos similares
- SEO optimizado

---

## 🎨 ESTRUCTURA DE LA PÁGINA

```
┌─────────────────────────────────────────────────────┐
│ HEADER                                              │
├─────────────────────────────────────────────────────┤
│ BREADCRUMB: Inicio > Vehículos > Toyota Camry 2024  │
├─────────────────────────────────────────────────────┤
│ ┌─────────────────────┬─────────────────────────┐   │
│ │                     │ TÍTULO Y PRECIO         │   │
│ │                     │ 2024 Toyota Camry SE    │   │
│ │    GALLERY          │ RD$ 1,850,000           │   │
│ │    [Main Image]     │                         │   │
│ │                     │ [❤️ Favorito] [📤 Share]│   │
│ │    [thumb][thumb]   │                         │   │
│ │                     │ ─────────────────────── │   │
│ └─────────────────────┤ SELLER INFO             │   │
│                       │ 👤 AutoMax RD           │   │
│                       │ ⭐ 4.8 (120 reviews)    │   │
│                       │ ✓ Verificado            │   │
│                       │                         │   │
│                       │ [📞 Llamar] [💬 WhatsApp]│   │
│                       │ [✉️ Enviar mensaje]     │   │
│                       └─────────────────────────┘   │
├─────────────────────────────────────────────────────┤
│ TABS: [Descripción] [Especificaciones] [Features]   │
├─────────────────────────────────────────────────────┤
│ CONTENIDO DEL TAB SELECCIONADO                      │
│ ...                                                 │
├─────────────────────────────────────────────────────┤
│ VEHÍCULOS SIMILARES                                 │
│ [Card] [Card] [Card] [Card]                         │
├─────────────────────────────────────────────────────┤
│ FOOTER                                              │
└─────────────────────────────────────────────────────┘
```

---

## 🔧 PASO 1: Página Principal

```typescript
// filepath: src/app/(main)/vehiculos/[slug]/page.tsx
import { Metadata } from "next";
import { notFound } from "next/navigation";
import { Suspense } from "react";
import { VehicleGallery } from "@/components/vehicles/VehicleGallery";
import { VehicleHeader } from "@/components/vehicle-detail/VehicleHeader";
import { VehicleTabs } from "@/components/vehicle-detail/VehicleTabs";
import { SellerCard } from "@/components/vehicle-detail/SellerCard";
import { SimilarVehicles } from "@/components/vehicles/SimilarVehicles";
import { ContactModal } from "@/components/vehicle-detail/ContactModal";
import { Breadcrumbs } from "@/components/ui/Breadcrumbs";
import { VehicleCardSkeleton } from "@/components/vehicles/VehicleCardSkeleton";
import { vehicleService } from "@/lib/services/vehicleService";
import { formatPrice } from "@/lib/utils";

interface VehiclePageProps {
  params: Promise<{ slug: string }>;
}

// Generate metadata for SEO
export async function generateMetadata({
  params,
}: VehiclePageProps): Promise<Metadata> {
  const { slug } = await params;

  try {
    const vehicle = await vehicleService.getBySlug(slug);

    const title = `${vehicle.year} ${vehicle.make} ${vehicle.model} ${vehicle.trim || ""} | OKLA`;
    const description = `Compra ${vehicle.year} ${vehicle.make} ${vehicle.model} en ${vehicle.city}. ${formatPrice(vehicle.price)}. ${vehicle.mileage?.toLocaleString() || 0} km. ${vehicle.condition === "new" ? "Nuevo" : "Usado"}.`;

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
      title: "Vehículo no encontrado | OKLA",
    };
  }
}

export default async function VehiclePage({ params }: VehiclePageProps) {
  const { slug } = await params;

  let vehicle;
  try {
    vehicle = await vehicleService.getBySlug(slug);
  } catch {
    notFound();
  }

  const title = `${vehicle.year} ${vehicle.make} ${vehicle.model}`;

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Breadcrumbs */}
      <div className="bg-white border-b">
        <div className="container py-3">
          <Breadcrumbs
            items={[
              { label: "Inicio", href: "/" },
              { label: "Vehículos", href: "/vehiculos" },
              { label: title },
            ]}
          />
        </div>
      </div>

      {/* Main content */}
      <div className="container py-6 lg:py-8">
        <div className="grid lg:grid-cols-3 gap-8">
          {/* Left column - Gallery and Details */}
          <div className="lg:col-span-2 space-y-6">
            {/* Gallery */}
            <VehicleGallery
              images={vehicle.images ?? []}
              title={title}
              className="bg-white rounded-xl p-4"
            />

            {/* Mobile: Header (hidden on desktop) */}
            <div className="lg:hidden">
              <VehicleHeader vehicle={vehicle} />
            </div>

            {/* Mobile: Seller Card */}
            <div className="lg:hidden">
              <SellerCard vehicle={vehicle} />
            </div>

            {/* Tabs - Description, Specs, Features */}
            <VehicleTabs vehicle={vehicle} />
          </div>

          {/* Right column - Sticky sidebar */}
          <div className="hidden lg:block">
            <div className="sticky top-24 space-y-6">
              {/* Header with price */}
              <VehicleHeader vehicle={vehicle} />

              {/* Seller info */}
              <SellerCard vehicle={vehicle} />
            </div>
          </div>
        </div>

        {/* Similar vehicles */}
        <div className="mt-12">
          <Suspense
            fallback={
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
                {Array.from({ length: 4 }).map((_, i) => (
                  <VehicleCardSkeleton key={i} />
                ))}
              </div>
            }
          >
            <SimilarVehicles
              vehicleId={vehicle.id}
              makeId={vehicle.makeId}
              priceRange={500000}
              limit={4}
            />
          </Suspense>
        </div>
      </div>

      {/* Contact Modal (global) */}
      <ContactModal vehicle={vehicle} />
    </div>
  );
}
```

---

## 🔧 PASO 2: VehicleHeader

```typescript
// filepath: src/components/vehicle-detail/VehicleHeader.tsx
"use client";

import * as React from "react";
import { Heart, Share2, MapPin, Calendar, Gauge, Check, Shield } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { PriceDisplay } from "@/components/ui/PriceDisplay";
import { useFavorites } from "@/lib/hooks/useFavorites";
import { showToast } from "@/lib/toast";
import { cn, formatNumber, formatRelativeDate } from "@/lib/utils";
import type { Vehicle } from "@/types";

interface VehicleHeaderProps {
  vehicle: Vehicle;
  className?: string;
}

export function VehicleHeader({ vehicle, className }: VehicleHeaderProps) {
  const { isFavorite, toggleFavorite } = useFavorites();
  const isFav = isFavorite(vehicle.id);

  const title = `${vehicle.year} ${vehicle.make} ${vehicle.model}`;

  const handleShare = async () => {
    const url = window.location.href;
    const text = `${title} - ${formatNumber(vehicle.price)} RD$`;

    if (navigator.share) {
      try {
        await navigator.share({ title, text, url });
      } catch {
        // User cancelled or error
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

  return (
    <div className={cn("bg-white rounded-xl p-6", className)}>
      {/* Badges */}
      <div className="flex flex-wrap gap-2 mb-3">
        {vehicle.condition === "new" && (
          <Badge variant="new">Nuevo</Badge>
        )}
        {vehicle.condition === "used" && (
          <Badge variant="used">Usado</Badge>
        )}
        {vehicle.isCertified && (
          <Badge variant="certified" className="gap-1">
            <Shield size={12} />
            Certificado
          </Badge>
        )}
        {vehicle.sellerType === "dealer" && vehicle.isVerified && (
          <Badge variant="dealer" className="gap-1">
            <Check size={12} />
            Dealer Verificado
          </Badge>
        )}
        {vehicle.isFeatured && (
          <Badge variant="featured">Destacado</Badge>
        )}
      </div>

      {/* Title */}
      <h1 className="text-2xl lg:text-3xl font-bold text-gray-900">
        {title}
      </h1>

      {/* Trim */}
      {vehicle.trim && (
        <p className="text-lg text-gray-600 mt-1">{vehicle.trim}</p>
      )}

      {/* Quick specs */}
      <div className="flex flex-wrap items-center gap-4 mt-4 text-sm text-gray-600">
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

      {/* Price */}
      <div className="mt-6 pt-6 border-t" data-testid="vehicle-price">
        <PriceDisplay
          price={vehicle.price}
          originalPrice={vehicle.originalPrice}
          size="xl"
        />
        {vehicle.isNegotiable && (
          <p className="text-sm text-gray-500 mt-1">Precio negociable</p>
        )}
      </div>

      {/* Actions */}
      <div className="flex gap-3 mt-6">
        <Button
          variant={isFav ? "default" : "outline"}
          onClick={handleFavorite}
          className="flex-1"
        >
          <Heart
            size={18}
            className={cn(
              isFav && "fill-current"
            )}
          />
          {isFav ? "Guardado" : "Guardar"}
        </Button>

        <Button variant="outline" onClick={handleShare}>
          <Share2 size={18} />
          Compartir
        </Button>
      </div>

      {/* Published date */}
      <p className="text-xs text-gray-400 mt-4 text-center">
        Publicado {formatRelativeDate(vehicle.createdAt)}
      </p>
    </div>
  );
}
```

---

## 🔧 PASO 3: SellerCard

```typescript
// filepath: src/components/vehicle-detail/SellerCard.tsx
"use client";

import * as React from "react";
import Image from "next/image";
import Link from "next/link";
import { Phone, MessageCircle, Mail, MapPin, Star, Check, Clock, Shield } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { useContactModal } from "@/lib/hooks/useContactModal";
import { cn, formatPhoneNumber } from "@/lib/utils";
import type { Vehicle } from "@/types";

interface SellerCardProps {
  vehicle: Vehicle;
  className?: string;
}

export function SellerCard({ vehicle, className }: SellerCardProps) {
  const { openModal } = useContactModal();
  const seller = vehicle.seller;
  const isDealer = vehicle.sellerType === "dealer";

  const handleCall = () => {
    window.location.href = `tel:${seller?.phone}`;
  };

  const handleWhatsApp = () => {
    const message = encodeURIComponent(
      `Hola, me interesa el ${vehicle.year} ${vehicle.make} ${vehicle.model} que vi en OKLA.`
    );
    const phone = seller?.phone?.replace(/\D/g, "");
    window.open(`https://wa.me/1${phone}?text=${message}`, "_blank");
  };

  const handleContact = () => {
    openModal(vehicle);
  };

  return (
    <div
      className={cn("bg-white rounded-xl p-6", className)}
      data-testid="seller-info"
    >
      {/* Seller header */}
      <div className="flex items-start gap-4">
        {/* Avatar */}
        <div className="relative w-14 h-14 rounded-full overflow-hidden bg-gray-100 flex-shrink-0">
          {seller?.avatar ? (
            <Image
              src={seller.avatar}
              alt={seller.name || "Vendedor"}
              fill
              sizes="56px"
              className="object-cover"
            />
          ) : (
            <div className="w-full h-full flex items-center justify-center text-gray-400 text-xl font-semibold">
              {seller?.name?.[0]?.toUpperCase() || "V"}
            </div>
          )}

          {/* Verified badge */}
          {vehicle.isVerified && (
            <div className="absolute -bottom-1 -right-1 bg-primary-600 rounded-full p-1">
              <Check size={10} className="text-white" />
            </div>
          )}
        </div>

        {/* Info */}
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2">
            {isDealer ? (
              <Link
                href={`/dealers/${seller?.slug}`}
                className="font-semibold text-gray-900 hover:text-primary-600 transition-colors"
              >
                {seller?.name}
              </Link>
            ) : (
              <span className="font-semibold text-gray-900">
                {seller?.name}
              </span>
            )}
          </div>

          {/* Badges */}
          <div className="flex flex-wrap gap-2 mt-1">
            {vehicle.isVerified && (
              <Badge variant="success" size="sm">
                <Check size={10} className="mr-1" />
                Verificado
              </Badge>
            )}
            {isDealer && (
              <Badge variant="primary" size="sm">Dealer</Badge>
            )}
          </div>

          {/* Rating for dealers */}
          {isDealer && seller?.rating && (
            <div className="flex items-center gap-1 mt-2 text-sm">
              <Star size={14} className="fill-yellow-400 text-yellow-400" />
              <span className="font-medium">{seller.rating.toFixed(1)}</span>
              <span className="text-gray-500">
                ({seller.reviewCount} opiniones)
              </span>
            </div>
          )}

          {/* Location */}
          <div className="flex items-center gap-1 mt-2 text-sm text-gray-600">
            <MapPin size={14} className="text-gray-400" />
            <span>{vehicle.city}</span>
          </div>

          {/* Response time for dealers */}
          {isDealer && seller?.responseTime && (
            <div className="flex items-center gap-1 mt-1 text-sm text-gray-500">
              <Clock size={14} className="text-gray-400" />
              <span>Responde en {seller.responseTime}</span>
            </div>
          )}
        </div>
      </div>

      {/* CTA Buttons */}
      <div className="space-y-3 mt-6">
        {seller?.phone && (
          <>
            <Button
              variant="default"
              size="lg"
              className="w-full"
              onClick={handleCall}
            >
              <Phone size={18} />
              Llamar
              <span className="ml-auto text-sm opacity-80">
                {formatPhoneNumber(seller.phone)}
              </span>
            </Button>

            <Button
              variant="success"
              size="lg"
              className="w-full bg-green-600 hover:bg-green-700"
              onClick={handleWhatsApp}
            >
              <MessageCircle size={18} />
              WhatsApp
            </Button>
          </>
        )}

        <Button
          variant="outline"
          size="lg"
          className="w-full"
          onClick={handleContact}
        >
          <Mail size={18} />
          Enviar mensaje
        </Button>
      </div>

      {/* Trust indicators */}
      <div className="mt-6 pt-6 border-t">
        <div className="flex items-center gap-2 text-sm text-gray-600">
          <Shield size={16} className="text-green-600" />
          <span>Transacción segura con OKLA</span>
        </div>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 4: VehicleTabs

```typescript
// filepath: src/components/vehicle-detail/VehicleTabs.tsx
"use client";

import * as React from "react";
import Link from "next/link";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/Tabs";
import { VehicleSpecs } from "@/components/vehicles/VehicleSpecs";
import { VehicleFeatures } from "@/components/vehicles/VehicleFeatures";
import { Vehicle360Viewer } from "@/components/vehicles/Vehicle360Viewer";
import { Interior360Viewer } from "@/components/vehicles/Interior360Viewer";
import { useVehicle360 } from "@/lib/hooks/useVehicle360";
import { RotateCcw, Maximize2, Car, Sofa } from "lucide-react";
import { cn } from "@/lib/utils";
import type { Vehicle } from "@/types";

interface VehicleTabsProps {
  vehicle: Vehicle;
  className?: string;
}

export function VehicleTabs({ vehicle, className }: VehicleTabsProps) {
  // Verificar si el vehículo tiene vista 360° disponible
  const { view: view360, isLoading: is360Loading } = useVehicle360(vehicle.id, {
    autoFetch: true
  });
  const has360View = view360?.isReady;

  return (
    <div className={cn("bg-white rounded-xl", className)}>
      <Tabs defaultValue="description" className="w-full">
        <TabsList className="w-full justify-start rounded-none border-b bg-transparent p-0">
          <TabsTrigger
            value="description"
            className="rounded-none border-b-2 border-transparent data-[state=active]:border-primary-600 data-[state=active]:bg-transparent px-6 py-4"
          >
            Descripción
          </TabsTrigger>
          <TabsTrigger
            value="specs"
            className="rounded-none border-b-2 border-transparent data-[state=active]:border-primary-600 data-[state=active]:bg-transparent px-6 py-4"
          >
            Especificaciones
          </TabsTrigger>
          <TabsTrigger
            value="features"
            className="rounded-none border-b-2 border-transparent data-[state=active]:border-primary-600 data-[state=active]:bg-transparent px-6 py-4"
          >
            Características
          </TabsTrigger>
          {/* Tab Vista 360° - Solo visible si el vehículo tiene vista disponible */}
          {(has360View || is360Loading) && (
            <TabsTrigger
              value="360"
              className="rounded-none border-b-2 border-transparent data-[state=active]:border-primary-600 data-[state=active]:bg-transparent px-6 py-4 gap-2"
            >
              <RotateCcw size={16} className="animate-spin-slow" />
              Vista 360°
            </TabsTrigger>
          )}
        </TabsList>

        <TabsContent value="description" className="p-6">
          <VehicleDescription vehicle={vehicle} />
        </TabsContent>

        <TabsContent value="specs" className="p-6">
          <VehicleSpecs vehicle={vehicle} variant="grid" />
        </TabsContent>

        <TabsContent value="features" className="p-6">
          <VehicleFeatures
            features={vehicle.features ?? []}
            safetyFeatures={vehicle.safetyFeatures ?? []}
          />
        </TabsContent>

        {/* Contenido del Tab Vista 360° */}
        {has360View && (
          <TabsContent value="360" className="p-6">
            <Vehicle360TabContent vehicleId={vehicle.id} vehicleSlug={vehicle.slug} />
          </TabsContent>
        )}
      </Tabs>
    </div>
  );
}

// Componente para el contenido del Tab 360°
function Vehicle360TabContent({
  vehicleId,
  vehicleSlug
}: {
  vehicleId: string;
  vehicleSlug: string;
}) {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-lg font-semibold text-gray-900">
            Vista 360° Interactiva
          </h3>
          <p className="text-sm text-gray-500 mt-1">
            Arrastra para rotar el vehículo y verlo desde todos los ángulos
          </p>
        </div>
        <Link
          href={`/vehiculos/${vehicleSlug}/360`}
          className="inline-flex items-center gap-2 text-primary-600 hover:text-primary-700 font-medium text-sm"
        >
          <Maximize2 size={16} />
          Pantalla completa
        </Link>
      </div>

      {/* Visor 360° Exterior */}
      <div>
        <h4 className="font-medium text-gray-700 mb-3 flex items-center gap-2">
          <Car size={18} />
          Exterior
        </h4>
        <Vehicle360Viewer
          vehicleId={vehicleId}
          height={400}
          showThumbnails={true}
          showControls={true}
        />
      </div>

      {/* Visor Interior (si disponible) */}
      <div>
        <h4 className="font-medium text-gray-700 mb-3 flex items-center gap-2">
          <Sofa size={18} />
          Interior
        </h4>
        <Interior360Viewer vehicleId={vehicleId} />
      </div>
    </div>
  );
}

// Description sub-component
function VehicleDescription({ vehicle }: { vehicle: Vehicle }) {
  const [isExpanded, setIsExpanded] = React.useState(false);
  const description = vehicle.description ?? "";
  const isLong = description.length > 500;

  return (
    <div>
      <h3 className="text-lg font-semibold text-gray-900 mb-4">
        Descripción del Vehículo
      </h3>

      {description ? (
        <div className="prose prose-gray max-w-none">
          <p
            className={cn(
              "text-gray-600 whitespace-pre-line",
              !isExpanded && isLong && "line-clamp-6"
            )}
          >
            {description}
          </p>

          {isLong && (
            <button
              onClick={() => setIsExpanded(!isExpanded)}
              className="text-primary-600 hover:text-primary-700 font-medium mt-2"
            >
              {isExpanded ? "Ver menos" : "Ver más"}
            </button>
          )}
        </div>
      ) : (
        <p className="text-gray-500 italic">
          El vendedor no proporcionó una descripción.
        </p>
      )}

      {/* Additional info */}
      <div className="mt-6 pt-6 border-t">
        <h4 className="font-medium text-gray-900 mb-3">
          Información Adicional
        </h4>
        <dl className="grid grid-cols-2 gap-4 text-sm">
          <div>
            <dt className="text-gray-500">Tipo de vendedor</dt>
            <dd className="font-medium text-gray-900">
              {vehicle.sellerType === "dealer" ? "Dealer" : "Particular"}
            </dd>
          </div>
          <div>
            <dt className="text-gray-500">Condición</dt>
            <dd className="font-medium text-gray-900">
              {vehicle.condition === "new" ? "Nuevo" : "Usado"}
            </dd>
          </div>
          {vehicle.vin && (
            <div className="col-span-2">
              <dt className="text-gray-500">VIN</dt>
              <dd className="font-medium text-gray-900 font-mono">
                {vehicle.vin}
              </dd>
            </div>
          )}
        </dl>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 5: ContactModal

```typescript
// filepath: src/components/vehicle-detail/ContactModal.tsx
"use client";

import * as React from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Textarea } from "@/components/ui/Textarea";
import { FormField } from "@/components/ui/FormField";
import { useContactModal } from "@/lib/hooks/useContactModal";
import { contactService } from "@/lib/services/contactService";
import { showToast } from "@/lib/toast";

const contactSchema = z.object({
  name: z.string().min(2, "El nombre es requerido"),
  email: z.string().email("Email inválido"),
  phone: z.string().min(10, "Teléfono inválido"),
  message: z.string().min(10, "El mensaje debe tener al menos 10 caracteres"),
});

type ContactFormData = z.infer<typeof contactSchema>;

export function ContactModal() {
  const { isOpen, closeModal, vehicle } = useContactModal();

  const form = useForm<ContactFormData>({
    resolver: zodResolver(contactSchema),
    defaultValues: {
      name: "",
      email: "",
      phone: "",
      message: vehicle
        ? `Hola, me interesa el ${vehicle.year} ${vehicle.make} ${vehicle.model}. ¿Está disponible?`
        : "",
    },
  });

  // Update default message when vehicle changes
  React.useEffect(() => {
    if (vehicle) {
      form.setValue(
        "message",
        `Hola, me interesa el ${vehicle.year} ${vehicle.make} ${vehicle.model}. ¿Está disponible?`
      );
    }
  }, [vehicle, form]);

  const onSubmit = async (data: ContactFormData) => {
    if (!vehicle) return;

    try {
      await contactService.sendMessage({
        vehicleId: vehicle.id,
        sellerId: vehicle.seller?.id ?? "",
        ...data,
      });

      showToast.success(
        "Mensaje enviado",
        "El vendedor recibirá tu mensaje y te contactará pronto"
      );

      closeModal();
      form.reset();
    } catch (error) {
      showToast.error(
        "Error al enviar",
        "No pudimos enviar tu mensaje. Intenta de nuevo."
      );
    }
  };

  if (!vehicle) return null;

  const title = `${vehicle.year} ${vehicle.make} ${vehicle.model}`;

  return (
    <Dialog open={isOpen} onOpenChange={closeModal}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Contactar al vendedor</DialogTitle>
        </DialogHeader>

        <div className="mb-4 p-3 bg-gray-50 rounded-lg">
          <p className="font-medium text-gray-900">{title}</p>
          <p className="text-sm text-gray-600">
            RD$ {vehicle.price.toLocaleString()}
          </p>
        </div>

        <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
          <FormField
            label="Nombre"
            error={form.formState.errors.name?.message}
          >
            <Input
              placeholder="Tu nombre completo"
              {...form.register("name")}
            />
          </FormField>

          <FormField
            label="Email"
            error={form.formState.errors.email?.message}
          >
            <Input
              type="email"
              placeholder="tu@email.com"
              {...form.register("email")}
            />
          </FormField>

          <FormField
            label="Teléfono"
            error={form.formState.errors.phone?.message}
          >
            <Input
              type="tel"
              placeholder="809-555-1234"
              {...form.register("phone")}
            />
          </FormField>

          <FormField
            label="Mensaje"
            error={form.formState.errors.message?.message}
          >
            <Textarea
              placeholder="Escribe tu mensaje..."
              rows={4}
              {...form.register("message")}
            />
          </FormField>

          <div className="flex gap-3 pt-4">
            <Button
              type="button"
              variant="outline"
              onClick={closeModal}
              className="flex-1"
            >
              Cancelar
            </Button>
            <Button
              type="submit"
              className="flex-1"
              isLoading={form.formState.isSubmitting}
            >
              Enviar mensaje
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
```

---

## 🔧 PASO 6: useContactModal Hook

```typescript
// filepath: src/lib/hooks/useContactModal.ts
import { create } from "zustand";
import type { Vehicle } from "@/types";

interface ContactModalState {
  isOpen: boolean;
  vehicle: Vehicle | null;
  openModal: (vehicle: Vehicle) => void;
  closeModal: () => void;
}

export const useContactModal = create<ContactModalState>((set) => ({
  isOpen: false,
  vehicle: null,
  openModal: (vehicle) => set({ isOpen: true, vehicle }),
  closeModal: () => set({ isOpen: false, vehicle: null }),
}));
```

---

## 🔧 PASO 7: Not Found Page

```typescript
// filepath: src/app/(main)/vehiculos/[slug]/not-found.tsx
import Link from "next/link";
import { Search, ArrowLeft } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { EmptyState } from "@/components/ui/EmptyState";

export default function VehicleNotFound() {
  return (
    <div className="container py-16">
      <EmptyState
        variant="notFound"
        title="Vehículo no encontrado"
        description="Este vehículo ya no está disponible o el enlace es incorrecto"
        className="max-w-md mx-auto"
      >
        <div className="flex flex-col sm:flex-row gap-3 mt-6">
          <Button asChild variant="outline">
            <Link href="/vehiculos">
              <ArrowLeft size={16} className="mr-2" />
              Volver a búsqueda
            </Link>
          </Button>
          <Button asChild>
            <Link href="/">
              <Search size={16} className="mr-2" />
              Buscar vehículos
            </Link>
          </Button>
        </div>
      </EmptyState>
    </div>
  );
}
```

---

## 🔧 PASO 8: Loading State

```typescript
// filepath: src/app/(main)/vehiculos/[slug]/loading.tsx
export default function VehicleLoading() {
  return (
    <div className="min-h-screen bg-gray-50">
      {/* Breadcrumb skeleton */}
      <div className="bg-white border-b">
        <div className="container py-3">
          <div className="h-4 w-64 bg-gray-200 rounded animate-pulse" />
        </div>
      </div>

      <div className="container py-6 lg:py-8">
        <div className="grid lg:grid-cols-3 gap-8">
          {/* Gallery skeleton */}
          <div className="lg:col-span-2 space-y-6">
            <div className="bg-white rounded-xl p-4">
              <div className="aspect-[16/10] bg-gray-200 rounded-lg animate-pulse" />
              <div className="flex gap-2 mt-4">
                {Array.from({ length: 5 }).map((_, i) => (
                  <div
                    key={i}
                    className="w-20 h-16 bg-gray-200 rounded animate-pulse"
                  />
                ))}
              </div>
            </div>

            {/* Tabs skeleton */}
            <div className="bg-white rounded-xl p-6">
              <div className="flex gap-4 border-b pb-4 mb-4">
                <div className="h-4 w-24 bg-gray-200 rounded animate-pulse" />
                <div className="h-4 w-32 bg-gray-200 rounded animate-pulse" />
                <div className="h-4 w-28 bg-gray-200 rounded animate-pulse" />
              </div>
              <div className="space-y-3">
                <div className="h-4 w-full bg-gray-200 rounded animate-pulse" />
                <div className="h-4 w-5/6 bg-gray-200 rounded animate-pulse" />
                <div className="h-4 w-4/6 bg-gray-200 rounded animate-pulse" />
              </div>
            </div>
          </div>

          {/* Sidebar skeleton */}
          <div className="hidden lg:block space-y-6">
            <div className="bg-white rounded-xl p-6 space-y-4">
              <div className="h-6 w-48 bg-gray-200 rounded animate-pulse" />
              <div className="h-8 w-32 bg-gray-200 rounded animate-pulse" />
              <div className="flex gap-3">
                <div className="h-10 flex-1 bg-gray-200 rounded animate-pulse" />
                <div className="h-10 w-24 bg-gray-200 rounded animate-pulse" />
              </div>
            </div>

            <div className="bg-white rounded-xl p-6 space-y-4">
              <div className="flex gap-4">
                <div className="w-14 h-14 bg-gray-200 rounded-full animate-pulse" />
                <div className="flex-1 space-y-2">
                  <div className="h-5 w-32 bg-gray-200 rounded animate-pulse" />
                  <div className="h-4 w-24 bg-gray-200 rounded animate-pulse" />
                </div>
              </div>
              <div className="h-12 w-full bg-gray-200 rounded animate-pulse" />
              <div className="h-12 w-full bg-gray-200 rounded animate-pulse" />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
```

---

## ✅ VALIDACIÓN

### Tests

```typescript
// filepath: __tests__/app/vehicle-detail.test.tsx
import { describe, it, expect, vi } from "vitest";
import { screen } from "@testing-library/react";
import { render } from "@tests/utils/test-utils";
import { VehicleHeader } from "@/components/vehicle-detail/VehicleHeader";
import { createVehicle } from "@tests/utils/factories";

describe("VehicleHeader", () => {
  const vehicle = createVehicle({
    year: 2024,
    make: "Toyota",
    model: "Camry",
    price: 1850000,
    city: "Santo Domingo",
  });

  it("renders vehicle title", () => {
    render(<VehicleHeader vehicle={vehicle} />);
    expect(screen.getByText("2024 Toyota Camry")).toBeInTheDocument();
  });

  it("renders price", () => {
    render(<VehicleHeader vehicle={vehicle} />);
    expect(screen.getByText(/1,850,000/)).toBeInTheDocument();
  });

  it("shows favorite button", () => {
    render(<VehicleHeader vehicle={vehicle} />);
    expect(screen.getByRole("button", { name: /guardar/i })).toBeInTheDocument();
  });

  it("shows share button", () => {
    render(<VehicleHeader vehicle={vehicle} />);
    expect(screen.getByRole("button", { name: /compartir/i })).toBeInTheDocument();
  });
});
```

### Ejecutar

```bash
pnpm test vehicle-detail

pnpm dev
# Verificar en http://localhost:3000/vehiculos/toyota-camry-2024:
# - Galería con lightbox
# - Tabs funcionan
# - Botones de contacto
# - Share funciona
# - SEO metadata correcta
```

---

## 📊 RESUMEN

| Componente      | Archivo                            | Función                      |
| --------------- | ---------------------------------- | ---------------------------- |
| VehiclePage     | `vehiculos/[slug]/page.tsx`        | Página principal con SSR     |
| VehicleHeader   | `vehicle-detail/VehicleHeader.tsx` | Título, precio, acciones     |
| SellerCard      | `vehicle-detail/SellerCard.tsx`    | Info vendedor + CTAs         |
| VehicleTabs     | `vehicle-detail/VehicleTabs.tsx`   | Descripción, specs, features |
| ContactModal    | `vehicle-detail/ContactModal.tsx`  | Formulario de contacto       |
| VehicleNotFound | `vehiculos/[slug]/not-found.tsx`   | 404 personalizado            |
| VehicleLoading  | `vehiculos/[slug]/loading.tsx`     | Estado de carga              |

---

## 🔧 PASO 15: Deal Rating Badge (P2 - Feature de Confianza) 💎

```typescript
// filepath: src/components/vehicles/DealRatingBadge.tsx
"use client";

import * as React from "react";
import { motion } from "framer-motion";
import { Sparkles, TrendingDown, Info } from "lucide-react";
import { Badge } from "@/components/ui/Badge";
import { Tooltip } from "@/components/ui/Tooltip";
import { useDealRating } from "@/lib/hooks/useVehicleIntelligence";
import { cn } from "@/lib/utils";

interface DealRatingBadgeProps {
  vehicleId: string;
  price: number;
  className?: string;
}

export function DealRatingBadge({ vehicleId, price, className }: DealRatingBadgeProps) {
  const { data: rating, isLoading } = useDealRating(vehicleId);

  if (isLoading || !rating || rating.score < 85) return null;

  return (
    <Tooltip
      content={
        <div className="space-y-2">
          <p className="font-semibold">Excelente Deal</p>
          <p className="text-xs">Este vehículo tiene una excelente relación calidad-precio basado en:</p>
          <ul className="text-xs space-y-1">
            {rating.factors.map((factor, i) => (
              <li key={i}>• {factor}</li>
            ))}
          </ul>
          <p className="text-xs font-medium text-green-600 mt-2">
            Score: {rating.score}/100
          </p>
        </div>
      }
    >
      <motion.div
        initial={{ opacity: 0, scale: 0.9 }}
        animate={{ opacity: 1, scale: 1 }}
        className={cn("inline-flex", className)}
      >
        <Badge
          variant="success"
          className="bg-gradient-to-r from-green-500 to-emerald-500 text-white font-semibold px-3 py-1.5 shadow-lg"
        >
          <Sparkles className="h-3.5 w-3.5 mr-1.5" />
          Excelente Deal
        </Badge>
      </motion.div>
    </Tooltip>
  );
}

// Hook requerido
import { useQuery } from "@tanstack/react-query";
import { vehicleIntelligenceService } from "@/lib/services/vehicleIntelligenceService";

export function useDealRating(vehicleId: string) {
  return useQuery({
    queryKey: ["deal-rating", vehicleId],
    queryFn: () => vehicleIntelligenceService.getDealRating(vehicleId),
    staleTime: 10 * 60 * 1000, // 10 minutos
  });
}
```

**Uso en VehicleCard:**

```typescript
// filepath: src/components/vehicles/VehicleCard.tsx
import { DealRatingBadge } from "./DealRatingBadge";

export function VehicleCard({ vehicle }: { vehicle: Vehicle }) {
  return (
    <div className="relative bg-white rounded-xl shadow-sm hover:shadow-md transition-shadow">
      {/* Deal Rating Badge - Positioned top-left of image */}
      <div className="absolute top-2 left-2 z-10">
        <DealRatingBadge vehicleId={vehicle.id} price={vehicle.price} />
      </div>

      {/* Rest of card... */}
      <Image src={vehicle.image} alt={vehicle.title} />
      {/* ... */}
    </div>
  );
}
```

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/vehicle-detail.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Vehicle Detail Page", () => {
  const vehicleSlug = "toyota-camry-2023-santo-domingo"; // Usar slug de fixture

  test.beforeEach(async ({ page }) => {
    await page.goto(`/vehiculos/${vehicleSlug}`);
  });

  test("debe cargar información del vehículo", async ({ page }) => {
    await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
    await expect(page.getByTestId("vehicle-price")).toBeVisible();
    await expect(page.getByTestId("vehicle-specs")).toBeVisible();
  });

  test("debe mostrar galería de imágenes funcional", async ({ page }) => {
    const gallery = page.getByTestId("vehicle-gallery");
    await expect(gallery).toBeVisible();

    // Click en thumbnail
    await gallery.locator("button").nth(1).click();
    await expect(gallery.locator("[data-active='true']")).toBeVisible();
  });

  test("debe abrir lightbox al hacer click en imagen principal", async ({
    page,
  }) => {
    await page.getByTestId("main-image").click();
    await expect(page.getByRole("dialog")).toBeVisible();
    await expect(page.getByTestId("lightbox")).toBeVisible();

    // Cerrar con Escape
    await page.keyboard.press("Escape");
    await expect(page.getByTestId("lightbox")).not.toBeVisible();
  });

  test("debe mostrar tabs de especificaciones", async ({ page }) => {
    const tabs = page.getByRole("tablist");
    await expect(tabs).toBeVisible();

    await page.getByRole("tab", { name: /características/i }).click();
    await expect(page.getByRole("tabpanel")).toContainText(
      /motor|transmisión/i,
    );
  });

  test("debe mostrar información del vendedor", async ({ page }) => {
    const sellerCard = page.getByTestId("seller-info");
    await expect(sellerCard).toBeVisible();
    await expect(
      sellerCard.getByRole("button", { name: /contactar/i }),
    ).toBeVisible();
  });

  test("debe agregar/quitar de favoritos (autenticado)", async ({ page }) => {
    // Login primero
    await page.goto("/auth/login");
    await page.fill('input[name="email"]', "test@example.com");
    await page.fill('input[name="password"]', "password123");
    await page.click('button[type="submit"]');

    await page.goto(`/vehiculos/${vehicleSlug}`);

    const favButton = page.getByRole("button", { name: /favorito/i });
    await favButton.click();

    await expect(page.getByText(/agregado a favoritos/i)).toBeVisible();
  });

  test("debe mostrar modal de contacto", async ({ page }) => {
    await page.getByRole("button", { name: /contactar vendedor/i }).click();

    const modal = page.getByRole("dialog");
    await expect(modal).toBeVisible();
    await expect(
      modal.getByRole("textbox", { name: /mensaje/i }),
    ).toBeVisible();
  });

  test("debe compartir vehículo", async ({ page }) => {
    await page.getByRole("button", { name: /compartir/i }).click();

    const shareOptions = page.getByTestId("share-options");
    await expect(shareOptions).toBeVisible();
    await expect(
      shareOptions.getByRole("button", { name: /whatsapp/i }),
    ).toBeVisible();
    await expect(
      shareOptions.getByRole("button", { name: /copiar link/i }),
    ).toBeVisible();
  });

  test("debe mostrar vehículos similares", async ({ page }) => {
    await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
    await expect(page.getByTestId("similar-vehicles")).toBeVisible();
    await expect(page.locator("[data-testid='vehicle-card']")).toHaveCount({
      min: 3,
    });
  });

  test("debe tener meta tags SEO correctos para el vehículo", async ({
    page,
  }) => {
    const title = await page.title();
    expect(title).toContain("Toyota");
    expect(title).toContain("Camry");

    const ogImage = await page.getAttribute(
      'meta[property="og:image"]',
      "content",
    );
    expect(ogImage).toBeTruthy();
  });
});
```

---

## 📚 REFERENCIAS

### Documentos Process Matrix

- [01-vehicles-service.md](../../process-matrix/02-VEHICULOS-VENTA/01-vehicles-service.md) - Backend API
- [01-media-service.md](../../process-matrix/10-MEDIA-ARCHIVOS/01-media-service.md) - Media & CDN

### Documentos Frontend

- **[100-media-multimedia-completo.md](../04-VENDEDOR/05-media-multimedia.md)** - 🆕 Gallery mejorada, lightbox, vista 360°
- **[101-financiamiento-tradein-completo.md](../05-DEALER/15-financiamiento-tradein.md)** - 🆕 Widget de financiamiento, trade-in, historial vehículo
- [02-vehicle-components.md](../03-COMPONENTES/02-vehicle-components.md) - Componentes de vehículos
- [02-busqueda.md](./02-busqueda.md) - Página de búsqueda
- [04-publicar.md](../04-VENDEDOR/01-publicar-vehiculo.md) - Formulario de publicación

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/04-PAGINAS/04-publicar.md`
