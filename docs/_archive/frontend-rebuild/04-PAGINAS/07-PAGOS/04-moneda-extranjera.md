---
title: "💱 34 - Pagos en Moneda Extranjera"
priority: P1
estimated_time: "2 horas"
dependencies: []
apis: []
status: partial
last_updated: "2026-01-30"
---

# 💱 34 - Pagos en Moneda Extranjera

> **Sprint:** 4 (Pagos y Facturación)  
> **Prioridad:** P1 - Alta  
> **Roles:** User, Dealer, Admin  
> **Proceso Matrix:** [07-foreign-currency-payments.md](../../process-matrix/05-PAGOS-FACTURACION/07-foreign-currency-payments.md)  
> **Backend:** PaymentService (Puerto 15105)

---

## 📑 Tabla de Contenidos

1. [Resumen](#-resumen)
2. [Marco Legal](#-marco-legal)
3. [Componentes UI](#-componentes-ui)
4. [Flujos de Usuario](#-flujos-de-usuario)
5. [Integración API](#-integración-api)
6. [Testing](#-testing)
7. [Checklist](#-checklist-de-implementación)

---

## 📋 Resumen

Sistema de pagos en moneda extranjera (USD, EUR) cumpliendo con los requisitos fiscales de la DGII.

| Funcionalidad             | Descripción                    |
| ------------------------- | ------------------------------ |
| **Multi-Moneda**          | Soporta DOP, USD, EUR          |
| **Tasa BCRD**             | Tasa oficial del Banco Central |
| **Conversión Automática** | Factura siempre en DOP (DGII)  |
| **Auditoría**             | Registro de cada conversión    |

### Flujo de Conversión

```
┌────────────────────────────────────────────────────────────────────────────┐
│                         FLUJO USD/EUR → DOP                                │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│  1️⃣ Cliente selecciona pagar en USD                                        │
│     └─> $100 USD                                                           │
│                                                                            │
│  2️⃣ Sistema obtiene tasa BCRD                                              │
│     └─> Tasa de compra: 58.50 DOP/USD                                      │
│                                                                            │
│  3️⃣ Conversión y cálculo ITBIS                                             │
│     ├─> Subtotal DOP: $100 × 58.50 = 5,850.00 DOP                          │
│     ├─> ITBIS 18%: 1,053.00 DOP                                            │
│     └─> Total: 6,903.00 DOP                                                │
│                                                                            │
│  4️⃣ Cobro al cliente                                                       │
│     └─> Se cobra $100 USD (moneda original)                                │
│                                                                            │
│  5️⃣ NCF emitido en DOP                                                     │
│     └─> Factura con referencia a tasa aplicada                             │
│                                                                            │
└────────────────────────────────────────────────────────────────────────────┘
```

---

## ⚖️ Marco Legal

### Requisitos DGII

1. **Facturación en DOP**: Todas las facturas (NCF) deben emitirse en Pesos Dominicanos
2. **Tasa Oficial BCRD**: Usar la tasa publicada por el Banco Central
3. **Registro de Conversión**: Documentar la tasa para cada transacción
4. **ITBIS 18%**: Calcular sobre el monto convertido a DOP
5. **Formato 607**: Reportar ventas en DOP en el reporte mensual

### Fuente de Tasas

- **Primaria**: API del Banco Central de la República Dominicana
- **URL**: https://api.bancentral.gov.do/
- **Actualización**: Diaria a las 8:30 AM (hora RD)

---

## 🧩 Componentes UI

### 1. CurrencySelector

Selector de moneda en el checkout.

```tsx
// src/components/payments/CurrencySelector.tsx

import { useState } from "react";
import {
  Currency,
  getCurrencySymbol,
  getCurrencyFlag,
  getCurrencyName,
} from "@/services/exchangeRateService";
import { useAllCurrentRates } from "@/hooks/useExchangeRate";
import { cn } from "@/lib/utils";

interface CurrencySelectorProps {
  value: Currency;
  onChange: (currency: Currency) => void;
  amount?: number; // Monto original para mostrar conversión
  disabled?: boolean;
}

export function CurrencySelector({
  value,
  onChange,
  amount,
  disabled,
}: CurrencySelectorProps) {
  const { data: rates, isLoading } = useAllCurrentRates();

  const currencies = [
    { currency: Currency.DOP, rate: 1 },
    {
      currency: Currency.USD,
      rate: rates?.find((r) => r.sourceCurrency === Currency.USD)?.buyRate || 0,
    },
    {
      currency: Currency.EUR,
      rate: rates?.find((r) => r.sourceCurrency === Currency.EUR)?.buyRate || 0,
    },
  ];

  return (
    <div className="space-y-3">
      <label className="block text-sm font-medium text-gray-700">
        Moneda de Pago
      </label>

      <div className="grid grid-cols-3 gap-3">
        {currencies.map(({ currency, rate }) => (
          <button
            key={currency}
            type="button"
            disabled={disabled || isLoading}
            onClick={() => onChange(currency)}
            className={cn(
              "flex flex-col items-center p-4 border-2 rounded-lg transition-all",
              value === currency
                ? "border-blue-500 bg-blue-50"
                : "border-gray-200 hover:border-gray-300",
              disabled && "opacity-50 cursor-not-allowed",
            )}
          >
            <span className="text-2xl mb-1">{getCurrencyFlag(currency)}</span>
            <span className="font-semibold">{getCurrencySymbol(currency)}</span>
            <span className="text-xs text-gray-500">
              {getCurrencyName(currency)}
            </span>
          </button>
        ))}
      </div>

      {/* Mostrar tasa actual si no es DOP */}
      {value !== Currency.DOP && (
        <div className="bg-gray-50 rounded-lg p-3 text-sm">
          <div className="flex justify-between">
            <span className="text-gray-600">Tasa BCRD:</span>
            <span className="font-medium">
              1 {value} ={" "}
              {currencies.find((c) => c.currency === value)?.rate.toFixed(2)}{" "}
              DOP
            </span>
          </div>
          <p className="text-xs text-gray-400 mt-1">
            Actualizado hoy a las 8:30 AM
          </p>
        </div>
      )}
    </div>
  );
}
```

### 2. ExchangeRateDisplay

Muestra la tasa de cambio actual.

```tsx
// src/components/payments/ExchangeRateDisplay.tsx

import { Currency } from "@/services/exchangeRateService";
import { useCurrentRate } from "@/hooks/useExchangeRate";
import { FiRefreshCw, FiTrendingUp, FiTrendingDown } from "react-icons/fi";

interface ExchangeRateDisplayProps {
  currency: Currency;
  showHistory?: boolean;
}

export function ExchangeRateDisplay({
  currency,
  showHistory = false,
}: ExchangeRateDisplayProps) {
  const { data: rate, isLoading, refetch } = useCurrentRate(currency);

  if (currency === Currency.DOP) return null;
  if (isLoading) return <ExchangeRateSkeleton />;
  if (!rate) return null;

  return (
    <div className="bg-gradient-to-r from-blue-50 to-indigo-50 border border-blue-200 rounded-lg p-4">
      <div className="flex justify-between items-start">
        <div>
          <div className="flex items-center gap-2">
            <span className="text-2xl">{getCurrencyFlag(currency)}</span>
            <h4 className="font-semibold text-lg">
              1 {currency} = {rate.buyRate.toFixed(2)} DOP
            </h4>
          </div>
          <p className="text-sm text-gray-600 mt-1">
            Tasa de compra del Banco Central RD
          </p>
        </div>

        <div className="text-right">
          <p className="text-xs text-gray-500">{formatDate(rate.rateDate)}</p>
          <button
            onClick={() => refetch()}
            className="text-blue-600 hover:text-blue-800 text-sm flex items-center gap-1 mt-1"
          >
            <FiRefreshCw className="w-3 h-3" />
            Actualizar
          </button>
        </div>
      </div>

      {/* Compra vs Venta */}
      <div className="grid grid-cols-2 gap-4 mt-4 pt-4 border-t border-blue-200">
        <div>
          <p className="text-xs text-gray-500">Tasa de Compra</p>
          <p className="font-semibold text-green-600">
            {rate.buyRate.toFixed(4)}
          </p>
        </div>
        <div>
          <p className="text-xs text-gray-500">Tasa de Venta</p>
          <p className="font-semibold text-red-600">
            {rate.sellRate.toFixed(4)}
          </p>
        </div>
      </div>
    </div>
  );
}
```

### 3. CurrencyConversionSummary

Resumen de conversión en el checkout.

```tsx
// src/components/payments/CurrencyConversionSummary.tsx

import {
  Currency,
  formatAmount,
  calculateItbis,
  calculateTotalWithItbis,
} from "@/services/exchangeRateService";
import { useConversionQuote } from "@/hooks/useExchangeRate";

interface CurrencyConversionSummaryProps {
  amount: number;
  currency: Currency;
}

export function CurrencyConversionSummary({
  amount,
  currency,
}: CurrencyConversionSummaryProps) {
  const { data: quote, isLoading } = useConversionQuote(amount, currency);

  if (currency === Currency.DOP) {
    // Mostrar cálculo directo en DOP
    const itbis = calculateItbis(amount);
    const total = calculateTotalWithItbis(amount);

    return (
      <div className="bg-gray-50 rounded-lg p-4 space-y-2">
        <div className="flex justify-between">
          <span className="text-gray-600">Subtotal</span>
          <span>{formatAmount(amount, Currency.DOP)}</span>
        </div>
        <div className="flex justify-between">
          <span className="text-gray-600">ITBIS (18%)</span>
          <span>{formatAmount(itbis, Currency.DOP)}</span>
        </div>
        <div className="flex justify-between font-bold text-lg border-t pt-2">
          <span>Total a Pagar</span>
          <span>{formatAmount(total, Currency.DOP)}</span>
        </div>
      </div>
    );
  }

  if (isLoading) return <ConversionSummarySkeleton />;
  if (!quote) return null;

  return (
    <div className="bg-gray-50 rounded-lg p-4 space-y-3">
      {/* Monto original */}
      <div className="flex justify-between">
        <span className="text-gray-600">Monto en {currency}</span>
        <span className="font-medium">{formatAmount(amount, currency)}</span>
      </div>

      {/* Tasa aplicada */}
      <div className="flex justify-between text-sm">
        <span className="text-gray-500">Tasa BCRD ({quote.rateDate})</span>
        <span className="text-gray-500">×{quote.appliedRate.toFixed(4)}</span>
      </div>

      <div className="border-t pt-3 space-y-2">
        {/* Subtotal en DOP */}
        <div className="flex justify-between">
          <span className="text-gray-600">Subtotal (DOP)</span>
          <span>{formatAmount(quote.convertedAmountDop, Currency.DOP)}</span>
        </div>

        {/* ITBIS */}
        <div className="flex justify-between">
          <span className="text-gray-600">ITBIS (18%)</span>
          <span>{formatAmount(quote.itbisDop, Currency.DOP)}</span>
        </div>

        {/* Total */}
        <div className="flex justify-between font-bold text-lg border-t pt-2">
          <span>Total (DOP)</span>
          <span>{formatAmount(quote.totalWithItbisDop, Currency.DOP)}</span>
        </div>
      </div>

      {/* Nota informativa */}
      <div className="bg-blue-50 border border-blue-200 rounded p-2 text-xs text-blue-700">
        <strong>Nota:</strong> Se te cobrará {formatAmount(amount, currency)} en
        tu tarjeta. La factura NCF se emitirá en DOP según normativa DGII.
      </div>

      {/* Validez de la cotización */}
      <p className="text-xs text-gray-400 text-center">
        Cotización válida hasta {formatTime(quote.validUntil)}
      </p>
    </div>
  );
}
```

### 4. Integración en CheckoutPage

Cómo integrar los componentes en el checkout.

```tsx
// En CheckoutPage.tsx

import { useState } from "react";
import { Currency } from "@/services/exchangeRateService";
import { CurrencySelector } from "@/components/payments/CurrencySelector";
import { ExchangeRateDisplay } from "@/components/payments/ExchangeRateDisplay";
import { CurrencyConversionSummary } from "@/components/payments/CurrencyConversionSummary";

export function CheckoutPage() {
  const [selectedCurrency, setSelectedCurrency] = useState<Currency>(
    Currency.DOP,
  );
  const amount = 29; // Precio del listing en USD base

  return (
    <div className="grid lg:grid-cols-3 gap-8">
      {/* Formulario de pago */}
      <div className="lg:col-span-2">
        {/* ... otros campos ... */}

        {/* Selector de moneda */}
        <CurrencySelector
          value={selectedCurrency}
          onChange={setSelectedCurrency}
          amount={amount}
        />

        {/* Mostrar tasa si es moneda extranjera */}
        {selectedCurrency !== Currency.DOP && (
          <ExchangeRateDisplay currency={selectedCurrency} />
        )}

        {/* Formulario de tarjeta */}
        <CreditCardForm />
      </div>

      {/* Resumen */}
      <div className="lg:col-span-1">
        <div className="sticky top-4">
          <h3 className="font-semibold mb-4">Resumen de Compra</h3>

          <CurrencyConversionSummary
            amount={amount}
            currency={selectedCurrency}
          />

          <Button className="w-full mt-4" onClick={handlePayment}>
            Pagar {formatAmount(amount, selectedCurrency)}
          </Button>
        </div>
      </div>
    </div>
  );
}
```

---

## 🔌 Integración API

### Endpoints

| Método | Endpoint                                | Auth | Descripción             |
| ------ | --------------------------------------- | ---- | ----------------------- |
| `GET`  | `/api/exchangerates/current/{currency}` | ❌   | Tasa actual USD/EUR     |
| `GET`  | `/api/exchangerates/current`            | ❌   | Todas las tasas         |
| `GET`  | `/api/exchangerates/quote`              | ❌   | Cotización sin registro |
| `POST` | `/api/exchangerates/convert`            | ✅   | Convertir con registro  |
| `GET`  | `/api/exchangerates/history/{currency}` | ❌   | Historial de tasas      |
| `POST` | `/api/exchangerates/refresh`            | ✅   | Forzar actualización    |

### Hooks React Query

```typescript
// Hooks disponibles en @/hooks/useExchangeRate.ts

useCurrentRate(currency); // Tasa actual de una moneda
useAllCurrentRates(); // Todas las tasas (USD, EUR)
useRateHistory(currency); // Historial de tasas
useConversionQuote(amount, curr); // Cotización sin registro
useConvertToDop(); // Convertir con registro
useRefreshRates(); // Admin: forzar actualización
useCurrencySelection(); // Hook combinado para checkout
```

---

## 🧪 Testing

### Escenarios de Prueba

| Escenario      | Datos                | Resultado Esperado            |
| -------------- | -------------------- | ----------------------------- |
| Pago en DOP    | $1,000 DOP           | Total: $1,180 DOP (con ITBIS) |
| Pago en USD    | $100 USD, tasa 58.50 | Total: 6,903 DOP              |
| Pago en EUR    | €100 EUR, tasa 62.30 | Total: 7,351.40 DOP           |
| Tasa expirada  | Simular rate > 24h   | Debe refrescar automático     |
| API BCRD caída | Simular timeout      | Usar fallback                 |

---

## ✅ Checklist de Implementación

### Backend ✅

- [x] ExchangeRateService
- [x] API Banco Central integration
- [x] CurrencyConversion entity (auditoría)
- [x] Background job para actualizar tasas
- [x] Endpoints REST

### Frontend 🚧

- [ ] CurrencySelector component
- [ ] ExchangeRateDisplay component
- [ ] CurrencyConversionSummary component
- [ ] Integración en CheckoutPage
- [ ] React Query hooks (useExchangeRate.ts)
- [ ] Caché local de tasas

### Integración 🚧

- [ ] Mostrar moneda en CheckoutSummary
- [ ] Persistir preferencia de moneda
- [ ] Error handling si API BCRD falla

---

## 🎨 Estados de UI

### Loading State

```typescript
export function CurrencySelectorSkeleton() {
  return (
    <div className="space-y-2">
      <Skeleton className="h-4 w-24" />
      <div className="flex gap-2">
        {[1, 2, 3].map((i) => (
          <Skeleton key={i} className="h-10 w-24 rounded-lg" />
        ))}
      </div>
    </div>
  );
}

export function ExchangeRateSkeleton() {
  return (
    <div className="bg-blue-50 rounded-lg p-4 space-y-2">
      <Skeleton className="h-4 w-32" />
      <Skeleton className="h-6 w-48" />
      <Skeleton className="h-3 w-40" />
    </div>
  );
}
```

### Error State

```typescript
export function ExchangeRateError({ onRetry }: { onRetry: () => void }) {
  return (
    <div className="bg-amber-50 border border-amber-200 rounded-lg p-4">
      <div className="flex items-start gap-3">
        <AlertTriangle className="text-amber-500 flex-shrink-0 mt-0.5" size={20} />
        <div className="flex-1">
          <h4 className="font-medium text-amber-800">
            Error al obtener tasas de cambio
          </h4>
          <p className="text-sm text-amber-700 mt-1">
            No pudimos conectar con el Banco Central. Por favor intenta de nuevo.
          </p>
          <Button
            variant="outline"
            size="sm"
            className="mt-2"
            onClick={onRetry}
          >
            <RefreshCw size={14} className="mr-1" />
            Reintentar
          </Button>
        </div>
      </div>
    </div>
  );
}
```

### Fallback State (Tasa no disponible)

```typescript
export function ExchangeRateFallback() {
  return (
    <div className="bg-gray-50 border border-gray-200 rounded-lg p-4">
      <div className="flex items-center gap-2 text-gray-600">
        <Info size={16} />
        <span className="text-sm">
          Pagos en moneda extranjera temporalmente no disponibles.
          Por favor paga en DOP.
        </span>
      </div>
    </div>
  );
}
```

---

## 🧪 Testing E2E

```typescript
// filepath: e2e/payments/foreign-currency.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsUser, setupCheckout } from "../helpers";

test.describe("Foreign Currency Payments", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
    await setupCheckout(page, { productPrice: 10000 }); // 10,000 DOP
  });

  test("should display currency selector", async ({ page }) => {
    await page.goto("/checkout");

    await expect(page.getByTestId("currency-selector")).toBeVisible();
    await expect(page.getByText("DOP")).toBeVisible();
    await expect(page.getByText("USD")).toBeVisible();
    await expect(page.getByText("EUR")).toBeVisible();
  });

  test("should show DOP as default currency", async ({ page }) => {
    await page.goto("/checkout");

    const dopOption = page.getByTestId("currency-option-DOP");
    await expect(dopOption).toHaveClass(/selected|active/);
  });

  test("should switch to USD and show conversion", async ({ page }) => {
    await page.goto("/checkout");

    await page.getByTestId("currency-option-USD").click();

    // Should show exchange rate info
    await expect(page.getByTestId("exchange-rate-display")).toBeVisible();
    await expect(page.getByText(/Tasa:/)).toBeVisible();

    // Should show converted amount
    await expect(page.getByText(/\$\d+\.\d{2} USD/)).toBeVisible();
  });

  test("should show ITBIS calculated on DOP amount", async ({ page }) => {
    await page.goto("/checkout");
    await page.getByTestId("currency-option-USD").click();

    // ITBIS should be calculated on DOP equivalent
    await expect(page.getByText("ITBIS")).toBeVisible();
    await expect(page.getByText(/DOP$/)).toBeVisible();
  });

  test("should show rate source and timestamp", async ({ page }) => {
    await page.goto("/checkout");
    await page.getByTestId("currency-option-USD").click();

    await expect(page.getByText(/Banco Central/)).toBeVisible();
    await expect(page.getByText(/Actualizado:/)).toBeVisible();
  });

  test("should switch between currencies correctly", async ({ page }) => {
    await page.goto("/checkout");

    // Switch to USD
    await page.getByTestId("currency-option-USD").click();
    await expect(page.getByText(/USD/)).toBeVisible();

    // Switch to EUR
    await page.getByTestId("currency-option-EUR").click();
    await expect(page.getByText(/EUR/)).toBeVisible();

    // Back to DOP
    await page.getByTestId("currency-option-DOP").click();
    await expect(page.getByTestId("exchange-rate-display")).not.toBeVisible();
  });

  test("should handle rate refresh error gracefully", async ({ page }) => {
    // Mock API to fail
    await page.route("**/api/exchangerates/**", (route) => {
      route.abort();
    });

    await page.goto("/checkout");
    await page.getByTestId("currency-option-USD").click();

    await expect(page.getByText(/Error al obtener tasas/)).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Reintentar" }),
    ).toBeVisible();
  });

  test("should complete payment in USD", async ({ page }) => {
    await page.goto("/checkout");

    // Select USD
    await page.getByTestId("currency-option-USD").click();

    // Fill payment info
    await page.getByLabel("Número de tarjeta").fill("4242424242424242");
    await page.getByLabel("Vencimiento").fill("12/30");
    await page.getByLabel("CVV").fill("123");

    // Complete payment
    await page.getByRole("button", { name: /Pagar/ }).click();

    // Should show success
    await expect(page.getByText(/Pago exitoso/)).toBeVisible();
  });

  test("should show conversion record reference", async ({ page }) => {
    await page.goto("/checkout");
    await page.getByTestId("currency-option-USD").click();

    // Complete payment
    await page.getByLabel("Número de tarjeta").fill("4242424242424242");
    await page.getByLabel("Vencimiento").fill("12/30");
    await page.getByLabel("CVV").fill("123");
    await page.getByRole("button", { name: /Pagar/ }).click();

    // Confirmation should show conversion details
    await expect(page.getByText(/Tasa aplicada:/)).toBeVisible();
    await expect(page.getByText(/Monto original:/)).toBeVisible();
    await expect(page.getByText(/Monto facturado:/)).toBeVisible();
  });
});
```

---

## 📊 Analytics Events

```typescript
// filepath: src/lib/analytics/currencyEvents.ts
import { analytics } from "@/lib/analytics";

export const currencyEvents = {
  // Currency selection
  selectCurrency: (currency: "DOP" | "USD" | "EUR", amount: number) => {
    analytics.track("checkout_currency_selected", { currency, amount });
  },

  // Rate display
  viewExchangeRate: (currency: string, rate: number) => {
    analytics.track("exchange_rate_viewed", { currency, rate });
  },

  // Rate refresh
  refreshRate: (currency: string, success: boolean) => {
    analytics.track("exchange_rate_refreshed", { currency, success });
  },

  // Conversion completed
  conversionCompleted: (
    originalCurrency: string,
    originalAmount: number,
    dopAmount: number,
    rate: number,
  ) => {
    analytics.track("currency_conversion_completed", {
      originalCurrency,
      originalAmount,
      dopAmount,
      rate,
    });
  },

  // Error tracking
  rateError: (currency: string, errorType: string) => {
    analytics.track("exchange_rate_error", { currency, errorType });
  },
};
```

---

## 📝 Notas de Implementación

### Cache Strategy

```typescript
// Redis cache for exchange rates
const CACHE_KEY = "exchange_rates:current";
const CACHE_TTL = 3600; // 1 hour

// Fallback rates in case BCRD API is down
const FALLBACK_RATES = {
  USD: { buy: 58.5, sell: 59.5, timestamp: "manual" },
  EUR: { buy: 62.3, sell: 63.5, timestamp: "manual" },
};
```

### BCRD API Integration

```typescript
// Service configuration
const BCRD_API_URL = "https://api.bancentral.gov.do/";
const BCRD_API_KEY = process.env.BCRD_API_KEY;
const UPDATE_SCHEDULE = "0 8,12,16 * * *"; // 8 AM, 12 PM, 4 PM
```

### DGII Compliance

- Todas las facturas (NCF) se emiten en DOP
- Se registra la tasa de conversión aplicada
- Se guarda referencia al monto original en moneda extranjera
- El ITBIS (18%) se calcula sobre el monto en DOP

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/moneda-extranjera.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsUser } from "../helpers/auth";

test.describe("Moneda Extranjera", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test("debe mostrar selector de moneda", async ({ page }) => {
    await page.goto("/checkout");

    await expect(page.getByTestId("currency-selector")).toBeVisible();
  });

  test("debe mostrar tasa de cambio actual", async ({ page }) => {
    await page.goto("/checkout");
    await page.getByTestId("currency-selector").click();
    await page.getByRole("option", { name: /usd/i }).click();

    await expect(page.getByTestId("exchange-rate")).toBeVisible();
  });

  test("debe convertir precios a moneda seleccionada", async ({ page }) => {
    await page.goto("/checkout");
    await page.getByTestId("currency-selector").click();
    await page.getByRole("option", { name: /usd/i }).click();

    await expect(page.getByTestId("price-usd")).toBeVisible();
    await expect(page.getByTestId("price-dop")).toBeVisible();
  });

  test("debe mostrar factura en DOP", async ({ page }) => {
    await page.goto("/billing/invoice/123");

    await expect(page.getByText(/DOP/)).toBeVisible();
  });
});
```

---

**Última actualización:** Enero 29, 2026  
**Autor:** OKLA Team  
**Versión:** 1.1.0
