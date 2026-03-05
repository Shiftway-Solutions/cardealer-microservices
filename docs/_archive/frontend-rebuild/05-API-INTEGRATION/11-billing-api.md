# 💳 11 - Billing & Payments API (BillingService)

**Servicio:** BillingService  
**Puerto:** 8080  
**Base Path:** `/api/subscriptions`, `/api/payments`, `/api/billing/earlybird`  
**Autenticación:** ✅ Requerida (JWT Bearer Token)  
**Pasarelas:** AZUL, CardNET, PixelPay, Fygaro, PayPal

> ⚠️ **NOTA:** Este proyecto NO usa Stripe. Ver [29-payments-api.md](29-payments-api.md) para detalles de los 5 proveedores.

---

## 📋 Índice

1. [Descripción General](#descripción-general)
2. [Endpoints Disponibles](#endpoints-disponibles)
3. [TypeScript Types](#typescript-types)
4. [Service Layer](#service-layer)
5. [React Query Hooks](#react-query-hooks)
6. [Componentes de Ejemplo](#componentes-de-ejemplo)
7. [Flujo de Pago](#flujo-de-pago)
8. [Early Bird Program](#early-bird-program)

---

## 📖 Descripción General

El **BillingService** gestiona suscripciones, pagos, facturación y el programa Early Bird. Integra **5 pasarelas de pago** locales de República Dominicana:

| Proveedor    | Tipo       | Descripción                                 |
| ------------ | ---------- | ------------------------------------------- |
| **AZUL**     | Banking    | Banco Popular RD - Tarjetas locales (~2.5%) |
| **CardNET**  | Banking    | Red de tarjetas RD                          |
| **PixelPay** | Fintech    | Pagos digitales                             |
| **Fygaro**   | Aggregator | Agregador de pagos                          |
| **PayPal**   | Fintech    | Pagos internacionales (~3.5%)               |

### Casos de Uso

- 💳 Suscripciones mensuales de Dealers (Starter $49, Pro $129, Enterprise $299)
- 🎁 Programa Early Bird (3 meses gratis + 20% descuento de por vida)
- 📄 Facturación automática
- 🔄 Renovaciones y cancelaciones
- 📊 Historial de pagos

---

## 🎯 Endpoints Disponibles

### SubscriptionsController (11 endpoints)

#### 1. GET `/api/subscriptions` - Listar Todas las Suscripciones

**Auth:** ✅ Admin  
**Response 200:**

```json
[
  {
    "id": "sub-123",
    "dealerId": "dealer-456",
    "plan": "Pro",
    "status": "Active",
    "cycle": "Monthly",
    "pricePerCycle": 129.0,
    "startDate": "2026-01-15T00:00:00Z",
    "currentPeriodEnd": "2026-02-15T00:00:00Z",
    "maxUsers": 5,
    "maxVehicles": 50
  }
]
```

---

#### 2. GET `/api/subscriptions/{id}` - Obtener Suscripción por ID

**Auth:** ✅ Required  
**Response 200:**

```json
{
  "id": "sub-123",
  "dealerId": "dealer-456",
  "plan": "Pro",
  "status": "Active",
  "cycle": "Monthly",
  "pricePerCycle": 129.0,
  "startDate": "2026-01-15T00:00:00Z",
  "currentPeriodEnd": "2026-02-15T00:00:00Z",
  "nextBillingDate": "2026-02-15T00:00:00Z",
  "trialEndDate": "2026-04-15T00:00:00Z",
  "maxUsers": 5,
  "maxVehicles": 50,
  "features": "analytics,bulk_upload,priority_support",
  "createdAt": "2026-01-15T00:00:00Z"
}
```

---

#### 3. GET `/api/subscriptions/dealer/{dealerId}` - Suscripción de un Dealer

**Auth:** ✅ Required  
**Path Params:** `dealerId` (UUID)

**Response 200:** Igual que endpoint #2

---

#### 4. GET `/api/subscriptions/status/{status}` - Filtrar por Estado

**Auth:** ✅ Admin  
**Path Params:** `status` (Trial | Active | PastDue | Cancelled | Suspended)

**Response 200:** Array de suscripciones

---

#### 5. GET `/api/subscriptions/plan/{plan}` - Filtrar por Plan

**Auth:** ✅ Admin  
**Path Params:** `plan` (Starter | Pro | Enterprise)

**Response 200:** Array de suscripciones

---

#### 6. GET `/api/subscriptions/expiring-trials/{days}` - Trials por Expirar

**Auth:** ✅ Admin  
**Path Params:** `days` (int) - Ej: `7` para trials que expiran en 7 días

**Response 200:** Array de suscripciones con trial próximo a expirar

---

#### 7. GET `/api/subscriptions/due-billings` - Facturaciones Pendientes

**Auth:** ✅ Admin  
**Response 200:** Array de suscripciones con `nextBillingDate` <= hoy

---

#### 8. POST `/api/subscriptions` - Crear Suscripción

**Auth:** ✅ Required  
**Headers:** `X-Dealer-Id` (UUID)

**Request Body:**

```json
{
  "plan": "Pro",
  "cycle": "Monthly",
  "pricePerCycle": 129.0,
  "maxUsers": 5,
  "maxVehicles": 50,
  "trialDays": 90,
  "features": "analytics,bulk_upload,priority_support"
}
```

**Response 201:**

```json
{
  "id": "sub-789",
  "dealerId": "dealer-456",
  "plan": "Pro",
  "status": "Trial",
  "cycle": "Monthly",
  "pricePerCycle": 129.0,
  "startDate": "2026-01-30T00:00:00Z",
  "trialEndDate": "2026-04-30T00:00:00Z",
  "maxUsers": 5,
  "maxVehicles": 50
}
```

**Response 409:** Dealer ya tiene suscripción

---

#### 9. POST `/api/subscriptions/{id}/activate` - Activar Suscripción

**Auth:** ✅ Admin  
**Response 200:** Suscripción actualizada con `status: "Active"`

---

#### 10. POST `/api/subscriptions/{id}/suspend` - Suspender Suscripción

**Auth:** ✅ Admin  
**Response 200:** Suscripción actualizada con `status: "Suspended"`

---

#### 11. POST `/api/subscriptions/{id}/cancel` - Cancelar Suscripción

**Auth:** ✅ Required  
**Request Body:**

```json
{
  "reason": "Too expensive"
}
```

**Response 200:** Suscripción actualizada con `status: "Cancelled"`, `cancelledAt`, `cancellationReason`

---

### PaymentsController (9 endpoints)

#### 12. GET `/api/payments` - Listar Pagos del Dealer

**Auth:** ✅ Required  
**Headers:** `X-Dealer-Id` (UUID)

**Response 200:**

```json
[
  {
    "id": "pay-123",
    "subscriptionId": "sub-456",
    "dealerId": "dealer-789",
    "amount": 129.0,
    "currency": "USD",
    "status": "Succeeded",
    "method": "Azul",
    "externalPaymentId": "AZUL-2026013012345",
    "paymentDate": "2026-01-15T10:30:00Z",
    "description": "Pro Plan - Enero 2026"
  }
]
```

---

#### 13. GET `/api/payments/{id}` - Obtener Pago por ID

**Auth:** ✅ Required  
**Response 200:** Detalle completo del pago

---

#### 14. GET `/api/payments/subscription/{subscriptionId}` - Pagos de una Suscripción

**Auth:** ✅ Required  
**Response 200:** Array de pagos históricos de la suscripción

---

#### 15. GET `/api/payments/status/{status}` - Filtrar por Estado

**Auth:** ✅ Required  
**Headers:** `X-Dealer-Id` (UUID)  
**Path Params:** `status` (Pending | Succeeded | Failed | Refunded)

**Response 200:** Array de pagos

---

#### 16. GET `/api/payments/date-range` - Pagos por Rango de Fechas

**Auth:** ✅ Required  
**Headers:** `X-Dealer-Id` (UUID)  
**Query Params:**

- `startDate` (DateTime) - Ej: `2026-01-01T00:00:00Z`
- `endDate` (DateTime) - Ej: `2026-01-31T23:59:59Z`

**Response 200:** Array de pagos en el rango

---

#### 17. GET `/api/payments/pending` - Pagos Pendientes

**Auth:** ✅ Admin  
**Response 200:** Array de pagos con `status: "Pending"`

---

#### 18. GET `/api/payments/failed` - Pagos Fallidos

**Auth:** ✅ Admin  
**Response 200:** Array de pagos con `status: "Failed"`

---

#### 19. GET `/api/payments/external/{externalPaymentId}` - Buscar por ID Externo

**Auth:** ✅ Required  
**Response 200:** Pago asociado al ID de la pasarela (AZUL, CardNET, etc.)

---

#### 20. GET `/api/payments/total/{dealerId}` - Total Pagado por Dealer

**Auth:** ✅ Admin  
**Response 200:**

```json
{
  "total": 1548.0
}
```

---

### EarlyBirdController (2 endpoints)

#### 21. GET `/api/billing/earlybird/status` - Estado Early Bird del Usuario

**Auth:** ✅ Required  
**Response 200:**

```json
{
  "isEnrolled": true,
  "hasFounderBadge": true,
  "isInFreePeriod": true,
  "remainingFreeDays": 68,
  "enrolledAt": "2026-01-15T00:00:00Z",
  "freeUntil": "2026-04-15T00:00:00Z",
  "hasUsedBenefit": false,
  "message": "¡Tienes 68 días gratis restantes!"
}
```

**Response 200 (No inscrito):**

```json
{
  "isEnrolled": false,
  "hasFounderBadge": false,
  "isInFreePeriod": false,
  "remainingFreeDays": 0,
  "message": "Usuario no inscrito en Early Bird"
}
```

---

#### 22. POST `/api/billing/earlybird/enroll` - Inscribir en Early Bird

**Auth:** ✅ Required  
**Request Body (opcional):**

```json
{
  "freeMonths": 3
}
```

**Response 200:**

```json
{
  "isEnrolled": true,
  "hasFounderBadge": true,
  "isInFreePeriod": true,
  "remainingFreeDays": 90,
  "enrolledAt": "2026-01-30T10:30:00Z",
  "freeUntil": "2026-04-30T10:30:00Z",
  "hasUsedBenefit": false,
  "message": "¡Bienvenido al programa Early Bird! Tienes 3 meses gratis."
}
```

**Response 400:** Usuario ya inscrito

---

## 🔧 TypeScript Types

```typescript
// ============================================================================
// SUBSCRIPTION TYPES
// ============================================================================

export interface Subscription {
  id: string;
  dealerId: string;
  plan: SubscriptionPlan;
  status: SubscriptionStatus;
  cycle: BillingCycle;
  pricePerCycle: number;
  startDate: string;
  currentPeriodStart?: string;
  currentPeriodEnd?: string;
  nextBillingDate?: string;
  trialStartDate?: string;
  trialEndDate?: string;
  cancelledAt?: string;
  cancellationReason?: string;
  maxUsers: number;
  maxVehicles: number;
  features?: string;
  createdAt: string;
  updatedAt?: string;
}

export type SubscriptionPlan = "Starter" | "Pro" | "Enterprise";

export type SubscriptionStatus =
  | "Trial" // En período de prueba
  | "Active" // Activa y pagando
  | "PastDue" // Pago atrasado
  | "Cancelled" // Cancelada por usuario
  | "Suspended"; // Suspendida por admin

export type BillingCycle = "Monthly" | "Annually";

export interface CreateSubscriptionRequest {
  plan: SubscriptionPlan;
  cycle: BillingCycle;
  pricePerCycle: number;
  maxUsers: number;
  maxVehicles: number;
  trialDays?: number;
  features?: string;
}

// ============================================================================
// PAYMENT TYPES
// ============================================================================

export interface Payment {
  id: string;
  subscriptionId: string;
  dealerId: string;
  amount: number;
  currency: string;
  status: PaymentStatus;
  method: PaymentMethod;
  stripePaymentIntentId?: string;
  azulTransactionId?: string;
  paymentDate?: string;
  description?: string;
  failureReason?: string;
  createdAt: string;
}

export type PaymentStatus =
  | "Pending" // Pendiente de procesamiento
  | "Succeeded" // Pago exitoso
  | "Failed" // Falló
  | "Refunded"; // Reembolsado

export type PaymentMethod = "Stripe" | "Azul" | "Manual";

// ============================================================================
// EARLY BIRD TYPES
// ============================================================================

export interface EarlyBirdStatus {
  isEnrolled: boolean;
  hasFounderBadge: boolean;
  isInFreePeriod: boolean;
  remainingFreeDays: number;
  enrolledAt?: string;
  freeUntil?: string;
  hasUsedBenefit?: boolean;
  benefitUsedAt?: string;
  message: string;
}

export interface EnrollRequest {
  freeMonths?: number;
}

// ============================================================================
// PLAN INFO (Frontend Static Data)
// ============================================================================

export interface PlanInfo {
  name: SubscriptionPlan;
  displayName: string;
  priceMonthly: number;
  priceAnnually: number;
  earlyBirdMonthly: number;
  earlyBirdAnnually: number;
  maxVehicles: number;
  maxUsers: number;
  features: string[];
  recommended?: boolean;
}

export const PLANS: Record<SubscriptionPlan, PlanInfo> = {
  Starter: {
    name: "Starter",
    displayName: "Plan Starter",
    priceMonthly: 49,
    priceAnnually: 490,
    earlyBirdMonthly: 39,
    earlyBirdAnnually: 392,
    maxVehicles: 15,
    maxUsers: 2,
    features: [
      "15 vehículos activos",
      "2 usuarios",
      "Soporte email",
      "Estadísticas básicas",
    ],
  },
  Pro: {
    name: "Pro",
    displayName: "Plan Pro",
    priceMonthly: 129,
    priceAnnually: 1290,
    earlyBirdMonthly: 103,
    earlyBirdAnnually: 1032,
    maxVehicles: 50,
    maxUsers: 5,
    features: [
      "50 vehículos activos",
      "5 usuarios",
      "Soporte prioritario",
      "Analytics avanzados",
      "Import masivo CSV",
      "Badge verificado",
    ],
    recommended: true,
  },
  Enterprise: {
    name: "Enterprise",
    displayName: "Plan Enterprise",
    priceMonthly: 299,
    priceAnnually: 2990,
    earlyBirdMonthly: 239,
    earlyBirdAnnually: 2392,
    maxVehicles: -1, // Ilimitado
    maxUsers: 20,
    features: [
      "Vehículos ILIMITADOS",
      "20 usuarios",
      "Soporte 24/7",
      "Analytics premium",
      "API access",
      "White label",
      "Account manager",
    ],
  },
};
```

---

## 📡 Service Layer

```typescript
// src/services/billingService.ts
import { apiClient } from "./api-client";
import type {
  Subscription,
  CreateSubscriptionRequest,
  Payment,
  EarlyBirdStatus,
  EnrollRequest,
} from "@/types/billing";

class BillingService {
  // ============================================================================
  // SUBSCRIPTIONS
  // ============================================================================

  async getSubscriptions(): Promise<Subscription[]> {
    const response = await apiClient.get<Subscription[]>("/api/subscriptions");
    return response.data;
  }

  async getSubscriptionById(id: string): Promise<Subscription> {
    const response = await apiClient.get<Subscription>(
      `/api/subscriptions/${id}`,
    );
    return response.data;
  }

  async getSubscriptionByDealer(dealerId: string): Promise<Subscription> {
    const response = await apiClient.get<Subscription>(
      `/api/subscriptions/dealer/${dealerId}`,
    );
    return response.data;
  }

  async createSubscription(
    request: CreateSubscriptionRequest,
    dealerId: string,
  ): Promise<Subscription> {
    const response = await apiClient.post<Subscription>(
      "/api/subscriptions",
      request,
      { headers: { "X-Dealer-Id": dealerId } },
    );
    return response.data;
  }

  async activateSubscription(id: string): Promise<Subscription> {
    const response = await apiClient.post<Subscription>(
      `/api/subscriptions/${id}/activate`,
    );
    return response.data;
  }

  async suspendSubscription(id: string): Promise<Subscription> {
    const response = await apiClient.post<Subscription>(
      `/api/subscriptions/${id}/suspend`,
    );
    return response.data;
  }

  async cancelSubscription(id: string, reason: string): Promise<Subscription> {
    const response = await apiClient.post<Subscription>(
      `/api/subscriptions/${id}/cancel`,
      reason,
      { headers: { "Content-Type": "application/json" } },
    );
    return response.data;
  }

  // ============================================================================
  // PAYMENTS
  // ============================================================================

  async getPayments(dealerId: string): Promise<Payment[]> {
    const response = await apiClient.get<Payment[]>("/api/payments", {
      headers: { "X-Dealer-Id": dealerId },
    });
    return response.data;
  }

  async getPaymentById(id: string): Promise<Payment> {
    const response = await apiClient.get<Payment>(`/api/payments/${id}`);
    return response.data;
  }

  async getPaymentsBySubscription(subscriptionId: string): Promise<Payment[]> {
    const response = await apiClient.get<Payment[]>(
      `/api/payments/subscription/${subscriptionId}`,
    );
    return response.data;
  }

  async getPaymentsByDateRange(
    startDate: string,
    endDate: string,
    dealerId: string,
  ): Promise<Payment[]> {
    const response = await apiClient.get<Payment[]>(
      "/api/payments/date-range",
      {
        params: { startDate, endDate },
        headers: { "X-Dealer-Id": dealerId },
      },
    );
    return response.data;
  }

  async getTotalByDealer(dealerId: string): Promise<number> {
    const response = await apiClient.get<number>(
      `/api/payments/total/${dealerId}`,
    );
    return response.data;
  }

  // ============================================================================
  // EARLY BIRD
  // ============================================================================

  async getEarlyBirdStatus(): Promise<EarlyBirdStatus> {
    const response = await apiClient.get<EarlyBirdStatus>(
      "/api/billing/earlybird/status",
    );
    return response.data;
  }

  async enrollEarlyBird(request?: EnrollRequest): Promise<EarlyBirdStatus> {
    const response = await apiClient.post<EarlyBirdStatus>(
      "/api/billing/earlybird/enroll",
      request || {},
    );
    return response.data;
  }
}

export const billingService = new BillingService();
```

---

## 🎣 React Query Hooks

```typescript
// src/hooks/useBilling.ts
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { billingService } from "@/services/billingService";
import type { CreateSubscriptionRequest } from "@/types/billing";

export const billingKeys = {
  all: ["billing"] as const,
  subscriptions: () => [...billingKeys.all, "subscriptions"] as const,
  subscription: (id: string) => [...billingKeys.subscriptions(), id] as const,
  subscriptionByDealer: (dealerId: string) =>
    [...billingKeys.subscriptions(), "dealer", dealerId] as const,
  payments: (dealerId: string) =>
    [...billingKeys.all, "payments", dealerId] as const,
  paymentsBySubscription: (subscriptionId: string) =>
    [...billingKeys.all, "payments", "subscription", subscriptionId] as const,
  earlyBird: () => [...billingKeys.all, "earlybird"] as const,
};

// ============================================================================
// SUBSCRIPTIONS
// ============================================================================

export function useSubscription(id: string) {
  return useQuery({
    queryKey: billingKeys.subscription(id),
    queryFn: () => billingService.getSubscriptionById(id),
    enabled: !!id,
  });
}

export function useSubscriptionByDealer(dealerId: string) {
  return useQuery({
    queryKey: billingKeys.subscriptionByDealer(dealerId),
    queryFn: () => billingService.getSubscriptionByDealer(dealerId),
    enabled: !!dealerId,
  });
}

export function useCreateSubscription(dealerId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateSubscriptionRequest) =>
      billingService.createSubscription(request, dealerId),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: billingKeys.subscriptionByDealer(dealerId),
      });
    },
  });
}

export function useCancelSubscription() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      billingService.cancelSubscription(id, reason),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: billingKeys.subscription(variables.id),
      });
    },
  });
}

// ============================================================================
// PAYMENTS
// ============================================================================

export function usePayments(dealerId: string) {
  return useQuery({
    queryKey: billingKeys.payments(dealerId),
    queryFn: () => billingService.getPayments(dealerId),
    enabled: !!dealerId,
  });
}

export function usePaymentsBySubscription(subscriptionId: string) {
  return useQuery({
    queryKey: billingKeys.paymentsBySubscription(subscriptionId),
    queryFn: () => billingService.getPaymentsBySubscription(subscriptionId),
    enabled: !!subscriptionId,
  });
}

// ============================================================================
// EARLY BIRD
// ============================================================================

export function useEarlyBirdStatus() {
  return useQuery({
    queryKey: billingKeys.earlyBird(),
    queryFn: () => billingService.getEarlyBirdStatus(),
  });
}

export function useEnrollEarlyBird() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: billingService.enrollEarlyBird,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: billingKeys.earlyBird() });
    },
  });
}
```

---

## 🧩 Componentes de Ejemplo

### 1. SubscriptionCard - Tarjeta de Suscripción

```typescript
// src/components/billing/SubscriptionCard.tsx
import { useSubscriptionByDealer } from "@/hooks/useBilling";
import { formatDistanceToNow } from "date-fns";
import { es } from "date-fns/locale";

export const SubscriptionCard = ({ dealerId }: { dealerId: string }) => {
  const { data: subscription, isLoading } = useSubscriptionByDealer(dealerId);

  if (isLoading) return <div>Cargando...</div>;
  if (!subscription) return <div>No hay suscripción activa</div>;

  const statusColors = {
    Trial: "bg-blue-100 text-blue-800",
    Active: "bg-green-100 text-green-800",
    PastDue: "bg-yellow-100 text-yellow-800",
    Cancelled: "bg-red-100 text-red-800",
    Suspended: "bg-gray-100 text-gray-800",
  };

  return (
    <div className="bg-white p-6 rounded-lg border">
      {/* Header */}
      <div className="flex items-start justify-between mb-4">
        <div>
          <h3 className="text-lg font-semibold">Plan {subscription.plan}</h3>
          <p className="text-gray-600">${subscription.pricePerCycle}/{subscription.cycle}</p>
        </div>
        <span className={`px-3 py-1 rounded-full text-sm font-medium ${statusColors[subscription.status]}`}>
          {subscription.status}
        </span>
      </div>

      {/* Details */}
      <div className="space-y-2 text-sm">
        <div className="flex justify-between">
          <span className="text-gray-600">Vehículos:</span>
          <span className="font-medium">{subscription.maxVehicles === -1 ? "Ilimitados" : subscription.maxVehicles}</span>
        </div>
        <div className="flex justify-between">
          <span className="text-gray-600">Usuarios:</span>
          <span className="font-medium">{subscription.maxUsers}</span>
        </div>
        {subscription.nextBillingDate && (
          <div className="flex justify-between">
            <span className="text-gray-600">Próxima factura:</span>
            <span className="font-medium">
              {formatDistanceToNow(new Date(subscription.nextBillingDate), {
                addSuffix: true,
                locale: es,
              })}
            </span>
          </div>
        )}
      </div>

      {/* Actions */}
      <div className="mt-6 flex gap-2">
        <button className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700">
          Cambiar Plan
        </button>
        <button className="px-4 py-2 border rounded-lg hover:bg-gray-50">
          Cancelar
        </button>
      </div>
    </div>
  );
};
```

---

### 2. PaymentHistory - Historial de Pagos

```typescript
// src/components/billing/PaymentHistory.tsx
import { usePayments } from "@/hooks/useBilling";
import { format } from "date-fns";

export const PaymentHistory = ({ dealerId }: { dealerId: string }) => {
  const { data: payments, isLoading } = usePayments(dealerId);

  if (isLoading) return <div>Cargando pagos...</div>;

  return (
    <div className="bg-white rounded-lg border">
      <div className="px-6 py-4 border-b">
        <h3 className="text-lg font-semibold">Historial de Pagos</h3>
      </div>

      <div className="overflow-x-auto">
        <table className="w-full">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Fecha</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Descripción</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Método</th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">Monto</th>
              <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase">Estado</th>
            </tr>
          </thead>
          <tbody className="divide-y">
            {payments?.map((payment) => (
              <tr key={payment.id} className="hover:bg-gray-50">
                <td className="px-6 py-4 whitespace-nowrap text-sm">
                  {format(new Date(payment.paymentDate || payment.createdAt), "dd/MM/yyyy")}
                </td>
                <td className="px-6 py-4 text-sm">{payment.description}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm">
                  {payment.method === "Stripe" ? "💳 Stripe" : "🏦 Azul"}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-right font-medium">
                  ${payment.amount.toFixed(2)}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-center">
                  {payment.status === "Succeeded" ? (
                    <span className="px-2 py-1 text-xs bg-green-100 text-green-800 rounded-full">Exitoso</span>
                  ) : payment.status === "Failed" ? (
                    <span className="px-2 py-1 text-xs bg-red-100 text-red-800 rounded-full">Fallido</span>
                  ) : (
                    <span className="px-2 py-1 text-xs bg-yellow-100 text-yellow-800 rounded-full">Pendiente</span>
                  )}
                </td>
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

## 💰 Flujo de Pago Completo

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                  FLUJO DE SUSCRIPCIÓN Y PAGO                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1️⃣ SELECCIÓN DE PLAN                                                       │
│  ├─> Usuario ve /dealer/pricing                                            │
│  ├─> Compara: Starter ($49), Pro ($129), Enterprise ($299)                 │
│  ├─> Si Early Bird activo → Ve precios con 20% OFF                         │
│  └─> Click "Aprovechar Oferta"                                             │
│                                                                             │
│  2️⃣ REGISTRO DE DEALER                                                      │
│  ├─> POST /api/dealers (crea cuenta dealer)                                │
│  ├─> POST /api/billing/earlybird/enroll (si aplicable)                     │
│  └─> Recibe dealerId                                                        │
│                                                                             │
│  3️⃣ CREACIÓN DE SUSCRIPCIÓN                                                 │
│  ├─> POST /api/subscriptions                                               │
│  │   {                                                                      │
│  │     "plan": "Pro",                                                       │
│  │     "cycle": "Monthly",                                                  │
│  │     "pricePerCycle": 103, // Early Bird 20% OFF                         │
│  │     "trialDays": 90        // 3 meses gratis                            │
│  │   }                                                                      │
│  ├─> Backend crea Subscription con Status="Trial"                          │
│  ├─> trialEndDate = hoy + 90 días                                          │
│  └─> firstBillingDate = trialEndDate                                       │
│                                                                             │
│  4️⃣ MÉTODO DE PAGO (Opcional durante trial)                                │
│  ├─> Usuario puede agregar tarjeta ahora o después                         │
│  ├─> Si dominicano → Preferir Azul (comisión menor)                        │
│  ├─> Si extranjero → Stripe                                                │
│  └─> Tokenizar tarjeta (NO cobrar aún)                                     │
│                                                                             │
│  5️⃣ PRIMER COBRO (Después de trial)                                        │
│  ├─> Cronjob daily ejecuta: GET /api/subscriptions/due-billings            │
│  ├─> Para cada suscripción con nextBillingDate = hoy:                      │
│  │   • POST /api/payments/charge                                           │
│  │   • Si Stripe → Stripe PaymentIntent                                    │
│  │   • Si Azul → Azul API                                                  │
│  ├─> Si pago exitoso:                                                      │
│  │   • Payment.status = "Succeeded"                                        │
│  │   • Subscription.status = "Active"                                      │
│  │   • nextBillingDate += 1 mes                                            │
│  └─> Si pago falla:                                                        │
│      • Payment.status = "Failed"                                            │
│      • Subscription.status = "PastDue"                                      │
│      • Enviar email notificación                                           │
│      • Retry en 3 días                                                     │
│                                                                             │
│  6️⃣ RENOVACIONES AUTOMÁTICAS                                                │
│  ├─> Cada mes, cronjob ejecuta ciclo de facturación                        │
│  ├─> Si Early Bird: Aplica 20% descuento permanentemente                   │
│  └─> Genera Invoice + envia email                                          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🎁 Early Bird Program

### Beneficios

1. **3 MESES GRATIS** - Primera factura en Abril 2026
2. **20% DESCUENTO DE POR VIDA** - Mientras suscripción activa
3. **Badge "Miembro Fundador"** - Permanente en perfil

### Precios con Early Bird

| Plan           | Regular  | Early Bird | Ahorro Mensual | Ahorro Anual |
| -------------- | -------- | ---------- | -------------- | ------------ |
| **Starter**    | $49/mes  | $39/mes    | $10            | $120         |
| **Pro**        | $129/mes | $103/mes   | $26            | $312         |
| **Enterprise** | $299/mes | $239/mes   | $60            | $720         |

### Implementación

```typescript
// Check si usuario es Early Bird
const { data: earlyBirdStatus } = useEarlyBirdStatus();

if (earlyBirdStatus?.isEnrolled && earlyBirdStatus.isInFreePeriod) {
  // Aplicar precios con descuento
  const discountedPrice = PLANS.Pro.priceMonthly * 0.8; // 20% OFF

  // Mostrar countdown
  const daysLeft = earlyBirdStatus.remainingFreeDays;
  console.log(`¡Tienes ${daysLeft} días gratis restantes!`);
}
```

---

## 🎉 Resumen

✅ **22 Endpoints documentados**  
✅ **TypeScript Types completos**  
✅ **Service Layer** con integración Stripe + Azul  
✅ **React Query Hooks** optimizados  
✅ **2 Componentes UI** (SubscriptionCard + PaymentHistory)  
✅ **Early Bird Program** completo  
✅ **Flujo de pago end-to-end** documentado

---

**🚀 Próximos Pasos:**

- Integrar Stripe Checkout
- Integrar Azul Payment Page
- Implementar webhooks
- Dashboard de facturación para dealers

---

_Última actualización: Enero 30, 2026_
