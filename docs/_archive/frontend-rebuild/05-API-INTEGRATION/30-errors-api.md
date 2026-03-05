# 🚨 30 - Error Service API

**Servicio:** ErrorService  
**Puerto:** 8080  
**Base Path:** `/api/errors`  
**Autenticación:** ✅ Requerida (Policy: ErrorServiceRead)

---

## 📖 Descripción

Servicio centralizado de logging y monitoreo de errores para todos los microservicios. Incluye:

- Registro de errores
- Consulta con filtros
- Estadísticas agregadas
- Rate limiting integrado

---

## 🎯 Endpoints Disponibles

| #   | Método | Endpoint               | Auth | Rate Limit | Descripción           |
| --- | ------ | ---------------------- | ---- | ---------- | --------------------- |
| 1   | `POST` | `/api/errors`          | ✅   | 200/min    | Registrar error       |
| 2   | `GET`  | `/api/errors`          | ✅   | 150/min    | Listar errores        |
| 3   | `GET`  | `/api/errors/{id}`     | ✅   | 200/min    | Obtener error         |
| 4   | `GET`  | `/api/errors/stats`    | ✅   | 100/min    | Estadísticas          |
| 5   | `GET`  | `/api/errors/services` | ✅   | 150/min    | Servicios con errores |

---

## 📝 Detalle de Endpoints

### 1. POST `/api/errors` - Registrar Error

**Request:**

```json
{
  "serviceName": "VehiclesSaleService",
  "errorType": "ValidationException",
  "message": "Price must be greater than zero",
  "stackTrace": "at VehiclesSaleService.Application.Validators...",
  "correlationId": "corr-123-abc",
  "userId": "user-456",
  "requestPath": "/api/vehicles",
  "requestMethod": "POST",
  "statusCode": 400,
  "additionalData": {
    "vehicleId": "v-789",
    "attemptedPrice": -1000
  }
}
```

**Response 200:**

```json
{
  "id": "error-001",
  "serviceName": "VehiclesSaleService",
  "errorType": "ValidationException",
  "message": "Price must be greater than zero",
  "createdAt": "2026-01-30T10:00:00Z"
}
```

---

### 2. GET `/api/errors` - Listar Errores

**Query Params:**

- `serviceName` (string): Filtrar por servicio
- `from` (datetime): Fecha inicio
- `to` (datetime): Fecha fin
- `page` (int): Página (default: 1)
- `pageSize` (int): Tamaño (default: 50)

**Response 200:**

```json
{
  "items": [
    {
      "id": "error-001",
      "serviceName": "VehiclesSaleService",
      "errorType": "ValidationException",
      "message": "Price must be greater than zero",
      "correlationId": "corr-123-abc",
      "statusCode": 400,
      "createdAt": "2026-01-30T10:00:00Z"
    },
    {
      "id": "error-002",
      "serviceName": "AuthService",
      "errorType": "UnauthorizedException",
      "message": "Invalid token",
      "correlationId": "corr-456-def",
      "statusCode": 401,
      "createdAt": "2026-01-30T09:55:00Z"
    }
  ],
  "totalCount": 245,
  "page": 1,
  "pageSize": 50,
  "totalPages": 5
}
```

---

### 4. GET `/api/errors/stats` - Estadísticas

**Query Params:**

- `from` (datetime): Fecha inicio
- `to` (datetime): Fecha fin

**Response 200:**

```json
{
  "totalErrors": 1250,
  "errorsByService": [
    { "serviceName": "VehiclesSaleService", "count": 450, "percentage": 36 },
    { "serviceName": "AuthService", "count": 320, "percentage": 25.6 },
    { "serviceName": "PaymentService", "count": 180, "percentage": 14.4 },
    { "serviceName": "MediaService", "count": 150, "percentage": 12 },
    { "serviceName": "Others", "count": 150, "percentage": 12 }
  ],
  "errorsByType": [
    { "errorType": "ValidationException", "count": 520 },
    { "errorType": "NotFoundException", "count": 280 },
    { "errorType": "UnauthorizedException", "count": 200 },
    { "errorType": "InternalServerError", "count": 150 },
    { "errorType": "TimeoutException", "count": 100 }
  ],
  "errorsByHour": [
    { "hour": "2026-01-30T09:00:00Z", "count": 45 },
    { "hour": "2026-01-30T10:00:00Z", "count": 62 }
  ],
  "averagePerDay": 89,
  "peakHour": "14:00",
  "mostAffectedEndpoint": "/api/vehicles"
}
```

---

### 5. GET `/api/errors/services` - Servicios con Errores

**Response 200:**

```json
{
  "services": [
    "VehiclesSaleService",
    "AuthService",
    "PaymentService",
    "MediaService",
    "NotificationService",
    "UserService",
    "BillingService"
  ],
  "totalServices": 7
}
```

---

## 🔧 TypeScript Types

```typescript
// ============================================================================
// ERROR LOGGING
// ============================================================================

export interface LogErrorRequest {
  serviceName: string;
  errorType: string;
  message: string;
  stackTrace?: string;
  correlationId?: string;
  userId?: string;
  requestPath?: string;
  requestMethod?: string;
  statusCode?: number;
  additionalData?: Record<string, any>;
}

export interface LogErrorResponse {
  id: string;
  serviceName: string;
  errorType: string;
  message: string;
  createdAt: string;
}

// ============================================================================
// ERROR QUERIES
// ============================================================================

export interface ErrorEntry {
  id: string;
  serviceName: string;
  errorType: string;
  message: string;
  stackTrace?: string;
  correlationId?: string;
  userId?: string;
  requestPath?: string;
  requestMethod?: string;
  statusCode?: number;
  additionalData?: Record<string, any>;
  createdAt: string;
}

export interface GetErrorsRequest {
  serviceName?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

export interface GetErrorsResponse {
  items: ErrorEntry[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// ============================================================================
// STATISTICS
// ============================================================================

export interface ErrorStats {
  totalErrors: number;
  errorsByService: { serviceName: string; count: number; percentage: number }[];
  errorsByType: { errorType: string; count: number }[];
  errorsByHour: { hour: string; count: number }[];
  averagePerDay: number;
  peakHour: string;
  mostAffectedEndpoint: string;
}

export interface ServiceNamesResponse {
  services: string[];
  totalServices: number;
}
```

---

## 📡 Service Layer

```typescript
// src/services/errorService.ts
import { apiClient } from "./api-client";
import type {
  LogErrorRequest,
  LogErrorResponse,
  ErrorEntry,
  GetErrorsRequest,
  GetErrorsResponse,
  ErrorStats,
  ServiceNamesResponse,
} from "@/types/error";

class ErrorService {
  async logError(request: LogErrorRequest): Promise<LogErrorResponse> {
    const response = await apiClient.post<{ data: LogErrorResponse }>(
      "/api/errors",
      request,
    );
    return response.data.data;
  }

  async getErrors(request: GetErrorsRequest = {}): Promise<GetErrorsResponse> {
    const response = await apiClient.get<{ data: GetErrorsResponse }>(
      "/api/errors",
      {
        params: request,
      },
    );
    return response.data.data;
  }

  async getError(id: string): Promise<ErrorEntry> {
    const response = await apiClient.get<{ data: ErrorEntry }>(
      `/api/errors/${id}`,
    );
    return response.data.data;
  }

  async getStats(from?: string, to?: string): Promise<ErrorStats> {
    const response = await apiClient.get<{ data: ErrorStats }>(
      "/api/errors/stats",
      {
        params: { from, to },
      },
    );
    return response.data.data;
  }

  async getServiceNames(): Promise<ServiceNamesResponse> {
    const response = await apiClient.get<{ data: ServiceNamesResponse }>(
      "/api/errors/services",
    );
    return response.data.data;
  }
}

export const errorService = new ErrorService();
```

---

## 🎣 React Query Hooks

```typescript
// src/hooks/useErrors.ts
import { useMutation, useQuery } from "@tanstack/react-query";
import { errorService } from "@/services/errorService";
import type { LogErrorRequest, GetErrorsRequest } from "@/types/error";

export const errorKeys = {
  all: ["errors"] as const,
  list: (params: GetErrorsRequest) =>
    [...errorKeys.all, "list", params] as const,
  detail: (id: string) => [...errorKeys.all, "detail", id] as const,
  stats: (from?: string, to?: string) =>
    [...errorKeys.all, "stats", from, to] as const,
  services: () => [...errorKeys.all, "services"] as const,
};

export function useErrors(params: GetErrorsRequest = {}) {
  return useQuery({
    queryKey: errorKeys.list(params),
    queryFn: () => errorService.getErrors(params),
  });
}

export function useError(id: string) {
  return useQuery({
    queryKey: errorKeys.detail(id),
    queryFn: () => errorService.getError(id),
    enabled: !!id,
  });
}

export function useErrorStats(from?: string, to?: string) {
  return useQuery({
    queryKey: errorKeys.stats(from, to),
    queryFn: () => errorService.getStats(from, to),
  });
}

export function useServiceNames() {
  return useQuery({
    queryKey: errorKeys.services(),
    queryFn: () => errorService.getServiceNames(),
  });
}

export function useLogError() {
  return useMutation({
    mutationFn: (request: LogErrorRequest) => errorService.logError(request),
  });
}
```

---

## 🧩 Componente de Ejemplo

```typescript
// src/components/admin/ErrorDashboard.tsx
import { useErrors, useErrorStats, useServiceNames } from "@/hooks/useErrors";
import { useState } from "react";

export const ErrorDashboard = () => {
  const [serviceName, setServiceName] = useState<string>();
  const { data: services } = useServiceNames();
  const { data: stats } = useErrorStats();
  const { data: errors } = useErrors({ serviceName, pageSize: 20 });

  return (
    <div className="space-y-6">
      {/* Stats Cards */}
      <div className="grid grid-cols-4 gap-4">
        <div className="bg-red-50 p-4 rounded-lg">
          <p className="text-3xl font-bold text-red-600">{stats?.totalErrors}</p>
          <p className="text-sm text-gray-600">Errores Totales</p>
        </div>
        <div className="bg-yellow-50 p-4 rounded-lg">
          <p className="text-3xl font-bold text-yellow-600">{stats?.averagePerDay}</p>
          <p className="text-sm text-gray-600">Promedio/Día</p>
        </div>
        <div className="bg-blue-50 p-4 rounded-lg">
          <p className="text-3xl font-bold text-blue-600">{stats?.peakHour}</p>
          <p className="text-sm text-gray-600">Hora Pico</p>
        </div>
        <div className="bg-purple-50 p-4 rounded-lg">
          <p className="text-3xl font-bold text-purple-600">{services?.totalServices}</p>
          <p className="text-sm text-gray-600">Servicios</p>
        </div>
      </div>

      {/* Filter */}
      <select
        value={serviceName || ""}
        onChange={(e) => setServiceName(e.target.value || undefined)}
        className="select select-bordered"
      >
        <option value="">Todos los servicios</option>
        {services?.services.map(s => (
          <option key={s} value={s}>{s}</option>
        ))}
      </select>

      {/* Error List */}
      <div className="overflow-x-auto">
        <table className="table">
          <thead>
            <tr>
              <th>Servicio</th>
              <th>Tipo</th>
              <th>Mensaje</th>
              <th>Status</th>
              <th>Fecha</th>
            </tr>
          </thead>
          <tbody>
            {errors?.items.map(error => (
              <tr key={error.id}>
                <td>{error.serviceName}</td>
                <td><code>{error.errorType}</code></td>
                <td className="truncate max-w-xs">{error.message}</td>
                <td>{error.statusCode}</td>
                <td>{new Date(error.createdAt).toLocaleString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};
```

---

## 🎉 Resumen

✅ **5 Endpoints documentados**  
✅ **Rate Limiting** integrado (100-200 req/min)  
✅ **TypeScript Types** (LogError, GetErrors, Stats)  
✅ **Service Layer** (5 métodos)  
✅ **React Query Hooks** (5 hooks)  
✅ **Componente ejemplo** (ErrorDashboard)

---

_Última actualización: Enero 30, 2026_
