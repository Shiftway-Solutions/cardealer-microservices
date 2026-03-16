"""
Migrate pre-signed S3 URLs to permanent public URLs in vehicle_images table.

Pre-signed URL format:
  https://okla-images-2026.s3.us-east-2.amazonaws.com/vehicles/.../file.jpg
  ?X-Amz-Expires=86400&X-Amz-Algorithm=...&X-Amz-Credential=...&...

Permanent public URL format (objects are public-read):
  https://okla-images-2026.s3.us-east-2.amazonaws.com/vehicles/.../file.jpg

Steps:
1. Find all S3 URLs with ?X-Amz-* query params
2. Strip the query params → permanent URL
3. Verify a sample of the permanent URLs return HTTP 200
4. Update DB
5. Run full HTTP verification of all images
"""
import psycopg2
import urllib.request
import urllib.parse
import urllib.error
import concurrent.futures
import sys

DSN = "host=okla-db-do-user-31493168-0.g.db.ondigitalocean.com port=25060 dbname=vehiclessaleservice user=doadmin sslmode=require password=REDACTED_USE_DB_PASSWORD_ENV"

DRY_RUN = "--dry-run" in sys.argv

def to_permanent_url(url: str) -> str | None:
    """Strip X-Amz-* query parameters from a pre-signed S3 URL."""
    if "amazonaws.com" not in url:
        return None
    if "X-Amz" not in url and "x-amz" not in url:
        return None  # Already clean
    parsed = urllib.parse.urlparse(url)
    return urllib.parse.urlunparse(parsed._replace(query="", fragment=""))

def check_url(url: str) -> bool:
    """Return True if URL is reachable (HTTP 200)."""
    try:
        req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"}, method="HEAD")
        with urllib.request.urlopen(req, timeout=8) as resp:
            return 200 <= resp.status < 300
    except Exception:
        try:
            req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
            with urllib.request.urlopen(req, timeout=10) as resp:
                return 200 <= resp.status < 300
        except Exception:
            return False

def main():
    conn = psycopg2.connect(DSN)
    conn.autocommit = False
    cur = conn.cursor()

    print(f"{'[DRY RUN] ' if DRY_RUN else ''}Migrating pre-signed S3 URLs to permanent public URLs...\n")

    # ── 1. Find all S3 pre-signed URLs ───────────────────────────────────────
    cur.execute("""
        SELECT vi."Id", vi."VehicleId", vi."Url", vi."ThumbnailUrl", vi."IsPrimary",
               v."Make", v."Model", v."Year"
        FROM vehicle_images vi
        JOIN vehicles v ON vi."VehicleId" = v."Id"
        WHERE v."Status" = 'Active'
          AND (vi."Url" LIKE '%X-Amz-%' OR vi."Url" LIKE '%x-amz-%'
               OR vi."ThumbnailUrl" LIKE '%X-Amz-%' OR vi."ThumbnailUrl" LIKE '%x-amz-%')
        ORDER BY v."Make", v."Model"
    """)
    rows = cur.fetchall()
    print(f"Found {len(rows)} image rows with pre-signed URLs")

    if not rows:
        print("No pre-signed URLs found. Checking all S3 URLs are clean...")
        cur.execute("""
            SELECT COUNT(*) FROM vehicle_images vi
            JOIN vehicles v ON vi."VehicleId"=v."Id"
            WHERE v."Status"='Active' AND vi."Url" LIKE '%amazonaws.com%'
        """)
        row = cur.fetchone()
        s3_count = row[0] if row else 0
        print(f"Total S3 images: {s3_count} — all clean (no pre-signed params)")
        cur.close()
        conn.close()
        return

    # ── 2. Build migration plan ───────────────────────────────────────────────
    updates = []
    for img_id, vid, url, thumb_url, is_primary, make, model, year in rows:
        new_url = to_permanent_url(url) or url
        new_thumb = to_permanent_url(thumb_url) if thumb_url else thumb_url
        if new_thumb is None:
            new_thumb = thumb_url
        updates.append((img_id, url, new_url, thumb_url, new_thumb, f"{year} {make} {model}", is_primary))

    print(f"\nSample migrations:")
    for img_id, old_url, new_url, _, _, label, is_primary in updates[:3]:
        primary_tag = " [PRIMARY]" if is_primary else ""
        print(f"  {label}{primary_tag}")
        print(f"    OLD: {old_url[:90]}")
        print(f"    NEW: {new_url[:90]}")

    # ── 3. Verify a sample of permanent URLs before committing ───────────────
    print(f"\nVerifying sample of {min(10, len(updates))} permanent URLs...")
    sample = updates[:10]
    sample_urls = [u[2] for u in sample]
    with concurrent.futures.ThreadPoolExecutor(max_workers=5) as executor:
        results = list(executor.map(check_url, sample_urls))

    for url, ok in zip(sample_urls, results):
        status = "✅" if ok else "❌"
        print(f"  {status} {url[:90]}")

    failures = sum(1 for ok in results if not ok)
    if failures > 0:
        print(f"\n⛔ {failures}/{len(sample)} sample URLs FAILED — objects may not be public-read.")
        print("Fix: Set S3 bucket policy or object ACLs to public-read before migrating.")
        if not DRY_RUN:
            print("Aborting migration.")
            cur.close()
            conn.close()
            return
    else:
        print(f"\n✅ All {len(sample)} sample URLs verified accessible")

    if DRY_RUN:
        print(f"\n[DRY RUN] Would update {len(updates)} image rows")
        cur.close()
        conn.close()
        return

    # ── 4. Apply updates ──────────────────────────────────────────────────────
    updated = 0
    for img_id, old_url, new_url, old_thumb, new_thumb, label, _ in updates:
        cur.execute("""
            UPDATE vehicle_images
            SET "Url" = %s,
                "ThumbnailUrl" = CASE WHEN "ThumbnailUrl" = %s THEN %s ELSE "ThumbnailUrl" END
            WHERE "Id" = %s
        """, (new_url, old_thumb, new_thumb, img_id))
        updated += cur.rowcount

    conn.commit()
    print(f"\n✅ Updated {updated} image rows — pre-signed → permanent URLs")

    # ── 5. Final counts ───────────────────────────────────────────────────────
    cur.execute("""
        SELECT COUNT(*) FROM vehicle_images vi
        JOIN vehicles v ON vi."VehicleId"=v."Id"
        WHERE v."Status"='Active'
          AND (vi."Url" LIKE '%X-Amz-%' OR vi."Url" LIKE '%x-amz-%')
    """)
    remaining = cur.fetchone()
    remaining_count = remaining[0] if remaining else 0

    cur.execute("""
        SELECT COUNT(*) FROM vehicle_images vi
        JOIN vehicles v ON vi."VehicleId"=v."Id"
        WHERE v."Status"='Active' AND vi."Url" LIKE '%amazonaws.com%'
    """)
    row2 = cur.fetchone()
    s3_total = row2[0] if row2 else 0

    print(f"\nFinal DB state:")
    print(f"  Total S3 images: {s3_total}")
    print(f"  Pre-signed URLs remaining: {remaining_count}  (target: 0)")

    cur.close()
    conn.close()

if __name__ == "__main__":
    main()
