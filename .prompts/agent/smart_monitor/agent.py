#!/usr/bin/env python3
"""
agent.py — Entry Point del Smart Monitor con Gemma 3
=====================================================
Orquesta el ciclo completo: Observar → Pensar → Actuar → Aprender.

Sin dependencia en vscode_copilot_monitor.py (v7).
Todas las acciones sobre VS Code están en smart_monitor/actions.py.

Uso:
  python3 -m smart_monitor.agent                     # loop continuo
  python3 -m smart_monitor.agent --once              # un ciclo
  python3 -m smart_monitor.agent --status            # estado actual
  python3 -m smart_monitor.agent --interval 30       # poll cada 30s
  python3 -m smart_monitor.agent --debug             # verbose
  python3 -m smart_monitor.agent --dry-run           # no ejecutar acciones
"""

import argparse
import hashlib
import json
import logging
import os
import re
import signal
import sys
import time
from datetime import datetime
from dataclasses import asdict
from pathlib import Path

# ─── Path setup ───────────────────────────────────────────────────────────────
REPO_ROOT = Path(__file__).parent.parent.parent.parent
AGENT_DIR = Path(__file__).parent.parent

# Ensure the package root is on the path for the smart_monitor package
if str(AGENT_DIR) not in sys.path:
    sys.path.insert(0, str(AGENT_DIR))

# ─── Smart monitor modules ────────────────────────────────────────────────────
from smart_monitor.observer import Observer, Observation
from smart_monitor.brain import Brain, Decision
from smart_monitor.memory import Memory, seed_initial_lessons
from smart_monitor.feedback import FeedbackLoop

# ─── Action executors (standalone — no v7 dependency) ─────────────────────────
from smart_monitor.actions import (
    CHAT_SNAPSHOT_FILE,
    is_cdp_available,
    read_chat_via_cdp,
    classify_chat_text,
    _text_hash,
    capture_workbench_screenshot,
    notify,
    vscode_focus,
    is_vscode_focused,
    vscode_send_continue,
    vscode_open_new_chat,
    send_loop_prompt,
    open_new_chat_with_stop,
    cycle_model_next,
)

# ─── Constants ────────────────────────────────────────────────────────────────
DEFAULT_INTERVAL  = 20   # poll every 20s
STATE_FILE        = AGENT_DIR / "smart_monitor" / ".agent_state.json"
PID_FILE          = AGENT_DIR / "smart_monitor" / ".agent_pid"
LOG_FILE          = REPO_ROOT / ".github" / "smart-monitor.log"
AUDIT_LOG_FILE    = AGENT_DIR / "smart_monitor" / "audit_log.jsonl"

# Minimum seconds between sending the same action type (anti-spam)
ACTION_COOLDOWNS = {
    "send_continue":     60,
    "open_new_chat":     90,
    "stop_and_new_chat": 90,
    "cycle_model":      120,
    "focus_vscode":      30,
}

# ─── Logging ──────────────────────────────────────────────────────────────────
logger = logging.getLogger("smart_monitor")


def setup_logging(debug: bool = False) -> None:
    level = logging.DEBUG if debug else logging.INFO
    fmt = "[%(asctime)s] [%(levelname)s] %(message)s"
    datefmt = "%Y-%m-%d %H:%M:%S"

    # Console
    handler_console = logging.StreamHandler(sys.stdout)
    handler_console.setFormatter(logging.Formatter(fmt, datefmt=datefmt))

    # File
    LOG_FILE.parent.mkdir(parents=True, exist_ok=True)
    handler_file = logging.FileHandler(str(LOG_FILE), encoding="utf-8")
    handler_file.setFormatter(logging.Formatter(fmt, datefmt=datefmt))

    root = logging.getLogger("smart_monitor")
    root.setLevel(level)
    root.addHandler(handler_console)
    root.addHandler(handler_file)


# ─── State persistence ────────────────────────────────────────────────────────
def load_state() -> dict:
    try:
        return json.loads(STATE_FILE.read_text(encoding="utf-8"))
    except Exception:
        return {}


def save_state(state: dict) -> None:
    STATE_FILE.parent.mkdir(parents=True, exist_ok=True)
    STATE_FILE.write_text(json.dumps(state, indent=2), encoding="utf-8")


def default_state() -> dict:
    now = time.time()
    return {
        "last_activity_ts":       now,
        "last_action_ts":         0.0,
        "last_action_type":       "",
        "last_continue_ts":       0.0,
        "last_new_chat_ts":       0.0,
        "last_rate_limit_ts":     0.0,
        "error_continue_count":   0,
        "rate_limit_count":       0,
        "new_chat_count":         0,
        "continue_count":         0,
        "log_path":               "",
        "log_offset":             0,
        "chat_hash":              "",
        "chat_len":               0,
        "last_snapshot_body_hash": "",
        "last_snapshot_mtime":    0.0,
        "post_action_ts":         0.0,
        "post_action_type":       "",
        "post_action_retry_count": 0,
        "model_completion_seen_ts": 0.0,
        # Smart monitor specific
        "cycle_count":            0,
        "gemma_decisions":        0,
        "fallback_decisions":     0,
        "override_decisions":     0,
        "total_actions":          0,
        # Fix 1: CDP consecutive timeouts counter
        "cdp_consecutive_timeouts": 0,
        # Fix 3: Context proxy — accumulated diff bytes
        "context_proxy_bytes":    0,
        "context_proxy_last_hash": "",
    }


def refresh_live_chat_state(state: dict) -> None:
    """Refresh chat_len and chat_snapshot.txt directly from the live CDP session.
    
    Fix 1: Track consecutive CDP timeouts — signals VS Code under stress.
    Fix 3: Accumulate snapshot diff bytes as a proxy for total context size.
    Fix 5: Log chat_len and proxy_bytes in every cycle for diagnostics.
    """
    if not is_cdp_available():
        state["chat_len"] = 0
        # CDP endpoint unavailable (not a playwright timeout)
        return

    chat_text = read_chat_via_cdp()
    if not chat_text:
        # Fix 1: Playwright failed — could be a timeout from VS Code stress
        state["cdp_consecutive_timeouts"] = state.get("cdp_consecutive_timeouts", 0) + 1
        state["chat_len"] = 0
        logger.debug(
            f"CDP read failed — consecutive_timeouts={state['cdp_consecutive_timeouts']}"
        )
        return

    # CDP success — reset consecutive timeout counter
    state["cdp_consecutive_timeouts"] = 0

    previous_hash = state.get("chat_hash", "")
    _, has_changed = classify_chat_text(chat_text, previous_hash)

    state["chat_hash"] = _text_hash(chat_text)
    state["chat_len"] = len(chat_text)

    # Fix 3: Accumulate context proxy bytes from snapshot diffs
    if has_changed:
        proxy_prev_hash = state.get("context_proxy_last_hash", "")
        if proxy_prev_hash != state["chat_hash"]:
            # Fix 3: High-water mark instead of cumulative sum.
            # Old code did +=len(chat_text) on every diff → 9 diffs × 50k = 450k > threshold
            # even though the real context is only ~50k chars.
            # Using max() means we only trigger when the ACTUAL chat window is >400k chars.
            state["context_proxy_bytes"] = max(state.get("context_proxy_bytes", 0), len(chat_text))
            state["context_proxy_last_hash"] = state["chat_hash"]

    # Fix 5: Log chat_len and proxy every cycle for diagnostics
    logger.debug(
        f"CDP chat_len={state['chat_len']:,} chars | "
        f"context_proxy={state.get('context_proxy_bytes', 0):,} bytes | "
        f"cdp_timeouts={state.get('cdp_consecutive_timeouts', 0)}"
    )

    should_write_snapshot = has_changed
    if not should_write_snapshot:
        try:
            should_write_snapshot = (
                not CHAT_SNAPSHOT_FILE.exists()
                or CHAT_SNAPSHOT_FILE.stat().st_size == 0
            )
        except Exception:
            should_write_snapshot = True

    if should_write_snapshot:
        try:
            ts = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            CHAT_SNAPSHOT_FILE.write_text(
                f"# Chat Snapshot -- {ts}\n\n{chat_text}\n",
                encoding="utf-8",
            )
        except Exception as exc:
            logger.debug(f"Failed to refresh chat snapshot from CDP: {exc}")


# ─── Audit Record ────────────────────────────────────────────────────────────

def _write_audit_record(
    action: str,
    decision: Decision,
    ok: bool,
    before_png: str,
    after_png: str,
    cycle: int,
) -> None:
    """Append one JSON-line audit record for every real action executed."""
    record = {
        "ts":         datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
        "cycle":      cycle,
        "action":     action,
        "source":     decision.source,
        "reasoning":  decision.reasoning,
        "confidence": round(decision.confidence, 2),
        "ok":         ok,
        "before_png": before_png,
        "after_png":  after_png,
    }
    try:
        with AUDIT_LOG_FILE.open("a", encoding="utf-8") as fh:
            fh.write(json.dumps(record, ensure_ascii=False) + "\n")
    except Exception as exc:
        logger.warning(f"Audit log write failed: {exc}")


# ─── Action Executor ──────────────────────────────────────────────────────────

class ActionExecutor:
    """
    Translates Brain decisions into real VS Code actions.
    Reuses the proven executors from vscode_copilot_monitor.py (v7).
    """

    def __init__(self, state: dict, dry_run: bool = False):
        self._state = state
        self._dry_run = dry_run

    def execute(self, decision: Decision) -> bool:
        """
        Execute a decision. Returns True if the action was performed.
        Respects cooldowns to avoid spamming VS Code.
        """
        action = decision.action
        now = time.time()

        if action == "wait":
            return True  # nothing to do

        # Respect wait_before_action_secs from the brain
        # BUG FIX: compare against last_{action}_ts (same action), NOT last_action_ts
        # (any action). A recent send_continue must not block a cycle_model, and vice versa.
        if decision.wait_before_action_secs > 0:
            action_specific_ts = self._state.get(f"last_{action}_ts", 0.0)
            elapsed_since_same = now - action_specific_ts
            if elapsed_since_same < decision.wait_before_action_secs:
                logger.info(
                    f"Brain requested {decision.wait_before_action_secs}s wait before {action} "
                    f"({elapsed_since_same:.0f}s elapsed since last {action})"
                )
                logger.debug("Skipping action — wait timer not expired yet")
                return False
            # Enough time passed since last same action — proceed (skip logging the wait)


        # Cooldown check
        cooldown = ACTION_COOLDOWNS.get(action, 30)
        last_same = self._state.get(f"last_{action}_ts", 0.0)
        elapsed = now - last_same
        if elapsed < cooldown:
            logger.info(
                f"Action {action} on cooldown ({cooldown - elapsed:.0f}s remaining)"
            )
            return False

        if self._dry_run:
            logger.info(f"[DRY-RUN] Would execute: {action} ({decision.reasoning})")
            return True

        # ── Audit: screenshot BEFORE action ──────────────────────────────────
        before_extra = {
            "phase":      "before",
            "action":     action,
            "reasoning":  decision.reasoning,
            "confidence": round(decision.confidence, 2),
            "source":     decision.source,
            "cycle":      self._state.get("cycle_count", 0),
        }
        before_png = capture_workbench_screenshot(f"{action}_before", before_extra)

        logger.info(
            f"EXECUTING: {action} (confidence={decision.confidence:.0%}, "
            f"source={decision.source}) — {decision.reasoning}"
        )

        ok = False
        try:
            if action == "send_continue":
                ok = self._send_continue()
            elif action == "open_new_chat":
                ok = self._open_new_chat()
            elif action == "stop_and_new_chat":
                ok = self._stop_and_new_chat()
            elif action == "cycle_model":
                ok = self._cycle_model()
            elif action == "focus_vscode":
                ok = self._focus_vscode()
            else:
                logger.warning(f"Unknown action: {action}")
                ok = False
        except Exception as e:
            logger.error(f"Action {action} failed: {e}")
            ok = False

        # ── Audit: screenshot AFTER action + JSONL record ─────────────────────
        after_extra = {
            "phase":     "after",
            "action":    action,
            "ok":        ok,
            "reasoning": decision.reasoning,
            "source":    decision.source,
            "cycle":     self._state.get("cycle_count", 0),
        }
        after_png = capture_workbench_screenshot(f"{action}_after", after_extra)
        _write_audit_record(
            action=action,
            decision=decision,
            ok=ok,
            before_png=before_png,
            after_png=after_png,
            cycle=self._state.get("cycle_count", 0),
        )

        # Fix 1: Always stamp the per-action cooldown timestamp, even on failure.
        # Without this, a failed stop_and_new_chat never updates last_stop_and_new_chat_ts
        # → cooldown never blocks → agent retries every 20 s for minutes → spam + duplicate chats.
        self._state[f"last_{action}_ts"] = now

        if ok:
            self._state["last_action_ts"] = now
            self._state["last_action_type"] = action
            self._state["total_actions"] = self._state.get("total_actions", 0) + 1

            # Update v7-compatible counters
            if action == "send_continue":
                self._state["last_continue_ts"] = now
                self._state["continue_count"] = self._state.get("continue_count", 0) + 1
                self._state["error_continue_count"] = self._state.get("error_continue_count", 0) + 1
            elif action in ("open_new_chat", "stop_and_new_chat"):
                self._state["last_new_chat_ts"] = now
                self._state["new_chat_count"] = self._state.get("new_chat_count", 0) + 1
                self._state["error_continue_count"] = 0
                # Fix 3: Reset context proxy counters on new chat (fresh session)
                self._state["context_proxy_bytes"] = 0
                self._state["context_proxy_last_hash"] = ""
                self._state["cdp_consecutive_timeouts"] = 0
            elif action == "cycle_model":
                self._state["last_rate_limit_ts"] = now
                self._state["rate_limit_count"] = self._state.get("rate_limit_count", 0) + 1

            # Set post-action verification
            self._state["post_action_ts"] = now
            self._state["post_action_type"] = action
            self._state["post_action_retry_count"] = 0

            notify("OKLA Smart Monitor", f"{action}: {decision.reasoning[:60]}")

        return ok

    # ─── Individual action implementations ────────────────────────────────────

    def _send_continue(self) -> bool:
        return vscode_send_continue()

    def _open_new_chat(self) -> bool:
        ok = vscode_open_new_chat()
        if ok:
            time.sleep(1.0)
            send_loop_prompt()
        return ok

    def _stop_and_new_chat(self) -> bool:
        return open_new_chat_with_stop()

    def _cycle_model(self) -> bool:
        new = cycle_model_next(self._state)
        if not new:
            logger.warning("_cycle_model: could not determine new model (empty pool?)")
            return False
        logger.info(f"Model cycled → {new}")
        # Wait for VS Code to apply the settings change, then send continue
        time.sleep(2.0)
        vscode_send_continue()
        self._state["last_continue_ts"] = time.time()
        self._state["continue_count"] = self._state.get("continue_count", 0) + 1
        return True

    def _focus_vscode(self) -> bool:
        vscode_focus()
        return is_vscode_focused()


# ─── Post-Action Verification ────────────────────────────────────────────────

def handle_post_action(state: dict, obs: Observation, feedback: FeedbackLoop) -> bool:
    """
    If a post-action verification is active, check if normalized.
    Returns True if we should skip the normal decision cycle.
    """
    post_ts = state.get("post_action_ts", 0.0)
    if post_ts == 0.0:
        return False

    action_type = state.get("post_action_type", "")
    retry_count = state.get("post_action_retry_count", 0)
    now = time.time()
    elapsed = now - post_ts

    # Check feedback loop for normalization
    obs_dict = obs.to_dict()
    outcome = feedback.check_outcome(obs_dict)

    if outcome == "resolved":
        logger.info(
            f"POST-ACTION ({action_type}): normalized in {elapsed:.0f}s — back to monitoring"
        )
        state["post_action_ts"] = 0.0
        state["post_action_type"] = ""
        state["post_action_retry_count"] = 0
        state["error_continue_count"] = 0
        return False  # continue normal cycle

    if elapsed < 120:
        remaining = 120 - elapsed
        logger.info(
            f"POST-ACTION ({action_type}): waiting for normalization "
            f"({elapsed:.0f}s / 120s, {remaining:.0f}s left)"
        )
        return True  # skip normal cycle, keep waiting

    # Timeout — 2 minutes with no normalization
    if retry_count == 0:
        # First retry: resend same command
        logger.warning(
            f"POST-ACTION ({action_type}): no normalization after {elapsed:.0f}s — retrying"
        )
        feedback.force_close_tracking(outcome="no_effect", notes="Retrying same action")
        state["post_action_ts"] = now
        state["post_action_retry_count"] = 1
        return False  # let the brain decide again (it will likely pick the same action)

    # Retry also failed — escalate
    logger.warning(
        f"POST-ACTION ({action_type}): retry also failed — escalating to new chat"
    )
    feedback.force_close_tracking(outcome="escalated", notes="Escalated after retry timeout")
    state["post_action_ts"] = 0.0
    state["post_action_type"] = ""
    state["post_action_retry_count"] = 0
    # Don't return True — let brain decide with fresh observation (likely open_new_chat)
    return False


# ─── Verbose Terminal Output ──────────────────────────────────────────────────

def _print_cycle_summary(state: dict, obs, decision) -> None:
    """Imprime un resumen legible del ciclo para que el usuario vea el análisis."""
    cycle = state.get("cycle_count", 0)
    now_str = datetime.now().strftime("%H:%M:%S")

    # Separador visual
    print(f"\n{'━' * 70}")
    brain_label = {
        "gemma3":         "🤖 GEMMA 3",
        "override":       "⚡ OVERRIDE",
        "gemma3_offline": "💀 OFFLINE",
    }.get(decision.source, decision.source.upper())
    print(f"  🧠 CICLO #{cycle}  |  {now_str}  |  CEREBRO: {brain_label}")
    print(f"{'━' * 70}")

    # Observación resumida
    print(f"  📊 OBSERVACIÓN:")
    print(f"     Snapshot: {'existe' if obs.snapshot_exists else 'NO'}"
          f" | body cambió: {'SÍ' if obs.snapshot_body_changed else 'NO'}"
          f" | generación activa: {'SÍ' if obs.snapshot_has_active_generation else 'NO'}")
    print(f"     Errores chat: {', '.join(obs.snapshot_errors) if obs.snapshot_errors else 'ninguno'}")
    print(f"     Log dominante: {obs.log_dominant_event}"
          f" | nuevas líneas: {obs.log_new_lines_count}")
    print(f"     VS Code: {'corriendo' if obs.vscode_running else 'DETENIDO'}"
          f" | foco: {'SÍ' if obs.vscode_focused else 'NO'}")
    print(f"     Última actividad: hace {obs.secs_since_last_activity:.0f}s"
          f" ({obs.secs_since_last_activity/60:.1f} min)")
    # Fix 5: Always show context size signals
    proxy_kb = obs.cdp_context_proxy_bytes // 1024
    print(f"     [CTX] chat_len={obs.cdp_chat_text_length:,} chars"
          f" | proxy={proxy_kb:,} KB"
          f" | cdp_timeouts={obs.cdp_consecutive_timeouts}"
          f" | sesión={obs.session_age_mins:.0f} min/{obs.continue_count} continues"
          f" | {'🔴 CONTEXTO PELIGROSO' if obs.cdp_context_proxy_full or obs.cdp_context_full or obs.cdp_consecutive_timeouts >= 3 else 'OK'}")

    # Situation summary (lo que Gemma3 recibe como contexto)
    if obs.situation_summary and decision.source == "gemma3":
        print(f"\n  🔍 CONTEXTO ENVIADO A GEMMA 3:")
        for line in obs.situation_summary.split("."):
            line = line.strip()
            if line:
                print(f"     {line}.")

    # Decisión
    action_emoji = {
        "wait": "⏸️",
        "send_continue": "▶️",
        "open_new_chat": "🆕",
        "stop_and_new_chat": "⏹️🆕",
        "cycle_model": "🔄",
        "focus_vscode": "🖥️",
    }.get(decision.action, "❓")

    print(f"\n  🎯 DECISIÓN: {action_emoji} {decision.action.upper()}"
          f"  (confianza: {decision.confidence:.0%})")
    print(f"     Razonamiento: {decision.reasoning}")

    if decision.source == "gemma3" and decision.latency_ms > 0:
        print(f"     Latencia Gemma 3: {decision.latency_ms}ms")
    if decision.raw_response and decision.source == "gemma3":
        print(f"     Respuesta raw: {decision.raw_response[:200]}")

    if decision.source == "gemma3_offline":
        print(f"\n  ⛔ GEMMA 3 NO DISPONIBLE — Ejecuta: ollama serve")

    # Stats acumulados
    print(f"\n  📈 STATS: gemma={state.get('gemma_decisions', 0)}"
          f" | override={state.get('override_decisions', 0)}"
          f" | offline={state.get('fallback_decisions', 0)}"
          f" | acciones={state.get('total_actions', 0)}")
    print(f"{'━' * 70}")


# ─── Main Cycle ───────────────────────────────────────────────────────────────

def run_cycle(
    state: dict,
    memory: Memory,
    brain: Brain,
    feedback: FeedbackLoop,
    executor: ActionExecutor,
    dry_run: bool = False,
) -> dict:
    """
    Single observe → think → act → learn cycle.
    """
    now = time.time()
    state["cycle_count"] = state.get("cycle_count", 0) + 1
    previous_log_path = state.get("log_path", "")

    # ═══ OBSERVE ═══
    refresh_live_chat_state(state)
    observer = Observer(state)
    obs = observer.observe()

    # Update state with observer's new offsets
    offsets = observer.get_updated_offsets()
    state.update(offsets)

    # Update activity tracking from snapshot
    # REGLA: solo considerar como "actividad" cambios REALES en el contenido
    # del chat o nuevos success events en el log. Los log lines genéricos
    # (incluidos errores de request) NO deben resetear el timer de actividad
    # porque generan falsos positivos que ocultan stalls reales.
    if obs.snapshot_body_changed:
        state["last_activity_ts"] = now
        state["last_snapshot_body_hash"] = obs.snapshot_body_hash
        state["error_continue_count"] = 0
    elif obs.log_events.get("success", 0) > 0:
        # Solo success events cuentan como actividad desde logs
        state["last_activity_ts"] = now
    elif offsets.get("log_path") and offsets.get("log_path") != previous_log_path:
        # Cambio de archivo de log = nueva sesión → resetear
        state["last_activity_ts"] = now
    if obs.snapshot_mtime > state.get("last_snapshot_mtime", 0.0):
        state["last_snapshot_mtime"] = obs.snapshot_mtime

    # ═══ POST-ACTION CHECK ═══
    if handle_post_action(state, obs, feedback):
        return state  # in verification mode — skip thinking/acting

    # ═══ THINK ═══
    recent = memory.get_recent_decisions(limit=5)
    decision = brain.decide(obs, recent)

    # Track decision source
    if decision.source == "gemma3":
        state["gemma_decisions"] = state.get("gemma_decisions", 0) + 1
    elif decision.source == "gemma3_offline":
        state["fallback_decisions"] = state.get("fallback_decisions", 0) + 1
    elif decision.source == "override":
        state["override_decisions"] = state.get("override_decisions", 0) + 1

    # ═══ VERBOSE OUTPUT — Mostrar análisis de Gemma 3 en terminal ═══
    _print_cycle_summary(state, obs, decision)

    # Si Gemma3 está offline, NO ejecutar ninguna acción
    if decision.source == "gemma3_offline":
        logger.warning(
            f"⛔ Gemma 3 OFFLINE — Ciclo #{state['cycle_count']} sin cerebro. "
            f"Levanta Ollama: ollama serve && ollama run gemma3:4b"
        )
        return state

    # Log decision — SIEMPRE loguear (el usuario quiere ver todo)
    logger.info(
        f"Cycle #{state['cycle_count']} | Decision: {decision.action} "
        f"(conf={decision.confidence:.0%}, src={decision.source}) "
        f"— {decision.reasoning[:120]}"
    )

    # ═══ RECORD (before acting — so we have the episode_id for tracking) ═══
    episode_id = memory.record_episode(obs.to_dict(), asdict(decision))

    # ═══ ACT ═══
    if decision.action != "wait":
        executed = executor.execute(decision)
        if executed:
            feedback.start_tracking(episode_id, decision.action)
        else:
            # Action blocked by cooldown or error
            memory.update_outcome(episode_id, "no_effect", 0, "Blocked by cooldown or execution error")
    else:
        # wait → immediately resolved
        memory.update_outcome(episode_id, "resolved", 0, "wait — no action needed")

    return state


# ─── CLI Commands ─────────────────────────────────────────────────────────────

def cmd_status(state: dict, memory: Memory) -> None:
    now = time.time()
    print("\n=== OKLA Smart Monitor — Gemma 3 Agent ===")
    print(f"  Ciclos totales    : {state.get('cycle_count', 0)}")
    print(f"  Decisiones Gemma  : {state.get('gemma_decisions', 0)}")
    print(f"  Decisiones fallback: {state.get('fallback_decisions', 0)}")
    print(f"  Decisiones override: {state.get('override_decisions', 0)}")
    print(f"  Acciones totales  : {state.get('total_actions', 0)}")
    print()

    last_act = state.get("last_activity_ts", 0)
    if last_act:
        print(f"  Última actividad  : hace {now - last_act:.0f}s")
    last_action = state.get("last_action_ts", 0)
    if last_action:
        print(f"  Última acción     : {state.get('last_action_type', '?')} hace {now - last_action:.0f}s")
    print(f"  Rate limits       : {state.get('rate_limit_count', 0)}")
    print(f"  Nuevos chats      : {state.get('new_chat_count', 0)}")
    print(f"  Continues         : {state.get('continue_count', 0)}")
    print()

    # Memory stats
    print(f"  Episodios en DB   : {memory.get_episode_count()}")
    print(f"  Lecciones activas : {memory.get_lesson_count()}")
    print()

    # Recent decisions
    recent = memory.get_recent_decisions(limit=5)
    if recent:
        print("  Últimas 5 decisiones:")
        for d in recent:
            print(f"    [{d.get('timestamp_human', '?')}] {d.get('action', '?')} "
                  f"({d.get('source', '?')}) → {d.get('outcome', '?')}")
    print()

    # Lessons
    lessons = memory.get_all_lessons()
    if lessons:
        print(f"  Lecciones ({len(lessons)}):")
        for l in lessons[:5]:
            conf = l.get("confidence", 0)
            print(f"    [{conf:.0%}] {l.get('lesson', '')[:80]}")
        if len(lessons) > 5:
            print(f"    ... y {len(lessons) - 5} más")
    print()

    # Ollama connectivity
    try:
        import urllib.request
        with urllib.request.urlopen("http://localhost:11434/api/tags", timeout=3) as resp:
            data = json.loads(resp.read())
            models = [m["name"] for m in data.get("models", [])]
            gemma = [m for m in models if "gemma" in m.lower()]
            print(f"  Ollama modelos    : {', '.join(gemma) if gemma else 'Gemma NO encontrado'}")
            print(f"  Ollama total      : {len(models)} modelos")
    except Exception as e:
        print(f"  Ollama            : NO DISPONIBLE ({e})")
    print()


# ─── Ollama auto-start ────────────────────────────────────────────────────────

def _ensure_ollama_running() -> None:
    """
    Si Ollama no responde en el puerto 11434, intenta iniciarlo automáticamente.
    Usa 'ollama serve' en background. Espera hasta 15s para que levante.
    """
    import urllib.request as _ur
    import subprocess as _sp

    # Check if already running
    try:
        with _ur.urlopen("http://localhost:11434/api/tags", timeout=3) as r:
            if r.status == 200:
                return  # already up
    except Exception:
        pass

    # Try to start ollama serve
    ollama_bin = None
    for candidate in ["/opt/homebrew/bin/ollama", "/usr/local/bin/ollama", "ollama"]:
        try:
            result = _sp.run(["which" if candidate == "ollama" else "ls", candidate],
                             capture_output=True, text=True, timeout=3)
            if result.returncode == 0:
                ollama_bin = candidate
                break
        except Exception:
            continue

    if not ollama_bin:
        logger.warning("_ensure_ollama_running: ollama binary not found — skipping auto-start")
        return

    logger.info(f"🚀 Auto-starting Ollama ({ollama_bin} serve) ...")
    try:
        _sp.Popen(
            [ollama_bin, "serve"],
            stdin=_sp.DEVNULL,
            stdout=open("/tmp/ollama_autostart.log", "a"),
            stderr=_sp.STDOUT,
            start_new_session=True,
        )
    except Exception as e:
        logger.warning(f"_ensure_ollama_running: failed to start ollama: {e}")
        return

    # Wait up to 15s for Ollama to be ready
    for i in range(15):
        time.sleep(1)
        try:
            with _ur.urlopen("http://localhost:11434/api/tags", timeout=2) as r:
                if r.status == 200:
                    logger.info(f"✅ Ollama started successfully (after {i+1}s)")
                    return
        except Exception:
            pass

    logger.warning("_ensure_ollama_running: Ollama did not respond after 15s — continuing anyway")


# ─── Main ─────────────────────────────────────────────────────────────────────

def main() -> None:
    parser = argparse.ArgumentParser(
        description="OKLA Smart Monitor — Gemma 3 Agent",
    )
    parser.add_argument("--interval", type=int, default=DEFAULT_INTERVAL,
                        help=f"Seconds between cycles (default: {DEFAULT_INTERVAL})")
    parser.add_argument("--once", action="store_true",
                        help="Run one cycle and exit")
    parser.add_argument("--status", action="store_true",
                        help="Show status and exit")
    parser.add_argument("--debug", action="store_true",
                        help="Verbose output")
    parser.add_argument("--dry-run", action="store_true",
                        help="Don't execute actions — only show decisions")
    args = parser.parse_args()

    setup_logging(debug=args.debug)

    # Initialize components
    memory = Memory()
    seed_initial_lessons(memory)

    state = load_state()
    if not state:
        state = default_state()
        logger.info("First run — initializing state")
        save_state(state)

    if args.status:
        cmd_status(state, memory)
        memory.close()
        return

    # ─── Singleton check (early — before Gemma check to avoid restart loop) ────
    # This MUST run before sys.exit so the PID file is set even if Gemma is down.
    # That way VS Code task auto-restarts are blocked by the existing live PID.
    if args.once:
        pass  # --once skips singleton check
    else:
        if PID_FILE.exists():
            try:
                existing_pid = int(PID_FILE.read_text().strip())
                if existing_pid != os.getpid():
                    os.kill(existing_pid, 0)  # raises if process doesn't exist
                    logger.warning(f"Agent already running (PID {existing_pid}). Exiting.")
                    memory.close()
                    sys.exit(0)
            except (ValueError, ProcessLookupError, PermissionError):
                pass  # stale PID — take over

        PID_FILE.write_text(str(os.getpid()))

    brain = Brain(memory=memory)
    feedback = FeedbackLoop(memory)
    executor = ActionExecutor(state, dry_run=args.dry_run)

    # ═══ AUTO-START OLLAMA si no está corriendo ════════════════════════════════
    _ensure_ollama_running()

    # ═══ VERIFICAR GEMMA 3 ═════════════════════════════════════════════════════
    # --once: single fast check (no retries — used for debugging/testing)
    # loop mode: retry up to 3 times with 20s wait (gives Ollama time to start)
    logger.info("Verificando conexión con Ollama/Gemma 3...")
    gemma_ok = False
    max_attempts = 1 if args.once else 3
    for attempt in range(max_attempts):
        try:
            import urllib.request as _ur
            with _ur.urlopen("http://localhost:11434/api/tags", timeout=5) as resp:
                data = json.loads(resp.read())
                models = [m["name"] for m in data.get("models", [])]
                gemma = [m for m in models if "gemma" in m.lower()]
                if gemma:
                    logger.info(f"✅ Gemma 3 disponible: {', '.join(gemma)}")
                    gemma_ok = True
                    break
                else:
                    logger.warning(
                        f"⚠️ Ollama corriendo pero Gemma NO encontrado. "
                        f"Modelos: {', '.join(models)}"
                    )
                    logger.warning("   Ejecuta: ollama pull gemma3:4b")
        except Exception as e:
            logger.warning(f"⏳ Ollama no disponible (intento {attempt + 1}/{max_attempts}): {e}")

        if attempt < max_attempts - 1:
            logger.info("   Reintentando en 20s...")
            time.sleep(20)

    if not gemma_ok:
        logger.error("")
        logger.error("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━")
        logger.error(f"  ⛔ GEMMA 3 NO DISPONIBLE TRAS {max_attempts} INTENTO(S)")
        logger.error("  Ejecuta: ollama serve  (terminal separada)")
        logger.error("  Luego:   ollama pull gemma3:4b")
        logger.error("  El agente no puede tomar decisiones sin Gemma.")
        logger.error("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━")
        memory.close()
        try:
            PID_FILE.unlink()
        except Exception:
            pass
        sys.exit(1)

    # Fresh start: reset timers
    now = time.time()
    state["last_activity_ts"] = now
    state["post_action_ts"] = 0.0
    state["post_action_type"] = ""
    state["post_action_retry_count"] = 0
    state["model_completion_seen_ts"] = 0.0

    # Sync snapshot mtime from the imported CHAT_SNAPSHOT_FILE (actions module)
    try:
        if CHAT_SNAPSHOT_FILE.exists():
            state["last_snapshot_mtime"] = CHAT_SNAPSHOT_FILE.stat().st_mtime
            content = CHAT_SNAPSHOT_FILE.read_text(encoding="utf-8")
            lines = content.splitlines()
            body = [
                l for l in (lines[1:] if lines else [])
                if not re.match(r"^\s*\d+%\s*$", l)
            ]
            state["last_snapshot_body_hash"] = hashlib.md5(
                "\n".join(body).encode()
            ).hexdigest()
    except Exception:
        pass

    save_state(state)

    logger.info(
        f"Smart Monitor started — interval={args.interval}s, "
        f"dry_run={args.dry_run}, "
        f"episodes={memory.get_episode_count()}, "
        f"lessons={memory.get_lesson_count()}"
    )

    def one_cycle():
        nonlocal state
        state = run_cycle(state, memory, brain, feedback, executor, dry_run=args.dry_run)
        save_state(state)

    if args.once:
        one_cycle()
        memory.close()
        return

    # Graceful shutdown
    def _shutdown(signum, frame):
        logger.info("Shutting down (signal received)")
        memory.close()
        try:
            PID_FILE.unlink()
        except Exception:
            pass
        sys.exit(0)

    signal.signal(signal.SIGINT, _shutdown)
    signal.signal(signal.SIGTERM, _shutdown)

    # Main loop
    while True:
        try:
            one_cycle()
        except KeyboardInterrupt:
            logger.info("Stopped by user (Ctrl+C)")
            break
        except Exception as e:
            logger.error(f"Unexpected error in cycle: {e}", exc_info=True)
        time.sleep(args.interval)

    memory.close()
    try:
        PID_FILE.unlink()
    except Exception:
        pass


if __name__ == "__main__":
    main()
