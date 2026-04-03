# AUDITORÍA — Sprint 19: Checkout — Pagar un Plan de Suscripción
**Fecha:** 2026-04-02 19:50:13
**Fase:** AUDIT
**Ambiente:** LOCAL (Docker Desktop + cloudflared tunnel: https://changed-offered-exact-craig.trycloudflare.com)
**Usuario:** Seller (gmoreno@okla.com.do / $Gregory1)
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

## Instrucciones
Ejecuta TODA la auditoría con las herramientas MCP del browser (`mcp_aisquare-play_browser_*`).
NO uses scripts shell — usa `mcp_aisquare-play_browser_*`. Scripts solo para upload/download de fotos vía MediaService.

⚠️ **AMBIENTE LOCAL:** Todas las URLs apuntan a `https://changed-offered-exact-craig.trycloudflare.com` en vez de producción.
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

### S19-T01: Flujo de checkout y pago

**Pasos:**
- [x] Paso 1: TROUBLESHOOTING: Verifica billingservice corriendo si usas perfil business: docker compose --profile business ps billingservice — billingservice fue OOM killed, se reinició postgres (tenía permisos corruptos en pg_filenode.map), luego pgbouncer + authservice + billingservice + gateway. Todo healthy.
- [x] Paso 2: Login como seller (gmoreno@okla.com.do / $Gregory1) — Sesión ya estaba activa del browser anterior. Verificado: sidebar muestra Gregory / gmoreno@okla.com.do.
- [x] Paso 3: Navega a {BASE_URL}/cuenta/suscripcion — Página cargó correctamente en /cuenta/suscripcion. Título: "Mi Suscripción".
- [x] Paso 4: Toma screenshot — ¿veo mi plan actual y opciones de upgrade? — SÍ. Muestra badge "Sin Plan" y resumen "0/0 publicaciones, 0/0 fotos". 3 planes visibles: Libre (Gratis), Estándar (RD$579/publicación, MÁS POPULAR), Verificado (RD$2,029/mes). Tabla comparativa completa y FAQ incluidos.
- [x] Paso 5: Haz clic en 'Upgrade a Estándar' (o plan superior) — Botón "Mejorar a Estándar" clickeado → navega a /cuenta/upgrade?plan=estandar&type=seller
- [x] Paso 6: Toma screenshot de la página de checkout — Screenshots tomados (página completa, comparación detallada, período facturación, método de pago, total y footer de seguridad).
- [x] Paso 7: ¿Veo resumen del pedido? (plan, precio, período) — SÍ. Muestra plan actual "TU PLAN" (Libre) vs "SELECCIONADO" (Estándar). Precio: RD$579/listing. Período: Mensual o Anual. Total: RD$579/listing ≈ $9.98 USD. Tabla comparativa detallada incluida.
- [x] Paso 8: ¿Puedo elegir método de pago? (Tarjeta/PayPal/Azul) — SÍ. 3 opciones: PayPal (Recomendado, "Paga con tu cuenta PayPal o tarjeta"), Fygaro ("Pago recurrente automático"), Azul ("Tarjeta de crédito/débito RD"). PayPal es el preseleccionado.
- [x] Paso 9: ¿El precio es claro con ITBIS incluido? — **BUG**: NO menciona ITBIS en ningún lugar. El precio RD$579 no aclara si incluye o excluye el 18% ITBIS. Esto es un problema legal/regulatorio en RD. Debe decir "ITBIS incluido" o desglosar "Subtotal + ITBIS = Total".
- [x] Paso 10: ¿Hay selección de moneda (RD$/USD)? — **BUG**: NO hay selector de moneda. El total muestra "≈ $9.98 USD (tasa de cambio aproximada)" como referencia, pero no hay opción de pagar en USD. Para un mercado donde muchos vehículos se cotizan en USD, debería ofrecer la opción.
- [x] Paso 11: NO COMPLETAR EL PAGO — solo documentar todo el flujo — Documentado. No se completó ningún pago.
- [x] Paso 12: ¿Hay indicador de seguridad? (candado, logos de procesadores) — SÍ, múltiples: (1) "Conexión segura con cifrado SSL de 256 bits" con icono candado, (2) "Pago 100% seguro", (3) "Datos encriptados", (4) "Protección al comprador", (5) Logos PayPal/FYGARO/AZUL en footer, (6) "Tus datos financieros nunca son almacenados en nuestros servidores. Todos los pagos son procesados de forma segura por proveedores certificados PCI DSS."
- [x] Paso 13: ¿El formulario de tarjeta se ve seguro? — No hay formulario de tarjeta inline — los 3 procesadores (PayPal, Fygaro, Azul) manejan el ingreso de tarjeta externamente. Esto es CORRECTO y más seguro (PCI DSS compliant). Las señales de confianza son adecuadas.
- [x] Paso 14: Toma screenshot de cada paso del checkout — Screenshots tomados: (1) Página suscripción completa, (2) Checkout top con planes, (3) Tabla comparativa + período facturación, (4) Método de pago + total + footer seguridad, (5) Footer con PCI DSS.
- [x] Paso 15: Cierra sesión — Sesión cerrada. Redirigió a /login con callback a la página de upgrade.
- [x] Paso 16: Agrega `READ` al final de este archivo .prompts/prompt_1.md y luego ejecuta el prompt `.prompts/AGENT_LOOP_PROMPT.md`

**A validar:**
- [x] UF-117: ¿El flujo de checkout es claro y profesional? — **PARCIAL**. El flujo es profesional: navegación clara (Suscripción → Upgrade → Checkout), diseño limpio, comparación de planes, período de facturación seleccionable, 3 métodos de pago, señales de confianza abundantes. PERO: (1) No hay link "Suscripción" en el sidebar — el usuario no puede descubrir esta página fácilmente. (2) El usuario muestra "Sin Plan" con "0/0" cuando debería estar en plan Libre por defecto. (3) Botón "Mejorar a Libre" no tiene sentido — debería decir "Plan Actual" o "Activar Plan Libre".
- [x] UF-118: ¿El precio incluye ITBIS y es claro? — **NO**. El precio NO menciona ITBIS (18%). RD$579 sin aclarar si incluye impuesto. Falta desglose fiscal. Conversión USD mostrada como referencia (≈$9.98 USD) pero sin opción de cambio de moneda.
- [x] UF-119: ¿Los métodos de pago son visibles y confiables? — **SÍ**. 3 procesadores bien presentados: PayPal (recomendado), Fygaro (recurrente), Azul (tarjeta RD). Cada uno con su logo/iniciales y descripción clara. PayPal preseleccionado como opción más confiable internacionalmente.
- [x] UF-120: ¿El checkout tiene indicadores de seguridad? — **SÍ, EXCELENTE**. 6 señales de seguridad: SSL 256-bit, Pago 100% seguro, Datos encriptados, Protección al comprador, logos de procesadores, PCI DSS compliance. Sin formulario de tarjeta inline (procesadores externos = más seguro).

**Hallazgos:**

### BUGS ENCONTRADOS

**BUG-S19-001 (ALTA): ITBIS no mencionado en checkout**
- En /cuenta/upgrade, el precio muestra "RD$579" sin indicar si incluye o excluye ITBIS (18%).
- La Ley 253-12 de RD requiere claridad en los precios mostrados al consumidor.
- Fix: Agregar "ITBIS incluido" bajo el precio, o desglosar "Subtotal: RD$490.68 + ITBIS 18%: RD$88.32 = Total: RD$579".

**BUG-S19-002 (MEDIA): Sin selector de moneda RD$/USD**
- No hay opción de pagar en USD. Solo muestra "≈ $9.98 USD" como referencia.
- En el mercado automotriz dominicano donde muchos precios se manejan en USD, debería permitir elegir moneda.
- Fix: Agregar toggle RD$/USD que recalcule el precio total y pase la moneda al procesador de pago.

**BUG-S19-003 (MEDIA): No hay link "Suscripción" en el sidebar de cuenta**
- La página /cuenta/suscripcion existe pero NO aparece en la navegación lateral del dashboard.
- El usuario solo llegaría si conoce la URL directamente.
- Fix: Agregar link "Mi Suscripción" al sidebar bajo "Configuración" o crear sección "Facturación".

**BUG-S19-004 (BAJA): "Sin Plan" con "0/0" publicaciones**
- El usuario muestra "Sin Plan" con "Publicaciones activas: 0/0" y "Fotos por vehículo: 0/0".
- Todo usuario debería estar por defecto en el plan Libre (1 publicación, 5 fotos).
- Fix: Asignar plan Libre automáticamente al registrarse. Mostrar "Plan Libre" en vez de "Sin Plan".

**BUG-S19-005 (BAJA): Botón "Mejorar a Libre" confuso**
- En la tarjeta del plan Libre hay un botón "Mejorar a Libre", lo cual no tiene sentido.
- Si el usuario ya es Libre por defecto, debería decir "Plan Actual" (deshabilitado).
- Si el usuario no tiene plan, debería decir "Activar Plan Libre".

**BUG-S19-006 (BAJA): Pricing anual confuso para Estándar**
- El selector de período muestra "Anual: RD$579 / RD$48/mes" — no es claro qué significa.
- Si anual cuesta RD$48/mes = RD$576/año, ¿por qué también muestra RD$579?
- Fix: Mostrar claramente "RD$576/año (RD$48/mes)" vs "RD$579/mes" para que el descuento sea evidente.

### LO QUE FUNCIONA BIEN
- Diseño profesional y limpio del flujo completo
- 3 planes bien diferenciados con features claros
- Tabla comparativa detallada en página de suscripción Y en checkout
- Período de facturación seleccionable (Mensual/Anual)
- 3 procesadores de pago confiables (PayPal/Fygaro/Azul)
- Señales de seguridad excelentes (SSL, PCI DSS, cifrado)
- Sin formulario de tarjeta inline = más seguro (procesadores externos)
- Checkbox de términos y condiciones obligatorio antes de pagar
- Conversión USD como referencia
- CTA "Ver Planes Dealer" para negocios
- FAQ completo sobre cambios de plan y políticas
- Back navigation ("Volver a planes") funcional

---

### CIERRE: Ejecutar loop del agente

**Pasos:**
- [x] Paso 1: Agrega `READ` al final de este archivo y luego ejecuta el prompt `.prompts/AGENT_LOOP_PROMPT.md`

**A validar:**
- [x] ¿Se agregó `READ` al final del archivo y luego se ejecutó `.prompts/AGENT_LOOP_PROMPT.md`?

**Hallazgos:**
Ciclo de auditoría S19-T01 completado. 6 bugs documentados (1 alta, 2 media, 3 baja). Commit y push realizados.

---

## Resultado
- Sprint: 19 — Checkout — Pagar un Plan de Suscripción
- Fase: AUDIT
- Ambiente: LOCAL (Docker Desktop + cloudflared tunnel: https://changed-offered-exact-craig.trycloudflare.com)
- URL: https://changed-offered-exact-craig.trycloudflare.com
- Estado: COMPLETADO
- Bugs encontrados: 6 (1 alta: ITBIS no mencionado, 2 media: sin selector moneda + sin link sidebar, 3 baja: Sin Plan default, botón Libre confuso, pricing anual confuso)

---

_Cuando termines las tareas, agrega la palabra READ al final de este archivo y luego ejecuta la última tarea: `.prompts/AGENT_LOOP_PROMPT.md`._
