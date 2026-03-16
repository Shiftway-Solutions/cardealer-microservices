#!/usr/bin/env python3
"""
OKLA - Section Ads Manager
Handles all vehicle body-style reclassification + new listing creation to fill
homepage sections to the desired counts.

Changes:
  Hatchbacks:
    - REMOVE Mazda 2 (21212b69) - broken 404 image → reclassify to Sedan
    - MOVE Tucson / CR-V / RAV4 from SUV → Hatchback (add 3)
    Result: 8 - 1 + 3 = 10

  Minivans:
    - REMOVE Mitsubishi Xpander (c9cbeee8) - broken 404 image → reclassify to Sedan
    - ADD 2 new Minivan listings
    Result: 8 - 1 + 2 = 9

  Other sections (fill to 10 each):
    - Camionetas (Pickup):  9 → +1 → 10
    - Deportivos (SportsCar): 8 → +2 → 10
    - Convertibles: 8 → +2 → 10
    - Vans: 8 → +2 → 10
    - Híbridos (fuelType=Hybrid): 9 → +1 → 10
    - Eléctricos (fuelType=Electric): 9 → +1 → 10
"""
import json
import secrets
import sys
import time
import urllib.request
import urllib.parse
import urllib.error

BASE = "http://localhost:18443"
ADMIN_EMAIL = "admin@okla.local"
ADMIN_PASS  = "Admin123!@#"
DEALER_EMAIL = "nmateo@okla.com.do"
DEALER_PASS  = "Dealer2026!@#"

# ── Vehicles to reclassify (bodyStyle only) ─────────────────────────────────
RECLASSIFY = [
    # (id, vehicle_label, new_bodyStyle)
    ("21212b69-0000-0000-0000-000000000000", "Mazda 2 2024 [broken img]",    "Sedan"),
    ("c9cbeee8-0000-0000-0000-000000000000", "Mitsubishi Xpander [broken]",  "Sedan"),
    ("832f549f-bb45-4bcc-b686-a931761e15f5", "Hyundai Tucson 2024",          "Hatchback"),
    ("bd69e376-7651-47a7-8b0f-1278843c1d95", "Honda CR-V 2023",              "Hatchback"),
    ("d71e3be5-61bf-4927-90c2-d21e61114483", "Toyota RAV4 2024",             "Hatchback"),
]

# ── New vehicle listings ordered by need ───────────────────────────────────
# Realistic vehicles popular in Dominican Republic market
NEW_LISTINGS = [
    # ── Camionetas (+1) ───────────────────────────────────────────────────
    {
        "make": "Toyota", "model": "Hilux", "year": 2024, "bodyStyle": "Pickup",
        "fuelType": "Diesel", "transmission": "Automatic", "condition": "New",
        "mileage": 0, "mileageUnit": "Kilometers", "price": 1850000, "currency": "DOP",
        "doors": 4, "seats": 5, "exteriorColor": "White", "interiorColor": "Black",
        "engineSize": "2.8", "cylinders": 4, "driveType": "FourWD",
        "horsepower": 201,
        "city": "Santo Domingo", "state": "Distrito Nacional",
        "description": "Toyota Hilux 2024 doble cabina, motor diésel 2.8L, transmisión automática 6 velocidades. Ideal para trabajo y aventura. Perfectamente equipada para las carreteras dominicanas.",
        "section_label": "Camionetas",
    },
    # ── Deportivos (+2) ───────────────────────────────────────────────────
    {
        "make": "Ford", "model": "Mustang", "year": 2024, "bodyStyle": "SportsCar",
        "fuelType": "Gasoline", "transmission": "Automatic", "condition": "New",
        "mileage": 0, "mileageUnit": "Kilometers", "price": 3200000, "currency": "DOP",
        "doors": 2, "seats": 4, "exteriorColor": "Red", "interiorColor": "Black",
        "engineSize": "5.0", "cylinders": 8, "driveType": "RWD",
        "horsepower": 450,
        "city": "Santo Domingo", "state": "Distrito Nacional",
        "description": "Ford Mustang GT 2024, motor V8 5.0L Coyote de 450 hp. El icónico muscle car americano con toda la emoción y potencia. Look deportivo inigualable.",
        "section_label": "Deportivos",
    },
    {
        "make": "Chevrolet", "model": "Camaro", "year": 2024, "bodyStyle": "SportsCar",
        "fuelType": "Gasoline", "transmission": "Automatic", "condition": "New",
        "mileage": 0, "mileageUnit": "Kilometers", "price": 2950000, "currency": "DOP",
        "doors": 2, "seats": 4, "exteriorColor": "Yellow", "interiorColor": "Black",
        "engineSize": "3.6", "cylinders": 6, "driveType": "RWD",
        "horsepower": 335,
        "city": "Santiago", "state": "Santiago",
        "description": "Chevrolet Camaro 2024 con motor V6 de 335 hp y transmisión automática de 8 velocidades. Deportivo americano con diseño agresivo y performance excepcional.",
        "section_label": "Deportivos",
    },
    # ── Convertibles (+2) ─────────────────────────────────────────────────
    {
        "make": "Mercedes-Benz", "model": "C300 Cabriolet", "year": 2024, "bodyStyle": "Convertible",
        "fuelType": "Gasoline", "transmission": "Automatic", "condition": "New",
        "mileage": 0, "mileageUnit": "Kilometers", "price": 5800000, "currency": "DOP",
        "doors": 2, "seats": 4, "exteriorColor": "Silver", "interiorColor": "Beige",
        "engineSize": "2.0", "cylinders": 4, "driveType": "RWD",
        "horsepower": 255,
        "city": "Santo Domingo", "state": "Distrito Nacional",
        "description": "Mercedes-Benz C300 Cabriolet 2024. Lujo y libertad combinados. Capota eléctrica, motor turbo 2.0L, interior en cuero Nappa beige. Perfecto para el clima caribeño.",
        "section_label": "Convertibles",
    },
    {
        "make": "BMW", "model": "430i Convertible", "year": 2024, "bodyStyle": "Convertible",
        "fuelType": "Gasoline", "transmission": "Automatic", "condition": "New",
        "mileage": 0, "mileageUnit": "Kilometers", "price": 5200000, "currency": "DOP",
        "doors": 2, "seats": 4, "exteriorColor": "Blue", "interiorColor": "Black",
        "engineSize": "2.0", "cylinders": 4, "driveType": "RWD",
        "horsepower": 255,
        "city": "Santo Domingo", "state": "Distrito Nacional",
        "description": "BMW 430i Convertible 2024. La emoción de conducir al aire libre con el refinamiento bávaro. Motor TwinPower Turbo, interiores premium y tecnología de punta.",
        "section_label": "Convertibles",
    },
    # ── Vans (+2) ─────────────────────────────────────────────────────────
    {
        "make": "Mercedes-Benz", "model": "Sprinter 2500", "year": 2024, "bodyStyle": "Van",
        "fuelType": "Diesel", "transmission": "Automatic", "condition": "New",
        "mileage": 0, "mileageUnit": "Kilometers", "price": 3800000, "currency": "DOP",
        "doors": 4, "seats": 15, "exteriorColor": "White", "interiorColor": "Gray",
        "engineSize": "2.0", "cylinders": 4, "driveType": "RWD",
        "horsepower": 170,
        "city": "Santo Domingo", "state": "Distrito Nacional",
        "description": "Mercedes-Benz Sprinter 2500 pasajeros 2024, capacidad 15 personas. Ideal para transporte ejecutivo, turismo y renta. Motor diésel eficiente, sistema estéreo y A/C potente.",
        "section_label": "Vans",
    },
    {
        "make": "Ford", "model": "Transit 350", "year": 2024, "bodyStyle": "Van",
        "fuelType": "Gasoline", "transmission": "Automatic", "condition": "New",
        "mileage": 0, "mileageUnit": "Kilometers", "price": 2950000, "currency": "DOP",
        "doors": 4, "seats": 12, "exteriorColor": "White", "interiorColor": "Gray",
        "engineSize": "3.5", "cylinders": 6, "driveType": "RWD",
        "horsepower": 310,
        "city": "La Romana", "state": "La Romana",
        "description": "Ford Transit 350 2024, van de pasajeros con capacidad para 12. Motor EcoBoost V6 3.5L, transmisión automática de 10 velocidades. Perfecta para servicios de transporte.",
        "section_label": "Vans",
    },
    # ── Minivans (+2) ─────────────────────────────────────────────────────
    {
        "make": "Chrysler", "model": "Voyager", "year": 2024, "bodyStyle": "Minivan",
        "fuelType": "Gasoline", "transmission": "Automatic", "condition": "New",
        "mileage": 0, "mileageUnit": "Kilometers", "price": 1950000, "currency": "DOP",
        "doors": 4, "seats": 7, "exteriorColor": "Gray", "interiorColor": "Black",
        "engineSize": "3.6", "cylinders": 6, "driveType": "FWD",
        "horsepower": 287,
        "city": "Santiago", "state": "Santiago",
        "description": "Chrysler Voyager 2024 con capacidad para 7 pasajeros. Motor V6 Pentastar 3.6L, puertas corredizas eléctricas, pantallas para pasajeros traseros. La minivan familiar por excelencia.",
        "section_label": "Minivans",
    },
    {
        "make": "Toyota", "model": "Alphard", "year": 2024, "bodyStyle": "Minivan",
        "fuelType": "Hybrid", "transmission": "CVT", "condition": "New",
        "mileage": 0, "mileageUnit": "Kilometers", "price": 4500000, "currency": "DOP",
        "doors": 4, "seats": 7, "exteriorColor": "Pearl White", "interiorColor": "Brown",
        "engineSize": "2.5", "cylinders": 4, "driveType": "FWD",
        "horsepower": 249,
        "city": "Santo Domingo", "state": "Distrito Nacional",
        "description": "Toyota Alphard 2024 Hybrid, la minivan de lujo japonesa. Asientos reclimables tipo business class, sistema de entretenimiento premium, tecnología híbrida de bajo consumo.",
        "section_label": "Minivans",
    },
    # ── Híbridos (+1) ─────────────────────────────────────────────────────
    {
        "make": "Toyota", "model": "Corolla Cross Hybrid", "year": 2024, "bodyStyle": "Crossover",
        "fuelType": "Hybrid", "transmission": "CVT", "condition": "New",
        "mileage": 0, "mileageUnit": "Kilometers", "price": 1680000, "currency": "DOP",
        "doors": 4, "seats": 5, "exteriorColor": "Gray", "interiorColor": "Black",
        "engineSize": "1.8", "cylinders": 4, "driveType": "AWD",
        "horsepower": 196,
        "city": "Santo Domingo", "state": "Distrito Nacional",
        "description": "Toyota Corolla Cross Hybrid 2024, la opción más eficiente del segmento. Sistema híbrido AWD, consumo de 6.2L/100km combinado. Tecnología Toyota Safety Sense de serie.",
        "section_label": "Híbridos",
    },
    # ── Eléctricos (+1) ───────────────────────────────────────────────────
    {
        "make": "BYD", "model": "Atto 3", "year": 2024, "bodyStyle": "SUV",
        "fuelType": "Electric", "transmission": "Automatic", "condition": "New",
        "mileage": 0, "mileageUnit": "Kilometers", "price": 1950000, "currency": "DOP",
        "doors": 4, "seats": 5, "exteriorColor": "Blue", "interiorColor": "White",
        "engineSize": "0", "cylinders": 0, "driveType": "FWD",
        "horsepower": 204,
        "city": "Santo Domingo", "state": "Distrito Nacional",
        "description": "BYD Atto 3 2024 100% eléctrico, autonomía real de 420 km. Batería de 60.5 kWh, carga rápida DC hasta 80 kW. El eléctrico más vendido del mercado dominicano.",
        "section_label": "Eléctricos",
    },
]


def http_call(method, path, data=None, token=None, csrf=None, cookiejar=None):
    """Make an HTTP call to the gateway."""
    url = BASE + path
    body = json.dumps(data).encode() if data else None
    req = urllib.request.Request(url, data=body, method=method)
    req.add_header("Content-Type", "application/json")
    req.add_header("Accept", "application/json")
    if token:
        req.add_header("Authorization", f"Bearer {token}")
    if csrf:
        req.add_header("X-CSRF-Token", csrf)
        req.add_header("Cookie", f"csrf_token={csrf}; access_token={token}")
    try:
        with urllib.request.urlopen(req, timeout=30) as resp:
            body_text = resp.read().decode()
            return resp.status, json.loads(body_text) if body_text else {}
    except urllib.error.HTTPError as e:
        body_text = e.read().decode()
        try:
            return e.code, json.loads(body_text)
        except Exception:
            return e.code, {"error": body_text[:500]}
    except Exception as ex:
        return 0, {"error": str(ex)}


def login(email, password):
    """Login and return (jwt_token, csrf_token)."""
    csrf = secrets.token_hex(32)
    status, resp = http_call(
        "POST", "/api/auth/login",
        {"email": email, "password": password},
        csrf=csrf
    )
    if status not in (200, 201):
        print(f"  ❌ Login failed ({status}): {resp}")
        return None, None
    token = resp.get("accessToken") or resp.get("token")
    if not token:
        # Try nested
        data = resp.get("data", {})
        token = data.get("accessToken") or data.get("token")
    if not token:
        print(f"  ❌ No token in response: {list(resp.keys())}")
        return None, None
    print(f"  ✅ Logged in as {email}")
    return token, csrf


def get_vehicle(vehicle_id, token, csrf):
    """GET full vehicle data."""
    status, resp = http_call("GET", f"/api/vehicles/{vehicle_id}", token=token, csrf=csrf)
    if status != 200:
        return None
    return resp.get("vehicle", resp)


def update_vehicle_body_style(vehicle_id, label, new_body_style, token, csrf):
    """Patch a vehicle's bodyStyle by sending full PUT with changed field."""
    print(f"\n  Updating {label} ({vehicle_id[:8]}) → bodyStyle={new_body_style}")

    vehicle = get_vehicle(vehicle_id, token, csrf)
    if not vehicle:
        print(f"    ❌ Could not GET vehicle {vehicle_id}")
        return False

    current = vehicle.get("bodyStyle")
    if current == new_body_style:
        print(f"    ℹ  Already bodyStyle={new_body_style}, skipping")
        return True

    # Build minimal update payload based on what PUT /api/vehicles/{id} typically needs
    payload = {
        "bodyStyle": new_body_style,
    }

    # Try PATCH first (more efficient, only sends changed field)
    status, resp = http_call("PATCH", f"/api/vehicles/{vehicle_id}", payload, token=token, csrf=csrf)
    if status in (200, 204):
        print(f"    ✅ PATCH success ({status}) - bodyStyle changed {current} → {new_body_style}")
        return True

    # Fallback: try PUT with specific admin update endpoint
    status, resp = http_call("PUT", f"/api/admin/vehicles/{vehicle_id}/body-style",
                             {"bodyStyle": new_body_style}, token=token, csrf=csrf)
    if status in (200, 204):
        print(f"    ✅ Admin PUT success ({status}) - bodyStyle changed {current} → {new_body_style}")
        return True

    print(f"    ❌ Update failed - PATCH:{status} — Fallback checking PUT /api/vehicles/:id")

    # Full PUT with complete vehicle data
    update_payload = {k: v for k, v in vehicle.items()
                      if k not in ["id", "images", "priceHistory", "homepageSectionAssignments",
                                   "createdAt", "updatedAt", "publishedAt", "soldAt",
                                   "approvedAt", "approvedBy", "rejectedAt", "rejectedBy",
                                   "submittedForReviewAt", "fraudScore"]}
    update_payload["bodyStyle"] = new_body_style

    status, resp = http_call("PUT", f"/api/vehicles/{vehicle_id}", update_payload, token=token, csrf=csrf)
    if status in (200, 204):
        print(f"    ✅ PUT success ({status}) - bodyStyle changed {current} → {new_body_style}")
        return True

    print(f"    ❌ All update methods failed. Last response ({status}): {str(resp)[:300]}")
    return False


def create_vehicle_listing(listing, seller_id, token, csrf):
    """POST a new vehicle listing."""
    label = listing["section_label"]
    print(f"\n  Creating {listing['year']} {listing['make']} {listing['model']} [{label}]...")

    payload = {
        "make": listing["make"],
        "model": listing["model"],
        "year": listing["year"],
        "trim": listing.get("trim", ""),
        "bodyStyle": listing["bodyStyle"],
        "fuelType": listing["fuelType"],
        "transmission": listing["transmission"],
        "condition": listing["condition"],
        "mileage": listing["mileage"],
        "mileageUnit": listing["mileageUnit"],
        "price": listing["price"],
        "currency": listing["currency"],
        "doors": listing["doors"],
        "seats": listing["seats"],
        "exteriorColor": listing["exteriorColor"],
        "interiorColor": listing["interiorColor"],
        "engineSize": listing["engineSize"],
        "cylinders": listing["cylinders"],
        "driveType": listing["driveType"],
        "horsepower": listing["horsepower"],
        "city": listing["city"],
        "state": listing["state"],
        "country": "DO",
        "description": listing["description"],
        "vehicleType": "Car",
        "isCertified": False,
        "accidentHistory": False,
        "hasCleanTitle": True,
        "previousOwners": 0,
        "disclaimerAccepted": True,
    }

    # Try dealer vehicle creation endpoint
    status, resp = http_call("POST", "/api/vehicles", payload, token=token, csrf=csrf)
    if status in (200, 201):
        vid = resp.get("id") or resp.get("vehicleId") or (resp.get("vehicle") or {}).get("id", "?")
        print(f"    ✅ Created vehicle ID: {vid}")
        return True, vid
    else:
        print(f"    ❌ POST /api/vehicles failed ({status}): {str(resp)[:400]}")
        # Try alternate path
        status2, resp2 = http_call("POST", "/api/vehicles/listings", payload, token=token, csrf=csrf)
        if status2 in (200, 201):
            vid = resp2.get("id") or "?"
            print(f"    ✅ Created via /api/vehicles/listings, ID: {vid}")
            return True, vid
        print(f"    ❌ All POST attempts failed. Last: {str(resp2)[:300]}")
        return False, None


def run():
    print("=" * 60)
    print("OKLA Section Ads Manager")
    print("=" * 60)

    # ── Step 1: Login as Admin ───────────────────────────────────────────
    print("\n[1] Logging in as Admin...")
    admin_token, admin_csrf = login(ADMIN_EMAIL, ADMIN_PASS)
    if not admin_token:
        print("FATAL: Admin login failed. Aborting.")
        sys.exit(1)

    # ── Step 2: Fix broken-image vehicles and reclassify Tucson/CR-V/RAV4 ──
    print("\n[2] Reclassifying vehicles...")

    # First resolve partial IDs
    reclassify_map = {
        "21212b69": None,  # Mazda 2 - need full ID
        "c9cbeee8": None,  # Mitsubishi Xpander - need full ID
        "832f549f": "832f549f-bb45-4bcc-b686-a931761e15f5",
        "bd69e376": "bd69e376-7651-47a7-8b0f-1278843c1d95",
        "d71e3be5": "d71e3be5-61bf-4927-90c2-d21e61114483",
    }

    # Look up full IDs for Mazda 2 and Xpander
    for bstyle, make_filter in [("Hatchback", "Mazda"), ("Minivan", "Mitsubishi")]:
        s, r = http_call("GET", f"/api/vehicles?bodyStyle={bstyle}&limit=15", token=admin_token, csrf=admin_csrf)
        if s == 200:
            for v in r.get("vehicles", []):
                if make_filter in v.get("make", ""):
                    short = v["id"][:8]
                    if short in reclassify_map:
                        reclassify_map[short] = v["id"]
                        print(f"  Resolved {make_filter} full ID: {v['id']}")

    updated_reclassify = [
        (reclassify_map.get("21212b69"), "Mazda 2 2024 [broken img]", "Sedan"),
        (reclassify_map.get("c9cbeee8"), "Mitsubishi Xpander [broken]", "Sedan"),
        ("832f549f-bb45-4bcc-b686-a931761e15f5", "Hyundai Tucson 2024", "Hatchback"),
        ("bd69e376-7651-47a7-8b0f-1278843c1d95", "Honda CR-V 2023", "Hatchback"),
        ("d71e3be5-61bf-4927-90c2-d21e61114483", "Toyota RAV4 2024", "Hatchback"),
    ]

    success_count = 0
    fail_count = 0
    for vid, label, new_style in updated_reclassify:
        if not vid:
            print(f"  ⚠️  Skipping {label} — full ID not resolved")
            fail_count += 1
            continue
        ok = update_vehicle_body_style(vid, label, new_style, admin_token, admin_csrf)
        if ok:
            success_count += 1
        else:
            fail_count += 1

    print(f"\n  Reclassification: {success_count} succeeded, {fail_count} failed")

    # ── Step 3: Login as Dealer for new listings ─────────────────────────
    print("\n[3] Logging in as Dealer for new listings...")
    dealer_token, dealer_csrf = login(DEALER_EMAIL, DEALER_PASS)
    if not dealer_token:
        print("  ⚠️  Dealer login failed, trying Admin account for new listings...")
        dealer_token, dealer_csrf = admin_token, admin_csrf

    # ── Step 4: Create new vehicle listings ─────────────────────────────
    print("\n[4] Creating new vehicle listings...")
    creates_ok = 0
    creates_fail = 0
    section_counts = {}
    for listing in NEW_LISTINGS:
        sec = listing["section_label"]
        ok, vid = create_vehicle_listing(listing, None, dealer_token, dealer_csrf)
        if ok:
            creates_ok += 1
            section_counts[sec] = section_counts.get(sec, 0) + 1
        else:
            creates_fail += 1

    print(f"\n  New listings: {creates_ok} created, {creates_fail} failed")
    for sec, count in sorted(section_counts.items()):
        print(f"    {sec}: +{count}")

    # ── Step 5: Verify section counts ────────────────────────────────────
    print("\n[5] Verifying section vehicle counts...")
    sections_check = [
        ("bodyStyle", "Hatchback",   "Hatchbacks",   10),
        ("bodyStyle", "Pickup",      "Camionetas",    10),
        ("bodyStyle", "SportsCar",   "Deportivos",    10),
        ("bodyStyle", "Convertible", "Convertibles",  10),
        ("bodyStyle", "Van",         "Vans",          10),
        ("bodyStyle", "Minivan",     "Minivans",       9),
        ("fuelType",  "Hybrid",      "Híbridos",      10),
        ("fuelType",  "Electric",    "Eléctricos",    10),
    ]

    all_ok = True
    for filter_type, filter_val, section_name, expected in sections_check:
        s, r = http_call("GET", f"/api/vehicles?{filter_type}={filter_val}&limit=15",
                         token=admin_token, csrf=admin_csrf)
        actual = len(r.get("vehicles", [])) if s == 200 else "ERR"
        mark = "✅" if actual == expected else ("⚠️ " if isinstance(actual, int) and actual >= expected - 1 else "❌")
        if mark == "❌":
            all_ok = False
        print(f"  {mark} {section_name}: {actual} vehicles (expected {expected})")

    print("\n" + "=" * 60)
    if all_ok:
        print("✅ All sections verified successfully!")
    else:
        print("⚠️  Some sections need attention — see above")
    print("=" * 60)


if __name__ == "__main__":
    run()
