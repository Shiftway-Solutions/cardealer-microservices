# 🗃️ Estado Global - Zustand Configuration

> **Tiempo estimado:** 60 minutos
> **Prerrequisitos:** Proyecto Next.js configurado, TanStack Query instalado
> **Última actualización:** Enero 2026

---

## 📋 OBJETIVO

Configurar sistema de estado global con Zustand:

- Stores por dominio (user, cart, favorites, UI)
- Persistencia con localStorage
- Hidratación SSR-safe
- Integración con TanStack Query
- DevTools para debugging

---

## 🎯 ARQUITECTURA DE ESTADO

```
┌─────────────────────────────────────────────────────────────────┐
│                    ESTADO DE LA APLICACIÓN                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────────┐  ┌──────────────────┐  ┌───────────────┐  │
│  │   Server State   │  │   Client State   │  │   UI State    │  │
│  │  (TanStack Query)│  │    (Zustand)     │  │   (Zustand)   │  │
│  ├──────────────────┤  ├──────────────────┤  ├───────────────┤  │
│  │ • Vehicles       │  │ • Auth/User      │  │ • Modals      │  │
│  │ • Dealers        │  │ • Favorites      │  │ • Sidebars    │  │
│  │ • Catalog        │  │ • Comparisons    │  │ • Toasts      │  │
│  │ • Messages       │  │ • Cart           │  │ • Filters     │  │
│  │ • Notifications  │  │ • Preferences    │  │ • Theme       │  │
│  └──────────────────┘  └──────────────────┘  └───────────────┘  │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 PASO 1: Instalar Dependencias

```bash
npm install zustand
npm install zustand-persist # Para persistencia
```

---

## 🔧 PASO 2: Estructura de Stores

```
src/
├── stores/
│   ├── index.ts              # Re-export de todos los stores
│   ├── useAuthStore.ts       # Autenticación y usuario
│   ├── useFavoritesStore.ts  # Favoritos
│   ├── useCompareStore.ts    # Comparador de vehículos
│   ├── useCartStore.ts       # Carrito (para boost/promociones)
│   ├── useUIStore.ts         # Estado de UI global
│   ├── useFiltersStore.ts    # Filtros de búsqueda
│   └── usePreferencesStore.ts # Preferencias de usuario
└── lib/
    └── store-utils.ts        # Utilidades para stores
```

---

## 🔧 PASO 3: Store de Autenticación

```typescript
// filepath: src/stores/useAuthStore.ts
import { create } from "zustand";
import { persist, createJSONStorage } from "zustand/middleware";
import { immer } from "zustand/middleware/immer";

// Types
export interface User {
  id: string;
  email: string;
  fullName: string;
  avatar?: string;
  role: "buyer" | "seller" | "dealer" | "admin";
  dealerId?: string;
  isVerified: boolean;
  createdAt: string;
}

export interface AuthState {
  // State
  user: User | null;
  accessToken: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;

  // Actions
  setUser: (user: User) => void;
  setTokens: (accessToken: string, refreshToken: string) => void;
  login: (user: User, accessToken: string, refreshToken: string) => void;
  logout: () => void;
  updateUser: (updates: Partial<User>) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  clearError: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    immer((set) => ({
      // Initial state
      user: null,
      accessToken: null,
      refreshToken: null,
      isAuthenticated: false,
      isLoading: false,
      error: null,

      // Actions
      setUser: (user) =>
        set((state) => {
          state.user = user;
          state.isAuthenticated = true;
        }),

      setTokens: (accessToken, refreshToken) =>
        set((state) => {
          state.accessToken = accessToken;
          state.refreshToken = refreshToken;
        }),

      login: (user, accessToken, refreshToken) =>
        set((state) => {
          state.user = user;
          state.accessToken = accessToken;
          state.refreshToken = refreshToken;
          state.isAuthenticated = true;
          state.error = null;
        }),

      logout: () =>
        set((state) => {
          state.user = null;
          state.accessToken = null;
          state.refreshToken = null;
          state.isAuthenticated = false;
        }),

      updateUser: (updates) =>
        set((state) => {
          if (state.user) {
            state.user = { ...state.user, ...updates };
          }
        }),

      setLoading: (loading) =>
        set((state) => {
          state.isLoading = loading;
        }),

      setError: (error) =>
        set((state) => {
          state.error = error;
        }),

      clearError: () =>
        set((state) => {
          state.error = null;
        }),
    })),
    {
      name: "okla-auth",
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({
        user: state.user,
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        isAuthenticated: state.isAuthenticated,
      }),
    },
  ),
);

// Selectores para optimizar re-renders
export const useUser = () => useAuthStore((state) => state.user);
export const useIsAuthenticated = () =>
  useAuthStore((state) => state.isAuthenticated);
export const useAccessToken = () => useAuthStore((state) => state.accessToken);
export const useAuthLoading = () => useAuthStore((state) => state.isLoading);
export const useAuthError = () => useAuthStore((state) => state.error);
```

---

## 🔧 PASO 4: Store de Favoritos

```typescript
// filepath: src/stores/useFavoritesStore.ts
import { create } from "zustand";
import { persist, createJSONStorage } from "zustand/middleware";
import { immer } from "zustand/middleware/immer";

export interface FavoriteVehicle {
  id: string;
  slug: string;
  title: string;
  price: number;
  primaryImage: string;
  addedAt: string;
  note?: string;
  priceAlertEnabled: boolean;
  priceAlertThreshold?: number;
}

interface FavoritesState {
  // State
  favorites: FavoriteVehicle[];
  isLoading: boolean;

  // Actions
  addFavorite: (vehicle: Omit<FavoriteVehicle, "addedAt">) => void;
  removeFavorite: (vehicleId: string) => void;
  toggleFavorite: (vehicle: Omit<FavoriteVehicle, "addedAt">) => void;
  updateNote: (vehicleId: string, note: string) => void;
  setPriceAlert: (
    vehicleId: string,
    enabled: boolean,
    threshold?: number,
  ) => void;
  isFavorite: (vehicleId: string) => boolean;
  clearAll: () => void;
  syncFromServer: (favorites: FavoriteVehicle[]) => void;
  setLoading: (loading: boolean) => void;
}

export const useFavoritesStore = create<FavoritesState>()(
  persist(
    immer((set, get) => ({
      // Initial state
      favorites: [],
      isLoading: false,

      // Actions
      addFavorite: (vehicle) =>
        set((state) => {
          const exists = state.favorites.some((f) => f.id === vehicle.id);
          if (!exists) {
            state.favorites.push({
              ...vehicle,
              addedAt: new Date().toISOString(),
              priceAlertEnabled: vehicle.priceAlertEnabled ?? false,
            });
          }
        }),

      removeFavorite: (vehicleId) =>
        set((state) => {
          state.favorites = state.favorites.filter((f) => f.id !== vehicleId);
        }),

      toggleFavorite: (vehicle) => {
        const { favorites, addFavorite, removeFavorite } = get();
        const exists = favorites.some((f) => f.id === vehicle.id);
        if (exists) {
          removeFavorite(vehicle.id);
        } else {
          addFavorite(vehicle);
        }
      },

      updateNote: (vehicleId, note) =>
        set((state) => {
          const favorite = state.favorites.find((f) => f.id === vehicleId);
          if (favorite) {
            favorite.note = note;
          }
        }),

      setPriceAlert: (vehicleId, enabled, threshold) =>
        set((state) => {
          const favorite = state.favorites.find((f) => f.id === vehicleId);
          if (favorite) {
            favorite.priceAlertEnabled = enabled;
            favorite.priceAlertThreshold = threshold;
          }
        }),

      isFavorite: (vehicleId) => {
        return get().favorites.some((f) => f.id === vehicleId);
      },

      clearAll: () =>
        set((state) => {
          state.favorites = [];
        }),

      syncFromServer: (favorites) =>
        set((state) => {
          state.favorites = favorites;
        }),

      setLoading: (loading) =>
        set((state) => {
          state.isLoading = loading;
        }),
    })),
    {
      name: "okla-favorites",
      storage: createJSONStorage(() => localStorage),
    },
  ),
);

// Selectores
export const useFavorites = () => useFavoritesStore((state) => state.favorites);
export const useFavoritesCount = () =>
  useFavoritesStore((state) => state.favorites.length);
export const useIsFavorite = (vehicleId: string) =>
  useFavoritesStore((state) => state.favorites.some((f) => f.id === vehicleId));
```

---

## 🔧 PASO 5: Store de Comparador

```typescript
// filepath: src/stores/useCompareStore.ts
import { create } from "zustand";
import { persist, createJSONStorage } from "zustand/middleware";
import { immer } from "zustand/middleware/immer";

export interface CompareVehicle {
  id: string;
  slug: string;
  title: string;
  make: string;
  model: string;
  year: number;
  price: number;
  mileage: number;
  transmission: string;
  fuelType: string;
  primaryImage: string;
  specs: Record<string, string | number | boolean>;
}

interface CompareState {
  // State
  vehicles: CompareVehicle[];
  maxVehicles: number;

  // Actions
  addVehicle: (vehicle: CompareVehicle) => boolean;
  removeVehicle: (vehicleId: string) => void;
  clearAll: () => void;
  isInCompare: (vehicleId: string) => boolean;
  canAddMore: () => boolean;
}

export const useCompareStore = create<CompareState>()(
  persist(
    immer((set, get) => ({
      // Initial state
      vehicles: [],
      maxVehicles: 3,

      // Actions
      addVehicle: (vehicle) => {
        const { vehicles, maxVehicles } = get();
        if (vehicles.length >= maxVehicles) {
          return false; // No se puede agregar más
        }
        if (vehicles.some((v) => v.id === vehicle.id)) {
          return false; // Ya existe
        }
        set((state) => {
          state.vehicles.push(vehicle);
        });
        return true;
      },

      removeVehicle: (vehicleId) =>
        set((state) => {
          state.vehicles = state.vehicles.filter((v) => v.id !== vehicleId);
        }),

      clearAll: () =>
        set((state) => {
          state.vehicles = [];
        }),

      isInCompare: (vehicleId) => {
        return get().vehicles.some((v) => v.id === vehicleId);
      },

      canAddMore: () => {
        const { vehicles, maxVehicles } = get();
        return vehicles.length < maxVehicles;
      },
    })),
    {
      name: "okla-compare",
      storage: createJSONStorage(() => localStorage),
    },
  ),
);

// Selectores
export const useCompareVehicles = () =>
  useCompareStore((state) => state.vehicles);
export const useCompareCount = () =>
  useCompareStore((state) => state.vehicles.length);
export const useIsInCompare = (vehicleId: string) =>
  useCompareStore((state) => state.vehicles.some((v) => v.id === vehicleId));
```

---

## 🔧 PASO 6: Store de UI

```typescript
// filepath: src/stores/useUIStore.ts
import { create } from "zustand";
import { immer } from "zustand/middleware/immer";

export interface Toast {
  id: string;
  type: "success" | "error" | "warning" | "info";
  title: string;
  message?: string;
  duration?: number;
}

export interface Modal {
  id: string;
  component: string;
  props?: Record<string, unknown>;
}

interface UIState {
  // State
  isSidebarOpen: boolean;
  isMobileMenuOpen: boolean;
  isSearchOpen: boolean;
  isFiltersOpen: boolean;
  toasts: Toast[];
  modals: Modal[];
  theme: "light" | "dark" | "system";

  // Sidebar actions
  toggleSidebar: () => void;
  setSidebarOpen: (open: boolean) => void;

  // Mobile menu actions
  toggleMobileMenu: () => void;
  setMobileMenuOpen: (open: boolean) => void;

  // Search actions
  toggleSearch: () => void;
  setSearchOpen: (open: boolean) => void;

  // Filters actions
  toggleFilters: () => void;
  setFiltersOpen: (open: boolean) => void;

  // Toast actions
  addToast: (toast: Omit<Toast, "id">) => string;
  removeToast: (id: string) => void;
  clearToasts: () => void;

  // Modal actions
  openModal: (modal: Omit<Modal, "id">) => string;
  closeModal: (id: string) => void;
  closeAllModals: () => void;

  // Theme actions
  setTheme: (theme: "light" | "dark" | "system") => void;
}

export const useUIStore = create<UIState>()(
  immer((set) => ({
    // Initial state
    isSidebarOpen: true,
    isMobileMenuOpen: false,
    isSearchOpen: false,
    isFiltersOpen: false,
    toasts: [],
    modals: [],
    theme: "light",

    // Sidebar
    toggleSidebar: () =>
      set((state) => {
        state.isSidebarOpen = !state.isSidebarOpen;
      }),
    setSidebarOpen: (open) =>
      set((state) => {
        state.isSidebarOpen = open;
      }),

    // Mobile menu
    toggleMobileMenu: () =>
      set((state) => {
        state.isMobileMenuOpen = !state.isMobileMenuOpen;
      }),
    setMobileMenuOpen: (open) =>
      set((state) => {
        state.isMobileMenuOpen = open;
      }),

    // Search
    toggleSearch: () =>
      set((state) => {
        state.isSearchOpen = !state.isSearchOpen;
      }),
    setSearchOpen: (open) =>
      set((state) => {
        state.isSearchOpen = open;
      }),

    // Filters
    toggleFilters: () =>
      set((state) => {
        state.isFiltersOpen = !state.isFiltersOpen;
      }),
    setFiltersOpen: (open) =>
      set((state) => {
        state.isFiltersOpen = open;
      }),

    // Toasts
    addToast: (toast) => {
      const id = `toast-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
      set((state) => {
        state.toasts.push({ ...toast, id });
      });
      return id;
    },
    removeToast: (id) =>
      set((state) => {
        state.toasts = state.toasts.filter((t) => t.id !== id);
      }),
    clearToasts: () =>
      set((state) => {
        state.toasts = [];
      }),

    // Modals
    openModal: (modal) => {
      const id = `modal-${Date.now()}`;
      set((state) => {
        state.modals.push({ ...modal, id });
      });
      return id;
    },
    closeModal: (id) =>
      set((state) => {
        state.modals = state.modals.filter((m) => m.id !== id);
      }),
    closeAllModals: () =>
      set((state) => {
        state.modals = [];
      }),

    // Theme
    setTheme: (theme) =>
      set((state) => {
        state.theme = theme;
      }),
  })),
);

// Selectores
export const useToasts = () => useUIStore((state) => state.toasts);
export const useModals = () => useUIStore((state) => state.modals);
export const useTheme = () => useUIStore((state) => state.theme);
export const useIsSidebarOpen = () =>
  useUIStore((state) => state.isSidebarOpen);
```

---

## 🔧 PASO 7: Store de Filtros

```typescript
// filepath: src/stores/useFiltersStore.ts
import { create } from "zustand";
import { persist, createJSONStorage } from "zustand/middleware";
import { immer } from "zustand/middleware/immer";

export interface VehicleFilters {
  // Búsqueda
  query: string;

  // Ubicación
  city: string | null;
  province: string | null;
  radius: number | null;

  // Vehículo
  make: string | null;
  model: string | null;
  yearMin: number | null;
  yearMax: number | null;
  priceMin: number | null;
  priceMax: number | null;
  mileageMax: number | null;

  // Características
  condition: "new" | "used" | "certified" | null;
  transmission: "automatic" | "manual" | "cvt" | null;
  fuelType: "gasoline" | "diesel" | "electric" | "hybrid" | null;
  bodyType: string | null;
  color: string | null;

  // Vendedor
  sellerType: "dealer" | "individual" | null;
  isVerified: boolean | null;

  // Ordenamiento
  sortBy:
    | "newest"
    | "oldest"
    | "price_asc"
    | "price_desc"
    | "mileage_asc"
    | "year_desc";

  // Paginación
  page: number;
  pageSize: number;
}

interface FiltersState {
  // State
  filters: VehicleFilters;
  savedSearches: Array<{ id: string; name: string; filters: VehicleFilters }>;

  // Actions
  setFilter: <K extends keyof VehicleFilters>(
    key: K,
    value: VehicleFilters[K],
  ) => void;
  setFilters: (filters: Partial<VehicleFilters>) => void;
  resetFilters: () => void;
  resetPage: () => void;
  saveSearch: (name: string) => void;
  loadSearch: (id: string) => void;
  deleteSearch: (id: string) => void;
  getActiveFiltersCount: () => number;
}

const defaultFilters: VehicleFilters = {
  query: "",
  city: null,
  province: null,
  radius: null,
  make: null,
  model: null,
  yearMin: null,
  yearMax: null,
  priceMin: null,
  priceMax: null,
  mileageMax: null,
  condition: null,
  transmission: null,
  fuelType: null,
  bodyType: null,
  color: null,
  sellerType: null,
  isVerified: null,
  sortBy: "newest",
  page: 1,
  pageSize: 20,
};

export const useFiltersStore = create<FiltersState>()(
  persist(
    immer((set, get) => ({
      // Initial state
      filters: { ...defaultFilters },
      savedSearches: [],

      // Actions
      setFilter: (key, value) =>
        set((state) => {
          state.filters[key] = value;
          // Reset page cuando cambian filtros (excepto paginación)
          if (key !== "page" && key !== "pageSize") {
            state.filters.page = 1;
          }
        }),

      setFilters: (filters) =>
        set((state) => {
          state.filters = { ...state.filters, ...filters, page: 1 };
        }),

      resetFilters: () =>
        set((state) => {
          state.filters = { ...defaultFilters };
        }),

      resetPage: () =>
        set((state) => {
          state.filters.page = 1;
        }),

      saveSearch: (name) =>
        set((state) => {
          const id = `search-${Date.now()}`;
          state.savedSearches.push({
            id,
            name,
            filters: { ...state.filters },
          });
        }),

      loadSearch: (id) =>
        set((state) => {
          const saved = state.savedSearches.find((s) => s.id === id);
          if (saved) {
            state.filters = { ...saved.filters };
          }
        }),

      deleteSearch: (id) =>
        set((state) => {
          state.savedSearches = state.savedSearches.filter((s) => s.id !== id);
        }),

      getActiveFiltersCount: () => {
        const { filters } = get();
        let count = 0;
        if (filters.make) count++;
        if (filters.model) count++;
        if (filters.yearMin || filters.yearMax) count++;
        if (filters.priceMin || filters.priceMax) count++;
        if (filters.mileageMax) count++;
        if (filters.condition) count++;
        if (filters.transmission) count++;
        if (filters.fuelType) count++;
        if (filters.bodyType) count++;
        if (filters.sellerType) count++;
        if (filters.city || filters.province) count++;
        return count;
      },
    })),
    {
      name: "okla-filters",
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({
        savedSearches: state.savedSearches,
      }),
    },
  ),
);

// Selectores
export const useFilters = () => useFiltersStore((state) => state.filters);
export const useSavedSearches = () =>
  useFiltersStore((state) => state.savedSearches);
export const useActiveFiltersCount = () =>
  useFiltersStore((state) => state.getActiveFiltersCount());
```

---

## 🔧 PASO 8: Hidratación SSR-Safe

```typescript
// filepath: src/lib/store-utils.ts
import { useEffect, useState } from 'react';

/**
 * Hook para manejar hidratación de stores con persistencia
 * Evita mismatch entre SSR y cliente
 */
export function useHydration() {
  const [isHydrated, setIsHydrated] = useState(false);

  useEffect(() => {
    setIsHydrated(true);
  }, []);

  return isHydrated;
}

/**
 * Wrapper para componentes que dependen de stores persistidos
 */
export function HydrationBoundary({
  children,
  fallback = null,
}: {
  children: React.ReactNode;
  fallback?: React.ReactNode;
}) {
  const isHydrated = useHydration();

  if (!isHydrated) {
    return fallback;
  }

  return <>{children}</>;
}

/**
 * Hook que retorna valor del store solo después de hidratación
 */
export function useHydratedStore<T>(
  useStore: () => T,
  fallback: T
): T {
  const isHydrated = useHydration();
  const storeValue = useStore();

  return isHydrated ? storeValue : fallback;
}
```

### Uso en Componentes

```typescript
// filepath: src/components/layout/FavoritesIndicator.tsx
'use client';

import { Heart } from 'lucide-react';
import { useFavoritesCount } from '@/stores/useFavoritesStore';
import { useHydration } from '@/lib/store-utils';

export function FavoritesIndicator() {
  const isHydrated = useHydration();
  const count = useFavoritesCount();

  return (
    <div className="relative">
      <Heart className="h-5 w-5" />
      {isHydrated && count > 0 && (
        <span className="absolute -top-1 -right-1 h-4 w-4 rounded-full bg-red-500 text-xs text-white flex items-center justify-center">
          {count}
        </span>
      )}
    </div>
  );
}
```

---

## 🔧 PASO 9: Integración con TanStack Query

```typescript
// filepath: src/lib/sync-stores.ts
import { useQueryClient } from "@tanstack/react-query";
import { useAuthStore } from "@/stores/useAuthStore";
import { useFavoritesStore } from "@/stores/useFavoritesStore";
import { useEffect } from "react";

/**
 * Hook para sincronizar favoritos entre store local y servidor
 */
export function useSyncFavorites() {
  const queryClient = useQueryClient();
  const { isAuthenticated, user } = useAuthStore();
  const { favorites, syncFromServer, setLoading } = useFavoritesStore();

  // Sincronizar al login
  useEffect(() => {
    if (isAuthenticated && user) {
      syncFavoritesFromServer();
    }
  }, [isAuthenticated, user?.id]);

  const syncFavoritesFromServer = async () => {
    setLoading(true);
    try {
      const serverFavorites = await queryClient.fetchQuery({
        queryKey: ["favorites", user?.id],
        queryFn: () => api.get("/api/favorites"),
      });
      syncFromServer(serverFavorites);
    } catch (error) {
      console.error("Error syncing favorites:", error);
    } finally {
      setLoading(false);
    }
  };

  const syncFavoritesToServer = async () => {
    if (!isAuthenticated) return;

    try {
      await api.put("/api/favorites/sync", { favorites });
      queryClient.invalidateQueries({ queryKey: ["favorites"] });
    } catch (error) {
      console.error("Error syncing favorites to server:", error);
    }
  };

  return { syncFavoritesFromServer, syncFavoritesToServer };
}

/**
 * Hook para invalidar cache cuando cambia el usuario
 */
export function useAuthSync() {
  const queryClient = useQueryClient();
  const { isAuthenticated, user } = useAuthStore();

  useEffect(() => {
    if (!isAuthenticated) {
      // Limpiar cache de datos privados al logout
      queryClient.removeQueries({ queryKey: ["favorites"] });
      queryClient.removeQueries({ queryKey: ["messages"] });
      queryClient.removeQueries({ queryKey: ["notifications"] });
      queryClient.removeQueries({ queryKey: ["user"] });
    }
  }, [isAuthenticated]);

  return null;
}
```

---

## 🔧 PASO 10: Provider Global

```typescript
// filepath: src/providers/StoreProvider.tsx
'use client';

import { useEffect } from 'react';
import { useAuthSync, useSyncFavorites } from '@/lib/sync-stores';

// Componente interno para sincronización
function StoreSyncManager() {
  useAuthSync();
  useSyncFavorites();
  return null;
}

export function StoreProvider({ children }: { children: React.ReactNode }) {
  return (
    <>
      <StoreSyncManager />
      {children}
    </>
  );
}
```

### Integración en Layout

```typescript
// filepath: src/app/layout.tsx
import { QueryProvider } from '@/providers/QueryProvider';
import { StoreProvider } from '@/providers/StoreProvider';

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="es-DO">
      <body>
        <QueryProvider>
          <StoreProvider>
            {children}
          </StoreProvider>
        </QueryProvider>
      </body>
    </html>
  );
}
```

---

## 🔧 PASO 11: DevTools

```typescript
// filepath: src/lib/devtools.ts
import { mountStoreDevtool } from "simple-zustand-devtools";
import { useAuthStore } from "@/stores/useAuthStore";
import { useFavoritesStore } from "@/stores/useFavoritesStore";
import { useCompareStore } from "@/stores/useCompareStore";
import { useUIStore } from "@/stores/useUIStore";
import { useFiltersStore } from "@/stores/useFiltersStore";

// Montar DevTools solo en desarrollo
if (process.env.NODE_ENV === "development") {
  mountStoreDevtool("AuthStore", useAuthStore);
  mountStoreDevtool("FavoritesStore", useFavoritesStore);
  mountStoreDevtool("CompareStore", useCompareStore);
  mountStoreDevtool("UIStore", useUIStore);
  mountStoreDevtool("FiltersStore", useFiltersStore);
}
```

---

## 🔧 PASO 12: Re-export Central

```typescript
// filepath: src/stores/index.ts
// Auth
export {
  useAuthStore,
  useUser,
  useIsAuthenticated,
  useAccessToken,
  useAuthLoading,
  useAuthError,
} from "./useAuthStore";
export type { User, AuthState } from "./useAuthStore";

// Favorites
export {
  useFavoritesStore,
  useFavorites,
  useFavoritesCount,
  useIsFavorite,
} from "./useFavoritesStore";
export type { FavoriteVehicle } from "./useFavoritesStore";

// Compare
export {
  useCompareStore,
  useCompareVehicles,
  useCompareCount,
  useIsInCompare,
} from "./useCompareStore";
export type { CompareVehicle } from "./useCompareStore";

// UI
export {
  useUIStore,
  useToasts,
  useModals,
  useTheme,
  useIsSidebarOpen,
} from "./useUIStore";
export type { Toast, Modal } from "./useUIStore";

// Filters
export {
  useFiltersStore,
  useFilters,
  useSavedSearches,
  useActiveFiltersCount,
} from "./useFiltersStore";
export type { VehicleFilters } from "./useFiltersStore";
```

---

## 🧪 Testing

### Vitest

```typescript
// __tests__/stores/useFavoritesStore.test.ts
import { describe, it, expect, beforeEach } from "vitest";
import { act, renderHook } from "@testing-library/react";
import { useFavoritesStore } from "@/stores/useFavoritesStore";

describe("useFavoritesStore", () => {
  beforeEach(() => {
    // Reset store antes de cada test
    useFavoritesStore.setState({ favorites: [] });
  });

  it("should add a favorite", () => {
    const { result } = renderHook(() => useFavoritesStore());

    act(() => {
      result.current.addFavorite({
        id: "1",
        slug: "toyota-camry",
        title: "Toyota Camry",
        price: 1500000,
        primaryImage: "/img.jpg",
        priceAlertEnabled: false,
      });
    });

    expect(result.current.favorites).toHaveLength(1);
    expect(result.current.favorites[0].id).toBe("1");
  });

  it("should toggle favorite", () => {
    const { result } = renderHook(() => useFavoritesStore());

    const vehicle = {
      id: "1",
      slug: "toyota-camry",
      title: "Toyota Camry",
      price: 1500000,
      primaryImage: "/img.jpg",
      priceAlertEnabled: false,
    };

    // Add
    act(() => {
      result.current.toggleFavorite(vehicle);
    });
    expect(result.current.favorites).toHaveLength(1);

    // Remove
    act(() => {
      result.current.toggleFavorite(vehicle);
    });
    expect(result.current.favorites).toHaveLength(0);
  });

  it("should check if vehicle is favorite", () => {
    const { result } = renderHook(() => useFavoritesStore());

    act(() => {
      result.current.addFavorite({
        id: "1",
        slug: "test",
        title: "Test",
        price: 100,
        primaryImage: "/img.jpg",
        priceAlertEnabled: false,
      });
    });

    expect(result.current.isFavorite("1")).toBe(true);
    expect(result.current.isFavorite("2")).toBe(false);
  });
});
```

---

## ✅ Checklist de Implementación

- [ ] Instalar zustand
- [ ] Crear estructura de stores
- [ ] Implementar useAuthStore
- [ ] Implementar useFavoritesStore
- [ ] Implementar useCompareStore
- [ ] Implementar useUIStore
- [ ] Implementar useFiltersStore
- [ ] Configurar hidratación SSR-safe
- [ ] Integrar con TanStack Query
- [ ] Configurar DevTools
- [ ] Escribir tests unitarios

---

## 🔗 Referencias

- [Zustand Documentation](https://zustand-demo.pmnd.rs/)
- [Zustand Middleware](https://docs.pmnd.rs/zustand/middlewares)
- [TanStack Query](https://tanstack.com/query)

---

_El estado global con Zustand es ligero y performante. Usar TanStack Query para server state y Zustand para client state._
