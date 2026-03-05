# 📦 Mock Data y Ejemplos de Respuestas API

> **Propósito:** Proveer datos de ejemplo para todas las APIs críticas
> **Uso:** Testing, desarrollo sin backend, documentación
> **Última actualización:** Enero 31, 2026

---

## 📋 ÍNDICE

1. [Vehículos](#-vehículos)
2. [Usuarios](#-usuarios)
3. [Dealers](#-dealers)
4. [Autenticación](#-autenticación)
5. [Catálogo](#-catálogo)
6. [Mensajes](#-mensajes)
7. [Notificaciones](#-notificaciones)
8. [Pagos](#-pagos)
9. [Reviews](#-reviews)
10. [Alertas](#-alertas)

---

## 🚗 VEHÍCULOS

### GET /api/vehicles

**Request:**

```http
GET /api/vehicles?page=1&pageSize=20&makeSlug=toyota&yearFrom=2020
```

**Response:**

```json
{
  "items": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440001",
      "slug": "toyota-camry-se-2024-santo-domingo",
      "title": "Toyota Camry SE 2024",
      "year": 2024,
      "make": "Toyota",
      "makeSlug": "toyota",
      "model": "Camry",
      "modelSlug": "camry",
      "trim": "SE",
      "price": 1850000,
      "originalPrice": 1950000,
      "mileage": 15000,
      "condition": "used",
      "fuelType": "gasoline",
      "transmission": "automatic",
      "primaryImage": "https://cdn.okla.com.do/vehicles/550e8400/main.webp",
      "imagesCount": 24,
      "has360View": true,
      "city": "Santo Domingo",
      "province": "Distrito Nacional",
      "sellerType": "dealer",
      "sellerId": "dealer-001",
      "sellerName": "AutoMax RD",
      "sellerAvatar": "https://cdn.okla.com.do/dealers/automax/logo.webp",
      "isVerified": true,
      "isFeatured": true,
      "isBoosted": false,
      "viewCount": 1250,
      "favoriteCount": 45,
      "status": "active",
      "createdAt": "2026-01-15T10:30:00Z",
      "updatedAt": "2026-01-28T14:20:00Z"
    },
    {
      "id": "550e8400-e29b-41d4-a716-446655440002",
      "slug": "honda-accord-sport-2023-santiago",
      "title": "Honda Accord Sport 2023",
      "year": 2023,
      "make": "Honda",
      "makeSlug": "honda",
      "model": "Accord",
      "modelSlug": "accord",
      "trim": "Sport",
      "price": 1650000,
      "originalPrice": null,
      "mileage": 25000,
      "condition": "used",
      "fuelType": "gasoline",
      "transmission": "automatic",
      "primaryImage": "https://cdn.okla.com.do/vehicles/550e8400-002/main.webp",
      "imagesCount": 18,
      "has360View": false,
      "city": "Santiago",
      "province": "Santiago",
      "sellerType": "individual",
      "sellerId": "user-123",
      "sellerName": "Juan Pérez",
      "sellerAvatar": null,
      "isVerified": false,
      "isFeatured": false,
      "isBoosted": false,
      "viewCount": 320,
      "favoriteCount": 12,
      "status": "active",
      "createdAt": "2026-01-20T08:15:00Z",
      "updatedAt": "2026-01-25T11:45:00Z"
    }
  ],
  "totalItems": 1250,
  "totalPages": 63,
  "currentPage": 1,
  "pageSize": 20,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

### GET /api/vehicles/{slug}

**Response (Vehículo completo):**

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "slug": "toyota-camry-se-2024-santo-domingo",
  "title": "Toyota Camry SE 2024",
  "description": "Vehículo en excelentes condiciones, único dueño. Mantenimiento al día en dealer autorizado. Incluye garantía extendida de 2 años. Pintura original, interior impecable.\n\nCaracterísticas destacadas:\n- Sistema de navegación\n- Cámara de reversa\n- Sensores de estacionamiento\n- Bluetooth y Apple CarPlay\n- Asientos de cuero sintético",
  "year": 2024,
  "make": "Toyota",
  "makeSlug": "toyota",
  "model": "Camry",
  "modelSlug": "camry",
  "trim": "SE",
  "price": 1850000,
  "originalPrice": 1950000,
  "mileage": 15000,
  "condition": "used",
  "vin": "4T1BF1FK5EU******",

  "engine": "2.5L 4-Cylinder",
  "horsepower": 206,
  "cylinders": 4,
  "displacement": 2.5,
  "fuelType": "gasoline",
  "transmission": "automatic",
  "driveType": "fwd",
  "bodyType": "sedan",
  "doors": 4,
  "seats": 5,
  "exteriorColor": "Celestial Silver Metallic",
  "interiorColor": "Black",

  "images": [
    {
      "id": "img-001",
      "url": "https://cdn.okla.com.do/vehicles/550e8400/1.webp",
      "thumbnailUrl": "https://cdn.okla.com.do/vehicles/550e8400/1-thumb.webp",
      "alt": "Toyota Camry 2024 - Vista frontal",
      "order": 1,
      "isPrimary": true
    },
    {
      "id": "img-002",
      "url": "https://cdn.okla.com.do/vehicles/550e8400/2.webp",
      "thumbnailUrl": "https://cdn.okla.com.do/vehicles/550e8400/2-thumb.webp",
      "alt": "Toyota Camry 2024 - Vista lateral",
      "order": 2,
      "isPrimary": false
    }
  ],
  "view360Url": "https://cdn.okla.com.do/vehicles/550e8400/360/",
  "videoUrl": null,

  "featureCategories": [
    {
      "name": "Seguridad",
      "features": [
        "Bolsas de aire frontales",
        "Bolsas de aire laterales",
        "Sistema de frenos ABS",
        "Control de estabilidad",
        "Cámara de reversa"
      ]
    },
    {
      "name": "Confort",
      "features": [
        "Aire acondicionado automático",
        "Asientos eléctricos",
        "Volante multifunción",
        "Cruise control",
        "Espejos eléctricos"
      ]
    },
    {
      "name": "Tecnología",
      "features": [
        "Pantalla táctil 8\"",
        "Apple CarPlay",
        "Android Auto",
        "Bluetooth",
        "Puerto USB"
      ]
    }
  ],

  "city": "Santo Domingo",
  "province": "Distrito Nacional",
  "address": "Av. Winston Churchill esq. Gustavo Mejía Ricart",
  "coordinates": {
    "lat": 18.4666,
    "lng": -69.9388
  },

  "seller": {
    "id": "dealer-001",
    "name": "AutoMax RD",
    "type": "dealer",
    "avatar": "https://cdn.okla.com.do/dealers/automax/logo.webp",
    "phone": "+1 809-555-0123",
    "whatsapp": "+1 809-555-0123",
    "email": "ventas@automaxrd.com",
    "isVerified": true,
    "rating": 4.8,
    "totalReviews": 156,
    "totalListings": 45,
    "memberSince": "2022-03-15",
    "responseTime": "< 1 hora",
    "responseRate": 98
  },

  "dealerId": "dealer-001",
  "dealer": {
    "id": "dealer-001",
    "slug": "automax-rd",
    "businessName": "AutoMax RD",
    "logoUrl": "https://cdn.okla.com.do/dealers/automax/logo.webp",
    "city": "Santo Domingo",
    "province": "Distrito Nacional",
    "isVerified": true,
    "isPremium": true,
    "rating": 4.8,
    "totalReviews": 156,
    "totalListings": 45,
    "memberSince": "2022-03-15"
  },

  "previousOwners": 1,
  "serviceHistory": true,
  "accidentFree": true,

  "acceptsFinancing": true,
  "acceptsTradeIn": true,
  "monthlyPaymentEstimate": 32500,

  "viewCount": 1250,
  "favoriteCount": 45,
  "status": "active",
  "createdAt": "2026-01-15T10:30:00Z",
  "updatedAt": "2026-01-28T14:20:00Z"
}
```

---

## 👤 USUARIOS

### GET /api/users/me

**Response:**

```json
{
  "id": "user-550e8400-e29b-41d4",
  "email": "juan.perez@email.com",
  "fullName": "Juan Pérez García",
  "firstName": "Juan",
  "lastName": "Pérez García",
  "phone": "+1 809-555-1234",
  "avatar": "https://cdn.okla.com.do/users/user-550e8400/avatar.webp",
  "role": "buyer",
  "isEmailVerified": true,
  "isPhoneVerified": true,
  "isKycVerified": false,
  "kycStatus": "pending",
  "createdAt": "2025-06-15T08:30:00Z",
  "updatedAt": "2026-01-20T14:45:00Z",
  "lastLoginAt": "2026-01-31T09:15:00Z"
}
```

### GET /api/users/{id}/profile

**Response (Perfil público):**

```json
{
  "id": "user-550e8400-e29b-41d4",
  "fullName": "Juan Pérez G.",
  "avatar": "https://cdn.okla.com.do/users/user-550e8400/avatar.webp",
  "memberSince": "2025-06-15T08:30:00Z",
  "isVerified": true,
  "rating": 4.5,
  "totalReviews": 8,
  "totalListings": 3,
  "responseTime": "1-2 horas"
}
```

---

## 🏪 DEALERS

### GET /api/dealers/{slug}

**Response:**

```json
{
  "id": "dealer-001",
  "slug": "automax-rd",
  "businessName": "AutoMax RD",
  "legalName": "AutoMax Dominicana SRL",
  "rnc": "131-45678-9",
  "type": "independent",
  "status": "active",
  "plan": "pro",

  "logoUrl": "https://cdn.okla.com.do/dealers/automax/logo.webp",
  "coverImageUrl": "https://cdn.okla.com.do/dealers/automax/cover.webp",

  "email": "info@automaxrd.com",
  "phone": "+1 809-555-0123",
  "whatsapp": "+1 809-555-0123",
  "website": "https://automaxrd.com",

  "address": "Av. Winston Churchill #123",
  "city": "Santo Domingo",
  "province": "Distrito Nacional",
  "coordinates": {
    "lat": 18.4666,
    "lng": -69.9388
  },

  "businessHours": [
    {
      "day": "monday",
      "isOpen": true,
      "openTime": "08:00",
      "closeTime": "18:00"
    },
    {
      "day": "tuesday",
      "isOpen": true,
      "openTime": "08:00",
      "closeTime": "18:00"
    },
    {
      "day": "wednesday",
      "isOpen": true,
      "openTime": "08:00",
      "closeTime": "18:00"
    },
    {
      "day": "thursday",
      "isOpen": true,
      "openTime": "08:00",
      "closeTime": "18:00"
    },
    {
      "day": "friday",
      "isOpen": true,
      "openTime": "08:00",
      "closeTime": "18:00"
    },
    {
      "day": "saturday",
      "isOpen": true,
      "openTime": "09:00",
      "closeTime": "14:00"
    },
    { "day": "sunday", "isOpen": false }
  ],

  "description": "Somos un dealer familiar con más de 15 años de experiencia en el mercado automotriz dominicano. Nos especializamos en vehículos japoneses y coreanos de alta calidad.",
  "specialties": ["SUVs", "Sedanes", "Vehículos familiares"],
  "brands": ["Toyota", "Honda", "Hyundai", "Kia", "Nissan"],

  "isVerified": true,
  "isPremium": true,
  "verificationStatus": "verified",

  "subscription": {
    "plan": "pro",
    "status": "active",
    "currentPeriodStart": "2026-01-01T00:00:00Z",
    "currentPeriodEnd": "2026-02-01T00:00:00Z",
    "maxActiveListings": 50,
    "usedListings": 45,
    "features": [
      "Hasta 50 vehículos",
      "Badge verificado",
      "Estadísticas avanzadas",
      "CRM de leads",
      "Importación CSV"
    ],
    "price": 129,
    "currency": "USD",
    "billingInterval": "monthly",
    "cancelAtPeriodEnd": false
  },

  "stats": {
    "totalViews": 45000,
    "totalInquiries": 1250,
    "totalSales": 89,
    "avgResponseTime": 25,
    "responseRate": 98,
    "conversionRate": 7.1,
    "inventoryValue": 75000000,
    "avgDaysOnMarket": 21
  },

  "rating": 4.8,
  "totalReviews": 156,
  "totalListings": 45,

  "locations": [
    {
      "id": "loc-001",
      "name": "Sede Principal",
      "type": "headquarters",
      "address": "Av. Winston Churchill #123",
      "city": "Santo Domingo",
      "province": "Distrito Nacional",
      "phone": "+1 809-555-0123",
      "coordinates": { "lat": 18.4666, "lng": -69.9388 },
      "isPrimary": true
    }
  ],

  "employeeCount": 12,
  "establishedDate": "2010-05-15",
  "memberSince": "2022-03-15",
  "createdAt": "2022-03-15T10:00:00Z",
  "updatedAt": "2026-01-30T16:30:00Z"
}
```

---

## 🔐 AUTENTICACIÓN

### POST /api/auth/login

**Request:**

```json
{
  "email": "juan.perez@email.com",
  "password": "SecurePass123!"
}
```

**Response (Success):**

```json
{
  "success": true,
  "user": {
    "id": "user-550e8400-e29b-41d4",
    "email": "juan.perez@email.com",
    "fullName": "Juan Pérez García",
    "avatar": "https://cdn.okla.com.do/users/user-550e8400/avatar.webp",
    "role": "buyer",
    "isEmailVerified": true
  },
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4...",
  "expiresAt": 1706789400000
}
```

**Response (Error):**

```json
{
  "success": false,
  "message": "Credenciales inválidas",
  "statusCode": 401,
  "errorCode": "INVALID_CREDENTIALS"
}
```

### POST /api/auth/register

**Request:**

```json
{
  "email": "nuevo.usuario@email.com",
  "password": "SecurePass123!",
  "firstName": "María",
  "lastName": "González",
  "phone": "+1 809-555-5678",
  "acceptTerms": true,
  "acceptMarketing": false
}
```

**Response:**

```json
{
  "success": true,
  "message": "Usuario creado. Revisa tu email para verificar tu cuenta.",
  "userId": "user-new-12345",
  "requiresEmailVerification": true
}
```

---

## 📚 CATÁLOGO

### GET /api/catalog/makes

**Response:**

```json
{
  "items": [
    {
      "id": "make-001",
      "name": "Toyota",
      "slug": "toyota",
      "logoUrl": "https://cdn.okla.com.do/makes/toyota.svg",
      "vehicleCount": 450,
      "isPopular": true
    },
    {
      "id": "make-002",
      "name": "Honda",
      "slug": "honda",
      "logoUrl": "https://cdn.okla.com.do/makes/honda.svg",
      "vehicleCount": 380,
      "isPopular": true
    },
    {
      "id": "make-003",
      "name": "Hyundai",
      "slug": "hyundai",
      "logoUrl": "https://cdn.okla.com.do/makes/hyundai.svg",
      "vehicleCount": 290,
      "isPopular": true
    },
    {
      "id": "make-004",
      "name": "Kia",
      "slug": "kia",
      "logoUrl": "https://cdn.okla.com.do/makes/kia.svg",
      "vehicleCount": 245,
      "isPopular": true
    },
    {
      "id": "make-005",
      "name": "Nissan",
      "slug": "nissan",
      "logoUrl": "https://cdn.okla.com.do/makes/nissan.svg",
      "vehicleCount": 210,
      "isPopular": true
    },
    {
      "id": "make-006",
      "name": "Ford",
      "slug": "ford",
      "logoUrl": "https://cdn.okla.com.do/makes/ford.svg",
      "vehicleCount": 180,
      "isPopular": true
    },
    {
      "id": "make-007",
      "name": "Chevrolet",
      "slug": "chevrolet",
      "logoUrl": "https://cdn.okla.com.do/makes/chevrolet.svg",
      "vehicleCount": 165,
      "isPopular": false
    },
    {
      "id": "make-008",
      "name": "Mazda",
      "slug": "mazda",
      "logoUrl": "https://cdn.okla.com.do/makes/mazda.svg",
      "vehicleCount": 120,
      "isPopular": false
    },
    {
      "id": "make-009",
      "name": "BMW",
      "slug": "bmw",
      "logoUrl": "https://cdn.okla.com.do/makes/bmw.svg",
      "vehicleCount": 95,
      "isPopular": false
    },
    {
      "id": "make-010",
      "name": "Mercedes-Benz",
      "slug": "mercedes-benz",
      "logoUrl": "https://cdn.okla.com.do/makes/mercedes.svg",
      "vehicleCount": 88,
      "isPopular": false
    }
  ],
  "totalItems": 45
}
```

### GET /api/catalog/makes/{makeSlug}/models

**Response:**

```json
{
  "items": [
    {
      "id": "model-001",
      "makeId": "make-001",
      "name": "Camry",
      "slug": "camry",
      "vehicleCount": 85,
      "years": [2020, 2021, 2022, 2023, 2024]
    },
    {
      "id": "model-002",
      "makeId": "make-001",
      "name": "Corolla",
      "slug": "corolla",
      "vehicleCount": 120,
      "years": [2018, 2019, 2020, 2021, 2022, 2023, 2024]
    },
    {
      "id": "model-003",
      "makeId": "make-001",
      "name": "RAV4",
      "slug": "rav4",
      "vehicleCount": 95,
      "years": [2019, 2020, 2021, 2022, 2023, 2024]
    },
    {
      "id": "model-004",
      "makeId": "make-001",
      "name": "Highlander",
      "slug": "highlander",
      "vehicleCount": 45,
      "years": [2020, 2021, 2022, 2023, 2024]
    },
    {
      "id": "model-005",
      "makeId": "make-001",
      "name": "4Runner",
      "slug": "4runner",
      "vehicleCount": 38,
      "years": [2018, 2019, 2020, 2021, 2022, 2023]
    },
    {
      "id": "model-006",
      "makeId": "make-001",
      "name": "Tacoma",
      "slug": "tacoma",
      "vehicleCount": 52,
      "years": [2019, 2020, 2021, 2022, 2023, 2024]
    }
  ],
  "totalItems": 18
}
```

---

## 💬 MENSAJES

### GET /api/messages/conversations

**Response:**

```json
{
  "items": [
    {
      "id": "conv-001",
      "participants": [
        {
          "userId": "user-550e8400",
          "name": "Juan Pérez",
          "avatar": "https://cdn.okla.com.do/users/user-550e8400/avatar.webp",
          "role": "buyer",
          "isOnline": true
        },
        {
          "userId": "dealer-001",
          "name": "AutoMax RD",
          "avatar": "https://cdn.okla.com.do/dealers/automax/logo.webp",
          "role": "dealer",
          "isOnline": false,
          "lastSeenAt": "2026-01-31T08:30:00Z"
        }
      ],
      "vehicleId": "550e8400-e29b-41d4-a716-446655440001",
      "vehicle": {
        "id": "550e8400-e29b-41d4-a716-446655440001",
        "slug": "toyota-camry-se-2024",
        "title": "Toyota Camry SE 2024",
        "price": 1850000,
        "primaryImage": "https://cdn.okla.com.do/vehicles/550e8400/main.webp"
      },
      "lastMessage": {
        "id": "msg-123",
        "content": "Hola, ¿el precio es negociable?",
        "senderId": "user-550e8400",
        "createdAt": "2026-01-31T09:15:00Z",
        "isRead": false
      },
      "unreadCount": 2,
      "status": "active",
      "createdAt": "2026-01-30T14:00:00Z",
      "updatedAt": "2026-01-31T09:15:00Z"
    }
  ],
  "totalItems": 5,
  "totalUnread": 3
}
```

---

## 🔔 NOTIFICACIONES

### GET /api/notifications

**Response:**

```json
{
  "items": [
    {
      "id": "notif-001",
      "userId": "user-550e8400",
      "type": "price_alert",
      "title": "¡Bajó de precio!",
      "message": "Toyota Camry SE 2024 bajó de RD$1,950,000 a RD$1,850,000",
      "imageUrl": "https://cdn.okla.com.do/vehicles/550e8400/main.webp",
      "actionUrl": "/vehiculos/toyota-camry-se-2024",
      "actionLabel": "Ver vehículo",
      "isRead": false,
      "metadata": {
        "vehicleId": "550e8400",
        "oldPrice": 1950000,
        "newPrice": 1850000,
        "priceDrop": 100000
      },
      "createdAt": "2026-01-31T08:00:00Z"
    },
    {
      "id": "notif-002",
      "userId": "user-550e8400",
      "type": "message",
      "title": "Nuevo mensaje",
      "message": "AutoMax RD te envió un mensaje sobre Toyota Camry",
      "imageUrl": "https://cdn.okla.com.do/dealers/automax/logo.webp",
      "actionUrl": "/mensajes/conv-001",
      "actionLabel": "Ver mensaje",
      "isRead": true,
      "createdAt": "2026-01-30T16:45:00Z"
    }
  ],
  "totalItems": 12,
  "unreadCount": 3
}
```

---

## 💳 PAGOS

### POST /api/billing/checkout

**Request:**

```json
{
  "productType": "boost",
  "productId": "550e8400",
  "boostDuration": "14_days",
  "paymentMethod": "card",
  "provider": "azul"
}
```

**Response:**

```json
{
  "success": true,
  "checkoutUrl": "https://pagos.azul.com.do/checkout/abc123",
  "paymentId": "pay-001",
  "amount": 900,
  "currency": "DOP",
  "expiresAt": "2026-01-31T10:30:00Z"
}
```

### GET /api/billing/invoices

**Response:**

```json
{
  "items": [
    {
      "id": "inv-001",
      "number": "OKLA-2026-0001",
      "userId": "user-550e8400",
      "amount": 900,
      "tax": 162,
      "total": 1062,
      "currency": "DOP",
      "status": "paid",
      "dueDate": "2026-01-31",
      "paidAt": "2026-01-31T09:30:00Z",
      "pdfUrl": "https://cdn.okla.com.do/invoices/inv-001.pdf",
      "items": [
        {
          "description": "Boost 14 días - Toyota Camry SE 2024",
          "quantity": 1,
          "unitPrice": 900,
          "amount": 900
        }
      ],
      "createdAt": "2026-01-31T09:30:00Z"
    }
  ],
  "totalItems": 5
}
```

---

## ⭐ REVIEWS

### GET /api/reviews?targetId={dealerId}&targetType=dealer

**Response:**

```json
{
  "items": [
    {
      "id": "review-001",
      "reviewerId": "user-123",
      "reviewer": {
        "id": "user-123",
        "fullName": "María González",
        "avatar": "https://cdn.okla.com.do/users/user-123/avatar.webp",
        "memberSince": "2024-05-20"
      },
      "targetId": "dealer-001",
      "targetType": "dealer",
      "rating": 5,
      "title": "Excelente experiencia de compra",
      "comment": "Compré mi Toyota Camry aquí y el proceso fue muy profesional. El equipo de ventas fue muy atento y transparente con toda la información del vehículo. El precio fue justo y la entrega rápida.",
      "pros": [
        "Buen precio",
        "Atención profesional",
        "Vehículo en excelente estado"
      ],
      "cons": [],
      "vehicleId": "550e8400",
      "vehicle": {
        "id": "550e8400",
        "title": "Toyota Camry SE 2024",
        "primaryImage": "https://cdn.okla.com.do/vehicles/550e8400/main.webp"
      },
      "isVerifiedPurchase": true,
      "helpfulCount": 12,
      "response": {
        "content": "¡Gracias María! Fue un placer atenderla. Estamos aquí para cualquier cosa que necesite.",
        "createdAt": "2026-01-20T10:00:00Z"
      },
      "status": "published",
      "createdAt": "2026-01-18T14:30:00Z",
      "updatedAt": "2026-01-20T10:00:00Z"
    }
  ],
  "totalItems": 156,
  "summary": {
    "averageRating": 4.8,
    "totalReviews": 156,
    "distribution": {
      "5": 120,
      "4": 28,
      "3": 5,
      "2": 2,
      "1": 1
    }
  }
}
```

---

## 🔔 ALERTAS

### GET /api/alerts/price-alerts

**Response:**

```json
{
  "items": [
    {
      "id": "alert-001",
      "userId": "user-550e8400",
      "vehicleId": "550e8400",
      "vehicle": {
        "id": "550e8400",
        "slug": "toyota-camry-se-2024",
        "title": "Toyota Camry SE 2024",
        "price": 1850000,
        "primaryImage": "https://cdn.okla.com.do/vehicles/550e8400/main.webp"
      },
      "targetPrice": 1700000,
      "currentPrice": 1850000,
      "isActive": true,
      "isTriggered": false,
      "createdAt": "2026-01-25T10:00:00Z"
    }
  ],
  "totalItems": 3
}
```

### GET /api/alerts/saved-searches

**Response:**

```json
{
  "items": [
    {
      "id": "search-001",
      "userId": "user-550e8400",
      "name": "SUVs Toyota 2022+",
      "filters": {
        "makeSlugs": ["toyota"],
        "bodyType": ["suv"],
        "yearFrom": 2022,
        "priceFrom": 1000000,
        "priceTo": 2500000
      },
      "notifyOnNewResults": true,
      "notifyOnPriceDrops": true,
      "frequency": "daily",
      "lastNotifiedAt": "2026-01-30T08:00:00Z",
      "newResultsCount": 3,
      "createdAt": "2026-01-15T14:30:00Z",
      "updatedAt": "2026-01-30T08:00:00Z"
    }
  ],
  "totalItems": 2
}
```

---

## ❌ RESPUESTAS DE ERROR

### Error de Validación (400)

```json
{
  "success": false,
  "message": "Error de validación",
  "statusCode": 400,
  "errors": {
    "email": ["El email no es válido"],
    "password": ["La contraseña debe tener al menos 8 caracteres"]
  },
  "errorCode": "VALIDATION_ERROR",
  "traceId": "trace-abc123"
}
```

### No Autorizado (401)

```json
{
  "success": false,
  "message": "Token expirado o inválido",
  "statusCode": 401,
  "errorCode": "TOKEN_EXPIRED"
}
```

### Prohibido (403)

```json
{
  "success": false,
  "message": "No tienes permiso para realizar esta acción",
  "statusCode": 403,
  "errorCode": "FORBIDDEN"
}
```

### No Encontrado (404)

```json
{
  "success": false,
  "message": "Vehículo no encontrado",
  "statusCode": 404,
  "errorCode": "VEHICLE_NOT_FOUND"
}
```

### Rate Limit (429)

```json
{
  "success": false,
  "message": "Demasiadas solicitudes. Intenta de nuevo en 60 segundos.",
  "statusCode": 429,
  "errorCode": "RATE_LIMIT_EXCEEDED",
  "retryAfter": 60
}
```

### Error de Servidor (500)

```json
{
  "success": false,
  "message": "Error interno del servidor",
  "statusCode": 500,
  "errorCode": "INTERNAL_ERROR",
  "traceId": "trace-xyz789"
}
```

---

## 🔧 USO EN TESTS (MSW)

```typescript
// filepath: __tests__/mocks/handlers.ts
import { http, HttpResponse } from "msw";
import { mockVehicles, mockDealer, mockUser } from "./data";

export const handlers = [
  // Vehicles
  http.get("*/api/vehicles", () => {
    return HttpResponse.json({
      items: mockVehicles,
      totalItems: mockVehicles.length,
      totalPages: 1,
      currentPage: 1,
      pageSize: 20,
      hasNextPage: false,
      hasPreviousPage: false,
    });
  }),

  http.get("*/api/vehicles/:slug", ({ params }) => {
    const vehicle = mockVehicles.find((v) => v.slug === params.slug);
    if (!vehicle) {
      return HttpResponse.json(
        { success: false, message: "Not found", statusCode: 404 },
        { status: 404 },
      );
    }
    return HttpResponse.json(vehicle);
  }),

  // Auth
  http.post("*/api/auth/login", async ({ request }) => {
    const body = await request.json();
    if (body.email === "test@test.com" && body.password === "password") {
      return HttpResponse.json({
        success: true,
        user: mockUser,
        accessToken: "mock-token",
        refreshToken: "mock-refresh",
        expiresAt: Date.now() + 900000,
      });
    }
    return HttpResponse.json(
      { success: false, message: "Invalid credentials", statusCode: 401 },
      { status: 401 },
    );
  }),
];
```

---

## 📚 REFERENCIAS

- [MSW Documentation](https://mswjs.io/)
- [JSON Placeholder](https://jsonplaceholder.typicode.com/) (inspiración)
- Backend API: Ver `05-API-INTEGRATION/` para endpoints reales
