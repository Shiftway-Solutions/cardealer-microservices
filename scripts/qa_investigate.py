#!/usr/bin/env python3
"""Quick investigation of failed QA tests."""
import json, urllib.request, ssl
ctx = ssl.create_default_context()
CSRF = "okla-audit-csrf-token-2026"

def http(url, data=None, h=None):
    hdrs = {"Content-Type": "application/json", "X-CSRF-Token": CSRF, "Cookie": "csrf_token=" + CSRF}
    if h: hdrs.update(h)
    body = json.dumps(data).encode() if data else None
    req = urllib.request.Request(url, data=body, headers=hdrs, method="POST" if body else "GET")
    try:
        with urllib.request.urlopen(req, timeout=20, context=ctx) as r: return r.status, json.loads(r.read() or b'{}')
    except urllib.error.HTTPError as e:
        try: return e.code, json.loads(e.read().decode())
        except: return e.code, {"error": str(e)}
    except Exception as e: return 0, {"error": str(e)}

BASE = "https://okla.com.do/api"

# V01: Vehicle listing - test different query patterns
print("=== Vehicle Listing Tests ===")
urls = [
    "/vehicles?Page=1&PageSize=10",
    "/vehicles?StatusFilter=Active&Page=1&PageSize=10",
    "/vehicles?page=1&pageSize=10",
]
for url in urls:
    s, d = http(BASE + url)
    data = d.get("data", d) if isinstance(d, dict) else d
    items = data.get("items", []) if isinstance(data, dict) else (data if isinstance(data, list) else [])
    print(f"  {s} -> {len(items)} items | {url}")
    if items:
        print(f"    First: {items[0].get('title', items[0].get('id','?'))}")
    elif isinstance(data, dict):
        print(f"    Keys: {list(data.keys())[:5]}")
        if "totalItems" in data or "pagination" in data:
            print(f"    Pagination: {data.get('pagination', data.get('totalItems'))}")

# D01: Dealer listing - needs auth?
print("\n=== Dealer Listing ===")
s, d = http(BASE + "/auth/login", {"email": "admin@okla.local", "password": "Admin123!@#"})
admin_tok = d["data"]["accessToken"]
ah = {"Authorization": "Bearer " + admin_tok, "Cookie": "csrf_token=" + CSRF + "; okla_access_token=" + admin_tok}

for url in ["/dealers", "/dealers?page=1&pageSize=10"]:
    s, d = http(BASE + url)
    print(f"  No auth {url}: {s}")
    s, d = http(BASE + url, h=ah)
    print(f"  Auth {url}: {s}")

# ADM04: Error logs
print("\n=== Error Logs ===")
for url in ["/errors", "/errors?page=1&pageSize=5", "/errorlogs", "/admin/errors"]:
    s, d = http(BASE + url, h=ah)
    print(f"  {url}: {s} -> {json.dumps(d)[:80]}")

# Health checks - try via gateway
print("\n=== Health Paths ===")
for path in ["/health", "/api/vehicles/../health", "/api/auth/../health"]:
    try:
        req = urllib.request.Request("https://okla.com.do" + path, headers={"Accept": "application/json"})
        with urllib.request.urlopen(req, timeout=10, context=ctx) as r:
            print(f"  {path}: {r.status}")
    except urllib.error.HTTPError as e:
        print(f"  {path}: {e.code}")
    except Exception as e:
        print(f"  {path}: {str(e)[:50]}")

print("\nDone.")
