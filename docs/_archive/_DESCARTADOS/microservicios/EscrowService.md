# 🔐 EscrowService - DESCARTADO

> **Estado:** ❌ DESCARTADO  
> **Puerto Planificado:** 5047  
> **Fecha de Descarte:** Enero 23, 2026  
> **Razón:** No aplica al modelo de negocio de OKLA

---

## ⚠️ SERVICIO DESCARTADO

Este servicio fue planificado asumiendo incorrectamente que OKLA sería un **marketplace transaccional** donde la plataforma procesaría pagos de vehículos entre compradores y vendedores.

---

## 📋 Descripción Original

Sistema de pagos en garantía (escrow) para transacciones de alto valor entre compradores y vendedores. El dinero se retendría hasta que ambas partes confirmaran la transacción satisfactoria.

### Flujo Original (Descartado)

```
❌ FLUJO DESCARTADO - NO IMPLEMENTAR

   COMPRADOR                 OKLA (Escrow)              VENDEDOR
   ─────────                ─────────────               ────────

   1. Inicia compra
      └──────────────> 2. Crea Escrow Account
                              │
   3. Deposita fondos         │
      └──────────────> 4. Retiene dinero
                              │
                       5. Notifica pago ────────────>
                              │
                              │        6. Entrega vehículo
                              │  <────────────────────
                              │
   7. Confirma recepción      │
      └──────────────> 8. Verifica ambas partes
                              │
                       9. Libera fondos ────────────>  10. Recibe pago
```

---

## ❌ Por Qué NO Aplica

| Característica de Escrow     | Por qué NO aplica a OKLA                  |
| ---------------------------- | ----------------------------------------- |
| Retener dinero del comprador | OKLA no recibe dinero de compradores      |
| Liberar fondos al vendedor   | OKLA no transfiere dinero a vendedores    |
| Disputas de transacción      | Las disputas son entre comprador-vendedor |
| Comisión por transacción     | OKLA cobra suscripción fija, no comisión  |
| Verificación de entrega      | La entrega es entre comprador-vendedor    |

---

## ✅ Modelo Correcto de OKLA

```
┌────────────────────────────────────────────────────────────────────────┐
│                   MODELO DE NEGOCIO OKLA                               │
├────────────────────────────────────────────────────────────────────────┤
│                                                                        │
│   OKLA ES UNA PLATAFORMA DE PUBLICIDAD, NO UN MARKETPLACE              │
│   ═══════════════════════════════════════════════════════              │
│                                                                        │
│   ✅ Dealers PAGAN a OKLA: Suscripción mensual RD$2,900-14,900        │
│   ✅ Sellers PAGAN a OKLA: Publicación única RD$1,500                  │
│                                                                        │
│   ❌ OKLA NO procesa pagos de vehículos                                │
│   ❌ OKLA NO retiene dinero de compradores                             │
│   ❌ OKLA NO transfiere dinero a vendedores                            │
│   ❌ OKLA NO cobra comisión por ventas                                 │
│                                                                        │
│   LA TRANSACCIÓN DEL VEHÍCULO OCURRE DIRECTAMENTE:                     │
│   Comprador ───[Paga en efectivo/banco]───> Vendedor                  │
│                                                                        │
│   OKLA solo conecta compradores con vendedores (publicidad)            │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 📊 Endpoints Planificados (NO IMPLEMENTADOS)

Los siguientes endpoints fueron planificados pero **NO se implementarán**:

| Método | Endpoint                            | Descripción         |
| ------ | ----------------------------------- | ------------------- |
| `POST` | `/api/escrow`                       | Crear escrow        |
| `GET`  | `/api/escrow/{id}`                  | Obtener escrow      |
| `POST` | `/api/escrow/{id}/fund`             | Depositar fondos    |
| `POST` | `/api/escrow/{id}/confirm-delivery` | Confirmar entrega   |
| `POST` | `/api/escrow/{id}/confirm-receipt`  | Confirmar recepción |
| `POST` | `/api/escrow/{id}/release`          | Liberar fondos      |
| `POST` | `/api/escrow/{id}/dispute`          | Abrir disputa       |

---

## 🔄 Alternativas para Compradores

Si OKLA quisiera ofrecer protección a compradores en el futuro, las opciones serían:

1. **Partnership con servicio de escrow externo** (ej: Escrow.com)
2. **Verificación pre-compra** (inspección mecánica, historial)
3. **Garantía OKLA limitada** (cobertura post-venta con seguro)

Pero ninguna de estas implica que OKLA procese pagos de vehículos.

---

## 📁 Archivos Relacionados (No Creados)

Los siguientes archivos nunca fueron creados:

- `EscrowService.Api/`
- `EscrowService.Application/`
- `EscrowService.Domain/`
- `EscrowService.Infrastructure/`
- `EscrowService.Tests/`

---

## 📚 Referencia

- Documentación original: `docs/process-matrix/05-PAGOS-FACTURACION/05-escrow-service.md` (marcado como descartado)
