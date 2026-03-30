"""
memory.py — Sistema de Memoria y Aprendizaje con SQLite
========================================================
El agente aprende de cada ciclo observación → decisión → resultado.

Componentes:
  1. episodes — Cada ciclo completo: observación + decisión + resultado
  2. lessons  — Reglas aprendidas: "cuando X sucede, Y funciona/no funciona"
  3. patterns — Patrones estadísticos: "rate_limit ocurre 3x más entre 2-4pm"

Aprendizaje:
  - Después de cada acción, el feedback loop registra si funcionó o no.
  - Cada N episodios, se ejecuta un "reflection" con Gemma 3 para extraer
    lecciones de los episodios recientes.
  - Las lecciones se inyectan en futuros prompts para mejorar decisiones.

SQLite es ideal: cero dependencias, persistente, queries rápidos,
soporte nativo en Python.
"""

import json
import sqlite3
import time
from datetime import datetime
from pathlib import Path
from typing import Optional

REPO_ROOT = Path(__file__).parent.parent.parent.parent
DB_PATH   = REPO_ROOT / ".prompts" / "agent" / "smart_monitor" / "memory.db"

SCHEMA = """
CREATE TABLE IF NOT EXISTS episodes (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    timestamp REAL NOT NULL,
    timestamp_human TEXT NOT NULL,

    -- Observación (resumen compacto)
    situation_summary TEXT NOT NULL,
    snapshot_age_secs REAL,
    snapshot_body_changed INTEGER,
    snapshot_errors TEXT,  -- JSON array
    secs_since_last_activity REAL,
    log_dominant_event TEXT,
    cdp_context_full INTEGER,
    post_action_active INTEGER,

    -- Decisión
    action TEXT NOT NULL,
    confidence REAL,
    reasoning TEXT,
    source TEXT,  -- "gemma3" | "fallback" | "override"
    latency_ms INTEGER,

    -- Resultado (se actualiza después)
    outcome TEXT DEFAULT 'pending',  -- "resolved" | "no_effect" | "escalated" | "pending"
    outcome_secs REAL DEFAULT 0.0,   -- tiempo hasta resolución
    outcome_notes TEXT DEFAULT '',

    -- Para búsqueda
    tags TEXT DEFAULT ''  -- comma-separated tags
);

CREATE TABLE IF NOT EXISTS lessons (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    created_at REAL NOT NULL,
    updated_at REAL NOT NULL,
    lesson TEXT NOT NULL,  -- "Cuando hay rate limit, ciclar modelo funciona 80% de las veces"
    category TEXT NOT NULL,  -- "rate_limit" | "stall" | "error" | "general"
    success_count INTEGER DEFAULT 0,
    failure_count INTEGER DEFAULT 0,
    confidence REAL DEFAULT 0.5,
    active INTEGER DEFAULT 1
);

CREATE TABLE IF NOT EXISTS patterns (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    detected_at REAL NOT NULL,
    pattern_type TEXT NOT NULL,  -- "time_correlation" | "sequence" | "frequency"
    description TEXT NOT NULL,
    data TEXT DEFAULT '',  -- JSON con datos del patrón
    relevance REAL DEFAULT 0.5
);

CREATE INDEX IF NOT EXISTS idx_episodes_timestamp ON episodes(timestamp);
CREATE INDEX IF NOT EXISTS idx_episodes_action ON episodes(action);
CREATE INDEX IF NOT EXISTS idx_episodes_outcome ON episodes(outcome);
CREATE INDEX IF NOT EXISTS idx_lessons_category ON lessons(category);
"""


class Memory:
    """Memoria persistente con SQLite + aprendizaje acumulativo."""

    def __init__(self, db_path: Path = DB_PATH):
        self._db_path = db_path
        db_path.parent.mkdir(parents=True, exist_ok=True)
        self._conn = sqlite3.connect(str(db_path), timeout=10)
        self._conn.row_factory = sqlite3.Row
        self._conn.executescript(SCHEMA)
        self._conn.commit()

    def close(self):
        self._conn.close()

    # ─── Episodes ──────────────────────────────────────────────────────────────

    def record_episode(self, observation_dict: dict, decision_dict: dict) -> int:
        """Registra un episodio (observación + decisión). Retorna el ID."""
        now = time.time()
        cur = self._conn.execute(
            """INSERT INTO episodes (
                timestamp, timestamp_human,
                situation_summary, snapshot_age_secs, snapshot_body_changed,
                snapshot_errors, secs_since_last_activity, log_dominant_event,
                cdp_context_full, post_action_active,
                action, confidence, reasoning, source, latency_ms,
                tags
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
            (
                now,
                datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
                observation_dict.get("situation_summary", ""),
                observation_dict.get("snapshot_age_secs", 0),
                int(observation_dict.get("snapshot_body_changed", False)),
                json.dumps(observation_dict.get("snapshot_errors", [])),
                observation_dict.get("secs_since_last_activity", 0),
                observation_dict.get("log_dominant_event", "unknown"),
                int(observation_dict.get("cdp_context_full", False)),
                int(observation_dict.get("post_action_active", False)),

                decision_dict.get("action", "wait"),
                decision_dict.get("confidence", 0),
                decision_dict.get("reasoning", ""),
                decision_dict.get("source", "unknown"),
                decision_dict.get("latency_ms", 0),

                self._generate_tags(observation_dict, decision_dict),
            ),
        )
        self._conn.commit()
        return cur.lastrowid

    def update_outcome(self, episode_id: int, outcome: str, secs: float = 0, notes: str = "") -> None:
        """Actualiza el resultado de un episodio."""
        self._conn.execute(
            "UPDATE episodes SET outcome=?, outcome_secs=?, outcome_notes=? WHERE id=?",
            (outcome, secs, notes, episode_id),
        )
        self._conn.commit()

    def get_recent_decisions(self, limit: int = 10) -> list[dict]:
        """Retorna las últimas N decisiones (para contexto del LLM)."""
        rows = self._conn.execute(
            """SELECT timestamp_human, action, confidence, reasoning, source, outcome
               FROM episodes ORDER BY id DESC LIMIT ?""",
            (limit,),
        ).fetchall()
        return [dict(r) for r in reversed(rows)]

    def get_pending_episode(self) -> Optional[dict]:
        """Retorna el último episodio con outcome='pending' que tenga acción != wait."""
        row = self._conn.execute(
            """SELECT id, timestamp, action, confidence
               FROM episodes
               WHERE outcome='pending' AND action != 'wait'
               ORDER BY id DESC LIMIT 1"""
        ).fetchone()
        return dict(row) if row else None

    # ─── Lessons ────────────────────────────────────────────────────────────────

    def get_relevant_lessons(self, situation: str, limit: int = 5) -> list[str]:
        """Busca lecciones relevantes basándose en keywords del situation summary."""
        keywords = self._extract_keywords(situation)
        if not keywords:
            # Si no hay keywords, retornar las más confiables
            rows = self._conn.execute(
                "SELECT lesson FROM lessons WHERE active=1 ORDER BY confidence DESC LIMIT ?",
                (limit,),
            ).fetchall()
            return [r["lesson"] for r in rows]

        # Búsqueda por keywords en la lección o categoría
        conditions = " OR ".join(
            ["lesson LIKE ? OR category LIKE ?" for _ in keywords]
        )
        params = []
        for kw in keywords:
            params.extend([f"%{kw}%", f"%{kw}%"])
        params.append(limit)

        rows = self._conn.execute(
            f"""SELECT lesson FROM lessons
                WHERE active=1 AND ({conditions})
                ORDER BY confidence DESC LIMIT ?""",
            params,
        ).fetchall()
        return [r["lesson"] for r in rows]

    def add_lesson(self, lesson: str, category: str, confidence: float = 0.5) -> int:
        """Agrega una lección nueva."""
        now = time.time()
        cur = self._conn.execute(
            """INSERT INTO lessons (created_at, updated_at, lesson, category, confidence)
               VALUES (?, ?, ?, ?, ?)""",
            (now, now, lesson, category, confidence),
        )
        self._conn.commit()
        return cur.lastrowid

    def reinforce_lesson(self, lesson_id: int, success: bool) -> None:
        """Refuerza o penaliza una lección según resultado."""
        col = "success_count" if success else "failure_count"
        self._conn.execute(
            f"""UPDATE lessons SET
                {col} = {col} + 1,
                confidence = CAST(success_count AS REAL) / MAX(1, success_count + failure_count),
                updated_at = ?
                WHERE id = ?""",
            (time.time(), lesson_id),
        )
        self._conn.commit()

    def get_all_lessons(self) -> list[dict]:
        """Retorna todas las lecciones activas."""
        rows = self._conn.execute(
            "SELECT * FROM lessons WHERE active=1 ORDER BY confidence DESC"
        ).fetchall()
        return [dict(r) for r in rows]

    # ─── Patterns ───────────────────────────────────────────────────────────────

    def record_pattern(self, pattern_type: str, description: str, data: dict = None) -> int:
        """Registra un patrón detectado."""
        cur = self._conn.execute(
            "INSERT INTO patterns (detected_at, pattern_type, description, data) VALUES (?, ?, ?, ?)",
            (time.time(), pattern_type, description, json.dumps(data or {})),
        )
        self._conn.commit()
        return cur.lastrowid

    # ─── Statistics ──────────────────────────────────────────────────────────────

    def get_action_stats(self, hours: float = 24) -> dict:
        """Estadísticas de acciones en las últimas N horas."""
        since = time.time() - (hours * 3600)
        rows = self._conn.execute(
            """SELECT action, outcome, COUNT(*) as cnt
               FROM episodes WHERE timestamp > ?
               GROUP BY action, outcome""",
            (since,),
        ).fetchall()

        stats = {}
        for r in rows:
            action = r["action"]
            if action not in stats:
                stats[action] = {"total": 0, "resolved": 0, "no_effect": 0, "escalated": 0, "pending": 0}
            stats[action]["total"] += r["cnt"]
            outcome = r["outcome"]
            if outcome in stats[action]:
                stats[action][outcome] += r["cnt"]
        return stats

    def get_episode_count(self) -> int:
        """Total de episodios registrados."""
        row = self._conn.execute("SELECT COUNT(*) as c FROM episodes").fetchone()
        return row["c"]

    def get_lesson_count(self) -> int:
        """Total de lecciones activas."""
        row = self._conn.execute("SELECT COUNT(*) as c FROM lessons WHERE active=1").fetchone()
        return row["c"]

    # ─── Reflection (llamado por feedback_loop) ─────────────────────────────────

    def get_episodes_for_reflection(self, limit: int = 20) -> list[dict]:
        """Retorna episodios recientes con outcome para reflexión."""
        rows = self._conn.execute(
            """SELECT situation_summary, action, outcome, outcome_notes, reasoning, source
               FROM episodes
               WHERE outcome != 'pending'
               ORDER BY id DESC LIMIT ?""",
            (limit,),
        ).fetchall()
        return [dict(r) for r in rows]

    # ─── Private helpers ─────────────────────────────────────────────────────────

    def _generate_tags(self, obs: dict, dec: dict) -> str:
        """Genera tags para búsqueda rápida."""
        tags = []
        if obs.get("snapshot_errors"):
            tags.extend(obs["snapshot_errors"])
        if obs.get("log_dominant_event", "unknown") != "unknown":
            tags.append(obs["log_dominant_event"])
        tags.append(dec.get("action", "wait"))
        tags.append(dec.get("source", "unknown"))
        return ",".join(tags)

    def _extract_keywords(self, text: str) -> list[str]:
        """Extrae keywords útiles de un texto."""
        important = [
            "rate_limit", "rate limit", "hard_error", "error", "stall",
            "inactivo", "terminó", "contexto lleno", "foco", "cancelled",
            "footer", "modelo", "nuevo chat", "continuar",
        ]
        found = []
        text_lower = text.lower()
        for kw in important:
            if kw in text_lower:
                found.append(kw.replace(" ", "_"))
        return found[:5]  # máx 5 keywords


def seed_initial_lessons(memory: Memory) -> None:
    """
    Semilla de lecciones iniciales basadas en la experiencia acumulada
    del monitor v7 + catálogo completo de errores de GitHub Copilot.
    Estas lecciones se refinan con el tiempo por el feedback loop.
    """
    if memory.get_lesson_count() > 0:
        return  # ya tiene lecciones

    initial = [
        # ── Rate Limit ────────────────────────────────────────────────────────
        ("Cuando hay rate limit (429/quota exhausted/RateLimitError), ciclar modelo INMEDIATAMENTE es la acción correcta.", "rate_limit", 0.95),
        ("Después del rate limit esperar al menos 600 segundos antes de reintentar con el mismo modelo.", "rate_limit", 0.90),
        ("Tras cycle_model, enviar 'continuar' para que el agente retome con el nuevo modelo.", "rate_limit", 0.85),
        ("Los rate limits son más frecuentes durante horarios pico (2pm-6pm hora del servidor).", "rate_limit", 0.60),

        # ── Hard Errors ───────────────────────────────────────────────────────
        ("Error 500/502/503/overloaded: esperar 60s y enviar send_continue. Si falla 3 veces, open_new_chat.", "error", 0.85),
        ("Errores 500 temporales del servidor se resuelven solos en 1-3 minutos con send_continue.", "error", 0.75),
        ("Si hard_error persiste después de 3 send_continue, cambiar de modelo al abrir nuevo chat.", "error", 0.80),

        # ── Tool Validation Error ─────────────────────────────────────────────
        ("ToolValidationError significa que el schema de una herramienta MCP es inválido. Requiere fix manual. No actuar automáticamente.", "error", 0.90),
        ("Los tool validation errors no se resuelven enviando 'continuar'. Notificar al usuario.", "error", 0.85),

        # ── Request Cancelado ─────────────────────────────────────────────────
        ("Un request cancelado puede ser del usuario o del agente. Observar 5 min antes de actuar.", "cancelled", 0.70),
        ("Si el chat está cancelado y lleva >5 min sin actividad, send_continue reanuda el trabajo.", "cancelled", 0.75),

        # ── Contexto Lleno ────────────────────────────────────────────────────
        ("Si el contexto supera 600k chars, el modelo se congela. stop_and_new_chat INMEDIATO.", "context_full", 0.98),
        ("No esperar feedback del usuario cuando el contexto está lleno. Actuar de inmediato.", "context_full", 0.95),

        # ── Stall / Inactividad ───────────────────────────────────────────────
        ("Si el chat lleva >15 min sin cambios y no hay errores, enviar 'continuar' primero.", "stall", 0.85),
        ("Si el agente terminó (footer del modelo visible sin %), abrir nuevo chat para siguiente tarea.", "stall", 0.90),
        ("Stall >20 min sin respuesta a send_continue → open_new_chat con nuevo modelo.", "stall", 0.85),
        ("Las herramientas lentas (dotnet build, npm install) pueden tomar >5 min sin mostrar %. Esperar.", "stall", 0.75),
        ("'Compacting conversation...' puede tomar 30-90s. No intervenir durante compactación.", "stall", 0.90),

        # ── Generación Activa ─────────────────────────────────────────────────
        ("NUNCA enviar continuar si el agente está generando (% visible, herramienta corriendo).", "general", 0.98),
        ("'Preparing...', 'Thinking...', 'Running <herramienta>', 'Reading', 'Writing' son señales de trabajo activo.", "general", 0.95),
        ("Las intervenciones durante generación activa interrumpen el flujo y confunden al agente.", "general", 0.95),

        # ── Post-Acción ───────────────────────────────────────────────────────
        ("Después de enviar cualquier acción, esperar 2 minutos verificando normalización antes de actuar de nuevo.", "general", 0.90),
        ("Si la acción no tuvo efecto en 2 min, reintentar UNA VEZ. Si falla de nuevo, escalar.", "general", 0.85),
        ("Escalar si: rate_limit → hard_error → cancelled persisten después de los retries máximos.", "general", 0.80),

        # ── DOM/CDP ───────────────────────────────────────────────────────────
        ("El botón + de nuevo chat tiene selector: a[aria-label='New Chat (⌘N)']:not([aria-haspopup]). Click via page.mouse.click().", "dom_cdp", 0.95),
        ("El botón Stop tiene selector: button[aria-label='Stop Response']. Solo visible durante generación.", "dom_cdp", 0.95),
        ("Para navegar a un chat por nombre: JS el.click() en monaco-list-row[data-index='N'] — más confiable que coordenadas.", "dom_cdp", 0.90),
        ("El input del chat: .interactive-input-part .monaco-editor textarea. Focus con page.mouse.click(x, y).", "dom_cdp", 0.90),
        ("Los menús contextuales (aria-haspopup) NO se abren via CDP. Usar Command Palette via keyboard.", "dom_cdp", 0.95),

        # ── Modelos ───────────────────────────────────────────────────────────
        ("Preferir GPT-5.4 · Xhigh como modelo primario para máxima capacidad de generación.", "models", 0.85),
        ("Claude Sonnet 4.6 · High es el mejor fallback de Anthropic cuando GPT-5.4 tiene rate limit.", "models", 0.80),
        ("Pool de rotación recomendado: GPT-5.4 Xhigh → Claude Sonnet 4.6 High → GPT-4.1 → o3.", "models", 0.75),
        ("Modelos 3x (Opus, o4-mini High) usar solo como último recurso — consumen más cuota.", "models", 0.70),

        # ── VS Code ───────────────────────────────────────────────────────────
        ("VS Code debe tener el foco de aplicación para que CDP y AppleScript funcionen.", "general", 0.85),
        ("focus_vscode antes de cualquier operación de teclado para evitar que las teclas vayan al lugar equivocado.", "general", 0.80),
    ]

    for lesson, cat, conf in initial:
        memory.add_lesson(lesson, cat, conf)
