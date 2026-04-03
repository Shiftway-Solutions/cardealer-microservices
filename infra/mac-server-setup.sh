#!/bin/bash
# =============================================================================
# OKLA Mac Server Setup — Prevención de Sleep Permanente
# MacBook Pro M5, macOS 26.x
# Ejecutar UNA SOLA VEZ con: sudo bash infra/mac-server-setup.sh
# =============================================================================
set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

if [ "$EUID" -ne 0 ]; then
  echo -e "${RED}[ERROR] Debes ejecutar con sudo:${NC}"
  echo "  sudo bash infra/mac-server-setup.sh"
  exit 1
fi

echo -e "${BLUE}=============================================${NC}"
echo -e "${BLUE}  OKLA — Configuración Mac como Servidor${NC}"
echo -e "${BLUE}=============================================${NC}"
echo ""

# =============================================================================
# 1. PMSET — Configuración AC Power (cuando está conectado al cargador)
# =============================================================================
echo -e "${YELLOW}[1/5] Configurando AC Power (cargador conectado)...${NC}"

pmset -c sleep 0          # No dormir el sistema
pmset -c displaysleep 0   # No apagar pantalla
pmset -c disksleep 0      # No pausar discos (Docker/Postgres)
pmset -c standby 0        # No entrar en standby profundo
pmset -c hibernatemode 0  # Sin hibernación (no escribe RAM al disco)
pmset -c powernap 0       # Sin power nap
pmset -c tcpkeepalive 1   # Mantener conexiones TCP activas
pmset -c ttyskeepawake 1  # Mantener activo si hay sesión SSH/tty
pmset -c womp 1           # Wake On Magic Packet (wake on LAN)

echo -e "${GREEN}  ✅ AC Power: sleep=0, standby=0, hibernate=0${NC}"

# =============================================================================
# 2. PMSET — Configuración Battery (por si se va la corriente)
# =============================================================================
echo -e "${YELLOW}[2/5] Configurando Battery Power (plan B si falla el cargador)...${NC}"

pmset -b sleep 0          # No dormir aunque esté en batería
pmset -b displaysleep 10  # Pantalla apaga a 10 min (ahorro en batería)
pmset -b disksleep 0      # No pausar discos
pmset -b standby 0        # No standby en batería
pmset -b hibernatemode 0  # Sin hibernación
pmset -b powernap 0       # Sin power nap
pmset -b tcpkeepalive 1   # Mantener TCP activo
pmset -b ttyskeepawake 1  # Mantener activo si hay SSH
pmset -b lessbright 0     # No reducir brillo (para acceso remoto)

echo -e "${GREEN}  ✅ Battery: sleep=0, standby=0, hibernate=0${NC}"

# =============================================================================
# 3. Eliminar sleep image (archivo de hibernación ocupa RAM en disco)
# =============================================================================
echo -e "${YELLOW}[3/5] Limpiando archivo de hibernación...${NC}"

if [ -f /var/vm/sleepimage ]; then
  rm -f /var/vm/sleepimage
  # Crear archivo de 0 bytes con permisos bloqueados para que macOS no lo recree
  touch /var/vm/sleepimage
  chflags uchg /var/vm/sleepimage
  echo -e "${GREEN}  ✅ sleepimage eliminado y bloqueado${NC}"
else
  echo -e "${GREEN}  ✅ sleepimage ya no existe${NC}"
fi

# =============================================================================
# 4. LaunchDaemon — caffeinate permanente (sobrevive reinicios)
# =============================================================================
echo -e "${YELLOW}[4/5] Instalando LaunchDaemon com.okla.preventleep...${NC}"

DAEMON_PLIST="/Library/LaunchDaemons/com.okla.preventleep.plist"

cat > "$DAEMON_PLIST" << 'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN"
  "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.okla.preventleep</string>
    <key>ProgramArguments</key>
    <array>
        <string>/usr/bin/caffeinate</string>
        <string>-d</string>
        <string>-i</string>
        <string>-s</string>
        <string>-u</string>
    </array>
    <key>KeepAlive</key>
    <true/>
    <key>RunAtLoad</key>
    <true/>
    <key>ProcessType</key>
    <string>Interactive</string>
    <key>StandardOutPath</key>
    <string>/var/log/okla-preventleep.log</string>
    <key>StandardErrorPath</key>
    <string>/var/log/okla-preventleep.log</string>
</dict>
</plist>
EOF

# Permisos correctos para LaunchDaemon
chown root:wheel "$DAEMON_PLIST"
chmod 644 "$DAEMON_PLIST"

# Descargar si ya estaba cargado (por si se re-ejecuta este script)
launchctl unload "$DAEMON_PLIST" 2>/dev/null || true

# Cargar el daemon
launchctl load -w "$DAEMON_PLIST"

echo -e "${GREEN}  ✅ LaunchDaemon instalado y activo${NC}"

# =============================================================================
# 5. Verificación final
# =============================================================================
echo ""
echo -e "${YELLOW}[5/5] Verificando configuración final...${NC}"
echo ""

echo -e "${BLUE}--- pmset AC Power ---${NC}"
pmset -g custom | grep -A 15 "AC Power:"

echo ""
echo -e "${BLUE}--- pmset Battery ---${NC}"
pmset -g custom | grep -A 15 "Battery Power:"

echo ""
echo -e "${BLUE}--- LaunchDaemon activo ---${NC}"
launchctl list | grep okla || echo "⚠️ Daemon no encontrado en launchctl list"

echo ""
echo -e "${BLUE}--- caffeinate corriendo ---${NC}"
pgrep -la caffeinate || echo "⚠️ caffeinate no está corriendo todavía"

echo ""
echo -e "${BLUE}--- Assertions de sleep prevention ---${NC}"
pmset -g assertions | grep -E "PreventSystem|NoIdle|caffeinate" | head -10

echo ""
echo -e "${GREEN}=============================================${NC}"
echo -e "${GREEN}  ✅ CONFIGURACIÓN COMPLETADA${NC}"
echo -e "${GREEN}=============================================${NC}"
echo ""
echo -e "${YELLOW}⚠️  NOTA IMPORTANTE — Tapa cerrada (Clamshell):${NC}"
echo ""
echo "  Con caffeinate -s corriendo, el sistema NO debería dormir con"
echo "  la tapa cerrada mientras esté conectado a AC."
echo ""
echo "  Si igual duerme al cerrar la tapa, la solución 100% confiable es:"
echo "  → Conectar un monitor externo o un HDMI Dummy Plug (\$8 en Amazon)"
echo "     (engaña al Mac haciéndole creer que hay monitor conectado)"
echo ""
echo "  Adicionalmente, en Sistema > Batería > Options:"
echo "  → Activa 'Prevent automatic sleeping when the display is off'"
echo "    (disponible en macOS Ventura+ con monitores externos)"
echo ""
echo -e "${BLUE}  Puedes verificar en cualquier momento con:${NC}"
echo "    pmset -g live"
echo "    pmset -g assertions"  
echo "    launchctl list | grep okla"
echo ""
