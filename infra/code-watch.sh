#!/usr/bin/env bash
# code-watch.sh — Lanza VS Code con CDP habilitado para el Smart Monitor de Copilot.
#
# El Smart Monitor (smart_monitor/agent.py) reutiliza CDP y los ejecutores
# del watchdog original. VS Code es Electron/Chromium, por lo que acepta el
# flag --remote-debugging-port igual que Chrome.
#
# Uso:
#   ./infra/code-watch.sh          # lanza VS Code + Smart Monitor en background
#   ./infra/code-watch.sh --code   # solo lanza VS Code (sin monitor)
#   ./infra/code-watch.sh --watch  # solo lanza el Smart Monitor (VS Code ya corriendo)

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
WORKSPACE="${REPO_ROOT}/cardealer.code-workspace"
VENV="${REPO_ROOT}/.venv"
MONITOR="${REPO_ROOT}/.prompts/agent/smart_monitor/agent.py"
CDP_PORT=9222

CODE_BIN="/Applications/Visual Studio Code.app/Contents/Resources/app/bin/code"
if [[ ! -x "${CODE_BIN}" ]]; then
  CODE_BIN="code"
fi

launch_vscode() {
  echo "[code-watch] Lanzando VS Code con --remote-debugging-port=${CDP_PORT}..."
  "${CODE_BIN}" \
    --remote-debugging-port=${CDP_PORT} \
    --remote-debugging-address=127.0.0.1 \
    "${WORKSPACE}" &
  echo "[code-watch] VS Code PID: $!"
  sleep 6
}

launch_watchdog() {
  echo "[code-watch] Lanzando Smart Monitor con Gemma 3 (interval=30s)..."
  if [[ -d "${VENV}" ]]; then
    PYTHON="${VENV}/bin/python3"
  else
    PYTHON="python3"
  fi
  "${PYTHON}" "${MONITOR}" --interval 30 &
  echo "[code-watch] Smart Monitor PID: $!"
}

MODE="${1:-}"

case "${MODE}" in
  --code)
    launch_vscode
    ;;
  --watch)
    launch_watchdog
    ;;
  *)
    launch_vscode
    launch_watchdog
    echo "[code-watch] Todo listo. Ctrl+C para detener el Smart Monitor."
    wait
    ;;
esac
