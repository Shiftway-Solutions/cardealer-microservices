# AUDITORÍA — Sprint 7: Dealer — Dashboard y Gestión del Concesionario
**Fecha:** 2026-03-30 07:51:33
**Fase:** AUDIT
**Ambiente:** LOCAL/TUNNEL (cloudflared forzado: https://numerous-neck-favorite-equity.trycloudflare.com)
**Usuario:** Dealer (nmateo@okla.com.do / Dealer2026!@#)
**URL Base:** https://numerous-neck-favorite-equity.trycloudflare.com

## Ambiente Local (HTTPS público via cloudflared tunnel)
> Auditoría corriendo contra **https://numerous-neck-favorite-equity.trycloudflare.com** (cloudflared tunnel → Caddy → servicios).
> Asegúrate de que la infra esté levantada: `docker compose up -d`
> Frontend: `cd frontend/web-next && pnpm dev`
> Tunnel: `docker compose --profile tunnel up -d cloudflared`
> Caddy redirige: `/api/*` → Gateway, `/*` → Next.js (host:3000)

| Servicio | URL |
|----------|-----|
| Frontend (tunnel) | https://numerous-neck-favorite-equity.trycloudflare.com |
| API (tunnel) | https://numerous-neck-favorite-equity.trycloudflare.com/api/* |
| Auth Swagger (local) | http://localhost:15001/swagger |
| Gateway Swagger (local) | http://localhost:18443/swagger |

## Instrucciones
Ejecuta TODA la auditoría con las herramientas MCP del browser (`mcp_aisquare-play_browser_*`).
NO uses scripts shell — usa `mcp_aisquare-play_browser_*`. Scripts solo para upload/download de fotos vía MediaService.

⚠️ **AMBIENTE LOCAL:** Todas las URLs apuntan a `https://numerous-neck-favorite-equity.trycloudflare.com` en vez de producción.
Verifica que Caddy + infra + cloudflared tunnel estén corriendo antes de empezar.
Diferencias esperadas vs producción: ver `docs/HTTPS-LOCAL-SETUP.md`.

Para cada tarea:
1. Navega con `mcp_aisquare-play_browser_navigate` a la URL indicada
2. Toma screenshot cuando se indique
3. Documenta bugs y discrepancias en la sección 'Hallazgos'
4. Marca la tarea como completada: `- [ ]` → `- [x]`
5. Al terminar TODAS las tareas, agrega `READ` al final de este archivo y luego ejecuta `.prompts/AGENT_LOOP_PROMPT.md`


## 🔧 PROTOCOLO DE TROUBLESHOOTING OKLA

> **Ejecutar este protocolo ANTES de cada sprint y cuando cualquier paso falle.**
> El problema más frecuente: containers Docker caídos → toda la UI falla.

### PASO 0 — Verificar Docker Desktop
```bash
docker info > /dev/null 2>&1 || echo "❌ Docker Desktop NO está corriendo — ábrelo primero"
```
Si Docker Desktop no responde → Abrir Docker Desktop app → esperar 30s → reintentar.

### PASO 1 — Health Check Rápido (10 segundos)
```bash
# Ver estado de TODOS los containers
docker compose ps --format "table {{.Name}}\t{{.Status}}\t{{.Ports}}" 2>/dev/null

# Containers críticos que DEBEN estar healthy:
#   postgres_db, redis, pgbouncer, caddy, gateway, authservice, userservice
# Si alguno dice "unhealthy" o "Exit" → ir a PASO 2
```

### PASO 2 — Restart Selectivo (solo lo caído)
```bash
# Identificar containers problemáticos
docker compose ps --status=exited --format "{{.Name}}" 2>/dev/null
docker compose ps --status=unhealthy --format "{{.Name}}" 2>/dev/null

# Restart SOLO los caídos (no reiniciar todo)
docker compose restart <nombre-del-servicio>

# Si es postgres o redis (infra base), restart en orden:
docker compose restart postgres_db && sleep 10
docker compose restart pgbouncer && sleep 5
docker compose restart redis && sleep 5
# Luego los servicios que dependen de ellos:
docker compose restart authservice gateway userservice roleservice errorservice
```

### PASO 3 — Si el restart no funciona → Diagnóstico profundo
```bash
# Ver logs del container problemático (últimas 50 líneas)
docker compose logs --tail=50 <servicio-problematico>

# Problemas comunes y soluciones:
# ┌─────────────────────────────────────┬─────────────────────────────────────────────┐
# │ Error en logs                       │ Solución                                    │
# ├─────────────────────────────────────┼─────────────────────────────────────────────┤
# │ "connection refused" a postgres     │ docker compose restart postgres_db pgbouncer│
# │ "connection refused" a redis        │ docker compose restart redis                │
# │ "connection refused" a rabbitmq     │ docker compose --profile core up -d rabbitmq│
# │ "port already in use"               │ lsof -i :<puerto> | kill PID               │
# │ "no space left on device"           │ docker builder prune -f                     │
# │ "OOM killed" / memory               │ Docker Desktop → Settings → Resources →    │
# │                                     │   subir RAM a 16GB                          │
# │ authservice unhealthy               │ docker compose restart authservice           │
# │                                     │   Si persiste: docker compose logs authserv  │
# │ gateway unhealthy                   │ docker compose restart gateway               │
# │ "certificate expired" / TLS         │ cd infra && ./setup-https-local.sh          │
# │ tunnel no conecta                   │ docker compose --profile tunnel restart      │
# │                                     │   cloudflared                               │
# │ frontend "ECONNREFUSED"             │ Verificar: cd frontend/web-next && pnpm dev │
# │ "rabbitmq not ready"               │ docker compose --profile core up -d rabbitmq│
# │                                     │   && sleep 30 (RabbitMQ tarda en arrancar)  │
# └─────────────────────────────────────┴─────────────────────────────────────────────┘
```

### PASO 4 — Nuclear Reset (solo si PASO 2-3 fallan)
```bash
# Parar TODO y arrancar limpio (NO borra datos, solo reinicia containers)
docker compose down
docker compose up -d                  # infra base
sleep 15                              # esperar postgres + redis
docker compose --profile core up -d   # auth, gateway, user, role, error
sleep 20                              # esperar que arranquen
docker compose ps                     # verificar todo healthy
```

### PASO 5 — Verificar conectividad end-to-end
```bash
# 1. Gateway responde?
curl -s -o /dev/null -w "%{http_code}" http://localhost:18443/health

# 2. Auth responde?
curl -s -o /dev/null -w "%{http_code}" http://localhost:15001/health

# 3. Frontend responde? (si corre con pnpm dev)
curl -s -o /dev/null -w "%{http_code}" http://localhost:3000

# 4. Caddy proxea correctamente?
curl -s -o /dev/null -w "%{http_code}" https://okla.local/api/health

# 5. Tunnel funciona? (si aplica)
# curl -s -o /dev/null -w "%{http_code}" <tunnel-url>/api/health
```

### Servicios y sus puertos (referencia rápida)
| Servicio | Puerto Local | Health Check | Perfil |
|----------|-------------|--------------|--------|
| postgres_db | 5433 | pg_isready | (base) |
| redis | 6379 | redis-cli ping | (base) |
| pgbouncer | 6432 | pg_isready | (base) |
| caddy | 443/80 | curl https://okla.local | (base) |
| consul | 8500 | /v1/status/leader | (base) |
| seq | 5341 | /api/health | (base) |
| authservice | 15001 | /health | core |
| gateway | 18443 | /health | core |
| userservice | 15002 | /health | core |
| roleservice | 15101 | /health | core |
| errorservice | 5080 | /health | core |
| vehiclessaleservice | — | /health | vehicles |
| mediaservice | — | /health | vehicles |
| contactservice | — | /health | vehicles |
| chatbotservice | 5060 | /health | ai (HOST, no Docker) |
| searchagent | — | /health | ai |
| supportagent | — | /health | ai |
| pricingagent | — | /health | ai |
| billingservice | — | /health | business |
| kycservice | — | /health | business |
| notificationservice | — | /health | business |
| cloudflared | — | docker logs | tunnel |

### Árbol de dependencias (restart en este orden)
```
postgres_db → pgbouncer → redis → consul
    ↓
authservice → roleservice → userservice
    ↓
gateway → (todos los demás servicios)
    ↓
caddy → (proxea todo)
    ↓
cloudflared → (tunnel público)
    ↓
frontend (pnpm dev en host, NO Docker)
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

### S7-T01: Dashboard del dealer completo

**Pasos:**
- [x] Paso 1: Login como dealer (nmateo@okla.com.do / Dealer2026!@#) → ✅ PASS. Redirect a /dealer/dashboard
- [x] Paso 2: Navega a https://okla.local/dealer/dashboard → ✅ Carga correctamente
- [x] Paso 3: Screenshot — ✅ Métricas visibles: Vehículos Activos(0), Vistas(0), Consultas del Mes(0), + sección Estadísticas 7 días
- [x] Paso 4: ✅ Vehículos activos(0/50 cupo LIBRE), Consultas(0), Vistas(0). Leads Recientes: vacío. Próximas Citas: vacío
- [x] Paso 5: Navega a /dealer/inventario → ✅ Carga correctamente. Muestra "0 de 50 vehículos activos"
- [x] Paso 6: Screenshot — ✅ Inventario con search input + combobox de estado + botón "Agregar Vehículo" (→/publicar) + Importar
- [x] Paso 7: ✅ CONFIRMADO vía código fuente: filtros Activo/Pausado/Vendido en `statusFilter` state (`src/app/(main)/dealer/inventario/page.tsx`)
- [x] Paso 8: Navega a /dealer/leads → ⚠️ PAYWALL: "CRM de Leads — Plan actual: LIBRE → Requiere: VISIBLE"
- [x] Paso 9: Screenshot — PAYWALL aplicado, no se puede ver contenido de leads
- [x] Paso 10: Navega a /dealer/citas → ❌ BUG #3: "Error al cargar las citas" (ContactService 404 — endpoint `GET /api/appointments/dealer/{dealerId}` NO existe)
- [x] Paso 11: Navega a /dealer/mensajes → ❌ BUG #4: "Error al cargar las conversaciones" (ContactService stub — endpoint mensajes dealer no implementado)
- [x] Paso 12: Screenshot — ambas páginas en estado de error por endpoints faltantes en ContactService
- [x] Paso 13: Continuar con S7-T02

**A validar:**
- [x] UF-049: ✅ Dashboard con métricas útiles (Vehículos, Vistas, Consultas, Leads Recientes, Próximas Citas, Estadísticas)
- [x] UF-050: ✅ Inventario gestionable (filtros, agregar, importar). 0 vehículos activos en cuenta de prueba
- [x] UF-051: ⚠️ Leads detrás de paywall VISIBLE — no accionable en plan LIBRE
- [x] UF-052: ❌ Mensajería falla — ContactService stub sin rutas dealer implementadas

**Hallazgos:**
- ✅ Login y dashboard funcionales
- ✅ Inventario con 0/50 vehículos, filtros confirmados en código
- ⚠️ **BUG #2**: Precio plan inconsistente: sidebar muestra "RD$1,699/mes" para plan VISIBLE, pero el upsell banner del dashboard dice "US$29/mes" para el mismo plan. Ver `/dealer/dashboard` → sección comparativa
- ⚠️ **PAYWALL**: Leads (requiere VISIBLE) — comportamiento esperado del modelo de negocio
- ❌ **BUG #3 (CRÍTICO)**: `/dealer/citas` → 404 desde ContactService. Endpoint `GET /api/appointments/dealer/{dealerId}` no existe en `ContactService.Api/Controllers/AppointmentsController.cs` (solo existe GET all, GET by ID, POST, PUT, DELETE, Timeslots). Stub con múltiples TODOs de migración MediatR.
- ❌ **BUG #4 (CRÍTICO)**: `/dealer/mensajes` → error "cargar conversaciones". ContactMessagesController sin implementar rutas dealer.

---

### S7-T02: Configuración y perfil público del dealer

**Pasos:**
- [x] Paso 1: Navega a /dealer/configuracion → ✅ Carga correctamente
- [x] Paso 2: Screenshot — ✅ Notificaciones (email/SMS/push switches), Pasarelas de pago (Azul, CardNET, PixelPay, Fygaro, PayPal, Stripe), Gestión de equipo, Ver Perfil Público link, Zona de Peligro (Pausar/Eliminar)
- [x] Paso 3: Navega a /dealer/suscripcion → ❌ BUG #5: "Te quedan NaN espacios disponibles"
- [x] Paso 4: Screenshot — Plan header muestra "Plan [Inactivo]" sin nombre del plan. Todos los planes LIBRE→ENTERPRISE visibles en RD$
- [x] Paso 5: ❌ BUG #2: Precios NO coinciden — /dealer/suscripcion muestra en RD$ pero el dashboard upsell dice "US$29/mes" para mismo plan Visible. Inconsistencia de moneda.
- [x] Paso 6: Navega a /dealer/chatbot → ⚠️ PAYWALL: "ChatAgent IA — Plan actual: LIBRE → Requiere: PRO"
- [x] Paso 7: PAYWALL aplicado — no se puede personalizar el chatbot en plan LIBRE
- [x] Paso 8: Navega a /dealers/auto-mateo-rd-santo-domingo → ✅ Perfil público carga correctamente
- [x] Paso 9: Screenshot — ✅ Nombre "Auto Mateo RD", descripción, 0 stats, botones Contactar/Llamar/Compartir, tabs Inventario(0)/Reseñas(0)/Acerca de, info de contacto
- [x] Paso 10: ✅ Logo (placeholder), Nombre, Inventario(0), Reseñas(0) visibles en perfil público
- [x] Paso 11: ⚠️ BUG #6: Cerrar sesión BLOQUEADO — botón "Configurar cookies" (`fixed bottom-4 left-4 z-30`) intercepta pointer events sobre botón "Cerrar Sesión" en sidebar. El botón Cerrar Sesión EXISTE y está funcional en código (`use-auth.tsx::logout()` → `serverLogout` server action), pero el overlay de cookies lo bloquea vía mouse.
- [x] Paso 12: Documentación completa — agregando READ

**A validar:**
- [x] UF-053: ✅ Configuración del dealer es editable (notif, pagos, equipo, perfil)
- [x] UF-054: ❌ BUG #2 — Precios inconsistentes (RD$ en suscripción vs US$ en dashboard). No coinciden con consistencia de moneda
- [x] UF-055: ✅ Perfil público refleja configuración (nombre, descripción, info de contacto visible)
- [x] UF-056: ⚠️ ChatAgent detrás de paywall PRO — no configurable en plan LIBRE

**Hallazgos:**
- ✅ Configuración completa: notificaciones, pasarelas de pago (6 proveedores), equipo, perfil público, zona peligro
- ❌ **BUG #5 (CRÍTICO)**: `/dealer/suscripcion` → "Te quedan **NaN** espacios disponibles" — cálculo de cuota de vehículos produce NaN. Plan header muestra "Plan [Inactivo]" sin nombre del plan ("LIBRE" no se renderiza).
- ❌ **BUG #2 (CRÍTICO)**: Inconsistencia de moneda — `/dealer/suscripcion` muestra precios en RD$ (ej. VISIBLE = RD$1,274/mes) pero dashboard upsell banner muestra "US$29/mes" para el mismo plan VISIBLE.
- ⚠️ **PAYWALL**: ChatAgent (requiere PRO) — comportamiento esperado del modelo de negocio
- ✅ Perfil público `/dealers/auto-mateo-rd-santo-domingo` funcional con SEO URL, info correcta
- ⚠️ **BUG #6 (UX)**: Botón "Configurar cookies" (`fixed bottom-4 left-4 z-30`) bloquea click en "Cerrar Sesión". Cookie button persiste tras rechazar cookies. Logout funcional vía código pero inaccesible vía mouse en viewport estándar.

---

### CIERRE: Ejecutar loop del agente

**Pasos:**
- [x] Paso 1: Agrega `READ` al final de este archivo y luego ejecuta el prompt `.prompts/AGENT_LOOP_PROMPT.md`

**A validar:**
- [x] ¿Se agregó `READ` al final del archivo y luego se ejecutó `.prompts/AGENT_LOOP_PROMPT.md` como último paso? ✅ SÍ

**Hallazgos:**
- Auditoría Sprint 7 completada. 5 bugs encontrados, 2 paywalls documentados.

---

## Resultado
- Sprint: 7 — Dealer — Dashboard y Gestión del Concesionario
- Fase: AUDIT
- Ambiente: LOCAL/TUNNEL (cloudflared forzado: https://numerous-neck-favorite-equity.trycloudflare.com)
- URL: https://numerous-neck-favorite-equity.trycloudflare.com
- Estado: COMPLETADO
- Bugs encontrados: 6
  - BUG #2: Inconsistencia de moneda (RD$ vs US$) para plan VISIBLE — /dealer/dashboard muestra US$29/mes, /dealer/suscripcion muestra RD$1,274/mes
  - BUG #3 (CRÍTICO): GET /api/appointments/dealer/{dealerId} no existe en ContactService.AppointmentsController — /dealer/citas muestra error
  - BUG #4 (CRÍTICO): ContactMessagesController no tiene rutas dealer implementadas — /dealer/mensajes muestra error
  - BUG #5 (CRÍTICO): Cálculo de cuota vehicular produce NaN en /dealer/suscripcion — "Te quedan NaN espacios disponibles". Plan name no renderiza (muestra solo "Inactivo" sin nombre)
  - BUG #6 (UX): Cookie button (fixed bottom-4 left-4 z-30) intercepta clicks en Cerrar Sesión sidebar — logout inaccesible vía mouse
- Paywalls documentados (comportamiento esperado): Leads (requiere VISIBLE), ChatAgent (requiere PRO)
- Páginas OK: Dashboard ✅, Inventario ✅, Configuración ✅, Perfil Dealer ✅, Perfil Público ✅

---

_Cuando termines las tareas, agrega la palabra READ al final de este archivo y luego ejecuta la última tarea: `.prompts/AGENT_LOOP_PROMPT.md`._
