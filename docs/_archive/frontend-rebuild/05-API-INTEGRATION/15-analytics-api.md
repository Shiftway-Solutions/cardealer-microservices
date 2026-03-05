# 📊 15 - Analytics & Reports API

**Servicios:** AdminService (Reports), VehiclesSaleService (Listing Stats)  
**Puerto:** 8080  
**Base Path:** `/api/admin/reports`, `/api/analytics`  
**Autenticación:** ✅ Requerida

---

## 📋 Índice

1. [Descripción General](#descripción-general)
2. [Endpoints Disponibles](#endpoints-disponibles)
3. [TypeScript Types](#typescript-types)
4. [Service Layer](#service-layer)
5. [React Query Hooks](#react-query-hooks)
6. [Componentes de Ejemplo](#componentes-de-ejemplo)

---

## 📖 Descripción General

Los endpoints de Analytics proveen métricas y reportes para:

- 📈 Estadísticas de listings (vistas, contactos, favoritos)
- 👥 Métricas de usuarios y dealers
- 💰 Reportes de ventas e ingresos
- 📊 Dashboard ejecutivo
- 🚨 Reportes de contenido (moderación)

### Fuentes de Datos

| Servicio            | Datos                                     |
| ------------------- | ----------------------------------------- |
| VehiclesSaleService | Vistas, favoritos, contactos por vehículo |
| BillingService      | Ingresos, suscripciones, conversiones     |
| UserService         | Registros, actividad, retención           |
| ReviewService       | Ratings, respuestas, satisfacción         |
| AdminService        | Reportes de usuarios, moderación          |

---

## 🎯 Endpoints Disponibles

### Listing Analytics (VehiclesSaleService)

| #   | Método | Endpoint                        | Auth     | Descripción                 |
| --- | ------ | ------------------------------- | -------- | --------------------------- |
| 1   | `GET`  | `/api/vehicles/{id}/stats`      | ✅ Owner | Estadísticas de un vehículo |
| 2   | `GET`  | `/api/vehicles/my-stats`        | ✅       | Resumen de mis vehículos    |
| 3   | `POST` | `/api/vehicles/{id}/track-view` | ❌       | Registrar vista             |

### Platform Analytics (AdminService)

| #   | Método | Endpoint                        | Auth          | Descripción           |
| --- | ------ | ------------------------------- | ------------- | --------------------- |
| 4   | `GET`  | `/api/admin/analytics/overview` | ✅ Admin      | Métricas generales    |
| 5   | `GET`  | `/api/admin/analytics/users`    | ✅ Admin      | Métricas de usuarios  |
| 6   | `GET`  | `/api/admin/analytics/vehicles` | ✅ Admin      | Métricas de vehículos |
| 7   | `GET`  | `/api/admin/analytics/revenue`  | ✅ SuperAdmin | Métricas de ingresos  |
| 8   | `GET`  | `/api/admin/analytics/trends`   | ✅ Admin      | Tendencias temporales |

### User Reports (AdminService)

| #   | Método | Endpoint                          | Auth     | Descripción                 |
| --- | ------ | --------------------------------- | -------- | --------------------------- |
| 9   | `GET`  | `/api/admin/reports`              | ✅ Admin | Listar reportes de usuarios |
| 10  | `GET`  | `/api/admin/reports/{id}`         | ✅ Admin | Detalle de reporte          |
| 11  | `POST` | `/api/admin/reports/{id}/resolve` | ✅ Admin | Resolver reporte            |

### Export

| #   | Método | Endpoint                      | Auth     | Descripción                |
| --- | ------ | ----------------------------- | -------- | -------------------------- |
| 12  | `GET`  | `/api/admin/analytics/export` | ✅ Admin | Exportar datos (CSV/Excel) |

---

## 📝 Detalle de Endpoints

### 1. GET `/api/vehicles/{id}/stats` - Stats de un Vehículo

**Auth:** ✅ Owner del vehículo

**Response 200:**

```json
{
  "vehicleId": "vehicle-123",
  "title": "Toyota Corolla 2024",
  "stats": {
    "totalViews": 1250,
    "uniqueViews": 890,
    "todayViews": 45,
    "weekViews": 280,
    "monthViews": 1100,
    "favoriteCount": 34,
    "contactRequests": 12,
    "phoneClicks": 28,
    "whatsappClicks": 15,
    "shareCount": 8
  },
  "trends": {
    "viewsTrend": 15.5,
    "contactsTrend": 8.2
  },
  "topSources": [
    { "source": "search", "count": 520 },
    { "source": "homepage", "count": 380 },
    { "source": "social", "count": 150 }
  ],
  "lastUpdated": "2026-01-30T10:00:00Z"
}
```

---

### 4. GET `/api/admin/analytics/overview` - Métricas Generales

**Auth:** ✅ Admin

**Query Params:**

- `period` (string): `today`, `week`, `month`, `year`
- `compareWith` (string): `previous` para comparar con período anterior

**Response 200:**

```json
{
  "period": "month",
  "dateRange": {
    "from": "2026-01-01T00:00:00Z",
    "to": "2026-01-30T23:59:59Z"
  },
  "metrics": {
    "totalUsers": 15420,
    "newUsers": 1245,
    "activeUsers": 8930,
    "totalDealers": 156,
    "activeDealers": 142,
    "totalVehicles": 3450,
    "activeListings": 2890,
    "soldVehicles": 234,
    "totalRevenue": 25680.0,
    "avgOrderValue": 109.74
  },
  "comparison": {
    "newUsers": { "value": 1245, "change": 12.5, "trend": "up" },
    "activeListings": { "value": 2890, "change": -3.2, "trend": "down" },
    "totalRevenue": { "value": 25680, "change": 18.7, "trend": "up" }
  }
}
```

---

### 7. GET `/api/admin/analytics/revenue` - Métricas de Ingresos

**Auth:** ✅ SuperAdmin

**Query Params:**

- `from` (DateTime)
- `to` (DateTime)
- `groupBy` (string): `day`, `week`, `month`

**Response 200:**

```json
{
  "summary": {
    "totalRevenue": 25680.0,
    "subscriptionRevenue": 22450.0,
    "listingFees": 3230.0,
    "mrr": 18500.0,
    "arr": 222000.0,
    "avgRevenuePerDealer": 158.33,
    "churnRate": 2.5
  },
  "byPlan": {
    "Starter": { "count": 45, "revenue": 1755.0 },
    "Pro": { "count": 78, "revenue": 8034.0 },
    "Enterprise": { "count": 23, "revenue": 5497.0 }
  },
  "timeline": [
    { "date": "2026-01-01", "revenue": 850.0, "newSubscriptions": 3 },
    { "date": "2026-01-02", "revenue": 920.0, "newSubscriptions": 2 }
  ]
}
```

---

### 11. POST `/api/admin/reports/{id}/resolve` - Resolver Reporte

**Auth:** ✅ Admin

**Request Body:**

```json
{
  "resolvedBy": "admin@okla.com.do",
  "resolution": "Content removed - violated terms of service",
  "reporterEmail": "user@example.com",
  "reportSubject": "Inappropriate listing content"
}
```

**Response 200:**

```json
{
  "success": true,
  "message": "Report resolved successfully"
}
```

---

## 🔧 TypeScript Types

```typescript
// ============================================================================
// LISTING ANALYTICS
// ============================================================================

export interface VehicleStats {
  vehicleId: string;
  title: string;
  stats: {
    totalViews: number;
    uniqueViews: number;
    todayViews: number;
    weekViews: number;
    monthViews: number;
    favoriteCount: number;
    contactRequests: number;
    phoneClicks: number;
    whatsappClicks: number;
    shareCount: number;
  };
  trends: {
    viewsTrend: number; // % change
    contactsTrend: number;
  };
  topSources: Array<{
    source: string;
    count: number;
  }>;
  lastUpdated: string;
}

export interface MyListingsStats {
  totalListings: number;
  activeListings: number;
  soldListings: number;
  totalViews: number;
  totalContacts: number;
  conversionRate: number;
  topPerforming: Array<{
    vehicleId: string;
    title: string;
    views: number;
    contacts: number;
  }>;
}

// ============================================================================
// PLATFORM ANALYTICS
// ============================================================================

export interface AnalyticsOverview {
  period: AnalyticsPeriod;
  dateRange: {
    from: string;
    to: string;
  };
  metrics: PlatformMetrics;
  comparison?: Record<string, MetricComparison>;
}

export type AnalyticsPeriod = "today" | "week" | "month" | "quarter" | "year";

export interface PlatformMetrics {
  totalUsers: number;
  newUsers: number;
  activeUsers: number;
  totalDealers: number;
  activeDealers: number;
  totalVehicles: number;
  activeListings: number;
  soldVehicles: number;
  totalRevenue: number;
  avgOrderValue: number;
}

export interface MetricComparison {
  value: number;
  change: number; // % change
  trend: "up" | "down" | "stable";
}

// ============================================================================
// REVENUE ANALYTICS
// ============================================================================

export interface RevenueAnalytics {
  summary: RevenueSummary;
  byPlan: Record<string, { count: number; revenue: number }>;
  timeline: Array<{
    date: string;
    revenue: number;
    newSubscriptions: number;
  }>;
}

export interface RevenueSummary {
  totalRevenue: number;
  subscriptionRevenue: number;
  listingFees: number;
  mrr: number; // Monthly Recurring Revenue
  arr: number; // Annual Recurring Revenue
  avgRevenuePerDealer: number;
  churnRate: number;
}

// ============================================================================
// USER REPORTS (Moderation)
// ============================================================================

export interface UserReport {
  id: string;
  reporterId: string;
  reporterEmail: string;
  targetType: "User" | "Listing" | "Review" | "Dealer";
  targetId: string;
  reason: ReportReason;
  description: string;
  status: ReportStatus;
  priority: "Low" | "Medium" | "High" | "Critical";
  createdAt: string;
  resolvedAt?: string;
  resolvedBy?: string;
  resolution?: string;
}

export type ReportReason =
  | "spam"
  | "fraud"
  | "inappropriate_content"
  | "fake_listing"
  | "harassment"
  | "other";

export type ReportStatus = "Pending" | "InReview" | "Resolved" | "Dismissed";

export interface ResolveReportRequest {
  resolvedBy: string;
  resolution: string;
  reporterEmail: string;
  reportSubject: string;
}

// ============================================================================
// EXPORT
// ============================================================================

export interface ExportRequest {
  type: "users" | "vehicles" | "transactions" | "dealers";
  format: "csv" | "xlsx";
  dateFrom?: string;
  dateTo?: string;
  filters?: Record<string, unknown>;
}
```

---

## 📡 Service Layer

```typescript
// src/services/analyticsService.ts
import { apiClient } from "./api-client";
import type {
  VehicleStats,
  MyListingsStats,
  AnalyticsOverview,
  RevenueAnalytics,
  UserReport,
  ResolveReportRequest,
  ExportRequest,
  AnalyticsPeriod,
} from "@/types/analytics";

class AnalyticsService {
  // ============================================================================
  // LISTING ANALYTICS
  // ============================================================================

  async getVehicleStats(vehicleId: string): Promise<VehicleStats> {
    const response = await apiClient.get<VehicleStats>(
      `/api/vehicles/${vehicleId}/stats`,
    );
    return response.data;
  }

  async getMyListingsStats(): Promise<MyListingsStats> {
    const response = await apiClient.get<MyListingsStats>(
      "/api/vehicles/my-stats",
    );
    return response.data;
  }

  async trackView(vehicleId: string): Promise<void> {
    await apiClient.post(`/api/vehicles/${vehicleId}/track-view`);
  }

  // ============================================================================
  // PLATFORM ANALYTICS (Admin)
  // ============================================================================

  async getOverview(
    period: AnalyticsPeriod = "month",
  ): Promise<AnalyticsOverview> {
    const response = await apiClient.get<AnalyticsOverview>(
      "/api/admin/analytics/overview",
      { params: { period, compareWith: "previous" } },
    );
    return response.data;
  }

  async getUserAnalytics(period: AnalyticsPeriod = "month"): Promise<any> {
    const response = await apiClient.get("/api/admin/analytics/users", {
      params: { period },
    });
    return response.data;
  }

  async getVehicleAnalytics(period: AnalyticsPeriod = "month"): Promise<any> {
    const response = await apiClient.get("/api/admin/analytics/vehicles", {
      params: { period },
    });
    return response.data;
  }

  async getRevenueAnalytics(
    from: string,
    to: string,
    groupBy: "day" | "week" | "month" = "day",
  ): Promise<RevenueAnalytics> {
    const response = await apiClient.get<RevenueAnalytics>(
      "/api/admin/analytics/revenue",
      { params: { from, to, groupBy } },
    );
    return response.data;
  }

  // ============================================================================
  // USER REPORTS
  // ============================================================================

  async getReports(params?: {
    status?: string;
    priority?: string;
    page?: number;
    pageSize?: number;
  }): Promise<{ items: UserReport[]; totalCount: number }> {
    const response = await apiClient.get("/api/admin/reports", { params });
    return response.data;
  }

  async getReportById(reportId: string): Promise<UserReport> {
    const response = await apiClient.get<UserReport>(
      `/api/admin/reports/${reportId}`,
    );
    return response.data;
  }

  async resolveReport(
    reportId: string,
    request: ResolveReportRequest,
  ): Promise<void> {
    await apiClient.post(`/api/admin/reports/${reportId}/resolve`, request);
  }

  // ============================================================================
  // EXPORT
  // ============================================================================

  async exportData(request: ExportRequest): Promise<Blob> {
    const response = await apiClient.get("/api/admin/analytics/export", {
      params: request,
      responseType: "blob",
    });
    return response.data;
  }
}

export const analyticsService = new AnalyticsService();
```

---

## 🎣 React Query Hooks

```typescript
// src/hooks/useAnalytics.ts
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { analyticsService } from "@/services/analyticsService";
import type { AnalyticsPeriod, ResolveReportRequest } from "@/types/analytics";

export const analyticsKeys = {
  all: ["analytics"] as const,
  vehicleStats: (id: string) => [...analyticsKeys.all, "vehicle", id] as const,
  myStats: () => [...analyticsKeys.all, "my-stats"] as const,
  overview: (period: string) =>
    [...analyticsKeys.all, "overview", period] as const,
  revenue: (from: string, to: string) =>
    [...analyticsKeys.all, "revenue", from, to] as const,
  reports: (params: any) => [...analyticsKeys.all, "reports", params] as const,
};

// ============================================================================
// LISTING STATS
// ============================================================================

export function useVehicleStats(vehicleId: string) {
  return useQuery({
    queryKey: analyticsKeys.vehicleStats(vehicleId),
    queryFn: () => analyticsService.getVehicleStats(vehicleId),
    enabled: !!vehicleId,
  });
}

export function useMyListingsStats() {
  return useQuery({
    queryKey: analyticsKeys.myStats(),
    queryFn: () => analyticsService.getMyListingsStats(),
  });
}

export function useTrackView() {
  return useMutation({
    mutationFn: (vehicleId: string) => analyticsService.trackView(vehicleId),
  });
}

// ============================================================================
// PLATFORM ANALYTICS
// ============================================================================

export function useAnalyticsOverview(period: AnalyticsPeriod = "month") {
  return useQuery({
    queryKey: analyticsKeys.overview(period),
    queryFn: () => analyticsService.getOverview(period),
    refetchInterval: 300000, // 5 minutes
  });
}

export function useRevenueAnalytics(from: string, to: string) {
  return useQuery({
    queryKey: analyticsKeys.revenue(from, to),
    queryFn: () => analyticsService.getRevenueAnalytics(from, to),
    enabled: !!from && !!to,
  });
}

// ============================================================================
// REPORTS
// ============================================================================

export function useReports(params?: { status?: string; priority?: string }) {
  return useQuery({
    queryKey: analyticsKeys.reports(params),
    queryFn: () => analyticsService.getReports(params),
  });
}

export function useResolveReport() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      reportId,
      request,
    }: {
      reportId: string;
      request: ResolveReportRequest;
    }) => analyticsService.resolveReport(reportId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: analyticsKeys.reports({}) });
    },
  });
}

// ============================================================================
// EXPORT
// ============================================================================

export function useExportData() {
  return useMutation({
    mutationFn: analyticsService.exportData,
    onSuccess: (blob, variables) => {
      // Trigger download
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `export-${variables.type}-${new Date().toISOString()}.${variables.format}`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      a.remove();
    },
  });
}
```

---

## 🧩 Componentes de Ejemplo

### VehicleStatsCard

```typescript
// src/components/analytics/VehicleStatsCard.tsx
import { useVehicleStats } from "@/hooks/useAnalytics";
import { FiEye, FiHeart, FiMessageCircle, FiPhone, FiTrendingUp, FiTrendingDown } from "react-icons/fi";

export const VehicleStatsCard = ({ vehicleId }: { vehicleId: string }) => {
  const { data: stats, isLoading } = useVehicleStats(vehicleId);

  if (isLoading) return <div>Cargando estadísticas...</div>;
  if (!stats) return null;

  const TrendIcon = ({ value }: { value: number }) =>
    value >= 0 ? (
      <span className="text-green-500 flex items-center gap-1">
        <FiTrendingUp /> +{value}%
      </span>
    ) : (
      <span className="text-red-500 flex items-center gap-1">
        <FiTrendingDown /> {value}%
      </span>
    );

  return (
    <div className="bg-white rounded-xl p-6 border">
      <h3 className="text-lg font-semibold mb-4">Estadísticas del Vehículo</h3>
      <p className="text-gray-600 text-sm mb-6">{stats.title}</p>

      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <StatBox
          icon={<FiEye className="text-blue-500" />}
          label="Vistas"
          value={stats.stats.totalViews}
          sublabel={`${stats.stats.todayViews} hoy`}
        />
        <StatBox
          icon={<FiHeart className="text-red-500" />}
          label="Favoritos"
          value={stats.stats.favoriteCount}
        />
        <StatBox
          icon={<FiMessageCircle className="text-green-500" />}
          label="Contactos"
          value={stats.stats.contactRequests}
        />
        <StatBox
          icon={<FiPhone className="text-purple-500" />}
          label="Llamadas"
          value={stats.stats.phoneClicks}
        />
      </div>

      {/* Trends */}
      <div className="mt-6 pt-4 border-t flex gap-6">
        <div>
          <span className="text-sm text-gray-500">Tendencia vistas</span>
          <div className="font-medium">
            <TrendIcon value={stats.trends.viewsTrend} />
          </div>
        </div>
        <div>
          <span className="text-sm text-gray-500">Tendencia contactos</span>
          <div className="font-medium">
            <TrendIcon value={stats.trends.contactsTrend} />
          </div>
        </div>
      </div>

      {/* Top Sources */}
      <div className="mt-6 pt-4 border-t">
        <h4 className="text-sm font-medium text-gray-700 mb-2">Fuentes de tráfico</h4>
        <div className="space-y-2">
          {stats.topSources.map((source) => (
            <div key={source.source} className="flex items-center justify-between text-sm">
              <span className="capitalize">{source.source}</span>
              <span className="font-medium">{source.count}</span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

const StatBox = ({ icon, label, value, sublabel }: any) => (
  <div className="text-center p-4 bg-gray-50 rounded-lg">
    <div className="flex justify-center mb-2">{icon}</div>
    <div className="text-2xl font-bold">{value.toLocaleString()}</div>
    <div className="text-xs text-gray-500">{label}</div>
    {sublabel && <div className="text-xs text-gray-400 mt-1">{sublabel}</div>}
  </div>
);
```

---

### RevenueChart (Admin)

```typescript
// src/components/admin/RevenueChart.tsx
import { useRevenueAnalytics } from "@/hooks/useAnalytics";
import { format, subDays } from "date-fns";

export const RevenueChart = () => {
  const from = format(subDays(new Date(), 30), "yyyy-MM-dd");
  const to = format(new Date(), "yyyy-MM-dd");
  const { data: revenue, isLoading } = useRevenueAnalytics(from, to);

  if (isLoading) return <div>Cargando...</div>;
  if (!revenue) return null;

  return (
    <div className="bg-white rounded-xl p-6 border">
      <h3 className="text-lg font-semibold mb-4">Ingresos (últimos 30 días)</h3>

      {/* Summary Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <div className="bg-green-50 p-4 rounded-lg">
          <p className="text-sm text-green-600">Ingresos Totales</p>
          <p className="text-2xl font-bold text-green-700">
            ${revenue.summary.totalRevenue.toLocaleString()}
          </p>
        </div>
        <div className="bg-blue-50 p-4 rounded-lg">
          <p className="text-sm text-blue-600">MRR</p>
          <p className="text-2xl font-bold text-blue-700">
            ${revenue.summary.mrr.toLocaleString()}
          </p>
        </div>
        <div className="bg-purple-50 p-4 rounded-lg">
          <p className="text-sm text-purple-600">ARR</p>
          <p className="text-2xl font-bold text-purple-700">
            ${revenue.summary.arr.toLocaleString()}
          </p>
        </div>
        <div className="bg-orange-50 p-4 rounded-lg">
          <p className="text-sm text-orange-600">Churn Rate</p>
          <p className="text-2xl font-bold text-orange-700">
            {revenue.summary.churnRate}%
          </p>
        </div>
      </div>

      {/* By Plan Breakdown */}
      <div className="border-t pt-4">
        <h4 className="text-sm font-medium text-gray-700 mb-3">Ingresos por Plan</h4>
        <div className="space-y-3">
          {Object.entries(revenue.byPlan).map(([plan, data]) => (
            <div key={plan} className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <span className="font-medium">{plan}</span>
                <span className="text-sm text-gray-500">({data.count} dealers)</span>
              </div>
              <span className="font-bold">${data.revenue.toLocaleString()}</span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
```

---

## 🎉 Resumen

✅ **12 Endpoints documentados**  
✅ **TypeScript Types completos** (VehicleStats, Analytics, Reports)  
✅ **Service Layer** con 12 métodos  
✅ **React Query Hooks** (8 hooks)  
✅ **2 Componentes UI** (VehicleStatsCard + RevenueChart)

---

_Última actualización: Enero 30, 2026_
