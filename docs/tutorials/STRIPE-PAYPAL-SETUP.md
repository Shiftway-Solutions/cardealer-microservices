# Guía de Configuración: Stripe y PayPal para OKLA

## Tabla de Contenidos

1. [Configuración de Stripe](#1-configuración-de-stripe)
2. [Configuración de PayPal](#2-configuración-de-paypal)
3. [Variables de Entorno](#3-variables-de-entorno)
4. [Pruebas en Sandbox/Test](#4-pruebas-en-sandboxtest)
5. [Paso a Producción](#5-paso-a-producción)

---

## 1. Configuración de Stripe

### 1.1 Crear Cuenta de Stripe

1. Ve a [https://dashboard.stripe.com/register](https://dashboard.stripe.com/register)
2. Completa el formulario con tu email y contraseña
3. Verifica tu email
4. Completa la verificación de identidad para activar pagos en vivo

### 1.2 Obtener Claves API

1. Inicia sesión en el [Stripe Dashboard](https://dashboard.stripe.com)
2. Asegúrate de estar en modo **"Test"** (toggle arriba a la derecha)
3. Ve a **Developers** → **API Keys**
4. Copia las claves:
   - **Publishable key** (empieza con `pk_test_...`) → para el frontend
   - **Secret key** (empieza con `sk_test_...`) → para el backend

### 1.3 Configurar Webhook

1. En el Dashboard, ve a **Developers** → **Webhooks**
2. Clic en **"Add endpoint"**
3. URL del endpoint:
   - **Staging**: `https://staging.okla.com.do/api/webhook/stripe`
   - **Producción**: `https://okla.com.do/api/webhook/stripe`
4. Selecciona estos eventos:
   - `customer.created`
   - `customer.updated`
   - `customer.deleted`
   - `customer.subscription.created`
   - `customer.subscription.updated`
   - `customer.subscription.deleted`
   - `customer.subscription.trial_will_end`
   - `invoice.created`
   - `invoice.paid`
   - `invoice.payment_failed`
   - `payment_intent.succeeded`
   - `payment_intent.payment_failed`
   - `checkout.session.completed`
5. Clic en **"Add endpoint"**
6. Copia el **Signing secret** (`whsec_...`) → para verificar webhooks

### 1.4 Crear Precios/Productos en Stripe

1. Ve a **Products** → **Add product**
2. Crea los siguientes productos con sus precios:

| Producto          | Precio Mensual | Precio Anual  | Price ID   |
| ----------------- | -------------- | ------------- | ---------- |
| Basic Plan        | $29.00/mes     | $290.00/año   | Automático |
| Starter Plan      | $59.00/mes     | $590.00/año   | Automático |
| Professional Plan | $99.00/mes     | $990.00/año   | Automático |
| Enterprise Plan   | $349.00/mes    | $3,490.00/año | Automático |
| Corporate Plan    | $599.00/mes    | $5,990.00/año | Automático |

3. Copia cada **Price ID** (empieza con `price_...`) y configúralo en `appsettings.json`:

```json
"Stripe": {
  "PriceIds": {
    "basic_monthly": "price_1Abc...",
    "basic_yearly": "price_2Def...",
    "professional_monthly": "price_3Ghi...",
    "professional_yearly": "price_4Jkl...",
    "enterprise_monthly": "price_5Mno...",
    "enterprise_yearly": "price_6Pqr..."
  }
}
```

### 1.5 Tarjetas de Prueba

| Número                | Resultado               | CVV        | Fecha  |
| --------------------- | ----------------------- | ---------- | ------ |
| `4242 4242 4242 4242` | Pago exitoso            | Cualquiera | Futura |
| `4000 0025 0000 3155` | Requiere 3D Secure      | Cualquiera | Futura |
| `4000 0000 0000 9995` | Pago rechazado          | Cualquiera | Futura |
| `4000 0000 0000 0077` | Pago rechazado (fondos) | Cualquiera | Futura |

---

## 2. Configuración de PayPal

### 2.1 Crear Cuenta de Desarrollador

1. Ve a [https://developer.paypal.com/dashboard/](https://developer.paypal.com/dashboard/)
2. Inicia sesión con tu cuenta de PayPal (o crea una nueva)
3. Si es tu primera vez, acepta los términos de desarrollador

### 2.2 Crear Aplicación Sandbox

1. En el Dashboard, ve a **Apps & Credentials**
2. Asegúrate de estar en **"Sandbox"** mode
3. Clic en **"Create App"**
4. Nombre: `OKLA Marketplace`
5. Tipo: **Merchant**
6. Clic en **"Create App"**
7. Copia las credenciales:
   - **Client ID** → para frontend y backend
   - **Secret** → solo para backend

### 2.3 Configurar Cuentas de Prueba Sandbox

PayPal crea automáticamente 2 cuentas sandbox:

- **Business** (seller): simula el comercio de OKLA
- **Personal** (buyer): simula el comprador

Para ver las cuentas:

1. Ve a **Testing Tools** → **Sandbox Accounts**
2. La cuenta **Business** tiene las credenciales de la app
3. La cuenta **Personal** se usa para pagar en los tests

**Credenciales de la cuenta Personal (buyer) para pruebas:**

- Email: aparece en la lista de sandbox accounts
- Contraseña: clic en los `...` → "View/edit account" → puedes ver/cambiar la contraseña

### 2.4 Configurar Webhook

1. En el Dashboard, ve a tu App → **Webhooks**
2. Clic en **"Add Webhook"**
3. URL:
   - **Staging**: `https://staging.okla.com.do/api/webhook/paypal`
   - **Producción**: `https://okla.com.do/api/webhook/paypal`
4. Selecciona estos eventos:
   - `PAYMENT.CAPTURE.COMPLETED`
   - `PAYMENT.CAPTURE.DENIED`
   - `PAYMENT.CAPTURE.REFUNDED`
   - `CHECKOUT.ORDER.APPROVED`
5. Clic en **"Save"**
6. Copia el **Webhook ID** → para verificar firmas

### 2.5 Flujo de Prueba

1. En la app de OKLA, selecciona PayPal como método de pago
2. Se abre el popup de PayPal Sandbox
3. Inicia sesión con la cuenta **Personal (buyer)** de sandbox
4. Aprueba el pago
5. El popup se cierra y OKLA captura el pago automáticamente

---

## 3. Variables de Entorno

### Frontend (`.env.local`)

```env
# Stripe
NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY=pk_test_51SlBcBFEbUi2kuqF...
STRIPE_WEBHOOK_SECRET=whsec_...

# PayPal
NEXT_PUBLIC_PAYPAL_CLIENT_ID=AaBbCcDd...
```

### Backend (`appsettings.json` o Docker Secrets)

```json
{
  "Stripe": {
    "SecretKey": "sk_test_51SlBcBFE...",
    "PublishableKey": "pk_test_51SlBcBFE...",
    "WebhookSecret": "whsec_...",
    "DefaultTrialDays": 14,
    "PriceIds": {
      "basic_monthly": "price_...",
      "basic_yearly": "price_...",
      "professional_monthly": "price_...",
      "professional_yearly": "price_...",
      "enterprise_monthly": "price_...",
      "enterprise_yearly": "price_..."
    }
  },
  "PayPal": {
    "ClientId": "AaBbCcDd...",
    "ClientSecret": "EeFfGgHh...",
    "WebhookId": "1AB23456CD789012E",
    "Sandbox": true
  }
}
```

### Kubernetes Secrets (Producción)

```bash
# Crear secrets en DigitalOcean Kubernetes
kubectl create secret generic billing-secrets \
  --from-literal=stripe-secret-key="sk_live_..." \
  --from-literal=stripe-publishable-key="pk_live_..." \
  --from-literal=stripe-webhook-secret="whsec_..." \
  --from-literal=paypal-client-id="AaBbCcDd..." \
  --from-literal=paypal-client-secret="EeFfGgHh..." \
  --from-literal=paypal-webhook-id="1AB23456CD789012E" \
  -n okla
```

---

## 4. Pruebas en Sandbox/Test

### Stripe

```bash
# Instalar Stripe CLI para pruebas locales de webhooks
brew install stripe/stripe-cli/stripe

# Login con tu cuenta
stripe login

# Re-enviar webhooks a localhost
stripe listen --forward-to localhost:3000/api/webhook/stripe

# En otra terminal, disparar un evento de prueba
stripe trigger payment_intent.succeeded
```

### PayPal

1. Usa las cuentas sandbox generadas automáticamente
2. En la UI de OKLA, selecciona PayPal y paga con la cuenta sandbox Personal
3. Verifica en el Dashboard de PayPal Sandbox que el pago aparece

---

## 5. Paso a Producción

### Checklist Pre-Producción

- [ ] **Stripe**: Cambiar de `pk_test_`/`sk_test_` a `pk_live_`/`sk_live_`
- [ ] **Stripe**: Crear nuevo webhook endpoint con URL de producción
- [ ] **Stripe**: Crear productos/precios en modo Live (los de test no sirven en live)
- [ ] **PayPal**: Cambiar de Sandbox a Live en el Dashboard
- [ ] **PayPal**: Obtener Client ID y Secret de producción
- [ ] **PayPal**: Crear webhook con URL de producción
- [ ] **Backend**: Cambiar `"Sandbox": true` a `"Sandbox": false` en PayPal config
- [ ] **Frontend**: Actualizar `NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY` con key de producción
- [ ] **Frontend**: Actualizar `NEXT_PUBLIC_PAYPAL_CLIENT_ID` con ID de producción
- [ ] **K8s**: Actualizar secrets en Kubernetes
- [ ] **Verificar**: Hacer un pago de prueba con monto real mínimo ($1)
- [ ] **Verificar**: Confirmar que webhooks llegan y procesan correctamente
- [ ] **Verificar**: Probar flujo de reembolso

### Monitoreo Post-Producción

- Revisar Stripe Dashboard → **Payments** diariamente
- Revisar PayPal Dashboard → **Activity** diariamente
- Configurar alertas en Stripe para fallos de pago recurrentes
- Ejecutar reconciliación diaria (ya implementada en `DailyReconciliationJob`)
