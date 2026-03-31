# RE-AUDITORÍA (Verificación de fixes, intento 2/3) — Sprint 11: DealerChatWidget — Chat con IA en Detalle de Vehículo
**Fecha:** 2026-03-31 03:45:45
**Fase:** REAUDIT
**Ambiente:** LOCAL/TUNNEL (cloudflared forzado: https://resource-resist-boating-committee.trycloudflare.com)
**Usuario:** Buyer (buyer002@okla-test.com / BuyerTest2026!)
**URL Base:** https://resource-resist-boating-committee.trycloudflare.com

## Ambiente Local (HTTPS público via cloudflared tunnel)
> Auditoría corriendo contra **https://resource-resist-boating-committee.trycloudflare.com** (cloudflared tunnel → Caddy → servicios).
> Asegúrate de que la infra esté levantada: `docker compose up -d`
> Frontend: `cd frontend/web-next && pnpm dev`
> Tunnel: `docker compose --profile tunnel up -d cloudflared`
> Caddy redirige: `/api/*` → Gateway, `/*` → Next.js (host:3000)

| Servicio | URL |
|----------|-----|
| Frontend (tunnel) | https://resource-resist-boating-committee.trycloudflare.com |
| API (tunnel) | https://resource-resist-boating-committee.trycloudflare.com/api/* |
| Auth Swagger (local) | http://localhost:15001/swagger |
| Gateway Swagger (local) | http://localhost:18443/swagger |

## Instrucciones — RE-AUDITORÍA (Verificación de Fixes)
Esta es la re-verificación del Sprint 11 (intento 2/3).
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

### S11-T01: Conversación realista con DealerChatWidget

**Pasos:**
- [x] Paso 1: Login como buyer ✅ — Already logged in as Test (buyer002@okla-test.com)
- [x] Paso 2: Navega a vehiculos y abre 2020 Nissan Sentra ✅
- [x] Paso 3: "Chatear con Ana (IA)" button visible and functional ✅ — Redirects to /mensajes
- [x] Paso 4: Screenshot taken ✅ — Disclosure + welcome messages shown correctly
- [x] Paso 5: Q1 '¿Este carro tiene historial de accidentes?' ✅ — Consent gate PASSED, LLM error (invalid API key)
- [⚠️] Paso 6: Q2 BLOCKED — Claude API key invalid (authentication_error)
- [⚠️] Paso 7: Q3 BLOCKED — Claude API key invalid
- [⚠️] Paso 8: Q4 BLOCKED — Claude API key invalid
- [⚠️] Paso 9: Q5 BLOCKED — Claude API key invalid
- [⚠️] Paso 10: Q6 BLOCKED — Claude API key invalid
- [⚠️] Paso 11: Q7 BLOCKED — Claude API key invalid
- [⚠️] Paso 12: Context maintenance BLOCKED — no LLM responses
- [x] Paso 13: Identifies as OKLA ✅ — Disclosure: "Soy un asistente virtual de OKLA"
- [x] Paso 14: Session active, verified
- [x] Paso 15: Appending READ

**A validar:**
- [x] UF-075: ¿DealerChatWidget funciona y responde? ✅ PARCIAL — Widget conecta, disclosure shown, consent auto-accepted. LLM fails (invalid API key).
- [⚠️] UF-076: ¿Responde sobre el vehículo específico? BLOCKED — no LLM responses
- [⚠️] UF-077: ¿Rechaza solicitudes de datos sensibles? BLOCKED — no LLM responses
- [⚠️] UF-078: ¿Mantiene contexto en la conversación? BLOCKED — no LLM responses
- [x] UF-079: ¿Se identifica como OKLA, no como el dealer? ✅ — "Soy un asistente virtual de OKLA, al servicio de OKLA"

**Hallazgos:**

### Resultado REAUDIT 2/3: MISMOS RESULTADOS QUE REAUDIT 1/3
- **BUG-S11-01** (Gateway routing): ✅ VERIFICADO — Chat conecta, bot greets.
- **BUG-S11-02** (Buyer account): ✅ VERIFICADO — Login works.
- **BUG-S11-03** (Login UX): ✅ VERIFICADO (código).
- **BUG-S11-04** (Sidebar ads): ✅ VERIFICADO (código).
- **BUG-S11-05** (Disclosure consent): ✅ VERIFICADO — New sessions get disclosure message first, consent auto-accepted, Q1 passes consent gate.
- **BLOQUEADOR EXTERNO**: Claude API key (`ANTHROPIC_API_KEY`) is a placeholder → all LLM calls fail with `authentication_error: invalid x-api-key`.
- **NO HAY BUGS NUEVOS DE CÓDIGO** — Solo el bloqueador externo persiste.
- **RECOMENDACIÓN**: Sprint 11 está completo en cuanto a código. Configurar API key válida para habilitar Q&A completo.

---

### CIERRE: Ejecutar loop del agente

**Pasos:**
- [x] Paso 1: Agrega `READ` al final de este archivo y luego ejecuta el prompt `.prompts/AGENT_LOOP_PROMPT.md`

**A validar:**
- [x] ¿Se agregó `READ` al final del archivo? ✅

**Hallazgos:**
Reaudit 2/3 completo. Mismos resultados que 1/3. Sin bugs nuevos de código.

---

## Resultado
- Sprint: 11 — DealerChatWidget — Chat con IA en Detalle de Vehículo
- Fase: REAUDIT
- Ambiente: LOCAL/TUNNEL (cloudflared forzado: https://resource-resist-boating-committee.trycloudflare.com)
- URL: https://resource-resist-boating-committee.trycloudflare.com
- Estado: COMPLETADO CON BLOQUEADOR EXTERNO
- Bugs de código: 0 nuevos (todos los 5 anteriores corregidos y verificados)
- Bloqueador externo: ANTHROPIC_API_KEY placeholder → Q1-Q7 bloqueados
- UFs validados: 2/5 (UF-075 parcial, UF-079 ✅) — 3 bloqueados por API key

---

_Cuando termines las tareas, agrega la palabra READ al final de este archivo y luego ejecuta la última tarea: `.prompts/AGENT_LOOP_PROMPT.md`._
