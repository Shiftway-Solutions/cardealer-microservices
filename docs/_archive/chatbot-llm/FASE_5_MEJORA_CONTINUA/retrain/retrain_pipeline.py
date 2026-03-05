#!/usr/bin/env python3
"""
OKLA Chatbot LLM — Re-training Automation Pipeline
====================================================

Pipeline automatizado para re-entrenar el modelo Llama 3 con nuevos datos:
    1. Recopilar nuevas conversaciones + feedback positivo + correcciones
    2. Validar y formatear datos
    3. Mezclar con dataset original (preservar conocimiento)
    4. Generar script de fine-tuning actualizado
    5. Exportar modelo GGUF
    6. Evaluar nuevo modelo vs. modelo actual
    7. Promover si mejora (o rollback)

Uso:
    # Paso 1: Recopilar y preparar datos
    python retrain_pipeline.py collect \
        --feedback-dir ../feedback/feedback_data \
        --conversations-db postgres://... \
        --original-dataset ../../FASE_2_DATASET/output/train.jsonl \
        --output-dir ./retrain_data

    # Paso 2: Generar dataset mezclado
    python retrain_pipeline.py prepare \
        --retrain-dir ./retrain_data \
        --output ./retrain_data/merged_dataset.jsonl \
        --original-ratio 0.7

    # Paso 3: Validar dataset
    python retrain_pipeline.py validate \
        --dataset ./retrain_data/merged_dataset.jsonl

    # Paso 4: Generar script de Colab
    python retrain_pipeline.py generate-script \
        --dataset ./retrain_data/merged_dataset.jsonl \
        --output ./retrain_scripts/retrain_v2.py
"""

import argparse
import hashlib
import json
import logging
import os
import random
import re
import shutil
import statistics
from collections import Counter, defaultdict
from datetime import datetime
from pathlib import Path
from typing import Any

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s"
)
logger = logging.getLogger("okla-retrain")


# ============================================================
# CONSTANTS
# ============================================================

SYSTEM_PROMPT = (
    "Eres OKLA Assistant, el asistente virtual oficial de OKLA (okla.com.do), "
    "la plataforma líder de compra y venta de vehículos en República Dominicana. "
    "Responde SIEMPRE en español dominicano de forma profesional, amigable y concisa. "
    "Tu respuesta DEBE ser un JSON válido con los campos: response, intent, confidence, parameters. "
    "Si detectas intención de compra, incluye leadSignals con purchaseIntent, urgency, preferredContact."
)

MIN_MESSAGE_LENGTH = 5
MAX_MESSAGE_LENGTH = 2000
MAX_CONVERSATION_TURNS = 20

VALID_INTENTS = [
    "vehicle_search", "vehicle_details", "price_inquiry",
    "financing_info", "dealer_info", "test_drive",
    "trade_in", "insurance_info", "vehicle_comparison",
    "general_greeting", "general_farewell", "general_help",
    "complaint", "lead_capture", "negotiation",
    "documentation_info"
]


# ============================================================
# DATA COLLECTION
# ============================================================

def collect_from_feedback(feedback_dir: str, min_rating: int = 4) -> list[dict]:
    """Collect positive feedback and corrections from feedback files."""
    examples = []
    feedback_path = Path(feedback_dir)

    if not feedback_path.exists():
        logger.warning(f"Feedback directory not found: {feedback_dir}")
        return examples

    for jsonl_file in sorted(feedback_path.glob("feedback_*.jsonl")):
        with open(jsonl_file, "r", encoding="utf-8") as f:
            for line in f:
                line = line.strip()
                if not line:
                    continue
                try:
                    entry = json.loads(line)
                except json.JSONDecodeError:
                    continue

                # Use corrections as improved training data
                if entry.get("correction"):
                    examples.append({
                        "messages": [
                            {"role": "system", "content": SYSTEM_PROMPT},
                            {"role": "user", "content": entry["user_message"]},
                            {"role": "assistant", "content": entry["correction"]}
                        ],
                        "metadata": {
                            "source": "feedback_correction",
                            "original_intent": entry.get("intent"),
                            "timestamp": entry.get("timestamp")
                        }
                    })

                # Use highly-rated responses
                elif (entry.get("thumbs") == "up"
                      or (entry.get("rating") and entry["rating"] >= min_rating)):
                    examples.append({
                        "messages": [
                            {"role": "system", "content": SYSTEM_PROMPT},
                            {"role": "user", "content": entry["user_message"]},
                            {"role": "assistant", "content": entry["bot_response"]}
                        ],
                        "metadata": {
                            "source": "feedback_positive",
                            "intent": entry.get("intent"),
                            "rating": entry.get("rating"),
                            "timestamp": entry.get("timestamp")
                        }
                    })

    logger.info(f"Collected {len(examples)} examples from feedback")
    return examples


def collect_from_conversations_export(export_path: str) -> list[dict]:
    """
    Collect from exported conversation logs (JSONL format).
    This data comes from ChatbotService's conversation export endpoint.
    """
    examples = []

    if not os.path.exists(export_path):
        logger.warning(f"Conversation export not found: {export_path}")
        return examples

    with open(export_path, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            try:
                conv = json.loads(line)
            except json.JSONDecodeError:
                continue

            messages = conv.get("messages", [])
            if len(messages) < 2:
                continue

            # Only include conversations that ended positively
            # (had a resolution or lead capture)
            metadata = conv.get("metadata", {})
            if metadata.get("lead_captured") or metadata.get("satisfaction_rating", 0) >= 4:
                formatted = [{"role": "system", "content": SYSTEM_PROMPT}]
                for msg in messages:
                    role = msg.get("role", "user")
                    if role in ("user", "assistant"):
                        formatted.append({
                            "role": role,
                            "content": msg["content"]
                        })

                if len(formatted) >= 3:  # system + at least user + assistant
                    examples.append({
                        "messages": formatted,
                        "metadata": {
                            "source": "conversation_log",
                            "session_id": conv.get("session_id"),
                            "intent": metadata.get("intent"),
                            "timestamp": conv.get("timestamp")
                        }
                    })

    logger.info(f"Collected {len(examples)} examples from conversation logs")
    return examples


# ============================================================
# DATA PREPARATION
# ============================================================

def deduplicate(examples: list[dict]) -> list[dict]:
    """Remove duplicate examples based on content hash."""
    seen = set()
    unique = []

    for ex in examples:
        # Hash based on user messages only
        user_msgs = " ".join(
            m["content"] for m in ex.get("messages", [])
            if m.get("role") == "user"
        )
        h = hashlib.md5(user_msgs.encode()).hexdigest()
        if h not in seen:
            seen.add(h)
            unique.append(ex)

    removed = len(examples) - len(unique)
    if removed > 0:
        logger.info(f"Removed {removed} duplicate examples")
    return unique


def validate_example(example: dict) -> tuple[bool, str]:
    """Validate a single training example."""
    messages = example.get("messages", [])

    if not messages:
        return False, "No messages"

    if len(messages) < 2:
        return False, "Less than 2 messages"

    for msg in messages:
        if "role" not in msg or "content" not in msg:
            return False, "Missing role or content"

        if msg["role"] not in ("system", "user", "assistant"):
            return False, f"Invalid role: {msg['role']}"

        content = msg["content"]
        if not content or len(content.strip()) < MIN_MESSAGE_LENGTH:
            return False, f"Content too short: {len(content)} chars"

        if len(content) > MAX_MESSAGE_LENGTH:
            return False, f"Content too long: {len(content)} chars"

    # Check conversation structure: should alternate user/assistant
    non_system = [m for m in messages if m["role"] != "system"]
    if not non_system:
        return False, "No user/assistant messages"

    if non_system[0]["role"] != "user":
        return False, "First non-system message should be from user"

    return True, "OK"


def merge_datasets(
    original_path: str,
    new_examples: list[dict],
    original_ratio: float = 0.7
) -> list[dict]:
    """
    Merge original dataset with new examples.
    Uses original_ratio to control the balance.

    original_ratio = 0.7 means 70% original, 30% new data
    """
    # Load original
    original = []
    if os.path.exists(original_path):
        with open(original_path, "r", encoding="utf-8") as f:
            for line in f:
                line = line.strip()
                if line:
                    try:
                        original.append(json.loads(line))
                    except json.JSONDecodeError:
                        continue
    logger.info(f"Original dataset: {len(original)} examples")

    if not original:
        logger.warning("No original dataset found, using only new data")
        return new_examples

    if not new_examples:
        logger.warning("No new examples, returning original dataset")
        return original

    # Calculate target sizes
    total_target = len(original) + len(new_examples)
    original_target = int(total_target * original_ratio)
    new_target = total_target - original_target

    # Sample from each
    if len(original) > original_target:
        sampled_original = random.sample(original, original_target)
    else:
        sampled_original = original

    if len(new_examples) > new_target:
        sampled_new = random.sample(new_examples, new_target)
    else:
        sampled_new = new_examples

    merged = sampled_original + sampled_new
    random.shuffle(merged)

    logger.info(
        f"Merged dataset: {len(merged)} total "
        f"({len(sampled_original)} original + {len(sampled_new)} new)"
    )
    return merged


def split_dataset(
    examples: list[dict],
    train_ratio: float = 0.85,
    eval_ratio: float = 0.10,
    test_ratio: float = 0.05
) -> tuple[list, list, list]:
    """Split into train/eval/test."""
    random.shuffle(examples)
    n = len(examples)
    train_end = int(n * train_ratio)
    eval_end = train_end + int(n * eval_ratio)

    train = examples[:train_end]
    eval_set = examples[train_end:eval_end]
    test = examples[eval_end:]

    logger.info(f"Split: {len(train)} train, {len(eval_set)} eval, {len(test)} test")
    return train, eval_set, test


# ============================================================
# COLAB SCRIPT GENERATOR
# ============================================================

def generate_retrain_script(
    dataset_path: str,
    output_path: str,
    model_version: str = "v2",
    epochs: int = 3,
    learning_rate: float = 2e-4,
    lora_r: int = 16,
    lora_alpha: int = 32
) -> str:
    """Generate a Python fine-tuning script for Colab."""

    script = f'''#!/usr/bin/env python3
"""
OKLA Chatbot LLM — Re-training Script {model_version}
Auto-generated on {datetime.now().strftime('%Y-%m-%d %H:%M')}

Execute in Google Colab with A100/T4 GPU.
"""

# ============================================================
# 1. SETUP
# ============================================================

!pip install -q unsloth transformers datasets peft trl accelerate bitsandbytes

import json
import torch
from unsloth import FastLanguageModel
from datasets import Dataset
from trl import SFTTrainer
from transformers import TrainingArguments

# ============================================================
# 2. LOAD MODEL (same base as original fine-tune)
# ============================================================

model, tokenizer = FastLanguageModel.from_pretrained(
    model_name="unsloth/llama-3-8b-Instruct-bnb-4bit",
    max_seq_length=2048,
    dtype=None,  # Auto-detect
    load_in_4bit=True,
)

model = FastLanguageModel.get_peft_model(
    model,
    r={lora_r},
    target_modules=["q_proj", "k_proj", "v_proj", "o_proj",
                     "gate_proj", "up_proj", "down_proj"],
    lora_alpha={lora_alpha},
    lora_dropout=0.05,
    bias="none",
    use_gradient_checkpointing="unsloth",
    random_state=42,
)

# ============================================================
# 3. LOAD DATASET
# ============================================================

def load_jsonl(path):
    examples = []
    with open(path, "r") as f:
        for line in f:
            if line.strip():
                examples.append(json.loads(line))
    return examples

train_data = load_jsonl("{dataset_path.replace('merged_dataset', 'train')}")
eval_data = load_jsonl("{dataset_path.replace('merged_dataset', 'eval')}")

print(f"Train: {{len(train_data)}}, Eval: {{len(eval_data)}}")

# Format for training
def format_example(example):
    messages = example.get("messages", [])
    text = tokenizer.apply_chat_template(messages, tokenize=False, add_generation_prompt=False)
    return {{"text": text}}

train_dataset = Dataset.from_list([format_example(ex) for ex in train_data])
eval_dataset = Dataset.from_list([format_example(ex) for ex in eval_data])

# ============================================================
# 4. TRAIN
# ============================================================

trainer = SFTTrainer(
    model=model,
    tokenizer=tokenizer,
    train_dataset=train_dataset,
    eval_dataset=eval_dataset,
    dataset_text_field="text",
    max_seq_length=2048,
    dataset_num_proc=2,
    packing=True,
    args=TrainingArguments(
        output_dir="./okla-retrain-{model_version}",
        per_device_train_batch_size=2,
        gradient_accumulation_steps=4,
        num_train_epochs={epochs},
        learning_rate={learning_rate},
        lr_scheduler_type="cosine",
        warmup_ratio=0.1,
        fp16=not torch.cuda.is_bf16_supported(),
        bf16=torch.cuda.is_bf16_supported(),
        logging_steps=10,
        eval_strategy="steps",
        eval_steps=50,
        save_strategy="steps",
        save_steps=100,
        save_total_limit=3,
        weight_decay=0.01,
        optim="adamw_8bit",
        seed=42,
        report_to="none",
    ),
)

print("🚀 Starting re-training {model_version}...")
trainer.train()

# ============================================================
# 5. SAVE & EXPORT
# ============================================================

# Save LoRA adapter
model.save_pretrained("./okla-retrain-{model_version}/final")
tokenizer.save_pretrained("./okla-retrain-{model_version}/final")

# Export to GGUF Q4_K_M for production
model.save_pretrained_gguf(
    "./okla-retrain-{model_version}/gguf",
    tokenizer,
    quantization_method="q4_k_m",
)

print("✅ Re-training {model_version} complete!")
print(f"   GGUF model: ./okla-retrain-{model_version}/gguf/")
print(f"   Upload to: HuggingFace Hub or direct to K8s PVC")
'''

    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    with open(output_path, "w", encoding="utf-8") as f:
        f.write(script)

    logger.info(f"Re-training script saved to {output_path}")
    return output_path


# ============================================================
# VERSION MANAGEMENT
# ============================================================

class ModelVersionManager:
    """Track model versions and their performance metrics."""

    def __init__(self, registry_path: str):
        self.registry_path = Path(registry_path)
        self.registry_path.parent.mkdir(parents=True, exist_ok=True)
        self.versions = self._load()

    def _load(self) -> list[dict]:
        if self.registry_path.exists():
            with open(self.registry_path, "r") as f:
                return json.load(f)
        return []

    def _save(self):
        with open(self.registry_path, "w") as f:
            json.dump(self.versions, f, indent=2, ensure_ascii=False, default=str)

    def register(
        self,
        version: str,
        model_path: str,
        training_data_count: int,
        eval_results: dict | None = None
    ) -> dict:
        """Register a new model version."""
        entry = {
            "version": version,
            "model_path": model_path,
            "created_at": datetime.now().isoformat(),
            "training_data_count": training_data_count,
            "eval_results": eval_results or {},
            "status": "candidate",  # candidate → promoted → retired
            "promoted_at": None,
            "retired_at": None
        }
        self.versions.append(entry)
        self._save()
        logger.info(f"Registered model version: {version}")
        return entry

    def promote(self, version: str) -> bool:
        """Promote a candidate model to production."""
        for v in self.versions:
            if v["version"] == version and v["status"] == "candidate":
                # Retire current production
                for prev in self.versions:
                    if prev["status"] == "promoted":
                        prev["status"] = "retired"
                        prev["retired_at"] = datetime.now().isoformat()

                v["status"] = "promoted"
                v["promoted_at"] = datetime.now().isoformat()
                self._save()
                logger.info(f"Promoted model {version} to production")
                return True
        return False

    def get_current(self) -> dict | None:
        """Get the currently promoted model."""
        for v in reversed(self.versions):
            if v["status"] == "promoted":
                return v
        return None

    def get_history(self) -> list[dict]:
        """Get version history."""
        return list(reversed(self.versions))

    def should_retrain(
        self,
        new_feedback_count: int,
        min_feedback: int = 50,
        max_days_since_training: int = 30
    ) -> tuple[bool, str]:
        """Determine if re-training is needed."""
        current = self.get_current()
        if not current:
            return True, "No model in production"

        # Check feedback volume
        if new_feedback_count >= min_feedback:
            return True, f"Sufficient new feedback: {new_feedback_count} >= {min_feedback}"

        # Check time since last training
        created = datetime.fromisoformat(current["created_at"])
        days_old = (datetime.now() - created).days
        if days_old >= max_days_since_training:
            return True, f"Model is {days_old} days old (max: {max_days_since_training})"

        # Check eval results degradation
        eval_results = current.get("eval_results", {})
        intent_acc = eval_results.get("intent_accuracy", {}).get("overall_accuracy", 100)
        if intent_acc < 70:
            return True, f"Intent accuracy degraded: {intent_acc}%"

        return False, f"No re-training needed (feedback: {new_feedback_count}, age: {days_old}d)"


# ============================================================
# CLI
# ============================================================

def main():
    parser = argparse.ArgumentParser(description="OKLA Re-training Pipeline")
    subparsers = parser.add_subparsers(dest="command", required=True)

    # Collect
    collect_p = subparsers.add_parser("collect", help="Collect new training data")
    collect_p.add_argument("--feedback-dir", required=True)
    collect_p.add_argument("--conversations-export", default=None)
    collect_p.add_argument("--output-dir", required=True)
    collect_p.add_argument("--min-rating", type=int, default=4)

    # Prepare
    prepare_p = subparsers.add_parser("prepare", help="Prepare merged dataset")
    prepare_p.add_argument("--retrain-dir", required=True)
    prepare_p.add_argument("--original-dataset", required=True)
    prepare_p.add_argument("--output", required=True)
    prepare_p.add_argument("--original-ratio", type=float, default=0.7)

    # Validate
    validate_p = subparsers.add_parser("validate", help="Validate dataset")
    validate_p.add_argument("--dataset", required=True)

    # Generate script
    gen_p = subparsers.add_parser("generate-script", help="Generate Colab re-training script")
    gen_p.add_argument("--dataset", required=True)
    gen_p.add_argument("--output", required=True)
    gen_p.add_argument("--version", default="v2")
    gen_p.add_argument("--epochs", type=int, default=3)

    # Check
    check_p = subparsers.add_parser("check", help="Check if re-training is needed")
    check_p.add_argument("--registry", required=True)
    check_p.add_argument("--feedback-dir", required=True)

    args = parser.parse_args()

    if args.command == "collect":
        os.makedirs(args.output_dir, exist_ok=True)

        all_examples = []
        # From feedback
        feedback_examples = collect_from_feedback(args.feedback_dir, args.min_rating)
        all_examples.extend(feedback_examples)

        # From conversation exports
        if args.conversations_export:
            conv_examples = collect_from_conversations_export(args.conversations_export)
            all_examples.extend(conv_examples)

        # Deduplicate
        all_examples = deduplicate(all_examples)

        # Validate
        valid = []
        invalid_count = 0
        for ex in all_examples:
            is_valid, reason = validate_example(ex)
            if is_valid:
                valid.append(ex)
            else:
                invalid_count += 1

        # Save
        output_path = os.path.join(args.output_dir, "new_examples.jsonl")
        with open(output_path, "w", encoding="utf-8") as f:
            for ex in valid:
                f.write(json.dumps(ex, ensure_ascii=False) + "\n")

        print(f"\n✅ Collected: {len(valid)} valid examples ({invalid_count} invalid discarded)")
        print(f"   Saved to: {output_path}")

    elif args.command == "prepare":
        # Load new examples
        new_path = os.path.join(args.retrain_dir, "new_examples.jsonl")
        new_examples = []
        if os.path.exists(new_path):
            with open(new_path, "r") as f:
                for line in f:
                    if line.strip():
                        new_examples.append(json.loads(line))

        # Merge
        merged = merge_datasets(args.original_dataset, new_examples, args.original_ratio)

        # Split
        train, eval_set, test = split_dataset(merged)

        # Save
        base_dir = os.path.dirname(args.output)
        os.makedirs(base_dir, exist_ok=True)

        for name, data in [("train", train), ("eval", eval_set), ("test", test)]:
            path = os.path.join(base_dir, f"{name}.jsonl")
            with open(path, "w") as f:
                for ex in data:
                    f.write(json.dumps(ex, ensure_ascii=False) + "\n")

        print(f"\n✅ Prepared dataset: {len(train)} train, {len(eval_set)} eval, {len(test)} test")

    elif args.command == "validate":
        valid_count = 0
        invalid_count = 0
        errors = Counter()

        with open(args.dataset, "r") as f:
            for i, line in enumerate(f, 1):
                if not line.strip():
                    continue
                try:
                    ex = json.loads(line)
                    is_valid, reason = validate_example(ex)
                    if is_valid:
                        valid_count += 1
                    else:
                        invalid_count += 1
                        errors[reason] += 1
                except json.JSONDecodeError:
                    invalid_count += 1
                    errors["JSON parse error"] += 1

        total = valid_count + invalid_count
        print(f"\n📊 Validation Results:")
        print(f"   Total: {total}")
        print(f"   Valid: {valid_count} ({valid_count/total*100:.1f}%)")
        print(f"   Invalid: {invalid_count}")
        if errors:
            print(f"\n   Errors:")
            for err, count in errors.most_common():
                print(f"     - {err}: {count}")

    elif args.command == "generate-script":
        path = generate_retrain_script(
            dataset_path=args.dataset,
            output_path=args.output,
            model_version=args.version,
            epochs=args.epochs
        )
        print(f"\n✅ Re-training script: {path}")

    elif args.command == "check":
        mgr = ModelVersionManager(args.registry)
        # Count feedback files
        feedback_count = 0
        fd = Path(args.feedback_dir)
        if fd.exists():
            for jsonl in fd.glob("feedback_*.jsonl"):
                with open(jsonl) as f:
                    feedback_count += sum(1 for line in f if line.strip())

        needed, reason = mgr.should_retrain(feedback_count)
        current = mgr.get_current()

        print(f"\n📊 Re-training Check:")
        print(f"   Current model: {current['version'] if current else 'None'}")
        print(f"   New feedback: {feedback_count}")
        print(f"   Re-training needed: {'✅ YES' if needed else '❌ NO'}")
        print(f"   Reason: {reason}")


if __name__ == "__main__":
    main()
