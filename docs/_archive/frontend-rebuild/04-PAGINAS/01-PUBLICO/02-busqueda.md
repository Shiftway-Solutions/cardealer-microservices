---
title: "Página de Búsqueda - Base"
priority: P0
estimated_time: "90 minutos"
dependencies: []
apis: []
status: complete
last_updated: "2026-01-30"
---

# 🔍 Página de Búsqueda - Base

> **Scope:** Búsqueda básica con 11 filtros estándar (Marca, Modelo, Año, Precio, etc.)  
> **Extensiones disponibles:**  
> • [31-filtros-avanzados-completo.md](31-filtros-avanzados-completo.md) - 12 filtros ADICIONALES  
> • [32-search-completo.md](32-search-completo.md) - Full-text search con Elasticsearch

> **Tiempo estimado:** 90 minutos
> **Prerrequisitos:** VehicleCard, SearchFilters, API hooks

---

## 📋 OBJETIVO

Implementar la página de búsqueda con:

- Filtros laterales (desktop) / Sheet (mobile)
- Grid de resultados con paginación
- URL state management
- Ordenamiento
- Contador de resultados
- Vista lista/grid

---

## 🎨 ESTRUCTURA DE LA PÁGINA

```
Desktop:
┌─────────────────────────────────────────────────────┐
│ HEADER                                              │
├─────────────────────────────────────────────────────┤
│ BREADCRUMB: Inicio > Vehículos                      │
├────────┬────────────────────────────────────────────┤
│        │ TOOLBAR                                    │
│        │ [X Resultados] [Sort ▼] [Grid/List]        │
│ FILTERS├────────────────────────────────────────────┤
│        │ ┌────┐ ┌────┐ ┌────┐                       │
│ Marca  │ │    │ │    │ │    │                       │
│ Modelo │ └────┘ └────┘ └────┘                       │
│ Año    │ ┌────┐ ┌────┐ ┌────┐                       │
│ Precio │ │    │ │    │ │    │                       │
│ ...    │ └────┘ └────┘ └────┘                       │
│        │                                            │
│        │ PAGINATION                                 │
│        │ [< 1 2 3 4 5 ... 10 >]                     │
├────────┴────────────────────────────────────────────┤
│ FOOTER                                              │
└─────────────────────────────────────────────────────┘

Mobile:
┌─────────────────────────────────────────────────────┐
│ HEADER                                              │
├─────────────────────────────────────────────────────┤
│ [🔍 Buscar...        ] [Filtros] [Sort ▼]           │
├─────────────────────────────────────────────────────┤
│ 150 vehículos encontrados                           │
├─────────────────────────────────────────────────────┤
│ ┌────────────────────────────────────────────────┐  │
│ │ Vehicle Card                                   │  │
│ └────────────────────────────────────────────────┘  │
│ ┌────────────────────────────────────────────────┐  │
│ │ Vehicle Card                                   │  │
│ └────────────────────────────────────────────────┘  │
│                                                     │
│ [Cargar más]                                        │
├─────────────────────────────────────────────────────┤
│ MOBILE NAV                                          │
└─────────────────────────────────────────────────────┘
```

---

## 🔧 PASO 1: Hook de URL State

```typescript
// filepath: src/lib/hooks/useSearchParams.ts
"use client";

import * as React from "react";
import {
  useRouter,
  useSearchParams as useNextSearchParams,
  usePathname,
} from "next/navigation";

export interface VehicleSearchParams {
  // Filters
  q?: string;
  makeId?: string;
  modelId?: string;
  yearMin?: string;
  yearMax?: string;
  priceMin?: string;
  priceMax?: string;
  condition?: "new" | "used";
  bodyType?: string;
  fuelType?: string;
  transmission?: string;
  mileageMax?: string;
  city?: string;
  sellerType?: "individual" | "dealer";
  // Pagination
  page?: string;
  pageSize?: string;
  // Sorting
  sortBy?: string;
  sortOrder?: "asc" | "desc";
}

export function useVehicleSearchParams() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useNextSearchParams();

  // Parse current params
  const params = React.useMemo<VehicleSearchParams>(() => {
    return {
      q: searchParams.get("q") ?? undefined,
      makeId: searchParams.get("makeId") ?? undefined,
      modelId: searchParams.get("modelId") ?? undefined,
      yearMin: searchParams.get("yearMin") ?? undefined,
      yearMax: searchParams.get("yearMax") ?? undefined,
      priceMin: searchParams.get("priceMin") ?? undefined,
      priceMax: searchParams.get("priceMax") ?? undefined,
      condition: (searchParams.get("condition") as "new" | "used") ?? undefined,
      bodyType: searchParams.get("bodyType") ?? undefined,
      fuelType: searchParams.get("fuelType") ?? undefined,
      transmission: searchParams.get("transmission") ?? undefined,
      mileageMax: searchParams.get("mileageMax") ?? undefined,
      city: searchParams.get("city") ?? undefined,
      sellerType:
        (searchParams.get("sellerType") as "individual" | "dealer") ??
        undefined,
      page: searchParams.get("page") ?? "1",
      pageSize: searchParams.get("pageSize") ?? "20",
      sortBy: searchParams.get("sortBy") ?? "createdAt",
      sortOrder: (searchParams.get("sortOrder") as "asc" | "desc") ?? "desc",
    };
  }, [searchParams]);

  // Update params
  const setParams = React.useCallback(
    (newParams: Partial<VehicleSearchParams>) => {
      const current = new URLSearchParams(searchParams.toString());

      // Reset page when filters change
      if (!("page" in newParams)) {
        current.set("page", "1");
      }

      // Update params
      Object.entries(newParams).forEach(([key, value]) => {
        if (value === undefined || value === "" || value === null) {
          current.delete(key);
        } else {
          current.set(key, value);
        }
      });

      router.push(`${pathname}?${current.toString()}`, { scroll: false });
    },
    [router, pathname, searchParams],
  );

  // Reset all filters
  const resetFilters = React.useCallback(() => {
    router.push(pathname, { scroll: false });
  }, [router, pathname]);

  // Check if any filters are active
  const hasActiveFilters = React.useMemo(() => {
    const filterKeys = [
      "q",
      "makeId",
      "modelId",
      "yearMin",
      "yearMax",
      "priceMin",
      "priceMax",
      "condition",
      "bodyType",
      "fuelType",
      "transmission",
      "mileageMax",
      "city",
      "sellerType",
    ];
    return filterKeys.some((key) => searchParams.has(key));
  }, [searchParams]);

  // Count active filters
  const activeFilterCount = React.useMemo(() => {
    const filterKeys = [
      "makeId",
      "modelId",
      "yearMin",
      "priceMin",
      "condition",
      "bodyType",
      "fuelType",
      "transmission",
      "mileageMax",
      "city",
      "sellerType",
    ];
    return filterKeys.filter((key) => searchParams.has(key)).length;
  }, [searchParams]);

  return {
    params,
    setParams,
    resetFilters,
    hasActiveFilters,
    activeFilterCount,
  };
}
```

---

## 🔧 PASO 2: Página Principal

```typescript
// filepath: src/app/(main)/vehiculos/page.tsx
import { Metadata } from "next";
import { Suspense } from "react";
import { SearchLayout } from "@/components/search/SearchLayout";
import { SearchResults } from "@/components/search/SearchResults";
import { SearchFilters } from "@/components/search/SearchFilters";
import { SearchToolbar } from "@/components/search/SearchToolbar";
import { MobileFilterSheet } from "@/components/search/MobileFilterSheet";
import { VehicleCardSkeleton } from "@/components/vehicles/VehicleCardSkeleton";
import { Breadcrumbs } from "@/components/ui/Breadcrumbs";

export const metadata: Metadata = {
  title: "Buscar Vehículos | OKLA",
  description:
    "Encuentra vehículos nuevos y usados en República Dominicana. Filtra por marca, modelo, año, precio y más.",
};

interface SearchPageProps {
  searchParams: Promise<{ [key: string]: string | undefined }>;
}

export default async function SearchPage({ searchParams }: SearchPageProps) {
  const params = await searchParams;

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Breadcrumbs */}
      <div className="bg-white border-b">
        <div className="container py-3">
          <Breadcrumbs
            items={[
              { label: "Inicio", href: "/" },
              { label: "Vehículos" },
            ]}
          />
        </div>
      </div>

      {/* Mobile: Search bar and filter button */}
      <div className="lg:hidden sticky top-16 z-20 bg-white border-b p-4">
        <div className="flex gap-2">
          <Suspense fallback={<div className="h-10 flex-1 bg-gray-100 rounded animate-pulse" />}>
            <MobileSearchBar />
          </Suspense>
          <Suspense fallback={<div className="h-10 w-24 bg-gray-100 rounded animate-pulse" />}>
            <MobileFilterSheet />
          </Suspense>
        </div>
      </div>

      {/* Main content */}
      <div className="container py-6">
        <div className="flex gap-8">
          {/* Desktop sidebar filters */}
          <aside className="hidden lg:block w-64 flex-shrink-0">
            <div className="sticky top-24">
              <Suspense fallback={<FiltersSkeleton />}>
                <SearchFilters />
              </Suspense>
            </div>
          </aside>

          {/* Results area */}
          <main className="flex-1 min-w-0">
            {/* Toolbar */}
            <Suspense fallback={<ToolbarSkeleton />}>
              <SearchToolbar />
            </Suspense>

            {/* Results grid */}
            <div className="mt-6">
              <Suspense fallback={<ResultsSkeleton />}>
                <SearchResults />
              </Suspense>
            </div>
          </main>
        </div>
      </div>
    </div>
  );
}

// Mobile search bar component
function MobileSearchBar() {
  return (
    <input
      type="search"
      placeholder="Buscar vehículos..."
      className="flex-1 h-10 px-4 rounded-lg border border-gray-300 focus:border-primary-500 focus:ring-1 focus:ring-primary-500"
    />
  );
}

// Skeleton components
function FiltersSkeleton() {
  return (
    <div className="space-y-6 animate-pulse">
      {Array.from({ length: 6 }).map((_, i) => (
        <div key={i}>
          <div className="h-4 w-20 bg-gray-200 rounded mb-3" />
          <div className="h-10 bg-gray-200 rounded" />
        </div>
      ))}
    </div>
  );
}

function ToolbarSkeleton() {
  return (
    <div className="flex items-center justify-between animate-pulse">
      <div className="h-6 w-40 bg-gray-200 rounded" />
      <div className="flex gap-3">
        <div className="h-10 w-32 bg-gray-200 rounded" />
        <div className="h-10 w-20 bg-gray-200 rounded" />
      </div>
    </div>
  );
}

function ResultsSkeleton() {
  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
      {Array.from({ length: 9 }).map((_, i) => (
        <VehicleCardSkeleton key={i} />
      ))}
    </div>
  );
}
```

---

## 🔧 PASO 3: SearchFilters Component

```typescript
// filepath: src/components/search/SearchFilters.tsx
"use client";

import * as React from "react";
import { X, RotateCcw } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Select } from "@/components/ui/Select";
import { Input } from "@/components/ui/Input";
import { Label } from "@/components/ui/Label";
import { Checkbox } from "@/components/ui/Checkbox";
import { RadioGroup, RadioGroupItem } from "@/components/ui/RadioGroup";
import { Separator } from "@/components/ui/Separator";
import { useVehicleSearchParams } from "@/lib/hooks/useSearchParams";
import { useMakes, useModels, useCities, useBodyTypes } from "@/lib/hooks/useCatalog";
import { cn, formatNumber } from "@/lib/utils";

const FUEL_TYPES = [
  { value: "gasoline", label: "Gasolina" },
  { value: "diesel", label: "Diésel" },
  { value: "electric", label: "Eléctrico" },
  { value: "hybrid", label: "Híbrido" },
];

const TRANSMISSIONS = [
  { value: "automatic", label: "Automático" },
  { value: "manual", label: "Manual" },
];

const MILEAGE_OPTIONS = [
  { value: "10000", label: "Hasta 10,000 km" },
  { value: "30000", label: "Hasta 30,000 km" },
  { value: "50000", label: "Hasta 50,000 km" },
  { value: "100000", label: "Hasta 100,000 km" },
  { value: "150000", label: "Hasta 150,000 km" },
];

interface SearchFiltersProps {
  className?: string;
  onClose?: () => void;
}

export function SearchFilters({ className, onClose }: SearchFiltersProps) {
  const { params, setParams, resetFilters, hasActiveFilters, activeFilterCount } =
    useVehicleSearchParams();

  const { data: makes } = useMakes();
  const { data: models } = useModels(params.makeId);
  const { data: cities } = useCities();
  const { data: bodyTypes } = useBodyTypes();

  // Reset model when make changes
  React.useEffect(() => {
    if (params.modelId && !params.makeId) {
      setParams({ modelId: undefined });
    }
  }, [params.makeId, params.modelId, setParams]);

  const currentYear = new Date().getFullYear();
  const years = Array.from({ length: 35 }, (_, i) => ({
    value: (currentYear - i).toString(),
    label: (currentYear - i).toString(),
  }));

  return (
    <div className={cn("space-y-6", className)}>
      {/* Header with reset */}
      <div className="flex items-center justify-between">
        <h2 className="font-semibold text-gray-900">Filtros</h2>
        {hasActiveFilters && (
          <Button
            variant="ghost"
            size="sm"
            onClick={resetFilters}
            className="text-gray-500 hover:text-gray-700 -mr-2"
          >
            <RotateCcw size={14} className="mr-1" />
            Limpiar
          </Button>
        )}
      </div>

      <Separator />

      {/* Condition */}
      <FilterSection title="Condición">
        <RadioGroup
          value={params.condition ?? ""}
          onValueChange={(value) =>
            setParams({ condition: value as "new" | "used" | undefined })
          }
        >
          <div className="space-y-2">
            <Label className="flex items-center gap-2 font-normal cursor-pointer">
              <RadioGroupItem value="" />
              Todos
            </Label>
            <Label className="flex items-center gap-2 font-normal cursor-pointer">
              <RadioGroupItem value="new" />
              Nuevo
            </Label>
            <Label className="flex items-center gap-2 font-normal cursor-pointer">
              <RadioGroupItem value="used" />
              Usado
            </Label>
          </div>
        </RadioGroup>
      </FilterSection>

      {/* Make */}
      <FilterSection title="Marca">
        <Select
          value={params.makeId ?? ""}
          onValueChange={(value) =>
            setParams({ makeId: value || undefined, modelId: undefined })
          }
          options={makes?.data.map((m) => ({ value: m.id, label: m.name })) ?? []}
          placeholder="Todas las marcas"
        />
      </FilterSection>

      {/* Model */}
      <FilterSection title="Modelo">
        <Select
          value={params.modelId ?? ""}
          onValueChange={(value) => setParams({ modelId: value || undefined })}
          options={models?.data.map((m) => ({ value: m.id, label: m.name })) ?? []}
          placeholder="Todos los modelos"
          disabled={!params.makeId}
        />
      </FilterSection>

      {/* Year Range */}
      <FilterSection title="Año">
        <div className="grid grid-cols-2 gap-2">
          <Select
            value={params.yearMin ?? ""}
            onValueChange={(value) => setParams({ yearMin: value || undefined })}
            options={years}
            placeholder="Desde"
          />
          <Select
            value={params.yearMax ?? ""}
            onValueChange={(value) => setParams({ yearMax: value || undefined })}
            options={years}
            placeholder="Hasta"
          />
        </div>
      </FilterSection>

      {/* Price Range */}
      <FilterSection title="Precio (RD$)">
        <div className="grid grid-cols-2 gap-2">
          <Input
            type="number"
            placeholder="Mínimo"
            value={params.priceMin ?? ""}
            onChange={(e) => setParams({ priceMin: e.target.value || undefined })}
          />
          <Input
            type="number"
            placeholder="Máximo"
            value={params.priceMax ?? ""}
            onChange={(e) => setParams({ priceMax: e.target.value || undefined })}
          />
        </div>
      </FilterSection>

      {/* Body Type */}
      <FilterSection title="Tipo de carrocería">
        <Select
          value={params.bodyType ?? ""}
          onValueChange={(value) => setParams({ bodyType: value || undefined })}
          options={bodyTypes?.data.map((b) => ({ value: b.slug, label: b.name })) ?? []}
          placeholder="Todos los tipos"
        />
      </FilterSection>

      {/* Fuel Type */}
      <FilterSection title="Combustible">
        <div className="space-y-2">
          {FUEL_TYPES.map((fuel) => (
            <Label
              key={fuel.value}
              className="flex items-center gap-2 font-normal cursor-pointer"
            >
              <Checkbox
                checked={params.fuelType === fuel.value}
                onCheckedChange={(checked) =>
                  setParams({ fuelType: checked ? fuel.value : undefined })
                }
              />
              {fuel.label}
            </Label>
          ))}
        </div>
      </FilterSection>

      {/* Transmission */}
      <FilterSection title="Transmisión">
        <RadioGroup
          value={params.transmission ?? ""}
          onValueChange={(value) =>
            setParams({ transmission: value || undefined })
          }
        >
          <div className="space-y-2">
            <Label className="flex items-center gap-2 font-normal cursor-pointer">
              <RadioGroupItem value="" />
              Todos
            </Label>
            {TRANSMISSIONS.map((trans) => (
              <Label
                key={trans.value}
                className="flex items-center gap-2 font-normal cursor-pointer"
              >
                <RadioGroupItem value={trans.value} />
                {trans.label}
              </Label>
            ))}
          </div>
        </RadioGroup>
      </FilterSection>

      {/* Mileage */}
      <FilterSection title="Kilometraje">
        <Select
          value={params.mileageMax ?? ""}
          onValueChange={(value) => setParams({ mileageMax: value || undefined })}
          options={MILEAGE_OPTIONS}
          placeholder="Sin límite"
        />
      </FilterSection>

      {/* City */}
      <FilterSection title="Ciudad">
        <Select
          value={params.city ?? ""}
          onValueChange={(value) => setParams({ city: value || undefined })}
          options={cities?.data.map((c) => ({ value: c.slug, label: c.name })) ?? []}
          placeholder="Todas las ciudades"
        />
      </FilterSection>

      {/* Seller Type */}
      <FilterSection title="Tipo de vendedor">
        <RadioGroup
          value={params.sellerType ?? ""}
          onValueChange={(value) =>
            setParams({
              sellerType: value as "individual" | "dealer" | undefined,
            })
          }
        >
          <div className="space-y-2">
            <Label className="flex items-center gap-2 font-normal cursor-pointer">
              <RadioGroupItem value="" />
              Todos
            </Label>
            <Label className="flex items-center gap-2 font-normal cursor-pointer">
              <RadioGroupItem value="dealer" />
              Dealers
            </Label>
            <Label className="flex items-center gap-2 font-normal cursor-pointer">
              <RadioGroupItem value="individual" />
              Particulares
            </Label>
          </div>
        </RadioGroup>
      </FilterSection>

      {/* Mobile: Apply button */}
      {onClose && (
        <div className="sticky bottom-0 bg-white pt-4 border-t mt-6">
          <Button className="w-full" onClick={onClose}>
            Ver {activeFilterCount > 0 ? `(${activeFilterCount} filtros)` : "resultados"}
          </Button>
        </div>
      )}
    </div>
  );
}

// Helper component for filter sections
function FilterSection({
  title,
  children,
}: {
  title: string;
  children: React.ReactNode;
}) {
  return (
    <div>
      <h3 className="text-sm font-medium text-gray-700 mb-2">{title}</h3>
      {children}
    </div>
  );
}
```

---

## 🔧 PASO 4: SearchToolbar

```typescript
// filepath: src/components/search/SearchToolbar.tsx
"use client";

import * as React from "react";
import { Grid, List, ChevronDown } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Select } from "@/components/ui/Select";
import { useVehicleSearchParams } from "@/lib/hooks/useSearchParams";
import { useVehicleSearch } from "@/lib/hooks/useVehicles";
import { cn } from "@/lib/utils";

const SORT_OPTIONS = [
  { value: "createdAt_desc", label: "Más recientes" },
  { value: "createdAt_asc", label: "Más antiguos" },
  { value: "price_asc", label: "Precio: menor a mayor" },
  { value: "price_desc", label: "Precio: mayor a menor" },
  { value: "year_desc", label: "Año: más nuevo" },
  { value: "year_asc", label: "Año: más antiguo" },
  { value: "mileage_asc", label: "Menor kilometraje" },
];

export function SearchToolbar() {
  const { params, setParams } = useVehicleSearchParams();
  const { data, isLoading } = useVehicleSearch(params);
  const [viewMode, setViewMode] = React.useState<"grid" | "list">("grid");

  const sortValue = `${params.sortBy}_${params.sortOrder}`;

  const handleSortChange = (value: string) => {
    const [sortBy, sortOrder] = value.split("_");
    setParams({ sortBy, sortOrder: sortOrder as "asc" | "desc" });
  };

  const totalResults = data?.meta.total ?? 0;

  return (
    <div className="flex items-center justify-between gap-4 flex-wrap">
      {/* Results count */}
      <div className="text-sm text-gray-600" data-testid="results-count">
        {isLoading ? (
          <span className="inline-block h-4 w-32 bg-gray-200 rounded animate-pulse" />
        ) : (
          <>
            <span className="font-semibold text-gray-900">
              {totalResults.toLocaleString()}
            </span>{" "}
            vehículo{totalResults !== 1 ? "s" : ""} encontrado
            {totalResults !== 1 ? "s" : ""}
          </>
        )}
      </div>

      {/* Controls */}
      <div className="flex items-center gap-3">
        {/* Sort */}
        <Select
          value={sortValue}
          onValueChange={handleSortChange}
          options={SORT_OPTIONS}
          placeholder="Ordenar por"
          className="w-44"
        />

        {/* View mode toggle */}
        <div className="hidden md:flex items-center border rounded-lg overflow-hidden">
          <Button
            variant={viewMode === "grid" ? "secondary" : "ghost"}
            size="sm"
            onClick={() => setViewMode("grid")}
            className="rounded-none"
            aria-label="Vista de cuadrícula"
          >
            <Grid size={16} />
          </Button>
          <Button
            variant={viewMode === "list" ? "secondary" : "ghost"}
            size="sm"
            onClick={() => setViewMode("list")}
            className="rounded-none"
            aria-label="Vista de lista"
          >
            <List size={16} />
          </Button>
        </div>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 5: SearchResults

```typescript
// filepath: src/components/search/SearchResults.tsx
"use client";

import * as React from "react";
import { VehicleGrid } from "@/components/vehicles/VehicleGrid";
import { Pagination } from "@/components/ui/Pagination";
import { useVehicleSearchParams } from "@/lib/hooks/useSearchParams";
import { useVehicleSearch } from "@/lib/hooks/useVehicles";
import { useFavorites } from "@/lib/hooks/useFavorites";

export function SearchResults() {
  const { params, setParams } = useVehicleSearchParams();
  const { data, isLoading, error } = useVehicleSearch(params);
  const { favorites, toggleFavorite } = useFavorites();

  const handlePageChange = (page: number) => {
    setParams({ page: page.toString() });
    // Scroll to top of results
    window.scrollTo({ top: 0, behavior: "smooth" });
  };

  if (error) {
    return (
      <div className="text-center py-12">
        <p className="text-red-600">Error al cargar vehículos</p>
        <p className="text-gray-500 text-sm mt-1">
          Por favor intenta de nuevo más tarde
        </p>
      </div>
    );
  }

  const vehicles = data?.data ?? [];
  const meta = data?.meta;
  const currentPage = parseInt(params.page ?? "1");

  return (
    <div className="space-y-8">
      <VehicleGrid
        vehicles={vehicles}
        isLoading={isLoading}
        skeletonCount={9}
        favorites={favorites}
        onFavoriteClick={toggleFavorite}
        columns={3}
        emptyMessage="No encontramos vehículos"
        emptyDescription="Intenta ajustar los filtros o ampliar tu búsqueda"
      />

      {/* Pagination */}
      {meta && meta.totalPages > 1 && (
        <div className="flex justify-center pt-4">
          <Pagination
            currentPage={currentPage}
            totalPages={meta.totalPages}
            onPageChange={handlePageChange}
          />
        </div>
      )}
    </div>
  );
}
```

---

## 🔧 PASO 6: MobileFilterSheet

```typescript
// filepath: src/components/search/MobileFilterSheet.tsx
"use client";

import * as React from "react";
import { SlidersHorizontal, X } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetTrigger } from "@/components/ui/Sheet";
import { Badge } from "@/components/ui/Badge";
import { SearchFilters } from "./SearchFilters";
import { useVehicleSearchParams } from "@/lib/hooks/useSearchParams";

export function MobileFilterSheet() {
  const [open, setOpen] = React.useState(false);
  const { activeFilterCount } = useVehicleSearchParams();

  return (
    <Sheet open={open} onOpenChange={setOpen}>
      <SheetTrigger asChild>
        <Button variant="outline" size="default" className="relative">
          <SlidersHorizontal size={16} className="mr-2" />
          Filtros
          {activeFilterCount > 0 && (
            <Badge
              variant="primary"
              size="sm"
              className="absolute -top-2 -right-2 min-w-[20px] h-5 flex items-center justify-center"
            >
              {activeFilterCount}
            </Badge>
          )}
        </Button>
      </SheetTrigger>

      <SheetContent side="right" className="w-full sm:max-w-md p-0">
        <SheetHeader className="p-4 border-b">
          <div className="flex items-center justify-between">
            <SheetTitle>Filtros</SheetTitle>
            <Button
              variant="ghost"
              size="icon"
              onClick={() => setOpen(false)}
              className="-mr-2"
            >
              <X size={20} />
            </Button>
          </div>
        </SheetHeader>

        <div className="p-4 overflow-y-auto max-h-[calc(100vh-140px)]">
          <SearchFilters onClose={() => setOpen(false)} />
        </div>
      </SheetContent>
    </Sheet>
  );
}
```

---

## 🔧 PASO 7: Pagination Component

```typescript
// filepath: src/components/ui/Pagination.tsx
"use client";

import * as React from "react";
import { ChevronLeft, ChevronRight, MoreHorizontal } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

interface PaginationProps {
  currentPage: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  siblingCount?: number;
  className?: string;
}

export function Pagination({
  currentPage,
  totalPages,
  onPageChange,
  siblingCount = 1,
  className,
}: PaginationProps) {
  // Generate page numbers to display
  const pages = React.useMemo(() => {
    const totalNumbers = siblingCount * 2 + 3; // siblings + current + 2 boundaries
    const totalBlocks = totalNumbers + 2; // + 2 for dots

    if (totalPages <= totalBlocks) {
      return Array.from({ length: totalPages }, (_, i) => i + 1);
    }

    const leftSiblingIndex = Math.max(currentPage - siblingCount, 1);
    const rightSiblingIndex = Math.min(currentPage + siblingCount, totalPages);

    const shouldShowLeftDots = leftSiblingIndex > 2;
    const shouldShowRightDots = rightSiblingIndex < totalPages - 1;

    if (!shouldShowLeftDots && shouldShowRightDots) {
      const leftItemCount = 3 + 2 * siblingCount;
      const leftRange = Array.from({ length: leftItemCount }, (_, i) => i + 1);
      return [...leftRange, "dots", totalPages];
    }

    if (shouldShowLeftDots && !shouldShowRightDots) {
      const rightItemCount = 3 + 2 * siblingCount;
      const rightRange = Array.from(
        { length: rightItemCount },
        (_, i) => totalPages - rightItemCount + i + 1
      );
      return [1, "dots", ...rightRange];
    }

    const middleRange = Array.from(
      { length: rightSiblingIndex - leftSiblingIndex + 1 },
      (_, i) => leftSiblingIndex + i
    );
    return [1, "dots", ...middleRange, "dots", totalPages];
  }, [currentPage, totalPages, siblingCount]);

  return (
    <nav
      role="navigation"
      aria-label="Paginación"
      className={cn("flex items-center gap-1", className)}
    >
      {/* Previous */}
      <Button
        variant="outline"
        size="icon"
        onClick={() => onPageChange(currentPage - 1)}
        disabled={currentPage === 1}
        aria-label="Página anterior"
      >
        <ChevronLeft size={16} />
      </Button>

      {/* Page numbers */}
      {pages.map((page, index) => {
        if (page === "dots") {
          return (
            <span
              key={`dots-${index}`}
              className="px-2 text-gray-400"
              aria-hidden
            >
              <MoreHorizontal size={16} />
            </span>
          );
        }

        const pageNum = page as number;
        const isActive = pageNum === currentPage;

        return (
          <Button
            key={pageNum}
            variant={isActive ? "default" : "outline"}
            size="icon"
            onClick={() => onPageChange(pageNum)}
            aria-label={`Página ${pageNum}`}
            aria-current={isActive ? "page" : undefined}
          >
            {pageNum}
          </Button>
        );
      })}

      {/* Next */}
      <Button
        variant="outline"
        size="icon"
        onClick={() => onPageChange(currentPage + 1)}
        disabled={currentPage === totalPages}
        aria-label="Página siguiente"
      >
        <ChevronRight size={16} />
      </Button>
    </nav>
  );
}
```

---

## ✅ VALIDACIÓN

### Tests

```typescript
// filepath: __tests__/app/search.test.tsx
import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import { render } from "@tests/utils/test-utils";
import SearchPage from "@/app/(main)/vehiculos/page";

describe("SearchPage", () => {
  it("renders breadcrumbs", async () => {
    render(await SearchPage({ searchParams: Promise.resolve({}) }));
    expect(screen.getByText("Vehículos")).toBeInTheDocument();
  });

  it("renders results count", async () => {
    render(await SearchPage({ searchParams: Promise.resolve({}) }));
    expect(screen.getByTestId("results-count")).toBeInTheDocument();
  });
});
```

### Ejecutar

```bash
pnpm test app/search

pnpm dev
# Verificar en http://localhost:3000/vehiculos:
# - Filtros funcionan
# - URL se actualiza con cada filtro
# - Resultados se cargan dinámicamente
# - Paginación funciona
# - Mobile: Sheet de filtros
```

---

## 📊 RESUMEN

| Componente             | Archivo                        | Función            |
| ---------------------- | ------------------------------ | ------------------ |
| SearchPage             | `vehiculos/page.tsx`           | Página principal   |
| SearchFilters          | `search/SearchFilters.tsx`     | Panel de filtros   |
| SearchToolbar          | `search/SearchToolbar.tsx`     | Contador + Sort    |
| SearchResults          | `search/SearchResults.tsx`     | Grid + Paginación  |
| MobileFilterSheet      | `search/MobileFilterSheet.tsx` | Filtros mobile     |
| Pagination             | `ui/Pagination.tsx`            | Navegación páginas |
| useVehicleSearchParams | `hooks/useSearchParams.ts`     | URL state          |

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/search.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Search Page - Búsqueda de Vehículos", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/vehiculos");
  });

  test("debe cargar la página con filtros y resultados", async ({ page }) => {
    await expect(page.getByTestId("search-filters")).toBeVisible();
    await expect(page.getByTestId("search-results")).toBeVisible();
    await expect(page.getByTestId("results-count")).toBeVisible();
  });

  test("debe filtrar por marca y actualizar URL", async ({ page }) => {
    await page.getByRole("combobox", { name: /marca/i }).click();
    await page.getByRole("option", { name: "Toyota" }).click();

    await expect(page).toHaveURL(/make=toyota/);
    await expect(page.getByTestId("results-count")).toContainText(/Toyota/);
  });

  test("debe filtrar por rango de precio", async ({ page }) => {
    await page.getByLabel(/precio mínimo/i).fill("500000");
    await page.getByLabel(/precio máximo/i).fill("1500000");

    await expect(page).toHaveURL(/minPrice=500000/);
    await expect(page).toHaveURL(/maxPrice=1500000/);
  });

  test("debe filtrar por año", async ({ page }) => {
    await page.getByRole("combobox", { name: /año desde/i }).click();
    await page.getByRole("option", { name: "2020" }).click();

    await expect(page).toHaveURL(/yearFrom=2020/);
  });

  test("debe ordenar resultados", async ({ page }) => {
    await page.getByRole("combobox", { name: /ordenar/i }).click();
    await page.getByRole("option", { name: /menor precio/i }).click();

    await expect(page).toHaveURL(/sort=price_asc/);
  });

  test("debe navegar con paginación", async ({ page }) => {
    const pagination = page.getByTestId("pagination");
    await expect(pagination).toBeVisible();

    await pagination.getByRole("button", { name: "2" }).click();
    await expect(page).toHaveURL(/page=2/);
  });

  test("debe limpiar todos los filtros", async ({ page }) => {
    // Aplicar filtros primero
    await page.goto("/vehiculos?make=toyota&minPrice=500000");
    await expect(page.getByText("Toyota")).toBeVisible();

    // Limpiar
    await page.getByRole("button", { name: /limpiar filtros/i }).click();
    await expect(page).toHaveURL("/vehiculos");
  });

  test("debe abrir sheet de filtros en mobile", async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto("/vehiculos");

    await page.getByRole("button", { name: /filtros/i }).click();
    await expect(page.getByRole("dialog")).toBeVisible();
  });

  test("debe navegar a detalle al hacer click en vehículo", async ({
    page,
  }) => {
    await page.getByTestId("vehicle-card").first().click();
    await expect(page).toHaveURL(/\/vehiculos\/[a-z0-9-]+/);
  });

  test("debe persistir filtros al navegar atrás", async ({ page }) => {
    await page.goto("/vehiculos?make=toyota");
    await page.getByTestId("vehicle-card").first().click();
    await page.goBack();

    await expect(page).toHaveURL(/make=toyota/);
  });
});
```

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/04-PAGINAS/03-detalle-vehiculo.md`
