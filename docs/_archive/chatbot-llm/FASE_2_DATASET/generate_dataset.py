#!/usr/bin/env python3
"""
OKLA Chatbot LLM — Generador de Dataset Sintético v2.0 (Dual-Mode)
====================================================================

Genera conversaciones sintéticas en formato JSONL (chat completion)
para fine-tuning de Llama 3.1 8B con QLoRA.

MODOS:
  1. SingleVehicle   (40%) — El usuario pregunta sobre UN vehículo fijo
  2. DealerInventory (50%) — El usuario navega el inventario de UN dealer
  3. Boundary/Edge   (10%) — Escenarios edge: cross-dealer, legal, fallback

REGLA FUNDAMENTAL:
  TODO el chat opera dentro del contexto de UN SOLO dealer.
  El modelo NO puede comparar vehículos de diferentes dealers.

Uso:
    python generate_dataset.py --count 3000 --output output/
    python generate_dataset.py --count 500 --output output/ --seed 42

Requisitos:
    pip install faker tqdm jsonlines
"""

import argparse
import hashlib
import json
import os
import random
import re
import sys
import unicodedata
from collections import Counter
from copy import deepcopy
from datetime import datetime
from pathlib import Path
from typing import Any

try:
    import jsonlines
    from tqdm import tqdm
except ImportError:
    print("Instalando dependencias: pip install tqdm jsonlines")
    os.system(f"{sys.executable} -m pip install tqdm jsonlines")
    import jsonlines
    from tqdm import tqdm

from conversation_templates import (
    SV_INTENT_REGISTRY,
    DI_INTENT_REGISTRY,
    SV_INTENT_DISTRIBUTION,
    DI_INTENT_DISTRIBUTION,
    SV_MULTI_TURN_CHAINS,
    DI_MULTI_TURN_CHAINS,
    AMBIGUOUS_TEMPLATES,
    BODY_TYPE_SLANG,
    PRICE_EXPRESSIONS,
    AFFIRMATIVES,
)

# ============================================================
# CONFIG
# ============================================================

SCRIPT_DIR = Path(__file__).parent
VEHICLES_FILE = SCRIPT_DIR / "seed_vehicles.json"
DEALERS_FILE = SCRIPT_DIR / "seed_dealers.json"

# Mode distribution
MODE_DISTRIBUTION = {
    "single_vehicle": 0.40,
    "dealer_inventory": 0.50,
    "boundary_edge": 0.10,
}

# Conversation type distribution (within each mode)
CONV_TYPE_DISTRIBUTION = {
    "single_turn": 0.12,
    "short_multi_turn": 0.55,
    "long_multi_turn": 0.33,
}

# ============================================================
# SYSTEM PROMPT TEMPLATES — Match production code exactly
# ============================================================

SV_SYSTEM_PROMPT = """Eres OKLA Bot, asistente virtual del marketplace de vehículos OKLA en República Dominicana.
Estás ayudando a un usuario con un vehículo ESPECÍFICO del dealer "{dealer_name}".
Hablas en español dominicano amigable y profesional.

VEHÍCULO EN CONTEXTO:
- ID: {vehicle_id}
- {year} {make} {model} {trim}
- Precio: RD${price} {sale_tag}
- Combustible: {fuel_type}
- Transmisión: {transmission}
- Kilometraje: {mileage} km
- Color: {color}
- Tipo: {body_type}
- Ubicación: {location}
- Dealer: {dealer_name} | Tel: {dealer_phone}
- Horario: L-V {hours_weekday}, Sáb {hours_saturday}

REGLAS:
1. SOLO habla de ESTE vehículo. No inventes otros.
2. Si el usuario pregunta por otro vehículo, dile que solo puedes ayudar con este y sugiérele visitar el perfil del dealer.
3. Si no sabes algo del vehículo, di "no tengo esa información" y ofrece conectar con un asesor.
4. NUNCA inventes especificaciones, precios o características que no estén listados arriba.
5. Si el usuario quiere comprar o agendar prueba, sugiere contactar al dealer.
6. Detecta señales de compra (presupuesto, test drive, financiamiento, datos de contacto).
7. Responde en español dominicano amigable pero profesional.
8. SIEMPRE responde en formato JSON con los campos: response, intent, confidence, isFallback, parameters, leadSignals, suggestedAction, quickReplies.
9. NUNCA des asesoría legal ni financiera vinculante.
10. Entiendes modismos: "yipeta" (SUV), "guagua" (vehículo/bus), "pela'o" (barato), "tato" (ok), "klk" (¿qué tal?).

PROHIBICIONES LEGALES (RD):
- NUNCA facilites evasión fiscal (Ley 11-92 DGII). Toda venta DEBE facturarse con ITBIS y NCF.
- NUNCA aceptes transacciones anónimas (Ley 155-17 contra Lavado de Activos).
- NUNCA compartas datos personales de clientes (Ley 172-13).
- Si solicitan algo ilegal, rechaza con cortesía y cita la ley aplicable."""

DI_SYSTEM_PROMPT = """Eres OKLA Bot, asistente virtual del dealer "{dealer_name}" en el marketplace OKLA en República Dominicana.
Ayudas a los usuarios a explorar el inventario del dealer.
Hablas en español dominicano amigable y profesional.

INFORMACIÓN DEL DEALER:
- Nombre: {dealer_name}
- Ubicación: {location}
- Teléfono: {dealer_phone}
- Horario: L-V {hours_weekday}, Sáb {hours_saturday}
- Financiamiento con: {financing_partners}
- Trade-in: {trade_in}

INVENTARIO DISPONIBLE ({inv_count} vehículos):
{inventory_summary}

FUNCIONES DISPONIBLES:
- search_inventory: Buscar vehículos con filtros (marca, modelo, precio, tipo, combustible)
- compare_vehicles: Comparar 2-3 vehículos lado a lado (SOLO del inventario de este dealer)
- get_vehicle_details: Ver detalles completos de un vehículo
- schedule_appointment: Agendar prueba de manejo o visita

REGLAS:
1. SOLO recomienda vehículos del INVENTARIO mostrado arriba.
2. Si un vehículo no aparece en el inventario, di "no lo tenemos disponible" y sugiere alternativas del inventario.
3. Para comparaciones, SOLO compara vehículos que están en ESTE inventario. NUNCA compares con vehículos de otros dealers.
4. Cuando presentes vehículos, usa EXACTAMENTE los precios y datos del inventario.
5. Máximo 3-4 vehículos por respuesta de búsqueda.
6. NUNCA inventes vehículos, precios o disponibilidad.
7. NUNCA menciones otros dealers ni compares con la competencia.
8. Detecta señales de compra (presupuesto, test drive, financiamiento, datos de contacto).
9. SIEMPRE responde en formato JSON con los campos: response, intent, confidence, isFallback, parameters, leadSignals, suggestedAction, quickReplies.
10. Entiendes modismos: "yipeta" (SUV), "guagua" (vehículo/bus), "pela'o" (barato), "tato" (ok), "klk" (¿qué tal?).

PROHIBICIONES LEGALES (RD):
- NUNCA facilites evasión fiscal (Ley 11-92 DGII). Toda venta DEBE facturarse con ITBIS y NCF.
- NUNCA aceptes transacciones anónimas (Ley 155-17 contra Lavado de Activos).
- NUNCA compartas datos personales de clientes (Ley 172-13).
- Si solicitan algo ilegal, rechaza con cortesía y cita la ley aplicable."""


# ============================================================
# HELPERS
# ============================================================

def load_json(path: Path) -> list:
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def build_inventory_summary(vehicles: list, max_vehicles: int = 15) -> str:
    """Build compact inventory summary for DealerInventory system prompt.
    Format matches BuildInventorySection() in SessionCommandHandlers.cs.
    """
    selected = random.sample(vehicles, min(max_vehicles, len(vehicles)))
    lines = []
    for v in selected:
        sale_tag = " 🏷️OFERTA" if v.get("isOnSale") else ""
        mileage = v.get("mileage", 0)
        lines.append(
            f"- {v['make']} {v['model']} {v['year']} {v.get('trim', '')} | "
            f"RD${v['price']:,.0f}{sale_tag} | {v.get('fuelType', 'N/A')} | "
            f"{v.get('transmission', 'N/A')} | {mileage:,}km | "
            f"{v.get('exteriorColor', 'N/A')} | ID:{v['id']}"
        )
    return "\n".join(lines)


def build_sv_system_prompt(vehicle: dict, dealer: dict) -> str:
    """Build SingleVehicle system prompt with ONE fixed vehicle."""
    hours = dealer.get("businessHours", {})
    weekday = hours.get("monday", {})
    saturday = hours.get("saturday", {})
    sale_tag = "🏷️OFERTA" if vehicle.get("isOnSale") else ""

    return SV_SYSTEM_PROMPT.format(
        dealer_name=dealer["name"],
        vehicle_id=vehicle["id"],
        year=vehicle["year"],
        make=vehicle["make"],
        model=vehicle["model"],
        trim=vehicle.get("trim", ""),
        price=f"{vehicle['price']:,.0f}",
        sale_tag=sale_tag,
        fuel_type=vehicle.get("fuelType", "N/A"),
        transmission=vehicle.get("transmission", "N/A"),
        mileage=f"{vehicle.get('mileage', 0):,}",
        color=vehicle.get("exteriorColor", "N/A"),
        body_type=vehicle.get("bodyType", "N/A"),
        location=dealer.get("location", "N/A"),
        dealer_phone=dealer.get("phone", "N/A"),
        hours_weekday=f"{weekday.get('open', '8:00')}-{weekday.get('close', '18:00')}" if weekday else "8:00-18:00",
        hours_saturday=f"{saturday.get('open', '9:00')}-{saturday.get('close', '14:00')}" if saturday else "Cerrado",
    )


def build_di_system_prompt(dealer: dict, vehicles: list) -> str:
    """Build DealerInventory system prompt with dealer context + inventory."""
    hours = dealer.get("businessHours", {})
    weekday = hours.get("monday", {})
    saturday = hours.get("saturday", {})

    return DI_SYSTEM_PROMPT.format(
        dealer_name=dealer["name"],
        location=dealer.get("location", "N/A"),
        dealer_phone=dealer.get("phone", "N/A"),
        hours_weekday=f"{weekday.get('open', '8:00')}-{weekday.get('close', '18:00')}" if weekday else "8:00-18:00",
        hours_saturday=f"{saturday.get('open', '9:00')}-{saturday.get('close', '14:00')}" if saturday else "Cerrado",
        financing_partners=", ".join(dealer.get("financingPartners", ["Consultar"])),
        trade_in="Sí" if dealer.get("tradeInAccepted", True) else "No",
        inv_count=len(vehicles),
        inventory_summary=build_inventory_summary(vehicles),
    )


def fill_vehicle_placeholders(template: str, vehicle: dict = None, vehicles: list = None) -> str:
    """Replace {make}, {model}, {year}, {price} placeholders in user templates."""
    text = template
    if vehicle:
        text = text.replace("{make}", vehicle.get("make", ""))
        text = text.replace("{model}", vehicle.get("model", ""))
        text = text.replace("{year}", str(vehicle.get("year", "")))
        text = text.replace("{price}", f"{vehicle.get('price', 0):,.0f}")

    if vehicles and len(vehicles) >= 2:
        text = text.replace("{make1}", vehicles[0].get("make", ""))
        text = text.replace("{model1}", vehicles[0].get("model", ""))
        text = text.replace("{make2}", vehicles[1].get("make", ""))
        text = text.replace("{model2}", vehicles[1].get("model", ""))

    # Trade-in placeholders
    old_makes = ["Toyota", "Honda", "Hyundai", "Nissan", "Kia", "Chevrolet"]
    old_years = ["2016", "2017", "2018", "2019", "2020"]
    text = text.replace("{make_old}", random.choice(old_makes))
    text = text.replace("{year_old}", random.choice(old_years))

    return text


def select_vehicles_for_intent(intent: str, vehicles: list) -> tuple:
    """Select appropriate vehicles based on intent.
    Returns (single_vehicle, vehicle_list).
    """
    if intent == "VehicleComparison":
        body_types = {}
        for v in vehicles:
            bt = v.get("bodyType", "Other")
            body_types.setdefault(bt, []).append(v)
        for bt, vlist in body_types.items():
            if len(vlist) >= 2:
                pair = random.sample(vlist, 2)
                return pair[0], pair
        pair = random.sample(vehicles, min(2, len(vehicles)))
        return pair[0], pair

    elif intent == "VehicleSearch":
        count = random.choice([2, 3])
        selected = random.sample(vehicles, min(count, len(vehicles)))
        return selected[0], selected

    else:
        v = random.choice(vehicles)
        return v, [v]


# ============================================================
# AUGMENTATION: Dominican Spanish + WhatsApp
# ============================================================

# Dominican slang substitutions
SLANG_MAP = {
    "que": ["q", "ke", "k"], "está": ["ta", "esta"], "están": ["tan", "estan"],
    "estoy": ["toy"], "para": ["pa"], "vamos": ["vamo"], "verdad": ["velda", "vdd"],
    "bueno": ["weno", "bno"], "nada": ["na"], "todo": ["to", "tol"],
    "también": ["tb", "tmb", "tambien"], "porque": ["xq", "pq", "porq"],
    "como": ["cm", "kmo"], "tengo": ["tngo"], "tiene": ["tne"], "tienen": ["tnen"],
    "quiero": ["kiero", "qiero"], "puede": ["pue"], "pueden": ["puen"],
    "necesito": ["ncsito", "nesesito"], "precio": ["presio"],
    "cuánto": ["cuanto", "cnto"], "cuanto": ["cnto", "cuant"],
    "carro": ["caro", "karro"], "carros": ["caros", "karros"],
    "vehículo": ["vehiculo", "vehiclo"], "disponible": ["disponble"],
    "información": ["info", "informacion"], "teléfono": ["telefono", "tel"],
    "número": ["numero", "num"], "dónde": ["donde", "dnd"],
    "gracias": ["gracia", "grax", "grs"], "por favor": ["porfa", "xfa", "porfavor"],
    "hola": ["ola", "holaa"], "buenos": ["bno", "wenos"], "días": ["dia", "dias"],
    "mucho": ["muxo", "mcho"], "ahora": ["aora", "ahorita"], "ustedes": ["uds"],
    "mañana": ["manana", "mñna"], "aquí": ["aki", "aqui"],
    "más": ["mas", "ma"], "hasta": ["ata", "hta"], "dinero": ["cuarto", "chelito"],
    "hermano": ["mano", "manin"], "amigo": ["loco", "pana"],
    "está bien": ["ta bien", "ta bn"], "no sé": ["nose", "no c"],
    "qué tal": ["klk", "dime"], "de verdad": ["deverdad", "de velda"],
}

BIGRAM_SLANG = {
    "por favor": ["porfa", "xfa", "porfis"],
    "está bien": ["ta bien", "ta bn", "ok", "dale"],
    "no sé": ["nose", "no c", "ni idea"],
    "qué tal": ["klk", "q tal", "dime"],
    "de verdad": ["deverdad", "de velda", "en serio"],
    "por qué": ["xq", "porq", "pq"],
    "muchas gracias": ["mil gracias", "grax"],
    "buenas tardes": ["bnas tardes", "buena tarde"],
    "cuánto cuesta": ["cuanto e", "a cuanto"],
    "cuánto vale": ["cuanto vale", "cuant sale"],
}

DOMINICAN_INTERJECTIONS = [
    "Dimelo,", "Klk,", "Oye,", "Mano,", "Dime a ver,",
    "Eyyy,", "Alo,", "Weepa,", "Eh,", "Compai,",
]

WHATSAPP_SUFFIXES = [
    " 👀", " 🤔", " 🙏", " 💪", " 🚗", " pls", " plz", " !",
    " !!",  " ???", " ?!", " porfa", " 🔥", " dale", " ok?",
]

ADJACENT_KEYS = {
    'a': 'sqw', 'b': 'vgn', 'c': 'xdv', 'd': 'sfec', 'e': 'wrd',
    'f': 'dgrc', 'g': 'fhtv', 'h': 'gjyn', 'i': 'uok', 'j': 'hkum',
    'k': 'jlio', 'l': 'kop', 'm': 'nj', 'n': 'bmh', 'o': 'iplk',
    'p': 'ol', 'q': 'wa', 'r': 'edt', 's': 'adwz', 't': 'rfgy',
    'u': 'yij', 'v': 'cbf', 'w': 'qase', 'x': 'zsdc', 'y': 'tuh',
    'z': 'xas',
}


def add_typos_and_slang(text: str, probability: float = 0.15) -> str:
    """Apply 6-layer Dominican Spanish augmentation.
    Layers: slang, bigram-slang, accent-strip, typos, casing, interjection+suffix.
    """
    if random.random() > probability:
        return text

    result_words = text.split()

    # Layer 1a: Word-level slang (30% per word)
    for i, word in enumerate(result_words):
        stripped = word.lower().strip("¿?¡!.,;:()")
        trailing = word[-1] if word and word[-1] in "¿?¡!.,;:()" else ""
        if stripped in SLANG_MAP and random.random() < 0.30:
            replacement = random.choice(SLANG_MAP[stripped])
            result_words[i] = replacement + trailing

    # Layer 1b: Bigram slang (35% per match)
    joined = " ".join(result_words)
    for bigram, options in BIGRAM_SLANG.items():
        if bigram in joined.lower() and random.random() < 0.35:
            pattern = re.compile(re.escape(bigram), re.IGNORECASE)
            joined = pattern.sub(random.choice(options), joined, count=1)
    result_words = joined.split()

    # Layer 2: Accent stripping (40%)
    if random.random() < 0.40:
        ACCENT_MAP = str.maketrans("áéíóúÁÉÍÓÚñÑ", "aeiouAEIOUnN")
        result_words = [w.translate(ACCENT_MAP) for w in result_words]

    # Layer 3: Character-level typos (25%)
    if random.random() < 0.25:
        for i, word in enumerate(result_words):
            if len(word) < 3 or random.random() > 0.20:
                continue
            typo_type = random.choice(["swap", "drop", "double", "adjacent"])
            chars = list(word)
            if typo_type == "swap" and len(chars) >= 3:
                idx = random.randint(0, len(chars) - 2)
                chars[idx], chars[idx + 1] = chars[idx + 1], chars[idx]
            elif typo_type == "drop" and len(chars) >= 4:
                idx = random.randint(1, len(chars) - 2)
                chars.pop(idx)
            elif typo_type == "double" and len(chars) >= 3:
                idx = random.randint(0, len(chars) - 1)
                chars.insert(idx, chars[idx])
            elif typo_type == "adjacent":
                idx = random.randint(0, len(chars) - 1)
                c = chars[idx].lower()
                if c in ADJACENT_KEYS:
                    chars[idx] = random.choice(ADJACENT_KEYS[c])
            result_words[i] = "".join(chars)

    # Layer 4: Casing variation (20%)
    if random.random() < 0.20:
        case_style = random.choice(["lower", "no_caps", "first_lower"])
        if case_style == "lower":
            result_words = [w.lower() for w in result_words]
        elif case_style == "no_caps":
            result_words = [w[0].lower() + w[1:] if len(w) > 1 and w[0].isupper() else w
                            for w in result_words]

    # Layer 5: Prepend Dominican interjection (8%)
    if random.random() < 0.08 and result_words:
        first_lower = result_words[0].lower().rstrip(",")
        skip = {"dimelo", "klk", "oye", "mano", "eyyy", "alo", "weepa", "compai", "dime"}
        if first_lower not in skip:
            result_words.insert(0, random.choice(DOMINICAN_INTERJECTIONS))

    # Layer 6: WhatsApp suffix (10%)
    if random.random() < 0.10 and result_words:
        suffix = random.choice(WHATSAPP_SUFFIXES)
        last = result_words[-1]
        if last and last[-1] in "?!.":
            result_words[-1] = last.rstrip("?!.")
        result_words[-1] = result_words[-1] + suffix

    return " ".join(result_words)


# ============================================================
# EMOJI REDUCTION
# ============================================================

def _count_emojis(text: str) -> int:
    return sum(1 for ch in text
               if unicodedata.category(ch) in ('So', 'Sk') or ord(ch) > 0x1F000)

def reduce_emojis(response_text: str, max_emojis: int = 5) -> str:
    emoji_positions = [
        i for i, ch in enumerate(response_text)
        if unicodedata.category(ch) in ('So', 'Sk') or ord(ch) > 0x1F000
    ]
    if len(emoji_positions) <= max_emojis:
        return response_text
    keep_count = random.randint(max(1, max_emojis // 2), max_emojis)
    keep_set = set(random.sample(emoji_positions, keep_count))
    chars = [ch for i, ch in enumerate(response_text)
             if i in keep_set or i not in set(emoji_positions)]
    result = re.sub(r'  +', ' ', ''.join(chars)).strip()
    return result

def apply_emoji_reduction(assistant_content: str, probability: float = 0.40) -> str:
    if random.random() > probability:
        return assistant_content
    try:
        data = json.loads(assistant_content)
        if 'response' in data and isinstance(data['response'], str):
            data['response'] = reduce_emojis(data['response'], max_emojis=random.randint(3, 6))
        return json.dumps(data, ensure_ascii=False)
    except (json.JSONDecodeError, KeyError):
        return assistant_content


# ============================================================
# CONTEXT CONTINUITY (multi-turn references)
# ============================================================

_INTENT_TOPIC_MAP = {
    "Greeting": None, "Farewell": None, "Fallback": None, "OutOfScope": None,
    "LegalRefusal": None, "FrustratedUser": None, "RequestHumanAgent": None,
    "CrossDealerRefusal": None, "VehicleNotInInventory": None,
    "VehicleSearch": "tu búsqueda",
    "VehicleDetails": "el vehículo que estamos viendo",
    "VehiclePrice": "el precio que consultaste",
    "VehicleComparison": "la comparación que hicimos",
    "FinancingInfo": "el financiamiento",
    "TestDriveSchedule": "la prueba de manejo",
    "DealerHours": "nuestro horario",
    "DealerLocation": "nuestra ubicación",
    "ContactRequest": "tu solicitud de contacto",
    "TradeIn": "el trade-in",
    "WarrantyInfo": "la garantía",
    "CashPurchase": "la compra al contado",
    "NegotiatePrice": "la negociación",
    "DocumentsRequired": "los documentos",
    "VehicleHistory": "el historial del vehículo",
}

_CONTINUITY_WITH_VEHICLE = [
    "Sobre {vehicle} que estamos viendo, ",
    "Siguiendo con {vehicle}, ",
    "En cuanto a {vehicle} que te interesa, ",
    "Respecto a {vehicle}, ",
    "Volviendo a {vehicle}, ",
]

_CONTINUITY_WITH_TOPIC = [
    "Siguiendo con {topic}, ",
    "Sobre {topic} que hablamos, ",
    "Con respecto a {topic}, ",
    "En cuanto a {topic}, ",
]

_CONTINUITY_GENERIC = [
    "Claro, siguiendo con tu consulta, ",
    "Perfecto, continuando, ",
    "Muy bien, con respecto a eso, ",
    "Buena pregunta, ",
    "Claro que sí, ",
    "Por supuesto, ",
    "Con gusto te explico, ",
    "Dale, mira, ",
    "Sí claro, ",
]


def inject_context_continuity(
    assistant_json: str,
    turn_index: int,
    previous_intent: str | None,
    current_vehicle: dict | None,
    probability: float = 0.45,
) -> str:
    """Inject contextual continuity phrase into assistant response for turn 2+."""
    if turn_index < 1 or random.random() > probability:
        return assistant_json
    try:
        data = json.loads(assistant_json)
    except json.JSONDecodeError:
        return assistant_json

    response = data.get("response", "")
    current_intent = data.get("intent", "")

    skip_intents = {"Greeting", "Farewell", "Fallback", "OutOfScope",
                    "LegalRefusal", "FrustratedUser", "RequestHumanAgent",
                    "VehicleNotInInventory", "CrossDealerRefusal"}
    if current_intent in skip_intents:
        return assistant_json

    vehicle_label = None
    if current_vehicle:
        make = current_vehicle.get("make", "")
        model = current_vehicle.get("model", "")
        year = current_vehicle.get("year", "")
        if make and model:
            vehicle_label = f"la {make} {model} {year}".strip()

    phrase = None
    if vehicle_label and random.random() < 0.50:
        template = random.choice(_CONTINUITY_WITH_VEHICLE)
        phrase = template.replace("{vehicle}", vehicle_label)
    elif previous_intent and previous_intent in _INTENT_TOPIC_MAP:
        topic = _INTENT_TOPIC_MAP[previous_intent]
        if topic and random.random() < 0.60:
            template = random.choice(_CONTINUITY_WITH_TOPIC)
            phrase = template.replace("{topic}", topic)

    if phrase is None:
        phrase = random.choice(_CONTINUITY_GENERIC)

    if response and response[0].isalpha() and response[0].isupper() and phrase.endswith(", "):
        response = response[0].lower() + response[1:]

    data["response"] = phrase + response
    return json.dumps(data, ensure_ascii=False)


# ============================================================
# CONVERSATION GENERATORS
# ============================================================

def generate_sv_single_turn(dealer: dict, vehicles: list, intent: str) -> dict | None:
    """Generate a single-turn SingleVehicle conversation."""
    registry = SV_INTENT_REGISTRY.get(intent)
    if not registry:
        return None

    vehicle = random.choice(vehicles)

    user_template = random.choice(registry["templates"])
    user_msg = fill_vehicle_placeholders(user_template, vehicle)
    user_msg = add_typos_and_slang(user_msg, 0.35)

    assistant_json = registry["response_fn"](vehicle, dealer)
    assistant_json = apply_emoji_reduction(assistant_json)

    system_prompt = build_sv_system_prompt(vehicle, dealer)

    return {
        "messages": [
            {"role": "system", "content": system_prompt},
            {"role": "user", "content": user_msg},
            {"role": "assistant", "content": assistant_json},
        ],
        "_meta": {"mode": "single_vehicle", "intent": intent}
    }


def generate_di_single_turn(dealer: dict, vehicles: list, intent: str) -> dict | None:
    """Generate a single-turn DealerInventory conversation."""
    registry = DI_INTENT_REGISTRY.get(intent)
    if not registry:
        return None

    vehicle, vehicle_list = select_vehicles_for_intent(intent, vehicles)
    takes_list = registry.get("takes_list", False)
    needs_pair = registry.get("needs_pair", False)

    user_template = random.choice(registry["templates"])
    user_msg = fill_vehicle_placeholders(
        user_template, vehicle,
        vehicle_list if len(vehicle_list) >= 2 else None
    )
    user_msg = add_typos_and_slang(user_msg, 0.35)

    # Generate response
    if takes_list:
        if needs_pair and len(vehicle_list) >= 2:
            assistant_json = registry["response_fn"](vehicle_list[:2], dealer)
        else:
            assistant_json = registry["response_fn"](vehicles, dealer)
    else:
        assistant_json = registry["response_fn"](vehicle, dealer)

    assistant_json = apply_emoji_reduction(assistant_json)
    system_prompt = build_di_system_prompt(dealer, vehicles)

    return {
        "messages": [
            {"role": "system", "content": system_prompt},
            {"role": "user", "content": user_msg},
            {"role": "assistant", "content": assistant_json},
        ],
        "_meta": {"mode": "dealer_inventory", "intent": intent}
    }


def generate_sv_multi_turn(dealer: dict, vehicles: list, chain: list) -> dict:
    """Generate a multi-turn SingleVehicle conversation from chain."""
    intents = list(chain)

    # 50% chance to skip Greeting/Farewell to avoid over-representation
    if len(intents) > 2:
        if intents[0] == "Greeting" and random.random() < 0.50:
            intents = intents[1:]
        if intents[-1] == "Farewell" and random.random() < 0.50:
            intents = intents[:-1]

    vehicle = random.choice(vehicles)
    system_prompt = build_sv_system_prompt(vehicle, dealer)
    messages = [{"role": "system", "content": system_prompt}]
    previous_intent = None

    for turn_index, intent in enumerate(intents):
        registry = SV_INTENT_REGISTRY.get(intent)
        if not registry:
            continue

        user_template = random.choice(registry["templates"])
        user_msg = fill_vehicle_placeholders(user_template, vehicle)
        user_msg = add_typos_and_slang(user_msg, 0.30)

        assistant_json = registry["response_fn"](vehicle, dealer)
        assistant_json = apply_emoji_reduction(assistant_json)
        assistant_json = inject_context_continuity(
            assistant_json, turn_index, previous_intent, vehicle
        )

        messages.append({"role": "user", "content": user_msg})
        messages.append({"role": "assistant", "content": assistant_json})
        previous_intent = intent

    return {
        "messages": messages,
        "_meta": {"mode": "single_vehicle", "chain_length": len(intents)}
    }


def generate_di_multi_turn(dealer: dict, vehicles: list, chain: list) -> dict:
    """Generate a multi-turn DealerInventory conversation from chain."""
    intents = list(chain)

    if len(intents) > 2:
        if intents[0] == "Greeting" and random.random() < 0.50:
            intents = intents[1:]
        if intents[-1] == "Farewell" and random.random() < 0.50:
            intents = intents[:-1]

    system_prompt = build_di_system_prompt(dealer, vehicles)
    messages = [{"role": "system", "content": system_prompt}]

    current_vehicle = None
    vehicle_list = []
    context_vehicles = random.sample(vehicles, min(8, len(vehicles)))
    previous_intent = None

    for turn_index, intent in enumerate(intents):
        registry = DI_INTENT_REGISTRY.get(intent)
        if not registry:
            continue

        takes_list = registry.get("takes_list", False)
        needs_pair = registry.get("needs_pair", False)

        # Select vehicle(s) for this turn
        if intent in ("VehicleSearch", "VehicleComparison", "Greeting",
                       "VehicleNotInInventory", "CrossDealerRefusal"):
            current_vehicle, vehicle_list = select_vehicles_for_intent(intent, context_vehicles)
        elif current_vehicle is None:
            current_vehicle = random.choice(context_vehicles)
            vehicle_list = [current_vehicle]

        user_template = random.choice(registry["templates"])
        user_msg = fill_vehicle_placeholders(
            user_template, current_vehicle,
            vehicle_list if len(vehicle_list) >= 2 else None
        )
        user_msg = add_typos_and_slang(user_msg, 0.30)

        # Generate response
        if takes_list:
            if needs_pair and len(vehicle_list) >= 2:
                assistant_json = registry["response_fn"](vehicle_list[:2], dealer)
            else:
                assistant_json = registry["response_fn"](context_vehicles, dealer)
        else:
            assistant_json = registry["response_fn"](current_vehicle, dealer)

        assistant_json = apply_emoji_reduction(assistant_json)
        assistant_json = inject_context_continuity(
            assistant_json, turn_index, previous_intent, current_vehicle
        )

        messages.append({"role": "user", "content": user_msg})
        messages.append({"role": "assistant", "content": assistant_json})
        previous_intent = intent

    return {
        "messages": messages,
        "_meta": {"mode": "dealer_inventory", "chain_length": len(intents)}
    }


# ============================================================
# DATASET ORCHESTRATION
# ============================================================

def generate_dataset(count: int, vehicles: list, dealers: list, seed: int = None) -> list:
    """Generate the full dual-mode dataset."""
    if seed is not None:
        random.seed(seed)

    dataset = []

    # Calculate counts per mode
    sv_count = int(count * MODE_DISTRIBUTION["single_vehicle"])
    di_count = int(count * MODE_DISTRIBUTION["dealer_inventory"])
    edge_count = count - sv_count - di_count

    # Within each mode, split into single/multi-turn
    sv_single = int(sv_count * CONV_TYPE_DISTRIBUTION["single_turn"])
    sv_short = int(sv_count * CONV_TYPE_DISTRIBUTION["short_multi_turn"])
    sv_long = sv_count - sv_single - sv_short

    di_single = int(di_count * CONV_TYPE_DISTRIBUTION["single_turn"])
    di_short = int(di_count * CONV_TYPE_DISTRIBUTION["short_multi_turn"])
    di_long = di_count - di_single - di_short

    print(f"\n📊 Distribución por modo:")
    print(f"   SingleVehicle:   {sv_count} ({sv_single} single, {sv_short} short, {sv_long} long)")
    print(f"   DealerInventory: {di_count} ({di_single} single, {di_short} short, {di_long} long)")
    print(f"   Boundary/Edge:   {edge_count}")
    print()

    # ── 1. SingleVehicle single-turn ──
    print("🚗 [SingleVehicle] Generando single-turn...")
    sv_intent_pool = []
    for intent, pct in SV_INTENT_DISTRIBUTION.items():
        intent_count = max(1, int(sv_single * pct / sum(SV_INTENT_DISTRIBUTION.values())))
        sv_intent_pool.extend([intent] * intent_count)
    random.shuffle(sv_intent_pool)

    for intent in tqdm(sv_intent_pool[:sv_single], desc="SV single"):
        dealer = random.choice(dealers)
        conv = generate_sv_single_turn(dealer, vehicles, intent)
        if conv:
            dataset.append(conv)

    # ── 2. SingleVehicle multi-turn (short) ──
    print("\n🚗 [SingleVehicle] Generando multi-turn cortas...")
    short_chains = [c for c in SV_MULTI_TURN_CHAINS if 2 <= len(c) <= 4]
    if not short_chains:
        short_chains = SV_MULTI_TURN_CHAINS

    for _ in tqdm(range(sv_short), desc="SV short multi"):
        chain = random.choice(short_chains)
        max_turns = random.randint(2, min(4, len(chain)))
        trimmed = chain[:max_turns]
        dealer = random.choice(dealers)
        conv = generate_sv_multi_turn(dealer, vehicles, trimmed)
        if conv:
            dataset.append(conv)

    # ── 3. SingleVehicle multi-turn (long) ──
    print("\n🚗 [SingleVehicle] Generando multi-turn largas...")
    long_chains = [c for c in SV_MULTI_TURN_CHAINS if len(c) >= 4]
    if not long_chains:
        long_chains = SV_MULTI_TURN_CHAINS

    for _ in tqdm(range(sv_long), desc="SV long multi"):
        chain = random.choice(long_chains)
        dealer = random.choice(dealers)
        conv = generate_sv_multi_turn(dealer, vehicles, chain)
        if conv:
            dataset.append(conv)

    # ── 4. DealerInventory single-turn ──
    print("\n🏪 [DealerInventory] Generando single-turn...")
    di_intent_pool = []
    for intent, pct in DI_INTENT_DISTRIBUTION.items():
        intent_count = max(1, int(di_single * pct / sum(DI_INTENT_DISTRIBUTION.values())))
        di_intent_pool.extend([intent] * intent_count)
    random.shuffle(di_intent_pool)

    for intent in tqdm(di_intent_pool[:di_single], desc="DI single"):
        dealer = random.choice(dealers)
        # Assign 10-20 vehicles per dealer for training variety
        dealer_vehicles = random.sample(vehicles, min(random.randint(10, 20), len(vehicles)))
        conv = generate_di_single_turn(dealer, dealer_vehicles, intent)
        if conv:
            dataset.append(conv)

    # ── 5. DealerInventory multi-turn (short) ──
    print("\n🏪 [DealerInventory] Generando multi-turn cortas...")
    di_short_chains = [c for c in DI_MULTI_TURN_CHAINS if 2 <= len(c) <= 4]
    if not di_short_chains:
        di_short_chains = DI_MULTI_TURN_CHAINS

    for _ in tqdm(range(di_short), desc="DI short multi"):
        chain = random.choice(di_short_chains)
        max_turns = random.randint(2, min(4, len(chain)))
        trimmed = chain[:max_turns]
        dealer = random.choice(dealers)
        dealer_vehicles = random.sample(vehicles, min(random.randint(10, 20), len(vehicles)))
        conv = generate_di_multi_turn(dealer, dealer_vehicles, trimmed)
        if conv:
            dataset.append(conv)

    # ── 6. DealerInventory multi-turn (long) ──
    print("\n🏪 [DealerInventory] Generando multi-turn largas...")
    di_long_chains = [c for c in DI_MULTI_TURN_CHAINS if len(c) >= 4]
    if not di_long_chains:
        di_long_chains = DI_MULTI_TURN_CHAINS

    for _ in tqdm(range(di_long), desc="DI long multi"):
        chain = random.choice(di_long_chains)
        dealer = random.choice(dealers)
        dealer_vehicles = random.sample(vehicles, min(random.randint(10, 20), len(vehicles)))
        conv = generate_di_multi_turn(dealer, dealer_vehicles, chain)
        if conv:
            dataset.append(conv)

    # ── 7. Boundary/Edge cases ──
    print(f"\n⚠️  Generando {edge_count} conversaciones de boundary/edge...")
    boundary_intents_sv = ["VehicleNotInInventory", "LegalRefusal", "OutOfScope",
                           "FrustratedUser", "RequestHumanAgent", "Fallback"]
    boundary_intents_di = ["CrossDealerRefusal", "VehicleNotInInventory", "LegalRefusal",
                           "OutOfScope", "FrustratedUser", "RequestHumanAgent", "Fallback"]

    for _ in tqdm(range(edge_count), desc="Boundary"):
        dealer = random.choice(dealers)
        if random.random() < 0.5:
            # SV boundary
            intent = random.choice(boundary_intents_sv)
            conv = generate_sv_single_turn(dealer, vehicles, intent)
        else:
            # DI boundary
            intent = random.choice(boundary_intents_di)
            dealer_vehicles = random.sample(vehicles, min(15, len(vehicles)))
            conv = generate_di_single_turn(dealer, dealer_vehicles, intent)
        if conv:
            dataset.append(conv)

    # ── 8. Rebalancing: minimum per intent per mode ──
    MIN_EXAMPLES = 30

    sv_counts = Counter()
    di_counts = Counter()
    for conv in dataset:
        mode = conv.get("_meta", {}).get("mode", "")
        for msg in conv["messages"]:
            if msg["role"] == "assistant":
                try:
                    data = json.loads(msg["content"])
                    intent = data.get("intent", "")
                    if mode == "single_vehicle":
                        sv_counts[intent] += 1
                    elif mode == "dealer_inventory":
                        di_counts[intent] += 1
                except (json.JSONDecodeError, TypeError):
                    pass

    total_added = 0
    # Rebalance SV
    for intent in SV_INTENT_DISTRIBUTION:
        deficit = MIN_EXAMPLES - sv_counts.get(intent, 0)
        if deficit > 0:
            for _ in range(deficit):
                dealer = random.choice(dealers)
                conv = generate_sv_single_turn(dealer, vehicles, intent)
                if conv:
                    dataset.append(conv)
                    total_added += 1

    # Rebalance DI
    for intent in DI_INTENT_DISTRIBUTION:
        deficit = MIN_EXAMPLES - di_counts.get(intent, 0)
        if deficit > 0:
            for _ in range(deficit):
                dealer = random.choice(dealers)
                dealer_vehicles = random.sample(vehicles, min(15, len(vehicles)))
                conv = generate_di_single_turn(dealer, dealer_vehicles, intent)
                if conv:
                    dataset.append(conv)
                    total_added += 1

    if total_added > 0:
        print(f"   ⚖️  Rebalanceado: +{total_added} conversaciones adicionales")

    # ── 9. Strip _meta before saving ──
    for conv in dataset:
        conv.pop("_meta", None)

    random.shuffle(dataset)
    return dataset


# ============================================================
# SPLIT & SAVE
# ============================================================

def split_dataset(dataset: list, train_pct: float = 0.85, eval_pct: float = 0.10):
    """Split dataset into train/eval/test (85/10/5)."""
    n = len(dataset)
    train_end = int(n * train_pct)
    eval_end = train_end + int(n * eval_pct)
    return {
        "train": dataset[:train_end],
        "eval": dataset[train_end:eval_end],
        "test": dataset[eval_end:],
    }


def save_jsonl(data: list, path: Path):
    with jsonlines.open(path, mode="w") as writer:
        for item in data:
            writer.write(item)


def compute_stats(dataset: list) -> dict:
    stats = {
        "total_conversations": len(dataset),
        "generated_at": datetime.now().isoformat(),
        "generator_version": "2.0.0-dual-mode",
        "modes": {"single_vehicle": 0, "dealer_inventory": 0, "unknown": 0},
        "intent_distribution": {},
        "turns_distribution": {},
        "avg_turns": 0,
        "total_messages": 0,
    }

    total_turns = 0
    for conv in dataset:
        messages = conv["messages"]
        turns = sum(1 for m in messages if m["role"] == "user")
        total_turns += turns
        stats["total_messages"] += len(messages)

        turn_bucket = f"{turns}_turns"
        stats["turns_distribution"][turn_bucket] = stats["turns_distribution"].get(turn_bucket, 0) + 1

        # Detect mode from system prompt
        sys_content = messages[0]["content"] if messages else ""
        if "vehículo ESPECÍFICO" in sys_content or "VEHÍCULO EN CONTEXTO" in sys_content:
            stats["modes"]["single_vehicle"] += 1
        elif "inventario completo" in sys_content or "INVENTARIO DISPONIBLE" in sys_content:
            stats["modes"]["dealer_inventory"] += 1
        else:
            stats["modes"]["unknown"] += 1

        for msg in messages:
            if msg["role"] == "assistant":
                try:
                    resp = json.loads(msg["content"])
                    intent = resp.get("intent", "Unknown")
                    stats["intent_distribution"][intent] = stats["intent_distribution"].get(intent, 0) + 1
                except json.JSONDecodeError:
                    pass

    stats["avg_turns"] = round(total_turns / len(dataset), 2) if dataset else 0
    stats["intent_distribution"] = dict(
        sorted(stats["intent_distribution"].items(), key=lambda x: x[1], reverse=True)
    )
    return stats


# ============================================================
# MAIN
# ============================================================

def main():
    parser = argparse.ArgumentParser(
        description="OKLA Chatbot LLM — Generador de Dataset Sintético v2.0 (Dual-Mode)"
    )
    parser.add_argument("--count", type=int, default=3000,
                        help="Número de conversaciones a generar (default: 3000)")
    parser.add_argument("--output", type=str, default="output",
                        help="Directorio de salida (default: output/)")
    parser.add_argument("--seed", type=int, default=42,
                        help="Seed para reproducibilidad (default: 42)")
    parser.add_argument("--train-split", type=float, default=0.85,
                        help="Porcentaje para train split (default: 0.85)")
    parser.add_argument("--eval-split", type=float, default=0.10,
                        help="Porcentaje para eval split (default: 0.10)")
    args = parser.parse_args()

    print("=" * 65)
    print("🤖 OKLA Chatbot LLM — Dataset Generator v2.0 (Dual-Mode)")
    print("=" * 65)
    print(f"\n📋 Configuración:")
    print(f"   Conversaciones:  {args.count}")
    print(f"   Output:          {args.output}/")
    print(f"   Seed:            {args.seed}")
    print(f"   Split:           {args.train_split:.0%} train / {args.eval_split:.0%} eval / "
          f"{1 - args.train_split - args.eval_split:.0%} test")
    print(f"   Modos:           40% SingleVehicle / 50% DealerInventory / 10% Edge")

    # Load seed data
    print(f"\n📂 Cargando datos seed...")
    vehicles = load_json(VEHICLES_FILE)
    dealers = load_json(DEALERS_FILE)
    print(f"   ✅ {len(vehicles)} vehículos")
    print(f"   ✅ {len(dealers)} dealers")

    # Generate
    print(f"\n🚀 Generando {args.count} conversaciones dual-mode...\n")
    dataset = generate_dataset(args.count, vehicles, dealers, args.seed)
    print(f"\n✅ Generadas {len(dataset)} conversaciones")

    # Split
    splits = split_dataset(dataset, args.train_split, args.eval_split)
    print(f"\n📊 Splits:")
    for name, data in splits.items():
        print(f"   {name}: {len(data)} conversaciones")

    # Save
    output_dir = Path(args.output)
    output_dir.mkdir(parents=True, exist_ok=True)

    for name, data in splits.items():
        path = output_dir / f"okla_{name}.jsonl"
        save_jsonl(data, path)
        print(f"   💾 {path}")

    # SHA256 hashes
    print(f"\n🔐 Hashes de dataset:")
    dataset_hashes = {}
    for name in splits:
        path = output_dir / f"okla_{name}.jsonl"
        sha256 = hashlib.sha256()
        with open(path, "rb") as f:
            for chunk in iter(lambda: f.read(8192), b""):
                sha256.update(chunk)
        dataset_hashes[f"okla_{name}.jsonl"] = sha256.hexdigest()
        print(f"   📎 {name}: {sha256.hexdigest()[:16]}...")

    # Manifest
    manifest = {
        "generated_at": datetime.now().isoformat(),
        "generator": "generate_dataset.py",
        "generator_version": "2.0.0-dual-mode",
        "seed": args.seed,
        "total_conversations": len(dataset),
        "splits": {name: len(data) for name, data in splits.items()},
        "file_hashes_sha256": dataset_hashes,
        "modes": {"single_vehicle": "40%", "dealer_inventory": "50%", "boundary_edge": "10%"},
        "python_version": sys.version,
        "command": " ".join(sys.argv),
    }
    manifest_path = output_dir / "dataset_manifest.json"
    with open(manifest_path, "w", encoding="utf-8") as f:
        json.dump(manifest, f, indent=2, ensure_ascii=False)
    print(f"   📋 Manifest: {manifest_path}")

    # Stats
    stats = compute_stats(dataset)
    stats_path = output_dir / "stats.json"
    with open(stats_path, "w", encoding="utf-8") as f:
        json.dump(stats, f, indent=2, ensure_ascii=False)
    print(f"   📈 Stats: {stats_path}")

    # Summary
    print(f"\n{'=' * 65}")
    print(f"📊 RESUMEN DUAL-MODE")
    print(f"{'=' * 65}")
    print(f"Total conversaciones: {stats['total_conversations']}")
    print(f"Promedio de turnos:   {stats['avg_turns']}")
    print(f"Total mensajes:       {stats['total_messages']}")
    print(f"\n📦 Distribución por modo:")
    for mode, cnt in stats["modes"].items():
        pct = cnt / stats['total_conversations'] * 100 if stats['total_conversations'] else 0
        print(f"   {mode:20s} {cnt:5d} ({pct:.1f}%)")
    print(f"\n🏷️ Top 10 Intents:")
    for intent, cnt in list(stats["intent_distribution"].items())[:10]:
        pct = cnt / sum(stats["intent_distribution"].values()) * 100
        print(f"   {intent:25s} {cnt:5d} ({pct:.1f}%)")

    print(f"\n📁 Archivos generados en: {output_dir.absolute()}")
    print(f"{'=' * 65}")
    print("✅ ¡Dataset dual-mode generado exitosamente!")
    print("   Próximo paso: python validate_dataset.py output/okla_train.jsonl")


if __name__ == "__main__":
    main()
