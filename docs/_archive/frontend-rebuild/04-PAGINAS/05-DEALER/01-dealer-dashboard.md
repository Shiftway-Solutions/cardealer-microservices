---
title: "🏢 Dashboard de Dealer - Overview"
priority: P1
estimated_time: "40 minutos"
dependencies: []
apis: ["DealerManagementService"]
status: complete
last_updated: "2026-01-30"
---

# 🏢 Dashboard de Dealer - Overview

> **Ruta:** `/dealer/dashboard` (Página principal de dealer)  
> **Scope:** Vista rápida con KPIs básicos, actividad reciente y acciones rápidas  
> **Analytics avanzado:** Ver [28-dealer-analytics-completo.md](28-dealer-analytics-completo.md) para charts, funnels y reportes

> **Tiempo estimado:** 40 minutos
> **Prerrequisitos:** Dashboard usuario, DealerManagementService

---

## 📋 OBJETIVO

Implementar dashboard de dealer (Overview):

- Métricas básicas de negocio (4 KPI cards)
- Inventario de vehículos reciente
- Gestión de leads pendientes
- Acciones rápidas (Publicar, Ver Inventario, etc.)
- Link a Analytics avanzado: "Ver Analytics Completo" → `/dealer/analytics`

---

## 🔧 PASO 1: Layout de Dealer

```typescript
// filepath: src/app/(main)/dealer/layout.tsx
import { redirect } from "next/navigation";
import { auth } from "@/lib/auth";
import { DealerSidebar } from "@/components/dealer/DealerSidebar";
import { dealerService } from "@/lib/services/dealerService";

export default async function DealerLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await auth();

  if (!session?.user) {
    redirect("/login?callbackUrl=/dealer");
  }

  const dealer = await dealerService.getByUserId(session.user.id);

  if (!dealer) {
    redirect("/dealer/registro");
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="container py-8">
        <div className="flex flex-col lg:flex-row gap-8">
          <aside className="lg:w-64 flex-shrink-0">
            <DealerSidebar dealer={dealer} />
          </aside>
          <main className="flex-1 min-w-0">{children}</main>
        </div>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 2: DealerSidebar

```typescript
// filepath: src/components/dealer/DealerSidebar.tsx
"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  LayoutDashboard,
  Car,
  Users,
  BarChart3,
  Settings,
  Building2,
} from "lucide-react";
import { Badge } from "@/components/ui/Badge";
import { cn } from "@/lib/utils";
import type { Dealer } from "@/types";

const navItems = [
  { href: "/dealer", label: "Dashboard", icon: LayoutDashboard },
  { href: "/dealer/inventario", label: "Inventario", icon: Car },
  { href: "/dealer/leads", label: "Leads", icon: Users },
  { href: "/dealer/estadisticas", label: "Estadísticas", icon: BarChart3 },
  { href: "/dealer/sucursales", label: "Sucursales", icon: Building2 },
  { href: "/dealer/configuracion", label: "Configuración", icon: Settings },
];

interface DealerSidebarProps {
  dealer: Dealer;
}

export function DealerSidebar({ dealer }: DealerSidebarProps) {
  const pathname = usePathname();

  return (
    <div className="bg-white rounded-xl border p-4 lg:sticky lg:top-24">
      {/* Dealer info */}
      <div className="p-3 mb-4">
        <div className="flex items-center gap-3">
          {dealer.logo ? (
            <img
              src={dealer.logo}
              alt={dealer.businessName}
              className="w-12 h-12 rounded-lg object-cover"
            />
          ) : (
            <div className="w-12 h-12 rounded-lg bg-primary-100 flex items-center justify-center">
              <Building2 className="text-primary-600" />
            </div>
          )}
          <div className="min-w-0">
            <p className="font-medium text-gray-900 truncate">
              {dealer.businessName}
            </p>
            <Badge
              variant={dealer.status === "Active" ? "success" : "warning"}
              className="text-xs"
            >
              {dealer.status}
            </Badge>
          </div>
        </div>

        {/* Plan info */}
        <div className="mt-4 p-3 bg-gray-50 rounded-lg">
          <p className="text-xs text-gray-500">Plan actual</p>
          <p className="font-medium text-gray-900">{dealer.currentPlan}</p>
          <p className="text-xs text-gray-500 mt-1">
            {dealer.activeListingsCount} / {dealer.maxActiveListings} vehículos
          </p>
        </div>
      </div>

      {/* Navigation */}
      <nav className="space-y-1">
        {navItems.map((item) => {
          const isActive = pathname === item.href;
          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors",
                isActive
                  ? "bg-primary-50 text-primary-700"
                  : "text-gray-600 hover:bg-gray-50"
              )}
            >
              <item.icon size={18} />
              {item.label}
            </Link>
          );
        })}
      </nav>
    </div>
  );
}
```

---

## 🔧 PASO 3: Dashboard Principal

```typescript
// filepath: src/app/(main)/dealer/page.tsx
import { Metadata } from "next";
import { auth } from "@/lib/auth";
import { DealerStats } from "@/components/dealer/DealerStats";
import { DealerQuickActions } from "@/components/dealer/DealerQuickActions";
import { RecentLeads } from "@/components/dealer/RecentLeads";
import { InventoryOverview } from "@/components/dealer/InventoryOverview";
import { dealerService } from "@/lib/services/dealerService";

export const metadata: Metadata = {
  title: "Dashboard Dealer | OKLA",
};

export default async function DealerDashboardPage() {
  const session = await auth();
  const dealer = await dealerService.getByUserId(session!.user.id);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-gray-900">
          Hola, {dealer?.businessName}
        </h1>
        <p className="text-gray-600">Resumen de tu actividad</p>
      </div>

      {/* Stats */}
      <DealerStats dealerId={dealer!.id} />

      {/* Quick Actions */}
      <DealerQuickActions canPublish={
        dealer!.activeListingsCount < dealer!.maxActiveListings
      } />

      {/* Two columns */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <RecentLeads dealerId={dealer!.id} />
        <InventoryOverview dealerId={dealer!.id} />
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 4: DealerStats

```typescript
// filepath: src/components/dealer/DealerStats.tsx
"use client";

import { Car, Eye, Users, TrendingUp } from "lucide-react";
import { useDealerStats } from "@/lib/hooks/useDealerStats";

interface DealerStatsProps {
  dealerId: string;
}

export function DealerStats({ dealerId }: DealerStatsProps) {
  const { data: stats, isLoading } = useDealerStats(dealerId);

  const items = [
    {
      label: "Vehículos activos",
      value: stats?.activeVehicles ?? 0,
      icon: Car,
      color: "bg-blue-50 text-blue-600",
    },
    {
      label: "Vistas este mes",
      value: stats?.viewsThisMonth ?? 0,
      icon: Eye,
      color: "bg-green-50 text-green-600",
      change: stats?.viewsChange,
    },
    {
      label: "Leads nuevos",
      value: stats?.newLeads ?? 0,
      icon: Users,
      color: "bg-purple-50 text-purple-600",
      change: stats?.leadsChange,
    },
    {
      label: "Tasa conversión",
      value: `${stats?.conversionRate ?? 0}%`,
      icon: TrendingUp,
      color: "bg-amber-50 text-amber-600",
    },
  ];

  if (isLoading) {
    return (
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="bg-white rounded-xl border p-4 animate-pulse">
            <div className="w-10 h-10 bg-gray-200 rounded-lg" />
            <div className="h-6 w-16 bg-gray-200 rounded mt-3" />
            <div className="h-4 w-24 bg-gray-200 rounded mt-1" />
          </div>
        ))}
      </div>
    );
  }

  return (
    <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
      {items.map((item) => (
        <div key={item.label} className="bg-white rounded-xl border p-4">
          <div className={`inline-flex p-2 rounded-lg ${item.color}`}>
            <item.icon size={20} />
          </div>
          <p className="text-2xl font-bold text-gray-900 mt-3">
            {typeof item.value === "number"
              ? item.value.toLocaleString()
              : item.value}
          </p>
          <div className="flex items-center gap-2">
            <p className="text-sm text-gray-500">{item.label}</p>
            {item.change !== undefined && (
              <span
                className={`text-xs font-medium ${
                  item.change >= 0 ? "text-green-600" : "text-red-600"
                }`}
              >
                {item.change >= 0 ? "+" : ""}
                {item.change}%
              </span>
            )}
          </div>
        </div>
      ))}
    </div>
  );
}
```

---

## 🔧 PASO 5: RecentLeads

```typescript
// filepath: src/components/dealer/RecentLeads.tsx
"use client";

import Link from "next/link";
import { ArrowRight, Mail, Phone, Clock } from "lucide-react";
import { useRecentLeads } from "@/lib/hooks/useRecentLeads";
import { Badge } from "@/components/ui/Badge";
import { formatRelativeDate } from "@/lib/utils";

interface RecentLeadsProps {
  dealerId: string;
}

export function RecentLeads({ dealerId }: RecentLeadsProps) {
  const { data: leads, isLoading } = useRecentLeads(dealerId, { limit: 5 });

  if (isLoading) {
    return (
      <div className="bg-white rounded-xl border p-6">
        <div className="h-5 w-32 bg-gray-200 rounded animate-pulse mb-4" />
        <div className="space-y-4">
          {Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="flex gap-3 animate-pulse">
              <div className="w-10 h-10 bg-gray-200 rounded-full" />
              <div className="flex-1 space-y-2">
                <div className="h-4 w-2/3 bg-gray-200 rounded" />
                <div className="h-3 w-1/3 bg-gray-200 rounded" />
              </div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-xl border p-6">
      <div className="flex items-center justify-between mb-4">
        <h2 className="font-semibold text-gray-900">Leads recientes</h2>
        <Link
          href="/dealer/leads"
          className="text-sm text-primary-600 hover:text-primary-700 flex items-center gap-1"
        >
          Ver todos
          <ArrowRight size={14} />
        </Link>
      </div>

      {leads && leads.length > 0 ? (
        <div className="space-y-4">
          {leads.map((lead) => (
            <div
              key={lead.id}
              className="flex items-start gap-3 p-3 hover:bg-gray-50 rounded-lg transition-colors"
            >
              <div className="w-10 h-10 rounded-full bg-primary-100 flex items-center justify-center text-primary-600 font-medium">
                {lead.name.charAt(0)}
              </div>
              <div className="flex-1 min-w-0">
                <div className="flex items-center justify-between">
                  <p className="font-medium text-gray-900 truncate">
                    {lead.name}
                  </p>
                  <Badge variant={lead.status === "new" ? "warning" : "default"}>
                    {lead.status}
                  </Badge>
                </div>
                <p className="text-sm text-gray-600 truncate">
                  Interesado en: {lead.vehicleTitle}
                </p>
                <div className="flex items-center gap-4 mt-1 text-xs text-gray-500">
                  <span className="flex items-center gap-1">
                    <Clock size={12} />
                    {formatRelativeDate(lead.createdAt)}
                  </span>
                </div>
              </div>
            </div>
          ))}
        </div>
      ) : (
        <p className="text-gray-500 text-center py-4">Sin leads recientes</p>
      )}
    </div>
  );
}
```

---

## ✅ VALIDACIÓN

```bash
pnpm dev
# Verificar:
# - /dealer requiere ser dealer
# - Sidebar muestra info del dealer
# - Stats se cargan
# - Leads se muestran
```

---

## 📚 DOCUMENTACIÓN CONSOLIDADA

> **NOTA:** Este documento consolida la documentación de Dealer Dashboard previamente distribuida en múltiples archivos.

### Páginas Incluidas en este Documento

| Página                      | Ruta                   | Descripción                  |
| --------------------------- | ---------------------- | ---------------------------- |
| **DealerDashboardPage**     | `/dealer/dashboard`    | Dashboard principal con KPIs |
| **DealerHomePage**          | `/dealer/home`         | Vista alternativa del home   |
| **DealerPortalLayout**      | Layout                 | Layout del portal dealer     |
| **DealerBenchmarksPage**    | `/dealer/benchmarks`   | Comparación con el mercado   |
| **LeadFunnelPage**          | `/dealer/funnel`       | Análisis del funnel de leads |
| **DealerProfileEditorPage** | `/dealer/profile/edit` | Editor de perfil público     |

### Arquitectura Completa del Portal Dealer

```
pages/dealer/
├── DealerDashboardPage.tsx     # Dashboard principal (639 líneas)
├── DealerHomePage.tsx          # Vista alternativa home (801 líneas)
├── DealerBenchmarksPage.tsx    # Benchmarks vs mercado
├── LeadFunnelPage.tsx          # Análisis funnel leads
├── DealerProfileEditorPage.tsx # Editor perfil público (458 líneas)
├── layout/
│   └── DealerPortalLayout.tsx  # Layout del portal
└── components/
    ├── StatCard.tsx            # Tarjeta de estadística
    ├── QuickAction.tsx         # Botón de acción rápida
    ├── MetricCards.tsx         # Grid de métricas (4 cards)
    ├── RecentActivity.tsx      # Timeline de actividad
    └── InventoryBreakdown.tsx  # Desglose de inventario
```

### Navegación del Sidebar

```typescript
const sidebarLinks = [
  { href: "/dealer/dashboard", icon: FiGrid, label: "Dashboard" },
  { href: "/dealer/inventory", icon: FiPackage, label: "Inventario" },
  { href: "/dealer/analytics", icon: FiBarChart2, label: "Analíticas" },
  { href: "/dealer/leads", icon: FiUsers, label: "Leads" },
  { href: "/dealer/messages", icon: FiMessageSquare, label: "Mensajes" },
  { href: "/dealer/billing", icon: FiDollarSign, label: "Facturación" },
  { href: "/dealer/settings", icon: FiSettings, label: "Configuración" },
];
```

### DealerBenchmarksPage - Comparación con el Mercado

**Ruta:** `/dealer/benchmarks`

Compara el rendimiento del dealer con otros del mercado usando un sistema de tiers:

```typescript
interface BenchmarkData {
  overallScore: number; // 0-100
  tier: "Bronze" | "Silver" | "Gold" | "Platinum";
  metrics: {
    avgDaysOnMarket: { value: number; percentile: number };
    conversionRate: { value: number; percentile: number };
    responseTime: { value: number; percentile: number };
    inventoryTurnover: { value: number; percentile: number };
  };
}
```

### LeadFunnelPage - Análisis del Funnel

**Ruta:** `/dealer/funnel`

Visualización del embudo de conversión con Chart.js:

- **Funnel Visual** - Etapas del embudo (Views → Inquiries → Test Drives → Sales)
- **Source Donut** - Distribución por fuente (Organic, Paid, Social, Direct)
- **Trend Line** - Tendencia temporal de conversión
- **Top Performing** - Vehículos con mejor rendimiento

### Referencias Relacionadas

- **Analytics Avanzado:** [28-dealer-analytics-completo.md](28-dealer-analytics-completo.md)
- **Inventario:** [09-dealer-inventario.md](09-dealer-inventario.md)
- **CRM y Leads:** [10-dealer-crm.md](10-dealer-crm.md)
- **Onboarding:** [29-dealer-onboarding-completo.md](29-dealer-onboarding-completo.md)

---

## 🔧 Servicio del Dashboard

```typescript
// filepath: src/lib/services/dealerDashboardService.ts
import { apiClient } from "@/lib/apiClient";

export interface DealerDashboardStats {
  activeListings: number;
  maxListings: number;
  viewsThisMonth: number;
  viewsChange: number; // percentage vs last month
  leadsThisMonth: number;
  leadsChange: number;
  pendingLeads: number;
  inventoryValue: number;
  avgPrice: number;
  conversionRate: number;
}

export interface RecentLead {
  id: string;
  vehicleId: string;
  vehicleTitle: string;
  vehicleThumbnail: string;
  customerName: string;
  customerPhone?: string;
  customerEmail: string;
  message?: string;
  status: "new" | "contacted" | "scheduled" | "converted" | "lost";
  createdAt: string;
}

export interface RecentActivity {
  id: string;
  type:
    | "listing_view"
    | "lead_received"
    | "message_received"
    | "listing_published"
    | "listing_sold";
  title: string;
  description?: string;
  vehicleId?: string;
  createdAt: string;
}

export interface InventoryBreakdown {
  byStatus: {
    active: number;
    pending: number;
    sold: number;
    draft: number;
  };
  byType: Array<{ type: string; count: number }>;
  byPriceRange: Array<{ range: string; count: number }>;
  avgDaysOnMarket: number;
}

export const dealerDashboardService = {
  // Estadísticas principales
  async getStats(): Promise<DealerDashboardStats> {
    return apiClient.get("/dealer/dashboard/stats");
  },

  // Leads recientes
  async getRecentLeads(limit = 5): Promise<RecentLead[]> {
    return apiClient.get(`/dealer/leads/recent?limit=${limit}`);
  },

  // Actividad reciente
  async getRecentActivity(limit = 10): Promise<RecentActivity[]> {
    return apiClient.get(`/dealer/activity/recent?limit=${limit}`);
  },

  // Desglose de inventario
  async getInventoryBreakdown(): Promise<InventoryBreakdown> {
    return apiClient.get("/dealer/inventory/breakdown");
  },

  // Métricas rápidas para widgets
  async getQuickMetrics(): Promise<{
    todayViews: number;
    todayLeads: number;
    pendingResponses: number;
    expiringListings: number;
  }> {
    return apiClient.get("/dealer/dashboard/quick-metrics");
  },

  // Top performers del mes
  async getTopPerformers(limit = 5): Promise<
    Array<{
      vehicleId: string;
      title: string;
      thumbnail: string;
      views: number;
      leads: number;
    }>
  > {
    return apiClient.get(`/dealer/vehicles/top-performers?limit=${limit}`);
  },
};
```

---

## 🎨 Estados de UI

### Loading State

```typescript
// Dashboard Loading Skeleton
export function DealerDashboardSkeleton() {
  return (
    <div className="space-y-6">
      {/* Header Skeleton */}
      <div className="flex justify-between items-center">
        <div>
          <Skeleton className="h-8 w-48 mb-2" />
          <Skeleton className="h-4 w-64" />
        </div>
        <Skeleton className="h-10 w-40" />
      </div>

      {/* Stats Cards Skeleton */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        {[1, 2, 3, 4].map((i) => (
          <div key={i} className="bg-white rounded-xl border p-4">
            <div className="flex items-center gap-3">
              <Skeleton className="h-12 w-12 rounded-lg" />
              <div className="flex-1">
                <Skeleton className="h-6 w-16 mb-1" />
                <Skeleton className="h-4 w-24" />
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Content Grid Skeleton */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 bg-white rounded-xl border p-6">
          <Skeleton className="h-6 w-32 mb-4" />
          <div className="space-y-3">
            {[1, 2, 3].map((i) => (
              <div key={i} className="flex gap-3">
                <Skeleton className="h-16 w-20 rounded" />
                <div className="flex-1">
                  <Skeleton className="h-4 w-3/4 mb-2" />
                  <Skeleton className="h-3 w-1/2" />
                </div>
              </div>
            ))}
          </div>
        </div>
        <div className="bg-white rounded-xl border p-6">
          <Skeleton className="h-6 w-32 mb-4" />
          <div className="space-y-3">
            {[1, 2, 3, 4].map((i) => (
              <Skeleton key={i} className="h-10 w-full" />
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
```

### Empty State

```typescript
export function DealerDashboardEmpty() {
  return (
    <div className="space-y-6">
      {/* Welcome Banner */}
      <div className="bg-gradient-to-r from-blue-600 to-blue-800 rounded-xl p-8 text-white">
        <h1 className="text-2xl font-bold mb-2">¡Bienvenido a tu Dashboard!</h1>
        <p className="text-blue-100 mb-4">
          Comienza a publicar vehículos para ver tus estadísticas aquí.
        </p>
        <Link href="/dealer/inventario/nuevo">
          <Button variant="white" size="lg">
            <Plus size={16} className="mr-2" />
            Publicar tu primer vehículo
          </Button>
        </Link>
      </div>

      {/* Quick Start Guide */}
      <div className="bg-white rounded-xl border p-6">
        <h2 className="text-lg font-semibold mb-4">Guía de inicio rápido</h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="p-4 bg-gray-50 rounded-lg">
            <div className="w-10 h-10 bg-blue-100 rounded-full flex items-center justify-center mb-3">
              <span className="text-blue-600 font-bold">1</span>
            </div>
            <h3 className="font-medium mb-1">Completa tu perfil</h3>
            <p className="text-sm text-gray-500">Agrega logo, descripción y horarios</p>
          </div>
          <div className="p-4 bg-gray-50 rounded-lg">
            <div className="w-10 h-10 bg-blue-100 rounded-full flex items-center justify-center mb-3">
              <span className="text-blue-600 font-bold">2</span>
            </div>
            <h3 className="font-medium mb-1">Publica vehículos</h3>
            <p className="text-sm text-gray-500">Sube fotos y detalles de tu inventario</p>
          </div>
          <div className="p-4 bg-gray-50 rounded-lg">
            <div className="w-10 h-10 bg-blue-100 rounded-full flex items-center justify-center mb-3">
              <span className="text-blue-600 font-bold">3</span>
            </div>
            <h3 className="font-medium mb-1">Responde leads</h3>
            <p className="text-sm text-gray-500">Contacta a clientes interesados</p>
          </div>
        </div>
      </div>
    </div>
  );
}
```

### Error State

```typescript
export function DealerDashboardError({ onRetry }: { onRetry: () => void }) {
  return (
    <div className="text-center py-16 bg-white rounded-xl border">
      <AlertCircle size={48} className="mx-auto text-red-400 mb-4" />
      <h3 className="text-lg font-semibold text-gray-900 mb-2">
        Error al cargar el dashboard
      </h3>
      <p className="text-gray-500 mb-4 max-w-md mx-auto">
        No pudimos obtener tus estadísticas. Por favor intenta de nuevo.
      </p>
      <Button variant="outline" onClick={onRetry}>
        <RefreshCw size={16} className="mr-2" />
        Reintentar
      </Button>
    </div>
  );
}
```

---

## 🧪 Testing E2E

```typescript
// filepath: e2e/dealer/dashboard.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsDealer } from "../helpers/auth";

test.describe("Dealer Dashboard", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsDealer(page);
  });

  test("should display dashboard with stats", async ({ page }) => {
    await page.goto("/dealer/dashboard");

    await expect(
      page.getByRole("heading", { name: "Dashboard" }),
    ).toBeVisible();
    await expect(page.getByTestId("stat-card")).toHaveCount(4);
  });

  test("should show active listings count", async ({ page }) => {
    await page.goto("/dealer/dashboard");

    await expect(page.getByText("Vehículos Activos")).toBeVisible();
  });

  test("should display recent leads", async ({ page }) => {
    await page.goto("/dealer/dashboard");

    await expect(page.getByTestId("recent-leads")).toBeVisible();
  });

  test("should navigate to publish new vehicle", async ({ page }) => {
    await page.goto("/dealer/dashboard");

    await page.getByRole("button", { name: /Publicar/i }).click();

    await expect(page).toHaveURL(/\/dealer\/inventario\/nuevo/);
  });

  test("should navigate to inventory", async ({ page }) => {
    await page.goto("/dealer/dashboard");

    await page.getByRole("link", { name: "Ver Inventario" }).click();

    await expect(page).toHaveURL(/\/dealer\/inventario/);
  });

  test("should show quick actions", async ({ page }) => {
    await page.goto("/dealer/dashboard");

    await expect(page.getByTestId("quick-actions")).toBeVisible();
    await expect(page.getByRole("button", { name: /Publicar/i })).toBeVisible();
  });

  test("should display recent activity timeline", async ({ page }) => {
    await page.goto("/dealer/dashboard");

    await expect(page.getByTestId("recent-activity")).toBeVisible();
  });

  test("should navigate to analytics", async ({ page }) => {
    await page.goto("/dealer/dashboard");

    await page.getByRole("link", { name: /Analíticas|Analytics/i }).click();

    await expect(page).toHaveURL(/\/dealer\/analytics/);
  });

  test("should be responsive on tablet", async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.goto("/dealer/dashboard");

    // Stats should stack on tablet
    await expect(page.getByTestId("stat-card")).toBeVisible();
  });

  test("should show sidebar navigation", async ({ page }) => {
    await page.goto("/dealer/dashboard");

    await expect(page.getByTestId("dealer-sidebar")).toBeVisible();
    await expect(page.getByRole("link", { name: "Inventario" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Leads" })).toBeVisible();
  });
});
```

---

## 📊 Analytics Events

```typescript
// filepath: src/lib/analytics/dealerDashboardEvents.ts
import { analytics } from "@/lib/analytics";

export const dealerDashboardEvents = {
  // Page views
  viewDashboard: (dealerId: string) => {
    analytics.page("Dealer Dashboard", { dealerId });
  },

  // Quick actions
  clickQuickAction: (action: string) => {
    analytics.track("dealer_quick_action_clicked", { action });
  },

  // Navigation
  clickSidebarLink: (link: string) => {
    analytics.track("dealer_sidebar_clicked", { link });
  },

  // Lead interactions
  clickLead: (leadId: string) => {
    analytics.track("dealer_lead_clicked", { leadId });
  },

  // Stats interactions
  clickStatCard: (statName: string) => {
    analytics.track("dealer_stat_card_clicked", { statName });
  },

  // Refresh
  refreshDashboard: () => {
    analytics.track("dealer_dashboard_refreshed");
  },

  // Time on page
  trackTimeOnDashboard: (seconds: number) => {
    analytics.track("dealer_dashboard_time_spent", { seconds });
  },
};
```

---

## 🔐 Permisos y Acceso

| Acción                     | DEALER-OWNER | DEALER-MANAGER | DEALER-SALES |
| -------------------------- | ------------ | -------------- | ------------ |
| Ver dashboard completo     | ✅           | ✅             | ✅           |
| Ver estadísticas de ventas | ✅           | ✅             | ❌           |
| Ver valor de inventario    | ✅           | ✅             | ❌           |
| Ver todos los leads        | ✅           | ✅             | ❌           |
| Ver solo sus leads         | ✅           | ✅             | ✅           |
| Publicar vehículos         | ✅           | ✅             | ❌           |
| Ver benchmarks             | ✅           | ✅             | ❌           |
| Configurar dashboard       | ✅           | ❌             | ❌           |

---

## ✅ Checklist de Implementación

### Backend (DealerManagementService)

- [ ] Endpoint `GET /api/dealer/dashboard/stats` estadísticas
- [ ] Endpoint `GET /api/dealer/leads/recent` leads recientes
- [ ] Endpoint `GET /api/dealer/activity/recent` actividad reciente
- [ ] Endpoint `GET /api/dealer/inventory/breakdown` desglose
- [ ] Endpoint `GET /api/dealer/dashboard/quick-metrics` métricas rápidas
- [ ] Endpoint `GET /api/dealer/vehicles/top-performers` top performers
- [ ] Cache de estadísticas (Redis, 5 min TTL)
- [ ] Cálculo de cambios vs mes anterior

### Frontend

- [ ] Layout `DealerPortalLayout` con sidebar
- [ ] Componente `DealerSidebar` navegación
- [ ] Página `DealerDashboardPage` principal
- [ ] Componente `DealerStats` (4 cards)
- [ ] Componente `RecentLeads` lista
- [ ] Componente `RecentActivity` timeline
- [ ] Componente `QuickActions` botones
- [ ] Componente `InventoryBreakdown` (opcional)
- [ ] Estados: Loading, Empty, Error
- [ ] Servicio `dealerDashboardService`
- [ ] Tests E2E completos
- [ ] Analytics tracking
- [ ] Responsive design

---

## ➡️ SIGUIENTE PASO

Continuar con: [02-dealer-inventory.md](./02-dealer-inventario.md)
