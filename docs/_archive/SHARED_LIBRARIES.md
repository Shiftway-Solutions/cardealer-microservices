# 📚 Librerías Compartidas - CarDealer Microservicios

Este documento describe las 12 librerías compartidas disponibles en el proyecto CarDealer para estandarizar funcionalidades transversales entre microservicios.

## 📦 Librerías Disponibles

| #   | Librería                       | Descripción                       | Estado        |
| --- | ------------------------------ | --------------------------------- | ------------- |
| 1   | CarDealer.Shared.Logging       | Serilog → Seq logging             | ✅ Productivo |
| 2   | CarDealer.Shared.ErrorHandling | Global Exception Middleware       | ✅ Productivo |
| 3   | CarDealer.Shared.Observability | OpenTelemetry → Jaeger            | ✅ Productivo |
| 4   | CarDealer.Shared.RateLimiting  | Rate limiting con Redis           | ✅ Productivo |
| 5   | CarDealer.Shared.Idempotency   | Protección de pagos               | ✅ Productivo |
| 6   | CarDealer.Shared.FeatureFlags  | Cliente para FeatureToggleService | ✅ Productivo |
| 7   | CarDealer.Shared.Audit         | Publisher para AuditService       | ✅ Productivo |
| 8   | CarDealer.Shared.HealthChecks  | Health checks estándar            | ✅ Nuevo      |
| 9   | CarDealer.Shared.Resilience    | Polly resilience patterns         | ✅ Nuevo      |
| 10  | CarDealer.Shared.ApiVersioning | API versioning                    | ✅ Nuevo      |
| 11  | CarDealer.Shared.Sagas         | MassTransit Sagas                 | ✅ Nuevo      |

---

## 1️⃣ CarDealer.Shared.Logging

**Propósito:** Logging estructurado con Serilog hacia Seq.

### Uso

```csharp
// Program.cs
using CarDealer.Shared.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseStandardSerilog("MiServicio");
```

### Configuración (appsettings.json)

```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "Seq", "Args": { "serverUrl": "http://seq:5341" } }
    ]
  }
}
```

---

## 2️⃣ CarDealer.Shared.ErrorHandling

**Propósito:** Middleware global para captura y formato estándar de errores.

### Uso

```csharp
// Program.cs
using CarDealer.Shared.ErrorHandling.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddStandardErrorHandling();

var app = builder.Build();
app.UseGlobalErrorHandling();
```

---

## 3️⃣ CarDealer.Shared.Observability

**Propósito:** OpenTelemetry para traces distribuidos hacia Jaeger.

### Uso

```csharp
using CarDealer.Shared.Observability.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddStandardObservability(builder.Configuration);
```

### Configuración

```json
{
  "OpenTelemetry": {
    "ServiceName": "MiServicio",
    "JaegerEndpoint": "http://jaeger:4317"
  }
}
```

---

## 4️⃣ CarDealer.Shared.RateLimiting

**Propósito:** Rate limiting con soporte Redis para ambiente distribuido.

### Uso

```csharp
using CarDealer.Shared.RateLimiting.Extensions;

builder.Services.AddRateLimiting(builder.Configuration);
app.UseRateLimiter();
```

---

## 5️⃣ CarDealer.Shared.Idempotency

**Propósito:** Protección de idempotencia para operaciones de pago.

### Uso

```csharp
[HttpPost]
[Idempotent] // Atributo para proteger el endpoint
public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
{
    // ...
}
```

---

## 6️⃣ CarDealer.Shared.FeatureFlags

**Propósito:** Cliente para FeatureToggleService.

### Uso

```csharp
using CarDealer.Shared.FeatureFlags;

public class MiController
{
    private readonly IFeatureFlagService _features;

    public async Task<IActionResult> Get()
    {
        if (await _features.IsEnabledAsync("nueva-funcionalidad"))
        {
            // Nueva funcionalidad
        }
    }
}
```

---

## 7️⃣ CarDealer.Shared.Audit

**Propósito:** Publisher de eventos de auditoría hacia AuditService.

### Uso

```csharp
using CarDealer.Shared.Audit.Extensions;

// Program.cs
builder.Services.AddAuditPublisher(builder.Configuration);
app.UseAuditMiddleware();

// Controller
[Audit("AUTH_LOGIN", "Login", ResourceType = "User", Severity = AuditSeverity.Warning)]
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    // ...
}
```

### Configuración

```json
{
  "Audit": {
    "RabbitMq": {
      "Host": "rabbitmq",
      "Port": 5672,
      "Username": "guest",
      "Password": "guest"
    }
  }
}
```

---

## 8️⃣ CarDealer.Shared.HealthChecks

**Propósito:** Health checks estándar para PostgreSQL, Redis, RabbitMQ y custom checks.

### Uso

```csharp
using CarDealer.Shared.HealthChecks.Extensions;

// Program.cs
builder.Services.AddStandardHealthChecks(builder.Configuration);

app.MapStandardHealthChecks();
```

### Configuración

```json
{
  "HealthChecks": {
    "Enabled": true,
    "PostgresConnectionString": "Host=...",
    "RedisConnectionString": "redis:6379",
    "RabbitMqConnectionString": "amqp://guest:guest@rabbitmq:5672",
    "MemoryThresholdBytes": 1073741824,
    "ExternalServices": {
      "AuthService": "http://authservice:8080/health"
    }
  }
}
```

### Endpoints Generados

- `GET /health` - Health check básico
- `GET /health/ready` - Readiness probe
- `GET /health/live` - Liveness probe
- `GET /health/ui` - Health Check UI

---

## 9️⃣ CarDealer.Shared.Resilience

**Propósito:** Patrones de resiliencia con Polly (retry, circuit breaker, timeout).

### Uso

```csharp
using CarDealer.Shared.Resilience.Extensions;

// HttpClient con resiliencia
builder.Services.AddResilientHttpClient<IVehicleClient, VehicleClient>(
    builder.Configuration,
    clientName: "vehicles",
    configureClient: client =>
    {
        client.BaseAddress = new Uri("http://vehicleservice:8080");
    });

// O agregar a un HttpClient existente
builder.Services.AddHttpClient<IPaymentClient, PaymentClient>()
    .AddStandardResilience(builder.Configuration);
```

### Configuración

```json
{
  "Resilience": {
    "Enabled": true,
    "Retry": {
      "MaxRetries": 3,
      "DelaySeconds": 2,
      "UseExponentialBackoff": true,
      "UseJitter": true,
      "RetryStatusCodes": [500, 502, 503, 504]
    },
    "CircuitBreaker": {
      "FailureRatio": 0.5,
      "MinimumThroughput": 10,
      "SamplingDurationSeconds": 30,
      "BreakDurationSeconds": 30
    },
    "Timeout": {
      "TimeoutSeconds": 10,
      "TotalTimeoutSeconds": 30
    }
  }
}
```

---

## 🔟 CarDealer.Shared.ApiVersioning

**Propósito:** API versioning con soporte para URL, header y query string.

### Uso

```csharp
using CarDealer.Shared.ApiVersioning.Extensions;
using CarDealer.Shared.ApiVersioning.Attributes;

// Program.cs
builder.Services.AddStandardApiVersioning(builder.Configuration);
builder.Services.AddVersionedSwagger(builder.Configuration);

var app = builder.Build();
app.UseVersionedSwagger(app.Services.GetRequiredService<IApiVersionDescriptionProvider>());

// Controller con versiones
[ApiController]
[ApiV1]
[Route("api/v{version:apiVersion}/[controller]")]
public class VehiclesController : ControllerBase
{
    [HttpGet]
    public IActionResult GetV1() => Ok("Version 1");
}

[ApiController]
[ApiV2]
[Route("api/v{version:apiVersion}/[controller]")]
public class VehiclesV2Controller : ControllerBase
{
    [HttpGet]
    public IActionResult GetV2() => Ok("Version 2");
}
```

### Configuración

```json
{
  "ApiVersioning": {
    "Enabled": true,
    "DefaultVersion": "1.0",
    "AssumeDefaultVersionWhenUnspecified": true,
    "ReportApiVersions": true,
    "VersionReader": {
      "ReadFromQueryString": true,
      "QueryStringParameter": "api-version",
      "ReadFromHeader": true,
      "HeaderName": "X-Api-Version",
      "ReadFromUrl": true
    },
    "Swagger": {
      "Title": "CarDealer API",
      "Description": "API de microservicios CarDealer"
    }
  }
}
```

### URLs de Ejemplo

```
GET /api/v1.0/vehicles
GET /api/v2.0/vehicles
GET /api/vehicles?api-version=1.0
GET /api/vehicles (Header: X-Api-Version: 1.0)
```

---

## 1️⃣1️⃣ CarDealer.Shared.Sagas

**Propósito:** MassTransit Saga State Machine para transacciones distribuidas.

### Uso

```csharp
using CarDealer.Shared.Sagas.Extensions;
using CarDealer.Shared.Sagas.Contracts;

// Program.cs
builder.Services.AddMassTransitWithSagas(builder.Configuration);

// O con consumers personalizados
builder.Services.AddMassTransitWithSagas(builder.Configuration, x =>
{
    x.AddConsumer<MyConsumer>();
});
```

### Configuración

```json
{
  "Sagas": {
    "Enabled": true,
    "RabbitMq": {
      "Host": "rabbitmq",
      "Port": 5672,
      "VirtualHost": "/",
      "Username": "guest",
      "Password": "guest",
      "QueuePrefix": "cardealer"
    },
    "Repository": {
      "Type": "EntityFramework",
      "RedisConnectionString": "redis:6379",
      "RedisKeyPrefix": "saga:"
    },
    "Retry": {
      "MaxRetries": 3,
      "IntervalSeconds": 5,
      "UseExponentialBackoff": true
    },
    "Outbox": {
      "Enabled": true,
      "CleanupIntervalSeconds": 30,
      "DeliveredMessageTtlHours": 24
    }
  }
}
```

### Saga de Ejemplo: Orden de Vehículo

```
┌──────────────────────────────────────────────────────────────────────────┐
│                    SAGA: Order Processing                                │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  SubmitOrder                                                             │
│      │                                                                   │
│      ▼                                                                   │
│  [Submitted] ─────► ReserveVehicle ────► VehicleService                 │
│      │                                                                   │
│      │ VehicleReserved                                                  │
│      ▼                                                                   │
│  [PaymentPending] ─► ProcessPayment ────► PaymentService                │
│      │                                                                   │
│      │ PaymentCompleted                 PaymentFailed                   │
│      ▼                                      │                           │
│  [Completed] ◄────────────────────────┐     │                           │
│      │                                │     ▼                           │
│      │                                │ [Faulted]                       │
│      │                                │     │                           │
│      │                                │     │ ReleaseVehicle (COMPENSAR)│
│      │                                │     ▼                           │
│      │                                │ [Cancelled]                     │
│      │                                │     │                           │
│      ▼                                │     ▼                           │
│  OrderCompleted                       │  OrderCancelled                 │
│                                       │                                  │
└──────────────────────────────────────────────────────────────────────────┘
```

### Contracts Disponibles

```csharp
// Comandos
SubmitOrder { CustomerId, VehicleId, Amount, PaymentMethod }
ReserveVehicle { OrderId, VehicleId }
ProcessPayment { OrderId, Amount, PaymentMethod }
ReleaseVehicle { VehicleId, Reason } // Compensación

// Eventos
OrderAccepted { OrderId }
VehicleReserved { VehicleId, ReservedUntil }
VehicleReservationFailed { ErrorCode, ErrorMessage }
PaymentCompleted { PaymentId, TransactionId }
PaymentFailed { ErrorCode, ErrorMessage }
VehicleReleased { VehicleId }
OrderCompleted { OrderId, VehicleId, PaymentId }
OrderCancelled { OrderId, Reason }
```

---

## 📁 Ubicación de las Librerías

```
backend/
└── _Shared/
    ├── CarDealer.Shared.Logging/
    ├── CarDealer.Shared.ErrorHandling/
    ├── CarDealer.Shared.Observability/
    ├── CarDealer.Shared.RateLimiting/
    ├── CarDealer.Shared.Idempotency/
    ├── CarDealer.Shared.FeatureFlags/
    ├── CarDealer.Shared.Audit/
    ├── CarDealer.Shared.HealthChecks/    ← NUEVO
    ├── CarDealer.Shared.Resilience/       ← NUEVO
    ├── CarDealer.Shared.ApiVersioning/    ← NUEVO
    └── CarDealer.Shared.Sagas/            ← NUEVO
```

---

## 🔗 Cómo Referenciar en un Microservicio

```xml
<!-- MiServicio.Api.csproj -->
<ItemGroup>
  <ProjectReference Include="..\..\..\_Shared\CarDealer.Shared.Logging\CarDealer.Shared.Logging.csproj" />
  <ProjectReference Include="..\..\..\_Shared\CarDealer.Shared.ErrorHandling\CarDealer.Shared.ErrorHandling.csproj" />
  <ProjectReference Include="..\..\..\_Shared\CarDealer.Shared.Observability\CarDealer.Shared.Observability.csproj" />
  <ProjectReference Include="..\..\..\_Shared\CarDealer.Shared.HealthChecks\CarDealer.Shared.HealthChecks.csproj" />
  <ProjectReference Include="..\..\..\_Shared\CarDealer.Shared.Resilience\CarDealer.Shared.Resilience.csproj" />
  <ProjectReference Include="..\..\..\_Shared\CarDealer.Shared.ApiVersioning\CarDealer.Shared.ApiVersioning.csproj" />
  <!-- Agregar según necesidad -->
</ItemGroup>
```

---

## ⚡ Ejemplo Completo de Program.cs

```csharp
using CarDealer.Shared.Logging.Extensions;
using CarDealer.Shared.ErrorHandling.Extensions;
using CarDealer.Shared.Observability.Extensions;
using CarDealer.Shared.HealthChecks.Extensions;
using CarDealer.Shared.Resilience.Extensions;
using CarDealer.Shared.ApiVersioning.Extensions;
using CarDealer.Shared.Audit.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Host.UseStandardSerilog("MiServicio");

// Services
builder.Services.AddControllers();
builder.Services.AddStandardErrorHandling();
builder.Services.AddStandardObservability(builder.Configuration);
builder.Services.AddStandardHealthChecks(builder.Configuration);
builder.Services.AddStandardApiVersioning(builder.Configuration);
builder.Services.AddVersionedSwagger(builder.Configuration);
builder.Services.AddAuditPublisher(builder.Configuration);

// HttpClient con resiliencia
builder.Services.AddResilientHttpClient<IVehicleClient, VehicleClient>(
    builder.Configuration);

var app = builder.Build();

// Middleware (orden importa)
app.UseGlobalErrorHandling();
app.UseAuditMiddleware();

app.UseVersionedSwagger(app.Services.GetRequiredService<IApiVersionDescriptionProvider>());

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapStandardHealthChecks();

app.Run();
```

---

**Última actualización:** Enero 2026  
**Autor:** CarDealer Team
