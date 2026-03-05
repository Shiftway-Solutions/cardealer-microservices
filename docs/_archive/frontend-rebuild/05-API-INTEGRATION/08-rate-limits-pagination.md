# ⚡ Rate Limits y Paginación

> **Tiempo estimado:** 25 minutos de lectura
> **Propósito:** Documentar límites y patrones de paginación del API
> **Última actualización:** Enero 31, 2026

---

## 📋 OBJETIVO

Documentar:

- Límites de rate limiting por endpoint
- Patrones de paginación del API
- Implementación en frontend
- Manejo de errores 429

---

## 🚦 RATE LIMITING

### Configuración Global

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                           RATE LIMITING - OKLA API                                   │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  Implementación: Token Bucket Algorithm (via Ocelot Gateway)                        │
│  Almacenamiento: Redis (distribuido entre replicas)                                 │
│  Headers de respuesta:                                                               │
│  ├── X-RateLimit-Limit: 100       # Límite total                                    │
│  ├── X-RateLimit-Remaining: 95    # Requests restantes                              │
│  ├── X-RateLimit-Reset: 1706745600 # Unix timestamp de reset                        │
│  └── Retry-After: 60              # Segundos (solo en 429)                          │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### Límites por Tipo de Usuario

| Tipo de Usuario | Requests/Minuto | Requests/Hora | Requests/Día |
|-----------------|-----------------|---------------|--------------|
| **Anónimo** | 30 | 500 | 5,000 |
| **Autenticado** | 100 | 2,000 | 20,000 |
| **Dealer (Starter)** | 200 | 5,000 | 50,000 |
| **Dealer (Pro)** | 500 | 15,000 | 150,000 |
| **Dealer (Enterprise)** | 1,000 | 30,000 | Ilimitado |
| **Admin** | 1,000 | 30,000 | Ilimitado |

### Límites por Endpoint

#### 🔐 Auth Endpoints (Más restrictivos)

| Endpoint | Límite | Período | Razón |
|----------|--------|---------|-------|
| `POST /auth/login` | 5 | 1 min | Prevenir brute force |
| `POST /auth/register` | 3 | 1 min | Prevenir spam |
| `POST /auth/forgot-password` | 3 | 1 min | Prevenir abuso |
| `POST /auth/verify-2fa` | 5 | 1 min | Prevenir brute force |
| `POST /auth/resend-verification` | 2 | 5 min | Prevenir spam email |

#### 🚗 Vehicles Endpoints

| Endpoint | Límite | Período | Razón |
|----------|--------|---------|-------|
| `GET /vehicles` | 60 | 1 min | Búsqueda general |
| `GET /vehicles/{slug}` | 120 | 1 min | Detalle frecuente |
| `GET /vehicles/search` | 30 | 1 min | Búsqueda costosa |
| `POST /vehicles` | 10 | 1 min | Creación |
| `PUT /vehicles/{id}` | 20 | 1 min | Actualización |
| `DELETE /vehicles/{id}` | 10 | 1 min | Eliminación |

#### 📷 Media Endpoints

| Endpoint | Límite | Período | Razón |
|----------|--------|---------|-------|
| `POST /media/upload` | 30 | 1 min | Upload costoso |
| `POST /media/upload/bulk` | 5 | 1 min | Bulk muy costoso |
| `GET /media/{id}` | 200 | 1 min | CDN debería cachear |

#### 💳 Billing Endpoints

| Endpoint | Límite | Período | Razón |
|----------|--------|---------|-------|
| `POST /billing/checkout` | 5 | 1 min | Prevenir fraude |
| `POST /billing/subscribe` | 3 | 1 min | Cambios de plan |
| `GET /billing/invoices` | 20 | 1 min | Historial |

#### 📧 Contact/Messaging

| Endpoint | Límite | Período | Razón |
|----------|--------|---------|-------|
| `POST /contact/inquiry` | 10 | 1 min | Prevenir spam |
| `POST /contact/message` | 30 | 1 min | Mensajes |
| `GET /contact/conversations` | 60 | 1 min | Lista |

---

## 📄 PAGINACIÓN

### Patrón Estándar: Offset-Based

```typescript
// Request
GET /api/vehicles?page=1&pageSize=20&sortBy=createdAt&sortOrder=desc

// Response
{
  "success": true,
  "data": [...],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 1543,
    "totalPages": 78,
    "hasNextPage": true,
    "hasPreviousPage": false
  }
}
```

### Patrón Alternativo: Cursor-Based (Infinite Scroll)

```typescript
// Request (primera página)
GET /api/vehicles/feed?limit=20

// Response
{
  "success": true,
  "data": [...],
  "pagination": {
    "nextCursor": "eyJpZCI6MTIzNH0=",
    "hasMore": true
  }
}

// Request (siguiente página)
GET /api/vehicles/feed?limit=20&cursor=eyJpZCI6MTIzNH0=
```

### Límites de Paginación por Endpoint

| Endpoint | Default | Máximo | Mínimo | Cursor Support |
|----------|---------|--------|--------|----------------|
| `GET /vehicles` | 20 | 100 | 1 | ✅ |
| `GET /vehicles/search` | 20 | 50 | 1 | ✅ |
| `GET /favorites` | 20 | 100 | 1 | ❌ |
| `GET /dealers` | 20 | 50 | 1 | ✅ |
| `GET /messages` | 30 | 100 | 1 | ✅ |
| `GET /notifications` | 20 | 50 | 1 | ✅ |
| `GET /admin/users` | 20 | 100 | 1 | ❌ |
| `GET /admin/logs` | 50 | 200 | 10 | ✅ |

### Parámetros de Ordenamiento

```typescript
// Parámetros comunes
interface PaginationParams {
  page?: number;        // Default: 1
  pageSize?: number;    // Default: 20, Max: 100
  sortBy?: string;      // Campo de ordenamiento
  sortOrder?: 'asc' | 'desc';  // Default: 'desc'
}

// Campos ordenables por endpoint
const SORTABLE_FIELDS = {
  vehicles: ['createdAt', 'price', 'year', 'mileage', 'views'],
  dealers: ['createdAt', 'rating', 'totalVehicles', 'name'],
  users: ['createdAt', 'lastLoginAt', 'email'],
  messages: ['createdAt', 'unreadCount'],
};
```

---

## 🔧 IMPLEMENTACIÓN FRONTEND

### Hook de Paginación Offset-Based

```typescript
// filepath: src/hooks/usePaginatedQuery.ts
import { useQuery, keepPreviousData } from '@tanstack/react-query';
import { useState } from 'react';

interface PaginationState {
  page: number;
  pageSize: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}

interface PaginatedResponse<T> {
  data: T[];
  pagination: {
    page: number;
    pageSize: number;
    totalItems: number;
    totalPages: number;
    hasNextPage: boolean;
    hasPreviousPage: boolean;
  };
}

export function usePaginatedQuery<T>(
  queryKey: string[],
  fetcher: (params: PaginationState) => Promise<PaginatedResponse<T>>,
  initialState: Partial<PaginationState> = {}
) {
  const [pagination, setPagination] = useState<PaginationState>({
    page: 1,
    pageSize: 20,
    sortOrder: 'desc',
    ...initialState,
  });

  const query = useQuery({
    queryKey: [...queryKey, pagination],
    queryFn: () => fetcher(pagination),
    placeholderData: keepPreviousData,
    staleTime: 30 * 1000, // 30 segundos
  });

  const goToPage = (page: number) => {
    setPagination((prev) => ({ ...prev, page }));
  };

  const nextPage = () => {
    if (query.data?.pagination.hasNextPage) {
      goToPage(pagination.page + 1);
    }
  };

  const prevPage = () => {
    if (query.data?.pagination.hasPreviousPage) {
      goToPage(pagination.page - 1);
    }
  };

  const setPageSize = (pageSize: number) => {
    setPagination((prev) => ({ ...prev, pageSize, page: 1 }));
  };

  const setSort = (sortBy: string, sortOrder: 'asc' | 'desc' = 'desc') => {
    setPagination((prev) => ({ ...prev, sortBy, sortOrder, page: 1 }));
  };

  return {
    ...query,
    pagination: query.data?.pagination,
    currentPage: pagination.page,
    goToPage,
    nextPage,
    prevPage,
    setPageSize,
    setSort,
  };
}
```

### Hook de Infinite Scroll (Cursor-Based)

```typescript
// filepath: src/hooks/useInfiniteVehicles.ts
import { useInfiniteQuery } from '@tanstack/react-query';
import { vehicleApi } from '@/lib/api/vehicles';

interface UseInfiniteVehiclesOptions {
  filters?: Record<string, unknown>;
  limit?: number;
}

export function useInfiniteVehicles(options: UseInfiniteVehiclesOptions = {}) {
  const { filters = {}, limit = 20 } = options;

  return useInfiniteQuery({
    queryKey: ['vehicles', 'infinite', filters],
    queryFn: async ({ pageParam }) => {
      const response = await vehicleApi.getFeed({
        ...filters,
        limit,
        cursor: pageParam,
      });
      return response.data;
    },
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => {
      return lastPage.pagination.hasMore 
        ? lastPage.pagination.nextCursor 
        : undefined;
    },
    staleTime: 60 * 1000, // 1 minuto
  });
}

// Uso en componente
function VehicleFeed() {
  const {
    data,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
  } = useInfiniteVehicles({ filters: { make: 'Toyota' } });

  // Flatten pages
  const vehicles = data?.pages.flatMap((page) => page.data) ?? [];

  return (
    <div>
      {vehicles.map((vehicle) => (
        <VehicleCard key={vehicle.id} vehicle={vehicle} />
      ))}
      
      {hasNextPage && (
        <button
          onClick={() => fetchNextPage()}
          disabled={isFetchingNextPage}
        >
          {isFetchingNextPage ? 'Cargando...' : 'Cargar más'}
        </button>
      )}
    </div>
  );
}
```

### Intersection Observer para Auto-Load

```typescript
// filepath: src/hooks/useIntersectionObserver.ts
import { useEffect, useRef, useCallback } from 'react';

export function useIntersectionObserver(
  callback: () => void,
  options: IntersectionObserverInit = {}
) {
  const targetRef = useRef<HTMLDivElement>(null);

  const handleIntersect = useCallback(
    (entries: IntersectionObserverEntry[]) => {
      if (entries[0]?.isIntersecting) {
        callback();
      }
    },
    [callback]
  );

  useEffect(() => {
    const target = targetRef.current;
    if (!target) return;

    const observer = new IntersectionObserver(handleIntersect, {
      rootMargin: '100px',
      threshold: 0.1,
      ...options,
    });

    observer.observe(target);
    return () => observer.disconnect();
  }, [handleIntersect, options]);

  return targetRef;
}

// Uso
function VehicleFeed() {
  const { fetchNextPage, hasNextPage, isFetchingNextPage } = useInfiniteVehicles();
  
  const loadMoreRef = useIntersectionObserver(() => {
    if (hasNextPage && !isFetchingNextPage) {
      fetchNextPage();
    }
  });

  return (
    <div>
      {/* vehicles */}
      <div ref={loadMoreRef} className="h-10" />
      {isFetchingNextPage && <Spinner />}
    </div>
  );
}
```

---

## 🚨 MANEJO DE RATE LIMIT (429)

### Interceptor con Retry Automático

```typescript
// filepath: src/lib/api/rateLimitHandler.ts
import { AxiosError, AxiosInstance } from 'axios';
import { toast } from 'sonner';

export function setupRateLimitHandler(api: AxiosInstance) {
  api.interceptors.response.use(
    (response) => response,
    async (error: AxiosError) => {
      if (error.response?.status === 429) {
        const retryAfter = getRetryAfter(error);
        
        // Mostrar toast con countdown
        showRateLimitToast(retryAfter);
        
        // Esperar y reintentar automáticamente
        if (retryAfter <= 60) {
          await sleep(retryAfter * 1000);
          return api.request(error.config!);
        }
      }
      
      return Promise.reject(error);
    }
  );
}

function getRetryAfter(error: AxiosError): number {
  const retryAfter = error.response?.headers['retry-after'];
  if (retryAfter) {
    return parseInt(retryAfter, 10);
  }
  
  // Default: 60 segundos
  return 60;
}

function showRateLimitToast(seconds: number) {
  toast.error(
    `Demasiadas solicitudes. Intenta en ${seconds} segundos.`,
    {
      duration: seconds * 1000,
      id: 'rate-limit', // Evitar duplicados
    }
  );
}

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}
```

### Componente de Rate Limit Warning

```typescript
// filepath: src/components/RateLimitWarning.tsx
'use client';

import { useEffect, useState } from 'react';
import { AlertTriangle } from 'lucide-react';
import { Alert, AlertDescription } from '@/components/ui/alert';

interface RateLimitWarningProps {
  remaining: number;
  limit: number;
  resetTime: number;
}

export function RateLimitWarning({ 
  remaining, 
  limit, 
  resetTime 
}: RateLimitWarningProps) {
  const [secondsUntilReset, setSecondsUntilReset] = useState(0);
  const percentage = (remaining / limit) * 100;
  
  useEffect(() => {
    const updateCountdown = () => {
      const now = Math.floor(Date.now() / 1000);
      setSecondsUntilReset(Math.max(0, resetTime - now));
    };
    
    updateCountdown();
    const interval = setInterval(updateCountdown, 1000);
    return () => clearInterval(interval);
  }, [resetTime]);

  // Solo mostrar si queda menos del 20%
  if (percentage > 20) return null;

  return (
    <Alert variant="warning" className="mb-4">
      <AlertTriangle className="h-4 w-4" />
      <AlertDescription>
        Te quedan {remaining} de {limit} solicitudes. 
        Se reinicia en {secondsUntilReset} segundos.
      </AlertDescription>
    </Alert>
  );
}
```

### Hook para Tracking de Rate Limit

```typescript
// filepath: src/hooks/useRateLimit.ts
import { create } from 'zustand';

interface RateLimitState {
  limits: Record<string, {
    remaining: number;
    limit: number;
    resetTime: number;
  }>;
  updateLimit: (endpoint: string, headers: Headers) => void;
  isNearLimit: (endpoint: string) => boolean;
  getRemainingPercent: (endpoint: string) => number;
}

export const useRateLimitStore = create<RateLimitState>((set, get) => ({
  limits: {},
  
  updateLimit: (endpoint, headers) => {
    const limit = parseInt(headers.get('X-RateLimit-Limit') || '100', 10);
    const remaining = parseInt(headers.get('X-RateLimit-Remaining') || '100', 10);
    const resetTime = parseInt(headers.get('X-RateLimit-Reset') || '0', 10);
    
    set((state) => ({
      limits: {
        ...state.limits,
        [endpoint]: { limit, remaining, resetTime },
      },
    }));
  },
  
  isNearLimit: (endpoint) => {
    const limit = get().limits[endpoint];
    if (!limit) return false;
    return (limit.remaining / limit.limit) < 0.2;
  },
  
  getRemainingPercent: (endpoint) => {
    const limit = get().limits[endpoint];
    if (!limit) return 100;
    return (limit.remaining / limit.limit) * 100;
  },
}));

// Uso en API client
api.interceptors.response.use((response) => {
  const endpoint = new URL(response.config.url!, response.config.baseURL).pathname;
  useRateLimitStore.getState().updateLimit(endpoint, response.headers);
  return response;
});
```

---

## 🧩 COMPONENTE DE PAGINACIÓN UI

```typescript
// filepath: src/components/Pagination.tsx
'use client';

import { Button } from '@/components/ui/button';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from 'lucide-react';

interface PaginationProps {
  currentPage: number;
  totalPages: number;
  pageSize: number;
  totalItems: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (size: number) => void;
  pageSizeOptions?: number[];
}

export function Pagination({
  currentPage,
  totalPages,
  pageSize,
  totalItems,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = [10, 20, 50, 100],
}: PaginationProps) {
  const startItem = (currentPage - 1) * pageSize + 1;
  const endItem = Math.min(currentPage * pageSize, totalItems);

  // Generar números de página visibles
  const getPageNumbers = () => {
    const pages: (number | 'ellipsis')[] = [];
    const showPages = 5;
    
    if (totalPages <= showPages + 2) {
      return Array.from({ length: totalPages }, (_, i) => i + 1);
    }

    pages.push(1);
    
    if (currentPage > 3) {
      pages.push('ellipsis');
    }
    
    const start = Math.max(2, currentPage - 1);
    const end = Math.min(totalPages - 1, currentPage + 1);
    
    for (let i = start; i <= end; i++) {
      pages.push(i);
    }
    
    if (currentPage < totalPages - 2) {
      pages.push('ellipsis');
    }
    
    pages.push(totalPages);
    
    return pages;
  };

  return (
    <div className="flex flex-col sm:flex-row items-center justify-between gap-4 py-4">
      {/* Info */}
      <div className="text-sm text-muted-foreground">
        Mostrando {startItem} - {endItem} de {totalItems} resultados
      </div>

      {/* Controles */}
      <div className="flex items-center gap-2">
        {/* Page Size Selector */}
        <Select
          value={String(pageSize)}
          onValueChange={(value) => onPageSizeChange(Number(value))}
        >
          <SelectTrigger className="w-[120px]">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {pageSizeOptions.map((size) => (
              <SelectItem key={size} value={String(size)}>
                {size} por página
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        {/* Navigation */}
        <div className="flex items-center gap-1">
          <Button
            variant="outline"
            size="icon"
            onClick={() => onPageChange(1)}
            disabled={currentPage === 1}
          >
            <ChevronsLeft className="h-4 w-4" />
          </Button>
          
          <Button
            variant="outline"
            size="icon"
            onClick={() => onPageChange(currentPage - 1)}
            disabled={currentPage === 1}
          >
            <ChevronLeft className="h-4 w-4" />
          </Button>

          {getPageNumbers().map((page, idx) =>
            page === 'ellipsis' ? (
              <span key={`ellipsis-${idx}`} className="px-2">...</span>
            ) : (
              <Button
                key={page}
                variant={currentPage === page ? 'default' : 'outline'}
                size="icon"
                onClick={() => onPageChange(page)}
              >
                {page}
              </Button>
            )
          )}

          <Button
            variant="outline"
            size="icon"
            onClick={() => onPageChange(currentPage + 1)}
            disabled={currentPage === totalPages}
          >
            <ChevronRight className="h-4 w-4" />
          </Button>
          
          <Button
            variant="outline"
            size="icon"
            onClick={() => onPageChange(totalPages)}
            disabled={currentPage === totalPages}
          >
            <ChevronsRight className="h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  );
}
```

---

## 🧪 TESTS E2E

```typescript
// filepath: e2e/pagination.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Pagination', () => {
  test('navega entre páginas correctamente', async ({ page }) => {
    await page.goto('/vehicles');
    
    // Verificar primera página
    await expect(page.locator('[data-page="1"]')).toHaveAttribute('aria-current', 'page');
    
    // Ir a página 2
    await page.click('[data-page="2"]');
    await expect(page).toHaveURL(/page=2/);
    await expect(page.locator('[data-page="2"]')).toHaveAttribute('aria-current', 'page');
    
    // Verificar que se actualizaron los resultados
    await expect(page.locator('[data-testid="vehicle-card"]')).toHaveCount(20);
  });

  test('cambia el tamaño de página', async ({ page }) => {
    await page.goto('/vehicles');
    
    // Cambiar a 50 por página
    await page.click('[data-testid="page-size-select"]');
    await page.click('[data-value="50"]');
    
    await expect(page).toHaveURL(/pageSize=50/);
    await expect(page.locator('[data-testid="vehicle-card"]')).toHaveCount(50);
  });

  test('infinite scroll carga más resultados', async ({ page }) => {
    await page.goto('/vehicles?view=feed');
    
    // Contar items iniciales
    const initialCount = await page.locator('[data-testid="vehicle-card"]').count();
    
    // Scroll al final
    await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
    
    // Esperar carga
    await page.waitForResponse(/cursor=/);
    
    // Verificar más items
    const newCount = await page.locator('[data-testid="vehicle-card"]').count();
    expect(newCount).toBeGreaterThan(initialCount);
  });
});

test.describe('Rate Limiting', () => {
  test('muestra mensaje en 429', async ({ page }) => {
    // Mock 429 response
    await page.route('**/api/vehicles**', (route) => {
      route.fulfill({
        status: 429,
        headers: {
          'Retry-After': '30',
          'X-RateLimit-Remaining': '0',
          'X-RateLimit-Limit': '100',
        },
        body: JSON.stringify({
          status: 429,
          errorCode: 'RATE_LIMIT_EXCEEDED',
          detail: 'Too many requests',
        }),
      });
    });

    await page.goto('/vehicles');
    
    await expect(page.locator('.toast-error')).toContainText(
      'Demasiadas solicitudes'
    );
  });

  test('muestra warning cuando queda poco límite', async ({ page }) => {
    await page.route('**/api/vehicles**', (route) => {
      route.fulfill({
        status: 200,
        headers: {
          'X-RateLimit-Remaining': '5',
          'X-RateLimit-Limit': '100',
          'X-RateLimit-Reset': String(Math.floor(Date.now() / 1000) + 60),
        },
        body: JSON.stringify({ success: true, data: [] }),
      });
    });

    await page.goto('/vehicles');
    
    await expect(page.locator('[data-testid="rate-limit-warning"]')).toBeVisible();
  });
});
```

---

## 📋 CHECKLIST DE IMPLEMENTACIÓN

- [ ] Types de paginación definidos en `src/types/api.ts`
- [ ] Hook `usePaginatedQuery` implementado
- [ ] Hook `useInfiniteQuery` para infinite scroll
- [ ] Intersection Observer para auto-load
- [ ] Interceptor de rate limit con retry
- [ ] Componente `<Pagination />` reutilizable
- [ ] Componente `<RateLimitWarning />` 
- [ ] Store de rate limit (Zustand)
- [ ] Tests E2E para paginación
- [ ] Tests E2E para rate limiting

---

## 🔗 REFERENCIAS

- [TanStack Query Pagination](https://tanstack.com/query/latest/docs/react/guides/paginated-queries)
- [TanStack Query Infinite Queries](https://tanstack.com/query/latest/docs/react/guides/infinite-queries)
- [Rate Limiting Best Practices](https://cloud.google.com/architecture/rate-limiting-strategies-techniques)

---

_Última actualización: Enero 31, 2026_
