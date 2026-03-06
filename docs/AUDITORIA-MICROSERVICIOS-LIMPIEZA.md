# 🧹 Reporte de Auditoría: Microservicios No Utilizados y Documentos Eliminables

> **Fecha:** 2026-03-05  
> **Workspace:** cardealer-microservices

---

## PARTE 1: MICROSERVICIOS QUE SE PUEDEN ELIMINAR

### Resumen

De los **52 servicios** bajo `backend/`, se identificaron **26 que pueden eliminarse** porque:

- No tienen rutas en el Gateway (Ocelot)
- Están deshabilitados en K8s (replicas: 0)
- Su funcionalidad ya está cubierta por servicios activos
- Son anti-patrones (infraestructura empaquetada como microservicio)

### 🟢 MANTENER (19 servicios activos + futuros)

| #   | Servicio            | Replicas K8s     | Motivo                                     |
| --- | ------------------- | ---------------- | ------------------------------------------ |
| 1   | AuthService         | 1                | Core — autenticación, JWT, 2FA, OAuth      |
| 2   | UserService         | 1                | Core — perfiles, dealers, sellers          |
| 3   | VehiclesSaleService | 1                | Core — CRUD vehículos, catálogos           |
| 4   | MediaService        | 1                | Core — imágenes, videos, S3                |
| 5   | Gateway             | 1                | Core — Ocelot API Gateway                  |
| 6   | AdminService        | 1                | Core — panel admin, publicidad consolidada |
| 7   | NotificationService | 1                | Core — emails, push, webhooks              |
| 8   | BillingService      | 1                | Core — pagos, suscripciones, Azul, Stripe  |
| 9   | ContactService      | 1                | Core — contacto, mensajes, citas           |
| 10  | ErrorService        | 1                | Core — errores centralizados, DLQ          |
| 11  | RoleService         | 1                | Core — RBAC, permisos                      |
| 12  | AuditService        | 1                | Core — auditoría, compliance               |
| 13  | ReviewService       | 1                | Core — reseñas                             |
| 14  | KYCService          | 1                | Core — verificación identidad              |
| 15  | ChatbotService      | 1                | Core — AI chatbot, WhatsApp                |
| 16  | AIProcessingService | 0 (bajo demanda) | Core — procesamiento AI                    |
| 17  | RecoAgent           | 0 (futuro)       | Futuro — recomendaciones AI                |
| 18  | SearchAgent         | 0 (futuro)       | Futuro — búsqueda AI                       |
| 19  | SupportAgent        | 0 (futuro)       | Futuro — soporte AI                        |

### 🟡 DIFERIR (7 servicios — evaluar en próxima fase)

| #   | Servicio                   | Motivo                                          |
| --- | -------------------------- | ----------------------------------------------- |
| 1   | CRMService                 | Tiene rutas en Ocelot pero deshabilitado        |
| 2   | DealerAnalyticsService     | 74 archivos de código, puede ser útil           |
| 3   | ReportsService             | Tiene rutas en Ocelot, útil para reportes admin |
| 4   | VehicleIntelligenceService | Pricing intelligence, futuro                    |
| 5   | SpyneIntegrationService    | Integración 3rd party, código sustancial        |
| 6   | ComparisonService          | Fusionar con VehiclesSaleService                |
| 7   | RecommendationService      | Fusionar con RecoAgent                          |

### 🔴 ELIMINAR (26 servicios)

| #   | Servicio                        | Duplica a                | Razón                                                 |
| --- | ------------------------------- | ------------------------ | ----------------------------------------------------- |
| 1   | **AdvertisingService**          | AdminService             | Ocelot ya redirige publicidad a AdminService          |
| 2   | **StaffService**                | AdminService/UserService | UserService tiene DealerEmployeesController           |
| 3   | **AppointmentService**          | ContactService           | ContactService tiene AppointmentsController           |
| 4   | **AlertService**                | NotificationService      | NotificationService tiene PriceAlerts y SavedSearches |
| 5   | **ComplianceService**           | \_DESCARTADOS            | Ya descartados en FASE4, no tiene rutas               |
| 6   | **DataProtectionService**       | UserService              | GDPR/ARCO puede ser módulo de UserService             |
| 7   | **DealerManagementService**     | UserService              | UserService ya tiene DealersController                |
| 8   | **EventTrackingService**        | AuditService             | Sin Dockerfile, analytics es parte de AuditService    |
| 9   | **IdempotencyService**          | (librería compartida)    | Debe ser un patrón en librería, no un servicio        |
| 10  | **IntegrationService**          | (genérico)               | Abstracción genérica sin rutas                        |
| 11  | **InventoryManagementService**  | VehiclesSaleService      | VehiclesSaleService maneja inventario                 |
| 12  | **LeadScoringService**          | VehiclesSaleService      | VehiclesSaleService tiene LeadsController             |
| 13  | **MaintenanceService**          | AdminService             | AdminService tiene MaintenanceController              |
| 14  | **MarketingService**            | NotificationService      | Campañas email son parte de NotificationService       |
| 15  | **MessageBusService**           | (infraestructura)        | RabbitMQ management es infra, no servicio             |
| 16  | **PaymentService**              | BillingService           | BillingService ya maneja Azul, Stripe, invoices       |
| 17  | **RateLimitingService**         | Gateway                  | Debe ser middleware del Gateway                       |
| 18  | **SchedulerService**            | (librería)               | Usar Hangfire/Quartz en servicios existentes          |
| 19  | **ServiceDiscovery**            | Consul                   | Consul ya hace esto                                   |
| 20  | **TaxComplianceService**        | (vacío)                  | Sin Dockerfile, sin K8s, shell vacío                  |
| 21  | **Vehicle360ProcessingService** | AIProcessingService      | AIProcessingService maneja 360                        |
| 22  | **Video360Service**             | MediaService             | MediaService tiene Video360Controller                 |
| 23  | **BackgroundRemovalService**    | AIProcessingService      | AIProcessingService maneja backgrounds                |
| 24  | **CacheService**                | Redis directo            | Redis se usa directo, no necesita servicio            |
| 25  | **ConfigurationService**        | AdminService             | AdminService tiene configuración                      |
| 26  | **ApiDocsService**              | (per-service)            | Swagger se genera por servicio                        |

---

## PARTE 2: DOCUMENTOS Y ARCHIVOS ELIMINABLES

### 🔴 ELIMINAR INMEDIATAMENTE (~70 archivos)

#### Archivos corruptos/backup (.bak)

```
backend/cardealer.sln.corrupted-20260109-072458.bak
backend/Gateway/Gateway.Api/ocelot.prod.json.bak
backend/AuthService/AuthService.Api/AuthService.Api.csproj.bak
backend/AuthService/AuthService.Infrastructure/EntityFrameworkDesign.json.bak
```

#### Archivos Class1.cs vacíos (scaffolding) — ~18 archivos

```
backend/ContactService/ContactService.Application/Class1.cs
backend/ContactService/ContactService.Domain/Class1.cs
backend/VehiclesSaleService/VehiclesSaleService.Application/Class1.cs
backend/ComparisonService/*/Class1.cs
backend/VehicleIntelligenceService/*/Class1.cs
backend/CRMService/*/Class1.cs
(y ~12 más en servicios a eliminar)
```

#### Reportes "FILES_CREATED" (logs de sesión Copilot)

```
backend/ContactService/FILES_CREATED_TESTS.md
backend/NotificationService/FILES_CREATED.md
backend/AdminService/FILES_CREATED*.md
backend/RoleService/FILES_CREATED*.md
backend/UserService/FILES_CREATED*.md
```

#### Resultados E2E (point-in-time)

```
backend/ErrorService/E2E_TESTING_RESULTS.md
backend/RoleService/E2E_TESTING_RESULTS.md
backend/UserService/E2E_TESTING_RESULTS.md
```

#### Scripts E2E PowerShell (one-time use, rutas Windows hardcoded)

```
backend/AuthService/AuthService.Tests/E2E/Scripts/E2E-TESTING-SCRIPT.ps1
backend/ErrorService/E2E-TESTING-SCRIPT.ps1
backend/RoleService/E2E-TESTING-SCRIPT.ps1
backend/UserService/E2E-TESTING-SCRIPT.ps1
```

#### Reportes "Implementation Complete" / "Final Summary"

```
backend/AuthService/IMPLEMENTATION_COMPLETE.md
backend/AuthService/IMPLEMENTATION_PLAN.md
backend/AuthService/ADVANCED_FEATURES_IMPLEMENTATION.md
backend/ErrorService/FINAL_SUMMARY.md
backend/ErrorService/DEAD_LETTER_QUEUE_IMPLEMENTATION.md
backend/NotificationService/TEMPLATES_SCHEDULING_IMPLEMENTATION.md
```

#### Gap Analysis (ya completados)

```
backend/AuthService/ANALYSIS_GAP_BEFORE_E2E.md
backend/ErrorService/ANALYSIS_GAP_BEFORE_E2E.md
backend/RoleService/ANALYSIS_GAP_BEFORE_E2E.md
backend/UserService/ANALYSIS_GAP_BEFORE_E2E.md
```

#### Docker compose duplicados por servicio

```
backend/ErrorService/docker-compose-observability.yml
backend/AuthService/docker-compose-observability.yml
backend/NotificationService/docker-compose-observability.yml
backend/ErrorService/grafana-datasources.yml
backend/AuthService/grafana-datasources.yml
backend/NotificationService/grafana-datasources.yml
```

### 🟡 ARCHIVAR PRIMERO (~30 archivos)

Mover a `docs/_archive/` antes de eliminar:

```
backend/AuthService/ARCHITECTURE.md          → docs/_archive/auth-architecture.md
backend/AuthService/CHANGELOG.md             → docs/_archive/auth-changelog.md
backend/AuthService/TROUBLESHOOTING.md       → docs/_archive/auth-troubleshooting.md
backend/NotificationService/README.md        → docs/_archive/notification-readme.md
(y ~25 más de documentación per-service)
```

### 🟠 REVISAR (decisión humana requerida)

#### Directorio `_DESCARTADOS/` completo (~2,200+ archivos)

```
backend/_DESCARTADOS/                        → ¿Eliminar todo? Git history lo preserva
```

> **Recomendación:** Eliminar todo el directorio. Ya tiene README explicando por qué se descartó. El historial de Git preserva el código si alguna vez se necesita.

#### Directorio `_REMOVED_CONTROLLERS/` (en 6 servicios)

```
backend/AdminService/_REMOVED_CONTROLLERS/
backend/AuthService/_REMOVED_CONTROLLERS/
backend/BillingService/_REMOVED_CONTROLLERS/
backend/UserService/_REMOVED_CONTROLLERS/
backend/VehiclesSaleService/_REMOVED_CONTROLLERS/
backend/NotificationService/_REMOVED_CONTROLLERS/
```

> **Recomendación:** Eliminar. El código está en Git history.

#### Archivos `context.md` (~42 archivos)

> **Recomendación:** Mantener solo los de servicios activos si se usan para contexto de Copilot.

#### Archivos `prometheus-alerts.yml` por servicio (~8 archivos)

> **Recomendación:** Centralizar en `k8s/monitoring/` y eliminar las copias per-service.

---

## RESUMEN DE LIMPIEZA

| Categoría                  | Archivos                         | Acción                           |
| -------------------------- | -------------------------------- | -------------------------------- |
| 🔴 Eliminar inmediatamente | ~70                              | Sin impacto en build/deploy      |
| 🟡 Archivar primero        | ~30                              | Mover a docs/\_archive/          |
| 🟠 Revisar (\_DESCARTADOS) | ~2,200+                          | Eliminar si OK con equipo        |
| 🔴 Servicios completos     | 26 directorios                   | Mover a \_DESCARTADOS o eliminar |
| **TOTAL estimado**         | **~2,300+** archivos eliminables |

### Espacio estimado recuperado

- 26 servicios × ~50 archivos promedio = ~1,300 archivos de código
- \_DESCARTADOS = ~2,200 archivos
- Documentos sueltos = ~100 archivos
- **Total: ~3,600+ archivos eliminables**
