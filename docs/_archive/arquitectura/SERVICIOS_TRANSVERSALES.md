# 🔧 Servicios Transversales (Cross-Cutting Services)

**Última Actualización:** Enero 19, 2026  
**Versión:** 1.0.0  
**Estado:** Análisis Completo + Plan de Refactoring

---

## 📋 Resumen Ejecutivo

Este documento analiza los **10 servicios transversales** críticos del ecosistema OKLA, evalúa su estado actual de integración, identifica brechas y propone un plan de refactoring para estandarizar su uso en todos los microservicios.

---

## 📊 Inventario de Servicios Transversales

| #   | Servicio                 | Tipo           | Prioridad  | Todos lo usan  | Estado Actual   |
| --- | ------------------------ | -------------- | ---------- | -------------- | --------------- |
| 1   | **Gateway**              | Routing        | 🔴 Crítico | ✅             | ✅ Funcional    |
| 2   | **AuthService**          | Seguridad      | 🔴 Crítico | ✅             | ✅ Funcional    |
| 3   | **LoggingService**       | Observabilidad | 🔴 Crítico | ✅             | ⚠️ Parcial      |
| 4   | **ErrorService**         | Observabilidad | 🔴 Crítico | ✅             | ⚠️ Parcial      |
| 5   | **CacheService**         | Performance    | 🟠 Alto    | ✅             | ⚠️ Parcial      |
| 6   | **ConfigurationService** | Config         | 🟠 Alto    | ✅             | ⚠️ Parcial      |
| 7   | **TracingService**       | Observabilidad | 🟡 Medio   | Recomendado    | ⚠️ Parcial      |
| 8   | **AuditService**         | Compliance     | 🟡 Medio   | Críticos       | ⚠️ Parcial      |
| 9   | **RateLimitingService**  | Protección     | 🟡 Medio   | Gateway + APIs | ❌ No Integrado |
| 10  | **IdempotencyService**   | Protección     | 🟡 Medio   | Pagos          | ❌ No Integrado |

---

## 🏗️ Arquitectura de Servicios Transversales

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              CAPA DE ENTRADA                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                          ┌─────────────────┐                                │
│                          │     Gateway     │ ← Entry Point                  │
│                          │    (Ocelot)     │                                │
│                          └────────┬────────┘                                │
│                                   │                                         │
│         ┌─────────────────────────┼─────────────────────────┐               │
│         │                         │                         │               │
│         ▼                         ▼                         ▼               │
│  ┌─────────────┐          ┌─────────────┐          ┌─────────────┐         │
│  │RateLimiting │          │   Auth      │          │Idempotency  │         │
│  │  Service    │          │  Service    │          │  Service    │         │
│  └─────────────┘          └─────────────┘          └─────────────┘         │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                          CAPA DE OBSERVABILIDAD                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   ┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐ │
│   │  Logging    │    │   Error     │    │  Tracing    │    │   Audit     │ │
│   │  Service    │    │  Service    │    │  Service    │    │  Service    │ │
│   │ (Seq/ELK)   │    │(Dead Letter)│    │  (Jaeger)   │    │(Compliance) │ │
│   └─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘ │
│         ▲                  ▲                  ▲                  ▲         │
│         │                  │                  │                  │         │
│         └──────────────────┴──────────────────┴──────────────────┘         │
│                              (RabbitMQ / OpenTelemetry)                     │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                           CAPA DE CONFIGURACIÓN                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   ┌─────────────────┐         ┌─────────────────┐                          │
│   │ Configuration   │         │     Cache       │                          │
│   │    Service      │         │    Service      │                          │
│   │(Feature Flags)  │         │    (Redis)      │                          │
│   └─────────────────┘         └─────────────────┘                          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔍 Análisis Detallado por Servicio

### 1️⃣ Gateway (Ocelot) - ✅ FUNCIONAL

**Ubicación:** `backend/Gateway/`

**Características Implementadas:**

- ✅ Routing con Ocelot
- ✅ JWT Authentication integrado
- ✅ CORS configurado (desarrollo y producción)
- ✅ OpenTelemetry para tracing
- ✅ Clean Architecture (Domain, Application, Infrastructure)
- ✅ Health checks

**Integraciones Transversales:**

- ✅ ServiceDiscovery (Consul)
- ✅ Serilog con TraceId/SpanId
- ✅ OpenTelemetry metrics/tracing
- ❌ RateLimiting (no integrado como middleware)
- ❌ Idempotency (no integrado)

**Código Relevante:**

```csharp
// Program.cs - OpenTelemetry configurado
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName, serviceVersion))
    .WithTracing(tracing => { ... })
    .WithMetrics(metrics => { ... });
```

**Recomendaciones:**

1. Integrar RateLimitingService como middleware de Ocelot
2. Centralizar configuración de rutas en ConfigurationService

---

### 2️⃣ AuthService - ✅ FUNCIONAL

**Ubicación:** `backend/AuthService/`

**Características Implementadas:**

- ✅ JWT Token generation
- ✅ Refresh tokens
- ✅ Password hashing (BCrypt)
- ✅ User authentication/authorization
- ✅ Role-based access control

**Integraciones Transversales:**

- ✅ Serilog logging
- ✅ OpenTelemetry
- ⚠️ No publica a ErrorService
- ⚠️ No usa CacheService para tokens

**Recomendaciones:**

1. Cachear tokens en CacheService (Redis)
2. Publicar eventos de login/logout a AuditService
3. Publicar errores de autenticación a ErrorService

---

### 3️⃣ LoggingService - ⚠️ PARCIALMENTE INTEGRADO

**Ubicación:** `backend/LoggingService/`

**Características Implementadas:**

- ✅ Serilog configurado
- ✅ Seq como destino de logs
- ✅ Service Discovery con Consul
- ✅ Alert Evaluation Background Service
- ✅ API para queries de logs

**Problema Actual:**
Los microservicios de negocio **no envían logs centralizados** a LoggingService. Cada servicio tiene su propio Serilog local.

**Estado de Integración en Otros Servicios:**

| Servicio            | Usa Serilog | Envía a LoggingService | Tiene TraceId |
| ------------------- | ----------- | ---------------------- | ------------- |
| VehiclesSaleService | ❌          | ❌                     | ❌            |
| UserService         | ✅          | ❌                     | ✅            |
| Gateway             | ✅          | ❌                     | ✅            |
| BillingService      | ✅          | ❌                     | ✅            |

**Recomendaciones:**

1. Crear librería compartida `CarDealer.Shared.Logging`
2. Configurar Serilog para enviar a Seq centralizado
3. Agregar RabbitMQ sink para logs críticos

---

### 4️⃣ ErrorService - ⚠️ PARCIALMENTE INTEGRADO

**Ubicación:** `backend/ErrorService/`

**Características Implementadas:**

- ✅ Dead Letter Queue
- ✅ OpenTelemetry con TraceId/SpanId
- ✅ JWT Authentication
- ✅ Rate Limiting interno
- ✅ FluentValidation
- ✅ Service Discovery (Consul)
- ✅ RabbitMQ messaging

**Problema Actual:**
Los microservicios **no publican errores** a ErrorService. Las excepciones se manejan localmente.

**Estado de Integración:**

| Servicio            | Publica Errores | Usa Dead Letter Queue |
| ------------------- | --------------- | --------------------- |
| VehiclesSaleService | ❌              | ❌                    |
| UserService         | ❌              | ❌                    |
| BillingService      | ❌              | ❌                    |
| Todos los demás     | ❌              | ❌                    |

**Recomendaciones:**

1. Crear `ErrorService.Client` NuGet package
2. Implementar Global Exception Handler que publique a RabbitMQ
3. Crear middleware `UseGlobalErrorHandling()`

---

### 5️⃣ CacheService - ⚠️ PARCIALMENTE INTEGRADO

**Ubicación:** `backend/CacheService/`

**Características Implementadas:**

- ✅ Redis como backend
- ✅ MediatR CQRS
- ✅ Distributed locks
- ✅ Statistics manager
- ✅ Health checks con Redis ping

**Problema Actual:**
Los microservicios **acceden directamente a Redis** en lugar de usar CacheService como API.

**Estado de Integración:**

| Servicio            | Usa Redis Directo | Usa CacheService API |
| ------------------- | ----------------- | -------------------- |
| RateLimitingService | ✅                | ❌                   |
| IdempotencyService  | ✅                | ❌                   |
| VehiclesSaleService | ❌                | ❌                   |

**Recomendaciones:**

1. Para **alta performance**: Mantener acceso directo a Redis
2. Crear `CacheService.Client` con interface `ICacheClient`
3. Usar CacheService API para cache compartido (ej: configuraciones)

---

### 6️⃣ ConfigurationService - ⚠️ PARCIALMENTE INTEGRADO

**Ubicación:** `backend/ConfigurationService/`

**Características Implementadas:**

- ✅ Feature Flags
- ✅ Secret Management (AES encryption)
- ✅ Configuración dinámica
- ✅ PostgreSQL como storage
- ✅ MediatR CQRS

**Problema Actual:**
Los microservicios **no consultan ConfigurationService** para feature flags. Usan `appsettings.json` local.

**Estado de Integración:**

| Servicio | Usa appsettings | Consulta ConfigurationService |
| -------- | --------------- | ----------------------------- |
| Todos    | ✅              | ❌                            |

**Recomendaciones:**

1. Crear `ConfigurationService.Client` con polling
2. Implementar `IOptions<T>` dinámico desde ConfigurationService
3. Prioridad: Feature Flags para toggles en caliente

---

### 7️⃣ TracingService - ⚠️ PARCIALMENTE INTEGRADO

**Ubicación:** `backend/TracingService/`

**Características Implementadas:**

- ✅ Query interface para Jaeger
- ✅ MediatR CQRS
- ✅ Health checks

**Problema Actual:**
Algunos servicios tienen OpenTelemetry, otros no.

**Estado de Integración:**

| Servicio            | OpenTelemetry | Jaeger Export |
| ------------------- | ------------- | ------------- |
| Gateway             | ✅            | ✅ (OTLP)     |
| ErrorService        | ✅            | ⚠️ (Console)  |
| AuditService        | ✅            | ⚠️ (Console)  |
| UserService         | ✅            | ⚠️ (Console)  |
| VehiclesSaleService | ❌            | ❌            |
| BillingService      | ⚠️            | ❌            |

**Recomendaciones:**

1. Crear extensión `AddStandardOpenTelemetry()` en CarDealer.Shared
2. Configurar OTLP exporter a Jaeger en todos los servicios
3. Estandarizar sampling rate (10% producción)

---

### 8️⃣ AuditService - ⚠️ PARCIALMENTE INTEGRADO

**Ubicación:** `backend/AuditService/`

**Características Implementadas:**

- ✅ Audit logs persistence
- ✅ Dead Letter Queue
- ✅ OpenTelemetry
- ✅ Service Discovery
- ✅ Background services para procesamiento

**Problema Actual:**
Los servicios **no publican eventos de auditoría**. Solo AuditService tiene la infraestructura lista.

**Servicios que DEBEN auditar:**

- AuthService (login, logout, password change)
- UserService (profile updates)
- BillingService (payments, refunds)
- VehiclesSaleService (listings create/update/delete)

**Recomendaciones:**

1. Crear eventos de auditoría en `CarDealer.Contracts`
2. Publicar via RabbitMQ desde servicios críticos
3. Implementar `[Auditable]` attribute para acciones automáticas

---

### 9️⃣ RateLimitingService - ❌ NO INTEGRADO

**Ubicación:** `backend/RateLimitingService/`

**Características Implementadas:**

- ✅ Multiple algorithms (Token Bucket, Sliding Window, Fixed Window, Leaky Bucket)
- ✅ Redis storage
- ✅ PostgreSQL para persistencia de violaciones
- ✅ Health checks
- ✅ API REST para gestión de reglas

**Problema Actual:**
RateLimitingService existe como servicio independiente, pero **no está integrado en Gateway ni en otros servicios**.

**Estado Actual:**

- Gateway usa Ocelot sin rate limiting
- No hay middleware de rate limiting
- No hay protección contra DDoS

**Recomendaciones:**

1. **CRÍTICO:** Integrar como middleware de Ocelot
2. Crear `RateLimitingMiddleware` para Gateway
3. Configurar reglas por endpoint en Gateway

---

### 🔟 IdempotencyService - ❌ NO INTEGRADO

**Ubicación:** `backend/IdempotencyService/`

**Características Implementadas:**

- ✅ Redis storage
- ✅ `[Idempotent]` attribute
- ✅ Middleware disponible
- ✅ Swagger header filter

**Problema Actual:**
IdempotencyService existe pero **no está integrado en servicios de pagos**.

**Servicios que DEBEN usar idempotency:**

- BillingService
- StripePaymentService
- AzulPaymentService
- InvoicingService

**Recomendaciones:**

1. **CRÍTICO para pagos:** Integrar en BillingService
2. Usar `[Idempotent]` en endpoints POST de pagos
3. Requerir header `Idempotency-Key` en payments API

---

## 🚨 BRECHAS CRÍTICAS IDENTIFICADAS

### 🔴 Prioridad Alta (Seguridad/Estabilidad)

| #   | Brecha                               | Riesgo                 | Servicios Afectados   |
| --- | ------------------------------------ | ---------------------- | --------------------- |
| 1   | RateLimiting no integrado en Gateway | DDoS, Abuso de API     | Todos                 |
| 2   | Idempotency no integrado en Pagos    | Cobros duplicados      | Billing, Stripe, Azul |
| 3   | Errores no centralizados             | Pérdida de visibilidad | Todos                 |
| 4   | Auditoría no implementada            | Compliance, Seguridad  | Auth, Billing, Users  |

### 🟠 Prioridad Media (Operaciones)

| #   | Brecha                  | Impacto              | Servicios Afectados |
| --- | ----------------------- | -------------------- | ------------------- |
| 5   | Logs no centralizados   | Debug difícil        | Todos               |
| 6   | Tracing inconsistente   | No hay correlación   | 50% de servicios    |
| 7   | Feature Flags no usados | Deploys riesgosos    | Todos               |
| 8   | Cache no estandarizado  | Performance variable | Varios              |

---

## 📋 PLAN DE REFACTORING

### Fase 1: Seguridad (Sprint Inmediato) - 2 semanas

#### 1.1 Integrar RateLimiting en Gateway

```
Archivos a modificar:
- Gateway/Gateway.Api/Program.cs
- Gateway/Gateway.Api/Middleware/RateLimitingMiddleware.cs (CREAR)
- compose.frontend-only.yaml (agregar dependencia)

Tareas:
1. Crear middleware que llame a RateLimitingService
2. Configurar reglas en ocelot.json
3. Agregar headers X-RateLimit-*
```

#### 1.2 Integrar Idempotency en Pagos

```
Archivos a modificar:
- BillingService/BillingService.Api/Program.cs
- StripePaymentService/StripePaymentService.Api/Program.cs
- AzulPaymentService/AzulPaymentService.Api/Program.cs

Tareas:
1. Agregar IdempotencyService.Core reference
2. Configurar middleware de idempotency
3. Agregar [Idempotent] a endpoints de pago
```

### Fase 2: Observabilidad (Sprint 2) - 2 semanas

#### 2.1 Centralizar Logs

```
Crear: _Shared/CarDealer.Shared.Logging/

Contenido:
- SerilogExtensions.cs
- LoggingServiceClient.cs
- ILogPublisher.cs

Tareas:
1. Crear librería compartida
2. Configurar Serilog → Seq centralizado
3. Agregar RabbitMQ sink para logs críticos
4. Actualizar todos los Program.cs
```

#### 2.2 Centralizar Errores

```
Crear: _Shared/CarDealer.Shared.ErrorHandling/

Contenido:
- GlobalExceptionMiddleware.cs
- ErrorServiceClient.cs
- IErrorPublisher.cs

Tareas:
1. Crear middleware global
2. Publicar excepciones a RabbitMQ → ErrorService
3. Configurar Dead Letter Queue
```

#### 2.3 Estandarizar Tracing

```
Crear: _Shared/CarDealer.Shared.Observability/

Contenido:
- OpenTelemetryExtensions.cs
- TracingConfiguration.cs

Tareas:
1. Crear builder.Services.AddStandardObservability()
2. Configurar OTLP → Jaeger
3. Aplicar en todos los servicios
```

### Fase 3: Operaciones (Sprint 3) - 2 semanas

#### 3.1 Implementar Feature Flags

```
Crear: _Shared/CarDealer.Shared.Configuration/

Contenido:
- ConfigurationServiceClient.cs
- FeatureFlagProvider.cs
- DynamicOptionsProvider.cs

Tareas:
1. Polling a ConfigurationService
2. Implementar IOptions<T> dinámico
3. Crear UI de feature flags (AdminService)
```

#### 3.2 Implementar Auditoría

```
Crear: _Shared/CarDealer.Shared.Audit/

Contenido:
- AuditAttribute.cs
- AuditMiddleware.cs
- AuditEventPublisher.cs

Eventos a auditar:
- AuthService: Login, Logout, PasswordChange, TokenRefresh
- UserService: ProfileUpdate, RoleChange
- BillingService: PaymentCreated, PaymentFailed, RefundIssued
- VehiclesSaleService: ListingCreated, ListingUpdated, ListingDeleted
```

---

## 📦 Librerías Compartidas a Crear

| Librería                         | Propósito                           | Prioridad |
| -------------------------------- | ----------------------------------- | --------- |
| `CarDealer.Shared.Logging`       | Serilog + Seq centralizado          | Alta      |
| `CarDealer.Shared.ErrorHandling` | Exception middleware + ErrorService | Alta      |
| `CarDealer.Shared.Observability` | OpenTelemetry estandarizado         | Media     |
| `CarDealer.Shared.RateLimiting`  | Cliente RateLimitingService         | Alta      |
| `CarDealer.Shared.Idempotency`   | Cliente IdempotencyService          | Alta      |
| `CarDealer.Shared.Audit`         | Publicador de eventos audit         | Media     |
| `CarDealer.Shared.Configuration` | Cliente ConfigurationService        | Media     |
| `CarDealer.Shared.Cache`         | Cliente CacheService                | Baja      |

---

## 🎯 Métricas de Éxito

### Después de Fase 1

- [ ] 100% de requests pasan por rate limiting
- [ ] 0 pagos duplicados (idempotency activo)
- [ ] Gateway protegido contra abuso

### Después de Fase 2

- [ ] 100% de logs en Seq centralizado
- [ ] 100% de errores en ErrorService
- [ ] 100% de servicios con TraceId correlacionado
- [ ] Dashboard de errores operativo

### Después de Fase 3

- [ ] Feature flags activos en 10+ toggles
- [ ] 100% de acciones críticas auditadas
- [ ] Compliance report generado automáticamente

---

## 📊 Resumen de Estado

```
┌─────────────────────────────────────────────────────────────────┐
│                    ESTADO DE INTEGRACIÓN                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ✅ Funcional (2/10)      ██████░░░░░░░░░░░░░░  20%            │
│  ⚠️ Parcial (6/10)        ████████████████████░░░░░░  60%      │
│  ❌ No Integrado (2/10)   ████░░░░░░░░░░░░░░░░  20%            │
│                                                                 │
│  Gateway ............... ✅                                    │
│  AuthService ........... ✅                                    │
│  LoggingService ........ ⚠️ (no centralizado)                  │
│  ErrorService .......... ⚠️ (no usado por otros)               │
│  CacheService .......... ⚠️ (acceso directo a Redis)           │
│  ConfigurationService .. ⚠️ (no consultado)                    │
│  TracingService ........ ⚠️ (50% servicios)                    │
│  AuditService .......... ⚠️ (sin eventos entrantes)            │
│  RateLimitingService ... ❌ (no integrado)                     │
│  IdempotencyService .... ❌ (no integrado)                     │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📅 Timeline Propuesto

| Fase       | Duración      | Entregables                              |
| ---------- | ------------- | ---------------------------------------- |
| **Fase 1** | 2 semanas     | Rate Limiting + Idempotency integrados   |
| **Fase 2** | 2 semanas     | Logging + Errors + Tracing centralizados |
| **Fase 3** | 2 semanas     | Feature Flags + Audit implementados      |
| **Total**  | **6 semanas** | Integración completa                     |

---

## 🔗 Referencias

- [Gateway Documentation](../backend/Gateway/README.md)
- [LoggingService README](../backend/LoggingService/README.md)
- [ErrorService README](../backend/ErrorService/README.md)
- [CacheService README](../backend/CacheService/SERVICE_COMPLETION_REPORT.md)
- [RateLimitingService README](../backend/RateLimitingService/README.md)
- [IdempotencyService README](../backend/IdempotencyService/README.md)

---

**Documento creado por:** GitHub Copilot  
**Fecha:** Enero 19, 2026  
**Próxima revisión:** Después de completar Fase 1
