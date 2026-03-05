# 💳 Análisis de Gap - PAGOS Y FACTURACIÓN

> **Dominio:** 05-PAGOS-FACTURACION  
> **Fecha:** Enero 28, 2026  
> **Archivos Analizados:** 10 (1 descartado)  
> **Total Líneas:** ~5,159 líneas

---

## 📊 Resumen Ejecutivo

| Métrica                     | Valor               |
| --------------------------- | ------------------- |
| **Archivos Process-Matrix** | 10 (+ 1 descartado) |
| **Proveedores de Pago**     | 5 activos           |
| **Servicios Core**          | 4                   |
| **Coverage Backend**        | 95%                 |
| **Coverage Frontend**       | 75% ⚠️              |
| **Gaps Identificados**      | 6 features          |

---

## 🗂️ Inventario de Archivos

### Analizados ✅

| Archivo                 | Líneas | Backend | Frontend   | Prioridad |
| ----------------------- | ------ | ------- | ---------- | --------- |
| 01-billing-service.md   | 1,110  | 100%    | 95%        | P0        |
| 03-azul-payment.md      | 794    | 100%    | 95%        | P0        |
| 04-cardnet-payment.md   | 382    | 100%    | 100%       | P1        |
| 04-invoicing-service.md | 730    | 60%     | **0%** ⚠️  | P0        |
| 05-pixelpay-payment.md  | 462    | 100%    | 100%       | P0        |
| 06-fygaro-payment.md    | 439    | 100%    | 100%       | P0        |
| 06-subscriptions.md     | 744    | 100%    | 95%        | P0        |
| 07-foreign-currency.md  | 349    | 100%    | **50%** ⚠️ | P1        |
| 07-paypal-payment.md    | 499    | 100%    | 100%       | P1        |

### Descartado ❌

| Archivo              | Líneas | Razón                                                                                      |
| -------------------- | ------ | ------------------------------------------------------------------------------------------ |
| 05-escrow-service.md | 621    | No aplica al modelo de negocio OKLA (plataforma de anuncios, no marketplace transaccional) |

---

## 💳 Proveedores de Pago Activos

```
┌────────────────────────────────────────────────────────────────────────────┐
│                      ARQUITECTURA MULTI-PROVEEDOR                          │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│  🇩🇴 LOCALES (República Dominicana)                                        │
│  ├─ 🏦 AZUL (Banco Popular) ────── Primary - Tarjetas locales ✅ 100%     │
│  ├─ 💳 CardNET ──────────────────── Backup - Alta disponibilidad ✅ 100%  │
│  ├─ 🚀 PixelPay (Fintech) ───────── High Volume - Comisiones bajas ✅ 100%│
│  └─ 📦 Fygaro (Agregador) ───────── Suscripciones recurrentes ✅ 100%     │
│                                                                            │
│  🌎 INTERNACIONAL                                                          │
│  └─ 🅿️ PayPal ───────────────────── Global - 200+ países ✅ 100%          │
│                                                                            │
└────────────────────────────────────────────────────────────────────────────┘
```

### Comparativa de Comisiones

| Proveedor    | Tipo      | Comisión  | Por Tx       | Monedas       | Uso Recomendado      |
| ------------ | --------- | --------- | ------------ | ------------- | -------------------- |
| **AZUL**     | Bancario  | 2.9%-4.5% | RD$5-10      | DOP, USD      | Default 🇩🇴           |
| **CardNET**  | Bancario  | 2.5%-4.5% | RD$5-10      | DOP, USD      | Backup si AZUL falla |
| **PixelPay** | Fintech   | 1.0%-3.5% | US$0.15-0.25 | DOP, USD, EUR | ⭐ Alto volumen      |
| **Fygaro**   | Agregador | Variable  | Variable     | DOP, USD      | ⭐ Suscripciones     |
| **PayPal**   | Global    | 2.9%      | US$0.30      | Multi         | ⭐ Internacional     |

### Estrategia de Selección

```typescript
// Lógica implementada en PaymentService
function selectProvider(context: PaymentContext): PaymentGateway {
  // 1. Internacional → PayPal
  if (context.userCountry !== "DO") return PaymentGateway.PayPal;

  // 2. Suscripciones → Fygaro (reintentos + dunning)
  if (context.isSubscription) return PaymentGateway.Fygaro;

  // 3. Alto volumen (>$50K DOP/mes) → PixelPay (menor costo)
  if (context.merchantVolume > 50000) return PaymentGateway.PixelPay;

  // 4. Default → AZUL (bancario, confiable)
  return PaymentGateway.Azul;
}
```

---

## 🔴 GAPS IDENTIFICADOS

### Gap #1: InvoicingService UI (CRÍTICO)

| Campo         | Valor                          |
| ------------- | ------------------------------ |
| **Archivo**   | 04-invoicing-service.md        |
| **Backend**   | 60% (parcial)                  |
| **Frontend**  | **0%** ⚠️ NO EXISTE            |
| **Prioridad** | P0 - Crítica (compliance DGII) |
| **Esfuerzo**  | 400-500 líneas                 |

**Rutas Faltantes:**

| Ruta                  | Rol          | Descripción                   |
| --------------------- | ------------ | ----------------------------- |
| `/invoices`           | User, Dealer | Lista de facturas del usuario |
| `/invoices/:id`       | User, Dealer | Detalle y descarga PDF        |
| `/admin/invoices`     | Admin        | Gestión de NCF                |
| `/admin/dgii/reports` | Admin        | Generación formatos 606/607   |

**Componentes Faltantes:**

```
├── InvoicesListPage.tsx         → Lista de facturas con filtros
├── InvoiceDetailPage.tsx        → Detalle con descarga PDF
├── AdminInvoicesPage.tsx        → Gestión NCF para admin
├── AdminDGIIReportsPage.tsx     → Generación 606/607
├── InvoiceCard.tsx              → Card de factura en lista
├── InvoicePDFViewer.tsx         → Visor de PDF inline
├── NCFBadge.tsx                 → Badge con número NCF
└── invoicingService.ts          → API client
```

---

### Gap #2: Foreign Currency UI (50% → 100%)

| Campo         | Valor                           |
| ------------- | ------------------------------- |
| **Archivo**   | 07-foreign-currency-payments.md |
| **Backend**   | 100% ✅                         |
| **Frontend**  | 50% ⚠️                          |
| **Prioridad** | P1 - Alta                       |
| **Esfuerzo**  | 150 líneas                      |

**Faltantes:**

- Currency selector en checkout (USD/EUR/DOP)
- Display de tasa de cambio BCRD
- Conversión en tiempo real
- Integración con API `/api/exchangerates/current`

**Componentes Faltantes:**

```
├── CurrencySelector.tsx         → Radio/select USD/EUR/DOP
├── ExchangeRateDisplay.tsx      → Muestra tasa BCRD actual
├── CurrencyConversionSummary.tsx → Subtotal, ITBIS, total en DOP
└── hooks/useExchangeRate.ts     → React Query hook
```

---

### Gap #3: Refunds Admin UI (70% → 100%)

| Campo         | Valor                 |
| ------------- | --------------------- |
| **Archivo**   | 01-billing-service.md |
| **Backend**   | 100% ✅               |
| **Frontend**  | 70% ⚠️                |
| **Prioridad** | P2 - Media            |
| **Esfuerzo**  | 100 líneas            |

**Faltantes:**

- Tabla de reembolsos pendientes
- Botón "Aprobar Reembolso"
- Modal de confirmación con razón
- Historial de reembolsos procesados

**Componentes a Completar:**

```
├── AdminRefundsPage.tsx         → Lista de reembolsos pendientes
├── RefundApprovalModal.tsx      → Confirmar/rechazar con razón
└── RefundHistoryTable.tsx       → Historial de procesados
```

---

### Gap #4: Subscription Cancellation (90% → 100%)

| Campo         | Valor               |
| ------------- | ------------------- |
| **Archivo**   | 06-subscriptions.md |
| **Backend**   | 100% ✅             |
| **Frontend**  | 90% ⚠️              |
| **Prioridad** | P2 - Media          |
| **Esfuerzo**  | 80 líneas           |

**Faltantes:**

- Flujo de cancelación con survey (¿por qué cancelas?)
- Ofertas de retención (descuento, pause)
- Confirmación final con fecha de expiración

**Componentes a Completar:**

```
├── CancellationFlow.tsx         → Wizard de cancelación
├── CancellationSurvey.tsx       → Razón de cancelación
├── RetentionOffer.tsx           → Ofertas para retener
└── CancellationConfirmation.tsx → Confirmación final
```

---

### Gap #5: Webhooks UI Monitoring

| Campo         | Valor                 |
| ------------- | --------------------- |
| **Archivo**   | 01-billing-service.md |
| **Backend**   | 100% ✅               |
| **Frontend**  | 0% ⚠️                 |
| **Prioridad** | P3 - Baja             |
| **Esfuerzo**  | 120 líneas            |

**Faltantes:**

- Dashboard de webhooks recibidos
- Estado de procesamiento (success/failed)
- Retry manual de webhooks fallidos
- Logs de eventos por proveedor

**Componentes Faltantes:**

```
├── AdminWebhooksPage.tsx        → Dashboard de webhooks
├── WebhookEventCard.tsx         → Card con detalles de evento
├── WebhookRetryButton.tsx       → Retry manual
└── WebhookProviderFilter.tsx    → Filtrar por AZUL/PixelPay/etc
```

---

### Gap #6: Checkout Frontend Components

| Campo         | Valor                                   |
| ------------- | --------------------------------------- |
| **Archivo**   | 19-pagos-checkout.md (frontend-rebuild) |
| **Backend**   | 100% ✅                                 |
| **Frontend**  | 0% ⚠️ (componentes no creados)          |
| **Prioridad** | P0 - Crítica                            |
| **Esfuerzo**  | 600 líneas                              |

Según el checklist en `19-pagos-checkout.md`:

**Componentes Faltantes:**

```
├── PaymentMethodSelector.tsx    → Selector de proveedor con logos
├── CreditCardForm.tsx           → Formulario de tarjeta
├── CheckoutSummary.tsx          → Resumen de compra
├── SavedPaymentMethods.tsx      → Tarjetas guardadas
├── SubscriptionPlanCard.tsx     → Card de plan (Starter/Pro/Enterprise)
├── pages/CheckoutListingPage.tsx   → Checkout publicación individual
├── pages/CheckoutSubscriptionPage.tsx → Checkout suscripción dealer
├── pages/CheckoutPromotionPage.tsx → Checkout promoción/destacado
├── pages/CheckoutSuccessPage.tsx   → Confirmación de pago
├── pages/CheckoutFailurePage.tsx   → Error en pago
├── pages/CheckoutPendingPage.tsx   → Pago pendiente (ACH)
├── pages/BillingOverviewPage.tsx   → Dashboard facturación dealer
├── pages/PaymentMethodsPage.tsx    → Métodos de pago guardados
└── hooks/usePayments.ts            → React Query hooks
```

---

## 📈 Matriz de Priorización

### P0 - Crítico (Bloquea Revenue)

| Feature             | Líneas    | Justificación               |
| ------------------- | --------- | --------------------------- |
| Checkout Components | 600       | Sin checkout = sin pagos    |
| InvoicingService UI | 450       | Compliance DGII obligatorio |
| **Total P0**        | **1,050** |                             |

### P1 - Alta (Mejora UX significativa)

| Feature             | Líneas  | Justificación            |
| ------------------- | ------- | ------------------------ |
| Foreign Currency UI | 150     | Clientes internacionales |
| **Total P1**        | **150** |                          |

### P2 - Media (Nice to have)

| Feature                   | Líneas  | Justificación         |
| ------------------------- | ------- | --------------------- |
| Refunds Admin UI          | 100     | Admin workflow        |
| Subscription Cancellation | 80      | Retención de clientes |
| **Total P2**              | **180** |                       |

### P3 - Baja (Puede esperar)

| Feature             | Líneas  | Justificación       |
| ------------------- | ------- | ------------------- |
| Webhooks Monitoring | 120     | Solo para debugging |
| **Total P3**        | **120** |                     |

---

## 🎯 Plan de Implementación

### Fase 1: Checkout Foundation (P0) - Sprint 6

```
Objetivo: Habilitar flujo de pagos completo

1. PaymentMethodSelector
   - Logos de 5 proveedores
   - Selección automática por contexto
   - SavedPaymentMethods integrado

2. CreditCardForm
   - Tokenización con provider seleccionado
   - Validación en tiempo real
   - Test cards en desarrollo

3. Checkout Pages
   - /checkout/listing ($29)
   - /checkout/subscription (Starter/Pro/Enterprise)
   - /checkout/success + failure + pending

4. React Query Hooks
   - useChargePayment
   - useSavedPaymentMethods
   - useSubscriptionPlans

Estimado: 600 líneas
```

### Fase 2: Invoicing UI (P0) - Sprint 7

```
Objetivo: Compliance DGII completo

1. User Invoice Pages
   - /invoices → Lista con filtros
   - /invoices/:id → Detalle + PDF download

2. Admin Invoice Pages
   - /admin/invoices → Gestión NCF
   - /admin/dgii/reports → Formato 606/607

3. Components
   - InvoiceCard, NCFBadge, PDFViewer

Estimado: 450 líneas
```

### Fase 3: Enhancements (P1-P2) - Sprint 8

```
1. Foreign Currency UI (P1)
   - CurrencySelector
   - ExchangeRateDisplay
   - ConversionSummary

2. Refunds Admin (P2)
   - AdminRefundsPage
   - ApprovalModal

3. Subscription Cancel (P2)
   - CancellationFlow
   - RetentionOffer

Estimado: 330 líneas
```

---

## 📁 Archivos a Crear

### Frontend (frontend/web/src/)

```
src/
├── pages/
│   ├── checkout/
│   │   ├── CheckoutListingPage.tsx
│   │   ├── CheckoutSubscriptionPage.tsx
│   │   ├── CheckoutPromotionPage.tsx
│   │   ├── CheckoutSuccessPage.tsx
│   │   ├── CheckoutFailurePage.tsx
│   │   └── CheckoutPendingPage.tsx
│   ├── billing/
│   │   ├── InvoicesListPage.tsx
│   │   ├── InvoiceDetailPage.tsx
│   │   ├── BillingOverviewPage.tsx
│   │   └── PaymentMethodsPage.tsx
│   └── admin/
│       ├── AdminInvoicesPage.tsx
│       ├── AdminDGIIReportsPage.tsx
│       ├── AdminRefundsPage.tsx
│       └── AdminWebhooksPage.tsx
├── components/
│   └── payments/
│       ├── PaymentMethodSelector.tsx
│       ├── CreditCardForm.tsx
│       ├── CheckoutSummary.tsx
│       ├── SavedPaymentMethods.tsx
│       ├── SubscriptionPlanCard.tsx
│       ├── InvoiceCard.tsx
│       ├── InvoicePDFViewer.tsx
│       ├── NCFBadge.tsx
│       ├── CurrencySelector.tsx
│       ├── ExchangeRateDisplay.tsx
│       ├── CancellationFlow.tsx
│       └── RetentionOffer.tsx
├── services/
│   ├── paymentService.ts
│   ├── invoicingService.ts
│   └── exchangeRateService.ts
└── hooks/
    ├── usePayments.ts
    ├── useInvoices.ts
    └── useExchangeRate.ts
```

### Rutas App.tsx

```tsx
// Agregar en App.tsx

// Checkout Routes
<Route path="/checkout/listing" element={<CheckoutListingPage />} />
<Route path="/checkout/subscription" element={<CheckoutSubscriptionPage />} />
<Route path="/checkout/promotion" element={<CheckoutPromotionPage />} />
<Route path="/checkout/success" element={<CheckoutSuccessPage />} />
<Route path="/checkout/failure" element={<CheckoutFailurePage />} />
<Route path="/checkout/pending" element={<CheckoutPendingPage />} />

// Billing Routes (Protected)
<Route path="/invoices" element={<ProtectedRoute><InvoicesListPage /></ProtectedRoute>} />
<Route path="/invoices/:id" element={<ProtectedRoute><InvoiceDetailPage /></ProtectedRoute>} />
<Route path="/billing" element={<ProtectedRoute><BillingOverviewPage /></ProtectedRoute>} />
<Route path="/billing/methods" element={<ProtectedRoute><PaymentMethodsPage /></ProtectedRoute>} />

// Admin Routes
<Route path="/admin/invoices" element={<AdminRoute><AdminInvoicesPage /></AdminRoute>} />
<Route path="/admin/dgii/reports" element={<AdminRoute><AdminDGIIReportsPage /></AdminRoute>} />
<Route path="/admin/refunds" element={<AdminRoute><AdminRefundsPage /></AdminRoute>} />
<Route path="/admin/webhooks" element={<AdminRoute><AdminWebhooksPage /></AdminRoute>} />
```

---

## 📊 Métricas Finales

| Métrica                  | Antes   | Después | Delta  |
| ------------------------ | ------- | ------- | ------ |
| **Líneas Frontend**      | ~2,500  | ~4,000  | +1,500 |
| **Coverage Backend**     | 95%     | 100%    | +5%    |
| **Coverage Frontend**    | 75%     | 100%    | +25%   |
| **Páginas Checkout**     | 0       | 6       | +6     |
| **Páginas Billing**      | 1       | 5       | +4     |
| **Componentes Payments** | 0       | 12      | +12    |
| **Proveedores UI**       | 5       | 5       | =      |
| **DGII Compliance**      | Backend | Full    | ✅     |

---

## ✅ Conclusión

El dominio **PAGOS-FACTURACION** tiene:

- ✅ **Backend sólido** con 5 proveedores de pago activos
- ✅ **Arquitectura correcta** (Factory + Registry + Strategy)
- ⚠️ **Frontend incompleto** - 6 gaps identificados
- ❌ **EscrowService descartado** - No aplica al modelo de negocio

**Prioridad de Implementación:**

1. **P0 (Crítico):** Checkout Components + Invoicing UI = 1,050 líneas
2. **P1 (Alta):** Foreign Currency UI = 150 líneas
3. **P2 (Media):** Refunds + Cancellation = 180 líneas
4. **P3 (Baja):** Webhooks Monitoring = 120 líneas

**Total a implementar:** ~1,500 líneas para alcanzar 100% coverage

---

**Última actualización:** Enero 28, 2026  
**Autor:** OKLA Development Team
