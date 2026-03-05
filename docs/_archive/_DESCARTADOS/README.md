# 🗑️ Documentación y Servicios Descartados

> **Última actualización:** Enero 23, 2026  
> **Razón principal:** Corrección del modelo de negocio de OKLA

---

## ⚠️ IMPORTANTE

Los elementos en esta carpeta fueron **planificados incorrectamente** asumiendo que OKLA sería un **marketplace transaccional** donde la plataforma procesaría pagos de vehículos.

### Modelo Incorrecto (Descartado)

```
❌ COMPRADOR ───[Paga vehículo]───> OKLA ───[Transfiere]───> VENDEDOR
                                    │
                                    └── Cobra comisión 5-10%
```

### Modelo Correcto (Actual)

```
✅ OKLA ES PLATAFORMA DE PUBLICIDAD

   DEALER/SELLER ───[Paga suscripción RD$2,900-14,900/mes]───> OKLA

   COMPRADOR ───[Paga vehículo DIRECTAMENTE]───> VENDEDOR
                    (Fuera de OKLA)
```

---

## 📋 Índice de Elementos Descartados

### 🔴 Microservicios Descartados

| Servicio          | Puerto | Razón de Descarte                  | Ubicación                             |
| ----------------- | ------ | ---------------------------------- | ------------------------------------- |
| **EscrowService** | 5047   | OKLA no procesa pagos de vehículos | `backend/_DESCARTADOS/EscrowService/` |

> **NOTA:** El código fuente fue movido a `backend/_DESCARTADOS/`

### 📄 Documentación Descartada

| Documento               | Ubicación Original                     | Razón                             |
| ----------------------- | -------------------------------------- | --------------------------------- |
| `05-escrow-service.md`  | `process-matrix/05-PAGOS-FACTURACION/` | No aplica al modelo de publicidad |
| Stripe Connect (Fase 3) | `api/payments/STRIPE_ROADMAP.md`       | No hay split payments con dealers |

### ❌ Features/Funcionalidades Descartadas

| Feature                   | Descripción                       | Razón                              |
| ------------------------- | --------------------------------- | ---------------------------------- |
| **Stripe Connect**        | Marketplace con split payments    | Dealers no reciben pagos vía OKLA  |
| **Split Payments**        | 70% dealer / 30% OKLA             | No hay transacciones de vehículos  |
| **Escrow/Garantía**       | Retener dinero de compradores     | OKLA no procesa pagos de vehículos |
| **Comisión por Venta**    | 5-10% por transacción             | OKLA cobra suscripción fija        |
| **Dealer KYC para pagos** | Onboarding de comercios en Stripe | Dealers son clientes, no comercios |
| **Dashboard de Earnings** | Ganancias de dealers              | No aplica, dealers pagan a OKLA    |

---

## 📁 Estructura de Esta Carpeta

```
_DESCARTADOS/
├── README.md                    # Este archivo
├── microservicios/
│   └── EscrowService.md         # Documentación archivada
├── features/
│   ├── stripe-connect.md        # Stripe Connect descartado
│   ├── split-payments.md        # Split payments descartado
│   └── comisiones-venta.md      # Comisiones descartadas
└── referencias/
    └── modelo-marketplace-original.md  # Modelo incorrecto original
```

---

## 🔄 Alternativas Implementadas

| Descartado            | Alternativa                                                |
| --------------------- | ---------------------------------------------------------- |
| Stripe Connect        | Stripe básico (backup) + Azul (default) para suscripciones |
| Split Payments        | Cobro único de suscripción mensual a dealers               |
| Escrow                | N/A - Transacciones fuera de OKLA                          |
| Comisiones por venta  | Suscripción fija mensual                                   |
| Dashboard de Earnings | Dashboard de métricas de listings                          |

---

## 📚 Referencias

- [Modelo de Negocio Correcto](../process-matrix/02-USUARIOS-DEALERS/04-dealer-onboarding.md)
- [Integración Azul (Cobro de Suscripciones)](../process-matrix/05-PAGOS-FACTURACION/03-azul-payment.md)
- [Estrategia de Tipos de Usuario](../ESTRATEGIA_TIPOS_USUARIO_DEALERS.md)

---

**Nota:** Estos elementos se mantienen archivados para referencia histórica y para evitar que se vuelvan a proponer incorrectamente en el futuro.
