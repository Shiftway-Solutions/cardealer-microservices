# 🗺️ Mapeo Páginas ↔ APIs

> **Fecha:** Enero 30, 2026  
> **Propósito:** Referencia rápida para saber qué endpoints consume cada página  
> **Total Páginas:** 80 documentos  
> **Total Servicios Backend:** 15+ microservicios

---

## 📋 ÍNDICE DE MAPEO

1. [01-PUBLICO](#01-publico---páginas-públicas)
2. [02-AUTH](#02-auth---autenticación)
3. [03-COMPRADOR](#03-comprador---buyer-flows)
4. [04-VENDEDOR](#04-vendedor---seller-flows)
5. [05-DEALER](#05-dealer---portal-dealers)
6. [06-ADMIN](#06-admin---panel-administrativo)
7. [07-PAGOS](#07-pagos---billing--checkout)
8. [08-DGII-COMPLIANCE](#08-dgii-compliance---cumplimiento-legal)
9. [09-COMPONENTES-COMUNES](#09-componentes-comunes---shared-components)

---

## 🔌 SERVICIOS BACKEND DISPONIBLES

| Servicio                    | Puerto | Descripción                        | Documentación API                                                             |
| --------------------------- | ------ | ---------------------------------- | ----------------------------------------------------------------------------- |
| **VehiclesSaleService**     | 5030   | CRUD vehículos, catálogo, búsqueda | [06-vehicles-api.md](05-API-INTEGRATION/06-vehicles-api.md)                   |
| **AuthService**             | 5001   | Login, registro, OAuth, 2FA        | [02-autenticacion.md](05-API-INTEGRATION/02-autenticacion.md)                 |
| **UserService**             | 5002   | Perfiles, preferencias             | [07-users-api.md](05-API-INTEGRATION/07-users-api.md)                         |
| **MediaService**            | 5005   | Upload imágenes, S3                | [04-subida-imagenes.md](05-API-INTEGRATION/04-subida-imagenes.md)             |
| **NotificationService**     | 5006   | Email, SMS, Push                   | [09-notification-api.md](05-API-INTEGRATION/09-notification-api.md)           |
| **ContactService**          | 5007   | Inquiries, mensajes                | [08-contact-api.md](05-API-INTEGRATION/08-contact-api.md)                     |
| **BillingService**          | 5010   | Pagos, suscripciones               | [11-billing-api.md](05-API-INTEGRATION/11-billing-api.md)                     |
| **AdminService**            | 5015   | Panel admin                        | [14-admin-api.md](05-API-INTEGRATION/14-admin-api.md)                         |
| **DealerManagementService** | 5039   | Gestión dealers                    | [12-dealer-management-api.md](05-API-INTEGRATION/12-dealer-management-api.md) |
| **RoleService**             | 5003   | Roles y permisos                   | [10-roles-api.md](05-API-INTEGRATION/10-roles-api.md)                         |
| **ErrorService**            | 5008   | Logs de errores                    | [30-errors-api.md](05-API-INTEGRATION/30-errors-api.md)                       |

---

## 01-PUBLICO - Páginas Públicas

| Documento                                                                | Ruta Frontend          | Servicios Backend                              | Endpoints Principales                                                                                                |
| ------------------------------------------------------------------------ | ---------------------- | ---------------------------------------------- | -------------------------------------------------------------------------------------------------------------------- |
| [01-home.md](04-PAGINAS/01-PUBLICO/01-home.md)                           | `/`                    | VehiclesSaleService                            | `GET /api/vehicles/featured`<br>`GET /api/homepagesections/homepage`<br>`GET /api/catalog/makes`                     |
| [02-busqueda.md](04-PAGINAS/01-PUBLICO/02-busqueda.md)                   | `/search`, `/buscar`   | VehiclesSaleService                            | `GET /api/vehicles/search`<br>`GET /api/catalog/makes`<br>`GET /api/catalog/models/{makeId}`                         |
| [03-detalle-vehiculo.md](04-PAGINAS/01-PUBLICO/03-detalle-vehiculo.md)   | `/vehiculo/:slug`      | VehiclesSaleService, ContactService            | `GET /api/vehicles/{id}`<br>`GET /api/vehicles/slug/{slug}`<br>`POST /api/inquiries`                                 |
| [04-help-center.md](04-PAGINAS/01-PUBLICO/04-help-center.md)             | `/ayuda`, `/help`      | NotificationService, MediaService              | `GET /api/help/articles`<br>`GET /api/help/categories`<br>`POST /api/support/tickets`                                |
| [05-vehicle-360-page.md](04-PAGINAS/01-PUBLICO/05-vehicle-360-page.md)   | `/vehiculo/:slug/360`  | VehiclesSaleService, MediaService              | `GET /api/vehicles/{id}/media`<br>`GET /api/vehicles/{id}/360-images`                                                |
| [06-comparador.md](04-PAGINAS/01-PUBLICO/06-comparador.md)               | `/comparar`            | VehiclesSaleService                            | `GET /api/vehicles/{id}` (multiple)<br>`GET /api/comparisons`<br>`POST /api/comparisons`                             |
| [07-filtros-avanzados.md](04-PAGINAS/01-PUBLICO/07-filtros-avanzados.md) | `/search` (filters)    | VehiclesSaleService                            | `GET /api/vehicles/search`<br>`GET /api/catalog/makes`<br>`GET /api/catalog/models`<br>`GET /api/catalog/body-types` |
| [08-search-completo.md](04-PAGINAS/01-PUBLICO/08-search-completo.md)     | `/search`              | VehiclesSaleService, UserService, MediaService | `GET /api/vehicles/search`<br>`POST /api/saved-searches`<br>`GET /api/vehicles/suggestions`                          |
| [09-vehicle-browse.md](04-PAGINAS/01-PUBLICO/09-vehicle-browse.md)       | `/vehiculos/:category` | VehiclesSaleService, ContactService            | `GET /api/vehicles`<br>`GET /api/vehicles/by-category/{category}`                                                    |
| [10-homepage-public.md](04-PAGINAS/01-PUBLICO/10-homepage-public.md)     | `/`                    | VehiclesSaleService, DealerManagementService   | `GET /api/homepagesections/homepage`<br>`GET /api/dealers/featured`                                                  |

---

## 02-AUTH - Autenticación

| Documento                                                                     | Ruta Frontend                      | Servicios Backend         | Endpoints Principales                                                                                             |
| ----------------------------------------------------------------------------- | ---------------------------------- | ------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| [01-auth-login-register.md](04-PAGINAS/02-AUTH/01-auth-login-register.md)     | `/login`, `/register`              | AuthService               | `POST /api/auth/login`<br>`POST /api/auth/register`<br>`POST /api/auth/refresh`<br>`POST /api/auth/logout`        |
| [02-verification-flows.md](04-PAGINAS/02-AUTH/02-verification-flows.md)       | `/verify-email`, `/reset-password` | AuthService               | `POST /api/auth/verify-email`<br>`POST /api/auth/forgot-password`<br>`POST /api/auth/reset-password`              |
| [03-oauth-management.md](04-PAGINAS/02-AUTH/03-oauth-management.md)           | `/settings/security/oauth`         | AuthService               | `GET /api/auth/oauth/providers`<br>`POST /api/auth/oauth/link`<br>`DELETE /api/auth/oauth/unlink/{provider}`      |
| [04-kyc-verificacion.md](04-PAGINAS/02-AUTH/04-kyc-verificacion.md)           | `/verify`, `/kyc`                  | UserService, MediaService | `POST /api/users/kyc/submit`<br>`GET /api/users/kyc/status`<br>`POST /api/media/upload`                           |
| [05-privacy-gdpr.md](04-PAGINAS/02-AUTH/05-privacy-gdpr.md)                   | `/settings/privacy`                | UserService               | `GET /api/users/consents`<br>`PUT /api/users/consents`<br>`POST /api/users/data-export`<br>`DELETE /api/users/me` |
| [06-user-security-privacy.md](04-PAGINAS/02-AUTH/06-user-security-privacy.md) | `/settings/security`               | AuthService, UserService  | `POST /api/auth/2fa/enable`<br>`GET /api/auth/sessions`<br>`DELETE /api/auth/sessions/{id}`                       |

---

## 03-COMPRADOR - Buyer Flows

| Documento                                                                      | Ruta Frontend                      | Servicios Backend                                                                                            | Endpoints Principales                                                                                                                       |
| ------------------------------------------------------------------------------ | ---------------------------------- | ------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------- |
| [01-perfil.md](04-PAGINAS/03-COMPRADOR/01-perfil.md)                           | `/perfil/:id`, `/settings/profile` | UserService, MediaService                                                                                    | `GET /api/users/{id}`<br>`PUT /api/users/{id}`<br>`POST /api/media/upload/avatar`                                                           |
| [02-alertas-busquedas.md](04-PAGINAS/03-COMPRADOR/02-alertas-busquedas.md)     | `/alertas`, `/busquedas-guardadas` | VehiclesSaleService, BillingService, NotificationService                                                     | `GET /api/price-alerts`<br>`POST /api/price-alerts`<br>`GET /api/saved-searches`<br>`POST /api/saved-searches`                              |
| [03-notificaciones.md](04-PAGINAS/03-COMPRADOR/03-notificaciones.md)           | `/notificaciones`                  | NotificationService                                                                                          | `GET /api/notifications`<br>`PUT /api/notifications/{id}/read`<br>`PUT /api/notifications/read-all`<br>`GET /api/notifications/preferences` |
| [04-recomendaciones.md](04-PAGINAS/03-COMPRADOR/04-recomendaciones.md)         | `/recomendaciones`                 | VehiclesSaleService (ML)                                                                                     | `GET /api/recommendations`<br>`GET /api/vehicles/similar/{id}`<br>`POST /api/user-behavior/event`                                           |
| [05-inquiries-messaging.md](04-PAGINAS/03-COMPRADOR/05-inquiries-messaging.md) | `/mensajes`                        | NotificationService, ContactService                                                                          | `GET /api/inquiries`<br>`POST /api/inquiries`<br>`GET /api/messages/threads`<br>`POST /api/messages`                                        |
| [06-reviews-reputacion.md](04-PAGINAS/03-COMPRADOR/06-reviews-reputacion.md)   | `/reviews`, `/dealer/:id/reviews`  | VehiclesSaleService, UserService, DealerManagementService, BillingService, NotificationService, MediaService | `GET /api/reviews`<br>`POST /api/reviews`<br>`GET /api/reviews/seller/{id}`<br>`PUT /api/reviews/{id}`                                      |
| [07-chatbot.md](04-PAGINAS/03-COMPRADOR/07-chatbot.md)                         | Widget flotante                    | NotificationService, ChatbotService                                                                          | `POST /api/chatbot/message`<br>`GET /api/chatbot/history`<br>`POST /api/chatbot/handoff`                                                    |
| [08-favorites-compare.md](04-PAGINAS/03-COMPRADOR/08-favorites-compare.md)     | `/favoritos`, `/comparar`          | VehiclesSaleService                                                                                          | `GET /api/favorites`<br>`POST /api/favorites`<br>`DELETE /api/favorites/{id}`<br>`GET /api/comparisons`                                     |
| [09-user-dashboard.md](04-PAGINAS/03-COMPRADOR/09-user-dashboard.md)           | `/dashboard`                       | VehiclesSaleService, UserService, NotificationService                                                        | `GET /api/users/me/dashboard`<br>`GET /api/users/me/vehicles`<br>`GET /api/notifications/unread-count`                                      |
| [10-user-messages.md](04-PAGINAS/03-COMPRADOR/10-user-messages.md)             | `/mensajes`                        | ContactService, NotificationService                                                                          | `GET /api/messages/threads`<br>`GET /api/messages/thread/{id}`<br>`POST /api/messages`<br>`PUT /api/messages/{id}/read`                     |

---

## 04-VENDEDOR - Seller Flows

| Documento                                                                 | Ruta Frontend         | Servicios Backend                                  | Endpoints Principales                                                                                                  |
| ------------------------------------------------------------------------- | --------------------- | -------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| [01-publicar-vehiculo.md](04-PAGINAS/04-VENDEDOR/01-publicar-vehiculo.md) | `/publicar`           | VehiclesSaleService, MediaService                  | `POST /api/vehicles`<br>`POST /api/vehicles/draft`<br>`POST /api/media/upload`<br>`GET /api/catalog/makes`             |
| [02-seller-dashboard.md](04-PAGINAS/04-VENDEDOR/02-seller-dashboard.md)   | `/vendedor/dashboard` | VehiclesSaleService, AuthService                   | `GET /api/vehicles/my`<br>`GET /api/vehicles/my/stats`<br>`PUT /api/vehicles/{id}/status`                              |
| [03-seller-profiles.md](04-PAGINAS/04-VENDEDOR/03-seller-profiles.md)     | `/vendedor/:id`       | UserService, DealerManagementService, MediaService | `GET /api/sellers/{id}`<br>`GET /api/sellers/{id}/vehicles`<br>`GET /api/sellers/{id}/reviews`                         |
| [04-sell-your-car.md](04-PAGINAS/04-VENDEDOR/04-sell-your-car.md)         | `/vender`             | MediaService, VehiclesSaleService                  | `POST /api/vehicles/instant-offer`<br>`GET /api/vehicles/valuation`<br>`POST /api/media/upload`                        |
| [05-media-multimedia.md](04-PAGINAS/04-VENDEDOR/05-media-multimedia.md)   | `/publicar/fotos`     | VehiclesSaleService, MediaService                  | `POST /api/media/upload`<br>`POST /api/media/upload/360`<br>`DELETE /api/media/{id}`<br>`PUT /api/vehicles/{id}/media` |

---

## 05-DEALER - Portal Dealers

| Documento                                                                                 | Ruta Frontend                  | Servicios Backend                                        | Endpoints Principales                                                                                                         |
| ----------------------------------------------------------------------------------------- | ------------------------------ | -------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| [01-dealer-dashboard.md](04-PAGINAS/05-DEALER/01-dealer-dashboard.md)                     | `/dealer/dashboard`            | DealerManagementService                                  | `GET /api/dealers/me`<br>`GET /api/dealers/me/stats`<br>`GET /api/dealers/me/alerts`                                          |
| [02-dealer-inventario.md](04-PAGINAS/05-DEALER/02-dealer-inventario.md)                   | `/dealer/inventario`           | VehiclesSaleService                                      | `GET /api/dealers/me/vehicles`<br>`POST /api/vehicles/bulk`<br>`PUT /api/vehicles/bulk-update`<br>`POST /api/vehicles/import` |
| [03-dealer-crm.md](04-PAGINAS/05-DEALER/03-dealer-crm.md)                                 | `/dealer/crm`                  | ContactService                                           | `GET /api/leads`<br>`PUT /api/leads/{id}/status`<br>`GET /api/leads/pipeline`<br>`POST /api/leads/assign`                     |
| [04-dealer-analytics.md](04-PAGINAS/05-DEALER/04-dealer-analytics.md)                     | `/dealer/analytics`            | VehiclesSaleService, DealerManagementService             | `GET /api/dealers/me/analytics`<br>`GET /api/dealers/me/views`<br>`GET /api/dealers/me/leads-funnel`                          |
| [05-dealer-onboarding.md](04-PAGINAS/05-DEALER/05-dealer-onboarding.md)                   | `/dealer/onboarding`           | UserService, BillingService                              | `POST /api/dealers`<br>`POST /api/dealers/verify`<br>`POST /api/billing/subscriptions`                                        |
| [06-dealer-appointments.md](04-PAGINAS/05-DEALER/06-dealer-appointments.md)               | `/dealer/citas`                | NotificationService, MediaService                        | `GET /api/appointments`<br>`POST /api/appointments`<br>`PUT /api/appointments/{id}/status`                                    |
| [07-badges-display.md](04-PAGINAS/05-DEALER/07-badges-display.md)                         | `/dealer/badges`               | DealerManagementService, NotificationService             | `GET /api/dealers/me/badges`<br>`GET /api/badges/available`<br>`POST /api/badges/claim`                                       |
| [08-boost-promociones.md](04-PAGINAS/05-DEALER/08-boost-promociones.md)                   | `/dealer/boost`                | VehiclesSaleService, BillingService, NotificationService | `GET /api/boost/packages`<br>`POST /api/boost/purchase`<br>`GET /api/vehicles/{id}/boost-status`                              |
| [09-pricing-intelligence.md](04-PAGINAS/05-DEALER/09-pricing-intelligence.md)             | `/dealer/pricing`              | VehiclesSaleService (ML)                                 | `GET /api/pricing/market-analysis`<br>`GET /api/pricing/recommendations`<br>`GET /api/pricing/competitor-prices`              |
| [10-dealer-sales-market.md](04-PAGINAS/05-DEALER/10-dealer-sales-market.md)               | `/dealer/market`               | VehiclesSaleService, DealerManagementService             | `GET /api/market/trends`<br>`GET /api/market/demand`<br>`GET /api/dealers/me/performance`                                     |
| [11-dealer-employees-locations.md](04-PAGINAS/05-DEALER/11-dealer-employees-locations.md) | `/dealer/equipo`               | DealerManagementService                                  | `GET /api/dealers/me/employees`<br>`POST /api/dealers/me/employees`<br>`GET /api/dealers/me/locations`                        |
| [12-dealer-alerts-reports.md](04-PAGINAS/05-DEALER/12-dealer-alerts-reports.md)           | `/dealer/reportes`             | NotificationService, DealerManagementService             | `GET /api/dealers/me/reports`<br>`POST /api/reports/generate`<br>`GET /api/alerts/dealer`                                     |
| [13-inventory-analytics.md](04-PAGINAS/05-DEALER/13-inventory-analytics.md)               | `/dealer/inventario/analytics` | VehiclesSaleService                                      | `GET /api/dealers/me/inventory/analytics`<br>`GET /api/inventory/aging`<br>`GET /api/inventory/turnover`                      |
| [14-test-drives.md](04-PAGINAS/05-DEALER/14-test-drives.md)                               | `/dealer/test-drives`          | BillingService, NotificationService, MediaService        | `GET /api/test-drives`<br>`POST /api/test-drives`<br>`PUT /api/test-drives/{id}/complete`                                     |
| [15-financiamiento-tradein.md](04-PAGINAS/05-DEALER/15-financiamiento-tradein.md)         | `/dealer/financiamiento`       | VehiclesSaleService                                      | `POST /api/financing/calculate`<br>`POST /api/trade-in/evaluate`<br>`GET /api/financing/partners`                             |

---

## 06-ADMIN - Panel Administrativo

| Documento                                                                          | Ruta Frontend               | Servicios Backend                                           | Endpoints Principales                                                                                           |
| ---------------------------------------------------------------------------------- | --------------------------- | ----------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------- |
| [01-admin-dashboard.md](04-PAGINAS/06-ADMIN/01-admin-dashboard.md)                 | `/admin`                    | AdminService, NotificationService, MediaService             | `GET /api/admin/dashboard`<br>`GET /api/admin/stats`<br>`GET /api/admin/alerts`                                 |
| [02-admin-users.md](04-PAGINAS/06-ADMIN/02-admin-users.md)                         | `/admin/usuarios`           | AuthService, UserService, AdminService, NotificationService | `GET /api/admin/users`<br>`PUT /api/admin/users/{id}`<br>`POST /api/admin/users/{id}/roles`                     |
| [03-admin-moderation.md](04-PAGINAS/06-ADMIN/03-admin-moderation.md)               | `/admin/moderacion`         | VehiclesSaleService, AdminService                           | `GET /api/moderation/queue`<br>`POST /api/moderation/{id}/approve`<br>`POST /api/moderation/{id}/reject`        |
| [04-admin-compliance.md](04-PAGINAS/06-ADMIN/04-admin-compliance.md)               | `/admin/compliance`         | AdminService, BillingService                                | `GET /api/compliance/dashboard`<br>`GET /api/compliance/strs`<br>`GET /api/compliance/alerts`                   |
| [05-admin-support.md](04-PAGINAS/06-ADMIN/05-admin-support.md)                     | `/admin/soporte`            | AdminService, NotificationService                           | `GET /api/support/tickets`<br>`PUT /api/support/tickets/{id}`<br>`POST /api/support/tickets/{id}/reply`         |
| [06-admin-system.md](04-PAGINAS/06-ADMIN/06-admin-system.md)                       | `/admin/sistema`            | AdminService, MaintenanceService                            | `GET /api/maintenance/current`<br>`POST /api/maintenance/schedule`<br>`POST /api/maintenance/activate`          |
| [07-notificaciones-admin.md](04-PAGINAS/06-ADMIN/07-notificaciones-admin.md)       | `/admin/notificaciones`     | BillingService, NotificationService                         | `GET /api/notifications/templates`<br>`POST /api/notifications/campaigns`<br>`GET /api/notifications/analytics` |
| [08-admin-review-moderation.md](04-PAGINAS/06-ADMIN/08-admin-review-moderation.md) | `/admin/reviews`            | UserService, NotificationService, MediaService              | `GET /api/reviews/moderation`<br>`POST /api/reviews/{id}/moderate`<br>`GET /api/reviews/flagged`                |
| [09-admin-compliance-alerts.md](04-PAGINAS/06-ADMIN/09-admin-compliance-alerts.md) | `/admin/compliance/alertas` | NotificationService, AdminService                           | `GET /api/compliance/alerts`<br>`PUT /api/compliance/alerts/{id}`<br>`POST /api/compliance/investigate`         |
| [10-admin-operations.md](04-PAGINAS/06-ADMIN/10-admin-operations.md)               | `/admin/operaciones`        | VehiclesSaleService, AuthService, NotificationService       | `GET /api/admin/operations`<br>`GET /api/admin/audit-log`<br>`POST /api/admin/bulk-actions`                     |
| [11-users-roles-management.md](04-PAGINAS/06-ADMIN/11-users-roles-management.md)   | `/admin/roles`              | AuthService, UserService                                    | `GET /api/roles`<br>`POST /api/roles`<br>`PUT /api/roles/{id}/permissions`                                      |
| [12-listings-approvals.md](04-PAGINAS/06-ADMIN/12-listings-approvals.md)           | `/admin/listings`           | VehiclesSaleService, AdminService, MediaService             | `GET /api/admin/listings`<br>`POST /api/admin/listings/{id}/approve`<br>`POST /api/admin/listings/{id}/reject`  |
| [13-reports-kyc-queue.md](04-PAGINAS/06-ADMIN/13-reports-kyc-queue.md)             | `/admin/kyc`                | UserService, AdminService, MediaService                     | `GET /api/kyc/queue`<br>`POST /api/kyc/{id}/approve`<br>`POST /api/kyc/{id}/reject`                             |
| [14-admin-settings.md](04-PAGINAS/06-ADMIN/14-admin-settings.md)                   | `/admin/configuracion`      | AdminService                                                | `GET /api/admin/settings`<br>`PUT /api/admin/settings`<br>`GET /api/admin/categories`                           |
| [15-ml-admin-dashboards.md](04-PAGINAS/06-ADMIN/15-ml-admin-dashboards.md)         | `/admin/ml`                 | AdminService, AnalyticsService                              | `GET /api/ml/models`<br>`GET /api/ml/performance`<br>`POST /api/ml/retrain`                                     |

---

## 07-PAGOS - Billing & Checkout

| Documento                                                                    | Ruta Frontend          | Servicios Backend                                            | Endpoints Principales                                                                                    |
| ---------------------------------------------------------------------------- | ---------------------- | ------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------- |
| [01-pagos-checkout.md](04-PAGINAS/07-PAGOS/01-pagos-checkout.md)             | `/checkout`            | VehiclesSaleService, DealerManagementService, BillingService | `POST /api/billing/checkout`<br>`POST /api/billing/payment-intent`<br>`GET /api/billing/payment-methods` |
| [02-payment-results.md](04-PAGINAS/07-PAGOS/02-payment-results.md)           | `/pago/resultado`      | BillingService                                               | `GET /api/billing/transactions/{id}`<br>`POST /api/billing/transactions/{id}/retry`                      |
| [03-billing-dashboard.md](04-PAGINAS/07-PAGOS/03-billing-dashboard.md)       | `/facturacion`         | BillingService                                               | `GET /api/billing/invoices`<br>`GET /api/billing/subscription`<br>`GET /api/billing/payment-methods`     |
| [04-moneda-extranjera.md](04-PAGINAS/07-PAGOS/04-moneda-extranjera.md)       | `/checkout` (currency) | BillingService                                               | `GET /api/billing/exchange-rates`<br>`POST /api/billing/convert-currency`                                |
| [05-comercio-electronico.md](04-PAGINAS/07-PAGOS/05-comercio-electronico.md) | `/checkout` (legal)    | NotificationService, BillingService                          | `GET /api/billing/legal-requirements`<br>`POST /api/billing/compliance/126-02`                           |

---

## 08-DGII-COMPLIANCE - Cumplimiento Legal

| Documento                                                                                                | Ruta Frontend                 | Servicios Backend                                 | Endpoints Principales                                                                           |
| -------------------------------------------------------------------------------------------------------- | ----------------------------- | ------------------------------------------------- | ----------------------------------------------------------------------------------------------- |
| [01-facturacion-dgii.md](04-PAGINAS/08-DGII-COMPLIANCE/01-facturacion-dgii.md)                           | `/admin/dgii/facturacion`     | BillingService                                    | `GET /api/dgii/ncf-sequences`<br>`POST /api/dgii/generate-ncf`<br>`GET /api/dgii/invoices`      |
| [02-auditoria-compliance-legal.md](04-PAGINAS/08-DGII-COMPLIANCE/02-auditoria-compliance-legal.md)       | `/admin/compliance`           | BillingService, NotificationService, MediaService | `GET /api/compliance/audit`<br>`GET /api/compliance/checklist`<br>`POST /api/compliance/report` |
| [03-obligaciones-fiscales.md](04-PAGINAS/08-DGII-COMPLIANCE/03-obligaciones-fiscales.md)                 | `/admin/dgii/reportes`        | BillingService                                    | `GET /api/dgii/format-606`<br>`GET /api/dgii/format-607`<br>`POST /api/dgii/generate-report`    |
| [04-registro-gastos.md](04-PAGINAS/08-DGII-COMPLIANCE/04-registro-gastos.md)                             | `/admin/gastos`               | MediaService, BillingService                      | `GET /api/expenses`<br>`POST /api/expenses`<br>`GET /api/expenses/categories`                   |
| [05-automatizacion-reportes.md](04-PAGINAS/08-DGII-COMPLIANCE/05-automatizacion-reportes.md)             | `/admin/reportes/automaticos` | BillingService, NotificationService               | `GET /api/reports/scheduled`<br>`POST /api/reports/schedule`<br>`POST /api/reports/generate`    |
| [06-preparacion-auditoria.md](04-PAGINAS/08-DGII-COMPLIANCE/06-preparacion-auditoria.md)                 | `/admin/auditoria`            | MediaService, BillingService                      | `GET /api/audit/preparation`<br>`GET /api/audit/documents`<br>`POST /api/audit/export`          |
| [07-consentimiento-comunicaciones.md](04-PAGINAS/08-DGII-COMPLIANCE/07-consentimiento-comunicaciones.md) | `/settings/comunicaciones`    | AuthService, UserService, NotificationService     | `GET /api/users/consents`<br>`PUT /api/users/consents`<br>`GET /api/users/consent-history`      |
| [08-legal-common-pages.md](04-PAGINAS/08-DGII-COMPLIANCE/08-legal-common-pages.md)                       | `/terminos`, `/privacidad`    | -                                                 | Páginas estáticas (no requieren API)                                                            |

---

## 09-COMPONENTES-COMUNES - Shared Components

| Documento                                                                              | Uso              | Servicios Backend    | Endpoints Principales                                                 |
| -------------------------------------------------------------------------------------- | ---------------- | -------------------- | --------------------------------------------------------------------- |
| [01-common-components.md](04-PAGINAS/09-COMPONENTES-COMUNES/01-common-components.md)   | Global           | -                    | Componentes UI sin API                                                |
| [02-layouts.md](04-PAGINAS/09-COMPONENTES-COMUNES/02-layouts.md)                       | Global           | AuthService          | `GET /api/auth/me` (session check)                                    |
| [03-static-pages.md](04-PAGINAS/09-COMPONENTES-COMUNES/03-static-pages.md)             | SEO              | -                    | Páginas estáticas                                                     |
| [04-vehicle-media.md](04-PAGINAS/09-COMPONENTES-COMUNES/04-vehicle-media.md)           | Detalle vehículo | VehiclesSaleService  | `GET /api/vehicles/{id}/media`<br>`GET /api/vehicles/{id}/360-images` |
| [05-video-tour.md](04-PAGINAS/09-COMPONENTES-COMUNES/05-video-tour.md)                 | Detalle vehículo | MediaService         | `GET /api/media/video/{id}`<br>`POST /api/media/video/upload`         |
| [06-event-tracking-sdk.md](04-PAGINAS/09-COMPONENTES-COMUNES/06-event-tracking-sdk.md) | Global           | EventTrackingService | `POST /api/events/track`<br>`POST /api/events/batch`                  |

---

## 📊 RESUMEN DE DEPENDENCIAS

### Páginas por Servicio Backend

| Servicio                    | # Páginas que lo usan |
| --------------------------- | --------------------- |
| **VehiclesSaleService**     | 35                    |
| **NotificationService**     | 28                    |
| **BillingService**          | 20                    |
| **MediaService**            | 18                    |
| **AuthService**             | 15                    |
| **UserService**             | 14                    |
| **DealerManagementService** | 12                    |
| **AdminService**            | 12                    |
| **ContactService**          | 6                     |

### Páginas Sin Dependencias Backend

Las siguientes páginas son **estáticas** o solo usan datos ya cargados:

1. `08-legal-common-pages.md` - Términos, Privacidad, About
2. `01-common-components.md` - Componentes UI
3. `02-layouts.md` - Layouts (solo session check)
4. `03-static-pages.md` - Páginas SEO

---

## 🔗 REFERENCIAS CRUZADAS

### Documentación API Detallada

Cada servicio tiene su documentación completa en `05-API-INTEGRATION/`:

- [01-cliente-http.md](05-API-INTEGRATION/01-cliente-http.md) - Cliente HTTP base
- [02-autenticacion.md](05-API-INTEGRATION/02-autenticacion.md) - JWT, refresh, interceptors
- [03-formularios.md](05-API-INTEGRATION/03-formularios.md) - React Hook Form + Zod
- [04-subida-imagenes.md](05-API-INTEGRATION/04-subida-imagenes.md) - Upload a S3

### Orden de Implementación

Ver [00-ORDEN-IMPLEMENTACION.md](00-ORDEN-IMPLEMENTACION.md) para la secuencia recomendada de desarrollo.

---

**Última actualización:** Enero 30, 2026
