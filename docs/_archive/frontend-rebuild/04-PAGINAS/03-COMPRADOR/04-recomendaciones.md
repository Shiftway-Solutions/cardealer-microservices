---
title: "21. Sistema de Recomendaciones con ML"
priority: P1
estimated_time: "2 horas"
dependencies: []
apis: []
status: complete
last_updated: "2026-01-30"
---

# 21. Sistema de Recomendaciones con ML

**Objetivo:** Implementar sistema de recomendaciones inteligente basado en Machine Learning para sugerir vehículos personalizados según comportamiento, preferencias y perfiles similares.

**Prioridad:** P2 (Baja - Mejora de conversión y engagement)  
**Complejidad:** 🔴 Muy Alta (ML, Data pipelines, Embeddings)  
**Dependencias:** RecommendationService (backend), UserBehaviorService, VehicleIntelligenceService

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura ML](#arquitectura-ml)
2. [Tipos de Recomendaciones](#tipos-de-recomendaciones)
3. [Backend API](#backend-api)
4. [Componentes Frontend](#componentes-frontend)
5. [Algoritmos](#algoritmos)
6. [Hooks y Servicios](#hooks-y-servicios)
7. [Tipos TypeScript](#tipos-typescript)
8. [Validación](#validación)

---

## 🧠 ARQUITECTURA ML

### Pipeline de Recomendaciones

```
┌────────────────────────────────────────────────────────────────────┐
│                    RECOLECCIÓN DE DATOS                            │
├────────────────────────────────────────────────────────────────────┤
│ • Vistas de vehículos                                              │
│ • Clicks en filtros                                                │
│ • Tiempo en página                                                 │
│ • Favoritos agregados                                              │
│ • Búsquedas realizadas                                             │
│ • Comparaciones                                                    │
│ • Contactos a dealers                                              │
│ • Compras realizadas                                               │
└────────────────────────────────────────────────────────────────────┘
                            ↓
┌────────────────────────────────────────────────────────────────────┐
│                    FEATURE ENGINEERING                             │
├────────────────────────────────────────────────────────────────────┤
│ • User features:                                                   │
│   - Budget range (inferido)                                        │
│   - Brand preference                                               │
│   - Body type preference                                           │
│   - Fuel type preference                                           │
│   - Search patterns                                                │
│   - Session duration                                               │
│                                                                    │
│ • Vehicle features:                                                │
│   - Categorical: make, model, year, body_type, fuel_type          │
│   - Numerical: price, mileage, engine_size                         │
│   - Text embeddings: description (BERT/Sentence-BERT)              │
│   - Image embeddings: photos (ResNet/CLIP)                         │
│   - Popularity score                                               │
│   - Conversion rate                                                │
└────────────────────────────────────────────────────────────────────┘
                            ↓
┌────────────────────────────────────────────────────────────────────┐
│                        MODELOS ML                                  │
├────────────────────────────────────────────────────────────────────┤
│ 1. Collaborative Filtering (CF)                                    │
│    - User-based CF: Usuarios similares                             │
│    - Item-based CF: Vehículos similares                            │
│    - Matrix Factorization (ALS)                                    │
│                                                                    │
│ 2. Content-Based Filtering                                         │
│    - Cosine similarity de features                                 │
│    - TF-IDF de descripciones                                       │
│    - Image similarity (embeddings)                                 │
│                                                                    │
│ 3. Hybrid Model (Ensemble)                                         │
│    - Weighted combination                                          │
│    - Stacking de modelos                                           │
│    - Contextual bandits                                            │
│                                                                    │
│ 4. Deep Learning (Neural CF)                                       │
│    - User/Item embeddings                                          │
│    - Multi-layer perceptron                                        │
│    - Attention mechanism                                           │
└────────────────────────────────────────────────────────────────────┘
                            ↓
┌────────────────────────────────────────────────────────────────────┐
│                    RANKING & FILTERING                             │
├────────────────────────────────────────────────────────────────────┤
│ • Score de relevancia                                              │
│ • Diversidad de resultados                                         │
│ • Business rules (active listings, verified dealers)               │
│ • Freshness (listings recientes)                                   │
│ • Serendipity (10% inesperado)                                     │
│ • Deduplicación                                                    │
└────────────────────────────────────────────────────────────────────┘
                            ↓
┌────────────────────────────────────────────────────────────────────┐
│                      PRESENTACIÓN                                  │
├────────────────────────────────────────────────────────────────────┤
│ • "Para ti" (personalized)                                         │
│ • "Usuarios como tú también vieron" (collaborative)                │
│ • "Similares a este vehículo" (content-based)                      │
│ • "Trending ahora" (popularity-based)                              │
│ • "Porque buscaste X" (context-aware)                              │
└────────────────────────────────────────────────────────────────────┘
```

---

## 🎯 TIPOS DE RECOMENDACIONES

### 1. Personalized Recommendations ("Para ti")

**Algoritmo:** Hybrid (CF + Content-Based)  
**Input:** User ID + Historial completo  
**Output:** Top 20 vehículos rankeados

```typescript
interface PersonalizedRecommendation {
  vehicleId: string;
  score: number; // 0-1
  reason: string; // "Based on your searches"
  explanation: string; // "You viewed similar SUVs"
}
```

### 2. Similar Vehicles ("También te puede interesar")

**Algoritmo:** Content-Based Filtering  
**Input:** Vehicle ID  
**Output:** Top 10 vehículos similares

```typescript
interface SimilarVehicle {
  vehicleId: string;
  similarityScore: number; // 0-1
  matchingFeatures: string[]; // ["brand", "body_type", "price_range"]
}
```

### 3. Collaborative Filtering ("Usuarios como tú también vieron")

**Algoritmo:** User-based CF  
**Input:** User ID  
**Output:** Top 15 vehículos

```typescript
interface CollaborativeRecommendation {
  vehicleId: string;
  score: number;
  similarUsersCount: number; // Cuántos usuarios similares vieron esto
}
```

### 4. Trending ("Popular ahora")

**Algoritmo:** Time-decay weighted popularity  
**Input:** Timeframe (24h, 7d, 30d)  
**Output:** Top 10 vehículos trending

```typescript
interface TrendingVehicle {
  vehicleId: string;
  viewCount: number;
  growthRate: number; // % incremento vs periodo anterior
  trendingScore: number;
}
```

### 5. Context-Aware ("Porque buscaste X")

**Algoritmo:** Search-based recommendations  
**Input:** Search query + Filters  
**Output:** Top 10 vehículos

```typescript
interface ContextRecommendation {
  vehicleId: string;
  score: number;
  matchedCriteria: {
    query: string;
    filters: Record<string, any>;
  };
}
```

---

## 🔌 BACKEND API

### RecommendationService Endpoints

```typescript
// filepath: docs/backend/RecommendationService-API.md

GET    /api/recommendations/personalized        # Para ti (requiere auth)
GET    /api/recommendations/similar/{vehicleId} # Vehículos similares
GET    /api/recommendations/collaborative       # Usuarios como tú
GET    /api/recommendations/trending            # Trending ahora
GET    /api/recommendations/context             # Basado en búsqueda

POST   /api/recommendations/track-event         # Track evento de usuario
POST   /api/recommendations/feedback            # Feedback (thumbs up/down)

GET    /api/recommendations/explain/{vehicleId} # Explicación de recomendación
GET    /api/recommendations/stats               # Stats de recomendaciones (admin)
```

### Request/Response Examples

```typescript
// GET /api/recommendations/personalized?limit=20
{
  "recommendations": [
    {
      "vehicleId": "uuid",
      "score": 0.92,
      "reason": "based_on_searches",
      "explanation": "Buscaste SUVs Toyota en tu rango de precio",
      "vehicle": { /* Vehicle object */ }
    }
  ],
  "metadata": {
    "userId": "uuid",
    "timestamp": "2026-01-29T10:00:00Z",
    "modelVersion": "v2.3.1"
  }
}

// POST /api/recommendations/track-event
{
  "eventType": "view" | "click" | "favorite" | "contact" | "purchase",
  "vehicleId": "uuid",
  "context": {
    "source": "homepage" | "search" | "recommendations",
    "position": 2,
    "sessionId": "uuid"
  }
}
```

---

## 🎨 COMPONENTES FRONTEND

### PASO 1: RecommendationGrid - Grid de Recomendaciones

```typescript
// filepath: src/components/recommendations/RecommendationGrid.tsx
"use client";

import { VehicleCard } from "@/components/vehicles/VehicleCard";
import { Sparkles, TrendingUp, Users, Search } from "lucide-react";
import { Skeleton } from "@/components/ui/Skeleton";
import { useRecommendations } from "@/lib/hooks/useRecommendations";
import type { RecommendationType } from "@/types/recommendation";

interface RecommendationGridProps {
  type: RecommendationType;
  vehicleId?: string; // For "similar" type
  searchContext?: any; // For "context" type
  limit?: number;
}

export function RecommendationGrid({
  type,
  vehicleId,
  searchContext,
  limit = 10,
}: RecommendationGridProps) {
  const { data: recommendations, isLoading } = useRecommendations({
    type,
    vehicleId,
    searchContext,
    limit,
  });

  const getHeader = () => {
    switch (type) {
      case "personalized":
        return {
          icon: Sparkles,
          title: "Recomendado para ti",
          subtitle: "Basado en tu actividad reciente",
        };
      case "similar":
        return {
          icon: null,
          title: "Vehículos similares",
          subtitle: "También te puede interesar",
        };
      case "collaborative":
        return {
          icon: Users,
          title: "Usuarios como tú también vieron",
          subtitle: "Descubre qué están viendo otros",
        };
      case "trending":
        return {
          icon: TrendingUp,
          title: "Popular ahora",
          subtitle: "Los vehículos más vistos esta semana",
        };
      case "context":
        return {
          icon: Search,
          title: "Porque buscaste esto",
          subtitle: "Resultados relacionados con tu búsqueda",
        };
    }
  };

  const header = getHeader();
  const Icon = header.icon;

  if (isLoading) {
    return (
      <div className="space-y-4">
        <div className="animate-pulse">
          <div className="h-8 w-64 bg-gray-200 rounded mb-2" />
          <div className="h-4 w-48 bg-gray-200 rounded" />
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          {[...Array(4)].map((_, i) => (
            <Skeleton key={i} className="h-80" />
          ))}
        </div>
      </div>
    );
  }

  if (!recommendations || recommendations.length === 0) {
    return null;
  }

  return (
    <section className="space-y-6">
      {/* Header */}
      <div>
        <div className="flex items-center gap-2 mb-2">
          {Icon && <Icon size={24} className="text-primary-600" />}
          <h2 className="text-2xl font-bold text-gray-900">{header.title}</h2>
        </div>
        <p className="text-gray-600">{header.subtitle}</p>
      </div>

      {/* Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {recommendations.map((rec, index) => (
          <div key={rec.vehicleId} className="relative">
            <VehicleCard
              vehicle={rec.vehicle}
              onView={() => trackRecommendationClick(rec, index)}
            />

            {/* Explanation tooltip */}
            {rec.explanation && (
              <div className="absolute top-2 right-2 z-10">
                <ExplanationBadge
                  reason={rec.reason}
                  explanation={rec.explanation}
                />
              </div>
            )}
          </div>
        ))}
      </div>
    </section>
  );
}

function ExplanationBadge({ reason, explanation }: any) {
  const getLabel = () => {
    switch (reason) {
      case "based_on_searches":
        return "Por tus búsquedas";
      case "similar_to_viewed":
        return "Similar a lo que viste";
      case "popular":
        return "Popular";
      case "trending":
        return "Tendencia";
      default:
        return "Recomendado";
    }
  };

  return (
    <div className="group relative">
      <span className="inline-flex items-center gap-1 px-2 py-1 bg-white/90 backdrop-blur rounded-full text-xs font-medium text-primary-600 shadow-sm">
        <Sparkles size={12} />
        {getLabel()}
      </span>

      {/* Tooltip */}
      <div className="absolute right-0 top-full mt-2 w-48 p-3 bg-gray-900 text-white text-xs rounded-lg opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all shadow-lg">
        {explanation}
      </div>
    </div>
  );
}

function trackRecommendationClick(rec: any, position: number) {
  // Track analytics
  if (typeof window !== "undefined" && (window as any).gtag) {
    (window as any).gtag("event", "recommendation_click", {
      vehicle_id: rec.vehicleId,
      recommendation_type: rec.reason,
      position,
      score: rec.score,
    });
  }
}
```

---

### PASO 2: PersonalizedFeed - Feed Personalizado Infinite Scroll

```typescript
// filepath: src/components/recommendations/PersonalizedFeed.tsx
"use client";

import { useEffect, useRef } from "react";
import { useIntersectionObserver } from "@/lib/hooks/useIntersectionObserver";
import { VehicleCard } from "@/components/vehicles/VehicleCard";
import { Skeleton } from "@/components/ui/Skeleton";
import { useInfiniteRecommendations } from "@/lib/hooks/useRecommendations";

export function PersonalizedFeed() {
  const {
    data,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
    isLoading,
  } = useInfiniteRecommendations("personalized");

  const loadMoreRef = useRef<HTMLDivElement>(null);
  const isIntersecting = useIntersectionObserver(loadMoreRef, {
    threshold: 0.5,
  });

  useEffect(() => {
    if (isIntersecting && hasNextPage && !isFetchingNextPage) {
      fetchNextPage();
    }
  }, [isIntersecting, hasNextPage, isFetchingNextPage, fetchNextPage]);

  if (isLoading) {
    return (
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {[...Array(6)].map((_, i) => (
          <Skeleton key={i} className="h-96" />
        ))}
      </div>
    );
  }

  const recommendations = data?.pages.flatMap((page) => page.recommendations) || [];

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {recommendations.map((rec, index) => (
          <VehicleCard
            key={`${rec.vehicleId}-${index}`}
            vehicle={rec.vehicle}
            showRecommendationBadge
            recommendationReason={rec.reason}
          />
        ))}
      </div>

      {/* Load More Trigger */}
      <div ref={loadMoreRef} className="py-4 text-center">
        {isFetchingNextPage && (
          <div className="flex justify-center gap-4">
            {[...Array(3)].map((_, i) => (
              <Skeleton key={i} className="h-80 w-64" />
            ))}
          </div>
        )}
        {!hasNextPage && recommendations.length > 0 && (
          <p className="text-gray-500">Has visto todas las recomendaciones</p>
        )}
      </div>
    </div>
  );
}
```

---

### PASO 3: SimilarVehiclesCarousel - Carrusel de Similares

```typescript
// filepath: src/components/recommendations/SimilarVehiclesCarousel.tsx
"use client";

import { ChevronLeft, ChevronRight } from "lucide-react";
import { useState, useRef } from "react";
import { VehicleCard } from "@/components/vehicles/VehicleCard";
import { Button } from "@/components/ui/Button";
import { useRecommendations } from "@/lib/hooks/useRecommendations";

interface SimilarVehiclesCarouselProps {
  vehicleId: string;
}

export function SimilarVehiclesCarousel({ vehicleId }: SimilarVehiclesCarouselProps) {
  const { data: recommendations, isLoading } = useRecommendations({
    type: "similar",
    vehicleId,
    limit: 10,
  });

  const scrollRef = useRef<HTMLDivElement>(null);
  const [canScrollLeft, setCanScrollLeft] = useState(false);
  const [canScrollRight, setCanScrollRight] = useState(true);

  const scroll = (direction: "left" | "right") => {
    if (!scrollRef.current) return;

    const scrollAmount = 300;
    const newScrollLeft =
      scrollRef.current.scrollLeft + (direction === "left" ? -scrollAmount : scrollAmount);

    scrollRef.current.scrollTo({
      left: newScrollLeft,
      behavior: "smooth",
    });
  };

  const handleScroll = () => {
    if (!scrollRef.current) return;

    const { scrollLeft, scrollWidth, clientWidth } = scrollRef.current;
    setCanScrollLeft(scrollLeft > 0);
    setCanScrollRight(scrollLeft < scrollWidth - clientWidth - 10);
  };

  if (isLoading || !recommendations || recommendations.length === 0) {
    return null;
  }

  return (
    <section className="relative">
      <h2 className="text-2xl font-bold text-gray-900 mb-6">
        Vehículos similares
      </h2>

      {/* Navigation Buttons */}
      {canScrollLeft && (
        <Button
          variant="ghost"
          size="icon"
          className="absolute left-0 top-1/2 -translate-y-1/2 z-10 bg-white shadow-lg"
          onClick={() => scroll("left")}
        >
          <ChevronLeft size={24} />
        </Button>
      )}

      {canScrollRight && (
        <Button
          variant="ghost"
          size="icon"
          className="absolute right-0 top-1/2 -translate-y-1/2 z-10 bg-white shadow-lg"
          onClick={() => scroll("right")}
        >
          <ChevronRight size={24} />
        </Button>
      )}

      {/* Carousel */}
      <div
        ref={scrollRef}
        onScroll={handleScroll}
        className="flex gap-4 overflow-x-auto scrollbar-hide snap-x snap-mandatory"
        style={{ scrollbarWidth: "none", msOverflowStyle: "none" }}
      >
        {recommendations.map((rec) => (
          <div key={rec.vehicleId} className="snap-start flex-shrink-0 w-72">
            <VehicleCard vehicle={rec.vehicle} />
            {rec.matchingFeatures && rec.matchingFeatures.length > 0 && (
              <div className="mt-2 text-xs text-gray-500">
                Coincide en: {rec.matchingFeatures.join(", ")}
              </div>
            )}
          </div>
        ))}
      </div>
    </section>
  );
}
```

---

### PASO 4: RecommendationFeedback - Feedback (Thumbs Up/Down)

```typescript
// filepath: src/components/recommendations/RecommendationFeedback.tsx
"use client";

import { useState } from "react";
import { ThumbsUp, ThumbsDown } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { useRecommendationFeedback } from "@/lib/hooks/useRecommendations";

interface RecommendationFeedbackProps {
  vehicleId: string;
  recommendationType: string;
}

export function RecommendationFeedback({
  vehicleId,
  recommendationType,
}: RecommendationFeedbackProps) {
  const [feedback, setFeedback] = useState<"up" | "down" | null>(null);
  const { mutate: sendFeedback } = useRecommendationFeedback();

  const handleFeedback = (type: "up" | "down") => {
    setFeedback(type);
    sendFeedback({
      vehicleId,
      recommendationType,
      feedback: type,
    });
  };

  return (
    <div className="flex items-center gap-2">
      <span className="text-sm text-gray-600">¿Te gustó esta recomendación?</span>
      <Button
        variant="ghost"
        size="sm"
        onClick={() => handleFeedback("up")}
        className={feedback === "up" ? "text-green-600" : "text-gray-400"}
      >
        <ThumbsUp size={16} />
      </Button>
      <Button
        variant="ghost"
        size="sm"
        onClick={() => handleFeedback("down")}
        className={feedback === "down" ? "text-red-600" : "text-gray-400"}
      >
        <ThumbsDown size={16} />
      </Button>
    </div>
  );
}
```

---

## 📄 PÁGINAS

### PASO 5: Página de Recomendaciones Personalizadas

```typescript
// filepath: src/app/(main)/para-ti/page.tsx
import { Metadata } from "next";
import { redirect } from "next/navigation";
import { auth } from "@/lib/auth";
import { PersonalizedFeed } from "@/components/recommendations/PersonalizedFeed";
import { RecommendationFilters } from "@/components/recommendations/RecommendationFilters";

export const metadata: Metadata = {
  title: "Para Ti | OKLA",
  description: "Vehículos recomendados personalmente para ti",
};

export default async function ParaTiPage() {
  const session = await auth();

  if (!session?.user) {
    redirect("/login?callbackUrl=/para-ti");
  }

  return (
    <div className="max-w-7xl mx-auto px-4 py-8">
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">
          Recomendado para ti
        </h1>
        <p className="text-gray-600">
          Vehículos seleccionados basados en tu actividad y preferencias
        </p>
      </div>

      {/* Filters */}
      <div className="mb-6">
        <RecommendationFilters />
      </div>

      {/* Feed */}
      <PersonalizedFeed />
    </div>
  );
}
```

---

## 🪝 HOOKS Y SERVICIOS

### PASO 6: useRecommendations Hook

```typescript
// filepath: src/lib/hooks/useRecommendations.ts
import {
  useQuery,
  useInfiniteQuery,
  useMutation,
  useQueryClient,
} from "@tanstack/react-query";
import { recommendationService } from "@/lib/services/recommendationService";
import { toast } from "sonner";
import type {
  RecommendationType,
  RecommendationParams,
  RecommendationFeedback,
} from "@/types/recommendation";

export function useRecommendations(params: RecommendationParams) {
  return useQuery({
    queryKey: ["recommendations", params],
    queryFn: () => recommendationService.getRecommendations(params),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

export function useInfiniteRecommendations(type: RecommendationType) {
  return useInfiniteQuery({
    queryKey: ["recommendations", "infinite", type],
    queryFn: ({ pageParam = 0 }) =>
      recommendationService.getRecommendations({
        type,
        offset: pageParam,
        limit: 12,
      }),
    getNextPageParam: (lastPage, pages) => {
      if (lastPage.recommendations.length < 12) return undefined;
      return pages.length * 12;
    },
    initialPageParam: 0,
  });
}

export function useRecommendationFeedback() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (feedback: RecommendationFeedback) =>
      recommendationService.sendFeedback(feedback),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["recommendations"] });
      toast.success("Gracias por tu feedback");
    },
  });
}

export function useTrackRecommendationEvent() {
  return useMutation({
    mutationFn: (event: {
      eventType: string;
      vehicleId: string;
      context?: any;
    }) => recommendationService.trackEvent(event),
  });
}
```

---

### PASO 7: recommendationService API Client

```typescript
// filepath: src/lib/services/recommendationService.ts
import { apiClient } from "./apiClient";
import type {
  Recommendation,
  RecommendationParams,
  RecommendationFeedback,
  RecommendationExplanation,
} from "@/types/recommendation";

export const recommendationService = {
  async getRecommendations(params: RecommendationParams) {
    const { type, vehicleId, searchContext, limit = 10, offset = 0 } = params;

    let endpoint = "/recommendations/personalized";
    const queryParams = new URLSearchParams();
    queryParams.append("limit", limit.toString());
    queryParams.append("offset", offset.toString());

    switch (type) {
      case "personalized":
        endpoint = "/recommendations/personalized";
        break;
      case "similar":
        endpoint = `/recommendations/similar/${vehicleId}`;
        break;
      case "collaborative":
        endpoint = "/recommendations/collaborative";
        break;
      case "trending":
        endpoint = "/recommendations/trending";
        break;
      case "context":
        endpoint = "/recommendations/context";
        if (searchContext) {
          queryParams.append("context", JSON.stringify(searchContext));
        }
        break;
    }

    const { data } = await apiClient.get<{ recommendations: Recommendation[] }>(
      `${endpoint}?${queryParams}`,
    );
    return data;
  },

  async getExplanation(vehicleId: string) {
    const { data } = await apiClient.get<RecommendationExplanation>(
      `/recommendations/explain/${vehicleId}`,
    );
    return data;
  },

  async sendFeedback(feedback: RecommendationFeedback) {
    await apiClient.post("/recommendations/feedback", feedback);
  },

  async trackEvent(event: {
    eventType: string;
    vehicleId: string;
    context?: any;
  }) {
    await apiClient.post("/recommendations/track-event", event);
  },

  async getStats() {
    const { data } = await apiClient.get("/recommendations/stats");
    return data;
  },
};
```

---

## 🧮 ALGORITMOS

### Content-Based Filtering (Similarity Score)

```typescript
// filepath: docs/algorithms/content-based-similarity.ts

/**
 * Calcula similarity score entre dos vehículos usando features categóricas y numéricas
 */
function calculateSimilarityScore(
  vehicleA: Vehicle,
  vehicleB: Vehicle,
): number {
  let score = 0;
  let totalWeight = 0;

  // Categorical features (exact match)
  const categoricalWeights = {
    make: 0.25,
    bodyType: 0.2,
    fuelType: 0.1,
    transmission: 0.05,
  };

  for (const [feature, weight] of Object.entries(categoricalWeights)) {
    if (vehicleA[feature] === vehicleB[feature]) {
      score += weight;
    }
    totalWeight += weight;
  }

  // Numerical features (normalized distance)
  const numericalFeatures = [
    { key: "price", weight: 0.2, maxDiff: 50000 },
    { key: "year", weight: 0.1, maxDiff: 10 },
    { key: "mileage", weight: 0.1, maxDiff: 100000 },
  ];

  for (const { key, weight, maxDiff } of numericalFeatures) {
    const diff = Math.abs(vehicleA[key] - vehicleB[key]);
    const normalizedDiff = Math.min(diff / maxDiff, 1);
    const featureScore = (1 - normalizedDiff) * weight;
    score += featureScore;
    totalWeight += weight;
  }

  return score / totalWeight; // Normalize to 0-1
}
```

### Collaborative Filtering (Matrix Factorization)

```python
# filepath: docs/algorithms/collaborative-filtering.py

import numpy as np
from scipy.sparse.linalg import svds

def matrix_factorization_cf(user_item_matrix, k=50):
    """
    Alternating Least Squares (ALS) para CF

    Args:
        user_item_matrix: Sparse matrix (users x items)
        k: Number of latent factors

    Returns:
        user_factors, item_factors
    """
    # SVD decomposition
    U, sigma, Vt = svds(user_item_matrix, k=k)

    # Construct diagonal matrix
    sigma = np.diag(sigma)

    # User and Item embeddings
    user_factors = np.dot(U, sigma)
    item_factors = Vt.T

    return user_factors, item_factors

def predict_rating(user_id, item_id, user_factors, item_factors):
    """
    Predict rating for user-item pair
    """
    return np.dot(user_factors[user_id], item_factors[item_id])

def get_top_n_recommendations(user_id, user_factors, item_factors, n=10):
    """
    Get top N recommendations for user
    """
    user_vector = user_factors[user_id]
    scores = np.dot(item_factors, user_vector)
    top_indices = np.argsort(scores)[::-1][:n]

    return top_indices, scores[top_indices]
```

---

## 📦 TIPOS TYPESCRIPT

### PASO 8: Tipos de Recommendation

```typescript
// filepath: src/types/recommendation.ts
import type { Vehicle } from "./vehicle";

export type RecommendationType =
  | "personalized"
  | "similar"
  | "collaborative"
  | "trending"
  | "context";

export interface Recommendation {
  vehicleId: string;
  vehicle: Vehicle;
  score: number; // 0-1
  reason:
    | "based_on_searches"
    | "similar_to_viewed"
    | "similar_to_favorites"
    | "popular"
    | "trending"
    | "collaborative"
    | "context";
  explanation: string;
  matchingFeatures?: string[]; // For similar vehicles
  metadata?: {
    modelVersion: string;
    timestamp: string;
    computeTimeMs: number;
  };
}

export interface RecommendationParams {
  type: RecommendationType;
  vehicleId?: string; // For "similar"
  searchContext?: any; // For "context"
  limit?: number;
  offset?: number;
}

export interface RecommendationFeedback {
  vehicleId: string;
  recommendationType: string;
  feedback: "up" | "down";
  reason?: string;
}

export interface RecommendationExplanation {
  vehicleId: string;
  reasons: Array<{
    type: string;
    weight: number;
    description: string;
  }>;
  features: {
    userPreferences: Record<string, any>;
    vehicleAttributes: Record<string, any>;
    matchScore: number;
  };
}

export interface RecommendationStats {
  totalRecommendations: number;
  ctr: number; // Click-through rate
  conversionRate: number;
  avgRelevanceScore: number;
  modelPerformance: {
    precision: number;
    recall: number;
    f1Score: number;
  };
  byType: Record<
    RecommendationType,
    {
      count: number;
      ctr: number;
      avgScore: number;
    }
  >;
}
```

---

## ✅ VALIDACIÓN

```bash
pnpm dev

# Verificar:
# - /para-ti muestra recomendaciones personalizadas (requiere login)
# - Infinite scroll carga más recomendaciones
# - Badge de explicación aparece en hover
# - Similar vehicles carousel funciona (scroll horizontal)
# - Feedback thumbs up/down funciona
# - Click en vehículo trackea evento analytics
# - Recomendaciones cambian según interacciones
# - "Trending" muestra vehículos populares
# - "Porque buscaste X" muestra contexto de búsqueda
# - Filtros de recomendaciones funcionan
```

---

## 🎯 MÉTRICAS Y OPTIMIZACIÓN

### KPIs Clave

1. **Click-Through Rate (CTR)**: % de recomendaciones clickeadas
2. **Conversion Rate**: % de contactos/compras desde recomendaciones
3. **Diversity**: Variedad de resultados (evitar filter bubble)
4. **Serendipity**: % de descubrimientos inesperados pero relevantes
5. **Precision@K**: Precisión en top K resultados
6. **NDCG**: Normalized Discounted Cumulative Gain

### A/B Testing

```typescript
// Diferentes estrategias de ranking para A/B testing
const rankingStrategies = {
  A: "pure_cf", // Solo collaborative filtering
  B: "pure_content", // Solo content-based
  C: "hybrid_50_50", // 50% CF + 50% Content
  D: "hybrid_weighted", // Weighted by user history
  E: "contextual_bandit", // Thompson sampling
};
```

### Estrategias de Diversificación

```typescript
function diversifyRecommendations(
  recommendations: Recommendation[],
  diversityWeight = 0.3,
): Recommendation[] {
  const diversified: Recommendation[] = [];
  const selectedBrands = new Set<string>();
  const selectedBodyTypes = new Set<string>();

  for (const rec of recommendations) {
    const brand = rec.vehicle.make;
    const bodyType = rec.vehicle.bodyType;

    // Penalizar duplicados
    let diversityScore = 1.0;
    if (selectedBrands.has(brand)) diversityScore -= 0.3;
    if (selectedBodyTypes.has(bodyType)) diversityScore -= 0.2;

    // Re-score
    const finalScore =
      rec.score * (1 - diversityWeight) + diversityScore * diversityWeight;

    diversified.push({ ...rec, score: finalScore });

    selectedBrands.add(brand);
    selectedBodyTypes.add(bodyType);
  }

  return diversified.sort((a, b) => b.score - a.score);
}
```

---

## 🚀 MEJORAS FUTURAS

### Deep Learning Models

1. **Neural Collaborative Filtering (NCF)**
   - Multi-layer perceptron para embeddings
   - GMF (Generalized Matrix Factorization)
   - NeuMF (Neural Matrix Factorization)

2. **Transformers para Recommendations**
   - BERT4Rec: Bidirectional encoder para secuencias
   - SASRec: Self-attention sequential recommendations

3. **Graph Neural Networks**
   - User-Vehicle-Dealer knowledge graph
   - GraphSAGE para propagación de features

### Contextual Features

- **Temporal**: Hora del día, día de la semana, estacionalidad
- **Device**: Desktop vs mobile (diferentes intents)
- **Location**: Recomendaciones geográficas
- **Weather**: Clima influye en tipo de vehículo buscado

### Real-Time Personalization

- **Streaming ML**: Actualización de modelos en tiempo real
- **Online Learning**: Modelos que aprenden continuamente
- **Reinforcement Learning**: Contextual bandits, Thompson sampling

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/recomendaciones.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsUser } from "../helpers/auth";

test.describe("Recomendaciones", () => {
  test("debe mostrar recomendaciones en homepage (anónimo)", async ({
    page,
  }) => {
    await page.goto("/");

    await expect(page.getByTestId("recommendations-section")).toBeVisible();
    await expect(
      page.getByText(/recomendados para ti|populares/i),
    ).toBeVisible();
  });

  test.describe("Usuario Autenticado", () => {
    test.beforeEach(async ({ page }) => {
      await loginAsUser(page);
    });

    test("debe mostrar recomendaciones personalizadas", async ({ page }) => {
      await page.goto("/");

      await expect(
        page.getByTestId("personalized-recommendations"),
      ).toBeVisible();
    });

    test("debe mostrar 'similares' en detalle de vehículo", async ({
      page,
    }) => {
      await page.goto("/vehiculos/toyota-camry-2023");

      await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
      await expect(page.getByTestId("similar-vehicles")).toBeVisible();
    });

    test("debe actualizar recomendaciones con interacciones", async ({
      page,
    }) => {
      await page.goto("/vehiculos");

      // Interactuar con algunos vehículos
      await page.getByTestId("vehicle-card").first().click();
      await page.goBack();

      // Volver a home y ver recomendaciones actualizadas
      await page.goto("/");
      await expect(page.getByTestId("recommendations-section")).toBeVisible();
    });

    test("debe marcar 'no me interesa'", async ({ page }) => {
      await page.goto("/");

      await page.getByTestId("recommendation-card").first().hover();
      await page.getByRole("button", { name: /no me interesa/i }).click();

      await expect(page.getByText(/preferencias guardadas/i)).toBeVisible();
    });
  });
});
```

---

**Siguiente documento:** `22-chatbot.md` - Chatbot integration con IA
