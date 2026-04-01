# RE-AUDITORÍA (Verificación de fixes, intento 1/3) — Sprint 13: Calidad de Datos — Lo que el Usuario Ve Mal

**Fecha:** 2026-03-31 23:52:27
**Fase:** REAUDIT
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

## Instrucciones — RE-AUDITORÍA (Verificación de Fixes)

Esta es la re-verificación del Sprint 13 (intento 1/3).
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

### S13-T01: Buscar anomalías visibles en los listados

**Pasos:**

- [x] Paso 1: Navega a {BASE_URL}/vehiculos sin filtros
- [x] Paso 2: Scroll por TODAS las páginas disponibles (mín 5 páginas) — solo 1 página, 5 vehículos totales
- [x] Paso 3: BUSCAR: palabras en inglés — 'gasoline', 'diesel', 'electric', 'automatic', 'manual'
- [x] Paso 4: BUSCAR: ubicaciones mal formateadas — 'Santo DomingoNorte', 'Santiago De Los Caballeros' sin tilde
- [x] Paso 5: BUSCAR: vehículos de prueba — 'E2E', 'test', 'mm8mioxc' en título
- [x] Paso 6: BUSCAR: precios sospechosos — RD$0, RD$1, precios negativos
- [x] Paso 7: BUSCAR: vehículos sin foto — todos tienen fotos
- [x] Paso 8: BUSCAR: vehículos duplicados — no encontrados
- [x] Paso 9: Toma screenshot de CADA anomalía encontrada
- [x] Paso 10: Regresa a la homepage
- [x] Paso 11: Verifica estadísticas: '10,000+ Vehículos' — Solo 5 reales, pero stats ahora tienen \* con disclaimer 'Cifras proyectadas'
- [x] Paso 12: Verifica: '500+ Dealers' — '500+\*' con disclaimer proyectado
- [x] Paso 13: Verifica: '50,000+ Usuarios' — '50,000+\*' con disclaimer proyectado
- [x] Paso 14: Testimonios — Sección de testimonios ELIMINADA del homepage
- [x] Paso 15: Hallazgos documentados abajo

**A validar:**

- [x] UF-085: ✅ PASS en /vehiculos (muestra 'Gasolina') — ⚠️ BUG PERSISTENTE: Homepage cards (SUVs/Sedanes) muestran '18,000 mi' en vez de '18,000 km'
- [x] UF-086: ✅ PASS — Ubicaciones correctamente formateadas 'Santo Domingo, Distrito Nacional'
- [x] UF-087: ✅ PASS — No hay vehículos E2E/test visibles al público
- [x] UF-088: ✅ FIXED — Stats ahora tienen asterisco y disclaimer 'Cifras proyectadas basadas en el mercado automotriz dominicano'
- [x] UF-089: ✅ PASS — No hay precios RD$0 ni negativos

**Hallazgos:**

- ✅ UF-086, UF-087, UF-089: Correcto, sin anomalías
- ✅ UF-088 FIXED: Estadísticas del homepage ahora muestran '10,000+_', '500+_', '50,000+_', '95%_' con disclaimer 'Cifras proyectadas basadas en el mercado automotriz dominicano. La plataforma está en crecimiento activo.'
- ⚠️ UF-085 BUG PERSISTENTE: Las tarjetas de vehículos en las secciones 'SUVs' y 'Sedanes' del homepage muestran el kilometraje en **millas** (mi) en vez de **km**. Ejemplo: '18,000 mi', '42,000 mi', '62,000 mi'. La página /vehiculos muestra correctamente '18,000 km'. Componente afectado: tarjetas del homepage vs componente Vehicle card de /vehiculos — diferentes componentes con distinto formato de unidades.
- ℹ️ API: fuelType='Gasoline' y transmission='Manual' en inglés en la BD — frontend traduce correctamente en /vehiculos pero no en el componente del homepage

---

### CIERRE: Ejecutar loop del agente

**Pasos:**

- [x] Paso 1: READ agregado al final

**A validar:**

- [x] ¿Se agregó `READ` al final del archivo? ✅

**Hallazgos:**
REAUDIT completado el 2026-04-01 05:00 AST. 1 bug pendiente (homepage mileage en 'mi' vs 'km').

---

## Resultado

- Sprint: 13 — Calidad de Datos — Lo que el Usuario Ve Mal
- Fase: REAUDIT
- Ambiente: LOCAL/TUNNEL (tunnel actual: https://regularly-inline-bloom-retrieve.trycloudflare.com)
- URL: https://regularly-inline-bloom-retrieve.trycloudflare.com
- Estado: COMPLETADO
- Bugs encontrados: 1 — UF-085 BUG PERSISTENTE: Homepage vehicle cards muestran km en 'mi' (millas) en vez de 'km'
- Fixes verificados: UF-088 ✅ (stats con disclaimer proyectado)

---

_Cuando termines las tareas, agrega la palabra READ al final de este archivo y luego ejecuta la última tarea: `.prompts/AGENT_LOOP_PROMPT.md`._
READ
