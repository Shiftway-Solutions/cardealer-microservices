"""
feedback.py — Feedback Loop y Auto-Mejora
==========================================
Después de cada acción, monitorea si la situación se normaliza.
Registra el resultado en la memoria y, periódicamente, ejecuta
"reflection" con Gemma 3 para extraer nuevas lecciones.

Ciclo:
  1. Acción ejecutada → start_tracking(episode_id)
  2. Cada ciclo (20s) → check_outcome(observation)
  3. Si normalizado o timeout → save resultado
  4. Cada N episodios → reflect() con Gemma 3 → nuevas lecciones
"""

import json
import logging
import time
import urllib.request
import urllib.error
from typing import Optional

from .memory import Memory

logger = logging.getLogger("smart_monitor.feedback")

# ─── Configuración ──────────────────────────────────────────────────────────────

VERIFICATION_TIMEOUT_SECS = 120   # 2 minutos para verificar resultado
REFLECTION_EVERY_N = 15           # reflexionar cada N episodios con outcome
OLLAMA_URL = "http://localhost:11434/api/chat"
OLLAMA_MODEL = "gemma3:4b"

# Acciones que indican "normalizado"
NORMALIZING_SIGNALS = [
    "generation_active",     # el agente empezó a generar
    "body_changed",          # el snapshot cambió (indica actividad)
    "new_model_active",      # se cycló el modelo exitosamente
    "new_chat_opened",       # se abrió un nuevo chat
]

REFLECTION_PROMPT = """You are analyzing the performance of a VS Code Copilot monitoring agent.
Review these recent episodes (observation → decision → outcome) and extract NEW lessons.

Episodes:
{episodes_text}

Current lessons the agent already knows:
{current_lessons}

Action stats (last 24h):
{action_stats}

Your task:
1. Identify patterns: Which actions consistently work or fail for specific situations?
2. Find NEW insights not already captured in current lessons.
3. Rate existing lessons: Should any be strengthened (higher confidence) or weakened?

Respond in JSON:
{{
  "new_lessons": [
    {{"lesson": "description of what was learned", "category": "rate_limit|stall|error|general", "confidence": 0.7}}
  ],
  "reinforce": [
    {{"lesson_id": 1, "success": true}}
  ],
  "observations": "Brief summary of agent performance"
}}

Only include genuinely NEW lessons not already covered. If nothing new, return empty arrays."""


class FeedbackLoop:
    """Monitorea resultados de acciones y aprende de ellos."""

    def __init__(self, memory: Memory):
        self._memory = memory
        self._tracking: Optional[dict] = None
        self._episodes_since_reflection = 0
        self._last_reflection_time = 0.0

    @property
    def is_tracking(self) -> bool:
        return self._tracking is not None

    def start_tracking(self, episode_id: int, action: str) -> None:
        """Inicia el tracking de un episodio tras ejecutar una acción."""
        if action == "wait":
            # No trackear "wait" — no es una acción real
            return

        self._tracking = {
            "episode_id": episode_id,
            "action": action,
            "started_at": time.time(),
            "checks": 0,
        }
        logger.info(f"Tracking episode #{episode_id}, action={action}")

    def check_outcome(self, observation_dict: dict) -> Optional[str]:
        """
        Llamar cada ciclo mientras se está trackeando.
        Retorna el outcome si se resolvió o expiró, None si sigue pendiente.
        """
        if not self._tracking:
            return None

        elapsed = time.time() - self._tracking["started_at"]
        self._tracking["checks"] += 1
        action = self._tracking["action"]

        # ── Verificar si la situación se normalizó ──
        normalized = self._check_normalization(observation_dict, action)

        if normalized:
            outcome = "resolved"
            notes = f"Normalizado en {elapsed:.0f}s tras {self._tracking['checks']} checks"
            logger.info(f"Episode #{self._tracking['episode_id']}: {outcome} — {notes}")
        elif elapsed > VERIFICATION_TIMEOUT_SECS:
            # Timeout — la acción no tuvo efecto visible
            outcome = self._determine_timeout_outcome(observation_dict)
            notes = f"Timeout {VERIFICATION_TIMEOUT_SECS}s, {self._tracking['checks']} checks"
            logger.info(f"Episode #{self._tracking['episode_id']}: {outcome} — {notes}")
        else:
            return None  # Aún en verificación

        # ── Guardar resultado ──
        self._memory.update_outcome(
            self._tracking["episode_id"], outcome, elapsed, notes,
        )
        self._tracking = None
        self._episodes_since_reflection += 1

        # ── ¿Es hora de reflexionar? ──
        if self._episodes_since_reflection >= REFLECTION_EVERY_N:
            self._try_reflect()

        return outcome

    def force_close_tracking(self, outcome: str = "escalated", notes: str = "") -> None:
        """Cierra el tracking forzosamente (ej. al escalar a nueva acción)."""
        if not self._tracking:
            return
        elapsed = time.time() - self._tracking["started_at"]
        self._memory.update_outcome(
            self._tracking["episode_id"], outcome, elapsed, notes,
        )
        logger.info(f"Force-closed episode #{self._tracking['episode_id']}: {outcome}")
        self._tracking = None
        self._episodes_since_reflection += 1

    # ─── Normalization detection ────────────────────────────────────────────────

    def _check_normalization(self, obs: dict, action: str) -> bool:
        """Verifica si la situación se normalizó según la acción tomada."""

        # Señales genéricas de normalización
        if obs.get("generation_active"):
            return True
        if obs.get("snapshot_body_changed") and obs.get("secs_since_last_activity", 999) < 30:
            return True

        # Señales específicas por acción
        if action == "send_continue":
            # Éxito si el body cambió (el agente respondió al continuar)
            return obs.get("snapshot_body_changed", False)

        if action == "cycle_model":
            # BUG FIX: "rate_limit_active" does not exist in the serialized Observation.
            # Use the actual fields: log_dominant_event and snapshot_errors.
            still_rate_limited = (
                obs.get("log_dominant_event") == "rate_limited"
                or "rate_limited" in obs.get("snapshot_errors", [])
            )
            return not still_rate_limited

        if action in ("open_new_chat", "stop_and_new_chat"):
            # Éxito si hay un nuevo chat (snapshot nuevo y joven)
            return obs.get("snapshot_age_secs", 999) < 60

        if action == "focus_vscode":
            # Éxito si VS Code está corriendo
            return obs.get("vscode_running", False)

        return False

    def _determine_timeout_outcome(self, obs: dict) -> str:
        """Determina qué tipo de fallo fue al expirar el timeout."""
        if obs.get("snapshot_errors"):
            return "escalated"  # Hay errores nuevos, necesita escalación
        return "no_effect"  # Simplemente no funcionó

    # ─── Reflection (auto-mejora con Gemma 3) ──────────────────────────────────

    def _try_reflect(self) -> None:
        """Ejecuta una reflexión para extraer nuevas lecciones."""
        try:
            self._reflect()
        except Exception as e:
            logger.warning(f"Reflection failed: {e}")
        finally:
            self._episodes_since_reflection = 0
            self._last_reflection_time = time.time()

    def _reflect(self) -> None:
        """Usa Gemma 3 para analizar episodios recientes y extraer lecciones."""
        episodes = self._memory.get_episodes_for_reflection(limit=20)
        if len(episodes) < 5:
            logger.info("Not enough episodes for reflection")
            return

        current_lessons = self._memory.get_all_lessons()
        action_stats = self._memory.get_action_stats(hours=24)

        # Formatear episodios para el prompt
        episodes_text = "\n".join(
            f"  - Situation: {e['situation_summary'][:100]}"
            f"  Action: {e['action']} (source:{e['source']})"
            f"  Outcome: {e['outcome']} — {e['outcome_notes']}"
            for e in episodes
        )

        lessons_text = "\n".join(
            f"  [{l['id']}] {l['lesson']} (confidence: {l['confidence']:.1%})"
            for l in current_lessons
        )

        stats_text = json.dumps(action_stats, indent=2)

        prompt = REFLECTION_PROMPT.format(
            episodes_text=episodes_text,
            current_lessons=lessons_text,
            action_stats=stats_text,
        )

        # Llamar a Gemma 3
        payload = json.dumps({
            "model": OLLAMA_MODEL,
            "messages": [
                {"role": "system", "content": "You are a learning agent that extracts actionable lessons from experience data. Respond ONLY in JSON."},
                {"role": "user", "content": prompt},
            ],
            "stream": False,
            "options": {"temperature": 0.3, "num_predict": 512},
            "format": "json",
        }).encode()

        req = urllib.request.Request(
            OLLAMA_URL,
            data=payload,
            headers={"Content-Type": "application/json"},
            method="POST",
        )

        try:
            with urllib.request.urlopen(req, timeout=120) as resp:
                data = json.loads(resp.read())
        except (urllib.error.URLError, TimeoutError) as e:
            logger.warning(f"Ollama reflection call failed: {e}")
            return

        content = data.get("message", {}).get("content", "")
        try:
            result = json.loads(content)
        except json.JSONDecodeError:
            logger.warning(f"Failed to parse reflection JSON: {content[:200]}")
            return

        # Procesar nuevas lecciones
        for lesson_data in result.get("new_lessons", []):
            text = lesson_data.get("lesson", "").strip()
            cat = lesson_data.get("category", "general")
            conf = lesson_data.get("confidence", 0.5)
            if text and len(text) > 10:
                lid = self._memory.add_lesson(text, cat, conf)
                logger.info(f"New lesson #{lid}: {text[:80]}")

        # Reforzar/debilitar lecciones existentes
        for reinforcement in result.get("reinforce", []):
            lid = reinforcement.get("lesson_id")
            success = reinforcement.get("success", True)
            if lid:
                self._memory.reinforce_lesson(int(lid), success)
                logger.info(f"Reinforced lesson #{lid}: {'✓' if success else '✗'}")

        observations = result.get("observations", "")
        if observations:
            logger.info(f"Reflection observations: {observations[:200]}")
