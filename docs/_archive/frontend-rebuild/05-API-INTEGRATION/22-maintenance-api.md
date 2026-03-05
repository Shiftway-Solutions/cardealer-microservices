# 🔧 22 - Maintenance Mode API

**Servicio:** MaintenanceService  
**Puerto:** 8080  
**Base Path:** `/api/maintenance`  
**Autenticación:** ✅ Admin (mayoría) | ❌ Público (status)

---

## 📖 Descripción

Sistema de gestión de ventanas de mantenimiento programado. Permite a administradores programar, iniciar y finalizar períodos de mantenimiento, con notificaciones automáticas a usuarios.

---

## 🎯 Endpoints Disponibles

| #   | Método   | Endpoint                         | Auth     | Descripción               |
| --- | -------- | -------------------------------- | -------- | ------------------------- |
| 1   | `GET`    | `/api/maintenance/status`        | ❌       | Estado actual del sistema |
| 2   | `GET`    | `/api/maintenance`               | ✅ Admin | Listar todas las ventanas |
| 3   | `GET`    | `/api/maintenance/upcoming`      | ❌       | Ventanas próximas         |
| 4   | `GET`    | `/api/maintenance/{id}`          | ✅ Admin | Detalle de ventana        |
| 5   | `POST`   | `/api/maintenance`               | ✅ Admin | Crear ventana             |
| 6   | `POST`   | `/api/maintenance/{id}/start`    | ✅ Admin | Iniciar mantenimiento     |
| 7   | `POST`   | `/api/maintenance/{id}/complete` | ✅ Admin | Completar mantenimiento   |
| 8   | `POST`   | `/api/maintenance/{id}/cancel`   | ✅ Admin | Cancelar ventana          |
| 9   | `PUT`    | `/api/maintenance/{id}/schedule` | ✅ Admin | Reprogramar               |
| 10  | `PUT`    | `/api/maintenance/{id}/notes`    | ✅ Admin | Actualizar notas          |
| 11  | `DELETE` | `/api/maintenance/{id}`          | ✅ Admin | Eliminar ventana          |

---

## 📝 Detalle de Endpoints

### 1. GET `/api/maintenance/status` - Estado Actual (Público)

**Response 200:**

```json
{
  "isMaintenanceMode": false,
  "maintenanceWindow": null
}
```

**Response 200 (en mantenimiento):**

```json
{
  "isMaintenanceMode": true,
  "maintenanceWindow": {
    "id": "maint-001",
    "title": "Actualización de Seguridad",
    "description": "Aplicando parches de seguridad críticos",
    "type": "Emergency",
    "status": "InProgress",
    "scheduledStart": "2026-01-30T02:00:00Z",
    "scheduledEnd": "2026-01-30T04:00:00Z",
    "actualStart": "2026-01-30T02:00:00Z",
    "affectedServices": ["auth", "billing"],
    "isActive": true
  }
}
```

---

### 5. POST `/api/maintenance` - Crear Ventana

**Request:**

```json
{
  "title": "Mantenimiento Mensual",
  "description": "Actualización de base de datos y optimización",
  "type": "Scheduled",
  "scheduledStart": "2026-02-01T03:00:00Z",
  "scheduledEnd": "2026-02-01T05:00:00Z",
  "notifyUsers": true,
  "notifyMinutesBefore": 60,
  "affectedServices": ["database", "search"]
}
```

**Tipos de mantenimiento:**

- `Scheduled` - Mantenimiento programado
- `Emergency` - Emergencia
- `Upgrade` - Actualización de versión
- `Migration` - Migración de datos

**Response 201:**

```json
{
  "id": "maint-002",
  "title": "Mantenimiento Mensual",
  "type": "Scheduled",
  "status": "Scheduled",
  "scheduledStart": "2026-02-01T03:00:00Z",
  "scheduledEnd": "2026-02-01T05:00:00Z",
  "createdBy": "admin@okla.com.do",
  "createdAt": "2026-01-30T10:00:00Z",
  "notifyUsers": true,
  "notifyMinutesBefore": 60,
  "isActive": false,
  "isUpcoming": true
}
```

---

### 6. POST `/api/maintenance/{id}/start` - Iniciar

**Response 200:**

```json
{
  "id": "maint-002",
  "status": "InProgress",
  "actualStart": "2026-02-01T03:00:00Z",
  "isActive": true
}
```

---

## 🔧 TypeScript Types

```typescript
// ============================================================================
// MAINTENANCE TYPES
// ============================================================================

export interface MaintenanceStatus {
  isMaintenanceMode: boolean;
  maintenanceWindow: MaintenanceWindow | null;
}

export interface MaintenanceWindow {
  id: string;
  title: string;
  description: string;
  type: MaintenanceType;
  status: MaintenanceWindowStatus;
  scheduledStart: string;
  scheduledEnd: string;
  actualStart?: string;
  actualEnd?: string;
  createdBy: string;
  createdAt: string;
  updatedAt?: string;
  notes?: string;
  notifyUsers: boolean;
  notifyMinutesBefore: number;
  affectedServices: string[];
  isActive: boolean;
  isUpcoming: boolean;
}

export type MaintenanceType =
  | "Scheduled"
  | "Emergency"
  | "Upgrade"
  | "Migration";

export type MaintenanceWindowStatus =
  | "Scheduled"
  | "InProgress"
  | "Completed"
  | "Cancelled";

// ============================================================================
// REQUESTS
// ============================================================================

export interface CreateMaintenanceWindowRequest {
  title: string;
  description: string;
  type: MaintenanceType;
  scheduledStart: string;
  scheduledEnd: string;
  notifyUsers?: boolean;
  notifyMinutesBefore?: number;
  affectedServices?: string[];
}

export interface CancelMaintenanceRequest {
  reason: string;
}

export interface UpdateScheduleRequest {
  newStart: string;
  newEnd: string;
}

export interface UpdateNotesRequest {
  notes: string;
}
```

---

## 📡 Service Layer

```typescript
// src/services/maintenanceService.ts
import { apiClient } from "./api-client";
import type {
  MaintenanceStatus,
  MaintenanceWindow,
  CreateMaintenanceWindowRequest,
  CancelMaintenanceRequest,
  UpdateScheduleRequest,
  UpdateNotesRequest,
} from "@/types/maintenance";

class MaintenanceService {
  // ============================================================================
  // PUBLIC
  // ============================================================================

  async getStatus(): Promise<MaintenanceStatus> {
    const response = await apiClient.get<MaintenanceStatus>(
      "/api/maintenance/status",
    );
    return response.data;
  }

  async getUpcoming(days: number = 7): Promise<MaintenanceWindow[]> {
    const response = await apiClient.get<MaintenanceWindow[]>(
      `/api/maintenance/upcoming?days=${days}`,
    );
    return response.data;
  }

  // ============================================================================
  // ADMIN
  // ============================================================================

  async getAll(): Promise<MaintenanceWindow[]> {
    const response =
      await apiClient.get<MaintenanceWindow[]>("/api/maintenance");
    return response.data;
  }

  async getById(id: string): Promise<MaintenanceWindow> {
    const response = await apiClient.get<MaintenanceWindow>(
      `/api/maintenance/${id}`,
    );
    return response.data;
  }

  async create(
    request: CreateMaintenanceWindowRequest,
  ): Promise<MaintenanceWindow> {
    const response = await apiClient.post<MaintenanceWindow>(
      "/api/maintenance",
      request,
    );
    return response.data;
  }

  async start(id: string): Promise<MaintenanceWindow> {
    const response = await apiClient.post<MaintenanceWindow>(
      `/api/maintenance/${id}/start`,
    );
    return response.data;
  }

  async complete(id: string): Promise<MaintenanceWindow> {
    const response = await apiClient.post<MaintenanceWindow>(
      `/api/maintenance/${id}/complete`,
    );
    return response.data;
  }

  async cancel(
    id: string,
    request: CancelMaintenanceRequest,
  ): Promise<MaintenanceWindow> {
    const response = await apiClient.post<MaintenanceWindow>(
      `/api/maintenance/${id}/cancel`,
      request,
    );
    return response.data;
  }

  async updateSchedule(
    id: string,
    request: UpdateScheduleRequest,
  ): Promise<MaintenanceWindow> {
    const response = await apiClient.put<MaintenanceWindow>(
      `/api/maintenance/${id}/schedule`,
      request,
    );
    return response.data;
  }

  async updateNotes(
    id: string,
    request: UpdateNotesRequest,
  ): Promise<MaintenanceWindow> {
    const response = await apiClient.put<MaintenanceWindow>(
      `/api/maintenance/${id}/notes`,
      request,
    );
    return response.data;
  }

  async delete(id: string): Promise<void> {
    await apiClient.delete(`/api/maintenance/${id}`);
  }
}

export const maintenanceService = new MaintenanceService();
```

---

## 🎣 React Query Hooks

```typescript
// src/hooks/useMaintenance.ts
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { maintenanceService } from "@/services/maintenanceService";
import type { CreateMaintenanceWindowRequest } from "@/types/maintenance";

export const maintenanceKeys = {
  all: ["maintenance"] as const,
  status: () => [...maintenanceKeys.all, "status"] as const,
  upcoming: (days: number) =>
    [...maintenanceKeys.all, "upcoming", days] as const,
  list: () => [...maintenanceKeys.all, "list"] as const,
  detail: (id: string) => [...maintenanceKeys.all, "detail", id] as const,
};

// PUBLIC HOOKS
export function useMaintenanceStatus() {
  return useQuery({
    queryKey: maintenanceKeys.status(),
    queryFn: () => maintenanceService.getStatus(),
    refetchInterval: 60000, // Check every minute
    staleTime: 30000,
  });
}

export function useUpcomingMaintenance(days: number = 7) {
  return useQuery({
    queryKey: maintenanceKeys.upcoming(days),
    queryFn: () => maintenanceService.getUpcoming(days),
  });
}

// ADMIN HOOKS
export function useMaintenanceWindows() {
  return useQuery({
    queryKey: maintenanceKeys.list(),
    queryFn: () => maintenanceService.getAll(),
  });
}

export function useMaintenanceWindow(id: string) {
  return useQuery({
    queryKey: maintenanceKeys.detail(id),
    queryFn: () => maintenanceService.getById(id),
    enabled: !!id,
  });
}

export function useCreateMaintenanceWindow() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateMaintenanceWindowRequest) =>
      maintenanceService.create(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: maintenanceKeys.list() });
      queryClient.invalidateQueries({ queryKey: maintenanceKeys.upcoming(7) });
    },
  });
}

export function useStartMaintenance() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => maintenanceService.start(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: maintenanceKeys.all });
    },
  });
}

export function useCompleteMaintenance() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => maintenanceService.complete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: maintenanceKeys.all });
    },
  });
}
```

---

## 🧩 Componente de Ejemplo

```typescript
// src/components/common/MaintenanceBanner.tsx
import { useMaintenanceStatus, useUpcomingMaintenance } from "@/hooks/useMaintenance";
import { FiAlertTriangle, FiInfo, FiClock } from "react-icons/fi";
import { formatDistanceToNow } from "date-fns";
import { es } from "date-fns/locale";

export const MaintenanceBanner = () => {
  const { data: status } = useMaintenanceStatus();
  const { data: upcoming } = useUpcomingMaintenance(3);

  // Active maintenance
  if (status?.isMaintenanceMode && status.maintenanceWindow) {
    const maint = status.maintenanceWindow;
    return (
      <div className="bg-red-600 text-white px-4 py-3">
        <div className="max-w-7xl mx-auto flex items-center gap-3">
          <FiAlertTriangle className="w-5 h-5 flex-shrink-0" />
          <div>
            <strong>{maint.title}</strong>
            <span className="ml-2 text-red-100">{maint.description}</span>
            {maint.affectedServices.length > 0 && (
              <span className="ml-2 text-red-200">
                Servicios afectados: {maint.affectedServices.join(", ")}
              </span>
            )}
          </div>
        </div>
      </div>
    );
  }

  // Upcoming maintenance
  if (upcoming && upcoming.length > 0) {
    const next = upcoming[0];
    return (
      <div className="bg-yellow-50 border-b border-yellow-200 px-4 py-2">
        <div className="max-w-7xl mx-auto flex items-center gap-2 text-sm text-yellow-800">
          <FiClock className="w-4 h-4" />
          <span>
            Mantenimiento programado: <strong>{next.title}</strong> -
            {formatDistanceToNow(new Date(next.scheduledStart), {
              addSuffix: true,
              locale: es
            })}
          </span>
          <a href="/maintenance" className="ml-auto underline">
            Más información
          </a>
        </div>
      </div>
    );
  }

  return null;
};
```

---

## 🎉 Resumen

✅ **11 Endpoints documentados**  
✅ **TypeScript Types** (MaintenanceWindow, Status, Types)  
✅ **Service Layer** (11 métodos)  
✅ **React Query Hooks** (7 hooks con polling)  
✅ **Componente ejemplo** (MaintenanceBanner)

---

_Última actualización: Enero 30, 2026_
