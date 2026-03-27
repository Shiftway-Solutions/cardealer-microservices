#!/usr/bin/env python3
"""
vscode_copilot_monitor.py — Guardián de GitHub Copilot en VS Code

Objetivo: Copilot SIEMPRE trabajando. Tres capas de recuperación:

  CAPA 1 — ERROR DETECTADO
    → Envía "continuar" al chat activo
    → Si no reacciona en 90s → escala a capa 2

  CAPA 2 — NUEVO CHAT
    → Cierra chat actual, abre uno nuevo
    → Carga AGENT_LOOP_PROMPT.md completo
    → Si no reacciona en 120s → escala a capa 3

  CAPA 3 — REINICIO VS CODE
    → Mata y reabre VS Code con el workspace
    → Espera que cargue + abre chat nuevo
    → Carga AGENT_LOOP_PROMPT.md
    → Resetea contadores

Estado persistido en .prompts/.monitor_state.json para sobrevivir reinicios.

Uso:
  python3 .prompts/vscode_copilot_monitor.py            # loop infinito
  python3 .prompts/vscode_copilot_monitor.py --once     # un solo ciclo
  python3 .prompts/vscode_copilot_monitor.py --interval 30
  python3 .prompts/vscode_copilot_monitor.py --debug    # muestra OCR raw
  python3 .prompts/vscode_copilot_monitor.py --screenshot
  python3 .prompts/vscode_copilot_monitor.py --action-continue
  python3 .prompts/vscode_copilot_monitor.py --action-new-chat
  python3 .prompts/vscode_copilot_monitor.py --action-restart-vscode
"""

import argparse
import hashlib
import hashlib
import hmac as hmac_mod
import json
import subprocess
import sys
import time
import os
import re
import threading
from pathlib import Path
from datetime import datetime
import base64

try:
    import websocket  # websocket-client
    _WS_AVAILABLE = True
except ImportError:
    _WS_AVAILABLE = False

try:
    import openai as _openai_sdk
    _VISION_AVAILABLE = True
except ImportError:
    _VISION_AVAILABLE = False

# ─────────────────────────────────────────────────────────────────────────────
# CONFIGURACIÓN
# ─────────────────────────────────────────────────────────────────────────────
REPO_ROOT = Path(__file__).parent.parent
PROMPT_FILE = REPO_ROOT / ".prompts" / "prompt_1.md"
AGENT_LOOP_FILE = REPO_ROOT / ".prompts" / "AGENT_LOOP_PROMPT.md"
WORKSPACE_FILE = REPO_ROOT / "cardealer.code-workspace"
SCREENSHOTS_DIR = REPO_ROOT / ".github" / "screenshots" / "monitor"
LOG_FILE = REPO_ROOT / ".github" / "copilot-monitor.log"
STATE_FILE = REPO_ROOT / ".prompts" / ".monitor_state.json"
SCREENSHOTS_DIR.mkdir(parents=True, exist_ok=True)

# ─── Umbrales de tiempo (segundos) ───────────────────────────────────────────
STALL_THRESHOLD        = 180   # Sin cambio en prompt_1.md → posible stall
COOLDOWN_CONTINUE     = 75    # Entre intentos de "continuar"
COOLDOWN_NEW_CHAT     = 150   # Entre aperturas de nuevo chat
COOLDOWN_RESTART      = 300   # Entre reinicios de VS Code
READ_STABLE_THRESHOLD = 120   # READ estable → dispatch nuevo ciclo
VSCODE_BOOT_WAIT      = 25    # Segundos para que VS Code arranque
MAX_CONTINUE_ATTEMPTS = 3     # Intentos de "continuar" antes de nuevo chat
MAX_NEW_CHAT_ATTEMPTS = 2     # Intentos de nuevo chat antes de restart
VISUAL_STALL_THRESHOLD = 120  # Segundos sin cambio visual en el chat = conversación detenida
WORKSPACE_SETTLE_TIME  = 150  # Tiempo mínimo sin cambio en prompt_1.md antes de actuar
USER_ACTIVE_GRACE      = 180  # Segundos de cooldown cuando se detecta usuario escribiendo en el chat
HID_ACTIVE_THRESHOLD   = 30   # Segundos: último HID + VS Code en foco → actividad humana detectada
SCREENSHOTS_KEEP      = 20    # Número máximo de screenshots a conservar

# ─── OpenClaw — rotación automática de modelos GitHub Copilot gratuitos ──────
OPENCLAW_MODELS = [
    "github-copilot/gpt-4.1",     # Primario (GPT-4.1)
    "github-copilot/gpt-4o",       # Fallback 1 (GPT-4o)
    "github-copilot/gpt-4o-mini",  # Fallback 2 (Raptor mini)
]
OPENCLAW_CONFIG   = Path.home() / ".openclaw" / "openclaw.json"
COOLDOWN_MODEL_SW = 120    # Mínimo segundos entre rotaciones de modelo

# Patrones de fallo de modelo en el chat (activan rotación automática de OpenClaw)
MODEL_FAIL_PATTERNS = [
    r"model.*not.*available",
    r"model.*unavailable",
    r"service.*unavailable",
    r"(gpt-4\.1|gpt-4o|gpt-4o-mini).*error",
    r"provider.*error",
    r"llm.*error",
    r"503\b",
]

# ─────────────────────────────────────────────────────────────────────────────
# PATRONES DE DETECCIÓN (OCR)
# ─────────────────────────────────────────────────────────────────────────────

# Frases que indican error/detención en el chat de Copilot
ERROR_PATTERNS = [
    r"an error occurred",
    r"something went wrong",
    r"request failed",
    r"i'm sorry.*unable",
    r"lo siento.*no puedo",
    r"rate limit",
    r"context length exceeded",
    r"token limit",
    r"network error",
    r"connection error",
    r"timed out",
    r"error al procesar",
    r"failed to",
    r"cannot complete",
    r"unable to continue",
    r"try again",
    r"reintent",
    r"límite de contexto",
    r"límite alcanzado",
]

# Frases que indican que COPILOT terminó y está esperando input
DONE_PATTERNS = [
    r"¿(hay algo más|algo más que pueda|puedo ayudarte)",
    r"(is there anything else|let me know if you need)",
    r"task (complete|completed|done|finished)",
    r"tarea (completada|terminada|finalizada)",
    r"(all done|everything is done)",
    r"(ready|waiting) for (next|your)",
]

# Indicadores de que Copilot ESTÁ trabajando (no intervenir)
WORKING_PATTERNS = [
    r"(running|ejecutando|building|compilando|testing|probando)",
    r"(step \d+|paso \d+)",
    r"(thinking|procesando|analizando)",
    r"evaluating",
    r"processing",
    r"generating",
    r"generando",
]

# Indicadores de que el chat está vacío / no cargado
EMPTY_CHAT_PATTERNS = [
    r"describe what to build",
    r"ask copilot",
    r"start a new conversation",
    r"nueva conversaci",
]


# ─────────────────────────────────────────────────────────────────────────────
# UTILIDADES
# ─────────────────────────────────────────────────────────────────────────────

# ─────────────────────────────────────────────────────────────────────────────
# ESTADO PERSISTENTE
# ─────────────────────────────────────────────────────────────────────────────

def load_state() -> dict:
    try:
        return json.loads(STATE_FILE.read_text())
    except Exception:
        return {}


def save_state(state: dict):
    STATE_FILE.write_text(json.dumps(state, indent=2))


def log(msg: str, level: str = "INFO"):
    ts = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    line = f"[{ts}] [{level}] {msg}"
    print(line, flush=True)
    try:
        with open(LOG_FILE, "a", encoding="utf-8") as f:
            f.write(line + "\n")
    except Exception:
        pass


def take_screenshot(name: str = "monitor") -> Path:
    """Captura pantalla completa con screencapture nativo."""
    ts = int(time.time())
    path = SCREENSHOTS_DIR / f"{name}_{ts}.png"
    result = subprocess.run(
        ["screencapture", "-x", str(path)],
        capture_output=True
    )
    if result.returncode != 0 or not path.exists():
        log("screencapture falló. Verifica permisos de Screen Recording.", "WARN")
        return None
    return path


def ocr_image(image_path: Path, region: tuple = None) -> str:
    """
    OCR de imagen usando macOS Vision framework.
    region = (x_frac, y_frac, w_frac, h_frac) en fracción 0-1 de la imagen.
    """
    ocr_script = f"""
import sys
from pathlib import Path
from Cocoa import NSURL
from Vision import (
    VNRecognizeTextRequest,
    VNImageRequestHandler,
    VNRequestTextRecognitionLevelAccurate,
)
import Quartz

img_path = "{image_path}"
url = NSURL.fileURLWithPath_(img_path)
handler = VNImageRequestHandler.alloc().initWithURL_options_(url, None)
request = VNRecognizeTextRequest.alloc().init()
request.setRecognitionLevel_(VNRequestTextRecognitionLevelAccurate)
request.setUsesLanguageCorrection_(True)

# Región opcional (normalizada 0-1, origen abajo-izquierda en Vision)
region = {region!r}
if region:
    x_frac, y_frac, w_frac, h_frac = region
    from Vision import VNNormalizedRectForImageRect
    import Quartz.CoreGraphics as CG
    # Vision usa coords con origen bottom-left
    rect = CG.CGRectMake(x_frac, 1.0 - y_frac - h_frac, w_frac, h_frac)
    request.setRegionOfInterest_(rect)

success, error = handler.performRequests_error_([request], None)
if not success:
    print("OCR_ERROR: " + str(error))
    sys.exit(1)

texts = []
for obs in (request.results() or []):
    candidate = obs.topCandidates_(1)
    if candidate:
        texts.append(candidate[0].string())

print("\\n".join(texts))
"""
    result = subprocess.run(
        [sys.executable, "-c", ocr_script],
        capture_output=True, text=True, timeout=30
    )
    if result.returncode != 0:
        log(f"OCR error: {result.stderr[:200]}", "WARN")
        return ""
    return result.stdout.strip()


def get_vscode_chat_bounds() -> tuple:
    """
    Detecta la posición del panel CHAT de VS Code en pantalla.
    Devuelve (x_frac, y_frac, w_frac, h_frac) normalizados para OCR.
    Asume que VS Code está maximizado y el chat ocupa la columna derecha (~30%).
    """
    # Basado en el screenshot observado: chat está en ~70%-100% del ancho
    # y en ~5%-90% del alto (excluyendo titlebar y statusbar)
    return (0.68, 0.05, 0.32, 0.85)


def read_prompt_last_line() -> str:
    """Lee la última línea no vacía de prompt_1.md."""
    try:
        content = PROMPT_FILE.read_text(encoding="utf-8")
        lines = [l.strip() for l in content.splitlines() if l.strip()]
        return lines[-1] if lines else ""
    except Exception:
        return ""


def read_agent_loop_prompt() -> str:
    """Lee el contenido del AGENT_LOOP_PROMPT.md (sin el header 'Copia y pega...')."""
    try:
        content = AGENT_LOOP_FILE.read_text(encoding="utf-8")
        # Saltar las primeras 5 líneas (título + instrucción de copia)
        lines = content.splitlines()
        # Encontrar la línea con "Ejecuta el siguiente loop..."
        start = 0
        for i, line in enumerate(lines):
            if "Ejecuta el siguiente loop" in line:
                start = i
                break
        return "\n".join(lines[start:]).strip()
    except Exception as e:
        log(f"No se pudo leer AGENT_LOOP_PROMPT.md: {e}", "ERROR")
        return ""


# ─────────────────────────────────────────────────────────────────────────────
# DETECCIÓN DE ACTIVIDAD HUMANA (macOS HID + Accessibility)
# ─────────────────────────────────────────────────────────────────────────────

def _seconds_since_last_input() -> float:
    """
    Segundos desde el último evento de teclado o mouse del sistema operativo.
    Usa CGEventSource (Quartz framework) — disponible sin permisos adicionales.
    """
    script = """
import sys
try:
    from Quartz import CGEventSourceSecondsSinceLastEventType, kCGEventSourceStateHIDSystemState
    # 0xFFFFFFFF = kCGAnyInputEventType (todos los tipos de eventos HID)
    secs = CGEventSourceSecondsSinceLastEventType(kCGEventSourceStateHIDSystemState, 0xFFFFFFFF)
    print(f"{secs:.1f}")
except Exception:
    print("9999")
"""
    try:
        result = subprocess.run(
            [sys.executable, "-c", script],
            capture_output=True, text=True, timeout=5
        )
        return float(result.stdout.strip())
    except Exception:
        return 9999.0


def _is_vscode_frontmost() -> bool:
    """True si VS Code es la aplicación activa en primer plano."""
    out = run_applescript(
        'tell application "System Events" to '
        'return name of first application process whose frontmost is true'
    )
    return "Code" in out


def _is_human_active_in_vscode() -> tuple[bool, str]:
    """
    Detecta CUALQUIER actividad humana en VS Code usando dos señales nativas macOS:
      - HID: último evento de teclado/mouse del sistema < HID_ACTIVE_THRESHOLD segundos
      - Foco: VS Code es la aplicación activa en primer plano
    Cubre: escritura en editor, clicks, navegación, apertura de archivos,
           scroll en cualquier panel, uso del terminal integrado, etc.
    Retorna (activo: bool, descripción: str).
    """
    try:
        vs_front = _is_vscode_frontmost()
        secs_hid = _seconds_since_last_input()
        if vs_front and secs_hid < HID_ACTIVE_THRESHOLD:
            return True, f"VS Code activo + HID hace {secs_hid:.0f}s"
        return False, f"vs_front={vs_front}, hid={secs_hid:.0f}s"
    except Exception as e:
        return False, f"error HID: {e}"


# ─────────────────────────────────────────────────────────────────────────────
# CONTROL DE VS CODE (AppleScript)
# ─────────────────────────────────────────────────────────────────────────────

def run_applescript(script: str) -> str:
    result = subprocess.run(
        ["osascript", "-e", script],
        capture_output=True, text=True, timeout=15
    )
    return result.stdout.strip()


def focus_vscode():
    """Pone VS Code en primer plano."""
    run_applescript('tell application "Code" to activate')
    time.sleep(0.4)


def _get_focused_ax_role() -> str:
    """
    Retorna el AX role del elemento con foco en VS Code.
    Usa AXFocusedUIElement (acceso directo, sin traversal del árbol completo).
    """
    result = run_applescript('''
    tell application "System Events"
        tell process "Code"
            try
                return role of (value of attribute "AXFocusedUIElement" of window 1)
            on error
                return "unknown"
            end try
        end tell
    end tell
    ''')
    return result.strip()


def _click_chat_input_position():
    """
    Fallback: hace click en la posición estimada del input del chat de Copilot.
    Calcula coordenadas relativas a la ventana de VS Code via AX.
    Copilot Chat (panel lateral izquierdo): x~15% ancho, y~93% alto.
    """
    result = run_applescript('''
    tell application "System Events"
        tell process "Code"
            try
                set w to window 1
                set pos to position of w
                set sz to size of w
                return ((item 1 of pos) as text) & "," & ((item 2 of pos) as text) & "," & ((item 1 of sz) as text) & "," & ((item 2 of sz) as text)
            on error
                return ""
            end try
        end tell
    end tell
    ''')
    if not result.strip():
        log("_click_chat_input_position: no se pudo obtener bounds de ventana", "WARN")
        return
    try:
        parts = [float(x.strip()) for x in result.split(",")]
        x_win, y_win, w_win, h_win = parts
        click_x = int(x_win + w_win * 0.15)
        click_y = int(y_win + h_win * 0.93)
        run_applescript(f'''
        tell application "System Events"
            tell process "Code"
                click at {{{click_x}, {click_y}}}
            end tell
        end tell
        ''')
        time.sleep(0.5)
        log(f"Click fallback en chat input ({click_x},{click_y})")
    except Exception as e:
        log(f"Error en _click_chat_input_position: {e}", "WARN")


def is_vscode_running() -> bool:
    out = run_applescript('tell application "System Events" to (name of processes) contains "Code"')
    return out.strip().lower() == "true"


def restart_vscode():
    """Mata VS Code, lo reabre con el workspace y espera que cargue."""
    log("🔴 REINICIANDO VS Code...", "ACTION")

    # 1. Cerrar VS Code con Cmd+Q
    if is_vscode_running():
        run_applescript('tell application "Code" to quit')
        time.sleep(4)
        # Forzar si no cerró
        subprocess.run(["pkill", "-f", "Visual Studio Code"], capture_output=True)
        time.sleep(2)

    # 2. Reabrir con el workspace
    workspace = str(WORKSPACE_FILE) if WORKSPACE_FILE.exists() else str(REPO_ROOT)
    subprocess.Popen(["open", "-a", "Visual Studio Code", workspace])
    log(f"VS Code reabriendo: {workspace}")

    # 3. Esperar que cargue completamente
    log(f"Esperando {VSCODE_BOOT_WAIT}s para que VS Code cargue...")
    time.sleep(VSCODE_BOOT_WAIT)

    # 4. Verificar que abrió
    for _ in range(10):
        if is_vscode_running():
            log("✅ VS Code abierto")
            return
        time.sleep(3)
    log("⚠️  VS Code tardó en abrir", "WARN")


def vscode_open_new_chat():
    """Abre un nuevo chat de Copilot en VS Code."""
    focus_vscode()
    time.sleep(0.3)
    # Cmd+Shift+P → "Chat: New Chat"
    run_applescript('''
    tell application "System Events"
        tell process "Code"
            keystroke "p" using {command down, shift down}
        end tell
    end tell
    ''')
    time.sleep(0.8)
    # Escribir el comando
    run_applescript('''
    tell application "System Events"
        tell process "Code"
            keystroke "Chat: New Chat"
        end tell
    end tell
    ''')
    time.sleep(0.5)
    # Enter para confirmar
    run_applescript('''
    tell application "System Events"
        tell process "Code"
            key code 36
        end tell
    end tell
    ''')
    time.sleep(1.0)
    log("Nuevo chat abierto")


def vscode_focus_chat_input():
    """
    Foca el input del chat de Copilot SIN cerrarlo.

    Estrategia:
    1. Verifica via AXFocusedUIElement si ya hay un AXTextField/AXTextArea enfocado.
       Si sí → no hacer nada (evita toggle-close de Cmd+Ctrl+I).
    2. Si no → envía Cmd+Ctrl+I (keybinding nativo de Copilot Chat).
    3. Verifica resultado. Si Cmd+Ctrl+I no enfocó un campo de texto → click fallback
       en la posición estimada del input.

    NUNCA usa la command palette (Cmd+Shift+P) porque el autocomplete puede
    seleccionar un comando equivocado que cierra el chat.
    """
    focus_vscode()
    time.sleep(0.3)

    # Paso 1: verificar si ya hay un campo de texto enfocado → no hacer nada
    if _get_focused_ax_role() in ("AXTextField", "AXTextArea"):
        return

    # Paso 2: abrir/focar el chat con el keybinding dedicado de Copilot
    run_applescript('''
    tell application "System Events"
        tell process "Code"
            keystroke "i" using {command down, control down}
        end tell
    end tell
    ''')
    time.sleep(1.2)

    # Paso 3: verificar resultado
    if _get_focused_ax_role() not in ("AXTextField", "AXTextArea"):
        log("Cmd+Ctrl+I no enfocó el input — usando click fallback", "WARN")
        _click_chat_input_position()


def vscode_type_in_chat(message: str):
    """
    Escribe un mensaje en el chat de Copilot y lo envía.
    Usa pbcopy + Cmd+V para mensajes largos (más fiable).

    Verifica via AXFocusedUIElement que un campo de texto tenga el foco
    antes de pegar. Si no lo tiene, llama a vscode_focus_chat_input().
    Si tras el intento de foco siguen sin haber un campo de texto activo,
    aborta para no pegar en el lugar equivocado.
    """
    # Asegurar foco en el input del chat
    vscode_focus_chat_input()
    time.sleep(0.2)

    # Verificación final antes de pegar
    ax_role = _get_focused_ax_role()
    if ax_role not in ("AXTextField", "AXTextArea"):
        log(f"No se pudo obtener foco en campo de texto (AX role={ax_role}) — abortando envío", "ERROR")
        return

    # Poner mensaje en clipboard y pegar
    subprocess.run(["pbcopy"], input=message.encode("utf-8"))
    time.sleep(0.1)

    run_applescript('''
    tell application "System Events"
        tell process "Code"
            keystroke "v" using {command down}
        end tell
    end tell
    ''')
    time.sleep(0.3)

    # Enter para enviar
    run_applescript('''
    tell application "System Events"
        tell process "Code"
            key code 36
        end tell
    end tell
    ''')
    time.sleep(0.5)
    log(f"Mensaje enviado al chat ({len(message)} chars)")


def vscode_type_continue():
    """Escribe 'continuar' en el chat activo y lo envía."""
    vscode_type_in_chat("continuar")
    log("✅ Enviado 'continuar' al chat")


def _get_screen_size() -> tuple:
    # Mantener valores por defecto para cálculo relativo de coordenadas.
    return 1512, 982


def get_vscode_window_bounds() -> tuple | None:
    """
    Devuelve (x, y, width, height) de la ventana principal de VS Code en puntos de pantalla.
    Retorna None si VS Code no tiene ventana abierta.
    """
    script = '''
    tell application "Code"
        if (count of windows) > 0 then
            set b to bounds of window 1
            return ((item 1 of b) as string) & "," & ((item 2 of b) as string) & "," & ((item 3 of b) as string) & "," & ((item 4 of b) as string)
        end if
        return ""
    end tell
    '''
    result = run_applescript(script)
    if not result:
        return None
    try:
        parts = [int(x.strip()) for x in result.split(",")]
        x1, y1, x2, y2 = parts
        return x1, y1, x2 - x1, y2 - y1  # x, y, w, h
    except Exception:
        return None


def capture_chat_region_screenshot(name: str = "chat") -> Path | None:
    """
    Captura solo el área de mensajes del panel chat de VS Code usando screencapture -R.
    Excluye el input box inferior para evitar falsos positivos por cursor parpadeante.
    Retorna el path al PNG guardado, o None si falla.
    """
    bounds = get_vscode_window_bounds()
    if bounds:
        wx, wy, ww, wh = bounds
        # Panel chat: columna derecha ~32%, excluyendo toolbar (top 5%) e input box (bottom 18%)
        x = wx + int(ww * 0.68)
        y = wy + int(wh * 0.05)
        w = int(ww * 0.32)
        h = int(wh * 0.77)  # Solo área de mensajes, no el input box
    else:
        sw, sh = _get_screen_size()
        x = int(sw * 0.68)
        y = int(sh * 0.05)
        w = int(sw * 0.32)
        h = int(sh * 0.72)

    ts = int(time.time())
    path = SCREENSHOTS_DIR / f"{name}_{ts}.png"
    result = subprocess.run(
        ["screencapture", "-x", "-R", f"{x},{y},{w},{h}", str(path)],
        capture_output=True
    )
    if result.returncode != 0 or not path.exists():
        log(f"screencapture -R falló: {result.stderr.decode()[:120]}", "WARN")
        return None
    return path


def hash_file(path: Path) -> str:
    """Retorna el hash MD5 de un archivo de imagen."""
    try:
        return hashlib.md5(path.read_bytes()).hexdigest()
    except Exception:
        return ""


def cleanup_old_screenshots(keep: int = SCREENSHOTS_KEEP):
    """Elimina screenshots antiguos, conservando solo los `keep` más recientes."""
    try:
        files = sorted(SCREENSHOTS_DIR.glob("*.png"), key=lambda p: p.stat().st_mtime)
        for f in files[:-keep]:
            try:
                f.unlink()
            except Exception:
                pass
    except Exception:
        pass


# ─────────────────────────────────────────────────────────────────────────────
# LÓGICA DE DETECCIÓN
# ─────────────────────────────────────────────────────────────────────────────

# ─── OpenClaw gateway ────────────────────────────────────────────────────────
_OC_GATEWAY = "ws://127.0.0.1:18789"
_OC_TOKEN   = "f24fcea34191739b761db1153cefd910706083c1546c9c00"
_OC_AGENT   = "copilot-watchdog"
_OC_TIMEOUT = 20  # segundos máximo esperando respuesta del agente


def _ask_watchdog_agent(ocr_text: str, extra_context: str = "") -> str | None:
    """
    Envía el OCR del chat al agente copilot-watchdog de OpenClaw vía WebSocket.
    El agente responde con una sola palabra: OK | CONTINUAR | NUEVO_CHAT | REINICIAR.
    Retorna la palabra en mayúsculas, o None si falla la conexión.
    """
    if not _WS_AVAILABLE:
        log("websocket-client no instalado — usando clasificación local", "WARN")
        return None

    result = {"decision": None, "authenticated": False, "done": False}
    event  = threading.Event()

    prompt = (
        f"Estado del chat de GitHub Copilot en VS Code (OCR):\n\n"
        f"{ocr_text or '(sin texto visible)'}"
        f"\n\n{extra_context}" if extra_context else
        f"Estado del chat de GitHub Copilot en VS Code (OCR):\n\n"
        f"{ocr_text or '(sin texto visible)'}"
    )

    def on_message(ws_conn, raw):
        try:
            msg = json.loads(raw)
        except Exception:
            return
        ev = msg.get("event") or msg.get("type", "")

        # Autenticación
        if ev == "connect.challenge":
            nonce = (msg.get("payload") or {}).get("nonce", "")
            sig   = hmac_mod.new(_OC_TOKEN.encode(), nonce.encode(), "sha256").hexdigest()
            ws_conn.send(json.dumps({
                "type": "authenticate",
                "payload": {"token": _OC_TOKEN, "hmac": sig, "nonce": nonce},
                "agentId": _OC_AGENT,
            }))

        if ev in ("connect.ready", "authenticated", "auth.ok", "ready", "agent.idle"):
            if not result["authenticated"]:
                result["authenticated"] = True
                ws_conn.send(json.dumps({
                    "type": "message",
                    "payload": {"text": prompt, "role": "user"},
                    "agentId": _OC_AGENT,
                }))

        # Respuesta del agente
        if ev in ("agent.message", "message", "message.complete"):
            text = ""
            payload = msg.get("payload") or {}
            if isinstance(payload, dict):
                text = payload.get("text") or payload.get("content") or ""
            elif isinstance(payload, str):
                text = payload
            word = text.strip().upper().split()[0] if text.strip() else ""
            if word in ("OK", "CONTINUAR", "NUEVO_CHAT", "REINICIAR"):
                result["decision"] = word
                result["done"]   = True
                event.set()
                ws_conn.close()

    def on_error(ws_conn, err):
        log(f"WS watchdog error: {err}", "WARN")
        event.set()

    def on_close(ws_conn, *_):
        event.set()

    try:
        ws_conn = websocket.WebSocketApp(
            _OC_GATEWAY,
            on_message=on_message,
            on_error=on_error,
            on_close=on_close,
        )
        t = threading.Thread(target=ws_conn.run_forever, daemon=True)
        t.start()
        event.wait(timeout=_OC_TIMEOUT)
        ws_conn.close()
    except Exception as exc:
        log(f"WS watchdog excepción: {exc}", "WARN")
        return None

    return result["decision"]


# Mapa de respuesta del agente → estados internos del monitor
_WATCHDOG_MAP = {
    "OK":          "WORKING",
    "CONTINUAR":   "ERROR",
    "NUEVO_CHAT":  "DONE",
    "REINICIAR":   "RESTART",
    "ESPERAR":     "USER_ACTIVE",
}

# ─── OpenClaw model rotation ──────────────────────────────────────────────────

def _restart_openclaw_gateway():
    """Reinicia el gateway de OpenClaw para aplicar cambios de configuración."""
    log("🔄 Reiniciando OpenClaw gateway...", "ACTION")
    result = subprocess.run(
        ["openclaw", "gateway", "restart"],
        capture_output=True, text=True, timeout=30
    )
    if result.returncode == 0:
        log("✅ OpenClaw gateway reiniciado")
        time.sleep(3)
    else:
        log(f"⚠️  openclaw gateway restart falló: {result.stderr[:200]}", "WARN")
        # Fallback: stop + start
        subprocess.run(["openclaw", "gateway", "stop"],
                       capture_output=True, timeout=10)
        time.sleep(1)
        subprocess.Popen(["openclaw", "gateway", "start"])
        time.sleep(3)


def _switch_openclaw_model(state: dict, now: float) -> str:
    """
    Rota al siguiente modelo en OPENCLAW_MODELS, actualiza openclaw.json
    y reinicia el gateway para aplicar el cambio.
    Rotación: gpt-4.1 → gpt-4o → gpt-4o-mini → gpt-4.1 → ...
    """
    current_idx   = state.get("openclaw_model_idx", 0)
    next_idx      = (current_idx + 1) % len(OPENCLAW_MODELS)
    current_model = OPENCLAW_MODELS[current_idx]
    next_model    = OPENCLAW_MODELS[next_idx]

    log(f"⚡ Rotando modelo OpenClaw: {current_model} → {next_model}", "ACTION")
    try:
        config = json.loads(OPENCLAW_CONFIG.read_text())
        # Actualizar primary en defaults
        config.setdefault("agents", {}).setdefault("defaults", {}).setdefault("model", {})
        config["agents"]["defaults"]["model"]["primary"] = next_model
        # Actualizar también en cada agente individual
        for agent in config["agents"].get("list", []):
            agent["model"] = next_model
        OPENCLAW_CONFIG.write_text(json.dumps(config, indent=2, ensure_ascii=False))
        log(f"✅ openclaw.json actualizado → primary={next_model}")
    except Exception as e:
        log(f"Error actualizando openclaw.json: {e}", "WARN")
        return current_model

    state["openclaw_model_idx"]   = next_idx
    state["last_model_switch_ts"] = now
    _restart_openclaw_gateway()
    return next_model


# ─── Vision-based state detection (Computer-Use pattern) ─────────────────────
# Metodología profesional usada por Claude Computer Use, OpenAI CUA, etc.:
# 1. Screenshot PNG → base64 encode
# 2. Llamada directa a Claude Vision API (multimodal) con la imagen
# 3. El modelo "ve" la pantalla como un humano y decide la acción
# Sin OCR intermedio: el LLM interpreta directamente los píxeles.

_VISION_MODEL   = "gpt-4o"            # Soporta visión nativa; fallback: gpt-4.1
_VISION_PROMPT  = """\
{temporal_section}\
TEXTO EXTRAÍDO POR OCR DEL CHAT:
---
{ocr_text}
---

MÉTRICAS DEL SISTEMA:
{extra_context}

Tu tarea: clasifica el estado del agente GitHub Copilot en VS Code y devuelve UNA SOLA PALABRA.

Opciones:
- OK         → Copilot activo: spinner visible, texto generándose/streaming, o
               workspace_sin_cambio < 150s (puede estar corriendo tools en background).
               NO intervenir aunque el chat parezca quieto.
- CONTINUAR  → Copilot terminó: respuesta completa visible, sin spinner, sin actividad,
               y workspace_sin_cambio >= 150s. Enviar "continuar".
- NUEVO_CHAT → Panel vacío, pantalla de bienvenida, o error grave sin recovery.
               Abrir un chat nuevo con el loop prompt.
- REINICIAR  → VS Code crash, panel del chat inexistente, estado corrupto.
- ESPERAR    → Se detecta CUALQUIER actividad humana en VS Code: el usuario escribe
               en el input del chat, navega el editor, abre/guarda archivos, hace
               clicks o scroll en cualquier panel, usa el terminal integrado, o
               cualquier interacción visual del humano (no es Copilot generando).
               Señales: cursor parpadeante en input, texto emergente, panel activo
               con foco de usuario, archivos recientemente abiertos en árbol lateral.

REGLA CRÍTICA: si workspace_sin_cambio < 150 en las métricas → responde OK.
El agente escribe archivos y corre comandos en background después de responder en el chat.
REGLA: cualquier señal de interacción humana activa en VS Code → ESPERAR.

Responde UNA SOLA PALABRA: OK | CONTINUAR | NUEVO_CHAT | REINICIAR | ESPERAR
"""


def _ask_vision_agent(screenshot_path: Path,
                      prev_screenshot_path: Path | None = None,
                      extra_context: str = "",
                      ocr_text: str = "") -> str | None:
    """
    Computer-Use pattern: envía hasta 2 screenshots al LLM con visión.
    - screenshot_path:      imagen ACTUAL del chat (obligatoria)
    - prev_screenshot_path: imagen ANTERIOR del chat (opcional, referencia temporal)
    - ocr_text:             texto OCR extraído del chat (embebido en el prompt)

    Con 2 imágenes el modelo puede comparar el estado anterior vs actual,
    detectando si hubo actividad (streaming, typing) entre ambas capturas.
    Cost: ~85 tokens por imagen en detail=low → 2 imgs ≈ 170 tokens totales.

    Modelos: gpt-4o (primary) → gpt-4.1 → raptor-mini
    """
    if not _VISION_AVAILABLE:
        return None

    api_key = os.environ.get("OPENAI_API_KEY")
    if not api_key:
        log("OPENAI_API_KEY no configurado — skipping vision", "WARN")
        return None

    try:
        current_data = base64.standard_b64encode(screenshot_path.read_bytes()).decode("utf-8")
    except Exception as e:
        log(f"No se pudo leer screenshot para vision: {e}", "WARN")
        return None

    # Construir lista de imágenes (prev primero, actual después)
    content: list = []
    has_prev = (
        prev_screenshot_path is not None
        and prev_screenshot_path.exists()
        and prev_screenshot_path != screenshot_path
    )
    if has_prev:
        try:
            prev_data = base64.standard_b64encode(prev_screenshot_path.read_bytes()).decode("utf-8")
            content.append({
                "type": "image_url",
                "image_url": {"url": f"data:image/png;base64,{prev_data}", "detail": "low"},
            })
            content.append({
                "type": "image_url",
                "image_url": {"url": f"data:image/png;base64,{current_data}", "detail": "low"},
            })
            temporal_section = (
                "Estás clasificando el estado del agente GitHub Copilot en VS Code.\n"
                "IMAGEN 1: estado ANTERIOR del chat (referencia temporal).\n"
                "IMAGEN 2: estado ACTUAL del chat (toma la decisión basándote en esta).\n"
            )
        except Exception:
            has_prev = False  # falló leer prev — caer a imagen única

    if not has_prev:
        content.append({
            "type": "image_url",
            "image_url": {"url": f"data:image/png;base64,{current_data}", "detail": "low"},
        })
        temporal_section = "Estás clasificando el estado del agente GitHub Copilot en VS Code.\nIMAGEN: estado actual del chat.\n"

    prompt_text = (
        _VISION_PROMPT
        .replace("{temporal_section}", temporal_section)
        .replace("{ocr_text}", ocr_text.strip() or "(sin texto visible)")
        .replace("{extra_context}", extra_context or "(ninguno)")
    )
    content.append({"type": "text", "text": prompt_text})

    # Intentar modelos en orden de preferencia
    models_to_try = [_VISION_MODEL, "gpt-4.1", "raptor-mini"]
    seen: set = set()
    ordered = [m for m in models_to_try if m not in seen and not seen.add(m)]

    for model in ordered:
        try:
            client = _openai_sdk.OpenAI(api_key=api_key)
            response = client.chat.completions.create(
                model=model,
                max_tokens=16,
                messages=[{"role": "user", "content": content}],
            )
            raw     = response.choices[0].message.content.strip().upper().split()[0]
            allowed = ("OK", "CONTINUAR", "NUEVO_CHAT", "REINICIAR")
            if raw in allowed:
                imgs = "2 imgs" if has_prev else "1 img"
                log(f"Vision LLM → {raw} ({model}, {imgs})")
                return raw
            for word in allowed:
                if word in raw:
                    log(f"Vision LLM → {word} (extraído de '{raw}', {model})")
                    return word
            log(f"Vision LLM respuesta inválida: '{raw}' ({model})", "WARN")
            return None
        except Exception as exc:
            log(f"Vision API error con {model}: {exc}", "WARN")
            continue

    return None


def classify_chat_state(ocr_text: str, debug: bool = False,
                        extra_context: str = "",
                        screenshot_path: Path | None = None,
                        prev_screenshot_path: Path | None = None) -> str:
    """
    Clasifica el estado del chat usando un pipeline de 3 capas:

    CAPA 1 — Vision LLM (Computer-Use pattern)
      Screenshot PNG → base64 → Claude Vision API (claude-haiku-4-5)
      El modelo VE la pantalla directamente, sin OCR intermedio.
      Metodología usada por Claude Computer Use, OpenAI CUA, Playwright Agent.

    CAPA 2 — OCR + OpenClaw watchdog (texto)
      Fallback cuando Vision no está disponible o falla.
      OCR text → WebSocket → copilot-watchdog (github-copilot/gpt-4.1)

    CAPA 3 — Regex local
      Fallback final sin dependencias externas.

    Respuesta → estado interno:
      OK          → WORKING  (Copilot trabajando, no intervenir)
      CONTINUAR   → ERROR    (enviar "continuar" al chat)
      NUEVO_CHAT  → DONE     (abrir nuevo chat con AGENT_LOOP_PROMPT)
      REINICIAR   → RESTART  (reiniciar VS Code)
    """
    if debug:
        print(f"\n{'='*60}\nOCR RAW:\n{ocr_text}\n{'='*60}\n")

    # ── CAPA 1: Vision LLM — Computer-Use pattern ─────────────────────────────
    if screenshot_path and screenshot_path.exists():
        decision = _ask_vision_agent(
            screenshot_path,
            prev_screenshot_path=prev_screenshot_path,
            extra_context=extra_context,
            ocr_text=ocr_text,
        )
        if decision is not None:
            mapped = _WATCHDOG_MAP.get(decision, "IDLE")
            log(f"[Vision] {decision} → {mapped}")
            return mapped
        log("Vision no disponible o falló — bajando a capa OCR+watchdog", "WARN")

    # ── CAPA 2: OCR text → OpenClaw watchdog LLM ──────────────────────────────
    decision = _ask_watchdog_agent(ocr_text, extra_context=extra_context)
    if decision is not None:
        mapped = _WATCHDOG_MAP.get(decision, "IDLE")
        log(f"[Watchdog OCR] {decision} → {mapped}")
        return mapped

    # ── CAPA 3: Regex local (fallback sin dependencias) ───────────────────────
    log("Gateway OCR no disponible — usando regex local de respaldo", "WARN")
    text_lower = ocr_text.lower()

    for pattern in WORKING_PATTERNS:
        if re.search(pattern, text_lower):
            return "WORKING"
    for pattern in ERROR_PATTERNS:
        if re.search(pattern, text_lower):
            return "ERROR"
    for pattern in DONE_PATTERNS:
        if re.search(pattern, text_lower):
            return "DONE"
    return "IDLE"


def is_task_complete_by_file() -> bool:
    """Retorna True si la última línea de prompt_1.md es READ."""
    last = read_prompt_last_line()
    return last.upper() == "READ"


def get_last_action_time() -> float:
    """
    Heurística: retorna cuántos segundos han pasado desde la última
    modificación de prompt_1.md.
    """
    try:
        return time.time() - PROMPT_FILE.stat().st_mtime
    except Exception:
        return 9999.0


# ─────────────────────────────────────────────────────────────────────────────
# ACCIONES
# ─────────────────────────────────────────────────────────────────────────────

def _click_chat_input():
    """Foca el input del chat de Copilot sin dependencias externas."""
    vscode_focus_chat_input()
    time.sleep(0.2)


def action_send_continue():
    """CAPA 1: enviar 'continuar' al chat activo."""
    log("🔁 CAPA 1: Enviando 'continuar' al chat...", "ACTION")
    if not is_vscode_running():
        log("VS Code no está corriendo", "WARN")
        return False
    vscode_type_continue()
    return True


def action_new_chat_with_prompt():
    """CAPA 2: abrir nuevo chat y enviar AGENT_LOOP_PROMPT."""
    log("🚀 CAPA 2: Abriendo nuevo chat con AGENT_LOOP_PROMPT...", "ACTION")
    if not is_vscode_running():
        log("VS Code no está corriendo — escalando a reinicio", "WARN")
        return False

    agent_prompt = read_agent_loop_prompt()
    if not agent_prompt:
        log("No se pudo leer AGENT_LOOP_PROMPT.md", "ERROR")
        return False

    # 1. Abrir nuevo chat
    vscode_open_new_chat()
    time.sleep(1.5)

    # 2. Click en el input
    _click_chat_input()

    # 3. Pegar el prompt
    vscode_type_in_chat(agent_prompt)
    log("✅ AGENT_LOOP_PROMPT enviado al nuevo chat")
    return True


def action_restart_vscode_with_prompt():
    """CAPA 3: reiniciar VS Code y cargar el prompt en nuevo chat."""
    log("💀 CAPA 3: Reiniciando VS Code...", "ACTION")
    restart_vscode()
    time.sleep(3)

    agent_prompt = read_agent_loop_prompt()
    if not agent_prompt:
        log("No se pudo leer AGENT_LOOP_PROMPT.md", "ERROR")
        return False

    # Abrir nuevo chat
    vscode_open_new_chat()
    time.sleep(2)

    # Click en el input
    _click_chat_input()

    # Pegar el prompt
    vscode_type_in_chat(agent_prompt)
    log("✅ VS Code reiniciado y AGENT_LOOP_PROMPT cargado")
    return True


# ─────────────────────────────────────────────────────────────────────────────
# LOOP PRINCIPAL — 3 capas de recuperación
# ─────────────────────────────────────────────────────────────────────────────

def monitor_cycle(state: dict, debug: bool = False) -> tuple[str, dict]:
    """
    Ejecuta un ciclo. Devuelve (resultado, state_actualizado).

    Lógica principal:
      1. Captura el área de mensajes del chat (sin el input box inferior).
      2. Compara el hash con el ciclo anterior.
         - Si cambió → Copilot está generando contenido → NO intervenir.
         - Si es idéntico → medir cuánto tiempo lleva estático.
      3. Si el chat lleva < VISUAL_STALL_THRESHOLD segundos estático → todavía
         puede estar en una pausa breve → NO intervenir.
      4. Si el chat lleva ≥ VISUAL_STALL_THRESHOLD segundos estático → OCR para
         diagnosticar (vacío / error / READ / stall) → actuar según corresponda.

    Máquina de estado:
      last_chat_hash        — hash del último screenshot del área de mensajes
      last_visual_change_ts — cuándo cambió visualmente el chat por última vez
      continue_attempts     — cuántas veces se envió "continuar" sin efecto
      new_chat_attempts     — cuántas veces se abrió nuevo chat sin efecto
      last_continue_ts      — timestamp del último "continuar"
      last_new_chat_ts      — timestamp del último nuevo chat
      last_restart_ts       — timestamp del último reinicio
      read_since_ts         — cuándo se detectó READ por primera vez
    """
    now = time.time()

    # ── 0. Cooldown usuario activo — no interferir mientras alguien escribe ────
    user_active_until = state.get("user_active_until_ts", 0.0)
    if now < user_active_until:
        remaining = user_active_until - now
        log(f"👤 Cooldown usuario activo — {remaining:.0f}s restantes, sin intervención")
        return "idle", state

    # ── 0.5. Actividad humana nativa macOS (HID + foco) — sin LLM ni screenshot ─
    # Detección inmediata de cualquier actividad en VS Code: teclado, mouse, click,
    # navegación de editor, terminal, árbol de archivos, etc.
    human_active, hid_desc = _is_human_active_in_vscode()
    if human_active:
        log(f"👤 Actividad humana detectada ({hid_desc}) — cooldown {USER_ACTIVE_GRACE}s")
        state["user_active_until_ts"] = now + USER_ACTIVE_GRACE
        state["last_visual_change_ts"] = now
        return "user_active", state

    # ── 1. Capturar área de mensajes del chat ──────────────────────────────────
    chat_sc = capture_chat_region_screenshot("chat")
    if chat_sc is None:
        log("No se pudo capturar región del chat — saltando ciclo", "WARN")
        return "idle", state

    # ── 2. Diferencia visual — detector primario de actividad ──────────────────
    current_hash = hash_file(chat_sc)
    prev_hash = state.get("last_chat_hash", "")

    if current_hash and current_hash != prev_hash:
        # El área de mensajes cambió → Copilot está generando contenido
        state["prev_chat_screenshot"] = state.get("last_chat_screenshot", "")
        state["last_chat_screenshot"] = str(chat_sc)
        state["last_chat_hash"] = current_hash
        state["last_visual_change_ts"] = now
        state["continue_attempts"] = 0
        state["new_chat_attempts"] = 0
        state["read_since_ts"] = None
        log("Copilot activo (chat cambiando) — no se interviene ✓")
        cleanup_old_screenshots()
        return "active", state

    # Calcular cuánto tiempo lleva el chat estático
    secs_idle = now - state.get("last_visual_change_ts", 0)
    log(f"Chat estático: {secs_idle:.0f}s / umbral={VISUAL_STALL_THRESHOLD}s")

    # ── 3. Dentro del umbral → pausa normal, no intervenir ────────────────────
    if secs_idle < VISUAL_STALL_THRESHOLD:
        log("Pausa breve — esperando antes de actuar")
        cleanup_old_screenshots()
        return "idle", state

    # ── 3.5. Workspace settle: si prompt_1.md cambió recientemente, esperar ────
    # Copilot puede estar corriendo tool calls / escribiendo archivos en background
    # aunque el chat ya no tenga streaming activo.
    secs_since_prompt = get_last_action_time()
    if secs_since_prompt < WORKSPACE_SETTLE_TIME:
        log(f"Workspace activo hace {secs_since_prompt:.0f}s < {WORKSPACE_SETTLE_TIME}s — esperando settle")
        cleanup_old_screenshots()
        return "idle", state

    # ── 4. Chat estático demasiado tiempo → diagnosticar con OCR ──────────────
    log(f"⚠️  Chat estático {secs_idle:.0f}s, workspace {secs_since_prompt:.0f}s — analizando...", "WARN")

    # OCR sobre la imagen ya recortada del chat (region=None porque ya es el crop)
    ocr_text = ocr_image(chat_sc, region=None)

    last_line = read_prompt_last_line()
    file_is_read = last_line.upper() == "READ"
    secs_since_mod = get_last_action_time()

    # Contexto para el LLM: información del sistema con claves reconocibles en el prompt
    extra_context = (
        f"workspace_sin_cambio: {secs_since_mod:.0f}s\n"
        f"chat_estatico: {secs_idle:.0f}s\n"
        f"ultima_linea_prompt: '{last_line}'\n"
        f"intentos_continuar: {state.get('continue_attempts', 0)}\n"
        f"intentos_nuevo_chat: {state.get('new_chat_attempts', 0)}"
    )
    prev_sc_path = Path(state["prev_chat_screenshot"]) if state.get("prev_chat_screenshot") else None
    chat_ocr_state = classify_chat_state(
        ocr_text,
        debug=debug,
        extra_context=extra_context,
        screenshot_path=chat_sc,
        prev_screenshot_path=prev_sc_path,
    )

    log(f"State={chat_ocr_state} | prompt_1='{last_line}' | archivo_sin_cambio={secs_since_mod:.0f}s")

    # ── 4a.0. Fallo de modelo detectado → rotar OpenClaw automáticamente ───────
    if any(re.search(p, ocr_text.lower()) for p in MODEL_FAIL_PATTERNS):
        if now - state.get("last_model_switch_ts", 0) > COOLDOWN_MODEL_SW:
            log("⚡ Fallo de modelo detectado en chat — rotando OpenClaw model", "ACTION")
            _switch_openclaw_model(state, now)

    # ── 4a. Chat vacío → cargar el prompt inmediatamente ─────────────────────
    ocr_lower = ocr_text.lower()
    chat_empty = any(re.search(p, ocr_lower) for p in EMPTY_CHAT_PATTERNS)
    if chat_empty and (now - state.get("last_new_chat_ts", 0) > COOLDOWN_NEW_CHAT):
        log("Chat vacío detectado — cargando AGENT_LOOP_PROMPT", "ACTION")
        _click_chat_input()
        time.sleep(0.3)
        agent_prompt = read_agent_loop_prompt()
        if agent_prompt:
            vscode_type_in_chat(agent_prompt)
            state["last_new_chat_ts"] = now
            state["new_chat_attempts"] = state.get("new_chat_attempts", 0) + 1
            state["continue_attempts"] = 0
            state["last_visual_change_ts"] = now   # reiniciar el reloj visual
        cleanup_old_screenshots()
        return "prompt_loaded", state

    # ── 4b. Error / RESTART explícito detectado por agente LLM ──────────────
    if chat_ocr_state == "RESTART":
        cleanup_old_screenshots()
        return _handle_error(state, now, "REINICIAR (agente watchdog)")

    if chat_ocr_state == "ERROR":
        cleanup_old_screenshots()
        return _handle_error(state, now, "error OCR")

    # ── 4b2. Usuario activo detectado por visión → retroceder ─────────────────
    if chat_ocr_state == "USER_ACTIVE":
        log(f"👤 Usuario activo detectado — sin intervención por {USER_ACTIVE_GRACE}s", "ACTION")
        state["user_active_until_ts"] = now + USER_ACTIVE_GRACE
        state["last_visual_change_ts"] = now  # reiniciar reloj visual
        cleanup_old_screenshots()
        return "user_active", state

    # ── 4c. READ estable → tarea completada, despachar nuevo ciclo ────────────
    if file_is_read:
        read_since = state.get("read_since_ts")
        if read_since is None:
            state["read_since_ts"] = now
            log(f"READ detectado — esperando {READ_STABLE_THRESHOLD}s para confirmar")
        else:
            elapsed = now - read_since
            log(f"READ estable {elapsed:.0f}s / {READ_STABLE_THRESHOLD}s")
            if elapsed >= READ_STABLE_THRESHOLD:
                if now - state.get("last_new_chat_ts", 0) > COOLDOWN_NEW_CHAT:
                    log("✅ Tarea completada — abriendo nuevo chat con siguiente ciclo", "ACTION")
                    ok = action_new_chat_with_prompt()
                    if ok:
                        state["last_new_chat_ts"] = now
                        state["read_since_ts"] = None
                        state["continue_attempts"] = 0
                        state["new_chat_attempts"] = 0
                        state["last_visual_change_ts"] = 0.0  # forzar re-evaluación visual
                        cleanup_old_screenshots()
                        return "new_chat_sent", state
        cleanup_old_screenshots()
        return "idle", state
    else:
        state["read_since_ts"] = None  # archivo cambió desde READ anterior, reset

    # ── 4d. DONE sin READ → Copilot terminó pero no escribió READ ─────────────
    if chat_ocr_state == "DONE" and secs_since_mod > 300:
        cleanup_old_screenshots()
        return _handle_error(state, now, "DONE sin READ por >5min")

    # ── 4e. Stall genérico — chat estático + archivo sin cambio ───────────────
    if secs_since_mod > STALL_THRESHOLD:
        cleanup_old_screenshots()
        return _handle_error(state, now, f"stall visual={secs_idle:.0f}s archivo={secs_since_mod:.0f}s")

    cleanup_old_screenshots()
    return "idle", state


def _handle_error(state: dict, now: float, reason: str) -> tuple[str, dict]:
    """
    Lógica de escalamiento por capas:
      continuar → nuevo chat → reiniciar VS Code
    """
    continue_attempts = state.get("continue_attempts", 0)
    new_chat_attempts = state.get("new_chat_attempts", 0)
    last_continue = state.get("last_continue_ts", 0)
    last_new_chat = state.get("last_new_chat_ts", 0)
    last_restart = state.get("last_restart_ts", 0)

    log(f"⚠️  Problema detectado: {reason} | intentos continuar={continue_attempts} nuevo_chat={new_chat_attempts}", "WARN")

    # ─ CAPA 3: Reiniciar VS Code ─────────────────────────────────────────────
    if new_chat_attempts >= MAX_NEW_CHAT_ATTEMPTS:
        if now - last_restart > COOLDOWN_RESTART:
            log("🔴 Escalando a CAPA 3: Reinicio VS Code", "ACTION")
            ok = action_restart_vscode_with_prompt()
            if ok:
                state["last_restart_ts"] = now
                state["continue_attempts"] = 0
                state["new_chat_attempts"] = 0
                state["read_since_ts"] = None
            return "restarted", state
        else:
            remaining = COOLDOWN_RESTART - (now - last_restart)
            log(f"Cooldown reinicio: {remaining:.0f}s restantes")
            return "cooldown", state

    # ─ CAPA 2: Nuevo chat ────────────────────────────────────────────────────
    if continue_attempts >= MAX_CONTINUE_ATTEMPTS:
        if now - last_new_chat > COOLDOWN_NEW_CHAT:
            log("🟠 Escalando a CAPA 2: Nuevo chat", "ACTION")
            ok = action_new_chat_with_prompt()
            if ok:
                state["last_new_chat_ts"] = now
                state["new_chat_attempts"] = new_chat_attempts + 1
                state["continue_attempts"] = 0
                state["read_since_ts"] = None
            return "new_chat_sent", state
        else:
            remaining = COOLDOWN_NEW_CHAT - (now - last_new_chat)
            log(f"Cooldown nuevo chat: {remaining:.0f}s restantes")
            return "cooldown", state

    # ─ CAPA 1: Enviar "continuar" ────────────────────────────────────────────
    if now - last_continue > COOLDOWN_CONTINUE:
        log("🟡 CAPA 1: Enviando 'continuar'", "ACTION")
        ok = action_send_continue()
        if ok:
            state["last_continue_ts"] = now
            state["continue_attempts"] = continue_attempts + 1
        return "continue_sent", state
    else:
        remaining = COOLDOWN_CONTINUE - (now - last_continue)
        log(f"Cooldown 'continuar': {remaining:.0f}s restantes")
        return "cooldown", state


# ─────────────────────────────────────────────────────────────────────────────
# MAIN
# ─────────────────────────────────────────────────────────────────────────────

def _default_state() -> dict:
    return {
        "continue_attempts": 0,
        "new_chat_attempts": 0,
        "last_continue_ts": 0.0,
        "last_new_chat_ts": 0.0,
        "last_restart_ts": 0.0,
        "last_working_ts": 0.0,
        "read_since_ts": None,
        "last_chat_hash": "",          # Hash del último screenshot del área de mensajes
        "last_visual_change_ts": 0.0,  # Cuándo cambió visualmente el chat por última vez
        "user_active_until_ts": 0.0,   # Hasta cuándo esperar por actividad de usuario
        "openclaw_model_idx": 0,       # Índice del modelo activo en OPENCLAW_MODELS
        "last_model_switch_ts": 0.0,   # Timestamp de la última rotación de modelo
    }


def main():
    parser = argparse.ArgumentParser(
        description="Guardián de GitHub Copilot — 3 capas de recuperación"
    )
    parser.add_argument("--once", action="store_true", help="Un solo ciclo")
    parser.add_argument("--interval", type=int, default=20,
                        help="Segundos entre ciclos (default: 20)")
    parser.add_argument("--debug", action="store_true",
                        help="Mostrar OCR raw en consola")
    parser.add_argument("--action-continue", action="store_true",
                        help="Enviar 'continuar' al chat ahora")
    parser.add_argument("--action-new-chat", action="store_true",
                        help="Abrir nuevo chat + AGENT_LOOP_PROMPT ahora")
    parser.add_argument("--action-restart-vscode", action="store_true",
                        help="Reiniciar VS Code + cargar prompt ahora")
    parser.add_argument("--screenshot", action="store_true",
                        help="Screenshot + OCR + estado (sin actuar)")
    parser.add_argument("--reset-state", action="store_true",
                        help="Limpiar estado de recuperación guardado")
    args = parser.parse_args()

    log("=" * 60)
    log("🛡️  Copilot Guardian iniciado")
    log(f"   prompt_1.md : {PROMPT_FILE}")
    log(f"   AGENT_LOOP  : {AGENT_LOOP_FILE}")
    log(f"   Screenshots : {SCREENSHOTS_DIR}")
    log(f"   Intervalo   : {args.interval}s")
    log(f"   Stall umbral: {STALL_THRESHOLD}s")
    log(f"   Capas       : continuar×{MAX_CONTINUE_ATTEMPTS} → nuevo chat×{MAX_NEW_CHAT_ATTEMPTS} → restart")
    log("=" * 60)

    # ── Acciones directas ────────────────────────────────────────────────────
    if args.reset_state:
        save_state(_default_state())
        log("Estado de recuperación reseteado")
        return

    if args.action_continue:
        action_send_continue()
        return

    if args.action_new_chat:
        action_new_chat_with_prompt()
        return

    if args.action_restart_vscode:
        action_restart_vscode_with_prompt()
        return

    if args.screenshot:
        sc = take_screenshot("debug")
        if sc:
            text = ocr_image(sc, region=get_vscode_chat_bounds())
            state = classify_chat_state(text, debug=True)
            last_line = read_prompt_last_line()
            secs = get_last_action_time()
            print(f"\nEstado chat : {state}")
            print(f"prompt_1.md : última='{last_line}' | sin cambios={secs:.0f}s")
            print(f"Screenshot  : {sc}")
        return

    # ── Un solo ciclo ────────────────────────────────────────────────────────
    if args.once:
        state = load_state() or _default_state()
        result, state = monitor_cycle(state, debug=args.debug)
        save_state(state)
        log(f"Resultado: {result}")
        return

    # ── Loop continuo ────────────────────────────────────────────────────────
    log("")
    log("⚙️  Loop continuo activo. Ctrl+C para detener.")
    log("⚠️  Permisos requeridos (una sola vez):")
    log("    System Settings → Privacy → Screen Recording → Terminal ✓")
    log("    System Settings → Privacy → Accessibility    → Terminal ✓")
    log("")

    # Cargar o inicializar estado
    state = load_state()
    if not state:
        state = _default_state()
        log("Estado nuevo inicializado")
    else:
        ca = state.get('continue_attempts', 0)
        na = state.get('new_chat_attempts', 0)
        log(f"Estado cargado: continuar_intentos={ca} nuevo_chat_intentos={na}")

    cycle = 0
    try:
        while True:
            cycle += 1
            log(f"── CICLO {cycle} {'─'*40}")
            result, state = monitor_cycle(state, debug=args.debug)
            save_state(state)
            log(f"→ {result} | durmiendo {args.interval}s\n")
            time.sleep(args.interval)
    except KeyboardInterrupt:
        save_state(state)
        log("\n🛑 Monitor detenido por usuario (Ctrl+C)")
        log(f"   Estado guardado en {STATE_FILE}")


if __name__ == "__main__":
    main()
