#!/usr/bin/env bash
# =============================================================================
# deploy-local.sh — Deploy directo a Docker Desktop (SIN CI/CD)
# =============================================================================
#
# USO:
#   ./deploy-local.sh                        # auto-detecta servicios cambiados vs main
#   ./deploy-local.sh authservice gateway    # fuerza rebuild de servicios específicos
#   ./deploy-local.sh --all                  # rebuild de TODOS los servicios
#   ./deploy-local.sh --profile vehicles     # también levanta el perfil indicado
#
# FLUJO:
#   1. Auto-detecta qué cambió (git diff vs rama base)
#   2. Levanta infra base si no está corriendo
#   3. docker compose build (ARM64 nativo, sin QEMU)
#   4. docker compose up -d --no-deps --force-recreate
#   5. Health check
#
# PREREQUISITO:
#   docker compose up -d   (infra: postgres + redis + rabbitmq)
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

# Todos los servicios buildables
ALL_SERVICES="authservice userservice vehiclessaleservice mediaservice contactservice notificationservice adminservice errorservice roleservice billingservice gateway chatbotservice dealeranalyticsservice searchagent recoagent supportagent pricingagent frontend-next"

# ── Parsear argumentos ────────────────────────────────────────────────────────
FORCED_SERVICES=()
PROFILE=""
BUILD_ALL=false
BASE_BRANCH="${BASE_BRANCH:-main}"

for arg in "$@"; do
  case "$arg" in
    --all)
      BUILD_ALL=true
      ;;
    --profile=*)
      PROFILE="${arg#--profile=}"
      ;;
    --profile)
      # El siguiente argumento es el perfil — se maneja abajo
      ;;
    --branch=*)
      BASE_BRANCH="${arg#--branch=}"
      ;;
    --help|-h)
      head -25 "$0" | grep "^#" | sed 's/^# \?//'
      exit 0
      ;;
    -*)
      warn "Flag desconocido: $arg (ignorado)"
      ;;
    *)
      FORCED_SERVICES+=("$arg")
      ;;
  esac
done

# Manejar --profile como flag separado del valor
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

  # Obtener lista de archivos que cambiaron vs la rama base
  local diff_files
  if git rev-parse --verify "origin/$BASE_BRANCH" &>/dev/null; then
    diff_files=$(git diff --name-only "origin/$BASE_BRANCH"...HEAD 2>/dev/null || git diff --name-only HEAD~1 HEAD 2>/dev/null || echo "")
  else
    diff_files=$(git diff --name-only HEAD~1 HEAD 2>/dev/null || echo "")
  fi

  # También incluir cambios unstaged/staged en el working tree
  local staged_files
  staged_files=$(git diff --name-only HEAD 2>/dev/null || echo "")
  diff_files="$diff_files"$'\n'"$staged_files"

  log "Comparando contra: $BASE_BRANCH"

  # _Shared afecta a TODOS los servicios backend
  if echo "$diff_files" | grep -q "^backend/_Shared/"; then
    log "  _Shared cambió → marcando todos los servicios backend"
    for path in "${!SVC_PATHS[@]}"; do
      [[ "$path" == frontend/* ]] && continue
      changed_svcs+=("${SVC_PATHS[$path]}")
    done
    # También chequear frontend por si acaso
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

# Calcular lista final de servicios a rebuild
if [[ "$BUILD_ALL" == true ]]; then
  SERVICES_TO_BUILD="$ALL_SERVICES"
  log "Modo --all: rebuild de todos los servicios"
elif [[ ${#FORCED_SERVICES[@]} -gt 0 ]]; then
  SERVICES_TO_BUILD="${FORCED_SERVICES[*]}"
  log "Servicios forzados: $SERVICES_TO_BUILD"
else
  SERVICES_TO_BUILD=$(detect_changed_services)
fi

# Limpiar espacios extras
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
# PASO 2 — Asegurar que la infra base esté corriendo
# =============================================================================
header "Verificando infra base"

INFRA_UP=false
if docker compose ps postgres 2>/dev/null | grep -q "running"; then
  ok "Infra base ya está corriendo"
  INFRA_UP=true
fi

if [[ "$INFRA_UP" == false ]]; then
  log "Levantando infra base (postgres + redis + rabbitmq + seq + jaeger)..."
  docker compose up -d
  log "Esperando 8s para que los servicios de infra arranquen..."
  sleep 8
  ok "Infra base lista"
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

  # Verificar que el servicio existe en compose.yaml
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
# PASO 4 — Restart servicios (sin derribar dependencias)
# =============================================================================
header "Restart servicios"

FAILED_RESTART=()

for SVC in $SERVICES_TO_BUILD; do
  # Verificar que existe en compose
  if ! docker compose config --services 2>/dev/null | grep -qx "$SVC"; then
    continue
  fi

  log "🔄 Reiniciando ${BOLD}$SVC${RESET}..."
  if docker compose up -d --no-deps --force-recreate "$SVC" 2>&1; then
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
# PASO 5 — Health check post-deploy
# =============================================================================
header "Health checks (esperando 8s para arranque inicial)"
sleep 8

check_http() {
  local name="$1"
  local url="$2"
  local expected="${3:-200}"
  local code
  code=$(curl -s -m 15 -o /dev/null -w "%{http_code}" "$url" 2>/dev/null || echo "000")
  if [[ "$code" == "$expected" ]]; then
    ok "$name — HTTP $code"
  else
    warn "$name — esperado $expected, recibido $code [$url]"
  fi
}

check_http "Gateway"     "http://localhost:18443/health"
check_http "AuthService" "http://localhost:18443/api/auth/health"

# Health check de servicios específicos que se deployaron
for SVC in $SERVICES_TO_BUILD; do
  case "$SVC" in
    frontend-next) check_http "Frontend" "http://localhost:3000" ;;
    gateway)       : ;; # Ya chequeado arriba
    authservice)   : ;; # Ya chequeado arriba
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
    STATE=$(docker compose ps --status running "$SVC" 2>/dev/null | grep -c "$SVC" || echo "0")
    if [[ "$STATE" -gt "0" ]]; then
      ok "$SVC — running"
    else
      warn "$SVC — no está en estado running (revisa: docker compose logs $SVC)"
    fi
  fi
done

echo ""
echo -e "${BOLD}Stack accesible en:${RESET}"
echo "  https://okla.local         (si Caddy + mkcert están levantados)"
echo "  http://localhost:3000       (frontend directo)"
echo "  http://localhost:18443      (gateway API)"
echo "  http://localhost:5341       (Seq — logs)"
echo "  http://localhost:16686      (Jaeger — tracing)"
echo ""
echo "[$(date '+%Y-%m-%d %H:%M:%S')] [DEPLOY] deploy-local.sh — servicios: $SERVICES_TO_BUILD" >> .github/copilot-audit.log
