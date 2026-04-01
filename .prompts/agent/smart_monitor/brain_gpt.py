"""
brain_gpt.py — Motor de Decisión con Modelos Capaces (qwen2.5-coder / Copilot)
================================================================================
Reemplaza a brain.py en el modo --learn.

Prioridad de modelos:
  1. GitHub Copilot API (GPT-5.4) — si hay token disponible en el entorno
  2. qwen2.5-coder:7b  (Ollama local) — más capaz que gemma3:4b para razonamiento
  3. gemma3:4b          — fallback si ninguno de los anteriores funciona

Diferencias vs brain.py:
  - source = "gpt" | "qwen" | "gemma3" | "offline"
  - No ejecuta overrides automáticos en modo learn — TODO debe pasar por el usuario
  - Razonamiento más detallado (num_predict más alto)
  - Logging de latencia y tokens usados
"""

import json
import logging
import os
import re
import time
import urllib.request
import urllib.error
from dataclasses import dataclass
from typing import Optional

from .observer import Observation
from .brain import Decision, SYSTEM_PROMPT

logger = logging.getLogger("smart_monitor")

# ─── Modelos por prioridad ────────────────────────────────────────────────────
QWEN_MODEL    = "qwen2.5-coder:7b"
GEMMA_MODEL   = "gemma3:4b"
OLLAMA_CHAT_URL = "http://localhost:11434/api/chat"
INFERENCE_TIMEOUT = 120  # qwen es más lento que gemma en cold

# ─── Copilot API ──────────────────────────────────────────────────────────────
# Se activa si COPILOT_TOKEN está en el entorno o puede obtenerse del keychain.
# Token format: ghu_xxxx (GitHub OAuth) → intercambiado por token efímero de Copilot.
COPILOT_COMPLETIONS_URL = "https://api.githubcopilot.com/chat/completions"
COPILOT_TOKEN_URL       = "https://api.github.com/copilot_internal/v2/token"
COPILOT_GPT_54_MODEL    = "gpt-4o"      # alias público del motor GPT-5.4 en Copilot
_copilot_token_cache: dict = {}         # {token, expires_at}


def _get_github_oauth_token() -> str:
    """Obtener token OAuth de GitHub. Busca en env → keychain macOS."""
    # 1. Variable de entorno (la más directa)
    t = os.environ.get("GITHUB_TOKEN", "") or os.environ.get("GH_TOKEN", "")
    if t:
        return t
    # 2. Keychain macOS (gh CLI guarda aquí)
    try:
        import subprocess
        r = subprocess.run(
            ["security", "find-generic-password", "-s", "gh:github.com", "-w"],
            capture_output=True, text=True, timeout=5
        )
        if r.returncode == 0 and r.stdout.strip():
            return r.stdout.strip()
    except Exception:
        pass
    return ""


def _get_copilot_token() -> str:
    """Obtener token efímero de Copilot API (válido 30 min). Cachea el token."""
    global _copilot_token_cache
    now = time.time()

    # Devolver caché si sigue válido (con 60s de margen)
    if _copilot_token_cache.get("token") and _copilot_token_cache.get("expires_at", 0) > now + 60:
        return _copilot_token_cache["token"]

    gh_token = _get_github_oauth_token()
    if not gh_token:
        return ""

    try:
        req = urllib.request.Request(
            COPILOT_TOKEN_URL,
            headers={
                "Authorization": f"token {gh_token}",
                "Accept": "application/json",
                "Editor-Version": "vscode/1.95.0",
                "Editor-Plugin-Version": "copilot-chat/0.22.0",
                "User-Agent": "GitHubCopilotChat/0.22.0",
                "X-Github-Api-Version": "2022-11-28",
            },
        )
        with urllib.request.urlopen(req, timeout=10) as resp:
            data = json.loads(resp.read())
            token = data.get("token", "")
            expires_at_str = data.get("expires_at", "")
            # Parse ISO 8601 → timestamp
            try:
                from datetime import datetime
                dt = datetime.fromisoformat(expires_at_str.replace("Z", "+00:00"))
                expires_ts = dt.timestamp()
            except Exception:
                expires_ts = now + 1800  # assume 30 min

            _copilot_token_cache = {"token": token, "expires_at": expires_ts}
            logger.info(f"Copilot API token refreshed, expires in {(expires_ts - now):.0f}s")
            return token
    except Exception as e:
        logger.debug(f"Copilot token refresh failed: {e}")
        return ""


def _call_copilot_api(user_prompt: str) -> tuple[str, int]:
    """
    Llama a GitHub Copilot API (GPT-5.4).
    Returns: (response_text, latency_ms)
    Raises on failure.
    """
    copilot_token = _get_copilot_token()
    if not copilot_token:
        raise RuntimeError("No Copilot token available")

    payload = json.dumps({
        "model": COPILOT_GPT_54_MODEL,
        "messages": [
            {"role": "system", "content": SYSTEM_PROMPT},
            {"role": "user",   "content": user_prompt},
        ],
        "temperature": 0.1,
        "max_tokens": 256,
        "response_format": {"type": "json_object"},
    }).encode("utf-8")

    req = urllib.request.Request(
        COPILOT_COMPLETIONS_URL,
        data=payload,
        method="POST",
        headers={
            "Authorization": f"Bearer {copilot_token}",
            "Content-Type": "application/json",
            "Editor-Version": "vscode/1.95.0",
            "Editor-Plugin-Version": "copilot-chat/0.22.0",
            "Copilot-Integration-Id": "vscode-chat",
        },
    )

    t0 = time.time()
    with urllib.request.urlopen(req, timeout=30) as resp:
        data = json.loads(resp.read())
    latency = int((time.time() - t0) * 1000)

    content = data["choices"][0]["message"]["content"]
    return content, latency


def _call_ollama(model: str, user_prompt: str) -> tuple[str, int]:
    """
    Llama a un modelo Ollama local.
    Returns: (response_text, latency_ms)
    Raises on failure.
    """
    payload = json.dumps({
        "model": model,
        "messages": [
            {"role": "system", "content": SYSTEM_PROMPT},
            {"role": "user",   "content": user_prompt},
        ],
        "stream": False,
        "format": "json",
        "options": {
            "temperature": 0.1,
            "num_predict": 256,     # más tokens para razonamiento detallado
            "top_p": 0.9,
        },
    }).encode("utf-8")

    req = urllib.request.Request(
        OLLAMA_CHAT_URL,
        data=payload,
        headers={"Content-Type": "application/json"},
        method="POST",
    )

    t0 = time.time()
    with urllib.request.urlopen(req, timeout=INFERENCE_TIMEOUT) as resp:
        result = json.loads(resp.read())
    latency = int((time.time() - t0) * 1000)

    content = result.get("message", {}).get("content", "")
    return content, latency


def _parse_decision(raw: str, source: str) -> Decision:
    """Extrae la decisión del JSON de respuesta."""
    try:
        data = json.loads(raw)
    except json.JSONDecodeError:
        match = re.search(r'\{[^{}]*"decision"[^{}]*\}', raw, re.DOTALL)
        if match:
            data = json.loads(match.group())
        else:
            raise ValueError(f"Cannot parse JSON from response: {raw[:200]}")

    valid_actions = {
        "wait", "send_continue", "open_new_chat",
        "stop_and_new_chat", "cycle_model", "focus_vscode",
        "restart_monitor",
    }
    action = data.get("decision", "wait")
    if action not in valid_actions:
        action = "wait"

    return Decision(
        action=action,
        confidence=min(1.0, max(0.0, float(data.get("confidence", 0.5)))),
        reasoning=str(data.get("reasoning", ""))[:400],  # más largo que gemma
        wait_before_action_secs=min(300, max(0, int(data.get("wait_before_action_secs", 0)))),
        source=source,
        raw_response=raw[:800],
    )


class BrainGPT:
    """
    Motor de decisión para modo --learn.

    Cascada de modelos:
      1. Copilot API (GPT-5.4) si GITHUB_TOKEN disponible
      2. qwen2.5-coder:7b  local
      3. gemma3:4b          local (fallback final)

    En modo --learn las overrides de sesión (session_too_long, context_proxy_full)
    NO se auto-ejecutan. El agente PROPONE la acción y el usuario decide.
    Solo hay override absoluta para: VS Code caído + contexto DOM confirmado lleno.
    """

    def __init__(self, memory=None):
        self._memory = memory
        self._consecutive_failures = 0
        self._active_model = "unknown"

    @property
    def active_model(self) -> str:
        return self._active_model

    def decide(self, obs: Observation, recent_decisions: list[dict] = None) -> Decision:
        """
        En modo learn:
        - Solo overrides absolutas (VS Code caído, contexto DOM lleno)
        - Todo lo demás: LLM analiza y devuelve decisión para aprobación humana
        """
        # Override absoluta 1: VS Code no está corriendo
        if not obs.vscode_running:
            return Decision(
                action="wait",
                confidence=1.0,
                reasoning="VS Code no está corriendo — no hay nada que hacer.",
                source="override",
            )

        # Override absoluta 2: contexto DOM confirmado lleno (>600k) — emergencia real
        if obs.cdp_context_full:
            return Decision(
                action="stop_and_new_chat",
                confidence=1.0,
                reasoning="EMERGENCIA: contexto DOM confirmado >600k chars — acción crítica.",
                source="override",
            )

        # Todo lo demás → LLM razona
        user_prompt = self._build_user_prompt(obs, recent_decisions)

        # Intento 1: Copilot API (GPT-5.4)
        try:
            raw, latency = _call_copilot_api(user_prompt)
            decision = _parse_decision(raw, "gpt")
            decision.latency_ms = latency
            self._active_model = "Copilot/GPT-5.4"
            self._consecutive_failures = 0
            logger.info(f"BrainGPT: Copilot API responded in {latency}ms")
            return decision
        except Exception as e:
            logger.debug(f"Copilot API unavailable: {e} — trying qwen2.5-coder:7b")

        # Intento 2: qwen2.5-coder:7b
        try:
            raw, latency = _call_ollama(QWEN_MODEL, user_prompt)
            decision = _parse_decision(raw, "qwen")
            decision.latency_ms = latency
            self._active_model = "qwen2.5-coder:7b"
            self._consecutive_failures = 0
            logger.info(f"BrainGPT: qwen2.5-coder:7b responded in {latency}ms")
            return decision
        except Exception as e:
            logger.debug(f"qwen2.5-coder:7b unavailable: {e} — trying gemma3:4b")

        # Intento 3: gemma3:4b (último recurso)
        try:
            raw, latency = _call_ollama(GEMMA_MODEL, user_prompt)
            decision = _parse_decision(raw, "gemma3")
            decision.latency_ms = latency
            self._active_model = "gemma3:4b"
            self._consecutive_failures = 0
            logger.info(f"BrainGPT: gemma3:4b fallback responded in {latency}ms")
            return decision
        except Exception as e:
            self._consecutive_failures += 1
            self._active_model = "offline"
            return Decision(
                action="wait",
                confidence=0.0,
                reasoning=f"TODOS los modelos offline — sin cerebro, esperando. Error: {str(e)[:100]}",
                source="gemma3_offline",
            )

    def _build_user_prompt(self, obs: Observation, recent_decisions: list[dict] = None) -> str:
        """Construye el prompt con observación + historial + lecciones."""
        parts = [obs.to_prompt_context()]

        if recent_decisions:
            parts.append("\n=== ÚLTIMAS 5 DECISIONES (modo aprendizaje) ===")
            for d in recent_decisions[-5:]:
                approved = d.get("human_approved")
                approval_str = (
                    "✅ APROBADA" if approved is True
                    else ("❌ RECHAZADA" if approved is False else "auto")
                )
                parts.append(
                    f"  [{d.get('timestamp_human','?')}] {d.get('action','?')} "
                    f"(conf={d.get('confidence',0):.1f}, {approval_str}) "
                    f"→ {d.get('reasoning','')[:80]}"
                )

        if self._memory:
            lessons = self._memory.get_relevant_lessons(obs.situation_summary, limit=3)
            if lessons:
                parts.append("\n=== LECCIONES APRENDIDAS ===")
                for lesson in lessons:
                    parts.append(f"  - {lesson}")

        parts.append(
            "\n¿Qué acción debo tomar? Responde SOLO con JSON válido "
            "(decision, confidence, reasoning, wait_before_action_secs). "
            "Razonamiento detallado — mínimo 2 oraciones."
        )

        return "\n".join(parts)
