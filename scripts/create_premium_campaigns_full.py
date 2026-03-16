#!/usr/bin/env python3
"""
Create PremiumSpot campaigns with full vehicle metadata so the rotation
filter (title && imageUrl && price) passes in the FeaturedVehicles component.
"""

import json, sys, time, secrets
import urllib.request
import urllib.error

BASE = "http://localhost:18443"
CSRF = secrets.token_hex(32)

ADMIN_EMAIL = "admin@okla.local"
ADMIN_PASS  = "Admin123!@#"

VEHICLES = [
    {"id": "432bf76a-6d06-41a0-963e-b0481c23221c"},
    {"id": "15166e58-f0be-4d2f-9140-45e40de6fd52"},
    {"id": "3b0f2fd9-593e-419f-9e30-c4dc3e155275"},
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


def get_vehicle(vid, token):
    status, r = api("GET", "/api/vehicles/" + vid, token=token)
    if status == 200 and r.get("success"):
        return r.get("data", {})
    return {}


def main():
    print("\n" + "=" * 60)
    print("  Create PremiumSpot Campaigns WITH Full Vehicle Metadata")
    print("=" * 60)

    print("\n[1] Authenticating...")
    token, _ = login()

    # Get admin user ID
    s, me = api("GET", "/api/auth/me", token=token)
    admin_id = (me.get("data") or {}).get("id") or "9d16915c-e2be-47c9-9134-86b19304bd2c"
    print("  Admin ID: " + str(admin_id))

    print("\n[2] Fetching vehicle metadata...")
    enriched = []
    for v in VEHICLES:
        vdata = get_vehicle(v["id"], token)
        imgs = vdata.get("images") or []
        # Sort: isPrimary first, then by sortOrder
        imgs_sorted = sorted(imgs, key=lambda x: (not x.get("isPrimary", False), x.get("sortOrder", 99)))
        primary_img = next((i["url"] for i in imgs_sorted if i.get("url") and not i["url"].startswith("blob:")), None)

        title = vdata.get("title") or f"{vdata.get('year','')} {vdata.get('make','')} {vdata.get('model','')}".strip()
        slug  = vdata.get("slug") or ""
        price = vdata.get("price") or 0
        currency = vdata.get("currency") or "DOP"
        city  = vdata.get("city") or ""
        province = vdata.get("province") or vdata.get("state") or ""
        location = ", ".join(filter(None, [city, province])) or "R.D."

        enriched.append({
            "id": v["id"],
            "title": title,
            "slug": slug,
            "imageUrl": primary_img,
            "price": price,
            "currency": currency,
            "location": location,
        })
        print(f"  {title[:50]} | {price:,} {currency} | img={'OK' if primary_img else 'MISSING'}")

    print("\n[3] Creating PremiumSpot campaigns with metadata...")
    for v in enriched:
        print(f"\n  {v['title'][:50]}")
        payload = {
            "ownerId":       admin_id,
            "ownerType":     "Individual",
            "name":          v["title"][:80] + " — Premium",
            "vehicleId":     v["id"],
            "vehicleIds":    [v["id"]],
            "placementType": "PremiumSpot",
            "pricingModel":  "FlatFee",
            "totalBudget":   float(v["price"]) * 0.001,
            "bidAmount":     float(v["price"]) * 0.00001,
            "startDate":     "2026-03-14T00:00:00Z",
            "endDate":       "2026-06-14T00:00:00Z",
            # Vehicle metadata for rotation cache
            "vehicleTitle":    v["title"],
            "vehicleSlug":     v["slug"],
            "vehicleImageUrl": v["imageUrl"],
            "vehiclePrice":    float(v["price"]),
            "vehicleCurrency": v["currency"],
            "vehicleLocation": v["location"],
        }
        status, r = api("POST", "/api/advertising/campaigns", payload, token=token)
        cid = (r.get("data") or {}).get("id") if isinstance(r, dict) else None
        print(f"  HTTP {status} | campaign={cid or 'FAIL'}")
        if status not in (200, 201):
            print("  Error: " + str(r)[:300])
        time.sleep(0.3)

    print("\n[4] Verifying PremiumSpot rotation...")
    status, r = api("GET", "/api/advertising/rotation/PremiumSpot")
    items = []
    if isinstance(r, dict):
        d = r.get("data") or {}
        items = d.get("items", []) if isinstance(d, dict) else []
    print(f"  HTTP {status} | total items: {len(items)}")
    valid = [i for i in items if i.get("title") and i.get("imageUrl") and i.get("price")]
    print(f"  Items with complete metadata (title+image+price): {len(valid)}")
    for it in valid[:6]:
        print(f"    • {str(it.get('title','?'))[:50]} — {it.get('price')} {it.get('currency')}")

    if len(valid) >= 3:
        print("\n  ✅ SUCCESS — 3 premium vehicles ready in rotation!")
        print("  The FeaturedVehicles component filter (title && imageUrl && price) will pass.")
        print("  https://okla.com.do/ should now show them in 💎 Vehículos Premium")
    else:
        print(f"\n  ⚠️  Only {len(valid)} valid items (need 3). Check vehicle image URLs.")

    print("\n" + "=" * 60 + "\n")


if __name__ == "__main__":
    main()
