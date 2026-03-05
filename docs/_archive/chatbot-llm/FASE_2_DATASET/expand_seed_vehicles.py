#!/usr/bin/env python3
"""
OKLA Chatbot LLM — Seed Vehicle Expander
==========================================

Expands the seed_vehicles.json from 50 to 150+ vehicles with realistic
Dominican Republic market data. Generates vehicles procedurally with
diverse makes, models, body types, price ranges, and features.

This addresses WARN-2 from the AI Researcher Audit:
- Prevents model memorization of specific make/model/price combos
- Ensures diverse representation across body types and price ranges
- Reduces overfitting to specific brands

Usage:
    python expand_seed_vehicles.py
    python expand_seed_vehicles.py --count 200 --output seed_vehicles.json
"""

import argparse
import json
import random
import sys
from pathlib import Path

random.seed(42)

# ============================================================
# VEHICLE DATA — Dominican Republic Market
# ============================================================

VEHICLE_CATALOG = {
    "Toyota": {
        "SUV": [
            {"model": "RAV4", "years": [2021, 2022, 2023, 2024], "trims": ["LE", "XLE", "XLE Premium", "Limited"], "engine": "2.5L", "price_range": (2200000, 3200000)},
            {"model": "Fortuner", "years": [2022, 2023, 2024], "trims": ["SR5", "VX", "GR Sport"], "engine": "2.7L", "price_range": (2800000, 4200000)},
            {"model": "4Runner", "years": [2021, 2022, 2023], "trims": ["SR5", "TRD Off-Road", "Limited"], "engine": "4.0L V6", "price_range": (3500000, 5000000)},
            {"model": "Land Cruiser", "years": [2022, 2023], "trims": ["GX", "VX"], "engine": "3.3L V6 Twin-Turbo", "price_range": (6000000, 9500000)},
            {"model": "Corolla Cross", "years": [2023, 2024], "trims": ["L", "LE", "XLE"], "engine": "2.0L", "price_range": (1800000, 2600000)},
        ],
        "Sedan": [
            {"model": "Corolla", "years": [2021, 2022, 2023, 2024], "trims": ["L", "LE", "SE", "XSE"], "engine": "2.0L", "price_range": (1200000, 1900000)},
            {"model": "Camry", "years": [2022, 2023, 2024], "trims": ["LE", "SE", "XLE", "XSE"], "engine": "2.5L", "price_range": (2000000, 3000000)},
            {"model": "Yaris", "years": [2021, 2022, 2023], "trims": ["L", "LE", "XLE"], "engine": "1.5L", "price_range": (900000, 1400000)},
        ],
        "Pickup": [
            {"model": "Hilux", "years": [2022, 2023, 2024], "trims": ["SR", "SR5", "SRV"], "engine": "2.8L Turbo Diesel", "price_range": (2800000, 4000000)},
            {"model": "Tacoma", "years": [2023, 2024], "trims": ["SR", "SR5", "TRD Sport"], "engine": "2.4L Turbo", "price_range": (2500000, 3800000)},
        ],
        "Van": [
            {"model": "HiAce", "years": [2022, 2023], "trims": ["Commuter", "GL"], "engine": "2.8L Diesel", "price_range": (2200000, 3200000)},
        ],
    },
    "Hyundai": {
        "SUV": [
            {"model": "Tucson", "years": [2022, 2023, 2024], "trims": ["SE", "SEL", "Limited", "N Line"], "engine": "2.5L", "price_range": (1900000, 3000000)},
            {"model": "Santa Fe", "years": [2022, 2023, 2024], "trims": ["SE", "SEL", "Limited", "Calligraphy"], "engine": "2.5L Turbo", "price_range": (2500000, 4000000)},
            {"model": "Creta", "years": [2023, 2024], "trims": ["GL", "GLS", "Limited"], "engine": "1.5L", "price_range": (1400000, 2100000)},
            {"model": "Venue", "years": [2023, 2024], "trims": ["SE", "SEL", "Limited"], "engine": "1.6L", "price_range": (1100000, 1700000)},
            {"model": "Palisade", "years": [2023, 2024], "trims": ["SE", "SEL", "Limited", "Calligraphy"], "engine": "3.8L V6", "price_range": (3500000, 5500000)},
        ],
        "Sedan": [
            {"model": "Elantra", "years": [2022, 2023, 2024], "trims": ["SE", "SEL", "Limited", "N"], "engine": "2.0L", "price_range": (1300000, 2200000)},
            {"model": "Sonata", "years": [2022, 2023], "trims": ["SE", "SEL", "Limited"], "engine": "2.5L", "price_range": (1800000, 2800000)},
            {"model": "Accent", "years": [2021, 2022, 2023], "trims": ["SE", "SEL", "Limited"], "engine": "1.6L", "price_range": (850000, 1300000)},
        ],
    },
    "Honda": {
        "SUV": [
            {"model": "CR-V", "years": [2022, 2023, 2024], "trims": ["LX", "EX", "EX-L", "Touring"], "engine": "1.5L Turbo", "price_range": (2200000, 3400000)},
            {"model": "HR-V", "years": [2023, 2024], "trims": ["LX", "Sport", "EX-L"], "engine": "2.0L", "price_range": (1600000, 2400000)},
            {"model": "Pilot", "years": [2023, 2024], "trims": ["Sport", "EX-L", "Touring", "TrailSport"], "engine": "3.5L V6", "price_range": (3200000, 4800000)},
        ],
        "Sedan": [
            {"model": "Civic", "years": [2022, 2023, 2024], "trims": ["LX", "Sport", "EX", "Touring"], "engine": "2.0L", "price_range": (1400000, 2200000)},
            {"model": "Accord", "years": [2023, 2024], "trims": ["LX", "EX", "EX-L", "Touring"], "engine": "1.5L Turbo", "price_range": (2100000, 3200000)},
        ],
    },
    "Kia": {
        "SUV": [
            {"model": "Sportage", "years": [2022, 2023, 2024], "trims": ["LX", "EX", "SX", "SX Prestige"], "engine": "2.5L", "price_range": (1800000, 2900000)},
            {"model": "Seltos", "years": [2023, 2024], "trims": ["LX", "S", "EX", "SX"], "engine": "2.0L", "price_range": (1400000, 2200000)},
            {"model": "Sorento", "years": [2022, 2023, 2024], "trims": ["LX", "S", "EX", "SX"], "engine": "2.5L Turbo", "price_range": (2500000, 3800000)},
            {"model": "Telluride", "years": [2023, 2024], "trims": ["LX", "S", "EX", "SX", "X-Pro"], "engine": "3.8L V6", "price_range": (3500000, 5200000)},
        ],
        "Sedan": [
            {"model": "Forte", "years": [2022, 2023, 2024], "trims": ["FE", "LXS", "GT-Line", "GT"], "engine": "2.0L", "price_range": (1100000, 1800000)},
            {"model": "K5", "years": [2023, 2024], "trims": ["LX", "LXS", "GT-Line", "GT"], "engine": "1.6L Turbo", "price_range": (1800000, 2800000)},
        ],
    },
    "Nissan": {
        "SUV": [
            {"model": "X-Trail", "years": [2022, 2023, 2024], "trims": ["S", "SV", "SL", "Platinum"], "engine": "1.5L Turbo", "price_range": (2100000, 3200000)},
            {"model": "Kicks", "years": [2023, 2024], "trims": ["S", "SV", "SR"], "engine": "1.6L", "price_range": (1300000, 1900000)},
            {"model": "Pathfinder", "years": [2023, 2024], "trims": ["S", "SV", "SL", "Platinum"], "engine": "3.5L V6", "price_range": (3000000, 4500000)},
        ],
        "Sedan": [
            {"model": "Sentra", "years": [2022, 2023, 2024], "trims": ["S", "SV", "SR"], "engine": "2.0L", "price_range": (1200000, 1800000)},
            {"model": "Versa", "years": [2022, 2023], "trims": ["S", "SV", "SR"], "engine": "1.6L", "price_range": (850000, 1300000)},
        ],
        "Pickup": [
            {"model": "Frontier", "years": [2022, 2023, 2024], "trims": ["S", "SV", "PRO-4X", "PRO-X"], "engine": "3.8L V6", "price_range": (2800000, 4200000)},
            {"model": "Navara", "years": [2022, 2023], "trims": ["SE", "LE", "PRO-4X"], "engine": "2.5L Turbo Diesel", "price_range": (2400000, 3600000)},
        ],
    },
    "Mitsubishi": {
        "SUV": [
            {"model": "Outlander", "years": [2023, 2024], "trims": ["ES", "SE", "SEL", "GT"], "engine": "2.5L", "price_range": (2200000, 3400000)},
            {"model": "ASX", "years": [2023, 2024], "trims": ["ES", "SE", "GT"], "engine": "2.0L", "price_range": (1500000, 2200000)},
        ],
        "Pickup": [
            {"model": "L200", "years": [2022, 2023, 2024], "trims": ["GL", "GLS", "GT"], "engine": "2.4L Turbo Diesel", "price_range": (2300000, 3500000)},
        ],
        "Sedan": [
            {"model": "Mirage", "years": [2023, 2024], "trims": ["ES", "LE"], "engine": "1.2L", "price_range": (750000, 1100000)},
        ],
    },
    "Ford": {
        "SUV": [
            {"model": "Explorer", "years": [2023, 2024], "trims": ["Base", "XLT", "Limited", "ST"], "engine": "2.3L EcoBoost", "price_range": (3200000, 4800000)},
            {"model": "Escape", "years": [2023, 2024], "trims": ["S", "SE", "SEL", "Titanium"], "engine": "1.5L EcoBoost", "price_range": (2000000, 3000000)},
            {"model": "Bronco Sport", "years": [2023, 2024], "trims": ["Base", "Big Bend", "Outer Banks", "Badlands"], "engine": "1.5L EcoBoost", "price_range": (2200000, 3400000)},
        ],
        "Pickup": [
            {"model": "Ranger", "years": [2023, 2024], "trims": ["XL", "XLT", "Lariat", "Raptor"], "engine": "2.3L EcoBoost", "price_range": (2500000, 4500000)},
            {"model": "F-150", "years": [2023, 2024], "trims": ["XL", "XLT", "Lariat", "King Ranch"], "engine": "3.5L EcoBoost V6", "price_range": (3500000, 6000000)},
        ],
    },
    "Chevrolet": {
        "SUV": [
            {"model": "Tracker", "years": [2023, 2024], "trims": ["LS", "LT", "Premier"], "engine": "1.2L Turbo", "price_range": (1400000, 2100000)},
            {"model": "Equinox", "years": [2023, 2024], "trims": ["LS", "LT", "RS", "Premier"], "engine": "1.5L Turbo", "price_range": (2000000, 3000000)},
            {"model": "Tahoe", "years": [2023, 2024], "trims": ["LS", "LT", "RST", "Premier", "High Country"], "engine": "5.3L V8", "price_range": (4500000, 7500000)},
        ],
        "Sedan": [
            {"model": "Onix", "years": [2023, 2024], "trims": ["LS", "LT", "Premier"], "engine": "1.0L Turbo", "price_range": (900000, 1400000)},
        ],
        "Pickup": [
            {"model": "Colorado", "years": [2023, 2024], "trims": ["WT", "LT", "Z71", "ZR2"], "engine": "2.7L Turbo", "price_range": (2800000, 4500000)},
        ],
    },
    "Mazda": {
        "SUV": [
            {"model": "CX-5", "years": [2023, 2024], "trims": ["S", "Select", "Preferred", "Turbo"], "engine": "2.5L", "price_range": (2200000, 3200000)},
            {"model": "CX-50", "years": [2023, 2024], "trims": ["S", "Select", "Preferred", "Turbo"], "engine": "2.5L Turbo", "price_range": (2400000, 3600000)},
            {"model": "CX-30", "years": [2023, 2024], "trims": ["S", "Select", "Preferred", "Turbo"], "engine": "2.5L", "price_range": (1700000, 2500000)},
        ],
        "Sedan": [
            {"model": "Mazda3", "years": [2023, 2024], "trims": ["S", "Select", "Preferred", "Turbo"], "engine": "2.5L", "price_range": (1500000, 2400000)},
        ],
    },
    "Jeep": {
        "SUV": [
            {"model": "Wrangler", "years": [2023, 2024], "trims": ["Sport", "Sahara", "Rubicon"], "engine": "3.6L V6", "price_range": (3500000, 5500000)},
            {"model": "Grand Cherokee", "years": [2023, 2024], "trims": ["Laredo", "Limited", "Overland", "Summit"], "engine": "3.6L V6", "price_range": (3800000, 6000000)},
            {"model": "Compass", "years": [2023, 2024], "trims": ["Sport", "Latitude", "Limited", "Trailhawk"], "engine": "2.4L", "price_range": (2200000, 3200000)},
            {"model": "Renegade", "years": [2023, 2024], "trims": ["Sport", "Latitude", "Limited", "Trailhawk"], "engine": "1.3L Turbo", "price_range": (1800000, 2800000)},
        ],
    },
    "Volkswagen": {
        "SUV": [
            {"model": "Tiguan", "years": [2023, 2024], "trims": ["S", "SE", "SE R-Line", "SEL"], "engine": "2.0L Turbo", "price_range": (2200000, 3200000)},
            {"model": "Taos", "years": [2023, 2024], "trims": ["S", "SE", "SEL"], "engine": "1.5L Turbo", "price_range": (1800000, 2600000)},
        ],
        "Sedan": [
            {"model": "Jetta", "years": [2023, 2024], "trims": ["S", "Sport", "SE", "SEL"], "engine": "1.5L Turbo", "price_range": (1400000, 2100000)},
        ],
    },
    "Suzuki": {
        "SUV": [
            {"model": "Vitara", "years": [2023, 2024], "trims": ["GL", "GLX", "GLX Turbo"], "engine": "1.4L Turbo", "price_range": (1600000, 2400000)},
            {"model": "Jimny", "years": [2023, 2024], "trims": ["GL", "GLX"], "engine": "1.5L", "price_range": (1800000, 2400000)},
        ],
        "Sedan": [
            {"model": "Swift", "years": [2023, 2024], "trims": ["GL", "GLX", "Sport"], "engine": "1.2L", "price_range": (850000, 1300000)},
        ],
    },
}

COLORS = [
    "Blanco", "Blanco Perla", "Negro", "Gris", "Gris Oscuro", "Gris Plata",
    "Plateado", "Rojo", "Rojo Carmesí", "Azul", "Azul Oscuro", "Azul Marino",
    "Verde", "Bronce", "Beige", "Marrón", "Champagne", "Naranja",
]

INTERIOR_COLORS = ["Negro", "Gris", "Beige", "Marrón", "Negro/Rojo"]

TRANSMISSIONS = ["Automática", "Manual", "CVT", "Automática de Doble Embrague"]

FUEL_TYPES = {
    "default": "Gasolina",
    "diesel_models": ["Hilux", "L200", "Navara", "Frontier", "HiAce"],
    "hybrid_option": ["RAV4", "Corolla Cross", "CR-V", "Tucson", "Sportage"],
}

DRIVE_TYPES = {"SUV": ["FWD", "AWD", "4WD"], "Sedan": ["FWD"], "Pickup": ["4WD", "RWD"], "Van": ["RWD"]}

COMMON_FEATURES = {
    "basic": [
        "Apple CarPlay/Android Auto", "Pantalla táctil", "Bluetooth",
        "Cámara de reversa", "Control crucero", "Aire acondicionado",
    ],
    "mid": [
        "Apple CarPlay/Android Auto", "Pantalla táctil 8\"", "Bluetooth",
        "Cámara de reversa", "Control crucero adaptativo",
        "Sensores de estacionamiento", "Llave inteligente",
    ],
    "premium": [
        "Apple CarPlay/Android Auto", "Pantalla táctil 10.25\"",
        "Cámara 360°", "Asientos en piel", "Techo panorámico",
        "Control crucero adaptativo", "Asistente de carril",
        "Sensores de estacionamiento", "Llave inteligente", "Carga inalámbrica",
    ],
    "luxury": [
        "Apple CarPlay/Android Auto inalámbrico", "Pantalla táctil 12.3\"",
        "Cámara 360° con surround view", "Asientos en piel ventilados",
        "Techo panorámico", "Head-up display", "Sonido premium",
        "Control crucero adaptativo", "Asistente de carril",
        "Estacionamiento automático", "Suspensión adaptativa",
    ],
}


def generate_vehicle(vehicle_id: int, make: str, body_type: str, spec: dict) -> dict:
    """Generate a single vehicle with realistic data."""
    year = random.choice(spec["years"])
    trim = random.choice(spec["trims"])
    
    base_price = random.randint(spec["price_range"][0], spec["price_range"][1])
    # Round to nearest 50,000
    price = round(base_price / 50000) * 50000

    is_on_sale = random.random() < 0.25
    original_price = int(price * random.uniform(1.05, 1.15)) if is_on_sale else price
    original_price = round(original_price / 50000) * 50000

    # Mileage based on year
    current_year = 2026
    years_old = current_year - year
    base_mileage = years_old * random.randint(8000, 18000)
    mileage = max(0, base_mileage + random.randint(-2000, 5000))
    if year >= 2024:
        mileage = random.randint(0, 12000)

    # Fuel type
    model_name = spec["model"]
    if model_name in FUEL_TYPES["diesel_models"]:
        fuel_type = "Diesel"
    elif model_name in FUEL_TYPES["hybrid_option"] and random.random() < 0.2:
        fuel_type = "Híbrido"
    else:
        fuel_type = "Gasolina"

    # Transmission
    if body_type == "Pickup":
        transmission = random.choice(["Automática", "Manual"]) if fuel_type == "Diesel" else "Automática"
    elif body_type == "Van":
        transmission = "Automática"
    else:
        transmission = random.choice(["Automática", "CVT"]) if random.random() < 0.85 else "Manual"

    # Features based on trim level
    trim_index = spec["trims"].index(trim)
    trim_ratio = trim_index / max(1, len(spec["trims"]) - 1)
    if trim_ratio >= 0.75:
        features = random.sample(COMMON_FEATURES["luxury"], min(8, len(COMMON_FEATURES["luxury"])))
    elif trim_ratio >= 0.5:
        features = random.sample(COMMON_FEATURES["premium"], min(7, len(COMMON_FEATURES["premium"])))
    elif trim_ratio >= 0.25:
        features = random.sample(COMMON_FEATURES["mid"], min(6, len(COMMON_FEATURES["mid"])))
    else:
        features = random.sample(COMMON_FEATURES["basic"], min(5, len(COMMON_FEATURES["basic"])))

    # Tags
    tags = []
    if body_type == "SUV":
        tags.extend(random.sample(["familiar", "seguro", "espacioso", "todoterreno"], 2))
    elif body_type == "Sedan":
        tags.extend(random.sample(["económico", "eficiente", "elegante", "urbano"], 2))
    elif body_type == "Pickup":
        tags.extend(random.sample(["trabajo", "resistente", "4x4", "potente"], 2))
    if is_on_sale:
        tags.append("oferta")
    if fuel_type == "Híbrido":
        tags.append("eco")
    if price < 1500000:
        tags.append("económico")
    elif price > 4000000:
        tags.append("premium")

    drive_options = DRIVE_TYPES.get(body_type, ["FWD"])
    
    return {
        "id": f"v{vehicle_id:03d}",
        "make": make,
        "model": model_name,
        "year": year,
        "trim": trim,
        "price": price,
        "originalPrice": original_price,
        "isOnSale": is_on_sale,
        "bodyType": body_type,
        "fuelType": fuel_type,
        "transmission": transmission,
        "mileage": mileage,
        "engineSize": spec["engine"],
        "exteriorColor": random.choice(COLORS),
        "interiorColor": random.choice(INTERIOR_COLORS),
        "description": f"{make} {model_name} {year} {trim}, {spec['engine']}, {transmission.lower()}, {fuel_type.lower()}. {'¡En oferta!' if is_on_sale else ''}".strip(),
        "isAvailable": True,
        "isFeatured": random.random() < 0.15,
        "tags": tags,
        "doors": 5 if body_type != "Van" else 4,
        "seats": 7 if body_type in ("Van",) or (body_type == "SUV" and model_name in ("Palisade", "Pilot", "Tahoe", "Grand Cherokee", "Pathfinder", "Sorento", "Telluride", "Explorer")) else 5,
        "driveType": random.choice(drive_options),
        "features": features,
    }


def generate_vehicles(count: int = 160) -> list:
    """Generate a diverse set of vehicles."""
    vehicles = []
    vid = 1

    # Flatten all specs
    all_specs = []
    for make, body_types in VEHICLE_CATALOG.items():
        for body_type, models in body_types.items():
            for spec in models:
                all_specs.append((make, body_type, spec))

    # Generate vehicles proportionally
    while len(vehicles) < count:
        make, body_type, spec = random.choice(all_specs)
        v = generate_vehicle(vid, make, body_type, spec)
        vehicles.append(v)
        vid += 1

    # Ensure some are unavailable (5%)
    for v in random.sample(vehicles, max(1, int(len(vehicles) * 0.05))):
        v["isAvailable"] = False

    return vehicles


def main():
    parser = argparse.ArgumentParser(description="Expand seed vehicles")
    parser.add_argument("--count", type=int, default=160, help="Number of vehicles (default: 160)")
    parser.add_argument("--output", default=None, help="Output file (default: overwrite seed_vehicles.json)")
    args = parser.parse_args()

    script_dir = Path(__file__).parent
    output_path = Path(args.output) if args.output else script_dir / "seed_vehicles.json"

    vehicles = generate_vehicles(args.count)

    # Stats
    makes = {}
    body_types = {}
    price_ranges = {"<1M": 0, "1-2M": 0, "2-3M": 0, "3-5M": 0, "5M+": 0}
    for v in vehicles:
        makes[v["make"]] = makes.get(v["make"], 0) + 1
        body_types[v["bodyType"]] = body_types.get(v["bodyType"], 0) + 1
        p = v["price"]
        if p < 1000000: price_ranges["<1M"] += 1
        elif p < 2000000: price_ranges["1-2M"] += 1
        elif p < 3000000: price_ranges["2-3M"] += 1
        elif p < 5000000: price_ranges["3-5M"] += 1
        else: price_ranges["5M+"] += 1

    print(f"Generated {len(vehicles)} vehicles")
    print(f"\nMakes: {json.dumps(makes, indent=2)}")
    print(f"\nBody Types: {json.dumps(body_types, indent=2)}")
    print(f"\nPrice Ranges: {json.dumps(price_ranges, indent=2)}")
    print(f"\nOn Sale: {sum(1 for v in vehicles if v['isOnSale'])}")
    print(f"Featured: {sum(1 for v in vehicles if v['isFeatured'])}")

    with open(output_path, "w", encoding="utf-8") as f:
        json.dump(vehicles, f, indent=2, ensure_ascii=False)
    print(f"\n✅ Saved to {output_path}")


if __name__ == "__main__":
    main()
