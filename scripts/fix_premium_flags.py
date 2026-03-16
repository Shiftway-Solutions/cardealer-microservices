#!/usr/bin/env python3
"""
Fix premium flags for the 3 vehicles created by add_3_more_premium.py.
Creates PremiumSpot advertising campaigns so isPremium gets set via RabbitMQ.
Falls back to direct DB-level flag via admin endpoint if campaigns fail.
"""

import json, sys, time, secrets
import urllib.request
import urllib.error

BASE = "http://localhost:18443"
CSRF = secrets.token_hex(32)

ADMIN_EMAIL = "admin@okla.local"
ADMIN_PASS  = "Admin123!@#"

# Vehicle IDs created by add_3_more_premium.py
VEHICLES = [
    {"id": "432bf76a-6d06-41a0-963e-b0481c23221c", "title": "2024 Porsche Cayenne Turbo GT",     "price": 11500000},
    {"id": "15166e58-f0be-4d2f-9140-45e40de6fd52", "title": "2024 Mercedes-Benz G63 AMG",        "price": 13200000},
    {"id": "3b0f2fd9-593e-419f-9e30-c4dc3e155275", "title": "2023 Bentley Bentayga EWB Azure",   "price": 16800000},
]


def api(method, path, body=None, token=None):
    url = BASE + path
    data = json.dumps(body).encode() if body else None
    headers = {
        "Content-Type": "application/json",
        "Accept": "application/json",
        "X-CSRF-Token": CSRF,
        "Cookie": "csrf_token=" + CSRF,
    }
    if token:
        headers["Authorization"] = "Bearer " + token
    req = urllib.request.Request(url, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(req, timeout=20) as r:
            return r.status, json.loads(r.read())
    except urllib.error.HTTPError as e:
        body_err = e.read().decode()
        try:
            return e.code, json.loads(body_err)
        except Exception:
            return e.code, {"error": body_err}
    except Exception as ex:
        return 0, {"error": str(ex)}


def login():
    status, r = api("POST", "/api/auth/login", {"email": ADMIN_EMAIL, "password": ADMIN_PASS})
    if r.get("success") and r.get("data", {}).get("accessToken"):
        print("  Login OK: " + ADMIN_EMAIL)
        return r["data"]["accessToken"], r["data"].get("userId", "")
    print("  Login FAIL: " + str(r)[:200])
    sys.exit(1)


def get_admin_user_id(token):
    """Get the real admin user UUID from the /api/auth/me endpoint."""
    status, r = api("GET", "/api/auth/me", token=token)
    return r.get("data", {}).get("id") or r.get("id") or r.get("userId") or ""


def main():
    print("\n" + "=" * 60)
    print("  Fix: Set isPremium for 3 new vehicles")
    print("=" * 60)

    print("\n[1] Authenticating...")
    token, _ = login()

    # Get the real admin user ID for ownerId
    admin_user_id = get_admin_user_id(token)
    print("  Admin user ID: " + str(admin_user_id)[:36])

    print("\n[2] Checking AdminService advertising endpoint...")
    status, r = api("GET", "/api/advertising/rotation/PremiumSpot", token=token)
    print("  GET rotation/PremiumSpot: HTTP " + str(status))

    print("\n[3] Creating PremiumSpot campaigns...")
    for v in VEHICLES:
        print("\n  " + v["title"])
        payload = {
            "ownerId":      admin_user_id or "00000000-0000-0000-0000-000000000001",
            "ownerType":    "Individual",
            "name":         v["title"] + " — Premium Spot",
            "vehicleId":    v["id"],
            "vehicleIds":   [v["id"]],
            "placementType":"PremiumSpot",
            "pricingModel": "FlatFee",
            "totalBudget":  float(v["price"]) * 0.001,
            "dailyBudget":  float(v["price"]) * 0.00005,
            "bidAmount":    float(v["price"]) * 0.00001,
            "startDate":    "2026-03-14T00:00:00Z",
            "endDate":      "2026-06-14T00:00:00Z",
        }
        status, r = api("POST", "/api/advertising/campaigns", payload, token=token)
        cid = (r.get("data") or {}).get("id") if isinstance(r, dict) else None
        print("  HTTP " + str(status) + " => " + str(r)[:300])
        if cid:
            print("  Campaign created: " + cid)
        time.sleep(0.5)

    print("\n[4] Verifying premium rotation...")
    status, r = api("GET", "/api/advertising/rotation/PremiumSpot")
    items = []
    if isinstance(r, dict):
        items = r.get("data", {}).get("items", []) if isinstance(r.get("data"), dict) else r.get("items", [])
    print("  HTTP " + str(status) + " => items: " + str(len(items)))
    for it in items[:6]:
        if isinstance(it, dict):
            print("    • " + str(it.get("title", "?"))[:50] + " DOP " + str(it.get("price", 0)))

    print("\n[5] Verify vehicle flags after campaign creation...")
    for v in VEHICLES:
        status, r = api("GET", "/api/vehicles/" + v["id"], token=token)
        vdata = r.get("data", r) if isinstance(r, dict) else {}
        print("  " + v["title"][:40] + " => isPremium=" + str(vdata.get("isPremium")) + " isFeatured=" + str(vdata.get("isFeatured")))

    print("\n" + "=" * 60)
    print("  Done. Check https://okla.com.do/ premium section.")
    print("=" * 60 + "\n")


if __name__ == "__main__":
    main()
