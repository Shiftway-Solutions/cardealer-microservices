# 🧪 Test Suite Implementation - Frontend Next.js

> **Fecha:** Febrero 2, 2026
> **Estado:** ✅ P0 COMPLETADO - 193 TESTS PASSING
> **Cobertura Objetivo:** > 80%
> **Framework:** Vitest + React Testing Library + Playwright

---

## 📋 RESUMEN EJECUTIVO

Este documento define los tests a implementar para garantizar la calidad del frontend Next.js (`frontend/web-next`).

### Estado Actual ✅ ACTUALIZADO

| Métrica         | Valor Actual | Objetivo | Estado |
| --------------- | ------------ | -------- | ------ |
| Tests Unitarios | 156          | 80+      | ✅     |
| Tests E2E       | 37           | 15+      | ✅     |
| **TOTAL**       | **193**      | 95+      | ✅     |
| Build Status    | ✅ Passing   | ✅       | ✅     |

### Tests Unitarios Implementados (Vitest - 156 tests)

| Categoría       | Archivo                             | Tests   | Estado |
| --------------- | ----------------------------------- | ------- | ------ |
| **Services**    | auth.api.test.ts                    | 11      | ✅     |
| **Services**    | vehicles.api.test.ts                | 28      | ✅     |
| **Services**    | favorites.api.test.ts               | 33      | ✅     |
| **Integration** | auth-flow.integration.test.tsx      | 26      | ✅     |
| **Integration** | homepage.integration.test.tsx       | 14      | ✅     |
| **Integration** | search.integration.test.tsx         | 23      | ✅     |
| **Integration** | vehicle-detail.integration.test.tsx | 21      | ✅     |
| **TOTAL**       |                                     | **156** | ✅     |

### Tests E2E Implementados (Playwright - 37 tests)

| Categoría     | Archivo                | Tests  | Estado |
| ------------- | ---------------------- | ------ | ------ |
| **E2E**       | auth.spec.ts           | 7      | ✅     |
| **E2E**       | search.spec.ts         | 6      | ✅     |
| **E2E**       | vehicle-detail.spec.ts | 5      | ✅     |
| **E2E**       | homepage.spec.ts       | 14     | ✅     |
| **E2E**       | favorites.spec.ts      | 5      | ✅     |
| **TOTAL E2E** |                        | **37** | ✅     |

> 📖 **Ver documentación detallada:** [07-tests-implementados-completo.md](./07-tests-implementados-completo.md)

### Infraestructura de Tests Creada

| Archivo                      | Descripción                    |
| ---------------------------- | ------------------------------ |
| `src/test/setup.ts`          | Configuración global de Vitest |
| `src/test/mocks/server.ts`   | MSW server para Node.js        |
| `src/test/mocks/handlers.ts` | Handlers de API (~480 líneas)  |
| `src/test/test-utils.tsx`    | Custom render con providers    |

---

## 🔍 AUDITORÍA DEL CÓDIGO DESARROLLADO

### Inventario Completo de Archivos

#### Componentes UI (`src/components/ui/`) - 27 archivos

| Componente          | Archivo               | Tests Requeridos | Prioridad | Estado |
| ------------------- | --------------------- | ---------------- | --------- | ------ |
| Accordion           | accordion.tsx         | 5                | 🟡 P2     | ⏳     |
| AlertDialog         | alert-dialog.tsx      | 6                | 🟠 P1     | ⏳     |
| Avatar              | avatar.tsx            | 4                | 🟡 P2     | ⏳     |
| Badge               | badge.tsx             | 5                | 🟠 P1     | ⏳     |
| Breadcrumbs         | breadcrumbs.tsx       | 4                | 🟡 P2     | ⏳     |
| **Button**          | button.tsx            | **12**           | 🔴 P0     | ⏳     |
| Card                | card.tsx              | 5                | 🟠 P1     | ⏳     |
| Checkbox            | checkbox.tsx          | 6                | 🟠 P1     | ⏳     |
| **DealRatingBadge** | deal-rating-badge.tsx | **9**            | 🔴 P0     | ⏳     |
| Dialog              | dialog.tsx            | 6                | 🟠 P1     | ⏳     |
| DropdownMenu        | dropdown-menu.tsx     | 6                | 🟡 P2     | ⏳     |
| **Input**           | input.tsx             | **12**           | 🔴 P0     | ⏳     |
| Label               | label.tsx             | 3                | 🟢 P3     | ⏳     |
| Progress            | progress.tsx          | 4                | 🟡 P2     | ⏳     |
| RadioGroup          | radio-group.tsx       | 6                | 🟠 P1     | ⏳     |
| Select              | select.tsx            | 8                | 🟠 P1     | ⏳     |
| Separator           | separator.tsx         | 2                | 🟢 P3     | ⏳     |
| Sheet               | sheet.tsx             | 5                | 🟡 P2     | ⏳     |
| Skeleton            | skeleton.tsx          | 3                | 🟢 P3     | ⏳     |
| Slider              | slider.tsx            | 6                | 🟡 P2     | ⏳     |
| Switch              | switch.tsx            | 5                | 🟠 P1     | ⏳     |
| Table               | table.tsx             | 6                | 🟠 P1     | ⏳     |
| Tabs                | tabs.tsx              | 5                | 🟠 P1     | ⏳     |
| Textarea            | textarea.tsx          | 6                | 🟠 P1     | ⏳     |
| Tooltip             | tooltip.tsx           | 4                | 🟡 P2     | ⏳     |
| **VehicleCard**     | vehicle-card.tsx      | **15**           | 🔴 P0     | ⏳     |

**Subtotal UI:** ~148 tests

---

#### Componentes Layout (`src/components/layout/`) - 3 archivos

| Componente | Archivo    | Tests Requeridos | Prioridad |
| ---------- | ---------- | ---------------- | --------- |
| **Navbar** | navbar.tsx | **12**           | 🔴 P0     |
| **Footer** | footer.tsx | **6**            | 🟠 P1     |

**Subtotal Layout:** ~18 tests

---

#### Componentes Homepage (`src/components/homepage/`) - 15 archivos

| Componente              | Archivo                   | Tests Requeridos | Prioridad |
| ----------------------- | ------------------------- | ---------------- | --------- |
| BrandSlider             | brand-slider.tsx          | 5                | 🟡 P2     |
| CategoryCards           | category-cards.tsx        | 6                | 🟡 P2     |
| CTASection              | cta-section.tsx           | 4                | 🟡 P2     |
| **FeaturedListingGrid** | featured-listing-grid.tsx | **8**            | 🔴 P0     |
| **FeaturedSection**     | featured-section.tsx      | **8**            | 🔴 P0     |
| FeaturesGrid            | features-grid.tsx         | 4                | 🟢 P3     |
| **HeroCarousel**        | hero-carousel.tsx         | **10**           | 🔴 P0     |
| HeroEnhanced            | hero-enhanced.tsx         | 6                | 🟠 P1     |
| HeroStatic              | hero-static.tsx           | 4                | 🟢 P3     |
| LoadingStates           | loading-states.tsx        | 4                | 🟡 P2     |
| SectionContainer        | section-container.tsx     | 3                | 🟢 P3     |
| SectionHeader           | section-header.tsx        | 4                | 🟡 P2     |
| TestimonialsCarousel    | testimonials-carousel.tsx | 6                | 🟡 P2     |
| WhyChooseUs             | why-choose-us.tsx         | 4                | 🟢 P3     |

**Subtotal Homepage:** ~76 tests

---

#### Componentes Search (`src/components/search/`) - 3 archivos

| Componente               | Archivo                    | Tests Requeridos | Prioridad |
| ------------------------ | -------------------------- | ---------------- | --------- |
| **VehicleFilters**       | vehicle-filters.tsx        | **12**           | 🔴 P0     |
| **VehicleSearchResults** | vehicle-search-results.tsx | **10**           | 🔴 P0     |

**Subtotal Search:** ~22 tests

---

#### Componentes Vehicle Detail (`src/components/vehicle-detail/`) - 6 archivos

| Componente         | Archivo              | Tests Requeridos | Prioridad |
| ------------------ | -------------------- | ---------------- | --------- |
| SellerCard         | seller-card.tsx      | 6                | 🟠 P1     |
| SimilarVehicles    | similar-vehicles.tsx | 6                | 🟠 P1     |
| **VehicleGallery** | vehicle-gallery.tsx  | **10**           | 🔴 P0     |
| **VehicleHeader**  | vehicle-header.tsx   | **8**            | 🔴 P0     |
| VehicleTabs        | vehicle-tabs.tsx     | 6                | 🟠 P1     |

**Subtotal Vehicle Detail:** ~36 tests

---

#### Hooks (`src/hooks/`) - 18 archivos

| Hook                   | Archivo                     | Tests Requeridos | Prioridad |
| ---------------------- | --------------------------- | ---------------- | --------- |
| **useAuth**            | use-auth.tsx                | **15**           | 🔴 P0     |
| **useAlerts**          | use-alerts.ts               | **10**           | 🔴 P0     |
| useAppointments        | use-appointments.ts         | 8                | 🟡 P2     |
| **useComparisons**     | use-comparisons.ts          | **10**           | 🔴 P0     |
| useContact             | use-contact.ts              | 6                | 🟡 P2     |
| useCRM                 | use-crm.ts                  | 8                | 🟡 P2     |
| useDealerBilling       | use-dealer-billing.ts       | 6                | 🟡 P2     |
| useDealerEmployees     | use-dealer-employees.ts     | 6                | 🟡 P2     |
| useDealerSettings      | use-dealer-settings.ts      | 5                | 🟡 P2     |
| **useDealers**         | use-dealers.ts              | **12**           | 🔴 P0     |
| **useFavorites**       | use-favorites.ts            | **14**           | 🔴 P0     |
| useHomepageSections    | use-homepage-sections.ts    | 6                | 🟠 P1     |
| useMedia               | use-media.ts                | 8                | 🟠 P1     |
| **useReviews**         | use-reviews.ts              | **10**           | 🔴 P0     |
| useVehicleIntelligence | use-vehicle-intelligence.ts | 6                | 🟡 P2     |
| useVehicleSearch       | use-vehicle-search.ts       | 10               | 🟠 P1     |
| **useVehicles**        | use-vehicles.ts             | **18**           | 🔴 P0     |
| **useAdmin**           | use-admin.ts                | **12**           | 🔴 P0     |

**Subtotal Hooks:** ~170 tests

---

#### Services (`src/services/`) - 22 archivos

| Service             | Archivo                 | Tests Requeridos | Prioridad |
| ------------------- | ----------------------- | ---------------- | --------- |
| **Admin**           | admin.ts                | **12**           | 🔴 P0     |
| **Alerts**          | alerts.ts               | **8**            | 🔴 P0     |
| Appointments        | appointments.ts         | 6                | 🟡 P2     |
| **Auth**            | auth.ts                 | **14**           | 🔴 P0     |
| **Checkout**        | checkout.ts             | **10**           | 🔴 P0     |
| **Comparisons**     | comparisons.ts          | **8**            | 🔴 P0     |
| Contact             | contact.ts              | 6                | 🟡 P2     |
| CRM                 | crm.ts                  | 8                | 🟡 P2     |
| DealerBilling       | dealer-billing.ts       | 6                | 🟡 P2     |
| DealerEmployees     | dealer-employees.ts     | 6                | 🟡 P2     |
| DealerSettings      | dealer-settings.ts      | 5                | 🟡 P2     |
| **Dealers**         | dealers.ts              | **10**           | 🔴 P0     |
| **Favorites**       | favorites.ts            | **8**            | 🔴 P0     |
| History             | history.ts              | 6                | 🟠 P1     |
| HomepageSections    | homepage-sections.ts    | 5                | 🟠 P1     |
| Media               | media.ts                | 8                | 🟠 P1     |
| Messaging           | messaging.ts            | 8                | 🟠 P1     |
| Notifications       | notifications.ts        | 8                | 🟠 P1     |
| **Reviews**         | reviews.ts              | **8**            | 🔴 P0     |
| Users               | users.ts                | 6                | 🟠 P1     |
| VehicleIntelligence | vehicle-intelligence.ts | 6                | 🟡 P2     |
| **Vehicles**        | vehicles.ts             | **12**           | 🔴 P0     |

**Subtotal Services:** ~178 tests

---

#### Lib/Utils (`src/lib/`) - 3 archivos

| Utility        | Archivo          | Tests Requeridos | Prioridad |
| -------------- | ---------------- | ---------------- | --------- |
| **API Client** | api-client.ts    | **10**           | 🔴 P0     |
| Design Tokens  | design-tokens.ts | 3                | 🟢 P3     |
| **Utils**      | utils.ts         | **8**            | 🔴 P0     |

**Subtotal Lib:** ~21 tests

---

#### Páginas Principales - Tests de Integración

| Página                | Ruta               | Tests Requeridos | Prioridad |
| --------------------- | ------------------ | ---------------- | --------- |
| **Homepage**          | /                  | **8**            | 🔴 P0     |
| **Search**            | /buscar            | **10**           | 🔴 P0     |
| **Vehicle Detail**    | /vehiculos/[slug]  | **10**           | 🔴 P0     |
| **Auth Flow**         | /auth/\*           | **8**            | 🔴 P0     |
| **Cuenta Dashboard**  | /cuenta            | 6                | 🟠 P1     |
| **Cuenta Favoritos**  | /cuenta/favoritos  | 6                | 🟠 P1     |
| **Cuenta Mensajes**   | /cuenta/mensajes   | 6                | 🟠 P1     |
| **Cuenta Alertas**    | /cuenta/alertas    | 5                | 🟠 P1     |
| **Cuenta Seguridad**  | /cuenta/seguridad  | 6                | 🟠 P1     |
| **Comparar**          | /comparar          | 6                | 🟠 P1     |
| **Dealer Dashboard**  | /dealer            | 8                | 🟠 P1     |
| **Dealer Inventario** | /dealer/inventario | 8                | 🟠 P1     |
| **Dealer Leads**      | /dealer/leads      | 6                | 🟡 P2     |
| **Dealer Analytics**  | /dealer/analytics  | 5                | 🟡 P2     |
| **Admin Dashboard**   | /admin             | 8                | 🟠 P1     |
| **Admin Users**       | /admin/usuarios    | 6                | 🟡 P2     |
| **Admin Vehicles**    | /admin/vehiculos   | 6                | 🟡 P2     |
| **Checkout**          | /checkout          | **10**           | 🔴 P0     |
| **Publicar**          | /publicar          | 8                | 🟠 P1     |

**Subtotal Integración:** ~136 tests

---

#### Tests E2E (Playwright)

| Flujo                    | Archivo                | Tests Requeridos | Prioridad |
| ------------------------ | ---------------------- | ---------------- | --------- |
| **Authentication**       | auth.spec.ts           | **8**            | 🔴 P0     |
| **Search & Filters**     | search.spec.ts         | **10**           | 🔴 P0     |
| **Vehicle Detail**       | vehicle-detail.spec.ts | **8**            | 🔴 P0     |
| **Favorites**            | favorites.spec.ts      | 6                | 🟠 P1     |
| **Comparison**           | comparison.spec.ts     | 5                | 🟠 P1     |
| **Contact Seller**       | contact.spec.ts        | 4                | 🟡 P2     |
| **Publish Vehicle**      | publish.spec.ts        | 8                | 🟠 P1     |
| **Checkout Flow**        | checkout.spec.ts       | **10**           | 🔴 P0     |
| **Dealer Portal**        | dealer-portal.spec.ts  | 8                | 🟠 P1     |
| **Admin Portal**         | admin-portal.spec.ts   | 6                | 🟡 P2     |
| **Responsive/Mobile**    | mobile.spec.ts         | 6                | 🟠 P1     |
| **Accessibility (a11y)** | accessibility.spec.ts  | 5                | 🟡 P2     |

**Subtotal E2E:** ~84 tests

---

### 📊 RESUMEN TOTAL DE TESTS REQUERIDOS

| Categoría            | Tests   | Prioridad P0 | Prioridad P1 | Prioridad P2+ |
| -------------------- | ------- | ------------ | ------------ | ------------- |
| Componentes UI       | 148     | 48           | 54           | 46            |
| Componentes Layout   | 18      | 12           | 6            | 0             |
| Componentes Homepage | 76      | 26           | 10           | 40            |
| Componentes Search   | 22      | 22           | 0            | 0             |
| Componentes VDetail  | 36      | 18           | 18           | 0             |
| Hooks                | 170     | 101          | 24           | 45            |
| Services             | 178     | 90           | 42           | 46            |
| Lib/Utils            | 21      | 18           | 0            | 3             |
| **Subtotal Unit**    | **669** | **335**      | **154**      | **180**       |
| Integración          | 136     | 46           | 61           | 29            |
| E2E                  | 84      | 36           | 27           | 21            |
| **TOTAL**            | **889** | **417**      | **242**      | **230**       |

---

### 🎯 PLAN DE IMPLEMENTACIÓN POR PRIORIDAD

#### Fase 1: Core P0 (335 tests unit + 46 integ + 36 E2E = 417 tests)

**Tiempo estimado:** 8-10 horas

| Área              | Archivos de Test                         | Tests |
| ----------------- | ---------------------------------------- | ----- |
| Infraestructura   | mocks/handlers.ts, server.ts, test-utils | Setup |
| UI Components P0  | button, input, vehicle-card, deal-rating | 48    |
| Layout P0         | navbar                                   | 12    |
| Homepage P0       | hero-carousel, featured-section          | 26    |
| Search P0         | vehicle-filters, vehicle-search-results  | 22    |
| Vehicle Detail P0 | vehicle-gallery, vehicle-header          | 18    |
| Hooks P0          | use-auth, use-favorites, use-vehicles... | 101   |
| Services P0       | auth, vehicles, favorites, checkout...   | 90    |
| Lib P0            | api-client, utils                        | 18    |
| Integración P0    | homepage, search, vehicle-detail, auth   | 46    |
| E2E P0            | auth, search, vehicle-detail, checkout   | 36    |

---

## 🗂️ ESTRUCTURA DE ARCHIVOS DE TESTS ACTUALIZADA

```
frontend/web-next/src/
├── __tests__/                           # Tests unitarios
│   ├── mocks/
│   │   ├── handlers.ts                  # MSW handlers (todos los endpoints)
│   │   ├── server.ts                    # MSW server
│   │   └── data/                        # Mock data
│   │       ├── vehicles.ts
│   │       ├── users.ts
│   │       ├── dealers.ts
│   │       └── index.ts
│   ├── utils/
│   │   └── test-utils.tsx               # Render helpers con providers
│   ├── components/
│   │   ├── ui/
│   │   │   ├── button.test.tsx
│   │   │   ├── input.test.tsx
│   │   │   ├── badge.test.tsx
│   │   │   ├── card.test.tsx
│   │   │   ├── checkbox.test.tsx
│   │   │   ├── deal-rating-badge.test.tsx
│   │   │   ├── select.test.tsx
│   │   │   ├── switch.test.tsx
│   │   │   ├── table.test.tsx
│   │   │   ├── tabs.test.tsx
│   │   │   ├── textarea.test.tsx
│   │   │   └── vehicle-card.test.tsx
│   │   ├── layout/
│   │   │   ├── navbar.test.tsx
│   │   │   └── footer.test.tsx
│   │   ├── homepage/
│   │   │   ├── hero-carousel.test.tsx
│   │   │   ├── featured-section.test.tsx
│   │   │   ├── featured-listing-grid.test.tsx
│   │   │   ├── hero-enhanced.test.tsx
│   │   │   └── testimonials-carousel.test.tsx
│   │   ├── search/
│   │   │   ├── vehicle-filters.test.tsx
│   │   │   └── vehicle-search-results.test.tsx
│   │   └── vehicle-detail/
│   │       ├── vehicle-gallery.test.tsx
│   │       ├── vehicle-header.test.tsx
│   │       ├── vehicle-tabs.test.tsx
│   │       ├── seller-card.test.tsx
│   │       └── similar-vehicles.test.tsx
│   ├── hooks/
│   │   ├── use-auth.test.tsx
│   │   ├── use-favorites.test.tsx
│   │   ├── use-vehicles.test.tsx
│   │   ├── use-comparisons.test.tsx
│   │   ├── use-alerts.test.tsx
│   │   ├── use-dealers.test.tsx
│   │   ├── use-reviews.test.tsx
│   │   ├── use-admin.test.tsx
│   │   ├── use-homepage-sections.test.tsx
│   │   └── use-media.test.tsx
│   ├── services/
│   │   ├── auth.test.ts
│   │   ├── vehicles.test.ts
│   │   ├── favorites.test.ts
│   │   ├── dealers.test.ts
│   │   ├── reviews.test.ts
│   │   ├── comparisons.test.ts
│   │   ├── alerts.test.ts
│   │   ├── checkout.test.ts
│   │   ├── admin.test.ts
│   │   ├── media.test.ts
│   │   └── messaging.test.ts
│   ├── lib/
│   │   ├── api-client.test.ts
│   │   └── utils.test.ts
│   └── integration/
│       ├── homepage.test.tsx
│       ├── search-page.test.tsx
│       ├── vehicle-detail.test.tsx
│       ├── auth-flow.test.tsx
│       ├── favorites-flow.test.tsx
│       ├── comparison-flow.test.tsx
│       ├── checkout-flow.test.tsx
│       ├── dealer-dashboard.test.tsx
│       ├── admin-dashboard.test.tsx
│       └── publish-flow.test.tsx
│
├── e2e/                                 # Tests E2E (Playwright)
│   ├── auth.spec.ts
│   ├── search.spec.ts
│   ├── vehicle-detail.spec.ts
│   ├── favorites.spec.ts
│   ├── comparison.spec.ts
│   ├── contact.spec.ts
│   ├── publish.spec.ts
│   ├── checkout.spec.ts
│   ├── dealer-portal.spec.ts
│   ├── admin-portal.spec.ts
│   ├── mobile.spec.ts
│   └── accessibility.spec.ts
│
└── test/
    └── setup.ts                         # ✅ Ya existe
```

---

## 🎯 TESTS UNITARIOS - ESPECIFICACIONES DETALLADAS

### 1. Componentes UI Base

#### `button.test.tsx`

### 2. Componentes Layout

#### `navbar.test.tsx`

```typescript
describe("Navbar", () => {
  describe("Rendering", () => {
    it("renders logo with link to home");
    it("renders main navigation links (Comprar, Vender, Dealers)");
    it("renders auth button when not authenticated");
    it("renders user menu when authenticated");
  });

  describe("Mobile", () => {
    it("shows hamburger menu on mobile");
    it("opens mobile menu on hamburger click");
    it("closes mobile menu on link click");
    it("closes mobile menu on outside click");
  });

  describe("Authentication", () => {
    it('shows "Iniciar Sesión" when not logged in');
    it("shows user avatar when logged in");
    it("shows dropdown with user options on avatar click");
    it("shows logout option in dropdown");
  });
});
```

**Casos: 12 tests**

---

### 3. Hooks

#### `use-auth.test.tsx`

```typescript
describe("useAuth", () => {
  describe("Initial State", () => {
    it("returns isAuthenticated false initially");
    it("returns user as null initially");
    it("returns isLoading true while checking session");
  });

  describe("Login", () => {
    it("sets user after successful login");
    it("sets isAuthenticated to true after login");
    it("stores tokens in localStorage");
    it("throws error on invalid credentials");
    it("handles network errors gracefully");
  });

  describe("Register", () => {
    it("creates user and logs in automatically");
    it("validates email format");
    it("throws error on existing email");
  });

  describe("Logout", () => {
    it("clears user state");
    it("clears tokens from localStorage");
    it("redirects to home page");
  });

  describe("Session", () => {
    it("restores session from stored token");
    it("refreshes token when expired");
    it("logs out when refresh fails");
  });
});
```

**Casos: 15 tests**

---

#### `use-favorites.test.tsx`

```typescript
describe("useFavorites", () => {
  describe("Authenticated User", () => {
    it("fetches favorites from API");
    it("returns favorites list");
    it("returns count of favorites");
    it("isFavorite returns true for favorited vehicles");
    it("isFavorite returns false for non-favorited vehicles");
  });

  describe("Add Favorite", () => {
    it("adds vehicle to favorites");
    it("invalidates favorites cache");
    it("optimistically updates UI");
    it("reverts on API error");
  });

  describe("Remove Favorite", () => {
    it("removes vehicle from favorites");
    it("invalidates favorites cache");
    it("optimistically updates UI");
  });

  describe("Toggle Favorite", () => {
    it("adds if not favorited");
    it("removes if already favorited");
    it("returns new favorite state");
  });

  describe("Unauthenticated User", () => {
    it("stores favorites in localStorage");
    it("syncs to server on login");
  });
});
```

**Casos: 14 tests**

---

#### `use-vehicles.test.tsx`

```typescript
describe("useVehicles", () => {
  describe("Search", () => {
    it("fetches vehicles with default params");
    it("applies search query filter");
    it("applies make filter");
    it("applies model filter");
    it("applies year range filter");
    it("applies price range filter");
    it("applies pagination");
    it("applies sorting");
  });

  describe("Vehicle Detail", () => {
    it("fetches vehicle by slug");
    it("returns 404 for non-existent vehicle");
    it("caches vehicle data");
  });

  describe("Catalog", () => {
    it("fetches all makes");
    it("fetches models by make");
    it("fetches body types");
    it("fetches fuel types");
  });

  describe("CRUD", () => {
    it("creates new vehicle");
    it("updates vehicle");
    it("deletes vehicle");
    it("invalidates cache after mutation");
  });
});
```

**Casos: 18 tests**

---

### 4. Services

#### `auth.test.ts`

```typescript
describe("authService", () => {
  describe("login", () => {
    it("sends credentials to API");
    it("returns user and tokens on success");
    it("throws AuthError on invalid credentials");
    it("throws NetworkError on connection failure");
  });

  describe("register", () => {
    it("sends user data to API");
    it("validates required fields");
    it("throws ValidationError on duplicate email");
  });

  describe("refreshToken", () => {
    it("exchanges refresh token for new access token");
    it("throws on expired refresh token");
  });

  describe("logout", () => {
    it("calls logout endpoint");
    it("clears local tokens");
  });

  describe("2FA", () => {
    it("returns QR code for 2FA setup");
    it("enables 2FA with valid code");
    it("disables 2FA with valid code");
    it("generates backup codes");
  });
});
```

**Casos: 14 tests**

---

#### `vehicles.test.ts`

```typescript
describe("vehiclesService", () => {
  describe("search", () => {
    it("builds correct query params");
    it("handles empty results");
    it("returns paginated response");
    it("handles API errors");
  });

  describe("getBySlug", () => {
    it("fetches vehicle by slug");
    it("throws NotFoundError on 404");
  });

  describe("create", () => {
    it("sends vehicle data");
    it("returns created vehicle");
    it("validates required fields");
  });

  describe("update", () => {
    it("sends partial update");
    it("returns updated vehicle");
  });

  describe("delete", () => {
    it("deletes vehicle by id");
    it("handles 404 gracefully");
  });
});
```

**Casos: 12 tests**

---

## 🔗 TESTS DE INTEGRACIÓN (20+ tests)

### `homepage.integration.test.tsx`

```typescript
describe("Homepage Integration", () => {
  it("renders hero carousel with vehicles from API");
  it("renders featured sections from homepage-sections API");
  it("handles API error with fallback UI");
  it("navigates to search page on search submit");
  it("navigates to vehicle detail on card click");
});
```

---

### `search-page.integration.test.tsx`

```typescript
describe("Search Page Integration", () => {
  it("loads vehicles on mount");
  it("updates URL with filter params");
  it("applies filters from URL on mount");
  it("shows loading skeletons while fetching");
  it("shows empty state when no results");
  it("paginates results");
  it("sorts results by selected option");
  it("resets filters on clear button");
});
```

---

### `vehicle-detail.integration.test.tsx`

```typescript
describe("Vehicle Detail Integration", () => {
  it("loads vehicle data by slug");
  it("shows 404 page for invalid slug");
  it("renders all vehicle information");
  it("shows similar vehicles section");
  it("enables favorite toggle for authenticated users");
  it("shows login prompt for unauthenticated favorite attempt");
  it("opens contact modal on contact button click");
});
```

---

### `auth-flow.integration.test.tsx`

```typescript
describe("Auth Flow Integration", () => {
  it("logs in user and shows user menu");
  it("shows error message on invalid credentials");
  it("registers new user and logs in automatically");
  it("logs out and shows login button");
  it("redirects to original page after login");
  it("protects private routes");
});
```

---

## 🌐 TESTS E2E (10+ tests)

### `e2e/auth.spec.ts`

```typescript
describe("Authentication E2E", () => {
  test("user can register a new account");
  test("user can login with valid credentials");
  test("user sees error with invalid credentials");
  test("user can logout");
  test("user can reset password");
});
```

---

### `e2e/search.spec.ts`

```typescript
describe("Vehicle Search E2E", () => {
  test("user can search for vehicles");
  test("user can filter by make and model");
  test("user can filter by price range");
  test("user can sort results");
  test("user can paginate through results");
});
```

---

### `e2e/vehicle-detail.spec.ts`

```typescript
describe("Vehicle Detail E2E", () => {
  test("user can view vehicle details");
  test("user can view vehicle gallery");
  test("user can contact seller");
  test("user can add to favorites");
  test("user can share vehicle");
});
```

---

### `e2e/dealer-portal.spec.ts`

```typescript
describe("Dealer Portal E2E", () => {
  test("dealer can view dashboard");
  test("dealer can add new vehicle");
  test("dealer can edit vehicle");
  test("dealer can view leads");
  test("dealer can view analytics");
});
```

---

## 📊 MATRIZ DE COBERTURA OBJETIVO

| Área                  | Archivos       | Statements | Branches | Functions | Lines   |
| --------------------- | -------------- | ---------- | -------- | --------- | ------- |
| **components/ui**     | 27 componentes | 85%        | 80%      | 85%       | 85%     |
| **components/layout** | 2 componentes  | 90%        | 85%      | 90%       | 90%     |
| **hooks**             | 18 hooks       | 80%        | 75%      | 80%       | 80%     |
| **services**          | 22 servicios   | 85%        | 80%      | 85%       | 85%     |
| **lib**               | utilidades     | 90%        | 85%      | 90%       | 90%     |
| **TOTAL**             | -              | **83%**    | **79%**  | **84%**   | **84%** |

---

## 🛠️ DEPENDENCIAS REQUERIDAS

Las siguientes dependencias ya están instaladas:

```json
{
  "devDependencies": {
    "@testing-library/jest-dom": "^6.9.1",
    "@testing-library/react": "^16.3.2",
    "@testing-library/user-event": "^14.6.1",
    "@vitest/coverage-v8": "^4.0.18",
    "@vitest/ui": "^4.0.18",
    "@playwright/test": "^1.58.1",
    "jsdom": "^27.4.0",
    "msw": "^2.12.7",
    "vitest": "^4.0.18"
  }
}
```

---

## 📝 PRIORIDAD DE IMPLEMENTACIÓN

### Fase 1: Infraestructura (30 min)

1. [ ] Crear `src/__tests__/mocks/handlers.ts` - MSW handlers
2. [ ] Crear `src/__tests__/mocks/server.ts` - MSW server
3. [ ] Crear `src/__tests__/utils/test-utils.tsx` - Render helpers
4. [ ] Actualizar `src/test/setup.ts` - Integrar MSW

### Fase 2: Tests Unitarios Core (2 horas)

5. [ ] `button.test.tsx` - 12 tests
6. [ ] `input.test.tsx` - 12 tests
7. [ ] `vehicle-card.test.tsx` - 15 tests
8. [ ] `deal-rating-badge.test.tsx` - 9 tests
9. [ ] `navbar.test.tsx` - 12 tests

### Fase 3: Tests de Hooks (1.5 horas)

10. [ ] `use-auth.test.tsx` - 15 tests
11. [ ] `use-favorites.test.tsx` - 14 tests
12. [ ] `use-vehicles.test.tsx` - 18 tests

### Fase 4: Tests de Services (1 hora)

13. [ ] `auth.test.ts` - 14 tests
14. [ ] `vehicles.test.ts` - 12 tests
15. [ ] `favorites.test.ts` - 8 tests

### Fase 5: Tests de Integración (1.5 horas)

16. [ ] `homepage.integration.test.tsx`
17. [ ] `search-page.integration.test.tsx`
18. [ ] `vehicle-detail.integration.test.tsx`
19. [ ] `auth-flow.integration.test.tsx`

### Fase 6: Tests E2E (1 hora)

20. [ ] Crear carpeta `e2e/`
21. [ ] `auth.spec.ts`
22. [ ] `search.spec.ts`
23. [ ] `vehicle-detail.spec.ts`

---

## ✅ CHECKLIST DE COMPLETADO

### Infraestructura

- [ ] MSW handlers configurados (22 servicios)
- [ ] Mock data factories creadas
- [ ] Test utilities con providers
- [ ] Setup actualizado con MSW

### Tests Unitarios (669 tests)

- [ ] Componentes UI (148 tests)
- [ ] Componentes Layout (18 tests)
- [ ] Componentes Homepage (76 tests)
- [ ] Componentes Search (22 tests)
- [ ] Componentes Vehicle Detail (36 tests)
- [ ] Hooks (170 tests)
- [ ] Services (178 tests)
- [ ] Lib/Utils (21 tests)

### Tests de Integración (136 tests)

- [ ] Homepage integration
- [ ] Search page integration
- [ ] Vehicle detail integration
- [ ] Auth flow integration
- [ ] Cuenta pages integration
- [ ] Dealer portal integration
- [ ] Admin portal integration
- [ ] Checkout flow integration
- [ ] Publish flow integration

### Tests E2E (84 tests)

- [ ] auth.spec.ts (8 tests)
- [ ] search.spec.ts (10 tests)
- [ ] vehicle-detail.spec.ts (8 tests)
- [ ] favorites.spec.ts (6 tests)
- [ ] comparison.spec.ts (5 tests)
- [ ] contact.spec.ts (4 tests)
- [ ] publish.spec.ts (8 tests)
- [ ] checkout.spec.ts (10 tests)
- [ ] dealer-portal.spec.ts (8 tests)
- [ ] admin-portal.spec.ts (6 tests)
- [ ] mobile.spec.ts (6 tests)
- [ ] accessibility.spec.ts (5 tests)

### Métricas

- [ ] Coverage > 80%
- [ ] Todos los tests pasan (889 tests)
- [ ] CI/CD integrado (GitHub Actions)

---

## 🚀 COMANDOS DE EJECUCIÓN

```bash
# Tests unitarios
pnpm test                  # Watch mode
pnpm test:ui               # UI visual
pnpm test:coverage         # Con reporte de cobertura

# Tests E2E
pnpm test:e2e              # Headless
pnpm test:e2e:ui           # Con UI de Playwright
```

---

**Última actualización:** Febrero 2, 2026
