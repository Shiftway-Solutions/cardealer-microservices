# 🔔 18 - Price Alerts API

**Servicio:** AlertService  
**Puerto:** 8080  
**Base Path:** `/api/pricealerts`  
**Autenticación:** ✅ Requerida

---

## 📖 Descripción

Sistema de alertas de precio que notifica a usuarios cuando el precio de un vehículo baja a su precio objetivo. Incluye condiciones personalizables y múltiples estados de alerta.

---

## 🎯 Endpoints Disponibles

| #   | Método   | Endpoint                             | Auth | Descripción                |
| --- | -------- | ------------------------------------ | ---- | -------------------------- |
| 1   | `GET`    | `/api/pricealerts`                   | ✅   | Listar mis alertas         |
| 2   | `GET`    | `/api/pricealerts/{id}`              | ✅   | Obtener alerta específica  |
| 3   | `POST`   | `/api/pricealerts`                   | ✅   | Crear nueva alerta         |
| 4   | `PUT`    | `/api/pricealerts/{id}/target-price` | ✅   | Actualizar precio objetivo |
| 5   | `POST`   | `/api/pricealerts/{id}/activate`     | ✅   | Activar alerta             |
| 6   | `POST`   | `/api/pricealerts/{id}/deactivate`   | ✅   | Desactivar alerta          |
| 7   | `POST`   | `/api/pricealerts/{id}/reset`        | ✅   | Resetear alerta disparada  |
| 8   | `DELETE` | `/api/pricealerts/{id}`              | ✅   | Eliminar alerta            |

---

## 📝 Detalle de Endpoints

### 1. GET `/api/pricealerts` - Listar Mis Alertas

**Response 200:**

```json
[
  {
    "id": "alert-001",
    "userId": "user-123",
    "vehicleId": "vehicle-456",
    "targetPrice": 1500000.0,
    "currentPrice": 1800000.0,
    "condition": "LessThanOrEqual",
    "status": "Active",
    "timesTriggered": 0,
    "lastTriggeredAt": null,
    "createdAt": "2026-01-15T10:00:00Z",
    "updatedAt": null
  }
]
```

---

### 3. POST `/api/pricealerts` - Crear Alerta

**Request:**

```json
{
  "vehicleId": "vehicle-456",
  "targetPrice": 1500000.0,
  "condition": "LessThanOrEqual"
}
```

**Condiciones disponibles:**

- `LessThan` - Precio menor que objetivo
- `LessThanOrEqual` - Precio menor o igual
- `Equals` - Precio exacto
- `PercentageDecrease` - Bajó X% desde precio original

**Response 201:**

```json
{
  "id": "alert-002",
  "userId": "user-123",
  "vehicleId": "vehicle-456",
  "targetPrice": 1500000.0,
  "condition": "LessThanOrEqual",
  "status": "Active",
  "createdAt": "2026-01-30T10:00:00Z"
}
```

**Error 400:**

```json
{
  "error": "Ya existe una alerta para este vehículo"
}
```

---

### 4. PUT `/api/pricealerts/{id}/target-price` - Actualizar Precio

**Request:**

```json
{
  "targetPrice": 1400000.0
}
```

**Response 200:**

```json
{
  "id": "alert-001",
  "targetPrice": 1400000.0,
  "status": "Active",
  "updatedAt": "2026-01-30T11:00:00Z"
}
```

---

### 7. POST `/api/pricealerts/{id}/reset` - Resetear Alerta

Cuando una alerta se dispara (el precio bajó), su estado cambia a `Triggered`. Este endpoint permite resetearla para seguir monitoreando.

**Response 200:**

```json
{
  "id": "alert-001",
  "status": "Active",
  "timesTriggered": 1,
  "lastTriggeredAt": "2026-01-29T08:00:00Z"
}
```

---

## 🔧 TypeScript Types

```typescript
// ============================================================================
// PRICE ALERT TYPES
// ============================================================================

export interface PriceAlert {
  id: string;
  userId: string;
  vehicleId: string;
  targetPrice: number;
  currentPrice?: number;
  condition: AlertCondition;
  status: AlertStatus;
  timesTriggered: number;
  lastTriggeredAt?: string;
  createdAt: string;
  updatedAt?: string;
  // Vehicle info (populated on list)
  vehicle?: {
    id: string;
    title: string;
    imageUrl: string;
    currentPrice: number;
  };
}

export type AlertCondition =
  | "LessThan"
  | "LessThanOrEqual"
  | "Equals"
  | "PercentageDecrease";

export type AlertStatus = "Active" | "Inactive" | "Triggered" | "Expired";

// ============================================================================
// REQUESTS
// ============================================================================

export interface CreatePriceAlertRequest {
  vehicleId: string;
  targetPrice: number;
  condition: AlertCondition;
}

export interface UpdateTargetPriceRequest {
  targetPrice: number;
}
```

---

## 📡 Service Layer

```typescript
// src/services/priceAlertService.ts
import { apiClient } from "./api-client";
import type {
  PriceAlert,
  CreatePriceAlertRequest,
  UpdateTargetPriceRequest,
} from "@/types/price-alert";

class PriceAlertService {
  async getMyAlerts(): Promise<PriceAlert[]> {
    const response = await apiClient.get<PriceAlert[]>("/api/pricealerts");
    return response.data;
  }

  async getById(id: string): Promise<PriceAlert> {
    const response = await apiClient.get<PriceAlert>(`/api/pricealerts/${id}`);
    return response.data;
  }

  async create(request: CreatePriceAlertRequest): Promise<PriceAlert> {
    const response = await apiClient.post<PriceAlert>(
      "/api/pricealerts",
      request,
    );
    return response.data;
  }

  async updateTargetPrice(
    id: string,
    request: UpdateTargetPriceRequest,
  ): Promise<PriceAlert> {
    const response = await apiClient.put<PriceAlert>(
      `/api/pricealerts/${id}/target-price`,
      request,
    );
    return response.data;
  }

  async activate(id: string): Promise<PriceAlert> {
    const response = await apiClient.post<PriceAlert>(
      `/api/pricealerts/${id}/activate`,
    );
    return response.data;
  }

  async deactivate(id: string): Promise<PriceAlert> {
    const response = await apiClient.post<PriceAlert>(
      `/api/pricealerts/${id}/deactivate`,
    );
    return response.data;
  }

  async reset(id: string): Promise<PriceAlert> {
    const response = await apiClient.post<PriceAlert>(
      `/api/pricealerts/${id}/reset`,
    );
    return response.data;
  }

  async delete(id: string): Promise<void> {
    await apiClient.delete(`/api/pricealerts/${id}`);
  }
}

export const priceAlertService = new PriceAlertService();
```

---

## 🎣 React Query Hooks

```typescript
// src/hooks/usePriceAlerts.ts
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { priceAlertService } from "@/services/priceAlertService";
import type {
  CreatePriceAlertRequest,
  UpdateTargetPriceRequest,
} from "@/types/price-alert";

export const priceAlertKeys = {
  all: ["price-alerts"] as const,
  lists: () => [...priceAlertKeys.all, "list"] as const,
  details: () => [...priceAlertKeys.all, "detail"] as const,
  detail: (id: string) => [...priceAlertKeys.details(), id] as const,
};

export function usePriceAlerts() {
  return useQuery({
    queryKey: priceAlertKeys.lists(),
    queryFn: () => priceAlertService.getMyAlerts(),
  });
}

export function usePriceAlert(id: string) {
  return useQuery({
    queryKey: priceAlertKeys.detail(id),
    queryFn: () => priceAlertService.getById(id),
    enabled: !!id,
  });
}

export function useCreatePriceAlert() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreatePriceAlertRequest) =>
      priceAlertService.create(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: priceAlertKeys.lists() });
    },
  });
}

export function useUpdateTargetPrice() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      request,
    }: {
      id: string;
      request: UpdateTargetPriceRequest;
    }) => priceAlertService.updateTargetPrice(id, request),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: priceAlertKeys.detail(variables.id),
      });
      queryClient.invalidateQueries({ queryKey: priceAlertKeys.lists() });
    },
  });
}

export function useTogglePriceAlert() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, active }: { id: string; active: boolean }) =>
      active
        ? priceAlertService.activate(id)
        : priceAlertService.deactivate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: priceAlertKeys.lists() });
    },
  });
}

export function useResetPriceAlert() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => priceAlertService.reset(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: priceAlertKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: priceAlertKeys.lists() });
    },
  });
}

export function useDeletePriceAlert() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => priceAlertService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: priceAlertKeys.lists() });
    },
  });
}
```

---

## 🧩 Componente de Ejemplo

```typescript
// src/components/alerts/PriceAlertsList.tsx
import { usePriceAlerts, useTogglePriceAlert, useDeletePriceAlert } from "@/hooks/usePriceAlerts";
import { FiBell, FiBellOff, FiTrash2, FiTrendingDown } from "react-icons/fi";

export const PriceAlertsList = () => {
  const { data: alerts, isLoading } = usePriceAlerts();
  const toggleMutation = useTogglePriceAlert();
  const deleteMutation = useDeletePriceAlert();

  if (isLoading) return <div>Cargando alertas...</div>;

  const activeAlerts = alerts?.filter(a => a.status === "Active") || [];
  const triggeredAlerts = alerts?.filter(a => a.status === "Triggered") || [];

  return (
    <div className="space-y-6">
      {/* Triggered Alerts */}
      {triggeredAlerts.length > 0 && (
        <div className="bg-green-50 border border-green-200 rounded-lg p-4">
          <h3 className="font-semibold text-green-800 flex items-center gap-2">
            <FiTrendingDown /> ¡Alertas disparadas!
          </h3>
          <div className="mt-3 space-y-2">
            {triggeredAlerts.map(alert => (
              <div key={alert.id} className="flex items-center justify-between bg-white p-3 rounded">
                <div>
                  <p className="font-medium">{alert.vehicle?.title}</p>
                  <p className="text-sm text-gray-500">
                    Precio bajó a ${alert.currentPrice?.toLocaleString()}
                  </p>
                </div>
                <a href={`/vehicles/${alert.vehicleId}`} className="btn btn-sm btn-primary">
                  Ver vehículo
                </a>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Active Alerts */}
      <div className="space-y-3">
        <h3 className="font-semibold">Alertas Activas ({activeAlerts.length})</h3>

        {activeAlerts.map(alert => (
          <div key={alert.id} className="border rounded-lg p-4 flex items-center gap-4">
            {/* Vehicle Image */}
            <img
              src={alert.vehicle?.imageUrl}
              alt=""
              className="w-20 h-16 object-cover rounded"
            />

            {/* Alert Info */}
            <div className="flex-1">
              <p className="font-medium">{alert.vehicle?.title}</p>
              <p className="text-sm text-gray-500">
                Precio actual: ${alert.currentPrice?.toLocaleString()}
              </p>
              <p className="text-sm text-blue-600">
                Objetivo: ${alert.targetPrice.toLocaleString()}
                {alert.currentPrice && (
                  <span className="ml-2 text-gray-400">
                    ({Math.round(((alert.currentPrice - alert.targetPrice) / alert.currentPrice) * 100)}% de diferencia)
                  </span>
                )}
              </p>
            </div>

            {/* Actions */}
            <div className="flex items-center gap-2">
              <button
                onClick={() => toggleMutation.mutate({
                  id: alert.id,
                  active: alert.status !== "Active"
                })}
                className="p-2 rounded hover:bg-gray-100"
                title={alert.status === "Active" ? "Desactivar" : "Activar"}
              >
                {alert.status === "Active" ? <FiBell /> : <FiBellOff />}
              </button>
              <button
                onClick={() => deleteMutation.mutate(alert.id)}
                className="p-2 rounded hover:bg-red-50 text-red-500"
                title="Eliminar"
              >
                <FiTrash2 />
              </button>
            </div>
          </div>
        ))}

        {activeAlerts.length === 0 && (
          <p className="text-gray-500 text-center py-8">
            No tienes alertas de precio activas.
            <br />
            Visita un vehículo y activa "Notificarme cuando baje el precio".
          </p>
        )}
      </div>
    </div>
  );
};
```

---

## 🎉 Resumen

✅ **8 Endpoints documentados**  
✅ **TypeScript Types** (PriceAlert, AlertCondition, AlertStatus)  
✅ **Service Layer** (8 métodos)  
✅ **React Query Hooks** (7 hooks)  
✅ **Componente ejemplo** (PriceAlertsList con estados)

---

_Última actualización: Enero 30, 2026_
