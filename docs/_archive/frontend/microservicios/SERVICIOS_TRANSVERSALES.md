# 🔧 Servicios Transversales (Cross-Cutting Services)

**Proyecto:** OKLA - CarDealer Microservices  
**Última actualización:** Enero 19, 2026  
**Autor:** Equipo de Arquitectura

---

## 📋 Índice

1. [Introducción](#introducción)
2. [Lista de Servicios Transversales](#lista-de-servicios-transversales)
3. [Arquitectura de Integración](#arquitectura-de-integración)
4. [Detalle por Servicio](#detalle-por-servicio)
5. [Patrones de Comunicación](#patrones-de-comunicación)
6. [Cómo Integrar en Nuevos Servicios](#cómo-integrar-en-nuevos-servicios)

---

## Introducción

Los **servicios transversales** son microservicios que proporcionan funcionalidades comunes que otros servicios consumen. En lugar de duplicar lógica en cada microservicio, centralizamos estas capacidades en servicios dedicados.

### Beneficios

- ✅ **Consistencia:** Todos los servicios usan la misma implementación
- ✅ **Mantenibilidad:** Cambios en un solo lugar afectan a todos
- ✅ **Observabilidad:** Logs, errores y trazas centralizados
- ✅ **Seguridad:** Autenticación y autorización unificadas
- ✅ **Performance:** Cache distribuido compartido

---

## Lista de Servicios Transversales

### 🔴 Prioridad Crítica (Todos deben usarlo)

| #   | Servicio           | Puerto | Tipo           | Función Principal                        |
| --- | ------------------ | ------ | -------------- | ---------------------------------------- |
| 1   | **Gateway**        | 18443  | Routing        | API Gateway, entrada única, routing      |
| 2   | **AuthService**    | 15001  | Seguridad      | JWT, tokens, autenticación               |
| 3   | **LoggingService** | 15050  | Observabilidad | Logs centralizados (Seq/ELK)             |
| 4   | **ErrorService**   | 15009  | Observabilidad | Errores centralizados, Dead Letter Queue |

### 🟠 Prioridad Alta (Recomendado para todos)

| #   | Servicio                 | Puerto | Tipo        | Función Principal                     |
| --- | ------------------------ | ------ | ----------- | ------------------------------------- |
| 5   | **CacheService**         | 15051  | Performance | Cache distribuido (Redis abstraction) |
| 6   | **ConfigurationService** | 15052  | Config      | Feature flags, configuración dinámica |

### 🟡 Prioridad Media (Según contexto)

| #   | Servicio                | Puerto | Tipo           | Función Principal     | Quién lo usa   |
| --- | ----------------------- | ------ | -------------- | --------------------- | -------------- |
| 7   | **TracingService**      | 15053  | Observabilidad | Distributed tracing   | Recomendado    |
| 8   | **AuditService**        | 15054  | Compliance     | Auditoría de acciones | Críticos       |
| 9   | **RateLimitingService** | 15055  | Protección     | Límite de requests    | Gateway + APIs |
| 10  | **IdempotencyService**  | 15056  | Protección     | Evitar duplicados     | Pagos          |

---

## Arquitectura de Integración

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              CAPA DE ENTRADA                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                          ┌─────────────────┐                                │
│           Internet ───►  │     Gateway     │ ◄─── RateLimitingService       │
│                          │    (Ocelot)     │                                │
│                          └────────┬────────┘                                │
│                                   │                                         │
│                          ┌────────▼────────┐                                │
│                          │   AuthService   │ ◄─── JWT Validation            │
│                          └────────┬────────┘                                │
└───────────────────────────────────┼─────────────────────────────────────────┘
                                    │
┌───────────────────────────────────┼─────────────────────────────────────────┐
│                          SERVICIOS DE NEGOCIO                               │
├───────────────────────────────────┼─────────────────────────────────────────┤
│                                   ▼                                         │
│   ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐       │
│   │VehiclesSale │  │   Billing   │  │    User     │  │   Dealer    │       │
│   │  Service    │  │  Service    │  │  Service    │  │  Service    │       │
│   └──────┬──────┘  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘       │
│          │                │                │                │               │
│          └────────────────┴────────────────┴────────────────┘               │
│                                   │                                         │
└───────────────────────────────────┼─────────────────────────────────────────┘
                                    │
┌───────────────────────────────────┼─────────────────────────────────────────┐
│                          CAPA DE OBSERVABILIDAD                             │
├───────────────────────────────────┼─────────────────────────────────────────┤
│                                   ▼                                         │
│   ┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐ │
│   │  Logging    │    │   Error     │    │  Tracing    │    │   Audit     │ │
│   │  Service    │◄───│  Service    │◄───│  Service    │◄───│  Service    │ │
│   │ (Seq/ELK)   │    │(Dead Letter)│    │  (Jaeger)   │    │(Compliance) │ │
│   └─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘ │
│                                                                             │
│                          ▲ RabbitMQ / OpenTelemetry ▲                       │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                           CAPA DE INFRAESTRUCTURA                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   ┌─────────────────┐         ┌─────────────────┐         ┌─────────────┐  │
│   │ Configuration   │         │     Cache       │         │ Idempotency │  │
│   │    Service      │         │    Service      │         │   Service   │  │
│   │(Feature Flags)  │         │    (Redis)      │         │ (Duplicates)│  │
│   └─────────────────┘         └─────────────────┘         └─────────────┘  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Detalle por Servicio

### 1. Gateway (Ocelot)

**Puerto:** 18443  
**Tipo:** Routing  
**Todos lo usan:** ✅ SÍ

```
┌─────────────────────────────────────────────────────┐
│                    GATEWAY                           │
├─────────────────────────────────────────────────────┤
│ • Punto de entrada único para todas las APIs        │
│ • Routing a microservicios internos                 │
│ • Autenticación JWT (delegada a AuthService)        │
│ • Rate limiting (delegado a RateLimitingService)    │
│ • Load balancing                                    │
│ • Request/Response transformation                   │
│ • Agregación de APIs                                │
└─────────────────────────────────────────────────────┘
```

**Comunicación:**

- HTTP → Todos los servicios internos
- Request: `https://api.okla.com.do/api/{service}/{endpoint}`
- Response: Proxy al servicio interno

---

### 2. AuthService

**Puerto:** 15001  
**Tipo:** Seguridad  
**Todos lo usan:** ✅ SÍ

```
┌─────────────────────────────────────────────────────┐
│                  AUTH SERVICE                        │
├─────────────────────────────────────────────────────┤
│ • Generación de JWT tokens                          │
│ • Refresh tokens                                    │
│ • Validación de tokens                              │
│ • Gestión de sesiones                               │
│ • OAuth2 / Social login                             │
│ • MFA (Multi-Factor Authentication)                 │
└─────────────────────────────────────────────────────┘
```

**Endpoints principales:**

- `POST /api/auth/register` → Registro
- `POST /api/auth/login` → Login
- `POST /api/auth/refresh` → Refresh token
- `GET /api/auth/me` → Usuario actual
- `POST /api/auth/validate` → Validar token (interno)

---

### 3. LoggingService

**Puerto:** 15050  
**Tipo:** Observabilidad  
**Todos lo usan:** ✅ SÍ

```
┌─────────────────────────────────────────────────────┐
│                LOGGING SERVICE                       │
├─────────────────────────────────────────────────────┤
│ • Recepción de logs via RabbitMQ                    │
│ • Almacenamiento en Seq/Elasticsearch               │
│ • Búsqueda y filtrado de logs                       │
│ • Alertas basadas en patrones                       │
│ • Retención y rotación de logs                      │
│ • Dashboard de métricas                             │
└─────────────────────────────────────────────────────┘
```

**Integración:**

```csharp
// En cada microservicio (Program.cs)
builder.Host.UseSerilog((context, config) =>
{
    config.WriteTo.RabbitMQ(/* LoggingService queue */);
});
```

---

### 4. ErrorService

**Puerto:** 15009  
**Tipo:** Observabilidad  
**Todos lo usan:** ✅ SÍ

```
┌─────────────────────────────────────────────────────┐
│                 ERROR SERVICE                        │
├─────────────────────────────────────────────────────┤
│ • Dead Letter Queue (DLQ) processing                │
│ • Centralización de excepciones                     │
│ • Retry policies                                    │
│ • Notificación de errores críticos                  │
│ • Dashboard de errores                              │
│ • Análisis de patrones de error                     │
└─────────────────────────────────────────────────────┘
```

**Comunicación:** RabbitMQ (error.exchange → error.queue)

---

### 5. CacheService

**Puerto:** 15051  
**Tipo:** Performance  
**Todos lo usan:** ✅ Recomendado

```
┌─────────────────────────────────────────────────────┐
│                 CACHE SERVICE                        │
├─────────────────────────────────────────────────────┤
│ • Abstracción sobre Redis                           │
│ • Cache distribuido                                 │
│ • Invalidación de cache                             │
│ • Cache patterns (aside, through, etc.)             │
│ • TTL management                                    │
│ • Cache statistics                                  │
└─────────────────────────────────────────────────────┘
```

**Endpoints:**

- `GET /api/cache/{key}` → Obtener valor
- `POST /api/cache/{key}` → Guardar valor
- `DELETE /api/cache/{key}` → Invalidar
- `DELETE /api/cache/pattern/{pattern}` → Invalidar por patrón

---

### 6. ConfigurationService

**Puerto:** 15052  
**Tipo:** Config  
**Todos lo usan:** ✅ Recomendado

```
┌─────────────────────────────────────────────────────┐
│             CONFIGURATION SERVICE                    │
├─────────────────────────────────────────────────────┤
│ • Feature flags                                     │
│ • Configuración dinámica                            │
│ • A/B testing configuration                         │
│ • Environment-specific settings                     │
│ • Hot reload de configuración                       │
│ • Auditoría de cambios                              │
└─────────────────────────────────────────────────────┘
```

**Endpoints:**

- `GET /api/config/{key}` → Obtener configuración
- `GET /api/features/{feature}` → Feature flag status
- `PUT /api/features/{feature}` → Toggle feature

---

### 7. TracingService

**Puerto:** 15053  
**Tipo:** Observabilidad  
**Uso:** Recomendado

```
┌─────────────────────────────────────────────────────┐
│                TRACING SERVICE                       │
├─────────────────────────────────────────────────────┤
│ • Distributed tracing (Jaeger/Zipkin)               │
│ • Correlation ID management                         │
│ • Span collection y análisis                        │
│ • Latency analysis                                  │
│ • Service dependency mapping                        │
│ • Performance bottleneck detection                  │
└─────────────────────────────────────────────────────┘
```

**Integración:** OpenTelemetry SDK

---

### 8. AuditService

**Puerto:** 15054  
**Tipo:** Compliance  
**Uso:** Servicios críticos

```
┌─────────────────────────────────────────────────────┐
│                 AUDIT SERVICE                        │
├─────────────────────────────────────────────────────┤
│ • Registro de acciones de usuario                   │
│ • Compliance (GDPR, SOC2)                           │
│ • Inmutabilidad de registros                        │
│ • Búsqueda de auditoría                             │
│ • Reportes de actividad                             │
│ • Data retention policies                           │
└─────────────────────────────────────────────────────┘
```

**Eventos auditados:**

- Login/Logout
- Cambios en datos sensibles
- Pagos y transacciones
- Cambios de permisos
- Acceso a datos personales

---

### 9. RateLimitingService

**Puerto:** 15055  
**Tipo:** Protección  
**Uso:** Gateway + APIs públicas

```
┌─────────────────────────────────────────────────────┐
│             RATE LIMITING SERVICE                    │
├─────────────────────────────────────────────────────┤
│ • Token bucket / Sliding window                     │
│ • Per-user rate limits                              │
│ • Per-IP rate limits                                │
│ • Per-endpoint rate limits                          │
│ • Throttling policies                               │
│ • Rate limit headers (X-RateLimit-*)                │
└─────────────────────────────────────────────────────┘
```

**Endpoints:**

- `GET /api/ratelimit/check/{clientId}` → Verificar límite
- `POST /api/ratelimit/increment/{clientId}` → Incrementar contador

---

### 10. IdempotencyService

**Puerto:** 15056  
**Tipo:** Protección  
**Uso:** Pagos, creación de recursos

```
┌─────────────────────────────────────────────────────┐
│             IDEMPOTENCY SERVICE                      │
├─────────────────────────────────────────────────────┤
│ • Idempotency key management                        │
│ • Duplicate request detection                       │
│ • Response caching for retries                      │
│ • TTL for idempotency keys                          │
│ • Distributed locking                               │
└─────────────────────────────────────────────────────┘
```

**Uso:**

```
Header: Idempotency-Key: uuid-v4
```

---

## Patrones de Comunicación

### Síncrono (HTTP)

| Servicio             | Protocolo  | Patrón           |
| -------------------- | ---------- | ---------------- |
| Gateway              | HTTP/HTTPS | Request-Response |
| AuthService          | HTTP       | Request-Response |
| CacheService         | HTTP       | Request-Response |
| ConfigurationService | HTTP       | Request-Response |
| RateLimitingService  | HTTP       | Request-Response |
| IdempotencyService   | HTTP       | Request-Response |

### Asíncrono (RabbitMQ)

| Servicio       | Exchange       | Queue       | Patrón      |
| -------------- | -------------- | ----------- | ----------- |
| LoggingService | logs.exchange  | logs.queue  | Pub/Sub     |
| ErrorService   | error.exchange | error.queue | Dead Letter |
| AuditService   | audit.exchange | audit.queue | Pub/Sub     |

### Telemetría (OpenTelemetry)

| Servicio       | Protocolo | Formato |
| -------------- | --------- | ------- |
| TracingService | gRPC/HTTP | OTLP    |

---

## Cómo Integrar en Nuevos Servicios

### 1. Agregar paquetes NuGet compartidos

```xml
<ItemGroup>
  <!-- Logging -->
  <PackageReference Include="Serilog.Sinks.RabbitMQ" Version="6.0.0" />

  <!-- Tracing -->
  <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.7.0" />
  <PackageReference Include="OpenTelemetry.Exporter.Jaeger" Version="1.5.1" />

  <!-- Caching -->
  <PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.0" />
</ItemGroup>
```

### 2. Configurar en Program.cs

```csharp
// Logging → LoggingService
builder.Host.UseSerilog((context, config) =>
{
    config.WriteTo.Console()
          .WriteTo.RabbitMQ(/* config */);
});

// Tracing → TracingService
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddJaegerExporter();
    });

// Caching → CacheService
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "redis:6379";
});

// Rate Limiting → Gateway handles this
// Idempotency → Add middleware for payment endpoints
```

### 3. Middleware de Error Handling

```csharp
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        // Publicar a ErrorService via RabbitMQ
        await _rabbitMQ.PublishAsync("error.exchange", new ErrorEvent
        {
            Service = "MyService",
            Exception = exception,
            Timestamp = DateTime.UtcNow
        });
    });
});
```

---

## Referencias

- [Clean Architecture Documentation](../../ARCHITECTURE.md)
- [API Gateway Configuration](../../Gateway/README.md)
- [RabbitMQ Setup](../../compose.yaml)
- [OpenTelemetry Integration](../../observability/)

---

_Documento generado para el equipo de desarrollo de OKLA_
