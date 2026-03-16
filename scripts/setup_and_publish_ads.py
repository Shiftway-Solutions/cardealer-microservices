#!/usr/bin/env python3
"""
Add images + contact info to Draft vehicles, then publish and approve them.
"""
import json
import secrets
import urllib.request
import urllib.error

BASE = "http://localhost:18443"
ADMIN_EMAIL = "admin@okla.local"
ADMIN_PASS  = "Admin123!@#"

# Map vehicle ID → Unsplash image URL (matched by make/type)
VEHICLE_IMAGES = {
    "d96a9083-179f-4f3f-b6e8-5971ad0b5adb":  # Toyota Hilux Pickup
        "https://images.unsplash.com/photo-1551830820-330a71b99659?w=800&q=75",
    "ce3cf724-0984-470b-8a2f-ca715b68aa76":  # Ford Mustang SportsCar
        "https://images.unsplash.com/photo-1494976388531-d1058494cdd8?w=800&q=75",
    "fcbbdf9b-ab67-4607-9952-77af0aca4638":  # Chevrolet Camaro SportsCar
        "https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=800&q=75",
    "fa0e2c2c-0c34-4316-95fa-59e9e1319089":  # Mercedes C300 Cabriolet Convertible
        "https://images.unsplash.com/photo-1618843479313-40f8afb4b4d8?w=800&q=75",
    "255d2557-9d10-4e24-b899-a0a0d727416e":  # BMW 430i Convertible
        "https://images.unsplash.com/photo-1556189250-72ba954cfc2b?w=800&q=75",
    "ce60c847-6375-4acf-9548-2cfa6c0fec61":  # Mercedes Sprinter 2500 Van
        "https://images.unsplash.com/photo-1533473359331-0135ef1b58bf?w=800&q=75",
    "17b32483-3e11-4840-b606-f9005fdcf8fd":  # Ford Transit 350 Van
        "https://images.unsplash.com/photo-1551830820-330a71b99659?w=800&q=75",
    "46ee9b2c-69c5-46b9-a583-6fb411867fda":  # Chrysler Voyager Minivan
        "https://images.unsplash.com/photo-1559768713-bd0ed1fa4dbb?w=800&q=75",
    "c568a56d-9ce1-4046-89b8-32c53c490db5":  # Toyota Alphard Minivan
        "https://images.unsplash.com/photo-1619767886558-efdc259cde1a?w=800&q=75",
    "b52a0ec4-e6af-4f17-9411-4f79424cc39c":  # Toyota Corolla Cross Hybrid
        "https://images.unsplash.com/photo-1621007947382-bb3c3994e3fb?w=800&q=75",
    "0ef531fa-7e4f-49e0-a58b-8166b32ba6f1":  # BYD Atto 3 Electric
        "https://images.unsplash.com/photo-1560958089-b8a1929cea89?w=800&q=75",
}

SELLER_PHONE    = "809-555-1234"
SELLER_WHATSAPP = "18095551234"
SELLER_EMAIL    = "ventas@okla.com.do"


def http_call(method, path, data=None, token=None, csrf=None):
    url = BASE + path
    body = json.dumps(data).encode() if data is not None else b""
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
            return e.code, {"error": body_text[:300]}
    except Exception as ex:
        return 0, {"error": str(ex)}


def login(email, password):
    csrf = secrets.token_hex(32)
    status, resp = http_call("POST", "/api/auth/login", {"email": email, "password": password}, csrf=csrf)
    if status not in (200, 201):
        return None, None
    token = resp.get("accessToken") or resp.get("token") or resp.get("data", {}).get("accessToken")
    return token, csrf


def run():
    print("Setting up and publishing Draft vehicles...")
    token, csrf = login(ADMIN_EMAIL, ADMIN_PASS)
    if not token:
        print("❌ Admin login failed"); return

    print("✅ Admin logged in\n")

    for vid, img_url in VEHICLE_IMAGES.items():
        # GET current state
        s, v = http_call("GET", f"/api/vehicles/{vid}", token=token, csrf=csrf)
        if s != 200:
            print(f"❌ GET {vid[:8]} failed ({s})"); continue

        label = f"{v.get('year')} {v.get('make')} {v.get('model')}"
        status = v.get("status")

        if status == "Active":
            print(f"✅ {label} already Active"); continue

        print(f"→ {label} | {status}")

        # Step 1: Add image
        s1, r1 = http_call("POST", f"/api/vehicles/{vid}/images",
            {"images": [{"url": img_url, "isPrimary": True, "sortOrder": 0}]},
            token=token, csrf=csrf)
        if s1 in (200, 201):
            print(f"  ✅ Image added")
        else:
            print(f"  ⚠️  Image add failed ({s1}): {str(r1)[:120]}")

        # Step 2: Add contact info via full PUT
        # Build update payload from existing vehicle, adding contact fields
        exclude = {"id", "images", "priceHistory", "homepageSectionAssignments",
                   "createdAt", "updatedAt", "publishedAt", "soldAt",
                   "approvedAt", "approvedBy", "rejectedAt", "rejectedBy",
                   "submittedForReviewAt", "fraudScore", "hasBrokenImages",
                   "brokenImagesDetectedAt"}
        payload = {k: val for k, val in v.items() if k not in exclude}
        payload["sellerPhone"]    = SELLER_PHONE
        payload["sellerWhatsApp"] = SELLER_WHATSAPP
        payload["sellerEmail"]    = SELLER_EMAIL
        if not payload.get("sellerName"):
            payload["sellerName"] = "OKLA Automotriz"

        s2, r2 = http_call("PUT", f"/api/vehicles/{vid}", payload, token=token, csrf=csrf)
        if s2 in (200, 204):
            print(f"  ✅ Contact info updated")
        else:
            print(f"  ⚠️  Contact update failed ({s2}): {str(r2)[:120]}")

        # Step 3: Publish (submit for review → PendingReview)
        s3, r3 = http_call("POST", f"/api/vehicles/{vid}/publish", {}, token=token, csrf=csrf)
        if s3 in (200, 204):
            print(f"  ✅ Published (→ PendingReview)")
        else:
            msg3 = r3.get("message", str(r3)) if isinstance(r3, dict) else str(r3)
            print(f"  ⚠️  Publish failed ({s3}): {msg3[:200]}")
            # Check if it has specific validation errors
            errs = r3.get("errors") if isinstance(r3, dict) else []
            if errs:
                for e in errs[:3]:
                    print(f"     - {e}")

        # Step 4: Admin Approve
        s4, r4 = http_call("POST", f"/api/vehicles/{vid}/approve",
            {"notes": "Approved: OKLA section ads manager"},
            token=token, csrf=csrf)
        if s4 in (200, 204):
            print(f"  ✅ Approved → Active")
        else:
            # Check actual status
            _, v2 = http_call("GET", f"/api/vehicles/{vid}", token=token, csrf=csrf)
            final = v2.get("status") if isinstance(v2, dict) else "unknown"
            print(f"  ⚠️  Approve failed ({s4}): {str(r4)[:100]} | Current status={final}")

        print()

    # Final counts
    print("─" * 50)
    print("Final section counts:")
    checks = [
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
    for ftype, fval, name, expected in checks:
        s, r = http_call("GET", f"/api/vehicles?{ftype}={fval}&limit=15", token=token, csrf=csrf)
        cnt = len(r.get("vehicles", [])) if s == 200 else "ERR"
        mark = "✅" if cnt == expected else ("⚠️ " if isinstance(cnt, int) and cnt >= expected - 1 else "❌")
        if mark == "❌":
            all_ok = False
        print(f"  {mark} {name}: {cnt} vehicles (expected {expected})")

    print("\n" + ("✅ All sections ready!" if all_ok else "⚠️  See above for issues"))


if __name__ == "__main__":
    run()
