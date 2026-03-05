# 📊 AuditService - Documentación Frontend

> **Servicio:** AuditService  
> **Puerto:** 5002 (dev) / 8080 (k8s)  
> **Estado:** ✅ Listo para producción  
> **Última actualización:** Enero 2026

---

## 📋 Descripción

Servicio centralizado de auditoría que registra todas las operaciones del sistema. Proporciona trazabilidad completa para compliance, debugging y analytics. El frontend puede consultar logs de auditoría para mostrar historial de actividad.

---

## 🎯 Casos de Uso Frontend

### 1. Historial de Actividad del Usuario

```typescript
// Mostrar actividad reciente en perfil de usuario
const getUserActivity = async (userId: string) => {
  const logs = await auditService.getLogsByUser(userId, {
    limit: 20,
    actions: ["CREATE", "UPDATE", "DELETE"],
  });
  return logs.map((log) => ({
    action: log.action,
    entity: log.entityType,
    timestamp: log.createdAt,
    description: formatAuditDescription(log),
  }));
};
```

### 2. Auditoría de Vehículo

```typescript
// Ver historial de cambios de un vehículo
const getVehicleAuditTrail = async (vehicleId: string) => {
  const logs = await auditService.getLogsByEntity("Vehicle", vehicleId);
  return logs; // Muestra quién creó, editó, publicó, etc.
};
```

### 3. Dashboard de Admin

```typescript
// Métricas de actividad del sistema
const getSystemActivityMetrics = async () => {
  const today = new Date().toISOString().split("T")[0];
  const logs = await auditService.search({
    from: today,
    groupBy: "action",
  });
  return logs; // CREATE: 150, UPDATE: 230, DELETE: 45, LOGIN: 890
};
```

---

## 📡 API Endpoints

### Queries

| Método | Endpoint                        | Descripción            |
| ------ | ------------------------------- | ---------------------- |
| `GET`  | `/api/audit`                    | Listar logs (paginado) |
| `GET`  | `/api/audit/{id}`               | Detalle de un log      |
| `GET`  | `/api/audit/user/{userId}`      | Logs por usuario       |
| `GET`  | `/api/audit/entity/{type}/{id}` | Logs por entidad       |
| `GET`  | `/api/audit/search`             | Búsqueda avanzada      |
| `GET`  | `/api/audit/stats`              | Estadísticas           |

### Query Parameters

| Parámetro    | Tipo     | Descripción                                              |
| ------------ | -------- | -------------------------------------------------------- |
| `action`     | string   | Filtrar por acción (CREATE, UPDATE, DELETE, LOGIN, etc.) |
| `severity`   | string   | Filtrar por severidad (LOW, MEDIUM, HIGH, CRITICAL)      |
| `from`       | datetime | Fecha desde                                              |
| `to`         | datetime | Fecha hasta                                              |
| `entityType` | string   | Tipo de entidad (Vehicle, User, Dealer, etc.)            |
| `userId`     | string   | ID del usuario que ejecutó la acción                     |
| `page`       | number   | Número de página                                         |
| `pageSize`   | number   | Tamaño de página (max: 100)                              |

---

## 🔧 Cliente TypeScript

```typescript
// services/auditService.ts

import { apiClient } from "./apiClient";

// Tipos
interface AuditLog {
  id: string;
  action: AuditAction;
  severity: AuditSeverity;
  userId: string;
  userName?: string;
  userEmail?: string;
  entityType: string;
  entityId: string;
  entityName?: string;
  oldValue?: Record<string, any>;
  newValue?: Record<string, any>;
  changes?: FieldChange[];
  ipAddress?: string;
  userAgent?: string;
  correlationId?: string;
  metadata?: Record<string, any>;
  createdAt: string;
}

type AuditAction =
  | "CREATE"
  | "UPDATE"
  | "DELETE"
  | "LOGIN"
  | "LOGOUT"
  | "LOGIN_FAILED"
  | "VIEW"
  | "DOWNLOAD"
  | "EXPORT"
  | "APPROVE"
  | "REJECT"
  | "SUSPEND"
  | "PAYMENT"
  | "REFUND";

type AuditSeverity = "LOW" | "MEDIUM" | "HIGH" | "CRITICAL";

interface FieldChange {
  field: string;
  oldValue: any;
  newValue: any;
}

interface AuditSearchParams {
  action?: AuditAction;
  severity?: AuditSeverity;
  userId?: string;
  entityType?: string;
  entityId?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

interface AuditStats {
  totalLogs: number;
  byAction: Record<string, number>;
  bySeverity: Record<string, number>;
  byEntityType: Record<string, number>;
  topUsers: { userId: string; count: number }[];
}

interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export const auditService = {
  // Listar logs
  async getLogs(
    params: AuditSearchParams = {},
  ): Promise<PaginatedResult<AuditLog>> {
    const response = await apiClient.get("/api/audit", { params });
    return response.data;
  },

  // Obtener log por ID
  async getLog(id: string): Promise<AuditLog> {
    const response = await apiClient.get(`/api/audit/${id}`);
    return response.data;
  },

  // Logs por usuario
  async getLogsByUser(
    userId: string,
    params: Partial<AuditSearchParams> = {},
  ): Promise<PaginatedResult<AuditLog>> {
    const response = await apiClient.get(`/api/audit/user/${userId}`, {
      params,
    });
    return response.data;
  },

  // Logs por entidad
  async getLogsByEntity(
    entityType: string,
    entityId: string,
  ): Promise<AuditLog[]> {
    const response = await apiClient.get(
      `/api/audit/entity/${entityType}/${entityId}`,
    );
    return response.data;
  },

  // Búsqueda avanzada
  async search(params: AuditSearchParams): Promise<PaginatedResult<AuditLog>> {
    const response = await apiClient.get("/api/audit/search", { params });
    return response.data;
  },

  // Estadísticas
  async getStats(from?: string, to?: string): Promise<AuditStats> {
    const params = { from, to };
    const response = await apiClient.get("/api/audit/stats", { params });
    return response.data;
  },
};
```

---

## 🪝 Hook de React

```typescript
// hooks/useAuditLogs.ts

import { useQuery } from "@tanstack/react-query";
import { auditService, AuditSearchParams } from "../services/auditService";

export function useAuditLogs(params: AuditSearchParams = {}) {
  return useQuery({
    queryKey: ["audit-logs", params],
    queryFn: () => auditService.getLogs(params),
    staleTime: 30000, // 30 segundos
  });
}

export function useUserActivity(userId: string, limit: number = 10) {
  return useQuery({
    queryKey: ["user-activity", userId, limit],
    queryFn: () => auditService.getLogsByUser(userId, { pageSize: limit }),
    enabled: !!userId,
  });
}

export function useEntityAuditTrail(entityType: string, entityId: string) {
  return useQuery({
    queryKey: ["entity-audit", entityType, entityId],
    queryFn: () => auditService.getLogsByEntity(entityType, entityId),
    enabled: !!entityType && !!entityId,
  });
}

export function useAuditStats(dateRange?: { from: string; to: string }) {
  return useQuery({
    queryKey: ["audit-stats", dateRange],
    queryFn: () => auditService.getStats(dateRange?.from, dateRange?.to),
  });
}
```

---

## 🧩 Componentes de Ejemplo

### Activity Feed

```tsx
// components/ActivityFeed.tsx

import { useUserActivity } from "../hooks/useAuditLogs";
import { formatDistanceToNow } from "date-fns";
import { es } from "date-fns/locale";

const actionIcons: Record<string, string> = {
  CREATE: "➕",
  UPDATE: "✏️",
  DELETE: "🗑️",
  LOGIN: "🔑",
  VIEW: "👁️",
  PAYMENT: "💳",
};

const actionLabels: Record<string, string> = {
  CREATE: "creó",
  UPDATE: "actualizó",
  DELETE: "eliminó",
  LOGIN: "inició sesión",
  VIEW: "visualizó",
  PAYMENT: "realizó pago en",
};

export function ActivityFeed({ userId }: { userId: string }) {
  const { data, isLoading, error } = useUserActivity(userId, 10);

  if (isLoading) return <Skeleton count={5} />;
  if (error) return <ErrorMessage error={error} />;

  return (
    <div className="space-y-3">
      <h3 className="font-semibold">Actividad Reciente</h3>

      {data?.items.length === 0 ? (
        <p className="text-gray-500">Sin actividad reciente</p>
      ) : (
        <ul className="space-y-2">
          {data?.items.map((log) => (
            <li key={log.id} className="flex items-start gap-3 py-2 border-b">
              <span className="text-xl">{actionIcons[log.action] || "📋"}</span>
              <div className="flex-1">
                <p className="text-sm">
                  <span className="font-medium">
                    {actionLabels[log.action] || log.action}
                  </span>{" "}
                  <span className="text-gray-600">{log.entityType}</span>
                  {log.entityName && (
                    <span className="font-medium"> "{log.entityName}"</span>
                  )}
                </p>
                <p className="text-xs text-gray-400">
                  {formatDistanceToNow(new Date(log.createdAt), {
                    addSuffix: true,
                    locale: es,
                  })}
                </p>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
```

### Audit Trail de Vehículo

```tsx
// components/VehicleAuditTrail.tsx

import { useEntityAuditTrail } from "../hooks/useAuditLogs";

export function VehicleAuditTrail({ vehicleId }: { vehicleId: string }) {
  const { data: logs, isLoading } = useEntityAuditTrail("Vehicle", vehicleId);

  if (isLoading) return <Spinner />;

  return (
    <div className="space-y-4">
      <h3 className="font-semibold">Historial de Cambios</h3>

      <div className="relative border-l-2 border-gray-200 ml-3">
        {logs?.map((log, index) => (
          <div key={log.id} className="mb-6 ml-6">
            <div className="absolute -left-3 w-6 h-6 bg-blue-500 rounded-full flex items-center justify-center">
              <span className="text-white text-xs">{logs.length - index}</span>
            </div>

            <div className="bg-gray-50 p-3 rounded-lg">
              <div className="flex justify-between items-start">
                <div>
                  <span className="font-medium">{log.action}</span>
                  <span className="text-gray-500 text-sm ml-2">
                    por {log.userName || log.userId}
                  </span>
                </div>
                <time className="text-xs text-gray-400">
                  {new Date(log.createdAt).toLocaleString()}
                </time>
              </div>

              {log.changes && log.changes.length > 0 && (
                <ul className="mt-2 text-sm">
                  {log.changes.map((change, i) => (
                    <li key={i} className="text-gray-600">
                      <span className="font-medium">{change.field}:</span>{" "}
                      <span className="line-through text-red-400">
                        {String(change.oldValue)}
                      </span>
                      {" → "}
                      <span className="text-green-600">
                        {String(change.newValue)}
                      </span>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
```

---

## 🧪 Testing

### Vitest Mocks

```typescript
// __mocks__/auditService.ts
export const auditService = {
  getLogs: vi.fn().mockResolvedValue({
    items: [
      {
        id: "1",
        action: "CREATE",
        severity: "LOW",
        userId: "user-1",
        entityType: "Vehicle",
        entityId: "v-123",
        createdAt: new Date().toISOString(),
      },
    ],
    totalCount: 1,
    page: 1,
    pageSize: 10,
    totalPages: 1,
  }),
  getLogsByUser: vi.fn().mockResolvedValue({ items: [], totalCount: 0 }),
  getLogsByEntity: vi.fn().mockResolvedValue([]),
  getStats: vi.fn().mockResolvedValue({
    totalLogs: 1500,
    byAction: { CREATE: 400, UPDATE: 600, DELETE: 100, LOGIN: 400 },
    bySeverity: { LOW: 1000, MEDIUM: 400, HIGH: 90, CRITICAL: 10 },
  }),
};
```

### E2E Test (Playwright)

```typescript
// e2e/audit.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Audit Trail", () => {
  test("should show user activity feed", async ({ page }) => {
    await page.goto("/profile");

    await expect(page.locator('[data-testid="activity-feed"]')).toBeVisible();
    await expect(
      page.locator('[data-testid="activity-item"]'),
    ).toHaveCount.greaterThan(0);
  });

  test("should show vehicle audit trail for admin", async ({ page }) => {
    await page.goto("/admin/vehicles/v-123");
    await page.click('[data-testid="audit-trail-tab"]');

    await expect(page.locator('[data-testid="audit-entry"]')).toBeVisible();
  });
});
```

---

## 📊 Acciones de Auditoría

| Acción         | Severidad | Descripción                   |
| -------------- | --------- | ----------------------------- |
| `CREATE`       | LOW       | Creación de entidad           |
| `UPDATE`       | LOW       | Actualización de campos       |
| `DELETE`       | MEDIUM    | Eliminación de entidad        |
| `LOGIN`        | LOW       | Inicio de sesión exitoso      |
| `LOGIN_FAILED` | HIGH      | Intento de login fallido      |
| `LOGOUT`       | LOW       | Cierre de sesión              |
| `VIEW`         | LOW       | Visualización de datos        |
| `EXPORT`       | MEDIUM    | Exportación de datos          |
| `APPROVE`      | MEDIUM    | Aprobación (dealer, vehículo) |
| `REJECT`       | MEDIUM    | Rechazo                       |
| `SUSPEND`      | HIGH      | Suspensión de cuenta          |
| `PAYMENT`      | MEDIUM    | Transacción de pago           |
| `REFUND`       | HIGH      | Reembolso procesado           |

---

## 🔒 Consideraciones de Seguridad

1. **Acceso restringido**: Solo usuarios autenticados pueden ver sus propios logs
2. **Admin only**: Ver logs de otros usuarios requiere rol Admin
3. **Retención**: Logs se archivan después de 90 días, eliminan después de 365
4. **GDPR**: Datos sensibles se anonimizan en logs
5. **Inmutabilidad**: Los logs no se pueden editar ni eliminar (solo archivar)

---

## 🔗 Referencias

- [README del servicio](../../../backend/AuditService/README.md)
- [GDPR Compliance](../08-DGII-COMPLIANCE/)
- [Admin Dashboard](../04-PAGINAS/06-ADMIN/)

---

_Los logs de auditoría son inmutables y cumplen con requisitos de compliance._
