# CORRECCIÓN (Intento 1/3) — Sprint 11: DealerChatWidget — Chat con IA en Detalle de Vehículo
**Fecha:** 2026-03-31 02:24:09
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

- [x] Fix bugs de S11-T01: Conversación realista con DealerChatWidget
  - BUG-S11-01 (P0): Fixed gateway ocelot.dev.json — changed 13 chat routes from host.docker.internal:5060 to chatbotservice:80. Gateway restarted, /api/chat/health returns 200.
  - BUG-S11-02 (P2): Created buyer002@okla-test.com in authservice via register API + confirmed email in DB. Login verified working.
  - BUG-S11-03 (P3): Fixed login prompt UX — now renders immediately below whichever button triggered it (Chat en vivo vs Ana IA) instead of always at top.
  - BUG-S11-04 (P3): Fixed stale "Recomendados" sidebar — demo ad data no longer shown unless NEXT_PUBLIC_USE_DEMO_ADS=true. Empty sidebar when no real sponsored data.

- [x] Ejecutar Gate Pre-Commit (dotnet build + pnpm lint/typecheck/test/build + dotnet test) — All passed. 0 warnings, 0 errors backend. 576/576 tests frontend. Pre-existing test failures unchanged.
- [x] Agregar `READ` al final de este archivo y luego ejecutar `.prompts/AGENT_LOOP_PROMPT.md` como último paso

## Resultado
- Sprint: 11 — DealerChatWidget — Chat con IA en Detalle de Vehículo
- Fase: FIX
- Ambiente: LOCAL/TUNNEL (cloudflared forzado: https://resource-resist-boating-committee.trycloudflare.com)
- URL: https://resource-resist-boating-committee.trycloudflare.com
- Estado: COMPLETADO
- Bugs corregidos: 4/4 (BUG-S11-01 P0, BUG-S11-02 P2, BUG-S11-03 P3, BUG-S11-04 P3)

---

_Cuando termines las tareas, agrega la palabra READ al final de este archivo y luego ejecuta la última tarea: `.prompts/AGENT_LOOP_PROMPT.md`._

READ
