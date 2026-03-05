# 🚗 Guía: Catálogo de Vehículos para CarDealer

## Fuentes de Datos de Vehículos

### 1. **NHTSA vPIC API** (Recomendada - GRATIS)

La API oficial del gobierno de EE.UU. con datos de todos los vehículos:

| Endpoint | Descripción | URL |
|----------|-------------|-----|
| GetAllMakes | Todas las marcas | https://vpic.nhtsa.dot.gov/api/vehicles/GetAllMakes?format=json |
| GetModelsForMake | Modelos por marca | https://vpic.nhtsa.dot.gov/api/vehicles/GetModelsForMake/toyota?format=json |
| GetModelsForMakeYear | Modelos por marca/año | https://vpic.nhtsa.dot.gov/api/vehicles/GetModelsForMakeYear/make/honda/modelyear/2024?format=json |
| DecodeVin | Decodificar VIN | https://vpic.nhtsa.dot.gov/api/vehicles/DecodeVin/1HGBH41JXMN109186?format=json |

**Documentación:** https://vpic.nhtsa.dot.gov/api/

---

### 2. **Kaggle Datasets** (Datos históricos - GRATIS)

| Dataset | Registros | Link |
|---------|-----------|------|
| US Cars Dataset | 426K+ | https://www.kaggle.com/datasets/austinreese/craigslist-carstrucks-data |
| Car Features & MSRP | 11K+ | https://www.kaggle.com/datasets/CooperUnion/cardataset |
| Vehicle Dataset | 8K+ | https://www.kaggle.com/datasets/nehalbirla/vehicle-dataset-from-cardekho |

**Cómo usar:**
1. Crear cuenta en https://www.kaggle.com
2. Descargar el dataset (botón "Download")
3. Extraer CSV y procesar con el script

---

### 3. **Back4App Vehicle Database** (API lista - GRATIS con límites)

Base de datos con 15,000+ vehículos lista para consumir:

**Registrarse:** https://www.back4app.com/database/back4app/car-make-model-database

```bash
# Ejemplo de uso
curl "https://parseapi.back4app.com/classes/Car_Model_List?limit=100" \
  -H "X-Parse-Application-Id: YOUR_APP_ID" \
  -H "X-Parse-REST-API-Key: YOUR_REST_API_KEY"
```

---

## 🚀 Uso del Script de Seed

### Requisitos

```bash
# Python 3.8+
pip install requests
```

### Comandos

```bash
cd /Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices

# 1. Descargar catálogo de NHTSA (marcas principales, ~5 min)
python scripts/seed-vehicle-catalog.py --download

# 2. Descargar TODO (800+ marcas, ~30-60 min)
python scripts/seed-vehicle-catalog.py --download --full

# 3. Generar SQL para insertar en PostgreSQL
python scripts/seed-vehicle-catalog.py --sql

# 4. Generar tipos TypeScript para frontend
python scripts/seed-vehicle-catalog.py --types

# 5. Todo junto
python scripts/seed-vehicle-catalog.py --all
```

### Archivos Generados

```
scripts/vehicle-data/processed/
├── vehicle_catalog_nhtsa.json   # Catálogo completo (JSON)
├── vehicle_catalog_seed.sql     # Script SQL para PostgreSQL
└── vehicle-types.ts             # Tipos TypeScript
```

---

## 📊 Estructura de Base de Datos

```sql
-- Marcas (Toyota, Honda, Ford, etc.)
CREATE TABLE vehicle_makes (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    slug VARCHAR(100) NOT NULL UNIQUE,
    logo_url VARCHAR(500),
    country VARCHAR(100),
    is_active BOOLEAN DEFAULT true
);

-- Modelos (Camry, Civic, F-150, etc.)
CREATE TABLE vehicle_models (
    id SERIAL PRIMARY KEY,
    make_id INTEGER REFERENCES vehicle_makes(id),
    name VARCHAR(100) NOT NULL,
    slug VARCHAR(100) NOT NULL,
    body_type VARCHAR(50)  -- sedan, suv, truck, etc.
);

-- Años disponibles por modelo
CREATE TABLE vehicle_years (
    id SERIAL PRIMARY KEY,
    model_id INTEGER REFERENCES vehicle_models(id),
    year INTEGER NOT NULL
);

-- Trims/Versiones (LE, XLE, Limited, etc.)
CREATE TABLE vehicle_trims (
    id SERIAL PRIMARY KEY,
    year_id INTEGER REFERENCES vehicle_years(id),
    name VARCHAR(100) NOT NULL,
    engine VARCHAR(100),
    transmission VARCHAR(100),
    drivetrain VARCHAR(50),     -- FWD, AWD, RWD, 4WD
    fuel_type VARCHAR(50),      -- gasoline, diesel, electric, hybrid
    mpg_city INTEGER,
    mpg_highway INTEGER,
    horsepower INTEGER,
    torque INTEGER,
    msrp DECIMAL(10,2)
);
```

---

## 🔄 Integración con VehiclesSaleService

### 1. Ejecutar SQL en VehiclesSaleService DB

```bash
# Conectar a la base de datos de VehiclesSaleService
docker exec -it vehiclessaleservice-db psql -U postgres -d vehiclessaleservice

# Ejecutar el script SQL generado
\i /path/to/vehicle_catalog_vehiclessale.sql
```

### 2. API Endpoints Disponibles

```csharp
// GET /api/catalog/makes              - Todas las marcas
// GET /api/catalog/makes/popular      - Marcas populares
// GET /api/catalog/makes/search       - Buscar marcas
// GET /api/catalog/makes/{slug}/models - Modelos por marca
// GET /api/catalog/models/{id}/years  - Años disponibles
// GET /api/catalog/models/{id}/years/{year}/trims - Trims con specs
// GET /api/catalog/trims/{id}         - Detalle de trim (auto-fill)
// GET /api/catalog/stats              - Estadísticas del catálogo
```

### 3. Frontend: Formulario de Publicación

```typescript
// Flujo de selección
1. Usuario selecciona Marca → Carga modelos
2. Usuario selecciona Modelo → Carga años
3. Usuario selecciona Año → Carga trims (opcional)
4. Sistema auto-completa especificaciones
```

---

## 📈 Datos Disponibles

| Métrica | Cantidad Aproximada |
|---------|---------------------|
| Marcas principales | 40+ |
| Todas las marcas | 800+ |
| Modelos (principales) | 2,000+ |
| Modelos (total) | 15,000+ |
| Años cubiertos | 1995-2030 |

---

## 🔍 Ejemplo: Decodificar VIN

El VIN contiene información completa del vehículo:

```bash
# VIN de ejemplo: 1HGBH41JXMN109186
curl "https://vpic.nhtsa.dot.gov/api/vehicles/DecodeVin/1HGBH41JXMN109186?format=json"
```

Retorna:
- Make: Honda
- Model: Civic
- Year: 2021
- Body Class: Sedan
- Engine: 2.0L I4
- Fuel Type: Gasoline
- Transmission: CVT
- Drive Type: FWD
- Y más...

---

## 💡 Tips

1. **Para desarrollo:** Usa `--download` (solo marcas principales, rápido)
2. **Para producción:** Usa `--download --full` (todas las marcas)
3. **Actualizar catálogo:** Ejecuta mensualmente para nuevos modelos
4. **VIN lookup:** Usa DecodeVin para auto-completar información
