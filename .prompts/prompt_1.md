# RE-AUDITORÍA (Verificación de fixes, intento 2/3) — Sprint 9: Backend API & Seguridad OWASP
**Fecha:** 2026-03-26 21:58:36
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

| Servicio | URL |
|----------|-----|
| Frontend (tunnel) | https://twist-first-studios-transcription.trycloudflare.com |
| API (tunnel) | https://twist-first-studios-transcription.trycloudflare.com/api/* |
| Auth Swagger (local) | http://localhost:15001/swagger |
| Gateway Swagger (local) | http://localhost:18443/swagger |

## Instrucciones — RE-AUDITORÍA (Verificación de Fixes)
Esta es la re-verificación del Sprint 9 (intento 2/3).
Re-ejecuta las mismas tareas de auditoría con Chrome para verificar que los fixes funcionan.

- Si TODOS los bugs están corregidos → agrega `READ` al final
- Si ALGÚN bug persiste → documenta cuáles persisten en 'Hallazgos'
  y agrega `READ` igualmente. El script enviará otra ronda de fixes.

IMPORTANTE: Usa Chrome como un humano. NO scripts.

## Credenciales
| Rol | Email | Password |
|-----|-------|----------|
| Admin | admin@okla.local | Admin123!@# |
| Buyer | buyer002@okla-test.com | BuyerTest2026! |
| Dealer | nmateo@okla.com.do | Dealer2026!@# |
| Vendedor Particular | gmoreno@okla.com.do | $Gregory1 |

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
- [ ] BACKEND-001: ¿JWT con claims correctos?
- [ ] BACKEND-002: ¿HttpOnly cookies?
- [ ] BACKEND-003: ¿SameSite=Lax?
- [ ] BACKEND-018: ¿Security headers?
- [ ] BACKEND-021: ¿Health endpoints sin auth?

**Hallazgos:**
_(documentar aquí lo encontrado)_

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
- [ ] BACKEND-044: ¿Broken Access Control en admin?
- [ ] BACKEND-060: ¿Vehículos E2E en producción?
- [ ] BACKEND-063: ¿'gasoline' vs 'Gasolina'?
- [ ] BACKEND-064: ¿'Santo DomingoNorte'?

**Hallazgos:**
_(documentar aquí lo encontrado)_

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
- [ ] BACKEND-025: ¿API pricing sincronizado con frontend?
- [ ] BACKEND-065: ¿Tasa cambio actualizada o hardcoded?
- [ ] PLAN-026 a PLAN-035: Feature gating

**Hallazgos:**
_(documentar aquí lo encontrado)_

---

## Resultado
- Sprint: 9 — Backend API & Seguridad OWASP
- Fase: REAUDIT
- Ambiente: LOCAL (Docker Desktop + cloudflared tunnel: https://twist-first-studios-transcription.trycloudflare.com)
- URL: https://twist-first-studios-transcription.trycloudflare.com
- Estado: EN PROGRESO
- Bugs encontrados: _(completar)_

---

_Cuando termines, agrega la palabra READ al final de este archivo._
