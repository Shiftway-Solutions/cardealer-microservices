# 🔍 Auditoría de Impacto: Eliminación de 26 Microservicios

> **Fecha:** 2026-03-05  
> **Referencia:** `docs/AUDITORIA-MICROSERVICIOS-LIMPIEZA.md`  
> **Conclusión:** ⚠️ **NO se pueden eliminar los 26 de golpe.** 4 servicios tienen dependencias duras en servicios activos.

---

## RESUMEN EJECUTIVO

| Categoría                     | Cantidad | Acción                                    |
| ----------------------------- | -------- | ----------------------------------------- |
| 🟢 Seguros para eliminar      | **18**   | Mover a `backend/_backup/` inmediatamente |
| 🔴 Requieren migración        | **4**    | Migrar funcionalidad antes de eliminar    |
| 🟡 Dependen de los anteriores | **4**    | Eliminar después de Phase 2-5             |

---

## 🔴 SERVICIOS QUE AFECTARÍAN LA PLATAFORMA (4 servicios)

### 1. ServiceDiscovery — IMPACTO CRÍTICO

**10 servicios activos** tienen `<ProjectReference>` y `using` directo:

| Servicio Activo     | Archivos Afectados                                                                                        |
| ------------------- | --------------------------------------------------------------------------------------------------------- |
| AuthService         | Program.cs, ServiceRegistrationMiddleware.cs, NotificationServiceClient.cs, ExternalServiceHealthCheck.cs |
| UserService         | Program.cs, ServiceRegistrationMiddleware.cs + 5 External clients                                         |
| Gateway             | Program.cs, ServiceRegistrationMiddleware.cs + Tests                                                      |
| AdminService        | Program.cs, ServiceRegistrationMiddleware.cs + 3 External clients                                         |
| MediaService        | Program.cs, ServiceRegistrationMiddleware.cs                                                              |
| NotificationService | Program.cs, ServiceRegistrationMiddleware.cs                                                              |
| ErrorService        | Program.cs, ServiceRegistrationMiddleware.cs                                                              |
| AuditService        | Program.cs, ServiceRegistrationMiddleware.cs                                                              |
| RoleService         | Program.cs, ServiceRegistrationMiddleware.cs + 3 External clients                                         |
| ContactService      | ServiceRegistrationMiddleware.cs                                                                          |

**Interfaces usadas:** `IServiceRegistry`, `IServiceDiscovery`, `ConsulServiceRegistry`, `ConsulServiceDiscovery`, `ServiceInstance`

**Migración requerida:** Extraer estas interfaces/clases a `CarDealer.Shared` → actualizar los 10 servicios → luego eliminar ServiceDiscovery.

---

### 2. ConfigurationService — IMPACTO ALTO

**5 servicios activos** + librería compartida hacen llamadas HTTP:

| Servicio Activo     | Tipo de Dependencia                                                              |
| ------------------- | -------------------------------------------------------------------------------- |
| AuthService         | `SecurityConfigProvider` lee JWT/lockout settings desde ConfigurationService API |
| VehiclesSaleService | `VehiclesController` usa `IConfigurationServiceClient`                           |
| MediaService        | `MediaController` usa `IConfigurationServiceClient`                              |
| NotificationService | 8+ handlers/providers usan `IConfigurationServiceClient` para feature flags      |
| KYCService          | `KYCConfigurationService` lee configuración dinámica                             |

**Librería compartida:** `CarDealer.Shared/Configuration/` contiene `IConfigurationServiceClient` que hace HTTP calls a ConfigurationService.

**Migración requerida:** Migrar la API de configuración a AdminService (ya tiene stub `ConfigurationsController`) → redirigir URL base en shared lib.

---

### 3. DealerManagementService — IMPACTO MEDIO

| Servicio Activo | Tipo de Dependencia                                                                 |
| --------------- | ----------------------------------------------------------------------------------- |
| AdminService    | `DealerService.cs` hace HTTP calls para CRUD de dealers, verificación, estadísticas |

**Migración requerida:** Mover lógica de dealers a AdminService o UserService.

---

### 4. RateLimitingService — IMPACTO BAJO

**Librería compartida** `CarDealer.Shared.RateLimiting` tiene modo `"http"` que llama a `ratelimitingservice:8080`.

**Migración requerida:** Verificar que todos los servicios usen modo `"redis"` (directo) → eliminar modo `"http"` de shared lib.

---

## 🟡 SERVICIOS QUE DEPENDEN DE LOS ANTERIORES (4 servicios)

Estos se pueden eliminar **después** de migrar las dependencias de los 4 anteriores:

| #   | Servicio                    | Depende de                                                |
| --- | --------------------------- | --------------------------------------------------------- |
| 21  | Vehicle360ProcessingService | BackgroundRemovalService (ambos se eliminan)              |
| 23  | BackgroundRemovalService    | Vehicle360ProcessingService (ambos se eliminan)           |
| 24  | CacheService                | ServiceDiscovery + ConfigurationService                   |
| —   | (limpieza Ocelot)           | Rutas de PaymentService, EventTracking, LeadScoring, etc. |

---

## 🟢 SEGUROS PARA ELIMINAR INMEDIATAMENTE (18 servicios)

Cero dependencias desde servicios activos. Se pueden mover a `backend/_backup/`:

| #   | Servicio                       | Verificación                                      |
| --- | ------------------------------ | ------------------------------------------------- |
| 1   | **AdvertisingService**         | ✅ Cero referencias en código activo              |
| 2   | **StaffService**               | ✅ Solo en compose.yaml                           |
| 3   | **AppointmentService**         | ✅ Solo en test coverage XML                      |
| 4   | **AlertService**               | ✅ Solo su propia definición compose              |
| 5   | **ComplianceService**          | ✅ Solo auto-referencias                          |
| 6   | **DataProtectionService**      | ✅ Solo auto-referencias                          |
| 8   | **EventTrackingService**       | ✅ Solo auto-referencias + rutas Ocelot (limpiar) |
| 9   | **IdempotencyService**         | ✅ Solo en solution file                          |
| 10  | **IntegrationService**         | ✅ Solo en solution file                          |
| 11  | **InventoryManagementService** | ✅ Cero referencias                               |
| 12  | **LeadScoringService**         | ✅ Solo rutas Ocelot (limpiar)                    |
| 13  | **MaintenanceService**         | ✅ Solo auto-referencias + compose                |
| 14  | **MarketingService**           | ✅ Solo auto-referencias                          |
| 15  | **MessageBusService**          | ✅ Solo auto-referencias                          |
| 16  | **PaymentService**             | ✅ Solo rutas Ocelot (limpiar)                    |
| 18  | **SchedulerService**           | ✅ Solo en solution file + compose                |
| 20  | **TaxComplianceService**       | ✅ Solo auto-referencias                          |
| 22  | **Video360Service**            | ✅ Cero referencias                               |

**Nota:** ApiDocsService se incluye en los 18 seguros (total = 18 + ApiDocsService = 19 seguros realmente).

---

## 📋 PLAN DE ELIMINACIÓN EN FASES

### FASE 1 — Inmediata (sin riesgo)

- ✅ Mover 18 servicios seguros a `backend/_backup/`
- ✅ Limpiar rutas Ocelot de servicios eliminados
- ✅ Limpiar definiciones en compose.yaml

### FASE 2 — ServiceDiscovery (1-2 días)

1. Copiar `IServiceRegistry`, `IServiceDiscovery`, `ConsulServiceRegistry`, `ConsulServiceDiscovery`, `ServiceInstance` a `CarDealer.Shared`
2. Actualizar `<ProjectReference>` en los 10 servicios activos
3. Actualizar `using` statements
4. Verificar que compila
5. Mover ServiceDiscovery a `_backup/`

### FASE 3 — ConfigurationService (2-3 días)

1. Completar `AdminService.Api/Controllers/ConfigurationsController.cs`
2. Migrar datos de configuración
3. Actualizar URL base en `CarDealer.Shared/Configuration/ConfigurationServiceClient.cs`
4. Mover ConfigurationService a `_backup/`

### FASE 4 — DealerManagementService (1-2 días)

1. Migrar lógica de `DealerService.cs` a AdminService o UserService
2. Actualizar URLs/config en AdminService
3. Mover DealerManagementService a `_backup/`

### FASE 5 — RateLimitingService (0.5 días)

1. Verificar que todos usen modo `"redis"` en config
2. Eliminar modo `"http"` de shared lib
3. Mover RateLimitingService a `_backup/`

### FASE 6 — Limpieza final

1. Mover Vehicle360ProcessingService, BackgroundRemovalService, CacheService a `_backup/`
2. Limpiar todas las rutas Ocelot residuales
3. Limpiar compose.yaml
4. Actualizar cardealer.sln

---

## ⚠️ ARCHIVOS QUE NECESITAN LIMPIEZA POST-ELIMINACIÓN

### Ocelot Gateway (rutas a eliminar)

- `ocelot.Development.json` — rutas de PaymentService, EventTracking, LeadScoring, BackgroundRemoval, Vehicle360, CacheService, StaffService
- `ocelot.prod.json` — mismas rutas en producción

### compose.yaml (servicios a eliminar)

- StaffService (L762-803)
- EventTrackingService (L878-930)
- BackgroundRemovalService (L1073-1112)
- Vehicle360ProcessingService (L1126-1172)
- DealerManagementService (L1184-1223)
- AlertService (L1331-1370)
- MaintenanceService (L1384-1423)
- SchedulerService (L1597-1636)
- CacheService (L1706+)
- ConfigurationService (L2034)

### cardealer.sln

- Eliminar todas las referencias a proyectos de los 26 servicios eliminados
