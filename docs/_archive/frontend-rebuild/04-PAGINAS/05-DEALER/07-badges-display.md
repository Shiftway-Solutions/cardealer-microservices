---
title: "35. Sistema de Badges de Vendedores - Completo"
priority: P1
estimated_time: "2 horas"
dependencies: []
apis: ["DealerManagementService", "NotificationService"]
status: complete
last_updated: "2026-01-30"
---

# 35. Sistema de Badges de Vendedores - Completo

> **Objetivo:** Implementar sistema completo de visualización y gestión de badges de reputación para sellers/dealers basado en ReviewService.  
> **Tiempo estimado:** 2 horas  
> **Prioridad:** P1 (Crítico - Diferenciador competitivo)  
> **Complejidad:** 🟡 Media (Badge display, progress tracking, API integration)  
> **Dependencias:** ReviewService (BadgesController), DealerManagementService, SellerProfiles  
> **Puerto Backend:** 5091 (ReviewService)  
> **Estado:** ✅ Backend 100% | ✅ UI 100%  
> **Última actualización:** Enero 29, 2026

---

## ✅ INTEGRACIÓN CON REVIEWSERVICE (process-matrix/21-REVIEWS-REPUTACION)

### Servicios Involucrados

| Servicio                    | Puerto | Estado  | Responsabilidad             |
| --------------------------- | ------ | ------- | --------------------------- |
| **ReviewService**           | 5091   | ✅ 90%  | Cálculo y gestión de badges |
| **DealerManagementService** | 5039   | ✅ 100% | Perfiles de dealers         |
| **NotificationService**     | 5006   | ✅ 100% | Notificar badges ganados    |

Este documento complementa el sistema de reviews documentado en:

- [20-reviews-reputacion.md](../03-COMPRADOR/06-reviews-reputacion.md) - Sistema completo de reviews
- [process-matrix/21-REVIEWS-REPUTACION/01-dealer-reviews.md](../../process-matrix/21-REVIEWS-REPUTACION/01-dealer-reviews.md) - **Procesos detallados de backend** ⭐

**Relación con Procesos:**

| Proceso ReviewService | Código     | Documento            | Descripción                           |
| --------------------- | ---------- | -------------------- | ------------------------------------- |
| Comprador Deja Review | REVIEW-001 | 01-dealer-reviews.md | Trigger para recálculo de badges      |
| Dealer Responde       | REVIEW-002 | 01-dealer-reviews.md | Afecta badge "Responsive"             |
| Moderación            | REVIEW-003 | 01-dealer-reviews.md | Reviews aprobadas cuentan para badges |
| Recálculo de Badges   | REV-BADGE  | Este documento       | Automático post-review                |

### Tipos de Badges (desde process-matrix)

| Badge                 | Criterio                     | Icono |
| --------------------- | ---------------------------- | ----- |
| **Top Rated**         | Rating ≥ 4.5 + 20+ reviews   | ⭐    |
| **Trusted Dealer**    | 50+ compras verificadas      | ✓     |
| **Responsive**        | Response rate ≥ 90%          | 💬    |
| **Rising Star**       | Rating increase > 0.5 en 30d | 📈    |
| **Excellent Service** | Customer Service ≥ 4.8       | 🏆    |

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura del Sistema](#arquitectura-del-sistema)
2. [Backend API](#backend-api)
3. [Tipos de Badges](#tipos-de-badges)
4. [Componentes de Display](#componentes-de-display)
5. [Páginas](#páginas)
6. [Hooks y Servicios](#hooks-y-servicios)
7. [Tipos TypeScript](#tipos-typescript)
8. [Integración](#integración)

---

## 🏗️ ARQUITECTURA DEL SISTEMA

### Badge System Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          BADGE SYSTEM FLOW                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  📊 DATOS DE ENTRADA (ReviewService)                                        │
│  ├─ Promedio de rating (AVG de reviews)                                    │
│  ├─ Total de reviews                                                       │
│  ├─ Reviews recientes (últimos 6 meses)                                     │
│  ├─ Tiempo de respuesta promedio                                           │
│  ├─ % de reviews positivas (4-5 estrellas)                                 │
│  ├─ Keywords en reviews ("recomendado", "excelente", etc.)                  │
│  ├─ Helpful votes (engagement)                                             │
│  └─ Seller age (tiempo desde primera venta)                                │
│                                                                             │
│  ⚙️ CÁLCULO AUTOMÁTICO (Backend)                                            │
│  POST /api/badges/seller/{sellerId}/recalculate                             │
│  ├─ Evalúa criterios de 10 tipos de badges                                 │
│  ├─ Otorga badges que califican                                            │
│  ├─ Revoca badges que ya no califican                                      │
│  ├─ Publica eventos: BadgeEarnedEvent, BadgeRevokedEvent                   │
│  └─ Actualiza seller.Badges en DB                                          │
│                                                                             │
│  📦 RECALCULACIÓN AUTOMÁTICA                                                │
│  ├─ Trigger: Después de cada nuevo review                                  │
│  ├─ Trigger: Cron job mensual (día 1)                                      │
│  └─ Trigger: Manual por admin (si necesario)                               │
│                                                                             │
│  🎨 FRONTEND DISPLAY                                                        │
│  ├─ GET /api/badges/seller/{sellerId}  → Lista de badges activos           │
│  ├─ BadgesList component → Display visual de badges                        │
│  ├─ BadgeCard → Card individual con criterios                              │
│  ├─ BadgeTooltip → Hover info con descripción                              │
│  └─ BadgeProgress → Progress bar hacia badge bloqueado                     │
│                                                                             │
│  📍 UBICACIONES DE DISPLAY                                                  │
│  ├─ Seller Public Profile (/sellers/{username})                            │
│  ├─ Dealer Profile (/dealer/{slug})                                        │
│  ├─ Vehicle Listing (seller badges en sidebar)                             │
│  ├─ Search Results (badge count en card)                                   │
│  ├─ My Profile Settings (/settings/badges) ← PRINCIPAL                     │
│  └─ Admin Seller Management (/admin/sellers/{id})                          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔌 BACKEND API

### BadgesController Endpoints (ReviewService:5030)

**Base URL:** `http://localhost:5030` (desarrollo) | `https://api.okla.com.do/api/reviews` (producción)

| Método | Endpoint                                            | Descripción                   | Auth | Roles   |
| ------ | --------------------------------------------------- | ----------------------------- | ---- | ------- |
| `GET`  | `/api/reviews/seller/{sellerId}/badges`             | Badges activos del vendedor   | ❌   | Público |
| `GET`  | `/api/reviews/seller/{sellerId}/badges/history`     | Historial completo de badges  | ✅   | Admin   |
| `POST` | `/api/reviews/seller/{sellerId}/badges/recalculate` | Recalcular badges manualmente | ✅   | Admin   |
| `GET`  | `/api/reviews/seller/{sellerId}/badges/progress`    | Progreso hacia badges         | ✅   | Seller  |

**🔗 Ver procesos detallados:**

- [PROCESO 4: Recálculo Automático de Badges](../../process-matrix/07-REVIEWS-REPUTACION/01-review-service.md#proceso-4-recálculo-automático-de-badges)
- [PROCESO 7: Historial de Badges (Admin)](../../process-matrix/07-REVIEWS-REPUTACION/01-review-service.md#proceso-7-historial-de-badges-admin)

**Ejemplo Response - GET /api/reviews/seller/{sellerId}/badges:**

```json
{
  "sellerId": "uuid",
  "sellerName": "Auto Elite RD",
  "badges": [
    {
      "id": "uuid",
      "badgeType": "TopRated",
      "name": "Top Rated",
      "description": "Promedio 4.8+ estrellas con 10+ reviews",
      "icon": "⭐",
      "color": "gold",
      "earnedAt": "2026-01-01T00:00:00Z",
      "criteria": {
        "minRating": 4.8,
        "minReviews": 10,
        "currentRating": 4.9,
        "currentReviews": 25
      }
    },
    {
      "id": "uuid",
      "badgeType": "QuickResponder",
      "name": "Respuesta Rápida",
      "description": "Responde en menos de 24 horas",
      "icon": "⚡",
      "color": "blue",
      "earnedAt": "2025-12-15T00:00:00Z",
      "criteria": {
        "maxResponseTime": 24,
        "currentResponseTime": 12
      }
    }
  ],
  "totalBadges": 2,
  "availableBadges": 10
}
```

**Ejemplo Response - GET /api/badges/seller/{sellerId}/progress:**

```json
{
  "sellerId": "uuid",
  "progress": [
    {
      "badgeType": "VolumeLeader",
      "name": "Líder en Volumen",
      "description": "50+ reviews en total",
      "icon": "📈",
      "color": "purple",
      "earned": false,
      "criteria": {
        "target": 50,
        "current": 35,
        "unit": "reviews",
        "percentage": 70
      },
      "tips": "Necesitas 15 reviews más. Pide a tus clientes que dejen una review después de cada compra."
    },
    {
      "badgeType": "ConsistencyWinner",
      "name": "Consistencia Ganadora",
      "description": "Rating estable ±0.2 por 6+ meses",
      "icon": "🎯",
      "color": "green",
      "earned": false,
      "criteria": {
        "monthsRequired": 6,
        "currentMonths": 3,
        "ratingVariance": 0.15,
        "maxVariance": 0.2,
        "percentage": 50
      },
      "tips": "Mantén tu calidad de servicio. Llevas 3 meses con rating estable."
    }
  ]
}
```

---

## 🏅 TIPOS DE BADGES

### 10 Badges del Sistema

| #   | Badge Type               | Nombre                   | Icon | Color  | Criterios                           |
| --- | ------------------------ | ------------------------ | ---- | ------ | ----------------------------------- |
| 1   | **TopRated**             | Top Rated                | ⭐   | Gold   | 4.8+ rating, 10+ reviews            |
| 2   | **TrustedDealer**        | Dealer Confiable         | 🛡️   | Blue   | 6+ meses activo, 95%+ positivas     |
| 3   | **FiveStarSeller**       | 5 Estrellas              | 🌟   | Gold   | 100% reviews de 5 estrellas (min 5) |
| 4   | **CustomerChoice**       | Elección del Cliente     | 💎   | Purple | 80%+ mencionan "recomendado"        |
| 5   | **QuickResponder**       | Respuesta Rápida         | ⚡   | Blue   | <24h tiempo respuesta promedio      |
| 6   | **VerifiedProfessional** | Profesional Verificado   | ✓    | Green  | Docs verificados + 4+ stars         |
| 7   | **RisingStar**           | Estrella en Ascenso      | 🚀   | Orange | Rating mejoró en últimos 3 meses    |
| 8   | **VolumeLeader**         | Líder en Volumen         | 📈   | Purple | 50+ reviews totales                 |
| 9   | **ConsistencyWinner**    | Consistencia Ganadora    | 🎯   | Green  | Rating estable ±0.2 por 6+ meses    |
| 10  | **CommunityFavorite**    | Favorito de la Comunidad | ❤️   | Red    | Top 10% reviews más útiles          |

### Detalles de Cada Badge

#### 1. TopRated ⭐

**Criterios:**

- Promedio de rating: >= 4.8
- Mínimo de reviews: >= 10
- Período: Últimos 6 meses

**Cálculo:**

```sql
SELECT AVG(Rating) as avgRating, COUNT(*) as totalReviews
FROM Reviews
WHERE SellerId = @sellerId
  AND IsApproved = true
  AND CreatedAt >= DATEADD(month, -6, GETDATE())
HAVING avgRating >= 4.8 AND totalReviews >= 10
```

**Descripción para usuario:**

> "Los sellers Top Rated mantienen un promedio de 4.8+ estrellas con al menos 10 reviews. Esto representa el top 5% de dealers en OKLA."

---

#### 2. TrustedDealer 🛡️

**Criterios:**

- Tiempo activo: >= 6 meses
- % de reviews positivas (4-5 estrellas): >= 95%
- Mínimo de reviews: >= 20

**Cálculo:**

```sql
SELECT
  DATEDIFF(month, seller.CreatedAt, GETDATE()) as monthsActive,
  (COUNT(CASE WHEN Rating >= 4 THEN 1 END) * 100.0 / COUNT(*)) as positivePercent
FROM Reviews r
JOIN Sellers s ON r.SellerId = s.Id
WHERE r.SellerId = @sellerId AND r.IsApproved = true
HAVING monthsActive >= 6
  AND positivePercent >= 95
  AND COUNT(*) >= 20
```

**Descripción:**

> "Los Dealers Confiables tienen 6+ meses de experiencia con 95%+ de reviews positivas. Son sellers establecidos con historial comprobado."

---

#### 3. FiveStarSeller 🌟

**Criterios:**

- 100% de reviews con 5 estrellas
- Mínimo de reviews: >= 5

**Cálculo:**

```sql
SELECT COUNT(*) as fiveStarCount,
       COUNT(CASE WHEN Rating < 5 THEN 1 END) as nonFiveStarCount
FROM Reviews
WHERE SellerId = @sellerId AND IsApproved = true
HAVING nonFiveStarCount = 0 AND fiveStarCount >= 5
```

**Descripción:**

> "Sellers con servicio perfecto: 100% de sus clientes los calificaron con 5 estrellas. Excelencia garantizada."

---

#### 4. CustomerChoice 💎

**Criterios:**

- > = 80% de reviews mencionan keyword "recomendado" o sinónimos
- Análisis de texto en Content de reviews
- Mínimo de reviews: >= 10

**Cálculo (NLP):**

```csharp
var keywords = new[] { "recomendado", "recomiendo", "recomendar", "excelente", "confiable" };
var reviewsWithKeywords = reviews
    .Where(r => keywords.Any(k => r.Content.ToLower().Contains(k)))
    .Count();
var percentage = (reviewsWithKeywords * 100.0) / reviews.Count;
return percentage >= 80 && reviews.Count >= 10;
```

**Descripción:**

> "Los clientes activamente recomiendan este seller. 80%+ de reviews incluyen palabras como 'recomendado' o 'excelente'."

---

#### 5. QuickResponder ⚡

**Criterios:**

- Tiempo de respuesta promedio: < 24 horas
- Basado en seller responses a reviews
- Mínimo de responses: >= 5

**Cálculo:**

```sql
SELECT AVG(DATEDIFF(hour, r.CreatedAt, rr.ResponseDate)) as avgResponseHours
FROM Reviews r
JOIN ReviewResponses rr ON r.Id = rr.ReviewId
WHERE r.SellerId = @sellerId
HAVING avgResponseHours < 24 AND COUNT(*) >= 5
```

**Descripción:**

> "Responde a sus clientes en menos de 24 horas. Servicio al cliente excepcional."

---

#### 6. VerifiedProfessional ✓

**Criterios:**

- Documentos KYC verificados (DealerManagementService)
- Rating: >= 4.0
- Mínimo de reviews: >= 5

**Cálculo (multi-service):**

```csharp
var dealer = await dealerService.GetByIdAsync(sellerId);
var isVerified = dealer.VerificationStatus == VerificationStatus.Verified;
var stats = await reviewService.GetSellerStatsAsync(sellerId);
return isVerified && stats.AverageRating >= 4.0 && stats.TotalReviews >= 5;
```

**Descripción:**

> "Seller verificado con documentación completa y rating sólido. Identidad y negocio comprobados."

---

#### 7. RisingStar 🚀

**Criterios:**

- Rating actual > Rating de hace 3 meses
- Mejora mínima: +0.3 estrellas
- Mínimo de reviews en cada período: >= 5

**Cálculo:**

```sql
DECLARE @currentRating DECIMAL = (
  SELECT AVG(Rating) FROM Reviews
  WHERE SellerId = @sellerId AND CreatedAt >= DATEADD(month, -1, GETDATE())
);
DECLARE @oldRating DECIMAL = (
  SELECT AVG(Rating) FROM Reviews
  WHERE SellerId = @sellerId
    AND CreatedAt BETWEEN DATEADD(month, -4, GETDATE()) AND DATEADD(month, -3, GETDATE())
);
SELECT CASE WHEN @currentRating - @oldRating >= 0.3 THEN 1 ELSE 0 END as qualified;
```

**Descripción:**

> "Este seller está mejorando continuamente. Su rating subió +0.3 en los últimos 3 meses."

---

#### 8. VolumeLeader 📈

**Criterios:**

- Total de reviews: >= 50
- Cualquier período (all-time)

**Cálculo:**

```sql
SELECT COUNT(*) as totalReviews
FROM Reviews
WHERE SellerId = @sellerId AND IsApproved = true
HAVING totalReviews >= 50
```

**Descripción:**

> "Líder en volumen de transacciones con 50+ reviews. Experiencia comprobada en ventas."

---

#### 9. ConsistencyWinner 🎯

**Criterios:**

- Rating estable (varianza <= 0.2) por 6+ meses
- Mínimo de reviews por mes: >= 2

**Cálculo (estadístico):**

```csharp
var monthlyRatings = GetMonthlyAverageRatings(sellerId, months: 6);
if (monthlyRatings.Count < 6) return false;

var maxRating = monthlyRatings.Max();
var minRating = monthlyRatings.Min();
var variance = maxRating - minRating;

return variance <= 0.2 && monthlyRatings.All(m => m.ReviewCount >= 2);
```

**Descripción:**

> "Consistencia excepcional: rating estable por 6+ meses. Calidad garantizada en cada venta."

---

#### 10. CommunityFavorite ❤️

**Criterios:**

- Estar en top 10% de sellers por helpful votes
- Basado en (HelpfulVotes / TotalReviews) ratio
- Mínimo de reviews: >= 10

**Cálculo (ranking):**

```sql
WITH SellerRatios AS (
  SELECT
    SellerId,
    SUM(HelpfulVotes) * 1.0 / COUNT(*) as helpfulRatio,
    COUNT(*) as totalReviews,
    PERCENT_RANK() OVER (ORDER BY SUM(HelpfulVotes) * 1.0 / COUNT(*)) as percentile
  FROM Reviews
  WHERE IsApproved = true
  GROUP BY SellerId
  HAVING COUNT(*) >= 10
)
SELECT * FROM SellerRatios
WHERE percentile >= 0.90 -- Top 10%
  AND SellerId = @sellerId
```

**Descripción:**

> "La comunidad ama a este seller. Sus reviews son las más útiles en OKLA (top 10%)."

---

## 🎨 COMPONENTES DE DISPLAY

### PASO 1: BadgesList - Lista de Badges

```typescript
// filepath: src/components/badges/BadgesList.tsx
"use client";

import { useState } from "react";
import { useBadges } from "@/lib/hooks/useBadges";
import { BadgeCard } from "./BadgeCard";
import { Button } from "@/components/ui/Button";
import { Award, Lock } from "lucide-react";
import { Skeleton } from "@/components/ui/Skeleton";

interface BadgesListProps {
  sellerId: string;
  showProgress?: boolean; // Mostrar badges bloqueados
  variant?: "grid" | "inline"; // Grid completo o inline compacto
}

export function BadgesList({
  sellerId,
  showProgress = false,
  variant = "grid",
}: BadgesListProps) {
  const { data, isLoading, error } = useBadges(sellerId, showProgress);
  const [filter, setFilter] = useState<"all" | "earned" | "locked">("all");

  if (isLoading) {
    return (
      <div className={variant === "grid" ? "grid md:grid-cols-2 lg:grid-cols-3 gap-4" : "flex gap-2"}>
        {[1, 2, 3].map((i) => (
          <Skeleton key={i} className="h-32 rounded-lg" />
        ))}
      </div>
    );
  }

  if (error) {
    return (
      <div className="text-center py-8">
        <p className="text-red-600">Error al cargar badges</p>
      </div>
    );
  }

  const { badges, progress } = data;
  const earnedBadges = badges || [];
  const lockedBadges = progress || [];

  const filteredEarned = filter === "earned" || filter === "all" ? earnedBadges : [];
  const filteredLocked = filter === "locked" || filter === "all" ? lockedBadges : [];

  // Inline variant (para usar en perfiles)
  if (variant === "inline") {
    return (
      <div className="flex flex-wrap gap-2">
        {earnedBadges.slice(0, 5).map((badge) => (
          <div
            key={badge.id}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-white border rounded-full text-sm font-medium shadow-sm"
            title={badge.description}
          >
            <span className="text-lg">{badge.icon}</span>
            <span className="text-gray-900">{badge.name}</span>
          </div>
        ))}
        {earnedBadges.length > 5 && (
          <div className="inline-flex items-center px-3 py-1.5 bg-gray-100 border rounded-full text-sm font-medium text-gray-600">
            +{earnedBadges.length - 5} más
          </div>
        )}
      </div>
    );
  }

  // Grid variant (para página completa)
  return (
    <div className="space-y-6">
      {/* Filters */}
      {showProgress && (
        <div className="flex items-center gap-3">
          <Button
            variant={filter === "all" ? "primary" : "ghost"}
            size="sm"
            onClick={() => setFilter("all")}
          >
            Todos ({earnedBadges.length + lockedBadges.length})
          </Button>
          <Button
            variant={filter === "earned" ? "primary" : "ghost"}
            size="sm"
            onClick={() => setFilter("earned")}
          >
            <Award size={16} className="mr-1" />
            Obtenidos ({earnedBadges.length})
          </Button>
          <Button
            variant={filter === "locked" ? "primary" : "ghost"}
            size="sm"
            onClick={() => setFilter("locked")}
          >
            <Lock size={16} className="mr-1" />
            En Progreso ({lockedBadges.length})
          </Button>
        </div>
      )}

      {/* Earned Badges */}
      {filteredEarned.length > 0 && (
        <div>
          <h3 className="text-lg font-semibold text-gray-900 mb-4">
            Badges Obtenidos
          </h3>
          <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-4">
            {filteredEarned.map((badge) => (
              <BadgeCard key={badge.id} badge={badge} earned={true} />
            ))}
          </div>
        </div>
      )}

      {/* Locked Badges (Progress) */}
      {showProgress && filteredLocked.length > 0 && (
        <div>
          <h3 className="text-lg font-semibold text-gray-900 mb-4">
            En Progreso
          </h3>
          <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-4">
            {filteredLocked.map((item) => (
              <BadgeCard key={item.badgeType} badge={item} earned={false} />
            ))}
          </div>
        </div>
      )}

      {/* Empty States */}
      {filteredEarned.length === 0 && filteredLocked.length === 0 && (
        <div className="text-center py-12">
          <div className="text-6xl mb-4">🏆</div>
          <h3 className="text-xl font-semibold text-gray-900 mb-2">
            {filter === "earned" ? "Sin badges aún" : "Nada que mostrar"}
          </h3>
          <p className="text-gray-600">
            {filter === "earned"
              ? "Sigue vendiendo y mejorando para ganar tus primeros badges"
              : "Cambia los filtros para ver más badges"}
          </p>
        </div>
      )}
    </div>
  );
}
```

---

### PASO 2: BadgeCard - Card Individual de Badge

```typescript
// filepath: src/components/badges/BadgeCard.tsx
"use client";

import { CheckCircle, Lock, TrendingUp } from "lucide-react";
import { Badge } from "@/components/ui/Badge";
import { Card } from "@/components/ui/Card";
import { BadgeProgress } from "./BadgeProgress";
import { BadgeTooltip } from "./BadgeTooltip";
import { formatDistanceToNow } from "date-fns";
import { es } from "date-fns/locale";
import type { SellerBadge, BadgeProgressItem } from "@/types/badge";

interface BadgeCardProps {
  badge: SellerBadge | BadgeProgressItem;
  earned: boolean;
}

export function BadgeCard({ badge, earned }: BadgeCardProps) {
  // Color mapping
  const colorMap = {
    gold: "from-yellow-400 to-yellow-600",
    blue: "from-blue-400 to-blue-600",
    purple: "from-purple-400 to-purple-600",
    green: "from-green-400 to-green-600",
    orange: "from-orange-400 to-orange-600",
    red: "from-red-400 to-red-600",
  };

  const bgGradient = colorMap[badge.color] || colorMap.blue;

  if (earned) {
    const earnedBadge = badge as SellerBadge;

    return (
      <Card className="relative overflow-hidden hover:shadow-lg transition-shadow">
        {/* Gradient Background */}
        <div
          className={`absolute top-0 left-0 right-0 h-2 bg-gradient-to-r ${bgGradient}`}
        />

        {/* Earned Indicator */}
        <div className="absolute top-3 right-3">
          <CheckCircle size={24} className="text-green-600" />
        </div>

        <div className="p-6">
          {/* Icon */}
          <div className="text-5xl mb-3">{earnedBadge.icon}</div>

          {/* Name */}
          <h3 className="text-lg font-bold text-gray-900 mb-2">
            {earnedBadge.name}
          </h3>

          {/* Description */}
          <p className="text-sm text-gray-600 mb-4">
            {earnedBadge.description}
          </p>

          {/* Earned Date */}
          <p className="text-xs text-gray-500">
            Obtenido{" "}
            {formatDistanceToNow(new Date(earnedBadge.earnedAt), {
              addSuffix: true,
              locale: es,
            })}
          </p>

          {/* Tooltip (hover) */}
          <BadgeTooltip badge={earnedBadge} />
        </div>
      </Card>
    );
  }

  // Locked Badge (in progress)
  const lockedBadge = badge as BadgeProgressItem;

  return (
    <Card className="relative overflow-hidden opacity-75 hover:opacity-100 transition-opacity">
      {/* Locked Indicator */}
      <div className="absolute top-3 right-3">
        <Badge variant="secondary" className="gap-1">
          <Lock size={14} />
          Bloqueado
        </Badge>
      </div>

      <div className="p-6">
        {/* Icon (greyed out) */}
        <div className="text-5xl mb-3 opacity-40">{lockedBadge.icon}</div>

        {/* Name */}
        <h3 className="text-lg font-bold text-gray-900 mb-2">
          {lockedBadge.name}
        </h3>

        {/* Description */}
        <p className="text-sm text-gray-600 mb-4">{lockedBadge.description}</p>

        {/* Progress */}
        <BadgeProgress progress={lockedBadge.criteria} />

        {/* Tips */}
        {lockedBadge.tips && (
          <div className="mt-4 p-3 bg-blue-50 rounded-lg">
            <div className="flex items-start gap-2">
              <TrendingUp size={16} className="text-blue-600 mt-0.5 flex-shrink-0" />
              <div className="text-xs text-blue-900">
                <p className="font-semibold mb-1">Tip:</p>
                <p>{lockedBadge.tips}</p>
              </div>
            </div>
          </div>
        )}
      </div>
    </Card>
  );
}
```

---

### PASO 3: BadgeProgress - Progress Bar Component

```typescript
// filepath: src/components/badges/BadgeProgress.tsx
import { Progress } from "@/components/ui/Progress";

interface BadgeProgressProps {
  progress: {
    target: number;
    current: number;
    unit: string;
    percentage: number;
  };
}

export function BadgeProgress({ progress }: BadgeProgressProps) {
  const { target, current, unit, percentage } = progress;

  // Color por porcentaje
  const getProgressColor = () => {
    if (percentage >= 90) return "bg-green-600";
    if (percentage >= 70) return "bg-blue-600";
    if (percentage >= 50) return "bg-yellow-600";
    return "bg-gray-400";
  };

  return (
    <div className="space-y-2">
      {/* Label con números */}
      <div className="flex justify-between items-center text-sm">
        <span className="text-gray-600">Progreso</span>
        <span className="font-semibold text-gray-900">
          {current} / {target} {unit}
        </span>
      </div>

      {/* Progress Bar */}
      <div className="relative">
        <Progress value={percentage} className="h-2.5" />
        <div
          className={`absolute top-0 left-0 h-2.5 rounded-full transition-all ${getProgressColor()}`}
          style={{ width: `${Math.min(percentage, 100)}%` }}
        />
      </div>

      {/* Percentage Label */}
      <p className="text-xs text-gray-500 text-right">{percentage}%</p>
    </div>
  );
}
```

---

### PASO 4: BadgeTooltip - Tooltip con Criterios

```typescript
// filepath: src/components/badges/BadgeTooltip.tsx
"use client";

import { useState } from "react";
import { Info } from "lucide-react";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/Tooltip";
import type { SellerBadge } from "@/types/badge";

interface BadgeTooltipProps {
  badge: SellerBadge;
}

export function BadgeTooltip({ badge }: BadgeTooltipProps) {
  return (
    <TooltipProvider>
      <Tooltip>
        <TooltipTrigger asChild>
          <button className="absolute bottom-3 right-3 p-1.5 hover:bg-gray-100 rounded-full transition-colors">
            <Info size={16} className="text-gray-500" />
          </button>
        </TooltipTrigger>
        <TooltipContent side="top" className="max-w-xs">
          <div className="space-y-2">
            <p className="font-semibold text-sm">{badge.name}</p>
            <p className="text-xs text-gray-600">{badge.description}</p>

            {/* Criteria Details */}
            {badge.criteria && (
              <div className="mt-3 pt-3 border-t">
                <p className="text-xs font-semibold mb-2">Criterios:</p>
                <ul className="text-xs text-gray-600 space-y-1">
                  {Object.entries(badge.criteria).map(([key, value]) => (
                    <li key={key} className="flex items-start gap-1">
                      <span className="text-green-600">✓</span>
                      <span>
                        {formatCriteriaLabel(key)}: {formatCriteriaValue(value)}
                      </span>
                    </li>
                  ))}
                </ul>
              </div>
            )}
          </div>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  );
}

// Helpers
function formatCriteriaLabel(key: string): string {
  const labels: Record<string, string> = {
    minRating: "Rating mínimo",
    minReviews: "Reviews mínimas",
    currentRating: "Tu rating actual",
    currentReviews: "Tus reviews actuales",
    maxResponseTime: "Tiempo respuesta máximo",
    currentResponseTime: "Tu tiempo actual",
  };
  return labels[key] || key;
}

function formatCriteriaValue(value: any): string {
  if (typeof value === "number") {
    if (value < 10) return value.toFixed(1); // Decimales para ratings
    return value.toString();
  }
  return value.toString();
}
```

---

## 📄 PÁGINAS

### PASO 5: MyBadgesPage - Página de Mis Badges

```typescript
// filepath: src/app/(protected)/settings/badges/page.tsx
import { Metadata } from "next";
import { auth } from "@/lib/auth";
import { redirect } from "next/navigation";
import { BadgesList } from "@/components/badges/BadgesList";
import { Card } from "@/components/ui/Card";
import { Award, TrendingUp } from "lucide-react";

export const metadata: Metadata = {
  title: "Mis Badges | OKLA",
  description: "Gestiona tus badges de reputación",
};

export default async function MyBadgesPage() {
  const session = await auth();

  if (!session?.user) {
    redirect("/login?redirect=/settings/badges");
  }

  return (
    <div className="container max-w-6xl py-12 space-y-8">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold text-gray-900 mb-2">
          Mis Badges y Logros
        </h1>
        <p className="text-gray-600">
          Gana badges completando logros y mejorando tu perfil de vendedor
        </p>
      </div>

      {/* Info Card */}
      <Card className="bg-gradient-to-r from-blue-50 to-purple-50 border-none p-6">
        <div className="flex items-start gap-4">
          <div className="p-3 bg-white rounded-lg">
            <Award size={32} className="text-blue-600" />
          </div>
          <div className="flex-1">
            <h3 className="text-lg font-semibold text-gray-900 mb-2">
              ¿Cómo funcionan los badges?
            </h3>
            <p className="text-sm text-gray-700 mb-3">
              Los badges se otorgan automáticamente cuando cumples ciertos criterios
              de reputación, calidad de servicio y volumen de ventas. Se recalculan
              mensualmente y algunos pueden ser revocados si no mantienes los estándares.
            </p>
            <div className="flex flex-wrap gap-2 text-xs">
              <span className="px-3 py-1 bg-white rounded-full text-gray-700">
                🏆 10 tipos de badges
              </span>
              <span className="px-3 py-1 bg-white rounded-full text-gray-700">
                📊 Recálculo automático mensual
              </span>
              <span className="px-3 py-1 bg-white rounded-full text-gray-700">
                🎯 Progreso en tiempo real
              </span>
            </div>
          </div>
        </div>
      </Card>

      {/* Badges List */}
      <BadgesList sellerId={session.user.id} showProgress={true} variant="grid" />

      {/* Tips Section */}
      <Card className="p-6 bg-gradient-to-br from-green-50 to-emerald-50 border-green-200">
        <div className="flex items-start gap-3">
          <TrendingUp size={24} className="text-green-600 mt-1" />
          <div>
            <h3 className="text-lg font-semibold text-gray-900 mb-2">
              Tips para ganar más badges
            </h3>
            <ul className="space-y-2 text-sm text-gray-700">
              <li className="flex items-start gap-2">
                <span className="text-green-600 font-bold">•</span>
                <span>
                  <strong>Pide reviews:</strong> Envía un mensaje a tus clientes después
                  de cada venta pidiéndoles su opinión.
                </span>
              </li>
              <li className="flex items-start gap-2">
                <span className="text-green-600 font-bold">•</span>
                <span>
                  <strong>Responde rápido:</strong> Contesta reviews en menos de 24 horas
                  para ganar el badge QuickResponder.
                </span>
              </li>
              <li className="flex items-start gap-2">
                <span className="text-green-600 font-bold">•</span>
                <span>
                  <strong>Mantén calidad:</strong> Consistencia es clave. Un servicio
                  estable obtiene más badges que picos ocasionales.
                </span>
              </li>
              <li className="flex items-start gap-2">
                <span className="text-green-600 font-bold">•</span>
                <span>
                  <strong>Verifica tus documentos:</strong> Completa tu KYC para
                  desbloquear el badge VerifiedProfessional.
                </span>
              </li>
            </ul>
          </div>
        </div>
      </Card>
    </div>
  );
}
```

---

## 🪝 HOOKS Y SERVICIOS

### PASO 6: useBadges Hook

```typescript
// filepath: src/lib/hooks/useBadges.ts
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { badgesService } from "@/lib/services/badgesService";
import { toast } from "sonner";

export function useBadges(sellerId: string, includeProgress: boolean = false) {
  return useQuery({
    queryKey: ["badges", sellerId, includeProgress],
    queryFn: async () => {
      const badges = await badgesService.getSellerBadges(sellerId);

      if (includeProgress) {
        const progress = await badgesService.getSellerProgress(sellerId);
        return { badges, progress };
      }

      return { badges, progress: [] };
    },
    staleTime: 1000 * 60 * 10, // 10 minutos
  });
}

export function useMyBadges() {
  return useBadges("me", true);
}

export function useRecalculateBadges(sellerId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => badgesService.recalculateBadges(sellerId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["badges", sellerId] });
      toast.success("Badges recalculados correctamente");
    },
    onError: (error: Error) => {
      toast.error(`Error: ${error.message}`);
    },
  });
}
```

---

### PASO 7: badgesService - API Client

```typescript
// filepath: src/lib/services/badgesService.ts
import { apiClient } from "./apiClient";
import type {
  SellerBadge,
  BadgeProgressItem,
  BadgeType,
  RecalculateBadgesResponse,
} from "@/types/badge";

class BadgesService {
  private readonly baseUrl = "/api/badges";

  /**
   * Obtener badges activos de un seller
   */
  async getSellerBadges(sellerId: string): Promise<SellerBadge[]> {
    const { data } = await apiClient.get<{ badges: SellerBadge[] }>(
      `${this.baseUrl}/seller/${sellerId}`,
    );
    return data.badges;
  }

  /**
   * Obtener progreso hacia badges bloqueados
   */
  async getSellerProgress(sellerId: string): Promise<BadgeProgressItem[]> {
    const { data } = await apiClient.get<{ progress: BadgeProgressItem[] }>(
      `${this.baseUrl}/seller/${sellerId}/progress`,
    );
    return data.progress;
  }

  /**
   * Recalcular badges de un seller (admin only)
   */
  async recalculateBadges(
    sellerId: string,
  ): Promise<RecalculateBadgesResponse> {
    const { data } = await apiClient.post<RecalculateBadgesResponse>(
      `${this.baseUrl}/seller/${sellerId}/recalculate`,
    );
    return data;
  }

  /**
   * Listar todos los tipos de badges disponibles
   */
  async getBadgeTypes(): Promise<BadgeType[]> {
    const { data } = await apiClient.get<{ badgeTypes: BadgeType[] }>(
      `${this.baseUrl}/types`,
    );
    return data.badgeTypes;
  }
}

export const badgesService = new BadgesService();
```

---

## 📦 TIPOS TYPESCRIPT

### PASO 8: Badge Types

```typescript
// filepath: src/types/badge.ts

/**
 * Enum de tipos de badges
 */
export enum BadgeType {
  TopRated = "TopRated",
  TrustedDealer = "TrustedDealer",
  FiveStarSeller = "FiveStarSeller",
  CustomerChoice = "CustomerChoice",
  QuickResponder = "QuickResponder",
  VerifiedProfessional = "VerifiedProfessional",
  RisingStar = "RisingStar",
  VolumeLeader = "VolumeLeader",
  ConsistencyWinner = "ConsistencyWinner",
  CommunityFavorite = "CommunityFavorite",
}

/**
 * Badge Color Options
 */
export type BadgeColor =
  | "gold"
  | "blue"
  | "purple"
  | "green"
  | "orange"
  | "red";

/**
 * Seller Badge (earned)
 */
export interface SellerBadge {
  id: string;
  sellerId: string;
  badgeType: BadgeType;
  name: string;
  description: string;
  icon: string; // Emoji
  color: BadgeColor;
  earnedAt: string; // ISO date
  criteria: Record<string, any>; // Criterios específicos por badge
}

/**
 * Badge Progress Item (locked)
 */
export interface BadgeProgressItem {
  badgeType: BadgeType;
  name: string;
  description: string;
  icon: string;
  color: BadgeColor;
  earned: false;
  criteria: {
    target: number;
    current: number;
    unit: string; // "reviews", "months", "hours"
    percentage: number;
  };
  tips?: string; // Consejo para obtener el badge
}

/**
 * Badge Type Definition (para listar todos)
 */
export interface BadgeType {
  type: BadgeType;
  name: string;
  description: string;
  icon: string;
  color: BadgeColor;
  criteriaDescription: string[];
}

/**
 * Response del endpoint recalculate
 */
export interface RecalculateBadgesResponse {
  sellerId: string;
  badgesEarned: string[]; // Array de badge IDs nuevos
  badgesRevoked: string[]; // Array de badge IDs revocados
  totalBadges: number;
  message: string;
}
```

---

## 🔗 INTEGRACIÓN

### En Seller Public Profile

```typescript
// filepath: src/app/(public)/sellers/[username]/page.tsx

import { BadgesList } from "@/components/badges/BadgesList";

export default async function SellerProfilePage({ params }) {
  const seller = await getSellerByUsername(params.username);

  return (
    <div>
      {/* Header con badges inline */}
      <div className="mb-6">
        <h1 className="text-3xl font-bold mb-2">{seller.name}</h1>
        <BadgesList sellerId={seller.id} variant="inline" />
      </div>

      {/* Resto del perfil */}
    </div>
  );
}
```

### En Vehicle Listing Card

```typescript
// filepath: src/components/vehicles/VehicleCard.tsx

import { Award } from "lucide-react";
import { Badge } from "@/components/ui/Badge";

export function VehicleCard({ vehicle }) {
  const badgeCount = vehicle.seller.badgeCount || 0;

  return (
    <div>
      {/* Card content */}

      {/* Seller info con badge count */}
      <div className="flex items-center gap-2">
        <span className="text-sm text-gray-600">{vehicle.seller.name}</span>
        {badgeCount > 0 && (
          <Badge variant="secondary" size="sm" className="gap-1">
            <Award size={12} />
            {badgeCount}
          </Badge>
        )}
      </div>
    </div>
  );
}
```

### En Navbar (User Menu)

```typescript
// filepath: src/components/layout/Navbar.tsx

import { useMy Badges } from "@/lib/hooks/useBadges";

export function UserMenu() {
  const { data } = useMyBadges();
  const badgeCount = data?.badges.length || 0;

  return (
    <DropdownMenu>
      <DropdownMenuItem asChild>
        <Link href="/settings/badges">
          <Award size={16} className="mr-2" />
          Mis Badges
          {badgeCount > 0 && (
            <Badge variant="primary" size="sm" className="ml-auto">
              {badgeCount}
            </Badge>
          )}
        </Link>
      </DropdownMenuItem>
    </DropdownMenu>
  );
}
```

---

## ✅ CHECKLIST DE IMPLEMENTACIÓN

### Componentes

- [ ] `BadgesList` component (grid + inline variants)
- [ ] `BadgeCard` component (earned + locked states)
- [ ] `BadgeProgress` component (progress bar con colors)
- [ ] `BadgeTooltip` component (hover con criterios)

### Páginas

- [ ] `/settings/badges` - My Badges Page

### Hooks

- [ ] `useBadges` hook
- [ ] `useMyBadges` hook
- [ ] `useRecalculateBadges` mutation

### Servicios

- [ ] `badgesService` API client

### Tipos

- [ ] `badge.ts` types (BadgeType, SellerBadge, BadgeProgressItem, etc.)

### Integraciones

- [ ] Seller public profile (inline badges)
- [ ] Vehicle listing cards (badge count)
- [ ] User menu navbar (link con count)
- [ ] Admin seller management (recalculate button)

---

## 🎯 CASOS DE USO

### 1. Seller ve sus badges

```
Usuario → /settings/badges
  ├─ Ve 3 badges earned: TopRated, QuickResponder, VerifiedProfessional
  ├─ Ve 7 badges locked con progreso:
  │   • VolumeLeader: 35/50 reviews (70%)
  │   • ConsistencyWinner: 3/6 meses (50%)
  │   • CommunityFavorite: Top 30% (need top 10%)
  └─ Lee tips para conseguir cada badge
```

### 2. Comprador ve perfil de seller

```
Buyer → /sellers/auto-elite-rd
  ├─ Ve badges inline: ⭐ TopRated, 🛡️ TrustedDealer, ⚡ QuickResponder
  ├─ Hover en badge → Tooltip con criterios
  └─ Genera confianza → Más probabilidad de contactar
```

### 3. Admin recalcula badges

```
Admin → /admin/sellers/12345
  ├─ Click "Recalcular Badges"
  ├─ Backend evalúa 10 criterios
  ├─ Nuevo badge earned: VolumeLeader (50 reviews alcanzados)
  ├─ Email notificación al seller: "¡Felicidades! Ganaste badge VolumeLeader 📈"
  └─ Badge visible en perfil inmediatamente
```

### 4. Auto-recálculo mensual (Cron)

```
Cron Job (día 1 de cada mes, 00:00)
  ├─ Obtener todos los sellerId activos
  ├─ Para cada seller:
  │   ├─ POST /api/badges/seller/{id}/recalculate
  │   ├─ Evaluar 10 badges
  │   ├─ Otorgar nuevos / Revocar antiguos
  │   └─ Si cambios → Publicar BadgeEarnedEvent / BadgeRevokedEvent
  ├─ NotificationService escucha eventos
  └─ Envía emails a sellers con cambios
```

---

## 🚀 MEJORAS FUTURAS

### Sprint +1

- [ ] Badge leaderboard (top sellers por badge type)
- [ ] Badge sharing (share badge on social media)
- [ ] Badge unlock animations (confetti effect)
- [ ] Custom badge icons (upload por seller)

### Sprint +2

- [ ] Badge NFTs (blockchain badges)
- [ ] Badge marketplace (comprar badges premium?)
- [ ] Badge challenges (monthly competitions)
- [ ] Badge referral program (earn badge por referir sellers)

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/badges-display.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsDealer } from "../helpers/auth";

test.describe("Badges Display System", () => {
  test("debe mostrar badges en perfil público", async ({ page }) => {
    await page.goto("/dealer/auto-dealer-rd");

    await expect(page.getByTestId("dealer-badges")).toBeVisible();
  });

  test.describe("Dealer Dashboard", () => {
    test.beforeEach(async ({ page }) => {
      await loginAsDealer(page);
    });

    test("debe mostrar mis badges ganados", async ({ page }) => {
      await page.goto("/dealer/badges");

      await expect(page.getByTestId("earned-badges")).toBeVisible();
    });

    test("debe mostrar progreso hacia badges", async ({ page }) => {
      await page.goto("/dealer/badges");

      await expect(page.getByTestId("badge-progress")).toBeVisible();
    });

    test("debe ver detalle de badge al hacer click", async ({ page }) => {
      await page.goto("/dealer/badges");

      await page.getByTestId("badge-card").first().click();
      await expect(page.getByRole("dialog")).toBeVisible();
      await expect(page.getByText(/requisitos/i)).toBeVisible();
    });

    test("debe mostrar badges bloqueados", async ({ page }) => {
      await page.goto("/dealer/badges");

      await page.getByRole("tab", { name: /por desbloquear/i }).click();
      await expect(page.getByTestId("locked-badges")).toBeVisible();
    });
  });
});
```

---

**✅ Badges Display System COMPLETO**

_Los sellers ahora pueden ver sus badges earned, track progreso hacia badges bloqueados, y compradores pueden evaluar reputación visualmente._

---

_Última actualización: Enero 9, 2026_  
_Archivo: 35-badges-display-completo.md_  
_Sprint: Módulo 07 - Reviews y Reputación_
