# CORRECCIÓN (Intento 2/3) — Sprint 1: Visitante Anónimo — Primera Impresión de OKLA
**Fecha:** 2026-03-28 10:22:25
**Fase:** FIX
**Ambiente:** LOCAL/TUNNEL (cloudflared forzado: https://ought-feed-shipping-wright.trycloudflare.com)
**Usuario:** Guest (sin login)
**URL Base:** https://ought-feed-shipping-wright.trycloudflare.com

## Ambiente Local (HTTPS público via cloudflared tunnel)
> Auditoría corriendo contra **https://ought-feed-shipping-wright.trycloudflare.com** (cloudflared tunnel → Caddy → servicios).
> Asegúrate de que la infra esté levantada: `docker compose up -d`
> Frontend: `cd frontend/web-next && pnpm dev`
> Tunnel: `docker compose --profile tunnel up -d cloudflared`
> Caddy redirige: `/api/*` → Gateway, `/*` → Next.js (host:3000)

| Servicio | URL |
|----------|-----|
| Frontend (tunnel) | https://ought-feed-shipping-wright.trycloudflare.com |
| API (tunnel) | https://ought-feed-shipping-wright.trycloudflare.com/api/* |
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

## Ciclo de Monitoreo Obligatorio

1. **Monitorear este archivo** (`.prompts/prompt_1.md`) cada **delay actual** (inicia en 60 segundos)
2. **Ejecutar las tareas pendientes** marcadas con `- [ ]`
3. **Marcar tareas completadas** cambiando `- [ ]` por `- [x]`
4. **Agregar resultados** debajo de cada tarea completada como sub-items
5. **Agregar `READ`** al final del archivo cuando termines de procesar
6. **Verificar ambiente después de cada auditoría limpia — usar el tunnel cloudflared HTTPS:**
   - URL activa: `https://ought-feed-shipping-wright.trycloudflare.com` (ver sección Ambiente al inicio)
   - Si el tunnel está activo (cloudflared), usar esa URL para todas las pruebas
   - Confirmar infra levantada: `docker compose --profile business up -d`
   - Ejecutar smoke test manual con las cuentas de la sección Credenciales
7. **Gestión dinámica del intervalo de monitoreo:**
   - Si **no hay cambios** en el archivo desde la última verificación, **aumentar el delay** multiplicándolo por 2 (backoff exponencial), hasta un máximo de **3600 segundos (1 hora)**.
   - Si **el archivo cambia** (nuevas tareas o modificaciones), **reiniciar el delay a 60 segundos** y ejecutar los pasos 2 a 6 inmediatamente.
8. **Tu última tarea SIEMPRE es esperar el delay actual (según el estado del archivo) y luego volver al paso 1 para reiniciar el ciclo.**

```
Flujo: Detectar cambios → Si hay cambios: delay=60s, procesar tareas, reportar, verificar tunnel, agregar READ
Si no hay cambios: delay = min(delay*2, 3600) → esperar → volver a monitorear
El ciclo nunca termina.
```

## Credenciales
| Rol | Email | Password |
|-----|-------|----------|
| Admin | admin@okla.local | Admin123!@# |
| Buyer | buyer002@okla-test.com | BuyerTest2026! |
| Dealer | nmateo@okla.com.do | Dealer2026!@# |
| Vendedor Particular | gmoreno@okla.com.do | $Gregory1 |

---

## TAREAS

- [ ] Fix bugs de S1-T01: Primera impresión: Homepage completa
- [ ] Fix bugs de S1-T02: Navegación: ¿puedo encontrar lo que busco?

- [ ] Ejecutar Gate Pre-Commit (dotnet build + pnpm lint/typecheck/test/build + dotnet test)

## Resultado
- Sprint: 1 — Visitante Anónimo — Primera Impresión de OKLA
- Fase: FIX
- Ambiente: LOCAL/TUNNEL (cloudflared forzado: https://ought-feed-shipping-wright.trycloudflare.com)
- URL: https://ought-feed-shipping-wright.trycloudflare.com
- Estado: EN PROGRESO
- Bugs encontrados: _(completar)_

---

_Cuando termines, agrega la palabra READ al final de este archivo._
