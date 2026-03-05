#!/usr/bin/env python3
"""
OKLA Chatbot LLM — Feedback Collection & Processing
=====================================================

Recoge feedback de usuarios (thumbs up/down, ratings, correcciones),
lo almacena, y lo prepara para re-entrenamiento del modelo.

Componentes:
    1. FeedbackCollector  — Recibe y almacena feedback
    2. FeedbackAnalyzer   — Analiza patrones y genera insights
    3. FeedbackExporter   — Exporta datos para re-entrenamiento

Almacenamiento: JSONL local + PostgreSQL (vía ChatbotService)

Uso:
    # Analizar feedback acumulado
    python feedback_system.py analyze --data-dir ./feedback_data

    # Exportar para re-entrenamiento
    python feedback_system.py export \
        --data-dir ./feedback_data \
        --output ./retrain_data/feedback_conversations.jsonl \
        --min-rating 4

    # Generar reporte semanal
    python feedback_system.py report --data-dir ./feedback_data --period weekly
"""

import argparse
import json
import logging
import os
import statistics
from collections import Counter, defaultdict
from datetime import datetime, timedelta
from pathlib import Path
from typing import Any, Optional

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s"
)
logger = logging.getLogger("okla-feedback")


# ============================================================
# DATA MODELS
# ============================================================

class FeedbackEntry:
    """Represents a single piece of user feedback."""

    def __init__(
        self,
        session_id: str,
        message_id: str,
        user_message: str,
        bot_response: str,
        intent: str | None = None,
        confidence: float | None = None,
        rating: int | None = None,          # 1-5 stars
        thumbs: str | None = None,          # "up" or "down"
        correction: str | None = None,      # User-provided better response
        category: str | None = None,        # Error category
        comment: str | None = None,         # Free text feedback
        timestamp: str | None = None,
        metadata: dict | None = None
    ):
        self.session_id = session_id
        self.message_id = message_id
        self.user_message = user_message
        self.bot_response = bot_response
        self.intent = intent
        self.confidence = confidence
        self.rating = rating
        self.thumbs = thumbs
        self.correction = correction
        self.category = category
        self.comment = comment
        self.timestamp = timestamp or datetime.now().isoformat()
        self.metadata = metadata or {}

    def to_dict(self) -> dict:
        return {
            "session_id": self.session_id,
            "message_id": self.message_id,
            "user_message": self.user_message,
            "bot_response": self.bot_response,
            "intent": self.intent,
            "confidence": self.confidence,
            "rating": self.rating,
            "thumbs": self.thumbs,
            "correction": self.correction,
            "category": self.category,
            "comment": self.comment,
            "timestamp": self.timestamp,
            "metadata": self.metadata
        }

    @classmethod
    def from_dict(cls, data: dict) -> "FeedbackEntry":
        return cls(**{k: v for k, v in data.items() if k in cls.__init__.__code__.co_varnames})


# Feedback categories
FEEDBACK_CATEGORIES = {
    "wrong_intent": "Intent clasificado incorrectamente",
    "wrong_info": "Información incorrecta sobre vehículo/dealer",
    "wrong_price": "Precio o financiamiento incorrecto",
    "wrong_language": "Respuesta en idioma incorrecto o no natural",
    "too_generic": "Respuesta demasiado genérica",
    "hallucination": "Información inventada",
    "incomplete": "Respuesta incompleta",
    "rude_tone": "Tono inadecuado",
    "privacy_leak": "Filtración de datos sensibles",
    "off_topic": "Fuera de tema",
    "excellent": "Respuesta excelente",
    "other": "Otro",
}


# ============================================================
# FEEDBACK COLLECTOR
# ============================================================

class FeedbackCollector:
    """Collects and stores feedback entries to JSONL files."""

    def __init__(self, data_dir: str):
        self.data_dir = Path(data_dir)
        self.data_dir.mkdir(parents=True, exist_ok=True)

    def _get_file_path(self, date: datetime | None = None) -> Path:
        """Get JSONL file path for the given date (daily rotation)."""
        d = date or datetime.now()
        return self.data_dir / f"feedback_{d.strftime('%Y%m%d')}.jsonl"

    def save(self, entry: FeedbackEntry) -> str:
        """Save a feedback entry. Returns file path."""
        path = self._get_file_path()
        with open(path, "a", encoding="utf-8") as f:
            f.write(json.dumps(entry.to_dict(), ensure_ascii=False) + "\n")
        logger.debug(f"Saved feedback to {path}")
        return str(path)

    def load_all(self, days_back: int = 30) -> list[FeedbackEntry]:
        """Load all feedback entries from the last N days."""
        entries = []
        cutoff = datetime.now() - timedelta(days=days_back)

        for path in sorted(self.data_dir.glob("feedback_*.jsonl")):
            # Parse date from filename
            try:
                date_str = path.stem.replace("feedback_", "")
                file_date = datetime.strptime(date_str, "%Y%m%d")
                if file_date < cutoff:
                    continue
            except ValueError:
                continue

            with open(path, "r", encoding="utf-8") as f:
                for line in f:
                    line = line.strip()
                    if line:
                        try:
                            entries.append(FeedbackEntry.from_dict(json.loads(line)))
                        except (json.JSONDecodeError, TypeError) as e:
                            logger.warning(f"Skipping malformed feedback entry: {e}")

        logger.info(f"Loaded {len(entries)} feedback entries from last {days_back} days")
        return entries

    def get_stats(self, days_back: int = 7) -> dict:
        """Quick statistics summary."""
        entries = self.load_all(days_back)
        if not entries:
            return {"total": 0, "message": "No feedback data found"}

        thumbs_up = sum(1 for e in entries if e.thumbs == "up")
        thumbs_down = sum(1 for e in entries if e.thumbs == "down")
        ratings = [e.rating for e in entries if e.rating is not None]
        categories = Counter(e.category for e in entries if e.category)
        intents = Counter(e.intent for e in entries if e.intent)
        corrections = sum(1 for e in entries if e.correction)

        return {
            "total_entries": len(entries),
            "period_days": days_back,
            "thumbs_up": thumbs_up,
            "thumbs_down": thumbs_down,
            "satisfaction_rate": round(thumbs_up / (thumbs_up + thumbs_down) * 100, 2) if (thumbs_up + thumbs_down) > 0 else None,
            "avg_rating": round(statistics.mean(ratings), 2) if ratings else None,
            "rating_distribution": Counter(ratings) if ratings else {},
            "corrections_submitted": corrections,
            "top_categories": categories.most_common(10),
            "top_intents_with_issues": intents.most_common(10),
        }


# ============================================================
# FEEDBACK ANALYZER
# ============================================================

class FeedbackAnalyzer:
    """Analyzes feedback patterns to identify model weaknesses."""

    def __init__(self, entries: list[FeedbackEntry]):
        self.entries = entries

    def identify_weak_intents(self, min_feedback: int = 3) -> list[dict]:
        """Find intents with consistently negative feedback."""
        intent_data = defaultdict(lambda: {"up": 0, "down": 0, "ratings": [], "entries": []})

        for e in self.entries:
            if not e.intent:
                continue
            if e.thumbs == "up":
                intent_data[e.intent]["up"] += 1
            elif e.thumbs == "down":
                intent_data[e.intent]["down"] += 1
            if e.rating:
                intent_data[e.intent]["ratings"].append(e.rating)
            intent_data[e.intent]["entries"].append(e)

        weak_intents = []
        for intent, data in intent_data.items():
            total = data["up"] + data["down"]
            if total < min_feedback:
                continue

            neg_rate = data["down"] / total if total > 0 else 0
            avg_rating = statistics.mean(data["ratings"]) if data["ratings"] else 0

            if neg_rate >= 0.3 or avg_rating < 3.0:
                weak_intents.append({
                    "intent": intent,
                    "negative_rate": round(neg_rate * 100, 1),
                    "avg_rating": round(avg_rating, 2),
                    "total_feedback": total,
                    "sample_issues": [
                        {
                            "user_msg": e.user_message[:100],
                            "category": e.category,
                            "correction": e.correction[:100] if e.correction else None
                        }
                        for e in data["entries"][:5]
                        if e.thumbs == "down" or (e.rating and e.rating <= 2)
                    ]
                })

        return sorted(weak_intents, key=lambda x: x["negative_rate"], reverse=True)

    def identify_hallucination_patterns(self) -> list[dict]:
        """Find patterns where model generates incorrect information."""
        hallucinations = [
            e for e in self.entries
            if e.category == "hallucination" or e.category == "wrong_info"
        ]

        patterns = defaultdict(list)
        for e in hallucinations:
            key = e.intent or "unknown"
            patterns[key].append({
                "user_message": e.user_message[:150],
                "bot_response": e.bot_response[:200],
                "correction": e.correction[:200] if e.correction else None,
            })

        return [
            {"intent": k, "count": len(v), "examples": v[:3]}
            for k, v in sorted(patterns.items(), key=lambda x: len(x[1]), reverse=True)
        ]

    def identify_missing_capabilities(self) -> list[dict]:
        """Find questions the model can't answer (fallbacks, low confidence)."""
        fallbacks = [
            e for e in self.entries
            if (e.confidence and e.confidence < 0.5)
            or e.category == "incomplete"
            or e.category == "too_generic"
        ]

        topic_clusters = defaultdict(list)
        for e in fallbacks:
            # Simple clustering by keywords
            msg_lower = e.user_message.lower()
            if any(w in msg_lower for w in ["precio", "costo", "cuánto", "financ"]):
                topic_clusters["pricing_financing"].append(e.user_message)
            elif any(w in msg_lower for w in ["seguro", "cobertura", "póliza"]):
                topic_clusters["insurance"].append(e.user_message)
            elif any(w in msg_lower for w in ["traspaso", "documento", "placa", "marbete"]):
                topic_clusters["documentation"].append(e.user_message)
            elif any(w in msg_lower for w in ["taller", "mantenimiento", "reparación"]):
                topic_clusters["maintenance"].append(e.user_message)
            elif any(w in msg_lower for w in ["comparar", "versus", "mejor"]):
                topic_clusters["comparison"].append(e.user_message)
            else:
                topic_clusters["other"].append(e.user_message)

        return [
            {"topic": k, "count": len(v), "sample_questions": v[:5]}
            for k, v in sorted(topic_clusters.items(), key=lambda x: len(x[1]), reverse=True)
        ]

    def generate_improvement_priorities(self) -> list[dict]:
        """Generate prioritized list of model improvements needed."""
        priorities = []

        # Weak intents
        weak = self.identify_weak_intents()
        for w in weak[:5]:
            priorities.append({
                "priority": "HIGH" if w["negative_rate"] > 50 else "MEDIUM",
                "type": "intent_improvement",
                "description": f"Mejorar intent '{w['intent']}' — {w['negative_rate']}% negativo",
                "action": f"Agregar {max(10, w['total_feedback'] * 2)} ejemplos de entrenamiento",
                "data": w
            })

        # Hallucinations
        hallucinations = self.identify_hallucination_patterns()
        for h in hallucinations[:3]:
            priorities.append({
                "priority": "CRITICAL",
                "type": "hallucination_fix",
                "description": f"Alucinaciones en '{h['intent']}' — {h['count']} reportes",
                "action": "Agregar ejemplos correctivos y verificar datos de entrenamiento",
                "data": h
            })

        # Missing capabilities
        missing = self.identify_missing_capabilities()
        for m in missing[:3]:
            if m["count"] >= 3:
                priorities.append({
                    "priority": "MEDIUM",
                    "type": "new_capability",
                    "description": f"Capacidad faltante: '{m['topic']}' — {m['count']} preguntas sin respuesta",
                    "action": "Crear nuevos ejemplos de entrenamiento para este tema",
                    "data": m
                })

        # Sort by priority
        priority_order = {"CRITICAL": 0, "HIGH": 1, "MEDIUM": 2, "LOW": 3}
        return sorted(priorities, key=lambda x: priority_order.get(x["priority"], 99))


# ============================================================
# FEEDBACK EXPORTER (for re-training)
# ============================================================

class FeedbackExporter:
    """Export feedback data formatted for model re-training."""

    def __init__(self, entries: list[FeedbackEntry]):
        self.entries = entries

    def export_positive_examples(self, output_path: str, min_rating: int = 4) -> int:
        """Export highly-rated conversations as positive training examples."""
        count = 0
        with open(output_path, "w", encoding="utf-8") as f:
            for e in self.entries:
                # Include if: thumbs up, or rating >= min_rating
                if e.thumbs == "up" or (e.rating and e.rating >= min_rating):
                    example = {
                        "messages": [
                            {"role": "user", "content": e.user_message},
                            {"role": "assistant", "content": e.bot_response}
                        ],
                        "metadata": {
                            "source": "user_feedback_positive",
                            "intent": e.intent,
                            "rating": e.rating,
                            "thumbs": e.thumbs,
                            "timestamp": e.timestamp
                        }
                    }
                    f.write(json.dumps(example, ensure_ascii=False) + "\n")
                    count += 1

        logger.info(f"Exported {count} positive examples to {output_path}")
        return count

    def export_corrections(self, output_path: str) -> int:
        """Export user corrections as improved training examples."""
        count = 0
        with open(output_path, "w", encoding="utf-8") as f:
            for e in self.entries:
                if e.correction:
                    example = {
                        "messages": [
                            {"role": "user", "content": e.user_message},
                            {"role": "assistant", "content": e.correction}  # Use corrected response
                        ],
                        "metadata": {
                            "source": "user_correction",
                            "original_response": e.bot_response,
                            "intent": e.intent,
                            "category": e.category,
                            "timestamp": e.timestamp
                        }
                    }
                    f.write(json.dumps(example, ensure_ascii=False) + "\n")
                    count += 1

        logger.info(f"Exported {count} corrections to {output_path}")
        return count

    def export_negative_examples(self, output_path: str) -> int:
        """Export negative feedback for analysis (NOT for direct training)."""
        count = 0
        with open(output_path, "w", encoding="utf-8") as f:
            for e in self.entries:
                if e.thumbs == "down" or (e.rating and e.rating <= 2):
                    example = {
                        "user_message": e.user_message,
                        "bad_response": e.bot_response,
                        "category": e.category,
                        "comment": e.comment,
                        "intent": e.intent,
                        "confidence": e.confidence,
                        "timestamp": e.timestamp
                    }
                    f.write(json.dumps(example, ensure_ascii=False) + "\n")
                    count += 1

        logger.info(f"Exported {count} negative examples to {output_path}")
        return count


# ============================================================
# REPORT GENERATION
# ============================================================

def generate_feedback_report(collector: FeedbackCollector, output_dir: str, days: int = 7) -> str:
    """Generate a comprehensive feedback report."""
    entries = collector.load_all(days_back=days)
    if not entries:
        logger.warning("No feedback data to analyze")
        return ""

    analyzer = FeedbackAnalyzer(entries)
    stats = collector.get_stats(days)
    priorities = analyzer.generate_improvement_priorities()
    weak_intents = analyzer.identify_weak_intents()

    os.makedirs(output_dir, exist_ok=True)
    timestamp = datetime.now().strftime("%Y%m%d")
    md_path = os.path.join(output_dir, f"feedback_report_{timestamp}.md")

    with open(md_path, "w", encoding="utf-8") as f:
        f.write("# 📝 OKLA Chatbot — Reporte de Feedback\n\n")
        f.write(f"**Período:** Últimos {days} días\n")
        f.write(f"**Generado:** {datetime.now().strftime('%Y-%m-%d %H:%M')}\n\n")

        f.write("## 📊 Métricas Generales\n\n")
        f.write(f"| Métrica | Valor |\n")
        f.write(f"|---------|-------|\n")
        f.write(f"| Total feedback | {stats['total_entries']} |\n")
        f.write(f"| 👍 Thumbs Up | {stats['thumbs_up']} |\n")
        f.write(f"| 👎 Thumbs Down | {stats['thumbs_down']} |\n")
        f.write(f"| Satisfacción | {stats.get('satisfaction_rate', 'N/A')}% |\n")
        f.write(f"| Rating promedio | {stats.get('avg_rating', 'N/A')}/5 |\n")
        f.write(f"| Correcciones | {stats['corrections_submitted']} |\n\n")

        f.write("## 🎯 Prioridades de Mejora\n\n")
        for i, p in enumerate(priorities, 1):
            icon = "🔴" if p["priority"] == "CRITICAL" else "🟡" if p["priority"] == "HIGH" else "🟢"
            f.write(f"{i}. {icon} **[{p['priority']}]** {p['description']}\n")
            f.write(f"   - Acción: {p['action']}\n\n")

        f.write("## 📉 Intents Débiles\n\n")
        if weak_intents:
            f.write("| Intent | % Negativo | Rating | Total |\n")
            f.write("|--------|------------|--------|-------|\n")
            for w in weak_intents:
                f.write(f"| {w['intent']} | {w['negative_rate']}% | {w['avg_rating']} | {w['total_feedback']} |\n")
        else:
            f.write("✅ No se identificaron intents con problemas significativos.\n")

        f.write("\n## 🔄 Categorías de Errores\n\n")
        if stats["top_categories"]:
            f.write("| Categoría | Count |\n")
            f.write("|-----------|-------|\n")
            for cat, count in stats["top_categories"]:
                desc = FEEDBACK_CATEGORIES.get(cat, cat)
                f.write(f"| {desc} | {count} |\n")

    logger.info(f"Feedback report saved to {md_path}")
    return md_path


# ============================================================
# CLI
# ============================================================

def main():
    parser = argparse.ArgumentParser(description="OKLA Feedback System")
    subparsers = parser.add_subparsers(dest="command", required=True)

    # Analyze
    analyze_p = subparsers.add_parser("analyze", help="Analyze feedback data")
    analyze_p.add_argument("--data-dir", required=True)
    analyze_p.add_argument("--days", type=int, default=30)

    # Export
    export_p = subparsers.add_parser("export", help="Export for re-training")
    export_p.add_argument("--data-dir", required=True)
    export_p.add_argument("--output", required=True, help="Output JSONL path")
    export_p.add_argument("--min-rating", type=int, default=4)
    export_p.add_argument("--days", type=int, default=30)

    # Report
    report_p = subparsers.add_parser("report", help="Generate feedback report")
    report_p.add_argument("--data-dir", required=True)
    report_p.add_argument("--output-dir", default="./reports")
    report_p.add_argument("--period", choices=["daily", "weekly", "monthly"], default="weekly")

    args = parser.parse_args()

    if args.command == "analyze":
        collector = FeedbackCollector(args.data_dir)
        stats = collector.get_stats(args.days)
        print(json.dumps(stats, indent=2, ensure_ascii=False, default=str))

        entries = collector.load_all(args.days)
        if entries:
            analyzer = FeedbackAnalyzer(entries)
            priorities = analyzer.generate_improvement_priorities()
            print(f"\n🎯 Prioridades de mejora ({len(priorities)}):")
            for p in priorities:
                print(f"  [{p['priority']}] {p['description']}")

    elif args.command == "export":
        collector = FeedbackCollector(args.data_dir)
        entries = collector.load_all(args.days)
        exporter = FeedbackExporter(entries)

        base = Path(args.output)
        base.parent.mkdir(parents=True, exist_ok=True)

        pos = exporter.export_positive_examples(
            str(base.with_name(f"{base.stem}_positive.jsonl")),
            min_rating=args.min_rating
        )
        corr = exporter.export_corrections(
            str(base.with_name(f"{base.stem}_corrections.jsonl"))
        )
        neg = exporter.export_negative_examples(
            str(base.with_name(f"{base.stem}_negative.jsonl"))
        )
        print(f"\n✅ Exported: {pos} positive, {corr} corrections, {neg} negative examples")

    elif args.command == "report":
        collector = FeedbackCollector(args.data_dir)
        days = {"daily": 1, "weekly": 7, "monthly": 30}[args.period]
        path = generate_feedback_report(collector, args.output_dir, days)
        if path:
            print(f"✅ Report: {path}")


if __name__ == "__main__":
    main()
