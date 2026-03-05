---
title: "Admin - Compliance"
priority: P2
estimated_time: "40 minutos"
dependencies: []
apis: []
status: partial
last_updated: "2026-01-30"
---

# ✅ Admin - Compliance

> **Tiempo estimado:** 40 minutos
> **Prerrequisitos:** Admin layout, ComplianceService
> **Roles:** ADM-COMP, ADM-ADMIN, ADM-SUPER
> **Última Auditoría:** Enero 29, 2026

---

## 🔍 AUDITORÍA DE IMPLEMENTACIÓN (Enero 29, 2026)

### Estado de Cumplimiento Legal por Servicio

| Ley/Servicio                  | Backend | Frontend UI | Brecha   | Prioridad   |
| ----------------------------- | ------- | ----------- | -------- | ----------- |
| **Ley 155-17 (AML/PLD)**      | ✅ 80%  | 🟡 40%      | **-40%** | 🔴 CRÍTICA  |
| **Ley 172-13 (Privacidad)**   | ✅ 90%  | ✅ 95%      | **+5%**  | ✅ COMPLETO |
| **Ley 11-92 (DGII 607)**      | 🟡 60%  | 🔴 0%       | **-60%** | 🔴 CRÍTICA  |
| **ComplianceService General** | 🟢 63%  | 🔴 5%       | **-58%** | 🔴 CRÍTICA  |

### 🔴 PÁGINAS CRÍTICAS FALTANTES

El rol **ADM-COMP (Compliance Officer)** NO tiene dashboard dedicado:

| Ruta Propuesta                   | Funcionalidad          | Backend | UI    | Prioridad  |
| -------------------------------- | ---------------------- | ------- | ----- | ---------- |
| `/admin/compliance/dashboard`    | Dashboard compliance   | ✅ 100% | 🔴 0% | 🔴 CRÍTICA |
| `/admin/compliance/dgii/607`     | Formato 607 DGII       | ✅ 80%  | 🔴 0% | 🔴 CRÍTICA |
| `/admin/compliance/transactions` | Monitoreo > $500K      | 🟡 60%  | 🔴 0% | 🔴 CRÍTICA |
| `/admin/compliance/alerts`       | Alertas de umbral      | 🟡 60%  | 🔴 0% | 🔴 CRÍTICA |
| `/admin/compliance/aml`          | Dashboard AML          | ✅ 80%  | 🔴 0% | 🔴 ALTA    |
| `/admin/compliance/risks`        | Risk Assessment        | ✅ 100% | 🔴 0% | 🔴 ALTA    |
| `/admin/compliance/calendar`     | Calendario regulatorio | ✅ 100% | 🔴 0% | 🟡 MEDIA   |
| `/admin/compliance/training`     | Capacitaciones PLD     | ✅ 100% | 🔴 0% | 🟡 MEDIA   |

### ✅ PÁGINAS IMPLEMENTADAS (Parcial)

| Ruta Actual                   | Componente         | Funcionalidad | Estado |
| ----------------------------- | ------------------ | ------------- | ------ |
| `/admin/compliance/watchlist` | WatchlistAdminPage | Gestión PEPs  | ✅ 95% |
| `/admin/compliance/str`       | STRReportsPage     | Reportes UAF  | ✅ 90% |
| `/admin/kyc/queue`            | KYCAdminQueuePage  | Cola KYC      | ✅ 95% |
| `/admin/kyc/review/{id}`      | KYCAdminReviewPage | Revisión KYC  | ✅ 95% |

### 📊 Procesos vs UI (Ley 155-17)

| Proceso                              | Backend | UI Access             | Observación         |
| ------------------------------------ | ------- | --------------------- | ------------------- |
| **AML-KYC-001** Verificación         | ✅ 100% | ✅ VerificationPage   | COMPLETO            |
| **AML-DDC-001** Due Diligence        | ✅ 100% | 🟡 Básico             | Falta DDC reforzada |
| **AML-ROS-001** Reportes UAF         | ✅ 100% | ✅ STRReportsPage     | COMPLETO            |
| **AML-UMBRAL-001** Monitoreo > $500K | 🟡 80%  | 🔴 Sin UI             | SIN DASHBOARD       |
| **COMP-001** Reporte 607 DGII        | ✅ 80%  | 🔴 Sin UI             | SIN GENERADOR       |
| **RISK-001** Risk Assessment         | ✅ 100% | 🔴 Sin UI             | SIN DASHBOARD       |
| **WL-001** Watchlist Check           | ✅ 100% | ✅ WatchlistAdminPage | COMPLETO            |

### 📋 Plan de Cierre de Brechas (26 SP)

**Sprint Inmediato:** Cerrar 4 brechas críticas

1. **ComplianceDashboardPage** (8 SP)
   - Dashboard principal ADM-COMP
   - Métricas: STRs pendientes, alertas umbral, vencimientos, % staff capacitado
   - Timeline de actividad
   - Quick actions

2. **DGII607Page** (5 SP)
   - Generador de formato 607
   - Selector de período
   - Preview de transacciones
   - Validación y download .txt

3. **TransactionMonitoringPage** (8 SP)
   - Tabla de transacciones > $100K
   - Filtros y búsqueda
   - Risk score por transacción
   - Acciones: Revisar, Crear STR

4. **AlertsDashboardPage** (5 SP)
   - Grid de alertas activas
   - Tipos: SINGLE_LARGE, MULTIPLE_24H, STRUCTURING, PATTERN_CHANGE
   - Badge de prioridad
   - Link a investigación

---

## 🎨 WIREFRAME - DASHBOARD COMPLIANCE

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ ┌──────────┐                                                                │
│ │ SIDEBAR  │  COMPLIANCE DASHBOARD                          🔔 3 Alertas   │
│ │          │ ────────────────────────────────────────────────────────────── │
│ │ 📊 Dash  │                                                                │
│ │ 👥 Users │  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌───────────┐│
│ │ 🛡️ Mod   │  │ 🚨 5        │ │ ⏳ 12       │ │ 📋 3        │ │ ⚖️ 98%    ││
│ │ ✅ Comp ◀│  │ STRs        │ │ KYC Pend.   │ │ Vencimientos│ │ Staff     ││
│ │ ⚙️ System│  │ pendientes  │ │ revisión    │ │ próximos    │ │ capacitado││
│ │          │  └─────────────┘ └─────────────┘ └─────────────┘ └───────────┘│
│ │ ──────── │                                                                │
│ │ 💰 DGII  │  ┌──────────────────────────────┐ ┌───────────────────────────┐│
│ │ 🔍 AML   │  │ 📊 TRANSACCIONES MONITOREADAS │ │ 🚨 ALERTAS ACTIVAS        ││
│ │ 📋 STRs  │  │                              │ │                           ││
│ │ 📅 Calendar│  │  Transacciones > RD$500K:    │ │ 🔴 SINGLE_LARGE - 2       ││
│ │          │  │                              │ │ 🟠 STRUCTURING - 1        ││
│ │          │  │  ▓▓▓▓▓▓░░░░░░░░░░░░░  8     │ │ 🟡 PATTERN_CHANGE - 0     ││
│ │          │  │  Total: RD$12.5M            │ │ 🔵 MULTIPLE_24H - 0       ││
│ │          │  │                              │ │                           ││
│ │          │  │  [Ver todas →]               │ │ [Investigar alertas →]    ││
│ │          │  └──────────────────────────────┘ └───────────────────────────┘│
│ │          │                                                                │
│ │          │  ┌──────────────────────────────┐ ┌───────────────────────────┐│
│ │          │  │ 📅 CALENDARIO REGULATORIO    │ │ 📋 DGII - PRÓXIMO 607     ││
│ │          │  │                              │ │                           ││
│ │          │  │ 📌 Feb 15 - Entrega 607      │ │ Período: Enero 2026       ││
│ │          │  │ 📌 Feb 28 - Capacitación AML │ │ Transacciones: 245        ││
│ │          │  │ 📌 Mar 01 - Auditoría UAF    │ │ Monto total: RD$45.2M     ││
│ │          │  │                              │ │                           ││
│ │          │  │ [Ver calendario completo →]  │ │ [Generar 607 →]           ││
│ │          │  └──────────────────────────────┘ └───────────────────────────┘│
│ └──────────┘                                                                │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

**Referencias:**

- Auditoría completa: `docs/process-matrix/08-COMPLIANCE-LEGAL-RD/01-compliance-service.md`
- Ley 155-17: `docs/process-matrix/08-COMPLIANCE-LEGAL-RD/01-ley-155-17.md`
- Ley 172-13: `docs/process-matrix/08-COMPLIANCE-LEGAL-RD/02-ley-172-13.md`

---

## ✅ INTEGRACIÓN CON SERVICIOS DE RESOLUCIÓN

Este documento complementa:

- [process-matrix/15-CONFIANZA-SEGURIDAD/04-disputas-mediacion.md](../../process-matrix/15-CONFIANZA-SEGURIDAD/04-disputas-mediacion.md) - **Disputas** ⭐
- [process-matrix/15-CONFIANZA-SEGURIDAD/02-garantia-inspeccion.md](../../process-matrix/15-CONFIANZA-SEGURIDAD/02-garantia-inspeccion.md) - **Garantías** ⭐
- [process-matrix/15-CONFIANZA-SEGURIDAD/03-devolucion-cancelacion.md](../../process-matrix/15-CONFIANZA-SEGURIDAD/03-devolucion-cancelacion.md) - **Devoluciones** ⭐

**Estado:** ✅ DisputeService 80% BE | ✅ WarrantyService 100% BE | 🔴 ReturnService 0%

### Servicios de Mediación y Resolución

| Servicio              | Puerto | Función               | Estado              |
| --------------------- | ------ | --------------------- | ------------------- |
| DisputeService        | 5089   | Mediación de disputas | ✅ 80% BE + 0% UI   |
| WarrantyService       | 5083   | Reclamos de garantía  | ✅ 100% BE + 40% UI |
| TrustService          | 5082   | Devoluciones          | 🔴 0% (planificado) |
| FraudDetectionService | 5062   | Detección de fraude   | 🟡 Planificado      |

### Endpoints para Admin Compliance

| Método | Endpoint                           | Descripción         | Servicio        |
| ------ | ---------------------------------- | ------------------- | --------------- |
| `GET`  | `/api/admin/disputes`              | Lista de disputas   | DisputeService  |
| `GET`  | `/api/admin/disputes/{id}`         | Detalle de caso     | DisputeService  |
| `PUT`  | `/api/disputes/{id}/resolve`       | Resolver disputa    | DisputeService  |
| `PUT`  | `/api/disputes/{id}/escalate`      | Escalar a legal     | DisputeService  |
| `GET`  | `/api/admin/warranty/claims`       | Reclamos pendientes | WarrantyService |
| `PUT`  | `/api/warranty/claims/{id}/review` | Revisar reclamo     | WarrantyService |
| `GET`  | `/api/admin/trust/fraud-reports`   | Reportes de fraude  | TrustService    |

### Nuevos Tabs en Compliance Dashboard

```tsx
<Tabs defaultValue="verifications">
  <TabsList>
    <TabsTrigger value="verifications">Verificaciones</TabsTrigger>
    <TabsTrigger value="documents">Documentos</TabsTrigger>
    <TabsTrigger value="disputes">Disputas</TabsTrigger> {/* NUEVO ⭐ */}
    <TabsTrigger value="warranties">Garantías</TabsTrigger> {/* NUEVO ⭐ */}
    <TabsTrigger value="fraud">Fraude</TabsTrigger> {/* NUEVO ⭐ */}
    <TabsTrigger value="audit">Auditoría</TabsTrigger>
  </TabsList>

  {/* ... existing tabs ... */}

  <TabsContent value="disputes" className="mt-6">
    <DisputeMediationQueue />
  </TabsContent>

  <TabsContent value="warranties" className="mt-6">
    <WarrantyClaimsReview />
  </TabsContent>

  <TabsContent value="fraud" className="mt-6">
    <FraudReportsTable />
  </TabsContent>
</Tabs>
```

### Métricas de Resolución

```tsx
// Agregar a ComplianceStats component
<StatCard
  icon={AlertTriangle}
  label="Disputas Activas"
  value={stats.activeDisputes}
  subtext={`${stats.pendingMediation} esperan mediación`}
  trend={stats.disputesTrend}
/>

<StatCard
  icon={Shield}
  label="Reclamos de Garantía"
  value={stats.warrantyClaimsTotal}
  subtext={`${stats.warrantyClaims Approved} aprobados este mes`}
  trend={stats.warrantyTrend}
/>

<StatCard
  icon={AlertCircle}
  label="Reportes de Fraude"
  value={stats.fraudReports}
  subtext={`${stats.fraudConfirmed} confirmados`}
  trend={stats.fraudTrend}
/>
```

### Procesos de DisputeService

| Proceso       | Código           | Pasos | Descripción                                  |
| ------------- | ---------------- | ----- | -------------------------------------------- |
| Crear Disputa | DISP-CREATE-001  | 4     | Usuario abre disputa sobre transacción       |
| Mediar Caso   | DISP-MEDIATE-001 | 5     | Admin revisa evidencia y propone resolución  |
| Resolver      | DISP-RESOLVE-001 | 4     | Aplicar resolución (reembolso, compensación) |
| Escalar       | DISP-ESCAL-001   | 3     | Escalar a equipo legal si no resuelve        |

### Tipos de Disputa a Gestionar

- 🚗 **ItemNotReceived** - Producto no entregado
- 📝 **NotAsDescribed** - Vehículo no coincide con anuncio
- ⚙️ **HiddenDefect** - Defecto mecánico no revelado
- 📄 **DocumentationIssue** - Problemas de documentación/título
- 💰 **PaymentNotReceived** - Vendedor no recibió pago
- ❌ **UnfairCancellation** - Cancelación injustificada
- 🚨 **Fraud** - Fraude o estafa

---

## 📋 OBJETIVO

Implementar herramientas de compliance:

- Verificación de dealers
- Documentos pendientes
- **Mediación de disputas** ⭐
- **Revisión de reclamos de garantía** ⭐
- **Gestión de reportes de fraude** ⭐
- Auditoría de transacciones
- Reportes de cumplimiento

---

## 🔧 PASO 1: Dashboard Compliance

```typescript
// filepath: src/app/(admin)/admin/compliance/page.tsx
import { Metadata } from "next";
import { Suspense } from "react";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/Tabs";
import { ComplianceStats } from "@/components/admin/compliance/ComplianceStats";
import { PendingVerifications } from "@/components/admin/compliance/PendingVerifications";
import { DocumentsReview } from "@/components/admin/compliance/DocumentsReview";
import { AuditLog } from "@/components/admin/compliance/AuditLog";
import { LoadingCard } from "@/components/ui/LoadingCard";

export const metadata: Metadata = {
  title: "Compliance | Admin OKLA",
};

export default function CompliancePage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Compliance</h1>
        <p className="text-gray-600">Verificación y cumplimiento regulatorio</p>
      </div>

      {/* Stats */}
      <Suspense fallback={<LoadingCard className="h-24" />}>
        <ComplianceStats />
      </Suspense>

      {/* Tabs */}
      <Tabs defaultValue="verifications">
        <TabsList>
          <TabsTrigger value="verifications">Verificaciones</TabsTrigger>
          <TabsTrigger value="documents">Documentos</TabsTrigger>
          <TabsTrigger value="audit">Auditoría</TabsTrigger>
        </TabsList>

        <TabsContent value="verifications" className="mt-6">
          <Suspense fallback={<LoadingCard className="h-96" />}>
            <PendingVerifications />
          </Suspense>
        </TabsContent>

        <TabsContent value="documents" className="mt-6">
          <Suspense fallback={<LoadingCard className="h-96" />}>
            <DocumentsReview />
          </Suspense>
        </TabsContent>

        <TabsContent value="audit" className="mt-6">
          <Suspense fallback={<LoadingCard className="h-96" />}>
            <AuditLog />
          </Suspense>
        </TabsContent>
      </Tabs>
    </div>
  );
}
```

---

## 🔧 PASO 2: PendingVerifications

```typescript
// filepath: src/components/admin/compliance/PendingVerifications.tsx
"use client";

import Link from "next/link";
import { Building2, Clock, FileCheck, AlertCircle } from "lucide-react";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { usePendingVerifications } from "@/lib/hooks/useCompliance";
import { formatRelativeDate } from "@/lib/utils";

const statusConfig = {
  pending: { label: "Pendiente", color: "warning" },
  documents_uploaded: { label: "Docs subidos", color: "info" },
  under_review: { label: "En revisión", color: "purple" },
} as const;

export function PendingVerifications() {
  const { data: verifications } = usePendingVerifications();

  return (
    <div className="bg-white rounded-xl border">
      <div className="p-4 border-b">
        <h3 className="font-semibold text-gray-900">Dealers pendientes de verificación</h3>
      </div>

      <div className="divide-y">
        {verifications?.map((verification) => (
          <div key={verification.id} className="p-4 flex items-center gap-4">
            {/* Icon */}
            <div className="w-12 h-12 rounded-xl bg-purple-100 flex items-center justify-center">
              <Building2 size={24} className="text-purple-600" />
            </div>

            {/* Info */}
            <div className="flex-1 min-w-0">
              <h4 className="font-medium text-gray-900">{verification.dealerName}</h4>
              <div className="flex items-center gap-2 text-sm text-gray-500">
                <span>RNC: {verification.rnc}</span>
                <span>•</span>
                <span className="flex items-center gap-1">
                  <Clock size={12} />
                  {formatRelativeDate(verification.submittedAt)}
                </span>
              </div>
            </div>

            {/* Status */}
            <Badge variant={statusConfig[verification.status as keyof typeof statusConfig]?.color}>
              {statusConfig[verification.status as keyof typeof statusConfig]?.label}
            </Badge>

            {/* Documents count */}
            <div className="flex items-center gap-1 text-sm text-gray-500">
              <FileCheck size={16} />
              {verification.documentsCount} docs
            </div>

            {/* Actions */}
            <Link href={`/admin/compliance/verificaciones/${verification.id}`}>
              <Button size="sm">Revisar</Button>
            </Link>
          </div>
        ))}

        {verifications?.length === 0 && (
          <div className="p-12 text-center text-gray-500">
            <FileCheck size={48} className="mx-auto mb-4 text-green-500" />
            <p>No hay verificaciones pendientes</p>
          </div>
        )}
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 3: Detalle de Verificación

```typescript
// filepath: src/app/(admin)/admin/compliance/verificaciones/[id]/page.tsx
import { Metadata } from "next";
import { notFound } from "next/navigation";
import { VerificationHeader } from "@/components/admin/compliance/VerificationHeader";
import { DealerInfo } from "@/components/admin/compliance/DealerInfo";
import { DocumentsList } from "@/components/admin/compliance/DocumentsList";
import { VerificationActions } from "@/components/admin/compliance/VerificationActions";
import { VerificationHistory } from "@/components/admin/compliance/VerificationHistory";
import { complianceService } from "@/lib/services/complianceService";

interface Props {
  params: Promise<{ id: string }>;
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { id } = await params;
  const verification = await complianceService.getVerificationById(id);
  return { title: verification ? `Verificar ${verification.dealerName}` : "No encontrado" };
}

export default async function VerificationDetailPage({ params }: Props) {
  const { id } = await params;
  const verification = await complianceService.getVerificationById(id);

  if (!verification) notFound();

  return (
    <div className="space-y-6">
      <VerificationHeader verification={verification} />

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Main Content */}
        <div className="lg:col-span-2 space-y-6">
          <DealerInfo dealer={verification.dealer} />
          <DocumentsList documents={verification.documents} />
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          <VerificationActions verification={verification} />
          <VerificationHistory verificationId={verification.id} />
        </div>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 4: DocumentsList

```typescript
// filepath: src/components/admin/compliance/DocumentsList.tsx
"use client";

import { useState } from "react";
import { FileText, Eye, Check, X, Download } from "lucide-react";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { Dialog } from "@/components/ui/Dialog";
import { useApproveDocument, useRejectDocument } from "@/lib/hooks/useCompliance";
import { formatDate } from "@/lib/utils";
import type { DealerDocument } from "@/types";

const documentTypeLabels = {
  RNC: "Certificado RNC",
  BusinessLicense: "Licencia Comercial",
  IdentificationCard: "Cédula Representante",
  ProofOfAddress: "Comprobante de Dirección",
  VehicleImportPermit: "Permiso de Importación",
};

const statusColors = {
  Pending: "warning",
  Approved: "success",
  Rejected: "danger",
} as const;

interface Props {
  documents: DealerDocument[];
}

export function DocumentsList({ documents }: Props) {
  const [selectedDoc, setSelectedDoc] = useState<DealerDocument | null>(null);
  const { approve, isApproving } = useApproveDocument();
  const { reject, isRejecting } = useRejectDocument();

  return (
    <div className="bg-white rounded-xl border">
      <div className="p-4 border-b">
        <h3 className="font-semibold text-gray-900">Documentos</h3>
      </div>

      <div className="divide-y">
        {documents.map((doc) => (
          <div key={doc.id} className="p-4 flex items-center gap-4">
            {/* Icon */}
            <div className="p-2 rounded-lg bg-gray-100">
              <FileText size={20} className="text-gray-600" />
            </div>

            {/* Info */}
            <div className="flex-1 min-w-0">
              <h4 className="font-medium text-gray-900">
                {documentTypeLabels[doc.type as keyof typeof documentTypeLabels] || doc.type}
              </h4>
              <p className="text-sm text-gray-500">
                Subido el {formatDate(doc.uploadedAt)}
              </p>
            </div>

            {/* Status */}
            <Badge variant={statusColors[doc.status as keyof typeof statusColors]}>
              {doc.status}
            </Badge>

            {/* Actions */}
            <div className="flex items-center gap-2">
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setSelectedDoc(doc)}
              >
                <Eye size={16} />
              </Button>

              <a href={doc.fileUrl} download>
                <Button variant="ghost" size="sm">
                  <Download size={16} />
                </Button>
              </a>

              {doc.status === "Pending" && (
                <>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="text-green-600"
                    onClick={() => approve(doc.id)}
                    disabled={isApproving}
                  >
                    <Check size={16} />
                  </Button>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="text-red-600"
                    onClick={() => reject(doc.id)}
                    disabled={isRejecting}
                  >
                    <X size={16} />
                  </Button>
                </>
              )}
            </div>
          </div>
        ))}
      </div>

      {/* Preview Modal */}
      {selectedDoc && (
        <Dialog open onOpenChange={() => setSelectedDoc(null)}>
          <Dialog.Content className="max-w-4xl">
            <Dialog.Header>
              <Dialog.Title>
                {documentTypeLabels[selectedDoc.type as keyof typeof documentTypeLabels]}
              </Dialog.Title>
            </Dialog.Header>

            <div className="py-4">
              {selectedDoc.fileType.startsWith("image/") ? (
                <img
                  src={selectedDoc.fileUrl}
                  alt={selectedDoc.type}
                  className="w-full max-h-[600px] object-contain"
                />
              ) : (
                <iframe
                  src={selectedDoc.fileUrl}
                  className="w-full h-[600px]"
                  title={selectedDoc.type}
                />
              )}
            </div>

            <Dialog.Footer>
              <Button variant="outline" onClick={() => setSelectedDoc(null)}>
                Cerrar
              </Button>
              {selectedDoc.status === "Pending" && (
                <>
                  <Button
                    variant="outline"
                    className="text-red-600"
                    onClick={() => {
                      reject(selectedDoc.id);
                      setSelectedDoc(null);
                    }}
                  >
                    Rechazar
                  </Button>
                  <Button
                    onClick={() => {
                      approve(selectedDoc.id);
                      setSelectedDoc(null);
                    }}
                  >
                    Aprobar
                  </Button>
                </>
              )}
            </Dialog.Footer>
          </Dialog.Content>
        </Dialog>
      )}
    </div>
  );
}
```

---

## 🔧 PASO 5: AuditLog

```typescript
// filepath: src/components/admin/compliance/AuditLog.tsx
"use client";

import { useAuditLog } from "@/lib/hooks/useCompliance";
import { formatDate } from "@/lib/utils";

const actionLabels = {
  dealer_approved: "Dealer aprobado",
  dealer_rejected: "Dealer rechazado",
  document_approved: "Documento aprobado",
  document_rejected: "Documento rechazado",
  verification_started: "Verificación iniciada",
  note_added: "Nota agregada",
};

export function AuditLog() {
  const { data: logs } = useAuditLog();

  return (
    <div className="bg-white rounded-xl border">
      <div className="p-4 border-b">
        <h3 className="font-semibold text-gray-900">Log de auditoría</h3>
      </div>

      <div className="divide-y max-h-96 overflow-y-auto">
        {logs?.map((log) => (
          <div key={log.id} className="p-4">
            <div className="flex items-start justify-between mb-1">
              <p className="font-medium text-gray-900">
                {actionLabels[log.action as keyof typeof actionLabels] || log.action}
              </p>
              <span className="text-xs text-gray-500">
                {formatDate(log.createdAt)}
              </span>
            </div>
            <p className="text-sm text-gray-600">{log.details}</p>
            <p className="text-xs text-gray-400 mt-1">Por: {log.performedBy}</p>
          </div>
        ))}
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
# - /admin/compliance muestra tabs
# - Verificaciones pendientes se listan
# - Documentos se pueden revisar
# - Acciones de aprobar/rechazar funcionan
```

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/admin-compliance.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsAdmin } from "../helpers/auth";

test.describe("Admin Compliance", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("debe mostrar panel de compliance", async ({ page }) => {
    await page.goto("/admin/compliance");

    await expect(page.getByTestId("compliance-dashboard")).toBeVisible();
  });

  test("debe ver verificaciones pendientes", async ({ page }) => {
    await page.goto("/admin/compliance/verifications");

    await expect(page.getByTestId("pending-verifications")).toBeVisible();
  });

  test("debe revisar documentos de verificación", async ({ page }) => {
    await page.goto("/admin/compliance/verifications");

    await page.getByTestId("verification-row").first().click();
    await expect(page.getByTestId("document-viewer")).toBeVisible();
  });

  test("debe aprobar verificación de dealer", async ({ page }) => {
    await page.goto("/admin/compliance/verifications");
    await page.getByTestId("verification-row").first().click();

    await page.getByRole("button", { name: /aprobar/i }).click();
    await expect(page.getByText(/verificación aprobada/i)).toBeVisible();
  });

  test("debe rechazar verificación con razón", async ({ page }) => {
    await page.goto("/admin/compliance/verifications");
    await page.getByTestId("verification-row").first().click();

    await page.getByRole("button", { name: /rechazar/i }).click();
    await page.fill('textarea[name="reason"]', "Documento ilegible");
    await page.getByRole("button", { name: /confirmar/i }).click();

    await expect(page.getByText(/verificación rechazada/i)).toBeVisible();
  });
});
```

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/04-PAGINAS/16-admin-support.md`
