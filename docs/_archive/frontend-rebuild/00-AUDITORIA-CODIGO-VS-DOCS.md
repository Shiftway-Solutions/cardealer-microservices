# 🔍 AUDITORÍA: Código Frontend vs Documentación

**Fecha:** Enero 30, 2026  
**Auditor:** GitHub Copilot

---

## 📊 RESUMEN EJECUTIVO

| Métrica     | Código                      | Docs  | Gap                           |
| ----------- | --------------------------- | ----- | ----------------------------- |
| **Páginas** | 128 TSX                     | 56 MD | +72 páginas en código sin doc |
| **Estado**  | ⚠️ Documentación incompleta |

---

## ❓ RESPUESTA: ¿VehicleDetailPage tiene filtros?

**NO.** La página `VehicleDetailPage.tsx` es una vista de **detalle de un solo vehículo**.

Los filtros están en:

- ✅ `BrowsePage.tsx` - Usa `AdvancedFilters` component
- ✅ `SearchPage.tsx` - Tiene filtros inline
- ✅ Componentes de filtro disponibles:
  - `components/organisms/AdvancedFilters.tsx`
  - `components/organisms/FilterSidebar.tsx`
  - `components/okla/search/OklaFilterSidebar.tsx`
  - `components/okla/search/OklaActiveFilters.tsx`
  - `components/marketplace/SearchFilters.tsx`

**Comportamiento correcto:** Los filtros están en páginas de **listado/búsqueda**, no en detalle de vehículo.

---

## 🔴 PÁGINAS EN CÓDIGO SIN DOCUMENTACIÓN (72)

### 📁 Root pages/ (sin subcarpeta)

| Página                         | Documentación Requerida            |
| ------------------------------ | ---------------------------------- |
| `AdvancedDealerDashboard.tsx`  | ❌ Falta doc                       |
| `AzulApprovedPage.tsx`         | ❌ Falta doc (resultado pago AZUL) |
| `AzulCancelledPage.tsx`        | ❌ Falta doc                       |
| `AzulDeclinedPage.tsx`         | ❌ Falta doc                       |
| `AzulPaymentPage.tsx`          | ❌ Falta doc                       |
| `DealerAnalyticsDashboard.tsx` | ⚠️ Parcial (28-dealer-analytics)   |
| `DealerLandingPage.tsx`        | ❌ Falta doc                       |
| `DealerPricingPage.tsx`        | ❌ Falta doc                       |
| `DealerProfileEditorPage.tsx`  | ❌ Falta doc                       |
| `DealerRegistrationPage.tsx`   | ⚠️ Parcial (29-onboarding)         |
| `FeatureStoreDashboard.tsx`    | ❌ Falta doc                       |
| `LeadDetail.tsx`               | ⚠️ Parcial (35-crm-leads)          |
| `LeadsDashboard.tsx`           | ⚠️ Parcial (35-crm-leads)          |
| `MyInquiriesPage.tsx`          | ❌ Falta doc                       |
| `PricingIntelligencePage.tsx`  | ⚠️ Parcial (42-pricing)            |
| `PublicDealerProfilePage.tsx`  | ❌ Falta doc                       |
| `ReceivedInquiriesPage.tsx`    | ❌ Falta doc                       |
| `SellerReviewsPage.tsx`        | ⚠️ Parcial (20-reviews)            |
| `UserBehaviorDashboard.tsx`    | ⚠️ Parcial (39-event-tracking)     |
| `WriteReviewPage.tsx`          | ⚠️ Parcial (36-review-request)     |

### 📁 pages/admin/ (17 páginas, 6 documentadas)

| Página Código                   | Documentación                         |
| ------------------------------- | ------------------------------------- |
| `AdminDashboardPage.tsx`        | ✅ 12-admin-dashboard.md              |
| `AdminHomepagePage.tsx`         | ❌ Falta doc                          |
| `AdminListingsPage.tsx`         | ⚠️ Parcial (14-moderation)            |
| `AdminReportsPage.tsx`          | ❌ Falta doc                          |
| `AdminSettingsPage.tsx`         | ⚠️ Parcial (17-admin-system)          |
| `CategoriesManagementPage.tsx`  | ❌ Falta doc                          |
| `KYCAdminQueuePage.tsx`         | ⚠️ Parcial (27-kyc)                   |
| `KYCAdminReviewPage.tsx`        | ⚠️ Parcial (27-kyc)                   |
| `MLAdminDashboard.tsx`          | ❌ Falta doc                          |
| `MLDashboardPage.tsx`           | ❌ Falta doc (ML/AI)                  |
| `PendingApprovalsPage.tsx`      | ❌ Falta doc                          |
| `PermissionsManagementPage.tsx` | ✅ 29-admin-rbac.md (05-ADMIN)        |
| `RoleDetailPage.tsx`            | ✅ 29-admin-rbac.md (05-ADMIN)        |
| `RolesManagementPage.tsx`       | ✅ 29-admin-rbac.md (05-ADMIN)        |
| `STRReportsPage.tsx`            | ❌ Falta doc (Suspicious Transaction) |
| `UsersManagementPage.tsx`       | ✅ 13-admin-users.md                  |
| `WatchlistAdminPage.tsx`        | ❌ Falta doc                          |

### 📁 pages/auth/ (9 páginas, 1 doc)

| Página Código                      | Documentación             |
| ---------------------------------- | ------------------------- |
| `LoginPage.tsx`                    | ✅ 07-auth.md             |
| `RegisterPage.tsx`                 | ✅ 07-auth.md             |
| `ForgotPasswordPage.tsx`           | ⚠️ Parcial                |
| `ResetPasswordPage.tsx`            | ⚠️ Parcial                |
| `SetPasswordPage.tsx`              | ❌ Falta doc              |
| `EmailVerificationPendingPage.tsx` | ❌ Falta doc              |
| `VerifyEmailPage.tsx`              | ❌ Falta doc              |
| `OAuthCallbackPage.tsx`            | ✅ 28-oauth-management.md |
| `TwoFactorVerifyPage.tsx`          | ❌ Falta doc              |

### 📁 pages/billing/ (6 páginas, 1 doc)

| Página Código              | Documentación               |
| -------------------------- | --------------------------- |
| `CheckoutPage.tsx`         | ✅ 19-pagos-checkout.md     |
| `BillingDashboardPage.tsx` | ❌ Falta doc                |
| `InvoicesPage.tsx`         | ⚠️ Parcial (33-facturacion) |
| `PaymentMethodsPage.tsx`   | ❌ Falta doc                |
| `PaymentsPage.tsx`         | ❌ Falta doc                |
| `PlansPage.tsx`            | ❌ Falta doc                |

### 📁 pages/common/ (9 páginas, 2 docs)

| Página Código        | Documentación           |
| -------------------- | ----------------------- |
| `AboutPage.tsx`      | ❌ Falta doc            |
| `ContactPage.tsx`    | ❌ Falta doc            |
| `CookiesPage.tsx`    | ⚠️ Parcial (26-privacy) |
| `FAQPage.tsx`        | ❌ Falta doc            |
| `HelpCenterPage.tsx` | ✅ 11-help-center.md    |
| `HowItWorksPage.tsx` | ❌ Falta doc            |
| `PricingPage.tsx`    | ❌ Falta doc (planes)   |
| `PrivacyPage.tsx`    | ✅ 26-privacy-gdpr.md   |
| `TermsPage.tsx`      | ❌ Falta doc            |

### 📁 pages/dealer/ (38 páginas, ~8 docs)

| Página Código                       | Documentación              |
| ----------------------------------- | -------------------------- |
| `DealerDashboardPage.tsx`           | ✅ 06-dealer-dashboard.md  |
| `DealerInventoryPage.tsx`           | ✅ 09-dealer-inventario.md |
| `CRMPage.tsx`                       | ✅ 10-dealer-crm.md        |
| `DealerAnalyticsPage.tsx`           | ✅ 28-dealer-analytics.md  |
| `DealerOnboardingPage.tsx`          | ✅ 29-dealer-onboarding.md |
| `AdvancedAnalyticsDashboard.tsx`    | ❌ Falta doc               |
| `AlertsManagementPage.tsx`          | ❌ Falta doc               |
| `AnalyticsPage.tsx`                 | ⚠️ Duplicado?              |
| `CSVImportPage.tsx`                 | ❌ Falta doc               |
| `CreateListingTestPage.tsx`         | ❌ Test page               |
| `CreateSellerPage.tsx`              | ❌ Falta doc               |
| `DealerAddVehiclePage.tsx`          | ❌ Falta doc               |
| `DealerAlertsPage.tsx`              | ❌ Falta doc               |
| `DealerBenchmarksPage.tsx`          | ❌ Falta doc               |
| `DealerDocumentsPage.tsx`           | ❌ Falta doc               |
| `DealerEmailVerificationPage.tsx`   | ❌ Falta doc               |
| `DealerEmployeePermissionsPage.tsx` | ❌ Falta doc               |
| `DealerEmployeesPage.tsx`           | ❌ Falta doc               |
| `DealerHomePage.tsx`                | ❌ Falta doc               |
| `DealerInquiriesPage.tsx`           | ❌ Falta doc               |
| `DealerListingsPage.tsx`            | ⚠️ Similar a inventario    |
| `DealerOnboardingStatusPage.tsx`    | ❌ Falta doc               |
| `DealerPaymentSetupPage.tsx`        | ❌ Falta doc               |
| `DealerProfilePage.tsx`             | ❌ Falta doc               |
| `DealerSalesPage.tsx`               | ❌ Falta doc               |
| `DealerSettingsPage.tsx`            | ❌ Falta doc               |
| `DealerVehicleEditPage.tsx`         | ❌ Falta doc               |
| `InventoryAnalyticsPage.tsx`        | ❌ Falta doc               |
| `LeadFunnelPage.tsx`                | ❌ Falta doc               |
| `LocationsPage.tsx`                 | ❌ Falta doc               |
| `MarketAnalysisPage.tsx`            | ❌ Falta doc               |
| `ReportsPage.tsx`                   | ❌ Falta doc               |
| `SellerProfilePage.tsx`             | ✅ 30-seller-profiles.md   |

### 📁 pages/kyc/ (3 páginas, 1 doc)

| Página Código                   | Documentación             |
| ------------------------------- | ------------------------- |
| `KYCVerificationPage.tsx`       | ✅ 27-kyc-verificacion.md |
| `KYCStatusPage.tsx`             | ⚠️ Parcial                |
| `BiometricVerificationPage.tsx` | ❌ Falta doc              |

### 📁 pages/seller/ (3 páginas, 1 doc)

| Página Código                   | Documentación            |
| ------------------------------- | ------------------------ |
| `SellerDashboardPage.tsx`       | ✅ 05-dashboard.md       |
| `SellerProfileSettingsPage.tsx` | ❌ Falta doc             |
| `SellerPublicProfilePage.tsx`   | ✅ 30-seller-profiles.md |

### 📁 pages/user/ (9 páginas, 2 docs)

| Página Código              | Documentación             |
| -------------------------- | ------------------------- |
| `ProfilePage.tsx`          | ✅ 08-perfil.md           |
| `PrivacyCenterPage.tsx`    | ✅ 26-privacy-gdpr.md     |
| `UserDashboardPage.tsx`    | ✅ 05-dashboard.md        |
| `DataDownloadPage.tsx`     | ⚠️ Parcial (26-privacy)   |
| `DeleteAccountPage.tsx`    | ⚠️ Parcial (26-privacy)   |
| `MessagesPage.tsx`         | ❌ Falta doc              |
| `MyDataPage.tsx`           | ⚠️ Parcial                |
| `SecuritySettingsPage.tsx` | ❌ Falta doc              |
| `WishlistPage.tsx`         | ❌ Falta doc (favoritos?) |

### 📁 pages/vehicles/ (9 páginas, 6 docs)

| Página Código            | Documentación                  |
| ------------------------ | ------------------------------ |
| `VehicleDetailPage.tsx`  | ✅ 03-detalle-vehiculo.md      |
| `BrowsePage.tsx`         | ✅ 02-busqueda.md + 31-filtros |
| `Media360ViewerPage.tsx` | ✅ 18-vehicle-360-page.md      |
| `VehiclesHomePage.tsx`   | ✅ 01-home.md                  |
| `SellYourCarPage.tsx`    | ✅ 04-publicar.md              |
| `ComparePage.tsx`        | ✅ 23-comparador.md            |
| `MapViewPage.tsx`        | ❌ Falta doc                   |
| `RecentlyViewedPage.tsx` | ❌ Falta doc                   |
| `VideoTourPage.tsx`      | ❌ Falta doc                   |

---

## 📋 DOCUMENTACIÓN REQUERIDA ADICIONAL

### 🔴 Alta Prioridad (Funcionalidad Core)

| #   | Nueva Doc                      | Páginas que cubre                                                                   |
| --- | ------------------------------ | ----------------------------------------------------------------------------------- |
| 49  | `49-payment-results.md`        | AzulApprovedPage, AzulDeclinedPage, AzulCancelledPage, AzulPaymentPage              |
| 50  | `50-dealer-landing-pricing.md` | DealerLandingPage, DealerPricingPage                                                |
| 51  | `51-dealer-profile-editor.md`  | DealerProfileEditorPage, PublicDealerProfilePage                                    |
| 52  | `52-inquiries-messages.md`     | MyInquiriesPage, ReceivedInquiriesPage, MessagesPage                                |
| 53  | `53-billing-dashboard.md`      | BillingDashboardPage, PaymentMethodsPage, PaymentsPage, PlansPage, InvoicesPage     |
| 54  | `54-auth-verification.md`      | EmailVerificationPendingPage, VerifyEmailPage, SetPasswordPage, TwoFactorVerifyPage |

### 🟡 Media Prioridad (Dealer Features)

| #   | Nueva Doc                    | Páginas que cubre                                               |
| --- | ---------------------------- | --------------------------------------------------------------- |
| 55  | `55-dealer-employees.md`     | DealerEmployeesPage, DealerEmployeePermissionsPage              |
| 56  | `56-dealer-documents.md`     | DealerDocumentsPage                                             |
| 57  | `57-dealer-locations.md`     | LocationsPage                                                   |
| 58  | `58-dealer-vehicles-crud.md` | DealerAddVehiclePage, DealerVehicleEditPage, DealerListingsPage |
| 59  | `59-dealer-sales-reports.md` | DealerSalesPage, ReportsPage, InventoryAnalyticsPage            |
| 60  | `60-csv-import.md`           | CSVImportPage                                                   |
| 61  | `61-market-benchmarks.md`    | MarketAnalysisPage, DealerBenchmarksPage                        |
| 62  | `62-lead-funnel.md`          | LeadFunnelPage, LeadDetail, LeadsDashboard                      |

### 🟢 Baja Prioridad (Admin/ML)

| #   | Nueva Doc                      | Páginas que cubre                                                               |
| --- | ------------------------------ | ------------------------------------------------------------------------------- |
| 63  | `63-admin-ml-ai.md`            | MLAdminDashboard, MLDashboardPage, FeatureStoreDashboard, UserBehaviorDashboard |
| 64  | `64-admin-reports-listings.md` | AdminReportsPage, AdminListingsPage, AdminHomepagePage                          |
| 65  | `65-admin-aml-watchlist.md`    | STRReportsPage, WatchlistAdminPage, PendingApprovalsPage                        |
| 66  | `66-admin-categories.md`       | CategoriesManagementPage                                                        |
| 67  | `67-security-settings.md`      | SecuritySettingsPage                                                            |

### ⚪ Páginas Informativas/Legales

| #   | Nueva Doc                  | Páginas que cubre                                                       |
| --- | -------------------------- | ----------------------------------------------------------------------- |
| 68  | `68-static-pages.md`       | AboutPage, ContactPage, FAQPage, HowItWorksPage, TermsPage, CookiesPage |
| 69  | `69-vehicles-extras.md`    | MapViewPage, RecentlyViewedPage, VideoTourPage                          |
| 70  | `70-wishlist-favorites.md` | WishlistPage (si diferente a FavoritesPage)                             |

---

## ✅ PÁGINAS BIEN DOCUMENTADAS (Código ↔ Doc)

| Área             | Código                                             | Doc                             |
| ---------------- | -------------------------------------------------- | ------------------------------- |
| Home             | `HomePage.tsx`, `VehiclesHomePage.tsx`             | ✅ 01-home.md                   |
| Búsqueda         | `BrowsePage.tsx`, `SearchPage.tsx`                 | ✅ 02-busqueda.md, 32-search.md |
| Detalle          | `VehicleDetailPage.tsx`                            | ✅ 03-detalle-vehiculo.md       |
| Publicar         | `SellYourCarPage.tsx`                              | ✅ 04-publicar.md               |
| Dashboard        | `UserDashboardPage.tsx`, `SellerDashboardPage.tsx` | ✅ 05-dashboard.md              |
| Dealer Dashboard | `DealerDashboardPage.tsx`                          | ✅ 06-dealer-dashboard.md       |
| Auth             | `LoginPage.tsx`, `RegisterPage.tsx`                | ✅ 07-auth.md                   |
| Perfil           | `ProfilePage.tsx`                                  | ✅ 08-perfil.md                 |
| Inventario       | `DealerInventoryPage.tsx`                          | ✅ 09-dealer-inventario.md      |
| CRM              | `CRMPage.tsx`                                      | ✅ 10-dealer-crm.md             |
| Help             | `HelpCenterPage.tsx`                               | ✅ 11-help-center.md            |
| Admin Users      | `UsersManagementPage.tsx`                          | ✅ 13-admin-users.md            |
| 360°             | `Media360ViewerPage.tsx`                           | ✅ 18-vehicle-360-page.md       |
| Checkout         | `CheckoutPage.tsx`                                 | ✅ 19-pagos-checkout.md         |
| Reviews          | `WriteReviewPage.tsx`, `SellerReviewsPage.tsx`     | ✅ 20-reviews.md                |
| Chatbot          | (widget en MainLayout)                             | ✅ 22-chatbot.md                |
| Comparador       | `ComparePage.tsx`, `ComparisonPage.tsx`            | ✅ 23-comparador.md             |
| Alertas          | `AlertsPage.tsx`                                   | ✅ 24-alertas-busquedas.md      |
| Notificaciones   | (component)                                        | ✅ 25-notificaciones.md         |
| Privacy          | `PrivacyPage.tsx`, `PrivacyCenterPage.tsx`         | ✅ 26-privacy-gdpr.md           |
| KYC              | `KYCVerificationPage.tsx`                          | ✅ 27-kyc-verificacion.md       |
| OAuth            | `OAuthCallbackPage.tsx`                            | ✅ 28-oauth-management.md       |
| Analytics        | `DealerAnalyticsPage.tsx`                          | ✅ 28-dealer-analytics.md       |
| Onboarding       | `DealerOnboardingPage.tsx`                         | ✅ 29-dealer-onboarding.md      |
| Sellers          | `SellerPublicProfilePage.tsx`                      | ✅ 30-seller-profiles.md        |
| Filtros          | `AdvancedFilters.tsx` (component)                  | ✅ 31-filtros-avanzados.md      |
| Favoritos        | `FavoritesPage.tsx`                                | ⚠️ Parcial en 24-alertas        |
| Pricing IA       | `PricingIntelligencePage.tsx`                      | ✅ 42-pricing-intelligence.md   |
| RBAC             | `RolesManagementPage.tsx`, etc                     | ✅ 29-admin-rbac.md             |

---

## 📈 ESTADÍSTICAS FINALES

| Categoría          | Total   | Con Doc       | Sin Doc       |
| ------------------ | ------- | ------------- | ------------- |
| **Root pages**     | 27      | 8             | 19            |
| **pages/admin**    | 17      | 5             | 12            |
| **pages/auth**     | 9       | 3             | 6             |
| **pages/billing**  | 6       | 1             | 5             |
| **pages/common**   | 9       | 2             | 7             |
| **pages/dealer**   | 38      | 8             | 30            |
| **pages/kyc**      | 3       | 1             | 2             |
| **pages/seller**   | 3       | 2             | 1             |
| **pages/user**     | 9       | 3             | 6             |
| **pages/vehicles** | 9       | 6             | 3             |
| **TOTAL**          | **128** | **39 (~30%)** | **89 (~70%)** |

---

## 🎯 RECOMENDACIÓN

Se necesitan **~22 documentos adicionales** para cubrir las 89 páginas sin documentación:

1. **Alta prioridad (6 docs)**: Payment results, dealer landing, inquiries, billing, auth verification
2. **Media prioridad (8 docs)**: Dealer features (employees, documents, vehicles, reports)
3. **Baja prioridad (4 docs)**: Admin/ML dashboards
4. **Informativas (4 docs)**: Static pages, vehicles extras

**Total estimado: 70 docs** (actualmente 56) para cobertura 100%.

---

_Generado automáticamente - Enero 30, 2026_
