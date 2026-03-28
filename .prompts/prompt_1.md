# AUDITORÍA — Sprint 2: Comprador Anónimo — Buscando mi Próximo Carro
**Fecha:** 2026-03-28 13:07:08
**Fase:** AUDIT
**Ambiente:** LOCAL/TUNNEL (cloudflared forzado: https://ought-feed-shipping-wright.trycloudflare.com)
**Usuario:** Guest (sin login)
**URL Base:** https://ought-feed-shipping-wright.trycloudflare.com

## Ambiente Local (HTTPS público via cloudflared tunnel)
> Auditoría corriendo contra **https://ought-feed-shipping-wright.trycloudflare.com** (cloudflared tunnel → Caddy → servicios).
> Asegúrate de que la infra esté levantada: `docker compose up -d`
> Frontend: `cd frontend/web-next && pnpm dev`
> Tunnel: `docker compose --profile tunnel up -d cloudflared`
> Caddy redirige: `/api/*` → Gateway, `/*` → Next.js (host:3000)

| Servicio | URL |
|----------|-----|
| Frontend (tunnel) | https://ought-feed-shipping-wright.trycloudflare.com |
| API (tunnel) | https://ought-feed-shipping-wright.trycloudflare.com/api/* |
| Auth Swagger (local) | http://localhost:15001/swagger |
| Gateway Swagger (local) | http://localhost:18443/swagger |

## Instrucciones
Ejecuta TODA la auditoría con las herramientas MCP del browser (`mcp_aisquare-play_browser_*`).
NO uses scripts shell — usa `mcp_aisquare-play_browser_*`. Scripts solo para upload/download de fotos vía MediaService.

⚠️ **AMBIENTE LOCAL:** Todas las URLs apuntan a `https://ought-feed-shipping-wright.trycloudflare.com` en vez de producción.
Verifica que Caddy + infra + cloudflared tunnel estén corriendo antes de empezar.
Diferencias esperadas vs producción: ver `docs/HTTPS-LOCAL-SETUP.md`.

Para cada tarea:
1. Navega con `mcp_aisquare-play_browser_navigate` a la URL indicada
2. Toma screenshot cuando se indique
3. Documenta bugs y discrepancias en la sección 'Hallazgos'
4. Marca la tarea como completada: `- [ ]` → `- [x]`
5. Al terminar TODAS las tareas, agrega `READ` al final


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

### S2-T01: Buscar y filtrar vehículos

**Pasos:**
- [x] Paso 1: Navega a {BASE_URL}/vehiculos
- [x] Paso 2: Toma screenshot — ¿cómo se ve la página de resultados?
- [x] Paso 3: ¿Hay filtros visibles? (marca, modelo, precio, ubicación, año, combustible)
- [x] Paso 4: Filtra por: SUV, precio < 2,000,000 RD$
- [x] Paso 5: Toma screenshot de los resultados filtrados
- [x] Paso 6: ¿Los resultados hacen sentido? ¿Todos son SUVs y menores de 2M?
- [x] Paso 7: Ordena por 'Más recientes' — ¿cambia el orden?
- [x] Paso 8: Ordena por 'Menor precio' — ¿el primer resultado es el más barato?
- [x] Paso 9: Busca en la barra de búsqueda: 'Toyota Corolla 2020'
- [x] Paso 10: Toma screenshot — ¿los resultados son relevantes?
- [x] Paso 11: ¿Hay paginación? Si hay más de 20 resultados, ¿puedo ir a página 2?
- [x] Paso 12: ¿Las cards muestran: foto, precio, ubicación, año, kilometraje?
- [x] Paso 13: ¿Todo está en español? Busca 'gasoline', 'diesel', 'electric' en los resultados

**A validar:**
- [x] UF-009: ⚠️ PARCIAL — Filtros de click funcionan (carrocería, marca). Spinbutton de precio manual no responde a input directo. BUG-003 impide búsqueda por teclado.
- [x] UF-010: ⚠️ PARCIAL — Búsqueda via teclado rota (Enter limpia el texto, BUG-003). Funciona vía URL params directos.
- [x] UF-011: ✅ PASA — Paginación existe y renderiza. Dataset demo tiene solo 10 vehículos (1 página).
- [x] UF-012: ⚠️ PARCIAL — Cards muestran foto/precio/ubicación/año/km/combustible. **Falta transmisión** en card (solo aparece en specs).
- [x] UF-013: ✅ PASA — Ordenamiento "Más recientes" y "Menor precio" funcionan correctamente. Orden ascendente verificado: 2.45M → 2.85M → 3.2M.

**Hallazgos:**
| Bug ID | Severidad | Descripción |
|--------|-----------|-------------|
| BUG-001 | Media | Título de página duplica "OKLA": "Vehículos en Venta \| OKLA \| **OKLA**" |
| BUG-002 | Alta | `<button>` anidado dentro de `<button>` en `RecentSearchesDropdown` → React hydration error en consola: "In HTML, `<button>` cannot be a descendant of `<button>`" |
| BUG-003 | Alta | Presionar Enter en barra de búsqueda limpia el texto en vez de buscar. Causado por BUG-002: el outer `<button>` del dropdown intercepta el evento |
| BUG-004 | Media | URL persiste estado de sesión previa (`?q=Toyota` de sesión anterior del browser) al navegar a `/vehiculos` |
| BUG-005 | Baja | `/api/advertising/sponsored` retorna 404 — módulo no implementado |
| BUG-006 | Baja | `/api/analytics/track` retorna 403 para usuarios anónimos — debería 200/204 o silenciarse |
| BUG-007 | Baja | `/api/auth/refresh-token` POST retorna 400 — cookies de sesión vencidas |
| BUG-008 | Baja | `/api/configurations/category/general?environment=Development` retorna 401 — requiere auth pero se llama sin token |

---

### S2-T02: Ver detalle de un vehículo

**Pasos:**
- [x] Paso 1: Desde los resultados, haz clic en el primer vehículo
- [x] Paso 2: Toma screenshot de la página de detalle completa
- [x] Paso 3: ¿La galería de fotos funciona? ¿Puedo navegar entre fotos?
- [x] Paso 4: ¿Veo: precio, ubicación, año, kilometraje, combustible, transmisión?
- [x] Paso 5: ¿Hay descripción del vendedor?
- [x] Paso 6: ¿Hay botón de contactar al vendedor? (debería pedir login)
- [x] Paso 7: Haz clic en 'Contactar' → ¿me pide que inicie sesión? Toma screenshot
- [x] Paso 8: Scroll abajo — ¿hay 'Vehículos similares'?
- [x] Paso 9: ¿Hay botón de compartir? ¿Funciona?
- [x] Paso 10: ¿Hay botón de favoritos (corazón)? Haz clic → ¿pide login?

**A validar:**
- [x] UF-014: ⚠️ PARCIAL — Galería carga pero solo hay 1 foto (1/1). Navegación de galería no testeable por limitación de datos demo.
- [x] UF-015: ⚠️ PARCIAL — La mayoría de campos presentes PERO: BUG-009 (Combustible "Eléctrico" en specs pero "Gasolina" en card) y BUG-010 (colores en inglés).
- [x] UF-016: ✅ PASA — "Chat en vivo" → popup "Inicia sesión para chatear" con botones "Iniciar sesión" y "Crear cuenta gratis". Correcto.
- [x] UF-017: ❌ FALLA — No hay sección "Vehículos similares" orgánica. Solo secciones de publicidad pagada (BUG-013).

**Hallazgos:**
| Bug ID | Severidad | Descripción |
|--------|-----------|-------------|
| BUG-009 | **Crítica** | Inconsistencia de datos: `Combustible` muestra **"Eléctrico"** en tab Especificaciones del 2023 Toyota Camry, pero la card de listado y el modal de compartir muestran correctamente **"Gasolina"**. Error en mapeo de atributos de specs. |
| BUG-010 | Media | Colores del vehículo en **inglés**: "Midnight Black", "Black" — deben mostrarse en español. |
| BUG-011 | Media | Sección "Vehículos Destacados" muestra **el mismo vehículo que se está viendo** como publicidad pagada (auto-referencia). |
| BUG-012 | Media | URLs duplicadas para el mismo vehículo: `/vehiculos/2023-toyota-camry-b1000001` Y `/vehiculos/toyota-camry-le-2023-b1000001` — problema de canonicalización de slug. |
| BUG-013 | Alta | No existe sección "Vehículos similares" orgánica. El comprador anónimo no tiene recomendaciones naturales — solo publicidad pagada, afectando la retención. |

**Observaciones positivas:**
- ✅ UF-016: Login gate en "Chat en vivo" funciona correctamente
- ✅ UF-018 (extra): "Guardar en favoritos" muestra modal de upsell con propuesta de valor excelente: previsualización del vehículo + beneficios (alertas de precio, chat directo, ofertas exclusivas) + CTAs claros ("Crear cuenta gratis" / "Ya tengo cuenta — Iniciar sesión"). **Buen UX de conversión.**
- ✅ Compartir funciona: WhatsApp, Facebook, Twitter/X, Más opciones, URL copiable con dominio de producción `okla.com.do`

---

## Resultado
- Sprint: 2 — Comprador Anónimo — Buscando mi Próximo Carro
- Fase: AUDIT
- Ambiente: LOCAL/TUNNEL (cloudflared forzado: https://ought-feed-shipping-wright.trycloudflare.com)
- URL: https://ought-feed-shipping-wright.trycloudflare.com
- Estado: **COMPLETADO**
- Bugs encontrados: **13 bugs** (2 Alta, 4 Media, 5 Baja, 1 Crítica, 1 Alta UF)
  - BUG-001 (Media): Título duplicado "OKLA | OKLA"
  - BUG-002 (Alta): `<button>` anidado en `<button>` en RecentSearchesDropdown → hydration error
  - BUG-003 (Alta): Enter en búsqueda limpia el texto en vez de buscar
  - BUG-004 (Media): URL persiste estado de sesión previa
  - BUG-005 (Baja): `/api/advertising/sponsored` 404
  - BUG-006 (Baja): `/api/analytics/track` 403 para anónimos
  - BUG-007 (Baja): `/api/auth/refresh-token` 400
  - BUG-008 (Baja): `/api/configurations/category/general` 401
  - BUG-009 (Crítica): Combustible "Eléctrico" en specs vs "Gasolina" en card — inconsistencia de datos
  - BUG-010 (Media): Colores en inglés en detalle de vehículo
  - BUG-011 (Media): Sección Destacados muestra el mismo vehículo
  - BUG-012 (Media): URLs duplicadas para el mismo vehículo
  - BUG-013 (Alta): No hay sección "Vehículos similares" orgánica

---

_Cuando termines, agrega la palabra READ al final de este archivo._

READ
