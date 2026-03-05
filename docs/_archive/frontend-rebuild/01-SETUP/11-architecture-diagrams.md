# 🏗️ Diagramas de Arquitectura

> **Tiempo estimado:** 20 minutos de lectura
> **Propósito:** Visualizar la arquitectura del sistema completo
> **Última actualización:** Enero 31, 2026

---

## 📋 OBJETIVO

Proporcionar diagramas visuales de:

- Arquitectura general del sistema
- Flujo de datos Frontend ↔ Backend
- Estructura de microservicios
- Flujos de autenticación
- Patrones de comunicación

---

## 🎯 ARQUITECTURA GENERAL DEL SISTEMA

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                              OKLA - ARQUITECTURA GENERAL                             │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐    │
│  │                              CLIENTES                                        │    │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │    │
│  │  │   Browser    │  │  Mobile App  │  │  Third-party │  │   Admin      │     │    │
│  │  │  (Next.js)   │  │  (Flutter)   │  │     APIs     │  │   Portal     │     │    │
│  │  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘     │    │
│  └─────────┼─────────────────┼─────────────────┼─────────────────┼─────────────┘    │
│            │                 │                 │                 │                   │
│            └─────────────────┴────────┬────────┴─────────────────┘                   │
│                                       │                                              │
│                                       ▼                                              │
│  ┌─────────────────────────────────────────────────────────────────────────────┐    │
│  │                        CLOUDFLARE / CDN                                      │    │
│  │  • DDoS Protection  • WAF  • SSL Termination  • Edge Caching                │    │
│  └─────────────────────────────────────────────────────────────────────────────┘    │
│                                       │                                              │
│                                       ▼                                              │
│  ┌─────────────────────────────────────────────────────────────────────────────┐    │
│  │                     DIGITAL OCEAN LOAD BALANCER                              │    │
│  │                        146.190.199.0 (Production)                            │    │
│  └─────────────────────────────────────────────────────────────────────────────┘    │
│                                       │                                              │
│            ┌──────────────────────────┼──────────────────────────┐                   │
│            │                          │                          │                   │
│            ▼                          ▼                          ▼                   │
│  ┌─────────────────┐       ┌─────────────────┐       ┌─────────────────┐            │
│  │   okla.com.do   │       │api.okla.com.do  │       │cdn.okla.com.do  │            │
│  │   (Frontend)    │       │   (Gateway)     │       │   (Media/S3)    │            │
│  └─────────────────┘       └────────┬────────┘       └─────────────────┘            │
│                                     │                                                │
│  ═══════════════════════════════════╪════════════════════════════════════════════   │
│                                     │                                                │
│  ┌──────────────────────────────────┼───────────────────────────────────────────┐   │
│  │                    KUBERNETES CLUSTER (DOKS)                                  │   │
│  │                       Namespace: okla                                         │   │
│  │                                  │                                            │   │
│  │  ┌───────────────────────────────┼───────────────────────────────────────┐   │   │
│  │  │                        API GATEWAY (Ocelot)                            │   │   │
│  │  │                         Port: 8080                                     │   │   │
│  │  │  • Routing  • Rate Limiting  • Auth Validation  • Load Balancing      │   │   │
│  │  └───────────────────────────────┬───────────────────────────────────────┘   │   │
│  │                                  │                                            │   │
│  │     ┌────────────┬───────────────┼───────────────┬────────────┐              │   │
│  │     │            │               │               │            │              │   │
│  │     ▼            ▼               ▼               ▼            ▼              │   │
│  │  ┌──────┐    ┌──────┐       ┌──────┐       ┌──────┐    ┌──────┐             │   │
│  │  │Auth  │    │User  │       │Vehicle│      │Billing│   │Media │             │   │
│  │  │Svc   │    │Svc   │       │Svc   │       │Svc   │    │Svc   │             │   │
│  │  │:8080 │    │:8080 │       │:8080 │       │:8080 │    │:8080 │             │   │
│  │  └──┬───┘    └──┬───┘       └──┬───┘       └──┬───┘    └──┬───┘             │   │
│  │     │           │              │              │           │                  │   │
│  │     │     ┌─────┴──────────────┴──────────────┴───────────┘                  │   │
│  │     │     │                                                                   │   │
│  │     ▼     ▼                                                                   │   │
│  │  ┌────────────────────────────────────────────────────────────────────────┐  │   │
│  │  │                        INFRASTRUCTURE                                   │  │   │
│  │  │                                                                         │  │   │
│  │  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                  │  │   │
│  │  │  │  PostgreSQL  │  │    Redis     │  │   RabbitMQ   │                  │  │   │
│  │  │  │    :5432     │  │    :6379     │  │    :5672     │                  │  │   │
│  │  │  │  (Primary)   │  │   (Cache)    │  │  (Messages)  │                  │  │   │
│  │  │  └──────────────┘  └──────────────┘  └──────────────┘                  │  │   │
│  │  │                                                                         │  │   │
│  │  └────────────────────────────────────────────────────────────────────────┘  │   │
│  │                                                                               │   │
│  └───────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔄 FLUJO DE DATOS FRONTEND → BACKEND

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                         FLUJO DE REQUEST/RESPONSE                                    │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  FRONTEND (Next.js)                                                                  │
│  ═══════════════════                                                                 │
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                              │    │
│  │   1. COMPONENT                    2. HOOK                    3. SERVICE     │    │
│  │   ┌──────────────┐               ┌──────────────┐           ┌──────────────┐│    │
│  │   │ VehiclePage  │ ──onClick──▶  │ useVehicle   │ ─────────▶│ vehicleApi   ││    │
│  │   │              │               │   Query      │           │   .get()     ││    │
│  │   └──────────────┘               └──────────────┘           └──────┬───────┘│    │
│  │         ▲                              │                           │        │    │
│  │         │                              │                           │        │    │
│  │   6. RE-RENDER                   5. CACHE                          │        │    │
│  │   con nuevos datos               UPDATE                            │        │    │
│  │                                                                    │        │    │
│  └────────────────────────────────────────────────────────────────────┼────────┘    │
│                                                                       │              │
│  ════════════════════════════════════════════════════════════════════╪══════════    │
│                                                                       │              │
│  HTTP REQUEST                                                         │              │
│  ═══════════════                                                      │              │
│                                                                       ▼              │
│  ┌─────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                              │    │
│  │   GET https://api.okla.com.do/api/vehicles/toyota-camry-2024                │    │
│  │                                                                              │    │
│  │   Headers:                                                                   │    │
│  │   ├── Authorization: Bearer eyJhbGciOiJIUzI1NiIs...                         │    │
│  │   ├── X-Correlation-Id: 550e8400-e29b-41d4-a716-446655440000                │    │
│  │   ├── Accept-Language: es-DO                                                │    │
│  │   └── X-Client-Version: 1.0.0                                               │    │
│  │                                                                              │    │
│  └─────────────────────────────────────────────────────────────────────────────┘    │
│                                                                       │              │
│  ════════════════════════════════════════════════════════════════════╪══════════    │
│                                                                       │              │
│  BACKEND (Gateway → Microservices)                                    │              │
│  ═══════════════════════════════════                                  │              │
│                                                                       ▼              │
│  ┌─────────────────────────────────────────────────────────────────────────────┐    │
│  │  OCELOT GATEWAY                                                              │    │
│  │  ┌─────────────────────────────────────────────────────────────────────┐    │    │
│  │  │ 1. Rate Limiting Check                                               │    │    │
│  │  │ 2. JWT Validation (if required)                                      │    │    │
│  │  │ 3. Route Matching: /api/vehicles/* → vehiclessaleservice:8080        │    │    │
│  │  │ 4. Load Balancing (if multiple replicas)                             │    │    │
│  │  │ 5. Request Forwarding                                                │    │    │
│  │  └─────────────────────────────────────────────────────────────────────┘    │    │
│  └────────────────────────────────────────┬────────────────────────────────────┘    │
│                                           │                                          │
│                                           ▼                                          │
│  ┌─────────────────────────────────────────────────────────────────────────────┐    │
│  │  VEHICLES SALE SERVICE                                                       │    │
│  │  ┌─────────────────────────────────────────────────────────────────────┐    │    │
│  │  │                                                                      │    │    │
│  │  │  Controller          Application          Infrastructure            │    │    │
│  │  │  ┌─────────┐        ┌─────────┐          ┌─────────┐               │    │    │
│  │  │  │ GET /   │───────▶│ Query   │─────────▶│ EF Core │───▶ PostgreSQL│    │    │
│  │  │  │ {slug}  │        │ Handler │          │ DbContext│              │    │    │
│  │  │  └─────────┘        └─────────┘          └─────────┘               │    │    │
│  │  │       │                  │                    │                     │    │    │
│  │  │       │                  │                    ▼                     │    │    │
│  │  │       │                  │              ┌─────────┐                │    │    │
│  │  │       │                  │              │  Redis  │ (Cache Check) │    │    │
│  │  │       │                  │              └─────────┘                │    │    │
│  │  │       │                  │                                          │    │    │
│  │  └───────┼──────────────────┼──────────────────────────────────────────┘    │    │
│  │          │                  │                                               │    │
│  └──────────┼──────────────────┼───────────────────────────────────────────────┘    │
│             │                  │                                                     │
│  ═══════════╪══════════════════╪═════════════════════════════════════════════════   │
│             │                  │                                                     │
│  HTTP RESPONSE                 │                                                     │
│  ══════════════                │                                                     │
│             ▼                  │                                                     │
│  ┌─────────────────────────────────────────────────────────────────────────────┐    │
│  │                                                                              │    │
│  │   HTTP/1.1 200 OK                                                           │    │
│  │                                                                              │    │
│  │   Headers:                                                                   │    │
│  │   ├── Content-Type: application/json                                        │    │
│  │   ├── X-Correlation-Id: 550e8400-e29b-41d4-a716-446655440000                │    │
│  │   ├── X-RateLimit-Remaining: 99                                             │    │
│  │   └── Cache-Control: public, max-age=60                                     │    │
│  │                                                                              │    │
│  │   Body:                                                                      │    │
│  │   {                                                                          │    │
│  │     "success": true,                                                        │    │
│  │     "data": {                                                               │    │
│  │       "id": "uuid",                                                         │    │
│  │       "make": "Toyota",                                                     │    │
│  │       "model": "Camry",                                                     │    │
│  │       "year": 2024,                                                         │    │
│  │       "price": 2500000,                                                     │    │
│  │       ...                                                                   │    │
│  │     }                                                                        │    │
│  │   }                                                                          │    │
│  │                                                                              │    │
│  └─────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔐 FLUJO DE AUTENTICACIÓN

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                           FLUJO DE AUTENTICACIÓN JWT                                 │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐    │
│  │                          1. LOGIN FLOW                                       │    │
│  │                                                                              │    │
│  │   Browser                    Gateway                   AuthService           │    │
│  │      │                          │                          │                 │    │
│  │      │  POST /api/auth/login    │                          │                 │    │
│  │      │  {email, password}       │                          │                 │    │
│  │      │ ─────────────────────────▶                          │                 │    │
│  │      │                          │  POST /api/auth/login    │                 │    │
│  │      │                          │ ─────────────────────────▶                 │    │
│  │      │                          │                          │                 │    │
│  │      │                          │        Validate          │                 │    │
│  │      │                          │        Credentials       │                 │    │
│  │      │                          │           │              │                 │    │
│  │      │                          │           ▼              │                 │    │
│  │      │                          │    ┌─────────────┐       │                 │    │
│  │      │                          │    │ Generate    │       │                 │    │
│  │      │                          │    │ JWT Token   │       │                 │    │
│  │      │                          │    │ + Refresh   │       │                 │    │
│  │      │                          │    └─────────────┘       │                 │    │
│  │      │                          │                          │                 │    │
│  │      │                          │  {accessToken,           │                 │    │
│  │      │                          │   refreshToken,          │                 │    │
│  │      │                          │   expiresIn}             │                 │    │
│  │      │                          │ ◀─────────────────────────                 │    │
│  │      │                          │                          │                 │    │
│  │      │  200 OK                  │                          │                 │    │
│  │      │  Set-Cookie: refreshToken│                          │                 │    │
│  │      │ ◀─────────────────────────                          │                 │    │
│  │      │                          │                          │                 │    │
│  │      │  Store accessToken       │                          │                 │    │
│  │      │  in memory/zustand       │                          │                 │    │
│  │      │                          │                          │                 │    │
│  └──────┼──────────────────────────┼──────────────────────────┼─────────────────┘    │
│         │                          │                          │                      │
│  ┌──────┼──────────────────────────┼──────────────────────────┼─────────────────┐    │
│  │      │              2. AUTHENTICATED REQUEST               │                 │    │
│  │      │                          │                          │                 │    │
│  │      │  GET /api/user/profile   │                          │                 │    │
│  │      │  Authorization: Bearer   │                          │                 │    │
│  │      │ ─────────────────────────▶                          │                 │    │
│  │      │                          │                          │                 │    │
│  │      │                    ┌─────────────┐                  │                 │    │
│  │      │                    │ Validate    │                  │                 │    │
│  │      │                    │ JWT Token   │                  │                 │    │
│  │      │                    │ (Signature, │                  │                 │    │
│  │      │                    │  Expiry,    │                  │                 │    │
│  │      │                    │  Claims)    │                  │                 │    │
│  │      │                    └─────────────┘                  │                 │    │
│  │      │                          │                          │                 │    │
│  │      │                          │  Forward to UserService  │                 │    │
│  │      │                          │ ─────────────────────────▶                 │    │
│  │      │                          │                          │                 │    │
│  │      │  200 OK {profile}        │                          │                 │    │
│  │      │ ◀─────────────────────────◀─────────────────────────                  │    │
│  │      │                          │                          │                 │    │
│  └──────┼──────────────────────────┼──────────────────────────┼─────────────────┘    │
│         │                          │                          │                      │
│  ┌──────┼──────────────────────────┼──────────────────────────┼─────────────────┐    │
│  │      │              3. TOKEN REFRESH FLOW                  │                 │    │
│  │      │                          │                          │                 │    │
│  │      │  GET /api/vehicles       │                          │                 │    │
│  │      │  Authorization: Bearer   │                          │                 │    │
│  │      │  (EXPIRED TOKEN)         │                          │                 │    │
│  │      │ ─────────────────────────▶                          │                 │    │
│  │      │                          │                          │                 │    │
│  │      │  401 Unauthorized        │                          │                 │    │
│  │      │  {error: "TOKEN_EXPIRED"}│                          │                 │    │
│  │      │ ◀─────────────────────────                          │                 │    │
│  │      │                          │                          │                 │    │
│  │      │  ┌──────────────────┐    │                          │                 │    │
│  │      │  │ Axios Interceptor│    │                          │                 │    │
│  │      │  │ Auto-refresh     │    │                          │                 │    │
│  │      │  └────────┬─────────┘    │                          │                 │    │
│  │      │           │              │                          │                 │    │
│  │      │  POST /api/auth/refresh  │                          │                 │    │
│  │      │  Cookie: refreshToken    │                          │                 │    │
│  │      │ ─────────────────────────▶                          │                 │    │
│  │      │                          │ ─────────────────────────▶                 │    │
│  │      │                          │                          │                 │    │
│  │      │  200 OK {newAccessToken} │                          │                 │    │
│  │      │ ◀─────────────────────────◀─────────────────────────                  │    │
│  │      │                          │                          │                 │    │
│  │      │  Retry original request  │                          │                 │    │
│  │      │  with new token          │                          │                 │    │
│  │      │ ─────────────────────────▶                          │                 │    │
│  │      │                          │                          │                 │    │
│  │      │  200 OK {data}           │                          │                 │    │
│  │      │ ◀─────────────────────────                          │                 │    │
│  │      │                          │                          │                 │    │
│  └──────┴──────────────────────────┴──────────────────────────┴─────────────────┘    │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 🏢 ESTRUCTURA DE MICROSERVICIOS

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                          MICROSERVICIOS OKLA                                         │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐    │
│  │                           CORE SERVICES                                      │    │
│  │                                                                              │    │
│  │  ┌───────────────┐  ┌───────────────┐  ┌───────────────┐  ┌──────────────┐  │    │
│  │  │  AuthService  │  │  UserService  │  │  RoleService  │  │ AdminService │  │    │
│  │  │               │  │               │  │               │  │              │  │    │
│  │  │ • Login       │  │ • Profile     │  │ • RBAC        │  │ • Dashboard  │  │    │
│  │  │ • Register    │  │ • Settings    │  │ • Permissions │  │ • Moderation │  │    │
│  │  │ • 2FA         │  │ • Preferences │  │ • Roles CRUD  │  │ • Reports    │  │    │
│  │  │ • OAuth       │  │ • KYC         │  │               │  │              │  │    │
│  │  │ • Sessions    │  │ • GDPR        │  │               │  │              │  │    │
│  │  └───────────────┘  └───────────────┘  └───────────────┘  └──────────────┘  │    │
│  │                                                                              │    │
│  └─────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐    │
│  │                         BUSINESS SERVICES                                    │    │
│  │                                                                              │    │
│  │  ┌───────────────────────┐  ┌───────────────────────┐  ┌─────────────────┐  │    │
│  │  │  VehiclesSaleService  │  │  DealerMgmtService    │  │ InventoryService│  │    │
│  │  │                       │  │                       │  │                 │  │    │
│  │  │ • Vehicles CRUD       │  │ • Dealer Profiles     │  │ • Bulk Import   │  │    │
│  │  │ • Search & Filters    │  │ • Verification        │  │ • Stock Mgmt    │  │    │
│  │  │ • Homepage Sections   │  │ • Subscriptions       │  │ • CSV/Excel     │  │    │
│  │  │ • VIN Decode          │  │ • Locations           │  │                 │  │    │
│  │  │ • Catalog (Makes/     │  │ • Documents           │  │                 │  │    │
│  │  │   Models/Years)       │  │                       │  │                 │  │    │
│  │  │ • Favorites           │  │                       │  │                 │  │    │
│  │  │ • Comparisons         │  │                       │  │                 │  │    │
│  │  └───────────────────────┘  └───────────────────────┘  └─────────────────┘  │    │
│  │                                                                              │    │
│  │  ┌───────────────────────┐  ┌───────────────────────┐  ┌─────────────────┐  │    │
│  │  │    BillingService     │  │    ContactService     │  │  ReviewService  │  │    │
│  │  │                       │  │                       │  │                 │  │    │
│  │  │ • Stripe Integration  │  │ • Inquiries           │  │ • Dealer Reviews│  │    │
│  │  │ • AZUL Integration    │  │ • Messages            │  │ • Seller Ratings│  │    │
│  │  │ • Subscriptions       │  │ • Appointments        │  │ • Response Rate │  │    │
│  │  │ • Invoices            │  │ • WhatsApp            │  │                 │  │    │
│  │  │ • Payment Methods     │  │                       │  │                 │  │    │
│  │  └───────────────────────┘  └───────────────────────┘  └─────────────────┘  │    │
│  │                                                                              │    │
│  └─────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐    │
│  │                       INFRASTRUCTURE SERVICES                                │    │
│  │                                                                              │    │
│  │  ┌───────────────┐  ┌───────────────┐  ┌───────────────┐  ┌──────────────┐  │    │
│  │  │ MediaService  │  │ Notification  │  │  ErrorService │  │ AuditService │  │    │
│  │  │               │  │   Service     │  │               │  │              │  │    │
│  │  │ • S3 Upload   │  │ • Email       │  │ • Error Log   │  │ • User Actions│ │    │
│  │  │ • Image Proc  │  │ • SMS         │  │ • Dead Letter │  │ • Admin Logs │  │    │
│  │  │ • 360° Media  │  │ • Push        │  │ • Retry Queue │  │ • Changes    │  │    │
│  │  │ • CDN URLs    │  │ • WhatsApp    │  │ • Alerts      │  │              │  │    │
│  │  │               │  │ • Templates   │  │               │  │              │  │    │
│  │  └───────────────┘  └───────────────┘  └───────────────┘  └──────────────┘  │    │
│  │                                                                              │    │
│  └─────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐    │
│  │                          DATA & ML SERVICES                                  │    │
│  │                             (Planificados)                                   │    │
│  │                                                                              │    │
│  │  ┌───────────────┐  ┌───────────────┐  ┌───────────────┐  ┌──────────────┐  │    │
│  │  │ Analytics     │  │ Recommendation│  │ PricingIntel  │  │ LeadScoring  │  │    │
│  │  │ Service       │  │ Service       │  │ Service       │  │ Service      │  │    │
│  │  │               │  │               │  │               │  │              │  │    │
│  │  │ • Dashboards  │  │ • Similar     │  │ • Market      │  │ • Hot/Cold   │  │    │
│  │  │ • Reports     │  │   Vehicles    │  │   Analysis    │  │   Leads      │  │    │
│  │  │ • KPIs        │  │ • Personalized│  │ • Price       │  │ • Conversion │  │    │
│  │  │               │  │   Suggestions │  │   Suggestions │  │   Prediction │  │    │
│  │  └───────────────┘  └───────────────┘  └───────────────┘  └──────────────┘  │    │
│  │                                                                              │    │
│  └─────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 📁 ESTRUCTURA DE CARPETAS FRONTEND

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                         ESTRUCTURA NEXT.JS                                           │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  frontend/web-next/                                                                  │
│  │                                                                                   │
│  ├── src/                                                                            │
│  │   │                                                                               │
│  │   ├── app/                          # App Router (Next.js 14+)                   │
│  │   │   ├── (public)/                 # Rutas públicas (sin auth)                  │
│  │   │   │   ├── page.tsx              # Homepage                                   │
│  │   │   │   ├── vehicles/             # /vehicles, /vehicles/[slug]                │
│  │   │   │   ├── search/               # /search                                    │
│  │   │   │   └── dealers/              # /dealers, /dealers/[slug]                  │
│  │   │   │                                                                          │
│  │   │   ├── (auth)/                   # Rutas de autenticación                     │
│  │   │   │   ├── login/                # /login                                     │
│  │   │   │   ├── register/             # /register                                  │
│  │   │   │   └── forgot-password/      # /forgot-password                           │
│  │   │   │                                                                          │
│  │   │   ├── (protected)/              # Rutas protegidas (requieren auth)          │
│  │   │   │   ├── dashboard/            # /dashboard                                 │
│  │   │   │   ├── favorites/            # /favorites                                 │
│  │   │   │   ├── messages/             # /messages                                  │
│  │   │   │   ├── settings/             # /settings/*                                │
│  │   │   │   └── sell/                 # /sell (publicar vehículo)                  │
│  │   │   │                                                                          │
│  │   │   ├── (dealer)/                 # Portal del Dealer                          │
│  │   │   │   ├── dealer/               # /dealer/dashboard, inventory, etc.         │
│  │   │   │   └── layout.tsx            # Layout con sidebar dealer                  │
│  │   │   │                                                                          │
│  │   │   ├── (admin)/                  # Panel de Administración                    │
│  │   │   │   ├── admin/                # /admin/dashboard, users, etc.              │
│  │   │   │   └── layout.tsx            # Layout con sidebar admin                   │
│  │   │   │                                                                          │
│  │   │   ├── api/                      # API Routes (si se necesitan)               │
│  │   │   │   └── health/route.ts       # Health check endpoint                      │
│  │   │   │                                                                          │
│  │   │   ├── layout.tsx                # Root layout                                │
│  │   │   ├── error.tsx                 # Error boundary global                      │
│  │   │   ├── loading.tsx               # Loading global                             │
│  │   │   └── not-found.tsx             # 404 page                                   │
│  │   │                                                                               │
│  │   ├── components/                   # Componentes reutilizables                  │
│  │   │   ├── ui/                       # shadcn/ui components                       │
│  │   │   ├── layout/                   # Navbar, Footer, Sidebar                    │
│  │   │   ├── vehicles/                 # VehicleCard, Gallery, Filters              │
│  │   │   ├── dealers/                  # DealerCard, DealerProfile                  │
│  │   │   ├── forms/                    # Form components                            │
│  │   │   └── error/                    # Error boundaries, fallbacks                │
│  │   │                                                                               │
│  │   ├── lib/                          # Utilidades y configuración                 │
│  │   │   ├── api/                      # API client, error handling                 │
│  │   │   ├── utils/                    # Helpers, formatters                        │
│  │   │   └── validations/              # Zod schemas                                │
│  │   │                                                                               │
│  │   ├── hooks/                        # Custom hooks                               │
│  │   │   ├── queries/                  # TanStack Query hooks                       │
│  │   │   ├── mutations/                # TanStack mutations                         │
│  │   │   └── use*.ts                   # Otros hooks                                │
│  │   │                                                                               │
│  │   ├── stores/                       # Zustand stores                             │
│  │   │   ├── authStore.ts              # Estado de autenticación                    │
│  │   │   ├── uiStore.ts                # Estado de UI (modals, sidebars)            │
│  │   │   └── compareStore.ts           # Comparador de vehículos                    │
│  │   │                                                                               │
│  │   ├── providers/                    # Context providers                          │
│  │   │   ├── AuthProvider.tsx          # Auth context                               │
│  │   │   ├── QueryProvider.tsx         # TanStack Query                             │
│  │   │   └── ThemeProvider.tsx         # Theme (dark/light)                         │
│  │   │                                                                               │
│  │   ├── types/                        # TypeScript types                           │
│  │   │   ├── api.ts                    # API response types                         │
│  │   │   ├── vehicle.ts                # Vehicle types                              │
│  │   │   └── user.ts                   # User types                                 │
│  │   │                                                                               │
│  │   └── styles/                       # Estilos globales                           │
│  │       └── globals.css               # Tailwind + custom CSS                      │
│  │                                                                                   │
│  ├── public/                           # Static assets                              │
│  │   ├── images/                       # Imágenes estáticas                         │
│  │   ├── icons/                        # Iconos                                     │
│  │   └── locales/                      # i18n translations                          │
│  │                                                                                   │
│  ├── e2e/                              # Tests E2E (Playwright)                     │
│  │   ├── auth.spec.ts                                                               │
│  │   ├── vehicles.spec.ts                                                           │
│  │   └── checkout.spec.ts                                                           │
│  │                                                                                   │
│  ├── next.config.ts                    # Next.js configuration                      │
│  ├── tailwind.config.ts                # Tailwind configuration                     │
│  ├── tsconfig.json                     # TypeScript configuration                   │
│  ├── package.json                      # Dependencies                               │
│  └── Dockerfile                        # Docker build                               │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔄 FLUJO DE ESTADO (ZUSTAND + TANSTACK QUERY)

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                      GESTIÓN DE ESTADO EN FRONTEND                                   │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐    │
│  │                           TANSTACK QUERY                                     │    │
│  │                      (Server State - Datos del API)                          │    │
│  │                                                                              │    │
│  │   ┌─────────────────┐                      ┌────────────────────────────┐   │    │
│  │   │   Query Cache   │                      │    Automatic Features      │   │    │
│  │   │                 │                      │                            │   │    │
│  │   │  vehicles: [...]│◀─────────────────────│ • Background Refetch       │   │    │
│  │   │  user: {...}    │                      │ • Stale-While-Revalidate   │   │    │
│  │   │  favorites: [...│                      │ • Retry on Error           │   │    │
│  │   │  dealers: [...] │                      │ • Pagination               │   │    │
│  │   │                 │                      │ • Infinite Scroll          │   │    │
│  │   └─────────────────┘                      └────────────────────────────┘   │    │
│  │           │                                                                  │    │
│  │           ▼                                                                  │    │
│  │   ┌─────────────────────────────────────────────────────────────────────┐   │    │
│  │   │  useQuery('vehicles', fetchVehicles)                                 │   │    │
│  │   │  useQuery('user', fetchUser)                                         │   │    │
│  │   │  useInfiniteQuery('search', fetchSearchResults)                      │   │    │
│  │   │  useMutation('createVehicle', createVehicle)                         │   │    │
│  │   └─────────────────────────────────────────────────────────────────────┘   │    │
│  │                                                                              │    │
│  └─────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                      │
│                                       │                                              │
│                                       │ Components use both                          │
│                                       ▼                                              │
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐    │
│  │                              ZUSTAND                                         │    │
│  │                     (Client State - Estado Local)                            │    │
│  │                                                                              │    │
│  │   ┌─────────────────────────────────────────────────────────────────────┐   │    │
│  │   │                                                                      │   │    │
│  │   │  authStore                    uiStore                compareStore   │   │    │
│  │   │  ┌──────────────┐            ┌──────────────┐       ┌────────────┐  │   │    │
│  │   │  │ user: {...}  │            │ sidebarOpen  │       │ vehicles   │  │   │    │
│  │   │  │ isAuth: bool │            │ modalOpen    │       │ [id1, id2] │  │   │    │
│  │   │  │ token: "..."│             │ theme: dark  │       │ max: 3     │  │   │    │
│  │   │  │              │            │ filters: {}  │       │            │  │   │    │
│  │   │  │ login()      │            │ toast: {...} │       │ add()      │  │   │    │
│  │   │  │ logout()     │            │              │       │ remove()   │  │   │    │
│  │   │  │ refresh()    │            │ toggle()     │       │ clear()    │  │   │    │
│  │   │  └──────────────┘            └──────────────┘       └────────────┘  │   │    │
│  │   │                                                                      │   │    │
│  │   └─────────────────────────────────────────────────────────────────────┘   │    │
│  │                                                                              │    │
│  │   Características:                                                           │    │
│  │   • Persist: localStorage (auth, theme)                                     │    │
│  │   • DevTools: Integración para debugging                                    │    │
│  │   • Subscriptions: Re-render selectivo                                      │    │
│  │                                                                              │    │
│  └─────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 📡 COMUNICACIÓN ENTRE MICROSERVICIOS

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                    PATRONES DE COMUNICACIÓN                                          │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐    │
│  │                    1. SYNC: HTTP/REST (via Gateway)                          │    │
│  │                                                                              │    │
│  │     Client ──▶ Gateway ──▶ ServiceA                                          │    │
│  │                   │                                                          │    │
│  │                   └──────▶ ServiceB (si necesita datos adicionales)          │    │
│  │                                                                              │    │
│  │     Uso: Operaciones que requieren respuesta inmediata                       │    │
│  │     Ejemplo: GET /vehicles, POST /auth/login                                 │    │
│  │                                                                              │    │
│  └─────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐    │
│  │                    2. ASYNC: RabbitMQ (Events)                               │    │
│  │                                                                              │    │
│  │     ServiceA ──publish──▶ RabbitMQ ──consume──▶ ServiceB                     │    │
│  │                              │                                               │    │
│  │                              └───consume───▶ ServiceC                        │    │
│  │                                                                              │    │
│  │     Uso: Operaciones fire-and-forget, notificaciones                         │    │
│  │     Ejemplo:                                                                 │    │
│  │     • VehicleCreated → NotificationService (email al dealer)                 │    │
│  │     • PaymentCompleted → AuditService (log)                                  │    │
│  │     • UserRegistered → BillingService (crear customer Stripe)                │    │
│  │                                                                              │    │
│  └─────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐    │
│  │                    3. CACHE: Redis                                           │    │
│  │                                                                              │    │
│  │     ┌────────────┐                      ┌────────────┐                       │    │
│  │     │  Service   │──GET key──▶ Redis ◀──SET key──│  Service   │              │    │
│  │     └────────────┘              │       └────────────┘                       │    │
│  │                                 │                                            │    │
│  │                          ┌──────┴──────┐                                     │    │
│  │                          │ Cache Data  │                                     │    │
│  │                          │ • Sessions  │                                     │    │
│  │                          │ • Rate Limit│                                     │    │
│  │                          │ • Hot Data  │                                     │    │
│  │                          └─────────────┘                                     │    │
│  │                                                                              │    │
│  │     Uso: Datos frecuentes, sessions, rate limiting                          │    │
│  │                                                                              │    │
│  └─────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                      │
│  ┌─────────────────────────────────────────────────────────────────────────────┐    │
│  │                    FLUJO DE EJEMPLO: Publicar Vehículo                       │    │
│  │                                                                              │    │
│  │   1. Frontend                 POST /api/vehicles                             │    │
│  │      │                             │                                         │    │
│  │      └─────────────────────────────▶                                         │    │
│  │                                    │                                         │    │
│  │   2. Gateway                  Validate JWT, Route                            │    │
│  │      │                             │                                         │    │
│  │      └─────────────────────────────▶                                         │    │
│  │                                    │                                         │    │
│  │   3. VehicleService          Create vehicle in DB                            │    │
│  │      │                             │                                         │    │
│  │      ├─────────────────────────────▶ PostgreSQL (INSERT)                     │    │
│  │      │                             │                                         │    │
│  │      └─publish──▶ RabbitMQ        │                                         │    │
│  │                     │             │                                         │    │
│  │   4. Consumers      │             │                                         │    │
│  │      │              │             │                                         │    │
│  │      ├──────────────┼─────────────│──▶ MediaService (process images)         │    │
│  │      ├──────────────┼─────────────│──▶ NotificationService (notify dealer)   │    │
│  │      ├──────────────┼─────────────│──▶ AuditService (log creation)           │    │
│  │      └──────────────┼─────────────│──▶ AnalyticsService (track event)        │    │
│  │                     │             │                                         │    │
│  │   5. Response       │             │                                         │    │
│  │      ◀──────────────┼─────────────┘                                         │    │
│  │      201 Created    │                                                       │    │
│  │      {vehicle data} │                                                       │    │
│  │                                                                              │    │
│  └─────────────────────────────────────────────────────────────────────────────┘    │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔗 REFERENCIAS

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Microservices Patterns](https://microservices.io/patterns/index.html)
- [Next.js App Router](https://nextjs.org/docs/app)
- [TanStack Query](https://tanstack.com/query/latest)
- [Zustand](https://zustand-demo.pmnd.rs/)

---

_Última actualización: Enero 31, 2026_
