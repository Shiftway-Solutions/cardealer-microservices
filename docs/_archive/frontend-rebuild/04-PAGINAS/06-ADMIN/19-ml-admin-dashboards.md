---
title: "80 - ML Admin Dashboards"
priority: P2
estimated_time: "2 horas"
dependencies: []
apis: []
status: partial
last_updated: "2026-01-30"
---

# 80 - ML Admin Dashboards

> **Sprint**: 7 - Advanced Features  
> **Épica**: Machine Learning & Data Science  
> **Prioridad**: Alta  
> **Estimación**: 13 SP

---

## 📋 Descripción

Dashboards de Machine Learning para administradores y data scientists:

- **MLAdminDashboard**: Gestión de modelos ML (recomendaciones, scoring, pricing, fraude)
- **MLDashboardPage**: Métricas de inferencia con Recharts
- **UserBehaviorDashboard**: Perfiles de comportamiento de usuarios
- **FeatureStoreDashboard**: Gestión de features para ML

---

## 🏗️ Arquitectura

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      ML ADMIN DASHBOARDS                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │                  MLAdminDashboard (514 líneas)                        │  │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐   │  │
│  │  │   Metrics   │  │   Models    │  │  Training   │  │   Actions   │   │  │
│  │  │   Summary   │  │    Grid     │  │    Jobs     │  │  (Retrain)  │   │  │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘   │  │
│  │   4 Model Types: recommendation | lead_scoring | pricing | fraud      │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │                  MLDashboardPage (437 líneas)                         │  │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                    │  │
│  │  │ Performance │  │  Latency    │  │   Error     │                    │  │
│  │  │   Charts    │  │   Metrics   │  │   Rates     │                    │  │
│  │  └─────────────┘  └─────────────┘  └─────────────┘                    │  │
│  │               Recharts (LineChart, BarChart)                          │  │
│  │               TanStack Query for data fetching                        │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │               UserBehaviorDashboard (352 líneas)                      │  │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐   │  │
│  │  │  Segment    │  │  Purchase   │  │Preferences  │  │   Actions   │   │  │
│  │  │   Badge     │  │   Intent    │  │  (makes/    │  │   History   │   │  │
│  │  │             │  │   Score     │  │   prices)   │  │             │   │  │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘   │  │
│  │           userBehaviorService.ts                                       │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │               FeatureStoreDashboard (240 líneas)                      │  │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                    │  │
│  │  │  User       │  │  Vehicle    │  │  Feature    │                    │  │
│  │  │  Features   │  │  Features   │  │ Definitions │                    │  │
│  │  └─────────────┘  └─────────────┘  └─────────────┘                    │  │
│  │           featureStoreService.ts                                       │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │                       Backend Services                                │  │
│  │  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐    │  │
│  │  │vehicleIntelligence│  │ userBehavior    │  │  featureStore    │    │  │
│  │  │    Service        │  │   Service       │  │   Service        │    │  │
│  │  └──────────────────┘  └──────────────────┘  └──────────────────┘    │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 📁 Archivos

| Archivo                   | Ruta           | Líneas    | Descripción                |
| ------------------------- | -------------- | --------- | -------------------------- |
| MLAdminDashboard.tsx      | `pages/admin/` | 514       | Gestión de modelos ML      |
| MLDashboardPage.tsx       | `pages/admin/` | 437       | Métricas de inferencia     |
| UserBehaviorDashboard.tsx | `pages/`       | 352       | Perfiles de comportamiento |
| FeatureStoreDashboard.tsx | `pages/`       | 240       | Feature store management   |
| **Total**                 |                | **1,543** |                            |

---

## 🎨 Rutas

```typescript
// App.tsx
<Route path="/admin/ml" element={<MLAdminDashboard />} />
<Route path="/admin/ml/dashboard" element={<MLDashboardPage />} />
<Route path="/behavior/:userId?" element={<UserBehaviorDashboard />} />
<Route path="/features/:entityType?/:entityId?" element={<FeatureStoreDashboard />} />
```

---

## 📊 TypeScript Types

### ML Model Types

```typescript
interface MLModel {
  id: string;
  name: string;
  version: string;
  type: "recommendation" | "lead_scoring" | "pricing" | "fraud_detection";
  status: "active" | "training" | "inactive" | "error";
  accuracy: number; // 0-100
  lastTrained: string; // ISO date
  nextTraining: string; // ISO date
  inferenceCount: number;
  avgLatencyMs: number;
  featureCount: number;
  datasetSize: string; // "2.3M records"
}

interface ModelMetrics {
  totalInferences: number;
  avgLatency: number;
  successRate: number;
  errorRate: number;
  activeModels: number;
  trainingJobs: number;
}
```

### Inference Metrics

```typescript
interface ModelMetrics {
  modelName: string;
  accuracy: number;
  mae: number; // Mean Absolute Error
  rmse: number; // Root Mean Square Error
  lastTrained: string;
  nextTraining: string;
  status: "healthy" | "warning" | "error";
}

interface InferenceMetrics {
  totalInferences: number;
  successRate: number;
  avgLatencyMs: number;
  p95LatencyMs: number;
  p99LatencyMs: number;
  errorsLast24h: number;
}

interface PerformanceData {
  timestamp: string;
  latency: number;
  throughput: number;
  errorRate: number;
}
```

### User Behavior Types

```typescript
interface UserBehaviorProfile {
  userId: string;
  userSegment: "hot_lead" | "warm_lead" | "cold_lead" | "browser" | "new_user";
  engagementScore: number; // 0-100
  purchaseIntentScore: number; // 0-100
  totalSearches: number;
  totalVehicleViews: number;
  totalContactRequests: number;
  lastActivityAt: string;
  preferredMakes: string[];
  preferredPriceRange: { min: number; max: number };
  preferredBodyTypes: string[];
}

interface UserAction {
  id: string;
  userId: string;
  actionType: string;
  entityId?: string;
  metadata: Record<string, any>;
  createdAt: string;
}

interface UserBehaviorSummary {
  totalUsers: number;
  activeUsers24h: number;
  hotLeads: number;
  warmLeads: number;
  coldLeads: number;
  avgEngagementScore: number;
}
```

### Feature Store Types

```typescript
interface UserFeature {
  id: string;
  userId: string;
  featureName: string;
  featureValue: string;
  featureType: "numeric" | "categorical" | "boolean" | "array";
  version: number;
  computedAt: string;
  expiresAt?: string;
}

interface VehicleFeature {
  id: string;
  vehicleId: string;
  featureName: string;
  featureValue: string;
  featureType: "numeric" | "categorical" | "boolean" | "array";
  version: number;
  computedAt: string;
  expiresAt?: string;
}

interface FeatureDefinition {
  id: string;
  name: string;
  description: string;
  category: string;
  dataType: string;
  computationLogic: string;
  updateFrequency: string;
  isActive: boolean;
}
```

---

## 🧩 Componentes

### MLAdminDashboard - Model Cards

```typescript
// 5 ML models in the system
const mockModels: MLModel[] = [
  {
    id: "1",
    name: "Vehicle Recommendations",
    version: "v2.4.1",
    type: "recommendation",
    status: "active",
    accuracy: 87.5,
    lastTrained: "2026-01-24T08:00:00",
    nextTraining: "2026-01-31T08:00:00",
    inferenceCount: 145230,
    avgLatencyMs: 23,
    featureCount: 48,
    datasetSize: "2.3M records",
  },
  {
    id: "2",
    name: "Similar Vehicles",
    version: "v1.8.0",
    type: "recommendation",
    status: "active",
    accuracy: 92.1,
    // ...
  },
  {
    id: "3",
    name: "Lead Scoring",
    version: "v3.1.2",
    type: "lead_scoring",
    status: "active",
    accuracy: 78.3,
    // ...
  },
  {
    id: "4",
    name: "Price Intelligence",
    version: "v2.0.5",
    type: "pricing",
    status: "training",
    accuracy: 85.7,
    // ...
  },
  {
    id: "5",
    name: "Fraud Detection",
    version: "v1.2.0",
    type: "fraud_detection",
    status: "active",
    accuracy: 94.8,
    // ...
  },
];
```

### MLDashboardPage - Status Helpers

```typescript
const getStatusColor = (status: 'healthy' | 'warning' | 'error'): string => {
  switch (status) {
    case 'healthy':
      return 'text-green-600 bg-green-50 border-green-200';
    case 'warning':
      return 'text-yellow-600 bg-yellow-50 border-yellow-200';
    case 'error':
      return 'text-red-600 bg-red-50 border-red-200';
  }
};

const getStatusIcon = (status: 'healthy' | 'warning' | 'error') => {
  if (status === 'healthy') return <FiCheckCircle className="w-5 h-5 text-green-600" />;
  if (status === 'warning') return <FiAlertCircle className="w-5 h-5 text-yellow-600" />;
  return <FiAlertCircle className="w-5 h-5 text-red-600" />;
};
```

### UserBehaviorDashboard - Segment Colors

```typescript
// userBehaviorService.ts
const getSegmentColor = (segment: string) => {
  const colors: Record<string, string> = {
    hot_lead: "red",
    warm_lead: "yellow",
    cold_lead: "blue",
    browser: "gray",
    new_user: "green",
  };
  return colors[segment] || "gray";
};

const getSegmentLabel = (segment: string) => {
  const labels: Record<string, string> = {
    hot_lead: "Lead Caliente 🔥",
    warm_lead: "Lead Tibio",
    cold_lead: "Lead Frío",
    browser: "Navegador",
    new_user: "Nuevo Usuario",
  };
  return labels[segment] || segment;
};
```

### FeatureStoreDashboard - Feature Type Colors

```typescript
// featureStoreService.ts
const getFeatureTypeColor = (type: string) => {
  const colors: Record<string, string> = {
    numeric: "blue",
    categorical: "green",
    boolean: "purple",
    array: "orange",
  };
  return colors[type] || "gray";
};
```

---

## 📈 Charts (Recharts)

### Performance Chart (MLDashboardPage)

```tsx
import {
  LineChart,
  Line,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from "recharts";

// Performance data
const performanceData: PerformanceData[] = [
  { timestamp: "00:00", latency: 45, throughput: 150, errorRate: 0.5 },
  { timestamp: "04:00", latency: 52, throughput: 180, errorRate: 0.3 },
  { timestamp: "08:00", latency: 38, throughput: 220, errorRate: 0.2 },
  { timestamp: "12:00", latency: 61, throughput: 280, errorRate: 1.2 },
  { timestamp: "16:00", latency: 48, throughput: 240, errorRate: 0.4 },
  { timestamp: "20:00", latency: 42, throughput: 200, errorRate: 0.1 },
  { timestamp: "23:59", latency: 39, throughput: 160, errorRate: 0.0 },
];

<ResponsiveContainer width="100%" height={300}>
  <LineChart data={performanceData}>
    <CartesianGrid strokeDasharray="3 3" />
    <XAxis dataKey="timestamp" />
    <YAxis />
    <Tooltip />
    <Legend />
    <Line
      type="monotone"
      dataKey="latency"
      stroke="#8884d8"
      name="Latencia (ms)"
    />
    <Line
      type="monotone"
      dataKey="throughput"
      stroke="#82ca9d"
      name="Throughput"
    />
  </LineChart>
</ResponsiveContainer>;
```

---

## 🔌 Services

### vehicleIntelligenceService

```typescript
// vehicleIntelligenceService.ts
const vehicleIntelligenceService = {
  getMLStatistics: () => Promise<MLStatistics>,
  getModelPerformance: () => Promise<ModelPerformance>,
  getInferenceMetrics: () => Promise<InferenceMetrics>,
};
```

### userBehaviorService

```typescript
// userBehaviorService.ts
const userBehaviorService = {
  getUserProfile: (userId: string) => Promise<UserBehaviorProfile>,
  getUserActions: (userId: string, limit: number) => Promise<UserAction[]>,
  getSummary: () => Promise<UserBehaviorSummary>,
  getSegmentColor: (segment: string) => string,
  getSegmentLabel: (segment: string) => string,
};
```

### featureStoreService

```typescript
// featureStoreService.ts
const featureStoreService = {
  getUserFeatures: (userId: string) => Promise<UserFeature[]>,
  getVehicleFeatures: (vehicleId: string) => Promise<VehicleFeature[]>,
  getFeatureDefinitions: () => Promise<FeatureDefinition[]>,
  getFeatureTypeColor: (type: string) => string,
};
```

---

## 🔄 TanStack Query Integration

### MLDashboardPage

```typescript
// Fetch ML Statistics
const {
  data: mlStats,
  isLoading: statsLoading,
  refetch: refetchStats,
} = useQuery({
  queryKey: ["ml-statistics"],
  queryFn: () => vehicleIntelligenceService.getMLStatistics(),
  staleTime: 60 * 1000, // 1 minute
});

// Fetch Model Performance
const {
  data: modelPerformance,
  isLoading: performanceLoading,
  refetch: refetchPerformance,
} = useQuery({
  queryKey: ["model-performance"],
  queryFn: () => vehicleIntelligenceService.getModelPerformance(),
  staleTime: 60 * 1000,
});

// Fetch Inference Metrics
const {
  data: inferenceMetrics,
  isLoading: metricsLoading,
  refetch: refetchMetrics,
} = useQuery({
  queryKey: ["inference-metrics"],
  queryFn: () => vehicleIntelligenceService.getInferenceMetrics(),
  staleTime: 30 * 1000,
});

const handleRefreshAll = async () => {
  await Promise.all([refetchStats(), refetchPerformance(), refetchMetrics()]);
};
```

---

## 📤 Export Metrics

```typescript
const handleExportMetrics = () => {
  const data = {
    timestamp: new Date().toISOString(),
    mlStats,
    modelPerformance,
    inferenceMetrics,
  };
  const csv = JSON.stringify(data, null, 2);
  const element = document.createElement("a");
  element.setAttribute(
    "href",
    "data:text/json;charset=utf-8," + encodeURIComponent(csv),
  );
  element.setAttribute("download", `ml-metrics-${Date.now()}.json`);
  element.style.display = "none";
  document.body.appendChild(element);
  element.click();
  document.body.removeChild(element);
};
```

---

## 📱 Layouts

| Page                  | Layout            |
| --------------------- | ----------------- |
| MLAdminDashboard      | AdminLayout       |
| MLDashboardPage       | None (standalone) |
| UserBehaviorDashboard | MainLayout        |
| FeatureStoreDashboard | MainLayout        |

---

## 📦 Dependencies

```json
{
  "@tanstack/react-query": "^5.0.0",
  "recharts": "^2.10.0",
  "react-icons": "^5.0.0"
}
```

---

## ✅ Checklist de Validación

### MLAdminDashboard

- [ ] 5 modelos ML listados con cards
- [ ] Status badges (active/training/inactive/error)
- [ ] Accuracy % visible
- [ ] Last/Next training dates
- [ ] Inference count y latency
- [ ] Retrain button funcional
- [ ] AdminLayout aplicado

### MLDashboardPage

- [ ] TanStack Query fetching data
- [ ] Recharts LineChart/BarChart rendering
- [ ] Status icons (healthy/warning/error)
- [ ] Refresh all button
- [ ] Export metrics JSON
- [ ] Latency p95/p99 metrics

### UserBehaviorDashboard

- [ ] User profile con segment badge
- [ ] Engagement score display
- [ ] Purchase intent progress bar
- [ ] Preferred makes/prices
- [ ] Actions history table
- [ ] Summary view sin userId
- [ ] MainLayout aplicado

### FeatureStoreDashboard

- [ ] User features table
- [ ] Vehicle features table
- [ ] Feature definitions grid
- [ ] Category filter funcional
- [ ] Feature type badges con colores
- [ ] Expiry dates formateadas
- [ ] MainLayout aplicado

### General

- [ ] Loading spinners
- [ ] Error states con mensajes
- [ ] Responsive design
- [ ] Date formatting es-DO

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/ml-admin-dashboards.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsAdmin } from "../helpers/auth";

test.describe("ML Admin Dashboards", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("debe mostrar dashboard de ML", async ({ page }) => {
    await page.goto("/admin/ml");

    await expect(page.getByTestId("ml-dashboard")).toBeVisible();
  });

  test("debe ver métricas de modelos", async ({ page }) => {
    await page.goto("/admin/ml/models");

    await expect(page.getByTestId("model-metrics")).toBeVisible();
  });

  test("debe ver predicciones de pricing", async ({ page }) => {
    await page.goto("/admin/ml/pricing");

    await expect(page.getByTestId("pricing-predictions")).toBeVisible();
  });

  test("debe ver feature store", async ({ page }) => {
    await page.goto("/admin/ml/features");

    await expect(page.getByTestId("feature-store")).toBeVisible();
  });

  test("debe ver comportamiento de usuarios", async ({ page }) => {
    await page.goto("/admin/ml/user-behavior");

    await expect(page.getByTestId("behavior-analytics")).toBeVisible();
  });

  test("debe ver lead scoring", async ({ page }) => {
    await page.goto("/admin/ml/lead-scoring");

    await expect(page.getByTestId("lead-scores")).toBeVisible();
  });
});
```

---

## 📚 Referencias

- [Recharts Documentation](https://recharts.org/)
- [TanStack Query](https://tanstack.com/query)
- [vehicleIntelligenceService.ts](frontend/web/src/services/vehicleIntelligenceService.ts)
- [userBehaviorService.ts](frontend/web/src/services/userBehaviorService.ts)
- [featureStoreService.ts](frontend/web/src/services/featureStoreService.ts)
