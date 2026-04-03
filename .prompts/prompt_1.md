# CORRECCIÓN (Intento 3/3) — Sprint 20: Reviews — Reputación de Dealers
**Fecha:** 2026-04-03 03:18:23
**Fase:** FIX
**Ambiente:** LOCAL (Docker Desktop + cloudflared tunnel: https://weighted-somewhere-serum-copied.trycloudflare.com)
**Usuario:** Buyer (buyer002@okla-test.com / BuyerTest2026!)
**URL Base:** https://weighted-somewhere-serum-copied.trycloudflare.com

## Ambiente Local (HTTPS público via cloudflared tunnel)
> Auditoría corriendo contra **https://weighted-somewhere-serum-copied.trycloudflare.com** (cloudflared tunnel → Caddy → servicios).
> Asegúrate de que la infra esté levantada: `docker compose up -d`
> Frontend: `cd frontend/web-next && pnpm dev`
> Tunnel: `docker compose --profile tunnel up -d cloudflared`
> Caddy redirige: `/api/*` → Gateway, `/*` → Next.js (host:3000)

| Servicio | URL |
|----------|-----|
| Frontend (tunnel) | https://weighted-somewhere-serum-copied.trycloudflare.com |
| API (tunnel) | https://weighted-somewhere-serum-copied.trycloudflare.com/api/* |
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

- [x] Fix bugs de S20-T01: Leer y escribir reseñas de dealers
  - Fix ya aplicado en Intento 1 (`89d14c1e`) y verificado en REAUDIT 1/2 y 2/2
  - BUG-S20-1 CORREGIDO: `dealer-profile-client.tsx` onError handler cover/logo ✅
  - CI/CD `23937911633` → success ✅

- [x] Ejecutar Gate Pre-Commit → Corrido en Intento 1, sin cambios nuevos
- [x] Agregar `READ` al final de este archivo y luego ejecutar `.prompts/AGENT_LOOP_PROMPT.md` 

## Resultado
- Sprint: 20 — Reviews — Reputación de Dealers
- Fase: FIX
- Ambiente: LOCAL (Docker Desktop + cloudflared tunnel: https://weighted-somewhere-serum-copied.trycloudflare.com)
- URL: https://weighted-somewhere-serum-copied.trycloudflare.com
- Estado: COMPLETADO ✅ (Fix verificado en REAUDIT 2/2)
- Bugs encontrados: 1 corregido — sin pendientes. Sprint 20 cerrado definitivamente.

---

_Cuando termines las tareas, agrega la palabra READ al final de este archivo y luego ejecuta la última tarea: `.prompts/AGENT_LOOP_PROMPT.md`._
READ
