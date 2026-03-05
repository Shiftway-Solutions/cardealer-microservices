---
title: "62 - Reports & KYC Queue"
priority: P2
estimated_time: "2 horas"
dependencies: []
apis: ["UserService", "AdminService", "MediaService"]
status: complete
last_updated: "2026-01-30"
---

# 📋 62 - Reports & KYC Queue

**Objetivo:** Moderación de contenido reportado y cola de verificación KYC para admins.

**Prioridad:** P0 (Crítica)  
**Complejidad:** 🟡 Media  
**Dependencias:** AdminService, UserService, MediaService

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura](#-arquitectura)
2. [AdminReportsPage](#-adminreportspage)
3. [KYCAdminQueuePage](#-kycadminqueuepage)
4. [Tipos y Enums](#-tipos-y-enums)
5. [Servicios API](#-servicios-api)

---

## 🏗️ ARQUITECTURA

```
pages/admin/
├── AdminReportsPage.tsx        # Reportes de contenido (317 líneas)
├── KYCAdminQueuePage.tsx       # Cola de verificación KYC (569 líneas)
└── components/
    ├── ReportCard.tsx          # Card de reporte individual
    ├── KYCDetailsModal.tsx     # Modal con documentos KYC
    └── RiskBadge.tsx           # Badge de nivel de riesgo
```

---

## 📊 TIPOS Y ENUMS

```typescript
// src/types/admin.ts

// =====================
// KYC STATUS ENUM
// =====================
export enum KYCStatus {
  Pending = 1, // Recién enviado, sin revisar
  InProgress = 2, // Agente revisando
  DocsRequired = 3, // Se requieren más documentos
  UnderReview = 4, // Revisión secundaria
  Approved = 5, // ✅ Aprobado
  Rejected = 6, // ❌ Rechazado
  Expired = 7, // Documentos expirados
  Suspended = 8, // Suspendido temporalmente
}

export const KYCStatusLabels: Record<KYCStatus, string> = {
  [KYCStatus.Pending]: "Pendiente",
  [KYCStatus.InProgress]: "En Progreso",
  [KYCStatus.DocsRequired]: "Docs Requeridos",
  [KYCStatus.UnderReview]: "En Revisión",
  [KYCStatus.Approved]: "Aprobado",
  [KYCStatus.Rejected]: "Rechazado",
  [KYCStatus.Expired]: "Expirado",
  [KYCStatus.Suspended]: "Suspendido",
};

export const KYCStatusColors: Record<KYCStatus, string> = {
  [KYCStatus.Pending]: "bg-yellow-100 text-yellow-800",
  [KYCStatus.InProgress]: "bg-blue-100 text-blue-800",
  [KYCStatus.DocsRequired]: "bg-orange-100 text-orange-800",
  [KYCStatus.UnderReview]: "bg-purple-100 text-purple-800",
  [KYCStatus.Approved]: "bg-green-100 text-green-800",
  [KYCStatus.Rejected]: "bg-red-100 text-red-800",
  [KYCStatus.Expired]: "bg-gray-100 text-gray-800",
  [KYCStatus.Suspended]: "bg-red-100 text-red-800",
};

// =====================
// RISK LEVEL ENUM
// =====================
export enum RiskLevel {
  Low = 0,
  Medium = 1,
  High = 2,
  Critical = 3,
}

export const RiskLevelLabels: Record<RiskLevel, string> = {
  [RiskLevel.Low]: "Bajo",
  [RiskLevel.Medium]: "Medio",
  [RiskLevel.High]: "Alto",
  [RiskLevel.Critical]: "Crítico",
};

export const RiskLevelColors: Record<RiskLevel, string> = {
  [RiskLevel.Low]: "bg-green-100 text-green-800",
  [RiskLevel.Medium]: "bg-yellow-100 text-yellow-800",
  [RiskLevel.High]: "bg-orange-100 text-orange-800",
  [RiskLevel.Critical]: "bg-red-100 text-red-800",
};

// =====================
// REPORT STATUS
// =====================
export type ReportStatus = "pending" | "reviewed" | "resolved" | "dismissed";

export const ReportStatusLabels: Record<ReportStatus, string> = {
  pending: "Pendiente",
  reviewed: "Revisado",
  resolved: "Resuelto",
  dismissed: "Descartado",
};

// =====================
// REPORT TYPES
// =====================
export type ReportType =
  | "inappropriate_content"
  | "scam"
  | "fake_listing"
  | "spam"
  | "harassment"
  | "other";

export const ReportTypeLabels: Record<ReportType, string> = {
  inappropriate_content: "Contenido Inapropiado",
  scam: "Posible Estafa",
  fake_listing: "Publicación Falsa",
  spam: "Spam",
  harassment: "Acoso",
  other: "Otro",
};

// =====================
// INTERFACES
// =====================
export interface ContentReport {
  id: string;
  type: ReportType;
  status: ReportStatus;
  reportedById: string;
  reportedByName: string;
  targetId: string;
  targetType: "vehicle" | "user" | "message" | "review";
  targetTitle: string;
  description: string;
  createdAt: string;
  resolvedAt?: string;
  resolvedById?: string;
  resolution?: string;
}

export interface KYCSubmission {
  id: string;
  userId: string;
  userName: string;
  userEmail: string;
  userPhone?: string;
  status: KYCStatus;
  riskLevel: RiskLevel;
  documentType: "cedula" | "passport" | "license";
  frontImageUrl: string;
  backImageUrl?: string;
  selfieUrl?: string;
  extractedData?: {
    documentNumber?: string;
    fullName?: string;
    dateOfBirth?: string;
    expirationDate?: string;
  };
  verificationNotes?: string;
  submittedAt: string;
  reviewedAt?: string;
  reviewedById?: string;
}
```

---

## 📢 ADMINREPORTSPAGE

**Ruta:** `/admin/reports`

```typescript
// src/pages/admin/AdminReportsPage.tsx
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { adminService } from '@/services/adminService';
import type { ContentReport, ReportStatus, ReportType } from '@/types/admin';
import {
  FiAlertTriangle,
  FiCheck,
  FiX,
  FiEye,
  FiFilter,
  FiRefreshCw,
  FiMessageSquare,
} from 'react-icons/fi';

export default function AdminReportsPage() {
  const queryClient = useQueryClient();
  const [statusFilter, setStatusFilter] = useState<ReportStatus | 'all'>('pending');
  const [typeFilter, setTypeFilter] = useState<ReportType | 'all'>('all');
  const [selectedReport, setSelectedReport] = useState<ContentReport | null>(null);
  const [resolution, setResolution] = useState('');

  // Query: Get reports
  const { data: reports, isLoading, refetch } = useQuery({
    queryKey: ['admin', 'reports', statusFilter, typeFilter],
    queryFn: () => adminService.getReports({
      status: statusFilter !== 'all' ? statusFilter : undefined,
      type: typeFilter !== 'all' ? typeFilter : undefined,
    }),
  });

  // Mutation: Resolve report
  const resolveReport = useMutation({
    mutationFn: ({ id, resolution }: { id: string; resolution: string }) =>
      adminService.resolveReport(id, resolution),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'reports'] });
      setSelectedReport(null);
      setResolution('');
    },
  });

  // Mutation: Dismiss report
  const dismissReport = useMutation({
    mutationFn: (id: string) => adminService.dismissReport(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'reports'] });
    },
  });

  const getStatusBadge = (status: ReportStatus) => {
    const styles: Record<ReportStatus, string> = {
      pending: 'bg-yellow-100 text-yellow-800',
      reviewed: 'bg-blue-100 text-blue-800',
      resolved: 'bg-green-100 text-green-800',
      dismissed: 'bg-gray-100 text-gray-800',
    };

    return (
      <span className={`px-2 py-1 text-xs font-medium rounded-full ${styles[status]}`}>
        {ReportStatusLabels[status]}
      </span>
    );
  };

  const getTypeBadge = (type: ReportType) => {
    const isUrgent = type === 'scam' || type === 'harassment';
    const baseStyle = isUrgent
      ? 'bg-red-100 text-red-800'
      : 'bg-gray-100 text-gray-800';

    return (
      <span className={`px-2 py-1 text-xs font-medium rounded-full ${baseStyle}`}>
        {ReportTypeLabels[type]}
      </span>
    );
  };

  const formatTimeAgo = (timestamp: string) => {
    const now = new Date();
    const date = new Date(timestamp);
    const diffMs = now.getTime() - date.getTime();
    const hours = Math.floor(diffMs / (1000 * 60 * 60));

    if (hours < 1) return 'Hace menos de 1 hora';
    if (hours < 24) return `Hace ${hours}h`;
    const days = Math.floor(hours / 24);
    return `Hace ${days} día${days > 1 ? 's' : ''}`;
  };

  const handleResolve = () => {
    if (!selectedReport || !resolution.trim()) return;
    resolveReport.mutate({ id: selectedReport.id, resolution: resolution.trim() });
  };

  const handleDismiss = (id: string) => {
    if (!confirm('¿Descartar este reporte?')) return;
    dismissReport.mutate(id);
  };

  return (
    <div className="p-6">
      {/* Header */}
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900 mb-2">Reportes de Contenido</h1>
          <p className="text-gray-600">Gestionar reportes de usuarios sobre contenido</p>
        </div>
        <button
          onClick={() => refetch()}
          disabled={isLoading}
          className="flex items-center gap-2 px-4 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 transition-colors"
        >
          <FiRefreshCw className={isLoading ? 'animate-spin' : ''} />
          Actualizar
        </button>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-xl shadow-card p-4 mb-6 flex gap-4">
        <div className="flex items-center gap-2">
          <FiFilter className="text-gray-400" />
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value as ReportStatus | 'all')}
            className="px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary"
          >
            <option value="all">Todos los estados</option>
            <option value="pending">Pendientes</option>
            <option value="reviewed">Revisados</option>
            <option value="resolved">Resueltos</option>
            <option value="dismissed">Descartados</option>
          </select>
        </div>

        <select
          value={typeFilter}
          onChange={(e) => setTypeFilter(e.target.value as ReportType | 'all')}
          className="px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary"
        >
          <option value="all">Todos los tipos</option>
          <option value="inappropriate_content">Contenido Inapropiado</option>
          <option value="scam">Posible Estafa</option>
          <option value="fake_listing">Publicación Falsa</option>
          <option value="spam">Spam</option>
          <option value="harassment">Acoso</option>
          <option value="other">Otro</option>
        </select>
      </div>

      {/* Stats */}
      <div className="grid md:grid-cols-4 gap-4 mb-6">
        {[
          { label: 'Pendientes', value: reports?.filter((r) => r.status === 'pending').length || 0, color: 'yellow', icon: FiAlertTriangle },
          { label: 'Revisados', value: reports?.filter((r) => r.status === 'reviewed').length || 0, color: 'blue', icon: FiEye },
          { label: 'Resueltos', value: reports?.filter((r) => r.status === 'resolved').length || 0, color: 'green', icon: FiCheck },
          { label: 'Descartados', value: reports?.filter((r) => r.status === 'dismissed').length || 0, color: 'gray', icon: FiX },
        ].map((stat) => (
          <div key={stat.label} className="bg-white rounded-xl shadow-card p-4 flex items-center gap-4">
            <div className={`p-3 rounded-lg bg-${stat.color}-100`}>
              <stat.icon className={`w-6 h-6 text-${stat.color}-600`} />
            </div>
            <div>
              <p className="text-sm text-gray-600">{stat.label}</p>
              <p className="text-2xl font-bold text-gray-900">{stat.value}</p>
            </div>
          </div>
        ))}
      </div>

      {/* Reports List */}
      {isLoading ? (
        <div className="bg-white rounded-xl shadow-card p-12 text-center">
          <div className="animate-spin w-8 h-8 border-4 border-primary border-t-transparent rounded-full mx-auto" />
          <p className="text-gray-600 mt-4">Cargando reportes...</p>
        </div>
      ) : reports?.length === 0 ? (
        <div className="bg-white rounded-xl shadow-card p-12 text-center">
          <FiCheck className="w-12 h-12 text-green-500 mx-auto mb-4" />
          <p className="text-gray-600">No hay reportes que mostrar</p>
        </div>
      ) : (
        <div className="space-y-4">
          {reports?.map((report) => (
            <div
              key={report.id}
              className="bg-white rounded-xl shadow-card p-6 hover:shadow-lg transition-shadow"
            >
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <div className="flex items-center gap-3 mb-2">
                    {getTypeBadge(report.type)}
                    {getStatusBadge(report.status)}
                    <span className="text-sm text-gray-500">
                      {formatTimeAgo(report.createdAt)}
                    </span>
                  </div>

                  <h3 className="text-lg font-semibold text-gray-900 mb-1">
                    {report.targetTitle}
                  </h3>

                  <p className="text-gray-600 mb-3">{report.description}</p>

                  <p className="text-sm text-gray-500">
                    Reportado por: <span className="font-medium">{report.reportedByName}</span>
                  </p>
                </div>

                {report.status === 'pending' && (
                  <div className="flex gap-2 ml-4">
                    <button
                      onClick={() => setSelectedReport(report)}
                      className="flex items-center gap-2 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700"
                    >
                      <FiCheck />
                      Resolver
                    </button>
                    <button
                      onClick={() => handleDismiss(report.id)}
                      className="flex items-center gap-2 px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50"
                    >
                      <FiX />
                      Descartar
                    </button>
                  </div>
                )}
              </div>

              {report.resolution && (
                <div className="mt-4 pt-4 border-t border-gray-200">
                  <p className="text-sm text-gray-600">
                    <strong>Resolución:</strong> {report.resolution}
                  </p>
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      {/* Resolution Modal */}
      {selectedReport && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl shadow-xl max-w-md w-full mx-4 p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">Resolver Reporte</h3>

            <p className="text-sm text-gray-600 mb-4">
              <strong>Tipo:</strong> {ReportTypeLabels[selectedReport.type]}
            </p>

            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Acción tomada / Resolución *
              </label>
              <textarea
                value={resolution}
                onChange={(e) => setResolution(e.target.value)}
                rows={4}
                placeholder="Describe la acción tomada para resolver este reporte..."
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary"
              />
            </div>

            <div className="flex gap-3 justify-end">
              <button
                onClick={() => {
                  setSelectedReport(null);
                  setResolution('');
                }}
                className="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50"
              >
                Cancelar
              </button>
              <button
                onClick={handleResolve}
                disabled={!resolution.trim() || resolveReport.isPending}
                className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 disabled:opacity-50"
              >
                {resolveReport.isPending ? 'Procesando...' : 'Confirmar Resolución'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
```

---

## 🔐 KYCADMINQUEUEPAGE

**Ruta:** `/admin/kyc-queue`

```typescript
// src/pages/admin/KYCAdminQueuePage.tsx
import { useState, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { adminService } from '@/services/adminService';
import type { KYCSubmission, KYCStatus, RiskLevel } from '@/types/admin';
import { KYCStatusLabels, KYCStatusColors, RiskLevelLabels, RiskLevelColors } from '@/types/admin';
import {
  FiUser,
  FiCheck,
  FiX,
  FiEye,
  FiFilter,
  FiRefreshCw,
  FiAlertTriangle,
  FiClock,
  FiDownload,
  FiMessageSquare,
  FiChevronLeft,
  FiChevronRight,
} from 'react-icons/fi';

export default function KYCAdminQueuePage() {
  const queryClient = useQueryClient();
  const [statusFilter, setStatusFilter] = useState<KYCStatus | 'all'>('all');
  const [riskFilter, setRiskFilter] = useState<RiskLevel | 'all'>('all');
  const [selectedSubmission, setSelectedSubmission] = useState<KYCSubmission | null>(null);
  const [showDetailsModal, setShowDetailsModal] = useState(false);
  const [rejectionReason, setRejectionReason] = useState('');
  const [additionalNotes, setAdditionalNotes] = useState('');
  const [currentImageIndex, setCurrentImageIndex] = useState(0);

  // Query: Get KYC submissions
  const { data: submissions, isLoading, refetch } = useQuery({
    queryKey: ['admin', 'kyc-queue', statusFilter, riskFilter],
    queryFn: () => adminService.getKYCSubmissions({
      status: statusFilter !== 'all' ? statusFilter : undefined,
      riskLevel: riskFilter !== 'all' ? riskFilter : undefined,
    }),
    refetchInterval: 60000, // Refresh every minute
  });

  // Mutation: Approve KYC
  const approveKYC = useMutation({
    mutationFn: ({ id, notes }: { id: string; notes?: string }) =>
      adminService.approveKYC(id, notes),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'kyc-queue'] });
      closeModal();
    },
  });

  // Mutation: Reject KYC
  const rejectKYC = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      adminService.rejectKYC(id, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'kyc-queue'] });
      closeModal();
    },
  });

  // Mutation: Request more documents
  const requestDocs = useMutation({
    mutationFn: ({ id, message }: { id: string; message: string }) =>
      adminService.requestKYCDocs(id, message),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'kyc-queue'] });
      closeModal();
    },
  });

  const closeModal = () => {
    setSelectedSubmission(null);
    setShowDetailsModal(false);
    setRejectionReason('');
    setAdditionalNotes('');
    setCurrentImageIndex(0);
  };

  const handleViewDetails = (submission: KYCSubmission) => {
    setSelectedSubmission(submission);
    setShowDetailsModal(true);
    setCurrentImageIndex(0);
  };

  const handleApprove = () => {
    if (!selectedSubmission) return;
    approveKYC.mutate({
      id: selectedSubmission.id,
      notes: additionalNotes || undefined,
    });
  };

  const handleReject = () => {
    if (!selectedSubmission || !rejectionReason.trim()) {
      alert('Por favor proporciona una razón para el rechazo');
      return;
    }
    rejectKYC.mutate({
      id: selectedSubmission.id,
      reason: rejectionReason,
    });
  };

  const handleRequestDocs = () => {
    if (!selectedSubmission || !additionalNotes.trim()) {
      alert('Por favor indica qué documentos se requieren');
      return;
    }
    requestDocs.mutate({
      id: selectedSubmission.id,
      message: additionalNotes,
    });
  };

  const getStatusBadge = (status: KYCStatus) => (
    <span className={`px-2 py-1 text-xs font-medium rounded-full ${KYCStatusColors[status]}`}>
      {KYCStatusLabels[status]}
    </span>
  );

  const getRiskBadge = (risk: RiskLevel) => (
    <span className={`px-2 py-1 text-xs font-medium rounded-full ${RiskLevelColors[risk]}`}>
      🛡️ {RiskLevelLabels[risk]}
    </span>
  );

  const formatDate = (timestamp: string) => {
    return new Date(timestamp).toLocaleDateString('es-DO', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const getDocumentImages = (submission: KYCSubmission) => {
    const images: { url: string; label: string }[] = [];
    if (submission.frontImageUrl) images.push({ url: submission.frontImageUrl, label: 'Frente' });
    if (submission.backImageUrl) images.push({ url: submission.backImageUrl, label: 'Reverso' });
    if (submission.selfieUrl) images.push({ url: submission.selfieUrl, label: 'Selfie' });
    return images;
  };

  return (
    <div className="p-6">
      {/* Header */}
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900 mb-2">Cola de Verificación KYC</h1>
          <p className="text-gray-600">
            Verificación de identidad de usuarios
          </p>
        </div>
        <button
          onClick={() => refetch()}
          disabled={isLoading}
          className="flex items-center gap-2 px-4 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200"
        >
          <FiRefreshCw className={isLoading ? 'animate-spin' : ''} />
          Actualizar
        </button>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-xl shadow-card p-4 mb-6 flex flex-wrap gap-4">
        <div className="flex items-center gap-2">
          <FiFilter className="text-gray-400" />
          <span className="text-sm text-gray-600">Filtros:</span>
        </div>

        <select
          value={statusFilter === 'all' ? 'all' : String(statusFilter)}
          onChange={(e) => setStatusFilter(e.target.value === 'all' ? 'all' : Number(e.target.value) as KYCStatus)}
          className="px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary"
        >
          <option value="all">Todos los estados</option>
          <option value={KYCStatus.Pending}>Pendiente</option>
          <option value={KYCStatus.InProgress}>En Progreso</option>
          <option value={KYCStatus.DocsRequired}>Docs Requeridos</option>
          <option value={KYCStatus.UnderReview}>En Revisión</option>
          <option value={KYCStatus.Approved}>Aprobado</option>
          <option value={KYCStatus.Rejected}>Rechazado</option>
        </select>

        <select
          value={riskFilter === 'all' ? 'all' : String(riskFilter)}
          onChange={(e) => setRiskFilter(e.target.value === 'all' ? 'all' : Number(e.target.value) as RiskLevel)}
          className="px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary"
        >
          <option value="all">Todos los niveles</option>
          <option value={RiskLevel.Low}>Riesgo Bajo</option>
          <option value={RiskLevel.Medium}>Riesgo Medio</option>
          <option value={RiskLevel.High}>Riesgo Alto</option>
          <option value={RiskLevel.Critical}>Riesgo Crítico</option>
        </select>
      </div>

      {/* Stats Row */}
      <div className="grid md:grid-cols-5 gap-4 mb-6">
        {[
          { status: KYCStatus.Pending, icon: FiClock, color: 'yellow' },
          { status: KYCStatus.InProgress, icon: FiUser, color: 'blue' },
          { status: KYCStatus.DocsRequired, icon: FiMessageSquare, color: 'orange' },
          { status: KYCStatus.Approved, icon: FiCheck, color: 'green' },
          { status: KYCStatus.Rejected, icon: FiX, color: 'red' },
        ].map((stat) => (
          <div key={stat.status} className="bg-white rounded-xl shadow-card p-4">
            <div className="flex items-center gap-3">
              <div className={`p-2 rounded-lg bg-${stat.color}-100`}>
                <stat.icon className={`w-5 h-5 text-${stat.color}-600`} />
              </div>
              <div>
                <p className="text-xs text-gray-500">{KYCStatusLabels[stat.status]}</p>
                <p className="text-xl font-bold text-gray-900">
                  {submissions?.filter((s) => s.status === stat.status).length || 0}
                </p>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Submissions List */}
      {isLoading ? (
        <div className="bg-white rounded-xl shadow-card p-12 text-center">
          <div className="animate-spin w-8 h-8 border-4 border-primary border-t-transparent rounded-full mx-auto" />
          <p className="text-gray-600 mt-4">Cargando solicitudes KYC...</p>
        </div>
      ) : submissions?.length === 0 ? (
        <div className="bg-white rounded-xl shadow-card p-12 text-center">
          <FiCheck className="w-12 h-12 text-green-500 mx-auto mb-4" />
          <h3 className="text-lg font-semibold text-gray-900 mb-2">¡Cola vacía!</h3>
          <p className="text-gray-600">No hay solicitudes KYC que revisar</p>
        </div>
      ) : (
        <div className="space-y-4">
          {submissions?.map((submission) => (
            <div
              key={submission.id}
              className="bg-white rounded-xl shadow-card p-6 hover:shadow-lg transition-shadow"
            >
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-4">
                  {/* User Avatar */}
                  <div className="w-12 h-12 bg-gray-200 rounded-full flex items-center justify-center">
                    <FiUser className="w-6 h-6 text-gray-500" />
                  </div>

                  <div>
                    <h3 className="font-semibold text-gray-900">{submission.userName}</h3>
                    <p className="text-sm text-gray-600">{submission.userEmail}</p>
                    <p className="text-xs text-gray-500 mt-1">
                      Enviado: {formatDate(submission.submittedAt)}
                    </p>
                  </div>
                </div>

                <div className="flex items-center gap-4">
                  <div className="flex flex-col items-end gap-1">
                    {getStatusBadge(submission.status)}
                    {getRiskBadge(submission.riskLevel)}
                  </div>

                  <button
                    onClick={() => handleViewDetails(submission)}
                    className="flex items-center gap-2 px-4 py-2 bg-primary text-white rounded-lg hover:bg-primary-dark"
                  >
                    <FiEye />
                    Revisar
                  </button>
                </div>
              </div>

              {/* Extracted Data Preview */}
              {submission.extractedData && (
                <div className="mt-4 pt-4 border-t border-gray-100">
                  <div className="grid md:grid-cols-4 gap-4 text-sm">
                    <div>
                      <p className="text-gray-500">Número de Documento</p>
                      <p className="font-medium text-gray-900">
                        {submission.extractedData.documentNumber || 'N/A'}
                      </p>
                    </div>
                    <div>
                      <p className="text-gray-500">Nombre Completo</p>
                      <p className="font-medium text-gray-900">
                        {submission.extractedData.fullName || 'N/A'}
                      </p>
                    </div>
                    <div>
                      <p className="text-gray-500">Fecha de Nacimiento</p>
                      <p className="font-medium text-gray-900">
                        {submission.extractedData.dateOfBirth || 'N/A'}
                      </p>
                    </div>
                    <div>
                      <p className="text-gray-500">Vencimiento</p>
                      <p className="font-medium text-gray-900">
                        {submission.extractedData.expirationDate || 'N/A'}
                      </p>
                    </div>
                  </div>
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      {/* Details Modal */}
      {showDetailsModal && selectedSubmission && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-xl shadow-xl max-w-4xl w-full max-h-[90vh] overflow-y-auto">
            {/* Modal Header */}
            <div className="p-6 border-b border-gray-200">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-4">
                  <div className="w-12 h-12 bg-gray-200 rounded-full flex items-center justify-center">
                    <FiUser className="w-6 h-6 text-gray-500" />
                  </div>
                  <div>
                    <h2 className="text-xl font-semibold text-gray-900">
                      {selectedSubmission.userName}
                    </h2>
                    <p className="text-sm text-gray-600">{selectedSubmission.userEmail}</p>
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  {getStatusBadge(selectedSubmission.status)}
                  {getRiskBadge(selectedSubmission.riskLevel)}
                </div>
              </div>
            </div>

            {/* Document Images */}
            <div className="p-6 border-b border-gray-200">
              <h3 className="font-semibold text-gray-900 mb-4">Documentos Adjuntos</h3>
              <div className="relative">
                {(() => {
                  const images = getDocumentImages(selectedSubmission);
                  if (images.length === 0) return <p className="text-gray-500">Sin imágenes</p>;

                  return (
                    <div className="relative">
                      <img
                        src={images[currentImageIndex].url}
                        alt={images[currentImageIndex].label}
                        className="w-full h-80 object-contain bg-gray-100 rounded-lg"
                      />
                      <div className="absolute bottom-4 left-1/2 transform -translate-x-1/2 bg-black/50 text-white px-4 py-2 rounded-full text-sm">
                        {images[currentImageIndex].label} ({currentImageIndex + 1}/{images.length})
                      </div>
                      {images.length > 1 && (
                        <>
                          <button
                            onClick={() => setCurrentImageIndex((i) => (i - 1 + images.length) % images.length)}
                            className="absolute left-2 top-1/2 transform -translate-y-1/2 p-2 bg-black/50 text-white rounded-full"
                          >
                            <FiChevronLeft />
                          </button>
                          <button
                            onClick={() => setCurrentImageIndex((i) => (i + 1) % images.length)}
                            className="absolute right-2 top-1/2 transform -translate-y-1/2 p-2 bg-black/50 text-white rounded-full"
                          >
                            <FiChevronRight />
                          </button>
                        </>
                      )}
                    </div>
                  );
                })()}
              </div>
            </div>

            {/* Extracted Data */}
            {selectedSubmission.extractedData && (
              <div className="p-6 border-b border-gray-200">
                <h3 className="font-semibold text-gray-900 mb-4">Datos Extraídos (OCR)</h3>
                <div className="grid md:grid-cols-2 gap-4">
                  <div className="p-4 bg-gray-50 rounded-lg">
                    <p className="text-sm text-gray-500">Número de Documento</p>
                    <p className="font-mono font-medium text-gray-900">
                      {selectedSubmission.extractedData.documentNumber || 'N/A'}
                    </p>
                  </div>
                  <div className="p-4 bg-gray-50 rounded-lg">
                    <p className="text-sm text-gray-500">Nombre Completo</p>
                    <p className="font-medium text-gray-900">
                      {selectedSubmission.extractedData.fullName || 'N/A'}
                    </p>
                  </div>
                  <div className="p-4 bg-gray-50 rounded-lg">
                    <p className="text-sm text-gray-500">Fecha de Nacimiento</p>
                    <p className="font-medium text-gray-900">
                      {selectedSubmission.extractedData.dateOfBirth || 'N/A'}
                    </p>
                  </div>
                  <div className="p-4 bg-gray-50 rounded-lg">
                    <p className="text-sm text-gray-500">Fecha de Vencimiento</p>
                    <p className="font-medium text-gray-900">
                      {selectedSubmission.extractedData.expirationDate || 'N/A'}
                    </p>
                  </div>
                </div>
              </div>
            )}

            {/* Notes */}
            <div className="p-6 border-b border-gray-200">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Notas / Razón de Rechazo
              </label>
              <textarea
                value={selectedSubmission.status === KYCStatus.Pending ? additionalNotes : rejectionReason}
                onChange={(e) =>
                  selectedSubmission.status === KYCStatus.Pending
                    ? setAdditionalNotes(e.target.value)
                    : setRejectionReason(e.target.value)
                }
                rows={3}
                placeholder="Agregar notas o razón de rechazo..."
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary"
              />
            </div>

            {/* Actions */}
            <div className="p-6 flex justify-between">
              <button
                onClick={closeModal}
                className="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50"
              >
                Cerrar
              </button>

              <div className="flex gap-3">
                <button
                  onClick={handleRequestDocs}
                  disabled={!additionalNotes.trim() || requestDocs.isPending}
                  className="flex items-center gap-2 px-4 py-2 bg-orange-600 text-white rounded-lg hover:bg-orange-700 disabled:opacity-50"
                >
                  <FiMessageSquare />
                  Solicitar Docs
                </button>
                <button
                  onClick={handleReject}
                  disabled={!rejectionReason.trim() || rejectKYC.isPending}
                  className="flex items-center gap-2 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:opacity-50"
                >
                  <FiX />
                  Rechazar
                </button>
                <button
                  onClick={handleApprove}
                  disabled={approveKYC.isPending}
                  className="flex items-center gap-2 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 disabled:opacity-50"
                >
                  <FiCheck />
                  Aprobar
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
```

---

## 🔌 SERVICIOS API

```typescript
// src/services/adminService.ts

export const adminService = {
  // =====================
  // REPORTS
  // =====================
  getReports: async (filters?: {
    status?: ReportStatus;
    type?: ReportType;
  }): Promise<ContentReport[]> => {
    const params = new URLSearchParams();
    if (filters?.status) params.append("status", filters.status);
    if (filters?.type) params.append("type", filters.type);

    const response = await api.get(`/api/admin/reports?${params}`);
    return response.data;
  },

  resolveReport: async (id: string, resolution: string): Promise<void> => {
    await api.post(`/api/admin/reports/${id}/resolve`, { resolution });
  },

  dismissReport: async (id: string): Promise<void> => {
    await api.post(`/api/admin/reports/${id}/dismiss`);
  },

  // =====================
  // KYC
  // =====================
  getKYCSubmissions: async (filters?: {
    status?: KYCStatus;
    riskLevel?: RiskLevel;
  }): Promise<KYCSubmission[]> => {
    const params = new URLSearchParams();
    if (filters?.status !== undefined)
      params.append("status", String(filters.status));
    if (filters?.riskLevel !== undefined)
      params.append("riskLevel", String(filters.riskLevel));

    const response = await api.get(`/api/admin/kyc?${params}`);
    return response.data;
  },

  approveKYC: async (id: string, notes?: string): Promise<void> => {
    await api.post(`/api/admin/kyc/${id}/approve`, { notes });
  },

  rejectKYC: async (id: string, reason: string): Promise<void> => {
    await api.post(`/api/admin/kyc/${id}/reject`, { reason });
  },

  requestKYCDocs: async (id: string, message: string): Promise<void> => {
    await api.post(`/api/admin/kyc/${id}/request-docs`, { message });
  },
};
```

---

## ✅ VALIDACIÓN

### Reports Page

- [ ] Lista de reportes con filtros
- [ ] Filter por status y tipo
- [ ] Stats cards por estado
- [ ] Badge de tipo con colores urgentes
- [ ] Modal de resolución con textarea
- [ ] Botón descartar con confirmación
- [ ] Empty state cuando no hay reportes

### KYC Queue Page

- [ ] Lista de solicitudes KYC
- [ ] Filtros por status y nivel de riesgo
- [ ] Stats row con conteos
- [ ] Badge de status y riesgo
- [ ] Datos extraídos (OCR) preview
- [ ] Modal de detalles completo
- [ ] Carousel de imágenes de documentos
- [ ] Botón aprobar
- [ ] Botón rechazar con razón
- [ ] Botón solicitar más documentos
- [ ] Refresh automático cada 60s

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/reports-kyc-queue.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsAdmin } from "../helpers/auth";

test.describe("Reports & KYC Queue", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test.describe("KYC Queue", () => {
    test("debe mostrar cola de KYC", async ({ page }) => {
      await page.goto("/admin/kyc");

      await expect(page.getByTestId("kyc-queue")).toBeVisible();
    });

    test("debe ver documentos de verificación", async ({ page }) => {
      await page.goto("/admin/kyc");

      await page.getByTestId("kyc-request").first().click();
      await expect(page.getByTestId("document-carousel")).toBeVisible();
    });

    test("debe aprobar verificación KYC", async ({ page }) => {
      await page.goto("/admin/kyc");
      await page.getByTestId("kyc-request").first().click();

      await page.getByRole("button", { name: /aprobar/i }).click();
      await expect(page.getByText(/verificación aprobada/i)).toBeVisible();
    });

    test("debe solicitar más documentos", async ({ page }) => {
      await page.goto("/admin/kyc");
      await page.getByTestId("kyc-request").first().click();

      await page.getByRole("button", { name: /solicitar documentos/i }).click();
      await page.getByRole("checkbox", { name: /cédula/i }).click();
      await page.getByRole("button", { name: /enviar solicitud/i }).click();

      await expect(page.getByText(/solicitud enviada/i)).toBeVisible();
    });
  });

  test.describe("Reports", () => {
    test("debe generar reporte", async ({ page }) => {
      await page.goto("/admin/reports");

      await page.getByRole("button", { name: /generar/i }).click();
      await expect(page.getByTestId("report-preview")).toBeVisible();
    });
  });
});
```

---

_Última actualización: Enero 2026_
