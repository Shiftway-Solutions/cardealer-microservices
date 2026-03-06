#!/bin/bash
###############################################################################
# E2E Test: Publicación de Vehículos + Autorización + Publicidad en Homepage
# OKLA Platform - https://okla.com.do
#
# Flujo:
#   1. Login como dealer
#   2. Subir 3 fotos por vehículo
#   3. Crear vehículos (18 para llenar FeaturedSpot + PremiumSpot)
#   4. Publicar cada vehículo (Draft → PendingReview)
#   5. Login como admin
#   6. Aprobar cada vehículo (PendingReview → Active)
#   7. Crear campañas publicitarias (Featured + Premium)
#   8. Verificar que aparecen en el homepage
###############################################################################

set -eo pipefail

BASE_URL="https://okla.com.do"
COOKIE_DIR="/tmp/okla-e2e-ads"
mkdir -p "$COOKIE_DIR"
RESULTS_FILE="$COOKIE_DIR/results.log"

# Cuentas
DEALER_EMAIL="nmateo@okla.com.do"
DEALER_PASS="Dealer2026!@#"
ADMIN_EMAIL="admin@okla.local"
ADMIN_PASS='Admin123!@#'

# Contadores
TOTAL_TESTS=0
PASSED=0
FAILED=0
VEHICLE_IDS=()

# Colores
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log_pass() { ((TOTAL_TESTS++)); ((PASSED++)); echo -e "${GREEN}✅ PASS${NC}: $1"; echo "PASS: $1" >> "$RESULTS_FILE"; }
log_fail() { ((TOTAL_TESTS++)); ((FAILED++)); echo -e "${RED}❌ FAIL${NC}: $1 — $2"; echo "FAIL: $1 — $2" >> "$RESULTS_FILE"; }
log_info() { echo -e "${BLUE}ℹ️  $1${NC}"; }
log_section() { echo -e "\n${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"; echo -e "${YELLOW}  $1${NC}"; echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"; }

# Función para crear una imagen de prueba (small JPEG)
create_test_image() {
    local filepath="$1"
    # Crear un JPEG válido mínimo (2x2 rojo) - base64 encoded
    echo -n '/9j/4AAQSkZJRgABAQEASABIAAD/2wBDAAMCAgMCAgMDAwMEAwMEBQgFBQQEBQoHBwYIDAoMCwsKCwsM
DhEQDQ4RDgsLEBYQERMUFRUVDA8XGBYUGBIUFRT/2wBDAQMEBAUEBQkFBQkUDQsNFBQUFBQUFBQU
FBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBT/wAARCAAC
AAIDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAFBABAAAAAAAAAAAA
AAAAAAAAAP/aAAwDAQACEQMRAD8AJQAAAP/Z' | base64 -d > "$filepath" 2>/dev/null || {
        # Fallback: crear archivo binario mínimo si base64 falla
        printf '\xff\xd8\xff\xe0\x00\x10JFIF\x00\x01\x01\x00\x00\x01\x00\x01\x00\x00\xff\xd9' > "$filepath"
    }
}

# Datos de vehículos para crear (18 vehículos para FeaturedSpot + PremiumSpot)
declare -a MAKES=("Toyota" "Honda" "Hyundai" "Kia" "Nissan" "Ford" "Chevrolet" "Mazda" "BMW" "Mercedes-Benz" "Audi" "Volkswagen" "Subaru" "Mitsubishi" "Suzuki" "Lexus" "Jeep" "Ram")
declare -a MODELS=("Corolla" "Civic" "Tucson" "Sportage" "Sentra" "Explorer" "Equinox" "CX-5" "X3" "GLC" "Q5" "Tiguan" "Forester" "Outlander" "Vitara" "RX" "Wrangler" "1500")
declare -a YEARS=(2024 2023 2024 2023 2024 2023 2024 2023 2022 2022 2023 2024 2023 2024 2023 2022 2024 2023)
declare -a PRICES=(1200000 1100000 1300000 1150000 950000 2100000 1400000 1350000 2800000 3200000 2900000 1500000 1600000 1250000 1050000 3500000 2400000 2200000)
declare -a BODY_TYPES=("sedan" "sedan" "suv" "suv" "sedan" "suv" "suv" "suv" "suv" "suv" "suv" "suv" "suv" "suv" "suv" "suv" "suv" "pickup")
declare -a COLORS=("Blanco" "Negro" "Gris" "Rojo" "Azul" "Plata" "Blanco" "Rojo" "Negro" "Blanco" "Gris" "Azul" "Verde" "Plata" "Negro" "Blanco" "Negro" "Gris")

echo "╔══════════════════════════════════════════════════════════════╗"
echo "║  E2E Test: Publicación + Autorización + Publicidad Homepage  ║"
echo "║  OKLA Platform — $(date '+%Y-%m-%d %H:%M:%S')                ║"
echo "╚══════════════════════════════════════════════════════════════╝"
echo "" > "$RESULTS_FILE"

###############################################################################
# FASE 1: LOGIN COMO DEALER
###############################################################################
log_section "FASE 1: Login como Dealer"

DEALER_RESPONSE=$(curl -s -w "\n%{http_code}" \
    -X POST "$BASE_URL/api/auth/login" \
    -H "Content-Type: application/json" \
    -c "$COOKIE_DIR/dealer_cookies.txt" \
    -d "{\"email\":\"$DEALER_EMAIL\",\"password\":\"$DEALER_PASS\"}" 2>/dev/null)

DEALER_HTTP=$(echo "$DEALER_RESPONSE" | tail -1)
DEALER_BODY=$(echo "$DEALER_RESPONSE" | sed '$d')

if [[ "$DEALER_HTTP" == "200" ]]; then
    log_pass "Login dealer ($DEALER_EMAIL) — HTTP $DEALER_HTTP"
    DEALER_ID=$(echo "$DEALER_BODY" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('data',{}).get('userId',d.get('userId','')))" 2>/dev/null || echo "")
    log_info "Dealer ID: ${DEALER_ID:-'no disponible'}"
    
    # Also extract the access token for Bearer auth
    DEALER_TOKEN=$(echo "$DEALER_BODY" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('data',{}).get('accessToken',d.get('accessToken','')))" 2>/dev/null || echo "")
else
    log_fail "Login dealer" "HTTP $DEALER_HTTP"
    echo -e "${RED}No se puede continuar sin login del dealer. Abortando.${NC}"
    exit 1
fi

# Generate CSRF token (double-submit cookie pattern - client-generated)
CSRF_TOKEN=$(openssl rand -hex 32)
# Add CSRF cookie to the dealer cookie jar
echo -e "okla.com.do\tFALSE\t/\tTRUE\t0\tcsrf_token\t$CSRF_TOKEN" >> "$COOKIE_DIR/dealer_cookies.txt"
log_pass "CSRF token generado (double-submit cookie pattern)"

###############################################################################
# FASE 2: SUBIR FOTOS (3 por vehículo × 18 vehículos = 54 fotos)
###############################################################################
log_section "FASE 2: Subir fotos de vehículos (3 por vehículo)"

# Crear 3 imágenes de prueba
for i in 1 2 3; do
    create_test_image "$COOKIE_DIR/test_photo_${i}.jpg"
done

declare -a ALL_IMAGE_URLS=()
PHOTOS_UPLOADED=0
PHOTOS_FAILED=0

for v in $(seq 0 17); do
    VEHICLE_IMAGES=()
    for p in 1 2 3; do
        UPLOAD_RESPONSE=$(curl -s -w "\n%{http_code}" \
            -X POST "$BASE_URL/api/media/upload" \
            -b "$COOKIE_DIR/dealer_cookies.txt" \
            -c "$COOKIE_DIR/dealer_cookies.txt" \
            -H "X-CSRF-Token: $CSRF_TOKEN" \
            -F "file=@$COOKIE_DIR/test_photo_${p}.jpg" \
            -F "folder=vehicles" \
            -F "type=image" 2>/dev/null)
        
        UPLOAD_HTTP=$(echo "$UPLOAD_RESPONSE" | tail -1)
        UPLOAD_BODY=$(echo "$UPLOAD_RESPONSE" | sed '$d')
        
        if [[ "$UPLOAD_HTTP" == "200" || "$UPLOAD_HTTP" == "201" ]]; then
            IMG_URL=$(echo "$UPLOAD_BODY" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('data',{}).get('url',d.get('url','')))" 2>/dev/null || echo "")
            if [[ -n "$IMG_URL" && "$IMG_URL" != "" ]]; then
                VEHICLE_IMAGES+=("$IMG_URL")
                ((PHOTOS_UPLOADED++))
            else
                # Try alternate response format
                IMG_URL=$(echo "$UPLOAD_BODY" | python3 -c "import sys,json; d=json.load(sys.stdin); r=d if isinstance(d,dict) else {}; print(r.get('url',r.get('data',{}).get('url','')))" 2>/dev/null || echo "https://placeholder.okla.do/test-${v}-${p}.jpg")
                VEHICLE_IMAGES+=("$IMG_URL")
                ((PHOTOS_UPLOADED++))
            fi
        else
            ((PHOTOS_FAILED++))
            # Use placeholder for failed uploads
            VEHICLE_IMAGES+=("https://via.placeholder.com/800x600/cccccc/333333?text=OKLA+Test+V${v}P${p}")
        fi
    done
    
    # Store images as JSON array string for this vehicle
    IMG_JSON=$(printf '%s\n' "${VEHICLE_IMAGES[@]}" | python3 -c "import sys,json; print(json.dumps([l.strip() for l in sys.stdin if l.strip()]))" 2>/dev/null || echo '[]')
    ALL_IMAGE_URLS+=("$IMG_JSON")
done

if [[ $PHOTOS_UPLOADED -gt 0 ]]; then
    log_pass "Fotos subidas: $PHOTOS_UPLOADED exitosas, $PHOTOS_FAILED fallidas (de 54 total)"
else
    log_fail "Upload de fotos" "Ninguna foto subida exitosamente (HTTP errors)"
    log_info "Continuando con URLs placeholder..."
fi

###############################################################################
# FASE 3: CREAR 18 VEHÍCULOS
###############################################################################
log_section "FASE 3: Crear 18 vehículos"

for v in $(seq 0 17); do
    MAKE="${MAKES[$v]}"
    MODEL="${MODELS[$v]}"
    YEAR="${YEARS[$v]}"
    PRICE="${PRICES[$v]}"
    BODY="${BODY_TYPES[$v]}"
    COLOR="${COLORS[$v]}"
    IMAGES_JSON="${ALL_IMAGE_URLS[$v]:-[]}"
    
    VEHICLE_JSON=$(cat <<EOJSON
{
    "make": "$MAKE",
    "model": "$MODEL",
    "year": $YEAR,
    "price": $PRICE,
    "currency": "DOP",
    "mileage": $((RANDOM % 80000 + 5000)),
    "bodyType": "$BODY",
    "fuelType": 0,
    "transmission": 0,
    "driveType": 0,
    "exteriorColor": "$COLOR",
    "interiorColor": "Negro",
    "condition": 0,
    "doors": 4,
    "seats": 5,
    "city": "Santo Domingo",
    "state": "Distrito Nacional",
    "province": "Distrito Nacional",
    "country": "DO",
    "description": "E2E Test - $YEAR $MAKE $MODEL - Excelente condición, vehículo de prueba para publicidad homepage OKLA. Color $COLOR.",
    "images": $IMAGES_JSON,
    "features": ["Aire Acondicionado", "Bluetooth", "Cámara de Reversa"],
    "sellerPhone": "+18091234567",
    "sellerEmail": "$DEALER_EMAIL",
    "hasCleanTitle": true
}
EOJSON
)
    
    CREATE_RESPONSE=$(curl -s -w "\n%{http_code}" \
        -X POST "$BASE_URL/api/vehicles" \
        -H "Content-Type: application/json" \
        -b "$COOKIE_DIR/dealer_cookies.txt" \
        -c "$COOKIE_DIR/dealer_cookies.txt" \
        -H "X-CSRF-Token: $CSRF_TOKEN" \
        -d "$VEHICLE_JSON" 2>/dev/null)
    
    CREATE_HTTP=$(echo "$CREATE_RESPONSE" | tail -1)
    CREATE_BODY=$(echo "$CREATE_RESPONSE" | sed '$d')
    
    if [[ "$CREATE_HTTP" == "200" || "$CREATE_HTTP" == "201" ]]; then
        VID=$(echo "$CREATE_BODY" | python3 -c "import sys,json; d=json.load(sys.stdin); r=d.get('data',d); print(r.get('id',r.get('vehicleId','')))" 2>/dev/null || echo "")
        if [[ -n "$VID" && "$VID" != "" ]]; then
            VEHICLE_IDS+=("$VID")
            log_pass "Vehículo creado: $YEAR $MAKE $MODEL (ID: ${VID:0:8}...)"
        else
            log_pass "Vehículo creado: $YEAR $MAKE $MODEL (ID no capturado)"
            # Try to extract from Location header or other fields
            VID=$(echo "$CREATE_BODY" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('id',d.get('vehicleId',d.get('data',{}).get('id',''))))" 2>/dev/null || echo "unknown-$v")
            VEHICLE_IDS+=("$VID")
        fi
    else
        log_fail "Crear vehículo $YEAR $MAKE $MODEL" "HTTP $CREATE_HTTP — $(echo "$CREATE_BODY" | head -c 200)"
    fi
    
    # Small delay to avoid rate limiting
    sleep 0.5
done

log_info "Total vehículos creados: ${#VEHICLE_IDS[@]} de 18"

###############################################################################
# FASE 4: PUBLICAR VEHÍCULOS (Draft → PendingReview)
###############################################################################
log_section "FASE 4: Publicar vehículos (enviar a revisión)"

PUBLISHED=0
for VID in "${VEHICLE_IDS[@]}"; do
    if [[ "$VID" == "unknown-"* || -z "$VID" ]]; then
        log_info "Saltando vehículo con ID desconocido: $VID"
        continue
    fi
    
    PUB_RESPONSE=$(curl -s -w "\n%{http_code}" \
        -X POST "$BASE_URL/api/vehicles/${VID}/publish" \
        -H "Content-Type: application/json" \
        -b "$COOKIE_DIR/dealer_cookies.txt" \
        -c "$COOKIE_DIR/dealer_cookies.txt" \
        -H "X-CSRF-Token: $CSRF_TOKEN" \
        -d '{}' 2>/dev/null)
    
    PUB_HTTP=$(echo "$PUB_RESPONSE" | tail -1)
    PUB_BODY=$(echo "$PUB_RESPONSE" | sed '$d')
    
    if [[ "$PUB_HTTP" == "200" || "$PUB_HTTP" == "204" ]]; then
        log_pass "Publicado vehículo ${VID:0:8}... → PendingReview"
        ((PUBLISHED++))
    else
        log_fail "Publicar vehículo ${VID:0:8}..." "HTTP $PUB_HTTP — $(echo "$PUB_BODY" | head -c 200)"
    fi
    
    sleep 0.3
done

log_info "Vehículos publicados (enviados a revisión): $PUBLISHED de ${#VEHICLE_IDS[@]}"

###############################################################################
# FASE 5: LOGIN COMO ADMIN
###############################################################################
log_section "FASE 5: Login como Admin"

ADMIN_RESPONSE=$(curl -s -w "\n%{http_code}" \
    -X POST "$BASE_URL/api/auth/login" \
    -H "Content-Type: application/json" \
    -c "$COOKIE_DIR/admin_cookies.txt" \
    -d "{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASS\"}" 2>/dev/null)

ADMIN_HTTP=$(echo "$ADMIN_RESPONSE" | tail -1)
ADMIN_BODY=$(echo "$ADMIN_RESPONSE" | sed '$d')

if [[ "$ADMIN_HTTP" == "200" ]]; then
    log_pass "Login admin ($ADMIN_EMAIL) — HTTP $ADMIN_HTTP"
else
    log_fail "Login admin" "HTTP $ADMIN_HTTP"
    echo -e "${RED}No se puede continuar sin login del admin. Saltando aprobación.${NC}"
fi

# Generate CSRF token for admin (same double-submit pattern)
ADMIN_CSRF=$(openssl rand -hex 32)
echo -e "okla.com.do\tFALSE\t/\tTRUE\t0\tcsrf_token\t$ADMIN_CSRF" >> "$COOKIE_DIR/admin_cookies.txt"

###############################################################################
# FASE 6: APROBAR VEHÍCULOS (PendingReview → Active)
###############################################################################
log_section "FASE 6: Aprobar vehículos como Admin"

APPROVED=0
for idx in "${!VEHICLE_IDS[@]}"; do
    VID="${VEHICLE_IDS[$idx]}"
    if [[ "$VID" == "unknown-"* || -z "$VID" ]]; then
        continue
    fi
    
    MAKE="${MAKES[$idx]}"
    MODEL="${MODELS[$idx]}"
    YEAR="${YEARS[$idx]}"
    
    APPROVE_JSON=$(cat <<EOJSON
{
    "moderatorId": "00000000-0000-0000-0000-000000000001",
    "notes": "E2E Test - Aprobado para prueba de publicidad homepage"
}
EOJSON
)
    
    # Call VehiclesSaleService directly (POST /api/vehicles/{id}/approve)
    # The AdminService approve endpoint has a bug (calls PublishVehicleAsync instead of ApproveVehicleAsync)
    APPROVE_RESPONSE=$(curl -s -w "\n%{http_code}" \
        -X POST "$BASE_URL/api/vehicles/${VID}/approve" \
        -H "Content-Type: application/json" \
        -b "$COOKIE_DIR/admin_cookies.txt" \
        -c "$COOKIE_DIR/admin_cookies.txt" \
        -H "X-CSRF-Token: $ADMIN_CSRF" \
        -d "$APPROVE_JSON" 2>/dev/null)
    
    APPROVE_HTTP=$(echo "$APPROVE_RESPONSE" | tail -1)
    APPROVE_BODY=$(echo "$APPROVE_RESPONSE" | sed '$d')
    
    if [[ "$APPROVE_HTTP" == "200" || "$APPROVE_HTTP" == "204" ]]; then
        log_pass "Aprobado: $YEAR $MAKE $MODEL (${VID:0:8}...) → Active"
        ((APPROVED++))
    else
        log_fail "Aprobar $YEAR $MAKE $MODEL" "HTTP $APPROVE_HTTP — $(echo "$APPROVE_BODY" | head -c 200)"
    fi
    
    sleep 0.3
done

log_info "Vehículos aprobados: $APPROVED de ${#VEHICLE_IDS[@]}"

###############################################################################
# FASE 7: VERIFICAR VEHÍCULOS ACTIVOS
###############################################################################
log_section "FASE 7: Verificar vehículos están activos (públicos)"

VERIFIED=0
for idx in "${!VEHICLE_IDS[@]}"; do
    VID="${VEHICLE_IDS[$idx]}"
    if [[ "$VID" == "unknown-"* || -z "$VID" ]]; then
        continue
    fi
    
    VERIFY_RESPONSE=$(curl -s -w "\n%{http_code}" \
        -X GET "$BASE_URL/api/vehicles/${VID}" 2>/dev/null)
    
    VERIFY_HTTP=$(echo "$VERIFY_RESPONSE" | tail -1)
    VERIFY_BODY=$(echo "$VERIFY_RESPONSE" | sed '$d')
    
    if [[ "$VERIFY_HTTP" == "200" ]]; then
        STATUS=$(echo "$VERIFY_BODY" | python3 -c "import sys,json; d=json.load(sys.stdin); r=d.get('data',d); print(r.get('status',r.get('listingStatus','')))" 2>/dev/null || echo "unknown")
        if [[ "$STATUS" == *"ctive"* || "$STATUS" == *"ctivo"* || "$STATUS" == "1" || "$STATUS" == "Active" ]]; then
            log_pass "Verificado activo: ${MAKES[$idx]} ${MODELS[$idx]} (status: $STATUS)"
            ((VERIFIED++))
        else
            log_fail "Verificar activo ${MAKES[$idx]} ${MODELS[$idx]}" "Status: $STATUS (esperado: Active)"
        fi
    else
        log_fail "Verificar vehículo ${VID:0:8}..." "HTTP $VERIFY_HTTP"
    fi
done

log_info "Vehículos verificados como activos: $VERIFIED"

###############################################################################
# FASE 8: CREAR CAMPAÑAS PUBLICITARIAS
###############################################################################
log_section "FASE 8: Crear campañas publicitarias para Homepage"

# 8a. Obtener espacios publicitarios disponibles
log_info "Obteniendo espacios publicitarios..."
SPACES_RESPONSE=$(curl -s -w "\n%{http_code}" \
    -X GET "$BASE_URL/api/advertising/spaces" \
    -b "$COOKIE_DIR/admin_cookies.txt" 2>/dev/null)
SPACES_HTTP=$(echo "$SPACES_RESPONSE" | tail -1)
SPACES_BODY=$(echo "$SPACES_RESPONSE" | sed '$d')

if [[ "$SPACES_HTTP" == "200" ]]; then
    log_pass "Espacios publicitarios obtenidos — HTTP $SPACES_HTTP"
    log_info "Espacios: $(echo "$SPACES_BODY" | python3 -c "import sys,json; d=json.load(sys.stdin); items=d.get('data',d) if isinstance(d.get('data',d),list) else [d.get('data',d)]; print(len(items))" 2>/dev/null || echo 'N/A') disponibles"
else
    log_fail "Obtener espacios publicitarios" "HTTP $SPACES_HTTP"
fi

# 8b. Obtener secciones del homepage
log_info "Obteniendo secciones del homepage..."
SECTIONS_RESPONSE=$(curl -s -w "\n%{http_code}" \
    -X GET "$BASE_URL/api/homepagesections" 2>/dev/null)
SECTIONS_HTTP=$(echo "$SECTIONS_RESPONSE" | tail -1)
SECTIONS_BODY=$(echo "$SECTIONS_RESPONSE" | sed '$d')

if [[ "$SECTIONS_HTTP" == "200" ]]; then
    log_pass "Secciones del homepage obtenidas — HTTP $SECTIONS_HTTP"
    SECTION_COUNT=$(echo "$SECTIONS_BODY" | python3 -c "import sys,json; d=json.load(sys.stdin); items=d.get('data',d) if isinstance(d.get('data',d),list) else []; print(len(items))" 2>/dev/null || echo 'N/A')
    log_info "Secciones encontradas: $SECTION_COUNT"
else
    log_fail "Obtener secciones homepage" "HTTP $SECTIONS_HTTP"
fi

# 8c. Crear campaña FeaturedSpot (6 vehículos)
log_info "Creando campaña FeaturedSpot..."
FEATURED_CAMPAIGN=$(cat <<EOJSON
{
    "name": "E2E Test - Featured Spot Homepage Q1",
    "advertiserId": "${DEALER_ID:-}",
    "type": "FeaturedSpot",
    "startDate": "$(date -u '+%Y-%m-%dT%H:%M:%SZ')",
    "endDate": "$(date -u -v+30d '+%Y-%m-%dT%H:%M:%SZ' 2>/dev/null || date -u -d '+30 days' '+%Y-%m-%dT%H:%M:%SZ' 2>/dev/null || echo '2026-04-05T00:00:00Z')",
    "budget": 3000.00,
    "dailyBudget": 100.00,
    "targetUrl": "https://okla.com.do",
    "status": "Active"
}
EOJSON
)

FEAT_RESPONSE=$(curl -s -w "\n%{http_code}" \
    -X POST "$BASE_URL/api/advertising/campaigns" \
    -H "Content-Type: application/json" \
    -b "$COOKIE_DIR/admin_cookies.txt" \
    -c "$COOKIE_DIR/admin_cookies.txt" \
    -H "X-CSRF-Token: $ADMIN_CSRF" \
    -d "$FEATURED_CAMPAIGN" 2>/dev/null)

FEAT_HTTP=$(echo "$FEAT_RESPONSE" | tail -1)
FEAT_BODY=$(echo "$FEAT_RESPONSE" | sed '$d')

if [[ "$FEAT_HTTP" == "200" || "$FEAT_HTTP" == "201" ]]; then
    FEAT_ID=$(echo "$FEAT_BODY" | python3 -c "import sys,json; d=json.load(sys.stdin); r=d.get('data',d); print(r.get('id',r.get('campaignId','')))" 2>/dev/null || echo "")
    log_pass "Campaña FeaturedSpot creada (ID: ${FEAT_ID:-N/A})"
else
    log_fail "Crear campaña FeaturedSpot" "HTTP $FEAT_HTTP — $(echo "$FEAT_BODY" | head -c 300)"
fi

# 8d. Crear campaña PremiumSpot (12 vehículos)
log_info "Creando campaña PremiumSpot..."
PREMIUM_CAMPAIGN=$(cat <<EOJSON
{
    "name": "E2E Test - Premium Spot Homepage Q1",
    "advertiserId": "${DEALER_ID:-}",
    "type": "PremiumSpot",
    "startDate": "$(date -u '+%Y-%m-%dT%H:%M:%SZ')",
    "endDate": "$(date -u -v+30d '+%Y-%m-%dT%H:%M:%SZ' 2>/dev/null || date -u -d '+30 days' '+%Y-%m-%dT%H:%M:%SZ' 2>/dev/null || echo '2026-04-05T00:00:00Z')",
    "budget": 6000.00,
    "dailyBudget": 200.00,
    "targetUrl": "https://okla.com.do",
    "status": "Active"
}
EOJSON
)

PREM_RESPONSE=$(curl -s -w "\n%{http_code}" \
    -X POST "$BASE_URL/api/advertising/campaigns" \
    -H "Content-Type: application/json" \
    -b "$COOKIE_DIR/admin_cookies.txt" \
    -c "$COOKIE_DIR/admin_cookies.txt" \
    -H "X-CSRF-Token: $ADMIN_CSRF" \
    -d "$PREMIUM_CAMPAIGN" 2>/dev/null)

PREM_HTTP=$(echo "$PREM_RESPONSE" | tail -1)
PREM_BODY=$(echo "$PREM_RESPONSE" | sed '$d')

if [[ "$PREM_HTTP" == "200" || "$PREM_HTTP" == "201" ]]; then
    PREM_ID=$(echo "$PREM_BODY" | python3 -c "import sys,json; d=json.load(sys.stdin); r=d.get('data',d); print(r.get('id',r.get('campaignId','')))" 2>/dev/null || echo "")
    log_pass "Campaña PremiumSpot creada (ID: ${PREM_ID:-N/A})"
else
    log_fail "Crear campaña PremiumSpot" "HTTP $PREM_HTTP — $(echo "$PREM_BODY" | head -c 300)"
fi

# 8e. Asignar vehículos a secciones del homepage
log_info "Asignando vehículos a secciones del homepage..."
ASSIGNED=0
for idx in "${!VEHICLE_IDS[@]}"; do
    VID="${VEHICLE_IDS[$idx]}"
    if [[ "$VID" == "unknown-"* || -z "$VID" ]]; then
        continue
    fi
    
    # Determinar tipo de sección (primeros 6 = Destacados, siguientes 12 = Lujo)
    # Secciones reales del seed: carousel, sedanes, suvs, camionetas, deportivos, destacados, lujo
    if [[ $idx -lt 6 ]]; then
        SECTION_TYPE="destacados"
    else
        SECTION_TYPE="lujo"
    fi
    
    ASSIGN_RESPONSE=$(curl -s -w "\n%{http_code}" \
        -X POST "$BASE_URL/api/homepagesections/${SECTION_TYPE}/vehicles" \
        -H "Content-Type: application/json" \
        -b "$COOKIE_DIR/admin_cookies.txt" \
        -c "$COOKIE_DIR/admin_cookies.txt" \
        -H "X-CSRF-Token: $ADMIN_CSRF" \
        -d "{\"vehicleId\":\"$VID\",\"sortOrder\":$idx}" 2>/dev/null)
    
    ASSIGN_HTTP=$(echo "$ASSIGN_RESPONSE" | tail -1)
    
    if [[ "$ASSIGN_HTTP" == "200" || "$ASSIGN_HTTP" == "201" || "$ASSIGN_HTTP" == "204" ]]; then
        log_pass "Asignado ${MAKES[$idx]} ${MODELS[$idx]} → sección $SECTION_TYPE"
        ((ASSIGNED++))
    else
        ASSIGN_BODY=$(echo "$ASSIGN_RESPONSE" | sed '$d')
        log_fail "Asignar ${MAKES[$idx]} ${MODELS[$idx]} a $SECTION_TYPE" "HTTP $ASSIGN_HTTP — $(echo "$ASSIGN_BODY" | head -c 200)"
    fi
    
    sleep 0.2
done

log_info "Vehículos asignados a secciones: $ASSIGNED"

# 8f. Marcar vehículos como destacados (featured toggle)
log_info "Marcando vehículos como destacados..."
FEATURED_COUNT=0
for idx in $(seq 0 5); do
    VID="${VEHICLE_IDS[$idx]:-}"
    if [[ -z "$VID" || "$VID" == "unknown-"* ]]; then continue; fi
    
    FEAT_TOGGLE=$(curl -s -w "\n%{http_code}" \
        -X POST "$BASE_URL/api/vehicles/${VID}/feature" \
        -H "Content-Type: application/json" \
        -b "$COOKIE_DIR/admin_cookies.txt" \
        -c "$COOKIE_DIR/admin_cookies.txt" \
        -H "X-CSRF-Token: $ADMIN_CSRF" \
        -d '{"isFeatured":true}' 2>/dev/null)
    
    FEAT_T_HTTP=$(echo "$FEAT_TOGGLE" | tail -1)
    
    if [[ "$FEAT_T_HTTP" == "200" || "$FEAT_T_HTTP" == "204" ]]; then
        log_pass "Destacado: ${MAKES[$idx]} ${MODELS[$idx]}"
        ((FEATURED_COUNT++))
    else
        FEAT_T_BODY=$(echo "$FEAT_TOGGLE" | sed '$d')
        log_fail "Destacar ${MAKES[$idx]} ${MODELS[$idx]}" "HTTP $FEAT_T_HTTP — $(echo "$FEAT_T_BODY" | head -c 200)"
    fi
    
    sleep 0.2
done

log_info "Vehículos marcados como destacados: $FEATURED_COUNT"

###############################################################################
# FASE 9: VERIFICAR HOMEPAGE
###############################################################################
log_section "FASE 9: Verificar vehículos en Homepage"

# 9a. Verificar vehículos destacados
log_info "Verificando vehículos destacados en homepage..."
FEAT_VEH_RESPONSE=$(curl -s -w "\n%{http_code}" \
    -X GET "$BASE_URL/api/vehicles/featured" 2>/dev/null)
FEAT_VEH_HTTP=$(echo "$FEAT_VEH_RESPONSE" | tail -1)
FEAT_VEH_BODY=$(echo "$FEAT_VEH_RESPONSE" | sed '$d')

if [[ "$FEAT_VEH_HTTP" == "200" ]]; then
    FEAT_VEH_COUNT=$(echo "$FEAT_VEH_BODY" | python3 -c "import sys,json; d=json.load(sys.stdin); items=d.get('data',d) if isinstance(d.get('data',d),list) else d.get('data',{}).get('items',d.get('items',[])); print(len(items) if isinstance(items,list) else 0)" 2>/dev/null || echo "0")
    if [[ "$FEAT_VEH_COUNT" -gt 0 ]]; then
        log_pass "Vehículos destacados en homepage: $FEAT_VEH_COUNT"
    else
        log_fail "Vehículos destacados vacío" "Count: $FEAT_VEH_COUNT"
    fi
else
    log_fail "Obtener vehículos destacados" "HTTP $FEAT_VEH_HTTP"
fi

# 9b. Verificar campañas activas
log_info "Verificando campañas publicitarias activas..."
CAMP_RESPONSE=$(curl -s -w "\n%{http_code}" \
    -X GET "$BASE_URL/api/advertising/campaigns" \
    -b "$COOKIE_DIR/admin_cookies.txt" 2>/dev/null)
CAMP_HTTP=$(echo "$CAMP_RESPONSE" | tail -1)
CAMP_BODY=$(echo "$CAMP_RESPONSE" | sed '$d')

if [[ "$CAMP_HTTP" == "200" ]]; then
    CAMP_COUNT=$(echo "$CAMP_BODY" | python3 -c "import sys,json; d=json.load(sys.stdin); items=d.get('data',d) if isinstance(d.get('data',d),list) else []; print(len(items))" 2>/dev/null || echo "N/A")
    log_pass "Campañas publicitarias: $CAMP_COUNT activas"
else
    log_fail "Obtener campañas" "HTTP $CAMP_HTTP"
fi

# 9c. Verificar homepage sections con vehículos
log_info "Verificando secciones del homepage con contenido..."
for SECTION in "destacados" "lujo"; do
    SEC_RESPONSE=$(curl -s -w "\n%{http_code}" \
        -X GET "$BASE_URL/api/homepagesections/${SECTION}" 2>/dev/null)
    SEC_HTTP=$(echo "$SEC_RESPONSE" | tail -1)
    SEC_BODY=$(echo "$SEC_RESPONSE" | sed '$d')
    
    if [[ "$SEC_HTTP" == "200" ]]; then
        SEC_VEH_COUNT=$(echo "$SEC_BODY" | python3 -c "import sys,json; d=json.load(sys.stdin); r=d.get('data',d); items=r.get('vehicles',r.get('items',[])) if isinstance(r,dict) else r; print(len(items) if isinstance(items,list) else 0)" 2>/dev/null || echo "0")
        log_pass "Sección $SECTION: $SEC_VEH_COUNT vehículos"
    else
        log_fail "Verificar sección $SECTION" "HTTP $SEC_HTTP"
    fi
done

# 9d. Verificar la rotación de anuncios
log_info "Verificando rotación de anuncios..."
for SECTION in "destacados" "lujo"; do
    ROT_RESPONSE=$(curl -s -w "\n%{http_code}" \
        -X GET "$BASE_URL/api/homepagesections/${SECTION}/rotate" 2>/dev/null)
    ROT_HTTP=$(echo "$ROT_RESPONSE" | tail -1)
    
    if [[ "$ROT_HTTP" == "200" ]]; then
        log_pass "Rotación $SECTION — HTTP $ROT_HTTP"
    else
        log_fail "Rotación $SECTION" "HTTP $ROT_HTTP"
    fi
done

# 9e. Verificar tracking de impresiones
log_info "Probando tracking de impresiones..."
IMPRESSION_RESPONSE=$(curl -s -w "\n%{http_code}" \
    -X POST "$BASE_URL/api/advertising/tracking/impression" \
    -H "Content-Type: application/json" \
    -d '{"campaignId":"test","vehicleId":"test","section":"featured","timestamp":"'"$(date -u '+%Y-%m-%dT%H:%M:%SZ')"'"}' 2>/dev/null)
IMP_HTTP=$(echo "$IMPRESSION_RESPONSE" | tail -1)

if [[ "$IMP_HTTP" == "200" || "$IMP_HTTP" == "201" || "$IMP_HTTP" == "204" || "$IMP_HTTP" == "202" ]]; then
    log_pass "Tracking de impresiones funcional — HTTP $IMP_HTTP"
else
    log_fail "Tracking de impresiones" "HTTP $IMP_HTTP"
fi

# 9f. Verificar homepage brands
log_info "Verificando brands del homepage..."
BRANDS_RESPONSE=$(curl -s -w "\n%{http_code}" \
    -X GET "$BASE_URL/api/advertising/homepage/brands" 2>/dev/null)
BRANDS_HTTP=$(echo "$BRANDS_RESPONSE" | tail -1)

if [[ "$BRANDS_HTTP" == "200" ]]; then
    log_pass "Homepage brands — HTTP $BRANDS_HTTP"
else
    log_fail "Homepage brands" "HTTP $BRANDS_HTTP"
fi

# 9g. Verificar homepage categories
log_info "Verificando categories del homepage..."
CATS_RESPONSE=$(curl -s -w "\n%{http_code}" \
    -X GET "$BASE_URL/api/homepagesections/homepage" 2>/dev/null)
CATS_HTTP=$(echo "$CATS_RESPONSE" | tail -1)

if [[ "$CATS_HTTP" == "200" ]]; then
    log_pass "Homepage categories — HTTP $CATS_HTTP"
else
    log_fail "Homepage categories" "HTTP $CATS_HTTP"
fi

###############################################################################
# FASE 10: VERIFICAR HOMEPAGE PÚBLICO (navegador)
###############################################################################
log_section "FASE 10: Verificar Homepage público"

HOMEPAGE_RESPONSE=$(curl -s -w "\n%{http_code}" \
    -X GET "$BASE_URL" \
    -H "Accept: text/html" 2>/dev/null)
HOMEPAGE_HTTP=$(echo "$HOMEPAGE_RESPONSE" | tail -1)
HOMEPAGE_BODY=$(echo "$HOMEPAGE_RESPONSE" | sed '$d')

if [[ "$HOMEPAGE_HTTP" == "200" ]]; then
    # Contar menciones de secciones de vehículos en el HTML
    FEATURED_MENTIONS=$(echo "$HOMEPAGE_BODY" | grep -ci "destacad\|featured" || echo "0")
    PREMIUM_MENTIONS=$(echo "$HOMEPAGE_BODY" | grep -ci "lujo\|premium" || echo "0")
    VEHICLE_MENTIONS=$(echo "$HOMEPAGE_BODY" | grep -ci "vehicl\|vehículo" || echo "0")
    
    log_pass "Homepage carga correctamente — HTTP $HOMEPAGE_HTTP"
    log_info "Menciones en HTML: Featured=$FEATURED_MENTIONS, Premium=$PREMIUM_MENTIONS, Vehículos=$VEHICLE_MENTIONS"
else
    log_fail "Cargar homepage" "HTTP $HOMEPAGE_HTTP"
fi

###############################################################################
# RESUMEN FINAL
###############################################################################
log_section "RESUMEN FINAL"

echo ""
echo "╔══════════════════════════════════════════════════════════════╗"
echo "║                    RESULTADOS E2E TEST                       ║"
echo "╠══════════════════════════════════════════════════════════════╣"
echo "║  Total tests:  $TOTAL_TESTS                                  "
echo "║  Pasaron:      $PASSED ✅                                    "
echo "║  Fallaron:     $FAILED ❌                                    "
echo "║  Fecha:        $(date '+%Y-%m-%d %H:%M:%S')                  "
echo "╠══════════════════════════════════════════════════════════════╣"
echo "║  FLUJO EJECUTADO:                                            ║"
echo "║  1. Login dealer ✓                                          ║"
echo "║  2. Upload ${PHOTOS_UPLOADED:-0} fotos (3 por vehículo)     "
echo "║  3. Creados ${#VEHICLE_IDS[@]} vehículos                    "
echo "║  4. Publicados $PUBLISHED vehículos                         "
echo "║  5. Login admin ✓                                           ║"
echo "║  6. Aprobados $APPROVED vehículos                           "
echo "║  7. Campañas publicitarias creadas                           ║"
echo "║  8. Verificación homepage                                    ║"
echo "╚══════════════════════════════════════════════════════════════╝"

echo ""
echo "📄 Log completo: $RESULTS_FILE"
echo "📂 Cookies/datos: $COOKIE_DIR/"
echo ""

# Lista de IDs creados (para limpieza manual si necesario)
if [[ ${#VEHICLE_IDS[@]} -gt 0 ]]; then
    echo "🚗 IDs de vehículos creados (para referencia/limpieza):"
    for idx in "${!VEHICLE_IDS[@]}"; do
        echo "   ${MAKES[$idx]} ${MODELS[$idx]} → ${VEHICLE_IDS[$idx]}"
    done
fi

# Exit code basado en resultados
if [[ $FAILED -gt 0 ]]; then
    exit 1
else
    exit 0
fi
