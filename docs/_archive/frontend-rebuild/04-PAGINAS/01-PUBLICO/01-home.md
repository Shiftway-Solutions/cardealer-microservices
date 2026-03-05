---
title: "Página Principal (Home)"
priority: P0
estimated_time: "90 minutos"
dependencies: []
apis: []
status: complete
last_updated: "2026-01-30"
---

# 🏠 Página Principal (Home)

> **Tiempo estimado:** 90 minutos
> **Prerrequisitos:** Layout, VehicleCard, design tokens

---

## 📋 OBJETIVO

Implementar la página principal de OKLA con:

- Hero section con búsqueda
- Vehículos destacados
- Categorías por tipo
- Marcas populares
- Secciones de valor
- CTA para vendedores

---

## 🎨 ESTRUCTURA DE LA PÁGINA

```
┌─────────────────────────────────────────────────────┐
│ HEADER (Navbar)                                      │
├─────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────┐ │
│ │ HERO SECTION                                     │ │
│ │ • Título impactante                             │ │
│ │ • Subtítulo con propuesta de valor              │ │
│ │ • Barra de búsqueda grande                      │ │
│ │ • Stats (vehículos, dealers, ventas)            │ │
│ └─────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────┐ │
│ │ BÚSQUEDA RÁPIDA                                 │ │
│ │ [Marca ▼] [Modelo ▼] [Año ▼] [Precio ▼] [🔍]    │ │
│ └─────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────┐ │
│ │ VEHÍCULOS DESTACADOS                            │ │
│ │ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐     │ │
│ │ │    │ │    │ │    │ │    │ │    │ │    │     │ │
│ │ └────┘ └────┘ └────┘ └────┘ └────┘ └────┘     │ │
│ └─────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────┐ │
│ │ CATEGORÍAS POR TIPO                             │ │
│ │ [SUV] [Sedán] [Pickup] [Deportivo] [Eléctrico] │ │
│ └─────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────┐ │
│ │ MARCAS POPULARES                                │ │
│ │ [Toyota] [Honda] [Hyundai] [Kia] [Ford] [+]    │ │
│ └─────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────┐ │
│ │ ¿POR QUÉ OKLA?                                  │ │
│ │ • Verificación  • Seguridad  • Facilidad       │ │
│ └─────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────┐ │
│ │ CTA: ¿TIENES UN VEHÍCULO PARA VENDER?          │ │
│ │           [Publicar Gratis]                     │ │
│ └─────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────┤
│ FOOTER                                              │
└─────────────────────────────────────────────────────┘
```

---

## 🔧 PASO 1: Página Principal

```typescript
// filepath: src/app/(main)/page.tsx
import { Metadata } from "next";
import { Suspense } from "react";
import { HeroSection } from "@/components/home/HeroSection";
import { QuickSearch } from "@/components/home/QuickSearch";
import { FeaturedVehicles } from "@/components/home/FeaturedVehicles";
import { CategoryGrid } from "@/components/home/CategoryGrid";
import { PopularMakes } from "@/components/home/PopularMakes";
import { ValueProposition } from "@/components/home/ValueProposition";
import { SellerCTA } from "@/components/home/SellerCTA";
import { VehicleGridSkeleton } from "@/components/vehicles/VehicleCardSkeleton";

export const metadata: Metadata = {
  title: "OKLA | Compra y Vende Vehículos en República Dominicana",
  description:
    "El marketplace #1 de vehículos en RD. Encuentra carros nuevos y usados de dealers verificados y vendedores particulares. Toyota, Honda, Hyundai y más.",
  openGraph: {
    title: "OKLA | Compra y Vende Vehículos en República Dominicana",
    description:
      "El marketplace #1 de vehículos en RD. Encuentra carros nuevos y usados.",
    images: ["/og-home.jpg"],
  },
};

export default function HomePage() {
  return (
    <>
      {/* Hero con búsqueda */}
      <HeroSection />

      {/* Búsqueda rápida - Sticky en scroll */}
      <QuickSearch />

      {/* Vehículos destacados */}
      <section className="container py-12 md:py-16">
        <div className="flex items-center justify-between mb-8">
          <div>
            <h2 className="text-2xl md:text-3xl font-bold text-gray-900">
              Vehículos Destacados
            </h2>
            <p className="text-gray-600 mt-1">
              Los mejores vehículos seleccionados para ti
            </p>
          </div>
          <a
            href="/vehiculos?destacados=true"
            className="text-primary-600 hover:text-primary-700 font-medium hidden md:block"
          >
            Ver todos →
          </a>
        </div>

        <Suspense fallback={<VehicleGridSkeleton count={6} />}>
          <FeaturedVehicles />
        </Suspense>
      </section>

      {/* Categorías por tipo */}
      <section className="bg-gray-50 py-12 md:py-16">
        <div className="container">
          <h2 className="text-2xl md:text-3xl font-bold text-gray-900 text-center mb-8">
            Explora por Categoría
          </h2>
          <CategoryGrid />
        </div>
      </section>

      {/* Marcas populares */}
      <section className="container py-12 md:py-16">
        <h2 className="text-2xl md:text-3xl font-bold text-gray-900 text-center mb-8">
          Marcas Populares
        </h2>
        <PopularMakes />
      </section>

      {/* Propuesta de valor */}
      <ValueProposition />

      {/* CTA para vendedores */}
      <SellerCTA />
    </>
  );
}
```

---

## 🔧 PASO 2: HeroSection

```typescript
// filepath: src/components/home/HeroSection.tsx
import Image from "next/image";
import { Search } from "lucide-react";
import { Button } from "@/components/ui/Button";

const STATS = [
  { value: "15,000+", label: "Vehículos" },
  { value: "500+", label: "Dealers Verificados" },
  { value: "50,000+", label: "Usuarios Activos" },
];

export function HeroSection() {
  return (
    <section className="relative bg-gradient-to-br from-gray-900 via-gray-800 to-primary-900 overflow-hidden">
      {/* Background Pattern */}
      <div className="absolute inset-0 opacity-10">
        <Image
          src="/images/hero-pattern.svg"
          alt=""
          fill
          className="object-cover"
          priority
        />
      </div>

      <div className="container relative py-16 md:py-24 lg:py-32">
        <div className="max-w-3xl mx-auto text-center">
          {/* Badge */}
          <div className="inline-flex items-center gap-2 bg-primary-500/20 text-primary-300 px-4 py-1.5 rounded-full text-sm font-medium mb-6">
            <span className="relative flex h-2 w-2">
              <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-primary-400 opacity-75"></span>
              <span className="relative inline-flex rounded-full h-2 w-2 bg-primary-500"></span>
            </span>
            El marketplace #1 de vehículos en RD
          </div>

          {/* Title */}
          <h1 className="text-4xl md:text-5xl lg:text-6xl font-bold text-white leading-tight">
            Encuentra tu{" "}
            <span className="text-transparent bg-clip-text bg-gradient-to-r from-primary-400 to-primary-300">
              vehículo perfecto
            </span>
          </h1>

          {/* Subtitle */}
          <p className="mt-6 text-lg md:text-xl text-gray-300 max-w-2xl mx-auto">
            Miles de vehículos nuevos y usados de dealers verificados y
            vendedores particulares. Compra con confianza.
          </p>

          {/* CTA Buttons */}
          <div className="flex flex-col sm:flex-row items-center justify-center gap-4 mt-8">
            <Button
              size="lg"
              asChild
              className="w-full sm:w-auto"
            >
              <a href="/vehiculos">
                <Search size={18} className="mr-2" />
                Buscar Vehículos
              </a>
            </Button>
            <Button
              variant="outline"
              size="lg"
              asChild
              className="w-full sm:w-auto text-white border-white/30 hover:bg-white/10"
            >
              <a href="/publicar">Vender mi Vehículo</a>
            </Button>
          </div>

          {/* Stats */}
          <div className="grid grid-cols-3 gap-8 mt-12 pt-8 border-t border-white/10">
            {STATS.map((stat) => (
              <div key={stat.label} className="text-center">
                <div className="text-2xl md:text-3xl font-bold text-white">
                  {stat.value}
                </div>
                <div className="text-sm text-gray-400 mt-1">{stat.label}</div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Decorative car silhouettes */}
      <div className="absolute bottom-0 left-0 right-0 h-24 bg-gradient-to-t from-white to-transparent" />
    </section>
  );
}
```

---

## 🔧 PASO 3: QuickSearch

```typescript
// filepath: src/components/home/QuickSearch.tsx
"use client";

import * as React from "react";
import { useRouter } from "next/navigation";
import { Search } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Select } from "@/components/ui/Select";
import { useMakes, useModels } from "@/lib/hooks/useCatalog";

export function QuickSearch() {
  const router = useRouter();
  const [makeId, setMakeId] = React.useState<string>("");
  const [modelId, setModelId] = React.useState<string>("");
  const [yearMin, setYearMin] = React.useState<string>("");
  const [priceMax, setPriceMax] = React.useState<string>("");

  const { data: makes } = useMakes();
  const { data: models } = useModels(makeId);

  const handleSearch = () => {
    const params = new URLSearchParams();
    if (makeId) params.set("makeId", makeId);
    if (modelId) params.set("modelId", modelId);
    if (yearMin) params.set("yearMin", yearMin);
    if (priceMax) params.set("priceMax", priceMax);

    router.push(`/vehiculos?${params.toString()}`);
  };

  // Reset model when make changes
  React.useEffect(() => {
    setModelId("");
  }, [makeId]);

  const makeOptions = makes?.data.map((m) => ({ value: m.id, label: m.name })) ?? [];
  const modelOptions = models?.data.map((m) => ({ value: m.id, label: m.name })) ?? [];

  const currentYear = new Date().getFullYear();
  const yearOptions = Array.from({ length: 35 }, (_, i) => {
    const year = currentYear - i;
    return { value: year.toString(), label: year.toString() };
  });

  const priceOptions = [
    { value: "500000", label: "Hasta RD$ 500,000" },
    { value: "1000000", label: "Hasta RD$ 1,000,000" },
    { value: "1500000", label: "Hasta RD$ 1,500,000" },
    { value: "2000000", label: "Hasta RD$ 2,000,000" },
    { value: "3000000", label: "Hasta RD$ 3,000,000" },
    { value: "5000000", label: "Hasta RD$ 5,000,000" },
  ];

  return (
    <section className="sticky top-16 z-30 bg-white border-b border-gray-200 shadow-sm">
      <div className="container py-4">
        <div className="flex flex-wrap items-end gap-3">
          <div className="flex-1 min-w-[140px]">
            <label className="text-xs font-medium text-gray-600 mb-1 block">
              Marca
            </label>
            <Select
              value={makeId}
              onValueChange={setMakeId}
              options={makeOptions}
              placeholder="Todas"
            />
          </div>

          <div className="flex-1 min-w-[140px]">
            <label className="text-xs font-medium text-gray-600 mb-1 block">
              Modelo
            </label>
            <Select
              value={modelId}
              onValueChange={setModelId}
              options={modelOptions}
              placeholder="Todos"
              disabled={!makeId}
            />
          </div>

          <div className="w-32">
            <label className="text-xs font-medium text-gray-600 mb-1 block">
              Año desde
            </label>
            <Select
              value={yearMin}
              onValueChange={setYearMin}
              options={yearOptions}
              placeholder="Año"
            />
          </div>

          <div className="w-44">
            <label className="text-xs font-medium text-gray-600 mb-1 block">
              Precio máximo
            </label>
            <Select
              value={priceMax}
              onValueChange={setPriceMax}
              options={priceOptions}
              placeholder="Cualquier precio"
            />
          </div>

          <Button onClick={handleSearch} className="h-10">
            <Search size={18} className="mr-2" />
            Buscar
          </Button>
        </div>
      </div>
    </section>
  );
}
```

---

## 🔧 PASO 4: FeaturedVehicles

```typescript
// filepath: src/components/home/FeaturedVehicles.tsx
"use client";

import { VehicleCard } from "@/components/vehicles/VehicleCard";
import { VehicleCardSkeleton } from "@/components/vehicles/VehicleCardSkeleton";
import { useFeaturedVehicles } from "@/lib/hooks/useVehicles";
import { useFavorites } from "@/lib/hooks/useFavorites";

export function FeaturedVehicles() {
  const { data, isLoading, error } = useFeaturedVehicles();
  const { favorites, toggleFavorite } = useFavorites();

  if (isLoading) {
    return (
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
        {Array.from({ length: 6 }).map((_, i) => (
          <VehicleCardSkeleton key={i} />
        ))}
      </div>
    );
  }

  if (error || !data?.data) {
    return (
      <div className="text-center py-12 text-gray-500">
        No se pudieron cargar los vehículos destacados
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
      {data.data.slice(0, 6).map((vehicle, index) => (
        <VehicleCard
          key={vehicle.id}
          vehicle={vehicle}
          isFavorited={favorites.has(vehicle.id)}
          onFavoriteClick={toggleFavorite}
          priority={index < 3}
        />
      ))}
    </div>
  );
}

// Skeleton for Suspense
export function VehicleGridSkeleton({ count = 6 }: { count?: number }) {
  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
      {Array.from({ length: count }).map((_, i) => (
        <VehicleCardSkeleton key={i} />
      ))}
    </div>
  );
}
```

---

## 🔧 PASO 5: CategoryGrid

```typescript
// filepath: src/components/home/CategoryGrid.tsx
import Image from "next/image";
import Link from "next/link";
import { cn } from "@/lib/utils";

const CATEGORIES = [
  {
    id: "suv",
    name: "SUVs",
    image: "/images/categories/suv.jpg",
    count: 2500,
    href: "/vehiculos?bodyType=suv",
  },
  {
    id: "sedan",
    name: "Sedanes",
    image: "/images/categories/sedan.jpg",
    count: 3200,
    href: "/vehiculos?bodyType=sedan",
  },
  {
    id: "pickup",
    name: "Pickups",
    image: "/images/categories/pickup.jpg",
    count: 1800,
    href: "/vehiculos?bodyType=pickup",
  },
  {
    id: "hatchback",
    name: "Hatchback",
    image: "/images/categories/hatchback.jpg",
    count: 1500,
    href: "/vehiculos?bodyType=hatchback",
  },
  {
    id: "coupe",
    name: "Deportivos",
    image: "/images/categories/coupe.jpg",
    count: 450,
    href: "/vehiculos?bodyType=coupe",
  },
  {
    id: "electric",
    name: "Eléctricos",
    image: "/images/categories/electric.jpg",
    count: 320,
    href: "/vehiculos?fuelType=electric",
  },
];

export function CategoryGrid() {
  return (
    <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4">
      {CATEGORIES.map((category) => (
        <Link
          key={category.id}
          href={category.href}
          className={cn(
            "group relative aspect-[4/3] rounded-xl overflow-hidden",
            "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2"
          )}
        >
          {/* Image */}
          <Image
            src={category.image}
            alt={category.name}
            fill
            sizes="(max-width: 768px) 50vw, (max-width: 1024px) 33vw, 16vw"
            className="object-cover transition-transform duration-500 group-hover:scale-110"
          />

          {/* Overlay */}
          <div className="absolute inset-0 bg-gradient-to-t from-black/70 via-black/20 to-transparent" />

          {/* Content */}
          <div className="absolute inset-x-0 bottom-0 p-4">
            <h3 className="text-white font-semibold text-lg">
              {category.name}
            </h3>
            <p className="text-white/80 text-sm">
              {category.count.toLocaleString()} vehículos
            </p>
          </div>
        </Link>
      ))}
    </div>
  );
}
```

---

## 🔧 PASO 6: PopularMakes

```typescript
// filepath: src/components/home/PopularMakes.tsx
"use client";

import Image from "next/image";
import Link from "next/link";
import { useMakes } from "@/lib/hooks/useCatalog";
import { cn } from "@/lib/utils";

// Fallback logos for popular makes
const MAKE_LOGOS: Record<string, string> = {
  toyota: "/images/makes/toyota.svg",
  honda: "/images/makes/honda.svg",
  hyundai: "/images/makes/hyundai.svg",
  kia: "/images/makes/kia.svg",
  nissan: "/images/makes/nissan.svg",
  ford: "/images/makes/ford.svg",
  chevrolet: "/images/makes/chevrolet.svg",
  mercedes: "/images/makes/mercedes.svg",
  bmw: "/images/makes/bmw.svg",
  audi: "/images/makes/audi.svg",
};

export function PopularMakes() {
  const { data, isLoading } = useMakes();

  if (isLoading) {
    return (
      <div className="grid grid-cols-3 sm:grid-cols-5 lg:grid-cols-10 gap-4">
        {Array.from({ length: 10 }).map((_, i) => (
          <div
            key={i}
            className="aspect-square bg-gray-100 rounded-xl animate-pulse"
          />
        ))}
      </div>
    );
  }

  // Take top 10 by vehicle count
  const topMakes = data?.data
    ?.sort((a, b) => (b.vehicleCount || 0) - (a.vehicleCount || 0))
    .slice(0, 10) ?? [];

  return (
    <div className="grid grid-cols-3 sm:grid-cols-5 lg:grid-cols-10 gap-4">
      {topMakes.map((make) => (
        <Link
          key={make.id}
          href={`/marcas/${make.slug}`}
          className={cn(
            "group flex flex-col items-center justify-center p-4 bg-white rounded-xl",
            "border border-gray-200 transition-all duration-300",
            "hover:border-primary-300 hover:shadow-md",
            "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500"
          )}
        >
          <div className="relative w-12 h-12 mb-2">
            <Image
              src={MAKE_LOGOS[make.slug] || "/images/makes/generic.svg"}
              alt={make.name}
              fill
              sizes="48px"
              className="object-contain grayscale group-hover:grayscale-0 transition-all"
            />
          </div>
          <span className="text-sm font-medium text-gray-700 group-hover:text-primary-600 transition-colors">
            {make.name}
          </span>
          <span className="text-xs text-gray-400">
            {make.vehicleCount?.toLocaleString()}
          </span>
        </Link>
      ))}
    </div>
  );
}
```

---

## 🔧 PASO 7: ValueProposition

```typescript
// filepath: src/components/home/ValueProposition.tsx
import { ShieldCheck, Zap, Award, HeadphonesIcon } from "lucide-react";

const VALUES = [
  {
    icon: ShieldCheck,
    title: "Dealers Verificados",
    description:
      "Todos los dealers pasan por un proceso de verificación para garantizar transacciones seguras.",
    color: "text-green-600",
    bgColor: "bg-green-100",
  },
  {
    icon: Zap,
    title: "Publicación Rápida",
    description:
      "Publica tu vehículo en menos de 5 minutos y llega a miles de compradores potenciales.",
    color: "text-yellow-600",
    bgColor: "bg-yellow-100",
  },
  {
    icon: Award,
    title: "Los Mejores Precios",
    description:
      "Compara precios de miles de vehículos para encontrar la mejor oferta del mercado.",
    color: "text-blue-600",
    bgColor: "bg-blue-100",
  },
  {
    icon: HeadphonesIcon,
    title: "Soporte 24/7",
    description:
      "Nuestro equipo está disponible para ayudarte en cualquier momento que lo necesites.",
    color: "text-purple-600",
    bgColor: "bg-purple-100",
  },
];

export function ValueProposition() {
  return (
    <section className="bg-gray-50 py-16 md:py-20">
      <div className="container">
        <div className="text-center mb-12">
          <h2 className="text-2xl md:text-3xl font-bold text-gray-900">
            ¿Por qué elegir OKLA?
          </h2>
          <p className="text-gray-600 mt-2 max-w-2xl mx-auto">
            Somos el marketplace de vehículos más confiable de República
            Dominicana
          </p>
        </div>

        <div className="grid md:grid-cols-2 lg:grid-cols-4 gap-8">
          {VALUES.map((value) => (
            <div
              key={value.title}
              className="text-center p-6 bg-white rounded-xl shadow-sm hover:shadow-md transition-shadow"
            >
              <div
                className={`inline-flex p-4 rounded-full ${value.bgColor} mb-4`}
              >
                <value.icon className={`h-8 w-8 ${value.color}`} />
              </div>
              <h3 className="text-lg font-semibold text-gray-900 mb-2">
                {value.title}
              </h3>
              <p className="text-gray-600 text-sm">{value.description}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
```

---

## 🔧 PASO 8: SellerCTA

```typescript
// filepath: src/components/home/SellerCTA.tsx
import Image from "next/image";
import { ArrowRight, Camera, DollarSign, Users } from "lucide-react";
import { Button } from "@/components/ui/Button";

export function SellerCTA() {
  return (
    <section className="relative py-16 md:py-24 overflow-hidden">
      {/* Background */}
      <div className="absolute inset-0 bg-gradient-to-r from-primary-600 to-primary-700" />
      <div className="absolute inset-0 opacity-10">
        <Image
          src="/images/cta-pattern.svg"
          alt=""
          fill
          className="object-cover"
        />
      </div>

      <div className="container relative">
        <div className="grid lg:grid-cols-2 gap-12 items-center">
          {/* Content */}
          <div>
            <h2 className="text-3xl md:text-4xl font-bold text-white">
              ¿Tienes un vehículo para vender?
            </h2>
            <p className="mt-4 text-lg text-primary-100 max-w-lg">
              Llega a miles de compradores potenciales. Publicar es gratis y
              solo toma 5 minutos.
            </p>

            {/* Benefits */}
            <div className="grid sm:grid-cols-3 gap-6 mt-8">
              <div className="flex items-start gap-3">
                <div className="p-2 bg-white/20 rounded-lg">
                  <Camera size={24} className="text-white" />
                </div>
                <div>
                  <div className="font-semibold text-white">Fácil</div>
                  <div className="text-sm text-primary-100">
                    Hasta 20 fotos
                  </div>
                </div>
              </div>
              <div className="flex items-start gap-3">
                <div className="p-2 bg-white/20 rounded-lg">
                  <Users size={24} className="text-white" />
                </div>
                <div>
                  <div className="font-semibold text-white">Alcance</div>
                  <div className="text-sm text-primary-100">
                    Miles de compradores
                  </div>
                </div>
              </div>
              <div className="flex items-start gap-3">
                <div className="p-2 bg-white/20 rounded-lg">
                  <DollarSign size={24} className="text-white" />
                </div>
                <div>
                  <div className="font-semibold text-white">Gratis</div>
                  <div className="text-sm text-primary-100">
                    Primera publicación
                  </div>
                </div>
              </div>
            </div>

            {/* CTA Button */}
            <Button
              size="lg"
              variant="secondary"
              asChild
              className="mt-8 bg-white text-primary-700 hover:bg-gray-100"
            >
              <a href="/publicar">
                Publicar mi Vehículo
                <ArrowRight size={18} className="ml-2" />
              </a>
            </Button>
          </div>

          {/* Image */}
          <div className="relative hidden lg:block">
            <div className="relative aspect-[4/3] rounded-2xl overflow-hidden shadow-2xl">
              <Image
                src="/images/sell-your-car.jpg"
                alt="Vende tu vehículo en OKLA"
                fill
                sizes="50vw"
                className="object-cover"
              />
            </div>
            {/* Floating Stats Card */}
            <div className="absolute -bottom-6 -left-6 bg-white rounded-xl shadow-lg p-4">
              <div className="text-3xl font-bold text-primary-600">
                $1.2M
              </div>
              <div className="text-sm text-gray-600">
                Vendido esta semana
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
```

---

## ✅ VALIDACIÓN

### Tests

```typescript
// filepath: __tests__/app/home.test.tsx
import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import { render } from "@tests/utils/test-utils";
import HomePage from "@/app/(main)/page";

describe("HomePage", () => {
  it("renders hero section", () => {
    render(<HomePage />);

    expect(
      screen.getByText(/encuentra tu vehículo perfecto/i)
    ).toBeInTheDocument();
  });

  it("renders search CTA", () => {
    render(<HomePage />);

    expect(
      screen.getByRole("link", { name: /buscar vehículos/i })
    ).toBeInTheDocument();
  });

  it("renders featured vehicles section", () => {
    render(<HomePage />);

    expect(screen.getByText(/vehículos destacados/i)).toBeInTheDocument();
  });

  it("renders value proposition", () => {
    render(<HomePage />);

    expect(screen.getByText(/¿por qué elegir okla\?/i)).toBeInTheDocument();
  });
});
```

### Ejecutar

```bash
pnpm test app/home

pnpm dev
# Verificar en http://localhost:3000:
# - Hero con animaciones
# - Búsqueda rápida funcional
# - Grid de vehículos
# - Categorías clickeables
# - Marcas con logos
# - CTA de vendedor
```

---

## 📊 RESUMEN

| Componente       | Archivo                     | Función                |
| ---------------- | --------------------------- | ---------------------- |
| HeroSection      | `home/HeroSection.tsx`      | Título + Stats + CTAs  |
| QuickSearch      | `home/QuickSearch.tsx`      | Búsqueda rápida sticky |
| FeaturedVehicles | `home/FeaturedVehicles.tsx` | Grid de destacados     |
| CategoryGrid     | `home/CategoryGrid.tsx`     | Categorías visuales    |
| PopularMakes     | `home/PopularMakes.tsx`     | Logos de marcas        |
| ValueProposition | `home/ValueProposition.tsx` | 4 valores              |
| SellerCTA        | `home/SellerCTA.tsx`        | CTA para vendedores    |

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/home.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Home Page", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/");
  });

  test("debe cargar el hero con estadísticas", async ({ page }) => {
    await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
    await expect(page.getByTestId("hero-stats")).toBeVisible();
    await expect(page.getByText(/vehículos disponibles/i)).toBeVisible();
  });

  test("debe mostrar búsqueda rápida funcional", async ({ page }) => {
    const searchBox = page.getByTestId("quick-search");
    await expect(searchBox).toBeVisible();

    // Seleccionar marca
    await page.getByRole("combobox", { name: /marca/i }).click();
    await page.getByRole("option", { name: "Toyota" }).click();

    // Click buscar
    await page.getByRole("button", { name: /buscar/i }).click();

    await expect(page).toHaveURL(/\/vehiculos\?make=toyota/);
  });

  test("debe mostrar vehículos destacados", async ({ page }) => {
    await expect(page.getByTestId("featured-vehicles")).toBeVisible();
    await expect(page.locator("[data-testid='vehicle-card']")).toHaveCount({
      min: 4,
    });
  });

  test("debe navegar al hacer click en categoría", async ({ page }) => {
    await page.getByRole("link", { name: "SUVs" }).click();
    await expect(page).toHaveURL(/\/vehiculos\?bodyType=suv/);
  });

  test("debe navegar al hacer click en marca popular", async ({ page }) => {
    await page
      .getByTestId("popular-makes")
      .getByRole("link", { name: "Toyota" })
      .click();
    await expect(page).toHaveURL(/\/vehiculos\?make=toyota/);
  });

  test("debe mostrar CTA para vendedores", async ({ page }) => {
    const sellerCTA = page.getByTestId("seller-cta");
    await expect(sellerCTA).toBeVisible();
    await sellerCTA.getByRole("link", { name: /publicar/i }).click();
    await expect(page).toHaveURL(/\/vender/);
  });

  test("debe tener meta tags SEO correctos", async ({ page }) => {
    const title = await page.title();
    expect(title).toContain("OKLA");

    const description = await page.getAttribute(
      'meta[name="description"]',
      "content",
    );
    expect(description).toBeTruthy();
  });
});
```

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/04-PAGINAS/02-busqueda.md`

---

## 📚 REFERENCIAS ADICIONALES

### Documentos Relacionados

- **[101-financiamiento-tradein-completo.md](../05-DEALER/15-financiamiento-tradein.md)** - 🆕 Para agregar CTA de financiamiento y trade-in en homepage
  - Sección recomendada: "¿Quieres Cambiar tu Vehículo?" con link a `/trade-in/estimate`
  - Beneficio: Captura usuarios interesados en trade-in desde el home
