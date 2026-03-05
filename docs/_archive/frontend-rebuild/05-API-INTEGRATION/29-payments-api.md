# 💳 29 - Payments API (Multi-Proveedor)

**Servicio:** PaymentService  
**Puerto:** 8080  
**Base Path:** `/api/payments`  
**Autenticación:** ✅ Requerida

---

## 📖 Descripción

Sistema de pagos multi-proveedor para República Dominicana. Soporta 5 pasarelas de pago:

| Proveedor    | Tipo       | Descripción                         |
| ------------ | ---------- | ----------------------------------- |
| **AZUL**     | Banking    | Banco Popular RD - Tarjetas locales |
| **CardNET**  | Banking    | Red de tarjetas RD                  |
| **PixelPay** | Fintech    | Pagos digitales                     |
| **Fygaro**   | Aggregator | Agregador de pagos                  |
| **PayPal**   | Fintech    | Pagos internacionales               |

> ⚠️ **NOTA:** Este proyecto NO usa Stripe. Todos los pagos se procesan a través de pasarelas locales de República Dominicana.

---

## 🎯 Endpoints Disponibles

### Proveedores

| #   | Método | Endpoint                                   | Auth | Descripción         |
| --- | ------ | ------------------------------------------ | ---- | ------------------- |
| 1   | `GET`  | `/api/payments/providers`                  | ❌   | Listar proveedores  |
| 2   | `GET`  | `/api/payments/providers/{gateway}`        | ❌   | Info de proveedor   |
| 3   | `GET`  | `/api/payments/providers/{gateway}/health` | ❌   | Health de proveedor |

### Pagos

| #   | Método | Endpoint                        | Auth | Descripción         |
| --- | ------ | ------------------------------- | ---- | ------------------- |
| 4   | `POST` | `/api/payments/charge`          | ✅   | Procesar cobro      |
| 5   | `GET`  | `/api/payments/{transactionId}` | ✅   | Obtener transacción |
| 6   | `POST` | `/api/payments/refund`          | ✅   | Procesar reembolso  |
| 7   | `GET`  | `/api/payments/health`          | ❌   | Health de todos     |

### Suscripciones

| #   | Método   | Endpoint                  | Auth | Descripción          |
| --- | -------- | ------------------------- | ---- | -------------------- |
| 8   | `GET`    | `/api/subscriptions`      | ✅   | Mis suscripciones    |
| 9   | `POST`   | `/api/subscriptions`      | ✅   | Crear suscripción    |
| 10  | `DELETE` | `/api/subscriptions/{id}` | ✅   | Cancelar suscripción |

### Tasas de Cambio

| #   | Método | Endpoint                     | Auth | Descripción         |
| --- | ------ | ---------------------------- | ---- | ------------------- |
| 11  | `GET`  | `/api/exchangerates/current` | ❌   | Tasa actual USD/DOP |
| 12  | `GET`  | `/api/exchangerates/history` | ❌   | Historial de tasas  |

### Webhooks

| #   | Método | Endpoint                 | Auth | Descripción      |
| --- | ------ | ------------------------ | ---- | ---------------- |
| 13  | `POST` | `/api/webhooks/azul`     | ❌   | Webhook AZUL     |
| 14  | `POST` | `/api/webhooks/cardnet`  | ❌   | Webhook CardNET  |
| 15  | `POST` | `/api/webhooks/pixelpay` | ❌   | Webhook PixelPay |
| 16  | `POST` | `/api/webhooks/fygaro`   | ❌   | Webhook Fygaro   |
| 17  | `POST` | `/api/webhooks/paypal`   | ❌   | Webhook PayPal   |

---

## 📝 Detalle de Endpoints

### 1. GET `/api/payments/providers` - Listar Proveedores

**Response 200:**

```json
{
  "totalProviders": 5,
  "providers": [
    {
      "gateway": "Azul",
      "name": "AZUL",
      "type": "Banking",
      "isConfigured": true,
      "configurationErrors": []
    },
    {
      "gateway": "CardNET",
      "name": "CardNET",
      "type": "Banking",
      "isConfigured": true,
      "configurationErrors": []
    },
    {
      "gateway": "PixelPay",
      "name": "PixelPay",
      "type": "Fintech",
      "isConfigured": true,
      "configurationErrors": []
    },
    {
      "gateway": "Fygaro",
      "name": "Fygaro",
      "type": "Aggregator",
      "isConfigured": false,
      "configurationErrors": ["API_KEY not configured"]
    },
    {
      "gateway": "PayPal",
      "name": "PayPal",
      "type": "Fintech",
      "isConfigured": true,
      "configurationErrors": []
    }
  ]
}
```

---

### 4. POST `/api/payments/charge` - Procesar Cobro

**Request:**

```json
{
  "userId": "user-123",
  "amount": 1500.0,
  "currency": "DOP",
  "gateway": "Azul",
  "description": "Publicación de vehículo - Plan Pro",
  "metadata": {
    "planId": "pro",
    "vehicleId": "vehicle-456"
  },
  "cardToken": "tok_azul_abc123",
  "saveCard": true
}
```

**Gateways disponibles:** `Azul`, `CardNET`, `PixelPay`, `Fygaro`, `PayPal`

**Response 200:**

```json
{
  "transactionId": "txn-789",
  "externalId": "AZUL-2026013012345",
  "status": "Completed",
  "amount": 1500.0,
  "currency": "DOP",
  "gateway": "Azul",
  "gatewayResponse": {
    "authorizationCode": "123456",
    "referenceNumber": "REF789"
  },
  "createdAt": "2026-01-30T10:00:00Z"
}
```

---

### 7. GET `/api/payments/health` - Health de Todos los Proveedores

**Response 200:**

```json
{
  "overallStatus": "healthy",
  "service": "PaymentService",
  "totalProviders": 5,
  "availableProviders": 4,
  "unavailableProviders": 1,
  "defaultGateway": "Azul",
  "providers": [
    {
      "gateway": "Azul",
      "name": "AZUL",
      "isAvailable": true,
      "isConfigured": true,
      "status": "available",
      "latencyMs": 45,
      "checkedAt": "2026-01-30T10:00:00Z"
    },
    {
      "gateway": "CardNET",
      "name": "CardNET",
      "isAvailable": true,
      "isConfigured": true,
      "status": "available",
      "latencyMs": 62,
      "checkedAt": "2026-01-30T10:00:00Z"
    },
    {
      "gateway": "Fygaro",
      "name": "Fygaro",
      "isAvailable": false,
      "isConfigured": false,
      "status": "not_configured",
      "message": "1 configuration error(s)",
      "configurationErrors": ["API_KEY not configured"],
      "checkedAt": "2026-01-30T10:00:00Z"
    }
  ],
  "checkedAt": "2026-01-30T10:00:00Z"
}
```

---

### 11. GET `/api/exchangerates/current` - Tasa de Cambio

**Response 200:**

```json
{
  "baseCurrency": "USD",
  "targetCurrency": "DOP",
  "buyRate": 58.5,
  "sellRate": 59.25,
  "midRate": 58.875,
  "source": "BancoCentral",
  "updatedAt": "2026-01-30T09:00:00Z"
}
```

---

## 🔧 TypeScript Types

```typescript
// ============================================================================
// PAYMENT GATEWAYS
// ============================================================================

export type PaymentGateway =
  | "Azul"
  | "CardNET"
  | "PixelPay"
  | "Fygaro"
  | "PayPal";
export type GatewayType = "Banking" | "Fintech" | "Aggregator";

export interface ProviderInfo {
  gateway: PaymentGateway;
  name: string;
  type: GatewayType;
  isConfigured: boolean;
  configurationErrors: string[];
}

export interface ProviderHealth {
  gateway: PaymentGateway;
  name: string;
  isAvailable: boolean;
  isConfigured: boolean;
  status: "available" | "unavailable" | "not_configured" | "error";
  message?: string;
  latencyMs?: number;
  configurationErrors?: string[];
  checkedAt: string;
}

export interface AllProvidersHealth {
  overallStatus: "healthy" | "degraded" | "down";
  service: string;
  totalProviders: number;
  availableProviders: number;
  unavailableProviders: number;
  defaultGateway: PaymentGateway;
  providers: ProviderHealth[];
  checkedAt: string;
}

// ============================================================================
// CHARGES
// ============================================================================

export type TransactionStatus =
  | "Pending"
  | "Processing"
  | "Completed"
  | "Failed"
  | "Refunded";

export interface ChargeRequest {
  userId: string;
  amount: number;
  currency: "DOP" | "USD";
  gateway: PaymentGateway;
  description: string;
  metadata?: Record<string, string>;
  cardToken?: string;
  saveCard?: boolean;
}

export interface ChargeResponse {
  transactionId: string;
  externalId: string;
  status: TransactionStatus;
  amount: number;
  currency: string;
  gateway: PaymentGateway;
  gatewayResponse?: Record<string, any>;
  createdAt: string;
}

// ============================================================================
// REFUNDS
// ============================================================================

export interface RefundRequest {
  transactionId: string;
  amount?: number; // Partial refund
  reason: string;
}

// ============================================================================
// SUBSCRIPTIONS
// ============================================================================

export type SubscriptionStatus = "Active" | "Cancelled" | "Expired" | "PastDue";

export interface Subscription {
  id: string;
  userId: string;
  planId: string;
  planName: string;
  amount: number;
  currency: string;
  gateway: PaymentGateway;
  status: SubscriptionStatus;
  currentPeriodStart: string;
  currentPeriodEnd: string;
  cancelledAt?: string;
  createdAt: string;
}

export interface CreateSubscriptionRequest {
  planId: string;
  gateway: PaymentGateway;
  cardToken?: string;
}

// ============================================================================
// EXCHANGE RATES
// ============================================================================

export interface ExchangeRate {
  baseCurrency: string;
  targetCurrency: string;
  buyRate: number;
  sellRate: number;
  midRate: number;
  source: string;
  updatedAt: string;
}
```

---

## 📡 Service Layer

```typescript
// src/services/paymentService.ts
import { apiClient } from "./api-client";
import type {
  ProviderInfo,
  ProviderHealth,
  AllProvidersHealth,
  ChargeRequest,
  ChargeResponse,
  RefundRequest,
  Subscription,
  CreateSubscriptionRequest,
  ExchangeRate,
  PaymentGateway,
} from "@/types/payment";

class PaymentService {
  // ============================================================================
  // PROVIDERS
  // ============================================================================

  async getProviders(): Promise<{
    totalProviders: number;
    providers: ProviderInfo[];
  }> {
    const response = await apiClient.get("/api/payments/providers");
    return response.data;
  }

  async getProvider(gateway: PaymentGateway): Promise<ProviderInfo> {
    const response = await apiClient.get<ProviderInfo>(
      `/api/payments/providers/${gateway}`,
    );
    return response.data;
  }

  async checkProviderHealth(gateway: PaymentGateway): Promise<ProviderHealth> {
    const response = await apiClient.get<ProviderHealth>(
      `/api/payments/providers/${gateway}/health`,
    );
    return response.data;
  }

  async getAllProvidersHealth(): Promise<AllProvidersHealth> {
    const response = await apiClient.get<AllProvidersHealth>(
      "/api/payments/health",
    );
    return response.data;
  }

  // ============================================================================
  // CHARGES
  // ============================================================================

  async processCharge(request: ChargeRequest): Promise<ChargeResponse> {
    const response = await apiClient.post<ChargeResponse>(
      "/api/payments/charge",
      request,
    );
    return response.data;
  }

  async getTransaction(transactionId: string): Promise<ChargeResponse> {
    const response = await apiClient.get<ChargeResponse>(
      `/api/payments/${transactionId}`,
    );
    return response.data;
  }

  async processRefund(request: RefundRequest): Promise<ChargeResponse> {
    const response = await apiClient.post<ChargeResponse>(
      "/api/payments/refund",
      request,
    );
    return response.data;
  }

  // ============================================================================
  // SUBSCRIPTIONS
  // ============================================================================

  async getSubscriptions(): Promise<Subscription[]> {
    const response = await apiClient.get<Subscription[]>("/api/subscriptions");
    return response.data;
  }

  async createSubscription(
    request: CreateSubscriptionRequest,
  ): Promise<Subscription> {
    const response = await apiClient.post<Subscription>(
      "/api/subscriptions",
      request,
    );
    return response.data;
  }

  async cancelSubscription(id: string): Promise<void> {
    await apiClient.delete(`/api/subscriptions/${id}`);
  }

  // ============================================================================
  // EXCHANGE RATES
  // ============================================================================

  async getCurrentExchangeRate(): Promise<ExchangeRate> {
    const response = await apiClient.get<ExchangeRate>(
      "/api/exchangerates/current",
    );
    return response.data;
  }

  async getExchangeRateHistory(days: number = 30): Promise<ExchangeRate[]> {
    const response = await apiClient.get<ExchangeRate[]>(
      "/api/exchangerates/history",
      {
        params: { days },
      },
    );
    return response.data;
  }
}

export const paymentService = new PaymentService();
```

---

## 🎣 React Query Hooks

```typescript
// src/hooks/usePayments.ts
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { paymentService } from "@/services/paymentService";
import type {
  ChargeRequest,
  CreateSubscriptionRequest,
  PaymentGateway,
} from "@/types/payment";

export const paymentKeys = {
  all: ["payments"] as const,
  providers: () => [...paymentKeys.all, "providers"] as const,
  providerHealth: (gateway: PaymentGateway) =>
    [...paymentKeys.all, "health", gateway] as const,
  allHealth: () => [...paymentKeys.all, "all-health"] as const,
  transaction: (id: string) => [...paymentKeys.all, "transaction", id] as const,
  subscriptions: () => [...paymentKeys.all, "subscriptions"] as const,
  exchangeRate: () => [...paymentKeys.all, "exchange-rate"] as const,
};

export function usePaymentProviders() {
  return useQuery({
    queryKey: paymentKeys.providers(),
    queryFn: () => paymentService.getProviders(),
  });
}

export function useAllProvidersHealth() {
  return useQuery({
    queryKey: paymentKeys.allHealth(),
    queryFn: () => paymentService.getAllProvidersHealth(),
    refetchInterval: 60000, // Check every minute
  });
}

export function useProcessCharge() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: ChargeRequest) =>
      paymentService.processCharge(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: paymentKeys.subscriptions() });
    },
  });
}

export function useSubscriptions() {
  return useQuery({
    queryKey: paymentKeys.subscriptions(),
    queryFn: () => paymentService.getSubscriptions(),
  });
}

export function useCreateSubscription() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateSubscriptionRequest) =>
      paymentService.createSubscription(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: paymentKeys.subscriptions() });
    },
  });
}

export function useCurrentExchangeRate() {
  return useQuery({
    queryKey: paymentKeys.exchangeRate(),
    queryFn: () => paymentService.getCurrentExchangeRate(),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}
```

---

## 🎉 Resumen

✅ **17 Endpoints documentados**  
✅ **5 Proveedores de pago** (AZUL, CardNET, PixelPay, Fygaro, PayPal)  
✅ **NO Stripe** - Solo pasarelas locales de RD  
✅ **TypeScript Types** (Gateways, Charges, Subscriptions, ExchangeRates)  
✅ **Service Layer** (12 métodos)  
✅ **React Query Hooks** (6 hooks)

---

_Última actualización: Enero 30, 2026_
