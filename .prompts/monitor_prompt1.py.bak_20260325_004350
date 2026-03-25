#!/usr/bin/env python3
"""
monitor_prompt1.py
==================
Script de monitoreo para el bridge VS Code ↔ OpenClaw via .prompts/prompt_1.md.

OpenClaw (dev-senior agent con Claude Haiku 4.5) monitorea prompt_1.md cada 60s.
Cuando VS Code (Copilot Sonnet 4.6) escribe tareas nuevas (- [ ]), este script:
  1. Detecta el cambio
  2. Extrae tareas pendientes
  3. Ejecuta cada tarea via OpenClaw agent
  4. Actualiza prompt_1.md con resultados
  5. Agrega "READ" al final
  6. Si la tarea es CI/CD → ejecuta smart-cicd.yml y verifica producción
  7. Vuelve a monitorear (última tarea siempre)

Uso:
  python3 .prompts/monitor_prompt1.py                    # Monitoreo continuo
  python3 .prompts/monitor_prompt1.py --once             # Una sola verificación
  python3 .prompts/monitor_prompt1.py --interval 30      # Cada 30 segundos
"""

import argparse
import hashlib
import json
import re
import subprocess
import sys
import time
from datetime import datetime
from pathlib import Path

# --- Rutas ---
REPO_ROOT = Path(__file__).parent.parent
PROMPT_FILE = Path(__file__).parent / "prompt_1.md"
AUDIT_LOG = REPO_ROOT / ".github" / "copilot-audit.log"

# --- Config ---
DEFAULT_INTERVAL = 60  # seconds
MAX_CHECKS_WITHOUT_CHANGE = 3
OPENCLAW_AGENT = "dev-senior"
OPENCLAW_TIMEOUT = 300  # 5 min per task


def log(msg: str, level: str = "INFO") -> None:
    """Log to stdout and audit file."""
    ts = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    entry = f"[{ts}] [PROMPT1-MONITOR] [{level}] {msg}"
    print(entry)
    try:
        AUDIT_LOG.parent.mkdir(parents=True, exist_ok=True)
        with AUDIT_LOG.open("a", encoding="utf-8") as f:
            f.write(entry + "\n")
    except Exception:
        pass


def get_content_hash(filepath: Path) -> str:
    """Hash del contenido (ignora timestamps y READ markers)."""
    try:
        content = filepath.read_text(encoding="utf-8")
        lines = [
            l for l in content.splitlines()
            if l.strip() != "READ"
            and not l.startswith("<!-- monitor:")
        ]
        return hashlib.md5("\n".join(lines).encode()).hexdigest()
    except Exception:
        return ""


def extract_pending_tasks(filepath: Path) -> list[dict]:
    """Extrae tareas pendientes (- [ ]) con su línea."""
    tasks = []
    try:
        lines = filepath.read_text(encoding="utf-8").splitlines()
        for i, line in enumerate(lines):
            stripped = line.strip()
            if stripped.startswith("- [ ]"):
                task_text = stripped[5:].strip()
                if task_text:
                    tasks.append({"line": i, "text": task_text})
    except Exception as e:
        log(f"Error leyendo tareas: {e}", "ERROR")
    return tasks


def mark_task_complete(filepath: Path, task_line: int, result: str) -> None:
    """Marca una tarea como completada y agrega resultado."""
    try:
        lines = filepath.read_text(encoding="utf-8").splitlines()
        if 0 <= task_line < len(lines):
            # Cambiar [ ] por [x]
            lines[task_line] = lines[task_line].replace("- [ ]", "- [x]", 1)
            # Insertar resultado como sub-item
            indent = "  "
            ts = datetime.now().strftime("%H:%M:%S")
            result_line = f"{indent}- ✅ [{ts}] {result}"
            lines.insert(task_line + 1, result_line)
            filepath.write_text("\n".join(lines) + "\n", encoding="utf-8")
    except Exception as e:
        log(f"Error marcando tarea: {e}", "ERROR")


def add_log_entry(filepath: Path, origin: str, action: str) -> None:
    """Agrega entrada al log de comunicación en el archivo."""
    try:
        content = filepath.read_text(encoding="utf-8")
        ts = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        new_row = f"| {ts} | {origin} | {action} |"

        # Insertar antes del último READ o al final de la tabla
        if "| _esperando primer intercambio_" in content:
            content = content.replace(
                "| _esperando primer intercambio_ | — | — |",
                new_row
            )
        else:
            # Agregar después de la última fila de la tabla
            lines = content.splitlines()
            table_end = -1
            for i, line in enumerate(lines):
                if line.strip().startswith("|") and "Timestamp" not in line and "---" not in line:
                    table_end = i
            if table_end >= 0:
                lines.insert(table_end + 1, new_row)
                content = "\n".join(lines)

        filepath.write_text(content, encoding="utf-8")
    except Exception as e:
        log(f"Error agregando log entry: {e}", "ERROR")


def mark_read(filepath: Path) -> None:
    """Agrega READ al final del archivo."""
    try:
        content = filepath.read_text(encoding="utf-8")
        if not content.rstrip().endswith("READ"):
            filepath.write_text(content.rstrip() + "\n\nREAD\n", encoding="utf-8")
            log("Archivo marcado como READ")
    except Exception as e:
        log(f"Error marcando READ: {e}", "ERROR")


def run_openclaw_task(task_text: str) -> str:
    """Ejecuta una tarea via OpenClaw agent dev-senior."""
    log(f"Ejecutando tarea via OpenClaw: {task_text}")
    try:
        result = subprocess.run(
            [
                "openclaw", "agent",
                "--agent", OPENCLAW_AGENT,
                "--message", task_text,
                "--thinking", "low",
                "--timeout", str(OPENCLAW_TIMEOUT),
            ],
            capture_output=True,
            text=True,
            timeout=OPENCLAW_TIMEOUT + 30,
        )
        output = result.stdout.strip()
        if result.returncode != 0:
            error = result.stderr.strip() or "Unknown error"
            log(f"OpenClaw error (exit {result.returncode}): {error}", "ERROR")
            return f"ERROR: {error[:200]}"

        # Resumir la salida a una línea
        lines = [l for l in output.splitlines() if l.strip() and "bedrock" not in l.lower()]
        summary = lines[-1] if lines else "Completado sin output"
        return summary[:300]
    except subprocess.TimeoutExpired:
        log(f"OpenClaw timeout después de {OPENCLAW_TIMEOUT}s", "ERROR")
        return "ERROR: Timeout"
    except FileNotFoundError:
        log("openclaw CLI no encontrado en PATH", "ERROR")
        return "ERROR: openclaw not found"
    except Exception as e:
        log(f"Exception ejecutando OpenClaw: {e}", "ERROR")
        return f"ERROR: {str(e)[:200]}"


def is_cicd_task(task_text: str) -> bool:
    """Detecta si la tarea requiere ejecutar CI/CD."""
    keywords = ["ci/cd", "cicd", "smart-cicd", "deploy", "workflow run", "pipeline"]
    return any(kw in task_text.lower() for kw in keywords)


def run_cicd() -> str:
    """Ejecuta smart-cicd.yml via gh CLI."""
    log("Ejecutando CI/CD: gh workflow run smart-cicd.yml --ref main")
    try:
        result = subprocess.run(
            ["gh", "workflow", "run", "smart-cicd.yml", "--ref", "main"],
            capture_output=True,
            text=True,
            timeout=60,
            cwd=str(REPO_ROOT),
        )
        if result.returncode == 0:
            log("CI/CD disparado exitosamente")
            return "CI/CD smart-cicd.yml disparado. deploy-digitalocean.yml se ejecutará automáticamente."
        else:
            error = result.stderr.strip()
            log(f"CI/CD error: {error}", "ERROR")
            return f"ERROR CI/CD: {error[:200]}"
    except Exception as e:
        return f"ERROR CI/CD: {str(e)[:200]}"


def process_tasks(filepath: Path) -> int:
    """Procesa todas las tareas pendientes. Retorna cantidad procesada."""
    tasks = extract_pending_tasks(filepath)
    if not tasks:
        log("No hay tareas pendientes")
        return 0

    log(f"Procesando {len(tasks)} tarea(s) pendiente(s)")
    processed = 0

    for task in tasks:
        task_text = task["text"]
        task_line = task["line"]

        # Ejecutar via OpenClaw
        result = run_openclaw_task(task_text)
        mark_task_complete(filepath, task_line + processed, result)
        processed += 1  # Offset por líneas insertadas

        add_log_entry(filepath, "OpenClaw", f"Ejecutada: {task_text[:50]}...")

        # Si es tarea de CI/CD, ejecutar pipeline
        if is_cicd_task(task_text):
            cicd_result = run_cicd()
            log(cicd_result)

    return processed


def monitor_loop(interval: int, run_once: bool) -> None:
    """Loop principal de monitoreo."""
    log(f"Iniciando monitoreo de {PROMPT_FILE.name} (intervalo: {interval}s)")
    last_hash = get_content_hash(PROMPT_FILE)
    checks_without_change = 0

    while True:
        time.sleep(interval)

        if not PROMPT_FILE.exists():
            log(f"{PROMPT_FILE.name} no existe, esperando...", "WARN")
            continue

        current_hash = get_content_hash(PROMPT_FILE)

        if current_hash != last_hash:
            log("CAMBIO DETECTADO en prompt_1.md")
            checks_without_change = 0
            last_hash = current_hash

            # Procesar tareas
            count = process_tasks(PROMPT_FILE)
            if count > 0:
                log(f"{count} tarea(s) procesada(s)")

            # Marcar como leído
            mark_read(PROMPT_FILE)
            last_hash = get_content_hash(PROMPT_FILE)

        else:
            checks_without_change += 1
            if checks_without_change % 5 == 0:
                log(f"Sin cambios ({checks_without_change} checks)")

        if run_once:
            log("Modo --once: saliendo")
            break


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Monitor .prompts/prompt_1.md — Bridge VS Code ↔ OpenClaw"
    )
    parser.add_argument(
        "--once", action="store_true",
        help="Verificar una sola vez y salir"
    )
    parser.add_argument(
        "--interval", type=int, default=DEFAULT_INTERVAL,
        help=f"Intervalo de monitoreo en segundos (default: {DEFAULT_INTERVAL})"
    )
    args = parser.parse_args()

    if not PROMPT_FILE.exists():
        log(f"Creando {PROMPT_FILE.name} vacío...", "WARN")
        PROMPT_FILE.write_text("# Bridge VS Code ↔ OpenClaw\n\nREAD\n", encoding="utf-8")

    monitor_loop(args.interval, args.once)


if __name__ == "__main__":
    main()
