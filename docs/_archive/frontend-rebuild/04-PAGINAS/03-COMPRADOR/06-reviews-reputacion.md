---
title: "20. Reviews y Sistema de Reputación"
priority: P1
estimated_time: "2 horas"
dependencies: []
apis:
  [
    "VehiclesSaleService",
    "UserService",
    "DealerManagementService",
    "BillingService",
    "NotificationService",
    "MediaService",
  ]
status: complete
last_updated: "2026-01-30"
---

# 20. Reviews y Sistema de Reputación

> 👤 **SCOPE:** Interfaz USER-FACING para crear, ver y responder reviews  
> ❌ **NO incluye:** Herramientas de moderación admin (ver doc 37)  
> 🎯 **Rutas:** `/dealer/{id}/reviews`, `/dealer/{id}/reviews/new`, `/reviews/{id}`

**Objetivo:** Implementar sistema completo de calificaciones y reviews para dealers y vehículos, con estadísticas de reputación y verificación de compra.

**Prioridad:** P2 (Baja - Mejora de confianza y credibilidad)  
**Complejidad:** 🔴 Alta (Reviews, moderación, agregaciones)  
**Dependencias:** ReviewService (backend), MediaService (imágenes), UserService  
**Puerto Backend:** 5091  
**Estado:** ✅ Backend 90% | ✅ UI 80%  
**Última actualización:** Enero 29, 2026  
**Moderación Admin:** Ver [37-admin-review-moderation-completo.md](37-admin-review-moderation-completo.md)

---

## 🔗 INTEGRACIÓN CON REVIEWSERVICE

> **Referencias:**
>
> - [process-matrix/21-REVIEWS-REPUTACION/01-dealer-reviews.md](../../process-matrix/21-REVIEWS-REPUTACION/01-dealer-reviews.md)

### Servicio Involucrado

| Servicio                    | Puerto | Estado            | Descripción                             |
| --------------------------- | ------ | ----------------- | --------------------------------------- |
| **ReviewService**           | 5091   | ✅ 90% BE, 80% UI | Sistema de reviews y ratings de dealers |
| **NotificationService**     | 5006   | ✅ 100%           | Solicitudes de review, respuestas       |
| **UserService**             | 5004   | ✅ 100%           | Verificación de usuarios                |
| **DealerManagementService** | 5039   | ✅ 100%           | Información de dealers                  |

---

### ReviewService - Endpoints

| Método   | Endpoint                                 | Descripción                   | Auth      |
| -------- | ---------------------------------------- | ----------------------------- | --------- |
| `GET`    | `/api/reviews/dealer/{dealerId}`         | Reviews de un dealer          | ❌        |
| `GET`    | `/api/reviews/dealer/{dealerId}/summary` | Resumen de ratings            | ❌        |
| `POST`   | `/api/reviews/dealer/{dealerId}`         | Crear review                  | ✅        |
| `PUT`    | `/api/reviews/{id}`                      | Editar mi review              | ✅        |
| `DELETE` | `/api/reviews/{id}`                      | Eliminar mi review            | ✅        |
| `POST`   | `/api/reviews/{id}/helpful`              | Marcar como útil              | ✅        |
| `POST`   | `/api/reviews/{id}/report`               | Reportar review               | ✅        |
| `POST`   | `/api/reviews/{id}/response`             | Dealer responde               | ✅ Dealer |
| `GET`    | `/api/reviews/pending`                   | Reviews pendientes moderación | ✅ Admin  |
| `POST`   | `/api/reviews/{id}/moderate`             | Moderar review                | ✅ Admin  |

---

### ReviewService - Procesos

| Proceso        | Nombre                   | Pasos | Estado  |
| -------------- | ------------------------ | ----- | ------- |
| **REVIEW-001** | Comprador Deja Review    | 12    | ✅ 90%  |
| **REVIEW-002** | Dealer Responde a Review | 5     | ✅ 80%  |
| **REVIEW-003** | Moderación de Reviews    | 7     | ✅ 100% |

---

### ReviewService - Entidades

#### DealerReview

```csharp
public class DealerReview
{
    public Guid Id { get; set; }

    // Relaciones
    public Guid DealerId { get; set; }
    public Guid ReviewerId { get; set; }
    public Guid? VehicleId { get; set; }          // Vehículo comprado (si aplica)
    public Guid? TransactionId { get; set; }      // Transacción verificada

    // Ratings (1-5 estrellas cada uno)
    public int OverallRating { get; set; }        // Rating general
    public int CustomerServiceRating { get; set; } // Servicio al cliente
    public int TransparencyRating { get; set; }   // Transparencia
    public int ValueForMoneyRating { get; set; }  // Valor por dinero
    public int FacilitiesRating { get; set; }     // Instalaciones

    // Contenido
    public string Title { get; set; }
    public string Content { get; set; }
    public List<string> Pros { get; set; }        // Puntos positivos
    public List<string> Cons { get; set; }        // Puntos a mejorar

    // Tipo de experiencia
    public ReviewExperienceType ExperienceType { get; set; } // Purchased, TestDrove, Inquired, ServiceVisit, TradeIn
    public bool PurchaseVerified { get; set; }    // ✓ Compra Verificada

    // Respuesta del dealer
    public DealerResponse DealerResponse { get; set; }

    // Engagement
    public int HelpfulCount { get; set; }
    public int NotHelpfulCount { get; set; }
    public List<Guid> HelpfulByUsers { get; set; }

    // Moderación
    public ReviewStatus Status { get; set; }      // Pending, Approved, Rejected, Hidden, Flagged
    public string ModerationReason { get; set; }
    public Guid? ModeratedBy { get; set; }
    public DateTime? ModeratedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public enum ReviewExperienceType
{
    Purchased,           // Compró vehículo
    TestDrove,           // Solo test drive
    Inquired,            // Solo consultó
    ServiceVisit,        // Servicio/mantenimiento
    TradeIn              // Trade-in
}

public enum ReviewStatus
{
    Pending,             // Esperando moderación
    Approved,            // Publicado
    Rejected,            // Rechazado
    Hidden,              // Oculto por el usuario
    Flagged              // Reportado, bajo revisión
}
```

#### DealerRatingSummary

```csharp
public class DealerRatingSummary
{
    public Guid Id { get; set; }
    public Guid DealerId { get; set; }

    // Promedios (1-5 estrellas)
    public decimal OverallAverage { get; set; }
    public decimal CustomerServiceAverage { get; set; }
    public decimal TransparencyAverage { get; set; }
    public decimal ValueForMoneyAverage { get; set; }
    public decimal FacilitiesAverage { get; set; }

    // Conteos
    public int TotalReviews { get; set; }
    public int VerifiedReviews { get; set; }       // Con "Compra Verificada"
    public int ReviewsWithResponse { get; set; }   // Dealer respondió

    // Distribución de estrellas
    public int FiveStarCount { get; set; }
    public int FourStarCount { get; set; }
    public int ThreeStarCount { get; set; }
    public int TwoStarCount { get; set; }
    public int OneStarCount { get; set; }

    // Tendencias
    public decimal RatingChange30Days { get; set; }
    public int NewReviews30Days { get; set; }

    // Ranking
    public int RankInCity { get; set; }
    public int RankOverall { get; set; }

    public DateTime LastUpdated { get; set; }
}
```

#### DealerResponse

```csharp
public class DealerResponse
{
    public Guid Id { get; set; }
    public Guid ReviewId { get; set; }
    public Guid ResponderId { get; set; }          // Quién del dealer respondió
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

---

### Flujo de Usuario: Escribir Review

```
1. Usuario completa compra de vehículo
   ↓
2. Sistema registra TransactionId (VehiclesSaleService)
   ↓
3. 7 días después → Email automático: "¿Cómo fue tu experiencia?"
   ↓
4. Usuario hace click en email → Redirige a /dealer/:id/review
   ↓
5. Completa formulario de review:
   - Tipo de experiencia (Purchased, TestDrove, etc.)
   - Ratings (Overall, CustomerService, Transparency, ValueForMoney, Facilities)
   - Título (ej: "Excelente experiencia de compra")
   - Contenido (mínimo 50 caracteres)
   - Pros (opcional, lista)
   - Cons (opcional, lista)
   ↓
6. POST /api/reviews/dealer/{dealerId}
   ↓
7. ReviewService valida:
   - Spam filter (score < 0.05)
   - Profanity filter (palabras prohibidas)
   - Duplicate check (no múltiples reviews del mismo usuario)
   ↓
8. Review creado con Status = Pending o Approved (auto)
   ↓
9. Si auto-aprobado (score > 0.95):
   - Status = Approved
   - Actualiza DealerRatingSummary
   - Review visible inmediatamente
   ↓
10. Si requiere moderación (score 0.5-0.95):
    - Status = Pending
    - Admin debe revisar en /admin/reviews/moderation
    ↓
11. Notificación al dealer:
    - Email: "Nueva review de Juan P."
    - In-app notification en dashboard
    ↓
12. Dealer puede responder en /dealer/dashboard/reviews
```

---

### Flujo de Usuario: Dealer Responde

```
1. Dealer recibe notificación de nueva review
   ↓
2. Accede a /dealer/dashboard/reviews
   ↓
3. Ve lista de reviews (Todas, Con respuesta, Sin respuesta)
   ↓
4. Click "Responder" en review sin respuesta
   ↓
5. Escribe respuesta en modal:
   - Contenido (mínimo 20 caracteres)
   - Tono profesional (validación de spam)
   ↓
6. POST /api/reviews/{id}/response
   ↓
7. ReviewService guarda DealerResponse
   ↓
8. Notificación al reviewer:
   - Email: "AutoMax RD respondió a tu review"
   - Link a ver respuesta
   ↓
9. Respuesta visible públicamente en perfil dealer
   ↓
10. Incrementa ReviewsWithResponse en DealerRatingSummary
```

---

### Flujo de Moderación (Admin)

```
1. Review creada con Status = Pending
   ↓
2. Admin accede a /admin/reviews/moderation
   ↓
3. Ve cola de reviews pendientes (ordenadas por fecha)
   ↓
4. Click en review para ver detalles:
   - Contenido completo
   - Ratings
   - Pros/Cons
   - Score de spam (0-1)
   - Usuario reviewer (historial)
   - Dealer afectado
   ↓
5. Verifica políticas de contenido:
   - ¿Es spam?
   - ¿Contiene lenguaje ofensivo?
   - ¿Es legítima?
   - ¿Es constructiva?
   ↓
6. Decisión:
   - APROBAR: Status = Approved, actualiza summary
   - RECHAZAR: Status = Rejected, notifica a usuario
   ↓
7. POST /api/reviews/{id}/moderate
   ↓
8. Si aprobado:
   - Review visible públicamente
   - Actualiza DealerRatingSummary
   - Email a reviewer: "Tu review ha sido publicada"
   ↓
9. Si rechazado:
   - Review NO visible
   - Email a reviewer con razón
   - Opción de editar y reenviar
```

---

### Sistema de Ratings - Agregaciones

**Promedios Calculados:**

1. **OverallAverage:** Promedio de todos los OverallRating
2. **CustomerServiceAverage:** Promedio de CustomerServiceRating
3. **TransparencyAverage:** Promedio de TransparencyRating
4. **ValueForMoneyAverage:** Promedio de ValueForMoneyRating
5. **FacilitiesAverage:** Promedio de FacilitiesRating

**Fórmula de Promedio Ponderado:**

```
Rating Final = (Sum de todos los ratings aprobados) / Total de reviews aprobadas

Ejemplo:
Dealer tiene 10 reviews:
- 5 estrellas: 6 reviews
- 4 estrellas: 3 reviews
- 3 estrellas: 1 review

Overall Average = ((5 * 6) + (4 * 3) + (3 * 1)) / 10
                = (30 + 12 + 3) / 10
                = 4.5 ⭐
```

**Distribución de Estrellas:**

```
★★★★★ (5) ████████████████████████  60% (6 reviews)
★★★★☆ (4) ████████████              30% (3 reviews)
★★★☆☆ (3) ████                      10% (1 review)
★★☆☆☆ (2)                            0% (0 reviews)
★☆☆☆☆ (1)                            0% (0 reviews)
```

**Tendencias (30 días):**

- **RatingChange30Days:** Diferencia entre promedio actual vs promedio hace 30 días
- **NewReviews30Days:** Cantidad de reviews nuevas en los últimos 30 días

**Ejemplo:**

- Rating actual: 4.5 ⭐
- Rating hace 30 días: 4.2 ⭐
- Change: +0.3 (mejora) ✅
- New reviews: 5

---

### Validación de Reviews

#### Spam Filter

```csharp
public class SpamDetector
{
    public double CalculateSpamScore(string content)
    {
        double score = 0.0;

        // Factores que aumentan spam score:
        // - Muchas URLs (0.3 por cada URL)
        // - Palabras en mayúsculas (0.1 por cada 10%)
        // - Caracteres repetidos excesivos (0.2)
        // - Contenido muy corto (< 20 chars: 0.4)
        // - Palabras prohibidas (0.5 por palabra)

        return Math.Min(1.0, score);
    }
}
```

**Decisiones basadas en Score:**

- **Score < 0.05:** Auto-aprobado (review legítima)
- **Score 0.05-0.5:** Requiere moderación manual
- **Score > 0.5:** Auto-rechazado (spam claro)

#### Profanity Filter

Lista de palabras prohibidas que automáticamente rechazan la review o la envían a moderación.

#### Duplicate Check

Un usuario solo puede dejar 1 review por dealer (puede editarla después).

---

### Notificaciones Automáticas

#### Solicitud de Review (Post-Compra)

**Trigger:** Transacción completada  
**Delay:** 7 días después  
**Template:** `review-request`

```
Asunto: ¿Cómo fue tu experiencia con AutoMax RD?

Hola Juan,

Esperamos que estés disfrutando tu nuevo Toyota Corolla 2023.

Nos encantaría saber sobre tu experiencia con AutoMax RD.
Tu opinión ayuda a otros compradores a tomar mejores decisiones.

[Escribir Review]

¿Por qué tu opinión importa?
✓ Ayudas a otros compradores
✓ Reconoces el buen servicio
✓ Contribuyes a mejorar la industria

Gracias,
Equipo OKLA
```

#### Nueva Review (al Dealer)

**Trigger:** Review creada y aprobada  
**Template:** `new-review-dealer`

```
Asunto: Nueva review de Juan P.

Hola AutoMax RD,

Tienes una nueva review de Juan P.:

⭐⭐⭐⭐⭐ 5 estrellas
"Excelente experiencia de compra"

[Ver Review Completa]

¿Quieres responder? Una respuesta profesional aumenta tu credibilidad.

[Responder Ahora]
```

#### Dealer Respondió (al Reviewer)

**Trigger:** Dealer crea DealerResponse  
**Template:** `dealer-response`

```
Asunto: AutoMax RD respondió a tu review

Hola Juan,

AutoMax RD respondió a tu review:

"¡Gracias por tu review, Juan! Nos alegra que hayas
tenido una excelente experiencia..."

[Ver Respuesta Completa]
```

#### Review Aprobada (al Reviewer)

**Trigger:** Admin aprueba review  
**Template:** `review-approved`

```
Asunto: Tu review ha sido publicada

Hola Juan,

Tu review de AutoMax RD ha sido aprobada y ahora
es visible para todos los usuarios de OKLA.

[Ver Tu Review]

Gracias por contribuir a nuestra comunidad.
```

#### Review Rechazada (al Reviewer)

**Trigger:** Admin rechaza review  
**Template:** `review-rejected`

```
Asunto: Tu review requiere modificación

Hola Juan,

Tu review de AutoMax RD no pudo ser publicada porque:

"Contiene lenguaje ofensivo"

Puedes editar tu review y enviarla nuevamente.

[Editar Review]

Recuerda nuestras políticas:
✓ Lenguaje respetuoso
✓ Comentarios constructivos
✓ Sin spam ni autopromoción
```

---

### Métricas y KPIs

#### Para Dealers

| Métrica              | Objetivo    | Descripción                           |
| -------------------- | ----------- | ------------------------------------- |
| **Overall Rating**   | > 4.0 ⭐    | Promedio general                      |
| **Total Reviews**    | > 20        | Credibilidad                          |
| **Response Rate**    | > 80%       | % de reviews con respuesta del dealer |
| **Verified Reviews** | > 60%       | % con "Compra Verificada"             |
| **Recent Activity**  | > 5 new/30d | Reviews recientes                     |

#### Para Plataforma

| Métrica                | Objetivo           |
| ---------------------- | ------------------ |
| Review Submission Rate | 30% de compradores |
| Average Review Length  | > 100 caracteres   |
| Moderation Time        | < 24 horas         |
| False Positive Rate    | < 5%               |
| Dealer Response Rate   | > 70%              |

---

### Badges de Reputación

Basados en ratings y actividad:

| Badge                 | Criterios                    | Icon |
| --------------------- | ---------------------------- | ---- |
| **Top Rated**         | Rating ≥ 4.5 + 20+ reviews   | ⭐   |
| **Trusted Dealer**    | 50+ verified purchases       | ✓    |
| **Responsive**        | Response rate ≥ 90%          | 💬   |
| **Rising Star**       | Rating increase > 0.5 in 30d | 📈   |
| **Excellent Service** | Customer Service ≥ 4.8       | 🏆   |

Estos badges son visibles en:

- Cards de dealers en búsqueda
- Perfil del dealer
- Listings de vehículos del dealer

---

## ✅ AUDITORÍA DE ACCESO UI (Enero 27, 2026)

> **Estado:** ✅ Servicio funcional con UI completa.

| Proceso          | Backend | UI Access | Observación               |
| ---------------- | ------- | --------- | ------------------------- |
| Ver reviews      | ✅ 100% | ✅ 100%   | En perfil dealer/vendedor |
| Escribir review  | ✅ 100% | ✅ 100%   | Post-transacción          |
| Votar review     | ✅ 100% | ✅ 100%   | Botón útil/no útil        |
| Responder review | ✅ 100% | ✅ 100%   | Para dealers              |
| Badges           | ✅ 100% | ✅ 100%   | Visible en perfil         |
| Moderar reviews  | ✅ 100% | ✅ 100%   | En `/admin/reviews`       |

### Rutas UI Existentes ✅

- ✅ Sección en `/dealer/:slug` - Reviews del dealer
- ✅ Sección en `/vehicles/:slug` - Reviews del vendedor
- ✅ Modal post-transacción - Escribir review
- ✅ `/admin/reviews` - Moderar reviews

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura](#arquitectura)
2. [Backend API](#backend-api)
3. [Procesos Detallados](#procesos-detallados)
4. [Sistema de Badges](#sistema-de-badges)
5. [Solicitudes Automáticas](#solicitudes-automáticas)
6. [Componentes](#componentes)
7. [Páginas](#páginas)
8. [Hooks y Servicios](#hooks-y-servicios)
9. [Tipos TypeScript](#tipos-typescript)
10. [Eventos y Mensajería](#eventos-y-mensajería)
11. [Reglas de Negocio](#reglas-de-negocio)
12. [Validación](#validación)

---

## 🏗️ ARQUITECTURA

### Arquitectura Completa del Sistema

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        ReviewService Architecture                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   User Actions                       Core Service                            │
│   ┌────────────────┐                ┌────────────────────────────────┐      │
│   │ Buyers         │──┐             │          ReviewService           │      │
│   │ (Write Reviews)│  │             │  ┌──────────────────────────┐   │      │
│   └────────────────┘  │             │  │ Controllers              │   │      │
│   ┌────────────────┐  │             │  │ • ReviewsController      │   │      │
│   │ Visitors       │──┼────────────▶│  │ • BadgesController       │   │      │
│   │ (Read/Vote)    │  │             │  │ • ModerationController   │   │      │
│   └────────────────┘  │             │  └──────────────────────────┘   │      │
│   ┌────────────────┐  │             │  ┌──────────────────────────┐   │      │
│   │ Sellers        │──┤             │  │ Application (CQRS)       │   │      │
│   │ (Respond)      │  │             │  │ • CreateReviewCommand    │   │      │
│   └────────────────┘  │             │  │ • VoteReviewCommand      │   │      │
│   ┌────────────────┐  │             │  │ • CalculateBadgesJob     │   │      │
│   │ Admins         │──┘             │  │ • GetSellerReviewsQuery  │   │      │
│   │ (Moderate)     │               │  └──────────────────────────┘   │      │
│   └────────────────┘               │  ┌──────────────────────────┐   │      │
│                                    │  │ Domain                   │   │      │
│   Triggers                         │  │ • Review, Vote           │   │      │
│   ┌────────────────┐               │  │ • Badge, SellerStats     │   │      │
│   │ BillingService │──────────────▶│  │ • ReviewRequest          │   │      │
│   │ (Sale Complete)│               │  └──────────────────────────┘   │      │
│   └────────────────┘               └────────────────────────────────┘      │
│                                                    │                        │
│                                    ┌───────────────┼───────────────┐        │
│                                    ▼               ▼               ▼        │
│                            ┌────────────┐  ┌────────────┐  ┌────────────┐  │
│                            │ PostgreSQL │  │   Redis    │  │  RabbitMQ  │  │
│                            │ (Reviews,  │  │ (Rating    │  │ (Review    │  │
│                            │  Badges)   │  │  Cache)    │  │  Events)   │  │
│                            └────────────┘  └────────────┘  └────────────┘  │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Flujo de Reviews

```
Usuario → Compra verificada → Puede dejar review
   ↓
Review creado → Estado: Pending
   ↓
Moderación automática (IA) → Score
   ↓
Si Score > 0.8 → Aprobado automáticamente
Si Score < 0.5 → Rechazado (spam/abuso)
Si 0.5-0.8 → Pendiente moderación manual
   ↓
Admin revisa → Aprueba/Rechaza
   ↓
Review publicado → Visible en dealer/vehículo
   ↓
Agregaciones actualizadas → Rating promedio
   ↓
Badges recalculados → TopRated, TrustedDealer, etc.
```

### Tipos de Reviews

1. **Dealer Reviews** - Calificación del dealer (atención, honestidad, proceso)
2. **Vehicle Reviews** - Calificación del vehículo vendido (calidad, descripción)

### Entidades del Dominio

#### Review

```csharp
public class Review : BaseEntity<Guid>
{
    // ========================================
    // INFORMACIÓN PRINCIPAL
    // ========================================
    public Guid BuyerId { get; set; }           // Quien deja la review
    public Guid SellerId { get; set; }          // Quien recibe la review
    public Guid? VehicleId { get; set; }        // Vehículo relacionado
    public Guid? OrderId { get; set; }          // Transacción que valida la compra

    // ========================================
    // CONTENIDO DE LA REVIEW
    // ========================================
    public int Rating { get; set; }             // 1-5 estrellas
    public string Title { get; set; }           // "Excelente servicio"
    public string Content { get; set; }         // Texto detallado

    // ========================================
    // MODERACIÓN
    // ========================================
    public bool IsApproved { get; set; }        // Aprobada por moderación
    public bool IsVerifiedPurchase { get; set; } // Badge "Compra Verificada"
    public string? RejectionReason { get; set; }
    public Guid? ModeratedById { get; set; }
    public DateTime? ModeratedAt { get; set; }

    // ========================================
    // INFORMACIÓN DEL COMPRADOR (Cache)
    // ========================================
    public string BuyerName { get; set; }
    public string? BuyerPhotoUrl { get; set; }

    // ========================================
    // VOTOS DE UTILIDAD
    // ========================================
    public int HelpfulVotes { get; set; }       // Votos positivos
    public int TotalVotes { get; set; }         // Total de votos

    // ========================================
    // DETECCIÓN DE FRAUDE
    // ========================================
    public int TrustScore { get; set; }         // 0-100 (100 = confiable)
    public bool IsFlagged { get; set; }
    public string? FlagReason { get; set; }
    public string? UserIpAddress { get; set; }
    public string? UserAgent { get; set; }

    // ========================================
    // SOLICITUD AUTOMÁTICA
    // ========================================
    public bool WasAutoRequested { get; set; }
    public DateTime? AutoRequestedAt { get; set; }

    // Navigation
    public ReviewResponse? Response { get; set; }
    public List<ReviewHelpfulVote> HelpfulVotesList { get; set; }
}
```

#### SellerBadge

```csharp
public class SellerBadge : BaseEntity<Guid>
{
    public Guid SellerId { get; set; }
    public BadgeType BadgeType { get; set; }
    public string Title { get; set; }           // "Top Rated"
    public string Description { get; set; }     // Descripción del badge
    public string Icon { get; set; }            // Emoji o icono
    public string Color { get; set; }           // "#3B82F6"
    public bool IsActive { get; set; }
    public DateTime EarnedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Criteria { get; set; }       // Criterios cumplidos
    public string? QualifyingStats { get; set; } // Stats JSON
    public int DisplayOrder { get; set; }
}
```

#### ReviewRequest

```csharp
public class ReviewRequest : BaseEntity<Guid>
{
    public Guid BuyerId { get; set; }
    public Guid SellerId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid OrderId { get; set; }
    public string BuyerEmail { get; set; }
    public string BuyerName { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime RequestSentAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public ReviewRequestStatus Status { get; set; }
    public string Token { get; set; }           // Link único
    public int RemindersSent { get; set; }
    public DateTime? LastReminderAt { get; set; }
    public Guid? ReviewId { get; set; }
    public DateTime? ReviewCreatedAt { get; set; }
}
```

---

## 🔌 BACKEND API

### ReviewService Endpoints

**Base URL:** `http://localhost:5030` (desarrollo) | `https://api.okla.com.do/api/reviews` (producción)

#### Reviews CRUD

| Método   | Endpoint                                 | Descripción                       | Auth | Roles   |
| -------- | ---------------------------------------- | --------------------------------- | ---- | ------- |
| `GET`    | `/api/reviews/seller/{sellerId}`         | Reviews de un vendedor (paginado) | ❌   | Público |
| `GET`    | `/api/reviews/seller/{sellerId}/summary` | Estadísticas de reviews           | ❌   | Público |
| `GET`    | `/api/reviews/{reviewId}`                | Obtener review por ID             | ❌   | Público |
| `POST`   | `/api/reviews`                           | Crear review                      | ✅   | User    |
| `PUT`    | `/api/reviews/{reviewId}`                | Actualizar review (solo autor)    | ✅   | User    |
| `DELETE` | `/api/reviews/{reviewId}`                | Eliminar review (solo autor)      | ✅   | User    |

#### Moderación

| Método | Endpoint                           | Descripción                   | Auth | Roles  |
| ------ | ---------------------------------- | ----------------------------- | ---- | ------ |
| `GET`  | `/api/reviews/pending`             | Reviews pendientes moderación | ✅   | Admin  |
| `POST` | `/api/reviews/{reviewId}/moderate` | Aprobar/Rechazar review       | ✅   | Admin  |
| `POST` | `/api/reviews/{reviewId}/respond`  | Vendedor responde a review    | ✅   | Seller |

#### Votos de Utilidad

| Método | Endpoint                             | Descripción             | Auth | Roles   |
| ------ | ------------------------------------ | ----------------------- | ---- | ------- |
| `POST` | `/api/reviews/{reviewId}/vote`       | Votar si review es útil | ✅   | User    |
| `GET`  | `/api/reviews/{reviewId}/vote-stats` | Estadísticas de votos   | ❌   | Público |

#### Badges de Vendedor

| Método | Endpoint                                            | Descripción                   | Auth | Roles   |
| ------ | --------------------------------------------------- | ----------------------------- | ---- | ------- |
| `GET`  | `/api/reviews/seller/{sellerId}/badges`             | Badges activos del vendedor   | ❌   | Público |
| `GET`  | `/api/reviews/seller/{sellerId}/badges/history`     | Historial completo de badges  | ✅   | Admin   |
| `POST` | `/api/reviews/seller/{sellerId}/badges/recalculate` | Recalcular badges manualmente | ✅   | Admin   |

#### Solicitudes Automáticas

| Método | Endpoint                                | Descripción                | Auth | Roles         |
| ------ | --------------------------------------- | -------------------------- | ---- | ------------- |
| `POST` | `/api/reviews/requests`                 | Enviar solicitud de review | ✅   | Admin, System |
| `GET`  | `/api/reviews/requests/buyer/{buyerId}` | Solicitudes pendientes     | ✅   | User, Admin   |
| `GET`  | `/api/reviews/requests/mine`            | Mis solicitudes pendientes | ✅   | User          |

---

## 🔄 PROCESOS DETALLADOS

### PROCESO 1: Crear Review de Vendedor

**Endpoint:** `POST /api/reviews`

#### Flujo Completo

| Paso | Actor      | Acción                     | Sistema                              | Resultado            |
| ---- | ---------- | -------------------------- | ------------------------------------ | -------------------- |
| 1    | Comprador  | Envía review               | HTTP POST                            | Request recibido     |
| 2    | Controller | Extrae BuyerId de JWT      | ClaimTypes.NameIdentifier            | Usuario identificado |
| 3    | Handler    | Valida rating 1-5          | FluentValidation                     | Rating válido        |
| 4    | Handler    | Verifica compra (OrderId)  | Si existe, IsVerifiedPurchase = true | Badge asignado       |
| 5    | Handler    | Verifica no duplicado      | No existe review buyer+seller        | Sin duplicados       |
| 6    | Handler    | Crea entidad Review        | new Review()                         | Review creada        |
| 7    | Handler    | Aplica detección de fraude | CheckFraud(IP, UserAgent)            | TrustScore calculado |
| 8    | Repository | Persiste review            | INSERT                               | Guardado             |
| 9    | Events     | Publica ReviewCreatedEvent | RabbitMQ                             | Evento publicado     |
| 10   | Events     | Trigger recálculo badges   | BadgeService                         | Badges actualizados  |
| 11   | API        | Retorna 201 Created        | CreatedAtAction                      | Review creada        |

#### Request Body

```json
{
  "sellerId": "seller-uuid",
  "vehicleId": "vehicle-uuid",
  "orderId": "order-uuid",
  "rating": 5,
  "title": "Excelente experiencia de compra",
  "content": "El vendedor fue muy profesional y transparente. El vehículo estaba en perfectas condiciones, mejor de lo que esperaba. Muy recomendado para cualquiera que busque un auto usado de calidad."
}
```

#### Response (201 Created)

```json
{
  "id": "review-uuid",
  "buyerId": "buyer-uuid",
  "sellerId": "seller-uuid",
  "vehicleId": "vehicle-uuid",
  "rating": 5,
  "title": "Excelente experiencia de compra",
  "content": "El vendedor fue muy profesional...",
  "isApproved": true,
  "isVerifiedPurchase": true,
  "buyerName": "Juan Pérez",
  "buyerPhotoUrl": "https://cdn.okla.com.do/users/juan.jpg",
  "helpfulVotes": 0,
  "totalVotes": 0,
  "createdAt": "2026-01-21T15:30:00Z"
}
```

---

### PROCESO 2: Obtener Reviews de Vendedor con Estadísticas

**Endpoint:** `GET /api/reviews/seller/{sellerId}/summary`

#### Flujo Completo

| Paso | Actor    | Acción                     | Sistema                         | Resultado         |
| ---- | -------- | -------------------------- | ------------------------------- | ----------------- |
| 1    | Frontend | Solicita resumen           | HTTP GET                        | Request recibido  |
| 2    | Handler  | Consulta reviews aprobadas | WHERE SellerId AND IsApproved   | Reviews filtradas |
| 3    | Handler  | Calcula promedio           | AVG(Rating)                     | AverageRating     |
| 4    | Handler  | Cuenta totales             | COUNT(\*)                       | TotalReviews      |
| 5    | Handler  | Cuenta verificadas         | COUNT(WHERE IsVerifiedPurchase) | VerifiedCount     |
| 6    | Handler  | Agrupa por rating          | GROUP BY Rating                 | Distribución      |
| 7    | Handler  | Obtiene badges activos     | BadgeRepository.GetActive()     | Badges            |
| 8    | API      | Retorna ReviewSummaryDto   | HTTP 200                        | Resumen completo  |

#### Response (200 OK)

```json
{
  "sellerId": "seller-uuid",
  "averageRating": 4.7,
  "totalReviews": 156,
  "verifiedPurchases": 142,
  "ratingDistribution": {
    "5": 98,
    "4": 45,
    "3": 8,
    "2": 3,
    "1": 2
  },
  "percentagePositive": 91.7,
  "recentTrend": "up",
  "badges": [
    {
      "type": "TopRated",
      "title": "Top Rated",
      "icon": "⭐",
      "color": "#F59E0B"
    },
    {
      "type": "TrustedDealer",
      "title": "Vendedor Confiable",
      "icon": "✓",
      "color": "#10B981"
    }
  ]
}
```

---

### PROCESO 3: Votar Review como Útil

**Endpoint:** `POST /api/reviews/{reviewId}/vote`

#### Flujo Completo

| Paso | Actor      | Acción                  | Sistema                  | Resultado               |
| ---- | ---------- | ----------------------- | ------------------------ | ----------------------- |
| 1    | Usuario    | Vota si es útil         | HTTP POST                | Request recibido        |
| 2    | Controller | Extrae UserId de JWT    | Claims                   | Usuario identificado    |
| 3    | Handler    | Verifica review existe  | GetById()                | Review encontrada       |
| 4    | Handler    | Verifica no es autor    | BuyerId != UserId        | No auto-voto            |
| 5    | Handler    | Busca voto previo       | GetVoteByUser()          | ¿Ya votó?               |
| 6    | Handler    | Si votó, actualiza voto | Update vote              | Voto cambiado           |
| 7    | Handler    | Si no votó, crea voto   | Create vote              | Voto nuevo              |
| 8    | Handler    | Recalcula contadores    | HelpfulVotes, TotalVotes | Contadores actualizados |
| 9    | Repository | Persiste cambios        | SaveChanges()            | Guardado                |
| 10   | API        | Retorna estadísticas    | HTTP 200                 | VoteResultDto           |

#### Request Body

```json
{
  "isHelpful": true
}
```

#### Response (200 OK)

```json
{
  "reviewId": "review-uuid",
  "helpfulVotes": 24,
  "totalVotes": 28,
  "helpfulPercentage": 85.7,
  "userVote": "helpful"
}
```

---

### PROCESO 4: Moderación de Reviews (Panel Admin)

**Endpoint:** `GET /api/reviews/pending`

#### Flujo Completo

| Paso | Actor | Acción                       | Sistema                  | Resultado            |
| ---- | ----- | ---------------------------- | ------------------------ | -------------------- |
| 1    | Admin | Solicita reviews pendientes  | HTTP GET                 | Request recibido     |
| 2    | Auth  | Valida rol Admin             | JWT Claims               | Autorizado           |
| 3    | Query | Obtiene reviews no aprobadas | WHERE IsApproved = false | Lista de reviews     |
| 4    | Query | Ordena por fecha             | ORDER BY CreatedAt ASC   | Más antiguas primero |
| 5    | Query | Incluye datos de comprador   | Include Buyer info       | Info completa        |
| 6    | Query | Incluye datos de vendedor    | Include Seller info      | Info completa        |
| 7    | API   | Retorna lista paginada       | HTTP 200                 | PendingReviewsDto    |

#### Response (200 OK)

```json
{
  "items": [
    {
      "id": "review-uuid",
      "buyerId": "buyer-uuid",
      "buyerName": "Juan Pérez",
      "sellerId": "seller-uuid",
      "sellerName": "AutoDealer Pro",
      "vehicleId": "vehicle-uuid",
      "vehicleTitle": "Toyota Corolla 2022",
      "rating": 2,
      "title": "Mala experiencia",
      "content": "El vehículo tenía problemas que no mencionaron...",
      "isVerifiedPurchase": true,
      "fraudScore": 85,
      "flaggedWords": ["estafa", "robo"],
      "createdAt": "2026-01-20T10:30:00Z"
    }
  ],
  "totalCount": 12,
  "page": 1,
  "pageSize": 20
}
```

---

### PROCESO 5: Responder a Review (Vendedor)

**Endpoint:** `POST /api/reviews/{reviewId}/respond`

#### Flujo Completo

| Paso | Actor        | Acción                      | Sistema                   | Resultado            |
| ---- | ------------ | --------------------------- | ------------------------- | -------------------- |
| 1    | Vendedor     | Escribe respuesta           | HTTP POST                 | Request recibido     |
| 2    | Controller   | Extrae SellerId de JWT      | Claims                    | Usuario identificado |
| 3    | Handler      | Verifica review existe      | GetById()                 | Review encontrada    |
| 4    | Handler      | Verifica es el vendedor     | Review.SellerId == UserId | Autorizado           |
| 5    | Handler      | Verifica no tiene respuesta | Response == null          | Primera respuesta    |
| 6    | Handler      | Crea ReviewResponse         | new ReviewResponse()      | Respuesta creada     |
| 7    | Repository   | Persiste respuesta          | INSERT                    | Guardado             |
| 8    | Events       | Publica ResponseAddedEvent  | RabbitMQ                  | Evento publicado     |
| 9    | Notification | Notifica al comprador       | Email/Push                | Notificación enviada |
| 10   | API          | Retorna 200 OK              | ResponseDto               | Respuesta creada     |

#### Request Body

```json
{
  "content": "Gracias por tu feedback. Nos alegra que hayas tenido una buena experiencia con nosotros. Esperamos verte pronto."
}
```

---

## 🏆 SISTEMA DE BADGES

### Tipos de Badges

```typescript
export enum BadgeType {
  TopRated = 1, // 4.8+ estrellas, 10+ reviews
  TrustedDealer = 2, // 6+ meses, 95%+ positivas
  FiveStarSeller = 3, // Solo 5 estrellas (min 5)
  CustomerChoice = 4, // 80%+ mencionan "recomendado"
  QuickResponder = 5, // Responde en <24h
  VerifiedProfessional = 6, // Documentación verificada
  RisingStar = 7, // Tendencia positiva 3 meses
  VolumeLeader = 8, // 50+ reviews
  ConsistencyWinner = 9, // Rating estable 6+ meses
  CommunityFavorite = 10, // Reviews más útiles del mes
}
```

### Criterios de Badges

| Badge                       | Criterios                                                | Validación   |
| --------------------------- | -------------------------------------------------------- | ------------ |
| **TopRated** ⭐             | Rating promedio >= 4.8 Y total reviews >= 10             | Mensual      |
| **TrustedDealer** ✓         | 6+ meses activo Y 95%+ reviews positivas (4-5 estrellas) | Mensual      |
| **FiveStarSeller** 🌟       | 100% reviews de 5 estrellas Y mínimo 5 reviews           | Continuo     |
| **CustomerChoice** 🏆       | 80%+ reviews mencionan "recomendado" o similar           | Mensual      |
| **QuickResponder** ⚡       | Tiempo promedio de respuesta < 24 horas                  | Semanal      |
| **VerifiedProfessional** 🛡️ | Documentación verificada Y 4+ estrellas promedio         | Al verificar |
| **RisingStar** 📈           | Rating mejoró en últimos 3 meses                         | Trimestral   |
| **VolumeLeader** 📊         | 50+ reviews totales                                      | Continuo     |
| **ConsistencyWinner** 🎯    | Rating estable (±0.2) por 6+ meses                       | Semestral    |
| **CommunityFavorite** ❤️    | Top 10% reviews más útiles del mes                       | Mensual      |

### Recálculo Automático de Badges

**Trigger:** Después de cada nueva review

| Paso | Actor        | Acción                                         | Sistema          | Resultado          |
| ---- | ------------ | ---------------------------------------------- | ---------------- | ------------------ |
| 1    | System       | Nueva review creada                            | Event trigger    | Proceso iniciado   |
| 2    | BadgeService | Obtiene stats del vendedor                     | GetSellerStats() | Stats actualizadas |
| 3    | BadgeService | Evalúa cada tipo de badge                      | Loop BadgeType   | -                  |
| 4    | -            | TopRated: avg >= 4.8 AND count >= 10           | Check criteria   | Pass/Fail          |
| 5    | -            | TrustedDealer: months >= 6 AND positive >= 95% | Check criteria   | Pass/Fail          |
| 6    | -            | FiveStarSeller: all 5 stars AND count >= 5     | Check criteria   | Pass/Fail          |
| 7    | -            | ... (otros badges)                             | Check criteria   | Pass/Fail          |
| 8    | BadgeService | Para badges ganados                            | Grant badge      | Badge creado       |
| 9    | BadgeService | Para badges perdidos                           | Revoke badge     | Badge revocado     |
| 10   | Events       | Publica BadgeEarnedEvent                       | RabbitMQ         | Notificación       |

---

## 📧 SOLICITUDES AUTOMÁTICAS DE REVIEW

### PROCESO 6: Solicitud Automática Post-Compra

**Trigger:** 7 días después de compra completada

| Paso | Actor               | Acción                             | Sistema             | Resultado          |
| ---- | ------------------- | ---------------------------------- | ------------------- | ------------------ |
| 1    | System              | Compra completada (BillingService) | OrderCompletedEvent | Evento recibido    |
| 2    | Scheduler           | Espera 7 días                      | Timer               | Tiempo cumplido    |
| 3    | Handler             | Verifica no existe review          | Check existing      | Sin review         |
| 4    | Handler             | Crea ReviewRequest                 | new ReviewRequest() | Solicitud creada   |
| 5    | Handler             | Genera token único                 | Guid.NewGuid()      | Token generado     |
| 6    | Handler             | Calcula expiración                 | +30 días            | ExpiresAt          |
| 7    | Repository          | Persiste solicitud                 | INSERT              | Guardado           |
| 8    | NotificationService | Envía email                        | Publish event       | Email enviado      |
| 9    | Email               | Contiene link único                | /review/{token}     | Link en email      |
| 10   | System              | Programa recordatorio              | Timer +7 días       | Reminder scheduled |

### Email Template

```html
Asunto: ¿Cómo fue tu experiencia con [SellerName]? Hola [BuyerName], Esperamos
que estés disfrutando tu nuevo vehículo [VehicleTitle]. Nos encantaría saber
cómo fue tu experiencia con [SellerName]. Tu opinión ayuda a otros compradores a
tomar mejores decisiones. [⭐ Escribir mi review] Solo toma 2 minutos. ¡Gracias
por ser parte de OKLA! Este link expira en 30 días.
```

### Estados de Solicitud

```typescript
export enum ReviewRequestStatus {
  Sent = 1, // Esperando respuesta
  Viewed = 2, // Email visto, sin acción
  Completed = 3, // Review escrita
  Expired = 4, // Tiempo expirado
  Declined = 5, // Comprador declinó
  Cancelled = 6, // Cancelada
}
```

---

## 🔔 EVENTOS Y MENSAJERÍA

### Eventos Publicados (RabbitMQ)

| Evento                     | Exchange         | Routing Key        | Payload                        |
| -------------------------- | ---------------- | ------------------ | ------------------------------ |
| `ReviewCreatedEvent`       | `reviews.events` | `review.created`   | ReviewId, SellerId, Rating     |
| `ReviewUpdatedEvent`       | `reviews.events` | `review.updated`   | ReviewId, OldRating, NewRating |
| `ReviewDeletedEvent`       | `reviews.events` | `review.deleted`   | ReviewId, SellerId             |
| `ReviewModeratedEvent`     | `reviews.events` | `review.moderated` | ReviewId, IsApproved           |
| `ReviewResponseAddedEvent` | `reviews.events` | `review.responded` | ReviewId, SellerId             |
| `BadgeEarnedEvent`         | `reviews.events` | `badge.earned`     | SellerId, BadgeType            |
| `BadgeRevokedEvent`        | `reviews.events` | `badge.revoked`    | SellerId, BadgeType            |
| `ReviewRequestSentEvent`   | `reviews.events` | `request.sent`     | RequestId, BuyerId             |

### Eventos Consumidos

| Evento                | Origen         | Acción                        |
| --------------------- | -------------- | ----------------------------- |
| `OrderCompletedEvent` | BillingService | Programar solicitud de review |
| `UserDeletedEvent`    | UserService    | Anonimizar reviews            |

---

## ⚠️ REGLAS DE NEGOCIO

| #   | Regla                  | Descripción                                                   |
| --- | ---------------------- | ------------------------------------------------------------- |
| 1   | Rating 1-5             | Solo valores enteros 1-5 permitidos                           |
| 2   | Una review por compra  | Un usuario no puede dejar múltiples reviews al mismo vendedor |
| 3   | Solo compradores       | Solo usuarios que compraron pueden dejar reviews              |
| 4   | 24h para editar        | Reviews solo editables en primeras 24 horas                   |
| 5   | No auto-voto           | No se puede votar útil una review propia                      |
| 6   | Moderación             | Reviews con palabras ofensivas requieren aprobación           |
| 7   | Badge expiration       | Badges se recalculan mensualmente                             |
| 8   | Mínimo contenido       | Review debe tener mínimo 20 caracteres                        |
| 9   | Máximo 3 recordatorios | Solo 3 emails de recordatorio por solicitud                   |
| 10  | Expiración 30 días     | Solicitudes expiran después de 30 días                        |

### Códigos de Error

| Código      | HTTP Status | Mensaje           | Causa                    |
| ----------- | ----------- | ----------------- | ------------------------ |
| `REV_001`   | 404         | Review not found  | Review no existe         |
| `REV_002`   | 400         | Already reviewed  | Ya existe review         |
| `REV_003`   | 400         | Invalid rating    | Rating fuera de 1-5      |
| `REV_004`   | 403         | Not authorized    | No es autor de la review |
| `REV_005`   | 400         | Edit time expired | Más de 24h               |
| `REV_006`   | 400         | Cannot vote own   | Auto-voto no permitido   |
| `REV_007`   | 400         | Content too short | Menos de 20 caracteres   |
| `BADGE_001` | 404         | Seller not found  | Vendedor no existe       |
| `REQ_001`   | 400         | Already requested | Solicitud ya enviada     |
| `REQ_002`   | 400         | Request expired   | Token expirado           |

---

## ⚙️ CONFIGURACIÓN

### appsettings.json

```json
{
  "ReviewSettings": {
    "MinContentLength": 20,
    "MaxContentLength": 2000,
    "EditWindowHours": 24,
    "RequestExpirationDays": 30,
    "ReminderIntervalDays": 7,
    "MaxReminders": 3,
    "DaysAfterPurchaseToRequest": 7
  },
  "BadgeSettings": {
    "TopRatedMinRating": 4.8,
    "TopRatedMinReviews": 10,
    "TrustedDealerMinMonths": 6,
    "TrustedDealerMinPositive": 0.95,
    "FiveStarMinReviews": 5,
    "VolumeLeaderMinReviews": 50,
    "RecalculationIntervalHours": 24
  },
  "FraudDetection": {
    "MinTrustScore": 50,
    "SameIPDaysThreshold": 30,
    "MaxReviewsPerIP": 5
  }
}
```

---

## 📈 MÉTRICAS PROMETHEUS

| Métrica                           | Tipo    | Labels           | Descripción                      |
| --------------------------------- | ------- | ---------------- | -------------------------------- |
| `reviews_total`                   | Counter | rating, verified | Total de reviews                 |
| `reviews_avg_rating`              | Gauge   | seller_id        | Rating promedio por vendedor     |
| `reviews_moderation_pending`      | Gauge   | -                | Reviews pendientes de moderación |
| `badges_total`                    | Gauge   | badge_type       | Total de badges por tipo         |
| `review_requests_sent`            | Counter | status           | Solicitudes enviadas             |
| `review_requests_completion_rate` | Gauge   | -                | Tasa de conversión               |
| `helpful_votes_total`             | Counter | -                | Total de votos útiles            |

---

## 🎨 COMPONENTES

### PASO 1: ReviewCard - Tarjeta de Review Individual

```typescript
// filepath: src/components/reviews/ReviewCard.tsx
"use client";

import { Star, ThumbsUp, Flag, MessageCircle } from "lucide-react";
import { formatDistanceToNow } from "date-fns";
import { es } from "date-fns/locale";
import { Avatar } from "@/components/ui/Avatar";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import type { Review } from "@/types/review";

interface ReviewCardProps {
  review: Review;
  onMarkHelpful?: (reviewId: string) => void;
  onReport?: (reviewId: string) => void;
  onReply?: (reviewId: string) => void;
  showDealerResponse?: boolean;
}

export function ReviewCard({
  review,
  onMarkHelpful,
  onReport,
  onReply,
  showDealerResponse = true,
}: ReviewCardProps) {
  return (
    <div className="bg-white rounded-lg border p-6">
      {/* Header */}
      <div className="flex items-start justify-between mb-4">
        <div className="flex items-center gap-3">
          <Avatar src={review.user.image} alt={review.user.name} size="md" />
          <div>
            <div className="flex items-center gap-2">
              <span className="font-semibold text-gray-900">{review.user.name}</span>
              {review.isVerifiedPurchase && (
                <Badge variant="success" size="sm">
                  ✓ Compra verificada
                </Badge>
              )}
            </div>
            <time className="text-sm text-gray-500">
              {formatDistanceToNow(new Date(review.createdAt), {
                addSuffix: true,
                locale: es,
              })}
            </time>
          </div>
        </div>

        {/* Rating Stars */}
        <div className="flex items-center gap-1">
          {[1, 2, 3, 4, 5].map((star) => (
            <Star
              key={star}
              size={18}
              className={
                star <= review.rating
                  ? "fill-yellow-400 text-yellow-400"
                  : "text-gray-300"
              }
            />
          ))}
        </div>
      </div>

      {/* Title */}
      {review.title && (
        <h3 className="font-semibold text-gray-900 mb-2">{review.title}</h3>
      )}

      {/* Content */}
      <p className="text-gray-700 mb-4 whitespace-pre-wrap">{review.content}</p>

      {/* Images */}
      {review.images && review.images.length > 0 && (
        <div className="grid grid-cols-4 gap-2 mb-4">
          {review.images.map((image, index) => (
            <img
              key={index}
              src={image}
              alt={`Review imagen ${index + 1}`}
              className="w-full h-24 object-cover rounded-lg cursor-pointer hover:opacity-90"
            />
          ))}
        </div>
      )}

      {/* Rating Breakdown (for dealers) */}
      {review.ratingBreakdown && (
        <div className="bg-gray-50 rounded-lg p-4 mb-4 space-y-2">
          <RatingItem
            label="Atención al cliente"
            rating={review.ratingBreakdown.customerService}
          />
          <RatingItem
            label="Honestidad"
            rating={review.ratingBreakdown.honesty}
          />
          <RatingItem
            label="Proceso de compra"
            rating={review.ratingBreakdown.process}
          />
          <RatingItem
            label="Condición del vehículo"
            rating={review.ratingBreakdown.vehicleCondition}
          />
        </div>
      )}

      {/* Actions */}
      <div className="flex items-center gap-4 pt-4 border-t">
        <Button
          variant="ghost"
          size="sm"
          onClick={() => onMarkHelpful?.(review.id)}
          className="text-gray-600 hover:text-primary-600"
        >
          <ThumbsUp size={16} className="mr-1" />
          Útil ({review.helpfulCount})
        </Button>

        {onReply && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => onReply(review.id)}
            className="text-gray-600 hover:text-primary-600"
          >
            <MessageCircle size={16} className="mr-1" />
            Responder
          </Button>
        )}

        <Button
          variant="ghost"
          size="sm"
          onClick={() => onReport?.(review.id)}
          className="text-gray-600 hover:text-red-600"
        >
          <Flag size={16} className="mr-1" />
          Reportar
        </Button>
      </div>

      {/* Dealer Response */}
      {showDealerResponse && review.dealerResponse && (
        <div className="mt-4 ml-12 bg-blue-50 rounded-lg p-4 border-l-4 border-blue-500">
          <div className="flex items-center gap-2 mb-2">
            <Badge variant="primary" size="sm">
              Respuesta del dealer
            </Badge>
            <time className="text-xs text-gray-500">
              {formatDistanceToNow(new Date(review.dealerResponse.createdAt), {
                addSuffix: true,
                locale: es,
              })}
            </time>
          </div>
          <p className="text-gray-700 text-sm">{review.dealerResponse.content}</p>
        </div>
      )}
    </div>
  );
}

function RatingItem({ label, rating }: { label: string; rating: number }) {
  return (
    <div className="flex items-center justify-between">
      <span className="text-sm text-gray-600">{label}</span>
      <div className="flex items-center gap-1">
        {[1, 2, 3, 4, 5].map((star) => (
          <Star
            key={star}
            size={14}
            className={
              star <= rating
                ? "fill-yellow-400 text-yellow-400"
                : "text-gray-300"
            }
          />
        ))}
      </div>
    </div>
  );
}
```

---

### PASO 2: ReviewForm - Formulario para Crear Review

```typescript
// filepath: src/components/reviews/ReviewForm.tsx
"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Star, Upload, X } from "lucide-react";
import { FormField } from "@/components/ui/FormField";
import { Input } from "@/components/ui/Input";
import { Textarea } from "@/components/ui/Textarea";
import { Button } from "@/components/ui/Button";
import { ImageUpload } from "@/components/ui/ImageUpload";
import { useCreateReview } from "@/lib/hooks/useReviews";

const reviewSchema = z.object({
  rating: z.number().min(1).max(5),
  title: z.string().min(5, "Mínimo 5 caracteres").max(100, "Máximo 100 caracteres"),
  content: z.string().min(20, "Mínimo 20 caracteres").max(2000, "Máximo 2000 caracteres"),
  // Rating breakdown (for dealer reviews)
  customerService: z.number().min(1).max(5).optional(),
  honesty: z.number().min(1).max(5).optional(),
  process: z.number().min(1).max(5).optional(),
  vehicleCondition: z.number().min(1).max(5).optional(),
});

type ReviewFormData = z.infer<typeof reviewSchema>;

interface ReviewFormProps {
  targetType: "dealer" | "vehicle";
  targetId: string;
  onSuccess?: () => void;
}

export function ReviewForm({ targetType, targetId, onSuccess }: ReviewFormProps) {
  const [images, setImages] = useState<string[]>([]);
  const { mutate: createReview, isPending } = useCreateReview();

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors },
  } = useForm<ReviewFormData>({
    resolver: zodResolver(reviewSchema),
    defaultValues: {
      rating: 0,
    },
  });

  const rating = watch("rating");

  const onSubmit = async (data: ReviewFormData) => {
    createReview(
      {
        ...data,
        targetType,
        targetId,
        images,
        ratingBreakdown:
          targetType === "dealer"
            ? {
                customerService: data.customerService!,
                honesty: data.honesty!,
                process: data.process!,
                vehicleCondition: data.vehicleCondition!,
              }
            : undefined,
      },
      {
        onSuccess: () => {
          onSuccess?.();
        },
      }
    );
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
      {/* Overall Rating */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Calificación general *
        </label>
        <div className="flex items-center gap-2">
          {[1, 2, 3, 4, 5].map((star) => (
            <button
              key={star}
              type="button"
              onClick={() => setValue("rating", star, { shouldValidate: true })}
              className="focus:outline-none"
            >
              <Star
                size={32}
                className={
                  star <= rating
                    ? "fill-yellow-400 text-yellow-400 cursor-pointer hover:scale-110 transition"
                    : "text-gray-300 cursor-pointer hover:text-gray-400 transition"
                }
              />
            </button>
          ))}
          {rating > 0 && (
            <span className="ml-2 text-sm text-gray-600">
              {rating === 5 && "Excelente"}
              {rating === 4 && "Muy bueno"}
              {rating === 3 && "Bueno"}
              {rating === 2 && "Regular"}
              {rating === 1 && "Malo"}
            </span>
          )}
        </div>
        {errors.rating && (
          <p className="text-sm text-red-600 mt-1">Selecciona una calificación</p>
        )}
      </div>

      {/* Dealer Rating Breakdown */}
      {targetType === "dealer" && (
        <div className="bg-gray-50 rounded-lg p-4 space-y-4">
          <h3 className="font-semibold text-gray-900">Calificación detallada</h3>

          <RatingSlider
            label="Atención al cliente"
            value={watch("customerService") || 0}
            onChange={(val) => setValue("customerService", val)}
          />
          <RatingSlider
            label="Honestidad y transparencia"
            value={watch("honesty") || 0}
            onChange={(val) => setValue("honesty", val)}
          />
          <RatingSlider
            label="Proceso de compra"
            value={watch("process") || 0}
            onChange={(val) => setValue("process", val)}
          />
          <RatingSlider
            label="Condición del vehículo"
            value={watch("vehicleCondition") || 0}
            onChange={(val) => setValue("vehicleCondition", val)}
          />
        </div>
      )}

      {/* Title */}
      <FormField label="Título de tu review" error={errors.title?.message}>
        <Input {...register("title")} placeholder="Resumen de tu experiencia" />
      </FormField>

      {/* Content */}
      <FormField label="Tu opinión" error={errors.content?.message}>
        <Textarea
          {...register("content")}
          rows={6}
          placeholder="Cuéntanos sobre tu experiencia..."
          maxLength={2000}
        />
        <p className="text-xs text-gray-500 mt-1">
          {watch("content")?.length || 0}/2000 caracteres
        </p>
      </FormField>

      {/* Images */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Fotos (opcional)
        </label>
        <ImageUpload
          images={images}
          onImagesChange={setImages}
          maxImages={4}
          accept="image/*"
        />
        <p className="text-xs text-gray-500 mt-1">Máximo 4 fotos</p>
      </div>

      {/* Submit */}
      <div className="flex items-center gap-3">
        <Button type="submit" disabled={isPending}>
          {isPending ? "Publicando..." : "Publicar review"}
        </Button>
        <p className="text-xs text-gray-500">
          Tu review será revisada antes de publicarse
        </p>
      </div>
    </form>
  );
}

function RatingSlider({
  label,
  value,
  onChange,
}: {
  label: string;
  value: number;
  onChange: (value: number) => void;
}) {
  return (
    <div>
      <div className="flex items-center justify-between mb-2">
        <label className="text-sm text-gray-700">{label}</label>
        <span className="text-sm font-medium text-gray-900">{value}/5</span>
      </div>
      <div className="flex items-center gap-1">
        {[1, 2, 3, 4, 5].map((star) => (
          <button
            key={star}
            type="button"
            onClick={() => onChange(star)}
            className="focus:outline-none"
          >
            <Star
              size={20}
              className={
                star <= value
                  ? "fill-yellow-400 text-yellow-400"
                  : "text-gray-300 hover:text-gray-400"
              }
            />
          </button>
        ))}
      </div>
    </div>
  );
}
```

---

### PASO 3: ReviewStats - Estadísticas y Distribución de Ratings

```typescript
// filepath: src/components/reviews/ReviewStats.tsx
"use client";

import { Star } from "lucide-react";
import { Progress } from "@/components/ui/Progress";
import type { ReviewStats as ReviewStatsType } from "@/types/review";

interface ReviewStatsProps {
  stats: ReviewStatsType;
}

export function ReviewStats({ stats }: ReviewStatsProps) {
  const totalReviews = stats.totalReviews;
  const averageRating = stats.averageRating;

  return (
    <div className="bg-white rounded-lg border p-6">
      <div className="flex items-start gap-6">
        {/* Average Rating */}
        <div className="text-center">
          <div className="text-4xl font-bold text-gray-900 mb-1">
            {averageRating.toFixed(1)}
          </div>
          <div className="flex items-center justify-center gap-1 mb-2">
            {[1, 2, 3, 4, 5].map((star) => (
              <Star
                key={star}
                size={20}
                className={
                  star <= Math.round(averageRating)
                    ? "fill-yellow-400 text-yellow-400"
                    : "text-gray-300"
                }
              />
            ))}
          </div>
          <p className="text-sm text-gray-600">{totalReviews} reviews</p>
        </div>

        {/* Distribution */}
        <div className="flex-1 space-y-2">
          {[5, 4, 3, 2, 1].map((star) => {
            const count = stats.distribution[`star${star}` as keyof typeof stats.distribution] || 0;
            const percentage = totalReviews > 0 ? (count / totalReviews) * 100 : 0;

            return (
              <div key={star} className="flex items-center gap-3">
                <div className="flex items-center gap-1 w-16">
                  <Star size={14} className="fill-yellow-400 text-yellow-400" />
                  <span className="text-sm text-gray-700">{star}</span>
                </div>
                <Progress value={percentage} className="flex-1" />
                <span className="text-sm text-gray-600 w-12 text-right">
                  {count}
                </span>
              </div>
            );
          })}
        </div>
      </div>

      {/* Rating Breakdown (for dealers) */}
      {stats.ratingBreakdownAverage && (
        <div className="mt-6 pt-6 border-t grid grid-cols-2 gap-4">
          <BreakdownItem
            label="Atención"
            rating={stats.ratingBreakdownAverage.customerService}
          />
          <BreakdownItem
            label="Honestidad"
            rating={stats.ratingBreakdownAverage.honesty}
          />
          <BreakdownItem
            label="Proceso"
            rating={stats.ratingBreakdownAverage.process}
          />
          <BreakdownItem
            label="Condición"
            rating={stats.ratingBreakdownAverage.vehicleCondition}
          />
        </div>
      )}
    </div>
  );
}

function BreakdownItem({ label, rating }: { label: string; rating: number }) {
  return (
    <div className="flex items-center justify-between">
      <span className="text-sm text-gray-700">{label}</span>
      <div className="flex items-center gap-1">
        <span className="text-sm font-medium text-gray-900">{rating.toFixed(1)}</span>
        <Star size={14} className="fill-yellow-400 text-yellow-400" />
      </div>
    </div>
  );
}
```

---

### PASO 4: ReviewFilters - Filtros y Ordenamiento

```typescript
// filepath: src/components/reviews/ReviewFilters.tsx
"use client";

import { Star, ChevronDown } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Select } from "@/components/ui/Select";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/DropdownMenu";

interface ReviewFiltersProps {
  selectedRating: number | null;
  onRatingChange: (rating: number | null) => void;
  sortBy: "recent" | "helpful" | "rating_high" | "rating_low";
  onSortChange: (sort: string) => void;
  verifiedOnly: boolean;
  onVerifiedOnlyChange: (verified: boolean) => void;
}

export function ReviewFilters({
  selectedRating,
  onRatingChange,
  sortBy,
  onSortChange,
  verifiedOnly,
  onVerifiedOnlyChange,
}: ReviewFiltersProps) {
  return (
    <div className="flex items-center gap-3 flex-wrap">
      {/* Rating Filter */}
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="outline" size="sm">
            {selectedRating ? (
              <>
                <Star size={16} className="mr-1 fill-yellow-400 text-yellow-400" />
                {selectedRating} estrellas
              </>
            ) : (
              "Todas las calificaciones"
            )}
            <ChevronDown size={16} className="ml-1" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent>
          <DropdownMenuItem onClick={() => onRatingChange(null)}>
            Todas las calificaciones
          </DropdownMenuItem>
          {[5, 4, 3, 2, 1].map((rating) => (
            <DropdownMenuItem key={rating} onClick={() => onRatingChange(rating)}>
              <div className="flex items-center gap-2">
                {[...Array(rating)].map((_, i) => (
                  <Star key={i} size={14} className="fill-yellow-400 text-yellow-400" />
                ))}
                <span>({rating} estrellas)</span>
              </div>
            </DropdownMenuItem>
          ))}
        </DropdownMenuContent>
      </DropdownMenu>

      {/* Sort */}
      <Select value={sortBy} onChange={(e) => onSortChange(e.target.value)}>
        <option value="recent">Más recientes</option>
        <option value="helpful">Más útiles</option>
        <option value="rating_high">Mejor calificación</option>
        <option value="rating_low">Peor calificación</option>
      </Select>

      {/* Verified Only */}
      <Button
        variant={verifiedOnly ? "primary" : "outline"}
        size="sm"
        onClick={() => onVerifiedOnlyChange(!verifiedOnly)}
      >
        ✓ Solo compras verificadas
      </Button>
    </div>
  );
}
```

---

### PASO 5: ReputationBadge - Badge de Reputación

```typescript
// filepath: src/components/reviews/ReputationBadge.tsx
import { Star, Award, ShieldCheck } from "lucide-react";
import { Badge } from "@/components/ui/Badge";

interface ReputationBadgeProps {
  averageRating: number;
  totalReviews: number;
  size?: "sm" | "md" | "lg";
}

export function ReputationBadge({
  averageRating,
  totalReviews,
  size = "md",
}: ReputationBadgeProps) {
  const getReputationLevel = () => {
    if (averageRating >= 4.8 && totalReviews >= 50) {
      return {
        label: "Dealer Elite",
        icon: Award,
        color: "text-purple-600 bg-purple-100",
        description: "Top 1% de dealers",
      };
    }
    if (averageRating >= 4.5 && totalReviews >= 20) {
      return {
        label: "Dealer Verificado",
        icon: ShieldCheck,
        color: "text-blue-600 bg-blue-100",
        description: "Altamente confiable",
      };
    }
    if (averageRating >= 4.0) {
      return {
        label: "Buen Dealer",
        icon: Star,
        color: "text-green-600 bg-green-100",
        description: "Recomendado",
      };
    }
    return null;
  };

  const reputation = getReputationLevel();

  if (!reputation) return null;

  const Icon = reputation.icon;

  return (
    <div className="inline-flex items-center gap-2">
      <Badge className={reputation.color}>
        <Icon size={size === "sm" ? 12 : size === "md" ? 14 : 16} />
        <span className="ml-1">{reputation.label}</span>
      </Badge>
      {size !== "sm" && (
        <span className="text-xs text-gray-500">{reputation.description}</span>
      )}
    </div>
  );
}
```

---

## 📄 PÁGINAS

### PASO 6: Página de Reviews de Dealer

```typescript
// filepath: src/app/(main)/dealer/[id]/reviews/page.tsx
import { Metadata } from "next";
import { notFound } from "next/navigation";
import { ReviewsList } from "@/components/reviews/ReviewsList";
import { ReviewStats } from "@/components/reviews/ReviewStats";
import { ReputationBadge } from "@/components/reviews/ReputationBadge";
import { Button } from "@/components/ui/Button";
import { reviewService } from "@/lib/services/reviewService";

interface PageProps {
  params: { id: string };
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const stats = await reviewService.getDealerStats(params.id);

  return {
    title: `Reviews - ${stats.dealerName} | OKLA`,
    description: `${stats.totalReviews} reviews con ${stats.averageRating.toFixed(1)} estrellas`,
  };
}

export default async function DealerReviewsPage({ params }: PageProps) {
  const stats = await reviewService.getDealerStats(params.id);

  if (!stats) {
    notFound();
  }

  return (
    <div className="max-w-6xl mx-auto px-4 py-8">
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">
          Reviews de {stats.dealerName}
        </h1>
        <ReputationBadge
          averageRating={stats.averageRating}
          totalReviews={stats.totalReviews}
        />
      </div>

      {/* Stats */}
      <div className="mb-8">
        <ReviewStats stats={stats} />
      </div>

      {/* Reviews List */}
      <ReviewsList targetType="dealer" targetId={params.id} />
    </div>
  );
}
```

---

### PASO 7: Modal de Crear Review

```typescript
// filepath: src/components/reviews/CreateReviewModal.tsx
"use client";

import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/Dialog";
import { ReviewForm } from "./ReviewForm";

interface CreateReviewModalProps {
  isOpen: boolean;
  onClose: () => void;
  targetType: "dealer" | "vehicle";
  targetId: string;
  targetName: string;
}

export function CreateReviewModal({
  isOpen,
  onClose,
  targetType,
  targetId,
  targetName,
}: CreateReviewModalProps) {
  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>
            Escribe tu review
            {targetType === "dealer" ? " del dealer" : " del vehículo"}
          </DialogTitle>
          <p className="text-sm text-gray-600">{targetName}</p>
        </DialogHeader>

        <ReviewForm
          targetType={targetType}
          targetId={targetId}
          onSuccess={onClose}
        />
      </DialogContent>
    </Dialog>
  );
}
```

---

## 🪝 HOOKS Y SERVICIOS

### PASO 8: useReviews Hook

```typescript
// filepath: src/lib/hooks/useReviews.ts
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { reviewService } from "@/lib/services/reviewService";
import { toast } from "sonner";
import type { CreateReviewData, UpdateReviewData } from "@/types/review";

export function useReviews(targetType: "dealer" | "vehicle", targetId: string) {
  return useQuery({
    queryKey: ["reviews", targetType, targetId],
    queryFn: () => reviewService.getReviews({ targetType, targetId }),
  });
}

export function useCreateReview() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateReviewData) => reviewService.createReview(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["reviews"] });
      toast.success("Review enviada para moderación");
    },
    onError: () => {
      toast.error("Error al crear review");
    },
  });
}

export function useMarkHelpful() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (reviewId: string) => reviewService.markHelpful(reviewId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["reviews"] });
      toast.success("Gracias por tu feedback");
    },
  });
}

export function useReportReview() {
  return useMutation({
    mutationFn: ({ reviewId, reason }: { reviewId: string; reason: string }) =>
      reviewService.reportReview(reviewId, reason),
    onSuccess: () => {
      toast.success("Review reportada. Revisaremos pronto.");
    },
  });
}

export function useDealerResponse() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      reviewId,
      content,
    }: {
      reviewId: string;
      content: string;
    }) => reviewService.addDealerResponse(reviewId, content),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["reviews"] });
      toast.success("Respuesta publicada");
    },
  });
}
```

---

### PASO 9: reviewService API Client

```typescript
// filepath: src/lib/services/reviewService.ts
import { apiClient } from "./apiClient";
import type {
  Review,
  ReviewStats,
  CreateReviewData,
  UpdateReviewData,
  ReviewFilters,
} from "@/types/review";

export const reviewService = {
  async getReviews(filters: ReviewFilters) {
    const params = new URLSearchParams();
    if (filters.targetType) params.append("targetType", filters.targetType);
    if (filters.targetId) params.append("targetId", filters.targetId);
    if (filters.rating) params.append("rating", filters.rating.toString());
    if (filters.verifiedOnly) params.append("verifiedOnly", "true");
    if (filters.sortBy) params.append("sortBy", filters.sortBy);

    const { data } = await apiClient.get<Review[]>(`/reviews?${params}`);
    return data;
  },

  async getReview(id: string) {
    const { data } = await apiClient.get<Review>(`/reviews/${id}`);
    return data;
  },

  async createReview(reviewData: CreateReviewData) {
    const { data } = await apiClient.post<Review>("/reviews", reviewData);
    return data;
  },

  async updateReview(id: string, updateData: UpdateReviewData) {
    const { data } = await apiClient.put<Review>(`/reviews/${id}`, updateData);
    return data;
  },

  async deleteReview(id: string) {
    await apiClient.delete(`/reviews/${id}`);
  },

  async markHelpful(id: string) {
    await apiClient.post(`/reviews/${id}/helpful`);
  },

  async reportReview(id: string, reason: string) {
    await apiClient.post(`/reviews/${id}/report`, { reason });
  },

  async addDealerResponse(id: string, content: string) {
    const { data } = await apiClient.post(`/reviews/${id}/response`, {
      content,
    });
    return data;
  },

  async getDealerStats(dealerId: string) {
    const { data } = await apiClient.get<ReviewStats>(
      `/reviews/stats/dealer/${dealerId}`,
    );
    return data;
  },

  async getVehicleStats(vehicleId: string) {
    const { data } = await apiClient.get<ReviewStats>(
      `/reviews/stats/vehicle/${vehicleId}`,
    );
    return data;
  },

  async getPendingReviews() {
    const { data } = await apiClient.get<Review[]>("/reviews/pending");
    return data;
  },

  async moderateReview(
    id: string,
    action: "approve" | "reject",
    reason?: string,
  ) {
    const { data } = await apiClient.post(`/reviews/${id}/moderate`, {
      action,
      reason,
    });
    return data;
  },
};
```

---

## 📦 TIPOS TYPESCRIPT

### PASO 10: Tipos de Review

```typescript
// filepath: src/types/review.ts
export interface Review {
  id: string;
  targetType: "dealer" | "vehicle";
  targetId: string;
  userId: string;
  user: {
    id: string;
    name: string;
    image?: string;
  };
  rating: number;
  title: string;
  content: string;
  images?: string[];
  isVerifiedPurchase: boolean;
  helpfulCount: number;
  status: "pending" | "approved" | "rejected";
  moderationScore?: number;
  ratingBreakdown?: {
    customerService: number;
    honesty: number;
    process: number;
    vehicleCondition: number;
  };
  dealerResponse?: {
    content: string;
    createdAt: string;
  };
  createdAt: string;
  updatedAt: string;
}

export interface ReviewStats {
  targetType: "dealer" | "vehicle";
  targetId: string;
  dealerName?: string;
  vehicleName?: string;
  totalReviews: number;
  averageRating: number;
  distribution: {
    star5: number;
    star4: number;
    star3: number;
    star2: number;
    star1: number;
  };
  ratingBreakdownAverage?: {
    customerService: number;
    honesty: number;
    process: number;
    vehicleCondition: number;
  };
}

export interface CreateReviewData {
  targetType: "dealer" | "vehicle";
  targetId: string;
  rating: number;
  title: string;
  content: string;
  images?: string[];
  ratingBreakdown?: {
    customerService: number;
    honesty: number;
    process: number;
    vehicleCondition: number;
  };
}

export interface UpdateReviewData {
  rating?: number;
  title?: string;
  content?: string;
  images?: string[];
}

export interface ReviewFilters {
  targetType?: "dealer" | "vehicle";
  targetId?: string;
  rating?: number;
  verifiedOnly?: boolean;
  sortBy?: "recent" | "helpful" | "rating_high" | "rating_low";
  page?: number;
  pageSize?: number;
}
```

---

## ✅ VALIDACIÓN

```bash
pnpm dev

# Verificar:
# - /dealer/[id]/reviews muestra stats y lista de reviews
# - Filtros de rating funcionan correctamente
# - Ordenamiento (recientes, útiles, rating) funciona
# - Modal de crear review se abre y valida
# - Calificación con estrellas funciona (overall + breakdown)
# - Upload de imágenes funciona (máximo 4)
# - Review se envía para moderación
# - Badge "Compra verificada" aparece cuando aplica
# - Botón "Útil" incrementa contador
# - Botón "Reportar" abre diálogo
# - Dealer puede responder a reviews
# - Respuesta del dealer se muestra debajo del review
# - ReputationBadge se calcula correctamente
# - Estadísticas (promedio, distribución) actualizan
```

---

## 📝 NOTAS FINALES

### Moderación Automática con IA

El ReviewService backend implementa moderación con OpenAI/Claude para detectar:

- Spam
- Lenguaje ofensivo
- Reviews falsas
- Contenido inapropiado

Puntuación de moderación:

- **> 0.8**: Auto-aprobado
- **0.5 - 0.8**: Revisión manual
- **< 0.5**: Auto-rechazado

### Best Practices

1. **Solo compras verificadas** pueden dejar review de vehículo
2. **Un review por transacción** - No duplicados
3. **Dealers pueden responder** - Solo una respuesta por review
4. **Edición limitada** - Solo en primeras 24 horas
5. **Reportes anónimos** - Proteger identidad del reportador

### Agregaciones en Tiempo Real

Las estadísticas se actualizan mediante:

- **Redis cache** - Stats pre-calculadas (TTL: 5 min)
- **Event sourcing** - Actualización incremental
- **Background jobs** - Re-cálculo nocturno completo

### KPIs a Monitorear

- **Tasa de reviews** - % de compradores que dejan review
- **Distribución de ratings** - Sesgo hacia positivos/negativos
- **Tiempo de moderación** - Promedio de aprobación
- **False positives** - Reviews legítimas rechazadas
- **Response rate** - % de dealers que responden

---

## 🧪 SUITE DE TESTS

### Tests Implementados

#### ReviewServiceTests.cs (Sprint 14 - Básico)

| Test                                                | Descripción                    |
| --------------------------------------------------- | ------------------------------ |
| `Review_ShouldBeCreated_WithValidData`              | Crear review con datos válidos |
| `ReviewSummary_ShouldCalculateMetrics_Correctly`    | Cálculo de métricas agregadas  |
| `Review_Rating_ShouldBeValid` (Theory 1-5)          | Validar ratings 1-5            |
| `ReviewResponse_ShouldBeCreated_WithValidData`      | Respuesta de vendedor          |
| `ReviewSummary_WithNoReviews_ShouldHaveZeroMetrics` | Summary sin reviews            |
| `ReviewSummary_GetRatingDistribution_ShouldReturn`  | Distribución de ratings        |

#### Sprint15AdvancedReviewsTests.cs (Sprint 15 - Avanzado)

| Test                                             | Descripción                |
| ------------------------------------------------ | -------------------------- |
| `ReviewHelpfulVote_ShouldBeCreated_WithValid`    | Crear voto de utilidad     |
| `ReviewHelpfulVote_WhenNotHelpful_ShouldBeFalse` | Voto negativo              |
| `ReviewHelpfulVote_ShouldTrackUserAgent`         | Tracking de IP y UserAgent |
| `SellerBadge_ShouldBeCreated_WithValidData`      | Crear badge                |
| `BadgeType_ShouldHaveExpectedValues` (Theory)    | Validar enum BadgeType     |
| `SellerBadge_WhenRevoked_ShouldHaveRevokedAt`    | Revocación de badge        |
| `SellerBadge_WithExpiry_ShouldTrackExpiration`   | Expiración de badge        |
| `ReviewRequest_ShouldBeCreated_WithValidData`    | Solicitud automática       |

**Total:** 18 tests (✅ 100% passing)

---

## 📚 REFERENCIAS

### Backend

- [ReviewsController](../../backend/ReviewService/ReviewService.Api/Controllers/ReviewsController.cs)
- [Review Entity](../../backend/ReviewService/ReviewService.Domain/Entities/Review.cs)
- [SellerBadge Entity](../../backend/ReviewService/ReviewService.Domain/Entities/SellerBadge.cs)
- [ReviewRequest Entity](../../backend/ReviewService/ReviewService.Domain/Entities/ReviewRequest.cs)
- [BadgeCalculationService](../../backend/ReviewService/ReviewService.Domain/Services/BadgeCalculationService.cs)
- [ReviewServiceTests](../../backend/_Tests/ReviewService.Tests/ReviewServiceTests.cs)
- [Sprint15AdvancedReviewsTests](../../backend/_Tests/ReviewService.Tests/Sprint15AdvancedReviewsTests.cs)

### Documentación Process Matrix

- [01-review-service.md](../../docs/process-matrix/07-REVIEWS-REPUTACION/01-review-service.md) - **Documento fuente de esta implementación** ⭐

---

**Última actualización:** Enero 29, 2026  
**Versión:** 2.0.0 (Actualizado con todos los procesos de process-matrix)  
**Siguiente documento:** `21-recomendaciones.md` - Sistema de recomendaciones con ML

---

# ANEXO: Review Request Response (Fusionado desde 12-review-request-response.md)

> **Prioridad:** P1 (Crítico - Conversión de reviews)  
> **Complejidad:** 🟡 Media (Token validation, pre-filled forms, success states)  
> **Dependencias:** ReviewService (ReviewRequestController), BillingService (OrderCompleted event)  
> **Puerto Backend:** 5091 (ReviewService)  
> **Estado:** ✅ Backend 90% | ✅ UI 80%

---

## ✅ INTEGRACIÓN CON REVIEWSERVICE (process-matrix/21-REVIEWS-REPUTACION)

### Servicios Involucrados

| Servicio                    | Puerto | Estado  | Responsabilidad                     |
| --------------------------- | ------ | ------- | ----------------------------------- |
| **ReviewService**           | 5091   | ✅ 90%  | Solicitudes y creación de reviews   |
| **BillingService**          | 5008   | ✅ 100% | Trigger OrderCompletedEvent         |
| **NotificationService**     | 5006   | ✅ 100% | Emails de solicitud y recordatorios |
| **UserService**             | 5003   | ✅ 100% | Datos del comprador                 |
| **DealerManagementService** | 5039   | ✅ 100% | Datos del dealer                    |

### Proceso Relacionado: REVIEW-001 (Comprador Deja Review)

**Fuente:** `process-matrix/21-REVIEWS-REPUTACION/01-dealer-reviews.md`

| Paso | Acción                                    | Sistema             | Actor     | Evidencia           |
| ---- | ----------------------------------------- | ------------------- | --------- | ------------------- |
| 1    | **Trigger: Solicitud de review**          | Sistema             | SYS-NOTIF | **Email sent**      |
| 2    | Usuario recibe email con link único       | NotificationService | USR-REG   | Email delivered     |
| 3    | Click en link `/review/response/{token}`  | Frontend            | USR-REG   | Page view           |
| 4    | Validar token                             | ReviewService       | Sistema   | Token validation    |
| 5    | Si válido: Mostrar formulario pre-llenado | Frontend            | Sistema   | Form shown          |
| 6    | **Rating general (1-5 estrellas)**        | Frontend            | USR-REG   | **Rating input**    |
| 7    | **Escribir contenido del review**         | Frontend            | USR-REG   | **Content input**   |
| 8    | Click "Publicar Review"                   | Frontend            | USR-REG   | Submit clicked      |
| 9    | **POST /api/reviews/dealer/{dealerId}**   | Gateway             | USR-REG   | **Request**         |
| 10   | **Validar contenido** (spam, profanity)   | ReviewService       | Sistema   | **Validation**      |
| 11   | **Crear DealerReview**                    | ReviewService       | Sistema   | **Review created**  |
| 12   | **Actualizar DealerRatingSummary**        | ReviewService       | Sistema   | **Summary updated** |
| 13   | **Notificar al dealer**                   | NotificationService | SYS-NOTIF | **Dealer notified** |

### Endpoints de Review Request

| Método | Endpoint                                | Descripción                         | Auth |
| ------ | --------------------------------------- | ----------------------------------- | ---- |
| `GET`  | `/api/review-requests/validate/{token}` | Validar token de solicitud          | ❌   |
| `GET`  | `/api/review-requests/{token}/details`  | Info pre-llenada (dealer, vehículo) | ❌   |
| `POST` | `/api/reviews/dealer/{dealerId}`        | Crear review                        | ✅   |
| `PUT`  | `/api/review-requests/{token}/decline`  | Declinar solicitud                  | ❌   |

### Rutas UI

| Ruta                                 | Estado | Descripción                     |
| ------------------------------------ | ------ | ------------------------------- |
| `/review/response/{token}`           | ✅ 80% | Página de respuesta a solicitud |
| `/review/response/{token}/success`   | ✅ 90% | Confirmación de review enviado  |
| `/review/response/{token}/expired`   | ✅ 80% | Token expirado                  |
| `/review/response/{token}/completed` | ✅ 80% | Ya dejó review                  |

### Estados de ReviewRequest

| Estado      | Descripción                         |
| ----------- | ----------------------------------- |
| `Sent`      | Email enviado, esperando respuesta  |
| `Viewed`    | Usuario vio la página pero no envió |
| `Completed` | Review escrito exitosamente         |
| `Expired`   | Token expiró (30 días)              |
| `Declined`  | Usuario declinó dejar review        |
| `Cancelled` | Cancelada por admin                 |

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura del Sistema](#arquitectura-del-sistema)
2. [Backend API](#backend-api)
3. [Flujo Completo](#flujo-completo)
4. [Componentes](#componentes)
5. [Páginas](#páginas)
6. [Hooks y Servicios](#hooks-y-servicios)
7. [Tipos TypeScript](#tipos-typescript)
8. [Email Templates](#email-templates)

---

## 🏗️ ARQUITECTURA DEL SISTEMA

### Review Request Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    AUTOMATIC REVIEW REQUEST FLOW                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  📦 TRIGGER: Order Completed                                                │
│  BillingService publica: OrderCompletedEvent                                │
│  ├─ orderId: uuid                                                           │
│  ├─ buyerId: uuid                                                           │
│  ├─ sellerId: uuid                                                          │
│  ├─ vehicleId: uuid                                                         │
│  ├─ completedAt: timestamp                                                  │
│  └─ totalAmount: decimal                                                    │
│                                                                             │
│  ⏰ DELAY: 7 días después                                                   │
│  ReviewService consume OrderCompletedEvent                                  │
│  ├─ Verifica que no exista review de este buyer para este seller           │
│  ├─ Crea ReviewRequest entity:                                             │
│  │   • Token: unique GUID                                                  │
│  │   • Status: Sent                                                        │
│  │   • ExpiresAt: +30 días                                                 │
│  │   • MaxReminders: 3                                                     │
│  └─ Publica ReviewRequestSentEvent                                         │
│                                                                             │
│  📧 EMAIL: NotificationService                                              │
│  Template: review-request                                                   │
│  ├─ Subject: "¿Cómo fue tu experiencia con [Dealer Name]?"                 │
│  ├─ Body:                                                                  │
│  │   "Hola [Buyer Name],                                                   │
│  │    Hace 7 días compraste un [Vehicle] a [Dealer].                       │
│  │    ¿Nos cuentas cómo te fue?                                            │
│  │                                                                          │
│  │    [Deja tu review] 👉 https://okla.com.do/review/response/{token}"    │
│  │                                                                          │
│  └─ CTA Button: "Escribir Review"                                          │
│                                                                             │
│  🌐 LANDING: Review Response Page                                           │
│  GET https://okla.com.do/review/response/{token}                            │
│  ├─ Validar token: GET /api/review-requests/validate/{token}               │
│  ├─ Si válido:                                                             │
│  │   • Mostrar transaction summary (vehículo, dealer, fecha, precio)      │
│  │   • Formulario pre-llenado (buyerId, sellerId, vehicleId, orderId)     │
│  │   • Rating 1-5 estrellas                                                │
│  │   • Rating breakdown (opcional): 4 categorías                           │
│  │   • Content textarea (min 20 chars)                                     │
│  │   • Badge "Compra Verificada" automático ✅                             │
│  │   • Submit → POST /api/reviews                                          │
│  │                                                                          │
│  ├─ Si expirado:                                                           │
│  │   • Mensaje: "Este link expiró hace X días"                             │
│  │   • CTA: "Escribe tu review manualmente" → /dealer/{slug}/reviews       │
│  │                                                                          │
│  ├─ Si ya dejó review:                                                     │
│  │   • Mensaje: "Ya dejaste una review para esta compra"                   │
│  │   • Mostrar review existente                                            │
│  │   • CTA: "Editar review" (si dentro de 24h)                            │
│  │                                                                          │
│  └─ Si token inválido:                                                     │
│      • Mensaje: "Link inválido o no encontrado"                            │
│      • CTA: "Ir al homepage"                                               │
│                                                                             │
│  ✅ SUCCESS: Review Created                                                 │
│  POST /api/reviews                                                          │
│  ├─ IsVerifiedPurchase = true (automático)                                 │
│  ├─ ReviewRequestId = requestId (para tracking)                            │
│  ├─ ReviewRequest.Status = Completed                                       │
│  ├─ Publica ReviewCreatedEvent                                             │
│  └─ Redirect: /review/response/{token}/success                             │
│                                                                             │
│  📬 REMINDERS (Si no responde)                                              │
│  ├─ Reminder 1: Día 14 (7 días después del primer email)                   │
│  ├─ Reminder 2: Día 21 (7 días después del reminder 1)                     │
│  ├─ Reminder 3: Día 28 (último reminder)                                   │
│  └─ Si aún no responde → Status = Expired (día 30)                         │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔌 BACKEND API

### ReviewRequestController Endpoints

```typescript
// filepath: docs/backend/ReviewService-RequestAPI.md

GET    /api/review-requests/validate/{token}     # Validar token y obtener info
POST   /api/review-requests/send                 # Enviar solicitud (internal)
GET    /api/review-requests/buyer/{buyerId}      # Solicitudes del comprador
GET    /api/review-requests/mine                 # Mis solicitudes (auth required)
```

**Ejemplo Request - GET /api/review-requests/validate/{token}:**

```http
GET /api/review-requests/validate/a1b2c3d4-e5f6-7890-abcd-ef1234567890
Authorization: (not required, public endpoint)
```

**Ejemplo Response - Token Válido:**

```json
{
  "isValid": true,
  "status": "Sent",
  "request": {
    "id": "uuid",
    "token": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "buyerId": "buyer-uuid",
    "sellerId": "seller-uuid",
    "vehicleId": "vehicle-uuid",
    "orderId": "order-uuid",
    "sentAt": "2026-01-02T00:00:00Z",
    "expiresAt": "2026-02-01T00:00:00Z",
    "status": "Sent",
    "reminderCount": 0
  },
  "transaction": {
    "vehicleName": "Toyota Corolla 2020",
    "vehicleImage": "https://cdn.okla.com.do/vehicles/...",
    "sellerName": "Auto Elite RD",
    "sellerLogo": "https://cdn.okla.com.do/sellers/...",
    "sellerRating": 4.8,
    "purchaseDate": "2025-12-26T00:00:00Z",
    "totalAmount": 1850000.0
  },
  "alreadyReviewed": false,
  "existingReview": null
}
```

**Ejemplo Response - Ya Dejó Review:**

```json
{
  "isValid": true,
  "status": "Completed",
  "request": {
    /* ... */
  },
  "transaction": {
    /* ... */
  },
  "alreadyReviewed": true,
  "existingReview": {
    "id": "review-uuid",
    "rating": 5,
    "title": "Excelente servicio",
    "content": "Todo perfecto...",
    "createdAt": "2026-01-03T10:00:00Z",
    "canEdit": false // false si pasaron 24h
  }
}
```

**Ejemplo Response - Token Expirado:**

```json
{
  "isValid": false,
  "status": "Expired",
  "message": "Este link expiró el 2026-02-01. Aún puedes dejar una review manualmente.",
  "request": {
    /* info básica */
  },
  "transaction": {
    /* para mostrar contexto */
  },
  "alternativeUrl": "/dealer/auto-elite-rd/reviews"
}
```

**Ejemplo Response - Token Inválido:**

```json
{
  "isValid": false,
  "status": "Invalid",
  "message": "Token no encontrado o inválido.",
  "request": null,
  "transaction": null
}
```

---

## 🔄 FLUJO COMPLETO

### Caso 1: Usuario Responde en 7 Días (Happy Path)

```
1. Día 0: Usuario compra Toyota Corolla en Auto Elite RD
   ├─ BillingService: Order status = Completed
   └─ Publica OrderCompletedEvent

2. Día 7: ReviewService procesa evento
   ├─ Crea ReviewRequest con token único
   ├─ ExpiresAt = Día 37 (30 días desde ahora)
   └─ NotificationService envía email

3. Día 8: Usuario recibe email
   ├─ Subject: "¿Cómo fue tu experiencia con Auto Elite RD?"
   ├─ Click CTA "Escribir Review"
   └─ Abre: https://okla.com.do/review/response/{token}

4. Página carga:
   ├─ GET /api/review-requests/validate/{token}
   ├─ Response: isValid = true, alreadyReviewed = false
   ├─ Muestra transaction summary
   ├─ Formulario pre-llenado
   └─ Usuario completa review (5 estrellas, "Excelente servicio...")

5. Usuario submit:
   ├─ POST /api/reviews
   │   {
   │     "sellerId": "...",
   │     "vehicleId": "...",
   │     "orderId": "...",
   │     "reviewRequestId": "...",
   │     "rating": 5,
   │     "title": "Excelente servicio",
   │     "content": "Todo perfecto, muy recomendado",
   │     "isVerifiedPurchase": true // automático
   │   }
   ├─ Backend: Review creado con status Pending
   ├─ ReviewRequest.Status = Completed
   ├─ Publica ReviewCreatedEvent
   └─ Return 201 Created

6. Success Page:
   ├─ Redirect: /review/response/{token}/success
   ├─ Mensaje: "¡Gracias por tu review!"
   ├─ Badge "✓ Compra Verificada" visible
   └─ CTA: "Ver tu review" → /dealer/auto-elite-rd/reviews

7. Notificaciones:
   ├─ Email al seller: "Nuevo review recibido"
   ├─ Dashboard del seller actualizado
   └─ Badges recalculados (si aplica)
```

---

### Caso 2: Usuario No Responde - Reminders

```
1. Día 7: Email inicial enviado → Status = Sent

2. Día 14: Usuario no respondió
   ├─ Cron job detecta ReviewRequest con Status = Sent, ReminderCount < 3
   ├─ ReminderCount = 1
   ├─ NotificationService envía reminder 1
   └─ Subject: "Aún estamos esperando tu opinión 😊"

3. Día 21: Aún no respondió
   ├─ ReminderCount = 2
   ├─ Envía reminder 2
   └─ Subject: "Última oportunidad para dejar tu review"

4. Día 28: Último reminder
   ├─ ReminderCount = 3
   ├─ Envía reminder 3 (FINAL)
   └─ Subject: "Este es nuestro último recordatorio"

5. Día 37: Expiración
   ├─ Cron job: ReviewRequest.Status = Expired
   ├─ Token ya no válido
   └─ Si usuario intenta acceder: Mensaje "Link expirado"
```

---

### Caso 3: Usuario Ya Dejó Review (Duplicate Prevention)

```
1. Usuario click en link de email

2. GET /api/review-requests/validate/{token}
   ├─ Backend verifica: alreadyReviewed = true
   └─ Response incluye existingReview

3. Página muestra:
   ├─ Mensaje: "Ya dejaste una review para esta compra"
   ├─ Review existente en card read-only
   ├─ Si pasaron <24h: Botón "Editar review"
   └─ Si pasaron >24h: "No puedes editar después de 24 horas"

4. Si click "Editar" (y está dentro de 24h):
   ├─ Redirect: /review/edit/{reviewId}?token={token}
   ├─ Formulario pre-llenado con review actual
   ├─ Submit: PUT /api/reviews/{reviewId}
   └─ Success: "Review actualizada"
```

---

## 🎨 COMPONENTES

### PASO 1: TransactionSummaryCard - Info de la Compra

```typescript
// filepath: src/components/reviews/TransactionSummaryCard.tsx
import { Calendar, DollarSign, MapPin } from "lucide-react";
import { Card } from "@/components/ui/Card";
import { Badge } from "@/components/ui/Badge";
import { formatPrice } from "@/lib/utils";
import { format } from "date-fns";
import { es } from "date-fns/locale";

interface TransactionSummaryCardProps {
  vehicleName: string;
  vehicleImage: string;
  sellerName: string;
  sellerLogo?: string;
  sellerRating: number;
  purchaseDate: string;
  totalAmount: number;
}

export function TransactionSummaryCard({
  vehicleName,
  vehicleImage,
  sellerName,
  sellerLogo,
  sellerRating,
  purchaseDate,
  totalAmount,
}: TransactionSummaryCardProps) {
  return (
    <Card className="overflow-hidden">
      <div className="bg-gradient-to-r from-blue-600 to-purple-600 text-white p-4">
        <div className="flex items-center gap-2">
          <Badge variant="secondary" className="bg-white/20 text-white border-none">
            ✓ Compra Verificada
          </Badge>
        </div>
        <h2 className="text-xl font-bold mt-2">Tu Compra</h2>
      </div>

      <div className="p-6 space-y-4">
        {/* Vehicle */}
        <div className="flex gap-4">
          <img
            src={vehicleImage}
            alt={vehicleName}
            className="w-24 h-16 object-cover rounded-lg"
          />
          <div className="flex-1">
            <h3 className="font-semibold text-gray-900">{vehicleName}</h3>
            <p className="text-sm text-gray-600">Vehículo adquirido</p>
          </div>
        </div>

        {/* Seller */}
        <div className="flex items-center gap-3 py-3 border-t border-b">
          {sellerLogo && (
            <img
              src={sellerLogo}
              alt={sellerName}
              className="w-12 h-12 rounded-full object-cover"
            />
          )}
          <div className="flex-1">
            <p className="font-medium text-gray-900">{sellerName}</p>
            <div className="flex items-center gap-1.5 text-sm">
              <span className="text-yellow-500">★</span>
              <span className="text-gray-600">
                {sellerRating.toFixed(1)} estrellas
              </span>
            </div>
          </div>
        </div>

        {/* Details */}
        <div className="space-y-2 text-sm">
          <div className="flex items-center gap-2 text-gray-600">
            <Calendar size={16} />
            <span>
              Comprado el{" "}
              {format(new Date(purchaseDate), "d 'de' MMMM, yyyy", { locale: es })}
            </span>
          </div>

          <div className="flex items-center gap-2 text-gray-600">
            <DollarSign size={16} />
            <span className="font-semibold text-gray-900">
              {formatPrice(totalAmount)}
            </span>
          </div>
        </div>
      </div>
    </Card>
  );
}
```

---

### PASO 2: ReviewRequestForm - Formulario Simplificado

```typescript
// filepath: src/components/reviews/ReviewRequestForm.tsx
"use client";

import { useState } from "react";
import { Star } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Textarea } from "@/components/ui/Textarea";
import { Input } from "@/components/ui/Input";
import { Label } from "@/components/ui/Label";
import { useCreateReview } from "@/lib/hooks/useReviews";
import { toast } from "sonner";
import type { CreateReviewFromRequestData } from "@/types/review";

interface ReviewRequestFormProps {
  requestId: string;
  sellerId: string;
  vehicleId: string;
  orderId: string;
  onSuccess: (reviewId: string) => void;
}

export function ReviewRequestForm({
  requestId,
  sellerId,
  vehicleId,
  orderId,
  onSuccess,
}: ReviewRequestFormProps) {
  const [rating, setRating] = useState(0);
  const [hoverRating, setHoverRating] = useState(0);
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");

  const createReview = useCreateReview();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (rating === 0) {
      toast.error("Por favor selecciona una calificación");
      return;
    }

    if (content.length < 20) {
      toast.error("La reseña debe tener al menos 20 caracteres");
      return;
    }

    const data: CreateReviewFromRequestData = {
      reviewRequestId: requestId,
      sellerId,
      vehicleId,
      orderId,
      rating,
      title: title || undefined,
      content,
      isVerifiedPurchase: true, // Automático
    };

    try {
      const review = await createReview.mutateAsync(data);
      onSuccess(review.id);
    } catch (error) {
      // Error ya manejado por el hook
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* Rating Stars */}
      <div>
        <Label className="mb-3 block text-lg font-semibold">
          ¿Cómo calificarías tu experiencia? *
        </Label>
        <div className="flex items-center gap-2">
          {[1, 2, 3, 4, 5].map((star) => (
            <button
              key={star}
              type="button"
              className="transition-transform hover:scale-110"
              onMouseEnter={() => setHoverRating(star)}
              onMouseLeave={() => setHoverRating(0)}
              onClick={() => setRating(star)}
            >
              <Star
                size={48}
                className={
                  star <= (hoverRating || rating)
                    ? "fill-yellow-400 text-yellow-400"
                    : "text-gray-300"
                }
              />
            </button>
          ))}
          {rating > 0 && (
            <span className="ml-4 text-lg font-semibold text-gray-900">
              {rating === 5 && "¡Excelente! 🎉"}
              {rating === 4 && "Muy bueno 👍"}
              {rating === 3 && "Bueno 👌"}
              {rating === 2 && "Regular 😐"}
              {rating === 1 && "Malo 😞"}
            </span>
          )}
        </div>
      </div>

      {/* Title (opcional) */}
      <div>
        <Label htmlFor="title">Título de tu review (opcional)</Label>
        <Input
          id="title"
          type="text"
          placeholder="Ej: Excelente atención al cliente"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          maxLength={100}
        />
      </div>

      {/* Content */}
      <div>
        <Label htmlFor="content" required>
          Cuéntanos sobre tu experiencia *
        </Label>
        <Textarea
          id="content"
          rows={6}
          placeholder="¿Qué te gustó? ¿Qué podría mejorar el vendedor? Sé específico para ayudar a otros compradores."
          value={content}
          onChange={(e) => setContent(e.target.value)}
          required
          minLength={20}
          maxLength={2000}
          className="resize-none"
        />
        <p className="text-sm text-gray-500 mt-2">
          {content.length}/2000 caracteres (mínimo 20)
        </p>
      </div>

      {/* Verified Purchase Badge */}
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <div className="flex items-start gap-3">
          <div className="text-2xl">✓</div>
          <div>
            <h4 className="font-semibold text-blue-900 mb-1">
              Compra Verificada
            </h4>
            <p className="text-sm text-blue-700">
              Tu review llevará el badge de "Compra Verificada" porque compraste
              este vehículo en OKLA. Esto le da más credibilidad a tu opinión.
            </p>
          </div>
        </div>
      </div>

      {/* Submit */}
      <Button
        type="submit"
        size="lg"
        className="w-full"
        disabled={createReview.isPending || rating === 0}
      >
        {createReview.isPending ? "Enviando..." : "Publicar Review"}
      </Button>

      <p className="text-xs text-gray-500 text-center">
        Al publicar, aceptas nuestros{" "}
        <a href="/terms" className="text-blue-600 hover:underline">
          Términos y Condiciones
        </a>
        . Las reviews son moderadas antes de publicarse.
      </p>
    </form>
  );
}
```

---

## 📄 PÁGINAS

### PASO 3: ReviewResponsePage - Página Principal

```typescript
// filepath: src/app/(public)/review/response/[token]/page.tsx
import { Metadata } from "next";
import { notFound } from "next/navigation";
import { ReviewResponseClient } from "./ReviewResponseClient";
import { reviewRequestService } from "@/lib/services/reviewRequestService";

interface PageProps {
  params: { token: string };
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  return {
    title: "Deja tu Review | OKLA",
    description: "Comparte tu experiencia de compra",
  };
}

export default async function ReviewResponsePage({ params }: PageProps) {
  // Validate token server-side
  const validation = await reviewRequestService.validateToken(params.token);

  if (!validation.isValid && validation.status === "Invalid") {
    notFound();
  }

  return <ReviewResponseClient token={params.token} initialData={validation} />;
}
```

---

### PASO 4: ReviewResponseClient - Client Component

```typescript
// filepath: src/app/(public)/review/response/[token]/ReviewResponseClient.tsx
"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { AlertCircle, CheckCircle, Clock, XCircle } from "lucide-react";
import { TransactionSummaryCard } from "@/components/reviews/TransactionSummaryCard";
import { ReviewRequestForm } from "@/components/reviews/ReviewRequestForm";
import { ReviewCard } from "@/components/reviews/ReviewCard";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import type { ReviewRequestValidation } from "@/types/review-request";

interface ReviewResponseClientProps {
  token: string;
  initialData: ReviewRequestValidation;
}

export function ReviewResponseClient({
  token,
  initialData,
}: ReviewResponseClientProps) {
  const router = useRouter();
  const [showForm, setShowForm] = useState(!initialData.alreadyReviewed);

  const handleSuccess = (reviewId: string) => {
    router.push(`/review/response/${token}/success?reviewId=${reviewId}`);
  };

  // Estado: Token Expirado
  if (!initialData.isValid && initialData.status === "Expired") {
    return (
      <div className="min-h-screen bg-gray-50 py-12">
        <div className="container max-w-2xl">
          <Card className="p-8 text-center">
            <div className="text-6xl mb-4">⏰</div>
            <h1 className="text-2xl font-bold text-gray-900 mb-2">
              Link Expirado
            </h1>
            <p className="text-gray-600 mb-6">{initialData.message}</p>

            {initialData.transaction && (
              <div className="mb-6">
                <TransactionSummaryCard {...initialData.transaction} />
              </div>
            )}

            <Button
              onClick={() => router.push(initialData.alternativeUrl || "/")}
              variant="primary"
              size="lg"
            >
              Escribir Review Manualmente
            </Button>
          </Card>
        </div>
      </div>
    );
  }

  // Estado: Ya Dejó Review
  if (initialData.alreadyReviewed && initialData.existingReview) {
    const canEdit = initialData.existingReview.canEdit;

    return (
      <div className="min-h-screen bg-gray-50 py-12">
        <div className="container max-w-2xl space-y-6">
          <Card className="p-8 text-center">
            <div className="text-6xl mb-4">✓</div>
            <h1 className="text-2xl font-bold text-gray-900 mb-2">
              Ya Dejaste Tu Review
            </h1>
            <p className="text-gray-600">
              Gracias por compartir tu experiencia. Aquí está tu review:
            </p>
          </Card>

          {/* Existing Review */}
          <ReviewCard review={initialData.existingReview} />

          {/* Edit Button (si <24h) */}
          {canEdit && (
            <Card className="p-6">
              <div className="flex items-start gap-4">
                <Clock className="text-blue-600 flex-shrink-0 mt-1" size={24} />
                <div className="flex-1">
                  <h3 className="font-semibold text-gray-900 mb-1">
                    Puedes Editar Tu Review
                  </h3>
                  <p className="text-sm text-gray-600 mb-4">
                    Tienes 24 horas desde la publicación para hacer cambios.
                  </p>
                  <Button
                    onClick={() =>
                      router.push(
                        `/review/edit/${initialData.existingReview.id}?token=${token}`
                      )
                    }
                    variant="outline"
                  >
                    Editar Review
                  </Button>
                </div>
              </div>
            </Card>
          )}
        </div>
      </div>
    );
  }

  // Estado: Válido - Mostrar Formulario
  return (
    <div className="min-h-screen bg-gray-50 py-12">
      <div className="container max-w-3xl space-y-8">
        {/* Header */}
        <div className="text-center">
          <h1 className="text-3xl font-bold text-gray-900 mb-2">
            ¿Cómo fue tu Experiencia?
          </h1>
          <p className="text-gray-600">
            Tu opinión ayuda a otros compradores a tomar mejores decisiones
          </p>
        </div>

        {/* Transaction Summary */}
        {initialData.transaction && (
          <TransactionSummaryCard {...initialData.transaction} />
        )}

        {/* Review Form */}
        <Card className="p-6">
          <ReviewRequestForm
            requestId={initialData.request.id}
            sellerId={initialData.request.sellerId}
            vehicleId={initialData.request.vehicleId}
            orderId={initialData.request.orderId}
            onSuccess={handleSuccess}
          />
        </Card>

        {/* Trust Indicators */}
        <div className="grid md:grid-cols-3 gap-4 text-center">
          <div className="p-4 bg-white rounded-lg border">
            <div className="text-3xl mb-2">🔒</div>
            <p className="text-sm font-medium text-gray-900">Seguro</p>
            <p className="text-xs text-gray-500">Tu info está protegida</p>
          </div>
          <div className="p-4 bg-white rounded-lg border">
            <div className="text-3xl mb-2">✓</div>
            <p className="text-sm font-medium text-gray-900">Verificado</p>
            <p className="text-xs text-gray-500">Compra confirmada</p>
          </div>
          <div className="p-4 bg-white rounded-lg border">
            <div className="text-3xl mb-2">⚡</div>
            <p className="text-sm font-medium text-gray-900">Rápido</p>
            <p className="text-xs text-gray-500">Solo toma 2 minutos</p>
          </div>
        </div>
      </div>
    </div>
  );
}
```

---

### PASO 5: SuccessPage - Página de Éxito

```typescript
// filepath: src/app/(public)/review/response/[token]/success/page.tsx
import { Metadata } from "next";
import Link from "next/link";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { CheckCircle, Star, Share2 } from "lucide-react";

export const metadata: Metadata = {
  title: "¡Review Publicada! | OKLA",
  description: "Gracias por compartir tu experiencia",
};

interface PageProps {
  params: { token: string };
  searchParams: { reviewId?: string };
}

export default function ReviewSuccessPage({ params, searchParams }: PageProps) {
  return (
    <div className="min-h-screen bg-gradient-to-b from-green-50 to-white py-12">
      <div className="container max-w-2xl">
        <Card className="p-8 text-center">
          {/* Icon */}
          <div className="inline-flex items-center justify-center w-20 h-20 bg-green-100 rounded-full mb-6">
            <CheckCircle size={48} className="text-green-600" />
          </div>

          {/* Title */}
          <h1 className="text-3xl font-bold text-gray-900 mb-3">
            ¡Gracias por tu Review!
          </h1>

          {/* Message */}
          <p className="text-lg text-gray-600 mb-6">
            Tu opinión ha sido enviada y será revisada por nuestro equipo en las
            próximas 24 horas.
          </p>

          {/* Verified Badge */}
          <div className="inline-flex items-center gap-2 px-4 py-2 bg-blue-50 border border-blue-200 rounded-lg mb-8">
            <Star className="text-yellow-500 fill-yellow-500" size={20} />
            <span className="font-semibold text-blue-900">
              Tu review tiene el badge "Compra Verificada"
            </span>
          </div>

          {/* Info Cards */}
          <div className="grid md:grid-cols-2 gap-4 mb-8 text-left">
            <div className="p-4 bg-gray-50 rounded-lg">
              <h3 className="font-semibold text-gray-900 mb-2">
                ¿Cuándo se publicará?
              </h3>
              <p className="text-sm text-gray-600">
                Tu review será revisada y publicada en 24 horas máximo. Te
                notificaremos por email cuando esté visible.
              </p>
            </div>

            <div className="p-4 bg-gray-50 rounded-lg">
              <h3 className="font-semibold text-gray-900 mb-2">
                ¿Puedo editarla?
              </h3>
              <p className="text-sm text-gray-600">
                Tienes 24 horas para hacer cambios. Después de ese tiempo, la
                review quedará fija.
              </p>
            </div>
          </div>

          {/* Actions */}
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            {searchParams.reviewId && (
              <Button variant="outline" asChild>
                <Link href={`/reviews/${searchParams.reviewId}`}>
                  Ver Mi Review
                </Link>
              </Button>
            )}

            <Button variant="primary" asChild>
              <Link href="/">Volver al Inicio</Link>
            </Button>
          </div>

          {/* Social Share (opcional) */}
          <div className="mt-8 pt-8 border-t">
            <p className="text-sm text-gray-600 mb-3">
              ¿Quieres ayudar a más personas?
            </p>
            <Button variant="ghost" size="sm">
              <Share2 size={16} className="mr-2" />
              Compartir OKLA con amigos
            </Button>
          </div>
        </Card>
      </div>
    </div>
  );
}
```

---

## 🪝 HOOKS Y SERVICIOS

### PASO 6: reviewRequestService - API Client

```typescript
// filepath: src/lib/services/reviewRequestService.ts
import { apiClient } from "./apiClient";
import type { ReviewRequestValidation } from "@/types/review-request";

class ReviewRequestService {
  private readonly baseUrl = "/api/review-requests";

  /**
   * Validar token de review request
   */
  async validateToken(token: string): Promise<ReviewRequestValidation> {
    const { data } = await apiClient.get<ReviewRequestValidation>(
      `${this.baseUrl}/validate/${token}`,
    );
    return data;
  }

  /**
   * Enviar review request (internal - solo backend)
   */
  async sendRequest(data: {
    buyerId: string;
    sellerId: string;
    vehicleId: string;
    orderId: string;
  }): Promise<{ requestId: string; token: string }> {
    const { data: response } = await apiClient.post(
      `${this.baseUrl}/send`,
      data,
    );
    return response;
  }

  /**
   * Obtener mis review requests (requiere auth)
   */
  async getMyRequests(): Promise<ReviewRequest[]> {
    const { data } = await apiClient.get(`${this.baseUrl}/mine`);
    return data;
  }
}

export const reviewRequestService = new ReviewRequestService();
```

---

## 📦 TIPOS TYPESCRIPT

### PASO 7: ReviewRequest Types

```typescript
// filepath: src/types/review-request.ts

export enum ReviewRequestStatus {
  Sent = "Sent",
  Viewed = "Viewed",
  Completed = "Completed",
  Expired = "Expired",
  Declined = "Declined",
  Cancelled = "Cancelled",
}

export interface ReviewRequest {
  id: string;
  token: string;
  buyerId: string;
  sellerId: string;
  vehicleId: string;
  orderId: string;
  sentAt: string;
  expiresAt: string;
  status: ReviewRequestStatus;
  reminderCount: number;
  lastReminderAt?: string;
}

export interface ReviewRequestValidation {
  isValid: boolean;
  status: ReviewRequestStatus | "Invalid";
  message?: string;
  request: ReviewRequest | null;
  transaction: {
    vehicleName: string;
    vehicleImage: string;
    sellerName: string;
    sellerLogo?: string;
    sellerRating: number;
    purchaseDate: string;
    totalAmount: number;
  } | null;
  alreadyReviewed: boolean;
  existingReview: {
    id: string;
    rating: number;
    title?: string;
    content: string;
    createdAt: string;
    canEdit: boolean;
  } | null;
  alternativeUrl?: string;
}

export interface CreateReviewFromRequestData {
  reviewRequestId: string;
  sellerId: string;
  vehicleId: string;
  orderId: string;
  rating: number;
  title?: string;
  content: string;
  isVerifiedPurchase: true; // Siempre true cuando viene de request
}
```

---

## 📧 EMAIL TEMPLATES

### Email Inicial (Día 7)

```html
<!-- filepath: emails/review-request-initial.html -->
<!DOCTYPE html>
<html>
  <head>
    <meta charset="UTF-8" />
    <title>¿Cómo fue tu experiencia?</title>
  </head>
  <body
    style="font-family: Arial, sans-serif; background-color: #f3f4f6; padding: 20px;"
  >
    <div
      style="max-width: 600px; margin: 0 auto; background-color: white; border-radius: 8px; overflow: hidden;"
    >
      <!-- Header -->
      <div
        style="background: linear-gradient(to right, #3b82f6, #8b5cf6); padding: 30px; text-align: center;"
      >
        <h1 style="color: white; margin: 0; font-size: 28px;">
          ¿Cómo fue tu experiencia?
        </h1>
      </div>

      <!-- Body -->
      <div style="padding: 40px 30px;">
        <p style="font-size: 16px; color: #374151; margin-bottom: 20px;">
          Hola <strong>{{buyerName}}</strong>,
        </p>

        <p style="font-size: 16px; color: #374151; line-height: 1.6;">
          Hace 7 días compraste un <strong>{{vehicleName}}</strong> a
          <strong>{{sellerName}}</strong>.
        </p>

        <p style="font-size: 16px; color: #374151; line-height: 1.6;">
          Nos encantaría conocer tu opinión para ayudar a otros compradores a
          tomar mejores decisiones.
        </p>

        <!-- Vehicle Card -->
        <div
          style="background-color: #f9fafb; border: 1px solid #e5e7eb; border-radius: 8px; padding: 20px; margin: 30px 0;"
        >
          <img
            src="{{vehicleImage}}"
            alt="{{vehicleName}}"
            style="width: 100%; border-radius: 8px; margin-bottom: 15px;"
          />
          <h3 style="margin: 0 0 10px 0; color: #111827;">{{vehicleName}}</h3>
          <p style="margin: 0; color: #6b7280; font-size: 14px;">
            Comprado el {{purchaseDate}} • {{totalAmount}}
          </p>
        </div>

        <!-- CTA Button -->
        <div style="text-align: center; margin: 40px 0;">
          <a
            href="{{reviewUrl}}"
            style="display: inline-block; background-color: #3b82f6; color: white; padding: 16px 40px; text-decoration: none; border-radius: 8px; font-size: 18px; font-weight: bold;"
          >
            Deja tu Review
          </a>
        </div>

        <!-- Benefits -->
        <div
          style="background-color: #eff6ff; border-left: 4px solid #3b82f6; padding: 15px; margin: 30px 0;"
        >
          <p style="margin: 0; color: #1e40af; font-weight: bold;">
            ✓ Tu review llevará el badge "Compra Verificada"
          </p>
          <p style="margin: 8px 0 0 0; color: #1e40af; font-size: 14px;">
            Solo toma 2 minutos y ayudas a la comunidad
          </p>
        </div>

        <p style="font-size: 14px; color: #6b7280; margin-top: 30px;">
          Este link expira en 30 días. Si no quieres dejar una review,
          simplemente ignora este email.
        </p>
      </div>

      <!-- Footer -->
      <div
        style="background-color: #f9fafb; padding: 20px 30px; text-align: center; border-top: 1px solid #e5e7eb;"
      >
        <p style="margin: 0; color: #6b7280; font-size: 12px;">
          © 2026 OKLA. Todos los derechos reservados.
        </p>
        <p style="margin: 10px 0 0 0; color: #6b7280; font-size: 12px;">
          <a
            href="{{unsubscribeUrl}}"
            style="color: #3b82f6; text-decoration: none;"
            >Cancelar suscripción</a
          >
        </p>
      </div>
    </div>
  </body>
</html>
```

---

### Email Reminder (Día 14, 21, 28)

```html
<!-- filepath: emails/review-request-reminder.html -->
<!-- Similar structure pero con headlines diferentes: -->

<!-- Reminder 1 (Día 14): -->
<h1 style="color: white;">Aún estamos esperando tu opinión 😊</h1>

<!-- Reminder 2 (Día 21): -->
<h1 style="color: white;">Última oportunidad para dejar tu review</h1>

<!-- Reminder 3 (Día 28 - FINAL): -->
<h1 style="color: white;">Este es nuestro último recordatorio</h1>
<p>Este link expira en 2 días.</p>
```

---

## ✅ CHECKLIST DE IMPLEMENTACIÓN

### Componentes

- [ ] `TransactionSummaryCard` component
- [ ] `ReviewRequestForm` component (simplified)

### Páginas

- [ ] `/review/response/[token]` - Main page
- [ ] `/review/response/[token]/ReviewResponseClient` - Client component
- [ ] `/review/response/[token]/success` - Success page

### Servicios

- [ ] `reviewRequestService` - API client
- [ ] Token validation logic
- [ ] Create review from request endpoint integration

### Tipos

- [ ] `review-request.ts` types
- [ ] `ReviewRequestValidation` interface
- [ ] `CreateReviewFromRequestData` interface

### Email Templates

- [ ] `review-request-initial.html` - Email inicial
- [ ] `review-request-reminder.html` - Reminders

### Backend Integration

- [ ] GET `/api/review-requests/validate/{token}` endpoint
- [ ] POST `/api/reviews` acepta `reviewRequestId`
- [ ] Update `ReviewRequest.Status` cuando review creado
- [ ] Cron job para enviar reminders (días 14, 21, 28)
- [ ] Cron job para marcar expired (día 30)

---

## 🎯 MÉTRICAS DE ÉXITO

### Conversión

- **Target:** 30%+ de review requests resultan en reviews
- **Medir:** (Total reviews / Total requests sent) × 100

### Tiempo de Respuesta

- **Target:** 70%+ responden en primeros 7 días (antes del reminder 1)
- **Medir:** Promedio de días desde envío hasta review creado

### Quality

- **Target:** 80%+ de reviews desde requests tienen >50 caracteres
- **Target:** 90%+ de reviews desde requests son aprobadas

### Reminders Effectiveness

- **Reminder 1 (Día 14):** +10% conversion
- **Reminder 2 (Día 21):** +5% conversion
- **Reminder 3 (Día 28):** +3% conversion
- **Total reminders:** +18% conversion vs no reminders

---

**✅ Review Request Response Page COMPLETO**

_Los compradores ahora pueden dejar reviews fácilmente desde emails automáticos, con verified purchase badge garantizado._

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/reviews-reputacion.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsUser } from "../helpers/auth";

test.describe("Reviews y Reputación", () => {
  test("debe mostrar reviews en perfil de vendedor", async ({ page }) => {
    await page.goto("/vendedor/juan-perez");

    await expect(page.getByTestId("seller-rating")).toBeVisible();
    await expect(page.getByTestId("reviews-list")).toBeVisible();
  });

  test.describe("Dejar Review", () => {
    test.beforeEach(async ({ page }) => {
      await loginAsUser(page);
    });

    test("debe mostrar formulario de review después de compra", async ({
      page,
    }) => {
      await page.goto("/review/crear?transaction=txn123");

      await expect(
        page.getByRole("heading", { name: /tu experiencia/i }),
      ).toBeVisible();
      await expect(page.getByTestId("star-rating")).toBeVisible();
    });

    test("debe enviar review con estrellas y comentario", async ({ page }) => {
      await page.goto("/review/crear?transaction=txn123");

      await page.getByTestId("star-5").click();
      await page.fill(
        'textarea[name="comment"]',
        "Excelente vendedor, muy profesional",
      );
      await page.click('button[type="submit"]');

      await expect(page.getByText(/gracias por tu review/i)).toBeVisible();
    });

    test("debe mostrar badge 'Verified Purchase'", async ({ page }) => {
      await page.goto("/vendedor/juan-perez");

      await expect(page.getByTestId("verified-badge")).toBeVisible();
    });
  });

  test("debe filtrar reviews por rating", async ({ page }) => {
    await page.goto("/vendedor/juan-perez");

    await page.getByRole("button", { name: /5 estrellas/i }).click();
    await expect(page.getByTestId("review-item")).toHaveCount({ min: 0 });
  });
});
```

---

_Última actualización: Enero 9, 2026_  
_Archivo: 98-review-request-response-completo.md_  
_Sprint: Módulo 07 - Reviews y Reputación_

---

# ANEXO: Review Request Response Page

> Fusionado desde 12-review-request-response.md
