# 📊 ESTRATEGIA PROFESIONAL DE DATA SEEDING - OKLA

**Fecha:** Enero 15, 2026  
**Objetivo:** Llenar base de datos con 150+ vehículos, 30 dealers, 20+ usuarios  
**Enfoque:** Producción-ready, Testing-grade, Performance-optimized

---

## 🎯 PLAN GENERAL (3 FASES)

```
FASE 1: PREPARACIÓN (30 min)
├─ Instalar paquetes NuGet (Bogus, Faker)
├─ Crear Data Builders y Factories
└─ Crear Seeding Service

FASE 2: EJECUCIÓN LOCAL (45 min)
├─ Seed Usuarios (AuthService)
├─ Seed Dealers (DealerManagementService)
├─ Seed Vehículos (VehiclesSaleService)
├─ Seed Imágenes (MediaService con Picsum)
└─ Validar datos con queries

FASE 3: CLEANUP & RESET (15 min)
├─ Script para limpiar BD
├─ Script para reset de sequences
└─ Script para restaurar seed data
```

---

## 📋 DATOS A GENERAR

### USUARIOS (20 total)

| Tipo        | Cantidad | Propósito               |
| ----------- | -------- | ----------------------- |
| **Buyers**  | 10       | Compradores normales    |
| **Sellers** | 10       | Vendedores particulares |

### DEALERS (30 total)

| Tipos             | Cantidad | Estado           | Ubicaciones             |
| ----------------- | -------- | ---------------- | ----------------------- |
| **Independent**   | 10       | Active + Pending | Santo Domingo, Santiago |
| **Chain**         | 8        | Active           | 3-4 sucursales c/uno    |
| **MultipleStore** | 7        | Active           | 2-3 sucursales c/uno    |
| **Franchise**     | 5        | Active + Pending | 1-2 sucursales c/uno    |

### VEHÍCULOS (150 total)

| Distribución      | Cantidad | Marca                              |
| ----------------- | -------- | ---------------------------------- |
| **Toyota** (30%)  | 45       | Corolla, Hilux, Land Cruiser, RAV4 |
| **Hyundai** (20%) | 30       | Elantra, Tucson, Santa Fe          |
| **Nissan** (15%)  | 22       | Sentra, Altima, Pathfinder         |
| **Ford** (15%)    | 22       | Focus, F-150, Ecosport             |
| **Mazda** (10%)   | 15       | Mazda3, CX-5                       |
| **Honda** (10%)   | 16       | Civic, Accord, CR-V                |

### IMÁGENES (50 por vehículo)

- **Total:** 150 vehículos × 50 imágenes = **7,500 imágenes**
- **Fuente:** Picsum Photos (servicio de imágenes realistas)
- **Estructura:**
  - 1 imagen principal (primaria)
  - 49 imágenes adicionales (secundarias)
  - Tamaños: thumbnail, medium, full resolution

---

## 🏗️ ARQUITECTURA DE SEEDING

### Opción 1: Data Builders (RECOMENDADO)

**Ventajas:**

- ✅ Fluent API (fluent, readable)
- ✅ Reutilizable en tests
- ✅ Fácil mantenimiento
- ✅ Compatible con Bogus

**Estructura:**

```csharp
// Builders/DealerBuilder.cs
public class DealerBuilder
{
    private string _businessName = "Test Dealer";
    private string _type = "Independent";

    public DealerBuilder WithName(string name)
    {
        _businessName = name;
        return this;
    }

    public Dealer Build() => new Dealer { /* ... */ };
}

// Usage:
var dealer = new DealerBuilder()
    .WithName("Premium Motors RD")
    .Build();
```

### Opción 2: Factory Methods

**Ventajas:**

- Simple y directo
- Menos boilerplate

### Opción 3: EF Core HasData (DATA ANNOTATIONS)

**Ventajas:**

- Parte del EF Core
- Integrado en migrations

**Desventajas:**

- Difícil mantener datos complejos
- No sirve para relaciones grandes

---

## 🔧 PAQUETES A INSTALAR

```bash
dotnet add package Bogus --version 34.0.2          # Generador de datos fake
dotnet add package Bogus.Extensions.UnitedStates   # Localizaciones
dotnet add package Bogus.Extensions.DomainObjects   # Email/URLs realistas
```

### Alternativas Profesionales

| Paquete           | Propósito                    | Instalación                    |
| ----------------- | ---------------------------- | ------------------------------ |
| **Bogus**         | Generador de datos realistas | `dotnet add package Bogus`     |
| **Nando**         | Generador de datos fluent    | Alternativa (no recomendado)   |
| **Fixture.Xunit** | Para testing                 | `dotnet add package xunit`     |
| **AutoBogus**     | Auto-mapper con Bogus        | `dotnet add package AutoBogus` |
| **Respawn**       | Limpiar BD después tests     | `dotnet add package Respawn`   |

---

## 🗂️ ESTRUCTURA DE ARCHIVOS

```
backend/
├── VehiclesSaleService/
│   └── VehiclesSaleService.Infrastructure/
│       ├── Data/
│       │   ├── DataSeeders/
│       │   │   ├── VehicleDataSeeder.cs          # Genera vehículos
│       │   │   ├── DealerDataSeeder.cs           # Genera dealers
│       │   │   └── ImageDataSeeder.cs            # Genera imágenes
│       │   ├── Builders/
│       │   │   ├── DealerBuilder.cs              # Builder fluent
│       │   │   ├── VehicleBuilder.cs
│       │   │   └── ImageBuilder.cs
│       │   └── Seeds/
│       │       ├── DealersJsonSeed.json          # Data estática
│       │       └── VehiclesJsonSeed.json
│       ├── SeedingService.cs                     # Orquestador
│       └── Persistence/
│           └── ApplicationDbContext.cs           # OnModelCreating()
├── _Scripts/                                      # Scripts CLI
│   ├── seed-local.sh                             # Unix/Mac
│   ├── seed-local.ps1                            # Windows PowerShell
│   ├── seed-docker.sh                            # Inside container
│   └── clean-db.sql                              # SQL cleanup
└── _Documentation/
    └── SEEDING_GUIDE.md                          # README de uso
```

---

## 📡 API ENDPOINTS PARA SEEDING

### UserService (AuthService)

```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "buyer1@okla.com.do",
  "firstName": "Juan",
  "lastName": "Pérez",
  "password": "SecurePass123!",
  "accountType": "Individual"
}

Response: 201 Created
{
  "id": "guid",
  "email": "buyer1@okla.com.do",
  "token": "jwt_token..."
}
```

### DealerManagementService

```http
POST /api/dealers
Authorization: Bearer {token}
Content-Type: application/json

{
  "businessName": "Premium Motors RD",
  "rnc": "1234567890",
  "dealerType": "Independent",
  "email": "contact@premiummotors.do",
  "phone": "+1-809-555-1234",
  "city": "Santo Domingo"
}

Response: 201 Created
{
  "id": "dealer_guid",
  "businessName": "Premium Motors RD",
  "status": "Pending"
}
```

### VehiclesSaleService

```http
POST /api/vehicles
Authorization: Bearer {dealer_token}
Content-Type: application/json

{
  "title": "2020 Toyota Corolla",
  "make": "Toyota",
  "model": "Corolla",
  "year": 2020,
  "price": 15000,
  "mileage": 45000,
  "condition": "Used",
  "bodyStyle": "Sedan",
  "fuelType": "Gasoline",
  "transmission": "Automatic",
  "description": "Excelente condición..."
}

Response: 201 Created
{
  "id": "vehicle_guid",
  "title": "2020 Toyota Corolla",
  "dealerId": "dealer_guid"
}
```

### MediaService (Subir Imágenes)

```http
POST /api/media/upload
Authorization: Bearer {dealer_token}
Content-Type: multipart/form-data

{
  "vehicleId": "vehicle_guid",
  "file": <binary image data>,
  "isPrimary": true,
  "sortOrder": 0
}

Response: 201 Created
{
  "id": "image_guid",
  "url": "https://s3.amazonaws.com/cardealer/images/...",
  "vehicleId": "vehicle_guid"
}
```

---

## 🚀 SCRIPTS DE SEEDING

### Script 1: Data Seeder Service (C#)

Este es el servicio que orquesta todo:

```csharp
// Location: VehiclesSaleService.Infrastructure/Data/SeedingService.cs

public class DatabaseSeedingService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseSeedingService> _logger;

    public async Task SeedAllAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("🌱 Iniciando seeding de datos...");

        // Fase 1: Usuarios
        await SeedUsersAsync(ct);

        // Fase 2: Dealers
        await SeedDealersAsync(ct);

        // Fase 3: Vehículos
        await SeedVehiclesAsync(ct);

        // Fase 4: Imágenes (BULK IMPORT)
        await SeedImagesAsync(ct);

        _logger.LogInformation("✅ Seeding completado!");
    }

    private async Task SeedUsersAsync(CancellationToken ct)
    {
        // 10 buyers + 10 sellers
        var faker = new Faker<UserDto>();
        var users = faker
            .CustomInstantiator(f => new UserDto
            {
                Email = f.Internet.Email("cardealer.do"),
                FirstName = f.Person.FirstName,
                LastName = f.Person.LastName,
                PhoneNumber = f.Phone.PhoneNumber("809-####-####"),
                AccountType = Random.Shared.Next(0, 2) == 0 ? "Individual" : "Seller"
            })
            .Generate(20);

        // Guardar en BD
        foreach (var user in users)
        {
            await _userService.CreateUserAsync(user, ct);
        }
    }

    private async Task SeedDealersAsync(CancellationToken ct)
    {
        // 30 dealers con tipos variados
        var dealerFaker = new Faker<Dealer>();
        // ... implementación
    }

    private async Task SeedVehiclesAsync(CancellationToken ct)
    {
        // 150 vehículos distribuidos
        // ... implementación
    }

    private async Task SeedImagesAsync(CancellationToken ct)
    {
        // 7,500 imágenes desde Picsum Photos
        // Bulk insert en MediaService
        // ... implementación
    }
}
```

### Script 2: CLI de Seeding (Shell Script)

```bash
#!/bin/bash
# Location: _Scripts/seed-local.sh

set -e

API_URL="http://localhost:18443"
SEED_COUNT=30

echo "🔄 Iniciando seeding de datos..."

# 1. Health check
echo "✓ Verificando API..."
curl -s "${API_URL}/health" > /dev/null || exit 1

# 2. Crear usuarios (CLI)
echo "👥 Creando 20 usuarios..."
for i in {1..20}; do
    TYPE=$((i % 2 == 0 ? "Seller" : "Individual"))
    curl -X POST "${API_URL}/api/auth/register" \
        -H "Content-Type: application/json" \
        -d '{
            "email": "user'$i'@okla.local",
            "firstName": "User",
            "lastName": "'$i'",
            "password": "Secure123!",
            "accountType": "'$TYPE'"
        }' &
done
wait

# 3. Crear dealers (30 total)
echo "🏪 Creando 30 dealers..."
# ... similar loop

# 4. Crear vehículos (150 total)
echo "🚗 Creando 150 vehículos..."
# ... similar loop

# 5. Descargar imágenes
echo "🖼️  Descargando 7,500 imágenes de Picsum..."
# ... bulk image import

echo "✅ Seeding completado!"
```

### Script 3: Cleanup SQL

```sql
-- Location: _Scripts/clean-db.sql

-- Borrar datos en orden de dependencias
DELETE FROM vehicle_images;
DELETE FROM vehicles;
DELETE FROM dealers;
DELETE FROM users;
DELETE FROM categories;

-- Reset de sequences/IDs
ALTER SEQUENCE IF EXISTS vehicles_id_seq RESTART WITH 1;
ALTER SEQUENCE IF EXISTS dealers_id_seq RESTART WITH 1;
ALTER SEQUENCE IF EXISTS users_id_seq RESTART WITH 1;

-- Mostrar resumen
SELECT
    'vehicles' as table_name, COUNT(*) as count FROM vehicles
UNION ALL
SELECT 'dealers', COUNT(*) FROM dealers
UNION ALL
SELECT 'users', COUNT(*) FROM users
UNION ALL
SELECT 'vehicle_images', COUNT(*) FROM vehicle_images;
```

---

## 🎨 DATOS REALISTAS CON BOGUS

### Ejemplo: Generar Dealers Realistas

```csharp
var dealerFaker = new Faker<Dealer>()
    .RuleFor(d => d.BusinessName, f => f.Company.CompanyName())
    .RuleFor(d => d.Rnc, f => f.Random.ReplaceNumbers("###########"))
    .RuleFor(d => d.DealerType, f => f.PickRandom(
        DealerType.Independent,
        DealerType.Chain,
        DealerType.MultipleStore))
    .RuleFor(d => d.Email, f => f.Internet.Email())
    .RuleFor(d => d.Phone, f => f.Phone.PhoneNumber("809-####-####"))
    .RuleFor(d => d.Website, f => f.Internet.Url())
    .RuleFor(d => d.EmployeeCount, f => f.Random.Int(1, 500))
    .RuleFor(d => d.EstablishedDate, f => f.Date.PastDateOnly(20))
    .RuleFor(d => d.Status, f => f.PickRandom("Active", "Pending"))
    .RuleFor(d => d.VerificationStatus, f => f.PickRandom(
        "Verified",
        "UnderReview",
        "NotVerified"))
    .RuleFor(d => d.Locations, f => GenerateDealerLocations(f, 2));

var dealers = dealerFaker.Generate(30);
```

### Ejemplo: Generar Vehículos Realistas

```csharp
var makes = new[] { "Toyota", "Honda", "Nissan", "Hyundai", "Ford", "Mazda" };
var models = new Dictionary<string, string[]>
{
    { "Toyota", new[] { "Corolla", "Camry", "Hilux", "Land Cruiser", "RAV4" } },
    { "Honda", new[] { "Civic", "Accord", "CR-V", "Odyssey" } },
    // ...
};

var vehicleFaker = new Faker<Vehicle>()
    .RuleFor(v => v.Make, f => f.PickRandom(makes))
    .RuleFor(v => v.Model, (f, v) => f.PickRandom(models[v.Make]))
    .RuleFor(v => v.Year, f => f.Random.Int(2010, 2024))
    .RuleFor(v => v.Price, f => f.Random.Decimal(10_000, 250_000))
    .RuleFor(v => v.Mileage, f => f.Random.Int(0, 300_000))
    .RuleFor(v => v.Condition, f => f.PickRandom("New", "Used", "Certified"))
    .RuleFor(v => v.BodyStyle, f => f.PickRandom(
        "Sedan", "SUV", "Truck", "Coupe", "Hatchback"))
    .RuleFor(v => v.FuelType, f => f.PickRandom(
        "Gasoline", "Diesel", "Hybrid", "Electric"))
    .RuleFor(v => v.Transmission, f => f.PickRandom(
        "Manual", "Automatic"))
    .RuleFor(v => v.Description, f => f.Lorem.Sentences(5))
    .RuleFor(v => v.DealerId, f => dealerIds[f.Random.Int(0, dealerIds.Count - 1)]);

var vehicles = vehicleFaker.Generate(150);
```

---

## 🖼️ IMÁGENES CON PICSUM PHOTOS

### Estrategia (7,500 imágenes = 150 vehículos × 50 fotos)

**Picsum Photos API:**

```
https://picsum.photos/{width}/{height}?random={id}&blur={level}
```

**Implementación:**

```csharp
public class ImageBulkImporter
{
    private readonly HttpClient _httpClient;
    private const string PICSUM_URL = "https://picsum.photos";

    public async Task ImportImagesForVehicleAsync(
        Guid vehicleId,
        int imageCount = 50,
        CancellationToken ct = default)
    {
        var images = new List<VehicleImage>();

        for (int i = 0; i < imageCount; i++)
        {
            var imageUrl = $"{PICSUM_URL}/800/600?random={vehicleId}-{i}";

            var image = new VehicleImage
            {
                VehicleId = vehicleId,
                Url = imageUrl,
                ThumbnailUrl = $"{PICSUM_URL}/400/300?random={vehicleId}-{i}",
                IsPrimary = i == 0,
                SortOrder = i,
                Caption = i == 0 ? "Imagen Principal" : $"Foto {i + 1}",
                ImageType = ImageType.Exterior,
                CreatedAt = DateTime.UtcNow
            };

            images.Add(image);
        }

        await _imageRepository.BulkInsertAsync(images, ct);
    }
}
```

**Performance (Bulk Insert):**

```sql
-- Inserta 7,500 registros en ~30 segundos
INSERT INTO vehicle_images (id, vehicle_id, url, is_primary, sort_order)
SELECT * FROM (
    SELECT gen_random_uuid(), vehicle_id, url, is_primary, sort_order
    FROM vehicle_images_staging
) AS data;

-- Índices para queries rápidas
CREATE INDEX idx_vehicle_images_vehicle_id_primary
    ON vehicle_images(vehicle_id, is_primary);
CREATE INDEX idx_vehicle_images_sort_order
    ON vehicle_images(vehicle_id, sort_order);
```

---

## ✅ CHECKLIST FINAL

### Pre-Seeding

- [ ] Base de datos vacía (`DROP DATABASE; CREATE DATABASE;`)
- [ ] Migrations ejecutadas (`dotnet ef database update`)
- [ ] Paquetes Bogus instalados
- [ ] Servicios compilando sin errores
- [ ] API respondiendo en healthcheck

### Durante Seeding

- [ ] Usuarios creados (20 total)
  - [ ] 10 buyers (AccountType: Individual)
  - [ ] 10 sellers (AccountType: Seller)
- [ ] Dealers creados (30 total)
  - [ ] 10 Independent
  - [ ] 8 Chain
  - [ ] 7 MultipleStore
  - [ ] 5 Franchise
- [ ] Vehículos creados (150 total)
  - [ ] Distribuidos entre 30 dealers (~5 c/uno)
  - [ ] 45 Toyota
  - [ ] 30 Hyundai
  - [ ] 22 Nissan
  - [ ] 22 Ford
  - [ ] 15 Mazda
  - [ ] 16 Honda
- [ ] Imágenes descargadas (7,500 total)
  - [ ] 50 imágenes por vehículo
  - [ ] 1 imagen primaria
  - [ ] 49 imágenes secundarias

### Post-Seeding

- [ ] Queries de validación ejecutadas
- [ ] No hay datos duplicados
- [ ] Integridad referencial OK
- [ ] Índices creados
- [ ] Performance queries < 100ms

---

## 📊 VALIDACIÓN DE DATOS

```sql
-- Conteo final
SELECT
    (SELECT COUNT(*) FROM users) as total_users,
    (SELECT COUNT(*) FROM dealers) as total_dealers,
    (SELECT COUNT(*) FROM vehicles) as total_vehicles,
    (SELECT COUNT(*) FROM vehicle_images) as total_images;

-- Resultado esperado:
-- total_users: 20
-- total_dealers: 30
-- total_vehicles: 150
-- total_images: 7500

-- Distribución de vehículos
SELECT d.business_name, COUNT(v.id) as vehicle_count
FROM dealers d
LEFT JOIN vehicles v ON d.id = v.dealer_id
GROUP BY d.id, d.business_name
ORDER BY vehicle_count DESC;

-- Imágenes por vehículo
SELECT v.id, v.title, COUNT(i.id) as image_count
FROM vehicles v
LEFT JOIN vehicle_images i ON v.id = i.vehicle_id
GROUP BY v.id, v.title
HAVING COUNT(i.id) != 50;  -- Encontrar errores

-- Usuarios más activos
SELECT u.email, COUNT(v.id) as listings
FROM users u
LEFT JOIN vehicles v ON u.id = v.seller_id
GROUP BY u.id, u.email
ORDER BY listings DESC;
```

---

## 🎓 MEJORES PRÁCTICAS

### 1. Transaction Isolation

```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    await SeedAllAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

### 2. Bulk Operations

```csharp
// ❌ Lento (N queries)
foreach (var vehicle in vehicles)
    await _vehicleRepository.AddAsync(vehicle);

// ✅ Rápido (1 query)
await _vehicleRepository.BulkInsertAsync(vehicles);

// ✅✅ Ultra rápido (Raw SQL)
await _context.Database.ExecuteSqlRawAsync(
    @"INSERT INTO vehicles (id, title, make, model)
      SELECT id, title, make, model FROM vehicles_staging");
```

### 3. Índices Post-Seeding

```sql
-- Crear después de insertar datos (más rápido)
CREATE INDEX idx_vehicles_dealer_id ON vehicles(dealer_id);
CREATE INDEX idx_vehicles_make_model ON vehicles(make, model);
CREATE INDEX idx_images_vehicle_id ON vehicle_images(vehicle_id, is_primary);
```

### 4. Foreign Key Constraints

```csharp
// Desactivar durante bulk insert
SET session_replication_role = 'replica';
-- BULK INSERT HERE
SET session_replication_role = 'origin';
```

---

## 🚀 PRÓXIMOS PASOS

1. **Implementar Data Builders** (1 hora)

   - DealerBuilder.cs
   - VehicleBuilder.cs
   - ImageBuilder.cs

2. **Crear SeedingService** (2 horas)

   - Integración con Bogus
   - Bulk operations
   - Error handling

3. **Implementar CLI Scripts** (1 hora)

   - seed-local.sh
   - clean-db.sql
   - health-check.sh

4. **Testing** (1 hora)
   - Ejecutar seeding localmente
   - Validar integridad de datos
   - Probar APIs con datos reales

---

## 📖 REFERENCIAS

- Bogus Documentation: https://github.com/bchavez/Bogus
- PostgreSQL Bulk Insert: https://www.postgresql.org/docs/current/sql-copy.html
- EF Core Seeding: https://docs.microsoft.com/en-us/ef/core/modeling/data-seeding
- Picsum Photos API: https://picsum.photos

---

_Plan creado por: Gregory Moreno_  
_Última actualización: Enero 15, 2026_
