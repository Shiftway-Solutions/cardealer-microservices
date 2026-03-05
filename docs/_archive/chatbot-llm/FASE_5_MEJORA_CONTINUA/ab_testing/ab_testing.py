#!/usr/bin/env python3
"""
OKLA Chatbot LLM — A/B Testing Framework
==========================================

Framework para comparar dos versiones del modelo en producción:
    - Model A (control/actual) vs Model B (candidate/nuevo)
    - Traffic splitting configurable (ej. 90/10, 80/20, 50/50)
    - Métricas de comparación automatizadas
    - Statistical significance testing (chi-squared, t-test)
    - Auto-promotion si el candidato gana

Arquitectura:
    User → ChatbotService → ABRouter → Model A (port 8000)
                                      → Model B (port 8001)

Uso:
    # Crear un experimento
    python ab_testing.py create \
        --name "v2-vs-v1" \
        --model-a http://llm-server-a:8000 \
        --model-b http://llm-server-b:8001 \
        --traffic-split 90 \
        --config-dir ./experiments

    # Registrar resultado de una interacción
    python ab_testing.py log \
        --experiment "v2-vs-v1" \
        --config-dir ./experiments \
        --variant A \
        --latency-ms 1500 \
        --confidence 0.85 \
        --thumbs up

    # Analizar resultados
    python ab_testing.py analyze \
        --experiment "v2-vs-v1" \
        --config-dir ./experiments

    # Decidir ganador
    python ab_testing.py decide \
        --experiment "v2-vs-v1" \
        --config-dir ./experiments \
        --min-samples 100
"""

import argparse
import json
import logging
import math
import os
import random
import statistics
import time
from collections import Counter, defaultdict
from datetime import datetime
from pathlib import Path
from typing import Any

import requests

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s"
)
logger = logging.getLogger("okla-ab")


# ============================================================
# EXPERIMENT MANAGEMENT
# ============================================================

class ABExperiment:
    """Manages an A/B testing experiment."""

    def __init__(self, config_dir: str, name: str):
        self.config_dir = Path(config_dir)
        self.config_dir.mkdir(parents=True, exist_ok=True)
        self.name = name
        self.config_path = self.config_dir / f"{name}.json"
        self.results_path = self.config_dir / f"{name}_results.jsonl"
        self.config = self._load_config()

    def _load_config(self) -> dict:
        if self.config_path.exists():
            with open(self.config_path, "r") as f:
                return json.load(f)
        return {}

    def _save_config(self):
        with open(self.config_path, "w") as f:
            json.dump(self.config, f, indent=2, ensure_ascii=False, default=str)

    @classmethod
    def create(
        cls,
        config_dir: str,
        name: str,
        model_a_url: str,
        model_b_url: str,
        traffic_split_a: int = 90,
        description: str = ""
    ) -> "ABExperiment":
        """Create a new A/B experiment."""
        exp = cls(config_dir, name)
        exp.config = {
            "name": name,
            "description": description,
            "created_at": datetime.now().isoformat(),
            "status": "running",  # running, paused, completed, cancelled
            "model_a": {
                "url": model_a_url,
                "label": "control",
                "description": "Current production model"
            },
            "model_b": {
                "url": model_b_url,
                "label": "candidate",
                "description": "New candidate model"
            },
            "traffic_split": {
                "a_percent": traffic_split_a,
                "b_percent": 100 - traffic_split_a
            },
            "metrics_config": {
                "primary_metric": "satisfaction_rate",
                "secondary_metrics": ["latency_p95", "confidence_mean", "lead_capture_rate"],
                "min_samples_per_variant": 50,
                "significance_level": 0.05
            },
            "winner": None,
            "decided_at": None
        }
        exp._save_config()
        logger.info(f"Created experiment '{name}': {traffic_split_a}% A / {100-traffic_split_a}% B")
        return exp

    def route_request(self) -> str:
        """Route a request to variant A or B based on traffic split."""
        if self.config.get("status") != "running":
            return "A"  # Default to control if not running

        a_pct = self.config["traffic_split"]["a_percent"]
        return "A" if random.randint(1, 100) <= a_pct else "B"

    def get_model_url(self, variant: str) -> str:
        """Get the model URL for a variant."""
        key = "model_a" if variant == "A" else "model_b"
        return self.config[key]["url"]

    def log_result(
        self,
        variant: str,
        latency_ms: float,
        confidence: float | None = None,
        thumbs: str | None = None,
        intent: str | None = None,
        lead_captured: bool = False,
        tokens_used: int = 0,
        success: bool = True,
        user_message: str | None = None,
    ):
        """Log a single interaction result."""
        entry = {
            "timestamp": datetime.now().isoformat(),
            "variant": variant,
            "latency_ms": latency_ms,
            "confidence": confidence,
            "thumbs": thumbs,
            "intent": intent,
            "lead_captured": lead_captured,
            "tokens_used": tokens_used,
            "success": success,
            "user_message_length": len(user_message) if user_message else 0
        }

        with open(self.results_path, "a", encoding="utf-8") as f:
            f.write(json.dumps(entry, ensure_ascii=False) + "\n")

    def load_results(self) -> list[dict]:
        """Load all logged results."""
        results = []
        if self.results_path.exists():
            with open(self.results_path, "r") as f:
                for line in f:
                    if line.strip():
                        try:
                            results.append(json.loads(line))
                        except json.JSONDecodeError:
                            continue
        return results


# ============================================================
# STATISTICAL ANALYSIS
# ============================================================

def chi_squared_test(successes_a: int, total_a: int, successes_b: int, total_b: int) -> dict:
    """
    Chi-squared test for comparing proportions (satisfaction rates).
    Returns test statistic, p-value approximation, and significance.
    """
    if total_a == 0 or total_b == 0:
        return {"significant": False, "reason": "Insufficient data"}

    # Pooled proportion
    p_pool = (successes_a + successes_b) / (total_a + total_b)
    q_pool = 1 - p_pool

    if p_pool == 0 or q_pool == 0:
        return {"significant": False, "reason": "No variation in data"}

    # Expected values
    e_a_success = total_a * p_pool
    e_a_failure = total_a * q_pool
    e_b_success = total_b * p_pool
    e_b_failure = total_b * q_pool

    # Chi-squared statistic
    chi2 = 0
    for obs, exp in [
        (successes_a, e_a_success),
        (total_a - successes_a, e_a_failure),
        (successes_b, e_b_success),
        (total_b - successes_b, e_b_failure)
    ]:
        if exp > 0:
            chi2 += (obs - exp) ** 2 / exp

    # Approximate p-value using chi-squared distribution with 1 df
    # Using simple approximation: p ≈ e^(-chi2/2) for chi2 > 3.84
    significant = chi2 > 3.841  # 95% confidence level, 1 df

    return {
        "chi_squared": round(chi2, 4),
        "significant": significant,
        "confidence_level": "95%",
        "p_value_approx": round(math.exp(-chi2 / 2), 6) if chi2 > 0 else 1.0
    }


def welch_t_test(values_a: list[float], values_b: list[float]) -> dict:
    """
    Welch's t-test for comparing means (latency, confidence).
    Does not assume equal variances.
    """
    n_a = len(values_a)
    n_b = len(values_b)

    if n_a < 2 or n_b < 2:
        return {"significant": False, "reason": "Insufficient data (need >= 2 per group)"}

    mean_a = statistics.mean(values_a)
    mean_b = statistics.mean(values_b)
    var_a = statistics.variance(values_a)
    var_b = statistics.variance(values_b)

    # Welch's t-statistic
    se = math.sqrt(var_a / n_a + var_b / n_b)
    if se == 0:
        return {"significant": False, "reason": "No variance"}

    t_stat = (mean_a - mean_b) / se

    # Welch-Satterthwaite degrees of freedom
    num = (var_a / n_a + var_b / n_b) ** 2
    denom = (var_a / n_a) ** 2 / (n_a - 1) + (var_b / n_b) ** 2 / (n_b - 1)
    df = num / denom if denom > 0 else 1

    # Approximate significance (|t| > 1.96 for 95% with large df)
    significant = abs(t_stat) > 1.96

    return {
        "t_statistic": round(t_stat, 4),
        "degrees_of_freedom": round(df, 1),
        "mean_a": round(mean_a, 4),
        "mean_b": round(mean_b, 4),
        "diff": round(mean_b - mean_a, 4),
        "diff_pct": round((mean_b - mean_a) / mean_a * 100, 2) if mean_a != 0 else 0,
        "significant": significant,
        "confidence_level": "95%"
    }


# ============================================================
# ANALYSIS
# ============================================================

def analyze_experiment(experiment: ABExperiment) -> dict:
    """Full analysis of A/B experiment results."""
    results = experiment.load_results()
    if not results:
        return {"error": "No results logged yet"}

    # Split by variant
    a_results = [r for r in results if r["variant"] == "A"]
    b_results = [r for r in results if r["variant"] == "B"]

    analysis = {
        "experiment": experiment.name,
        "status": experiment.config.get("status"),
        "total_interactions": len(results),
        "variant_a": analyze_variant(a_results, "A (Control)"),
        "variant_b": analyze_variant(b_results, "B (Candidate)"),
    }

    # Statistical tests
    tests = {}

    # 1. Satisfaction rate test (chi-squared)
    a_thumbs_up = sum(1 for r in a_results if r.get("thumbs") == "up")
    a_thumbs_total = sum(1 for r in a_results if r.get("thumbs") in ("up", "down"))
    b_thumbs_up = sum(1 for r in b_results if r.get("thumbs") == "up")
    b_thumbs_total = sum(1 for r in b_results if r.get("thumbs") in ("up", "down"))

    tests["satisfaction_rate"] = chi_squared_test(
        a_thumbs_up, a_thumbs_total, b_thumbs_up, b_thumbs_total
    )

    # 2. Latency test (Welch's t)
    a_latencies = [r["latency_ms"] for r in a_results if r.get("latency_ms")]
    b_latencies = [r["latency_ms"] for r in b_results if r.get("latency_ms")]
    tests["latency"] = welch_t_test(a_latencies, b_latencies)

    # 3. Confidence test (Welch's t)
    a_confs = [r["confidence"] for r in a_results if r.get("confidence") is not None]
    b_confs = [r["confidence"] for r in b_results if r.get("confidence") is not None]
    tests["confidence"] = welch_t_test(a_confs, b_confs)

    # 4. Lead capture test (chi-squared)
    a_leads = sum(1 for r in a_results if r.get("lead_captured"))
    b_leads = sum(1 for r in b_results if r.get("lead_captured"))
    tests["lead_capture"] = chi_squared_test(
        a_leads, len(a_results), b_leads, len(b_results)
    )

    analysis["statistical_tests"] = tests

    return analysis


def analyze_variant(results: list[dict], label: str) -> dict:
    """Analyze metrics for a single variant."""
    if not results:
        return {"label": label, "count": 0}

    latencies = [r["latency_ms"] for r in results if r.get("latency_ms")]
    confidences = [r["confidence"] for r in results if r.get("confidence") is not None]
    thumbs_up = sum(1 for r in results if r.get("thumbs") == "up")
    thumbs_down = sum(1 for r in results if r.get("thumbs") == "down")
    thumbs_total = thumbs_up + thumbs_down
    leads = sum(1 for r in results if r.get("lead_captured"))
    successes = sum(1 for r in results if r.get("success", True))
    tokens = [r["tokens_used"] for r in results if r.get("tokens_used")]

    latencies.sort()
    n = len(latencies)

    return {
        "label": label,
        "count": len(results),
        "success_rate": round(successes / len(results) * 100, 2),
        "satisfaction_rate": round(thumbs_up / thumbs_total * 100, 2) if thumbs_total > 0 else None,
        "thumbs_up": thumbs_up,
        "thumbs_down": thumbs_down,
        "confidence_mean": round(statistics.mean(confidences), 4) if confidences else None,
        "confidence_median": round(statistics.median(confidences), 4) if confidences else None,
        "latency_p50": round(latencies[n // 2], 1) if latencies else None,
        "latency_p95": round(latencies[int(n * 0.95)], 1) if latencies else None,
        "latency_mean": round(statistics.mean(latencies), 1) if latencies else None,
        "lead_capture_rate": round(leads / len(results) * 100, 2),
        "avg_tokens": round(statistics.mean(tokens), 0) if tokens else None,
        "intents": Counter(r.get("intent") for r in results if r.get("intent")).most_common(10)
    }


def decide_winner(analysis: dict, min_samples: int = 50) -> dict:
    """Decide the winner based on statistical significance."""
    a = analysis.get("variant_a", {})
    b = analysis.get("variant_b", {})
    tests = analysis.get("statistical_tests", {})

    # Check minimum samples
    if a.get("count", 0) < min_samples or b.get("count", 0) < min_samples:
        return {
            "decision": "INSUFFICIENT_DATA",
            "reason": f"Need at least {min_samples} samples per variant. A: {a.get('count', 0)}, B: {b.get('count', 0)}",
            "winner": None
        }

    # Score each variant
    score_a = 0
    score_b = 0
    reasons = []

    # Primary: Satisfaction rate
    sat_test = tests.get("satisfaction_rate", {})
    a_sat = a.get("satisfaction_rate")
    b_sat = b.get("satisfaction_rate")
    if a_sat is not None and b_sat is not None:
        if sat_test.get("significant"):
            if b_sat > a_sat:
                score_b += 3
                reasons.append(f"B has significantly higher satisfaction: {b_sat}% vs {a_sat}%")
            else:
                score_a += 3
                reasons.append(f"A has significantly higher satisfaction: {a_sat}% vs {b_sat}%")
        else:
            reasons.append(f"Satisfaction not significantly different: A={a_sat}% vs B={b_sat}%")

    # Secondary: Latency (lower is better)
    lat_test = tests.get("latency", {})
    a_lat = a.get("latency_p95")
    b_lat = b.get("latency_p95")
    if a_lat is not None and b_lat is not None:
        if lat_test.get("significant"):
            if b_lat < a_lat:
                score_b += 1
                reasons.append(f"B has significantly lower latency: {b_lat}ms vs {a_lat}ms")
            else:
                score_a += 1
                reasons.append(f"A has significantly lower latency: {a_lat}ms vs {b_lat}ms")

    # Secondary: Confidence (higher is better)
    conf_test = tests.get("confidence", {})
    a_conf = a.get("confidence_mean")
    b_conf = b.get("confidence_mean")
    if a_conf is not None and b_conf is not None:
        if conf_test.get("significant"):
            if b_conf > a_conf:
                score_b += 1
                reasons.append(f"B has significantly higher confidence: {b_conf} vs {a_conf}")
            else:
                score_a += 1
                reasons.append(f"A has significantly higher confidence: {a_conf} vs {b_conf}")

    # Secondary: Lead capture (higher is better)
    lead_test = tests.get("lead_capture", {})
    a_lead = a.get("lead_capture_rate", 0)
    b_lead = b.get("lead_capture_rate", 0)
    if lead_test.get("significant"):
        if b_lead > a_lead:
            score_b += 2
            reasons.append(f"B has significantly higher lead capture: {b_lead}% vs {a_lead}%")
        else:
            score_a += 2
            reasons.append(f"A has significantly higher lead capture: {a_lead}% vs {b_lead}%")

    # Decision
    if score_b > score_a:
        winner = "B"
        decision = "PROMOTE_B"
    elif score_a > score_b:
        winner = "A"
        decision = "KEEP_A"
    else:
        winner = None
        decision = "NO_CLEAR_WINNER"

    return {
        "decision": decision,
        "winner": winner,
        "score_a": score_a,
        "score_b": score_b,
        "reasons": reasons,
        "recommendation": {
            "PROMOTE_B": "Promover modelo B a producción y retirar modelo A",
            "KEEP_A": "Mantener modelo A en producción, descartar candidato B",
            "NO_CLEAR_WINNER": "Sin ganador claro. Extender experimento o probar con más tráfico"
        }.get(decision, "")
    }


# ============================================================
# REPORT
# ============================================================

def generate_ab_report(analysis: dict, decision: dict, output_dir: str) -> str:
    """Generate A/B test report."""
    os.makedirs(output_dir, exist_ok=True)
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    md_path = os.path.join(output_dir, f"ab_report_{analysis['experiment']}_{timestamp}.md")

    a = analysis.get("variant_a", {})
    b = analysis.get("variant_b", {})
    tests = analysis.get("statistical_tests", {})

    with open(md_path, "w", encoding="utf-8") as f:
        f.write(f"# 🔬 A/B Test Report: {analysis['experiment']}\n\n")
        f.write(f"**Fecha:** {datetime.now().strftime('%Y-%m-%d %H:%M')}\n")
        f.write(f"**Total interacciones:** {analysis['total_interactions']}\n\n")

        # Decision
        icon = "✅" if decision["decision"] == "PROMOTE_B" else "🔄" if decision["decision"] == "KEEP_A" else "⏳"
        f.write(f"## {icon} Decisión: {decision['decision']}\n\n")
        f.write(f"**{decision['recommendation']}**\n\n")
        f.write(f"Score: A={decision['score_a']} vs B={decision['score_b']}\n\n")

        for r in decision.get("reasons", []):
            f.write(f"- {r}\n")
        f.write("\n")

        # Metrics comparison
        f.write("## 📊 Comparación de Métricas\n\n")
        f.write("| Métrica | A (Control) | B (Candidate) | Ganador |\n")
        f.write("|---------|-------------|---------------|----------|\n")

        a_sat = a.get("satisfaction_rate", "N/A")
        b_sat = b.get("satisfaction_rate", "N/A")
        f.write(f"| Satisfacción | {a_sat}% | {b_sat}% | "
                f"{'B ✅' if isinstance(b_sat, (int, float)) and isinstance(a_sat, (int, float)) and b_sat > a_sat else 'A' if isinstance(a_sat, (int, float)) and isinstance(b_sat, (int, float)) and a_sat > b_sat else '—'} |\n")

        f.write(f"| Latencia p95 | {a.get('latency_p95', 'N/A')}ms | {b.get('latency_p95', 'N/A')}ms | "
                f"{'B ✅' if (a.get('latency_p95') or 0) > (b.get('latency_p95') or 0) else 'A'} |\n")

        f.write(f"| Confidence | {a.get('confidence_mean', 'N/A')} | {b.get('confidence_mean', 'N/A')} | "
                f"{'B ✅' if (b.get('confidence_mean') or 0) > (a.get('confidence_mean') or 0) else 'A'} |\n")

        f.write(f"| Lead Capture | {a.get('lead_capture_rate', 0)}% | {b.get('lead_capture_rate', 0)}% | "
                f"{'B ✅' if (b.get('lead_capture_rate') or 0) > (a.get('lead_capture_rate') or 0) else 'A'} |\n")

        f.write(f"| Muestras | {a.get('count', 0)} | {b.get('count', 0)} | — |\n")

        # Statistical significance
        f.write("\n## 📐 Significancia Estadística\n\n")
        f.write("| Test | Tipo | Significativo | Detalle |\n")
        f.write("|------|------|---------------|----------|\n")
        for name, test in tests.items():
            sig = "✅ Sí" if test.get("significant") else "❌ No"
            detail = ""
            if "chi_squared" in test:
                detail = f"χ²={test['chi_squared']}"
            elif "t_statistic" in test:
                detail = f"t={test['t_statistic']}, diff={test.get('diff_pct', 0)}%"
            f.write(f"| {name} | {'χ²' if 'chi_squared' in test else 't-test'} | {sig} | {detail} |\n")

    logger.info(f"A/B report saved to {md_path}")
    return md_path


# ============================================================
# CLI
# ============================================================

def main():
    parser = argparse.ArgumentParser(description="OKLA A/B Testing Framework")
    subparsers = parser.add_subparsers(dest="command", required=True)

    # Create
    create_p = subparsers.add_parser("create", help="Create new experiment")
    create_p.add_argument("--name", required=True)
    create_p.add_argument("--model-a", required=True, help="Control model URL")
    create_p.add_argument("--model-b", required=True, help="Candidate model URL")
    create_p.add_argument("--traffic-split", type=int, default=90, help="% traffic to A")
    create_p.add_argument("--config-dir", default="./experiments")

    # Log
    log_p = subparsers.add_parser("log", help="Log interaction result")
    log_p.add_argument("--experiment", required=True)
    log_p.add_argument("--config-dir", default="./experiments")
    log_p.add_argument("--variant", required=True, choices=["A", "B"])
    log_p.add_argument("--latency-ms", type=float, required=True)
    log_p.add_argument("--confidence", type=float, default=None)
    log_p.add_argument("--thumbs", choices=["up", "down"], default=None)
    log_p.add_argument("--intent", default=None)
    log_p.add_argument("--lead", action="store_true")

    # Analyze
    analyze_p = subparsers.add_parser("analyze", help="Analyze experiment results")
    analyze_p.add_argument("--experiment", required=True)
    analyze_p.add_argument("--config-dir", default="./experiments")
    analyze_p.add_argument("--output-dir", default="./reports")

    # Decide
    decide_p = subparsers.add_parser("decide", help="Decide experiment winner")
    decide_p.add_argument("--experiment", required=True)
    decide_p.add_argument("--config-dir", default="./experiments")
    decide_p.add_argument("--min-samples", type=int, default=50)
    decide_p.add_argument("--output-dir", default="./reports")

    args = parser.parse_args()

    if args.command == "create":
        exp = ABExperiment.create(
            args.config_dir, args.name,
            args.model_a, args.model_b,
            args.traffic_split
        )
        print(f"\n✅ Experiment created: {args.name}")
        print(f"   Traffic: {args.traffic_split}% A / {100-args.traffic_split}% B")

    elif args.command == "log":
        exp = ABExperiment(args.config_dir, args.experiment)
        exp.log_result(
            variant=args.variant,
            latency_ms=args.latency_ms,
            confidence=args.confidence,
            thumbs=args.thumbs,
            intent=args.intent,
            lead_captured=args.lead
        )
        print(f"✅ Logged result for variant {args.variant}")

    elif args.command == "analyze":
        exp = ABExperiment(args.config_dir, args.experiment)
        analysis = analyze_experiment(exp)
        print(json.dumps(analysis, indent=2, ensure_ascii=False, default=str))

    elif args.command == "decide":
        exp = ABExperiment(args.config_dir, args.experiment)
        analysis = analyze_experiment(exp)
        decision = decide_winner(analysis, args.min_samples)

        report_path = generate_ab_report(analysis, decision, args.output_dir)

        icon = "✅" if decision["decision"] == "PROMOTE_B" else "🔄" if decision["decision"] == "KEEP_A" else "⏳"
        print(f"\n{icon} Decision: {decision['decision']}")
        print(f"   {decision['recommendation']}")
        print(f"   Report: {report_path}")


if __name__ == "__main__":
    main()
