---
title: "86. Dealer Alerts y Reports"
priority: P1
estimated_time: "2 horas"
dependencies: []
apis: []
status: partial
last_updated: "2026-01-30"
---

# 86. Dealer Alerts y Reports

## Descripción General

Documentación de las páginas de alertas y reportes para dealers:

- **DealerAlertsPage.tsx** - Alertas de precio y búsquedas guardadas del dealer
- **AlertsManagementPage.tsx** - Gestión de alertas del sistema para dealers
- **ReportsPage.tsx** - Dashboard de reportes con métricas y exportación

## Arquitectura

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         DEALER ALERTS & REPORTS                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                     DealerAlertsPage                                 │   │
│  │  ┌─────────────────┐    ┌─────────────────┐                         │   │
│  │  │  Price Alerts   │    │ Saved Searches  │                         │   │
│  │  │  Tab            │    │    Tab          │                         │   │
│  │  │  - Vehicle list │    │  - Criteria     │                         │   │
│  │  │  - Toggle       │    │  - Frequency    │                         │   │
│  │  │  - Delete       │    │  - Notifications│                         │   │
│  │  └─────────────────┘    └─────────────────┘                         │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                              │                                              │
│              GET /api/alerts/price | /api/alerts/searches                   │
│                                                                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                   AlertsManagementPage                               │   │
│  │  ┌────────────────────────────────────────────────────────────────┐ │   │
│  │  │  Critical | Warning | Info Alerts                              │ │   │
│  │  │  - slow_inventory, price_drop_needed, lead_response_slow      │ │   │
│  │  │  - competitor_price, low_engagement, performance_drop         │ │   │
│  │  │  - payment_due, review_needed, opportunity                    │ │   │
│  │  └────────────────────────────────────────────────────────────────┘ │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                       ReportsPage                                    │   │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │   │
│  │  │  Metrics     │  │   Charts     │  │  Export      │              │   │
│  │  │  Summary     │  │  (Chart.js)  │  │  PDF/Excel   │              │   │
│  │  └──────────────┘  └──────────────┘  └──────────────┘              │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 1. DealerAlertsPage.tsx

### Ubicación

`frontend/web/src/pages/dealer/DealerAlertsPage.tsx`

### Descripción

Gestión de alertas de precio y búsquedas guardadas del dealer. Sistema de tabs para organizar diferentes tipos de alertas.

### Ruta

`/dealer/alerts`

### Características

- **Tabs** - Price Alerts vs Saved Searches
- **Stats Cards** - Contadores de alertas activas, triggered, etc.
- **Toggle Active/Inactive** - Activar/desactivar alertas
- **Delete** - Eliminar alertas
- **Vehicle Info** - Carga info del vehículo para alertas de precio

### Líneas de Código

~516 líneas

### Dependencias

```typescript
import {
  getPriceAlerts,
  getSavedSearches,
  activatePriceAlert,
  deactivatePriceAlert,
  deletePriceAlert,
  activateSavedSearch,
  deactivateSavedSearch,
  deleteSavedSearch,
  formatCriteriaForDisplay,
  getFrequencyLabel,
  getConditionLabel,
  type PriceAlert,
  type SavedSearch,
} from "../../services/alertsService";
import api from "../../services/api";
```

### TypeScript Types

```typescript
// Alerta de precio
interface PriceAlert {
  id: string;
  vehicleId: string;
  targetPrice: number;
  condition: "below" | "above" | "equals";
  isActive: boolean;
  isTriggered: boolean;
  createdAt: string;
  lastTriggeredAt?: string;
}

// Búsqueda guardada
interface SavedSearch {
  id: string;
  name: string;
  criteria: Record<string, any>;
  frequency: "daily" | "weekly" | "immediate";
  sendEmailNotifications: boolean;
  isActive: boolean;
  matchCount: number;
  createdAt: string;
  lastRunAt?: string;
}
```

### Estado del Componente

```typescript
const DealerAlertsPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'price' | 'searches'>('price');
  const [priceAlerts, setPriceAlerts] = useState<PriceAlert[]>([]);
  const [savedSearches, setSavedSearches] = useState<SavedSearch[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [vehicleInfo, setVehicleInfo] = useState<Record<string, {
    title: string;
    price: number;
    imageUrl?: string;
  }>>({});
```

### Funciones Principales

```typescript
// Cargar info de vehículos para alertas de precio
const loadVehicleInfo = async (vehicleIds: string[]) => {
  const info: Record<string, {...}> = {};
  await Promise.all(
    vehicleIds.map(async (id) => {
      try {
        const response = await api.get(`/api/vehicles/${id}`);
        info[id] = {
          title: response.data.title || 'Vehículo',
          price: response.data.price || 0,
          imageUrl: response.data.images?.[0]?.url,
        };
      } catch {
        info[id] = { title: 'Vehículo no disponible', price: 0 };
      }
    })
  );
  setVehicleInfo(info);
};

// Toggle alerta de precio
const handleTogglePriceAlert = async (alert: PriceAlert) => {
  if (alert.isActive) {
    await deactivatePriceAlert(alert.id);
  } else {
    await activatePriceAlert(alert.id);
  }
  const updated = await getPriceAlerts();
  setPriceAlerts(updated);
};

// Eliminar alerta
const handleDeletePriceAlert = async (id: string) => {
  if (!confirm('¿Estás seguro de eliminar esta alerta de precio?')) return;
  await deletePriceAlert(id);
  setPriceAlerts((prev) => prev.filter((a) => a.id !== id));
};
```

### Estadísticas

```typescript
// Contadores para stats cards
const activeAlerts = priceAlerts.filter((a) => a.isActive).length;
const triggeredAlerts = priceAlerts.filter((a) => a.isTriggered).length;
const activeSearches = savedSearches.filter((s) => s.isActive).length;
const withNotifications = savedSearches.filter(
  (s) => s.sendEmailNotifications,
).length;
```

### Formateo

```typescript
const formatDate = (dateString: string) => {
  return new Date(dateString).toLocaleDateString("es-DO", {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
};

const formatPrice = (price: number) => {
  return `RD$${price.toLocaleString("es-DO")}`;
};
```

---

## 2. AlertsManagementPage.tsx

### Ubicación

`frontend/web/src/pages/dealer/AlertsManagementPage.tsx`

### Descripción

Sistema de alertas inteligentes generadas por el sistema para ayudar al dealer a optimizar su inventario y rendimiento.

### Ruta

`/dealer/alerts/management`

### Características

- **Severidades** - Critical, Warning, Info
- **Estados** - Unread, Read, Acted, Dismissed
- **Tipos de Alertas** - 10+ tipos diferentes
- **Acciones** - Ver, Marcar como leída, Actuar, Descartar
- **Filtros** - Por severidad, estado, tipo

### Líneas de Código

~668 líneas

### TypeScript Types

```typescript
type AlertSeverity = "info" | "warning" | "critical";

type AlertStatus = "unread" | "read" | "acted" | "dismissed";

type AlertType =
  | "slow_inventory" // Inventario lento (>60 días)
  | "price_drop_needed" // Sugerencia de baja de precio
  | "low_engagement" // Bajo engagement en listados
  | "lead_response_slow" // Leads sin responder
  | "competitor_price" // Competencia con mejor precio
  | "inventory_low" // Inventario bajo
  | "performance_drop" // Caída en rendimiento
  | "payment_due" // Pago próximo
  | "review_needed" // Review pendiente
  | "opportunity"; // Oportunidad de mercado

interface DealerAlert {
  id: string;
  type: AlertType;
  severity: AlertSeverity;
  status: AlertStatus;
  title: string;
  message: string;
  vehicleId?: string;
  vehicleTitle?: string;
  actionUrl?: string;
  actionLabel?: string;
  createdAt: string;
  readAt?: string;
  expiresAt?: string;
  metadata?: Record<string, any>;
}

interface AlertSummary {
  total: number;
  unread: number;
  critical: number;
  warning: number;
  info: number;
  byType: Record<AlertType, number>;
}
```

### Estilos por Severidad

```typescript
const getSeverityStyles = (severity: AlertSeverity) => {
  switch (severity) {
    case "critical":
      return {
        bg: "bg-red-50",
        border: "border-red-200",
        icon: "text-red-600",
        badge: "bg-red-100 text-red-700",
      };
    case "warning":
      return {
        bg: "bg-amber-50",
        border: "border-amber-200",
        icon: "text-amber-600",
        badge: "bg-amber-100 text-amber-700",
      };
    case "info":
      return {
        bg: "bg-blue-50",
        border: "border-blue-200",
        icon: "text-blue-600",
        badge: "bg-blue-100 text-blue-700",
      };
  }
};
```

### Ejemplos de Alertas (Mock Data)

```typescript
const mockAlerts: DealerAlert[] = [
  {
    id: "1",
    type: "slow_inventory",
    severity: "critical",
    status: "unread",
    title: "Inventario Lento - Acción Requerida",
    message: "Tienes 5 vehículos con más de 60 días en inventario.",
    actionUrl: "/inventory?filter=aging",
    actionLabel: "Ver Inventario",
    createdAt: new Date(Date.now() - 3600000).toISOString(),
    metadata: { vehicleCount: 5, avgDays: 78 },
  },
  {
    id: "2",
    type: "price_drop_needed",
    severity: "warning",
    status: "unread",
    title: "Ajuste de Precio Sugerido",
    message: "El Toyota Camry 2022 está 15% por encima del precio de mercado.",
    vehicleId: "v123",
    vehicleTitle: "Toyota Camry 2022",
    actionUrl: "/vehicles/v123/edit",
    actionLabel: "Editar Precio",
    metadata: { currentPrice: 32000, suggestedPrice: 27500, marketAvg: 27200 },
  },
  {
    id: "3",
    type: "lead_response_slow",
    severity: "critical",
    status: "unread",
    title: "Leads Sin Responder",
    message: "Tienes 3 leads calientes esperando respuesta por más de 2 horas.",
    actionUrl: "/leads?filter=pending",
    actionLabel: "Ver Leads",
    metadata: { pendingLeads: 3, avgWaitTime: 145 },
  },
  // ... más alertas
];
```

---

## 3. ReportsPage.tsx

### Ubicación

`frontend/web/src/pages/dealer/ReportsPage.tsx`

### Descripción

Dashboard de reportes con métricas detalladas, gráficos y capacidad de exportación a PDF/Excel/CSV.

### Ruta

`/dealer/reports`

### Características

- **Period Selector** - Daily, Weekly, Monthly, Custom
- **Metrics Grid** - Inventory, Engagement, Leads, Revenue
- **Charts** - Views trend, Leads trend, Conversions
- **Highlights** - Puntos destacados automáticos
- **Recommendations** - Sugerencias basadas en datos
- **Export** - PDF, Excel, CSV

### Líneas de Código

~754 líneas

### Dependencias

```typescript
import { Line } from "react-chartjs-2";
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
  Filler,
} from "chart.js";
import MainLayout from "../../layouts/MainLayout";
```

### TypeScript Types

```typescript
type ReportPeriod = "daily" | "weekly" | "monthly" | "custom";
type ExportFormat = "pdf" | "excel" | "csv";

interface ReportMetric {
  label: string;
  current: number;
  previous: number;
  change: number;
  changePercent: number;
  trend: "up" | "down" | "stable";
  format: "number" | "currency" | "percentage" | "time";
}

interface ReportData {
  period: string;
  dateRange: { start: string; end: string };
  generatedAt: string;
  metrics: {
    inventory: ReportMetric[];
    engagement: ReportMetric[];
    leads: ReportMetric[];
    revenue: ReportMetric[];
  };
  charts: {
    viewsTrend: { labels: string[]; values: number[] };
    leadsTrend: { labels: string[]; values: number[] };
    conversionsTrend: { labels: string[]; values: number[] };
  };
  highlights: string[];
  recommendations: string[];
}
```

### Métricas de Ejemplo

```typescript
const mockReportData: ReportData = {
  period: "weekly",
  dateRange: { start: "2024-01-22", end: "2024-01-28" },
  metrics: {
    inventory: [
      {
        label: "Vehículos Activos",
        current: 45,
        previous: 42,
        change: 3,
        changePercent: 7.1,
        trend: "up",
        format: "number",
      },
      {
        label: "Nuevos Listados",
        current: 8,
        previous: 5,
        change: 3,
        changePercent: 60,
        trend: "up",
        format: "number",
      },
      {
        label: "Vendidos",
        current: 5,
        previous: 4,
        change: 1,
        changePercent: 25,
        trend: "up",
        format: "number",
      },
      {
        label: "Días Promedio",
        current: 28,
        previous: 32,
        change: -4,
        changePercent: -12.5,
        trend: "down",
        format: "number",
      }, // Mejor si baja
    ],
    engagement: [
      {
        label: "Vistas Totales",
        current: 12450,
        previous: 10890,
        changePercent: 14.3,
        trend: "up",
        format: "number",
      },
      {
        label: "CTR",
        current: 3.8,
        previous: 3.2,
        changePercent: 18.8,
        trend: "up",
        format: "percentage",
      },
      // ...
    ],
    leads: [
      {
        label: "Nuevos Leads",
        current: 68,
        previous: 52,
        changePercent: 30.8,
        trend: "up",
        format: "number",
      },
      {
        label: "Tasa Conversión",
        current: 8.8,
        previous: 7.2,
        changePercent: 22.2,
        trend: "up",
        format: "percentage",
      },
      // ...
    ],
  },
  highlights: [
    "Vistas aumentaron 14.3% esta semana",
    "Tiempo de respuesta mejoró a 12 minutos",
  ],
  recommendations: [
    "Considera agregar más fotos a los listados con bajo CTR",
    "Revisa precios de inventario con más de 30 días",
  ],
};
```

### Funciones de Exportación

```typescript
const handleExport = async (format: ExportFormat) => {
  switch (format) {
    case "pdf":
      // Generar PDF con métricas y gráficos
      break;
    case "excel":
      // Exportar a Excel con hojas por categoría
      break;
    case "csv":
      // Exportar datos crudos a CSV
      break;
  }
};
```

---

## API Endpoints

### DealerAlertsPage

```
GET /api/alerts/price
Response: PriceAlert[]

POST /api/alerts/price/{id}/activate
POST /api/alerts/price/{id}/deactivate
DELETE /api/alerts/price/{id}

GET /api/alerts/searches
Response: SavedSearch[]

POST /api/alerts/searches/{id}/activate
POST /api/alerts/searches/{id}/deactivate
DELETE /api/alerts/searches/{id}
```

### AlertsManagementPage

```
GET /api/dealers/{dealerId}/alerts
Query: ?status=unread&severity=critical
Response: { alerts: DealerAlert[], summary: AlertSummary }

PATCH /api/dealers/{dealerId}/alerts/{alertId}
Body: { status: 'read' | 'acted' | 'dismissed' }
```

### ReportsPage

```
GET /api/dealers/{dealerId}/reports
Query: ?period=weekly&startDate=2024-01-22&endDate=2024-01-28
Response: ReportData

GET /api/dealers/{dealerId}/reports/export
Query: ?format=pdf|excel|csv&period=weekly
Response: File download
```

---

## Servicios Relacionados

| Servicio                 | Uso                                   |
| ------------------------ | ------------------------------------- |
| `alertsService`          | CRUD de alertas de precio y búsquedas |
| `dealerAnalyticsService` | Alertas del sistema                   |
| `reportsService`         | Generación de reportes                |
| `vehicleService`         | Info de vehículos para alertas        |

---

## Checklist de Validación

### DealerAlertsPage

- [ ] Tabs funcionan (Price Alerts / Saved Searches)
- [ ] Stats cards muestran contadores correctos
- [ ] Toggle activo/inactivo funciona
- [ ] Delete con confirmación
- [ ] Info de vehículo se carga para alertas de precio
- [ ] Formateo de fechas en español (es-DO)
- [ ] Formateo de precios en DOP

### AlertsManagementPage

- [ ] Alertas se agrupan por severidad
- [ ] Colores correctos por severidad (crítico=rojo, warning=amber, info=azul)
- [ ] Acciones funcionan (ver, marcar leída, actuar, descartar)
- [ ] Links de acción navegan correctamente
- [ ] Metadata se muestra (vehicleCount, suggestedPrice, etc.)

### ReportsPage

- [ ] Period selector funciona
- [ ] Métricas se calculan correctamente
- [ ] Trends (up/down) muestran íconos correctos
- [ ] Gráficos renderizan con Chart.js
- [ ] Export a PDF funciona
- [ ] Export a Excel funciona
- [ ] Export a CSV funciona
- [ ] Highlights y Recommendations se muestran

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/dealer-alerts-reports.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsDealer } from "../helpers/auth";

test.describe("Dealer Alerts & Reports", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsDealer(page);
  });

  test.describe("Alerts", () => {
    test("debe mostrar lista de alertas", async ({ page }) => {
      await page.goto("/dealer/alerts");

      await expect(page.getByTestId("alerts-list")).toBeVisible();
    });

    test("debe crear nueva alerta", async ({ page }) => {
      await page.goto("/dealer/alerts");

      await page.getByRole("button", { name: /nueva alerta/i }).click();
      await page.getByRole("combobox", { name: /tipo/i }).click();
      await page.getByRole("option", { name: /inventario bajo/i }).click();
      await page.getByRole("button", { name: /crear/i }).click();

      await expect(page.getByText(/alerta creada/i)).toBeVisible();
    });

    test("debe marcar alerta como leída", async ({ page }) => {
      await page.goto("/dealer/alerts");

      await page.getByTestId("alert-item").first().click();
      await expect(page.getByTestId("alert-item").first()).not.toHaveClass(
        /unread/,
      );
    });
  });

  test.describe("Reports", () => {
    test("debe mostrar dashboard de reportes", async ({ page }) => {
      await page.goto("/dealer/reports");

      await expect(page.getByTestId("reports-dashboard")).toBeVisible();
    });

    test("debe generar reporte mensual", async ({ page }) => {
      await page.goto("/dealer/reports");

      await page.getByRole("button", { name: /generar reporte/i }).click();
      await page.getByRole("combobox", { name: /tipo/i }).click();
      await page.getByRole("option", { name: /mensual/i }).click();
      await page.getByRole("button", { name: /generar/i }).click();

      await expect(page.getByText(/reporte generado/i)).toBeVisible();
    });

    test("debe exportar reporte a PDF", async ({ page }) => {
      await page.goto("/dealer/reports");

      const downloadPromise = page.waitForEvent("download");
      await page
        .getByTestId("report-row")
        .first()
        .getByRole("button", { name: /descargar pdf/i })
        .click();
      const download = await downloadPromise;

      expect(download.suggestedFilename()).toMatch(/\.pdf$/i);
    });
  });
});
```

---

## Notas de Implementación

1. **Chart.js Registration**: Registrar todos los elementos necesarios antes de usar gráficos
2. **Real-time Updates**: Considerar WebSocket para alertas críticas
3. **Export Service**: Usar servicio backend para generar PDFs (jsPDF o similar)
4. **Caching**: Cachear reportes por período para evitar recálculos
5. **Permissions**: Verificar que el dealer tiene acceso a analytics según su plan
