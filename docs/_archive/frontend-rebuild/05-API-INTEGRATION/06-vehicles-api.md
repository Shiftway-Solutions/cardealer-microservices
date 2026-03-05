# 🚗 Integración API - Vehicles Service

**Fecha:** Enero 30, 2026  
**Propósito:** Documentación completa del VehiclesSaleService para integración frontend  
**Endpoints Documentados:** 33 endpoints

---

## 📋 ÍNDICE

1. [Cliente HTTP Base](#1-cliente-http-base)
2. [Tipos TypeScript](#2-tipos-typescript)
3. [Vehicles Service](#3-vehicles-service)
4. [Catalog Service](#4-catalog-service)
5. [Favorites Service](#5-favorites-service)
6. [Homepage Sections Service](#6-homepage-sections-service)
7. [Hooks de React Query](#7-hooks-de-react-query)
8. [Componentes de Ejemplo](#8-componentes-de-ejemplo)

---

## 1. Cliente HTTP Base

Utiliza el cliente HTTP configurado en [01-cliente-http.md](01-cliente-http.md).

```typescript
// filepath: src/services/api/apiClient.ts
import axios from "axios";

const apiClient = axios.create({
  baseURL: process.env.REACT_APP_API_URL || "https://api.okla.com.do",
  headers: {
    "Content-Type": "application/json",
  },
});

// Interceptor para agregar token automáticamente
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem("accessToken");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export default apiClient;
```

---

## 2. Tipos TypeScript

```typescript
// filepath: src/types/vehicle.ts

export enum VehicleStatus {
  Draft = "Draft",
  Published = "Published",
  Sold = "Sold",
  Archived = "Archived",
}

export enum VehicleCondition {
  New = "New",
  Used = "Used",
  Certified = "Certified",
}

export enum FuelType {
  Gasoline = "Gasoline",
  Diesel = "Diesel",
  Electric = "Electric",
  Hybrid = "Hybrid",
  PlugInHybrid = "PlugInHybrid",
}

export enum TransmissionType {
  Manual = "Manual",
  Automatic = "Automatic",
  CVT = "CVT",
  SemiAutomatic = "SemiAutomatic",
}

export enum DriveType {
  FWD = "FWD",
  RWD = "RWD",
  AWD = "AWD",
  FourWD = "4WD",
}

export enum BodyStyle {
  Sedan = "Sedan",
  SUV = "SUV",
  Truck = "Truck",
  Coupe = "Coupe",
  Convertible = "Convertible",
  Hatchback = "Hatchback",
  Van = "Van",
  Wagon = "Wagon",
}

export interface Vehicle {
  id: string;
  vin: string;
  make: string;
  model: string;
  year: number;
  trim?: string;
  bodyStyle: BodyStyle;
  exteriorColor: string;
  interiorColor: string;
  mileage: number;
  price: number;
  condition: VehicleCondition;
  fuelType: FuelType;
  transmission: TransmissionType;
  driveType: DriveType;
  engineSize: string;
  cylinders?: number;
  horsePower?: number;
  cityMPG?: number;
  highwayMPG?: number;
  description: string;
  features: string[];
  images: VehicleImage[];
  thumbnailUrl?: string;
  status: VehicleStatus;
  isFeatured: boolean;
  isCertified: boolean;
  hasCleanTitle: boolean;
  dealerId: string;
  dealerName?: string;
  locationCity: string;
  locationState: string;
  locationZipCode: string;
  viewCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface VehicleImage {
  id: string;
  url: string;
  thumbnailUrl: string;
  displayOrder: number;
  isPrimary: boolean;
  altText?: string;
}

export interface VehicleSearchRequest {
  search?: string;
  categoryId?: string;
  make?: string;
  model?: string;
  minYear?: number;
  maxYear?: number;
  minPrice?: number;
  maxPrice?: number;
  maxMileage?: number;
  vehicleType?: string;
  bodyStyle?: BodyStyle;
  fuelType?: FuelType;
  transmission?: TransmissionType;
  driveType?: DriveType;
  condition?: VehicleCondition;
  exteriorColor?: string;
  state?: string;
  city?: string;
  zipCode?: string;
  isCertified?: boolean;
  hasCleanTitle?: boolean;
  page: number;
  pageSize: number;
  sortBy?: string;
  sortDescending?: boolean;
}

export interface VehicleSearchResult {
  vehicles: Vehicle[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface CreateVehicleRequest {
  vin: string;
  make: string;
  model: string;
  year: number;
  trim?: string;
  bodyStyle: BodyStyle;
  exteriorColor: string;
  interiorColor: string;
  mileage: number;
  price: number;
  condition: VehicleCondition;
  fuelType: FuelType;
  transmission: TransmissionType;
  driveType: DriveType;
  engineSize: string;
  cylinders?: number;
  horsePower?: number;
  cityMPG?: number;
  highwayMPG?: number;
  description: string;
  features: string[];
  isCertified: boolean;
  hasCleanTitle: boolean;
  locationCity: string;
  locationState: string;
  locationZipCode: string;
}

export interface CompareVehiclesRequest {
  vehicleIds: string[];
}

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
  makeName: string;
  name: string;
  slug: string;
  vehicleCount: number;
}

export interface Favorite {
  id: string;
  userId: string;
  vehicleId: string;
  vehicle: Vehicle;
  notes?: string;
  notifyOnPriceChange: boolean;
  createdAt: string;
}
```

---

## 3. Vehicles Service

### 3.1 Búsqueda y Listado

#### GET /api/vehicles - Buscar vehículos con filtros

```typescript
// filepath: src/services/api/vehiclesService.ts

export const vehiclesService = {
  /**
   * Buscar vehículos con filtros y paginación
   */
  async search(params: VehicleSearchRequest): Promise<VehicleSearchResult> {
    const response = await apiClient.get<VehicleSearchResult>("/vehicles", {
      params,
    });
    return response.data;
  },

  /**
   * Obtener vehículo por ID
   */
  async getById(id: string): Promise<Vehicle> {
    const response = await apiClient.get<Vehicle>(`/vehicles/${id}`);
    return response.data;
  },

  /**
   * Obtener vehículo por VIN
   */
  async getByVIN(vin: string): Promise<Vehicle> {
    const response = await apiClient.get<Vehicle>(`/vehicles/vin/${vin}`);
    return response.data;
  },

  /**
   * Obtener vehículos destacados
   */
  async getFeatured(take: number = 10): Promise<Vehicle[]> {
    const response = await apiClient.get<Vehicle[]>("/vehicles/featured", {
      params: { take },
    });
    return response.data;
  },

  /**
   * Obtener vehículos de un dealer
   */
  async getByDealer(dealerId: string): Promise<Vehicle[]> {
    const response = await apiClient.get<Vehicle[]>(
      `/vehicles/dealer/${dealerId}`,
    );
    return response.data;
  },

  /**
   * Comparar múltiples vehículos
   */
  async compare(vehicleIds: string[]): Promise<Vehicle[]> {
    const response = await apiClient.post<Vehicle[]>("/vehicles/compare", {
      vehicleIds,
    });
    return response.data;
  },
};
```

### 3.2 CRUD de Vehículos

```typescript
// filepath: src/services/api/vehiclesService.ts (continued)

export const vehiclesService = {
  // ... métodos anteriores

  /**
   * Crear nuevo vehículo
   * Requiere autenticación: Dealer o Admin
   */
  async create(data: CreateVehicleRequest): Promise<Vehicle> {
    const response = await apiClient.post<Vehicle>("/vehicles", data);
    return response.data;
  },

  /**
   * Actualizar vehículo existente
   * Requiere autenticación: Dealer (propietario) o Admin
   */
  async update(
    id: string,
    data: Partial<CreateVehicleRequest>,
  ): Promise<Vehicle> {
    const response = await apiClient.put<Vehicle>(`/vehicles/${id}`, data);
    return response.data;
  },

  /**
   * Eliminar vehículo (soft delete)
   * Requiere autenticación: Dealer (propietario) o Admin
   */
  async delete(id: string): Promise<void> {
    await apiClient.delete(`/vehicles/${id}`);
  },

  /**
   * Publicar vehículo (cambiar status a Published)
   * Requiere autenticación: Dealer (propietario) o Admin
   */
  async publish(id: string): Promise<Vehicle> {
    const response = await apiClient.post<Vehicle>(`/vehicles/${id}/publish`);
    return response.data;
  },

  /**
   * Despublicar vehículo (cambiar status a Draft)
   * Requiere autenticación: Dealer (propietario) o Admin
   */
  async unpublish(id: string): Promise<Vehicle> {
    const response = await apiClient.post<Vehicle>(`/vehicles/${id}/unpublish`);
    return response.data;
  },

  /**
   * Marcar vehículo como vendido
   * Requiere autenticación: Dealer (propietario) o Admin
   */
  async markAsSold(id: string): Promise<Vehicle> {
    const response = await apiClient.post<Vehicle>(`/vehicles/${id}/sold`);
    return response.data;
  },

  /**
   * Destacar/quitar destacado de vehículo
   * Requiere autenticación: Admin
   */
  async toggleFeature(id: string): Promise<Vehicle> {
    const response = await apiClient.post<Vehicle>(`/vehicles/${id}/feature`);
    return response.data;
  },

  /**
   * Incrementar contador de vistas
   * No requiere autenticación
   */
  async incrementViews(id: string): Promise<void> {
    await apiClient.post(`/vehicles/${id}/views`);
  },

  /**
   * Agregar imágenes a un vehículo
   * Requiere autenticación: Dealer (propietario) o Admin
   */
  async addImages(id: string, imageIds: string[]): Promise<Vehicle> {
    const response = await apiClient.post<Vehicle>(`/vehicles/${id}/images`, {
      imageIds,
    });
    return response.data;
  },

  /**
   * Subir múltiples imágenes en batch
   * Requiere autenticación: Dealer o Admin
   */
  async bulkUploadImages(
    vehicleId: string,
    files: File[],
  ): Promise<VehicleImage[]> {
    const formData = new FormData();
    formData.append("vehicleId", vehicleId);
    files.forEach((file, index) => {
      formData.append(`files`, file);
    });

    const response = await apiClient.post<VehicleImage[]>(
      "/vehicles/bulk-images",
      formData,
      {
        headers: {
          "Content-Type": "multipart/form-data",
        },
      },
    );
    return response.data;
  },
};
```

---

## 4. Catalog Service

### 4.1 Catálogo de Marcas y Modelos

```typescript
// filepath: src/services/api/catalogService.ts

export const catalogService = {
  /**
   * Obtener todas las marcas
   */
  async getMakes(): Promise<Make[]> {
    const response = await apiClient.get<Make[]>("/catalog/makes");
    return response.data;
  },

  /**
   * Obtener marcas populares
   */
  async getPopularMakes(limit: number = 20): Promise<Make[]> {
    const response = await apiClient.get<Make[]>("/catalog/makes/popular", {
      params: { limit },
    });
    return response.data;
  },

  /**
   * Buscar marcas por nombre
   */
  async searchMakes(query: string): Promise<Make[]> {
    const response = await apiClient.get<Make[]>("/catalog/makes/search", {
      params: { q: query },
    });
    return response.data;
  },

  /**
   * Obtener modelos de una marca (por slug)
   */
  async getModelsByMakeSlug(makeSlug: string): Promise<Model[]> {
    const response = await apiClient.get<Model[]>(
      `/catalog/makes/${makeSlug}/models`,
    );
    return response.data;
  },

  /**
   * Buscar modelos de una marca
   */
  async searchModels(makeId: string, query: string): Promise<Model[]> {
    const response = await apiClient.get<Model[]>(
      `/catalog/makes/${makeId}/models/search`,
      {
        params: { q: query },
      },
    );
    return response.data;
  },

  /**
   * Obtener años disponibles para un modelo
   */
  async getYearsByModel(modelId: string): Promise<number[]> {
    const response = await apiClient.get<number[]>(
      `/catalog/models/${modelId}/years`,
    );
    return response.data;
  },

  /**
   * Obtener trims de un modelo y año específico
   */
  async getTrimsByModelAndYear(
    modelId: string,
    year: number,
  ): Promise<string[]> {
    const response = await apiClient.get<string[]>(
      `/catalog/models/${modelId}/years/${year}/trims`,
    );
    return response.data;
  },

  /**
   * Obtener información de un trim específico
   */
  async getTrimById(trimId: string): Promise<any> {
    const response = await apiClient.get(`/catalog/trims/${trimId}`);
    return response.data;
  },

  /**
   * Decodificar VIN y obtener información del vehículo
   */
  async decodeVIN(vin: string): Promise<any> {
    const response = await apiClient.get(`/catalog/vin/${vin}/decode`);
    return response.data;
  },

  /**
   * Obtener estadísticas del catálogo
   * Requiere autenticación: Admin
   */
  async getStats(): Promise<any> {
    const response = await apiClient.get("/catalog/stats");
    return response.data;
  },
};
```

---

## 5. Favorites Service

```typescript
// filepath: src/services/api/favoritesService.ts

export interface CreateFavoriteRequest {
  vehicleId: string;
  notes?: string;
  notifyOnPriceChange?: boolean;
}

export interface UpdateFavoriteRequest {
  notes?: string;
  notifyOnPriceChange?: boolean;
}

export const favoritesService = {
  /**
   * Obtener favoritos del usuario autenticado
   * Requiere autenticación
   */
  async getFavorites(): Promise<Favorite[]> {
    const response = await apiClient.get<Favorite[]>("/favorites");
    return response.data;
  },

  /**
   * Obtener cantidad de favoritos
   * Requiere autenticación
   */
  async getCount(): Promise<number> {
    const response = await apiClient.get<{ count: number }>("/favorites/count");
    return response.data.count;
  },

  /**
   * Verificar si un vehículo está en favoritos
   * Requiere autenticación
   */
  async isFavorite(vehicleId: string): Promise<boolean> {
    const response = await apiClient.get<{ isFavorite: boolean }>(
      `/favorites/check/${vehicleId}`,
    );
    return response.data.isFavorite;
  },

  /**
   * Agregar vehículo a favoritos
   * Requiere autenticación
   */
  async addFavorite(data: CreateFavoriteRequest): Promise<Favorite> {
    const response = await apiClient.post<Favorite>(
      `/favorites/${data.vehicleId}`,
      data,
    );
    return response.data;
  },

  /**
   * Eliminar de favoritos
   * Requiere autenticación
   */
  async removeFavorite(vehicleId: string): Promise<void> {
    await apiClient.delete(`/favorites/${vehicleId}`);
  },

  /**
   * Actualizar favorito (notas, notificaciones)
   * Requiere autenticación
   */
  async updateFavorite(
    vehicleId: string,
    data: UpdateFavoriteRequest,
  ): Promise<Favorite> {
    const response = await apiClient.put<Favorite>(
      `/favorites/${vehicleId}`,
      data,
    );
    return response.data;
  },
};
```

---

## 6. Homepage Sections Service

```typescript
// filepath: src/services/api/homepageSectionsService.ts

export interface HomepageSection {
  name: string;
  slug: string;
  subtitle?: string;
  displayOrder: number;
  maxItems: number;
  isActive: boolean;
  accentColor?: string;
  viewAllHref?: string;
  vehicles: Vehicle[];
}

export const homepageSectionsService = {
  /**
   * Obtener todas las secciones activas del homepage con sus vehículos
   * No requiere autenticación
   */
  async getHomepage(): Promise<HomepageSection[]> {
    const response = await apiClient.get<HomepageSection[]>(
      "/homepagesections/homepage",
    );
    return response.data;
  },
};
```

---

## 7. Hooks de React Query

### 7.1 Vehicles Hooks

```typescript
// filepath: src/hooks/useVehicles.ts
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { vehiclesService } from "@/services/api/vehiclesService";
import type {
  VehicleSearchRequest,
  CreateVehicleRequest,
} from "@/types/vehicle";

export const useVehicleSearch = (params: VehicleSearchRequest) => {
  return useQuery({
    queryKey: ["vehicles", "search", params],
    queryFn: () => vehiclesService.search(params),
    keepPreviousData: true,
  });
};

export const useVehicle = (id: string) => {
  return useQuery({
    queryKey: ["vehicles", id],
    queryFn: () => vehiclesService.getById(id),
    enabled: !!id,
  });
};

export const useFeaturedVehicles = (take: number = 10) => {
  return useQuery({
    queryKey: ["vehicles", "featured", take],
    queryFn: () => vehiclesService.getFeatured(take),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

export const useCompareVehicles = (vehicleIds: string[]) => {
  return useQuery({
    queryKey: ["vehicles", "compare", vehicleIds],
    queryFn: () => vehiclesService.compare(vehicleIds),
    enabled: vehicleIds.length > 0,
  });
};

export const useCreateVehicle = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateVehicleRequest) => vehiclesService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries(["vehicles"]);
    },
  });
};

export const useUpdateVehicle = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      id,
      data,
    }: {
      id: string;
      data: Partial<CreateVehicleRequest>;
    }) => vehiclesService.update(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries(["vehicles", variables.id]);
      queryClient.invalidateQueries(["vehicles", "search"]);
    },
  });
};

export const useDeleteVehicle = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => vehiclesService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries(["vehicles"]);
    },
  });
};

export const usePublishVehicle = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => vehiclesService.publish(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries(["vehicles", id]);
    },
  });
};

export const useIncrementViews = () => {
  return useMutation({
    mutationFn: (id: string) => vehiclesService.incrementViews(id),
  });
};
```

### 7.2 Catalog Hooks

```typescript
// filepath: src/hooks/useCatalog.ts
import { useQuery } from "@tanstack/react-query";
import { catalogService } from "@/services/api/catalogService";

export const useMakes = () => {
  return useQuery({
    queryKey: ["catalog", "makes"],
    queryFn: () => catalogService.getMakes(),
    staleTime: 60 * 60 * 1000, // 1 hour
  });
};

export const usePopularMakes = (limit: number = 20) => {
  return useQuery({
    queryKey: ["catalog", "makes", "popular", limit],
    queryFn: () => catalogService.getPopularMakes(limit),
    staleTime: 60 * 60 * 1000,
  });
};

export const useModelsByMake = (makeSlug: string) => {
  return useQuery({
    queryKey: ["catalog", "models", makeSlug],
    queryFn: () => catalogService.getModelsByMakeSlug(makeSlug),
    enabled: !!makeSlug,
    staleTime: 60 * 60 * 1000,
  });
};

export const useDecodeVIN = (vin: string) => {
  return useQuery({
    queryKey: ["catalog", "vin", vin],
    queryFn: () => catalogService.decodeVIN(vin),
    enabled: vin.length === 17,
    staleTime: Infinity, // VIN decode result never changes
  });
};
```

### 7.3 Favorites Hooks

```typescript
// filepath: src/hooks/useFavorites.ts
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { favoritesService } from "@/services/api/favoritesService";

export const useFavorites = () => {
  return useQuery({
    queryKey: ["favorites"],
    queryFn: () => favoritesService.getFavorites(),
  });
};

export const useIsFavorite = (vehicleId: string) => {
  return useQuery({
    queryKey: ["favorites", "check", vehicleId],
    queryFn: () => favoritesService.isFavorite(vehicleId),
    enabled: !!vehicleId,
  });
};

export const useAddFavorite = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: favoritesService.addFavorite,
    onSuccess: () => {
      queryClient.invalidateQueries(["favorites"]);
    },
  });
};

export const useRemoveFavorite = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (vehicleId: string) =>
      favoritesService.removeFavorite(vehicleId),
    onSuccess: () => {
      queryClient.invalidateQueries(["favorites"]);
    },
  });
};
```

### 7.4 Homepage Sections Hook

```typescript
// filepath: src/hooks/useHomepageSections.ts
import { useQuery } from "@tanstack/react-query";
import { homepageSectionsService } from "@/services/api/homepageSectionsService";

export const useHomepageSections = () => {
  return useQuery({
    queryKey: ["homepage", "sections"],
    queryFn: () => homepageSectionsService.getHomepage(),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};
```

---

## 8. Componentes de Ejemplo

### 8.1 Página de Búsqueda de Vehículos

```typescript
// filepath: src/pages/VehicleSearchPage.tsx
import React, { useState } from 'react';
import { useVehicleSearch } from '@/hooks/useVehicles';
import { useMakes } from '@/hooks/useCatalog';
import type { VehicleSearchRequest } from '@/types/vehicle';

export const VehicleSearchPage: React.FC = () => {
  const [filters, setFilters] = useState<VehicleSearchRequest>({
    page: 1,
    pageSize: 20,
  });

  const { data: searchResult, isLoading } = useVehicleSearch(filters);
  const { data: makes } = useMakes();

  const handleFilterChange = (key: string, value: any) => {
    setFilters((prev) => ({ ...prev, [key]: value, page: 1 }));
  };

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-3xl font-bold mb-8">Buscar Vehículos</h1>

      {/* Filtros */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-8">
        <select
          onChange={(e) => handleFilterChange('make', e.target.value)}
          className="px-4 py-2 border rounded"
        >
          <option value="">Todas las marcas</option>
          {makes?.map((make) => (
            <option key={make.id} value={make.name}>
              {make.name}
            </option>
          ))}
        </select>

        <input
          type="number"
          placeholder="Año mínimo"
          onChange={(e) => handleFilterChange('minYear', Number(e.target.value))}
          className="px-4 py-2 border rounded"
        />

        <input
          type="number"
          placeholder="Precio máximo"
          onChange={(e) => handleFilterChange('maxPrice', Number(e.target.value))}
          className="px-4 py-2 border rounded"
        />

        <button
          onClick={() => setFilters({ page: 1, pageSize: 20 })}
          className="px-4 py-2 bg-gray-200 rounded"
        >
          Limpiar filtros
        </button>
      </div>

      {/* Resultados */}
      {isLoading ? (
        <div>Cargando...</div>
      ) : (
        <>
          <div className="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-4 gap-6">
            {searchResult?.vehicles.map((vehicle) => (
              <VehicleCard key={vehicle.id} vehicle={vehicle} />
            ))}
          </div>

          {/* Paginación */}
          <div className="flex justify-center gap-2 mt-8">
            {Array.from({ length: searchResult?.totalPages || 0 }, (_, i) => (
              <button
                key={i + 1}
                onClick={() => handleFilterChange('page', i + 1)}
                className={`px-4 py-2 rounded ${
                  filters.page === i + 1 ? 'bg-blue-600 text-white' : 'bg-gray-200'
                }`}
              >
                {i + 1}
              </button>
            ))}
          </div>
        </>
      )}
    </div>
  );
};

// Componente auxiliar
const VehicleCard: React.FC<{ vehicle: Vehicle }> = ({ vehicle }) => {
  return (
    <div className="border rounded-lg overflow-hidden hover:shadow-lg transition">
      <img
        src={vehicle.thumbnailUrl || '/placeholder.jpg'}
        alt={`${vehicle.year} ${vehicle.make} ${vehicle.model}`}
        className="w-full h-48 object-cover"
      />
      <div className="p-4">
        <h3 className="font-bold text-lg">
          {vehicle.year} {vehicle.make} {vehicle.model}
        </h3>
        <p className="text-gray-600">{vehicle.mileage.toLocaleString()} km</p>
        <p className="text-xl font-bold text-blue-600 mt-2">
          ${vehicle.price.toLocaleString()}
        </p>
        <p className="text-sm text-gray-500 mt-1">
          {vehicle.locationCity}, {vehicle.locationState}
        </p>
      </div>
    </div>
  );
};
```

### 8.2 Detalle de Vehículo

```typescript
// filepath: src/pages/VehicleDetailPage.tsx
import React, { useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { useVehicle, useIncrementViews } from '@/hooks/useVehicles';
import { useAddFavorite, useRemoveFavorite, useIsFavorite } from '@/hooks/useFavorites';

export const VehicleDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const { data: vehicle, isLoading } = useVehicle(id!);
  const { data: isFavorite } = useIsFavorite(id!);
  const incrementViews = useIncrementViews();
  const addFavorite = useAddFavorite();
  const removeFavorite = useRemoveFavorite();

  useEffect(() => {
    if (id) {
      incrementViews.mutate(id);
    }
  }, [id]);

  const toggleFavorite = async () => {
    if (isFavorite) {
      await removeFavorite.mutateAsync(id!);
    } else {
      await addFavorite.mutateAsync({ vehicleId: id! });
    }
  };

  if (isLoading) return <div>Cargando...</div>;
  if (!vehicle) return <div>Vehículo no encontrado</div>;

  return (
    <div className="container mx-auto px-4 py-8">
      {/* Galería de imágenes */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8 mb-8">
        <div>
          <img
            src={vehicle.images[0]?.url || '/placeholder.jpg'}
            alt={`${vehicle.year} ${vehicle.make} ${vehicle.model}`}
            className="w-full rounded-lg"
          />
        </div>

        <div>
          <h1 className="text-3xl font-bold mb-4">
            {vehicle.year} {vehicle.make} {vehicle.model}
          </h1>
          <p className="text-2xl font-bold text-blue-600 mb-4">
            ${vehicle.price.toLocaleString()}
          </p>

          <div className="flex gap-4 mb-6">
            <button
              onClick={toggleFavorite}
              className={`px-6 py-3 rounded ${
                isFavorite ? 'bg-red-600 text-white' : 'bg-gray-200'
              }`}
            >
              {isFavorite ? '❤️ Guardado' : '🤍 Guardar'}
            </button>
            <button className="px-6 py-3 bg-green-600 text-white rounded">
              Contactar Vendedor
            </button>
          </div>

          <div className="space-y-2 text-sm">
            <p>
              <strong>VIN:</strong> {vehicle.vin}
            </p>
            <p>
              <strong>Kilometraje:</strong> {vehicle.mileage.toLocaleString()} km
            </p>
            <p>
              <strong>Transmisión:</strong> {vehicle.transmission}
            </p>
            <p>
              <strong>Combustible:</strong> {vehicle.fuelType}
            </p>
            <p>
              <strong>Color:</strong> {vehicle.exteriorColor}
            </p>
          </div>
        </div>
      </div>

      {/* Descripción y características */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        <div className="lg:col-span-2">
          <h2 className="text-2xl font-bold mb-4">Descripción</h2>
          <p className="text-gray-700 whitespace-pre-wrap">{vehicle.description}</p>

          <h2 className="text-2xl font-bold mt-8 mb-4">Características</h2>
          <ul className="grid grid-cols-2 gap-2">
            {vehicle.features.map((feature, index) => (
              <li key={index} className="flex items-center gap-2">
                <span className="text-green-600">✓</span>
                {feature}
              </li>
            ))}
          </ul>
        </div>

        <div>
          <div className="border rounded-lg p-6">
            <h3 className="text-xl font-bold mb-4">Ubicación</h3>
            <p>
              {vehicle.locationCity}, {vehicle.locationState}
            </p>
            <p className="text-sm text-gray-500">{vehicle.locationZipCode}</p>

            <h3 className="text-xl font-bold mt-6 mb-4">Dealer</h3>
            <p className="font-semibold">{vehicle.dealerName}</p>

            <button className="w-full mt-4 px-4 py-2 bg-blue-600 text-white rounded">
              Ver más vehículos de este dealer
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
```

---

## 🎯 Resumen de Endpoints Documentados

### Vehicles Controller (15 endpoints)

| Método   | Endpoint                       | Autenticación | Descripción                  |
| -------- | ------------------------------ | ------------- | ---------------------------- |
| `GET`    | `/api/vehicles`                | No            | Buscar con filtros           |
| `GET`    | `/api/vehicles/{id}`           | No            | Obtener por ID               |
| `GET`    | `/api/vehicles/vin/{vin}`      | No            | Obtener por VIN              |
| `GET`    | `/api/vehicles/featured`       | No            | Vehículos destacados         |
| `GET`    | `/api/vehicles/dealer/{id}`    | No            | Vehículos de un dealer       |
| `POST`   | `/api/vehicles/compare`        | No            | Comparar múltiples vehículos |
| `POST`   | `/api/vehicles`                | ✅ Dealer     | Crear vehículo               |
| `PUT`    | `/api/vehicles/{id}`           | ✅ Dealer     | Actualizar vehículo          |
| `DELETE` | `/api/vehicles/{id}`           | ✅ Dealer     | Eliminar vehículo            |
| `POST`   | `/api/vehicles/{id}/publish`   | ✅ Dealer     | Publicar vehículo            |
| `POST`   | `/api/vehicles/{id}/unpublish` | ✅ Dealer     | Despublicar vehículo         |
| `POST`   | `/api/vehicles/{id}/sold`      | ✅ Dealer     | Marcar como vendido          |
| `POST`   | `/api/vehicles/{id}/feature`   | ✅ Admin      | Destacar/quitar destacado    |
| `POST`   | `/api/vehicles/{id}/views`     | No            | Incrementar vistas           |
| `POST`   | `/api/vehicles/{id}/images`    | ✅ Dealer     | Agregar imágenes             |
| `POST`   | `/api/vehicles/bulk-images`    | ✅ Dealer     | Upload masivo de imágenes    |

### Catalog Controller (11 endpoints)

| Método | Endpoint                                      | Descripción              |
| ------ | --------------------------------------------- | ------------------------ |
| `GET`  | `/api/catalog/makes`                          | Todas las marcas         |
| `GET`  | `/api/catalog/makes/popular`                  | Marcas populares         |
| `GET`  | `/api/catalog/makes/search`                   | Buscar marcas            |
| `GET`  | `/api/catalog/makes/{slug}/models`            | Modelos por marca (slug) |
| `GET`  | `/api/catalog/makes/{id}/models/search`       | Buscar modelos           |
| `GET`  | `/api/catalog/models/{id}/years`              | Años por modelo          |
| `GET`  | `/api/catalog/models/{id}/years/{year}/trims` | Trims por modelo y año   |
| `GET`  | `/api/catalog/trims/{id}`                     | Info de trim             |
| `GET`  | `/api/catalog/vin/{vin}/decode`               | Decodificar VIN          |
| `GET`  | `/api/catalog/stats`                          | Estadísticas (Admin)     |
| `POST` | `/api/catalog/seed`                           | Seed data (Dev)          |

### Favorites Controller (6 endpoints)

| Método   | Endpoint                     | Autenticación | Descripción           |
| -------- | ---------------------------- | ------------- | --------------------- |
| `GET`    | `/api/favorites`             | ✅            | Listar favoritos      |
| `GET`    | `/api/favorites/count`       | ✅            | Cantidad de favoritos |
| `GET`    | `/api/favorites/check/{id}`  | ✅            | Verificar si favorito |
| `POST`   | `/api/favorites/{vehicleId}` | ✅            | Agregar favorito      |
| `DELETE` | `/api/favorites/{vehicleId}` | ✅            | Eliminar favorito     |
| `PUT`    | `/api/favorites/{vehicleId}` | ✅            | Actualizar favorito   |

### Homepage Sections (1 endpoint)

| Método | Endpoint                         | Descripción             |
| ------ | -------------------------------- | ----------------------- |
| `GET`  | `/api/homepagesections/homepage` | Secciones con vehículos |

**Total: 33 endpoints documentados** ✅

---

_Generado: Enero 30, 2026_  
_Actualizado por: Sistema de Auditoría Automatizado_
