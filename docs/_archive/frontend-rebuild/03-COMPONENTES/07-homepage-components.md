---
title: "Componentes del Homepage - Implementación Next.js"
priority: P0
estimated_time: "✅ Implementado"
dependencies: ["design-tokens", "api-client", "react-query"]
status: implemented
last_updated: "2026-01-31"
---

# 🏠 Componentes del Homepage - Next.js

> **Estado:** ✅ IMPLEMENTADO  
> **Ubicación:** `frontend/web-next/src/components/homepage/`  
> **Última actualización:** Enero 31, 2026

---

## 📋 Resumen

Componentes específicos para el homepage de OKLA:

| Componente          | Archivo                     | Estado |
| ------------------- | --------------------------- | ------ |
| HeroCarousel        | `hero-carousel.tsx`         | ✅     |
| FeaturedSection     | `featured-section.tsx`      | ✅     |
| FeaturedListingGrid | `featured-listing-grid.tsx` | ✅     |
| Index (exports)     | `index.ts`                  | ✅     |

---

## 🎠 HeroCarousel

### Descripción

Carousel full-screen para el hero del homepage. Muestra vehículos destacados con auto-play y navegación.

### Ubicación

```
src/components/homepage/hero-carousel.tsx
```

### Props

```typescript
interface HeroCarouselProps {
  vehicles: Vehicle[];
  autoPlayInterval?: number; // default: 5000ms
  showScrollHint?: boolean; // default: true
  className?: string;
}
```

### Features

- ✅ **Auto-play** configurable (default 5 segundos)
- ✅ **Navegación con flechas** (hidden en mobile)
- ✅ **Indicadores (dots)** clickeables
- ✅ **Swipe gestures** en mobile (minSwipeDistance: 50px)
- ✅ **Pausa al interactuar** (resume después de 10s)
- ✅ **Botón play/pause**
- ✅ **Badges por tier** (Premium, Top Dealer, Destacado)
- ✅ **Framer Motion** animaciones de fade
- ✅ **Next.js Image** optimization con priority para slide 0

### Uso

```tsx
import { HeroCarousel } from "@/components/homepage";

<HeroCarousel
  vehicles={heroVehicles}
  autoPlayInterval={5000}
  showScrollHint={false}
/>;
```

### Slide Content

Cada slide muestra:

- Imagen de fondo con gradient overlay
- Badge de tier (si aplica)
- Título: `{year} {make}` + `{model}` en color primary
- Precio formateado
- Specs: mileage • transmission • fuelType
- CTAs: "Ver Detalles" + "Contactar Vendedor"

---

## 📊 FeaturedListingGrid

### Descripción

Grid responsive de vehículos destacados con cards interactivas.

### Ubicación

```
src/components/homepage/featured-listing-grid.tsx
```

### Props

```typescript
interface FeaturedListingGridProps {
  vehicles: Vehicle[];
  columns?: 2 | 3 | 4; // default: 3
  maxItems?: number;
  className?: string;
}
```

### Features

- ✅ **Grid responsive**: 1 col (mobile) → 2 col (md) → 3 col (lg)
- ✅ **Límite de items** configurable via `maxItems`
- ✅ **Favoritos toggle** (corazón con estado local)
- ✅ **Badges de condición**: "Nuevo", "Certificado"
- ✅ **Badges de tier**: Premium, Top Dealer, Destacado
- ✅ **Animaciones staggered** con Framer Motion
- ✅ **Image priority** para primeras 4 cards

### Uso

```tsx
import { FeaturedListingGrid } from "@/components/homepage";

<FeaturedListingGrid vehicles={gridVehicles} maxItems={9} columns={3} />;
```

### Card Content

Cada card muestra:

- Imagen con aspect ratio 4:3
- Favorito button (top-left)
- Tier badge (top-right)
- Condition badge (bottom-left): Nuevo/Certificado
- Título: `{year} {make} {model}`
- Precio en color primary
- Specs grid: mileage • year • location

---

## ↔️ FeaturedSection

### Descripción

Sección con scroll horizontal para categorías de vehículos (Sedanes, SUVs, Camionetas, etc.)

### Ubicación

```
src/components/homepage/featured-section.tsx
```

### Props

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

### Features

- ✅ **Scroll horizontal** con botones de navegación
- ✅ **Detección de posición** (show/hide flechas según scroll)
- ✅ **Snap scroll** en mobile
- ✅ **Accent colors** por categoría (9 colores disponibles)
- ✅ **Barra vertical** de color accent en el título
- ✅ **Link "Ver todos"** con flecha animada
- ✅ **Cards 280-300px** de ancho fijo

### Accent Colors

```typescript
const accentColorClasses = {
  blue: { bg: "bg-blue-500", text: "text-blue-600", border: "border-blue-500" },
  green: {
    bg: "bg-emerald-500",
    text: "text-emerald-600",
    border: "border-emerald-500",
  },
  amber: {
    bg: "bg-amber-500",
    text: "text-amber-600",
    border: "border-amber-500",
  },
  red: { bg: "bg-red-500", text: "text-red-600", border: "border-red-500" },
  purple: {
    bg: "bg-purple-500",
    text: "text-purple-600",
    border: "border-purple-500",
  },
  indigo: {
    bg: "bg-indigo-500",
    text: "text-indigo-600",
    border: "border-indigo-500",
  },
  pink: { bg: "bg-pink-500", text: "text-pink-600", border: "border-pink-500" },
  teal: { bg: "bg-teal-500", text: "text-teal-600", border: "border-teal-500" },
  orange: {
    bg: "bg-orange-500",
    text: "text-orange-600",
    border: "border-orange-500",
  },
};
```

### Uso

```tsx
import { FeaturedSection } from "@/components/homepage";

<FeaturedSection
  title="Sedanes"
  subtitle="Los mejores sedanes del mercado"
  listings={sedanListings}
  viewAllHref="/vehiculos?bodyStyle=sedan"
  accentColor="blue"
/>;
```

---

## 📁 Barrel Export

```typescript
// filepath: src/components/homepage/index.ts
export { default as HeroCarousel } from "./hero-carousel";
export { default as FeaturedSection } from "./featured-section";
export { default as FeaturedListingGrid } from "./featured-listing-grid";
export type { FeaturedListingItem } from "./featured-section";
```

---

## 🔗 Dependencias

### NPM Packages

| Package         | Uso                      |
| --------------- | ------------------------ |
| `framer-motion` | Animaciones              |
| `lucide-react`  | Íconos                   |
| `next/image`    | Optimización de imágenes |
| `next/link`     | Navegación client-side   |

### Componentes UI

| Componente | Ubicación                      |
| ---------- | ------------------------------ |
| `Button`   | `src/components/ui/button.tsx` |
| `Badge`    | `src/components/ui/badge.tsx`  |

### Utilities

| Función          | Ubicación          | Uso                     |
| ---------------- | ------------------ | ----------------------- |
| `cn`             | `src/lib/utils.ts` | Merge classNames        |
| `formatCurrency` | `src/lib/utils.ts` | Formatear precios (RD$) |
| `formatMileage`  | `src/lib/utils.ts` | Formatear kilometraje   |

---

## 🎨 Responsive Breakpoints

| Breakpoint | HeroCarousel     | FeaturedGrid | FeaturedSection |
| ---------- | ---------------- | ------------ | --------------- |
| **Mobile** | Swipe, no arrows | 1 columna    | Snap scroll     |
| **sm**     | Arrows visible   | 1 columna    | Cards 280px     |
| **md**     | Arrows visible   | 2 columnas   | Cards 300px     |
| **lg**     | Full controls    | 3 columnas   | Navigation btns |
| **xl**     | Full controls    | 3-4 columnas | Navigation btns |

---

## 🧪 Test Cases Recomendados

### HeroCarousel

```typescript
describe("HeroCarousel", () => {
  it("renders all slides", () => {});
  it("auto-advances after interval", () => {});
  it("handles swipe left/right", () => {});
  it("pauses on user interaction", () => {});
  it("toggles play/pause", () => {});
  it("shows correct badge for tier", () => {});
});
```

### FeaturedListingGrid

```typescript
describe("FeaturedListingGrid", () => {
  it("respects maxItems prop", () => {});
  it("toggles favorite on click", () => {});
  it("shows condition badges", () => {});
  it("links to correct vehicle URL", () => {});
});
```

### FeaturedSection

```typescript
describe("FeaturedSection", () => {
  it("scrolls on button click", () => {});
  it("hides left arrow at start", () => {});
  it("hides right arrow at end", () => {});
  it("applies correct accent color", () => {});
});
```

---

## 🔍 HeroEnhanced (con SearchBar)

### Descripción

Hero section profesional con barra de búsqueda integrada. Se muestra cuando no hay vehículos de carousel disponibles.

### Ubicación

```
src/components/homepage/hero-enhanced.tsx
```

### Componentes Internos

| Componente       | Descripción                               |
| ---------------- | ----------------------------------------- |
| `AnimatedShapes` | Orbes de gradient animados + grid pattern |
| `HeroSearchBar`  | Barra de búsqueda con 3 dropdowns         |
| `TrustBadgesBar` | Badges de confianza (verificados, etc.)   |

### SearchBar - Campos (Actualizado Enero 31, 2026)

```
┌─────────────────────────────────────────────────────────────────────────┐
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌────────────────────┐ │
│  │   Estado  ▼ │ │   Marca   ▼ │ │  Modelo   ▼ │ │    🔍 Buscar      │ │
│  └─────────────┘ └─────────────┘ └─────────────┘ └────────────────────┘ │
│                                                                         │
│  [SUV] [Sedán] [Camioneta] [Deportivo] [Híbrido] [Eléctrico]           │
└─────────────────────────────────────────────────────────────────────────┘
```

| Campo  | Tipo     | Opciones                                  |
| ------ | -------- | ----------------------------------------- |
| Estado | Dropdown | Nuevo, Recién Importado, Usado            |
| Marca  | Dropdown | Toyota, Honda, Hyundai, Kia, Nissan, etc. |
| Modelo | Dropdown | Dinámico según marca seleccionada         |

### Lógica del Modelo Dinámico

```typescript
// El modelo se resetea al cambiar de marca
const modelsByMake: Record<string, string[]> = {
  Toyota: ['Corolla', 'Camry', 'RAV4', 'Hilux', 'Land Cruiser', ...],
  Honda: ['Civic', 'Accord', 'CR-V', 'HR-V', 'Pilot', ...],
  // ... 8 marcas con 6-8 modelos cada una
};

// Modelo deshabilitado hasta seleccionar marca
disabled={!make}
```

### Quick Filters

Botones de acceso rápido por tipo de carrocería:

- SUV, Sedán, Camioneta, Deportivo, Híbrido, Eléctrico
- Hover: transición a verde OKLA (#00A870)

### Props

```typescript
interface HeroEnhancedProps {
  className?: string;
}
```

### Trust Badges

```typescript
const TRUST_BADGES = [
  { icon: Shield, text: "Vendedores Verificados" },
  { icon: CheckCircle2, text: "Historial Garantizado" },
  { icon: Star, text: "Precios Transparentes" },
];
```

### Uso

```tsx
import { HeroEnhanced } from "@/components/homepage";

// Se usa en homepage cuando no hay carousel vehicles
{
  carouselVehicles.length > 0 ? (
    <HeroCarousel vehicles={carouselVehicles} />
  ) : (
    <HeroEnhanced />
  );
}
```

### Auditoría vs Competencia (Enero 31, 2026)

| Feature            | CarrosRD | SuperCarro | OKLA              |
| ------------------ | -------- | ---------- | ----------------- |
| Layout             | Vertical | Vertical   | **Horizontal** ✅ |
| Estado Nuevo/Usado | ❌       | ✅         | ✅                |
| Marca              | ✅       | ✅         | ✅                |
| Modelo dinámico    | ❌       | ❌         | ✅                |
| Quick Filters      | ❌       | ❌         | ✅                |
| Diseño moderno     | ❌       | ❌         | ✅                |

---

## ✅ Checklist de Implementación

- [x] HeroCarousel con auto-play
- [x] HeroCarousel con swipe gestures
- [x] HeroCarousel con badges por tier
- [x] FeaturedListingGrid con favoritos
- [x] FeaturedListingGrid con badges
- [x] FeaturedSection con scroll horizontal
- [x] FeaturedSection con accent colors
- [x] **HeroEnhanced con SearchBar de 3 campos**
- [x] **Modelo dinámico según marca**
- [x] **Quick Filters con hover verde**
- [x] Barrel export (index.ts)
- [x] Integración con homepage
- [x] Build passing

---

_Última actualización: Enero 31, 2026_
