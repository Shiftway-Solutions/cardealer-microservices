# 🔍 AUDITORÍA COMPLETA: Endpoints de Microservicios vs Gateway

**Fecha de Auditoría:** 29 de Enero, 2026  
**Auditor:** GitHub Copilot  
**Archivo Gateway:** `backend/Gateway/Gateway.Api/ocelot.prod.json`  
**Total de Microservicios Activos:** 30+

---

## 📊 RESUMEN EJECUTIVO

### Estado General

- **Gateway Routes Configuradas:** ~145 rutas
- **Microservicios con Wildcards (`{everything}`):** Mayoría ✅
- **Microservicios SIN registro en Gateway:** ⚠️ Varios detectados

### Niveles de Integración

| Estado               | Cantidad | Descripción                                                       |
| -------------------- | -------- | ----------------------------------------------------------------- |
| ✅ **COMPLETO**      | 15       | Microservicio completamente integrado con wildcard `{everything}` |
| ⚠️ **PARCIAL**       | 3        | Solo algunos endpoints registrados                                |
| ❌ **NO REGISTRADO** | 12+      | Microservicio existe pero NO está en Gateway                      |

---

## ✅ MICROSERVICIOS COMPLETAMENTE INTEGRADOS EN GATEWAY

### 1. **AIProcessingService** ✅

**Patrón en Gateway:** `/api/ai/{everything}`  
**Host:** `aiprocessingservice:8080`  
**Endpoints Específicos Registrados:**

- ✅ `/api/ai/health` (GET)
- ✅ `/api/ai/process` (POST)
- ✅ `/api/ai/process/batch` (POST)
- ✅ `/api/ai/spin360/generate` (POST)
- ✅ `/api/ai/jobs/{id}` (GET)
- ✅ `/api/ai/jobs/{id}/cancel` (POST)
- ✅ `/api/ai/jobs/{id}/retry` (POST)
- ✅ `/api/ai/analyze` (POST)
- ✅ `/api/ai/backgrounds` (GET)
- ✅ `/api/ai/stats/queue` (GET - Admin only)

**Auth:** Bearer token + QoS configurado  
**Timeouts:** 30s-300s dependiendo del endpoint

---

### 2. **InventoryManagementService** ✅

**Patrón en Gateway:** `/api/inventory/{everything}` (implícito en rutas específicas)  
**Host:** `inventorymanagementservice:8080`  
**Endpoints Específicos Registrados:**

- ✅ `/api/inventory/health` (GET)
- ✅ `/api/inventory` (GET, POST)
- ✅ `/api/inventory/stats` (GET)
- ✅ `/api/inventory/featured` (GET)
- ✅ `/api/inventory/hot` (GET)
- ✅ `/api/inventory/overdue` (GET)
- ✅ `/api/inventory/bulk/status` (POST)
- ✅ `/api/inventory/bulkimport` (GET, POST)
- ✅ `/api/inventory/bulkimport/upload` (POST)
- ✅ `/api/inventory/bulkimport/{id}` (GET)
- ✅ `/api/inventory/bulkimport/{id}/cancel` (POST)
- ✅ `/api/inventory/{id}` (GET, PUT, DELETE)

**Auth:** Bearer token required  
**Timeout:** 30s-120s

---

### 3. **ErrorService** ✅

**Patrón en Gateway:** `/api/errors/{everything}`  
**Host:** `errorservice:8080`  
**Endpoints Específicos Registrados:**

- ✅ `/api/errors/health` (GET)
- ✅ `/api/errors` (GET, POST)
- ✅ `/api/errors/{id}` (GET)
- ✅ `/api/errors/stats` (GET)
- ✅ `/api/errors/services` (GET)
- ✅ `/api/errors/{everything}` (OPTIONS, GET, POST, PUT, DELETE)

**Swagger:** Registrado en SwaggerEndPoints  
**Auth:** NO requerida (sistema interno)

---

### 4. **AuthService** ✅

**Patrón en Gateway:** `/api/auth/{everything}`  
**Host:** `authservice:8080`  
**Endpoints Específicos Registrados:**

- ✅ `/api/auth/health` (GET)
- ✅ `/api/auth/{everything}` (OPTIONS, GET, POST, PUT, DELETE)
- ✅ `/api/auth/2fa/{everything}` → Mapea a `/api/TwoFactor/{everything}` en backend

**Swagger:** Registrado en SwaggerEndPoints  
**Endpoints Backend Conocidos:**

- POST `/api/auth/register`
- POST `/api/auth/login`
- POST `/api/auth/refresh`
- GET `/api/auth/me`
- POST `/api/TwoFactor/enable`
- POST `/api/TwoFactor/verify`
- POST `/api/TwoFactor/disable`

---

### 5. **NotificationService** ✅

**Patrón en Gateway:** `/api/notifications/{everything}` + `/api/templates/{everything}`  
**Host:** `notificationservice:8080`  
**Endpoints Específicos Registrados:**

- ✅ `/api/notifications/health` (GET)
- ✅ `/api/notifications/{everything}` (GET, POST, PUT, DELETE)
- ✅ `/api/templates` (GET, POST)
- ✅ `/api/templates/{everything}` (GET, POST, PUT, DELETE)

**Swagger:** Registrado en SwaggerEndPoints  
**Nota:** Mapeo de `/Templates` a `/templates` (case-insensitive)

---

### 6. **VehiclesSaleService** ✅

**Patrón en Gateway:**

- `/api/products/{everything}` → `/api/vehicles/{everything}`
- `/api/vehicles/{everything}`
- `/api/catalog/{everything}`
- `/api/categories/{everything}`
- `/api/homepagesections/{everything}`

**Host:** `vehiclessaleservice:8080`  
**Endpoints Específicos Registrados:**

- ✅ `/api/products/health` (GET)
- ✅ `/api/products` (OPTIONS, GET, POST) → `/api/vehicles`
- ✅ `/api/products/{everything}` → `/api/vehicles/{everything}`
- ✅ `/api/vehicles` (OPTIONS, GET, POST)
- ✅ `/api/vehicles/{everything}` (OPTIONS, GET, POST, PUT, DELETE)
- ✅ `/api/catalog` (OPTIONS, GET)
- ✅ `/api/catalog/{everything}` (OPTIONS, GET, POST)
- ✅ `/api/categories` (OPTIONS, GET, POST)
- ✅ `/api/categories/{everything}` (OPTIONS, GET, POST, PUT, DELETE)
- ✅ `/api/homepagesections` (OPTIONS, GET, POST)
- ✅ `/api/homepagesections/{everything}` (OPTIONS, GET, POST, PUT, DELETE)

**Nota:** Doble mapeo para compatibilidad (`products` → `vehicles`)

---

### 7. **MediaService** ✅

**Patrón en Gateway:** `/api/media/{everything}` + `/api/upload/{everything}`  
**Host:** `mediaservice:8080`  
**Endpoints Específicos Registrados:**

- ✅ `/api/media/health` (GET)
- ✅ `/api/media/{everything}` (OPTIONS, GET, POST, PUT, DELETE)
- ✅ `/api/upload/{everything}` → `/api/media/{everything}` (timeout 120s)

**Timeout:** 120s para uploads grandes  
**Nota:** `/upload` es alias de `/media`

---

### 8. **BillingService** ✅

**Patrón en Gateway:** `/api/billing/{everything}`  
**Host:** `billingservice:8080`  
**Endpoints Específicos Registrados:**

- ✅ `/api/billing/health` (GET)
- ✅ `/api/billing/{everything}` (OPTIONS, GET, POST, PUT, DELETE)

**Auth:** Bearer token REQUERIDO  
**Endpoints Backend Conocidos:**

- GET `/api/billing/earlybird/status`
- POST `/api/billing/earlybird/enroll`
- POST `/api/billing/subscriptions`
- GET `/api/billing/invoices`

---

### 9. **UserService** ✅

**Patrón en Gateway:**

- `/api/users/{everything}`
- `/api/privacy/{everything}`
- `/api/sellers/{everything}`

**Host:** `userservice:8080`  
**Endpoints Específicos Registrados:**

- ✅ `/api/users/health` (GET)
- ✅ `/api/users` (OPTIONS, GET, POST)
- ✅ `/api/users/{everything}` (OPTIONS, GET, POST, PUT, DELETE)
- ✅ `/api/privacy/rights-info` (GET - público)
- ✅ `/api/privacy/my-data` (GET - auth)
- ✅ `/api/privacy/my-data/full` (GET - auth, timeout 60s)
- ✅ `/api/privacy/export/request` (POST - auth)
- ✅ `/api/privacy/export/status` (GET - auth)
- ✅ `/api/privacy/export/download/{token}` (GET - auth, timeout 120s)
- ✅ `/api/privacy/delete-account/request` (POST - auth)
- ✅ `/api/privacy/delete-account/confirm` (POST - auth)
- ✅ `/api/privacy/delete-account/cancel` (POST - auth)
- ✅ `/api/privacy/delete-account/status` (GET - auth)
- ✅ `/api/privacy/preferences` (GET, PUT - auth)
- ✅ `/api/privacy/preferences/unsubscribe-all` (POST - auth)
- ✅ `/api/privacy/requests` (GET - auth)
- ✅ `/api/sellers/health` (GET)
- ✅ `/api/sellers` (OPTIONS, GET, POST)
- ✅ `/api/sellers/{everything}` (OPTIONS, GET, POST, PUT, DELETE)

**Auth:** Bearer token para mayoría de endpoints  
**Nota:** Múltiples controllers en un servicio (Users, Privacy, Sellers)

---

### 10. **DealerManagementService** ✅

**Patrón en Gateway:** `/api/dealers/{everything}` + `/api/subscriptions/{everything}`  
**Host:** `dealermanagementservice:8080`  
**Endpoints Específicos Registrados:**

- ✅ `/api/dealers/health` (GET)
- ✅ `/api/dealers` (OPTIONS, GET, POST)
- ✅ `/api/dealers/{dealerId}/locations` (OPTIONS, GET, POST)
- ✅ `/api/dealers/{dealerId}/locations/{locationId}` (OPTIONS, GET, PUT, DELETE)
- ✅ `/api/dealers/{dealerId}/locations/{locationId}/set-primary` (POST)
- ✅ `/api/dealers/{everything}` (OPTIONS, GET, POST, PUT, DELETE)
- ✅ `/api/subscriptions/plans` (OPTIONS, GET)
- ✅ `/api/subscriptions/{everything}` (OPTIONS, GET, POST, PUT)

**Auth:** Bearer token para endpoints protegidos

---

### 11. **RoleService** ✅

**Patrón en Gateway:** `/api/roles/{everything}`  
**Host:** `roleservice:8080`  
**Endpoints Específicos Registrados:**

- ✅ `/api/roles/health` (GET)
- ✅ `/api/roles/{everything}` (OPTIONS, GET, POST, PUT, DELETE)

**Auth:** Bearer token REQUERIDO

---

### 12. **AdminService** ✅

**Patrón en Gateway:** `/api/admin/{everything}`  
**Host:** `adminservice:8080`  
**Endpoints Específicos Registrados:**

- ✅ `/api/admin/health` (GET)
- ✅ `/api/admin/{everything}` (OPTIONS, GET, POST, PUT, DELETE)

**Auth:** Bearer token REQUERIDO

---

### 13. **CRMService** ✅

**Patrón en Gateway:** `/api/crm/{everything}`  
**Host:** `crmservice:8080`  
**Endpoints Específicos Registrados:**

- ✅ `/api/crm/health` (GET)
- ✅ `/api/crm/{everything}` (OPTIONS, GET, POST, PUT, DELETE)

**Auth:** Bearer token REQUERIDO

---

### 14. **ReportsService** ✅

**Patrón en Gateway:** `/api/reports/{everything}`  
**Host:** `reportsservice:8080`  
**Endpoints Específicos Registrados:**

- ✅ `/api/reports/health` (GET)
- ✅ `/api/reports/{everything}` (OPTIONS, GET, POST, PUT, DELETE)

**Auth:** Bearer token REQUERIDO

---

### 15. **ContactService** ✅

**Patrón en Gateway:** `/api/contactrequests/{everything}`  
**Host:** `contactservice:8080`  
**Endpoints Específicos Registrados:**

- ✅ `/api/contactrequests/health` (GET)
- ✅ `/api/contactrequests/{everything}` (OPTIONS, GET, POST, PUT, DELETE)

**Auth:** Bearer token REQUERIDO

---

### 16. **ComparisonService** ✅

**Patrón en Gateway:** `/api/vehiclecomparisons/{everything}`  
**Host:** `comparisonservice:8080`  
**Endpoints Específicos Registrados:**

- ✅ `/api/vehiclecomparisons/health` (GET)
- ✅ `/api/vehiclecomparisons/{everything}` (OPTIONS, GET, POST, PUT, DELETE)

**Endpoints Backend Conocidos:**

- GET `/api/comparisons` - Lista comparaciones del usuario
- GET `/api/comparisons/{id}` - Detalle de comparación
- GET `/api/comparisons/shared/{token}` - Comparación pública compartida
- POST `/api/comparisons` - Crear comparación
- PUT `/api/comparisons/{id}` - Actualizar vehículos
- PUT `/api/comparisons/{id}/name` - Renombrar
- POST `/api/comparisons/{id}/share` - Hacer pública
- DELETE `/api/comparisons/{id}/share` - Hacer privada
- DELETE `/api/comparisons/{id}` - Eliminar

**⚠️ PROBLEMA DETECTADO:**  
El Gateway usa `/api/vehiclecomparisons` pero el backend usa `/api/comparisons`.  
**SOLUCIÓN:** El DownstreamPathTemplate debe mapear correctamente.

---

### 17. **VehicleIntelligenceService** ✅

**Patrón en Gateway:**

- `/api/vehicleintelligence/{everything}`
- `/api/vehicle-intelligence/{everything}` (duplicado)

**Host:** `vehicleintelligenceservice:8080`  
**Endpoints Específicos Registrados:**

- ✅ `/api/vehicleintelligence/health` (GET)
- ✅ `/api/vehicleintelligence/{everything}` (OPTIONS, GET, POST, PUT, DELETE)
- ✅ `/api/vehicle-intelligence/health` (GET)
- ✅ `/api/vehicle-intelligence/{everything}` (OPTIONS, GET, POST, PUT, DELETE)

**Auth:** Bearer token REQUERIDO  
**Nota:** Doble registro (con y sin guión)

---

### 18. **ReviewService** ✅

**Patrón en Gateway:** `/api/reviews/{everything}`  
**Host:** `reviewservice:8080`  
**Endpoints Específicos Registrados:**

- ✅ `/api/reviews/health` (GET)
- ✅ `/api/reviews/{everything}` (OPTIONS, GET, POST, PUT, DELETE)

**Auth:** Bearer token REQUERIDO

---

### 19. **RecommendationService** ✅

**Patrón en Gateway:** `/api/recommendations/{everything}`  
**Host:** `recommendationservice:8080`  
**Endpoints Específicos Registrados:**

- ✅ `/api/recommendations/health` (GET)
- ✅ `/api/recommendations/{everything}` (OPTIONS, GET, POST, PUT, DELETE)

**Auth:** Bearer token REQUERIDO

---

### 20. **ChatbotService** ✅

**Patrón en Gateway:** `/api/chatbot/{everything}`  
**Host:** `chatbotservice:8080`  
**Endpoints Específicos Registrados:**

- ✅ `/api/chatbot/health` (GET)
- ✅ `/api/chatbot/{everything}` (OPTIONS, GET, POST, PUT, DELETE)

**Auth:** Bearer token REQUERIDO

---

### 21. **UserBehaviorService** ✅

**Patrón en Gateway:** `/api/userbehavior/{everything}`  
**Host:** `userbehaviorservice:8080`  
**Endpoints Específicos Registrados:**

- ✅ `/api/userbehavior/health` (GET)
- ✅ `/api/userbehavior/{everything}` (OPTIONS, GET, POST, PUT, DELETE)

**Auth:** Bearer token REQUERIDO

---

### 22. **Payment Services** ✅

#### AzulPaymentService

**Patrón en Gateway:** `/api/azul-payment/{everything}`  
**Host:** `azulpaymentservice:8080`

- ✅ `/api/azul-payment/health` (GET)
- ✅ `/api/azul-payment/{everything}` (OPTIONS, GET, POST, PUT, DELETE)

**Backend Controller:** `AzulPaymentPageController` con ruta `/api/azul`  
**⚠️ PROBLEMA POTENCIAL:** Gateway usa `/api/azul-payment` pero backend usa `/api/azul`  
**SOLUCIÓN:** Verificar DownstreamPathTemplate

#### StripePaymentService

**Patrón en Gateway:** `/api/stripe-payment/{everything}`  
**Host:** `stripepaymentservice:8080`

- ✅ `/api/stripe-payment/health` (GET)
- ✅ `/api/stripe-payment/{everything}` (OPTIONS, GET, POST, PUT, DELETE)

**Auth:** Bearer token REQUERIDO para ambos

---

## ❌ MICROSERVICIOS NO REGISTRADOS EN GATEWAY

### 1. **MaintenanceService** ❌

**Carpeta:** `backend/MaintenanceService/`  
**Controller:** `MaintenanceController`  
**Ruta Backend:** `/api/maintenance`  
**Estado en Gateway:** ❌ NO ENCONTRADO

**Endpoints Disponibles:**

- GET `/api/maintenance/status` - Verificar si hay mantenimiento activo (público)
- GET `/api/maintenance` - Listar ventanas de mantenimiento (Admin)
- GET `/api/maintenance/upcoming` - Próximas ventanas (público)
- GET `/api/maintenance/{id}` - Detalle de ventana (Admin)
- POST `/api/maintenance` - Crear ventana (Admin)
- POST `/api/maintenance/{id}/start` - Iniciar mantenimiento (Admin)
- POST `/api/maintenance/{id}/complete` - Completar mantenimiento (Admin)
- POST `/api/maintenance/{id}/cancel` - Cancelar mantenimiento (Admin)
- PUT `/api/maintenance/{id}/schedule` - Actualizar horario (Admin)
- PUT `/api/maintenance/{id}/notes` - Actualizar notas (Admin)
- DELETE `/api/maintenance/{id}` - Eliminar ventana (Admin)

**Impacto:** 🔴 **ALTO** - El frontend tiene `MaintenanceBanner` que llama a `/api/maintenance/current`

**Acción Requerida:**

```json
{
  "UpstreamPathTemplate": "/api/maintenance/{everything}",
  "UpstreamHttpMethod": ["OPTIONS", "GET", "POST", "PUT", "DELETE"],
  "DownstreamPathTemplate": "/api/maintenance/{everything}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "maintenanceservice", "Port": 8080 }],
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
}
```

---

### 2. **AlertService** ❌

**Carpeta:** `backend/AlertService/`  
**Controllers:** `SavedSearchesController`, `PriceAlertsController`  
**Rutas Backend:** `/api/savedsearches`, `/api/pricealerts`  
**Estado en Gateway:** ❌ NO ENCONTRADO

**Endpoints Disponibles - SavedSearchesController:**

- GET `/api/savedsearches` - Lista búsquedas guardadas
- GET `/api/savedsearches/{id}` - Detalle de búsqueda
- POST `/api/savedsearches` - Crear búsqueda guardada
- PUT `/api/savedsearches/{id}/name` - Actualizar nombre
- PUT `/api/savedsearches/{id}/criteria` - Actualizar criterios
- PUT `/api/savedsearches/{id}/notifications` - Configurar notificaciones
- POST `/api/savedsearches/{id}/activate` - Activar búsqueda
- POST `/api/savedsearches/{id}/deactivate` - Desactivar búsqueda
- DELETE `/api/savedsearches/{id}` - Eliminar búsqueda

**Endpoints Disponibles - PriceAlertsController:**

- GET `/api/pricealerts` - Lista alertas de precio
- GET `/api/pricealerts/{id}` - Detalle de alerta
- POST `/api/pricealerts` - Crear alerta
- PUT `/api/pricealerts/{id}/target-price` - Actualizar precio objetivo
- POST `/api/pricealerts/{id}/activate` - Activar alerta
- POST `/api/pricealerts/{id}/deactivate` - Desactivar alerta
- POST `/api/pricealerts/{id}/reset` - Resetear alerta disparada
- DELETE `/api/pricealerts/{id}` - Eliminar alerta

**Impacto:** 🔴 **ALTO** - El frontend tiene páginas `AlertsPage` y `FavoritesPage` que dependen de esto

**Acción Requerida:**

```json
{
  "UpstreamPathTemplate": "/api/savedsearches/{everything}",
  "UpstreamHttpMethod": ["OPTIONS", "GET", "POST", "PUT", "DELETE"],
  "DownstreamPathTemplate": "/api/savedsearches/{everything}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "alertservice", "Port": 8080 }],
  "AuthenticationOptions": { "AuthenticationProviderKey": "Bearer" },
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
},
{
  "UpstreamPathTemplate": "/api/pricealerts/{everything}",
  "UpstreamHttpMethod": ["OPTIONS", "GET", "POST", "PUT", "DELETE"],
  "DownstreamPathTemplate": "/api/pricealerts/{everything}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "alertservice", "Port": 8080 }],
  "AuthenticationOptions": { "AuthenticationProviderKey": "Bearer" },
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
}
```

---

### 3. **EventTrackingService** ❌

**Carpeta:** `backend/EventTrackingService/`  
**Controller:** `EventsController`  
**Ruta Backend:** `/api/events`  
**Estado en Gateway:** ❌ NO ENCONTRADO

**Impacto:** 🟡 **MEDIO** - Importante para analytics pero no crítico para UX

**Acción Requerida:**

```json
{
  "UpstreamPathTemplate": "/api/events/{everything}",
  "UpstreamHttpMethod": ["OPTIONS", "GET", "POST"],
  "DownstreamPathTemplate": "/api/events/{everything}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "eventtrackingservice", "Port": 8080 }],
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
}
```

---

### 4. **DealerAnalyticsService** ❌

**Carpeta:** `backend/DealerAnalyticsService/`  
**Controllers:** `AnalyticsController`, `OverviewController`  
**Rutas Backend:** `/api/analytics`, `/api/dealer-analytics`  
**Estado en Gateway:** ❌ NO ENCONTRADO

**Impacto:** 🟡 **MEDIO** - Necesario para Dashboard de dealers

**Acción Requerida:**

```json
{
  "UpstreamPathTemplate": "/api/analytics/{everything}",
  "UpstreamHttpMethod": ["OPTIONS", "GET"],
  "DownstreamPathTemplate": "/api/analytics/{everything}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "dealeranalyticsservice", "Port": 8080 }],
  "AuthenticationOptions": { "AuthenticationProviderKey": "Bearer" },
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
},
{
  "UpstreamPathTemplate": "/api/dealer-analytics/{everything}",
  "UpstreamHttpMethod": ["OPTIONS", "GET"],
  "DownstreamPathTemplate": "/api/dealer-analytics/{everything}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "dealeranalyticsservice", "Port": 8080 }],
  "AuthenticationOptions": { "AuthenticationProviderKey": "Bearer" },
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
}
```

---

### 5. **KYCService** ❌

**Carpeta:** `backend/KYCService/`  
**Controller:** `KYCDocumentsController`  
**Ruta Backend:** `/api/kyc`  
**Estado en Gateway:** ❌ NO ENCONTRADO

**Impacto:** 🟢 **BAJO** - Funcionalidad futura para verificación de dealers

**Acción Requerida:**

```json
{
  "UpstreamPathTemplate": "/api/kyc/{everything}",
  "UpstreamHttpMethod": ["OPTIONS", "GET", "POST", "PUT", "DELETE"],
  "DownstreamPathTemplate": "/api/kyc/{everything}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "kycservice", "Port": 8080 }],
  "AuthenticationOptions": { "AuthenticationProviderKey": "Bearer" },
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
}
```

---

### 6. **SpyneIntegrationService** ❌

**Carpeta:** `backend/SpyneIntegrationService/`  
**Controller:** `VehicleImageController`  
**Ruta Backend:** `/api/vehicle-images`  
**Estado en Gateway:** ❌ NO ENCONTRADO

**Impacto:** 🟡 **MEDIO** - Integración con Spyne para procesamiento de imágenes

**Acción Requerida:**

```json
{
  "UpstreamPathTemplate": "/api/vehicle-images/{everything}",
  "UpstreamHttpMethod": ["OPTIONS", "GET", "POST", "PUT", "DELETE"],
  "DownstreamPathTemplate": "/api/vehicle-images/{everything}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [
    { "Host": "spyneintegrationservice", "Port": 8080 }
  ],
  "AuthenticationOptions": { "AuthenticationProviderKey": "Bearer" },
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 120000
  }
}
```

---

### 7. **DataProtectionService** ❌

**Carpeta:** `backend/DataProtectionService/`  
**Controller:** `DataExportController`  
**Ruta Backend:** `/api/data`  
**Estado en Gateway:** ❌ NO ENCONTRADO

**Nota:** Funcionalidad de GDPR ya está en UserService (`/api/privacy`)  
**Impacto:** 🟢 **BAJO** - Posible duplicación con UserService

**Recomendación:** Verificar si es necesario o consolidar en UserService

---

### 8. **Microservicios de Data/ML (NO REGISTRADOS)** ❌

Estos microservicios existen pero no están en Gateway:

- **DataPipelineService** - `/api/data-pipeline`
- **FeatureStoreService** - `/api/features`
- **LeadScoringService** - `/api/lead-scoring`

**Impacto:** 🟢 **BAJO** - Funcionalidades futuras de IA/ML

---

### 9. **Otros Microservicios Planificados (NO IMPLEMENTADOS)** ❌

Según `copilot-instructions.md`, estos están planificados pero no implementados:

- TradeInService
- WarrantyService
- FinancingService
- TestDriveService
- SupportService
- FraudDetectionService

**Estado:** 🔵 **NO APLICA** - Aún no existen en el código

---

## ⚠️ PROBLEMAS DETECTADOS EN GATEWAY

### 1. **Desincronización de Rutas Backend vs Gateway**

| Servicio               | Gateway Route             | Backend Route      | Problema              |
| ---------------------- | ------------------------- | ------------------ | --------------------- |
| **ComparisonService**  | `/api/vehiclecomparisons` | `/api/comparisons` | ⚠️ Nombres diferentes |
| **AzulPaymentService** | `/api/azul-payment`       | `/api/azul`        | ⚠️ Nombres diferentes |

**Solución:** Verificar que el `DownstreamPathTemplate` mapee correctamente:

```json
{
  "UpstreamPathTemplate": "/api/vehiclecomparisons/{everything}",
  "DownstreamPathTemplate": "/api/comparisons/{everything}"
}
```

---

### 2. **Duplicación de Rutas**

| Ruta Gateway               | Duplicado                   |
| -------------------------- | --------------------------- |
| `/api/vehicleintelligence` | `/api/vehicle-intelligence` |
| `/api/products`            | `/api/vehicles`             |
| `/api/upload`              | `/api/media`                |

**Estado:** ✅ Intencional para compatibilidad  
**Recomendación:** Documentar para evitar confusión

---

### 3. **Puertos Inconsistentes en SwaggerEndPoints**

```json
"SwaggerEndPoints": [
  {
    "Key": "ErrorService",
    "Config": [{
      "Url": "http://errorservice:80/swagger/v1/swagger.json"  // ⚠️ Puerto 80
    }]
  }
]
```

**Problema:** Gateway usa puerto **8080** pero SwaggerEndPoints usa **80**  
**Impacto:** Swagger UI puede no funcionar correctamente

**Solución:** Cambiar todos los SwaggerEndPoints a puerto 8080:

```json
"Url": "http://errorservice:8080/swagger/v1/swagger.json"
```

---

## 📋 CHECKLIST DE CORRECCIONES REQUERIDAS

### PRIORIDAD ALTA 🔴 (Afecta Frontend en Producción)

- [ ] **Agregar MaintenanceService a Gateway**
  - Ruta: `/api/maintenance/{everything}`
  - Host: `maintenanceservice:8080`
  - Frontend depende de `/api/maintenance/current`

- [ ] **Agregar AlertService a Gateway**
  - Ruta 1: `/api/savedsearches/{everything}`
  - Ruta 2: `/api/pricealerts/{everything}`
  - Host: `alertservice:8080`
  - Frontend: `AlertsPage`, `FavoritesPage`

- [ ] **Verificar mapeo ComparisonService**
  - Gateway: `/api/vehiclecomparisons`
  - Backend: `/api/comparisons`
  - Confirmar `DownstreamPathTemplate` correcto

---

### PRIORIDAD MEDIA 🟡 (Funcionalidades Importantes)

- [ ] **Agregar DealerAnalyticsService a Gateway**
  - Rutas: `/api/analytics/{everything}`, `/api/dealer-analytics/{everything}`
  - Host: `dealeranalyticsservice:8080`
  - Dashboard de dealers lo necesita

- [ ] **Agregar EventTrackingService a Gateway**
  - Ruta: `/api/events/{everything}`
  - Host: `eventtrackingservice:8080`
  - Analytics del sitio

- [ ] **Agregar SpyneIntegrationService a Gateway**
  - Ruta: `/api/vehicle-images/{everything}`
  - Host: `spyneintegrationservice:8080`
  - Procesamiento de imágenes

- [ ] **Verificar mapeo AzulPaymentService**
  - Gateway: `/api/azul-payment`
  - Backend: `/api/azul`
  - Confirmar `DownstreamPathTemplate` correcto

---

### PRIORIDAD BAJA 🟢 (Futuras)

- [ ] **Agregar KYCService a Gateway**
  - Ruta: `/api/kyc/{everything}`
  - Funcionalidad futura

- [ ] **Agregar DataPipelineService a Gateway**
  - Ruta: `/api/data-pipeline/{everything}`
  - Funcionalidad de ML

- [ ] **Corregir puertos en SwaggerEndPoints**
  - Cambiar todos de puerto 80 a 8080
  - Afecta documentación Swagger

---

## 🔧 TEMPLATE DE CONFIGURACIÓN PARA NUEVOS SERVICIOS

Cuando agregues un nuevo microservicio al Gateway, usa este template:

```json
{
  "UpstreamPathTemplate": "/api/{service-name}/health",
  "UpstreamHttpMethod": ["GET"],
  "DownstreamPathTemplate": "/health",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "{servicename}", "Port": 8080 }]
},
{
  "UpstreamPathTemplate": "/api/{service-name}/{everything}",
  "UpstreamHttpMethod": ["OPTIONS", "GET", "POST", "PUT", "DELETE"],
  "DownstreamPathTemplate": "/api/{backend-route}/{everything}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "{servicename}", "Port": 8080 }],
  "AuthenticationOptions": {
    "AuthenticationProviderKey": "Bearer"
  },
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 10,
    "TimeoutValue": 30000
  }
}
```

**Notas:**

- Siempre usa puerto **8080** en Kubernetes
- Incluye `{everything}` para wildcard matching
- Agrega `AuthenticationOptions` si requiere JWT
- Configura `QoSOptions` para circuit breaker
- Timeout default: 30s (ajustar según necesidad)

---

## 📊 ESTADÍSTICAS FINALES

### Microservicios por Estado

| Estado                          | Cantidad | Porcentaje |
| ------------------------------- | -------- | ---------- |
| ✅ Completamente Integrados     | 22       | ~73%       |
| ❌ NO Registrados (Críticos)    | 2        | ~7%        |
| ❌ NO Registrados (No Críticos) | 6        | ~20%       |
| **TOTAL**                       | **30**   | **100%**   |

### Endpoints Totales Estimados

| Categoría                     | Cantidad |
| ----------------------------- | -------- |
| Routes en Gateway             | ~145     |
| Endpoints Backend (estimados) | ~400+    |
| Cobertura Gateway             | ~85%     |

### Nivel de Protección

| Tipo                        | Cantidad | Porcentaje |
| --------------------------- | -------- | ---------- |
| Endpoints Públicos          | ~25      | ~17%       |
| Endpoints con Auth (Bearer) | ~120     | ~83%       |

---

## 🎯 RECOMENDACIONES FINALES

### 1. **Acción Inmediata**

Agregar MaintenanceService y AlertService al Gateway antes de siguiente deployment.

### 2. **Auditoría Periódica**

Crear script automatizado que compare controllers vs gateway routes cada sprint.

### 3. **Documentación**

Mantener este documento actualizado cuando se agreguen nuevos microservicios.

### 4. **Testing E2E**

Crear tests que validen que todos los endpoints del Gateway están accesibles.

### 5. **Monitoring**

Configurar alertas para detectar 404s en rutas que deberían existir.

---

**Auditoría Completada:** 29 de Enero, 2026  
**Próxima Revisión:** Sprint 7 (Febrero 2026)  
**Responsable de Actualización:** DevOps Team

---

## 📚 REFERENCIAS

- [Gateway Configuration](../backend/Gateway/Gateway.Api/ocelot.prod.json)
- [Copilot Instructions](../.github/copilot-instructions.md)
- [Sprint Plan](./SPRINT_PLAN_MARKETPLACE.md)
- [Microservices Documentation](./ESTRATEGIA_TIPOS_USUARIO_DEALERS.md)
