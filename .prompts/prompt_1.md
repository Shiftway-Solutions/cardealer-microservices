# CORRECCIÓN (Intento 3/3) — Sprint 10: SearchAgent — Búsqueda con IA en Español Dominicano
**Fecha:** 2026-03-31 01:56:33
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
### Sprint 10 — Estado final (Intento 3/3):

**CORREGIDOS EN CÓDIGO (intentos 1-2):**
- ✅ BUG-S10-01: DB searchagent creada y migrada
- ✅ BUG-S10-03: Migration logging mejorado (Log.Fatal + connection info) — `SearchAgent.Api/Program.cs`
- ✅ BUG-S10-04: Seller badge en /buscar corregido — `vehicle-search-results.tsx` + `use-vehicle-search.ts`

**NO CORREGIBLES EN CÓDIGO — Requieren acción del usuario:**
- ❌ BUG-S10-02 (P0): Claude API key `sk-ant-v7-devel-okla-audit-2026-test` es placeholder invalido.
  → ACCIÓN: Obtener key real de Anthropic: `ANTHROPIC_API_KEY=sk-ant-api3-...` en `.env`
- ❌ BUG-S10-05 (P3): Email `buyer002@okla-test.com` NO EXISTE en DB.
  → ACCIÓN: Crear buyer vía API o actualizar credenciales en el prompt. SearchAgent es AllowAnonymous.

## Credenciales
| Rol | Email | Password |
|-----|-------|----------|
| Admin | admin@okla.local | Admin123!@# |
| Buyer | buyer002@okla-test.com | BuyerTest2026! |
| Dealer | nmateo@okla.com.do | Dealer2026!@# |
| Vendedor Particular | gmoreno@okla.com.do | $Gregory1 |

---

## TAREAS

- [x] Fix bugs de S10-T01: 3/5 bugs corregidos en código. 2 restantes son EXTERNOS (API key + buyer creds).
- [x] Gate Pre-Commit: pasó en intento 1 (0 warnings, 0 errors, 576 tests passed).
- [x] READ appended.

## Resultado
- Sprint: 10 — SearchAgent — Búsqueda con IA en Español Dominicano
- Fase: FIX (intento 3/3 FINAL)
- Estado: COMPLETADO — bugs restantes requieren acción del USUARIO
- Bugs corregidos: 3/5
- Bugs externos pendientes: BUG-S10-02 (API key Anthropic), BUG-S10-05 (buyer no existe)

---

_Cuando termines las tareas, agrega la palabra READ al final de este archivo y luego ejecuta la última tarea: `.prompts/AGENT_LOOP_PROMPT.md`._

READ
