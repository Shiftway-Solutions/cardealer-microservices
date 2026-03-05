#!/usr/bin/env python3
"""Fill ALL homepage advertising slots: FeaturedSpot (6), PremiumSpot (12), DealerShowcase (8)."""
import json, urllib.request, ssl, datetime

BASE = "https://okla.com.do/api"
ctx = ssl.create_default_context()
CSRF = "okla-audit-csrf-token-2026"

def http(url, data=None, headers=None, method=None, timeout=20):
    hdrs = {"Content-Type": "application/json", "X-CSRF-Token": CSRF, "Cookie": "csrf_token=" + CSRF}
    if headers:
        hdrs.update(headers)
    body = json.dumps(data).encode() if data else None
    m = method or ("POST" if body else "GET")
    req = urllib.request.Request(url, data=body, headers=hdrs, method=m)
    try:
        with urllib.request.urlopen(req, timeout=timeout, context=ctx) as r:
            raw = r.read()
            return r.status, json.loads(raw) if raw else {}
    except urllib.error.HTTPError as e:
        try:
            return e.code, json.loads(e.read().decode())
        except:
            return e.code, {"error": str(e)}
    except Exception as e:
        return 0, {"error": str(e)}

def login(email, pw):
    s, d = http(BASE + "/auth/login", {"email": email, "password": pw})
    if s == 200:
        return d["data"]["accessToken"], d["data"]["userId"]
    print(f"  ✗ Login {email}: {s}")
    return None, None

def ah(token):
    return {"Authorization": "Bearer " + token, "Cookie": "csrf_token=" + CSRF + "; okla_access_token=" + token}

def unwrap(d):
    if isinstance(d, dict) and "data" in d:
        return d["data"]
    return d

# ── Auth ──
print("=" * 60)
print("OKLA — FILL ALL ADVERTISING SLOTS")
print("=" * 60)

admin_tok, admin_id = login("admin@okla.local", "Admin123!@#")
dealer_tok, dealer_id = login("nmateo@okla.com.do", "Dealer2026!@#")
print(f"✓ Admin: {admin_id}")
print(f"✓ Dealer: {dealer_id}\n")

# ── 1. Get ALL active vehicles ──
print("── 1. Fetch All Active Vehicles ──")
s, d = http(BASE + "/vehicles?StatusFilter=Active&Page=1&PageSize=50", headers=ah(admin_tok))
result = unwrap(d)
vehicles = result.get("items", []) if isinstance(result, dict) else (result if isinstance(result, list) else [])
if not vehicles:
    s2, d2 = http(BASE + "/admin/vehicles?statusFilter=Active&page=1&pageSize=50", headers=ah(admin_tok))
    r2 = unwrap(d2)
    vehicles = r2.get("items", []) if isinstance(r2, dict) else (r2 if isinstance(r2, list) else [])
print(f"Active vehicles: {len(vehicles)}")
vids = [v["id"] for v in vehicles]

# ── 2. Feature all vehicles (mark isFeatured=true) ──
print("\n── 2. Feature Vehicles ──")
for v in vehicles[:15]:
    vid = v["id"]
    title = v.get("title", f"{v.get('year','')} {v.get('make','')} {v.get('model','')}")
    # Toggle feature
    s, d = http(BASE + f"/vehicles/{vid}/feature", {"isFeatured": True}, ah(admin_tok))
    if s == 200:
        if isinstance(d, dict) and d.get("isFeatured") == True:
            print(f"  ★ {title}")
        elif isinstance(d, dict) and d.get("isFeatured") == False:
            # Was featured, got toggled off. Toggle back on
            s2, d2 = http(BASE + f"/vehicles/{vid}/feature", {"isFeatured": True}, ah(admin_tok))
            if s2 == 200 and isinstance(d2, dict) and d2.get("isFeatured"):
                print(f"  ★ {title} (re-featured)")
            else:
                print(f"  ✗ {title}: toggle issue")
        else:
            print(f"  ✓ {title}: {s}")
    else:
        print(f"  ✗ {title}: {s}")

# ── 3. List existing campaigns ──
print("\n── 3. Existing Campaigns ──")
s, d = http(BASE + "/advertising/campaigns", headers=ah(admin_tok))
existing_campaigns = unwrap(d)
if isinstance(existing_campaigns, list):
    print(f"  Total campaigns: {len(existing_campaigns)}")
    for c in existing_campaigns[:10]:
        status = c.get("status", c.get("statusName", "?"))
        pt = c.get("placementType", c.get("placementTypeName", "?"))
        print(f"  [{status}] Placement:{pt} Vehicle:{c.get('vehicleId','?')[:8]}.. Budget:${c.get('totalBudget','?')}")
elif isinstance(existing_campaigns, dict) and "items" in existing_campaigns:
    items = existing_campaigns["items"]
    print(f"  Total campaigns: {len(items)}")
    for c in items[:10]:
        status = c.get("status", c.get("statusName", "?"))
        pt = c.get("placementType", c.get("placementTypeName", "?"))
        print(f"  [{status}] Placement:{pt} Vehicle:{c.get('vehicleId','?')[:8]}.. Budget:${c.get('totalBudget','?')}")
else:
    print(f"  Campaigns response: {s} - {json.dumps(d)[:200]}")

# ── 4. Create campaigns for EVERY placement type ──
print("\n── 4. Create Ad Campaigns ──")
now = datetime.datetime.now(datetime.UTC)
start = now.strftime("%Y-%m-%dT%H:%M:%SZ")
end = (now + datetime.timedelta(days=30)).strftime("%Y-%m-%dT%H:%M:%SZ")

placements = {
    0: "FeaturedSpot",
    1: "PremiumSpot",
}

created_campaigns = []
# FeaturedSpot: 6 vehicles, PremiumSpot: 12 vehicles
featured_count = 6
premium_count = 12

for i, vid in enumerate(vids[:featured_count]):
    title = next((f"{v.get('year','?')} {v.get('make','?')} {v.get('model','?')}" for v in vehicles if v.get("id") == vid), "?")
    campaign = {
        "ownerId": dealer_id,
        "ownerType": "Dealer",
        "vehicleId": vid,
        "placementType": 0,  # FeaturedSpot
        "pricingModel": 1,  # FixedDaily
        "totalBudget": 1000.0,
        "startDate": start,
        "endDate": end,
    }
    s, d = http(BASE + "/advertising/campaigns", campaign, ah(dealer_tok))
    if s in (200, 201):
        cid = unwrap(d)
        camp_id = cid.get("id", "?") if isinstance(cid, dict) else str(cid)
        print(f"  ✓ FeaturedSpot: {title} -> {camp_id[:12]}")
        created_campaigns.append(camp_id if isinstance(camp_id, str) else cid)
    else:
        print(f"  ✗ FeaturedSpot: {title} -> {s} {json.dumps(d)[:80]}")

for i, vid in enumerate(vids[:premium_count]):
    title = next((f"{v.get('year','?')} {v.get('make','?')} {v.get('model','?')}" for v in vehicles if v.get("id") == vid), "?")
    campaign = {
        "ownerId": dealer_id,
        "ownerType": "Dealer",
        "vehicleId": vid,
        "placementType": 1,  # PremiumSpot
        "pricingModel": 1,  # FixedDaily
        "totalBudget": 1000.0,
        "startDate": start,
        "endDate": end,
    }
    s, d = http(BASE + "/advertising/campaigns", campaign, ah(dealer_tok))
    if s in (200, 201):
        cid = unwrap(d)
        camp_id = cid.get("id", "?") if isinstance(cid, dict) else str(cid)
        print(f"  ✓ PremiumSpot: {title} -> {camp_id[:12]}")
        created_campaigns.append(camp_id if isinstance(camp_id, str) else cid)
    else:
        print(f"  ✗ PremiumSpot: {title} -> {s} {json.dumps(d)[:80]}")

# ── 5. Activate all campaigns ──
print("\n── 5. Activate Campaigns ──")
# Get all campaigns again
s, d = http(BASE + "/advertising/campaigns", headers=ah(admin_tok))
all_campaigns = unwrap(d)
campaign_list = all_campaigns if isinstance(all_campaigns, list) else (all_campaigns.get("items", []) if isinstance(all_campaigns, dict) else [])

for c in campaign_list:
    cid = c.get("id", "")
    status = c.get("status", -1)
    if status == 0 or status == "Draft" or status == "Pending":
        # Try to activate via PUT
        s, d = http(BASE + f"/advertising/campaigns/{cid}/activate", {}, ah(admin_tok))
        if s in (200, 204):
            print(f"  ✓ Activated: {cid[:12]}")
        else:
            # Try PATCH with status update
            s2, d2 = http(BASE + f"/advertising/campaigns/{cid}", {"status": 1}, ah(admin_tok), method="PUT")
            if s2 in (200, 204):
                print(f"  ✓ Activated (PUT): {cid[:12]}")
            else:
                # Try PATCH
                s3, d3 = http(BASE + f"/advertising/campaigns/{cid}/status", {"status": "Active"}, ah(admin_tok))
                if s3 in (200, 204):
                    print(f"  ✓ Activated (status): {cid[:12]}")
                else:
                    print(f"  ✗ Activate {cid[:12]}: {s}/{s2}/{s3}")

# ── 6. Force rotation refresh ──
print("\n── 6. Force Ad Rotation Refresh ──")
s, d = http(BASE + "/advertising/rotation/refresh", {}, ah(admin_tok))
print(f"  Refresh: HTTP {s} - {json.dumps(d)[:150]}")

# Also try POST force-refresh
s, d = http(BASE + "/advertising/rotation/force-refresh", {}, ah(admin_tok))
print(f"  Force-Refresh: HTTP {s} - {json.dumps(d)[:150]}")

# ── 7. Update Dealer Showcase (Brands) ──
print("\n── 7. Dealer Showcase / Brands ──")
# Get existing brands
s, d = http(BASE + "/advertising/homepage/brands?activeOnly=false", headers=ah(admin_tok))
brands = unwrap(d)
if isinstance(brands, list):
    print(f"  Existing brands: {len(brands)}")
    for b in brands:
        print(f"    [{b.get('brandKey','?')}] {b.get('displayName','?')} visible={b.get('isVisible')}")
else:
    print(f"  Brands response: {json.dumps(brands)[:200]}")

# Ensure brands are visible and have routes
dealer_brands = [
    {"brandKey": "toyota", "displayName": "Toyota", "logoInitials": "TO", "vehicleCount": 15, "isVisible": True, "route": "/buscar?marca=toyota", "displayOrder": 1},
    {"brandKey": "honda", "displayName": "Honda", "logoInitials": "HN", "vehicleCount": 8, "isVisible": True, "route": "/buscar?marca=honda", "displayOrder": 2},
    {"brandKey": "hyundai", "displayName": "Hyundai", "logoInitials": "HY", "vehicleCount": 5, "isVisible": True, "route": "/buscar?marca=hyundai", "displayOrder": 3},
    {"brandKey": "kia", "displayName": "Kia", "logoInitials": "KI", "vehicleCount": 4, "isVisible": True, "route": "/buscar?marca=kia", "displayOrder": 4},
    {"brandKey": "nissan", "displayName": "Nissan", "logoInitials": "NI", "vehicleCount": 3, "isVisible": True, "route": "/buscar?marca=nissan", "displayOrder": 5},
    {"brandKey": "chevrolet", "displayName": "Chevrolet", "logoInitials": "CH", "vehicleCount": 3, "isVisible": True, "route": "/buscar?marca=chevrolet", "displayOrder": 6},
    {"brandKey": "mitsubishi", "displayName": "Mitsubishi", "logoInitials": "MI", "vehicleCount": 2, "isVisible": True, "route": "/buscar?marca=mitsubishi", "displayOrder": 7},
    {"brandKey": "suzuki", "displayName": "Suzuki", "logoInitials": "SU", "vehicleCount": 2, "isVisible": True, "route": "/buscar?marca=suzuki", "displayOrder": 8},
]

# Update brands via PUT
s, d = http(BASE + "/advertising/homepage/brands", dealer_brands, ah(admin_tok), method="PUT")
if s in (200, 204):
    print(f"  ✓ Updated brands: {len(dealer_brands)}")
else:
    print(f"  ✗ Update brands: {s} - {json.dumps(d)[:150]}")
    # Try POST for each brand
    for brand in dealer_brands:
        s2, d2 = http(BASE + "/advertising/homepage/brands", brand, ah(admin_tok))
        if s2 in (200, 201):
            print(f"  ✓ Added brand: {brand['displayName']}")
        else:
            print(f"  ✗ Add brand {brand['displayName']}: {s2}")

# ── 8. Verify Rotation ──
print("\n── 8. Final Rotation Status ──")
for slot in ["FeaturedSpot", "PremiumSpot"]:
    s, d = http(BASE + f"/advertising/rotation/{slot}")
    r = unwrap(d)
    if isinstance(r, dict):
        items = r.get("items", [])
        print(f"  {slot}: {len(items)} ads (algorithm: {r.get('algorithmUsed', '?')})")
        for item in items[:5]:
            print(f"    - {item.get('vehicleTitle', item.get('title', '?'))}")
    else:
        print(f"  {slot}: {json.dumps(r)[:100]}")

# ── 9. Final featured count ──
print("\n── 9. Featured Vehicles ──")
s, d = http(BASE + "/vehicles/featured?count=20")
fl = unwrap(d)
featured = fl if isinstance(fl, list) else (fl.get("items", []) if isinstance(fl, dict) else [])
print(f"  Total featured: {len(featured)}")
for v in featured[:10]:
    print(f"  ★ {v.get('title','?')} (${v.get('price','?')})")

# ── 10. Summary ──
print("\n" + "=" * 60)
print("AD FILL SUMMARY")
print("=" * 60)
print(f"  Vehicles available: {len(vehicles)}")
print(f"  Featured vehicles: {len(featured)}")
print(f"  Campaigns created: {len(created_campaigns)}")
print(f"  Brands configured: {len(dealer_brands)}")
print("=" * 60)
