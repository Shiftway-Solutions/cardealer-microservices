---
title: "19 - Pagos y Checkout"
priority: P1
estimated_time: "2 horas"
dependencies: []
apis: ["VehiclesSaleService", "DealerManagementService"]
status: complete
last_updated: "2026-01-30"
---

# 💳 19 - Pagos y Checkout

> **Sprint:** 4 (Pagos y Facturación)  
> **Prioridad:** P0 - Crítica  
> **Roles:** Buyer, Seller, Dealer, Admin  
> **Proceso Matrix:** [pagos-checkout](../../process-matrix/pagos-checkout/)  
> **Backend:** PaymentService (Multi-Proveedor)

---

## 📑 Tabla de Contenidos

1. [Resumen](#-resumen)
2. [Arquitectura de Pagos](#-arquitectura-de-pagos)
3. [Proveedores Soportados](#-proveedores-soportados)
4. [Páginas y Rutas](#-páginas-y-rutas)
5. [Componentes UI](#-componentes-ui)
6. [Flujos de Pago](#-flujos-de-pago)
7. [Estados y Transiciones](#-estados-y-transiciones)
8. [Integración API](#-integración-api)
9. [Webhooks](#-webhooks)
10. [Suscripciones](#-suscripciones)
11. [Seguridad](#-seguridad)
12. [Testing](#-testing)

---

## 📋 Resumen

Sistema de pagos multi-proveedor para OKLA que soporta:

| Funcionalidad                | Descripción                                 |
| ---------------------------- | ------------------------------------------- |
| **Pago de Listados**         | Vendedores individuales ($29/listing)       |
| **Suscripciones Dealers**    | Plans Starter/Pro/Enterprise ($49-$299/mes) |
| **Destacados y Promociones** | Upgrades de visibilidad                     |
| **Tokenización**             | Guardar tarjetas para pagos recurrentes     |
| **Multi-Moneda**             | DOP, USD, EUR                               |

### Proveedores Activos (NO Stripe)

```
┌────────────────────────────────────────────────────────────────────────────┐
│                      ARQUITECTURA MULTI-PROVEEDOR                         │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│  🇩🇴 LOCALES (República Dominicana)                                        │
│  ├─ 🏦 AZUL (Banco Popular) ────── Primary - Tarjetas locales             │
│  ├─ 💳 CardNET ──────────────────── Backup - Alta disponibilidad          │
│  ├─ 🚀 PixelPay (Fintech) ───────── High Volume - Comisiones bajas        │
│  └─ 📦 Fygaro (Agregador) ───────── Suscripciones recurrentes             │
│                                                                            │
│  🌎 INTERNACIONAL                                                          │
│  └─ 🅿️ PayPal ───────────────────── Global - 200+ países (reemplaza Stripe)│
│                                                                            │
└────────────────────────────────────────────────────────────────────────────┘
```

---

## 🏗️ Arquitectura de Pagos

### Patrón: Factory + Registry + Strategy

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          PaymentService                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────────────┐    ┌──────────────────┐    ┌──────────────────────┐  │
│  │ PaymentsController│    │IPaymentGateway  │    │PaymentGatewayRegistry│  │
│  │   /api/payments   │───▶│    Factory      │───▶│ (todos los providers)│  │
│  └──────────────────┘    └──────────────────┘    └──────────────────────┘  │
│                                    │                                        │
│                    ┌───────────────┼───────────────┬───────────────┐        │
│                    ▼               ▼               ▼               ▼        │
│            ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐  │
│            │AzulProvider │ │CardNET      │ │PixelPay    │ │FygaroProvider│ │
│            │(Primary RD) │ │Provider     │ │Provider    │ │(Subs)       │  │
│            └─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘  │
│                    │               │               │               │        │
│                    └───────────────┴───────────────┴───────────────┘        │
│                                    │                                        │
│                    ┌───────────────┴───────────────┐                        │
│                    ▼                               ▼                        │
│            ┌─────────────┐                 ┌─────────────┐                  │
│            │PayPalProvider│               │  Webhooks   │                  │
│            │(International)│              │  Handler    │                  │
│            └─────────────┘                 └─────────────┘                  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Flujo de Selección de Proveedor

```typescript
// Lógica de selección automática
function selectPaymentProvider(
  amount: number,
  currency: string,
  isSubscription: boolean,
  userCountry: string,
): PaymentGateway {
  // 1. Internacional → PayPal
  if (userCountry !== "DO") {
    return PaymentGateway.PayPal;
  }

  // 2. Suscripciones → Fygaro (optimizado para recurrentes)
  if (isSubscription) {
    return PaymentGateway.Fygaro;
  }

  // 3. Alto volumen (>$50,000 DOP/mes) → PixelPay (comisiones más bajas)
  if (merchantVolume > 50000) {
    return PaymentGateway.PixelPay;
  }

  // 4. Default → AZUL (bancario, confiable)
  return PaymentGateway.Azul;
}
```

---

## 💳 Proveedores Soportados

### Comparativa de Comisiones

| Proveedor    | Tipo      | Comisión  | Por Tx       | Mensual  | Monedas       | Cobertura      |
| ------------ | --------- | --------- | ------------ | -------- | ------------- | -------------- |
| **AZUL**     | Bancario  | 2.9%-4.5% | RD$5-10      | US$30-50 | DOP, USD      | 🇩🇴 RD          |
| **CardNET**  | Bancario  | 2.5%-4.5% | RD$5-10      | US$30-50 | DOP, USD      | 🇩🇴 RD          |
| **PixelPay** | Fintech   | 1.0%-3.5% | US$0.15-0.25 | Varía    | DOP, USD, EUR | 🇩🇴 RD, LAT     |
| **Fygaro**   | Agregador | Varía     | Varía        | US$15+   | DOP, USD      | 🇩🇴 RD          |
| **PayPal**   | Global    | 2.9%      | US$0.30      | Gratis\* | USD, EUR, DOP | 🌎 200+ países |

### Cuándo Usar Cada Uno

```
📊 RECOMENDACIÓN POR CASO DE USO
────────────────────────────────────────────────────────────────
│ Caso de Uso                    │ Proveedor Recomendado      │
├────────────────────────────────┼────────────────────────────┤
│ Volumen bajo/medio (<$50K/mes) │ 🏦 AZUL (confiable)        │
│ Volumen alto (>$50K/mes)       │ 🚀 PixelPay (menor costo)  │
│ Suscripciones mensuales        │ 📦 Fygaro (optimizado)     │
│ Pagos internacionales          │ 🅿️ PayPal (global)         │
│ Backup/Redundancia             │ 💳 CardNET (si AZUL falla) │
────────────────────────────────────────────────────────────────
```

---

## 🛣️ Páginas y Rutas

### Estructura de Navegación

```
/checkout
├── /checkout/[productType]/[productId]    → Checkout de producto
├── /checkout/listing                      → Pagar publicación individual
├── /checkout/subscription                 → Suscripción de Dealer
├── /checkout/promotion                    → Destacar vehículo
├── /checkout/success                      → Confirmación de pago
├── /checkout/failure                      → Error en pago
└── /checkout/pending                      → Pago pendiente (ACH)

/billing (Dealer/Admin)
├── /billing/overview                      → Dashboard de facturación
├── /billing/invoices                      → Lista de facturas
├── /billing/invoices/[id]                 → Detalle de factura
├── /billing/payment-methods               → Métodos de pago guardados
├── /billing/subscriptions                 → Suscripciones activas
└── /billing/transactions                  → Historial de transacciones

/payments (Admin)
├── /payments/transactions                 → Todas las transacciones
├── /payments/refunds                      → Reembolsos pendientes
├── /payments/providers                    → Estado de proveedores
└── /payments/reports                      → Reportes de ingresos
```

---

## 🧩 Componentes UI

### 1. PaymentMethodSelector

Selector de método de pago con logos de proveedores.

```tsx
// src/components/payments/PaymentMethodSelector.tsx

interface PaymentMethodSelectorProps {
  availableProviders: PaymentGateway[];
  selectedProvider: PaymentGateway;
  onProviderChange: (provider: PaymentGateway) => void;
  amount: number;
  currency: Currency;
}

export function PaymentMethodSelector({
  availableProviders,
  selectedProvider,
  onProviderChange,
  amount,
  currency,
}: PaymentMethodSelectorProps) {
  return (
    <div className="space-y-4">
      <h3 className="text-lg font-semibold">Método de Pago</h3>

      {/* Tarjetas de Crédito/Débito */}
      <div className="border rounded-lg p-4">
        <h4 className="font-medium mb-3">💳 Tarjeta de Crédito/Débito</h4>
        <div className="grid grid-cols-2 gap-3">
          {availableProviders
            .filter((p) => p !== "PayPal")
            .map((provider) => (
              <ProviderCard
                key={provider}
                provider={provider}
                isSelected={selectedProvider === provider}
                onSelect={() => onProviderChange(provider)}
                commission={getCommission(provider, amount)}
              />
            ))}
        </div>
      </div>

      {/* PayPal */}
      {availableProviders.includes("PayPal") && (
        <div className="border rounded-lg p-4">
          <h4 className="font-medium mb-3">🅿️ PayPal</h4>
          <ProviderCard
            provider="PayPal"
            isSelected={selectedProvider === "PayPal"}
            onSelect={() => onProviderChange("PayPal")}
            commission={getCommission("PayPal", amount)}
            subtitle="Paga con tu cuenta PayPal o tarjeta internacional"
          />
        </div>
      )}

      {/* Fee Preview */}
      <div className="bg-gray-50 rounded-lg p-4">
        <div className="flex justify-between">
          <span>Subtotal:</span>
          <span>{formatCurrency(amount, currency)}</span>
        </div>
        <div className="flex justify-between text-gray-500">
          <span>Comisión procesamiento:</span>
          <span>
            +{formatCurrency(getCommission(selectedProvider, amount), currency)}
          </span>
        </div>
        <div className="flex justify-between font-bold border-t pt-2 mt-2">
          <span>Total:</span>
          <span>
            {formatCurrency(
              amount + getCommission(selectedProvider, amount),
              currency,
            )}
          </span>
        </div>
      </div>
    </div>
  );
}
```

### 2. CreditCardForm

Formulario seguro de tarjeta de crédito.

```tsx
// src/components/payments/CreditCardForm.tsx

interface CreditCardFormProps {
  onSubmit: (cardData: CardData) => Promise<void>;
  saveCard?: boolean;
  onSaveCardChange?: (save: boolean) => void;
  loading?: boolean;
}

export function CreditCardForm({
  onSubmit,
  saveCard = false,
  onSaveCardChange,
  loading = false,
}: CreditCardFormProps) {
  const [cardNumber, setCardNumber] = useState("");
  const [expiryDate, setExpiryDate] = useState("");
  const [cvv, setCvv] = useState("");
  const [cardholderName, setCardholderName] = useState("");
  const [errors, setErrors] = useState<Record<string, string>>({});

  // Detectar tipo de tarjeta
  const cardType = detectCardType(cardNumber);

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {/* Número de Tarjeta */}
      <div>
        <label className="block text-sm font-medium mb-1">
          Número de Tarjeta
        </label>
        <div className="relative">
          <input
            type="text"
            value={formatCardNumber(cardNumber)}
            onChange={(e) => setCardNumber(e.target.value.replace(/\D/g, ""))}
            placeholder="1234 5678 9012 3456"
            maxLength={19}
            className={cn(
              "w-full px-4 py-3 border rounded-lg",
              errors.cardNumber && "border-red-500",
            )}
          />
          <div className="absolute right-3 top-1/2 -translate-y-1/2">
            <CardTypeIcon type={cardType} />
          </div>
        </div>
        {errors.cardNumber && (
          <p className="text-red-500 text-sm mt-1">{errors.cardNumber}</p>
        )}
      </div>

      {/* Fecha y CVV */}
      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium mb-1">
            Fecha de Expiración
          </label>
          <input
            type="text"
            value={formatExpiryDate(expiryDate)}
            onChange={(e) => setExpiryDate(e.target.value.replace(/\D/g, ""))}
            placeholder="MM/AA"
            maxLength={5}
            className={cn(
              "w-full px-4 py-3 border rounded-lg",
              errors.expiryDate && "border-red-500",
            )}
          />
        </div>
        <div>
          <label className="block text-sm font-medium mb-1">CVV</label>
          <input
            type="text"
            value={cvv}
            onChange={(e) => setCvv(e.target.value.replace(/\D/g, ""))}
            placeholder="123"
            maxLength={4}
            className={cn(
              "w-full px-4 py-3 border rounded-lg",
              errors.cvv && "border-red-500",
            )}
          />
        </div>
      </div>

      {/* Nombre del Titular */}
      <div>
        <label className="block text-sm font-medium mb-1">
          Nombre del Titular
        </label>
        <input
          type="text"
          value={cardholderName}
          onChange={(e) => setCardholderName(e.target.value.toUpperCase())}
          placeholder="JUAN PEREZ"
          className="w-full px-4 py-3 border rounded-lg"
        />
      </div>

      {/* Guardar Tarjeta */}
      {onSaveCardChange && (
        <label className="flex items-center gap-2 cursor-pointer">
          <input
            type="checkbox"
            checked={saveCard}
            onChange={(e) => onSaveCardChange(e.target.checked)}
            className="rounded border-gray-300"
          />
          <span className="text-sm">Guardar tarjeta para futuros pagos</span>
        </label>
      )}

      {/* Seguridad */}
      <div className="flex items-center gap-2 text-gray-500 text-sm">
        <LockIcon className="w-4 h-4" />
        <span>Tu información está encriptada y protegida</span>
      </div>

      {/* Submit */}
      <button
        type="submit"
        disabled={loading}
        className="w-full bg-blue-600 text-white py-4 rounded-lg font-semibold hover:bg-blue-700 disabled:opacity-50"
      >
        {loading ? <Spinner className="mx-auto" /> : "Pagar Ahora"}
      </button>
    </form>
  );
}
```

### 3. CheckoutSummary

Resumen del pedido antes de pagar.

```tsx
// src/components/payments/CheckoutSummary.tsx

interface CheckoutSummaryProps {
  items: CheckoutItem[];
  discount?: DiscountCode;
  provider: PaymentGateway;
  currency: Currency;
}

export function CheckoutSummary({
  items,
  discount,
  provider,
  currency,
}: CheckoutSummaryProps) {
  const subtotal = items.reduce(
    (sum, item) => sum + item.price * item.quantity,
    0,
  );
  const discountAmount = discount ? calculateDiscount(subtotal, discount) : 0;
  const commission = getCommission(provider, subtotal - discountAmount);
  const tax = calculateTax(subtotal - discountAmount); // ITBIS 18%
  const total = subtotal - discountAmount + commission + tax;

  return (
    <div className="bg-gray-50 rounded-xl p-6 sticky top-4">
      <h3 className="text-xl font-bold mb-4">Resumen del Pedido</h3>

      {/* Items */}
      <div className="space-y-3 mb-6">
        {items.map((item, index) => (
          <div key={index} className="flex justify-between">
            <div>
              <p className="font-medium">{item.name}</p>
              <p className="text-sm text-gray-500">{item.description}</p>
            </div>
            <span>{formatCurrency(item.price * item.quantity, currency)}</span>
          </div>
        ))}
      </div>

      {/* Discount Code Input */}
      <div className="mb-6">
        <DiscountCodeInput onApply={handleApplyDiscount} applied={discount} />
      </div>

      {/* Breakdown */}
      <div className="space-y-2 border-t pt-4">
        <div className="flex justify-between text-gray-600">
          <span>Subtotal:</span>
          <span>{formatCurrency(subtotal, currency)}</span>
        </div>

        {discount && (
          <div className="flex justify-between text-green-600">
            <span>Descuento ({discount.code}):</span>
            <span>-{formatCurrency(discountAmount, currency)}</span>
          </div>
        )}

        <div className="flex justify-between text-gray-600">
          <span>ITBIS (18%):</span>
          <span>{formatCurrency(tax, currency)}</span>
        </div>

        <div className="flex justify-between text-gray-600">
          <span>Comisión ({provider}):</span>
          <span>{formatCurrency(commission, currency)}</span>
        </div>

        <div className="flex justify-between font-bold text-lg border-t pt-2 mt-2">
          <span>Total:</span>
          <span>{formatCurrency(total, currency)}</span>
        </div>
      </div>

      {/* Security Badges */}
      <div className="mt-6 flex items-center justify-center gap-4">
        <img src="/badges/pci-dss.svg" alt="PCI DSS" className="h-8" />
        <img src="/badges/ssl-secure.svg" alt="SSL" className="h-8" />
        <img
          src={`/badges/${provider.toLowerCase()}.svg`}
          alt={provider}
          className="h-8"
        />
      </div>
    </div>
  );
}
```

### 4. SavedPaymentMethods

Lista de métodos de pago guardados.

```tsx
// src/components/payments/SavedPaymentMethods.tsx

interface SavedPaymentMethodsProps {
  methods: TokenizedCard[];
  selectedMethod?: string;
  onSelect: (tokenId: string) => void;
  onAddNew: () => void;
  onDelete: (tokenId: string) => Promise<void>;
}

export function SavedPaymentMethods({
  methods,
  selectedMethod,
  onSelect,
  onAddNew,
  onDelete,
}: SavedPaymentMethodsProps) {
  return (
    <div className="space-y-3">
      <h4 className="font-medium">Tarjetas Guardadas</h4>

      {methods.map((method) => (
        <div
          key={method.token}
          onClick={() => onSelect(method.token)}
          className={cn(
            "flex items-center justify-between p-4 border rounded-lg cursor-pointer",
            selectedMethod === method.token
              ? "border-blue-500 bg-blue-50"
              : "hover:border-gray-300",
          )}
        >
          <div className="flex items-center gap-3">
            <CardTypeIcon type={method.cardType} className="w-10" />
            <div>
              <p className="font-medium">
                •••• •••• •••• {method.lastFourDigits}
              </p>
              <p className="text-sm text-gray-500">
                Expira {method.expiryMonth}/{method.expiryYear}
              </p>
            </div>
          </div>

          <div className="flex items-center gap-2">
            {method.isDefault && (
              <span className="bg-blue-100 text-blue-700 text-xs px-2 py-1 rounded">
                Predeterminada
              </span>
            )}
            <button
              onClick={(e) => {
                e.stopPropagation();
                onDelete(method.token);
              }}
              className="text-red-500 hover:text-red-700 p-2"
            >
              <TrashIcon className="w-4 h-4" />
            </button>
          </div>
        </div>
      ))}

      {/* Add New Card */}
      <button
        onClick={onAddNew}
        className="w-full flex items-center justify-center gap-2 p-4 border-2 border-dashed rounded-lg text-gray-500 hover:border-blue-500 hover:text-blue-500"
      >
        <PlusIcon className="w-5 h-5" />
        <span>Agregar Nueva Tarjeta</span>
      </button>
    </div>
  );
}
```

### 5. SubscriptionPlanCard

Tarjeta de plan de suscripción para Dealers.

```tsx
// src/components/payments/SubscriptionPlanCard.tsx

interface SubscriptionPlanCardProps {
  plan: DealerPlan;
  currentPlan?: DealerPlan;
  onSelect: (plan: DealerPlan) => void;
  isEarlyBird?: boolean;
}

const PLANS = {
  Starter: {
    name: "Starter",
    price: 49,
    earlyBirdPrice: 39,
    maxListings: 15,
    features: [
      "Hasta 15 vehículos activos",
      "Panel básico de estadísticas",
      "Badge de verificación",
      "Soporte por email",
    ],
  },
  Pro: {
    name: "Pro",
    price: 129,
    earlyBirdPrice: 103,
    maxListings: 50,
    recommended: true,
    features: [
      "Hasta 50 vehículos activos",
      "Analytics avanzados",
      "Import/Export CSV",
      "Múltiples ubicaciones",
      "Prioridad en búsquedas",
      "Soporte prioritario",
    ],
  },
  Enterprise: {
    name: "Enterprise",
    price: 299,
    earlyBirdPrice: 239,
    maxListings: Infinity,
    features: [
      "Vehículos ILIMITADOS",
      "API de integración",
      "White-label (dominio propio)",
      "CRM integrado",
      "Account manager dedicado",
      "Soporte 24/7",
    ],
  },
};

export function SubscriptionPlanCard({
  plan,
  currentPlan,
  onSelect,
  isEarlyBird = false,
}: SubscriptionPlanCardProps) {
  const config = PLANS[plan];
  const isCurrentPlan = currentPlan === plan;
  const displayPrice = isEarlyBird ? config.earlyBirdPrice : config.price;

  return (
    <div
      className={cn(
        "relative border-2 rounded-2xl p-6",
        config.recommended && "border-blue-500",
        isCurrentPlan && "bg-gray-50",
      )}
    >
      {/* Recommended Badge */}
      {config.recommended && (
        <div className="absolute -top-3 left-1/2 -translate-x-1/2 bg-blue-500 text-white px-4 py-1 rounded-full text-sm font-medium">
          ⭐ Más Popular
        </div>
      )}

      {/* Early Bird Badge */}
      {isEarlyBird && (
        <div className="absolute -top-3 right-4 bg-gradient-to-r from-orange-500 to-red-500 text-white px-3 py-1 rounded-full text-xs font-bold animate-pulse">
          🎉 Early Bird
        </div>
      )}

      {/* Plan Name */}
      <h3 className="text-2xl font-bold mb-2">{config.name}</h3>

      {/* Price */}
      <div className="mb-6">
        {isEarlyBird && (
          <span className="line-through text-gray-400 text-lg">
            ${config.price}
          </span>
        )}
        <div className="flex items-baseline">
          <span className="text-4xl font-bold">${displayPrice}</span>
          <span className="text-gray-500 ml-1">/mes</span>
        </div>
        {isEarlyBird && (
          <p className="text-green-600 text-sm mt-1">
            ¡Ahorras ${config.price - config.earlyBirdPrice}/mes de por vida!
          </p>
        )}
      </div>

      {/* Listings */}
      <p className="text-lg font-medium mb-4">
        📦{" "}
        {config.maxListings === Infinity
          ? "ILIMITADOS"
          : `Hasta ${config.maxListings}`}{" "}
        vehículos
      </p>

      {/* Features */}
      <ul className="space-y-2 mb-6">
        {config.features.map((feature, index) => (
          <li key={index} className="flex items-center gap-2">
            <CheckIcon className="w-5 h-5 text-green-500" />
            <span>{feature}</span>
          </li>
        ))}
      </ul>

      {/* CTA Button */}
      {isCurrentPlan ? (
        <div className="w-full py-3 text-center bg-gray-200 rounded-lg font-medium">
          Plan Actual
        </div>
      ) : (
        <button
          onClick={() => onSelect(plan)}
          className={cn(
            "w-full py-3 rounded-lg font-semibold transition-colors",
            config.recommended
              ? "bg-blue-600 text-white hover:bg-blue-700"
              : "bg-gray-100 text-gray-800 hover:bg-gray-200",
          )}
        >
          {currentPlan
            ? "Cambiar a " + config.name
            : "Comenzar con " + config.name}
        </button>
      )}
    </div>
  );
}
```

---

## 🔄 Flujos de Pago

### Flujo 1: Pago de Publicación Individual (Seller)

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                    FLUJO: PAGO DE PUBLICACIÓN ($29)                         │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  1️⃣ Usuario completa formulario de vehículo (/publicar)                      │
│     ├─ Fotos, detalles, precio                                              │
│     └─ Click "Publicar" → Redirige a /checkout/listing                      │
│                                                                              │
│  2️⃣ Checkout Page (/checkout/listing)                                        │
│     ├─ CheckoutSummary: Publicación $29 + ITBIS                             │
│     ├─ PaymentMethodSelector: AZUL, CardNET, PixelPay, PayPal               │
│     │   ├─ Si tiene tarjetas guardadas → SavedPaymentMethods                │
│     │   └─ Si no → CreditCardForm                                           │
│     ├─ DiscountCodeInput: Código promocional                                │
│     └─ Terms checkbox + "Pagar $XX.XX"                                      │
│                                                                              │
│  3️⃣ Procesamiento                                                            │
│     ├─ Frontend: POST /api/payments/charge                                  │
│     ├─ PaymentService: Factory selecciona proveedor                         │
│     ├─ Provider.ChargeAsync() → API externa                                 │
│     └─ Resultado: { transactionId, status, gateway }                        │
│                                                                              │
│  4️⃣ Resultado                                                                │
│     ├─ ✅ Success → /checkout/success?txId=xxx                              │
│     │   └─ VehiclesSaleService.ActivateVehicle(vehicleId)                   │
│     ├─ ❌ Failed → /checkout/failure?reason=xxx                             │
│     │   └─ Mostrar error + "Intentar de nuevo"                              │
│     └─ ⏳ Pending → /checkout/pending (para ACH)                             │
│         └─ Email cuando se confirme                                         │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

### Flujo 2: Suscripción de Dealer

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                    FLUJO: SUSCRIPCIÓN DEALER                                │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  1️⃣ Dealer Landing (/dealer/landing)                                         │
│     └─ CTA "Ver Planes" → /dealer/pricing                                   │
│                                                                              │
│  2️⃣ Pricing Page (/dealer/pricing)                                           │
│     ├─ 3x SubscriptionPlanCard (Starter, Pro, Enterprise)                   │
│     ├─ Early Bird Banner (si aplica)                                        │
│     └─ Click "Comenzar" → /checkout/subscription?plan=Pro                   │
│                                                                              │
│  3️⃣ Checkout Subscription (/checkout/subscription)                           │
│     ├─ Plan seleccionado con precio                                         │
│     ├─ Si Early Bird: 3 meses gratis + 20% off                              │
│     ├─ PaymentMethodSelector (default: Fygaro para suscripciones)           │
│     ├─ CreditCardForm con saveCard=true obligatorio                         │
│     └─ "Suscribirse por $XX/mes"                                            │
│                                                                              │
│  4️⃣ Procesamiento                                                            │
│     ├─ POST /api/payments/tokenize → Guarda tarjeta                         │
│     ├─ POST /api/subscriptions → Crea suscripción                           │
│     │   ├─ Gateway: Fygaro (optimizado para recurrentes)                    │
│     │   ├─ Frecuencia: Monthly                                              │
│     │   └─ Primer cobro: Ahora (o en 3 meses si Early Bird)                 │
│     └─ DealerManagementService.ActivateDealer()                             │
│                                                                              │
│  5️⃣ Post-Success                                                             │
│     ├─ Redirige a /dealer/dashboard                                         │
│     ├─ Email de bienvenida + factura                                        │
│     └─ Webhook mensual para cobros recurrentes                              │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

### Flujo 3: Destacar Vehículo

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                    FLUJO: PROMOCIÓN/DESTACADO                               │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  1️⃣ Desde Detalle de Vehículo o Dashboard                                    │
│     └─ Click "Destacar" → Modal o /checkout/promotion?vehicleId=xxx         │
│                                                                              │
│  2️⃣ Selección de Promoción                                                   │
│     ├─ Destacado Homepage (7 días): $15                                     │
│     ├─ Top de Búsquedas (14 días): $25                                      │
│     ├─ Featured Badge (30 días): $10                                        │
│     └─ Pack Completo: $40 (ahorra $10)                                      │
│                                                                              │
│  3️⃣ Checkout rápido                                                          │
│     ├─ Si tiene tarjeta guardada → 1-click payment                          │
│     ├─ Si no → CreditCardForm simplificado                                  │
│     └─ Procesamiento con proveedor default (AZUL/PixelPay)                  │
│                                                                              │
│  4️⃣ Activación inmediata                                                     │
│     ├─ VehiclesSaleService.ApplyPromotion(vehicleId, type)                  │
│     └─ Vehículo aparece en sección destacada                                │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## 📊 Estados y Transiciones

### TransactionStatus Enum

```typescript
enum TransactionStatus {
  Pending = "Pending", // Esperando confirmación
  Processing = "Processing", // En proceso
  Approved = "Approved", // Aprobada ✅
  Declined = "Declined", // Rechazada por banco ❌
  Failed = "Failed", // Error técnico ❌
  Cancelled = "Cancelled", // Cancelada por usuario
  Refunded = "Refunded", // Reembolsada
  PartialRefund = "PartialRefund", // Reembolso parcial
  Chargeback = "Chargeback", // Disputa/Contracargo
  Expired = "Expired", // Expirada (no completada)
}
```

### Diagrama de Estados

```
                              ┌─────────────┐
                              │   Pending   │
                              └──────┬──────┘
                                     │
                              ┌──────▼──────┐
                              │ Processing  │
                              └──────┬──────┘
                                     │
              ┌──────────────────────┼──────────────────────┐
              │                      │                      │
       ┌──────▼──────┐        ┌──────▼──────┐        ┌──────▼──────┐
       │  Approved   │        │  Declined   │        │   Failed    │
       └──────┬──────┘        └─────────────┘        └─────────────┘
              │
      ┌───────┴───────┐
      │               │
┌─────▼─────┐   ┌─────▼─────┐
│ Refunded  │   │Chargeback │
└───────────┘   └───────────┘
```

---

## 🔌 Integración API

### DTOs de Request/Response

```typescript
// types/payment.types.ts

// ============ ENUMS ============

export enum PaymentGateway {
  Azul = "Azul",
  CardNET = "CardNET",
  PixelPay = "PixelPay",
  Fygaro = "Fygaro",
  PayPal = "PayPal",
}

export enum PaymentMethod {
  CreditCard = "CreditCard",
  DebitCard = "DebitCard",
  ACH = "ACH",
  MobilePayment = "MobilePayment",
  EWallet = "EWallet",
  TokenizedCard = "TokenizedCard",
}

export enum Currency {
  DOP = "DOP",
  USD = "USD",
  EUR = "EUR",
}

// ============ REQUESTS ============

export interface ChargeRequest {
  userId: string;
  amount: number;
  currency: Currency;
  gateway?: PaymentGateway; // Opcional, default = AZUL
  paymentMethod: PaymentMethod;

  // Card details (si no usa token)
  cardNumber?: string;
  cardExpiryMonth?: string;
  cardExpiryYear?: string;
  cardCVV?: string;
  cardholderName?: string;

  // O usar token guardado
  cardToken?: string;

  // Metadata
  description?: string;
  metadata?: Record<string, string>;
  idempotencyKey?: string;
}

export interface TokenizeRequest {
  userId: string;
  cardNumber: string;
  cardExpiryMonth: string;
  cardExpiryYear: string;
  cardCVV: string;
  cardholderName: string;
  gateway?: PaymentGateway;
  setAsDefault?: boolean;
}

export interface RefundRequest {
  transactionId: string;
  amount?: number; // Opcional para reembolso parcial
  reason?: string;
}

export interface CreateSubscriptionRequest {
  userId: string;
  planId: string;
  gateway?: PaymentGateway; // Default: Fygaro
  cardToken: string;
  frequency: SubscriptionFrequency;
  startDate?: string; // ISO date, default = now
  metadata?: Record<string, string>;
}

// ============ RESPONSES ============

export interface ChargeResponse {
  transactionId: string;
  externalTransactionId: string; // ID del proveedor
  status: TransactionStatus;
  gateway: PaymentGateway;
  providerName: string;

  // Amounts
  amount: number;
  commission: number;
  commissionPercentage: number;
  netAmount: number;
  currency: Currency;

  // Timestamps
  createdAt: string;
  processedAt?: string;

  // Result
  isSuccessful: boolean;
  errorCode?: string;
  errorMessage?: string;

  // Additional
  receiptUrl?: string;
  metadata?: Record<string, string>;
}

export interface TokenizedCard {
  token: string;
  lastFourDigits: string;
  cardType: "visa" | "mastercard" | "amex" | "discover";
  expiryMonth: string;
  expiryYear: string;
  cardholderName: string;
  isDefault: boolean;
  gateway: PaymentGateway;
  createdAt: string;
}

export interface Subscription {
  subscriptionId: string;
  userId: string;
  planId: string;
  planName: string;

  status: SubscriptionStatus;
  gateway: PaymentGateway;

  amount: number;
  currency: Currency;
  frequency: SubscriptionFrequency;

  currentPeriodStart: string;
  currentPeriodEnd: string;
  nextBillingDate: string;

  cardToken: string;
  cardLastFour: string;

  cancelledAt?: string;
  cancelReason?: string;

  createdAt: string;
  updatedAt: string;
}
```

### API Service

```typescript
// services/payment.service.ts

import { api } from "@/lib/api";

class PaymentService {
  private baseUrl = "/api/payments";

  // ============ PROVIDERS ============

  async getProviders(): Promise<ProviderInfo[]> {
    const { data } = await api.get(`${this.baseUrl}/providers`);
    return data;
  }

  async getProviderHealth(gateway: PaymentGateway): Promise<ProviderHealth> {
    const { data } = await api.get(
      `${this.baseUrl}/providers/${gateway}/health`,
    );
    return data;
  }

  // ============ CHARGES ============

  async charge(request: ChargeRequest): Promise<ChargeResponse> {
    const { data } = await api.post(`${this.baseUrl}/charge`, request);
    return data;
  }

  async authorize(request: ChargeRequest): Promise<ChargeResponse> {
    const { data } = await api.post(`${this.baseUrl}/authorize`, request);
    return data;
  }

  async capture(
    transactionId: string,
    amount?: number,
  ): Promise<ChargeResponse> {
    const { data } = await api.post(`${this.baseUrl}/capture`, {
      transactionId,
      amount,
    });
    return data;
  }

  async refund(request: RefundRequest): Promise<ChargeResponse> {
    const { data } = await api.post(`${this.baseUrl}/refund`, request);
    return data;
  }

  async getTransaction(transactionId: string): Promise<ChargeResponse> {
    const { data } = await api.get(`${this.baseUrl}/${transactionId}`);
    return data;
  }

  // ============ TOKENIZATION ============

  async tokenizeCard(request: TokenizeRequest): Promise<TokenizedCard> {
    const { data } = await api.post(`${this.baseUrl}/tokenize`, request);
    return data;
  }

  async getSavedCards(userId: string): Promise<TokenizedCard[]> {
    const { data } = await api.get(`${this.baseUrl}/tokens`, {
      params: { userId },
    });
    return data;
  }

  async chargeWithToken(
    cardToken: string,
    amount: number,
    currency: Currency,
  ): Promise<ChargeResponse> {
    const { data } = await api.post(`${this.baseUrl}/charge-token`, {
      cardToken,
      amount,
      currency,
    });
    return data;
  }

  async deleteCard(token: string): Promise<void> {
    await api.delete(`${this.baseUrl}/tokens/${token}`);
  }

  // ============ SUBSCRIPTIONS ============

  async createSubscription(
    request: CreateSubscriptionRequest,
  ): Promise<Subscription> {
    const { data } = await api.post("/api/subscriptions", request);
    return data;
  }

  async getSubscription(subscriptionId: string): Promise<Subscription> {
    const { data } = await api.get(`/api/subscriptions/${subscriptionId}`);
    return data;
  }

  async cancelSubscription(
    subscriptionId: string,
    reason?: string,
  ): Promise<Subscription> {
    const { data } = await api.delete(`/api/subscriptions/${subscriptionId}`, {
      data: { reason },
    });
    return data;
  }

  async updateSubscriptionPaymentMethod(
    subscriptionId: string,
    newCardToken: string,
  ): Promise<Subscription> {
    const { data } = await api.put(`/api/subscriptions/${subscriptionId}`, {
      cardToken: newCardToken,
    });
    return data;
  }
}

export const paymentService = new PaymentService();
```

### React Query Hooks

```typescript
// hooks/usePayments.ts

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { paymentService } from "@/services/payment.service";

// ============ QUERIES ============

export function useProviders() {
  return useQuery({
    queryKey: ["payment-providers"],
    queryFn: () => paymentService.getProviders(),
    staleTime: 5 * 60 * 1000, // 5 minutos
  });
}

export function useSavedCards(userId: string) {
  return useQuery({
    queryKey: ["saved-cards", userId],
    queryFn: () => paymentService.getSavedCards(userId),
    enabled: !!userId,
  });
}

export function useTransaction(transactionId: string) {
  return useQuery({
    queryKey: ["transaction", transactionId],
    queryFn: () => paymentService.getTransaction(transactionId),
    enabled: !!transactionId,
  });
}

export function useSubscription(subscriptionId: string) {
  return useQuery({
    queryKey: ["subscription", subscriptionId],
    queryFn: () => paymentService.getSubscription(subscriptionId),
    enabled: !!subscriptionId,
  });
}

// ============ MUTATIONS ============

export function useCharge() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: ChargeRequest) => paymentService.charge(request),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ["transactions"] });
      // Toast de éxito
    },
    onError: (error) => {
      // Toast de error
    },
  });
}

export function useTokenizeCard() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: TokenizeRequest) =>
      paymentService.tokenizeCard(request),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({
        queryKey: ["saved-cards", variables.userId],
      });
    },
  });
}

export function useDeleteCard() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (token: string) => paymentService.deleteCard(token),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["saved-cards"] });
    },
  });
}

export function useCreateSubscription() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateSubscriptionRequest) =>
      paymentService.createSubscription(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["subscriptions"] });
      queryClient.invalidateQueries({ queryKey: ["dealer-profile"] });
    },
  });
}

export function useCancelSubscription() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      subscriptionId,
      reason,
    }: {
      subscriptionId: string;
      reason?: string;
    }) => paymentService.cancelSubscription(subscriptionId, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["subscriptions"] });
    },
  });
}
```

---

## 🔔 Webhooks

### Endpoints de Webhook

| Proveedor | Endpoint                      | Eventos                                             |
| --------- | ----------------------------- | --------------------------------------------------- |
| AZUL      | `POST /api/webhooks/azul`     | payment.completed, payment.failed, refund.completed |
| CardNET   | `POST /api/webhooks/cardnet`  | transaction.approved, transaction.declined          |
| PixelPay  | `POST /api/webhooks/pixelpay` | charge.succeeded, charge.failed, refund.created     |
| Fygaro    | `POST /api/webhooks/fygaro`   | subscription.created, invoice.paid, invoice.failed  |
| PayPal    | `POST /api/webhooks/paypal`   | PAYMENT.CAPTURE.COMPLETED, PAYMENT.CAPTURE.DENIED   |

### Procesamiento de Webhooks

```typescript
// Backend: WebhooksController.cs (simplificado)

[HttpPost("azul")]
public async Task<IActionResult> AzulWebhook([FromBody] AzulWebhookPayload payload)
{
    // 1. Validar firma
    if (!ValidateAzulSignature(Request.Headers, payload))
        return Unauthorized();

    // 2. Procesar evento
    switch (payload.EventType)
    {
        case "payment.completed":
            await _transactionService.MarkAsApproved(payload.TransactionId);
            await _vehicleService.ActivateVehicle(payload.Metadata["vehicleId"]);
            break;

        case "payment.failed":
            await _transactionService.MarkAsFailed(payload.TransactionId, payload.ErrorCode);
            await _notificationService.SendPaymentFailedEmail(payload.UserId);
            break;

        case "refund.completed":
            await _transactionService.MarkAsRefunded(payload.TransactionId);
            break;
    }

    // 3. Responder 200 OK
    return Ok();
}
```

### Frontend: Polling de Estado

```typescript
// Para pagos que quedan en "Pending", polling hasta resolver

export function usePaymentPolling(transactionId: string) {
  const [status, setStatus] = useState<TransactionStatus>("Pending");

  useEffect(() => {
    if (!transactionId || status !== "Pending") return;

    const interval = setInterval(async () => {
      const tx = await paymentService.getTransaction(transactionId);
      setStatus(tx.status);

      if (tx.status !== "Pending" && tx.status !== "Processing") {
        clearInterval(interval);
      }
    }, 3000); // Cada 3 segundos

    return () => clearInterval(interval);
  }, [transactionId, status]);

  return status;
}
```

---

## 🔁 Suscripciones

### Planes de Dealer

| Plan           | Precio   | Precio Early Bird | Max Vehículos | Features                |
| -------------- | -------- | ----------------- | ------------- | ----------------------- |
| **Starter**    | $49/mes  | $39/mes           | 15            | Básico                  |
| **Pro** ⭐     | $129/mes | $103/mes          | 50            | Analytics + CSV         |
| **Enterprise** | $299/mes | $239/mes          | ∞             | API + CRM + White-label |

### Frecuencias Soportadas

```typescript
enum SubscriptionFrequency {
  Monthly = "Monthly",
  Quarterly = "Quarterly",
  Annually = "Annually",
}
```

### Página de Gestión de Suscripción

```tsx
// src/app/(dashboard)/billing/subscriptions/page.tsx

export default function SubscriptionsPage() {
  const { data: user } = useCurrentUser();
  const { data: subscription, isLoading } = useSubscription(
    user?.subscriptionId,
  );
  const cancelMutation = useCancelSubscription();
  const [showCancelModal, setShowCancelModal] = useState(false);

  if (isLoading) return <SubscriptionSkeleton />;

  return (
    <div className="max-w-4xl mx-auto p-6">
      <h1 className="text-2xl font-bold mb-6">Mi Suscripción</h1>

      {/* Current Plan */}
      <div className="bg-white rounded-xl shadow p-6 mb-6">
        <div className="flex justify-between items-start">
          <div>
            <h2 className="text-xl font-semibold">{subscription.planName}</h2>
            <p className="text-gray-500">
              {formatCurrency(subscription.amount, subscription.currency)}/mes
            </p>
          </div>
          <StatusBadge status={subscription.status} />
        </div>

        {/* Billing Info */}
        <div className="mt-6 grid grid-cols-2 gap-4">
          <div>
            <p className="text-sm text-gray-500">Próximo cobro</p>
            <p className="font-medium">
              {formatDate(subscription.nextBillingDate)}
            </p>
          </div>
          <div>
            <p className="text-sm text-gray-500">Método de pago</p>
            <p className="font-medium">•••• {subscription.cardLastFour}</p>
          </div>
        </div>

        {/* Actions */}
        <div className="mt-6 flex gap-3">
          <Link href="/billing/payment-methods" className="btn-secondary">
            Cambiar Tarjeta
          </Link>
          <Link href="/dealer/pricing" className="btn-secondary">
            Cambiar Plan
          </Link>
          <button
            onClick={() => setShowCancelModal(true)}
            className="text-red-600 hover:text-red-800"
          >
            Cancelar Suscripción
          </button>
        </div>
      </div>

      {/* Billing History */}
      <div className="bg-white rounded-xl shadow p-6">
        <h3 className="font-semibold mb-4">Historial de Facturación</h3>
        <BillingHistoryTable subscriptionId={subscription.subscriptionId} />
      </div>

      {/* Cancel Modal */}
      <CancelSubscriptionModal
        isOpen={showCancelModal}
        onClose={() => setShowCancelModal(false)}
        onConfirm={(reason) => {
          cancelMutation.mutate({
            subscriptionId: subscription.subscriptionId,
            reason,
          });
        }}
      />
    </div>
  );
}
```

---

## 🔒 Seguridad

### PCI DSS Compliance

```
✅ NUNCA almacenamos números completos de tarjeta
✅ Solo últimos 4 dígitos + token del proveedor
✅ CVV nunca se guarda
✅ Transmisión siempre sobre HTTPS
✅ Tokenización manejada por proveedores certificados
```

### Validación de Webhooks

```typescript
// Cada proveedor tiene su método de validación

function validateAzulSignature(headers: Headers, body: any): boolean {
  const signature = headers.get("X-Azul-Signature");
  const secret = process.env.AZUL_WEBHOOK_SECRET;
  const expectedSignature = hmacSha256(JSON.stringify(body), secret);
  return signature === expectedSignature;
}

function validatePixelPaySignature(headers: Headers, body: any): boolean {
  const signature = headers.get("X-PixelPay-Signature");
  const timestamp = headers.get("X-PixelPay-Timestamp");
  // ... similar validation
}
```

### Idempotency Keys

```typescript
// Prevenir cobros duplicados

async function charge(request: ChargeRequest) {
  const idempotencyKey = request.idempotencyKey || uuidv4();

  // Verificar si ya existe transacción con esta key
  const existing = await transactionRepo.findByIdempotencyKey(idempotencyKey);
  if (existing) {
    return existing; // Retornar transacción existente
  }

  // Procesar nuevo cobro
  const result = await provider.ChargeAsync(request);
  result.idempotencyKey = idempotencyKey;
  await transactionRepo.save(result);

  return result;
}
```

---

## 🧪 Testing

### Test Cards por Proveedor

| Proveedor    | Número                | CVV | Exp   | Resultado   |
| ------------ | --------------------- | --- | ----- | ----------- |
| **AZUL**     | 4111111111111111      | 123 | 12/25 | ✅ Approved |
| **AZUL**     | 4000000000000002      | 123 | 12/25 | ❌ Declined |
| **PixelPay** | 4242424242424242      | 123 | 12/26 | ✅ Approved |
| **PixelPay** | 4000000000000069      | 123 | 12/26 | ❌ Expired  |
| **PayPal**   | N/A (sandbox account) | -   | -     | ✅ Sandbox  |

### Componente de Testing

```tsx
// Solo visible en desarrollo

{
  process.env.NODE_ENV === "development" && (
    <div className="bg-yellow-100 border border-yellow-300 rounded p-4 mb-4">
      <h4 className="font-bold mb-2">🧪 Test Cards</h4>
      <div className="grid grid-cols-2 gap-2 text-sm">
        <button
          onClick={() => fillTestCard("success")}
          className="bg-green-100 p-2 rounded"
        >
          ✅ Aprobar: 4111...1111
        </button>
        <button
          onClick={() => fillTestCard("decline")}
          className="bg-red-100 p-2 rounded"
        >
          ❌ Rechazar: 4000...0002
        </button>
      </div>
    </div>
  );
}
```

---

## 📱 Responsive Design

### Checkout Mobile

```tsx
// Layout responsive para checkout

<div className="min-h-screen bg-gray-50">
  <div className="max-w-7xl mx-auto px-4 py-8">
    <div className="grid lg:grid-cols-3 gap-8">

      {/* Form Section - Full width en mobile, 2/3 en desktop */}
      <div className="lg:col-span-2 order-2 lg:order-1">
        <PaymentMethodSelector ... />
        <CreditCardForm ... />
      </div>

      {/* Summary Section - Sticky en desktop, arriba en mobile */}
      <div className="lg:col-span-1 order-1 lg:order-2">
        <div className="lg:sticky lg:top-4">
          <CheckoutSummary ... />
        </div>
      </div>

    </div>
  </div>
</div>
```

---

## 📊 Analytics y Métricas

### Eventos a Trackear

```typescript
// Google Analytics / Segment events

// Checkout iniciado
analytics.track("Checkout Started", {
  productType: "listing",
  amount: 29,
  currency: "USD",
});

// Método de pago seleccionado
analytics.track("Payment Method Selected", {
  gateway: "PixelPay",
  hasSavedCard: true,
});

// Pago completado
analytics.track("Payment Completed", {
  transactionId: "xxx",
  gateway: "PixelPay",
  amount: 29,
  currency: "USD",
  productType: "listing",
});

// Pago fallido
analytics.track("Payment Failed", {
  gateway: "Azul",
  errorCode: "card_declined",
  amount: 29,
});
```

---

## 📚 Referencias

- [PaymentService README](/backend/PaymentService/README.md)
- [AZUL API Docs](https://azul.com/api)
- [PixelPay API Docs](https://pixelpay.com/api)
- [PayPal Developer](https://developer.paypal.com)
- [Fygaro API](https://fygaro.com/api)

---

## ✅ Checklist de Implementación

### Backend ✅

- [x] PaymentService con 5 proveedores
- [x] Factory + Registry + Strategy pattern
- [x] Webhooks por proveedor
- [x] Tokenización
- [x] Suscripciones

### Frontend 🚧

- [ ] PaymentMethodSelector component
- [ ] CreditCardForm component
- [ ] CheckoutSummary component
- [ ] SavedPaymentMethods component
- [ ] SubscriptionPlanCard component
- [ ] Checkout pages (/checkout/\*)
- [ ] Billing dashboard (/billing/\*)
- [ ] React Query hooks
- [ ] Test mode UI

### Integración 🚧

- [ ] Gateway routes en ocelot.prod.json
- [ ] Environment variables
- [ ] Webhook endpoints configurados
- [ ] Error handling global

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/checkout.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsUser } from "../helpers/auth";

test.describe("Checkout & Pagos", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test.describe("Checkout Flow - Publicación Individual", () => {
    test("debe mostrar resumen de compra", async ({ page }) => {
      await page.goto("/checkout/listing?plan=featured");

      await expect(page.getByTestId("checkout-summary")).toBeVisible();
      await expect(page.getByText(/publicación destacada/i)).toBeVisible();
      await expect(page.getByTestId("total-amount")).toBeVisible();
    });

    test("debe mostrar opciones de pago", async ({ page }) => {
      await page.goto("/checkout/listing?plan=featured");

      await expect(page.getByText(/tarjeta de crédito/i)).toBeVisible();
      await expect(page.getByText(/paypal/i)).toBeVisible();
      await expect(page.getByText(/azul/i)).toBeVisible();
    });

    test("debe validar formulario de tarjeta", async ({ page }) => {
      await page.goto("/checkout/listing?plan=featured");

      await page.getByRole("radio", { name: /tarjeta/i }).click();
      await page.getByRole("button", { name: /pagar/i }).click();

      await expect(
        page.getByText(/número de tarjeta es requerido/i),
      ).toBeVisible();
    });

    test("debe completar pago con tarjeta de prueba", async ({ page }) => {
      await page.goto("/checkout/listing?plan=featured");

      // Seleccionar tarjeta
      await page.getByRole("radio", { name: /tarjeta/i }).click();

      // Llenar formulario (usando tarjeta de prueba)
      await page.fill('[data-testid="card-number"]', "4111111111111111");
      await page.fill('[data-testid="card-expiry"]', "12/28");
      await page.fill('[data-testid="card-cvc"]', "123");
      await page.fill('[data-testid="card-holder"]', "Test User");

      await page.getByRole("button", { name: /pagar/i }).click();

      // Esperar confirmación
      await expect(page).toHaveURL(/\/checkout\/success/);
      await expect(page.getByText(/pago exitoso/i)).toBeVisible();
    });

    test("debe manejar error de pago", async ({ page }) => {
      await page.goto("/checkout/listing?plan=featured");

      await page.getByRole("radio", { name: /tarjeta/i }).click();

      // Usar tarjeta que falla
      await page.fill('[data-testid="card-number"]', "4000000000000002");
      await page.fill('[data-testid="card-expiry"]', "12/28");
      await page.fill('[data-testid="card-cvc"]', "123");
      await page.fill('[data-testid="card-holder"]', "Test User");

      await page.getByRole("button", { name: /pagar/i }).click();

      await expect(page.getByText(/pago rechazado/i)).toBeVisible();
    });
  });

  test.describe("Checkout Flow - Suscripción Dealer", () => {
    test("debe mostrar planes de suscripción", async ({ page }) => {
      await page.goto("/checkout/subscription");

      await expect(page.getByTestId("plan-starter")).toBeVisible();
      await expect(page.getByTestId("plan-pro")).toBeVisible();
      await expect(page.getByTestId("plan-enterprise")).toBeVisible();
    });

    test("debe seleccionar plan y continuar", async ({ page }) => {
      await page.goto("/checkout/subscription");

      await page
        .getByTestId("plan-pro")
        .getByRole("button", { name: /seleccionar/i })
        .click();

      await expect(page.getByTestId("checkout-summary")).toContainText("Pro");
      await expect(page.getByTestId("total-amount")).toContainText("$129");
    });

    test("debe aplicar código de descuento", async ({ page }) => {
      await page.goto("/checkout/subscription");
      await page
        .getByTestId("plan-pro")
        .getByRole("button", { name: /seleccionar/i })
        .click();

      await page.fill('[data-testid="promo-code"]', "LAUNCH20");
      await page.getByRole("button", { name: /aplicar/i }).click();

      await expect(page.getByText(/20% descuento/i)).toBeVisible();
      await expect(page.getByTestId("discounted-amount")).toBeVisible();
    });
  });

  test.describe("Métodos de Pago Guardados", () => {
    test("debe mostrar tarjetas guardadas", async ({ page }) => {
      await page.goto("/checkout/listing?plan=featured");

      await expect(page.getByTestId("saved-cards")).toBeVisible();
      await expect(page.getByText(/visa \*\*\*\*4242/i)).toBeVisible();
    });

    test("debe pagar con tarjeta guardada", async ({ page }) => {
      await page.goto("/checkout/listing?plan=featured");

      await page.getByTestId("saved-card-1").click();
      await page.getByRole("button", { name: /pagar/i }).click();

      await expect(page).toHaveURL(/\/checkout\/success/);
    });
  });

  test.describe("Billing Dashboard", () => {
    test("debe mostrar historial de facturas", async ({ page }) => {
      await page.goto("/billing");

      await expect(
        page.getByRole("heading", { name: /facturación/i }),
      ).toBeVisible();
      await expect(page.getByTestId("invoices-table")).toBeVisible();
    });

    test("debe descargar factura PDF", async ({ page }) => {
      await page.goto("/billing");

      const downloadPromise = page.waitForEvent("download");
      await page
        .getByRole("button", { name: /descargar pdf/i })
        .first()
        .click();
      const download = await downloadPromise;

      expect(download.suggestedFilename()).toMatch(/factura.*\.pdf/i);
    });

    test("debe gestionar métodos de pago", async ({ page }) => {
      await page.goto("/billing/payment-methods");

      await expect(
        page.getByRole("heading", { name: /métodos de pago/i }),
      ).toBeVisible();
      await expect(
        page.getByRole("button", { name: /agregar tarjeta/i }),
      ).toBeVisible();
    });
  });
});
```

---

**Última actualización:** Enero 28, 2026  
**Autor:** OKLA Team  
**Versión:** 2.0.0 (Multi-Proveedor)
