"""Fix Mercedes-Benz Sprinter 2500 primary image."""
import psycopg2

DSN = "host=okla-db-do-user-31493168-0.g.db.ondigitalocean.com port=25060 dbname=vehiclessaleservice user=doadmin sslmode=require password=REDACTED_USE_DB_PASSWORD_ENV"

conn = psycopg2.connect(DSN)
conn.autocommit = False
cur = conn.cursor()

cur.execute("""
    SELECT v."Id" FROM vehicles v
    WHERE v."Make"='Mercedes-Benz' AND v."Model" LIKE '%Sprinter%' AND v."Status"='Active'
""")
sprinter_id = cur.fetchone()[0]
print(f"Sprinter ID: {sprinter_id}")

cur.execute("""
    SELECT "Id", "Url" FROM vehicle_images
    WHERE "VehicleId"=%s AND "Url" LIKE '%%amazonaws.com%%'
    ORDER BY "CreatedAt" ASC LIMIT 1
""", (sprinter_id,))
s3_row = cur.fetchone()
print(f"First S3 image: {s3_row[1][:80]}")

# Clear all primaries for Sprinter
cur.execute('UPDATE vehicle_images SET "IsPrimary"=false WHERE "VehicleId"=%s', (sprinter_id,))
# Promote first S3 as primary
cur.execute('UPDATE vehicle_images SET "IsPrimary"=true WHERE "Id"=%s', (s3_row[0],))
# Delete the wrong Unsplash image (photo-1533473359331 shows SUV, wrong for a Van)
cur.execute("""
    DELETE FROM vehicle_images
    WHERE "VehicleId"=%s AND "Url" LIKE '%%unsplash%%'
""", (sprinter_id,))
deleted = cur.rowcount
print(f"Deleted {deleted} Unsplash image(s) from Sprinter")

conn.commit()
print("Sprinter fix committed")

# Verify
cur.execute("""
    SELECT "IsPrimary", "Url" FROM vehicle_images
    WHERE "VehicleId"=%s ORDER BY "IsPrimary" DESC
""", (sprinter_id,))
rows = cur.fetchall()
print(f"\nSprinter now has {len(rows)} images:")
for r in rows:
    kind = "UNSPLASH" if "unsplash" in r[1] else "S3"
    print(f"  primary={r[0]} | {kind} | {r[1][:90]}")

cur.close()
conn.close()
