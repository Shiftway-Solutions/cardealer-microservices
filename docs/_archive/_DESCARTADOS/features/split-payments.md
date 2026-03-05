# 💰 Split Payments - DESCARTADO

> **Estado:** ❌ DESCARTADO  
> **Fecha de Descarte:** Enero 23, 2026  
> **Razón:** OKLA no procesa pagos de vehículos

---

## ⚠️ FEATURE DESCARTADO

Los Split Payments fueron planificados asumiendo que OKLA procesaría pagos de vehículos y dividiría automáticamente entre la plataforma y el vendedor.

---

## 📋 Descripción Original

Sistema para dividir pagos automáticamente:

- 70-95% va al dealer/vendedor
- 5-30% va a OKLA como comisión
- Transferencia automática a cuenta bancaria del dealer

---

## ❌ Configuración Planificada (NO IMPLEMENTAR)

```typescript
// ❌ ESTO NO SE IMPLEMENTARÁ

const payment = await stripe.paymentIntents.create({
  amount: 2000000, // $20,000 USD (precio vehículo)
  currency: "usd",
  application_fee_amount: 100000, // $1,000 (5% comisión OKLA)
  transfer_data: {
    destination: dealerStripeAccountId, // ❌ Dealers NO tienen cuenta Stripe
  },
});
```

---

## ❌ Por Qué NO Aplica

| Concepto                        | Realidad de OKLA                      |
| ------------------------------- | ------------------------------------- |
| **División de pagos**           | No hay pagos que dividir              |
| **Comisión por venta**          | OKLA cobra suscripción fija           |
| **Cuenta del dealer en Stripe** | Dealers son clientes, no comerciantes |
| **Transferencias a dealers**    | No existen, dealers pagan a OKLA      |

---

## ✅ Modelo Correcto

```
┌─────────────────────────────────────────────────────────────┐
│                NO HAY SPLIT PAYMENTS EN OKLA                │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   DEALER ───[Paga RD$5,900/mes]───> OKLA                   │
│                                      │                      │
│                           100% va a OKLA                    │
│                           (no hay split)                    │
│                                                             │
│   COMPRADOR ───[Paga vehículo]───> DEALER                  │
│                                      │                      │
│                           100% va al dealer                 │
│                           (fuera de OKLA)                   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 📊 Ingresos de OKLA (Modelo Correcto)

| Fuente de Ingreso        | Cantidad     | Frecuencia   |
| ------------------------ | ------------ | ------------ |
| Suscripción Starter      | RD$2,900     | Mensual      |
| Suscripción Professional | RD$5,900     | Mensual      |
| Suscripción Enterprise   | RD$14,900    | Mensual      |
| Publicación Individual   | RD$1,500     | Por vehículo |
| Listados Destacados      | RD$500-2,000 | Opcional     |

**NO hay comisiones por venta de vehículos.**
