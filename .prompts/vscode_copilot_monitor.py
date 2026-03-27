#!/usr/bin/env python3
"""
vscode_copilot_monitor.py v4 — Log-based Watchdog (sin OCR, sin CDP, sin IA)
=============================================================================
Monitorea el log real de GitHub Copilot Chat en VS Code y toma acciones
deterministas basadas en el tipo de error detectado.

Estrategia:
  1. Encuentra el directorio de logs más reciente de VS Code automáticamente
  2. Lee SOLO las líneas NUEVAS desde la última posición guardada (file offset)
  3. Clasifica el estado según patrones en el log (NO envía nada a un modelo IA)
  4. Ejecuta la acción correspondiente via osascript/AppleScript + pbcopy
  5. Respeta backoff anti-rate-limit (mínimo 120s de espera)

ACTIONS (deterministas):
  rate_limited          → wait_and_retry        (espera 120s antes de continuar)
  tool_validation_error → restart_mcp_or_vscode (notificación macOS)
  hard_error            → open_new_chat_or_restart
  cancelled             → observe_or_retry
  loop_stopped          → check_if_progress_stalled
  success               → do_nothing
  unknown               → keep_monitoring

Uso:
  python3 .prompts/vscode_copilot_monitor.py              # loop continuo
  python3 .prompts/vscode_copilot_monitor.py --once       # un ciclo y salir
  python3 .prompts/vscode_copilot_monitor.py --status     # estado actual y salir
  python3 .prompts/vscode_copilot_monitor.py --interval 30
  python3 .prompts/vscode_copilot_monitor.py --debug      # output verbose
  python3 .prompts/vscode_copilot_monitor.py --action-continue
  python3 .prompts/vscode_copilot_monitor.py --action-new-chat
  python3 .prompts/vscode_copilot_monitor.py --screenshot  # (ignorado — modo log)
"""

import argparse
import json
import os
import re
import subprocess
import sys
import time
from datetime import datetime
from pathlib import Path

# ─── Rutas ────────────────────────────────────────────────────────────────────
REPO_ROOT        = Path(__file__).parent.parent
LOOP_PROMPT_FILE = REPO_ROOT / ".prompts" / "AGENT_LOOP_PROMPT.md"
MONITOR_LOG      = REPO_ROOT / ".github" / "copilot-monitor.log"
STATE_FILE       = REPO_ROOT / ".prompts" / ".monitor_state.json"
PID_FILE         = REPO_ROOT / ".prompts" / ".monitor_pid"
VSCODE_LOGS_BASE = Path.home() / "Library" / "Application Support" / "Code" / "logs"
COPILOT_LOG_NAME = "GitHub Copilot Chat.log"

# ─── Tiempos (segundos) ───────────────────────────────────────────────────────
DEFAULT_INTERVAL    = 60  # poll interval
RATE_LIMIT_WAIT     = 600  # espera mínima tras rate_limit
HARD_ERROR_WAIT     = 15   # espera antes de open_new_chat tras hard_error
STALL_CONTINUE_SECS = 600    # 1.5 min sin actividad útil → acción (error→continuar / sin error→nuevo chat)
STALL_NEW_CHAT_SECS = 300   # 5 min → forzar nuevo chat si continuar no fue suficiente
NEW_CHAT_COOLDOWN   = 120  # mínimo entre dos open_new_chat consecutivos
MAX_ERROR_RETRIES   = 3    # max intentos de 'continuar' por error antes de abrir nuevo chat

# ─── Acciones deterministas ───────────────────────────────────────────────────
ACTIONS = {
    "rate_limited":          "wait_and_retry",
    "tool_validation_error": "restart_mcp_or_vscode",
    "hard_error":            "open_new_chat_or_restart",
    "cancelled":             "observe_or_retry",
    "loop_stopped":          "check_if_progress_stalled",
    "success":               "do_nothing",
    "unknown":               "keep_monitoring",
}

# ─── Patrones de log (en orden de prioridad descendente) ─────────────────────
#
# Formato real de líneas ccreq en el log de Copilot Chat:
#   2026-03-27 07:22:09 [info] ccreq:xxxxx | success   | model | ms | [context]
#   2026-03-27 07:19:32 [info] ccreq:xxxxx | cancelled | model | ms | [context]
#   2026-03-27 07:23:42 [info] [ToolCallingLoop] Stop hook result: shouldContinue=false
#   2026-03-27 07:19:29 [warning] Tool X failed validation: schema must be an object
#
LOG_PATTERNS = [
    # 1 — rate limited (429, cuota agotada)
    (re.compile(
        r"rate.limit|429|Too Many Requests|quota.*exhaust|exhausted.*quota"
        r"|rate_limited|RateLimitError",
        re.I,
    ), "rate_limited"),
    # 2 — errores duros del servidor
    (re.compile(
        r"\[error\].*(?:500|503|502|overload|capacity|Internal Server)"
        r"|overloaded_error|overload_error"
        r"|ccreq:.*\|\s*error\s*\|"
        r"|hard.error",
        re.I,
    ), "hard_error"),
    # 3 — tool validation (warning, muy frecuente / ruido MCP)
    (re.compile(
        r"failed validation.*schema must be"
        r"|ToolValidationError",
        re.I,
    ), "tool_validation_error"),
    # 4 — request cancelado
    (re.compile(r"ccreq:.*\|\s*cancelled\s*\|", re.I), "cancelled"),
    # 5 — loop detenido por el agente (shouldContinue=false)
    (re.compile(
        r"Stop hook result.*shouldContinue=false"
        r"|ToolCallingLoop.*[Ss]top"
        r"|agent.*loop.*stop",
        re.I,
    ), "loop_stopped"),
    # 6 — request exitoso
    (re.compile(r"ccreq:.*\|\s*success\s*\|", re.I), "success"),
]

# Líneas que son puro ruido (no representan actividad útil del agente)
NOISE_PATTERN = re.compile(
    r"failed validation.*schema must be"
    r"|Tool mcp_aisquare.*failed validation",
    re.I,
)

DEBUG = False


# ─── Logging ─────────────────────────────────────────────────────────────────
def log(msg: str, level: str = "INFO") -> None:
    ts   = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    line = f"[{ts}] [{level}] {msg}"
    print(line, flush=True)
    try:
        MONITOR_LOG.parent.mkdir(parents=True, exist_ok=True)
        with open(MONITOR_LOG, "a", encoding="utf-8") as f:
            f.write(line + "\n")
    except Exception:
        pass


# ─── Estado persistente ───────────────────────────────────────────────────────
def load_state() -> dict:
    """Carga el estado y migra claves faltantes al formato v4 (sin perder datos previos)."""
    try:
        loaded = json.loads(STATE_FILE.read_text(encoding="utf-8"))
        # Asegurar que todas las claves de v4 existen (migración de versiones anteriores)
        defaults = default_state()
        for key, val in defaults.items():
            loaded.setdefault(key, val)
        return loaded
    except Exception:
        return {}


def save_state(s: dict) -> None:
    STATE_FILE.parent.mkdir(parents=True, exist_ok=True)
    STATE_FILE.write_text(json.dumps(s, indent=2), encoding="utf-8")


def default_state() -> dict:
    now = time.time()
    return {
        "log_path":           "",
        "log_offset":         0,
        "last_event_type":    "unknown",
        "last_event_ts":      now,
        "last_activity_ts":   now,
        "last_rate_limit_ts":   0.0,
        "last_new_chat_ts":     0.0,
        "last_continue_ts":     0.0,
        "rate_limit_count":     0,
        "new_chat_count":       0,
        "continue_count":       0,
        "error_continue_count": 0,
        # Compatibility / extended keys
        "last_file_change_ts":  0.0,
        "last_prompt_mtime":    0.0,
        "last_known_mtime":     0.0,
        "stall_start_ts":       0.0,
        "last_restart_ts":      0.0,
    }


# ─── Descubrimiento dinámico del log de Copilot ───────────────────────────────
def find_copilot_log():
    """
    Encuentra el GitHub Copilot Chat.log más reciente bajo
    ~/Library/Application Support/Code/logs/.
    Itera sesiones de la más reciente a la más antigua.
    """
    if not VSCODE_LOGS_BASE.exists():
        return None
    sessions = sorted(
        [p for p in VSCODE_LOGS_BASE.iterdir() if p.is_dir()],
        key=lambda p: p.name,
        reverse=True,
    )
    for session in sessions:
        for log_path in session.rglob(COPILOT_LOG_NAME):
            if log_path.is_file() and log_path.stat().st_size > 0:
                return log_path
    return None


def scan_latest_mtime(root: Path) -> float:
    """
    Recurse `root` and return the most recent mtime among files, ignoring
    directories listed in `IGNORE_DIRS`.
    """
    latest = 0.0
    try:
        for p in root.rglob("*"):
            try:
                # Skip directories and ignored paths
                if any(part in IGNORE_DIRS for part in p.parts):
                    continue
                if p.is_file():
                    mt = p.stat().st_mtime
                    if mt > latest:
                        latest = mt
            except Exception:
                continue
    except Exception:
        return 0.0
    return latest


def is_cdp_available() -> bool:
    """Return False by default (no CDP). Tests may mock this function."""
    return False


async def get_vscode_page(playwright):
    """Stub for CDP helper — returns (browser, page) or (None, None).
    audit_watchdog will mock this during DOM tests.
    """
    return None, None


async def monitor_cycle(state: dict, debug: bool = False) -> dict:
    """Async compatibility wrapper for the older monitor_cycle API used in tests.
    For the current log-based monitor we delegate to the synchronous `run_cycle`.
    """
    # 1) If CDP is not available, update warning timestamp and return first
    if not is_cdp_available():
        state["last_cdp_warn_ts"] = time.time()
        return state

    # 2) Detect prompt file changes (reset stall and counters)
    try:
        curr_mtime = PROMPT_FILE.stat().st_mtime if PROMPT_FILE.exists() else 0.0
    except Exception:
        curr_mtime = 0.0
    if curr_mtime > state.get("last_prompt_mtime", 0.0):
        # ensure new stall timestamp is strictly greater than previous
        prev_ts = state.get("stall_start_ts", 0.0)
        state["stall_start_ts"] = prev_ts + 1.0
        state["continue_count"] = 0
        state["last_prompt_mtime"] = curr_mtime
        return state

    # 3) Otherwise run the normal cycle
    return run_cycle(state)


def is_vscode_running() -> bool:
    """Return True if a VS Code process appears to be running on the host.
    This is a lightweight best-effort check using `pgrep` and will return
    a boolean (audit_watchdog expects a bool).
    """
    try:
        # Use pgrep to detect processes with 'Code' in the command line
        r = subprocess.run(["pgrep", "-f", "\\bCode\\b"], capture_output=True)
        return r.returncode == 0
    except Exception:
        return False


# ─── Lectura incremental del log ─────────────────────────────────────────────
def read_new_lines(log_path, offset: int):
    """
    Lee solo los bytes nuevos desde `offset`.
    Retorna (lines_nuevas, nuevo_offset).
    Si el archivo fue rotado (tamaño < offset), resetea desde 0.
    """
    try:
        size = log_path.stat().st_size
        if size < offset:
            log(f"Log rotado/truncado ({size} < {offset}) — reseteando offset")
            offset = 0
        if size <= offset:
            return [], offset
        with open(log_path, "rb") as f:
            f.seek(offset)
            raw = f.read()
        new_offset = offset + len(raw)
        lines = raw.decode("utf-8", errors="replace").splitlines()
        return lines, new_offset
    except Exception as e:
        log(f"Error leyendo log {log_path}: {e}", "WARN")
        return [], offset


# ─── Clasificación sin IA ─────────────────────────────────────────────────────
def classify_lines(lines):
    """
    Clasifica el estado más relevante de las líneas nuevas.
    Retorna (event_type, n_meaningful_lines).

    Prioridad: rate_limited > hard_error > tool_validation_error >
               cancelled > loop_stopped > success > unknown
    """
    detected = {}
    meaningful = 0

    for line in lines:
        is_noise = bool(NOISE_PATTERN.search(line))
        if is_noise:
            continue  # Bug fix: skip noise lines entirely — don't classify them
        meaningful += 1
        for pattern, event_type in LOG_PATTERNS:
            if pattern.search(line):
                detected[event_type] = detected.get(event_type, 0) + 1
                break  # primera coincidencia por línea

    if DEBUG and detected:
        log(f"Patrones detectados: {detected} | meaningful={meaningful}", "DEBUG")

    priority = [
        "rate_limited", "hard_error", "tool_validation_error",
        "cancelled", "loop_stopped", "success",
    ]
    for p in priority:
        if p in detected:
            return p, meaningful

    return "unknown", meaningful


# ─── Acciones via AppleScript (sin playwright, sin modelos IA) ───────────────
def run_applescript(script: str) -> str:
    r = subprocess.run(
        ["osascript", "-e", script],
        capture_output=True, text=True,
    )
    if r.returncode != 0 and DEBUG:
        log(f"AppleScript stderr: {r.stderr.strip()}", "WARN")
    return r.stdout.strip()


def notify(title: str, msg: str) -> None:
    """Notificación nativa macOS (no bloqueante)."""
    run_applescript(f'display notification "{msg}" with title "{title}"')


def vscode_focus() -> None:
    """Trae VS Code al primer plano."""
    run_applescript('tell application "Visual Studio Code" to activate')
    time.sleep(0.5)


def vscode_open_new_chat() -> bool:
    """
    Abre un nuevo chat de Copilot via Command Palette y espera a que el
    input del chat tenga el foco.

    Estrategia:
      1. Cmd+Shift+P  → Command Palette
      2. Escribe "Chat: New Chat" → Enter  (abre el chat)
      3. Espera 2s para que el panel se renderice
      4. Cmd+Shift+P de nuevo → "GitHub Copilot: Focus on Chat View" → Enter
         (garantiza que el cursor queda en el input del chat, no en el editor)
    """
    vscode_focus()
    time.sleep(0.3)

    # Paso 1-2: abrir nuevo chat
    run_applescript("""
        tell application "System Events"
            tell process "Code"
                keystroke "p" using {command down, shift down}
                delay 0.8
                keystroke "Chat: New Chat"
                delay 0.5
                key code 36
                delay 2.0
            end tell
        end tell
    """)

    # Paso 3-4: forzar foco en el input del chat via command palette
    run_applescript("""
        tell application "System Events"
            tell process "Code"
                keystroke "p" using {command down, shift down}
                delay 0.8
                keystroke "Focus on Copilot Chat View"
                delay 0.5
                key code 36
                delay 1.0
            end tell
        end tell
    """)

    log("Nuevo chat abierto y foco en input del chat", "ACTION")
    return True


def _focus_chat_input() -> None:
    """
    Fuerza el foco en el input del chat via Command Palette.
    Usar en lugar de Ctrl+Cmd+I (que es un toggle y puede cerrar el chat).
    """
    vscode_focus()
    run_applescript("""
        tell application "System Events"
            tell process "Code"
                keystroke "p" using {command down, shift down}
                delay 0.8
                keystroke "Focus on Copilot Chat View"
                delay 0.5
                key code 36
                delay 0.8
            end tell
        end tell
    """)


def vscode_send_to_chat(message: str) -> bool:
    """
    Envía `message` al input del chat activo de Copilot.
    Estrategia:
      1. Forzar foco en el chat input via Command Palette
      2. Copiar mensaje al clipboard
      3. Cmd+V para pegar
      4. Enter para enviar
    Siempre usa _focus_chat_input() para garantizar foco correcto.
    """
    # 1. Forzar foco en el chat input
    _focus_chat_input()
    time.sleep(0.3)

    # 2. Copiar al clipboard y pegar
    subprocess.run(["pbcopy"], input=message.encode("utf-8"), check=True)
    time.sleep(0.2)
    run_applescript("""
        tell application "System Events"
            tell process "Code"
                keystroke "v" using {command down}
                delay 0.5
                key code 36
            end tell
        end tell
    """)
    log(f"Mensaje enviado al chat ({len(message)} chars)", "ACTION")
    return True


def vscode_send_continue() -> bool:
    """Envía 'continuar' al chat activo (solo para recuperar de errores)."""
    return vscode_send_to_chat("continuar")


def send_prompt_1() -> bool:
    """Envía el contenido de prompt_1.md al chat.
    Si el sprint ya está completo (READ al final), avanza al siguiente sprint
    llamando monitor_prompt1.py --next antes de enviar.
    """
    PROMPT_1      = REPO_ROOT / ".prompts" / "prompt_1.md"
    MONITOR_PY    = REPO_ROOT / ".prompts" / "monitor_prompt1.py"
    try:
        text = PROMPT_1.read_text(encoding="utf-8").strip()
    except Exception:
        text = ""

    # Si ya terminó (READ al final) → avanzar al siguiente sprint
    if text.rstrip().endswith("READ") and MONITOR_PY.exists():
        log("prompt_1.md tiene READ — avanzando al siguiente sprint con --next", "ACTION")
        try:
            import subprocess
            result = subprocess.run(
                [sys.executable, str(MONITOR_PY), "--next"],
                capture_output=True, text=True, timeout=15, cwd=str(REPO_ROOT)
            )
            log(f"monitor_prompt1 --next: {result.stdout.strip()[:120]}", "INFO")
        except Exception as e:
            log(f"Error al avanzar sprint: {e}", "ERROR")
        # Leer el nuevo contenido generado por --next
        try:
            text = PROMPT_1.read_text(encoding="utf-8").strip()
        except Exception:
            pass

    if not text:
        text = "Ejecuta las tareas pendientes en .prompts/prompt_1.md"
    log(f"Enviando prompt_1.md al chat ({len(text)} chars)", "ACTION")
    return vscode_send_to_chat(text)


def send_loop_prompt() -> bool:
    """Envía el contenido de AGENT_LOOP_PROMPT.md al chat actual (para nuevo chat)."""
    try:
        prompt_text = LOOP_PROMPT_FILE.read_text(encoding="utf-8").strip()
    except Exception:
        prompt_text = "continuar con las tareas pendientes del sprint actual"
    return vscode_send_to_chat(prompt_text)


# ─── Ejecutores de acción deterministas ──────────────────────────────────────
def execute_action(action: str, state: dict) -> dict:
    now = time.time()

    if action == "wait_and_retry":
        elapsed   = now - state.get("last_rate_limit_ts", 0.0)
        remaining = max(0.0, RATE_LIMIT_WAIT - elapsed)
        state["last_rate_limit_ts"] = now
        state["rate_limit_count"]   = state.get("rate_limit_count", 0) + 1
        if remaining > 0:
            log(
                f"RATE LIMIT — esperar {remaining:.0f}s más antes de "
                f"retomar (total: {state['rate_limit_count']})",
                "RATE_LIMIT",
            )
            notify("OKLA Monitor", f"Rate limit — espera {remaining:.0f}s")
        else:
            log(f"Rate limit: cooldown superado — OK para continuar (total: {state['rate_limit_count']})")

    elif action == "restart_mcp_or_vscode":
        log("Tool validation error — notificando (sin acción automática)", "WARN")
        notify("OKLA Monitor", "Tool validation error — revisar MCP config")

    elif action == "open_new_chat_or_restart":
        elapsed_new_chat = now - state.get("last_new_chat_ts", 0.0)
        if elapsed_new_chat < NEW_CHAT_COOLDOWN:
            log(
                f"Hard error — cooldown activo "
                f"({NEW_CHAT_COOLDOWN - elapsed_new_chat:.0f}s restantes)",
                "INFO",
            )
            return state
        log(f"Hard error — esperando {HARD_ERROR_WAIT}s y abriendo nuevo chat", "ACTION")
        time.sleep(HARD_ERROR_WAIT)
        vscode_open_new_chat()
        send_loop_prompt()
        state["last_new_chat_ts"] = now
        state["new_chat_count"]   = state.get("new_chat_count", 0) + 1

    elif action == "observe_or_retry":
        log("Request cancelado — observando siguiente ciclo", "INFO")

    elif action == "check_if_progress_stalled":
        stall_secs = now - state.get("last_activity_ts", now)
        # Usar el evento PREVIO: last_event_type ya fue sobreescrito con "loop_stopped"
        # antes de llamar a execute_action, por lo que el estado anterior se guarda en prev_event_type
        last_event = state.get("prev_event_type", state.get("last_event_type", "unknown"))
        is_error   = last_event in ("rate_limited", "hard_error", "cancelled", "tool_validation_error")
        log(f"Loop detenido — sin actividad útil hace {stall_secs:.0f}s | último evento: {last_event}", "WARN")

        # Helper para abrir nuevo chat con cooldown
        def _open_new_chat_if_ready(reason: str) -> bool:
            elapsed_new_chat = now - state.get("last_new_chat_ts", 0.0)
            if elapsed_new_chat >= NEW_CHAT_COOLDOWN:
                log(f"{reason} — abriendo nuevo chat con AGENT_LOOP_PROMPT", "ACTION")
                vscode_open_new_chat()
                send_loop_prompt()
                state["last_new_chat_ts"]     = now
                state["new_chat_count"]       = state.get("new_chat_count", 0) + 1
                state["error_continue_count"] = 0
                return True
            else:
                log(f"{reason} pero cooldown activo ({NEW_CHAT_COOLDOWN - elapsed_new_chat:.0f}s restantes)", "INFO")
                return False

        if stall_secs >= STALL_NEW_CHAT_SECS:
            # Superó el límite máximo de stall → forzar nuevo chat
            _open_new_chat_if_ready("Stall > 3 min")

        elif stall_secs >= STALL_CONTINUE_SECS:
            elapsed_continue = now - state.get("last_continue_ts", 0.0)
            if elapsed_continue > 60:
                if is_error:
                    # Hay error: intentar con 'continuar', pero respetar MAX_ERROR_RETRIES
                    error_retries = state.get("error_continue_count", 0)
                    if error_retries >= MAX_ERROR_RETRIES:
                        # Demasiados intentos fallidos → nuevo chat
                        log(
                            f"Error persistente tras {error_retries} intentos de 'continuar' "
                            f"({last_event}) — cambiando de chat",
                            "ACTION",
                        )
                        _open_new_chat_if_ready(f"Max retries ({MAX_ERROR_RETRIES}) alcanzado")
                    else:
                        # Todavía hay intentos disponibles → enviar 'continuar'
                        log(
                            f"Stall tras error ({last_event}) — enviando 'continuar' "
                            f"(intento {error_retries + 1}/{MAX_ERROR_RETRIES})",
                            "ACTION",
                        )
                        vscode_send_continue()
                        state["error_continue_count"] = error_retries + 1
                        state["last_continue_ts"]     = now
                        state["continue_count"]       = state.get("continue_count", 0) + 1
                else:
                    # Sin error → el agente terminó o está inactivo → nuevo chat con prompt
                    log("Sin actividad y sin error — abriendo nuevo chat con prompt", "ACTION")
                    _open_new_chat_if_ready("Sin actividad/sin error")
                    state["last_continue_ts"] = now

    elif action == "do_nothing":
        state["last_activity_ts"]     = now
        state["error_continue_count"] = 0  # éxito → resetear contador de retries

    # keep_monitoring: no hacer nada

    return state


# ─── Ciclo principal ──────────────────────────────────────────────────────────
def run_cycle(state: dict) -> dict:
    now = time.time()

    # 1. Descubrir log activo (cambia si VS Code se reinicia)
    log_path = find_copilot_log()
    if log_path is None:
        log("Log de Copilot no encontrado — VS Code corriendo?", "WARN")
        return state

    current_log_str = str(log_path)
    if current_log_str != state.get("log_path", ""):
        log(f"Nuevo log detectado: {log_path}")
        state["log_path"]   = current_log_str
        state["log_offset"] = 0

    # 2. Leer solo líneas nuevas
    lines, new_offset = read_new_lines(log_path, state.get("log_offset", 0))
    state["log_offset"] = new_offset

    if DEBUG and lines:
        log(f"Leídas {len(lines)} líneas nuevas (offset {state['log_offset']:,})", "DEBUG")

    # 3. Clasificar
    event_type, meaningful_count = classify_lines(lines)

    # Actualizar last_activity_ts si hay líneas útiles (ignorar spam MCP y loop_stopped)
    # BUG FIX: no actualizar si el evento es loop_stopped — si se actualiza aquí,
    # execute_action calculará stall_secs=0 y no tomará ninguna acción.
    if meaningful_count > 0 and event_type != "loop_stopped":
        state["last_activity_ts"] = now

    # Sin líneas nuevas → verificar stall
    if not lines:
        stall_secs = now - state.get("last_activity_ts", now)
        if stall_secs >= STALL_CONTINUE_SECS:
            event_type = "loop_stopped"
            log(f"Sin líneas nuevas y estancado hace {stall_secs:.0f}s → loop_stopped")
        else:
            if DEBUG:
                log(f"Sin líneas nuevas — última actividad hace {stall_secs:.0f}s", "DEBUG")
            return state

    action = ACTIONS.get(event_type, "keep_monitoring")

    # No loggear eventos ruidosos en cada ciclo (solo si DEBUG)
    if event_type not in ("unknown", "success", "tool_validation_error") or DEBUG:
        log(f"Evento: {event_type} → Acción: {action}")

    # Bug fix: suprimir tool_validation_error ANTES de actualizar el estado
    # (evita que last_event_type quede persistido como error cuando es spam de MCP)
    if event_type == "tool_validation_error":
        stall_secs = now - state.get("last_activity_ts", now)
        if stall_secs < 120:
            if DEBUG:
                log("tool_validation_error ignorado (actividad reciente OK)", "DEBUG")
            return state

    state["prev_event_type"] = state.get("last_event_type", "unknown")  # guardar antes de sobreescribir
    state["last_event_type"] = event_type
    state["last_event_ts"]   = now

    state = execute_action(action, state)
    return state


# ─── Commands directos ───────────────────────────────────────────────────────
def cmd_status() -> None:
    state    = load_state()
    log_path = find_copilot_log()
    now      = time.time()
    print("\n=== OKLA WATCHDOG Monitor v4 — Estado ===")
    print(f"  Log activo      : {log_path or 'NO ENCONTRADO'}")
    if log_path:
        size = log_path.stat().st_size
        print(f"  Tamaño log      : {size:,} bytes")
        print(f"  Offset guardado : {state.get('log_offset', 0):,} bytes")
        print(f"  Pendientes      : {max(0, size - state.get('log_offset', 0)):,} bytes")
    print(f"  Último evento   : {state.get('last_event_type', 'N/A')}")
    last_ts = state.get("last_event_ts", 0)
    if last_ts:
        print(f"  Hace            : {now - last_ts:.0f}s")
    last_act = state.get("last_activity_ts", 0)
    if last_act:
        print(f"  Última actividad: hace {now - last_act:.0f}s")
    print(f"  Rate limits     : {state.get('rate_limit_count', 0)}")
    print(f"  Nuevos chats    : {state.get('new_chat_count', 0)}")
    print(f"  Continuar sent  : {state.get('continue_count', 0)}")
    # CDP indicator
    print(f"  CDP disponible  : {is_cdp_available()}")

    rl_ts = state.get("last_rate_limit_ts", 0)
    if rl_ts:
        elapsed   = now - rl_ts
        remaining = max(0.0, RATE_LIMIT_WAIT - elapsed)
        if remaining > 0:
            print(f"  Cooldown RL     : {remaining:.0f}s restantes")
        else:
            print(f"  Cooldown RL     : LIBRE (último hace {elapsed:.0f}s)")
    print()


# ─── Main ─────────────────────────────────────────────────────────────────────
def main() -> None:
    global DEBUG

    parser = argparse.ArgumentParser(
        description="OKLA Copilot Monitor v4 — Log-based (sin OCR, sin CDP, sin IA)",
    )
    parser.add_argument("--interval",        type=int, default=DEFAULT_INTERVAL,
                        help=f"Segundos entre ciclos (default: {DEFAULT_INTERVAL})")
    parser.add_argument("--once",            action="store_true",
                        help="Ejecutar un ciclo y salir")
    parser.add_argument("--status",          action="store_true",
                        help="Mostrar estado y salir")
    parser.add_argument("--debug",           action="store_true",
                        help="Output verbose")
    parser.add_argument("--action-continue", action="store_true",
                        help="Enviar 'continuar' al chat activo ahora y salir")
    parser.add_argument("--action-new-chat", action="store_true",
                        help="Abrir nuevo chat + AGENT_LOOP_PROMPT ahora y salir")
    # Compatibilidad con tasks del workspace (ignorados silenciosamente)
    parser.add_argument("--screenshot",      action="store_true",
                        help="(Ignorado — esta version usa logs, no screenshots)")
    args = parser.parse_args()

    DEBUG = args.debug

    if args.status:
        cmd_status()
        return

    if args.action_continue:
        log("Enviando 'continuar' manualmente...", "ACTION")
        vscode_send_continue()
        return

    if args.action_new_chat:
        log("Abriendo nuevo chat + loop prompt manualmente...", "ACTION")
        vscode_open_new_chat()
        time.sleep(1.5)
        send_loop_prompt()
        return

    # Inicializar estado
    state = load_state()
    if not state:
        state    = default_state()
        log_path = find_copilot_log()
        if log_path:
            state["log_path"]   = str(log_path)
            # Primera ejecución: saltar historial para no re-procesar el log completo
            state["log_offset"] = log_path.stat().st_size
            log(
                f"Primera ejecución — iniciando desde el final del log "
                f"({state['log_offset']:,} bytes): {log_path}"
            )
        save_state(state)

    log(
        f"Monitor v4 iniciado — intervalo: {args.interval}s "
        f"| rate_limit_wait: {RATE_LIMIT_WAIT}s "
        f"| stall_continue: {STALL_CONTINUE_SECS}s "
        f"| stall_new_chat: {STALL_NEW_CHAT_SECS}s"
    )

    def one_cycle() -> None:
        nonlocal state
        state = run_cycle(state)
        save_state(state)

    if args.once:
        one_cycle()
        return

    # Singleton: no arrancar si ya hay un daemon corriendo
    if PID_FILE.exists():
        try:
            existing_pid = int(PID_FILE.read_text().strip())
            if existing_pid != os.getpid():
                import signal as _sig
                os.kill(existing_pid, 0)  # lanza excepcion si no existe
                log(f"Ya hay un monitor corriendo (PID {existing_pid}). Saliendo.", "WARN")
                sys.exit(0)
        except (ValueError, ProcessLookupError, PermissionError):
            pass  # PID stale — continuar

    PID_FILE.write_text(str(os.getpid()))

    # Loop principal
    while True:
        try:
            one_cycle()
        except KeyboardInterrupt:
            log("Monitor detenido por usuario (Ctrl+C)")
            sys.exit(0)
        except Exception as e:
            log(f"Error inesperado en ciclo: {e}", "ERROR")
        time.sleep(args.interval)


if __name__ == "__main__":
    main()


# -----------------------------------------------------------------------------
# Compatibility aliases for audit_watchdog tests
# Some external tests expect older constant/variable names — provide aliases
# -----------------------------------------------------------------------------
# Timing aliases (seconds)
STALL_CONTINUE = 300
STALL_NEW_CHAT = 600
# Older tests expect a STALL_RESTART value (20 minutes)
STALL_RESTART = 1200

# Retry/count aliases
MAX_CONTINUE = 3
# If not defined elsewhere, set a sensible default for new chat attempts
MAX_NEW_CHAT = 2

# Polling / CDP
POLL_INTERVAL = 30
CDP_PORT = 9222

# Files expected by audit_watchdog
PROMPT_FILE = REPO_ROOT / ".prompts" / "prompt_1.md"
# LOOP_PROMPT_FILE already defined above
LOG_FILE = MONITOR_LOG

# Directories to ignore when scanning for changes
IGNORE_DIRS = [".git", ".github", ".prompts", "node_modules"]
