# 🔔 AlertService - Documentación Frontend

> **Servicio:** AlertService  
> **Puerto:** 5067 (dev) / 8080 (k8s)  
> **Estado:** ✅ Implementado  
> **Base de datos:** PostgreSQL  
> **Última actualización:** Enero 2026

---

## 📋 Descripción

Servicio de alertas y búsquedas guardadas para usuarios del marketplace. Permite crear **alertas de precio** para vehículos específicos y **guardar búsquedas** para recibir notificaciones cuando haya nuevos resultados.

---

## 🎯 Casos de Uso Frontend

### 1. Crear Alerta de Precio

```typescript
// Desde página de detalle de vehículo
const CreatePriceAlertButton = ({ vehicle }: { vehicle: Vehicle }) => {
  const createAlert = useCreatePriceAlert();

  const handleCreate = async () => {
    const suggestedPrice = vehicle.price * 0.9; // 10% menos

    await createAlert.mutateAsync({
      vehicleId: vehicle.id,
      targetPrice: suggestedPrice,
      condition: 'LessThanOrEqual'
    });

    toast.success(`Te avisaremos cuando baje a ${formatPrice(suggestedPrice)}`);
  };

  return (
    <Button onClick={handleCreate}>
      🔔 Alertarme cuando baje de precio
    </Button>
  );
};
```

### 2. Guardar Búsqueda Actual

```typescript
// Guardar criterios de búsqueda actuales
const SaveSearchButton = ({ currentFilters }: { currentFilters: SearchFilters }) => {
  const saveSearch = useSaveSearch();
  const [name, setName] = useState('');

  const handleSave = async () => {
    await saveSearch.mutateAsync({
      name: name || 'Mi búsqueda',
      criteria: currentFilters,
      notificationFrequency: 'Daily'
    });

    toast.success('Búsqueda guardada. Recibirás alertas de nuevos vehículos.');
  };

  return (
    <Dialog>
      <DialogTrigger>
        <Button variant="outline">💾 Guardar búsqueda</Button>
      </DialogTrigger>
      <DialogContent>
        <Input
          placeholder="Nombre de la búsqueda"
          value={name}
          onChange={e => setName(e.target.value)}
        />
        <DialogFooter>
          <Button onClick={handleSave}>Guardar</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};
```

### 3. Gestionar Alertas

```typescript
// Página de gestión de alertas
const MyAlertsPage = () => {
  const { data: priceAlerts } = useMyPriceAlerts();
  const { data: savedSearches } = useMySavedSearches();

  return (
    <Tabs defaultValue="price-alerts">
      <TabsList>
        <TabsTrigger value="price-alerts">
          Alertas de Precio ({priceAlerts?.length || 0})
        </TabsTrigger>
        <TabsTrigger value="saved-searches">
          Búsquedas Guardadas ({savedSearches?.length || 0})
        </TabsTrigger>
      </TabsList>

      <TabsContent value="price-alerts">
        <PriceAlertsList alerts={priceAlerts} />
      </TabsContent>

      <TabsContent value="saved-searches">
        <SavedSearchesList searches={savedSearches} />
      </TabsContent>
    </Tabs>
  );
};
```

---

## 📡 API Endpoints

### Price Alerts (JWT Required)

| Método   | Endpoint                             | Descripción                |
| -------- | ------------------------------------ | -------------------------- |
| `GET`    | `/api/pricealerts`                   | Listar mis alertas         |
| `GET`    | `/api/pricealerts/{id}`              | Obtener alerta específica  |
| `POST`   | `/api/pricealerts`                   | Crear nueva alerta         |
| `PUT`    | `/api/pricealerts/{id}/target-price` | Actualizar precio objetivo |
| `POST`   | `/api/pricealerts/{id}/activate`     | Activar alerta             |
| `POST`   | `/api/pricealerts/{id}/deactivate`   | Desactivar alerta          |
| `POST`   | `/api/pricealerts/{id}/reset`        | Resetear alerta disparada  |
| `DELETE` | `/api/pricealerts/{id}`              | Eliminar alerta            |

### Saved Searches (JWT Required)

| Método   | Endpoint                                | Descripción                 |
| -------- | --------------------------------------- | --------------------------- |
| `GET`    | `/api/savedsearches`                    | Listar mis búsquedas        |
| `GET`    | `/api/savedsearches/{id}`               | Obtener búsqueda específica |
| `POST`   | `/api/savedsearches`                    | Crear nueva búsqueda        |
| `PUT`    | `/api/savedsearches/{id}/name`          | Renombrar búsqueda          |
| `PUT`    | `/api/savedsearches/{id}/criteria`      | Actualizar criterios        |
| `PUT`    | `/api/savedsearches/{id}/notifications` | Configurar notificaciones   |
| `POST`   | `/api/savedsearches/{id}/activate`      | Activar búsqueda            |
| `POST`   | `/api/savedsearches/{id}/deactivate`    | Desactivar búsqueda         |
| `DELETE` | `/api/savedsearches/{id}`               | Eliminar búsqueda           |

---

## 🔧 Cliente TypeScript

```typescript
// services/alertService.ts

import { apiClient } from "./apiClient";

// Enums
export type AlertCondition = "LessThanOrEqual" | "GreaterThanOrEqual";
export type NotificationFrequency = "Instant" | "Daily" | "Weekly";

// Tipos
interface PriceAlert {
  id: string;
  userId: string;
  vehicleId: string;
  vehicle?: {
    id: string;
    title: string;
    make: string;
    model: string;
    year: number;
    currentPrice: number;
    imageUrl: string;
  };
  targetPrice: number;
  condition: AlertCondition;
  isActive: boolean;
  isTriggered: boolean;
  triggeredAt?: string;
  createdAt: string;
}

interface CreatePriceAlertRequest {
  vehicleId: string;
  targetPrice: number;
  condition: AlertCondition;
}

interface SavedSearch {
  id: string;
  userId: string;
  name: string;
  criteria: SearchCriteria;
  isActive: boolean;
  notificationFrequency: NotificationFrequency;
  lastNotifiedAt?: string;
  newResultsCount?: number;
  createdAt: string;
  updatedAt: string;
}

interface SearchCriteria {
  makes?: string[];
  models?: string[];
  yearRange?: { min: number; max: number };
  priceRange?: { min: number; max: number };
  mileageRange?: { min: number; max: number };
  bodyTypes?: string[];
  fuelTypes?: string[];
  transmissions?: string[];
  location?: string;
}

interface CreateSavedSearchRequest {
  name: string;
  criteria: SearchCriteria;
  notificationFrequency?: NotificationFrequency;
}

export const alertService = {
  // === PRICE ALERTS ===

  async getMyPriceAlerts(): Promise<PriceAlert[]> {
    const response = await apiClient.get("/api/pricealerts");
    return response.data;
  },

  async getPriceAlert(id: string): Promise<PriceAlert> {
    const response = await apiClient.get(`/api/pricealerts/${id}`);
    return response.data;
  },

  async createPriceAlert(
    request: CreatePriceAlertRequest,
  ): Promise<PriceAlert> {
    const response = await apiClient.post("/api/pricealerts", request);
    return response.data;
  },

  async updateTargetPrice(
    id: string,
    targetPrice: number,
  ): Promise<PriceAlert> {
    const response = await apiClient.put(
      `/api/pricealerts/${id}/target-price`,
      { targetPrice },
    );
    return response.data;
  },

  async activatePriceAlert(id: string): Promise<void> {
    await apiClient.post(`/api/pricealerts/${id}/activate`);
  },

  async deactivatePriceAlert(id: string): Promise<void> {
    await apiClient.post(`/api/pricealerts/${id}/deactivate`);
  },

  async resetPriceAlert(id: string): Promise<void> {
    await apiClient.post(`/api/pricealerts/${id}/reset`);
  },

  async deletePriceAlert(id: string): Promise<void> {
    await apiClient.delete(`/api/pricealerts/${id}`);
  },

  // === SAVED SEARCHES ===

  async getMySavedSearches(): Promise<SavedSearch[]> {
    const response = await apiClient.get("/api/savedsearches");
    return response.data;
  },

  async getSavedSearch(id: string): Promise<SavedSearch> {
    const response = await apiClient.get(`/api/savedsearches/${id}`);
    return response.data;
  },

  async createSavedSearch(
    request: CreateSavedSearchRequest,
  ): Promise<SavedSearch> {
    const response = await apiClient.post("/api/savedsearches", request);
    return response.data;
  },

  async renameSavedSearch(id: string, name: string): Promise<SavedSearch> {
    const response = await apiClient.put(`/api/savedsearches/${id}/name`, {
      name,
    });
    return response.data;
  },

  async updateSearchCriteria(
    id: string,
    criteria: SearchCriteria,
  ): Promise<SavedSearch> {
    const response = await apiClient.put(`/api/savedsearches/${id}/criteria`, {
      criteria,
    });
    return response.data;
  },

  async updateNotificationSettings(
    id: string,
    frequency: NotificationFrequency,
  ): Promise<SavedSearch> {
    const response = await apiClient.put(
      `/api/savedsearches/${id}/notifications`,
      {
        notificationFrequency: frequency,
      },
    );
    return response.data;
  },

  async activateSavedSearch(id: string): Promise<void> {
    await apiClient.post(`/api/savedsearches/${id}/activate`);
  },

  async deactivateSavedSearch(id: string): Promise<void> {
    await apiClient.post(`/api/savedsearches/${id}/deactivate`);
  },

  async deleteSavedSearch(id: string): Promise<void> {
    await apiClient.delete(`/api/savedsearches/${id}`);
  },

  // === HELPERS ===

  formatCondition(condition: AlertCondition): string {
    return condition === "LessThanOrEqual"
      ? "menor o igual a"
      : "mayor o igual a";
  },

  getFrequencyLabel(frequency: NotificationFrequency): string {
    const labels: Record<NotificationFrequency, string> = {
      Instant: "Inmediato",
      Daily: "Diario",
      Weekly: "Semanal",
    };
    return labels[frequency];
  },
};
```

---

## 🪝 Hooks de React

```typescript
// hooks/useAlerts.ts

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  alertService,
  CreatePriceAlertRequest,
  CreateSavedSearchRequest,
} from "../services/alertService";

// === PRICE ALERTS ===

export function useMyPriceAlerts() {
  return useQuery({
    queryKey: ["my-price-alerts"],
    queryFn: () => alertService.getMyPriceAlerts(),
  });
}

export function useCreatePriceAlert() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreatePriceAlertRequest) =>
      alertService.createPriceAlert(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["my-price-alerts"] });
    },
  });
}

export function useTogglePriceAlert() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      isActive
        ? alertService.deactivatePriceAlert(id)
        : alertService.activatePriceAlert(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["my-price-alerts"] });
    },
  });
}

export function useDeletePriceAlert() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => alertService.deletePriceAlert(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["my-price-alerts"] });
    },
  });
}

// === SAVED SEARCHES ===

export function useMySavedSearches() {
  return useQuery({
    queryKey: ["my-saved-searches"],
    queryFn: () => alertService.getMySavedSearches(),
  });
}

export function useSaveSearch() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateSavedSearchRequest) =>
      alertService.createSavedSearch(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["my-saved-searches"] });
    },
  });
}

export function useDeleteSavedSearch() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => alertService.deleteSavedSearch(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["my-saved-searches"] });
    },
  });
}

// Hook para verificar si ya existe alerta para un vehículo
export function useHasPriceAlert(vehicleId: string) {
  const { data: alerts } = useMyPriceAlerts();
  return alerts?.some((a) => a.vehicleId === vehicleId) ?? false;
}
```

---

## 🧩 Componentes de Ejemplo

### Price Alert Card

```tsx
// components/PriceAlertCard.tsx

import { formatPrice, formatDate } from "../utils/format";
import { alertService, PriceAlert } from "../services/alertService";
import { useTogglePriceAlert, useDeletePriceAlert } from "../hooks/useAlerts";

interface PriceAlertCardProps {
  alert: PriceAlert;
}

export function PriceAlertCard({ alert }: PriceAlertCardProps) {
  const toggleAlert = useTogglePriceAlert();
  const deleteAlert = useDeletePriceAlert();

  const priceChange = alert.vehicle
    ? (
        ((alert.vehicle.currentPrice - alert.targetPrice) / alert.targetPrice) *
        100
      ).toFixed(1)
    : null;

  return (
    <Card
      className={`${alert.isTriggered ? "border-green-500 bg-green-50" : ""}`}
    >
      <CardContent className="p-4">
        <div className="flex gap-4">
          {/* Imagen del vehículo */}
          <img
            src={alert.vehicle?.imageUrl || "/placeholder-car.png"}
            alt={alert.vehicle?.title}
            className="w-24 h-18 object-cover rounded"
          />

          <div className="flex-1">
            <h3 className="font-medium">
              {alert.vehicle?.title || "Vehículo no disponible"}
            </h3>

            <div className="flex items-center gap-2 mt-1">
              <span className="text-gray-500">
                Alertar cuando precio sea{" "}
                {alertService.formatCondition(alert.condition)}
              </span>
              <span className="font-bold text-green-600">
                {formatPrice(alert.targetPrice)}
              </span>
            </div>

            {alert.vehicle && (
              <p className="text-sm text-gray-500 mt-1">
                Precio actual: {formatPrice(alert.vehicle.currentPrice)}
                {priceChange && (
                  <span
                    className={
                      Number(priceChange) > 0
                        ? "text-red-500"
                        : "text-green-500"
                    }
                  >
                    {" "}
                    ({priceChange}% diferencia)
                  </span>
                )}
              </p>
            )}

            {alert.isTriggered && (
              <Badge variant="success" className="mt-2">
                ✅ Alerta disparada {formatDate(alert.triggeredAt!)}
              </Badge>
            )}
          </div>

          <div className="flex flex-col gap-2">
            <Switch
              checked={alert.isActive}
              onCheckedChange={() =>
                toggleAlert.mutate({
                  id: alert.id,
                  isActive: alert.isActive,
                })
              }
            />
            <Button
              variant="ghost"
              size="sm"
              onClick={() => deleteAlert.mutate(alert.id)}
            >
              🗑️
            </Button>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
```

### Saved Search Card

```tsx
// components/SavedSearchCard.tsx

import { SavedSearch } from "../services/alertService";
import { useDeleteSavedSearch } from "../hooks/useAlerts";
import { Link } from "react-router-dom";
import { searchService } from "../services/searchService";

interface SavedSearchCardProps {
  search: SavedSearch;
}

export function SavedSearchCard({ search }: SavedSearchCardProps) {
  const deleteSearch = useDeleteSavedSearch();

  // Construir URL de búsqueda
  const searchUrl = `/search?${searchService.buildQueryString(search.criteria)}`;

  // Describir criterios
  const criteriaDescription = useMemo(() => {
    const parts: string[] = [];
    if (search.criteria.makes?.length)
      parts.push(search.criteria.makes.join(", "));
    if (search.criteria.priceRange) {
      parts.push(
        `${formatPrice(search.criteria.priceRange.min)} - ${formatPrice(search.criteria.priceRange.max)}`,
      );
    }
    if (search.criteria.yearRange) {
      parts.push(
        `${search.criteria.yearRange.min}-${search.criteria.yearRange.max}`,
      );
    }
    return parts.join(" • ");
  }, [search.criteria]);

  return (
    <Card>
      <CardContent className="p-4">
        <div className="flex justify-between items-start">
          <div>
            <h3 className="font-medium">{search.name}</h3>
            <p className="text-sm text-gray-500 mt-1">{criteriaDescription}</p>

            <div className="flex items-center gap-4 mt-2 text-sm">
              <span>
                📧{" "}
                {alertService.getFrequencyLabel(search.notificationFrequency)}
              </span>
              {search.newResultsCount && search.newResultsCount > 0 && (
                <Badge variant="secondary">
                  {search.newResultsCount} nuevos
                </Badge>
              )}
            </div>
          </div>

          <div className="flex gap-2">
            <Link to={searchUrl}>
              <Button variant="outline" size="sm">
                Ver resultados
              </Button>
            </Link>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => deleteSearch.mutate(search.id)}
            >
              🗑️
            </Button>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
```

---

## 🧪 Testing

### E2E Test (Playwright)

```typescript
// e2e/alerts.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Price Alerts", () => {
  test("should create price alert from vehicle page", async ({ page }) => {
    await loginAsUser(page);
    await page.goto("/vehicles/toyota-camry-2024");

    await page.click('[data-testid="create-alert-button"]');

    await expect(
      page.locator('[data-testid="alert-created-toast"]'),
    ).toBeVisible();
  });

  test("should manage alerts in my alerts page", async ({ page }) => {
    await loginAsUser(page);
    await page.goto("/my-alerts");

    // Toggle alert
    await page.click('[data-testid="alert-toggle"]');
    await expect(
      page.locator('[data-testid="alert-disabled-indicator"]'),
    ).toBeVisible();

    // Delete alert
    await page.click('[data-testid="delete-alert"]');
    await expect(page.locator('[data-testid="alert-card"]')).toHaveCount(0);
  });
});

test.describe("Saved Searches", () => {
  test("should save current search", async ({ page }) => {
    await loginAsUser(page);
    await page.goto("/search?makes=Toyota&priceMax=2000000");

    await page.click('[data-testid="save-search-button"]');
    await page.fill('[data-testid="search-name-input"]', "Toyotas baratos");
    await page.click('[data-testid="confirm-save"]');

    await expect(
      page.locator('[data-testid="search-saved-toast"]'),
    ).toBeVisible();
  });
});
```

---

## 📊 Frecuencias de Notificación

| Frecuencia  | Descripción             | Uso recomendado                |
| ----------- | ----------------------- | ------------------------------ |
| **Instant** | Inmediatamente          | Vehículos específicos, urgente |
| **Daily**   | Resumen diario 9 AM     | Búsquedas generales            |
| **Weekly**  | Resumen semanal (Lunes) | Búsquedas amplias              |

---

## 🔗 Referencias

- [AlertService README](../../../backend/AlertService/README.md)
- [NotificationService](../05-API-INTEGRATION/14-notifications-api.md)
- [SearchService](./11-searchservice.md)

---

_Las alertas son clave para retención de usuarios. Enviar cuando sea relevante._
