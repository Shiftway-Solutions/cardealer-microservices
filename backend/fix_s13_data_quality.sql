-- S13: Calidad de Datos - Fix anomalias visibles en listados
-- Fecha: 2026-03-31 (Intento 1) + 2026-04-01 (Intento 2) + 2026-04-01 (Intento 3 AUDIT)
-- Bugs:
--   1. Imagen de perro (beagle) en Kia Sportage LX (photo-1543466835-00a7907e9de1)
--   2. BodyStyle=Sedan incorrecto en Kia Sportage (debe ser SUV)
--   3. BodyStyle=Sedan incorrecto en Hyundai Tucson (debe ser SUV)
--   4. Imagen de paisaje de montana en Hyundai Tucson primary (photo-1519641471654-76ce0107ad1b)
--   5. Imagen primaria rota 404 en Nissan Sentra (photo-1552519507-da3b142a6e3d)
--   6. MileageUnit=Miles incorrecto para todos los vehiculos (RD usa km, no millas)
--   7. Description typos: "al dia" -> "al dia" (acentos), "dueno" -> "dueno" (tilde n)
--   8. FeaturesJson: "Camara de reversa" -> "Camara de reversa" (acento faltante)
--   9. Transmission=Manual para todos los vehiculos (4 de 5 deben ser Automatic)

BEGIN;

-- Bug 1: Reemplazar imagen de perro beagle en Kia Sportage
-- photo-1543466835-00a7907e9de1 es un beagle / perro (error de datos quality)
-- Reemplazar con photo-1549317661-bd32c8ce0db2 (SUV validado, HTTP 200)
UPDATE vehicle_images
SET "Url" = 'https://images.unsplash.com/photo-1549317661-bd32c8ce0db2?w=800',
    "ThumbnailUrl" = 'https://images.unsplash.com/photo-1549317661-bd32c8ce0db2?w=400'
WHERE "Url" = 'https://images.unsplash.com/photo-1543466835-00a7907e9de1?w=800';

-- Bug 2: Kia Sportage BodyStyle Sedan -> SUV
UPDATE vehicles
SET "BodyStyle" = 'SUV'
WHERE "Make" = 'Kia' AND "Model" = 'Sportage' AND "IsDeleted" = false;

-- Bug 3: Hyundai Tucson BodyStyle Sedan -> SUV
UPDATE vehicles
SET "BodyStyle" = 'SUV'
WHERE "Make" = 'Hyundai' AND "Model" = 'Tucson' AND "IsDeleted" = false;

-- Bug 4: Reemplazar imagen de paisaje de montana en Hyundai Tucson
-- photo-1519641471654-76ce0107ad1b es un paisaje nevado (no un auto)
-- Reemplazar con photo-1560958089-b8a1929cea89 (auto generico validado, HTTP 200)
UPDATE vehicle_images
SET "Url" = 'https://images.unsplash.com/photo-1560958089-b8a1929cea89?w=800',
    "ThumbnailUrl" = 'https://images.unsplash.com/photo-1560958089-b8a1929cea89?w=400'
WHERE "Url" = 'https://images.unsplash.com/photo-1519641471654-76ce0107ad1b?w=800';

-- Bug 5: Imagen primaria rota (404) en Nissan Sentra
-- photo-1552519507-da3b142a6e3d retorna 404 via Next.js image proxy
-- Cambiar a photo-1492144534655-ae79c964c9d7 (auto oscuro/gris - validado)
UPDATE vehicle_images
SET "IsPrimary" = false, "SortOrder" = 99
WHERE "Url" = 'https://images.unsplash.com/photo-1552519507-da3b142a6e3d?w=800'
  AND "IsPrimary" = true;

UPDATE vehicle_images
SET "IsPrimary" = true, "SortOrder" = 0
WHERE "Url" = 'https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?w=800';

COMMIT;

-- Bug 6 (Intento 2, 2026-04-01): MileageUnit=Miles incorrecto — RD usa kilometros
-- Todos los vehiculos del seed tienen MileageUnit='Miles' pero el mercado RD usa km
-- Los valores de mileage (62000, 42000, 35000, 18000, 28500) son kilometros reales
UPDATE vehicles
SET "MileageUnit" = 'Kilometers'
WHERE "MileageUnit" = 'Miles' AND "IsDeleted" = false;

COMMIT;

-- Bug 7 (Intento 3, 2026-04-01): Descriptions con tildes faltantes
-- "al dia" -> "al día", "dueno" / "Dueno" -> "dueño" / "Dueño"
-- Visible en pagina de detalle de vehiculo (descripcion)
UPDATE vehicles
SET "Description" = REPLACE(REPLACE("Description", 'al dia', 'al día'), 'dueno', 'dueño')
WHERE "IsDeleted" = false;

-- Bug 8 (Intento 3, 2026-04-01): FeaturesJson "Camara de reversa" missing tilde
-- Visible en tab Caracteristicas de la pagina de detalle
UPDATE vehicles
SET "FeaturesJson" = REPLACE("FeaturesJson"::text, 'Camara de reversa', 'Cámara de reversa')::jsonb
WHERE "FeaturesJson"::text LIKE '%Camara de reversa%' AND "IsDeleted" = false;

-- Bug 9 (Intento 3, 2026-04-01): Todos los vehiculos tenian Transmission=Manual
-- Mercado RD es mayormente automaticos. Dejar Nissan Sentra como Manual; el resto Automatic
UPDATE vehicles
SET "Transmission" = 'Automatic'
WHERE "Title" IN ('2023 Kia Sportage LX', '2021 Hyundai Tucson Sport', '2023 Toyota Corolla LE - Impecable', '2022 Honda Civic EX')
  AND "IsDeleted" = false;

-- Verificacion
SELECT v."Title", vi."Url", vi."IsPrimary", v."BodyStyle", v."MileageUnit", v."Transmission",
       LEFT(v."Description", 60) AS "DescPreview",
       LEFT(v."FeaturesJson"::text, 80) AS "FeaturesPreview"
FROM vehicle_images vi
JOIN vehicles v ON vi."VehicleId" = v."Id"
WHERE v."IsDeleted" = false AND vi."IsPrimary" = true
ORDER BY v."Title";
