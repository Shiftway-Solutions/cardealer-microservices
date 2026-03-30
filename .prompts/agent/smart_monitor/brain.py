"""
brain.py — Motor de Decisión con Gemma 3 Local
================================================
Envía las observaciones a Gemma 3 (via Ollama) y recibe una decisión
estructurada en JSON. Incluye:

  1. System prompt con todas las reglas del dominio
  2. Observación actual del entorno
  3. Historial de decisiones recientes (de la memoria)
  4. Extracción de JSON de la respuesta
  5. Si Gemma 3 no está disponible → NO actuar (wait obligatorio)

⚠️ REGLA: Gemma 3 es el CEREBRO del agente. Sin cerebro, el agente
no toma ninguna acción. NO hay fallback automático.

Decisiones posibles:
  - wait              → no hacer nada, seguir monitoreando
  - send_continue     → enviar "continuar" al chat actual
  - open_new_chat     → abrir nuevo chat + AGENT_LOOP_PROMPT
  - stop_and_new_chat → detener respuesta actual + nuevo chat
  - cycle_model       → cambiar al siguiente modelo (rate limit)
  - focus_vscode      → traer VS Code al frente
  - restart_monitor   → reiniciar el monitor completo
"""

import json
import time
import urllib.request
from dataclasses import dataclass
from typing import Optional

from .observer import Observation

OLLAMA_URL = "http://localhost:11434/api/generate"
OLLAMA_CHAT_URL = "http://localhost:11434/api/chat"
MODEL_NAME = "gemma3:4b"
INFERENCE_TIMEOUT = 90  # segundos (gemma3:4b cold start puede tomar 60s+)

SYSTEM_PROMPT = """\
Eres el cerebro de un agente watchdog que monitorea GitHub Copilot en VS Code.
Tu misión es mantener SIEMPRE un modelo de Copilot generando código.
Analiza la observación del entorno y decide la acción óptima.

━━━ ÁRBOL DE DECISIÓN (SEGUIR EN ORDEN) ━━━

1. VS Code NO está corriendo → wait (nada que hacer)
2. Contexto lleno → stop_and_new_chat INMEDIATO si cualquiera de:
   - cdp_context_full=true (DOM scraping confirmó >600k)
   - cdp_context_proxy_full=true (high-water mark del chat >400k chars — solo cuando no hay generación activa)
   - cdp_consecutive_timeouts>=3 (VS Code bajo estrés severo)
   - session_too_long=true (sesión >3h o >20 continues)
3. Generación activa (snapshot_has_active_generation=true) → wait SIEMPRE (incluso si hay snapshot_errors: pueden ser falsos positivos donde la IA escribe sobre errores en su código)
4. Actividad reciente (<2 min, secs_since_last_activity<120) + log sin errores concretos → wait
5. VS Code sin foco (vscode_focused=false) → focus_vscode
6. Rate limit CONFIRMADO en log (log_dominant_event="rate_limited") → cycle_model
   IMPORTANTE: solo actuar si el LOG confirma el rate limit. snapshot_errors con "rate_limited" puede ser la IA escribiendo sobre ese error.
7. Hard error (hard_error en snapshot_errors o log_dominant_event):
   - error_continue_count < 3 → send_continue (esperar 60s entre intentos)
   - error_continue_count >= 3 → open_new_chat
8. Tool validation error (tool_validation_error en log) → wait (requiere fix manual, notificar)
9. Request cancelado (cancelled en errores) → wait; si stall >3 min → send_continue
10. Stall 5-8 min (secs_since_last_activity entre 300 y 480) → send_continue
11. Stall >8 min (secs_since_last_activity > 480) → open_new_chat
12. Post-action activo (post_action_active=true, elapsed<120s) → wait (verificando resultado)
13. Sin situación clara → wait

━━━ CATÁLOGO DE ERRORES DEL CHAT ━━━

RATE_LIMITED — "rate limit", "429", "Too Many Requests", "quota exhausted", "RateLimitError"
  Acción: cycle_model → esperar 600s → send_continue en siguiente ciclo

HARD_ERROR — "overloaded", "503", "502", "500", "Internal Server Error", "capacity", "overloaded_error"
  Acción: send_continue × 3 (con 60s entre intentos) → open_new_chat si persiste

TOOL_VALIDATION_ERROR — "failed validation.*schema must be", "ToolValidationError"
  Acción: wait + notificar (no es recuperable automáticamente, el usuario debe revisar)

CANCELLED — "cancelled", "canceled"
  Acción: wait → send_continue si stall >5 min

CONTEXT_FULL — cdp_context_full=true, o chat_chars >600000, o cdp_context_proxy_full=true, o cdp_consecutive_timeouts>=3
  Acción: stop_and_new_chat INMEDIATO (prioridad máxima)

LOOP_STOPPED — "shouldContinue=false", footer del modelo visible, sin % de progreso
  Acción: open_new_chat si stall >3 min

STALL_SOFT — sin cambios 5-8 min, sin errores visibles
  Acción: send_continue

STALL_HARD — sin cambios >8 min
  Acción: open_new_chat con nuevo modelo

━━━ ESTADOS NORMALES (NO INTERVENIR) ━━━

- Texto del chat cambia cada ~2s → generación activa → wait
- "Preparing..." o "Thinking..." visible → wait
- "Compacting conversation..." → wait hasta 90s
- % de progreso visible en cualquier herramienta → wait
- "Running <herramienta>..." → wait
- Texto de respuesta completo reciente (<2 min) → wait

━━━ REGLAS CRÍTICAS ━━━

- NUNCA intervenir si hay generación activa (% visible, texto cambiando).
- Preferir send_continue sobre open_new_chat — menos disruptivo.
- Respetar cooldowns: send_continue(60s), open_new_chat(90s), cycle_model(120s).
- Tras cualquier acción esperar 2 min antes de actuar de nuevo (post_action_active).
- Si el mismo patrón de error persiste >3 veces → escalar al siguiente nivel.
- El modelo preferido es GPT-5.4 · Xhigh. Arrancar siempre con él si está disponible.

DECISIONES VÁLIDAS (responde EXACTAMENTE una de estas):
- wait              → no hacer nada
- send_continue     → enviar "continuar" al chat
- open_new_chat     → abrir nuevo chat con el prompt completo
- stop_and_new_chat → detener generación + nuevo chat (para contexto lleno)
- cycle_model       → cambiar modelo (para rate limit)
- focus_vscode      → traer VS Code al frente

FORMATO DE RESPUESTA (JSON estricto):
{
  "decision": "<una de las decisiones válidas>",
  "confidence": <0.0 a 1.0>,
  "reasoning": "<explicación breve de por qué elegiste esta acción>",
  "wait_before_action_secs": <0-300>
}
"""


@dataclass
class Decision:
    """Resultado de la deliberación de Gemma 3."""
    action: str = "wait"
    confidence: float = 0.0
    reasoning: str = ""
    wait_before_action_secs: int = 0
    source: str = "gemma3"  # "gemma3" | "fallback" | "override"
    raw_response: str = ""
    latency_ms: int = 0


class Brain:
    """Motor de decisión inteligente: Gemma 3 local OBLIGATORIO.
    
    Sin Gemma 3 corriendo, el agente NO toma decisiones ni ejecuta acciones.
    Gemma 3 es el cerebro — sin cerebro, solo se espera.
    """

    def __init__(self, memory=None):
        self._memory = memory
        self._model = MODEL_NAME
        self._consecutive_failures = 0
        self._gemma_available = False  # se confirma en el primer decide() exitoso

    def decide(self, obs: Observation, recent_decisions: list[dict] = None) -> Decision:
        """
        Analiza la observación y decide la acción.

        Flujo:
          1. Verificar overrides (situaciones críticas que no necesitan LLM)
          2. Construir prompt con observación + historial
          3. Enviar a Gemma 3
          4. Parsear respuesta JSON
          5. Fallback determinista si Gemma falla
        """
        # ─── Overrides: decisiones que no necesitan razonamiento ──────────────
        override = self._check_overrides(obs)
        if override:
            return override

        # ─── Construir mensajes para Gemma 3 ─────────────────────────────────
        user_prompt = self._build_user_prompt(obs, recent_decisions)

        # ─── Intentar Gemma 3 (OBLIGATORIO) ───────────────────────────────────
        try:
            t0 = time.time()
            raw = self._call_ollama(user_prompt)
            latency = int((time.time() - t0) * 1000)

            decision = self._parse_response(raw)
            decision.latency_ms = latency
            decision.raw_response = raw[:500]
            decision.source = "gemma3"
            self._consecutive_failures = 0
            self._gemma_available = True
            return decision

        except Exception as e:
            self._consecutive_failures += 1
            self._gemma_available = False
            # SIN GEMMA 3 → NO ACTUAR. El cerebro está apagado.
            return Decision(
                action="wait",
                confidence=0.0,
                reasoning=f"⛔ GEMMA 3 NO DISPONIBLE — cerebro apagado, no se toman acciones. Error: {str(e)[:100]}",
                source="gemma3_offline",
            )

    def _check_overrides(self, obs: Observation) -> Optional[Decision]:
        """
        Seguridad de rail: SOLO los casos donde la respuesta es
        100% obvia sin necesitar razonamiento del LLM.
        TODO lo demás → Gemma 3.

        ORDEN CRÍTICO: contexto lleno y CDP stress tienen MÁXIMA prioridad
        y deben ejecutarse ANTES de cualquier check de generación activa.
        """

        # 1. VS Code no está corriendo → no hay nada que hacer
        if not obs.vscode_running:
            return Decision(
                action="wait",
                confidence=1.0,
                reasoning="VS Code no está corriendo — esperar.",
                source="override",
            )

        # 2. Contexto lleno (DOM scraping) → acción crítica inmediata.
        # PRIORIDAD MÁXIMA — este check va ANTES que snapshot_has_active_generation
        # porque un contexto lleno mientras hay generación activa sigue siendo
        # una emergencia (VS Code se caerá de todos modos).
        if obs.cdp_context_full:
            return Decision(
                action="stop_and_new_chat",
                confidence=1.0,
                reasoning="Contexto del chat lleno (>600k chars) — abrir nuevo chat inmediatamente.",
                source="override",
            )

        # 3. Generación activa visible → NUNCA interrumpir con heurísticas.
        # Fix 2: este guard sube ANTES de los checks de proxy/timeouts/session.
        # cdp_context_full (check #2) es DOM-confirmado y sí puede interrumpir generación
        # porque un contexto realmente lleno colapsará VS Code de todos modos.
        # Pero proxy_full, consecutive_timeouts y session_too_long son ESTIMACIONES
        # que pueden ser incorrectas — no deben interrumpir trabajo en curso.
        if obs.snapshot_has_active_generation:
            return Decision(
                action="wait",
                confidence=1.0,
                reasoning="Generación activa detectada en el chat — no interrumpir.",
                source="override",
            )

        # 4. Contexto proxy lleno (high-water mark del chat) → emergencia heurística.
        # Señal alternativa cuando CDP scraping no puede medir el contexto real.
        # Solo aplica cuando NO hay generación activa (check #3 ya pasó).
        if obs.cdp_context_proxy_full:
            return Decision(
                action="stop_and_new_chat",
                confidence=0.95,
                reasoning=(
                    f"Contexto proxy lleno ({obs.cdp_context_proxy_bytes:,} bytes peak del chat), "
                    "CDP no puede medir el contexto real — abrir nuevo chat por seguridad."
                ),
                source="override",
            )

        # 5. CDP timeout consecutivo ≥ 3 → VS Code bajo estrés severo → emergencia
        if obs.cdp_consecutive_timeouts >= 3:
            return Decision(
                action="stop_and_new_chat",
                confidence=0.90,
                reasoning=(
                    f"CDP falló {obs.cdp_consecutive_timeouts} veces seguidas (timeout) — "
                    "VS Code está bajo estrés severo probablemente por contexto grande."
                ),
                source="override",
            )

        # 6. Sesión demasiado larga → contexto creció demasiado aunque no sea medible
        if obs.session_too_long:
            return Decision(
                action="stop_and_new_chat",
                confidence=0.85,
                reasoning=(
                    f"Sesión activa por más de {obs.session_age_mins:.0f} min con "
                    f"{obs.continue_count} continues — abrir nuevo chat para evitar contexto lleno."
                ),
                source="override",
            )

        # 7. Actividad reciente (<2 min) sin errores confirmados en el LOG
        # El log_dominant_event es más fiable que snapshot_errors para detectar
        # problemas reales.  Si el chat cambió recientemente y el log no reporta
        # un error concreto, lo más seguro es esperar.
        if (
            obs.secs_since_last_activity < 120
            and obs.log_dominant_event not in ("rate_limited", "hard_error")
        ):
            return Decision(
                action="wait",
                confidence=0.95,
                reasoning=(
                    f"Actividad reciente hace {obs.secs_since_last_activity:.0f}s — "
                    "esperando sin errores confirmados en log."
                ),
                source="override",
            )

        # Todo lo demás → Gemma 3 decide con inteligencia
        return None

    def _build_user_prompt(self, obs: Observation, recent_decisions: list[dict] = None) -> str:
        """Construye el prompt de usuario con observación + historial."""
        parts = [obs.to_prompt_context()]

        if recent_decisions:
            parts.append("\n=== ÚLTIMAS 5 DECISIONES ===")
            for d in recent_decisions[-5:]:
                parts.append(
                    f"  [{d.get('timestamp_human', '?')}] {d.get('action', '?')} "
                    f"(confianza: {d.get('confidence', 0):.1f}) — {d.get('reasoning', '')[:80]}"
                )

        if self._memory:
            lessons = self._memory.get_relevant_lessons(obs.situation_summary, limit=3)
            if lessons:
                parts.append("\n=== LECCIONES APRENDIDAS RELEVANTES ===")
                for lesson in lessons:
                    parts.append(f"  - {lesson}")

        parts.append(
            "\n¿Qué acción debo tomar? Responde SOLO con JSON válido "
            "(decision, confidence, reasoning, wait_before_action_secs)."
        )

        return "\n".join(parts)

    def _call_ollama(self, user_prompt: str) -> str:
        """Llama a Ollama vía HTTP (sin dependencias externas)."""
        payload = json.dumps({
            "model": self._model,
            "messages": [
                {"role": "system", "content": SYSTEM_PROMPT},
                {"role": "user", "content": user_prompt},
            ],
            "stream": False,
            "format": "json",
            "options": {
                "temperature": 0.1,  # casi determinista
                "num_predict": 128,  # respuesta corta — JSON en <100 tokens
                "top_p": 0.9,
            },
        }).encode("utf-8")

        req = urllib.request.Request(
            OLLAMA_CHAT_URL,
            data=payload,
            headers={"Content-Type": "application/json"},
            method="POST",
        )

        with urllib.request.urlopen(req, timeout=INFERENCE_TIMEOUT) as resp:
            result = json.loads(resp.read())

        return result.get("message", {}).get("content", "")

    def _parse_response(self, raw: str) -> Decision:
        """Extrae la decisión del JSON de respuesta de Gemma."""
        try:
            data = json.loads(raw)
        except json.JSONDecodeError:
            # Intentar extraer JSON embebido
            import re
            match = re.search(r'\{[^{}]*"decision"[^{}]*\}', raw, re.DOTALL)
            if match:
                data = json.loads(match.group())
            else:
                raise ValueError(f"No se pudo parsear JSON de la respuesta: {raw[:200]}")

        action = data.get("decision", "wait")
        valid_actions = {
            "wait", "send_continue", "open_new_chat",
            "stop_and_new_chat", "cycle_model", "focus_vscode",
            "restart_monitor",
        }
        if action not in valid_actions:
            action = "wait"

        return Decision(
            action=action,
            confidence=min(1.0, max(0.0, float(data.get("confidence", 0.5)))),
            reasoning=str(data.get("reasoning", ""))[:200],
            wait_before_action_secs=min(300, max(0, int(data.get("wait_before_action_secs", 0)))),
        )

    @property
    def is_gemma_available(self) -> bool:
        """Retorna True si Gemma 3 respondió exitosamente al menos una vez."""
        return self._gemma_available
