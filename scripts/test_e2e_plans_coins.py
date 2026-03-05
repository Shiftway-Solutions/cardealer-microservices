#!/usr/bin/env python3
"""
OKLA Production E2E Test — Plans, Ad Catalog, OKLA Coins, Homepage Fill
Tests all 3 actors (Admin, Buyer, Dealer) against production at okla.com.do
"""
import requests
import json
import sys
import time

BASE = "https://okla.com.do/api"

# ═══════════════════════════════════════════════════════════════
# Credentials
# ═══════════════════════════════════════════════════════════════
ADMIN = {"email": "admin@okla.local", "password": "Admin123!@#"}
BUYER = {"email": "buyer002@okla-test.com", "password": "BuyerTest2026!"}
DEALER = {"email": "nmateo@okla.com.do", "password": "Dealer2026!@#"}

results = {"pass": 0, "fail": 0, "tests": []}

def log(status, test, detail=""):
    icon = "✅" if status == "PASS" else "❌"
    results[status.lower()] += 1
    results["tests"].append({"status": status, "test": test, "detail": detail})
    print(f"  {icon} {test}" + (f" — {detail}" if detail else ""))

def login(creds, role="user"):
    """Login and return session with token"""
    s = requests.Session()
    s.headers.update({"Content-Type": "application/json", "Accept": "application/json"})
    r = s.post(f"{BASE}/auth/login", json=creds, timeout=15)
    if r.status_code == 200:
        data = r.json()
        token = data.get("data", {}).get("accessToken") or data.get("accessToken") or data.get("token")
        if token:
            s.headers["Authorization"] = f"Bearer {token}"
            return s, data
    return None, None

# ═══════════════════════════════════════════════════════════════
# 1. AUTHENTICATION TESTS
# ═══════════════════════════════════════════════════════════════
print("\n" + "="*70)
print("📋 PHASE 1: AUTHENTICATION")
print("="*70)

admin_session, admin_data = login(ADMIN, "admin")
if admin_session:
    log("PASS", "Admin login", f"Token received")
else:
    log("FAIL", "Admin login", "Could not authenticate")

buyer_session, buyer_data = login(BUYER, "buyer")
if buyer_session:
    log("PASS", "Buyer login", f"Token received")
else:
    log("FAIL", "Buyer login", "Could not authenticate")

dealer_session, dealer_data = login(DEALER, "dealer")
if dealer_session:
    log("PASS", "Dealer login", f"Token received")
else:
    log("FAIL", "Dealer login", "Could not authenticate")

# ═══════════════════════════════════════════════════════════════
# 2. DEALER PLANS TESTS
# ═══════════════════════════════════════════════════════════════
print("\n" + "="*70)
print("📋 PHASE 2: DEALER PLANS (4-tier: Libre/Visible/Pro/Elite)")
print("="*70)

# Test public plans endpoint
r = requests.get(f"{BASE}/dealer-billing/plans", timeout=15)
if r.status_code == 200:
    plans = r.json()
    if isinstance(plans, list) and len(plans) >= 4:
        log("PASS", "GET /dealer-billing/plans", f"Got {len(plans)} plans")
        
        # Verify plan names
        plan_names = [p.get("name", "").lower() for p in plans]
        for expected in ["libre", "visible", "pro", "elite"]:
            if any(expected in n for n in plan_names):
                log("PASS", f"Plan '{expected}' exists", f"Found in response")
            else:
                log("FAIL", f"Plan '{expected}' exists", f"Not found. Got: {plan_names}")
        
        # Verify pricing
        for p in plans:
            pid = p.get("id", "")
            prices = p.get("prices", {})
            monthly = prices.get("monthly", -1)
            
            expected_prices = {"free": 0, "basic": 29, "professional": 89, "enterprise": 199}
            if pid in expected_prices:
                if monthly == expected_prices[pid]:
                    log("PASS", f"Plan {pid} price", f"${monthly}/mo ✓")
                else:
                    log("FAIL", f"Plan {pid} price", f"Expected ${expected_prices[pid]}, got ${monthly}")
    else:
        log("FAIL", "GET /dealer-billing/plans", f"Expected 4+ plans, got: {r.text[:200]}")
else:
    log("FAIL", "GET /dealer-billing/plans", f"HTTP {r.status_code}: {r.text[:200]}")

# Test dealer subscription status
if dealer_session:
    r = dealer_session.get(f"{BASE}/dealer-billing/subscription",
                           headers={"X-Dealer-Id": "f3aaadc5-d6ab-4992-9e48-e74454fb6ca2"})
    if r.status_code == 200:
        sub = r.json()
        plan = sub.get("plan", "unknown")
        log("PASS", "Dealer subscription status", f"Plan: {plan}")
    else:
        log("FAIL", "Dealer subscription status", f"HTTP {r.status_code}")

# ═══════════════════════════════════════════════════════════════
# 3. ADVERTISING CATALOG TESTS
# ═══════════════════════════════════════════════════════════════
print("\n" + "="*70)
print("📋 PHASE 3: ADVERTISING CATALOG (7 products)")
print("="*70)

r = requests.get(f"{BASE}/advertising/catalog", timeout=15)
if r.status_code == 200:
    catalog = r.json()
    products = catalog.get("products", catalog if isinstance(catalog, list) else [])
    log("PASS", "GET /advertising/catalog", f"Got {len(products)} products")
    
    expected_slugs = [
        "listing-destacado", "top-3-busquedas", "oferta-del-dia",
        "banner-homepage", "dealer-showcase", "pack-alertas-email",
        "paquete-visibilidad-total"
    ]
    
    for slug in expected_slugs:
        found = any(p.get("slug") == slug for p in products)
        if found:
            product = next(p for p in products if p.get("slug") == slug)
            price_info = []
            if product.get("pricePerDay"): price_info.append(f"${product['pricePerDay']}/día")
            if product.get("pricePerWeek"): price_info.append(f"${product['pricePerWeek']}/sem")
            if product.get("pricePerMonth"): price_info.append(f"${product['pricePerMonth']}/mes")
            log("PASS", f"Product: {slug}", ", ".join(price_info))
        else:
            log("FAIL", f"Product: {slug}", "Not found in catalog")
    
    # Test price estimate
    r2 = requests.get(f"{BASE}/advertising/catalog/listing-destacado/estimate?duration=month&quantity=5", timeout=15)
    if r2.status_code == 200:
        est = r2.json()
        total = est.get("totalPriceUsd", 0)
        log("PASS", "Price estimate (5x listing-destacado/month)", f"Total: ${total}")
    else:
        log("FAIL", "Price estimate", f"HTTP {r2.status_code}")
elif r.status_code == 404:
    log("FAIL", "GET /advertising/catalog", "404 — Route not yet deployed to gateway")
else:
    log("FAIL", "GET /advertising/catalog", f"HTTP {r.status_code}: {r.text[:200]}")

# ═══════════════════════════════════════════════════════════════
# 4. OKLA COINS TESTS
# ═══════════════════════════════════════════════════════════════
print("\n" + "="*70)
print("📋 PHASE 4: OKLA COINS (4 packages)")
print("="*70)

r = requests.get(f"{BASE}/okla-coins/packages", timeout=15)
if r.status_code == 200:
    pkg_data = r.json()
    packages = pkg_data.get("packages", pkg_data if isinstance(pkg_data, list) else [])
    log("PASS", "GET /okla-coins/packages", f"Got {len(packages)} packages")
    
    expected_pkgs = {
        "pack-basico": {"credits": 2500, "price": 25.00, "bonus": 0},
        "pack-intermedio": {"credits": 5500, "price": 50.00, "bonus": 10},
        "pack-profesional": {"credits": 12000, "price": 100.00, "bonus": 20},
        "pack-dealer": {"credits": 32500, "price": 250.00, "bonus": 30}
    }
    
    for slug, expected in expected_pkgs.items():
        found = next((p for p in packages if p.get("slug") == slug), None)
        if found:
            total = found.get("totalCredits", 0)
            price = found.get("priceUsd", 0)
            bonus = found.get("bonusPercentage", 0)
            if total == expected["credits"] and float(price) == expected["price"]:
                log("PASS", f"Package: {slug}", f"{total:,} coins, ${price}, +{bonus}% bonus")
            else:
                log("FAIL", f"Package: {slug}", f"Expected {expected}, got credits={total}, price={price}")
        else:
            log("FAIL", f"Package: {slug}", "Not found")
elif r.status_code == 404:
    log("FAIL", "GET /okla-coins/packages", "404 — Route not yet deployed to gateway")
else:
    log("FAIL", "GET /okla-coins/packages", f"HTTP {r.status_code}: {r.text[:200]}")

# Test wallet (requires auth)
if dealer_session:
    r = dealer_session.get(f"{BASE}/okla-coins/wallet",
                           headers={"X-Dealer-Id": "f3aaadc5-d6ab-4992-9e48-e74454fb6ca2"})
    if r.status_code == 200:
        wallet = r.json()
        balance = wallet.get("balance", 0)
        log("PASS", "Dealer wallet balance", f"{balance:,} OKLA Coins")
    elif r.status_code == 404:
        log("FAIL", "Dealer wallet", "404 — Route not deployed")
    else:
        log("FAIL", "Dealer wallet", f"HTTP {r.status_code}")

# ═══════════════════════════════════════════════════════════════
# 5. HOMEPAGE SECTIONS — FILL & VERIFY
# ═══════════════════════════════════════════════════════════════
print("\n" + "="*70)
print("📋 PHASE 5: HOMEPAGE SECTIONS & ADVERTISING")
print("="*70)

# Get all existing sections
r = requests.get(f"{BASE}/homepagesections", timeout=15)
if r.status_code == 200:
    sections = r.json()
    if isinstance(sections, dict) and "data" in sections:
        sections = sections["data"]
    section_list = sections if isinstance(sections, list) else []
    log("PASS", "GET /homepagesections", f"Got {len(section_list)} sections")
    
    for sec in section_list:
        name = sec.get("title") or sec.get("name") or sec.get("slug", "?")
        count = len(sec.get("vehicles", sec.get("items", [])))
        status = "PASS" if count > 0 else "FAIL"
        log(status, f"Section: {name}", f"{count} vehicles")
else:
    log("FAIL", "GET /homepagesections", f"HTTP {r.status_code}")

# Get all vehicles to use for filling
if admin_session:
    r = admin_session.get(f"{BASE}/admin/vehicles?pageSize=50", timeout=15)
    if r.status_code == 200:
        vdata = r.json()
        vehicles = vdata.get("data", {}).get("items", vdata.get("items", []))
        if isinstance(vdata, list):
            vehicles = vdata
        vehicle_ids = [v.get("id") for v in vehicles if v.get("id")]
        log("PASS", "Admin vehicle list", f"Got {len(vehicle_ids)} vehicles to work with")
        
        # Sections we need to create/fill
        needed_sections = [
            {"slug": "crossovers", "name": "Crossovers", "icon": "🚙"},
            {"slug": "hatchbacks", "name": "Hatchbacks", "icon": "🚗"},
            {"slug": "coupes", "name": "Coupés", "icon": "🏎️"},
            {"slug": "convertibles", "name": "Convertibles", "icon": "🏎️"},
            {"slug": "vans", "name": "Vans", "icon": "🚐"},
            {"slug": "minivans", "name": "Minivans", "icon": "🚐"},
            {"slug": "hibridos", "name": "Híbridos", "icon": "🔋"},
            {"slug": "electricos", "name": "Eléctricos", "icon": "⚡"},
            {"slug": "oferta-del-dia", "name": "🔥 Oferta del Día", "icon": "🔥"},
            {"slug": "premium", "name": "💎 Vehículos Premium", "icon": "💎"},
        ]
        
        # Try to create missing sections
        existing_slugs = [s.get("slug", "") for s in section_list] if 'section_list' in dir() else []
        
        for idx, sec in enumerate(needed_sections):
            if sec["slug"] not in existing_slugs:
                create_body = {
                    "name": sec["name"],
                    "slug": sec["slug"],
                    "description": f"Los mejores {sec['name']} en República Dominicana",
                    "displayOrder": 20 + idx,
                    "maxItems": 10,
                    "isActive": True,
                    "accentColor": "blue",
                    "icon": sec["icon"]
                }
                r = admin_session.post(f"{BASE}/homepagesections", json=create_body, timeout=15)
                if r.status_code in [200, 201]:
                    log("PASS", f"Created section: {sec['slug']}")
                else:
                    log("FAIL", f"Create section: {sec['slug']}", f"HTTP {r.status_code}: {r.text[:150]}")
        
        # Re-fetch sections after creation
        r = requests.get(f"{BASE}/homepagesections", timeout=15)
        if r.status_code == 200:
            sections = r.json()
            if isinstance(sections, dict) and "data" in sections:
                sections = sections["data"]
            section_list = sections if isinstance(sections, list) else []
        
        # Fill ALL sections with vehicles (cycle through available vehicle IDs)
        for sec in section_list:
            slug = sec.get("slug", "")
            existing_vehicles = sec.get("vehicles", sec.get("items", []))
            existing_count = len(existing_vehicles)
            
            if existing_count < 3 and vehicle_ids:
                # Pick vehicles to add (cycle through available ones)
                to_add = []
                for i in range(min(5, len(vehicle_ids))):
                    vid = vehicle_ids[(hash(slug) + i) % len(vehicle_ids)]
                    if vid not in [v.get("id") or v for v in existing_vehicles]:
                        to_add.append(vid)
                
                if to_add:
                    r = admin_session.post(
                        f"{BASE}/homepagesections/{slug}/vehicles/bulk",
                        json={"vehicleIds": to_add[:5]},
                        timeout=15
                    )
                    if r.status_code in [200, 201]:
                        log("PASS", f"Filled section: {slug}", f"Added {len(to_add[:5])} vehicles")
                    else:
                        log("FAIL", f"Fill section: {slug}", f"HTTP {r.status_code}: {r.text[:150]}")
                else:
                    log("PASS", f"Section {slug} already filled", f"{existing_count} vehicles")
            else:
                log("PASS", f"Section {slug} has content", f"{existing_count} vehicles")
        
        # Feature vehicles for "Destacados" section
        featured_count = 0
        for vid in vehicle_ids[:15]:
            r = admin_session.post(f"{BASE}/vehicles/{vid}/feature",
                                   json={"isFeatured": True}, timeout=10)
            if r.status_code == 200:
                featured_count += 1
        log("PASS" if featured_count > 0 else "FAIL",
            "Feature vehicles", f"Featured {featured_count}/{min(15, len(vehicle_ids))}")
    else:
        log("FAIL", "Admin vehicle list", f"HTTP {r.status_code}")

# ═══════════════════════════════════════════════════════════════
# 6. ADVERTISING ROTATION (DISPLAY ADS)
# ═══════════════════════════════════════════════════════════════
print("\n" + "="*70)
print("📋 PHASE 6: ADVERTISING ROTATION & DISPLAY")
print("="*70)

ad_slots = ["homepage-featured", "homepage-premium", "search-sidebar", "vehicle-detail"]
for slot in ad_slots:
    r = requests.get(f"{BASE}/advertising/rotation/{slot}", timeout=15)
    if r.status_code == 200:
        ads = r.json()
        ad_list = ads if isinstance(ads, list) else ads.get("ads", ads.get("data", []))
        log("PASS", f"Ad rotation: {slot}", f"{len(ad_list)} ads")
    else:
        log("FAIL", f"Ad rotation: {slot}", f"HTTP {r.status_code}")

# Homepage categories
r = requests.get(f"{BASE}/advertising/homepage/categories", timeout=15)
if r.status_code == 200:
    cats = r.json()
    log("PASS", "Homepage categories config", f"Got {len(cats) if isinstance(cats, list) else 'config'}")
else:
    log("FAIL", "Homepage categories config", f"HTTP {r.status_code}")

# Homepage brands
r = requests.get(f"{BASE}/advertising/homepage/brands", timeout=15)
if r.status_code == 200:
    brands = r.json()
    brand_list = brands if isinstance(brands, list) else brands.get("data", brands.get("brands", []))
    log("PASS", "Homepage brands config", f"Got {len(brand_list) if isinstance(brand_list, list) else 'config'}")
else:
    log("FAIL", "Homepage brands config", f"HTTP {r.status_code}")

# ═══════════════════════════════════════════════════════════════
# 7. BUYER FLOW TESTS
# ═══════════════════════════════════════════════════════════════
print("\n" + "="*70)
print("📋 PHASE 7: BUYER ACTOR TESTS")
print("="*70)

if buyer_session:
    # Browse vehicles
    r = buyer_session.get(f"{BASE}/vehicles?pageSize=10", timeout=15)
    if r.status_code == 200:
        vdata = r.json()
        items = vdata.get("data", {}).get("items", vdata.get("items", []))
        log("PASS", "Buyer: Browse vehicles", f"Got {len(items)} vehicles")
    else:
        log("FAIL", "Buyer: Browse vehicles", f"HTTP {r.status_code}")
    
    # Search vehicles
    r = buyer_session.get(f"{BASE}/vehicles?search=toyota&pageSize=5", timeout=15)
    if r.status_code == 200:
        log("PASS", "Buyer: Search vehicles (toyota)")
    else:
        log("FAIL", "Buyer: Search vehicles", f"HTTP {r.status_code}")
    
    # View featured vehicles
    r = buyer_session.get(f"{BASE}/vehicles?isFeatured=true&pageSize=5", timeout=15)
    if r.status_code == 200:
        log("PASS", "Buyer: View featured vehicles")
    else:
        log("FAIL", "Buyer: View featured vehicles", f"HTTP {r.status_code}")

# ═══════════════════════════════════════════════════════════════
# 8. DEALER FLOW TESTS
# ═══════════════════════════════════════════════════════════════
print("\n" + "="*70)
print("📋 PHASE 8: DEALER ACTOR TESTS")
print("="*70)

if dealer_session:
    dealer_id = "f3aaadc5-d6ab-4992-9e48-e74454fb6ca2"
    
    # View own listings
    r = dealer_session.get(f"{BASE}/vehicles?dealerId={dealer_id}&pageSize=10", timeout=15)
    if r.status_code == 200:
        log("PASS", "Dealer: View own listings")
    else:
        log("FAIL", "Dealer: View own listings", f"HTTP {r.status_code}")
    
    # View billing dashboard
    r = dealer_session.get(f"{BASE}/dealer-billing/dashboard/{dealer_id}", timeout=15)
    if r.status_code == 200:
        dashboard = r.json()
        log("PASS", "Dealer: Billing dashboard")
    else:
        log("FAIL", "Dealer: Billing dashboard", f"HTTP {r.status_code}")
    
    # View available plans
    r = dealer_session.get(f"{BASE}/dealer-billing/plans", timeout=15)
    if r.status_code == 200:
        plans = r.json()
        plan_count = len(plans) if isinstance(plans, list) else 0
        log("PASS", "Dealer: View plans", f"{plan_count} plans available")
    else:
        log("FAIL", "Dealer: View plans", f"HTTP {r.status_code}")

# ═══════════════════════════════════════════════════════════════
# 9. ADMIN FLOW TESTS
# ═══════════════════════════════════════════════════════════════
print("\n" + "="*70)
print("📋 PHASE 9: ADMIN ACTOR TESTS")
print("="*70)

if admin_session:
    # Admin: List all dealers
    r = admin_session.get(f"{BASE}/admin/dealers?pageSize=10", timeout=15)
    if r.status_code == 200:
        log("PASS", "Admin: List dealers")
    else:
        log("FAIL", "Admin: List dealers", f"HTTP {r.status_code}")
    
    # Admin: List all vehicles
    r = admin_session.get(f"{BASE}/admin/vehicles?pageSize=10", timeout=15)
    if r.status_code == 200:
        log("PASS", "Admin: List vehicles")
    else:
        log("FAIL", "Admin: List vehicles", f"HTTP {r.status_code}")
    
    # Admin: Platform stats
    r = admin_session.get(f"{BASE}/admin/analytics/summary", timeout=15)
    if r.status_code == 200:
        log("PASS", "Admin: Platform analytics")
    elif r.status_code == 404:
        log("FAIL", "Admin: Platform analytics", "Endpoint not found")
    else:
        log("FAIL", "Admin: Platform analytics", f"HTTP {r.status_code}")

# ═══════════════════════════════════════════════════════════════
# 10. CHATBOT INTEGRATION TEST
# ═══════════════════════════════════════════════════════════════
print("\n" + "="*70)
print("📋 PHASE 10: CHATBOT & SUPPORT AGENT")
print("="*70)

# Start support chat
r = requests.post(f"{BASE}/chat/start", json={"agentType": "support"}, timeout=15)
if r.status_code == 200:
    chat_data = r.json()
    session_token = chat_data.get("data", {}).get("sessionToken") or chat_data.get("sessionToken")
    if session_token:
        log("PASS", "Support chat: Start session", f"Token: {session_token[:20]}...")
        
        # Ask about plans
        r2 = requests.post(f"{BASE}/chat/message", json={
            "sessionToken": session_token,
            "message": "¿Cuáles son los planes disponibles para dealers?",
            "type": 1
        }, timeout=30)
        if r2.status_code == 200:
            resp = r2.json()
            bot_msg = resp.get("data", {}).get("response") or resp.get("response", "")
            has_plan_info = any(w in bot_msg.lower() for w in ["plan", "libre", "visible", "pro", "elite", "gratis", "$"])
            if has_plan_info:
                log("PASS", "Support: Knows about plans", f"Response mentions plans ({len(bot_msg)} chars)")
            else:
                log("FAIL", "Support: Knows about plans", f"Response: {bot_msg[:100]}")
        else:
            log("FAIL", "Support: Ask about plans", f"HTTP {r2.status_code}")
    else:
        log("FAIL", "Support chat start", "No session token in response")
else:
    log("FAIL", "Support chat start", f"HTTP {r.status_code}")

# ═══════════════════════════════════════════════════════════════
# SUMMARY
# ═══════════════════════════════════════════════════════════════
print("\n" + "="*70)
print("📊 E2E TEST SUMMARY")
print("="*70)
total = results["pass"] + results["fail"]
pct = (results["pass"] / total * 100) if total > 0 else 0
print(f"  ✅ Passed: {results['pass']}")
print(f"  ❌ Failed: {results['fail']}")
print(f"  📊 Total:  {total}")
print(f"  📈 Rate:   {pct:.1f}%")

# List failures
failures = [t for t in results["tests"] if t["status"] == "FAIL"]
if failures:
    print(f"\n  ⚠️ Failed tests:")
    for f in failures:
        print(f"     • {f['test']}: {f['detail']}")

print("="*70)
sys.exit(0 if results["fail"] == 0 else 1)
