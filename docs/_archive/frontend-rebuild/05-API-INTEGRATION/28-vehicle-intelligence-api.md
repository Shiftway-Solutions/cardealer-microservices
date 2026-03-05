# 🧠 28 - Vehicle Intelligence API

**Servicio:** VehicleIntelligenceService  
**Puerto:** 8080  
**Base Path:** `/api/pricing`, `/api/demand`, `/api/vehicleintelligence`  
**Autenticación:** ✅ Requerida

---

## 📖 Descripción

Servicio de inteligencia artificial para:

- Análisis y sugerencia de precios
- Predicción de demanda
- Análisis de mercado
- Dashboard de métricas ML

---

## 🎯 Endpoints Disponibles

### PricingController

| #   | Método | Endpoint                                  | Auth | Descripción     |
| --- | ------ | ----------------------------------------- | ---- | --------------- |
| 1   | `POST` | `/api/pricing/analyze`                    | ✅   | Analizar precio |
| 2   | `GET`  | `/api/pricing/vehicle/{vehicleId}/latest` | ✅   | Último análisis |
| 3   | `GET`  | `/api/pricing/{id}`                       | ✅   | Análisis por ID |

### DemandController

| #   | Método | Endpoint                            | Auth | Descripción          |
| --- | ------ | ----------------------------------- | ---- | -------------------- |
| 4   | `POST` | `/api/demand/predict`               | ✅   | Predecir demanda     |
| 5   | `GET`  | `/api/demand/{make}/{model}/{year}` | ✅   | Demanda por vehículo |

### VehicleIntelligenceController

| #   | Método | Endpoint                                                         | Auth     | Descripción           |
| --- | ------ | ---------------------------------------------------------------- | -------- | --------------------- |
| 6   | `POST` | `/api/vehicleintelligence/price-suggestion`                      | ✅       | Sugerencia de precio  |
| 7   | `GET`  | `/api/vehicleintelligence/demand/categories`                     | ✅       | Demanda por categoría |
| 8   | `GET`  | `/api/vehicleintelligence/market-analysis/{make}/{model}/{year}` | ✅       | Análisis de mercado   |
| 9   | `GET`  | `/api/vehicleintelligence/market-analysis/dashboard`             | ✅       | Dashboard mercado     |
| 10  | `GET`  | `/api/vehicleintelligence/ml/statistics`                         | ✅ Admin | Stats ML              |
| 11  | `GET`  | `/api/vehicleintelligence/ml/performance`                        | ✅ Admin | Performance ML        |
| 12  | `GET`  | `/api/vehicleintelligence/ml/metrics`                            | ✅ Admin | Métricas ML           |

---

## 📝 Detalle de Endpoints

### 6. POST `/api/vehicleintelligence/price-suggestion` - Sugerencia de Precio

**Request:**

```json
{
  "make": "Toyota",
  "model": "Camry",
  "year": 2024,
  "mileage": 15000,
  "condition": "Excellent",
  "fuelType": "Gasoline",
  "transmission": "Automatic",
  "bodyType": "Sedan",
  "features": ["Leather", "Sunroof", "Navigation"],
  "currentListingPrice": 1900000
}
```

**Response 200:**

```json
{
  "suggestedPrice": 1850000,
  "priceRange": {
    "min": 1750000,
    "max": 1950000
  },
  "marketPosition": "AboveAverage",
  "competitivenessScore": 78,
  "estimatedDaysToSell": 21,
  "demandLevel": "High",
  "priceVsMarket": {
    "percentile": 65,
    "averageMarketPrice": 1780000,
    "difference": 70000,
    "differencePercent": 3.9
  },
  "recommendations": [
    {
      "type": "PriceAdjustment",
      "message": "Reducir precio en RD$50,000 para venta más rápida",
      "impact": "Reduce tiempo de venta en ~7 días"
    },
    {
      "type": "Feature",
      "message": "El sunroof agrega ~RD$80,000 al valor",
      "impact": "Destacar en descripción"
    }
  ],
  "similarListings": [
    {
      "id": "v1",
      "title": "Toyota Camry 2024",
      "price": 1780000,
      "daysListed": 14
    },
    {
      "id": "v2",
      "title": "Toyota Camry 2024",
      "price": 1820000,
      "daysListed": 28
    }
  ],
  "confidence": 0.89,
  "modelVersion": "v2.3.1"
}
```

---

### 7. GET `/api/vehicleintelligence/demand/categories` - Demanda por Categoría

**Response 200:**

```json
[
  {
    "category": "SUV",
    "demandLevel": "VeryHigh",
    "demandScore": 92,
    "averageDaysToSell": 18,
    "priceGrowthPercent": 5.2,
    "topMakes": ["Toyota", "Hyundai", "Kia"],
    "trend": "Rising"
  },
  {
    "category": "Sedan",
    "demandLevel": "High",
    "demandScore": 78,
    "averageDaysToSell": 24,
    "priceGrowthPercent": 2.1,
    "topMakes": ["Toyota", "Honda", "Nissan"],
    "trend": "Stable"
  },
  {
    "category": "Pickup",
    "demandLevel": "Medium",
    "demandScore": 65,
    "averageDaysToSell": 32,
    "priceGrowthPercent": -1.5,
    "topMakes": ["Toyota", "Ford", "Chevrolet"],
    "trend": "Declining"
  }
]
```

---

### 8. GET `/api/vehicleintelligence/market-analysis/{make}/{model}/{year}`

**Response 200:**

```json
{
  "make": "Toyota",
  "model": "Camry",
  "year": 2024,
  "marketOverview": {
    "totalListings": 45,
    "averagePrice": 1780000,
    "medianPrice": 1750000,
    "priceRange": { "min": 1600000, "max": 2100000 },
    "averageMileage": 22000,
    "averageDaysOnMarket": 21
  },
  "priceHistory": [
    { "month": "2025-11", "averagePrice": 1820000 },
    { "month": "2025-12", "averagePrice": 1800000 },
    { "month": "2026-01", "averagePrice": 1780000 }
  ],
  "demandIndicators": {
    "searchVolume": 1250,
    "viewsPerListing": 89,
    "contactRate": 12.5,
    "favoriteRate": 8.2
  },
  "seasonality": {
    "bestMonthToSell": "March",
    "worstMonthToSell": "September",
    "currentSeasonScore": 72
  }
}
```

---

## 🔧 TypeScript Types

```typescript
// ============================================================================
// PRICE ANALYSIS
// ============================================================================

export interface PriceSuggestionRequest {
  make: string;
  model: string;
  year: number;
  mileage: number;
  condition: VehicleCondition;
  fuelType: string;
  transmission: string;
  bodyType: string;
  features?: string[];
  currentListingPrice?: number;
}

export type VehicleCondition = "Excellent" | "Good" | "Fair" | "Poor";
export type MarketPosition =
  | "BelowAverage"
  | "Average"
  | "AboveAverage"
  | "Premium";
export type DemandLevel = "VeryLow" | "Low" | "Medium" | "High" | "VeryHigh";
export type PriceTrend = "Rising" | "Stable" | "Declining";

export interface PriceSuggestion {
  suggestedPrice: number;
  priceRange: { min: number; max: number };
  marketPosition: MarketPosition;
  competitivenessScore: number;
  estimatedDaysToSell: number;
  demandLevel: DemandLevel;
  priceVsMarket: {
    percentile: number;
    averageMarketPrice: number;
    difference: number;
    differencePercent: number;
  };
  recommendations: PriceRecommendation[];
  similarListings: SimilarListing[];
  confidence: number;
  modelVersion: string;
}

export interface PriceRecommendation {
  type: "PriceAdjustment" | "Feature" | "Timing" | "Description";
  message: string;
  impact: string;
}

export interface SimilarListing {
  id: string;
  title: string;
  price: number;
  daysListed: number;
}

// ============================================================================
// DEMAND
// ============================================================================

export interface CategoryDemand {
  category: string;
  demandLevel: DemandLevel;
  demandScore: number;
  averageDaysToSell: number;
  priceGrowthPercent: number;
  topMakes: string[];
  trend: PriceTrend;
}

export interface DemandPrediction {
  make: string;
  model: string;
  year: number;
  demandScore: number;
  demandLevel: DemandLevel;
  estimatedDaysToSell: number;
  optimalPriceRange: { min: number; max: number };
  seasonalFactors: {
    currentFactor: number;
    bestMonth: string;
    worstMonth: string;
  };
}

// ============================================================================
// MARKET ANALYSIS
// ============================================================================

export interface MarketAnalysis {
  make: string;
  model: string;
  year: number;
  marketOverview: {
    totalListings: number;
    averagePrice: number;
    medianPrice: number;
    priceRange: { min: number; max: number };
    averageMileage: number;
    averageDaysOnMarket: number;
  };
  priceHistory: { month: string; averagePrice: number }[];
  demandIndicators: {
    searchVolume: number;
    viewsPerListing: number;
    contactRate: number;
    favoriteRate: number;
  };
  seasonality: {
    bestMonthToSell: string;
    worstMonthToSell: string;
    currentSeasonScore: number;
  };
}

// ============================================================================
// ML STATS (Admin)
// ============================================================================

export interface MLStatistics {
  totalPredictions: number;
  predictionsToday: number;
  averageConfidence: number;
  modelAccuracy: number;
  lastModelUpdate: string;
}

export interface ModelPerformance {
  modelVersion: string;
  accuracy: number;
  mae: number; // Mean Absolute Error
  mape: number; // Mean Absolute Percentage Error
  r2Score: number;
  trainedAt: string;
  samplesUsed: number;
}
```

---

## 📡 Service Layer

```typescript
// src/services/vehicleIntelligenceService.ts
import { apiClient } from "./api-client";
import type {
  PriceSuggestionRequest,
  PriceSuggestion,
  CategoryDemand,
  DemandPrediction,
  MarketAnalysis,
  MLStatistics,
  ModelPerformance,
} from "@/types/vehicleIntelligence";

class VehicleIntelligenceService {
  // ============================================================================
  // PRICE SUGGESTION
  // ============================================================================

  async getPriceSuggestion(
    request: PriceSuggestionRequest,
  ): Promise<PriceSuggestion> {
    const response = await apiClient.post<PriceSuggestion>(
      "/api/vehicleintelligence/price-suggestion",
      request,
    );
    return response.data;
  }

  // ============================================================================
  // DEMAND
  // ============================================================================

  async getDemandByCategory(): Promise<CategoryDemand[]> {
    const response = await apiClient.get<CategoryDemand[]>(
      "/api/vehicleintelligence/demand/categories",
    );
    return response.data;
  }

  async predictDemand(
    make: string,
    model: string,
    year: number,
  ): Promise<DemandPrediction> {
    const response = await apiClient.get<DemandPrediction>(
      `/api/demand/${make}/${model}/${year}`,
    );
    return response.data;
  }

  // ============================================================================
  // MARKET ANALYSIS
  // ============================================================================

  async getMarketAnalysis(
    make: string,
    model: string,
    year: number,
  ): Promise<MarketAnalysis> {
    const response = await apiClient.get<MarketAnalysis>(
      `/api/vehicleintelligence/market-analysis/${make}/${model}/${year}`,
    );
    return response.data;
  }

  async getMarketDashboard(filters?: {
    make?: string;
    model?: string;
    minYear?: number;
    maxYear?: number;
  }): Promise<MarketAnalysis[]> {
    const response = await apiClient.get<MarketAnalysis[]>(
      "/api/vehicleintelligence/market-analysis/dashboard",
      { params: filters },
    );
    return response.data;
  }

  // ============================================================================
  // ML ADMIN
  // ============================================================================

  async getMLStatistics(): Promise<MLStatistics> {
    const response = await apiClient.get<MLStatistics>(
      "/api/vehicleintelligence/ml/statistics",
    );
    return response.data;
  }

  async getModelPerformance(): Promise<ModelPerformance> {
    const response = await apiClient.get<ModelPerformance>(
      "/api/vehicleintelligence/ml/performance",
    );
    return response.data;
  }
}

export const vehicleIntelligenceService = new VehicleIntelligenceService();
```

---

## 🎉 Resumen

✅ **12 Endpoints documentados**  
✅ **TypeScript Types** (Pricing, Demand, Market, ML)  
✅ **Service Layer** (8 métodos)  
✅ **ML Dashboard** para admins

---

_Última actualización: Enero 30, 2026_
