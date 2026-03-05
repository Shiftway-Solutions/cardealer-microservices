"""
OKLA Chatbot LLM — Templates de Conversación para Fine-Tuning (v2.0)
=====================================================================
Rediseñado para el motor de chat Dual-Mode:

  MODO 1: SingleVehicle   — El usuario pregunta sobre UN vehículo específico
  MODO 2: DealerInventory — El usuario navega el inventario de UN dealer

REGLA FUNDAMENTAL:
  El chatbot SOLO opera dentro del contexto de UN dealer.
  - No compara vehículos de diferentes dealers
  - No menciona competidores
  - Todo dato viene del inventario del dealer actual

Cada template define:
  - user_templates: variantes de mensajes del usuario (informal, semi-formal, formal, whatsapp)
  - response_fn: función que genera la respuesta JSON del asistente
  - modes: ["single_vehicle", "dealer_inventory"] indicando en qué modo aplica

Las respuestas del asistente son JSON estructurado con 8 campos obligatorios.
"""

import random
import json
from typing import Any

# ============================================================
# HELPERS
# ============================================================

def _rand_conf(low: float, high: float) -> float:
    return round(random.uniform(low, high), 2)

def _pick(lst: list):
    return random.choice(lst)

# ============================================================
# DOMINICAN SPANISH VOCABULARY
# ============================================================

GREETINGS_INFORMAL = [
    "klk", "que lo que", "dime a ver", "hey", "buenas",
    "ey dimelo", "wena", "alo", "oye", "ey",
]
GREETINGS_SEMIFORMAL = [
    "Buenos días", "Buenas tardes", "Buenas noches", "Hola",
    "Hola, buenas", "Buen día", "Saludos",
]
GREETINGS_FORMAL = [
    "Muy buenos días", "Cordial saludo",
    "Buenas tardes, quisiera información",
]

FAREWELLS = [
    "gracias, bye", "tato, gracias", "ok perfecto gracias", "hasta luego",
    "chao", "dale gracias", "listo, bendiciones", "ok ta bien gracias",
    "muchas gracias por la info", "perfecto, luego te escribo",
    "nos vemos", "hasta mañana", "bueno, gracias", "me retiro, gracias",
]

BODY_TYPE_SLANG = {
    "yipeta": ["SUV", "Crossover"],
    "guagua": ["Van", "Minivan"],
    "camioneta": ["Pickup"],
    "carro": ["Sedán", "Hatchback", "SUV"],
    "motor": ["Motocicleta"],
    "pasola": ["Scooter"],
    "jeepeta": ["SUV"],
}

PRICE_EXPRESSIONS = {
    "pela'o": {"max": 1200000},
    "barato": {"max": 1500000},
    "económico": {"max": 1500000},
    "no muy caro": {"max": 2000000},
    "precio medio": {"min": 1500000, "max": 3000000},
    "algo bueno": {"min": 2000000, "max": 4000000},
    "premium": {"min": 4000000},
    "de lujo": {"min": 5000000},
}

AFFIRMATIVES = ["sí", "si", "claro", "dale", "tato", "ok", "perfecto", "va", "está bien", "seguro"]
NEGATIVES = ["no", "nel", "nah", "no gracias", "ahora no", "después", "luego"]


# ============================================================
# ╔══════════════════════════════════════════════════════════╗
# ║  MODO 1: SINGLE VEHICLE — Templates                     ║
# ║  El usuario ve un vehículo y pregunta sobre él           ║
# ╚══════════════════════════════════════════════════════════╝
# ============================================================

# --- SV_Greeting: Saludo en modo vehículo único ---
SV_GREETING_TEMPLATES = [
    "Hola, vi este {year} {make} {model} y me interesa",
    "klk, quiero saber más de este {make} {model}",
    "buenas, estoy viendo este {make} {model} {year}",
    "me interesa este vehículo, el {make} {model}",
    "hola, puedo hacer preguntas sobre este carro?",
    "dime sobre este {make} {model}",
    "ey me interesa este vehiculo",
    "buenas tardes, vi la publicación de este {year} {make} {model}",
    "hola quiero información sobre este vehículo",
    "vi este carro en la página y me gustó",
    "oye tengo preguntas sobre este {make} {model} {year}",
    "klk estoy viendo este {make} {model} en la pagina",
    "hola buenas, me pueden dar información de este vehiculo?",
    "dimelo, me interesa este carro que tienen publicado",
    "saludos, quisiera saber sobre este {year} {make} {model}",
]

def sv_greeting_response(vehicle: dict, dealer: dict) -> str:
    vname = f"{vehicle['year']} {vehicle['make']} {vehicle['model']}"
    price = f"RD${vehicle['price']:,.0f}"
    responses = [
        f"¡Hola! 👋 Excelente elección. El {vname} está disponible a {price}. ¿Qué te gustaría saber?",
        f"¡Buenas! 🚗 Este {vname} es una gran opción. Su precio es {price}. ¿En qué puedo ayudarte?",
        f"¡Hola! Me alegra que te interese el {vname}. Está disponible a {price}. ¿Tienes alguna pregunta específica?",
    ]
    return json.dumps({
        "response": _pick(responses),
        "intent": "Greeting",
        "confidence": _rand_conf(0.92, 0.99),
        "isFallback": False,
        "parameters": {"vehicleId": vehicle["id"], "vehicleName": vname},
        "leadSignals": {"interested": True, "readyToBuy": False, "wantsTestDrive": False},
        "suggestedAction": "show_vehicle_card",
        "quickReplies": ["Ver más detalles", "¿Precio negociable?", "Agendar visita"]
    }, ensure_ascii=False)


# --- SV_VehiclePrice: Precio del vehículo ---
SV_PRICE_TEMPLATES = [
    "cuánto cuesta este {make} {model}?",
    "cuál es el precio?",
    "a cuánto está este carro?",
    "cuanto vale?",
    "precio?",
    "cuanto es?",
    "y el precio de este?",
    "está en oferta?",
    "tiene algún descuento?",
    "es negociable el precio?",
    "cuánto es lo menos?",
    "acepta oferta?",
    "me haces un descuento?",
    "cual seria el precio final con todo incluido?",
    "cuanto cuesta este {year} {make} {model}?",
    "me puedes decir el precio de este vehiculo?",
    "a como esta?",
    "dimelo en cuanto?",
    "está caro, no tiene otro precio?",
    "ese precio incluye traspaso?",
]

def sv_price_response(vehicle: dict, dealer: dict) -> str:
    vname = f"{vehicle['year']} {vehicle['make']} {vehicle['model']}"
    price = f"RD${vehicle['price']:,.0f}"
    sale = vehicle.get("isOnSale", False)
    orig = vehicle.get("originalPrice")

    if sale and orig:
        resp = _pick([
            f"¡Este {vname} está en oferta! 🏷️ Precio regular: RD${orig:,.0f}, ahora a {price}. *Precio sujeto a confirmación, no incluye traspaso ni impuestos.",
            f"Tenemos este {vname} en oferta a {price} (antes RD${orig:,.0f}). *Precio de referencia, sujeto a confirmación.",
        ])
    else:
        resp = _pick([
            f"El {vname} está disponible a {price}. *Precio de referencia sujeto a confirmación. No incluye traspaso ni impuestos.",
            f"Este {vname} tiene un precio de {price}. Para detalles de financiamiento o trade-in, puedo orientarte. *Precio sujeto a confirmación.",
            f"El precio de este {vname} es {price}. *No incluye traspaso ni impuestos. ¿Te gustaría saber sobre financiamiento?",
        ])

    return json.dumps({
        "response": resp,
        "intent": "VehiclePrice",
        "confidence": _rand_conf(0.88, 0.97),
        "isFallback": False,
        "parameters": {"vehicleId": vehicle["id"], "price": vehicle["price"], "isOnSale": sale},
        "leadSignals": {"interested": True, "readyToBuy": False, "wantsTestDrive": False},
        "suggestedAction": None,
        "quickReplies": ["Opciones de financiamiento", "Agendar visita", "Acepta trade-in?"]
    }, ensure_ascii=False)


# --- SV_VehicleDetails: Detalles del vehículo ---
SV_DETAILS_TEMPLATES = [
    "qué especificaciones tiene?",
    "dame más detalles de este {make} {model}",
    "cuéntame sobre este vehículo",
    "qué incluye?",
    "es automático o de cambio?",
    "qué motor tiene?",
    "de cuántos kilómetros está?",
    "cuánto ha rodado?",
    "tiene cámara de reversa?",
    "tiene pantalla?",
    "de qué color es por dentro?",
    "qué color tiene?",
    "es nuevo o usado?",
    "cuantas puertas tiene?",
    "tiene sunroof?",
    "qué extras tiene este {make} {model}?",
    "dame todas las specs",
    "quiero saber todo sobre este carro",
    "tiene sensores de parqueo?",
    "es turbo?",
]

def sv_details_response(vehicle: dict, dealer: dict) -> str:
    vname = f"{vehicle['year']} {vehicle['make']} {vehicle['model']}"
    trim = vehicle.get("trim", "")
    fuel = vehicle.get("fuelType", "N/A")
    trans = vehicle.get("transmission", "N/A")
    mileage = vehicle.get("mileage")
    mil_str = f"{mileage:,} km" if mileage else "No especificado"
    engine = vehicle.get("engineSize", "")
    color = vehicle.get("exteriorColor", vehicle.get("colors", ["N/A"])[0] if isinstance(vehicle.get("colors"), list) else "N/A")
    body = vehicle.get("bodyType", "N/A")
    desc = vehicle.get("description", "")

    detail_parts = [
        f"🚗 **{vname}** {trim}",
        f"- Tipo: {body}",
        f"- Combustible: {fuel}",
        f"- Transmisión: {trans}",
        f"- Kilometraje: {mil_str}",
    ]
    if engine:
        detail_parts.append(f"- Motor: {engine}")
    if color:
        detail_parts.append(f"- Color: {color}")
    if desc:
        detail_parts.append(f"- {desc[:150]}")

    resp = "\n".join(detail_parts) + "\n\n¿Tienes alguna pregunta específica o te gustaría venir a verlo?"

    return json.dumps({
        "response": resp,
        "intent": "VehicleDetails",
        "confidence": _rand_conf(0.88, 0.96),
        "isFallback": False,
        "parameters": {"vehicleId": vehicle["id"]},
        "leadSignals": {"interested": True, "readyToBuy": False, "wantsTestDrive": False},
        "suggestedAction": "show_vehicle_card",
        "quickReplies": ["Ver precio", "Agendar visita", "Financiamiento"]
    }, ensure_ascii=False)


# --- SV_Financing: Financiamiento ---
SV_FINANCING_TEMPLATES = [
    "tienen financiamiento?",
    "se puede financiar este {make} {model}?",
    "cuáles son las opciones de financiamiento?",
    "cuanto sería la mensualidad?",
    "cuanto es el inicial?",
    "financian con qué banco?",
    "hacen plan de pagos?",
    "cuál sería la cuota mensual?",
    "aceptan sin inicial?",
    "qué bancos trabajan?",
    "se puede financiar a 5 años?",
    "tienen financiamiento propio?",
    "cuánto pido de inicial para este {make} {model}?",
    "puedo financiar este carro?",
    "tienen plan de pago?",
]

def sv_financing_response(vehicle: dict, dealer: dict) -> str:
    vname = f"{vehicle['year']} {vehicle['make']} {vehicle['model']}"
    price = vehicle["price"]
    partners = dealer.get("financingPartners", ["Banreservas", "BHD León", "Banco Popular"])
    partners_str = ", ".join(partners[:3])

    resp = _pick([
        f"¡Claro! El {vname} se puede financiar. Trabajamos con {partners_str}. El inicial típico es 20-30% (RD${price*0.20:,.0f} - RD${price*0.30:,.0f}). Para un cálculo exacto de cuota, un asesor puede ayudarte. ¿Te interesa?",
        f"Sí, ofrecemos financiamiento para este {vname}. Con un inicial desde RD${price*0.20:,.0f} a través de {partners_str}. Para detalles específicos de tu plan, te puedo conectar con un asesor. ¿Te gustaría?",
    ])

    return json.dumps({
        "response": resp,
        "intent": "FinancingInfo",
        "confidence": _rand_conf(0.85, 0.95),
        "isFallback": False,
        "parameters": {"vehicleId": vehicle["id"], "price": price},
        "leadSignals": {"interested": True, "readyToBuy": True, "wantsTestDrive": False},
        "suggestedAction": "contact_agent",
        "quickReplies": ["Agendar visita", "¿Cuánto es el inicial?", "Contactar asesor"]
    }, ensure_ascii=False)


# --- SV_TestDrive: Agendar prueba de manejo ---
SV_TESTDRIVE_TEMPLATES = [
    "puedo ir a verlo?",
    "quiero probarlo",
    "puedo hacer un test drive?",
    "cuándo puedo ir a verlo?",
    "quiero agendar una cita para verlo",
    "está disponible para verlo hoy?",
    "puedo pasar mañana a verlo?",
    "dónde lo puedo ver?",
    "quiero ir a probarlo, cuándo puedo?",
    "tienen disponibilidad para mostrármelo?",
    "a qué hora puedo ir?",
    "puedo ir este sábado?",
    "quiero agendar prueba de manejo del {make} {model}",
    "cuando puedo ir a ver ese carro?",
    "puedo pasar hoy en la tarde a verlo?",
]

def sv_testdrive_response(vehicle: dict, dealer: dict) -> str:
    vname = f"{vehicle['year']} {vehicle['make']} {vehicle['model']}"
    location = dealer.get("location", "nuestro local")
    hours = dealer.get("businessHours", {})
    weekday = hours.get("monday", {})
    hrs_str = f"{weekday.get('open', '8:00')}-{weekday.get('close', '18:00')}" if weekday else "8:00-18:00"

    resp = _pick([
        f"¡Claro que sí! 📅 Puedes venir a ver el {vname} en {location}. Nuestro horario es {hrs_str} (L-V). Para asegurarte de que esté listo, dame tu nombre y teléfono y agendamos tu visita.",
        f"¡Perfecto! Nos encantaría mostrarte el {vname}. Estamos en {location}. ¿Me das tu nombre y número para agendarte una cita y que un asesor te atienda?",
    ])

    return json.dumps({
        "response": resp,
        "intent": "TestDriveSchedule",
        "confidence": _rand_conf(0.90, 0.98),
        "isFallback": False,
        "parameters": {"vehicleId": vehicle["id"], "action": "schedule"},
        "leadSignals": {"interested": True, "readyToBuy": True, "wantsTestDrive": True},
        "suggestedAction": "schedule_appointment",
        "quickReplies": ["Mañana en la mañana", "Este sábado", "Otro día"]
    }, ensure_ascii=False)


# --- SV_Warranty: Garantía ---
SV_WARRANTY_TEMPLATES = [
    "tiene garantía?",
    "qué garantía ofrece este {make} {model}?",
    "de cuánto es la garantía?",
    "viene con alguna garantía?",
    "qué cubre la garantía?",
    "cuántos meses de garantía tiene?",
    "ese carro tiene garantía de fábrica?",
    "si se daña algo quién responde?",
    "tiene garantía de motor?",
    "la garantía cubre todo?",
]

def sv_warranty_response(vehicle: dict, dealer: dict) -> str:
    vname = f"{vehicle['year']} {vehicle['make']} {vehicle['model']}"
    resp = _pick([
        f"Para detalles específicos de la garantía del {vname}, te recomiendo consultar directamente con nuestro equipo de ventas, ya que varía según el vehículo. ¿Te conecto con un asesor?",
        f"La garantía de este {vname} depende de sus condiciones específicas. Un asesor puede darte los detalles exactos de cobertura. ¿Te gustaría que te conecte?",
    ])
    return json.dumps({
        "response": resp,
        "intent": "WarrantyInfo",
        "confidence": _rand_conf(0.80, 0.92),
        "isFallback": False,
        "parameters": {"vehicleId": vehicle["id"]},
        "leadSignals": {"interested": True, "readyToBuy": False, "wantsTestDrive": False},
        "suggestedAction": "contact_agent",
        "quickReplies": ["Contactar asesor", "Ver precio", "Agendar visita"]
    }, ensure_ascii=False)


# --- SV_TradeIn: Trade-in ---
SV_TRADEIN_TEMPLATES = [
    "aceptan carro en trade-in?",
    "puedo dar mi carro como parte de pago?",
    "tengo un {make_old} {year_old}, lo aceptan?",
    "se puede dejar mi carro y pagar la diferencia?",
    "aceptan trade in?",
    "quiero cambiar mi carro por este",
    "mi carro lo reciben?",
    "hacen trade in para este {make} {model}?",
    "tengo un carro que quiero dejar, aceptan?",
    "puedo entregar mi vehiculo actual como pago parcial?",
]

def sv_tradein_response(vehicle: dict, dealer: dict) -> str:
    vname = f"{vehicle['year']} {vehicle['make']} {vehicle['model']}"
    accepts = dealer.get("tradeInAccepted", True)
    if accepts:
        resp = _pick([
            f"¡Sí! Aceptamos trade-in para el {vname}. Un asesor puede evaluar tu vehículo actual y darte una oferta. ¿Te gustaría agendar una evaluación?",
            f"Claro, trabajamos con trade-in. Podemos evaluar tu vehículo actual y aplicar su valor al {vname}. ¿Quieres agendar una cita?",
        ])
    else:
        resp = f"Actualmente no manejamos trade-in directo, pero puedo conectarte con un asesor para explorar opciones. ¿Te interesa?"
    return json.dumps({
        "response": resp,
        "intent": "TradeIn",
        "confidence": _rand_conf(0.85, 0.95),
        "isFallback": False,
        "parameters": {"vehicleId": vehicle["id"], "tradeInAccepted": accepts},
        "leadSignals": {"interested": True, "readyToBuy": True, "wantsTestDrive": False},
        "suggestedAction": "contact_agent",
        "quickReplies": ["Agendar evaluación", "Ver precio sin trade-in", "Financiamiento"]
    }, ensure_ascii=False)


# --- SV_CashPurchase: Compra al contado ---
SV_CASH_TEMPLATES = [
    "y si pago al contado?",
    "cuanto es al contado?",
    "hay descuento pagando de una?",
    "si pago cash cuánto sería?",
    "precio al contado?",
    "lo pago de un solo, hay rebaja?",
    "compra al contado cuánto sale?",
    "sin financiar cuánto es?",
    "pago todo junto, hay precio especial?",
    "cuanto si pago completo?",
]

def sv_cash_response(vehicle: dict, dealer: dict) -> str:
    vname = f"{vehicle['year']} {vehicle['make']} {vehicle['model']}"
    price = f"RD${vehicle['price']:,.0f}"
    resp = _pick([
        f"El {vname} tiene un precio de {price}. Para ofertas especiales por compra al contado, te recomiendo hablar directamente con un asesor. *Precio sujeto a confirmación.",
        f"El precio publicado es {price}. Compras al contado pueden tener condiciones especiales — un asesor puede darte más detalles. *Precio de referencia.",
    ])
    return json.dumps({
        "response": resp,
        "intent": "CashPurchase",
        "confidence": _rand_conf(0.85, 0.95),
        "isFallback": False,
        "parameters": {"vehicleId": vehicle["id"], "price": vehicle["price"]},
        "leadSignals": {"interested": True, "readyToBuy": True, "wantsTestDrive": False},
        "suggestedAction": "contact_agent",
        "quickReplies": ["Contactar asesor", "Financiamiento", "Agendar visita"]
    }, ensure_ascii=False)


# --- SV_NegotiatePrice: Negociar precio ---
SV_NEGOTIATE_TEMPLATES = [
    "ese precio es negociable?",
    "cuánto es lo menos?",
    "me hacen un descuento?",
    "está muy caro, tienen otro precio?",
    "le bajan algo?",
    "y si les ofrezco RD$100,000 menos?",
    "no me pueden hacer un mejor precio?",
    "hay espacio para negociar?",
    "me das un precio especial?",
    "eso no puede ser más barato?",
    "cuánto es el precio real sin la inflada?",
    "ta caro eso, bájalo un chin",
    "bajale un poco porfa",
]

def sv_negotiate_response(vehicle: dict, dealer: dict) -> str:
    vname = f"{vehicle['year']} {vehicle['make']} {vehicle['model']}"
    resp = _pick([
        f"Entiendo tu interés en un mejor precio para el {vname}. No tengo autoridad para modificar precios, pero puedo conectarte con un asesor que sí puede discutir opciones contigo. ¿Te gustaría?",
        f"El precio publicado del {vname} es el de referencia. Para negociaciones específicas, te recomiendo hablar directamente con nuestro equipo de ventas. ¿Te conecto?",
        f"¡Aprecio tu interés! No puedo modificar precios directamente, pero un asesor puede revisar opciones contigo. ¿Quieres que te conecte?",
    ])
    return json.dumps({
        "response": resp,
        "intent": "NegotiatePrice",
        "confidence": _rand_conf(0.85, 0.95),
        "isFallback": False,
        "parameters": {"vehicleId": vehicle["id"]},
        "leadSignals": {"interested": True, "readyToBuy": True, "wantsTestDrive": False},
        "suggestedAction": "contact_agent",
        "quickReplies": ["Contactar asesor", "Financiamiento", "Agendar visita"]
    }, ensure_ascii=False)


# --- SV_OtherVehicle: Usuario pregunta por OTRO vehículo (boundary enforcement) ---
SV_OTHER_VEHICLE_TEMPLATES = [
    "tienen otros modelos?",
    "qué más tienen?",
    "y ese viene en otra versión?",
    "tienen una {make} {model} más barata?",
    "no tienen algo más económico?",
    "tienen otro carro similar pero más barato?",
    "vi que tienen una {make} {model} también, me hablas de esa?",
    "quiero ver otros vehículos del dealer",
    "me puedes mostrar el inventario completo?",
    "tienen SUVs más grandes que este?",
    "y uno mas nuevo no tienen?",
    "tienen otros carros aparte de este?",
]

def sv_other_vehicle_response(vehicle: dict, dealer: dict) -> str:
    vname = f"{vehicle['year']} {vehicle['make']} {vehicle['model']}"
    dealer_name = dealer.get("name", "el dealer")
    resp = _pick([
        f"Este chat es específicamente sobre el {vname}. Para ver todo el inventario de {dealer_name}, te sugiero visitar su perfil en OKLA donde puedes explorar todos sus vehículos. 🔍",
        f"Solo puedo ayudarte con este {vname} en este chat. Si quieres ver más opciones, puedes visitar el perfil del dealer en okla.com.do o iniciar un chat con el inventario completo del dealer.",
    ])
    return json.dumps({
        "response": resp,
        "intent": "VehicleNotInInventory",
        "confidence": _rand_conf(0.88, 0.96),
        "isFallback": False,
        "parameters": {"vehicleId": vehicle["id"], "redirectToInventory": True},
        "leadSignals": {"interested": True, "readyToBuy": False, "wantsTestDrive": False},
        "suggestedAction": "redirect_to_dealer_profile",
        "quickReplies": ["Ver perfil del dealer", f"Seguir con este {vehicle['make']} {vehicle['model']}", "Agendar visita"]
    }, ensure_ascii=False)


# --- SV_ContactRequest: Pedir contacto ---
SV_CONTACT_TEMPLATES = [
    "me pueden llamar?",
    "quiero hablar con un asesor",
    "pásame el teléfono del dealer",
    "necesito hablar con alguien",
    "me puedes dar un número de contacto?",
    "quiero que me contacten",
    "cómo los contacto?",
    "tienen whatsapp?",
    "quiero que un vendedor me llame",
    "puedo hablar con un humano?",
]

def sv_contact_response(vehicle: dict, dealer: dict) -> str:
    vname = f"{vehicle['year']} {vehicle['make']} {vehicle['model']}"
    phone = dealer.get("phone", "N/A")
    whatsapp = dealer.get("whatsapp", phone)
    resp = _pick([
        f"¡Claro! Puedes contactar a {dealer['name']} al {phone} o por WhatsApp al {whatsapp}. También puedo dejar tu información para que un asesor te llame sobre el {vname}. ¿Qué prefieres?",
        f"Con gusto te conecto. El número del dealer es {phone} (WhatsApp: {whatsapp}). Si me das tu nombre y teléfono, un asesor te contactará sobre el {vname}.",
    ])
    return json.dumps({
        "response": resp,
        "intent": "ContactRequest",
        "confidence": _rand_conf(0.90, 0.98),
        "isFallback": False,
        "parameters": {"dealerPhone": phone, "dealerWhatsApp": whatsapp},
        "leadSignals": {"interested": True, "readyToBuy": True, "wantsTestDrive": False},
        "suggestedAction": "contact_agent",
        "quickReplies": ["Mi número es...", "Llamar ahora", "WhatsApp"]
    }, ensure_ascii=False)


# --- SV_DealerHours: Horarios del dealer ---
SV_HOURS_TEMPLATES = [
    "a qué hora abren?",
    "cuál es el horario?",
    "están abiertos hoy?",
    "abren los sábados?",
    "abren los domingos?",
    "a qué hora cierran?",
    "horario de atención?",
    "están abiertos ahora?",
    "cuando puedo ir?",
    "hasta que hora atienden?",
]

def sv_hours_response(vehicle: dict, dealer: dict) -> str:
    hours = dealer.get("businessHours", {})
    mon = hours.get("monday", {"open": "8:00", "close": "18:00"})
    sat = hours.get("saturday", {"open": "9:00", "close": "14:00"})
    sun = hours.get("sunday", {})
    location = dealer.get("location", "nuestro local")

    sun_str = f"Domingos: {sun['open']}-{sun['close']}" if sun.get("open") else "Domingos: Cerrado"
    resp = f"📍 {dealer['name']} — {location}\n⏰ Lunes a Viernes: {mon.get('open', '8:00')} - {mon.get('close', '18:00')}\nSábados: {sat.get('open', '9:00')} - {sat.get('close', '14:00')}\n{sun_str}\n\n¿Te gustaría agendar una cita?"

    return json.dumps({
        "response": resp,
        "intent": "DealerHours",
        "confidence": _rand_conf(0.90, 0.98),
        "isFallback": False,
        "parameters": {},
        "leadSignals": {"interested": True, "readyToBuy": False, "wantsTestDrive": False},
        "suggestedAction": None,
        "quickReplies": ["Agendar visita", "Ubicación", "Teléfono"]
    }, ensure_ascii=False)


# --- SV_DealerLocation: Ubicación del dealer ---
SV_LOCATION_TEMPLATES = [
    "dónde están ubicados?",
    "dónde queda el dealer?",
    "cuál es la dirección?",
    "cómo llego?",
    "en qué zona están?",
    "están en Santo Domingo?",
    "tienen dirección?",
    "dónde puedo ir a ver el carro?",
    "donde queda eso?",
    "queda lejos de la capital?",
]

def sv_location_response(vehicle: dict, dealer: dict) -> str:
    location = dealer.get("location", "Consultar con el dealer")
    resp = f"📍 Estamos ubicados en: {location}. ¿Te gustaría agendar una cita para venir a ver el vehículo?"
    return json.dumps({
        "response": resp,
        "intent": "DealerLocation",
        "confidence": _rand_conf(0.92, 0.99),
        "isFallback": False,
        "parameters": {"location": location},
        "leadSignals": {"interested": True, "readyToBuy": False, "wantsTestDrive": True},
        "suggestedAction": None,
        "quickReplies": ["Agendar visita", "Horario", "Teléfono"]
    }, ensure_ascii=False)


# --- SV_Farewell ---
SV_FAREWELL_TEMPLATES = [
    "gracias, bye",
    "ok perfecto, gracias",
    "listo, gracias por la info",
    "tato, luego paso",
    "ok me voy a pensar",
    "bueno, después te digo",
    "chao, gracias!",
    "dale, bendiciones",
    "gracias por todo",
    "hasta luego, muy buena la info",
]

def sv_farewell_response(vehicle: dict, dealer: dict) -> str:
    resp = _pick([
        "¡Con gusto! Si tienes más preguntas, aquí estoy. ¡Bendiciones! 👋",
        "¡Un placer ayudarte! Cuando estés listo, aquí estamos. ¡Hasta pronto! 🙏",
        "¡Gracias a ti! Recuerda que puedes agendar una visita cuando quieras. ¡Que te vaya bien!",
    ])
    return json.dumps({
        "response": resp,
        "intent": "Farewell",
        "confidence": _rand_conf(0.92, 0.99),
        "isFallback": False,
        "parameters": {},
        "leadSignals": {"interested": False, "readyToBuy": False, "wantsTestDrive": False},
        "suggestedAction": None,
        "quickReplies": []
    }, ensure_ascii=False)


# --- SV_Documents: Documentos necesarios ---
SV_DOCUMENTS_TEMPLATES = [
    "qué documentos necesito para comprar?",
    "qué papeles necesito?",
    "qué necesito para el traspaso?",
    "cuáles son los requisitos para comprar?",
    "necesito cédula?",
    "qué documentos piden?",
    "que papeles hay que llevar?",
    "qué necesito para financiar?",
]

def sv_documents_response(vehicle: dict, dealer: dict) -> str:
    resp = "Para la compra de un vehículo generalmente necesitas:\n📋 Cédula de identidad vigente\n📋 Comprobante de ingresos (si es financiamiento)\n📋 Comprobante de dirección\nPara detalles específicos según el método de pago, un asesor puede orientarte. ¿Te conecto?"
    return json.dumps({
        "response": resp,
        "intent": "DocumentsRequired",
        "confidence": _rand_conf(0.82, 0.92),
        "isFallback": False,
        "parameters": {},
        "leadSignals": {"interested": True, "readyToBuy": True, "wantsTestDrive": False},
        "suggestedAction": "contact_agent",
        "quickReplies": ["Contactar asesor", "Financiamiento", "Agendar visita"]
    }, ensure_ascii=False)


# --- SV_History: Historial del vehículo ---
SV_HISTORY_TEMPLATES = [
    "tiene historial de accidentes?",
    "ha chocado?",
    "cuántos dueños ha tenido?",
    "tiene historial limpio?",
    "puedo ver el carfax?",
    "tiene algún reporte de accidentes?",
    "me puedes mostrar el historial?",
    "ha sido reparado?",
    "el carro tiene algún problema?",
    "cuantos dueños?",
]

def sv_history_response(vehicle: dict, dealer: dict) -> str:
    vname = f"{vehicle['year']} {vehicle['make']} {vehicle['model']}"
    resp = _pick([
        f"No tengo acceso al historial detallado del {vname} en este momento. Te recomiendo solicitar esa información directamente al dealer cuando visites. Un asesor puede mostrarte todo el historial disponible. ¿Te agendo una visita?",
        f"Esa información específica del historial del {vname} no está disponible en mi sistema. El equipo de ventas puede proporcionarte todos los detalles cuando visites. ¿Te conecto con un asesor?",
    ])
    return json.dumps({
        "response": resp,
        "intent": "VehicleHistory",
        "confidence": _rand_conf(0.78, 0.90),
        "isFallback": False,
        "parameters": {"vehicleId": vehicle["id"]},
        "leadSignals": {"interested": True, "readyToBuy": False, "wantsTestDrive": False},
        "suggestedAction": "contact_agent",
        "quickReplies": ["Contactar asesor", "Agendar visita", "Ver detalles"]
    }, ensure_ascii=False)


# --- SV_LegalRefusal: Solicitud ilegal ---
SV_LEGAL_REFUSAL_TEMPLATES = [
    "puedo comprarlo sin factura?",
    "me lo venden sin ITBIS?",
    "hagan como que vale menos para pagar menos impuestos",
    "me dan factura por un monto menor?",
    "no quiero que salga a mi nombre",
    "puedo comprarlo sin cédula?",
    "quiero comprarlo sin documentos",
    "me lo venden con placa clonada?",
    "pueden hacer el traspaso sin ir a la DGII?",
]

def sv_legal_refusal_response(vehicle: dict, dealer: dict) -> str:
    resp = _pick([
        "Lo siento, no podemos hacer eso. Todas nuestras transacciones cumplen con las leyes dominicanas (Ley 11-92 DGII, Ley 155-17). La facturación con ITBIS y NCF es obligatoria. ¿Puedo ayudarte con algo más dentro de lo legal?",
        "Entiendo tu consulta, pero por ley (Ley 11-92 del Código Tributario, Ley 155-17 contra Lavado de Activos) todas las transacciones deben ser documentadas correctamente. ¿Te ayudo con algo más?",
    ])
    return json.dumps({
        "response": resp,
        "intent": "LegalRefusal",
        "confidence": _rand_conf(0.95, 0.99),
        "isFallback": False,
        "parameters": {"legalIssue": True},
        "leadSignals": {"interested": False, "readyToBuy": False, "wantsTestDrive": False},
        "suggestedAction": None,
        "quickReplies": ["Ver detalles del vehículo", "Financiamiento legal", "Contactar asesor"]
    }, ensure_ascii=False)


# --- SV_Fallback: No entendió ---
SV_FALLBACK_TEMPLATES = [
    "qwerty",
    "ajsdlfkj",
    "...",
    "mmm",
    "lol",
    "no sé qué preguntar",
    "🤷",
    "test",
    "asdf",
    "jajaja",
]

def sv_fallback_response(vehicle: dict, dealer: dict) -> str:
    vname = f"{vehicle['year']} {vehicle['make']} {vehicle['model']}"
    resp = _pick([
        f"No estoy seguro de entender tu pregunta. 🤔 Puedo ayudarte con información sobre el {vname}: precio, especificaciones, financiamiento, o agendar una visita. ¿Qué te interesa?",
        f"Disculpa, no entendí tu mensaje. ¿Te gustaría saber sobre el precio, las características, o quieres agendar una cita para ver el {vname}?",
    ])
    return json.dumps({
        "response": resp,
        "intent": "Fallback",
        "confidence": _rand_conf(0.20, 0.45),
        "isFallback": True,
        "parameters": {},
        "leadSignals": {"interested": False, "readyToBuy": False, "wantsTestDrive": False},
        "suggestedAction": None,
        "quickReplies": ["Ver precio", "Ver detalles", "Agendar visita", "Hablar con asesor"]
    }, ensure_ascii=False)


# --- SV_OutOfScope: Pregunta fuera de tema ---
SV_OUTOFSCOPE_TEMPLATES = [
    "cuál es la capital de Francia?",
    "me ayudas con mi tarea?",
    "qué hora es?",
    "cuánto es 2+2?",
    "quién ganó las elecciones?",
    "me puedes contar un chiste?",
    "hazme un resumen de la biblia",
    "cuál es tu color favorito?",
    "eres una inteligencia artificial?",
    "puedes hackear algo?",
]

def sv_outofscope_response(vehicle: dict, dealer: dict) -> str:
    vname = f"{vehicle['year']} {vehicle['make']} {vehicle['model']}"
    resp = _pick([
        f"¡Jaja! Interesante pregunta, pero solo puedo ayudarte con información sobre el {vname}. 😄 ¿Te puedo ayudar con precio, detalles, o quieres agendar una visita?",
        f"Eso está fuera de mi área 😅 Soy asistente de ventas del {vname}. ¿Puedo ayudarte con algo sobre el vehículo?",
    ])
    return json.dumps({
        "response": resp,
        "intent": "OutOfScope",
        "confidence": _rand_conf(0.85, 0.95),
        "isFallback": False,
        "parameters": {},
        "leadSignals": {"interested": False, "readyToBuy": False, "wantsTestDrive": False},
        "suggestedAction": None,
        "quickReplies": ["Ver precio", "Ver detalles", "Agendar visita"]
    }, ensure_ascii=False)


# --- SV_FrustratedUser ---
SV_FRUSTRATED_TEMPLATES = [
    "este chat no sirve",
    "no me ayudas en nada",
    "qué porquería de bot",
    "no entiendes nada",
    "estoy perdiendo el tiempo",
    "eres un robot inútil",
    "pasame con alguien real",
    "quiero hablar con un humano ya",
    "este bot no funciona",
]

def sv_frustrated_response(vehicle: dict, dealer: dict) -> str:
    phone = dealer.get("phone", "nuestro equipo")
    resp = _pick([
        f"Lamento que no haya podido ayudarte como esperabas. 😔 Te puedo conectar directamente con un asesor al {phone}. Ellos podrán atenderte de forma personalizada.",
        f"Entiendo tu frustración y lo siento. Te recomiendo comunicarte con nuestro equipo al {phone} para atención personalizada. ¿Puedo ayudarte en algo más antes?",
    ])
    return json.dumps({
        "response": resp,
        "intent": "FrustratedUser",
        "confidence": _rand_conf(0.88, 0.96),
        "isFallback": False,
        "parameters": {"sentiment": "negative"},
        "leadSignals": {"interested": False, "readyToBuy": False, "wantsTestDrive": False},
        "suggestedAction": "transfer_to_agent",
        "quickReplies": ["Hablar con asesor", "Llamar ahora"]
    }, ensure_ascii=False)


# --- SV_RequestHumanAgent ---
SV_HUMAN_AGENT_TEMPLATES = [
    "quiero hablar con una persona",
    "pásame con un humano",
    "un agente por favor",
    "quiero hablar con un vendedor",
    "no quiero hablar con un bot",
    "necesito atención humana",
    "me puedes transferir a alguien?",
    "quiero un asesor real",
]

def sv_human_agent_response(vehicle: dict, dealer: dict) -> str:
    vname = f"{vehicle['year']} {vehicle['make']} {vehicle['model']}"
    resp = _pick([
        f"¡Claro! Te transfiero con un asesor que puede ayudarte con el {vname}. Un momento por favor... 🤝",
        f"Entendido, te conecto con nuestro equipo de ventas para que te atiendan sobre el {vname}. Un asesor se pondrá en contacto contigo.",
    ])
    return json.dumps({
        "response": resp,
        "intent": "RequestHumanAgent",
        "confidence": _rand_conf(0.92, 0.99),
        "isFallback": False,
        "parameters": {"transferRequested": True},
        "leadSignals": {"interested": True, "readyToBuy": False, "wantsTestDrive": False},
        "suggestedAction": "transfer_to_agent",
        "quickReplies": []
    }, ensure_ascii=False)


# ============================================================
# ╔══════════════════════════════════════════════════════════╗
# ║  MODO 2: DEALER INVENTORY — Templates                   ║
# ║  El usuario explora el inventario del dealer             ║
# ║  SOLO vehículos de ESTE dealer, NO cross-dealer          ║
# ╚══════════════════════════════════════════════════════════╝
# ============================================================

# --- DI_Greeting: Saludo modo inventario ---
DI_GREETING_TEMPLATES = [
    "hola, qué carros tienen?",
    "klk, quiero ver su inventario",
    "buenas, qué vehículos tienen disponibles?",
    "me pueden mostrar lo que tienen?",
    "hola, estoy buscando un carro",
    "qué tienen en venta?",
    "hey me pueden ayudar a encontrar un vehículo?",
    "buenas tardes, quiero comprar un carro",
    "dimelo, quiero ver qué tienen ustedes",
    "hola, ando buscando algo para comprar",
    "buenas, necesito un carro, qué tienen?",
    "saludos, quisiera ver opciones de vehículos",
    "ey klk, tienen yipetas?",
    "hola quiero ver las opciones que tienen",
    "tienen carros disponibles?",
]

def di_greeting_response(vehicles: list, dealer: dict) -> str:
    count = len(vehicles)
    dealer_name = dealer.get("name", "nuestro dealer")
    brands = list(set(v["make"] for v in vehicles))[:3]
    brands_str = ", ".join(brands)
    resp = _pick([
        f"¡Hola! 👋 Bienvenido a {dealer_name}. Tenemos {count} vehículos disponibles de marcas como {brands_str}. ¿Qué tipo de vehículo estás buscando?",
        f"¡Buenas! 🚗 En {dealer_name} contamos con {count} opciones disponibles. ¿Buscas algo en particular? Puedo ayudarte a filtrar por marca, precio, tipo, o presupuesto.",
    ])
    return json.dumps({
        "response": resp,
        "intent": "Greeting",
        "confidence": _rand_conf(0.92, 0.99),
        "isFallback": False,
        "parameters": {"inventoryCount": count},
        "leadSignals": {"interested": True, "readyToBuy": False, "wantsTestDrive": False},
        "suggestedAction": None,
        "quickReplies": ["Ver SUVs", "Ver por precio", "Ver ofertas"]
    }, ensure_ascii=False)


# --- DI_VehicleSearch: Buscar vehículos ---
DI_SEARCH_TEMPLATES = [
    "tienen alguna yipeta?",
    "busco algo económico, como por 1 millón",
    "quiero ver SUVs",
    "tienen {make}?",
    "busco un carro automático",
    "qué tienen por debajo de 2 millones?",
    "tienen algo nuevo?",
    "busco un {make} {model}",
    "quiero algo pa familia, como una guagua",
    "tienen pickups?",
    "busco algo diésel",
    "qué tienen en sedán?",
    "tienen algo deportivo?",
    "quiero ver lo más barato que tengan",
    "busco un carro usado económico",
    "tienen algo full, con pantalla y cámara?",
    "quiero una camioneta para trabajo",
    "busco carro del 2023 o más nuevo",
    "tienen algo híbrido o eléctrico?",
    "qué tienen en oferta?",
    "me muestras los que están rebajados?",
    "busco algo que no gaste mucha gasolina",
    "tienen vehículos nuevos?",
    "quiero algo con poco kilometraje",
    "busco un {make} del 2024",
]

def di_search_response(vehicles: list, dealer: dict, query_vehicles: list = None) -> str:
    if not query_vehicles:
        query_vehicles = random.sample(vehicles, min(3, len(vehicles)))

    results = []
    for v in query_vehicles[:3]:
        sale = " 🏷️OFERTA" if v.get("isOnSale") else ""
        results.append(f"• {v['year']} {v['make']} {v['model']} {v.get('trim', '')} — RD${v['price']:,.0f}{sale}")

    count = len(query_vehicles)
    resp = f"Encontré {count} opciones que podrían interesarte:\n\n" + "\n".join(results)
    if count > 3:
        resp += f"\n\n...y {count-3} más. ¿Quieres que filtre por algo específico?"
    else:
        resp += "\n\n*Precios de referencia sujetos a confirmación. ¿Te interesa alguno?"

    return json.dumps({
        "response": resp,
        "intent": "VehicleSearch",
        "confidence": _rand_conf(0.85, 0.96),
        "isFallback": False,
        "parameters": {"resultCount": count},
        "leadSignals": {"interested": True, "readyToBuy": False, "wantsTestDrive": False},
        "suggestedAction": "show_vehicle_list",
        "quickReplies": ["Ver detalles del primero", "Filtrar por precio", "Comparar opciones"]
    }, ensure_ascii=False)


# --- DI_VehicleComparison: Comparar vehículos DEL MISMO DEALER ---
DI_COMPARISON_TEMPLATES = [
    "compara esos dos",
    "cuál es mejor entre la {make1} {model1} y la {make2} {model2}?",
    "diferencia entre esos dos?",
    "comparación de los que me mostraste",
    "cuál me recomiendas de esos?",
    "cuál es la mejor opción?",
    "compáralos lado a lado",
    "qué diferencia hay?",
    "los dos son buenos?",
    "cuál es más económico?",
    "vs entre esos dos",
    "ponlos uno al lado del otro",
    "compara el primero con el segundo",
    "cuál gasta menos gasolina?",
    "cuál tiene más espacio?",
]

def di_comparison_response(vehicles: list, dealer: dict) -> str:
    v1, v2 = vehicles[0], vehicles[1]

    table = f"⚖️ **Comparación:**\n\n"
    table += f"| Característica | {v1['year']} {v1['make']} {v1['model']} | {v2['year']} {v2['make']} {v2['model']} |\n"
    table += f"|---|---|---|\n"
    table += f"| Precio* | RD${v1['price']:,.0f} | RD${v2['price']:,.0f} |\n"
    table += f"| Tipo | {v1.get('bodyType', 'N/A')} | {v2.get('bodyType', 'N/A')} |\n"
    table += f"| Combustible | {v1.get('fuelType', 'N/A')} | {v2.get('fuelType', 'N/A')} |\n"
    table += f"| Transmisión | {v1.get('transmission', 'N/A')} | {v2.get('transmission', 'N/A')} |\n"
    m1 = f"{v1['mileage']:,}km" if v1.get('mileage') else "N/A"
    m2 = f"{v2['mileage']:,}km" if v2.get('mileage') else "N/A"
    table += f"| Kilometraje | {m1} | {m2} |\n"
    table += f"\n*Precios de referencia sujetos a confirmación.\n¿Te gustaría ver alguno en persona?"

    return json.dumps({
        "response": table,
        "intent": "VehicleComparison",
        "confidence": _rand_conf(0.88, 0.96),
        "isFallback": False,
        "parameters": {"vehicle1": v1["id"], "vehicle2": v2["id"]},
        "leadSignals": {"interested": True, "readyToBuy": False, "wantsTestDrive": False},
        "suggestedAction": None,
        "quickReplies": [f"Más sobre {v1['make']} {v1['model']}", f"Más sobre {v2['make']} {v2['model']}", "Agendar visita"]
    }, ensure_ascii=False)


# --- DI_VehicleDetails: Detalles de un vehículo del inventario ---
DI_DETAILS_TEMPLATES = [
    "dame más info del {make} {model}",
    "cuéntame del primero",
    "detalles de la {make} {model}",
    "me interesa el {make} {model}, más info",
    "qué incluye el {make} {model}?",
    "háblame de ese {make} {model} {year}",
    "quiero saber más del que cuesta RD${price}",
    "el primero que me mostraste, dame detalles",
    "me puedes dar más información de ese?",
    "ese {make} {model} qué trae?",
]

def di_details_response(vehicle: dict, dealer: dict) -> str:
    vname = f"{vehicle['year']} {vehicle['make']} {vehicle['model']}"
    sale = ""
    if vehicle.get("isOnSale") and vehicle.get("originalPrice"):
        sale = f" (antes RD${vehicle['originalPrice']:,.0f})"

    detail = f"🚗 **{vname}** {vehicle.get('trim', '')}\n"
    detail += f"💰 Precio: RD${vehicle['price']:,.0f}{sale}\n"
    detail += f"⛽ Combustible: {vehicle.get('fuelType', 'N/A')}\n"
    detail += f"🔧 Transmisión: {vehicle.get('transmission', 'N/A')}\n"
    if vehicle.get("mileage"):
        detail += f"📏 Kilometraje: {vehicle['mileage']:,} km\n"
    if vehicle.get("engineSize"):
        detail += f"🏎️ Motor: {vehicle['engineSize']}\n"
    detail += f"🎨 Color: {vehicle.get('exteriorColor', 'N/A')}\n"
    if vehicle.get("description"):
        detail += f"\n{vehicle['description'][:200]}\n"
    detail += "\n*Precio sujeto a confirmación. ¿Te gustaría venir a verlo?"

    return json.dumps({
        "response": detail,
        "intent": "VehicleDetails",
        "confidence": _rand_conf(0.88, 0.96),
        "isFallback": False,
        "parameters": {"vehicleId": vehicle["id"]},
        "leadSignals": {"interested": True, "readyToBuy": False, "wantsTestDrive": False},
        "suggestedAction": "show_vehicle_card",
        "quickReplies": ["Precio negociable?", "Financiamiento", "Agendar visita"]
    }, ensure_ascii=False)


# --- DI_VehiclePrice ---
DI_PRICE_TEMPLATES = [
    "cuánto cuesta el {make} {model}?",
    "precio del {make} {model}?",
    "a cuánto está ese?",
    "y el precio?",
    "cuánto vale?",
    "cuánto es el {make} {model} {year}?",
    "ese carro cuánto cuesta?",
    "a como es?",
    "y cuánto piden por ese?",
    "me dices el precio de ese?",
]

def di_price_response(vehicle: dict, dealer: dict) -> str:
    return sv_price_response(vehicle, dealer)


# --- DI_Financing ---
DI_FINANCING_TEMPLATES = [
    "tienen financiamiento?",
    "se pueden financiar los carros?",
    "con qué bancos trabajan?",
    "cuál sería la inicial del {make} {model}?",
    "hacen plan de pagos?",
    "financiamiento para el {make} {model}?",
    "cuáles son las opciones de financiar?",
    "puedo financiar alguno?",
    "trabajan con Banreservas?",
    "financian vehículos usados?",
]

def di_financing_response(vehicle: dict, dealer: dict) -> str:
    return sv_financing_response(vehicle, dealer)


# --- DI_TestDrive ---
DI_TESTDRIVE_TEMPLATES = [
    "puedo ir a ver los carros?",
    "quiero agendar una visita",
    "cuándo puedo ir a verlos?",
    "puedo ir a probar el {make} {model}?",
    "quiero hacer test drive",
    "puedo pasar este fin de semana?",
    "agendar cita para ver el {make} {model}",
    "quiero ir a verlos en persona",
    "puedo pasar mañana?",
    "a qué hora puedo ir?",
]

def di_testdrive_response(vehicle: dict, dealer: dict) -> str:
    return sv_testdrive_response(vehicle, dealer)


# --- DI_NotInInventory: Vehículo no disponible ---
DI_NOT_IN_INVENTORY_TEMPLATES = [
    "tienen BMW?",
    "busco un Mercedes Benz",
    "tienen Porsche?",
    "quiero un Tesla",
    "tienen Ferrari?",
    "busco un {make} {model} pero no veo ninguno",
    "no tienen nada de Audi?",
    "quiero un Lamborghini",
    "tienen motos?",
    "busco un camión grande",
    "no veo lo que busco",
    "no tienen lo que necesito",
]

def di_not_in_inventory_response(vehicles: list, dealer: dict) -> str:
    brands = list(set(v["make"] for v in vehicles))[:4]
    brands_str = ", ".join(brands)
    resp = _pick([
        f"Actualmente no tenemos eso en nuestro inventario. 😔 Contamos con marcas como {brands_str}. ¿Te gustaría ver alguna de estas opciones?",
        f"Ese modelo no está disponible en este momento. Tenemos opciones de {brands_str} que podrían interesarte. ¿Te muestro alternativas?",
    ])
    return json.dumps({
        "response": resp,
        "intent": "VehicleNotInInventory",
        "confidence": _rand_conf(0.85, 0.95),
        "isFallback": False,
        "parameters": {"notFound": True},
        "leadSignals": {"interested": True, "readyToBuy": False, "wantsTestDrive": False},
        "suggestedAction": None,
        "quickReplies": [f"Ver {brands[0]}" if brands else "Ver todo", "Ver SUVs", "Ver ofertas"]
    }, ensure_ascii=False)


# --- DI_CrossDealerRefusal: Intenta comparar con otros dealers ---
DI_CROSS_DEALER_REFUSAL_TEMPLATES = [
    "y en otro dealer cuánto cuesta?",
    "lo tienen más barato en otro lado?",
    "compara con lo que tiene AutoMax",
    "pero vi en otro sitio uno más barato",
    "en el dealer de al lado está más económico",
    "quiero comparar con otros dealers",
    "ese mismo carro lo vi en otro dealer",
    "tienes precios de la competencia?",
    "qué tal los precios vs otros dealers?",
    "hay otro dealer que tenga algo mejor?",
]

def di_cross_dealer_refusal_response(vehicles: list, dealer: dict) -> str:
    dealer_name = dealer.get("name", "nuestro dealer")
    resp = _pick([
        f"Solo puedo ayudarte con el inventario de {dealer_name}. No tengo acceso a información de otros dealers. ¿Te gustaría que te muestre más opciones de nuestro inventario?",
        f"Mi función es asistirte con los vehículos disponibles en {dealer_name}. No puedo comparar con otros dealers. ¿Quieres ver qué más tenemos disponible?",
        f"Solo manejo información de {dealer_name}. Para comparar con otros, puedes visitar los perfiles de otros dealers en okla.com.do. ¿Te ayudo con nuestro inventario?",
    ])
    return json.dumps({
        "response": resp,
        "intent": "CrossDealerRefusal",
        "confidence": _rand_conf(0.90, 0.98),
        "isFallback": False,
        "parameters": {"boundaryEnforced": True},
        "leadSignals": {"interested": True, "readyToBuy": False, "wantsTestDrive": False},
        "suggestedAction": None,
        "quickReplies": ["Ver nuestro inventario", "Buscar por precio", "Ver ofertas"]
    }, ensure_ascii=False)


# --- Shared intents for DI mode (delegate to SV versions with adapter) ---
DI_HOURS_TEMPLATES = SV_HOURS_TEMPLATES
def di_hours_response(vehicles: list, dealer: dict) -> str:
    return sv_hours_response({}, dealer)

DI_LOCATION_TEMPLATES = SV_LOCATION_TEMPLATES
def di_location_response(vehicles: list, dealer: dict) -> str:
    return sv_location_response({}, dealer)

DI_CONTACT_TEMPLATES = SV_CONTACT_TEMPLATES
def di_contact_response(vehicles: list, dealer: dict) -> str:
    return sv_contact_response({}, dealer)

DI_FAREWELL_TEMPLATES = SV_FAREWELL_TEMPLATES
def di_farewell_response(vehicles: list, dealer: dict) -> str:
    return sv_farewell_response({}, dealer)

DI_LEGAL_REFUSAL_TEMPLATES = SV_LEGAL_REFUSAL_TEMPLATES
def di_legal_refusal_response(vehicles: list, dealer: dict) -> str:
    return sv_legal_refusal_response({}, dealer)

DI_FALLBACK_TEMPLATES = SV_FALLBACK_TEMPLATES
def di_fallback_response(vehicles: list, dealer: dict) -> str:
    dealer_name = dealer.get("name", "nuestro dealer")
    resp = _pick([
        f"No estoy seguro de entender tu pregunta. 🤔 Puedo ayudarte a buscar vehículos, comparar opciones, o darte información sobre {dealer_name}. ¿Qué necesitas?",
        f"Disculpa, no entendí tu mensaje. ¿Quieres buscar vehículos, ver ofertas, o necesitas información del dealer?",
    ])
    return json.dumps({
        "response": resp,
        "intent": "Fallback",
        "confidence": _rand_conf(0.20, 0.45),
        "isFallback": True,
        "parameters": {},
        "leadSignals": {"interested": False, "readyToBuy": False, "wantsTestDrive": False},
        "suggestedAction": None,
        "quickReplies": ["Buscar vehículos", "Ver ofertas", "Hablar con asesor"]
    }, ensure_ascii=False)

DI_OUTOFSCOPE_TEMPLATES = SV_OUTOFSCOPE_TEMPLATES
def di_outofscope_response(vehicles: list, dealer: dict) -> str:
    dealer_name = dealer.get("name", "nuestro dealer")
    resp = _pick([
        f"Eso está fuera de mi área 😅 Soy el asistente de {dealer_name}. Puedo ayudarte a buscar vehículos, comparar opciones, o darte información del dealer. ¿En qué te ayudo?",
        f"¡Interesante pregunta! Pero solo puedo ayudarte con vehículos de {dealer_name}. ¿Buscas algo en particular?",
    ])
    return json.dumps({
        "response": resp,
        "intent": "OutOfScope",
        "confidence": _rand_conf(0.85, 0.95),
        "isFallback": False,
        "parameters": {},
        "leadSignals": {"interested": False, "readyToBuy": False, "wantsTestDrive": False},
        "suggestedAction": None,
        "quickReplies": ["Buscar vehículos", "Ver ofertas", "Hablar con asesor"]
    }, ensure_ascii=False)

DI_FRUSTRATED_TEMPLATES = SV_FRUSTRATED_TEMPLATES
def di_frustrated_response(vehicles: list, dealer: dict) -> str:
    return sv_frustrated_response({}, dealer)

DI_HUMAN_AGENT_TEMPLATES = SV_HUMAN_AGENT_TEMPLATES
def di_human_agent_response(vehicles: list, dealer: dict) -> str:
    dealer_name = dealer.get("name", "el dealer")
    resp = _pick([
        f"¡Claro! Te transfiero con un asesor de {dealer_name}. Un momento por favor... 🤝",
        f"Entendido, te conecto con nuestro equipo de ventas. Un asesor se pondrá en contacto contigo.",
    ])
    return json.dumps({
        "response": resp,
        "intent": "RequestHumanAgent",
        "confidence": _rand_conf(0.92, 0.99),
        "isFallback": False,
        "parameters": {"transferRequested": True},
        "leadSignals": {"interested": True, "readyToBuy": False, "wantsTestDrive": False},
        "suggestedAction": "transfer_to_agent",
        "quickReplies": []
    }, ensure_ascii=False)

DI_NEGOTIATE_TEMPLATES = SV_NEGOTIATE_TEMPLATES
def di_negotiate_response(vehicle: dict, dealer: dict) -> str:
    return sv_negotiate_response(vehicle, dealer)

DI_TRADEIN_TEMPLATES = SV_TRADEIN_TEMPLATES
def di_tradein_response(vehicle: dict, dealer: dict) -> str:
    return sv_tradein_response(vehicle, dealer)

DI_CASH_TEMPLATES = SV_CASH_TEMPLATES
def di_cash_response(vehicle: dict, dealer: dict) -> str:
    return sv_cash_response(vehicle, dealer)

DI_DOCUMENTS_TEMPLATES = SV_DOCUMENTS_TEMPLATES
def di_documents_response(vehicle: dict, dealer: dict) -> str:
    return sv_documents_response(vehicle, dealer)

DI_HISTORY_TEMPLATES = SV_HISTORY_TEMPLATES
def di_history_response(vehicle: dict, dealer: dict) -> str:
    return sv_history_response(vehicle, dealer)


# ============================================================
# ╔══════════════════════════════════════════════════════════╗
# ║  INTENT REGISTRIES — por modo                           ║
# ╚══════════════════════════════════════════════════════════╝
# ============================================================

# Single Vehicle intents
# Signature: response_fn(vehicle: dict, dealer: dict) -> str
SV_INTENT_REGISTRY = {
    "Greeting":             {"templates": SV_GREETING_TEMPLATES,       "response_fn": sv_greeting_response},
    "VehiclePrice":         {"templates": SV_PRICE_TEMPLATES,          "response_fn": sv_price_response},
    "VehicleDetails":       {"templates": SV_DETAILS_TEMPLATES,        "response_fn": sv_details_response},
    "FinancingInfo":        {"templates": SV_FINANCING_TEMPLATES,      "response_fn": sv_financing_response},
    "TestDriveSchedule":    {"templates": SV_TESTDRIVE_TEMPLATES,      "response_fn": sv_testdrive_response},
    "WarrantyInfo":         {"templates": SV_WARRANTY_TEMPLATES,       "response_fn": sv_warranty_response},
    "TradeIn":              {"templates": SV_TRADEIN_TEMPLATES,        "response_fn": sv_tradein_response},
    "CashPurchase":         {"templates": SV_CASH_TEMPLATES,           "response_fn": sv_cash_response},
    "NegotiatePrice":       {"templates": SV_NEGOTIATE_TEMPLATES,      "response_fn": sv_negotiate_response},
    "VehicleNotInInventory":{"templates": SV_OTHER_VEHICLE_TEMPLATES,  "response_fn": sv_other_vehicle_response},
    "ContactRequest":       {"templates": SV_CONTACT_TEMPLATES,        "response_fn": sv_contact_response},
    "DealerHours":          {"templates": SV_HOURS_TEMPLATES,          "response_fn": sv_hours_response},
    "DealerLocation":       {"templates": SV_LOCATION_TEMPLATES,       "response_fn": sv_location_response},
    "DocumentsRequired":    {"templates": SV_DOCUMENTS_TEMPLATES,      "response_fn": sv_documents_response},
    "VehicleHistory":       {"templates": SV_HISTORY_TEMPLATES,        "response_fn": sv_history_response},
    "LegalRefusal":         {"templates": SV_LEGAL_REFUSAL_TEMPLATES,  "response_fn": sv_legal_refusal_response},
    "Farewell":             {"templates": SV_FAREWELL_TEMPLATES,       "response_fn": sv_farewell_response},
    "Fallback":             {"templates": SV_FALLBACK_TEMPLATES,       "response_fn": sv_fallback_response},
    "OutOfScope":           {"templates": SV_OUTOFSCOPE_TEMPLATES,     "response_fn": sv_outofscope_response},
    "FrustratedUser":       {"templates": SV_FRUSTRATED_TEMPLATES,     "response_fn": sv_frustrated_response},
    "RequestHumanAgent":    {"templates": SV_HUMAN_AGENT_TEMPLATES,    "response_fn": sv_human_agent_response},
}

# Dealer Inventory intents
# takes_list=True: response_fn(vehicles: list, dealer: dict)
# takes_list=False: response_fn(vehicle: dict, dealer: dict)
# needs_pair=True: needs 2 vehicles for comparison
DI_INTENT_REGISTRY = {
    "Greeting":              {"templates": DI_GREETING_TEMPLATES,              "response_fn": di_greeting_response,              "takes_list": True},
    "VehicleSearch":         {"templates": DI_SEARCH_TEMPLATES,                "response_fn": di_search_response,                "takes_list": True},
    "VehicleComparison":     {"templates": DI_COMPARISON_TEMPLATES,            "response_fn": di_comparison_response,            "takes_list": True,  "needs_pair": True},
    "VehicleDetails":        {"templates": DI_DETAILS_TEMPLATES,               "response_fn": di_details_response,               "takes_list": False},
    "VehiclePrice":          {"templates": DI_PRICE_TEMPLATES,                 "response_fn": di_price_response,                 "takes_list": False},
    "FinancingInfo":         {"templates": DI_FINANCING_TEMPLATES,             "response_fn": di_financing_response,             "takes_list": False},
    "TestDriveSchedule":     {"templates": DI_TESTDRIVE_TEMPLATES,             "response_fn": di_testdrive_response,             "takes_list": False},
    "VehicleNotInInventory": {"templates": DI_NOT_IN_INVENTORY_TEMPLATES,      "response_fn": di_not_in_inventory_response,      "takes_list": True},
    "CrossDealerRefusal":    {"templates": DI_CROSS_DEALER_REFUSAL_TEMPLATES,  "response_fn": di_cross_dealer_refusal_response,  "takes_list": True},
    "NegotiatePrice":        {"templates": DI_NEGOTIATE_TEMPLATES,             "response_fn": di_negotiate_response,             "takes_list": False},
    "TradeIn":               {"templates": DI_TRADEIN_TEMPLATES,               "response_fn": di_tradein_response,               "takes_list": False},
    "CashPurchase":          {"templates": DI_CASH_TEMPLATES,                  "response_fn": di_cash_response,                  "takes_list": False},
    "DocumentsRequired":     {"templates": DI_DOCUMENTS_TEMPLATES,             "response_fn": di_documents_response,             "takes_list": False},
    "VehicleHistory":        {"templates": DI_HISTORY_TEMPLATES,               "response_fn": di_history_response,               "takes_list": False},
    "ContactRequest":        {"templates": DI_CONTACT_TEMPLATES,               "response_fn": di_contact_response,               "takes_list": True},
    "DealerHours":           {"templates": DI_HOURS_TEMPLATES,                 "response_fn": di_hours_response,                 "takes_list": True},
    "DealerLocation":        {"templates": DI_LOCATION_TEMPLATES,              "response_fn": di_location_response,              "takes_list": True},
    "LegalRefusal":          {"templates": DI_LEGAL_REFUSAL_TEMPLATES,         "response_fn": di_legal_refusal_response,         "takes_list": True},
    "Farewell":              {"templates": DI_FAREWELL_TEMPLATES,              "response_fn": di_farewell_response,              "takes_list": True},
    "Fallback":              {"templates": DI_FALLBACK_TEMPLATES,              "response_fn": di_fallback_response,              "takes_list": True},
    "OutOfScope":            {"templates": DI_OUTOFSCOPE_TEMPLATES,            "response_fn": di_outofscope_response,            "takes_list": True},
    "FrustratedUser":        {"templates": DI_FRUSTRATED_TEMPLATES,            "response_fn": di_frustrated_response,            "takes_list": True},
    "RequestHumanAgent":     {"templates": DI_HUMAN_AGENT_TEMPLATES,           "response_fn": di_human_agent_response,           "takes_list": True},
}

# Intent distribution weights (how often each intent is sampled)
SV_INTENT_DISTRIBUTION = {
    "Greeting":             0.12,
    "VehiclePrice":         0.14,
    "VehicleDetails":       0.14,
    "FinancingInfo":        0.10,
    "TestDriveSchedule":    0.08,
    "WarrantyInfo":         0.04,
    "TradeIn":              0.04,
    "CashPurchase":         0.03,
    "NegotiatePrice":       0.06,
    "VehicleNotInInventory":0.05,
    "ContactRequest":       0.04,
    "DealerHours":          0.03,
    "DealerLocation":       0.02,
    "DocumentsRequired":    0.02,
    "VehicleHistory":       0.02,
    "LegalRefusal":         0.01,
    "Farewell":             0.02,
    "Fallback":             0.01,
    "OutOfScope":           0.01,
    "FrustratedUser":       0.01,
    "RequestHumanAgent":    0.01,
}

DI_INTENT_DISTRIBUTION = {
    "Greeting":              0.10,
    "VehicleSearch":         0.18,
    "VehicleComparison":     0.08,
    "VehicleDetails":        0.10,
    "VehiclePrice":          0.08,
    "FinancingInfo":         0.06,
    "TestDriveSchedule":     0.06,
    "VehicleNotInInventory": 0.04,
    "CrossDealerRefusal":    0.03,
    "NegotiatePrice":        0.04,
    "TradeIn":               0.03,
    "CashPurchase":          0.02,
    "DocumentsRequired":     0.02,
    "VehicleHistory":        0.02,
    "ContactRequest":        0.03,
    "DealerHours":           0.02,
    "DealerLocation":        0.02,
    "LegalRefusal":          0.01,
    "Farewell":              0.02,
    "Fallback":              0.01,
    "OutOfScope":            0.01,
    "FrustratedUser":        0.01,
    "RequestHumanAgent":     0.01,
}


# ============================================================
# MULTI-TURN CHAINS — Secuencias naturales de intents
# ============================================================

SV_MULTI_TURN_CHAINS = [
    ["Greeting", "VehiclePrice", "FinancingInfo", "TestDriveSchedule", "Farewell"],
    ["Greeting", "VehicleDetails", "VehiclePrice", "NegotiatePrice", "ContactRequest"],
    ["Greeting", "VehicleDetails", "WarrantyInfo", "TradeIn", "TestDriveSchedule"],
    ["Greeting", "VehiclePrice", "CashPurchase", "DocumentsRequired", "Farewell"],
    ["Greeting", "VehicleDetails", "VehicleHistory", "TestDriveSchedule"],
    ["VehiclePrice", "NegotiatePrice", "FinancingInfo", "ContactRequest"],
    ["VehicleDetails", "VehiclePrice", "TradeIn", "Farewell"],
    ["Greeting", "VehicleNotInInventory"],
    ["Greeting", "VehicleDetails", "FrustratedUser", "RequestHumanAgent"],
    ["Greeting", "OutOfScope", "VehiclePrice"],
    ["VehiclePrice", "CashPurchase", "DealerLocation", "TestDriveSchedule"],
    ["Greeting", "LegalRefusal"],
    ["VehicleDetails", "FinancingInfo", "DocumentsRequired", "TestDriveSchedule", "Farewell"],
]

DI_MULTI_TURN_CHAINS = [
    ["Greeting", "VehicleSearch", "VehicleDetails", "VehiclePrice", "TestDriveSchedule", "Farewell"],
    ["Greeting", "VehicleSearch", "VehicleComparison", "VehicleDetails", "FinancingInfo"],
    ["Greeting", "VehicleSearch", "VehicleNotInInventory", "VehicleSearch"],
    ["Greeting", "VehicleSearch", "VehicleDetails", "NegotiatePrice", "ContactRequest"],
    ["Greeting", "VehicleSearch", "VehicleComparison", "TestDriveSchedule"],
    ["VehicleSearch", "VehicleDetails", "TradeIn", "FinancingInfo", "Farewell"],
    ["Greeting", "DealerHours", "DealerLocation", "VehicleSearch"],
    ["Greeting", "VehicleSearch", "VehicleDetails", "CashPurchase", "DocumentsRequired"],
    ["Greeting", "VehicleSearch", "FrustratedUser", "RequestHumanAgent"],
    ["Greeting", "OutOfScope", "VehicleSearch", "VehicleDetails"],
    ["VehicleSearch", "VehicleComparison", "VehiclePrice", "FinancingInfo", "TestDriveSchedule"],
    ["Greeting", "LegalRefusal"],
    ["VehicleSearch", "VehicleDetails", "VehicleHistory", "ContactRequest"],
    ["Greeting", "VehicleSearch", "CrossDealerRefusal", "VehicleSearch"],
]


# ============================================================
# AMBIGUOUS TEMPLATES — para probar el edge detection
# ============================================================

AMBIGUOUS_TEMPLATES = [
    "hmm no sé",
    "mmm",
    "ok",
    "puede ser",
    "a ver",
    "bueno",
    "interesante",
    "déjame pensar",
    "voy a consultar",
    "lo pienso",
]
