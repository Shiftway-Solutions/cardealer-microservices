#!/usr/bin/env python3
"""
Create 3 PremiumSpot advertising campaigns with full vehicle metadata.
Requires:
  - kubectl port-forward svc/gateway 18443:8080 -n okla  (running)
  - AdminService running with the controller fix (vehicles metadata fields)
"""
import json, urllib.request, urllib.error, urllib.parse, sys, time

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

def get(path):
    req = urllib.request.Request(f"{BASE}{path}")
    with urllib.request.urlopen(req, timeout=30) as resp:
        return resp.status, json.loads(resp.read())

# Step 1: Login as admin
print("Step 1: Logging in as admin...")
status, resp = post("/api/auth/login", {"email": "admin@okla.local", "password": "Admin123!@#"})
if status not in (200, 201):
    print(f"Login FAILED: {status} {resp}")
    sys.exit(1)
token = resp["data"]["accessToken"]
print(f"  ✓ Got token: {token[:30]}...")

# Step 2: Create 3 PremiumSpot campaigns with full vehicle metadata
# The middleware uses Double Submit Cookie pattern:
# X-CSRF-Token header must equal the csrf_token cookie value.
import secrets
CSRF = secrets.token_hex(32)  # 64-char hex, matches frontend generateToken(32)
auth_headers = {
    "Authorization": f"Bearer {token}",
    "X-CSRF-Token": CSRF,
    "Cookie": f"csrf_token={CSRF}",  # correct cookie name from CsrfValidationMiddleware
}

campaigns = [
    {
        "vehicleId": "432bf76a-6d06-41a0-963e-b0481c23221c",
        "ownerId": "9d16915c-e2be-47c9-9134-86b19304bd2c",
        "placementType": "PremiumSpot",
        "pricingModel": "FlatFee",
        "totalBudget": 5000.00,
        "dailyBudget": 500.00,
        "startDate": "2026-01-01T00:00:00Z",
        "endDate": "2026-12-31T23:59:59Z",
        "vehicleTitle": "2024 Porsche Cayenne Turbo GT",
        "vehicleSlug": "porsche-cayenne-turbo-gt-2024",
        "vehicleImageUrl": "https://okla-images-2026.s3.us-east-2.amazonaws.com/vehicles/2026/03/06/0c0a99b8-21bb-4676-bf2b-793d99193d20.jpg",
        "vehiclePrice": 11500000.00,
        "vehicleCurrency": "DOP",
        "vehicleLocation": "Distrito Nacional",
    },
    {
        "vehicleId": "15166e58-f0be-4d2f-9140-45e40de6fd52",
        "ownerId": "9d16915c-e2be-47c9-9134-86b19304bd2c",
        "placementType": "PremiumSpot",
        "pricingModel": "FlatFee",
        "totalBudget": 5000.00,
        "dailyBudget": 500.00,
        "startDate": "2026-01-01T00:00:00Z",
        "endDate": "2026-12-31T23:59:59Z",
        "vehicleTitle": "2024 Mercedes-Benz G63 AMG",
        "vehicleSlug": "mercedes-benz-g63-amg-2024",
        "vehicleImageUrl": "https://okla-images-2026.s3.us-east-2.amazonaws.com/vehicles/2026/03/06/ec0d97ae-c18f-4d2a-b31e-15ea227f77d6.jpg",
        "vehiclePrice": 13200000.00,
        "vehicleCurrency": "DOP",
        "vehicleLocation": "Distrito Nacional",
    },
    {
        "vehicleId": "3b0f2fd9-593e-419f-9e30-c4dc3e155275",
        "ownerId": "9d16915c-e2be-47c9-9134-86b19304bd2c",
        "placementType": "PremiumSpot",
        "pricingModel": "FlatFee",
        "totalBudget": 5000.00,
        "dailyBudget": 500.00,
        "startDate": "2026-01-01T00:00:00Z",
        "endDate": "2026-12-31T23:59:59Z",
        "vehicleTitle": "2023 Bentley Bentayga EWB Azure",
        "vehicleSlug": "bentley-bentayga-ewb-azure-2023",
        "vehicleImageUrl": "https://okla-images-2026.s3.us-east-2.amazonaws.com/vehicles/2026/03/06/f9170344-f56a-4a39-8031-5a60b352e894.jpg",
        "vehiclePrice": 16800000.00,
        "vehicleCurrency": "DOP",
        "vehicleLocation": "Punta Cana",
    },
]

print("\nStep 2: Creating 3 PremiumSpot campaigns...")
created = []
for i, campaign in enumerate(campaigns, 1):
    status, resp = post("/api/advertising/campaigns", campaign, auth_headers)
    if status in (200, 201):
        cid = resp.get("data", {}).get("id", "?")
        print(f"  ✓ Campaign {i} created: {campaign['vehicleTitle']} (ID: {cid})")
        created.append(cid)
    else:
        print(f"  ✗ Campaign {i} FAILED ({status}): {resp}")

print(f"\nStep 3: Verifying rotation endpoint...")
time.sleep(1)
status, resp = get("/api/advertising/rotation/PremiumSpot")
data = resp.get("data", {})
items = data.get("items", []) if isinstance(data, dict) else []
print(f"  Status: {status}, Items: {len(items)}")
for item in items:
    if isinstance(item, dict):
        print(f"    - {item.get('vehicleTitle', 'NO TITLE')} | {str(item.get('vehicleImageUrl', 'NO IMAGE'))[:50]}... | {item.get('vehiclePrice', 'NO PRICE')}")
    else:
        print(f"    - (raw) {str(item)[:80]}")

if items and all(isinstance(item, dict) and item.get("vehicleTitle") and item.get("vehicleImageUrl") and item.get("vehiclePrice") for item in items):
    print("\n✅ SUCCESS: Rotation returns complete vehicle metadata!")
    print("   The 💎 Vehículos Premium section will now show vehicles via advertising rotation.")
else:
    print("\n⚠️  Some items missing metadata - check if AdminService was redeployed with fix.")
