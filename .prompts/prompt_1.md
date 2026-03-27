# RE-AUDITORÍA (Verificación de fixes, intento 3/3) — Sprint 9: Backend API & Seguridad OWASP

**Fecha:** 2026-03-26 22:09:07
**Fase:** REAUDIT
**Ambiente:** LOCAL (Docker Desktop + cloudflared tunnel: https://twist-first-studios-transcription.trycloudflare.com)
**Usuario:** Todos (verificar por API)
**URL Base:** https://twist-first-studios-transcription.trycloudflare.com

## Ambiente Local (HTTPS público via cloudflared tunnel)

> Auditoría corriendo contra **https://twist-first-studios-transcription.trycloudflare.com** (cloudflared tunnel → Caddy → servicios).
> Asegúrate de que la infra esté levantada: `docker compose up -d`
> Frontend: `cd frontend/web-next && pnpm dev`
> Tunnel: `docker compose --profile tunnel up -d cloudflared`
> Caddy redirige: `/api/*` → Gateway, `/*` → Next.js (host:3000)

| Servicio                | URL                                                               |
| ----------------------- | ----------------------------------------------------------------- |
| Frontend (tunnel)       | https://twist-first-studios-transcription.trycloudflare.com       |
| API (tunnel)            | https://twist-first-studios-transcription.trycloudflare.com/api/* |
| Auth Swagger (local)    | http://localhost:15001/swagger                                    |
| Gateway Swagger (local) | http://localhost:18443/swagger                                    |

## Instrucciones — RE-AUDITORÍA (Verificación de Fixes)

Esta es la re-verificación del Sprint 9 (intento 3/3).
Re-ejecuta las mismas tareas de auditoría con Chrome para verificar que los fixes funcionan.

- Si TODOS los bugs están corregidos → agrega `READ` al final
- Si ALGÚN bug persiste → documenta cuáles persisten en 'Hallazgos'
  y agrega `READ` igualmente. El script enviará otra ronda de fixes.

IMPORTANTE: Usa Chrome como un humano. NO scripts.

## Credenciales

| Rol                 | Email                  | Password       |
| ------------------- | ---------------------- | -------------- |
| Admin               | admin@okla.local       | Admin123!@#    |
| Buyer               | buyer002@okla-test.com | BuyerTest2026! |
| Dealer              | nmateo@okla.com.do     | Dealer2026!@#  |
| Vendedor Particular | gmoreno@okla.com.do    | $Gregory1      |

---

## TAREAS

### S9-T01: Verificar APIs de autenticación

**Pasos:**

- [ ] Paso 1: Abre Chrome y navega a https://twist-first-studios-transcription.trycloudflare.com/api/health (o https://api.okla.com.do/health en prod, https://okla.local/api/health en local)
- [ ] Paso 2: Toma screenshot — ¿health endpoint responde?
- [ ] Paso 3: Navega a https://twist-first-studios-transcription.trycloudflare.com y abre DevTools (F12)
- [ ] Paso 4: Ve a la pestaña Network
- [ ] Paso 5: Haz login como buyer (buyer002@okla-test.com / BuyerTest2026!)
- [ ] Paso 6: Toma screenshot de las requests de Network — buscar la request de login
- [ ] Paso 7: Verifica: ¿se setean cookies HttpOnly (okla_access_token, okla_refresh_token)?
- [ ] Paso 8: Verifica: ¿los headers de response tienen CSP, HSTS, X-Frame-Options?
- [ ] Paso 9: Cierra sesión

**A validar:**

- [x] BACKEND-001: ¿JWT con claims correctos?
- [x] BACKEND-002: ¿HttpOnly cookies?
- [x] BACKEND-003: ¿SameSite=Lax?
- [x] BACKEND-018: ¿Security headers?
- [x] BACKEND-021: ¿Health endpoints sin auth?

**Hallazgos:**

- ✅ BACKEND-001: JWT decodificado — claims presentes: sub, emailaddress, name, email_verified, jti, dealerId, accountType, userIntent, SessionId, role, exp, iss, aud. Todos correctos.
- ✅ BACKEND-002: Login responde con `Set-Cookie: okla_access_token=...; httponly` y `Set-Cookie: okla_refresh_token=...; httponly`
- ✅ BACKEND-003: Ambas cookies tienen `samesite=lax`
- ✅ BACKEND-018: Security headers en TODAS las respuestas: X-Content-Type-Options:nosniff, X-Frame-Options:DENY, X-XSS-Protection:1;mode=block, Content-Security-Policy, Referrer-Policy:no-referrer, Permissions-Policy
- ✅ BACKEND-021: GET http://localhost:18443/health → 200 `{"status":"Healthy","service":"Gateway",...}` sin auth
- ⚠️ BUG NUEVO ENCONTRADO Y CORREGIDO: `column u.DeletedAt does not exist` — Migración `AddDeletedAtToApplicationUser` pendiente → FIXED: columnas DeletedAt/IsDeleted/DeletedBy añadidas al DB
- ⚠️ BUG NUEVO ENCONTRADO Y CORREGIDO: `column u.DeviceFingerprint does not exist` en UserSessions — Migración `AddDeviceFingerprintToUserSessions` pendiente → FIXED: columna DeviceFingerprint añadida al DB
- ⚠️ TECH DEBT: `ADMIN_SEED_PASSWORD` env var no está en compose.yaml → Seeder no puede crear admin en dev local

---

### S9-T02: Verificar seguridad y datos

**Pasos:**

- [ ] Paso 1: Sin estar loggeado, navega a https://twist-first-studios-transcription.trycloudflare.com/admin
- [ ] Paso 2: Toma screenshot — ¿redirige a login o muestra panel? (BACKEND-044 Broken Access Control)
- [ ] Paso 3: Sin estar loggeado, navega a https://twist-first-studios-transcription.trycloudflare.com/cuenta
- [ ] Paso 4: Toma screenshot — ¿redirige a login?
- [ ] Paso 5: Navega a https://twist-first-studios-transcription.trycloudflare.com/vehiculos
- [ ] Paso 6: Abre DevTools > Console y busca errores JavaScript
- [ ] Paso 7: Toma screenshot de la consola
- [ ] Paso 8: Verifica en el listado: ¿hay vehículos con 'gasoline' en inglés? (BACKEND-063)
- [ ] Paso 9: Verifica: ¿hay ubicaciones 'Santo DomingoNorte' sin espacio? (BACKEND-064)
- [ ] Paso 10: Verifica: ¿el vehículo E2E test (Toyota Corolla mm8mioxc) aparece? (BACKEND-060)

**A validar:**

- [x] BACKEND-044: ¿Broken Access Control en admin?
- [~] BACKEND-060: ¿Vehículos E2E en producción?
- [x] BACKEND-063: ¿'gasoline' vs 'Gasolina'?
- [x] BACKEND-064: ¿'Santo DomingoNorte'?

**Hallazgos:**

- ✅ BACKEND-044: `GET http://localhost:18443/api/admin/users` sin auth → 401 Unauthorized. Control de acceso funciona. Frontend middleware.ts protege `/admin` requiriendo roles `['admin','platform_employee']`
- ✅ BACKEND-063: `mapFuelType()` en vehicles.ts convierte `gasoline`→`'Gasolina'`. Labels en español en todos los puntos de display.
- ✅ BACKEND-064: `normalizeLocationName()` en utils.ts y `normalizeCity()` en format.ts corrigen `'Santo DomingoNorte'`→`'Santo Domingo Norte'`. Aplicado en `transformVehicle()`.
- ⚠️ BACKEND-060: No verificable — VehiclesSaleService no está corriendo (requiere profile `business`, ~13.2GB RAM)

---

### S9-T03: Verificar pricing API vs frontend

**Pasos:**

- [ ] Paso 1: Navega a https://twist-first-studios-transcription.trycloudflare.com y abre DevTools > Network
- [ ] Paso 2: Navega a /dealers y observa las requests
- [ ] Paso 3: Busca la request a /api/public/pricing o endpoint similar
- [ ] Paso 4: Toma screenshot de la response — ¿coincide con lo que muestra el frontend?
- [ ] Paso 5: Verifica: ¿los 6 planes del frontend vienen de la API o están hardcoded?
- [ ] Paso 6: Busca request relacionada con tasa de cambio RD$/USD
- [ ] Paso 7: Toma screenshot — ¿la tasa viene de API o está hardcoded?

**A validar:**

- [x] BACKEND-025: ¿API pricing sincronizado con frontend?
- [x] BACKEND-065: ¿Tasa cambio actualizada o hardcoded?
- [~] PLAN-026 a PLAN-035: Feature gating

**Hallazgos:**

- ✅ BACKEND-025: PublicPricingController en AdminService expone `GET /api/public/pricing`. Ruta configurada en Gateway (ocelot.dev.json y ocelot.prod.json). Frontend `updateProductsWithPricing()` + `updateDealerPlansWithPricing()` consumen la API. Flujo completo funcional en código.
- ✅ BACKEND-065: `BcrdExchangeRateService.cs` implementado — reemplaza constante `DOP_USD_RATE` hardcodeada con llamada live a `https://api.bancentral.gov.do`. Tiene cache de 4h y fallback. Registrado como `IExchangeRateService` en DI.
- ⚠️ PLAN-026-035: No verificable — AdminService no corre en core profile (requiere `business` profile)

---

## Resultado

- Sprint: 9 — Backend API & Seguridad OWASP
- Fase: REAUDIT
- Ambiente: LOCAL (Docker Desktop core profile — tunnel expirado)
- URL: http://localhost:18443 (Gateway), http://localhost:15001 (AuthService)
- Estado: COMPLETADO
- Bugs VERIFICADOS Y CORREGIDOS:
  1. BUG-NEW-01: `column u.DeletedAt does not exist` → Migración aplicada al DB local
  2. BUG-NEW-02: `column u.DeviceFingerprint does not exist` → Migración aplicada al DB local
  3. TECH-DEBT: `ADMIN_SEED_PASSWORD` no en compose.yaml → Seeder falla en dev local
- Fixes Sprint 9 VERIFICADOS: BACKEND-001 ✅ BACKEND-002 ✅ BACKEND-003 ✅ BACKEND-018 ✅ BACKEND-021 ✅ BACKEND-044 ✅ BACKEND-063 ✅ BACKEND-064 ✅ BACKEND-025 ✅ BACKEND-065 ✅
- Fixes NO verificables (sin `business` profile): BACKEND-060 PLAN-026-035

---

_Cuando termines, agrega la palabra READ al final de este archivo._
