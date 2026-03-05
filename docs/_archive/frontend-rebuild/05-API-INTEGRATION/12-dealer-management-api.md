# 🏢 12 - Dealer Management API (DealerManagementService)

**Servicio:** DealerManagementService  
**Puerto:** 8080  
**Base Path:** `/api/dealers`, `/api/dealers/{dealerId}/locations`  
**Autenticación:** ✅ Requerida (JWT Bearer Token)

---

## 📋 Índice

1. [Descripción General](#descripción-general)
2. [Endpoints Disponibles](#endpoints-disponibles)
3. [TypeScript Types](#typescript-types)
4. [Service Layer](#service-layer)
5. [React Query Hooks](#react-query-hooks)
6. [Componentes de Ejemplo](#componentes-de-ejemplo)

---

## 📖 Descripción General

El **DealerManagementService** gestiona el ciclo de vida completo de los dealers (concesionarios):

- 🏪 Registro y perfil de dealer
- 📍 Múltiples ubicaciones (sucursales, showrooms)
- ✅ Verificación de documentos (RNC, licencias)
- 📊 Estadísticas del dealer

### Tipos de Dealer

| Tipo            | Descripción                        |
| --------------- | ---------------------------------- |
| `Independent`   | Dealer independiente (1 ubicación) |
| `Chain`         | Cadena de concesionarios           |
| `MultipleStore` | Múltiples tiendas                  |
| `Franchise`     | Franquicia de marca                |

### Estados del Dealer

| Estado        | Descripción                    |
| ------------- | ------------------------------ |
| `Pending`     | Registro pendiente de revisión |
| `UnderReview` | Documentos en revisión         |
| `Active`      | Dealer activo y verificado     |
| `Suspended`   | Suspendido temporalmente       |
| `Rejected`    | Solicitud rechazada            |
| `Inactive`    | Inactivo por el dealer         |

---

## 🎯 Endpoints Disponibles

### DealersController (8 endpoints)

| #   | Método   | Endpoint                       | Auth     | Descripción                         |
| --- | -------- | ------------------------------ | -------- | ----------------------------------- |
| 1   | `GET`    | `/api/dealers`                 | ❌       | Listar dealers (paginado + filtros) |
| 2   | `GET`    | `/api/dealers/{id}`            | ❌       | Obtener dealer por ID               |
| 3   | `GET`    | `/api/dealers/user/{userId}`   | ❌       | Obtener dealer por User ID          |
| 4   | `POST`   | `/api/dealers`                 | ✅       | Crear cuenta dealer                 |
| 5   | `PUT`    | `/api/dealers/{id}`            | ✅       | Actualizar información              |
| 6   | `POST`   | `/api/dealers/{id}/verify`     | ✅ Admin | Verificar/Rechazar dealer           |
| 7   | `GET`    | `/api/dealers/{id}/statistics` | ✅       | Estadísticas del dealer             |
| 8   | `DELETE` | `/api/dealers/{id}`            | ✅ Admin | Eliminar dealer                     |

### LocationsController (6 endpoints)

| #   | Método   | Endpoint                                             | Auth | Descripción           |
| --- | -------- | ---------------------------------------------------- | ---- | --------------------- |
| 9   | `GET`    | `/api/dealers/{dealerId}/locations`                  | ❌   | Listar ubicaciones    |
| 10  | `GET`    | `/api/dealers/{dealerId}/locations/{id}`             | ❌   | Obtener ubicación     |
| 11  | `POST`   | `/api/dealers/{dealerId}/locations`                  | ✅   | Crear ubicación       |
| 12  | `PUT`    | `/api/dealers/{dealerId}/locations/{id}`             | ✅   | Actualizar ubicación  |
| 13  | `DELETE` | `/api/dealers/{dealerId}/locations/{id}`             | ✅   | Eliminar ubicación    |
| 14  | `POST`   | `/api/dealers/{dealerId}/locations/{id}/set-primary` | ✅   | Marcar como principal |

---

## 📝 Detalle de Endpoints

### 1. GET `/api/dealers` - Listar Dealers

**Query Params:**

- `page` (int, default: 1)
- `pageSize` (int, default: 20)
- `status` (DealerStatus)
- `verificationStatus` (VerificationStatus)
- `searchTerm` (string) - Busca en nombre, RNC, email, ciudad

**Response 200:**

```json
{
  "data": [
    {
      "id": "dealer-123",
      "userId": "user-456",
      "businessName": "Auto Premium RD",
      "legalName": "Auto Premium SRL",
      "rnc": "123456789",
      "type": "Independent",
      "status": "Active",
      "verificationStatus": "Verified",
      "email": "ventas@autopremium.do",
      "phone": "809-555-1234",
      "logoUrl": "https://cdn.okla.com.do/dealers/logo.png",
      "city": "Santo Domingo",
      "province": "Distrito Nacional",
      "hasFounderBadge": true,
      "createdAt": "2026-01-15T00:00:00Z"
    }
  ],
  "totalCount": 45,
  "page": 1,
  "pageSize": 20,
  "totalPages": 3
}
```

---

### 4. POST `/api/dealers` - Crear Dealer

**Auth:** ✅ Required

**Request Body:**

```json
{
  "businessName": "Auto Premium RD",
  "legalName": "Auto Premium SRL",
  "rnc": "123456789",
  "type": "Independent",
  "email": "ventas@autopremium.do",
  "phone": "809-555-1234",
  "mobilePhone": "809-555-5678",
  "website": "https://autopremium.do",
  "address": "Av. Winston Churchill #45",
  "city": "Santo Domingo",
  "province": "Distrito Nacional",
  "establishedDate": "2015-03-20",
  "employeeCount": 12,
  "description": "Especialistas en vehículos de lujo"
}
```

**Response 201:** Dealer creado con `status: "Pending"`

---

### 6. POST `/api/dealers/{id}/verify` - Verificar Dealer

**Auth:** ✅ Admin only

**Request Body:**

```json
{
  "approved": true,
  "rejectionReason": null
}
```

**Response 200:** Dealer actualizado con `status: "Active"`, `verificationStatus: "Verified"`

**Request (Rechazo):**

```json
{
  "approved": false,
  "rejectionReason": "Documentos incompletos. Falta licencia comercial."
}
```

---

### 11. POST `/api/dealers/{dealerId}/locations` - Crear Ubicación

**Auth:** ✅ Required

**Request Body:**

```json
{
  "name": "Sucursal Piantini",
  "type": "Showroom",
  "address": "Calle Gustavo Mejía Ricart #78",
  "city": "Santo Domingo",
  "province": "Distrito Nacional",
  "postalCode": "10148",
  "phone": "809-555-9999",
  "email": "piantini@autopremium.do",
  "latitude": 18.4722,
  "longitude": -69.9392,
  "operatingHours": "Lun-Sáb 9:00-18:00",
  "isPrimary": false
}
```

**Response 201:**

```json
{
  "id": "loc-789",
  "dealerId": "dealer-123",
  "name": "Sucursal Piantini",
  "type": "Showroom",
  "address": "Calle Gustavo Mejía Ricart #78",
  "city": "Santo Domingo",
  "province": "Distrito Nacional",
  "latitude": 18.4722,
  "longitude": -69.9392,
  "isPrimary": false,
  "isActive": true
}
```

---

## 🔧 TypeScript Types

```typescript
// ============================================================================
// DEALER TYPES
// ============================================================================

export interface Dealer {
  id: string;
  userId: string;
  businessName: string;
  legalName: string;
  rnc: string;
  type: DealerType;
  status: DealerStatus;
  verificationStatus: VerificationStatus;
  email: string;
  phone: string;
  mobilePhone?: string;
  website?: string;
  logoUrl?: string;
  bannerUrl?: string;
  address: string;
  city: string;
  province: string;
  postalCode?: string;
  establishedDate?: string;
  employeeCount?: number;
  description?: string;
  socialMedia?: SocialMediaLinks;
  hasFounderBadge: boolean;
  currentPlan?: string;
  maxActiveListings?: number;
  isSubscriptionActive: boolean;
  verifiedAt?: string;
  verifiedBy?: string;
  rejectionReason?: string;
  createdAt: string;
  updatedAt?: string;
  locations?: DealerLocation[];
}

export type DealerType =
  | "Independent"
  | "Chain"
  | "MultipleStore"
  | "Franchise";

export type DealerStatus =
  | "Pending" // Recién registrado
  | "UnderReview" // Documentos en revisión
  | "Active" // Activo y operando
  | "Suspended" // Suspendido temporalmente
  | "Rejected" // Solicitud rechazada
  | "Inactive"; // Inactivo por el dealer

export type VerificationStatus =
  | "NotVerified" // Sin verificar
  | "DocumentsUploaded" // Documentos subidos
  | "UnderReview" // En revisión
  | "Verified" // Verificado ✓
  | "Rejected"; // Rechazado

export interface SocialMediaLinks {
  facebook?: string;
  instagram?: string;
  twitter?: string;
  whatsapp?: string;
}

// ============================================================================
// LOCATION TYPES
// ============================================================================

export interface DealerLocation {
  id: string;
  dealerId: string;
  name: string;
  type: LocationType;
  address: string;
  city: string;
  province: string;
  postalCode?: string;
  phone?: string;
  email?: string;
  latitude?: number;
  longitude?: number;
  operatingHours?: string;
  isPrimary: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

export type LocationType =
  | "Headquarters" // Sede principal
  | "Branch" // Sucursal
  | "Showroom" // Sala de exhibición
  | "ServiceCenter" // Centro de servicio
  | "Warehouse"; // Almacén

// ============================================================================
// REQUEST TYPES
// ============================================================================

export interface CreateDealerRequest {
  businessName: string;
  legalName: string;
  rnc: string;
  type: DealerType;
  email: string;
  phone: string;
  mobilePhone?: string;
  website?: string;
  address: string;
  city: string;
  province: string;
  postalCode?: string;
  establishedDate?: string;
  employeeCount?: number;
  description?: string;
}

export interface UpdateDealerRequest {
  businessName?: string;
  legalName?: string;
  email?: string;
  phone?: string;
  mobilePhone?: string;
  website?: string;
  address?: string;
  city?: string;
  province?: string;
  description?: string;
  logoUrl?: string;
  bannerUrl?: string;
  socialMedia?: SocialMediaLinks;
}

export interface VerifyDealerRequest {
  approved: boolean;
  rejectionReason?: string;
}

export interface CreateLocationRequest {
  name: string;
  type: LocationType;
  address: string;
  city: string;
  province: string;
  postalCode?: string;
  phone?: string;
  email?: string;
  latitude?: number;
  longitude?: number;
  operatingHours?: string;
  isPrimary?: boolean;
}

// ============================================================================
// RESPONSE TYPES
// ============================================================================

export interface DealerListResponse {
  data: Dealer[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface DealerStatistics {
  totalVehicles: number;
  activeVehicles: number;
  soldVehicles: number;
  totalViews: number;
  totalInquiries: number;
  conversionRate: number;
  averageResponseTime: number;
  rating: number;
  reviewCount: number;
}
```

---

## 📡 Service Layer

```typescript
// src/services/dealerManagementService.ts
import { apiClient } from "./api-client";
import type {
  Dealer,
  DealerLocation,
  DealerListResponse,
  DealerStatistics,
  CreateDealerRequest,
  UpdateDealerRequest,
  VerifyDealerRequest,
  CreateLocationRequest,
  DealerStatus,
  VerificationStatus,
} from "@/types/dealer";

interface GetDealersParams {
  page?: number;
  pageSize?: number;
  status?: DealerStatus;
  verificationStatus?: VerificationStatus;
  searchTerm?: string;
}

class DealerManagementService {
  // ============================================================================
  // DEALERS
  // ============================================================================

  async getDealers(params?: GetDealersParams): Promise<DealerListResponse> {
    const response = await apiClient.get<DealerListResponse>("/api/dealers", {
      params,
    });
    return response.data;
  }

  async getDealerById(id: string): Promise<Dealer> {
    const response = await apiClient.get<Dealer>(`/api/dealers/${id}`);
    return response.data;
  }

  async getDealerByUserId(userId: string): Promise<Dealer> {
    const response = await apiClient.get<Dealer>(`/api/dealers/user/${userId}`);
    return response.data;
  }

  async createDealer(request: CreateDealerRequest): Promise<Dealer> {
    const response = await apiClient.post<Dealer>("/api/dealers", request);
    return response.data;
  }

  async updateDealer(
    id: string,
    request: UpdateDealerRequest,
  ): Promise<Dealer> {
    const response = await apiClient.put<Dealer>(`/api/dealers/${id}`, request);
    return response.data;
  }

  async verifyDealer(id: string, request: VerifyDealerRequest): Promise<void> {
    await apiClient.post(`/api/dealers/${id}/verify`, request);
  }

  async getDealerStatistics(id: string): Promise<DealerStatistics> {
    const response = await apiClient.get<DealerStatistics>(
      `/api/dealers/${id}/statistics`,
    );
    return response.data;
  }

  async deleteDealer(id: string): Promise<void> {
    await apiClient.delete(`/api/dealers/${id}`);
  }

  // ============================================================================
  // LOCATIONS
  // ============================================================================

  async getLocations(dealerId: string): Promise<DealerLocation[]> {
    const response = await apiClient.get<DealerLocation[]>(
      `/api/dealers/${dealerId}/locations`,
    );
    return response.data;
  }

  async getLocationById(
    dealerId: string,
    locationId: string,
  ): Promise<DealerLocation> {
    const response = await apiClient.get<DealerLocation>(
      `/api/dealers/${dealerId}/locations/${locationId}`,
    );
    return response.data;
  }

  async createLocation(
    dealerId: string,
    request: CreateLocationRequest,
  ): Promise<DealerLocation> {
    const response = await apiClient.post<DealerLocation>(
      `/api/dealers/${dealerId}/locations`,
      request,
    );
    return response.data;
  }

  async updateLocation(
    dealerId: string,
    locationId: string,
    request: Partial<CreateLocationRequest>,
  ): Promise<DealerLocation> {
    const response = await apiClient.put<DealerLocation>(
      `/api/dealers/${dealerId}/locations/${locationId}`,
      request,
    );
    return response.data;
  }

  async deleteLocation(dealerId: string, locationId: string): Promise<void> {
    await apiClient.delete(`/api/dealers/${dealerId}/locations/${locationId}`);
  }

  async setPrimaryLocation(
    dealerId: string,
    locationId: string,
  ): Promise<void> {
    await apiClient.post(
      `/api/dealers/${dealerId}/locations/${locationId}/set-primary`,
    );
  }
}

export const dealerManagementService = new DealerManagementService();
```

---

## 🎣 React Query Hooks

```typescript
// src/hooks/useDealers.ts
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { dealerManagementService } from "@/services/dealerManagementService";
import type {
  CreateDealerRequest,
  UpdateDealerRequest,
  CreateLocationRequest,
} from "@/types/dealer";

export const dealerKeys = {
  all: ["dealers"] as const,
  lists: () => [...dealerKeys.all, "list"] as const,
  list: (params: any) => [...dealerKeys.lists(), params] as const,
  details: () => [...dealerKeys.all, "detail"] as const,
  detail: (id: string) => [...dealerKeys.details(), id] as const,
  byUser: (userId: string) => [...dealerKeys.all, "user", userId] as const,
  statistics: (id: string) => [...dealerKeys.all, "statistics", id] as const,
  locations: (dealerId: string) =>
    [...dealerKeys.all, "locations", dealerId] as const,
};

// ============================================================================
// DEALERS
// ============================================================================

export function useDealers(params?: { status?: string; searchTerm?: string }) {
  return useQuery({
    queryKey: dealerKeys.list(params),
    queryFn: () => dealerManagementService.getDealers(params),
  });
}

export function useDealer(id: string) {
  return useQuery({
    queryKey: dealerKeys.detail(id),
    queryFn: () => dealerManagementService.getDealerById(id),
    enabled: !!id,
  });
}

export function useDealerByUser(userId: string) {
  return useQuery({
    queryKey: dealerKeys.byUser(userId),
    queryFn: () => dealerManagementService.getDealerByUserId(userId),
    enabled: !!userId,
  });
}

export function useDealerStatistics(dealerId: string) {
  return useQuery({
    queryKey: dealerKeys.statistics(dealerId),
    queryFn: () => dealerManagementService.getDealerStatistics(dealerId),
    enabled: !!dealerId,
  });
}

export function useCreateDealer() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateDealerRequest) =>
      dealerManagementService.createDealer(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: dealerKeys.lists() });
    },
  });
}

export function useUpdateDealer() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      request,
    }: {
      id: string;
      request: UpdateDealerRequest;
    }) => dealerManagementService.updateDealer(id, request),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: dealerKeys.detail(variables.id),
      });
    },
  });
}

export function useVerifyDealer() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      approved,
      rejectionReason,
    }: {
      id: string;
      approved: boolean;
      rejectionReason?: string;
    }) =>
      dealerManagementService.verifyDealer(id, { approved, rejectionReason }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: dealerKeys.lists() });
    },
  });
}

// ============================================================================
// LOCATIONS
// ============================================================================

export function useDealerLocations(dealerId: string) {
  return useQuery({
    queryKey: dealerKeys.locations(dealerId),
    queryFn: () => dealerManagementService.getLocations(dealerId),
    enabled: !!dealerId,
  });
}

export function useCreateLocation(dealerId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateLocationRequest) =>
      dealerManagementService.createLocation(dealerId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: dealerKeys.locations(dealerId),
      });
    },
  });
}

export function useDeleteLocation(dealerId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (locationId: string) =>
      dealerManagementService.deleteLocation(dealerId, locationId),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: dealerKeys.locations(dealerId),
      });
    },
  });
}
```

---

## 🧩 Componentes de Ejemplo

### DealerProfileCard

```typescript
// src/components/dealers/DealerProfileCard.tsx
import { useDealer, useDealerStatistics } from "@/hooks/useDealers";
import { FiMapPin, FiPhone, FiMail, FiStar, FiCheck } from "react-icons/fi";

export const DealerProfileCard = ({ dealerId }: { dealerId: string }) => {
  const { data: dealer, isLoading } = useDealer(dealerId);
  const { data: stats } = useDealerStatistics(dealerId);

  if (isLoading) return <div>Cargando...</div>;
  if (!dealer) return <div>Dealer no encontrado</div>;

  return (
    <div className="bg-white rounded-xl shadow-lg overflow-hidden">
      {/* Banner */}
      <div className="h-32 bg-gradient-to-r from-blue-600 to-blue-800">
        {dealer.bannerUrl && (
          <img src={dealer.bannerUrl} alt="" className="w-full h-full object-cover" />
        )}
      </div>

      {/* Logo & Info */}
      <div className="relative px-6 pb-6">
        <div className="-mt-12 mb-4">
          <img
            src={dealer.logoUrl || "/default-dealer-logo.png"}
            alt={dealer.businessName}
            className="w-24 h-24 rounded-xl border-4 border-white shadow-lg object-cover"
          />
        </div>

        <div className="flex items-start justify-between">
          <div>
            <h2 className="text-2xl font-bold flex items-center gap-2">
              {dealer.businessName}
              {dealer.verificationStatus === "Verified" && (
                <span className="bg-blue-100 text-blue-600 p-1 rounded-full">
                  <FiCheck className="w-4 h-4" />
                </span>
              )}
              {dealer.hasFounderBadge && (
                <span className="bg-yellow-100 text-yellow-600 px-2 py-0.5 rounded-full text-xs font-medium">
                  🏆 Fundador
                </span>
              )}
            </h2>
            <p className="text-gray-600">{dealer.type}</p>
          </div>

          {stats && (
            <div className="flex items-center gap-1 text-yellow-500">
              <FiStar className="fill-current" />
              <span className="font-bold">{stats.rating.toFixed(1)}</span>
              <span className="text-gray-400">({stats.reviewCount})</span>
            </div>
          )}
        </div>

        {/* Contact Info */}
        <div className="mt-4 space-y-2 text-sm text-gray-600">
          <div className="flex items-center gap-2">
            <FiMapPin className="text-gray-400" />
            <span>{dealer.address}, {dealer.city}</span>
          </div>
          <div className="flex items-center gap-2">
            <FiPhone className="text-gray-400" />
            <span>{dealer.phone}</span>
          </div>
          <div className="flex items-center gap-2">
            <FiMail className="text-gray-400" />
            <span>{dealer.email}</span>
          </div>
        </div>

        {/* Stats */}
        {stats && (
          <div className="mt-6 grid grid-cols-3 gap-4 text-center">
            <div className="bg-gray-50 rounded-lg p-3">
              <div className="text-2xl font-bold text-blue-600">{stats.activeVehicles}</div>
              <div className="text-xs text-gray-500">Vehículos</div>
            </div>
            <div className="bg-gray-50 rounded-lg p-3">
              <div className="text-2xl font-bold text-green-600">{stats.soldVehicles}</div>
              <div className="text-xs text-gray-500">Vendidos</div>
            </div>
            <div className="bg-gray-50 rounded-lg p-3">
              <div className="text-2xl font-bold text-purple-600">{stats.conversionRate}%</div>
              <div className="text-xs text-gray-500">Conversión</div>
            </div>
          </div>
        )}

        {/* CTA */}
        <button className="mt-6 w-full bg-blue-600 text-white py-3 rounded-lg font-medium hover:bg-blue-700">
          Ver Inventario
        </button>
      </div>
    </div>
  );
};
```

---

### LocationsManager

```typescript
// src/components/dealers/LocationsManager.tsx
import { useDealerLocations, useCreateLocation, useDeleteLocation } from "@/hooks/useDealers";
import { FiMapPin, FiTrash2, FiStar } from "react-icons/fi";

export const LocationsManager = ({ dealerId }: { dealerId: string }) => {
  const { data: locations, isLoading } = useDealerLocations(dealerId);
  const createMutation = useCreateLocation(dealerId);
  const deleteMutation = useDeleteLocation(dealerId);

  if (isLoading) return <div>Cargando ubicaciones...</div>;

  const locationTypeLabels = {
    Headquarters: "Sede Principal",
    Branch: "Sucursal",
    Showroom: "Showroom",
    ServiceCenter: "Taller",
    Warehouse: "Almacén",
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-semibold">Ubicaciones</h3>
        <button
          onClick={() => createMutation.mutate({
            name: "Nueva Sucursal",
            type: "Branch",
            address: "",
            city: "",
            province: "",
          })}
          className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm"
        >
          + Agregar Ubicación
        </button>
      </div>

      <div className="grid gap-4">
        {locations?.map((location) => (
          <div
            key={location.id}
            className="bg-white border rounded-lg p-4 flex items-start justify-between"
          >
            <div className="flex items-start gap-3">
              <div className="p-2 bg-blue-100 rounded-lg">
                <FiMapPin className="text-blue-600" />
              </div>
              <div>
                <div className="flex items-center gap-2">
                  <span className="font-medium">{location.name}</span>
                  {location.isPrimary && (
                    <span className="flex items-center gap-1 text-xs bg-yellow-100 text-yellow-700 px-2 py-0.5 rounded-full">
                      <FiStar className="w-3 h-3" /> Principal
                    </span>
                  )}
                </div>
                <p className="text-sm text-gray-600">{location.address}</p>
                <p className="text-sm text-gray-500">
                  {location.city}, {location.province}
                </p>
                <span className="text-xs text-gray-400">
                  {locationTypeLabels[location.type]}
                </span>
              </div>
            </div>

            <button
              onClick={() => deleteMutation.mutate(location.id)}
              className="p-2 text-red-500 hover:bg-red-50 rounded-lg"
              disabled={location.isPrimary}
            >
              <FiTrash2 />
            </button>
          </div>
        ))}
      </div>
    </div>
  );
};
```

---

## 🎉 Resumen

✅ **14 Endpoints documentados**  
✅ **TypeScript Types completos** (Dealer, Location, enums)  
✅ **Service Layer** con 14 métodos  
✅ **React Query Hooks** (10 hooks)  
✅ **2 Componentes UI** (DealerProfileCard + LocationsManager)

---

_Última actualización: Enero 30, 2026_
