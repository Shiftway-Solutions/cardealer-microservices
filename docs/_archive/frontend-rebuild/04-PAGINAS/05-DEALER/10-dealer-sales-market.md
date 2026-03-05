---
title: "81 - Dealer Sales & Market Intelligence Pages"
priority: P1
estimated_time: "2 horas"
dependencies: []
apis: []
status: partial
last_updated: "2026-01-30"
---

# 81 - Dealer Sales & Market Intelligence Pages

## Descripción General

Este documento cubre las páginas de ventas del dealer, análisis de mercado e inteligencia de precios con IA. Incluye el historial de ventas con estadísticas, análisis de demanda por categoría de vehículos, y herramientas de pricing inteligente.

---

## Arquitectura de Páginas

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         DEALER SALES & MARKET                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    DealerSalesPage                                   │   │
│  │  /dealer/sales                                                       │   │
│  │                                                                      │   │
│  │  ┌────────────────────────────────────────────────────────────────┐ │   │
│  │  │  Stats Cards (4)                                               │ │   │
│  │  │  [Total Vendidos] [Ingresos] [Precio Promedio] [Tiempo Prom]   │ │   │
│  │  └────────────────────────────────────────────────────────────────┘ │   │
│  │                                                                      │   │
│  │  ┌─────────────────┐ ┌─────────────────┐                            │   │
│  │  │  Este Mes       │ │  Mes Anterior   │                            │   │
│  │  │  ventas + $     │ │  ventas + $     │                            │   │
│  │  └─────────────────┘ └─────────────────┘                            │   │
│  │                                                                      │   │
│  │  ┌────────────────────────────────────────────────────────────────┐ │   │
│  │  │  Filters: [Search] [Sort: date|price]                          │ │   │
│  │  └────────────────────────────────────────────────────────────────┘ │   │
│  │                                                                      │   │
│  │  ┌────────────────────────────────────────────────────────────────┐ │   │
│  │  │  SaleCard (list)                                               │ │   │
│  │  │  [Image] [Title] [Price] [Date] [Days] [Views] [Inquiries]     │ │   │
│  │  └────────────────────────────────────────────────────────────────┘ │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                  MarketAnalysisPage                                  │   │
│  │  /dealer/market                                                      │   │
│  │                                                                      │   │
│  │  ┌────────────────────────────────────────────────────────────────┐ │   │
│  │  │  Filters: [Search Category] [Time Range: week|month|quarter]   │ │   │
│  │  └────────────────────────────────────────────────────────────────┘ │   │
│  │                                                                      │   │
│  │  ┌────────────────────────────────────────────────────────────────┐ │   │
│  │  │  Category Cards Grid (3 cols)                                  │ │   │
│  │  │  [Demand Badge] [Score Bar] [Days Avg] [Searches]              │ │   │
│  │  └────────────────────────────────────────────────────────────────┘ │   │
│  │                                                                      │   │
│  │  ┌────────────────────────────────────────────────────────────────┐ │   │
│  │  │  Summary Stats (4 cards)                                       │ │   │
│  │  │  [Categorías] [Demanda Prom] [Búsquedas] [Listados Activos]    │ │   │
│  │  └────────────────────────────────────────────────────────────────┘ │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                PricingIntelligencePage                               │   │
│  │  /pricing-intelligence                                               │   │
│  │                                                                      │   │
│  │  ┌────────────────┐ ┌──────────────────────────────────────────┐    │   │
│  │  │  Form Column   │ │  Results Column                          │    │   │
│  │  │                │ │                                          │    │   │
│  │  │  [Make]        │ │  Tab: Pricing                            │    │   │
│  │  │  [Model]       │ │  ┌──────────────────────────────────────┐│    │   │
│  │  │  [Year]        │ │  │  PricingWidget                       ││    │   │
│  │  │  [Mileage]     │ │  │  - Suggested price                   ││    │   │
│  │  │  [Condition]   │ │  │  - Price range                       ││    │   │
│  │  │  [Price]       │ │  │  - Recommendations                   ││    │   │
│  │  │  [Photos]      │ │  └──────────────────────────────────────┘│    │   │
│  │  │                │ │                                          │    │   │
│  │  │                │ │  Tab: Demand                             │    │   │
│  │  │                │ │  ┌──────────────────────────────────────┐│    │   │
│  │  │                │ │  │  DemandPredictor                     ││    │   │
│  │  │                │ │  │  - Demand level                      ││    │   │
│  │  │                │ │  │  - Days to sell                      ││    │   │
│  │  │                │ │  └──────────────────────────────────────┘│    │   │
│  │  └────────────────┘ └──────────────────────────────────────────┘    │   │
│  │                                                                      │   │
│  │  ┌────────────────────────────────────────────────────────────────┐ │   │
│  │  │  Info Cards (3): Precisión | Predicción | Recomendaciones      │ │   │
│  │  └────────────────────────────────────────────────────────────────┘ │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Tipos TypeScript

### Sales Types (DealerSalesPage)

```typescript
// State interfaces
interface SalesStats {
  totalSold: number;
  totalRevenue: number;
  averagePrice: number;
  averageDaysToSell: number;
  thisMonth: number;
  thisMonthRevenue: number;
  lastMonth: number;
  lastMonthRevenue: number;
}

// Component props
interface StatCardProps {
  title: string;
  value: string | number;
  subtitle?: string;
  icon: React.ReactNode;
  color: "blue" | "green" | "purple" | "amber";
  trend?: {
    value: number;
    isPositive: boolean;
  };
}

interface SaleCardProps {
  vehicle: DealerVehicle;
  formatCurrency: (value: number) => string;
  formatDate: (date: string | Date | undefined) => string;
}

// From vehicleService
interface DealerVehicle {
  id: string;
  title: string;
  make: string;
  model: string;
  year: number;
  price: number;
  mileage: number;
  status: "active" | "sold" | "draft" | "expired";
  images: string[];
  viewCount?: number;
  inquiryCount?: number;
  soldAt?: string;
  publishedAt?: string;
  createdAt?: string;
}
```

### Market Analysis Types

```typescript
// State interface
interface MarketAnalysisState {
  selectedCategory?: string;
  timeRange: "week" | "month" | "quarter" | "year";
  searchTerm?: string;
}

// From vehicleIntelligenceService
interface CategoryDemandDto {
  category: string;
  demandLevel: "VeryLow" | "Low" | "Medium" | "High" | "VeryHigh";
  demandScore: number; // 0-100
  avgDaysToSale: number;
  totalSearches: number;
  activeListings: number;
}
```

### Pricing Intelligence Types

```typescript
// Form state
interface PricingFormData {
  make: string;
  model: string;
  year: number;
  mileage: number;
  condition: "Excellent" | "Good" | "Fair" | "Poor";
  fuelType: string;
  transmission: string;
  currentPrice: number;
  photoCount: number;
}

// Tab state
type ActiveTab = "pricing" | "demand";

// PricingWidget props
interface PricingWidgetProps {
  make: string;
  model: string;
  year: number;
  mileage: number;
  condition: string;
  fuelType: string;
  transmission: string;
  currentPrice: number;
  photoCount: number;
  viewCount: number;
  daysListed: number;
  onPriceChange?: (suggestedPrice: number) => void;
}

// DemandPredictor props
interface DemandPredictorProps {
  make: string;
  model: string;
  year: number;
  fuelType: string;
  transmission: string;
}
```

---

## Componentes

### 1. DealerSalesPage (547 líneas)

**Ubicación:** `src/pages/dealer/DealerSalesPage.tsx`

**Layout:** DealerPortalLayout

**Características:**

- Estadísticas de ventas en tiempo real
- Comparación mensual (este mes vs anterior)
- Historial de vehículos vendidos
- Búsqueda y ordenamiento
- Cálculo de días en venta por vehículo
- Engagement metrics (views, inquiries)

**State Management:**

```typescript
const [soldVehicles, setSoldVehicles] = useState<DealerVehicle[]>([]);
const [stats, setStats] = useState<SalesStats>({...});
const [isLoading, setIsLoading] = useState(true);
const [error, setError] = useState<string | null>(null);
const [searchTerm, setSearchTerm] = useState('');
const [sortBy, setSortBy] = useState<'date' | 'price'>('date');
```

**Stats Cards (4 métricas):**

| Card             | Icon         | Color  | Description                 |
| ---------------- | ------------ | ------ | --------------------------- |
| Total Vendidos   | FaCar        | blue   | Count de vehículos vendidos |
| Ingresos Totales | FiDollarSign | green  | Suma de precios + trend %   |
| Precio Promedio  | FiTrendingUp | purple | Promedio por vehículo       |
| Tiempo Promedio  | FiClock      | amber  | Días promedio en vender     |

**Cálculo de días en venta:**

```typescript
const daysToSell = (() => {
  if (!vehicle.soldAt) return null;
  const soldDate = new Date(vehicle.soldAt);
  const publishedDate = vehicle.publishedAt
    ? new Date(vehicle.publishedAt)
    : vehicle.createdAt
      ? new Date(vehicle.createdAt)
      : soldDate;
  const diff = soldDate.getTime() - publishedDate.getTime();
  return Math.ceil(diff / (1000 * 60 * 60 * 24));
})();
```

**Formato de moneda (DOP):**

```typescript
const formatCurrency = (value: number) =>
  new Intl.NumberFormat("es-DO", {
    style: "currency",
    currency: "DOP",
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(value);
```

---

### 2. MarketAnalysisPage (363 líneas)

**Ubicación:** `src/pages/dealer/MarketAnalysisPage.tsx`

**Layout:** DealerPortalLayout

**Características:**

- Análisis de demanda por categoría
- Filtros de tiempo (semana/mes/trimestre/año)
- Búsqueda de categorías
- Export a CSV
- TanStack Query para data fetching

**TanStack Query Setup:**

```typescript
const {
  data: demandByCategory = [],
  isLoading: demandLoading,
  error: demandError,
  refetch: refetchDemand,
} = useQuery({
  queryKey: ["demand-categories"],
  queryFn: () => vehicleIntelligenceService.getDemandByCategory(),
  staleTime: 5 * 60 * 1000, // 5 minutes
});
```

**Demand Level Colors:**

```typescript
const getDemandLevelColor = (level: string): string => {
  const levelStr = String(level).toLowerCase();
  if (levelStr.includes("veryhigh"))
    return "text-green-700 bg-green-100 border-green-300";
  if (levelStr.includes("high"))
    return "text-green-600 bg-green-50 border-green-200";
  if (levelStr.includes("medium"))
    return "text-yellow-600 bg-yellow-50 border-yellow-200";
  if (levelStr.includes("low"))
    return "text-orange-600 bg-orange-50 border-orange-200";
  if (levelStr.includes("verylow"))
    return "text-red-700 bg-red-100 border-red-300";
  return "text-gray-600 bg-gray-50 border-gray-200";
};
```

**Demand Level Text Mapping:**

| Level    | Spanish  |
| -------- | -------- |
| VeryHigh | Muy Alta |
| High     | Alta     |
| Medium   | Media    |
| Low      | Baja     |
| VeryLow  | Muy Baja |

**Export CSV Functionality:**

```typescript
const handleExportData = async () => {
  const csv = generateCSV(demandByCategory);
  downloadCSV(csv, 'market-analysis.csv');
};

const generateCSV = (data: CategoryDemandDto[]): string => {
  const headers = [
    'Categoría', 'Demanda', 'Score',
    'Promedio Días Venta', 'Búsquedas', 'Listados Activos'
  ];
  const rows = data.map((item) => [...]);
  return [headers, ...rows].map((row) => row.join(',')).join('\n');
};

const downloadCSV = (csv: string, filename: string) => {
  const element = document.createElement('a');
  element.setAttribute('href', 'data:text/csv;charset=utf-8,' + encodeURIComponent(csv));
  element.setAttribute('download', filename);
  element.click();
};
```

**Summary Stats (4 cards):**

| Stat             | Icon           | Color  | Calculation           |
| ---------------- | -------------- | ------ | --------------------- |
| Categorías       | FiBarChart2    | blue   | filteredDemand.length |
| Demanda Promedio | FiTarget       | green  | Average demandScore   |
| Total Búsquedas  | FiSearch       | orange | Sum totalSearches     |
| Listados Activos | FiShoppingCart | purple | Sum activeListings    |

---

### 3. PricingIntelligencePage (243 líneas)

**Ubicación:** `src/pages/PricingIntelligencePage.tsx`

**Layout:** MainLayout (público)

**Características:**

- Tabs: Pricing Analysis / Demand Prediction
- Formulario de datos del vehículo
- Componentes ML: PricingWidget, DemandPredictor
- Información cards sobre IA

**Default Form Values:**

```typescript
const [formData, setFormData] = useState({
  make: "Toyota",
  model: "Corolla",
  year: 2021,
  mileage: 35000,
  condition: "Good",
  fuelType: "Gasoline",
  transmission: "Automatic",
  currentPrice: 28000,
  photoCount: 10,
});
```

**Make Options:**

- Toyota
- Honda
- Nissan
- Hyundai
- Kia
- Ford
- Chevrolet

**Condition Options:**
| Value | Label |
|-------|-------|
| Excellent | Excelente |
| Good | Bueno |
| Fair | Regular |
| Poor | Pobre |

**Tabs Styling:**

```typescript
// Active tab
"border-blue-600 text-blue-600"; // Pricing
"border-purple-600 text-purple-600"; // Demand

// Inactive tab
"border-transparent text-gray-600 hover:text-gray-900";
```

**Info Cards (3):**

| Card | Emoji | Title                        | Description                                  |
| ---- | ----- | ---------------------------- | -------------------------------------------- |
| 1    | 🎯    | Alta Precisión               | Modelo ML analiza miles de vehículos         |
| 2    | 📈    | Predicción de Demanda        | Predice qué vehículos se venderán más rápido |
| 3    | 💡    | Recomendaciones Inteligentes | Sugerencias para vender al mejor precio      |

---

## Hooks Utilizados

### useAuth

```typescript
const { user } = useAuth();
const dealerId = user?.dealerId || user?.id;
```

### useAuthStore (Zustand)

```typescript
const user = useAuthStore((state) => state.user);
```

### TanStack Query

```typescript
const { data, isLoading, error, refetch } = useQuery({
  queryKey: ["demand-categories"],
  queryFn: () => vehicleIntelligenceService.getDemandByCategory(),
  staleTime: 5 * 60 * 1000,
});
```

---

## Servicios API

### vehicleService

```typescript
import { getDealerVehicles } from "@/services/vehicleService";

// Fetch dealer vehicles
const data = await getDealerVehicles(dealerId);
const sold = data.vehicles.filter((v) => v.status === "sold");
```

### vehicleIntelligenceService

```typescript
import vehicleIntelligenceService, {
  type CategoryDemandDto,
} from "@/services/vehicleIntelligenceService";

// Fetch demand by category
vehicleIntelligenceService.getDemandByCategory();
```

---

## Componentes de ML/IA

### PricingWidget

```typescript
<PricingWidget
  make={formData.make}
  model={formData.model}
  year={formData.year}
  mileage={formData.mileage}
  condition={formData.condition}
  fuelType={formData.fuelType}
  transmission={formData.transmission}
  currentPrice={formData.currentPrice}
  photoCount={formData.photoCount}
  viewCount={0}
  daysListed={0}
  onPriceChange={(suggestedPrice) => {
    setFormData((prev) => ({ ...prev, currentPrice: suggestedPrice }));
  }}
/>
```

### DemandPredictor

```typescript
<DemandPredictor
  make={formData.make}
  model={formData.model}
  year={formData.year}
  fuelType={formData.fuelType}
  transmission={formData.transmission}
/>
```

---

## Rutas

```typescript
// Rutas definidas en App.tsx
<Route path="/dealer/sales" element={<DealerSalesPage />} />
<Route path="/dealer/market" element={<MarketAnalysisPage />} />
<Route path="/pricing-intelligence" element={<PricingIntelligencePage />} />
```

---

## Iconos Utilizados

### react-icons/fi (Feather)

- FiDollarSign - Currency/precio
- FiTrendingUp/Down - Trends
- FiCalendar - Fechas
- FiClock - Tiempo
- FiRefreshCw - Actualizar
- FiAlertCircle - Error
- FiCheckCircle - Éxito
- FiEye - Views
- FiMessageCircle - Mensajes/Inquiries
- FiSearch - Búsqueda
- FiFilter - Filtros
- FiDownload - Export
- FiBarChart2 - Analytics
- FiTarget - Target/Meta
- FiShoppingCart - Listados

### react-icons/fa (Font Awesome)

- FaCar - Vehículo

---

## Formateo de Datos

### Currency (DOP)

```typescript
new Intl.NumberFormat("es-DO", {
  style: "currency",
  currency: "DOP",
  minimumFractionDigits: 0,
  maximumFractionDigits: 0,
}).format(value);
```

### Fecha

```typescript
new Intl.DateTimeFormat("es-DO", {
  day: "numeric",
  month: "short",
  year: "numeric",
}).format(new Date(date));
```

### Números con separadores

```typescript
value.toLocaleString(); // e.g., 1,234,567
```

---

## Estados de UI

### Loading State

```typescript
{isLoading && (
  <div className="flex items-center justify-center h-64">
    <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
  </div>
)}
```

### Error State

```typescript
{error && (
  <div className="bg-red-50 border border-red-200 rounded-lg p-4">
    <p className="text-red-600">{error}</p>
    <button onClick={handleRefresh}>Reintentar</button>
  </div>
)}
```

### Empty State

```typescript
{filteredVehicles.length === 0 && (
  <div className="bg-white rounded-xl p-12 text-center">
    <FaCar className="h-16 w-16 text-gray-300 mx-auto" />
    <h3>Sin ventas registradas</h3>
    <Link to="/dealer/inventory">Ver Inventario</Link>
  </div>
)}
```

---

## Checklist de Implementación

### DealerSalesPage

- [ ] Layout con DealerPortalLayout
- [ ] 4 StatCards con métricas principales
- [ ] Comparación mensual (2 cards)
- [ ] Filtros de búsqueda y ordenamiento
- [ ] Lista de SaleCards con datos de vehículo
- [ ] Cálculo automático de días en venta
- [ ] Engagement metrics (views, inquiries)
- [ ] Loading, error, empty states
- [ ] Formato DOP para precios
- [ ] Trends con porcentaje de cambio

### MarketAnalysisPage

- [ ] Layout con DealerPortalLayout
- [ ] TanStack Query para data fetching
- [ ] Filtros: búsqueda + rango de tiempo
- [ ] Grid de category cards (3 cols)
- [ ] Demand score progress bar
- [ ] Trend icons (rising/falling/stable)
- [ ] Export CSV functionality
- [ ] Summary stats (4 cards)
- [ ] staleTime de 5 minutos

### PricingIntelligencePage

- [ ] Layout con MainLayout (público)
- [ ] Tabs: Pricing / Demand
- [ ] Formulario sticky (left column)
- [ ] PricingWidget component
- [ ] DemandPredictor component
- [ ] Info cards (3)
- [ ] Make/Model/Year selectors
- [ ] Condition dropdown
- [ ] Price input con callback
- [ ] Photo count input

---

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/dealer-sales-market.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsDealer } from "../helpers/auth";

test.describe("Dealer Sales & Market Analysis", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsDealer(page);
  });

  test("debe mostrar dashboard de ventas", async ({ page }) => {
    await page.goto("/dealer/sales");

    await expect(page.getByTestId("sales-summary")).toBeVisible();
    await expect(page.getByTestId("sales-chart")).toBeVisible();
  });

  test("debe filtrar ventas por periodo", async ({ page }) => {
    await page.goto("/dealer/sales");

    await page.getByRole("combobox", { name: /periodo/i }).click();
    await page.getByRole("option", { name: /este mes/i }).click();

    await expect(page).toHaveURL(/period=month/);
  });

  test("debe mostrar análisis de mercado", async ({ page }) => {
    await page.goto("/dealer/market-analysis");

    await expect(page.getByTestId("market-trends")).toBeVisible();
    await expect(page.getByTestId("demand-heatmap")).toBeVisible();
  });

  test("debe mostrar pricing intelligence", async ({ page }) => {
    await page.goto("/dealer/pricing-intelligence");

    await expect(page.getByTestId("price-comparison")).toBeVisible();
    await expect(page.getByTestId("suggested-prices")).toBeVisible();
  });

  test("debe exportar reporte de ventas", async ({ page }) => {
    await page.goto("/dealer/sales");

    const downloadPromise = page.waitForEvent("download");
    await page.getByRole("button", { name: /exportar csv/i }).click();
    const download = await downloadPromise;

    expect(download.suggestedFilename()).toMatch(/ventas.*\.csv/i);
  });
});
```

---

## Notas de Integración

1. **DealerSalesPage** requiere `dealerId` del usuario autenticado
2. **MarketAnalysisPage** usa `vehicleIntelligenceService` para ML data
3. **PricingIntelligencePage** es público, no requiere auth
4. Todos los precios en **DOP** (Peso Dominicano)
5. Fechas formateadas con locale `es-DO`
6. Export CSV usa encoding UTF-8
7. TanStack Query con staleTime para cache eficiente
