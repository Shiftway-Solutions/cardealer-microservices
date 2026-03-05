# 💳 API STRIPE - International Payments

**Documentación oficial:** https://stripe.com/docs/api  
**Versión:** v20240115  
**Actualizado:** Enero 2026

---

## 📋 Tabla de Contenidos

1. [Autenticación](#autenticación)
2. [Endpoints principales](#endpoints-principales)
3. [Métodos de pago](#métodos-de-pago)
4. [Payment Intents](#payment-intents)
5. [Clientes](#clientes)
6. [Suscripciones](#suscripciones)
7. [Webhooks](#webhooks)
8. [Manejo de errores](#manejo-de-errores)
9. [Ejemplos de código](#ejemplos-de-código)

---

## 🔑 Autenticación

### Credenciales Requeridas

Obten tus credenciales en:

- **Dashboard:** https://dashboard.stripe.com
- **Sandbox (Test):** Para pruebas sin dinero real
- **Live (Production):** Para transacciones reales

### Tipos de Claves

```
Publishable Key (pk_test_ / pk_live_)   - Usar en frontend (público)
Secret Key (sk_test_ / sk_live_)         - Usar en backend (privado)
Restricted API Key                       - Permisos limitados
Webhook Signing Secret (whsec_)          - Para validar webhooks
```

### Autenticación HTTP

**Header HTTP:**

```
Authorization: Bearer {SecretKey}
```

O usando Basic Auth:

```
Authorization: Basic {base64(SecretKey:)}
```

**Ejemplo en C#:**

```csharp
using Stripe;

StripeConfiguration.ApiKey = "sk_test_...";
// O
StripeConfiguration.ApiKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");
```

---

## 🌐 Endpoints Principales

### URL Base

- **Sandbox:** `https://api.stripe.com/v1/` (con claves sk*test*)
- **Production:** `https://api.stripe.com/v1/` (con claves sk*live*)

### Health Check

```bash
curl https://api.stripe.com/v1/charges \
  -u sk_test_xxxxx: \
  -d limit=1
```

---

## 💳 Métodos de Pago Soportados

| Método              | Código          | Descripción                      |
| ------------------- | --------------- | -------------------------------- |
| **Tarjeta Crédito** | card            | Visa, Mastercard, Amex, Discover |
| **Apple Pay**       | apple_pay       | Apple Pay (iOS)                  |
| **Google Pay**      | google_pay      | Google Pay (Android)             |
| **Link**            | link            | Stripe Link (pago rápido)        |
| **ACH Transfer**    | us_bank_account | Transferencia bancaria USA       |
| **iDEAL**           | ideal           | Pago online Holanda              |
| **Giropay**         | giropay         | Pago alemán                      |
| **SEPA Debit**      | sepa_debit      | Débito directo europeo           |
| **Bitcoin**         | bitcoin         | Pagos en Bitcoin                 |

---

## 💰 Payment Intents (Flujo Moderno)

### 1. Crear Payment Intent

**POST** `/payment_intents`

**Request:**

```bash
curl https://api.stripe.com/v1/payment_intents \
  -u sk_test_xxxxx: \
  -d amount=1999 \
  -d currency=usd \
  -d payment_method_types[]=card \
  -d customer=cus_xxxxx
```

**Request JSON:**

```json
{
  "amount": 1999,
  "currency": "usd",
  "payment_method_types": ["card"],
  "customer": "cus_IEHhf0Kdd1D1Q1",
  "description": "Purchase of vehicle parts",
  "metadata": {
    "dealerId": "dealer-123",
    "vehicleId": "vehicle-456",
    "orderId": "order-789"
  },
  "receipt_email": "customer@example.com",
  "statement_descriptor": "OKLA Vehicle Sales"
}
```

**Response:**

```json
{
  "id": "pi_1Mv3N0L2z3z3z3z3",
  "object": "payment_intent",
  "amount": 1999,
  "amount_capturable": 0,
  "amount_details": {
    "tip": null
  },
  "amount_received": 1999,
  "application": null,
  "application_fee_amount": null,
  "automatic_payment_methods": null,
  "canceled_at": null,
  "cancellation_reason": null,
  "capture_method": "automatic",
  "charges": {
    "object": "list",
    "data": [
      {
        "id": "ch_1Mv3N0L2z3z3z3z4",
        "object": "charge",
        "amount": 1999,
        "amount_captured": 1999,
        "amount_refunded": 0,
        "status": "succeeded"
      }
    ],
    "has_more": false,
    "total_count": 1,
    "url": "/v1/charges?payment_intent=pi_1Mv3N0L2z3z3z3z3"
  },
  "client_secret": "pi_1Mv3N0L2z3z3z3z3_secret_xxxxx",
  "confirmation_method": "automatic",
  "created": 1670000000,
  "currency": "usd",
  "customer": "cus_IEHhf0Kdd1D1Q1",
  "description": "Purchase of vehicle parts",
  "last_payment_error": null,
  "livemode": false,
  "metadata": {
    "dealerId": "dealer-123",
    "vehicleId": "vehicle-456"
  },
  "next_action": null,
  "on_behalf_of": null,
  "payment_method": "pm_1Mv3N0L2z3z3z3z5",
  "payment_method_options": {
    "card": {
      "installments": null,
      "mandate_options": null,
      "network": null,
      "request_three_d_secure": "automatic"
    }
  },
  "payment_method_types": ["card"],
  "processing": null,
  "receipt_email": "customer@example.com",
  "review": null,
  "setup_future_usage": null,
  "shipping": {
    "address": {
      "city": "Santo Domingo",
      "country": "DO",
      "line1": "Calle Principal 123",
      "line2": null,
      "postal_code": "10101",
      "state": null
    },
    "carrier": null,
    "name": "Juan Perez",
    "phone": "+1 809-555-0123",
    "tracking_number": null
  },
  "statement_descriptor": "OKLA Vehicle Sales",
  "statement_descriptor_suffix": null,
  "status": "succeeded"
}
```

### 2. Confirmar Payment Intent

**POST** `/payment_intents/{id}/confirm`

**Request:**

```bash
curl https://api.stripe.com/v1/payment_intents/pi_1Mv3N0L2z3z3z3z3/confirm \
  -u sk_test_xxxxx: \
  -d payment_method=pm_1Mv3N0L2z3z3z3z5
```

### 3. Obtener Payment Intent

**GET** `/payment_intents/{id}`

**Response:** (igual al crear, con status actualizado)

### 4. Actualizar Payment Intent

**POST** `/payment_intents/{id}`

**Request:**

```bash
curl https://api.stripe.com/v1/payment_intents/pi_1Mv3N0L2z3z3z3z3 \
  -u sk_test_xxxxx: \
  -d amount=2500 \
  -d description="Updated description"
```

### 5. Cancelar Payment Intent

**POST** `/payment_intents/{id}/cancel`

**Request:**

```bash
curl https://api.stripe.com/v1/payment_intents/pi_1Mv3N0L2z3z3z3z3/cancel \
  -u sk_test_xxxxx: \
  -d cancellation_reason=requested_by_customer
```

---

## 👥 Clientes (Customers)

### 1. Crear Cliente

**POST** `/customers`

**Request:**

```bash
curl https://api.stripe.com/v1/customers \
  -u sk_test_xxxxx: \
  -d email="juan@example.com" \
  -d name="Juan Perez" \
  -d phone="+1 809-555-0123" \
  -d description="Dealer account" \
  -d metadata[dealerId]="dealer-123"
```

**Response:**

```json
{
  "id": "cus_IEHhf0Kdd1D1Q1",
  "object": "customer",
  "address": null,
  "balance": 0,
  "created": 1670000000,
  "currency": null,
  "default_source": null,
  "delinquent": false,
  "description": "Dealer account",
  "discount": null,
  "email": "juan@example.com",
  "invoice_prefix": "INV",
  "invoice_settings": {
    "custom_fields": null,
    "default_payment_method": null,
    "footer": null
  },
  "livemode": false,
  "metadata": {
    "dealerId": "dealer-123"
  },
  "name": "Juan Perez",
  "next_invoice_sequence": 1,
  "phone": "+1 809-555-0123",
  "preferred_locales": [],
  "shipping": null,
  "sources": {
    "object": "list",
    "data": [],
    "has_more": false,
    "total_count": 0,
    "url": "/v1/customers/cus_IEHhf0Kdd1D1Q1/sources"
  },
  "subscriptions": {
    "object": "list",
    "data": [],
    "has_more": false,
    "total_count": 0,
    "url": "/v1/customers/cus_IEHhf0Kdd1D1Q1/subscriptions"
  },
  "tax_ids": {
    "object": "list",
    "data": [],
    "has_more": false,
    "total_count": 0,
    "url": "/v1/customers/cus_IEHhf0Kdd1D1Q1/tax_ids"
  }
}
```

### 2. Obtener Cliente

**GET** `/customers/{id}`

### 3. Actualizar Cliente

**POST** `/customers/{id}`

**Request:**

```bash
curl https://api.stripe.com/v1/customers/cus_IEHhf0Kdd1D1Q1 \
  -u sk_test_xxxxx: \
  -d email="newemail@example.com" \
  -d name="Juan Carlos Perez"
```

### 4. Eliminar Cliente

**DELETE** `/customers/{id}`

### 5. Listar Clientes

**GET** `/customers?limit=100&created[gte]=1670000000`

---

## 🔄 Suscripciones (Subscriptions)

### 1. Crear Producto

**POST** `/products`

**Request:**

```bash
curl https://api.stripe.com/v1/products \
  -u sk_test_xxxxx: \
  -d name="Dealer Pro Plan" \
  -d description="Professional dealer subscription" \
  -d type=service
```

**Response:**

```json
{
  "id": "prod_IEHhf0Kdd1D1Q1",
  "object": "product",
  "active": true,
  "created": 1670000000,
  "description": "Professional dealer subscription",
  "livemode": false,
  "metadata": {},
  "name": "Dealer Pro Plan",
  "type": "service"
}
```

### 2. Crear Plan (Precio)

**POST** `/prices`

**Request:**

```bash
curl https://api.stripe.com/v1/prices \
  -u sk_test_xxxxx: \
  -d product=prod_IEHhf0Kdd1D1Q1 \
  -d unit_amount=12900 \
  -d currency=usd \
  -d recurring[interval]=month \
  -d recurring[interval_count]=1 \
  -d billing_scheme=per_unit
```

**Response:**

```json
{
  "id": "price_1Mv3N0L2z3z3z3z6",
  "object": "price",
  "active": true,
  "billing_scheme": "per_unit",
  "created": 1670000000,
  "currency": "usd",
  "custom_unit_amount": null,
  "livemode": false,
  "lookup_key": null,
  "metadata": {},
  "nickname": null,
  "product": "prod_IEHhf0Kdd1D1Q1",
  "recurring": {
    "aggregate_usage": null,
    "interval": "month",
    "interval_count": 1,
    "meter": null,
    "trial_period_days": null,
    "usage_type": "licensed"
  },
  "tax_behavior": "unspecified",
  "tiers_mode": null,
  "type": "recurring",
  "unit_amount": 12900,
  "unit_amount_decimal": "12900"
}
```

### 3. Crear Suscripción

**POST** `/subscriptions`

**Request:**

```bash
curl https://api.stripe.com/v1/subscriptions \
  -u sk_test_xxxxx: \
  -d customer=cus_IEHhf0Kdd1D1Q1 \
  -d items[0][price]=price_1Mv3N0L2z3z3z3z6 \
  -d payment_behavior=default_incomplete \
  -d payment_settings[save_default_payment_method]=on_subscription \
  -d expand[]=latest_invoice.payment_intent
```

**Response:**

```json
{
  "id": "sub_1Mv3N0L2z3z3z3z7",
  "object": "subscription",
  "application": null,
  "application_fee_percent": null,
  "automatic_tax": {
    "enabled": false
  },
  "billing_cycle_anchor": 1670000000,
  "billing_thresholds": null,
  "cancel_at": null,
  "cancel_at_period_end": false,
  "canceled_at": null,
  "collection_method": "charge_automatically",
  "created": 1670000000,
  "currency": "usd",
  "current_period_end": 1672678400,
  "current_period_start": 1670000000,
  "customer": "cus_IEHhf0Kdd1D1Q1",
  "days_until_due": null,
  "default_payment_method": "pm_1Mv3N0L2z3z3z3z5",
  "default_source": null,
  "default_tax_rates": [],
  "description": null,
  "discount": null,
  "ended_at": null,
  "items": {
    "object": "list",
    "data": [
      {
        "id": "si_IEHhf0Kdd1D1Q2",
        "object": "subscription_item",
        "billing_thresholds": null,
        "created": 1670000000,
        "currency": "usd",
        "custom_price": null,
        "metadata": {},
        "price": {
          "id": "price_1Mv3N0L2z3z3z3z6",
          "object": "price",
          "active": true,
          "currency": "usd",
          "product": "prod_IEHhf0Kdd1D1Q1",
          "recurring": {
            "interval": "month",
            "interval_count": 1
          },
          "unit_amount": 12900
        },
        "quantity": 1,
        "subscription": "sub_1Mv3N0L2z3z3z3z7",
        "tax_rates": []
      }
    ],
    "has_more": false,
    "total_count": 1,
    "url": "/v1/subscription_items?subscription=sub_1Mv3N0L2z3z3z3z7"
  },
  "latest_invoice": "in_1Mv3N0L2z3z3z3z8",
  "livemode": false,
  "metadata": {},
  "next_pending_invoice_item_invoice": null,
  "on_behalf_of": null,
  "pause_at": null,
  "paused_from": null,
  "payment_settings": {
    "payment_method_options": null,
    "payment_method_types": null,
    "save_default_payment_method": "on_subscription"
  },
  "pending_invoice_item_interval": null,
  "pending_setup_intent": null,
  "pending_update": null,
  "schedule": null,
  "start_date": 1670000000,
  "status": "incomplete",
  "test_clock": null,
  "transfer_data": null,
  "trial_end": null,
  "trial_start": null
}
```

### 4. Actualizar Suscripción

**POST** `/subscriptions/{id}`

**Request:**

```bash
curl https://api.stripe.com/v1/subscriptions/sub_1Mv3N0L2z3z3z3z7 \
  -u sk_test_xxxxx: \
  -d items[0][id]=si_IEHhf0Kdd1D1Q2 \
  -d items[0][price]=price_NUEVA_ID
```

### 5. Cancelar Suscripción

**DELETE** `/subscriptions/{id}`

**Request:**

```bash
curl https://api.stripe.com/v1/subscriptions/sub_1Mv3N0L2z3z3z3z7 \
  -u sk_test_xxxxx: \
  -X DELETE
```

### 6. Pausar Suscripción

**POST** `/subscriptions/{id}`

**Request:**

```bash
curl https://api.stripe.com/v1/subscriptions/sub_1Mv3N0L2z3z3z3z7 \
  -u sk_test_xxxxx: \
  -d pause_collection[behavior]=mark_uncollectible
```

---

## 💰 Cargas y Reembolsos

### 1. Crear Cargo (Charge) - Método Legacy

**POST** `/charges`

**Request:**

```bash
curl https://api.stripe.com/v1/charges \
  -u sk_test_xxxxx: \
  -d amount=2500 \
  -d currency=usd \
  -d source=tok_visa \
  -d description="Purchase of vehicle parts"
```

### 2. Obtener Cargo

**GET** `/charges/{id}`

### 3. Reembolsar Cargo

**POST** `/refunds`

**Request:**

```bash
curl https://api.stripe.com/v1/refunds \
  -u sk_test_xxxxx: \
  -d charge=ch_1Mv3N0L2z3z3z3z4 \
  -d amount=1000 \
  -d reason=requested_by_customer
```

**Response:**

```json
{
  "id": "re_1Mv3N0L2z3z3z3z9",
  "object": "refund",
  "amount": 1000,
  "balance_transaction": "txn_1Mv3N0L2z3z3z3zA",
  "charge": "ch_1Mv3N0L2z3z3z3z4",
  "created": 1670000000,
  "currency": "usd",
  "metadata": {},
  "reason": "requested_by_customer",
  "receipt_number": null,
  "source_transfer_reversal": null,
  "status": "succeeded",
  "transfer_reversal": null
}
```

---

## 🪝 Webhooks

### Eventos Disponibles

```
payment_intent.created
payment_intent.succeeded
payment_intent.payment_failed
payment_intent.canceled
charge.succeeded
charge.failed
charge.refunded
charge.dispute.created
customer.created
customer.updated
customer.deleted
invoice.created
invoice.paid
invoice.payment_failed
subscription.created
subscription.updated
subscription.deleted
subscription_schedule.created
subscription_schedule.updated
subscription_schedule.released
subscription_schedule.canceled
```

### Registrar Endpoint

**POST** `/webhook_endpoints`

**Request:**

```bash
curl https://api.stripe.com/v1/webhook_endpoints \
  -u sk_test_xxxxx: \
  -d url="https://api.okla.com.do/webhooks/stripe" \
  -d enabled_events[]=payment_intent.succeeded \
  -d enabled_events[]=payment_intent.payment_failed \
  -d enabled_events[]=invoice.paid \
  -d enabled_events[]=customer.subscription.deleted
```

### Recibir Webhook

**POST** (desde Stripe a tu servidor)

**Headers:**

```
Stripe-Signature: v1=timestamp,signature
Content-Type: application/json
```

**Body:**

```json
{
  "id": "evt_1Mv3N0L2z3z3z3zA",
  "object": "event",
  "api_version": "2024-01-15",
  "created": 1670000000,
  "data": {
    "object": {
      "id": "pi_1Mv3N0L2z3z3z3z3",
      "object": "payment_intent",
      "amount": 1999,
      "status": "succeeded",
      "currency": "usd"
    }
  },
  "livemode": false,
  "pending_webhooks": 1,
  "request": {
    "id": null,
    "idempotency_key": "key-123"
  },
  "type": "payment_intent.succeeded"
}
```

### Validar Webhook Signature

```csharp
using Stripe;

public bool ValidateStripeWebhookSignature(
    string json,
    string signatureHeader,
    string webhookSecret)
{
    try
    {
        var stripeEvent = EventUtility.ParseEvent(json);
        var computedSignature = ComputeSignature(
            json,
            webhookSecret);

        // Stripe signature format: v1=timestamp,signature
        var parts = signatureHeader.Split(',');
        var signature = parts[1].Split('=')[1];

        return signature == computedSignature;
    }
    catch (Exception e)
    {
        Console.WriteLine($"Webhook signature validation failed: {e.Message}");
        return false;
    }
}
```

---

## ⚠️ Manejo de Errores

### Tipos de Errores

| Tipo                      | Código                | Descripción                        |
| ------------------------- | --------------------- | ---------------------------------- |
| **card_error**            | generic_decline       | Tarjeta rechazada (razón genérica) |
| **card_error**            | insufficient_funds    | Fondos insuficientes               |
| **card_error**            | lost_card             | Tarjeta perdida                    |
| **card_error**            | stolen_card           | Tarjeta robada                     |
| **card_error**            | expired_card          | Tarjeta vencida                    |
| **card_error**            | incorrect_cvc         | CVC incorrecto                     |
| **card_error**            | processing_error      | Error en procesamiento             |
| **rate_limit_error**      | rate_limit            | Límite de tasa excedido            |
| **authentication_error**  | authentication_error  | Error de autenticación             |
| **invalid_request_error** | invalid_request_error | Parámetros inválidos               |
| **api_error**             | api_error             | Error del servidor Stripe          |

### Estructura de Error

```json
{
  "error": {
    "code": "card_declined",
    "decline_code": "generic_decline",
    "doc_url": "https://stripe.com/docs/error-codes/card-declined",
    "message": "Your card was declined",
    "param": "card",
    "payment_intent": {
      "id": "pi_1Mv3N0L2z3z3z3z3",
      "object": "payment_intent",
      "last_payment_error": {}
    },
    "payment_method": {
      "id": "pm_1Mv3N0L2z3z3z3z5",
      "object": "payment_method"
    },
    "payment_method_type": "card",
    "type": "card_error"
  }
}
```

---

## 💻 Ejemplos de Código

### C# - Usando Stripe.net Library

```csharp
using Stripe;
using System.Threading.Tasks;

public class StripePaymentService
{
    public StripePaymentService(string apiKey)
    {
        StripeConfiguration.ApiKey = apiKey;
    }

    // Crear Payment Intent
    public async Task<PaymentIntent> CreatePaymentIntentAsync(
        long amount,
        string customerId,
        string description)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = amount,
            Currency = "usd",
            Customer = customerId,
            Description = description,
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true
            },
            Metadata = new Dictionary<string, string>
            {
                { "dealerId", "dealer-123" }
            }
        };

        var service = new PaymentIntentService();
        var paymentIntent = await service.CreateAsync(options);

        return paymentIntent;
    }

    // Confirmar Payment Intent
    public async Task<PaymentIntent> ConfirmPaymentIntentAsync(
        string paymentIntentId,
        string paymentMethodId)
    {
        var options = new PaymentIntentConfirmOptions
        {
            PaymentMethod = paymentMethodId
        };

        var service = new PaymentIntentService();
        var paymentIntent = await service.ConfirmAsync(paymentIntentId, options);

        return paymentIntent;
    }

    // Crear Cliente
    public async Task<Customer> CreateCustomerAsync(
        string email,
        string name,
        string description)
    {
        var options = new CustomerCreateOptions
        {
            Email = email,
            Name = name,
            Description = description,
            Metadata = new Dictionary<string, string>
            {
                { "dealerId", "dealer-123" }
            }
        };

        var service = new CustomerService();
        var customer = await service.CreateAsync(options);

        return customer;
    }

    // Crear Suscripción
    public async Task<Subscription> CreateSubscriptionAsync(
        string customerId,
        string priceId)
    {
        var options = new SubscriptionCreateOptions
        {
            Customer = customerId,
            Items = new List<SubscriptionItemOptions>
            {
                new SubscriptionItemOptions { Price = priceId }
            },
            PaymentBehavior = "default_incomplete",
            PaymentSettings = new SubscriptionPaymentSettingsOptions
            {
                SaveDefaultPaymentMethod = "on_subscription"
            }
        };

        var service = new SubscriptionService();
        var subscription = await service.CreateAsync(options);

        return subscription;
    }

    // Crear Reembolso
    public async Task<Refund> CreateRefundAsync(
        string chargeId,
        long amount,
        string reason)
    {
        var options = new RefundCreateOptions
        {
            Charge = chargeId,
            Amount = amount,
            Reason = reason,
            Metadata = new Dictionary<string, string>
            {
                { "dealerId", "dealer-123" }
            }
        };

        var service = new RefundService();
        var refund = await service.CreateAsync(options);

        return refund;
    }
}
```

### C# - Procesar Webhook

```csharp
using Stripe;
using System.IO;
using System.Threading.Tasks;

[HttpPost("webhooks/stripe")]
public async Task<IActionResult> HandleStripeWebhook()
{
    var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

    try
    {
        var stripeEvent = EventUtility.ParseEvent(json);

        // Validate the signature
        var signatureHeader = Request.Headers["Stripe-Signature"];
        stripeEvent = EventUtility.ConstructEvent(
            json,
            signatureHeader,
            _webhookSecret
        );

        // Handle the event
        switch (stripeEvent.Type)
        {
            case Events.PaymentIntentSucceeded:
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                await _paymentService.ProcessPaymentSucceeded(paymentIntent);
                break;

            case Events.PaymentIntentPaymentFailed:
                var failedPayment = stripeEvent.Data.Object as PaymentIntent;
                await _paymentService.ProcessPaymentFailed(failedPayment);
                break;

            case Events.InvoicePaid:
                var invoice = stripeEvent.Data.Object as Invoice;
                await _invoiceService.ProcessInvoicePaid(invoice);
                break;

            case Events.CustomerSubscriptionDeleted:
                var subscription = stripeEvent.Data.Object as Subscription;
                await _subscriptionService.ProcessSubscriptionDeleted(subscription);
                break;
        }

        return Ok();
    }
    catch (StripeException e)
    {
        return BadRequest(e.Message);
    }
}
```

---

## 🧪 Tarjetas de Prueba

Para el ambiente Sandbox, usa estas tarjetas:

| Número              | Mes/Año | CVC | Resultado    |
| ------------------- | ------- | --- | ------------ |
| 4242 4242 4242 4242 | 12/25   | 123 | ✅ Aprobado  |
| 4000 0000 0000 0002 | 12/25   | 123 | ❌ Rechazado |
| 4000 0000 0000 9995 | 12/25   | 123 | ⚠️ 3D Secure |
| 4000 0000 0000 9235 | 12/25   | 123 | ❌ No funds  |
| 378282246310005     | 12/25   | 123 | ✅ Amex      |

---

## 📊 Límites y Cuotas

| Límite                        | Valor                                  |
| ----------------------------- | -------------------------------------- |
| **Monto máximo por cargo**    | No hay límite (verificación de fraude) |
| **Monto mínimo por cargo**    | $0.50 USD                              |
| **Requests por segundo**      | 100 por IP                             |
| **Requests por hora**         | Ilimitados                             |
| **Webhook retries**           | 3 intentos en 5 horas                  |
| **Validez de Payment Intent** | 24 horas                               |
| **Validez de Setup Intent**   | 24 horas                               |
| **Webhook event retention**   | 30 días                                |

---

## 🔗 Recursos Útiles

- **Dashboard:** https://dashboard.stripe.com
- **API Reference:** https://stripe.com/docs/api
- **Payment Intents Guide:** https://stripe.com/docs/payments/payment-intents
- **Stripe.net Library:** https://github.com/stripe/stripe-dotnet
- **Testing Guide:** https://stripe.com/docs/testing
- **Status Page:** https://status.stripe.com
- **Support:** https://support.stripe.com

---

**Última actualización:** Enero 2026  
**Versión API:** v20240115
