# 💳 PaymentService - Documentación Frontend

> **Servicio:** PaymentService  
> **Puerto:** 5100 (dev) / 8080 (k8s)  
> **Estado:** ✅ Implementado  
> **Proveedores:** AZUL, CardNET, PixelPay, Fygaro, PayPal  
> **Última actualización:** Enero 2026

---

## 📋 Descripción

Servicio de pagos multi-proveedor que unifica la integración con múltiples pasarelas de pago dominicanas e internacionales. Proporciona una abstracción única que permite cambiar proveedores sin modificar el código del frontend.

---

## 🎯 Casos de Uso Frontend

### 1. Checkout de Suscripción Dealer

```typescript
// Pagar suscripción de dealer
const processSubscription = async (
  planId: string,
  paymentMethod: PaymentMethodData,
) => {
  const result = await paymentService.charge({
    gateway: "Azul", // Proveedor preferido
    amount: 4900, // $49.00 en centavos
    currency: "DOP",
    description: `Suscripción OKLA Plan ${planId}`,
    paymentMethod,
    metadata: { planId, dealerId: currentDealer.id },
  });

  if (result.success) {
    await subscriptionService.activate(planId);
    navigate("/dealer/dashboard");
  }
  return result;
};
```

### 2. Pago de Listing Individual

```typescript
// Vendedor paga para publicar vehículo
const payForListing = async (vehicleId: string) => {
  const result = await paymentService.charge({
    gateway: "PixelPay", // Menor comisión
    amount: 2900, // $29.00
    currency: "DOP",
    description: "Publicación de vehículo en OKLA",
    metadata: { vehicleId, userId: currentUser.id },
  });

  if (result.success) {
    await vehicleService.publish(vehicleId);
  }
  return result;
};
```

### 3. Selección de Pasarela

```typescript
// Mostrar opciones de pago al usuario
const PaymentMethodSelector = () => {
  const { data: providers } = usePaymentProviders();

  const availableProviders = providers?.filter(p => p.isConfigured);

  return (
    <RadioGroup>
      {availableProviders?.map(provider => (
        <RadioOption key={provider.gateway} value={provider.gateway}>
          <img src={`/icons/${provider.gateway.toLowerCase()}.svg`} />
          <span>{provider.name}</span>
        </RadioOption>
      ))}
    </RadioGroup>
  );
};
```

---

## 📡 API Endpoints

### Providers

| Método | Endpoint                                   | Descripción                    |
| ------ | ------------------------------------------ | ------------------------------ |
| `GET`  | `/api/payments/providers`                  | Listar proveedores disponibles |
| `GET`  | `/api/payments/providers/{gateway}`        | Info de proveedor              |
| `GET`  | `/api/payments/providers/{gateway}/health` | Verificar disponibilidad       |

### Payments

| Método | Endpoint                               | Descripción          |
| ------ | -------------------------------------- | -------------------- |
| `POST` | `/api/payments/charge`                 | Procesar cobro       |
| `GET`  | `/api/payments/{transactionId}`        | Obtener transacción  |
| `POST` | `/api/payments/{transactionId}/refund` | Reembolsar           |
| `GET`  | `/api/payments/user/{userId}`          | Historial de usuario |

### Subscriptions

| Método | Endpoint                                 | Descripción                  |
| ------ | ---------------------------------------- | ---------------------------- |
| `POST` | `/api/subscriptions`                     | Crear suscripción recurrente |
| `GET`  | `/api/subscriptions/{id}`                | Obtener suscripción          |
| `POST` | `/api/subscriptions/{id}/cancel`         | Cancelar suscripción         |
| `PUT`  | `/api/subscriptions/{id}/payment-method` | Cambiar método de pago       |

### Webhooks

| Método | Endpoint                  | Descripción            |
| ------ | ------------------------- | ---------------------- |
| `POST` | `/api/webhooks/{gateway}` | Recibir notificaciones |

---

## 🔧 Cliente TypeScript

```typescript
// services/paymentService.ts

import { apiClient } from "./apiClient";

// Enums
export type PaymentGateway =
  | "Azul"
  | "CardNET"
  | "PixelPay"
  | "Fygaro"
  | "PayPal";
export type PaymentStatus =
  | "Pending"
  | "Processing"
  | "Completed"
  | "Failed"
  | "Refunded"
  | "Cancelled";
export type Currency = "DOP" | "USD" | "EUR";

// Tipos
interface ChargeRequest {
  gateway: PaymentGateway;
  amount: number; // En centavos
  currency: Currency;
  description: string;
  paymentMethod: PaymentMethodData;
  metadata?: Record<string, any>;
  idempotencyKey?: string;
}

interface PaymentMethodData {
  type: "card";
  cardNumber: string;
  expiryMonth: string;
  expiryYear: string;
  cvv: string;
  cardholderName: string;
}

interface ChargeResponse {
  success: boolean;
  transactionId: string;
  status: PaymentStatus;
  amount: number;
  currency: Currency;
  gateway: PaymentGateway;
  authorizationCode?: string;
  errorCode?: string;
  errorMessage?: string;
  createdAt: string;
}

interface ProviderInfo {
  gateway: string;
  name: string;
  type: "Banking" | "Fintech" | "Aggregator";
  isConfigured: boolean;
  configurationErrors: string[];
}

interface ProviderHealth {
  gateway: string;
  name: string;
  isAvailable: boolean;
  checkedAt: string;
}

interface Transaction {
  id: string;
  userId: string;
  gateway: PaymentGateway;
  amount: number;
  currency: Currency;
  status: PaymentStatus;
  description: string;
  authorizationCode?: string;
  metadata?: Record<string, any>;
  createdAt: string;
  updatedAt: string;
}

interface RefundRequest {
  amount?: number; // Parcial, si no se especifica es total
  reason: string;
}

interface RefundResponse {
  success: boolean;
  refundId: string;
  amount: number;
  status: "Pending" | "Completed" | "Failed";
}

export const paymentService = {
  // === PROVIDERS ===

  async getProviders(): Promise<{
    totalProviders: number;
    providers: ProviderInfo[];
  }> {
    const response = await apiClient.get("/api/payments/providers");
    return response.data;
  },

  async getProvider(gateway: PaymentGateway): Promise<ProviderInfo> {
    const response = await apiClient.get(`/api/payments/providers/${gateway}`);
    return response.data;
  },

  async checkProviderHealth(gateway: PaymentGateway): Promise<ProviderHealth> {
    const response = await apiClient.get(
      `/api/payments/providers/${gateway}/health`,
    );
    return response.data;
  },

  // === PAYMENTS ===

  async charge(request: ChargeRequest): Promise<ChargeResponse> {
    // Agregar idempotency key para evitar cobros duplicados
    const idempotencyKey =
      request.idempotencyKey ||
      `charge_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;

    const response = await apiClient.post("/api/payments/charge", request, {
      headers: { "Idempotency-Key": idempotencyKey },
    });
    return response.data;
  },

  async getTransaction(transactionId: string): Promise<Transaction> {
    const response = await apiClient.get(`/api/payments/${transactionId}`);
    return response.data;
  },

  async refund(
    transactionId: string,
    request: RefundRequest,
  ): Promise<RefundResponse> {
    const response = await apiClient.post(
      `/api/payments/${transactionId}/refund`,
      request,
    );
    return response.data;
  },

  async getUserTransactions(
    userId: string,
    params?: {
      page?: number;
      pageSize?: number;
      status?: PaymentStatus;
    },
  ): Promise<{ items: Transaction[]; totalCount: number }> {
    const response = await apiClient.get(`/api/payments/user/${userId}`, {
      params,
    });
    return response.data;
  },

  // === SUBSCRIPTIONS ===

  async createSubscription(data: {
    gateway: PaymentGateway;
    planId: string;
    paymentMethod: PaymentMethodData;
  }): Promise<{ subscriptionId: string; status: string }> {
    const response = await apiClient.post("/api/subscriptions", data);
    return response.data;
  },

  async cancelSubscription(
    subscriptionId: string,
  ): Promise<{ success: boolean }> {
    const response = await apiClient.post(
      `/api/subscriptions/${subscriptionId}/cancel`,
    );
    return response.data;
  },

  // === HELPERS ===

  formatAmount(amount: number, currency: Currency): string {
    const value = amount / 100;
    const symbols = { DOP: "RD$", USD: "$", EUR: "€" };
    return `${symbols[currency]}${value.toLocaleString("es-DO", { minimumFractionDigits: 2 })}`;
  },

  getProviderRecommendation(amount: number): PaymentGateway {
    // Recomendar proveedor según monto
    if (amount > 500000) return "PixelPay"; // Menor comisión para montos altos
    return "Azul"; // Más confiable para montos menores
  },
};
```

---

## 🪝 Hooks de React

```typescript
// hooks/usePayment.ts

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  paymentService,
  ChargeRequest,
  PaymentGateway,
} from "../services/paymentService";

export function usePaymentProviders() {
  return useQuery({
    queryKey: ["payment-providers"],
    queryFn: () => paymentService.getProviders(),
    staleTime: 5 * 60 * 1000,
  });
}

export function useProviderHealth(gateway: PaymentGateway) {
  return useQuery({
    queryKey: ["provider-health", gateway],
    queryFn: () => paymentService.checkProviderHealth(gateway),
    refetchInterval: 30000, // Verificar cada 30s
  });
}

export function useCharge() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: ChargeRequest) => paymentService.charge(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["user-transactions"] });
    },
  });
}

export function useUserTransactions(userId: string) {
  return useQuery({
    queryKey: ["user-transactions", userId],
    queryFn: () => paymentService.getUserTransactions(userId),
    enabled: !!userId,
  });
}

export function useRefund() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      transactionId,
      reason,
      amount,
    }: {
      transactionId: string;
      reason: string;
      amount?: number;
    }) => paymentService.refund(transactionId, { reason, amount }),
    onSuccess: (_, { transactionId }) => {
      queryClient.invalidateQueries({
        queryKey: ["transaction", transactionId],
      });
      queryClient.invalidateQueries({ queryKey: ["user-transactions"] });
    },
  });
}
```

---

## 🧩 Componente de Checkout

```tsx
// components/CheckoutForm.tsx

import { useState } from "react";
import { useCharge, usePaymentProviders } from "../hooks/usePayment";
import { paymentService, PaymentGateway } from "../services/paymentService";

interface CheckoutFormProps {
  amount: number;
  currency: "DOP" | "USD";
  description: string;
  onSuccess: (transactionId: string) => void;
  onError: (error: string) => void;
}

export function CheckoutForm({
  amount,
  currency,
  description,
  onSuccess,
  onError,
}: CheckoutFormProps) {
  const { data: providers } = usePaymentProviders();
  const charge = useCharge();

  const [selectedGateway, setSelectedGateway] =
    useState<PaymentGateway>("Azul");
  const [cardData, setCardData] = useState({
    cardNumber: "",
    expiryMonth: "",
    expiryYear: "",
    cvv: "",
    cardholderName: "",
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      const result = await charge.mutateAsync({
        gateway: selectedGateway,
        amount,
        currency,
        description,
        paymentMethod: {
          type: "card",
          ...cardData,
        },
      });

      if (result.success) {
        onSuccess(result.transactionId);
      } else {
        onError(result.errorMessage || "Error procesando pago");
      }
    } catch (error: any) {
      onError(error.message || "Error de conexión");
    }
  };

  const availableProviders =
    providers?.providers.filter((p) => p.isConfigured) || [];

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* Monto */}
      <div className="text-center py-4 bg-gray-50 rounded-lg">
        <p className="text-sm text-gray-600">Total a pagar</p>
        <p className="text-3xl font-bold">
          {paymentService.formatAmount(amount, currency)}
        </p>
      </div>

      {/* Selector de pasarela */}
      <div>
        <label className="block text-sm font-medium mb-2">Método de pago</label>
        <div className="grid grid-cols-2 gap-3">
          {availableProviders.map((provider) => (
            <button
              key={provider.gateway}
              type="button"
              onClick={() =>
                setSelectedGateway(provider.gateway as PaymentGateway)
              }
              className={`p-3 border rounded-lg flex items-center gap-2 ${
                selectedGateway === provider.gateway
                  ? "border-blue-500 bg-blue-50"
                  : "border-gray-200"
              }`}
            >
              <img
                src={`/icons/${provider.gateway.toLowerCase()}.svg`}
                alt={provider.name}
                className="w-8 h-8"
              />
              <span className="font-medium">{provider.name}</span>
            </button>
          ))}
        </div>
      </div>

      {/* Datos de tarjeta */}
      <div className="space-y-4">
        <Input
          label="Número de tarjeta"
          value={cardData.cardNumber}
          onChange={(e) =>
            setCardData((prev) => ({ ...prev, cardNumber: e.target.value }))
          }
          placeholder="4111 1111 1111 1111"
          maxLength={19}
        />

        <div className="grid grid-cols-3 gap-4">
          <Input
            label="Mes"
            value={cardData.expiryMonth}
            onChange={(e) =>
              setCardData((prev) => ({ ...prev, expiryMonth: e.target.value }))
            }
            placeholder="12"
            maxLength={2}
          />
          <Input
            label="Año"
            value={cardData.expiryYear}
            onChange={(e) =>
              setCardData((prev) => ({ ...prev, expiryYear: e.target.value }))
            }
            placeholder="26"
            maxLength={2}
          />
          <Input
            label="CVV"
            value={cardData.cvv}
            onChange={(e) =>
              setCardData((prev) => ({ ...prev, cvv: e.target.value }))
            }
            placeholder="123"
            maxLength={4}
            type="password"
          />
        </div>

        <Input
          label="Nombre en la tarjeta"
          value={cardData.cardholderName}
          onChange={(e) =>
            setCardData((prev) => ({ ...prev, cardholderName: e.target.value }))
          }
          placeholder="JUAN PEREZ"
        />
      </div>

      <Button type="submit" className="w-full" disabled={charge.isPending}>
        {charge.isPending
          ? "Procesando..."
          : `Pagar ${paymentService.formatAmount(amount, currency)}`}
      </Button>

      <p className="text-xs text-gray-500 text-center">
        🔒 Pago seguro procesado por {selectedGateway}
      </p>
    </form>
  );
}
```

---

## 📊 Proveedores Disponibles

| Proveedor    | Tipo       | Comisión   | Uso Recomendado       |
| ------------ | ---------- | ---------- | --------------------- |
| **AZUL**     | Banking    | 2.9%-4.5%  | Principal (doméstico) |
| **CardNET**  | Banking    | 2.5%-4.5%  | Backup                |
| **PixelPay** | Fintech    | 1.0%-3.5%  | Alto volumen          |
| **Fygaro**   | Aggregator | Variable   | Suscripciones         |
| **PayPal**   | Fintech    | 2.9%+$0.30 | Internacional         |

---

## 🧪 Testing

### E2E Test (Playwright)

```typescript
// e2e/payment.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Payment Flow", () => {
  test("should complete checkout", async ({ page }) => {
    await page.goto("/checkout?plan=pro");

    // Seleccionar pasarela
    await page.click('[data-testid="gateway-azul"]');

    // Llenar datos de tarjeta (test card)
    await page.fill('[name="cardNumber"]', "4111111111111111");
    await page.fill('[name="expiryMonth"]', "12");
    await page.fill('[name="expiryYear"]', "26");
    await page.fill('[name="cvv"]', "123");
    await page.fill('[name="cardholderName"]', "TEST USER");

    await page.click('[data-testid="submit-payment"]');

    await expect(page.locator('[data-testid="success-message"]')).toBeVisible();
  });
});
```

---

## 🔗 Referencias

- [README PaymentService](../../../backend/PaymentService/README.md)
- [StripePaymentService](../../../backend/StripePaymentService/)
- [Billing API](../05-API-INTEGRATION/18-billing-api.md)

---

_El servicio soporta idempotencia para evitar cobros duplicados. Siempre usar idempotency keys._
