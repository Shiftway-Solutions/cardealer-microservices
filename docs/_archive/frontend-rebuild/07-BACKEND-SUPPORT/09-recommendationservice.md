# 🤖 RecommendationService - Documentación Frontend

> **Servicio:** RecommendationService  
> **Puerto:** 5101 (dev) / 8080 (k8s)  
> **Estado:** ✅ Implementado  
> **ML:** Collaborative filtering + Content-based  
> **Última actualización:** Enero 2026

---

## 📋 Descripción

Servicio de recomendaciones personalizadas usando machine learning. Genera sugerencias de vehículos basadas en comportamiento del usuario, preferencias y vehículos similares. Alimenta las secciones "Para Ti" y "Vehículos Similares".

---

## 🎯 Casos de Uso Frontend

### 1. Sección "Para Ti" en Homepage

```typescript
// Mostrar recomendaciones personalizadas
const ForYouSection = () => {
  const { data: recommendations, isLoading } = useForYouRecommendations(10);

  return (
    <section>
      <h2>Vehículos Para Ti</h2>
      <VehicleCarousel
        vehicles={recommendations?.map(r => r.vehicle)}
        onVehicleClick={(id, index) => {
          // Trackear click para mejorar recomendaciones
          recommendationService.markClicked(recommendations[index].id);
        }}
      />
    </section>
  );
};
```

### 2. Vehículos Similares

```typescript
// En página de detalle de vehículo
const SimilarVehicles = ({ vehicleId }: { vehicleId: string }) => {
  const { data: similar } = useSimilarVehicles(vehicleId, 6);

  return (
    <section>
      <h3>Vehículos Similares</h3>
      <VehicleGrid vehicles={similar?.map(r => r.vehicle)} />
    </section>
  );
};
```

### 3. Actualizar Preferencias

```typescript
// Cuando usuario indica preferencias explícitamente
const savePreferences = async (preferences: UserPreferences) => {
  await recommendationService.updatePreferences(preferences);
  // Regenerar recomendaciones con nuevas preferencias
  await recommendationService.generateRecommendations(20);
};
```

---

## 📡 API Endpoints

### Recommendations

| Método | Endpoint                                   | Descripción                    |
| ------ | ------------------------------------------ | ------------------------------ |
| `GET`  | `/api/recommendations/for-you`             | Recomendaciones personalizadas |
| `GET`  | `/api/recommendations/similar/{vehicleId}` | Vehículos similares            |
| `POST` | `/api/recommendations/generate`            | Generar nuevas recomendaciones |
| `POST` | `/api/recommendations/{id}/viewed`         | Marcar como vista              |
| `POST` | `/api/recommendations/{id}/clicked`        | Marcar como clickeada          |

### Preferences

| Método | Endpoint                           | Descripción             |
| ------ | ---------------------------------- | ----------------------- |
| `GET`  | `/api/recommendations/preferences` | Obtener preferencias    |
| `PUT`  | `/api/recommendations/preferences` | Actualizar preferencias |

### Interactions

| Método | Endpoint                          | Descripción                |
| ------ | --------------------------------- | -------------------------- |
| `POST` | `/api/interactions`               | Registrar interacción      |
| `GET`  | `/api/interactions/user/{userId}` | Historial de interacciones |

---

## 🔧 Cliente TypeScript

```typescript
// services/recommendationService.ts

import { apiClient } from "./apiClient";

// Tipos
interface Recommendation {
  id: string;
  userId: string;
  vehicle: RecommendedVehicle;
  score: number; // 0-1, relevancia
  reason: RecommendationReason;
  isViewed: boolean;
  isClicked: boolean;
  createdAt: string;
}

interface RecommendedVehicle {
  id: string;
  title: string;
  make: string;
  model: string;
  year: number;
  price: number;
  imageUrl: string;
  slug: string;
}

type RecommendationReason =
  | "similar_to_viewed"
  | "similar_to_favorited"
  | "matches_preferences"
  | "popular_in_area"
  | "price_drop"
  | "new_listing";

interface UserPreferences {
  makes?: string[];
  models?: string[];
  yearRange?: { min: number; max: number };
  priceRange?: { min: number; max: number };
  bodyTypes?: string[];
  fuelTypes?: string[];
  transmissions?: string[];
  colors?: string[];
  features?: string[];
}

interface Interaction {
  userId: string;
  vehicleId: string;
  type: "view" | "click" | "favorite" | "contact" | "share";
  duration?: number; // Tiempo en página (segundos)
  metadata?: Record<string, any>;
}

export const recommendationService = {
  // Recomendaciones para el usuario
  async getForYou(limit: number = 10): Promise<Recommendation[]> {
    const response = await apiClient.get("/api/recommendations/for-you", {
      params: { limit },
    });
    return response.data;
  },

  // Vehículos similares
  async getSimilar(
    vehicleId: string,
    limit: number = 6,
  ): Promise<Recommendation[]> {
    const response = await apiClient.get(
      `/api/recommendations/similar/${vehicleId}`,
      {
        params: { limit },
      },
    );
    return response.data;
  },

  // Generar nuevas recomendaciones
  async generateRecommendations(limit: number = 20): Promise<Recommendation[]> {
    const response = await apiClient.post("/api/recommendations/generate", {
      limit,
    });
    return response.data;
  },

  // Marcar como vista
  async markViewed(recommendationId: string): Promise<void> {
    await apiClient.post(`/api/recommendations/${recommendationId}/viewed`);
  },

  // Marcar como clickeada
  async markClicked(recommendationId: string): Promise<void> {
    await apiClient.post(`/api/recommendations/${recommendationId}/clicked`);
  },

  // Obtener preferencias
  async getPreferences(): Promise<UserPreferences> {
    const response = await apiClient.get("/api/recommendations/preferences");
    return response.data;
  },

  // Actualizar preferencias
  async updatePreferences(preferences: UserPreferences): Promise<void> {
    await apiClient.put("/api/recommendations/preferences", preferences);
  },

  // Registrar interacción
  async trackInteraction(
    interaction: Omit<Interaction, "userId">,
  ): Promise<void> {
    await apiClient.post("/api/interactions", interaction);
  },

  // Helper: Obtener razón legible
  getReasonText(reason: RecommendationReason): string {
    const reasons: Record<RecommendationReason, string> = {
      similar_to_viewed: "Similar a lo que has visto",
      similar_to_favorited: "Similar a tus favoritos",
      matches_preferences: "Basado en tus preferencias",
      popular_in_area: "Popular en tu zona",
      price_drop: "Bajó de precio",
      new_listing: "Recién publicado",
    };
    return reasons[reason];
  },
};
```

---

## 🪝 Hooks de React

```typescript
// hooks/useRecommendations.ts

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { recommendationService } from "../services/recommendationService";

export function useForYouRecommendations(limit: number = 10) {
  return useQuery({
    queryKey: ["recommendations", "for-you", limit],
    queryFn: () => recommendationService.getForYou(limit),
    staleTime: 5 * 60 * 1000, // 5 minutos
  });
}

export function useSimilarVehicles(vehicleId: string, limit: number = 6) {
  return useQuery({
    queryKey: ["recommendations", "similar", vehicleId, limit],
    queryFn: () => recommendationService.getSimilar(vehicleId, limit),
    enabled: !!vehicleId,
  });
}

export function useUserPreferences() {
  const queryClient = useQueryClient();

  const { data: preferences, isLoading } = useQuery({
    queryKey: ["user-preferences"],
    queryFn: () => recommendationService.getPreferences(),
  });

  const updatePreferences = useMutation({
    mutationFn: recommendationService.updatePreferences,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["user-preferences"] });
      queryClient.invalidateQueries({ queryKey: ["recommendations"] });
    },
  });

  return { preferences, isLoading, updatePreferences };
}

export function useRecommendationTracking() {
  const markViewed = useMutation({
    mutationFn: recommendationService.markViewed,
  });

  const markClicked = useMutation({
    mutationFn: recommendationService.markClicked,
  });

  return { markViewed, markClicked };
}

// Hook para tracking automático de interacciones
export function useInteractionTracking(vehicleId: string) {
  const startTime = useRef(Date.now());

  useEffect(() => {
    // Track view
    recommendationService.trackInteraction({
      vehicleId,
      type: "view",
    });

    // Track duration on unmount
    return () => {
      const duration = Math.floor((Date.now() - startTime.current) / 1000);
      if (duration > 5) {
        // Solo si estuvo más de 5 segundos
        recommendationService.trackInteraction({
          vehicleId,
          type: "view",
          duration,
        });
      }
    };
  }, [vehicleId]);
}
```

---

## 🧩 Componentes de Ejemplo

### For You Section

```tsx
// components/ForYouSection.tsx

import {
  useForYouRecommendations,
  useRecommendationTracking,
} from "../hooks/useRecommendations";
import { recommendationService } from "../services/recommendationService";

export function ForYouSection() {
  const {
    data: recommendations,
    isLoading,
    error,
  } = useForYouRecommendations(10);
  const { markClicked, markViewed } = useRecommendationTracking();

  // Marcar como vistas cuando aparecen en viewport
  const handleVisible = (recommendationId: string) => {
    markViewed.mutate(recommendationId);
  };

  const handleClick = (recommendation: Recommendation) => {
    markClicked.mutate(recommendation.id);
    navigate(`/vehicles/${recommendation.vehicle.slug}`);
  };

  if (isLoading) return <VehicleCarouselSkeleton count={5} />;
  if (error || !recommendations?.length) return null;

  return (
    <section className="py-8">
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-2xl font-bold">Para Ti ✨</h2>
        <Link to="/recommendations" className="text-blue-600">
          Ver todos
        </Link>
      </div>

      <div className="flex gap-4 overflow-x-auto pb-4">
        {recommendations.map((rec, index) => (
          <IntersectionObserver
            key={rec.id}
            onVisible={() => handleVisible(rec.id)}
          >
            <VehicleCard
              vehicle={rec.vehicle}
              onClick={() => handleClick(rec)}
              badge={
                <span className="text-xs bg-purple-100 text-purple-800 px-2 py-1 rounded">
                  {recommendationService.getReasonText(rec.reason)}
                </span>
              }
            />
          </IntersectionObserver>
        ))}
      </div>
    </section>
  );
}
```

### Preferences Form

```tsx
// components/PreferencesForm.tsx

import { useUserPreferences } from "../hooks/useRecommendations";

export function PreferencesForm() {
  const { preferences, isLoading, updatePreferences } = useUserPreferences();
  const [formData, setFormData] = useState<UserPreferences>(preferences || {});

  useEffect(() => {
    if (preferences) setFormData(preferences);
  }, [preferences]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await updatePreferences.mutateAsync(formData);
    toast.success("Preferencias actualizadas");
  };

  if (isLoading) return <Spinner />;

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div>
        <label className="block font-medium mb-2">Marcas favoritas</label>
        <MultiSelect
          options={makes}
          value={formData.makes || []}
          onChange={(makes) => setFormData((prev) => ({ ...prev, makes }))}
          placeholder="Selecciona marcas..."
        />
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block font-medium mb-2">Precio mínimo</label>
          <Input
            type="number"
            value={formData.priceRange?.min || ""}
            onChange={(e) =>
              setFormData((prev) => ({
                ...prev,
                priceRange: { ...prev.priceRange, min: Number(e.target.value) },
              }))
            }
          />
        </div>
        <div>
          <label className="block font-medium mb-2">Precio máximo</label>
          <Input
            type="number"
            value={formData.priceRange?.max || ""}
            onChange={(e) =>
              setFormData((prev) => ({
                ...prev,
                priceRange: { ...prev.priceRange, max: Number(e.target.value) },
              }))
            }
          />
        </div>
      </div>

      <div>
        <label className="block font-medium mb-2">Tipo de carrocería</label>
        <CheckboxGroup
          options={["Sedán", "SUV", "Pickup", "Hatchback", "Coupé"]}
          value={formData.bodyTypes || []}
          onChange={(bodyTypes) =>
            setFormData((prev) => ({ ...prev, bodyTypes }))
          }
        />
      </div>

      <Button type="submit" disabled={updatePreferences.isPending}>
        {updatePreferences.isPending ? "Guardando..." : "Guardar Preferencias"}
      </Button>
    </form>
  );
}
```

---

## 🧪 Testing

### Vitest Mocks

```typescript
// __mocks__/recommendationService.ts
export const recommendationService = {
  getForYou: vi.fn().mockResolvedValue([
    {
      id: "rec-1",
      vehicle: { id: "v-1", title: "Toyota Camry 2024", price: 35000 },
      score: 0.95,
      reason: "matches_preferences",
      isViewed: false,
      isClicked: false,
    },
  ]),
  getSimilar: vi.fn().mockResolvedValue([]),
  markViewed: vi.fn().mockResolvedValue(undefined),
  markClicked: vi.fn().mockResolvedValue(undefined),
  getPreferences: vi
    .fn()
    .mockResolvedValue({ makes: ["Toyota"], bodyTypes: ["SUV"] }),
  updatePreferences: vi.fn().mockResolvedValue(undefined),
  getReasonText: vi.fn().mockReturnValue("Basado en tus preferencias"),
};
```

### E2E Test (Playwright)

```typescript
// e2e/recommendations.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Recommendations", () => {
  test("should show personalized recommendations", async ({ page }) => {
    await page.goto("/");

    // Buscar sección "Para Ti"
    const forYouSection = page.locator('h2:has-text("Para Ti")');
    await expect(forYouSection).toBeVisible();

    // Verificar que hay vehículos
    const vehicles = page.locator('[data-testid="recommendation-card"]');
    await expect(vehicles).toHaveCount.greaterThan(0);
  });

  test("should show similar vehicles on detail page", async ({ page }) => {
    await page.goto("/vehicles/toyota-camry-2024");

    const similarSection = page.locator('h3:has-text("Vehículos Similares")');
    await expect(similarSection).toBeVisible();
  });
});
```

---

## 📊 Algoritmos de Recomendación

| Tipo                        | Descripción                               | Peso |
| --------------------------- | ----------------------------------------- | ---- |
| **Collaborative Filtering** | Usuarios con gustos similares             | 40%  |
| **Content-Based**           | Atributos similares (marca, precio, tipo) | 35%  |
| **Popularity**              | Tendencias y populares en zona            | 15%  |
| **Recency**                 | Listados recientes                        | 10%  |

---

## 🔗 Referencias

- [EventTrackingService](./07-eventtrackingservice.md)
- [Data ML Strategy](../../docs/DATA_ML_MICROSERVICES_STRATEGY.md)
- [Homepage Sections](../04-PAGINAS/01-PUBLICO/)

---

_Las recomendaciones mejoran con más interacciones. El tracking es crucial._
