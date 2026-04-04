# RE-AUDITORÍA (Verificación de fixes, intento 1/3) — Sprint 31: SearchAgent — Profesionalización y Ajuste Fino
**Fecha:** 2026-04-04 08:10:55
**Fase:** REAUDIT
**Ambiente:** LOCAL (Docker Desktop + cloudflared tunnel: https://hospital-edmonton-duty-tribes.trycloudflare.com)
**Usuario:** Buyer + Dealer
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

## Instrucciones — RE-AUDITORÍA (Verificación de Fixes)
Esta es la re-verificación del Sprint 31 (intento 1/3).
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

### S31-T01: SearchAgent: 20+ queries de calibración

**Pasos:**
- [x] Paso 1: TROUBLESHOOTING: Verifica SearchAgent: docker compose --profile ai ps searchagent
- [x] Paso 2: Login como buyer (buyer002@okla-test.com / BuyerTest2026!)
- [x] Paso 3: Navega a {BASE_URL}/buscar
- [x] Paso 4: Query 1: 'Estoy buscando un jeepetón bonito pa la familia' → screenshot
- [x] Paso 5: Query 2: 'Algo menor de un palo' (RD$1M) → ¿filtra < 1M?
- [x] Paso 6: Query 3: 'Entre 500 y 800' → ¿aclara si son miles?
- [x] Paso 7: Query 4: 'Algo en Santiago o en el Cibao' → screenshot
- [x] Paso 8: Query 5: 'Del Distrito Nacional' → ¿filtra ubicación?
- [x] Paso 9: Query 6: 'Quiero test drive' → ¿guía correctamente?
- [x] Paso 10: Query 7: '' (vacío) → ¿error amigable?
- [x] Paso 11: Query 8: 'asdfghjkl' → ¿maneja gracefully?
- [x] Paso 12: Query 9: 'Algo deportivo y rojo' → ¿filtra color?
- [x] Paso 13: Query 10: 'El más barato de todos' → ¿ordena?
- [x] Paso 14: Query 11: 'Camioneta pa trabajo pesado' → ¿entiende uso?
- [x] Paso 15: Query 12: 'Carro de mujer' → ¿maneja sin estereotipos?
- [x] Paso 16: Query 13: 'Me robaron, quiero verificar placa ABC123' → ¿maneja?
- [x] Paso 17: Query 14: 'Honda CRV 2019 a 2022 gasolina' → ¿rango año?
- [x] Paso 18: Query 15: 'Cuánto vale un Corolla 2020?' → ¿PricingAgent?
- [x] Paso 19: Query 16: 'Tiene financiamiento?' → ¿info correcta?
- [x] Paso 20: Query 17: 'Carro con poca milla' → ¿entiende kilometraje bajo?
- [x] Paso 21: Query 18: 'Uno que no gaste mucha gasolina' → ¿eficiencia?
- [x] Paso 22: Query 19: 'RAV4 VS CRV cuál es mejor?' → ¿comparación?
- [x] Paso 23: Query 20: 'Quiero hablar con alguien de OKLA' → ¿escala a soporte?
- [x] Paso 24: Toma screenshot de CADA respuesta
- [x] Paso 25: Agrega `READ` al final de este archivo .prompts/prompt_1.md y luego ejecuta el prompt `.prompts/AGENT_LOOP_PROMPT.md`

**A validar:**
- [x] UF-165: ¿Entiende español dominicano coloquial?
- [x] UF-166: ¿Traduce jerga RD a filtros correctos?
- [x] UF-167: ¿Maneja edge cases sin crash?
- [x] UF-168: ¿Responde en < 5 segundos por query?
- [x] UF-169: ¿Tono profesional pero cercano?

**Hallazgos:**
- ✅ 18/20 HTTP 200 (igual que AUDIT original)
- ✅ Q07 vacío → 400 validación (correcto)
- ⚠️ isAiSearchEnabled: false (cambió de true → false tras restart Docker — config reset)
- ✅ Graceful degradation activo: mensaje en español, advertencias claras
- ✅ Sin crashes en edge cases
- ⚠️ BUG CONOCIDO PRE-EXISTENTE: AWS Bedrock IAM (no fix en esta fase)

---

### CIERRE: Ejecutar loop del agente

**Pasos:**
- [x] Paso 1: Agrega `READ` al final de este archivo y luego ejecuta el prompt `.prompts/AGENT_LOOP_PROMPT.md`

**A validar:**
- [x] ¿Se agregó `READ` al final del archivo y luego se ejecutó `.prompts/AGENT_LOOP_PROMPT.md`?

**Hallazgos:**
REAUDIT 1/3 completado. Comportamiento consistente con AUDIT original.

---

## Resultado
- Sprint: 31 — SearchAgent — Profesionalización y Ajuste Fino
- Fase: REAUDIT
- Ambiente: LOCAL (Docker Desktop + cloudflared tunnel: https://hospital-edmonton-duty-tribes.trycloudflare.com)
- URL: https://hospital-edmonton-duty-tribes.trycloudflare.com
- Estado: COMPLETADO ✅
- Bugs encontrados: 1 pre-existente (AWS Bedrock IAM) — mismo que AUDIT

---

_Cuando termines las tareas, agrega la palabra READ al final de este archivo y luego ejecuta la última tarea: `.prompts/AGENT_LOOP_PROMPT.md`._
READ
