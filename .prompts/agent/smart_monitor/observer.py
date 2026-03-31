"""
observer.py — Capa de Observación Estructurada
===============================================
Recopila TODAS las señales del entorno y las empaqueta en un objeto
`Observation` que Gemma 3 puede razonar sobre él.

Señales recopiladas:
  1. chat_snapshot.txt — contenido, mtime, hash de cuerpo, errores visibles
  2. Copilot Chat log — líneas nuevas, clasificación de patrones
  3. CDP (Chrome DevTools) — texto del chat, tamaño del contexto
  4. VS Code process — foco, running status
  5. Estado del monitor — contadores, timers, historial de acciones
"""

import hashlib
import json
import os
import re
import subprocess
import time
import urllib.request
from dataclasses import dataclass, field, asdict
from datetime import datetime
from pathlib import Path
from typing import Optional

# ─── Rutas (heredadas del monitor original) ──────────────────────────────────
REPO_ROOT          = Path(__file__).parent.parent.parent.parent
CHAT_SNAPSHOT_FILE = REPO_ROOT / ".prompts" / "agent" / "chat_snapshot.txt"

# ─── Umbrales de contexto ─────────────────────────────────────────────────────
# DOM scraping via CDP solo ve texto visible (virtual scroll) — no el contexto real.
# Proxy: acumular bytes de diffs de snapshots como aproximación del contexto total.
CONTEXT_PROXY_THRESHOLD = 400_000   # 400 KB de diffs acumulados → peligro
CDP_TIMEOUT_EMERGENCY   = 3         # N timeouts consecutivos → VS Code bajo estrés
SESSION_MAX_MINUTES     = 180       # 3 horas de sesión continua → nuevo chat
SESSION_MAX_CONTINUES   = 20        # 20 continues en misma sesión → nuevo chat
MONITOR_LOG        = REPO_ROOT / ".github" / "copilot-monitor.log"
VSCODE_LOGS_BASE   = Path.home() / "Library" / "Application Support" / "Code" / "logs"
COPILOT_LOG_NAME   = "GitHub Copilot Chat.log"
CDP_HOST           = "localhost"
CDP_PORT           = 9222
VSCODE_PROCESS_MARKERS = (
    "Visual Studio Code.app/Contents/MacOS/Code",
    "Visual Studio Code.app/Contents/Frameworks/Code Helper",
)

# ─── Patrones de detección ────────────────────────────────────────────────────

# Errores visibles en el chat (buscar en últimos 500 chars del snapshot)
CHAT_ERROR_PATTERNS = [
    (re.compile(r"rate.limit|429|Too Many Requests|quota.*exhaust|exhausted.*quota|RateLimitError", re.I), "rate_limited"),
    (re.compile(r"overloaded|503|502|500|Internal Server Error|capacity|overloaded_error", re.I), "hard_error"),
    (re.compile(r"cancelled|canceled", re.I), "cancelled"),
    # VS Code Copilot suggests switching agent/model (rate limit on model, model unavailable)
    (re.compile(
        r"switch.*(?:model|agent)"
        r"|change.*model"
        r"|not.*available.*model"
        r"|model.*not.*available"
        r"|your.*agent.*limit"
        r"|request.*limit.*reached"
        r"|try.*(?:a )?different.*model"
        r"|this model.*(?:isn.t|is not).*available"
        r"|unable.*to.*use.*model"
        r"|model.*unavailable"
        r"|please.*switch.*model"
        r"|agent.*request.*limit",
        re.I,
    ), "agent_switch"),
    # Context window / token limit saturation messages from Copilot Chat UI
    (re.compile(
        r"context.*(?:full|limit|too.long|length|exceeded|window)"
        r"|conversation.*too.long"
        r"|maximum.*context"
        r"|token.*limit"
        r"|context_length_exceeded"
        r"|too many tokens"
        r"|This conversation is too long"
        r"|context window.*full"
        r"|The model.*maximum context"
        r"|exceeds.*token",
        re.I,
    ), "context_full"),
]

# Errores en el log de Copilot
# NOTA: ccreq:.*| error | se eliminó de hard_error porque genera demasiados
# falsos positivos — Copilot registra "error" para cualquier request fallido
# (tool validation, timeout parcial, etc.) incluso durante generación activa.
# Solo detectar errores de servidor reales con prefijo [error].
LOG_PATTERNS = [
    (re.compile(r"rate.limit|429|Too Many Requests|quota.*exhaust|exhausted.*quota|rate_limited|RateLimitError", re.I), "rate_limited"),
    (re.compile(r"\[error\].*(?:500|503|502|overload|capacity|Internal Server)|overloaded_error|overload_error", re.I), "hard_error"),
    (re.compile(r"failed validation.*schema must be|ToolValidationError", re.I), "tool_validation_error"),
    (re.compile(r"ccreq:.*\|\s*cancelled\s*\|", re.I), "cancelled"),
    (re.compile(r"Stop hook result.*shouldContinue=false|ToolCallingLoop.*[Ss]top|agent.*loop.*stop", re.I), "loop_stopped"),
    (re.compile(r"ccreq:.*\|\s*success\s*\|", re.I), "success"),
    (re.compile(r"ccreq:.*\|\s*error\s*\|", re.I), "request_error"),
    (re.compile(r"context_length_exceeded|context.*too.long|maximum.*context|token.*limit|exceeds.*token", re.I), "context_full"),
]

NOISE_PATTERN = re.compile(r"failed validation.*schema must be|Tool mcp_aisquare.*failed validation", re.I)

# Footer de modelo visible (el agente terminó su turno)
MODEL_COMPLETION_PATTERN = re.compile(
    r'(?:Claude\s+(?:Sonnet|Opus|Haiku)|GPT-\d(?:\.\d+)?|o[34](?:-mini)?|GPT-5)'
    r'.{0,60}'
    r'(?:\n\s*(?:Local|Remote|Shared|Default)\s*\n'
    r'|\s*[·•]\s*(?:High|Low|Medium|Xhigh|XHigh)\s*\n'
    r'|\s+(?:High|Low|Medium|Xhigh|XHigh)\s*\n)',
    re.I,
)

# Señales de generación activa (NO intervenir)
ACTIVE_GENERATION_PATTERNS = re.compile(
    r'\d+\s*%'                                          # barra de progreso %
    r'|Preparing\.\.\.'                                 # iniciando
    r'|Thinking\.\.\.'                                  # thinking effort
    r'|Running\s+\w'                                    # Running <herramienta>
    r'|Compacting conversation'                         # compactación
    r'|Analyzing\s'                                     # analizando
    r'|Reading\s'                                       # leyendo archivos
    r'|Searching\s'                                     # buscando
    r'|Writing\s'                                       # escribiendo
    r'|Editing\s'                                       # editando
    r'|Calling\s',                                      # llamando herramienta
    re.I,
)


@dataclass
class Observation:
    """Snapshot completo del estado del entorno en un momento dado."""
    timestamp: float = 0.0
    timestamp_human: str = ""

    # chat_snapshot.txt
    snapshot_exists: bool = False
    snapshot_mtime: float = 0.0
    snapshot_age_secs: float = 0.0
    snapshot_body_hash: str = ""
    snapshot_body_changed: bool = False
    snapshot_size_chars: int = 0
    snapshot_tail_500: str = ""
    snapshot_errors: list = field(default_factory=list)  # ["rate_limited", ...]
    snapshot_has_active_generation: bool = False
    snapshot_has_model_footer: bool = False

    # Log de Copilot
    log_path: str = ""
    log_new_lines_count: int = 0
    log_events: dict = field(default_factory=dict)  # {"success": 3, "cancelled": 1, ...}
    log_dominant_event: str = "idle"

    # Context saturation (any source)
    context_saturated: bool = False          # True when ANY context-full signal fires
    context_saturations_count: int = 0       # consecutive saturations on current model
    # Chat UI model switch trigger
    # True when rate_limited / hard_error / agent_switch detected in chat or log
    # → agent should call cycle_chat_ui_model() to switch immediately in current session
    chat_ui_switch_needed: bool = False

    # CDP
    cdp_available: bool = False
    cdp_chat_text_length: int = 0
    cdp_chat_changed: bool = False
    cdp_context_full: bool = False
    # CDP stress signals (Fix 1: CDP timeouts as emergency)
    cdp_consecutive_timeouts: int = 0
    # Context proxy — bytes acumulados de diffs (Fix 3)
    cdp_context_proxy_bytes: int = 0
    cdp_context_proxy_full: bool = False

    # VS Code
    vscode_running: bool = True
    vscode_focused: bool = True

    # Timers derivados del estado
    secs_since_last_activity: float = 0.0
    secs_since_last_action: float = 0.0
    secs_since_last_new_chat: float = 0.0
    secs_since_last_continue: float = 0.0

    # Contadores del estado
    error_continue_count: int = 0
    rate_limit_count: int = 0
    new_chat_count: int = 0
    continue_count: int = 0
    # Session age — Fix 4: max session duration safeguard
    session_age_mins: float = 0.0
    session_too_long: bool = False

    # Post-action verification
    post_action_active: bool = False
    post_action_type: str = ""
    post_action_elapsed_secs: float = 0.0
    post_action_retry_count: int = 0

    # Resumen para Gemma
    situation_summary: str = ""

    def to_prompt_context(self) -> str:
        """Genera un resumen estructurado para enviar a Gemma 3."""
        lines = [
            f"=== OBSERVACIÓN DEL ENTORNO ({self.timestamp_human}) ===",
            "",
            f"SNAPSHOT: {'existe' if self.snapshot_exists else 'NO EXISTE'}"
            f" | edad: {self.snapshot_age_secs:.0f}s"
            f" | cambió: {'SÍ' if self.snapshot_body_changed else 'NO'}"
            f" | tamaño: {self.snapshot_size_chars} chars"
            f" | generación_activa: {'SÍ' if self.snapshot_has_active_generation else 'NO'}"
            f" | footer_modelo: {'SÍ' if self.snapshot_has_model_footer else 'NO'}",
        ]
        if self.snapshot_errors:
            lines.append(f"ERRORES EN SNAPSHOT: {', '.join(self.snapshot_errors)}")

        lines.extend([
            "",
            f"LOG: {self.log_new_lines_count} líneas nuevas | evento dominante: {self.log_dominant_event}",
            f"CDP: {'disponible' if self.cdp_available else 'no disponible'}"
            f" | contexto_lleno: {'SÍ' if self.cdp_context_full else 'NO'}"
            f" | chat_chars: {self.cdp_chat_text_length}"
            f" | timeouts_consecutivos: {self.cdp_consecutive_timeouts}"
            f" | proxy_bytes: {self.cdp_context_proxy_bytes:,}"
            f" | proxy_lleno: {'SÍ' if self.cdp_context_proxy_full else 'NO'}"
            f" | SATURADO: {'⚠️ SÍ' if self.context_saturated else 'NO'}"
            f" (saturaciones_consecutivas={self.context_saturations_count})"
            f" | SWITCH_UI: {'⚠️ SÍ' if self.chat_ui_switch_needed else 'NO'}",
            "",
            f"VS CODE: {'corriendo' if self.vscode_running else 'DETENIDO'}"
            f" | foco: {'SÍ' if self.vscode_focused else 'NO'}",
            "",
            "TIMERS:",
            f"  última_actividad: hace {self.secs_since_last_activity:.0f}s ({self.secs_since_last_activity/60:.1f} min)",
            f"  último_continue: hace {self.secs_since_last_continue:.0f}s",
            f"  último_new_chat: hace {self.secs_since_last_new_chat:.0f}s",
            "",
            "CONTADORES:",
            f"  error_retries: {self.error_continue_count}/{3}",
            f"  rate_limits_totales: {self.rate_limit_count}",
            f"  new_chats_totales: {self.new_chat_count}",
            f"  continues_totales: {self.continue_count}",
            f"  sesion_activa: {self.session_age_mins:.0f} min | sesion_demasiado_larga: {'SÍ' if self.session_too_long else 'NO'}",
        ])

        if self.post_action_active:
            lines.extend([
                "",
                f"VERIFICACIÓN POST-ACCIÓN ACTIVA:",
                f"  tipo: {self.post_action_type}",
                f"  elapsed: {self.post_action_elapsed_secs:.0f}s / 120s",
                f"  reintentos: {self.post_action_retry_count}",
            ])

        if self.snapshot_tail_500:
            # Solo el tail limpio (sin datos sensibles)
            clean_tail = self.snapshot_tail_500[:300]
            lines.extend([
                "",
                f"ÚLTIMAS LÍNEAS DEL CHAT:",
                clean_tail,
            ])

        return "\n".join(lines)

    def to_dict(self) -> dict:
        return asdict(self)


class Observer:
    """Recopila observaciones del entorno."""

    def __init__(self, state: dict):
        self._state = state
        self._log_offset = state.get("log_offset", 0)
        self._log_path = state.get("log_path", "")
        self._prev_body_hash = state.get("last_snapshot_body_hash", "")
        self._prev_chat_hash = state.get("chat_hash", "")

    def observe(self) -> Observation:
        """Recopila todas las señales y retorna una Observation."""
        now = time.time()
        obs = Observation(
            timestamp=now,
            timestamp_human=datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
        )

        self._observe_snapshot(obs, now)
        self._observe_log(obs, now)
        self._observe_cdp(obs)
        self._observe_vscode(obs)
        self._observe_state_timers(obs, now)

        # Generar resumen de situación
        obs.situation_summary = self._summarize_situation(obs)

        return obs

    def _observe_snapshot(self, obs: Observation, now: float) -> None:
        """Lee el chat_snapshot.txt y extrae señales."""
        if not CHAT_SNAPSHOT_FILE.exists():
            obs.snapshot_exists = False
            return

        try:
            stat = CHAT_SNAPSHOT_FILE.stat()
            obs.snapshot_exists = True
            obs.snapshot_mtime = stat.st_mtime
            obs.snapshot_age_secs = now - stat.st_mtime

            content = CHAT_SNAPSHOT_FILE.read_text(encoding="utf-8")
            obs.snapshot_size_chars = len(content)

            # Hash del cuerpo (sin header timestamp ni líneas %)
            lines = content.splitlines()
            body = [
                l for l in (lines[1:] if lines else [])
                if not re.match(r"^\s*\d+%\s*$", l)
            ]
            obs.snapshot_body_hash = hashlib.md5("\n".join(body).encode("utf-8")).hexdigest()
            obs.snapshot_body_changed = (
                obs.snapshot_body_hash != self._prev_body_hash
                and obs.snapshot_body_hash != ""
            )

            # Tail para análisis de errores
            tail = content[-500:] if len(content) > 500 else content
            obs.snapshot_tail_500 = tail

            # Detectar errores en el tail
            for pattern, evt in CHAT_ERROR_PATTERNS:
                if pattern.search(tail):
                    obs.snapshot_errors.append(evt)

            # Indicadores de generación activa y footer del modelo
            # Usar cola más larga (800 chars) para capturar estados de herramientas
            tail_800 = content[-800:] if len(content) > 800 else content
            # Generación activa: % de progreso O cualquier herramienta corriendo
            obs.snapshot_has_active_generation = bool(
                ACTIVE_GENERATION_PATTERNS.search(tail_800)
            )
            obs.snapshot_has_model_footer = bool(MODEL_COMPLETION_PATTERN.search(tail_800))

            # ── GUARD: Snapshot estale ───────────────────────────────────────
            # Si el snapshot lleva >10 min sin actualizarse, los patrones de
            # "generación activa" son residuales de una sesión anterior.
            # Una generación real actualiza el snapshot cada ~20s via CDP.
            # Sin este guard el agente queda paralizado indefinidamente cuando
            # CDP falla y el snapshot queda congelado con "Compacting…"/"Preparing…".
            SNAPSHOT_STALE_SECS = 600  # 10 minutos sin update = estale
            if obs.snapshot_has_active_generation and obs.snapshot_age_secs > SNAPSHOT_STALE_SECS:
                import logging as _logging
                _logging.getLogger("smart_monitor").warning(
                    f"[STALE GUARD] Snapshot tiene {obs.snapshot_age_secs:.0f}s sin actualizar "
                    f"con patrones de generación activa — ignorando false positive."
                )
                obs.snapshot_has_active_generation = False

        except Exception:
            obs.snapshot_exists = False

    def _observe_log(self, obs: Observation, now: float) -> None:
        """Lee líneas nuevas del log de Copilot Chat."""
        previous_log_path = self._log_path
        log_path = self._find_copilot_log()
        if not log_path:
            return

        current_log_path = str(log_path)
        obs.log_path = current_log_path

        # Detectar cambio de log
        if current_log_path != previous_log_path:
            self._log_offset = 0
        self._log_path = current_log_path

        try:
            size = log_path.stat().st_size
            if size < self._log_offset:
                self._log_offset = 0  # rotación
            if size <= self._log_offset:
                obs.log_dominant_event = "idle"
                return

            with open(log_path, "rb") as f:
                f.seek(self._log_offset)
                raw = f.read()
            self._log_offset += len(raw)
            lines = raw.decode("utf-8", errors="replace").splitlines()

            # Clasificar líneas
            events = {}
            meaningful = 0
            for line in lines:
                if NOISE_PATTERN.search(line):
                    continue
                meaningful += 1
                for pattern, event_type in LOG_PATTERNS:
                    if pattern.search(line):
                        events[event_type] = events.get(event_type, 0) + 1
                        break

            obs.log_new_lines_count = meaningful
            obs.log_events = events

            # Evento dominante — considerar ratios, no solo prioridad estricta.
            # Si hay éxitos mezclados con errores de request (request_error),
            # la sesión está funcionando normalmente y no requiere intervención.
            success_count = events.get("success", 0)
            request_error_count = events.get("request_error", 0)
            hard_error_count = events.get("hard_error", 0)

            # Rate limit siempre tiene prioridad absoluta
            if "rate_limited" in events:
                obs.log_dominant_event = "rate_limited"
            # Hard error real (servidor) solo si no hay éxitos recientes
            elif hard_error_count > 0 and success_count == 0:
                obs.log_dominant_event = "hard_error"
            elif "tool_validation_error" in events:
                obs.log_dominant_event = "tool_validation_error"
            elif "cancelled" in events and success_count == 0:
                obs.log_dominant_event = "cancelled"
            elif "loop_stopped" in events:
                obs.log_dominant_event = "loop_stopped"
            elif success_count > 0:
                obs.log_dominant_event = "success"
            elif request_error_count > 0 and success_count == 0:
                # Solo request errors sin éxitos — posible problema
                obs.log_dominant_event = "request_error"
            else:
                obs.log_dominant_event = "no_pattern_match"
        except Exception:
            pass

    def _observe_cdp(self, obs: Observation) -> None:
        """Verifica disponibilidad de CDP y tamaño del contexto."""
        try:
            url = f"http://{CDP_HOST}:{CDP_PORT}/json/version"
            with urllib.request.urlopen(url, timeout=3) as resp:
                data = json.loads(resp.read())
                obs.cdp_available = bool(data.get("webSocketDebuggerUrl") or data.get("Browser"))
        except Exception:
            obs.cdp_available = False

        # Fix 1: Rastrear timeouts consecutivos de CDP como señal de estrés
        obs.cdp_consecutive_timeouts = self._state.get("cdp_consecutive_timeouts", 0)

        if obs.cdp_available:
            # Intentar leer longitud del chat via CDP (sin playwright para ser ligero)
            obs.cdp_chat_text_length = self._state.get("chat_len", 0)
            obs.cdp_context_full = obs.cdp_chat_text_length >= 600_000

        # Fix 3: Proxy de contexto — bytes acumulados de diffs del snapshot
        obs.cdp_context_proxy_bytes = self._state.get("context_proxy_bytes", 0)
        obs.cdp_context_proxy_full = obs.cdp_context_proxy_bytes >= CONTEXT_PROXY_THRESHOLD

        # Aggregate context_saturated: any source that indicates context window is full
        obs.context_saturated = (
            obs.cdp_context_full
            or obs.cdp_context_proxy_full
            or "context_full" in obs.snapshot_errors
        )
        obs.context_saturations_count = self._state.get("context_saturations_count", 0)

        # Chat UI switch trigger: rate limit / hard error / agent_switch in any source
        obs.chat_ui_switch_needed = (
            obs.log_dominant_event in ("rate_limited", "hard_error")
            or "rate_limited" in obs.snapshot_errors
            or "hard_error" in obs.snapshot_errors
            or "agent_switch" in obs.snapshot_errors
        )

    def _observe_vscode(self, obs: Observation) -> None:
        """Verifica si VS Code está corriendo y tiene foco."""
        obs.vscode_running = self._is_vscode_running(obs.cdp_available)

        try:
            r = subprocess.run(
                ["osascript", "-e",
                 'tell application "System Events" to get name of first application process whose frontmost is true'],
                capture_output=True, text=True, timeout=3,
            )
            obs.vscode_focused = "Code" in r.stdout
        except Exception:
            obs.vscode_focused = True

    def _is_vscode_running(self, cdp_available: bool) -> bool:
        """Best-effort detection that works for macOS app bundles and Code Helper processes."""
        try:
            r = subprocess.run(
                ["ps", "-axo", "command"],
                capture_output=True,
                text=True,
                timeout=3,
            )
            if r.returncode == 0:
                for line in r.stdout.splitlines():
                    if any(marker in line for marker in VSCODE_PROCESS_MARKERS):
                        return True
        except Exception:
            pass

        return cdp_available

    def _observe_state_timers(self, obs: Observation, now: float) -> None:
        """Copia timers y contadores del estado persistente."""
        s = self._state
        obs.secs_since_last_activity = now - s.get("last_activity_ts", now)
        obs.secs_since_last_continue = now - s.get("last_continue_ts", now)
        obs.secs_since_last_new_chat = now - s.get("last_new_chat_ts", now)
        # BUG FIX: include last_cycle_model_ts so cycle_model actions count as "last action"
        obs.secs_since_last_action = now - max(
            s.get("last_continue_ts", 0),
            s.get("last_new_chat_ts", 0),
            s.get("last_cycle_model_ts", 0),
        )

        obs.error_continue_count = s.get("error_continue_count", 0)
        obs.rate_limit_count = s.get("rate_limit_count", 0)
        obs.new_chat_count = s.get("new_chat_count", 0)
        obs.continue_count = s.get("continue_count", 0)

        # Fix 4: Session age — cuánto tiempo lleva esta sesión (desde último new_chat)
        # Si nunca se abrió un nuevo chat, usar la primera actividad del agente
        last_new_chat = s.get("last_new_chat_ts", 0.0)
        session_start = last_new_chat if last_new_chat > 0 else s.get("last_activity_ts", now)
        obs.session_age_mins = (now - session_start) / 60.0
        obs.session_too_long = (
            obs.session_age_mins > SESSION_MAX_MINUTES
            or obs.continue_count >= SESSION_MAX_CONTINUES
        )

        # Post-action
        post_ts = s.get("post_action_ts", 0.0)
        if post_ts > 0:
            obs.post_action_active = True
            obs.post_action_type = s.get("post_action_type", "")
            obs.post_action_elapsed_secs = now - post_ts
            obs.post_action_retry_count = s.get("post_action_retry_count", 0)

    def _summarize_situation(self, obs: Observation) -> str:
        """Genera un resumen en lenguaje natural para el LLM."""
        parts = []

        # Estado de generación activa (DON'T TOUCH)
        if obs.snapshot_has_active_generation and obs.snapshot_body_changed and not obs.snapshot_errors:
            parts.append("✅ GENERACIÓN ACTIVA — el agente está generando código ahora mismo. NO intervenir.")
        elif obs.snapshot_body_changed and not obs.snapshot_errors and obs.secs_since_last_activity < 120:
            parts.append("✅ ACTIVIDAD NORMAL — el chat cambió recientemente sin errores.")
        elif obs.snapshot_body_changed and obs.snapshot_errors:
            parts.append(f"⚠️ El chat cambió pero hay errores: {', '.join(obs.snapshot_errors)}.")
        elif not obs.snapshot_body_changed and obs.snapshot_has_model_footer and not obs.snapshot_has_active_generation:
            parts.append("🏁 TURNO COMPLETADO — footer del modelo visible, sin generación activa. Abrir nuevo chat si stall >5 min.")
        elif obs.secs_since_last_activity >= 480:
            parts.append(f"🔴 STALL CRÍTICO — {obs.secs_since_last_activity/60:.1f} min sin actividad → open_new_chat.")
        elif obs.secs_since_last_activity >= 300:
            parts.append(f"⚠️ STALL — {obs.secs_since_last_activity/60:.1f} min sin actividad → send_continue.")
        else:
            parts.append("Sin cambios recientes en el chat.")

        # Errores específicos con su acción recomendada
        if obs.cdp_context_full:
            parts.append("🔴 CONTEXTO LLENO (>600k chars via DOM) → stop_and_new_chat INMEDIATO.")

        if obs.cdp_context_proxy_full:
            parts.append(
                f"🔴 CONTEXTO PROXY LLENO ({obs.cdp_context_proxy_bytes:,} bytes acumulados) → "
                "stop_and_new_chat INMEDIATO (DOM scraping no puede medir el contexto real)."
            )

        if obs.cdp_consecutive_timeouts >= CDP_TIMEOUT_EMERGENCY:
            parts.append(
                f"🔴 CDP TIMEOUT x{obs.cdp_consecutive_timeouts} consecutivos → VS Code bajo estrés severo → "
                "stop_and_new_chat INMEDIATO."
            )

        if obs.session_too_long and not obs.cdp_context_proxy_full and not obs.cdp_context_full:
            parts.append(
                f"⚠️ SESIÓN LARGA ({obs.session_age_mins:.0f} min, {obs.continue_count} continues) → "
                "stop_and_new_chat preventivo."
            )

        if "rate_limited" in obs.snapshot_errors or obs.log_dominant_event == "rate_limited":
            parts.append(f"⚠️ RATE LIMIT detectado → cycle_model. Intentos previos: {obs.rate_limit_count}.")

        if "hard_error" in obs.snapshot_errors or obs.log_dominant_event == "hard_error":
            retries_left = max(0, 3 - obs.error_continue_count)
            if retries_left > 0:
                parts.append(f"⚠️ HARD ERROR (500/503) → send_continue ({retries_left} intentos restantes).")
            else:
                parts.append("🔴 HARD ERROR persistente → open_new_chat (max retries alcanzado).")

        if "tool_validation_error" in (obs.log_events or {}):
            parts.append("ℹ️ TOOL VALIDATION ERROR → wait (requiere fix manual, no actuar).")

        if "cancelled" in obs.snapshot_errors:
            parts.append("ℹ️ REQUEST CANCELADO → wait, enviar continue si stall >5 min.")

        if obs.log_dominant_event == "loop_stopped" and not obs.snapshot_has_active_generation:
            parts.append("🏁 LOOP DETENIDO en log → verificar si hay actividad, si no → open_new_chat.")

        # Verificación post-acción
        if obs.post_action_active:
            parts.append(
                f"⏳ POST-ACCIÓN '{obs.post_action_type}' hace {obs.post_action_elapsed_secs:.0f}s "
                f"(esperar normalización, reintento #{obs.post_action_retry_count}/1)."
            )

        # VS Code
        if not obs.vscode_running:
            parts.append("🔴 VS Code NO está corriendo.")
        elif not obs.vscode_focused:
            parts.append("⚠️ VS Code perdió el foco → focus_vscode.")

        return " ".join(parts)

    def get_updated_offsets(self) -> dict:
        """Retorna los offsets actualizados para guardar en el estado."""
        return {
            "log_offset": self._log_offset,
            "log_path": self._log_path,
        }

    def _find_copilot_log(self) -> Optional[Path]:
        """Encuentra el log activo de Copilot Chat."""
        if not VSCODE_LOGS_BASE.exists():
            return None
        best_path = None
        best_mtime = 0.0
        try:
            for session in VSCODE_LOGS_BASE.iterdir():
                if not session.is_dir():
                    continue
                for log_path in session.rglob(COPILOT_LOG_NAME):
                    if not log_path.is_file():
                        continue
                    try:
                        mt = log_path.stat().st_mtime
                        if mt > best_mtime:
                            best_mtime = mt
                            best_path = log_path
                    except Exception:
                        continue
        except Exception:
            return None

        return best_path
