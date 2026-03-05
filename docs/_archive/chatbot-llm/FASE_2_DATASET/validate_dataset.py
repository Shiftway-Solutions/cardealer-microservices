#!/usr/bin/env python3
"""
OKLA Chatbot LLM — Validador y Estadísticas del Dataset
========================================================

Valida la estructura JSONL, verifica distribución de intents,
y genera reportes de calidad del dataset.

Uso:
    python validate_dataset.py output/okla_train.jsonl
    python validate_dataset.py output/ --stats
    python validate_dataset.py output/okla_train.jsonl --check-quality
"""

import argparse
import json
import os
import sys
from collections import Counter, defaultdict
from pathlib import Path

try:
    import jsonlines
except ImportError:
    os.system(f"{sys.executable} -m pip install jsonlines")
    import jsonlines


# ============================================================
# VALIDATION
# ============================================================

REQUIRED_RESPONSE_FIELDS = [
    "response", "intent", "confidence", "isFallback",
    "parameters", "leadSignals", "suggestedAction", "quickReplies"
]

REQUIRED_LEAD_SIGNAL_FIELDS = [
    "mentionedBudget", "requestedTestDrive", "askedFinancing", "providedContactInfo"
]

VALID_INTENTS = [
    # Core intents
    "Greeting", "Farewell", "Help", "Fallback",
    "VehicleSearch", "VehicleDetails", "VehicleComparison",
    "VehicleAvailability", "VehiclePrice", "VehicleFeatures",
    "VehicleSpecsQuestion", "CashPurchase",
    "FinancingInfo", "FinancingCalculation", "FinancingRequirements", "TradeIn",
    "TestDriveSchedule", "AppointmentSchedule", "AppointmentCancel", "AppointmentReschedule",
    "DealerLocation", "DealerHours", "DealerContact", "DealerServices",
    "ServiceAppointment", "WarrantyInfo", "PartsInquiry",
    "ContactRequest", "QuoteRequest", "CallbackRequest",
    "Complaint", "Feedback", "Other",
    # Phase 3: Edge-case intents
    "NegotiatePrice", "InsuranceInfo", "DocumentsRequired",
    "DeliveryInfo", "VehicleHistory", "PaymentMethods",
    "ReturnPolicy", "MaintenanceCost", "OutOfScope",
    "LanguageBarrier", "ColorAvailability", "UrgentPurchase", "NewVsUsed",
    # Legal compliance
    "LegalRefusal",
    # Negative responses, conflict & escalation
    "UserObjection", "FrustratedUser", "RequestHumanAgent",
    # Inventory grounding
    "VehicleNotInInventory",
]

VALID_SUGGESTED_ACTIONS = [
    None, "TRANSFER_TO_AGENT", "SCHEDULE_APPOINTMENT", "SCORE_LEAD"
]


def validate_conversation(conv: dict, index: int) -> list:
    """Validate a single conversation and return list of errors."""
    errors = []

    # Check top-level structure
    if "messages" not in conv:
        errors.append(f"[Conv {index}] Missing 'messages' field")
        return errors

    messages = conv["messages"]
    if not isinstance(messages, list) or len(messages) < 2:
        errors.append(f"[Conv {index}] 'messages' must be a list with at least 2 items")
        return errors

    # Check system message
    if messages[0].get("role") != "system":
        errors.append(f"[Conv {index}] First message must have role 'system'")

    # Check alternating user/assistant
    prev_role = "system"
    for i, msg in enumerate(messages[1:], 1):
        role = msg.get("role")
        content = msg.get("content")

        if not role:
            errors.append(f"[Conv {index}, Msg {i}] Missing 'role'")
            continue
        if not content:
            errors.append(f"[Conv {index}, Msg {i}] Missing 'content'")
            continue

        expected_role = "user" if prev_role in ("system", "assistant") else "assistant"
        if role != expected_role:
            errors.append(
                f"[Conv {index}, Msg {i}] Expected role '{expected_role}', got '{role}'"
            )

        # Validate assistant responses are valid JSON
        if role == "assistant":
            try:
                resp = json.loads(content)
                # Check required fields
                for field in REQUIRED_RESPONSE_FIELDS:
                    if field not in resp:
                        errors.append(
                            f"[Conv {index}, Msg {i}] Assistant response missing '{field}'"
                        )

                # Validate intent
                intent = resp.get("intent")
                if intent and intent not in VALID_INTENTS:
                    errors.append(
                        f"[Conv {index}, Msg {i}] Invalid intent: '{intent}'"
                    )

                # Validate confidence
                confidence = resp.get("confidence")
                if confidence is not None and not (0.0 <= confidence <= 1.0):
                    errors.append(
                        f"[Conv {index}, Msg {i}] Confidence {confidence} out of range [0, 1]"
                    )

                # Validate leadSignals
                ls = resp.get("leadSignals", {})
                if ls:
                    for field in REQUIRED_LEAD_SIGNAL_FIELDS:
                        if field not in ls:
                            errors.append(
                                f"[Conv {index}, Msg {i}] leadSignals missing '{field}'"
                            )

                # Validate suggestedAction
                action = resp.get("suggestedAction")
                if action not in VALID_SUGGESTED_ACTIONS:
                    errors.append(
                        f"[Conv {index}, Msg {i}] Invalid suggestedAction: '{action}'"
                    )

                # Check response is not empty
                if not resp.get("response", "").strip():
                    errors.append(
                        f"[Conv {index}, Msg {i}] Empty response text"
                    )

            except json.JSONDecodeError as e:
                errors.append(
                    f"[Conv {index}, Msg {i}] Assistant content is not valid JSON: {e}"
                )

        prev_role = role

    return errors


def validate_file(path: Path) -> dict:
    """Validate a JSONL file and return report."""
    print(f"\n🔍 Validando: {path}")

    total = 0
    errors = []
    valid = 0

    with jsonlines.open(path) as reader:
        for i, conv in enumerate(reader):
            total += 1
            conv_errors = validate_conversation(conv, i)
            if conv_errors:
                errors.extend(conv_errors)
            else:
                valid += 1

    error_rate = (total - valid) / total * 100 if total > 0 else 0

    print(f"   Total conversaciones: {total}")
    print(f"   ✅ Válidas: {valid}")
    print(f"   ❌ Con errores: {total - valid} ({error_rate:.1f}%)")

    if errors:
        print(f"\n   🔴 Primeros 20 errores:")
        for err in errors[:20]:
            print(f"      {err}")
        if len(errors) > 20:
            print(f"      ... y {len(errors) - 20} errores más")

    return {
        "file": str(path),
        "total": total,
        "valid": valid,
        "invalid": total - valid,
        "error_rate": error_rate,
        "errors": errors,
    }


# ============================================================
# STATISTICS
# ============================================================

def compute_detailed_stats(path: Path) -> dict:
    """Compute detailed statistics from a JSONL file."""
    print(f"\n📊 Calculando estadísticas: {path}")

    intent_counter = Counter()
    turns_counter = Counter()
    confidence_scores = []
    response_lengths = []
    user_msg_lengths = []
    fallback_count = 0
    transfer_count = 0
    lead_signals = defaultdict(int)
    total = 0

    with jsonlines.open(path) as reader:
        for conv in reader:
            total += 1
            messages = conv.get("messages", [])

            # Count turns
            user_turns = sum(1 for m in messages if m["role"] == "user")
            turns_counter[user_turns] += 1

            for msg in messages:
                if msg["role"] == "user":
                    user_msg_lengths.append(len(msg.get("content", "")))

                elif msg["role"] == "assistant":
                    try:
                        resp = json.loads(msg["content"])
                        intent = resp.get("intent", "Unknown")
                        intent_counter[intent] += 1

                        conf = resp.get("confidence", 0)
                        confidence_scores.append(conf)

                        response_lengths.append(len(resp.get("response", "")))

                        if resp.get("isFallback"):
                            fallback_count += 1

                        if resp.get("suggestedAction") == "TRANSFER_TO_AGENT":
                            transfer_count += 1

                        ls = resp.get("leadSignals", {})
                        for signal, value in ls.items():
                            if value:
                                lead_signals[signal] += 1

                    except (json.JSONDecodeError, AttributeError):
                        pass

    # Compute averages
    avg_confidence = sum(confidence_scores) / len(confidence_scores) if confidence_scores else 0
    avg_response_len = sum(response_lengths) / len(response_lengths) if response_lengths else 0
    avg_user_len = sum(user_msg_lengths) / len(user_msg_lengths) if user_msg_lengths else 0
    avg_turns = sum(k * v for k, v in turns_counter.items()) / total if total > 0 else 0

    stats = {
        "file": str(path),
        "total_conversations": total,
        "avg_turns_per_conversation": round(avg_turns, 2),
        "avg_confidence": round(avg_confidence, 3),
        "avg_response_length_chars": round(avg_response_len, 0),
        "avg_user_message_length_chars": round(avg_user_len, 0),
        "fallback_rate": round(fallback_count / sum(intent_counter.values()) * 100, 2) if intent_counter else 0,
        "transfer_rate": round(transfer_count / sum(intent_counter.values()) * 100, 2) if intent_counter else 0,
        "intent_distribution": dict(intent_counter.most_common()),
        "turns_distribution": dict(sorted(turns_counter.items())),
        "lead_signals": dict(lead_signals),
    }

    return stats


def print_stats_report(stats: dict):
    """Print a formatted stats report."""
    print(f"\n{'=' * 60}")
    print(f"📊 REPORTE DE ESTADÍSTICAS")
    print(f"{'=' * 60}")

    print(f"\n📋 General:")
    print(f"   Total conversaciones: {stats['total_conversations']}")
    print(f"   Promedio turnos/conv: {stats['avg_turns_per_conversation']}")
    print(f"   Avg confianza: {stats['avg_confidence']:.3f}")
    print(f"   Avg largo respuesta: {stats['avg_response_length_chars']:.0f} chars")
    print(f"   Avg largo msg usuario: {stats['avg_user_message_length_chars']:.0f} chars")
    print(f"   Tasa fallback: {stats['fallback_rate']:.2f}%")
    print(f"   Tasa transferencia: {stats['transfer_rate']:.2f}%")

    print(f"\n🏷️ Distribución de Intents:")
    total_intents = sum(stats["intent_distribution"].values())
    for intent, count in stats["intent_distribution"].items():
        pct = count / total_intents * 100
        bar = "█" * int(pct / 2) + "░" * (25 - int(pct / 2))
        print(f"   {intent:25s} {bar} {count:5d} ({pct:5.1f}%)")

    print(f"\n📈 Distribución de Turnos:")
    for turns, count in sorted(stats["turns_distribution"].items()):
        pct = count / stats["total_conversations"] * 100
        bar = "█" * int(pct / 2) + "░" * (25 - int(pct / 2))
        print(f"   {turns} turnos {bar} {count:5d} ({pct:5.1f}%)")

    print(f"\n🎯 Señales de Lead detectadas:")
    for signal, count in sorted(stats["lead_signals"].items(), key=lambda x: x[1], reverse=True):
        print(f"   {signal:30s} {count:5d}")

    print(f"\n{'=' * 60}")


# ============================================================
# QUALITY CHECKS
# ============================================================

def check_quality(path: Path) -> list:
    """Run quality checks on the dataset."""
    print(f"\n🔬 Control de calidad: {path}")
    warnings = []

    intent_counter = Counter()
    total = 0

    with jsonlines.open(path) as reader:
        for conv in reader:
            total += 1
            for msg in conv.get("messages", []):
                if msg["role"] == "assistant":
                    try:
                        resp = json.loads(msg["content"])
                        intent_counter[resp.get("intent", "Unknown")] += 1

                        # Check: response has emoji
                        response_text = resp.get("response", "")
                        if not any(ord(c) > 0x1F000 for c in response_text):
                            pass  # Not all responses need emoji

                        # Check: confidence is reasonable
                        conf = resp.get("confidence", 0)
                        if resp.get("isFallback") and conf > 0.5:
                            warnings.append(
                                f"Fallback with high confidence ({conf})"
                            )

                        # Check: response length
                        if len(response_text) < 20:
                            warnings.append(
                                f"Very short response ({len(response_text)} chars): "
                                f"'{response_text[:50]}...'"
                            )
                        if len(response_text) > 2000:
                            warnings.append(
                                f"Very long response ({len(response_text)} chars)"
                            )

                    except (json.JSONDecodeError, AttributeError):
                        pass

    # Check intent balance
    total_intents = sum(intent_counter.values())
    for intent, count in intent_counter.items():
        pct = count / total_intents * 100
        if pct > 30:
            warnings.append(f"Intent '{intent}' is overrepresented ({pct:.1f}%)")
        if pct < 0.1 and intent not in ("Fallback", "Other"):
            warnings.append(f"Intent '{intent}' is underrepresented ({pct:.1f}%)")

    # Check missing intents
    found_intents = set(intent_counter.keys())
    important_intents = {
        "Greeting", "VehicleSearch", "VehicleDetails", "VehiclePrice",
        "FinancingInfo", "TestDriveSchedule", "Farewell"
    }
    missing = important_intents - found_intents
    if missing:
        warnings.append(f"Missing important intents: {missing}")

    print(f"   Total warnings: {len(warnings)}")
    if warnings:
        for w in warnings[:30]:
            print(f"   ⚠️  {w}")

    return warnings


# ============================================================
# MAIN
# ============================================================

def main():
    parser = argparse.ArgumentParser(
        description="OKLA Chatbot LLM — Validador de Dataset"
    )
    parser.add_argument(
        "path", type=str,
        help="Archivo JSONL o directorio con archivos JSONL"
    )
    parser.add_argument(
        "--stats", action="store_true",
        help="Mostrar estadísticas detalladas"
    )
    parser.add_argument(
        "--check-quality", action="store_true",
        help="Ejecutar checks de calidad"
    )
    parser.add_argument(
        "--save-report", type=str, default=None,
        help="Guardar reporte en archivo JSON"
    )
    args = parser.parse_args()

    target = Path(args.path)

    if target.is_dir():
        files = list(target.glob("*.jsonl"))
    elif target.is_file():
        files = [target]
    else:
        print(f"❌ No se encontró: {target}")
        sys.exit(1)

    if not files:
        print(f"❌ No se encontraron archivos JSONL en: {target}")
        sys.exit(1)

    print("=" * 60)
    print("🔍 OKLA Chatbot LLM — Validador de Dataset")
    print("=" * 60)

    all_reports = {}

    for f in sorted(files):
        # Always validate
        report = validate_file(f)
        all_reports[f.name] = {"validation": report}

        # Stats
        if args.stats:
            stats = compute_detailed_stats(f)
            print_stats_report(stats)
            all_reports[f.name]["stats"] = stats

        # Quality
        if args.check_quality:
            warnings = check_quality(f)
            all_reports[f.name]["quality_warnings"] = len(warnings)

    # Save report
    if args.save_report:
        report_path = Path(args.save_report)
        with open(report_path, "w", encoding="utf-8") as f:
            json.dump(all_reports, f, indent=2, ensure_ascii=False, default=str)
        print(f"\n💾 Reporte guardado en: {report_path}")

    print(f"\n✅ Validación completada.")


if __name__ == "__main__":
    main()
