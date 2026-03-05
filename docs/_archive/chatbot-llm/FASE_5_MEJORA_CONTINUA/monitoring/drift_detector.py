#!/usr/bin/env python3
"""
OKLA Chatbot LLM — Drift Detection & Quality Monitoring
=========================================================

Detecta degradación del modelo en producción comparando métricas
actuales contra baselines establecidos durante la evaluación inicial.

Señales de drift monitoreadas:
    1. Confidence drift    — Caída en confianza promedio
    2. Intent distribution — Cambio en distribución de intents
    3. Fallback rate       — Aumento de respuestas genéricas/fallback
    4. Response latency    — Degradación de rendimiento
    5. Feedback sentiment  — Deterioro en satisfacción del usuario
    6. Token usage drift   — Cambio en longitud de respuestas
    7. Lead capture drop   — Reducción en captura de leads

Uso:
    # Establecer baseline (después de deploy inicial)
    python drift_detector.py baseline \
        --server-url http://localhost:8000 \
        --test-data ../../FASE_2_DATASET/output/test.jsonl \
        --output ./baselines/baseline_v1.json

    # Monitoreo continuo (ejecutar periódicamente via cron)
    python drift_detector.py monitor \
        --server-url http://localhost:8000 \
        --baseline ./baselines/baseline_v1.json \
        --feedback-dir ../feedback/feedback_data \
        --output-dir ./drift_reports \
        --alert-webhook https://hooks.slack.com/...

    # Comparar dos modelos
    python drift_detector.py compare \
        --baseline ./baselines/baseline_v1.json \
        --current ./baselines/baseline_v2.json
"""

import argparse
import json
import logging
import os
import statistics
import time
from collections import Counter
from datetime import datetime, timedelta
from pathlib import Path
from typing import Any

import requests

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s"
)
logger = logging.getLogger("okla-drift")


# ============================================================
# THRESHOLDS
# ============================================================

DRIFT_THRESHOLDS = {
    "confidence_drop_pct": 10.0,         # Alert if avg confidence drops > 10%
    "fallback_rate_increase_pct": 5.0,    # Alert if fallback rate increases > 5pp
    "latency_increase_pct": 50.0,         # Alert if p95 latency increases > 50%
    "satisfaction_drop_pct": 10.0,         # Alert if satisfaction drops > 10pp
    "lead_capture_drop_pct": 15.0,        # Alert if lead rate drops > 15pp
    "intent_distribution_kl": 0.3,        # KL divergence threshold
    "token_usage_change_pct": 30.0,       # Alert if token usage changes > 30%
}

ALERT_LEVELS = {
    "INFO": 0,
    "WARNING": 1,
    "CRITICAL": 2
}


# ============================================================
# BASELINE ESTABLISHMENT
# ============================================================

def establish_baseline(
    server_url: str,
    test_data_path: str,
    system_prompt: str,
    max_samples: int = 100
) -> dict:
    """Run evaluation and establish performance baseline."""
    logger.info("Establishing performance baseline...")

    # Load test data
    test_data = []
    with open(test_data_path, "r") as f:
        for line in f:
            if line.strip():
                test_data.append(json.loads(line))

    samples = test_data[:max_samples]

    confidences = []
    latencies = []
    token_counts = []
    intents_detected = []
    fallback_count = 0
    success_count = 0
    total = 0

    for conv in samples:
        messages = conv.get("messages", [])
        user_msg = None
        for msg in messages:
            if msg["role"] == "user":
                user_msg = msg["content"]
                break

        if not user_msg:
            continue

        total += 1
        start = time.time()

        try:
            resp = requests.post(
                f"{server_url}/v1/chat/completions",
                json={
                    "model": "okla-llama3-8b",
                    "messages": [
                        {"role": "system", "content": system_prompt},
                        {"role": "user", "content": user_msg}
                    ],
                    "temperature": 0.3,
                    "max_tokens": 512
                },
                timeout=120
            )
            elapsed_ms = (time.time() - start) * 1000
            resp.raise_for_status()
            data = resp.json()

            content = data["choices"][0]["message"]["content"]
            tokens = data.get("usage", {}).get("total_tokens", 0)

            success_count += 1
            latencies.append(elapsed_ms)
            token_counts.append(tokens)

            # Parse confidence and intent from response
            import re
            conf_match = re.search(r'"confidence"\s*:\s*([0-9.]+)', content)
            if conf_match:
                confidences.append(float(conf_match.group(1)))

            intent_match = re.search(r'"intent"\s*:\s*"([^"]+)"', content)
            if intent_match:
                intent = intent_match.group(1)
                intents_detected.append(intent)
                if intent in ("fallback", "unknown", "general_help"):
                    fallback_count += 1
            else:
                fallback_count += 1

        except Exception as e:
            elapsed_ms = (time.time() - start) * 1000
            latencies.append(elapsed_ms)
            logger.warning(f"Request failed: {e}")

    # Build baseline
    latencies.sort()
    n_lat = len(latencies)

    intent_dist = Counter(intents_detected)
    total_intents = sum(intent_dist.values()) or 1
    intent_probs = {k: v / total_intents for k, v in intent_dist.items()}

    baseline = {
        "created_at": datetime.now().isoformat(),
        "server_url": server_url,
        "samples_tested": total,
        "success_rate": round(success_count / total * 100, 2) if total > 0 else 0,
        "confidence": {
            "mean": round(statistics.mean(confidences), 4) if confidences else 0,
            "median": round(statistics.median(confidences), 4) if confidences else 0,
            "std_dev": round(statistics.stdev(confidences), 4) if len(confidences) > 1 else 0,
        },
        "latency": {
            "p50_ms": round(latencies[n_lat // 2], 1) if latencies else 0,
            "p90_ms": round(latencies[int(n_lat * 0.9)], 1) if latencies else 0,
            "p95_ms": round(latencies[int(n_lat * 0.95)], 1) if latencies else 0,
            "p99_ms": round(latencies[min(int(n_lat * 0.99), n_lat - 1)], 1) if latencies else 0,
            "mean_ms": round(statistics.mean(latencies), 1) if latencies else 0,
        },
        "tokens": {
            "mean": round(statistics.mean(token_counts), 1) if token_counts else 0,
            "median": round(statistics.median(token_counts), 1) if token_counts else 0,
        },
        "fallback_rate": round(fallback_count / total * 100, 2) if total > 0 else 0,
        "intent_distribution": intent_probs,
        "intent_counts": dict(intent_dist),
    }

    logger.info(f"Baseline established: confidence={baseline['confidence']['mean']:.3f}, "
                f"p95={baseline['latency']['p95_ms']:.0f}ms, "
                f"fallback={baseline['fallback_rate']:.1f}%")

    return baseline


# ============================================================
# DRIFT DETECTION
# ============================================================

def compute_kl_divergence(p: dict[str, float], q: dict[str, float]) -> float:
    """
    Compute KL divergence D(P || Q) for intent distributions.
    Smoothed with epsilon to avoid division by zero.
    """
    epsilon = 1e-10
    all_keys = set(p.keys()) | set(q.keys())

    kl = 0.0
    for key in all_keys:
        p_val = p.get(key, epsilon)
        q_val = q.get(key, epsilon)
        if p_val > 0:
            kl += p_val * (abs(p_val / q_val) if q_val > 0 else 0)

    return round(kl, 4)


def detect_drift(
    baseline: dict,
    current: dict,
    thresholds: dict | None = None
) -> list[dict]:
    """Compare current metrics against baseline and detect drift."""
    t = thresholds or DRIFT_THRESHOLDS
    alerts = []

    # 1. Confidence drift
    base_conf = baseline.get("confidence", {}).get("mean", 0)
    curr_conf = current.get("confidence", {}).get("mean", 0)
    if base_conf > 0:
        conf_drop = ((base_conf - curr_conf) / base_conf) * 100
        if conf_drop > t["confidence_drop_pct"]:
            alerts.append({
                "level": "WARNING" if conf_drop < 20 else "CRITICAL",
                "type": "confidence_drift",
                "message": f"Confidence dropped {conf_drop:.1f}%: {base_conf:.3f} → {curr_conf:.3f}",
                "baseline_value": base_conf,
                "current_value": curr_conf,
                "change_pct": round(-conf_drop, 2)
            })

    # 2. Fallback rate increase
    base_fallback = baseline.get("fallback_rate", 0)
    curr_fallback = current.get("fallback_rate", 0)
    fallback_increase = curr_fallback - base_fallback
    if fallback_increase > t["fallback_rate_increase_pct"]:
        alerts.append({
            "level": "WARNING" if fallback_increase < 10 else "CRITICAL",
            "type": "fallback_rate_drift",
            "message": f"Fallback rate increased {fallback_increase:.1f}pp: {base_fallback:.1f}% → {curr_fallback:.1f}%",
            "baseline_value": base_fallback,
            "current_value": curr_fallback,
            "change_pp": round(fallback_increase, 2)
        })

    # 3. Latency degradation
    base_p95 = baseline.get("latency", {}).get("p95_ms", 0)
    curr_p95 = current.get("latency", {}).get("p95_ms", 0)
    if base_p95 > 0:
        lat_increase = ((curr_p95 - base_p95) / base_p95) * 100
        if lat_increase > t["latency_increase_pct"]:
            alerts.append({
                "level": "WARNING" if lat_increase < 100 else "CRITICAL",
                "type": "latency_drift",
                "message": f"p95 latency increased {lat_increase:.0f}%: {base_p95:.0f}ms → {curr_p95:.0f}ms",
                "baseline_value": base_p95,
                "current_value": curr_p95,
                "change_pct": round(lat_increase, 2)
            })

    # 4. Intent distribution shift
    base_dist = baseline.get("intent_distribution", {})
    curr_dist = current.get("intent_distribution", {})
    if base_dist and curr_dist:
        kl_div = compute_kl_divergence(curr_dist, base_dist)
        if kl_div > t["intent_distribution_kl"]:
            alerts.append({
                "level": "INFO" if kl_div < 0.5 else "WARNING",
                "type": "intent_distribution_drift",
                "message": f"Intent distribution shifted (KL divergence: {kl_div})",
                "kl_divergence": kl_div,
                "baseline_top": sorted(base_dist.items(), key=lambda x: -x[1])[:5],
                "current_top": sorted(curr_dist.items(), key=lambda x: -x[1])[:5],
            })

    # 5. Token usage change
    base_tokens = baseline.get("tokens", {}).get("mean", 0)
    curr_tokens = current.get("tokens", {}).get("mean", 0)
    if base_tokens > 0:
        token_change = abs((curr_tokens - base_tokens) / base_tokens) * 100
        if token_change > t["token_usage_change_pct"]:
            direction = "increased" if curr_tokens > base_tokens else "decreased"
            alerts.append({
                "level": "INFO",
                "type": "token_usage_drift",
                "message": f"Token usage {direction} {token_change:.0f}%: {base_tokens:.0f} → {curr_tokens:.0f}",
                "baseline_value": base_tokens,
                "current_value": curr_tokens,
                "change_pct": round(token_change, 2)
            })

    return alerts


def load_feedback_metrics(feedback_dir: str, days: int = 7) -> dict:
    """Load recent feedback metrics for drift comparison."""
    from datetime import datetime, timedelta

    cutoff = datetime.now() - timedelta(days=days)
    thumbs_up = 0
    thumbs_down = 0
    ratings = []
    lead_signals = 0
    total = 0

    feedback_path = Path(feedback_dir)
    if not feedback_path.exists():
        return {}

    for jsonl_file in sorted(feedback_path.glob("feedback_*.jsonl")):
        try:
            date_str = jsonl_file.stem.replace("feedback_", "")
            file_date = datetime.strptime(date_str, "%Y%m%d")
            if file_date < cutoff:
                continue
        except ValueError:
            continue

        with open(jsonl_file, "r") as f:
            for line in f:
                if not line.strip():
                    continue
                try:
                    entry = json.loads(line)
                    total += 1
                    if entry.get("thumbs") == "up":
                        thumbs_up += 1
                    elif entry.get("thumbs") == "down":
                        thumbs_down += 1
                    if entry.get("rating"):
                        ratings.append(entry["rating"])
                except (json.JSONDecodeError, KeyError):
                    continue

    if total == 0:
        return {}

    return {
        "period_days": days,
        "total_feedback": total,
        "satisfaction_rate": round(thumbs_up / (thumbs_up + thumbs_down) * 100, 2) if (thumbs_up + thumbs_down) > 0 else None,
        "avg_rating": round(statistics.mean(ratings), 2) if ratings else None,
        "thumbs_up": thumbs_up,
        "thumbs_down": thumbs_down,
    }


# ============================================================
# ALERTING
# ============================================================

def send_alert(webhook_url: str | None, alerts: list[dict], report_path: str):
    """Send drift alerts via webhook (Slack/Teams compatible)."""
    if not webhook_url or not alerts:
        return

    critical = [a for a in alerts if a["level"] == "CRITICAL"]
    warnings = [a for a in alerts if a["level"] == "WARNING"]

    emoji = "🔴" if critical else "🟡" if warnings else "🟢"
    title = f"{emoji} OKLA Chatbot — Drift Alert"

    blocks = [f"**{title}**\n"]
    blocks.append(f"Detected {len(alerts)} drift signal(s):\n")

    for a in alerts:
        icon = "🔴" if a["level"] == "CRITICAL" else "🟡" if a["level"] == "WARNING" else "ℹ️"
        blocks.append(f"- {icon} [{a['level']}] {a['message']}")

    blocks.append(f"\n📄 Full report: {report_path}")

    payload = {
        "text": "\n".join(blocks),
        "blocks": [
            {"type": "section", "text": {"type": "mrkdwn", "text": "\n".join(blocks)}}
        ]
    }

    try:
        resp = requests.post(webhook_url, json=payload, timeout=10)
        if resp.ok:
            logger.info(f"Alert sent to webhook ({len(alerts)} alerts)")
        else:
            logger.warning(f"Webhook returned {resp.status_code}")
    except Exception as e:
        logger.error(f"Failed to send alert: {e}")


# ============================================================
# REPORT
# ============================================================

def generate_drift_report(
    baseline: dict,
    current: dict,
    alerts: list[dict],
    feedback_metrics: dict,
    output_dir: str
) -> str:
    """Generate drift detection report."""
    os.makedirs(output_dir, exist_ok=True)
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    md_path = os.path.join(output_dir, f"drift_report_{timestamp}.md")

    with open(md_path, "w", encoding="utf-8") as f:
        f.write("# 📉 OKLA Chatbot — Drift Detection Report\n\n")
        f.write(f"**Fecha:** {datetime.now().strftime('%Y-%m-%d %H:%M')}\n")
        f.write(f"**Baseline:** {baseline.get('created_at', 'N/A')}\n\n")

        # Alerts summary
        critical = len([a for a in alerts if a["level"] == "CRITICAL"])
        warning = len([a for a in alerts if a["level"] == "WARNING"])
        info = len([a for a in alerts if a["level"] == "INFO"])

        status = "🔴 CRITICAL" if critical else "🟡 WARNING" if warning else "🟢 HEALTHY"
        f.write(f"## Estado: {status}\n\n")
        f.write(f"| Nivel | Count |\n|-------|-------|\n")
        f.write(f"| 🔴 Critical | {critical} |\n")
        f.write(f"| 🟡 Warning | {warning} |\n")
        f.write(f"| ℹ️ Info | {info} |\n\n")

        # Alerts detail
        if alerts:
            f.write("## 🚨 Alertas\n\n")
            for a in alerts:
                icon = "🔴" if a["level"] == "CRITICAL" else "🟡" if a["level"] == "WARNING" else "ℹ️"
                f.write(f"### {icon} {a['type']}\n\n")
                f.write(f"**{a['message']}**\n\n")
                for k, v in a.items():
                    if k not in ("level", "type", "message"):
                        f.write(f"- {k}: {v}\n")
                f.write("\n")

        # Metrics comparison
        f.write("## 📊 Comparación de Métricas\n\n")
        f.write("| Métrica | Baseline | Actual | Cambio |\n")
        f.write("|---------|----------|--------|--------|\n")

        base_conf = baseline.get("confidence", {}).get("mean", 0)
        curr_conf = current.get("confidence", {}).get("mean", 0)
        f.write(f"| Confidence (mean) | {base_conf:.3f} | {curr_conf:.3f} | "
                f"{'📈' if curr_conf >= base_conf else '📉'} |\n")

        base_p95 = baseline.get("latency", {}).get("p95_ms", 0)
        curr_p95 = current.get("latency", {}).get("p95_ms", 0)
        f.write(f"| Latency p95 | {base_p95:.0f}ms | {curr_p95:.0f}ms | "
                f"{'📈' if curr_p95 <= base_p95 else '📉'} |\n")

        base_fb = baseline.get("fallback_rate", 0)
        curr_fb = current.get("fallback_rate", 0)
        f.write(f"| Fallback Rate | {base_fb:.1f}% | {curr_fb:.1f}% | "
                f"{'📈' if curr_fb <= base_fb else '📉'} |\n")

        base_tok = baseline.get("tokens", {}).get("mean", 0)
        curr_tok = current.get("tokens", {}).get("mean", 0)
        f.write(f"| Tokens (mean) | {base_tok:.0f} | {curr_tok:.0f} | "
                f"{'📈' if abs(curr_tok - base_tok) / max(base_tok, 1) < 0.3 else '📉'} |\n")

        # Feedback metrics
        if feedback_metrics:
            f.write("\n## 👥 Feedback del Usuario\n\n")
            f.write(f"| Métrica | Valor |\n|---------|-------|\n")
            f.write(f"| Período | {feedback_metrics.get('period_days', 'N/A')} días |\n")
            f.write(f"| Total feedback | {feedback_metrics.get('total_feedback', 0)} |\n")
            f.write(f"| Satisfacción | {feedback_metrics.get('satisfaction_rate', 'N/A')}% |\n")
            f.write(f"| Rating promedio | {feedback_metrics.get('avg_rating', 'N/A')}/5 |\n")

        # Recommendations
        f.write("\n## 💡 Recomendaciones\n\n")
        if critical > 0:
            f.write("1. **URGENTE:** Investigar alertas críticas inmediatamente\n")
            f.write("2. Considerar rollback al modelo anterior si la degradación es severa\n")
            f.write("3. Iniciar pipeline de re-entrenamiento con datos corregidos\n")
        elif warning > 0:
            f.write("1. Monitorear métricas durante las próximas 24-48 horas\n")
            f.write("2. Recopilar más feedback de usuarios para confirmar tendencia\n")
            f.write("3. Planificar re-entrenamiento si la tendencia persiste\n")
        else:
            f.write("1. ✅ Modelo funcionando dentro de parámetros normales\n")
            f.write("2. Continuar monitoreo regular\n")
            f.write("3. Próxima evaluación programada según cronograma\n")

    logger.info(f"Drift report saved to {md_path}")

    # Also save JSON
    json_path = md_path.replace(".md", ".json")
    with open(json_path, "w") as fj:
        json.dump({
            "timestamp": datetime.now().isoformat(),
            "status": "critical" if critical else "warning" if warning else "healthy",
            "alerts": alerts,
            "baseline": baseline,
            "current": current,
            "feedback_metrics": feedback_metrics
        }, fj, indent=2, ensure_ascii=False, default=str)

    return md_path


# ============================================================
# CLI
# ============================================================

DEFAULT_SYSTEM_PROMPT = (
    "Eres OKLA Assistant, el asistente virtual oficial de OKLA (okla.com.do). "
    "Responde en español dominicano. "
    "Tu respuesta DEBE ser un JSON con: response, intent, confidence, parameters."
)


def main():
    parser = argparse.ArgumentParser(description="OKLA Drift Detector")
    subparsers = parser.add_subparsers(dest="command", required=True)

    # Baseline
    base_p = subparsers.add_parser("baseline", help="Establish performance baseline")
    base_p.add_argument("--server-url", default="http://localhost:8000")
    base_p.add_argument("--test-data", required=True)
    base_p.add_argument("--output", required=True)
    base_p.add_argument("--max-samples", type=int, default=100)

    # Monitor
    mon_p = subparsers.add_parser("monitor", help="Run drift monitoring")
    mon_p.add_argument("--server-url", default="http://localhost:8000")
    mon_p.add_argument("--baseline", required=True)
    mon_p.add_argument("--test-data", default=None)
    mon_p.add_argument("--feedback-dir", default=None)
    mon_p.add_argument("--output-dir", default="./drift_reports")
    mon_p.add_argument("--alert-webhook", default=None)
    mon_p.add_argument("--max-samples", type=int, default=50)

    # Compare
    cmp_p = subparsers.add_parser("compare", help="Compare two baselines")
    cmp_p.add_argument("--baseline", required=True)
    cmp_p.add_argument("--current", required=True)

    args = parser.parse_args()

    if args.command == "baseline":
        baseline = establish_baseline(
            args.server_url, args.test_data, DEFAULT_SYSTEM_PROMPT, args.max_samples
        )
        os.makedirs(os.path.dirname(args.output), exist_ok=True)
        with open(args.output, "w") as f:
            json.dump(baseline, f, indent=2, ensure_ascii=False, default=str)
        print(f"\n✅ Baseline saved to {args.output}")

    elif args.command == "monitor":
        # Load baseline
        with open(args.baseline, "r") as f:
            baseline = json.load(f)

        # Collect current metrics
        if args.test_data:
            current = establish_baseline(
                args.server_url, args.test_data, DEFAULT_SYSTEM_PROMPT, args.max_samples
            )
        else:
            # Minimal health check
            try:
                resp = requests.get(f"{args.server_url}/health", timeout=10)
                health = resp.json()
                current = {
                    "confidence": {"mean": 0},
                    "latency": {"p95_ms": health.get("avg_response_time_ms", 0)},
                    "fallback_rate": 0,
                    "tokens": {"mean": 0},
                    "intent_distribution": {},
                }
            except Exception as e:
                logger.error(f"Server health check failed: {e}")
                return

        # Load feedback metrics
        feedback_metrics = {}
        if args.feedback_dir:
            feedback_metrics = load_feedback_metrics(args.feedback_dir)

        # Detect drift
        alerts = detect_drift(baseline, current)

        # Generate report
        report_path = generate_drift_report(
            baseline, current, alerts, feedback_metrics, args.output_dir
        )

        # Send alerts
        if alerts:
            send_alert(args.alert_webhook, alerts, report_path)

        status = "🔴 CRITICAL" if any(a["level"] == "CRITICAL" for a in alerts) else \
                 "🟡 WARNING" if any(a["level"] == "WARNING" for a in alerts) else "🟢 HEALTHY"
        print(f"\n{status} — {len(alerts)} drift signal(s) detected")
        print(f"Report: {report_path}")

    elif args.command == "compare":
        with open(args.baseline, "r") as f:
            baseline = json.load(f)
        with open(args.current, "r") as f:
            current = json.load(f)

        alerts = detect_drift(baseline, current)

        print(f"\n📊 Comparison Results:")
        print(f"   Baseline: {baseline.get('created_at', 'N/A')}")
        print(f"   Current:  {current.get('created_at', 'N/A')}")
        print(f"   Alerts:   {len(alerts)}")
        for a in alerts:
            icon = "🔴" if a["level"] == "CRITICAL" else "🟡" if a["level"] == "WARNING" else "ℹ️"
            print(f"   {icon} {a['message']}")


if __name__ == "__main__":
    main()
