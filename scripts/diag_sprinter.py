"""Diagnostic: Show all images for Mercedes-Benz Sprinter."""
import psycopg2

DSN = "host=okla-db-do-user-31493168-0.g.db.ondigitalocean.com port=25060 dbname=vehiclessaleservice user=doadmin sslmode=require password=REDACTED_USE_DB_PASSWORD_ENV"

conn = psycopg2.connect(DSN)
cur = conn.cursor()

cur.execute("""
    SELECT v."Id", v."Make", v."Model", v."Year", v."Status"
    FROM vehicles v
    WHERE v."Make"='Mercedes-Benz' AND v."Model" LIKE '%Sprinter%'
""")
vehicles = cur.fetchall()
print(f"Found {len(vehicles)} Sprinter vehicle(s):")

for v in vehicles:
    vid, make, model, year, status = v
    print(f"\n  {year} {make} {model} (Status={status}, ID={vid})")
    cur.execute("""
        SELECT "Id", "IsPrimary", "Url", "ThumbnailUrl"
        FROM vehicle_images
        WHERE "VehicleId"=%s
        ORDER BY "IsPrimary" DESC, "CreatedAt" ASC
    """, (vid,))
    images = cur.fetchall()
    if not images:
        print("    NO IMAGES FOUND")
    for img in images:
        iid, is_primary, url, thumb = img
        kind = "UNSPLASH" if "unsplash" in (url or "") else ("S3" if "amazonaws" in (url or "") else "OTHER")
        print(f"    primary={is_primary} | {kind} | {(url or 'NULL')[:90]}")

# Also show total summary
cur.execute("""
    SELECT COUNT(*), SUM(CASE WHEN "Url" LIKE '%unsplash%' THEN 1 ELSE 0 END),
           SUM(CASE WHEN "Url" LIKE '%amazonaws.com%' THEN 1 ELSE 0 END)
    FROM vehicle_images vi
    JOIN vehicles v ON vi."VehicleId"=v."Id"
    WHERE v."Status"='Active'
""")
total, unsplash, s3 = cur.fetchone()
print(f"\nGlobal totals: {total} images | {s3} S3 | {unsplash} Unsplash")

cur.close()
conn.close()
