# 🏪 Modelo Marketplace Original - DESCARTADO

> **Estado:** ❌ DESCARTADO  
> **Fecha de Descarte:** Enero 23, 2026  
> **Razón:** OKLA es plataforma de publicidad, no marketplace transaccional

---

## ⚠️ MODELO DESCARTADO

Este documento describe el modelo de negocio original que fue **incorrectamente planificado** para OKLA. Se mantiene como referencia histórica para evitar que se vuelva a proponer.

---

## 📋 Modelo Original (Incorrecto)

### Flujo de Transacción Propuesto

```
❌ MODELO DESCARTADO - NO IMPLEMENTAR

┌────────────────────────────────────────────────────────────────────────┐
│                    FLUJO MARKETPLACE (INCORRECTO)                      │
├────────────────────────────────────────────────────────────────────────┤
│                                                                        │
│   COMPRADOR                    OKLA                     VENDEDOR       │
│   ─────────                   ─────                    ────────        │
│                                                                        │
│   1. Encuentra vehículo                                               │
│   2. Click "Comprar"                                                  │
│   3. Paga $20,000 ─────────> 4. Retiene dinero                       │
│                                    │                                  │
│                               5. Cobra comisión 5%                    │
│                                    │                                  │
│                               6. Transfiere $19,000 ───────> Recibe  │
│                                                                        │
│   OKLA recibe: $1,000 (comisión)                                      │
│   Vendedor recibe: $19,000                                            │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

### Servicios Planificados (Incorrectos)

| Servicio          | Función                    | Estado        |
| ----------------- | -------------------------- | ------------- |
| EscrowService     | Retener pagos de vehículos | ❌ Descartado |
| Stripe Connect    | Split payments a dealers   | ❌ Descartado |
| ComisionesService | Calcular % por venta       | ❌ Descartado |
| TransferService   | Transferir a dealers       | ❌ Descartado |
| DisputasService   | Mediar en conflictos       | ❌ Descartado |

---

## ❌ Problemas del Modelo Marketplace

| Problema                  | Impacto                                 |
| ------------------------- | --------------------------------------- |
| **Regulación financiera** | Requiere licencia bancaria/fintech      |
| **Riesgo de fraude**      | Responsabilidad legal por transacciones |
| **Complejidad operativa** | Soporte para disputas, reembolsos       |
| **Capital requerido**     | Retención de fondos requiere liquidez   |
| **KYC/AML**               | Verificación de identidad obligatoria   |
| **Impuestos**             | Retención de ITBIS en cada transacción  |

---

## ✅ Modelo Correcto (Actual)

### OKLA como Plataforma de Publicidad

```
┌────────────────────────────────────────────────────────────────────────┐
│                    MODELO PUBLICIDAD (CORRECTO)                        │
├────────────────────────────────────────────────────────────────────────┤
│                                                                        │
│   OKLA ES COMO COROTOS.COM.DO O CLASIFICADOS                          │
│   ═════════════════════════════════════════════                        │
│                                                                        │
│   DEALER ───[Paga suscripción RD$2,900-14,900/mes]───> OKLA          │
│                                                                        │
│   SELLER INDIVIDUAL ───[Paga RD$1,500 por anuncio]───> OKLA          │
│                                                                        │
│   OKLA provee:                                                        │
│   • Plataforma para publicar vehículos                                │
│   • Búsqueda y filtros para compradores                               │
│   • Contacto entre comprador y vendedor                               │
│   • Estadísticas de vistas y leads                                    │
│                                                                        │
│   OKLA NO participa en:                                               │
│   • Pago del vehículo                                                 │
│   • Entrega del vehículo                                              │
│   • Garantías post-venta                                              │
│   • Disputas de transacción                                           │
│                                                                        │
│   COMPRADOR ───[Contacta]───> VENDEDOR ───[Negocian]───> VENTA       │
│                                             (fuera de OKLA)           │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

### Comparación de Modelos

| Aspecto             | Marketplace (Descartado) | Publicidad (Actual)  |
| ------------------- | ------------------------ | -------------------- |
| **Ingresos**        | % de ventas (variable)   | Suscripciones (fijo) |
| **Riesgo legal**    | Alto                     | Bajo                 |
| **Regulación**      | Fintech/Bancaria         | Servicios digitales  |
| **Capital inicial** | Alto                     | Bajo                 |
| **Complejidad**     | Alta                     | Baja                 |
| **Escalabilidad**   | Depende de ventas        | Depende de dealers   |

---

## 📊 Proyección de Ingresos (Modelo Correcto)

### Año 1 - Proyección Conservadora

| Mes | Dealers | MRR (RD$) | MRR (USD aprox.) |
| --- | ------- | --------- | ---------------- |
| 1   | 10      | 59,000    | $1,000           |
| 3   | 30      | 177,000   | $3,000           |
| 6   | 75      | 442,500   | $7,500           |
| 12  | 150     | 885,000   | $15,000          |

### Año 2 - Proyección Optimista

| Mes | Dealers | MRR (RD$) | MRR (USD aprox.) |
| --- | ------- | --------- | ---------------- |
| 18  | 300     | 1,770,000 | $30,000          |
| 24  | 500     | 2,950,000 | $50,000          |

---

## 💡 Lección Aprendida

**OKLA debe mantenerse como plataforma de publicidad:**

1. ✅ Menor riesgo legal y regulatorio
2. ✅ Ingresos predecibles (MRR)
3. ✅ Operación más simple
4. ✅ Menor capital requerido
5. ✅ Escalabilidad lineal con dealers
6. ✅ Sin responsabilidad por transacciones

**Evitar convertirse en marketplace transaccional:**

1. ❌ No procesar pagos de vehículos
2. ❌ No retener dinero de compradores
3. ❌ No transferir dinero a vendedores
4. ❌ No cobrar comisiones por venta
5. ❌ No mediar en disputas de transacción

---

## 📚 Referencias

- [Modelo de Negocio Actual](../../ESTRATEGIA_TIPOS_USUARIO_DEALERS.md)
- [Integración de Cobros Azul](../../process-matrix/05-PAGOS-FACTURACION/03-azul-payment.md)
- [Onboarding de Dealers](../../process-matrix/02-USUARIOS-DEALERS/04-dealer-onboarding.md)
