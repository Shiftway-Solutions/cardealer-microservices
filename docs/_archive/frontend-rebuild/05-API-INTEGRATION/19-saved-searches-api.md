# 🔍 19 - Saved Searches API

**Servicio:** AlertService  
**Puerto:** 8080  
**Base Path:** `/api/savedsearches`  
**Autenticación:** ✅ Requerida

---

## 📖 Descripción

Sistema de búsquedas guardadas que permite a usuarios guardar criterios de búsqueda y recibir notificaciones cuando aparecen nuevos vehículos que coinciden.

---

## 🎯 Endpoints Disponibles

| #   | Método   | Endpoint                                | Auth | Descripción                 |
| --- | -------- | --------------------------------------- | ---- | --------------------------- |
| 1   | `GET`    | `/api/savedsearches`                    | ✅   | Listar búsquedas guardadas  |
| 2   | `GET`    | `/api/savedsearches/{id}`               | ✅   | Obtener búsqueda específica |
| 3   | `POST`   | `/api/savedsearches`                    | ✅   | Crear búsqueda guardada     |
| 4   | `PUT`    | `/api/savedsearches/{id}/name`          | ✅   | Renombrar búsqueda          |
| 5   | `PUT`    | `/api/savedsearches/{id}/criteria`      | ✅   | Actualizar criterios        |
| 6   | `PUT`    | `/api/savedsearches/{id}/notifications` | ✅   | Configurar notificaciones   |
| 7   | `POST`   | `/api/savedsearches/{id}/activate`      | ✅   | Activar búsqueda            |
| 8   | `POST`   | `/api/savedsearches/{id}/deactivate`    | ✅   | Desactivar búsqueda         |
| 9   | `DELETE` | `/api/savedsearches/{id}`               | ✅   | Eliminar búsqueda           |

---

## 📝 Detalle de Endpoints

### 1. GET `/api/savedsearches` - Listar Búsquedas

**Response 200:**

```json
[
  {
    "id": "search-001",
    "userId": "user-123",
    "name": "SUVs Toyota menos de 2M",
    "searchCriteria": {
      "make": "Toyota",
      "bodyType": "SUV",
      "maxPrice": 2000000,
      "yearFrom": 2020
    },
    "status": "Active",
    "sendEmailNotifications": true,
    "frequency": "Daily",
    "matchCount": 12,
    "lastMatchAt": "2026-01-29T08:00:00Z",
    "createdAt": "2026-01-15T10:00:00Z"
  }
]
```

---

### 3. POST `/api/savedsearches` - Crear Búsqueda

**Request:**

```json
{
  "name": "Sedanes 2022-2024 Santo Domingo",
  "searchCriteria": {
    "bodyType": "Sedan",
    "yearFrom": 2022,
    "yearTo": 2024,
    "province": "Santo Domingo",
    "maxPrice": 1500000
  },
  "sendEmailNotifications": true,
  "frequency": "Instant"
}
```

**Frecuencias disponibles:**

- `Instant` - Notificar inmediatamente
- `Daily` - Resumen diario
- `Weekly` - Resumen semanal
- `Never` - Sin notificaciones

**Response 201:**

```json
{
  "id": "search-002",
  "name": "Sedanes 2022-2024 Santo Domingo",
  "searchCriteria": { ... },
  "status": "Active",
  "sendEmailNotifications": true,
  "frequency": "Instant",
  "createdAt": "2026-01-30T10:00:00Z"
}
```

---

### 5. PUT `/api/savedsearches/{id}/criteria` - Actualizar Criterios

**Request:**

```json
{
  "searchCriteria": {
    "bodyType": "Sedan",
    "yearFrom": 2021,
    "yearTo": 2024,
    "province": "Santo Domingo",
    "maxPrice": 1800000,
    "transmission": "Automatic"
  }
}
```

---

### 6. PUT `/api/savedsearches/{id}/notifications` - Configurar Notificaciones

**Request:**

```json
{
  "sendEmailNotifications": true,
  "frequency": "Daily"
}
```

---

## 🔧 TypeScript Types

```typescript
// ============================================================================
// SAVED SEARCH TYPES
// ============================================================================

export interface SavedSearch {
  id: string;
  userId: string;
  name: string;
  searchCriteria: SearchCriteria;
  status: SavedSearchStatus;
  sendEmailNotifications: boolean;
  frequency: NotificationFrequency;
  matchCount: number;
  lastMatchAt?: string;
  lastNotifiedAt?: string;
  createdAt: string;
  updatedAt?: string;
}

export type SavedSearchStatus = "Active" | "Inactive" | "Paused";

export type NotificationFrequency = "Instant" | "Daily" | "Weekly" | "Never";

export interface SearchCriteria {
  // Basic
  searchTerm?: string;
  make?: string;
  model?: string;
  yearFrom?: number;
  yearTo?: number;

  // Price
  minPrice?: number;
  maxPrice?: number;

  // Vehicle Details
  bodyType?: string;
  transmission?: string;
  fuelType?: string;
  condition?: "New" | "Used";

  // Location
  province?: string;
  city?: string;

  // Features
  features?: string[];
  colors?: string[];

  // Mileage
  maxMileage?: number;
}

// ============================================================================
// REQUESTS
// ============================================================================

export interface CreateSavedSearchRequest {
  name: string;
  searchCriteria: SearchCriteria;
  sendEmailNotifications?: boolean;
  frequency?: NotificationFrequency;
}

export interface UpdateNameRequest {
  name: string;
}

export interface UpdateCriteriaRequest {
  searchCriteria: SearchCriteria;
}

export interface UpdateNotificationsRequest {
  sendEmailNotifications: boolean;
  frequency: NotificationFrequency;
}
```

---

## 📡 Service Layer

```typescript
// src/services/savedSearchService.ts
import { apiClient } from "./api-client";
import type {
  SavedSearch,
  CreateSavedSearchRequest,
  UpdateNameRequest,
  UpdateCriteriaRequest,
  UpdateNotificationsRequest,
} from "@/types/saved-search";

class SavedSearchService {
  async getMySearches(): Promise<SavedSearch[]> {
    const response = await apiClient.get<SavedSearch[]>("/api/savedsearches");
    return response.data;
  }

  async getById(id: string): Promise<SavedSearch> {
    const response = await apiClient.get<SavedSearch>(
      `/api/savedsearches/${id}`,
    );
    return response.data;
  }

  async create(request: CreateSavedSearchRequest): Promise<SavedSearch> {
    const response = await apiClient.post<SavedSearch>(
      "/api/savedsearches",
      request,
    );
    return response.data;
  }

  async updateName(
    id: string,
    request: UpdateNameRequest,
  ): Promise<SavedSearch> {
    const response = await apiClient.put<SavedSearch>(
      `/api/savedsearches/${id}/name`,
      request,
    );
    return response.data;
  }

  async updateCriteria(
    id: string,
    request: UpdateCriteriaRequest,
  ): Promise<SavedSearch> {
    const response = await apiClient.put<SavedSearch>(
      `/api/savedsearches/${id}/criteria`,
      request,
    );
    return response.data;
  }

  async updateNotifications(
    id: string,
    request: UpdateNotificationsRequest,
  ): Promise<SavedSearch> {
    const response = await apiClient.put<SavedSearch>(
      `/api/savedsearches/${id}/notifications`,
      request,
    );
    return response.data;
  }

  async activate(id: string): Promise<SavedSearch> {
    const response = await apiClient.post<SavedSearch>(
      `/api/savedsearches/${id}/activate`,
    );
    return response.data;
  }

  async deactivate(id: string): Promise<SavedSearch> {
    const response = await apiClient.post<SavedSearch>(
      `/api/savedsearches/${id}/deactivate`,
    );
    return response.data;
  }

  async delete(id: string): Promise<void> {
    await apiClient.delete(`/api/savedsearches/${id}`);
  }
}

export const savedSearchService = new SavedSearchService();
```

---

## 🎣 React Query Hooks

```typescript
// src/hooks/useSavedSearches.ts
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { savedSearchService } from "@/services/savedSearchService";
import type {
  CreateSavedSearchRequest,
  UpdateCriteriaRequest,
  UpdateNotificationsRequest,
} from "@/types/saved-search";

export const savedSearchKeys = {
  all: ["saved-searches"] as const,
  lists: () => [...savedSearchKeys.all, "list"] as const,
  details: () => [...savedSearchKeys.all, "detail"] as const,
  detail: (id: string) => [...savedSearchKeys.details(), id] as const,
};

export function useSavedSearches() {
  return useQuery({
    queryKey: savedSearchKeys.lists(),
    queryFn: () => savedSearchService.getMySearches(),
  });
}

export function useSavedSearch(id: string) {
  return useQuery({
    queryKey: savedSearchKeys.detail(id),
    queryFn: () => savedSearchService.getById(id),
    enabled: !!id,
  });
}

export function useCreateSavedSearch() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateSavedSearchRequest) =>
      savedSearchService.create(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: savedSearchKeys.lists() });
    },
  });
}

export function useUpdateSearchCriteria() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      request,
    }: {
      id: string;
      request: UpdateCriteriaRequest;
    }) => savedSearchService.updateCriteria(id, request),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: savedSearchKeys.detail(variables.id),
      });
      queryClient.invalidateQueries({ queryKey: savedSearchKeys.lists() });
    },
  });
}

export function useUpdateSearchNotifications() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      request,
    }: {
      id: string;
      request: UpdateNotificationsRequest;
    }) => savedSearchService.updateNotifications(id, request),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: savedSearchKeys.detail(variables.id),
      });
    },
  });
}

export function useToggleSavedSearch() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, active }: { id: string; active: boolean }) =>
      active
        ? savedSearchService.activate(id)
        : savedSearchService.deactivate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: savedSearchKeys.lists() });
    },
  });
}

export function useDeleteSavedSearch() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => savedSearchService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: savedSearchKeys.lists() });
    },
  });
}
```

---

## 🧩 Componente de Ejemplo

```typescript
// src/components/search/SavedSearchesList.tsx
import { useSavedSearches, useDeleteSavedSearch } from "@/hooks/useSavedSearches";
import { FiSearch, FiBell, FiTrash2, FiEdit2 } from "react-icons/fi";
import { formatDistanceToNow } from "date-fns";
import { es } from "date-fns/locale";

export const SavedSearchesList = () => {
  const { data: searches, isLoading } = useSavedSearches();
  const deleteMutation = useDeleteSavedSearch();

  if (isLoading) return <div>Cargando...</div>;

  return (
    <div className="space-y-4">
      <div className="flex justify-between items-center">
        <h2 className="text-xl font-semibold">Búsquedas Guardadas</h2>
        <span className="text-gray-500">{searches?.length || 0} guardadas</span>
      </div>

      {searches?.map((search) => (
        <div key={search.id} className="border rounded-lg p-4 hover:shadow-md transition">
          <div className="flex items-start justify-between">
            <div className="flex-1">
              <h3 className="font-medium flex items-center gap-2">
                <FiSearch className="text-blue-500" />
                {search.name}
              </h3>

              {/* Criteria Pills */}
              <div className="flex flex-wrap gap-2 mt-2">
                {search.searchCriteria.make && (
                  <span className="bg-gray-100 px-2 py-1 rounded text-sm">
                    {search.searchCriteria.make}
                  </span>
                )}
                {search.searchCriteria.bodyType && (
                  <span className="bg-gray-100 px-2 py-1 rounded text-sm">
                    {search.searchCriteria.bodyType}
                  </span>
                )}
                {search.searchCriteria.maxPrice && (
                  <span className="bg-gray-100 px-2 py-1 rounded text-sm">
                    Hasta ${search.searchCriteria.maxPrice.toLocaleString()}
                  </span>
                )}
                {search.searchCriteria.yearFrom && (
                  <span className="bg-gray-100 px-2 py-1 rounded text-sm">
                    {search.searchCriteria.yearFrom}+
                  </span>
                )}
              </div>

              {/* Stats */}
              <div className="flex items-center gap-4 mt-3 text-sm text-gray-500">
                <span className="flex items-center gap-1">
                  <FiBell className={search.sendEmailNotifications ? "text-green-500" : ""} />
                  {search.frequency}
                </span>
                <span>{search.matchCount} coincidencias</span>
                {search.lastMatchAt && (
                  <span>
                    Última hace {formatDistanceToNow(new Date(search.lastMatchAt), { locale: es })}
                  </span>
                )}
              </div>
            </div>

            {/* Actions */}
            <div className="flex gap-2">
              <a
                href={`/search?${new URLSearchParams(search.searchCriteria as any).toString()}`}
                className="btn btn-sm btn-outline"
              >
                Ver resultados
              </a>
              <button
                onClick={() => deleteMutation.mutate(search.id)}
                className="p-2 text-red-500 hover:bg-red-50 rounded"
              >
                <FiTrash2 />
              </button>
            </div>
          </div>
        </div>
      ))}

      {searches?.length === 0 && (
        <div className="text-center py-12 text-gray-500">
          <FiSearch className="w-12 h-12 mx-auto mb-4 opacity-50" />
          <p>No tienes búsquedas guardadas.</p>
          <p className="text-sm mt-1">
            Realiza una búsqueda y haz clic en "Guardar búsqueda" para recibir alertas.
          </p>
        </div>
      )}
    </div>
  );
};
```

---

## 🎉 Resumen

✅ **9 Endpoints documentados**  
✅ **TypeScript Types** (SavedSearch, SearchCriteria, NotificationFrequency)  
✅ **Service Layer** (9 métodos)  
✅ **React Query Hooks** (7 hooks)  
✅ **Componente ejemplo** (SavedSearchesList con criterios)

---

_Última actualización: Enero 30, 2026_
