# 📋 Índice de Microservicios Backend - OKLA

> **Última actualización:** Enero 2026  
> **Total servicios:** 70+ microservicios  
> **En producción DOKS:** 13 servicios

---

## 🏭 SERVICIOS EN PRODUCCIÓN (DOKS)

Estos servicios están desplegados y funcionando en producción:

| Servicio            | Puerto | Docs API | Estado     |
| ------------------- | ------ | -------- | ---------- |
| AuthService         | 8080   | ✅       | 🟢 Running |
| UserService         | 8080   | ✅       | 🟢 Running |
| RoleService         | 8080   | ✅       | 🟢 Running |
| VehiclesSaleService | 8080   | ✅       | 🟢 Running |
| MediaService        | 8080   | ✅       | 🟢 Running |
| NotificationService | 8080   | ✅       | 🟢 Running |
| BillingService      | 8080   | ✅       | 🟢 Running |
| ErrorService        | 8080   | ✅       | 🟢 Running |
| Gateway (Ocelot)    | 8080   | ✅       | 🟢 Running |
| ContactService      | 8080   | ⚠️       | 🟢 Running |

---

## 📂 CATÁLOGO COMPLETO DE SERVICIOS

### 🔐 Autenticación y Usuarios

| Servicio    | Descripción               | Documentación                                  |
| ----------- | ------------------------- | ---------------------------------------------- |
| AuthService | JWT, login, 2FA, OAuth    | [Ver API](../../backend/AuthService/README.md) |
| UserService | Perfiles, preferencias    | [Ver API](../../backend/UserService/README.md) |
| RoleService | RBAC, roles, permisos     | [Ver API](../../backend/RoleService/README.md) |
| KYCService  | Verificación de identidad | [Ver Doc](./05-kycservice.md) ✅               |

### 🚗 Vehículos

| Servicio                    | Descripción                        | Documentación                                          |
| --------------------------- | ---------------------------------- | ------------------------------------------------------ |
| VehiclesSaleService         | CRUD vehículos, catálogo, búsqueda | [Ver API](../../backend/VehiclesSaleService/README.md) |
| Vehicle360ProcessingService | Procesamiento de fotos 360°        | Pendiente                                              |
| VehicleIntelligenceService  | Pricing IA, valoraciones           | Pendiente                                              |
| VehicleRegistryService      | Registro de vehículos              | Pendiente                                              |

### 🏪 Dealers

| Servicio                   | Descripción              | Documentación                                 |
| -------------------------- | ------------------------ | --------------------------------------------- |
| DealerManagementService    | Perfiles, verificación   | [Ver Doc](./06-dealermanagementservice.md) ✅ |
| DealerAnalyticsService     | Métricas de dealers      | Pendiente                                     |
| InventoryManagementService | Import/export inventario | Pendiente                                     |

### 💳 Pagos y Facturación

| Servicio             | Descripción              | Documentación                                     |
| -------------------- | ------------------------ | ------------------------------------------------- |
| BillingService       | Subscripciones, cobros   | [Ver API](../../backend/BillingService/README.md) |
| PaymentService       | Pasarelas multi-provider | [Ver Doc](./08-paymentservice.md) ✅              |
| StripePaymentService | Integración Stripe       | [Ver Doc](./08-paymentservice.md) ✅              |
| InvoicingService     | Facturación electrónica  | Pendiente                                         |

### 📧 Comunicaciones

| Servicio            | Descripción              | Documentación                                          |
| ------------------- | ------------------------ | ------------------------------------------------------ |
| NotificationService | Email, SMS, Push, In-App | [Ver API](../../backend/NotificationService/README.md) |
| ContactService      | Formularios de contacto  | [Ver API](../../backend/ContactService/README.md)      |
| ChatbotService      | Chatbot IA para leads    | Pendiente                                              |
| MarketingService    | Campañas, newsletters    | Pendiente                                              |

### 📊 Analytics y Data

| Servicio             | Descripción                | Documentación                              |
| -------------------- | -------------------------- | ------------------------------------------ |
| EventTrackingService | Tracking de eventos        | [Ver Doc](./07-eventtrackingservice.md) ✅ |
| DataPipelineService  | ETL y transformaciones     | Pendiente                                  |
| UserBehaviorService  | Comportamiento de usuarios | Pendiente                                  |
| FeatureStoreService  | Features para ML           | Pendiente                                  |
| ReportingService     | Reportes y dashboards      | Pendiente                                  |

### 🤖 IA y ML

| Servicio              | Descripción                    | Documentación                               |
| --------------------- | ------------------------------ | ------------------------------------------- |
| RecommendationService | Recomendaciones personalizadas | [Ver Doc](./09-recommendationservice.md) ✅ |
| LeadScoringService    | Calificación de leads          | Pendiente                                   |
| AIProcessingService   | Procesamiento con IA           | Pendiente                                   |

### ⚖️ Compliance y Legal

| Servicio                     | Descripción              | Documentación                           |
| ---------------------------- | ------------------------ | --------------------------------------- |
| ComplianceService            | Compliance general       | [Ver Doc](./10-complianceservice.md) ✅ |
| ComplianceReportingService   | Reportes de compliance   | Pendiente                               |
| ComplianceIntegrationService | Integraciones compliance | Pendiente                               |
| TaxComplianceService         | Compliance fiscal DGII   | Pendiente                               |
| ConsumerProtectionService    | Protección al consumidor | Pendiente                               |
| DataProtectionService        | Protección de datos      | Pendiente                               |
| ECommerceComplianceService   | Compliance e-commerce    | Pendiente                               |
| RegulatoryAlertService       | Alertas regulatorias     | Pendiente                               |
| AntiMoneyLaunderingService   | Anti lavado de dinero    | Pendiente                               |

### ⭐ Reviews y Reputación

| Servicio      | Descripción              | Documentación |
| ------------- | ------------------------ | ------------- |
| ReviewService | Reviews y calificaciones | Pendiente     |

### 🔧 Infraestructura

| Servicio             | Descripción                | Documentación                                   |
| -------------------- | -------------------------- | ----------------------------------------------- |
| Gateway              | API Gateway (Ocelot)       | [Ver Config](../../backend/Gateway/README.md)   |
| ErrorService         | Centralización de errores  | [Ver API](../../backend/ErrorService/README.md) |
| MediaService         | Upload S3, procesamiento   | [Ver API](../../backend/MediaService/README.md) |
| CacheService         | Redis cache                | [Ver Doc](./02-cacheservice.md) ✅              |
| SchedulerService     | Jobs programados           | [Ver Doc](./03-schedulerservice.md) ✅          |
| AuditService         | Logs de auditoría          | [Ver Doc](./04-auditservice.md) ✅              |
| LoggingService       | Centralización de logs     | Pendiente                                       |
| TracingService       | Distributed tracing        | Pendiente                                       |
| HealthCheckService   | Health checks              | Pendiente                                       |
| ConfigurationService | Configuración centralizada | Pendiente                                       |

### 📅 Otros Servicios

| Servicio           | Descripción                | Documentación                       |
| ------------------ | -------------------------- | ----------------------------------- |
| AlertService       | Alertas de precio/búsqueda | [Ver Doc](./12-alertservice.md) ✅  |
| ComparisonService  | Comparador de vehículos    | Pendiente                           |
| AppointmentService | Citas y test drives        | Pendiente                           |
| MaintenanceService | Modo mantenimiento         | Pendiente                           |
| SearchService      | Búsqueda Elasticsearch     | [Ver Doc](./11-searchservice.md) ✅ |
| SupportService     | Tickets de soporte         | [Ver](./01-supportservice.md)       |

---

## 📊 Estadísticas de Documentación

| Categoría         | Servicios | Documentados | Pendientes |
| ----------------- | --------- | ------------ | ---------- |
| Core (Producción) | 13        | 10 (77%)     | 3          |
| Dealers           | 3         | 1 (33%)      | 2          |
| Pagos             | 4         | 2 (50%)      | 2          |
| Analytics         | 5         | 1 (20%)      | 4          |
| IA/ML             | 3         | 1 (33%)      | 2          |
| Compliance        | 9         | 1 (11%)      | 8          |
| Infraestructura   | 10        | 7 (70%)      | 3          |
| Auth/KYC          | 4         | 4 (100%)     | 0          |
| Búsqueda/Alertas  | 2         | 2 (100%)     | 0          |
| Otros             | 4         | 1 (25%)      | 3          |
| **TOTAL**         | **57**    | **30 (53%)** | **27**     |

---

## 🎯 Plan de Documentación

### Sprint 1: Servicios Core (Semana 1)

1. ✅ AuthService - Documentado
2. ✅ UserService - Documentado
3. ✅ RoleService - Documentado
4. ✅ VehiclesSaleService - Documentado
5. ⏳ KYCService

### Sprint 2: Dealers y Pagos (Semana 2)

1. DealerManagementService
2. DealerAnalyticsService
3. PaymentService
4. StripePaymentService
5. InvoicingService

### Sprint 3: Analytics y ML (Semana 3)

1. EventTrackingService
2. RecommendationService
3. LeadScoringService
4. ReportingService

### Sprint 4: Compliance (Semana 4)

1. ComplianceService
2. TaxComplianceService
3. DataProtectionService

---

## 🔗 Enlaces Rápidos

- [Auditoría de Endpoints Gateway](../AUDITORIA-GATEWAY-ENDPOINTS.md)
- [Resumen Visual de APIs](../AUDITORIA-RESUMEN-VISUAL.md)
- [API Integration Docs](../05-API-INTEGRATION/)

---

_Este documento se actualizará conforme se documenten más servicios._
