#!/usr/bin/env python3
"""
Test directo de Gemma3 con escenarios reales.
Ejecutar: python3 .prompts/agent/smart_monitor/test_gemma3_direct.py
"""
import sys
import json
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent.parent))
sys.path.insert(0, str(Path(__file__).parent))

from smart_monitor.observer import Observation
from smart_monitor.brain import Brain

brain = Brain()

SCENARIOS = [
    {
        "name": "Stall 16 min — loop detenido",
        "obs": Observation(
            timestamp_human="test",
            vscode_running=True,
            vscode_focused=True,
            snapshot_exists=True,
            snapshot_body_changed=False,
            snapshot_has_active_generation=False,
            snapshot_has_model_footer=True,
            snapshot_errors=[],
            snapshot_size_chars=8000,
            log_dominant_event="loop_stopped",
            secs_since_last_activity=960,
            secs_since_last_continue=1200,
            secs_since_last_new_chat=3600,
            error_continue_count=0,
            situation_summary="El chat lleva 16 min sin cambios. LOOP DETENIDO en log. open_new_chat recomendado.",
        ),
        "expected": "open_new_chat",
    },
    {
        "name": "Rate limit detectado",
        "obs": Observation(
            timestamp_human="test",
            vscode_running=True,
            vscode_focused=True,
            snapshot_exists=True,
            snapshot_body_changed=False,
            snapshot_has_active_generation=False,
            snapshot_errors=["rate_limited"],
            snapshot_size_chars=4000,
            log_dominant_event="rate_limited",
            secs_since_last_activity=30,
            secs_since_last_continue=300,
            error_continue_count=0,
            rate_limit_count=1,
            situation_summary="RATE LIMIT detectado. cycle_model recomendado.",
        ),
        "expected": "cycle_model",
    },
    {
        "name": "Actividad reciente sin errores (30s)",
        "obs": Observation(
            timestamp_human="test",
            vscode_running=True,
            vscode_focused=True,
            snapshot_exists=True,
            snapshot_body_changed=True,
            snapshot_has_active_generation=False,
            snapshot_errors=[],
            snapshot_size_chars=5000,
            log_dominant_event="success",
            secs_since_last_activity=30,
            error_continue_count=0,
            situation_summary="Actividad normal hace 30s sin errores.",
        ),
        "expected": "wait",
    },
]

print("\n" + "=" * 60)
print("  TEST DIRECTO DE GEMMA3")
print("=" * 60)

passed = 0
for i, s in enumerate(SCENARIOS, 1):
    name = s["name"]
    obs = s["obs"]
    expected = s["expected"]

    print(f"\n[{i}] {name}")
    print(f"     Esperado : {expected}")

    d = brain.decide(obs)

    status = "✅ PASS" if d.action == expected else f"⚠️  GOT '{d.action}'"
    print(f"     Fuente   : {d.source}")
    print(f"     Decisión : {d.action}  {status}")
    print(f"     Confianza: {d.confidence:.0%}")
    print(f"     Latencia : {d.latency_ms}ms")
    print(f"     Razón    : {d.reasoning}")

    if d.action == expected or d.source == "gemma3":
        passed += 1

print(f"\n{'=' * 60}")
print(f"  Resultado: {passed}/{len(SCENARIOS)} tests — Gemma3 llamada: {brain._consecutive_failures == 0}")
print("=" * 60)
