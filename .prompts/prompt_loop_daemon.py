#!/usr/bin/env python3
"""prompt_loop_daemon.py — monitor continuo de .prompts/prompt_1.md.

Implementa la parte persistente del loop pedido en AGENT_LOOP_PROMPT.md:
  - sleep con backoff 60 -> 120 -> 240 ... -> 900 cuando prompt_1 termina en READ
  - STOP como unica condicion de salida
  - despacho de AGENT_LOOP_PROMPT al agente cuando aparecen tareas nuevas

La ejecucion de las tareas sigue ocurriendo dentro del agente de Copilot.
Este daemon solo vigila prompt_1 y reactiva el agente cuando corresponde.
"""

from __future__ import annotations

import argparse
import hashlib
import os
import re
import signal
import subprocess
import sys
import time
from datetime import datetime
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parent.parent
PROMPTS_DIR = REPO_ROOT / ".prompts"
PROMPT_FILE = PROMPTS_DIR / "prompt_1.md"
LOOP_PROMPT_FILE = PROMPTS_DIR / "AGENT_LOOP_PROMPT.md"
STATE_FILE = PROMPTS_DIR / ".prompt_loop_daemon.state.json"
PID_FILE = PROMPTS_DIR / ".prompt_loop_daemon.pid"
LOG_FILE = REPO_ROOT / ".github" / "prompt-loop-daemon.log"
WORKSPACE_SETTINGS_FILE = REPO_ROOT / ".vscode" / "settings.json"
USER_SETTINGS_FILE = Path.home() / "Library" / "Application Support" / "Code" / "User" / "settings.json"
SMART_MONITOR_START = PROMPTS_DIR / "agent" / "start-monitor.py"
SMART_MONITOR_PID_FILE = PROMPTS_DIR / "agent" / "smart_monitor" / ".agent_pid"
WATCHDOG_PID_FILE = PROMPTS_DIR / "agent" / ".monitor_pid"

DEFAULT_SLEEP = 60
MAX_SLEEP = 900
RETRY_SLEEP = 30
REDISPATCH_AFTER = 600


def log(message: str, level: str = "INFO") -> None:
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    line = f"[{timestamp}] [{level}] {message}"
    print(line, flush=True)
    try:
        LOG_FILE.parent.mkdir(parents=True, exist_ok=True)
        with LOG_FILE.open("a", encoding="utf-8") as handle:
            handle.write(line + "\n")
    except Exception:
        pass


def is_pid_running(pid: int) -> bool:
    try:
        os.kill(pid, 0)
        return True
    except (ProcessLookupError, PermissionError, ValueError):
        return False


def read_pid(pid_file: Path) -> int | None:
    try:
        return int(pid_file.read_text(encoding="utf-8").strip())
    except Exception:
        return None


def default_state() -> dict:
    now = time.time()
    return {
        "started_at": now,
        "updated_at": now,
        "status": "starting",
        "sleep_seconds": DEFAULT_SLEEP,
        "last_line": "READ",
        "last_hash": "",
        "last_dispatch_hash": "",
        "last_dispatch_ts": 0.0,
        "delivery_mode": get_prompt_delivery_mode(),
    }


def load_state() -> dict:
    defaults = default_state()
    try:
        import json

        loaded = json.loads(STATE_FILE.read_text(encoding="utf-8"))
        for key, value in defaults.items():
            loaded.setdefault(key, value)
        return loaded
    except Exception:
        return defaults


def save_state(state: dict) -> None:
    try:
        import json

        state["updated_at"] = time.time()
        STATE_FILE.parent.mkdir(parents=True, exist_ok=True)
        STATE_FILE.write_text(json.dumps(state, indent=2), encoding="utf-8")
    except Exception:
        pass


def read_setting(key: str, default: str) -> str:
    pattern = re.compile(rf'"{re.escape(key)}"\s*:\s*"([^"]+)"')
    for settings_path in (WORKSPACE_SETTINGS_FILE, USER_SETTINGS_FILE):
        try:
            if not settings_path.exists():
                continue
            match = pattern.search(settings_path.read_text(encoding="utf-8"))
            if match:
                return match.group(1)
        except Exception:
            continue
    return default


def get_prompt_delivery_mode() -> str:
    return read_setting("modelCycler.agent.promptDeliveryMode", "chat")


def get_agent_console_path() -> Path:
    configured = read_setting("modelCycler.agent.promptConsolePath", ".prompts/agent_console.md")
    configured_path = Path(configured)
    if configured_path.is_absolute():
        return configured_path
    return REPO_ROOT / configured_path


def append_to_agent_console(message: str, source: str, reason: str) -> Path:
    target = get_agent_console_path()
    target.parent.mkdir(parents=True, exist_ok=True)
    payload = "\n".join(
        [
            f"## {datetime.now().isoformat()} | prompt",
            f"source: {source}",
            f"reason: {reason}",
            "",
            message.rstrip(),
            "",
            "---",
            "",
        ]
    )
    with target.open("a", encoding="utf-8") as handle:
        handle.write(payload)
    return target


def send_via_code_chat(message: str) -> tuple[bool, str]:
    try:
        result = subprocess.run(
            ["code", "chat", "--mode", "agent", "--reuse-window", message],
            capture_output=True,
            text=True,
            cwd=str(REPO_ROOT),
            timeout=60,
        )
    except Exception as exc:
        return False, str(exc)

    output = (result.stdout or result.stderr or "").strip()
    if result.returncode == 0:
        return True, output
    return False, output or f"code chat exited with {result.returncode}"


def read_loop_prompt() -> str:
    try:
        prompt_text = LOOP_PROMPT_FILE.read_text(encoding="utf-8").strip()
    except Exception:
        prompt_text = "Ejecuta las tareas pendientes en .prompts/prompt_1.md"
    return prompt_text


def ensure_smart_monitor_running() -> None:
    existing_pid = read_pid(SMART_MONITOR_PID_FILE)
    if existing_pid and is_pid_running(existing_pid):
        return

    if not SMART_MONITOR_START.exists():
        log("Smart Monitor launcher no existe; continúo sin babysitter del chat", "WARN")
        return

    try:
        result = subprocess.run(
            [sys.executable, str(SMART_MONITOR_START)],
            capture_output=True,
            text=True,
            cwd=str(REPO_ROOT),
            timeout=30,
        )
        summary = (result.stdout or result.stderr or "").strip().replace("\n", " ")
        if result.returncode == 0:
            log(f"Smart Monitor asegurado: {summary[:200]}")
        else:
            log(f"No se pudo arrancar Smart Monitor: {summary[:200]}", "WARN")
    except Exception as exc:
        log(f"Error arrancando Smart Monitor: {exc}", "WARN")


def dispatch_loop_prompt(reason: str) -> bool:
    ensure_smart_monitor_running()
    prompt_text = read_loop_prompt()
    delivery_mode = get_prompt_delivery_mode()

    if delivery_mode == "agentConsole":
        target = append_to_agent_console(
            prompt_text,
            source="prompt_loop_daemon.dispatch_loop_prompt",
            reason=reason,
        )
        log(f"AGENT_LOOP_PROMPT entregado en {target}")
        return True

    ok, output = send_via_code_chat(prompt_text)
    if ok:
        if output:
            log(f"AGENT_LOOP_PROMPT enviado via code chat: {output[:200]}")
        else:
            log("AGENT_LOOP_PROMPT enviado via code chat")
        return True

    log(f"code chat falló; usando fallback agentConsole: {output[:200]}", "WARN")
    target = append_to_agent_console(
        prompt_text,
        source="prompt_loop_daemon.dispatch_loop_prompt",
        reason=f"fallback:{reason}",
    )
    log(f"AGENT_LOOP_PROMPT encolado en {target}")
    return True


def prompt_last_line_and_hash() -> tuple[str, str]:
    try:
        raw = PROMPT_FILE.read_text(encoding="utf-8")
    except FileNotFoundError:
        return "READ", ""

    if not raw.strip():
        return "READ", ""

    last_line = "READ"
    for line in reversed(raw.splitlines()):
        candidate = line.strip()
        if candidate:
            last_line = candidate
            break

    content_hash = hashlib.md5(raw.encode("utf-8")).hexdigest()
    return last_line, content_hash


def stop_monitor_from_pid(pid_file: Path, label: str) -> None:
    pid = read_pid(pid_file)
    if not pid or not is_pid_running(pid):
        try:
            pid_file.unlink(missing_ok=True)
        except Exception:
            pass
        return

    try:
        os.kill(pid, signal.SIGTERM)
        log(f"{label} detenido por STOP (PID {pid})")
    except Exception as exc:
        log(f"No se pudo detener {label} (PID {pid}): {exc}", "WARN")
    finally:
        try:
            pid_file.unlink(missing_ok=True)
        except Exception:
            pass


def stop_related_monitors() -> None:
    stop_monitor_from_pid(SMART_MONITOR_PID_FILE, "Smart Monitor")
    stop_monitor_from_pid(WATCHDOG_PID_FILE, "Copilot Watchdog")


def cleanup_pidfile() -> None:
    try:
        current_pid = read_pid(PID_FILE)
        if current_pid == os.getpid():
            PID_FILE.unlink(missing_ok=True)
    except Exception:
        pass


def install_signal_handlers(state: dict) -> None:
    def _handle_signal(signum: int, _frame) -> None:
        state["status"] = f"stopped_by_signal_{signum}"
        save_state(state)
        cleanup_pidfile()
        log(f"Daemon detenido por señal {signum}")
        raise SystemExit(0)

    signal.signal(signal.SIGTERM, _handle_signal)
    signal.signal(signal.SIGINT, _handle_signal)


def sleep_with_state(state: dict, seconds: int) -> None:
    state["status"] = "sleeping"
    state["sleep_seconds"] = seconds
    save_state(state)
    for _ in range(seconds):
        time.sleep(1)


def status_command() -> int:
    state = load_state()
    daemon_pid = read_pid(PID_FILE)
    running = bool(daemon_pid and is_pid_running(daemon_pid))
    updated_at = state.get("updated_at", 0.0)
    updated_delta = max(0, int(time.time() - updated_at)) if updated_at else -1
    dispatch_delta = max(0, int(time.time() - state.get("last_dispatch_ts", 0.0))) if state.get("last_dispatch_ts") else -1

    print("=== Prompt Loop Daemon ===")
    print(f"  PID activo        : {daemon_pid if running else 'no'}")
    print(f"  Estado            : {state.get('status', 'unknown')}")
    print(f"  Sleep actual      : {state.get('sleep_seconds', DEFAULT_SLEEP)}s")
    print(f"  Ultima linea      : {state.get('last_line', '')}")
    print(f"  Delivery mode     : {get_prompt_delivery_mode()}")
    print(f"  Ultimo update     : hace {updated_delta}s" if updated_delta >= 0 else "  Ultimo update     : n/d")
    print(f"  Ultimo dispatch   : hace {dispatch_delta}s" if dispatch_delta >= 0 else "  Ultimo dispatch   : nunca")
    print(f"  Smart Monitor PID : {read_pid(SMART_MONITOR_PID_FILE) or 'no'}")
    return 0


def stop_command() -> int:
    daemon_pid = read_pid(PID_FILE)
    if not daemon_pid or not is_pid_running(daemon_pid):
        cleanup_pidfile()
        print("prompt_loop_daemon: no hay proceso corriendo")
        return 0

    os.kill(daemon_pid, signal.SIGTERM)
    print(f"prompt_loop_daemon: detenido PID {daemon_pid}")
    return 0


def ensure_single_instance() -> None:
    existing_pid = read_pid(PID_FILE)
    if existing_pid and existing_pid != os.getpid() and is_pid_running(existing_pid):
        raise SystemExit(f"prompt_loop_daemon ya está corriendo con PID {existing_pid}")
    PID_FILE.write_text(str(os.getpid()), encoding="utf-8")


def run_loop() -> int:
    ensure_single_instance()
    state = load_state()
    install_signal_handlers(state)

    current_sleep = DEFAULT_SLEEP
    state["status"] = "running"
    state["sleep_seconds"] = current_sleep
    state["delivery_mode"] = get_prompt_delivery_mode()
    save_state(state)
    log("Prompt loop daemon iniciado")

    while True:
        sleep_with_state(state, current_sleep)

        while True:
            try:
                last_line, content_hash = prompt_last_line_and_hash()
                break
            except Exception as exc:
                state["status"] = "read_error"
                save_state(state)
                log(f"Error leyendo prompt_1.md: {exc}. Reintento en {RETRY_SLEEP}s", "WARN")
                time.sleep(RETRY_SLEEP)

        state["last_line"] = last_line or ""
        state["last_hash"] = content_hash
        state["delivery_mode"] = get_prompt_delivery_mode()
        save_state(state)

        if last_line == "STOP":
            state["status"] = "stopped_by_stop"
            state["sleep_seconds"] = 0
            state["last_dispatch_hash"] = ""
            save_state(state)
            log("Loop detenido por STOP en prompt_1.md")
            stop_related_monitors()
            cleanup_pidfile()
            return 0

        if last_line == "READ":
            state["status"] = "no_tasks"
            state["last_dispatch_hash"] = ""
            current_sleep = min(max(DEFAULT_SLEEP, current_sleep * 2), MAX_SLEEP)
            state["sleep_seconds"] = current_sleep
            save_state(state)
            log(f"prompt_1.md sin tareas; proximo sleep={current_sleep}s")
            continue

        current_sleep = DEFAULT_SLEEP
        now = time.time()
        last_dispatch_hash = state.get("last_dispatch_hash", "")
        last_dispatch_ts = float(state.get("last_dispatch_ts", 0.0) or 0.0)
        should_dispatch = (
            not content_hash
            or content_hash != last_dispatch_hash
            or (now - last_dispatch_ts) >= REDISPATCH_AFTER
        )

        if should_dispatch:
            ok = dispatch_loop_prompt("prompt_1")
            if ok:
                state["status"] = "dispatched"
                state["last_dispatch_hash"] = content_hash
                state["last_dispatch_ts"] = now
                save_state(state)
                log("Tareas detectadas en prompt_1.md; AGENT_LOOP_PROMPT despachado")
                continue

            state["status"] = "dispatch_failed"
            save_state(state)
            log(f"No se pudo despachar AGENT_LOOP_PROMPT. Reintento en {RETRY_SLEEP}s", "WARN")
            time.sleep(RETRY_SLEEP)
            continue

        state["status"] = "tasks_pending"
        save_state(state)
        log("prompt_1.md sigue pendiente; esperando READ o cambios antes de redispatch")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Daemon del loop de prompt_1")
    parser.add_argument("--status", action="store_true", help="Muestra estado y sale")
    parser.add_argument("--stop", action="store_true", help="Detiene el daemon por PID")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    if args.status:
        return status_command()
    if args.stop:
        return stop_command()
    return run_loop()


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    finally:
        cleanup_pidfile()