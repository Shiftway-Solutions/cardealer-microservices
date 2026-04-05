# CORRECCIÓN (Intento 3/3) — Sprint 34: E2E Buyer — Buscar → Comparar → Contactar → Favoritos

**Fecha:** 2026-04-04 22:03:11
**Fase:** FIX
**Ambiente:** LOCAL (Docker Desktop + cloudflared tunnel: https://hospital-edmonton-duty-tribes.trycloudflare.com)
**Usuario:** Buyer (buyer002@okla-test.com / BuyerTest2026!)
**URL Base:** https://hospital-edmonton-duty-tribes.trycloudflare.com

## Ambiente Local (HTTPS público via cloudflared tunnel)

> Auditoría corriendo contra **https://hospital-edmonton-duty-tribes.trycloudflare.com** (cloudflared tunnel → Caddy → servicios).
> Asegúrate de que la infra esté levantada: `docker compose up -d`
> Frontend: `cd frontend/web-next && pnpm dev`
> Tunnel: `docker compose --profile tunnel up -d cloudflared`
> Caddy redirige: `/api/*` → Gateway, `/*` → Next.js (host:3000)

| Servicio                | URL                                                           |
| ----------------------- | ------------------------------------------------------------- |
| Frontend (tunnel)       | https://hospital-edmonton-duty-tribes.trycloudflare.com       |
| API (tunnel)            | https://hospital-edmonton-duty-tribes.trycloudflare.com/api/* |
| Auth Swagger (local)    | http://localhost:15001/swagger                                |
| Gateway Swagger (local) | http://localhost:18443/swagger                                |

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

| Rol                 | Email                  | Password       |
| ------------------- | ---------------------- | -------------- |
| Admin               | admin@okla.local       | Admin123!@#    |
| Buyer               | buyer002@okla-test.com | BuyerTest2026! |
| Dealer              | nmateo@okla.com.do     | Dealer2026!@#  |
| Vendedor Particular | gmoreno@okla.com.do    | $Gregory1      |

---

## TAREAS

- [x] Fix bugs de S34-T01: E2E Journey completo del buyer (0 bugs pre-existentes confirmados, sprint limpio)

- [x] Ejecutar Gate Pre-Commit (dotnet build + pnpm lint/typecheck/test/build + dotnet test) — ✅ 0 errores
- [x] Agregar `READ` al final de este archivo y luego ejecutar `.prompts/AGENT_LOOP_PROMPT.md`

## Resultado

- Sprint: 34 — E2E Buyer — Buscar → Comparar → Contactar → Favoritos
- Fase: FIX
- Ambiente: LOCAL (Docker Desktop + cloudflared tunnel: https://hospital-edmonton-duty-tribes.trycloudflare.com)
- URL: https://hospital-edmonton-duty-tribes.trycloudflare.com
- Estado: COMPLETO — 0 bugs. Gate ✅ (dotnet build 0 errors/warnings, pnpm lint 0 errors, 576 frontend tests passed, 0 unit test failures backend)
- Bugs encontrados: 0 (confirmado en 3 rondas de auditoría)

---

_Cuando termines las tareas, agrega la palabra READ al final de este archivo y luego ejecuta la última tarea: `.prompts/AGENT_LOOP_PROMPT.md`._
READ
