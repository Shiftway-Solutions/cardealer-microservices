# 📧 Verificación: ¿Se envía factura por email al pagar?

> **Fecha:** 2026-03-05  
> **Pregunta:** ¿El sistema le envía al usuario un correo con la factura por pago de membresía y publicidad?

---

## Respuesta Corta

**PARCIALMENTE.** El sistema envía un **recibo de pago** (payment receipt), pero **NO envía una factura formal** con número fiscal, desglose de ITBIS ni cumplimiento con la DGII.

---

## ¿Qué existe hoy?

### ✅ Lo que SÍ funciona

| Componente                   | Estado      | Descripción                                                    |
| ---------------------------- | ----------- | -------------------------------------------------------------- |
| **Recibo de pago por email** | ✅ Funciona | Se envía email con asunto "Recibo de Pago - $XX.XX USD"        |
| **Notificación in-app**      | ✅ Funciona | Se crea notificación dentro de la plataforma                   |
| **Alerta a admin**           | ✅ Funciona | Admin recibe alerta de pago recibido                           |
| **Invoice en base de datos** | ✅ Funciona | BillingService crea registro de Invoice con lifecycle completo |

### Flujo actual:

```
Stripe Webhook (payment_intent.succeeded)
  → BillingService: Publica PaymentCompletedEvent (RabbitMQ)
    → NotificationService: PaymentReceiptNotificationConsumer
      → Envía email "Recibo de Pago" al usuario
      → Crea notificación in-app
      → Envía alerta a admin
```

### ❌ Lo que FALTA

| Gap                                                 | Severidad | Descripción                                                               |
| --------------------------------------------------- | --------- | ------------------------------------------------------------------------- |
| **No se envía factura formal (Invoice)**            | 🔴 Alta   | Solo se envía recibo, no factura con # de invoice, líneas, impuestos      |
| **Evento `InvoiceGeneratedEvent` nunca se publica** | 🟡 Media  | El contrato existe en `CarDealer.Contracts` pero nadie lo publica         |
| **No existe template de email para facturas**       | 🟡 Media  | Solo existe template para recibo de pago (y es HTML inline, no archivo)   |
| **No se adjunta PDF de Stripe**                     | 🟡 Media  | Stripe genera PDF con URL, se guarda en DB pero nunca se envía al usuario |
| **No hay NCF (factura fiscal DGII)**                | 🔴 Alta   | Requerido por ley dominicana para facturación electrónica                 |
| **No hay desglose de ITBIS**                        | 🔴 Alta   | El recibo no muestra el 18% de ITBIS separado                             |
| **Notificación de pago fallido falta**              | 🟡 Media  | `payment_intent.payment_failed` no envía notificación                     |
| **Notificación de factura próxima falta**           | 🟠 Baja   | `invoice.upcoming` handler tiene TODO sin implementar                     |

---

## ¿Qué contiene el recibo actual?

El email de "Recibo de Pago" incluye:

- Payment ID
- Monto en USD
- Fecha de pago
- Descripción del servicio
- Plan de suscripción (si aplica)
- Stripe Payment Intent ID
- Enlace a "Ver Historial de Pagos"
- Nota: "Conserve este correo como comprobante de pago"

**Lo que NO incluye:**

- ❌ Número de factura formal
- ❌ Desglose de ITBIS (18%)
- ❌ NCF (Número de Comprobante Fiscal)
- ❌ RNC de OKLA
- ❌ RNC/Cédula del cliente
- ❌ PDF adjunto de la factura
- ❌ Cumplimiento con formato e-CF de la DGII

---

## Plan para Cerrar las Brechas

### Paso 1: Publicar `InvoiceGeneratedEvent`

- En `StripeWebhooksController`, después de crear el Invoice, publicar el evento

### Paso 2: Crear Consumer de Invoice en NotificationService

- Nuevo consumer `InvoiceEmailConsumer` que escuche `billing.invoice.generated`

### Paso 3: Crear template de email `InvoiceEmail.html`

- Con: número de invoice, líneas, subtotal, ITBIS, total, datos fiscales, link a PDF

### Paso 4: Refactorizar recibo de pago

- Cambiar de HTML inline a template basado en archivo

### Paso 5: Adjuntar/enlazar PDF de Stripe

- Incluir link al PDF que Stripe genera (`InvoicePdfUrl`)

### Paso 6: Implementar facturación e-CF (Doc 01 legal)

- Nuevo BillingService para generar XML e-CF con NCF
- Integración con API de DGII
- Desglose de ITBIS obligatorio
