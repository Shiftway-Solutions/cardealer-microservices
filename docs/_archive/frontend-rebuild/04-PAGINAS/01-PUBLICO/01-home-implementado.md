---
title: "Homepage - Implementación Actual (Next.js)"
priority: P0
estimated_time: "✅ Implementado"
dependencies: ["layout", "vehicle-card", "design-tokens"]
apis: ["/api/homepagesections/homepage"]
status: implemented
last_updated: "2026-01-31"
---

# 🏠 Homepage - Implementación Next.js

> **Estado:** ✅ IMPLEMENTADO  
> **Ubicación:** `frontend/web-next/src/app/page.tsx`  
> **Última actualización:** Enero 31, 2026

---

## 📋 Resumen

La página principal de OKLA está implementada con los siguientes componentes:

| Componente          | Archivo                                             | Estado |
| ------------------- | --------------------------------------------------- | ------ |
| Homepage            | `src/app/page.tsx`                                  | ✅     |
| HeroCarousel        | `src/components/homepage/hero-carousel.tsx`         | ✅     |
| FeaturedListingGrid | `src/components/homepage/featured-listing-grid.tsx` | ✅     |
| FeaturedSection     | `src/components/homepage/featured-section.tsx`      | ✅     |
| useHomepageSections | `src/hooks/use-homepage-sections.ts`                | ✅     |
| homepage-sections   | `src/services/homepage-sections.ts`                 | ✅     |

---

## 🎨 Estructura de la Página

```
┌─────────────────────────────────────────────────────────────────┐
│ NAVBAR                                                          │
├─────────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ HERO CAROUSEL (100vh - 4rem)                                │ │
│ │ • Auto-play 5 segundos                                      │ │
│ │ • Swipe en mobile                                           │ │
│ │ • Badges por tier (Premium, Top Dealer, Destacado)          │ │
│ │ • CTAs: "Ver Detalles" + "Contactar Vendedor"              │ │
│ └─────────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ VEHÍCULOS DESTACADOS (Grid 3 columnas, max 9)               │ │
│ │ ┌────┐ ┌────┐ ┌────┐                                       │ │
│ │ │    │ │    │ │    │                                       │ │
│ │ └────┘ └────┘ └────┘                                       │ │
│ │ ┌────┐ ┌────┐ ┌────┐                                       │ │
│ │ │    │ │    │ │    │                                       │ │
│ │ └────┘ └────┘ └────┘                                       │ │
│ └─────────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ SEDANES (Scroll horizontal) ◀ ──────────────────────── ▶   │ │
│ │ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ...                    │ │
│ │ └────┘ └────┘ └────┘ └────┘ └────┘                         │ │
│ └─────────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────────┤
│ │ SUVs (Scroll horizontal)                                    │ │
├─────────────────────────────────────────────────────────────────┤
│ │ CAMIONETAS (Scroll horizontal)                              │ │
├─────────────────────────────────────────────────────────────────┤
│ │ DEPORTIVOS (Scroll horizontal)                              │ │
├─────────────────────────────────────────────────────────────────┤
│ │ LUJO (Scroll horizontal)                                    │ │
├─────────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ TODO LO QUE NECESITAS (4 cards)                             │ │
│ │ • Encuentra tu Vehículo                                     │ │
│ │ • Vende más Rápido                                          │ │
│ │ • Compra con Confianza                                      │ │
│ │ • Contacto Directo                                          │ │
│ └─────────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ CTA SECTION (bg-primary)                                    │ │
│ │ ¿Listo para vender tu vehículo?                            │ │
│ │ [Publicar Gratis] [Para Dealers]                           │ │
│ └─────────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────────┤
│ FOOTER                                                          │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📡 API Integration

### Endpoint Principal

```
GET /api/homepagesections/homepage
```

### Hook: useHomepageSections

```typescript
// filepath: src/hooks/use-homepage-sections.ts
import { useQuery } from "@tanstack/react-query";

export const useHomepageSections = () => {
  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ["homepage-sections"],
    queryFn: getHomepageSections,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });

  return {
    sections: data || [],
    isLoading,
    error,
    refetch,
    // Named sections for convenience
    carousel: sections.find((s) => s.slug === "carousel"),
    sedanes: sections.find((s) => s.slug === "sedanes"),
    suvs: sections.find((s) => s.slug === "suvs"),
    camionetas: sections.find((s) => s.slug === "camionetas"),
    deportivos: sections.find((s) => s.slug === "deportivos"),
    destacados: sections.find((s) => s.slug === "destacados"),
    lujo: sections.find((s) => s.slug === "lujo"),
  };
};
```

### Service: homepage-sections.ts

```typescript
// filepath: src/services/homepage-sections.ts
export interface HomepageSectionDto {
  id: string;
  name: string;
  slug: string;
  description: string;
  displayOrder: number;
  maxItems: number;
  isActive: boolean;
  icon: string | null;
  accentColor: string | null;
  viewAllHref: string | null;
  layoutType: "Hero" | "Carousel" | "Grid" | "Featured";
  subtitle: string | null;
  vehicles: HomepageVehicleDto[];
}

export interface HomepageVehicleDto {
  id: string;
  name: string;
  make: string;
  model: string;
  year: number;
  price: number;
  mileage: number;
  fuelType: string;
  transmission: string;
  exteriorColor: string;
  bodyStyle: string;
  imageUrl: string;
  imageUrls: string[];
  sortOrder: number;
  isPinned: boolean;
}
```

---

## 🧩 Componentes Implementados

### 1. HeroCarousel

**Ubicación:** `src/components/homepage/hero-carousel.tsx`

**Features:**

- ✅ Auto-play configurable (default: 5 segundos)
- ✅ Navegación con flechas (hidden en mobile)
- ✅ Indicadores (dots) clickeables
- ✅ Swipe gestures en mobile
- ✅ Pausa al interactuar (resume después de 10s)
- ✅ Botón play/pause
- ✅ Badges por tier (Premium, Top Dealer, Destacado)
- ✅ Animaciones con Framer Motion
- ✅ Next.js Image optimization

**Props:**

```typescript
interface HeroCarouselProps {
  vehicles: Vehicle[];
  autoPlayInterval?: number; // default: 5000ms
  showScrollHint?: boolean; // default: true
  className?: string;
}
```

### 2. FeaturedListingGrid

**Ubicación:** `src/components/homepage/featured-listing-grid.tsx`

**Features:**

- ✅ Grid responsive (1-2-3 columnas)
- ✅ Límite configurable de items
- ✅ Cards con favoritos (corazón)
- ✅ Badges de condición (Nuevo, Certificado)
- ✅ Badges de tier (Premium, Top Dealer)
- ✅ Animaciones staggered con Framer Motion
- ✅ Next.js Image con priority para primeras 4

**Props:**

```typescript
interface FeaturedListingGridProps {
  vehicles: Vehicle[];
  columns?: 2 | 3 | 4; // default: 3
  maxItems?: number;
  className?: string;
}
```

### 3. FeaturedSection

**Ubicación:** `src/components/homepage/featured-section.tsx`

**Features:**

- ✅ Scroll horizontal con botones
- ✅ Detección de scroll position (show/hide arrows)
- ✅ Accent color por categoría
- ✅ Link "Ver todos" configurable
- ✅ Snap scroll en mobile
- ✅ Cards con imagen, precio, specs
- ✅ Animaciones de entrada

**Props:**

```typescript
interface FeaturedSectionProps {
  title: string;
  subtitle?: string;
  listings: FeaturedListingItem[];
  viewAllHref?: string; // default: '/vehiculos'
  accentColor?: string; // 'blue' | 'green' | 'amber' | 'red' | etc.
  className?: string;
}

interface FeaturedListingItem {
  id: string;
  title: string;
  price: number;
  mileage: number;
  location: string;
  imageUrl: string;
  category: string;
  year: number;
  make: string;
  model: string;
  fuelType: string;
  transmission: string;
}
```

---

## 🎨 Features Section

4 cards con íconos y descripción:

| Ícono           | Título                | Descripción                                                   |
| --------------- | --------------------- | ------------------------------------------------------------- |
| `Search`        | Encuentra tu Vehículo | Búsqueda avanzada con filtros por marca, modelo, año, precio. |
| `Zap`           | Vende más Rápido      | Publica en minutos y llega a miles de compradores en RD.      |
| `Shield`        | Compra con Confianza  | Historial verificado, fotos reales, vendedores evaluados.     |
| `MessageCircle` | Contacto Directo      | Habla directamente con vendedores. Sin intermediarios.        |

---

## 📱 Estados

### Loading State

```tsx
{
  isLoading && (
    <section className="py-12 bg-gray-50">
      <div className="max-w-7xl mx-auto px-4 text-center">
        <Loader2 className="w-12 h-12 mx-auto animate-spin text-primary mb-4" />
        <p className="text-gray-600">Cargando vehículos...</p>
      </div>
    </section>
  );
}
```

### Error State

```tsx
{
  error && (
    <section className="py-12 bg-red-50">
      <div className="max-w-7xl mx-auto px-4 text-center">
        <p className="text-red-600 mb-2">Error al cargar vehículos</p>
        <p className="text-sm text-red-500">{error}</p>
      </div>
    </section>
  );
}
```

### Fallback (Sin datos del API)

Cuando no hay datos del carousel, muestra un hero estático:

```tsx
<section className="relative overflow-hidden bg-gradient-to-br from-gray-900 to-gray-800 h-[calc(100vh-4rem)]">
  <h1>
    Tu próximo vehículo está en <span className="text-primary">OKLA</span>
  </h1>
  <Button>Explorar Vehículos</Button>
  <Button variant="outline">Vender mi Vehículo</Button>
</section>
```

---

## 🔗 Dependencias

### NPM Packages

- `@tanstack/react-query` - Data fetching
- `framer-motion` - Animaciones
- `lucide-react` - Íconos
- `next/image` - Optimización de imágenes

### Componentes UI

- `Button` - `src/components/ui/button.tsx`
- `Badge` - `src/components/ui/badge.tsx`

### Services

- `apiClient` - `src/lib/api-client.ts`

---

## 🧪 Testing

### Test Cases Recomendados

1. **HeroCarousel**
   - Renderiza vehículos correctamente
   - Auto-advance funciona
   - Swipe cambia slide
   - Play/pause toggle funciona

2. **FeaturedListingGrid**
   - Muestra cantidad correcta de items (maxItems)
   - Toggle de favoritos funciona
   - Links navegan correctamente

3. **FeaturedSection**
   - Scroll buttons funcionan
   - Muestra/oculta flechas según posición
   - Link "Ver todos" tiene href correcto

4. **useHomepageSections**
   - Fetches data on mount
   - Returns correct section slugs
   - Handles loading state
   - Handles error state

---

## 📁 Estructura de Archivos

```
frontend/web-next/src/
├── app/
│   └── page.tsx                          # Homepage principal
├── components/
│   └── homepage/
│       ├── index.ts                      # Barrel export
│       ├── hero-carousel.tsx             # Carousel hero
│       ├── featured-section.tsx          # Sección horizontal
│       └── featured-listing-grid.tsx     # Grid de destacados
├── hooks/
│   └── use-homepage-sections.ts          # Hook React Query
└── services/
    └── homepage-sections.ts              # API client + types
```

---

## ✅ Checklist de Implementación

- [x] Hook `useHomepageSections` con React Query
- [x] Service `homepage-sections.ts` con types y API calls
- [x] Componente `HeroCarousel` con auto-play y swipe
- [x] Componente `FeaturedListingGrid` con favoritos
- [x] Componente `FeaturedSection` con scroll horizontal
- [x] Estados de loading y error
- [x] Fallback hero cuando no hay datos
- [x] Features section (4 cards)
- [x] CTA section con botones
- [x] Build passing ✅

---

_Última actualización: Enero 31, 2026_
