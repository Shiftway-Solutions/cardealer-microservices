# 📋 Informe de Implementación — Homepage Featured Vehicles Enrichment

**Fecha inicio:** 2026-02-23  
**Fecha fin:** 2026-02-23  
**Rama:** `feat/homepage-featured-vehicles-enrichment`  
**PR:** [#4 — feat(homepage): vehicle rotation enrichment + premium properties + campaign sync](https://github.com/gregorymorenoiem/cardealer-microservices/pull/4)  
**Commit:** `e9714a5b`

---

## 🎯 Objetivo

Implementar el flujo completo para que los vehículos con campañas activas de publicidad
aparezcan en la página principal `https://okla.com.do/` con sus badges **⭐ Destacado** y
**💎 Premium**, incluyendo imagen, título, precio y ubicación.

---

## 📊 Estado de GAPs

| GAP    | Descripción                                                            | Prioridad  | Estado      |
| ------ | ---------------------------------------------------------------------- | ---------- | ----------- |
| GAP #1 | RotatedVehicleDto no incluye detalles del vehículo                     | 🔴 CRÍTICO | ✅ RESUELTO |
| GAP #2 | VehicleBasicInfo le faltaban campos + parsing roto                     | 🔴 CRÍTICO | ✅ RESUELTO |
| GAP #3 | Vehicle.cs sin propiedades Premium                                     | 🟡 ALTO    | ✅ RESUELTO |
| GAP #4 | Sin sincronización de eventos AdvertisingService → VehiclesSaleService | 🟡 ALTO    | ✅ RESUELTO |
| E-001  | ApiResponse serializado en PascalCase (frontend recibe undefined)      | 🔴 CRÍTICO | ✅ RESUELTO |
| E-002  | Sin timeout en VehicleServiceClient (homepage se bloqueaba)            | 🟡 ALTO    | ✅ RESUELTO |

---

## 🔧 Cambios implementados

### AdvertisingService

#### 1. `AdvertisingService.Application/Clients/VehicleServiceClient.cs`

**Problema:** `VehicleBasicInfo` no tenía campos de UI, y el cliente intentaba parsear la
respuesta como `ApiResponseWrapper<VehicleBasicInfo>` cuando el endpoint devuelve la entidad
`Vehicle` directamente → siempre retornaba `null`.

**Solución:**

- Clase interna `VehicleRawDto` para deserializar la entidad `Vehicle` crudas
- Mapeo hacia `VehicleBasicInfo` computando campos derivados:
  - `PrimaryImageUrl` ← imagen con `IsPrimary=true` o menor `SortOrder`
  - `Slug` ← `{year}-{make}-{model}-{shortId8}` (igual que VehiclesSaleService slug pattern)
  - `Location` ← `"City, State"` (filtrando nulls)
  - `Currency`, `IsFeatured`, `IsPremium` ← campos directos del Vehicle
- Timeout de 2 segundos por request vía `CancellationTokenSource` linked (E-002)

#### 2. `AdvertisingService.Application/DTOs/HomepageRotationDto.cs`

**Problema:** `Vehicles` se serializaba como `"vehicles"` en JSON pero el frontend espera `"items"`. `Score` se serializaba como `"score"` pero TypeScript espera `"qualityScore"`.

**Solución:**

- `[property: JsonPropertyName("items")]` en `Vehicles`
- `[property: JsonPropertyName("qualityScore")]` en `Score`
- Atributos camelCase explícitos en todos los campos para máxima robustez
- Nuevos campos con sus JsonPropertyName: `title`, `slug`, `imageUrl`, `price`, `currency`, `location`, `isFeatured`, `isPremium`

#### 3. `AdvertisingService.Application/Features/Rotation/.../GetHomepageRotationQueryHandler.cs`

**Problema:** El handler solo retornaba IDs y scores, sin detalles del vehículo.

**Solución:**

- Inyección de `VehicleServiceClient` via constructor
- `Task.WhenAll()` para enriquecer todos los slots en paralelo (cada uno con su propio 2s timeout)
- Fallback inteligente: si `VehicleServiceClient` falla, el slot aparece con `isFeatured`/`isPremium` basado en el contexto de la sección (FeaturedSpot vs PremiumSpot)

#### 4. `AdvertisingService.Api/Program.cs`

**Problema (E-001):** `AddControllers()` sin `JsonOptions` → PascalCase en respuestas → frontend recibía `response.data.Success` en vez de `response.data.success` → todo undefined.

**Solución:**

```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
```

#### 5. `AdvertisingService.Infrastructure/Messaging/Consumers/BillingPaymentCompletedConsumer.cs`

**Adición:** Después de activar una campaña via billing, publica `advertising.campaign.activated` en RabbitMQ exchange `cardealer.events` con routing key correcto. Usa canal separado (`_publishChannel`) para no interferir con el canal de consuming.

#### 6. `AdvertisingService.Infrastructure/BackgroundJobs/CampaignExpirationJob.cs`

**Adición:** Cuando una campaña expira, publica `advertising.campaign.completed` en RabbitMQ. Inyecta `IConnection` (singleton ya registrado en Program.cs) para publicar.

---

### VehiclesSaleService

#### 7. `VehiclesSaleService.Domain/Entities/Vehicle.cs`

**Adición de propiedades premium:**

```csharp
public bool IsPremium { get; set; } = false;
public Guid? LinkedCampaignId { get; set; }
public DateTime? FeaturedUntil { get; set; }
public int FeaturedPriority { get; set; } = 0;
```

**Métodos de dominio:**

- `MarkAsPremium(campaignId, until, priority)` — campaña PremiumSpot activada
- `MarkAsFeaturedByAdmin(priority)` — campaña FeaturedSpot o asignación manual
- `ClearPromotion()` — limpia todos los flags de promoción

#### 8. `VehiclesSaleService.Infrastructure/Migrations/20260223080333_AddVehiclePremiumProperties.cs`

**Nueva migración** — 4 columnas con defaults seguros (sin data loss en filas existentes):
| Columna | Tipo | Default |
|---------|------|---------|
| `IsPremium` | `boolean` | `false` |
| `LinkedCampaignId` | `uuid nullable` | `null` |
| `FeaturedUntil` | `timestamp with time zone nullable` | `null` |
| `FeaturedPriority` | `integer` | `0` |

_Nota: La migración también incluye `ConcurrencyStamp` y tablas `leads`/`lead_messages` que EF Core detectó como pendientes._

#### 9. `VehiclesSaleService.Infrastructure/Messaging/CampaignEventsConsumer.cs` ← **NUEVO**

**BackgroundService** que consume eventos de AdvertisingService:

- Queue: `vehiclessaleservice.campaign-events`
- Bindings:
  - `advertising.campaign.activated` → `Vehicle.MarkAsPremium()` o `MarkAsFeaturedByAdmin()`
  - `advertising.campaign.completed` → `Vehicle.ClearPromotion()`
  - `advertising.campaign.budget_depleted` → `Vehicle.ClearPromotion()`
- Guard: solo limpia si `LinkedCampaignId` coincide con la campaña que terminó
- Respeta `RabbitMQ:Enabled` config para environments sin RabbitMQ

#### 10. `VehiclesSaleService.Api/Program.cs`

**Registro del HostedService:**

```csharp
builder.Services.AddHostedService<CampaignEventsConsumer>();
```

---

## 📦 Commits

| Hash       | Descripción                                                                               |
| ---------- | ----------------------------------------------------------------------------------------- |
| `e9714a5b` | feat(homepage): enrich rotation with vehicle details + premium properties + campaign sync |

---

## 🐛 Bugs adicionales encontrados y corregidos

### BUG-E001 — ApiResponse serializado en PascalCase

- **Archivo:** `AdvertisingService.Api/Program.cs`
- **Causa:** `AddControllers()` sin `JsonOptions` → PascalCase → frontend TypeScript recibía `undefined` en todos los campos
- **Fix:** `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`
- **Severidad:** 🔴 CRÍTICA (todo el sistema de advertising era invisible para el frontend)
- **Status:** ✅ FIXED

### BUG-E002 — VehicleServiceClient sin timeout por request

- **Archivo:** `AdvertisingService.Application/Clients/VehicleServiceClient.cs`
- **Causa:** Si VehiclesSaleService tardaba >10s (timeout global), bloqueaba la homepage
- **Fix:** `CancellationTokenSource.CreateLinkedTokenSource(ct)` con `CancelAfter(2s)` por request
- **Severidad:** 🟡 ALTO
- **Status:** ✅ FIXED

### BUG-E003 — VehicleServiceClient parseaba respuesta incorrectamente

- **Archivo:** `AdvertisingService.Application/Clients/VehicleServiceClient.cs`
- **Causa:** `ApiResponseWrapper<VehicleBasicInfo>` pero el endpoint devuelve `Vehicle` entity directamente
- **Fix:** Deserializar como `VehicleRawDto` + mapper
- **Severidad:** 🔴 CRÍTICA (siempre retornaba null, tarjetas vacías)
- **Status:** ✅ FIXED

---

## ✅ Checklist final

### Backend (10 puntos)

- [x] 1. `RotatedVehicleDto` incluye: title, slug, imageUrl, price, currency, location, isFeatured, isPremium
- [x] 2. `VehicleBasicInfo` incluye: PrimaryImageUrl, Slug, Location, Currency, IsFeatured, IsPremium
- [x] 3. `GetHomepageRotationQueryHandler` enriquece en paralelo con `VehicleServiceClient`
- [ ] 4. `GET /api/advertising/rotation/FeaturedSpot` retorna items con title e imageUrl poblados _(pendiente campaña activa en prod)_
- [x] 5. `Vehicle.cs` tiene: IsPremium, LinkedCampaignId, FeaturedUntil, FeaturedPriority + métodos MarkAsPremium/ClearPromotion
- [ ] 6. Migration `AddVehiclePremiumProperties` aplicada en producción _(pendiente deploy CI/CD)_
- [x] 7. `CampaignEventsConsumer` registrado como HostedService en VehiclesSaleService.Program.cs
- [ ] 8. Cuando se crea una campaña, el vehículo se marca IsPremium=true _(pendiente deploy)_
- [ ] 9. `advertisingservice` pod Running post-deploy _(pendiente CI/CD build)_
- [ ] 10. `vehiclessaleservice` pod Running post-deploy _(pendiente CI/CD build)_

### Frontend (4 puntos)

- [ ] 11. Sección "⭐ Vehículos Destacados" visible en homepage con tarjetas completas _(pendiente deploy + campaña activa)_
- [ ] 12. Tarjetas muestran imagen, título y precio (no vacías) _(pendiente deploy)_
- [ ] 13. Badge "💎 Premium" visible cuando `isPremium = true` _(pendiente campaña PremiumSpot)_
- [ ] 14. Tracking `POST /api/advertising/tracking/impression` se dispara _(pendiente deploy)_

### Integridad del sistema (3 puntos)

- [ ] 15. Health checks pasando: `curl https://okla.com.do/api/health` retorna Healthy _(pendiente deploy)_
- [ ] 16. No hay errores 500 en logs de gateway post-deploy _(pendiente deploy)_
- [x] 17. CI/CD build disparado con plataforma `linux/amd64` ✅ (4 workflows corriendo)

---

## 🚀 Pasos siguientes

### 1. Monitorear CI/CD (5-10 min)

```bash
gh run list --limit 5
gh run watch
```

### 2. Verificar deployment y migración

```bash
kubectl rollout status deploy/advertisingservice -n okla --timeout=180s
kubectl rollout status deploy/vehiclessaleservice -n okla --timeout=180s

# Verificar columnas nuevas
kubectl exec -n okla statefulset/postgres -- \
  psql -U postgres -d vehiclessaleservice_db -c \
  "\d \"vehicles\"" | grep -E "IsPremium|LinkedCampaign|FeaturedUntil|FeaturedPriority"
```

### 3. Crear campaña de prueba para verificar E2E

```bash
# Obtener un vehículo activo
VEHICLE_ID=$(kubectl exec -n okla statefulset/postgres -- \
  psql -U postgres -d vehiclessaleservice_db -t -c \
  "SELECT id FROM vehicles WHERE status = 2 LIMIT 1;" | tr -d ' ')

# Obtener token admin
ADMIN_TOKEN=$(curl -s -X POST https://okla.com.do/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@okla.local","password":"Admin123!@#"}' | jq -r '.data.access_token')

# Crear campaña de prueba
curl -s -X POST https://okla.com.do/api/advertising/campaigns \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d "{\"vehicleId\": \"$VEHICLE_ID\", \"ownerId\": \"...\", \"ownerType\": \"Dealer\",
    \"placementType\": 0, \"pricingModel\": 2, \"totalBudget\": 100,
    \"startDate\": \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\",
    \"endDate\": \"$(date -u -v+7d +%Y-%m-%dT%H:%M:%SZ)\"}" | jq .

# Forzar refresh y verificar items enriquecidos
curl -s -X POST "https://okla.com.do/api/advertising/rotation/refresh?section=FeaturedSpot" \
  -H "Authorization: Bearer $ADMIN_TOKEN"
curl -s https://okla.com.do/api/advertising/rotation/FeaturedSpot | \
  jq '.data.items[0] | {title, imageUrl, price, isFeatured}'
```

### 4. Verificar consumer RabbitMQ

```bash
kubectl exec deployment/rabbitmq -n okla -- \
  rabbitmqctl list_queues name consumers messages | grep campaign
```

---

## 📐 Arquitectura del flujo implementado

```
Browser → GET /api/advertising/rotation/FeaturedSpot
  ↓ Gateway (Ocelot)
  ↓ AdvertisingService.RotationController
  ↓ GetHomepageRotationQuery (MediatR)
  ↓ GetHomepageRotationQueryHandler
    ↓ HomepageRotationCacheService.GetRotationAsync() → [VehicleId1, VehicleId2, ...]
    ↓ Task.WhenAll([
        VehicleServiceClient.GetVehicleBasicInfoAsync(VehicleId1) → title, imageUrl, price ...
        VehicleServiceClient.GetVehicleBasicInfoAsync(VehicleId2) → ...
      ]) — timeout 2s each
    ↓ Map to RotatedVehicleDto[] con todos los campos enriquecidos
  ↓ ApiResponse<HomepageRotationDto> {
      "success": true,          ← camelCase (E-001 fix)
      "data": {
        "section": "FeaturedSpot",
        "items": [              ← "items" not "vehicles" (E-001 + JsonPropertyName)
          {
            "vehicleId": "...",
            "campaignId": "...",
            "qualityScore": 0.9, ← "qualityScore" not "score"
            "title": "Toyota Camry 2022",
            "imageUrl": "https://...",
            "price": 900000,
            "currency": "DOP",
            "location": "Santo Domingo, Nacional",
            "isFeatured": true,
            "isPremium": false
          }
        ]
      }
    }
  ↓ Frontend FeaturedVehicles component renders card con imagen/título/precio/badge
```

```
AdvertisingService [billing.payment.completed]
  → campaign.Activate()
  → publish "advertising.campaign.activated" {CampaignId, VehicleId, PlacementType}

VehiclesSaleService.CampaignEventsConsumer
  → Vehicle.MarkAsPremium(campaignId) / MarkAsFeaturedByAdmin()
  → vehicleRepo.UpdateAsync(vehicle)
  → IsPremium = true en vehiclessaleservice_db
```

---

_Informe generado por GitHub Copilot — 2026-02-23_
