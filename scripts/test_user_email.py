#!/usr/bin/env python3
"""Test user service endpoint to see if we can get email from userId."""
import urllib.request, json, ssl, time

BASE = "https://okla.com.do"
ctx = ssl.create_default_context()
CSRF = "okla-audit-csrf-token-2026"

# Login as admin
admin_pass = "Admin123" + "!" + "@#"
data = json.dumps({"email": "admin@okla.local", "password": admin_pass}).encode()
req = urllib.request.Request(BASE+"/api/auth/login", data=data, headers={"Content-Type": "application/json", "X-CSRF-Token": CSRF})
resp = urllib.request.urlopen(req, context=ctx)
d = json.loads(resp.read())
inner = d.get("data", d) if isinstance(d, dict) else d
tok = inner.get("token") or inner.get("accessToken") if isinstance(inner, dict) else None
if not tok:
    print("Login response:", json.dumps(d, indent=2, default=str)[:500])
    exit(1)
print(f"Logged in, token: {tok[:30]}...")
h = {"Authorization": "Bearer "+tok, "X-CSRF-Token": CSRF}

# Test user service - get dealer user by id
dealer_id = "f3aaadc5-d6ab-4992-9e48-e74454fb6ca2"
try:
    req2 = urllib.request.Request(f"{BASE}/api/users/{dealer_id}", headers=h)
    resp2 = urllib.request.urlopen(req2, context=ctx)
    user_data = json.loads(resp2.read())
    print("USER DATA:")
    print(json.dumps(user_data, indent=2, default=str)[:1500])
except urllib.error.HTTPError as e:
    body = e.read().decode() if e.fp else ""
    print(f"HTTP {e.code}: {body[:500]}")

# Also try auth service profile
time.sleep(1)
try:
    req3 = urllib.request.Request(f"{BASE}/api/auth/profile/{dealer_id}", headers=h)
    resp3 = urllib.request.urlopen(req3, context=ctx)
    profile = json.loads(resp3.read())
    print("\nAUTH PROFILE:")
    print(json.dumps(profile, indent=2, default=str)[:1500])
except urllib.error.HTTPError as e:
    body = e.read().decode() if e.fp else ""
    print(f"Auth profile HTTP {e.code}: {body[:300]}")

# Try KYC profile  
time.sleep(1)
try:
    req4 = urllib.request.Request(f"{BASE}/api/kyc/kycprofiles/user/{dealer_id}", headers=h)
    resp4 = urllib.request.urlopen(req4, context=ctx)
    kyc = json.loads(resp4.read())
    print("\nKYC PROFILE:")
    print(json.dumps(kyc, indent=2, default=str)[:1500])
except urllib.error.HTTPError as e:
    body = e.read().decode() if e.fp else ""
    print(f"KYC profile HTTP {e.code}: {body[:300]}")
