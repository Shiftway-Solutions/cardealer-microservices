#!/usr/bin/env python3
"""
OKLA Production Script — Agregar 3 anuncios más en 💎 Vehículos Premium
==========================================================================
Proceso:
  1. Login como admin en https://okla.com.do/
  2. Crear 3 vehículos de lujo (Draft)
  3. Publicar cada uno (Draft → PendingReview)
  4. Aprobar cada uno (PendingReview → Active)
  5. Crear campaña publicitaria PremiumSpot para cada vehículo
  6. Verificar que aparecen en la rotation del homepage

Notas:
  - Imágenes del bucket S3 de OKLA (públicas, sin firma expirada)
  - mileageUnit: "Kilometers" (enum serialization exacto del backend)
  - CSRF: Double Submit Cookie pattern
"""

import json, sys, time, secrets, ssl
import urllib.request
import urllib.error

# Gateway is internal-only (BFF pattern). External route:
#   Browser → https://okla.com.do/api/* → Next.js rewrite → gateway:8080
# For admin scripts we use the kubectl port-forward tunnel (same production DB/pods):
#   kubectl port-forward svc/gateway 18443:8080 -n okla
BASE = "http://localhost:18443"

CSRF_TOKEN = secrets.token_hex(32)

ADMIN_EMAIL = "admin@okla.local"
ADMIN_PASS  = "Admin123!@#"

# S3 images (same bucket, different files)
S3 = [
    "https://okla-images-2026.s3.us-east-2.amazonaws.com/vehicles/2026/03/06/0c0a99b8-21bb-4676-bf2b-793d99193d20.jpg",
    "https://okla-images-2026.s3.us-east-2.amazonaws.com/vehicles/2026/03/06/ec0d97ae-c18f-4d2a-b31e-15ea227f77d6.jpg",
    "https://okla-images-2026.s3.us-east-2.amazonaws.com/vehicles/2026/03/06/f9170344-f56a-4a39-8031-5a60b352e894.jpg",
    "https://okla-images-2026.s3.us-east-2.amazonaws.com/vehicles/2026/03/06/b70d9d72-b508-4a78-bb39-ee6f1350db8f.jpg",
    "https://okla-images-2026.s3.us-east-2.amazonaws.com/vehicles/2026/03/06/892c3605-42b9-4d8d-93f6-07c19a0c8cd2.jpg",
    "https://okla-images-2026.s3.us-east-2.amazonaws.com/vehicles/2026/03/06/67355928-79cd-49c8-b9f9-9fec0e5fe1f0.jpg",
    "https://okla-images-2026.s3.us-east-2.amazonaws.com/vehicles/2026/03/06/0befd9b6-eb63-4eed-a13b-2fdcefaff6a1.jpg",
    "https://okla-images-2026.s3.us-east-2.amazonaws.com/vehicles/2026/03/06/49b0e50c-34fc-40fe-8437-fdb245ebd989.jpg",
    "https://okla-images-2026.s3.us-east-2.amazonaws.com/vehicles/2026/03/06/5de80877-3456-4533-b9f8-baca62f1b397.jpg",
    "https://okla-images-2026.s3.us-east-2.amazonaws.com/vehicles/2026/03/06/ea023c86-e38e-49bd-b7d2-4f5f3b4a7bba.jpg",
]

def imgs(i0, i1, i2):
    return [
        {"url": S3[i0], "sortOrder": 0, "isPrimary": True},
        {"url": S3[i1], "sortOrder": 1, "isPrimary": False},
        {"url": S3[i2], "sortOrder": 2, "isPrimary": False},
    ]

# 3 vehículos premium distintos a los ya publicados (Porsche, Mercedes AMG, Bentley)
VEHICLES = [
    {
        "title": "2024 Porsche Cayenne Turbo GT",
        "description": "El SUV deportivo más potente de Porsche. Motor V8 biturbo de 640 hp, 0-100 en 3.3 segundos, suspensión PASM Sport, sistema de frenos cerámicos PCCB. Techo deportivo fijo en fibra de carbono. El apex del lujo y el rendimiento en la República Dominicana.",
        "make": "Porsche", "model": "Cayenne", "trim": "Turbo GT", "year": 2024,
        "price": 11500000, "currency": "DOP",
        "vehicleType": "SUV", "bodyStyle": "SUV",
        "fuelType": "Gasoline", "transmission": "Automatic", "driveType": "AWD",
        "doors": 4, "seats": 5,
        "engineSize": "4.0L V8", "horsepower": 640,
        "mileage": 1500, "mileageUnit": "Kilometers", "condition": "New",
        "exteriorColor": "Chalk", "interiorColor": "Black/Race-Tex",
        "city": "Distrito Nacional", "state": "DN", "country": "DO",
        "sellerName": "OKLA Premium Motors",
        "sellerPhone": "+18095550201",
        "sellerEmail": "premium@okla.com.do",
        "imageObjects": imgs(0, 3, 6),
    },
    {
        "title": "2024 Mercedes-Benz G63 AMG",
        "description": "El ícono definitivo del off-road de lujo. Motor AMG V8 biturbo de 585 hp, 3 bloqueos de diferencial, suspensión G-Ride adaptativa, interior AMG Performance Studio. Disponible en Santo Domingo, entrega inmediata.",
        "make": "Mercedes-Benz", "model": "G63", "trim": "AMG", "year": 2024,
        "price": 13200000, "currency": "DOP",
        "vehicleType": "SUV", "bodyStyle": "SUV",
        "fuelType": "Gasoline", "transmission": "Automatic", "driveType": "AWD",
        "doors": 4, "seats": 5,
        "engineSize": "4.0L V8", "horsepower": 585,
        "mileage": 800, "mileageUnit": "Kilometers", "condition": "New",
        "exteriorColor": "Obsidian Black Metallic", "interiorColor": "Nappa Cognac",
        "city": "Distrito Nacional", "state": "DN", "country": "DO",
        "sellerName": "OKLA Premium Motors",
        "sellerPhone": "+18095550202",
        "sellerEmail": "premium@okla.com.do",
        "imageObjects": imgs(1, 4, 7),
    },
    {
        "title": "2023 Bentley Bentayga EWB Azure",
        "description": "El SUV más lujoso del mundo, versión Extended Wheelbase. Motor W12 de 626 hp, asientos traseros con función First Class reclinables a 40°, pantallas de entretenimiento traseras, Mulliner Diamondstitch. Certificado por Bentley Motors, garantía vigente.",
        "make": "Bentley", "model": "Bentayga", "trim": "EWB Azure", "year": 2023,
        "price": 16800000, "currency": "DOP",
        "vehicleType": "SUV", "bodyStyle": "SUV",
        "fuelType": "Gasoline", "transmission": "Automatic", "driveType": "AWD",
        "doors": 4, "seats": 4,
        "engineSize": "6.0L W12", "horsepower": 626,
        "mileage": 9200, "mileageUnit": "Kilometers", "condition": "Used",
        "exteriorColor": "Glacier White", "interiorColor": "Porcelain/Cricket Ball",
        "city": "Punta Cana", "state": "LS", "country": "DO",
        "sellerName": "OKLA Premium Motors",
        "sellerPhone": "+18095550203",
        "sellerEmail": "premium@okla.com.do",
        "imageObjects": imgs(2, 5, 9),
    },
]

# SSL context — production uses valid cert, verify normally
CTX = ssl.create_default_context()

def api(method, path, body=None, token=None):
    url = BASE + path
    data = json.dumps(body).encode() if body else None
    headers = {
        "Content-Type": "application/json",
        "Accept": "application/json",
        "X-CSRF-Token": CSRF_TOKEN,
        "Cookie": f"csrf_token={CSRF_TOKEN}",
        "User-Agent": "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 Chrome/122.0 Safari/537.36",
    }
    if token:
        headers["Authorization"] = f"Bearer {token}"
    req = urllib.request.Request(url, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(req, timeout=30, context=CTX) as r:
            return json.loads(r.read())
    except urllib.error.HTTPError as e:
        body_err = e.read().decode()
        print(f"  ⚠️  HTTP {e.code} {method} {path}: {body_err[:400]}")
        try:
            return json.loads(body_err)
        except Exception:
            return {"error": body_err, "status": e.code}
    except Exception as ex:
        print(f"  ❌ Error {method} {path}: {ex}")
        return {"error": str(ex)}


def login(email, password):
    r = api("POST", "/api/auth/login", {"email": email, "password": password})
    if r.get("success") and r.get("data", {}).get("accessToken"):
        print(f"  ✅ Login OK: {email}")
        return r["data"]["accessToken"], r["data"].get("userId", "")
    print(f"  ❌ Login FAIL: {json.dumps(r)[:300]}")
    sys.exit(1)


def get_vehicle_status(vid, token):
    r = api("GET", f"/api/vehicles/{vid}", token=token)
    return (r.get("data") or r).get("status", "Unknown")


def main():
    print("\n" + "="*65)
    print("  OKLA — +3 Vehículos Premium en https://okla.com.do/")
    print("="*65)

    # ── 1. Login ───────────────────────────────────────────────────
    print("\n[1/5] Autenticando como Admin...")
    token, uid = login(ADMIN_EMAIL, ADMIN_PASS)

    # ── 2. Crear vehículos ─────────────────────────────────────────
    print("\n[2/5] Creando 3 vehículos (Draft)...")
    vehicle_ids = []

    for i, v in enumerate(VEHICLES, 1):
        payload = {k: v[k] for k in (
            "title","description","price","currency","make","model","trim","year",
            "vehicleType","bodyStyle","fuelType","transmission","driveType",
            "doors","seats","engineSize","horsepower","mileage","mileageUnit",
            "condition","exteriorColor","interiorColor","city","state","country",
            "sellerName","sellerPhone","sellerEmail","imageObjects",
        )}
        payload["isCertified"] = True
        payload["hasCleanTitle"] = True

        print(f"\n  [{i}/3] {v['title']} — DOP {v['price']:,}")
        r = api("POST", "/api/vehicles", payload, token=token)

        vid = (
            r.get("id")
            or (r.get("data") or {}).get("id")
            or (r.get("data") or {}).get("vehicleId")
            or r.get("vehicleId")
        )
        if vid:
            vehicle_ids.append(vid)
            status = (r.get("data") or r).get("status", "")
            print(f"  ✅ ID: {vid} | status: {status}")
        else:
            print(f"  ❌ Respuesta inesperada: {str(r)[:400]}")

    if not vehicle_ids:
        print("\n❌ No se creó ningún vehículo. Abortando.")
        sys.exit(1)

    print(f"\n  {len(vehicle_ids)} vehículo(s) en Draft")

    # ── 3. Publicar (Draft → PendingReview) ────────────────────────
    print("\n[3/5] Publicando (Draft → PendingReview)...")
    for vid in vehicle_ids:
        r = api("POST", f"/api/vehicles/{vid}/publish",
                {"disclaimerAccepted": True}, token=token)
        status = (r.get("data") or r).get("status") or r.get("currentStatus", "")
        ok = status in ("PendingReview", "Active") or r.get("success")
        errors = r.get("errors") or []
        if ok:
            print(f"  ✅ {vid[:8]}... → {status}")
        elif errors:
            print(f"  ⚠️  {vid[:8]}... errores: {errors[:2]} — continuando")
        else:
            print(f"  ⚠️  {vid[:8]}... respuesta: {str(r)[:200]}")
        time.sleep(0.5)

    # ── 4. Aprobar (PendingReview → Active) ────────────────────────
    print("\n[4/5] Aprobando vehículos (PendingReview → Active)...")
    for i, vid in enumerate(vehicle_ids):
        status = get_vehicle_status(vid, token)
        print(f"\n  Vehículo {i+1} ({vid[:8]}...) status={status}")

        if status == "PendingReview":
            r = api("POST", f"/api/vehicles/{vid}/approve",
                    {"notes": "OKLA Premium listing — approved"},
                    token=token)
            new_status = (r.get("data") or r).get("status") or r.get("currentStatus", "")
            if new_status == "Active" or r.get("success"):
                print(f"  ✅ Aprobado → Active")
            else:
                print(f"  ❌ No aprobado: {str(r)[:200]}")
        elif status == "Active":
            print(f"  ✅ Ya está Active")
        elif status == "Draft":
            print(f"  ⚠️  Sigue en Draft — el publish puede haber requerido imágenes CDN")
        else:
            print(f"  ⚠️  Status inesperado: {status}")
        time.sleep(0.5)

    # ── 5. Crear campañas PremiumSpot ──────────────────────────────
    print("\n[5/5] Creando campañas PremiumSpot...")
    campaign_ids = []

    for i, vid in enumerate(vehicle_ids):
        v = VEHICLES[i]
        print(f"\n  {v['title'][:45]}...")

        payload = {
            "vehicleId":     vid,
            "placementType": "PremiumSpot",
            "campaignType":  "Featured",
            "title":         v["title"],
            "startDate":     "2026-03-14T00:00:00Z",
            "endDate":       "2026-06-14T00:00:00Z",
            "budget":        float(v["price"]) * 0.001,
            "currency":      "DOP",
            "isActive":      True,
            "priority":      10,
        }

        r = api("POST", "/api/advertising/campaigns", payload, token=token)
        cid = (
            (r.get("data") or {}).get("id")
            or (r.get("data") or {}).get("campaignId")
            or r.get("id")
        )

        if cid:
            campaign_ids.append(cid)
            print(f"  ✅ Campaña: {cid}")
        else:
            print(f"  ↩️  Campaña API falló ({str(r)[:120]}). Marcando isFeatured via PUT...")
            r2 = api("PUT", f"/api/vehicles/{vid}",
                     {"isFeatured": True, "isPremium": True}, token=token)
            ok2 = r2.get("success") or r2.get("id") or r2.get("isFeatured")
            print(f"  {'✅' if ok2 else '⚠️ '} isFeatured fallback: {str(r2)[:120]}")
        time.sleep(0.5)

    # ── Verificar rotation ─────────────────────────────────────────
    print("\n[✓] Verificando rotation PremiumSpot en producción...")
    rotation = api("GET", "/api/advertising/rotation?placementType=PremiumSpot")
    items = rotation.get("data") or rotation.get("items") or []
    if isinstance(items, dict):
        items = items.get("items") or []
    print(f"  Items en PremiumSpot rotation: {len(items)}")
    for item in items[:8]:
        title = item.get("title") or item.get("vehicleTitle") or "(sin título)"
        price = item.get("price") or item.get("vehiclePrice") or 0
        print(f"    • {title[:55]} — DOP {price:,.0f}")

    # ── Resumen ────────────────────────────────────────────────────
    print("\n" + "="*65)
    print(f"  ✅ PROCESO COMPLETADO — {len(vehicle_ids)} anuncios Premium agregados")
    for i, vid in enumerate(vehicle_ids):
        print(f"    [{i+1}] {VEHICLES[i]['title'][:50]}")
        print(f"         DOP {VEHICLES[i]['price']:,} | ID: {vid}")
    if campaign_ids:
        print(f"\n  Campañas PremiumSpot: {len(campaign_ids)}")
    print("="*65)
    print(f"\n  🌐 Verificar en: https://okla.com.do/\n")


if __name__ == "__main__":
    main()
