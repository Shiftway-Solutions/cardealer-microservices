# CORRECCIÓN (Intento 1/3) — Sprint 9: Backend API & Seguridad OWASP
**Fecha:** 2026-03-26 14:59:18
**Fase:** FIX
**Ambiente:** LOCAL (Docker Desktop + cloudflared tunnel: https://twist-first-studios-transcription.trycloudflare.com)
**Usuario:** Todos (verificar por API)
**URL Base:** https://twist-first-studios-transcription.trycloudflare.com

## Ambiente Local (HTTPS público via cloudflared tunnel)
> Auditoría corriendo contra **https://twist-first-studios-transcription.trycloudflare.com** (cloudflared tunnel → Caddy → servicios).
> Asegúrate de que la infra esté levantada: `docker compose up -d`
> Frontend: `cd frontend/web-next && pnpm dev`
> Tunnel: `docker compose --profile tunnel up -d cloudflared`
> Caddy redirige: `/api/*` → Gateway, `/*` → Next.js (host:3000)

| Servicio | URL |
|----------|-----|
| Frontend (tunnel) | https://twist-first-studios-transcription.trycloudflare.com |
| API (tunnel) | https://twist-first-studios-transcription.trycloudflare.com/api/* |
| Auth Swagger (local) | http://localhost:15001/swagger |
| Gateway Swagger (local) | http://localhost:18443/swagger |

## Instrucciones — FASE DE CORRECCIÓN
En la auditoría anterior se encontraron bugs. Tu trabajo ahora es:

1. Lee la sección 'BUGS A CORREGIR' abajo
2. Corrige cada bug en el código fuente
3. Ejecuta el Gate Pre-Commit (8 pasos) para validar
4. Marca cada fix como completado: `- [ ]` → `- [x]`
5. Al terminar, agrega `READ` al final

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

- [ ] Fix bugs de S9-T01: Verificar APIs de autenticación
- [ ] Fix bugs de S9-T02: Verificar seguridad y datos
- [ ] Fix bugs de S9-T03: Verificar pricing API vs frontend

- [ ] Ejecutar Gate Pre-Commit (dotnet build + pnpm lint/typecheck/test/build + dotnet test)

## Resultado
- Sprint: 9 — Backend API & Seguridad OWASP
- Fase: FIX
- Ambiente: LOCAL (Docker Desktop + cloudflared tunnel: https://twist-first-studios-transcription.trycloudflare.com)
- URL: https://twist-first-studios-transcription.trycloudflare.com
- Estado: EN PROGRESO
- Bugs encontrados: _(completar)_

---

_Cuando termines, agrega la palabra READ al final de este archivo._
