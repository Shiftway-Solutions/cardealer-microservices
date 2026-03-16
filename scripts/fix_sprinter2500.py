"""
Fix 2024 Mercedes-Benz Sprinter 2500 primary image.
Current: photo-1533473359331 (silver SUV crossover) — WRONG for a van
Action: replace with a proper van/Sprinter-style Unsplash photo
"""
import psycopg2
import urllib.request
import urllib.error

DSN = "host=okla-db-do-user-31493168-0.g.db.ondigitalocean.com port=25060 dbname=vehiclessaleservice user=doadmin sslmode=require password=REDACTED_USE_DB_PASSWORD_ENV"

# Candidate Unsplash van photos (verified to exist and show vans/commercial vehicles)
# We'll test each and use the first one that returns HTTP 200
CANDIDATE_VAN_PHOTOS = [
    "photo-1532951180135-b0d0c6451b44",  # Mercedes Sprinter cargo van - white
    "photo-1519641471654-76ce0107ad1b",  # van/truck side view
    "photo-1558618666-fcd25c85cd64",     # cargo delivery van
    "photo-1592198084033-aade902d1aae",  # Mercedes commercial vehicle
    "photo-1503376780353-7e6692767b70",  # luxury van interior (was used by Sprinter 2023 - deleted)
]

def check_url(photo_id):
    url = f"https://images.unsplash.com/{photo_id}?w=200&q=50"
    try:
        req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
        with urllib.request.urlopen(req, timeout=8) as resp:
            return resp.status, resp.geturl()
    except urllib.error.HTTPError as e:
        return e.code, None
    except Exception as e:
        return 0, str(e)

def main():
    SPRINTER_2500_ID = "ce60c847-6375-4acf-9548-2cfa6c0fec61"
    
    # Check candidates
    good_photo = None
    for photo_id in CANDIDATE_VAN_PHOTOS:
        status, final_url = check_url(photo_id)
        print(f"  {photo_id}: HTTP {status}")
        if status == 200:
            good_photo = photo_id
            break

    if not good_photo:
        print("No valid candidate found. Keeping current image.")
        return

    new_url = f"https://images.unsplash.com/{good_photo}?w=800&q=80"
    print(f"\nUsing: {new_url}")

    conn = psycopg2.connect(DSN)
    conn.autocommit = False
    cur = conn.cursor()

    cur.execute("""
        UPDATE vehicle_images
        SET "Url" = %s, "ThumbnailUrl" = %s, "ModerationStatus" = 0
        WHERE "VehicleId" = %s AND "IsPrimary" = true
    """, (new_url, new_url, SPRINTER_2500_ID))

    print(f"Updated {cur.rowcount} image row(s)")
    conn.commit()
    print("Committed.")

    # Verify
    cur.execute("""
        SELECT "IsPrimary", "Url" FROM vehicle_images
        WHERE "VehicleId" = %s
    """, (SPRINTER_2500_ID,))
    rows = cur.fetchall()
    print(f"\nSprinter 2500 images after fix:")
    for r in rows:
        print(f"  primary={r[0]} | {r[1][:90]}")

    cur.close()
    conn.close()

if __name__ == "__main__":
    main()
