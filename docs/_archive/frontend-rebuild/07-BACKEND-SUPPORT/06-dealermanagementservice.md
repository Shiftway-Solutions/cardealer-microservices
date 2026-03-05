# 🏪 DealerManagementService - Documentación Frontend

> **Servicio:** DealerManagementService  
> **Puerto:** 5098 (dev) / 8080 (k8s)  
> **Estado:** ✅ Implementado  
> **Última actualización:** Enero 2026

---

## 📋 Descripción

Servicio para gestión completa de cuentas de dealer: registro, verificación, suscripciones, sucursales y métricas. Es el servicio principal para todo el módulo de dealers en OKLA.

---

## 🎯 Casos de Uso Frontend

### 1. Registro de Dealer

```typescript
// Flujo de registro de nuevo dealer
const registerDealer = async (formData: DealerRegistrationForm) => {
  const dealer = await dealerService.createDealer({
    businessName: formData.businessName,
    rnc: formData.rnc,
    legalName: formData.legalName,
    dealerType: formData.dealerType,
    email: formData.email,
    phone: formData.phone,
    address: formData.address,
    city: formData.city,
    province: formData.province,
  });

  // Redirigir a suscripción
  navigate(
    `/dealer/subscribe?dealerId=${dealer.id}&plan=${formData.selectedPlan}`,
  );
};
```

### 2. Dashboard de Dealer

```typescript
// Obtener datos para dashboard
const loadDealerDashboard = async () => {
  const dealer = await dealerService.getDealerByUserId(currentUser.id);
  const locations = await dealerService.getLocations(dealer.id);
  const subscription = await dealerService.getSubscription(dealer.id);

  return {
    dealer,
    locations,
    subscription,
    canAddMore: dealer.activeListings < subscription.maxListings,
  };
};
```

### 3. Admin: Verificación de Dealers

```typescript
// Lista de dealers pendientes de verificación
const getPendingDealers = async () => {
  const dealers = await dealerService.getDealers({
    status: "Pending",
    verificationStatus: "DocumentsUploaded",
  });
  return dealers;
};

// Aprobar/Rechazar dealer
const verifyDealer = async (
  dealerId: string,
  approved: boolean,
  reason?: string,
) => {
  await dealerService.verifyDealer(dealerId, {
    approved,
    reason,
    verifiedBy: currentAdmin.id,
  });
};
```

---

## 📡 API Endpoints

### Dealers

| Método | Endpoint                     | Descripción                |
| ------ | ---------------------------- | -------------------------- |
| `GET`  | `/api/dealers`               | Listar dealers (paginado)  |
| `GET`  | `/api/dealers/{id}`          | Obtener dealer por ID      |
| `GET`  | `/api/dealers/user/{userId}` | Obtener dealer por User ID |
| `POST` | `/api/dealers`               | Crear nuevo dealer         |
| `PUT`  | `/api/dealers/{id}`          | Actualizar dealer          |
| `POST` | `/api/dealers/{id}/verify`   | Verificar dealer (Admin)   |

### Locations (Sucursales)

| Método   | Endpoint                           | Descripción         |
| -------- | ---------------------------------- | ------------------- |
| `GET`    | `/api/locations/dealer/{dealerId}` | Listar sucursales   |
| `GET`    | `/api/locations/{id}`              | Obtener sucursal    |
| `POST`   | `/api/locations`                   | Crear sucursal      |
| `PUT`    | `/api/locations/{id}`              | Actualizar sucursal |
| `DELETE` | `/api/locations/{id}`              | Eliminar sucursal   |

### Subscriptions

| Método | Endpoint                               | Descripción          |
| ------ | -------------------------------------- | -------------------- |
| `GET`  | `/api/subscriptions/dealer/{dealerId}` | Suscripción activa   |
| `POST` | `/api/subscriptions`                   | Crear suscripción    |
| `PUT`  | `/api/subscriptions/{id}/upgrade`      | Upgrade de plan      |
| `POST` | `/api/subscriptions/{id}/cancel`       | Cancelar suscripción |

---

## 🔧 Cliente TypeScript

```typescript
// services/dealerService.ts

import { apiClient } from "./apiClient";

// Enums
export type DealerType =
  | "Independent"
  | "Chain"
  | "MultipleStore"
  | "Franchise";
export type DealerStatus =
  | "Pending"
  | "UnderReview"
  | "Active"
  | "Suspended"
  | "Rejected"
  | "Inactive";
export type VerificationStatus =
  | "NotVerified"
  | "DocumentsUploaded"
  | "UnderReview"
  | "Verified"
  | "Rejected";
export type DealerPlan = "None" | "Starter" | "Pro" | "Enterprise";

// Tipos
interface Dealer {
  id: string;
  userId: string;
  businessName: string;
  legalName: string;
  rnc: string;
  dealerType: DealerType;
  status: DealerStatus;
  verificationStatus: VerificationStatus;
  currentPlan: DealerPlan;
  email: string;
  phone: string;
  mobilePhone?: string;
  website?: string;
  address: string;
  city: string;
  province: string;
  description?: string;
  logoUrl?: string;
  establishedDate?: string;
  employeeCount?: number;
  activeListings: number;
  maxActiveListings: number;
  isSubscriptionActive: boolean;
  subscriptionExpiresAt?: string;
  isVerified: boolean;
  verifiedAt?: string;
  isEarlyBird: boolean;
  createdAt: string;
  updatedAt: string;
}

interface DealerLocation {
  id: string;
  dealerId: string;
  name: string;
  locationType:
    | "Headquarters"
    | "Branch"
    | "Showroom"
    | "ServiceCenter"
    | "Warehouse";
  address: string;
  city: string;
  province: string;
  latitude?: number;
  longitude?: number;
  phone?: string;
  email?: string;
  isPrimary: boolean;
  isActive: boolean;
  businessHours?: BusinessHours;
}

interface BusinessHours {
  monday?: { open: string; close: string };
  tuesday?: { open: string; close: string };
  wednesday?: { open: string; close: string };
  thursday?: { open: string; close: string };
  friday?: { open: string; close: string };
  saturday?: { open: string; close: string };
  sunday?: { open: string; close: string };
}

interface CreateDealerRequest {
  businessName: string;
  legalName?: string;
  rnc: string;
  dealerType: DealerType;
  email: string;
  phone: string;
  mobilePhone?: string;
  website?: string;
  address: string;
  city: string;
  province: string;
  description?: string;
  establishedDate?: string;
  employeeCount?: number;
}

interface DealerListResponse {
  items: Dealer[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

interface DealerFilter {
  page?: number;
  pageSize?: number;
  status?: DealerStatus;
  verificationStatus?: VerificationStatus;
  searchTerm?: string;
}

export const dealerService = {
  // Listar dealers
  async getDealers(filter: DealerFilter = {}): Promise<DealerListResponse> {
    const response = await apiClient.get("/api/dealers", { params: filter });
    return response.data;
  },

  // Obtener dealer por ID
  async getDealerById(id: string): Promise<Dealer> {
    const response = await apiClient.get(`/api/dealers/${id}`);
    return response.data;
  },

  // Obtener dealer por User ID
  async getDealerByUserId(userId: string): Promise<Dealer | null> {
    try {
      const response = await apiClient.get(`/api/dealers/user/${userId}`);
      return response.data;
    } catch (error: any) {
      if (error.response?.status === 404) return null;
      throw error;
    }
  },

  // Crear dealer
  async createDealer(request: CreateDealerRequest): Promise<Dealer> {
    const response = await apiClient.post("/api/dealers", request);
    return response.data;
  },

  // Actualizar dealer
  async updateDealer(
    id: string,
    updates: Partial<CreateDealerRequest>,
  ): Promise<Dealer> {
    const response = await apiClient.put(`/api/dealers/${id}`, updates);
    return response.data;
  },

  // Verificar dealer (Admin)
  async verifyDealer(
    id: string,
    verification: {
      approved: boolean;
      reason?: string;
      verifiedBy: string;
    },
  ): Promise<Dealer> {
    const response = await apiClient.post(
      `/api/dealers/${id}/verify`,
      verification,
    );
    return response.data;
  },

  // === LOCATIONS ===

  async getLocations(dealerId: string): Promise<DealerLocation[]> {
    const response = await apiClient.get(`/api/locations/dealer/${dealerId}`);
    return response.data;
  },

  async createLocation(
    location: Omit<DealerLocation, "id">,
  ): Promise<DealerLocation> {
    const response = await apiClient.post("/api/locations", location);
    return response.data;
  },

  async updateLocation(
    id: string,
    updates: Partial<DealerLocation>,
  ): Promise<DealerLocation> {
    const response = await apiClient.put(`/api/locations/${id}`, updates);
    return response.data;
  },

  async deleteLocation(id: string): Promise<void> {
    await apiClient.delete(`/api/locations/${id}`);
  },

  // === SUBSCRIPTIONS ===

  async getSubscription(dealerId: string): Promise<{
    plan: DealerPlan;
    maxListings: number;
    isActive: boolean;
    expiresAt?: string;
    isEarlyBird: boolean;
    monthlyPrice: number;
  }> {
    const response = await apiClient.get(
      `/api/subscriptions/dealer/${dealerId}`,
    );
    return response.data;
  },

  // === HELPERS ===

  getPlanDetails(plan: DealerPlan): {
    name: string;
    price: number;
    earlyBirdPrice: number;
    maxListings: number;
    features: string[];
  } {
    const plans = {
      None: {
        name: "Sin Plan",
        price: 0,
        earlyBirdPrice: 0,
        maxListings: 0,
        features: [],
      },
      Starter: {
        name: "Starter",
        price: 49,
        earlyBirdPrice: 39,
        maxListings: 15,
        features: ["15 vehículos", "Soporte email", "Dashboard básico"],
      },
      Pro: {
        name: "Pro",
        price: 129,
        earlyBirdPrice: 103,
        maxListings: 50,
        features: [
          "50 vehículos",
          "Import CSV",
          "Analytics",
          "Badge verificado",
          "Soporte prioritario",
        ],
      },
      Enterprise: {
        name: "Enterprise",
        price: 299,
        earlyBirdPrice: 239,
        maxListings: 9999,
        features: [
          "Vehículos ilimitados",
          "API access",
          "Multiple sucursales",
          "Account manager",
        ],
      },
    };
    return plans[plan];
  },
};
```

---

## 🪝 Hooks de React

```typescript
// hooks/useDealer.ts

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { dealerService } from "../services/dealerService";
import { useAuth } from "./useAuth";

export function useCurrentDealer() {
  const { user } = useAuth();

  return useQuery({
    queryKey: ["dealer", "current", user?.id],
    queryFn: () => dealerService.getDealerByUserId(user!.id),
    enabled: !!user?.id,
    staleTime: 5 * 60 * 1000,
  });
}

export function useDealers(filter: DealerFilter = {}) {
  return useQuery({
    queryKey: ["dealers", filter],
    queryFn: () => dealerService.getDealers(filter),
  });
}

export function useDealer(id: string) {
  return useQuery({
    queryKey: ["dealer", id],
    queryFn: () => dealerService.getDealerById(id),
    enabled: !!id,
  });
}

export function useDealerLocations(dealerId: string) {
  return useQuery({
    queryKey: ["dealer-locations", dealerId],
    queryFn: () => dealerService.getLocations(dealerId),
    enabled: !!dealerId,
  });
}

export function useCreateDealer() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: dealerService.createDealer,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["dealers"] });
      queryClient.invalidateQueries({ queryKey: ["dealer", "current"] });
    },
  });
}

export function useVerifyDealer() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      id,
      ...data
    }: {
      id: string;
      approved: boolean;
      reason?: string;
      verifiedBy: string;
    }) => dealerService.verifyDealer(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["dealers"] });
    },
  });
}
```

---

## 🧩 Componentes de Ejemplo

### Dealer Card

```tsx
// components/DealerCard.tsx

import { Dealer } from "../services/dealerService";

interface DealerCardProps {
  dealer: Dealer;
  showActions?: boolean;
  onVerify?: (approved: boolean) => void;
}

export function DealerCard({ dealer, showActions, onVerify }: DealerCardProps) {
  const statusColors = {
    Pending: "bg-yellow-100 text-yellow-800",
    UnderReview: "bg-blue-100 text-blue-800",
    Active: "bg-green-100 text-green-800",
    Suspended: "bg-red-100 text-red-800",
    Rejected: "bg-gray-100 text-gray-800",
  };

  return (
    <div className="bg-white rounded-lg shadow p-6">
      <div className="flex items-start justify-between">
        <div className="flex items-center gap-4">
          {dealer.logoUrl ? (
            <img
              src={dealer.logoUrl}
              alt={dealer.businessName}
              className="w-16 h-16 rounded-lg object-cover"
            />
          ) : (
            <div className="w-16 h-16 rounded-lg bg-gray-200 flex items-center justify-center">
              <span className="text-2xl">🏪</span>
            </div>
          )}
          <div>
            <h3 className="font-bold text-lg">{dealer.businessName}</h3>
            <p className="text-gray-500 text-sm">RNC: {dealer.rnc}</p>
            <p className="text-gray-500 text-sm">
              {dealer.city}, {dealer.province}
            </p>
          </div>
        </div>

        <div className="flex flex-col items-end gap-2">
          <span
            className={`px-3 py-1 rounded-full text-xs font-medium ${statusColors[dealer.status]}`}
          >
            {dealer.status}
          </span>
          {dealer.isVerified && (
            <span className="text-green-600 text-sm flex items-center gap-1">
              ✓ Verificado
            </span>
          )}
        </div>
      </div>

      <div className="mt-4 grid grid-cols-3 gap-4 text-center">
        <div>
          <p className="text-2xl font-bold">{dealer.activeListings}</p>
          <p className="text-xs text-gray-500">Vehículos</p>
        </div>
        <div>
          <p className="text-2xl font-bold">{dealer.currentPlan}</p>
          <p className="text-xs text-gray-500">Plan</p>
        </div>
        <div>
          <p className="text-2xl font-bold">{dealer.employeeCount || "-"}</p>
          <p className="text-xs text-gray-500">Empleados</p>
        </div>
      </div>

      {showActions && dealer.status === "Pending" && (
        <div className="mt-4 flex gap-2">
          <Button variant="success" onClick={() => onVerify?.(true)}>
            Aprobar
          </Button>
          <Button variant="danger" onClick={() => onVerify?.(false)}>
            Rechazar
          </Button>
        </div>
      )}
    </div>
  );
}
```

---

## 🧪 Testing

### E2E Test (Playwright)

```typescript
// e2e/dealer.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Dealer Management", () => {
  test("should register new dealer", async ({ page }) => {
    await page.goto("/dealer/register");

    await page.fill('[name="businessName"]', "Auto Test RD");
    await page.fill('[name="rnc"]', "123456789");
    await page.fill('[name="email"]', "test@autotest.com");
    await page.fill('[name="phone"]', "809-555-0100");
    await page.fill('[name="address"]', "Calle Test 123");
    await page.selectOption('[name="city"]', "Santo Domingo");
    await page.selectOption('[name="province"]', "Distrito Nacional");

    await page.click('[data-testid="submit-registration"]');

    await expect(page).toHaveURL(/\/dealer\/subscribe/);
  });

  test("admin should verify pending dealer", async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto("/admin/dealers?status=Pending");

    await page.click(
      '[data-testid="dealer-card"]:first-child [data-testid="verify-btn"]',
    );
    await page.click('[data-testid="approve-btn"]');

    await expect(page.locator('[data-testid="success-toast"]')).toBeVisible();
  });
});
```

---

## 📊 Planes de Dealer

| Plan           | Precio   | Early Bird | Vehículos | Features                                   |
| -------------- | -------- | ---------- | --------- | ------------------------------------------ |
| **Starter**    | $49/mes  | $39/mes    | 15        | Dashboard básico, soporte email            |
| **Pro**        | $129/mes | $103/mes   | 50        | CSV import, analytics, badge verificado    |
| **Enterprise** | $299/mes | $239/mes   | ∞         | API, múltiples sucursales, account manager |

---

## 🔗 Referencias

- [Sprint 5 Completed](../../docs/SPRINT_5_DEALER_DASHBOARD_COMPLETED.md)
- [Dealer Landing Page](../04-PAGINAS/05-DEALER/)
- [Admin Dealers Management](../04-PAGINAS/06-ADMIN/)

---

_Los dealers son el principal modelo de ingresos de OKLA. Priorizar su experiencia._
