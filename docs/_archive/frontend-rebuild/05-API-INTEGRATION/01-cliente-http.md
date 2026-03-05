# 🔌 Configuración de API - Cliente HTTP y Hooks

> **Tiempo estimado:** 40 minutos
> **Prerrequisitos:** Proyecto configurado, TanStack Query instalado

---

## 📋 OBJETIVO

Configurar sistema completo de comunicación con el backend:

- Cliente HTTP con interceptores
- TanStack Query para cache y estado
- Hooks reutilizables para cada entidad
- Manejo de autenticación

---

## 🔧 PASO 1: Cliente HTTP con Axios

### Código a crear

```typescript
// filepath: src/lib/api/client.ts
import axios, { AxiosError, AxiosInstance, AxiosRequestConfig } from "axios";
import { getSession, signOut } from "next-auth/react";

// API base URL
const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_URL || "https://api.okla.com.do";

// Types
export interface ApiError {
  message: string;
  statusCode: number;
  errors?: Record<string, string[]>;
}

// Create axios instance
const apiClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
  headers: {
    "Content-Type": "application/json",
    Accept: "application/json",
  },
});

// Request interceptor - add auth token
apiClient.interceptors.request.use(
  async (config) => {
    // Get session for auth token
    const session = await getSession();

    if (session?.accessToken) {
      config.headers.Authorization = `Bearer ${session.accessToken}`;
    }

    return config;
  },
  (error) => Promise.reject(error),
);

// Response interceptor - handle errors
apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiError>) => {
    const status = error.response?.status;

    // Handle 401 - Unauthorized
    if (status === 401) {
      // Try to refresh token or logout
      await signOut({ redirect: true, callbackUrl: "/login" });
      return Promise.reject(error);
    }

    // Handle 403 - Forbidden
    if (status === 403) {
      console.error("Access forbidden:", error.response?.data?.message);
    }

    // Handle 500+ - Server errors
    if (status && status >= 500) {
      console.error("Server error:", error.response?.data?.message);
    }

    return Promise.reject(error);
  },
);

// Generic request function
export async function request<T>(config: AxiosRequestConfig): Promise<T> {
  try {
    const response = await apiClient.request<T>(config);
    return response.data;
  } catch (error) {
    if (axios.isAxiosError(error)) {
      const apiError: ApiError = {
        message:
          error.response?.data?.message || error.message || "Error desconocido",
        statusCode: error.response?.status || 500,
        errors: error.response?.data?.errors,
      };
      throw apiError;
    }
    throw error;
  }
}

// HTTP methods
export const api = {
  get: <T>(url: string, config?: AxiosRequestConfig) =>
    request<T>({ ...config, method: "GET", url }),

  post: <T>(url: string, data?: unknown, config?: AxiosRequestConfig) =>
    request<T>({ ...config, method: "POST", url, data }),

  put: <T>(url: string, data?: unknown, config?: AxiosRequestConfig) =>
    request<T>({ ...config, method: "PUT", url, data }),

  patch: <T>(url: string, data?: unknown, config?: AxiosRequestConfig) =>
    request<T>({ ...config, method: "PATCH", url, data }),

  delete: <T>(url: string, config?: AxiosRequestConfig) =>
    request<T>({ ...config, method: "DELETE", url }),
};

export default apiClient;
```

---

## 🔧 PASO 2: Configurar TanStack Query

### Provider

```tsx
// filepath: src/lib/providers/query-provider.tsx
"use client";

import * as React from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools";

function makeQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        // Stale time - datos considerados frescos por 1 minuto
        staleTime: 60 * 1000,
        // Cache time - datos en cache por 5 minutos
        gcTime: 5 * 60 * 1000,
        // Retry configuration
        retry: (failureCount, error) => {
          // No retry on 4xx errors
          if (error && typeof error === "object" && "statusCode" in error) {
            const statusCode = (error as { statusCode: number }).statusCode;
            if (statusCode >= 400 && statusCode < 500) return false;
          }
          return failureCount < 2;
        },
        retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
        // Refetch on window focus for fresh data
        refetchOnWindowFocus: true,
        // Don't refetch on mount if data is fresh
        refetchOnMount: true,
      },
      mutations: {
        retry: 1,
        retryDelay: 1000,
      },
    },
  });
}

// Create query client once
let browserQueryClient: QueryClient | undefined = undefined;

function getQueryClient() {
  if (typeof window === "undefined") {
    // Server: always make a new query client
    return makeQueryClient();
  } else {
    // Browser: make a new client if we don't already have one
    if (!browserQueryClient) browserQueryClient = makeQueryClient();
    return browserQueryClient;
  }
}

export function QueryProvider({ children }: { children: React.ReactNode }) {
  const queryClient = getQueryClient();

  return (
    <QueryClientProvider client={queryClient}>
      {children}
      {process.env.NODE_ENV === "development" && (
        <ReactQueryDevtools initialIsOpen={false} position="bottom" />
      )}
    </QueryClientProvider>
  );
}
```

### Query Keys

```typescript
// filepath: src/lib/api/query-keys.ts

/**
 * Centralized query keys for TanStack Query
 * Estructura jerárquica para invalidación fácil
 */

export const queryKeys = {
  // Auth
  auth: {
    all: ["auth"] as const,
    session: () => [...queryKeys.auth.all, "session"] as const,
    user: () => [...queryKeys.auth.all, "user"] as const,
  },

  // Vehicles
  vehicles: {
    all: ["vehicles"] as const,
    lists: () => [...queryKeys.vehicles.all, "list"] as const,
    list: (filters?: Record<string, unknown>) =>
      [...queryKeys.vehicles.lists(), filters] as const,
    details: () => [...queryKeys.vehicles.all, "detail"] as const,
    detail: (slug: string) => [...queryKeys.vehicles.details(), slug] as const,
    search: (params: Record<string, unknown>) =>
      [...queryKeys.vehicles.all, "search", params] as const,
    featured: () => [...queryKeys.vehicles.all, "featured"] as const,
    byDealer: (dealerId: string) =>
      [...queryKeys.vehicles.all, "dealer", dealerId] as const,
    byUser: (userId: string) =>
      [...queryKeys.vehicles.all, "user", userId] as const,
  },

  // Favorites
  favorites: {
    all: ["favorites"] as const,
    list: () => [...queryKeys.favorites.all, "list"] as const,
    check: (vehicleId: string) =>
      [...queryKeys.favorites.all, "check", vehicleId] as const,
  },

  // Dealers
  dealers: {
    all: ["dealers"] as const,
    lists: () => [...queryKeys.dealers.all, "list"] as const,
    list: (filters?: Record<string, unknown>) =>
      [...queryKeys.dealers.lists(), filters] as const,
    details: () => [...queryKeys.dealers.all, "detail"] as const,
    detail: (id: string) => [...queryKeys.dealers.details(), id] as const,
    bySlug: (slug: string) => [...queryKeys.dealers.all, "slug", slug] as const,
    statistics: (id: string) =>
      [...queryKeys.dealers.all, "statistics", id] as const,
  },

  // Catalog (makes, models, etc)
  catalog: {
    all: ["catalog"] as const,
    makes: () => [...queryKeys.catalog.all, "makes"] as const,
    models: (makeId: string) =>
      [...queryKeys.catalog.all, "models", makeId] as const,
    years: () => [...queryKeys.catalog.all, "years"] as const,
    bodyTypes: () => [...queryKeys.catalog.all, "bodyTypes"] as const,
    fuelTypes: () => [...queryKeys.catalog.all, "fuelTypes"] as const,
    transmissions: () => [...queryKeys.catalog.all, "transmissions"] as const,
    colors: () => [...queryKeys.catalog.all, "colors"] as const,
    provinces: () => [...queryKeys.catalog.all, "provinces"] as const,
    cities: (provinceId: string) =>
      [...queryKeys.catalog.all, "cities", provinceId] as const,
  },

  // Homepage
  homepage: {
    all: ["homepage"] as const,
    sections: () => [...queryKeys.homepage.all, "sections"] as const,
    banners: () => [...queryKeys.homepage.all, "banners"] as const,
  },

  // User
  users: {
    all: ["users"] as const,
    profile: (userId: string) =>
      [...queryKeys.users.all, "profile", userId] as const,
    settings: (userId: string) =>
      [...queryKeys.users.all, "settings", userId] as const,
  },

  // Messages
  messages: {
    all: ["messages"] as const,
    inbox: () => [...queryKeys.messages.all, "inbox"] as const,
    thread: (threadId: string) =>
      [...queryKeys.messages.all, "thread", threadId] as const,
    unreadCount: () => [...queryKeys.messages.all, "unread"] as const,
  },

  // Notifications
  notifications: {
    all: ["notifications"] as const,
    list: () => [...queryKeys.notifications.all, "list"] as const,
    unreadCount: () => [...queryKeys.notifications.all, "unread"] as const,
  },

  // Alerts
  alerts: {
    all: ["alerts"] as const,
    list: () => [...queryKeys.alerts.all, "list"] as const,
    detail: (id: string) => [...queryKeys.alerts.all, "detail", id] as const,
  },

  // Billing
  billing: {
    all: ["billing"] as const,
    subscription: () => [...queryKeys.billing.all, "subscription"] as const,
    invoices: () => [...queryKeys.billing.all, "invoices"] as const,
    plans: () => [...queryKeys.billing.all, "plans"] as const,
  },

  // Comparisons
  comparisons: {
    all: ["comparisons"] as const,
    current: () => [...queryKeys.comparisons.all, "current"] as const,
    shared: (shareId: string) =>
      [...queryKeys.comparisons.all, "shared", shareId] as const,
  },
};
```

---

## 🔧 PASO 3: API Services

### Vehicle Service

```typescript
// filepath: src/lib/api/services/vehicle-service.ts
import { api } from "../client";
import type {
  Vehicle,
  VehicleSummary,
  VehicleSearchFilters,
  PaginatedResponse,
  ApiResponse,
} from "@/types";

const ENDPOINT = "/api/vehicles";

export interface VehicleSearchParams extends VehicleSearchFilters {
  page?: number;
  pageSize?: number;
  sortBy?: string;
}

export interface CreateVehicleData {
  title: string;
  description: string;
  price: number;
  makeId: string;
  modelId: string;
  year: number;
  condition: string;
  mileage: number;
  fuelType: string;
  transmission: string;
  exteriorColor: string;
  interiorColor: string;
  bodyType: string;
  features: string[];
  city: string;
  province: string;
}

export const vehicleService = {
  /**
   * Search vehicles with filters
   */
  search: (params: VehicleSearchParams) =>
    api.get<PaginatedResponse<VehicleSummary>>(`${ENDPOINT}/search`, {
      params,
    }),

  /**
   * Get vehicle by slug
   */
  getBySlug: (slug: string) =>
    api.get<ApiResponse<Vehicle>>(`${ENDPOINT}/${slug}`),

  /**
   * Get vehicle by ID
   */
  getById: (id: string) =>
    api.get<ApiResponse<Vehicle>>(`${ENDPOINT}/id/${id}`),

  /**
   * Get featured vehicles for homepage
   */
  getFeatured: () =>
    api.get<ApiResponse<VehicleSummary[]>>(`${ENDPOINT}/featured`),

  /**
   * Get recent vehicles
   */
  getRecent: (limit = 10) =>
    api.get<ApiResponse<VehicleSummary[]>>(`${ENDPOINT}/recent`, {
      params: { limit },
    }),

  /**
   * Get vehicles by dealer
   */
  getByDealer: (dealerId: string, page = 1, pageSize = 20) =>
    api.get<PaginatedResponse<VehicleSummary>>(
      `${ENDPOINT}/dealer/${dealerId}`,
      { params: { page, pageSize } },
    ),

  /**
   * Get vehicles by current user
   */
  getMyVehicles: (page = 1, pageSize = 20) =>
    api.get<PaginatedResponse<Vehicle>>(`${ENDPOINT}/my`, {
      params: { page, pageSize },
    }),

  /**
   * Create new vehicle listing
   */
  create: (data: CreateVehicleData) =>
    api.post<ApiResponse<Vehicle>>(ENDPOINT, data),

  /**
   * Update vehicle
   */
  update: (id: string, data: Partial<CreateVehicleData>) =>
    api.put<ApiResponse<Vehicle>>(`${ENDPOINT}/${id}`, data),

  /**
   * Delete vehicle
   */
  delete: (id: string) => api.delete<ApiResponse<void>>(`${ENDPOINT}/${id}`),

  /**
   * Mark as sold
   */
  markAsSold: (id: string) =>
    api.patch<ApiResponse<Vehicle>>(`${ENDPOINT}/${id}/sold`),

  /**
   * Increment view count
   */
  incrementViews: (id: string) => api.post<void>(`${ENDPOINT}/${id}/views`),

  /**
   * Get similar vehicles
   */
  getSimilar: (id: string, limit = 4) =>
    api.get<ApiResponse<VehicleSummary[]>>(`${ENDPOINT}/${id}/similar`, {
      params: { limit },
    }),
};
```

### Catalog Service

```typescript
// filepath: src/lib/api/services/catalog-service.ts
import { api } from "../client";
import type { ApiResponse } from "@/types";

const ENDPOINT = "/api/catalog";

export interface Make {
  id: string;
  name: string;
  slug: string;
  logoUrl?: string;
  vehicleCount: number;
}

export interface Model {
  id: string;
  makeId: string;
  name: string;
  slug: string;
  vehicleCount: number;
}

export interface SelectOption {
  value: string;
  label: string;
}

export const catalogService = {
  /**
   * Get all makes
   */
  getMakes: () => api.get<ApiResponse<Make[]>>(`${ENDPOINT}/makes`),

  /**
   * Get models by make
   */
  getModels: (makeId: string) =>
    api.get<ApiResponse<Model[]>>(`${ENDPOINT}/makes/${makeId}/models`),

  /**
   * Get available years
   */
  getYears: () => api.get<ApiResponse<number[]>>(`${ENDPOINT}/years`),

  /**
   * Get body types
   */
  getBodyTypes: () =>
    api.get<ApiResponse<SelectOption[]>>(`${ENDPOINT}/body-types`),

  /**
   * Get fuel types
   */
  getFuelTypes: () =>
    api.get<ApiResponse<SelectOption[]>>(`${ENDPOINT}/fuel-types`),

  /**
   * Get transmission types
   */
  getTransmissions: () =>
    api.get<ApiResponse<SelectOption[]>>(`${ENDPOINT}/transmissions`),

  /**
   * Get exterior colors
   */
  getColors: () => api.get<ApiResponse<SelectOption[]>>(`${ENDPOINT}/colors`),

  /**
   * Get provinces
   */
  getProvinces: () =>
    api.get<ApiResponse<SelectOption[]>>(`${ENDPOINT}/provinces`),

  /**
   * Get cities by province
   */
  getCities: (provinceId: string) =>
    api.get<ApiResponse<SelectOption[]>>(
      `${ENDPOINT}/provinces/${provinceId}/cities`,
    ),
};
```

### Favorites Service

```typescript
// filepath: src/lib/api/services/favorites-service.ts
import { api } from "../client";
import type { VehicleSummary, ApiResponse, PaginatedResponse } from "@/types";

const ENDPOINT = "/api/favorites";

export interface FavoriteVehicle extends VehicleSummary {
  addedAt: string;
  note?: string;
  notifyPriceChange: boolean;
}

export const favoritesService = {
  /**
   * Get all favorites
   */
  getAll: (page = 1, pageSize = 20) =>
    api.get<PaginatedResponse<FavoriteVehicle>>(ENDPOINT, {
      params: { page, pageSize },
    }),

  /**
   * Check if vehicle is favorited
   */
  check: (vehicleId: string) =>
    api.get<ApiResponse<{ isFavorite: boolean }>>(
      `${ENDPOINT}/check/${vehicleId}`,
    ),

  /**
   * Add to favorites
   */
  add: (vehicleId: string, note?: string) =>
    api.post<ApiResponse<void>>(ENDPOINT, { vehicleId, note }),

  /**
   * Remove from favorites
   */
  remove: (vehicleId: string) =>
    api.delete<ApiResponse<void>>(`${ENDPOINT}/${vehicleId}`),

  /**
   * Toggle favorite status
   */
  toggle: (vehicleId: string) =>
    api.post<ApiResponse<{ isFavorite: boolean }>>(
      `${ENDPOINT}/toggle/${vehicleId}`,
    ),

  /**
   * Update note
   */
  updateNote: (vehicleId: string, note: string) =>
    api.patch<ApiResponse<void>>(`${ENDPOINT}/${vehicleId}/note`, { note }),

  /**
   * Toggle price change notification
   */
  toggleNotification: (vehicleId: string, notify: boolean) =>
    api.patch<ApiResponse<void>>(`${ENDPOINT}/${vehicleId}/notify`, { notify }),
};
```

---

## 🔧 PASO 4: Hooks de Query

### useVehicles Hook

```typescript
// filepath: src/lib/hooks/use-vehicles.ts
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "../api/query-keys";
import {
  vehicleService,
  VehicleSearchParams,
  CreateVehicleData,
} from "../api/services/vehicle-service";
import { useToast } from "./use-toast";

/**
 * Hook for searching vehicles
 */
export function useVehicleSearch(params: VehicleSearchParams, enabled = true) {
  return useQuery({
    queryKey: queryKeys.vehicles.search(params),
    queryFn: () => vehicleService.search(params),
    enabled,
    placeholderData: (previousData) => previousData,
  });
}

/**
 * Hook for getting single vehicle by slug
 */
export function useVehicle(slug: string, enabled = true) {
  return useQuery({
    queryKey: queryKeys.vehicles.detail(slug),
    queryFn: () => vehicleService.getBySlug(slug),
    enabled: enabled && !!slug,
  });
}

/**
 * Hook for featured vehicles (homepage)
 */
export function useFeaturedVehicles() {
  return useQuery({
    queryKey: queryKeys.vehicles.featured(),
    queryFn: () => vehicleService.getFeatured(),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Hook for user's own vehicles
 */
export function useMyVehicles(page = 1, pageSize = 20) {
  return useQuery({
    queryKey: queryKeys.vehicles.list({ mine: true, page, pageSize }),
    queryFn: () => vehicleService.getMyVehicles(page, pageSize),
  });
}

/**
 * Hook for creating vehicle
 */
export function useCreateVehicle() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (data: CreateVehicleData) => vehicleService.create(data),
    onSuccess: () => {
      // Invalidate vehicle lists
      queryClient.invalidateQueries({ queryKey: queryKeys.vehicles.lists() });

      toast.success({
        title: "¡Vehículo publicado!",
        description: "Tu vehículo ya está visible para compradores",
      });
    },
    onError: (error) => {
      toast.error({
        title: "Error al publicar",
        description:
          error instanceof Error ? error.message : "Intenta nuevamente",
      });
    },
  });
}

/**
 * Hook for updating vehicle
 */
export function useUpdateVehicle() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({
      id,
      data,
    }: {
      id: string;
      data: Partial<CreateVehicleData>;
    }) => vehicleService.update(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: queryKeys.vehicles.detail(variables.id),
      });
      queryClient.invalidateQueries({ queryKey: queryKeys.vehicles.lists() });

      toast.success({
        title: "Cambios guardados",
        description: "El vehículo se actualizó correctamente",
      });
    },
    onError: (error) => {
      toast.error({
        title: "Error al actualizar",
        description:
          error instanceof Error ? error.message : "Intenta nuevamente",
      });
    },
  });
}

/**
 * Hook for deleting vehicle
 */
export function useDeleteVehicle() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (id: string) => vehicleService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.vehicles.lists() });

      toast.success({
        title: "Vehículo eliminado",
        description: "El vehículo se eliminó correctamente",
      });
    },
    onError: (error) => {
      toast.error({
        title: "Error al eliminar",
        description:
          error instanceof Error ? error.message : "Intenta nuevamente",
      });
    },
  });
}

/**
 * Hook for similar vehicles
 */
export function useSimilarVehicles(vehicleId: string, limit = 4) {
  return useQuery({
    queryKey: ["vehicles", "similar", vehicleId, limit],
    queryFn: () => vehicleService.getSimilar(vehicleId, limit),
    enabled: !!vehicleId,
  });
}
```

### useFavorites Hook

```typescript
// filepath: src/lib/hooks/use-favorites.ts
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "../api/query-keys";
import { favoritesService } from "../api/services/favorites-service";
import { useToast } from "./use-toast";

/**
 * Hook for getting all favorites
 */
export function useFavorites(page = 1, pageSize = 20) {
  return useQuery({
    queryKey: queryKeys.favorites.list(),
    queryFn: () => favoritesService.getAll(page, pageSize),
  });
}

/**
 * Hook for checking if a vehicle is favorited
 */
export function useIsFavorite(vehicleId: string) {
  return useQuery({
    queryKey: queryKeys.favorites.check(vehicleId),
    queryFn: () => favoritesService.check(vehicleId),
    enabled: !!vehicleId,
    staleTime: 30 * 1000, // 30 seconds
  });
}

/**
 * Hook for toggling favorite status
 */
export function useToggleFavorite() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (vehicleId: string) => favoritesService.toggle(vehicleId),
    onMutate: async (vehicleId) => {
      // Optimistic update
      await queryClient.cancelQueries({
        queryKey: queryKeys.favorites.check(vehicleId),
      });

      const previousData = queryClient.getQueryData(
        queryKeys.favorites.check(vehicleId),
      );

      queryClient.setQueryData(
        queryKeys.favorites.check(vehicleId),
        (old: { data: { isFavorite: boolean } } | undefined) => ({
          ...old,
          data: { isFavorite: !old?.data?.isFavorite },
        }),
      );

      return { previousData };
    },
    onSuccess: (data) => {
      // Invalidate favorites list
      queryClient.invalidateQueries({ queryKey: queryKeys.favorites.list() });

      toast.success({
        title: data.data.isFavorite
          ? "Agregado a favoritos"
          : "Eliminado de favoritos",
      });
    },
    onError: (error, vehicleId, context) => {
      // Rollback on error
      if (context?.previousData) {
        queryClient.setQueryData(
          queryKeys.favorites.check(vehicleId),
          context.previousData,
        );
      }

      toast.error({
        title: "Error",
        description: "No se pudo actualizar favoritos",
      });
    },
  });
}

/**
 * Hook for removing from favorites
 */
export function useRemoveFavorite() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (vehicleId: string) => favoritesService.remove(vehicleId),
    onSuccess: (_, vehicleId) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.favorites.list() });
      queryClient.setQueryData(queryKeys.favorites.check(vehicleId), {
        data: { isFavorite: false },
      });

      toast.success({
        title: "Eliminado de favoritos",
      });
    },
    onError: (error) => {
      toast.error({
        title: "Error al eliminar",
        description:
          error instanceof Error ? error.message : "Intenta nuevamente",
      });
    },
  });
}
```

### useCatalog Hook

```typescript
// filepath: src/lib/hooks/use-catalog.ts
import { useQuery } from "@tanstack/react-query";
import { queryKeys } from "../api/query-keys";
import { catalogService } from "../api/services/catalog-service";

/**
 * Hook for makes (marcas)
 */
export function useMakes() {
  return useQuery({
    queryKey: queryKeys.catalog.makes(),
    queryFn: () => catalogService.getMakes(),
    staleTime: 60 * 60 * 1000, // 1 hour - rarely changes
  });
}

/**
 * Hook for models by make
 */
export function useModels(makeId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.catalog.models(makeId || ""),
    queryFn: () => catalogService.getModels(makeId!),
    enabled: !!makeId,
    staleTime: 60 * 60 * 1000, // 1 hour
  });
}

/**
 * Hook for years
 */
export function useYears() {
  return useQuery({
    queryKey: queryKeys.catalog.years(),
    queryFn: () => catalogService.getYears(),
    staleTime: 24 * 60 * 60 * 1000, // 24 hours
  });
}

/**
 * Hook for body types
 */
export function useBodyTypes() {
  return useQuery({
    queryKey: queryKeys.catalog.bodyTypes(),
    queryFn: () => catalogService.getBodyTypes(),
    staleTime: 24 * 60 * 60 * 1000, // 24 hours
  });
}

/**
 * Hook for fuel types
 */
export function useFuelTypes() {
  return useQuery({
    queryKey: queryKeys.catalog.fuelTypes(),
    queryFn: () => catalogService.getFuelTypes(),
    staleTime: 24 * 60 * 60 * 1000, // 24 hours
  });
}

/**
 * Hook for transmissions
 */
export function useTransmissions() {
  return useQuery({
    queryKey: queryKeys.catalog.transmissions(),
    queryFn: () => catalogService.getTransmissions(),
    staleTime: 24 * 60 * 60 * 1000, // 24 hours
  });
}

/**
 * Hook for colors
 */
export function useColors() {
  return useQuery({
    queryKey: queryKeys.catalog.colors(),
    queryFn: () => catalogService.getColors(),
    staleTime: 24 * 60 * 60 * 1000, // 24 hours
  });
}

/**
 * Hook for provinces
 */
export function useProvinces() {
  return useQuery({
    queryKey: queryKeys.catalog.provinces(),
    queryFn: () => catalogService.getProvinces(),
    staleTime: 24 * 60 * 60 * 1000, // 24 hours
  });
}

/**
 * Hook for cities by province
 */
export function useCities(provinceId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.catalog.cities(provinceId || ""),
    queryFn: () => catalogService.getCities(provinceId!),
    enabled: !!provinceId,
    staleTime: 24 * 60 * 60 * 1000, // 24 hours
  });
}

/**
 * Composite hook for vehicle form dropdowns
 */
export function useVehicleFormCatalog() {
  const makes = useMakes();
  const bodyTypes = useBodyTypes();
  const fuelTypes = useFuelTypes();
  const transmissions = useTransmissions();
  const colors = useColors();
  const provinces = useProvinces();

  return {
    makes: makes.data?.data || [],
    bodyTypes: bodyTypes.data?.data || [],
    fuelTypes: fuelTypes.data?.data || [],
    transmissions: transmissions.data?.data || [],
    colors: colors.data?.data || [],
    provinces: provinces.data?.data || [],
    isLoading:
      makes.isLoading ||
      bodyTypes.isLoading ||
      fuelTypes.isLoading ||
      transmissions.isLoading ||
      colors.isLoading ||
      provinces.isLoading,
    isError:
      makes.isError ||
      bodyTypes.isError ||
      fuelTypes.isError ||
      transmissions.isError ||
      colors.isError ||
      provinces.isError,
  };
}
```

---

## 🔧 PASO 5: Re-exportar todos los hooks

```typescript
// filepath: src/lib/hooks/index.ts

// Toast
export { useToast } from "./use-toast";

// Vehicles
export {
  useVehicleSearch,
  useVehicle,
  useFeaturedVehicles,
  useMyVehicles,
  useCreateVehicle,
  useUpdateVehicle,
  useDeleteVehicle,
  useSimilarVehicles,
} from "./use-vehicles";

// Favorites
export {
  useFavorites,
  useIsFavorite,
  useToggleFavorite,
  useRemoveFavorite,
} from "./use-favorites";

// Catalog
export {
  useMakes,
  useModels,
  useYears,
  useBodyTypes,
  useFuelTypes,
  useTransmissions,
  useColors,
  useProvinces,
  useCities,
  useVehicleFormCatalog,
} from "./use-catalog";
```

---

## ✅ VALIDACIÓN

### Test de hooks

```typescript
// filepath: __tests__/hooks/use-vehicles.test.ts
import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderHook, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useVehicleSearch, useFeaturedVehicles } from "@/lib/hooks";

// Mock the API
vi.mock("@/lib/api/services/vehicle-service", () => ({
  vehicleService: {
    search: vi.fn(),
    getFeatured: vi.fn(),
  },
}));

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
    },
  });
  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}

describe("useVehicleSearch", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("fetches vehicles with params", async () => {
    const { vehicleService } = await import("@/lib/api/services/vehicle-service");

    vi.mocked(vehicleService.search).mockResolvedValue({
      data: [{ id: "1", title: "Test Vehicle" }],
      pagination: { page: 1, pageSize: 20, totalItems: 1, totalPages: 1 },
    });

    const { result } = renderHook(
      () => useVehicleSearch({ page: 1, pageSize: 20 }),
      { wrapper: createWrapper() }
    );

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.data?.data).toHaveLength(1);
  });
});

describe("useFeaturedVehicles", () => {
  it("fetches featured vehicles", async () => {
    const { vehicleService } = await import("@/lib/api/services/vehicle-service");

    vi.mocked(vehicleService.getFeatured).mockResolvedValue({
      data: [
        { id: "1", title: "Featured 1" },
        { id: "2", title: "Featured 2" },
      ],
    });

    const { result } = renderHook(() => useFeaturedVehicles(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.data?.data).toHaveLength(2);
  });
});
```

### Ejecutar tests

```bash
pnpm test __tests__/hooks/

# Output esperado:
# ✓ __tests__/hooks/use-vehicles.test.ts (2)
```

---

## 📊 RESUMEN

| Archivo                            | Propósito                      |
| ---------------------------------- | ------------------------------ |
| `lib/api/client.ts`                | Cliente HTTP con interceptores |
| `lib/providers/query-provider.tsx` | Provider de TanStack Query     |
| `lib/api/query-keys.ts`            | Keys centralizados para cache  |
| `lib/api/services/*.ts`            | Servicios de API por entidad   |
| `lib/hooks/*.ts`                   | Hooks de React Query           |

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/05-API-INTEGRATION/02-autenticacion.md`
