# RE-AUDITORÍA (Verificación de fixes, intento 1/3) — Sprint 3: Visitante — Explorando Cómo Vender en OKLA

**Fecha:** 2026-03-28 15:57:14
**Fase:** REAUDIT
**Ambiente:** LOCAL/TUNNEL (cloudflared forzado: https://ought-feed-shipping-wright.trycloudflare.com)
**Usuario:** Guest (sin login)
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

Esta es la re-verificación del Sprint 3 (intento 1/3).
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

### S3-T01: Explorar página de vender y planes

**Pasos:**

- [x] Paso 1: Navega a {BASE_URL}/vender
- [x] Paso 2: Toma screenshot — ¿qué veo como visitante?
- [x] Paso 3: ¿Hay una explicación clara de cómo funciona vender en OKLA?
- [x] Paso 4: ¿Veo los planes de vendedor? (Libre, Estándar, Verificado)
- [x] Paso 5: ¿Los precios están claros en RD$ y USD?
- [x] Paso 6: ¿Se explica qué incluye cada plan?
- [x] Paso 7: ¿Hay un CTA claro ('Publicar mi vehículo' o similar)?
- [x] Paso 8: Haz clic en el CTA → ¿me lleva a registro/login?
- [x] Paso 9: Navega a {BASE_URL}/dealers
- [x] Paso 10: Toma screenshot — ¿Veo planes para dealers?
- [x] Paso 11: ¿Los planes de dealer son diferentes a los de vendedor particular?
- [x] Paso 12: ¿Hay testimonios? ¿Se ven reales o ficticios?

**A validar:**

- [x] UF-018: ¿La página /vender explica claramente el proceso?
- [x] UF-019: ¿Los planes y precios son claros y consistentes?
- [x] UF-020: ¿El CTA lleva correctamente al registro?
- [x] UF-021: ¿Los planes dealer vs seller son distintos y claros?

**Hallazgos:**

- /vender carga correctamente con proceso en 4 pasos (Crea, Precio, Ofertas, Cierre) ✅
- Planes vendedor: Libre (RD$0), Estándar (RD$579/publicación), Verificado (RD$2,029/mes) ✅
- Verificado muestra "Hasta 3 publicaciones activas" — fix S3-T01 CONFIRMADO ✅
- CTA "Crear cuenta y vender" → /vender/registro ✅
- /dealers: planes LIBRE ($0), VISIBLE (RD$1,682), STARTER (RD$3,422), PRO (RD$5,742) — distintos a vendedor particular ✅
- Testimonios en /dealers: Juan, María y otros dealers reales ✅

---

### S3-T02: Explorar páginas públicas: Legal, FAQ, Contacto

**Pasos:**

- [x] Paso 1: Navega a {BASE_URL}/preguntas-frecuentes (o /faq)
- [x] Paso 2: Toma screenshot — ¿hay preguntas frecuentes útiles?
- [x] Paso 3: Navega a {BASE_URL}/contacto
- [x] Paso 4: Toma screenshot — ¿hay formulario de contacto? ¿Email? ¿Teléfono?
- [x] Paso 5: Navega a {BASE_URL}/privacidad
- [x] Paso 6: ¿Menciona Ley 172-13 de Protección de Datos?
- [x] Paso 7: Navega a {BASE_URL}/terminos
- [x] Paso 8: ¿Menciona jurisdicción RD? ¿Fecha actualizada?
- [x] Paso 9: Navega a {BASE_URL}/nosotros (o /about)
- [x] Paso 10: ¿Hay info sobre OKLA? ¿Equipo? ¿Misión?

**A validar:**

- [x] UF-022: ¿FAQ tiene respuestas útiles?
- [x] UF-023: ¿Contacto tiene datos reales?
- [x] UF-024: ¿Privacidad menciona Ley 172-13?
- [x] UF-025: ¿Términos tienen jurisdicción RD?

**Hallazgos:**
- /preguntas-frecuentes → redirect 301 a /faq — fix S3-T02 CONFIRMADO ✅
- /faq carga con preguntas útiles sobre compra/venta y planes ✅
- /contacto: formulario presente, email soporte@okla.com.do, teléfono +1 (809) 200-6552, WhatsApp, dirección RD ✅
- CTA "Ver preguntas frecuentes" en /contacto → href="/faq" — fix S3-T02 CONFIRMADO ✅
- /privacidad: Ley 172-13 mencionada ✅
- /terminos: jurisdicción República Dominicana, fecha Marzo 2026 (v2026) ✅
- /nosotros: misión, equipo, Santo Domingo RD ✅

---

## Resultado

- Sprint: 3 — Visitante — Explorando Cómo Vender en OKLA
- Fase: REAUDIT
- Ambiente: LOCAL/TUNNEL (cloudflared forzado: https://ought-feed-shipping-wright.trycloudflare.com)
- URL: https://ought-feed-shipping-wright.trycloudflare.com
- Estado: COMPLETADO
- Bugs encontrados: 0 (todos los fixes de FIX phase fueron confirmados)

---

_Cuando termines, agrega la palabra READ al final de este archivo._
