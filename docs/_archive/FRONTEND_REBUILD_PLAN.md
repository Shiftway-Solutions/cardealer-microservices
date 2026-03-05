# 🚀 PLAN DE RECONSTRUCCIÓN FRONTEND - OKLA

> **Inspiración:** CarGurus.com  
> **Fecha de Creación:** Enero 29, 2026  
> **Estado:** ✅ APROBADO - LISTO PARA EJECUTAR  
> **Objetivo:** Frontend profesional, testeable, escalable y performante

---

## ✅ DECISIONES CONFIRMADAS (Enero 29, 2026)

| Decisión            | Opción Elegida              | Implicación                                 |
| ------------------- | --------------------------- | ------------------------------------------- |
| **Framework**       | Next.js 15                  | SSR/SSG para SEO, App Router                |
| **Frontend Actual** | PAUSADO                     | Sin desarrollo paralelo, migración completa |
| **SupportService**  | SÍ crear                    | Backend nuevo requerido (Sprint dedicado)   |
| **Recursos**        | 2 desarrolladores full-time | Timeline de 14 semanas factible             |

### Equipo Asignado

- **Dev 1:** Core components, Auth, Vehicles, User features
- **Dev 2:** Dealer Portal, Admin, Billing, Compliance

---

## 📋 ÍNDICE

1. [Resumen Ejecutivo](#1-resumen-ejecutivo)
2. [Análisis de CarGurus](#2-análisis-de-cargurus)
3. [Stack Tecnológico Recomendado](#3-stack-tecnológico-recomendado)
4. [Arquitectura Propuesta](#4-arquitectura-propuesta)
5. [Estrategia de Testing](#5-estrategia-de-testing)
6. [Migración por Fases](#6-migración-por-fases)
7. [Mapeo de Procesos API](#7-mapeo-de-procesos-api)
8. [Estructura de Carpetas](#8-estructura-de-carpetas)
9. [Design System](#9-design-system)
10. [Checklist Pre-Producción](#10-checklist-pre-producción)

---

## 1. RESUMEN EJECUTIVO

### Estado Actual vs Objetivo

| Métrica               | Actual         | Objetivo        | Mejora        |
| --------------------- | -------------- | --------------- | ------------- |
| **Bundle Size**       | 3.16 MB        | < 500 KB        | 84% reducción |
| **First Contentful**  | ~4s (estimado) | < 1.5s          | 63% mejora    |
| **Test Coverage**     | 5.5%           | > 80%           | +75%          |
| **Lighthouse Score**  | ~50 (estimado) | > 90            | +40 puntos    |
| **Páginas**           | 130 (monolito) | 130 (lazy)      | Code Split    |
| **TypeScript Errors** | Runtime errors | 0 (compile)     | Type Safety   |
| **A11y Compliance**   | Parcial        | WCAG 2.1 AA     | Completo      |
| **SSR/SSG**           | ❌ No          | ✅ Sí (Next.js) | SEO Crítico   |

### Decisión Clave: ¿Migrar o Reconstruir?

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    ANÁLISIS DE OPCIONES                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  OPCIÓN A: REFACTORIZAR GRADUALMENTE (Vite + React)                        │
│  ─────────────────────────────────────────────────────────────────────────  │
│  ✅ Pros:                                                                   │
│     • Menor riesgo inmediato                                               │
│     • Desarrollo continuo mientras se refactoriza                          │
│     • No requiere aprender nueva tecnología                                │
│  ❌ Contras:                                                                │
│     • Sin SSR/SSG = SEO limitado para marketplace                          │
│     • Mantiene deuda técnica subyacente                                    │
│     • 6-8 meses para estabilizar                                           │
│                                                                             │
│  OPCIÓN B: RECONSTRUIR CON NEXT.JS 15 (RECOMENDADO) ⭐                      │
│  ─────────────────────────────────────────────────────────────────────────  │
│  ✅ Pros:                                                                   │
│     • SSR/SSG = SEO óptimo para vehículos (crítico para marketplace)       │
│     • App Router + Server Components = mejor performance                    │
│     • Mejor DX con Turbopack                                               │
│     • CarGurus y competidores usan este patrón                             │
│     • Estructura limpia desde el inicio                                    │
│  ❌ Contras:                                                                │
│     • Curva de aprendizaje App Router                                      │
│     • 3-4 meses para MVP funcional                                         │
│     • Requiere planning cuidadoso                                          │
│                                                                             │
│  🎯 RECOMENDACIÓN: OPCIÓN B (Next.js 15)                                    │
│     Razón: Un marketplace de vehículos NECESITA SEO para que Google        │
│     indexe cada vehículo. Sin SSR, los vehículos no aparecen en búsquedas. │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. ANÁLISIS DE CARGURUS

### Stack Tecnológico de CarGurus (según StackShare)

| Categoría      | Tecnología                     | Uso                   |
| -------------- | ------------------------------ | --------------------- |
| **Frontend**   | React, Bootstrap, Less         | UI Components         |
| **Backend**    | Java (Spring), Node.js, Python | Microservices         |
| **Database**   | MySQL, MongoDB                 | Transaccional + NoSQL |
| **Search**     | Elasticsearch, Lucene          | Búsqueda de vehículos |
| **CDN**        | Fastly                         | Assets y imágenes     |
| **Analytics**  | Google Analytics, Snowplow     | Tracking              |
| **Testing**    | Jest, JUnit                    | Unit + Integration    |
| **Monitoring** | Prometheus, Grafana            | Observabilidad        |
| **CI/CD**      | Jenkins, Bazel                 | Build + Deploy        |
| **Messaging**  | Kafka                          | Event Streaming       |
| **Auth**       | Okta                           | Identity Management   |
| **Charts**     | Highcharts                     | Analytics             |

### Patrones de UX de CarGurus a Replicar

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    PATRONES UX CARGURUS                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1️⃣ BÚSQUEDA PROMINENTE                                                     │
│  ┌─────────────────────────────────────────────────────────────────┐        │
│  │  [Make ▼] [Model ▼] [Price Range ▼] [🔍 Search Cars]            │        │
│  └─────────────────────────────────────────────────────────────────┘        │
│  • Hero con búsqueda principal                                              │
│  • Filtros inteligentes con autocompletado                                  │
│  • Sugerencias basadas en ubicación                                         │
│                                                                             │
│  2️⃣ DEAL RATING (OKLA: "Precio Justo")                                      │
│  ┌─────────────────────────────────────────────────────────────────┐        │
│  │  🟢 GREAT DEAL    Below market price                            │        │
│  │  🟡 GOOD DEAL     Near market price                             │        │
│  │  🟠 FAIR DEAL     At market price                               │        │
│  │  🔴 HIGH PRICE    Above market price                            │        │
│  └─────────────────────────────────────────────────────────────────┘        │
│  • Algoritmo de pricing que compara con mercado                             │
│  • Badge visual prominente en cada listing                                  │
│  • Genera confianza en compradores                                          │
│                                                                             │
│  3️⃣ VEHICLE CARDS OPTIMIZADAS                                               │
│  ┌─────────────────┬─────────────────────────────────────────────┐          │
│  │                 │ 2024 Toyota Camry SE                        │          │
│  │   [IMAGEN]      │ ⭐⭐⭐⭐⭐ (4.8) · 120 reviews                │          │
│  │   [GALLERY]     │ $28,500 · 🟢 Great Deal                     │          │
│  │                 │ 📍 Santo Domingo · 15,000 km                │          │
│  │   ❤️ 📤        │ [Contact Dealer] [Get E-Price]             │          │
│  └─────────────────┴─────────────────────────────────────────────┘          │
│  • Imagen grande con gallery                                                │
│  • Precio + Deal Rating prominentes                                         │
│  • CTAs claros                                                              │
│  • Quick actions (favoritos, compartir)                                     │
│                                                                             │
│  4️⃣ FILTROS STICKY + RESULTADOS                                             │
│  ┌─────────────────┬─────────────────────────────────────────────┐          │
│  │  FILTROS        │  1,234 vehicles found                       │          │
│  │  ────────────   │  ─────────────────────────────────────────  │          │
│  │  Make: Toyota   │  [Card] [Card] [Card]                       │          │
│  │  Model: Camry   │  [Card] [Card] [Card]                       │          │
│  │  Price: $20-40K │  [Card] [Card] [Card]                       │          │
│  │  Year: 2020+    │                                             │          │
│  │  Mileage: <50K  │  [Load More] or infinite scroll             │          │
│  │                 │                                             │          │
│  └─────────────────┴─────────────────────────────────────────────┘          │
│  • Sidebar sticky en desktop                                                │
│  • Bottom sheet en mobile                                                   │
│  • Contador de resultados dinámico                                          │
│  • URL con query params para compartir búsquedas                            │
│                                                                             │
│  5️⃣ VEHICLE DETAIL PAGE                                                     │
│  • Gallery con 360° view                                                    │
│  • Historial de precios (gráfico)                                           │
│  • Market comparison (vs similares)                                         │
│  • Dealer info con reviews                                                  │
│  • Financing calculator                                                     │
│  • Similar vehicles carousel                                                │
│                                                                             │
│  6️⃣ DEALER PROFILES VERIFICADOS                                             │
│  • Badge de verificado ✓                                                    │
│  • Rating agregado                                                          │
│  • Response time                                                            │
│  • Inventario actual                                                        │
│  • Reviews recientes                                                        │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 3. STACK TECNOLÓGICO RECOMENDADO

### Core Framework

| Tecnología       | Versión | Propósito                   | Por qué                        |
| ---------------- | ------- | --------------------------- | ------------------------------ |
| **Next.js**      | 15.x    | Framework React con SSR/SSG | SEO crítico para marketplace   |
| **React**        | 19.x    | UI Library                  | Ya lo usan, mantener expertise |
| **TypeScript**   | 5.6+    | Type Safety                 | Prevenir errores runtime       |
| **Tailwind CSS** | 4.x     | Styling                     | Ya lo usan, performance óptimo |

### UI Components

| Tecnología        | Propósito             | Por qué                           |
| ----------------- | --------------------- | --------------------------------- |
| **shadcn/ui**     | Component Library     | Headless, accesible, customizable |
| **Radix UI**      | Primitivos accesibles | Base de shadcn/ui                 |
| **Framer Motion** | Animaciones           | Ya lo usan, performance           |
| **Lucide Icons**  | Iconos                | Consistente, tree-shakeable       |

### Data Fetching & State

| Tecnología                | Propósito          | Por qué                          |
| ------------------------- | ------------------ | -------------------------------- |
| **TanStack Query**        | Server State       | Ya lo usan, excelente caching    |
| **Zustand**               | Client State       | Ya lo usan, simple y performante |
| **React Hook Form + Zod** | Forms + Validation | Ya lo usan, type-safe            |

### Testing Stack (CRÍTICO)

| Tecnología          | Propósito               | Coverage Target        |
| ------------------- | ----------------------- | ---------------------- |
| **Vitest**          | Unit Testing            | 80%+ de funciones      |
| **Testing Library** | Component Testing       | 80%+ de componentes    |
| **Playwright**      | E2E Testing             | Flujos críticos (100%) |
| **MSW**             | API Mocking             | Ya instalado           |
| **Storybook**       | Component Documentation | Ya instalado           |

### Build & DX

| Tecnología              | Propósito            | Por qué                    |
| ----------------------- | -------------------- | -------------------------- |
| **Turbopack**           | Dev Server (Next.js) | 10x más rápido que Webpack |
| **pnpm**                | Package Manager      | Más rápido, menos disco    |
| **ESLint Flat Config**  | Linting              | Configuración moderna      |
| **Prettier**            | Formatting           | Consistencia               |
| **Husky + lint-staged** | Pre-commit hooks     | Prevenir código malo       |
| **Commitlint**          | Commit messages      | Conventional commits       |

### Monitoring & Analytics

| Tecnología             | Propósito      | Ya tienen                    |
| ---------------------- | -------------- | ---------------------------- |
| **Sentry**             | Error Tracking | ✅ Sí                        |
| **Web Vitals**         | Performance    | ✅ Sí                        |
| **OKLA Analytics SDK** | Event Tracking | ✅ Sí (EventTrackingService) |

### Librerías a ELIMINAR (Duplicadas/Innecesarias)

| Eliminar                | Reemplazar con       | Razón        |
| ----------------------- | -------------------- | ------------ |
| `chart.js` + `recharts` | Solo `recharts`      | Duplicado    |
| `react-icons`           | Solo `lucide-react`  | Duplicado    |
| `@heroicons/react`      | Solo `lucide-react`  | Duplicado    |
| `firebase` (completo)   | Solo auth si lo usan | Bundle bloat |

---

## 4. ARQUITECTURA PROPUESTA

### Estructura Next.js App Router

```
frontend/web-next/
├── app/                           # App Router (Next.js 15)
│   ├── (auth)/                    # Auth route group
│   │   ├── login/page.tsx
│   │   ├── register/page.tsx
│   │   ├── forgot-password/page.tsx
│   │   └── layout.tsx             # Auth layout (no navbar)
│   │
│   ├── (public)/                  # Public route group
│   │   ├── page.tsx               # Home
│   │   ├── vehicles/
│   │   │   ├── page.tsx           # Vehicle listing (SSG + ISR)
│   │   │   └── [slug]/
│   │   │       └── page.tsx       # Vehicle detail (SSG)
│   │   ├── search/page.tsx        # Search (SSR)
│   │   ├── dealers/
│   │   │   └── [slug]/page.tsx    # Dealer profile (SSG)
│   │   └── layout.tsx             # Public layout (navbar + footer)
│   │
│   ├── (protected)/               # Protected route group
│   │   ├── dashboard/page.tsx
│   │   ├── favorites/page.tsx
│   │   ├── alerts/page.tsx
│   │   ├── profile/page.tsx
│   │   ├── messages/page.tsx
│   │   └── layout.tsx             # Protected layout + auth check
│   │
│   ├── dealer/                    # Dealer Portal
│   │   ├── page.tsx               # Dealer dashboard
│   │   ├── inventory/
│   │   │   ├── page.tsx           # Inventory list
│   │   │   ├── new/page.tsx       # Create vehicle
│   │   │   └── [id]/edit/page.tsx # Edit vehicle
│   │   ├── analytics/page.tsx
│   │   ├── crm/page.tsx
│   │   ├── settings/page.tsx
│   │   └── layout.tsx             # Dealer sidebar layout
│   │
│   ├── admin/                     # Admin Portal
│   │   ├── page.tsx               # Admin dashboard
│   │   ├── users/page.tsx
│   │   ├── listings/page.tsx
│   │   ├── pending/page.tsx
│   │   ├── compliance/            # NUEVO: UI para ComplianceService
│   │   │   ├── page.tsx           # Dashboard compliance
│   │   │   ├── dgii-607/page.tsx  # Reporte 607
│   │   │   ├── calendar/page.tsx  # Calendario regulatorio
│   │   │   └── risks/page.tsx     # Risk assessment
│   │   ├── support/               # NUEVO: UI para SupportService
│   │   │   ├── tickets/page.tsx
│   │   │   └── faq/page.tsx
│   │   ├── moderation/            # NUEVO: Queue de moderación
│   │   │   └── queue/page.tsx
│   │   └── layout.tsx             # Admin sidebar layout
│   │
│   ├── seller/                    # Seller Individual Portal
│   │   ├── page.tsx
│   │   ├── profile/page.tsx
│   │   └── layout.tsx
│   │
│   ├── billing/                   # Billing pages
│   │   ├── page.tsx
│   │   ├── plans/page.tsx
│   │   ├── invoices/page.tsx
│   │   ├── checkout/page.tsx
│   │   └── payment/
│   │       ├── azul/page.tsx
│   │       └── success/page.tsx
│   │
│   ├── help/                      # Help Center (público)
│   │   ├── page.tsx               # Help home
│   │   ├── [category]/page.tsx    # Category
│   │   └── articles/[slug]/page.tsx
│   │
│   ├── api/                       # API Routes (BFF pattern)
│   │   ├── auth/[...nextauth]/route.ts
│   │   └── revalidate/route.ts    # ISR revalidation
│   │
│   ├── layout.tsx                 # Root layout
│   ├── loading.tsx                # Global loading
│   ├── error.tsx                  # Global error boundary
│   └── not-found.tsx              # 404 page
│
├── components/
│   ├── ui/                        # shadcn/ui components
│   │   ├── button.tsx
│   │   ├── card.tsx
│   │   ├── dialog.tsx
│   │   ├── dropdown-menu.tsx
│   │   ├── input.tsx
│   │   ├── select.tsx
│   │   ├── skeleton.tsx
│   │   ├── toast.tsx
│   │   └── ...
│   │
│   ├── layout/                    # Layout components
│   │   ├── navbar.tsx
│   │   ├── footer.tsx
│   │   ├── sidebar.tsx
│   │   ├── mobile-nav.tsx
│   │   └── breadcrumbs.tsx
│   │
│   ├── vehicles/                  # Vehicle feature components
│   │   ├── vehicle-card.tsx
│   │   ├── vehicle-grid.tsx
│   │   ├── vehicle-filters.tsx
│   │   ├── vehicle-gallery.tsx
│   │   ├── vehicle-specs.tsx
│   │   ├── deal-badge.tsx         # Precio Justo badge
│   │   ├── price-history.tsx
│   │   └── similar-vehicles.tsx
│   │
│   ├── search/                    # Search feature
│   │   ├── search-bar.tsx
│   │   ├── search-filters.tsx
│   │   ├── search-results.tsx
│   │   └── saved-searches.tsx
│   │
│   ├── dealers/                   # Dealer feature
│   │   ├── dealer-card.tsx
│   │   ├── dealer-badge.tsx
│   │   ├── dealer-reviews.tsx
│   │   └── dealer-inventory.tsx
│   │
│   ├── forms/                     # Form components
│   │   ├── vehicle-form.tsx
│   │   ├── contact-form.tsx
│   │   ├── review-form.tsx
│   │   └── dealer-form.tsx
│   │
│   └── shared/                    # Shared components
│       ├── empty-state.tsx
│       ├── error-boundary.tsx
│       ├── loading-spinner.tsx
│       ├── page-header.tsx
│       └── data-table.tsx
│
├── lib/                           # Utilities
│   ├── api/                       # API client
│   │   ├── client.ts              # Axios instance
│   │   ├── auth.ts                # Auth endpoints
│   │   ├── vehicles.ts            # Vehicle endpoints
│   │   ├── dealers.ts             # Dealer endpoints
│   │   └── types.ts               # API types
│   │
│   ├── hooks/                     # Custom hooks
│   │   ├── use-auth.ts
│   │   ├── use-vehicles.ts
│   │   ├── use-favorites.ts
│   │   ├── use-dealers.ts
│   │   └── use-analytics.ts
│   │
│   ├── store/                     # Zustand stores
│   │   ├── auth-store.ts
│   │   ├── search-store.ts
│   │   └── comparison-store.ts
│   │
│   ├── utils/                     # Utility functions
│   │   ├── format.ts              # Number, date, currency formatters
│   │   ├── validation.ts          # Zod schemas
│   │   └── cn.ts                  # Class names utility
│   │
│   └── constants/                 # Constants
│       ├── routes.ts
│       ├── api-routes.ts
│       └── config.ts
│
├── types/                         # TypeScript types
│   ├── vehicle.ts
│   ├── dealer.ts
│   ├── user.ts
│   ├── payment.ts
│   └── api.ts
│
├── styles/
│   └── globals.css                # Tailwind + custom styles
│
├── public/
│   ├── images/
│   ├── icons/
│   └── fonts/
│
├── __tests__/                     # Test files
│   ├── components/
│   ├── hooks/
│   ├── lib/
│   └── e2e/
│
├── .storybook/                    # Storybook config
├── next.config.ts
├── tailwind.config.ts
├── tsconfig.json
├── vitest.config.ts
├── playwright.config.ts
└── package.json
```

---

## 5. ESTRATEGIA DE TESTING

### Pirámide de Tests

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         PIRÁMIDE DE TESTING OKLA                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│                            ┌─────────┐                                      │
│                           /  E2E (5%)  \                                    │
│                          / Playwright   \                                   │
│                         /  ~30 tests     \                                  │
│                        ─────────────────────                                │
│                       /   Integration (15%)  \                              │
│                      /  Testing Library + MSW  \                            │
│                     /      ~200 tests          \                            │
│                    ───────────────────────────────                          │
│                   /         Unit (80%)            \                         │
│                  /    Vitest + Testing Library     \                        │
│                 /         ~800 tests               \                        │
│                ─────────────────────────────────────                        │
│                                                                             │
│   TOTAL ESTIMADO: ~1,030 tests para 80%+ coverage                          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Flujos E2E Críticos (Playwright)

Estos son los flujos que DEBEN funcionar siempre:

| #   | Flujo                      | Actor     | Pasos                                          | Prioridad |
| --- | -------------------------- | --------- | ---------------------------------------------- | --------- |
| 1   | **Registro completo**      | USR-ANON  | Landing → Register → Verify Email → Dashboard  | 🔴 P0     |
| 2   | **Login + 2FA**            | USR-REG   | Login → 2FA Code → Dashboard                   | 🔴 P0     |
| 3   | **Buscar vehículo**        | USR-ANON  | Home → Search → Filters → Vehicle Detail       | 🔴 P0     |
| 4   | **Contactar vendedor**     | USR-REG   | Vehicle → Contact Form → Message Sent          | 🔴 P0     |
| 5   | **Agregar favorito**       | USR-REG   | Vehicle → ❤️ → Favorites Page                  | 🟠 P1     |
| 6   | **Dealer: Publicar**       | DLR-ADMIN | Dashboard → New Vehicle → Form → Published     | 🔴 P0     |
| 7   | **Dealer: Checkout**       | DLR-NEW   | Pricing → Plan → Checkout → Payment            | 🔴 P0     |
| 8   | **Admin: Aprobar listing** | ADM-MOD   | Pending → Review → Approve                     | 🔴 P0     |
| 9   | **Comparar vehículos**     | USR-REG   | Vehicle A → Compare → Vehicle B → Side by Side | 🟠 P1     |
| 10  | **Crear alerta precio**    | USR-REG   | Favorites → Alert → Price Target → Confirm     | 🟠 P1     |

### Configuración Vitest

```typescript
// vitest.config.ts
import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import path from "path";

export default defineConfig({
  plugins: [react()],
  test: {
    globals: true,
    environment: "jsdom",
    setupFiles: ["./src/test/setup.ts"],
    include: ["**/*.{test,spec}.{ts,tsx}"],
    coverage: {
      provider: "v8",
      reporter: ["text", "json", "html", "lcov"],
      exclude: [
        "node_modules/",
        "src/test/",
        "**/*.d.ts",
        "**/*.config.*",
        "**/types/",
      ],
      thresholds: {
        global: {
          branches: 75,
          functions: 80,
          lines: 80,
          statements: 80,
        },
      },
    },
  },
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
});
```

### Template de Test para Componentes

```typescript
// __tests__/components/vehicles/vehicle-card.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { VehicleCard } from '@/components/vehicles/vehicle-card';

const mockVehicle = {
  id: '123',
  title: '2024 Toyota Camry SE',
  price: 28500,
  dealRating: 'great',
  imageUrl: '/test-image.jpg',
  location: 'Santo Domingo',
  mileage: 15000,
};

describe('VehicleCard', () => {
  it('renders vehicle information correctly', () => {
    render(<VehicleCard vehicle={mockVehicle} />);

    expect(screen.getByText('2024 Toyota Camry SE')).toBeInTheDocument();
    expect(screen.getByText('RD$ 28,500')).toBeInTheDocument();
    expect(screen.getByText('Santo Domingo')).toBeInTheDocument();
  });

  it('shows correct deal badge for great deal', () => {
    render(<VehicleCard vehicle={mockVehicle} />);

    expect(screen.getByText('Precio Excelente')).toHaveClass('bg-green-500');
  });

  it('calls onFavorite when heart icon is clicked', () => {
    const onFavorite = vi.fn();
    render(<VehicleCard vehicle={mockVehicle} onFavorite={onFavorite} />);

    fireEvent.click(screen.getByLabelText('Agregar a favoritos'));
    expect(onFavorite).toHaveBeenCalledWith('123');
  });

  it('navigates to detail page on click', () => {
    render(<VehicleCard vehicle={mockVehicle} />);

    const link = screen.getByRole('link');
    expect(link).toHaveAttribute('href', '/vehicles/123');
  });
});
```

### CI/CD Testing Pipeline

```yaml
# .github/workflows/frontend-tests.yml
name: Frontend Tests

on:
  push:
    paths:
      - "frontend/web-next/**"
  pull_request:
    paths:
      - "frontend/web-next/**"

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: pnpm/action-setup@v2
      - uses: actions/setup-node@v4
        with:
          node-version: "20"
          cache: "pnpm"

      - run: pnpm install
      - run: pnpm test:coverage

      - name: Upload coverage to Codecov
        uses: codecov/codecov-action@v3
        with:
          files: ./coverage/lcov.info
          fail_ci_if_error: true

  e2e-tests:
    runs-on: ubuntu-latest
    needs: unit-tests
    steps:
      - uses: actions/checkout@v4
      - uses: pnpm/action-setup@v2
      - uses: actions/setup-node@v4

      - run: pnpm install
      - run: pnpm exec playwright install --with-deps

      - name: Build
        run: pnpm build

      - name: Run E2E tests
        run: pnpm test:e2e
        env:
          NEXT_PUBLIC_API_URL: ${{ secrets.TEST_API_URL }}

      - uses: actions/upload-artifact@v4
        if: failure()
        with:
          name: playwright-report
          path: playwright-report/
```

---

## 6. MIGRACIÓN POR FASES

### Fase 0: Preparación (1 semana)

```
□ Crear repositorio frontend/web-next
□ Configurar Next.js 15 con App Router
□ Configurar Tailwind CSS 4
□ Instalar y configurar shadcn/ui
□ Configurar ESLint + Prettier + Husky
□ Configurar Vitest + Testing Library
□ Configurar Playwright
□ Configurar Storybook
□ Crear CI/CD pipeline
□ Documentar decision records (ADRs)
```

### Fase 1: Core Components (2 semanas)

```
□ Migrar lib/utils (formatters, validators)
□ Crear componentes shadcn/ui base
□ Crear layout components (navbar, footer, sidebar)
□ Crear empty-state, error-boundary, loading
□ Crear auth store (Zustand)
□ Crear API client con interceptors
□ Tests: 50+ unit tests
```

### Fase 2: Auth Flow (1 semana)

```
□ Login page
□ Register page
□ Forgot/Reset password
□ 2FA verification
□ OAuth callback
□ Protected route middleware
□ E2E: Flujos 1, 2
```

### Fase 3: Vehicle Module (2 semanas)

```
□ Vehicle listing page (SSG + ISR)
□ Vehicle detail page (SSG)
□ Search page (SSR)
□ Vehicle card component
□ Vehicle filters component
□ Deal badge (Precio Justo)
□ Vehicle gallery
□ Similar vehicles
□ E2E: Flujos 3, 4
```

### Fase 4: User Features (1 semana)

```
□ User dashboard
□ Favorites page
□ Alerts page
□ Comparison page
□ Messages page
□ Profile/Settings
□ E2E: Flujos 5, 9, 10
```

### Fase 5: Dealer Portal (2 semanas)

```
□ Dealer dashboard
□ Inventory management
□ Vehicle create/edit forms
□ CSV import
□ Analytics dashboard
□ CRM page
□ Settings page
□ E2E: Flujo 6
```

### Fase 6: Billing & Payments (1 semana)

```
□ Plans page
□ Checkout page
□ AZUL integration
□ Stripe integration
□ Payment success/failure pages
□ Invoices page
□ E2E: Flujo 7
```

### Fase 7: Admin Portal (1 semana)

```
□ Admin dashboard
□ Pending approvals
□ User management
□ Listings management
□ E2E: Flujo 8
```

### Fase 8: Compliance UI (1 semana)

```
□ /admin/compliance/* (7 páginas)
□ /admin/support/* (4 páginas)
□ /admin/moderation/queue
□ /help/* (Centro de ayuda público)
```

### Fase 9: Polish & Launch (1 semana)

```
□ Performance optimization
□ SEO meta tags
□ Sitemap generation
□ robots.txt
□ Analytics integration
□ Sentry error tracking
□ Final E2E test pass
□ Load testing
□ Documentation
```

### Timeline Total: ~14 semanas (3.5 meses)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         TIMELINE DE MIGRACIÓN                               │
│                     (2 Desarrolladores Full-Time)                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  DEVELOPER 1 (Core + User)              DEVELOPER 2 (Dealer + Admin)        │
│  ─────────────────────────              ──────────────────────────          │
│                                                                             │
│  Semana 1       ████ Setup (AMBOS)                                          │
│  Semana 2-3     ████████ Core Components    ████████ SupportService Backend │
│  Semana 4       ████ Auth Flow              ████ Dealer Onboarding          │
│  Semana 5-6     ████████ Vehicles           ████████ Dealer Portal          │
│  Semana 7       ████ User Features          ████ Dealer Analytics           │
│  Semana 8-9     ████████ Search + Filters   ████████ Admin Portal           │
│  Semana 10      ████ Billing UI             ████ Billing Backend            │
│  Semana 11      ████ Messages/Chat          ████ Compliance UI              │
│  Semana 12      ████ Reviews/Ratings        ████ Support/Moderation UI      │
│  Semana 13-14   ████████ Polish & Launch (AMBOS)                            │
│                                                                             │
│  📅 Inicio: Febrero 3, 2026                                                 │
│  📅 MVP (Vehicles + Auth): Marzo 7, 2026 (Semana 5)                         │
│  📅 Beta (80% features): Abril 4, 2026 (Semana 9)                           │
│  📅 Full Launch: Mayo 9, 2026 (Semana 14)                                   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Distribución de Trabajo por Desarrollador

#### Developer 1: Core + Public + User

| Semana | Módulo                    | Páginas                | Tests            |
| ------ | ------------------------- | ---------------------- | ---------------- |
| 1      | Setup conjunto            | -                      | CI/CD            |
| 2-3    | Core Components           | shadcn/ui, layouts     | 100+ unit        |
| 4      | Auth Flow                 | 8 páginas              | 50+ unit, 2 E2E  |
| 5-6    | Vehicles                  | 6 páginas + components | 100+ unit, 3 E2E |
| 7      | User Features             | 8 páginas              | 60+ unit, 2 E2E  |
| 8-9    | Search + Filters          | 4 páginas              | 50+ unit, 2 E2E  |
| 10     | Billing UI                | 6 páginas              | 40+ unit, 1 E2E  |
| 11     | Messages/Chat             | 3 páginas              | 30+ unit         |
| 12     | Reviews                   | 4 páginas              | 30+ unit         |
| 13-14  | Polish, performance, bugs | -                      | E2E final        |

#### Developer 2: Backend Support + Dealer + Admin

| Semana | Módulo                      | Entregables    | Tests           |
| ------ | --------------------------- | -------------- | --------------- |
| 1      | Setup conjunto              | -              | CI/CD           |
| 2-3    | **SupportService Backend**  | API completa   | 50+ unit        |
| 4      | Dealer Onboarding           | 5 páginas      | 40+ unit        |
| 5-6    | Dealer Portal               | 12 páginas     | 80+ unit, 2 E2E |
| 7      | Dealer Analytics            | 5 páginas      | 40+ unit        |
| 8-9    | Admin Portal                | 10 páginas     | 60+ unit, 2 E2E |
| 10     | Billing Backend integration | Webhooks, APIs | 30+ unit        |
| 11     | Compliance UI               | 7 páginas      | 40+ unit        |
| 12     | Support/Moderation UI       | 6 páginas      | 40+ unit        |
| 13-14  | Polish, security, bugs      | -              | E2E final       |

---

## 7. MAPEO DE PROCESOS API

### Procesos 100% Listos (Backend + UI Nueva)

Basado en `PROCESOS_LISTOS_PRODUCCION.md`:

#### Autenticación (27 procesos)

| Código       | Endpoint                          | Componente Next.js           |
| ------------ | --------------------------------- | ---------------------------- |
| AUTH-REG-001 | `POST /api/auth/register`         | `app/(auth)/register/page`   |
| AUTH-LOG-001 | `POST /api/auth/login`            | `app/(auth)/login/page`      |
| AUTH-2FA-001 | `POST /api/TwoFactor/enable`      | `app/(protected)/settings`   |
| AUTH-PWD-001 | `POST /api/auth/forgot-password`  | `app/(auth)/forgot-password` |
| AUTH-EXT-001 | `POST /api/ExternalAuth/callback` | `app/(auth)/callback/[prov]` |

#### Vehículos (45 procesos)

| Código      | Endpoint                   | Componente Next.js                 | Render    |
| ----------- | -------------------------- | ---------------------------------- | --------- |
| VEH-LST-001 | `GET /api/vehicles`        | `app/(public)/vehicles/page`       | SSG + ISR |
| VEH-DET-001 | `GET /api/vehicles/{slug}` | `app/(public)/vehicles/[slug]`     | SSG       |
| VEH-SRH-001 | `GET /api/vehicles/search` | `app/(public)/search/page`         | SSR       |
| VEH-CRT-001 | `POST /api/vehicles`       | `app/dealer/inventory/new`         | Client    |
| FAV-001     | `POST /api/favorites`      | `components/vehicles/vehicle-card` | Client    |

#### Dealers (40 procesos)

| Código  | Endpoint                        | Componente Next.js            |
| ------- | ------------------------------- | ----------------------------- |
| DLR-001 | `GET /api/dealers/me/dashboard` | `app/dealer/page`             |
| DLR-002 | `POST /api/dealers`             | `app/dealer/register/page`    |
| DLR-004 | `GET /api/dealers/{slug}`       | `app/(public)/dealers/[slug]` |
| DLR-006 | `GET /api/dealer-analytics`     | `app/dealer/analytics/page`   |

#### Pagos (35 procesos)

| Código     | Endpoint                          | Componente Next.js              |
| ---------- | --------------------------------- | ------------------------------- |
| PAY-001    | `GET /api/billing/plans`          | `app/billing/plans/page`        |
| PAY-002    | `POST /api/billing/checkout`      | `app/billing/checkout/page`     |
| AZUL-001   | `POST /api/azul/checkout`         | `app/billing/payment/azul/page` |
| STRIPE-001 | `POST /api/stripe/payment-intent` | `app/billing/checkout/page`     |

### Procesos Faltantes (Backend existe, UI nueva)

Basado en `PROCESOS_FALTANTES_UI.md`:

#### Compliance UI (P0 - Crítico)

| Código   | Proceso              | Componente Next.js               |
| -------- | -------------------- | -------------------------------- |
| COMP-001 | Reporte 607 DGII     | `app/admin/compliance/dgii-607`  |
| COMP-002 | Reportes UAF         | `app/admin/compliance/aml`       |
| COMP-003 | Dashboard Compliance | `app/admin/compliance/page`      |
| STR-001  | Suspicious Trans.    | `app/admin/compliance/str/new`   |
| RISK-001 | Risk Assessment      | `app/admin/compliance/risks`     |
| WL-001   | Watchlist Check      | `app/admin/compliance/watchlist` |
| CAL-001  | Regulatory Calendar  | `app/admin/compliance/calendar`  |

#### Support UI (P0 - Backend NO existe)

| Código          | Proceso          | Backend Requerido | Componente Next.js          |
| --------------- | ---------------- | ----------------- | --------------------------- |
| HELP-FAQ-001    | FAQ Management   | SupportService    | `app/admin/support/faq`     |
| HELP-TICKET-001 | Ticket Dashboard | SupportService    | `app/admin/support/tickets` |
| HELP-PUBLIC-001 | Help Center      | SupportService    | `app/help/page`             |

#### Moderation UI (P0 - Backend parcial)

| Código  | Proceso          | Componente Next.js             |
| ------- | ---------------- | ------------------------------ |
| MOD-001 | Moderation Queue | `app/admin/moderation/queue`   |
| MOD-002 | Content Reports  | `app/admin/moderation/reports` |

---

## 8. ESTRUCTURA DE CARPETAS DETALLADA

```
frontend/web-next/
│
├── app/                                # Next.js App Router
│   │
│   ├── (auth)/                         # 🔓 Auth route group (no navbar)
│   │   ├── login/
│   │   │   ├── page.tsx
│   │   │   └── login-form.tsx
│   │   ├── register/
│   │   │   ├── page.tsx
│   │   │   └── register-form.tsx
│   │   ├── forgot-password/
│   │   ├── reset-password/
│   │   ├── verify-email/
│   │   ├── verify-2fa/
│   │   ├── callback/
│   │   │   └── [provider]/page.tsx
│   │   └── layout.tsx
│   │
│   ├── (public)/                       # 🌐 Public pages (SSG/SSR)
│   │   ├── page.tsx                    # Home
│   │   ├── vehicles/
│   │   │   ├── page.tsx                # Listing (ISR 1h)
│   │   │   ├── [slug]/
│   │   │   │   ├── page.tsx            # Detail (SSG)
│   │   │   │   └── loading.tsx
│   │   │   ├── map/page.tsx
│   │   │   └── compare/page.tsx
│   │   ├── search/
│   │   │   └── page.tsx                # SSR con query params
│   │   ├── dealers/
│   │   │   └── [slug]/page.tsx         # Dealer profile (SSG)
│   │   ├── sellers/
│   │   │   └── [id]/page.tsx           # Seller profile
│   │   ├── about/page.tsx
│   │   ├── pricing/page.tsx
│   │   ├── faq/page.tsx
│   │   ├── contact/page.tsx
│   │   ├── terms/page.tsx
│   │   ├── privacy/page.tsx
│   │   └── layout.tsx                  # Navbar + Footer
│   │
│   ├── (protected)/                    # 🔐 User authenticated
│   │   ├── dashboard/page.tsx
│   │   ├── favorites/page.tsx
│   │   ├── alerts/page.tsx
│   │   ├── comparison/page.tsx
│   │   ├── messages/
│   │   │   ├── page.tsx
│   │   │   └── [conversationId]/page.tsx
│   │   ├── profile/
│   │   │   ├── page.tsx
│   │   │   └── settings/page.tsx
│   │   ├── privacy-center/page.tsx     # ARCO compliance
│   │   ├── my-data/page.tsx
│   │   └── layout.tsx                  # Auth check middleware
│   │
│   ├── dealer/                         # 🏢 Dealer Portal
│   │   ├── page.tsx                    # Dashboard
│   │   ├── inventory/
│   │   │   ├── page.tsx                # List
│   │   │   ├── new/page.tsx            # Create
│   │   │   └── [id]/
│   │   │       └── edit/page.tsx       # Edit
│   │   ├── analytics/
│   │   │   ├── page.tsx                # Overview
│   │   │   ├── inventory/page.tsx      # Inventory analytics
│   │   │   └── leads/page.tsx          # Lead funnel
│   │   ├── crm/
│   │   │   ├── page.tsx                # CRM dashboard
│   │   │   └── [leadId]/page.tsx       # Lead detail
│   │   ├── conversations/page.tsx
│   │   ├── appointments/page.tsx
│   │   ├── employees/
│   │   │   ├── page.tsx                # List
│   │   │   └── [id]/permissions/page.tsx
│   │   ├── profile/edit/page.tsx
│   │   ├── billing/page.tsx
│   │   ├── settings/page.tsx
│   │   ├── onboarding/
│   │   │   ├── page.tsx
│   │   │   ├── documents/page.tsx
│   │   │   └── payment/page.tsx
│   │   └── layout.tsx                  # Dealer sidebar
│   │
│   ├── seller/                         # 👤 Seller Individual
│   │   ├── page.tsx
│   │   ├── create/page.tsx
│   │   ├── profile/page.tsx
│   │   └── layout.tsx
│   │
│   ├── admin/                          # 🛡️ Admin Portal
│   │   ├── page.tsx                    # Dashboard
│   │   ├── users/page.tsx
│   │   ├── listings/page.tsx
│   │   ├── pending/page.tsx
│   │   ├── categories/page.tsx
│   │   ├── kyc/
│   │   │   ├── page.tsx                # Queue
│   │   │   └── [profileId]/page.tsx    # Review
│   │   ├── roles/
│   │   │   ├── page.tsx
│   │   │   └── [id]/page.tsx
│   │   ├── compliance/                 # 📋 NUEVO: Compliance UI
│   │   │   ├── page.tsx                # Dashboard
│   │   │   ├── dgii-607/page.tsx
│   │   │   ├── aml/page.tsx
│   │   │   ├── calendar/page.tsx
│   │   │   ├── risks/page.tsx
│   │   │   ├── watchlist/page.tsx
│   │   │   └── str/
│   │   │       └── new/page.tsx
│   │   ├── support/                    # 🎫 NUEVO: Support UI
│   │   │   ├── tickets/
│   │   │   │   ├── page.tsx
│   │   │   │   └── [id]/page.tsx
│   │   │   └── faq/page.tsx
│   │   ├── moderation/                 # ⚖️ NUEVO: Moderation
│   │   │   ├── queue/page.tsx
│   │   │   └── reports/page.tsx
│   │   ├── audit/page.tsx
│   │   ├── reports/page.tsx
│   │   ├── settings/page.tsx
│   │   ├── ml/page.tsx
│   │   └── layout.tsx                  # Admin sidebar
│   │
│   ├── billing/                        # 💳 Billing
│   │   ├── page.tsx
│   │   ├── plans/page.tsx
│   │   ├── checkout/page.tsx
│   │   ├── invoices/page.tsx
│   │   ├── payment-methods/page.tsx
│   │   └── payment/
│   │       ├── azul/
│   │       │   ├── page.tsx
│   │       │   ├── approved/page.tsx
│   │       │   ├── declined/page.tsx
│   │       │   └── cancelled/page.tsx
│   │       └── success/page.tsx
│   │
│   ├── help/                           # ❓ Help Center (público)
│   │   ├── page.tsx
│   │   ├── [category]/page.tsx
│   │   ├── articles/
│   │   │   └── [slug]/page.tsx
│   │   └── tickets/page.tsx            # Mis tickets (auth)
│   │
│   ├── kyc/                            # 🆔 KYC Flow
│   │   ├── page.tsx
│   │   ├── verify/page.tsx
│   │   ├── status/page.tsx
│   │   └── biometric/page.tsx
│   │
│   ├── reviews/                        # ⭐ Reviews
│   │   └── write/
│   │       ├── dealer/[id]/page.tsx
│   │       └── seller/[id]/page.tsx
│   │
│   ├── api/                            # API Routes
│   │   ├── revalidate/route.ts
│   │   └── og/route.tsx                # Open Graph images
│   │
│   ├── layout.tsx                      # Root layout
│   ├── loading.tsx
│   ├── error.tsx
│   ├── not-found.tsx
│   └── sitemap.ts                      # Dynamic sitemap
│
├── components/
│   ├── ui/                             # shadcn/ui components (~40)
│   ├── layout/                         # Layout components (~10)
│   ├── vehicles/                       # Vehicle components (~15)
│   ├── search/                         # Search components (~8)
│   ├── dealers/                        # Dealer components (~10)
│   ├── forms/                          # Form components (~12)
│   ├── charts/                         # Chart components (~8)
│   ├── admin/                          # Admin components (~10)
│   └── shared/                         # Shared components (~15)
│
├── lib/
│   ├── api/
│   ├── hooks/
│   ├── store/
│   ├── utils/
│   └── validations/
│
├── types/
│   ├── api/
│   ├── entities/
│   └── forms/
│
├── styles/
│   └── globals.css
│
├── public/
│   ├── images/
│   ├── icons/
│   └── fonts/
│
└── __tests__/
    ├── components/
    ├── hooks/
    ├── lib/
    └── e2e/
```

---

## 9. DESIGN SYSTEM

### Tokens de Diseño

```css
/* styles/globals.css */
@tailwind base;
@tailwind components;
@tailwind utilities;

@layer base {
  :root {
    /* Brand Colors - OKLA */
    --brand-primary: 220 90% 56%; /* Blue */
    --brand-secondary: 142 76% 36%; /* Green */
    --brand-accent: 38 92% 50%; /* Orange */

    /* Deal Rating Colors */
    --deal-great: 142 76% 36%; /* Green */
    --deal-good: 142 70% 45%; /* Light Green */
    --deal-fair: 38 92% 50%; /* Orange */
    --deal-high: 0 84% 60%; /* Red */

    /* Semantic Colors */
    --background: 0 0% 100%;
    --foreground: 222 47% 11%;
    --muted: 210 40% 96%;
    --muted-foreground: 215 16% 47%;
    --border: 214 32% 91%;
    --input: 214 32% 91%;
    --ring: 220 90% 56%;

    /* Component specific */
    --card: 0 0% 100%;
    --card-foreground: 222 47% 11%;
    --popover: 0 0% 100%;
    --popover-foreground: 222 47% 11%;

    /* Status Colors */
    --destructive: 0 84% 60%;
    --destructive-foreground: 0 0% 100%;
    --success: 142 76% 36%;
    --success-foreground: 0 0% 100%;
    --warning: 38 92% 50%;
    --warning-foreground: 0 0% 100%;

    /* Radius */
    --radius: 0.5rem;
  }

  .dark {
    --background: 222 47% 11%;
    --foreground: 210 40% 98%;
    /* ... dark mode tokens */
  }
}
```

### Componentes Base (shadcn/ui)

```bash
# Instalar componentes shadcn/ui necesarios
npx shadcn@latest init

npx shadcn@latest add button
npx shadcn@latest add card
npx shadcn@latest add input
npx shadcn@latest add select
npx shadcn@latest add dialog
npx shadcn@latest add dropdown-menu
npx shadcn@latest add tabs
npx shadcn@latest add table
npx shadcn@latest add form
npx shadcn@latest add toast
npx shadcn@latest add skeleton
npx shadcn@latest add badge
npx shadcn@latest add avatar
npx shadcn@latest add sheet
npx shadcn@latest add accordion
npx shadcn@latest add alert
npx shadcn@latest add alert-dialog
npx shadcn@latest add calendar
npx shadcn@latest add checkbox
npx shadcn@latest add command
npx shadcn@latest add label
npx shadcn@latest add pagination
npx shadcn@latest add popover
npx shadcn@latest add progress
npx shadcn@latest add radio-group
npx shadcn@latest add separator
npx shadcn@latest add slider
npx shadcn@latest add switch
npx shadcn@latest add textarea
npx shadcn@latest add tooltip
```

### Componente: Deal Badge (Precio Justo)

```tsx
// components/vehicles/deal-badge.tsx
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "@/lib/utils";

const dealBadgeVariants = cva(
  "inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-sm font-semibold",
  {
    variants: {
      rating: {
        great: "bg-green-500 text-white",
        good: "bg-green-400 text-white",
        fair: "bg-orange-500 text-white",
        high: "bg-red-500 text-white",
        unknown: "bg-gray-400 text-white",
      },
    },
    defaultVariants: {
      rating: "unknown",
    },
  },
);

const ratingLabels = {
  great: "Precio Excelente",
  good: "Buen Precio",
  fair: "Precio Justo",
  high: "Precio Alto",
  unknown: "Sin Evaluar",
} as const;

interface DealBadgeProps extends VariantProps<typeof dealBadgeVariants> {
  className?: string;
  showIcon?: boolean;
}

export function DealBadge({
  rating,
  className,
  showIcon = true,
}: DealBadgeProps) {
  return (
    <span className={cn(dealBadgeVariants({ rating }), className)}>
      {showIcon && <span className="text-xs">●</span>}
      {ratingLabels[rating || "unknown"]}
    </span>
  );
}
```

### Componente: Vehicle Card

```tsx
// components/vehicles/vehicle-card.tsx
import Image from "next/image";
import Link from "next/link";
import { Heart, MapPin, Share2 } from "lucide-react";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { DealBadge } from "./deal-badge";
import { formatCurrency, formatNumber } from "@/lib/utils/format";
import type { Vehicle } from "@/types/entities/vehicle";

interface VehicleCardProps {
  vehicle: Vehicle;
  onFavorite?: (id: string) => void;
  onShare?: (id: string) => void;
  isFavorite?: boolean;
}

export function VehicleCard({
  vehicle,
  onFavorite,
  onShare,
  isFavorite = false,
}: VehicleCardProps) {
  const {
    id,
    slug,
    title,
    price,
    dealRating,
    images,
    location,
    mileage,
    year,
  } = vehicle;

  return (
    <Card className="group overflow-hidden transition-shadow hover:shadow-lg">
      {/* Image Container */}
      <div className="relative aspect-[4/3] overflow-hidden">
        <Link href={`/vehicles/${slug}`}>
          <Image
            src={images[0]?.url || "/images/placeholder-vehicle.jpg"}
            alt={title}
            fill
            className="object-cover transition-transform group-hover:scale-105"
            sizes="(max-width: 768px) 100vw, (max-width: 1200px) 50vw, 33vw"
          />
        </Link>

        {/* Quick Actions */}
        <div className="absolute right-2 top-2 flex gap-2">
          <Button
            size="icon"
            variant="secondary"
            className="h-8 w-8 rounded-full bg-white/90 hover:bg-white"
            onClick={() => onFavorite?.(id)}
            aria-label={
              isFavorite ? "Quitar de favoritos" : "Agregar a favoritos"
            }
          >
            <Heart
              className={cn(
                "h-4 w-4",
                isFavorite && "fill-red-500 text-red-500",
              )}
            />
          </Button>
          <Button
            size="icon"
            variant="secondary"
            className="h-8 w-8 rounded-full bg-white/90 hover:bg-white"
            onClick={() => onShare?.(id)}
            aria-label="Compartir"
          >
            <Share2 className="h-4 w-4" />
          </Button>
        </div>

        {/* Deal Badge */}
        <div className="absolute bottom-2 left-2">
          <DealBadge rating={dealRating} />
        </div>

        {/* Image Counter */}
        {images.length > 1 && (
          <span className="absolute bottom-2 right-2 rounded bg-black/60 px-2 py-0.5 text-xs text-white">
            1/{images.length}
          </span>
        )}
      </div>

      {/* Content */}
      <CardContent className="p-4">
        <Link href={`/vehicles/${slug}`} className="block">
          <h3 className="font-semibold text-gray-900 line-clamp-1 group-hover:text-blue-600">
            {title}
          </h3>

          <p className="mt-1 text-2xl font-bold text-gray-900">
            {formatCurrency(price)}
          </p>

          <div className="mt-2 flex items-center gap-4 text-sm text-gray-500">
            <span className="flex items-center gap-1">
              <MapPin className="h-4 w-4" />
              {location.city}
            </span>
            <span>{formatNumber(mileage)} km</span>
            <span>{year}</span>
          </div>
        </Link>

        {/* CTA */}
        <div className="mt-4 flex gap-2">
          <Button asChild className="flex-1">
            <Link href={`/vehicles/${slug}`}>Ver Detalles</Link>
          </Button>
          <Button variant="outline" className="flex-1">
            Contactar
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}
```

---

## 10. CHECKLIST PRE-PRODUCCIÓN

### Antes de Cada Merge a Main

```
□ Todos los tests pasan (unit + integration + e2e)
□ Coverage > 80%
□ No hay errores de TypeScript
□ No hay warnings de ESLint
□ Lighthouse Score > 90
□ Bundle size < 500KB inicial
□ Accessibility audit passed
□ Mobile responsive verificado
□ i18n strings actualizadas
□ API types sincronizados con backend
```

### Antes de Deploy a Producción

```
□ Feature flags configurados
□ Environment variables validadas
□ Sentry configurado
□ Analytics tracking verificado
□ SEO meta tags completas
□ Sitemap generado
□ robots.txt actualizado
□ CDN cache configurado
□ Backup de DB antes de migraciones
□ Rollback plan documentado
```

### Monitoreo Post-Deploy

```
□ Sentry: Sin nuevos errores críticos
□ Web Vitals: LCP < 2.5s, FID < 100ms, CLS < 0.1
□ API: Latencia < 200ms p95
□ Uptime: 99.9%
□ User feedback: Sin reportes críticos
```

---

## 📝 NOTAS FINALES

### Preguntas Clave a Responder Antes de Comenzar

1. **¿Mantener Vite o migrar a Next.js?**
   - Recomendación: Next.js para SEO de marketplace

2. **¿Desarrollo paralelo o pausa del frontend actual?**
   - Recomendación: Desarrollo paralelo con feature freeze en el viejo

3. **¿Prioridad de módulos?**
   - Recomendación: Auth → Vehicles → User → Dealer → Admin

4. **¿Quién implementará SupportService backend?**
   - Es el único servicio sin backend

5. **¿Recursos disponibles?**
   - Timeline de 14 semanas asume 1-2 desarrolladores full-time

### Riesgos Identificados

| Riesgo                    | Probabilidad | Impacto | Mitigación                   |
| ------------------------- | ------------ | ------- | ---------------------------- |
| Curva aprendizaje Next.js | Media        | Medio   | Capacitación previa 1 semana |
| Retrasos por bugs backend | Media        | Alto    | Pruebas E2E tempranas        |
| Scope creep               | Alta         | Alto    | Priorización estricta        |
| Performance issues        | Baja         | Alto    | Lighthouse CI en cada PR     |

### Próximos Pasos Inmediatos

1. ☐ Revisar y aprobar este plan
2. ☐ Decidir Next.js vs Vite refactor
3. ☐ Asignar recursos (personas + tiempo)
4. ☐ Crear repositorio `frontend/web-next`
5. ☐ Configurar proyecto base (Fase 0)
6. ☐ Iniciar desarrollo de Fase 1

---

**Documento preparado por:** GitHub Copilot  
**Fecha:** Enero 29, 2026  
**Estado:** ✅ APROBADO

---

## 🚀 PRÓXIMOS PASOS INMEDIATOS (Semana 1)

### Día 1-2: Setup del Proyecto

```bash
# 1. Crear nuevo directorio Next.js
cd frontend
npx create-next-app@latest web-next --typescript --tailwind --eslint --app --src-dir --import-alias "@/*"

# 2. Instalar dependencias core
cd web-next
pnpm add @tanstack/react-query zustand react-hook-form @hookform/resolvers zod axios
pnpm add lucide-react framer-motion clsx tailwind-merge date-fns

# 3. Instalar shadcn/ui
npx shadcn@latest init

# 4. Instalar dependencias de testing
pnpm add -D vitest @vitest/coverage-v8 @vitest/ui @testing-library/react @testing-library/jest-dom jsdom
pnpm add -D @playwright/test msw

# 5. Instalar Storybook
npx storybook@latest init

# 6. Configurar pre-commit hooks
pnpm add -D husky lint-staged @commitlint/cli @commitlint/config-conventional
```

### Día 3-4: Configuración Base

```
□ Configurar path aliases en tsconfig.json
□ Configurar Vitest con jsdom
□ Configurar Playwright
□ Crear estructura de carpetas
□ Configurar ESLint strict mode
□ Configurar Prettier
□ Crear GitHub Actions workflow
□ Instalar componentes shadcn/ui base
```

### Día 5: Primer Componente + Test

```
□ Crear Button component
□ Crear Card component
□ Crear Input component
□ Escribir tests para cada uno
□ Crear stories en Storybook
□ Verificar CI/CD pipeline
```

---

## 📊 MÉTRICAS DE ÉXITO

| Métrica                  | Target Semana 5 (MVP) | Target Semana 14 (Launch) |
| ------------------------ | --------------------- | ------------------------- |
| Test Coverage            | > 60%                 | > 80%                     |
| Lighthouse Performance   | > 80                  | > 90                      |
| Lighthouse Accessibility | > 90                  | > 95                      |
| Bundle Size (initial)    | < 300 KB              | < 200 KB                  |
| First Contentful Paint   | < 2s                  | < 1.5s                    |
| E2E Tests Passing        | 10/10                 | 30/30                     |
| TypeScript Errors        | 0                     | 0                         |

---

_Este plan está APROBADO y listo para comenzar la ejecución._
_Fecha de inicio programada: Febrero 3, 2026_
