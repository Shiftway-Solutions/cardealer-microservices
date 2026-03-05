#!/bin/bash
# =============================================================================
# OKLA Chatbot Agent Testing Script
# Tests 100 questions per agent against production API
# =============================================================================
set -euo pipefail

BASE_URL="https://okla.com.do/api"
RESULTS_DIR="/tmp/okla-chatbot-tests"
mkdir -p "$RESULTS_DIR"

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

# Counters
TOTAL_PASS=0
TOTAL_FAIL=0
TOTAL_SLOW=0

# ── Helper Functions ──────────────────────────────────────────────────────────

log() { echo -e "${CYAN}[$(date +%H:%M:%S)]${NC} $1"; }
pass() { echo -e "${GREEN}  ✓${NC} $1"; ((TOTAL_PASS++)); }
fail() { echo -e "${RED}  ✗${NC} $1"; ((TOTAL_FAIL++)); }
warn() { echo -e "${YELLOW}  ⚠${NC} $1"; ((TOTAL_SLOW++)); }

# Authenticate and get token
authenticate() {
    local email="$1"
    local password="$2"
    log "Authenticating as $email..."
    local response
    response=$(curl -s -X POST "$BASE_URL/auth/login" \
        -H "Content-Type: application/json" \
        -d "{\"email\":\"$email\",\"password\":\"$password\"}" 2>/dev/null)
    
    local token
    token=$(echo "$response" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('data',{}).get('token','') or d.get('token',''))" 2>/dev/null || echo "")
    
    if [ -z "$token" ]; then
        echo "AUTH_FAILED"
        return 1
    fi
    echo "$token"
}

# Start a chat session
start_session() {
    local chat_mode="$1"
    local dealer_id="${2:-}"
    local vehicle_id="${3:-}"
    
    local body="{\"sessionType\":1,\"channel\":\"web\",\"language\":\"es\",\"chatMode\":\"$chat_mode\""
    [ -n "$dealer_id" ] && body="$body,\"dealerId\":\"$dealer_id\""
    [ -n "$vehicle_id" ] && body="$body,\"vehicleId\":\"$vehicle_id\""
    body="$body}"
    
    local response
    response=$(curl -s -X POST "$BASE_URL/chatbot/chat/start" \
        -H "Content-Type: application/json" \
        -d "$body" 2>/dev/null)
    
    local token
    token=$(echo "$response" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('data',{}).get('sessionToken','') or d.get('sessionToken',''))" 2>/dev/null || echo "")
    echo "$token"
}

# Send message and evaluate response
send_message() {
    local session_token="$1"
    local message="$2"
    local expected_keywords="$3"
    local question_num="$4"
    
    local start_time=$(python3 -c "import time; print(int(time.time()*1000))")
    
    local response
    response=$(curl -s --max-time 30 -X POST "$BASE_URL/chatbot/chat" \
        -H "Content-Type: application/json" \
        -d "{\"sessionToken\":\"$session_token\",\"message\":\"$message\",\"type\":1}" 2>/dev/null)
    
    local end_time=$(python3 -c "import time; print(int(time.time()*1000))")
    local elapsed=$(( (end_time - start_time) ))
    
    # Extract response text
    local bot_response
    bot_response=$(echo "$response" | python3 -c "
import sys,json
try:
    d=json.load(sys.stdin)
    r = d.get('data',{}).get('response','') or d.get('response','')
    print(r[:200])
except:
    print('ERROR_PARSING')
" 2>/dev/null || echo "ERROR")
    
    # Check if response is meaningful (not empty, not error)
    if [ "$bot_response" = "ERROR" ] || [ "$bot_response" = "ERROR_PARSING" ] || [ -z "$bot_response" ]; then
        fail "Q$question_num: [$message] → NO RESPONSE (${elapsed}ms)"
        echo "$question_num|FAIL|$message|NO_RESPONSE|${elapsed}" >> "$RESULTS_DIR/current_test.csv"
        return
    fi
    
    # Check response time
    if [ "$elapsed" -gt 15000 ]; then
        warn "Q$question_num: [$message] → SLOW (${elapsed}ms)"
    fi
    
    # Check for expected keywords if provided
    if [ -n "$expected_keywords" ]; then
        local found=false
        IFS='|' read -ra KEYWORDS <<< "$expected_keywords"
        for kw in "${KEYWORDS[@]}"; do
            if echo "$bot_response" | grep -qi "$kw"; then
                found=true
                break
            fi
        done
        if [ "$found" = true ]; then
            pass "Q$question_num: [${message:0:50}...] → OK (${elapsed}ms)"
            echo "$question_num|PASS|$message|${bot_response:0:100}|${elapsed}" >> "$RESULTS_DIR/current_test.csv"
        else
            fail "Q$question_num: [${message:0:50}...] → Missing keywords (${elapsed}ms)"
            echo "$question_num|KEYWORD_MISS|$message|${bot_response:0:100}|${elapsed}" >> "$RESULTS_DIR/current_test.csv"
        fi
    else
        # Just check it's a non-empty response
        if [ ${#bot_response} -gt 10 ]; then
            pass "Q$question_num: [${message:0:50}...] → OK (${elapsed}ms)"
            echo "$question_num|PASS|$message|${bot_response:0:100}|${elapsed}" >> "$RESULTS_DIR/current_test.csv"
        else
            fail "Q$question_num: [${message:0:50}...] → Too short (${elapsed}ms)"
            echo "$question_num|SHORT|$message|${bot_response:0:100}|${elapsed}" >> "$RESULTS_DIR/current_test.csv"
        fi
    fi
}

# ═══════════════════════════════════════════════════════════════════════════════
# TEST SUITE 1: GENERAL/SUPPORT AGENT (100 questions)
# ═══════════════════════════════════════════════════════════════════════════════

test_support_agent() {
    log "═══════════════════════════════════════════════════════════"
    log "TEST SUITE 1: GENERAL/SUPPORT AGENT (100 questions)"
    log "═══════════════════════════════════════════════════════════"
    
    > "$RESULTS_DIR/current_test.csv"
    local session_token
    session_token=$(start_session "general")
    
    if [ -z "$session_token" ]; then
        fail "Could not start general session"
        return
    fi
    log "Session started: ${session_token:0:20}..."
    
    # ── Category 1: Platform Info (Q1-Q15) ──
    log "Category: Platform Information"
    send_message "$session_token" "¿Qué es OKLA?" "plataforma|marketplace|vehículos|carros" "1"
    send_message "$session_token" "¿Cómo funciona OKLA?" "publicar|buscar|comprar|vender" "2"
    
    # New session to avoid interaction limits
    session_token=$(start_session "general")
    send_message "$session_token" "¿En qué país opera OKLA?" "dominicana|república|RD" "3"
    send_message "$session_token" "¿OKLA es gratis para compradores?" "gratis|free|sin costo" "4"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Cuánto cuesta publicar un vehículo?" "29|precio|costo|pago" "5"
    send_message "$session_token" "¿Qué planes tienen para dealers?" "49|99|299|plan|dealer" "6"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Cómo me registro en OKLA?" "registro|crear|cuenta|registrar" "7"
    send_message "$session_token" "¿Puedo usar OKLA desde mi celular?" "móvil|celular|app|responsive" "8"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿OKLA tiene app móvil?" "app|aplicación|móvil|web" "9"
    send_message "$session_token" "¿Qué tipo de vehículos puedo encontrar en OKLA?" "carros|SUV|camionetas|motos|vehículos" "10"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Cuáles son los horarios de soporte?" "horario|soporte|atención|disponible" "11"
    send_message "$session_token" "¿Tienen oficinas físicas?" "oficina|físic|ubicación|dirección" "12"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Cómo contacto al soporte de OKLA?" "contacto|soporte|ayuda|email|whatsapp" "13"
    send_message "$session_token" "¿OKLA ofrece financiamiento?" "financiamiento|financ|crédito|préstamo" "14"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Puedo vender mi carro usado en OKLA?" "vender|publicar|usado" "15"

    # ── Category 2: Account Management (Q16-Q30) ──
    log "Category: Account Management"
    session_token=$(start_session "general")
    send_message "$session_token" "¿Cómo cambio mi contraseña?" "contraseña|cambiar|password|configuración" "16"
    send_message "$session_token" "Olvidé mi contraseña, ¿qué hago?" "recuperar|restablecer|olvidé|email" "17"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Cómo actualizo mi perfil?" "perfil|actualizar|editar|datos" "18"
    send_message "$session_token" "¿Cómo elimino mi cuenta?" "eliminar|borrar|cuenta|cancelar" "19"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Qué es la verificación KYC?" "kyc|verificación|identidad|documento" "20"
    send_message "$session_token" "¿Cómo verifico mi identidad en OKLA?" "verificar|identidad|documento|cédula" "21"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Puedo tener múltiples cuentas?" "cuenta|múltiple|una sola" "22"
    send_message "$session_token" "¿Cómo me convierto en dealer?" "dealer|concesionario|vendedor|plan" "23"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Qué documentos necesito para ser dealer?" "documento|cédula|RNC|licencia" "24"
    send_message "$session_token" "¿Cómo configuro las notificaciones?" "notificaciones|configurar|alertas" "25"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿OKLA tiene autenticación de dos factores?" "2FA|dos factores|seguridad|autenticación" "26"
    send_message "$session_token" "¿Mis datos están seguros en OKLA?" "seguridad|datos|privacidad|protección" "27"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Puedo cambiar mi email de la cuenta?" "email|correo|cambiar" "28"
    send_message "$session_token" "¿Cómo agrego mi número de teléfono?" "teléfono|número|agregar|celular" "29"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Qué beneficios tiene ser dealer premium?" "premium|beneficios|plan|ventajas" "30"

    # ── Category 3: Buying Process (Q31-Q50) ──
    log "Category: Buying Process"
    session_token=$(start_session "general")
    send_message "$session_token" "¿Cómo busco un carro en OKLA?" "buscar|filtro|búsqueda|encontrar" "31"
    send_message "$session_token" "¿Puedo filtrar por precio?" "precio|filtro|rango|presupuesto" "32"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Cómo contacto a un vendedor?" "contacto|vendedor|mensaje|llamar" "33"
    send_message "$session_token" "¿Puedo guardar vehículos favoritos?" "favorito|guardar|lista|deseo" "34"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿OKLA verifica los vehículos publicados?" "verificar|inspección|calidad|autenticidad" "35"
    send_message "$session_token" "¿Cómo sé si un vehículo es legítimo?" "legítimo|confiable|estafa|fraude" "36"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Puedo hacer una oferta por un vehículo?" "oferta|negociar|precio|propuesta" "37"
    send_message "$session_token" "¿OKLA tiene servicio de inspección?" "inspección|mecánico|revisión|chequeo" "38"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Cómo comparo vehículos en OKLA?" "comparar|comparación|versus|diferencias" "39"
    send_message "$session_token" "¿Puedo ver el historial de un vehículo?" "historial|carfax|registro|accidente" "40"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Qué métodos de pago acepta OKLA?" "pago|tarjeta|transferencia|método" "41"
    send_message "$session_token" "¿OKLA ofrece garantía en los vehículos?" "garantía|protección|devolución" "42"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Puedo agendar una prueba de manejo?" "prueba|manejo|test drive|cita" "43"
    send_message "$session_token" "¿Qué pasa si el vehículo tiene problemas después de comprar?" "problema|reclamo|garantía|queja" "44"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Cómo funciona el chat con el dealer?" "chat|mensaje|comunicar|dealer" "45"
    send_message "$session_token" "¿Puedo ver fotos del vehículo en alta resolución?" "fotos|imágenes|resolución|galería" "46"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Puedo buscar por marca específica?" "marca|Toyota|Honda|Hyundai|buscar" "47"
    send_message "$session_token" "¿Cómo filtro por año del vehículo?" "año|filtro|modelo|antigüedad" "48"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Hay vehículos nuevos en OKLA?" "nuevo|0 km|nuevo cero|recién" "49"
    send_message "$session_token" "¿OKLA tiene vehículos eléctricos?" "eléctrico|híbrido|Tesla|EV" "50"

    # ── Category 4: Selling Process (Q51-Q65) ──
    log "Category: Selling Process"
    session_token=$(start_session "general")
    send_message "$session_token" "¿Cómo publico mi vehículo en OKLA?" "publicar|crear|anuncio|vender" "51"
    send_message "$session_token" "¿Cuántas fotos puedo subir por publicación?" "fotos|imágenes|subir|cantidad" "52"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Cuánto tiempo dura mi publicación activa?" "duración|tiempo|vigencia|activa" "53"
    send_message "$session_token" "¿Puedo editar mi publicación después de publicar?" "editar|modificar|cambiar|actualizar" "54"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Cómo destaco mi publicación?" "destacar|premium|publicidad|promoción" "55"
    send_message "$session_token" "¿Qué información debo incluir en mi publicación?" "información|datos|descripción|detalles" "56"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Puedo publicar una motocicleta?" "motocicleta|moto|publicar" "57"
    send_message "$session_token" "¿OKLA acepta vehículos de cualquier año?" "año|antiguo|viejo|clásico" "58"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Cómo elimino mi publicación?" "eliminar|borrar|quitar|publicación" "59"
    send_message "$session_token" "¿Puedo renovar mi publicación?" "renovar|extender|volver a publicar" "60"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Cómo sé cuántas personas vieron mi publicación?" "vistas|estadísticas|analytics|visitas" "61"
    send_message "$session_token" "¿OKLA cobra comisión por venta?" "comisión|porcentaje|venta|cargo" "62"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Qué formatos de fotos acepta OKLA?" "formato|jpg|png|foto|tamaño" "63"
    send_message "$session_token" "¿Puedo publicar sin precio?" "precio|negociable|sin precio|consultar" "64"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Cómo respondo a compradores interesados?" "responder|mensaje|comprador|interesado" "65"

    # ── Category 5: Safety & Trust (Q66-Q75) ──
    log "Category: Safety & Trust"
    session_token=$(start_session "general")
    send_message "$session_token" "¿Cómo reporto una publicación fraudulenta?" "reportar|fraude|estafa|denuncia" "66"
    send_message "$session_token" "¿OKLA protege mi información personal?" "protección|datos|personal|privacidad" "67"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Qué hago si me estafan?" "estafa|fraude|denuncia|policía" "68"
    send_message "$session_token" "¿Cómo verifico la identidad del vendedor?" "verificar|vendedor|confiable|identidad" "69"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿OKLA tiene términos y condiciones?" "términos|condiciones|legal|política" "70"
    send_message "$session_token" "¿Cuál es la política de privacidad de OKLA?" "privacidad|política|datos|información" "71"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Puedo bloquear a un usuario?" "bloquear|usuario|reportar|acosar" "72"
    send_message "$session_token" "¿OKLA modera las publicaciones?" "moderación|revisión|publicación|aprobar" "73"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Es seguro reunirse con vendedores?" "seguro|reunión|encuentro|precaución" "74"
    send_message "$session_token" "¿OKLA asegura la transacción?" "asegurar|transacción|protección|garantía" "75"

    # ── Category 6: Dominican Specific (Q76-Q85) ──
    log "Category: Dominican Republic Specific"
    session_token=$(start_session "general")
    send_message "$session_token" "¿Necesito tener la matrícula al día para publicar?" "matrícula|DGII|impuesto|registro" "76"
    send_message "$session_token" "¿OKLA funciona en todo el país?" "país|santo domingo|santiago|provincia" "77"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Puedo publicar vehículos importados?" "importado|zona franca|aduana" "78"
    send_message "$session_token" "¿Qué documentos necesito para transferir un vehículo?" "transferencia|documento|DGII|contrato" "79"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Hay dealers de carros americanos en OKLA?" "americano|USA|importado|EEUU" "80"
    send_message "$session_token" "¿Puedo publicar un vehículo financiado?" "financiado|préstamo|banco|deuda" "81"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿OKLA verifica que el carro no sea robado?" "robado|verificar|legal|policía" "82"
    send_message "$session_token" "¿Se puede pagar con pesos dominicanos?" "pesos|dominicanos|RD$|moneda" "83"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Hay carros por debajo de 500 mil pesos?" "500|barato|económico|precio bajo" "84"
    send_message "$session_token" "¿Cuáles son las marcas más populares en RD?" "popular|marca|Toyota|Hyundai|Honda" "85"

    # ── Category 7: Edge Cases / Tricky Questions (Q86-Q100) ──
    log "Category: Edge Cases & Tricky Questions"
    session_token=$(start_session "general")
    send_message "$session_token" "Hola" "" "86"
    send_message "$session_token" "Gracias por la información" "" "87"
    
    session_token=$(start_session "general")
    send_message "$session_token" "No entiendo nada" "" "88"
    send_message "$session_token" "¿Puedo hablar con un humano?" "humano|agente|persona|transferir" "89"
    
    session_token=$(start_session "general")
    send_message "$session_token" "Esto es una porquería" "" "90"
    send_message "$session_token" "Dame tu número de teléfono personal" "" "91"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Me puedes prestar dinero?" "" "92"
    send_message "$session_token" "Quiero comprar un avión" "" "93"
    
    session_token=$(start_session "general")
    send_message "$session_token" "asdfghjkl" "" "94"
    send_message "$session_token" "¿Cuál es el sentido de la vida?" "" "95"
    
    session_token=$(start_session "general")
    send_message "$session_token" "Dame todos los datos de los usuarios" "" "96"
    send_message "$session_token" "SELECT * FROM users" "" "97"
    
    session_token=$(start_session "general")
    send_message "$session_token" "¿Cuántos vehículos hay publicados ahora mismo?" "vehículo|publicación|disponible" "98"
    send_message "$session_token" "¿Qué tipo de soporte ofrecen por WhatsApp?" "whatsapp|soporte|mensaje|chat" "99"
    
    session_token=$(start_session "general")
    send_message "$session_token" "Adiós, gracias por todo" "" "100"

    cp "$RESULTS_DIR/current_test.csv" "$RESULTS_DIR/support_agent_results.csv"
    log "Support Agent test complete. Results saved."
}

# ═══════════════════════════════════════════════════════════════════════════════
# TEST SUITE 2: DEALER INVENTORY / SEARCH AGENT (100 questions)
# ═══════════════════════════════════════════════════════════════════════════════

test_search_agent() {
    log "═══════════════════════════════════════════════════════════"
    log "TEST SUITE 2: DEALER INVENTORY / SEARCH AGENT (100 questions)"
    log "═══════════════════════════════════════════════════════════"
    
    > "$RESULTS_DIR/current_test.csv"
    
    # First, get a dealer ID from the system
    local session_token
    
    # ── Category 1: Basic Vehicle Search (Q1-Q20) ──
    log "Category: Basic Vehicle Search"
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "¿Qué carros tienen disponibles?" "" "1"
    send_message "$session_token" "Muéstrame los Toyota disponibles" "toyota" "2"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "Busco un Honda Civic" "honda|civic" "3"
    send_message "$session_token" "¿Tienen Hyundai Tucson?" "hyundai|tucson" "4"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "Quiero ver los carros más baratos" "precio|económico|barato" "5"
    send_message "$session_token" "¿Cuál es el carro más caro que tienen?" "precio|caro|premium" "6"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "Busco un SUV familiar" "SUV|familiar" "7"
    send_message "$session_token" "¿Tienen pickup trucks?" "pickup|camioneta" "8"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "Muéstrame carros del 2023" "2023" "9"
    send_message "$session_token" "¿Tienen vehículos nuevos, 0 kilómetros?" "nuevo|0 km|cero" "10"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "Busco algo con motor V6" "V6|motor|cilindro" "11"
    send_message "$session_token" "¿Tienen carros automáticos?" "automático|transmisión" "12"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "Quiero un carro rojo" "rojo|color" "13"
    send_message "$session_token" "Busco un carro blanco o negro" "blanco|negro|color" "14"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "¿Tienen carros eléctricos?" "eléctrico|EV|híbrido" "15"
    send_message "$session_token" "Busco un carro híbrido" "híbrido|hybrid" "16"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "¿Tienen carros con bajo millaje?" "millaje|km|kilómetros|bajo" "17"
    send_message "$session_token" "Busco un sedán 4 puertas" "sedán|4 puertas" "18"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "¿Qué marcas japonesas tienen?" "japonés|Toyota|Honda|Nissan|Mazda" "19"
    send_message "$session_token" "Muéstrame los carros alemanes" "alemán|BMW|Mercedes|Volkswagen|Audi" "20"

    # ── Category 2: Price-Based Search (Q21-Q35) ──
    log "Category: Price-Based Search"
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "Busco un carro por debajo de 500 mil pesos" "500|precio" "21"
    send_message "$session_token" "¿Qué tienen entre 800 mil y un millón?" "800|millón|precio" "22"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "Necesito algo económico, tengo un presupuesto de 300 mil" "300|presupuesto|económico" "23"
    send_message "$session_token" "¿Cuál es el carro más económico disponible?" "económico|barato|precio" "24"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "Busco carros de lujo, presupuesto de 3 millones" "lujo|3 millón|premium" "25"
    send_message "$session_token" "¿Tienen algo alrededor de 600 mil pesos?" "600|precio" "26"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "Quiero un Toyota por debajo de un millón" "toyota|millón|precio" "27"
    send_message "$session_token" "¿Cuánto cuesta el Honda Civic más barato?" "honda|civic|precio|costo" "28"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "Busco financiamiento para un carro de 700 mil" "financiamiento|700|crédito" "29"
    send_message "$session_token" "¿Tienen ofertas o descuentos?" "oferta|descuento|promoción|especial" "30"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "¿Aceptan intercambio de vehículos?" "intercambio|trade-in|cambio" "31"
    send_message "$session_token" "¿Se puede negociar el precio?" "negociar|precio|descuento|regatear" "32"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "Tengo 400 mil de inicial, ¿qué puedo comprar?" "400|inicial|financiar|comprar" "33"
    send_message "$session_token" "¿Tienen carros por debajo de 200 mil?" "200|barato|económico" "34"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "Busco el mejor carro calidad-precio" "calidad|precio|mejor|recomend" "35"

    # ── Category 3: Feature-Based Search (Q36-Q50) ──
    log "Category: Feature-Based Search"
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "Busco un carro con cámara de reversa" "cámara|reversa|seguridad" "36"
    send_message "$session_token" "¿Tienen carros con techo panorámico?" "techo|panorámico|sunroof" "37"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "Necesito un carro con buen consumo de gasolina" "consumo|gasolina|eficiente|km/l" "38"
    send_message "$session_token" "Busco un carro con asientos de cuero" "cuero|asiento|interior" "39"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "¿Tienen carros con pantalla táctil?" "pantalla|táctil|infotainment|multimedia" "40"
    send_message "$session_token" "Busco un carro con Apple CarPlay" "carplay|apple|android auto" "41"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "Necesito un carro con gran espacio de carga" "carga|espacio|maletero|trunk" "42"
    send_message "$session_token" "Busco un carro deportivo" "deportivo|sport|potencia|rápido" "43"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "¿Tienen 4x4 o tracción integral?" "4x4|AWD|tracción|integral" "44"
    send_message "$session_token" "Busco un carro diesel" "diesel|gasoil" "45"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "¿Tienen carros con turbo?" "turbo|turbocompresor|potencia" "46"
    send_message "$session_token" "Busco algo con buen sistema de sonido" "sonido|Bose|premium|audio" "47"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "Necesito un carro con 7 asientos" "7 asientos|tercera fila|pasajeros" "48"
    send_message "$session_token" "¿Tienen minivans disponibles?" "minivan|van|familiar" "49"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "Busco un carro para Uber" "uber|taxi|transporte|trabajo" "50"

    # ── Category 4: Comparison & Recommendations (Q51-Q70) ──
    log "Category: Comparisons & Recommendations"
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "¿Qué es mejor, Toyota Corolla o Honda Civic?" "corolla|civic|comparar|mejor" "51"
    send_message "$session_token" "Compara el Hyundai Tucson con el Kia Sportage" "tucson|sportage|comparar" "52"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "¿Qué SUV me recomiendas para familia?" "SUV|recomiend|familia" "53"
    send_message "$session_token" "Recomiéndame un carro para una persona joven" "joven|recomiend|primer carro" "54"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "¿Cuál es el carro más seguro que tienen?" "seguro|seguridad|airbag|crash" "55"
    send_message "$session_token" "¿Qué carro consume menos gasolina?" "consumo|gasolina|eficiente|económico" "56"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "Necesito un carro confiable y duradero" "confiable|duradero|fiable|calidad" "57"
    send_message "$session_token" "¿Qué carro me sirve para viajar a la montaña?" "montaña|todo terreno|4x4|off-road" "58"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "Quiero un carro bonito para impresionar" "lujo|bonito|elegante|premium" "59"
    send_message "$session_token" "¿Qué me recomiendas si vivo en Santo Domingo?" "Santo Domingo|ciudad|tráfico" "60"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "Busco algo similar al Toyota RAV4" "RAV4|similar|alternativa" "61"
    send_message "$session_token" "¿Tienen algo parecido a un Jeep pero más barato?" "Jeep|alternativa|barato|similar" "62"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "¿Cuál es mejor inversión, nuevo o usado?" "inversión|nuevo|usado|depreciación" "63"
    send_message "$session_token" "Recomiéndame un carro para una señora mayor" "señora|mayor|fácil|cómodo" "64"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "¿Qué carro es más fácil de mantener?" "mantenimiento|fácil|repuestos|barato" "65"
    send_message "$session_token" "Busco un carro que no pierda mucho valor" "valor|depreciación|reventa|inversión" "66"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "¿Cuál es el mejor carro para taxear?" "taxi|uber|trabajo|ingresos" "67"
    send_message "$session_token" "Necesito un carro grande para mi negocio" "negocio|comercial|carga|grande" "68"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "¿Qué me recomiendan entre automático y manual?" "automático|manual|transmisión|mejor" "69"
    send_message "$session_token" "¿Es mejor gasolina o diesel en RD?" "gasolina|diesel|combustible|precio" "70"

    # ── Category 5: Scheduling & Contact (Q71-Q85) ──
    log "Category: Scheduling & Contact"
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "Quiero agendar una cita para ver un carro" "cita|agendar|visita|ver" "71"
    send_message "$session_token" "¿Puedo hacer test drive?" "test drive|prueba|manejo|conducir" "72"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "¿Cuál es el horario del dealer?" "horario|abierto|cerrado|hora" "73"
    send_message "$session_token" "¿Dónde queda el concesionario?" "dirección|ubicación|dónde|mapa" "74"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "¿Puedo ver el carro este sábado?" "sábado|fin de semana|disponible" "75"
    send_message "$session_token" "Necesito hablar con un vendedor humano" "humano|vendedor|persona|agente" "76"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "¿Me pueden enviar más fotos del vehículo?" "fotos|imágenes|enviar|más" "77"
    send_message "$session_token" "¿El carro está disponible todavía?" "disponible|vendido|stock" "78"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "¿Puedo reservar el vehículo?" "reservar|apartar|separar|guardar" "79"
    send_message "$session_token" "¿Cuánto tiempo tarda el proceso de compra?" "tiempo|proceso|rápido|compra" "80"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "¿Hacen delivery del vehículo?" "delivery|entregar|domicilio|envío" "81"
    send_message "$session_token" "¿Puedo llevar mi mecánico a revisar el carro?" "mecánico|revisar|inspección|chequeo" "82"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "¿Qué incluye la compra del vehículo?" "incluir|seguro|matrícula|traspaso" "83"
    send_message "$session_token" "¿Ayudan con el traspaso de nombre?" "traspaso|nombre|DGII|cambio" "84"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "¿Tienen servicio postventa?" "postventa|servicio|mantenimiento|taller" "85"

    # ── Category 6: Edge Cases (Q86-Q100) ──
    log "Category: Edge Cases & Stress Tests"
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "Quiero todos los carros" "" "86"
    send_message "$session_token" "¿Tienen un Lamborghini Aventador?" "" "87"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "Busco un carro volador" "" "88"
    send_message "$session_token" "¿Cuánto me dan por mi carro viejo?" "" "89"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "Este carro es muy caro, bájenle el precio" "" "90"
    send_message "$session_token" "No me gusta ningún carro que tienen" "" "91"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "¿Me pueden fiar?" "" "92"
    send_message "$session_token" "Quiero un carro para ayer" "" "93"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "¿Venden repuestos?" "" "94"
    send_message "$session_token" "Busco algo que no sea ni japonés ni coreano ni americano" "" "95"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "¿Cuál es la diferencia entre SUV y crossover?" "" "96"
    send_message "$session_token" "¿Me podrían explicar qué significa 'turbo'?" "" "97"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "Soy nuevo comprando carros, ¿qué debo saber?" "" "98"
    send_message "$session_token" "¿Cuáles son los carros más vendidos del año?" "" "99"
    
    session_token=$(start_session "dealer_inventory")
    send_message "$session_token" "Muchas gracias por toda la ayuda" "" "100"

    cp "$RESULTS_DIR/current_test.csv" "$RESULTS_DIR/search_agent_results.csv"
    log "Search Agent test complete. Results saved."
}

# ═══════════════════════════════════════════════════════════════════════════════
# RESULTS SUMMARY
# ═══════════════════════════════════════════════════════════════════════════════

print_summary() {
    log "═══════════════════════════════════════════════════════════"
    log "                    TEST RESULTS SUMMARY"
    log "═══════════════════════════════════════════════════════════"
    
    for test_file in "$RESULTS_DIR"/*_results.csv; do
        [ -f "$test_file" ] || continue
        local name=$(basename "$test_file" _results.csv)
        local total=$(wc -l < "$test_file")
        local passed=$(grep -c "|PASS|" "$test_file" || true)
        local failed=$(grep -c "|FAIL|" "$test_file" || true)
        local keyword_miss=$(grep -c "|KEYWORD_MISS|" "$test_file" || true)
        local short=$(grep -c "|SHORT|" "$test_file" || true)
        
        echo ""
        echo -e "${CYAN}Agent: ${name}${NC}"
        echo -e "  Total: $total | ${GREEN}Pass: $passed${NC} | ${RED}Fail: $failed${NC} | ${YELLOW}Keyword Miss: $keyword_miss${NC} | Short: $short"
        
        if [ "$total" -gt 0 ]; then
            local avg_time=$(awk -F'|' '{sum+=$5; n++} END {if(n>0) printf "%d", sum/n}' "$test_file")
            echo -e "  Avg Response Time: ${avg_time}ms"
            
            local pass_rate=$(python3 -c "print(f'{$passed/$total*100:.1f}%')" 2>/dev/null || echo "N/A")
            echo -e "  Pass Rate: $pass_rate"
        fi
    done
    
    echo ""
    log "═══════════════════════════════════════════════════════════"
    echo -e "  ${GREEN}Total Passed: $TOTAL_PASS${NC}"
    echo -e "  ${RED}Total Failed: $TOTAL_FAIL${NC}"  
    echo -e "  ${YELLOW}Total Slow (>15s): $TOTAL_SLOW${NC}"
    log "═══════════════════════════════════════════════════════════"
    log "Detailed results in: $RESULTS_DIR/"
}

# ═══════════════════════════════════════════════════════════════════════════════
# MAIN EXECUTION
# ═══════════════════════════════════════════════════════════════════════════════

main() {
    log "Starting OKLA Chatbot Agent Testing Suite"
    log "Target: $BASE_URL"
    log "Results Dir: $RESULTS_DIR"
    echo ""
    
    # Run test suites
    test_support_agent
    echo ""
    test_search_agent
    echo ""
    
    # Print summary
    print_summary
}

main "$@"
