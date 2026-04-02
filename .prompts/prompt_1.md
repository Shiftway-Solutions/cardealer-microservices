# CORRECCIÓN (Intento 3/3) — Sprint 15: Onboarding — Primera Experiencia de Usuario Nuevo
**Fecha:** 2026-04-02 01:28:50
**Fase:** FIX
**Ambiente:** LOCAL (Docker Desktop + cloudflared tunnel: https://thousand-erik-cheers-clubs.trycloudflare.com)
**Usuario:** Guest → Seller
**URL Base:** https://thousand-erik-cheers-clubs.trycloudflare.com

## Ambiente Local (HTTPS público via cloudflared tunnel)
> Auditoría corriendo contra **https://thousand-erik-cheers-clubs.trycloudflare.com** (cloudflared tunnel → Caddy → servicios).
> Asegúrate de que la infra esté levantada: `docker compose up -d`
> Frontend: `cd frontend/web-next && pnpm dev`
> Tunnel: `docker compose --profile tunnel up -d cloudflared`
> Caddy redirige: `/api/*` → Gateway, `/*` → Next.js (host:3000)

| Servicio | URL |
|----------|-----|
| Frontend (tunnel) | https://thousand-erik-cheers-clubs.trycloudflare.com |
| API (tunnel) | https://thousand-erik-cheers-clubs.trycloudflare.com/api/* |
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

- [x] Fix bugs de S15-T01: Registro y onboarding de nuevo usuario — NO hay bugs nuevos. Todos corregidos en FIX 1/3 y FIX 2/3 (commits 54ec34a4, b79a126e)

- [x] Ejecutar Gate Pre-Commit (dotnet build + pnpm lint/typecheck/test/build + dotnet test) — typecheck ✅ 0 errors | lint ✅ 0 errors | tests ✅ 576/576
- [x] Agregar `READ` al final de este archivo y luego ejecutar `.prompts/AGENT_LOOP_PROMPT.md` 

## Resultado
- Sprint: 15 — Onboarding — Primera Experiencia de Usuario Nuevo
- Fase: FIX
- Ambiente: LOCAL (Docker Desktop + https://okla.local)
- URL: https://okla.local
- Estado: COMPLETADO ✅ — no hay bugs pendientes
- Bugs encontrados: 0 (todos resueltos en iteraciones 1/3 y 2/3)

---

_Cuando termines las tareas, agrega la palabra READ al final de este archivo y luego ejecuta la última tarea: `.prompts/AGENT_LOOP_PROMPT.md`._

READ
