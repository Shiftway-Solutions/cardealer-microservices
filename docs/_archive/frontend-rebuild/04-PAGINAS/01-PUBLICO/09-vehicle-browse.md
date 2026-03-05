---
title: "74 - Páginas de Detalle y Navegación de Vehículos"
priority: P0
estimated_time: "2 horas"
dependencies: []
apis: ["VehiclesSaleService", "ContactService"]
status: partial
last_updated: "2026-01-30"
---

# 74 - Páginas de Detalle y Navegación de Vehículos

> **Módulo**: VehicleDetailPage, BrowsePage, RecentlyViewedPage  
> **Ubicación**: `frontend/web/src/pages/vehicles/`  
> **Última actualización**: Enero 2026

---

## 📐 Arquitectura General

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        VEHICLE PAGES                                    │
│                                                                         │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │                     VehicleDetailPage                          │    │
│  │  /vehicles/:slug (SEO-friendly URL con UUID)                   │    │
│  │                                                                 │    │
│  │  ┌─────────────────┐  ┌────────────────────────────────────┐  │    │
│  │  │ Breadcrumbs     │  │ Title + Price + Actions            │  │    │
│  │  └─────────────────┘  │ (Favorite, Share, Print)           │  │    │
│  │                       └────────────────────────────────────┘  │    │
│  │  ┌────────────────────────────────────────────────────────┐   │    │
│  │  │ VehicleMediaGallery (360° viewer, images, video)       │   │    │
│  │  └────────────────────────────────────────────────────────┘   │    │
│  │  ┌─────────────────────┐  ┌───────────────────────────────┐  │    │
│  │  │ Description         │  │ ContactSellerForm             │  │    │
│  │  │ VehicleSpecs        │  │ (sidebar - contact dealer)    │  │    │
│  │  │ Features List       │  │                               │  │    │
│  │  └─────────────────────┘  └───────────────────────────────┘  │    │
│  │  ┌────────────────────────────────────────────────────────┐   │    │
│  │  │ SimilarVehicles (recomendaciones)                      │   │    │
│  │  └────────────────────────────────────────────────────────┘   │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                         │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │                        BrowsePage                              │    │
│  │  /browse (listado con filtros avanzados)                       │    │
│  │                                                                 │    │
│  │  ┌─────────────────┐  ┌────────────────────────────────────┐  │    │
│  │  │ AdvancedFilters │  │ Results Grid/List                  │  │    │
│  │  │ (sidebar)       │  │ - VehicleCard components           │  │    │
│  │  │ - Make/Model    │  │ - Grid/List toggle                 │  │    │
│  │  │ - Price Range   │  │ - Sort options                     │  │    │
│  │  │ - Year Range    │  │ - Pagination                       │  │    │
│  │  │ - Mileage       │  │ - Save Search modal                │  │    │
│  │  │ - Transmission  │  └────────────────────────────────────┘  │    │
│  │  │ - Fuel Type     │                                          │    │
│  │  │ - Body Type     │                                          │    │
│  │  │ - Condition     │                                          │    │
│  │  └─────────────────┘                                           │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                         │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │                   RecentlyViewedPage                           │    │
│  │  /recently-viewed (historial del usuario)                      │    │
│  │                                                                 │    │
│  │  - Grid/List toggle                                            │    │
│  │  - Price change indicators                                     │    │
│  │  - Sold status badges                                          │    │
│  │  - Clear history action                                        │    │
│  │  - Time-based grouping (Today, Yesterday, Last Week)           │    │
│  └────────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 🔧 Tipos TypeScript

```typescript
// VehicleDetailPage types
interface VehicleDetailParams {
  slug: string; // SEO-friendly: {year}-{make}-{model}-{uuid}
}

// Extract UUID from SEO-friendly slug
// Format: /vehicles/2024-mercedes-benz-clase-c-amg-a1111111-1111-1111-1111-111111111111
const extractIdFromSlug = (slugWithId: string): string => {
  const uuidRegex =
    /([a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12})$/i;
  const match = slugWithId.match(uuidRegex);
  return match ? match[1] : slugWithId;
};

// Vehicle from VehiclesSaleService
interface Vehicle {
  id: string;
  make: string;
  model: string;
  year: number;
  price: number;
  mileage: number;
  transmission: string;
  fuelType: string;
  bodyType: string;
  condition: "New" | "Used" | "Certified";
  location: string;
  description: string;
  features: string[];
  images: string[];
  isFeatured: boolean;
  isNew: boolean;
  sellerId: string;
  dealerId?: string;
  createdAt: string;
}

// BrowsePage filter types
interface VehicleFilters {
  make?: string;
  model?: string;
  minYear?: number;
  maxYear?: number;
  minPrice?: number;
  maxPrice?: number;
  minMileage?: number;
  maxMileage?: number;
  transmission?: string;
  fuelType?: string;
  bodyType?: string;
  condition?: string;
}

type SortOption =
  | "price-asc"
  | "price-desc"
  | "year-asc"
  | "year-desc"
  | "mileage-asc"
  | "mileage-desc"
  | "recent";

// SaveSearchModal types
interface SaveSearchData {
  name: string;
  filters: VehicleFilters;
  notificationsEnabled: boolean;
}

// RecentlyViewedPage types
interface ViewedVehicle {
  id: string;
  slug: string;
  title: string;
  price: number;
  year: number;
  mileage: number;
  location: string;
  imageUrl: string;
  viewedAt: string; // ISO timestamp
  priceChange?: {
    type: "up" | "down";
    amount: number;
    percentage: number;
  };
  isFavorite: boolean;
  isSold: boolean;
}
```

---

## 🧩 Componentes Principales

### VehicleDetailPage

```
frontend/web/src/pages/vehicles/VehicleDetailPage.tsx (372 líneas)
│
├── Imports
│   ├── useParams, Link, Navigate (react-router-dom)
│   ├── useQuery (TanStack Query v5)
│   ├── useTranslation (react-i18next)
│   └── useFavorites (hook local)
│
├── extractIdFromSlug() - Extraer UUID de slug SEO-friendly
│
├── VehicleDetailPage Component
│   ├── State
│   │   ├── vehicle (from useQuery)
│   │   ├── isLoading, isError
│   │   └── isLiked (from useFavorites)
│   │
│   ├── Effects
│   │   └── Scroll to top on page load
│   │
│   ├── Loading State
│   │   └── FiLoader spinner + message
│   │
│   ├── Error State
│   │   └── Navigate to /vehicles
│   │
│   └── Render
│       ├── Breadcrumbs (Home > Browse > Vehicle Title)
│       ├── Title Section
│       │   ├── Year Make Model
│       │   ├── Location + Condition badge
│       │   ├── Price (formatPrice)
│       │   └── Actions (Favorite, Share, Print)
│       ├── Badges (Featured, New)
│       ├── 2-Column Layout
│       │   ├── Left (2/3)
│       │   │   ├── VehicleMediaGallery
│       │   │   ├── Description (LocalizedContent)
│       │   │   ├── VehicleSpecs
│       │   │   └── Features list
│       │   └── Right (1/3)
│       │       └── ContactSellerForm (sticky)
│       └── SimilarVehicles
```

### BrowsePage

```
frontend/web/src/pages/vehicles/BrowsePage.tsx (375 líneas)
│
├── Imports
│   ├── useState, useCallback, useMemo
│   ├── useSearchParams (React Router)
│   ├── useSearchPage, useAddRecentSearch, useCreateSavedSearch
│   └── useCompare
│
├── BrowsePage Component
│   ├── State
│   │   ├── filters (VehicleFilters)
│   │   ├── sortBy (SortOption)
│   │   ├── currentPage
│   │   ├── viewMode ('grid' | 'list')
│   │   └── showSaveModal
│   │
│   ├── URL Sync
│   │   ├── getInitialFilters() - Parse URL params
│   │   └── updateURLParams() - Sync state to URL
│   │
│   ├── Data Processing
│   │   ├── useSearchPage hook (API + pagination)
│   │   ├── Fallback to mockVehicles on error
│   │   └── Client-side sorting
│   │
│   └── Render
│       ├── Header (title, subtitle)
│       ├── 2-Column Layout
│       │   ├── Sidebar
│       │   │   └── AdvancedFilters
│       │   └── Main Content
│       │       ├── Results Header
│       │       │   ├── Count + Live/Demo indicator
│       │       │   ├── Saved Searches link
│       │       │   ├── Compare button
│       │       │   ├── Save Search button
│       │       │   ├── Map View link
│       │       │   └── Grid/List toggle
│       │       ├── Loading (VehicleCardSkeleton grid)
│       │       ├── Empty State
│       │       ├── Vehicle Cards (grid/list)
│       │       └── Pagination
│       └── SaveSearchModal
```

### RecentlyViewedPage

```
frontend/web/src/pages/vehicles/RecentlyViewedPage.tsx (501 líneas)
│
├── State
│   ├── viewMode ('grid' | 'list')
│   ├── vehicles (ViewedVehicle[])
│   └── isLoading
│
├── Features
│   ├── Time-based grouping (Today, Yesterday, This Week, Older)
│   ├── Price change indicators (up/down arrows with %)
│   ├── Sold status badge
│   ├── Favorite toggle
│   ├── Remove individual item
│   ├── Clear all history
│   └── Grid/List toggle
│
└── Data Source
    └── UserBehaviorService (planned)
    └── Currently: Mock data
```

---

## 🔗 Hooks Utilizados

### useFavorites

```typescript
// hooks/useFavorites.ts
const useFavorites = () => {
  const [favorites, setFavorites] = useState<Set<string>>(new Set());

  const isFavorite = (vehicleId: string): boolean => favorites.has(vehicleId);

  const toggleFavorite = (vehicleId: string): void => {
    setFavorites((prev) => {
      const next = new Set(prev);
      if (next.has(vehicleId)) {
        next.delete(vehicleId);
      } else {
        next.add(vehicleId);
      }
      return next;
    });
    // TODO: Sync with VehiclesSaleService /api/favorites
  };

  return { favorites, isFavorite, toggleFavorite };
};
```

### useSearchPage

```typescript
// hooks/useSearch.ts
interface UseSearchPageResult {
  vehicles: Vehicle[];
  total: number;
  totalPages: number;
  currentPage: number;
  isLoading: boolean;
  isError: boolean;
  savedSearches: SavedSearch[];
}

const useSearchPage = (
  filters: VehicleFilters,
  page: number,
  pageSize: number,
): UseSearchPageResult => {
  const vehiclesQuery = useQuery({
    queryKey: ["vehicles", "search", filters, page, pageSize],
    queryFn: () => searchVehicles({ ...filters, page, pageSize }),
    staleTime: 30_000,
  });

  const savedSearchesQuery = useQuery({
    queryKey: ["savedSearches"],
    queryFn: getSavedSearches,
    staleTime: 60_000,
  });

  return {
    vehicles: vehiclesQuery.data?.items ?? [],
    total: vehiclesQuery.data?.total ?? 0,
    totalPages: vehiclesQuery.data?.totalPages ?? 0,
    currentPage: page,
    isLoading: vehiclesQuery.isLoading,
    isError: vehiclesQuery.isError,
    savedSearches: savedSearchesQuery.data ?? [],
  };
};
```

### useCompare

```typescript
// hooks/useCompare.ts
const MAX_COMPARE_ITEMS = 4;

const useCompare = () => {
  const [compareItems, setCompareItems] = useState<string[]>([]);

  const count = compareItems.length;
  const isFull = count >= MAX_COMPARE_ITEMS;

  const addToCompare = (vehicleId: string): boolean => {
    if (isFull) return false;
    setCompareItems((prev) => [...new Set([...prev, vehicleId])]);
    return true;
  };

  const removeFromCompare = (vehicleId: string): void => {
    setCompareItems((prev) => prev.filter((id) => id !== vehicleId));
  };

  const isInCompare = (vehicleId: string): boolean =>
    compareItems.includes(vehicleId);

  return {
    compareItems,
    count,
    isFull,
    addToCompare,
    removeFromCompare,
    isInCompare,
  };
};
```

---

## 🌐 API Services

### vehicleService.ts

```typescript
// services/vehicleService.ts
import api from "@/lib/api";

export interface VehicleFilters {
  search?: string;
  minPrice?: number;
  maxPrice?: number;
  make?: string;
  model?: string;
  minYear?: number;
  maxYear?: number;
  transmission?: string;
  fuelType?: string;
  bodyType?: string;
  condition?: string;
  page?: number;
  pageSize?: number;
}

export interface PaginatedVehicles {
  items: Vehicle[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// GET /api/vehicles/:id
export const getVehicleById = async (id: string): Promise<Vehicle> => {
  const { data } = await api.get(`/vehicles/${id}`);
  return data;
};

// GET /api/vehicles/search
export const searchVehicles = async (
  filters: VehicleFilters,
): Promise<PaginatedVehicles> => {
  const { data } = await api.get("/vehicles/search", { params: filters });
  return data;
};

// POST /api/vehicles (create listing)
export const createVehicle = async (
  vehicle: CreateVehicleDto,
): Promise<Vehicle> => {
  const { data } = await api.post("/vehicles", vehicle);
  return data;
};
```

### savedSearchService.ts

```typescript
// services/savedSearchService.ts

// GET /api/saved-searches
export const getSavedSearches = async (): Promise<SavedSearch[]> => {
  const { data } = await api.get("/saved-searches");
  return data;
};

// POST /api/saved-searches
export const createSavedSearch = async (
  search: CreateSavedSearchDto,
): Promise<SavedSearch> => {
  const { data } = await api.post("/saved-searches", search);
  return data;
};

// DELETE /api/saved-searches/:id
export const deleteSavedSearch = async (id: string): Promise<void> => {
  await api.delete(`/saved-searches/${id}`);
};
```

---

## 🧩 Componentes Secundarios

### VehicleMediaGallery

```
components/organisms/VehicleMediaGallery.tsx
│
├── Props
│   ├── vehicleId: string
│   ├── vehicleSlug: string
│   ├── images: string[]
│   └── alt: string
│
├── Features
│   ├── Main image with zoom
│   ├── Thumbnail strip
│   ├── 360° View button (link to Media360ViewerPage)
│   ├── Video Tour button (link to VideoTourPage)
│   └── Fullscreen gallery modal
```

### VehicleSpecs

```
components/organisms/VehicleSpecs.tsx
│
├── Props
│   └── vehicle: Vehicle
│
├── Displays
│   ├── Year
│   ├── Mileage (formatted)
│   ├── Transmission
│   ├── Fuel Type
│   ├── Body Type
│   ├── Drivetrain
│   ├── Engine
│   └── Exterior/Interior Color
```

### ContactSellerForm

```
components/organisms/ContactSellerForm.tsx
│
├── Props
│   └── vehicle: Vehicle
│
├── Form Fields
│   ├── Name
│   ├── Email
│   ├── Phone
│   └── Message (pre-filled with vehicle interest)
│
├── Actions
│   ├── Send Message (ContactService)
│   ├── Call Seller (tel: link)
│   └── WhatsApp (wa.me link)
```

### AdvancedFilters

```
components/organisms/AdvancedFilters.tsx
│
├── Props
│   ├── onFilterChange: (filters: VehicleFilters) => void
│   ├── onSortChange: (sort: SortOption) => void
│   ├── currentFilters: VehicleFilters
│   └── currentSort: SortOption
│
├── Filter Sections
│   ├── Make (dropdown with search)
│   ├── Model (dependent on Make)
│   ├── Year Range (min/max sliders)
│   ├── Price Range (min/max inputs)
│   ├── Mileage Range
│   ├── Transmission (checkboxes)
│   ├── Fuel Type (checkboxes)
│   ├── Body Type (checkboxes)
│   └── Condition (New/Used/Certified)
│
├── Sort Options
│   ├── Price (Low to High)
│   ├── Price (High to Low)
│   ├── Year (Newest)
│   ├── Year (Oldest)
│   ├── Mileage (Low to High)
│   └── Most Recent
│
└── Actions
    ├── Apply Filters
    └── Clear All
```

### VehicleCard

```
components/organisms/VehicleCard.tsx
│
├── Props
│   ├── vehicle: Vehicle
│   ├── viewMode: 'grid' | 'list'
│   └── showCompare?: boolean
│
├── Displays
│   ├── Image carousel (hover)
│   ├── Title (Year Make Model)
│   ├── Price (formatPrice)
│   ├── Key specs (mileage, transmission, fuel)
│   ├── Location
│   └── Badges (Featured, New, Price Drop)
│
├── Actions
│   ├── Favorite toggle
│   ├── Compare checkbox
│   └── Quick View (modal)
```

---

## 🛣️ Rutas

```typescript
// App.tsx
<Route path="/vehicles/:slug" element={<VehicleDetailPage />} />
<Route path="/browse" element={<BrowsePage />} />
<Route path="/recently-viewed" element={<RecentlyViewedPage />} />
```

---

## 🌍 Internacionalización

```json
// locales/es/vehicles.json
{
  "browse": {
    "title": "Explorar Vehículos",
    "subtitle": "Encuentra tu próximo vehículo",
    "vehiclesFound": "{{count}} vehículos encontrados",
    "filteredResults": "Resultados filtrados"
  },
  "detail": {
    "description": "Descripción",
    "featuresAndOptions": "Características y Opciones",
    "save": "Guardar",
    "saved": "Guardado",
    "featured": "Destacado",
    "new": "Nuevo"
  },
  "recentlyViewed": {
    "title": "Vistos Recientemente",
    "clearAll": "Limpiar Todo",
    "priceDropped": "Bajó {{percentage}}%",
    "priceIncreased": "Subió {{percentage}}%",
    "sold": "Vendido"
  }
}
```

---

## 📦 Dependencias

```json
{
  "@tanstack/react-query": "^5.x",
  "react-router-dom": "^6.x",
  "react-i18next": "^13.x",
  "react-icons": "^4.x"
}
```

---

## ✅ Checklist de Validación

### VehicleDetailPage

- [ ] SEO-friendly URLs con UUID extraction
- [ ] Breadcrumbs navegables
- [ ] Galería de imágenes con zoom
- [ ] Link a vista 360° y video tour
- [ ] Formulario de contacto funcional
- [ ] Botón de favoritos
- [ ] Share y Print buttons
- [ ] Vehículos similares
- [ ] i18n para todos los textos
- [ ] Loading y error states

### BrowsePage

- [ ] Filtros sincronizados con URL
- [ ] Grid/List toggle
- [ ] Paginación funcional
- [ ] Ordenamiento múltiple
- [ ] Live/Demo data indicator
- [ ] Guardar búsqueda modal
- [ ] Link a búsquedas guardadas
- [ ] Link a vista de mapa
- [ ] Comparar vehículos

### RecentlyViewedPage

- [ ] Agrupación por tiempo
- [ ] Indicadores de cambio de precio
- [ ] Badge de vendido
- [ ] Favoritos toggle
- [ ] Eliminar individual
- [ ] Limpiar historial
- [ ] Grid/List toggle

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/vehicle-browse.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Vehicle Browse - Navegación de Vehículos", () => {
  test("debe mostrar grid de vehículos con paginación", async ({ page }) => {
    await page.goto("/vehiculos");

    await expect(page.getByTestId("vehicle-grid")).toBeVisible();
    await expect(page.getByTestId("pagination")).toBeVisible();
    await expect(page.getByTestId("vehicle-card")).toHaveCount({ min: 12 });
  });

  test("debe cambiar ordenamiento", async ({ page }) => {
    await page.goto("/vehiculos");

    await page.getByRole("combobox", { name: /ordenar/i }).click();
    await page.getByRole("option", { name: /más recientes/i }).click();

    await expect(page).toHaveURL(/sort=newest/);
  });

  test("debe alternar entre vista grid y lista", async ({ page }) => {
    await page.goto("/vehiculos");

    await page.getByRole("button", { name: /vista lista/i }).click();
    await expect(page.getByTestId("vehicle-list")).toBeVisible();

    await page.getByRole("button", { name: /vista grid/i }).click();
    await expect(page.getByTestId("vehicle-grid")).toBeVisible();
  });

  test("debe mostrar vehículos vistos recientemente", async ({ page }) => {
    // Visitar un vehículo
    await page.goto("/vehiculos/toyota-camry-2023");

    // Ir a sección de recientes
    await page.goto("/vehiculos/vistos-recientemente");

    await expect(page.getByText(/toyota camry/i)).toBeVisible();
  });

  test("debe indicar cambios de precio en recientes", async ({ page }) => {
    await page.goto("/vehiculos/vistos-recientemente");

    // Si hay vehículos con cambio de precio
    const priceChange = page.getByTestId("price-change-indicator");
    if (await priceChange.first().isVisible()) {
      await expect(priceChange.first()).toContainText(/bajó|subió/i);
    }
  });

  test("debe limpiar historial de vistos", async ({ page }) => {
    await page.goto("/vehiculos/vistos-recientemente");

    await page.getByRole("button", { name: /limpiar historial/i }).click();
    await page.getByRole("button", { name: /confirmar/i }).click();

    await expect(page.getByText(/historial vacío/i)).toBeVisible();
  });
});
```

---

## 📚 Documentación Relacionada

- [10-dealer-crm.md](../05-DEALER/03-dealer-crm.md) - Leads que vienen de contactos
- [68-common-components.md](../09-COMPONENTES-COMUNES/01-common-components.md) - Componentes compartidos
- [75-vehicle-media-pages.md](../09-COMPONENTES-COMUNES/04-vehicle-media.md) - 360° y Video Tour
