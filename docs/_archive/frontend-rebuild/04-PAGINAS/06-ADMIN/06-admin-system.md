---
title: "Admin - Sistema"
priority: P2
estimated_time: "50 minutos"
dependencies: []
apis: []
status: partial
last_updated: "2026-01-30"
---

# ⚙️ Admin - Sistema

> **Tiempo estimado:** 50 minutos
> **Prerrequisitos:** Admin layout, múltiples servicios backend
> **Roles:** ADM-SUPER
> **Dependencias:** MaintenanceService (5061), ErrorService (5008), AuditService (5091)

---

## ✅ INTEGRACIÓN CON SERVICIOS DE ADMINISTRACIÓN

Este documento complementa:

- [12-admin-dashboard.md](./01-admin-dashboard.md) - Dashboard principal
- [13-admin-users.md](./02-admin-users.md) - Gestión de usuarios admin
- [process-matrix/12-ADMINISTRACION/03-maintenance-mode.md](../../process-matrix/12-ADMINISTRACION/03-maintenance-mode.md) - **Mantenimiento** ⭐
- [process-matrix/12-ADMINISTRACION/04-feature-flags.md](../../process-matrix/12-ADMINISTRACION/04-feature-flags.md) - **Feature Flags** ⭐
- [process-matrix/12-ADMINISTRACION/05-error-service.md](../../process-matrix/12-ADMINISTRACION/05-error-service.md) - **Error Service** ⭐

**Estado:** ✅ Maintenance 100% | 🔴 Feature Flags 0% | ✅ Error Service 85%

### Servicios Involucrados

| Servicio           | Puerto | Función                   | Estado                     |
| ------------------ | ------ | ------------------------- | -------------------------- |
| MaintenanceService | 5061   | Modo mantenimiento        | ✅ 100% BE + 90% UI        |
| ErrorService       | 5008   | Centralización de errores | ✅ 85%                     |
| AuditService       | 5091   | Logs de auditoría         | 🟡 70%                     |
| Feature Flags      | TBD    | Feature toggles           | 🔴 0% (Planificado Fase 2) |

### MaintenanceService - Endpoints

| Método | Endpoint                      | Descripción             | Auth       |
| ------ | ----------------------------- | ----------------------- | ---------- |
| `GET`  | `/api/maintenance/current`    | Estado actual           | Public     |
| `GET`  | `/api/maintenance/upcoming`   | Próximos mantenimientos | Public     |
| `POST` | `/api/maintenance/schedule`   | Programar mantenimiento | Admin      |
| `POST` | `/api/maintenance/activate`   | Activar inmediatamente  | SuperAdmin |
| `POST` | `/api/maintenance/deactivate` | Desactivar              | SuperAdmin |
| `GET`  | `/api/maintenance/history`    | Historial               | Admin      |
| `POST` | `/api/maintenance/banners`    | Crear banner            | Admin      |

### MaintenanceService - Procesos

| Proceso   | Nombre                           | Pasos | Archivo                |
| --------- | -------------------------------- | ----- | ---------------------- |
| MAINT-001 | Programar Mantenimiento          | 9     | 03-maintenance-mode.md |
| MAINT-002 | Activar Mantenimiento Emergencia | 6     | 03-maintenance-mode.md |
| MAINT-003 | Crear Banner Informativo         | 7     | 03-maintenance-mode.md |
| MAINT-004 | Monitorear Progreso              | 5     | 03-maintenance-mode.md |

### MaintenanceService - Entidades

```csharp
// MaintenanceService/MaintenanceService.Domain/Entities/MaintenanceWindow.cs
public class MaintenanceWindow
{
    public Guid Id { get; set; }
    public MaintenanceType Type { get; set; } // Scheduled, Emergency, PartialOutage
    public MaintenanceStatus Status { get; set; } // Scheduled, Active, Completed
    public MaintenanceSeverity Severity { get; set; } // Info, Warning, Error, Critical

    public DateTime ScheduledStartTime { get; set; }
    public DateTime ScheduledEndTime { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }

    public List<string> AffectedServices { get; set; } = new();
    public List<string> BlockedRoutes { get; set; } = new();
    public bool IsFullMaintenance { get; set; }

    public string TitleEs { get; set; }
    public string MessageEs { get; set; }
    public string? InternalNotes { get; set; }
}

public class MaintenanceBanner
{
    public Guid Id { get; set; }
    public BannerType Type { get; set; } // TopBar, Modal, Toast, FullPage
    public MaintenanceSeverity Severity { get; set; }
    public string TitleEs { get; set; }
    public string MessageEs { get; set; }
    public bool IsDismissible { get; set; } = true;
    public bool ShowCountdown { get; set; }
    public DateTime? CountdownTarget { get; set; }
    public bool IsActive { get; set; } = true;
}
```

---

## 🎨 WIREFRAME - CONFIGURACIÓN DEL SISTEMA

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ ┌──────────┐                                                                │
│ │ SIDEBAR  │  CONFIGURACIÓN DEL SISTEMA                    [Guardar Todo]  │
│ │          │ ────────────────────────────────────────────────────────────── │
│ │ 📊 Dash  │                                                                │
│ │ 👥 Users │  ┌──────────────────────────────────────────────────────────┐ │
│ │ 🛡️ Mod   │  │ [General] [Mantenimiento] [Notificaciones] [Integraciones]│ │
│ │ ⚙️ Sys ◀ │  └──────────────────────────────────────────────────────────┘ │
│ │          │                                                                │
│ │ ──────── │  ┌──────────────────────────────────────────────────────────┐ │
│ │ 🔧 General│  │ MODO MANTENIMIENTO                                       │ │
│ │ 🚧 Maint │  │                                                          │ │
│ │ 📧 Notif │  │ Estado actual: 🟢 Activo (Sistema funcionando)           │ │
│ │ 🔌 APIs  │  │                                                          │ │
│ │ 🔐 Security│  │ ┌────────────────────────────────────────────────────┐   │ │
│ │ 📊 Logs  │  │ │ ⚡ ACTIVAR MANTENIMIENTO EMERGENCIA                 │   │ │
│ │          │  │ │    Activar inmediatamente (bloquea acceso usuarios) │   │ │
│ │          │  │ └────────────────────────────────────────────────────┘   │ │
│ │          │  │                                                          │ │
│ │          │  │ ┌────────────────────────────────────────────────────┐   │ │
│ │          │  │ │ 📅 PROGRAMAR MANTENIMIENTO                         │   │ │
│ │          │  │ │                                                    │   │ │
│ │          │  │ │ Tipo: [Programado ▼]                               │   │ │
│ │          │  │ │                                                    │   │ │
│ │          │  │ │ Inicio:  [2026-02-01] [02:00]                      │   │ │
│ │          │  │ │ Fin:     [2026-02-01] [06:00]                      │   │ │
│ │          │  │ │                                                    │   │ │
│ │          │  │ │ Mensaje:                                           │   │ │
│ │          │  │ │ ┌──────────────────────────────────────────────┐   │   │ │
│ │          │  │ │ │ Estaremos realizando mejoras en la plataforma│   │   │ │
│ │          │  │ │ └──────────────────────────────────────────────┘   │   │ │
│ │          │  │ │                                                    │   │ │
│ │          │  │ │ Servicios afectados:                               │   │ │
│ │          │  │ │ ☑️ Publicaciones  ☑️ Pagos  ☐ Búsqueda            │   │ │
│ │          │  │ │                                                    │   │ │
│ │          │  │ │                      [Programar Mantenimiento]     │   │ │
│ │          │  │ └────────────────────────────────────────────────────┘   │ │
│ │          │  └──────────────────────────────────────────────────────────┘ │
│ │          │                                                                │
│ │          │  ┌──────────────────────────────────────────────────────────┐ │
│ │          │  │ HISTORIAL DE MANTENIMIENTOS                              │ │
│ │          │  │                                                          │ │
│ │          │  │ 📅 Ene 15, 2026 02:00-04:00 • ✅ Completado • Full       │ │
│ │          │  │ 📅 Dic 20, 2025 03:00-05:00 • ✅ Completado • Parcial    │ │
│ │          │  │ 📅 Nov 10, 2025 01:00-02:00 • ✅ Completado • Emergencia │ │
│ │          │  └──────────────────────────────────────────────────────────┘ │
│ └──────────┘                                                                │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

### Feature Flags (Planificado)

**Estado:** 🔴 No implementado - Planificado para Fase 2

**Tipos de Feature Flags:**

| Tipo        | Descripción                       | Ejemplo                    |
| ----------- | --------------------------------- | -------------------------- |
| Release     | Nuevas features en desarrollo     | `enable_new_checkout`      |
| Experiment  | A/B testing                       | `ab_test_hero_design`      |
| Ops         | Control operacional               | `enable_maintenance_mode`  |
| Kill Switch | Desactivar features problemáticas | `disable_image_processing` |
| Permission  | Features por plan/rol             | `dealer_pro_analytics`     |

**Alternativas recomendadas:**

- LaunchDarkly (SaaS)
- Unleash (open-source)
- ConfigCat (SaaS)

### ErrorService - Endpoints

| Método | Endpoint                   | Descripción          | Auth   |
| ------ | -------------------------- | -------------------- | ------ |
| `POST` | `/api/errors`              | Reportar error       | Public |
| `GET`  | `/api/errors`              | Listar errores       | Admin  |
| `GET`  | `/api/errors/{id}`         | Detalle de error     | Admin  |
| `GET`  | `/api/errors/stats`        | Estadísticas         | Admin  |
| `PUT`  | `/api/errors/{id}/resolve` | Marcar como resuelto | Admin  |

---

## 📋 OBJETIVO

Implementar configuración del sistema:

- **Modo mantenimiento** (programado y emergencia)
- **Feature flags** (cuando se implemente)
- **Monitoreo de errores** (centralizado)
- **Logs y auditoría**
- **Configuración de plataforma**
- **Gestión de admins**

---

## 🔧 PASO 1: Dashboard de Sistema

```typescript
// filepath: src/app/(admin)/admin/sistema/page.tsx
import { Metadata } from "next";
import Link from "next/link";
import {
  Settings,
  ToggleLeft,
  Wrench,
  Activity,
  Users,
  Database,
  Globe,
  Mail,
} from "lucide-react";

export const metadata: Metadata = {
  title: "Sistema | Admin OKLA",
};

const sections = [
  {
    title: "Feature Flags",
    description: "Activar o desactivar funcionalidades",
    icon: ToggleLeft,
    href: "/admin/sistema/features",
    color: "bg-purple-500",
  },
  {
    title: "Configuración",
    description: "Ajustes generales de la plataforma",
    icon: Settings,
    href: "/admin/sistema/config",
    color: "bg-blue-500",
  },
  {
    title: "Mantenimiento",
    description: "Programar ventanas de mantenimiento",
    icon: Wrench,
    href: "/admin/sistema/mantenimiento",
    color: "bg-amber-500",
  },
  {
    title: "Monitoreo",
    description: "Health checks y logs del sistema",
    icon: Activity,
    href: "/admin/sistema/monitoreo",
    color: "bg-green-500",
  },
  {
    title: "Administradores",
    description: "Gestionar usuarios admin",
    icon: Users,
    href: "/admin/sistema/admins",
    color: "bg-red-500",
  },
  {
    title: "Base de datos",
    description: "Backups y mantenimiento de DB",
    icon: Database,
    href: "/admin/sistema/database",
    color: "bg-gray-500",
  },
  {
    title: "SEO & Meta",
    description: "Configuración SEO global",
    icon: Globe,
    href: "/admin/sistema/seo",
    color: "bg-teal-500",
  },
  {
    title: "Email Templates",
    description: "Editar plantillas de correo",
    icon: Mail,
    href: "/admin/sistema/emails",
    color: "bg-pink-500",
  },
];

export default function SystemPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Sistema</h1>
        <p className="text-gray-600">Configuración avanzada de la plataforma</p>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {sections.map((section) => (
          <Link
            key={section.href}
            href={section.href}
            className="bg-white rounded-xl border p-6 hover:shadow-md transition-shadow"
          >
            <div className={`w-12 h-12 rounded-xl ${section.color} flex items-center justify-center mb-4`}>
              <section.icon size={24} className="text-white" />
            </div>
            <h3 className="font-semibold text-gray-900">{section.title}</h3>
            <p className="text-sm text-gray-500 mt-1">{section.description}</p>
          </Link>
        ))}
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 2: Feature Flags

```typescript
// filepath: src/app/(admin)/admin/sistema/features/page.tsx
import { Metadata } from "next";
import { FeatureFlagsList } from "@/components/admin/system/FeatureFlagsList";

export const metadata: Metadata = {
  title: "Feature Flags | Sistema",
};

export default function FeatureFlagsPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Feature Flags</h1>
        <p className="text-gray-600">Activa o desactiva funcionalidades sin desplegar código</p>
      </div>

      <FeatureFlagsList />
    </div>
  );
}
```

```typescript
// filepath: src/components/admin/system/FeatureFlagsList.tsx
"use client";

import { useState } from "react";
import { Search, Plus } from "lucide-react";
import { Input } from "@/components/ui/Input";
import { Button } from "@/components/ui/Button";
import { Switch } from "@/components/ui/Switch";
import { Badge } from "@/components/ui/Badge";
import { useFeatureFlags, useToggleFeatureFlag } from "@/lib/hooks/useSystem";

export function FeatureFlagsList() {
  const [search, setSearch] = useState("");
  const { data: flags } = useFeatureFlags();
  const { toggle, isToggling } = useToggleFeatureFlag();

  const filteredFlags = flags?.filter((flag) =>
    flag.name.toLowerCase().includes(search.toLowerCase()) ||
    flag.description.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div className="bg-white rounded-xl border">
      {/* Header */}
      <div className="p-4 border-b flex items-center gap-4">
        <div className="relative flex-1">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
          <Input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Buscar feature flags..."
            className="pl-9"
          />
        </div>
        <Button>
          <Plus size={16} className="mr-2" />
          Nuevo flag
        </Button>
      </div>

      {/* List */}
      <div className="divide-y">
        {filteredFlags?.map((flag) => (
          <div key={flag.id} className="p-4 flex items-center gap-4">
            <div className="flex-1">
              <div className="flex items-center gap-2">
                <h4 className="font-medium text-gray-900">{flag.name}</h4>
                <Badge variant={flag.environment === "production" ? "danger" : "default"}>
                  {flag.environment}
                </Badge>
              </div>
              <p className="text-sm text-gray-500 mt-1">{flag.description}</p>
              <code className="text-xs text-gray-400 mt-2 block">{flag.key}</code>
            </div>

            <Switch
              checked={flag.enabled}
              onCheckedChange={() => toggle(flag.id)}
              disabled={isToggling}
            />
          </div>
        ))}
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 3: Mantenimiento Programado

```typescript
// filepath: src/app/(admin)/admin/sistema/mantenimiento/page.tsx
import { Metadata } from "next";
import { MaintenanceScheduler } from "@/components/admin/system/MaintenanceScheduler";
import { MaintenanceHistory } from "@/components/admin/system/MaintenanceHistory";
import { ActiveMaintenance } from "@/components/admin/system/ActiveMaintenance";

export const metadata: Metadata = {
  title: "Mantenimiento | Sistema",
};

export default function MaintenancePage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Mantenimiento</h1>
        <p className="text-gray-600">Programa y gestiona ventanas de mantenimiento</p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <ActiveMaintenance />
        <MaintenanceScheduler />
      </div>

      <MaintenanceHistory />
    </div>
  );
}
```

```typescript
// filepath: src/components/admin/system/MaintenanceScheduler.tsx
"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Calendar, Clock, AlertTriangle } from "lucide-react";
import { FormField } from "@/components/ui/FormField";
import { Input } from "@/components/ui/Input";
import { Textarea } from "@/components/ui/Textarea";
import { Select } from "@/components/ui/Select";
import { Button } from "@/components/ui/Button";
import { useScheduleMaintenance } from "@/lib/hooks/useSystem";

const schema = z.object({
  title: z.string().min(5, "Título muy corto"),
  description: z.string().min(10, "Descripción muy corta"),
  severity: z.enum(["info", "warning", "critical"]),
  startTime: z.string().min(1, "Requerido"),
  endTime: z.string().min(1, "Requerido"),
  notifyUsers: z.boolean(),
});

type MaintenanceFormData = z.infer<typeof schema>;

export function MaintenanceScheduler() {
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<MaintenanceFormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      severity: "warning",
      notifyUsers: true,
    },
  });

  const { schedule, isScheduling } = useScheduleMaintenance();

  const onSubmit = async (data: MaintenanceFormData) => {
    await schedule(data);
    reset();
  };

  return (
    <div className="bg-white rounded-xl border p-6">
      <h3 className="font-semibold text-gray-900 mb-4 flex items-center gap-2">
        <Calendar size={20} />
        Programar mantenimiento
      </h3>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <FormField label="Título" error={errors.title?.message}>
          <Input {...register("title")} placeholder="Mantenimiento programado" />
        </FormField>

        <FormField label="Descripción" error={errors.description?.message}>
          <Textarea
            {...register("description")}
            rows={3}
            placeholder="Descripción del mantenimiento..."
          />
        </FormField>

        <FormField label="Severidad" error={errors.severity?.message}>
          <Select {...register("severity")}>
            <option value="info">Informativo</option>
            <option value="warning">Advertencia</option>
            <option value="critical">Crítico</option>
          </Select>
        </FormField>

        <div className="grid grid-cols-2 gap-4">
          <FormField label="Inicio" error={errors.startTime?.message}>
            <Input {...register("startTime")} type="datetime-local" />
          </FormField>

          <FormField label="Fin" error={errors.endTime?.message}>
            <Input {...register("endTime")} type="datetime-local" />
          </FormField>
        </div>

        <label className="flex items-center gap-2">
          <input type="checkbox" {...register("notifyUsers")} className="rounded" />
          <span className="text-sm text-gray-600">Notificar a usuarios por email</span>
        </label>

        <Button type="submit" disabled={isScheduling} className="w-full">
          Programar mantenimiento
        </Button>
      </form>
    </div>
  );
}
```

---

## 🔧 PASO 4: Monitoreo

```typescript
// filepath: src/app/(admin)/admin/sistema/monitoreo/page.tsx
import { Metadata } from "next";
import { Suspense } from "react";
import { ServiceHealthGrid } from "@/components/admin/system/ServiceHealthGrid";
import { SystemLogs } from "@/components/admin/system/SystemLogs";
import { PerformanceMetrics } from "@/components/admin/system/PerformanceMetrics";
import { LoadingCard } from "@/components/ui/LoadingCard";

export const metadata: Metadata = {
  title: "Monitoreo | Sistema",
};

export default function MonitoringPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Monitoreo</h1>
        <p className="text-gray-600">Estado de servicios y logs del sistema</p>
      </div>

      {/* Service Health */}
      <Suspense fallback={<LoadingCard className="h-48" />}>
        <ServiceHealthGrid />
      </Suspense>

      {/* Performance */}
      <Suspense fallback={<LoadingCard className="h-64" />}>
        <PerformanceMetrics />
      </Suspense>

      {/* Logs */}
      <Suspense fallback={<LoadingCard className="h-96" />}>
        <SystemLogs />
      </Suspense>
    </div>
  );
}
```

```typescript
// filepath: src/components/admin/system/ServiceHealthGrid.tsx
import { CheckCircle, XCircle, AlertCircle, RefreshCw } from "lucide-react";
import { systemService } from "@/lib/services/systemService";

const statusIcons = {
  healthy: { icon: CheckCircle, color: "text-green-500" },
  degraded: { icon: AlertCircle, color: "text-amber-500" },
  unhealthy: { icon: XCircle, color: "text-red-500" },
};

export async function ServiceHealthGrid() {
  const services = await systemService.getServiceHealth();

  return (
    <div className="bg-white rounded-xl border">
      <div className="p-4 border-b flex items-center justify-between">
        <h3 className="font-semibold text-gray-900">Estado de servicios</h3>
        <button className="text-gray-400 hover:text-gray-600">
          <RefreshCw size={16} />
        </button>
      </div>

      <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-4 p-4">
        {services.map((service) => {
          const config = statusIcons[service.status as keyof typeof statusIcons];
          const Icon = config.icon;

          return (
            <div
              key={service.name}
              className="text-center p-4 rounded-lg border hover:shadow-sm"
            >
              <Icon size={24} className={`mx-auto mb-2 ${config.color}`} />
              <p className="text-sm font-medium text-gray-900">{service.name}</p>
              <p className="text-xs text-gray-500">{service.responseTime}ms</p>
            </div>
          );
        })}
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 5: Gestión de Admins

```typescript
// filepath: src/app/(admin)/admin/sistema/admins/page.tsx
import { Metadata } from "next";
import { Suspense } from "react";
import { AdminsList } from "@/components/admin/system/AdminsList";
import { InviteAdminButton } from "@/components/admin/system/InviteAdminButton";
import { LoadingTable } from "@/components/ui/LoadingTable";

export const metadata: Metadata = {
  title: "Administradores | Sistema",
};

export default function AdminsPage() {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Administradores</h1>
          <p className="text-gray-600">Gestiona usuarios con acceso administrativo</p>
        </div>
        <InviteAdminButton />
      </div>

      <Suspense fallback={<LoadingTable rows={5} cols={5} />}>
        <AdminsList />
      </Suspense>
    </div>
  );
}
```

```typescript
// filepath: src/components/admin/system/AdminsList.tsx
import { MoreHorizontal, Shield, Mail, Key } from "lucide-react";
import { Avatar } from "@/components/ui/Avatar";
import { Badge } from "@/components/ui/Badge";
import { DropdownMenu } from "@/components/ui/DropdownMenu";
import { systemService } from "@/lib/services/systemService";
import { formatDate } from "@/lib/utils";

const roleLabels = {
  "ADM-SUPPORT": { label: "Soporte", color: "info" },
  "ADM-MOD": { label: "Moderador", color: "purple" },
  "ADM-COMP": { label: "Compliance", color: "warning" },
  "ADM-ADMIN": { label: "Admin", color: "success" },
  "ADM-SUPER": { label: "Super Admin", color: "danger" },
} as const;

export async function AdminsList() {
  const admins = await systemService.getAdmins();

  return (
    <div className="bg-white rounded-xl border">
      <div className="overflow-x-auto">
        <table className="w-full">
          <thead className="bg-gray-50 border-b">
            <tr>
              <th className="p-4 text-left text-sm font-medium text-gray-500">Usuario</th>
              <th className="p-4 text-left text-sm font-medium text-gray-500">Rol</th>
              <th className="p-4 text-left text-sm font-medium text-gray-500">Último acceso</th>
              <th className="p-4 text-left text-sm font-medium text-gray-500">2FA</th>
              <th className="p-4 w-12"></th>
            </tr>
          </thead>
          <tbody className="divide-y">
            {admins.map((admin) => {
              const roleConfig = roleLabels[admin.role as keyof typeof roleLabels];

              return (
                <tr key={admin.id} className="hover:bg-gray-50">
                  <td className="p-4">
                    <div className="flex items-center gap-3">
                      <Avatar src={admin.avatar} name={admin.fullName} size="sm" />
                      <div>
                        <p className="font-medium text-gray-900">{admin.fullName}</p>
                        <p className="text-sm text-gray-500">{admin.email}</p>
                      </div>
                    </div>
                  </td>
                  <td className="p-4">
                    <Badge variant={roleConfig?.color}>{roleConfig?.label}</Badge>
                  </td>
                  <td className="p-4 text-sm text-gray-500">
                    {admin.lastLoginAt ? formatDate(admin.lastLoginAt) : "Nunca"}
                  </td>
                  <td className="p-4">
                    <Badge variant={admin.twoFactorEnabled ? "success" : "default"}>
                      {admin.twoFactorEnabled ? "Activo" : "Inactivo"}
                    </Badge>
                  </td>
                  <td className="p-4">
                    <DropdownMenu
                      trigger={
                        <button className="p-2 hover:bg-gray-100 rounded-lg">
                          <MoreHorizontal size={16} />
                        </button>
                      }
                      items={[
                        { icon: Shield, label: "Cambiar rol", onClick: () => {} },
                        { icon: Key, label: "Reset 2FA", onClick: () => {} },
                        { icon: Mail, label: "Enviar reset password", onClick: () => {} },
                      ]}
                    />
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}
```

---

## ✅ VALIDACIÓN

```bash
pnpm dev
# Verificar:
# - /admin/sistema muestra grid de secciones
# - Feature flags se toggle correctamente
# - Mantenimiento se puede programar
# - Monitoreo muestra estado de servicios
# - Lista de admins funciona
```

---

## 📊 RESUMEN DE DOCUMENTOS COMPLETADOS

| #   | Documento            | Rol principal          | Estado |
| --- | -------------------- | ---------------------- | ------ |
| 09  | dealer-inventario.md | DLR-STAFF, DLR-ADMIN   | ✅     |
| 10  | dealer-crm.md        | DLR-ADMIN              | ✅     |
| 11  | help-center.md       | USR-ANON, USR-REG      | ✅     |
| 12  | admin-dashboard.md   | ADM-ADMIN, ADM-SUPER   | ✅     |
| 13  | admin-users.md       | ADM-ADMIN, ADM-SUPER   | ✅     |
| 14  | admin-moderation.md  | ADM-MOD, ADM-ADMIN     | ✅     |
| 15  | admin-compliance.md  | ADM-COMP, ADM-ADMIN    | ✅     |
| 16  | admin-support.md     | ADM-SUPPORT, ADM-ADMIN | ✅     |
| 17  | admin-system.md      | ADM-SUPER              | ✅     |

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/admin-system.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsAdmin } from "../helpers/auth";

test.describe("Admin System Configuration", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("debe mostrar configuración del sistema", async ({ page }) => {
    await page.goto("/admin/system");

    await expect(page.getByRole("heading", { name: /sistema/i })).toBeVisible();
  });

  test("debe ver estado de servicios", async ({ page }) => {
    await page.goto("/admin/system/health");

    await expect(page.getByTestId("services-status")).toBeVisible();
  });

  test("debe configurar modo mantenimiento", async ({ page }) => {
    await page.goto("/admin/system/maintenance");

    await page.getByRole("switch", { name: /modo mantenimiento/i }).click();
    await page.fill('input[name="message"]', "Actualización programada");
    await page.getByRole("button", { name: /guardar/i }).click();

    await expect(page.getByText(/configuración guardada/i)).toBeVisible();
  });

  test("debe ver logs del sistema", async ({ page }) => {
    await page.goto("/admin/system/logs");

    await expect(page.getByTestId("system-logs")).toBeVisible();
  });

  test("debe ver métricas de performance", async ({ page }) => {
    await page.goto("/admin/system/performance");

    await expect(page.getByTestId("performance-metrics")).toBeVisible();
  });
});
```

---

## 🎯 TOTAL DE DOCUMENTACIÓN

**Documentos en `04-PAGINAS/`:** 17 documentos  
**Páginas totales cubiertas:** ~73  
**Roles cubiertos:** 10/10 (100%)
