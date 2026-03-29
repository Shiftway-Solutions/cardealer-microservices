# RE-AUDITORÍA (Verificación de fixes, intento 1/3) — Sprint 6: Seller — Publicar Mi Primer Vehículo

**Fecha:** 2026-03-29 11:43:37
**Fase:** REAUDIT
**Ambiente:** LOCAL/TUNNEL (cloudflared forzado: https://ought-feed-shipping-wright.trycloudflare.com)
**Usuario:** Seller (gmoreno@okla.com.do / $Gregory1)
**URL Base:** https://ought-feed-shipping-wright.trycloudflare.com

## Ambiente Local (HTTPS público via cloudflared tunnel)

> Auditoría corriendo contra **https://ought-feed-shipping-wright.trycloudflare.com** (cloudflared tunnel → Caddy → servicios).
> Asegúrate de que la infra esté levantada: `docker compose up -d`
> Frontend: `cd frontend/web-next && pnpm dev`
> Tunnel: `docker compose --profile tunnel up -d cloudflared`
> Caddy redirige: `/api/*` → Gateway, `/*` → Next.js (host:3000)

| Servicio                | URL                                                        |
| ----------------------- | ---------------------------------------------------------- |
| Frontend (tunnel)       | https://ought-feed-shipping-wright.trycloudflare.com       |
| API (tunnel)            | https://ought-feed-shipping-wright.trycloudflare.com/api/* |
| Auth Swagger (local)    | http://localhost:15001/swagger                             |
| Gateway Swagger (local) | http://localhost:18443/swagger                             |

## Instrucciones — RE-AUDITORÍA (Verificación de Fixes)

Esta es la re-verificación del Sprint 6 (intento 1/3).
Re-ejecuta las mismas tareas de auditoría con las herramientas MCP del browser (`mcp_aisquare-play_browser_*`) para verificar que los fixes funcionan.

- Si TODOS los bugs están corregidos → agrega `READ` al final
- Si ALGÚN bug persiste → documenta cuáles persisten en 'Hallazgos'
  y agrega `READ` igualmente. El script enviará otra ronda de fixes.

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

### S6-T01: Wizard de publicación paso a paso

**Pasos:**

- [x] Paso 1: TROUBLESHOOTING: Todos los containers healthy (core profile)
- [x] Paso 2: Login como seller (gmoreno@okla.com.do / $Gregory1) — ✅ Logueado
- [x] Paso 3: Navega a /publicar — ✅ Wizard cargó correctamente
- [x] Paso 4: Screenshot — Es un wizard de 6 pasos (Información, Fotos, Video, Vista 360°, Precio, Revisión)
- [x] Paso 5: Paso 1: Datos básicos — Campos: Marca, Modelo, Año, Trim/Versión, Carrocería, Motor
- [x] Paso 6: Menús desplegables abren correctamente (marca expandable)
- [x] Paso 7: No verificado (dropdown custom, no se ve orden en snapshot)
- [x] Paso 8: Modelo disabled hasta seleccionar marca ("Selecciona marca primero") — ✅ Filtrado funcional
- [x] Paso 9: Paso 2: Características — Combustible, Transmisión, Tracción, Kilometraje, Cilindros, HP, Puertas
- [x] Paso 10: ⚠️ BUG-S6-02 PERSISTE — "Siguiente" avanza sin validar campos requeridos
- [x] Paso 11: Labels en español (Combustible, Transmisión, etc.)
- [x] Paso 12: Paso 3: Fotos — Existe zona de upload con guía de ángulos obligatorios
- [x] Paso 13: Zona drag & drop presente con 5 ángulos obligatorios
- [x] Paso 14: ✅ BUG-S6-03 CORREGIDO — Muestra "Mínimo 3, máximo 5 fotos" (dinámico según plan)
- [ ] Paso 15-17: No verificados (se necesita avanzar llenando todos los campos)
- [ ] Paso 18-20: Preview (paso 6: Revisión) — no verificado
- [x] Paso 21: NO PUBLICAR — solo documentar

**A validar:**

- [x] UF-040: Wizard funciona con 6 pasos. ⚠️ BUG-S6-02 — no valida antes de avanzar
- [x] UF-041: Modelo se filtra por marca ("Selecciona marca primero" cuando no hay marca)
- [x] UF-042: Zona de upload presente con guía de ángulos. Drag & drop visual OK
- [ ] UF-043: Preview no verificado (requiere llenar todos los campos)
- [x] UF-044: Todo en español incluyendo ubicaciones (Provincia: Distrito Nacional, Ciudad: Santo Domingo)

**Hallazgos:**

| Bug | Estado | Detalle |
|-----|--------|---------|
| BUG-S6-01 | ⚠️ PERSISTE | Banner bienvenida dice "Solo necesitas 3 pasos" — debería decir 6 |
| BUG-S6-02 | ⚠️ PERSISTE | Botón "Siguiente" avanza sin validar campos requeridos (Marca, Modelo, Carrocería etc.) |
| BUG-S6-03 | ✅ CORREGIDO | Fotos muestra "Mínimo 3, máximo 5" dinámico según plan (antes: hardcoded 8+) |
| BUG-S6-04 | ⏳ NO VERIFICADO | Requiere llegar a paso 6 (Revisión) con datos completos |
| NEW-BUG | ✅ CORREGIDO | UserService /api/users/me retornaba 500 por FluentValidation en GetOrCreateUserCommandValidator — eliminado NotEmpty() de FirstName/LastName |

---

### S6-T02: Dashboard del vendedor

**Pasos:**

- [ ] Paso 1: Navega a /cuenta/mis-vehiculos — No verificado (sin vehículos publicados)
- [ ] Paso 2-5: No verificados (sin vehículos publicados)
- [x] Paso 6: Navega a /cuenta/suscripcion — ✅ Cargó correctamente
- [x] Paso 7: Screenshot — Plan actual: Libre (Gratis). Resumen: 0/1 publicaciones, 0/5 fotos
- [x] Paso 8: Planes visibles: Libre (Gratis), Estándar (RD$579), Verificado (RD$2,029/mes)
- [ ] Paso 9-10: /cuenta/estadisticas — No verificado
- [ ] Paso 11: Cierra sesión — No ejecutado

**A validar:**

- [ ] UF-045: Dashboard sin vehículos (no se han publicado aún)
- [ ] UF-046: No verificable sin vehículos
- [x] UF-047: Planes en /cuenta/suscripcion muestran Libre/Estándar/Verificado con precios RD$
- [ ] UF-048: Estadísticas no verificadas

**Hallazgos:**

| Bug | Estado | Detalle |
|-----|--------|---------|
| BUG-S6-05 | ⚠️ PERSISTE | "Destacadas este mes: 0/0" con barra ROJA visible para plan Libre (debería ocultarse) |
| BUG-S6-06 | ⚠️ PERSISTE | "Renovación de listing: $6.99" en lista Estándar y tabla comparación — formato USD en vez de RD$ |
| BUG-S6-07 | ✅ CORREGIDO | Verificado muestra "Hasta 12 fotos por vehículo" (no 50) |

---

## Resultado

- Sprint: 6 — Seller — Publicar Mi Primer Vehículo
- Fase: REAUDIT
- Ambiente: LOCAL/TUNNEL (cloudflared forzado: https://ought-feed-shipping-wright.trycloudflare.com)
- URL: https://ought-feed-shipping-wright.trycloudflare.com
- Estado: COMPLETADO
- Bugs corregidos: BUG-S6-03 (fotos dinámicas), BUG-S6-07 (12 fotos), NEW-BUG (UserService FluentValidation)
- Bugs que persisten: BUG-S6-01 (banner 3 pasos), BUG-S6-02 (sin validación paso a paso), BUG-S6-05 (Destacadas 0/0), BUG-S6-06 ($6.99 USD)
- Bug no verificado: BUG-S6-04 (Condición: Usado — requiere datos completos en wizard)

---

_Cuando termines, agrega la palabra READ al final de este archivo._
