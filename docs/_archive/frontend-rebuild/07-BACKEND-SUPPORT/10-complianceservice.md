# ⚖️ ComplianceService - Documentación Frontend

> **Servicio:** ComplianceService  
> **Puerto:** 5102 (dev) / 8080 (k8s)  
> **Estado:** ✅ Implementado  
> **Regulaciones:** DGII, Ley 155-17, Pro-Consumidor  
> **Última actualización:** Enero 2026

---

## 📋 Descripción

Servicio de gestión de compliance regulatorio para República Dominicana. Administra marcos regulatorios, requerimientos, auditorías y reportes para cumplir con DGII, Ley 155-17 (prevención lavado), Pro-Consumidor y otras regulaciones aplicables al marketplace automotriz.

---

## 🎯 Casos de Uso Frontend

### 1. Dashboard de Compliance (Admin)

```typescript
// Ver estado general de compliance
const ComplianceDashboard = () => {
  const { data: frameworks } = useRegulatoryFrameworks();
  const { data: upcomingDeadlines } = useUpcomingDeadlines(30);
  const { data: audits } = useRecentAudits();

  return (
    <div className="grid grid-cols-3 gap-6">
      <ComplianceScoreCard score={calculateOverallScore(frameworks)} />
      <DeadlinesCard deadlines={upcomingDeadlines} />
      <AuditsCard audits={audits} />
    </div>
  );
};
```

### 2. Verificación de Dealer

```typescript
// Verificar que dealer cumple requisitos para operar
const checkDealerCompliance = async (dealerId: string) => {
  const status = await complianceService.getDealerStatus(dealerId);

  if (!status.isCompliant) {
    const missing = status.missingRequirements.map((r) => r.name);
    throw new Error(`Requisitos faltantes: ${missing.join(", ")}`);
  }

  return status;
};
```

### 3. Alertas de Compliance

```typescript
// Widget de alertas para admin
const ComplianceAlerts = () => {
  const { data: alerts } = useComplianceAlerts();

  const critical = alerts?.filter(a => a.severity === 'Critical');

  if (critical?.length > 0) {
    return (
      <Alert variant="destructive">
        <AlertTitle>⚠️ {critical.length} alertas críticas</AlertTitle>
        <AlertDescription>
          Hay requisitos de compliance que requieren atención inmediata.
        </AlertDescription>
      </Alert>
    );
  }

  return null;
};
```

---

## 📡 API Endpoints

### Regulatory Frameworks

| Método | Endpoint                         | Descripción                |
| ------ | -------------------------------- | -------------------------- |
| `GET`  | `/api/frameworks`                | Listar marcos regulatorios |
| `GET`  | `/api/frameworks/{id}`           | Detalle de marco           |
| `GET`  | `/api/frameworks/by-type/{type}` | Marcos por tipo            |
| `POST` | `/api/frameworks`                | Crear marco (Admin)        |
| `PUT`  | `/api/frameworks/{id}`           | Actualizar marco (Admin)   |

### Requirements

| Método | Endpoint                               | Descripción                 |
| ------ | -------------------------------------- | --------------------------- |
| `GET`  | `/api/requirements/by-framework/{id}`  | Requerimientos por marco    |
| `GET`  | `/api/requirements/{id}`               | Detalle de requerimiento    |
| `GET`  | `/api/requirements/upcoming-deadlines` | Próximos vencimientos       |
| `POST` | `/api/requirements`                    | Crear requerimiento (Admin) |
| `PUT`  | `/api/requirements/{id}`               | Actualizar (Admin)          |

### Compliance Status

| Método | Endpoint                                   | Descripción          |
| ------ | ------------------------------------------ | -------------------- |
| `GET`  | `/api/compliance/status/dealer/{dealerId}` | Estado de dealer     |
| `GET`  | `/api/compliance/status/platform`          | Estado de plataforma |
| `GET`  | `/api/compliance/score`                    | Score general        |

### Audits

| Método | Endpoint                    | Descripción             |
| ------ | --------------------------- | ----------------------- |
| `GET`  | `/api/audits`               | Listar auditorías       |
| `GET`  | `/api/audits/{id}`          | Detalle de auditoría    |
| `POST` | `/api/audits`               | Crear auditoría (Admin) |
| `PUT`  | `/api/audits/{id}/complete` | Completar auditoría     |

### Documents

| Método | Endpoint                         | Descripción         |
| ------ | -------------------------------- | ------------------- |
| `GET`  | `/api/compliance-documents`      | Listar documentos   |
| `POST` | `/api/compliance-documents`      | Subir documento     |
| `GET`  | `/api/compliance-documents/{id}` | Descargar documento |

---

## 🔧 Cliente TypeScript

```typescript
// services/complianceService.ts

import { apiClient } from "./apiClient";

// Enums
export type RegulationType =
  | "Tax" // DGII
  | "AntiMoneyLaundering" // Ley 155-17
  | "ConsumerProtection" // Pro-Consumidor
  | "DataProtection" // Ley 172-13
  | "ECommerce" // Ley 126-02
  | "Automotive"; // Regulaciones automotrices

export type ComplianceStatus =
  | "Compliant"
  | "NonCompliant"
  | "Pending"
  | "Expired";
export type RequirementPriority = "Critical" | "High" | "Medium" | "Low";

// Tipos
interface RegulatoryFramework {
  id: string;
  name: string;
  code: string; // Ej: "LEY-155-17"
  type: RegulationType;
  description: string;
  effectiveDate: string;
  version: string;
  isActive: boolean;
  requirements: ComplianceRequirement[];
}

interface ComplianceRequirement {
  id: string;
  frameworkId: string;
  name: string;
  description: string;
  priority: RequirementPriority;
  deadline?: string;
  frequency: "Once" | "Daily" | "Weekly" | "Monthly" | "Quarterly" | "Annually";
  documentRequired: boolean;
  status: ComplianceStatus;
  lastVerifiedAt?: string;
  nextDueAt?: string;
}

interface DealerComplianceStatus {
  dealerId: string;
  isCompliant: boolean;
  overallScore: number; // 0-100
  frameworks: FrameworkStatus[];
  missingRequirements: ComplianceRequirement[];
  expiringRequirements: ComplianceRequirement[];
  lastAuditAt?: string;
}

interface FrameworkStatus {
  frameworkId: string;
  frameworkName: string;
  status: ComplianceStatus;
  completedRequirements: number;
  totalRequirements: number;
  score: number;
}

interface ComplianceAudit {
  id: string;
  entityType: "Platform" | "Dealer" | "User";
  entityId: string;
  frameworkId?: string;
  status: "Scheduled" | "InProgress" | "Completed" | "Failed";
  findings: AuditFinding[];
  scheduledAt: string;
  completedAt?: string;
  auditor?: string;
}

interface AuditFinding {
  requirementId: string;
  status: "Pass" | "Fail" | "Observation";
  notes: string;
  evidence?: string[];
}

interface ComplianceDocument {
  id: string;
  name: string;
  type: string;
  requirementId?: string;
  uploadedBy: string;
  uploadedAt: string;
  expiresAt?: string;
  fileUrl: string;
}

export const complianceService = {
  // === FRAMEWORKS ===

  async getFrameworks(
    includeInactive: boolean = false,
  ): Promise<RegulatoryFramework[]> {
    const response = await apiClient.get("/api/frameworks", {
      params: { includeInactive },
    });
    return response.data;
  },

  async getFramework(id: string): Promise<RegulatoryFramework> {
    const response = await apiClient.get(`/api/frameworks/${id}`);
    return response.data;
  },

  async getFrameworksByType(
    type: RegulationType,
  ): Promise<RegulatoryFramework[]> {
    const response = await apiClient.get(`/api/frameworks/by-type/${type}`);
    return response.data;
  },

  // === REQUIREMENTS ===

  async getRequirementsByFramework(
    frameworkId: string,
  ): Promise<ComplianceRequirement[]> {
    const response = await apiClient.get(
      `/api/requirements/by-framework/${frameworkId}`,
    );
    return response.data;
  },

  async getUpcomingDeadlines(
    daysAhead: number = 30,
  ): Promise<ComplianceRequirement[]> {
    const response = await apiClient.get(
      "/api/requirements/upcoming-deadlines",
      {
        params: { daysAhead },
      },
    );
    return response.data;
  },

  // === STATUS ===

  async getDealerStatus(dealerId: string): Promise<DealerComplianceStatus> {
    const response = await apiClient.get(
      `/api/compliance/status/dealer/${dealerId}`,
    );
    return response.data;
  },

  async getPlatformStatus(): Promise<{
    isCompliant: boolean;
    score: number;
    frameworks: FrameworkStatus[];
  }> {
    const response = await apiClient.get("/api/compliance/status/platform");
    return response.data;
  },

  // === AUDITS ===

  async getAudits(params?: {
    entityType?: string;
    status?: string;
    page?: number;
    pageSize?: number;
  }): Promise<{ items: ComplianceAudit[]; totalCount: number }> {
    const response = await apiClient.get("/api/audits", { params });
    return response.data;
  },

  async getAudit(id: string): Promise<ComplianceAudit> {
    const response = await apiClient.get(`/api/audits/${id}`);
    return response.data;
  },

  async createAudit(data: {
    entityType: "Platform" | "Dealer" | "User";
    entityId: string;
    frameworkId?: string;
    scheduledAt: string;
  }): Promise<ComplianceAudit> {
    const response = await apiClient.post("/api/audits", data);
    return response.data;
  },

  // === DOCUMENTS ===

  async uploadDocument(
    file: File,
    requirementId?: string,
  ): Promise<ComplianceDocument> {
    const formData = new FormData();
    formData.append("file", file);
    if (requirementId) formData.append("requirementId", requirementId);

    const response = await apiClient.post(
      "/api/compliance-documents",
      formData,
      {
        headers: { "Content-Type": "multipart/form-data" },
      },
    );
    return response.data;
  },

  async getDocuments(requirementId?: string): Promise<ComplianceDocument[]> {
    const response = await apiClient.get("/api/compliance-documents", {
      params: { requirementId },
    });
    return response.data;
  },

  // === HELPERS ===

  getStatusColor(status: ComplianceStatus): string {
    const colors = {
      Compliant: "green",
      NonCompliant: "red",
      Pending: "yellow",
      Expired: "orange",
    };
    return colors[status];
  },

  getPriorityIcon(priority: RequirementPriority): string {
    const icons = {
      Critical: "🔴",
      High: "🟠",
      Medium: "🟡",
      Low: "🟢",
    };
    return icons[priority];
  },
};
```

---

## 🪝 Hooks de React

```typescript
// hooks/useCompliance.ts

import { useQuery, useMutation } from "@tanstack/react-query";
import {
  complianceService,
  RegulationType,
} from "../services/complianceService";

export function useRegulatoryFrameworks(includeInactive: boolean = false) {
  return useQuery({
    queryKey: ["regulatory-frameworks", includeInactive],
    queryFn: () => complianceService.getFrameworks(includeInactive),
  });
}

export function useFrameworksByType(type: RegulationType) {
  return useQuery({
    queryKey: ["frameworks-by-type", type],
    queryFn: () => complianceService.getFrameworksByType(type),
  });
}

export function useUpcomingDeadlines(daysAhead: number = 30) {
  return useQuery({
    queryKey: ["upcoming-deadlines", daysAhead],
    queryFn: () => complianceService.getUpcomingDeadlines(daysAhead),
    refetchInterval: 60000, // Refrescar cada minuto
  });
}

export function useDealerCompliance(dealerId: string) {
  return useQuery({
    queryKey: ["dealer-compliance", dealerId],
    queryFn: () => complianceService.getDealerStatus(dealerId),
    enabled: !!dealerId,
  });
}

export function usePlatformCompliance() {
  return useQuery({
    queryKey: ["platform-compliance"],
    queryFn: () => complianceService.getPlatformStatus(),
  });
}

export function useComplianceAudits(params?: {
  entityType?: string;
  status?: string;
}) {
  return useQuery({
    queryKey: ["compliance-audits", params],
    queryFn: () => complianceService.getAudits(params),
  });
}
```

---

## 🧩 Componentes de Ejemplo

### Compliance Score Card

```tsx
// components/ComplianceScoreCard.tsx

interface ComplianceScoreCardProps {
  score: number;
  status: ComplianceStatus;
  frameworkName: string;
  completedRequirements: number;
  totalRequirements: number;
}

export function ComplianceScoreCard({
  score,
  status,
  frameworkName,
  completedRequirements,
  totalRequirements,
}: ComplianceScoreCardProps) {
  const getScoreColor = (score: number) => {
    if (score >= 90) return "text-green-600";
    if (score >= 70) return "text-yellow-600";
    return "text-red-600";
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center justify-between">
          {frameworkName}
          <Badge variant={status === "Compliant" ? "success" : "destructive"}>
            {status}
          </Badge>
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="text-center">
          <p className={`text-5xl font-bold ${getScoreColor(score)}`}>
            {score}%
          </p>
          <p className="text-sm text-gray-500 mt-2">
            {completedRequirements} de {totalRequirements} requisitos cumplidos
          </p>
        </div>

        <Progress value={score} className="mt-4" />
      </CardContent>
    </Card>
  );
}
```

### Upcoming Deadlines

```tsx
// components/UpcomingDeadlines.tsx

import { useUpcomingDeadlines } from "../hooks/useCompliance";
import { formatDistanceToNow } from "date-fns";
import { es } from "date-fns/locale";

export function UpcomingDeadlines() {
  const { data: deadlines, isLoading } = useUpcomingDeadlines(30);

  if (isLoading) return <Skeleton count={3} />;
  if (!deadlines?.length)
    return <EmptyState message="No hay vencimientos próximos" />;

  return (
    <Card>
      <CardHeader>
        <CardTitle>📅 Próximos Vencimientos</CardTitle>
      </CardHeader>
      <CardContent>
        <ul className="space-y-3">
          {deadlines.map((req) => (
            <li
              key={req.id}
              className="flex items-center justify-between py-2 border-b"
            >
              <div className="flex items-center gap-2">
                <span>{complianceService.getPriorityIcon(req.priority)}</span>
                <div>
                  <p className="font-medium">{req.name}</p>
                  <p className="text-sm text-gray-500">{req.description}</p>
                </div>
              </div>
              <div className="text-right">
                <p className="text-sm font-medium">
                  {formatDistanceToNow(new Date(req.nextDueAt!), {
                    addSuffix: true,
                    locale: es,
                  })}
                </p>
                <p className="text-xs text-gray-400">
                  {new Date(req.nextDueAt!).toLocaleDateString()}
                </p>
              </div>
            </li>
          ))}
        </ul>
      </CardContent>
    </Card>
  );
}
```

---

## 📊 Marcos Regulatorios Principales

| Marco              | Código     | Tipo      | Descripción                                |
| ------------------ | ---------- | --------- | ------------------------------------------ |
| **DGII**           | LEY-11-92  | Tax       | Código Tributario, facturación electrónica |
| **Ley 155-17**     | LEY-155-17 | AML       | Prevención lavado de activos               |
| **Pro-Consumidor** | LEY-358-05 | Consumer  | Protección al consumidor                   |
| **Ley 172-13**     | LEY-172-13 | Data      | Protección de datos personales             |
| **Ley 126-02**     | LEY-126-02 | ECommerce | Comercio electrónico                       |

---

## 🧪 Testing

### E2E Test (Playwright)

```typescript
// e2e/compliance.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Compliance Dashboard", () => {
  test("should show compliance score", async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto("/admin/compliance");

    await expect(
      page.locator('[data-testid="compliance-score"]'),
    ).toBeVisible();
    await expect(
      page.locator('[data-testid="frameworks-list"]'),
    ).toHaveCount.greaterThan(0);
  });

  test("should show upcoming deadlines", async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto("/admin/compliance");

    const deadlines = page.locator('[data-testid="upcoming-deadlines"]');
    await expect(deadlines).toBeVisible();
  });
});
```

---

## 🔗 Referencias

- [DGII Compliance](../04-PAGINAS/08-DGII-COMPLIANCE/)
- [KYCService](./05-kycservice.md)
- [AuditService](./04-auditservice.md)

---

_El cumplimiento regulatorio es crítico para operar legalmente en RD._
