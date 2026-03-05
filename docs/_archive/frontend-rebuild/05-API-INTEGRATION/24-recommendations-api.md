# 🎯 24 - Recommendations API

**Servicio:** RecommendationService  
**Puerto:** 8080  
**Base Path:** `/api/recommendations`, `/api/interactions`  
**Autenticación:** ✅ Parcial

---

## 📖 Descripción

Sistema de recomendaciones personalizadas basado en comportamiento del usuario. Incluye:

- Recomendaciones "Para Ti" basadas en historial
- Vehículos similares
- Tracking de interacciones
- Preferencias de usuario

---

## 🎯 Endpoints Disponibles

### RecommendationsController

| #   | Método | Endpoint                                   | Auth | Descripción                    |
| --- | ------ | ------------------------------------------ | ---- | ------------------------------ |
| 1   | `GET`  | `/api/recommendations/for-you`             | ✅   | Recomendaciones personalizadas |
| 2   | `GET`  | `/api/recommendations/similar/{vehicleId}` | ❌   | Vehículos similares            |
| 3   | `POST` | `/api/recommendations/generate`            | ✅   | Generar nuevas recomendaciones |
| 4   | `POST` | `/api/recommendations/{id}/viewed`         | ✅   | Marcar como vista              |
| 5   | `POST` | `/api/recommendations/{id}/clicked`        | ✅   | Marcar como clickeada          |
| 6   | `GET`  | `/api/recommendations/preferences`         | ✅   | Obtener preferencias           |

### InteractionsController

| #   | Método | Endpoint                      | Auth | Descripción           |
| --- | ------ | ----------------------------- | ---- | --------------------- |
| 7   | `POST` | `/api/interactions`           | ✅   | Registrar interacción |
| 8   | `POST` | `/api/interactions/anonymous` | ❌   | Interacción anónima   |

---

## 📝 Detalle de Endpoints

### 1. GET `/api/recommendations/for-you` - Personalizadas

**Query Params:**

- `limit` (int): Número de recomendaciones (default: 10)

**Response 200:**

```json
[
  {
    "id": "rec-001",
    "vehicleId": "vehicle-123",
    "score": 0.95,
    "reason": "Similar a vehículos que has visto",
    "vehicle": {
      "id": "vehicle-123",
      "title": "Toyota Camry 2024",
      "price": 1800000,
      "imageUrl": "https://...",
      "make": "Toyota",
      "model": "Camry",
      "year": 2024
    },
    "createdAt": "2026-01-30T10:00:00Z"
  }
]
```

---

### 2. GET `/api/recommendations/similar/{vehicleId}` - Similares

**Query Params:**

- `limit` (int): Número de resultados (default: 10)

**Response 200:**

```json
[
  {
    "id": "rec-002",
    "vehicleId": "vehicle-456",
    "score": 0.88,
    "reason": "Mismo segmento y rango de precio",
    "vehicle": {
      "id": "vehicle-456",
      "title": "Honda Accord 2024",
      "price": 1750000,
      "imageUrl": "https://...",
      "make": "Honda",
      "model": "Accord",
      "year": 2024
    }
  }
]
```

---

### 6. GET `/api/recommendations/preferences` - Preferencias

**Response 200:**

```json
{
  "userId": "user-123",
  "preferredMakes": ["Toyota", "Honda", "Hyundai"],
  "preferredBodyTypes": ["Sedan", "SUV"],
  "priceRange": {
    "min": 1000000,
    "max": 2500000
  },
  "yearRange": {
    "min": 2020,
    "max": 2025
  },
  "preferredFeatures": ["Leather", "Sunroof", "BackupCamera"],
  "lastUpdated": "2026-01-28T10:00:00Z"
}
```

---

### 7. POST `/api/interactions` - Registrar Interacción

**Request:**

```json
{
  "vehicleId": "vehicle-123",
  "type": "View",
  "durationSeconds": 45,
  "source": "SearchResults"
}
```

**Tipos de interacción:**

- `View` - Vista del listado
- `DetailView` - Vista de detalle
- `Contact` - Contacto al vendedor
- `Favorite` - Agregado a favoritos
- `Share` - Compartido
- `Compare` - Agregado a comparación

**Sources:**

- `SearchResults` - Desde resultados de búsqueda
- `HomePage` - Desde homepage
- `Recommendations` - Desde recomendaciones
- `Similar` - Desde vehículos similares
- `Direct` - Acceso directo

**Response 200:**

```json
{
  "id": "interaction-789",
  "userId": "user-123",
  "vehicleId": "vehicle-123",
  "type": "View",
  "durationSeconds": 45,
  "source": "SearchResults",
  "createdAt": "2026-01-30T10:00:00Z"
}
```

---

## 🔧 TypeScript Types

```typescript
// ============================================================================
// RECOMMENDATION TYPES
// ============================================================================

export interface Recommendation {
  id: string;
  vehicleId: string;
  score: number;
  reason: string;
  vehicle: RecommendedVehicle;
  isViewed: boolean;
  isClicked: boolean;
  createdAt: string;
}

export interface RecommendedVehicle {
  id: string;
  title: string;
  price: number;
  imageUrl: string;
  make: string;
  model: string;
  year: number;
  mileage?: number;
  condition?: string;
}

// ============================================================================
// USER PREFERENCES
// ============================================================================

export interface UserPreferences {
  userId: string;
  preferredMakes: string[];
  preferredBodyTypes: string[];
  priceRange: {
    min: number;
    max: number;
  };
  yearRange: {
    min: number;
    max: number;
  };
  preferredFeatures: string[];
  lastUpdated: string;
}

// ============================================================================
// INTERACTIONS
// ============================================================================

export type InteractionType =
  | "View"
  | "DetailView"
  | "Contact"
  | "Favorite"
  | "Share"
  | "Compare";

export type InteractionSource =
  | "SearchResults"
  | "HomePage"
  | "Recommendations"
  | "Similar"
  | "Direct";

export interface TrackInteractionRequest {
  vehicleId: string;
  type: InteractionType;
  durationSeconds?: number;
  source?: InteractionSource;
}

export interface VehicleInteraction {
  id: string;
  userId: string;
  vehicleId: string;
  type: InteractionType;
  durationSeconds?: number;
  source?: InteractionSource;
  createdAt: string;
}

// ============================================================================
// REQUESTS
// ============================================================================

export interface GenerateRecommendationsRequest {
  limit?: number;
}
```

---

## 📡 Service Layer

```typescript
// src/services/recommendationService.ts
import { apiClient } from "./api-client";
import type {
  Recommendation,
  UserPreferences,
  TrackInteractionRequest,
  VehicleInteraction,
  GenerateRecommendationsRequest,
} from "@/types/recommendation";

class RecommendationService {
  // ============================================================================
  // RECOMMENDATIONS
  // ============================================================================

  async getForYou(limit: number = 10): Promise<Recommendation[]> {
    const response = await apiClient.get<Recommendation[]>(
      `/api/recommendations/for-you?limit=${limit}`,
    );
    return response.data;
  }

  async getSimilar(
    vehicleId: string,
    limit: number = 10,
  ): Promise<Recommendation[]> {
    const response = await apiClient.get<Recommendation[]>(
      `/api/recommendations/similar/${vehicleId}?limit=${limit}`,
    );
    return response.data;
  }

  async generate(
    request: GenerateRecommendationsRequest,
  ): Promise<Recommendation[]> {
    const response = await apiClient.post<Recommendation[]>(
      "/api/recommendations/generate",
      request,
    );
    return response.data;
  }

  async markViewed(recommendationId: string): Promise<void> {
    await apiClient.post(`/api/recommendations/${recommendationId}/viewed`);
  }

  async markClicked(recommendationId: string): Promise<void> {
    await apiClient.post(`/api/recommendations/${recommendationId}/clicked`);
  }

  async getPreferences(): Promise<UserPreferences> {
    const response = await apiClient.get<UserPreferences>(
      "/api/recommendations/preferences",
    );
    return response.data;
  }

  // ============================================================================
  // INTERACTIONS
  // ============================================================================

  async trackInteraction(
    request: TrackInteractionRequest,
  ): Promise<VehicleInteraction> {
    const response = await apiClient.post<VehicleInteraction>(
      "/api/interactions",
      request,
    );
    return response.data;
  }

  async trackAnonymousInteraction(
    request: TrackInteractionRequest,
  ): Promise<void> {
    await apiClient.post("/api/interactions/anonymous", request);
  }
}

export const recommendationService = new RecommendationService();
```

---

## 🎣 React Query Hooks

```typescript
// src/hooks/useRecommendations.ts
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { recommendationService } from "@/services/recommendationService";
import type { TrackInteractionRequest } from "@/types/recommendation";

export const recommendationKeys = {
  all: ["recommendations"] as const,
  forYou: (limit: number) =>
    [...recommendationKeys.all, "for-you", limit] as const,
  similar: (vehicleId: string) =>
    [...recommendationKeys.all, "similar", vehicleId] as const,
  preferences: () => [...recommendationKeys.all, "preferences"] as const,
};

export function useRecommendationsForYou(limit: number = 10) {
  return useQuery({
    queryKey: recommendationKeys.forYou(limit),
    queryFn: () => recommendationService.getForYou(limit),
  });
}

export function useSimilarVehicles(vehicleId: string, limit: number = 10) {
  return useQuery({
    queryKey: recommendationKeys.similar(vehicleId),
    queryFn: () => recommendationService.getSimilar(vehicleId, limit),
    enabled: !!vehicleId,
  });
}

export function useUserPreferences() {
  return useQuery({
    queryKey: recommendationKeys.preferences(),
    queryFn: () => recommendationService.getPreferences(),
  });
}

export function useGenerateRecommendations() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: recommendationService.generate,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: recommendationKeys.all });
    },
  });
}

export function useMarkRecommendationViewed() {
  return useMutation({
    mutationFn: (id: string) => recommendationService.markViewed(id),
  });
}

export function useMarkRecommendationClicked() {
  return useMutation({
    mutationFn: (id: string) => recommendationService.markClicked(id),
  });
}

// ============================================================================
// INTERACTION TRACKING
// ============================================================================

export function useTrackInteraction() {
  return useMutation({
    mutationFn: (request: TrackInteractionRequest) =>
      recommendationService.trackInteraction(request),
  });
}

export function useTrackAnonymousInteraction() {
  return useMutation({
    mutationFn: (request: TrackInteractionRequest) =>
      recommendationService.trackAnonymousInteraction(request),
  });
}
```

---

## 🧩 Componente de Ejemplo

```typescript
// src/components/recommendations/ForYouSection.tsx
import { useRecommendationsForYou, useMarkRecommendationClicked } from "@/hooks/useRecommendations";
import { FiChevronRight } from "react-icons/fi";
import { Link } from "react-router-dom";

export const ForYouSection = () => {
  const { data: recommendations, isLoading } = useRecommendationsForYou(8);
  const markClicked = useMarkRecommendationClicked();

  if (isLoading) {
    return (
      <div className="grid grid-cols-4 gap-4">
        {[...Array(4)].map((_, i) => (
          <div key={i} className="animate-pulse bg-gray-100 h-64 rounded-lg" />
        ))}
      </div>
    );
  }

  if (!recommendations?.length) return null;

  const handleClick = (rec: Recommendation) => {
    markClicked.mutate(rec.id);
  };

  return (
    <section className="py-8">
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold">Para Ti</h2>
        <Link to="/recommendations" className="text-blue-600 flex items-center gap-1">
          Ver todos <FiChevronRight />
        </Link>
      </div>

      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {recommendations.map((rec) => (
          <Link
            key={rec.id}
            to={`/vehicles/${rec.vehicleId}`}
            onClick={() => handleClick(rec)}
            className="group"
          >
            <div className="border rounded-lg overflow-hidden hover:shadow-lg transition">
              <div className="aspect-video relative">
                <img
                  src={rec.vehicle.imageUrl}
                  alt={rec.vehicle.title}
                  className="w-full h-full object-cover group-hover:scale-105 transition"
                />
                <div className="absolute top-2 left-2 bg-blue-600 text-white text-xs px-2 py-1 rounded">
                  {Math.round(rec.score * 100)}% match
                </div>
              </div>
              <div className="p-3">
                <h3 className="font-medium truncate">{rec.vehicle.title}</h3>
                <p className="text-blue-600 font-bold">
                  ${rec.vehicle.price.toLocaleString()}
                </p>
                <p className="text-xs text-gray-500 mt-1">{rec.reason}</p>
              </div>
            </div>
          </Link>
        ))}
      </div>
    </section>
  );
};
```

---

## 🧩 Hook de Tracking Automático

```typescript
// src/hooks/useVehicleTracking.ts
import { useEffect, useRef } from "react";
import {
  useTrackInteraction,
  useTrackAnonymousInteraction,
} from "./useRecommendations";
import { useAuth } from "./useAuth";

export function useVehicleTracking(
  vehicleId: string,
  source: InteractionSource,
) {
  const { isAuthenticated } = useAuth();
  const trackInteraction = useTrackInteraction();
  const trackAnonymous = useTrackAnonymousInteraction();
  const startTime = useRef(Date.now());

  useEffect(() => {
    startTime.current = Date.now();

    // Track view on mount
    const request = { vehicleId, type: "View" as const, source };
    if (isAuthenticated) {
      trackInteraction.mutate(request);
    } else {
      trackAnonymous.mutate(request);
    }

    // Track duration on unmount
    return () => {
      const durationSeconds = Math.round(
        (Date.now() - startTime.current) / 1000,
      );
      if (durationSeconds > 5) {
        // Only track if viewed for more than 5 seconds
        const detailRequest = {
          vehicleId,
          type: "DetailView" as const,
          durationSeconds,
          source,
        };
        if (isAuthenticated) {
          trackInteraction.mutate(detailRequest);
        }
      }
    };
  }, [vehicleId]);
}
```

---

## 🎉 Resumen

✅ **8 Endpoints documentados**  
✅ **TypeScript Types** (Recommendation, Preferences, Interactions)  
✅ **Service Layer** (8 métodos)  
✅ **React Query Hooks** (8 hooks)  
✅ **Componente ejemplo** (ForYouSection)  
✅ **Hook de tracking** (useVehicleTracking)

---

_Última actualización: Enero 30, 2026_
