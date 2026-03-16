-- Activate all Draft vehicles that were just created
-- and set their contact info directly in the database

-- First, check the current state of the 11 new vehicles
SELECT id, make, model, year, body_style, fuel_type, status, seller_phone, seller_email
FROM "Vehicles"
WHERE id IN (
    'd96a9083-179f-4f3f-b6e8-5971ad0b5adb',
    'ce3cf724-0984-470b-8a2f-ca715b68aa76',
    'fcbbdf9b-ab67-4607-9952-77af0aca4638',
    'fa0e2c2c-0c34-4316-95fa-59e9e1319089',
    '255d2557-9d10-4e24-b899-a0a0d727416e',
    'ce60c847-6375-4acf-9548-2cfa6c0fec61',
    '17b32483-3e11-4840-b606-f9005fdcf8fd',
    '46ee9b2c-69c5-46b9-a583-6fb411867fda',
    'c568a56d-9ce1-4046-89b8-32c53c490db5',
    'b52a0ec4-e6af-4f17-9411-4f79424cc39c',
    '0ef531fa-7e4f-49e0-a58b-8166b32ba6f1'
)
ORDER BY body_style, make;
