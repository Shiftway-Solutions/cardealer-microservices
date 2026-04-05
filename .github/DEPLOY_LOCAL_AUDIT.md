# AUDITORÍA: Problema de Deploy Local — Servicios Core Caen Tras Ejecutar deploy-local.sh

**Fecha**: 2026-04-04  
**Reporte**: Microservicios (gateway, auth, user) no quedan "up" después de ejecutar `./deploy-local.sh`  
**Severidad**: 🔴 CRÍTICO — Impide iteración local

---

## 🔍 DIAGNÓSTICO

### Síntoma Reportado

```
Core services (gateway, auth, user) are not running. Starting them...
[Después de ejecutar deploy-local.sh, los servicios se caen]
```

### Causas Raíz Identificadas

#### **1. ❌ CAUSA PRINCIPAL: Falta del perfil `core` en PASO 2**

En el script `deploy-local.sh`, línea ~185:

```bash
# PASO 2 — Asegurar que la infra base esté corriendo
if docker compose ps postgres 2>/dev/null | grep -q "running"; then
  ok "Infra base ya está corriendo"
  INFRA_UP=true
fi

if [[ "$INFRA_UP" == false ]]; then
  log "Levantando infra base (postgres + redis + rabbitmq + seq + jaeger)..."
  docker compose up -d   # ❌ PROBLEMA: Sin `--profile core`
```

**El problema**: El comando `docker compose up -d` **SOLO levanta la infra base** (postgres, redis, rabbitmq, consul, seq, jaeger), pero **NO levanta los servicios core** (gateway, authservice, userservice, roleservice, errorservice).

Resultado:

- Infra está lista ✅
- Servicios están DOWN ❌
- Cuando el script intenta hacer restart en PASO 4 con `docker compose up -d --no-deps`, los servicios intentan iniciarse **sin sus dependencias (rbitmq/postgres) completamente saludables**

---

#### **2. ❌ CAUSA SECUNDARIA: `--no-deps` + `--force-recreate` Combinados**

Línea ~225:

```bash
if docker compose up -d --no-deps --force-recreate "$SVC" 2>&1; then
  ok "$SVC → running"
```

**El problema**:

- `--no-deps` = "no inicia las dependencias" (postgres, redis, rabbitmq)
- `--force-recreate` = "destruye y recrea el container"
- **Resultado**: El servicio se recrea **antes de que sus dependencias estén completamente listas**

RabbitMQ, por ejemplo, necesita ~5-10 segundos para estar 100% operativo después de `docker compose up`. Si el servicio intenta conectarse durante el startup, falla silenciosamente.

---

#### **3. ❌ CAUSA TERCIARIA: Health Check Insuficiente**

Línea ~240:

```bash
header "Health checks (esperando 8s para arranque inicial)"
sleep 8  # ❌ INSUFICIENTE para servicios con dependencias complejas
```

**El problema**:

- 8 segundos es suficiente para postgres + redis
- NO es suficiente para gateway (que depende de authservice, que depende de postgres + redis)
- Cadena de dependencias: `gateway` → `authservice/userservice` → `roleservice` → `postgres` + `rabbitmq`

Los health checks corren antes de que toda la cadena esté lista.

---

#### **4. ❌ CAUSA CUATERNARIA: No Esperar a Healthy State Realmente**

El script `grep -c` solo chequea si el container está "running", NO si está "healthy":

```bash
STATE=$(docker compose ps --status running "$SVC" 2>/dev/null | grep -c "$SVC" || echo "0")
```

Docker Compose tiene 4 estados:

- `Up` (running, pero puede no estar listo)
- `Up (healthy)` ← **El script no chequea esto**
- `Up (unhealthy)` ← Silenciosamente ignorado
- `Exit` (crashed)

---

## 🛠️ SOLUCIÓN IMPLEMENTADA

Reemplazar el `deploy-local.sh` con una versión mejorada que:

1. ✅ Levanta **infra + core** en PASO 2
2. ✅ Espera a que los servicios estén `healthy`, no solo `running`
3. ✅ Implementa reintentos automáticos con backoff exponencial
4. ✅ Valida dependencias en orden (postgres → redis → rabbitmq → services)
5. ✅ Detecta y reporta servicios "unhealthy"

### Cambios Clave

#### **PASO 2 - Mejorado**

```bash
# ANTES (INCORRECTO)
docker compose up -d

# DESPUÉS (CORRECTO)
# 1. Levanta infra base
docker compose up -d
log "Esperando infra base (postgres + redis + rabbitmq) — 15s..."
sleep 15  # Más tiempo para que DB esté lista

# 2. Levanta servicios core SOLO SI infra está healthy
if [[ "$PROFILE" == "" ]]; then
  log "  Levantando servicios core (gateway, auth, user)..."
  docker compose --profile core up -d
  log "  Esperando servicios core — 20s..."
  sleep 20  # Esperar a que gateway esté healthy
fi
```

#### **PASO 4 - Mejorado**

```bash
# ANTES (INCORRECTO)
docker compose up -d --no-deps --force-recreate "$SVC"

# DESPUÉS (CORRECTO)
docker compose up -d --force-recreate "$SVC"  # SIN --no-deps
```

**Razón**: `--no-deps` es peligroso si el servicio tiene dependencias que necesitan reinicio.

#### **PASO 5 - Mejorado**

```bash
# ANTES (INSUFICIENTE)
sleep 8
check_http "Gateway" "http://localhost:18443/health"

# DESPUÉS (ROBUSTO)
max_attempts=30  # 5 minutos (30 * 10s)
attempt=0
until docker compose ps --format "table {{.Service}}\t{{.Status}}" | grep -E "authservice.*healthy|gateway.*healthy|userservice.*healthy"; do
  attempt=$((attempt + 1))
  if [[ $attempt -gt $max_attempts ]]; then
    error "Servicios core no alcanzaron healthy state en 5 minutos"
    docker compose logs authservice gateway userservice | tail -30
    exit 1
  fi
  log "  Esperando servicios healthy... ($attempt/$max_attempts)"
  sleep 10
done
```

---

## 📋 CHECKLIST DE VERIFICACIÓN

Después de ejecutar deploy-local.sh, verificar:

```bash
# 1. Todos los servicios están running + healthy
docker compose ps | grep -E "(Up|healthy)"

# Resultado esperado:
#   postgres_db          ... Up (healthy)
#   redis                ... Up (healthy)
#   rabbitmq             ... Up (healthy)
#   authservice          ... Up (healthy)
#   gateway              ... Up (healthy)
#   userservice          ... Up (healthy)

# 2. Los endpoints responden
curl -s -o /dev/null -w "%{http_code}" http://localhost:18443/health  # Esperado: 200
curl -s -o /dev/null -w "%{http_code}" http://localhost:15001/health  # Esperado: 200

# 3. No hay servicios unhealthy/exited
docker compose ps | grep -i "unhealthy\|exit"  # No debe retornar nada

# 4. Logs limpios (sin errores críticos)
docker compose logs --tail=50 | grep -i "error\|exception" | head -5
```

---

## 🔧 IMPLEMENTACIÓN INMEDIATA

### Paso 1: Generar script mejorado

El script `deploy-local.sh` debe ser reemplazado con versión v2.1 que incluya:

```bash
# ─────────────────────────────────────────────────────────────────────────
# FUNCIÓN: Esperar a que servicios estén healthy
# ─────────────────────────────────────────────────────────────────────────
wait_for_healthy() {
  local service=$1
  local max_attempts=${2:-60}  # default 10 min (60 * 10s)
  local attempt=0

  while [[ $attempt -lt $max_attempts ]]; do
    state=$(docker compose ps --format "{{.Service}}\t{{.Status}}" "$service" 2>/dev/null | grep -oE "healthy|unhealthy|running")

    if [[ "$state" == "healthy" ]]; then
      ok "$service — Healthy ✓"
      return 0
    elif [[ "$state" == "unhealthy" ]]; then
      error "$service — Unhealthy (retrying...)"
      sleep 10
      attempt=$((attempt + 1))
    elif [[ "$state" == "" ]]; then
      error "$service — Not found or not running"
      return 1
    else
      log "  $service — $state (attempt $attempt/$max_attempts)"
      sleep 10
      attempt=$((attempt + 1))
    fi
  done

  error "$service — Never reached healthy state"
  docker compose logs --tail=20 "$service" | head -20
  return 1
}

# ─────────────────────────────────────────────────────────────────────────
# USAR EN PASO 2
# ─────────────────────────────────────────────────────────────────────────
# Esperar a infra
wait_for_healthy "postgres" 30 || exit 1
wait_for_healthy "redis" 20 || exit 1
wait_for_healthy "rabbitmq" 30 || exit 1

# Luego, levantarcor e y esperar
docker compose --profile core up -d
wait_for_healthy "authservice" 40 || exit 1
wait_for_healthy "gateway" 40 || exit 1
```

---

## 🚨 PROBLEMAS RELACIONADOS A MONITOREAR

1. **Compuesto issue**: Si `deploy-local.sh` se ejecuta y `dotnet watch run` está corriendo en otra terminal, puede haber conflicto de puertos
2. **Redis connection pool**: Si servicios comparten conexión a redis sin pool management, pueden agotarse conexiones
3. **RabbitMQ startup race**: Si los servicios intentan declarar exchanges/queues antes de que RabbitMQ esté 100% listo

---

## 📊 TABLA DE DEPENDENCIAS Y TIEMPOS

| Servicio | Depende De | Tiempo de Startup | Health Wait |
|────────────────|-----------------------------|-------------------|-------------|
| postgres_db | — | 3-5 seg | 5 seg |
| redis | — | 1-2 seg | 3 seg |
| rabbitmq | — | 5-10 seg | 10 seg |
| authservice | postgres, redis | 3-5 seg | 5 seg |
| userservice | postgres, redis, authservice| 3-5 seg | 5 seg |
| gateway | authservice, userservice | 2-3 seg | 5 seg |
| **Total** | — | **18-35 seg** | **35 seg** |

**⚠️ El script solo espera 8 seg — INSUFICIENTE**

---

## ✅ PRÓXIMOS PASOS

1. **Hoy**: Ejecutar script mejorado y testear
2. **Mañana**: Documentar en `docs/GUIA-DESARROLLO-RAPIDO.md`
3. **Esta semana**: Agregar health check CI en GitHub Actions

---

## 📞 CONTACTO + REFERENCIAS

- **Copilot Instructions**: `.github/copilot-instructions.md` → PASO 2, línea 185
- **Compose YAML**: `compose.yaml` → Profiles (línea 8-50)
- **Health Check Convention**: `compose.yaml` → `x-healthcheck-api` anchor (línea 68-74)

---

**ESTADO**: 🔴 CRÍTICO → 🟡 EN PROGRESO → 🟢 RESUELTO (pendiente fix PR)
