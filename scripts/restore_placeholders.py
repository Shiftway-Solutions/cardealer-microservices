"""
Restore placeholder Unsplash images for 3 vehicles left without any images
after S3 pre-signed URL cleanup.

Vehicles:
- 2024 Ford Mustang (SportsCar)   — e6ab39c8-65b3-4c0e-a330-f7b9deef64cd
- 2024 Hyundai Tucson (SUV)       — f034711f-0133-47e4-8104-470eb5046184
- 2022 Nissan 370Z (SportsCar)    — 3076084a-1cbe-4df5-a02c-daedc6fc423d
"""
import psycopg2
import urllib.request
import urllib.error
import uuid

DSN = "host=okla-db-do-user-31493168-0.g.db.ondigitalocean.com port=25060 dbname=vehiclessaleservice user=doadmin sslmode=require password=REDACTED_USE_DB_PASSWORD_ENV"

# Candidate Unsplash photos per body type (will test each until HTTP 200)
CANDIDATES = {
    # Ford Mustang, Nissan 370Z — SportsCar
    "SportsCar": [
        "photo-1544636331-e26879cd4d9b",  # red sports car
        "photo-1603584173870-7f23fdae1b7a",  # sports sedan -- already in DB but OK
        "photo-1552519507-da3b142c6e3d",  # yellow sports car
        "photo-1494976388531-d1058494cdd8",  # red car driving
    ],
    # Hyundai Tucson — SUV
    "SUV": [
        "photo-1519641471654-76ce0107ad1b",  # white van/SUV
        "photo-1609521263047-f8f205293f24",  # SUV side view
        "photo-1626443252351-c85b5b8b5055",  # SUV front
        "photo-1583121274602-3e2820c69888",  # SUV outdoor
    ],
}

VEHICLES = [
    {
        "id": "e6ab39c8-65b3-4c0e-a330-f7b9deef64cd",
        "label": "2024 Ford Mustang",
        "type": "SportsCar",
    },
    {
        "id": "f034711f-0133-47e4-8104-470eb5046184",
        "label": "2024 Hyundai Tucson",
        "type": "SUV",
    },
    {
        "id": "3076084a-1cbe-4df5-a02c-daedc6fc423d",
        "label": "2022 Nissan 370Z",
        "type": "SportsCar",
    },
]


def check_url(photo_id):
    url = f"https://images.unsplash.com/{photo_id}?w=200&q=50"
    try:
        req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
        with urllib.request.urlopen(req, timeout=8) as resp:
            return resp.status == 200
    except Exception:
        return False


def find_working_photo(candidates):
    for photo_id in candidates:
        print(f"    Testing {photo_id}...", end=" ")
        if check_url(photo_id):
            print("OK")
            return photo_id
        print("FAIL")
    return None


def main():
    conn = psycopg2.connect(DSN)
    conn.autocommit = False
    cur = conn.cursor()

    for v in VEHICLES:
        print(f"\n{v['label']} ({v['id']})")
        photo_id = find_working_photo(CANDIDATES[v["type"]])
        if not photo_id:
            print(f"  No valid photo found for {v['label']} — skipping")
            continue

        url = f"https://images.unsplash.com/{photo_id}?w=800&q=80"
        new_id = str(uuid.uuid4())

        # Get DealerId for this vehicle
        cur.execute('SELECT "DealerId" FROM vehicles WHERE "Id"=%s', (v["id"],))
        dealer_row = cur.fetchone()
        dealer_id = dealer_row[0] if dealer_row else None

        cur.execute("""
            INSERT INTO vehicle_images
              ("Id", "DealerId", "VehicleId", "Url", "ThumbnailUrl", "IsPrimary",
               "ImageType", "SortOrder", "ModerationStatus", "CreatedAt")
            VALUES
              (%s, %s, %s, %s, %s, true, 0, 0, 0, NOW())
        """, (new_id, dealer_id, v["id"], url, url))

        print(f"  Inserted placeholder: {url}")

    conn.commit()
    print("\n✅ Committed placeholders")

    # Final count
    cur.execute("""
        SELECT COUNT(*) FROM vehicle_images vi
        JOIN vehicles v ON vi."VehicleId"=v."Id"
        WHERE v."Status"='Active'
    """)
    row = cur.fetchone()
    total = row[0] if row else 0

    cur.execute("SELECT COUNT(*) FROM vehicles WHERE \"HasBrokenImages\"=true AND \"Status\"='Active'")
    row = cur.fetchone()
    still_broken = row[0] if row else 0

    # Check vehicles with no images
    cur.execute("""
        SELECT COUNT(DISTINCT v."Id") FROM vehicles v
        LEFT JOIN vehicle_images vi ON vi."VehicleId"=v."Id"
        WHERE v."Status"='Active' AND v."IsDeleted"=false
        AND vi."Id" IS NULL
    """)
    row = cur.fetchone()
    no_images = row[0] if row else 0

    print(f"\nFinal: {total} images | HasBrokenImages=true: {still_broken} | Vehicles with 0 images: {no_images}")

    cur.close()
    conn.close()


if __name__ == "__main__":
    main()
