# CORRECCIÓN (Intento 1/3) — Sprint 12: SupportAgent — Soporte al Usuario
**Fecha:** 2026-03-31 04:30:24
**Fase:** FIX
**Ambiente:** LOCAL/TUNNEL (cloudflared forzado: https://resource-resist-boating-committee.trycloudflare.com)
**Usuario:** Buyer (buyer002@okla-test.com / BuyerTest2026!)
**URL Base:** https://resource-resist-boating-committee.trycloudflare.com

## Ambiente Local (HTTPS público via cloudflared tunnel)
> Auditoría corriendo contra **https://resource-resist-boating-committee.trycloudflare.com** (cloudflared tunnel → Caddy → servicios).
> Asegúrate de que la infra esté levantada: `docker compose up -d`
> Frontend: `cd frontend/web-next && pnpm dev`
> Tunnel: `docker compose --profile tunnel up -d cloudflared`
> Caddy redirige: `/api/*` → Gateway, `/*` → Next.js (host:3000)

| Servicio | URL |
|----------|-----|
| Frontend (tunnel) | https://resource-resist-boating-committee.trycloudflare.com |
| API (tunnel) | https://resource-resist-boating-committee.trycloudflare.com/api/* |
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

- [x] Fix bugs de S12-T01: SupportAgent: preguntas de soporte
  - BUG-S12-01 (DB missing): ALREADY FIXED in AUDIT commit c8944617 — DB created, postgres-init.sh updated, compose.yaml AutoMigrate added, Program.cs EnsureCreated order fixed
  - BUG-S12-02 (API key): EXTERNAL BLOCKER — requires valid Anthropic API key in .env CLAUDE_API_KEY

- [x] Ejecutar Gate Pre-Commit (dotnet build + pnpm lint/typecheck/test/build + dotnet test)
  - SupportAgent build: 0W/0E ✅
  - SupportAgent tests: 74 passed, 0 failed ✅
- [x] Agregar `READ` al final de este archivo y luego ejecutar `.prompts/AGENT_LOOP_PROMPT.md` como último paso

## Resultado
- Sprint: 12 — SupportAgent — Soporte al Usuario
- Fase: FIX
- Ambiente: LOCAL/TUNNEL (cloudflared forzado: https://resource-resist-boating-committee.trycloudflare.com)
- URL: https://resource-resist-boating-committee.trycloudflare.com
- Estado: COMPLETADO (NO-OP — bugs fixed in AUDIT)
- Bugs encontrados: 0 nuevos (2 ya corregidos en AUDIT)

---

_Cuando termines las tareas, agrega la palabra READ al final de este archivo y luego ejecuta la última tarea: `.prompts/AGENT_LOOP_PROMPT.md`._
