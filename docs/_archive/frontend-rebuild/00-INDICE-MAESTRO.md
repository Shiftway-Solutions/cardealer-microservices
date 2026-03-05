# 📚 ÍNDICE MAESTRO - Reconstrucción Frontend OKLA

> **Propósito:** Documentación ejecutable para que un modelo de IA pueda implementar sin errores
> **Fecha:** Enero 31, 2026 (Auditoría 14 - Navbar Optimizado)
> **Estado:** ✅ DOCUMENTACIÓN COMPLETA + 🔍 AUDITORÍAS 1-14 COMPLETADAS

---

## ✅ AUDITORÍA DE DOCUMENTACIÓN COMPLETADA

**Estado:** 14 de 15 auditorías completadas.

### 📊 Resumen de Auditorías

- **Auditorías 1-9:** Reorganización de 04-PAGINAS en 9 subcarpetas + secciones estándar
- **Auditoría 10:** Tests E2E (Playwright) agregados a 80 documentos
- **Auditoría 11:** Referencias internas corregidas entre documentos
- **Auditoría 12:** Índice maestro actualizado con nueva estructura
- **Auditoría 13:** ✅ Sincronización índice con archivos reales + eliminación de duplicados
- **Auditoría 14:** ✅ Navbar optimizado para RD (Comprar/Vender/Dealers + Auth button)

**Pendientes:** Auditoría 15 (backend docs)

---

## 🎯 OBJETIVO PRINCIPAL

Crear un frontend **profesional con tema visual OKLA** con:

1. **Tema OKLA** - Verde esmeralda (#00A870), UI limpia, sombras sutiles
2. **Sistema Deal Rating** - Badges de calificación de precio (Great, Good, Fair, High)
3. **UX excepcional** - Flujos intuitivos, feedback inmediato, accesibilidad
4. **Performance óptimo** - < 1.5s FCP, < 500 KB bundle
5. **Testing robusto** - > 80% coverage, E2E críticos
6. **Código mantenible** - TypeScript estricto, patrones consistentes

> 📖 **Guía de tema:** Ver [02-UX-DESIGN-SYSTEM/00-TEMA-CARGURUS-AUDITORIA.md](./02-UX-DESIGN-SYSTEM/00-TEMA-CARGURUS-AUDITORIA.md)

---

## 🧭 NAVBAR - ESTRUCTURA ACTUALIZADA (Enero 31, 2026)

### Diseño Final

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ [O] OKLA  │ 🚗 Comprar │ + Vender │ 🏢 Dealers │  [Iniciar Sesión / Registrarse]  │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Decisiones de Diseño

| Elemento                     | Implementación                 | Razón                            |
| ---------------------------- | ------------------------------ | -------------------------------- |
| **3 links principales**      | Comprar, Vender, Dealers       | Optimizado para conversión en RD |
| **Sin búsqueda en navbar**   | Búsqueda en Hero               | Evita redundancia, UX limpio     |
| **Botón auth único**         | "Iniciar Sesión / Registrarse" | Menos fricción, un solo click    |
| **Color verde**              | `#00A870`                      | Tema OKLA consistente            |
| **Sin corazón de favoritos** | Eliminado                      | Disponible en menú de usuario    |

### Ubicación del Código

- **Archivo:** `frontend/web-next/src/components/layout/navbar.tsx`
- **Documentación:** `docs/frontend-rebuild/03-COMPONENTES/01-layout.md`

---

## 📁 ESTRUCTURA DE DOCUMENTOS (ACTUALIZADA - Enero 31, 2026)

```
docs/frontend-rebuild/
│
├── 00-INDICE-MAESTRO.md              # ✅ Este archivo (ACTUALIZADO)
├── 00-PLAN-AUDITORIA-CORRECCION.md  # 📋 Plan de auditoría y corrección
├── 00-RESUMEN-AUDITORIA.md          # 📊 Resumen de auditoría
├── AUDITORIA-GATEWAY-ENDPOINTS.md    # 🔍 Auditoría completa de endpoints (187)
├── AUDITORIA-RESUMEN-VISUAL.md       # 📈 Dashboard visual de cobertura
│
├── 01-SETUP/                         # ⚙️ Configuración Inicial (17 archivos)
│   ├── 01-crear-proyecto.md         # Crear proyecto Next.js
│   ├── 02-configurar-typescript.md  # tsconfig.json completo
│   ├── 03-configurar-eslint.md      # ESLint strict mode
│   ├── 04-instalar-shadcn.md        # Componentes base shadcn/ui
│   ├── 05-configurar-playwright.md  # Testing E2E con Playwright
│   ├── 06-internationalization.md   # 🆕 i18n con next-intl (es-DO/en-US)
│   ├── 07-performance-optimization.md # 🆕 Core Web Vitals, bundle optimization
│   ├── 08-seo-configuration.md      # 🆕 SEO, JSON-LD, sitemap, robots.txt
│   ├── 09-environment-variables.md  # 🆕 Variables de entorno por ambiente
│   ├── 10-deploy-production.md      # 🆕 Deploy a producción (Docker, K8s, CI/CD)
│   ├── 11-architecture-diagrams.md  # 🆕 Diagramas de arquitectura del sistema
│   ├── 12-migracion-vite-nextjs.md  # 🆕 Guía migración Vite → Next.js
│   ├── 13-routing-map.md            # 🆕✅ Mapa completo de rutas (~96 rutas)
│   ├── 14-architecture-decisions.md # 🆕✅ ADR: Decisiones arquitectónicas (10 ADRs)
│   ├── 15-storybook-config.md       # 🆕✅ Configuración Storybook + shadcn/ui
│   ├── 16-deployment-checklist.md   # 🆕✅ Checklist pre-producción completo
│   └── 17-changelog-template.md     # 🆕✅ Template CHANGELOG + Conventional Commits
│
├── 02-UX-DESIGN-SYSTEM/             # 🎨 Sistema de Diseño (9 archivos) - TEMA CARGURUS
│   ├── 00-TEMA-CARGURUS-AUDITORIA.md # 🆕⭐ Auditoría tema CarGurus USA (verde #00A870)
│   ├── 01-principios-ux.md          # 10 principios de UX OKLA
│   ├── 02-design-tokens.md          # Colores verde, tipografía, espaciado
│   ├── 03-componentes-base.md       # Button (verde), Card, Input, DealRatingBadge
│   ├── 04-patrones-ux.md            # Loading, errors, empty states
│   ├── 05-animaciones.md            # Framer Motion guidelines
│   ├── 06-accesibilidad.md          # WCAG 2.1 AA checklist
│   ├── 07-error-handling.md         # 🆕 Error Boundaries, Sentry, fallbacks
│   └── 08-api-error-codes.md        # 🆕 Códigos de error por servicio
│
├── 03-COMPONENTES/                  # 🧩 Componentes Reutilizables (9 archivos)
│   ├── 00-metodologia-componentes.md # 🆕✅ Metodología: estilos centralizados, páginas limpias
│   ├── 00-global-types.md           # 🆕✅ Tipos TypeScript centralizados (Vehicle, User, Dealer, etc.)
│   ├── 01-layout.md                 # Navbar, Footer, MainLayout
│   ├── 02-formularios.md            # Form components con validación
│   ├── 03-vehiculos.md              # VehicleCard, Gallery, Filters
│   ├── 04-dealers.md                # DealerCard, DealerProfile
│   ├── 05-usuarios.md               # UserProfile, UserMenu
│   ├── 06-vehicle-360-viewer.md     # Visor 360° de vehículos
│   └── 07-homepage-components.md    # 🆕✅ HeroCarousel, FeaturedSection, FeaturedListingGrid
│
├── 04-PAGINAS/                      # 📄 Páginas Completas (103 archivos, 9 subcarpetas)
│   │
│   ├── 00-reglas-paginas.md         # 🆕✅ Reglas: páginas solo componen componentes
│   │
│   ├── 01-PUBLICO/                  # 🌐 Páginas Públicas (10 docs)
│   │   ├── 01-home.md               # Homepage con secciones dinámicas
│   │   ├── 02-busqueda.md           # Búsqueda avanzada de vehículos
│   │   ├── 03-detalle-vehiculo.md   # Página de detalle del vehículo
│   │   ├── 04-help-center.md        # Centro de ayuda y FAQ
│   │   ├── 05-vehicle-360-page.md   # Visor 360° de vehículos
│   │   ├── 06-comparador.md         # Comparador hasta 3 vehículos
│   │   ├── 07-filtros-avanzados.md  # Filtros avanzados de búsqueda
│   │   ├── 08-search-completo.md    # Búsqueda completa con todos los filtros
│   │   ├── 09-vehicle-browse.md     # Navegación de vehículos por categoría
│   │   └── 10-static-pages.md       # About, Terms, Privacy, Contact
│   │
│   ├── 02-AUTH/                     # 🔐 Autenticación (7 docs)
│   │   ├── 01-auth-login-register.md   # Login y registro combinado
│   │   ├── 02-verification-flows.md    # Flujos de verificación (email, phone)
│   │   ├── 03-oauth-management.md      # Gestión de cuentas OAuth
│   │   ├── 04-kyc-verificacion.md      # Verificación de identidad (KYC)
│   │   ├── 05-privacy-gdpr.md          # Privacidad y GDPR
│   │   ├── 06-user-security-privacy.md # Seguridad y privacidad de usuario
│   │   └── 07-auth-flow-diagrams.md    # 🆕✅ Diagramas de flujo de auth (7 flujos ASCII)
│   │
│   ├── 03-COMPRADOR/                # 🛒 Flujo del Comprador (14 docs)
│   │   ├── 01-perfil.md                      # Perfil del comprador
│   │   ├── 02-alertas-busquedas.md           # Alertas y búsquedas guardadas
│   │   ├── 03-notificaciones.md              # Centro de notificaciones
│   │   ├── 04-recomendaciones.md             # Recomendaciones personalizadas
│   │   ├── 05-inquiries-messaging.md         # Consultas y mensajería
│   │   ├── 06-reviews-reputacion.md          # Reviews y reputación
│   │   ├── 07-chatbot.md                     # Chatbot de ayuda
│   │   ├── 08-favorites-compare.md           # Favoritos y comparador
│   │   ├── 09-user-dashboard.md              # Dashboard del usuario
│   │   ├── 10-user-messages.md               # Mensajes del usuario
│   │   ├── 12-inquiries-messages.md          # Sistema de consultas
│   │   ├── 13-security-settings.md           # Configuración de seguridad
│   │   ├── 14-vehicle-extras.md              # Extras de vehículos
│   │   └── 15-wishlist-favorites.md          # Lista de deseos y favoritos
│   │
│   ├── 04-VENDEDOR/                 # 🚗 Flujo del Vendedor Individual (5 docs)
│   │   ├── 01-publicar-vehiculo.md   # Formulario de publicación completo
│   │   ├── 02-seller-dashboard.md    # Dashboard del vendedor
│   │   ├── 03-seller-profiles.md     # Perfiles de vendedor
│   │   ├── 04-sell-your-car.md       # Vende tu carro (landing)
│   │   └── 05-media-multimedia.md    # Subida de fotos/videos/360°
│   │
│   ├── 05-DEALER/                   # 🏪 Portal del Dealer (25 docs)
│   │   ├── 01-dealer-dashboard.md            # Dashboard principal
│   │   ├── 02-dealer-inventario.md           # Gestión de inventario
│   │   ├── 03-dealer-crm.md                  # CRM de leads y contactos
│   │   ├── 04-dealer-analytics.md            # Analytics y reportes
│   │   ├── 05-dealer-onboarding.md           # Proceso de registro dealer
│   │   ├── 06-dealer-appointments.md         # Gestión de citas
│   │   ├── 07-badges-display.md              # Sistema de badges
│   │   ├── 08-boost-promociones.md           # Promoción de publicaciones
│   │   ├── 09-pricing-intelligence.md        # IA de pricing
│   │   ├── 10-dealer-sales-market.md         # Ventas y mercado
│   │   ├── 11-dealer-employees-locations.md  # Empleados y ubicaciones
│   │   ├── 12-dealer-employees.md            # Gestión de empleados
│   │   ├── 13-dealer-alerts-reports.md       # Alertas y reportes
│   │   ├── 14-dealer-documents.md            # Documentos del dealer
│   │   ├── 15-dealer-locations.md            # Ubicaciones/sucursales
│   │   ├── 16-inventory-analytics.md         # Analytics de inventario
│   │   ├── 17-dealer-vehicles-crud.md        # CRUD de vehículos
│   │   ├── 18-test-drives.md                 # Test drives
│   │   ├── 19-dealer-sales-reports.md        # Reportes de ventas
│   │   ├── 20-financiamiento-tradein.md      # Financiamiento y trade-in
│   │   ├── 21-dealer-landing-pricing.md      # Landing y precios
│   │   ├── 22-dealer-profile-editor.md       # Editor de perfil
│   │   ├── 23-csv-import.md                  # Importación CSV
│   │   ├── 24-market-benchmarks.md           # Benchmarks de mercado
│   │   └── 25-lead-funnel.md                 # Funnel de leads
│   │
│   ├── 06-ADMIN/                    # 👑 Panel Administrativo (20 docs)
│   │   ├── 01-admin-dashboard.md             # Dashboard administrativo
│   │   ├── 02-admin-users.md                 # Gestión de usuarios
│   │   ├── 03-admin-moderation.md            # Moderación de contenido
│   │   ├── 04-admin-compliance.md            # Compliance y normativas
│   │   ├── 05-admin-support.md               # Tickets de soporte
│   │   ├── 06-admin-system.md                # Configuración del sistema
│   │   ├── 07-notificaciones-admin.md        # Gestión de notificaciones
│   │   ├── 08-admin-review-moderation.md     # Moderación de reviews
│   │   ├── 09-admin-compliance-alerts.md     # Alertas de compliance
│   │   ├── 10-admin-ml-ai.md                 # Machine Learning e IA
│   │   ├── 11-admin-operations.md            # Operaciones y mantenimiento
│   │   ├── 12-admin-reports-listings.md      # Reportes de listings
│   │   ├── 13-users-roles-management.md      # Gestión de roles
│   │   ├── 14-admin-aml-watchlist.md         # AML y watchlist
│   │   ├── 15-listings-approvals.md          # Aprobación de listings
│   │   ├── 16-admin-categories.md            # Gestión de categorías
│   │   ├── 17-reports-kyc-queue.md           # Cola de KYC
│   │   ├── 18-admin-settings.md              # Configuración admin
│   │   ├── 19-ml-admin-dashboards.md         # Dashboards de ML
│   │   └── 20-admin-rbac.md                  # Roles y permisos (RBAC)
│   │
│   ├── 07-PAGOS/                    # 💳 Checkout y Pagos (5 docs)
│   │   ├── 01-pagos-checkout.md              # Flujo de checkout
│   │   ├── 02-payment-results.md             # Resultados de pago
│   │   ├── 03-billing-dashboard.md           # Dashboard de facturación
│   │   ├── 04-moneda-extranjera.md           # Multi-moneda (USD/DOP)
│   │   └── 05-comercio-electronico.md        # E-commerce y pagos
│   │
│   ├── 08-DGII-COMPLIANCE/          # 📋 Compliance DGII RD (8 docs)
│   │   ├── 01-facturacion-dgii.md            # NCF y facturación electrónica
│   │   ├── 02-auditoria-compliance-legal.md  # Auditoría y compliance legal
│   │   ├── 03-obligaciones-fiscales.md       # Obligaciones fiscales
│   │   ├── 04-registro-gastos.md             # Registro de gastos operativos
│   │   ├── 05-automatizacion-reportes.md     # Automatización DGII
│   │   ├── 06-preparacion-auditoria.md       # Preparación para auditoría
│   │   ├── 07-consentimiento-comunicaciones.md # Ley 172-13 RD
│   │   └── 08-legal-common-pages.md          # Páginas legales requeridas
│   │
│   └── 09-COMPONENTES-COMUNES/      # 🧩 Componentes Compartidos (6 docs)
│       ├── 01-common-components.md           # Componentes genéricos
│       ├── 02-layouts.md                     # MainLayout, DashboardLayout
│       ├── 03-static-pages.md                # About, Contact, FAQ
│       ├── 04-vehicle-media.md               # Galería, 360°, Video
│       ├── 05-video-tour.md                  # Video tour interactivo
│       └── 06-event-tracking-sdk.md          # SDK de analytics
│
├── 05-API-INTEGRATION/              # 🔌 Integración con APIs (9 archivos)
│   ├── 01-cliente-http.md           # ✅ Cliente Axios base
│   ├── 02-autenticacion.md          # ✅ Auth endpoints (8)
│   ├── 03-formularios.md            # ✅ Patrones de formularios
│   ├── 04-subida-imagenes.md        # ✅ Upload de imágenes/media
│   ├── 05-vehicle-360-api.md        # ✅ Vehicle360 API (6 endpoints)
│   ├── 08-rate-limits-pagination.md # 🆕 Rate limits y paginación
│   ├── 31-state-management.md       # 🆕 Zustand stores (auth, favorites, etc.)
│   ├── 32-realtime-websockets.md    # 🆕 WebSocket para chat y notificaciones
│   └── 33-mock-data-examples.md     # 🆕✅ Mock data y ejemplos de respuestas API
│
├── 06-TESTING/                      # 🧪 Testing (6 archivos)
│   ├── 01-estrategia-testing.md     # Estrategia de testing (Vitest + RTL)
│   ├── 02-coverage-ci.md            # Coverage y CI/CD
│   ├── 03-e2e-fixtures.md           # 🆕 Factories y fixtures para E2E
│   ├── 04-ci-cd-integration.md      # 🆕 GitHub Actions, Playwright CI
│   ├── 05-implementation-checklists.md # 🆕✅ Checklists por tipo de componente
│   └── 06-test-suite-implementation.md # 🆕✅ Plan detallado de tests (Feb 2026)
│
└── 07-BACKEND-SUPPORT/              # 🔧 Soporte Backend (12 archivos)
    └── 01-supportservice.md         # SupportService nuevo
```

### 📊 Estadísticas de Documentación

| **Carpeta**               | **Archivos** | **Estado**              |
| ------------------------- | ------------ | ----------------------- |
| **00-ROOT**               | 5            | ✅ Completo             |
| **01-SETUP**              | 14           | ✅ Completo (+6 nuevos) |
| **02-UX-DESIGN-SYSTEM**   | 8            | ✅ Completo (+2 nuevos) |
| **03-COMPONENTES**        | 7            | ✅ Completo (+1 nuevo)  |
| **04-PAGINAS/**           | **104**      | ✅ Completo             |
| └─ 01-PUBLICO             | 10           | ✅ Completo             |
| └─ 02-AUTH                | 7            | ✅ Completo (+1 nuevo)  |
| └─ 03-COMPRADOR           | 14           | ✅ Completo             |
| └─ 04-VENDEDOR            | 5            | ✅ Completo             |
| └─ 05-DEALER              | 25           | ✅ Completo             |
| └─ 06-ADMIN               | 20           | ✅ Completo             |
| └─ 07-PAGOS               | 5            | ✅ Completo             |
| └─ 08-DGII-COMPLIANCE     | 8            | ✅ Completo             |
| └─ 09-COMPONENTES-COMUNES | 6            | ✅ Completo             |
| **05-API-INTEGRATION**    | 33           | ✅ Completo (+1 nuevo)  |
| **06-TESTING**            | 6            | ✅ Completo (+4 nuevos) |
| **07-BACKEND-SUPPORT**    | 12           | ✅ Completo             |
| **TOTAL**                 | **179**      | ✅ 100%                 |

### 🆕 Documentos Agregados (Enero 31, 2026 - Auditoría IA)

| Archivo                                       | Descripción                           | Prioridad |
| --------------------------------------------- | ------------------------------------- | --------- |
| `01-SETUP/13-routing-map.md`                  | ✅ Mapa completo de rutas (~96 rutas) | 🔴 P0     |
| `01-SETUP/14-architecture-decisions.md`       | ✅ ADR: 10 decisiones arquitectónicas | 🔴 P0     |
| `03-COMPONENTES/00-global-types.md`           | ✅ Tipos TypeScript centralizados     | 🔴 P0     |
| `04-PAGINAS/02-AUTH/07-auth-flow-diagrams.md` | ✅ 7 flujos de auth con diagramas     | 🔴 P0     |
| `05-API-INTEGRATION/33-mock-data-examples.md` | ✅ Mock data para testing (10 APIs)   | 🟠 P1     |
| `06-TESTING/05-implementation-checklists.md`  | ✅ Checklists por tipo de componente  | 🟠 P1     |

---

## 🗺️ ROADMAP DE IMPLEMENTACIÓN

| **07-BACKEND-SUPPORT** | 1 | ✅ Completo |
| **TOTAL** | **79 archivos** | **Mixto** |

**⚠️ CRÍTICO:** Solo 5 de 187 endpoints del Gateway están documentados en API-INTEGRATION (2.7%)

---

## 🔄 ORDEN DE EJECUCIÓN RECOMENDADO

### 🚨 PRIORIDAD CRÍTICA: Documentar APIs Faltantes

**ANTES de implementar frontend, se deben crear 32 procesos de API faltantes:**

Ver: [AUDITORIA-GATEWAY-ENDPOINTS.md](./AUDITORIA-GATEWAY-ENDPOINTS.md)

**Sprint 1 (Esta semana):**

- 06-vehicles-api.md (16 endpoints, 8h)
- 07-users-api.md (24 endpoints, 10h)
- 10-media-api.md (5 endpoints, 3h)
- 14-comparison-api.md (7 endpoints, 3h)
- 16-maintenance-api.md (5 endpoints, 2h)

**Total Sprint 1:** 57 endpoints, 26 horas → Cobertura: 8% → 38.5%

---

### 📅 Roadmap de Implementación Frontend

#### Fase 1: Setup & Fundamentos (Semana 1)

```
✅ SETUP INICIAL
├── 01-SETUP/01-crear-proyecto.md
├── 01-SETUP/02-configurar-typescript.md
├── 01-SETUP/03-configurar-eslint.md
├── 01-SETUP/04-instalar-shadcn.md
└── 01-SETUP/05-configurar-playwright.md

✅ SISTEMA DE DISEÑO
├── 02-UX-DESIGN-SYSTEM/01-principios-ux.md
├── 02-UX-DESIGN-SYSTEM/02-design-tokens.md
├── 02-UX-DESIGN-SYSTEM/03-componentes-base.md
├── 02-UX-DESIGN-SYSTEM/04-patrones-ux.md
├── 02-UX-DESIGN-SYSTEM/05-animaciones.md
└── 02-UX-DESIGN-SYSTEM/06-accesibilidad.md

✅ CLIENTE HTTP & AUTH
├── 05-API-INTEGRATION/01-cliente-http.md
└── 05-API-INTEGRATION/02-autenticacion.md
```

#### Fase 2: Componentes Base (Semana 2)

```
✅ COMPONENTES CORE
├── 03-COMPONENTES/01-layout.md
├── 03-COMPONENTES/02-formularios.md
├── 03-COMPONENTES/03-vehiculos.md
├── 03-COMPONENTES/04-dealers.md
├── 03-COMPONENTES/05-usuarios.md
└── 03-COMPONENTES/06-vehicle-360-viewer.md
```

#### Fase 3: Páginas Públicas (Semana 3-4)

```
✅ PÁGINAS PÚBLICAS
├── 04-PAGINAS/01-PUBLICO/  (10 documentos)
├── 04-PAGINAS/02-AUTH/     (6 documentos)
└── 04-PAGINAS/09-COMPONENTES-COMUNES/  (6 documentos)
```

#### Fase 4: Portal de Usuario (Semana 5-6)

```
✅ USUARIO COMPRADOR Y VENDEDOR
├── 04-PAGINAS/03-COMPRADOR/  (10 documentos)
└── 04-PAGINAS/04-VENDEDOR/   (5 documentos)
```

#### Fase 5: Portal de Dealer (Semana 7-9)

```
✅ DEALER COMPLETO
└── 04-PAGINAS/05-DEALER/  (15 documentos)
```

#### Fase 6: Pagos & Facturación (Semana 10-11)

```
✅ BILLING SYSTEM
├── 04-PAGINAS/07-PAGOS/           (5 documentos)
└── 04-PAGINAS/08-DGII-COMPLIANCE/ (8 documentos)
```

#### Fase 7-9: Admin Portal (Semana 12-18)

```
✅ ADMIN COMPLETO
└── 04-PAGINAS/06-ADMIN/  (16 documentos incluido RBAC)
```

#### Fase 10: Testing & Optimización (Semana 19-20)

```
✅ TESTING COMPLETO
├── 06-TESTING/01-estrategia-testing.md
└── 06-TESTING/02-coverage-ci.md

✅ OPTIMIZACIÓN
├── Performance audit
├── SEO optimization
├── Accessibility audit
└── Security review
```

---

## ⏱️ TIMELINE ESTIMADO

| Fase               | Duración       | Acumulado | Entregables                         |
| ------------------ | -------------- | --------- | ----------------------------------- |
| **APIs Faltantes** | 7 semanas      | 7 sem     | 32 procesos API, 187 endpoints doc  |
| **Fase 1-3**       | 4 semanas      | 11 sem    | Setup + Core + Páginas públicas     |
| **Fase 4-5**       | 5 semanas      | 16 sem    | Portales Usuario + Dealer           |
| **Fase 6-8**       | 6 semanas      | 22 sem    | Pagos + Reviews + IA                |
| **Fase 9-10**      | 4 semanas      | 26 sem    | Admin + Compliance                  |
| **Fase 11-12**     | 3 semanas      | 29 sem    | Features finales + Testing          |
| **TOTAL**          | **29 semanas** | -         | **Frontend completo en producción** |

### 🚀 Con 2 Devs en Paralelo: ~15 semanas (~4 meses)

---

## ⚠️ REGLAS PARA MODELO DE IA

### Al ejecutar cada documento:

1. **LEER COMPLETO** antes de ejecutar comandos
2. **VERIFICAR** que el paso anterior está completo
3. **EJECUTAR** comandos uno por uno, no en batch
4. **VALIDAR** output esperado vs real
5. **REPORTAR** cualquier error antes de continuar

### Formato de cada documento:

````markdown
## Paso X: [Nombre]

### Prerrequisitos

- [ ] Paso anterior completado
- [ ] Directorio correcto

### Comandos

\```bash

# Comando a ejecutar

comando aquí
\```

### Código a crear

\```typescript
// filepath: ruta/al/archivo.ts
// Código completo aquí
\```

### Validación

\```bash

# Cómo verificar que funcionó

comando de verificación
\```

### Output esperado

\```
Lo que deberías ver
\```

### Troubleshooting

- Error X → Solución Y
````

---

## 📊 MÉTRICAS DE VALIDACIÓN

Después de cada fase, verificar:

| Fase     | Comando                             | Criterio de Éxito |
| -------- | ----------------------------------- | ----------------- |
| Setup    | `pnpm build`                        | Exit code 0       |
| Setup    | `pnpm test`                         | 0 failures        |
| Setup    | `pnpm lint`                         | 0 errors          |
| Core     | `pnpm storybook`                    | Abre en :6006     |
| Auth     | `pnpm test:e2e -- --grep "auth"`    | 5/5 pass          |
| Vehicles | `pnpm test:e2e -- --grep "vehicle"` | 8/8 pass          |
| Full     | Lighthouse audit                    | > 90 score        |

---

## 📊 PROGRESO ACTUAL (Actualizado - Auditoría 12)

### ✅ Completado

| Área                         | Items                      | Estado  |
| ---------------------------- | -------------------------- | ------- |
| **Documentación de Páginas** | 80/80 (9 subcarpetas)      | ✅ 100% |
| **Sistema de Diseño**        | 6/6                        | ✅ 100% |
| **Componentes**              | 6/6                        | ✅ 100% |
| **Setup Guides**             | 5/5                        | ✅ 100% |
| **API Integration**          | 6/6 (Auditoría completada) | ✅ 100% |
| **Tests E2E (Playwright)**   | Agregados a 80 docs        | ✅ 100% |
| **Backend Services**         | 13/13 microservicios       | ✅ 100% |
| **Kubernetes Deploy**        | DOKS producción            | ✅ 100% |

### � Métricas de Cobertura

```
Backend:              ████████████████████ 100% (13 servicios en producción)
Docs Páginas:         ████████████████████ 100% (80 docs en 9 subcarpetas)
Docs APIs:            ████████████████████ 100% (187 endpoints auditados)
Tests E2E Docs:       ████████████████████ 100% (secciones agregadas)
Frontend Código:      ████░░░░░░░░░░░░░░░░  20% (Next.js implementación en progreso)
```

### 🚀 Progreso de Implementación Frontend (Next.js)

> **Proyecto:** `frontend/web-next/`  
> **Stack:** Next.js 16.1.6 + React 19 + TypeScript + TailwindCSS 4.x  
> **Última actualización:** Enero 2026

#### ✅ Implementado

| Categoría    | Componente/Página     | Archivo                                             | Estado |
| ------------ | --------------------- | --------------------------------------------------- | ------ |
| **Setup**    | Proyecto Next.js      | `frontend/web-next/`                                | ✅     |
| **Setup**    | TypeScript + ESLint   | `tsconfig.json`, `eslint.config.mjs`                | ✅     |
| **Setup**    | TailwindCSS 4.x       | `postcss.config.mjs`, `globals.css`                 | ✅     |
| **UI Base**  | Dialog, Select, Tabs  | `src/components/ui/`                                | ✅     |
| **UI Base**  | VehicleCard, Badge    | `src/components/ui/`                                | ✅     |
| **Homepage** | HeroCarousel          | `src/components/homepage/hero-carousel.tsx`         | ✅     |
| **Homepage** | FeaturedSection       | `src/components/homepage/featured-section.tsx`      | ✅     |
| **Homepage** | FeaturedListingGrid   | `src/components/homepage/featured-listing-grid.tsx` | ✅     |
| **Páginas**  | Homepage (/)          | `src/app/page.tsx`                                  | ✅     |
| **Páginas**  | Búsqueda (/vehiculos) | `src/app/vehiculos/page.tsx`                        | ✅     |
| **Services** | homepage-sections     | `src/services/homepage-sections.ts`                 | ✅     |
| **Hooks**    | useHomepageSections   | `src/hooks/use-homepage-sections.ts`                | ✅     |

#### ⏳ Pendiente

| Categoría       | Componente/Página | Doc de Referencia                              |
| --------------- | ----------------- | ---------------------------------------------- |
| **Páginas**     | Detalle Vehículo  | `04-PAGINAS/01-PUBLICO/02-detalle-vehiculo.md` |
| **Páginas**     | Help Center       | `04-PAGINAS/01-PUBLICO/09-help-center.md`      |
| **Páginas**     | Login/Register    | `04-PAGINAS/02-AUTENTICACION/`                 |
| **Páginas**     | Dashboard Dealer  | `04-PAGINAS/03-DEALER/`                        |
| **Componentes** | Forms avanzados   | `03-COMPONENTES/03-forms.md`                   |
| **Auth**        | Zustand store     | `03-COMPONENTES/05-state-management.md`        |

### ✅ Auditorías Completadas (14/15)

| #   | Auditoría                              | Estado |
| --- | -------------------------------------- | ------ |
| 1-9 | Reorganización y secciones estándar    | ✅     |
| 10  | Agregar sección E2E Tests (Playwright) | ✅     |
| 11  | Validar dependencias entre docs        | ✅     |
| 12  | Actualizar índice maestro              | ✅     |
| 13  | Limpiar archivos obsoletos             | ✅     |
| 14  | Crear script de validación             | ✅     |
| 15  | Documentar backend (índice creado)     | 🟡     |

### ⏳ Trabajo Continuo

| Tarea        | Descripción                             | Estado      |
| ------------ | --------------------------------------- | ----------- |
| Docs Backend | Documentación detallada de 37 servicios | En progreso |

---

## 🎯 PRÓXIMOS PASOS INMEDIATOS

### Completar Auditorías Restantes

```bash
# Auditoría 13: Limpiar archivos obsoletos (30 min)
□ Eliminar archivos duplicados/viejos en 04-PAGINAS root
□ Verificar que no haya archivos huérfanos

# Auditoría 14: Crear script de validación (1 hora)
□ Crear validate-docs.sh
□ Verificar enlaces internos
□ Verificar estructura de secciones

# Auditoría 15: Documentar backend faltante (8 horas)
□ Documentar microservicios no documentados
□ Actualizar endpoints faltantes
```

### Implementar Frontend (En Progreso ✅)

```bash
# Setup (COMPLETADO ✅)
✅ Proyecto Next.js 16.1.6 + TypeScript + TailwindCSS 4.x
✅ ESLint + Prettier configurados
✅ Radix UI + Framer Motion + Lucide React instalados

# Homepage (COMPLETADO ✅)
✅ HeroCarousel con auto-play (5s), swipe, badges
✅ FeaturedListingGrid con favoritos toggle
✅ FeaturedSection horizontal scroll con snap
✅ Integración API /homepagesections/homepage
✅ useHomepageSections hook (React Query)

# Búsqueda (COMPLETADO ✅)
✅ Página /vehiculos con filtros
✅ VehicleCard component
✅ Paginación y grids responsivos

# Siguiente Fase:
□ Página detalle vehículo (/vehiculos/[slug])
□ Sistema de autenticación (Zustand + JWT)
□ Dashboard para dealers
□ Help Center
```

---

## 📚 RECURSOS ADICIONALES

### Documentos de Referencia

| Documento                                                            | Propósito                           | Audiencia                 |
| -------------------------------------------------------------------- | ----------------------------------- | ------------------------- |
| [AUDITORIA-GATEWAY-ENDPOINTS.md](./AUDITORIA-GATEWAY-ENDPOINTS.md)   | Auditoría completa de 187 endpoints | Tech Lead, Backend Team   |
| [AUDITORIA-RESUMEN-VISUAL.md](./AUDITORIA-RESUMEN-VISUAL.md)         | Dashboard visual de progreso        | Management, Frontend Team |
| [00-PLAN-AUDITORIA-CORRECCION.md](./00-PLAN-AUDITORIA-CORRECCION.md) | Plan de correcciones                | QA Team                   |
| [00-RESUMEN-AUDITORIA.md](./00-RESUMEN-AUDITORIA.md)                 | Resumen ejecutivo                   | Stakeholders              |

### Enlaces Útiles

- **GitHub Repo:** [cardealer-microservices](https://github.com/gregorymorenoiem/cardealer-microservices)
- **Producción:** https://okla.com.do
- **API Gateway:** https://api.okla.com.do
- **Kubernetes:** Digital Ocean DOKS (okla-cluster, namespace: okla)
- **CI/CD:** GitHub Actions (.github/workflows/)

---

## 🚀 COMENZAR IMPLEMENTACIÓN

### Para Desarrolladores Frontend

**IMPORTANTE:** No comenzar implementación hasta completar Sprint 1 de APIs.

**Cuando las APIs estén documentadas:**

1. Ejecutar `docs/frontend-rebuild/01-SETUP/01-crear-proyecto.md`
2. Seguir orden secuencial de fases (ver sección "Roadmap de Implementación")
3. Validar cada fase con métricas definidas
4. Reportar bloqueos inmediatamente

### Para Tech Lead

**Acción Inmediata:**

1. ✅ Revisar esta auditoría completa
2. ⚠️ Asignar recursos para Sprint 1 (26 horas, 5 procesos API)
3. 📅 Planificar Sprints 2-7 (restantes 32-5=27 procesos, 111 horas)
4. 🎯 Definir KPIs de progreso semanal

### Para Product Manager

**Expectativas Realistas:**

- **APIs completas:** 7 semanas (o 4 semanas con 2 devs)
- **Frontend MVP:** 15 semanas adicionales con 2 devs
- **Total hasta producción:** ~5-6 meses

**Recomendación:** Priorizar MVP con funcionalidades core antes de features avanzados.

---

## 🏁 CRITERIOS DE ÉXITO

### Definición de "Completado"

Un proceso está **COMPLETADO** cuando:

✅ Código implementado y funcionando  
✅ Tests unitarios > 80% coverage  
✅ Tests E2E para flujos críticos  
✅ Documentación actualizada  
✅ Code review aprobado  
✅ Deploy a staging exitoso  
✅ QA sign-off

### MVP Frontend (Mínimo Viable)

**Páginas Críticas:**

- Home + Search + Vehicle Detail (Público)
- Login + Register (Auth)
- Dashboard + Publish (Usuario)
- Dealer Dashboard + Inventory (Dealer)
- Checkout + Payments (Billing)

**Endpoints Críticos:**

- Auth (8 endpoints) ✅
- Vehicles (16 endpoints) ❌
- Users (24 endpoints) ❌
- Media (8 endpoints) 🟨
- Billing (12 endpoints) ❌

**Total MVP:** ~60 endpoints, ~8 semanas de trabajo

---

## 📞 CONTACTO Y SOPORTE

**Para preguntas sobre esta documentación:**

- **Tech Lead:** Gregory Moreno
- **Email:** gmoreno@okla.com.do
- **Slack:** #okla-frontend-rebuild
- **Jira Board:** OKLA Frontend Sprint Planning

**Reportar problemas:**

- Issues en GitHub: https://github.com/gregorymorenoiem/cardealer-microservices/issues
- Etiquetar con: `frontend`, `documentation`, `api`

---

**✅ DOCUMENTO MAESTRO ACTUALIZADO**

_Este índice refleja el estado real de la documentación e implementación._
_Última actualización: Enero 2026 - Homepage + Búsqueda implementados en Next.js._

---

_Última actualización: Enero 29, 2026 17:30 AST_  
_Autor: Gregory Moreno_  
_Versión: 3.0 (Auditoría Completa)_
