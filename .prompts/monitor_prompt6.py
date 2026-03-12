#!/usr/bin/env python3
"""
monitor_prompt6.py
Monitorea .prompts/prompt_6.md cada 30 segundos y agrega
un timestamp (fecha hora:minuto:segundo) debajo del contenido existente
sin alterar el texto original.
"""

import time
from datetime import datetime
from pathlib import Path

PROMPT_FILE = Path(__file__).parent / "prompt_6.md"
INTERVAL_SECONDS = 30


def update_timestamp(filepath: Path) -> None:
    """Reemplaza el único timestamp monitor del archivo; si no existe, lo agrega."""
    import re

    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    new_tag = f"<!-- monitor: {timestamp} -->"

    content = filepath.read_text(encoding="utf-8")

    # Elimina TODAS las líneas <!-- monitor: ... --> y el bloque compact existentes
    cleaned = re.sub(r"\nEjecutar el comando \"/compact\" al comienzo de cada auditoria|\n<!-- monitor: [\d\- :]+ -->", "", content)

    # Agrega primero el comando /compact y luego el timestamp
    updated = cleaned.rstrip() + f"\nEjecutar el comando \"/compact\" al comienzo de cada auditoria\n{new_tag}"

    filepath.write_text(updated, encoding="utf-8")
    print(f"[{timestamp}] Timestamp actualizado en: {filepath}")


def main() -> None:
    print(f"Iniciando monitoreo de: {PROMPT_FILE}")
    print(f"Intervalo: {INTERVAL_SECONDS} segundos. Ctrl+C para detener.\n")

    if not PROMPT_FILE.exists():
        print(f"ERROR: El archivo no existe: {PROMPT_FILE}")
        return

    while True:
        try:
            update_timestamp(PROMPT_FILE)
        except Exception as exc:
            print(f"[ERROR] {datetime.now().strftime('%Y-%m-%d %H:%M:%S')} — {exc}")
        time.sleep(INTERVAL_SECONDS)


if __name__ == "__main__":
    main()
