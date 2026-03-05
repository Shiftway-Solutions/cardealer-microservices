# 🏗️ Diagrama de Arquitectura Frontend-Backend

**Versión:** 1.0  
**Actualizado:** Enero 18, 2026

---

## 📊 Arquitectura General

```
┌──────────────────────────────────────────────────────────────────────┐
│                                                                       │
│                        🌐 NAVEGADOR / CLIENTE                        │
│                                                                       │
│                    Frontend (React 19 + TypeScript)                  │
│                          okla.com.do                                 │
│                                                                       │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │  src/                                                        │    │
│  │  ├── pages/          (Homepage, Search, Vehicles, etc)      │    │
│  │  ├── components/     (UI components)                        │    │
│  │  ├── services/       (API clients)                          │    │
│  │  └── stores/         (Zustand state management)             │    │
│  └─────────────────────────────────────────────────────────────┘    │
│                                                                       │
│  📤 HTTP/HTTPS (Authorization: Bearer {JWT})                         │
│                                                                       │
└──────────────────────────────────────────────────────────────────────┘
                              │
                              │ https://api.okla.com.do:18443
                              ▼
┌──────────────────────────────────────────────────────────────────────┐
│                                                                       │
│              🚀 API GATEWAY (Ocelot) - Punto de Entrada              │
│                                                                       │
│  ✓ Validación de JWT                                                 │
│  ✓ Rate Limiting                                                     │
│  ✓ CORS Handling                                                     │
│  ✓ Logging & Monitoring                                              │
│  ✓ Request/Response Transformation                                   │
│                                                                       │
└──────────────────────────────────────────────────────────────────────┘
                              │
                 ┌────────────┼────────────┬───────────┬──────────┐
                 ▼            ▼            ▼           ▼          ▼
            ┌─────────┐  ┌──────────┐ ┌───────────┐ ┌─────────┐ ...
            │  Auth   │  │ Vehicles │ │   User    │ │ Contact │
            │ Service │  │  Sales   │ │ Service   │ │ Service │
            │         │  │ Service  │ │           │ │         │
            │ Port    │  │          │ │ Port      │ │ Port    │
            │ 5001    │  │ Port     │ │ 5002      │ │ 5003    │
            │ (K8s:   │  │ 5010     │ │ (K8s:     │ │ (K8s:   │
            │ 8080)   │  │ (K8s:    │ │ 8080)     │ │ 8080)   │
            │         │  │ 8080)    │ │           │ │         │
            └─────────┘  └──────────┘ └───────────┘ └─────────┘
                 │            │            │           │
                 ▼            ▼            ▼           ▼
            ┌─────────────────────────────────────────────────┐
            │         📦 PostgreSQL Database (16 DBs)         │
            │                                                 │
            │  authservice │ vehiclessaleservice │ userservice│
            │                                                 │
            └─────────────────────────────────────────────────┘
                              ▲
                              │
                 ┌────────────┴────────────┐
                 ▼                         ▼
            ┌─────────┐            ┌──────────────┐
            │ RabbitMQ│  Events    │    Redis     │
            │ (Async) │◄─ ────────►│   (Cache)    │
            └─────────┘            └──────────────┘
```

---

## 🔄 Flujo de Autenticación

```
User
  │
  ├─ "John Doe"
  └─ "password123"
       │
       ▼
Frontend (React)
  │
  ├─ POST /api/auth/login
  │  {
  │    "email": "john@example.com",
  │    "password": "password123"
  │  }
       │
       ▼
Gateway (Ocelot)
  │
  ├─ Ruta: /api/auth/* → AuthService:8080
  │
       │
       ▼
AuthService (Puerto 5001)
  │
  ├─ Valida email
  ├─ Verifica password (bcrypt)
  ├─ Genera JWT token
  ├─ Genera Refresh token
  │
  └─ Retorna: { accessToken, refreshToken, expiresIn }
       │
       ▼
Frontend
  │
  ├─ localStorage.setItem('accessToken', token)
  ├─ localStorage.setItem('refreshToken', refreshToken)
  │
  └─ Redirects a: /vehicles
       │
       ▼
Request Siguiente
  │
  ├─ GET /api/vehicles
  │ Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
       │
       ▼
Gateway
  │
  ├─ Valida JWT token
  │ ├─ ¿Token válido?
  │ │  ├─ SÍ → Ruta a servicio
  │ │  └─ NO → Retorna 401 Unauthorized
       │
       ▼
VehiclesSaleService
  │
  ├─ Procesa request autenticado
  ├─ Accede a userId del JWT
  │
  └─ Retorna datos
```

---

## 🛍️ Flujo de Listar Vehículos

```
Frontend (Homepage)
  │
  ├─ useEffect(() => {
  │    fetchVehicles()
  │  })
       │
       ▼
vehicleService.ts
  │
  ├─ GET /api/vehicles
  │  ├─ ?page=1
  │  ├─ ?pageSize=20
  │  └─ &make=Toyota (opcional)
       │
       ▼
Gateway (Ocelot)
  │
  ├─ Ruta: /api/vehicles/* → vehiclessaleservice:8080
       │
       ▼
VehiclesSaleService (Puerto 5010)
  │
  ├─ Query a PostgreSQL
  │ ├─ SELECT * FROM vehicles
  │ ├─ WHERE make = 'Toyota'
  │ ├─ ORDER BY createdAt DESC
  │ └─ LIMIT 20 OFFSET 0
       │
       ▼
PostgreSQL (vehiclessaleservice DB)
  │
  ├─ Retorna: 20 vehículos
       │
       ▼
VehiclesSaleService
  │
  ├─ Enriquece con datos:
  │ ├─ Información del seller (UserService)
  │ ├─ URLs de imágenes (MediaService)
  │ ├─ Reviews del vendedor
       │
       ▼
Response:
{
  "data": [
    {
      "id": "uuid",
      "title": "Toyota Corolla 2020",
      "price": 12000,
      "images": [
        { "url": "https://s3.../1.jpg" }
      ],
      "seller": {
        "id": "uuid",
        "name": "John's Cars",
        "rating": 4.5
      }
    }
    ... 19 más
  ],
  "total": 234,
  "page": 1
}
       │
       ▼
Frontend
  │
  ├─ Estado actualizado (vehicleStore)
  │
  ├─ Renderiza:
  │ ├─ Grid de 20 vehículos
  │ ├─ Cada uno con imagen y precio
  │ └─ Paginación
```

---

## 📸 Flujo de Subir Imágenes

```
Seller (Dashboard)
  │
  ├─ Click "Agregar imágenes"
  │
  └─ Selecciona 5 archivos (JPEG, PNG)
       │
       ▼
Frontend
  │
  ├─ FormData.append('file', file1)
  ├─ FormData.append('file', file2)
  ├─ FormData.append('file', file3)
  ├─ FormData.append('file', file4)
  ├─ FormData.append('file', file5)
  ├─ FormData.append('vehicleId', uuid)
  │
  └─ POST /api/media/upload
       │
       ▼
Gateway
  │
  ├─ Valida JWT (Authorization header)
  │
  └─ Ruta: /api/media/* → mediaservice:8080
       │
       ▼
MediaService (Puerto 5020)
  │
  ├─ Valida archivos
  │ ├─ Tipos: JPEG, PNG
  │ └─ Tamaño: Max 5MB cada uno
  │
  ├─ Sube a AWS S3
  │ ├─ Prefijo: okla/vehicles/{vehicleId}/
  │ │ ├─ 1.jpg (original)
  │ │ ├─ 1-thumb.jpg
  │ │ ├─ 1-medium.jpg
  │ │ └─ 1-large.jpg
  │ └─ 5 imágenes = 4-5 archivos por imagen
  │
  ├─ Guarda URLs en PostgreSQL
       │
       ▼
Response:
{
  "uploadedCount": 5,
  "images": [
    {
      "id": "uuid-1",
      "url": "https://s3.../1.jpg",
      "thumbnail": "https://s3.../1-thumb.jpg"
    }
    ... 4 más
  ]
}
       │
       ▼
Frontend
  │
  ├─ Notifica al usuario: "5 imágenes subidas"
  │
  └─ Actualiza preview en UI
```

---

## 💬 Flujo de Contactar Vendedor

```
Comprador (Viewing: Toyota 2020)
  │
  ├─ Lee descripción
  │
  └─ Click "Contactar vendedor"
       │
       ▼
Frontend
  │
  ├─ ¿Está autenticado?
  │ ├─ NO → Redirige a /login
  │ │
  │ └─ SÍ → Abre modal de mensaje
       │
       ▼
Modal "Enviar Mensaje"
  │
  ├─ Escribe: "¿Aún disponible?"
  │
  └─ Click "Enviar"
       │
       ▼
Frontend
  │
  ├─ POST /api/contacts/send
  │ {
  │   "recipientId": "seller-uuid",
  │   "vehicleId": "vehicle-uuid",
  │   "subject": "Pregunta sobre Toyota",
  │   "body": "¿Aún disponible?"
  │ }
       │
       ▼
Gateway
  │
  ├─ Valida JWT
  │
  └─ Ruta: /api/contacts/* → contactservice:8080
       │
       ▼
ContactService (Puerto 5003)
  │
  ├─ Crea conversación
  │ ├─ conversationId = uuid
  │ └─ participants = [buyerId, sellerId]
  │
  ├─ Guarda mensaje en PostgreSQL
  │ ├─ conversationId
  │ ├─ senderId = buyerId
  │ ├─ body = "¿Aún disponible?"
  │ └─ isRead = false
  │
  ├─ Publica evento en RabbitMQ
  │ └─ "MessageSent" → NotificationService
       │
       ▼
NotificationService
  │
  ├─ Escucha evento "MessageSent"
  │
  ├─ Busca preferencias del vendedor
  │ ├─ ¿Email notifications ON?
  │ ├─ ¿SMS notifications ON?
  │ └─ ¿Push notifications ON?
  │
  └─ Envía notificación (email, SMS, push)
       │
       ▼
Response:
{
  "conversationId": "uuid-conv",
  "messageId": "uuid-msg",
  "status": "sent"
}
       │
       ▼
Frontend (Comprador)
  │
  └─ Muestra: "Mensaje enviado a John's Cars"
```

---

## 🔔 Flujo de Notificaciones

```
Seller (Recibe mensaje)
       │
       ▼
ContactService
  │
  └─ Publica evento: "NewMessage"
       │
       ▼
RabbitMQ
  │
  ├─ Queue: notifications-queue
  │
  └─ Routing: notification-service
       │
       ▼
NotificationService (Puerto 5005)
  │
  ├─ Escucha evento
  │
  ├─ Obtiene preferencias del seller
  │ ├─ emailNotifications: true
  │ ├─ pushNotifications: true
  │ └─ smsNotifications: false
  │
  ├─ Envía Email
  │ ├─ Template: "new-message.html"
  │ ├─ A: seller@example.com
  │ └─ Asunto: "Nuevo mensaje de John Doe"
  │
  ├─ Envía Push (optional)
  │ ├─ Título: "Nuevo mensaje"
  │ ├─ Body: "John Doe: ¿Aún disponible?"
  │ └─ Via Firebase Cloud Messaging
  │
  └─ Guarda en base de datos
       │
       ▼
Seller (Check Frontend)
  │
  ├─ Icono de notificación muestra badge "1"
  │
  ├─ GET /api/notifications
  │ ├─ Authorization: Bearer {token}
  │
       │
       ▼
Frontend
  │
  └─ Abre: Dropdown Notifications
       │
       ├─ "John Doe pregunta: ¿Aún disponible?"
       │
       └─ Click → Abre conversación
```

---

## 🛡️ Flujo Admin: Aprobar Vehículo

```
Admin (Dashboard)
  │
  └─ Ve: "47 vehículos pendientes de aprobación"
       │
       ▼
Frontend
  │
  ├─ GET /api/admin/vehicles/pending
  │ ├─ Authorization: Bearer {admin-token}
  │ ├─ Validación: user.role === "Admin"
       │
       ▼
Gateway
  │
  ├─ Valida JWT
  │ ├─ Verifica que sea Admin
  │ │ └─ NO → Retorna 403 Forbidden
  │
  └─ Ruta: /api/admin/* → adminservice:8080
       │
       ▼
AdminService (Puerto 5007)
  │
  ├─ Query a PostgreSQL
  │ └─ SELECT * FROM vehicles WHERE status = 'Pending'
       │
       ▼
Response:
[
  {
    "id": "uuid",
    "title": "Toyota 2020",
    "seller": "John's Cars",
    "createdAt": "2026-01-15...",
    "images": ["urls..."],
    "status": "Pending"
  }
  ... más
]
       │
       ▼
Frontend
  │
  ├─ Muestra lista de vehículos pendientes
  │
  └─ Admin hace click "Aprobar"
       │
       ▼
Modal "Confirmar aprobación"
  │
  ├─ Campo: "Notas (opcional)"
  │ ├─ "Verificado, imágenes claras"
  │
  └─ Click "Aprobar"
       │
       ▼
Frontend
  │
  ├─ POST /api/admin/vehicles/uuid/approve
  │ ├─ Authorization: Bearer {admin-token}
  │ ├─ Body: { "notes": "Verificado..." }
       │
       ▼
AdminService
  │
  ├─ Actualiza status a "Active"
  │ ├─ UPDATE vehicles SET status = 'Active'
  │ ├─ WHERE id = 'uuid'
  │
  ├─ Publica evento: "VehicleApproved"
  │ └─ RabbitMQ → NotificationService
       │
       ▼
NotificationService
  │
  ├─ Obtiene email del seller
  │
  └─ Envía email: "Tu Toyota fue aprobada! Ahora es visible"
       │
       ▼
Response:
{
  "id": "uuid",
  "status": "Active",
  "message": "Vehículo aprobado exitosamente"
}
       │
       ▼
Frontend (Admin)
  │
  └─ Notifica: "Vehículo aprobado"
```

---

## 📊 Microservicios por Funcionalidad

```
┌────────────────────────────────────────────────────────────────┐
│                                                                 │
│  FRONTEND NECESITA PARA FUNCIONAR COMPLETAMENTE                │
│                                                                 │
├────────────────────────────────────────────────────────────────┤
│                                                                 │
│  🔴 CRÍTICOS (Must-Have):                                      │
│  ├─ AuthService ............ Autenticación                     │
│  ├─ VehiclesSaleService .... CRUD de vehículos                │
│  ├─ MediaService ........... Subir/gestionar imágenes         │
│  └─ Gateway (Ocelot) ....... Enrutamiento de requests         │
│                                                                 │
│  🟠 IMPORTANTES (Should-Have):                                 │
│  ├─ UserService ............ Perfiles de usuarios              │
│  ├─ ContactService ......... Mensajería                        │
│  ├─ NotificationService .... Notificaciones                    │
│  └─ AdminService ........... Panel de administración           │
│                                                                 │
│  🟡 OPCIONALES (Nice-to-Have):                                │
│  ├─ SearchService .......... Búsqueda avanzada (Elasticsearch) │
│  └─ BillingService ......... Pagos y suscripciones            │
│                                                                 │
│  🔵 INFRAESTRUCTURA:                                           │
│  ├─ PostgreSQL ............. Base de datos                     │
│  ├─ RabbitMQ ............... Eventos async                    │
│  ├─ Redis .................. Cache                             │
│  └─ Consul ................. Service Discovery                │
│                                                                 │
└────────────────────────────────────────────────────────────────┘
```

---

## 🌐 Request Flow Completo

```
1. USUARIO REALIZA ACCIÓN
   │
   └─ Click en botón, escribe en campo, etc.

2. FRONTEND JAVASCRIPT EVENT
   │
   └─ onClick, onChange, onSubmit

3. VALIDACIÓN FRONTEND (Optional)
   │
   ├─ Validar email format
   ├─ Validar password strength
   └─ Validar campos requeridos

4. CONSTRUIR REQUEST
   │
   ├─ GET, POST, PUT, DELETE
   ├─ URL: /api/service/endpoint
   ├─ Headers: { Authorization: Bearer token }
   └─ Body: { datos }

5. AXIOS INTERCEPTOR
   │
   ├─ Agrega token JWT
   └─ Agrega headers comunes

6. ENVÍO HTTPS
   │
   └─ okla.com.do → api.okla.com.do:18443

7. GATEWAY (Ocelot)
   │
   ├─ Recibe request
   ├─ Valida HTTPS/TLS
   ├─ Valida JWT token
   ├─ Rate limiting
   ├─ Logging
   └─ Ruta a servicio correcto

8. MICROSERVICIO
   │
   ├─ Recibe request
   ├─ Extrae userId del JWT
   ├─ Ejecuta lógica
   ├─ Accede a PostgreSQL
   ├─ Publica eventos en RabbitMQ
   └─ Retorna respuesta

9. RESPUESTA JSON
   │
   ├─ Status: 200, 400, 401, 500, etc.
   └─ Body: { data, error, message }

10. AXIOS INTERCEPTOR (Response)
    │
    ├─ ¿Status 401 (Expired)?
    │ └─ POST /api/auth/refresh-token
    │
    └─ ¿Status 500?
        └─ Log error, notificar usuario

11. FRONTEND STATE UPDATE
    │
    ├─ Zustand store
    ├─ useState
    └─ useEffect

12. RENDER KOMPONENTE
    │
    ├─ Mostrar datos
    ├─ Notificar usuario
    └─ Cargar siguiente pantalla
```

---

## 🔐 Seguridad & Validación

```
Frontend             →  Gateway         →  Servicio
    │                      │                   │
    │                      │                   │
1. Validar form      1. Validar JWT     1. Validar usuario
2. Encriptar (TLS)   2. Check role      2. Verificar permisos
3. Enviar token      3. Rate limit      3. Sanitizar input
                     4. Logging         4. Acceso a BD
                                        5. Responder
```

---

**Diagrama de Arquitectura - OKLA Marketplace**  
Enero 2026
