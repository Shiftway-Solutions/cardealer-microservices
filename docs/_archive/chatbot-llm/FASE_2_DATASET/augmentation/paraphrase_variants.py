#!/usr/bin/env python3
"""
OKLA Chatbot LLM — Aumentación de Dataset con Parafraseo
==========================================================

Genera variantes de los mensajes de usuario usando:
1. Sinónimos dominicanos
2. Variaciones de formato (con/sin tildes, mayúsculas)
3. Errores tipográficos comunes
4. Variaciones de WhatsApp/SMS

Uso:
    python augmentation/paraphrase_variants.py input.jsonl output.jsonl --factor 2
"""

import argparse
import json
import os
import random
import re
import sys
from copy import deepcopy
from pathlib import Path

try:
    import jsonlines
    from tqdm import tqdm
except ImportError:
    os.system(f"{sys.executable} -m pip install tqdm jsonlines")
    import jsonlines
    from tqdm import tqdm


# ============================================================
# DOMINICAN SPANISH PARAPHRASING RULES
# ============================================================

# Synonym groups for common words in vehicle shopping
SYNONYM_GROUPS = {
    # Vehicle types
    "suv": ["yipeta", "jeepeta", "camioneta", "SUV"],
    "sedán": ["carro", "sedan", "sedán", "vehículo"],
    "pickup": ["camioneta", "pickup", "la doble cabina"],
    "van": ["guagua", "van", "minivan"],
    "hatchback": ["hatchback", "carro compacto"],

    # Price related
    "barato": ["pela'o", "económico", "accesible", "que no sea caro", "barato"],
    "caro": ["costoso", "premium", "de lujo", "alto"],
    "precio": ["valor", "costo", "cuánto sale", "a cómo"],
    "presupuesto": ["budget", "plata", "cuartos", "dinero disponible"],

    # Actions
    "busco": ["quiero", "necesito", "ando buscando", "estoy buscando", "me interesa"],
    "ver": ["mirar", "checar", "revisar", "echarle un ojo"],
    "comprar": ["adquirir", "llevarme", "coger", "hacerme con"],

    # Quality/State
    "nuevo": ["0km", "del año", "nuevecito", "sin uso"],
    "usado": ["de uso", "second hand", "seminuevo", "de segunda"],
    "bueno": ["chivo", "jevi", "chevere", "de primera", "excelente"],
    "grande": ["espacioso", "grande", "amplio", "familiar"],
    "pequeño": ["compacto", "chiquito", "pequeño"],

    # General
    "ayuda": ["dame un mano", "ayúdame", "necesito apoyo", "auxilio"],
    "información": ["info", "detalles", "datos", "especificaciones"],
    "disponible": ["hay", "tienen", "queda"],
    "financiamiento": ["préstamo", "financiar", "plan de pago", "crédito"],
    "garantía": ["warranty", "garantía", "cobertura"],
}

# Common typo patterns (intentional for training data)
TYPO_RULES = [
    (r"qu", "k"),          # que → ke
    (r"ción", "cion"),      # acción → accion
    (r"á", "a"),            # está → esta
    (r"é", "e"),            # qué → que
    (r"í", "i"),            # aquí → aqui
    (r"ó", "o"),            # cómo → como
    (r"ú", "u"),            # tú → tu
]

# WhatsApp/SMS abbreviations
WHATSAPP_ABBREV = {
    "que": "q",
    "para": "pa",
    "por": "x",
    "porque": "xq",
    "también": "tb",
    "bueno": "weno",
    "bien": "bn",
    "nada": "na",
    "todo": "to",
    "tengo": "tng",
    "tienen": "tnen",
    "cuánto": "cuanto",
    "dónde": "donde",
    "cómo": "como",
    "está": "ta",
    "estoy": "toy",
    "vamos": "vamo",
    "verdad": "velda",
    "necesito": "ncsto",
    "número": "num",
    "teléfono": "tel",
    "mensaje": "msg",
    "información": "info",
}

# Dominican filler expressions to randomly insert
FILLERS = [
    "dimelo ahí", "mira", "oye", "dime a ver",
    "tú sabes", "verdad", "mano", "mi pana",
    "compadre", "loco", "hermano",
]


# ============================================================
# PARAPHRASING FUNCTIONS
# ============================================================

def apply_synonyms(text: str, probability: float = 0.3) -> str:
    """Replace words with Dominican synonyms."""
    words = text.lower().split()
    result = []
    for word in words:
        clean = word.strip("?!.,¿¡")
        replaced = False
        for key, synonyms in SYNONYM_GROUPS.items():
            if clean == key or clean in synonyms:
                if random.random() < probability:
                    replacement = random.choice(synonyms)
                    # Preserve punctuation
                    punct = word[len(clean):] if len(word) > len(clean) else ""
                    result.append(replacement + punct)
                    replaced = True
                    break
        if not replaced:
            result.append(word)
    return " ".join(result)


def apply_typos(text: str, probability: float = 0.2) -> str:
    """Remove accents and apply common typos."""
    if random.random() > probability:
        return text
    result = text
    for pattern, replacement in TYPO_RULES:
        if random.random() < 0.3:
            result = re.sub(pattern, replacement, result)
    return result


def apply_whatsapp_style(text: str, probability: float = 0.15) -> str:
    """Convert to WhatsApp/SMS abbreviation style."""
    if random.random() > probability:
        return text

    words = text.split()
    result = []
    for word in words:
        clean = word.lower().strip("?!.,¿¡")
        if clean in WHATSAPP_ABBREV and random.random() < 0.5:
            punct = word[len(clean):] if len(word) > len(clean) else ""
            result.append(WHATSAPP_ABBREV[clean] + punct)
        else:
            result.append(word)

    # Sometimes remove all punctuation
    text = " ".join(result)
    if random.random() < 0.3:
        text = text.replace("?", "").replace("¿", "").replace("!", "").replace("¡", "")

    return text


def add_filler(text: str, probability: float = 0.1) -> str:
    """Add Dominican filler expressions."""
    if random.random() > probability:
        return text

    filler = random.choice(FILLERS)
    if random.random() < 0.5:
        return f"{filler}, {text.lower()}"
    else:
        return f"{text.rstrip('?!.')} {filler}"


def change_case(text: str, probability: float = 0.2) -> str:
    """Randomly change case (all lowercase, all caps, etc.)."""
    if random.random() > probability:
        return text

    choice = random.random()
    if choice < 0.5:
        return text.lower()
    elif choice < 0.8:
        return text.upper()
    else:
        return text  # Keep as-is


def paraphrase_user_message(text: str) -> str:
    """Apply random paraphrasing transformations to a user message."""
    result = text

    # Apply transformations in random order
    transforms = [
        (apply_synonyms, 0.3),
        (apply_typos, 0.2),
        (apply_whatsapp_style, 0.15),
        (add_filler, 0.1),
        (change_case, 0.2),
    ]
    random.shuffle(transforms)

    for fn, prob in transforms:
        result = fn(result, prob)

    # Ensure it's different from original
    if result == text:
        result = apply_synonyms(text, 0.5)

    return result.strip()


# ============================================================
# AUGMENTATION PIPELINE
# ============================================================

def augment_conversation(conv: dict) -> dict:
    """Create an augmented copy of a conversation."""
    augmented = deepcopy(conv)

    for msg in augmented["messages"]:
        if msg["role"] == "user":
            msg["content"] = paraphrase_user_message(msg["content"])

    return augmented


def augment_dataset(input_path: Path, output_path: Path, factor: int = 2, seed: int = 42):
    """Augment a JSONL dataset by generating paraphrased variants."""
    random.seed(seed)

    print(f"\n🔄 Aumentando dataset:")
    print(f"   Input: {input_path}")
    print(f"   Output: {output_path}")
    print(f"   Factor: {factor}x")

    # Read original
    original = []
    with jsonlines.open(input_path) as reader:
        for conv in reader:
            original.append(conv)

    print(f"   Originales: {len(original)}")

    # Generate augmented versions
    augmented_all = list(original)  # Start with originals

    for i in range(factor - 1):
        print(f"\n   Generando variante {i + 2}/{factor}...")
        for conv in tqdm(original, desc=f"   Variante {i + 2}"):
            aug = augment_conversation(conv)
            augmented_all.append(aug)

    # Shuffle
    random.shuffle(augmented_all)

    # Save
    with jsonlines.open(output_path, mode="w") as writer:
        for conv in augmented_all:
            writer.write(conv)

    print(f"\n✅ Dataset aumentado: {len(augmented_all)} conversaciones")
    print(f"   ({len(original)} originales + {len(augmented_all) - len(original)} variantes)")
    print(f"   Guardado en: {output_path}")


# ============================================================
# MAIN
# ============================================================

def main():
    parser = argparse.ArgumentParser(
        description="OKLA Chatbot LLM — Aumentación de Dataset"
    )
    parser.add_argument(
        "input", type=str,
        help="Archivo JSONL de entrada"
    )
    parser.add_argument(
        "output", type=str,
        help="Archivo JSONL de salida aumentado"
    )
    parser.add_argument(
        "--factor", type=int, default=2,
        help="Factor de aumentación (default: 2x)"
    )
    parser.add_argument(
        "--seed", type=int, default=42,
        help="Seed para reproducibilidad (default: 42)"
    )
    args = parser.parse_args()

    input_path = Path(args.input)
    output_path = Path(args.output)

    if not input_path.exists():
        print(f"❌ No se encontró: {input_path}")
        sys.exit(1)

    output_path.parent.mkdir(parents=True, exist_ok=True)
    augment_dataset(input_path, output_path, args.factor, args.seed)


if __name__ == "__main__":
    main()
