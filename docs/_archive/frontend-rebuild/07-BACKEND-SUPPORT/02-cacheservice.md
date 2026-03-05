# 🗄️ CacheService - Documentación Frontend

> **Servicio:** CacheService  
> **Puerto:** 5095 (dev) / 8080 (k8s)  
> **Estado:** ✅ Listo para producción  
> **Última actualización:** Enero 2026

---

## 📋 Descripción

Servicio de caché distribuido basado en **Redis** que proporciona almacenamiento temporal de datos para mejorar la performance del frontend. Soporta multi-tenancy, locks distribuidos y TTL configurable.

---

## 🎯 Casos de Uso Frontend

### 1. Caché de Sesión de Usuario

```typescript
// Almacenar datos de sesión para evitar llamadas repetidas
const cacheUserSession = async (userId: string, sessionData: object) => {
  await cacheService.set(
    `session:${userId}`,
    JSON.stringify(sessionData),
    3600,
  );
};
```

### 2. Caché de Catálogo de Vehículos

```typescript
// Cachear lista de marcas/modelos (cambia poco)
const getCachedMakes = async () => {
  const cached = await cacheService.get("catalog:makes");
  if (cached) return JSON.parse(cached);

  const makes = await vehicleService.getMakes();
  await cacheService.set("catalog:makes", JSON.stringify(makes), 86400); // 24h
  return makes;
};
```

### 3. Prevenir Acciones Duplicadas

```typescript
// Lock para evitar publicaciones duplicadas
const publishVehicle = async (vehicleData: VehicleData) => {
  const lockKey = `publish:${vehicleData.vin}`;
  const lock = await cacheService.acquireLock(lockKey, userId, 30);

  if (!lock.acquired) {
    throw new Error("Esta publicación ya está siendo procesada");
  }

  try {
    // Procesar publicación
  } finally {
    await cacheService.releaseLock(lockKey, userId);
  }
};
```

---

## 📡 API Endpoints

### Cache Operations

| Método   | Endpoint                       | Descripción             |
| -------- | ------------------------------ | ----------------------- |
| `GET`    | `/api/cache/{key}`             | Obtener valor del cache |
| `POST`   | `/api/cache`                   | Guardar valor en cache  |
| `DELETE` | `/api/cache/{key}`             | Eliminar valor          |
| `DELETE` | `/api/cache/pattern/{pattern}` | Eliminar por patrón     |
| `DELETE` | `/api/cache/flush`             | Limpiar todo el cache   |

### Distributed Locks

| Método   | Endpoint                   | Descripción     |
| -------- | -------------------------- | --------------- |
| `POST`   | `/api/locks/acquire`       | Adquirir lock   |
| `DELETE` | `/api/locks/{key}/release` | Liberar lock    |
| `GET`    | `/api/locks/{key}/status`  | Estado del lock |

### Statistics

| Método | Endpoint           | Descripción         |
| ------ | ------------------ | ------------------- |
| `GET`  | `/api/cache/stats` | Estadísticas de uso |
| `GET`  | `/api/cache/keys`  | Listar keys activas |

---

## 🔧 Cliente TypeScript

```typescript
// services/cacheService.ts

import { apiClient } from "./apiClient";

interface CacheEntry {
  key: string;
  value: string;
  tenantId?: string;
  ttlSeconds?: number;
}

interface LockRequest {
  key: string;
  ownerId: string;
  ttlSeconds: number;
}

interface LockResponse {
  acquired: boolean;
  lockKey: string;
  expiresAt?: string;
}

interface CacheStats {
  totalKeys: number;
  hitCount: number;
  missCount: number;
  hitRatio: number;
  memoryUsedMb: number;
}

export const cacheService = {
  // GET valor del cache
  async get(key: string, tenantId?: string): Promise<string | null> {
    const params = tenantId ? { tenantId } : {};
    const response = await apiClient.get(
      `/api/cache/${encodeURIComponent(key)}`,
      { params },
    );
    return response.data?.value ?? null;
  },

  // SET valor en cache
  async set(
    key: string,
    value: string,
    ttlSeconds: number = 3600,
    tenantId?: string,
  ): Promise<void> {
    const entry: CacheEntry = { key, value, ttlSeconds, tenantId };
    await apiClient.post("/api/cache", entry);
  },

  // DELETE valor
  async delete(key: string): Promise<void> {
    await apiClient.delete(`/api/cache/${encodeURIComponent(key)}`);
  },

  // DELETE por patrón (ej: "user:*")
  async deleteByPattern(pattern: string): Promise<number> {
    const response = await apiClient.delete(
      `/api/cache/pattern/${encodeURIComponent(pattern)}`,
    );
    return response.data.deletedCount;
  },

  // LOCK - Adquirir
  async acquireLock(
    key: string,
    ownerId: string,
    ttlSeconds: number = 30,
  ): Promise<LockResponse> {
    const request: LockRequest = { key, ownerId, ttlSeconds };
    const response = await apiClient.post("/api/locks/acquire", request);
    return response.data;
  },

  // LOCK - Liberar
  async releaseLock(key: string, ownerId: string): Promise<boolean> {
    const response = await apiClient.delete(
      `/api/locks/${encodeURIComponent(key)}/release`,
      {
        params: { ownerId },
      },
    );
    return response.data.released;
  },

  // Estadísticas
  async getStats(): Promise<CacheStats> {
    const response = await apiClient.get("/api/cache/stats");
    return response.data;
  },
};
```

---

## 🪝 Hook de React

```typescript
// hooks/useCache.ts

import { useState, useCallback } from "react";
import { cacheService } from "../services/cacheService";

interface UseCacheOptions {
  ttl?: number;
  tenantId?: string;
}

export function useCache<T>(key: string, options: UseCacheOptions = {}) {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  const get = useCallback(async (): Promise<T | null> => {
    setLoading(true);
    setError(null);
    try {
      const cached = await cacheService.get(key, options.tenantId);
      if (cached) {
        const parsed = JSON.parse(cached) as T;
        setData(parsed);
        return parsed;
      }
      return null;
    } catch (err) {
      setError(err as Error);
      return null;
    } finally {
      setLoading(false);
    }
  }, [key, options.tenantId]);

  const set = useCallback(
    async (value: T): Promise<void> => {
      setLoading(true);
      setError(null);
      try {
        await cacheService.set(
          key,
          JSON.stringify(value),
          options.ttl ?? 3600,
          options.tenantId,
        );
        setData(value);
      } catch (err) {
        setError(err as Error);
        throw err;
      } finally {
        setLoading(false);
      }
    },
    [key, options.ttl, options.tenantId],
  );

  const invalidate = useCallback(async (): Promise<void> => {
    await cacheService.delete(key);
    setData(null);
  }, [key]);

  return { data, loading, error, get, set, invalidate };
}

// Uso:
// const { data, get, set, invalidate } = useCache<User[]>('users:list', { ttl: 3600 });
```

---

## 🧪 Testing

### Vitest Mocks

```typescript
// __mocks__/cacheService.ts
export const cacheService = {
  get: vi.fn().mockResolvedValue(null),
  set: vi.fn().mockResolvedValue(undefined),
  delete: vi.fn().mockResolvedValue(undefined),
  deleteByPattern: vi.fn().mockResolvedValue(5),
  acquireLock: vi.fn().mockResolvedValue({ acquired: true, lockKey: "test" }),
  releaseLock: vi.fn().mockResolvedValue(true),
  getStats: vi.fn().mockResolvedValue({
    totalKeys: 100,
    hitCount: 80,
    missCount: 20,
    hitRatio: 0.8,
    memoryUsedMb: 50,
  }),
};
```

### E2E Test (Playwright)

```typescript
// e2e/cache.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Cache Integration", () => {
  test("should cache vehicle catalog", async ({ request }) => {
    // Primera llamada - miss
    const response1 = await request.get("/api/vehicles/makes");
    expect(response1.ok()).toBeTruthy();

    // Verificar que se cacheó
    const cacheResponse = await request.get("/api/cache/catalog:makes");
    expect(cacheResponse.ok()).toBeTruthy();
  });

  test("should handle cache invalidation", async ({ request }) => {
    // Invalidar cache de catálogo
    const deleteResponse = await request.delete("/api/cache/pattern/catalog:*");
    expect(deleteResponse.ok()).toBeTruthy();
    const { deletedCount } = await deleteResponse.json();
    expect(deletedCount).toBeGreaterThanOrEqual(0);
  });
});
```

---

## ⚙️ Configuración

### Variables de Entorno

```env
# Redis connection
REDIS_HOST=localhost
REDIS_PORT=6379
REDIS_PASSWORD=
REDIS_DATABASE=0

# Cache settings
CACHE_DEFAULT_TTL_SECONDS=3600
CACHE_MAX_MEMORY_MB=512

# Multi-tenancy
CACHE_TENANT_ISOLATION=true
```

### Estrategias de Invalidación

| Estrategia   | Uso                            | Ejemplo                   |
| ------------ | ------------------------------ | ------------------------- |
| TTL          | Datos que expiran naturalmente | Sesiones, tokens          |
| On-Write     | Invalidar al actualizar datos  | Perfil de usuario         |
| Pattern      | Invalidar grupo de keys        | `vehicle:*` al re-indexar |
| Event-Driven | Invalidar por eventos          | Precio actualizado        |

---

## 📊 Métricas Importantes

```typescript
// Dashboard de métricas de cache
interface CacheMetrics {
  hitRatio: number; // > 0.8 es bueno
  latencyP99Ms: number; // < 10ms ideal
  memoryUsage: number; // % de memoria usada
  evictionRate: number; // Keys expulsadas/minuto
}
```

---

## 🔗 Referencias

- [README del servicio](../../../backend/CacheService/README.md)
- [Redis Best Practices](https://redis.io/docs/manual/patterns/)
- [API Integration](../05-API-INTEGRATION/)

---

_Este servicio es crítico para la performance del frontend. Monitorear hit ratio regularmente._
