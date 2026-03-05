#!/usr/bin/env python3
"""Test AdvertisingService reporting endpoints and NotificationService templates."""
import urllib.request
import urllib.parse
import json
import ssl
import time

BASE = "https://okla.com.do"
ctx = ssl.create_default_context()
CSRF = "okla-audit-csrf-token-2026"

def login(email, password):
    data = json.dumps({"email": email, "password": password}).encode()
    req = urllib.request.Request(
        f"{BASE}/api/auth/login",
        data=data,
        headers={"Content-Type": "application/json", "X-CSRF-Token": CSRF}
    )
    resp = urllib.request.urlopen(req, context=ctx)
    d = json.loads(resp.read())
    # Try multiple response structures
    if isinstance(d, dict):
        inner = d.get("data", d)
        if isinstance(inner, dict):
            token = inner.get("token") or inner.get("accessToken")
            uid = inner.get("userId") or inner.get("id")
        else:
            token = d.get("token") or d.get("accessToken")
            uid = d.get("userId") or d.get("id")
    else:
        token = None
        uid = None
    if not token:
        print(f"  Login response keys: {list(d.keys()) if isinstance(d, dict) else type(d)}")
        print(f"  Full response: {json.dumps(d, indent=2, default=str)[:500]}")
    return token, uid

def get(url, token):
    headers = {"Authorization": f"Bearer {token}", "X-CSRF-Token": CSRF}
    req = urllib.request.Request(url, headers=headers)
    resp = urllib.request.urlopen(req, context=ctx)
    return json.loads(resp.read())

def safe_get(url, token, label=""):
    try:
        data = get(url, token)
        print(f"\n=== {label} (200 OK) ===")
        print(json.dumps(data, indent=2, default=str)[:1200])
        return data
    except urllib.error.HTTPError as e:
        body = e.read().decode() if e.fp else ""
        print(f"\n=== {label} (HTTP {e.code}) ===")
        print(body[:500])
        return None

# Login as dealer
print("Logging in as dealer...")
dealer_pass = "Dealer2026" + "!" + "@#"
dealer_token, dealer_id = login("nmateo@okla.com.do", dealer_pass)
if not dealer_token:
    print("ERROR: Could not get dealer token")
    exit(1)
print(f"Dealer token: {dealer_token[:30]}...")
print(f"Dealer ID: {dealer_id}")

time.sleep(2)

# Login as admin
print("\nLogging in as admin...")
admin_pass = "Admin123" + "!" + "@#"
admin_token, admin_id = login("admin@okla.local", admin_pass)
if not admin_token:
    print("ERROR: Could not get admin token")
    exit(1)
print(f"Admin token: {admin_token[:30]}...")

time.sleep(2)

# 1. Owner Report (dealer)
safe_get(
    f"{BASE}/api/advertising/reports/owner/{dealer_id}?ownerType=Dealer&daysBack=30",
    dealer_token,
    "Owner Report (Dealer, 30 days)"
)

# 2. Public Stats
safe_get(
    f"{BASE}/api/advertising/reports/public-stats",
    dealer_token,
    "Public Stats"
)

# 3. Platform Report (admin only)
safe_get(
    f"{BASE}/api/advertising/reports/platform?daysBack=30",
    admin_token,
    "Platform Report (Admin, 30 days)"
)

# 4. Dealer Campaigns
safe_get(
    f"{BASE}/api/advertising/campaigns?ownerId={dealer_id}",
    dealer_token,
    "Dealer Campaigns"
)

# 5. Check notification templates
safe_get(
    f"{BASE}/api/notifications/templates",
    admin_token,
    "Notification Templates"
)

# 6. Check if scheduled notifications exist
safe_get(
    f"{BASE}/api/notifications/scheduled",
    admin_token,
    "Scheduled Notifications"
)

# 7. Check advertising tracking impressions today
safe_get(
    f"{BASE}/api/advertising/catalog",
    dealer_token,
    "Advertising Catalog (active campaigns)"
)

print("\n\n=== SUMMARY ===")
print("AdvertisingService reporting endpoints tested.")
print("Check above for data availability and response structure.")
