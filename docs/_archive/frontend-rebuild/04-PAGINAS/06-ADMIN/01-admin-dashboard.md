---
title: "Admin - Dashboard"
priority: P2
estimated_time: "45 minutos"
dependencies: []
apis: ["AdminService", "NotificationService", "MediaService"]
status: complete
last_updated: "2026-01-30"
---

# 📊 Admin - Dashboard

> **Tiempo estimado:** 45 minutos
> **Prerrequisitos:** AdminService, PlatformAnalyticsService, ReportsService, DashboardsService
> **Roles:** ADM-ADMIN, ADM-SUPER
> **Última actualización:** Enero 2026

---

## 📊 AUDITORÍA DE INTEGRACIONES

| Backend Service                     | Puerto | Estado Backend | Estado UI       |
| ----------------------------------- | ------ | -------------- | --------------- |
| AdminService                        | 5015   | ✅ 100%        | 🟡 70%          |
| PlatformAnalyticsService            | 5070   | ✅ 80%         | 🟡 60%          |
| **ReportsService** (Puerto 5095)    | 5095   | ✅ 100%        | 🟡 60%          |
| **DashboardsService** (Puerto 5020) | 5020   | ✅ 90%         | 🟡 70%          |
| EventTrackingService                | 5050   | ✅ 100%        | ⚫ Backend only |

### Rutas UI Faltantes

| Ruta                          | Prioridad | Estado | Descripción                    |
| ----------------------------- | --------- | ------ | ------------------------------ |
| `/admin/reports/scheduled`    | P2        | ❌     | Reportes programados           |
| `/admin/reports/builder`      | P2        | ❌     | Constructor de reportes custom |
| `/admin/analytics/platform`   | P1        | ❌     | Analytics de plataforma        |
| `/admin/analytics/users`      | P1        | ❌     | Análisis de usuarios           |
| `/admin/analytics/revenue`    | P1        | ❌     | Dashboard de ingresos          |
| `/admin/dashboard/widgets`    | P2        | ❌     | Catálogo de widgets            |
| `/admin/dashboard/compliance` | P2        | ❌     | Dashboard de compliance        |
| `/admin/compliance/alerts`    | P1        | ❌     | Alertas regulatorias           |

---

## 📋 OBJETIVO

Implementar dashboard administrativo:

- KPIs principales de la plataforma
- Gráficos de tendencias
- Alertas del sistema
- Accesos rápidos

---

## 🎨 WIREFRAME - ADMIN DASHBOARD

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ ┌──────────┐                                                                │
│ │ SIDEBAR  │  ADMIN DASHBOARD                           🔔 5 │ 👤 Admin ▼  │
│ │          │ ────────────────────────────────────────────────────────────── │
│ │ 📊 Dash  │                                                                │
│ │ 👥 Users │  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌───────────┐│
│ │ 🛡️ Mod   │  │ 👥 15,420   │ │ 🚗 2,340    │ │ 💰 $125K    │ │ 🏢 89     ││
│ │ ⚙️ System│  │ Usuarios    │ │ Listings    │ │ MRR         │ │ Dealers   ││
│ │ 📈 Stats │  │ ↑ 12% ✅    │ │ ↑ 8% ✅     │ │ ↑ 15% ✅    │ │ ↑ 5% ✅   ││
│ │ 🔧 Config│  └─────────────┘ └─────────────┘ └─────────────┘ └───────────┘│
│ │          │                                                                │
│ │ ──────── │  ┌──────────────────────────────┐ ┌───────────────────────────┐│
│ │ 📋 Reportes│  │ 📈 TENDENCIAS (30 días)     │ │ 🚨 ALERTAS SISTEMA        ││
│ │ 💼 Billing │  │                             │ │                           ││
│ │ 🔐 Security│  │      ╱╲    ╱╲              │ │ ⚠️ 3 reportes pendientes  ││
│ │          │  │    ╱    ╲╱    ╲             │ │ 🔴 2 dealers sin verificar││
│ │          │  │  ╱            ╲             │ │ ⚡ CPU al 85%             ││
│ │          │  │ ╱               ╲───        │ │ ✅ Todos sistemas OK       ││
│ │          │  │                             │ │                           ││
│ │          │  │ [Usuarios] [Listings] [Rev] │ │ [Ver todas las alertas →] ││
│ │          │  └──────────────────────────────┘ └───────────────────────────┘│
│ │          │                                                                │
│ │          │  ┌──────────────────────────────┐ ┌───────────────────────────┐│
│ │          │  │ 📋 ACTIVIDAD RECIENTE        │ │ ⚡ ACCESOS RÁPIDOS        ││
│ │          │  │                             │ │                           ││
│ │          │  │ • Juan aprobó listing #234  │ │ [+ Nuevo Admin]           ││
│ │          │  │ • Maria rechazó reporte     │ │ [📊 Generar Reporte]      ││
│ │          │  │ • Pedro verificó dealer     │ │ [🔧 Configuración]        ││
│ │          │  │ • Sistema: Backup completado │ │ [📧 Enviar Notificación]  ││
│ │          │  │                             │ │                           ││
│ │          │  │ [Ver historial completo →]  │ │                           ││
│ │          │  └──────────────────────────────┘ └───────────────────────────┘│
│ └──────────┘                                                                │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔧 PASO 1: Layout Admin

```typescript
// filepath: src/app/(admin)/layout.tsx
import { redirect } from "next/navigation";
import { auth } from "@/lib/auth";
import { AdminSidebar } from "@/components/admin/AdminSidebar";
import { AdminHeader } from "@/components/admin/AdminHeader";

export default async function AdminLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await auth();

  if (!session?.user) {
    redirect("/login?callbackUrl=/admin");
  }

  const adminRoles = ["ADM-SUPPORT", "ADM-MOD", "ADM-COMP", "ADM-ADMIN", "ADM-SUPER"];
  if (!adminRoles.includes(session.user.role)) {
    redirect("/");
  }

  return (
    <div className="min-h-screen bg-gray-100">
      <AdminSidebar role={session.user.role} />
      <div className="lg:pl-64">
        <AdminHeader user={session.user} />
        <main className="p-6">{children}</main>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 2: AdminSidebar

```typescript
// filepath: src/components/admin/AdminSidebar.tsx
"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  LayoutDashboard,
  Users,
  Car,
  Building2,
  Shield,
  AlertTriangle,
  FileCheck,
  Headphones,
  Settings,
  BarChart3,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface NavItem {
  icon: React.ComponentType<{ size: number }>;
  label: string;
  href: string;
  roles: string[];
}

const navItems: NavItem[] = [
  { icon: LayoutDashboard, label: "Dashboard", href: "/admin", roles: ["ADM-SUPPORT", "ADM-MOD", "ADM-COMP", "ADM-ADMIN", "ADM-SUPER"] },
  { icon: Users, label: "Usuarios", href: "/admin/usuarios", roles: ["ADM-ADMIN", "ADM-SUPER"] },
  { icon: Car, label: "Vehículos", href: "/admin/vehiculos", roles: ["ADM-MOD", "ADM-ADMIN", "ADM-SUPER"] },
  { icon: Building2, label: "Dealers", href: "/admin/dealers", roles: ["ADM-ADMIN", "ADM-SUPER"] },
  { icon: AlertTriangle, label: "Moderación", href: "/admin/moderacion", roles: ["ADM-MOD", "ADM-ADMIN", "ADM-SUPER"] },
  { icon: FileCheck, label: "Compliance", href: "/admin/compliance", roles: ["ADM-COMP", "ADM-ADMIN", "ADM-SUPER"] },
  { icon: Headphones, label: "Soporte", href: "/admin/soporte", roles: ["ADM-SUPPORT", "ADM-ADMIN", "ADM-SUPER"] },
  { icon: BarChart3, label: "Analytics", href: "/admin/analytics", roles: ["ADM-ADMIN", "ADM-SUPER"] },
  { icon: Settings, label: "Sistema", href: "/admin/sistema", roles: ["ADM-SUPER"] },
];

interface Props {
  role: string;
}

export function AdminSidebar({ role }: Props) {
  const pathname = usePathname();

  const visibleItems = navItems.filter((item) => item.roles.includes(role));

  return (
    <aside className="fixed inset-y-0 left-0 w-64 bg-gray-900 text-white hidden lg:block">
      {/* Logo */}
      <div className="h-16 flex items-center px-6 border-b border-gray-800">
        <Link href="/admin" className="text-xl font-bold">
          OKLA Admin
        </Link>
      </div>

      {/* Navigation */}
      <nav className="p-4 space-y-1">
        {visibleItems.map((item) => {
          const isActive = pathname === item.href || pathname.startsWith(`${item.href}/`);

          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "flex items-center gap-3 px-4 py-3 rounded-lg transition-colors",
                isActive
                  ? "bg-primary-600 text-white"
                  : "text-gray-400 hover:text-white hover:bg-gray-800"
              )}
            >
              <item.icon size={20} />
              <span>{item.label}</span>
            </Link>
          );
        })}
      </nav>
    </aside>
  );
}
```

---

## 🔧 PASO 3: Dashboard Page

```typescript
// filepath: src/app/(admin)/admin/page.tsx
import { Suspense } from "react";
import { Metadata } from "next";
import { AdminKPIs } from "@/components/admin/dashboard/AdminKPIs";
import { RevenueChart } from "@/components/admin/dashboard/RevenueChart";
import { UsersChart } from "@/components/admin/dashboard/UsersChart";
import { SystemAlerts } from "@/components/admin/dashboard/SystemAlerts";
import { RecentActivity } from "@/components/admin/dashboard/RecentActivity";
import { LoadingCard } from "@/components/ui/LoadingCard";

export const metadata: Metadata = {
  title: "Dashboard | Admin OKLA",
};

export default function AdminDashboardPage() {
  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>

      {/* KPIs */}
      <Suspense fallback={<LoadingCard className="h-32" />}>
        <AdminKPIs />
      </Suspense>

      {/* Charts */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Suspense fallback={<LoadingCard className="h-80" />}>
          <RevenueChart />
        </Suspense>
        <Suspense fallback={<LoadingCard className="h-80" />}>
          <UsersChart />
        </Suspense>
      </div>

      {/* Alerts & Activity */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Suspense fallback={<LoadingCard className="h-64" />}>
          <SystemAlerts />
        </Suspense>
        <Suspense fallback={<LoadingCard className="h-64" />}>
          <RecentActivity />
        </Suspense>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 4: AdminKPIs

```typescript
// filepath: src/components/admin/dashboard/AdminKPIs.tsx
import { Users, Car, Building2, DollarSign, TrendingUp, TrendingDown } from "lucide-react";
import { adminService } from "@/lib/services/adminService";
import { formatNumber, formatCurrency } from "@/lib/utils";

export async function AdminKPIs() {
  const stats = await adminService.getPlatformStats();

  const kpis = [
    {
      label: "Usuarios totales",
      value: formatNumber(stats.totalUsers),
      change: stats.usersChange,
      icon: Users,
      color: "bg-blue-500",
    },
    {
      label: "Vehículos activos",
      value: formatNumber(stats.activeListings),
      change: stats.listingsChange,
      icon: Car,
      color: "bg-green-500",
    },
    {
      label: "Dealers verificados",
      value: formatNumber(stats.verifiedDealers),
      change: stats.dealersChange,
      icon: Building2,
      color: "bg-purple-500",
    },
    {
      label: "MRR",
      value: formatCurrency(stats.mrr),
      change: stats.mrrChange,
      icon: DollarSign,
      color: "bg-amber-500",
    },
  ];

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      {kpis.map((kpi) => (
        <div key={kpi.label} className="bg-white rounded-xl border p-6">
          <div className="flex items-center justify-between mb-4">
            <div className={`w-10 h-10 rounded-lg ${kpi.color} flex items-center justify-center`}>
              <kpi.icon size={20} className="text-white" />
            </div>
            {kpi.change !== undefined && (
              <span
                className={`flex items-center text-sm font-medium ${
                  kpi.change >= 0 ? "text-green-600" : "text-red-600"
                }`}
              >
                {kpi.change >= 0 ? <TrendingUp size={14} /> : <TrendingDown size={14} />}
                {Math.abs(kpi.change)}%
              </span>
            )}
          </div>
          <p className="text-2xl font-bold text-gray-900">{kpi.value}</p>
          <p className="text-sm text-gray-500">{kpi.label}</p>
        </div>
      ))}
    </div>
  );
}
```

---

## 🔧 PASO 5: SystemAlerts

```typescript
// filepath: src/components/admin/dashboard/SystemAlerts.tsx
import Link from "next/link";
import { AlertTriangle, AlertCircle, Info, ChevronRight } from "lucide-react";
import { adminService } from "@/lib/services/adminService";
import { formatRelativeDate } from "@/lib/utils";

const severityConfig = {
  critical: { icon: AlertTriangle, color: "text-red-600 bg-red-50" },
  warning: { icon: AlertCircle, color: "text-amber-600 bg-amber-50" },
  info: { icon: Info, color: "text-blue-600 bg-blue-50" },
};

export async function SystemAlerts() {
  const alerts = await adminService.getSystemAlerts();

  return (
    <div className="bg-white rounded-xl border">
      <div className="p-4 border-b flex items-center justify-between">
        <h2 className="font-semibold text-gray-900">Alertas del sistema</h2>
        <Link href="/admin/sistema/alertas" className="text-sm text-primary-600 hover:underline">
          Ver todas
        </Link>
      </div>

      <div className="divide-y">
        {alerts.slice(0, 5).map((alert) => {
          const config = severityConfig[alert.severity as keyof typeof severityConfig];
          const Icon = config.icon;

          return (
            <div key={alert.id} className="p-4 flex items-start gap-3">
              <div className={`p-2 rounded-lg ${config.color}`}>
                <Icon size={16} />
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-gray-900">{alert.title}</p>
                <p className="text-xs text-gray-500 mt-0.5">
                  {formatRelativeDate(alert.createdAt)}
                </p>
              </div>
              <Link href={`/admin/sistema/alertas/${alert.id}`}>
                <ChevronRight size={16} className="text-gray-400" />
              </Link>
            </div>
          );
        })}

        {alerts.length === 0 && (
          <div className="p-8 text-center text-gray-500">
            No hay alertas activas
          </div>
        )}
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 6: RevenueChart

```typescript
// filepath: src/components/admin/dashboard/RevenueChart.tsx
"use client";

import { Line } from "react-chartjs-2";
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler,
} from "chart.js";
import { adminService } from "@/lib/services/adminService";
import { useSuspenseQuery } from "@tanstack/react-query";

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler
);

export function RevenueChart() {
  const { data } = useSuspenseQuery({
    queryKey: ["admin", "revenue-chart"],
    queryFn: () => adminService.getRevenueChart(),
  });

  const chartData = {
    labels: data.labels,
    datasets: [
      {
        label: "Ingresos",
        data: data.values,
        borderColor: "rgb(59, 130, 246)",
        backgroundColor: "rgba(59, 130, 246, 0.1)",
        fill: true,
        tension: 0.4,
      },
    ],
  };

  const options = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        display: false,
      },
      title: {
        display: false,
      },
    },
    scales: {
      y: {
        beginAtZero: true,
        ticks: {
          callback: (value: any) => `$${value.toLocaleString()}`,
        },
      },
    },
  };

  return (
    <div className="bg-white rounded-xl border p-6">
      <h2 className="font-semibold text-gray-900 mb-4">Ingresos (últimos 30 días)</h2>
      <div className="h-64">
        <Line data={chartData} options={options} />
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 7: UsersChart

```typescript
// filepath: src/components/admin/dashboard/UsersChart.tsx
"use client";

import { Bar } from "react-chartjs-2";
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  BarElement,
  Title,
  Tooltip,
  Legend,
} from "chart.js";
import { adminService } from "@/lib/services/adminService";
import { useSuspenseQuery } from "@tanstack/react-query";

ChartJS.register(CategoryScale, LinearScale, BarElement, Title, Tooltip, Legend);

export function UsersChart() {
  const { data } = useSuspenseQuery({
    queryKey: ["admin", "users-chart"],
    queryFn: () => adminService.getUsersChart(),
  });

  const chartData = {
    labels: ["Buyers", "Sellers", "Dealers", "Admins"],
    datasets: [
      {
        label: "Usuarios activos",
        data: [
          data.buyers,
          data.sellers,
          data.dealers,
          data.admins,
        ],
        backgroundColor: [
          "rgba(59, 130, 246, 0.8)",
          "rgba(34, 197, 94, 0.8)",
          "rgba(168, 85, 247, 0.8)",
          "rgba(251, 146, 60, 0.8)",
        ],
      },
    ],
  };

  const options = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        display: false,
      },
    },
  };

  return (
    <div className="bg-white rounded-xl border p-6">
      <h2 className="font-semibold text-gray-900 mb-4">Usuarios por tipo</h2>
      <div className="h-64">
        <Bar data={chartData} options={options} />
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 8: RecentActivity

```typescript
// filepath: src/components/admin/dashboard/RecentActivity.tsx
import Link from "next/link";
import { User, Car, DollarSign, AlertTriangle } from "lucide-react";
import { adminService } from "@/lib/services/adminService";
import { formatRelativeDate } from "@/lib/utils";

const activityIcons = {
  user: User,
  vehicle: Car,
  payment: DollarSign,
  moderation: AlertTriangle,
};

const activityColors = {
  user: "bg-blue-100 text-blue-600",
  vehicle: "bg-green-100 text-green-600",
  payment: "bg-amber-100 text-amber-600",
  moderation: "bg-red-100 text-red-600",
};

export async function RecentActivity() {
  const activities = await adminService.getRecentActivity();

  return (
    <div className="bg-white rounded-xl border">
      <div className="p-4 border-b flex items-center justify-between">
        <h2 className="font-semibold text-gray-900">Actividad reciente</h2>
        <Link href="/admin/activity" className="text-sm text-primary-600 hover:underline">
          Ver todas
        </Link>
      </div>

      <div className="divide-y">
        {activities.slice(0, 10).map((activity) => {
          const Icon = activityIcons[activity.type as keyof typeof activityIcons];
          const colorClass = activityColors[activity.type as keyof typeof activityColors];

          return (
            <div key={activity.id} className="p-4 flex items-start gap-3">
              <div className={`p-2 rounded-lg ${colorClass}`}>
                <Icon size={16} />
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm text-gray-900">{activity.description}</p>
                <div className="flex items-center gap-2 mt-1">
                  <span className="text-xs text-gray-500">
                    {formatRelativeDate(activity.createdAt)}
                  </span>
                  {activity.userName && (
                    <span className="text-xs text-gray-400">
                      por {activity.userName}
                    </span>
                  )}
                </div>
              </div>
            </div>
          );
        })}

        {activities.length === 0 && (
          <div className="p-8 text-center text-gray-500">
            No hay actividad reciente
          </div>
        )}
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 9: Quick Actions

```typescript
// filepath: src/components/admin/dashboard/QuickActions.tsx
import Link from "next/link";
import { Users, Car, Building2, FileCheck, AlertTriangle, Settings } from "lucide-react";

const actions = [
  {
    icon: Users,
    title: "Nuevo Usuario",
    description: "Crear cuenta manualmente",
    href: "/admin/usuarios/nuevo",
    color: "bg-blue-500",
  },
  {
    icon: Car,
    title: "Revisar Vehículos",
    description: "Aprobar publicaciones",
    href: "/admin/vehiculos?status=pending",
    color: "bg-green-500",
  },
  {
    icon: Building2,
    title: "Verificar Dealer",
    description: "Documentos pendientes",
    href: "/admin/dealers?verification=pending",
    color: "bg-purple-500",
  },
  {
    icon: FileCheck,
    title: "Compliance",
    description: "Revisar reportes",
    href: "/admin/compliance",
    color: "bg-amber-500",
  },
  {
    icon: AlertTriangle,
    title: "Moderación",
    description: "Contenido reportado",
    href: "/admin/moderacion",
    color: "bg-red-500",
  },
  {
    icon: Settings,
    title: "Configuración",
    description: "Ajustes del sistema",
    href: "/admin/sistema",
    color: "bg-gray-500",
  },
];

export function QuickActions() {
  return (
    <div className="bg-white rounded-xl border p-6">
      <h2 className="font-semibold text-gray-900 mb-4">Acciones rápidas</h2>
      <div className="grid grid-cols-2 gap-3">
        {actions.map((action) => (
          <Link
            key={action.href}
            href={action.href}
            className="flex items-start gap-3 p-4 rounded-lg border hover:border-primary-300 hover:bg-primary-50 transition-colors"
          >
            <div className={`p-2 rounded-lg ${action.color}`}>
              <action.icon size={16} className="text-white" />
            </div>
            <div>
              <p className="font-medium text-gray-900 text-sm">{action.title}</p>
              <p className="text-xs text-gray-500 mt-0.5">{action.description}</p>
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 10: PendingReviews Table

```typescript
// filepath: src/components/admin/dashboard/PendingReviews.tsx
import Link from "next/link";
import { Eye, Check, X } from "lucide-react";
import { adminService } from "@/lib/services/adminService";
import { formatRelativeDate } from "@/lib/utils";
import { Badge } from "@/components/ui/Badge";

export async function PendingReviews() {
  const reviews = await adminService.getPendingReviews();

  return (
    <div className="bg-white rounded-xl border">
      <div className="p-4 border-b flex items-center justify-between">
        <h2 className="font-semibold text-gray-900">Pendientes de revisión</h2>
        <Badge variant="warning">{reviews.length}</Badge>
      </div>

      <div className="overflow-x-auto">
        <table className="w-full">
          <thead className="bg-gray-50 border-b">
            <tr>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                Tipo
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                Contenido
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                Usuario
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                Fecha
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                Acciones
              </th>
            </tr>
          </thead>
          <tbody className="divide-y">
            {reviews.map((review) => (
              <tr key={review.id} className="hover:bg-gray-50">
                <td className="px-4 py-3">
                  <Badge variant={review.type === "vehicle" ? "blue" : "purple"}>
                    {review.type}
                  </Badge>
                </td>
                <td className="px-4 py-3">
                  <p className="text-sm font-medium text-gray-900 truncate max-w-xs">
                    {review.title}
                  </p>
                </td>
                <td className="px-4 py-3">
                  <p className="text-sm text-gray-600">{review.userName}</p>
                </td>
                <td className="px-4 py-3">
                  <p className="text-sm text-gray-500">
                    {formatRelativeDate(review.createdAt)}
                  </p>
                </td>
                <td className="px-4 py-3">
                  <div className="flex items-center gap-2">
                    <Link
                      href={`/admin/${review.type}s/${review.id}`}
                      className="p-1 hover:bg-blue-50 rounded"
                    >
                      <Eye size={16} className="text-blue-600" />
                    </Link>
                    <button className="p-1 hover:bg-green-50 rounded">
                      <Check size={16} className="text-green-600" />
                    </button>
                    <button className="p-1 hover:bg-red-50 rounded">
                      <X size={16} className="text-red-600" />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>

        {reviews.length === 0 && (
          <div className="p-8 text-center text-gray-500">
            No hay elementos pendientes de revisión
          </div>
        )}
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 11: AdminHeader

```typescript
// filepath: src/components/admin/AdminHeader.tsx
"use client";

import { Bell, Search, Menu } from "lucide-react";
import { UserDropdown } from "@/components/layout/UserDropdown";
import { useState } from "react";

interface AdminHeaderProps {
  user: {
    name: string;
    email: string;
    image?: string;
    role: string;
  };
}

export function AdminHeader({ user }: AdminHeaderProps) {
  const [showSearch, setShowSearch] = useState(false);

  return (
    <header className="h-16 border-b bg-white flex items-center justify-between px-6">
      {/* Mobile Menu Button */}
      <button className="lg:hidden">
        <Menu size={24} />
      </button>

      {/* Search */}
      <div className="flex-1 max-w-2xl">
        {showSearch ? (
          <div className="relative">
            <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
            <input
              type="text"
              placeholder="Buscar usuarios, vehículos, dealers..."
              className="w-full pl-9 pr-4 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500"
              autoFocus
              onBlur={() => setShowSearch(false)}
            />
          </div>
        ) : (
          <button
            onClick={() => setShowSearch(true)}
            className="flex items-center gap-2 text-gray-500 hover:text-gray-700"
          >
            <Search size={20} />
            <span className="text-sm">Buscar...</span>
          </button>
        )}
      </div>

      {/* Right Actions */}
      <div className="flex items-center gap-4">
        <button className="relative p-2 hover:bg-gray-100 rounded-lg">
          <Bell size={20} className="text-gray-600" />
          <span className="absolute top-1 right-1 w-2 h-2 bg-red-500 rounded-full" />
        </button>

        <UserDropdown user={user} />
      </div>
    </header>
  );
}
```

---

## 🔧 PASO 12: Admin Service

```typescript
// filepath: src/lib/services/adminService.ts
import { api } from "@/lib/api";

class AdminService {
  private baseUrl = "/api/admin";

  // Platform Stats
  async getPlatformStats() {
    const { data } = await api.get(`${this.baseUrl}/stats`);
    return data;
  }

  // Charts
  async getRevenueChart() {
    const { data } = await api.get(`${this.baseUrl}/charts/revenue`);
    return data;
  }

  async getUsersChart() {
    const { data } = await api.get(`${this.baseUrl}/charts/users`);
    return data;
  }

  // Activity
  async getRecentActivity(limit = 20) {
    const { data } = await api.get(`${this.baseUrl}/activity`, {
      params: { limit },
    });
    return data;
  }

  // Alerts
  async getSystemAlerts() {
    const { data } = await api.get(`${this.baseUrl}/alerts`);
    return data;
  }

  async dismissAlert(alertId: string) {
    await api.delete(`${this.baseUrl}/alerts/${alertId}`);
  }

  // Pending Reviews
  async getPendingReviews() {
    const { data } = await api.get(`${this.baseUrl}/reviews/pending`);
    return data;
  }

  async approveReview(type: string, id: string) {
    const { data } = await api.post(`${this.baseUrl}/reviews/approve`, {
      type,
      id,
    });
    return data;
  }

  async rejectReview(type: string, id: string, reason: string) {
    const { data } = await api.post(`${this.baseUrl}/reviews/reject`, {
      type,
      id,
      reason,
    });
    return data;
  }

  // Users Management
  async getUsers(filters?: any) {
    const { data } = await api.get(`${this.baseUrl}/users`, {
      params: filters,
    });
    return data;
  }

  async getUserById(id: string) {
    const { data } = await api.get(`${this.baseUrl}/users/${id}`);
    return data;
  }

  async updateUserRole(userId: string, role: string) {
    const { data } = await api.patch(`${this.baseUrl}/users/${userId}/role`, {
      role,
    });
    return data;
  }

  async suspendUser(userId: string, reason: string) {
    const { data } = await api.post(`${this.baseUrl}/users/${userId}/suspend`, {
      reason,
    });
    return data;
  }

  async unsuspendUser(userId: string) {
    const { data } = await api.post(
      `${this.baseUrl}/users/${userId}/unsuspend`,
    );
    return data;
  }

  // Vehicles Management
  async getVehicles(filters?: any) {
    const { data } = await api.get(`${this.baseUrl}/vehicles`, {
      params: filters,
    });
    return data;
  }

  async approveVehicle(vehicleId: string) {
    const { data } = await api.post(
      `${this.baseUrl}/vehicles/${vehicleId}/approve`,
    );
    return data;
  }

  async rejectVehicle(vehicleId: string, reason: string) {
    const { data } = await api.post(
      `${this.baseUrl}/vehicles/${vehicleId}/reject`,
      { reason },
    );
    return data;
  }

  async featureVehicle(vehicleId: string, featured: boolean) {
    const { data } = await api.patch(`${this.baseUrl}/vehicles/${vehicleId}`, {
      featured,
    });
    return data;
  }

  // Dealers Management
  async getDealers(filters?: any) {
    const { data } = await api.get(`${this.baseUrl}/dealers`, {
      params: filters,
    });
    return data;
  }

  async verifyDealer(dealerId: string) {
    const { data } = await api.post(
      `${this.baseUrl}/dealers/${dealerId}/verify`,
    );
    return data;
  }

  async rejectDealerVerification(dealerId: string, reason: string) {
    const { data } = await api.post(
      `${this.baseUrl}/dealers/${dealerId}/reject`,
      { reason },
    );
    return data;
  }

  // System Settings
  async getSettings() {
    const { data } = await api.get(`${this.baseUrl}/settings`);
    return data;
  }

  async updateSetting(key: string, value: any) {
    const { data } = await api.patch(`${this.baseUrl}/settings/${key}`, {
      value,
    });
    return data;
  }
}

export const adminService = new AdminService();
```

---

## ✅ VALIDACIÓN

```bash
pnpm dev
# Verificar:
# - /admin muestra dashboard completo
# - KPIs cargan correctamente
# - Gráficos de Revenue y Users renderizan
# - Sidebar muestra items según rol
# - Alertas del sistema se muestran
# - Actividad reciente se actualiza
# - Quick actions funcionan
# - Tabla de pending reviews carga
# - Search header funciona
# - Notificaciones muestran badge
```

---

## 🔌 BACKEND API EXTENDIDA - ReportsService (Puerto 5095)

### Endpoints de Reportes

```typescript
// REPORTES CRUD
POST   /api/reports                  # Crear definición de reporte (REPORT-001)
GET    /api/reports/{id}             # Obtener reporte por ID
PUT    /api/reports/{id}             # Actualizar reporte
DELETE /api/reports/{id}             # Eliminar reporte
POST   /api/reports/{id}/generate    # Iniciar generación (REPORT-002)
GET    /api/reports/ready            # Listar reportes listos (REPORT-003)

// PROGRAMACIÓN DE REPORTES
POST   /api/reportschedules          # Programar reporte automático (SCHEDULE-001)
GET    /api/reportschedules          # Listar programaciones
PUT    /api/reportschedules/{id}     # Actualizar programación
DELETE /api/reportschedules/{id}     # Eliminar programación
POST   /api/reportschedules/{id}/run # Ejecutar ahora

// DASHBOARDS
POST   /api/dashboards               # Crear dashboard (DASHBOARD-001)
GET    /api/dashboards               # Listar dashboards del dealer
GET    /api/dashboards/{id}          # Obtener dashboard con widgets
PUT    /api/dashboards/{id}          # Actualizar dashboard
DELETE /api/dashboards/{id}          # Eliminar dashboard
POST   /api/dashboards/{id}/widgets  # Agregar widget (DASHBOARD-002)
PUT    /api/dashboards/widgets/{id}  # Actualizar widget
DELETE /api/dashboards/widgets/{id}  # Eliminar widget
```

### Entidades del Backend

```csharp
// Report Entity
public class Report : ITenantEntity
{
    public Guid Id { get; private set; }
    public Guid DealerId { get; set; }

    // Metadatos
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public ReportType Type { get; private set; }
    public ReportFormat Format { get; private set; }
    public ReportStatus Status { get; private set; }

    // Configuración
    public DateTime? StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public string? QueryDefinition { get; private set; }  // JSON
    public string? FilterCriteria { get; private set; }   // JSON
    public string? Parameters { get; private set; }       // JSON

    // Resultado
    public string? FilePath { get; private set; }
    public long? FileSize { get; private set; }
    public DateTime? GeneratedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    // Auditoría
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
}

// Enums
public enum ReportType { Sales, Inventory, Financial, CRM, Marketing, Analytics, Custom }
public enum ReportFormat { Pdf, Excel, Csv, Html, Json }
public enum ReportStatus { Draft, Ready, Generating, Completed, Failed, Expired, Cancelled }
public enum ScheduleFrequency { Daily, Weekly, BiWeekly, Monthly, Quarterly, Custom }
public enum DashboardType { Executive, Operations, Sales, Finance, Compliance, Dealer, Custom }
public enum WidgetType { Counter, BarChart, LineChart, PieChart, Table, KPI, Gauge, Timeline }
```

---

## 🔌 BACKEND API - DashboardsService (Puerto 5020)

### Proceso DASH-001: Cargar Dashboard Ejecutivo

| Campo       | Valor                         |
| ----------- | ----------------------------- |
| **ID**      | DASH-001                      |
| **Nombre**  | Cargar Dashboard Ejecutivo    |
| **Actor**   | Admin C-Level                 |
| **Trigger** | GET /api/dashboards/executive |

**Flujo del Proceso:**

| Paso | Acción                 | Sistema       | Validación        |
| ---- | ---------------------- | ------------- | ----------------- |
| 1    | Usuario abre dashboard | Frontend      | C-Level auth      |
| 2    | Request GET /executive | Frontend      | Auth header       |
| 3    | Verificar permisos     | DashboardsAPI | Role check        |
| 4    | Obtener config usuario | DashboardsAPI | User config       |
| 5    | Para cada widget:      | DashboardsAPI | Parallel          |
| 6    | - Check Redis cache    | Redis         | Cache hit?        |
| 7    | - Si miss: calcular    | DashboardsAPI | Aggregation       |
| 8    | - Guardar en cache     | Redis         | TTL by widget     |
| 9    | Ensamblar response     | DashboardsAPI | All widgets       |
| 10   | Iniciar WebSocket      | Frontend      | Real-time updates |

### Catálogo de Widgets

**KPIs:**

| Widget ID             | Nombre             | Descripción    |
| --------------------- | ------------------ | -------------- |
| `kpi-revenue-total`   | Ingresos totales   | MRR/ARR        |
| `kpi-active-users`    | Usuarios activos   | DAU/MAU        |
| `kpi-active-listings` | Listings activos   | Count + change |
| `kpi-conversion-rate` | Tasa de conversión | % + trend      |
| `kpi-avg-dom`         | Días en mercado    | Avg days       |
| `kpi-open-tickets`    | Tickets abiertos   | Count          |

**Charts:**

| Widget ID               | Tipo   | Descripción                |
| ----------------------- | ------ | -------------------------- |
| `chart-revenue-trend`   | Line   | Tendencia de ingresos      |
| `chart-users-growth`    | Area   | Crecimiento de usuarios    |
| `chart-listings-funnel` | Funnel | Funnel de publicaciones    |
| `chart-revenue-sources` | Pie    | Distribución de ingresos   |
| `chart-geographic-dist` | Map    | Distribución geográfica    |
| `chart-category-dist`   | Bar    | Distribución por categoría |

---

## 🔌 BACKEND API - EventTrackingService (Puerto 5050)

> **Nota:** Este es un servicio backend-only. No tiene UI propia pero alimenta todos los dashboards.

### Procesos de Tracking

**EVT-001: Ingestión de Eventos**

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

**Categorías de Eventos:**

| Categoría  | Eventos                                                |
| ---------- | ------------------------------------------------------ |
| Navigation | page_view, page_leave, session_start, session_end      |
| Vehicles   | vehicle_view, vehicle_share, add_to_favorites, etc.    |
| Search     | search, filter_apply, sort_apply                       |
| Lead       | lead_form_submit, phone_click, whatsapp_click          |
| User       | user_signup, user_login, profile_update                |
| Dealer     | dealer_profile_view, inventory_update, listing_publish |

### SDK JavaScript para Tracking

```javascript
// Instalación del SDK en el Frontend
<script>
  !function(){
    var okla=window.okla=window.okla||[];
    okla.methods=["page","track","identify","reset","group"];
    okla.factory=function(t){
      return function(){
        var e=Array.prototype.slice.call(arguments);
        e.unshift(t);
        okla.push(e);
        return okla
      }
    };
    for(var t=0;t<okla.methods.length;t++){
      var e=okla.methods[t];
      okla[e]=okla.factory(e)
    }
    okla.load=function(t){
      var e=document.createElement("script");
      e.type="text/javascript";
      e.async=!0;
      e.src="https://cdn.okla.com.do/analytics.min.js";
      var n=document.getElementsByTagName("script")[0];
      n.parentNode.insertBefore(e,n);
      okla._writeKey=t
    };
    okla.load("YOUR_WRITE_KEY");
    okla.page();
  }();
</script>

// Uso del SDK
okla.track('vehicle_view', {
  vehicleId: 'abc123',
  source: 'search_results',
  position: 3
});

okla.identify('user-123', {
  email: 'juan@email.com',
  name: 'Juan Pérez',
  accountType: 'Buyer'
});
```

---

## 🔌 BACKEND API - ComplianceService (Puerto 5027)

### Proceso REG-001: Escanear Fuente Regulatoria

| Campo       | Valor                      |
| ----------- | -------------------------- |
| **ID**      | REG-001                    |
| **Nombre**  | Scan Regulatory Source     |
| **Actor**   | Sistema (Hangfire job)     |
| **Trigger** | Scheduled (diario/semanal) |

**Flujo del Proceso:**

| Paso | Acción                       | Sistema             | Validación        |
| ---- | ---------------------------- | ------------------- | ----------------- |
| 1    | Job scheduled se activa      | Hangfire            | Source.NextScanAt |
| 2    | Determinar tipo de fuente    | Crawler             | SourceType        |
| 3    | Ejecutar scraping/RSS        | Crawler             | Response OK       |
| 4    | Parsear documentos nuevos    | Crawler             | NewDocs found     |
| 5    | Por cada documento nuevo:    | Processor           | -                 |
| 6    | - Extraer texto              | Processor           | Text extracted    |
| 7    | - Buscar keywords            | Processor           | Keywords matched  |
| 8    | - Calcular impact score      | Processor           | Score 1-100       |
| 9    | - Clasificar severidad       | Processor           | Severity assigned |
| 10   | Crear alerta si score >= 30  | AlertEngine         | Alert saved       |
| 11   | Notificar si severity >= Med | NotificationService | Notification sent |
| 12   | Actualizar LastScannedAt     | ComplianceService   | Source updated    |

**Fuentes Regulatorias Monitoreadas:**

| Fuente         | Tipo       | Frecuencia | Categoría Default    |
| -------------- | ---------- | ---------- | -------------------- |
| DGII           | RssFeed    | Diario     | Tax                  |
| Pro Consumidor | WebScrape  | Diario     | ConsumerProtection   |
| SB (Bancos)    | ApiPolling | Semanal    | Finance              |
| DGCP           | RssFeed    | Semanal    | BusinessRegistration |
| INDOTEL        | WebScrape  | Semanal    | DataPrivacy          |

**Severidad de Alertas:**

| Severidad | Impact Score | Deadline (horas) | Notificación        |
| --------- | ------------ | ---------------- | ------------------- |
| Critical  | >= 80        | 24               | Email + SMS + Teams |
| High      | >= 60        | 72               | Email + Teams       |
| Medium    | >= 40        | 168              | Email               |
| Low       | >= 30        | 336              | In-app only         |

---

## 📊 PROCESOS DE REPORTES DETALLADOS

### REPORT-001: Crear Definición de Reporte

| Campo          | Valor           |
| -------------- | --------------- |
| **ID**         | REPORT-001      |
| **Nombre**     | Crear Reporte   |
| **Actor**      | Gerente / Admin |
| **Criticidad** | 🟡 MEDIO        |

**Request Body:**

```json
{
  "name": "Reporte de Ventas Mensual",
  "description": "Resumen de ventas del mes con comparativo",
  "type": "Sales",
  "format": "Pdf",
  "startDate": "2026-01-01T00:00:00Z",
  "endDate": "2026-01-31T23:59:59Z",
  "queryDefinition": "{\"metrics\": [\"totalSales\", \"avgPrice\", \"conversionRate\"]}",
  "filterCriteria": "{\"vehicleType\": \"SUV\"}"
}
```

**Flujo Paso a Paso:**

| Paso | Acción                  | Componente           | Descripción             |
| ---- | ----------------------- | -------------------- | ----------------------- |
| 1    | Recibir request         | ReportsController    | POST /api/reports       |
| 2    | Validar ReportType      | Handler              | Enum válido             |
| 3    | Validar ReportFormat    | Handler              | Enum válido             |
| 4    | Crear entidad Report    | Constructor          | Status = Draft          |
| 5    | Configurar rango fechas | SetDateRange()       | Si aplica               |
| 6    | Configurar query        | SetQueryDefinition() | JSON de métricas        |
| 7    | Configurar filtros      | SetFilter()          | JSON de filtros         |
| 8    | Persistir               | ReportRepository     | INSERT                  |
| 9    | Responder               | Controller           | 201 Created + ReportDto |

### REPORT-002: Generar Reporte

| Campo          | Valor                         |
| -------------- | ----------------------------- |
| **ID**         | REPORT-002                    |
| **Nombre**     | Iniciar Generación de Reporte |
| **Criticidad** | 🔴 CRÍTICO                    |

**Job de Generación (Background):**

| Paso | Acción          | Componente      | Descripción                          |
| ---- | --------------- | --------------- | ------------------------------------ |
| 1    | Consultar datos | DataService     | Según QueryDefinition                |
| 2    | Aplicar filtros | FilterEngine    | Según FilterCriteria                 |
| 3    | Generar archivo | ReportGenerator | QuestPDF / ClosedXML                 |
| 4    | Subir a storage | MediaService    | S3 compatible                        |
| 5    | Si éxito        | Complete()      | FilePath, FileSize, ExpiresAt        |
| 6    | Si error        | Fail()          | ErrorMessage                         |
| 7    | Publicar evento | RabbitMQ        | `report.generated` o `report.failed` |

### SCHEDULE-001: Crear Programación de Reporte

| Campo          | Valor                        |
| -------------- | ---------------------------- |
| **ID**         | SCHEDULE-001                 |
| **Nombre**     | Programar Reporte Automático |
| **Criticidad** | 🟡 MEDIO                     |

**Request Body:**

```json
{
  "reportId": "5fa85f64-5717-4562-b3fc-2c963f66afa8",
  "name": "Reporte Semanal de Ventas",
  "frequency": "Weekly",
  "executionTime": "08:00",
  "dayOfWeek": "Monday",
  "recipients": "[\"gerente@dealer.com\"]",
  "sendEmail": true,
  "saveToStorage": true
}
```

---

## 📊 REGLAS DE NEGOCIO

### Límites por Plan (Reportes)

| Plan       | Reportes/Mes | Programados | Dashboards | Widgets   |
| ---------- | ------------ | ----------- | ---------- | --------- |
| Starter    | 10           | 1           | 1          | 4         |
| Pro        | 100          | 10          | 5          | 20        |
| Enterprise | Ilimitado    | Ilimitado   | Ilimitado  | Ilimitado |

### Cache TTL por Widget

| Tipo Widget | TTL Cache |
| ----------- | --------- |
| KPIs        | 5 minutos |
| Trends      | 1 hora    |
| Tables      | 1 minuto  |

---

## 📡 EVENTOS RABBITMQ

| Evento                        | Exchange         | Descripción              |
| ----------------------------- | ---------------- | ------------------------ |
| `report.created`              | `reports.events` | Reporte definido         |
| `report.generation.started`   | `reports.events` | Generación iniciada      |
| `report.generated`            | `reports.events` | Reporte listo            |
| `report.failed`               | `reports.events` | Generación fallida       |
| `schedule.executed`           | `reports.events` | Programación ejecutada   |
| `event.received`              | `events`         | Evento de tracking       |
| `RegulatoryAlertCreatedEvent` | `compliance`     | Nueva alerta regulatoria |

---

## 📊 MÉTRICAS PROMETHEUS

```
# Reports
reports_generated_total{type, format, status}
report_generation_duration_seconds{type}
report_file_size_bytes{type}
report_schedules_active_count

# Dashboards
dashboard_requests_total{type, user_role}
dashboard_load_time_ms{type}
dashboard_cache_hit_rate{widget}
dashboard_websocket_connections{type}

# Events
events_received_total{type}
events_processed_total
events_failed_total{reason}

# Regulatory
regulatory_alerts_total{category, severity}
regulatory_source_scan_duration_seconds{source}
```

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/admin-dashboard.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsAdmin } from "../helpers/auth";

test.describe("Admin Dashboard", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("debe mostrar dashboard principal", async ({ page }) => {
    await page.goto("/admin");

    await expect(
      page.getByRole("heading", { name: /dashboard/i }),
    ).toBeVisible();
    await expect(page.getByTestId("admin-metrics")).toBeVisible();
  });

  test("debe mostrar métricas clave", async ({ page }) => {
    await page.goto("/admin");

    await expect(page.getByTestId("metric-users")).toBeVisible();
    await expect(page.getByTestId("metric-listings")).toBeVisible();
    await expect(page.getByTestId("metric-revenue")).toBeVisible();
  });

  test("debe mostrar alertas regulatorias", async ({ page }) => {
    await page.goto("/admin");

    await expect(page.getByTestId("regulatory-alerts")).toBeVisible();
  });

  test("debe navegar a sección de usuarios", async ({ page }) => {
    await page.goto("/admin");

    await page.getByRole("link", { name: /usuarios/i }).click();
    await expect(page).toHaveURL(/\/admin\/users/);
  });

  test("debe cambiar rango de fechas en métricas", async ({ page }) => {
    await page.goto("/admin");

    await page.getByRole("combobox", { name: /periodo/i }).click();
    await page.getByRole("option", { name: /últimos 30 días/i }).click();

    await expect(page).toHaveURL(/period=30d/);
  });
});
```

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/04-PAGINAS/13-admin-users.md`
