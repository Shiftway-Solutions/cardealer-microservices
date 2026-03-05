# 💵 Comisiones por Venta - DESCARTADO

> **Estado:** ❌ DESCARTADO  
> **Fecha de Descarte:** Enero 23, 2026  
> **Razón:** OKLA no participa en transacciones de vehículos

---

## ⚠️ FEATURE DESCARTADO

El sistema de comisiones por venta fue planificado asumiendo que OKLA cobraría un porcentaje de cada transacción de vehículo.

---

## 📋 Descripción Original

- OKLA cobraría 5-10% del precio de venta de cada vehículo
- Se calcularía automáticamente al completar una venta
- Se descontaría del pago antes de transferir al vendedor
- Generaría facturas de comisión a los dealers

---

## ❌ Flujo Planificado (NO IMPLEMENTAR)

```
❌ FLUJO DESCARTADO

Vehículo vendido: $20,000
├── Comisión OKLA (5%): $1,000
└── Dealer recibe: $19,000

Total transacciones mes: $500,000
├── Comisión OKLA: $25,000
└── Dealers reciben: $475,000
```

---

## ❌ Por Qué NO Aplica

| Aspecto                        | Modelo Comisión (Descartado) | Modelo Actual         |
| ------------------------------ | ---------------------------- | --------------------- |
| **OKLA participa en venta**    | Sí                           | No                    |
| **OKLA procesa pago vehículo** | Sí                           | No                    |
| **Ingreso variable**           | Sí (% de ventas)             | No (suscripción fija) |
| **Riesgo de fraude**           | Alto                         | Bajo                  |
| **Regulación financiera**      | Requerida                    | No requerida          |
| **Complejidad legal**          | Alta                         | Baja                  |

---

## ✅ Modelo de Ingresos Correcto

### Suscripciones Mensuales (Ingresos Fijos)

| Plan         | Precio/Mes | Vehículos | Proyección 100 dealers |
| ------------ | ---------- | --------- | ---------------------- |
| Starter      | RD$2,900   | 10        | RD$290,000/mes         |
| Professional | RD$5,900   | 50        | RD$590,000/mes         |
| Enterprise   | RD$14,900  | Ilimitado | RD$1,490,000/mes       |

### Servicios Adicionales (Opcionales)

| Servicio               | Precio       | Descripción               |
| ---------------------- | ------------ | ------------------------- |
| Listado Destacado      | RD$500-2,000 | Aparecer en homepage      |
| Publicación Individual | RD$1,500     | Para sellers individuales |
| Renovación de Listing  | RD$500       | Extender 30 días          |

---

## 💡 Ventajas del Modelo de Suscripción

| Ventaja                  | Descripción                             |
| ------------------------ | --------------------------------------- |
| **Ingresos predecibles** | MRR (Monthly Recurring Revenue) estable |
| **Sin riesgo de fraude** | No manejamos dinero de ventas           |
| **Menor regulación**     | Solo cobramos por publicidad            |
| **Escalabilidad**        | Más dealers = más ingresos lineales     |
| **Simplicidad legal**    | Contrato de servicio simple             |

---

## 📚 Referencia

- Planes de suscripción: `docs/process-matrix/02-USUARIOS-DEALERS/04-dealer-onboarding.md`
- Integración de cobros: `docs/process-matrix/05-PAGOS-FACTURACION/03-azul-payment.md`
