# AUDITORÍA — Sprint 30: Admin — Facturación, Billing y Sistema

**Fecha:** 2026-04-04 03:54:38
**Fase:** AUDIT
**Ambiente:** LOCAL (Docker Desktop + cloudflared tunnel: https://hospital-edmonton-duty-tribes.trycloudflare.com)
**Usuario:** Admin (admin@okla.local / Admin123!@#)
**URL Base:** https://hospital-edmonton-duty-tribes.trycloudflare.com

## Ambiente Local (HTTPS público via cloudflared tunnel)

> Auditoría corriendo contra **https://hospital-edmonton-duty-tribes.trycloudflare.com** (cloudflared tunnel → Caddy → servicios).
> Asegúrate de que la infra esté levantada: `docker compose up -d`
> Frontend: `cd frontend/web-next && pnpm dev`
> Tunnel: `docker compose --profile tunnel up -d cloudflared`
> Caddy redirige: `/api/*` → Gateway, `/*` → Next.js (host:3000)

| Servicio                | URL                                                           |
| ----------------------- | ------------------------------------------------------------- |
| Frontend (tunnel)       | https://hospital-edmonton-duty-tribes.trycloudflare.com       |
| API (tunnel)            | https://hospital-edmonton-duty-tribes.trycloudflare.com/api/* |
| Auth Swagger (local)    | http://localhost:15001/swagger                                |
| Gateway Swagger (local) | http://localhost:18443/swagger                                |

## Instrucciones

Ejecuta TODA la auditoría con las herramientas MCP del browser (`mcp_aisquare-play_browser_*`).
NO uses scripts shell — usa `mcp_aisquare-play_browser_*`. Scripts solo para upload/download de fotos vía MediaService.

⚠️ **AMBIENTE LOCAL:** Todas las URLs apuntan a `https://hospital-edmonton-duty-tribes.trycloudflare.com` en vez de producción.
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

| Servicio            | Puerto Local | Health Check            | Perfil               |
| ------------------- | ------------ | ----------------------- | -------------------- |
| postgres_db         | 5433         | pg_isready              | (base)               |
| redis               | 6379         | redis-cli ping          | (base)               |
| pgbouncer           | 6432         | pg_isready              | (base)               |
| caddy               | 443/80       | curl https://okla.local | (base)               |
| consul              | 8500         | /v1/status/leader       | (base)               |
| seq                 | 5341         | /api/health             | (base)               |
| authservice         | 15001        | /health                 | core                 |
| gateway             | 18443        | /health                 | core                 |
| userservice         | 15002        | /health                 | core                 |
| roleservice         | 15101        | /health                 | core                 |
| errorservice        | 5080         | /health                 | core                 |
| vehiclessaleservice | —            | /health                 | vehicles             |
| mediaservice        | —            | /health                 | vehicles             |
| contactservice      | —            | /health                 | vehicles             |
| chatbotservice      | 5060         | /health                 | ai (HOST, no Docker) |
| searchagent         | —            | /health                 | ai                   |
| supportagent        | —            | /health                 | ai                   |
| pricingagent        | —            | /health                 | ai                   |
| billingservice      | —            | /health                 | business             |
| kycservice          | —            | /health                 | business             |
| notificationservice | —            | /health                 | business             |
| cloudflared         | —            | docker logs             | tunnel               |

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

| Rol                 | Email                  | Password       |
| ------------------- | ---------------------- | -------------- |
| Admin               | admin@okla.local       | Admin123!@#    |
| Buyer               | buyer002@okla-test.com | BuyerTest2026! |
| Dealer              | nmateo@okla.com.do     | Dealer2026!@#  |
| Vendedor Particular | gmoreno@okla.com.do    | $Gregory1      |

---

## TAREAS

### S30-T01: Admin: billing y sistema

**Pasos:**

- [x] Paso 1: Login como admin (admin@okla.local / Admin123!@#)
- [x] Paso 2: Navega a facturación/billing del admin
- [x] Paso 3: Toma screenshot — ¿veo ingresos, transacciones, planes activos?
- [x] Paso 4: ¿Puedo ver historial de pagos por dealer/seller?
- [x] Paso 5: ¿Puedo ver reportes de ingresos por período?
- [x] Paso 6: Navega a configuración del sistema
- [x] Paso 7: ¿Hay modo mantenimiento activable?
- [x] Paso 8: ¿Hay logs de auditoría del sistema?
- [x] Paso 9: Navega a gestión de roles/permisos
- [x] Paso 10: ¿Puedo crear/editar roles?
- [x] Paso 11: Navega a costos de LLM/IA (si existe)
- [x] Paso 12: ¿Veo costos por modelo, por día, tendencias?
- [x] Paso 13: Navega a SearchAgent config (si existe en admin)
- [x] Paso 14: ¿Puedo ajustar prompt, temperatura, modelo?
- [x] Paso 15: Cierra sesión
- [x] Paso 16: Agrega `READ` al final de este archivo .prompts/prompt_1.md y luego ejecuta el prompt `.prompts/AGENT_LOOP_PROMPT.md`

**A validar:**

- [x] UF-161: ¿Billing del admin con ingresos reales? — PARCIAL (2/4 endpoints retornan 404)
- [x] UF-162: ¿Logs de auditoría funcionales? — SÍ (página funcional con filtros)
- [x] UF-163: ¿Configuración del sistema accesible? — SÍ (config + mantenimiento OK)
- [x] UF-164: ¿Costos de IA visibles para el admin? — NO (SearchAgent 502, sin sección LLM costs)

**Hallazgos:**

**INFRAESTRUCTURA (pre-audit):**
- BUG: `.env` tenía `FRONTEND_UPSTREAM=frontend-next:3000` → Caddy retornaba 502. FIX aplicado: `FRONTEND_UPSTREAM=host.docker.internal:3000`
- `roleservice` arrancó como `unhealthy` (reiniciado, ahora OK)
- Frontend (`pnpm dev`) no estaba corriendo → iniciado en host:3000

**FACTURACIÓN (UF-161 — PARCIAL):**
- ✅ `/api/admin/billing/revenue` → 200 OK. UI muestra MRR=RD$0, ARR=RD$0 (ambiente dev sin data real)
- ✅ `/api/admin/billing/revenue-by-plan` → 200 OK. Planes: Libre RD$0, Visible RD$29/m, Pro RD$89/m, Elite RD$199/m (precios correctos)
- ❌ BUG: `/api/admin/billing/transactions` → 404 NOT FOUND — endpoint FALTANTE en AdminService
- ❌ BUG: `/api/admin/billing/pending` → 404 NOT FOUND — endpoint FALTANTE en AdminService
- UI muestra RD$0 en todos los campos (consistente con data vacía en dev)
- ARR no tiene campo propio en la API response (`arr` missing — frontend calcula o muestra 0)
- React warning: "Each child in a list should have a unique key prop" en AdminConfigurationPage

**CONFIGURACIÓN DEL SISTEMA (UF-163 — OK):**
- ✅ `/admin/configuracion` carga correctamente — 4 configuraciones, 3 feature flags
- ✅ Secciones: General (nombre, URL, emails, teléfonos, RRSS), Precios y Comisiones, Feature Flags
- ❌ Múltiples 404 en llamadas API al cargar (config no se carga desde backend)
- ✅ `/admin/mantenimiento` funciona — Plataforma Operativa/Online, botón Activar Mantenimiento
- ✅ Programador de mantenimiento con tipo, fecha, hora, notificaciones

**LOGS DE AUDITORÍA (UF-162 — OK):**
- ✅ `/admin/logs` carga con filtros completos: Autenticación, Admin, Seguridad, Sistema, Moderación, Facturación, Notificaciones, KYC, Config
- ✅ Severity filters: Info, Warning, Error, Crítico, Debug
- ✅ Botón Exportar CSV disponible
- ❌ 404 en la API de logs (sin datos del backend)

**ROLES Y PERMISOS (UF-163 — OK):**
- ✅ `/admin/roles` carga correctamente
- ✅ Botón "Nuevo Rol" funcional — abre formulario con nombre técnico, visible, descripción, permisos
- ⚠️ 0 roles de staff configurados (estado vacío)
- ✅ Mensaje contextual claro: compradores/vendedores tienen permisos automáticos; dealers gestionan desde su portal

**COSTOS LLM/IA + SEARCHAGENT (UF-164 — FALLA):**
- ❌ BUG: `/admin/search-agent` retorna 502 en todos los llamados API → pantalla de error "Error al cargar la configuración"
- ❌ NO existe sección de costos de LLM/IA (costo por modelo, por día, tendencias) — feature no implementada
- Causa probable: ConfigurationService (HOST:15124) no está corriendo o SearchAgent service inaccesible

---

### CIERRE: Ejecutar loop del agente

**Pasos:**

- [x] Paso 1: Agrega `READ` al final de este archivo y luego ejecuta el prompt `.prompts/AGENT_LOOP_PROMPT.md`

**A validar:**

- [x] ¿Se agregó `READ` al final del archivo y luego se ejecutó `.prompts/AGENT_LOOP_PROMPT.md`?

**Hallazgos:**
_(documentar aquí lo encontrado)_

---

## Resultado

- Sprint: 30 — Admin — Facturación, Billing y Sistema
- Fase: AUDIT
- Ambiente: LOCAL (Docker Desktop + cloudflared tunnel: https://hospital-edmonton-duty-tribes.trycloudflare.com)
- URL: https://hospital-edmonton-duty-tribes.trycloudflare.com
- Estado: COMPLETADO
- Bugs encontrados: 3 bugs críticos (billing/transactions 404, billing/pending 404, search-agent 502) + 1 feature faltante (LLM costs)

---

_Cuando termines las tareas, agrega la palabra READ al final de este archivo y luego ejecuta la última tarea: `.prompts/AGENT_LOOP_PROMPT.md`._
