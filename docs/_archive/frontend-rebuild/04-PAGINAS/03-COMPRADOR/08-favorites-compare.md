---
title: "65 - Favorites & Compare Pages"
priority: P1
estimated_time: "2 horas"
dependencies: []
apis: ["VehiclesSaleService"]
status: partial
last_updated: "2026-01-30"
---

# 📋 65 - Favorites & Compare Pages

**Objetivo:** Gestión de favoritos y comparación de vehículos lado a lado.

**Prioridad:** P0 (Crítica)  
**Complejidad:** 🟡 Media  
**Dependencias:** VehiclesSaleService

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura](#-arquitectura)
2. [FavoritesPage](#-favoritespage)
3. [ComparePage](#-comparepage)
4. [Hooks](#-hooks)
5. [Servicios API](#-servicios-api)

---

## 🏗️ ARQUITECTURA

```
pages/
├── FavoritesPage.tsx               # Lista de favoritos (160 líneas)
└── vehicles/
    └── ComparePage.tsx             # Comparación de vehículos (349 líneas)

hooks/
├── useCompare.ts                   # Hook para gestión de comparación
└── useFavorites.ts                 # Hook para gestión de favoritos

services/
├── favoritesService.ts             # API de favoritos
└── vehicleService.ts               # Incluye compareVehicles()
```

---

## 📊 TIPOS

```typescript
// src/types/favorites.ts

export interface FavoriteVehicle {
  id: string;
  vehicleId: string;
  title: string;
  make: string;
  model: string;
  year: number;
  price: number;
  mileage?: number;
  fuelType?: string;
  transmission?: string;
  bodyStyle?: string;
  primaryImageUrl?: string;
  notes?: string;
  notifyPriceChanges?: boolean;
  addedAt: string;
}

// src/types/compare.ts

export interface CompareState {
  items: string[]; // Vehicle IDs
  maxItems: number;
}
```

---

## ❤️ FAVORITESPAGE

**Ruta:** `/favorites`

```typescript
// src/pages/FavoritesPage.tsx
import { useState, useEffect } from 'react';
import { FiHeart, FiTrash2, FiBell, FiEdit3, FiExternalLink } from 'react-icons/fi';
import MainLayout from '@/layouts/MainLayout';
import Button from '@/components/atoms/Button';
import EmptyState from '@/components/organisms/EmptyState';
import {
  getFavorites,
  removeFavorite as removeFavoriteApi,
  updateFavorite,
  type FavoriteVehicle,
} from '@/services/favoritesService';

export function FavoritesPage() {
  const [favorites, setFavorites] = useState<FavoriteVehicle[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadFavorites();
  }, []);

  const loadFavorites = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getFavorites();
      setFavorites(data);
    } catch (err) {
      console.error('Failed to load favorites:', err);
      setError('Error al cargar favoritos. Por favor, intenta de nuevo.');
    } finally {
      setLoading(false);
    }
  };

  const handleRemoveFavorite = async (vehicleId: string) => {
    if (!confirm('¿Estás seguro de eliminar este favorito?')) return;

    try {
      await removeFavoriteApi(vehicleId);
      setFavorites(favorites.filter((f) => f.id !== vehicleId));
    } catch (err) {
      console.error('Failed to remove favorite:', err);
      alert('Error al eliminar favorito.');
    }
  };

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('es-DO', {
      style: 'currency',
      currency: 'DOP',
      minimumFractionDigits: 0,
    }).format(price);
  };

  if (loading) {
    return (
      <MainLayout>
        <div className="container mx-auto px-4 py-8">
          <div className="text-center py-12">
            <div className="animate-spin h-12 w-12 border-4 border-blue-500 border-t-transparent rounded-full mx-auto" />
            <p className="mt-4 text-gray-600">Cargando favoritos...</p>
          </div>
        </div>
      </MainLayout>
    );
  }

  if (error) {
    return (
      <MainLayout>
        <div className="container mx-auto px-4 py-8">
          <div className="text-center py-12">
            <p className="text-red-600">{error}</p>
            <Button onClick={loadFavorites} className="mt-4">
              Reintentar
            </Button>
          </div>
        </div>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <div className="container mx-auto px-4 py-8">
        <div className="flex items-center justify-between mb-8">
          <div>
            <h1 className="text-3xl font-bold">Mis Favoritos</h1>
            <p className="text-gray-600 mt-2">
              {favorites.length}{' '}
              {favorites.length === 1 ? 'vehículo guardado' : 'vehículos guardados'}
            </p>
          </div>
        </div>

        {favorites.length === 0 ? (
          <EmptyState
            icon={<FiHeart className="text-gray-400" size={64} />}
            title="No tienes favoritos aún"
            message="Marca los vehículos que te gusten para verlos aquí más tarde"
            actionLabel="Buscar Vehículos"
            onAction={() => (window.location.href = '/search')}
          />
        ) : (
          <div className="space-y-4">
            {favorites.map((vehicle) => (
              <div
                key={vehicle.id}
                className="border rounded-lg overflow-hidden hover:shadow-md transition-shadow"
              >
                <div className="flex flex-col md:flex-row">
                  {/* Image */}
                  <div className="md:w-1/3">
                    <img
                      src={vehicle.primaryImageUrl || '/placeholder-vehicle.jpg'}
                      alt={vehicle.title}
                      className="w-full h-48 md:h-full object-cover"
                    />
                  </div>

                  {/* Content */}
                  <div className="flex-1 p-6">
                    <div className="flex justify-between items-start mb-4">
                      <div>
                        <h3 className="text-2xl font-bold mb-2">{vehicle.title}</h3>
                        <p className="text-gray-600">
                          {vehicle.year} • {vehicle.make} {vehicle.model} •{' '}
                          {vehicle.mileage?.toLocaleString() || 'N/A'} km
                        </p>
                      </div>
                      <p className="text-3xl font-bold text-blue-600">
                        {formatPrice(vehicle.price)}
                      </p>
                    </div>

                    {/* Vehicle Info */}
                    <div className="mb-4 flex gap-4 text-sm text-gray-600">
                      <span>{vehicle.fuelType}</span>
                      <span>•</span>
                      <span>{vehicle.transmission}</span>
                      <span>•</span>
                      <span>{vehicle.bodyStyle}</span>
                    </div>

                    {/* Actions */}
                    <div className="flex gap-2">
                      <Button
                        onClick={() => (window.location.href = `/vehicles/${vehicle.id}`)}
                        leftIcon={<FiExternalLink />}
                      >
                        Ver Detalles
                      </Button>
                      <Button
                        variant="outline"
                        onClick={() => handleRemoveFavorite(vehicle.id)}
                        leftIcon={<FiTrash2 />}
                      >
                        Eliminar
                      </Button>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </MainLayout>
  );
}
```

---

## ⚖️ COMPAREPAGE

**Ruta:** `/vehicles/compare`

```typescript
// src/pages/vehicles/ComparePage.tsx
import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import MainLayout from '@/layouts/MainLayout';
import Button from '@/components/atoms/Button';
import ComparisonTable from '@/components/organisms/ComparisonTable';
import { useCompare } from '@/hooks/useCompare';
import { getAllVehicles, compareVehicles } from '@/services/vehicleService';
import type { Vehicle } from '@/services/vehicleService';
import { FiX, FiPlus } from 'react-icons/fi';

export default function ComparePage() {
  const navigate = useNavigate();
  const { compareItems, removeFromCompare, clearCompare, canAddMore, addToCompare, maxItems } = useCompare();
  const [showSelector, setShowSelector] = useState(false);
  const [vehiclesToCompare, setVehiclesToCompare] = useState<Vehicle[]>([]);
  const [availableVehicles, setAvailableVehicles] = useState<Vehicle[]>([]);
  const [loading, setLoading] = useState(true);

  // Load vehicles to compare from backend
  useEffect(() => {
    const loadVehicles = async () => {
      try {
        setLoading(true);

        if (compareItems.length > 0) {
          const compared = await compareVehicles(compareItems);
          setVehiclesToCompare(compared);
        } else {
          setVehiclesToCompare([]);
        }

        // Fetch available vehicles for selector
        const result = await getAllVehicles({ page: 0, pageSize: 20 });
        const available = result.vehicles.filter((v: Vehicle) => !compareItems.includes(v.id));
        setAvailableVehicles(available);
      } catch (error) {
        console.error('Error loading vehicles:', error);
      } finally {
        setLoading(false);
      }
    };

    loadVehicles();
  }, [compareItems]);

  const handleAddVehicle = (vehicleId: string) => {
    addToCompare(vehicleId);
    setShowSelector(false);
  };

  if (loading) {
    return (
      <MainLayout>
        <div className="min-h-screen bg-gray-50 flex items-center justify-center py-12 px-4">
          <div className="text-center">
            <div className="w-16 h-16 border-4 border-primary border-t-transparent rounded-full animate-spin mx-auto mb-4" />
            <p className="text-gray-600">Cargando comparación...</p>
          </div>
        </div>
      </MainLayout>
    );
  }

  // Empty state
  if (compareItems.length === 0) {
    return (
      <MainLayout>
        <div className="min-h-screen bg-gray-50 flex items-center justify-center py-12 px-4">
          <div className="max-w-md text-center">
            <div className="w-20 h-20 bg-gray-200 rounded-full flex items-center justify-center mx-auto mb-6">
              <FiPlus size={40} className="text-gray-400" />
            </div>
            <h1 className="text-3xl font-bold text-gray-900 mb-4">
              No hay vehículos para comparar
            </h1>
            <p className="text-gray-600 mb-8">
              Agrega vehículos para comparar sus especificaciones, características y precios lado a lado.
            </p>
            <Button variant="primary" size="lg" onClick={() => navigate('/browse')}>
              Explorar Vehículos
            </Button>
          </div>
        </div>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <div className="bg-gray-50 min-h-screen py-8">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          {/* Header */}
          <div className="mb-8">
            <div className="flex items-center justify-between mb-4">
              <div>
                <h1 className="text-3xl sm:text-4xl font-bold text-gray-900 mb-2">
                  Comparar Vehículos
                </h1>
                <p className="text-gray-600">
                  Comparando {vehiclesToCompare.length} vehículo
                  {vehiclesToCompare.length !== 1 ? 's' : ''}
                </p>
              </div>
              <div className="flex gap-3">
                {canAddMore() && (
                  <Button variant="outline" onClick={() => setShowSelector(!showSelector)}>
                    <FiPlus className="mr-2" />
                    Agregar Vehículo
                  </Button>
                )}
                <Button
                  variant="outline"
                  onClick={() => {
                    if (confirm('¿Eliminar todos los vehículos de la comparación?')) {
                      clearCompare();
                    }
                  }}
                >
                  Limpiar Todo
                </Button>
              </div>
            </div>

            {/* Vehicle Selector */}
            {showSelector && canAddMore() && (
              <div className="bg-white rounded-xl shadow-card p-6 mb-6">
                <div className="flex items-center justify-between mb-4">
                  <h3 className="text-lg font-semibold text-gray-900">
                    Selecciona un Vehículo para Agregar
                  </h3>
                  <button
                    onClick={() => setShowSelector(false)}
                    className="text-gray-400 hover:text-gray-600"
                  >
                    <FiX size={24} />
                  </button>
                </div>
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 max-h-96 overflow-y-auto">
                  {availableVehicles.slice(0, 12).map((vehicle) => (
                    <button
                      key={vehicle.id}
                      onClick={() => handleAddVehicle(vehicle.id)}
                      className="flex items-center gap-3 p-3 border border-gray-200 rounded-lg hover:border-primary hover:bg-primary/5 transition-all text-left"
                    >
                      <img
                        src={vehicle.images[0]}
                        alt={`${vehicle.make} ${vehicle.model}`}
                        className="w-16 h-16 object-cover rounded"
                      />
                      <div className="flex-1 min-w-0">
                        <p className="font-semibold text-gray-900 truncate">
                          {vehicle.year} {vehicle.make} {vehicle.model}
                        </p>
                        <p className="text-sm text-primary font-semibold">
                          ${vehicle.price.toLocaleString()}
                        </p>
                      </div>
                    </button>
                  ))}
                </div>
              </div>
            )}
          </div>

          {/* Vehicle Cards Preview */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mb-8">
            {vehiclesToCompare.map((vehicle) => (
              <div key={vehicle.id} className="bg-white rounded-xl shadow-card overflow-hidden">
                <div className="relative">
                  <img
                    src={vehicle.images[0]}
                    alt={`${vehicle.make} ${vehicle.model}`}
                    className="w-full h-48 object-cover"
                  />
                  <button
                    onClick={() => removeFromCompare(vehicle.id)}
                    className="absolute top-3 right-3 p-2 bg-red-500 text-white rounded-lg hover:bg-red-600 transition-colors"
                    title="Remover de comparación"
                  >
                    <FiX size={20} />
                  </button>
                </div>
                <div className="p-4">
                  <h3 className="text-lg font-bold text-gray-900 mb-1">
                    {vehicle.year} {vehicle.make} {vehicle.model}
                  </h3>
                  <p className="text-2xl font-bold text-primary mb-2">
                    ${vehicle.price.toLocaleString()}
                  </p>
                  <div className="flex items-center gap-4 text-sm text-gray-600">
                    <span>{vehicle.mileage?.toLocaleString()} km</span>
                    <span>{vehicle.transmission}</span>
                    <span>{vehicle.fuelType}</span>
                  </div>
                </div>
              </div>
            ))}
          </div>

          {/* Comparison Table */}
          <ComparisonTable vehicles={vehiclesToCompare} />
        </div>
      </div>
    </MainLayout>
  );
}
```

---

## 🪝 HOOKS

```typescript
// src/hooks/useCompare.ts
import { useState, useEffect, useCallback } from "react";

const STORAGE_KEY = "vehicle-compare";
const MAX_ITEMS = 3;

export function useCompare() {
  const [compareItems, setCompareItems] = useState<string[]>(() => {
    const stored = localStorage.getItem(STORAGE_KEY);
    return stored ? JSON.parse(stored) : [];
  });

  // Sync with localStorage
  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(compareItems));
  }, [compareItems]);

  const addToCompare = useCallback((vehicleId: string) => {
    setCompareItems((prev) => {
      if (prev.includes(vehicleId) || prev.length >= MAX_ITEMS) return prev;
      return [...prev, vehicleId];
    });
  }, []);

  const removeFromCompare = useCallback((vehicleId: string) => {
    setCompareItems((prev) => prev.filter((id) => id !== vehicleId));
  }, []);

  const clearCompare = useCallback(() => {
    setCompareItems([]);
  }, []);

  const isInCompare = useCallback(
    (vehicleId: string) => compareItems.includes(vehicleId),
    [compareItems],
  );

  const canAddMore = useCallback(
    () => compareItems.length < MAX_ITEMS,
    [compareItems],
  );

  return {
    compareItems,
    addToCompare,
    removeFromCompare,
    clearCompare,
    isInCompare,
    canAddMore,
    maxItems: MAX_ITEMS,
    count: compareItems.length,
  };
}
```

---

## 🔌 SERVICIOS API

```typescript
// src/services/favoritesService.ts
import { api } from "@/lib/api";

export interface FavoriteVehicle {
  id: string;
  vehicleId: string;
  title: string;
  make: string;
  model: string;
  year: number;
  price: number;
  mileage?: number;
  fuelType?: string;
  transmission?: string;
  bodyStyle?: string;
  primaryImageUrl?: string;
  notes?: string;
  notifyPriceChanges?: boolean;
  addedAt: string;
}

export const getFavorites = async (): Promise<FavoriteVehicle[]> => {
  const response = await api.get("/api/favorites");
  return response.data;
};

export const addFavorite = async (vehicleId: string): Promise<void> => {
  await api.post("/api/favorites", { vehicleId });
};

export const removeFavorite = async (vehicleId: string): Promise<void> => {
  await api.delete(`/api/favorites/${vehicleId}`);
};

export const updateFavorite = async (
  vehicleId: string,
  data: { notes?: string; notifyPriceChanges?: boolean },
): Promise<void> => {
  await api.patch(`/api/favorites/${vehicleId}`, data);
};

// src/services/vehicleService.ts (compare methods)
export const compareVehicles = async (
  vehicleIds: string[],
): Promise<Vehicle[]> => {
  const response = await api.post("/api/vehicles/compare", { vehicleIds });
  return response.data;
};
```

---

## ✅ VALIDACIÓN

### Favorites Page

- [ ] Lista de favoritos del usuario
- [ ] Imagen y detalles de cada vehículo
- [ ] Precio formateado en DOP
- [ ] Botón ver detalles
- [ ] Botón eliminar con confirmación
- [ ] Empty state cuando no hay favoritos
- [ ] Loading state con spinner
- [ ] Error state con reintentar
- [ ] Responsive (horizontal en desktop, vertical en mobile)

### Compare Page

- [ ] Lista de vehículos a comparar (máx 3)
- [ ] Selector para agregar más vehículos
- [ ] Botón X para remover de comparación
- [ ] Grid de preview cards
- [ ] Tabla de comparación lado a lado
- [ ] Botón limpiar todo con confirmación
- [ ] Empty state cuando no hay vehículos
- [ ] Persistencia en localStorage
- [ ] Límite de 3 vehículos respetado

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/favorites-compare.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsUser } from "../helpers/auth";

test.describe("Favoritos y Comparar", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test.describe("Favoritos", () => {
    test("debe agregar a favoritos desde detalle", async ({ page }) => {
      await page.goto("/vehiculos/toyota-camry-2023");

      await page.getByRole("button", { name: /favorito/i }).click();
      await expect(page.getByText(/agregado a favoritos/i)).toBeVisible();
    });

    test("debe mostrar lista de favoritos", async ({ page }) => {
      await page.goto("/favoritos");

      await expect(page.getByTestId("favorites-list")).toBeVisible();
    });

    test("debe remover de favoritos", async ({ page }) => {
      await page.goto("/favoritos");

      const count = await page.getByTestId("favorite-item").count();
      await page.getByTestId("remove-favorite").first().click();

      await expect(page.getByTestId("favorite-item")).toHaveCount(count - 1);
    });
  });

  test.describe("Comparar", () => {
    test("debe agregar a comparación desde detalle", async ({ page }) => {
      await page.goto("/vehiculos/toyota-camry-2023");

      await page.getByRole("button", { name: /comparar/i }).click();
      await expect(page.getByText(/agregado a comparación/i)).toBeVisible();
    });

    test("debe mostrar página de comparación", async ({ page }) => {
      await page.goto("/comparar?v=id1,id2");

      await expect(page.getByTestId("comparison-table")).toBeVisible();
    });

    test("debe limitar a 3 vehículos", async ({ page }) => {
      await page.goto("/comparar?v=id1,id2,id3");

      await expect(
        page.getByRole("button", { name: /agregar/i }),
      ).toBeDisabled();
    });

    test("debe remover vehículo de comparación", async ({ page }) => {
      await page.goto("/comparar?v=id1,id2");

      await page.getByTestId("remove-from-compare").first().click();
      await expect(page.getByTestId("comparison-vehicle")).toHaveCount(1);
    });

    test("debe persistir en localStorage", async ({ page }) => {
      await page.goto("/vehiculos/toyota-camry-2023");
      await page.getByRole("button", { name: /comparar/i }).click();

      await page.reload();
      await page.goto("/comparar");

      await expect(page.getByText(/toyota camry/i)).toBeVisible();
    });
  });
});
```

---

_Última actualización: Enero 2026_
