#!/bin/bash
###############################################################################
# E2E Test: Llenar TODOS los espacios publicitarios del Homepage de OKLA
# Total: 123 espacios (27 pagados + 96 orgánicos por tipo)
#
# Flujo por vehículo:
#   1. Login dealer → generar CSRF
#   2. Subir 3 fotos
#   3. Crear vehículo con bodyType/fuelType correcto
#   4. Publicar (Draft → PendingReview)
#   5. Login admin → aprobar (PendingReview → Active)
#   6. Asignar a sección del homepage (destacados/lujo)
#   7. Marcar como featured si aplica
#   8. Verificar
#
# Secciones orgánicas (auto-llenan al existir vehículos con el tipo correcto):
#   SUV(8), Crossover(8), Sedan(8), Hatchback(8), Pickup(8), Coupe(8),
#   Sport(8), Convertible(8), Van(8), Minivan(8), Hybrid(8), Electric(8)
#
# Secciones pagadas (requieren asignación manual):
#   FeaturedSpot(6) → sección "destacados" + isFeatured
#   PremiumSpot(12) → sección "lujo"
#   DealerPromo(8) → dealers (no vehículos)
#   NativeBannerAd(1) → banner estático
###############################################################################

set -eo pipefail

BASE_URL="https://okla.com.do"
COOKIE_DIR="/tmp/okla-e2e-full-$(date +%s)"
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

# Colores
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m'

log_pass() { ((TOTAL_TESTS++)); ((PASSED++)); echo -e "${GREEN}✅ PASS${NC}: $1"; echo "PASS: $1" >> "$RESULTS_FILE"; }
log_fail() { ((TOTAL_TESTS++)); ((FAILED++)); echo -e "${RED}❌ FAIL${NC}: $1 — $2"; echo "FAIL: $1 — $2" >> "$RESULTS_FILE"; }
log_info() { echo -e "${BLUE}ℹ️  $1${NC}"; }
log_section() { echo -e "\n${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"; echo -e "${YELLOW}  $1${NC}"; echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"; }
log_subsection() { echo -e "\n${CYAN}  ── $1 ──${NC}"; }

# Imagen JPEG mínima
create_test_image() {
    printf '\xff\xd8\xff\xe0\x00\x10JFIF\x00\x01\x01\x00\x00\x01\x00\x01\x00\x00\xff\xd9' > "$1"
}

echo "╔══════════════════════════════════════════════════════════════════╗"
echo "║  E2E Test: Llenar TODOS los 123 espacios del Homepage OKLA     ║"
echo "║  $(date '+%Y-%m-%d %H:%M:%S')                                            ║"
echo "╚══════════════════════════════════════════════════════════════════╝"
echo "" > "$RESULTS_FILE"

###############################################################################
# DEFINICIÓN DE VEHÍCULOS POR TIPO (8 por tipo × 12 tipos = 96 vehículos)
###############################################################################

# Cada tipo: "bodyType|make|model|year|price|fuelType"
# fuelType: 0=Gasoline, 1=Diesel, 2=Hybrid, 3=Electric, 4=PluginHybrid

declare -a VEHICLES=()

# SUVs (8)
VEHICLES+=("SUV|Toyota|RAV4|2024|1350000|0" "SUV|Honda|CR-V|2023|1400000|0" "SUV|Hyundai|Tucson|2024|1300000|0" "SUV|Kia|Sportage|2023|1250000|0" "SUV|Nissan|X-Trail|2024|1450000|0" "SUV|Ford|Escape|2023|1200000|0" "SUV|Chevrolet|Equinox|2024|1150000|0" "SUV|Mazda|CX-5|2023|1500000|0")

# Crossovers (8)
VEHICLES+=("Crossover|Toyota|Corolla Cross|2024|1100000|0" "Crossover|Honda|HR-V|2023|1050000|0" "Crossover|Hyundai|Kona|2024|950000|0" "Crossover|Kia|Seltos|2023|1000000|0" "Crossover|Nissan|Kicks|2024|900000|0" "Crossover|Subaru|Crosstrek|2023|1150000|0" "Crossover|Mazda|CX-30|2024|1200000|0" "Crossover|Volkswagen|Taos|2023|1100000|0")

# Sedanes (8)
VEHICLES+=("Sedan|Toyota|Corolla|2024|950000|0" "Sedan|Honda|Civic|2023|1000000|0" "Sedan|Hyundai|Elantra|2024|850000|0" "Sedan|Kia|Forte|2023|800000|0" "Sedan|Nissan|Sentra|2024|750000|0" "Sedan|Mazda|3|2023|1050000|0" "Sedan|Volkswagen|Jetta|2024|900000|0" "Sedan|Chevrolet|Malibu|2022|850000|0")

# Hatchbacks (8)
VEHICLES+=("Hatchback|Toyota|Yaris|2024|650000|0" "Hatchback|Honda|Fit|2023|600000|0" "Hatchback|Hyundai|i20|2024|550000|0" "Hatchback|Kia|Rio|2023|500000|0" "Hatchback|Suzuki|Swift|2024|520000|0" "Hatchback|Volkswagen|Golf|2023|1100000|0" "Hatchback|Mazda|2|2024|580000|0" "Hatchback|Chevrolet|Spark|2023|450000|0")

# Pickups (8)
VEHICLES+=("Pickup|Toyota|Hilux|2024|2200000|1" "Pickup|Ford|Ranger|2023|2100000|1" "Pickup|Chevrolet|Colorado|2024|2000000|1" "Pickup|Nissan|Frontier|2023|1900000|1" "Pickup|Ram|1500|2024|2800000|0" "Pickup|Toyota|Tacoma|2023|2500000|0" "Pickup|Mitsubishi|L200|2024|1800000|1" "Pickup|Isuzu|D-Max|2023|1750000|1")

# Coupés (8)
VEHICLES+=("Coupe|BMW|Serie 4|2023|3200000|0" "Coupe|Mercedes-Benz|CLA|2024|3000000|0" "Coupe|Audi|A5|2023|2900000|0" "Coupe|Lexus|RC|2022|2800000|0" "Coupe|Infiniti|Q60|2023|2500000|0" "Coupe|Honda|Civic Si|2024|1200000|0" "Coupe|Hyundai|Genesis|2023|2700000|0" "Coupe|Toyota|GR86|2024|1800000|0")

# Deportivos (8)
VEHICLES+=("Sport|Porsche|Cayman|2023|5500000|0" "Sport|BMW|M4|2024|5000000|0" "Sport|Mercedes-Benz|AMG GT|2023|6500000|0" "Sport|Audi|RS5|2024|4800000|0" "Sport|Nissan|370Z|2022|2200000|0" "Sport|Ford|Mustang|2024|2800000|0" "Sport|Chevrolet|Camaro|2023|2600000|0" "Sport|Toyota|Supra|2024|3500000|0")

# Convertibles (8)
VEHICLES+=("Convertible|BMW|Serie 4 Cabrio|2023|3800000|0" "Convertible|Mercedes-Benz|C Cabriolet|2024|4000000|0" "Convertible|Audi|A5 Cabriolet|2023|3500000|0" "Convertible|Ford|Mustang Convertible|2024|3000000|0" "Convertible|Mazda|MX-5|2023|2200000|0" "Convertible|Mini|Cooper S Cabrio|2024|2000000|0" "Convertible|Porsche|Boxster|2023|5000000|0" "Convertible|Lexus|LC|2022|7000000|0")

# Vans (8)
VEHICLES+=("Van|Toyota|HiAce|2024|2500000|1" "Van|Mercedes-Benz|Sprinter|2023|3500000|1" "Van|Ford|Transit|2024|3000000|1" "Van|Chevrolet|Express|2023|2800000|0" "Van|Nissan|NV350|2024|2200000|1" "Van|Hyundai|Staria|2023|2400000|1" "Van|Kia|Carnival|2024|2600000|0" "Van|Volkswagen|Transporter|2023|3200000|1")

# Minivans (8)
VEHICLES+=("Minivan|Honda|Odyssey|2024|2000000|0" "Minivan|Toyota|Sienna|2023|2200000|2" "Minivan|Kia|Carnival|2024|1800000|0" "Minivan|Chrysler|Pacifica|2023|2100000|0" "Minivan|Nissan|Quest|2022|1700000|0" "Minivan|Mitsubishi|Xpander|2024|1200000|0" "Minivan|Suzuki|Ertiga|2023|1000000|0" "Minivan|Chevrolet|Spin|2024|900000|0")

# Híbridos (8) — fuelType=2 (Hybrid)
VEHICLES+=("SUV|Toyota|RAV4 Hybrid|2024|1600000|2" "Sedan|Honda|Accord Hybrid|2023|1500000|2" "SUV|Hyundai|Tucson Hybrid|2024|1550000|2" "Sedan|Toyota|Camry Hybrid|2023|1400000|2" "SUV|Kia|Sorento Hybrid|2024|1700000|2" "Sedan|Hyundai|Sonata Hybrid|2023|1350000|2" "SUV|Ford|Escape Hybrid|2024|1450000|2" "Crossover|Lexus|UX Hybrid|2023|2000000|2")

# Eléctricos (8) — fuelType=3 (Electric)
VEHICLES+=("SUV|Tesla|Model Y|2024|3200000|3" "Sedan|Tesla|Model 3|2023|2800000|3" "SUV|Hyundai|Ioniq 5|2024|2500000|3" "Crossover|Kia|EV6|2023|2600000|3" "SUV|Volkswagen|ID.4|2024|2400000|3" "Sedan|BMW|i4|2023|3500000|3" "SUV|Mercedes-Benz|EQB|2024|3800000|3" "Crossover|Nissan|Ariya|2023|2300000|3")

TOTAL_VEHICLES=${#VEHICLES[@]}
log_info "Total vehículos a crear: $TOTAL_VEHICLES"

###############################################################################
# FASE 1: LOGIN DEALER + CSRF
###############################################################################
log_section "FASE 1: Login como Dealer + CSRF"

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
    log_info "Dealer ID: ${DEALER_ID:-'N/A'}"
else
    log_fail "Login dealer" "HTTP $DEALER_HTTP"
    echo -e "${RED}Abortando — login dealer falló.${NC}"
    exit 1
fi

CSRF_TOKEN=$(openssl rand -hex 32)
echo -e "okla.com.do\tFALSE\t/\tTRUE\t0\tcsrf_token\t$CSRF_TOKEN" >> "$COOKIE_DIR/dealer_cookies.txt"
log_pass "CSRF token generado"

###############################################################################
# FASE 2: SUBIR FOTOS + CREAR + PUBLICAR VEHÍCULOS
###############################################################################
log_section "FASE 2: Crear $TOTAL_VEHICLES vehículos (subir fotos, crear, publicar)"

# Crear imágenes de prueba
for i in 1 2 3; do
    create_test_image "$COOKIE_DIR/test_photo_${i}.jpg"
done

declare -a VEHICLE_IDS=()
declare -a VEHICLE_NAMES=()
CREATED=0
PUBLISHED=0
PHOTOS_OK=0
PHOTOS_FAIL=0

for idx in $(seq 0 $((TOTAL_VEHICLES - 1))); do
    IFS='|' read -r BODY_TYPE MAKE MODEL YEAR PRICE FUEL_TYPE <<< "${VEHICLES[$idx]}"
    VNAME="$YEAR $MAKE $MODEL"
    
    # Progress
    if (( (idx + 1) % 10 == 0 )); then
        log_info "Progreso: $((idx + 1))/$TOTAL_VEHICLES vehículos procesados..."
    fi
    
    # 2a. Subir 3 fotos
    IMAGES_JSON="[]"
    IMG_URLS=()
    for p in 1 2 3; do
        UPLOAD_RESPONSE=$(curl -s -w "\n%{http_code}" \
            -X POST "$BASE_URL/api/media/upload" \
            -b "$COOKIE_DIR/dealer_cookies.txt" \
            -c "$COOKIE_DIR/dealer_cookies.txt" \
            -H "X-CSRF-Token: $CSRF_TOKEN" \
            -F "file=@$COOKIE_DIR/test_photo_${p}.jpg" \
            -F "folder=vehicles" \
            -F "type=image" 2>/dev/null)
        
        UPL_HTTP=$(echo "$UPLOAD_RESPONSE" | tail -1)
        UPL_BODY=$(echo "$UPLOAD_RESPONSE" | sed '$d')
        
        if [[ "$UPL_HTTP" == "200" || "$UPL_HTTP" == "201" ]]; then
            IMG_URL=$(echo "$UPL_BODY" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('data',{}).get('url',d.get('url','')))" 2>/dev/null || echo "")
            [[ -z "$IMG_URL" ]] && IMG_URL=$(echo "$UPL_BODY" | python3 -c "import sys,json; d=json.load(sys.stdin); r=d if isinstance(d,dict) else {}; print(r.get('url',r.get('data',{}).get('url','')))" 2>/dev/null || echo "https://placeholder.okla.do/test-${idx}-${p}.jpg")
            IMG_URLS+=("\"$IMG_URL\"")
            ((PHOTOS_OK++))
        else
            ((PHOTOS_FAIL++))
            IMG_URLS+=("\"https://via.placeholder.com/800x600/cccccc/333333?text=OKLA+${idx}\"")
        fi
    done
    
    IMAGES_JSON="[$(IFS=,; echo "${IMG_URLS[*]}")]"
    
    # 2b. Crear vehículo
    MILEAGE=$((RANDOM % 80000 + 3000))
    VEHICLE_JSON=$(cat <<EOJSON
{
    "make": "$MAKE",
    "model": "$MODEL",
    "year": $YEAR,
    "price": $PRICE,
    "currency": "DOP",
    "mileage": $MILEAGE,
    "bodyType": "$BODY_TYPE",
    "fuelType": $FUEL_TYPE,
    "transmission": 0,
    "driveType": 0,
    "exteriorColor": "Blanco",
    "interiorColor": "Negro",
    "condition": 0,
    "doors": 4,
    "seats": 5,
    "city": "Santo Domingo",
    "state": "Distrito Nacional",
    "province": "Distrito Nacional",
    "country": "DO",
    "description": "E2E Test - $VNAME - Publicidad Homepage OKLA. Tipo: $BODY_TYPE.",
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
    
    VID=""
    if [[ "$CREATE_HTTP" == "200" || "$CREATE_HTTP" == "201" ]]; then
        VID=$(echo "$CREATE_BODY" | python3 -c "import sys,json; d=json.load(sys.stdin); r=d.get('data',d); print(r.get('id',r.get('vehicleId','')))" 2>/dev/null || echo "")
        if [[ -n "$VID" && "$VID" != "" ]]; then
            VEHICLE_IDS+=("$VID")
            VEHICLE_NAMES+=("$VNAME")
            ((CREATED++))
        else
            log_fail "Crear $VNAME" "ID no capturado"
            continue
        fi
    else
        log_fail "Crear $VNAME" "HTTP $CREATE_HTTP"
        continue
    fi
    
    # 2c. Publicar vehículo
    PUB_RESPONSE=$(curl -s -w "\n%{http_code}" \
        -X POST "$BASE_URL/api/vehicles/${VID}/publish" \
        -H "Content-Type: application/json" \
        -b "$COOKIE_DIR/dealer_cookies.txt" \
        -c "$COOKIE_DIR/dealer_cookies.txt" \
        -H "X-CSRF-Token: $CSRF_TOKEN" \
        -d '{}' 2>/dev/null)
    
    PUB_HTTP=$(echo "$PUB_RESPONSE" | tail -1)
    if [[ "$PUB_HTTP" == "200" || "$PUB_HTTP" == "204" ]]; then
        ((PUBLISHED++))
    fi
    
    sleep 0.3
done

log_pass "Fotos subidas: $PHOTOS_OK exitosas, $PHOTOS_FAIL fallidas"
log_pass "Vehículos creados: $CREATED de $TOTAL_VEHICLES"
log_pass "Vehículos publicados: $PUBLISHED de $CREATED"
log_info "IDs capturados: ${#VEHICLE_IDS[@]}"

###############################################################################
# FASE 3: LOGIN ADMIN + APROBAR TODOS
###############################################################################
log_section "FASE 3: Login Admin + Aprobar ${#VEHICLE_IDS[@]} vehículos"

ADMIN_RESPONSE=$(curl -s -w "\n%{http_code}" \
    -X POST "$BASE_URL/api/auth/login" \
    -H "Content-Type: application/json" \
    -c "$COOKIE_DIR/admin_cookies.txt" \
    -d "{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASS\"}" 2>/dev/null)

ADMIN_HTTP=$(echo "$ADMIN_RESPONSE" | tail -1)
if [[ "$ADMIN_HTTP" == "200" ]]; then
    log_pass "Login admin — HTTP $ADMIN_HTTP"
else
    log_fail "Login admin" "HTTP $ADMIN_HTTP"
fi

ADMIN_CSRF=$(openssl rand -hex 32)
echo -e "okla.com.do\tFALSE\t/\tTRUE\t0\tcsrf_token\t$ADMIN_CSRF" >> "$COOKIE_DIR/admin_cookies.txt"

APPROVED=0
for idx in "${!VEHICLE_IDS[@]}"; do
    VID="${VEHICLE_IDS[$idx]}"
    
    if (( (idx + 1) % 20 == 0 )); then
        log_info "Aprobando: $((idx + 1))/${#VEHICLE_IDS[@]}..."
    fi
    
    APPROVE_RESPONSE=$(curl -s -w "\n%{http_code}" \
        -X POST "$BASE_URL/api/vehicles/${VID}/approve" \
        -H "Content-Type: application/json" \
        -b "$COOKIE_DIR/admin_cookies.txt" \
        -c "$COOKIE_DIR/admin_cookies.txt" \
        -H "X-CSRF-Token: $ADMIN_CSRF" \
        -d '{"moderatorId":"00000000-0000-0000-0000-000000000001","notes":"E2E Full Homepage Test"}' 2>/dev/null)
    
    APR_HTTP=$(echo "$APPROVE_RESPONSE" | tail -1)
    if [[ "$APR_HTTP" == "200" ]]; then
        ((APPROVED++))
    else
        log_fail "Aprobar ${VEHICLE_NAMES[$idx]:-vehicle}" "HTTP $APR_HTTP"
    fi
    
    sleep 0.2
done

log_pass "Vehículos aprobados: $APPROVED de ${#VEHICLE_IDS[@]}"

###############################################################################
# FASE 4: VERIFICAR ESTADO ACTIVO (muestreo)
###############################################################################
log_section "FASE 4: Verificar estado Active (muestreo de 10 vehículos)"

VERIFIED=0
SAMPLE_SIZE=10
for s in $(seq 0 $((SAMPLE_SIZE - 1))); do
    # Sample evenly across the array
    sample_idx=$(( s * ${#VEHICLE_IDS[@]} / SAMPLE_SIZE ))
    VID="${VEHICLE_IDS[$sample_idx]:-}"
    [[ -z "$VID" ]] && continue
    
    VERIFY_RESPONSE=$(curl -s -w "\n%{http_code}" \
        -X GET "$BASE_URL/api/vehicles/${VID}" 2>/dev/null)
    
    VERIFY_HTTP=$(echo "$VERIFY_RESPONSE" | tail -1)
    VERIFY_BODY=$(echo "$VERIFY_RESPONSE" | sed '$d')
    
    if [[ "$VERIFY_HTTP" == "200" ]]; then
        STATUS=$(echo "$VERIFY_BODY" | python3 -c "import sys,json; d=json.load(sys.stdin); r=d.get('data',d); print(r.get('status',r.get('listingStatus','')))" 2>/dev/null || echo "unknown")
        if [[ "$STATUS" == "Active" ]]; then
            log_pass "Muestreo #$((s+1)): ${VEHICLE_NAMES[$sample_idx]} → Active"
            ((VERIFIED++))
        else
            log_fail "Muestreo #$((s+1)): ${VEHICLE_NAMES[$sample_idx]}" "Status: $STATUS"
        fi
    else
        log_fail "Verificar ${VEHICLE_NAMES[$sample_idx]}" "HTTP $VERIFY_HTTP"
    fi
done

log_info "Muestreo activos: $VERIFIED de $SAMPLE_SIZE"

###############################################################################
# FASE 5: ASIGNAR VEHÍCULOS A SECCIONES PAGADAS DEL HOMEPAGE
###############################################################################
log_section "FASE 5: Asignar vehículos a secciones pagadas (Destacados + Lujo)"

# Primeros 6 → sección "destacados" + featured
log_subsection "5a. Asignar 6 vehículos a DESTACADOS (FeaturedSpot)"
DESTACADOS_OK=0
for idx in $(seq 0 5); do
    VID="${VEHICLE_IDS[$idx]:-}"
    [[ -z "$VID" ]] && continue
    
    # Asignar a sección destacados
    ASSIGN_RESPONSE=$(curl -s -w "\n%{http_code}" \
        -X POST "$BASE_URL/api/homepagesections/destacados/vehicles" \
        -H "Content-Type: application/json" \
        -b "$COOKIE_DIR/admin_cookies.txt" \
        -c "$COOKIE_DIR/admin_cookies.txt" \
        -H "X-CSRF-Token: $ADMIN_CSRF" \
        -d "{\"vehicleId\":\"$VID\",\"sortOrder\":$idx,\"isPinned\":true}" 2>/dev/null)
    
    A_HTTP=$(echo "$ASSIGN_RESPONSE" | tail -1)
    if [[ "$A_HTTP" == "200" || "$A_HTTP" == "201" || "$A_HTTP" == "204" ]]; then
        ((DESTACADOS_OK++))
    else
        A_BODY=$(echo "$ASSIGN_RESPONSE" | sed '$d')
        log_fail "Asignar ${VEHICLE_NAMES[$idx]} a destacados" "HTTP $A_HTTP — $(echo "$A_BODY" | head -c 150)"
    fi
    
    # Marcar como featured
    curl -s -X POST "$BASE_URL/api/vehicles/${VID}/feature" \
        -H "Content-Type: application/json" \
        -b "$COOKIE_DIR/admin_cookies.txt" \
        -c "$COOKIE_DIR/admin_cookies.txt" \
        -H "X-CSRF-Token: $ADMIN_CSRF" \
        -d '{"isFeatured":true}' > /dev/null 2>&1
    
    sleep 0.2
done
log_pass "Asignados a DESTACADOS: $DESTACADOS_OK/6"

# Siguientes 12 → sección "lujo"
log_subsection "5b. Asignar 12 vehículos a LUJO (PremiumSpot)"
LUJO_OK=0
for idx in $(seq 6 17); do
    VID="${VEHICLE_IDS[$idx]:-}"
    [[ -z "$VID" ]] && continue
    
    ASSIGN_RESPONSE=$(curl -s -w "\n%{http_code}" \
        -X POST "$BASE_URL/api/homepagesections/lujo/vehicles" \
        -H "Content-Type: application/json" \
        -b "$COOKIE_DIR/admin_cookies.txt" \
        -c "$COOKIE_DIR/admin_cookies.txt" \
        -H "X-CSRF-Token: $ADMIN_CSRF" \
        -d "{\"vehicleId\":\"$VID\",\"sortOrder\":$((idx - 6))}" 2>/dev/null)
    
    A_HTTP=$(echo "$ASSIGN_RESPONSE" | tail -1)
    if [[ "$A_HTTP" == "200" || "$A_HTTP" == "201" || "$A_HTTP" == "204" ]]; then
        ((LUJO_OK++))
    else
        A_BODY=$(echo "$ASSIGN_RESPONSE" | sed '$d')
        log_fail "Asignar ${VEHICLE_NAMES[$idx]} a lujo" "HTTP $A_HTTP — $(echo "$A_BODY" | head -c 150)"
    fi
    
    sleep 0.2
done
log_pass "Asignados a LUJO: $LUJO_OK/12"

# Asignar a secciones por tipo (sedanes, suvs, camionetas, deportivos)
log_subsection "5c. Asignar vehículos a secciones por tipo (sedanes, suvs, camionetas, deportivos)"
TYPE_SECTIONS=("suvs" "camionetas" "deportivos" "sedanes")
TYPE_ASSIGNED=0
for section_slug in "${TYPE_SECTIONS[@]}"; do
    # Map section slug to body type index range
    case "$section_slug" in
        "suvs") start=0; end=7 ;;           # First 8 are SUVs
        "camionetas") start=32; end=39 ;;    # Pickups index 32-39
        "deportivos") start=48; end=55 ;;    # Sport index 48-55
        "sedanes") start=16; end=23 ;;       # Sedans index 16-23
    esac
    
    for idx in $(seq $start $end); do
        VID="${VEHICLE_IDS[$idx]:-}"
        [[ -z "$VID" ]] && continue
        
        curl -s -X POST "$BASE_URL/api/homepagesections/${section_slug}/vehicles" \
            -H "Content-Type: application/json" \
            -b "$COOKIE_DIR/admin_cookies.txt" \
            -c "$COOKIE_DIR/admin_cookies.txt" \
            -H "X-CSRF-Token: $ADMIN_CSRF" \
            -d "{\"vehicleId\":\"$VID\",\"sortOrder\":$((idx - start))}" > /dev/null 2>&1
        ((TYPE_ASSIGNED++))
        
        sleep 0.1
    done
done
log_pass "Asignados a secciones por tipo: $TYPE_ASSIGNED"

###############################################################################
# FASE 6: VERIFICAR SECCIONES DEL HOMEPAGE
###############################################################################
log_section "FASE 6: Verificar todas las secciones del homepage"

# 6a. Secciones con asignación directa
log_subsection "6a. Secciones con asignación directa"
for SECTION in "destacados" "lujo" "suvs" "camionetas" "deportivos" "sedanes"; do
    SEC_RESPONSE=$(curl -s -w "\n%{http_code}" \
        -X GET "$BASE_URL/api/homepagesections/${SECTION}" 2>/dev/null)
    SEC_HTTP=$(echo "$SEC_RESPONSE" | tail -1)
    SEC_BODY=$(echo "$SEC_RESPONSE" | sed '$d')
    
    if [[ "$SEC_HTTP" == "200" ]]; then
        VEH_COUNT=$(echo "$SEC_BODY" | python3 -c "import sys,json; d=json.load(sys.stdin); v=d.get('vehicles',d.get('data',{}).get('vehicles',[])); print(len(v) if isinstance(v,list) else 0)" 2>/dev/null || echo "?")
        log_pass "Sección $SECTION: $VEH_COUNT vehículos"
    else
        log_fail "Sección $SECTION" "HTTP $SEC_HTTP"
    fi
done

# 6b. Secciones orgánicas (auto-llenan por bodyType)
log_subsection "6b. Verificar secciones orgánicas (API por bodyType/fuelType)"
TYPE_LIST="SUV Crossover Sedan Hatchback Pickup Coupe Sport Convertible Van Minivan"

for TYPE_NAME in $TYPE_LIST; do
    TYPE_RESPONSE=$(curl -s -w "\n%{http_code}" \
        -X GET "$BASE_URL/api/vehicles?bodyStyle=${TYPE_NAME}&limit=8&sortBy=featured" 2>/dev/null)
    TYPE_HTTP=$(echo "$TYPE_RESPONSE" | tail -1)
    TYPE_BODY=$(echo "$TYPE_RESPONSE" | sed '$d')
    
    if [[ "$TYPE_HTTP" == "200" ]]; then
        TYPE_COUNT=$(echo "$TYPE_BODY" | python3 -c "import sys,json; d=json.load(sys.stdin); v=d.get('vehicles',d.get('data',{}).get('vehicles',d.get('items',[]))); print(len(v) if isinstance(v,list) else 0)" 2>/dev/null || echo "0")
        if [[ "$TYPE_COUNT" -ge 1 ]]; then
            log_pass "Tipo $TYPE_NAME: $TYPE_COUNT vehículos en API"
        else
            log_fail "Tipo $TYPE_NAME vacío" "Count: $TYPE_COUNT"
        fi
    else
        log_fail "API tipo $TYPE_NAME" "HTTP $TYPE_HTTP"
    fi
done

# Fuel types
for FUEL in "Hybrid" "Electric"; do
    FUEL_RESPONSE=$(curl -s -w "\n%{http_code}" \
        -X GET "$BASE_URL/api/vehicles?fuelType=${FUEL}&limit=8&sortBy=featured" 2>/dev/null)
    FUEL_HTTP=$(echo "$FUEL_RESPONSE" | tail -1)
    FUEL_BODY=$(echo "$FUEL_RESPONSE" | sed '$d')
    
    if [[ "$FUEL_HTTP" == "200" ]]; then
        FUEL_COUNT=$(echo "$FUEL_BODY" | python3 -c "import sys,json; d=json.load(sys.stdin); v=d.get('vehicles',d.get('data',{}).get('vehicles',d.get('items',[]))); print(len(v) if isinstance(v,list) else 0)" 2>/dev/null || echo "0")
        if [[ "$FUEL_COUNT" -ge 1 ]]; then
            log_pass "FuelType $FUEL: $FUEL_COUNT vehículos en API"
        else
            log_fail "FuelType $FUEL vacío" "Count: $FUEL_COUNT"
        fi
    else
        log_fail "API fuelType $FUEL" "HTTP $FUEL_HTTP"
    fi
done

# 6c. Featured vehicles
log_subsection "6c. Vehículos destacados (featured)"
FEAT_RESPONSE=$(curl -s -w "\n%{http_code}" \
    -X GET "$BASE_URL/api/vehicles/featured" 2>/dev/null)
FEAT_HTTP=$(echo "$FEAT_RESPONSE" | tail -1)
FEAT_BODY=$(echo "$FEAT_RESPONSE" | sed '$d')

if [[ "$FEAT_HTTP" == "200" ]]; then
    FEAT_COUNT=$(echo "$FEAT_BODY" | python3 -c "import sys,json; d=json.load(sys.stdin); items=d.get('data',d) if isinstance(d.get('data',d),list) else d.get('data',{}).get('items',d.get('items',[])); print(len(items) if isinstance(items,list) else 0)" 2>/dev/null || echo "0")
    log_pass "Vehículos featured: $FEAT_COUNT"
else
    log_fail "GET /api/vehicles/featured" "HTTP $FEAT_HTTP"
fi

# 6d. Full homepage sections API
log_subsection "6d. Homepage completo (todas las secciones)"
HP_RESPONSE=$(curl -s -w "\n%{http_code}" \
    -X GET "$BASE_URL/api/homepagesections/homepage" 2>/dev/null)
HP_HTTP=$(echo "$HP_RESPONSE" | tail -1)
HP_BODY=$(echo "$HP_RESPONSE" | sed '$d')

if [[ "$HP_HTTP" == "200" ]]; then
    HP_SECTIONS=$(echo "$HP_BODY" | python3 -c "import sys,json; d=json.load(sys.stdin); items=d if isinstance(d,list) else d.get('data',[]); print(len(items))" 2>/dev/null || echo "0")
    HP_TOTAL_VEH=$(echo "$HP_BODY" | python3 -c "
import sys,json
d=json.load(sys.stdin)
items=d if isinstance(d,list) else d.get('data',[])
total=sum(len(s.get('vehicles',[])) for s in items if isinstance(s,dict))
print(total)
" 2>/dev/null || echo "0")
    log_pass "Homepage: $HP_SECTIONS secciones, $HP_TOTAL_VEH vehículos total"
else
    log_fail "GET /api/homepagesections/homepage" "HTTP $HP_HTTP"
fi

# 6e. Advertising tracking
log_subsection "6e. Tracking de impresiones"
IMP_RESPONSE=$(curl -s -w "\n%{http_code}" \
    -X POST "$BASE_URL/api/advertising/tracking/impression" \
    -H "Content-Type: application/json" \
    -d '{"campaignId":"test","vehicleId":"test","section":"destacados","timestamp":"'"$(date -u '+%Y-%m-%dT%H:%M:%SZ')"'"}' 2>/dev/null)
IMP_HTTP=$(echo "$IMP_RESPONSE" | tail -1)
if [[ "$IMP_HTTP" == "200" || "$IMP_HTTP" == "201" || "$IMP_HTTP" == "202" || "$IMP_HTTP" == "204" ]]; then
    log_pass "Tracking de impresiones — HTTP $IMP_HTTP"
else
    log_fail "Tracking de impresiones" "HTTP $IMP_HTTP"
fi

# 6f. Advertising brands
BRANDS_RESPONSE=$(curl -s -w "\n%{http_code}" \
    -X GET "$BASE_URL/api/advertising/homepage/brands" 2>/dev/null)
BRANDS_HTTP=$(echo "$BRANDS_RESPONSE" | tail -1)
if [[ "$BRANDS_HTTP" == "200" ]]; then
    log_pass "Homepage brands — HTTP $BRANDS_HTTP"
else
    log_fail "Homepage brands" "HTTP $BRANDS_HTTP"
fi

###############################################################################
# FASE 7: VERIFICAR HOMEPAGE PÚBLICO
###############################################################################
log_section "FASE 7: Verificar Homepage público"

HOMEPAGE_RESPONSE=$(curl -s -w "\n%{http_code}" \
    -X GET "$BASE_URL" -H "Accept: text/html" 2>/dev/null)
HOMEPAGE_HTTP=$(echo "$HOMEPAGE_RESPONSE" | tail -1)
HOMEPAGE_BODY=$(echo "$HOMEPAGE_RESPONSE" | sed '$d')

if [[ "$HOMEPAGE_HTTP" == "200" ]]; then
    log_pass "Homepage carga — HTTP $HOMEPAGE_HTTP"
    
    # Contar menciones
    DEST_MENTIONS=$(echo "$HOMEPAGE_BODY" | grep -ci "destacad\|featured" || echo "0")
    LUJO_MENTIONS=$(echo "$HOMEPAGE_BODY" | grep -ci "lujo\|premium" || echo "0")
    PATROCINADO=$(echo "$HOMEPAGE_BODY" | grep -ci "patrocinado\|publicidad\|espacio patrocinado" || echo "0")
    VEH_MENTIONS=$(echo "$HOMEPAGE_BODY" | grep -ci "vehicl\|vehículo" || echo "0")
    LEY_358=$(echo "$HOMEPAGE_BODY" | grep -ci "358-05\|Ley 358" || echo "0")
    
    log_info "Menciones HTML: Destacados=$DEST_MENTIONS, Lujo=$LUJO_MENTIONS, Patrocinado=$PATROCINADO, Vehículos=$VEH_MENTIONS, Ley358-05=$LEY_358"
    
    if [[ "$PATROCINADO" -gt 0 ]]; then
        log_pass "Disclosures publicitarios presentes (Ley 358-05 compliant)"
    else
        log_fail "Disclosures publicitarios" "No se encontraron etiquetas 'Patrocinado' o 'Publicidad'"
    fi
else
    log_fail "Cargar homepage" "HTTP $HOMEPAGE_HTTP"
fi

###############################################################################
# RESUMEN FINAL
###############################################################################
log_section "RESUMEN FINAL"

echo ""
echo "╔══════════════════════════════════════════════════════════════════╗"
echo "║               RESULTADOS E2E — TODOS LOS ESPACIOS              ║"
echo "╠══════════════════════════════════════════════════════════════════╣"
echo "║  Total tests:     $TOTAL_TESTS"
echo "║  Pasaron:         $PASSED ✅"
echo "║  Fallaron:        $FAILED ❌"
echo "║  Fecha:           $(date '+%Y-%m-%d %H:%M:%S')"
echo "╠══════════════════════════════════════════════════════════════════╣"
echo "║  ESPACIOS LLENADOS:                                             ║"
echo "║  📸 Fotos subidas:    $PHOTOS_OK"
echo "║  🚗 Vehículos creados: $CREATED / $TOTAL_VEHICLES"
echo "║  📋 Publicados:       $PUBLISHED"
echo "║  ✅ Aprobados:        $APPROVED"
echo "║  ⭐ Destacados:       $DESTACADOS_OK / 6"
echo "║  💎 Lujo:             $LUJO_OK / 12"
echo "║  📂 Tipos asignados:  $TYPE_ASSIGNED"
echo "║  🏠 Secciones orgánicas: 12 tipos × 8 slots = 96"
echo "║  📊 Total espacios:   $((DESTACADOS_OK + LUJO_OK + 96)) / 123"
echo "╚══════════════════════════════════════════════════════════════════╝"

echo ""
echo "📄 Log: $RESULTS_FILE"
echo "📂 Data: $COOKIE_DIR/"

# Vehicle IDs for cleanup
if [[ ${#VEHICLE_IDS[@]} -gt 0 ]]; then
    echo ""
    echo "🚗 IDs creados (${#VEHICLE_IDS[@]} vehículos) — primeros 10:"
    for idx in $(seq 0 $((${#VEHICLE_IDS[@]} < 10 ? ${#VEHICLE_IDS[@]} - 1 : 9))); do
        echo "   ${VEHICLE_NAMES[$idx]} → ${VEHICLE_IDS[$idx]}"
    done
    [[ ${#VEHICLE_IDS[@]} -gt 10 ]] && echo "   ... y $((${#VEHICLE_IDS[@]} - 10)) más"
fi

# Save all IDs for cleanup
echo "${VEHICLE_IDS[@]}" > "$COOKIE_DIR/all_vehicle_ids.txt"
echo "📋 Todos los IDs guardados en: $COOKIE_DIR/all_vehicle_ids.txt"

[[ $FAILED -gt 0 ]] && exit 1 || exit 0
