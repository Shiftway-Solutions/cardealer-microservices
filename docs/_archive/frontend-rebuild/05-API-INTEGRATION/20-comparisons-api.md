# ⚖️ 20 - Vehicle Comparisons API

**Servicio:** ComparisonService  
**Puerto:** 8080  
**Base Path:** `/api/vehiclecomparisons`  
**Autenticación:** ✅ Requerida (algunas públicas)

---

## 📖 Descripción

Sistema de comparación de vehículos que permite a usuarios comparar hasta 4 vehículos lado a lado, guardar comparaciones y compartirlas públicamente.

---

## 🎯 Endpoints Disponibles

| #   | Método   | Endpoint                                      | Auth | Descripción                     |
| --- | -------- | --------------------------------------------- | ---- | ------------------------------- |
| 1   | `GET`    | `/api/vehiclecomparisons`                     | ✅   | Listar mis comparaciones        |
| 2   | `GET`    | `/api/vehiclecomparisons/{id}`                | ✅   | Obtener comparación con detalle |
| 3   | `GET`    | `/api/vehiclecomparisons/shared/{shareToken}` | ❌   | Ver comparación compartida      |
| 4   | `POST`   | `/api/vehiclecomparisons`                     | ✅   | Crear nueva comparación         |
| 5   | `PUT`    | `/api/vehiclecomparisons/{id}/vehicles`       | ✅   | Actualizar vehículos            |
| 6   | `PUT`    | `/api/vehiclecomparisons/{id}/name`           | ✅   | Renombrar comparación           |
| 7   | `POST`   | `/api/vehiclecomparisons/{id}/share`          | ✅   | Hacer pública (generar link)    |
| 8   | `DELETE` | `/api/vehiclecomparisons/{id}/share`          | ✅   | Hacer privada                   |
| 9   | `DELETE` | `/api/vehiclecomparisons/{id}`                | ✅   | Eliminar comparación            |

---

## 📝 Detalle de Endpoints

### 1. GET `/api/vehiclecomparisons` - Listar Mis Comparaciones

**Response 200:**

```json
[
  {
    "id": "comp-001",
    "name": "SUVs Familiares",
    "vehicleIds": ["vehicle-1", "vehicle-2", "vehicle-3"],
    "vehicleCount": 3,
    "isPublic": false,
    "shareToken": null,
    "createdAt": "2026-01-25T10:00:00Z",
    "updatedAt": "2026-01-28T15:00:00Z"
  }
]
```

---

### 2. GET `/api/vehiclecomparisons/{id}` - Detalle con Vehículos

**Response 200:**

```json
{
  "id": "comp-001",
  "name": "SUVs Familiares",
  "vehicleIds": ["vehicle-1", "vehicle-2", "vehicle-3"],
  "vehicles": [
    {
      "id": "vehicle-1",
      "title": "Toyota RAV4 2024",
      "make": "Toyota",
      "model": "RAV4",
      "year": 2024,
      "price": 2500000,
      "imageUrl": "https://...",
      "specs": {
        "engine": "2.5L 4-Cylinder",
        "horsepower": 203,
        "transmission": "8-Speed Automatic",
        "fuelType": "Gasoline",
        "mpgCity": 27,
        "mpgHighway": 35,
        "seatingCapacity": 5,
        "cargoSpace": "37.6 cu ft"
      }
    },
    {
      "id": "vehicle-2",
      "title": "Honda CR-V 2024",
      "make": "Honda",
      "model": "CR-V",
      "year": 2024,
      "price": 2400000,
      "imageUrl": "https://...",
      "specs": { ... }
    }
  ],
  "isPublic": true,
  "shareToken": "abc123xyz",
  "shareUrl": "https://okla.com.do/compare/abc123xyz",
  "createdAt": "2026-01-25T10:00:00Z"
}
```

---

### 4. POST `/api/vehiclecomparisons` - Crear Comparación

**Request:**

```json
{
  "name": "Sedanes Deportivos",
  "vehicleIds": ["vehicle-4", "vehicle-5"],
  "isPublic": false
}
```

**Response 201:**

```json
{
  "id": "comp-002",
  "name": "Sedanes Deportivos",
  "vehicleIds": ["vehicle-4", "vehicle-5"],
  "isPublic": false,
  "createdAt": "2026-01-30T10:00:00Z"
}
```

**Error 400:**

```json
{
  "error": "La comparación debe tener entre 2 y 4 vehículos"
}
```

---

### 7. POST `/api/vehiclecomparisons/{id}/share` - Compartir

**Response 200:**

```json
{
  "shareToken": "abc123xyz789",
  "shareUrl": "https://okla.com.do/compare/abc123xyz789"
}
```

---

## 🔧 TypeScript Types

```typescript
// ============================================================================
// COMPARISON TYPES
// ============================================================================

export interface Comparison {
  id: string;
  userId: string;
  name: string;
  vehicleIds: string[];
  vehicleCount: number;
  isPublic: boolean;
  shareToken?: string;
  shareUrl?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface ComparisonDetail extends Comparison {
  vehicles: VehicleComparisonDto[];
}

export interface VehicleComparisonDto {
  id: string;
  title: string;
  make: string;
  model: string;
  year: number;
  price: number;
  imageUrl: string;
  condition: "New" | "Used";
  mileage?: number;
  specs: VehicleSpecs;
}

export interface VehicleSpecs {
  engine?: string;
  horsepower?: number;
  torque?: number;
  transmission?: string;
  drivetrain?: string;
  fuelType?: string;
  mpgCity?: number;
  mpgHighway?: number;
  seatingCapacity?: number;
  cargoSpace?: string;
  length?: string;
  width?: string;
  height?: string;
  wheelbase?: string;
  curbWeight?: string;
  // Safety
  airbags?: number;
  safetyRating?: number;
  // Features (booleans)
  hasBackupCamera?: boolean;
  hasBlindSpotMonitor?: boolean;
  hasLaneKeepAssist?: boolean;
  hasAdaptiveCruise?: boolean;
  hasSunroof?: boolean;
  hasLeatherSeats?: boolean;
  hasNavigationSystem?: boolean;
}

// ============================================================================
// REQUESTS
// ============================================================================

export interface CreateComparisonRequest {
  name: string;
  vehicleIds: string[];
  isPublic?: boolean;
}

export interface UpdateVehiclesRequest {
  vehicleIds: string[];
}

export interface RenameRequest {
  name: string;
}

export interface ShareResponse {
  shareToken: string;
  shareUrl: string;
}
```

---

## 📡 Service Layer

```typescript
// src/services/comparisonService.ts
import { apiClient } from "./api-client";
import type {
  Comparison,
  ComparisonDetail,
  CreateComparisonRequest,
  UpdateVehiclesRequest,
  RenameRequest,
  ShareResponse,
} from "@/types/comparison";

class ComparisonService {
  async getMyComparisons(): Promise<Comparison[]> {
    const response = await apiClient.get<Comparison[]>(
      "/api/vehiclecomparisons",
    );
    return response.data;
  }

  async getById(id: string): Promise<ComparisonDetail> {
    const response = await apiClient.get<ComparisonDetail>(
      `/api/vehiclecomparisons/${id}`,
    );
    return response.data;
  }

  async getByShareToken(shareToken: string): Promise<ComparisonDetail> {
    const response = await apiClient.get<ComparisonDetail>(
      `/api/vehiclecomparisons/shared/${shareToken}`,
    );
    return response.data;
  }

  async create(request: CreateComparisonRequest): Promise<Comparison> {
    const response = await apiClient.post<Comparison>(
      "/api/vehiclecomparisons",
      request,
    );
    return response.data;
  }

  async updateVehicles(
    id: string,
    request: UpdateVehiclesRequest,
  ): Promise<Comparison> {
    const response = await apiClient.put<Comparison>(
      `/api/vehiclecomparisons/${id}/vehicles`,
      request,
    );
    return response.data;
  }

  async rename(id: string, request: RenameRequest): Promise<Comparison> {
    const response = await apiClient.put<Comparison>(
      `/api/vehiclecomparisons/${id}/name`,
      request,
    );
    return response.data;
  }

  async share(id: string): Promise<ShareResponse> {
    const response = await apiClient.post<ShareResponse>(
      `/api/vehiclecomparisons/${id}/share`,
    );
    return response.data;
  }

  async makePrivate(id: string): Promise<void> {
    await apiClient.delete(`/api/vehiclecomparisons/${id}/share`);
  }

  async delete(id: string): Promise<void> {
    await apiClient.delete(`/api/vehiclecomparisons/${id}`);
  }
}

export const comparisonService = new ComparisonService();
```

---

## 🎣 React Query Hooks

```typescript
// src/hooks/useComparisons.ts
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { comparisonService } from "@/services/comparisonService";
import type {
  CreateComparisonRequest,
  UpdateVehiclesRequest,
} from "@/types/comparison";

export const comparisonKeys = {
  all: ["comparisons"] as const,
  lists: () => [...comparisonKeys.all, "list"] as const,
  details: () => [...comparisonKeys.all, "detail"] as const,
  detail: (id: string) => [...comparisonKeys.details(), id] as const,
  shared: (token: string) => [...comparisonKeys.all, "shared", token] as const,
};

export function useComparisons() {
  return useQuery({
    queryKey: comparisonKeys.lists(),
    queryFn: () => comparisonService.getMyComparisons(),
  });
}

export function useComparison(id: string) {
  return useQuery({
    queryKey: comparisonKeys.detail(id),
    queryFn: () => comparisonService.getById(id),
    enabled: !!id,
  });
}

export function useSharedComparison(shareToken: string) {
  return useQuery({
    queryKey: comparisonKeys.shared(shareToken),
    queryFn: () => comparisonService.getByShareToken(shareToken),
    enabled: !!shareToken,
  });
}

export function useCreateComparison() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateComparisonRequest) =>
      comparisonService.create(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: comparisonKeys.lists() });
    },
  });
}

export function useUpdateComparisonVehicles() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      request,
    }: {
      id: string;
      request: UpdateVehiclesRequest;
    }) => comparisonService.updateVehicles(id, request),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: comparisonKeys.detail(variables.id),
      });
      queryClient.invalidateQueries({ queryKey: comparisonKeys.lists() });
    },
  });
}

export function useShareComparison() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => comparisonService.share(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: comparisonKeys.detail(id) });
    },
  });
}

export function useDeleteComparison() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => comparisonService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: comparisonKeys.lists() });
    },
  });
}
```

---

## 🧩 Componente de Ejemplo

```typescript
// src/components/comparison/ComparisonTable.tsx
import { useComparison } from "@/hooks/useComparisons";
import { FiCheck, FiX } from "react-icons/fi";

const SPEC_LABELS: Record<string, string> = {
  engine: "Motor",
  horsepower: "Potencia (HP)",
  transmission: "Transmisión",
  fuelType: "Combustible",
  mpgCity: "Consumo Ciudad (MPG)",
  mpgHighway: "Consumo Carretera (MPG)",
  seatingCapacity: "Asientos",
  cargoSpace: "Espacio Carga",
  safetyRating: "Calificación Seguridad",
};

export const ComparisonTable = ({ comparisonId }: { comparisonId: string }) => {
  const { data: comparison, isLoading } = useComparison(comparisonId);

  if (isLoading) return <div>Cargando comparación...</div>;
  if (!comparison) return <div>Comparación no encontrada</div>;

  const vehicles = comparison.vehicles;
  const specKeys = Object.keys(SPEC_LABELS);

  return (
    <div className="overflow-x-auto">
      <table className="w-full border-collapse">
        {/* Header with vehicle images and names */}
        <thead>
          <tr>
            <th className="p-4 text-left bg-gray-50 w-48"></th>
            {vehicles.map((v) => (
              <th key={v.id} className="p-4 text-center min-w-[200px]">
                <img
                  src={v.imageUrl}
                  alt={v.title}
                  className="w-full h-32 object-cover rounded-lg mb-2"
                />
                <div className="font-semibold">{v.title}</div>
                <div className="text-xl text-blue-600 font-bold">
                  ${v.price.toLocaleString()}
                </div>
              </th>
            ))}
          </tr>
        </thead>

        <tbody>
          {/* Specs rows */}
          {specKeys.map((key) => (
            <tr key={key} className="border-t">
              <td className="p-4 font-medium bg-gray-50">
                {SPEC_LABELS[key]}
              </td>
              {vehicles.map((v) => (
                <td key={v.id} className="p-4 text-center">
                  {renderValue(v.specs[key as keyof typeof v.specs])}
                </td>
              ))}
            </tr>
          ))}

          {/* Boolean Features */}
          <tr className="border-t bg-gray-100">
            <td colSpan={vehicles.length + 1} className="p-4 font-semibold">
              Características
            </td>
          </tr>
          {["hasBackupCamera", "hasBlindSpotMonitor", "hasSunroof", "hasLeatherSeats"].map((feat) => (
            <tr key={feat} className="border-t">
              <td className="p-4 bg-gray-50">
                {feat.replace("has", "").replace(/([A-Z])/g, " $1").trim()}
              </td>
              {vehicles.map((v) => (
                <td key={v.id} className="p-4 text-center">
                  {v.specs[feat as keyof typeof v.specs] ? (
                    <FiCheck className="inline text-green-500 w-5 h-5" />
                  ) : (
                    <FiX className="inline text-gray-300 w-5 h-5" />
                  )}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

function renderValue(value: unknown): React.ReactNode {
  if (value === undefined || value === null) return "—";
  if (typeof value === "boolean") {
    return value ? <FiCheck className="inline text-green-500" /> : <FiX className="inline text-red-400" />;
  }
  return String(value);
}
```

---

## 🎉 Resumen

✅ **9 Endpoints documentados**  
✅ **TypeScript Types** (Comparison, VehicleSpecs, ShareResponse)  
✅ **Service Layer** (9 métodos)  
✅ **React Query Hooks** (7 hooks)  
✅ **Componente ejemplo** (ComparisonTable con specs)

---

_Última actualización: Enero 30, 2026_
