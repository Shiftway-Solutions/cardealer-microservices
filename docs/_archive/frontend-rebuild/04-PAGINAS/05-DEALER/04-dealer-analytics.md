---
title: "28. Dealer Analytics Dashboard Completo"
priority: P1
estimated_time: "2 horas"
dependencies: []
apis: ["VehiclesSaleService", "DealerManagementService"]
status: complete
last_updated: "2026-01-30"
---

# 28. Dealer Analytics Dashboard Completo

> **Ruta:** `/dealer/analytics` (Separado del dashboard principal)  
> **Scope:** Analytics avanzado con charts interactivos, funnels, benchmarks, reportes descargables  
> **Prerrequisito:** [06-dealer-dashboard.md](06-dealer-dashboard.md) para layout y navegación  
> **Diferencia:** Dashboard básico (06) = KPIs simples | Este doc = Charts + Reports + ML Insights

> **Objetivo:** Implementar sistema completo de analytics para dealers con métricas en tiempo real, dashboards interactivos, conversion funnels, benchmarks, insights con IA y reportes descargables.  
> **Tiempo estimado:** 3-4 horas  
> **Prioridad:** P1 (Crítico - Core dealer experience)  
> **Complejidad:** 🔴 Alta (Charts, Real-time data, ML insights, Export reports)  
> **Dependencias:** DealerAnalyticsService (Puerto 5041), AnalyticsService (Puerto 5070), EventTrackingService (Puerto 5050), DealerManagementService, VehiclesSaleService
> **Última actualización:** Enero 2026

---

## 📊 AUDITORÍA DE INTEGRACIONES

| Backend Service                 | Puerto | Estado Backend | Estado UI       |
| ------------------------------- | ------ | -------------- | --------------- |
| DealerAnalyticsService          | 5041   | ✅ 90%         | 🟡 65%          |
| **AnalyticsService (Platform)** | 5070   | ✅ 80%         | 🟡 60%          |
| **EventTrackingService**        | 5050   | ✅ 100%        | ⚫ Backend only |
| ReportsService                  | 5095   | ✅ 100%        | 🟡 60%          |
| DealerManagementService         | 5039   | ✅ 85%         | 🟢 70%          |

### Procesos de AnalyticsService Integrados

| Proceso  | Nombre            | Estado     | Descripción                        |
| -------- | ----------------- | ---------- | ---------------------------------- |
| ANLT-001 | Event Collection  | ✅ Backend | Ingestión de eventos via SDK       |
| ANLT-002 | Real-time Metrics | ✅ Backend | Actualización métricas en Redis    |
| ANLT-003 | Scheduled Reports | ✅ Backend | Generación de reportes automáticos |

### KPIs de Plataforma Disponibles

| Categoría      | KPIs                                                 |
| -------------- | ---------------------------------------------------- |
| **Usuarios**   | MAU, DAU, WAU, Bounce Rate, Avg Session Duration     |
| **Vehículos**  | CTR, Lead Rate, Avg Time to Sale, Inventory Turnover |
| **Conversión** | Visitors → Leads, Leads → Sales, Funnel Drop-offs    |
| **Revenue**    | MRR, ARR, ARPU, LTV, Churn Rate, GMV                 |

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura](#arquitectura)
2. [Backend API](#backend-api)
3. [Componentes de Analytics](#componentes-de-analytics)
4. [Páginas](#páginas)
5. [Hooks y Servicios](#hooks-y-servicios)
6. [Tipos TypeScript](#tipos-typescript)
7. [Validación](#validación)

---

## 🏗️ ARQUITECTURA

### System Overview

```
┌────────────────────────────────────────────────────────────────────────────┐
│                      DEALER ANALYTICS SYSTEM                                │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  📊 OVERVIEW DASHBOARD (ANAL-001)                                          │
│  /dealer/dashboard                                                         │
│  ├─ 4 KPI Cards: Vehículos activos, Vistas, Consultas, Valor inventario   │
│  ├─ Chart: Vistas últimos 30 días (Line chart)                            │
│  ├─ Chart: Consultas por vehículo (Bar chart)                             │
│  └─ Actividad reciente (Timeline)                                         │
│                                                                             │
│  📦 INVENTORY ANALYTICS (ANAL-002)                                         │
│  /dealer/analytics/inventory                                               │
│  ├─ Por categoría: Vehículos by type (Sedán, SUV, etc.)                   │
│  ├─ Por precio: Distribution chart                                        │
│  ├─ Por antigüedad: Days on market                                        │
│  ├─ Performance: Vistas/consultas por vehículo                            │
│  ├─ Slow movers: Alertas de inventario estancado (>60 días)               │
│  └─ Exportar reporte CSV                                                   │
│                                                                             │
│  🎯 CONVERSION FUNNEL (ANAL-003)                                           │
│  /dealer/analytics/funnel                                                  │
│  ├─ Stage 1: Impresiones (vistas en búsqueda)                             │
│  ├─ Stage 2: Vistas (clicks al detalle)                                   │
│  ├─ Stage 3: Consultas (mensajes/llamadas)                                │
│  ├─ Stage 4: Conversión (venta cerrada)                                   │
│  ├─ Conversion rates entre stages                                         │
│  ├─ Benchmark vs promedio de la plataforma                                │
│  └─ Filtros: Fecha, tipo vehículo, precio                                 │
│                                                                             │
│  🏆 BENCHMARKS (ANAL-004)                                                  │
│  /dealer/benchmarks                                                        │
│  ├─ Posición en el mercado (Top 10%, 25%, 50%)                            │
│  ├─ Comparación con dealers similares:                                    │
│  │   • Tiempo promedio de venta                                           │
│  │   • Precio promedio                                                    │
│  │   • Tasa de respuesta                                                  │
│  │   • Calificación promedio                                              │
│  ├─ Tendencias del mercado (precios, demanda)                             │
│  └─ Recomendaciones de mejora                                             │
│                                                                             │
│  🤖 AI INSIGHTS (ANAL-005)                                                 │
│  /dealer/analytics/insights                                                │
│  ├─ Predicciones:                                                          │
│  │   • "Este vehículo puede venderse en 7-10 días"                        │
│  │   • "Reducir precio $500 aumentaría vistas 35%"                        │
│  │   • "Mejor momento para publicar: Viernes 2-5pm"                       │
│  ├─ Análisis de texto: Reviews sentiment analysis                         │
│  ├─ Scoring de listings: Quality score (1-100)                            │
│  └─ Alertas inteligentes: Oportunidades detectadas                        │
│                                                                             │
│  🔔 ALERTAS ANALYTICS (ANAL-006)                                           │
│  /dealer/analytics/alerts                                                  │
│  ├─ Configurar alertas:                                                    │
│  │   • Vistas diarias < X                                                 │
│  │   • Inventario estancado > 60 días                                     │
│  │   • Precio fuera de rango del mercado                                  │
│  │   • Nueva competencia en tu área                                       │
│  ├─ Notificaciones: Email, SMS, Push, In-app                              │
│  └─ Historial de alertas disparadas                                       │
│                                                                             │
│  📈 ADVANCED DASHBOARD (DASH-001)                                          │
│  /dealer/analytics/dashboard                                               │
│  ├─ Grid personalizable con widgets drag-and-drop                         │
│  ├─ Widgets disponibles: 15+ tipos                                        │
│  ├─ Date range selector (Hoy, 7d, 30d, 90d, Custom)                       │
│  ├─ Exportar dashboard como PDF                                           │
│  └─ Compartir con equipo (permisos)                                       │
│                                                                             │
│  📊 KPIs AVANZADOS (DASH-002)                                              │
│  /dealer/analytics/advanced                                                │
│  ├─ Métricas financieras:                                                  │
│  │   • Revenue total, promedio por venta                                  │
│  │   • ROI por vehículo                                                   │
│  │   • Costo de adquisición de cliente (CAC)                              │
│  ├─ Métricas operativas:                                                   │
│  │   • Tiempo promedio de respuesta                                       │
│  │   • Tasa de cierre (lead → venta)                                      │
│  │   • Inventory turnover rate                                            │
│  └─ Gráficos avanzados: Cohort analysis, Heat maps                        │
│                                                                             │
│  📄 REPORTES (REPORT-001)                                                  │
│  /dealer/analytics/reports                                                 │
│  ├─ Templates pre-configurados:                                           │
│  │   • Reporte mensual ejecutivo                                          │
│  │   • Análisis de inventario                                             │
│  │   • Performance de listings                                            │
│  │   • Análisis de competencia                                            │
│  ├─ Custom reports builder                                                │
│  ├─ Scheduled reports (envío automático email)                            │
│  └─ Formatos: PDF, Excel, CSV                                             │
│                                                                             │
└────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔌 BACKEND API

### DealerAnalyticsService Endpoints (Ya Implementados ✅)

```typescript
// OVERVIEW ANALYTICS (ANAL-001)
GET    /api/dealer-analytics/overview/{dealerId}
// Response: { totalListings, activeListings, totalViews, totalInquiries,
//            totalValue, viewsLast30Days[], inquiriesLast30Days[], recentActivity[] }

GET    /api/dealer-analytics/overview/{dealerId}/kpis
// Response: { kpis: [{ name, value, change, trend }] }

GET    /api/dealer-analytics/overview/{dealerId}/charts
// Response: { viewsChart: {}, inquiriesChart: {}, performanceChart: {} }

// INVENTORY ANALYTICS (ANAL-002)
GET    /api/dealer-analytics/inventory/{dealerId}/stats
// Response: { byCategory: {}, byPriceRange: {}, byDaysOnMarket: {},
//            slowMovers: [], bestPerformers: [] }

GET    /api/dealer-analytics/inventory/{dealerId}/performance
// Query: ?sortBy=views|inquiries|daysOnMarket
// Response: { listings: [{ id, title, views, inquiries, daysOnMarket, score }] }

GET    /api/dealer-analytics/inventory/{dealerId}/slow-movers
// Query: ?threshold=60 (days)
// Response: { slowMovers: [{ listingId, title, daysOnMarket, lastView, suggestions[] }] }

POST   /api/dealer-analytics/inventory/{dealerId}/export
// Body: { format: 'csv'|'excel', filters: {} }
// Response: { downloadUrl, expiresAt }

// CONVERSION FUNNEL (ANAL-003)
GET    /api/dealer-analytics/funnel/{dealerId}
// Query: ?startDate=2026-01-01&endDate=2026-01-31&vehicleType=SUV
// Response: { stages: [{ name, count, rate, dropOff }], averageTime: {} }

GET    /api/dealer-analytics/funnel/{dealerId}/comparison
// Response: { dealer: {}, platformAverage: {}, percentile: 75 }

// BENCHMARKS (ANAL-004)
GET    /api/dealer-analytics/benchmarks/{dealerId}
// Response: { marketPosition: 'Top25', metrics: {}, competitors: [], trends: [] }

GET    /api/dealer-analytics/benchmarks/{dealerId}/competitors
// Query: ?limit=10
// Response: { competitors: [{ id, name, metrics: {} }] }

GET    /api/dealer-analytics/benchmarks/{dealerId}/recommendations
// Response: { recommendations: [{ type, priority, title, description, impact }] }

// AI INSIGHTS (ANAL-005)
GET    /api/dealer-analytics/insights/{dealerId}
// Response: { predictions: [], sentiments: [], scores: [], opportunities: [] }

GET    /api/dealer-analytics/insights/{dealerId}/predictions/{listingId}
// Response: { estimatedDaysToSell, priceOptimization, bestPublishTime, confidence }

POST   /api/dealer-analytics/insights/{dealerId}/analyze-listing
// Body: { listingId }
// Response: { qualityScore, improvements: [], predictedPerformance }

GET    /api/dealer-analytics/insights/{dealerId}/sentiment
// Response: { overallSentiment: 0.85, reviewsSummary: {}, keywords: [] }

// ALERTS (ANAL-006)
GET    /api/dealer-analytics/alerts/{dealerId}
// Response: { alerts: [{ id, type, severity, message, triggeredAt, isRead }] }

POST   /api/dealer-analytics/alerts/{dealerId}/configure
// Body: { type, threshold, notificationChannels: ['email', 'sms'] }
// Response: { alertConfigId }

PUT    /api/dealer-analytics/alerts/{dealerId}/{alertId}/read
// Mark alert as read

DELETE /api/dealer-analytics/alerts/{dealerId}/config/{configId}
// Delete alert configuration

GET    /api/dealer-analytics/alerts/{dealerId}/history
// Query: ?page=1&pageSize=20
// Response: { alerts: [], pagination: {} }

// DASHBOARD (DASH-001)
GET    /api/dealer-analytics/dashboard/{dealerId}/widgets
// Response: { widgets: [{ id, type, config, position }] }

POST   /api/dealer-analytics/dashboard/{dealerId}/widgets
// Body: { type: 'kpi-card'|'line-chart'|'bar-chart'|'table', config: {} }
// Response: { widget }

PUT    /api/dealer-analytics/dashboard/{dealerId}/widgets/{widgetId}
// Update widget config or position

DELETE /api/dealer-analytics/dashboard/{dealerId}/widgets/{widgetId}

POST   /api/dealer-analytics/dashboard/{dealerId}/export-pdf
// Body: { dateRange: {}, widgets: [] }
// Response: { pdfUrl, expiresAt }

// ADVANCED KPIS (DASH-002)
GET    /api/dealer-analytics/kpis/{dealerId}/financial
// Response: { totalRevenue, avgRevenuePerSale, roi: [], cac }

GET    /api/dealer-analytics/kpis/{dealerId}/operational
// Response: { avgResponseTime, closeRate, inventoryTurnover, leadQuality }

GET    /api/dealer-analytics/kpis/{dealerId}/cohort
// Query: ?cohortType=monthly|weekly
// Response: { cohorts: [{ cohortDate, metrics: {} }] }

// REPORTS (REPORT-001)
GET    /api/dealer-analytics/reports/templates
// Response: { templates: [{ id, name, description, format }] }

POST   /api/dealer-analytics/reports/{dealerId}/generate
// Body: { templateId?, customConfig?, format: 'pdf'|'excel'|'csv' }
// Response: { reportId, status: 'processing' }

GET    /api/dealer-analytics/reports/{dealerId}/{reportId}
// Response: { report: { id, status, downloadUrl?, generatedAt } }

POST   /api/dealer-analytics/reports/{dealerId}/schedule
// Body: { templateId, frequency: 'daily'|'weekly'|'monthly', recipients: [] }
// Response: { scheduleId }

GET    /api/dealer-analytics/reports/{dealerId}/scheduled
// Response: { schedules: [{ id, template, frequency, nextRun }] }

DELETE /api/dealer-analytics/reports/{dealerId}/schedule/{scheduleId}
```

---

## 🎨 COMPONENTES DE ANALYTICS

### PASO 1: AnalyticsKPICard - Tarjeta de Métrica

```typescript
// filepath: src/components/dealer/analytics/AnalyticsKPICard.tsx
"use client";

import { LucideIcon, TrendingUp, TrendingDown } from "lucide-react";
import { Card } from "@/components/ui/Card";
import { cn } from "@/lib/utils";

interface AnalyticsKPICardProps {
  title: string;
  value: string | number;
  change?: number; // Porcentaje de cambio
  trend?: "up" | "down" | "neutral";
  icon: LucideIcon;
  description?: string;
  loading?: boolean;
}

export function AnalyticsKPICard({
  title,
  value,
  change,
  trend = "neutral",
  icon: Icon,
  description,
  loading,
}: AnalyticsKPICardProps) {
  const trendColors = {
    up: "text-green-600 bg-green-50",
    down: "text-red-600 bg-red-50",
    neutral: "text-gray-600 bg-gray-50",
  };

  const TrendIcon = trend === "up" ? TrendingUp : TrendingDown;

  if (loading) {
    return (
      <Card className="p-6 animate-pulse">
        <div className="h-4 bg-gray-200 rounded w-24 mb-4" />
        <div className="h-8 bg-gray-200 rounded w-32" />
      </Card>
    );
  }

  return (
    <Card className="p-6 hover:shadow-md transition-shadow">
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <p className="text-sm font-medium text-gray-600 mb-1">{title}</p>
          <p className="text-3xl font-bold text-gray-900 mb-2">{value}</p>

          {change !== undefined && (
            <div className="flex items-center gap-1">
              <span
                className={cn(
                  "inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium",
                  trendColors[trend]
                )}
              >
                <TrendIcon size={12} />
                {Math.abs(change)}%
              </span>
              {description && (
                <span className="text-xs text-gray-500 ml-2">{description}</span>
              )}
            </div>
          )}
        </div>

        <div className="p-3 bg-primary-50 rounded-lg">
          <Icon size={24} className="text-primary-600" />
        </div>
      </div>
    </Card>
  );
}
```

---

### PASO 2: AnalyticsLineChart - Gráfico de Líneas

```typescript
// filepath: src/components/dealer/analytics/AnalyticsLineChart.tsx
"use client";

import { Card } from "@/components/ui/Card";
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from "recharts";

interface DataPoint {
  date: string;
  [key: string]: string | number;
}

interface AnalyticsLineChartProps {
  title: string;
  data: DataPoint[];
  lines: Array<{
    dataKey: string;
    stroke: string;
    name: string;
  }>;
  height?: number;
}

export function AnalyticsLineChart({
  title,
  data,
  lines,
  height = 300,
}: AnalyticsLineChartProps) {
  return (
    <Card className="p-6">
      <h3 className="text-lg font-semibold text-gray-900 mb-4">{title}</h3>

      <ResponsiveContainer width="100%" height={height}>
        <LineChart data={data}>
          <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
          <XAxis
            dataKey="date"
            tick={{ fontSize: 12 }}
            stroke="#9ca3af"
          />
          <YAxis tick={{ fontSize: 12 }} stroke="#9ca3af" />
          <Tooltip
            contentStyle={{
              backgroundColor: "#fff",
              border: "1px solid #e5e7eb",
              borderRadius: "8px",
            }}
          />
          <Legend />
          {lines.map((line) => (
            <Line
              key={line.dataKey}
              type="monotone"
              dataKey={line.dataKey}
              stroke={line.stroke}
              name={line.name}
              strokeWidth={2}
              dot={{ r: 4 }}
              activeDot={{ r: 6 }}
            />
          ))}
        </LineChart>
      </ResponsiveContainer>
    </Card>
  );
}
```

---

### PASO 3: ConversionFunnel - Embudo de Conversión

```typescript
// filepath: src/components/dealer/analytics/ConversionFunnel.tsx
"use client";

import { Card } from "@/components/ui/Card";
import { Badge } from "@/components/ui/Badge";
import { ChevronRight } from "lucide-react";
import { cn } from "@/lib/utils";

interface FunnelStage {
  name: string;
  count: number;
  rate: number;
  dropOff?: number;
}

interface ConversionFunnelProps {
  stages: FunnelStage[];
  benchmark?: {
    dealer: number;
    platform: number;
  };
}

export function ConversionFunnel({ stages, benchmark }: ConversionFunnelProps) {
  const maxCount = stages[0]?.count || 1;

  return (
    <Card className="p-6">
      <div className="flex items-center justify-between mb-6">
        <h3 className="text-lg font-semibold text-gray-900">
          Embudo de Conversión
        </h3>
        {benchmark && (
          <div className="text-sm">
            <span className="text-gray-600">Tu tasa: </span>
            <span className="font-semibold text-gray-900">
              {benchmark.dealer.toFixed(1)}%
            </span>
            <span className="text-gray-400 mx-2">vs</span>
            <span className="text-gray-600">Promedio: </span>
            <span className="font-medium text-gray-700">
              {benchmark.platform.toFixed(1)}%
            </span>
          </div>
        )}
      </div>

      <div className="space-y-4">
        {stages.map((stage, index) => {
          const widthPercentage = (stage.count / maxCount) * 100;
          const isLast = index === stages.length - 1;

          return (
            <div key={stage.name}>
              <div className="flex items-center justify-between mb-2">
                <div className="flex items-center gap-2">
                  <span className="font-medium text-gray-900">{stage.name}</span>
                  <Badge variant="secondary">{stage.count.toLocaleString()}</Badge>
                </div>
                <span className="text-sm font-medium text-gray-700">
                  {stage.rate.toFixed(1)}%
                </span>
              </div>

              <div className="relative h-12 bg-gray-100 rounded-lg overflow-hidden">
                <div
                  className={cn(
                    "h-full rounded-lg transition-all duration-500",
                    index === 0 && "bg-blue-500",
                    index === 1 && "bg-green-500",
                    index === 2 && "bg-yellow-500",
                    index === 3 && "bg-purple-500"
                  )}
                  style={{ width: `${widthPercentage}%` }}
                />
                <span className="absolute inset-0 flex items-center px-4 text-sm font-medium text-gray-900">
                  {stage.count.toLocaleString()} usuarios
                </span>
              </div>

              {!isLast && stage.dropOff !== undefined && (
                <div className="flex items-center gap-2 mt-2 text-sm text-red-600">
                  <ChevronRight size={16} />
                  <span>
                    {stage.dropOff.toFixed(1)}% abandonan en esta etapa
                  </span>
                </div>
              )}
            </div>
          );
        })}
      </div>
    </Card>
  );
}
```

---

### PASO 4: BenchmarkComparison - Comparación con Competencia

```typescript
// filepath: src/components/dealer/analytics/BenchmarkComparison.tsx
"use client";

import { Card } from "@/components/ui/Card";
import { Badge } from "@/components/ui/Badge";
import { TrendingUp, TrendingDown, Minus } from "lucide-react";
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from "recharts";

interface BenchmarkMetric {
  name: string;
  yourValue: number;
  avgValue: number;
  unit: string;
  higherIsBetter: boolean;
}

interface BenchmarkComparisonProps {
  position: string; // "Top10" | "Top25" | "Top50"
  metrics: BenchmarkMetric[];
}

export function BenchmarkComparison({
  position,
  metrics,
}: BenchmarkComparisonProps) {
  const positionColors = {
    Top10: "bg-yellow-500",
    Top25: "bg-green-500",
    Top50: "bg-blue-500",
    Bottom50: "bg-red-500",
  };

  const positionLabels = {
    Top10: "Top 10%",
    Top25: "Top 25%",
    Top50: "Top 50%",
    Bottom50: "Bottom 50%",
  };

  const chartData = metrics.map((metric) => ({
    name: metric.name,
    Tu: metric.yourValue,
    Promedio: metric.avgValue,
  }));

  const getPerformanceIcon = (metric: BenchmarkMetric) => {
    const diff = metric.yourValue - metric.avgValue;
    const isBetter = metric.higherIsBetter ? diff > 0 : diff < 0;

    if (Math.abs(diff) < 0.1) return <Minus size={16} className="text-gray-400" />;
    if (isBetter) return <TrendingUp size={16} className="text-green-600" />;
    return <TrendingDown size={16} className="text-red-600" />;
  };

  return (
    <Card className="p-6">
      <div className="flex items-center justify-between mb-6">
        <h3 className="text-lg font-semibold text-gray-900">
          Benchmarks del Mercado
        </h3>
        <Badge className={positionColors[position as keyof typeof positionColors]}>
          {positionLabels[position as keyof typeof positionLabels]}
        </Badge>
      </div>

      {/* Metrics table */}
      <div className="space-y-3 mb-6">
        {metrics.map((metric) => {
          const diff = metric.yourValue - metric.avgValue;
          const diffPercent = ((diff / metric.avgValue) * 100).toFixed(1);

          return (
            <div
              key={metric.name}
              className="flex items-center justify-between p-3 bg-gray-50 rounded-lg"
            >
              <div className="flex items-center gap-2">
                {getPerformanceIcon(metric)}
                <span className="font-medium text-gray-900">{metric.name}</span>
              </div>
              <div className="flex items-center gap-4">
                <div className="text-right">
                  <p className="text-sm text-gray-600">Tu</p>
                  <p className="font-semibold text-gray-900">
                    {metric.yourValue} {metric.unit}
                  </p>
                </div>
                <div className="text-right">
                  <p className="text-sm text-gray-600">Promedio</p>
                  <p className="font-medium text-gray-700">
                    {metric.avgValue} {metric.unit}
                  </p>
                </div>
                <div className="text-right min-w-[60px]">
                  <p className="text-xs text-gray-500">Diferencia</p>
                  <p
                    className={`font-semibold ${
                      parseFloat(diffPercent) > 0
                        ? "text-green-600"
                        : parseFloat(diffPercent) < 0
                        ? "text-red-600"
                        : "text-gray-600"
                    }`}
                  >
                    {diffPercent > 0 ? "+" : ""}
                    {diffPercent}%
                  </p>
                </div>
              </div>
            </div>
          );
        })}
      </div>

      {/* Comparison chart */}
      <ResponsiveContainer width="100%" height={300}>
        <BarChart data={chartData}>
          <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
          <XAxis dataKey="name" tick={{ fontSize: 12 }} />
          <YAxis tick={{ fontSize: 12 }} />
          <Tooltip />
          <Legend />
          <Bar dataKey="Tu" fill="#3b82f6" />
          <Bar dataKey="Promedio" fill="#94a3b8" />
        </BarChart>
      </ResponsiveContainer>
    </Card>
  );
}
```

---

### PASO 5: AIInsightsCard - Insights con IA

```typescript
// filepath: src/components/dealer/analytics/AIInsightsCard.tsx
"use client";

import { Card } from "@/components/ui/Card";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { Sparkles, TrendingUp, Clock, DollarSign, ArrowRight } from "lucide-react";

interface Insight {
  type: "prediction" | "optimization" | "opportunity" | "warning";
  title: string;
  description: string;
  impact: "high" | "medium" | "low";
  confidence: number; // 0-1
  actionable?: boolean;
  actionLabel?: string;
  onAction?: () => void;
}

interface AIInsightsCardProps {
  insights: Insight[];
}

export function AIInsightsCard({ insights }: AIInsightsCardProps) {
  const typeIcons = {
    prediction: Clock,
    optimization: TrendingUp,
    opportunity: Sparkles,
    warning: DollarSign,
  };

  const typeColors = {
    prediction: "bg-blue-50 text-blue-700 border-blue-200",
    optimization: "bg-green-50 text-green-700 border-green-200",
    opportunity: "bg-purple-50 text-purple-700 border-purple-200",
    warning: "bg-yellow-50 text-yellow-700 border-yellow-200",
  };

  const impactColors = {
    high: "bg-red-100 text-red-800",
    medium: "bg-yellow-100 text-yellow-800",
    low: "bg-green-100 text-green-800",
  };

  return (
    <Card className="p-6">
      <div className="flex items-center gap-2 mb-6">
        <Sparkles size={20} className="text-purple-600" />
        <h3 className="text-lg font-semibold text-gray-900">
          Insights con IA
        </h3>
        <Badge variant="secondary" className="ml-auto">
          {insights.length} insights
        </Badge>
      </div>

      <div className="space-y-4">
        {insights.map((insight, index) => {
          const Icon = typeIcons[insight.type];

          return (
            <div
              key={index}
              className={`p-4 rounded-lg border-2 ${typeColors[insight.type]}`}
            >
              <div className="flex items-start gap-3">
                <Icon size={20} className="mt-0.5 flex-shrink-0" />

                <div className="flex-1 min-w-0">
                  <div className="flex items-start justify-between gap-2 mb-2">
                    <h4 className="font-semibold text-gray-900">
                      {insight.title}
                    </h4>
                    <div className="flex items-center gap-2 flex-shrink-0">
                      <Badge
                        variant="secondary"
                        className={impactColors[insight.impact]}
                      >
                        {insight.impact === "high"
                          ? "Alto impacto"
                          : insight.impact === "medium"
                          ? "Impacto medio"
                          : "Bajo impacto"}
                      </Badge>
                      <Badge variant="outline">
                        {Math.round(insight.confidence * 100)}% confianza
                      </Badge>
                    </div>
                  </div>

                  <p className="text-sm text-gray-700 mb-3">
                    {insight.description}
                  </p>

                  {insight.actionable && insight.actionLabel && (
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={insight.onAction}
                      className="gap-2"
                    >
                      {insight.actionLabel}
                      <ArrowRight size={14} />
                    </Button>
                  )}
                </div>
              </div>
            </div>
          );
        })}
      </div>
    </Card>
  );
}
```

---

## 📄 PÁGINAS

### PASO 6: Inventory Analytics Page

```typescript
// filepath: src/app/(dealer)/dealer/analytics/inventory/page.tsx
import { Metadata } from "next";
import { redirect } from "next/navigation";
import { Package, TrendingDown, Clock, Download } from "lucide-react";
import { auth } from "@/lib/auth";
import { dealerService } from "@/lib/services/dealerService";
import { AnalyticsKPICard } from "@/components/dealer/analytics/AnalyticsKPICard";
import { InventoryDistributionChart } from "@/components/dealer/analytics/InventoryDistributionChart";
import { SlowMoversTable } from "@/components/dealer/analytics/SlowMoversTable";
import { PerformanceTable } from "@/components/dealer/analytics/PerformanceTable";
import { Button } from "@/components/ui/Button";

export const metadata: Metadata = {
  title: "Análisis de Inventario | OKLA Dealer",
};

export default async function InventoryAnalyticsPage() {
  const session = await auth();

  if (!session?.user) {
    redirect("/login?callbackUrl=/dealer/analytics/inventory");
  }

  const dealer = await dealerService.getByUserId(session.user.id);

  if (!dealer) {
    redirect("/dealer/registro");
  }

  // Fetch analytics data (server-side)
  const stats = await fetch(
    `${process.env.API_URL}/api/dealer-analytics/inventory/${dealer.id}/stats`,
    {
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
      },
      next: { revalidate: 300 }, // 5 min cache
    }
  ).then((res) => res.json());

  return (
    <div className="max-w-7xl mx-auto px-4 py-8">
      {/* Header */}
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">
            Análisis de Inventario
          </h1>
          <p className="text-gray-600 mt-1">
            Monitorea el performance y distribución de tu inventario
          </p>
        </div>
        <Button variant="outline" className="gap-2">
          <Download size={16} />
          Exportar Reporte
        </Button>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
        <AnalyticsKPICard
          title="Inventario Total"
          value={stats.totalVehicles}
          icon={Package}
          description={`${stats.activeVehicles} activos`}
        />
        <AnalyticsKPICard
          title="Días Promedio en Inventario"
          value={stats.avgDaysOnMarket}
          change={-12}
          trend="up"
          icon={Clock}
          description="vs mes anterior"
        />
        <AnalyticsKPICard
          title="Inventario Estancado"
          value={stats.slowMoversCount}
          change={5}
          trend="down"
          icon={TrendingDown}
          description="> 60 días"
        />
      </div>

      {/* Distribution Charts */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-8">
        <InventoryDistributionChart
          title="Por Categoría"
          data={stats.byCategory}
          type="pie"
        />
        <InventoryDistributionChart
          title="Por Rango de Precio"
          data={stats.byPriceRange}
          type="bar"
        />
      </div>

      {/* Slow Movers Alert */}
      {stats.slowMoversCount > 0 && (
        <div className="mb-8">
          <SlowMoversTable slowMovers={stats.slowMovers} />
        </div>
      )}

      {/* Performance Table */}
      <PerformanceTable dealerId={dealer.id} />
    </div>
  );
}
```

---

### PASO 7: Lead Funnel Page

```typescript
// filepath: src/app/(dealer)/dealer/analytics/funnel/page.tsx
import { Metadata } from "next";
import { redirect } from "next/navigation";
import { Filter } from "lucide-react";
import { auth } from "@/lib/auth";
import { dealerService } from "@/lib/services/dealerService";
import { ConversionFunnel } from "@/components/dealer/analytics/ConversionFunnel";
import { FunnelFilters } from "@/components/dealer/analytics/FunnelFilters";

export const metadata: Metadata = {
  title: "Embudo de Conversión | OKLA Dealer",
};

interface PageProps {
  searchParams: Promise<{
    startDate?: string;
    endDate?: string;
    vehicleType?: string;
  }>;
}

export default async function LeadFunnelPage({ searchParams }: PageProps) {
  const session = await auth();
  const params = await searchParams;

  if (!session?.user) {
    redirect("/login?callbackUrl=/dealer/analytics/funnel");
  }

  const dealer = await dealerService.getByUserId(session.user.id);

  if (!dealer) {
    redirect("/dealer/registro");
  }

  // Build query params
  const queryParams = new URLSearchParams({
    startDate: params.startDate || "",
    endDate: params.endDate || "",
    vehicleType: params.vehicleType || "",
  }).toString();

  // Fetch funnel data
  const funnelData = await fetch(
    `${process.env.API_URL}/api/dealer-analytics/funnel/${dealer.id}?${queryParams}`,
    {
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
      },
      next: { revalidate: 300 },
    }
  ).then((res) => res.json());

  // Fetch benchmark comparison
  const comparison = await fetch(
    `${process.env.API_URL}/api/dealer-analytics/funnel/${dealer.id}/comparison`,
    {
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
      },
      next: { revalidate: 3600 }, // 1 hour cache
    }
  ).then((res) => res.json());

  return (
    <div className="max-w-7xl mx-auto px-4 py-8">
      {/* Header */}
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">
            Embudo de Conversión
          </h1>
          <p className="text-gray-600 mt-1">
            Analiza cómo los usuarios interactúan con tus listados
          </p>
        </div>
        <FunnelFilters />
      </div>

      {/* Funnel visualization */}
      <div className="mb-8">
        <ConversionFunnel
          stages={funnelData.stages}
          benchmark={{
            dealer: funnelData.conversionRate,
            platform: comparison.platformAverage,
          }}
        />
      </div>

      {/* Insights */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-6">
          <h3 className="font-semibold text-blue-900 mb-2">
            📊 Análisis
          </h3>
          <ul className="space-y-2 text-sm text-blue-800">
            <li>
              • Tu tasa de conversión ({funnelData.conversionRate.toFixed(1)}%)
              está{" "}
              {funnelData.conversionRate > comparison.platformAverage
                ? "por encima"
                : "por debajo"}{" "}
              del promedio ({comparison.platformAverage.toFixed(1)}%)
            </li>
            <li>
              • La mayor pérdida ocurre entre{" "}
              <strong>{funnelData.biggestDropStage}</strong>
            </li>
            <li>
              • Tiempo promedio de conversión:{" "}
              <strong>{funnelData.avgTimeToConversion} días</strong>
            </li>
          </ul>
        </div>

        <div className="bg-green-50 border border-green-200 rounded-lg p-6">
          <h3 className="font-semibold text-green-900 mb-2">
            💡 Recomendaciones
          </h3>
          <ul className="space-y-2 text-sm text-green-800">
            <li>
              • Responder consultas en <strong>menos de 2 horas</strong>{" "}
              aumenta conversión en 35%
            </li>
            <li>
              • Agregar <strong>más fotos</strong> a tus listados reduce
              abandono en vista detalle
            </li>
            <li>
              • Ofrecer <strong>test drive</strong> mejora cierre de ventas
            </li>
          </ul>
        </div>
      </div>
    </div>
  );
}
```

---

### PASO 8: Benchmarks Page

```typescript
// filepath: src/app/(dealer)/dealer/benchmarks/page.tsx
import { Metadata } from "next";
import { redirect } from "next/navigation";
import { Trophy, TrendingUp } from "lucide-react";
import { auth } from "@/lib/auth";
import { dealerService } from "@/lib/services/dealerService";
import { BenchmarkComparison } from "@/components/dealer/analytics/BenchmarkComparison";
import { MarketTrendsChart } from "@/components/dealer/analytics/MarketTrendsChart";
import { RecommendationsPanel } from "@/components/dealer/analytics/RecommendationsPanel";

export const metadata: Metadata = {
  title: "Benchmarks del Mercado | OKLA Dealer",
};

export default async function BenchmarksPage() {
  const session = await auth();

  if (!session?.user) {
    redirect("/login?callbackUrl=/dealer/benchmarks");
  }

  const dealer = await dealerService.getByUserId(session.user.id);

  if (!dealer) {
    redirect("/dealer/registro");
  }

  // Fetch benchmarks
  const benchmarks = await fetch(
    `${process.env.API_URL}/api/dealer-analytics/benchmarks/${dealer.id}`,
    {
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
      },
      next: { revalidate: 3600 }, // 1 hour cache
    }
  ).then((res) => res.json());

  // Fetch recommendations
  const recommendations = await fetch(
    `${process.env.API_URL}/api/dealer-analytics/benchmarks/${dealer.id}/recommendations`,
    {
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
      },
      next: { revalidate: 3600 },
    }
  ).then((res) => res.json());

  return (
    <div className="max-w-7xl mx-auto px-4 py-8">
      {/* Header */}
      <div className="mb-8">
        <div className="flex items-center gap-3 mb-2">
          <Trophy size={28} className="text-yellow-500" />
          <h1 className="text-2xl font-bold text-gray-900">
            Benchmarks del Mercado
          </h1>
        </div>
        <p className="text-gray-600">
          Compara tu desempeño con otros dealers y el mercado en general
        </p>
      </div>

      {/* Benchmark comparison */}
      <div className="mb-8">
        <BenchmarkComparison
          position={benchmarks.marketPosition}
          metrics={benchmarks.metrics}
        />
      </div>

      {/* Market trends */}
      <div className="mb-8">
        <MarketTrendsChart trends={benchmarks.trends} />
      </div>

      {/* Recommendations */}
      <RecommendationsPanel recommendations={recommendations.recommendations} />
    </div>
  );
}
```

---

## 🪝 HOOKS Y SERVICIOS

### PASO 9: Analytics Hooks

```typescript
// filepath: src/lib/hooks/useDealerAnalytics.ts
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { dealerAnalyticsService } from "@/lib/services/dealerAnalyticsService";
import { toast } from "sonner";

export function useAnalyticsOverview(dealerId: string) {
  return useQuery({
    queryKey: ["dealerAnalytics", "overview", dealerId],
    queryFn: () => dealerAnalyticsService.getOverview(dealerId),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

export function useInventoryStats(dealerId: string) {
  return useQuery({
    queryKey: ["dealerAnalytics", "inventory", dealerId],
    queryFn: () => dealerAnalyticsService.getInventoryStats(dealerId),
    staleTime: 5 * 60 * 1000,
  });
}

export function useFunnelData(
  dealerId: string,
  filters?: {
    startDate?: string;
    endDate?: string;
    vehicleType?: string;
  },
) {
  return useQuery({
    queryKey: ["dealerAnalytics", "funnel", dealerId, filters],
    queryFn: () => dealerAnalyticsService.getFunnel(dealerId, filters),
    staleTime: 5 * 60 * 1000,
  });
}

export function useBenchmarks(dealerId: string) {
  return useQuery({
    queryKey: ["dealerAnalytics", "benchmarks", dealerId],
    queryFn: () => dealerAnalyticsService.getBenchmarks(dealerId),
    staleTime: 60 * 60 * 1000, // 1 hour
  });
}

export function useAIInsights(dealerId: string) {
  return useQuery({
    queryKey: ["dealerAnalytics", "insights", dealerId],
    queryFn: () => dealerAnalyticsService.getInsights(dealerId),
    staleTime: 10 * 60 * 1000, // 10 minutes
  });
}

export function useAnalyticsAlerts(dealerId: string) {
  return useQuery({
    queryKey: ["dealerAnalytics", "alerts", dealerId],
    queryFn: () => dealerAnalyticsService.getAlerts(dealerId),
    refetchInterval: 60 * 1000, // Refetch every minute
  });
}

export function useConfigureAlert(dealerId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (config: any) =>
      dealerAnalyticsService.configureAlert(dealerId, config),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ["dealerAnalytics", "alerts", dealerId],
      });
      toast.success("Alerta configurada");
    },
  });
}

export function useExportReport(dealerId: string) {
  return useMutation({
    mutationFn: (config: { templateId?: string; format: string }) =>
      dealerAnalyticsService.generateReport(dealerId, config),
    onSuccess: (data) => {
      toast.success("Reporte generado");
      // Auto-download
      if (data.downloadUrl) {
        window.open(data.downloadUrl, "_blank");
      }
    },
  });
}
```

---

## 📦 TIPOS TYPESCRIPT

### PASO 10: Analytics Types

```typescript
// filepath: src/types/dealer-analytics.ts
export interface AnalyticsOverview {
  totalListings: number;
  activeListings: number;
  totalViews: number;
  totalInquiries: number;
  totalValue: number;
  viewsLast30Days: Array<{ date: string; views: number }>;
  inquiriesLast30Days: Array<{ date: string; inquiries: number }>;
  recentActivity: Array<{
    type: "view" | "inquiry" | "sale";
    listingTitle: string;
    timestamp: string;
  }>;
}

export interface InventoryStats {
  totalVehicles: number;
  activeVehicles: number;
  avgDaysOnMarket: number;
  slowMoversCount: number;
  byCategory: Record<string, number>;
  byPriceRange: Array<{ range: string; count: number }>;
  slowMovers: Array<{
    listingId: string;
    title: string;
    daysOnMarket: number;
    lastView: string;
    suggestions: string[];
  }>;
}

export interface FunnelStage {
  name: string;
  count: number;
  rate: number;
  dropOff?: number;
}

export interface FunnelData {
  stages: FunnelStage[];
  conversionRate: number;
  biggestDropStage: string;
  avgTimeToConversion: number;
}

export interface BenchmarkMetric {
  name: string;
  yourValue: number;
  avgValue: number;
  unit: string;
  higherIsBetter: boolean;
}

export interface Benchmarks {
  marketPosition: "Top10" | "Top25" | "Top50" | "Bottom50";
  metrics: BenchmarkMetric[];
  trends: Array<{
    category: string;
    data: Array<{ date: string; value: number }>;
  }>;
}

export interface AIInsight {
  type: "prediction" | "optimization" | "opportunity" | "warning";
  title: string;
  description: string;
  impact: "high" | "medium" | "low";
  confidence: number;
  actionable?: boolean;
  actionLabel?: string;
  metadata?: Record<string, any>;
}

export interface AnalyticsAlert {
  id: string;
  type: "low_views" | "stagnant_inventory" | "pricing" | "competition";
  severity: "high" | "medium" | "low";
  message: string;
  triggeredAt: string;
  isRead: boolean;
  metadata?: Record<string, any>;
}
```

---

## ✅ VALIDACIÓN

```bash
pnpm dev

# Verificar Overview Dashboard:
# - /dealer/dashboard muestra 4 KPI cards con datos reales
# - Charts de vistas/consultas renderizan correctamente
# - Actividad reciente se actualiza

# Verificar Inventory Analytics:
# - /dealer/analytics/inventory muestra distribución por categoría
# - Slow movers table muestra vehículos >60 días
# - Exportar CSV funciona

# Verificar Conversion Funnel:
# - /dealer/analytics/funnel muestra 4 stages
# - Benchmark comparison visible
# - Filtros de fecha funcionan

# Verificar Benchmarks:
# - /dealer/benchmarks muestra posición en mercado
# - Comparación con competidores funciona
# - Recomendaciones se generan

# Verificar AI Insights:
# - Insights con IA se muestran correctamente
# - Confidence scores visibles
# - Acciones sugeridas funcionan

# Verificar Alertas:
# - Alertas en tiempo real funcionan
# - Configurar nueva alerta funciona
# - Historial de alertas accesible

# Verificar Reportes:
# - Templates de reportes listados
# - Generar reporte PDF funciona
# - Scheduled reports configurables
```

---

## � PASO 15: AI Insights Panel (P2 - Inteligencia Predictiva) 🤖

```typescript
// filepath: src/components/dealer/analytics/AIInsightsPanel.tsx
"use client";

import * as React from "react";
import { motion } from "framer-motion";
import {
  Sparkles,
  TrendingUp,
  TrendingDown,
  Clock,
  DollarSign,
  Calendar,
  Target,
  AlertCircle,
  Info,
  Zap,
  Lightbulb,
} from "lucide-react";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { Progress } from "@/components/ui/Progress";
import { useAIInsights } from "@/lib/hooks/useDealerAnalytics";
import { formatPrice, formatDate, cn } from "@/lib/utils";

interface AIInsightsPanelProps {
  dealerId: string;
}

export function AIInsightsPanel({ dealerId }: AIInsightsPanelProps) {
  const { data: insights, isLoading } = useAIInsights(dealerId);

  if (isLoading || !insights) {
    return (
      <div className="space-y-4">
        {[1, 2, 3].map((i) => (
          <div key={i} className="bg-white rounded-xl p-6 animate-pulse">
            <div className="h-6 bg-gray-200 rounded w-3/4 mb-4" />
            <div className="h-4 bg-gray-200 rounded w-full mb-2" />
            <div className="h-4 bg-gray-200 rounded w-5/6" />
          </div>
        ))}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-3">
        <Sparkles className="h-6 w-6 text-primary-600" />
        <div>
          <h2 className="text-2xl font-bold text-gray-900">Insights con IA</h2>
          <p className="text-sm text-gray-600">Predicciones y recomendaciones personalizadas</p>
        </div>
      </div>

      {/* Predictions */}
      {insights.predictions && insights.predictions.length > 0 && (
        <div className="space-y-4">
          <h3 className="font-semibold text-gray-900 flex items-center gap-2">
            <Target className="h-5 w-5 text-primary-600" />
            Predicciones de Venta
          </h3>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
            {insights.predictions.map((prediction) => (
              <motion.div
                key={prediction.listingId}
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                className="bg-gradient-to-br from-blue-50 to-indigo-50 border border-blue-200 rounded-xl p-5"
              >
                <div className="flex items-start justify-between mb-3">
                  <div>
                    <h4 className="font-semibold text-gray-900">
                      {prediction.vehicleTitle}
                    </h4>
                    <p className="text-sm text-gray-600 mt-1">
                      {prediction.make} {prediction.model} {prediction.year}
                    </p>
                  </div>
                  <Badge
                    variant={prediction.confidence >= 80 ? "success" : "default"}
                    className="ml-2"
                  >
                    {prediction.confidence}% confianza
                  </Badge>
                </div>

                {/* Days to sell */}
                <div className="flex items-center gap-2 mb-3">
                  <Clock className="h-5 w-5 text-blue-600" />
                  <div>
                    <p className="text-sm text-gray-600">Estimado de venta</p>
                    <p className="font-semibold text-gray-900">
                      {prediction.daysToSellMin}-{prediction.daysToSellMax} días
                    </p>
                  </div>
                </div>

                {/* Price optimization */}
                {prediction.priceOptimization && (
                  <div className="bg-white rounded-lg p-3 mb-3">
                    <div className="flex items-center gap-2 mb-2">
                      <DollarSign className="h-4 w-4 text-green-600" />
                      <p className="text-sm font-medium text-gray-900">Optimización de Precio</p>
                    </div>
                    <p className="text-xs text-gray-700">
                      {prediction.priceOptimization.action === "decrease" && (
                        <>
                          <TrendingDown className="inline h-3 w-3 text-red-500" />
                          {" "}Reducir precio a{" "}
                          <span className="font-semibold">
                            {formatPrice(prediction.priceOptimization.suggestedPrice)}
                          </span>
                          {" "}podría aumentar vistas{" "}
                          <span className="font-semibold text-green-600">
                            +{prediction.priceOptimization.expectedImpact}%
                          </span>
                        </>
                      )}
                      {prediction.priceOptimization.action === "maintain" && (
                        <>
                          ✓ Precio actual es óptimo ({formatPrice(prediction.priceOptimization.suggestedPrice)})
                        </>
                      )}
                      {prediction.priceOptimization.action === "increase" && (
                        <>
                          <TrendingUp className="inline h-3 w-3 text-green-500" />
                          {" "}Puedes aumentar precio a{" "}
                          <span className="font-semibold">
                            {formatPrice(prediction.priceOptimization.suggestedPrice)}
                          </span>
                          {" "}sin afectar demanda
                        </>
                      )}
                    </p>
                  </div>
                )}

                {/* Best publish time */}
                {prediction.bestPublishTime && (
                  <div className="bg-white rounded-lg p-3">
                    <div className="flex items-center gap-2 mb-1">
                      <Calendar className="h-4 w-4 text-purple-600" />
                      <p className="text-sm font-medium text-gray-900">Mejor Momento</p>
                    </div>
                    <p className="text-xs text-gray-700">
                      {prediction.bestPublishTime.dayOfWeek} entre{" "}
                      {prediction.bestPublishTime.hourStart}:00 -{" "}
                      {prediction.bestPublishTime.hourEnd}:00
                    </p>
                    <p className="text-xs text-purple-600 mt-1">
                      +{prediction.bestPublishTime.expectedEngagement}% de engagement
                    </p>
                  </div>
                )}

                {/* CTA */}
                <Button
                  size="sm"
                  variant="outline"
                  className="w-full mt-3"
                  onClick={() => window.open(`/dealer/inventory/${prediction.listingId}/edit`, "_blank")}
                >
                  Aplicar Recomendaciones
                </Button>
              </motion.div>
            ))}
          </div>
        </div>
      )}

      {/* Opportunities */}
      {insights.opportunities && insights.opportunities.length > 0 && (
        <div className="space-y-4">
          <h3 className="font-semibold text-gray-900 flex items-center gap-2">
            <Lightbulb className="h-5 w-5 text-yellow-600" />
            Oportunidades Detectadas
          </h3>

          <div className="grid grid-cols-1 gap-4">
            {insights.opportunities.map((opportunity, i) => (
              <motion.div
                key={i}
                initial={{ opacity: 0, x: -10 }}
                animate={{ opacity: 1, x: 0 }}
                transition={{ delay: i * 0.1 }}
                className={cn(
                  "rounded-xl p-4 border-l-4",
                  opportunity.priority === "high" && "bg-red-50 border-red-500",
                  opportunity.priority === "medium" && "bg-yellow-50 border-yellow-500",
                  opportunity.priority === "low" && "bg-blue-50 border-blue-500"
                )}
              >
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-2">
                      {opportunity.type === "pricing" && <DollarSign className="h-5 w-5" />}
                      {opportunity.type === "marketing" && <Zap className="h-5 w-5" />}
                      {opportunity.type === "inventory" && <Target className="h-5 w-5" />}
                      <h4 className="font-semibold text-gray-900">{opportunity.title}</h4>
                    </div>
                    <p className="text-sm text-gray-700 mb-3">
                      {opportunity.description}
                    </p>
                    {opportunity.impact && (
                      <div className="flex items-center gap-4 text-xs">
                        <div>
                          <span className="text-gray-600">Impacto estimado:</span>
                          <span className="font-semibold text-green-600 ml-1">
                            +{opportunity.impact.metric} {opportunity.impact.unit}
                          </span>
                        </div>
                        <div>
                          <span className="text-gray-600">Esfuerzo:</span>
                          <span className="font-medium ml-1">{opportunity.effort}</span>
                        </div>
                      </div>
                    )}
                  </div>
                  <Badge
                    variant={
                      opportunity.priority === "high"
                        ? "destructive"
                        : opportunity.priority === "medium"
                        ? "default"
                        : "secondary"
                    }
                  >
                    {opportunity.priority === "high" && "Alta"}
                    {opportunity.priority === "medium" && "Media"}
                    {opportunity.priority === "low" && "Baja"}
                  </Badge>
                </div>

                {opportunity.actionUrl && (
                  <Button
                    size="sm"
                    variant="outline"
                    className="mt-3"
                    onClick={() => window.open(opportunity.actionUrl, "_blank")}
                  >
                    Tomar Acción
                  </Button>
                )}
              </motion.div>
            ))}
          </div>
        </div>
      )}

      {/* Quality Scores */}
      {insights.qualityScores && insights.qualityScores.length > 0 && (
        <div className="space-y-4">
          <h3 className="font-semibold text-gray-900 flex items-center gap-2">
            <Target className="h-5 w-5 text-primary-600" />
            Quality Scores de Listings
          </h3>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {insights.qualityScores.slice(0, 6).map((score) => (
              <div key={score.listingId} className="bg-white rounded-xl p-4 shadow-sm border">
                <h4 className="font-medium text-gray-900 mb-2 truncate">
                  {score.vehicleTitle}
                </h4>

                <div className="flex items-center gap-3 mb-3">
                  <div className="flex-1">
                    <Progress value={score.score} className="h-2" />
                  </div>
                  <span className="text-2xl font-bold text-primary-600">
                    {score.score}
                  </span>
                </div>

                <div className="space-y-1.5">
                  {score.improvements.slice(0, 3).map((improvement, i) => (
                    <div key={i} className="flex items-start gap-2">
                      <Info className="h-3.5 w-3.5 text-blue-500 flex-shrink-0 mt-0.5" />
                      <p className="text-xs text-gray-600">{improvement}</p>
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Info card */}
      <div className="bg-gradient-to-r from-primary-50 to-blue-50 border border-primary-200 rounded-xl p-4">
        <div className="flex items-start gap-3">
          <Info className="h-5 w-5 text-primary-600 flex-shrink-0" />
          <div>
            <h4 className="font-medium text-primary-900 mb-1">¿Cómo funciona la IA?</h4>
            <p className="text-sm text-primary-700">
              Nuestros modelos de Machine Learning analizan millones de datos históricos de ventas,
              comportamiento de compradores, tendencias del mercado y tu inventario actual para
              generar predicciones y recomendaciones personalizadas que maximizan tus ventas.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
```

**Hook requerido:**

```typescript
// filepath: src/lib/hooks/useDealerAnalytics.ts
export function useAIInsights(dealerId: string) {
  return useQuery({
    queryKey: ["dealer-ai-insights", dealerId],
    queryFn: () => dealerAnalyticsService.getAIInsights(dealerId),
    staleTime: 5 * 60 * 1000, // 5 minutos
  });
}
```

---

## 🔌 INTEGRACIÓN CON ANALYTICSSERVICE (PUERTO 5070)

### Procesos del AnalyticsService

#### ANLT-001: Event Collection Pipeline

| Campo       | Valor                     |
| ----------- | ------------------------- |
| **ID**      | ANLT-001                  |
| **Nombre**  | Event Collection Pipeline |
| **Actor**   | Sistema                   |
| **Trigger** | Event desde SDK           |

**Flujo del Proceso:**

| Paso | Acción                | Sistema       | Validación           |
| ---- | --------------------- | ------------- | -------------------- |
| 1    | SDK captura evento    | Frontend      | Buffer local         |
| 2    | Batch cada 5 segundos | SDK           | Max 100 eventos      |
| 3    | POST a /api/events    | HTTP          | GZIP compressed      |
| 4    | Validar API key       | EventService  | Write key            |
| 5    | Validar schema        | EventService  | JSON Schema          |
| 6    | Enriquecer evento     | EventService  | GeoIP, User-Agent    |
| 7    | Deduplicar            | Redis         | messageId            |
| 8    | Escribir a Kafka      | Producer      | Partición por userId |
| 9    | ACK al cliente        | Response      | 202 Accepted         |
| 10   | Consumer procesa      | Worker        | Async                |
| 11   | Escribir a ClickHouse | DataWarehouse | Insert               |
| 12   | Actualizar Redis      | Cache         | Métricas RT          |
| 13   | Publicar a RabbitMQ   | MessageBus    | Para consumers       |

#### ANLT-002: Real-time Metrics Update

| Campo       | Valor                  |
| ----------- | ---------------------- |
| **ID**      | ANLT-002               |
| **Nombre**  | Actualizar Métricas RT |
| **Actor**   | Sistema                |
| **Trigger** | Cada evento            |

**Estructura Redis:**

```
# Contadores diarios
analytics:views:vehicle:{id}:2026-01-21 = 245
analytics:views:dealer:{id}:2026-01-21 = 1580

# Usuarios únicos (HyperLogLog)
analytics:users:dau:2026-01-21 = HLL(...)
analytics:users:mau:2026-01 = HLL(...)

# Top vehículos (Sorted Set)
analytics:top:vehicles:2026-01-21 = {
  vehicleId1: 1245,
  vehicleId2: 980,
  ...
}

# Métricas en tiempo real (Hash)
analytics:realtime:platform = {
  activeUsers: 342,
  pageViewsLastHour: 2450,
  leadsLastHour: 23
}
```

#### ANLT-003: Generar Reporte Programado

| Campo       | Valor                       |
| ----------- | --------------------------- |
| **ID**      | ANLT-003                    |
| **Nombre**  | Scheduled Report Generation |
| **Actor**   | Sistema                     |
| **Trigger** | Cron job                    |

**Tipos de Reportes:**

| Reporte                   | Frecuencia    | Destinatarios |
| ------------------------- | ------------- | ------------- |
| Daily Platform Summary    | Diario 6am    | Executives    |
| Weekly Dealer Performance | Lunes 8am     | Dealers       |
| Monthly Revenue Report    | Día 1 del mes | Finance       |
| Quarterly Business Review | Trimestral    | Board         |
| Ad-hoc Custom Reports     | On-demand     | Requested by  |

---

## 📊 DATA WAREHOUSE SCHEMA (ClickHouse)

```sql
-- Eventos de usuario (tabla principal)
CREATE TABLE events (
    event_id UUID,
    event_type String,
    user_id Nullable(UUID),
    session_id UUID,
    timestamp DateTime64(3),

    -- Contexto
    page_url String,
    referrer String,
    device_type String,
    browser String,
    country String,
    city String,

    -- Datos del evento
    properties Map(String, String),

    -- Particionado por fecha
    event_date Date DEFAULT toDate(timestamp)
) ENGINE = MergeTree()
PARTITION BY toYYYYMM(event_date)
ORDER BY (event_type, event_date, user_id);

-- Métricas agregadas diarias
CREATE TABLE daily_metrics (
    date Date,
    metric_type String,
    dimension String,
    dimension_value String,
    value Float64,
    count UInt64
) ENGINE = SummingMergeTree()
PARTITION BY toYYYYMM(date)
ORDER BY (metric_type, dimension, date);
```

---

## 📡 API RESPONSES DE ANALYTICS

### Platform Overview

```json
{
  "period": "2026-01-21",
  "metrics": {
    "activeUsers": {
      "dau": 2450,
      "mau": 45230,
      "dauChange": 8.5,
      "mauChange": 12.3
    },
    "traffic": {
      "pageViews": 125000,
      "sessions": 42000,
      "avgSessionDuration": 245,
      "bounceRate": 35.2
    },
    "vehicles": {
      "total": 12450,
      "active": 10280,
      "newThisMonth": 890,
      "avgViewsPerListing": 45
    },
    "leads": {
      "total": 3450,
      "qualified": 1230,
      "conversionRate": 35.6
    },
    "revenue": {
      "mrr": 485000,
      "arr": 5820000,
      "arpu": 103.5,
      "churnRate": 2.3
    }
  }
}
```

### Dealer Dashboard

```json
{
  "dealerId": "uuid",
  "period": "2026-01",
  "overview": {
    "activeListings": 38,
    "maxListings": 50,
    "totalViews": 8450,
    "totalLeads": 156,
    "salesThisMonth": 12,
    "averageTimeToSale": 21
  },
  "topVehicles": [
    {
      "id": "uuid",
      "title": "Toyota RAV4 XLE 2024",
      "views": 1245,
      "leads": 23,
      "favorites": 45,
      "daysListed": 15
    }
  ],
  "leadFunnel": {
    "views": 8450,
    "leads": 156,
    "contacts": 48,
    "appointments": 28,
    "sales": 12
  },
  "priceInsights": [
    {
      "vehicleId": "uuid",
      "currentPrice": 2300000,
      "recommendedPrice": 2150000,
      "marketAverage": 2180000,
      "recommendation": "Reducir 6% para mayor conversión"
    }
  ]
}
```

---

## 📡 EVENTOS RABBITMQ

| Evento                       | Exchange           | Payload                    |
| ---------------------------- | ------------------ | -------------------------- |
| `analytics.event.received`   | `analytics.events` | `{ eventType, data }`      |
| `analytics.metrics.updated`  | `analytics.events` | `{ metricType, value }`    |
| `analytics.report.generated` | `analytics.events` | `{ reportId, type }`       |
| `analytics.alert.triggered`  | `analytics.alerts` | `{ alertType, threshold }` |

---

## 📊 MÉTRICAS PROMETHEUS

```
# Pipeline
analytics_events_received_total{type="..."}
analytics_events_processed_total
analytics_events_failed_total
analytics_pipeline_latency_ms

# Queries
analytics_queries_total{type="realtime|historical"}
analytics_query_duration_seconds

# Reports
analytics_reports_generated_total{type="..."}
analytics_reports_failed_total

# Storage
analytics_clickhouse_rows_total
analytics_redis_memory_bytes
```

---

## ⚙️ CONFIGURACIÓN

```json
{
  "Analytics": {
    "EventBatchSize": 100,
    "EventFlushInterval": "5s",
    "RealtimeWindow": "1h",
    "RetentionDays": {
      "RawEvents": 90,
      "AggregatedDaily": 730,
      "AggregatedMonthly": 1825
    }
  },
  "ClickHouse": {
    "Host": "clickhouse.okla.internal",
    "Database": "analytics",
    "MaxConnections": 10
  },
  "Reports": {
    "DefaultTimezone": "America/Santo_Domingo",
    "PdfQuality": "high",
    "EmailRetryAttempts": 3
  }
}
```

---

## 🚀 MEJORAS FUTURAS

1. **Real-time Updates**: WebSocket para actualización en vivo de métricas
2. **Custom Dashboards**: Drag-and-drop widgets builder
3. **Predictive Analytics**: ML models para predicción de ventas
4. **Competitor Analysis**: Monitoreo automático de competencia
5. **A/B Testing**: Probar diferentes precios/descripciones
6. **Mobile App**: Notificaciones push para alertas críticas

---

## 📚 DOCUMENTACIÓN CONSOLIDADA

> **NOTA:** Este documento consolida toda la documentación de Dealer Analytics previamente distribuida en múltiples archivos.

### Páginas Incluidas en este Documento

| Página                         | Ruta                          | LOC | Descripción                     |
| ------------------------------ | ----------------------------- | --- | ------------------------------- |
| **DealerAnalyticsPage**        | `/dealer/analytics`           | 289 | Dashboard básico de analytics   |
| **AdvancedAnalyticsDashboard** | `/dealer/analytics/advanced`  | 905 | Dashboard ejecutivo completo    |
| **DealerBenchmarksPage**       | `/dealer/benchmarks`          | 507 | Comparación con mercado         |
| **InventoryAnalyticsPage**     | `/dealer/analytics/inventory` | 864 | Análisis de inventario          |
| **LeadFunnelPage**             | `/dealer/funnel`              | 773 | Embudo de conversión            |
| **FeatureStoreDashboard**      | `/admin/ml/features`          | -   | Features para ML (Admin)        |
| **UserBehaviorDashboard**      | `/admin/ml/behavior`          | -   | Comportamiento usuarios (Admin) |
| **PricingIntelligencePage**    | `/dealer/pricing`             | -   | IA para pricing                 |

### Arquitectura de Analytics Avanzado

```
pages/dealer/analytics/
├── DealerAnalyticsPage.tsx          # Dashboard básico (289 líneas)
├── AdvancedAnalyticsDashboard.tsx   # Dashboard ejecutivo (905 líneas)
├── DealerBenchmarksPage.tsx         # Benchmarks (507 líneas)
├── InventoryAnalyticsPage.tsx       # Inventario analytics (864 líneas)
├── LeadFunnelPage.tsx               # Funnel de leads (773 líneas)
└── components/
    ├── analytics/
    │   ├── MetricCard.tsx           # Card de métrica
    │   ├── ConversionFunnel.tsx     # Embudo 6 etapas
    │   ├── ViewsChart.tsx           # Chart.js Line
    │   ├── TopVehiclesTable.tsx     # Top performing
    │   ├── LeadSourceChart.tsx      # Doughnut fuentes
    │   ├── AgingChart.tsx           # Bar chart aging
    │   └── DateRangePicker.tsx      # Selector fechas
    └── reports/
        ├── SalesReport.tsx          # PDF exportable
        └── InventoryReport.tsx      # Excel exportable
```

### Sistema de Tiers (Benchmarks)

```
Bronze → Silver → Gold → Platinum → Diamond

Criterios por Tier:
- Bronze: Score 0-39 (nuevo dealer, sin métricas)
- Silver: Score 40-59 (rendimiento básico)
- Gold: Score 60-79 (por encima del promedio)
- Platinum: Score 80-89 (top 10% del mercado)
- Diamond: Score 90-100 (élite, top 1%)
```

### Métricas del Embudo (6 Etapas)

```
Views (100%) → Interest (35%) → Leads (8%) → Contacts (4.5%) → Test Drives (2%) → Sales (0.45%)
```

---

## 📚 REFERENCIAS

- [docs/process-matrix/09-REPORTES-ANALYTICS/02-analytics-service.md](../../process-matrix/09-REPORTES-ANALYTICS/02-analytics-service.md)
- [docs/process-matrix/09-REPORTES-ANALYTICS/03-event-tracking.md](../../process-matrix/09-REPORTES-ANALYTICS/03-event-tracking.md)
- [docs/process-matrix/09-REPORTES-ANALYTICS/04-dashboards.md](../../process-matrix/09-REPORTES-ANALYTICS/04-dashboards.md)

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/dealer-analytics.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsDealer } from "../helpers/auth";

test.describe("Dealer Analytics", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsDealer(page);
  });

  test("debe mostrar dashboard de analíticas", async ({ page }) => {
    await page.goto("/dealer/analytics");

    await expect(
      page.getByRole("heading", { name: /analíticas/i }),
    ).toBeVisible();
    await expect(page.getByTestId("analytics-charts")).toBeVisible();
  });

  test("debe mostrar métricas clave", async ({ page }) => {
    await page.goto("/dealer/analytics");

    await expect(page.getByTestId("metric-views")).toBeVisible();
    await expect(page.getByTestId("metric-leads")).toBeVisible();
    await expect(page.getByTestId("metric-conversion")).toBeVisible();
  });

  test("debe cambiar rango de fechas", async ({ page }) => {
    await page.goto("/dealer/analytics");

    await page.getByRole("combobox", { name: /periodo/i }).click();
    await page.getByRole("option", { name: /últimos 30 días/i }).click();

    await expect(page).toHaveURL(/period=30d/);
  });

  test("debe mostrar gráfico de vistas por vehículo", async ({ page }) => {
    await page.goto("/dealer/analytics");

    await expect(page.getByTestId("views-chart")).toBeVisible();
  });

  test("debe exportar reporte", async ({ page }) => {
    await page.goto("/dealer/analytics");

    const downloadPromise = page.waitForEvent("download");
    await page.getByRole("button", { name: /exportar/i }).click();
    await page.getByRole("menuitem", { name: /csv/i }).click();
    const download = await downloadPromise;

    expect(download.suggestedFilename()).toMatch(/analytics.*\.csv/i);
  });
});
```

---

**Siguiente documento:** `29-dealer-onboarding-completo.md` - Flujo completo de onboarding para dealers
