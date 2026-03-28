-- ============================================================
-- SCRIPT: Fix imágenes incorrectas de vehículos — Sprint 2
-- Base de datos: vehiclessaleservice
-- Fecha: 2026-03-28
-- BUG: RAV4 mostraba Porsche, Hilux mostraba persona, Hiace mostraba montaña
-- ============================================================
\c vehiclessaleservice

-- Toyota RAV4 Hybrid XSE 2023: reemplazar foto de Porsche Panamera
UPDATE vehicle_images SET "Url" = 'https://images.unsplash.com/photo-1533473359331-0135ef1b58bf?w=800&q=75'
WHERE "VehicleId" = (SELECT "Id" FROM vehicles WHERE "Title" LIKE '%RAV4%' LIMIT 1);

-- Toyota Hilux SR5 4x4 2022: reemplazar foto de persona/mecánico
UPDATE vehicle_images SET "Url" = 'https://images.unsplash.com/photo-1568605117036-5fe5e7bab0b7?w=800&q=75'
WHERE "VehicleId" = (SELECT "Id" FROM vehicles WHERE "Title" LIKE '%Hilux%' LIMIT 1);

-- Toyota Hiace Commuter 2022: reemplazar foto de paisaje de montaña
UPDATE vehicle_images SET "Url" = 'https://images.unsplash.com/photo-1549399542-7e3f8b79c341?w=800&q=75'
WHERE "VehicleId" = (SELECT "Id" FROM vehicles WHERE "Title" LIKE '%Hiace%' LIMIT 1);
