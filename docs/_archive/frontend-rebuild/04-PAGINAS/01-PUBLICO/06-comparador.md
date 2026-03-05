---
title: "23. Comparador de Vehículos"
priority: P0
estimated_time: "2 horas"
dependencies: []
apis: []
status: complete
last_updated: "2026-01-30"
---

# 23. Comparador de Vehículos

**Objetivo:** Implementar herramienta avanzada de comparación lado a lado de hasta 3 vehículos, con análisis detallado de specs, pricing, features y recomendaciones inteligentes.

**Prioridad:** P2 (Baja - Mejora de decisión de compra)  
**Complejidad:** 🟡 Media (UI compleja, múltiples vistas)  
**Dependencias:** ComparisonService (✅ YA IMPLEMENTADO en Sprint 1), VehiclesService

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura](#arquitectura)
2. [Backend API](#backend-api)
3. [Componentes](#componentes)
4. [Páginas](#páginas)
5. [Hooks y Servicios](#hooks-y-servicios)
6. [Tipos TypeScript](#tipos-typescript)
7. [Validación](#validación)

---

## 🏗️ ARQUITECTURA

### Flujo de Comparación

```
Usuario agrega vehículo a comparación (3 máx)
    ↓
ComparisonService almacena selección
    ↓
Usuario navega a /comparar
    ↓
ComparisonTable renderiza tabla lado a lado
    ↓
Sistema calcula diferencias automáticamente
├─ Precio: % diferencia
├─ Specs: Highlight mejores valores
├─ Features: Check/X marks
└─ Ratings: Stars comparison
    ↓
Recomendaciones inteligentes
├─ "Mejor valor por dinero"
├─ "Más económico"
├─ "Mejor equipado"
└─ "Mayor reventa estimada"
    ↓
Acciones:
├─ Eliminar vehículo
├─ Compartir comparación (URL pública)
├─ Exportar PDF
└─ Contactar dealers
```

---

## 🔌 BACKEND API

### ComparisonService Endpoints (Ya Implementados ✅)

```typescript
// filepath: docs/backend/ComparisonService-API.md (Sprint 1)

GET    /api/comparisons                        # Comparaciones del usuario
GET    /api/comparisons/{id}                   # Comparación específica
POST   /api/comparisons                        # Crear comparación
PUT    /api/comparisons/{id}                   # Actualizar comparación
DELETE /api/comparisons/{id}                   # Eliminar comparación

POST   /api/comparisons/{id}/vehicles          # Agregar vehículo
DELETE /api/comparisons/{id}/vehicles/{vehicleId} # Eliminar vehículo

POST   /api/comparisons/{id}/share             # Generar link público
GET    /api/comparisons/shared/{token}         # Ver comparación pública

GET    /api/comparisons/recommendations        # Recomendaciones de comparación
```

---

## 🎨 COMPONENTES

### PASO 1: ComparisonTable - Tabla Principal

```typescript
// filepath: src/components/comparison/ComparisonTable.tsx
"use client";

import { useState } from "react";
import { X, Share2, Download, TrendingUp, DollarSign } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { ComparisonRow } from "./ComparisonRow";
import { VehicleImage } from "./VehicleImage";
import { ShareModal } from "./ShareModal";
import { useComparison } from "@/lib/hooks/useComparison";
import type { Vehicle } from "@/types/vehicle";

interface ComparisonTableProps {
  vehicles: Vehicle[];
  onRemoveVehicle: (vehicleId: string) => void;
}

export function ComparisonTable({ vehicles, onRemoveVehicle }: ComparisonTableProps) {
  const [showShareModal, setShowShareModal] = useState(false);
  const { generateShareLink, exportToPDF } = useComparison();

  const specs = [
    // Básicas
    { key: "year", label: "Año", type: "number" },
    { key: "make", label: "Marca", type: "text" },
    { key: "model", label: "Modelo", type: "text" },
    { key: "trim", label: "Versión", type: "text" },
    { key: "price", label: "Precio", type: "currency" },
    { key: "mileage", label: "Kilometraje", type: "number", suffix: " km" },

    // Motor y Performance
    { key: "engine", label: "Motor", type: "text" },
    { key: "horsepower", label: "Caballos de fuerza", type: "number", suffix: " HP" },
    { key: "torque", label: "Torque", type: "number", suffix: " lb-ft" },
    { key: "transmission", label: "Transmisión", type: "text" },
    { key: "drivetrain", label: "Tracción", type: "text" },
    { key: "fuelType", label: "Combustible", type: "text" },
    { key: "fuelEconomy", label: "Consumo", type: "text" },

    // Dimensiones
    { key: "bodyType", label: "Tipo de carrocería", type: "text" },
    { key: "doors", label: "Puertas", type: "number" },
    { key: "seats", label: "Asientos", type: "number" },
    { key: "length", label: "Largo", type: "number", suffix: " mm" },
    { key: "width", label: "Ancho", type: "number", suffix: " mm" },
    { key: "height", label: "Alto", type: "number", suffix: " mm" },
    { key: "wheelbase", label: "Distancia entre ejes", type: "number", suffix: " mm" },
    { key: "cargoCapacity", label: "Capacidad de carga", type: "number", suffix: " L" },

    // Features
    { key: "hasAirConditioning", label: "Aire acondicionado", type: "boolean" },
    { key: "hasLeatherSeats", label: "Asientos de cuero", type: "boolean" },
    { key: "hasSunroof", label: "Techo solar", type: "boolean" },
    { key: "hasNavigationSystem", label: "GPS", type: "boolean" },
    { key: "hasBackupCamera", label: "Cámara trasera", type: "boolean" },
    { key: "hasBlindSpotMonitoring", label: "Monitor punto ciego", type: "boolean" },
    { key: "hasLaneDepartureWarning", label: "Alerta cambio carril", type: "boolean" },
    { key: "hasAdaptiveCruiseControl", label: "Control crucero adaptativo", type: "boolean" },
    { key: "hasAutomaticEmergencyBraking", label: "Frenado automático", type: "boolean" },
    { key: "hasKeylessEntry", label: "Entrada sin llave", type: "boolean" },
    { key: "hasPushButtonStart", label: "Encendido por botón", type: "boolean" },
    { key: "hasRemoteStart", label: "Encendido remoto", type: "boolean" },

    // Audio y Conectividad
    { key: "hasBluetoothConnectivity", label: "Bluetooth", type: "boolean" },
    { key: "hasAppleCarPlay", label: "Apple CarPlay", type: "boolean" },
    { key: "hasAndroidAuto", label: "Android Auto", type: "boolean" },
    { key: "hasPremiumAudio", label: "Audio premium", type: "boolean" },
    { key: "hasWirelessCharging", label: "Carga inalámbrica", type: "boolean" },
  ];

  // Agrupar specs por categoría
  const categories = [
    {
      name: "Información Básica",
      specs: specs.filter((s) =>
        ["year", "make", "model", "trim", "price", "mileage"].includes(s.key)
      ),
    },
    {
      name: "Motor y Performance",
      specs: specs.filter((s) =>
        ["engine", "horsepower", "torque", "transmission", "drivetrain", "fuelType", "fuelEconomy"].includes(s.key)
      ),
    },
    {
      name: "Dimensiones",
      specs: specs.filter((s) =>
        ["bodyType", "doors", "seats", "length", "width", "height", "wheelbase", "cargoCapacity"].includes(s.key)
      ),
    },
    {
      name: "Seguridad",
      specs: specs.filter((s) =>
        ["hasBackupCamera", "hasBlindSpotMonitoring", "hasLaneDepartureWarning", "hasAdaptiveCruiseControl", "hasAutomaticEmergencyBraking"].includes(s.key)
      ),
    },
    {
      name: "Confort",
      specs: specs.filter((s) =>
        ["hasAirConditioning", "hasLeatherSeats", "hasSunroof", "hasKeylessEntry", "hasPushButtonStart", "hasRemoteStart"].includes(s.key)
      ),
    },
    {
      name: "Tecnología",
      specs: specs.filter((s) =>
        ["hasNavigationSystem", "hasBluetoothConnectivity", "hasAppleCarPlay", "hasAndroidAuto", "hasPremiumAudio", "hasWirelessCharging"].includes(s.key)
      ),
    },
  ];

  return (
    <div className="space-y-6">
      {/* Header with Actions */}
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold text-gray-900">
          Comparación de Vehículos ({vehicles.length}/3)
        </h2>
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => setShowShareModal(true)}
          >
            <Share2 size={16} className="mr-1" />
            Compartir
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => exportToPDF(vehicles)}
          >
            <Download size={16} className="mr-1" />
            Exportar PDF
          </Button>
        </div>
      </div>

      {/* Recommendations */}
      <ComparisonRecommendations vehicles={vehicles} />

      {/* Table */}
      <div className="bg-white rounded-xl border overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            {/* Vehicle Headers */}
            <thead>
              <tr className="border-b bg-gray-50">
                <th className="sticky left-0 z-20 bg-gray-50 p-4 text-left font-semibold text-gray-900 border-r w-64">
                  Característica
                </th>
                {vehicles.map((vehicle) => (
                  <th key={vehicle.id} className="p-4 min-w-[300px]">
                    <div className="space-y-3">
                      {/* Image */}
                      <div className="relative">
                        <VehicleImage
                          src={vehicle.images?.[0]}
                          alt={vehicle.name}
                          className="w-full h-40 object-cover rounded-lg"
                        />
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => onRemoveVehicle(vehicle.id)}
                          className="absolute top-2 right-2 bg-white/90 hover:bg-white"
                        >
                          <X size={16} />
                        </Button>
                      </div>

                      {/* Name & Price */}
                      <div>
                        <h3 className="font-bold text-lg text-gray-900">
                          {vehicle.name}
                        </h3>
                        <p className="text-2xl font-bold text-primary-600">
                          ${vehicle.price.toLocaleString()}
                        </p>
                      </div>
                    </div>
                  </th>
                ))}
              </tr>
            </thead>

            {/* Rows by Category */}
            <tbody>
              {categories.map((category) => (
                <>
                  {/* Category Header */}
                  <tr className="bg-gray-100">
                    <td
                      colSpan={vehicles.length + 1}
                      className="px-4 py-3 font-semibold text-gray-900"
                    >
                      {category.name}
                    </td>
                  </tr>

                  {/* Specs */}
                  {category.specs.map((spec) => (
                    <ComparisonRow
                      key={spec.key}
                      label={spec.label}
                      values={vehicles.map((v) => v[spec.key])}
                      type={spec.type}
                      suffix={spec.suffix}
                    />
                  ))}
                </>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Share Modal */}
      <ShareModal
        isOpen={showShareModal}
        onClose={() => setShowShareModal(false)}
        vehicles={vehicles}
      />
    </div>
  );
}
```

---

### PASO 2: ComparisonRow - Fila de Comparación

```typescript
// filepath: src/components/comparison/ComparisonRow.tsx
"use client";

import { Check, X, TrendingUp } from "lucide-react";
import { cn } from "@/lib/utils";

interface ComparisonRowProps {
  label: string;
  values: any[];
  type: "text" | "number" | "currency" | "boolean";
  suffix?: string;
}

export function ComparisonRow({ label, values, type, suffix = "" }: ComparisonRowProps) {
  // Find best value for highlighting
  const getBestValueIndex = () => {
    if (type === "boolean") {
      return values.findIndex((v) => v === true);
    }
    if (type === "number" || type === "currency") {
      const numValues = values.map((v) => (typeof v === "number" ? v : 0));
      const maxValue = Math.max(...numValues);
      return numValues.indexOf(maxValue);
    }
    return -1;
  };

  const bestIndex = getBestValueIndex();

  const formatValue = (value: any) => {
    if (value === null || value === undefined || value === "") {
      return <span className="text-gray-400 text-sm">N/A</span>;
    }

    switch (type) {
      case "boolean":
        return value ? (
          <Check size={20} className="text-green-600 mx-auto" />
        ) : (
          <X size={20} className="text-gray-300 mx-auto" />
        );

      case "currency":
        return (
          <span className="font-semibold">
            ${value.toLocaleString()}
          </span>
        );

      case "number":
        return (
          <span>
            {value.toLocaleString()}
            {suffix}
          </span>
        );

      case "text":
      default:
        return <span>{value}</span>;
    }
  };

  return (
    <tr className="border-b hover:bg-gray-50 transition">
      <td className="sticky left-0 z-10 bg-white px-4 py-3 text-sm text-gray-700 border-r font-medium">
        {label}
      </td>
      {values.map((value, index) => (
        <td
          key={index}
          className={cn(
            "px-4 py-3 text-center text-sm",
            index === bestIndex && type !== "text" && "bg-green-50 border-l-4 border-green-500"
          )}
        >
          <div className="flex items-center justify-center gap-2">
            {formatValue(value)}
            {index === bestIndex && type !== "text" && type !== "boolean" && (
              <TrendingUp size={16} className="text-green-600" />
            )}
          </div>
        </td>
      ))}
    </tr>
  );
}
```

---

### PASO 3: ComparisonRecommendations - Recomendaciones

```typescript
// filepath: src/components/comparison/ComparisonRecommendations.tsx
"use client";

import { Award, DollarSign, TrendingUp, Zap } from "lucide-react";
import { Badge } from "@/components/ui/Badge";
import type { Vehicle } from "@/types/vehicle";

interface ComparisonRecommendationsProps {
  vehicles: Vehicle[];
}

export function ComparisonRecommendations({ vehicles }: ComparisonRecommendationsProps) {
  if (vehicles.length < 2) return null;

  // Calcular recomendaciones
  const cheapest = vehicles.reduce((min, v) =>
    v.price < min.price ? v : min
  );

  const mostExpensive = vehicles.reduce((max, v) =>
    v.price > max.price ? v : max
  );

  const bestValue = vehicles.reduce((best, v) => {
    const valuScore = calculateValueScore(v);
    const bestScore = calculateValueScore(best);
    return valueScore > bestScore ? v : best;
  });

  const mostPowerful = vehicles.reduce((max, v) =>
    (v.horsepower || 0) > (max.horsepower || 0) ? v : max
  );

  const recommendations = [
    {
      icon: DollarSign,
      label: "Más Económico",
      vehicle: cheapest,
      color: "text-green-600 bg-green-100",
      description: `Ahorra $${(mostExpensive.price - cheapest.price).toLocaleString()}`,
    },
    {
      icon: Award,
      label: "Mejor Valor",
      vehicle: bestValue,
      color: "text-blue-600 bg-blue-100",
      description: "Mejor relación calidad-precio",
    },
    {
      icon: Zap,
      label: "Más Potente",
      vehicle: mostPowerful,
      color: "text-orange-600 bg-orange-100",
      description: `${mostPowerful.horsepower} HP`,
    },
  ];

  return (
    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
      {recommendations.map((rec) => {
        const Icon = rec.icon;
        return (
          <div
            key={rec.label}
            className="bg-white rounded-lg border p-4 flex items-center gap-3"
          >
            <div className={`p-3 rounded-lg ${rec.color}`}>
              <Icon size={24} />
            </div>
            <div>
              <p className="text-sm text-gray-600">{rec.label}</p>
              <p className="font-semibold text-gray-900">{rec.vehicle.make} {rec.vehicle.model}</p>
              <p className="text-xs text-gray-500">{rec.description}</p>
            </div>
          </div>
        );
      })}
    </div>
  );
}

function calculateValueScore(vehicle: Vehicle): number {
  let score = 0;

  // Precio (inverso: más barato = mejor)
  score += (1 - vehicle.price / 100000) * 30;

  // Features (cada feature suma)
  const features = [
    "hasAirConditioning",
    "hasLeatherSeats",
    "hasSunroof",
    "hasNavigationSystem",
    "hasBackupCamera",
    "hasAppleCarPlay",
    "hasAndroidAuto",
  ];
  const featureCount = features.filter((f) => vehicle[f]).length;
  score += (featureCount / features.length) * 40;

  // Performance
  if (vehicle.horsepower) {
    score += Math.min((vehicle.horsepower / 300) * 20, 20);
  }

  // Año (más nuevo = mejor)
  score += Math.min(((vehicle.year - 2010) / 15) * 10, 10);

  return score;
}
```

---

### PASO 4: AddVehicleModal - Agregar Vehículo a Comparación

```typescript
// filepath: src/components/comparison/AddVehicleModal.tsx
"use client";

import { useState } from "react";
import { Search } from "lucide-react";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/Dialog";
import { Input } from "@/components/ui/Input";
import { VehicleCard } from "@/components/vehicles/VehicleCard";
import { useVehicleSearch } from "@/lib/hooks/useVehicles";

interface AddVehicleModalProps {
  isOpen: boolean;
  onClose: () => void;
  onAddVehicle: (vehicleId: string) => void;
  excludeIds: string[];
}

export function AddVehicleModal({
  isOpen,
  onClose,
  onAddVehicle,
  excludeIds,
}: AddVehicleModalProps) {
  const [searchQuery, setSearchQuery] = useState("");
  const { data: results, isLoading } = useVehicleSearch(searchQuery);

  const filteredResults = results?.filter(
    (v) => !excludeIds.includes(v.id)
  );

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="max-w-4xl max-h-[80vh]">
        <DialogHeader>
          <DialogTitle>Agregar vehículo a comparación</DialogTitle>
        </DialogHeader>

        {/* Search */}
        <div className="relative">
          <Search size={20} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
          <Input
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="Buscar por marca, modelo..."
            className="pl-10"
          />
        </div>

        {/* Results */}
        <div className="overflow-y-auto max-h-[500px]">
          {isLoading && <p>Cargando...</p>}

          {filteredResults && filteredResults.length === 0 && (
            <p className="text-center text-gray-500 py-8">
              No se encontraron vehículos
            </p>
          )}

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {filteredResults?.map((vehicle) => (
              <div key={vehicle.id} className="relative">
                <VehicleCard vehicle={vehicle} />
                <Button
                  onClick={() => {
                    onAddVehicle(vehicle.id);
                    onClose();
                  }}
                  className="absolute top-2 right-2"
                  size="sm"
                >
                  Agregar
                </Button>
              </div>
            ))}
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
```

---

## 📄 PÁGINAS

### PASO 5: Página de Comparación

```typescript
// filepath: src/app/(main)/comparar/page.tsx
import { Metadata } from "next";
import { redirect } from "next/navigation";
import { auth } from "@/lib/auth";
import { ComparisonTable } from "@/components/comparison/ComparisonTable";
import { AddVehicleButton } from "@/components/comparison/AddVehicleButton";
import { comparisonService } from "@/lib/services/comparisonService";

export const metadata: Metadata = {
  title: "Comparar Vehículos | OKLA",
  description: "Compara hasta 3 vehículos lado a lado",
};

export default async function ComparePage() {
  const session = await auth();

  if (!session?.user) {
    redirect("/login?callbackUrl=/comparar");
  }

  const comparison = await comparisonService.getUserComparison(session.user.id);

  if (!comparison || comparison.vehicles.length === 0) {
    return (
      <div className="max-w-4xl mx-auto px-4 py-16 text-center">
        <h1 className="text-3xl font-bold text-gray-900 mb-4">
          Aún no tienes vehículos para comparar
        </h1>
        <p className="text-gray-600 mb-8">
          Agrega vehículos desde la página de búsqueda o detalles del vehículo
        </p>
        <AddVehicleButton />
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 py-8">
      <ComparisonTable
        vehicles={comparison.vehicles}
        onRemoveVehicle={(id) => comparisonService.removeVehicle(comparison.id, id)}
      />

      {comparison.vehicles.length < 3 && (
        <div className="mt-6 text-center">
          <AddVehicleButton />
        </div>
      )}
    </div>
  );
}
```

---

## 🪝 HOOKS Y SERVICIOS

### PASO 6: useComparison Hook (Extendido)

```typescript
// filepath: src/lib/hooks/useComparison.ts
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { comparisonService } from "@/lib/services/comparisonService";
import { toast } from "sonner";
import jsPDF from "jspdf";

export function useComparison() {
  const queryClient = useQueryClient();

  const generateShareLink = async (vehicles: Vehicle[]) => {
    const vehicleIds = vehicles.map((v) => v.id);
    const token = await comparisonService.generateShareLink(vehicleIds);
    const url = `${window.location.origin}/comparar/shared/${token}`;

    await navigator.clipboard.writeText(url);
    toast.success("Link copiado al portapapeles");

    return url;
  };

  const exportToPDF = async (vehicles: Vehicle[]) => {
    const pdf = new jsPDF("landscape");

    // Title
    pdf.setFontSize(20);
    pdf.text("Comparación de Vehículos - OKLA", 20, 20);

    // Add vehicle names
    let y = 40;
    vehicles.forEach((vehicle, index) => {
      pdf.setFontSize(14);
      pdf.text(
        `${index + 1}. ${vehicle.name} - $${vehicle.price.toLocaleString()}`,
        20,
        y,
      );
      y += 10;
    });

    // Add specs table
    y += 10;
    pdf.setFontSize(12);
    pdf.text("Especificaciones:", 20, y);
    y += 10;

    // ... (Add more details)

    pdf.save(`comparacion-okla-${Date.now()}.pdf`);
    toast.success("PDF descargado");
  };

  return {
    generateShareLink,
    exportToPDF,
  };
}
```

---

## 📦 TIPOS TYPESCRIPT

### PASO 7: Tipos de Comparison (Extendidos)

```typescript
// filepath: src/types/comparison.ts
import type { Vehicle } from "./vehicle";

export interface Comparison {
  id: string;
  userId: string;
  vehicles: Vehicle[];
  createdAt: string;
  updatedAt: string;
}

export interface ComparisonRecommendation {
  type: "cheapest" | "best_value" | "most_powerful" | "best_features";
  vehicleId: string;
  reason: string;
  score: number;
}

export interface SharedComparison {
  token: string;
  vehicles: Vehicle[];
  expiresAt: string;
}
```

---

## ✅ VALIDACIÓN

```bash
pnpm dev

# Verificar:
# - /comparar muestra tabla de comparación lado a lado
# - Hasta 3 vehículos máximo
# - Botón "Agregar vehículo" abre modal de búsqueda
# - Remover vehículo funciona
# - Highlighting de mejores valores funciona (verde)
# - Recomendaciones calculan correctamente
# - Compartir genera link público
# - Exportar PDF funciona
# - Tabla scrollable horizontal en mobile
# - Sticky column para labels
# - Categorías de specs agrupadas
# - Checkmarks y X para booleans
# - Formato correcto para currency/number
```

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/comparador.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Comparador de Vehículos", () => {
  test("debe mostrar tabla de comparación vacía inicialmente", async ({
    page,
  }) => {
    await page.goto("/comparar");

    await expect(
      page.getByRole("heading", { name: /comparar/i }),
    ).toBeVisible();
    await expect(page.getByText(/agregar vehículo/i)).toBeVisible();
  });

  test("debe agregar vehículo desde modal", async ({ page }) => {
    await page.goto("/comparar");

    await page.getByRole("button", { name: /agregar vehículo/i }).click();
    await expect(page.getByRole("dialog")).toBeVisible();

    await page.fill('input[placeholder*="buscar"]', "Toyota");
    await page.getByTestId("vehicle-result").first().click();

    await expect(page.getByTestId("comparison-vehicle")).toHaveCount(1);
  });

  test("debe limitar a 3 vehículos máximo", async ({ page }) => {
    await page.goto("/comparar?v=id1,id2,id3");

    await expect(page.getByTestId("comparison-vehicle")).toHaveCount(3);
    await expect(page.getByRole("button", { name: /agregar/i })).toBeDisabled();
  });

  test("debe remover vehículo de comparación", async ({ page }) => {
    await page.goto("/comparar?v=id1,id2");

    await page.getByTestId("remove-vehicle-0").click();
    await expect(page.getByTestId("comparison-vehicle")).toHaveCount(1);
  });

  test("debe resaltar mejores valores en verde", async ({ page }) => {
    await page.goto("/comparar?v=id1,id2");

    await expect(page.locator('[data-best="true"]')).toHaveCount({ min: 1 });
  });

  test("debe generar link para compartir", async ({ page }) => {
    await page.goto("/comparar?v=id1,id2");

    await page.getByRole("button", { name: /compartir/i }).click();
    await expect(page.getByText(/link copiado/i)).toBeVisible();
  });

  test("debe exportar PDF de comparación", async ({ page }) => {
    await page.goto("/comparar?v=id1,id2");

    const downloadPromise = page.waitForEvent("download");
    await page.getByRole("button", { name: /exportar pdf/i }).click();
    const download = await downloadPromise;

    expect(download.suggestedFilename()).toMatch(/comparacion.*\.pdf/i);
  });
});
```

---

## 🚀 MEJORAS FUTURAS

1. **Side-by-Side Images**: Slider para comparar fotos
2. **Video Comparisons**: Videos 360° lado a lado
3. **Print Mode**: CSS optimizado para impresión
4. **Dealer Contact**: Contactar múltiples dealers a la vez
5. **Save Comparisons**: Guardar múltiples comparaciones
6. **Compare History**: Historial de comparaciones pasadas

---

**Siguiente documento:** `24-alertas-busquedas.md` - Saved searches y alertas automáticas
