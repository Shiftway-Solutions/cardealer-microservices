-- Insert 10th Minivan: 2023 Dodge Grand Caravan GT
-- Uses same DealerId/SellerId pattern as existing Minivans
DO $$
DECLARE
  new_id UUID := gen_random_uuid();
  img_id UUID := gen_random_uuid();
BEGIN

  INSERT INTO vehicles (
    "Id", "DealerId", "SellerId",
    "Title", "Make", "Model", "Trim", "Year",
    "VehicleType", "BodyStyle", "Doors", "Seats",
    "FuelType", "EngineSize", "Horsepower", "Cylinders",
    "Transmission", "DriveType",
    "Mileage", "MileageUnit", "Condition",
    "Price", "Currency",
    "ExteriorColor", "InteriorColor",
    "Description",
    "City", "State", "Country",
    "SellerName", "SellerType", "SellerPhone", "SellerEmail", "SellerWhatsApp",
    "SellerVerified",
    "Status", "IsFeatured", "IsPremium", "IsCertified",
    "AccidentHistory", "HasCleanTitle", "OdometerRollbackDetected", "HasBrokenImages",
    "ViewCount", "FavoriteCount", "InquiryCount", "FraudScore",
    "RejectionCount", "ReportCount", "HomepageSections", "FeaturedPriority",
    "FeaturesJson", "PackagesJson",
    "ConcurrencyStamp",
    "PublishedAt", "ApprovedAt", "DisclaimerAcceptedAt", "DisclaimerTosVersion",
    "CreatedAt", "UpdatedAt", "IsDeleted"
  ) VALUES (
    new_id,
    '00000000-0000-0000-0000-000000000000',
    'f3aaadc5-d6ab-4992-9e48-e74454fb6ca2',
    '2023 Dodge Grand Caravan GT',
    'Dodge', 'Grand Caravan', 'GT', 2023,
    'Car', 'Minivan', 4, 7,
    'Gasoline', '3.6', 283, 6,
    'Automatic', 'FWD',
    22450, 'Kilometers', 'Used',
    1600000, 'DOP',
    'Blanco Perla', 'Negro',
    'Dodge Grand Caravan GT 2023 en excelente estado. Motor 3.6L V6 Pentastar 283 HP. Transmisión automática de 6 velocidades. Aire acondicionado dual de zona, pantalla táctil de 7 pulgadas, cámara de retroceso, sensores de parqueo delantera y trasera. Bluetooth, Apple CarPlay y Android Auto. Asientos de cuero, tercera fila reclinable. Ideal para familia numerosa. Documentos al día.',
    'Santo Domingo', 'DN', 'DO',
    'OKLA Automotriz', 0, '809-555-0100', 'ventas@okla.com.do', '18095550100',
    false,
    'Active', false, false, false,
    false, true, false, false,
    0, 0, 0, 0,
    0, 0, 0, 0,
    '["Aire Acondicionado Dual", "Cámara de Reversa", "Bluetooth", "Apple CarPlay", "Android Auto", "Sensores de Parqueo", "Asientos de Cuero", "Tercera Fila"]'::jsonb,
    '[]'::jsonb,
    gen_random_uuid()::text,
    NOW(), NOW(), NOW(), '2026.1',
    NOW(), NOW(), false
  );

  INSERT INTO vehicle_images (
    "Id", "DealerId", "VehicleId",
    "Url", "ImageType", "SortOrder", "IsPrimary",
    "ModerationStatus", "CreatedAt"
  ) VALUES (
    img_id,
    '00000000-0000-0000-0000-000000000000',
    new_id,
    'https://images.unsplash.com/photo-1503376780353-7e6692767b70?w=800&q=80',
    0, 0, true,
    0, NOW()
  );

  RAISE NOTICE 'Inserted Minivan #10 — ID: %', new_id;
END $$;

-- Verify count after insert
SELECT "BodyStyle", COUNT(*) as count 
FROM vehicles 
WHERE "Status"='Active' AND "IsDeleted"=false AND "BodyStyle"='Minivan'
GROUP BY "BodyStyle";
