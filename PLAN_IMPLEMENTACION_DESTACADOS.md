# ✅ VERIFICACIÓN & PLAN DE IMPLEMENTACIÓN

## Flujo de Vehículos Destacados/Premium en Página Principal

**Fecha:** 23 de Febrero, 2026  
**Prioridad:** 🔴 ALTA  
**Esfuerzo Estimado:** 3-4 sprints

---

## 📋 CHECKLIST DE VERIFICACIÓN

### PARTE 1: Backend - VehiclesSaleService

- [ ] **CRÍTICO:** Verificar si Vehicle entity tiene propiedades:
  - [ ] `IsFeatured` bool
  - [ ] `IsPremium` bool
  - [ ] `LinkedCampaignId` Guid?
  - [ ] `FeaturedUntil` DateTime?
  - [ ] `FeaturedPriority` int

  **Comando para verificar:**

  ```bash
  grep -r "IsFeatured\|IsPremium\|LinkedCampaignId" \
    backend/VehiclesSaleService/VehiclesSaleService.Domain/
  ```

  **Archivo a revisar:**

  ```
  backend/VehiclesSaleService/VehiclesSaleService.Domain/Vehicles/Vehicle.cs
  ```

- [ ] Verificar HomepageSectionsController existe

  ```bash
  ls backend/VehiclesSaleService/VehiclesSaleService.Api/Controllers/ \
    | grep HomepageSection
  ```

- [ ] Verificar endpoint POST /api/homepagesections/{slug}/vehicles

  ```bash
  grep -A 20 "AssignVehicleToSection\|/vehicles" \
    backend/VehiclesSaleService/VehiclesSaleService.Api/Controllers/HomepageSectionsController.cs
  ```

- [ ] Verificar evento VehicleHomepageAssignment se crea correctamente
  ```sql
  SELECT * FROM "VehicleHomepageAssignments" LIMIT 5;
  ```

### PARTE 2: Backend - AdvertisingService

- [ ] Verificar Campaign entity existe y tiene:
  - [ ] `VehicleId` Guid
  - [ ] `PlacementType` string (FeaturedSpot, PremiumSpot)
  - [ ] `Status` enum (Active, Paused, Ended)
  - [ ] Budget tracking

  **Archivo a revisar:**

  ```
  backend/AdvertisingService/AdvertisingService.Domain/Campaigns/Campaign.cs
  ```

- [ ] Verificar RotationController existe

  ```bash
  ls backend/AdvertisingService/AdvertisingService.Api/Controllers/ \
    | grep -i rotation
  ```

- [ ] Verificar endpoint GET /api/advertising/rotation existe

  ```bash
  grep -r "GetRotation\|/rotation" \
    backend/AdvertisingService/AdvertisingService.Api/Controllers/
  ```

- [ ] Verificar Impression & Click tracking tables existen
  ```sql
  SELECT table_name FROM information_schema.tables
  WHERE table_schema = 'public'
  AND table_name IN ('Impressions', 'Clicks');
  ```

### PARTE 3: Backend - Integración

- [ ] Verificar RabbitMQ event handlers para sincronización

  ```bash
  grep -r "CampaignCreatedEvent\|VehiclePromoted" \
    backend/VehiclesSaleService/
  ```

- [ ] Verificar Gateway ocelot.prod.json tiene rutas correctas

  ```bash
  grep -E "advertising|homepagesections" \
    backend/Gateway/Gateway.Api/ocelot.prod.json
  ```

- [ ] Verificar PaymentService está integrado (si existe)
  ```bash
  ls -la backend/ | grep -i payment
  ```

### PARTE 4: Frontend

- [ ] Verificar hook `useHomepageRotation` existe

  ```bash
  grep -r "useHomepageRotation" frontend/web-next/src/hooks/
  ```

- [ ] Verificar componente `FeaturedVehicles` existe

  ```bash
  ls frontend/web-next/src/components/advertising/
  ```

- [ ] Verificar tracking functions `recordImpression` y `recordClick` existen

  ```bash
  grep -r "recordImpression\|recordClick\|useRecordImpression" \
    frontend/web-next/src/hooks/
  ```

- [ ] Verificar AdminHomepagePage existe

  ```bash
  ls frontend/web-next/src/app/*/admin* 2>/dev/null
  ```

- [ ] Verificar servicio `homepageSectionsService` existe
  ```bash
  ls frontend/web-next/src/services/ | grep -i homepage
  ```

### PARTE 5: Base de Datos

- [ ] Verificar tablas existen en vehiclessaleservice_db

  ```sql
  SELECT table_name FROM information_schema.tables
  WHERE table_schema = 'public'
  AND table_name IN ('Vehicles', 'VehicleHomepageAssignments');
  ```

- [ ] Verificar tablas existen en advertisingservice_db

  ```sql
  SELECT table_name FROM information_schema.tables
  WHERE table_schema = 'public'
  AND table_name IN ('Campaigns', 'Impressions', 'Clicks');
  ```

- [ ] Verificar índices de performance
  ```sql
  SELECT indexname FROM pg_indexes
  WHERE tablename IN ('Vehicles', 'Campaigns', 'Impressions');
  ```

---

## 🎯 PLAN DE IMPLEMENTACIÓN

### FASE 1: Preparación (1 Sprint - 1 semana)

#### 1.1 Auditoría Detallada

- [ ] Ejecutar todo el checklist anterior
- [ ] Documentar hallazgos en tickets Jira
- [ ] Crear diagrama de dependencias actual
- [ ] Meeting con equipo para validar hallazgos

#### 1.2 Diseño de Solución

- [ ] Definir claro: ¿Qué es "featured" vs "premium"?
- [ ] Definir propiedades faltantes en Vehicle
- [ ] Definir flujo de sincronización por eventos
- [ ] Documentar nuevos endpoints necesarios

**Deliverable:** DOCUMENTO_DISEÑO_SOLUCION.md

---

### FASE 2: Backend - VehiclesSaleService (1.5 Sprints)

#### 2.1 Agregar Propiedades a Vehicle

**Ticket:** feat/vehicle-featured-premium-properties
**Esfuerzo:** 4-6 horas

```csharp
// VehiclesSaleService.Domain/Vehicles/Vehicle.cs
public class Vehicle : AggregateRoot
{
    // Existing fields...

    // NEW FIELDS
    public bool IsFeatured { get; set; }
    public bool IsPremium { get; set; }
    public Guid? LinkedCampaignId { get; set; }
    public DateTime? FeaturedUntil { get; set; }
    public int FeaturedPriority { get; set; } = 0;

    // Helper methods
    public void MarkAsFeatured(Guid? campaignId = null, DateTime? until = null, int priority = 0)
    {
        if (campaignId.HasValue)
            IsPremium = true;
        else
            IsFeatured = true;

        LinkedCampaignId = campaignId;
        FeaturedUntil = until;
        FeaturedPriority = priority;

        AddDomainEvent(new VehiclePromotedEvent(Id, IsFeatured, IsPremium));
    }

    public void UnmarkAsFeatured()
    {
        IsFeatured = false;
        IsPremium = false;
        LinkedCampaignId = null;
        FeaturedUntil = null;
        FeaturedPriority = 0;

        AddDomainEvent(new VehicleUnpromotedEvent(Id));
    }
}
```

**Tareas:**

1. [ ] Modificar Vehicle.cs
2. [ ] Crear AddIsFeaturedIsPremiumToVehicles migration
3. [ ] Actualizar VehicleRepository queries
4. [ ] Actualizar GetVehicleByIdQuery con nuevas propiedades
5. [ ] Tests unitarios para Vehicle.MarkAsFeatured()

#### 2.2 Event Handlers para Sincronización

**Ticket:** feat/vehicle-advertising-event-sync
**Esfuerzo:** 8 horas

**Archivo:** `VehiclesSaleService.Infrastructure/EventHandlers/CampaignEventsHandler.cs`

```csharp
public class CampaignCreatedEventHandler : IEventHandler<CampaignCreatedEvent>
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IMediator _mediator;

    public async Task Handle(CampaignCreatedEvent @event)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(@event.Data.VehicleId);
        if (vehicle == null) return;

        vehicle.MarkAsFeatured(
            campaignId: Guid.Parse(@event.Data.CampaignId),
            until: @event.Data.EndDate,
            priority: 100  // Premium = alta prioridad
        );

        await _vehicleRepository.UpdateAsync(vehicle);
    }
}

public class CampaignExpiredEventHandler : IEventHandler<CampaignExpiredEvent>
{
    private readonly IVehicleRepository _vehicleRepository;

    public async Task Handle(CampaignExpiredEvent @event)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(@event.Data.VehicleId);
        if (vehicle == null) return;

        vehicle.UnmarkAsFeatured();
        await _vehicleRepository.UpdateAsync(vehicle);
    }
}

public class VehicleSoldEventHandler : IEventHandler<VehicleSoldEvent>
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IMediator _mediator;

    public async Task Handle(VehicleSoldEvent @event)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(@event.AggregateId);
        if (vehicle?.LinkedCampaignId != null)
        {
            // Notificar a AdvertisingService para pausar campaña
            var command = new PauseCampaignCommand(vehicle.LinkedCampaignId.Value);
            await _mediator.Send(command);
        }
    }
}
```

**Tareas:**

1. [ ] Crear IEventHandler implementation
2. [ ] Registrar handlers en DI
3. [ ] Agregar RabbitMQ consumer para events
4. [ ] Tests de integración con eventos

#### 2.3 Nuevos Endpoints

**Ticket:** feat/vehiclesale-featured-endpoints
**Esfuerzo:** 6 horas

```csharp
// VehiclesSaleService.Api/Controllers/VehiclesController.cs

[HttpGet("api/vehicles/featured")]
public async Task<ActionResult<ApiResponse<List<VehicleDto>>>> GetFeaturedVehicles(
    [FromQuery] int limit = 8,
    [FromQuery] bool premium = false
)
{
    var query = new GetFeaturedVehiclesQuery(limit, premium);
    var result = await _mediator.Send(query);
    return Ok(new ApiResponse<List<VehicleDto>>(result));
}

[HttpGet("api/vehicles/featured/by-section/{sectionId}")]
public async Task<ActionResult<ApiResponse<List<VehicleDto>>>> GetFeaturedBySection(
    string sectionId,
    [FromQuery] int limit = 8
)
{
    var query = new GetVehiclesByHomepageSectionQuery(sectionId, limit);
    var result = await _mediator.Send(query);
    return Ok(new ApiResponse<List<VehicleDto>>(result));
}
```

---

### FASE 3: Backend - AdvertisingService (1 Sprint)

#### 3.1 Implementar/Verificar RotationController

**Ticket:** feat/advertising-rotation-controller
**Esfuerzo:** 8 horas

```csharp
// AdvertisingService.Api/Controllers/RotationController.cs

[ApiController]
[Route("api/advertising")]
public class RotationController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpGet("rotation")]
    public async Task<ActionResult<RotationResponse>> GetRotation(
        [FromQuery] string placementType = "FeaturedSpot",
        [FromQuery] int limit = 8,
        [FromQuery] string rotationType = "random"
    )
    {
        var query = new GetActiveRotationQuery(placementType, limit, rotationType);
        var result = await _mediator.Send(query);

        return Ok(new RotationResponse
        {
            PlacementType = placementType,
            Items = result.Items,
            TotalCount = result.TotalCount,
            RotationType = rotationType,
            RefreshInterval = 3600  // 1 hora
        });
    }
}

public class RotationResponse
{
    public string PlacementType { get; set; }
    public List<RotatedVehicle> Items { get; set; }
    public int TotalCount { get; set; }
    public string RotationType { get; set; }
    public int RefreshInterval { get; set; }
}

public class RotatedVehicle
{
    public string CampaignId { get; set; }
    public string VehicleId { get; set; }
    public string Title { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; }
    public string ImageUrl { get; set; }
    public string Slug { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsPremium { get; set; }
    public string Location { get; set; }
}
```

**Tareas:**

1. [ ] Crear RotationController si no existe
2. [ ] Crear GetActiveRotationQuery
3. [ ] Crear GetActiveRotationQueryHandler
4. [ ] Implementar rotación (random/fifo/priority)
5. [ ] Caching de resultados (Redis 5 min)
6. [ ] Tests

#### 3.2 Eventos de Expiración

**Ticket:** feat/advertising-campaign-expiration
**Esfuerzo:** 8 horas

```csharp
// AdvertisingService.Infrastructure/Jobs/CampaignExpirationJob.cs

public class CampaignExpirationJob : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private Timer _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(CheckExpiredCampaigns, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        return Task.CompletedTask;
    }

    private void CheckExpiredCampaigns(object state)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var campaignService = scope.ServiceProvider.GetRequiredService<ICampaignService>();
            var expiredCampaigns = campaignService.GetExpiredCampaigns();

            foreach (var campaign in expiredCampaigns)
            {
                campaign.End();  // Emite CampaignExpiredEvent
                campaignService.UpdateAsync(campaign).Wait();
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        return Task.CompletedTask;
    }
}
```

---

### FASE 4: Frontend (1 Sprint)

#### 4.1 Actualizar Hooks

**Ticket:** feat/frontend-advertising-hooks
**Esfuerzo:** 4 horas

```typescript
// frontend/web-next/src/hooks/use-advertising.ts

export function useHomepageRotation(
  placementType: "FeaturedSpot" | "PremiumSpot" = "FeaturedSpot",
) {
  return useQuery({
    queryKey: ["homepage-rotation", placementType],
    queryFn: async () => {
      const response = await fetch(
        `/api/advertising/rotation?placementType=${placementType}&limit=8`,
      );
      if (!response.ok) throw new Error("Failed to fetch rotation");
      return response.json();
    },
    staleTime: 5 * 60 * 1000, // 5 minutos
    cacheTime: 10 * 60 * 1000, // 10 minutos en cache
  });
}

export function useRecordImpression() {
  return useMutation({
    mutationFn: async (data: {
      campaignId: string;
      vehicleId: string;
      section: string;
    }) => {
      const response = await fetch("/api/advertising/tracking/impression", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data),
      });
      if (!response.ok) throw new Error("Failed to record impression");
      return response.json();
    },
    // No mostrar error - es tracking, no crítico
    onError: (error) => console.warn("Impression tracking failed:", error),
  });
}

export function useRecordClick() {
  return useMutation({
    mutationFn: async (data: {
      campaignId: string;
      vehicleId: string;
      section: string;
    }) => {
      const response = await fetch("/api/advertising/tracking/click", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data),
      });
      if (!response.ok) throw new Error("Failed to record click");
      return response.json();
    },
    onError: (error) => console.warn("Click tracking failed:", error),
  });
}
```

#### 4.2 Actualizar FeaturedVehicles Component

**Ticket:** feat/frontend-featured-vehicles-component
**Esfuerzo:** 6 horas

```typescript
// frontend/web-next/src/components/advertising/featured-vehicles.tsx

export default function FeaturedVehicles({
  title = 'Vehículos Destacados',
  placementType = 'FeaturedSpot',
  maxItems = 8,
}: FeaturedVehiclesProps) {
  const { data: rotation, isLoading } = useHomepageRotation(placementType);
  const recordImpression = useRecordImpression();
  const recordClick = useRecordClick();

  if (isLoading) return <FeaturedVehiclesSkeleton maxItems={maxItems} />;
  if (!rotation?.items?.length) return null;

  return (
    <section className="py-8 bg-white">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="mb-6 flex items-center justify-between">
          <h2 className="text-2xl font-bold">
            {placementType === 'PremiumSpot' ? '💎' : '⭐'} {title}
          </h2>
          <Link href="/buscar" className="text-primary text-sm hover:underline">
            Ver todos →
          </Link>
        </div>

        <div className="grid grid-cols-2 gap-4 md:grid-cols-3 lg:grid-cols-4">
          {rotation.items.slice(0, maxItems).map((vehicle) => (
            <FeaturedVehicleCard
              key={vehicle.vehicleId}
              vehicle={vehicle}
              placementType={placementType}
              onImpression={() =>
                recordImpression.mutate({
                  campaignId: vehicle.campaignId,
                  vehicleId: vehicle.vehicleId,
                  section: placementType,
                })
              }
              onClick={() =>
                recordClick.mutate({
                  campaignId: vehicle.campaignId,
                  vehicleId: vehicle.vehicleId,
                  section: placementType,
                })
              }
            />
          ))}
        </div>
      </div>
    </section>
  );
}
```

---

### FASE 5: Testing & Deployment (1 Sprint)

#### 5.1 Tests E2E

**Ticket:** test/e2e-featured-vehicles-flow
**Esfuerzo:** 8 horas

```typescript
// frontend/web-next/tests/e2e/featured-vehicles.spec.ts

describe("Featured Vehicles Flow", () => {
  it("should display featured vehicles on homepage", async ({ page }) => {
    await page.goto("https://localhost/");

    const featuredSection = page.locator("text=⭐ Vehículos Destacados");
    await expect(featuredSection).toBeVisible();

    const vehicleCards = page.locator('[data-testid="featured-vehicle-card"]');
    const count = await vehicleCards.count();
    expect(count).toBeGreaterThan(0);
  });

  it("should display premium vehicles with correct badge", async ({ page }) => {
    await page.goto("https://localhost/");

    const premiumSection = page.locator("text=💎 Vehículos Premium");
    await expect(premiumSection).toBeVisible();

    const premiumBadges = page.locator('[data-testid="premium-badge"]');
    const count = await premiumBadges.count();
    expect(count).toBeGreaterThan(0);
  });

  it("should track impression when premium vehicle is viewed", async ({
    page,
  }) => {
    let impressionCalled = false;

    await page.route("**/api/advertising/tracking/impression", (route) => {
      impressionCalled = true;
      route.continue();
    });

    await page.goto("https://localhost/");
    await page.waitForTimeout(2000); // Wait for impression to be recorded

    expect(impressionCalled).toBe(true);
  });

  it("should track click when vehicle is selected", async ({ page }) => {
    let clickCalled = false;

    await page.route("**/api/advertising/tracking/click", (route) => {
      clickCalled = true;
      route.continue();
    });

    await page.goto("https://localhost/");
    const vehicleCard = page
      .locator('[data-testid="featured-vehicle-card"]')
      .first();
    await vehicleCard.click();

    expect(clickCalled).toBe(true);
  });
});
```

#### 5.2 Deployment Checklist

- [ ] Migrations ejecutadas en DB
- [ ] RabbitMQ queues creadas
- [ ] Environment variables configuradas
- [ ] Redis initialized
- [ ] SSL certificates validas
- [ ] Health checks pasando
- [ ] Load tests satisfactorios
- [ ] Smoke tests en producción

---

## 📊 Matriz de Riesgos

| Riesgo                                      | Probabilidad | Impacto  | Mitigación                      |
| ------------------------------------------- | ------------ | -------- | ------------------------------- |
| Propiedades IsFeatured/IsPremium no existen | 🟡 Media     | 🔴 Alto  | Verificar inmediatamente        |
| Sincronización de eventos falla             | 🟡 Media     | 🔴 Alto  | Tests unitarios + E2E           |
| Endpoint /rotation no existe                | 🟡 Media     | 🟡 Medio | Implementar en sprint 3         |
| Base de datos sin índices                   | 🟡 Media     | 🟡 Medio | Agregar índices antes de deploy |
| Frontend no implementa tracking             | 🔴 Alto      | 🟡 Medio | Code review + tests             |
| Campaña expira pero sigue mostrándose       | 🟡 Media     | 🟡 Medio | Job scheduled para expiración   |

---

## 📞 Tickets Jira a Crear

### Epics

- [ ] Epic: "Flujo de Vehículos Destacados/Premium en Homepage"

### Stories

1. [ ] Story: "Agregar propiedades IsFeatured/IsPremium a Vehicle"
2. [ ] Story: "Implementar sincronización por eventos RabbitMQ"
3. [ ] Story: "Crear/Verificar endpoint GET /advertising/rotation"
4. [ ] Story: "Implementar CampaignExpirationJob"
5. [ ] Story: "Actualizar FeaturedVehicles component"
6. [ ] Story: "Agregar tests E2E para featured vehicles"

### Tasks

- [ ] Ejecutar checklist de verificación
- [ ] Documentar hallazgos
- [ ] Code review de cambios
- [ ] Performance testing
- [ ] Security audit

---

## 🚀 Timeline Propuesto

```
┌─────────────────────────────────────────────────────────┐
│                  TIMELINE: 4 SPRINTS (8 SEMANAS)         │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  SPRINT 1 (Semana 1): PREPARACIÓN & AUDITORÍA           │
│  ├─ Ejecutar verificaciones                            │
│  ├─ Documentar hallazgos                               │
│  └─ Design review con equipo                           │
│                                                          │
│  SPRINT 2 (Semana 2-3): VEHICLESSALESERVICE            │
│  ├─ Agregar propiedades a Vehicle                      │
│  ├─ Crear migrations                                   │
│  ├─ Implementar Event Handlers                         │
│  └─ Tests unitarios                                    │
│                                                          │
│  SPRINT 3 (Semana 4-5): ADVERTISINGSERVICE             │
│  ├─ Verificar/Implementar RotationController           │
│  ├─ Agregar propiedades a Campaign                     │
│  ├─ Implementar CampaignExpirationJob                  │
│  └─ Tests e integración                                │
│                                                          │
│  SPRINT 4 (Semana 6-8): FRONTEND & TESTING             │
│  ├─ Actualizar hooks                                   │
│  ├─ Actualizar components                              │
│  ├─ E2E tests                                           │
│  ├─ Performance tuning                                 │
│  └─ Deployment a producción                            │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

---

## ✅ Criterios de Aceptación

### Para dar por COMPLETO el proyecto:

- [ ] ✅ Todo el checklist de verificación pasó
- [ ] ✅ Dealer puede ver opción para pagar publicidad
- [ ] ✅ Vehículos premium aparecen en sección "💎 Premium"
- [ ] ✅ Vehículos destacados aparecen en sección "⭐ Destacados"
- [ ] ✅ Tracking de impresiones/clicks funciona correctamente
- [ ] ✅ Dashboard del dealer muestra métricas
- [ ] ✅ Campañas expiradas desaparecen automáticamente
- [ ] ✅ E2E tests pasan al 100%
- [ ] ✅ Load tests satisfactorios (1000 req/s)
- [ ] ✅ Security audit pasó
- [ ] ✅ Documentación completada
- [ ] ✅ Team training completado

---

_Documento generado: 2026-02-23_  
_Versión: 1.0_  
_Estado: LISTO PARA IMPLEMENTACIÓN_
