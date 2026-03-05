#!/bin/bash
# =============================================================================
# OKLA Frontend Documentation Validator
# =============================================================================
# Valida que todos los documentos cumplan el estándar establecido
# 
# Uso: ./validate-docs.sh [--verbose] [--fix]
#
# Opciones:
#   --verbose  Muestra detalles de cada archivo
#   --fix      Intenta corregir problemas automáticamente
#
# Autor: Gregory Moreno
# Fecha: Enero 2026
# =============================================================================

set -e

# Colores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Contadores
TOTAL_FILES=0
VALID_FILES=0
INVALID_FILES=0
WARNINGS=0

# Flags
VERBOSE=false
FIX_MODE=false

# Parse arguments
for arg in "$@"; do
    case $arg in
        --verbose)
            VERBOSE=true
            ;;
        --fix)
            FIX_MODE=true
            ;;
    esac
done

# Directorio base
DOCS_DIR="$(dirname "$0")"

echo ""
echo "═══════════════════════════════════════════════════════════════════════════"
echo "                    🔍 OKLA Documentation Validator"
echo "═══════════════════════════════════════════════════════════════════════════"
echo ""
echo "📁 Validando: $DOCS_DIR"
echo ""

# =============================================================================
# Función: Validar un archivo de documentación
# =============================================================================
validate_doc() {
    local file="$1"
    local filename=$(basename "$file")
    local issues=()
    
    # Incrementar contador
    ((TOTAL_FILES++))
    
    # 1. Verificar que existe contenido
    if [ ! -s "$file" ]; then
        issues+=("❌ Archivo vacío")
    fi
    
    # 2. Verificar header H1
    if ! grep -q "^# " "$file" 2>/dev/null; then
        issues+=("❌ Falta header H1")
    fi
    
    # 3. Verificar secciones obligatorias para docs en 04-PAGINAS
    if [[ "$file" == *"04-PAGINAS"* ]]; then
        # Verificar sección de Componentes
        if ! grep -qi "## .*Componente" "$file" 2>/dev/null && \
           ! grep -qi "## .*Component" "$file" 2>/dev/null; then
            issues+=("⚠️  Falta sección de Componentes")
            ((WARNINGS++))
        fi
        
        # Verificar sección de API
        if ! grep -qi "## .*API" "$file" 2>/dev/null && \
           ! grep -qi "## .*Endpoint" "$file" 2>/dev/null; then
            issues+=("⚠️  Falta sección de API/Endpoints")
            ((WARNINGS++))
        fi
        
        # Verificar sección de Tests E2E (Auditoría 10)
        if ! grep -qi "## .*E2E\|## .*Playwright\|## .*Test" "$file" 2>/dev/null; then
            issues+=("⚠️  Falta sección de Tests E2E")
            ((WARNINGS++))
        fi
        
        # Verificar sección de Accesibilidad (Auditoría 8)
        if ! grep -qi "Accesibilidad\|Accessibility\|WCAG\|a11y" "$file" 2>/dev/null; then
            issues+=("⚠️  Falta sección de Accesibilidad")
            ((WARNINGS++))
        fi
    fi
    
    # 4. Verificar links rotos internos
    local broken_links=$(grep -oE '\[.*\]\(\.\.?/[^)]+\.md\)' "$file" 2>/dev/null | while read -r link; do
        # Extraer path del link
        path=$(echo "$link" | sed 's/.*(\(.*\))/\1/')
        dir=$(dirname "$file")
        full_path="$dir/$path"
        
        # Normalizar path
        full_path=$(cd "$(dirname "$full_path")" 2>/dev/null && pwd)/$(basename "$full_path")
        
        if [ ! -f "$full_path" ] 2>/dev/null; then
            echo "$path"
        fi
    done)
    
    if [ -n "$broken_links" ]; then
        issues+=("❌ Links rotos: $broken_links")
    fi
    
    # 5. Contar líneas
    local line_count=$(wc -l < "$file")
    if [ "$line_count" -lt 50 ]; then
        issues+=("⚠️  Documento muy corto ($line_count líneas)")
        ((WARNINGS++))
    fi
    
    # Reportar resultado
    if [ ${#issues[@]} -eq 0 ]; then
        ((VALID_FILES++))
        if [ "$VERBOSE" = true ]; then
            echo -e "${GREEN}✅ $filename${NC}"
        fi
    else
        # Verificar si solo son warnings
        has_errors=false
        for issue in "${issues[@]}"; do
            if [[ "$issue" == "❌"* ]]; then
                has_errors=true
                break
            fi
        done
        
        if [ "$has_errors" = true ]; then
            ((INVALID_FILES++))
            echo -e "${RED}❌ $filename${NC}"
        else
            ((VALID_FILES++))
            if [ "$VERBOSE" = true ]; then
                echo -e "${YELLOW}⚠️  $filename${NC}"
            fi
        fi
        
        if [ "$VERBOSE" = true ]; then
            for issue in "${issues[@]}"; do
                echo -e "   $issue"
            done
        fi
    fi
}

# =============================================================================
# Validar estructura de carpetas
# =============================================================================
echo "📂 Verificando estructura de carpetas..."
echo ""

required_folders=(
    "01-SETUP"
    "02-UX-DESIGN-SYSTEM"
    "03-COMPONENTES"
    "04-PAGINAS"
    "05-API-INTEGRATION"
    "06-TESTING"
)

for folder in "${required_folders[@]}"; do
    if [ -d "$DOCS_DIR/$folder" ]; then
        echo -e "${GREEN}✅ $folder/${NC}"
    else
        echo -e "${RED}❌ $folder/ - NO ENCONTRADO${NC}"
        ((INVALID_FILES++))
    fi
done

echo ""

# =============================================================================
# Verificar subcarpetas de 04-PAGINAS
# =============================================================================
echo "📂 Verificando subcarpetas de 04-PAGINAS..."
echo ""

paginas_subfolders=(
    "01-PUBLICO"
    "02-AUTH"
    "03-COMPRADOR"
    "04-VENDEDOR"
    "05-DEALER"
    "06-ADMIN"
    "07-PAGOS"
    "08-DGII-COMPLIANCE"
    "09-COMPONENTES-COMUNES"
)

for folder in "${paginas_subfolders[@]}"; do
    if [ -d "$DOCS_DIR/04-PAGINAS/$folder" ]; then
        count=$(find "$DOCS_DIR/04-PAGINAS/$folder" -name "*.md" | wc -l)
        echo -e "${GREEN}✅ 04-PAGINAS/$folder/ ($count archivos)${NC}"
    else
        echo -e "${RED}❌ 04-PAGINAS/$folder/ - NO ENCONTRADO${NC}"
        ((INVALID_FILES++))
    fi
done

echo ""

# =============================================================================
# Validar archivos individuales
# =============================================================================
echo "📄 Validando archivos de documentación..."
echo ""

# Validar archivos raíz
for file in "$DOCS_DIR"/*.md; do
    if [ -f "$file" ]; then
        validate_doc "$file"
    fi
done

# Validar archivos en subcarpetas
for folder in "${required_folders[@]}"; do
    if [ -d "$DOCS_DIR/$folder" ]; then
        for file in "$DOCS_DIR/$folder"/*.md; do
            if [ -f "$file" ]; then
                validate_doc "$file"
            fi
        done
    fi
done

# Validar archivos en 04-PAGINAS subcarpetas
for subfolder in "${paginas_subfolders[@]}"; do
    if [ -d "$DOCS_DIR/04-PAGINAS/$subfolder" ]; then
        for file in "$DOCS_DIR/04-PAGINAS/$subfolder"/*.md; do
            if [ -f "$file" ]; then
                validate_doc "$file"
            fi
        done
    fi
done

echo ""

# =============================================================================
# Verificar índices
# =============================================================================
echo "📋 Verificando índices..."
echo ""

indices=(
    "00-INDICE-MAESTRO.md"
    "04-PAGINAS/00-INDICE.md"
)

for idx in "${indices[@]}"; do
    if [ -f "$DOCS_DIR/$idx" ]; then
        echo -e "${GREEN}✅ $idx${NC}"
    else
        echo -e "${YELLOW}⚠️  $idx - No encontrado${NC}"
        ((WARNINGS++))
    fi
done

echo ""

# =============================================================================
# Resumen final
# =============================================================================
echo "═══════════════════════════════════════════════════════════════════════════"
echo "                           📊 RESUMEN"
echo "═══════════════════════════════════════════════════════════════════════════"
echo ""
echo -e "📁 Total archivos validados: ${BLUE}$TOTAL_FILES${NC}"
echo -e "✅ Válidos:                  ${GREEN}$VALID_FILES${NC}"
echo -e "❌ Con errores:              ${RED}$INVALID_FILES${NC}"
echo -e "⚠️  Warnings:                 ${YELLOW}$WARNINGS${NC}"
echo ""

# Calcular porcentaje
if [ $TOTAL_FILES -gt 0 ]; then
    PERCENTAGE=$((VALID_FILES * 100 / TOTAL_FILES))
    echo -e "📈 Tasa de validación: ${BLUE}$PERCENTAGE%${NC}"
fi

echo ""

# Exit code
if [ $INVALID_FILES -gt 0 ]; then
    echo -e "${RED}❌ Validación FALLIDA - Hay $INVALID_FILES archivos con errores${NC}"
    exit 1
else
    echo -e "${GREEN}✅ Validación EXITOSA${NC}"
    exit 0
fi
