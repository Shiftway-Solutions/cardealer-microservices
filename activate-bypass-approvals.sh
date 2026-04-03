#!/usr/bin/env bash
# activate-bypass-approvals.sh
# Guion de activación rápida para Bypass Approvals Mode
# Uso: ./activate-bypass-approvals.sh
# Esto es solo informativo — las configuraciones ya están en settings.json

set -e

echo "════════════════════════════════════════════════════════════════════"
echo "  BYPASS APPROVALS MODE — Activación Rápida"
echo "════════════════════════════════════════════════════════════════════"
echo ""

# Verificar que estamos en el workspace
if [ ! -f "cardealer.sln" ]; then
  echo "❌ Error: No estamos en el workspace OKLA"
  echo "   Ejecuta desde: /Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices"
  exit 1
fi

echo "✅ Workspace detectado: cardealer-microservices"
echo ""

# Verificar configuraciones
SETTINGS_FILE="$HOME/Library/Application Support/Code/User/settings.json"
if [ ! -f "$SETTINGS_FILE" ]; then
  echo "❌ Error: settings.json global no encontrado"
  exit 1
fi

if grep -q '"chat.tools.global.autoApprove": true' "$SETTINGS_FILE"; then
  echo "✅ chat.tools.global.autoApprove: true"
else
  echo "⚠️  chat.tools.global.autoApprove no encontrado"
fi

if grep -q '"chat.tools.global.autoRetry": true' "$SETTINGS_FILE"; then
  echo "✅ chat.tools.global.autoRetry: true"
else
  echo "⚠️  chat.tools.global.autoRetry no encontrado"
fi

if grep -q '"github.copilot.chat.agent.bypassApprovalsMode": "always"' "$SETTINGS_FILE"; then
  echo "✅ github.copilot.chat.agent.bypassApprovalsMode: always"
else
  echo "⚠️  github.copilot.chat.agent.bypassApprovalsMode no encontrado"
fi

echo ""
echo "════════════════════════════════════════════════════════════════════"
echo "  FORMA 1️⃣  — Usar Bypass Approvals Ahora (Manual)"
echo "════════════════════════════════════════════════════════════════════"
echo ""
echo "1. Copiar prompt de loop:"
echo "   $ cat .prompts/AGENT_LOOP_PROMPT.md | pbcopy"
echo ""
echo "2. En VS Code:"
echo "   • Ctrl+Shift+I (abre Copilot Agent Chat)"
echo "   • Cmd+V (pega AGENT_LOOP_PROMPT)"
echo "   • El agente entra en Bypass Approvals automáticamente"
echo ""

echo "════════════════════════════════════════════════════════════════════"
echo "  FORMA 2️⃣  — Usar Daemon (Automático)"
echo "════════════════════════════════════════════════════════════════════"
echo ""
echo "1. Activar ambiente virtual:"
echo "   $ source .venv/bin/activate"
echo ""
echo "2. Iniciar daemon:"
echo "   $ python3 .prompts/prompt_loop_daemon.py"
echo ""
echo "3. El daemon monitorea .prompts/prompt_1.md y despacha al agente"
echo ""

echo "════════════════════════════════════════════════════════════════════"
echo "  TAREAS DISPONIBLES EN .prompts/prompt_1.md"
echo "════════════════════════════════════════════════════════════════════"
echo ""

if [ -f ".prompts/prompt_1.md" ]; then
  HEAD_LINES=$(head -10 ".prompts/prompt_1.md")
  echo "$HEAD_LINES"
  echo ""
  echo "📄 Ver archivo completo: cat .prompts/prompt_1.md"
else
  echo "⚠️  .prompts/prompt_1.md no encontrado"
fi

echo ""
echo "════════════════════════════════════════════════════════════════════"
echo "  AUDITORÍA EN TIEMPO REAL"
echo "════════════════════════════════════════════════════════════════════"
echo ""
echo "Ver logs del agente (actualización en vivo):"
echo "  $ tail -f .github/copilot-audit.log"
echo ""

echo "════════════════════════════════════════════════════════════════════"
echo "  ✅ BYPASS APPROVALS ESTÁ LISTO"
echo "════════════════════════════════════════════════════════════════════"
echo ""
echo "Elige tu forma preferida:"
echo ""
echo "  FORMA 1️⃣  (Manual)  → cat .prompts/AGENT_LOOP_PROMPT.md | pbcopy"
echo "  FORMA 2️⃣  (Daemon)  → python3 .prompts/prompt_loop_daemon.py"
echo ""
echo "Para detener: echo 'STOP' >> .prompts/prompt_1.md"
echo ""
