# CORRECCIÓN (Intento 3/3) — Sprint 14: Errores y Edge Cases — La Plataforma es Amigable
**Fecha:** 2026-04-01 23:28:45
**Fase:** FIX
**Ambiente:** LOCAL (Docker Desktop + cloudflared tunnel: https://thousand-erik-cheers-clubs.trycloudflare.com)
**Usuario:** Guest + Buyer
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

- [x] Fix bugs de S14-T01: UF-090 ya corregido en middleware.ts (commit 248ff670) — isKnownProtectedRoute check
- [x] Fix bugs de S14-T02: UF-096 ya corregido en login/page.tsx (commit 248ff670) — session expired banner amber

- [x] Gate Pre-Commit: typecheck ✅ | lint 0 errors ✅ | 576/576 tests ✅ | pnpm build ✅ | CI/CD 23882211986 success ✅
- [x] Agregar `READ` al final de este archivo y luego ejecutar `.prompts/AGENT_LOOP_PROMPT.md` 

## Resultado
- Sprint: 14 — Errores y Edge Cases — La Plataforma es Amigable
- Fase: FIX Intento 3/3
- Ambiente: LOCAL
- Estado: COMPLETADO — 0 bugs nuevos. UF-090+UF-096 corregidos en commits anteriores
- Bugs corregidos: UF-090 middleware pass-through + UF-096 session expired banner + fix(ci) Docker ACL deny delete

---

_Cuando termines las tareas, agrega la palabra READ al final de este archivo y luego ejecuta la última tarea: `.prompts/AGENT_LOOP_PROMPT.md`._
