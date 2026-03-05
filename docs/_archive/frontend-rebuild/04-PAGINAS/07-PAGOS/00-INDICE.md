# 📁 07-PAGOS - Pagos y Facturación

> **Descripción:** Flujos de pago, checkout y facturación  
> **Total:** 5 documentos  
> **Prioridad:** 🔴 P0 - Monetización

---

## 📋 Documentos en Esta Sección

| #   | Archivo                                                  | Descripción                          | Prioridad |
| --- | -------------------------------------------------------- | ------------------------------------ | --------- |
| 1   | [01-pagos-checkout.md](01-pagos-checkout.md)             | Checkout y pasarelas de pago         | P0        |
| 2   | [02-payment-results.md](02-payment-results.md)           | Páginas de resultado (success/error) | P0        |
| 3   | [03-billing-dashboard.md](03-billing-dashboard.md)       | Dashboard de facturación             | P1        |
| 4   | [04-moneda-extranjera.md](04-moneda-extranjera.md)       | Soporte multi-moneda (USD/DOP)       | P2        |
| 5   | [05-comercio-electronico.md](05-comercio-electronico.md) | E-commerce y carrito                 | P2        |

---

## 🎯 Orden de Implementación para IA

```
1. 01-pagos-checkout.md      → Checkout (Stripe + Azul)
2. 02-payment-results.md     → Páginas de resultado
3. 03-billing-dashboard.md   → Dashboard de facturación
4. 04-moneda-extranjera.md   → Multi-moneda
5. 05-comercio-electronico.md → E-commerce avanzado
```

---

## 🔗 Dependencias Externas

- **02-AUTH/**: Autenticación requerida
- **05-API-INTEGRATION/**: billing-api, payments-api
- **05-DEALER/08-boost-promociones.md**: Pagos de boost

---

## 📊 APIs Utilizadas

| Servicio             | Endpoints Principales                         |
| -------------------- | --------------------------------------------- |
| BillingService       | POST /billing/checkout, GET /billing/invoices |
| StripePaymentService | POST /stripe/create-session                   |
| AzulPaymentService   | POST /azul/process                            |
| BillingService       | GET /subscriptions, PUT /subscriptions/:id    |

---

## 💳 Pasarelas de Pago

| Pasarela                 | Uso                                        | Comisión |
| ------------------------ | ------------------------------------------ | -------- |
| **Azul (Banco Popular)** | Tarjetas dominicanas (DEFAULT)             | ~2.5%    |
| **Stripe**               | Tarjetas internacionales, Apple/Google Pay | ~3.5%    |
