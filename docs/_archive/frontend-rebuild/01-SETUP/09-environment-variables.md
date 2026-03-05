# 🔐 Environment Variables Reference

> **Tiempo estimado:** 15 minutos
> **Prerrequisitos:** Proyecto Next.js configurado
> **Última actualización:** Enero 2026

---

## 📋 OBJETIVO

Documentar todas las variables de entorno requeridas para el frontend de OKLA, organizadas por categoría y ambiente.

---

## 🎯 ESTRUCTURA DE ARCHIVOS

```
/
├── .env.local           # Variables locales (NO commitear)
├── .env.development     # Variables de desarrollo
├── .env.staging         # Variables de staging
├── .env.production      # Variables de producción
├── .env.example         # Template con todas las variables
└── .env.test            # Variables para testing
```

---

## 🔧 VARIABLES DE ENTORNO

### `.env.example` (Template Completo)

```bash
# =============================================================================
# OKLA Frontend - Environment Variables
# =============================================================================
# Copiar este archivo a .env.local y completar los valores
# NUNCA commitear .env.local con secrets reales
# =============================================================================

# =============================================================================
# 🌐 API & BACKEND
# =============================================================================

# URL base del API Gateway
NEXT_PUBLIC_API_URL=http://localhost:18443

# URL del WebSocket para real-time features
NEXT_PUBLIC_WS_URL=ws://localhost:18443

# Timeout para requests (en ms)
NEXT_PUBLIC_API_TIMEOUT=30000

# =============================================================================
# 🔐 AUTENTICACIÓN
# =============================================================================

# NextAuth Secret (generar con: openssl rand -base64 32)
NEXTAUTH_SECRET=your-super-secret-key-min-32-chars

# URL base de la aplicación
NEXTAUTH_URL=http://localhost:3000

# OAuth - Google
GOOGLE_CLIENT_ID=
GOOGLE_CLIENT_SECRET=

# OAuth - Facebook (opcional)
FACEBOOK_CLIENT_ID=
FACEBOOK_CLIENT_SECRET=

# OAuth - Apple (opcional)
APPLE_CLIENT_ID=
APPLE_CLIENT_SECRET=

# =============================================================================
# 💳 PAGOS
# =============================================================================

# Stripe
NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY=pk_test_xxx
STRIPE_SECRET_KEY=sk_test_xxx
STRIPE_WEBHOOK_SECRET=whsec_xxx

# Azul (Banco Popular RD)
NEXT_PUBLIC_AZUL_MERCHANT_ID=
AZUL_AUTH_KEY=
AZUL_MERCHANT_NAME=OKLA
AZUL_ENVIRONMENT=test # test | production

# =============================================================================
# 📧 COMUNICACIONES
# =============================================================================

# Email (para formulario de contacto server-side)
SMTP_HOST=smtp.example.com
SMTP_PORT=587
SMTP_USER=
SMTP_PASSWORD=
EMAIL_FROM=noreply@okla.com.do

# =============================================================================
# 📁 STORAGE & MEDIA
# =============================================================================

# DigitalOcean Spaces (S3-compatible)
NEXT_PUBLIC_CDN_URL=https://cdn.okla.com.do
SPACES_KEY=
SPACES_SECRET=
SPACES_BUCKET=okla-media
SPACES_REGION=nyc3
SPACES_ENDPOINT=https://nyc3.digitaloceanspaces.com

# Límites de upload
NEXT_PUBLIC_MAX_FILE_SIZE_MB=10
NEXT_PUBLIC_MAX_IMAGES_PER_VEHICLE=20

# =============================================================================
# 📊 ANALYTICS & MONITORING
# =============================================================================

# Google Analytics 4
NEXT_PUBLIC_GA_MEASUREMENT_ID=G-XXXXXXXXXX

# Google Tag Manager
NEXT_PUBLIC_GTM_ID=GTM-XXXXXXX

# Sentry (Error Tracking)
NEXT_PUBLIC_SENTRY_DSN=https://xxx@xxx.ingest.sentry.io/xxx
SENTRY_AUTH_TOKEN=
SENTRY_ORG=okla
SENTRY_PROJECT=okla-frontend

# Hotjar (Session Recording)
NEXT_PUBLIC_HOTJAR_ID=
NEXT_PUBLIC_HOTJAR_VERSION=6

# =============================================================================
# 🗺️ MAPAS & GEOLOCALIZACIÓN
# =============================================================================

# Google Maps
NEXT_PUBLIC_GOOGLE_MAPS_API_KEY=

# Mapbox (alternativa)
NEXT_PUBLIC_MAPBOX_ACCESS_TOKEN=

# =============================================================================
# 🤖 AI & ML
# =============================================================================

# OpenAI (para chatbot)
OPENAI_API_KEY=sk-xxx

# =============================================================================
# 🔄 CACHE & PERFORMANCE
# =============================================================================

# Redis (para caching server-side si aplica)
REDIS_URL=redis://localhost:6379

# Revalidación de páginas estáticas (segundos)
NEXT_PUBLIC_REVALIDATE_INTERVAL=60

# =============================================================================
# 🏷️ FEATURE FLAGS
# =============================================================================

# Habilitar/deshabilitar features
NEXT_PUBLIC_FEATURE_CHATBOT=true
NEXT_PUBLIC_FEATURE_360_VIEWER=true
NEXT_PUBLIC_FEATURE_VIDEO_TOURS=true
NEXT_PUBLIC_FEATURE_FINANCING=false
NEXT_PUBLIC_FEATURE_TRADE_IN=false

# =============================================================================
# 📍 REGIONAL
# =============================================================================

# Configuración regional
NEXT_PUBLIC_DEFAULT_LOCALE=es-DO
NEXT_PUBLIC_DEFAULT_CURRENCY=DOP
NEXT_PUBLIC_DEFAULT_TIMEZONE=America/Santo_Domingo

# Tasa de cambio USD/DOP (actualizar periódicamente)
NEXT_PUBLIC_EXCHANGE_RATE_USD_DOP=58.5

# =============================================================================
# 🧪 TESTING & DEVELOPMENT
# =============================================================================

# Modo de desarrollo
NODE_ENV=development

# Mostrar DevTools
NEXT_PUBLIC_SHOW_DEVTOOLS=true

# Mock de APIs (para desarrollo sin backend)
NEXT_PUBLIC_USE_MOCK_API=false

# Cypress
CYPRESS_BASE_URL=http://localhost:3000

# =============================================================================
# 🔒 SECURITY
# =============================================================================

# CSP Nonce (generado en runtime)
# No configurar aquí, se genera dinámicamente

# Rate Limiting
RATE_LIMIT_MAX_REQUESTS=100
RATE_LIMIT_WINDOW_MS=60000

# =============================================================================
# 📱 PWA
# =============================================================================

# Configuración de PWA
NEXT_PUBLIC_APP_NAME=OKLA
NEXT_PUBLIC_APP_SHORT_NAME=OKLA
NEXT_PUBLIC_APP_DESCRIPTION=Marketplace de Vehículos RD

# =============================================================================
# 🏢 BUSINESS
# =============================================================================

# Información de contacto
NEXT_PUBLIC_SUPPORT_EMAIL=soporte@okla.com.do
NEXT_PUBLIC_SUPPORT_PHONE=+1-809-555-0100
NEXT_PUBLIC_WHATSAPP_NUMBER=18095550100

# Redes sociales
NEXT_PUBLIC_FACEBOOK_URL=https://facebook.com/oklard
NEXT_PUBLIC_INSTAGRAM_URL=https://instagram.com/okla_rd
NEXT_PUBLIC_TWITTER_URL=https://twitter.com/okla_rd

# Early Bird (fecha límite)
NEXT_PUBLIC_EARLY_BIRD_DEADLINE=2026-01-31T23:59:59-04:00
```

---

## 🌍 VARIABLES POR AMBIENTE

### Desarrollo Local (`.env.local`)

```bash
# API local
NEXT_PUBLIC_API_URL=http://localhost:18443
NEXT_PUBLIC_WS_URL=ws://localhost:18443

# Auth
NEXTAUTH_URL=http://localhost:3000
NEXTAUTH_SECRET=dev-secret-key-for-local-development

# Dev tools
NEXT_PUBLIC_SHOW_DEVTOOLS=true
NEXT_PUBLIC_USE_MOCK_API=false

# Stripe Test
NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY=pk_test_xxx

# Analytics (deshabilitado en dev)
NEXT_PUBLIC_GA_MEASUREMENT_ID=

# Sentry (deshabilitado en dev)
NEXT_PUBLIC_SENTRY_DSN=
```

### Staging (`.env.staging`)

```bash
# API staging
NEXT_PUBLIC_API_URL=https://api-staging.okla.com.do
NEXT_PUBLIC_WS_URL=wss://api-staging.okla.com.do

# Auth
NEXTAUTH_URL=https://staging.okla.com.do

# Stripe Test (aún en modo test)
NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY=pk_test_xxx

# Analytics (habilitar para testing)
NEXT_PUBLIC_GA_MEASUREMENT_ID=G-XXXXXXXXXX

# Sentry (habilitar para capturar errores)
NEXT_PUBLIC_SENTRY_DSN=https://xxx@xxx.ingest.sentry.io/xxx

# Features (probar antes de producción)
NEXT_PUBLIC_FEATURE_CHATBOT=true
```

### Producción (`.env.production`)

```bash
# API producción
NEXT_PUBLIC_API_URL=https://api.okla.com.do
NEXT_PUBLIC_WS_URL=wss://api.okla.com.do

# Auth
NEXTAUTH_URL=https://okla.com.do

# Stripe Live
NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY=pk_live_xxx

# Azul Production
AZUL_ENVIRONMENT=production

# Analytics completo
NEXT_PUBLIC_GA_MEASUREMENT_ID=G-XXXXXXXXXX
NEXT_PUBLIC_GTM_ID=GTM-XXXXXXX
NEXT_PUBLIC_HOTJAR_ID=xxxxxxx

# Sentry con sampling
NEXT_PUBLIC_SENTRY_DSN=https://xxx@xxx.ingest.sentry.io/xxx

# Dev tools deshabilitados
NEXT_PUBLIC_SHOW_DEVTOOLS=false
```

---

## 🔐 MANEJO DE SECRETS

### Secrets que NUNCA deben estar en código

```bash
# Estos van en el sistema de CI/CD o vault:
NEXTAUTH_SECRET
STRIPE_SECRET_KEY
STRIPE_WEBHOOK_SECRET
GOOGLE_CLIENT_SECRET
FACEBOOK_CLIENT_SECRET
APPLE_CLIENT_SECRET
AZUL_AUTH_KEY
SMTP_PASSWORD
SPACES_SECRET
SENTRY_AUTH_TOKEN
OPENAI_API_KEY
REDIS_URL (si tiene password)
```

### GitHub Secrets (para CI/CD)

```yaml
# .github/workflows/deploy.yml
env:
  NEXTAUTH_SECRET: ${{ secrets.NEXTAUTH_SECRET }}
  STRIPE_SECRET_KEY: ${{ secrets.STRIPE_SECRET_KEY }}
  SENTRY_AUTH_TOKEN: ${{ secrets.SENTRY_AUTH_TOKEN }}
```

### Kubernetes Secrets

```yaml
# k8s/secrets.yaml
apiVersion: v1
kind: Secret
metadata:
  name: okla-frontend-secrets
  namespace: okla
type: Opaque
stringData:
  NEXTAUTH_SECRET: "xxx"
  STRIPE_SECRET_KEY: "xxx"
```

---

## 🔍 VALIDACIÓN DE VARIABLES

### Script de Validación

```typescript
// filepath: src/lib/env.ts
import { z } from "zod";

const envSchema = z.object({
  // Públicas (NEXT_PUBLIC_*)
  NEXT_PUBLIC_API_URL: z.string().url(),
  NEXT_PUBLIC_WS_URL: z.string().optional(),
  NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY: z.string().startsWith("pk_"),
  NEXT_PUBLIC_GA_MEASUREMENT_ID: z.string().optional(),
  NEXT_PUBLIC_DEFAULT_LOCALE: z.enum(["es-DO", "en-US"]).default("es-DO"),
  NEXT_PUBLIC_DEFAULT_CURRENCY: z.enum(["DOP", "USD"]).default("DOP"),

  // Privadas (server-side only)
  NEXTAUTH_SECRET: z.string().min(32),
  NEXTAUTH_URL: z.string().url(),
  STRIPE_SECRET_KEY: z.string().startsWith("sk_").optional(),
});

// Validar al iniciar la app
export function validateEnv() {
  try {
    envSchema.parse(process.env);
    console.log("✅ Environment variables validated");
  } catch (error) {
    console.error("❌ Invalid environment variables:", error);
    process.exit(1);
  }
}

// Exportar variables tipadas
export const env = {
  apiUrl: process.env.NEXT_PUBLIC_API_URL!,
  wsUrl: process.env.NEXT_PUBLIC_WS_URL,
  stripePublishableKey: process.env.NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY!,
  gaId: process.env.NEXT_PUBLIC_GA_MEASUREMENT_ID,
  defaultLocale: process.env.NEXT_PUBLIC_DEFAULT_LOCALE || "es-DO",
  defaultCurrency: process.env.NEXT_PUBLIC_DEFAULT_CURRENCY || "DOP",
} as const;
```

### Uso en la App

```typescript
// filepath: src/app/layout.tsx
import { validateEnv, env } from "@/lib/env";

// Validar en desarrollo
if (process.env.NODE_ENV === "development") {
  validateEnv();
}

// Usar variables tipadas
console.log("API URL:", env.apiUrl);
```

---

## 📝 NOTAS IMPORTANTES

### Prefijo `NEXT_PUBLIC_`

```bash
# ✅ Accesible en cliente Y servidor
NEXT_PUBLIC_API_URL=https://api.okla.com.do

# ❌ Solo accesible en servidor
STRIPE_SECRET_KEY=sk_xxx
```

### Prioridad de Carga

1. `.env.$(NODE_ENV).local` - Máxima prioridad, no commitear
2. `.env.local` - No commitear
3. `.env.$(NODE_ENV)` - Por ambiente
4. `.env` - Defaults

### Recarga de Variables

```bash
# Las variables se cargan al build/start
# Para cambios en runtime, reiniciar el servidor:

npm run dev  # Desarrollo
npm run build && npm run start  # Producción
```

---

## ✅ Checklist

- [ ] Crear `.env.example` con todas las variables
- [ ] Agregar `.env.local` a `.gitignore`
- [ ] Configurar secrets en CI/CD
- [ ] Documentar variables requeridas por ambiente
- [ ] Implementar validación con Zod
- [ ] Crear scripts de verificación

---

## 🔗 Referencias

- [Next.js Environment Variables](https://nextjs.org/docs/app/building-your-application/configuring/environment-variables)
- [Vercel Environment Variables](https://vercel.com/docs/projects/environment-variables)
- [T3 Env](https://env.t3.gg/) - Alternativa con mejor DX

---

_Las variables de entorno son la única forma segura de manejar configuración específica por ambiente._
