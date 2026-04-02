# AUDITORÍA — Sprint 16: Legal y Privacidad — Confianza del Usuario
**Fecha:** 2026-04-02 01:46:21
**Fase:** AUDIT
**Ambiente:** LOCAL (Docker Desktop + cloudflared tunnel: https://thousand-erik-cheers-clubs.trycloudflare.com)
**Usuario:** Guest (incógnito)
**URL Base:** https://thousand-erik-cheers-clubs.trycloudflare.com

## Ambiente Local (HTTPS público via cloudflared tunnel)
> Auditoría corriendo contra **https://thousand-erik-cheers-clubs.trycloudflare.com** (cloudflared tunnel → Caddy → servicios).
> Asegúrate de que la infra esté levantada: `docker compose up -d`
> Frontend: `cd frontend/web-next && pnpm dev`
> Tunnel: `docker compose --profile tunnel up -d cloudflared`
> Caddy redirige: `/api/*` → Gateway, `/*` → Next.js (host:3000)

| Servicio | URL |
|----------|-----|
| Frontend (tunnel) | https://thousand-erik-cheers-clubs.trycloudflare.com |
| API (tunnel) | https://thousand-erik-cheers-clubs.trycloudflare.com/api/* |
| Auth Swagger (local) | http://localhost:15001/swagger |
| Gateway Swagger (local) | http://localhost:18443/swagger |

## Instrucciones
Ejecuta TODA la auditoría con las herramientas MCP del browser (`mcp_aisquare-play_browser_*`).
NO uses scripts shell — usa `mcp_aisquare-play_browser_*`. Scripts solo para upload/download de fotos vía MediaService.

⚠️ **AMBIENTE LOCAL:** Todas las URLs apuntan a `https://thousand-erik-cheers-clubs.trycloudflare.com` en vez de producción.
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

### S16-T01: Cookie consent y políticas legales

**Pasos:**
- [ ] Paso 1: Abre ventana de incógnito y navega a {BASE_URL}
- [ ] Paso 2: ¿Aparece banner de cookie consent? Toma screenshot
- [ ] Paso 3: Si hay botón 'Configurar cookies' → haz clic y toma screenshot
- [ ] Paso 4: ¿Hay categorías granulares? (esenciales, analytics, marketing)
- [ ] Paso 5: ¿Puedo rechazar todo excepto esenciales?
- [ ] Paso 6: ¿La elección persiste? (cierra y reabre)
- [ ] Paso 7: Navega a {BASE_URL}/privacidad
- [ ] Paso 8: ¿Menciona Ley 172-13 de Protección de Datos? Toma screenshot
- [ ] Paso 9: ¿Describe qué datos se recopilan?
- [ ] Paso 10: ¿Explica derechos del usuario?
- [ ] Paso 11: Navega a {BASE_URL}/terminos
- [ ] Paso 12: ¿Dice 'jurisdicción: República Dominicana'? ¿Fecha 2026?
- [ ] Paso 13: Navega a {BASE_URL}/cookies (si existe)
- [ ] Paso 14: ¿Lista de cookies con propósito y duración?
- [ ] Paso 15: Agrega `READ` al final de este archivo .prompts/prompt_1.md y luego ejecuta el prompt `.prompts/AGENT_LOOP_PROMPT.md`

**A validar:**
- [ ] UF-101: ¿Cookie banner aparece en primera visita?
- [ ] UF-102: ¿Se puede rechazar cookies no esenciales?
- [ ] UF-103: ¿Privacidad menciona Ley 172-13?
- [ ] UF-104: ¿Términos con jurisdicción RD y fecha actualizada?

**Hallazgos:**
_(documentar aquí lo encontrado)_

---

### CIERRE: Ejecutar loop del agente

**Pasos:**
- [ ] Paso 1: Agrega `READ` al final de este archivo y luego ejecuta el prompt `.prompts/AGENT_LOOP_PROMPT.md`

**A validar:**
- [ ] ¿Se agregó `READ` al final del archivo y luego se ejecutó `.prompts/AGENT_LOOP_PROMPT.md`?

**Hallazgos:**
_(documentar aquí lo encontrado)_

---

## Resultado
- Sprint: 16 — Legal y Privacidad — Confianza del Usuario
- Fase: AUDIT
- Ambiente: LOCAL (Docker Desktop + cloudflared tunnel: https://thousand-erik-cheers-clubs.trycloudflare.com)
- URL: https://thousand-erik-cheers-clubs.trycloudflare.com
- Estado: EN PROGRESO
- Bugs encontrados: _(completar)_

---

_Cuando termines las tareas, agrega la palabra READ al final de este archivo y luego ejecuta la última tarea: `.prompts/AGENT_LOOP_PROMPT.md`._
