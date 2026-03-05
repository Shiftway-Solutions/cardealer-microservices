#!/usr/bin/env python3
"""Activate all campaigns and force rotation refresh."""
import json, urllib.request, ssl

BASE = "https://okla.com.do/api"
ctx = ssl.create_default_context()
CSRF = "okla-audit-csrf-token-2026"

def http(url, data=None, headers=None, method=None, timeout=20):
    hdrs = {"Content-Type": "application/json", "X-CSRF-Token": CSRF, "Cookie": "csrf_token=" + CSRF}
    if headers: hdrs.update(headers)
    body = json.dumps(data).encode() if data else None
    m = method or ("POST" if body else "GET")
    req = urllib.request.Request(url, data=body, headers=hdrs, method=m)
    try:
        with urllib.request.urlopen(req, timeout=timeout, context=ctx) as r:
            return r.status, json.loads(r.read() or b'{}')
    except urllib.error.HTTPError as e:
        try: return e.code, json.loads(e.read().decode())
        except: return e.code, {"error": str(e)}
    except Exception as e: return 0, {"error": str(e)}

def unwrap(d):
    if isinstance(d, dict) and "data" in d:
        return d["data"]
    return d

# Login
s, d = http(BASE + "/auth/login", {"email": "admin@okla.local", "password": "Admin123!@#"})
admin_tok = d["data"]["accessToken"]
s, d = http(BASE + "/auth/login", {"email": "nmateo@okla.com.do", "password": "Dealer2026!@#"})
dealer_tok = d["data"]["accessToken"]
dealer_id = d["data"]["userId"]
ah = lambda t: {"Authorization": "Bearer " + t, "Cookie": "csrf_token=" + CSRF + "; okla_access_token=" + t}

print("Admin and dealer authenticated.\n")

# List campaigns for dealer
print("=== Dealer Campaigns ===")
s, d = http(BASE + f"/advertising/campaigns?ownerId={dealer_id}", headers=ah(dealer_tok))
print(f"HTTP {s}")
data = unwrap(d)
items = data.get("items", data) if isinstance(data, dict) else (data if isinstance(data, list) else [])
if not isinstance(items, list):
    items = []
print(f"Total campaigns: {len(items)}")

# Try activating each
for c in items:
    cid = c.get("id", "")
    status = c.get("status", -1)
    pt = c.get("placementType", "?")
    print(f"\n  Campaign {cid[:12]}  status={status}  placement={pt}")

    if status == 0:
        # Try multiple activation endpoints
        endpoints = [
            ("POST", f"/advertising/campaigns/{cid}/activate"),
            ("POST", f"/advertising/campaigns/{cid}/approve"),
            ("PUT", f"/advertising/campaigns/{cid}", {"status": 1}),
            ("PATCH", f"/advertising/campaigns/{cid}", {"status": "Active"}),
            ("POST", f"/advertising/campaigns/{cid}/status", {"status": "Active"}),
        ]
        for method, endpoint, *payload in endpoints:
            body = payload[0] if payload else {}
            s2, d2 = http(BASE + endpoint, body if body else None, ah(admin_tok), method=method)
            print(f"    {method} {endpoint.split('/advertising')[-1]}: {s2} -> {json.dumps(d2)[:100]}")
            if s2 in (200, 204):
                print(f"    ✓ ACTIVATED")
                break
    else:
        print(f"    Already active/status={status}")

# Force rotation refresh
print("\n=== Force Rotation Refresh ===")
for endpoint in ["/advertising/rotation/refresh", "/advertising/rotation/force-refresh"]:
    s, d = http(BASE + endpoint, {}, ah(admin_tok))
    result = unwrap(d)
    items_count = len(result.get("items", [])) if isinstance(result, dict) else 0
    print(f"  {endpoint}: HTTP {s} -> {items_count} items")

# Check rotation results
print("\n=== Final Rotation Status ===")
for slot in ["FeaturedSpot", "PremiumSpot"]:
    s, d = http(BASE + f"/advertising/rotation/{slot}")
    r = unwrap(d)
    if isinstance(r, dict):
        items = r.get("items", [])
        print(f"  {slot}: {len(items)} ads")
    else:
        print(f"  {slot}: {json.dumps(r)[:100]}")

# Check what's on homepage now
print("\n=== Homepage Featured ===")
s, d = http(BASE + "/vehicles/featured?count=20")
fl = unwrap(d)
featured = fl if isinstance(fl, list) else []
print(f"  Featured: {len(featured)}")

print("\nDone.")
