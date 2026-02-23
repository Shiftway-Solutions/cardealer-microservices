# 🤖 PROMPT: Implementación E2E — Vehículos Destacados/Premium en Homepage

Eres un agente de ingeniería con acceso al repositorio cardealer-microservices (branch: main,
owner: gregorymorenoiem) y al cluster Kubernetes de producción DOKS (okla-cluster, namespace: okla).
Tu misión: implementar el flujo completo para que los vehículos con campañas activas de publicidad
aparezcan correctamente en la página principal https://okla.com.do/ con sus badges **⭐ Destacado**
y **💎 Premium**, incluyendo imagen, título, precio y ubicación.

Si encuentras errores adicionales durante la implementación, corrígelos, commitéalos y abre un PR.
Al terminar entrega un informe markdown completo con todo lo ejecutado.

> ⚠️ **CRÍTICO — DOCKER BUILD:** Los nodos del cluster DOKS son **linux/amd64**. Todo `docker build`
> debe usar `--platform linux/amd64`. El CI/CD (`.github/workflows/smart-cicd.yml`) ya usa
> `platforms: linux/amd64` — no cambies esto. Si construyes imágenes localmente para pruebas,
> añade `--platform linux/amd64` explícitamente.

══════════════════════════════════════════════════════════════
CONTEXTO DE ARQUITECTURA — LEE ESTO ANTES DE EMPEZAR
══════════════════════════════════════════════════════════════

**Stack:**

- Backend: .NET 8, Clean Architecture, CQRS + MediatR, FluentValidation, RabbitMQ, PostgreSQL, Redis
- Frontend: Next.js 16 App Router, TypeScript, TanStack Query, shadcn/ui, pnpm (**NO npm/yarn**)
- Gateway: Ocelot en puerto 8080. BFF: Browser → `https://okla.com.do/api/*` → gateway:8080 → microservicio
- K8s: namespace `okla` | todos los servicios en puerto **8080** interno

**Servicios relevantes:**

- `AdvertisingService` — gestiona campañas, motor de rotación, tracking impressions/clicks
- `VehiclesSaleService` — gestiona vehículos, HomepageSections, catálogo público
- `frontend-web` — Next.js, componentes `FeaturedVehicles` y hooks de advertising

**Bases de datos:**

- `advertisingservice_db` — campañas, rotaciones, impressiones, clicks
- `vehiclessaleservice_db` — vehículos, imágenes, homepage assignments

**Rutas frontend relevantes:**

- `frontend/web-next/src/components/advertising/featured-vehicles.tsx` — componente principal
- `frontend/web-next/src/hooks/use-advertising.ts` — hook `useHomepageRotation`
- `frontend/web-next/src/services/advertising.ts` — función `getHomepageRotation`
- `frontend/web-next/src/types/advertising.ts` — interfaces `HomepageRotation`, `RotatedVehicle`

**Rutas backend relevantes:**

- `backend/AdvertisingService/AdvertisingService.Api/Controllers/RotationController.cs`
- `backend/AdvertisingService/AdvertisingService.Application/Features/Rotation/`
- `backend/AdvertisingService/AdvertisingService.Application/DTOs/HomepageRotationDto.cs`
- `backend/AdvertisingService/AdvertisingService.Application/Clients/VehicleServiceClient.cs`
- `backend/AdvertisingService/AdvertisingService.Infrastructure/Services/AdRotationEngine.cs`
- `backend/AdvertisingService/AdvertisingService.Infrastructure/Services/HomepageRotationCacheService.cs`
- `backend/VehiclesSaleService/VehiclesSaleService.Domain/Entities/Vehicle.cs`
- `backend/VehiclesSaleService/VehiclesSaleService.Infrastructure/Messaging/RabbitMqEventPublisher.cs`

**Reglas críticas de codificación:**

- ❌ NO uses `CreateBootstrapLogger()` + `UseStandardSerilog()` simultáneamente
- ❌ Health `/health` debe excluir checks con tag `"external"`
- ❌ Todas las interfaces inyectadas en DI **DEBEN** estar registradas en `Program.cs`
- ❌ Si cambias queue RabbitMQ args → DELETE la queue vieja primero
- ❌ Nunca guardes secrets en código — usa K8s secrets o env vars
- ✅ Validaciones: `NoSqlInjection().NoXss()` en todos los string inputs del backend
- ✅ C#: PascalCase clases/métodos, camelCase parámetros, `_camelCase` campos privados
- ✅ TypeScript: PascalCase componentes, camelCase funciones/variables
- ✅ Shared libs: `CarDealer.Shared`, `CarDealer.Contracts`

══════════════════════════════════════════════════════════════
DIAGNÓSTICO PREVIO — LO QUE YA EXISTE (NO TOCAR)
══════════════════════════════════════════════════════════════

Antes de implementar, confirma que estos componentes existen y funcionan:

```bash
# 1. RotationController (GET /api/advertising/rotation/{section})
cat backend/AdvertisingService/AdvertisingService.Api/Controllers/RotationController.cs

# 2. AdPlacementType enum (FeaturedSpot=0, PremiumSpot=1)
cat backend/AdvertisingService/AdvertisingService.Domain/Enums/AdPlacementType.cs

# 3. CampaignCreatedEvent (advertising.campaign.created)
cat backend/AdvertisingService/AdvertisingService.Domain/Events/CampaignCreatedEvent.cs

# 4. VehicleServiceClient (llama a /api/vehicles/{id} en VehiclesSaleService)
cat backend/AdvertisingService/AdvertisingService.Application/Clients/VehicleServiceClient.cs

# 5. IsFeatured en Vehicle.cs (solo este campo existe, el resto falta)
grep -n "IsFeatured\|IsPremium\|LinkedCampaignId\|FeaturedUntil" \
  backend/VehiclesSaleService/VehiclesSaleService.Domain/Entities/Vehicle.cs

# 6. HomepageRotationDto (solo tiene: CampaignId, VehicleId, OwnerId, OwnerType, Position, Score)
cat backend/AdvertisingService/AdvertisingService.Application/DTOs/HomepageRotationDto.cs

# 7. Frontend RotatedVehicle interface (espera: title, slug, imageUrl, price, currency, location, isFeatured, isPremium)
grep -A 20 "interface RotatedVehicle" frontend/web-next/src/types/advertising.ts

# 8. Gateway routes para rotation (ya existen en prod y dev)
grep "rotation" backend/Gateway/Gateway.Api/ocelot.prod.json | head -5
```

══════════════════════════════════════════════════════════════
BRECHAS A CORREGIR — 4 GAPS CONFIRMADOS
══════════════════════════════════════════════════════════════

```
GAP #1 [CRÍTICO] RotatedVehicleDto no incluye detalles del vehículo
   Backend devuelve:  { CampaignId, VehicleId, Position, Score }
   Frontend necesita: { title, slug, imageUrl, price, currency, location, isFeatured, isPremium }
   Resultado actual:  FeaturedVehicles renderiza tarjetas vacías (sin imagen, sin título, sin precio)

GAP #2 [CRÍTICO] VehicleBasicInfo en AdvertisingService le falta campos
   Actual:   { Id, Title, Make, Model, Year, Price, ImageCount, HasDescription, SellerId }
   Faltante: PrimaryImageUrl, Slug, Location, Currency, IsFeatured, IsPremium

GAP #3 [ALTO] Vehicle.cs le faltan propiedades Premium
   Actual:   IsFeatured (bool) = false
   Faltante: IsPremium, LinkedCampaignId, FeaturedUntil, FeaturedPriority

GAP #4 [ALTO] No hay sincronización por eventos entre AdvertisingService → VehiclesSaleService
   Falta: Consumer en VehiclesSaleService para CampaignCreatedEvent (para marcar vehicle.IsPremium = true)
   Falta: Consumer para CampaignExpiredEvent (para limpiar IsPremium)
```

══════════════════════════════════════════════════════════════
PASOS A EJECUTAR
══════════════════════════════════════════════════════════════

────────────────────────────────
PASO 1 — Verificar salud del cluster
────────────────────────────────

```bash
kubectl get pods -n okla | grep -E "advertisingservice|vehiclessaleservice|gateway|frontend-web"
```

Todos deben estar en estado `Running`. Si hay `CrashLoopBackOff` o `ImagePullBackOff`:

```bash
kubectl logs -n okla deploy/<servicio> --tail=50
kubectl describe pod -n okla <pod-name>
```

Si necesitas renovar registry credentials:

```bash
TOKEN=$(gh auth token)
kubectl delete secret registry-credentials -n okla
kubectl create secret docker-registry registry-credentials \
  --docker-server=ghcr.io \
  --docker-username=gregorymorenoiem \
  --docker-password=$TOKEN -n okla
```

Verifica que el endpoint de rotación responde (puede retornar lista vacía si no hay campañas):

```bash
curl -s https://okla.com.do/api/advertising/rotation/FeaturedSpot | jq .
```

Si retorna 404, el Gateway o el AdvertisingService no está respondiendo → revisa los logs antes de continuar.

────────────────────────────────
PASO 2 — Corregir GAP #2: Ampliar VehicleBasicInfo y endpoint de vehículos
────────────────────────────────

**Archivo:** `backend/AdvertisingService/AdvertisingService.Application/Clients/VehicleServiceClient.cs`

Amplía `VehicleBasicInfo` para incluir los campos que necesita el frontend:

```csharp
public class VehicleBasicInfo
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "DOP";
    public string? PrimaryImageUrl { get; set; }   // ← NUEVO
    public string? Slug { get; set; }              // ← NUEVO
    public string? Location { get; set; }          // ← NUEVO (ciudad/provincia)
    public bool IsFeatured { get; set; }           // ← NUEVO
    public bool IsPremium { get; set; }            // ← NUEVO
    public int ImageCount { get; set; }
    public bool HasDescription { get; set; }
    public Guid SellerId { get; set; }
}
```

Luego verifica que `VehiclesSaleService` devuelve estos campos en `GET /api/vehicles/{id}`.
Busca el DTO de respuesta:

```bash
find backend/VehiclesSaleService/ -name "VehicleDto.cs" -o -name "VehicleDetailDto.cs" \
  | grep -v "obj\|bin" | xargs cat 2>/dev/null | head -80
```

Si los campos `PrimaryImageUrl`, `Slug`, `Location`, `IsFeatured`, `IsPremium` no están en el DTO
de respuesta de VehiclesSaleService, agrégalos. Busca el DTO de respuesta del endpoint de vehículos:

```bash
grep -rn "VehicleDto\|VehicleResponse\|VehicleDetailResponse" \
  backend/VehiclesSaleService/VehiclesSaleService.Application/ | grep -v "obj\|bin" | head -20
```

Añade los campos faltantes y asegúrate de que el mapper los incluya.

────────────────────────────────
PASO 3 — Corregir GAP #1: Enriquecer RotatedVehicleDto con detalles del vehículo
────────────────────────────────

**Problema central:** `GetHomepageRotationQueryHandler` retorna solo IDs y scores, sin detalles del vehículo.

**Archivo:** `backend/AdvertisingService/AdvertisingService.Application/Features/Rotation/Queries/GetHomepageRotation/GetHomepageRotationQueryHandler.cs`

Modifica el handler para enriquecer cada vehículo rotado con sus detalles:

```csharp
public class GetHomepageRotationQueryHandler : IRequestHandler<GetHomepageRotationQuery, HomepageRotationDto?>
{
    private readonly IHomepageRotationCacheService _cacheService;
    private readonly VehicleServiceClient _vehicleClient;  // ← INYECTAR

    public GetHomepageRotationQueryHandler(
        IHomepageRotationCacheService cacheService,
        VehicleServiceClient vehicleClient)
    {
        _cacheService = cacheService;
        _vehicleClient = vehicleClient;
    }

    public async Task<HomepageRotationDto?> Handle(GetHomepageRotationQuery request, CancellationToken ct)
    {
        var result = await _cacheService.GetRotationAsync(request.Section, ct);
        if (result == null) return null;

        // Enriquecer en paralelo (máx 8 vehículos, timeout corto para no bloquear homepage)
        var enrichmentTasks = result.Vehicles.Select(v =>
            _vehicleClient.GetVehicleBasicInfoAsync(v.VehicleId, ct));
        var vehicleDetails = await Task.WhenAll(enrichmentTasks);

        var items = result.Vehicles.Select((v, i) =>
        {
            var details = vehicleDetails[i];
            var isPremium = request.Section == AdPlacementType.PremiumSpot;
            return new RotatedVehicleDto(
                v.CampaignId,
                v.VehicleId,
                v.OwnerId,
                v.OwnerType,
                v.Position,
                v.Score,
                details?.Title,
                details?.Slug,
                details?.PrimaryImageUrl,
                details?.Price ?? 0,
                details?.Currency ?? "DOP",
                details?.Location,
                details?.IsFeatured ?? !isPremium,
                details?.IsPremium ?? isPremium
            );
        }).ToList();

        return new HomepageRotationDto(
            result.Section,
            items,
            result.AlgorithmUsed,
            result.GeneratedAt
        );
    }
}
```

**Actualizar RotatedVehicleDto** para incluir los campos de enriquecimiento:

**Archivo:** `backend/AdvertisingService/AdvertisingService.Application/DTOs/HomepageRotationDto.cs`

```csharp
public record HomepageRotationDto(
    AdPlacementType Section,
    List<RotatedVehicleDto> Vehicles,
    RotationAlgorithmType AlgorithmUsed,
    DateTime GeneratedAt
);

public record RotatedVehicleDto(
    Guid? CampaignId,
    Guid VehicleId,
    Guid OwnerId,
    string OwnerType,
    int Position,
    decimal Score,
    // Campos de enriquecimiento desde VehiclesSaleService:
    string? Title,
    string? Slug,
    string? ImageUrl,
    decimal Price,
    string Currency,
    string? Location,
    bool IsFeatured,
    bool IsPremium
);
```

Verifica que el serializador JSON mapea `Vehicles` → `items` para que coincida con el tipo TypeScript.
Busca si hay algún `JsonPropertyName` o convención en el AdvertisingService para nombres camelCase:

```bash
grep -rn "JsonPropertyName\|JsonNamingPolicy\|AddJsonOptions\|camelCase" \
  backend/AdvertisingService/AdvertisingService.Api/Program.cs | head -10
```

Si usa la convención camelCase automática de .NET (AddControllers + JsonOptions), el campo
`Vehicles` se serializará como `vehicles`. Pero el frontend espera `items` (ver `HomepageRotation.items`
en `frontend/web-next/src/types/advertising.ts`). Asegúrate de que el JSON field name sea `items`:

```csharp
// Opción A: Record con JsonPropertyName
public record HomepageRotationDto(
    [property: JsonPropertyName("section")] AdPlacementType Section,
    [property: JsonPropertyName("items")] List<RotatedVehicleDto> Vehicles,
    [property: JsonPropertyName("algorithmUsed")] RotationAlgorithmType AlgorithmUsed,
    [property: JsonPropertyName("generatedAt")] DateTime GeneratedAt
);

// Opción B: Si ya hay una clase separada, agrega [JsonPropertyName("items")] al campo Vehicles
```

────────────────────────────────
PASO 4 — Corregir GAP #3: Agregar propiedades Premium a Vehicle.cs
────────────────────────────────

**Archivo:** `backend/VehiclesSaleService/VehiclesSaleService.Domain/Entities/Vehicle.cs`

Agrega las propiedades faltantes. Actualmente solo existe `IsFeatured`. Añade:

```csharp
// En la sección de METADATOS, después de IsFeatured:
public bool IsFeatured { get; set; } = false;       // ya existe
public bool IsPremium { get; set; } = false;         // ← NUEVO
public Guid? LinkedCampaignId { get; set; }          // ← NUEVO: vínculo con AdvertisingService
public DateTime? FeaturedUntil { get; set; }         // ← NUEVO: expiración automática
public int FeaturedPriority { get; set; } = 0;       // ← NUEVO: 100=premium, 50=featured, 0=normal

// Métodos de dominio (añadir como métodos de la clase Vehicle):
public void MarkAsPremium(Guid campaignId, DateTime? until = null, int priority = 100)
{
    IsPremium = true;
    LinkedCampaignId = campaignId;
    FeaturedUntil = until;
    FeaturedPriority = priority;
    UpdatedAt = DateTime.UtcNow;
}

public void MarkAsFeaturedByAdmin(int priority = 50)
{
    IsFeatured = true;
    FeaturedPriority = priority;
    UpdatedAt = DateTime.UtcNow;
}

public void ClearPromotion()
{
    IsFeatured = false;
    IsPremium = false;
    LinkedCampaignId = null;
    FeaturedUntil = null;
    FeaturedPriority = 0;
    UpdatedAt = DateTime.UtcNow;
}
```

**Crear migration EF Core:**

```bash
cd backend/VehiclesSaleService/VehiclesSaleService.Infrastructure

# Verifica el DbContext y herramienta
grep -rn "DbContext\|IDesignTimeDbContextFactory" . | grep -v "obj\|bin" | head -5

# Crea la migration (ajusta el nombre del DbContext si es necesario)
dotnet ef migrations add AddVehiclePremiumProperties \
  --project VehiclesSaleService.Infrastructure.csproj \
  --startup-project ../VehiclesSaleService.Api/VehiclesSaleService.Api.csproj \
  --context VehiclesSaleServiceDbContext
```

Si la herramienta `dotnet-ef` no está disponible localmente:

```bash
dotnet tool install --global dotnet-ef
```

Verifica que la migration generada incluye las 4 columnas nuevas:

```bash
cat Migrations/$(ls Migrations/ | grep AddVehiclePremiumProperties | head -1)
```

Las columnas deben ser: `IsPremium` (bool), `LinkedCampaignId` (uuid nullable),
`FeaturedUntil` (timestamp nullable), `FeaturedPriority` (int, default 0).

Si el DbContext usa `EnableAutoMigration: true` (appsettings.json), las migrations se aplican
al arrancar el servicio. Si no, aplica manualmente:

```bash
dotnet ef database update \
  --project VehiclesSaleService.Infrastructure.csproj \
  --startup-project ../VehiclesSaleService.Api/VehiclesSaleService.Api.csproj \
  --context VehiclesSaleServiceDbContext
```

────────────────────────────────
PASO 5 — Corregir GAP #4: RabbitMQ Consumer en VehiclesSaleService para eventos de campañas
────────────────────────────────

VehiclesSaleService ya tiene `RabbitMqEventPublisher` y `IEventPublisher`. Necesita un **consumer**
para recibir eventos del AdvertisingService.

**Paso 5.1 — Verificar estructura de consumers existente:**

```bash
# Ver si ya hay algún consumer en VehiclesSaleService
find backend/VehiclesSaleService/ -name "*.cs" | \
  xargs grep -l "IHostedService\|BackgroundService\|Consumer\|Subscribe" 2>/dev/null | \
  grep -v "obj\|bin"

# Ver cómo AdvertisingService publica sus eventos
grep -rn "ExchangeName\|exchange\|routing" \
  backend/AdvertisingService/AdvertisingService.Infrastructure/ | grep -v "obj\|bin" | head -20
```

**Paso 5.2 — Ver qué exchange/queue usa AdvertisingService para CampaignCreatedEvent:**

```bash
grep -rn "campaign.created\|advertising.campaign\|CampaignCreatedEvent" \
  backend/AdvertisingService/ | grep -v "obj\|bin" | head -20
```

**Paso 5.3 — Crear el consumer.** Busca si CarDealer.Shared tiene una base de consumer RabbitMQ:

```bash
find backend/_Shared/ -name "*.cs" | xargs grep -l "IConsumer\|RabbitMQ\|IHostedService" 2>/dev/null | \
  grep -v "obj\|bin" | head -10
```

Si existe una clase base (ej. `RabbitMqConsumerBase` o similar), úsala. Si no, implementa:

**Archivo:** `backend/VehiclesSaleService/VehiclesSaleService.Infrastructure/Messaging/CampaignEventsConsumer.cs`

```csharp
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using CarDealer.Contracts.Abstractions;
using VehiclesSaleService.Domain.Interfaces;

namespace VehiclesSaleService.Infrastructure.Messaging;

/// <summary>
/// Consumes campaign events from AdvertisingService to sync vehicle promotion status.
/// </summary>
public class CampaignEventsConsumer : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CampaignEventsConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private const string ExchangeName = "cardealer.events";
    private const string QueueName = "vehiclessaleservice.campaign-events";

    public CampaignEventsConsumer(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger<CampaignEventsConsumer> logger)
    {
        _services = services;
        _configuration = configuration;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitEnabled = _configuration.GetValue<bool>("RabbitMQ:Enabled");
        if (!rabbitEnabled)
        {
            _logger.LogInformation("RabbitMQ disabled. CampaignEventsConsumer will not start.");
            return Task.CompletedTask;
        }

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:Username"]
                    ?? throw new InvalidOperationException("RabbitMQ:Username not configured"),
                Password = _configuration["RabbitMQ:Password"]
                    ?? throw new InvalidOperationException("RabbitMQ:Password not configured"),
                VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);
            _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(QueueName, ExchangeName, "advertising.campaign.created");
            _channel.QueueBind(QueueName, ExchangeName, "advertising.campaign.expired");
            _channel.QueueBind(QueueName, ExchangeName, "advertising.campaign.cancelled");

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += HandleMessageAsync;
            _channel.BasicConsume(QueueName, autoAck: false, consumer: consumer);

            _logger.LogInformation("CampaignEventsConsumer started. Listening on queue: {Queue}", QueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start CampaignEventsConsumer");
        }

        return Task.CompletedTask;
    }

    private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs ea)
    {
        var body = Encoding.UTF8.GetString(ea.Body.Span);
        var routingKey = ea.RoutingKey;

        try
        {
            using var scope = _services.CreateScope();
            var vehicleRepo = scope.ServiceProvider.GetRequiredService<IVehicleRepository>();

            switch (routingKey)
            {
                case "advertising.campaign.created":
                    await HandleCampaignCreated(body, vehicleRepo);
                    break;
                case "advertising.campaign.expired":
                case "advertising.campaign.cancelled":
                    await HandleCampaignEnded(body, vehicleRepo);
                    break;
            }

            _channel!.BasicAck(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing campaign event {RoutingKey}: {Body}", routingKey, body);
            _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    private async Task HandleCampaignCreated(string body, IVehicleRepository vehicleRepo)
    {
        var evt = JsonSerializer.Deserialize<CampaignCreatedPayload>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (evt?.VehicleId == null) return;

        var vehicle = await vehicleRepo.GetByIdAsync(evt.VehicleId.Value);
        if (vehicle == null)
        {
            _logger.LogWarning("Vehicle {VehicleId} not found for campaign {CampaignId}",
                evt.VehicleId, evt.CampaignId);
            return;
        }

        // Si es PremiumSpot → IsPremium = true, si es FeaturedSpot → IsFeatured = true
        if (evt.PlacementType == "PremiumSpot")
            vehicle.MarkAsPremium(evt.CampaignId!.Value);
        else
            vehicle.MarkAsFeaturedByAdmin(priority: 50);

        await vehicleRepo.UpdateAsync(vehicle);
        _logger.LogInformation("Vehicle {VehicleId} promoted via campaign {CampaignId} [{PlacementType}]",
            evt.VehicleId, evt.CampaignId, evt.PlacementType);
    }

    private async Task HandleCampaignEnded(string body, IVehicleRepository vehicleRepo)
    {
        var evt = JsonSerializer.Deserialize<CampaignEndedPayload>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (evt?.VehicleId == null) return;

        var vehicle = await vehicleRepo.GetByIdAsync(evt.VehicleId.Value);
        if (vehicle == null) return;

        vehicle.ClearPromotion();
        await vehicleRepo.UpdateAsync(vehicle);
        _logger.LogInformation("Vehicle {VehicleId} promotion cleared (campaign ended)", evt.VehicleId);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}

// Payloads para deserializar los eventos RabbitMQ
file record CampaignCreatedPayload(Guid? CampaignId, Guid? VehicleId, string PlacementType);
file record CampaignEndedPayload(Guid? CampaignId, Guid? VehicleId);
```

> **Nota:** Antes de crear la queue `vehiclessaleservice.campaign-events`, verifica si ya existe
> en RabbitMQ con argumentos diferentes para no causar conflicto:
>
> ```bash
> kubectl exec deployment/rabbitmq -n okla -- \
>   rabbitmqctl list_queues name durable arguments | grep campaign
> # Si existe con argumentos distintos, elimínala primero:
> kubectl exec deployment/rabbitmq -n okla -- \
>   rabbitmqctl delete_queue vehiclessaleservice.campaign-events
> ```

**Paso 5.4 — Registrar el consumer en DI:**

**Archivo:** `backend/VehiclesSaleService/VehiclesSaleService.Api/Program.cs`

```csharp
// Añadir después de los registros existentes de HostedServices:
builder.Services.AddHostedService<CampaignEventsConsumer>();
```

────────────────────────────────
PASO 6 — Verificar que VehiclesSaleService expone los campos nuevos en GET /api/vehicles/{id}
────────────────────────────────

El `VehicleServiceClient` de AdvertisingService llama a `GET /api/vehicles/{vehicleId}`.
Verifica que el response de este endpoint incluye los campos que añadiste a `VehicleBasicInfo`:

```bash
# Busca el DTO de respuesta del endpoint GET /api/vehicles/{id}
grep -rn "VehicleDto\|GetVehicleByIdQuery\|VehicleResponse" \
  backend/VehiclesSaleService/VehiclesSaleService.Application/Features/ | grep -v "obj\|bin" | head -20
```

Busca dónde se mapea `Vehicle` → `VehicleDto` (puede ser AutoMapper, Mapster, o manual).
Agrega el mapeo para:

- `PrimaryImageUrl` ← primera imagen en `vehicle.Images.OrderBy(i => i.Order).FirstOrDefault()?.Url`
- `Slug` ← campo slug del vehículo (buscar en Vehicle.cs si existe, si no: `$"{vehicle.Make}-{vehicle.Model}-{vehicle.Year}-{vehicle.Id:N}".ToLower()`)
- `Location` ← campo de ciudad/provincia (buscar en Vehicle.cs: `City`, `Province`, `Location`)
- `IsPremium` ← `vehicle.IsPremium`
- `IsFeatured` ← `vehicle.IsFeatured`
- `Currency` ← `vehicle.Currency` si existe, default "DOP"

Ajusta los nombres de campo según lo que realmente existe en Vehicle.cs. Usa:

```bash
cat backend/VehiclesSaleService/VehiclesSaleService.Domain/Entities/Vehicle.cs | head -200
```

────────────────────────────────
PASO 7 — Compilar y verificar sin errores
────────────────────────────────

```bash
# Compilar AdvertisingService
cd backend/AdvertisingService
dotnet build AdvertisingService.sln --configuration Release 2>&1 | tail -20

# Compilar VehiclesSaleService
cd backend/VehiclesSaleService
dotnet build VehiclesSaleService.sln --configuration Release 2>&1 | tail -20
```

Corrige todos los errores de compilación antes de continuar.

────────────────────────────────
PASO 8 — Commit y Push para CI/CD (build amd64)
────────────────────────────────

```bash
cd /path/to/cardealer-microservices

git add \
  backend/AdvertisingService/AdvertisingService.Application/DTOs/HomepageRotationDto.cs \
  backend/AdvertisingService/AdvertisingService.Application/Clients/VehicleServiceClient.cs \
  backend/AdvertisingService/AdvertisingService.Application/Features/Rotation/Queries/GetHomepageRotation/GetHomepageRotationQueryHandler.cs \
  backend/VehiclesSaleService/VehiclesSaleService.Domain/Entities/Vehicle.cs \
  backend/VehiclesSaleService/VehiclesSaleService.Infrastructure/Messaging/CampaignEventsConsumer.cs \
  backend/VehiclesSaleService/VehiclesSaleService.Api/Program.cs \
  backend/VehiclesSaleService/VehiclesSaleService.Infrastructure/Migrations/

git commit -m "feat(homepage): enrich rotation dto with vehicle details + premium properties + campaign sync

- RotatedVehicleDto: add title, slug, imageUrl, price, currency, location, isFeatured, isPremium
- VehicleBasicInfo: add PrimaryImageUrl, Slug, Location, Currency, IsFeatured, IsPremium
- GetHomepageRotationQueryHandler: enrich rotation with VehicleServiceClient in parallel
- Vehicle.cs: add IsPremium, LinkedCampaignId, FeaturedUntil, FeaturedPriority + domain methods
- CampaignEventsConsumer: BackgroundService consuming campaign.created/expired from RabbitMQ
- Program.cs: register CampaignEventsConsumer as HostedService
- Migration: AddVehiclePremiumProperties"

git push origin main
```

Observa el CI/CD en GitHub Actions:

```bash
gh run list --limit 3
gh run watch  # o abre: https://github.com/gregorymorenoiem/cardealer-microservices/actions
```

> ⚠️ El CI/CD construye imágenes `linux/amd64` automáticamente. NO fuerces `arm64` ni `multi-platform`
> salvo que el workflow lo indique explícitamente. Los nodos DOKS son siempre `linux/amd64`.

Si el build no refleja los cambios (caché de Docker buildx):

```bash
for SVC in advertisingservice vehiclessaleservice; do
  gh cache list --key "Linux-buildx-${SVC}" | awk '{print $1}' | \
    xargs -I{} gh cache delete {} 2>/dev/null || true
done
```

────────────────────────────────
PASO 9 — Verificar deployment en producción
────────────────────────────────

Espera a que los pods estén corriendo con la nueva imagen:

```bash
kubectl rollout status deploy/advertisingservice -n okla --timeout=180s
kubectl rollout status deploy/vehiclessaleservice -n okla --timeout=180s
```

Verifica la migration en vehiclessaleservice_db:

```bash
kubectl exec -n okla statefulset/postgres -- \
  psql -U postgres -d vehiclessaleservice_db -c \
  "SELECT column_name, data_type FROM information_schema.columns
   WHERE table_name = 'Vehicles'
   AND column_name IN ('IsPremium', 'LinkedCampaignId', 'FeaturedUntil', 'FeaturedPriority');"
```

Deben aparecer las 4 columnas nuevas.

────────────────────────────────
PASO 10 — Prueba E2E del endpoint de rotación
────────────────────────────────

```bash
# Test endpoint FeaturedSpot (puede retornar lista vacía si no hay campañas activas)
curl -s https://okla.com.do/api/advertising/rotation/FeaturedSpot | jq .

# Test endpoint PremiumSpot
curl -s https://okla.com.do/api/advertising/rotation/PremiumSpot | jq .
```

**Response esperado** (con o sin items):

```json
{
  "success": true,
  "data": {
    "section": "FeaturedSpot",
    "items": [
      {
        "vehicleId": "uuid-aqui",
        "campaignId": "uuid-aqui",
        "position": 1,
        "qualityScore": 0.95,
        "title": "Toyota Corolla 2022",
        "slug": "toyota-corolla-2022-...",
        "imageUrl": "https://...",
        "price": 900000,
        "currency": "DOP",
        "location": "Santo Domingo",
        "isFeatured": true,
        "isPremium": false
      }
    ],
    "generatedAt": "2026-02-23T..."
  }
}
```

Si `items` está vacío: es correcto si no hay campañas activas en DB. Crea una de prueba:

```bash
# Obtén un vehicleId existente
kubectl exec -n okla statefulset/postgres -- \
  psql -U postgres -d vehiclessaleservice_db -t -c \
  "SELECT id FROM \"Vehicles\" WHERE \"Status\" = 1 LIMIT 1;" | tr -d ' '

# Obtén token de admin
ADMIN_TOKEN=$(curl -s -X POST https://okla.com.do/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@okla.local","password":"Admin123!@#"}' | jq -r '.data.access_token // .access_token')

# Obtén un userId de dealer para la campaña
DEALER_ID=$(kubectl exec -n okla statefulset/postgres -- \
  psql -U postgres -d userservice_db -t -c \
  "SELECT id FROM users WHERE account_type = 'Dealer' LIMIT 1;" | tr -d ' ')

VEHICLE_ID="<vehicleId-del-paso-anterior>"

# Crea campaña de prueba (FeaturedSpot, PerDay, budget mínimo)
curl -s -X POST https://okla.com.do/api/advertising/campaigns \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d "{
    \"vehicleId\": \"$VEHICLE_ID\",
    \"ownerId\": \"$DEALER_ID\",
    \"ownerType\": \"Dealer\",
    \"placementType\": 0,
    \"pricingModel\": 1,
    \"totalBudget\": 50,
    \"startDate\": \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\",
    \"endDate\": \"$(date -u -v+7d +%Y-%m-%dT%H:%M:%SZ)\"
  }" | jq .
```

Después de crear la campaña, fuerza un refresh de la rotación:

```bash
curl -s -X POST "https://okla.com.do/api/advertising/rotation/refresh?section=FeaturedSpot" \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq .

# Verifica que ahora aparece con datos completos:
curl -s https://okla.com.do/api/advertising/rotation/FeaturedSpot | jq '.data.items[0]'
```

El item debe tener `title`, `imageUrl`, `price` populados (no null).

────────────────────────────────
PASO 11 — Verificar visualmente en homepage
────────────────────────────────

Abre `https://okla.com.do/` en modo incógnito y verifica:

1. ✅ La sección "⭐ Vehículos Destacados" aparece con tarjetas que tienen imagen, título y precio
2. ✅ La sección "💎 Vehículos Premium" aparece (si hay campañas PremiumSpot activas)
3. ✅ Las tarjetas NO aparecen vacías (sin imagen, sin texto)
4. ✅ El badge "💎 Premium" aparece en tarjetas de PremiumSpot
5. ✅ El badge "⭐ Destacado" aparece en tarjetas de FeaturedSpot
6. ✅ En la consola del browser NO hay errores 404 en `/api/advertising/rotation/*`
7. ✅ Las requests de tracking se disparan: `POST /api/advertising/tracking/impression`

Si los badges no aparecen, verifica en el componente `FeaturedVehicleCard` de
`frontend/web-next/src/components/advertising/featured-vehicles.tsx` que las propiedades
`vehicle.isPremium` y `vehicle.isFeatured` llegan con los valores correctos desde el backend.

────────────────────────────────
PASO 12 — Bugs adicionales encontrados durante implementación
────────────────────────────────

Por cada bug adicional encontrado:

a) Identifica el archivo afectado (ruta completa)
b) Describe la causa raíz en 1-2 líneas
c) Aplica el fix mínimo
d) Commit separado: `fix(<servicio>): <descripción corta>`
e) Push → espera CI/CD

**Reglas de fixes en K8s:**

```bash
# Si falla un pod post-deploy:
kubectl rollout restart deploy/<servicio> -n okla
kubectl rollout status deploy/<servicio> -n okla
kubectl logs -f deploy/<servicio> -n okla --tail=100

# Si hay DI failure (CrashLoopBackOff):
kubectl logs deploy/<servicio> -n okla --previous | grep -i "unable to resolve\|not registered\|DI\|injection"
# → Revisa Program.cs y registra el servicio faltante
```

══════════════════════════════════════════════════════════════
ERRORES CONOCIDOS A VALIDAR DURANTE IMPLEMENTACIÓN
══════════════════════════════════════════════════════════════

- [ ] **E-001:** `RotatedVehicleDto` no serializa `Vehicles` como `items` → verificar `JsonPropertyName` o rename
- [ ] **E-002:** `VehicleServiceClient` timeout al enriquecer 8 vehículos en paralelo → agregar `CancellationTokenSource` con timeout de 2 segundos por request
- [ ] **E-003:** `CampaignEventsConsumer` falla DI startup por `IVehicleRepository` no registrado → verificar scope lifetime
- [ ] **E-004:** Migration falla si la tabla `Vehicles` tiene filas activas → verifica que las nuevas columnas tienen defaults (IsPremium DEFAULT false, FeaturedPriority DEFAULT 0)
- [ ] **E-005:** Queue `vehiclessaleservice.campaign-events` tiene args conflictivos → delete antes de crear
- [ ] **E-006:** `GetHomepageRotationQueryHandler` no tiene `VehicleServiceClient` inyectado → agregar a DI en AdvertisingService Program.cs si no está

══════════════════════════════════════════════════════════════
CHECKLIST FINAL — VERIFICA CADA PUNTO
══════════════════════════════════════════════════════════════

### Backend (10 puntos)

- [ ] 1. `RotatedVehicleDto` incluye: title, slug, imageUrl, price, currency, location, isFeatured, isPremium
- [ ] 2. `VehicleBasicInfo` incluye: PrimaryImageUrl, Slug, Location, Currency, IsFeatured, IsPremium
- [ ] 3. `GetHomepageRotationQueryHandler` enriquece en paralelo con `VehicleServiceClient`
- [ ] 4. `GET /api/advertising/rotation/FeaturedSpot` retorna items con title e imageUrl populados (con campaña activa)
- [ ] 5. `Vehicle.cs` tiene: IsPremium, LinkedCampaignId, FeaturedUntil, FeaturedPriority + métodos MarkAsPremium/ClearPromotion
- [ ] 6. Migration `AddVehiclePremiumProperties` aplicada en producción (4 columnas nuevas en tabla Vehicles)
- [ ] 7. `CampaignEventsConsumer` registrado como HostedService en VehiclesSaleService.Program.cs
- [ ] 8. Cuando se crea una campaña, el vehículo se marca IsPremium=true en vehiclessaleservice_db
- [ ] 9. `advertisingservice` pod Running post-deploy, sin errores en logs
- [ ] 10. `vehiclessaleservice` pod Running post-deploy, sin errores en logs

### Frontend (4 puntos)

- [ ] 11. Sección "⭐ Vehículos Destacados" visible en homepage con tarjetas completas
- [ ] 12. Tarjetas muestran imagen, título y precio (no vacías)
- [ ] 13. Badge "💎 Premium" visible cuando `isPremium = true`
- [ ] 14. Tracking `POST /api/advertising/tracking/impression` se dispara (Network tab del browser)

### Integridad del sistema (3 puntos)

- [ ] 15. Health checks pasando: `curl https://okla.com.do/api/health` retorna Healthy
- [ ] 16. No hay errores 500 en logs de gateway post-deploy
- [ ] 17. CI/CD build pasó con plataforma `linux/amd64` (verificar en GitHub Actions)

══════════════════════════════════════════════════════════════
ENTREGABLES FINALES
══════════════════════════════════════════════════════════════

1. **Informe Markdown:**
   - Archivo: `REPORT_IMPLEMENTACION_DESTACADOS_YYYYMMDD.md`
   - Contenido:
     - Fecha y hora inicio/fin
     - Estado de cada GAP: ✅ resuelto / ⚠️ parcial / ❌ bloqueado
     - Checklist final con evidencia (curl output, SQL queries, logs)
     - Lista de commits con hash y descripción
     - Bugs adicionales encontrados: archivo, causa, fix / pendiente
     - Tiempo total de implementación

2. **PR (si hay cambios significativos):**
   - Rama: `feat/homepage-featured-vehicles-enrichment`
   - Título: `feat(homepage): vehicle rotation enrichment + premium properties + campaign sync`
   - Descripción: enlace al informe + lista de gaps resueltos

3. **PR de bugs adicionales (si los hay):**
   - Rama: `fix/homepage-rotation-bugs-YYYYMMDD`
   - Commits atómicos por bug

══════════════════════════════════════════════════════════════
TIMELINE ESTIMADO
══════════════════════════════════════════════════════════════

- Verificación inicial (PASO 1): 5 min
- GAP #2 + #1 (RotatedVehicleDto enriquecimiento): 30-45 min
- GAP #3 (Vehicle.cs + migration): 20-30 min
- GAP #4 (CampaignEventsConsumer): 45-60 min
- Build + Deploy + Verificación: 20-30 min
- Prueba E2E visual homepage: 15 min
- Informe + PR: 15 min
- **TOTAL:** 2.5 - 3.5 horas

══════════════════════════════════════════════════════════════
COMANDOS DE REFERENCIA RÁPIDA
══════════════════════════════════════════════════════════════

```bash
# Ver logs en tiempo real
kubectl logs -f deploy/advertisingservice -n okla
kubectl logs -f deploy/vehiclessaleservice -n okla

# Forzar redeploy si la imagen no cambió pero hay problema
kubectl rollout restart deploy/advertisingservice -n okla
kubectl rollout restart deploy/vehiclessaleservice -n okla

# Verificar columnas nuevas en Vehicle
kubectl exec -n okla statefulset/postgres -- \
  psql -U postgres -d vehiclessaleservice_db -c \
  "\d \"Vehicles\"" | grep -E "IsPremium|LinkedCampaign|FeaturedUntil|FeaturedPriority"

# Verificar que el consumer está consumiendo (debe aparecer la queue)
kubectl exec deployment/rabbitmq -n okla -- \
  rabbitmqctl list_queues name consumers messages | grep campaign

# Verificar rotación con datos (después de crear campaña de prueba)
curl -s https://okla.com.do/api/advertising/rotation/FeaturedSpot | \
  jq '.data.items[] | {vehicleId, title, imageUrl, price, isFeatured}'

# Ver qué campañas activas hay
kubectl exec -n okla statefulset/postgres -- \
  psql -U postgres -d advertisingservice_db -c \
  "SELECT id, vehicle_id, placement_type, status, start_date, end_date
   FROM \"AdCampaigns\" WHERE status = 'Active' ORDER BY created_at DESC LIMIT 5;"
```

══════════════════════════════════════════════════════════════
EMPIEZA AHORA
══════════════════════════════════════════════════════════════

Comienza con PASO 1 (cluster health), luego procede secuencialmente.
Al terminar cada paso, documenta resultado (✅ o ❌ + evidencia).

Referencias si necesitas más contexto:

- `AUDITORIA_FLUJO_VEHICULOS_DESTACADOS.md` — análisis detallado de los gaps
- `DIAGRAMA_TECNICO_FLUJO_DESTACADOS.md` — diagramas de arquitectura
- `docs/ARCHITECTURE.md` — arquitectura general del sistema
- `docs/KUBERNETES.md` — comandos K8s y gestión de secrets

¡Adelante! 🚀
