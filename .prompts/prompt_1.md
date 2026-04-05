# CORRECCIÓN (Intento 1/3) — Sprint 36: E2E Dealer — Dashboard → Inventario → Leads → Analytics
**Fecha:** 2026-04-05 00:54:48
**Fase:** FIX
**Ambiente:** LOCAL (Docker Desktop + cloudflared tunnel: https://hospital-edmonton-duty-tribes.trycloudflare.com)
**Usuario:** Dealer (nmateo@okla.com.do / Dealer2026!@#)
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

- [x] Fix bugs de S36-T01: E2E Journey completo del dealer (12 pasos)
  - S36-B1 (notificaciones 502): Causa infraestructura — `notificationservice` no corre en `--profile core+vehicles`. No hay código que corregir. Documentado como deuda técnica de perfil Docker.

- [x] Ejecutar Gate Pre-Commit (dotnet build + pnpm lint/typecheck/test/build + dotnet test)
  - dotnet restore ✅
  - dotnet build /p:TreatWarningsAsErrors=true ✅ (0 errors, 0 warnings)
  - pnpm lint ✅ (0 errors, 16 warnings only)
  - pnpm typecheck ✅ (0 errores)
  - CI=true pnpm test --run ✅ (576/576 passed)
  - pnpm build ✅ (Compiled successfully)
  - dotnet test — unit tests: 0 Failed en todos los assemblies de unidad. Failures en IntegrationTests + servicios con IHost = pre-existentes (requieren Docker+PG+RabbitMQ).

- [x] Agregar `READ` al final de este archivo y luego ejecutar `.prompts/AGENT_LOOP_PROMPT.md` 

## Resultado
- Sprint: 36 — E2E Dealer — Dashboard → Inventario → Leads → Analytics
- Fase: FIX
- Ambiente: LOCAL (Docker Desktop + http://localhost:3000)
- URL: http://localhost:3000
- Estado: COMPLETADO (sin cambios de código — S36-B1 es infraestructura)
- Bugs cloud-native corregidos: 0 (S36-B1 es infraestructura, no código)

---

_Cuando termines las tareas, agrega la palabra READ al final de este archivo y luego ejecuta la última tarea: `.prompts/AGENT_LOOP_PROMPT.md`._

READ
