---
title: "88. Inventory Analytics y Vehicle Discovery"
priority: P1
estimated_time: "2 horas"
dependencies: []
apis: []
status: partial
last_updated: "2026-01-30"
---

# 88. Inventory Analytics y Vehicle Discovery

## Descripción General

Documentación de las páginas de analytics de inventario y descubrimiento de vehículos:

- **InventoryAnalyticsPage.tsx** - Analytics detallados del inventario del dealer
- **MapViewPage.tsx** - Vista de mapa con dealers y sus vehículos
- **RecentlyViewedPage.tsx** - Historial de vehículos vistos recientemente

## Arquitectura

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    INVENTORY & DISCOVERY PAGES                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                   InventoryAnalyticsPage                             │   │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │   │
│  │  │ Stats Cards  │  │ Aging Buckets│  │ Aging Table  │              │   │
│  │  │ (total,      │  │ (Donut chart)│  │ (Critical    │              │   │
│  │  │  active,etc) │  │              │  │  vehicles)   │              │   │
│  │  └──────────────┘  └──────────────┘  └──────────────┘              │   │
│  │  ┌──────────────────────────────────────────────────┐              │   │
│  │  │           Turnover Rate Bar Chart                 │              │   │
│  │  └──────────────────────────────────────────────────┘              │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                       MapViewPage                                    │   │
│  │  ┌────────────────────────────────────────────────────────────────┐ │   │
│  │  │                    Google Maps                                  │ │   │
│  │  │    🔵 Dealer A (12 vehículos)                                  │ │   │
│  │  │         🔵 Dealer B (8 vehículos)                              │ │   │
│  │  │              🔵 Dealer C (25 vehículos)                        │ │   │
│  │  └────────────────────────────────────────────────────────────────┘ │   │
│  │  ┌──────────────────────┐                                           │   │
│  │  │ Dealer Detail Panel  │ ← Click on marker                        │   │
│  │  │ - Images carousel    │                                           │   │
│  │  │ - Contact buttons    │                                           │   │
│  │  │ - Share              │                                           │   │
│  │  └──────────────────────┘                                           │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    RecentlyViewedPage                                │   │
│  │  ┌─────────────────────────────────────────────────────────────┐    │   │
│  │  │  Grouped by Date: Today | Yesterday | This Week | Older    │    │   │
│  │  └─────────────────────────────────────────────────────────────┘    │   │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │   │
│  │  │ Vehicle Card │  │ Vehicle Card │  │ Vehicle Card │              │   │
│  │  │ + Price Δ    │  │ + Sold badge │  │ + Favorite   │              │   │
│  │  └──────────────┘  └──────────────┘  └──────────────┘              │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 1. InventoryAnalyticsPage.tsx

### Ubicación

`frontend/web/src/pages/dealer/InventoryAnalyticsPage.tsx`

### Descripción

Dashboard de analytics específico para el inventario del dealer. Muestra métricas de aging, turnover y vehículos que necesitan atención.

### Ruta

`/dealer/inventory/analytics`

### Características

- **Stats Cards** - Total vehículos, activos, pausados, vendidos
- **Aging Buckets** - Distribución por antigüedad (0-15, 16-30, 31-45, 46-60, 61-90, 90+ días)
- **Donut Chart** - Visualización del aging
- **Aging Table** - Lista de vehículos críticos (>60 días)
- **Turnover Bar Chart** - Tasa de rotación por período
- **Export** - Descargar reporte CSV

### Líneas de Código

~864 líneas

### Dependencias

```typescript
import { Bar, Doughnut } from "react-chartjs-2";
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  Title,
  Tooltip,
  Legend,
  ArcElement,
  Filler,
} from "chart.js";
import MainLayout from "../../layouts/MainLayout";
```

### TypeScript Types

```typescript
interface InventoryStats {
  totalVehicles: number;
  activeVehicles: number;
  pausedVehicles: number;
  soldThisMonth: number;
  totalValue: number;
  avgPrice: number;
  avgDaysOnMarket: number;
  turnoverRate: number; // 0-1 (20% = 0.2)
}

interface AgingBucket {
  label: string;
  minDays: number;
  maxDays: number;
  count: number;
  value: number; // Valor total en DOP
  percentage: number;
  color: string;
  status: "healthy" | "warning" | "critical";
}

interface VehicleAgingItem {
  id: string;
  title: string;
  make: string;
  model: string;
  year: number;
  price: number;
  daysOnMarket: number;
  views: number;
  contacts: number;
  priceReduction: number; // % ya reducido
  thumbnail: string;
  status: "active" | "paused";
}

interface TurnoverData {
  period: string;
  vehiclesSold: number;
  avgInventory: number;
  turnoverRate: number;
}
```

### Mock Data - Stats

```typescript
const mockStats: InventoryStats = {
  totalVehicles: 60,
  activeVehicles: 52,
  pausedVehicles: 8,
  soldThisMonth: 12,
  totalValue: 1580000,
  avgPrice: 26333,
  avgDaysOnMarket: 35,
  turnoverRate: 0.2,
};
```

### Mock Data - Aging Buckets

```typescript
const mockAgingBuckets: AgingBucket[] = [
  {
    label: "0-15 días",
    minDays: 0,
    maxDays: 15,
    count: 18,
    value: 485000,
    percentage: 30.7,
    color: "#10B981",
    status: "healthy",
  },
  {
    label: "16-30 días",
    minDays: 16,
    maxDays: 30,
    count: 15,
    value: 420000,
    percentage: 26.6,
    color: "#22C55E",
    status: "healthy",
  },
  {
    label: "31-45 días",
    minDays: 31,
    maxDays: 45,
    count: 12,
    value: 310000,
    percentage: 19.6,
    color: "#FBBF24",
    status: "warning",
  },
  {
    label: "46-60 días",
    minDays: 46,
    maxDays: 60,
    count: 8,
    value: 195000,
    percentage: 12.3,
    color: "#F97316",
    status: "warning",
  },
  {
    label: "61-90 días",
    minDays: 61,
    maxDays: 90,
    count: 5,
    value: 125000,
    percentage: 7.9,
    color: "#EF4444",
    status: "critical",
  },
  {
    label: "90+ días",
    minDays: 91,
    maxDays: 9999,
    count: 2,
    value: 45000,
    percentage: 2.8,
    color: "#DC2626",
    status: "critical",
  },
];
```

### Mock Data - Aging Vehicles

```typescript
const mockAgingVehicles: VehicleAgingItem[] = [
  {
    id: "1",
    title: "Honda Civic 2020",
    make: "Honda",
    model: "Civic",
    year: 2020,
    price: 22000,
    daysOnMarket: 95,
    views: 120,
    contacts: 3,
    priceReduction: 0,
    thumbnail: "",
    status: "active",
  },
  // ... más vehículos
];
```

### Chart Configuration

```typescript
// Donut Chart para Aging Distribution
const agingDonutData = {
  labels: mockAgingBuckets.map((b) => b.label),
  datasets: [
    {
      data: mockAgingBuckets.map((b) => b.count),
      backgroundColor: mockAgingBuckets.map((b) => b.color),
      borderWidth: 0,
    },
  ],
};

// Bar Chart para Turnover
const turnoverBarData = {
  labels: turnoverData.map((t) => t.period),
  datasets: [
    {
      label: "Turnover Rate",
      data: turnoverData.map((t) => t.turnoverRate * 100),
      backgroundColor: "#3B82F6",
    },
  ],
};
```

---

## 2. MapViewPage.tsx

### Ubicación

`frontend/web/src/pages/vehicles/MapViewPage.tsx`

### Descripción

Vista de mapa interactivo que muestra dealers con sus vehículos. Usa Google Maps con markers personalizados.

### Ruta

`/vehicles/map`

### Características

- **Google Maps** - Mapa interactivo con @react-google-maps/api
- **Custom Markers** - OverlayView con info del dealer
- **Dealer Panel** - Panel lateral con detalles del dealer seleccionado
- **Filtros** - Por verificación y distancia
- **Share** - Copiar link o compartir por WhatsApp
- **URL State** - Preserva dealer seleccionado en URL params

### Líneas de Código

~613 líneas

### Dependencias

```typescript
import { GoogleMap, useJsApiLoader, OverlayView } from "@react-google-maps/api";
import MainLayout from "@/layouts/MainLayout";
import {
  getDealersWithVehicles,
  type DealerLocation,
} from "@/services/dealerService";
import { integrationConfig } from "@/config/env";
import { motion, AnimatePresence } from "framer-motion";
```

### TypeScript Types

```typescript
interface DealerLocation {
  id: string;
  name: string;
  address: string;
  latitude: number;
  longitude: number;
  phone: string;
  rating: number;
  reviewCount: number;
  isVerified: boolean;
  vehicles: VehicleSummary[];
  businessHours: Record<string, { open: string; close: string }>;
}

interface VehicleSummary {
  id: string;
  title: string;
  price: number;
  imageUrl: string;
}

interface DealerFilters {
  verified: boolean;
  maxDistance: number; // en km, 0 = mostrar todos
}
```

### Estado del Componente

```typescript
const MapViewPage = () => {
  const navigate = useNavigate();
  const location = useLocation();

  // URL params
  const urlParams = new URLSearchParams(location.search);
  const dealerIdFromUrl = urlParams.get('dealer');

  // State
  const [dealers, setDealers] = useState<DealerLocation[]>([]);
  const [isLoadingDealers, setIsLoadingDealers] = useState(true);
  const [dealersError, setDealersError] = useState<string | null>(null);
  const [selectedDealer, setSelectedDealer] = useState<string | null>(dealerIdFromUrl);
  const [hoveredDealer, setHoveredDealer] = useState<string | null>(null);
  const [showFilters, setShowFilters] = useState(false);
  const [filters, setFilters] = useState<DealerFilters>({ verified: false, maxDistance: 0 });
  const [userLocation] = useState({ lat: 18.4861, lng: -69.9312 });  // Santo Domingo default
```

### Google Maps Configuration

```typescript
const GOOGLE_MAPS_API_KEY = integrationConfig.googleMapsKey || "YOUR_KEY";

const mapContainerStyle = {
  width: "100%",
  height: "100vh",
};

const { isLoaded, loadError } = useJsApiLoader({
  googleMapsApiKey: GOOGLE_MAPS_API_KEY,
  id: "google-map-script",
});
```

### Cálculo de Distancia

```typescript
const getDistance = useCallback(
  (dealer: DealerLocation) => {
    const R = 6371; // Radio de la Tierra en km
    const dLat = ((dealer.latitude - userLocation.lat) * Math.PI) / 180;
    const dLon = ((dealer.longitude - userLocation.lng) * Math.PI) / 180;
    const a =
      Math.sin(dLat / 2) * Math.sin(dLat / 2) +
      Math.cos((userLocation.lat * Math.PI) / 180) *
        Math.cos((dealer.latitude * Math.PI) / 180) *
        Math.sin(dLon / 2) *
        Math.sin(dLon / 2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    const distance = R * c;
    return distance < 1
      ? `${Math.round(distance * 1000)}m`
      : `${distance.toFixed(1)} km`;
  },
  [userLocation],
);
```

### Compartir Dealer

```typescript
const handleShare = useCallback(
  (dealer: DealerLocation, method: "copy" | "whatsapp") => {
    const url = `${window.location.origin}/vehicles/map?dealer=${dealer.id}`;
    const message = `🚗 ${dealer.name}\n📍 ${dealer.address}\n⭐ ${dealer.rating.toFixed(1)}/5 (${dealer.reviewCount} reseñas)\n🔗 ${url}`;

    if (method === "copy") {
      navigator.clipboard.writeText(url);
      alert("¡Enlace copiado al portapapeles!");
    } else if (method === "whatsapp") {
      window.open(
        `https://wa.me/?text=${encodeURIComponent(message)}`,
        "_blank",
      );
    }
  },
  [],
);
```

### Formateo de Precios

```typescript
const formatPrice = (price: number) => {
  return new Intl.NumberFormat("es-DO", {
    style: "currency",
    currency: "DOP",
    maximumFractionDigits: 0,
  }).format(price);
};
```

---

## 3. RecentlyViewedPage.tsx

### Ubicación

`frontend/web/src/pages/vehicles/RecentlyViewedPage.tsx`

### Descripción

Historial de vehículos que el usuario ha visto recientemente. Permite retomar búsquedas y muestra cambios de precio.

### Ruta

`/vehicles/recently-viewed`

### Características

- **Agrupación por Fecha** - Today, Yesterday, This Week, Older
- **Price Change Indicator** - Muestra si el precio subió/bajó
- **Sold Badge** - Indica si el vehículo fue vendido
- **Favorite Toggle** - Agregar/quitar de favoritos
- **Remove** - Eliminar del historial
- **Clear All** - Limpiar todo el historial
- **View Modes** - Grid o List

### Líneas de Código

~501 líneas

### TypeScript Types

```typescript
interface ViewedVehicle {
  id: string;
  slug: string;
  title: string;
  price: number;
  year: number;
  mileage: number;
  location: string;
  imageUrl: string;
  viewedAt: string;
  priceChange?: {
    type: "up" | "down";
    amount: number;
    percentage: number;
  };
  isFavorite: boolean;
  isSold: boolean;
}
```

### Estado del Componente

```typescript
const RecentlyViewedPage = () => {
  const [viewMode, setViewMode] = useState<'grid' | 'list'>('grid');
  const [vehicles, setVehicles] = useState<ViewedVehicle[]>([]);
  const [isLoading, setIsLoading] = useState(true);
```

### Formateo de Tiempo

```typescript
const formatTimeAgo = (dateString: string) => {
  const date = new Date(dateString);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMins = Math.floor(diffMs / (1000 * 60));
  const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

  if (diffMins < 60) return `Hace ${diffMins} minutos`;
  if (diffHours < 24) return `Hace ${diffHours} horas`;
  if (diffDays === 1) return "Ayer";
  return `Hace ${diffDays} días`;
};
```

### Agrupación por Fecha

```typescript
const groupVehiclesByDate = () => {
  const today: ViewedVehicle[] = [];
  const yesterday: ViewedVehicle[] = [];
  const thisWeek: ViewedVehicle[] = [];
  const older: ViewedVehicle[] = [];

  const now = new Date();
  const todayStart = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const yesterdayStart = new Date(todayStart.getTime() - 24 * 60 * 60 * 1000);
  const weekStart = new Date(todayStart.getTime() - 7 * 24 * 60 * 60 * 1000);

  vehicles.forEach((v) => {
    const viewedDate = new Date(v.viewedAt);
    if (viewedDate >= todayStart) {
      today.push(v);
    } else if (viewedDate >= yesterdayStart) {
      yesterday.push(v);
    } else if (viewedDate >= weekStart) {
      thisWeek.push(v);
    } else {
      older.push(v);
    }
  });

  return { today, yesterday, thisWeek, older };
};
```

### Handlers

```typescript
const handleToggleFavorite = (id: string) => {
  setVehicles((prev) =>
    prev.map((v) => (v.id === id ? { ...v, isFavorite: !v.isFavorite } : v)),
  );
};

const handleRemove = (id: string) => {
  setVehicles((prev) => prev.filter((v) => v.id !== id));
};

const handleClearAll = () => {
  if (confirm("¿Estás seguro de que deseas borrar todo tu historial?")) {
    setVehicles([]);
  }
};
```

### Mock Data de Ejemplo

```typescript
const mockVehicles: ViewedVehicle[] = [
  {
    id: "1",
    slug: "toyota-camry-2023",
    title: "Toyota Camry 2023 XSE",
    price: 1850000,
    year: 2023,
    mileage: 15000,
    location: "Santo Domingo",
    imageUrl: "https://images.unsplash.com/photo-...",
    viewedAt: new Date(Date.now() - 1000 * 60 * 30).toISOString(), // 30 min ago
    isFavorite: true,
    isSold: false,
  },
  {
    id: "2",
    slug: "honda-accord-2022",
    title: "Honda Accord 2022 Sport",
    price: 1650000,
    year: 2022,
    mileage: 22000,
    location: "Santiago",
    imageUrl: "...",
    viewedAt: new Date(Date.now() - 1000 * 60 * 60 * 2).toISOString(), // 2 hours ago
    priceChange: { type: "down", amount: 50000, percentage: 3 },
    isFavorite: false,
    isSold: false,
  },
];
```

---

## API Endpoints

### InventoryAnalyticsPage

```
GET /api/dealers/{dealerId}/inventory/analytics
Response: {
  stats: InventoryStats;
  agingBuckets: AgingBucket[];
  agingVehicles: VehicleAgingItem[];
  turnoverHistory: TurnoverData[];
}

GET /api/dealers/{dealerId}/inventory/export
Query: ?format=csv
Response: File download
```

### MapViewPage

```
GET /api/dealers/with-vehicles
Query: ?lat=18.4861&lng=-69.9312&radius=50
Response: DealerLocation[]
```

### RecentlyViewedPage

```
GET /api/users/{userId}/recently-viewed
Query: ?limit=50
Response: ViewedVehicle[]

DELETE /api/users/{userId}/recently-viewed/{vehicleId}

DELETE /api/users/{userId}/recently-viewed
(Clear all)
```

---

## Servicios Relacionados

| Servicio                    | Uso                            |
| --------------------------- | ------------------------------ |
| `inventoryAnalyticsService` | Analytics de inventario        |
| `dealerService`             | Lista de dealers con vehículos |
| `userBehaviorService`       | Historial de vistas            |
| `favoritesService`          | Toggle de favoritos            |

---

## Checklist de Validación

### InventoryAnalyticsPage

- [ ] Stats cards muestran valores correctos
- [ ] Donut chart renderiza con colores correctos
- [ ] Aging table muestra vehículos críticos primero
- [ ] Status badges (healthy/warning/critical) con colores
- [ ] Bar chart de turnover renderiza
- [ ] Export CSV funciona

### MapViewPage

- [ ] Google Maps carga correctamente
- [ ] Markers de dealers se muestran
- [ ] Click en marker abre panel de detalles
- [ ] Filtro de verificados funciona
- [ ] Filtro de distancia funciona
- [ ] Share copy funciona
- [ ] Share WhatsApp abre en nueva pestaña
- [ ] URL preserva dealer seleccionado
- [ ] Loading state mientras carga mapa/dealers

### RecentlyViewedPage

- [ ] Vehículos agrupados correctamente por fecha
- [ ] Time ago formatea bien (minutos, horas, ayer, días)
- [ ] Price change muestra flecha y color correcto
- [ ] Sold badge aparece en vehículos vendidos
- [ ] Toggle favorite funciona
- [ ] Remove individual funciona
- [ ] Clear all con confirmación
- [ ] View mode grid/list toggle

---

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/inventory-analytics.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsDealer } from "../helpers/auth";

test.describe("Inventory Analytics", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsDealer(page);
  });

  test("debe mostrar dashboard de inventario analytics", async ({ page }) => {
    await page.goto("/dealer/inventory-analytics");

    await expect(page.getByTestId("inventory-metrics")).toBeVisible();
  });

  test("debe mostrar mapa de vehículos", async ({ page }) => {
    await page.goto("/dealer/inventory-analytics/map");

    await expect(page.getByTestId("vehicles-map")).toBeVisible();
  });

  test("debe filtrar por tipo de vehículo", async ({ page }) => {
    await page.goto("/dealer/inventory-analytics");

    await page.getByRole("combobox", { name: /tipo/i }).click();
    await page.getByRole("option", { name: /suv/i }).click();

    await expect(page).toHaveURL(/type=suv/);
  });

  test("debe mostrar historial de precios", async ({ page }) => {
    await page.goto("/dealer/inventory-analytics");

    await page.getByTestId("vehicle-row").first().click();
    await expect(page.getByTestId("price-history-chart")).toBeVisible();
  });

  test("debe mostrar vehículos vistos recientemente", async ({ page }) => {
    await page.goto("/dealer/inventory-analytics");

    await expect(page.getByTestId("recently-viewed")).toBeVisible();
  });
});
```

---

## Notas de Implementación

1. **Google Maps API Key**: Configurar en variables de entorno
2. **UserBehaviorService**: En producción, obtener historial de backend
3. **Real Data**: MapViewPage usa `getDealersWithVehicles()` del service
4. **Chart.js**: Registrar todos los componentes necesarios
5. **Responsive**: MapViewPage oculta panel lateral en mobile
6. **Performance**: Limitar historial a últimos 50 vehículos
7. **Accessibility**: Labels ARIA en controles del mapa
