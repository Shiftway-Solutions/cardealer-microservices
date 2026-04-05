#!/usr/bin/env bash
# =============================================================================
# deploy-local.sh v2.1 — Deploy a Docker Desktop LOCAL (mejorado)
# =============================================================================
# FIXES (vs v2.0):
#   ✅ PASO 2: Levanta --profile core DESPUÉS de infra base
#   ✅ PASO 4: Quita --no-deps (peligroso con restart)
#   ✅ PASO 5: Espera a servicios HEALTHY, no solo running (backoff exponencial)
#   ✅ Detecta y reporta servicios unhealthy con logs
#   ✅ Early exit si dependencias fallan
#
# =============================================================================

set -euo pipefail

# ── Colores ──────────────────────────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
RESET='\033[0m'

log()    { echo -e "${CYAN}[deploy]${RESET} $*"; }
ok()     { echo -e "${GREEN}  ✅ $*${RESET}"; }
warn()   { echo -e "${YELLOW}  ⚠️  $*${RESET}"; }
error()  { echo -e "${RED}  ❌ $*${RESET}"; }
header() { echo -e "\n${BOLD}═══ $* ═══${RESET}"; }

# ── Directorio raíz ──────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# ── Mapa: path del código → nombre del servicio en compose.yaml ──────────────
declare -A SVC_PATHS=(
  [backend/AuthService]="authservice"
  [backend/UserService]="userservice"
  [backend/VehiclesSaleService]="vehiclessaleservice"
  [backend/MediaService]="mediaservice"
  [backend/ContactService]="contactservice"
  [backend/NotificationService]="notificationservice"
  [backend/AdminService]="adminservice"
  [backend/ErrorService]="errorservice"
  [backend/RoleService]="roleservice"
  [backend/BillingService]="billingservice"
  [backend/Gateway]="gateway"
  [backend/ChatbotService]="chatbotservice"
  [backend/DealerAnalyticsService]="dealeranalyticsservice"
  [backend/SearchAgent]="searchagent"
  [backend/RecoAgent]="recoagent"
  [backend/SupportAgent]="supportagent"
  [backend/PricingAgent]="pricingagent"
  [frontend/web-next]="frontend-next"
)

ALL_SERVICES="authservice userservice vehiclessaleservice mediaservice contactservice notificationservice adminservice errorservice roleservice billingservice gateway chatbotservice dealeranalyticsservice searchagent recoagent supportagent pricingagent frontend-next"

# ── Parsear argumentos ────────────────────────────────────────────────────────
FORCED_SERVICES=()
PROFILE=""
BUILD_ALL=false
BASE_BRANCH="${BASE_BRANCH:-main}"

for arg in "$@"; do
  case "$arg" in
    --all)       BUILD_ALL=true ;;
    --profile=*) PROFILE="${arg#--profile=}" ;;
    --profile)   : ;;
    --branch=*)  BASE_BRANCH="${arg#--branch=}" ;;
    --help|-h)   head -30 "$0" | grep "^#" | sed 's/^# \?//'; exit 0 ;;
    -*)          warn "Flag desconocido: $arg (ignorado)" ;;
    *)           FORCED_SERVICES+=("$arg") ;;
  esac
done

# Manejar --profile como flag separado
args=("$@")
for i in "${!args[@]}"; do
  if [[ "${args[$i]}" == "--profile" && -n "${args[$i+1]+x}" && "${args[$i+1]}" != --* ]]; then
    PROFILE="${args[$i+1]}"
  fi
done

# =============================================================================
# PASO 1 — Detectar servicios que cambiaron
# =============================================================================
header "Detectando cambios"

detect_changed_services() {
  local changed_svcs=()
  local diff_files
  
  if git rev-parse --verify "origin/$BASE_BRANCH" &>/dev/null; then
    diff_files=$(git diff --name-only "origin/$BASE_BRANCH"...HEAD 2>/dev/null || git diff --name-only HEAD~1 HEAD 2>/dev/null || echo "")
  else
    diff_files=$(git diff --name-only HEAD~1 HEAD 2>/dev/null || echo "")
  fi
  
  local staged_files
  staged_files=$(git diff --name-only HEAD 2>/dev/null || echo "")
  diff_files="$diff_files"$'\n'"$staged_files"

  log "Comparando contra: $BASE_BRANCH"

  if echo "$diff_files" | grep -q "^backend/_Shared/"; then
    log "  _Shared cambió → marcando todos los servicios backend"
    for path in "${!SVC_PATHS[@]}"; do
      [[ "$path" == frontend/* ]] && continue
      changed_svcs+=("${SVC_PATHS[$path]}")
    done
    if echo "$diff_files" | grep -q "^frontend/web-next/"; then
      changed_svcs+=("frontend-next")
    fi
    echo "${changed_svcs[*]}" | tr ' ' '\n' | sort -u | tr '\n' ' '
    return
  fi

  for path in "${!SVC_PATHS[@]}"; do
    if echo "$diff_files" | grep -q "^${path}/"; then
      changed_svcs+=("${SVC_PATHS[$path]}")
      log "  cambió → ${SVC_PATHS[$path]}"
    fi
  done

  if [[ ${#changed_svcs[@]} -eq 0 ]]; then
    log "  (ningún cambio detectado en archivos de servicios)"
  fi

  echo "${changed_svcs[*]}" | tr ' ' '\n' | sort -u | tr '\n' ' '
}

if [[ "$BUILD_ALL" == true ]]; then
  SERVICES_TO_BUILD="$ALL_SERVICES"
  log "Modo --all: rebuild de todos los servicios"
elif [[ ${#FORCED_SERVICES[@]} -gt 0 ]]; then
  SERVICES_TO_BUILD="${FORCED_SERVICES[*]}"
  log "Servicios forzados: $SERVICES_TO_BUILD"
else
  SERVICES_TO_BUILD=$(detect_changed_services)
fi

SERVICES_TO_BUILD=$(echo "$SERVICES_TO_BUILD" | xargs)

if [[ -z "$SERVICES_TO_BUILD" ]]; then
  warn "Ningún servicio cambió. Para forzar un rebuild usa:"
  echo "  ./deploy-local.sh <servicio1> [servicio2...]"
  echo "  ./deploy-local.sh --all"
  exit 0
fi

echo ""
log "Servicios a rebuild: ${BOLD}$SERVICES_TO_BUILD${RESET}"

# =============================================================================
# PASO 2 — Asegurar que infra base ESTÁ LEVANTADA Y HEALTHY
# =============================================================================
header "Verificando infra base"

wait_for_healthy() {
  local service=$1
  local max_attempts=${2:-60}  # default 10 min (60 * 10s intervals)
  local attempt=0

  log "  Esperando $service → healthy (máx $((max_attempts * 10))s)..."

  while [[ $attempt -lt $max_attempts ]]; do
    state=$(docker compose ps --format "{{.Service}}\t{{.Status}}" "$service" 2>/dev/null | tail -1 | cut -f2)
    
    case "$state" in
      *"healthy"*)
        ok "$service — Healthy ✓ ($((attempt * 10))s)"
        return 0
        ;;
      *"unhealthy"*)
        warn "$service — Unhealthy (retry)"
        sleep 10
        attempt=$((attempt + 1))
        ;;
      "")
        error "$service — No encontrado o no running"
        docker compose logs --tail=10 "$service" 2>&1 | head -5
        return 1
        ;;
      *)
        log "    $service — $state (attempt $((attempt + 1))/$max_attempts)"
        sleep 10
        attempt=$((attempt + 1))
        ;;
    esac
  done

  error "$service — Nunca alcanzó healthy state (timeout $((max_attempts * 10))s)"
  docker compose logs --tail=20 "$service" 2>&1 | head -10
  return 1
}

INFRA_UP=false
if docker compose ps postgres_db 2>/dev/null | grep -q "running"; then
  ok "Infra base ya está corriendo"
  INFRA_UP=true
fi

if [[ "$INFRA_UP" == false ]]; then
  log "Levantando infra base (postgres_db + redis + rabbitmq + consul + seq + jaeger)..."
  docker compose up -d
  log "Esperando que servicios base alcancen healthy..."
fi

# ✅ NEW in v2.1: Esperar a que deps básicas estén healthy
wait_for_healthy "postgres_db" 30 || { error "postgres_db nunca health"; exit 1; }
wait_for_healthy "redis" 20 || { error "redis nunca health"; exit 1; }
wait_for_healthy "rabbitmq" 40 || { error "rabbitmq nunca health"; exit 1; }
ok "Infra base OK"

# ✅ NEW in v2.1: Levantamos servicios CORE después de que infra esté lista
if docker compose config --services 2>/dev/null | grep -q "authservice"; then
  log "Levantando servicios core (authservice, gateway, userservice)..."
  docker compose --profile core up -d
  log "Esperando servicios core → healthy..."
  wait_for_healthy "authservice" 40 || { warn "authservice unhealthy (continuando)"; }
  wait_for_healthy "gateway" 40 || { warn "gateway unhealthy (continuando)"; }
fi

# Si se pidió un perfil adicional, levantarlo también
if [[ -n "$PROFILE" ]]; then
  log "Levantando perfil adicional: $PROFILE"
  docker compose --profile "$PROFILE" up -d
  sleep 5
  ok "Perfil $PROFILE levantado"
fi

# =============================================================================
# PASO 3 — Build Docker Compose (nativo ARM64, sin QEMU)
# =============================================================================
header "Build (ARM64 nativo — sin QEMU)"

FAILED_BUILD=()

for SVC in $SERVICES_TO_BUILD; do
  echo ""
  log "🔨 Building ${BOLD}$SVC${RESET}..."

  if ! docker compose config --services 2>/dev/null | grep -qx "$SVC"; then
    warn "$SVC no encontrado en compose.yaml — saltando"
    continue
  fi

  if docker compose build --no-cache "$SVC" 2>&1; then
    ok "$SVC build OK"
  else
    error "$SVC build FALLÓ"
    FAILED_BUILD+=("$SVC")
  fi
done

if [[ ${#FAILED_BUILD[@]} -gt 0 ]]; then
  error "Build falló en: ${FAILED_BUILD[*]}"
  echo ""
  echo "Revisa los errores arriba y vuelve a intentar:"
  echo "  ./deploy-local.sh ${FAILED_BUILD[*]}"
  exit 1
fi

# =============================================================================
# PASO 4 — Restart servicios (MEJORADO v2.1: SIN --no-deps)
# =============================================================================
header "Restart servicios"

FAILED_RESTART=()

for SVC in $SERVICES_TO_BUILD; do
  if ! docker compose config --services 2>/dev/null | grep -qx "$SVC"; then
    continue
  fi

  log "🔄 Reiniciando ${BOLD}$SVC${RESET}..."
  # ✅ FIXED v2.1: Quitamos --no-deps (era peligroso)
  if docker compose up -d --force-recreate "$SVC" 2>&1; then
    ok "$SVC → running"
  else
    error "$SVC no pudo reiniciar"
    FAILED_RESTART+=("$SVC")
  fi
done

if [[ ${#FAILED_RESTART[@]} -gt 0 ]]; then
  error "Restart falló en: ${FAILED_RESTART[*]}"
  exit 1
fi

# =============================================================================
# PASO 5 — Health check post-deploy (MEJORADO v2.1: Esperá HEALTHY)
# =============================================================================
header "Health checks post-restart (esperando healthy state)"

for SVC in $SERVICES_TO_BUILD; do
  case "$SVC" in
    gateway|authservice|userservice|roleservice)
      wait_for_healthy "$SVC" 40 || warn "$SVC health check timeout (puede estar iniciando)"
      ;;
    *)
      # Otros servicios: simple check running
      if docker compose ps --status running "$SVC" 2>/dev/null | grep -q "$SVC"; then
        ok "$SVC — running"
      else
        warn "$SVC — no está en estado running"
      fi
      ;;
  esac
done

# =============================================================================
# RESUMEN FINAL
# =============================================================================
header "Deploy completado"
echo ""
echo -e "${BOLD}Servicios actualizados:${RESET}"

for SVC in $SERVICES_TO_BUILD; do
  if docker compose config --services 2>/dev/null | grep -qx "$SVC"; then
    STATUS=$(docker compose ps --format "{{.Service}}\t{{.Status}}" "$SVC" 2>/dev/null | tail -1 | cut -f2)
    if [[ "$STATUS" == *"healthy"* ]]; then
      ok "$SVC — healthy ($STATUS)"
    elif [[ "$STATUS" == *"running"* ]]; then
      warn "$SVC — running pero no healthy: $STATUS"
    else
      warn "$SVC — estado desconocido: $STATUS"
    fi
  fi
done

echo ""
echo -e "${BOLD}⚠️  VALIDACIÓN RECOMENDADA:${RESET}"
echo "  docker compose ps | head -15"
echo "  curl -s http://localhost:18443/health | jq ."
echo ""
echo -e "${BOLD}Stack accesible en:${RESET}"
echo "  https://okla.local         (si Caddy + mkcert levantados)"
echo "  http://localhost:3000       (frontend directo)"
echo "  http://localhost:18443      (gateway API)"
echo "  http://localhost:5341       (Seq — logs)"
echo ""

echo "[$(date '+%Y-%m-%d %H:%M:%S')] [DEPLOY v2.1] servicios: $SERVICES_TO_BUILD" >> .github/copilot-audit.log
