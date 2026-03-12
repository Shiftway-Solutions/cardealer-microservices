#!/bin/bash

##############################################################################
# Script para actualizar Visual Studio Code a versión 1.111.0 en macOS
# Plataforma: Universal (ARM64 + Intel)
# Fecha: 2026-03-11
##############################################################################

set -e

# Colores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuración
VSCODE_VERSION="1.111.0"
VSCODE_APP_PATH="/Applications/Visual Studio Code.app"
TEMP_DIR="/tmp/vscode-update-$$"
DOWNLOAD_URL="https://code.visualstudio.com/sha/download?build=stable&os=darwin-universal"

echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}  Visual Studio Code — Actualizar a versión ${VSCODE_VERSION}${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo ""

# Paso 1: Crear directorio temporal
echo -e "${YELLOW}[1/6]${NC} Creando directorio temporal..."
mkdir -p "$TEMP_DIR"
echo -e "${GREEN}✓${NC} Directorio temporal creado: $TEMP_DIR"
echo ""

# Paso 2: Verificar VS Code actual
echo -e "${YELLOW}[2/6]${NC} Verificando instalación actual de VS Code..."
if [ -d "$VSCODE_APP_PATH" ]; then
    CURRENT_VERSION=$("$VSCODE_APP_PATH/Contents/Resources/app/bin/code" --version 2>/dev/null | head -1 || echo "Desconocida")
    echo -e "${GREEN}✓${NC} VS Code encontrado en: $VSCODE_APP_PATH"
    echo -e "${GREEN}✓${NC} Versión actual: $CURRENT_VERSION"
else
    echo -e "${YELLOW}⚠${NC} VS Code no está instalado en $VSCODE_APP_PATH"
fi
echo ""

# Paso 3: Crear backup de configuración (opcional pero seguro)
echo -e "${YELLOW}[3/6]${NC} Creando backup de configuración de VS Code..."
CONFIG_BACKUP="$TEMP_DIR/vscode-config-backup.tar.gz"
if [ -d "$HOME/Library/Application Support/Code" ]; then
    tar -czf "$CONFIG_BACKUP" -C "$HOME/Library/Application Support" Code 2>/dev/null || true
    echo -e "${GREEN}✓${NC} Backup de configuración creado: $CONFIG_BACKUP"
else
    echo -e "${YELLOW}⚠${NC} No se encontró configuración previa"
fi
echo ""

# Paso 4: Descargar VS Code 1.111.0
echo -e "${YELLOW}[4/6]${NC} Descargando VS Code versión ${VSCODE_VERSION}..."
DOWNLOAD_FILE="$TEMP_DIR/VSCode-darwin-universal.zip"
echo -e "${BLUE}   URL: $DOWNLOAD_URL${NC}"
echo -e "${BLUE}   Destino: $DOWNLOAD_FILE${NC}"

if command -v curl &> /dev/null; then
    curl -L --progress-bar "$DOWNLOAD_URL" -o "$DOWNLOAD_FILE" || {
        echo -e "${RED}✗ Error descargando VS Code${NC}"
        exit 1
    }
else
    echo -e "${RED}✗ curl no está disponible${NC}"
    exit 1
fi

# Verificar que la descarga fue exitosa
if [ ! -f "$DOWNLOAD_FILE" ] || [ ! -s "$DOWNLOAD_FILE" ]; then
    echo -e "${RED}✗ La descarga falló o el archivo está vacío${NC}"
    exit 1
fi

FILE_SIZE=$(du -h "$DOWNLOAD_FILE" | awk '{print $1}')
echo -e "${GREEN}✓${NC} Descarga completada: $FILE_SIZE"
echo ""

# Paso 5: Instalar VS Code
echo -e "${YELLOW}[5/6]${NC} Instalando VS Code versión ${VSCODE_VERSION}..."
echo -e "${BLUE}   Descomprimiendo...${NC}"

# Crear directorio temporal para descomprimir
UNZIP_DIR="$TEMP_DIR/vscode-unzip"
mkdir -p "$UNZIP_DIR"
unzip -q "$DOWNLOAD_FILE" -d "$UNZIP_DIR" || {
    echo -e "${RED}✗ Error descomprimiendo VS Code${NC}"
    exit 1
}

echo -e "${BLUE}   Deteniendo VS Code si está ejecutándose...${NC}"
killall "Visual Studio Code" 2>/dev/null || true
killall code 2>/dev/null || true
sleep 2

echo -e "${BLUE}   Removiendo versión anterior...${NC}"
if [ -d "$VSCODE_APP_PATH" ]; then
    rm -rf "$VSCODE_APP_PATH" || {
        echo -e "${RED}✗ Error removiendo versión anterior${NC}"
        exit 1
    }
fi

echo -e "${BLUE}   Moviendo nueva versión a /Applications/...${NC}"
if [ -d "$UNZIP_DIR/Visual Studio Code.app" ]; then
    mv "$UNZIP_DIR/Visual Studio Code.app" "$VSCODE_APP_PATH" || {
        echo -e "${RED}✗ Error instalando nueva versión${NC}"
        exit 1
    }
else
    echo -e "${RED}✗ No se encontró Visual Studio Code.app en el archivo descargado${NC}"
    exit 1
fi

echo -e "${GREEN}✓${NC} VS Code instalado en: $VSCODE_APP_PATH"
echo ""

# Paso 6: Verificar instalación
echo -e "${YELLOW}[6/6]${NC} Verificando instalación..."
sleep 2

if [ -d "$VSCODE_APP_PATH" ]; then
    INSTALLED_VERSION=$("$VSCODE_APP_PATH/Contents/Resources/app/bin/code" --version 2>/dev/null | head -1 || echo "Desconocida")
    echo -e "${GREEN}✓${NC} Versión instalada: $INSTALLED_VERSION"
    
    if [[ "$INSTALLED_VERSION" == "$VSCODE_VERSION"* ]]; then
        echo -e "${GREEN}✓${NC} ¡Instalación exitosa!"
    else
        echo -e "${YELLOW}⚠${NC} Versión instalada difiere de la esperada (esperada: $VSCODE_VERSION, instalada: $INSTALLED_VERSION)"
    fi
else
    echo -e "${RED}✗ VS Code no se encontró después de la instalación${NC}"
    exit 1
fi
echo ""

# Limpiar archivos temporales
echo -e "${BLUE}Limpiando archivos temporales...${NC}"
rm -rf "$TEMP_DIR"
echo -e "${GREEN}✓${NC} Directorio temporal eliminado"
echo ""

# Verificar que no haya otras versiones de VS Code
echo -e "${BLUE}Buscando otras versiones de VS Code en el sistema...${NC}"
OTHER_VERSIONS=$(find /Applications -maxdepth 1 -name "*Code*" -o -name "*VSCode*" 2>/dev/null | grep -v "Visual Studio Code.app$" || true)

if [ -n "$OTHER_VERSIONS" ]; then
    echo -e "${YELLOW}⚠${NC} Se encontraron otras versiones de VS Code:"
    echo "$OTHER_VERSIONS"
    echo ""
    read -p "¿Deseas eliminarlas? (s/n): " -n 1 -r
    echo ""
    if [[ $REPLY =~ ^[Ss]$ ]]; then
        echo "$OTHER_VERSIONS" | while read -r path; do
            if [ -n "$path" ] && [ "$path" != "$VSCODE_APP_PATH" ]; then
                echo -e "${BLUE}   Eliminando: $path${NC}"
                rm -rf "$path"
                echo -e "${GREEN}   ✓ Eliminado${NC}"
            fi
        done
        echo -e "${GREEN}✓${NC} Versiones adicionales eliminadas"
    fi
else
    echo -e "${GREEN}✓${NC} No se encontraron otras versiones de VS Code"
fi

echo ""
echo -e "${GREEN}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${GREEN}  ¡Actualización completada!${NC}"
echo -e "${GREEN}═══════════════════════════════════════════════════════════════${NC}"
echo ""
echo -e "Ahora puedes ejecutar VS Code con:"
echo -e "${BLUE}  /Applications/Visual Studio Code.app/Contents/Resources/app/bin/code${NC}"
echo ""
echo -e "O simplemente usar el comando: ${BLUE}code${NC}"
echo ""

# Registro en auditoría si existe el archivo
AUDIT_LOG=".github/copilot-audit.log"
if [ -f "$AUDIT_LOG" ]; then
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] [EJECUCIÓN] update-vscode.sh — Actualización de VS Code de 1.107.1 a 1.111.0" >> "$AUDIT_LOG"
fi
