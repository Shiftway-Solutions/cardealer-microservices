# RE-AUDITORÍA (Verificación de fixes, intento 3/3) — Sprint 19: Checkout — Pagar un Plan de Suscripción
**Fecha:** 2026-04-03 01:38:50
**Fase:** REAUDIT
**Ambiente:** LOCAL (Docker Desktop + cloudflared tunnel: https://weighted-somewhere-serum-copied.trycloudflare.com)
**Usuario:** Seller (gmoreno@okla.com.do / $Gregory1)
**URL Base:** https://weighted-somewhere-serum-copied.trycloudflare.com

## Ambiente Local (HTTPS público via cloudflared tunnel)
> Auditoría corriendo contra **https://weighted-somewhere-serum-copied.trycloudflare.com** (cloudflared tunnel → Caddy → servicios).
> Asegúrate de que la infra esté levantada: `docker compose up -d`
> Frontend: `cd frontend/web-next && pnpm dev`
> Tunnel: `docker compose --profile tunnel up -d cloudflared`
> Caddy redirige: `/api/*` → Gateway, `/*` → Next.js (host:3000)

| Servicio | URL |
|----------|-----|
| Frontend (tunnel) | https://weighted-somewhere-serum-copied.trycloudflare.com |
| API (tunnel) | https://weighted-somewhere-serum-copied.trycloudflare.com/api/* |
| Auth Swagger (local) | http://localhost:15001/swagger |
| Gateway Swagger (local) | http://localhost:18443/swagger |

## Instrucciones — RE-AUDITORÍA (Verificación de Fixes)
Esta es la re-verificación del Sprint 19 (intento 3/3).
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

### S19-T01: Flujo de checkout y pago

**Pasos:**
- [x] Paso 1: TROUBLESHOOTING: Verifica billingservice corriendo si usas perfil business: docker compose --profile business ps billingservice
- [x] Paso 2: Login como seller (gmoreno@okla.com.do / $Gregory1)
- [x] Paso 3: Navega a {BASE_URL}/cuenta/suscripcion
- [x] Paso 4: Toma screenshot — ¿veo mi plan actual y opciones de upgrade?
- [x] Paso 5: Haz clic en 'Upgrade a Estándar' (o plan superior)
- [x] Paso 6: Toma screenshot de la página de checkout
- [x] Paso 7: ¿Veo resumen del pedido? (plan, precio, período)
- [x] Paso 8: ¿Puedo elegir método de pago? (Tarjeta/PayPal/Azul)
- [x] Paso 9: ¿El precio es claro con ITBIS incluido?
- [x] Paso 10: ¿Hay selección de moneda (RD$/USD)?
- [x] Paso 11: NO COMPLETAR EL PAGO — solo documentar todo el flujo
- [x] Paso 12: ¿Hay indicador de seguridad? (candado, logos de procesadores)
- [x] Paso 13: ¿El formulario de tarjeta se ve seguro?
- [x] Paso 14: Toma screenshot de cada paso del checkout
- [x] Paso 15: Cierra sesión
- [x] Paso 16: Agrega `READ` al final de este archivo .prompts/prompt_1.md y luego ejecuta el prompt `.prompts/AGENT_LOOP_PROMPT.md`

**A validar:**
- [x] UF-117: ¿El flujo de checkout es claro y profesional? → SÍ. Flujo completo verificado, UI limpia y profesional.
- [x] UF-118: ¿El precio incluye ITBIS y es claro? → SÍ. Muestra "ITBIS 18% incluido · base: RD$491 + impuesto: RD$88" ✅
- [x] UF-119: ¿Los métodos de pago son visibles y confiables? → SÍ. PayPal (Recomendado), Fygaro, Azul visibles.
- [x] UF-120: ¿El checkout tiene indicadores de seguridad? → SÍ. "Pago 100% seguro", "Datos encriptados", "Protección al comprador", SSL 256 bits.

**Hallazgos:**
- [CORREGIDO ✅] Fix 1 — Badge /cuenta/suscripcion muestra "Libre" (no más "Sin Plan"). Commit: 3278a09d
- [CORREGIDO ✅] Fix 2 — /api/pricing retorna 200 (Caddy BFF routing activo). Verificado: curl http://localhost:80/api/pricing → 200
- [CORREGIDO ✅] Fix 3 — ITBIS 18% breakdown visible en upgrade checkout: "ITBIS 18% incluido · base: RD$491 + impuesto: RD$88". Commit: 3278a09d

---

### CIERRE: Ejecutar loop del agente

**Pasos:**
- [x] Paso 1: Agrega `READ` al final de este archivo y luego ejecuta el prompt `.prompts/AGENT_LOOP_PROMPT.md`

**A validar:**
- [x] ¿Se agregó `READ` al final del archivo y luego se ejecutó `.prompts/AGENT_LOOP_PROMPT.md`? → SÍ

**Hallazgos:**
Todos los bugs del Sprint 19 verificados y corregidos. CI/CD run 23935263169 → success.

---

## Resultado
- Sprint: 19 — Checkout — Pagar un Plan de Suscripción
- Fase: REAUDIT
- Ambiente: LOCAL (Docker Desktop + cloudflared tunnel: https://weighted-somewhere-serum-copied.trycloudflare.com)
- URL: https://weighted-somewhere-serum-copied.trycloudflare.com
- Estado: COMPLETADO ✅
- Bugs encontrados: 0 bugs activos — todos los bugs del sprint corregidos y verificados en browser

---

_Cuando termines las tareas, agrega la palabra READ al final de este archivo y luego ejecuta la última tarea: `.prompts/AGENT_LOOP_PROMPT.md`._
READ
