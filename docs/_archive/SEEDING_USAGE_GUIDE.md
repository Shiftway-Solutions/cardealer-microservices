# 🚀 GUÍA DE USO - DATA SEEDING OKLA

**Última actualización:** Enero 15, 2026  
**Estado:** ✅ Listo para usar

---

## 📋 Resumen Rápido

```bash
# Opción 1: Script bash (macOS/Linux)
bash _Scripts/seed-local.sh

# Opción 2: PowerShell (Windows)
.\_Scripts\seed-local.ps1

# Opción 3: C# CLI
cd backend/VehiclesSaleService
dotnet run seed:all

# Opción 4: Limpiar base de datos
psql -h localhost -U postgres -d cardealer -f _Scripts/clean-db.sql
```

---

## 🎯 QUÉ SE GENERA

| Recurso         | Cantidad | Detalles                          |
| --------------- | -------- | --------------------------------- |
| **Usuarios**    | 20       | 10 buyers + 10 sellers            |
| **Dealers**     | 30       | 10 Ind + 8 Chain + 7 Multi + 5 Fr |
| **Vehículos**   | 150      | ~5 por dealer, 10 marcas          |
| **Imágenes**    | 7,500    | 50 por vehículo (Picsum URLs)     |
| **Ubicaciones** | ~75      | 2-3 por dealer                    |

---

## 🏗️ ARQUITECTURA DE LA SOLUCIÓN

### Capas de Seeding

```
┌─────────────────────────────────────────────────────────────┐
│                    DATABASE SEEDING                         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Capa 1: BUILDERS (Generan objetos DTO)                   │
│  ├─ DealerBuilder        → DealerDto (fluent API)          │
│  ├─ VehicleBuilder       → VehicleDto (fluent API)         │
│  └─ ImageBuilder         → VehicleImageDto (fluent API)    │
│                                                             │
│  Capa 2: FAKER INTEGRATION (Datos realistas)               │
│  ├─ Bogus 34.0.2         → Generador de datos              │
│  ├─ Nombres reales       → faker.Person.FirstName()        │
│  └─ Datos variados       → faker.Random.* ()               │
│                                                             │
│  Capa 3: SEEDING SERVICE (Orquestación)                    │
│  ├─ DatabaseSeedingService.cs (fase 1-4)                 │
│  ├─ SeedUsersAsync() → 20 usuarios                        │
│  ├─ SeedDealersAsync() → 30 dealers                       │
│  ├─ SeedVehiclesAsync() → 150 vehículos                   │
│  └─ SeedImagesAsync() → 7,500 referencias                 │
│                                                             │
│  Capa 4: CLI/SCRIPTS (Ejecución)                          │
│  ├─ seed-local.sh        → Bash (macOS/Linux)             │
│  ├─ seed-local.ps1       → PowerShell (Windows)           │
│  ├─ clean-db.sql         → SQL cleanup                     │
│  └─ dotnet CLI           → C# runner                       │
│                                                             │
│  Capa 5: BASE DE DATOS (Persistencia)                      │
│  ├─ PostgreSQL 16+                                         │
│  ├─ Bulk Insert optimizado                                │
│  └─ Índices para queries rápidas                          │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 🔍 ESTRUCTURA DE DIRECTORIOS

```
cardealer-microservices/
├── _Scripts/
│   ├── seed-local.sh          ← Bash script (macOS/Linux)
│   ├── seed-local.ps1         ← PowerShell (Windows)
│   └── clean-db.sql           ← SQL cleanup
│
├── backend/_Shared/
│   └── CarDealer.DataSeeding/
│       ├── DataBuilders/
│       │   ├── DealerBuilder.cs      ← Fluent builder para dealers
│       │   ├── VehicleBuilder.cs     ← Fluent builder para vehículos
│       │   └── ImageBuilder.cs       ← Builder para imágenes
│       │
│       └── Services/
│           └── DatabaseSeedingService.cs  ← Orquestador principal
│
└── docs/
    └── DATA_SEEDING_STRATEGY.md    ← Plan estratégico
```

---

## 🚀 GUÍA PASO A PASO

### Requisito Previo: Ambiente Local Funcionando

```bash
# 1. Clonar repositorio
git clone https://github.com/gregorymorenoiem/cardealer-microservices.git
cd cardealer-microservices

# 2. Levantar Docker Compose
docker-compose up -d
# Esperar ~2 minutos a que todo esté listo

# 3. Verificar health
curl http://localhost:18443/health
# Debe retornar: {"status":"healthy"}

# 4. Verificar PostgreSQL
PGPASSWORD=postgres psql -h localhost -U postgres -d cardealer -c "SELECT 1"
# Debe retornar: (1 row)
```

### Opción 1: Bash Script (macOS/Linux)

```bash
# Hacer script ejecutable
chmod +x _Scripts/seed-local.sh

# Ejecutar
_Scripts/seed-local.sh

# Output esperado:
# ✓ API disponible en http://localhost:18443
# ✓ PostgreSQL disponible
# ✓ 20 usuarios creados
# ...
# ✅ Seeding completado!
```

### Opción 2: PowerShell (Windows)

```powershell
# Permitir ejecución de scripts (si es necesario)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Ejecutar
.\_Scripts\seed-local.ps1

# Output esperado:
# ✓ API disponible...
# ✓ 20 usuarios creados
# ...
```

### Opción 3: C# Seeding Service (Recomendado)

```csharp
// En VehiclesSaleService.Api/Program.cs
var app = builder.Build();

// Agregar durante startup
if (app.Environment.IsDevelopment())
{
    var seeding = app.Services.GetRequiredService<DatabaseSeedingService>();
    await seeding.SeedAllAsync();
}

app.Run();
```

Luego ejecutar:

```bash
cd backend/VehiclesSaleService/VehiclesSaleService.Api
dotnet run

# Verás logs como:
# 🌱 ========== INICIANDO SEEDING COMPLETO ==========
# 📝 Fase 1/4: Creando usuarios...
# ✓ 20 usuarios creados
# 🏪 Fase 2/4: Creando dealers...
# ✓ 30 dealers creados
# 🚗 Fase 3/4: Creando vehículos...
# ✓ 150 vehículos creados
# 🖼️ Fase 4/4: Creando referencias de imágenes...
# ✓ 7,500 referencias de imágenes creadas
# ✅ SEEDING COMPLETADO EXITOSAMENTE
```

### Opción 4: Limpiar Base de Datos

```bash
# Limpiar todos los datos
psql -h localhost -U postgres -d cardealer -f _Scripts/clean-db.sql

# Output:
# ✅ Base de datos limpiada exitosamente!
```

---

## 📊 VALIDAR DATOS DESPUÉS DE SEEDING

### Via SQL

```sql
-- Conectarse a la BD
psql -h localhost -U postgres -d cardealer

-- Verificar conteos
SELECT
    'users' as tabla, COUNT(*) as registros FROM users
UNION ALL
SELECT 'dealers', COUNT(*) FROM dealers
UNION ALL
SELECT 'vehicles', COUNT(*) FROM vehicles
UNION ALL
SELECT 'vehicle_images', COUNT(*) FROM vehicle_images;

-- Resultado esperado:
-- tabla           | registros
-- ----------------+-----------
-- vehicle_images  | 7500
-- vehicles        | 150
-- dealers         | 30
-- users           | 20

-- Ver distribución de vehículos por dealer
SELECT d.business_name, COUNT(v.id) as vehicle_count
FROM dealers d
LEFT JOIN vehicles v ON d.id = v.dealer_id
GROUP BY d.business_name
ORDER BY vehicle_count DESC
LIMIT 10;

-- Ver imágenes por vehículo (debe ser 50 cada una)
SELECT v.title, COUNT(i.id) as image_count
FROM vehicles v
LEFT JOIN vehicle_images i ON v.id = i.vehicle_id
GROUP BY v.title
LIMIT 10;
```

### Via API

```bash
# Listar vehículos
curl -s http://localhost:18443/api/vehicles | jq '.data | length'
# Debe retornar: 150

# Listar dealers
curl -s http://localhost:18443/api/dealers | jq '.data | length'
# Debe retornar: 30

# Ver detalle de un vehículo
curl -s http://localhost:18443/api/vehicles/{id} | jq '.'
```

---

## 🎨 PERSONALIZACION DE DATOS

### Cambiar cantidad de datos

**VehicleBuilder:**

```csharp
// Por defecto: 150 vehículos
var vehicles = VehicleBuilder.GenerateBatch(
    count: 300,           // ← Cambiar a 300
    dealerIds: dealerIds,
    vehiclesPerDealer: 10 // ← 10 por dealer en lugar de 5
);
```

**DealerBuilder:**

```csharp
// Por defecto: 30 dealers
var dealers = DealerBuilder.GenerateBatch(
    count: 100,          // ← Cambiar a 100
    type: "Mixed"        // ← "Independent", "Chain", etc.
);
```

**ImageBuilder:**

```csharp
// Por defecto: 50 imágenes por vehículo
var images = new ImageBuilder()
    .ForVehicle(vehicleId)
    .WithImageCount(100)  // ← 100 imágenes en lugar de 50
    .Build();
```

### Cambiar distribución de datos

```csharp
// Cambiar proporción de nuevos vs usados
// En VehicleBuilder.GenerateBatch():

var conditionRandom = _faker.Random.Int(0, 9);
if (conditionRandom < 2)        // ← Cambiar 3 a 2 (menos "nuevos")
    builder = builder.AsNew();
else if (conditionRandom < 8)   // ← Cambiar 9 a 8
    builder = builder.AsUsed();
```

---

## ⚡ PERFORMANCE & OPTIMIZACIÓN

### Bulk Insert (Rápido)

```sql
-- Insertar 7,500 imágenes en ~30 segundos
INSERT INTO vehicle_images (id, vehicle_id, url, is_primary, sort_order)
SELECT gen_random_uuid(), vehicle_id, url, is_primary, sort_order
FROM vehicle_images_staging;

-- Crear índices DESPUÉS de insertar (más rápido)
CREATE INDEX idx_vehicle_images_vehicle_id ON vehicle_images(vehicle_id);
CREATE INDEX idx_vehicle_images_primary ON vehicle_images(vehicle_id, is_primary);
```

### Deshabilitar Constraints Temporalmente

```sql
-- Antes de bulk insert
SET session_replication_role = 'replica';

-- ... INSERT aquí ...

-- Después
SET session_replication_role = 'origin';
```

### Timeouts y Conexiones

```bash
# Si hay timeout, aumentar timeout de conexión
PGCONNECT_TIMEOUT=30 psql -h localhost ...

# Para bulk operations grandes, usar UNLOGGED tables
CREATE UNLOGGED TABLE vehicles_staging AS SELECT * FROM vehicles;
```

---

## 🐛 TROUBLESHOOTING

### Error: "API no disponible"

```bash
# Verificar que Docker está corriendo
docker ps | grep cardealer

# Reiniciar servicios
docker-compose restart gateway

# Verificar logs
docker logs cardealer-gateway-1
```

### Error: "PostgreSQL connection refused"

```bash
# Verificar que PostgreSQL está activo
docker ps | grep postgres

# Revisar credenciales en compose.yaml
grep -A 3 "POSTGRES_" compose.yaml

# Reiniciar PostgreSQL
docker-compose restart postgres
```

### Error: "Duplicate key value"

```sql
-- Limpiar datos e intentar de nuevo
_Scripts/clean-db.sql

-- Reset de sequences
ALTER SEQUENCE vehicles_id_seq RESTART WITH 1;
```

### Error: "Foreign key constraint violation"

```sql
-- Deshabilitar FK temporalmente durante seeding
SET session_replication_role = 'replica';
-- ... INSERT ...
SET session_replication_role = 'origin';
```

### Seeding muy lento

```csharp
// Usar transacciones más grandes
using var transaction = await db.Database.BeginTransactionAsync();
try {
    await SeedAllAsync();
    await transaction.CommitAsync();
} catch {
    await transaction.RollbackAsync();
    throw;
}
```

---

## 📚 REFERENCIAS DE CÓDIGO

### DealerBuilder Usage

```csharp
// Crear 1 dealer
var dealer = new DealerBuilder()
    .WithName("Premium Motors RD")
    .AsChain()
    .AsVerified()
    .WithCity("Santo Domingo")
    .Build();

// Crear 30 dealers variados
var dealers = DealerBuilder.GenerateBatch(30, type: "Mixed");
```

### VehicleBuilder Usage

```csharp
// Crear 1 vehículo
var vehicle = new VehicleBuilder()
    .WithMake("Toyota")
    .WithModel("Corolla")
    .WithYear(2020)
    .WithPrice(15000)
    .AsUsed()
    .AsFeatured()
    .ForDealer(dealerId)
    .Build();

// Crear 150 vehículos
var vehicles = VehicleBuilder.GenerateBatch(150, dealerIds, 5);
```

### ImageBuilder Usage

```csharp
// Crear 50 imágenes para un vehículo
var images = new ImageBuilder()
    .ForVehicle(vehicleId)
    .WithImageCount(50)
    .Build();

// Crear 7,500 imágenes para todos los vehículos
var allImages = ImageBuilder.GenerateBatchForVehicles(vehicleIds, 50);

// Obtener solo URLs (sin guardar en BD)
var urls = ImageBuilder.GenerateImageUrls(vehicleId, count: 50);
```

---

## ✅ CHECKLIST FINAL

- [ ] Docker Compose está corriendo
- [ ] API responde en http://localhost:18443/health
- [ ] PostgreSQL está accesible
- [ ] Scripts tienen permisos de ejecución
- [ ] Datos limpios (sin seeding previo)
- [ ] Ejecutar seeding script
- [ ] Validar conteos en BD
- [ ] Validar vía API
- [ ] Probar vistas del frontend
- [ ] Probar búsquedas y filtros

---

## 📞 SOPORTE

Si encuentras problemas:

1. **Revisar logs:**

   ```bash
   docker logs cardealer-gateway-1
   docker logs cardealer-postgres-1
   ```

2. **Ejecutar health check:**

   ```bash
   curl http://localhost:18443/health
   ```

3. **Validar BD:**

   ```bash
   psql -h localhost -U postgres -d cardealer -c "SELECT COUNT(*) FROM vehicles;"
   ```

4. **Consultar documentación:**
   - [DATA_SEEDING_STRATEGY.md](DATA_SEEDING_STRATEGY.md)
   - [README.md](../../README.md)

---

**¡Listo! Tu base de datos ahora tiene datos realistas para testing.** ✨

_Última actualización: Enero 15, 2026_
