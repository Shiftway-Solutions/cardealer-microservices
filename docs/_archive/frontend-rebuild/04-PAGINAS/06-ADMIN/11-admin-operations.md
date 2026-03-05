---
title: "Admin Operations - Sistema Completo de Operaciones"
priority: P2
estimated_time: "2 horas"
dependencies: []
apis: ["VehiclesSaleService", "AuthService", "NotificationService"]
status: complete
last_updated: "2026-01-30"
---

# ⚙️ Admin Operations - Sistema Completo de Operaciones

> **Última actualización:** Enero 29, 2026  
> **Complejidad:** 🔴 Alta (Dashboards de monitoreo y gestión)  
> **Estado:** 📖 Documentación Completa - Listo para Implementación  
> **Dependencias:** ErrorService, SchedulerService, Health Checks, Feature Toggle

---

## 📚 DOCUMENTACIÓN BASE

Este documento integra TODOS los componentes UI de operaciones de la carpeta `docs/process-matrix/11-INFRAESTRUCTURA-DEVOPS/`:

| Documento Process Matrix  | Secciones Cubiertas                                |
| ------------------------- | -------------------------------------------------- |
| `02-error-service.md`     | Dashboard de errores, DLQ management, analytics    |
| `04-health-checks.md`     | Status page, uptime monitoring, incident tracking  |
| `10-scheduler-service.md` | Job dashboard, historial ejecuciones, gestión jobs |
| `12-feature-toggle.md`    | Feature flags CRUD, A/B testing, rollout control   |

---

## ⚠️ AUDITORÍA DE ESTADO (Enero 29, 2026)

### Estado de Implementación Backend

| Componente Backend   | Estado  | Observación              |
| -------------------- | ------- | ------------------------ |
| ErrorService API     | ✅ 100% | `/backend/ErrorService/` |
| Health Checks System | ✅ 100% | Todos los servicios      |
| SchedulerService API | 🟡 40%  | Hangfire configurado     |
| Feature Toggle API   | 🔴 0%   | **NO IMPLEMENTADO**      |

### Estado de Acceso UI

| Funcionalidad UI | Estado | Ubicación Propuesta             |
| ---------------- | ------ | ------------------------------- |
| Error Dashboard  | 🟡 50% | `/admin/errors` existe (básico) |
| DLQ Management   | 🔴 0%  | `/admin/errors/dlq`             |
| Status Page      | 🟡 70% | `/health` UI básica             |
| Job Scheduler    | 🔴 0%  | `/admin/scheduler`              |
| Feature Flags    | 🔴 0%  | `/admin/features`               |

---

## 📊 RESUMEN DE PROCESOS A IMPLEMENTAR

### ERR-\* (Error Management) - 9 procesos

| ID          | Proceso                 | Backend | UI     | Prioridad    |
| ----------- | ----------------------- | ------- | ------ | ------------ |
| ERR-LOG-01  | Registrar error         | ✅ 100% | N/A    | Backend only |
| ERR-LOG-02  | Error aggregation       | ✅ 100% | N/A    | Backend only |
| ERR-DASH-01 | Dashboard de errores    | ✅ 100% | 🟡 50% | 🔴 ALTA      |
| ERR-DASH-02 | Filtros y búsqueda      | ✅ 100% | 🔴 0%  | 🔴 ALTA      |
| ERR-DASH-03 | Analytics de tendencias | ✅ 100% | 🔴 0%  | 🟡 MEDIA     |
| DLQ-MGT-01  | Ver mensajes DLQ        | ✅ 100% | 🔴 0%  | 🔴 CRÍTICA   |
| DLQ-MGT-02  | Retry mensaje           | ✅ 100% | 🔴 0%  | 🔴 CRÍTICA   |
| DLQ-MGT-03  | Delete mensaje          | ✅ 100% | 🔴 0%  | 🟡 MEDIA     |
| DLQ-MGT-04  | Batch operations        | ✅ 100% | 🔴 0%  | 🟡 MEDIA     |

### HC-\* (Health Checks) - 5 procesos

| ID           | Proceso                | Backend | UI     | Prioridad |
| ------------ | ---------------------- | ------- | ------ | --------- |
| HC-STATUS-01 | Status page pública    | ✅ 100% | 🟡 70% | 🔴 ALTA   |
| HC-STATUS-02 | Uptime history         | ✅ 100% | 🔴 0%  | 🟡 MEDIA  |
| HC-STATUS-03 | Incident timeline      | 🔴 0%   | 🔴 0%  | 🟡 MEDIA  |
| HC-ADMIN-01  | Admin dashboard        | ✅ 100% | 🔴 0%  | 🔴 ALTA   |
| HC-ADMIN-02  | Service health details | ✅ 100% | 🔴 0%  | 🔴 ALTA   |

### SCHED-\* (Scheduler) - 8 procesos

| ID               | Proceso            | Backend | UI    | Prioridad |
| ---------------- | ------------------ | ------- | ----- | --------- |
| SCHED-LIST-01    | Listar jobs        | 🟡 40%  | 🔴 0% | 🔴 ALTA   |
| SCHED-CREATE-01  | Crear job          | 🟡 40%  | 🔴 0% | 🔴 ALTA   |
| SCHED-EDIT-01    | Editar job         | 🟡 40%  | 🔴 0% | 🔴 ALTA   |
| SCHED-DELETE-01  | Eliminar job       | 🟡 40%  | 🔴 0% | 🟡 MEDIA  |
| SCHED-TRIGGER-01 | Trigger manual     | 🟡 40%  | 🔴 0% | 🔴 ALTA   |
| SCHED-PAUSE-01   | Pausar/Reanudar    | 🟡 40%  | 🔴 0% | 🟡 MEDIA  |
| SCHED-HISTORY-01 | Ver historial      | 🔴 0%   | 🔴 0% | 🔴 ALTA   |
| SCHED-LOGS-01    | Ver logs ejecución | 🔴 0%   | 🔴 0% | 🟡 MEDIA  |

### FT-\* (Feature Toggles) - 12 procesos

| ID              | Proceso           | Backend | UI    | Prioridad |
| --------------- | ----------------- | ------- | ----- | --------- |
| FT-LIST-01      | Listar flags      | 🔴 0%   | 🔴 0% | 🟡 MEDIA  |
| FT-CREATE-01    | Crear flag        | 🔴 0%   | 🔴 0% | 🟡 MEDIA  |
| FT-EDIT-01      | Editar flag       | 🔴 0%   | 🔴 0% | 🟡 MEDIA  |
| FT-DELETE-01    | Eliminar flag     | 🔴 0%   | 🔴 0% | 🟢 BAJA   |
| FT-TOGGLE-01    | Enable/Disable    | 🔴 0%   | 🔴 0% | 🟡 MEDIA  |
| FT-ROLLOUT-01   | Ajustar % rollout | 🔴 0%   | 🔴 0% | 🟡 MEDIA  |
| FT-TARGET-01    | Targeting rules   | 🔴 0%   | 🔴 0% | 🟡 MEDIA  |
| FT-AB-01        | A/B test config   | 🔴 0%   | 🔴 0% | 🟢 BAJA   |
| FT-EVAL-01      | Ver evaluations   | 🔴 0%   | 🔴 0% | 🟢 BAJA   |
| FT-ANALYTICS-01 | Analytics         | 🔴 0%   | 🔴 0% | 🟢 BAJA   |
| FT-EXPORT-01    | Export config     | 🔴 0%   | 🔴 0% | 🟢 BAJA   |
| FT-IMPORT-01    | Import config     | 🔴 0%   | 🔴 0% | 🟢 BAJA   |

**TOTAL: 34 procesos** (21 backend completo, 13 sin implementar, 34 sin UI)

---

## 🎯 OBJETIVO DE ESTE DOCUMENTO

Implementar dashboards de admin completos para:

1. **Error Dashboard:** Monitoreo de errores, DLQ management, analytics
2. **Status Page:** Estado de servicios, uptime, incidentes
3. **Scheduler Dashboard:** Gestión de jobs programados
4. **Feature Flags:** Control de feature toggles y A/B testing

---

## 🏗️ ARQUITECTURA GENERAL

### Flujo de Monitoreo de Errores

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     Error Monitoring Architecture                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1️⃣ ERROR COLLECTION                                                        │
│  Microservices → ErrorService API                                           │
│       │                                                                      │
│       │ POST /api/errors { service, type, message, stackTrace... }          │
│       ▼                                                                      │
│  ┌─────────────────────────────────────────┐                                │
│  │         ErrorService                    │                                │
│  │  • Store in PostgreSQL                  │                                │
│  │  • Aggregate similar errors             │                                │
│  │  • Publish to RabbitMQ                  │                                │
│  │  • Alert on critical errors             │                                │
│  └─────────────────────────────────────────┘                                │
│                   │                                                          │
│                   │ RabbitMQ: error.critical                                 │
│                   ▼                                                          │
│  NotificationService → Email/Slack alerts                                    │
│                                                                             │
│  2️⃣ ADMIN MONITORING                                                         │
│  Admin → /admin/errors                                                       │
│       │                                                                      │
│       │ GET /api/errors?severity=Critical&service=VehiclesSaleService        │
│       ▼                                                                      │
│  ┌─────────────────────────────────────────┐                                │
│  │      ErrorDashboard Component           │                                │
│  │  • Real-time error stream               │                                │
│  │  • Filters: service, severity, date     │                                │
│  │  • Group by: type, service              │                                │
│  │  • Charts: trends, top errors           │                                │
│  └─────────────────────────────────────────┘                                │
│                                                                             │
│  3️⃣ DEAD LETTER QUEUE MANAGEMENT                                            │
│  Admin → /admin/errors/dlq                                                   │
│       │                                                                      │
│       │ GET /api/errors/dlq?queue=vehicle-events                            │
│       ▼                                                                      │
│  ┌─────────────────────────────────────────┐                                │
│  │       DLQManagement Component           │                                │
│  │  • List failed messages                 │                                │
│  │  • Retry single/batch                   │                                │
│  │  • Delete after inspection              │                                │
│  │  • View full message body               │                                │
│  └─────────────────────────────────────────┘                                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 📦 COMPONENTES A IMPLEMENTAR

### 1. ErrorDashboard (Admin)

**Ubicación:** `src/components/admin/errors/ErrorDashboard.tsx`

```typescript
// filepath: src/components/admin/errors/ErrorDashboard.tsx
"use client";

import * as React from "react";
import { useQuery } from "@tanstack/react-query";
import {
  AlertTriangle,
  AlertCircle,
  Info,
  XCircle,
  Filter,
  Download,
  RefreshCw
} from "lucide-react";
import { Card } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Input } from "@/components/ui/input";
import { DateRangePicker } from "@/components/ui/date-range-picker";
import { ErrorDetailsModal } from "./ErrorDetailsModal";
import { ErrorTrendsChart } from "./ErrorTrendsChart";
import { errorService } from "@/lib/services/errorService";
import { formatDate, cn } from "@/lib/utils";

interface ErrorDashboardProps {
  className?: string;
}

export function ErrorDashboard({ className }: ErrorDashboardProps) {
  const [filters, setFilters] = React.useState({
    serviceName: "all",
    severity: "all",
    searchTerm: "",
    dateRange: {
      from: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000), // Last 7 days
      to: new Date(),
    },
  });

  const [selectedError, setSelectedError] = React.useState<string | null>(null);

  const { data: errors, isLoading, refetch } = useQuery({
    queryKey: ["admin-errors", filters],
    queryFn: () => errorService.getErrors(filters),
    refetchInterval: 30000, // Auto-refresh every 30s
  });

  const { data: stats } = useQuery({
    queryKey: ["admin-error-stats", filters.dateRange],
    queryFn: () => errorService.getStats(filters.dateRange),
  });

  const { data: services } = useQuery({
    queryKey: ["admin-error-services"],
    queryFn: () => errorService.getServicesWithErrors(),
  });

  const getSeverityIcon = (severity: string) => {
    switch (severity) {
      case "Critical":
        return <XCircle className="h-5 w-5 text-red-600" />;
      case "Error":
        return <AlertCircle className="h-5 w-5 text-orange-600" />;
      case "Warning":
        return <AlertTriangle className="h-5 w-5 text-yellow-600" />;
      default:
        return <Info className="h-5 w-5 text-blue-600" />;
    }
  };

  const getSeverityBadgeVariant = (severity: string) => {
    switch (severity) {
      case "Critical":
        return "destructive";
      case "Error":
        return "default";
      case "Warning":
        return "secondary";
      default:
        return "outline";
    }
  };

  return (
    <div className={cn("space-y-6", className)}>
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Error Monitoring</h1>
          <p className="text-gray-600 mt-1">
            Monitor and manage application errors across all services
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => refetch()}
          >
            <RefreshCw className="h-4 w-4 mr-2" />
            Refresh
          </Button>
          <Button type="button" variant="outline" size="sm">
            <Download className="h-4 w-4 mr-2" />
            Export
          </Button>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid md:grid-cols-4 gap-4">
        <Card className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-gray-600">Total Errors</p>
              <p className="text-3xl font-bold mt-1">
                {stats?.totalErrors?.toLocaleString() || 0}
              </p>
            </div>
            <XCircle className="h-10 w-10 text-red-600" />
          </div>
          <p className="text-xs text-gray-500 mt-2">
            Last 7 days
          </p>
        </Card>

        <Card className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-gray-600">Critical</p>
              <p className="text-3xl font-bold mt-1 text-red-600">
                {stats?.criticalErrors || 0}
              </p>
            </div>
            <AlertCircle className="h-10 w-10 text-red-600" />
          </div>
          <p className="text-xs text-gray-500 mt-2">
            Requires immediate attention
          </p>
        </Card>

        <Card className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-gray-600">Services Affected</p>
              <p className="text-3xl font-bold mt-1">
                {stats?.affectedServices || 0}
              </p>
            </div>
            <AlertTriangle className="h-10 w-10 text-orange-600" />
          </div>
          <p className="text-xs text-gray-500 mt-2">
            Out of {stats?.totalServices || 0} total
          </p>
        </Card>

        <Card className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-gray-600">Error Rate</p>
              <p className="text-3xl font-bold mt-1">
                {stats?.errorRate?.toFixed(2) || 0}%
              </p>
            </div>
            <Info className="h-10 w-10 text-blue-600" />
          </div>
          <p className="text-xs text-gray-500 mt-2">
            Errors per total requests
          </p>
        </Card>
      </div>

      {/* Trends Chart */}
      <Card className="p-6">
        <h3 className="text-lg font-semibold mb-4">Error Trends</h3>
        <ErrorTrendsChart dateRange={filters.dateRange} />
      </Card>

      {/* Filters */}
      <Card className="p-6">
        <div className="flex items-center gap-4">
          <Filter className="h-5 w-5 text-gray-500" />
          <div className="flex-1 grid md:grid-cols-4 gap-4">
            <Select
              value={filters.serviceName}
              onValueChange={(value) =>
                setFilters({ ...filters, serviceName: value })
              }
            >
              <SelectTrigger>
                <SelectValue placeholder="All Services" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Services</SelectItem>
                {services?.map((service) => (
                  <SelectItem key={service} value={service}>
                    {service}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>

            <Select
              value={filters.severity}
              onValueChange={(value) =>
                setFilters({ ...filters, severity: value })
              }
            >
              <SelectTrigger>
                <SelectValue placeholder="All Severities" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Severities</SelectItem>
                <SelectItem value="Critical">Critical</SelectItem>
                <SelectItem value="Error">Error</SelectItem>
                <SelectItem value="Warning">Warning</SelectItem>
                <SelectItem value="Info">Info</SelectItem>
              </SelectContent>
            </Select>

            <Input
              placeholder="Search errors..."
              value={filters.searchTerm}
              onChange={(e) =>
                setFilters({ ...filters, searchTerm: e.target.value })
              }
            />

            <DateRangePicker
              value={filters.dateRange}
              onChange={(range) =>
                setFilters({ ...filters, dateRange: range })
              }
            />
          </div>
        </div>
      </Card>

      {/* Errors Table */}
      <Card className="p-6">
        <div className="space-y-4">
          <h3 className="text-lg font-semibold">Recent Errors</h3>

          {isLoading ? (
            <div className="text-center py-8">Loading errors...</div>
          ) : errors?.items?.length === 0 ? (
            <div className="text-center py-8 text-gray-500">
              No errors found matching your filters
            </div>
          ) : (
            <div className="space-y-2">
              {errors?.items?.map((error) => (
                <div
                  key={error.id}
                  className="border rounded-lg p-4 hover:bg-gray-50 cursor-pointer transition"
                  onClick={() => setSelectedError(error.id)}
                >
                  <div className="flex items-start gap-4">
                    {getSeverityIcon(error.severity)}

                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 mb-1">
                        <Badge variant={getSeverityBadgeVariant(error.severity)}>
                          {error.severity}
                        </Badge>
                        <Badge variant="outline">{error.serviceName}</Badge>
                        <span className="text-xs text-gray-500">
                          {formatDate(error.occurredAt)}
                        </span>
                      </div>

                      <p className="font-semibold text-gray-900 mb-1">
                        {error.errorType}
                      </p>

                      <p className="text-sm text-gray-600 line-clamp-2">
                        {error.message}
                      </p>

                      {error.requestPath && (
                        <p className="text-xs text-gray-500 mt-2">
                          {error.requestMethod} {error.requestPath}
                        </p>
                      )}

                      {error.count > 1 && (
                        <Badge variant="secondary" className="mt-2">
                          Occurred {error.count} times
                        </Badge>
                      )}
                    </div>

                    {error.userId && (
                      <div className="text-xs text-gray-500">
                        User: {error.userId}
                      </div>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}

          {/* Pagination */}
          {errors?.totalPages > 1 && (
            <div className="flex items-center justify-between border-t pt-4">
              <p className="text-sm text-gray-600">
                Showing {errors.items.length} of {errors.totalItems} errors
              </p>
              <div className="flex gap-2">
                <Button type="button" variant="outline" size="sm">
                  Previous
                </Button>
                <Button type="button" variant="outline" size="sm">
                  Next
                </Button>
              </div>
            </div>
          )}
        </div>
      </Card>

      {/* Error Details Modal */}
      {selectedError && (
        <ErrorDetailsModal
          errorId={selectedError}
          onClose={() => setSelectedError(null)}
        />
      )}
    </div>
  );
}
```

---

### 2. DLQManagement (Dead Letter Queue)

**Ubicación:** `src/components/admin/errors/DLQManagement.tsx`

```typescript
// filepath: src/components/admin/errors/DLQManagement.tsx
"use client";

import * as React from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  AlertTriangle,
  RotateCcw,
  Trash2,
  Eye,
  CheckSquare,
  Square
} from "lucide-react";
import { Card } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { errorService } from "@/lib/services/errorService";
import { formatDate } from "@/lib/utils";
import { toast } from "sonner";

export function DLQManagement() {
  const queryClient = useQueryClient();
  const [selectedMessages, setSelectedMessages] = React.useState<Set<string>>(new Set());
  const [viewingMessage, setViewingMessage] = React.useState<any>(null);

  const { data: dlqMessages, isLoading } = useQuery({
    queryKey: ["dlq-messages"],
    queryFn: () => errorService.getDLQMessages(),
    refetchInterval: 10000, // Refresh every 10s
  });

  const retryMutation = useMutation({
    mutationFn: (messageIds: string[]) => errorService.retryDLQMessages(messageIds),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["dlq-messages"] });
      setSelectedMessages(new Set());
      toast.success("Messages queued for retry");
    },
    onError: () => {
      toast.error("Failed to retry messages");
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (messageIds: string[]) => errorService.deleteDLQMessages(messageIds),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["dlq-messages"] });
      setSelectedMessages(new Set());
      toast.success("Messages deleted");
    },
    onError: () => {
      toast.error("Failed to delete messages");
    },
  });

  const toggleSelection = (id: string) => {
    const newSelection = new Set(selectedMessages);
    if (newSelection.has(id)) {
      newSelection.delete(id);
    } else {
      newSelection.add(id);
    }
    setSelectedMessages(newSelection);
  };

  const selectAll = () => {
    if (selectedMessages.size === dlqMessages?.length) {
      setSelectedMessages(new Set());
    } else {
      setSelectedMessages(new Set(dlqMessages?.map((m) => m.id)));
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold">Dead Letter Queue</h1>
        <p className="text-gray-600 mt-1">
          Manage failed messages that couldn't be processed
        </p>
      </div>

      {/* Stats */}
      <Card className="p-6">
        <div className="flex items-center gap-8">
          <div>
            <p className="text-sm text-gray-600">Total Messages</p>
            <p className="text-3xl font-bold">{dlqMessages?.length || 0}</p>
          </div>
          <div>
            <p className="text-sm text-gray-600">Selected</p>
            <p className="text-3xl font-bold">{selectedMessages.size}</p>
          </div>
        </div>
      </Card>

      {/* Bulk Actions */}
      {selectedMessages.size > 0 && (
        <Card className="p-4 bg-blue-50 border-blue-200">
          <div className="flex items-center justify-between">
            <p className="text-sm font-medium">
              {selectedMessages.size} message{selectedMessages.size > 1 ? "s" : ""} selected
            </p>
            <div className="flex gap-2">
              <Button
                type="button"
                size="sm"
                onClick={() => retryMutation.mutate(Array.from(selectedMessages))}
                disabled={retryMutation.isPending}
              >
                <RotateCcw className="h-4 w-4 mr-2" />
                Retry Selected
              </Button>
              <Button
                type="button"
                size="sm"
                variant="destructive"
                onClick={() => deleteMutation.mutate(Array.from(selectedMessages))}
                disabled={deleteMutation.isPending}
              >
                <Trash2 className="h-4 w-4 mr-2" />
                Delete Selected
              </Button>
            </div>
          </div>
        </Card>
      )}

      {/* Messages Table */}
      <Card className="p-6">
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="text-lg font-semibold">Failed Messages</h3>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={selectAll}
            >
              {selectedMessages.size === dlqMessages?.length ? (
                <CheckSquare className="h-4 w-4 mr-2" />
              ) : (
                <Square className="h-4 w-4 mr-2" />
              )}
              Select All
            </Button>
          </div>

          {isLoading ? (
            <div className="text-center py-8">Loading DLQ messages...</div>
          ) : dlqMessages?.length === 0 ? (
            <div className="text-center py-8">
              <AlertTriangle className="h-12 w-12 text-green-500 mx-auto mb-2" />
              <p className="text-gray-600">No messages in Dead Letter Queue</p>
              <p className="text-sm text-gray-500">All messages are processing correctly</p>
            </div>
          ) : (
            <div className="space-y-2">
              {dlqMessages?.map((message) => (
                <div
                  key={message.id}
                  className="border rounded-lg p-4 hover:bg-gray-50 transition"
                >
                  <div className="flex items-start gap-4">
                    <Checkbox
                      checked={selectedMessages.has(message.id)}
                      onCheckedChange={() => toggleSelection(message.id)}
                    />

                    <div className="flex-1">
                      <div className="flex items-center gap-2 mb-2">
                        <Badge variant="destructive">Failed</Badge>
                        <Badge variant="outline">{message.queue}</Badge>
                        <span className="text-xs text-gray-500">
                          Failed {message.retryCount} times
                        </span>
                      </div>

                      <p className="font-semibold text-gray-900 mb-1">
                        {message.messageType}
                      </p>

                      <p className="text-sm text-gray-600 mb-2">
                        {message.errorMessage}
                      </p>

                      <p className="text-xs text-gray-500">
                        Last attempt: {formatDate(message.lastAttemptAt)}
                      </p>
                    </div>

                    <div className="flex gap-2">
                      <Button
                        type="button"
                        size="sm"
                        variant="outline"
                        onClick={() => setViewingMessage(message)}
                      >
                        <Eye className="h-4 w-4" />
                      </Button>
                      <Button
                        type="button"
                        size="sm"
                        onClick={() => retryMutation.mutate([message.id])}
                        disabled={retryMutation.isPending}
                      >
                        <RotateCcw className="h-4 w-4" />
                      </Button>
                      <Button
                        type="button"
                        size="sm"
                        variant="destructive"
                        onClick={() => deleteMutation.mutate([message.id])}
                        disabled={deleteMutation.isPending}
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </Card>

      {/* Message Details Dialog */}
      {viewingMessage && (
        <Dialog open={!!viewingMessage} onOpenChange={() => setViewingMessage(null)}>
          <DialogContent className="max-w-3xl max-h-[80vh] overflow-y-auto">
            <DialogHeader>
              <DialogTitle>Message Details</DialogTitle>
            </DialogHeader>

            <div className="space-y-4">
              <div>
                <Label>Message Type</Label>
                <p className="font-mono text-sm">{viewingMessage.messageType}</p>
              </div>

              <div>
                <Label>Queue</Label>
                <p className="font-mono text-sm">{viewingMessage.queue}</p>
              </div>

              <div>
                <Label>Error Message</Label>
                <p className="text-sm text-red-600">{viewingMessage.errorMessage}</p>
              </div>

              <div>
                <Label>Message Body</Label>
                <pre className="bg-gray-50 p-4 rounded-lg text-xs overflow-x-auto">
                  {JSON.stringify(JSON.parse(viewingMessage.body), null, 2)}
                </pre>
              </div>

              <div>
                <Label>Stack Trace</Label>
                <pre className="bg-gray-50 p-4 rounded-lg text-xs overflow-x-auto">
                  {viewingMessage.stackTrace}
                </pre>
              </div>
            </div>
          </DialogContent>
        </Dialog>
      )}
    </div>
  );
}
```

---

### 3. StatusPage (Public)

**Ubicación:** `src/components/status/StatusPage.tsx`

```typescript
// filepath: src/components/status/StatusPage.tsx
"use client";

import * as React from "react";
import { useQuery } from "@tanstack/react-query";
import { CheckCircle, XCircle, AlertTriangle, Clock } from "lucide-react";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Progress } from "@/components/ui/progress";
import { healthService } from "@/lib/services/healthService";
import { formatDate, cn } from "@/lib/utils";

export function StatusPage() {
  const { data: status, isLoading } = useQuery({
    queryKey: ["system-status"],
    queryFn: () => healthService.getSystemStatus(),
    refetchInterval: 30000, // Refresh every 30s
  });

  const getStatusIcon = (health: string) => {
    switch (health) {
      case "Healthy":
        return <CheckCircle className="h-6 w-6 text-green-600" />;
      case "Degraded":
        return <AlertTriangle className="h-6 w-6 text-yellow-600" />;
      case "Unhealthy":
        return <XCircle className="h-6 w-6 text-red-600" />;
      default:
        return <Clock className="h-6 w-6 text-gray-400" />;
    }
  };

  const getStatusColor = (health: string) => {
    switch (health) {
      case "Healthy":
        return "text-green-600 bg-green-50 border-green-200";
      case "Degraded":
        return "text-yellow-600 bg-yellow-50 border-yellow-200";
      case "Unhealthy":
        return "text-red-600 bg-red-50 border-red-200";
      default:
        return "text-gray-600 bg-gray-50 border-gray-200";
    }
  };

  const overallHealth = status?.overallStatus || "Unknown";

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="container max-w-4xl py-12">
        {/* Header */}
        <div className="text-center mb-12">
          <div className="flex items-center justify-center gap-3 mb-4">
            {getStatusIcon(overallHealth)}
            <h1 className="text-4xl font-bold">OKLA System Status</h1>
          </div>

          <div className={cn("inline-flex px-4 py-2 rounded-full border-2", getStatusColor(overallHealth))}>
            <span className="font-semibold">
              {overallHealth === "Healthy" && "All Systems Operational"}
              {overallHealth === "Degraded" && "Some Systems Experiencing Issues"}
              {overallHealth === "Unhealthy" && "Major Outage"}
              {overallHealth === "Unknown" && "Status Unknown"}
            </span>
          </div>

          <p className="text-sm text-gray-500 mt-4">
            Last updated: {formatDate(new Date())}
          </p>
        </div>

        {/* Uptime */}
        <Card className="p-6 mb-6">
          <h2 className="text-lg font-semibold mb-4">System Uptime (Last 90 days)</h2>
          <div className="flex items-center gap-4">
            <div className="flex-1">
              <Progress value={status?.uptimePercentage || 0} className="h-3" />
            </div>
            <div className="text-2xl font-bold text-green-600">
              {status?.uptimePercentage?.toFixed(2) || 0}%
            </div>
          </div>
        </Card>

        {/* Services */}
        <div className="space-y-3">
          <h2 className="text-lg font-semibold mb-4">Service Status</h2>

          {isLoading ? (
            <Card className="p-6 text-center text-gray-500">
              Loading service status...
            </Card>
          ) : (
            status?.services?.map((service) => (
              <Card key={service.name} className="p-6">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-4">
                    {getStatusIcon(service.health)}
                    <div>
                      <p className="font-semibold">{service.name}</p>
                      <p className="text-sm text-gray-500">{service.description}</p>
                    </div>
                  </div>

                  <div className="text-right">
                    <Badge
                      variant={
                        service.health === "Healthy"
                          ? "default"
                          : service.health === "Degraded"
                          ? "secondary"
                          : "destructive"
                      }
                    >
                      {service.health}
                    </Badge>
                    {service.responseTime && (
                      <p className="text-xs text-gray-500 mt-1">
                        {service.responseTime}ms response
                      </p>
                    )}
                  </div>
                </div>

                {service.health !== "Healthy" && service.message && (
                  <div className="mt-4 p-3 bg-yellow-50 border border-yellow-200 rounded-lg">
                    <p className="text-sm text-yellow-800">{service.message}</p>
                  </div>
                )}
              </Card>
            ))
          )}
        </div>

        {/* Recent Incidents */}
        {status?.recentIncidents && status.recentIncidents.length > 0 && (
          <div className="mt-8">
            <h2 className="text-lg font-semibold mb-4">Recent Incidents</h2>
            <div className="space-y-3">
              {status.recentIncidents.map((incident) => (
                <Card key={incident.id} className="p-6">
                  <div className="flex items-start gap-4">
                    <AlertTriangle className="h-5 w-5 text-orange-600 mt-0.5" />
                    <div className="flex-1">
                      <div className="flex items-center gap-2 mb-1">
                        <p className="font-semibold">{incident.title}</p>
                        <Badge variant="outline">{incident.status}</Badge>
                      </div>
                      <p className="text-sm text-gray-600 mb-2">
                        {incident.description}
                      </p>
                      <p className="text-xs text-gray-500">
                        {formatDate(incident.startedAt)}
                        {incident.resolvedAt && ` - ${formatDate(incident.resolvedAt)}`}
                      </p>
                    </div>
                  </div>
                </Card>
              ))}
            </div>
          </div>
        )}

        {/* Footer */}
        <div className="mt-12 text-center text-sm text-gray-500">
          <p>
            Having issues? Contact support at{" "}
            <a href="mailto:support@okla.com.do" className="text-blue-600 hover:underline">
              support@okla.com.do
            </a>
          </p>
        </div>
      </div>
    </div>
  );
}
```

---

## 🔌 API SERVICES

### errorService.ts

```typescript
// filepath: src/lib/services/errorService.ts
import { api } from "./api";

export interface ErrorRecord {
  id: string;
  serviceName: string;
  errorType: string;
  message: string;
  stackTrace?: string;
  severity: "Info" | "Warning" | "Error" | "Critical";
  userId?: string;
  requestPath?: string;
  requestMethod?: string;
  traceId?: string;
  environment: string;
  occurredAt: string;
  count?: number;
}

export interface ErrorStats {
  totalErrors: number;
  criticalErrors: number;
  affectedServices: number;
  totalServices: number;
  errorRate: number;
}

export interface DLQMessage {
  id: string;
  queue: string;
  messageType: string;
  body: string;
  errorMessage: string;
  stackTrace: string;
  retryCount: number;
  lastAttemptAt: string;
}

class ErrorService {
  /**
   * Get errors with filters
   */
  async getErrors(filters: {
    serviceName?: string;
    severity?: string;
    searchTerm?: string;
    dateRange?: { from: Date; to: Date };
    page?: number;
    pageSize?: number;
  }) {
    const response = await api.get("/errors", { params: filters });
    return response.data;
  }

  /**
   * Get error statistics
   */
  async getStats(dateRange: { from: Date; to: Date }): Promise<ErrorStats> {
    const response = await api.get("/errors/stats", { params: dateRange });
    return response.data;
  }

  /**
   * Get services with errors
   */
  async getServicesWithErrors(): Promise<string[]> {
    const response = await api.get("/errors/services");
    return response.data;
  }

  /**
   * Get error by ID
   */
  async getErrorById(id: string): Promise<ErrorRecord> {
    const response = await api.get(`/errors/${id}`);
    return response.data;
  }

  /**
   * Get DLQ messages
   */
  async getDLQMessages(): Promise<DLQMessage[]> {
    const response = await api.get("/errors/dlq");
    return response.data;
  }

  /**
   * Retry DLQ messages
   */
  async retryDLQMessages(messageIds: string[]): Promise<void> {
    await api.post("/errors/dlq/retry", { messageIds });
  }

  /**
   * Delete DLQ messages
   */
  async deleteDLQMessages(messageIds: string[]): Promise<void> {
    await api.delete("/errors/dlq", { data: { messageIds } });
  }
}

export const errorService = new ErrorService();
```

### healthService.ts

```typescript
// filepath: src/lib/services/healthService.ts
import { api } from "./api";

export interface ServiceHealth {
  name: string;
  description: string;
  health: "Healthy" | "Degraded" | "Unhealthy";
  responseTime?: number;
  message?: string;
}

export interface Incident {
  id: string;
  title: string;
  description: string;
  status: "Investigating" | "Identified" | "Monitoring" | "Resolved";
  startedAt: string;
  resolvedAt?: string;
}

export interface SystemStatus {
  overallStatus: "Healthy" | "Degraded" | "Unhealthy";
  uptimePercentage: number;
  services: ServiceHealth[];
  recentIncidents: Incident[];
}

class HealthService {
  /**
   * Get system status (public)
   */
  async getSystemStatus(): Promise<SystemStatus> {
    const response = await api.get("/health/status");
    return response.data;
  }

  /**
   * Get detailed health check (admin)
   */
  async getDetailedHealth() {
    const response = await api.get("/health/deep");
    return response.data;
  }
}

export const healthService = new HealthService();
```

---

## 📍 INTEGRACIÓN EN PÁGINAS

### Admin Portal Layout

```typescript
// filepath: src/app/(admin)/admin/layout.tsx
import { AdminSidebar } from "@/components/admin/AdminSidebar";

const adminNavigation = [
  // ... existing items
  {
    name: "Operations",
    items: [
      { name: "Error Monitoring", href: "/admin/errors", icon: AlertTriangle },
      { name: "Dead Letter Queue", href: "/admin/errors/dlq", icon: XCircle },
      { name: "Scheduler", href: "/admin/scheduler", icon: Clock },
      { name: "Feature Flags", href: "/admin/features", icon: Flag },
    ],
  },
];
```

### Public Status Page

```typescript
// filepath: src/app/(public)/status/page.tsx
import { StatusPage } from "@/components/status/StatusPage";

export const metadata = {
  title: "System Status | OKLA",
  description: "Real-time status of all OKLA services",
};

export default function StatusPageRoute() {
  return <StatusPage />;
}
```

---

## 🧪 TESTING

### Unit Tests - ErrorDashboard

```typescript
// filepath: src/components/admin/errors/__tests__/ErrorDashboard.test.tsx
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ErrorDashboard } from "../ErrorDashboard";
import { errorService } from "@/lib/services/errorService";

jest.mock("@/lib/services/errorService");

describe("ErrorDashboard", () => {
  it("should display error stats", async () => {
    (errorService.getStats as jest.Mock).mockResolvedValue({
      totalErrors: 1234,
      criticalErrors: 5,
      affectedServices: 3,
      errorRate: 0.12,
    });

    render(<ErrorDashboard />);

    await waitFor(() => {
      expect(screen.getByText("1,234")).toBeInTheDocument();
      expect(screen.getByText("5")).toBeInTheDocument();
    });
  });

  it("should filter errors by service", async () => {
    const user = userEvent.setup();

    (errorService.getServicesWithErrors as jest.Mock).mockResolvedValue([
      "VehiclesSaleService",
      "AuthService",
    ]);

    render(<ErrorDashboard />);

    await user.click(screen.getByRole("combobox"));
    await user.click(screen.getByText("VehiclesSaleService"));

    expect(errorService.getErrors).toHaveBeenCalledWith(
      expect.objectContaining({
        serviceName: "VehiclesSaleService",
      })
    );
  });
});
```

---

## 📊 MÉTRICAS DE ÉXITO

| Métrica                        | Objetivo | Medición                                      |
| ------------------------------ | -------- | --------------------------------------------- |
| MTTR (Mean Time To Resolution) | < 15 min | Tiempo desde error crítico hasta fix deployed |
| DLQ Processing Time            | < 5 min  | Tiempo para retry/delete mensajes DLQ         |
| Status Page Availability       | 99.9%    | Uptime de status page                         |
| Admin Dashboard Load Time      | < 2s     | First contentful paint                        |

---

## 🚀 PRÓXIMOS PASOS

### Sprint 1: Error Dashboard (Prioridad 🔴 CRÍTICA)

- [ ] ErrorDashboard component
- [ ] ErrorDetailsModal component
- [ ] ErrorTrendsChart component
- [ ] DLQManagement component
- [ ] errorService.ts completo
- [ ] Tests unitarios (> 80% coverage)

### Sprint 2: Status Page (Prioridad 🔴 ALTA)

- [ ] StatusPage component (público)
- [ ] Incident management system
- [ ] Uptime monitoring
- [ ] healthService.ts completo
- [ ] Tests E2E

### Sprint 3: Scheduler Dashboard (Prioridad 🟡 MEDIA)

- [ ] SchedulerDashboard component
- [ ] Job creation/edit forms
- [ ] Execution history viewer
- [ ] schedulerService.ts completo

### Sprint 4: Feature Flags (Prioridad 🟡 MEDIA - Fase 2)

- [ ] **Backend:** Feature Toggle Service completo
- [ ] FeatureFlagsManagement component
- [ ] A/B test configuration
- [ ] Rollout percentage control
- [ ] featureToggleService.ts completo

---

## 📚 REFERENCIAS

### Documentos Process Matrix

- [02-error-service.md](../../process-matrix/11-INFRAESTRUCTURA-DEVOPS/02-error-service.md)
- [04-health-checks.md](../../process-matrix/11-INFRAESTRUCTURA-DEVOPS/04-health-checks.md)
- [10-scheduler-service.md](../../process-matrix/11-INFRAESTRUCTURA-DEVOPS/10-scheduler-service.md)
- [12-feature-toggle.md](../../process-matrix/11-INFRAESTRUCTURA-DEVOPS/12-feature-toggle.md)

### Backend Services

- `ErrorService.Api` - `/backend/ErrorService/` (puerto 15101)
- `SchedulerService.Api` - `/backend/SchedulerService/` (puerto 5046)
- Health Checks - Todos los servicios exponen `/health`

### Herramientas Externas

- **Seq:** http://seq:5341 - Logging centralizado
- **Grafana:** http://grafana:3000 - Dashboards
- **Prometheus:** http://prometheus:9090 - Métricas
- **Hangfire:** http://scheduler:5046/hangfire - Job dashboard (interno)

---

## **✅ DOCUMENTO COMPLETO - LISTO PARA IMPLEMENTACIÓN**

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/admin-operations.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsAdmin } from "../helpers/auth";

test.describe("Admin Operations", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("debe mostrar dashboard de operaciones", async ({ page }) => {
    await page.goto("/admin/operations");

    await expect(page.getByTestId("operations-dashboard")).toBeVisible();
  });

  test("debe ver status de servicios", async ({ page }) => {
    await page.goto("/admin/operations/services");

    await expect(page.getByTestId("services-status")).toBeVisible();
  });

  test("debe ver cola de errores (DLQ)", async ({ page }) => {
    await page.goto("/admin/operations/dlq");

    await expect(page.getByTestId("dlq-queue")).toBeVisible();
  });

  test("debe reprocesar mensaje de DLQ", async ({ page }) => {
    await page.goto("/admin/operations/dlq");

    await page
      .getByTestId("dlq-message")
      .first()
      .getByRole("button", { name: /reprocesar/i })
      .click();
    await expect(page.getByText(/mensaje reprocesado/i)).toBeVisible();
  });

  test("debe ver jobs programados", async ({ page }) => {
    await page.goto("/admin/operations/scheduler");

    await expect(page.getByTestId("scheduled-jobs")).toBeVisible();
  });
});
```

---

_Este documento consolida TODOS los componentes UI de operaciones de infraestructura en dashboards admin completos._

---

**Siguiente documento:** N/A (documentación frontend-rebuild completa para operaciones)

**Dependencias backend:** ErrorService (100%), Health Checks (100%), SchedulerService (40%), Feature Toggle (0%)

**Prioridad:** 🔴 CRÍTICA (Error Dashboard y DLQ), 🔴 ALTA (Status Page)
