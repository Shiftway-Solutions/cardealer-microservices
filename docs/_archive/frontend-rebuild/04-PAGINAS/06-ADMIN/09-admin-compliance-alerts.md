---
title: "38. Admin Compliance - Alertas Regulatorias"
priority: P2
estimated_time: "2 horas"
dependencies: []
apis: ["NotificationService"]
status: partial
last_updated: "2026-01-30"
---

# 38. Admin Compliance - Alertas Regulatorias

> **Objetivo:** Implementar sistema de monitoreo de alertas regulatorias para el equipo de compliance  
> **Tiempo estimado:** 2-3 horas  
> **Prioridad:** P1 (Crítico - Compliance legal)  
> **Complejidad:** 🟡 Media  
> **Dependencias:** ComplianceService (Puerto 5027), NotificationService
> **Última actualización:** Enero 2026

---

## 📊 AUDITORÍA DE INTEGRACIONES

| Backend Service     | Puerto | Estado Backend | Estado UI |
| ------------------- | ------ | -------------- | --------- |
| ComplianceService   | 5027   | ✅ 85%         | 🔴 10%    |
| NotificationService | 5006   | ✅ 100%        | 🟡 70%    |

### Rutas UI Faltantes (CRÍTICAS)

| Ruta                         | Prioridad | Estado | Descripción                   |
| ---------------------------- | --------- | ------ | ----------------------------- |
| `/admin/compliance/alerts`   | P1        | ❌     | Lista de alertas regulatorias |
| `/admin/compliance/calendar` | P2        | ❌     | Calendario de deadlines       |
| `/admin/compliance/sources`  | P2        | ❌     | Gestión de fuentes            |
| `/admin/compliance/history`  | P2        | ❌     | Historial de alertas          |
| `/admin/compliance/keywords` | P3        | ❌     | Gestión de keywords           |

---

## 🏗️ ARQUITECTURA

```
┌────────────────────────────────────────────────────────────────────────────┐
│                    REGULATORY ALERTS SYSTEM                                 │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  📡 FUENTES REGULATORIAS                                                   │
│  ├─ DGII (Dirección General de Impuestos Internos)                        │
│  │   • Tipo: RSS Feed                                                      │
│  │   • Frecuencia: Diario                                                  │
│  │   • Categoría: Tax                                                      │
│  ├─ Pro Consumidor                                                         │
│  │   • Tipo: Web Scraping                                                  │
│  │   • Frecuencia: Diario                                                  │
│  │   • Categoría: ConsumerProtection                                       │
│  ├─ Superintendencia de Bancos (SB)                                        │
│  │   • Tipo: API Polling                                                   │
│  │   • Frecuencia: Semanal                                                 │
│  │   • Categoría: Finance                                                  │
│  ├─ DGCP (Dirección General de Contrataciones Públicas)                   │
│  │   • Tipo: RSS Feed                                                      │
│  │   • Frecuencia: Semanal                                                 │
│  │   • Categoría: BusinessRegistration                                     │
│  └─ INDOTEL                                                                │
│      • Tipo: Web Scraping                                                  │
│      • Frecuencia: Semanal                                                 │
│      • Categoría: DataPrivacy                                              │
│                                                                             │
│  ⚠️ NIVELES DE SEVERIDAD                                                   │
│  ├─ 🔴 CRITICAL (Score >= 80): Acción en 24h                              │
│  ├─ 🟠 HIGH (Score >= 60): Acción en 72h                                  │
│  ├─ 🟡 MEDIUM (Score >= 40): Acción en 7 días                             │
│  └─ 🔵 LOW (Score >= 30): Acción en 14 días                               │
│                                                                             │
│  📋 WORKFLOW DE ALERTAS                                                    │
│  NEW → ACKNOWLEDGED → UNDER_REVIEW → ACTION_REQUIRED → IN_PROGRESS →      │
│  RESOLVED / NOT_APPLICABLE                                                  │
│                                                                             │
└────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔌 BACKEND API

### ComplianceService Endpoints (Puerto 5027)

```typescript
// ALERTAS REGULATORIAS
GET    /api/regulatory-alerts                    # Listar alertas (paginado)
GET    /api/regulatory-alerts/{id}               # Obtener alerta por ID
POST   /api/regulatory-alerts/{id}/acknowledge   # Acusar recibo
POST   /api/regulatory-alerts/{id}/review        # Marcar en revisión
POST   /api/regulatory-alerts/{id}/action-plan   # Crear plan de acción
POST   /api/regulatory-alerts/{id}/resolve       # Resolver alerta
POST   /api/regulatory-alerts/{id}/not-applicable # Marcar no aplicable

// FUENTES REGULATORIAS
GET    /api/regulatory-sources                   # Listar fuentes
POST   /api/regulatory-sources                   # Agregar fuente
PUT    /api/regulatory-sources/{id}              # Actualizar fuente
DELETE /api/regulatory-sources/{id}              # Eliminar fuente
POST   /api/regulatory-sources/{id}/scan-now     # Escanear ahora

// KEYWORDS
GET    /api/regulatory-keywords                  # Listar keywords
POST   /api/regulatory-keywords                  # Agregar keyword
PUT    /api/regulatory-keywords/{id}             # Actualizar keyword
DELETE /api/regulatory-keywords/{id}             # Eliminar keyword

// DASHBOARD
GET    /api/regulatory/dashboard                 # Métricas del sistema
GET    /api/regulatory/calendar                  # Calendario de deadlines
```

---

## 📋 ENTIDADES

### RegulatoryAlert

```csharp
public class RegulatoryAlert
{
    public Guid Id { get; private set; }

    // Fuente
    public Guid SourceId { get; private set; }
    public RegulatorySource Source { get; private set; }
    public string SourceUrl { get; private set; }
    public DateTime PublishedAt { get; private set; }

    // Clasificación
    public AlertCategory Category { get; private set; }
    public AlertSeverity Severity { get; private set; }
    public int ImpactScore { get; private set; }  // 0-100

    // Contenido
    public string Title { get; private set; }
    public string Summary { get; private set; }
    public string? FullText { get; private set; }
    public List<string> MatchedKeywords { get; private set; }
    public string? DocumentUrl { get; private set; }

    // Workflow
    public AlertStatus Status { get; private set; }
    public DateTime DetectedAt { get; private set; }
    public DateTime? AcknowledgedAt { get; private set; }
    public Guid? AcknowledgedBy { get; private set; }
    public string? ActionPlan { get; private set; }
    public DateTime? ActionDeadline { get; private set; }
    public string? Resolution { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public Guid? ResolvedBy { get; private set; }

    // Asignación
    public Guid? AssignedTo { get; private set; }
    public string? Notes { get; private set; }
}

// Enums
public enum AlertCategory
{
    Tax, ConsumerProtection, Finance, DataPrivacy, BusinessRegistration,
    Environmental, Labor, Other
}

public enum AlertSeverity { Low, Medium, High, Critical }

public enum AlertStatus
{
    New, Acknowledged, UnderReview, ActionRequired, InProgress,
    Resolved, NotApplicable
}
```

### RegulatorySource

```csharp
public class RegulatorySource
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public SourceType Type { get; private set; }

    // Configuración
    public string BaseUrl { get; private set; }
    public string? RssFeedUrl { get; private set; }
    public string? CrawlPattern { get; private set; }
    public AlertCategory DefaultCategory { get; private set; }

    // Programación
    public int ScanFrequencyHours { get; private set; }
    public DateTime? LastScannedAt { get; private set; }
    public DateTime? NextScanAt { get; private set; }
    public int FailedScanCount { get; private set; }

    // Estado
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
}

public enum SourceType { RssFeed, WebScraping, ApiPolling, ManualEntry }
```

---

## 📋 PROCESOS DETALLADOS

### REG-001: Escanear Fuente Regulatoria

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

### REG-002: Procesar Alerta Regulatoria

| Paso | Acción                          | Sistema            | Validación        |
| ---- | ------------------------------- | ------------------ | ----------------- |
| 1    | Compliance recibe notificación  | Email/Teams        | Alert created     |
| 2    | Abre alerta en dashboard        | Frontend           | Alert loaded      |
| 3    | Revisa documento original       | Frontend           | Doc available     |
| 4    | Acusa recibo (Acknowledge)      | ComplianceService  | Status updated    |
| 5    | Evalúa impacto en OKLA          | Compliance Officer | Manual            |
| 6    | Si aplica: crear plan de acción | ComplianceService  | ActionPlan saved  |
| 7    | Asignar responsable             | ComplianceService  | AssignedTo set    |
| 8    | Seguimiento hasta resolución    | ComplianceService  | Status tracking   |
| 9    | Documentar resolución           | ComplianceService  | Resolution saved  |
| 10   | Cerrar alerta                   | ComplianceService  | Status = Resolved |

---

## 🎨 COMPONENTES

### PASO 1: RegulatoryAlertsList

```typescript
// filepath: src/components/admin/compliance/RegulatoryAlertsList.tsx
"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Card } from "@/components/ui/Card";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/Tabs";
import {
  AlertTriangle,
  AlertCircle,
  Info,
  CheckCircle,
  Clock,
  ExternalLink,
  Eye,
  FileText
} from "lucide-react";
import { formatDistanceToNow } from "date-fns";
import { es } from "date-fns/locale";
import { complianceService } from "@/lib/services/complianceService";

const severityConfig = {
  Critical: { icon: AlertTriangle, color: "bg-red-100 text-red-700 border-red-300" },
  High: { icon: AlertCircle, color: "bg-orange-100 text-orange-700 border-orange-300" },
  Medium: { icon: Info, color: "bg-yellow-100 text-yellow-700 border-yellow-300" },
  Low: { icon: Info, color: "bg-blue-100 text-blue-700 border-blue-300" },
};

const statusConfig = {
  New: { label: "Nueva", color: "bg-purple-100 text-purple-700" },
  Acknowledged: { label: "Acusada", color: "bg-blue-100 text-blue-700" },
  UnderReview: { label: "En Revisión", color: "bg-yellow-100 text-yellow-700" },
  ActionRequired: { label: "Acción Requerida", color: "bg-orange-100 text-orange-700" },
  InProgress: { label: "En Progreso", color: "bg-indigo-100 text-indigo-700" },
  Resolved: { label: "Resuelta", color: "bg-green-100 text-green-700" },
  NotApplicable: { label: "No Aplica", color: "bg-gray-100 text-gray-700" },
};

export function RegulatoryAlertsList() {
  const [selectedStatus, setSelectedStatus] = useState<string>("pending");
  const queryClient = useQueryClient();

  const { data: alerts, isLoading } = useQuery({
    queryKey: ["regulatory-alerts", selectedStatus],
    queryFn: () => complianceService.getAlerts({ status: selectedStatus }),
  });

  const acknowledgeMutation = useMutation({
    mutationFn: (alertId: string) => complianceService.acknowledgeAlert(alertId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["regulatory-alerts"] }),
  });

  if (isLoading) {
    return <div className="animate-pulse">Cargando alertas...</div>;
  }

  return (
    <div className="space-y-6">
      {/* Filtros por estado */}
      <Tabs value={selectedStatus} onValueChange={setSelectedStatus}>
        <TabsList>
          <TabsTrigger value="pending">Pendientes</TabsTrigger>
          <TabsTrigger value="in_progress">En Progreso</TabsTrigger>
          <TabsTrigger value="resolved">Resueltas</TabsTrigger>
          <TabsTrigger value="all">Todas</TabsTrigger>
        </TabsList>
      </Tabs>

      {/* Lista de alertas */}
      <div className="space-y-4">
        {alerts?.items?.map((alert) => {
          const severity = severityConfig[alert.severity as keyof typeof severityConfig];
          const status = statusConfig[alert.status as keyof typeof statusConfig];
          const SeverityIcon = severity?.icon || Info;

          return (
            <Card key={alert.id} className="p-4 hover:shadow-md transition-shadow">
              <div className="flex items-start gap-4">
                {/* Icono de severidad */}
                <div className={`p-2 rounded-lg ${severity?.color}`}>
                  <SeverityIcon size={20} />
                </div>

                {/* Contenido */}
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-1">
                    <h3 className="font-semibold text-gray-900 truncate">
                      {alert.title}
                    </h3>
                    <Badge className={status?.color}>{status?.label}</Badge>
                    <Badge variant="outline">{alert.category}</Badge>
                  </div>

                  <p className="text-sm text-gray-600 mb-2 line-clamp-2">
                    {alert.summary}
                  </p>

                  <div className="flex items-center gap-4 text-xs text-gray-500">
                    <span className="flex items-center gap-1">
                      <FileText size={12} />
                      {alert.source?.name}
                    </span>
                    <span className="flex items-center gap-1">
                      <Clock size={12} />
                      {formatDistanceToNow(new Date(alert.detectedAt), {
                        addSuffix: true,
                        locale: es
                      })}
                    </span>
                    <span>Score: {alert.impactScore}/100</span>
                    {alert.matchedKeywords?.length > 0 && (
                      <span>Keywords: {alert.matchedKeywords.join(", ")}</span>
                    )}
                  </div>

                  {alert.actionDeadline && (
                    <div className="mt-2 text-xs text-red-600 flex items-center gap-1">
                      <AlertTriangle size={12} />
                      Deadline: {new Date(alert.actionDeadline).toLocaleDateString("es-DO")}
                    </div>
                  )}
                </div>

                {/* Acciones */}
                <div className="flex flex-col gap-2">
                  {alert.documentUrl && (
                    <Button size="sm" variant="outline" asChild>
                      <a href={alert.documentUrl} target="_blank" rel="noopener noreferrer">
                        <ExternalLink size={14} className="mr-1" />
                        Ver Doc
                      </a>
                    </Button>
                  )}

                  {alert.status === "New" && (
                    <Button
                      size="sm"
                      onClick={() => acknowledgeMutation.mutate(alert.id)}
                      disabled={acknowledgeMutation.isPending}
                    >
                      <Eye size={14} className="mr-1" />
                      Acusar Recibo
                    </Button>
                  )}

                  <Button size="sm" variant="ghost" asChild>
                    <a href={`/admin/compliance/alerts/${alert.id}`}>
                      Ver Detalle
                    </a>
                  </Button>
                </div>
              </div>
            </Card>
          );
        })}

        {alerts?.items?.length === 0 && (
          <div className="text-center py-12 text-gray-500">
            <CheckCircle className="mx-auto h-12 w-12 text-green-400 mb-4" />
            <p>No hay alertas {selectedStatus === "pending" ? "pendientes" : ""}</p>
          </div>
        )}
      </div>
    </div>
  );
}
```

### PASO 2: ComplianceDashboard

```typescript
// filepath: src/components/admin/compliance/ComplianceDashboard.tsx
"use client";

import { useQuery } from "@tanstack/react-query";
import { Card } from "@/components/ui/Card";
import {
  AlertTriangle,
  AlertCircle,
  CheckCircle,
  Clock,
  TrendingUp,
  Calendar
} from "lucide-react";
import { complianceService } from "@/lib/services/complianceService";

export function ComplianceDashboard() {
  const { data: metrics } = useQuery({
    queryKey: ["compliance-dashboard"],
    queryFn: () => complianceService.getDashboard(),
  });

  const kpis = [
    {
      label: "Alertas Críticas",
      value: metrics?.criticalAlerts || 0,
      icon: AlertTriangle,
      color: "bg-red-500",
      subtext: "Requieren acción inmediata",
    },
    {
      label: "Alertas Pendientes",
      value: metrics?.pendingAlerts || 0,
      icon: AlertCircle,
      color: "bg-yellow-500",
      subtext: "Sin acusar recibo",
    },
    {
      label: "En Progreso",
      value: metrics?.inProgressAlerts || 0,
      icon: Clock,
      color: "bg-blue-500",
      subtext: "Con plan de acción",
    },
    {
      label: "Resueltas Este Mes",
      value: metrics?.resolvedThisMonth || 0,
      icon: CheckCircle,
      color: "bg-green-500",
      subtext: `${metrics?.avgResolutionTime || 0} días promedio`,
    },
  ];

  return (
    <div className="space-y-6">
      {/* KPIs */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {kpis.map((kpi) => (
          <Card key={kpi.label} className="p-6">
            <div className="flex items-center justify-between mb-4">
              <div className={`w-10 h-10 rounded-lg ${kpi.color} flex items-center justify-center`}>
                <kpi.icon size={20} className="text-white" />
              </div>
            </div>
            <p className="text-3xl font-bold text-gray-900">{kpi.value}</p>
            <p className="text-sm font-medium text-gray-600">{kpi.label}</p>
            <p className="text-xs text-gray-500 mt-1">{kpi.subtext}</p>
          </Card>
        ))}
      </div>

      {/* Próximos Deadlines */}
      <Card className="p-6">
        <h3 className="text-lg font-semibold mb-4 flex items-center gap-2">
          <Calendar size={20} />
          Próximos Deadlines
        </h3>
        <div className="space-y-3">
          {metrics?.upcomingDeadlines?.map((deadline) => (
            <div
              key={deadline.alertId}
              className="flex items-center justify-between p-3 bg-gray-50 rounded-lg"
            >
              <div>
                <p className="font-medium text-gray-900">{deadline.title}</p>
                <p className="text-sm text-gray-500">{deadline.category}</p>
              </div>
              <div className="text-right">
                <p className={`font-medium ${
                  deadline.daysRemaining <= 1 ? "text-red-600" :
                  deadline.daysRemaining <= 3 ? "text-orange-600" :
                  "text-gray-700"
                }`}>
                  {deadline.daysRemaining === 0 ? "HOY" :
                   deadline.daysRemaining === 1 ? "Mañana" :
                   `${deadline.daysRemaining} días`}
                </p>
                <p className="text-xs text-gray-500">
                  {new Date(deadline.deadline).toLocaleDateString("es-DO")}
                </p>
              </div>
            </div>
          ))}
        </div>
      </Card>

      {/* Estado de Fuentes */}
      <Card className="p-6">
        <h3 className="text-lg font-semibold mb-4 flex items-center gap-2">
          <TrendingUp size={20} />
          Estado de Fuentes Regulatorias
        </h3>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {metrics?.sourcesStatus?.map((source) => (
            <div
              key={source.id}
              className={`p-4 rounded-lg border ${
                source.isHealthy ? "border-green-200 bg-green-50" : "border-red-200 bg-red-50"
              }`}
            >
              <div className="flex items-center justify-between mb-2">
                <p className="font-medium">{source.name}</p>
                {source.isHealthy ? (
                  <CheckCircle size={16} className="text-green-600" />
                ) : (
                  <AlertTriangle size={16} className="text-red-600" />
                )}
              </div>
              <p className="text-xs text-gray-600">
                Último escaneo: {source.lastScanned
                  ? new Date(source.lastScanned).toLocaleString("es-DO")
                  : "Nunca"}
              </p>
              <p className="text-xs text-gray-500">
                Próximo: {source.nextScan
                  ? new Date(source.nextScan).toLocaleString("es-DO")
                  : "No programado"}
              </p>
            </div>
          ))}
        </div>
      </Card>
    </div>
  );
}
```

---

## 📄 PÁGINAS

### Página de Alertas

```typescript
// filepath: src/app/(admin)/admin/compliance/alerts/page.tsx
import { Metadata } from "next";
import { RegulatoryAlertsList } from "@/components/admin/compliance/RegulatoryAlertsList";
import { ComplianceDashboard } from "@/components/admin/compliance/ComplianceDashboard";

export const metadata: Metadata = {
  title: "Alertas Regulatorias | Admin OKLA",
};

export default function ComplianceAlertsPage() {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Alertas Regulatorias</h1>
      </div>

      <ComplianceDashboard />

      <RegulatoryAlertsList />
    </div>
  );
}
```

---

## 📡 EVENTOS RABBITMQ

| Evento                             | Exchange            | Descripción      |
| ---------------------------------- | ------------------- | ---------------- |
| `RegulatoryAlertCreatedEvent`      | `compliance.events` | Nueva alerta     |
| `RegulatoryAlertAcknowledgedEvent` | `compliance.events` | Alerta acusada   |
| `RegulatoryAlertResolvedEvent`     | `compliance.events` | Alerta resuelta  |
| `SourceScanFailedEvent`            | `compliance.events` | Fallo de escaneo |

---

## 📊 REGLAS DE NEGOCIO

| Código  | Regla                                  | Validación          |
| ------- | -------------------------------------- | ------------------- |
| REG-R01 | Alerta crítica requiere acuse en 4h    | Deadline check      |
| REG-R02 | Fuente fallida 5 veces se desactiva    | FailedScanCount     |
| REG-R03 | Solo Compliance puede resolver alertas | Role check          |
| REG-R04 | Resolución requiere documentación      | Resolution required |
| REG-R05 | Keywords con weight >= 3 = High        | Weight calculation  |
| REG-R06 | Escanear DGII diario, otros semanal    | Frequency config    |

---

## ⚙️ CONFIGURACIÓN

```json
{
  "RegulatoryAlerts": {
    "MinImpactScoreForAlert": 30,
    "SeverityThresholds": {
      "Low": 30,
      "Medium": 40,
      "High": 60,
      "Critical": 80
    },
    "AcknowledgeDeadlines": {
      "Critical": 4,
      "High": 24,
      "Medium": 72,
      "Low": 168
    },
    "DefaultScanFrequencyHours": {
      "DGII": 24,
      "ProConsumidor": 24,
      "SB": 168,
      "DGCP": 168
    },
    "NotifyOnSeverity": ["Medium", "High", "Critical"],
    "MaxFailedScansBeforeDisable": 5
  }
}
```

---

## 📊 MÉTRICAS PROMETHEUS

```
# Alerts
regulatory_alerts_total{category, severity}
regulatory_alerts_pending{status}

# Processing
regulatory_source_scan_duration_seconds{source}
regulatory_source_scan_failures_total{source}

# Response time
regulatory_alert_acknowledge_time_seconds{severity}
regulatory_alert_resolution_time_seconds{severity}
```

---

## ✅ VALIDACIÓN

```bash
pnpm dev
# Verificar:
# - /admin/compliance/alerts muestra dashboard y lista
# - KPIs cargan correctamente
# - Filtros por estado funcionan
# - Acusar recibo funciona
# - Links a documentos externos abren
# - Deadlines se calculan correctamente
# - Estado de fuentes se muestra
```

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/admin-compliance-alerts.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsAdmin } from "../helpers/auth";

test.describe("Admin Compliance Alerts", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("debe mostrar alertas de compliance", async ({ page }) => {
    await page.goto("/admin/compliance/alerts");

    await expect(page.getByTestId("compliance-alerts")).toBeVisible();
  });

  test("debe filtrar por severidad", async ({ page }) => {
    await page.goto("/admin/compliance/alerts");

    await page.getByRole("combobox", { name: /severidad/i }).click();
    await page.getByRole("option", { name: /crítica/i }).click();

    await expect(page).toHaveURL(/severity=critical/);
  });

  test("debe ver detalle de alerta", async ({ page }) => {
    await page.goto("/admin/compliance/alerts");

    await page.getByTestId("alert-row").first().click();
    await expect(page.getByTestId("alert-detail")).toBeVisible();
  });

  test("debe marcar alerta como resuelta", async ({ page }) => {
    await page.goto("/admin/compliance/alerts");
    await page.getByTestId("alert-row").first().click();

    await page.fill(
      'textarea[name="resolution"]',
      "Problema corregido, documentos actualizados",
    );
    await page.getByRole("button", { name: /resolver/i }).click();

    await expect(page.getByText(/alerta resuelta/i)).toBeVisible();
  });

  test("debe ver historial de alertas", async ({ page }) => {
    await page.goto("/admin/compliance/alerts/history");

    await expect(page.getByTestId("alerts-history")).toBeVisible();
  });
});
```

---

## 📚 REFERENCIAS

- [docs/process-matrix/09-REPORTES-ANALYTICS/05-regulatory-alerts.md](../../process-matrix/09-REPORTES-ANALYTICS/05-regulatory-alerts.md)
- [docs/process-matrix/08-COMPLIANCE-LEGAL-RD/01-compliance-service.md](../../process-matrix/08-COMPLIANCE-LEGAL-RD/01-compliance-service.md)

---

**Siguiente documento:** Continuar con otros módulos de compliance
