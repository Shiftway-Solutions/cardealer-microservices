# ⭐ 13 - Reviews & Ratings API (ReviewService)

**Servicio:** ReviewService  
**Puerto:** 8080  
**Base Path:** `/api/reviews`  
**Autenticación:** Mixta (algunas rutas públicas)

---

## 📋 Índice

1. [Descripción General](#descripción-general)
2. [Endpoints Disponibles](#endpoints-disponibles)
3. [TypeScript Types](#typescript-types)
4. [Service Layer](#service-layer)
5. [React Query Hooks](#react-query-hooks)
6. [Componentes de Ejemplo](#componentes-de-ejemplo)

---

## 📖 Descripción General

El **ReviewService** gestiona reseñas y calificaciones de vendedores/dealers estilo Amazon:

- ⭐ Ratings de 1-5 estrellas
- 📝 Reviews con título y contenido
- ✅ Compras verificadas
- 💬 Respuestas de vendedores
- 👍👎 Votos de utilidad ("¿Fue útil esta reseña?")
- 🏆 Badges de vendedor (Top Rated, Quick Response, etc.)
- 🛡️ Moderación de contenido

### Flujo de Reviews

```
Compra Completada → Solicitud Automática (7 días) → Usuario Escribe Review
                                                          ↓
                      Vendedor Responde ← Moderación ← Review Publicada
```

---

## 🎯 Endpoints Disponibles

| #   | Método   | Endpoint                                 | Auth     | Descripción                       |
| --- | -------- | ---------------------------------------- | -------- | --------------------------------- |
| 1   | `GET`    | `/api/reviews/seller/{sellerId}`         | ❌       | Reviews de un vendedor (paginado) |
| 2   | `GET`    | `/api/reviews/seller/{sellerId}/summary` | ❌       | Estadísticas agregadas            |
| 3   | `GET`    | `/api/reviews/{reviewId}`                | ❌       | Obtener review específica         |
| 4   | `POST`   | `/api/reviews`                           | ✅       | Crear review                      |
| 5   | `PUT`    | `/api/reviews/{reviewId}`                | ✅       | Actualizar review (solo autor)    |
| 6   | `DELETE` | `/api/reviews/{reviewId}`                | ✅       | Eliminar review (solo autor)      |
| 7   | `POST`   | `/api/reviews/{reviewId}/moderate`       | ✅ Admin | Aprobar/Rechazar review           |
| 8   | `POST`   | `/api/reviews/{reviewId}/respond`        | ✅       | Respuesta del vendedor            |
| 9   | `POST`   | `/api/reviews/{reviewId}/vote`           | ✅       | Votar útil/no útil                |
| 10  | `GET`    | `/api/reviews/{reviewId}/vote-stats`     | ❌       | Estadísticas de votos             |
| 11  | `GET`    | `/api/reviews/seller/{sellerId}/badges`  | ❌       | Badges del vendedor               |

---

## 📝 Detalle de Endpoints

### 1. GET `/api/reviews/seller/{sellerId}` - Reviews de un Vendedor

**Query Params:**

- `page` (int, default: 1)
- `pageSize` (int, default: 20)
- `rating` (int, 1-5) - Filtrar por rating
- `onlyVerified` (bool) - Solo compras verificadas

**Response 200:**

```json
{
  "reviews": [
    {
      "id": "review-123",
      "buyerId": "user-456",
      "buyerName": "María García",
      "buyerPhotoUrl": "https://cdn.okla.com.do/users/maria.jpg",
      "sellerId": "dealer-789",
      "vehicleId": "vehicle-012",
      "rating": 5,
      "title": "Excelente experiencia de compra",
      "content": "El dealer fue muy profesional, el vehículo estaba en perfectas condiciones...",
      "isVerifiedPurchase": true,
      "status": "Approved",
      "helpfulCount": 12,
      "notHelpfulCount": 2,
      "sellerResponse": {
        "responseText": "¡Gracias María! Fue un placer atenderte.",
        "respondedAt": "2026-01-20T10:00:00Z"
      },
      "createdAt": "2026-01-15T14:30:00Z"
    }
  ],
  "totalCount": 45,
  "page": 1,
  "pageSize": 20,
  "totalPages": 3
}
```

---

### 2. GET `/api/reviews/seller/{sellerId}/summary` - Estadísticas

**Response 200:**

```json
{
  "sellerId": "dealer-789",
  "averageRating": 4.7,
  "totalReviews": 156,
  "verifiedPurchaseCount": 142,
  "ratingDistribution": {
    "5": 98,
    "4": 35,
    "3": 15,
    "2": 5,
    "1": 3
  },
  "responseRate": 85.5,
  "averageResponseTime": "2 horas"
}
```

---

### 4. POST `/api/reviews` - Crear Review

**Auth:** ✅ Required

**Request Body:**

```json
{
  "sellerId": "dealer-789",
  "vehicleId": "vehicle-012",
  "orderId": "order-345",
  "rating": 5,
  "title": "Excelente experiencia",
  "content": "El proceso de compra fue muy fluido. El vendedor respondió todas mis preguntas..."
}
```

**Response 201:**

```json
{
  "id": "review-456",
  "buyerId": "user-123",
  "buyerName": "Carlos Pérez",
  "sellerId": "dealer-789",
  "vehicleId": "vehicle-012",
  "rating": 5,
  "title": "Excelente experiencia",
  "content": "El proceso de compra fue muy fluido...",
  "isVerifiedPurchase": true,
  "status": "Pending",
  "createdAt": "2026-01-30T10:00:00Z"
}
```

---

### 8. POST `/api/reviews/{reviewId}/respond` - Respuesta del Vendedor

**Auth:** ✅ Required (debe ser el vendedor de la review)

**Request Body:**

```json
{
  "responseText": "¡Gracias por tu comentario! Nos alegra que hayas tenido una buena experiencia."
}
```

**Response 200:**

```json
{
  "message": "Respuesta publicada exitosamente"
}
```

---

### 9. POST `/api/reviews/{reviewId}/vote` - Votar Utilidad

**Auth:** ✅ Required

**Request Body:**

```json
{
  "isHelpful": true
}
```

**Response 200:**

```json
{
  "reviewId": "review-123",
  "helpfulCount": 13,
  "notHelpfulCount": 2,
  "userVote": "helpful"
}
```

---

## 🔧 TypeScript Types

```typescript
// ============================================================================
// REVIEW TYPES
// ============================================================================

export interface Review {
  id: string;
  buyerId: string;
  buyerName: string;
  buyerPhotoUrl?: string;
  sellerId: string;
  vehicleId?: string;
  orderId?: string;
  rating: number; // 1-5
  title: string;
  content: string;
  isVerifiedPurchase: boolean;
  status: ReviewStatus;
  helpfulCount: number;
  notHelpfulCount: number;
  sellerResponse?: SellerResponse;
  moderatedAt?: string;
  moderatedBy?: string;
  rejectionReason?: string;
  createdAt: string;
  updatedAt?: string;
}

export type ReviewStatus =
  | "Pending" // En espera de moderación
  | "Approved" // Aprobada y visible
  | "Rejected"; // Rechazada

export interface SellerResponse {
  responseText: string;
  respondedAt: string;
}

// ============================================================================
// SUMMARY & STATS
// ============================================================================

export interface ReviewSummary {
  sellerId: string;
  averageRating: number;
  totalReviews: number;
  verifiedPurchaseCount: number;
  ratingDistribution: Record<string, number>;
  responseRate: number;
  averageResponseTime: string;
}

export interface VoteStats {
  reviewId: string;
  helpfulCount: number;
  notHelpfulCount: number;
  userVote?: "helpful" | "not_helpful" | null;
}

// ============================================================================
// BADGES
// ============================================================================

export interface SellerBadge {
  id: string;
  type: BadgeType;
  name: string;
  description: string;
  iconUrl: string;
  earnedAt: string;
}

export type BadgeType =
  | "TopRated" // Rating >= 4.8
  | "QuickResponse" // Responde < 2h
  | "Verified" // Documentos verificados
  | "SuperSeller" // > 50 ventas
  | "Founder"; // Early Bird

// ============================================================================
// REQUEST TYPES
// ============================================================================

export interface CreateReviewRequest {
  sellerId: string;
  vehicleId?: string;
  orderId?: string;
  rating: number;
  title: string;
  content: string;
}

export interface UpdateReviewRequest {
  rating?: number;
  title?: string;
  content?: string;
}

export interface VoteRequest {
  isHelpful: boolean;
}

export interface SellerResponseRequest {
  responseText: string;
}

// ============================================================================
// RESPONSE TYPES
// ============================================================================

export interface PagedReviews {
  reviews: Review[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
```

---

## 📡 Service Layer

```typescript
// src/services/reviewService.ts
import { apiClient } from "./api-client";
import type {
  Review,
  ReviewSummary,
  PagedReviews,
  VoteStats,
  SellerBadge,
  CreateReviewRequest,
  UpdateReviewRequest,
  VoteRequest,
  SellerResponseRequest,
} from "@/types/review";

interface GetReviewsParams {
  page?: number;
  pageSize?: number;
  rating?: number;
  onlyVerified?: boolean;
}

class ReviewService {
  // ============================================================================
  // REVIEWS
  // ============================================================================

  async getSellerReviews(
    sellerId: string,
    params?: GetReviewsParams,
  ): Promise<PagedReviews> {
    const response = await apiClient.get<PagedReviews>(
      `/api/reviews/seller/${sellerId}`,
      { params },
    );
    return response.data;
  }

  async getReviewSummary(sellerId: string): Promise<ReviewSummary> {
    const response = await apiClient.get<ReviewSummary>(
      `/api/reviews/seller/${sellerId}/summary`,
    );
    return response.data;
  }

  async getReviewById(reviewId: string): Promise<Review> {
    const response = await apiClient.get<Review>(`/api/reviews/${reviewId}`);
    return response.data;
  }

  async createReview(request: CreateReviewRequest): Promise<Review> {
    const response = await apiClient.post<Review>("/api/reviews", request);
    return response.data;
  }

  async updateReview(
    reviewId: string,
    request: UpdateReviewRequest,
  ): Promise<Review> {
    const response = await apiClient.put<Review>(
      `/api/reviews/${reviewId}`,
      request,
    );
    return response.data;
  }

  async deleteReview(reviewId: string): Promise<void> {
    await apiClient.delete(`/api/reviews/${reviewId}`);
  }

  // ============================================================================
  // SELLER INTERACTIONS
  // ============================================================================

  async respondToReview(
    reviewId: string,
    request: SellerResponseRequest,
  ): Promise<void> {
    await apiClient.post(`/api/reviews/${reviewId}/respond`, request);
  }

  // ============================================================================
  // VOTES
  // ============================================================================

  async voteReview(reviewId: string, request: VoteRequest): Promise<VoteStats> {
    const response = await apiClient.post<VoteStats>(
      `/api/reviews/${reviewId}/vote`,
      request,
    );
    return response.data;
  }

  async getVoteStats(reviewId: string): Promise<VoteStats> {
    const response = await apiClient.get<VoteStats>(
      `/api/reviews/${reviewId}/vote-stats`,
    );
    return response.data;
  }

  // ============================================================================
  // BADGES
  // ============================================================================

  async getSellerBadges(sellerId: string): Promise<SellerBadge[]> {
    const response = await apiClient.get<SellerBadge[]>(
      `/api/reviews/seller/${sellerId}/badges`,
    );
    return response.data;
  }

  // ============================================================================
  // MODERATION (Admin)
  // ============================================================================

  async moderateReview(
    reviewId: string,
    isApproved: boolean,
    rejectionReason?: string,
  ): Promise<void> {
    await apiClient.post(`/api/reviews/${reviewId}/moderate`, null, {
      params: { isApproved, rejectionReason },
    });
  }
}

export const reviewService = new ReviewService();
```

---

## 🎣 React Query Hooks

```typescript
// src/hooks/useReviews.ts
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { reviewService } from "@/services/reviewService";
import type { CreateReviewRequest, VoteRequest } from "@/types/review";

export const reviewKeys = {
  all: ["reviews"] as const,
  sellerReviews: (sellerId: string) =>
    [...reviewKeys.all, "seller", sellerId] as const,
  sellerSummary: (sellerId: string) =>
    [...reviewKeys.all, "summary", sellerId] as const,
  sellerBadges: (sellerId: string) =>
    [...reviewKeys.all, "badges", sellerId] as const,
  detail: (id: string) => [...reviewKeys.all, "detail", id] as const,
  voteStats: (id: string) => [...reviewKeys.all, "votes", id] as const,
};

// ============================================================================
// QUERIES
// ============================================================================

export function useSellerReviews(
  sellerId: string,
  params?: { rating?: number; onlyVerified?: boolean },
) {
  return useQuery({
    queryKey: [...reviewKeys.sellerReviews(sellerId), params],
    queryFn: () => reviewService.getSellerReviews(sellerId, params),
    enabled: !!sellerId,
  });
}

export function useReviewSummary(sellerId: string) {
  return useQuery({
    queryKey: reviewKeys.sellerSummary(sellerId),
    queryFn: () => reviewService.getReviewSummary(sellerId),
    enabled: !!sellerId,
  });
}

export function useSellerBadges(sellerId: string) {
  return useQuery({
    queryKey: reviewKeys.sellerBadges(sellerId),
    queryFn: () => reviewService.getSellerBadges(sellerId),
    enabled: !!sellerId,
  });
}

export function useReview(reviewId: string) {
  return useQuery({
    queryKey: reviewKeys.detail(reviewId),
    queryFn: () => reviewService.getReviewById(reviewId),
    enabled: !!reviewId,
  });
}

// ============================================================================
// MUTATIONS
// ============================================================================

export function useCreateReview() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateReviewRequest) =>
      reviewService.createReview(request),
    onSuccess: (data) => {
      queryClient.invalidateQueries({
        queryKey: reviewKeys.sellerReviews(data.sellerId),
      });
      queryClient.invalidateQueries({
        queryKey: reviewKeys.sellerSummary(data.sellerId),
      });
    },
  });
}

export function useDeleteReview(sellerId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (reviewId: string) => reviewService.deleteReview(reviewId),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: reviewKeys.sellerReviews(sellerId),
      });
      queryClient.invalidateQueries({
        queryKey: reviewKeys.sellerSummary(sellerId),
      });
    },
  });
}

export function useVoteReview() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      reviewId,
      isHelpful,
    }: {
      reviewId: string;
      isHelpful: boolean;
    }) => reviewService.voteReview(reviewId, { isHelpful }),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: reviewKeys.voteStats(variables.reviewId),
      });
    },
  });
}

export function useRespondToReview() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      reviewId,
      responseText,
    }: {
      reviewId: string;
      responseText: string;
    }) => reviewService.respondToReview(reviewId, { responseText }),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: reviewKeys.detail(variables.reviewId),
      });
    },
  });
}
```

---

## 🧩 Componentes de Ejemplo

### ReviewCard

```typescript
// src/components/reviews/ReviewCard.tsx
import { useVoteReview } from "@/hooks/useReviews";
import { formatDistanceToNow } from "date-fns";
import { es } from "date-fns/locale";
import { FiThumbsUp, FiThumbsDown, FiCheck } from "react-icons/fi";
import type { Review } from "@/types/review";

interface Props {
  review: Review;
}

export const ReviewCard = ({ review }: Props) => {
  const voteMutation = useVoteReview();

  const handleVote = (isHelpful: boolean) => {
    voteMutation.mutate({ reviewId: review.id, isHelpful });
  };

  return (
    <div className="border-b py-6">
      {/* Header */}
      <div className="flex items-start gap-3 mb-3">
        <img
          src={review.buyerPhotoUrl || "/default-avatar.png"}
          alt={review.buyerName}
          className="w-10 h-10 rounded-full"
        />
        <div className="flex-1">
          <div className="flex items-center gap-2">
            <span className="font-medium">{review.buyerName}</span>
            {review.isVerifiedPurchase && (
              <span className="flex items-center gap-1 text-xs text-green-600 bg-green-50 px-2 py-0.5 rounded-full">
                <FiCheck className="w-3 h-3" />
                Compra verificada
              </span>
            )}
          </div>
          <div className="flex items-center gap-2 text-sm text-gray-500">
            <div className="flex">
              {[1, 2, 3, 4, 5].map((star) => (
                <span
                  key={star}
                  className={star <= review.rating ? "text-yellow-400" : "text-gray-300"}
                >
                  ★
                </span>
              ))}
            </div>
            <span>·</span>
            <span>
              {formatDistanceToNow(new Date(review.createdAt), {
                addSuffix: true,
                locale: es,
              })}
            </span>
          </div>
        </div>
      </div>

      {/* Content */}
      <h4 className="font-semibold mb-2">{review.title}</h4>
      <p className="text-gray-700 mb-4">{review.content}</p>

      {/* Seller Response */}
      {review.sellerResponse && (
        <div className="bg-gray-50 rounded-lg p-4 mb-4 ml-6 border-l-4 border-blue-500">
          <p className="text-sm font-medium text-blue-600 mb-1">Respuesta del vendedor:</p>
          <p className="text-gray-700 text-sm">{review.sellerResponse.responseText}</p>
        </div>
      )}

      {/* Votes */}
      <div className="flex items-center gap-4 text-sm text-gray-500">
        <span>¿Fue útil esta reseña?</span>
        <button
          onClick={() => handleVote(true)}
          className="flex items-center gap-1 hover:text-green-600"
        >
          <FiThumbsUp /> Sí ({review.helpfulCount})
        </button>
        <button
          onClick={() => handleVote(false)}
          className="flex items-center gap-1 hover:text-red-600"
        >
          <FiThumbsDown /> No ({review.notHelpfulCount})
        </button>
      </div>
    </div>
  );
};
```

---

### SellerRatingSummary

```typescript
// src/components/reviews/SellerRatingSummary.tsx
import { useReviewSummary, useSellerBadges } from "@/hooks/useReviews";

export const SellerRatingSummary = ({ sellerId }: { sellerId: string }) => {
  const { data: summary } = useReviewSummary(sellerId);
  const { data: badges } = useSellerBadges(sellerId);

  if (!summary) return null;

  return (
    <div className="bg-white rounded-xl p-6 border">
      {/* Average Rating */}
      <div className="flex items-center gap-4 mb-6">
        <div className="text-5xl font-bold text-gray-900">
          {summary.averageRating.toFixed(1)}
        </div>
        <div>
          <div className="flex text-2xl text-yellow-400">
            {[1, 2, 3, 4, 5].map((star) => (
              <span key={star} className={star <= Math.round(summary.averageRating) ? "" : "text-gray-300"}>
                ★
              </span>
            ))}
          </div>
          <p className="text-gray-500">{summary.totalReviews} reseñas</p>
        </div>
      </div>

      {/* Rating Distribution */}
      <div className="space-y-2 mb-6">
        {[5, 4, 3, 2, 1].map((rating) => {
          const count = summary.ratingDistribution[rating] || 0;
          const percentage = summary.totalReviews > 0
            ? (count / summary.totalReviews) * 100
            : 0;

          return (
            <div key={rating} className="flex items-center gap-2 text-sm">
              <span className="w-8">{rating}★</span>
              <div className="flex-1 bg-gray-200 rounded-full h-2">
                <div
                  className="bg-yellow-400 h-2 rounded-full"
                  style={{ width: `${percentage}%` }}
                />
              </div>
              <span className="w-8 text-gray-500">{count}</span>
            </div>
          );
        })}
      </div>

      {/* Badges */}
      {badges && badges.length > 0 && (
        <div className="border-t pt-4">
          <h4 className="text-sm font-medium text-gray-700 mb-2">Badges</h4>
          <div className="flex flex-wrap gap-2">
            {badges.map((badge) => (
              <span
                key={badge.id}
                className="px-3 py-1 bg-blue-50 text-blue-700 rounded-full text-sm"
                title={badge.description}
              >
                {badge.name}
              </span>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};
```

---

## 🎉 Resumen

✅ **11 Endpoints documentados**  
✅ **TypeScript Types completos** (Review, Summary, Badges, Votes)  
✅ **Service Layer** con 11 métodos  
✅ **React Query Hooks** (8 hooks)  
✅ **2 Componentes UI** (ReviewCard + SellerRatingSummary)

---

_Última actualización: Enero 30, 2026_
