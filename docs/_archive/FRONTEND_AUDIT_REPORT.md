# 🔍 Frontend Comprehensive Audit Report

**Date:** 2026-03-03  
**Scope:** `frontend/web-next/src/app/` — All pages, API routes, and supporting services  
**Total pages found:** 162  
**Total API routes found:** 23

---

## 1. Complete Page Inventory by User Type

### 🌐 Public Pages (No auth required) — 25 pages

| #   | Route                    | File                                    |
| --- | ------------------------ | --------------------------------------- |
| 1   | `/`                      | `(main)/page.tsx`                       |
| 2   | `/vehiculos`             | `(main)/vehiculos/page.tsx`             |
| 3   | `/vehiculos/[slug]`      | `(main)/vehiculos/[slug]/page.tsx`      |
| 4   | `/vehiculos/[slug]/360`  | `(main)/vehiculos/[slug]/360/page.tsx`  |
| 5   | `/buscar`                | `(main)/buscar/page.tsx`                |
| 6   | `/dealers`               | `(main)/dealers/page.tsx`               |
| 7   | `/dealers/[slug]`        | `(main)/dealers/[slug]/page.tsx`        |
| 8   | `/comparar`              | `(main)/comparar/page.tsx`              |
| 9   | `/vender` (landing)      | `(main)/vender/page.tsx`                |
| 10  | `/about`                 | `(main)/about/page.tsx`                 |
| 11  | `/nosotros`              | `(main)/nosotros/page.tsx`              |
| 12  | `/contacto`              | `(main)/contacto/page.tsx`              |
| 13  | `/precios`               | `(main)/precios/page.tsx`               |
| 14  | `/guias`                 | `(main)/guias/page.tsx`                 |
| 15  | `/blog`                  | `(main)/blog/page.tsx`                  |
| 16  | `/faq`                   | `(main)/faq/page.tsx`                   |
| 17  | `/ayuda`                 | `(main)/ayuda/page.tsx`                 |
| 18  | `/ayuda/[category]`      | `(main)/ayuda/[category]/page.tsx`      |
| 19  | `/ayuda/articulo/[slug]` | `(main)/ayuda/articulo/[slug]/page.tsx` |
| 20  | `/terminos`              | `(main)/terminos/page.tsx`              |
| 21  | `/privacidad`            | `(main)/privacidad/page.tsx`            |
| 22  | `/cookies`               | `(main)/cookies/page.tsx`               |
| 23  | `/seguridad`             | `(main)/seguridad/page.tsx`             |
| 24  | `/empleos`               | `(main)/empleos/page.tsx`               |
| 25  | `/prensa`                | `(main)/prensa/page.tsx`                |

### 🔓 Auth/Guest-Only Pages — 10 pages

| #   | Route                     | File                                     |
| --- | ------------------------- | ---------------------------------------- |
| 1   | `/login`                  | `(auth)/login/page.tsx`                  |
| 2   | `/registro`               | `(auth)/registro/page.tsx`               |
| 3   | `/registro/dealer`        | `(auth)/registro/dealer/page.tsx`        |
| 4   | `/recuperar-contrasena`   | `(auth)/recuperar-contrasena/page.tsx`   |
| 5   | `/restablecer-contrasena` | `(auth)/restablecer-contrasena/page.tsx` |
| 6   | `/reset-password`         | `(auth)/reset-password/page.tsx`         |
| 7   | `/forgot-password`        | `(auth)/forgot-password/page.tsx`        |
| 8   | `/crear-contrasena`       | `(auth)/crear-contrasena/page.tsx`       |
| 9   | `/verificar-email`        | `(auth)/verificar-email/page.tsx`        |
| 10  | `/callback/[provider]`    | `(auth)/callback/[provider]/page.tsx`    |

### 👤 Authenticated Buyer/User Pages (`/cuenta/*`) — 23 pages

| #   | Route                               | File                                               |
| --- | ----------------------------------- | -------------------------------------------------- |
| 1   | `/cuenta` (dashboard)               | `(main)/cuenta/page.tsx`                           |
| 2   | `/cuenta/perfil`                    | `(main)/cuenta/perfil/page.tsx`                    |
| 3   | `/cuenta/seguridad`                 | `(main)/cuenta/seguridad/page.tsx`                 |
| 4   | `/cuenta/favoritos`                 | `(main)/cuenta/favoritos/page.tsx`                 |
| 5   | `/cuenta/alertas`                   | `(main)/cuenta/alertas/page.tsx`                   |
| 6   | `/cuenta/busquedas`                 | `(main)/cuenta/busquedas/page.tsx`                 |
| 7   | `/cuenta/mensajes`                  | `(main)/cuenta/mensajes/page.tsx`                  |
| 8   | `/cuenta/mensajes/[conversationId]` | `(main)/cuenta/mensajes/[conversationId]/page.tsx` |
| 9   | `/cuenta/notificaciones`            | `(main)/cuenta/notificaciones/page.tsx`            |
| 10  | `/cuenta/historial`                 | `(main)/cuenta/historial/page.tsx`                 |
| 11  | `/cuenta/configuracion`             | `(main)/cuenta/configuracion/page.tsx`             |
| 12  | `/cuenta/estadisticas`              | `(main)/cuenta/estadisticas/page.tsx`              |
| 13  | `/cuenta/consultas`                 | `(main)/cuenta/consultas/page.tsx`                 |
| 14  | `/cuenta/resenas`                   | `(main)/cuenta/resenas/page.tsx`                   |
| 15  | `/cuenta/pagos`                     | `(main)/cuenta/pagos/page.tsx`                     |
| 16  | `/cuenta/pagos/callback`            | `(main)/cuenta/pagos/callback/page.tsx`            |
| 17  | `/cuenta/verificacion` (KYC)        | `(main)/cuenta/verificacion/page.tsx`              |
| 18  | `/cuenta/convert-to-seller`         | `(main)/cuenta/convert-to-seller/page.tsx`         |
| 19  | `/cuenta/suscripcion`               | `(main)/cuenta/suscripcion/page.tsx`               |
| 20  | `/cuenta/mis-vehiculos`             | `(main)/cuenta/mis-vehiculos/page.tsx`             |
| 21  | `/dashboard`                        | `(main)/dashboard/page.tsx`                        |
| 22  | `/mensajes`                         | `(messaging)/mensajes/page.tsx`                    |
| 23  | `/reportar`                         | `(main)/reportar/page.tsx`                         |

### 🏷️ Seller Pages (`/vender/*`, `/publicar/*`, `/mis-vehiculos/*`) — 17 pages

| #   | Route                         | File                                         |
| --- | ----------------------------- | -------------------------------------------- |
| 1   | `/vender/registro`            | `(main)/vender/registro/page.tsx`            |
| 2   | `/vender/dashboard`           | `(main)/vender/dashboard/page.tsx`           |
| 3   | `/vender/leads`               | `(main)/vender/leads/page.tsx`               |
| 4   | `/vender/publicidad`          | `(main)/vender/publicidad/page.tsx`          |
| 5   | `/vender/editar/[id]`         | `(main)/vender/editar/[id]/page.tsx`         |
| 6   | `/vender/promover/[id]`       | `(main)/vender/promover/[id]/page.tsx`       |
| 7   | `/vender/publicar`            | `(main)/vender/publicar/page.tsx`            |
| 8   | `/publicar`                   | `(main)/publicar/page.tsx`                   |
| 9   | `/publicar/fotos`             | `(main)/publicar/fotos/page.tsx`             |
| 10  | `/publicar/preview`           | `(main)/publicar/preview/page.tsx`           |
| 11  | `/mis-vehiculos`              | `(main)/mis-vehiculos/page.tsx`              |
| 12  | `/mis-vehiculos/[id]`         | `(main)/mis-vehiculos/[id]/page.tsx`         |
| 13  | `/mis-vehiculos/[id]/boost`   | `(main)/mis-vehiculos/[id]/boost/page.tsx`   |
| 14  | `/mis-vehiculos/[id]/stats`   | `(main)/mis-vehiculos/[id]/stats/page.tsx`   |
| 15  | `/impulsar`                   | `(main)/impulsar/page.tsx`                   |
| 16  | `/impulsar/mis-campanas`      | `(main)/impulsar/mis-campanas/page.tsx`      |
| 17  | `/impulsar/mis-campanas/[id]` | `(main)/impulsar/mis-campanas/[id]/page.tsx` |

### 🏢 Dealer Pages (`/dealer/*`) — 39 pages

| #   | Route                             | File                                             |
| --- | --------------------------------- | ------------------------------------------------ |
| 1   | `/dealer` (dashboard)             | `(main)/dealer/page.tsx`                         |
| 2   | `/dealer/inventario`              | `(main)/dealer/inventario/page.tsx`              |
| 3   | `/dealer/inventario/nuevo`        | `(main)/dealer/inventario/nuevo/page.tsx`        |
| 4   | `/dealer/inventario/importar`     | `(main)/dealer/inventario/importar/page.tsx`     |
| 5   | `/dealer/inventario/[id]`         | `(main)/dealer/inventario/[id]/page.tsx`         |
| 6   | `/dealer/inventario/[id]/boost`   | `(main)/dealer/inventario/[id]/boost/page.tsx`   |
| 7   | `/dealer/inventario/[id]/stats`   | `(main)/dealer/inventario/[id]/stats/page.tsx`   |
| 8   | `/dealer/analytics`               | `(main)/dealer/analytics/page.tsx`               |
| 9   | `/dealer/analytics/ventas`        | `(main)/dealer/analytics/ventas/page.tsx`        |
| 10  | `/dealer/analytics/inventario`    | `(main)/dealer/analytics/inventario/page.tsx`    |
| 11  | `/dealer/leads`                   | `(main)/dealer/leads/page.tsx`                   |
| 12  | `/dealer/leads/[id]`              | `(main)/dealer/leads/[id]/page.tsx`              |
| 13  | `/dealer/mensajes`                | `(main)/dealer/mensajes/page.tsx`                |
| 14  | `/dealer/citas`                   | `(main)/dealer/citas/page.tsx`                   |
| 15  | `/dealer/citas/calendario`        | `(main)/dealer/citas/calendario/page.tsx`        |
| 16  | `/dealer/resenas`                 | `(main)/dealer/resenas/page.tsx`                 |
| 17  | `/dealer/empleados`               | `(main)/dealer/empleados/page.tsx`               |
| 18  | `/dealer/empleados/nuevo`         | `(main)/dealer/empleados/nuevo/page.tsx`         |
| 19  | `/dealer/ubicaciones`             | `(main)/dealer/ubicaciones/page.tsx`             |
| 20  | `/dealer/ubicaciones/nueva`       | `(main)/dealer/ubicaciones/nueva/page.tsx`       |
| 21  | `/dealer/documentos`              | `(main)/dealer/documentos/page.tsx`              |
| 22  | `/dealer/facturacion`             | `(main)/dealer/facturacion/page.tsx`             |
| 23  | `/dealer/facturacion/historial`   | `(main)/dealer/facturacion/historial/page.tsx`   |
| 24  | `/dealer/suscripcion`             | `(main)/dealer/suscripcion/page.tsx`             |
| 25  | `/dealer/configuracion`           | `(main)/dealer/configuracion/page.tsx`           |
| 26  | `/dealer/perfil`                  | `(main)/dealer/perfil/page.tsx`                  |
| 27  | `/dealer/pricing`                 | `(main)/dealer/pricing/page.tsx`                 |
| 28  | `/dealer/reportes`                | `(main)/dealer/reportes/page.tsx`                |
| 29  | `/dealer/rendimiento`             | `(main)/dealer/rendimiento/page.tsx`             |
| 30  | `/dealer/publicar`                | `(main)/dealer/publicar/page.tsx`                |
| 31  | `/dealer/importar`                | `(main)/dealer/importar/page.tsx`                |
| 32  | `/dealer/registro`                | `(main)/dealer/registro/page.tsx`                |
| 33  | `/dealer/historial-pagos`         | `(main)/dealer/historial-pagos/page.tsx`         |
| 34  | `/dealer/publicidad`              | `(main)/dealer/publicidad/page.tsx`              |
| 35  | `/dealer/publicidad/nueva`        | `(main)/dealer/publicidad/nueva/page.tsx`        |
| 36  | `/dealer/publicidad/en-vivo`      | `(main)/dealer/publicidad/en-vivo/page.tsx`      |
| 37  | `/dealer/publicidad/estadisticas` | `(main)/dealer/publicidad/estadisticas/page.tsx` |
| 38  | `/dealer/publicidad/paquetes`     | `(main)/dealer/publicidad/paquetes/page.tsx`     |
| 39  | `/dealer/publicidad/roi`          | `(main)/dealer/publicidad/roi/page.tsx`          |

### 🛡️ Admin Pages (`/admin/*`) — 38 pages

| #   | Route                              | File                                                         |
| --- | ---------------------------------- | ------------------------------------------------------------ |
| 1   | `/admin` (dashboard)               | `(admin)/admin/page.tsx`                                     |
| 2   | `/admin/usuarios`                  | `(admin)/admin/usuarios/page.tsx`                            |
| 3   | `/admin/usuarios/[id]`             | `(admin)/admin/usuarios/[id]/page.tsx`                       |
| 4   | `/admin/vehiculos`                 | `(admin)/admin/vehiculos/page.tsx`                           |
| 5   | `/admin/dealers`                   | `(admin)/admin/dealers/page.tsx`                             |
| 6   | `/admin/dealers/[id]`              | `(admin)/admin/dealers/[id]/page.tsx`                        |
| 7   | `/admin/reportes`                  | `(admin)/admin/reportes/page.tsx`                            |
| 8   | `/admin/kyc`                       | `(admin)/admin/kyc/page.tsx`                                 |
| 9   | `/admin/kyc/[id]`                  | `(admin)/admin/kyc/[id]/page.tsx`                            |
| 10  | `/admin/reviews`                   | `(admin)/admin/reviews/page.tsx`                             |
| 11  | `/admin/analytics`                 | `(admin)/admin/analytics/page.tsx`                           |
| 12  | `/admin/mensajes`                  | `(admin)/admin/mensajes/page.tsx`                            |
| 13  | `/admin/configuracion`             | `(admin)/admin/configuracion/page.tsx`                       |
| 14  | `/admin/sistema`                   | `(admin)/admin/sistema/page.tsx`                             |
| 15  | `/admin/mantenimiento`             | `(admin)/admin/mantenimiento/page.tsx`                       |
| 16  | `/admin/equipo`                    | `(admin)/admin/equipo/page.tsx`                              |
| 17  | `/admin/equipo/[id]`               | `(admin)/admin/equipo/[id]/page.tsx`                         |
| 18  | `/admin/equipo/invitar`            | `(admin)/admin/equipo/invitar/page.tsx`                      |
| 19  | `/admin/equipo/invitaciones`       | `(admin)/admin/equipo/invitaciones/page.tsx`                 |
| 20  | `/admin/equipo/invitacion/aceptar` | `(admin)/admin/equipo/invitacion/aceptar/page.tsx`           |
| 21  | `/admin/equipo/departamentos`      | `(admin)/admin/equipo/departamentos/page.tsx`                |
| 22  | `/admin/equipo/posiciones`         | `(admin)/admin/equipo/posiciones/page.tsx`                   |
| 23  | `/admin/facturacion`               | `(admin)/admin/facturacion/page.tsx`                         |
| 24  | `/admin/leads`                     | `(admin)/admin/leads/page.tsx`                               |
| 25  | `/admin/roles`                     | `(admin)/admin/roles/page.tsx`                               |
| 26  | `/admin/compliance`                | `(admin)/admin/compliance/page.tsx`                          |
| 27  | `/admin/soporte`                   | `(admin)/admin/soporte/page.tsx`                             |
| 28  | `/admin/soporte/[id]`              | `(admin)/admin/soporte/[id]/page.tsx`                        |
| 29  | `/admin/contenido`                 | `(admin)/admin/contenido/page.tsx`                           |
| 30  | `/admin/suscripciones`             | `(admin)/admin/suscripciones/page.tsx`                       |
| 31  | `/admin/transacciones`             | `(admin)/admin/transacciones/page.tsx`                       |
| 32  | `/admin/early-bird`                | `(admin)/admin/early-bird/page.tsx`                          |
| 33  | `/admin/banners`                   | `(admin)/admin/banners/page.tsx`                             |
| 34  | `/admin/promociones`               | `(admin)/admin/promociones/page.tsx`                         |
| 35  | `/admin/okla-score`                | `(admin)/admin/okla-score/page.tsx`                          |
| 36  | `/admin/search-agent`              | `(admin)/admin/search-agent/page.tsx`                        |
| 37  | `/admin/publicidad`                | `(admin)/admin/publicidad/page.tsx`                          |
| 38  | `/admin/advertising`               | `(main)/admin/advertising/page.tsx` ⚠️ **Wrong route group** |

### 🔧 Utility/Special Pages — 10 pages

| #   | Route                 | File                                 | Notes                    |
| --- | --------------------- | ------------------------------------ | ------------------------ |
| 1   | `/mantenimiento`      | `mantenimiento/page.tsx`             | Outside route groups     |
| 2   | `/403`                | `(main)/403/page.tsx`                | Forbidden                |
| 3   | `/500`                | `(main)/500/page.tsx`                | Server Error             |
| 4   | `/offline`            | `(main)/offline/page.tsx`            | PWA offline              |
| 5   | `/checkout`           | `(main)/checkout/page.tsx`           | Auth required            |
| 6   | `/checkout/exito`     | `(main)/checkout/exito/page.tsx`     |                          |
| 7   | `/checkout/cancelado` | `(main)/checkout/cancelado/page.tsx` |                          |
| 8   | `/checkout/error`     | `(main)/checkout/error/page.tsx`     |                          |
| 9   | `/okla-score`         | `(main)/okla-score/page.tsx`         | Public tool              |
| 10  | `/test-chatbot`       | `(main)/test-chatbot/page.tsx`       | ⚠️ **DEV ONLY — remove** |

---

## 2. Issues Found

### 🔴 CRITICAL Issues

#### C1. Stripe Webhook — Signature NOT verified

- **File:** `src/app/api/webhook/stripe/route.ts` line 9
- **Issue:** `_STRIPE_WEBHOOK_SECRET` is declared (with `_` prefix indicating unused) but **never used** to verify the webhook signature. The comment says "In production, verify the signature" — this is NOT production-safe.
- **Impact:** Any party can forge Stripe webhook calls and trigger payment events.
- **Fix:** Implement Stripe signature verification using the `stripe` SDK.

#### C2. AZUL Webhook — No signature/secret verification

- **File:** `src/app/api/webhook/azul/route.ts` lines 11-35
- **Issue:** No authentication/verification of any kind. Webhook body is logged in full (including potentially sensitive payment data) and blindly forwarded.
- **Impact:** Anyone can POST to `/api/webhook/azul` and trigger payment processing.
- **Fix:** Add HMAC signature verification or shared secret check.

#### C3. AZUL Webhook — Sensitive data logged to console

- **File:** `src/app/api/webhook/azul/route.ts` line 16
- **Issue:** `console.log('AZUL webhook received:', JSON.stringify(body, null, 2))` logs the **entire webhook payload** including payment information.
- **Fix:** Remove or mask sensitive fields before logging.

#### C4. Revalidation Route — Secret check is optional

- **File:** `src/app/api/revalidate/route.ts` lines 16-18
- **Issue:** `if (expectedSecret && secret !== expectedSecret)` — if `REVALIDATION_SECRET` env var is not set, **any request** can revalidate paths.
- **Fix:** Make the secret required: fail if `REVALIDATION_SECRET` is undefined.

#### C5. Test/Dev page deployed to production

- **File:** `src/app/(main)/test-chatbot/page.tsx`
- **Issue:** This is explicitly a test page (`test-chatbot`) that connects to `http://localhost:8000` (LLM) and `http://localhost:5060` (ChatBot API) with hardcoded dev fallbacks. It's accessible at `/test-chatbot` without auth.
- **Fix:** Either remove from production build, add middleware protection, or move behind `/admin`.

#### C6. Admin advertising page in wrong route group

- **File:** `src/app/(main)/admin/advertising/page.tsx`
- **Issue:** This admin page lives under `(main)` instead of `(admin)` route group. It may lack the admin layout and admin-specific middleware protections.
- **Fix:** Move to `src/app/(admin)/admin/advertising/page.tsx`.

---

### 🟠 HIGH Issues

#### H1. Debug `console.log` statements in production — `settings.ts`

- **File:** `src/services/settings.ts` lines 146-179
- **Issue:** 10+ verbose debug log statements in `applyTheme()` that fire on every theme change, including CSS variable values. These are clearly debug remnants.
- **Fix:** Remove all `console.log` calls from `applyTheme()`.

#### H2. Hardcoded `https://okla.com.do` in share dialog

- **File:** `src/components/ui/share-dialog.tsx` lines 145-146
- **Issue:** URL is hardcoded instead of using `process.env.NEXT_PUBLIC_SITE_URL` or `window.location.origin`. The ternary for `typeof window` check produces the same value on both sides.
- **Fix:** Use `process.env.NEXT_PUBLIC_SITE_URL || window.location.origin`.

#### H3. Missing error boundaries for most route segments

- **Existing `error.tsx` files (only 2):**
  - `src/app/error.tsx` (root)
  - `src/app/(main)/vender/registro/error.tsx`
- **Missing for:** `(admin)/admin/`, `(auth)/`, `(messaging)/`, `(main)/dealer/`, `(main)/cuenta/`, `(main)/checkout/`
- **Fix:** Add `error.tsx` boundaries for each major route group.

#### H4. Missing `loading.tsx` for several route segments

- **Existing:** root, admin, buscar, dealers, vender, publicar, checkout, vehiculos, comparar, cuenta, mis-vehiculos, dealer (13 total)
- **Missing for:** `(auth)`, `(messaging)`, `impulsar`, `ayuda`
- **Fix:** Add `loading.tsx` for better perceived performance.

#### H5. Pages using `useSearchParams()` without Suspense boundary

- `(main)/publicar/fotos/page.tsx` line 61 — ❌ **No Suspense wrapper**
- `(admin)/admin/equipo/invitacion/aceptar/page.tsx` line 48 — ❌ **No Suspense wrapper**
- (Other pages using `useSearchParams` — login, restablecer-contrasena, verificar-email, comparar, checkout/exito, checkout/error, cuenta/pagos/callback — all properly use Suspense ✅)
- **Fix:** Wrap these two pages in `<Suspense>`.

---

### 🟡 MEDIUM Issues

#### M1. `console.log` statements in production (160+ total matches)

**Key files to clean up:**

| File                                              | Lines                          | Statement                         | Action                 |
| ------------------------------------------------- | ------------------------------ | --------------------------------- | ---------------------- |
| `services/settings.ts`                            | 146-179                        | 10 debug logs in `applyTheme()`   | ❌ Remove              |
| `services/auth.ts`                                | 594, 623                       | `console.log('[getSessions]...')` | ❌ Remove              |
| `components/pwa/install-prompt.tsx`               | 116, 120, 144, 149             | PWA install debug logs            | ❌ Remove              |
| `components/pwa/pwa-wrapper.tsx`                  | 26, 40                         | PWA install event logs            | ❌ Remove              |
| `components/pwa/service-worker-provider.tsx`      | 68, 74, 85, 103, 125, 139, 169 | SW lifecycle logs                 | Gate behind `NODE_ENV` |
| `components/chat/ChatWidget.tsx`                  | 26, 29, 32                     | Chat event logs                   | ❌ Remove              |
| `components/vehicle-detail/VehicleChatWidget.tsx` | 34, 37, 40                     | Chat event logs                   | ❌ Remove              |
| `components/monitoring/web-vitals.tsx`            | 143, 215                       | Render time logs                  | ❌ Remove              |
| `app/(auth)/callback/[provider]/page.tsx`         | 42                             | OAuth callback debug              | ❌ Remove              |
| `app/(admin)/admin/kyc/[id]/page.tsx`             | 106                            | Documents fetch debug             | ❌ Remove              |
| `app/api/webhook/azul/route.ts`                   | 16                             | **Full webhook payload**          | 🔴 Security risk       |

**~100 `console.error` in catch blocks** — Acceptable for server-side but consider structured logger (pino).

#### M2. `window.confirm()` used instead of custom dialog

- **File:** `src/app/(admin)/admin/roles/page.tsx` line 451
- **Issue:** `window.confirm()` is used for role deletion — inconsistent with shadcn/ui design system.
- **Fix:** Use `AlertDialog` component from shadcn/ui.

#### M3. Hardcoded `https://okla.com.do` usage

| File                                 | Lines   | Context                    | Severity |
| ------------------------------------ | ------- | -------------------------- | -------- |
| `components/ui/share-dialog.tsx`     | 145-146 | Share URL — **no env var** | ⚠️ Fix   |
| `providers/site-config-provider.tsx` | 61      | Default fallback           | ✅ OK    |
| `lib/seo.tsx`                        | 86      | SEO metadata fallback      | ✅ OK    |
| `app/robots.ts`                      | 9       | Robots.txt fallback        | ✅ OK    |
| `app/layout.tsx`                     | 44      | metadataBase fallback      | ✅ OK    |
| `app/api/sitemap/route.ts`           | 9       | Sitemap fallback           | ✅ OK    |
| `app/sitemap.ts`                     | 13      | Sitemap fallback           | ✅ OK    |

**Only `share-dialog.tsx` is truly hardcoded** — all others use `process.env.NEXT_PUBLIC_SITE_URL || 'https://okla.com.do'` pattern (acceptable).

#### M4. `http://localhost` fallback URLs in production code

| File                           | Fallback URL                           | Risk                             |
| ------------------------------ | -------------------------------------- | -------------------------------- |
| All API routes                 | `http://localhost:18443` (via env var) | Low — server-only                |
| `api/upload/route.ts`          | `http://localhost:8080`                | Low — server-only                |
| `api/webhook/stripe/route.ts`  | `http://localhost:8080`                | Low — server-only                |
| `api/webhook/azul/route.ts`    | `http://localhost:8080`                | Low — server-only                |
| `lib/api-url.ts`               | `http://localhost:18443`               | Medium — used by client code too |
| `test-chatbot/page.tsx`        | `http://localhost:8000`, `:5060`       | 🔴 Client-exposed                |
| `callback/[provider]/page.tsx` | `http://localhost:18443`               | Medium — client-side OAuth       |

---

## 3. API Routes Audit

### Summary: 23 API Routes

| Route                                | Methods   | Has try/catch | Error Format | Issues                              |
| ------------------------------------ | --------- | ------------- | ------------ | ----------------------------------- |
| `/api/health`                        | GET       | No            | JSON         | ✅ Simple, no error handling needed |
| `/api/pricing`                       | GET       | ✅            | JSON         | ✅ Clean, multi-fallback            |
| `/api/kyc-config`                    | GET       | ✅            | JSON         | ✅ Clean, multi-fallback            |
| `/api/upload`                        | POST      | ✅            | JSON         | ✅ OK                               |
| `/api/revalidate`                    | POST      | ✅            | JSON         | ⚠️ Optional secret (C4)             |
| `/api/sitemap`                       | GET       | ✅            | XML          | ✅ OK                               |
| `/api/webhook/stripe`                | POST      | ✅            | JSON         | 🔴 No sig verification (C1)         |
| `/api/webhook/azul`                  | POST      | ✅            | JSON         | 🔴 No auth + logs body (C2, C3)     |
| `/api/analytics/track`               | POST, GET | ✅            | JSON         | ✅ OK                               |
| `/api/analytics/leads`               | GET       | ✅            | JSON         | ✅ OK                               |
| `/api/alerts/stats`                  | GET       | ✅            | JSON         | ✅ OK                               |
| `/api/score/calculate`               | POST      | ✅            | JSON         | ✅ OK                               |
| `/api/score/vin-decode`              | GET       | ✅            | JSON         | ✅ OK                               |
| `/api/score/recalls`                 | GET       | ✅            | JSON         | ✅ OK                               |
| `/api/score/safety`                  | GET       | ✅            | JSON         | ✅ OK                               |
| `/api/score/complaints`              | GET       | ✅            | JSON         | ✅ OK                               |
| `/api/advertising/campaigns`         | POST, GET | ✅            | JSON         | ✅ OK                               |
| `/api/advertising/reports`           | GET       | ✅            | JSON         | ✅ OK                               |
| `/api/advertising/sponsored`         | GET       | ✅            | JSON         | ✅ OK                               |
| `/api/advertising/targeted`          | GET       | ✅            | JSON         | ✅ OK                               |
| `/api/advertising/tracking`          | POST      | ✅            | JSON         | ✅ OK                               |
| `/api/advertising/live-dashboard`    | GET       | ✅            | JSON         | ✅ OK                               |
| `/api/advertising/advertiser-report` | GET       | ✅            | JSON         | ✅ OK                               |

**All API routes use consistent error format** (`{ error: string }`) and `NextResponse.json()`.

---

## 4. Console Statements Summary

| Category                        | Count    | Action                                        |
| ------------------------------- | -------- | --------------------------------------------- |
| Debug `console.log` (remove)    | ~45      | ❌ Remove for production                      |
| `console.warn` (review)         | ~15      | Review case-by-case                           |
| `console.error` in catch blocks | ~100     | ⚠️ OK server-side; consider structured logger |
| **Total**                       | **~160** |                                               |

---

## 5. Hardcoded URLs / Secrets Summary

### ✅ No actual secrets found in source code

No hardcoded API keys, passwords, or tokens were found. Environment variables are used correctly.

### ⚠️ URLs requiring fixes

| File                               | Issue                                                    |
| ---------------------------------- | -------------------------------------------------------- |
| `components/ui/share-dialog.tsx`   | `https://okla.com.do` hardcoded without env var fallback |
| `app/(main)/test-chatbot/page.tsx` | `http://localhost:8000` and `:5060` exposed client-side  |

---

## 6. Route Protection Audit

### Middleware Coverage

The middleware at `src/middleware.ts` correctly handles:

- ✅ Public routes — well-defined list
- ✅ Guest-only routes — redirect if authenticated
- ✅ Authenticated routes — redirect to login if no token
- ✅ Role-based routes — dealer, admin, seller roles enforced
- ✅ JWT decoding (payload only — crypto verification at gateway)

### Protection Gaps

| Route                | Issue                                                |
| -------------------- | ---------------------------------------------------- |
| `/admin/advertising` | Under `(main)` — may bypass admin layout/protections |
| `/test-chatbot`      | Not protected — accessible without auth              |

### Layout Hierarchy

```
app/layout.tsx (root)
├── (auth)/layout.tsx — No navbar/footer
├── (main)/layout.tsx — With navbar/footer
│   ├── cuenta/layout.tsx — AuthGuard + sidebar
│   ├── dealer/layout.tsx — AuthGuard + dealer sidebar
│   └── dashboard/layout.tsx
├── (admin)/layout.tsx — No navbar/footer
│   └── admin/layout.tsx — Admin-specific
├── (messaging)/layout.tsx
└── mantenimiento/layout.tsx — Standalone
```

---

## 7. Recommendations (Priority Order)

### 🔴 Critical — Fix Immediately

1. **Implement Stripe webhook signature verification** in `api/webhook/stripe/route.ts`
2. **Add AZUL webhook authentication** in `api/webhook/azul/route.ts`
3. **Remove sensitive data logging** from AZUL webhook route (line 16)
4. **Make revalidation secret required** in `api/revalidate/route.ts`
5. **Remove or protect `/test-chatbot`** — not for production

### 🟠 High — Fix Before Next Release

6. **Move `(main)/admin/advertising/`** → `(admin)/admin/advertising/`
7. **Remove ~45 debug `console.log`** statements (see M1 table for files)
8. **Add `error.tsx` boundaries** for `(admin)`, `dealer`, `cuenta`, `checkout` groups
9. **Add Suspense wrappers** for `publicar/fotos/page.tsx` and `admin/equipo/invitacion/aceptar/page.tsx`

### 🟡 Medium — Address In Sprint

10. **Fix hardcoded URL** in `share-dialog.tsx` — use env var
11. **Add `loading.tsx`** for `(auth)`, `(messaging)`, `impulsar`, `ayuda`
12. **Replace `window.confirm()`** with shadcn `AlertDialog` in `admin/roles/page.tsx`
13. **Gate PWA/SW `console.log`** behind `process.env.NODE_ENV !== 'production'`

### 🟢 Low — Tech Debt

14. Adopt structured logging (e.g., `pino`) instead of `console.error` in API routes
15. Add `not-found.tsx` pages for dynamic route segments (`[slug]`, `[id]`)
16. Standardize all `console.error` formats across service files
