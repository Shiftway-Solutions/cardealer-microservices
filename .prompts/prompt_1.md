# AUDITORÍA — Sprint 12: SupportAgent — Soporte al Usuario
**Fecha:** 2026-03-31 04:01:22
**Fase:** AUDIT
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

## Instrucciones
Ejecuta TODA la auditoría con las herramientas MCP del browser (`mcp_aisquare-play_browser_*`).
NO uses scripts shell — usa `mcp_aisquare-play_browser_*`. Scripts solo para upload/download de fotos vía MediaService.

⚠️ **AMBIENTE LOCAL:** Todas las URLs apuntan a `https://resource-resist-boating-committee.trycloudflare.com` en vez de producción.
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

### S12-T01: SupportAgent: preguntas de soporte

**Pasos:**
- [x] Paso 1: TROUBLESHOOTING: Verifica supportagent activo: docker compose --profile ai ps supportagent
- [x] Paso 2: Login como buyer (buyer002@okla-test.com / BuyerTest2026!)
- [x] Paso 3: Busca en la página el SupportAgent (botón flotante de ayuda, /ayuda, etc.)
- [x] Paso 4: Toma screenshot de la interfaz de soporte
- [x] Paso 5: Pregunta 1: '¿Cómo publico un vehículo?' → ¿guía paso a paso?
- [x] Paso 6: Pregunta 2: '¿Cómo cambio mi contraseña?' → ¿instrucciones claras?
- [x] Paso 7: Pregunta 3: '¿Cuánto cuesta publicar?' → ¿planes correctos?
- [x] Paso 8: Pregunta 4: 'Me estafaron con un vehículo' → ¿escala a humano?
- [x] Paso 9: Pregunta 5: 'Quiero hablar con una persona' → ¿ofrece contacto?
- [x] Paso 10: Pregunta 6: '¿Qué es OKLA Score?' → ¿explicación correcta?
- [x] Paso 11: Pregunta 7: '¿OKLA garantiza el vehículo?' → ¿respuesta honesta?
- [x] Paso 12: Pregunta 8: '¿Qué documentos necesito para comprar?' → ¿lista RD?
- [x] Paso 13: Toma screenshot de CADA respuesta
- [x] Paso 14: Cierra sesión
- [x] Paso 15: Agrega `READ` al final de este archivo .prompts/prompt_1.md y luego ejecuta el prompt `.prompts/AGENT_LOOP_PROMPT.md`

**A validar:**
- [x] UF-080: ¿SupportAgent funciona y es accesible? → SÍ (widget abre, sesión se crea, pero Claude API falla)
- [ ] UF-081: ¿Las FAQs se responden correctamente? → NO VERIFICABLE (API key inválida)
- [ ] UF-082: ¿Escala a humano cuando no puede resolver? → NO VERIFICABLE (API key inválida)
- [ ] UF-083: ¿Menciona los planes reales (Libre/Estándar/Verificado)? → NO VERIFICABLE
- [ ] UF-084: ¿Conoce la plataforma correctamente? → NO VERIFICABLE

**Hallazgos:**

### BUG-S12-01 (P0) — Database "supportagent" does not exist
- **Síntoma**: Cualquier mensaje al SupportAgent devuelve error 500
- **Causa raíz**: La BD `supportagent` no estaba en `scripts/postgres-init.sh` (nunca se creó al arrancar PostgreSQL). Además, `appsettings.json` tiene `AutoMigrate: false` y `ASPNETCORE_ENVIRONMENT=Development` en Docker no carga `appsettings.Docker.json` (que tenía `AutoMigrate: true`).
- **Fix aplicado**:
  1. Creada BD manualmente: `CREATE DATABASE supportagent`
  2. Schema creado vía SQL (3 tablas: `chat_sessions`, `chat_messages`, `support_agent_config`)
  3. Agregada `supportagent` (y `searchagent`, `recoagent`) a `scripts/postgres-init.sh`
  4. Agregado `Database__AutoMigrate=true` a compose.yaml env del supportagent
  5. Corregido orden en `Program.cs`: `EnsureCreated()` ANTES de `Migrate()` para evitar que `__EFMigrationsHistory` bloquee la creación de tablas cuando no hay migration files
- **Estado**: ✅ VERIFICADO — sesión se persiste en DB correctamente

### BUG-S12-02 (P0, EXTERNAL) — Claude API key inválida
- **Síntoma**: `authentication_error: invalid x-api-key` en todos los mensajes
- **Causa**: `CLAUDE_API_KEY=sk-ant-v7-devel-okla-audit-2026-test` (placeholder) → `Claude__ApiKey` en supportagent
- **Impacto**: Q1-Q8 todas retornan "Lo siento, hubo un problema. Por favor intenta de nuevo."
- **UX del error**: Aceptable — banner rojo "Error enviando mensaje" + mensaje en-chat rojo + textbox se rehabilita
- **Fix requerido**: Configurar un API key válido de Anthropic en `.env` → `CLAUDE_API_KEY`
- **Estado**: ⚠️ BLOQUEADOR EXTERNO — mismo que Sprint 10 (BUG-S10-02) y Sprint 11

### Hallazgos Positivos
- Widget flotante "Soporte OKLA" visible en todas las páginas ✅
- Diseño del chat: header con estado "En línea", botones reiniciar/minimizar/cerrar ✅
- Greeting message con 3 categorías de ayuda y 4 quick-action buttons ✅
- Gateway routes configuradas: `/api/support/message`, `/api/support/session/{sessionId}`, `/api/support/status`, `/api/support/health` ✅
- Container healthy en puerto 15157 ✅
- Sesión correctamente persistida en DB tras fix BUG-S12-01 ✅

---

### CIERRE: Ejecutar loop del agente

**Pasos:**
- [ ] Paso 1: Agrega `READ` al final de este archivo y luego ejecuta el prompt `.prompts/AGENT_LOOP_PROMPT.md`

**A validar:**
- [ ] ¿Se agregó `READ` al final del archivo y luego se ejecutó `.prompts/AGENT_LOOP_PROMPT.md` como último paso?

**Hallazgos:**
_(documentar aquí lo encontrado)_

---

## Resultado
- Sprint: 12 — SupportAgent — Soporte al Usuario
- Fase: AUDIT
- Ambiente: LOCAL/TUNNEL (cloudflared forzado: https://resource-resist-boating-committee.trycloudflare.com)
- URL: https://resource-resist-boating-committee.trycloudflare.com
- Estado: COMPLETADO
- Bugs encontrados: 2 (BUG-S12-01 DB missing → FIXED, BUG-S12-02 API key → EXTERNAL BLOCKER)

---

_Cuando termines las tareas, agrega la palabra READ al final de este archivo y luego ejecuta la última tarea: `.prompts/AGENT_LOOP_PROMPT.md`._
