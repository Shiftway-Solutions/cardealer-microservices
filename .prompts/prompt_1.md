# CORRECCIÓN (Intento 1/3) — Sprint 13: Calidad de Datos — Lo que el Usuario Ve Mal

**Fecha:** 2026-03-31 23:08:26
**Fase:** FIX
**Ambiente:** LOCAL/TUNNEL (cloudflared forzado: https://louisville-companies-ranger-musician.trycloudflare.com)
**Usuario:** Guest
**URL Base:** https://louisville-companies-ranger-musician.trycloudflare.com

## Ambiente Local (HTTPS público via cloudflared tunnel)

> Auditoría corriendo contra **https://louisville-companies-ranger-musician.trycloudflare.com** (cloudflared tunnel → Caddy → servicios).
> Asegúrate de que la infra esté levantada: `docker compose up -d`
> Frontend: `cd frontend/web-next && pnpm dev`
> Tunnel: `docker compose --profile tunnel up -d cloudflared`
> Caddy redirige: `/api/*` → Gateway, `/*` → Next.js (host:3000)

| Servicio                | URL                                                                  |
| ----------------------- | -------------------------------------------------------------------- |
| Frontend (tunnel)       | https://louisville-companies-ranger-musician.trycloudflare.com       |
| API (tunnel)            | https://louisville-companies-ranger-musician.trycloudflare.com/api/* |
| Auth Swagger (local)    | http://localhost:15001/swagger                                       |
| Gateway Swagger (local) | http://localhost:18443/swagger                                       |

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

## BUGS ENCONTRADOS Y CORREGIDOS

### S13-T01: Anomalías en listados /vehiculos

**Bug 1 — CRÍTICO**: Imagen de perro (beagle) como foto principal del Kia Sportage LX 2023

- URL incorrecta: `photo-1543466835-00a7907e9de1` (perro beagle de Unsplash)
- Fix: Reemplazada con `photo-1549317661-bd32c8ce0db2` (SUV validado, HTTP 200)
- Tabla: `vehicle_images`, vehicle_id = Kia Sportage LX 2023

**Bug 2 — ALTO**: BodyStyle = "Sedan" en Kia Sportage LX 2023 (debería ser SUV)

- Fix: UPDATE vehicles SET BodyStyle = 'SUV' WHERE Make = 'Kia' AND Model = 'Sportage'

**Bug 3 — ALTO**: BodyStyle = "Sedan" en Hyundai Tucson Sport 2021 (debería ser SUV)

- Fix: UPDATE vehicles SET BodyStyle = 'SUV' WHERE Make = 'Hyundai' AND Model = 'Tucson'

**Bug 4 — ALTO**: Imagen de paisaje nevado (montañas) como foto principal del Hyundai Tucson 2021

- URL incorrecta: `photo-1519641471654-76ce0107ad1b` (montañas con nieve)
- Fix: Reemplazada con `photo-1560958089-b8a1929cea89` (auto validado, HTTP 200)

**Bug 5 — MEDIO**: Imagen primaria rota (HTTP 404) en Nissan Sentra SV 2020

- URL rota: `photo-1552519507-da3b142a6e3d` (404 via Next.js image proxy)
- Fix: Cambiada IsPrimary=false, promovida `photo-1492144534655-ae79c964c9d7` como nueva primaria

Script de fix: `backend/fix_s13_data_quality.sql`

## TAREAS

- [x] Fix bugs de S13-T01: Buscar anomalías visibles en los listados

- [ ] Ejecutar Gate Pre-Commit (dotnet build + pnpm lint/typecheck/test/build + dotnet test)
- [ ] Agregar `READ` al final de este archivo y luego ejecutar `.prompts/AGENT_LOOP_PROMPT.md`

## Resultado

- Sprint: 13 — Calidad de Datos — Lo que el Usuario Ve Mal
- Fase: FIX
- Ambiente: LOCAL/TUNNEL (cloudflared forzado: https://louisville-companies-ranger-musician.trycloudflare.com)
- URL: https://louisville-companies-ranger-musician.trycloudflare.com
- Estado: EN PROGRESO
- Bugs encontrados: _(completar)_

---

_Cuando termines las tareas, agrega la palabra READ al final de este archivo y luego ejecuta la última tarea: `.prompts/AGENT_LOOP_PROMPT.md`._
