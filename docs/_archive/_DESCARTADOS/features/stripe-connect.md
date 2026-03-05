# 💳 Stripe Connect - DESCARTADO

> **Estado:** ❌ DESCARTADO  
> **Fecha de Descarte:** Enero 23, 2026  
> **Razón:** No aplica al modelo de negocio de OKLA

---

## ⚠️ FEATURE DESCARTADO

Stripe Connect fue planificado asumiendo incorrectamente que OKLA sería un **marketplace transaccional** donde la plataforma procesaría pagos de vehículos y cobraría comisiones.

---

## 📋 Descripción Original

Stripe Connect es una solución de pagos para marketplaces que permite:

- Onboarding de comerciantes (dealers)
- Split payments (dividir pagos entre plataforma y vendedor)
- Transferencias automáticas a vendedores
- KYC/verificación de identidad de comerciantes

---

## ❌ Funcionalidades Planificadas (NO IMPLEMENTAR)

### 3.1 Stripe Connect Setup ❌

- ~~Crear Stripe Connect account~~
- ~~Onboarding de dealers (KYC)~~
- ~~Verificar bank account info~~
- ~~Configurar split payments (70% dealer, 30% plataforma)~~

### 3.2 Payment Transfers ❌

- ~~Direct charges (plataforma cobra, transfiere a dealer)~~
- ~~Destination charges (dealer cobra, comisión a plataforma)~~
- ~~Transferir fondos a dealers~~
- ~~Dashboard de earnings para dealers~~

### 3.3 Comisiones ❌

- ~~Calcular comisión por venta (ej: 5%)~~
- ~~Retener comisión en cada transacción~~
- ~~Reportes de comisiones~~
- ~~Invoices de comisiones a dealers~~

---

## ❌ Por Qué NO Aplica

| Funcionalidad Stripe Connect | Por qué NO aplica a OKLA                       |
| ---------------------------- | ---------------------------------------------- |
| Onboarding de comerciantes   | Dealers son CLIENTES de OKLA, no comerciantes  |
| Split payments               | No hay pagos de vehículos que dividir          |
| Transferencias a dealers     | OKLA no procesa ventas de vehículos            |
| Comisiones por transacción   | OKLA cobra suscripción fija mensual            |
| Dashboard de earnings        | Dealers no tienen "earnings" en OKLA           |
| KYC para pagos               | No se requiere, dealers solo pagan suscripción |

---

## ✅ Alternativa Implementada

En lugar de Stripe Connect, OKLA utiliza:

### Azul (Banco Popular) - DEFAULT

```typescript
// OKLA es el MERCHANT, dealers son CLIENTES
const subscription = await azulPaymentService.createSubscription({
  customerId: dealer.id, // Dealer como cliente
  planName: "Professional",
  amount: 5900, // RD$5,900/mes
  currency: "DOP",
  merchantId: OKLA_MERCHANT_ID, // OKLA recibe el pago
});
```

### Stripe (Backup para tarjetas internacionales)

```typescript
// Stripe básico para suscripciones, NO Connect
const subscription = await stripe.subscriptions.create({
  customer: stripeCustomerId, // Dealer como cliente
  items: [{ price: "price_professional" }],
  // NO hay connect_account, OKLA recibe todo
});
```

---

## 🔄 Flujo Correcto vs Descartado

### ❌ Flujo Descartado (Stripe Connect)

```
COMPRADOR ─[Paga $20,000]─> STRIPE CONNECT
                               │
                      ┌────────┴────────┐
                      │                 │
                      ▼                 ▼
              OKLA ($1,000)      DEALER ($19,000)
              (5% comisión)      (95% del pago)
```

### ✅ Flujo Correcto (Actual)

```
DEALER ─[Paga RD$5,900/mes]─> OKLA (vía Azul/Stripe)
              │
              └── Suscripción mensual por publicidad

COMPRADOR ─[Paga vehículo]─> DEALER (DIRECTO, fuera de OKLA)
              │
              └── Transferencia bancaria/efectivo/cheque
```

---

## 📊 Comparación de Modelos

| Aspecto             | Stripe Connect (Descartado)       | Modelo Actual            |
| ------------------- | --------------------------------- | ------------------------ |
| **OKLA recibe**     | Comisión 5-10% por venta          | Suscripción fija mensual |
| **Dealers reciben** | Pagos de vehículos menos comisión | Nada vía OKLA            |
| **Complejidad**     | Alta (KYC, verificación bancaria) | Baja (solo cobro)        |
| **Riesgo legal**    | Alto (regulación financiera)      | Bajo (solo publicidad)   |
| **Flujo de dinero** | OKLA intermedia en ventas         | Solo suscripciones       |

---

## 📚 Referencias

- Documentación original: `docs/api/payments/STRIPE_ROADMAP.md` (Fase 3 marcada como descartada)
- Modelo correcto: `docs/process-matrix/02-USUARIOS-DEALERS/04-dealer-onboarding.md`
