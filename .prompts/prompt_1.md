# AUDITORÍA — Sprint 9: Detalle de Vehículo — La Página Más Importante

**Fecha:** 2026-03-30 22:30:20
**Fase:** AUDIT
**Ambiente:** LOCAL/TUNNEL (cloudflared forzado: https://numerous-neck-favorite-equity.trycloudflare.com)
**Usuario:** Guest + Buyer
**URL Base:** https://numerous-neck-favorite-equity.trycloudflare.com

## Ambiente Local (HTTPS público via cloudflared tunnel)

> Auditoría corriendo contra **https://numerous-neck-favorite-equity.trycloudflare.com** (cloudflared tunnel → Caddy → servicios).
> Asegúrate de que la infra esté levantada: `docker compose up -d`
> Frontend: `cd frontend/web-next && pnpm dev`
> Tunnel: `docker compose --profile tunnel up -d cloudflared`
> Caddy redirige: `/api/*` → Gateway, `/*` → Next.js (host:3000)

| Servicio                | URL                                                           |
| ----------------------- | ------------------------------------------------------------- |
| Frontend (tunnel)       | https://numerous-neck-favorite-equity.trycloudflare.com       |
| API (tunnel)            | https://numerous-neck-favorite-equity.trycloudflare.com/api/* |
| Auth Swagger (local)    | http://localhost:15001/swagger                                |
| Gateway Swagger (local) | http://localhost:18443/swagger                                |

## Instrucciones

Ejecuta TODA la auditoría con las herramientas MCP del browser (`mcp_aisquare-play_browser_*`).
NO uses scripts shell — usa `mcp_aisquare-play_browser_*`. Scripts solo para upload/download de fotos vía MediaService.

⚠️ **AMBIENTE LOCAL:** Todas las URLs apuntan a `https://numerous-neck-favorite-equity.trycloudflare.com` en vez de producción.
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

### S9-T01: Página de detalle completa como guest

**Pasos:**

- [x] Paso 1: Navega a {BASE_URL}/vehiculos y selecciona un vehículo con múltiples fotos
- [x] Paso 2: Toma screenshot completo de la página de detalle
- [x] Paso 3: Galería: ¿funciona el carrusel? ¿Puedo hacer clic para agrandar?
- [x] Paso 4: ¿La foto principal es de buena calidad?
- [x] Paso 5: Info principal: ¿precio, año, km, ubicación, combustible, transmisión?
- [x] Paso 6: ¿Todo en español y con formato correcto?
- [x] Paso 7: Tabs/secciones: ¿Descripción, Especificaciones, Ubicación?
- [x] Paso 8: ¿La descripción del vendedor es legible?
- [x] Paso 9: Contacto: ¿hay botón prominente de contactar?
- [x] Paso 10: ¿Hay calculadora de financiamiento?
- [x] Paso 11: ¿Hay OKLA Score o evaluación de precio?
- [x] Paso 12: Compartir: ¿hay botones de compartir en WhatsApp, Facebook?
- [x] Paso 13: Similares: ¿hay sección de vehículos similares abajo?
- [x] Paso 14: ¿El breadcrumb funciona? (Home > Vehículos > Toyota Corolla)
- [x] Paso 15: Agrega `READ` al final de este archivo .prompts/prompt_1.md y luego ejecuta el prompt `.prompts/AGENT_LOOP_PROMPT.md`

**A validar:**

- [x] UF-065: ¿La galería funciona con navegación entre fotos?
- [x] UF-066: ¿Toda la info del vehículo es completa y en español?
- [x] UF-067: ¿El botón de contacto es prominente y funcional?
- [x] UF-068: ¿Compartir por WhatsApp funciona?
- [x] UF-069: ¿Vehículos similares son relevantes?

**Hallazgos:**

**URL auditada:** https://numerous-neck-favorite-equity.trycloudflare.com/vehiculos/2023-toyota-corolla-af334d33

**✅ FUNCIONA CORRECTAMENTE:**

1. **Galería (UF-065):** Carrusel con 4 imágenes, botones prev/next, contador "1/4", thumbnails clickeables, modo fullscreen (lightbox) con overlay oscuro y navegación. Todas las transiciones fluidas.
2. **Info completa en español (UF-066):**
   - Título: "2023 Toyota Corolla" + trim "LE" + badge "Certificado"
   - Precio: RD$1,250,000 (verde, prominente) + equivalente USD ≈ $20,661
   - Año: 2023, Kilometraje: 28,500 km, Ubicación: Santo Domingo
   - Especificaciones tab: Transmisión (Manual), Combustible (Gasolina), Tracción (2WD), Carrocería (Sedán), Color exterior (Blanco Perla), Color interior (Negro), Puertas (4), Asientos (5)
   - Características tab: Aire acondicionado, Bluetooth, Cámara de reversa, Aros de aleación
   - Todo en español con formato correcto (separadores de miles, RD$)
3. **Contacto (UF-067):** 5 botones prominentes en sidebar derecho:
   - "Chat en vivo" (verde, botón principal)
   - "WhatsApp" (verde outline)
   - "Ver teléfono" → revela 809-555-0100
   - "Chatear con Ana (IA)"
   - "Agendar visita" (link a perfil dealer)
   - Trust signals: "Contacto verificado por OKLA" + mensaje de seguridad
4. **Compartir (UF-068):** Modal "Compartir publicación" con preview del vehículo + 4 opciones: WhatsApp, Facebook, Twitter/X, Más opciones. Funcional.
5. **Vehículos similares (UF-069):** Sección "Vehículos similares" con 2 cards (Hyundai Tucson RD$1,450,000 y Honda Civic RD$1,100,000) + link "Ver todos". Vehicles son del mismo rango de precio — relevantes.
6. **Breadcrumb:** Inicio > Vehículos > 2023 Toyota Corolla. Clickable y funcional — "Vehículos" navega a /vehiculos.
7. **Financiamiento:** Estimado RD$20,833/mes (60 meses) + link "Calcular cuota real →" a /herramientas/calculadora-financiamiento
8. **Descripción:** Legible, texto claro del vendedor
9. **Seller info:** Auto Mateo RD, badge "Dealer", 95% tasa de respuesta, < 1h tiempo de respuesta, link a perfil completo
10. **Tabs:** Descripción, Especificaciones, Características — todos funcionales con contenido

**⚠️ BUGS ENCONTRADOS:**

1. **BUG-S9-01 (Media):** Thumbnail 2 de 4 muestra icono de imagen rota + texto "2023 Toyota" en lugar de la imagen real. Una de las URLs de unsplash no carga correctamente. Severidad: P3.
2. **BUG-S9-02 (UX):** En cards de "Vehículos similares" y en listing (/vehiculos), los vehículos de tipo Dealer muestran badge "PARTICULAR" en lugar de "DEALER" o "PROFESIONAL". La página de detalle sí muestra correctamente "Dealer". Severidad: P2 (confunde al comprador).
3. **BUG-S9-03 (Feature ausente):** No existe "OKLA Score" ni evaluación de precio en la página de detalle. Este feature no está implementado — solo hay el badge "Certificado" genérico. Severidad: P3 (feature futuro).
4. **BUG-S9-04 (UX menor):** Al cargar por primera vez, la sección "Vehículos similares" muestra 6 skeletons en estado "Cargando" por varios segundos antes de mostrar los 2 vehículos reales. Severidad: P4.

---

### CIERRE: Ejecutar loop del agente

**Pasos:**

- [x] Paso 1: Agrega `READ` al final de este archivo y luego ejecuta el prompt `.prompts/AGENT_LOOP_PROMPT.md`

**A validar:**

- [x] ¿Se agregó `READ` al final del archivo y luego se ejecutó `.prompts/AGENT_LOOP_PROMPT.md` como último paso?

**Hallazgos:**
Auditoría Sprint 9 completada exitosamente. 10/15 features 100% funcionales. 4 bugs documentados (P2-P4). Detalle de vehículo es una página sólida y profesional.

---

## Resultado

- Sprint: 9 — Detalle de Vehículo — La Página Más Importante
- Fase: AUDIT
- Estado: ✅ COMPLETADO
- Fecha finalización: 2026-03-31
- Bugs encontrados: 4 (1x P2, 1x P3-media, 1x P3-feature, 1x P4)
- Features validados: UF-065 ✅, UF-066 ✅, UF-067 ✅, UF-068 ✅, UF-069 ✅

READ
- Ambiente: LOCAL/TUNNEL (cloudflared forzado: https://numerous-neck-favorite-equity.trycloudflare.com)
- URL: https://numerous-neck-favorite-equity.trycloudflare.com
- Estado: EN PROGRESO
- Bugs encontrados: _(completar)_

---

_Cuando termines las tareas, agrega la palabra READ al final de este archivo y luego ejecuta la última tarea: `.prompts/AGENT_LOOP_PROMPT.md`._
