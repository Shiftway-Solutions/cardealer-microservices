#!/bin/bash
# QA Screenshot Helper for OKLA
# Uso: ./scripts/qa-screenshots.sh [test-name]

set -e

SCREENSHOTS_DIR="screenshots"
REPORT_DIR="playwright-report"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

echo "🎬 OKLA QA - Playwright Screenshot Helper"
echo "=========================================="

# Crear directorios
mkdir -p "$SCREENSHOTS_DIR"
mkdir -p "$REPORT_DIR"

# Verificar que el servidor está corriendo
echo "✓ Verificando servidor en localhost:3000..."
if ! curl -s http://localhost:3000 > /dev/null 2>&1; then
  echo "⚠️  Servidor no detectado. Iniciando pnpm dev..."
  cd frontend/web-next
  pnpm dev &
  PID=$!
  sleep 5
  cd ../..
  SERVER_PID=$PID
fi

# Ejecutar tests específicos o todos
if [ -z "$1" ]; then
  echo "📊 Ejecutando TODOS los tests con screenshots..."
  npx playwright test e2e/ --reporter=html,json,list
else
  echo "📊 Ejecutando test: $1"
  npx playwright test e2e/$1.spec.ts --reporter=html,json,list
fi

# Generar reporte
echo ""
echo "✅ Tests completados"
echo "📁 Screenshots: $SCREENSHOTS_DIR/"
echo "📊 Reporte HTML: $REPORT_DIR/index.html"
echo ""
echo "Para ver el reporte interactivo:"
echo "  npx playwright show-report"

# Copiar reportes con timestamp
cp -r "$REPORT_DIR" "playwright-report-$TIMESTAMP" 2>/dev/null || true

echo ""
echo "✨ Archivos generados:"
ls -lahS "$SCREENSHOTS_DIR" | head -20
