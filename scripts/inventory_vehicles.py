#!/usr/bin/env python3
"""
Inventory current vehicles by body type and check advertising state.
"""
import urllib.request, urllib.error, json, secrets, sys

BASE = "http://localhost:18443"

def post(path, data, headers=None):
    body = json.dumps(data).encode()
    req = urllib.request.Request(f"{BASE}{path}", data=body, method="POST")
    req.add_header("Content-Type", "application/json")
    if headers:
        for k, v in headers.items():
            req.add_header(k, v)
    try:
        with urllib.request.urlopen(req, timeout=30) as resp:
            return resp.status, json.loads(resp.read())
    except urllib.error.HTTPError as e:
        return e.code, json.loads(e.read())

def get(path, headers=None):
    req = urllib.request.Request(f"{BASE}{path}")
    if headers:
        for k, v in headers.items():
            req.add_header(k, v)
    try:
        with urllib.request.urlopen(req, timeout=30) as resp:
            return resp.status, json.loads(resp.read())
    except urllib.error.HTTPError as e:
        return e.code, {}

# Login
print("Logging in as admin...")
status, resp = post("/api/auth/login", {"email": "admin@okla.local", "password": "Admin123!@#"})
token = resp["data"]["accessToken"]
CSRF = secrets.token_hex(32)
auth = {
    "Authorization": f"Bearer {token}",
    "X-CSRF-Token": CSRF,
    "Cookie": f"csrf_token={CSRF}",
}
print(f"  Token: {token[:40]}...\n")

# Current rotation state
print("=== ADVERTISING ROTATION ===")
for section in ["FeaturedSpot", "PremiumSpot"]:
    _, r = get(f"/api/advertising/rotation/{section}")
    items = r.get("data", {}).get("items", [])
    print(f"\n{section} ({len(items)} items):")
    for v in items:
        print(f"  [{v.get('campaignId','')}] {v.get('title','')} | price={v.get('price','')} | vehicleId={v.get('vehicleId','')}")

# Vehicles by body type
print("\n\n=== VEHICLES BY BODY TYPE ===")
body_types = ["Hatchback", "Pickup", "SportsCar", "Convertible", "Van", "Minivan", "Hybrid", "Electric"]
for bt in body_types:
    _, r = get(f"/api/vehicles?bodyStyle={bt}&limit=10&sortBy=featured")
    vehicles = r.get("data", {}).get("items", r.get("items", r.get("data", [])))
    if isinstance(vehicles, dict):
        vehicles = vehicles.get("items", [])
    print(f"\n{bt} ({len(vehicles)} vehicles):")
    for v in vehicles[:6]:
        featured = v.get("isFeatured", False) or v.get("isPremium", False)
        img = (v.get("images") or [{}])[0].get("url", "NO_IMAGE")[:60]
        print(f"  id={v.get('id','')} [{'+AD' if featured else ''}] {v.get('year','')} {v.get('make','')} {v.get('model','')} | ${v.get('price','')} | img={img}")

# Also check for Tucson, CR-V, RAV4
print("\n\n=== SEARCH FOR SPECIFIC VEHICLES ===")
for query in ["Tucson", "CR-V", "RAV4"]:
    _, r = get(f"/api/vehicles?search={query}&limit=5")
    vehicles = r.get("data", {}).get("items", r.get("items", r.get("data", [])))
    if isinstance(vehicles, dict):
        vehicles = vehicles.get("items", [])
    print(f"\nSearch '{query}' ({len(vehicles)} results):")
    for v in vehicles:
        featured = v.get("isFeatured", False) or v.get("isPremium", False)
        print(f"  id={v.get('id','')} [{'+AD' if featured else ''}] {v.get('year','')} {v.get('make','')} {v.get('model','')} bodyStyle={v.get('bodyStyle',v.get('bodyType','?'))} | ${v.get('price','')}")

# Check what the Featured section fallback shows
print("\n\n=== FEATURED VEHICLES FALLBACK (isFeatured=true) ===")
_, r = get("/api/vehicles?isFeatured=true&limit=20")
vehicles = r.get("data", {}).get("items", r.get("items", []))
if isinstance(vehicles, dict):
    vehicles = vehicles.get("items", [])
print(f"isFeatured=true vehicles: {len(vehicles)}")
for v in vehicles:
    img = (v.get("images") or [{}])[0].get("url", "NO_IMAGE")[:60]
    print(f"  id={v.get('id','')} {v.get('year','')} {v.get('make','')} {v.get('model','')} bodyStyle={v.get('bodyStyle','?')} | ${v.get('price','')} | img:{img}")
