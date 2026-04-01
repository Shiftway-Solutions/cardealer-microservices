#!/usr/bin/env python3
"""
test_actions_audit.py — Auditoría de acciones del Smart Monitor
================================================================
Verifica que stop_current_response, open_new_chat_with_stop,
vscode_open_new_chat, y _snapshot_is_fresh se comporten
correctamente post RC5-fix (CDP ciego).

Ejecutar:
    python3 .prompts/agent/smart_monitor/test_actions_audit.py
"""
import os
import sys
import time
import traceback
from pathlib import Path

# Run as: python3 .prompts/agent/smart_monitor/test_actions_audit.py
# from the repo root (cardealer-microservices/).
_REPO_ROOT = Path(__file__).resolve().parent.parent.parent.parent
_AGENT_DIR  = Path(__file__).resolve().parent.parent  # .prompts/agent/
sys.path.insert(0, str(_REPO_ROOT))

# Direct import — smart_monitor is NOT a package, import file directly
import importlib.util as _ilu

_spec = _ilu.spec_from_file_location(
    "actions",
    str(Path(__file__).resolve().parent / "actions.py"),
)
assert _spec is not None and _spec.loader is not None
_mod = _ilu.module_from_spec(_spec)
_spec.loader.exec_module(_mod)  # type: ignore[union-attr]

_snapshot_is_fresh   = _mod._snapshot_is_fresh
is_cdp_available     = _mod.is_cdp_available
CHAT_SNAPSHOT_FILE   = _mod.CHAT_SNAPSHOT_FILE
stop_current_response    = _mod.stop_current_response
vscode_open_new_chat     = _mod.vscode_open_new_chat
open_new_chat_with_stop  = _mod.open_new_chat_with_stop
vscode_send_continue     = _mod.vscode_send_continue
ensure_vscode_focused    = _mod.ensure_vscode_focused
vscode_exec_command      = _mod.vscode_exec_command
VSCODE_CMD_CHAT_STOP     = _mod.VSCODE_CMD_CHAT_STOP
VSCODE_CMD_CHAT_NEW      = _mod.VSCODE_CMD_CHAT_NEW

PASS = "✅ PASS"
FAIL = "❌ FAIL"
SKIP = "⚠️  SKIP"

results = []


def check(name: str, condition: bool, detail: str = "") -> None:
    status = PASS if condition else FAIL
    msg = f"  {status}  {name}"
    if detail:
        msg += f"\n         → {detail}"
    print(msg)
    results.append((name, condition))


def section(title: str) -> None:
    print(f"\n{'─' * 60}")
    print(f"  {title}")
    print('─' * 60)


def backdate_snapshot(secs: float = 30.0) -> None:
    """Backdate the snapshot file's mtime to `secs` seconds ago using os.utime."""
    if CHAT_SNAPSHOT_FILE.exists():
        past = time.time() - secs
        os.utime(str(CHAT_SNAPSHOT_FILE), (past, past))


# ─── 0. Background agent check ───────────────────────────────────────────────

section("Background-agent check")

import subprocess as _sp
_bg = _sp.run(
    ["pgrep", "-f", "smart_monitor/agent.py"],
    capture_output=True, text=True,
)
bg_pids = _bg.stdout.strip().split()
bg_running = bool(bg_pids)
check(
    "No hay agente background corriendo con código pre-RC5",
    not bg_running,
    (f"PID(s) corriendo: {', '.join(bg_pids)} — "
     "el agente usa código viejo en memoria; reiniciarlo para que RC5 tome efecto.")
    if bg_running else "OK — no hay proceso previo",
)
if bg_running:
    print(f"  ⚠️  ADVERTENCIA: el agente PID {bg_pids[0]} aún corre con código pre-RC5.")
    print("     Reiniciarlo para que RC5 tome efecto en producción.")
    print("     Los tests de snapshot usarán os.utime para backdatar el mtime.")


# ─── 1. RC5: _snapshot_is_fresh con CDP ciego ──────────────────────────────

section("RC5 — _snapshot_is_fresh con CDP ciego")

cdp_up = is_cdp_available()
print(f"  CDP disponible: {'SÍ' if cdp_up else 'NO (esperado)'}")

if not cdp_up:
    # Backdate snapshot to 30s ago so it's reliably stale (bypasses background-agent interference)
    try:
        backdate_snapshot(30.0)
        age = time.time() - CHAT_SNAPSHOT_FILE.stat().st_mtime
        is_fresh = _snapshot_is_fresh(3.0)
        check(
            "Snapshot NO fresh con CDP ciego (mtime backdateado 30s)",
            not is_fresh,
            f"age={age:.1f}s → _snapshot_is_fresh(3.0)={is_fresh} (esperado: False)",
        )
    except Exception as e:
        check("_snapshot_is_fresh legible", False, str(e))

    # touch → verify correctly returns True
    try:
        CHAT_SNAPSHOT_FILE.touch()
        is_fresh_now = _snapshot_is_fresh(3.0)
        check(
            "Snapshot SÍ fresh si mtime=now (lógica correcta)",
            is_fresh_now,
            f"_snapshot_is_fresh(3.0)={is_fresh_now} (esperado: True)",
        )
        # Restore + backdate so rest of tests work
        CHAT_SNAPSHOT_FILE.write_text(
            "# Chat Snapshot\n\n[CDP no disponible — puerto 9222 no está abierto]\n",
            encoding="utf-8",
        )
        backdate_snapshot(30.0)
        is_fresh_after = _snapshot_is_fresh(3.0)
        check(
            "Snapshot NO fresh después de restaurar+backdatar (simula RC5)",
            not is_fresh_after,
            f"_snapshot_is_fresh(3.0)={is_fresh_after} (esperado: False — RC5 debe mantener mtime estable)",
        )
    except Exception as e:
        check("Simulación mtime test", False, str(e))
else:
    print(f"  CDP está disponible — RC5 test no aplica (solo sin CDP)")


# ─── 2. Auditoría: vscode_exec_command (base de todas las acciones) ──────────

section("URL Protocol — vscode_exec_command")

try:
    # Abrir el chat (no destructivo)
    ok_open = vscode_exec_command("workbench.action.chat.open")
    check("vscode://command/workbench.action.chat.open", ok_open,
          f"subprocess exit_code=0 → {ok_open}")
except Exception as e:
    check("vscode_exec_command", False, str(e))


# ─── 3. Auditoría: ensure_vscode_focused ────────────────────────────────────

section("RC2/RC3/RC4 — ensure_vscode_focused()")

try:
    focused = ensure_vscode_focused()
    check("VS Code se puede traer al frente", focused,
          f"AppleScript activate → frontmost={focused}")
except Exception as e:
    check("ensure_vscode_focused", False, str(e))


# ─── 4. Auditoría: stop_current_response ────────────────────────────────────

section("Cmd+ESC — stop_current_response() [DRY: solo verifica que no lanza excepción]")

print("  NOTA: Esta acción enviará Cmd+ESC a VS Code. Asegúrate de que no haya")
print("  generación activa o acepta que el modelo será detenido.")
print()

try:
    # Solo disparamos el URL command (no el fallback AppleScript) para no interrumpir
    ok_stop_url = vscode_exec_command(VSCODE_CMD_CHAT_STOP)
    check(
        "vscode://command/workbench.action.chat.stop (URL)",
        ok_stop_url,
        f"URL command dispatched → {ok_stop_url}",
    )
    # Verificar también que la función completa no lanza excepciones
    # La ejecutamos pero no podemos verificar el efecto sin CDP
    ensure_vscode_focused()
    ok_full = stop_current_response()
    check(
        "stop_current_response() completo (focus→URL→Cmd+ESC fallback)",
        ok_full,
        f"Returns True = comando dispatched → {ok_full}",
    )
except Exception as e:
    check("stop_current_response", False, traceback.format_exc()[:300])


# ─── 5. Auditoría: vscode_open_new_chat ─────────────────────────────────────

section("vscode_open_new_chat() — sin snapshot fresh")

# Backdate snapshot so _snapshot_is_fresh(3.0) returns False regardless of bg agent
try:
    original_content = (
        CHAT_SNAPSHOT_FILE.read_text(encoding="utf-8")
        if CHAT_SNAPSHOT_FILE.exists() else ""
    )
    CHAT_SNAPSHOT_FILE.write_text(
        "# Chat Snapshot (test)\n\n[CDP no disponible]\n",
        encoding="utf-8",
    )
    backdate_snapshot(30.0)  # set mtime to 30s ago — reliably stale

    is_fresh_pre = _snapshot_is_fresh(3.0)
    check("Snapshot stale antes del test (backdateado 30s)", not is_fresh_pre,
          f"_snapshot_is_fresh(3.0)={is_fresh_pre}")

    ok_new = vscode_open_new_chat()
    check(
        "vscode_open_new_chat() con snapshot stale",
        ok_new,
        f"Returns True = URL dispatched → {ok_new}",
    )
except Exception as e:
    check("vscode_open_new_chat", False, traceback.format_exc()[:300])


# ─── 6. Auditoría: vscode_send_continue ─────────────────────────────────────

section("vscode_send_continue() — tier 1: code chat CLI")

try:
    # Backdate snapshot again since vscode_open_new_chat may have updated it
    backdate_snapshot(30.0)
    is_fresh_pre = _snapshot_is_fresh(3.0)
    check("Snapshot stale para send_continue (backdateado)", not is_fresh_pre,
          f"_snapshot_is_fresh(3.0)={is_fresh_pre}")

    ok_continue = vscode_send_continue()
    check(
        "vscode_send_continue() — 'continuar' via code chat CLI",
        ok_continue,
        f"Returns True = enviado → {ok_continue}",
    )
except Exception as e:
    check("vscode_send_continue", False, traceback.format_exc()[:300])


# ─── Resumen final ───────────────────────────────────────────────────────────

section("RESUMEN")
passed = sum(1 for _, ok in results if ok)
failed = sum(1 for _, ok in results if not ok)
total = len(results)
print(f"  Total: {total}   ✅ {passed}   ❌ {failed}")
print()
if failed > 0:
    print("  Fallidos:")
    for name, ok in results:
        if not ok:
            print(f"    ❌ {name}")
sys.exit(0 if failed == 0 else 1)
