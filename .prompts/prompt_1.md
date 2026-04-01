# CORRECCIÓN (Intento 2/3) — Sprint 13: Calidad de Datos — Lo que el Usuario Ve Mal

**Fecha:** 2026-04-01 07:44:21
**Fase:** FIX
**Ambiente:** LOCAL/TUNNEL (cloudflared forzado: https://customized-fantastic-mia-justin.trycloudflare.com)
**Usuario:** Guest
**URL Base:** https://customized-fantastic-mia-justin.trycloudflare.com

## Ambiente Local (HTTPS público via cloudflared tunnel)

> Auditoría corriendo contra **https://customized-fantastic-mia-justin.trycloudflare.com** (cloudflared tunnel → Caddy → servicios).
> Asegúrate de que la infra esté levantada: `docker compose up -d`
> Frontend: `cd frontend/web-next && pnpm dev`
> Tunnel: `docker compose --profile tunnel up -d cloudflared`
> Caddy redirige: `/api/*` → Gateway, `/*` → Next.js (host:3000)

| Servicio                | URL                                                             |
| ----------------------- | --------------------------------------------------------------- |
| Frontend (tunnel)       | https://customized-fantastic-mia-justin.trycloudflare.com       |
| API (tunnel)            | https://customized-fantastic-mia-justin.trycloudflare.com/api/* |
| Auth Swagger (local)    | http://localhost:15001/swagger                                  |
| Gateway Swagger (local) | http://localhost:18443/swagger                                  |

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

- [x] Fix bugs de S13-T01: Buscar anomalías visibles en los listados

- [x] Ejecutar Gate Pre-Commit (dotnet build + pnpm lint/typecheck/test/build + dotnet test)
- [x] Agregar `READ` al final de este archivo y luego ejecutar `.prompts/AGENT_LOOP_PROMPT.md`

## Resultado

- Sprint: 13 — Calidad de Datos — Lo que el Usuario Ve Mal
- Fase: FIX
- Ambiente: LOCAL (tunnel: https://apparently-bride-achieved-pdt.trycloudflare.com)
- URL: https://apparently-bride-achieved-pdt.trycloudflare.com
- Estado: COMPLETADO
- Bugs encontrados:
  - Bug 6: MileageUnit=Miles (todos los vehículos seed) → corregido a Kilometers en DB (UPDATE 5 filas)
  - Bugs 1-5: ya corregidos en intento 1 (imágenes + BodyStyle SUV)

---

_Cuando termines las tareas, agrega la palabra READ al final de este archivo y luego ejecuta la última tarea: `.prompts/AGENT_LOOP_PROMPT.md`._
READ
