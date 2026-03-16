#!/usr/bin/env python3
"""
Audit: Find vehicles sharing Unsplash photo IDs.
A vehicle listing page should never show the same stock photo used by other listings.
"""
import subprocess, re, urllib.request, urllib.error
from collections import defaultdict
from concurrent.futures import ThreadPoolExecutor, as_completed

DB_DSN = (
    "host=okla-db-do-user-31493168-0.g.db.ondigitalocean.com "
    "port=25060 dbname=vehiclessaleservice user=doadmin "
    "sslmode=require password=REDACTED_USE_DB_PASSWORD_ENV"
)
PSQL = "/opt/homebrew/opt/libpq/bin/psql"
PSQL_ENV = {"PGPASSWORD": "REDACTED_USE_DB_PASSWORD_ENV",
             "PATH": "/opt/homebrew/opt/libpq/bin:/usr/bin:/bin"}

def psql(sql: str) -> str:
    r = subprocess.run([PSQL, DB_DSN, "-t", "-A", "-c", sql],
                       capture_output=True, text=True, env=PSQL_ENV)
    if r.returncode != 0:
        raise RuntimeError(r.stderr[:300])
    return r.stdout.strip()

def extract_photo_id(url: str) -> str | None:
    """Extract Unsplash photo ID from URL."""
    m = re.search(r'photo-([a-f0-9\-]+)', url)
    return m.group(0) if m else None

# ── Pull all images ──────────────────────────────────────────────────────────
rows_raw = psql("""
SELECT vi."VehicleId", vi."Id", vi."Url", vi."IsPrimary"::text, vi."SortOrder"::text,
       v."Make", v."Model", v."Year"::text, v."BodyStyle"
FROM vehicle_images vi
JOIN vehicles v ON vi."VehicleId" = v."Id"
WHERE v."Status" = 'Active' AND v."IsDeleted" = false
ORDER BY vi."VehicleId", vi."IsPrimary" DESC, vi."SortOrder"
""")

# Parse
images = []
for line in rows_raw.splitlines():
    parts = line.split("|")
    if len(parts) < 9:
        continue
    vid, img_id, url, is_primary, sort, make, model, year, body = parts[:9]
    photo_id = extract_photo_id(url) if "unsplash" in url else None
    images.append({
        "vehicle_id": vid, "img_id": img_id, "url": url,
        "is_primary": is_primary == "t",
        "sort": int(sort) if sort.isdigit() else 0,
        "make": make, "model": model, "year": year, "body": body,
        "photo_id": photo_id,
        "label": f"{year} {make} {model} ({body})",
    })

print(f"Total images: {len(images)}")
print(f"Unique vehicles: {len({i['vehicle_id'] for i in images})}")
print(f"Unsplash images: {sum(1 for i in images if i['photo_id'])}")

# ── Group by photo_id → list of (vehicle_id, label, is_primary) ─────────────
photo_to_vehicles: dict[str, list[dict]] = defaultdict(list)
for img in images:
    if img["photo_id"]:
        photo_to_vehicles[img["photo_id"]].append(img)

# ── Find photo IDs used by 2+ DIFFERENT vehicles ────────────────────────────
multi_vehicle_photos = {
    pid: rows
    for pid, rows in photo_to_vehicles.items()
    if len({r["vehicle_id"] for r in rows}) >= 2
}

print(f"\n{'='*70}")
print("DUPLICATE UNSPLASH PHOTO IDs ACROSS VEHICLES")
print(f"{'='*70}")
print(f"Photos used by 2+ vehicles: {len(multi_vehicle_photos)}")

# Build per-vehicle breakdown
vehicle_dup_images: dict[str, list[dict]] = defaultdict(list)  # vehicle_id → [img rows affected]

total_affected_rows = 0
for photo_id, rows in sorted(multi_vehicle_photos.items(),
                              key=lambda x: -len({r['vehicle_id'] for r in x[1]})):
    vids = list(dict.fromkeys(r["vehicle_id"] for r in rows))  # preserve insertion order, unique
    labels = [rows[[r["vehicle_id"] for r in rows].index(v)]["label"] for v in vids]
    print(f"\n  📷 {photo_id} — used by {len(vids)} vehicles:")
    for row in rows:
        prim = "★ PRIMARY" if row["is_primary"] else "  img"
        print(f"     {prim} | {row['label']}")
        vehicle_dup_images[row["vehicle_id"]].append(row)
        total_affected_rows += 1

print(f"\n{'─'*70}")
print(f"Total affected image rows  : {total_affected_rows}")
print(f"Total affected vehicles    : {len(vehicle_dup_images)}")

# ── Vehicles where 100% of their images are duplicated ──────────────────────
print(f"\n{'─'*70}")
print("VEHICLES WHERE ALL IMAGES ARE SHARED PHOTOS:")
vehicle_images_map: dict[str, list[dict]] = defaultdict(list)
for img in images:
    vehicle_images_map[img["vehicle_id"]].append(img)

all_shared_vehicles = []
for vid, imgs in vehicle_images_map.items():
    unsplash_imgs = [i for i in imgs if i["photo_id"]]
    shared_imgs = [i for i in unsplash_imgs if i["photo_id"] in multi_vehicle_photos]
    if unsplash_imgs and len(shared_imgs) == len(unsplash_imgs) and len(imgs) == len(unsplash_imgs):
        all_shared_vehicles.append(imgs[0]["label"] + " — " + vid)

for v in sorted(all_shared_vehicles):
    print(f"  ⚠ {v}")
print(f"  Total: {len(all_shared_vehicles)}")
