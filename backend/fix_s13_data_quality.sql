-- S13: Calidad de Datos - Fix anomalias visibles en listados
-- Fecha: 2026-03-31
-- Bugs:
--   1. Imagen de perro (beagle) en Kia Sportage LX (photo-1543466835-00a7907e9de1)
--   2. BodyStyle=Sedan incorrecto en Kia Sportage (debe ser SUV)
--   3. BodyStyle=Sedan incorrecto en Hyundai Tucson (debe ser SUV)
--   4. Imagen de paisaje de montana en Hyundai Tucson primary (photo-1519641471654-76ce0107ad1b)
--   5. Imagen primaria rota 404 en Nissan Sentra (photo-1552519507-da3b142a6e3d)

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

-- Verificacion
SELECT v."Title", vi."Url", vi."IsPrimary", v."BodyStyle"
FROM vehicle_images vi
JOIN vehicles v ON vi."VehicleId" = v."Id"
WHERE v."IsDeleted" = false AND vi."IsPrimary" = true
ORDER BY v."Title";
