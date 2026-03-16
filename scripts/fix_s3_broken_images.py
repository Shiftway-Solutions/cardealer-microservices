#!/usr/bin/env python3
"""
Stage 2 fix:
1. Delete the 8 specific broken S3 image rows
2. Fix Chrysler Voyager: promote its only image to IsPrimary=true
3. Deduplicate image rows (same URL appearing multiple times per vehicle)
4. Ensure exactly 1 IsPrimary=true per vehicle (keep lowest SortOrder)
5. Reset HasBrokenImages=false for the 8 remaining broken vehicles
"""
import subprocess

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

def run(sql: str, label: str = ""):
    r = subprocess.run([PSQL, DB_DSN, "-c", sql],
                       capture_output=True, text=True, env=PSQL_ENV)
    if r.returncode != 0:
        print(f"  ERROR [{label}]: {r.stderr[:200]}")
        return False
    lines = [l for l in r.stdout.strip().splitlines() if l.strip() and not l.startswith("--")]
    if lines:
        print(f"  [{label}]: {lines[-1].strip()}")
    return True

def query(sql: str) -> str:
    r = subprocess.run([PSQL, DB_DSN, "-t", "-A", "-c", sql],
                       capture_output=True, text=True, env=PSQL_ENV)
    return r.stdout.strip()

print("=" * 60)
print("STAGE 2 — S3 broken images + primary/duplicate cleanup")
print("=" * 60)

# ── 1. Delete the 8 specific broken S3 image rows ─────────────────────────
print("\n1. Deleting broken S3 image rows...")
broken_img_ids = [
    "69083150-ea78-49ca-8453-525512e46809",  # Audi A5 — non-primary
    "bcb37219-c14f-474b-9530-f3c473a0ed5d",  # Lexus UX Hybrid — non-primary
    "bb5dff12-4d60-4a74-9371-9a65e76ea2f1",  # Mazda CX-30 — non-primary
    "cf31d407-996d-4f57-b30a-90b998c4773f",  # Toyota GR86 — non-primary
    "0f35e4dc-35d2-4ca2-8d2e-2f35a78aca01",  # VW ID.4 — non-primary
    "b9237e39-9952-4eba-b08a-2f91bdd813fe",  # VW Jetta — non-primary
    "a3789b40-d6e5-4c3d-baeb-92e4e87e90ed",  # Jeep Wrangler — PRIMARY (has other primaries)
    "77ac2932-3e72-428d-b0d4-588718ddf298",  # Kia Forte — PRIMARY (has other primaries)
]
ids_list = "', '".join(broken_img_ids)
run(f"DELETE FROM vehicle_images WHERE \"Id\" IN ('{ids_list}');", "DELETE S3-broken")

# ── 2. Deduplicate images: same VehicleId+Url appearing more than once ────
print("\n2. Deduplicating image rows (same URL per vehicle)...")
run("""
DELETE FROM vehicle_images
WHERE "Id" IN (
    SELECT "Id" FROM (
        SELECT "Id",
               ROW_NUMBER() OVER (
                   PARTITION BY "VehicleId", "Url"
                   ORDER BY "IsPrimary" DESC, "SortOrder" ASC, "CreatedAt" ASC
               ) AS rn
        FROM vehicle_images
    ) t
    WHERE rn > 1
);
""", "DEDUPLICATE URLs")

# ── 3. Fix IsPrimary: ensure exactly 1 primary per vehicle ────────────────
print("\n3. Fixing IsPrimary — set exactly 1 per vehicle...")
# Step 3a: Set all to false
run("""
UPDATE vehicle_images
SET "IsPrimary" = false
WHERE "VehicleId" IN (
    SELECT "VehicleId" FROM vehicle_images
    GROUP BY "VehicleId" HAVING COUNT(*) FILTER (WHERE "IsPrimary" = true) > 1
);
""", "CLEAR multi-primaries")

# Step 3b: Promote the first image (by SortOrder, then CreatedAt) as primary per vehicle
run("""
UPDATE vehicle_images
SET "IsPrimary" = true
WHERE "Id" IN (
    SELECT DISTINCT ON ("VehicleId") "Id"
    FROM vehicle_images
    ORDER BY "VehicleId", "SortOrder" ASC, "CreatedAt" ASC
)
AND "IsPrimary" = false
AND "VehicleId" IN (
    SELECT "VehicleId" FROM vehicle_images
    GROUP BY "VehicleId" HAVING COUNT(*) FILTER (WHERE "IsPrimary" = true) = 0
);
""", "PROMOTE first image as primary")

# ── 4. Fix Chrysler Voyager: its single image must be IsPrimary=true ──────
print("\n4. Chrysler Voyager — promote its only image to IsPrimary=true...")
chrysler_vid = "46ee9b2c-69c5-46b9-a583-6fb411867fda"
run(f"""
UPDATE vehicle_images SET "IsPrimary" = true
WHERE "VehicleId" = '{chrysler_vid}';
""", "CHRYSLER-primary")

# ── 5. Reset HasBrokenImages for the 8 vehicles ───────────────────────────
print("\n5. Resetting HasBrokenImages for 8 S3-fixed vehicles...")
s3_vehicle_ids = [
    "b7a9e07e-1643-4f8d-ba03-948c1292389b",  # Mazda CX-30
    "0c9b0b5d-5494-473c-8b05-b48f93bc73fa",  # Audi A5
    "10f4f0ab-d864-4a31-9191-168d3f84182e",  # Jeep Wrangler
    "71180961-1992-4274-8602-d586e4c6e405",  # VW ID.4
    "068d4dc2-8221-40d0-8827-e71f0eff3237",  # Lexus UX Hybrid
    "ac06d109-1f4d-49a8-9ea7-8b9939665f30",  # Kia Forte
    "8a1673e3-8485-473b-b9af-df68f95ee1ff",  # VW Jetta
    "90e83067-258e-4e31-81ab-c819e2620347",  # Toyota GR86
    chrysler_vid,                              # Chrysler Voyager
]
vids_list = "', '".join(s3_vehicle_ids)
run(f"""
UPDATE vehicles
SET "HasBrokenImages" = false,
    "BrokenImagesDetectedAt" = NULL,
    "UpdatedAt" = NOW()
WHERE "Id" IN ('{vids_list}');
""", "RESET HasBrokenImages")

# ── 6. Final verification ─────────────────────────────────────────────────
print("\n6. Final verification...")
still_broken = query("SELECT COUNT(*) FROM vehicles WHERE \"HasBrokenImages\"=true AND \"Status\"='Active' AND \"IsDeleted\"=false;")
no_images = query("SELECT COUNT(*) FROM vehicles v WHERE v.\"Status\"='Active' AND v.\"IsDeleted\"=false AND NOT EXISTS (SELECT 1 FROM vehicle_images vi WHERE vi.\"VehicleId\"=v.\"Id\");")
no_primary = query("SELECT COUNT(*) FROM vehicles v WHERE v.\"Status\"='Active' AND v.\"IsDeleted\"=false AND NOT EXISTS (SELECT 1 FROM vehicle_images vi WHERE vi.\"VehicleId\"=v.\"Id\" AND vi.\"IsPrimary\"=true) AND EXISTS (SELECT 1 FROM vehicle_images vi2 WHERE vi2.\"VehicleId\"=v.\"Id\");")
multi_primary = query("SELECT COUNT(*) FROM (SELECT \"VehicleId\" FROM vehicle_images WHERE \"IsPrimary\"=true GROUP BY \"VehicleId\" HAVING COUNT(*)>1) t;")
total_images = query("SELECT COUNT(*) FROM vehicle_images vi JOIN vehicles v ON vi.\"VehicleId\"=v.\"Id\" WHERE v.\"Status\"='Active' AND v.\"IsDeleted\"=false;")

print(f"""
  HasBrokenImages=true still : {still_broken}  (target: 0)
  Vehicles with 0 images     : {no_images}      (target: 0)
  Vehicles with 0 primary    : {no_primary}     (target: 0)
  Vehicles with >1 primary   : {multi_primary}  (target: 0)
  Total images remaining     : {total_images}
""")
print("Done.")
