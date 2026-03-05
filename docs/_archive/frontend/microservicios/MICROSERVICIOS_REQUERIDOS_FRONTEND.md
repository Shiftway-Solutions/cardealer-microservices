# 🎯 Microservicios Requeridos por Frontend - OKLA Marketplace

**Última actualización:** Enero 18, 2026  
**Versión:** 2.0  
**Estado:** Documentación Completa

---

## 📋 Tabla de Contenidos

1. [Resumen Ejecutivo](#-resumen-ejecutivo)
2. [Microservicios Críticos (Must-Have)](#-microservicios-críticos-must-have)
3. [Microservicios Importantes](#-microservicios-importantes)
4. [Microservicios Opcionales](#-microservicios-opcionales)
5. [Infraestructura Requerida](#-infraestructura-requerida)
6. [Arquitectura de Comunicación](#-arquitectura-de-comunicación)
7. [Matriz de Dependencias](#-matriz-de-dependencias)
8. [Estado de Implementación](#-estado-de-implementación)

---

## 📊 Resumen Ejecutivo

### Total de Microservicios en Proyecto

**29 servicios/módulos** de los cuales:

| Categoría                        | Cantidad | Requisito                         | Estado           |
| -------------------------------- | -------- | --------------------------------- | ---------------- |
| 🔴 **Críticos (Must-Have)**      | **4**    | Frontend no funciona sin ellos    | Desplegados ✅   |
| 🟠 **Importantes (Should-Have)** | **4**    | Funcionalidad reducida sin ellos  | Desplegados ✅   |
| 🟡 **Opcionales (Nice-to-Have)** | **2**    | Mejoras de UX/performance         | En desarrollo ⏳ |
| 🔵 **Backend Only**              | **15**   | No requieren UI                   | Varios estados   |
| ⚪ **Infraestructura**           | **4**    | Soporte (Postgres, RabbitMQ, etc) | Activos ✅       |

### Resumen de Microservicios del Frontend

```
Frontend (React 19) → API Gateway (Ocelot)
                      ├── AuthService ⭐ CRÍTICO
                      ├── VehiclesSaleService ⭐ CRÍTICO
                      ├── UserService 🟠 IMPORTANTE
                      ├── MediaService 🟠 IMPORTANTE
                      ├── ContactService 🟠 IMPORTANTE
                      ├── NotificationService 🟡 OPCIONAL
                      ├── AdminService (si role=admin)
                      └── SearchService 🟡 OPCIONAL
```

---

## 🔴 Microservicios Críticos (Must-Have)

### Sin estos servicios, el frontend NO FUNCIONA

#### 1. 🔐 **AuthService** - Autenticación y Seguridad

**Propósito:** Autenticar usuarios, gestionar tokens JWT, reset de contraseña

**Puerto:** 5001 (K8s: 8080)  
**Status:** ✅ Desplegado en producción

**Endpoints Requeridos:**

```http
POST   /api/auth/login              Inicio de sesión
POST   /api/auth/register           Registro de usuario
POST   /api/auth/refresh-token      Renovar JWT token
POST   /api/auth/logout             Cerrar sesión
POST   /api/auth/forgot-password    Solicitar reset
POST   /api/auth/reset-password     Resetear contraseña
GET    /api/auth/me                 Obtener usuario actual
PUT    /api/auth/me                 Actualizar perfil
POST   /api/auth/verify-email       Verificar email
GET    /api/auth/email/{email}/exists  Verificar existencia
```

**Funcionalidades del Frontend que Dependen:**

- ✅ Login / Registro (páginas públicas)
- ✅ Gestión de sesiones (JWT storage)
- ✅ Rutas protegidas (ProtectedRoute)
- ✅ Token refresh automático
- ✅ Logout
- ✅ Reset de contraseña
- ✅ Verificación de email

**Tipos de Usuario Soportados:**

- Buyer (Comprador) - Puede comprar vehículos
- Seller (Vendedor Individual) - Puede publicar vehículos
- Dealer (Distribuidor) - Múltiples vehículos, planes pagos
- Admin (Administrador) - Moderación y gestión

---

#### 2. 🛍️ **VehiclesSaleService** - Gestión de Vehículos

**Propósito:** CRUD de vehículos, búsqueda, filtros, catálogo

**Puerto:** 5010 (K8s: 8080)  
**Status:** ✅ Desplegado en producción  
**Base de Datos:** PostgreSQL

**Endpoints Requeridos:**

```http
GET    /api/vehicles                Listar vehículos (paginado)
GET    /api/vehicles/{id}           Obtener detalle de vehículo
GET    /api/vehicles/search         Búsqueda con filtros
POST   /api/vehicles                Crear vehículo (auth required)
PUT    /api/vehicles/{id}           Actualizar vehículo (auth required)
DELETE /api/vehicles/{id}           Eliminar vehículo (auth required)
GET    /api/vehicles/user/{userId}  Vehículos de un usuario
GET    /api/catalog/makes           Marcas disponibles
GET    /api/catalog/models/{makeId} Modelos por marca
GET    /api/catalog/years           Años disponibles
GET    /api/homepagesections/homepage Secciones del homepage
POST   /api/vehicles/{id}/favorite  Agregar a favoritos
DELETE /api/vehicles/{id}/favorite  Remover de favoritos
GET    /api/vehicles/{id}/similar   Vehículos similares
```

**Funcionalidades del Frontend que Dependen:**

- ✅ Homepage (secciones dinámicas)
- ✅ Listado de vehículos
- ✅ Detalle de vehículo
- ✅ Búsqueda avanzada
- ✅ Filtros (marca, modelo, año, precio, etc)
- ✅ Publicar vehículo
- ✅ Editar vehículos propios
- ✅ Favoritos
- ✅ Vehículos similares
- ✅ Dashboard del vendedor (mis vehículos)

**Datos Principales:**

```typescript
Vehicle {
  id: UUID
  title: string
  description: string
  make: string      // Marca (Toyota, Honda, etc)
  model: string     // Modelo
  year: number
  price: number
  mileage: number
  transmission: string  // Manual, Automático
  fuelType: string      // Gasolina, Diésel, Híbrido
  condition: string     // Nuevo, Usado, Refurbished
  status: string        // Active, Paused, Sold, Rejected
  sellerId: UUID
  createdAt: DateTime
  images: VehicleImage[]
  specifications: object
}
```

---

#### 3. 📸 **MediaService** - Gestión de Imágenes

**Propósito:** Subir, procesar y servir imágenes de vehículos

**Puerto:** 5020 (K8s: 8080)  
**Status:** ✅ Desplegado en producción  
**Storage:** AWS S3 (o compatible)

**Endpoints Requeridos:**

```http
POST   /api/media/upload            Subir imagen
GET    /api/media/{mediaId}         Obtener imagen
DELETE /api/media/{mediaId}         Eliminar imagen
POST   /api/media/batch-upload      Subir múltiples imágenes
GET    /api/media/vehicle/{vehicleId} Imágenes de vehículo
```

**Funcionalidades del Frontend que Dependen:**

- ✅ Subir fotos al crear vehículo
- ✅ Editar fotos de vehículo existente
- ✅ Mostrar galería de imágenes
- ✅ Preview antes de subir
- ✅ Eliminar imágenes

**Tipos de Media Soportados:**

- Imágenes JPEG, PNG (máx 5MB por imagen)
- Hasta 20 imágenes por vehículo
- Auto-resize a múltiples resoluciones (thumbnail, medium, large)
- URL presignadas para acceso seguro

---

#### 4. 🔒 **Gateway (Ocelot)** - API Gateway

**Propósito:** Enrutamiento de requests a microservicios, autenticación JWT

**Puerto:** 18443 (HTTPS en producción)  
**Status:** ✅ Desplegado en producción  
**Punto de entrada único:** https://api.okla.com.do

**Configuración Crítica:**

```json
{
  "Routes": [
    {
      "DownstreamPathTemplate": "/api/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [{ "Host": "authservice", "Port": 8080 }],
      "UpstreamPathTemplate": "/api/auth/{everything}",
      "AuthenticationOptions": {
        "AuthenticationProviderKey": "Bearer"
      }
    }
    // ... más rutas para cada servicio
  ]
}
```

**Funcionalidades:**

- ✅ Ruteo de requests
- ✅ Validación de JWT
- ✅ Rate limiting
- ✅ Logging centralizado
- ✅ CORS handling
- ✅ Agregación de datos (opcional)

**Variables de Entorno en Frontend:**

```env
# Producción
VITE_API_URL=https://api.okla.com.do

# Desarrollo
VITE_API_URL=http://localhost:18443
```

---

## 🟠 Microservicios Importantes (Should-Have)

### Sin estos servicios, funcionalidad reducida

#### 5. 👤 **UserService** - Gestión de Usuarios

**Propósito:** Perfiles de usuario, configuración, estadísticas

**Puerto:** 5002 (K8s: 8080)  
**Status:** ✅ Desplegado en producción

**Endpoints Requeridos:**

```http
GET    /api/users/{id}              Perfil público de usuario
GET    /api/users/me                Mi perfil
PUT    /api/users/me                Actualizar mi perfil
GET    /api/users/me/favorites      Mis favoritos
POST   /api/users/me/favorites/{vehicleId}  Agregar favorito
DELETE /api/users/me/favorites/{vehicleId}  Remover favorito
GET    /api/users/{id}/vehicles     Vehículos de usuario
GET    /api/users/{id}/reviews      Reviews/ratings de usuario
POST   /api/users/{id}/reviews      Crear review
PUT    /api/users/me/settings       Actualizar configuración
DELETE /api/users/me                Eliminar cuenta
GET    /api/users/search            Buscar usuarios
```

**Funcionalidades del Frontend que Dependen:**

- ✅ Perfil público (ver información del vendedor)
- ✅ Perfil privado (mis datos)
- ✅ Editar perfil
- ✅ Favoritos
- ✅ Reviews/ratings
- ✅ Configuración de cuenta
- ✅ Información de vendedor en detalle de vehículo
- ✅ Estadísticas del vendedor

**Información de Usuario:**

```typescript
User {
  id: UUID
  email: string
  fullName: string
  phone: string
  accountType: 'Individual' | 'Dealer'
  avatar: string (URL)
  bio: string
  joinedDate: DateTime
  isVerified: boolean
  rating: number (0-5)
  reviewCount: number
  location: {
    city: string
    province: string
    coordinates: { lat, lng }
  }
  preferences: {
    emailNotifications: boolean
    pushNotifications: boolean
    smsNotifications: boolean
  }
}
```

---

#### 6. 💬 **ContactService** - Mensajería entre Usuarios

**Propósito:** Contacto entre compradores y vendedores

**Puerto:** 5003 (K8s: 8080)  
**Status:** ✅ Desplegado en producción

**Endpoints Requeridos:**

```http
POST   /api/contacts/send           Enviar mensaje
GET    /api/contacts/inbox          Bandeja de entrada
GET    /api/contacts/sent           Mensajes enviados
GET    /api/contacts/{id}           Ver conversación
PUT    /api/contacts/{id}/read      Marcar como leído
DELETE /api/contacts/{id}           Eliminar conversación
GET    /api/contacts/unread-count   Contar no leídos
```

**Funcionalidades del Frontend que Dependen:**

- ✅ Formulario de contacto en detalle de vehículo
- ✅ Bandeja de mensajes
- ✅ Historial de conversaciones
- ✅ Notificación de nuevos mensajes
- ✅ Marcar como leído
- ✅ Badge de mensajes sin leer

**Estructura de Mensaje:**

```typescript
Message {
  id: UUID
  conversationId: UUID
  senderId: UUID
  recipientId: UUID
  vehicleId: UUID (opcional)
  subject: string
  body: string
  isRead: boolean
  createdAt: DateTime
  attachments: Attachment[] (opcional)
}
```

---

#### 7. 🔔 **NotificationService** - Notificaciones

**Propósito:** Notificaciones por email, SMS, push, in-app

**Puerto:** 5005 (K8s: 8080)  
**Status:** ✅ Desplegado en producción

**Endpoints Requeridos:**

```http
GET    /api/notifications           Obtener notificaciones
PUT    /api/notifications/{id}/read Marcar como leído
GET    /api/notifications/unread-count Contar sin leer
GET    /api/notifications/settings  Obtener preferencias
PUT    /api/notifications/settings  Actualizar preferencias
DELETE /api/notifications/{id}      Eliminar notificación
```

**Funcionalidades del Frontend que Dependen:**

- ✅ Centro de notificaciones (dropdown)
- ✅ Badge con contador
- ✅ Preferencias de notificación
- ✅ WebSocket para notificaciones real-time (opcional)
- ✅ Push notifications (opcional)

---

#### 8. 🛡️ **AdminService** - Panel de Administración

**Propósito:** Moderación, estadísticas, gestión de contenido

**Puerto:** 5007 (K8s: 8080)  
**Status:** ✅ Desplegado en producción  
**Restricción:** Solo para usuarios con rol = Admin

**Endpoints Requeridos:**

```http
GET    /api/admin/dashboard         Dashboard principal
GET    /api/admin/dashboard/stats   Estadísticas
GET    /api/admin/vehicles/pending  Vehículos pendientes de aprobación
POST   /api/admin/vehicles/{id}/approve Aprobar vehículo
POST   /api/admin/vehicles/{id}/reject  Rechazar vehículo
PUT    /api/admin/vehicles/{id}/verify  Marcar como verificado
GET    /api/admin/users             Listar usuarios
POST   /api/admin/users/{id}/ban    Banear usuario
GET    /api/admin/reports           Reportes de usuarios
POST   /api/admin/reports/{id}/resolve Resolver reporte
GET    /api/admin/activity-logs     Logs de actividad
```

**Funcionalidades del Frontend que Dependen:**

- ✅ Dashboard de admin
- ✅ Gestión de vehículos (aprobar/rechazar)
- ✅ Gestión de usuarios
- ✅ Reportes
- ✅ Logs de auditoría
- ✅ Estadísticas del sistema

**Datos de Dashboard:**

```typescript
DashboardStats {
  totalVehicles: number
  totalUsers: number
  totalReports: number
  pendingApprovals: number
  dailyActiveUsers: number
  revenue: number
  topListedBrands: string[]
  topCities: string[]
}
```

---

## 🟡 Microservicios Opcionales (Nice-to-Have)

### Mejoran la experiencia pero frontend funciona sin ellos

#### 9. 🔍 **SearchService** - Búsqueda Avanzada (Elasticsearch)

**Propósito:** Búsqueda rápida y filtros avanzados

**Puerto:** 5030 (K8s: 8080)  
**Status:** ⏳ En desarrollo  
**Base de Datos:** Elasticsearch

**Endpoints Planificados:**

```http
GET    /api/search/vehicles         Búsqueda con filtros
GET    /api/search/suggestions      Autocompletado
GET    /api/search/filters          Opciones de filtros
GET    /api/search/advanced         Búsqueda avanzada
POST   /api/search/saved            Guardar búsqueda
```

**Funcionalidades del Frontend que Dependen:**

- ✅ Autocompletado de búsqueda
- ✅ Búsqueda avanzada
- ✅ Búsquedas guardadas (opcional)
- ✅ Alertas de precio (cuando precio baja)

**Nota:** Si SearchService no está disponible, VehiclesSaleService maneja búsqueda básica

---

#### 10. 💳 **BillingService** - Pagos y Suscripciones

**Propósito:** Gestión de suscripciones de dealers, pagos

**Puerto:** 5023 (K8s: 8080)  
**Status:** ✅ Desplegado en producción  
**Proveedores:** Stripe + AZUL (Banco Popular)

**Endpoints Requeridos (Dealers):**

```http
GET    /api/billing/plans           Listar planes disponibles
POST   /api/billing/subscribe       Subscribirse a plan
GET    /api/billing/subscription    Mi suscripción actual
PUT    /api/billing/subscription    Cambiar plan
DELETE /api/billing/subscription    Cancelar suscripción
GET    /api/billing/invoices        Mis facturas
GET    /api/billing/methods         Métodos de pago guardados
POST   /api/billing/methods         Agregar método de pago
```

**Funcionalidades del Frontend que Dependen:**

- ✅ Página de precios (dealers)
- ✅ Checkout de suscripción
- ✅ Dashboard de billing
- ✅ Historial de facturas (dealers)
- ✅ Cambiar plan
- ✅ Cancelar suscripción

**Planes Disponibles:**

```typescript
Plan {
  id: string
  name: string         // Starter, Pro, Enterprise
  price: number
  currency: string     // USD, DOP
  maxListings: number
  features: string[]
  billingPeriod: 'monthly' | 'annual'
}
```

---

## 🔵 Infraestructura Requerida

### Servicios que NO tienen UI pero son críticos

#### PostgreSQL Database

**Propósito:** Base de datos principal de todos los servicios

**Versión:** 16+  
**Puerto:** 5432  
**Status:** ✅ Activo en producción  
**Bases de Datos:** Una por microservicio (16 totales)

**Información Importante:**

- Todas las entidades usan UUID como clave primaria
- Auditoría automática (CreatedAt, UpdatedAt)
- Conexión string en cada servicio via appsettings.json

---

#### RabbitMQ - Message Broker

**Propósito:** Comunicación async entre servicios

**Versión:** 3.12+  
**Puerto:** 5672 (AMQP), 15672 (Management)  
**Status:** ✅ Activo en producción

**Eventos que se publican:**

- VehicleCreated
- VehicleUpdated
- VehicleDeleted
- UserRegistered
- MessageSent
- ReportCreated
- PaymentSuccessful
- Etc.

**Frontend Impact:** El frontend no se conecta directamente a RabbitMQ, pero depende de eventos que se publican (ejemplo: notificaciones real-time)

---

#### Redis - Cache Distribuido

**Propósito:** Cache para mejorar performance

**Versión:** 7+  
**Puerto:** 6379  
**Status:** ✅ Activo en producción

**Usados para:**

- Caché de sesiones
- Rate limiting del Gateway
- Cache de búsquedas frecuentes
- Cache de imágenes de vehículos

---

#### Consul - Service Discovery

**Propósito:** Descubrimiento dinámico de servicios

**Versión:** 1.15+  
**Puerto:** 8500  
**Status:** ✅ Activo en producción

**Funcionalidad:** Permite que servicios se registren automáticamente y Gateway sepa dónde encontrarlos

---

## 🏗️ Arquitectura de Comunicación

### Flujo General de Requests

```
┌──────────────────────────────────────────────────────────────┐
│                         FRONTEND (React 19)                   │
│                      (http://okla.com.do)                     │
└──────────────────────────────────────────────────────────────┘
                              │
                              │ HTTP/HTTPS
                              ▼
┌──────────────────────────────────────────────────────────────┐
│                  API GATEWAY (Ocelot)                         │
│              (https://api.okla.com.do:18443)                  │
│                                                               │
│  ✓ Autenticación JWT                                         │
│  ✓ Rate Limiting                                             │
│  ✓ Logging/Monitoring                                        │
│  ✓ CORS                                                       │
└──────────────────────────────────────────────────────────────┘
        │           │           │           │           │
        ▼           ▼           ▼           ▼           ▼
   AuthService VehicleService  UserService MediaService  ...
   (Port 8080) (Port 8080)    (Port 8080) (Port 8080)
        │           │           │           │           │
        ▼           ▼           ▼           ▼           ▼
    PostgreSQL ─── PostgreSQL ─ PostgreSQL ─ S3 ────────
    (Database)
        ▲           ▲           ▲           ▲
        └───────────┴───────────┴───────────┘
             Events via RabbitMQ
```

### Autenticación

```
1. User → Frontend: POST /login (email, password)
2. Frontend → Gateway → AuthService: /api/auth/login
3. AuthService: Valida credenciales, genera JWT token
4. AuthService → Frontend: { accessToken, refreshToken }
5. Frontend: Almacena tokens en localStorage
6. Frontend: Incluye JWT en headers para requests posteriores
   Authorization: Bearer <jwt_token>
7. Gateway: Valida JWT antes de rutear a otros servicios
8. Servicio: Procesa request autenticado
```

---

## 🗺️ Matriz de Dependencias

### Microservicios por Pantalla del Frontend

| Pantalla        | Auth | Vehicles | User | Media | Contact | Notif | Admin | Billing |
| --------------- | :--: | :------: | :--: | :---: | :-----: | :---: | :---: | :-----: |
| Login           |  ✅  |          |      |       |         |       |       |         |
| Register        |  ✅  |          |      |       |         |       |       |         |
| Homepage        |  ✅  |    ✅    |      |  ✅   |         |       |       |         |
| Search          |      |    ✅    |      |  ✅   |         |       |       |         |
| Vehicle Detail  |  ✅  |    ✅    |  ✅  |  ✅   |   ✅    |       |       |         |
| Publish Vehicle |  ✅  |    ✅    |      |  ✅   |         |       |       |         |
| My Vehicles     |  ✅  |    ✅    |      |  ✅   |         |       |       |         |
| Profile         |  ✅  |    ✅    |  ✅  |       |         |       |       |         |
| Messages        |  ✅  |          |  ✅  |       |   ✅    |       |       |         |
| Notifications   |  ✅  |          |      |       |         |  ✅   |       |         |
| Admin Dashboard |  ✅  |    ✅    |  ✅  |       |         |       |  ✅   |         |
| Dealer Pricing  |  ✅  |          |      |       |         |       |       |   ✅    |
| My Subscription |  ✅  |          |      |       |         |       |       |   ✅    |

### Dependencias Críticas

**Para Homepage funcionar:**

1. AuthService (aunque sea no-auth)
2. VehiclesSaleService (datos de vehículos)
3. MediaService (imágenes)

**Para Buscar funcionar:**

1. VehiclesSaleService (búsqueda básica) O SearchService (búsqueda avanzada)
2. MediaService (mostrar imágenes)

**Para Publicar vehículo funcionar:**

1. AuthService (validar usuario)
2. VehiclesSaleService (crear vehículo)
3. MediaService (subir imágenes)

---

## 📊 Estado de Implementación

### Estado Actual (Enero 2026)

| Servicio                | Status | Producción |    Frontend     |
| ----------------------- | :----: | :--------: | :-------------: |
| **AuthService**         |   ✅   |  En DOKS   |    Integrado    |
| **VehiclesSaleService** |   ✅   |  En DOKS   |    Integrado    |
| **MediaService**        |   ✅   |  En DOKS   |    Integrado    |
| **UserService**         |   ✅   |  En DOKS   |    Integrado    |
| **ContactService**      |   ✅   |  En DOKS   |    Integrado    |
| **NotificationService** |   ✅   |  En DOKS   |    Integrado    |
| **AdminService**        |   ✅   |  En DOKS   |    Integrado    |
| **BillingService**      |   ✅   |  En DOKS   |    Integrado    |
| **Gateway (Ocelot)**    |   ✅   |  En DOKS   |    Principal    |
| **SearchService**       |   ⏳   | Desarrollo |  No integrado   |
| **RoleService**         |   ✅   |  En DOKS   |  Backend only   |
| **ErrorService**        |   ✅   |  En DOKS   |  Backend only   |
| **PostgreSQL**          |   ✅   |  En DOKS   | Infraestructura |
| **RabbitMQ**            |   ✅   |  En DOKS   | Infraestructura |
| **Redis**               |   ✅   |  En DOKS   | Infraestructura |
| **Consul**              |   ✅   |  En DOKS   | Infraestructura |

### Matriz de Porcentaje de Completitud

```
AuthService          ████████████████████ 100%  ✅ COMPLETO
VehiclesSaleService  ████████████████████ 100%  ✅ COMPLETO
MediaService         ████████████████████ 100%  ✅ COMPLETO
UserService          ████████████████████ 100%  ✅ COMPLETO
ContactService       ████████████████████ 100%  ✅ COMPLETO
NotificationService  ████████████████████ 100%  ✅ COMPLETO
AdminService         ████████████████████ 100%  ✅ COMPLETO
BillingService       ████████████████████ 100%  ✅ COMPLETO
SearchService        ████████████        80%   ⏳ EN DESARROLLO
```

---

## 🔗 Endpoints Resumido por Servicio

### AuthService Endpoints

```
POST   /api/auth/login
POST   /api/auth/register
POST   /api/auth/logout
POST   /api/auth/refresh-token
POST   /api/auth/forgot-password
POST   /api/auth/reset-password
GET    /api/auth/me
PUT    /api/auth/me
```

### VehiclesSaleService Endpoints

```
GET    /api/vehicles
POST   /api/vehicles
GET    /api/vehicles/{id}
PUT    /api/vehicles/{id}
DELETE /api/vehicles/{id}
GET    /api/vehicles/search
GET    /api/vehicles/{id}/similar
GET    /api/vehicles/user/{userId}
POST   /api/vehicles/{id}/favorite
GET    /api/catalog/makes
GET    /api/catalog/models/{makeId}
GET    /api/homepagesections/homepage
```

### MediaService Endpoints

```
POST   /api/media/upload
GET    /api/media/{id}
DELETE /api/media/{id}
POST   /api/media/batch-upload
GET    /api/media/vehicle/{vehicleId}
```

### UserService Endpoints

```
GET    /api/users/{id}
GET    /api/users/me
PUT    /api/users/me
GET    /api/users/me/favorites
POST   /api/users/me/favorites/{vehicleId}
DELETE /api/users/me/favorites/{vehicleId}
```

### ContactService Endpoints

```
POST   /api/contacts/send
GET    /api/contacts/inbox
GET    /api/contacts/{id}
PUT    /api/contacts/{id}/read
GET    /api/contacts/unread-count
```

### NotificationService Endpoints

```
GET    /api/notifications
PUT    /api/notifications/{id}/read
GET    /api/notifications/unread-count
GET    /api/notifications/settings
PUT    /api/notifications/settings
```

### AdminService Endpoints

```
GET    /api/admin/dashboard
GET    /api/admin/vehicles/pending
POST   /api/admin/vehicles/{id}/approve
POST   /api/admin/vehicles/{id}/reject
GET    /api/admin/users
GET    /api/admin/reports
```

### BillingService Endpoints

```
GET    /api/billing/plans
POST   /api/billing/subscribe
GET    /api/billing/subscription
PUT    /api/billing/subscription
GET    /api/billing/invoices
```

---

## 🎯 Configuración del Frontend

### Variables de Entorno Necesarias

```env
# API Gateway
VITE_API_URL=https://api.okla.com.do

# Autenticación
VITE_JWT_STORAGE_KEY=accessToken
VITE_JWT_REFRESH_KEY=refreshToken

# Timeouts
VITE_API_TIMEOUT=30000

# Admin Panel (opcional)
VITE_ADMIN_SERVICE_URL=https://api.okla.com.do/api/admin

# Desarrolllo local
VITE_API_URL=http://localhost:18443
```

### Estructura de Servicios en Frontend

```typescript
src/services/
├── api.ts                    // Axios instance
├── endpoints/
│   ├── authService.ts       // AuthService
│   ├── vehicleService.ts    // VehiclesSaleService
│   ├── userService.ts       // UserService
│   ├── mediaService.ts      // MediaService
│   ├── contactService.ts    // ContactService
│   ├── notificationService.ts // NotificationService
│   ├── adminService.ts      // AdminService
│   └── billingService.ts    // BillingService
└── stores/
    ├── authStore.ts         // Estado de autenticación
    ├── vehicleStore.ts      // Estado de vehículos
    ├── userStore.ts         // Estado de usuario
    └── uiStore.ts           // Estado de UI
```

---

## 📋 Checklist de Configuración

### Antes de Producción

- [ ] Todos los 8 servicios principales desplegados en K8s
- [ ] Gateway configurado con todas las rutas
- [ ] JWT secret compartido entre todos los servicios
- [ ] CORS configurado correctamente
- [ ] PostgreSQL con todas las bases de datos
- [ ] RabbitMQ con todas las colas
- [ ] Redis para cache
- [ ] Consul para service discovery
- [ ] SSL/TLS en api.okla.com.do
- [ ] Frontend variables de entorno configuradas
- [ ] Logs centralizados funcionando
- [ ] Monitoring y alertas configuradas

---

## 🚀 Próximas Mejoras

### Corto Plazo (Q1 2026)

- [ ] Implementar SearchService (Elasticsearch)
- [ ] WebSocket para notificaciones real-time
- [ ] Push notifications
- [ ] Dark mode en frontend
- [ ] Multi-idioma (es, en, pt)

### Medio Plazo (Q2-Q3 2026)

- [ ] CRMService (gestión de leads)
- [ ] ReportsService (análisis de datos)
- [ ] FinanceService (gestión financiera)
- [ ] AppointmentService (test drives)
- [ ] Chat en tiempo real (SignalR)

### Largo Plazo (Q4 2026+)

- [ ] Mobile app (Flutter)
- [ ] API pública (partners)
- [ ] Marketplace de parts y accesorios
- [ ] Sistema de warranty
- [ ] Financing integration (bancos locales)

---

## 📞 Soporte

**Si necesitas información sobre un microservicio específico:**

1. Busca en este documento (Ctrl+F)
2. Ver en `/docs/sprints/frontend/FRONTEND_REQUIREMENTS_ANALYSIS.md`
3. Revisar Swagger del servicio: `http://service-url/swagger`
4. Contactar al propietario del servicio

---

**Documento mantenido por:** Equipo de Desarrollo  
**Última actualización:** Enero 18, 2026  
**Próxima revisión:** Abril 2026
