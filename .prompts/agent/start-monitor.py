#!/usr/bin/env python3
"""
start-monitor.py — Lanza el Smart Monitor con Gemma 3 como daemon desconectado.

Uso:
  python3 .prompts/agent/start-monitor.py            # arranca el daemon
  python3 .prompts/agent/start-monitor.py --stop     # mata el daemon por PID
  python3 .prompts/agent/start-monitor.py --status   # muestra si esta corriendo
"""
import subprocess
import sys
from pathlib import Path

REPO    = Path(__file__).parent.parent.parent
MONITOR = REPO / ".prompts" / "agent" / "smart_monitor" / "agent.py"
LOG     = REPO / ".github" / "smart-monitor.log"
PIDFILE = REPO / ".prompts" / "agent" / "smart_monitor" / ".agent_pid"
PYTHON  = sys.executable


def is_running(pid: int) -> bool:
    try:
        import os; os.kill(pid, 0)
        return True
    except (ProcessLookupError, PermissionError):
        return False


def start():
    # Verificar si ya hay uno corriendo
    if PIDFILE.exists():
        try:
            pid = int(PIDFILE.read_text().strip())
            if is_running(pid):
                print(f"[start-monitor] El Smart Monitor ya esta corriendo (PID {pid}). Usa --stop para detenerlo.")
                return
        except ValueError:
            pass

    LOG.parent.mkdir(parents=True, exist_ok=True)

    proc = subprocess.Popen(
        [PYTHON, str(MONITOR), "--interval", "20"],
        stdin=subprocess.DEVNULL,          # desconectado del terminal
        stdout=open(LOG, "a"),
        stderr=subprocess.STDOUT,
        start_new_session=True,            # nueva sesion: inmune a SIGINT/SIGHUP del terminal
        cwd=str(REPO),
    )
    PIDFILE.write_text(str(proc.pid))
    print(f"[start-monitor] Smart Monitor iniciado (PID {proc.pid})")
    print(f"[start-monitor] Log: {LOG}")


def stop():
    if not PIDFILE.exists():
        print("[start-monitor] No hay PID guardado.")
        return
    pid = int(PIDFILE.read_text().strip())
    if is_running(pid):
        import os, signal
        os.kill(pid, signal.SIGTERM)
        print(f"[start-monitor] Smart Monitor detenido (PID {pid})")
    else:
        print(f"[start-monitor] PID {pid} no esta corriendo.")
    PIDFILE.unlink(missing_ok=True)


def status():
    if not PIDFILE.exists():
        print("[start-monitor] No hay daemon corriendo (sin PID guardado).")
        return
    pid = int(PIDFILE.read_text().strip())
    if is_running(pid):
        print(f"[start-monitor] SMART MONITOR ACTIVO — PID {pid}")
    else:
        print(f"[start-monitor] INACTIVO — PID {pid} ya no existe.")


if __name__ == "__main__":
    if "--stop" in sys.argv:
        stop()
    elif "--status" in sys.argv:
        status()
    else:
        start()
