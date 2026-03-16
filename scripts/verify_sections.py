#!/usr/bin/env python3
import urllib.request, json

GATEWAY = "http://localhost:18443"

def count(params):
    url = f"{GATEWAY}/api/vehicles?{params}&limit=20&sortBy=featured"
    try:
        with urllib.request.urlopen(url, timeout=8) as r:
            d = json.load(r)
            return d.get("totalCount", len(d.get("vehicles", d.get("data", []))))
    except Exception as e:
        return f"ERR: {e}"

sections = [
    ("Hatchbacks",   "bodyStyle=Hatchback"),
    ("Camionetas",   "bodyStyle=Pickup"),
    ("Deportivos",   "bodyStyle=SportsCar"),
    ("Convertibles", "bodyStyle=Convertible"),
    ("Vans",         "bodyStyle=Van"),
    ("Minivans",     "bodyStyle=Minivan"),
    ("Hibridos",     "fuelType=Hybrid"),
    ("Electricos",   "fuelType=Electric"),
]

targets = {
    "Hatchbacks": 10,
    "Camionetas": 10,
    "Deportivos": 10,
    "Convertibles": 10,
    "Vans": 10,
    "Minivans": 10,
    "Hibridos": 10,
    "Electricos": 10,
}

print(f"{'Section':<15} {'Count':>6}  {'Target':>6}  Status")
print("-" * 46)
all_ok = True
for name, params in sections:
    c = count(params)
    t = targets[name]
    if isinstance(c, int):
        ok = "✓" if c == t else f"MISS (expected {t})"
        if c != t:
            all_ok = False
    else:
        ok = c
        all_ok = False
    print(f"{name:<15} {str(c):>6}  {t:>6}  {ok}")

print("-" * 46)
print("ALL SECTIONS OK" if all_ok else "SOME SECTIONS NEED ATTENTION")
