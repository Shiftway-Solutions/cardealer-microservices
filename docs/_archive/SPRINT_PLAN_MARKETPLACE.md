# 🚀 Plan de Sprints - OKLA Marketplace

**Fecha:** Enero 8, 2026  
**Objetivo:** Marketplace de vehículos 100% funcional  
**Metodología:** Sprints de 2 semanas  
**Equipo estimado:** 3-4 desarrolladores full-stack

---

## 🎁 ESTRATEGIA DE LANZAMIENTO: EARLY BIRD

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       PLAN EARLY BIRD (3 MESES GRATIS)                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  🎁 DURANTE LANZAMIENTO (3 meses):                                         │
│  ├── ✅ TODOS publican GRATIS                                              │
│  ├── ✅ Sin límite de publicaciones                                        │
│  ├── ✅ Todas las features premium                                         │
│  ├── ✅ Badge "Miembro Fundador" permanente                                │
│  └── ✅ 20% descuento DE POR VIDA después del período                      │
│                                                                             │
│  💰 DESPUÉS DE EARLY BIRD:                                                  │
│  ├── Vendedores: $29/listing (Early Birds: $23)                            │
│  ├── Dealer Starter: $49/mes (Early Birds: $39)                            │
│  ├── Dealer Pro: $129/mes (Early Birds: $103)                              │
│  └── Dealer Enterprise: $299/mes (Early Birds: $239)                       │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🚨 SPRINT 3: CONSOLIDACIÓN DE BASE DE DATOS (PRIORIDAD ALTA)

**Duración:** 14 días  
**Objetivo:** Centralizar todas las bases de datos de microservicios en PostgresDbService

### 🎯 Objetivo Principal

Migrar todas las bases de datos individuales de microservicios (`*_db`) a un servicio centralizado `postgres_db` usando arquitectura JSONB flexible.

### 📝 Tareas del Sprint 3

#### 1️⃣ PostgresDbService - Servicio de Base de Datos Centralizada

**PRIMERA TAREA DEL SPRINT 3:**

- [ ] **PostgresDbService.Domain** ✅ COMPLETADO

  - Entidades base con JSONB support
  - Interfaces de repositorio genérico y específico
  - Multi-tenancy integration

- [ ] **PostgresDbService.Infrastructure** ✅ COMPLETADO

  - CentralizedDbContext con EF Core
  - GenericRepository con CRUD completo
  - Repositorios específicos (User, Vehicle, Contact)
  - Indexing strategy para performance

- [ ] **PostgresDbService.Api** ✅ COMPLETADO

  - Controllers genéricos y específicos
  - Health checks y Swagger docs
  - JWT authentication
  - Docker containerization

- [ ] **Testing Infrastructure** ✅ COMPLETADO
  - Proyecto de tests PostgresDbService.Tests
  - Test helpers y factories
  - Unit tests para GenericRepository (8 tests)
  - Integration tests para controllers

#### 2️⃣ Migración de Datos

- [ ] **Análisis de Esquemas Existentes**

  - Inventario de todas las bases de datos actuales
  - Mapeo de entidades a estructura JSONB
  - Plan de migración de datos sin downtime

- [ ] **Scripts de Migración**

  - Exportar datos de ApplicationDbContext individuales
  - Transformar a formato JSONB
  - Import automático a PostgresDbService

- [ ] **Validación de Datos**
  - Verificar integridad después de migración
  - Tests de performance con datos reales
  - Rollback plan en caso de problemas

#### 3️⃣ Actualización de Microservicios

- [ ] **Remover ApplicationDbContext individuales**

  - AuthService → Use PostgresDbService
  - UserService → Use PostgresDbService
  - VehiclesSaleService → Use PostgresDbService
  - ContactService → Use PostgresDbService
  - Otros servicios según aplique

- [ ] **Actualizar Referencias**

  - Cambiar dependencias de Entity Framework
  - Actualizar connection strings
  - Refactorizar repositories

- [ ] **Testing de Integración**
  - Todos los endpoints funcionando
  - Performance igual o mejor
  - No breaking changes en API

#### 4️⃣ CI/CD y Deployment

- [ ] **Agregar PostgresDbService a smart-cicd.yml**
- [ ] **Kubernetes manifests**

  - Deployment para PostgresDbService
  - Service y ConfigMaps
  - Ingress routing

- [ ] **Database Migration Strategy**
  - Blue-green deployment approach
  - Zero-downtime migration
  - Monitoring y alerting

### 🧪 Testing Requirements (OBLIGATORIO)

- [ ] **PostgresDbService.Tests**

  - ✅ GenericRepositoryTests (8 tests)
  - [ ] UserRepositoryTests (6 tests)
  - [ ] VehicleRepositoryTests (7 tests)
  - [ ] ContactRepositoryTests (5 tests)
  - [ ] ControllersTests (10 tests)
  - [ ] IntegrationTests (5 tests)

- [ ] **Migration Tests**
  - [ ] Data integrity tests (3 tests)
  - [ ] Performance regression tests (2 tests)
  - [ ] Rollback scenario tests (2 tests)

### 📊 Success Criteria

1. ✅ PostgresDbService completamente funcional
2. [ ] Todos los datos migrados sin pérdida
3. [ ] Todos los microservicios funcionando con nueva DB
4. [ ] Performance igual o mejor que antes
5. [ ] Zero downtime durante migración
6. [ ] 100% test coverage en componentes críticos
7. [ ] CI/CD pipeline funcionando
8. [ ] Documentación de migración completa

---

## 📊 ESTADO ACTUAL (Baseline)

### ✅ Ya en Producción (DOKS)

| Servicio            | Estado | Funcionalidad             |
| ------------------- | ------ | ------------------------- |
| frontend-web        | ✅     | React 19 SPA básica       |
| gateway             | ✅     | Ocelot API Gateway        |
| authservice         | ✅     | Login/Register/JWT        |
| userservice         | ✅     | CRUD usuarios básico      |
| roleservice         | ✅     | Roles y permisos          |
| vehiclessaleservice | ✅     | CRUD vehículos + catálogo |
| mediaservice        | ✅     | Upload imágenes S3        |
| notificationservice | ✅     | Email/SMS básico          |
| billingservice      | ✅     | Stripe básico             |
| errorservice        | ✅     | Logging errores           |

### ❌ Falta para MVP Marketplace

| Feature                            | Prioridad  | Sprint Target |
| ---------------------------------- | ---------- | ------------- |
| Búsqueda avanzada con filtros      | 🔴 CRÍTICO | Sprint 1      |
| Favoritos y guardados              | 🔴 CRÍTICO | Sprint 1      |
| Plan Early Bird + Onboarding       | 🔴 CRÍTICO | Sprint 1      |
| MaintenanceService                 | 🔴 CRÍTICO | Sprint 1      |
| Contactar vendedor                 | 🔴 CRÍTICO | Sprint 2      |
| Comparador de vehículos            | 🟡 ALTO    | Sprint 2      |
| Alertas de precio                  | 🟡 ALTO    | Sprint 2      |
| Publicar vehículos (wizard)        | 🔴 CRÍTICO | Sprint 3      |
| Sistema de pagos (post Early Bird) | 🔴 CRÍTICO | Sprint 4      |
| Panel de dealer                    | 🟡 ALTO    | Sprint 5-6    |

---

## 🎯 FASES DEL PROYECTO

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          ROADMAP GENERAL                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  FASE 1: MVP MARKETPLACE (Sprints 1-4)                      ████████░░░░░░  │
│  └── Compradores pueden buscar, ver y contactar vendedores                  │
│  └── Vendedores individuales pueden publicar vehículos                      │
│  └── Plan Early Bird: 3 meses GRATIS para todos                             │
│  └── MaintenanceService para operaciones                                    │
│                                                                              │
│  FASE 2: DEALERS BÁSICO (Sprints 5-8)                       ░░░░░░████░░░░  │
│  └── Cuentas de dealer con suscripción mensual                              │
│  └── Panel de dealer con inventario                                         │
│  └── Estadísticas básicas de listings                                       │
│                                                                              │
│  FASE 3: DATA & ANALYTICS (Sprints 9-12)                    ░░░░░░░░░░████  │
│  └── Event tracking completo                                                 │
│  └── Lead scoring para dealers                                              │
│  └── Dashboard de métricas                                                  │
│                                                                              │
│  FASE 4: IA & DIFERENCIACIÓN (Sprints 13-18)                ░░░░░░░░░░░░██  │
│  └── Chatbot con calificación de leads                                      │
│  └── Recomendaciones personalizadas                                         │
│  └── Reviews estilo Amazon                                                  │
│  └── Pricing inteligente                                                    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

# 📅 FASE 1: MVP MARKETPLACE

## Sprint 1 (Semanas 1-2) - Búsqueda y Descubrimiento

**Objetivo:** Los compradores pueden encontrar vehículos fácilmente

### Backend

| Task                                            | Servicio                      | Story Points |
| ----------------------------------------------- | ----------------------------- | ------------ |
| Implementar búsqueda full-text con PostgreSQL   | VehiclesSaleService           | 5            |
| API de filtros (marca, modelo, año, precio, km) | VehiclesSaleService           | 5            |
| Endpoint de vehículos similares                 | VehiclesSaleService           | 3            |
| API de favoritos (añadir/quitar/listar)         | VehiclesSaleService           | 5            |
| Paginación y ordenamiento optimizado            | VehiclesSaleService           | 3            |
| **MaintenanceService base**                     | **MaintenanceService (5061)** | **5**        |
| **Plan Early Bird en BillingService**           | **BillingService**            | **5**        |
| **Onboarding flags en UserService**             | **UserService**               | **3**        |

### Frontend

| Task                                           | Componente           | Story Points |
| ---------------------------------------------- | -------------------- | ------------ |
| Página de búsqueda con filtros sidebar         | SearchPage           | 8            |
| Componente de filtros (marca/modelo cascading) | FilterSidebar        | 5            |
| Grid de resultados con lazy loading            | VehicleGrid          | 5            |
| Detalle de vehículo mejorado                   | VehicleDetailPage    | 5            |
| Botón y lista de favoritos                     | FavoritesFeature     | 3            |
| Carrusel de fotos con zoom                     | PhotoGallery         | 3            |
| **Página de mantenimiento**                    | **MaintenancePage**  | **3**        |
| **Banner "3 meses gratis" + Countdown**        | **EarlyBirdBanner**  | **3**        |
| **Onboarding wizard (comprador/vendedor)**     | **OnboardingWizard** | **8**        |
| **Badge "Miembro Fundador"**                   | **FounderBadge**     | **2**        |

### Entregables Sprint 1

```
✅ Usuario puede buscar vehículos por texto
✅ Usuario puede filtrar por marca, modelo, año, precio, km
✅ Usuario puede ordenar resultados (precio, fecha, km)
✅ Usuario puede guardar vehículos en favoritos
✅ Usuario puede ver galería de fotos completa
🆕 MaintenanceService funcionando (admin puede activar modo mantenimiento)
🆕 Plan Early Bird activo (todos publican gratis 3 meses)
🆕 Onboarding guiado para nuevos usuarios
🆕 Badge "Miembro Fundador" para Early Birds
```

**Story Points Total:** 71  
**Velocidad esperada:** 60-75 SP (sprint de lanzamiento, más esfuerzo)

---

## Sprint 2 (Semanas 3-4) - Contacto + UX Avanzado

**Objetivo:** Compradores pueden contactar vendedores + features de engagement

### Backend

| Task                                              | Servicio                     | Story Points |
| ------------------------------------------------- | ---------------------------- | ------------ |
| ContactService: crear consulta                    | ContactService               | 5            |
| ContactService: listar consultas (vendedor)       | ContactService               | 3            |
| ContactService: responder consulta                | ContactService               | 3            |
| NotificationService: email de nueva consulta      | NotificationService          | 3            |
| NotificationService: email de respuesta           | NotificationService          | 3            |
| UserService: perfil público de vendedor           | UserService                  | 5            |
| **ComparisonService: comparar hasta 3 vehículos** | **ComparisonService (5066)** | **5**        |
| **AlertService: alertas de precio/búsqueda**      | **AlertService (5067)**      | **5**        |

### Frontend

| Task                                     | Componente            | Story Points |
| ---------------------------------------- | --------------------- | ------------ |
| Modal de contactar vendedor              | ContactModal          | 5            |
| Formulario con validación                | ContactForm           | 3            |
| Página de mis consultas (comprador)      | MyInquiriesPage       | 5            |
| Página de consultas recibidas (vendedor) | ReceivedInquiriesPage | 5            |
| Perfil público del vendedor              | SellerProfilePage     | 5            |
| Chat/mensajería básica                   | MessageThread         | 8            |
| **Comparador de vehículos (hasta 3)**    | **VehicleComparator** | **8**        |
| **Crear/gestionar alertas de precio**    | **PriceAlerts**       | **5**        |

### Entregables Sprint 2

```
✅ Comprador puede enviar consulta sobre vehículo
✅ Vendedor recibe email de nueva consulta
✅ Vendedor puede responder consulta
✅ Comprador recibe email de respuesta
✅ Ambos pueden ver historial de mensajes
✅ Comprador puede ver perfil del vendedor
```

**Story Points Total:** 53  
**Velocidad esperada:** 45-55 SP

---

## Sprint 3 (Semanas 5-6) - Publicar Vehículos

**Objetivo:** Vendedores individuales pueden publicar vehículos

### Backend

| Task                             | Servicio            | Story Points |
| -------------------------------- | ------------------- | ------------ |
| API de publicación multi-step    | VehiclesSaleService | 5            |
| Validación de datos del vehículo | VehiclesSaleService | 3            |
| Upload múltiple de imágenes      | MediaService        | 5            |
| Ordenamiento de imágenes         | MediaService        | 3            |
| Draft/borrador de publicación    | VehiclesSaleService | 3            |
| Previsualización de listing      | VehiclesSaleService | 2            |

### Frontend

| Task                                     | Componente     | Story Points |
| ---------------------------------------- | -------------- | ------------ |
| Wizard de publicación (5 pasos)          | PublishWizard  | 13           |
| Step 1: Datos básicos (marca/modelo/año) | BasicInfoStep  | 5            |
| Step 2: Características y detalles       | FeaturesStep   | 5            |
| Step 3: Upload y ordenar fotos           | PhotosStep     | 8            |
| Step 4: Precio y ubicación               | PricingStep    | 3            |
| Step 5: Revisión y publicar              | ReviewStep     | 5            |
| Mis publicaciones (vendedor)             | MyListingsPage | 5            |

### Entregables Sprint 3

```
✅ Vendedor puede crear publicación paso a paso
✅ Vendedor puede subir hasta 20 fotos
✅ Vendedor puede ordenar fotos (drag & drop)
✅ Vendedor puede guardar borrador
✅ Vendedor puede previsualizar antes de publicar
✅ Vendedor puede ver sus publicaciones activas
✅ Vendedor puede editar/pausar/eliminar publicación
```

**Story Points Total:** 60  
**Velocidad esperada:** 50-60 SP

---

## Sprint 4 (Semanas 7-8) - Pagos y Monetización

**Objetivo:** Sistema de cobro por publicación funcional (Stripe + Azul)

### Backend

| Task                                    | Servicio            | Story Points |
| --------------------------------------- | ------------------- | ------------ |
| BillingService: checkout de listing     | BillingService      | 8            |
| Integración Stripe Checkout             | BillingService      | 5            |
| Webhooks de Stripe (payment_intent)     | BillingService      | 5            |
| **Integración Azul (Banco Popular RD)** | **BillingService**  | **8**        |
| **Webhooks de Azul**                    | **BillingService**  | **5**        |
| **PaymentGatewayFactory (Stripe/Azul)** | **BillingService**  | **5**        |
| Activar listing post-pago               | VehiclesSaleService | 3            |
| Historial de pagos del usuario          | BillingService      | 3            |
| Facturas/recibos automáticos            | BillingService      | 5            |

### Frontend

| Task                                            | Componente                | Story Points |
| ----------------------------------------------- | ------------------------- | ------------ |
| Página de pricing ($29/listing)                 | PricingPage               | 5            |
| **Selector de método de pago (Stripe/Azul)**    | **PaymentMethodSelector** | **5**        |
| Checkout embebido de Stripe                     | StripeCheckout            | 5            |
| **Checkout de Azul (formulario tarjeta local)** | **AzulCheckout**          | **8**        |
| Página de éxito post-pago                       | PaymentSuccessPage        | 3            |
| Historial de pagos                              | PaymentHistoryPage        | 5            |
| Banner de listing pendiente de pago             | PendingPaymentBanner      | 2            |

### Integración Azul (Banco Popular)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    PASARELAS DE PAGO OKLA                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  💳 SELECTOR DE MÉTODO DE PAGO                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  ¿Cómo deseas pagar?                                                │   │
│  │                                                                      │   │
│  │  ○ 🏦 Azul (Banco Popular) - Tarjetas dominicanas                   │   │
│  │     Visa, Mastercard, American Express                              │   │
│  │     ✅ Sin comisión internacional                                    │   │
│  │                                                                      │   │
│  │  ○ 💳 Stripe - Tarjetas internacionales                             │   │
│  │     Visa, Mastercard, American Express                              │   │
│  │     Ideal para tarjetas de USA/Europa                               │   │
│  │                                                                      │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  BENEFICIOS DE AZUL:                                                        │
│  ├── ✅ Comisiones más bajas para tarjetas locales (2.5% vs 3.5%)         │
│  ├── ✅ Confianza del usuario dominicano                                  │
│  ├── ✅ Soporte en español 24/7                                           │
│  ├── ✅ Acepta todas las tarjetas de bancos RD                            │
│  └── ✅ Depósitos en cuenta local en 24-48 horas                          │
│                                                                             │
│  BENEFICIOS DE STRIPE:                                                      │
│  ├── ✅ Acepta tarjetas internacionales                                   │
│  ├── ✅ Mejor para dominicanos en el exterior                             │
│  ├── ✅ Apple Pay, Google Pay                                             │
│  └── ✅ Mejor detección de fraude                                         │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Arquitectura de Pagos Multi-Gateway

```csharp
// PaymentGatewayFactory - Patrón Strategy
public interface IPaymentGateway
{
    Task<PaymentResult> ProcessPayment(PaymentRequest request);
    Task<PaymentResult> ProcessSubscription(SubscriptionRequest request);
    Task<RefundResult> ProcessRefund(RefundRequest request);
    Task HandleWebhook(string payload, string signature);
}

public class StripeGateway : IPaymentGateway { }
public class AzulGateway : IPaymentGateway { }

public class PaymentGatewayFactory
{
    public IPaymentGateway GetGateway(PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.Stripe => new StripeGateway(_stripeConfig),
            PaymentMethod.Azul => new AzulGateway(_azulConfig),
            _ => throw new NotSupportedException()
        };
    }
}
```

### Entregables Sprint 4

```
✅ Vendedor ve precio antes de publicar ($29)
✅ Vendedor puede elegir: Azul (local) o Stripe (internacional)
✅ Vendedor puede pagar con tarjeta dominicana (Azul)
✅ Vendedor puede pagar con tarjeta internacional (Stripe)
✅ Listing se activa automáticamente post-pago
✅ Vendedor recibe factura por email
✅ Vendedor puede ver historial de pagos
✅ Sistema maneja pagos fallidos correctamente
✅ Webhooks de ambas pasarelas funcionando
```

**Story Points Total:** 72  
**Velocidad esperada:** 60-75 SP (sprint más largo por integración Azul)

---

## Sprint 4.5 (Semanas 9-10) - 🧾 Facturación Electrónica DGII (e-CF)

**Objetivo:** Cumplir con normativa DGII de República Dominicana para comprobantes fiscales electrónicos

### ⚠️ REQUISITO LEGAL OBLIGATORIO

En RD, desde 2023 la DGII exige que todas las empresas emitan **Comprobantes Fiscales Electrónicos (e-CF)** en lugar de NCF físicos. OKLA DEBE cumplir con esto para:

1. Emitir facturas válidas a dealers (suscripciones)
2. Emitir facturas a vendedores individuales (listings)
3. Evitar multas y sanciones de DGII

### Tipos de Comprobantes Necesarios

| Código | Tipo                      | Uso en OKLA                   |
| ------ | ------------------------- | ----------------------------- |
| **31** | Factura de Crédito Fiscal | Ventas a Dealers (con RNC)    |
| **32** | Factura de Consumo        | Ventas a individuos (sin RNC) |
| **33** | Nota de Débito            | Cargos adicionales            |
| **34** | Nota de Crédito           | Reembolsos y anulaciones      |

### Backend Tasks

| Task                                    | Servicio       | Story Points |
| --------------------------------------- | -------------- | ------------ |
| DGIIService base (nuevo)                | Nuevo servicio | 8            |
| Certificado digital DGII (setup)        | DGIIService    | 3            |
| API de autenticación DGII               | DGIIService    | 5            |
| Generación de e-CF (XML firmado)        | DGIIService    | 8            |
| Envío de e-CF a DGII                    | DGIIService    | 5            |
| Recepción de respuesta DGII             | DGIIService    | 3            |
| Almacenamiento de e-CF                  | DGIIService    | 3            |
| Anulación de e-CF                       | DGIIService    | 3            |
| Consulta de estado e-CF                 | DGIIService    | 2            |
| Actualizar Invoice entity (e-CF fields) | BillingService | 3            |
| Workflow: pago → e-CF → email           | BillingService | 5            |
| Cron: reintentos de e-CF fallidos       | DGIIService    | 3            |

### Entidad Invoice Actualizada

```csharp
public class Invoice
{
    // ... campos existentes ...

    // 🆕 Campos DGII
    public string? ECF { get; private set; }              // e-CF número (ej: E310000000001)
    public string? NCF { get; private set; }              // NCF legacy (si aplica)
    public int TipoComprobante { get; private set; }      // 31, 32, 33, 34
    public string? RncComprador { get; private set; }     // RNC del dealer/comprador
    public string? RazonSocialComprador { get; private set; }
    public DateTime? FechaAutorizacionDGII { get; private set; }
    public string? CodigoSeguridad { get; private set; }  // Código de seguridad DGII
    public string? UrlVerificacion { get; private set; }  // URL para verificar en DGII
    public string? XmlFirmado { get; private set; }       // XML completo firmado
    public ECFStatus ECFStatus { get; private set; }      // Pending, Sent, Accepted, Rejected
    public string? ECFErrorMessage { get; private set; }
}

public enum ECFStatus
{
    NotApplicable,  // Pagos internacionales (Stripe fuera de RD)
    Pending,        // Esperando envío a DGII
    Sent,           // Enviado, esperando respuesta
    Accepted,       // Aceptado por DGII ✅
    Rejected,       // Rechazado por DGII ❌
    Cancelled       // Anulado
}
```

### Frontend Tasks

| Task                                   | Componente         | Story Points |
| -------------------------------------- | ------------------ | ------------ |
| Campo RNC en checkout (opcional)       | RNCInput           | 3            |
| Validación de RNC (dígito verificador) | RNCValidator       | 2            |
| Mostrar e-CF en factura                | InvoiceECF         | 3            |
| Descargar factura con e-CF (PDF)       | InvoiceDownload    | 3            |
| QR de verificación DGII                | DGIIVerificationQR | 2            |
| Admin: Monitor de e-CF                 | ECFMonitor         | 5            |

### Flujo de Facturación Electrónica

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    FLUJO e-CF OKLA                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1️⃣ PAGO EXITOSO (Stripe o Azul)                                           │
│      ↓                                                                      │
│  2️⃣ DETERMINAR TIPO DE COMPROBANTE                                         │
│      ├── ¿Tiene RNC? → Tipo 31 (Crédito Fiscal)                            │
│      └── ¿No tiene RNC? → Tipo 32 (Consumo)                                │
│      ↓                                                                      │
│  3️⃣ GENERAR XML DEL e-CF                                                   │
│      ├── Datos del emisor (OKLA)                                           │
│      ├── Datos del receptor (dealer/usuario)                               │
│      ├── Items facturados                                                  │
│      ├── ITBIS (18%)                                                       │
│      └── Totales                                                           │
│      ↓                                                                      │
│  4️⃣ FIRMAR XML CON CERTIFICADO DIGITAL                                     │
│      └── Certificado emitido por DGII                                      │
│      ↓                                                                      │
│  5️⃣ ENVIAR A DGII                                                          │
│      └── POST https://ecf.dgii.gov.do/...                                  │
│      ↓                                                                      │
│  6️⃣ RECIBIR RESPUESTA                                                      │
│      ├── ✅ Aceptado → Guardar e-CF, generar PDF                           │
│      └── ❌ Rechazado → Log error, reintentar o alertar                    │
│      ↓                                                                      │
│  7️⃣ ENVIAR FACTURA AL CLIENTE                                              │
│      ├── Email con PDF adjunto                                             │
│      └── QR para verificar en DGII                                         │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Estructura del XML e-CF

```xml
<?xml version="1.0" encoding="UTF-8"?>
<ECF xmlns="https://dgii.gov.do/ecf">
  <Encabezado>
    <IdDoc>
      <TipoeCF>31</TipoeCF>
      <eNCF>E310000000001</eNCF>
      <FechaVencimientoSecuencia>2026-12-31</FechaVencimientoSecuencia>
    </IdDoc>
    <Emisor>
      <RNCEmisor>131123456</RNCEmisor>
      <RazonSocialEmisor>OKLA SRL</RazonSocialEmisor>
    </Emisor>
    <Comprador>
      <RNCComprador>101234567</RNCComprador>
      <RazonSocialComprador>Auto Dealer XYZ</RazonSocialComprador>
    </Comprador>
    <Totales>
      <MontoGravadoTotal>8474.58</MontoGravadoTotal>
      <TotalITBIS>1525.42</TotalITBIS>
      <MontoTotal>10000.00</MontoTotal>
    </Totales>
  </Encabezado>
  <DetallesItems>
    <Item>
      <NumeroLinea>1</NumeroLinea>
      <NombreItem>Suscripción Dealer Pro - Enero 2026</NombreItem>
      <CantidadItem>1</CantidadItem>
      <MontoItem>8474.58</MontoItem>
      <MontoITBIS>1525.42</MontoITBIS>
    </Item>
  </DetallesItems>
  <FirmaDigital>...</FirmaDigital>
</ECF>
```

### Requisitos DGII

| Requisito               | Descripción                                           |
| ----------------------- | ----------------------------------------------------- |
| **RNC de OKLA**         | Debe estar registrado en DGII                         |
| **Certificado Digital** | Emitido por DGII para firmar e-CF                     |
| **Secuencia e-CF**      | Autorizada por DGII (rango de números)                |
| **Ambiente**            | Pruebas: ecf-test.dgii.gov.do / Prod: ecf.dgii.gov.do |

### Entregables Sprint 4.5

```
✅ Certificado digital DGII configurado
✅ Secuencia de e-CF autorizada por DGII
✅ Generación de e-CF tipo 31 y 32
✅ Firma digital de XML
✅ Envío automático a DGII post-pago
✅ PDF de factura con e-CF y QR
✅ Campo RNC opcional en checkout
✅ Validación de RNC (dígito verificador)
✅ Monitor de e-CF para admin
✅ Reintentos automáticos de e-CF fallidos
✅ Notas de crédito para reembolsos
```

**Story Points Total:** 63  
**Velocidad esperada:** 55-65 SP

### ⚠️ Dependencias Externas

1. **RNC de empresa:** OKLA debe tener RNC activo
2. **Certificado digital:** Solicitar a DGII (~2-4 semanas)
3. **Secuencia e-CF:** Autorizar rango en DGII (~1 semana)
4. **Ambiente de pruebas:** Solicitar acceso a sandbox DGII

---

## Sprint 4.6 (Semanas 11-12) - ⚖️ Cumplimiento Ley 358-05 (Comercio Electrónico RD)

**Objetivo:** Cumplir con la Ley 358-05 sobre Comercio Electrónico, Documentos y Firmas Digitales

### ⚠️ REQUISITO LEGAL OBLIGATORIO

La Ley 358-05 de República Dominicana regula el comercio electrónico y establece requisitos obligatorios para plataformas como OKLA. El incumplimiento puede resultar en:

- Multas administrativas
- Invalidez jurídica de transacciones
- Responsabilidad civil

### A. INFORMACIÓN OBLIGATORIA DEL PRESTADOR (Art. 18)

**Dónde publicar:** Footer de TODAS las páginas + Sección "Legal"

| Elemento           | Requisito                      | Implementación                         |
| ------------------ | ------------------------------ | -------------------------------------- |
| Razón Social       | Nombre legal completo          | "OKLA S.R.L."                          |
| RNC                | Número de RNC                  | "RNC: 1-31-XXXXX-X"                    |
| Domicilio físico   | Dirección completa (no PO Box) | Calle, Número, Ciudad, Provincia       |
| Teléfono           | Línea de contacto              | "+1 (809) XXX-XXXX"                    |
| Email              | Correo electrónico             | "legal@okla.com.do"                    |
| Registro Mercantil | Si aplica                      | "Inscrita en Registro Mercantil de SD" |

### B. TÉRMINOS Y CONDICIONES ESPECÍFICOS (Art. 19-20)

**Requisitos de la Ley:**

1. **Procedimiento para celebrar contrato** - Pasos claros para completar transacción
2. **Medios para identificar/corregir errores** - Cómo modificar antes de pagar
3. **Idioma del contrato** - Español
4. **Archivo del contrato** - Cómo se archiva y cómo acceder
5. **Códigos de conducta** - Si aplican

### C. INFORMACIÓN PRE-CONTRACTUAL (Art. 19)

**Antes de confirmar cada compra:**

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    PÁGINA DE CHECKOUT - ANTES DE PAGAR                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  📋 RESUMEN DE SU PEDIDO                                                    │
│  ├── Producto: Publicación de Vehículo - 30 días                           │
│  ├── Subtotal: RD$ 1,230.51                                                │
│  ├── ITBIS (18%): RD$ 221.49                                               │
│  └── TOTAL: RD$ 1,452.00                                                   │
│                                                                             │
│  ℹ️ INFORMACIÓN IMPORTANTE:                                                 │
│  • Puede modificar su pedido usando el botón "Volver"                      │
│  • Al hacer clic en "Pagar" acepta los Términos y Condiciones              │
│  • Este contrato se archivará y podrá consultarlo en "Mis Pedidos"         │
│  • Conservaremos este documento por 10 años según Ley 358-05               │
│                                                                             │
│  ☐ He leído y acepto los [Términos y Condiciones] y la                     │
│    [Política de Privacidad]                                                │
│                                                                             │
│  [← Volver]                              [Pagar RD$ 1,452.00 →]            │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### D. CONFIRMACIÓN DEL CONTRATO (Art. 21)

**Email automático obligatorio post-compra:**

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    EMAIL DE CONFIRMACIÓN                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Asunto: ✅ Confirmación de Compra #OKLA-2026-00001                         │
│                                                                             │
│  Estimado/a [Nombre],                                                       │
│                                                                             │
│  Confirmamos la recepción de su pedido:                                    │
│                                                                             │
│  📋 DETALLES DEL CONTRATO:                                                  │
│  • Número de Pedido: OKLA-2026-00001                                       │
│  • Fecha: 9 de enero de 2026, 14:30 hrs                                    │
│  • Producto: Publicación de Vehículo - 30 días                             │
│  • Total pagado: RD$ 1,452.00                                              │
│                                                                             │
│  📄 DOCUMENTOS ADJUNTOS:                                                    │
│  • Factura con e-CF (PDF)                                                  │
│  • Términos y Condiciones aceptados (PDF)                                  │
│                                                                             │
│  🔗 ACCESO A SU CONTRATO:                                                   │
│  Este contrato ha sido archivado y puede consultarlo en cualquier          │
│  momento en: [Ver en Mis Pedidos]                                          │
│                                                                             │
│  Conservaremos este documento por 10 años según la Ley 358-05.             │
│                                                                             │
│  Atentamente,                                                               │
│  Equipo OKLA                                                                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### E. CONSERVACIÓN DE DOCUMENTOS (Art. 14)

**Requisito:** Guardar contratos electrónicos íntegros por **10 años**

### Backend Tasks

| Task                                    | Servicio               | Story Points |
| --------------------------------------- | ---------------------- | ------------ |
| LegalComplianceService base             | Nuevo servicio         | 5            |
| Entidad Contract (pedidos archivados)   | LegalComplianceService | 5            |
| Hash criptográfico SHA-256 por contrato | LegalComplianceService | 3            |
| Tabla orders_audit_log (inmutable)      | LegalComplianceService | 5            |
| API: obtener contrato archivado         | LegalComplianceService | 3            |
| Política de retención 10 años (S3)      | Infrastructure         | 3            |
| Email de confirmación Art. 21           | NotificationService    | 5            |
| PDF de T&C aceptados (versioned)        | LegalComplianceService | 3            |
| Registro de aceptación (timestamp, IP)  | LegalComplianceService | 3            |
| Backup automático a S3 Glacier          | Infrastructure         | 3            |

### Entidad Contract

```csharp
public class Contract
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? OrderId { get; private set; }
    public Guid? SubscriptionId { get; private set; }

    // Datos del contrato
    public string ContractNumber { get; private set; }  // OKLA-2026-00001
    public string ContractType { get; private set; }    // "listing_purchase", "subscription"
    public DateTime ContractDate { get; private set; }

    // Contenido archivado (inmutable)
    public string ContractContentJson { get; private set; }  // Snapshot completo
    public string ContractHash { get; private set; }         // SHA-256
    public string TermsVersionId { get; private set; }       // Versión de T&C aceptados

    // Registro de aceptación
    public DateTime AcceptedAt { get; private set; }
    public string AcceptedFromIP { get; private set; }
    public string UserAgent { get; private set; }

    // Acceso
    public string PdfUrl { get; private set; }               // S3 URL
    public DateTime ExpiresAt { get; private set; }          // ContractDate + 10 años

    public DateTime CreatedAt { get; private set; }
}
```

### Tabla de Auditoría (Inmutable)

```sql
CREATE TABLE orders_audit_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id UUID NOT NULL,
    action VARCHAR(50) NOT NULL,  -- 'created', 'paid', 'cancelled'
    data_snapshot JSONB NOT NULL,
    hash_sha256 VARCHAR(64) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    ip_address VARCHAR(45),
    user_agent TEXT
);

-- Trigger para prevenir UPDATE/DELETE
CREATE OR REPLACE FUNCTION prevent_audit_modification()
RETURNS TRIGGER AS $$
BEGIN
    RAISE EXCEPTION 'Audit log cannot be modified (Ley 358-05 compliance)';
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER no_update_audit BEFORE UPDATE ON orders_audit_log
    FOR EACH ROW EXECUTE FUNCTION prevent_audit_modification();

CREATE TRIGGER no_delete_audit BEFORE DELETE ON orders_audit_log
    FOR EACH ROW EXECUTE FUNCTION prevent_audit_modification();
```

### Frontend Tasks

| Task                                | Componente              | Story Points |
| ----------------------------------- | ----------------------- | ------------ |
| Footer legal completo               | LegalFooter             | 5            |
| Página "Información Legal"          | LegalInfoPage           | 5            |
| Términos y Condiciones (versioned)  | TermsPage               | 5            |
| Política de Privacidad              | PrivacyPolicyPage       | 5            |
| Checkbox de aceptación en checkout  | AcceptTermsCheckbox     | 3            |
| Resumen pre-contractual en checkout | PreContractualSummary   | 5            |
| Sección "Mis Contratos" en perfil   | MyContractsPage         | 5            |
| Descargar contrato PDF              | ContractDownload        | 3            |
| Página de confirmación post-pago    | PaymentConfirmationPage | 3            |

### Footer Legal Requerido

```tsx
// LegalFooter.tsx
const LegalFooter = () => (
  <footer className="bg-gray-900 text-white py-8">
    <div className="container mx-auto">
      {/* ... otros links ... */}

      <div className="border-t border-gray-700 mt-6 pt-6">
        <div className="text-sm text-gray-400">
          <p className="font-semibold text-white">OKLA S.R.L.</p>
          <p>RNC: 1-31-XXXXX-X</p>
          <p>Av. Winston Churchill #123, Piantini</p>
          <p>Santo Domingo, República Dominicana</p>
          <p>Tel: +1 (809) XXX-XXXX | Email: legal@okla.com.do</p>
          <p className="mt-2">
            Inscrita en el Registro Mercantil de Santo Domingo
          </p>
        </div>

        <div className="mt-4 flex flex-wrap gap-4 text-sm">
          <Link to="/legal/terminos">Términos y Condiciones</Link>
          <Link to="/legal/privacidad">Política de Privacidad</Link>
          <Link to="/legal/cookies">Política de Cookies</Link>
          <Link to="/legal/comercio-electronico">Comercio Electrónico</Link>
        </div>

        <p className="mt-4 text-xs text-gray-500">
          © 2026 OKLA S.R.L. Todos los derechos reservados. Operamos en
          cumplimiento de la Ley 358-05 sobre Comercio Electrónico.
        </p>
      </div>
    </div>
  </footer>
);
```

### Documentos Legales Requeridos

| Documento                          | Contenido Principal                              | Versión |
| ---------------------------------- | ------------------------------------------------ | ------- |
| **Términos y Condiciones**         | Contrato de uso, responsabilidades, jurisdicción | v1.0    |
| **Política de Privacidad**         | Tratamiento de datos, derechos ARCO              | v1.0    |
| **Política de Cookies**            | Tipos de cookies, consentimiento                 | v1.0    |
| **Aviso de Comercio Electrónico**  | Info Ley 358-05, derechos del consumidor         | v1.0    |
| **Condiciones de Venta**           | Para vendedores individuales                     | v1.0    |
| **Contrato de Suscripción Dealer** | Para dealers                                     | v1.0    |

### Flujo de Aceptación de T&C

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    FLUJO DE ACEPTACIÓN LEY 358-05                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1️⃣ USUARIO EN CHECKOUT                                                     │
│      ↓                                                                      │
│  2️⃣ MOSTRAR RESUMEN PRE-CONTRACTUAL                                         │
│      ├── Detalles del pedido                                               │
│      ├── Precio con ITBIS desglosado                                       │
│      ├── Cómo modificar (botón "Volver")                                   │
│      └── Cómo acceder después ("Mis Pedidos")                              │
│      ↓                                                                      │
│  3️⃣ CHECKBOX OBLIGATORIO                                                    │
│      └── "He leído y acepto los T&C y Política de Privacidad"              │
│      ↓                                                                      │
│  4️⃣ REGISTRO DE ACEPTACIÓN                                                  │
│      ├── Timestamp (UTC)                                                   │
│      ├── IP del usuario                                                    │
│      ├── User Agent                                                        │
│      ├── Versión de T&C aceptados                                          │
│      └── Hash del contenido aceptado                                       │
│      ↓                                                                      │
│  5️⃣ PROCESAR PAGO                                                           │
│      ↓                                                                      │
│  6️⃣ CREAR CONTRATO ARCHIVADO                                                │
│      ├── Snapshot completo del pedido                                      │
│      ├── Hash SHA-256                                                      │
│      ├── PDF generado                                                      │
│      └── Guardado en S3 (retención 10 años)                                │
│      ↓                                                                      │
│  7️⃣ ENVIAR CONFIRMACIÓN (Art. 21)                                           │
│      ├── Email con resumen                                                 │
│      ├── Adjuntar factura e-CF                                             │
│      ├── Adjuntar T&C aceptados                                            │
│      └── Link a "Mis Contratos"                                            │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Entregables Sprint 4.6

```
✅ Footer legal en todas las páginas (razón social, RNC, dirección)
✅ Página "Información Legal" completa
✅ Términos y Condiciones versionados
✅ Política de Privacidad
✅ Política de Cookies con banner de consentimiento
✅ Resumen pre-contractual obligatorio en checkout
✅ Checkbox de aceptación obligatorio
✅ Registro de aceptación (timestamp, IP, versión T&C)
✅ Email de confirmación Art. 21 automático
✅ Sección "Mis Contratos" en perfil de usuario
✅ Contratos archivados por 10 años (S3 Glacier)
✅ Hash criptográfico de cada contrato
✅ Tabla de auditoría inmutable
✅ PDF de contrato descargable
```

**Story Points Total:** 77  
**Velocidad esperada:** 65-80 SP

### ⚠️ Documentos Legales Requeridos (Preparar con Abogado)

Antes de lanzar, un abogado especializado en comercio electrónico RD debe preparar:

1. ✅ Términos y Condiciones (adaptados a marketplace vehicular)
2. ✅ Política de Privacidad (cumplimiento GDPR-like RD)
3. ✅ Aviso Legal de Comercio Electrónico
4. ✅ Contrato de Vendedor Individual
5. ✅ Contrato de Suscripción Dealer
6. ✅ Política de Devoluciones/Cancelaciones

---

## 🎉 MILESTONE: MVP MARKETPLACE COMPLETO

**Fecha estimada:** Semana 8 (2 meses desde inicio)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          MVP MARKETPLACE ✅                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  👤 COMPRADORES pueden:                                                      │
│  ├── ✅ Buscar vehículos con filtros avanzados                              │
│  ├── ✅ Ver detalle con galería de fotos                                    │
│  ├── ✅ Guardar favoritos                                                   │
│  ├── ✅ Contactar vendedores                                                │
│  └── ✅ Ver perfil público del vendedor                                     │
│                                                                              │
│  🚗 VENDEDORES INDIVIDUALES pueden:                                          │
│  ├── ✅ Publicar vehículos (wizard 5 pasos)                                 │
│  ├── ✅ Subir hasta 20 fotos                                                │
│  ├── ✅ Pagar por publicación ($29)                                         │
│  ├── ✅ Recibir consultas por email                                         │
│  ├── ✅ Responder a compradores                                             │
│  └── ✅ Gestionar sus publicaciones                                         │
│                                                                              │
│  💰 MONETIZACIÓN:                                                            │
│  └── ✅ $29 por publicación (Stripe)                                        │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

# 📅 FASE 2: DEALERS BÁSICO

## Sprint 5 (Semanas 9-10) - Cuentas de Dealer

**Objetivo:** Dealers pueden registrarse y suscribirse

### Backend

| Task                                       | Servicio                | Story Points |
| ------------------------------------------ | ----------------------- | ------------ |
| DealerManagementService: CRUD dealers      | Nuevo servicio          | 8            |
| Modelo de dealer (nombre, RNC, sucursales) | DealerManagementService | 5            |
| Verificación de dealer (manual/docs)       | DealerManagementService | 5            |
| BillingService: suscripciones Stripe       | BillingService          | 8            |
| 3 planes: Starter/Pro/Enterprise           | BillingService          | 5            |
| Webhooks de suscripción                    | BillingService          | 5            |

### Frontend

| Task                             | Componente             | Story Points |
| -------------------------------- | ---------------------- | ------------ |
| Landing page para dealers        | DealerLandingPage      | 8            |
| Página de planes y pricing       | DealerPricingPage      | 5            |
| Registro de dealer (formulario)  | DealerRegistrationForm | 5            |
| Upload de documentos (RNC, etc.) | DocumentUpload         | 3            |
| Checkout de suscripción          | SubscriptionCheckout   | 5            |
| Dashboard de dealer (básico)     | DealerDashboard        | 8            |

### Entregables Sprint 5

```
✅ Dealer puede registrarse con datos de empresa
✅ Dealer puede subir documentos de verificación
✅ Admin puede aprobar/rechazar dealers
✅ Dealer puede ver planes (Starter $49/Pro $129/Enterprise $299)
✅ Dealer puede suscribirse mensualmente
✅ Dealer tiene acceso a dashboard básico
```

**Story Points Total:** 70  
**Velocidad esperada:** 55-65 SP (sprint más pesado)

---

## Sprint 6 (Semanas 11-12) - Inventario de Dealer

**Objetivo:** Dealers pueden gestionar su inventario

### Backend

| Task                                   | Servicio                   | Story Points |
| -------------------------------------- | -------------------------- | ------------ |
| InventoryManagementService base        | Nuevo servicio             | 8            |
| Bulk upload (CSV/Excel)                | InventoryManagementService | 8            |
| Edición en batch                       | InventoryManagementService | 5            |
| Sincronización con VehiclesSaleService | InventoryManagementService | 5            |
| Límites por plan (15/50/ilimitado)     | InventoryManagementService | 3            |

### Frontend

| Task                                        | Componente      | Story Points |
| ------------------------------------------- | --------------- | ------------ |
| Tabla de inventario con filtros             | InventoryTable  | 8            |
| Acciones en batch (activar/pausar/eliminar) | BatchActions    | 5            |
| Import CSV/Excel                            | BulkImportModal | 8            |
| Export de inventario                        | ExportInventory | 3            |
| Vista de límite de listings                 | LimitIndicator  | 2            |
| Quick-edit inline                           | InlineEdit      | 5            |

### Entregables Sprint 6

```
✅ Dealer puede ver tabla de todo su inventario
✅ Dealer puede importar vehículos desde CSV/Excel
✅ Dealer puede editar múltiples vehículos a la vez
✅ Dealer puede activar/pausar/eliminar en batch
✅ Dealer puede exportar inventario
✅ Sistema respeta límites según plan
```

**Story Points Total:** 60  
**Velocidad esperada:** 50-60 SP

---

## Sprint 7 (Semanas 13-14) - Perfil Público de Dealer

**Objetivo:** Dealers tienen presencia profesional en el marketplace

### Backend

| Task                                    | Servicio                | Story Points |
| --------------------------------------- | ----------------------- | ------------ |
| DealerManagementService: perfil público | DealerManagementService | 5            |
| Sucursales con ubicación/horario        | DealerManagementService | 5            |
| Galería de fotos del dealer             | MediaService            | 3            |
| SEO metadata para dealers               | VehiclesSaleService     | 3            |
| Verificación "Trusted Dealer" badge     | DealerManagementService | 3            |

### Frontend

| Task                                       | Componente         | Story Points |
| ------------------------------------------ | ------------------ | ------------ |
| Página pública del dealer                  | DealerPublicPage   | 8            |
| Header con logo y banner                   | DealerHeader       | 3            |
| Grid de vehículos del dealer               | DealerVehiclesGrid | 5            |
| Mapa con sucursales                        | DealerLocationsMap | 5            |
| Horarios de atención                       | BusinessHours      | 2            |
| Botones de contacto (tel, WhatsApp, email) | ContactButtons     | 3            |
| Editor de perfil (dealer dashboard)        | ProfileEditor      | 5            |

### Entregables Sprint 7

```
✅ Dealer tiene página pública profesional
✅ Página muestra logo, banner, descripción
✅ Compradores ven todos los vehículos del dealer
✅ Mapa muestra ubicación de sucursales
✅ Dealers verificados tienen badge "Trusted"
✅ Dealer puede editar su perfil desde dashboard
```

**Story Points Total:** 50  
**Velocidad esperada:** 45-55 SP

---

## Sprint 8 (Semanas 15-16) - Estadísticas Básicas para Dealers

**Objetivo:** Dealers ven métricas de su performance

### Backend

| Task                            | Servicio                | Story Points |
| ------------------------------- | ----------------------- | ------------ |
| ListingAnalyticsService base    | Nuevo servicio          | 8            |
| Tracking de vistas por vehículo | ListingAnalyticsService | 5            |
| Tracking de contactos/leads     | ListingAnalyticsService | 5            |
| Agregaciones diarias/semanales  | ListingAnalyticsService | 5            |
| API de métricas para dashboard  | ListingAnalyticsService | 5            |

### Frontend

| Task                             | Componente        | Story Points |
| -------------------------------- | ----------------- | ------------ |
| Dashboard con KPIs principales   | MetricsDashboard  | 8            |
| Gráfico de vistas en el tiempo   | ViewsChart        | 5            |
| Top 5 vehículos más vistos       | TopVehiclesWidget | 3            |
| Indicadores de contactos/leads   | LeadsWidget       | 3            |
| Comparación con período anterior | PeriodComparison  | 3            |
| Estadísticas por vehículo        | VehicleStatsRow   | 3            |

### Entregables Sprint 8

```
✅ Dealer ve total de vistas del mes
✅ Dealer ve total de contactos/leads
✅ Dealer ve gráfico de tendencias
✅ Dealer ve cuáles vehículos tienen más interés
✅ Dealer puede comparar con mes anterior
✅ Cada vehículo muestra sus estadísticas individuales
```

**Story Points Total:** 53  
**Velocidad esperada:** 45-55 SP

---

## 🎉 MILESTONE: DEALERS BÁSICO COMPLETO

**Fecha estimada:** Semana 16 (4 meses desde inicio)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        DEALERS BÁSICO ✅                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  🏢 DEALERS pueden:                                                          │
│  ├── ✅ Registrarse y verificarse                                           │
│  ├── ✅ Suscribirse a plan mensual ($49/$129/$299)                          │
│  ├── ✅ Gestionar inventario completo                                       │
│  ├── ✅ Importar vehículos desde CSV/Excel                                  │
│  ├── ✅ Editar en batch                                                     │
│  ├── ✅ Tener página pública profesional                                    │
│  ├── ✅ Ver estadísticas de vistas y contactos                              │
│  └── ✅ Badge "Trusted Dealer"                                              │
│                                                                              │
│  💰 MONETIZACIÓN:                                                            │
│  ├── ✅ $29 por listing (vendedores individuales)                           │
│  └── ✅ $49-$299/mes suscripción dealers                                    │
│                                                                              │
│  📊 NUEVOS SERVICIOS:                                                        │
│  ├── ✅ DealerManagementService (5039)                                      │
│  ├── ✅ InventoryManagementService (5040)                                   │
│  └── ✅ ListingAnalyticsService (5058)                                      │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

# 📅 FASE 3: DATA & ANALYTICS

## Sprint 9 (Semanas 17-18) - Event Tracking ✅ COMPLETADO

**Objetivo:** Capturar todas las acciones de usuarios  
**Fecha completado:** Enero 9, 2026

> ✅ **IMPLEMENTADO:**  
> El SDK `okla-analytics.js` y el backend `EventTrackingService` están funcionando.
>
> - Endpoints disponibles: `/api/events/track`, `/api/events/track/batch`
> - Repositorio InMemory para desarrollo (ClickHouse para producción)
> - SDK habilitado (`enabled: true`) en el frontend
> - Gateway configurado con rutas para EventTrackingService

### Backend ✅

| Task                      | Servicio             | Story Points | Estado |
| ------------------------- | -------------------- | ------------ | ------ |
| EventTrackingService base | Nuevo servicio       | 8            | ✅     |
| InMemory Repository       | EventTrackingService | 5            | ✅     |
| API de ingesta de eventos | EventTrackingService | 5            | ✅     |
| **Gateway routes config** | Gateway              | 2            | ✅     |

### Frontend ✅

| Task                         | Componente        | Story Points | Estado |
| ---------------------------- | ----------------- | ------------ | ------ |
| SDK de tracking (JS library) | okla-analytics.js | 8            | ✅     |
| Auto-track de page views     | AutoTrack         | 3            | ✅     |
| Track de clicks importantes  | ClickTrack        | 3            | ✅     |
| Track de búsquedas y filtros | SearchTrack       | 3            | ✅     |
| Track de tiempo en página    | TimeOnPageTrack   | 3            | ✅     |
| **Habilitar SDK (enabled)**  | okla-analytics.js | 1            | ✅     |

### Entregables Sprint 9 ✅

```
✅ Sistema captura todas las page views
✅ Sistema captura búsquedas realizadas
✅ Sistema captura filtros aplicados
✅ Sistema captura tiempo en cada vehículo
✅ Sistema captura favoritos y contactos
✅ Eventos almacenados en memoria (ClickHouse para producción)
```

**Story Points Total:** 38 (completados)  
**Velocidad real:** 38 SP

---

## Sprint 10 (Semanas 19-20) - User Behavior & Features

**Objetivo:** Entender comportamiento de usuarios

### Backend

| Task                              | Servicio            | Story Points |
| --------------------------------- | ------------------- | ------------ |
| UserBehaviorService base          | Nuevo servicio      | 8            |
| Perfil de preferencias inferidas  | UserBehaviorService | 5            |
| Historial de acciones por usuario | UserBehaviorService | 5            |
| FeatureStoreService base          | Nuevo servicio      | 8            |
| Features de usuarios              | FeatureStoreService | 5            |
| Features de vehículos             | FeatureStoreService | 5            |

### Tareas de Data

| Task                      | Servicio            | Story Points |
| ------------------------- | ------------------- | ------------ |
| ETL de eventos a features | DataPipelineService | 8            |
| Agregaciones diarias      | DataPipelineService | 5            |
| Segmentación de usuarios  | UserBehaviorService | 5            |

### Entregables Sprint 10

```
✅ Sistema infiere preferencias (SUV, Toyota, <$30k)
✅ Sistema segmenta usuarios (comprador serio, browser, etc.)
✅ Feature store con features de usuarios
✅ Feature store con features de vehículos
✅ Pipeline de ETL funcionando
```

**Story Points Total:** 54  
**Velocidad esperada:** 45-55 SP

---

## Sprint 11 (Semanas 21-22) - Lead Scoring

**Objetivo:** Identificar leads HOT para dealers

### Backend

| Task                                 | Servicio            | Story Points |
| ------------------------------------ | ------------------- | ------------ |
| LeadScoringService base              | Nuevo servicio      | 8            |
| Modelo de scoring (reglas iniciales) | LeadScoringService  | 8            |
| Integración con eventos              | LeadScoringService  | 5            |
| API de leads por dealer              | LeadScoringService  | 5            |
| Notificaciones de leads HOT          | NotificationService | 3            |

### Frontend

| Task                                | Componente       | Story Points |
| ----------------------------------- | ---------------- | ------------ |
| Widget de leads en dashboard dealer | LeadsWidget      | 5            |
| Lista de leads con score            | LeadsList        | 5            |
| Detalle de lead (historial)         | LeadDetail       | 5            |
| Indicador visual HOT/WARM/COLD      | LeadScoreBadge   | 2            |
| Notificación push de lead HOT       | PushNotification | 3            |

### Entregables Sprint 11

```
✅ Sistema calcula score de cada lead (0-100)
✅ Leads clasificados como HOT/WARM/COLD
✅ Dealers ven lista de leads ordenada por score
✅ Dealers reciben notificación de leads HOT
✅ Dealers ven historial de acciones del lead
```

**Story Points Total:** 49  
**Velocidad esperada:** 45-55 SP

---

## Sprint 12 (Semanas 23-24) - Dashboard Avanzado

**Objetivo:** Analytics completos para dealers

### Backend

| Task                               | Servicio               | Story Points |
| ---------------------------------- | ---------------------- | ------------ |
| DealerAnalyticsService base        | Nuevo servicio         | 8            |
| Métricas de conversión             | DealerAnalyticsService | 5            |
| Comparación con competencia (anon) | DealerAnalyticsService | 5            |
| Reportes exportables               | DealerAnalyticsService | 5            |
| Insights automáticos               | DealerAnalyticsService | 5            |

### Frontend

| Task                        | Componente        | Story Points |
| --------------------------- | ----------------- | ------------ |
| Dashboard rediseñado        | AdvancedDashboard | 8            |
| Funnel de conversión visual | ConversionFunnel  | 5            |
| Benchmark vs mercado        | MarketBenchmark   | 5            |
| Insights/recomendaciones    | InsightsCard      | 5            |
| Export PDF/Excel            | ReportExport      | 3            |

### Entregables Sprint 12

```
✅ Dashboard con métricas avanzadas
✅ Funnel: Vistas → Contactos → Test Drives → Ventas
✅ Comparación anónima con otros dealers
✅ Insights: "Tu Toyota está 10% arriba del mercado"
✅ Export de reportes en PDF/Excel
```

**Story Points Total:** 54  
**Velocidad esperada:** 45-55 SP

---

## 🎉 MILESTONE: DATA & ANALYTICS COMPLETO

**Fecha estimada:** Semana 24 (6 meses desde inicio)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     DATA & ANALYTICS ✅                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  📊 TRACKING:                                                                │
│  ├── ✅ Todas las acciones de usuarios capturadas                           │
│  ├── ✅ Perfiles de comportamiento inferidos                                │
│  └── ✅ Feature store para ML                                               │
│                                                                              │
│  🔥 LEAD SCORING:                                                            │
│  ├── ✅ Score 0-100 para cada lead                                          │
│  ├── ✅ Clasificación HOT/WARM/COLD                                         │
│  └── ✅ Notificaciones de leads HOT                                         │
│                                                                              │
│  📈 ANALYTICS DEALERS:                                                       │
│  ├── ✅ Dashboard con todas las métricas                                    │
│  ├── ✅ Funnel de conversión                                                │
│  ├── ✅ Benchmark vs mercado                                                │
│  └── ✅ Insights automáticos                                                │
│                                                                              │
│  🆕 NUEVOS SERVICIOS:                                                        │
│  ├── ✅ EventTrackingService (5050)                                         │
│  ├── ✅ DataPipelineService (5051)                                          │
│  ├── ✅ UserBehaviorService (5052)                                          │
│  ├── ✅ FeatureStoreService (5053)                                          │
│  ├── ✅ LeadScoringService (5055)                                           │
│  └── ✅ DealerAnalyticsService (5041)                                       │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

# 📅 FASE 4: IA & DIFERENCIACIÓN

## Sprint 13 (Semanas 25-26) - Recomendaciones

**Objetivo:** "Vehículos para ti" personalizados

### Backend

| Task                                    | Servicio              | Story Points |
| --------------------------------------- | --------------------- | ------------ |
| RecommendationService base              | Nuevo servicio        | 8            |
| Modelo de recomendación (collaborative) | RecommendationService | 8            |
| "Vehículos similares"                   | RecommendationService | 5            |
| "Usuarios también vieron"               | RecommendationService | 5            |
| Cache de recomendaciones (Redis)        | RecommendationService | 3            |

### Frontend

| Task                             | Componente          | Story Points |
| -------------------------------- | ------------------- | ------------ |
| Sección "Para ti" en homepage    | ForYouSection       | 5            |
| Carrusel de similares en detalle | SimilarVehicles     | 5            |
| "También vieron" en detalle      | AlsoViewed          | 3            |
| Email de recomendaciones         | RecommendationEmail | 5            |

### Entregables Sprint 13

```
✅ Homepage muestra vehículos personalizados
✅ Detalle muestra vehículos similares
✅ Detalle muestra "usuarios también vieron"
✅ Email semanal con recomendaciones
```

**Story Points Total:** 47  
**Velocidad esperada:** 45-55 SP

---

## Sprint 14 (Semanas 27-28) - Reviews Básico

**Objetivo:** Sistema de reviews estilo Amazon

### Backend

| Task                           | Servicio       | Story Points |
| ------------------------------ | -------------- | ------------ |
| ReviewService base             | Nuevo servicio | 8            |
| CRUD de reviews                | ReviewService  | 5            |
| Rating summary por vendedor    | ReviewService  | 5            |
| Validación "compra verificada" | ReviewService  | 5            |
| Moderación básica              | ReviewService  | 3            |

### Frontend

| Task                                  | Componente         | Story Points |
| ------------------------------------- | ------------------ | ------------ |
| Sección de reviews en perfil vendedor | ReviewsSection     | 8            |
| Formulario de review                  | ReviewForm         | 5            |
| Rating con estrellas                  | StarRating         | 3            |
| Distribución de ratings               | RatingDistribution | 3            |
| Badge "Compra verificada"             | VerifiedBadge      | 2            |

### Entregables Sprint 14

```
✅ Compradores pueden dejar reviews
✅ Rating 1-5 estrellas + texto
✅ Badge de compra verificada
✅ Vendedor ve rating promedio
✅ Distribución visual de ratings
```

**Story Points Total:** 47  
**Velocidad esperada:** 45-55 SP

---

## Sprint 15 (Semanas 29-30) - Reviews Avanzado

**Objetivo:** Reviews completo con respuestas y votos

### Backend

| Task                             | Servicio      | Story Points |
| -------------------------------- | ------------- | ------------ |
| Respuestas de vendedor a reviews | ReviewService | 5            |
| Votos de utilidad                | ReviewService | 3            |
| Sistema de badges                | ReviewService | 5            |
| Solicitud automática de review   | ReviewService | 5            |
| Anti-spam y fraude               | ReviewService | 5            |

### Frontend

| Task                          | Componente         | Story Points |
| ----------------------------- | ------------------ | ------------ |
| Respuesta del vendedor UI     | SellerResponse     | 3            |
| Botón "¿Te resultó útil?"     | HelpfulVote        | 3            |
| Badges en perfil              | BadgeDisplay       | 3            |
| Modal de solicitud de review  | ReviewRequestModal | 5            |
| Filtrar reviews por estrellas | ReviewFilters      | 3            |

### Entregables Sprint 15

```
✅ Vendedor puede responder reviews
✅ Usuarios pueden votar reviews útiles
✅ Badges: "Top Rated", "Trusted Dealer"
✅ Solicitud automática 7 días después de compra
✅ Sistema anti-fraude de reviews
```

**Story Points Total:** 40  
**Velocidad esperada:** 35-45 SP

---

## Sprint 16 (Semanas 31-32) - Chatbot MVP

**Objetivo:** Chatbot básico con OpenAI

### Backend

| Task                           | Servicio       | Story Points |
| ------------------------------ | -------------- | ------------ |
| ChatbotService base            | Nuevo servicio | 8            |
| Integración OpenAI GPT-4o-mini | ChatbotService | 8            |
| SignalR para real-time         | ChatbotService | 5            |
| Contexto del vehículo en chat  | ChatbotService | 5            |
| Historial de conversaciones    | ChatbotService | 3            |

### Frontend

| Task                      | Componente      | Story Points |
| ------------------------- | --------------- | ------------ |
| Widget de chat flotante   | ChatWidget      | 8            |
| Interfaz de conversación  | ChatInterface   | 5            |
| Indicador de typing       | TypingIndicator | 2            |
| Botón de cerrar/minimizar | ChatControls    | 2            |

### Entregables Sprint 16

```
✅ Widget de chat en páginas de vehículos
✅ Chatbot responde preguntas del vehículo
✅ Conversación en tiempo real (SignalR)
✅ Contexto del vehículo actual
✅ Historial de conversación
```

**Story Points Total:** 46  
**Velocidad esperada:** 40-50 SP

---

## Sprint 17 (Semanas 33-34) - Chatbot con Lead Scoring

**Objetivo:** Chatbot califica leads y transfiere a WhatsApp

### Backend

| Task                               | Servicio       | Story Points |
| ---------------------------------- | -------------- | ------------ |
| RAG con Pinecone                   | ChatbotService | 8            |
| Análisis de intención de compra    | ChatbotService | 8            |
| Integración con LeadScoringService | ChatbotService | 5            |
| Integración WhatsApp (Twilio)      | ChatbotService | 8            |
| Handoff a vendedor                 | ChatbotService | 5            |

### Frontend

| Task                              | Componente         | Story Points |
| --------------------------------- | ------------------ | ------------ |
| Botón "Hablar con vendedor"       | TransferButton     | 3            |
| Transición a WhatsApp             | WhatsAppHandoff    | 5            |
| Indicador de lead score (interno) | LeadScoreIndicator | 2            |

### Entregables Sprint 17

```
✅ Chatbot responde con info específica del vehículo (RAG)
✅ Sistema detecta intención de compra
✅ Lead clasificado como HOT/WARM/COLD
✅ Lead HOT transferido a WhatsApp automáticamente
✅ Vendedor recibe contexto de la conversación
```

**Story Points Total:** 44  
**Velocidad esperada:** 40-50 SP

---

## Sprint 18 (Semanas 35-36) - Pricing Inteligente

**Objetivo:** IA sugiere precio óptimo

### Backend

| Task                            | Servicio                   | Story Points |
| ------------------------------- | -------------------------- | ------------ |
| VehicleIntelligenceService base | Nuevo servicio             | 8            |
| Modelo de pricing (XGBoost)     | VehicleIntelligenceService | 8            |
| Predicción de demanda           | VehicleIntelligenceService | 8            |
| Tiempo estimado de venta        | VehicleIntelligenceService | 5            |
| API de sugerencias              | VehicleIntelligenceService | 3            |

### Frontend

| Task                                | Componente       | Story Points |
| ----------------------------------- | ---------------- | ------------ |
| Widget de precio sugerido (publish) | PriceSuggestion  | 5            |
| Indicador vs mercado                | MarketComparison | 3            |
| Tips para vender más rápido         | SellingTips      | 3            |
| Predicción de tiempo de venta       | TimeToSell       | 3            |

### Entregables Sprint 18

```
✅ Vendedor ve precio sugerido al publicar
✅ Indicador: "Tu precio está 10% arriba del mercado"
✅ Predicción: "Este vehículo se venderá en ~18 días"
✅ Tips para mejorar el listing
✅ Dealers ven demanda por categoría
```

**Story Points Total:** 46  
**Velocidad esperada:** 40-50 SP

---

## 🎉 MILESTONE: MARKETPLACE 100% COMPLETO

**Fecha estimada:** Semana 36 (9 meses desde inicio)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                   🚀 OKLA MARKETPLACE 100% ✅                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  👤 COMPRADORES:                                                             │
│  ├── ✅ Búsqueda avanzada con filtros                                       │
│  ├── ✅ Favoritos y guardados                                               │
│  ├── ✅ Contactar vendedores                                                │
│  ├── ✅ Recomendaciones personalizadas                                      │
│  ├── ✅ Reviews de vendedores                                               │
│  └── ✅ Chatbot 24/7 para preguntas                                         │
│                                                                              │
│  🚗 VENDEDORES INDIVIDUALES:                                                 │
│  ├── ✅ Publicar con wizard de 5 pasos                                      │
│  ├── ✅ Pago por listing ($29)                                              │
│  ├── ✅ Precio sugerido por IA                                              │
│  ├── ✅ Estadísticas de vistas                                              │
│  ├── ✅ Reviews y reputación                                                │
│  └── ✅ Leads pre-calificados                                               │
│                                                                              │
│  🏢 DEALERS:                                                                 │
│  ├── ✅ Suscripción mensual ($49/$129/$299)                                 │
│  ├── ✅ Gestión de inventario completo                                      │
│  ├── ✅ Import/export masivo                                                │
│  ├── ✅ Página pública profesional                                          │
│  ├── ✅ Dashboard con todas las métricas                                    │
│  ├── ✅ Lead scoring (HOT/WARM/COLD)                                        │
│  ├── ✅ Chatbot con transferencia a WhatsApp                                │
│  ├── ✅ Pricing inteligente                                                 │
│  └── ✅ Badges y reputación                                                 │
│                                                                              │
│  🤖 INTELIGENCIA ARTIFICIAL:                                                 │
│  ├── ✅ Recomendaciones personalizadas                                      │
│  ├── ✅ Lead scoring automático                                             │
│  ├── ✅ Chatbot con GPT-4                                                   │
│  ├── ✅ Pricing óptimo sugerido                                             │
│  └── ✅ Predicción de demanda                                               │
│                                                                              │
│  📊 SERVICIOS TOTALES: 17 nuevos microservicios                              │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

# 📊 RESUMEN EJECUTIVO

## Timeline Visual

```
MES 1    MES 2    MES 3    MES 4    MES 5    MES 6    MES 7    MES 8    MES 9
─────────────────────────────────────────────────────────────────────────────
S1  S2   S3  S4   S5  S6   S7  S8   S9  S10  S11 S12  S13 S14  S15 S16  S17 S18
███████████████   ███████████████   ████████████████   ████████████████████████
    FASE 1            FASE 2              FASE 3              FASE 4
   MVP MARKETPLACE   DEALERS BÁSICO    DATA & ANALYTICS    IA & DIFERENCIACIÓN

   🎯 Semana 8       🎯 Semana 16      🎯 Semana 24        🎯 Semana 36
   MVP Live!         Dealers Live!     Analytics Live!     100% Complete!
```

## Métricas por Fase

| Fase                    | Sprints | Semanas | Story Points | Servicios Nuevos                                              |
| ----------------------- | ------- | ------- | ------------ | ------------------------------------------------------------- |
| 1 - MVP Marketplace     | 1-4     | 1-8     | ~209         | 0 (mejoras a existentes)                                      |
| 2 - Dealers Básico      | 5-8     | 9-16    | ~233         | 3 (Dealer, Inventory, ListingAnalytics)                       |
| 3 - Data & Analytics    | 9-12    | 17-24   | ~206         | 6 (Event, Pipeline, Behavior, Feature, Lead, DealerAnalytics) |
| 4 - IA & Diferenciación | 13-18   | 25-36   | ~270         | 4 (Recommendation, Review, Chatbot, VehicleIntelligence)      |
| **TOTAL**               | **18**  | **36**  | **~918**     | **13**                                                        |

## Equipo Sugerido

| Rol                        | Cantidad | Notas                      |
| -------------------------- | -------- | -------------------------- |
| Backend Developer (.NET)   | 2        | Full-time, senior          |
| Frontend Developer (React) | 1-2      | Full-time                  |
| ML/Data Engineer           | 1        | Desde Sprint 9             |
| DevOps/SRE                 | 0.5      | Part-time o contratista    |
| QA                         | 1        | Part-time o desde Sprint 3 |
| Product Owner              | 1        | Part-time                  |

## Dependencias Críticas

```
Sprint 1: ─────────────────────────────────────────►
Sprint 2: ─────────────────────────────────────────►
Sprint 3: ───────────────────────► (depende de Auth funcional)
Sprint 4: ───────────────────────────────► (depende de Stripe config)
Sprint 5: ───────────────────────────────────────► (nuevo servicio)
Sprint 9: ─────────────────────────────────────────► (Kafka/ClickHouse)
Sprint 11: ──────────────────────────────► (depende de Sprint 9-10)
Sprint 16: ─────────────────────────────────────────► (OpenAI API)
Sprint 17: ──────────────────────────────► (WhatsApp Business API)
```

## Riesgos y Mitigaciones

| Riesgo                     | Probabilidad | Impacto | Mitigación                      |
| -------------------------- | ------------ | ------- | ------------------------------- |
| Integración Stripe demora  | Media        | Alto    | Empezar config en Sprint 3      |
| OpenAI API costs higher    | Media        | Medio   | Usar GPT-4o-mini, monitor costs |
| WhatsApp Business approval | Alta         | Alto    | Iniciar proceso en Sprint 12    |
| ML models underperform     | Media        | Medio   | Empezar con reglas, iterar      |
| Team velocity lower        | Media        | Alto    | Buffer de 20% en estimaciones   |

---

## 🚀 PRÓXIMOS PASOS INMEDIATOS

### Esta Semana (Prep Sprint 1)

1. ✅ Validar plan con stakeholders
2. ✅ Configurar board de Jira/Linear
3. ✅ Crear tickets de Sprint 1
4. ✅ Setup de ambiente de desarrollo
5. ✅ Revisión técnica de VehiclesSaleService

### Sprint 1 Kick-off

1. 📋 Sprint planning (4 horas)
2. 📋 Asignar tareas a developers
3. 📋 Definir criterios de aceptación
4. 📋 Daily standups
5. 📋 Sprint review + retro (Semana 2)

---

# 📅 FASE 5: ML & ENTRENAMIENTO DE MODELOS

> **Stack Seleccionado (Estrategia Económica ~$30-80/mes):**
>
> - 🗄️ **Vector DB:** Qdrant Self-Hosted (GRATIS)
> - 🤖 **Chatbot:** Llama 3.1 8B via Ollama/Groq (~$20-50/mes)
> - 📊 **ML Models:** XGBoost/LightGBM (GRATIS)
> - 🔤 **Embeddings:** all-MiniLM-L6-v2 (GRATIS)

---

## Sprint 19 (Semanas 37-38) - Infraestructura ML Base

**Objetivo:** Setup completo de infraestructura para ML

### Instalación de Dependencias (Python)

```bash
# Crear requirements-ml.txt
pip install \
  qdrant-client==1.7.0 \
  sentence-transformers==2.2.2 \
  xgboost==2.0.3 \
  lightgbm==4.2.0 \
  scikit-learn==1.4.0 \
  pandas==2.1.4 \
  numpy==1.26.3 \
  fastapi==0.109.0 \
  uvicorn==0.27.0 \
  httpx==0.26.0 \
  python-dotenv==1.0.0 \
  pydantic==2.5.3 \
  joblib==1.3.2 \
  mlflow==2.10.0 \
  optuna==3.5.0
```

### Backend Tasks

| Task                                          | Servicio                | Story Points |
| --------------------------------------------- | ----------------------- | ------------ |
| Deploy Qdrant en DOKS (StatefulSet)           | Infrastructure          | 5            |
| MLInfrastructureService base (Python/FastAPI) | Nuevo servicio          | 8            |
| Configurar persistent volume para modelos     | Infrastructure          | 3            |
| Setup MLflow para tracking de experimentos    | MLInfrastructureService | 5            |
| Health checks y monitoring de Qdrant          | Infrastructure          | 3            |
| API Gateway routes para ML services           | Gateway                 | 3            |

### Kubernetes Manifests

```yaml
# k8s/qdrant.yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: qdrant
  namespace: okla
spec:
  serviceName: qdrant
  replicas: 1
  template:
    spec:
      containers:
        - name: qdrant
          image: qdrant/qdrant:v1.7.4
          ports:
            - containerPort: 6333 # REST API
            - containerPort: 6334 # gRPC
          resources:
            requests:
              memory: "2Gi"
              cpu: "500m"
            limits:
              memory: "4Gi"
              cpu: "1000m"
          volumeMounts:
            - name: qdrant-storage
              mountPath: /qdrant/storage
  volumeClaimTemplates:
    - metadata:
        name: qdrant-storage
      spec:
        accessModes: ["ReadWriteOnce"]
        resources:
          requests:
            storage: 10Gi
```

### Entregables Sprint 19

```
✅ Qdrant corriendo en DOKS con persistencia
✅ MLInfrastructureService (Python/FastAPI) desplegado
✅ MLflow configurado para experimentos
✅ Volume para almacenar modelos entrenados
✅ Monitoring básico de servicios ML
✅ Gateway configurado para rutas /api/ml/*
```

**Story Points Total:** 27
**Velocidad esperada:** 25-30 SP

---

## Sprint 20 (Semanas 39-40) - Dataset de Vehículos (Alta Calidad)

**Objetivo:** Crear dataset limpio y enriquecido de vehículos para ML

### Fuentes de Datos

| Fuente                 | Datos                  | Cantidad Estimada |
| ---------------------- | ---------------------- | ----------------- |
| VehiclesSaleService DB | Vehículos actuales     | 5,000-10,000      |
| Web Scraping (legal)   | Precios mercado RD     | 20,000-50,000     |
| APIs públicas          | Specs técnicos (NHTSA) | Todos los modelos |
| Synthetic data         | Augmentation           | +20%              |

### Backend Tasks

| Task                                   | Servicio              | Story Points |
| -------------------------------------- | --------------------- | ------------ |
| DataCollectionService base (Python)    | Nuevo servicio        | 8            |
| ETL pipeline: PostgreSQL → Dataset CSV | DataCollectionService | 5            |
| Web scraper para precios mercado RD    | DataCollectionService | 8            |
| Integración NHTSA API (specs técnicos) | DataCollectionService | 5            |
| Data cleaning y normalización          | DataCollectionService | 5            |
| Feature engineering pipeline           | DataCollectionService | 8            |

### Schema del Dataset

```python
# vehicle_dataset.py
vehicle_features = {
    # Identificación
    'vehicle_id': 'uuid',
    'vin': 'string',

    # Básicos
    'make': 'category',           # Toyota, Honda, etc.
    'model': 'category',          # Corolla, Civic, etc.
    'year': 'int',                # 2015-2026
    'trim': 'category',           # LE, SE, XLE, etc.

    # Especificaciones
    'body_type': 'category',      # Sedan, SUV, Pickup, etc.
    'transmission': 'category',   # Automatic, Manual, CVT
    'fuel_type': 'category',      # Gasoline, Diesel, Electric, Hybrid
    'engine_size': 'float',       # 1.8, 2.0, 3.5, etc.
    'cylinders': 'int',           # 4, 6, 8
    'horsepower': 'int',          # 130, 180, 300, etc.
    'mpg_city': 'float',          # 25, 30, etc.
    'mpg_highway': 'float',       # 32, 38, etc.

    # Estado
    'mileage': 'int',             # 0-300,000 km
    'condition': 'category',      # Excellent, Good, Fair, Poor
    'color_exterior': 'category', # White, Black, Silver, etc.
    'color_interior': 'category', # Black, Beige, Gray
    'num_owners': 'int',          # 1, 2, 3+

    # Precio
    'price': 'float',             # Target variable
    'price_usd': 'float',         # Normalizado a USD
    'price_vs_market': 'float',   # Ratio vs promedio mercado

    # Ubicación
    'province': 'category',       # Santo Domingo, Santiago, etc.
    'city': 'category',

    # Engagement (del EventTracking)
    'total_views': 'int',
    'favorites_count': 'int',
    'contact_requests': 'int',
    'days_listed': 'int',
    'sold': 'bool',               # Target para tiempo de venta
    'days_to_sale': 'int',        # Target para predicción

    # Features derivadas
    'age_years': 'int',           # 2026 - year
    'mileage_per_year': 'float',  # mileage / age_years
    'is_luxury': 'bool',          # BMW, Mercedes, Lexus, etc.
    'is_economy': 'bool',         # Toyota, Honda, Hyundai básicos
    'popularity_score': 'float',  # Calculado de engagement
}
```

### Entregables Sprint 20

```
✅ Dataset de 50,000+ vehículos (histórico + mercado)
✅ Datos limpios y normalizados
✅ 40+ features por vehículo
✅ Specs técnicos de NHTSA integrados
✅ Pipeline reproducible de ETL
✅ Documentación de cada feature
```

**Story Points Total:** 39
**Velocidad esperada:** 35-40 SP

---

## Sprint 21 (Semanas 41-42) - Dataset de Usuarios y Comportamiento

**Objetivo:** Crear dataset de interacciones usuario-vehículo para recomendaciones

### Backend Tasks

| Task                                  | Servicio              | Story Points |
| ------------------------------------- | --------------------- | ------------ |
| ETL de EventTrackingService → Dataset | DataCollectionService | 5            |
| User-Vehicle interaction matrix       | DataCollectionService | 8            |
| User preference profiles              | DataCollectionService | 5            |
| Session reconstruction                | DataCollectionService | 5            |
| Negative sampling para training       | DataCollectionService | 5            |
| Train/Validation/Test split           | DataCollectionService | 3            |

### Schema de Interacciones

```python
# interactions_dataset.py
user_vehicle_interactions = {
    'user_id': 'uuid',
    'vehicle_id': 'uuid',
    'session_id': 'string',
    'timestamp': 'datetime',

    # Tipos de interacción (implicit feedback)
    'viewed': 'bool',
    'view_duration_seconds': 'int',
    'scrolled_full_page': 'bool',
    'viewed_all_photos': 'bool',
    'favorited': 'bool',
    'unfavorited': 'bool',
    'contacted': 'bool',
    'shared': 'bool',
    'compared': 'bool',

    # Contexto
    'device_type': 'category',    # mobile, desktop, tablet
    'referrer': 'category',       # search, homepage, similar, etc.
    'search_query': 'string',     # Si vino de búsqueda
}

# User profiles inferidos
user_profiles = {
    'user_id': 'uuid',

    # Preferencias inferidas (de comportamiento)
    'preferred_makes': 'list[string]',     # Top 5 marcas vistas
    'preferred_body_types': 'list[string]',
    'price_range_min': 'float',
    'price_range_max': 'float',
    'year_range_min': 'int',
    'year_range_max': 'int',
    'preferred_fuel_types': 'list[string]',

    # Métricas de engagement
    'total_sessions': 'int',
    'total_views': 'int',
    'avg_session_duration': 'float',
    'conversion_stage': 'category',  # browsing, researching, ready_to_buy

    # Embeddings
    'preference_embedding': 'vector[384]',  # Para similarity search
}
```

### Entregables Sprint 21

```
✅ Dataset de 100,000+ interacciones usuario-vehículo
✅ 10,000+ perfiles de usuario con preferencias
✅ Matriz de interacciones para collaborative filtering
✅ Negative samples para training balanceado
✅ Split estratificado: 70% train, 15% val, 15% test
✅ Embeddings de usuarios generados
```

**Story Points Total:** 31
**Velocidad esperada:** 28-35 SP

---

## Sprint 22 (Semanas 43-44) - Dataset de Leads para Scoring

**Objetivo:** Crear dataset etiquetado para entrenar modelo de Lead Scoring

### Backend Tasks

| Task                                   | Servicio              | Story Points |
| -------------------------------------- | --------------------- | ------------ |
| ETL de LeadScoringService → Dataset    | DataCollectionService | 5            |
| Labeling de leads convertidos/perdidos | DataCollectionService | 5            |
| Feature engineering para leads         | DataCollectionService | 8            |
| Balanceo de clases (SMOTE)             | DataCollectionService | 3            |
| Temporal validation split              | DataCollectionService | 3            |
| Data augmentation                      | DataCollectionService | 5            |

### Schema de Leads

```python
# leads_dataset.py
lead_features = {
    'lead_id': 'uuid',
    'user_id': 'uuid',
    'dealer_id': 'uuid',
    'vehicle_id': 'uuid',
    'created_at': 'datetime',

    # Features de comportamiento (antes del contacto)
    'views_before_contact': 'int',
    'favorites_before_contact': 'int',
    'days_active_before_contact': 'int',
    'sessions_before_contact': 'int',
    'vehicles_viewed_total': 'int',
    'avg_time_on_vehicle_page': 'float',
    'viewed_similar_vehicles': 'int',
    'used_financing_calculator': 'bool',
    'compared_vehicles': 'bool',

    # Features del vehículo de interés
    'vehicle_price': 'float',
    'vehicle_age': 'int',
    'vehicle_mileage': 'int',
    'vehicle_popularity': 'float',
    'price_vs_user_avg': 'float',  # vs promedio de lo que ve el usuario

    # Features del usuario
    'user_days_since_registration': 'int',
    'user_total_contacts': 'int',
    'user_conversion_history': 'int',  # Compras previas
    'user_engagement_score': 'float',

    # Features temporales
    'hour_of_contact': 'int',
    'day_of_week': 'int',
    'is_weekend': 'bool',

    # Features de la conversación
    'message_length': 'int',
    'has_phone_number': 'bool',
    'asked_for_test_drive': 'bool',
    'mentioned_financing': 'bool',
    'mentioned_trade_in': 'bool',

    # TARGET VARIABLES
    'converted': 'bool',           # ¿Se convirtió en venta?
    'days_to_conversion': 'int',   # Días hasta conversión (si aplicó)
    'response_received': 'bool',   # ¿Dealer respondió?
    'response_time_hours': 'float',
}
```

### Entregables Sprint 22

```
✅ Dataset de 5,000+ leads etiquetados
✅ Balance de clases: 30% convertidos, 70% no convertidos
✅ 35+ features por lead
✅ Temporal split (últimos 2 meses = test)
✅ Pipeline de feature engineering reproducible
✅ Análisis exploratorio documentado
```

**Story Points Total:** 29
**Velocidad esperada:** 25-30 SP

---

## Sprint 23 (Semanas 45-46) - Entrenamiento: Modelo de Pricing

**Objetivo:** Entrenar XGBoost para predicción de precios óptimos

### Dependencias Python

```bash
pip install \
  xgboost==2.0.3 \
  optuna==3.5.0 \
  shap==0.44.0 \
  matplotlib==3.8.2 \
  seaborn==0.13.1
```

### Backend Tasks

| Task                              | Servicio            | Story Points |
| --------------------------------- | ------------------- | ------------ |
| PricingModelService base (Python) | Nuevo servicio      | 8            |
| Training pipeline XGBoost         | PricingModelService | 8            |
| Hyperparameter tuning con Optuna  | PricingModelService | 5            |
| Model evaluation (MAE, MAPE, R²)  | PricingModelService | 3            |
| SHAP explainability               | PricingModelService | 5            |
| Model serialization y versionado  | PricingModelService | 3            |
| REST API para inferencia          | PricingModelService | 5            |

### Código de Entrenamiento

```python
# training/pricing_model.py
import xgboost as xgb
import optuna
from sklearn.model_selection import cross_val_score
import joblib
import mlflow

def train_pricing_model(X_train, y_train, X_val, y_val):
    """Entrenar modelo de pricing con XGBoost"""

    def objective(trial):
        params = {
            'max_depth': trial.suggest_int('max_depth', 3, 10),
            'learning_rate': trial.suggest_float('learning_rate', 0.01, 0.3),
            'n_estimators': trial.suggest_int('n_estimators', 100, 1000),
            'min_child_weight': trial.suggest_int('min_child_weight', 1, 10),
            'subsample': trial.suggest_float('subsample', 0.6, 1.0),
            'colsample_bytree': trial.suggest_float('colsample_bytree', 0.6, 1.0),
            'reg_alpha': trial.suggest_float('reg_alpha', 0, 10),
            'reg_lambda': trial.suggest_float('reg_lambda', 0, 10),
        }

        model = xgb.XGBRegressor(**params, random_state=42)
        model.fit(X_train, y_train, eval_set=[(X_val, y_val)],
                  early_stopping_rounds=50, verbose=False)

        return model.best_score

    # Optimización con Optuna (100 trials)
    study = optuna.create_study(direction='minimize')
    study.optimize(objective, n_trials=100)

    # Entrenar modelo final
    best_params = study.best_params
    final_model = xgb.XGBRegressor(**best_params, random_state=42)
    final_model.fit(X_train, y_train)

    # Log en MLflow
    with mlflow.start_run():
        mlflow.log_params(best_params)
        mlflow.log_metric('val_rmse', study.best_value)
        mlflow.xgboost.log_model(final_model, 'pricing_model')

    return final_model

# Métricas objetivo:
# - MAE: < $1,500 USD
# - MAPE: < 8%
# - R²: > 0.85
```

### Entregables Sprint 23

```
✅ Modelo XGBoost entrenado con 50K+ vehículos
✅ MAE < $1,500, MAPE < 8%, R² > 0.85
✅ Hyperparameters optimizados con Optuna
✅ SHAP values para explicar predicciones
✅ Modelo versionado en MLflow
✅ API endpoint: POST /api/ml/pricing/predict
✅ Endpoint: POST /api/ml/pricing/explain (SHAP)
```

**Story Points Total:** 37
**Velocidad esperada:** 32-40 SP

---

## Sprint 24 (Semanas 47-48) - Entrenamiento: Modelo de Lead Scoring

**Objetivo:** Entrenar LightGBM para clasificación de leads HOT/WARM/COLD

### Backend Tasks

| Task                                 | Servicio                | Story Points |
| ------------------------------------ | ----------------------- | ------------ |
| LeadScoringModelService base         | Nuevo servicio          | 5            |
| Training pipeline LightGBM           | LeadScoringModelService | 8            |
| Class balancing (SMOTE/class_weight) | LeadScoringModelService | 3            |
| Threshold optimization               | LeadScoringModelService | 5            |
| Probability calibration              | LeadScoringModelService | 3            |
| Feature importance analysis          | LeadScoringModelService | 3            |
| A/B test framework                   | LeadScoringModelService | 5            |

### Código de Entrenamiento

```python
# training/lead_scoring_model.py
import lightgbm as lgb
from sklearn.calibration import CalibratedClassifierCV
from sklearn.metrics import precision_recall_curve, f1_score
import optuna

def train_lead_scoring_model(X_train, y_train, X_val, y_val):
    """Entrenar modelo de lead scoring con LightGBM"""

    # Calcular class weights para desbalance
    class_weights = compute_class_weight('balanced', classes=[0, 1], y=y_train)

    def objective(trial):
        params = {
            'objective': 'binary',
            'metric': 'auc',
            'boosting_type': 'gbdt',
            'num_leaves': trial.suggest_int('num_leaves', 20, 150),
            'learning_rate': trial.suggest_float('learning_rate', 0.01, 0.2),
            'feature_fraction': trial.suggest_float('feature_fraction', 0.6, 1.0),
            'bagging_fraction': trial.suggest_float('bagging_fraction', 0.6, 1.0),
            'min_child_samples': trial.suggest_int('min_child_samples', 5, 100),
            'scale_pos_weight': class_weights[1] / class_weights[0],
        }

        train_data = lgb.Dataset(X_train, label=y_train)
        val_data = lgb.Dataset(X_val, label=y_val)

        model = lgb.train(params, train_data, valid_sets=[val_data],
                          num_boost_round=1000, early_stopping_rounds=50,
                          verbose_eval=False)

        return model.best_score['valid_0']['auc']

    study = optuna.create_study(direction='maximize')
    study.optimize(objective, n_trials=100)

    # Modelo final con calibración de probabilidades
    final_model = lgb.LGBMClassifier(**study.best_params)
    calibrated_model = CalibratedClassifierCV(final_model, cv=5, method='isotonic')
    calibrated_model.fit(X_train, y_train)

    return calibrated_model

# Clasificación por threshold:
# - HOT: probability >= 0.7
# - WARM: 0.3 <= probability < 0.7
# - COLD: probability < 0.3

# Métricas objetivo:
# - AUC-ROC: > 0.80
# - Precision@HOT: > 0.75
# - Recall@HOT: > 0.60
```

### Entregables Sprint 24

```
✅ Modelo LightGBM entrenado con 5K+ leads
✅ AUC-ROC > 0.80
✅ Probabilidades calibradas
✅ Thresholds optimizados para HOT/WARM/COLD
✅ Feature importance ranking
✅ API endpoint: POST /api/ml/leads/score
✅ Integración con LeadScoringService existente
```

**Story Points Total:** 32
**Velocidad esperada:** 28-35 SP

---

## Sprint 25 (Semanas 49-50) - Entrenamiento: Embeddings y Recomendaciones

**Objetivo:** Generar embeddings y configurar sistema de recomendaciones con Qdrant

### Dependencias

```bash
pip install \
  sentence-transformers==2.2.2 \
  qdrant-client==1.7.0 \
  implicit==0.7.2  # Para collaborative filtering
```

### Backend Tasks

| Task                                         | Servicio                | Story Points |
| -------------------------------------------- | ----------------------- | ------------ |
| EmbeddingService base                        | Nuevo servicio          | 5            |
| Generar embeddings de vehículos              | EmbeddingService        | 5            |
| Generar embeddings de usuarios               | EmbeddingService        | 5            |
| Indexar en Qdrant                            | EmbeddingService        | 5            |
| Collaborative filtering (ALS)                | RecommendationMLService | 8            |
| Hybrid recommender (content + collaborative) | RecommendationMLService | 8            |
| API de recomendaciones                       | RecommendationMLService | 5            |

### Código de Embeddings

```python
# embeddings/vehicle_embeddings.py
from sentence_transformers import SentenceTransformer
from qdrant_client import QdrantClient
from qdrant_client.models import VectorParams, Distance, PointStruct

# Modelo de embeddings (384 dimensiones, muy eficiente)
model = SentenceTransformer('all-MiniLM-L6-v2')

def create_vehicle_text(vehicle: dict) -> str:
    """Crear texto descriptivo del vehículo para embedding"""
    return f"""
    {vehicle['year']} {vehicle['make']} {vehicle['model']} {vehicle['trim']}
    {vehicle['body_type']} {vehicle['transmission']} {vehicle['fuel_type']}
    {vehicle['engine_size']}L {vehicle['horsepower']}hp
    {vehicle['mileage']} km {vehicle['condition']}
    Color: {vehicle['color_exterior']}
    Precio: ${vehicle['price']:,.0f}
    Ubicación: {vehicle['city']}, {vehicle['province']}
    """

def generate_vehicle_embeddings(vehicles: list) -> list:
    """Generar embeddings para lista de vehículos"""
    texts = [create_vehicle_text(v) for v in vehicles]
    embeddings = model.encode(texts, show_progress_bar=True)
    return embeddings

def index_in_qdrant(vehicles: list, embeddings: list):
    """Indexar vehículos en Qdrant"""
    client = QdrantClient(host="qdrant", port=6333)

    # Crear colección si no existe
    client.recreate_collection(
        collection_name="vehicles",
        vectors_config=VectorParams(size=384, distance=Distance.COSINE)
    )

    # Insertar puntos
    points = [
        PointStruct(
            id=str(v['vehicle_id']),
            vector=emb.tolist(),
            payload={
                'make': v['make'],
                'model': v['model'],
                'year': v['year'],
                'price': v['price'],
                'body_type': v['body_type'],
            }
        )
        for v, emb in zip(vehicles, embeddings)
    ]

    client.upsert(collection_name="vehicles", points=points)

def find_similar_vehicles(vehicle_id: str, limit: int = 10):
    """Encontrar vehículos similares"""
    client = QdrantClient(host="qdrant", port=6333)

    # Obtener embedding del vehículo
    result = client.retrieve(collection_name="vehicles", ids=[vehicle_id])
    vehicle_embedding = result[0].vector

    # Buscar similares
    similar = client.search(
        collection_name="vehicles",
        query_vector=vehicle_embedding,
        limit=limit + 1  # +1 porque incluye el mismo
    )

    return [s for s in similar if s.id != vehicle_id][:limit]
```

### Entregables Sprint 25

```
✅ 50K+ vehículos indexados en Qdrant
✅ 10K+ usuarios con embeddings de preferencias
✅ Búsqueda de similares en < 50ms
✅ Hybrid recommender funcionando
✅ API: GET /api/ml/recommendations/similar/{vehicleId}
✅ API: GET /api/ml/recommendations/for-user/{userId}
✅ Integración con RecommendationService existente
```

**Story Points Total:** 41
**Velocidad esperada:** 35-42 SP

---

## Sprint 26 (Semanas 51-52) - Setup Chatbot Llama Local

**Objetivo:** Configurar Llama 3.1 8B para chatbot con RAG

### Dependencias

```bash
pip install \
  ollama==0.1.6 \
  langchain==0.1.0 \
  langchain-community==0.0.13 \
  tiktoken==0.5.2
```

### Backend Tasks

| Task                                | Servicio         | Story Points |
| ----------------------------------- | ---------------- | ------------ |
| Deploy Ollama en DOKS               | Infrastructure   | 5            |
| Descargar modelo Llama 3.1 8B       | Infrastructure   | 3            |
| ChatbotMLService base               | Nuevo servicio   | 8            |
| RAG pipeline con Qdrant             | ChatbotMLService | 8            |
| Prompt engineering para vehículos   | ChatbotMLService | 5            |
| Context injection (vehículo actual) | ChatbotMLService | 5            |
| Response streaming (SSE)            | ChatbotMLService | 5            |

### Configuración Ollama en K8s

```yaml
# k8s/ollama.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: ollama
  namespace: okla
spec:
  replicas: 1
  template:
    spec:
      containers:
        - name: ollama
          image: ollama/ollama:latest
          ports:
            - containerPort: 11434
          resources:
            requests:
              memory: "8Gi"
              cpu: "2000m"
            limits:
              memory: "16Gi"
              cpu: "4000m"
          volumeMounts:
            - name: ollama-models
              mountPath: /root/.ollama
      volumes:
        - name: ollama-models
          persistentVolumeClaim:
            claimName: ollama-models-pvc
---
# PVC para modelos (Llama 8B = ~5GB)
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: ollama-models-pvc
  namespace: okla
spec:
  accessModes: ["ReadWriteOnce"]
  resources:
    requests:
      storage: 20Gi
```

### Código del Chatbot con RAG

```python
# chatbot/rag_chatbot.py
from langchain.chains import ConversationalRetrievalChain
from langchain_community.llms import Ollama
from langchain_community.vectorstores import Qdrant
from qdrant_client import QdrantClient

def create_chatbot():
    # Conectar a Ollama
    llm = Ollama(
        model="llama3.1:8b",
        base_url="http://ollama:11434",
        temperature=0.7,
    )

    # Conectar a Qdrant para RAG
    qdrant_client = QdrantClient(host="qdrant", port=6333)

    return llm, qdrant_client

def chat_with_vehicle_context(
    query: str,
    vehicle_id: str,
    conversation_history: list
) -> str:
    """Chat con contexto del vehículo actual"""

    llm, qdrant = create_chatbot()

    # Obtener info del vehículo
    vehicle_info = get_vehicle_details(vehicle_id)

    # Buscar info relevante en Qdrant
    relevant_docs = qdrant.search(
        collection_name="vehicle_knowledge",
        query_vector=embed_query(query),
        limit=3
    )

    # Construir prompt con contexto
    system_prompt = f"""Eres un asistente experto en vehículos para OKLA Marketplace.

    VEHÍCULO ACTUAL:
    {vehicle_info}

    INFORMACIÓN RELEVANTE:
    {format_docs(relevant_docs)}

    Responde de forma amigable y profesional en español.
    Si no sabes algo, di que el vendedor puede dar más detalles.
    Si detectas intención de compra alta, sugiere contactar al vendedor.
    """

    response = llm.invoke(
        f"{system_prompt}\n\nHistorial: {conversation_history}\n\nUsuario: {query}"
    )

    return response

# Prompts optimizados por caso:
# - Preguntas sobre especificaciones
# - Preguntas sobre precio/negociación
# - Comparación con otros vehículos
# - Financiamiento
# - Test drive
```

### Entregables Sprint 26

```
✅ Ollama corriendo en DOKS con Llama 3.1 8B
✅ RAG pipeline conectado a Qdrant
✅ Prompts optimizados para marketplace
✅ Context injection del vehículo actual
✅ Streaming responses (SSE)
✅ API: POST /api/ml/chat/message
✅ Latencia < 2 segundos primera respuesta
```

**Story Points Total:** 39
**Velocidad esperada:** 35-42 SP

---

## 🎉 MILESTONE: MODELOS ML ENTRENADOS

**Fecha estimada:** Semana 52 (12 meses desde inicio)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     ML MODELS TRAINED ✅                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  🗄️ INFRAESTRUCTURA:                                                         │
│  ├── ✅ Qdrant Vector DB (self-hosted, 0 costo)                             │
│  ├── ✅ Ollama con Llama 3.1 8B                                             │
│  ├── ✅ MLflow para tracking                                                │
│  └── ✅ Persistent volumes para modelos                                     │
│                                                                              │
│  📊 DATASETS:                                                                │
│  ├── ✅ 50,000+ vehículos con 40+ features                                  │
│  ├── ✅ 100,000+ interacciones usuario-vehículo                             │
│  ├── ✅ 5,000+ leads etiquetados                                            │
│  └── ✅ Embeddings de 50K vehículos + 10K usuarios                          │
│                                                                              │
│  🤖 MODELOS:                                                                 │
│  ├── ✅ Pricing: XGBoost (MAE < $1,500, R² > 0.85)                          │
│  ├── ✅ Lead Scoring: LightGBM (AUC > 0.80)                                 │
│  ├── ✅ Recommendations: Hybrid (content + collaborative)                    │
│  └── ✅ Chatbot: Llama 3.1 8B + RAG                                         │
│                                                                              │
│  💰 COSTO TOTAL: ~$30-80/mes (hosting + Groq fallback)                       │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Sprint 27 (Semanas 53-54) - Integración ML → Producción

**Objetivo:** Conectar modelos ML con servicios .NET existentes

### Backend Tasks

| Task                                       | Servicio                   | Story Points |
| ------------------------------------------ | -------------------------- | ------------ |
| MLGatewayService (.NET) - cliente HTTP     | Nuevo servicio             | 8            |
| Cache de predicciones (Redis)              | MLGatewayService           | 5            |
| Circuit breaker para ML calls              | MLGatewayService           | 3            |
| Integrar VehicleIntelligenceService con ML | VehicleIntelligenceService | 5            |
| Integrar LeadScoringService con ML         | LeadScoringService         | 5            |
| Integrar RecommendationService con Qdrant  | RecommendationService      | 5            |
| Integrar ChatbotService con Llama          | ChatbotService             | 5            |
| Fallback a reglas si ML falla              | MLGatewayService           | 5            |

### Entregables Sprint 27

```
✅ MLGatewayService conectando .NET ↔ Python
✅ Cache de predicciones en Redis
✅ Circuit breaker para tolerancia
✅ Todos los servicios usando ML real
✅ Fallback automático
```

**Story Points Total:** 41

---

## Sprint 28 (Semanas 55-56) - Monitoreo y A/B Testing

**Objetivo:** Monitorear modelos y validar mejoras

### Backend Tasks

| Task                      | Servicio            | Story Points |
| ------------------------- | ------------------- | ------------ |
| MLMonitoringService base  | Nuevo servicio      | 8            |
| Dashboard Grafana para ML | Infrastructure      | 5            |
| A/B test framework        | MLMonitoringService | 8            |
| Model drift detection     | MLMonitoringService | 5            |
| Alertas de degradación    | MLMonitoringService | 3            |
| Logging de predicciones   | MLMonitoringService | 3            |
| Scheduler para retraining | MLMonitoringService | 5            |

### Entregables Sprint 28

```
✅ Dashboard Grafana con métricas ML
✅ A/B testing framework
✅ Drift detection
✅ Alertas automáticas
✅ Retraining scheduler
```

**Story Points Total:** 37

---

## Sprint 29 (Semanas 57-58) - 📚 DOCUMENTACIÓN COMPLETA

**Objetivo:** Documentar TODO para que puedas aprenderlo

### Documentos a Crear

| Documento                 | Páginas |
| ------------------------- | ------- |
| ML_ARCHITECTURE.md        | 15-20   |
| DATASET_CREATION_GUIDE.md | 10-15   |
| MODEL_TRAINING_GUIDE.md   | 20-25   |
| DEPLOYMENT_GUIDE.md       | 10-15   |
| MONITORING_GUIDE.md       | 10-15   |
| AB_TESTING_GUIDE.md       | 8-10    |
| TROUBLESHOOTING.md        | 10-12   |
| 8 Jupyter Notebooks       | -       |

### Estructura

```
docs/ml/
├── README.md
├── 01-architecture/
├── 02-datasets/
├── 03-training/
├── 04-deployment/
├── 05-monitoring/
├── 06-experiments/
├── 07-troubleshooting/
└── notebooks/ (8 notebooks)
```

### Entregables Sprint 29

```
✅ 7 documentos (~100 páginas)
✅ 8 Jupyter notebooks
✅ README con índice
✅ Diagramas Mermaid
```

**Story Points Total:** 56

---

## Sprint 30 (Semanas 59-60) - 🖼️ Dataset de Imágenes de Vehículos

**Objetivo:** Crear dataset de imágenes etiquetadas para entrenar modelo de validación

### Dependencias Python

```bash
pip install \
  opencv-python==4.9.0.80 \
  pillow==10.2.0 \
  albumentations==1.3.1 \
  imagehash==4.3.1 \
  torch==2.2.0 \
  torchvision==0.17.0 \
  timm==0.9.12 \
  ultralytics==8.1.0 \
  roboflow==1.1.18
```

### Fuentes de Datos para Imágenes

| Fuente                | Tipo                       | Cantidad Est.  |
| --------------------- | -------------------------- | -------------- |
| MediaService (S3)     | Imágenes existentes OKLA   | 50,000-100,000 |
| Stanford Cars Dataset | Dataset público            | 16,185         |
| CompCars Dataset      | Dataset público            | 136,726        |
| Web scraping (legal)  | Imágenes RD                | 20,000-30,000  |
| Negative samples      | No-vehículos, baja calidad | 10,000         |

### Categorías de Etiquetado

```python
# image_labels.py

# 1. Calidad de imagen
image_quality_labels = {
    'quality_score': float,       # 0.0 - 1.0
    'is_blurry': bool,            # Blur detection
    'is_dark': bool,              # Subexposición
    'is_overexposed': bool,       # Sobreexposición
    'resolution_ok': bool,        # >= 800x600
    'has_watermark': bool,        # Watermarks externos
    'is_screenshot': bool,        # Screenshots de otras apps
}

# 2. Contenido de imagen
content_labels = {
    'is_vehicle': bool,           # ¿Es un vehículo?
    'vehicle_type': str,          # car, suv, truck, motorcycle, etc.
    'view_angle': str,            # front, rear, side, interior, engine, wheel
    'is_exterior': bool,          # Exterior vs interior
    'shows_plate': bool,          # ¿Se ve la placa?
    'has_people': bool,           # ¿Hay personas?
    'is_professional': bool,      # Foto profesional vs amateur
}

# 3. Identificación del vehículo
vehicle_identity_labels = {
    'detected_make': str,         # Toyota, Honda, BMW...
    'detected_model': str,        # Corolla, Civic, X5...
    'detected_year_range': str,   # 2018-2022
    'detected_color': str,        # White, Black, Silver...
    'detected_body_type': str,    # Sedan, SUV, Hatchback...
    'confidence': float,          # Confianza de la detección
}

# 4. Validación de publicación
listing_validation = {
    'matches_declared_make': bool,    # ¿Coincide con lo declarado?
    'matches_declared_model': bool,
    'matches_declared_year': bool,
    'matches_declared_color': bool,
    'is_stock_photo': bool,           # Foto de stock/genérica
    'is_duplicate': bool,             # Duplicada de otra publicación
    'approval_recommendation': str,   # approve, review, reject
}
```

### Backend Tasks

| Task                               | Servicio            | Story Points |
| ---------------------------------- | ------------------- | ------------ |
| ImageDatasetService base (Python)  | Nuevo servicio      | 5            |
| ETL de imágenes desde S3           | ImageDatasetService | 5            |
| Labeling tool UI (interno)         | Frontend Admin      | 8            |
| Stanford/CompCars integration      | ImageDatasetService | 5            |
| Data augmentation pipeline         | ImageDatasetService | 5            |
| Duplicate detection (imagehash)    | ImageDatasetService | 3            |
| Quality auto-labeling (OpenCV)     | ImageDatasetService | 5            |
| Train/val/test split estratificado | ImageDatasetService | 3            |

### Entregables Sprint 30

```
✅ 200,000+ imágenes de vehículos recopiladas
✅ 50,000+ imágenes etiquetadas manualmente
✅ Auto-labeling de calidad (blur, light, resolution)
✅ Mapping make/model → imágenes
✅ Dataset de negative samples (no-vehículos)
✅ Herramienta de labeling para admin
✅ Split: 70% train, 15% val, 15% test
```

**Story Points Total:** 39

---

## Sprint 31 (Semanas 61-62) - 🤖 Entrenamiento: Modelo de Calidad de Imagen

**Objetivo:** Entrenar modelo para detectar calidad de imágenes

### Arquitectura del Modelo

```python
# models/image_quality_model.py
"""
Modelo: EfficientNet-B0 fine-tuned
Input: Imagen 224x224
Output:
  - quality_score (0-1)
  - is_blurry (bool)
  - is_dark (bool)
  - is_overexposed (bool)
  - resolution_adequate (bool)
"""

import torch
import torch.nn as nn
import timm

class ImageQualityModel(nn.Module):
    def __init__(self):
        super().__init__()
        # Backbone: EfficientNet-B0 (5.3M params, rápido)
        self.backbone = timm.create_model(
            'efficientnet_b0',
            pretrained=True,
            num_classes=0  # Remove classifier
        )

        # Multi-task heads
        self.quality_head = nn.Sequential(
            nn.Linear(1280, 256),
            nn.ReLU(),
            nn.Dropout(0.3),
            nn.Linear(256, 1),
            nn.Sigmoid()  # Score 0-1
        )

        self.defects_head = nn.Sequential(
            nn.Linear(1280, 256),
            nn.ReLU(),
            nn.Dropout(0.3),
            nn.Linear(256, 4),  # blurry, dark, overexposed, low_res
            nn.Sigmoid()
        )

    def forward(self, x):
        features = self.backbone(x)
        quality_score = self.quality_head(features)
        defects = self.defects_head(features)
        return quality_score, defects
```

### Backend Tasks

| Task                               | Servicio                 | Story Points |
| ---------------------------------- | ------------------------ | ------------ |
| ImageQualityModelService base      | Nuevo servicio           | 5            |
| Training pipeline PyTorch          | ImageQualityModelService | 8            |
| Data augmentation (Albumentations) | ImageQualityModelService | 3            |
| Hyperparameter tuning              | ImageQualityModelService | 5            |
| Model evaluation (accuracy, F1)    | ImageQualityModelService | 3            |
| ONNX export para inferencia rápida | ImageQualityModelService | 3            |
| REST API para validación           | ImageQualityModelService | 5            |
| Integración con MediaService       | MediaService             | 5            |

### Métricas Objetivo

| Métrica                    | Target  |
| -------------------------- | ------- |
| Accuracy (calidad general) | > 92%   |
| F1-Score (blur detection)  | > 0.88  |
| F1-Score (lighting issues) | > 0.85  |
| Latencia por imagen        | < 100ms |
| Modelo size (ONNX)         | < 50MB  |

### Entregables Sprint 31

```
✅ Modelo EfficientNet-B0 fine-tuned
✅ Accuracy > 92% en test set
✅ Detección de blur, dark, overexposed
✅ Exportado a ONNX para inferencia rápida
✅ API: POST /api/ml/images/quality
✅ Integrado con MediaService (validación al upload)
```

**Story Points Total:** 37

---

## Sprint 32 (Semanas 63-64) - 🚗 Entrenamiento: Reconocimiento de Vehículos

**Objetivo:** Entrenar modelo que identifique make/model/year de vehículos

### Arquitectura del Modelo

```python
# models/vehicle_recognition_model.py
"""
Modelo: ResNet-50 fine-tuned en Stanford Cars + CompCars
Input: Imagen 299x299
Output:
  - make (clasificación ~50 marcas)
  - model (clasificación ~500 modelos)
  - year_range (regresión o clasificación)
  - body_type (sedan, suv, truck, etc.)
  - color (clasificación ~15 colores)
"""

import torch
import torch.nn as nn
import timm

class VehicleRecognitionModel(nn.Module):
    def __init__(self, num_makes=50, num_models=500, num_colors=15):
        super().__init__()
        # Backbone: ResNet-50 (más profundo para fine-grained classification)
        self.backbone = timm.create_model(
            'resnet50',
            pretrained=True,
            num_classes=0
        )

        hidden_size = 2048  # ResNet-50 output

        # Heads especializados
        self.make_head = nn.Sequential(
            nn.Linear(hidden_size, 512),
            nn.ReLU(),
            nn.Dropout(0.4),
            nn.Linear(512, num_makes)
        )

        self.model_head = nn.Sequential(
            nn.Linear(hidden_size + num_makes, 1024),  # Concat make features
            nn.ReLU(),
            nn.Dropout(0.4),
            nn.Linear(1024, num_models)
        )

        self.body_type_head = nn.Sequential(
            nn.Linear(hidden_size, 256),
            nn.ReLU(),
            nn.Linear(256, 7)  # sedan, suv, truck, hatchback, coupe, van, wagon
        )

        self.color_head = nn.Sequential(
            nn.Linear(hidden_size, 256),
            nn.ReLU(),
            nn.Linear(256, num_colors)
        )

        self.year_head = nn.Sequential(
            nn.Linear(hidden_size, 256),
            nn.ReLU(),
            nn.Linear(256, 1)  # Regresión año normalizado
        )

    def forward(self, x):
        features = self.backbone(x)

        make_logits = self.make_head(features)
        make_probs = torch.softmax(make_logits, dim=1)

        # Concatenar make probs para ayudar a model prediction
        model_input = torch.cat([features, make_probs], dim=1)
        model_logits = self.model_head(model_input)

        body_type = self.body_type_head(features)
        color = self.color_head(features)
        year = self.year_head(features)

        return {
            'make': make_logits,
            'model': model_logits,
            'body_type': body_type,
            'color': color,
            'year': year
        }

# Marcas soportadas (mercado RD)
SUPPORTED_MAKES = [
    'Toyota', 'Honda', 'Hyundai', 'Kia', 'Nissan', 'Mitsubishi',
    'Mazda', 'Suzuki', 'Ford', 'Chevrolet', 'Jeep', 'Dodge',
    'BMW', 'Mercedes-Benz', 'Audi', 'Lexus', 'Volkswagen',
    'Subaru', 'Acura', 'Infiniti', 'Land Rover', 'Porsche',
    # ... hasta ~50 marcas
]
```

### Backend Tasks

| Task                                     | Servicio                  | Story Points |
| ---------------------------------------- | ------------------------- | ------------ |
| VehicleRecognitionService base           | Nuevo servicio            | 5            |
| Training con Stanford Cars               | VehicleRecognitionService | 8            |
| Fine-tuning con datos RD                 | VehicleRecognitionService | 5            |
| Hierarchical classification (make→model) | VehicleRecognitionService | 5            |
| Color detection module                   | VehicleRecognitionService | 3            |
| Year estimation                          | VehicleRecognitionService | 5            |
| Model ensemble (opcional)                | VehicleRecognitionService | 5            |
| API de reconocimiento                    | VehicleRecognitionService | 5            |

### Entregables Sprint 32

```
✅ Modelo ResNet-50 fine-tuned
✅ Top-1 accuracy make: > 85%
✅ Top-1 accuracy model: > 70%
✅ Top-3 accuracy model: > 88%
✅ Color accuracy: > 80%
✅ Body type accuracy: > 90%
✅ API: POST /api/ml/images/recognize
```

**Story Points Total:** 41

---

## Sprint 33 (Semanas 65-66) - ✅ Sistema de Validación de Publicaciones

**Objetivo:** Integrar modelos en sistema de validación automática

### Arquitectura del Sistema

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    LISTING VALIDATION PIPELINE                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Dealer sube publicación                                                     │
│           │                                                                  │
│           ▼                                                                  │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  1. IMAGE QUALITY CHECK                                              │    │
│  │     - Blur detection                                                 │    │
│  │     - Lighting check                                                 │    │
│  │     - Resolution validation                                          │    │
│  │     - Watermark detection                                            │    │
│  │     OUTPUT: quality_score, issues[]                                  │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│           │                                                                  │
│           ▼                                                                  │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  2. VEHICLE RECOGNITION                                              │    │
│  │     - Detect make/model/year                                         │    │
│  │     - Detect color                                                   │    │
│  │     - Detect body type                                               │    │
│  │     OUTPUT: detected_vehicle{}, confidence                           │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│           │                                                                  │
│           ▼                                                                  │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  3. CONSISTENCY VALIDATION                                           │    │
│  │     - Compare detected vs declared (make, model, year, color)        │    │
│  │     - Duplicate image detection (imagehash)                          │    │
│  │     - Stock photo detection                                          │    │
│  │     OUTPUT: consistency_score, mismatches[]                          │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│           │                                                                  │
│           ▼                                                                  │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  4. CONTENT MODERATION                                               │    │
│  │     - Check for inappropriate content                                │    │
│  │     - Check all images are vehicles                                  │    │
│  │     - Check image angles variety                                     │    │
│  │     OUTPUT: moderation_result                                        │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│           │                                                                  │
│           ▼                                                                  │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │  5. FINAL DECISION                                                   │    │
│  │     ✅ APPROVED: Todas las validaciones pasaron                      │    │
│  │     ⚠️ REVIEW: Algunas alertas, requiere revisión manual            │    │
│  │     ❌ REJECTED: Problemas críticos detectados                       │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Backend Tasks

| Task                                       | Servicio                 | Story Points |
| ------------------------------------------ | ------------------------ | ------------ |
| ListingValidationService base              | Nuevo servicio           | 8            |
| Orchestration pipeline                     | ListingValidationService | 5            |
| Consistency checker (detected vs declared) | ListingValidationService | 5            |
| Stock photo detection                      | ListingValidationService | 5            |
| Duplicate detection (cross-listings)       | ListingValidationService | 5            |
| Image angle diversity check                | ListingValidationService | 3            |
| Admin review queue                         | AdminService             | 5            |
| Notification de resultado                  | NotificationService      | 3            |
| Dashboard de validaciones                  | Frontend                 | 8            |

### Reglas de Validación

```python
# validation_rules.py

def calculate_listing_score(validation_result: dict) -> dict:
    """Calcular score final y decisión"""

    score = 100
    issues = []

    # 1. Quality issues (-5 a -20 por imagen)
    for img in validation_result['images']:
        if img['is_blurry']:
            score -= 10
            issues.append(f"Imagen {img['index']} borrosa")
        if img['is_dark'] or img['is_overexposed']:
            score -= 5
            issues.append(f"Imagen {img['index']} problemas de iluminación")
        if img['quality_score'] < 0.5:
            score -= 15
            issues.append(f"Imagen {img['index']} baja calidad")

    # 2. Consistency issues (-10 a -30)
    consistency = validation_result['consistency']
    if not consistency['make_matches']:
        score -= 20
        issues.append(f"Marca detectada ({consistency['detected_make']}) "
                     f"no coincide con declarada ({consistency['declared_make']})")
    if not consistency['model_matches']:
        score -= 15
        issues.append("Modelo no coincide")
    if not consistency['color_matches']:
        score -= 10
        issues.append("Color no coincide")

    # 3. Critical issues (-50)
    if validation_result['is_duplicate']:
        score -= 50
        issues.append("Imágenes duplicadas de otra publicación")
    if validation_result['is_stock_photo']:
        score -= 40
        issues.append("Detectada foto de stock/genérica")
    if validation_result['has_inappropriate_content']:
        score = 0
        issues.append("Contenido inapropiado detectado")

    # 4. Positive factors (+5 a +15)
    if validation_result['has_exterior_views'] >= 4:
        score += 5  # Buena variedad de ángulos
    if validation_result['has_interior_views']:
        score += 5
    if validation_result['avg_quality_score'] > 0.8:
        score += 5

    # Decision
    if score >= 80:
        decision = 'APPROVED'
    elif score >= 50:
        decision = 'REVIEW'
    else:
        decision = 'REJECTED'

    return {
        'score': max(0, min(100, score)),
        'decision': decision,
        'issues': issues,
        'recommendations': generate_recommendations(issues)
    }
```

### Frontend: Feedback al Dealer

```typescript
// components/ListingValidationResult.tsx
interface ValidationResult {
  score: number;
  decision: "APPROVED" | "REVIEW" | "REJECTED";
  issues: ValidationIssue[];
  recommendations: string[];
  imageResults: ImageValidation[];
}

// UI muestra:
// ✅ "Tu publicación fue aprobada automáticamente"
// ⚠️ "Tu publicación está en revisión. Problemas detectados: [lista]"
// ❌ "Tu publicación fue rechazada. Razones: [lista]. Recomendaciones: [lista]"
```

### Entregables Sprint 33

```
✅ Pipeline de validación completo
✅ Validación automática al subir imágenes
✅ Detección de inconsistencias (make/model/color)
✅ Detección de duplicados cross-listings
✅ Detección de fotos de stock
✅ Cola de revisión para admin
✅ Feedback visual al dealer
✅ Dashboard de métricas de validación
✅ Latencia total < 5 segundos
```

**Story Points Total:** 47

---

## Sprint 34 (Semanas 67-68) - 📚 Documentación Modelos de Visión

**Objetivo:** Documentar sistema de validación de imágenes

### Documentos Adicionales

```
docs/ml/
├── 08-computer-vision/
│   ├── IMAGE_QUALITY_MODEL.md       # Arquitectura y training
│   ├── VEHICLE_RECOGNITION_MODEL.md # Fine-grained classification
│   ├── LISTING_VALIDATION.md        # Pipeline completo
│   ├── LABELING_GUIDE.md            # Cómo etiquetar imágenes
│   └── CV_TROUBLESHOOTING.md        # Problemas comunes
└── notebooks/
    ├── 09_image_quality_training.ipynb
    ├── 10_vehicle_recognition_training.ipynb
    ├── 11_validation_pipeline_demo.ipynb
    └── 12_labeling_tool_usage.ipynb
```

### Entregables Sprint 34

```
✅ 5 documentos de Computer Vision
✅ 4 Jupyter notebooks adicionales
✅ Guía de labeling para equipo
✅ Runbook de troubleshooting
```

**Story Points Total:** 28

---

## 🎉 MILESTONE FINAL ACTUALIZADO

```
┌─────────────────────────────────────────────────────────────────────────────┐
│              OKLA + ML + CV 100% COMPLETO ✅                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│  📊 6 MODELOS EN PRODUCCIÓN                                                  │
│  ├── Pricing (XGBoost)                                                      │
│  ├── Lead Scoring (LightGBM)                                                │
│  ├── Recommendations (Embeddings + Qdrant)                                  │
│  ├── Chatbot (Llama 3.1 + RAG)                                              │
│  ├── Image Quality (EfficientNet-B0)   ← NUEVO                              │
│  └── Vehicle Recognition (ResNet-50)   ← NUEVO                              │
│                                                                              │
│  🖼️ VALIDACIÓN AUTOMÁTICA:                                                   │
│  ├── Calidad de imágenes                                                    │
│  ├── Reconocimiento make/model/year/color                                   │
│  ├── Consistencia declared vs detected                                      │
│  ├── Detección de duplicados y stock photos                                 │
│  └── Decisión automática: Approved/Review/Rejected                          │
│                                                                              │
│  📚 DOCUMENTACIÓN:                                                           │
│  ├── ~120 PÁGINAS (antes ~100)                                              │
│  └── 12 JUPYTER NOTEBOOKS (antes 8)                                         │
│                                                                              │
│  💰 COSTO TOTAL: ~$40-100/MES                                                │
│  ⏱️ 34 SPRINTS (17 MESES)                                                    │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

# 📅 FASE 7: GROWTH & ESCALABILIDAD

---

## Sprint 35 (Semanas 69-70) - 🔍 SEO & Landing Pages

**Objetivo:** Posicionar OKLA en Google para búsquedas de vehículos en RD

### Por qué es CRÍTICO

- 70%+ del tráfico de marketplaces viene de búsqueda orgánica
- Sin SEO, dependes 100% de ads pagados (costoso)
- Competidores ya están posicionados

### Backend Tasks

| Task                                 | Servicio            | Story Points |
| ------------------------------------ | ------------------- | ------------ |
| SEOService base                      | Nuevo servicio      | 5            |
| Sitemap.xml dinámico                 | SEOService          | 5            |
| Meta tags dinámicos por vehículo     | SEOService          | 5            |
| Schema.org markup (Vehicle, Product) | SEOService          | 5            |
| Canonical URLs                       | SEOService          | 3            |
| robots.txt optimizado                | SEOService          | 2            |
| URL slugs SEO-friendly               | VehiclesSaleService | 5            |

### Frontend Tasks

| Task                        | Componente                | Story Points |
| --------------------------- | ------------------------- | ------------ |
| Landing pages por marca     | /vehiculos/toyota         | 8            |
| Landing pages por modelo    | /vehiculos/toyota/corolla | 5            |
| Landing pages por ciudad    | /vehiculos/santo-domingo  | 5            |
| Landing pages por tipo      | /vehiculos/suv            | 5            |
| Breadcrumbs estructurados   | BreadcrumbNav             | 3            |
| Open Graph & Twitter Cards  | MetaTags                  | 3            |
| Google Search Console setup | -                         | 2            |

### Estructura de URLs SEO

```
okla.com.do/vehiculos                           → Todos los vehículos
okla.com.do/vehiculos/toyota                    → Todos los Toyota
okla.com.do/vehiculos/toyota/corolla            → Todos los Corolla
okla.com.do/vehiculos/toyota/corolla/2024       → Corolla 2024
okla.com.do/vehiculos/suv                       → Todos los SUVs
okla.com.do/vehiculos/santo-domingo             → Vehículos en SD
okla.com.do/vehiculos/usados/menos-500000       → Usados < $500K DOP
okla.com.do/dealers/auto-plaza-toyota           → Página del dealer
```

### Entregables Sprint 35

```
✅ Sitemap.xml con 50K+ URLs
✅ Schema.org en todas las páginas de vehículos
✅ Landing pages por marca (50+ páginas)
✅ Landing pages por modelo (200+ páginas)
✅ Landing pages por ciudad (10+ páginas)
✅ Meta descriptions dinámicas
✅ Google Search Console configurado
✅ Core Web Vitals optimizados
```

**Story Points Total:** 56

---

## Sprint 36 (Semanas 71-72) - 📋 Historial de Vehículo

**Objetivo:** Mostrar historial verificable del vehículo (como CarFax)

### Integraciones RD

| Fuente  | Datos                      | API                      |
| ------- | -------------------------- | ------------------------ |
| DGII    | Verificar RNC del vendedor | Consulta manual/scraping |
| INTRANT | Multas de tránsito         | Por definir              |
| Bancos  | Gravámenes/préstamos       | Partnership              |
| Seguros | Historial de siniestros    | Partnership              |

### Backend Tasks

| Task                             | Servicio              | Story Points |
| -------------------------------- | --------------------- | ------------ |
| VehicleHistoryService base       | Nuevo servicio        | 8            |
| Integración DGII (verificar RNC) | VehicleHistoryService | 5            |
| Placeholder INTRANT (manual)     | VehicleHistoryService | 3            |
| Sistema de reportes manuales     | VehicleHistoryService | 5            |
| Caché de consultas               | VehicleHistoryService | 3            |
| API de historial                 | VehicleHistoryService | 5            |

### Frontend Tasks

| Task                           | Componente            | Story Points |
| ------------------------------ | --------------------- | ------------ |
| Sección "Historial" en detalle | VehicleHistory        | 8            |
| Badge "Verificado OKLA"        | VerifiedBadge         | 3            |
| Checklist de verificaciones    | VerificationChecklist | 5            |
| Modal de detalles              | HistoryDetailModal    | 5            |

### Entregables Sprint 36

```
✅ Verificación de RNC del vendedor
✅ Sección de historial en página de vehículo
✅ Badge "Verificado OKLA" para vehículos completos
✅ Checklist visual de verificaciones
✅ Disclaimer legal apropiado
```

**Story Points Total:** 50

---

## Sprint 37 (Semanas 73-74) - 📅 Test Drive Scheduling

**Objetivo:** Agendar citas de test drive entre compradores y vendedores

### Backend Tasks

| Task                              | Servicio              | Story Points |
| --------------------------------- | --------------------- | ------------ |
| TestDriveService base             | Nuevo servicio (5064) | 8            |
| CRUD de disponibilidad (vendedor) | TestDriveService      | 5            |
| CRUD de citas (comprador)         | TestDriveService      | 5            |
| Calendar sync (Google Calendar)   | TestDriveService      | 8            |
| Reminders (email + SMS)           | NotificationService   | 5            |
| Confirmación y cancelación        | TestDriveService      | 3            |
| Reagendamiento                    | TestDriveService      | 3            |
| Calificación post-test drive      | TestDriveService      | 3            |

### Frontend Tasks

| Task                                  | Componente           | Story Points |
| ------------------------------------- | -------------------- | ------------ |
| Widget de agendar en detalle          | ScheduleTestDrive    | 8            |
| Selector de fecha/hora                | DateTimePicker       | 5            |
| Calendario de disponibilidad (dealer) | AvailabilityCalendar | 8            |
| Mis citas (comprador)                 | MyAppointments       | 5            |
| Citas recibidas (vendedor)            | ReceivedAppointments | 5            |

### Entregables Sprint 37

```
✅ Compradores pueden agendar test drives
✅ Vendedores definen disponibilidad
✅ Sync con Google Calendar
✅ Reminders 24h y 1h antes
✅ Confirmación y cancelación
✅ Rating post-test drive
```

**Story Points Total:** 71

---

## Sprint 38-39 (Semanas 75-78) - 💳 Financiamiento

**Objetivo:** Integrar opciones de financiamiento con bancos RD

### Bancos Target (RD)

| Banco              | Tipo              | Prioridad |
| ------------------ | ----------------- | --------- |
| Banco Popular      | Préstamo auto     | Alta      |
| Banreservas        | Préstamo auto     | Alta      |
| BHD León           | Préstamo auto     | Media     |
| Scotiabank         | Préstamo auto     | Media     |
| Asociación Popular | Préstamo personal | Baja      |

### Sprint 38: Backend

| Task                               | Servicio         | Story Points |
| ---------------------------------- | ---------------- | ------------ |
| FinancingService base (5065)       | Nuevo servicio   | 8            |
| Calculadora de cuotas              | FinancingService | 5            |
| Pre-calificación (score básico)    | FinancingService | 8            |
| API de solicitud de financiamiento | FinancingService | 5            |
| Webhook para respuesta de banco    | FinancingService | 5            |
| Partnership docs/contratos         | Legal            | -            |

**Story Points Sprint 38:** 31

### Sprint 39: Frontend + Integración

| Task                           | Componente           | Story Points |
| ------------------------------ | -------------------- | ------------ |
| Calculadora de cuotas UI       | FinanceCalculator    | 8            |
| Formulario de pre-calificación | PreQualificationForm | 8            |
| Comparador de ofertas          | OfferComparison      | 5            |
| Widget en detalle de vehículo  | FinanceWidget        | 5            |
| Dashboard de solicitudes       | FinanceApplications  | 5            |
| Email de aprobación/rechazo    | NotificationService  | 3            |

**Story Points Sprint 39:** 34

### Entregables Sprint 38-39

```
✅ Calculadora de cuotas (12-72 meses)
✅ Pre-calificación en 2 minutos
✅ Envío de solicitud a banco(s)
✅ Comparación de ofertas
✅ Tracking de estado de solicitud
✅ Partnership con 2+ bancos
```

**Story Points Total:** 65

---

## Sprint 40-42 (Semanas 79-84) - 📱 Mobile App (Flutter)

**Objetivo:** App nativa iOS/Android con Flutter

### Ya Existe

- Carpeta `frontend/mobile/cardealer/` con código Flutter
- Necesita completarse e integrarse

### Sprint 40: Core Features

| Task                                 | Story Points |
| ------------------------------------ | ------------ |
| Setup proyecto Flutter actualizado   | 5            |
| Autenticación (login, register, JWT) | 8            |
| Navegación y routing                 | 5            |
| Home con secciones                   | 8            |
| Búsqueda y filtros                   | 8            |
| Detalle de vehículo                  | 8            |

**Story Points Sprint 40:** 42

### Sprint 41: Features Avanzados

| Task                          | Story Points |
| ----------------------------- | ------------ |
| Favoritos sincronizados       | 5            |
| Push notifications (Firebase) | 8            |
| Chat/Mensajería               | 8            |
| Perfil de usuario             | 5            |
| Publicar vehículo (wizard)    | 13           |
| Cámara para fotos             | 5            |

**Story Points Sprint 41:** 44

### Sprint 42: Polish & Launch

| Task                            | Story Points |
| ------------------------------- | ------------ |
| Dashboard dealer (móvil)        | 8            |
| Offline mode básico             | 5            |
| Deep linking                    | 5            |
| App Store optimization (ASO)    | 3            |
| TestFlight / Play Console setup | 3            |
| Bug fixes y QA                  | 8            |

**Story Points Sprint 42:** 32

### Entregables Sprint 40-42

```
✅ App iOS publicada en App Store
✅ App Android publicada en Play Store
✅ Todas las features del web (excepto admin)
✅ Push notifications
✅ Cámara nativa para fotos
✅ Deep links funcionando
```

**Story Points Total:** 118

---

## Sprint 43 (Semanas 85-86) - 🔐 Verificación de Identidad

**Objetivo:** Prevenir fraude verificando identidad de vendedores

### Backend Tasks

| Task                                     | Servicio                    | Story Points |
| ---------------------------------------- | --------------------------- | ------------ |
| IdentityVerificationService base         | Nuevo servicio              | 8            |
| Verificación de cédula (OCR)             | IdentityVerificationService | 8            |
| Liveness detection (selfie)              | IdentityVerificationService | 8            |
| Match cédula vs selfie                   | IdentityVerificationService | 5            |
| Integración con proveedor (Jumio/Onfido) | IdentityVerificationService | 8            |
| Almacenamiento seguro (encriptado)       | IdentityVerificationService | 5            |

### Frontend Tasks

| Task                             | Componente            | Story Points |
| -------------------------------- | --------------------- | ------------ |
| Flujo de verificación            | VerificationFlow      | 8            |
| Captura de cédula (frente/dorso) | IDCapture             | 5            |
| Captura de selfie                | SelfieCapture         | 5            |
| Status de verificación           | VerificationStatus    | 3            |
| Badge "Identidad Verificada"     | VerifiedIdentityBadge | 2            |

### Entregables Sprint 43

```
✅ Vendedores pueden verificar identidad
✅ OCR extrae datos de cédula
✅ Liveness detection anti-fraude
✅ Match facial cédula ↔ selfie
✅ Badge "Identidad Verificada" en perfil
✅ Datos encriptados y seguros
```

**Story Points Total:** 65

---

## Sprint 44 (Semanas 87-88) - 🔄 Trade-In / Retoma

**Objetivo:** Dealers pueden ofrecer trade-in a compradores

### Backend Tasks

| Task                              | Servicio            | Story Points |
| --------------------------------- | ------------------- | ------------ |
| TradeInService base (5043)        | Nuevo servicio      | 8            |
| Valuación automática (ML pricing) | TradeInService      | 5            |
| CRUD de solicitudes trade-in      | TradeInService      | 5            |
| Workflow de aprobación            | TradeInService      | 5            |
| Notificaciones al dealer          | NotificationService | 3            |

### Frontend Tasks

| Task                            | Componente       | Story Points |
| ------------------------------- | ---------------- | ------------ |
| Formulario trade-in (comprador) | TradeInForm      | 8            |
| Widget en detalle vehículo      | TradeInWidget    | 5            |
| Dashboard trade-ins (dealer)    | TradeInDashboard | 8            |
| Valuación estimada              | ValuationResult  | 5            |
| Aceptar/rechazar trade-in       | TradeInActions   | 3            |

### Entregables Sprint 44

```
✅ Comprador ingresa datos de su vehículo actual
✅ Sistema calcula valuación estimada
✅ Dealer recibe solicitud de trade-in
✅ Dealer aprueba/rechaza/contraoferta
✅ Comprador ve descuento en checkout
```

**Story Points Total:** 55

---

## Sprint 45 (Semanas 89-90) - 🎁 Programa de Referidos

**Objetivo:** Growth hacking mediante referidos

### Backend Tasks

| Task                           | Servicio        | Story Points |
| ------------------------------ | --------------- | ------------ |
| ReferralService base           | Nuevo servicio  | 8            |
| Generación de códigos únicos   | ReferralService | 3            |
| Tracking de referidos          | ReferralService | 5            |
| Sistema de rewards             | ReferralService | 5            |
| Integración con BillingService | ReferralService | 5            |
| Anti-fraude de referidos       | ReferralService | 5            |

### Frontend Tasks

| Task                   | Componente          | Story Points |
| ---------------------- | ------------------- | ------------ |
| Página "Invita y Gana" | ReferralPage        | 8            |
| Widget de compartir    | ShareWidget         | 5            |
| Dashboard de referidos | ReferralDashboard   | 5            |
| Historial de rewards   | RewardsHistory      | 3            |
| Leaderboard (opcional) | ReferralLeaderboard | 5            |

### Estructura de Rewards

```
COMPRADOR REFIERE A COMPRADOR:
├── Referidor: RD$500 crédito cuando referido compra
└── Referido: RD$500 descuento en primera compra

VENDEDOR REFIERE A VENDEDOR:
├── Referidor: 1 listing gratis
└── Referido: 50% descuento primer listing

DEALER REFIERE A DEALER:
├── Referidor: 1 mes gratis
└── Referido: 1 mes gratis
```

### Entregables Sprint 45

```
✅ Códigos de referido únicos
✅ Tracking de conversiones
✅ Rewards automáticos
✅ Dashboard de referidos
✅ Compartir por WhatsApp, email, link
✅ Anti-fraude básico
```

**Story Points Total:** 57

---

## Sprint 46 (Semanas 91-92) - 💱 Multi-Moneda (DOP/USD)

**Objetivo:** Soportar precios en pesos y dólares

### Backend Tasks

| Task                                     | Servicio            | Story Points |
| ---------------------------------------- | ------------------- | ------------ |
| CurrencyService base                     | Nuevo servicio      | 5            |
| API de tasa de cambio (Banco Central RD) | CurrencyService     | 5            |
| Conversión automática                    | CurrencyService     | 3            |
| Preferencia de moneda por usuario        | UserService         | 3            |
| Precios en ambas monedas (DB)            | VehiclesSaleService | 5            |
| Billing en moneda seleccionada           | BillingService      | 5            |

### Frontend Tasks

| Task                         | Componente         | Story Points |
| ---------------------------- | ------------------ | ------------ |
| Selector de moneda (header)  | CurrencySelector   | 3            |
| Mostrar precios en ambas     | DualPriceDisplay   | 5            |
| Filtros de precio por moneda | PriceFilter        | 3            |
| Preferencia en settings      | CurrencyPreference | 2            |

### Entregables Sprint 46

```
✅ Tasa de cambio actualizada diariamente
✅ Usuario puede ver precios en DOP o USD
✅ Publicar en cualquier moneda
✅ Conversión automática
✅ Filtros de precio funcionan en ambas
✅ Facturación en moneda preferida
```

**Story Points Total:** 39

---

# 📅 FASE 8: MARKETPLACE AVANZADO

---

## Sprint 47 (Semanas 93-94) - 🔨 Subastas de Vehículos

**Objetivo:** Modelo de venta por subasta (opcional para vendedores)

### Backend Tasks

| Task                           | Servicio            | Story Points |
| ------------------------------ | ------------------- | ------------ |
| AuctionService base            | Nuevo servicio      | 8            |
| Crear subasta (vendedor)       | AuctionService      | 5            |
| Sistema de pujas (real-time)   | AuctionService      | 8            |
| Auto-extensión (últimos 5 min) | AuctionService      | 3            |
| Cierre y ganador               | AuctionService      | 5            |
| WebSocket para pujas live      | AuctionService      | 8            |
| Depósito de garantía           | BillingService      | 5            |
| Notificaciones de puja         | NotificationService | 3            |

### Frontend Tasks

| Task                           | Componente          | Story Points |
| ------------------------------ | ------------------- | ------------ |
| Página de subasta              | AuctionPage         | 8            |
| Widget de pujas en tiempo real | LiveBidding         | 8            |
| Historial de pujas             | BidHistory          | 5            |
| Countdown timer                | AuctionTimer        | 3            |
| Mis subastas (vendedor)        | MyAuctions          | 5            |
| Mis pujas (comprador)          | MyBids              | 5            |
| Crear subasta wizard           | CreateAuctionWizard | 8            |

### Entregables Sprint 47

```
✅ Vendedor puede crear subasta
✅ Precio inicial y reserva
✅ Duración configurable (1-7 días)
✅ Pujas en tiempo real (WebSocket)
✅ Auto-extensión en últimos 5 min
✅ Notificaciones de superación de puja
✅ Depósito de garantía del ganador
```

**Story Points Total:** 87

---

## Sprint 48 (Semanas 95-96) - 🛡️ Garantías Extendidas

**Objetivo:** Vender garantías extendidas en vehículos usados

### Backend Tasks

| Task                            | Servicio                  | Story Points |
| ------------------------------- | ------------------------- | ------------ |
| WarrantyService base (5044)     | Nuevo servicio            | 8            |
| Planes de garantía              | WarrantyService           | 5            |
| Pricing dinámico (por vehículo) | WarrantyService           | 5            |
| Compra de garantía              | WarrantyService + Billing | 5            |
| Reclamaciones                   | WarrantyService           | 5            |
| Partnership con aseguradoras    | Legal                     | -            |

### Frontend Tasks

| Task                      | Componente     | Story Points |
| ------------------------- | -------------- | ------------ |
| Widget en checkout        | WarrantyWidget | 5            |
| Comparador de planes      | WarrantyPlans  | 5            |
| Mi garantía (dashboard)   | MyWarranty     | 5            |
| Formulario de reclamación | ClaimForm      | 5            |

### Planes de Garantía

```
BÁSICA (6 meses - $5,000 DOP):
├── Motor y transmisión
└── Límite: $50,000 DOP

ESTÁNDAR (12 meses - $12,000 DOP):
├── Motor, transmisión, A/C
├── Eléctricos principales
└── Límite: $100,000 DOP

PREMIUM (24 meses - $25,000 DOP):
├── Todo lo mecánico
├── Eléctricos completos
├── Asistencia en carretera
└── Límite: $200,000 DOP
```

### Entregables Sprint 48

```
✅ 3 planes de garantía
✅ Pricing dinámico por vehículo
✅ Checkout integrado
✅ Dashboard de garantía activa
✅ Sistema de reclamaciones
✅ Partnership con proveedor de garantías
```

**Story Points Total:** 48

---

## Sprint 49 (Semanas 97-98) - 🚗 Integración de Seguros

**Objetivo:** Cotizar y comprar seguro vehicular desde OKLA

### Backend Tasks

| Task                                | Servicio                   | Story Points |
| ----------------------------------- | -------------------------- | ------------ |
| InsuranceService base               | Nuevo servicio             | 8            |
| API de cotización multi-aseguradora | InsuranceService           | 8            |
| Comparador de pólizas               | InsuranceService           | 5            |
| Compra de seguro                    | InsuranceService + Billing | 5            |
| Webhook de emisión                  | InsuranceService           | 5            |

### Aseguradoras Target (RD)

| Aseguradora         | API         | Prioridad |
| ------------------- | ----------- | --------- |
| Seguros Reservas    | Por definir | Alta      |
| Seguros Universal   | Por definir | Alta      |
| Seguros Banreservas | Por definir | Media     |
| Mapfre BHD          | Por definir | Media     |

### Frontend Tasks

| Task                          | Componente          | Story Points |
| ----------------------------- | ------------------- | ------------ |
| Widget cotización en checkout | InsuranceQuote      | 8            |
| Comparador de pólizas         | InsuranceComparison | 5            |
| Formulario de compra          | InsurancePurchase   | 5            |
| Mis seguros (dashboard)       | MyInsurance         | 5            |

### Entregables Sprint 49

```
✅ Cotización en tiempo real de 2+ aseguradoras
✅ Comparador lado a lado
✅ Compra de póliza sin salir de OKLA
✅ Comisión por referencia (5-10%)
✅ Dashboard de pólizas activas
```

**Story Points Total:** 54

---

## Sprint 50 (Semanas 99-100) - 📝 Blog & Content Marketing

**Objetivo:** SEO content para atraer tráfico orgánico

### Backend Tasks

| Task                             | Servicio       | Story Points |
| -------------------------------- | -------------- | ------------ |
| BlogService base                 | Nuevo servicio | 5            |
| CRUD de artículos                | BlogService    | 5            |
| Categorías y tags                | BlogService    | 3            |
| SEO automático (meta, slug)      | BlogService    | 3            |
| Relacionar artículos ↔ vehículos | BlogService    | 5            |

### Frontend Tasks

| Task                               | Componente      | Story Points |
| ---------------------------------- | --------------- | ------------ |
| Página de blog                     | BlogPage        | 8            |
| Artículo individual                | ArticlePage     | 5            |
| Sidebar de artículos relacionados  | RelatedArticles | 3            |
| CTA en artículos (buscar vehículo) | ArticleCTA      | 3            |
| Admin: Editor de artículos         | ArticleEditor   | 8            |

### Contenido Inicial (30 artículos)

```
GUÍAS DE COMPRA:
├── "Cómo comprar tu primer auto en RD"
├── "Guía de financiamiento vehicular 2026"
├── "Usados vs nuevos: ¿Qué conviene?"
└── 10 artículos más...

REVIEWS DE MODELOS:
├── "Toyota Corolla 2024 - Review completo"
├── "Hyundai Tucson vs Honda CR-V"
└── 8 artículos más...

MANTENIMIENTO:
├── "Calendario de mantenimiento preventivo"
├── "Cómo preparar tu auto para la venta"
└── 5 artículos más...
```

### Entregables Sprint 50

```
✅ Blog con 30 artículos iniciales
✅ SEO optimizado por artículo
✅ Categorías: Guías, Reviews, Mantenimiento, Noticias
✅ CTAs integrados ("Ver Toyota Corolla →")
✅ Admin editor WYSIWYG
✅ RSS feed
```

**Story Points Total:** 48

---

## Sprint 51 (Semanas 101-102) - 🌐 Internacionalización (i18n)

**Objetivo:** Soporte multi-idioma (Español, Inglés)

### Backend Tasks

| Task                                      | Servicio            | Story Points |
| ----------------------------------------- | ------------------- | ------------ |
| Middleware de idioma                      | Gateway             | 3            |
| Respuestas traducidas (errores, mensajes) | Todos los servicios | 8            |
| Emails en idioma preferido                | NotificationService | 5            |

### Frontend Tasks

| Task                             | Componente         | Story Points |
| -------------------------------- | ------------------ | ------------ |
| Setup i18next/react-intl         | -                  | 5            |
| Extraer strings (500+)           | -                  | 8            |
| Traducción ES → EN               | -                  | 5            |
| Selector de idioma               | LanguageSelector   | 3            |
| Preferencia de idioma (settings) | LanguagePreference | 2            |
| SEO multi-idioma (hreflang)      | SEOService         | 5            |

### Estructura de URLs

```
okla.com.do/es/vehiculos/toyota    → Español (default)
okla.com.do/en/vehicles/toyota     → English
```

### Entregables Sprint 51

```
✅ Toda la UI en Español e Inglés
✅ Emails en idioma preferido
✅ Selector de idioma persistente
✅ URLs localizadas
✅ hreflang tags para SEO
✅ Detección automática de idioma
```

**Story Points Total:** 44

---

## Sprint 52 (Semanas 103-104) - 📊 Advanced Analytics (Admin)

**Objetivo:** Dashboard ejecutivo para dueños de OKLA

### Backend Tasks

| Task                            | Servicio                 | Story Points |
| ------------------------------- | ------------------------ | ------------ |
| PlatformAnalyticsService (5068) | Nuevo servicio           | 8            |
| Métricas de negocio agregadas   | PlatformAnalyticsService | 8            |
| Export a CSV/Excel              | PlatformAnalyticsService | 3            |
| Scheduled reports (email)       | PlatformAnalyticsService | 5            |
| Cohort analysis                 | PlatformAnalyticsService | 5            |

### Frontend Tasks

| Task                             | Componente         | Story Points |
| -------------------------------- | ------------------ | ------------ |
| Dashboard ejecutivo              | ExecutiveDashboard | 13           |
| Gráficos interactivos (Recharts) | AnalyticsCharts    | 8            |
| Selector de período              | DateRangePicker    | 3            |
| Export buttons                   | ExportButtons      | 2            |
| Comparación períodos             | PeriodComparison   | 5            |

### Métricas del Dashboard Ejecutivo

```
MÉTRICAS PRINCIPALES:
├── GMV (Gross Merchandise Value)
├── Revenue (comisiones + suscripciones)
├── Active Users (DAU/MAU)
├── Active Listings
├── Conversion Rate (vista → contacto → venta)
└── NPS Score

GRÁFICOS:
├── Revenue over time
├── User growth
├── Listing growth
├── Top dealers by GMV
├── Top marcas/modelos
└── Cohort retention

ALERTAS:
├── Caída de revenue > 10%
├── Churn de dealers > 5%
├── Caída de listings > 20%
└── NPS < 50
```

### Entregables Sprint 52

```
✅ Dashboard ejecutivo completo
✅ Gráficos interactivos
✅ Filtros por período
✅ Export CSV/Excel
✅ Scheduled email reports (semanal)
✅ Alertas automáticas
✅ Comparación año vs año
```

**Story Points Total:** 60

---

# RESUMEN FINAL COMPLETO

| Fase          | Sprints | SP         | Semanas |
| ------------- | ------- | ---------- | ------- |
| 1 - MVP       | 1-4.6   | ~349       | 1-12    |
| 2 - Dealers   | 5-8     | ~233       | 13-20   |
| 3 - Analytics | 9-12    | ~206       | 21-28   |
| 4 - IA        | 13-18   | ~270       | 29-40   |
| 5 - ML        | 19-29   | ~409       | 41-62   |
| 6 - Vision    | 30-34   | ~192       | 63-72   |
| 7 - Growth    | 35-46   | ~651       | 73-96   |
| 8 - Avanzado  | 47-52   | ~341       | 97-108  |
| **TOTAL**     | **53**  | **~2,651** | **108** |

> ⚠️ **Sprints 4.5-4.6 (DGII + Ley 358-05):** Requisitos legales OBLIGATORIOS para operar en RD

---

## 📈 Resumen por Área

| Área                 | Sprints | Descripción                      |
| -------------------- | ------- | -------------------------------- |
| **Marketplace Core** | 1-4     | Búsqueda, publicación, favoritos |
| **Facturación DGII** | 4.5     | e-CF, NCF, comprobantes fiscales |
| **Ley 358-05**       | 4.6     | Comercio electrónico, contratos  |
| **Dealers/B2B**      | 5-8     | Suscripciones, inventario        |
| **Analytics**        | 9-12    | Estadísticas, métricas           |
| **IA/Chatbot**       | 13-18   | Chatbot, leads                   |
| **ML Training**      | 19-29   | Pricing, recomendaciones         |
| **Computer Vision**  | 30-34   | Validación de imágenes           |
| **SEO/Growth**       | 35-46   | SEO, mobile, referidos           |
| **Avanzado**         | 47-52   | Subastas, seguros, i18n          |

---

## ⚖️ Cumplimiento Legal RD (CRÍTICO)

| Sprint  | Ley        | Requisitos                                              |
| ------- | ---------- | ------------------------------------------------------- |
| **4.5** | DGII e-CF  | Comprobantes fiscales electrónicos, certificado digital |
| **4.6** | Ley 358-05 | Comercio electrónico, T&C, archivo 10 años, Art. 21     |

---

## 💰 Estimación de Costos Mensuales (Producción)

| Servicio                           | Costo/Mes          |
| ---------------------------------- | ------------------ |
| DOKS (6 nodes)                     | ~$120              |
| Managed PostgreSQL                 | ~$30               |
| Spaces (S3)                        | ~$20               |
| Load Balancer                      | ~$12               |
| ML Infrastructure (Qdrant, Ollama) | ~$40-80            |
| Emails (SendGrid)                  | ~$15               |
| SMS (Twilio)                       | ~$20-50            |
| Certificado DGII (anual)           | ~$5/mes (~$60/año) |
| Abogado documentos legales (único) | ~$500-1000 inicial |
| **TOTAL**                          | **~$265-335/mes**  |

---

## 🗓️ Timeline General

```
2026:
├── Q1 (Ene-Mar): Sprints 1-6   → MVP + e-CF DGII + Ley 358-05 + Dealers base
├── Q2 (Abr-Jun): Sprints 7-12  → Dealers + Analytics
├── Q3 (Jul-Sep): Sprints 13-18 → IA/Chatbot
├── Q4 (Oct-Dic): Sprints 19-26 → ML Training

2027:
├── Q1 (Ene-Mar): Sprints 27-34 → ML + Computer Vision
├── Q2 (Abr-Jun): Sprints 35-42 → SEO + Mobile App
├── Q3 (Jul-Sep): Sprints 43-48 → Referidos + Subastas
├── Q4 (Oct-Dic): Sprints 49-52 → Seguros + i18n + Polish
```

---

## 🎯 Hitos Principales

| Mes                 | Hito             | Descripción                 |
| ------------------- | ---------------- | --------------------------- |
| **Marzo 2026**      | 🚀 MVP Launch    | Marketplace funcional       |
| **Junio 2026**      | 🏢 Dealers Live  | Sistema de dealers completo |
| **Septiembre 2026** | 🤖 Chatbot Live  | IA de soporte funcionando   |
| **Diciembre 2026**  | 🧠 ML v1         | Pricing y recomendaciones   |
| **Marzo 2027**      | 📸 Vision Live   | Validación de imágenes AI   |
| **Junio 2027**      | 📱 Mobile Launch | App iOS/Android             |
| **Diciembre 2027**  | 🌟 Full Platform | Todas las features          |

---

_Actualizado: Enero 9, 2026 - Plan Completo 52 Sprints_  
_Autor: Equipo OKLA_
