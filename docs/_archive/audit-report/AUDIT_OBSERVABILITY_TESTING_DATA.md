# 🔍 Architecture Audit Report — Observability, Testing & Data Architecture

**Project:** OKLA (CarDealer Microservices)  
**Date:** February 13, 2026  
**Scope:** Observability, Testing, Data Architecture, API Design, Logging Standards  
**Standards:** ISO 25010, OpenTelemetry, Testing Pyramid, Database-per-Service, REST Maturity Model

---

## Executive Summary

| Area                  | Score      | Grade  | Severity Count      |
| --------------------- | ---------- | ------ | ------------------- |
| **Observability**     | 82/100     | B+     | 🔴 1 · 🟡 3 · 🔵 2  |
| **Testing**           | 52/100     | D      | 🔴 3 · 🟡 2 · 🔵 1  |
| **Data Architecture** | 71/100     | C+     | 🔴 2 · 🟡 3 · 🔵 1  |
| **API Design**        | 66/100     | C      | 🔴 2 · 🟡 4 · 🔵 2  |
| **Logging Standards** | 78/100     | B      | 🔴 1 · 🟡 2 · 🔵 2  |
| **Overall**           | **70/100** | **C+** | 🔴 9 · 🟡 14 · 🔵 8 |

**Legend:** 🔴 Critical · 🟡 Major · 🔵 Minor

---

## 1. OBSERVABILITY

### 1.1 Serilog Configuration ✅ Largely Compliant

A shared library (`CarDealer.Shared.Logging`) provides `UseStandardSerilog()` ensuring consistency.

| Service             | UseStandardSerilog | Seq | Enrichments                                   | Grade |
| ------------------- | ------------------ | --- | --------------------------------------------- | ----- |
| AuthService         | ✅                 | ✅  | TraceId, SpanId, ServiceName, Machine, Thread | A     |
| Gateway             | ✅                 | ✅  | Same                                          | A     |
| NotificationService | ✅                 | ✅  | Same                                          | A     |
| AdminService        | ✅                 | ✅  | Same                                          | A     |
| ContactService      | ✅                 | ✅  | Same                                          | A     |
| MediaService        | ✅                 | ✅  | Same                                          | A     |
| **ErrorService**    | ❌ Manual          | ✅  | TraceId, SpanId only                          | C     |

**Files:**

- Shared library: `backend/_Shared/CarDealer.Shared.Logging/Extensions/SerilogExtensions.cs`
- ErrorService manual config: `backend/ErrorService/ErrorService.Api/Program.cs` (lines 39-45)

> 🟡 **F-OBS-01 (Major):** ErrorService uses manual Serilog configuration instead of `UseStandardSerilog()`, missing enrichments: `ServiceName`, `Environment`, `MachineName`, `ThreadId`. This creates a log format inconsistency in the centralized Seq index.

### 1.2 OpenTelemetry ✅ Well Implemented

A shared library (`CarDealer.Shared.Observability`) provides `AddStandardObservability()`.

| Service             | Tracing   | Metrics   | OTLP Export | Sampling            | Health Filter |
| ------------------- | --------- | --------- | ----------- | ------------------- | ------------- |
| AuthService         | ✅        | ✅        | ✅ Jaeger   | 10% prod / 100% dev | ✅            |
| Gateway             | ✅        | ✅        | ✅ Jaeger   | ✅                  | ✅            |
| NotificationService | ✅        | ✅        | ✅          | ✅                  | ✅            |
| AdminService        | ✅        | ✅        | ✅          | ✅                  | ✅            |
| ContactService      | ✅        | ✅        | ✅          | ✅                  | ✅            |
| MediaService        | ✅        | ✅        | ✅          | ✅                  | ✅            |
| ErrorService        | ✅ Manual | ✅ Manual | ✅          | ✅                  | ✅            |

**Files:**

- Shared library: `backend/_Shared/CarDealer.Shared.Observability/Extensions/ObservabilityExtensions.cs`
- Custom metrics: `backend/AuthService/AuthService.Infrastructure/Metrics/AuthServiceMetrics.cs`

> 🔵 **F-OBS-02 (Minor):** ErrorService configures OpenTelemetry manually (lines 230-275 in Program.cs) instead of using `AddStandardObservability()`. Functionally equivalent but creates maintenance burden.

### 1.3 Health Checks ⚠️ Inconsistent

| Service      | `/health`         | `/health/ready` | `/health/live` | Custom Checks             |
| ------------ | ----------------- | --------------- | -------------- | ------------------------- |
| AuthService  | ✅                | ✅              | ✅             | Tag-based filtering       |
| ErrorService | ✅                | ❌              | ❌             | None                      |
| Gateway      | Custom Controller | ❌              | ❌             | Downstream service checks |
| Others       | ✅ (basic)        | ❌              | ❌             | None                      |

**Files:**

- AuthService health: `backend/AuthService/AuthService.Api/Program.cs` (lines 325-331)
- Gateway health: `backend/Gateway/Gateway.Application/UseCases/HealthCheckUseCases.cs`
- Shared health library: `backend/_Shared/CarDealer.Shared.HealthChecks/`

> 🟡 **F-OBS-03 (Major):** Only AuthService implements Kubernetes-standard readiness/liveness probes (`/health/ready`, `/health/live`). Other services only expose `/health`. In Kubernetes, this means the cluster cannot differentiate between a service that is starting up vs one that is unhealthy — leading to premature traffic routing or unnecessary pod kills.

> 🟡 **F-OBS-04 (Major):** No health checks include dependency checks (database, RabbitMQ, Redis). The basic `AddHealthChecks()` call without `AddNpgsql()`, `AddRabbitMQ()`, or `AddRedis()` only confirms the HTTP server is running, not that the service can actually process requests.

### 1.4 Prometheus Alerts ✅ Good Coverage

8 services have `prometheus-alerts.yml` files with well-structured alerts:

- `backend/AuthService/prometheus-alerts.yml`
- `backend/ErrorService/prometheus-alerts.yml`
- `backend/Gateway/prometheus-alerts.yml`
- `backend/NotificationService/prometheus-alerts.yml`
- `backend/MediaService/prometheus-alerts.yml`
- `backend/RoleService/prometheus-alerts.yml`
- `backend/AuditService/prometheus-alerts.yml`
- `backend/UserService/prometheus-alerts.yml`

Alert categories covered:

- ✅ High error rates
- ✅ Circuit breaker state
- ✅ Slow operations (p95 latency)
- ✅ Memory usage
- ✅ Suspicious login activity (AuthService)
- ✅ DLQ backlog

> 🔵 **F-OBS-05 (Minor):** No Prometheus scrape endpoint (`/metrics`) is explicitly mapped in any Program.cs. The `ObservabilityExtensions` configures OTLP export but the `PrometheusEnabled` flag's effect is unclear — verify that `app.MapPrometheusScrapingEndpoint()` is called, otherwise the alert rules have no data source.

### 1.5 Centralized Error Handling ✅ Excellent

ErrorService implements a comprehensive error pipeline:

- RabbitMQ consumer for async error ingestion
- Dead Letter Queue with retry (max 5 retries)
- MediatR CQRS pattern for error processing
- OpenTelemetry integration with TraceId/SpanId enrichment
- Rate limiting on error ingestion endpoints

**File:** `backend/ErrorService/ErrorService.Api/Program.cs` (379 lines)

> 🔴 **F-OBS-06 (Critical):** ErrorService `ClockSkew` is set to `TimeSpan.FromMinutes(5)` (line 128) while AuthService uses `TimeSpan.Zero`. This 5-minute window allows expired tokens to authenticate against ErrorService, creating a security inconsistency. All services must use `ClockSkew = TimeSpan.Zero`.

---

## 2. TESTING

### 2.1 Test Project Inventory

**Per-Service Test Projects (in-service): ~79 test .csproj files found**

| Service             | Test Project                 | Exists |
| ------------------- | ---------------------------- | ------ |
| AuthService         | `AuthService.Tests/`         | ✅     |
| ErrorService        | `ErrorService.Tests/`        | ✅     |
| Gateway             | `Gateway.Tests/`             | ✅     |
| ContactService      | `ContactService.Tests/`      | ✅     |
| AdminService        | `AdminService.Tests/`        | ✅     |
| NotificationService | `NotificationService.Tests/` | ✅     |
| MediaService        | `MediaService.Tests/`        | ✅     |
| RoleService         | `RoleService.Tests/`         | ✅     |

**Centralized Test Projects (`_Tests/`):**

| Project                         | Type          | Contents                               |
| ------------------------------- | ------------- | -------------------------------------- |
| `IntegrationTests/`             | Cross-service | E2E flows, contract tests, performance |
| `ChatbotService.Tests/`         | Unit          | Isolated                               |
| `DealerAnalyticsService.Tests/` | Unit          | Isolated                               |
| + 10 others in `_Tests/`        | Unit          | Isolated                               |

### 2.2 Test Quality Assessment

**Sample: AuthControllerTests** (`backend/AuthService/AuthService.Tests/Unit/Controllers/AuthControllerTests.cs`)

```
Quality Indicators:
✅ Uses xUnit + Moq (proper mocking framework)
✅ Follows AAA pattern (Arrange/Act/Assert)
✅ Tests API response wrapper (ApiResponse<T>)
❌ Only 1 test method in the file (Register_ValidRequest_ReturnsOk)
❌ No negative path tests (invalid email, weak password, duplicate registration)
❌ No boundary tests
❌ Constructor uses 2-param constructor while actual AuthController has 4 params
```

> 🔴 **F-TST-01 (Critical):** The AuthController test file uses a 2-parameter constructor `new AuthController(_mediatorMock.Object)` but the actual AuthController requires 4 parameters (IMediator, IConfiguration, ILogger, IUserRepository). This test either doesn't compile or tests against an obsolete API. **Stale tests give false confidence.**

### 2.3 Test Pyramid Compliance

**Documented target** (from `backend/_Tests/IntegrationTests/TEST_PLAN.md`):

- 60% Unit Tests · 30% Integration Tests · 10% E2E Tests
- Coverage target: >80%

**Actual state:**

| Category          | Found                                                            | Quality         |
| ----------------- | ---------------------------------------------------------------- | --------------- |
| Unit Tests        | Sparse per service (1-5 tests)                                   | ⚠️ Low coverage |
| Integration Tests | Docker-based factories found (NotificationService, ErrorService) | Moderate        |
| E2E Tests         | 1 file: `E2EFlowTests.cs` — only tests Gateway health endpoints  | ⚠️ Very thin    |
| Contract Tests    | `_Tests/IntegrationTests/Contract/` directory exists             | Unknown         |
| Performance Tests | `_Tests/IntegrationTests/Performance/` directory exists          | Unknown         |

> 🔴 **F-TST-02 (Critical):** Estimated actual unit test coverage is **<15%** against the 80% target. Critical paths like authentication flows (login, OAuth, 2FA), vehicle CRUD operations, payment processing, and KYC verification have minimal or no automated test coverage. This is the highest-risk finding in this audit.

> 🔴 **F-TST-03 (Critical):** E2E tests (`backend/_Tests/IntegrationTests/E2E/E2EFlowTests.cs`) only test health check endpoints and HTTP header handling — not actual business flows (register → login → create vehicle → checkout). The "E2E" label is misleading.

> 🟡 **F-TST-04 (Major):** No test infrastructure for database integration testing in most services. Only NotificationService and ErrorService have `WebApplicationFactory`/Docker-based test fixtures.

> 🟡 **F-TST-05 (Major):** No evidence of CI-enforced test execution with coverage thresholds. The `pr-checks.yml` workflow should gate PRs on test pass + minimum coverage.

> 🔵 **F-TST-06 (Minor):** The test plan document (`TEST_PLAN.md`) is from November 2025 and references a "refactorization" project. It should be updated to reflect current state.

---

## 3. DATA ARCHITECTURE

### 3.1 Database-per-Service Pattern ⚠️ Partial Compliance

**Connection string analysis from appsettings:**

| Service             | Database Name         | Host              | Compliant      |
| ------------------- | --------------------- | ----------------- | -------------- |
| AuthService         | `authservice`         | `authservice-db`  | ✅ Dedicated   |
| ErrorService        | `errorservice`        | `localhost:25432` | ✅ Dedicated   |
| NotificationService | `notificationservice` | `localhost:25433` | ✅ Dedicated   |
| ContactService      | `contactservice`      | `postgres:5432`   | ⚠️ Shared host |
| AdminService        | (empty string)        | —                 | ⚠️ Missing     |
| MediaService        | (via secrets)         | —                 | ✅ Abstracted  |

> 🔴 **F-DAT-01 (Critical):** ContactService connects to `Host=postgres;Port=5432;Database=contactservice` — using the shared `postgres` hostname. While it has its own database name, pointing to a shared PostgreSQL instance violates database-per-service isolation. If the shared postgres pod crashes, all services sharing that host go down simultaneously.

> 🟡 **F-DAT-02 (Major):** AdminService has `"DefaultConnection": ""` (empty string) in `appsettings.json`. This implies the service relies entirely on runtime secrets/environment variables with no `.Development.json` fallback visible.

### 3.2 DbContext & Entity Configuration ✅ Good

Each service has its own `ApplicationDbContext`:

| Service             | DbContext                 | Multi-tenant | Fluent Config                |
| ------------------- | ------------------------- | ------------ | ---------------------------- |
| ContactService      | ✅ `MultiTenantDbContext` | ✅           | ✅ Inline in OnModelCreating |
| AuthService         | ✅ Identity-based         | ❌           | ✅ Separate config files     |
| ErrorService        | ✅ Standard               | ❌           | ✅                           |
| NotificationService | ✅ `MultiTenantDbContext` | ✅           | ✅                           |
| MediaService        | ✅ `MultiTenantDbContext` | ✅           | ✅                           |

**File:** `backend/ContactService/ContactService.Infrastructure/Persistence/ApplicationDbContext.cs`

> 🟡 **F-DAT-03 (Major):** Inconsistent multi-tenancy adoption. ContactService, NotificationService, and MediaService use `MultiTenantDbContext`, while AuthService and ErrorService use standard `DbContext`. The architecture should document which services are explicitly tenant-agnostic.

### 3.3 EF Core Migrations ✅ Good

125 migration files found across services. Each service manages its own migrations independently.

| Service             | Migrations | Latest                                             |
| ------------------- | ---------- | -------------------------------------------------- |
| AuthService         | 3          | `20260212070858_AddUserIntent`                     |
| MediaService        | 1          | `20251206020102_AddMultiTenantSupport`             |
| RoleService         | 2          | `20260123030652_AddDisplayNameToRoleAndPermission` |
| NotificationService | 2          | `20251206020716_AddMultiTenantSupport`             |
| ContactService      | 1          | `20260210000000_AddMissingColumns`                 |

> 🟡 **F-DAT-04 (Major):** Auto-migration is enabled in production for most services (`Database:AutoMigrate` defaults to `true`). ErrorService explicitly documents the risk: _"disabled in production to avoid race conditions with HPA replicas"_. With Kubernetes HPA scaling multiple replicas, concurrent migration execution can cause deadlocks. **All services should disable auto-migrate in production and use CI/CD-driven migrations.**

### 3.4 Data Seeding ✅ Present

- AuthService: `AdminSeeder.SeedAsync()` creates default admin user/roles — gated via `Database:SeedDefaultAdmin`.
- Shared library exists: `_Shared/CarDealer.DataSeeding/`

> 🔵 **F-DAT-05 (Minor):** Only AuthService implements seeding. RoleService would benefit from seed data for default RBAC roles/permissions.

> 🔴 **F-DAT-06 (Critical):** Production connection string with plaintext password found in committed file: `backend/ErrorService/ErrorService.Api/appsettings.Production.json` → `"Password=production-password"`. Production configs with password fields **must not** be in source control. Use Kubernetes Secrets or a vault provider exclusively.

---

## 4. API DESIGN

### 4.1 REST Maturity Assessment

**Level 2 (HTTP Verbs + Status Codes)** — Partially compliant, no HATEOAS (Level 3).

| Controller                | HTTP Verbs          | Status Codes               | HATEOAS | Grade |
| ------------------------- | ------------------- | -------------------------- | ------- | ----- |
| AuthController            | ✅ GET, POST        | ✅ 200, 400, 401, 404, 500 | ❌      | B     |
| ErrorsController          | ✅ GET, POST        | ✅ 200, 404                | ❌      | B     |
| AdminController           | ✅ GET, POST, PUT   | ✅ 200, 204, 403, 404      | ❌      | A-    |
| ContactRequestsController | ✅ GET, POST, PATCH | ⚠️ Inconsistent            | ❌      | D     |

### 4.2 API Response Format ⚠️ Inconsistent

**Three different response patterns detected:**

**Pattern 1 — `ApiResponse<T>` wrapper** (AuthService, ErrorService):

```
{ "success": true, "data": {...}, "message": null }
```

**Pattern 2 — Anonymous objects** (ContactService):

```
{ "Id": "...", "VehicleId": "..." }
```

**Pattern 3 — Direct DTO** (AdminService):

```
{ "items": [...], "totalCount": 0, ... }
```

**Files:**

- Pattern 1: `backend/ErrorService/ErrorService.Api/Controllers/ErrorsController.cs` (line 37)
- Pattern 2: `backend/ContactService/ContactService.Api/Controllers/ContactRequestsController.cs` (lines 53-59)
- Pattern 3: `backend/AdminService/AdminService.Api/Controllers/AdminController.cs` (line 43)

> 🔴 **F-API-01 (Critical):** Three incompatible API response formats across services. Frontend developers must handle three different response shapes. All services should use the shared `ApiResponse<T>` wrapper.

> 🟡 **F-API-02 (Major):** ContactService error responses include raw exception details: `return StatusCode(500, new { error = "...", details = ex.Message })` (lines 85, 117). This is an information disclosure vulnerability (CWE-209).

### 4.3 API Versioning ⚠️ Built but Not Used

A comprehensive versioning library exists at `backend/_Shared/CarDealer.Shared.ApiVersioning/`:

- Supports: query string, header, URL segment, media type versioning
- Includes: `[ApiV1]`, `[ApiV2]`, `[ApiV3]`, `[ApiVersionDeprecated]` attributes
- Includes: Swagger multi-version support

**However:** Zero services call `AddStandardApiVersioning()` in their Program.cs. The library is dead code.

> 🟡 **F-API-03 (Major):** API versioning infrastructure is fully built but not wired into any service. All controllers use unversioned routes (`/api/[controller]`). When a breaking API change is needed, there is no mechanism to maintain backwards compatibility.

### 4.4 Pagination ⚠️ Partially Implemented

| Service        | Pagination                    | Pattern     |
| -------------- | ----------------------------- | ----------- |
| ErrorService   | ✅ `page` + `pageSize` params | Query-based |
| AdminService   | ✅ `PaginatedResult<T>`       | DTO-based   |
| ContactService | ❌ Returns full collections   | None        |
| AuthService    | ❌                            | N/A         |

> 🟡 **F-API-04 (Major):** `PaginatedResult<T>` is defined **twice** within AdminService (in `AdminUserDtos.cs` and `PlatformEmployeeDtos.cs`), and there's no shared pagination model in `_Shared`. ContactService returns unbounded collections that will cause performance issues at scale.

### 4.5 Additional API Findings

> 🔵 **F-API-05 (Minor):** ErrorsController and ContactRequestsController lack `[ProducesResponseType]` annotations. Swagger documentation doesn't show expected response types.

> 🔵 **F-API-06 (Minor):** ContactService `CreateContactRequest` returns `Ok` (200) instead of `CreatedAtAction` (201) for resource creation. Per REST semantics, `POST` should return `201 Created` with a `Location` header.

> 🔴 **F-API-07 (Critical):** ContactRequestsController directly injects repositories (`IContactRequestRepository`, `IContactMessageRepository`) into the controller, bypassing the Application layer entirely. This violates Clean Architecture — business logic (message creation, status update) is in the controller. This means: **no FluentValidation, no security validators (NoSqlInjection, NoXss), no audit logging, no idempotency checks.**

---

## 5. LOGGING STANDARDS

### 5.1 Structured Logging ✅ Good

The shared Serilog configuration uses structured message templates throughout:

```csharp
_logger.LogInformation("Approving vehicle {VehicleId} by {ApprovedBy}", request.VehicleId, request.ApprovedBy);
_logger.LogWarning("Moderation item not found: {Id}", request.Id);
```

All services use `ILogger<T>` DI injection with Serilog as the provider. Noisy namespaces are properly overridden:

- `Microsoft` → Warning
- `Microsoft.EntityFrameworkCore` → Warning
- `System` → Warning

### 5.2 Correlation ID Propagation ⚠️ Partial

| Service      | Reads X-Correlation-ID             | Propagates Downstream | Logs CorrelationId |
| ------------ | ---------------------------------- | --------------------- | ------------------ |
| AuthService  | ✅ via `HttpRequestContext`        | ❌ Not verified       | ✅                 |
| MediaService | ✅ via `DomainEvent.CorrelationId` | ✅ RabbitMQ header    | ✅                 |
| Others       | ❌                                 | ❌                    | ❌                 |

**Files:**

- `backend/AuthService/AuthService.Infrastructure/Services/HttpRequestContext.cs` (lines 74-79)
- `backend/MediaService/MediaService.Infrastructure/Messaging/RabbitMQMediaProducer.cs` (lines 151-153)
- `backend/MediaService/MediaService.Shared/ValidationPatterns.cs` (lines 51-53, 159-166)

> 🟡 **F-LOG-01 (Major):** No shared middleware exists to automatically extract/generate correlation IDs and propagate them. Only AuthService reads `X-Correlation-ID` from headers. Other services don't participate, making distributed trace stitching unreliable outside of OpenTelemetry's automatic `TraceId`.

### 5.3 PII in Logs ⚠️ Risk Detected

| Finding                           | Location                                                | Severity           |
| --------------------------------- | ------------------------------------------------------- | ------------------ |
| User ID in log (safe)             | AuthController: `LogWarning("User {UserId} not found")` | ✅ Safe            |
| JWT error message logged          | ErrorService Program.cs line 132                        | ⚠️ May leak tokens |
| Exception message in API response | ContactService line 85                                  | 🔴 Leaks internals |

> 🟡 **F-LOG-02 (Major):** JWT authentication failure logging includes `context.Exception.Message` which may contain token fragments. Found in `ErrorService.Api/Program.cs` (line 132) and `AdminService.Api/Program.cs` (line 77). Log only the exception type.

### 5.4 Console.WriteLine Usage

> 🔵 **F-LOG-03 (Minor):** `Console.WriteLine` used instead of structured logging in `ServiceRegistrationMiddleware.cs` for both Gateway and ContactService. These bypass Serilog and all observability infrastructure.

> 🔵 **F-LOG-04 (Minor):** `Log.Fatal` calls pass `ex.Message` instead of the full exception object, losing the stack trace. Use `Log.Fatal(ex, "...")` instead.

---

## 6. SHARED LIBRARIES INVENTORY

| Library                          | Purpose                    | Adoption                                   |
| -------------------------------- | -------------------------- | ------------------------------------------ |
| `CarDealer.Shared`               | Base utilities, middleware | ✅ All services                            |
| `CarDealer.Shared.Logging`       | Serilog setup              | ✅ Most (not ErrorService)                 |
| `CarDealer.Shared.Observability` | OpenTelemetry              | ✅ Most (not ErrorService)                 |
| `CarDealer.Shared.ErrorHandling` | Global error handling      | ✅ AuthService, others                     |
| `CarDealer.Shared.ApiVersioning` | API versioning             | ❌ **None**                                |
| `CarDealer.Shared.Audit`         | Audit middleware           | ✅ AuthService, ErrorService, AdminService |
| `CarDealer.Shared.HealthChecks`  | Health check extensions    | ⚠️ Unknown adoption                        |
| `CarDealer.Shared.Idempotency`   | Idempotency middleware     | ⚠️ Partial                                 |
| `CarDealer.Shared.RateLimiting`  | Rate limiting              | ✅ ErrorService                            |
| `CarDealer.Shared.Resilience`    | Circuit breaker, retry     | ⚠️ Unknown                                 |
| `CarDealer.Shared.Sagas`         | Saga orchestration         | ⚠️ Unknown                                 |
| `CarDealer.Shared.FeatureFlags`  | Feature toggles            | ⚠️ Unknown                                 |
| `CarDealer.Contracts`            | Shared DTOs/Events         | ✅ All services                            |
| `CarDealer.DataSeeding`          | Centralized seeding        | ⚠️ Unknown                                 |

---

## 7. REMEDIATION PRIORITIES

### 🔴 Critical (Fix Immediately — Sprint 0)

| ID       | Finding                                         | Effort | Impact                     |
| -------- | ----------------------------------------------- | ------ | -------------------------- |
| F-TST-02 | Unit test coverage <15% vs 80% target           | High   | Regressions undetectable   |
| F-DAT-06 | Plaintext production password in source control | Low    | Security breach risk       |
| F-OBS-06 | JWT ClockSkew inconsistency (5min vs 0)         | Low    | Expired token bypass       |
| F-API-01 | Three incompatible response formats             | Medium | Frontend integration chaos |
| F-API-07 | ContactService bypasses Clean Architecture      | Medium | No validation/security     |
| F-TST-01 | Stale/broken unit test (AuthController)         | Low    | False confidence           |
| F-TST-03 | E2E tests don't test business flows             | High   | No integration assurance   |
| F-DAT-01 | Shared database host for ContactService         | Medium | Single point of failure    |
| F-API-02 | Exception details leaked in API responses       | Low    | Information disclosure     |

### 🟡 Major (Fix in Next 2 Sprints)

| ID       | Finding                              | Effort | Impact                        |
| -------- | ------------------------------------ | ------ | ----------------------------- |
| F-OBS-01 | ErrorService manual Serilog config   | Low    | Log format inconsistency      |
| F-OBS-03 | Missing readiness/liveness probes    | Medium | K8s health management         |
| F-OBS-04 | No dependency health checks          | Medium | False healthy status          |
| F-LOG-01 | No shared correlation ID middleware  | Medium | Distributed tracing gaps      |
| F-LOG-02 | JWT exception may contain PII        | Low    | GDPR/privacy risk             |
| F-API-03 | API versioning built but unused      | Low    | Future breaking changes       |
| F-API-04 | No shared pagination model           | Medium | Performance at scale          |
| F-DAT-02 | AdminService empty connection string | Low    | Dev experience                |
| F-DAT-03 | Inconsistent multi-tenancy adoption  | Medium | Architectural confusion       |
| F-DAT-04 | Auto-migrate in production with HPA  | Low    | Deadlock risk                 |
| F-TST-04 | Most services lack test fixtures     | Medium | Can't write integration tests |
| F-TST-05 | No CI-enforced coverage gates        | Low    | Quality regression            |

### 🔵 Minor (Backlog)

| ID       | Finding                                            | Effort |
| -------- | -------------------------------------------------- | ------ |
| F-OBS-02 | ErrorService manual OpenTelemetry setup            | Low    |
| F-OBS-05 | Prometheus scrape endpoint unverified              | Low    |
| F-API-05 | Missing ProducesResponseType annotations           | Low    |
| F-API-06 | POST returns 200 instead of 201                    | Low    |
| F-DAT-05 | No seed data for RoleService                       | Low    |
| F-TST-06 | Test plan document outdated                        | Low    |
| F-LOG-03 | Console.WriteLine in ServiceRegistrationMiddleware | Low    |
| F-LOG-04 | Log.Fatal without exception object                 | Low    |

---

## 8. ISO 25010 COMPLIANCE MATRIX

| Characteristic             | Sub-characteristic   | Status | Key Gaps                                  |
| -------------------------- | -------------------- | ------ | ----------------------------------------- |
| **Functional Suitability** | Completeness         | ✅     | 86 services cover all business domains    |
|                            | Correctness          | ⚠️     | ContactService lacks validation layer     |
| **Performance**            | Time behaviour       | ⚠️     | No load test evidence; unbounded queries  |
|                            | Resource utilization | ✅     | OpenTelemetry metrics in place            |
| **Compatibility**          | Interoperability     | ⚠️     | 3 response formats break interop          |
| **Usability**              | Operability          | ⚠️     | Missing K8s probes; no runbooks           |
| **Reliability**            | Maturity             | ⚠️     | <15% test coverage                        |
|                            | Availability         | ✅     | Health checks present; DLQ for resilience |
|                            | Fault tolerance      | ✅     | Circuit breakers, DLQ, retry patterns     |
| **Security**               | Confidentiality      | ⚠️     | Plaintext password in source control      |
|                            | Integrity            | ✅     | JWT, RBAC, CSRF, XSS, SQL injection       |
| **Maintainability**        | Modularity           | ✅     | Clean Architecture per service            |
|                            | Reusability          | ✅     | 14 shared libraries                       |
|                            | Testability          | ❌     | Minimal test infrastructure               |
| **Portability**            | Adaptability         | ✅     | Containerized, K8s-native                 |

---

_Report generated: February 13, 2026_  
_Next audit scheduled: March 2026_
