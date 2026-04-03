# CORRECCIÓN (Intento 1/3) — Sprint 21: Favoritos y Alertas — Sistema de Guardado
**Fecha:** 2026-04-03 03:57:56
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

- [x] Fix bugs de S21-T01: Guardar favorito y configurar alertas — ✅ SIN BUGS DE CÓDIGO. La auditoría confirmó que el frontend implementa correctamente manejo de errores. Los 502s son por vehiclessaleservice/notificationservice no disponibles en --profile core. No hay correcciones de código necesarias.

- [x] Ejecutar Gate Pre-Commit — ✅ PASADO: pnpm typecheck (0 errores), pnpm lint (0 errors/15 warnings pre-existentes), 576 tests passed, pnpm build success, dotnet build 0 errors.
- [x] Agregar `READ` al final de este archivo — ✅ COMPLETADO

## Resultado
- Sprint: 21 — Favoritos y Alertas — Sistema de Guardado
- Fase: FIX
- Ambiente: LOCAL (Docker Desktop + cloudflared tunnel: https://weighted-somewhere-serum-copied.trycloudflare.com)
- URL: https://weighted-somewhere-serum-copied.trycloudflare.com
- Estado: COMPLETADO ✅ (sin bugs de código que corregir)
- Bugs encontrados: 0 (frontend correcto — errores 502 son dependencia de infra no levantada)

---

_Cuando termines las tareas, agrega la palabra READ al final de este archivo y luego ejecuta la última tarea: `.prompts/AGENT_LOOP_PROMPT.md`._
READ
