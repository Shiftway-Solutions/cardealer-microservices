#!/usr/bin/env python3
"""
Audit: All published vehicle image URLs.
- Fetches every image row for Active+non-deleted vehicles
- Checks each URL concurrently (HEAD then GET fallback)
- Reports broken URLs per vehicle
- Updates HasBrokenImages + BrokenImagesDetectedAt in DB
"""
import subprocess, json, sys, urllib.request, urllib.error
from concurrent.futures import ThreadPoolExecutor, as_completed
from datetime import datetime, timezone

DB_DSN = (
    "host=okla-db-do-user-31493168-0.g.db.ondigitalocean.com "
    "port=25060 dbname=vehiclessaleservice user=doadmin "
    "sslmode=require password=REDACTED_USE_DB_PASSWORD_ENV"
)
PSQL = "/opt/homebrew/opt/libpq/bin/psql"

def psql(sql: str) -> str:
    r = subprocess.run(
        [PSQL, DB_DSN, "-t", "-A", "-c", sql],
        capture_output=True, text=True,
        env={"PGPASSWORD": "REDACTED_USE_DB_PASSWORD_ENV",
             "PATH": "/opt/homebrew/opt/libpq/bin:/usr/bin:/bin"}
    )
    if r.returncode != 0:
        raise RuntimeError(r.stderr[:300])
    return r.stdout.strip()

def psql_exec(sql: str):
    r = subprocess.run(
        [PSQL, DB_DSN, "-c", sql],
        capture_output=True, text=True,
        env={"PGPASSWORD": "REDACTED_USE_DB_PASSWORD_ENV",
             "PATH": "/opt/homebrew/opt/libpq/bin:/usr/bin:/bin"}
    )
    return r.returncode == 0

HEADERS = {
    "User-Agent": "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 Chrome/122 Safari/537.36",
    "Accept": "image/avif,image/webp,image/apng,image/*,*/*;q=0.8",
}

def check_url(url: str) -> tuple[str, bool, str]:
    """Returns (url, is_ok, reason)"""
    # Strip expired S3 pre-signed query params — test the base path
    base_url = url.split("?")[0] if "X-Amz-" in url else url
    try:
        req = urllib.request.Request(base_url, headers=HEADERS, method="HEAD")
        req.get_method = lambda: "HEAD"
        with urllib.request.urlopen(req, timeout=10) as r:
            if r.status < 400:
                return (url, True, f"HEAD {r.status}")
            return (url, False, f"HEAD {r.status}")
    except urllib.error.HTTPError as e:
        if e.code == 403 and "amazonaws.com" in base_url:
            # S3 HEAD 403 is normal for presigned URLs — try GET with range
            try:
                req2 = urllib.request.Request(base_url, headers={**HEADERS, "Range": "bytes=0-1023"})
                with urllib.request.urlopen(req2, timeout=10) as r2:
                    return (url, True, f"GET-range {r2.status}")
            except Exception as e2:
                return (url, False, f"S3-GET {e2}")
        if e.code == 405:
            # HEAD not allowed — try GET
            try:
                req3 = urllib.request.Request(base_url, headers={**HEADERS, "Range": "bytes=0-0"})
                with urllib.request.urlopen(req3, timeout=10) as r3:
                    return (url, True, f"GET {r3.status}")
            except Exception as e3:
                return (url, False, f"GET-fallback {e3}")
        return (url, False, f"HTTP {e.code}")
    except Exception as e:
        return (url, False, str(e)[:80])

# ── Pull all images for active vehicles ─────────────────────────────────────
print("Fetching image records from DB...", flush=True)
rows_raw = psql("""
SELECT vi."VehicleId", vi."Id", vi."Url", vi."IsPrimary", vi."SortOrder",
       v."Make", v."Model", v."Year", v."BodyStyle"
FROM vehicle_images vi
JOIN vehicles v ON vi."VehicleId" = v."Id"
WHERE v."Status" = 'Active' AND v."IsDeleted" = false
ORDER BY vi."VehicleId", vi."SortOrder"
""")

if not rows_raw:
    print("No image rows found.")
    sys.exit(0)

# Parse pipe-delimited rows
images = []
for line in rows_raw.splitlines():
    parts = line.split("|")
    if len(parts) < 9:
        continue
    vid, img_id, url, is_primary, sort, make, model, year, body = parts
    images.append({"vehicle_id": vid, "img_id": img_id, "url": url,
                   "primary": is_primary == "t", "sort": sort,
                   "label": f"{year} {make} {model} ({body})"})

total_imgs = len(images)
unique_vehicles = len({i["vehicle_id"] for i in images})
print(f"Found {total_imgs} images across {unique_vehicles} vehicles. Checking URLs...", flush=True)

# ── Concurrent URL check ─────────────────────────────────────────────────────
url_results: dict[str, tuple[bool, str]] = {}
unique_urls = list({i["url"] for i in images})
print(f"Unique URLs to check: {len(unique_urls)}", flush=True)

with ThreadPoolExecutor(max_workers=30) as pool:
    futures = {pool.submit(check_url, url): url for url in unique_urls}
    done = 0
    for fut in as_completed(futures):
        url, ok, reason = fut.result()
        url_results[url] = (ok, reason)
        done += 1
        if done % 50 == 0 or done == len(unique_urls):
            print(f"  Checked {done}/{len(unique_urls)} URLs...", flush=True)

# ── Aggregate broken images per vehicle ──────────────────────────────────────
from collections import defaultdict
vehicle_broken: dict[str, list[dict]] = defaultdict(list)
vehicle_ok: dict[str, int] = defaultdict(int)
vehicle_label: dict[str, str] = {}

for img in images:
    vid = img["vehicle_id"]
    vehicle_label[vid] = img["label"]
    ok, reason = url_results.get(img["url"], (True, "not-checked"))
    if ok:
        vehicle_ok[vid] += 1
    else:
        vehicle_broken[vid].append({
            "img_id": img["img_id"],
            "url": img["url"],
            "primary": img["primary"],
            "reason": reason,
        })

# ── Print report ──────────────────────────────────────────────────────────────
print("\n" + "="*70)
print("OKLA IMAGE AUDIT REPORT — " + datetime.now().strftime("%Y-%m-%d %H:%M:%S"))
print("="*70)

total_broken_vehicles = len(vehicle_broken)
total_broken_imgs = sum(len(v) for v in vehicle_broken.values())
total_ok_imgs = sum(vehicle_ok.values())

print(f"\nSummary:")
print(f"  Vehicles audited  : {unique_vehicles}")
print(f"  Total images      : {total_imgs}")
print(f"  ✅ OK images      : {total_ok_imgs}")
print(f"  ❌ Broken images  : {total_broken_imgs}")
print(f"  Vehicles affected : {total_broken_vehicles}")

if vehicle_broken:
    print(f"\n{'─'*70}")
    print("VEHICLES WITH BROKEN IMAGES:")
    print(f"{'─'*70}")
    for vid, broken_list in sorted(vehicle_broken.items(), key=lambda x: vehicle_label[x[0]]):
        label = vehicle_label[vid]
        ok_count = vehicle_ok[vid]
        print(f"\n  🚗 {label}")
        print(f"     Vehicle ID : {vid}")
        print(f"     OK images  : {ok_count} | Broken: {len(broken_list)}")
        for b in broken_list:
            prim = "★PRIMARY★" if b["primary"] else "  "
            print(f"     {prim} ❌ {b['reason']:20s} {b['url'][:90]}")
else:
    print("\n✅ ALL vehicles have valid image URLs. No broken images found.")

# ── Update HasBrokenImages in DB ──────────────────────────────────────────────
print(f"\n{'─'*70}")
print("Updating HasBrokenImages in database...")

# Get all active vehicle IDs that currently exist
all_vehicle_ids = list(vehicle_label.keys())

# Reset ALL to false first (batch)
id_list = "', '".join(all_vehicle_ids)
reset_sql = f"""
UPDATE vehicles
SET "HasBrokenImages" = false,
    "UpdatedAt" = NOW()
WHERE "Id" IN ('{id_list}')
  AND "HasBrokenImages" = true;
"""
if psql_exec(reset_sql):
    print(f"  Reset HasBrokenImages=false for previously broken vehicles.")

# Set broken vehicles to true
if vehicle_broken:
    broken_ids = "', '".join(vehicle_broken.keys())
    now_ts = datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M:%S+00")
    mark_sql = f"""
UPDATE vehicles
SET "HasBrokenImages" = true,
    "BrokenImagesDetectedAt" = '{now_ts}',
    "UpdatedAt" = NOW()
WHERE "Id" IN ('{broken_ids}');
"""
    if psql_exec(mark_sql):
        print(f"  Marked {total_broken_vehicles} vehicles as HasBrokenImages=true.")
    else:
        print("  WARNING: Could not update HasBrokenImages in DB.")
else:
    print("  No broken vehicles to mark.")

print("\nAudit complete.")
