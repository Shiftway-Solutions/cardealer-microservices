#!/usr/bin/env python3
"""
Fix broken image URLs in vehicle_images:
- For each broken image row, replace with a working body-style-appropriate URL
- For non-primary broken rows of vehicles that have OTHER working images: delete
- Resets HasBrokenImages=false for fixed vehicles
"""
import subprocess, json, sys, urllib.request, urllib.error
from concurrent.futures import ThreadPoolExecutor, as_completed

DB_DSN = (
    "host=okla-db-do-user-31493168-0.g.db.ondigitalocean.com "
    "port=25060 dbname=vehiclessaleservice user=doadmin "
    "sslmode=require password=REDACTED_USE_DB_PASSWORD_ENV"
)
PSQL = "/opt/homebrew/opt/libpq/bin/psql"
PSQL_ENV = {
    "PGPASSWORD": "REDACTED_USE_DB_PASSWORD_ENV",
    "PATH": "/opt/homebrew/opt/libpq/bin:/usr/bin:/bin"
}

def psql(sql: str) -> str:
    r = subprocess.run([PSQL, DB_DSN, "-t", "-A", "-c", sql],
                       capture_output=True, text=True, env=PSQL_ENV)
    if r.returncode != 0:
        raise RuntimeError(r.stderr[:400])
    return r.stdout.strip()

def psql_exec(sql: str) -> bool:
    r = subprocess.run([PSQL, DB_DSN, "-c", sql],
                       capture_output=True, text=True, env=PSQL_ENV)
    if r.returncode != 0:
        print(f"  SQL ERROR: {r.stderr[:200]}")
    return r.returncode == 0

HEADERS = {
    "User-Agent": "Mozilla/5.0 AppleWebKit/537.36 Chrome/122 Safari/537.36",
    "Accept": "image/avif,image/webp,image/*,*/*;q=0.8",
}

def url_ok(url: str) -> bool:
    try:
        req = urllib.request.Request(url, headers=HEADERS, method="HEAD")
        with urllib.request.urlopen(req, timeout=8) as r:
            return r.status < 400
    except urllib.error.HTTPError as e:
        if e.code == 405:
            try:
                req2 = urllib.request.Request(url, headers={**HEADERS, "Range": "bytes=0-0"})
                with urllib.request.urlopen(req2, timeout=8) as r2:
                    return r2.status < 400
            except Exception:
                return False
        return False
    except Exception:
        return False

# ── Candidate replacement URLs per body style ──────────────────────────────
# Each list is in priority order; first verified working one will be used.
CANDIDATES = {
    "Sedan": [
        "https://images.unsplash.com/photo-1553440569-bcc63803a83d?w=800&q=80",
        "https://images.unsplash.com/photo-1590362891991-f776e747a588?w=800&q=80",
        "https://images.unsplash.com/photo-1494976388531-d1058494cdd8?w=800&q=80",
        "https://images.unsplash.com/photo-1550355291-bbee04a92027?w=800&q=80",
    ],
    "Hatchback": [
        "https://images.unsplash.com/photo-1568605117036-5fe5e7bab0b7?w=800&q=80",
        "https://images.unsplash.com/photo-1553440569-bcc63803a83d?w=800&q=80",
        "https://images.unsplash.com/photo-1494976388531-d1058494cdd8?w=800&q=80",
    ],
    "SUV": [
        "https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=800&q=80",
        "https://images.unsplash.com/photo-1519641471654-76ce0107ad1b?w=800&q=80",
        "https://images.unsplash.com/photo-1537462715879-360eeb61a0ad?w=800&q=80",
        "https://images.unsplash.com/photo-1533473359331-0135ef1b58bf?w=800&q=80",
    ],
    "Crossover": [
        "https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=800&q=80",
        "https://images.unsplash.com/photo-1537462715879-360eeb61a0ad?w=800&q=80",
        "https://images.unsplash.com/photo-1519641471654-76ce0107ad1b?w=800&q=80",
    ],
    "Pickup": [
        "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=800&q=80",
        "https://images.unsplash.com/photo-1503376780353-7e6692767b70?w=800&q=80",
        "https://images.unsplash.com/photo-1519641471654-76ce0107ad1b?w=800&q=80",
    ],
    "Coupe": [
        "https://images.unsplash.com/photo-1566274360960-320a3de4de9c?w=800&q=80",
        "https://images.unsplash.com/photo-1553440569-bcc63803a83d?w=800&q=80",
        "https://images.unsplash.com/photo-1590362891991-f776e747a588?w=800&q=80",
    ],
    "Minivan": [
        "https://images.unsplash.com/photo-1503376780353-7e6692767b70?w=800&q=80",
        "https://images.unsplash.com/photo-1494976388531-d1058494cdd8?w=800&q=80",
        "https://images.unsplash.com/photo-1541899481282-d53bffe3c35d?w=800&q=80",
    ],
    "Van": [
        "https://images.unsplash.com/photo-1503376780353-7e6692767b70?w=800&q=80",
        "https://images.unsplash.com/photo-1494976388531-d1058494cdd8?w=800&q=80",
    ],
    "Convertible": [
        "https://images.unsplash.com/photo-1541899481282-d53bffe3c35d?w=800&q=80",
        "https://images.unsplash.com/photo-1566274360960-320a3de4de9c?w=800&q=80",
    ],
    "SportsCar": [
        "https://images.unsplash.com/photo-1566274360960-320a3de4de9c?w=800&q=80",
        "https://images.unsplash.com/photo-1553440569-bcc63803a83d?w=800&q=80",
    ],
    "Hybrid": [
        "https://images.unsplash.com/photo-1593941707882-a5bba14938c7?w=800&q=80",
        "https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=800&q=80",
    ],
    "Electric": [
        "https://images.unsplash.com/photo-1593941707882-a5bba14938c7?w=800&q=80",
        "https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=800&q=80",
    ],
    # fallback for any unmatched body style
    "default": [
        "https://images.unsplash.com/photo-1494976388531-d1058494cdd8?w=800&q=80",
        "https://images.unsplash.com/photo-1553440569-bcc63803a83d?w=800&q=80",
        "https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=800&q=80",
    ],
}

# ── Pre-test all candidate URLs ────────────────────────────────────────────
print("Pre-testing candidate replacement URLs...", flush=True)
all_candidates = list({u for lst in CANDIDATES.values() for u in lst})
cand_results: dict[str, bool] = {}
with ThreadPoolExecutor(max_workers=20) as pool:
    futures = {pool.submit(url_ok, u): u for u in all_candidates}
    for fut in as_completed(futures):
        u = futures[fut]
        ok = fut.result()
        cand_results[u] = ok
        if not ok:
            print(f"  WARN: candidate down → {u}")

# Build verified candidates per body style
def get_replacement(body_style: str) -> str | None:
    candidates = CANDIDATES.get(body_style) or CANDIDATES.get("default", [])
    for url in candidates:
        if cand_results.get(url, False):
            return url
    return None

# ── Pull broken image rows ────────────────────────────────────────────────────
print("\nFetching broken image rows from DB...", flush=True)
broken_raw = psql("""
SELECT vi."Id", vi."VehicleId", vi."Url", vi."IsPrimary"::text, vi."SortOrder",
       v."Make", v."Model", v."Year", v."BodyStyle"
FROM vehicle_images vi
JOIN vehicles v ON vi."VehicleId" = v."Id"
WHERE v."Status" = 'Active' AND v."IsDeleted" = false
  AND vi."Url" IN (
    'https://images.unsplash.com/photo-1617469767253-70a026ef7ed5?w=800&q=80',
    'https://images.unsplash.com/photo-1611867626292-e7ad37b3f282?w=800&q=80',
    'https://images.unsplash.com/photo-1559768713-bd0ed1fa4dbb?w=800&q=75',
    'https://images.unsplash.com/photo-1559768713-bd0ed1fa4dbb?w=800&q=80',
    'https://images.unsplash.com/photo-1606611013016-969c19ba27c5?w=800',
    'https://images.unsplash.com/photo-1606611013016-969c19ba27c5?w=800&q=80'
  )
ORDER BY vi."VehicleId", vi."IsPrimary" DESC
""")

# Also get S3 connection-reset broken images programmatically by checking
s3_broken_raw = psql("""
SELECT vi."Id", vi."VehicleId", vi."Url", vi."IsPrimary"::text, vi."SortOrder",
       v."Make", v."Model", v."Year", v."BodyStyle"
FROM vehicle_images vi
JOIN vehicles v ON vi."VehicleId" = v."Id"
WHERE v."Status" = 'Active' AND v."IsDeleted" = false
  AND v."HasBrokenImages" = true
  AND vi."Url" LIKE '%amazonaws.com%'
  AND vi."Url" NOT LIKE '%X-Amz-%'
ORDER BY vi."VehicleId", vi."IsPrimary" DESC
""")

# Combine
all_rows = broken_raw + ("\n" + s3_broken_raw if s3_broken_raw else "")

broken_images = []
for line in all_rows.splitlines():
    parts = line.split("|")
    if len(parts) < 9:
        continue
    img_id, vid, url, is_primary, sort, make, model, year, body = parts[:9]
    if not img_id or not vid:
        continue
    broken_images.append({
        "img_id": img_id, "vehicle_id": vid, "url": url,
        "primary": is_primary == "t",
        "sort": int(sort) if sort.isdigit() else 0,
        "label": f"{year} {make} {model}", "body": body,
    })

print(f"Found {len(broken_images)} broken image rows to handle.", flush=True)

# Deduplicate (same img_id may appear from both queries)
seen_ids = set()
unique_broken = []
for row in broken_images:
    if row["img_id"] not in seen_ids:
        seen_ids.add(row["img_id"])
        unique_broken.append(row)

# ── Determine how many OK images each vehicle has ──────────────────────────
# Count working images per vehicle for the broken vehicles
broken_vids = list({r["vehicle_id"] for r in unique_broken})
vid_ok_count: dict[str, int] = {}
if broken_vids:
    vid_list = "', '".join(broken_vids)
    ok_raw = psql(f"""
    SELECT vi."VehicleId", COUNT(*) as ok_count
    FROM vehicle_images vi
    WHERE vi."VehicleId" IN ('{vid_list}')
      AND vi."Url" NOT IN (
        'https://images.unsplash.com/photo-1617469767253-70a026ef7ed5?w=800&q=80',
        'https://images.unsplash.com/photo-1611867626292-e7ad37b3f282?w=800&q=80',
        'https://images.unsplash.com/photo-1559768713-bd0ed1fa4dbb?w=800&q=75',
        'https://images.unsplash.com/photo-1559768713-bd0ed1fa4dbb?w=800&q=80',
        'https://images.unsplash.com/photo-1606611013016-969c19ba27c5?w=800',
        'https://images.unsplash.com/photo-1606611013016-969c19ba27c5?w=800&q=80'
      )
    GROUP BY vi."VehicleId"
    """)
    for line in ok_raw.splitlines():
        parts = line.split("|")
        if len(parts) == 2:
            vid_ok_count[parts[0]] = int(parts[1]) if parts[1].isdigit() else 0

# ── Apply fixes ────────────────────────────────────────────────────────────
print("\nApplying fixes...", flush=True)
fixed = 0
deleted = 0
skipped = 0

for row in unique_broken:
    vid = row["vehicle_id"]
    img_id = row["img_id"]
    is_primary = row["primary"]
    body = row["body"]
    label = row["label"]
    ok_count = vid_ok_count.get(vid, 0)

    if not is_primary and ok_count >= 1:
        # Non-primary broken image, vehicle has other good images → delete broken row
        if psql_exec(f'DELETE FROM vehicle_images WHERE "Id" = \'{img_id}\';'):
            print(f"  🗑  DELETE non-primary broken image | {label} ({body})")
            deleted += 1
        else:
            skipped += 1
    else:
        # Primary OR vehicle has no other images → replace with working URL
        replacement = get_replacement(body)
        if not replacement:
            print(f"  ⚠️  No working candidate for {body} — skipping {label}")
            skipped += 1
            continue
        safe_url = replacement.replace("'", "''")
        safe_orig = row["url"].replace("'", "''")
        # Only set IsPrimary=true if this was primary, else keep sort order
        primary_flag = "true" if is_primary else "false"
        sql = f"""
UPDATE vehicle_images
SET "Url" = '{safe_url}',
    "ThumbnailUrl" = '{safe_url}',
    "IsPrimary" = {primary_flag},
    "ModerationStatus" = 0
WHERE "Id" = '{img_id}';
"""
        if psql_exec(sql):
            action = "★ REPLACE PRIMARY" if is_primary else "  REPLACE non-primary"
            print(f"  {action} | {label} ({body})")
            print(f"        OLD: {row['url'][:80]}")
            print(f"        NEW: {replacement}")
            fixed += 1
        else:
            skipped += 1

print(f"\n{'─'*60}")
print(f"Fixed (replaced) : {fixed}")
print(f"Deleted          : {deleted}")
print(f"Skipped          : {skipped}")

# ── Also handle S3 connection-reset broken PRIMARY images via DB lookup ────
# Get the vehicles that still have broken primary images (the S3 ones from the audit)
s3_check = psql("""
SELECT vi."Id", vi."VehicleId", vi."Url", v."Make", v."Model", v."Year", v."BodyStyle"
FROM vehicle_images vi
JOIN vehicles v ON vi."VehicleId" = v."Id"
WHERE v."HasBrokenImages" = true
  AND vi."IsPrimary" = true
  AND v."Status" = 'Active' AND v."IsDeleted" = false
ORDER BY v."Make"
""")

s3_fix_count = 0
if s3_check.strip():
    print("\nChecking remaining primary images on HasBrokenImages vehicles...", flush=True)
    for line in s3_check.splitlines():
        parts = line.split("|")
        if len(parts) < 7:
            continue
        img_id, vid, url, make, model, year, body = parts[:7]
        ok = url_ok(url if "X-Amz-" not in url else url.split("?")[0])
        if not ok:
            replacement = get_replacement(body)
            if replacement:
                safe_url = replacement.replace("'", "''")
                sql = f"""
UPDATE vehicle_images
SET "Url" = '{safe_url}', "ThumbnailUrl" = '{safe_url}', "ModerationStatus" = 0
WHERE "Id" = '{img_id}';
"""
                if psql_exec(sql):
                    print(f"  ★ REPLACE S3 PRIMARY | {year} {make} {model} ({body})")
                    print(f"        NEW: {replacement}")
                    s3_fix_count += 1

# ── Reset HasBrokenImages for all fixed vehicles ──────────────────────────
print(f"\nResetting HasBrokenImages flags...", flush=True)
if broken_vids:
    vid_list = "', '".join(broken_vids)
    reset_sql = f"""
UPDATE vehicles
SET "HasBrokenImages" = false,
    "BrokenImagesDetectedAt" = NULL,
    "UpdatedAt" = NOW()
WHERE "Id" IN ('{vid_list}');
"""
    if psql_exec(reset_sql):
        print(f"  Reset HasBrokenImages=false for {len(broken_vids)} vehicles.")

print(f"\nTotal fixes applied: {fixed + deleted + s3_fix_count} (replaced: {fixed + s3_fix_count}, deleted: {deleted})")
print("Done.")
