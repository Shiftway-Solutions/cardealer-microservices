# RE-AUDITORÍA (Verificación de fixes, intento 3/3) — Sprint 29: Admin — Contenido, Homepage, Banners, Promociones

**Fecha:** 2026-04-04 03:47:38
**Fase:** REAUDIT
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

## Instrucciones — RE-AUDITORÍA (Verificación de Fixes)

Esta es la re-verificación del Sprint 29 (intento 3/3).
Re-ejecuta las mismas tareas de auditoría con las herramientas MCP del browser (`mcp_aisquare-play_browser_*`) para verificar que los fixes funcionan.

- Si TODOS los bugs están corregidos → agrega `READ` al final de este archivo y luego ejecuta `.prompts/AGENT_LOOP_PROMPT.md`
- Si ALGÚN bug persiste → documenta cuáles persisten en 'Hallazgos'
  luego agrega `READ` al final de este archivo y ejecuta `.prompts/AGENT_LOOP_PROMPT.md`. El script enviará otra ronda de fixes.

IMPORTANTE: Usa `mcp_aisquare-play_browser_*` para todas las interacciones. NO scripts shell.

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

### S29-T01: Admin: gestión de contenido

**Pasos:**

- [x] Paso 1: Login como admin (admin@okla.local / Admin123!@#)
- [x] Paso 2: Navega a gestión de secciones de homepage
- [x] Paso 3: Toma screenshot — ¿puedo editar qué se muestra en el homepage?
- [x] Paso 4: Navega a gestión de banners/promociones
- [x] Paso 5: ¿Puedo crear/editar/activar banners?
- [x] Paso 6: Navega a gestión de FAQs
- [x] Paso 7: ¿Puedo agregar/editar preguntas frecuentes?
- [x] Paso 8: Navega a gestión de testimonios
- [x] Paso 9: ¿Los testimonios son editables? ¿Hay disclaimer de que son reales?
- [x] Paso 10: Navega a gestión de vehículos reportados
- [x] Paso 11: ¿Puedo ver y moderar reportes de listados?
- [x] Paso 12: Cierra sesión
- [x] Paso 13: Agrega `READ` al final de este archivo .prompts/prompt_1.md y luego ejecuta el prompt `.prompts/AGENT_LOOP_PROMPT.md`

**A validar:**

- [x] UF-157: ¿Secciones de homepage editables?
- [x] UF-158: ¿Banners y promociones gestionables?
- [x] UF-159: ¿FAQs editables desde admin?
- [x] UF-160: ¿Reportes de listados moderables?

**Hallazgos:**
- UF-157 PASS: /admin/secciones carga "Secciones del Homepage" con 7 secciones y controles de orden/toggle
- UF-158 PASS: /admin/contenido → Banners tab con 4 banners activos y botón "Nuevo Banner"
- UF-159 PASS: /admin/contenido → Páginas tab con "Preguntas Frecuentes (faq)" + botón "Editar página"
- UF-160 PASS: /admin/reportes carga con métricas (Total:0/Pendientes:0/Resueltos:0/Alta prioridad:0) y tabla moderación

---

### CIERRE: Ejecutar loop del agente

**Pasos:**

- [x] Paso 1: Agrega `READ` al final de este archivo y luego ejecuta el prompt `.prompts/AGENT_LOOP_PROMPT.md`

**A validar:**

- [x] ¿Se agregó `READ` al final del archivo y luego se ejecutó `.prompts/AGENT_LOOP_PROMPT.md`?

**Hallazgos:**
READ escrito. Loop ejecutado.

---

## Resultado

- Sprint: 29 — Admin — Contenido, Homepage, Banners, Promociones
- Fase: REAUDIT
- Ambiente: LOCAL (Docker Desktop + cloudflared tunnel: https://hospital-edmonton-duty-tribes.trycloudflare.com)
- URL: https://hospital-edmonton-duty-tribes.trycloudflare.com
- Estado: COMPLETADO
- Bugs encontrados: 0

---

_Cuando termines las tareas, agrega la palabra READ al final de este archivo y luego ejecuta la última tarea: `.prompts/AGENT_LOOP_PROMPT.md`._

READ
