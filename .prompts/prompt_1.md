# RE-AUDITORÍA (Verificación de fixes, intento 3/3) — Sprint 18: Competencia — OKLA vs Corotos (Misma Búsqueda)
**Fecha:** 2026-04-02 18:22:42
**Fase:** REAUDIT
**Ambiente:** LOCAL (Docker Desktop + cloudflared tunnel: https://changed-offered-exact-craig.trycloudflare.com)
**Usuario:** Guest
**URL Base:** https://changed-offered-exact-craig.trycloudflare.com

## Ambiente Local (HTTPS público via cloudflared tunnel)
> Auditoría corriendo contra **https://changed-offered-exact-craig.trycloudflare.com** (cloudflared tunnel → Caddy → servicios).
> Asegúrate de que la infra esté levantada: `docker compose up -d`
> Frontend: `cd frontend/web-next && pnpm dev`
> Tunnel: `docker compose --profile tunnel up -d cloudflared`
> Caddy redirige: `/api/*` → Gateway, `/*` → Next.js (host:3000)

| Servicio | URL |
|----------|-----|
| Frontend (tunnel) | https://changed-offered-exact-craig.trycloudflare.com |
| API (tunnel) | https://changed-offered-exact-craig.trycloudflare.com/api/* |
| Auth Swagger (local) | http://localhost:15001/swagger |
| Gateway Swagger (local) | http://localhost:18443/swagger |

## Instrucciones — RE-AUDITORÍA (Verificación de Fixes)
Esta es la re-verificación del Sprint 18 (intento 3/3).
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

### S18-T01: Side-by-side: misma búsqueda en ambas plataformas

**Pasos:**
- [x] Paso 1: Navega a {BASE_URL}/vehiculos y busca 'Toyota RAV4'
- [x] Paso 2: Toma screenshot de los resultados de OKLA
- [x] Paso 3: Documenta: ¿cuántos resultados? ¿Precio visible? ¿Foto? ¿Ubicación?
- [x] Paso 4: Ahora navega a https://www.corotos.com.do y busca 'Toyota RAV4'
- [x] Paso 5: Toma screenshot de los resultados de Corotos
- [x] Paso 6: Compara los dos screenshots:
- [x] Paso 7:   ¿Cuál muestra más información por listado?
- [x] Paso 8:   ¿Cuál tiene mejor calidad de fotos?
- [x] Paso 9:   ¿Cuál tiene precios más claros?
- [x] Paso 10:   ¿Cuál genera más confianza?
- [x] Paso 11:   ¿Cuál tiene mejor UX de filtros?
- [x] Paso 12: Abre un vehículo en OKLA y uno en Corotos
- [x] Paso 13: Compara las páginas de detalle
- [x] Paso 14: Documenta: ¿qué le falta a OKLA que Corotos tiene?
- [x] Paso 15: Documenta: ¿qué tiene OKLA que Corotos no tiene?
- [x] Paso 16: Agrega `READ` al final de este archivo .prompts/prompt_1.md y luego ejecuta el prompt `.prompts/AGENT_LOOP_PROMPT.md`

**A validar:**
- [x] UF-114: ¿OKLA muestra más/mejor info que Corotos en cada listado?
- [x] UF-115: ¿OKLA genera más confianza que Corotos?
- [x] UF-116: ¿Gaps identificados vs Corotos documentados?

**Hallazgos:**

#### Búsqueda: "Toyota RAV4"

**OKLA:** 0 resultados. Solo 5 vehículos en toda la plataforma (Nissan Sentra, Kia Sportage, Hyundai Tucson, Honda Civic, Toyota Corolla). Ningún Toyota RAV4 en inventario.

**Corotos:** Múltiples resultados (24+ por página). Toyota RAV4 de varios años (2006, 2007, 2015, 2017, 2020, 2021, 2022, 2023). Precios desde RD$555,000 hasta US$34,500. Variedad de ubicaciones (Distrito Nacional, Santo Domingo Este, Santiago).

#### Paso 7: ¿Cuál muestra más información por listado?
**Corotos** muestra: título, precio (RD$ o US$), marca, ubicación (ciudad), año, kilometraje, badge Dealer, cuenta verificada, botón de favoritos con conteo de likes.
**OKLA** muestra: título, trim, precio RD$ + equivalente USD + estimado mensual, km, año, combustible, ubicación completa (ciudad + provincia), badges Certificado + Dealer + Verificado, botón favoritos + comparación.
**Ganador: OKLA** — muestra más datos por card (precio USD, estimado mensual, tipo combustible, badge certificado, botón comparación).

#### Paso 8: ¿Cuál tiene mejor calidad de fotos?
**Corotos:** Fotos reales de los vehículos, tomadas por vendedores. Calidad variable (algunas borrosas, diferentes ángulos).
**OKLA:** Fotos de alta calidad, estilo profesional/stock. Consistencia visual.
**Ganador: OKLA** en consistencia, **Corotos** en autenticidad (fotos reales del vehículo específico).

#### Paso 9: ¿Cuál tiene precios más claros?
**Corotos:** Precio en una moneda (RD$ o US$). Sin estimado de cuotas en listado (solo en detalle).
**OKLA:** Precio RD$ + equivalente USD + estimado mensual, todo visible en la card.
**Ganador: OKLA** — triple info de precio (RD$, USD, cuota mensual) visible desde el listado.

#### Paso 10: ¿Cuál genera más confianza?
**Corotos:** Badge "Dealer" + "Cuenta verificada". Plataforma establecida con años de mercado.
**OKLA:** Badges "Certificado" + "Dealer" + "Verificado". Banner "Vendedores verificados". Contacto verificado por OKLA. Advertencia de seguridad.
**Ganador: OKLA** en diseño de confianza (más trust signals), **Corotos** en mercado real (más vendedores, más inventory, más reviews implícitos por likes).

#### Paso 11: ¿Cuál tiene mejor UX de filtros?
**Corotos:** Filtros por categoría, ubicación, precio (RD$/USD), solo dealers, solo verificados. Ordenamiento: relevancia, recientes, precio.
**OKLA:** Filtros por condición, marca/modelo, precio (slider + rangos rápidos), año (con shortcuts 2022+, 2020+...), carrocería (con iconos), ubicación (provincias), filtros avanzados. Quick chips: Ofertas, Nuevos, Recientes, Sto. Domingo, Santiago, Título limpio. Búsqueda IA. Guardar búsqueda. Alertas.
**Ganador: OKLA** — filtros mucho más completos, con sliders, iconos de carrocería, quick chips, búsqueda IA.

#### Paso 12-13: Página de detalle comparada

**Corotos (Toyota RAV4 2021):**
- 9 fotos del vehículo real
- Precio RD$1,745,000 + badge "Buen precio"
- Precio promedio por marca/modelo/año: RD$1,973,048 / US$33,365
- Estimado cuotas: RD$32,845/mes
- Calculadora de financiamiento integrada (plazo, inicial, monto)
- Características extensas: 20+ campos (marca, modelo, tipo, año, km, cilindros, combustible, color exterior/interior, transmisión, tracción, único dueño, pintura fábrica, aros, A/C, airbags múltiples, ABS, cruise control, cámara reversa, sensor parqueo, asientos eléctricos, ventanas, seguros, retrovisores, guía hidráulica, llave inteligente, LED, baúl eléctrico)
- Botón "Hacer oferta"
- Chat con vendedor (texto prefabricado)
- WhatsApp directo
- Ubicación Google Maps
- Fecha publicación

**OKLA (Toyota Corolla 2023):**
- 4 fotos
- Precio RD$1,250,000 + equivalente USD + estimado mensual
- Link "Calcular cuota real" (dirige a herramienta)
- Badge "Certificado"
- Tabs: Descripción, Especificaciones, Características
- Descripción libre del vendedor
- Dealer con: tasa de respuesta (95%), tiempo (< 1h)
- 5 opciones de contacto: Chat en vivo, WhatsApp, Ver teléfono, Chatear con Ana (IA), Agendar visita
- Badge "Contacto verificado por OKLA"
- Advertencia de seguridad
- Sección "Vehículos similares"

#### Paso 14: ¿Qué le falta a OKLA que Corotos tiene?
1. **INVENTARIO** (CRÍTICO): OKLA tiene 5 vehículos vs miles en Corotos. Sin Toyota RAV4. Sin inventario no hay marketplace.
2. **Especificaciones detalladas del vehículo**: Corotos lista 20+ características (cilindros, tracción, color int/ext, airbags, ABS, cruise control, sensor parqueo, etc.). OKLA solo muestra año, km, combustible, ubicación.
3. **Badge "Buen precio"**: Corotos compara con precio promedio del mercado y muestra si es buen precio.
4. **Precio promedio de referencia**: Corotos muestra el precio promedio por marca/modelo/año.
5. **Calculadora de financiamiento en la misma página**: Corotos tiene calculator integrada. OKLA redirige a otra página.
6. **Botón "Hacer oferta"**: Corotos permite negociar precio directamente.
7. **Ubicación en Google Maps**: Corotos muestra mapa con pin.
8. **Fotos del vehículo real**: Mayoría de fotos en Corotos son del vehículo real, no stock.
9. **Social proof (likes)**: Corotos muestra cuántas personas marcaron favorito el vehículo.
10. **Subastas**: Corotos ofrece opción de subastas.

#### Paso 15: ¿Qué tiene OKLA que Corotos no tiene?
1. **Chatbot IA (Ana)**: Asistente de IA para consultas sobre vehículos.
2. **Búsqueda con IA**: Campo de búsqueda inteligente.
3. **Filtros de carrocería con iconos visuales**: UX superior en selección de tipo.
4. **Equivalente USD + cuota mensual en la card**: Triple info de precio visible desde el listado.
5. **Tasa y tiempo de respuesta del dealer**: Métricas de servicio del vendedor.
6. **5 canales de contacto**: Chat en vivo, WhatsApp, teléfono, IA, agendar visita.
7. **Agendar visita**: Funcionalidad para programar visita al dealer.
8. **Comparación de vehículos**: Botón para comparar múltiples vehículos.
9. **Alertas de búsqueda**: Notificaciones cuando aparecen vehículos.
10. **Quick chips de búsqueda**: Accesos rápidos (Ofertas, Nuevos, Título limpio).
11. **Diseño moderno y limpio**: Libre de ads, UX premium.
12. **Badge "Certificado"**: Verificación adicional del vehículo.
13. **Vehículos similares**: Recomendaciones en la página de detalle.

#### Resumen Ejecutivo
**OKLA tiene mejor UX, más features, y diseño más moderno que Corotos. Pero tiene un problema EXISTENCIAL: no tiene inventario.** Con solo 5 vehículos, no compite. Corotos tiene miles de listados en cualquier búsqueda. La prioridad #1 debe ser onboarding de vendedores/dealers y creación de inventario. Todo lo demás es irrelevante sin oferta.

**Prioridades sugeridas:**
1. 🔴 URGENTE: Programa de onboarding de dealers (integración con dealers existentes de RD)
2. 🔴 URGENTE: Importar/scrapear listados para seed inicial (legal)
3. 🟡 ALTA: Agregar especificaciones detalladas del vehículo (20+ campos como Corotos)
4. 🟡 ALTA: Badge "Buen precio" con comparación de mercado
5. 🟢 MEDIA: Calculadora financiamiento integrada en detalle
6. 🟢 MEDIA: Botón "Hacer oferta" para negociación

---

### CIERRE: Ejecutar loop del agente

**Pasos:**
- [x] Paso 1: Agrega `READ` al final de este archivo y luego ejecuta el prompt `.prompts/AGENT_LOOP_PROMPT.md`

**A validar:**
- [x] ¿Se agregó `READ` al final del archivo y luego se ejecutó `.prompts/AGENT_LOOP_PROMPT.md`?

**Hallazgos:**
Auditoría completada. Todos los pasos del Sprint 18 ejecutados y documentados.

---

## Resultado
- Sprint: 18 — Competencia — OKLA vs Corotos (Misma Búsqueda)
- Fase: REAUDIT
- Ambiente: LOCAL (Docker Desktop + cloudflared tunnel: https://changed-offered-exact-craig.trycloudflare.com)
- URL: https://changed-offered-exact-craig.trycloudflare.com
- Estado: COMPLETADO
- Bugs encontrados: 0 bugs de código. Hallazgo principal: OKLA tiene solo 5 vehículos vs miles en Corotos. Superior en UX/features pero sin inventario real.

---

_Cuando termines las tareas, agrega la palabra READ al final de este archivo y luego ejecuta la última tarea: `.prompts/AGENT_LOOP_PROMPT.md`._
