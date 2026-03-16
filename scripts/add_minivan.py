#!/usr/bin/env python3
"""Create 1 Dodge Grand Caravan Minivan, then activate via SQL."""
import urllib.request, urllib.error, json, subprocess

GATEWAY = "http://localhost:18443"

# Step 1 — Login
login_data = json.dumps({"email": "nmateo@okla.com.do", "password": "Dealer2024!"}).encode()
req = urllib.request.Request(f"{GATEWAY}/api/auth/login", data=login_data,
                             headers={"Content-Type": "application/json"})
with urllib.request.urlopen(req) as r:
    token = json.load(r).get("accessToken", "")
print(f"Token: {token[:30]}...")

# Step 2 — Create vehicle
vehicle = {
    "title": "2023 Dodge Grand Caravan",
    "make": "Dodge",
    "model": "Grand Caravan",
    "trim": "GT",
    "year": 2023,
    "price": 1600000,
    "currency": "DOP",
    "mileage": 22450,
    "mileageUnit": "Kilometers",
    "bodyStyle": "Minivan",
    "fuelType": "Gasoline",
    "transmission": "Automatic",
    "driveType": "FWD",
    "condition": "Used",
    "engineSize": "3.6",
    "horsepower": 283,
    "seats": 7,
    "doors": 4,
    "color": "Blanco Perla",
    "interiorColor": "Negro",
    "description": "Dodge Grand Caravan GT 2023. Motor 3.6L V6, transmision automatica. Aire acondicionado dual, pantalla tactil, camara de retroceso, sensores de parqueo. Ideal para familia numerosa.",
    "features": ["Aire Acondicionado Dual", "Pantalla Tactil", "Camara de Retroceso", "Bluetooth"],
    "images": [
        {"url": "https://okla-images-2026.s3.us-east-2.amazonaws.com/stock/minivan-dodge-grand-caravan-2023-white.jpg",
         "isPrimary": True, "sortOrder": 1}
    ],
    "city": "Santo Domingo",
    "state": "DN",
    "locationText": "Santo Domingo, Distrito Nacional",
    "sellerName": "OKLA Automotriz",
    "sellerPhone": "809-555-0100",
    "sellerEmail": "ventas@okla.com.do",
    "sellerWhatsApp": "18095550100",
    "disclaimerAccepted": True,
}
data = json.dumps(vehicle).encode()
req = urllib.request.Request(f"{GATEWAY}/api/vehicles", data=data,
                             headers={"Content-Type": "application/json",
                                      "Authorization": f"Bearer {token}"})
try:
    with urllib.request.urlopen(req) as r:
        resp = json.load(r)
        vid = resp.get("id")
        print(f"Created: id={vid} status={resp.get('status')}")
except urllib.error.HTTPError as e:
    body = e.read().decode()
    print(f"HTTP Error {e.code}: {body[:300]}")
    exit(1)

# Step 3 — Activate via SQL
sql = f"""
UPDATE vehicles
SET
  "Status" = 'Active',
  "PublishedAt" = NOW(),
  "ApprovedAt" = NOW(),
  "ApprovedBy" = NULL,
  "IsFeatured" = false,
  "DisclaimerAcceptedAt" = NOW(),
  "DisclaimerTosVersion" = '2026.1',
  "UpdatedAt" = NOW()
WHERE "Id" = '{vid}'
RETURNING "Id", "Make", "Model", "BodyStyle", "Status";
"""

result = subprocess.run(
    ["/opt/homebrew/opt/libpq/bin/psql",
     "-h", "okla-db-do-user-31493168-0.g.db.ondigitalocean.com",
     "-p", "25060", "-U", "doadmin", "-d", "vehiclessaleservice",
     "--set=sslmode=require", "-c", sql],
    env={"PGPASSWORD": "REDACTED_USE_DB_PASSWORD_ENV", "PATH": "/opt/homebrew/opt/libpq/bin:/usr/bin:/bin"},
    capture_output=True, text=True
)
print(result.stdout.strip())
if result.returncode != 0:
    print("STDERR:", result.stderr[:200])
