#!/usr/bin/env python3
"""
Publish and approve newly created vehicle listings so they appear in public sections.
"""
import json
import secrets
import urllib.request
import urllib.error

BASE = "http://localhost:18443"
ADMIN_EMAIL = "admin@okla.local"
ADMIN_PASS  = "Admin123!@#"

NEW_VEHICLE_IDS = [
    "d96a9083-179f-4f3f-b6e8-5971ad0b5adb",   # Toyota Hilux [Pickup]
    "ce3cf724-0984-470b-8a2f-ca715b68aa76",   # Ford Mustang [SportsCar]
    "fcbbdf9b-ab67-4607-9952-77af0aca4638",   # Chevrolet Camaro [SportsCar]
    "fa0e2c2c-0c34-4316-95fa-59e9e1319089",   # Mercedes C300 Cabriolet [Convertible]
    "255d2557-9d10-4e24-b899-a0a0d727416e",   # BMW 430i Convertible [Convertible]
    "ce60c847-6375-4acf-9548-2cfa6c0fec61",   # MB Sprinter 2500 [Van]
    "17b32483-3e11-4840-b606-f9005fdcf8fd",   # Ford Transit 350 [Van]
    "46ee9b2c-69c5-46b9-a583-6fb411867fda",   # Chrysler Voyager [Minivan]
    "c568a56d-9ce1-4046-89b8-32c53c490db5",   # Toyota Alphard [Minivan]
    "b52a0ec4-e6af-4f17-9411-4f79424cc39c",   # Toyota Corolla Cross Hybrid [Hybrid]
    "0ef531fa-7e4f-49e0-a58b-8166b32ba6f1",   # BYD Atto 3 [Electric]
]


def http_call(method, path, data=None, token=None, csrf=None):
    url = BASE + path
    body = json.dumps(data).encode() if data is not None else b""
    req = urllib.request.Request(url, data=body, method=method)
    req.add_header("Content-Type", "application/json")
    req.add_header("Accept", "application/json")
    if token:
        req.add_header("Authorization", f"Bearer {token}")
    if csrf:
        req.add_header("X-CSRF-Token", csrf)
        req.add_header("Cookie", f"csrf_token={csrf}; access_token={token}")
    try:
        with urllib.request.urlopen(req, timeout=30) as resp:
            body_text = resp.read().decode()
            return resp.status, json.loads(body_text) if body_text else {}
    except urllib.error.HTTPError as e:
        body_text = e.read().decode()
        try:
            return e.code, json.loads(body_text)
        except Exception:
            return e.code, {"error": body_text[:300]}
    except Exception as ex:
        return 0, {"error": str(ex)}


def login(email, password):
    csrf = secrets.token_hex(32)
    status, resp = http_call("POST", "/api/auth/login", {"email": email, "password": password}, csrf=csrf)
    if status not in (200, 201):
        return None, None, None
    token = resp.get("accessToken") or resp.get("token")
    if not token:
        token = resp.get("data", {}).get("accessToken")
    return token, csrf, resp.get("userId") or resp.get("id")


def run():
    print("Publishing and approving new vehicle listings...")
    token, csrf, _ = login(ADMIN_EMAIL, ADMIN_PASS)
    if not token:
        print("❌ Admin login failed")
        return

    print(f"✅ Admin logged in\n")

    results = {"published": 0, "approved": 0, "already_active": 0, "failed": 0}

    for vid in NEW_VEHICLE_IDS:
        # Check current status
        s, v = http_call("GET", f"/api/vehicles/{vid}", token=token, csrf=csrf)
        if s != 200:
            print(f"  ❌ GET {vid[:8]} failed ({s})")
            results["failed"] += 1
            continue

        current_status = v.get("status")
        label = f"{v.get('year')} {v.get('make')} {v.get('model')} [{v.get('bodyStyle')}]"

        if current_status == "Active":
            print(f"  ℹ  {label} already Active, skipping")
            results["already_active"] += 1
            continue

        print(f"  → {label} | status={current_status}")

        # Step 1: Submit for review / publish (from Draft → PendingReview or Active)
        if current_status == "Draft":
            s2, r2 = http_call("POST", f"/api/vehicles/{vid}/publish", {}, token=token, csrf=csrf)
            if s2 in (200, 204):
                print(f"    ✅ Published (status may be PendingReview)")
                results["published"] += 1
                current_status = "PendingReview"
            else:
                print(f"    ⚠️  Publish returned {s2}: {str(r2)[:150]}")

        # Step 2: Admin approve (PendingReview → Active)
        s3, r3 = http_call(
            "POST", f"/api/vehicles/{vid}/approve",
            {"notes": "Approved via OKLA section ads manager"},
            token=token, csrf=csrf
        )
        if s3 in (200, 204):
            print(f"    ✅ Approved → Active")
            results["approved"] += 1
        else:
            # Try approving directly without publish step
            print(f"    ⚠️  Approve returned {s3}: {str(r3)[:150]}")
            # Check final status
            s4, v4 = http_call("GET", f"/api/vehicles/{vid}", token=token, csrf=csrf)
            final = v4.get("status") if s4 == 200 else "unknown"
            print(f"    Final status: {final}")
            if final == "Active":
                results["approved"] += 1
            else:
                results["failed"] += 1

    print(f"\nResults: published={results['published']}, approved={results['approved']}, "
          f"already_active={results['already_active']}, failed={results['failed']}")

    # Final section counts
    print("\nFinal section counts:")
    checks = [
        ("bodyStyle", "Hatchback",   "Hatchbacks"),
        ("bodyStyle", "Pickup",      "Camionetas"),
        ("bodyStyle", "SportsCar",   "Deportivos"),
        ("bodyStyle", "Convertible", "Convertibles"),
        ("bodyStyle", "Van",         "Vans"),
        ("bodyStyle", "Minivan",     "Minivans"),
        ("fuelType",  "Hybrid",      "Híbridos"),
        ("fuelType",  "Electric",    "Eléctricos"),
    ]
    for ftype, fval, name in checks:
        s, r = http_call("GET", f"/api/vehicles?{ftype}={fval}&limit=15", token=token, csrf=csrf)
        cnt = len(r.get("vehicles", [])) if s == 200 else "ERR"
        print(f"  {name}: {cnt}")


if __name__ == "__main__":
    run()
