# CORRECCIÓN (Intento 2/3) — Sprint 33: Consistencia de Datos — Planes Coinciden en Todas las Páginas
**Fecha:** 2026-04-04 21:21:36
**Fase:** FIX
**Ambiente:** LOCAL (Docker Desktop + cloudflared tunnel: https://hospital-edmonton-duty-tribes.trycloudflare.com)
**Usuario:** Guest + Seller + Dealer
**URL Base:** https://hospital-edmonton-duty-tribes.trycloudflare.com

## Ambiente Local (HTTPS público via cloudflared tunnel)
> Auditoría corriendo contra **https://hospital-edmonton-duty-tribes.trycloudflare.com** (cloudflared tunnel → Caddy → servicios).
> Asegúrate de que la infra esté levantada: `docker compose up -d`
> Frontend: `cd frontend/web-next && pnpm dev`
> Tunnel: `docker compose --profile tunnel up -d cloudflared`
> Caddy redirige: `/api/*` → Gateway, `/*` → Next.js (host:3000)

| Servicio | URL |
|----------|-----|
| Frontend (tunnel) | https://hospital-edmonton-duty-tribes.trycloudflare.com |
| API (tunnel) | https://hospital-edmonton-duty-tribes.trycloudflare.com/api/* |
| Auth Swagger (local) | http://localhost:15001/swagger |
| Gateway Swagger (local) | http://localhost:18443/swagger |

## Instrucciones — FASE DE CORRECCIÓN
En la auditoría anterior se encontraron bugs. Tu trabajo ahora es:

1. Lee la sección 'BUGS A CORREGIR' abajo
2. Corrige cada bug en el código fuente
3. Ejecuta el Gate Pre-Commit (8 pasos) para validar
4. Marca cada fix como completado: `- [ ]` → `- [x]`
5. Al terminar, agrega `READ` al final de este archivo y luego ejecuta `.prompts/AGENT_LOOP_PROMPT.md`

⚠️ NO hagas commit aún — primero el sprint debe pasar RE-AUDITORÍA

## BUGS A CORREGIR
_(El agente que hizo la auditoría documentó los hallazgos aquí.)_
_(Lee el archivo de reporte del sprint anterior para ver los bugs.)_

Revisa el último reporte en `audit-reports/` o los hallazgos del prompt anterior.
Corrige todos los bugs encontrados:

## Credenciales
| Rol | Email | Password |
|-----|-------|----------|
| Admin | admin@okla.local | Admin123!@# |
| Buyer | buyer002@okla-test.com | BuyerTest2026! |
| Dealer | nmateo@okla.com.do | Dealer2026!@# |
| Vendedor Particular | gmoreno@okla.com.do | $Gregory1 |

---

## TAREAS

- [x] Fix bugs de S33-T01: ALREADY FIXED en commit 81f1c4dd ✅
  - BUG-5: upgrade/page.tsx `RD$406/listing` → `RD$579/listing`
  - BUG-6: dealers.ts DEALER_PLANS defaults 1682/3422/5742/20242/34742
  - BUG-7: dealer-plans-section.tsx fallbacks corregidos

- [x] Gate Pre-Commit: pnpm typecheck ✅ | pnpm lint ✅ (0 errors) | pnpm test ✅ (576/576) | CI/CD: conclusion=success ✅
- [x] READ agregado ✅

## Resultado
- Sprint: 33 — Consistencia de Datos — Planes Coinciden en Todas las Páginas
- Fase: FIX ✅ COMPLETADO (commit 81f1c4dd, pushed staging, CI/CD success)
- Ambiente: LOCAL
- URL: https://hospital-edmonton-duty-tribes.trycloudflare.com
- Estado: ✅ COMPLETADO
- Bugs encontrados: BUG-5 ✅ BUG-6 ✅ BUG-7 ✅

---

_Cuando termines las tareas, agrega la palabra READ al final de este archivo y luego ejecuta la última tarea: `.prompts/AGENT_LOOP_PROMPT.md`._

READ
