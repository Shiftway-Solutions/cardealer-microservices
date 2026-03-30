#!/usr/bin/env python3
"""
audit_actions.py — Auditoría completa del agente.
Prueba:
  1. Ollama / Gemma3 inference (brain.decide via real Observation)
  2. CDP disponibilidad y lectura de chat
  3. Cada executor de acción (sin ejecutarlos realmente — dry run para las acciones destructivas)
  4. vscode_focus (se ejecuta — solo trae VS Code al frente)
  5. is_cdp_available + read_chat_via_cdp
  6. Ciclo completo del agente (--once)
"""

import sys
import os
import json
import time
import urllib.request
from pathlib import Path

# Path setup
THIS = Path(__file__).resolve()
AGENT_DIR = THIS.parent.parent
SMART_MON = THIS.parent

if str(AGENT_DIR) not in sys.path:
    sys.path.insert(0, str(AGENT_DIR))

PASS = "✅"
FAIL = "❌"
WARN = "⚠️ "


def section(title):
    print(f"\n{'='*60}")
    print(f"  {title}")
    print('='*60)


# ─── 1. Ollama health ─────────────────────────────────────────────────────────
section("1. OLLAMA HEALTH + MODEL LIST")
try:
    with urllib.request.urlopen("http://localhost:11434/api/tags", timeout=5) as r:
        data = json.loads(r.read())
        models = [m["name"] for m in data.get("models", [])]
        print(f"  Models: {models}")
        has_gemma = any("gemma3" in m for m in models)
        if has_gemma:
            print(f"  {PASS} gemma3:4b found")
        else:
            print(f"  {FAIL} gemma3:4b NOT found — agent will fallback to wait")
except Exception as e:
    print(f"  {FAIL} Ollama not reachable: {e}")
    print("  Run: ollama serve &")


# ─── 2. Gemma3 full inference ─────────────────────────────────────────────────
section("2. GEMMA3 FULL INFERENCE TEST (Brain decision format)")
try:
    from smart_monitor.brain import Brain
    from smart_monitor.observer import Observation

    obs = Observation()
    obs.vscode_running = True
    obs.vscode_focused = True
    obs.snapshot_exists = True
    obs.snapshot_age_secs = 180.0
    obs.snapshot_has_active_generation = False
    obs.secs_since_last_activity = 180
    obs.secs_since_last_action = 600
    obs.log_dominant_event = "unknown"
    obs.cdp_context_full = False
    obs.cdp_available = False
    obs.situation_summary = "VS Code running, no errors, chat stalled 3min"

    brain = Brain()
    t0 = time.time()
    decision = brain.decide(obs)
    elapsed = int((time.time() - t0) * 1000)

    print(f"  Decision: {decision.action}")
    print(f"  Confidence: {decision.confidence:.2f}")
    print(f"  Reasoning: {decision.reasoning[:120]}")
    print(f"  Source: {decision.source}")
    print(f"  Latency: {elapsed}ms")

    if decision.source == "gemma3":
        print(f"  {PASS} Gemma3 responded and decision parsed correctly")
    elif decision.source == "override":
        print(f"  {WARN} Override triggered (expected for fast recent activity)")
    elif decision.source == "gemma3_offline":
        print(f"  {FAIL} Gemma3 offline — brain returned wait. Error: {decision.reasoning[:200]}")
    else:
        print(f"  {WARN} Source={decision.source}: {decision.reasoning[:100]}")
except Exception as e:
    import traceback
    print(f"  {FAIL} Brain import/run error: {e}")
    traceback.print_exc()


# ─── 3. CDP availability ──────────────────────────────────────────────────────
section("3. CDP AVAILABILITY (port 9222)")
try:
    from smart_monitor.actions import is_cdp_available, read_chat_via_cdp

    cdp_ok = is_cdp_available()
    if cdp_ok:
        print(f"  {PASS} CDP available on localhost:9222")
        print("  Attempting read_chat_via_cdp ...")
        text = read_chat_via_cdp()
        if text:
            print(f"  {PASS} read_chat_via_cdp returned {len(text)} chars")
            print(f"  Preview: {text[:200]!r}")
        else:
            print(f"  {WARN} read_chat_via_cdp returned None (chat may be empty or selectors miss)")
    else:
        print(f"  {WARN} CDP not available — VS Code must be launched with --remote-debugging-port=9222")
        print("  Agent will use AppleScript fallback for all UI actions.")
except Exception as e:
    print(f"  {FAIL} CDP check error: {e}")


# ─── 4. AppleScript / vscode_focus ───────────────────────────────────────────
section("4. APPLESCRIPT — vscode_focus()")
try:
    from smart_monitor.actions import vscode_focus, is_vscode_focused

    before = is_vscode_focused()
    vscode_focus()
    time.sleep(0.8)
    after = is_vscode_focused()
    print(f"  Before focus: {before}")
    print(f"  After focus:  {after}")
    if after:
        print(f"  {PASS} vscode_focus() works — VS Code is now frontmost")
    else:
        print(f"  {WARN} VS Code did not gain focus (may already be focused or window not open)")
except Exception as e:
    print(f"  {FAIL} vscode_focus error: {e}")


# ─── 5. Dry-run each action executor ─────────────────────────────────────────
section("5. ACTION EXECUTORS — Import + signature check (dry run)")
actions_to_check = [
    "vscode_send_continue",
    "vscode_open_new_chat",
    "send_loop_prompt",
    "open_new_chat_with_stop",
    "cycle_model_next",
    "stop_current_response",
    "notify",
    "capture_workbench_screenshot",
]
try:
    import smart_monitor.actions as act
    for name in actions_to_check:
        fn = getattr(act, name, None)
        if fn is None:
            print(f"  {FAIL} {name} — NOT FOUND in actions.py")
        else:
            print(f"  {PASS} {name} — importable, type={type(fn).__name__}")
except Exception as e:
    print(f"  {FAIL} actions import error: {e}")


# ─── 6. Model catalog + cycle_model_next ─────────────────────────────────────
section("6. MODEL CATALOG + cycle_model_next (dry run — no settings write)")
try:
    from smart_monitor.actions import get_model_pool, get_current_model

    pool = get_model_pool()
    current = get_current_model()
    print(f"  Model pool ({len(pool)}): {pool}")
    print(f"  Current model: {current!r}")

    if pool:
        print(f"  {PASS} Model pool loaded — cycle_model_next() would cycle through {len(pool)} models")
    else:
        print(f"  {WARN} Model pool empty — check .prompts/agent/model_catalog.json")
except Exception as e:
    print(f"  {FAIL} model pool error: {e}")


# ─── 7. LOOP PROMPT file ─────────────────────────────────────────────────────
section("7. LOOP PROMPT FILE (.prompts/AGENT_LOOP_PROMPT.md)")
try:
    from smart_monitor.actions import LOOP_PROMPT_FILE
    if LOOP_PROMPT_FILE.exists():
        size = LOOP_PROMPT_FILE.stat().st_size
        print(f"  {PASS} Found: {LOOP_PROMPT_FILE} ({size} bytes)")
    else:
        print(f"  {FAIL} NOT FOUND: {LOOP_PROMPT_FILE}")
        print("  send_loop_prompt() will use fallback text 'continuar con las tareas pendientes del sprint actual'")
except Exception as e:
    print(f"  {FAIL} error: {e}")


# ─── 8. Observer full cycle ───────────────────────────────────────────────────
section("8. OBSERVER — full observation cycle")
try:
    from smart_monitor.observer import Observer

    observer = Observer(state={})
    obs = observer.observe()

    print(f"  vscode_running:              {obs.vscode_running}")
    print(f"  vscode_focused:              {obs.vscode_focused}")
    print(f"  snapshot_exists:             {obs.snapshot_exists}")
    print(f"  snapshot_age_secs:           {obs.snapshot_age_secs:.1f}")
    print(f"  snapshot_has_active_gen:     {obs.snapshot_has_active_generation}")
    print(f"  secs_since_last_activity:    {obs.secs_since_last_activity:.0f}")
    print(f"  cdp_available:               {obs.cdp_available}")
    print(f"  cdp_context_full:            {obs.cdp_context_full}")
    print(f"  log_dominant_event:          {obs.log_dominant_event!r}")
    print(f"  snapshot_errors:             {obs.snapshot_errors}")
    print(f"  situation_summary:           {obs.situation_summary[:100]}")
    print(f"  {PASS} Observer.observe() completed without error")
except Exception as e:
    import traceback
    print(f"  {FAIL} Observer error: {e}")
    traceback.print_exc()


# ─── Summary ─────────────────────────────────────────────────────────────────
section("AUDIT COMPLETE")
print("  Review ✅/❌/⚠️  above for each component.")
print("  To start agent: python3 -m smart_monitor.agent --once --debug")
print("  Loop mode:      python3 -m smart_monitor.agent --interval 20 --debug")
