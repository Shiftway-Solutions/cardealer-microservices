# 🎯 Guía Rápida: Microservicios del Frontend

**Versión:** 1.0 - Referencia Rápida  
**Actualizado:** Enero 18, 2026

---

## 📊 Microservicios de un Vistazo

### Nivel de Criticidad

| Servicio                |  Criticidad   | Sin él                | Puerto | Status |
| ----------------------- | :-----------: | --------------------- | :----: | :----: |
| **AuthService**         |  🔴 CRÍTICO   | ❌ Login falla        |  5001  |   ✅   |
| **VehiclesSaleService** |  🔴 CRÍTICO   | ❌ Homepage vacío     |  5010  |   ✅   |
| **MediaService**        |  🔴 CRÍTICO   | ❌ Sin imágenes       |  5020  |   ✅   |
| **Gateway**             |  🔴 CRÍTICO   | ❌ Sin rutas          | 18443  |   ✅   |
| **UserService**         | 🟠 IMPORTANTE | ⚠️ Sin perfiles       |  5002  |   ✅   |
| **ContactService**      | 🟠 IMPORTANTE | ⚠️ Sin mensajes       |  5003  |   ✅   |
| **NotificationService** | 🟠 IMPORTANTE | ⚠️ Sin notificaciones |  5005  |   ✅   |
| **AdminService**        | 🟠 IMPORTANTE | ⚠️ Sin panel admin    |  5007  |   ✅   |
| **BillingService**      |  🟡 OPCIONAL  | ℹ️ Sin pagos          |  5023  |   ✅   |
| **SearchService**       |  🟡 OPCIONAL  | ℹ️ Búsqueda lenta     |  5030  |   ⏳   |

---

## 🎯 Flujo de Inicio (Login)

```
Usuario escribe credenciales
          ↓
Frontend: POST /api/auth/login
          ↓
Gateway (Ocelot): Valida request
          ↓
AuthService: Verifica email/password en PostgreSQL
          ↓
✅ Éxito: Retorna JWT token + RefreshToken
          ↓
Frontend: Almacena JWT en localStorage
          ↓
Frontend: Incluye JWT en header para requests posteriores
          ↓
Gateway: Valida JWT antes de routear a otros servicios
          ↓
✅ Usuario autenticado y puede usar frontend
```

---

## 🏠 Homepage Necesita

```
Frontend carga Homepage
          ↓
VehiclesSaleService: GET /api/homepagesections/homepage
          ↓
Base de datos PostgreSQL devuelve secciones de vehículos
          ↓
MediaService: GET /api/media/vehicle/{id}
          ↓
AWS S3 devuelve URLs de imágenes
          ↓
Frontend renderiza homepage con:
  ✅ Carrusel de destacados
  ✅ Grid de sedanes
  ✅ Grid de SUVs
  ✅ Etc (cada sección con imágenes)
```

---

## 🔐 Autenticación JWT

### Flow Completo

```
1. LOGIN
   POST /api/auth/login
   {
     "email": "user@example.com",
     "password": "password123"
   }

   Response:
   {
     "accessToken": "eyJhbGciOiJIUzI1NiIs...",
     "refreshToken": "eyJhbGciOiJIUzI1NiIs...",
     "expiresIn": 3600,
     "user": {
       "id": "uuid",
       "email": "user@example.com",
       "name": "John Doe",
       "role": "User"
     }
   }

2. ALMACENAR
   localStorage.setItem('accessToken', accessToken)
   localStorage.setItem('refreshToken', refreshToken)

3. USAR EN REQUESTS
   GET /api/users/me
   Header: Authorization: Bearer eyJhbGciOiJIUzI1NiIs...

   Response:
   {
     "id": "uuid",
     "email": "user@example.com",
     "name": "John Doe",
     "verified": true,
     ...
   }

4. REFRESH (cada 60 minutos)
   POST /api/auth/refresh-token
   {
     "refreshToken": "eyJhbGciOiJIUzI1NiIs..."
   }

   Response: Nuevo accessToken

5. LOGOUT
   POST /api/auth/logout
   (Borra tokens del localStorage)
```

---

## 🛍️ Flujo de Vehículos

### Listar Vehículos

```
GET /api/vehicles?page=1&pageSize=20&make=Toyota&year=2020

Response:
{
  "data": [
    {
      "id": "uuid",
      "title": "Toyota Corolla 2020",
      "make": "Toyota",
      "model": "Corolla",
      "year": 2020,
      "price": 12000,
      "mileage": 45000,
      "condition": "Used",
      "transmission": "Automatic",
      "fuelType": "Gasoline",
      "images": [
        {
          "id": "uuid",
          "url": "https://s3.amazonaws.com/okla/vehicle123/1.jpg"
        }
      ],
      "seller": {
        "id": "uuid",
        "name": "John's Cars",
        "rating": 4.5,
        "reviewCount": 23
      },
      "createdAt": "2026-01-15T10:30:00Z"
    }
  ],
  "total": 150,
  "page": 1,
  "pageSize": 20
}
```

### Detalle de Vehículo

```
GET /api/vehicles/uuid-123

Response:
{
  "id": "uuid",
  "title": "Toyota Corolla 2020",
  "description": "Excelente condición, mantenimiento al día",
  "price": 12000,
  "specifications": {
    "make": "Toyota",
    "model": "Corolla",
    "year": 2020,
    "transmission": "Automatic",
    "engine": "1.8L 4-cylinder",
    "horsepower": 132,
    "mileage": 45000,
    "fuelType": "Gasoline",
    "mpg": 28,
    "exteriorColor": "Silver",
    "interiorColor": "Black",
    "doors": 4,
    "seats": 5
  },
  "images": [ /* 10-20 imágenes */ ],
  "seller": {
    "id": "uuid",
    "name": "John's Cars",
    "email": "john@example.com",
    "phone": "+1-555-0100",
    "verified": true,
    "rating": 4.5,
    "reviewCount": 23,
    "location": {
      "city": "Santo Domingo",
      "province": "Santo Domingo",
      "latitude": 18.4861,
      "longitude": -69.9312
    }
  },
  "features": [
    "Air Conditioning",
    "Power Steering",
    "Power Windows",
    "Cruise Control",
    "Bluetooth"
  ],
  "isFavorited": false,
  "viewCount": 234,
  "contactCount": 12,
  "createdAt": "2026-01-15T10:30:00Z"
}
```

### Crear Vehículo (Seller/Dealer)

```
POST /api/vehicles
Header: Authorization: Bearer {token}

Body:
{
  "title": "Toyota Corolla 2020",
  "description": "Excelente condición",
  "specifications": {
    "make": "Toyota",
    "model": "Corolla",
    "year": 2020,
    "transmission": "Automatic",
    "price": 12000,
    "mileage": 45000,
    "fuelType": "Gasoline",
    "exteriorColor": "Silver",
    "interiorColor": "Black"
  },
  "features": ["Air Conditioning", "Bluetooth", "Cruise Control"]
}

Response:
{
  "id": "uuid-new",
  "title": "Toyota Corolla 2020",
  "status": "Pending",  // Awaiting admin approval
  "message": "Vehículo creado exitosamente. Pendiente de aprobación."
}
```

---

## 📸 Subir Imágenes

### Después de Crear Vehículo

```
POST /api/media/upload
Header: Authorization: Bearer {token}
Body: FormData with images

FormData:
  file: <image1.jpg>
  file: <image2.jpg>
  ... (hasta 20 imágenes)
  vehicleId: uuid-123

Response:
{
  "uploadedCount": 5,
  "message": "5 imágenes subidas exitosamente",
  "images": [
    {
      "id": "uuid-img1",
      "url": "https://s3.amazonaws.com/okla/vehicle-uuid/1.jpg",
      "thumbnail": "https://s3.amazonaws.com/okla/vehicle-uuid/1-thumb.jpg"
    }
  ]
}
```

---

## 👤 Perfil de Usuario

### Obtener Mi Perfil

```
GET /api/users/me
Header: Authorization: Bearer {token}

Response:
{
  "id": "uuid",
  "email": "john@example.com",
  "fullName": "John Doe",
  "phone": "+1-555-0100",
  "accountType": "Individual",  // o "Dealer"
  "avatar": "https://s3.amazonaws.com/okla/avatars/uuid.jpg",
  "bio": "Amante de los autos",
  "joinedDate": "2025-10-15T00:00:00Z",
  "isVerified": true,
  "rating": 4.5,
  "reviewCount": 23,
  "location": {
    "city": "Santo Domingo",
    "province": "Santo Domingo"
  },
  "preferences": {
    "emailNotifications": true,
    "pushNotifications": true,
    "smsNotifications": false
  }
}
```

### Actualizar Perfil

```
PUT /api/users/me
Header: Authorization: Bearer {token}

Body:
{
  "fullName": "John Doe Updated",
  "bio": "Vendedor profesional de autos",
  "phone": "+1-555-0101"
}

Response:
{
  "id": "uuid",
  "fullName": "John Doe Updated",
  "bio": "Vendedor profesional de autos",
  ...
}
```

---

## 💬 Contactar Vendedor

### Enviar Mensaje

```
POST /api/contacts/send
Header: Authorization: Bearer {token}

Body:
{
  "recipientId": "uuid-seller",
  "vehicleId": "uuid-vehicle",
  "subject": "Pregunta sobre Toyota Corolla",
  "body": "¿Aún disponible? ¿Puedo verlo mañana?"
}

Response:
{
  "conversationId": "uuid-conv",
  "messageId": "uuid-msg",
  "status": "sent",
  "message": "Mensaje enviado exitosamente"
}
```

### Ver Bandeja de Entrada

```
GET /api/contacts/inbox
Header: Authorization: Bearer {token}

Response:
{
  "conversations": [
    {
      "conversationId": "uuid",
      "with": {
        "id": "uuid",
        "name": "John's Cars",
        "avatar": "url"
      },
      "vehicle": {
        "id": "uuid",
        "title": "Toyota Corolla 2020"
      },
      "lastMessage": {
        "body": "¿Aún disponible?",
        "createdAt": "2026-01-18T15:30:00Z",
        "isRead": false
      },
      "unreadCount": 2
    }
  ],
  "total": 5
}
```

---

## 🔔 Notificaciones

### Centro de Notificaciones

```
GET /api/notifications?unreadOnly=true
Header: Authorization: Bearer {token}

Response:
{
  "notifications": [
    {
      "id": "uuid",
      "type": "NewMessage",
      "title": "Nuevo mensaje de John's Cars",
      "body": "¿Aún disponible el Toyota?",
      "relatedId": "uuid-conversation",
      "isRead": false,
      "createdAt": "2026-01-18T15:30:00Z"
    },
    {
      "id": "uuid2",
      "type": "VehicleApproved",
      "title": "Tu vehículo fue aprobado",
      "body": "Tu Toyota Corolla ahora está visible",
      "relatedId": "uuid-vehicle",
      "isRead": true,
      "createdAt": "2026-01-17T10:15:00Z"
    }
  ],
  "total": 5,
  "unreadCount": 2
}
```

### Marcar como Leído

```
PUT /api/notifications/uuid/read
Header: Authorization: Bearer {token}

Response:
{
  "id": "uuid",
  "isRead": true
}
```

---

## 🛡️ Panel de Admin (Solo Admin)

### Dashboard

```
GET /api/admin/dashboard
Header: Authorization: Bearer {token}
Constraint: User.Role must be "Admin"

Response:
{
  "stats": {
    "totalVehicles": 1250,
    "activeVehicles": 980,
    "pendingApproval": 47,
    "rejectedToday": 3,
    "totalUsers": 3500,
    "activeToday": 420,
    "totalReports": 12,
    "unresolvedReports": 3,
    "revenue": 15420.50
  },
  "recentActivity": [
    {
      "type": "VehicleCreated",
      "vehicleTitle": "BMW X5 2024",
      "seller": "Dealer Premium",
      "createdAt": "2026-01-18T16:30:00Z"
    }
  ]
}
```

### Aprobar Vehículo

```
POST /api/admin/vehicles/uuid-123/approve
Header: Authorization: Bearer {token}

Body:
{
  "notes": "Verificado, todas las imágenes claras"
}

Response:
{
  "id": "uuid-123",
  "status": "Active",
  "message": "Vehículo aprobado exitosamente"
}
```

### Rechazar Vehículo

```
POST /api/admin/vehicles/uuid-123/reject
Header: Authorization: Bearer {token}

Body:
{
  "reason": "Imágenes borrosas, falta documentación"
}

Response:
{
  "id": "uuid-123",
  "status": "Rejected",
  "message": "Vehículo rechazado"
}
```

---

## 💳 Planes y Billing (Dealers)

### Ver Planes

```
GET /api/billing/plans

Response:
{
  "plans": [
    {
      "id": "starter",
      "name": "Starter",
      "price": 49,
      "currency": "USD",
      "billingPeriod": "monthly",
      "maxListings": 15,
      "features": [
        "Hasta 15 vehículos activos",
        "Dashboard básico",
        "Email support",
        "Renovación automática"
      ]
    },
    {
      "id": "pro",
      "name": "Pro",
      "price": 129,
      "currency": "USD",
      "billingPeriod": "monthly",
      "maxListings": 50,
      "features": [
        "Hasta 50 vehículos activos",
        "Dashboard avanzado",
        "Analytics detallados",
        "Priority support",
        "API access",
        "Hasta 5 usuarios"
      ]
    }
  ]
}
```

### Crear Suscripción

```
POST /api/billing/subscribe
Header: Authorization: Bearer {token}

Body:
{
  "planId": "pro",
  "paymentMethod": "stripe"  // o "azul"
}

Response:
{
  "subscriptionId": "sub-uuid",
  "planName": "Pro",
  "status": "Active",
  "currentPeriodStart": "2026-01-18T00:00:00Z",
  "currentPeriodEnd": "2026-02-18T00:00:00Z",
  "nextBillingDate": "2026-02-18T00:00:00Z"
}
```

---

## 🔄 Favoritos

### Agregar a Favoritos

```
POST /api/vehicles/uuid-123/favorite
Header: Authorization: Bearer {token}

Response:
{
  "vehicleId": "uuid-123",
  "isFavorited": true,
  "message": "Vehículo agregado a favoritos"
}
```

### Ver Mis Favoritos

```
GET /api/users/me/favorites
Header: Authorization: Bearer {token}

Response:
{
  "favorites": [
    {
      "id": "uuid-123",
      "title": "Toyota Corolla 2020",
      "price": 12000,
      "thumbnail": "url",
      "addedAt": "2026-01-15T10:30:00Z"
    }
  ],
  "total": 5
}
```

---

## ⚙️ Variables de Entorno Frontend

```bash
# Producción (okla.com.do)
VITE_API_URL=https://api.okla.com.do

# Desarrollo local (docker-compose)
VITE_API_URL=http://localhost:18443

# Testing
VITE_API_URL=http://localhost:15095

# Timeouts
VITE_API_TIMEOUT=30000

# Feature flags (opcional)
VITE_ENABLE_ADMIN_PANEL=true
VITE_ENABLE_DEALER_FEATURES=true
VITE_ENABLE_SEARCH_SERVICE=false
```

---

## 🔌 Estructura de Axios/API

```typescript
// src/services/api.ts

import axios from "axios";

const API_BASE_URL = import.meta.env.VITE_API_URL || "http://localhost:18443";

const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
  headers: { "Content-Type": "application/json" },
});

// Request interceptor
api.interceptors.request.use((config) => {
  const token = localStorage.getItem("accessToken");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Response interceptor
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      // Token expirado, intentar refresh
      // ...
    }
    return Promise.reject(error);
  },
);

export default api;
```

---

## 🚦 Status Codes Importantes

| Código | Significado                       | Acción                   |
| ------ | --------------------------------- | ------------------------ |
| 200    | OK - Éxito                        | Procesar respuesta       |
| 201    | Created - Recurso creado          | Procesar y notificar     |
| 400    | Bad Request - Error en datos      | Mostrar error al usuario |
| 401    | Unauthorized - No autenticado     | Redirigir a login        |
| 403    | Forbidden - No autorizado         | Mostrar acceso denegado  |
| 404    | Not Found - No existe             | Mostrar página 404       |
| 429    | Too Many Requests - Rate limited  | Mostrar espera           |
| 500    | Server Error - Error del servidor | Contactar soporte        |

---

## 📱 Flujo Típico del Usuario

```
1. PRIMERA VISITA (No autenticado)
   Homepage → Ver vehículos sin auth

2. BUSCAR VEHÍCULO
   Search → Filtros → Lista → Detalle

3. CONTACTAR VENDEDOR
   Botón "Contactar" → Redirige a login

4. LOGIN
   Email + Password → JWT token

5. VER PERFIL
   Icono usuario → Mi Perfil

6. CONTACTAR (después de login)
   Botón "Contactar" → Formulario → Enviar

7. VER MENSAJES
   Icono mensajes → Inbox → Responder

8. AGREGAR A FAVORITOS
   Icono corazón → Se agrega

9. LOGOUT
   Settings → Logout → Redirige a homepage
```

---

**Referencia Rápida - Enero 2026**
