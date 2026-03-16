"""
Dynamic fix: verify and delete broken images for vehicles flagged with HasBrokenImages=true.
Similar to fix_s3_broken_images.py but detects broken images dynamically via HTTP check.
"""
import psycopg2
import urllib.request
import urllib.error
import concurrent.futures

DSN = "host=okla-db-do-user-31493168-0.g.db.ondigitalocean.com port=25060 dbname=vehiclessaleservice user=doadmin sslmode=require password=REDACTED_USE_DB_PASSWORD_ENV"

def check_url(img_id, url):
    try:
        req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"}, method="HEAD")
        with urllib.request.urlopen(req, timeout=8):
            return img_id, url, True
    except Exception:
        # Try GET fallback
        try:
            req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
            with urllib.request.urlopen(req, timeout=8):
                return img_id, url, True
        except Exception:
            return img_id, url, False

def main():
    conn = psycopg2.connect(DSN)
    conn.autocommit = False
    cur = conn.cursor()

    # Find all images for vehicles flagged as broken
    cur.execute("""
        SELECT vi."Id", vi."VehicleId", vi."IsPrimary", vi."Url",
               v."Make", v."Model", v."Year"
        FROM vehicle_images vi
        JOIN vehicles v ON vi."VehicleId" = v."Id"
        WHERE v."HasBrokenImages" = true
          AND v."Status" = 'Active'
        ORDER BY v."Make", v."Year", vi."IsPrimary" DESC
    """)
    images = cur.fetchall()
    print(f"Checking {len(images)} images for {len(set(r[1] for r in images))} vehicles with HasBrokenImages=true...")

    # Check all URLs concurrently
    with concurrent.futures.ThreadPoolExecutor(max_workers=10) as executor:
        futures = [executor.submit(check_url, r[0], r[3]) for r in images]
        results = {f.result()[0]: f.result()[2] for f in concurrent.futures.as_completed(futures)}

    # Report and collect broken IDs
    broken_ids = []
    by_vehicle = {}
    for img in images:
        img_id, vid, is_primary, url, make, model, year = img
        ok = results.get(img_id, False)
        key = (vid, f"{year} {make} {model}")
        by_vehicle.setdefault(key, []).append((img_id, is_primary, url, ok))

    for (vid, label), imgs in by_vehicle.items():
        ok_count = sum(1 for _, _, _, ok in imgs if ok)
        bad_count = sum(1 for _, _, _, ok in imgs if not ok)
        print(f"\n  {label} (VehicleId={vid})")
        for img_id, is_primary, url, ok in imgs:
            status = "✅ OK" if ok else "❌ BROKEN"
            primary_tag = " ★PRIMARY" if is_primary else ""
            print(f"    {status}{primary_tag} | {url[:80]}")
            if not ok:
                broken_ids.append((img_id, vid, is_primary))

    print(f"\nBroken images found: {len(broken_ids)}")
    if not broken_ids:
        # All images are actually OK — just reset the flags
        cur.execute('UPDATE vehicles SET "HasBrokenImages"=false WHERE "HasBrokenImages"=true AND "Status"=\'Active\'')
        print("All images verified OK — cleared HasBrokenImages flags")
        conn.commit()
        return

    # Delete broken images
    broken_img_ids = [str(b[0]) for b in broken_ids]
    cur.execute('DELETE FROM vehicle_images WHERE "Id" = ANY(%s::uuid[])', (broken_img_ids,))
    print(f"Deleted {cur.rowcount} broken image rows")

    # For vehicles where primary was deleted, promote first remaining image
    primary_broken_vehicles = {str(b[1]) for b in broken_ids if b[2]}
    for vid in primary_broken_vehicles:
        cur.execute("""
            SELECT "Id" FROM vehicle_images WHERE "VehicleId"=%s
            ORDER BY "CreatedAt" ASC LIMIT 1
        """, (vid,))
        row = cur.fetchone()
        if row:
            cur.execute('UPDATE vehicle_images SET "IsPrimary"=true WHERE "Id"=%s', (row[0],))
            print(f"  Promoted new primary for vehicle {vid}")
        else:
            print(f"  WARNING: no images left for vehicle {vid}")

    # Reset HasBrokenImages for affected vehicles
    affected_vehicle_ids = list({str(b[1]) for b in broken_ids})
    cur.execute('UPDATE vehicles SET "HasBrokenImages"=false WHERE "Id" = ANY(%s::uuid[])', (affected_vehicle_ids,))
    print(f"Reset HasBrokenImages=false for {cur.rowcount} vehicles")

    conn.commit()
    print("\n✅ Done")

    # Final check
    cur.execute("SELECT COUNT(*) FROM vehicles WHERE \"HasBrokenImages\"=true AND \"Status\"='Active'")
    row1 = cur.fetchone()
    still_broken = row1[0] if row1 else 0
    cur.execute("""
        SELECT COUNT(*), SUM(CASE WHEN "Url" LIKE '%unsplash%' THEN 1 ELSE 0 END),
               SUM(CASE WHEN "Url" LIKE '%amazonaws.com%' THEN 1 ELSE 0 END)
        FROM vehicle_images vi JOIN vehicles v ON vi."VehicleId"=v."Id"
        WHERE v."Status"='Active'
    """)
    row2 = cur.fetchone()
    total, unsplash, s3 = row2 if row2 else (0, 0, 0)
    print(f"\nFinal: {total} images | {s3} S3 | {unsplash} Unsplash | HasBrokenImages=true: {still_broken}")

    cur.close()
    conn.close()

if __name__ == "__main__":
    main()
