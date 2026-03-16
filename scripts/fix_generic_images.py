"""
Fix generic/duplicate Unsplash images in vehicle listings.

Strategy:
1. For vehicles with S3 images + Unsplash primary: promote first S3 image as primary
2. For vehicles with S3 images + Unsplash secondaries: delete the Unsplash secondaries
3. Special case: Mercedes-Benz Sprinter 2500 already flagged
"""
import psycopg2
import sys

DSN = "host=okla-db-do-user-31493168-0.g.db.ondigitalocean.com port=25060 dbname=vehiclessaleservice user=doadmin sslmode=require password=REDACTED_USE_DB_PASSWORD_ENV"

DRY_RUN = "--dry-run" in sys.argv

def main():
    conn = psycopg2.connect(DSN)
    conn.autocommit = False
    cur = conn.cursor()

    print(f"{'[DRY RUN] ' if DRY_RUN else ''}Starting generic image cleanup...\n")

    # ─────────────────────────────────────────────────────────
    # 1. Find vehicles where Unsplash is PRIMARY but S3 exists
    # ─────────────────────────────────────────────────────────
    cur.execute("""
        SELECT DISTINCT vi."VehicleId",
               v."Make", v."Model", v."Year",
               vi."Id" as unsplash_primary_id,
               vi."Url" as unsplash_primary_url
        FROM vehicle_images vi
        JOIN vehicles v ON vi."VehicleId" = v."Id"
        WHERE v."Status" = 'Active' AND v."IsDeleted" = false
          AND vi."IsPrimary" = true
          AND vi."Url" LIKE '%unsplash%'
          AND vi."VehicleId" IN (
              SELECT DISTINCT vi2."VehicleId" FROM vehicle_images vi2
              WHERE vi2."Url" LIKE '%amazonaws.com%'
              AND vi2."Url" NOT LIKE '%unsplash%'
          )
        ORDER BY v."Make", v."Model"
    """)
    unsplash_primary_vehicles = cur.fetchall()
    print(f"Vehicles with Unsplash PRIMARY + S3 images: {len(unsplash_primary_vehicles)}")
    for row in unsplash_primary_vehicles:
        vid, make, model, year, uid, url = row
        photo_id = url.split("photo-")[1].split("?")[0] if "photo-" in url else "unknown"
        print(f"  {year} {make} {model} — photo-{photo_id}")

    print()

    # ─────────────────────────────────────────────────────────
    # 2. For each: promote first S3 image as primary, demote Unsplash primary
    # ─────────────────────────────────────────────────────────
    promoted = 0
    for row in unsplash_primary_vehicles:
        vid, make, model, year, unsplash_primary_id, _ = row

        # Find first S3 image
        cur.execute("""
            SELECT "Id", "Url" FROM vehicle_images
            WHERE "VehicleId" = %s
              AND "Url" LIKE '%%amazonaws.com%%'
              AND "Url" NOT LIKE '%%unsplash%%'
            ORDER BY "CreatedAt" ASC
            LIMIT 1
        """, (vid,))
        s3_row = cur.fetchone()
        if not s3_row:
            print(f"  WARNING: No S3 image found for {year} {make} {model}, skipping")
            continue

        s3_id, s3_url = s3_row
        if not DRY_RUN:
            # Clear all primaries for this vehicle first
            cur.execute('UPDATE vehicle_images SET "IsPrimary"=false WHERE "VehicleId"=%s', (vid,))
            # Set S3 image as primary
            cur.execute('UPDATE vehicle_images SET "IsPrimary"=true WHERE "Id"=%s', (s3_id,))
        promoted += 1
        print(f"  PROMOTE S3 primary: {year} {make} {model}")
        print(f"    Old: {_[:70]}")
        print(f"    New: {s3_url[:70]}")

    print(f"\nPromoted S3 to primary: {promoted}")

    # ─────────────────────────────────────────────────────────
    # 3. Delete ALL non-primary Unsplash images from S3+Unsplash vehicles
    # ─────────────────────────────────────────────────────────
    cur.execute("""
        SELECT vi."Id", vi."VehicleId", v."Make", v."Model", v."Year", vi."Url"
        FROM vehicle_images vi
        JOIN vehicles v ON vi."VehicleId" = v."Id"
        WHERE v."Status" = 'Active' AND v."IsDeleted" = false
          AND vi."IsPrimary" = false
          AND vi."Url" LIKE '%unsplash%'
          AND vi."VehicleId" IN (
              SELECT DISTINCT vi2."VehicleId" FROM vehicle_images vi2
              WHERE vi2."Url" LIKE '%amazonaws.com%'
              AND vi2."Url" NOT LIKE '%unsplash%'
          )
        ORDER BY v."Make", v."Model"
    """)
    secondary_unsplash = cur.fetchall()
    print(f"\nSecondary Unsplash images to delete (in vehicles with S3): {len(secondary_unsplash)}")

    ids_to_delete = [str(r[0]) for r in secondary_unsplash]
    if ids_to_delete and not DRY_RUN:
        cur.execute('DELETE FROM vehicle_images WHERE "Id" = ANY(%s::uuid[])', (ids_to_delete,))

    # Print sample
    by_vehicle = {}
    for row in secondary_unsplash:
        vid = row[1]
        label = f"{row[4]} {row[2]} {row[3]}"
        by_vehicle.setdefault(label, []).append(row[5])

    for vehicle, urls in list(by_vehicle.items())[:10]:
        print(f"  DELETE {len(urls)} Unsplash secondary: {vehicle}")

    if len(by_vehicle) > 10:
        print(f"  ... and {len(by_vehicle)-10} more vehicles")

    # ─────────────────────────────────────────────────────────
    # 4. Also remove remaining primary Unsplash images that were demoted above
    #    (now they have IsPrimary=false after step 2 ran)
    # ─────────────────────────────────────────────────────────
    unsplash_demoted_ids = [str(r[4]) for r in unsplash_primary_vehicles]
    if unsplash_demoted_ids and not DRY_RUN:
        cur.execute('DELETE FROM vehicle_images WHERE "Id" = ANY(%s::uuid[])', (unsplash_demoted_ids,))
        print(f"\nDeleted {len(unsplash_demoted_ids)} demoted Unsplash primary images")
    else:
        print(f"\nWould delete {len(unsplash_demoted_ids)} demoted Unsplash primary images")

    # ─────────────────────────────────────────────────────────
    # 5. Final stats
    # ─────────────────────────────────────────────────────────
    if not DRY_RUN:
        conn.commit()
        print("\n✅ Commit successful")
    else:
        conn.rollback()
        print("\n[DRY RUN] No changes committed")

    cur.execute('SELECT COUNT(*) FROM vehicle_images vi JOIN vehicles v ON vi."VehicleId"=v."Id" WHERE v."Status"=\'Active\'')
    total = cur.fetchone()[0]
    cur.execute('SELECT COUNT(*) FROM vehicle_images vi JOIN vehicles v ON vi."VehicleId"=v."Id" WHERE v."Status"=\'Active\' AND vi."Url" LIKE \'%unsplash%\'')
    unsplash_remaining = cur.fetchone()[0]
    cur.execute('SELECT COUNT(*) FROM vehicle_images vi JOIN vehicles v ON vi."VehicleId"=v."Id" WHERE v."Status"=\'Active\' AND vi."Url" LIKE \'%amazonaws.com%\'')
    s3_remaining = cur.fetchone()[0]

    print(f"\nFinal image counts:")
    print(f"  Total images: {total}")
    print(f"  S3 images: {s3_remaining}")
    print(f"  Unsplash remaining: {unsplash_remaining}")

    cur.close()
    conn.close()

if __name__ == "__main__":
    main()
