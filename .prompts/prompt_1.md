# CORRECCIÓN (Intento 3/3) — Sprint 32: DealerChatAgent — Profesionalización del Chat de Vehículos
**Fecha:** 2026-04-04 19:37:02
**Fase:** FIX
**Ambiente:** LOCAL (Docker Desktop + cloudflared tunnel: https://hospital-edmonton-duty-tribes.trycloudflare.com)
**Usuario:** Buyer + Dealer
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

- [x] Fix bugs de S32-T01: DealerChatWidget como comprador
- [x] Fix bugs de S32-T02: DealerChatAgent como dealer (datos reales)

- [x] Ejecutar Gate Pre-Commit (dotnet build + pnpm lint/typecheck/test/build + dotnet test)
- [x] Agregar `READ` al final de este archivo y luego ejecutar `.prompts/AGENT_LOOP_PROMPT.md` 

## Resultado
- Sprint: 32 — DealerChatAgent — Profesionalización del Chat de Vehículos
- Fase: FIX
- Ambiente: LOCAL (Docker Desktop + cloudflared tunnel)
- Estado: COMPLETADO
- Bugs corregidos:
  - BUG-2: Backend retornaba Response="" en modo HumanActive → burbuja vacía en chat buyer. Fix: retornar mensaje de confirmación "Tu mensaje fue recibido."
  - BUG-3: Welcome message decía "El vendedor OKLA" para dealers FREE plan. Fix: propagar dealerName desde frontend vía StartSessionRequest.DealerName; backend usa ese nombre cuando config es default/global.
  - BUG-4 (S32-T02): getActiveSessionsCount() requería Rol Admin → dealer siempre veía 0 conversaciones. Fix: nuevo endpoint GET /api/chat/dealer-session-count/{dealerId} + función getDealerSessionCount() en frontend que usa GetMonthSessionCountAsync del repo.
  - ocelot.prod.json: JSON corrupto en ruta /api/chat/session (OPTIONS duplicado) → 2 tests de gateway fallando. Fix: corregido y agregada ruta dealer-session-count en prod.

READ
