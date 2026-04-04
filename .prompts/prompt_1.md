# AUDITORÍA — Sprint 31: SearchAgent — Profesionalización y Ajuste Fino
**Fecha:** 2026-04-04 09:52:15
**Fase:** AUDIT
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
- [x] Paso 1: TROUBLESHOOTING: SearchAgent healthy en 15155 ✅
- [x] Paso 2: Login como buyer (buyer002@okla-test.com / BuyerTest2026!) → OK
- [x] Paso 3: Navega a {BASE_URL}/buscar → 5 vehículos disponibles
- [x] Paso 4: Q1: 'jeepetón bonito pa la familia' → Jeep SUV, confianza 78%, URL ?make=Jeep&body_type=suv ✅
- [x] Paso 5: Q2: 'Algo menor de un palo' → precio_max 1M DOP, confianza 85%, 1 vehículo ✅✅
- [x] Paso 6: Q3: 'Entre 500 y 800' → "consulta ambigua" mensaje clarificación ✅
- [x] Paso 7: Q4: 'Santiago o el Cibao' → provincia:Santiago (2768ms) ✅
- [x] Paso 8: Q5: 'Del Distrito Nacional' → provincia:Distrito Nacional ✅
- [x] Paso 9: Q6: 'Quiero test drive' → consulta incompleta, mensaje clarificación (5725ms SLOW) ⚠️
- [x] Paso 10: Q7: '' (vacío) → UI button disabled, API 400 (esperado) ✅
- [x] Paso 11: Q8: 'asdfghjkl' → "búsqueda no está clara" graceful ✅
- [x] Paso 12: Q9: 'Algo deportivo y rojo' → tipo:coupe, color:rojo (5311ms SLOW) ⚠️
- [x] Paso 13: Q10: 'El más barato' → precio_max 800K DOP (14111ms SLOW) ❌
- [x] Paso 14: Q11: 'Camioneta pa trabajo pesado' → pickup+diesel+4x4, confianza 92% ✅✅
- [x] Paso 15: Q12: 'Carro de mujer' → sin esterotipos de género ✅
- [x] Paso 16: Q13: 'placa ABC123' → routing a P.Nacional 911 + DGIT, empático ✅✅
- [x] Paso 17: Q14: 'Honda CRV 2019-2022 gasolina' → filtros exactos, confianza alta ✅
- [x] Paso 18: Q15: 'Cuánto vale Corolla 2020?' → redirecta a búsqueda ✅
- [x] Paso 19: Q16: 'Tiene financiamiento?' → out-of-scope, redirecta ✅
- [x] Paso 20: Q17: 'Carro con poca milla' → km_max:50,000 ✅
- [x] Paso 21: Q18: 'No gastar gasolina' → solo condicion:usado, fuel eff. no extraída ⚠️
- [x] Paso 22: Q19: 'RAV4 VS CRV' → "consulta comparativa" graceful ✅
- [x] Paso 23: Q20: 'Quiero hablar con alguien' → TIMEOUT en primera llamada (>15s) ❌
- [x] Paso 24: Screenshots tomados (Q1 jeepetón, Q2 un palo, Q13 placa)
- [x] Paso 25: READ al final

**A validar:**
- [x] UF-165: ¿Entiende español dominicano coloquial? → ✅ PASA — "jeepetón"→Jeep (78%), "un palo"→1M DOP (85%), "Cibao"→Santiago, "poca milla"→km_max:50K
- [x] UF-166: ¿Traduce jerga RD a filtros correctos? → ✅ PASA — Q02, Q04, Q05, Q09, Q10, Q11, Q14, Q17 con filtros exactos
- [x] UF-167: ¿Maneja edge cases sin crash? → ✅ PASA — ningún crash. Q12 sin estereotipos, Q13 routing empático, Q19 comparación graceful
- [ ] UF-168: ¿Responde en < 5 segundos? → ❌ FALLA — Q06: 5725ms, Q09: 5311ms, Q10: 14111ms (primera llamada sin caché). Q20 timeout
- [x] UF-169: ¿Tono profesional pero cercano? → ✅ PASA — "¡Hola! 👋", "Lamento tu situación", emojis, directo

**Hallazgos:**
1. **AI NLP FUNCIONANDO** — AWS Bedrock IAM resuelto. 11/20 queries con filtros exactos extraídos. 
2. **Q02 "un palo" → 1M DOP** — Jerga dominicana correctamente interpretada ✅
3. **Q11 "camioneta/trabajo pesado" → pickup+diesel+4x4** — confianza 92% ✅
4. **Q13 "placa robada" → routing a Policía Nacional 911** — excelente manejo out-of-scope ✅
5. **BUG: UF-168 first-call latency** — Q06: 5.7s, Q09: 5.3s, Q10: 14.1s, Q20: timeout (>15s). Caché funciona (2-15ms). Bedrock primera llamada es lenta.
6. **BUG: Q18 fuel efficiency** — "no gastar gasolina" solo extrae condicion:usado, no fuel_type:híbrido/eléctrico
7. **BUG: Q20 timeout** — "Quiero hablar con alguien de OKLA" timeout en primera llamada (posible código path diferente)

---

### CIERRE: Ejecutar loop del agente

**Pasos:**
- [x] Paso 1: READ al final del archivo

**A validar:**
- [x] ¿Se agregó `READ`?

**Hallazgos:**
REAUDIT 3/3 completado 2026-04-04 ~10:05 AST. AI funcionando. 3 bugs de latencia/timeout encontrados.

---

## Resultado
- Sprint: 31 — SearchAgent — Profesionalización y Ajuste Fino
- Fase: REAUDIT (3/3)
- Estado: COMPLETADO — AI NLP funcional. UF-165/166/167/169 PASA. UF-168 FALLA (latencia primera llamada)
- Bugs encontrados: 3 (latencia Bedrock cold-start, Q18 fuel efficiency, Q20 timeout)

---

_Cuando termines las tareas, agrega la palabra READ al final de este archivo y luego ejecuta la última tarea: `.prompts/AGENT_LOOP_PROMPT.md`._

READ
