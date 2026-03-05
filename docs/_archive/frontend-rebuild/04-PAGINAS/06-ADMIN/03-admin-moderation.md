---
title: "Admin - Moderación General"
priority: P2
estimated_time: "45 minutos"
dependencies: []
apis: []
status: complete
last_updated: "2026-01-30"
---

# 🛡️ Admin - Moderación General

> **Ruta:** `/admin/moderacion` (Dashboard general de moderación)  
> **Scope:** Moderación de TODO tipo de contenido (listings, reportes, usuarios)  
> **Reviews:** Tab con contador + link a herramienta especializada (ver doc 37)  
> **Moderación Reviews Avanzada:** Ver [37-admin-review-moderation-completo.md](37-admin-review-moderation-completo.md)

> **Tiempo estimado:** 45 minutos
> **Prerrequisitos:** Admin layout, ModerationService
> **Roles:** ADM-MOD, ADM-ADMIN, ADM-SUPER

---

## 📋 OBJETIVO

Implementar dashboard general de moderación:

- Cola de revisión de listings de vehículos
- Reportes de usuarios
- Acciones de moderación básicas (Aprobar/Rechazar)
- Historial de acciones
- **Tab "Reviews":** Muestra contador de reviews pendientes + link a [37-admin-review-moderation-completo.md](37-admin-review-moderation-completo.md)

---

## 🎨 WIREFRAME - DASHBOARD MODERACIÓN

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ ┌──────────┐                                                                │
│ │ SIDEBAR  │  MODERACIÓN                                     🔔 12         │
│ │          │ ────────────────────────────────────────────────────────────── │
│ │ 📊 Dash  │                                                                │
│ │ 👥 Users │  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌───────────┐│
│ │ 🛡️ Mod ◀ │  │ ⏳ 23       │ │ 🚨 8        │ │ ✅ 156      │ │ ❌ 12     ││
│ │ ⚙️ System│  │ Pendientes  │ │ Reportes    │ │ Aprobados   │ │ Rechazados││
│ │          │  │ hoy         │ │ abiertos    │ │ esta semana │ │ esta sem. ││
│ │          │  └─────────────┘ └─────────────┘ └─────────────┘ └───────────┘│
│ │          │                                                                │
│ │          │  ┌─────────────────────────────────────────────────────────┐  │
│ │          │  │ [Pendientes (23)] [Reportes (8)] [Reviews (5)] [Historial]│ │
│ │          │  └─────────────────────────────────────────────────────────┘  │
│ │          │                                                                │
│ │          │  ┌─────────────────────────────────────────────────────────┐  │
│ │          │  │ COLA DE REVISIÓN                        [Filtros ▼]      │  │
│ │          │  │                                                          │  │
│ │          │  │  ┌────────────────────────────────────────────────────┐  │  │
│ │          │  │  │ 🚗 Toyota Camry 2023                               │  │  │
│ │          │  │  │ Por: Juan Pérez • Hace 15 min                       │  │  │
│ │          │  │  │ ⚠️ Revisión requerida: Precio sospechoso            │  │  │
│ │          │  │  │                     [✅ Aprobar] [❌ Rechazar] [👁️]│  │  │
│ │          │  │  └────────────────────────────────────────────────────┘  │  │
│ │          │  │                                                          │  │
│ │          │  │  ┌────────────────────────────────────────────────────┐  │  │
│ │          │  │  │ 🚗 Honda Civic 2022                                │  │  │
│ │          │  │  │ Por: María García • Hace 45 min                     │  │  │
│ │          │  │  │ ⚠️ Revisión requerida: Imágenes duplicadas          │  │  │
│ │          │  │  │                     [✅ Aprobar] [❌ Rechazar] [👁️]│  │  │
│ │          │  │  └────────────────────────────────────────────────────┘  │  │
│ │          │  │                                                          │  │
│ │          │  │  [Cargar más...]                                         │  │
│ │          │  └─────────────────────────────────────────────────────────┘  │
│ └──────────┘                                                                │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔧 PASO 1: Dashboard de Moderación

```typescript
// filepath: src/app/(admin)/admin/moderacion/page.tsx
import { Metadata } from "next";
import { Suspense } from "react";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/Tabs";
import { ModerationStats } from "@/components/admin/moderation/ModerationStats";
import { PendingReviewQueue } from "@/components/admin/moderation/PendingReviewQueue";
import { ReportsQueue } from "@/components/admin/moderation/ReportsQueue";
import { LoadingCard } from "@/components/ui/LoadingCard";

export const metadata: Metadata = {
  title: "Moderación | Admin OKLA",
};

export default function ModerationPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Moderación</h1>
        <p className="text-gray-600">Revisa contenido y gestiona reportes</p>
      </div>

      {/* Stats */}
      <Suspense fallback={<LoadingCard className="h-24" />}>
        <ModerationStats />
      </Suspense>

      {/* Tabs */}
      <Tabs defaultValue="pending">
        <TabsList>
          <TabsTrigger value="pending">Pendientes de revisión</TabsTrigger>
          <TabsTrigger value="reports">Reportes</TabsTrigger>
          <TabsTrigger value="history">Historial</TabsTrigger>
        </TabsList>

        <TabsContent value="pending" className="mt-6">
          <Suspense fallback={<LoadingCard className="h-96" />}>
            <PendingReviewQueue />
          </Suspense>
        </TabsContent>

        <TabsContent value="reports" className="mt-6">
          <Suspense fallback={<LoadingCard className="h-96" />}>
            <ReportsQueue />
          </Suspense>
        </TabsContent>

        <TabsContent value="history" className="mt-6">
          {/* History component */}
        </TabsContent>
      </Tabs>
    </div>
  );
}
```

---

## 🔧 PASO 2: ModerationStats

```typescript
// filepath: src/components/admin/moderation/ModerationStats.tsx
import { Clock, AlertTriangle, CheckCircle, XCircle } from "lucide-react";
import { moderationService } from "@/lib/services/moderationService";

export async function ModerationStats() {
  const stats = await moderationService.getStats();

  const items = [
    { icon: Clock, label: "Pendientes", value: stats.pending, color: "text-blue-600 bg-blue-50" },
    { icon: AlertTriangle, label: "Reportes", value: stats.reports, color: "text-amber-600 bg-amber-50" },
    { icon: CheckCircle, label: "Aprobados hoy", value: stats.approvedToday, color: "text-green-600 bg-green-50" },
    { icon: XCircle, label: "Rechazados hoy", value: stats.rejectedToday, color: "text-red-600 bg-red-50" },
  ];

  return (
    <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
      {items.map((item) => (
        <div key={item.label} className="bg-white rounded-xl border p-4">
          <div className="flex items-center gap-3">
            <div className={`p-2 rounded-lg ${item.color}`}>
              <item.icon size={20} />
            </div>
            <div>
              <p className="text-2xl font-bold text-gray-900">{item.value}</p>
              <p className="text-sm text-gray-500">{item.label}</p>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}
```

---

## 🔧 PASO 3: PendingReviewQueue

```typescript
// filepath: src/components/admin/moderation/PendingReviewQueue.tsx
"use client";

import { useState } from "react";
import { Eye, Check, X, Flag } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { usePendingReviews, useModerateItem } from "@/lib/hooks/useModeration";
import { formatRelativeDate } from "@/lib/utils";
import { ReviewDetailModal } from "./ReviewDetailModal";

export function PendingReviewQueue() {
  const { data: items, isLoading } = usePendingReviews();
  const { approve, reject } = useModerateItem();
  const [selectedItem, setSelectedItem] = useState<string | null>(null);

  if (isLoading) {
    return <div className="animate-pulse bg-gray-100 rounded-xl h-96" />;
  }

  return (
    <div className="bg-white rounded-xl border">
      <div className="divide-y">
        {items?.map((item) => (
          <div key={item.id} className="p-4 flex items-center gap-4">
            {/* Thumbnail */}
            <img
              src={item.thumbnail || "/placeholder-car.jpg"}
              alt={item.title}
              className="w-20 h-14 rounded-lg object-cover"
            />

            {/* Info */}
            <div className="flex-1 min-w-0">
              <h4 className="font-medium text-gray-900 truncate">{item.title}</h4>
              <div className="flex items-center gap-2 text-sm text-gray-500">
                <span>{item.sellerName}</span>
                <span>•</span>
                <span>{formatRelativeDate(item.submittedAt)}</span>
              </div>
            </div>

            {/* Type Badge */}
            <Badge>
              {item.type === "new" ? "Nuevo" : "Editado"}
            </Badge>

            {/* Actions */}
            <div className="flex items-center gap-2">
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setSelectedItem(item.id)}
              >
                <Eye size={16} />
              </Button>

              <Button
                variant="ghost"
                size="sm"
                className="text-green-600 hover:text-green-700 hover:bg-green-50"
                onClick={() => approve(item.id)}
              >
                <Check size={16} />
              </Button>

              <Button
                variant="ghost"
                size="sm"
                className="text-red-600 hover:text-red-700 hover:bg-red-50"
                onClick={() => reject(item.id)}
              >
                <X size={16} />
              </Button>
            </div>
          </div>
        ))}

        {items?.length === 0 && (
          <div className="p-12 text-center text-gray-500">
            <Check size={48} className="mx-auto mb-4 text-green-500" />
            <p>No hay items pendientes de revisión</p>
          </div>
        )}
      </div>

      {/* Detail Modal */}
      {selectedItem && (
        <ReviewDetailModal
          itemId={selectedItem}
          onClose={() => setSelectedItem(null)}
          onApprove={() => {
            approve(selectedItem);
            setSelectedItem(null);
          }}
          onReject={() => {
            reject(selectedItem);
            setSelectedItem(null);
          }}
        />
      )}
    </div>
  );
}
```

---

## 🔧 PASO 4: ReportsQueue

```typescript
// filepath: src/components/admin/moderation/ReportsQueue.tsx
"use client";

import { AlertTriangle, User, Car, MessageCircle } from "lucide-react";
import Link from "next/link";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { useReports, useDismissReport } from "@/lib/hooks/useModeration";
import { formatRelativeDate } from "@/lib/utils";

const reportTypeIcons = {
  vehicle: Car,
  user: User,
  message: MessageCircle,
};

const priorityColors = {
  low: "default",
  medium: "warning",
  high: "danger",
} as const;

export function ReportsQueue() {
  const { data: reports } = useReports();
  const { dismiss } = useDismissReport();

  return (
    <div className="bg-white rounded-xl border">
      <div className="divide-y">
        {reports?.map((report) => {
          const Icon = reportTypeIcons[report.type as keyof typeof reportTypeIcons] || AlertTriangle;

          return (
            <div key={report.id} className="p-4">
              <div className="flex items-start gap-4">
                {/* Icon */}
                <div className="p-2 rounded-lg bg-red-50 text-red-600">
                  <Icon size={20} />
                </div>

                {/* Content */}
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-1">
                    <h4 className="font-medium text-gray-900">{report.reason}</h4>
                    <Badge variant={priorityColors[report.priority as keyof typeof priorityColors]}>
                      {report.priority}
                    </Badge>
                  </div>

                  <p className="text-sm text-gray-600 mb-2">{report.description}</p>

                  <div className="flex items-center gap-4 text-xs text-gray-500">
                    <span>Reportado por: {report.reporterName}</span>
                    <span>•</span>
                    <span>{formatRelativeDate(report.createdAt)}</span>
                  </div>
                </div>

                {/* Actions */}
                <div className="flex items-center gap-2">
                  <Link href={`/admin/moderacion/reportes/${report.id}`}>
                    <Button variant="outline" size="sm">
                      Revisar
                    </Button>
                  </Link>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => dismiss(report.id)}
                  >
                    Descartar
                  </Button>
                </div>
              </div>
            </div>
          );
        })}

        {reports?.length === 0 && (
          <div className="p-12 text-center text-gray-500">
            <AlertTriangle size={48} className="mx-auto mb-4 text-gray-300" />
            <p>No hay reportes pendientes</p>
          </div>
        )}
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 5: ReviewDetailModal

```typescript
// filepath: src/components/admin/moderation/ReviewDetailModal.tsx
"use client";

import { X, Check, Flag, ChevronLeft, ChevronRight } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Dialog } from "@/components/ui/Dialog";
import { Textarea } from "@/components/ui/Textarea";
import { useReviewDetail } from "@/lib/hooks/useModeration";
import { formatPrice, formatNumber } from "@/lib/utils";
import { useState } from "react";

interface Props {
  itemId: string;
  onClose: () => void;
  onApprove: () => void;
  onReject: () => void;
}

export function ReviewDetailModal({ itemId, onClose, onApprove, onReject }: Props) {
  const { data: item } = useReviewDetail(itemId);
  const [rejectReason, setRejectReason] = useState("");
  const [showRejectForm, setShowRejectForm] = useState(false);
  const [currentImage, setCurrentImage] = useState(0);

  if (!item) return null;

  return (
    <Dialog open onOpenChange={onClose}>
      <Dialog.Content className="max-w-4xl">
        <Dialog.Header>
          <Dialog.Title>Revisar: {item.title}</Dialog.Title>
        </Dialog.Header>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6 py-4">
          {/* Images */}
          <div>
            <div className="relative aspect-video rounded-lg overflow-hidden bg-gray-100">
              <img
                src={item.images[currentImage]}
                alt={`Imagen ${currentImage + 1}`}
                className="w-full h-full object-cover"
              />

              {item.images.length > 1 && (
                <>
                  <button
                    onClick={() => setCurrentImage((p) => Math.max(0, p - 1))}
                    className="absolute left-2 top-1/2 -translate-y-1/2 p-2 bg-black/50 rounded-full text-white"
                  >
                    <ChevronLeft size={20} />
                  </button>
                  <button
                    onClick={() => setCurrentImage((p) => Math.min(item.images.length - 1, p + 1))}
                    className="absolute right-2 top-1/2 -translate-y-1/2 p-2 bg-black/50 rounded-full text-white"
                  >
                    <ChevronRight size={20} />
                  </button>
                </>
              )}

              <div className="absolute bottom-2 right-2 bg-black/50 text-white text-xs px-2 py-1 rounded">
                {currentImage + 1} / {item.images.length}
              </div>
            </div>

            {/* Thumbnails */}
            <div className="flex gap-2 mt-2 overflow-x-auto">
              {item.images.map((img, i) => (
                <button
                  key={i}
                  onClick={() => setCurrentImage(i)}
                  className={`flex-shrink-0 w-16 h-12 rounded-lg overflow-hidden border-2 ${
                    i === currentImage ? "border-primary-600" : "border-transparent"
                  }`}
                >
                  <img src={img} alt="" className="w-full h-full object-cover" />
                </button>
              ))}
            </div>
          </div>

          {/* Details */}
          <div className="space-y-4">
            <div>
              <h3 className="text-lg font-bold text-gray-900">{item.title}</h3>
              <p className="text-2xl font-bold text-primary-600">{formatPrice(item.price)}</p>
            </div>

            <div className="grid grid-cols-2 gap-4 text-sm">
              <div>
                <p className="text-gray-500">Año</p>
                <p className="font-medium">{item.year}</p>
              </div>
              <div>
                <p className="text-gray-500">Kilometraje</p>
                <p className="font-medium">{formatNumber(item.mileage)} km</p>
              </div>
              <div>
                <p className="text-gray-500">Vendedor</p>
                <p className="font-medium">{item.sellerName}</p>
              </div>
              <div>
                <p className="text-gray-500">Tipo</p>
                <p className="font-medium">{item.sellerType}</p>
              </div>
            </div>

            <div>
              <p className="text-gray-500 text-sm mb-1">Descripción</p>
              <p className="text-sm text-gray-700">{item.description}</p>
            </div>
          </div>
        </div>

        {/* Reject Form */}
        {showRejectForm && (
          <div className="border-t pt-4">
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Razón del rechazo
            </label>
            <Textarea
              value={rejectReason}
              onChange={(e) => setRejectReason(e.target.value)}
              rows={3}
              placeholder="Explica por qué se rechaza este listing..."
            />
          </div>
        )}

        <Dialog.Footer>
          <Button variant="outline" onClick={onClose}>
            Cancelar
          </Button>

          {showRejectForm ? (
            <Button
              variant="danger"
              onClick={() => {
                onReject();
              }}
              disabled={!rejectReason.trim()}
            >
              <X size={16} className="mr-2" />
              Confirmar rechazo
            </Button>
          ) : (
            <>
              <Button
                variant="outline"
                className="text-red-600"
                onClick={() => setShowRejectForm(true)}
              >
                <X size={16} className="mr-2" />
                Rechazar
              </Button>
              <Button onClick={onApprove}>
                <Check size={16} className="mr-2" />
                Aprobar
              </Button>
            </>
          )}
        </Dialog.Footer>
      </Dialog.Content>
    </Dialog>
  );
}
```

---

## ✅ VALIDACIÓN

```bash
pnpm dev
# Verificar:
# - /admin/moderacion muestra tabs
# - Cola de pendientes funciona
# - Reportes se muestran
# - Modal de revisión funciona
```

---

## 🔧 PASO 6: Servicio de Moderación

```typescript
// filepath: src/lib/services/moderationService.ts
import { apiClient } from "@/lib/apiClient";
import type { PaginatedResponse } from "@/types";

export type ModerationStatus = "pending" | "approved" | "rejected" | "flagged";
export type ModerationAction = "approve" | "reject" | "flag" | "unflag";
export type ContentType =
  | "listing"
  | "review"
  | "comment"
  | "profile"
  | "dealer";
export type ReportType =
  | "spam"
  | "fraud"
  | "inappropriate"
  | "misleading"
  | "other";

export interface PendingItem {
  id: string;
  type: ContentType;
  title: string;
  description: string;
  submittedBy: {
    id: string;
    name: string;
    email: string;
  };
  submittedAt: string;
  priority: "low" | "medium" | "high" | "urgent";
  previewUrl?: string;
  thumbnailUrl?: string;
  metadata?: Record<string, unknown>;
}

export interface Report {
  id: string;
  type: ReportType;
  targetType: ContentType;
  targetId: string;
  targetTitle: string;
  reason: string;
  reportedBy: {
    id: string;
    name: string;
  };
  reportedAt: string;
  status: "open" | "investigating" | "resolved" | "dismissed";
  assignedTo?: string;
  resolutionNotes?: string;
}

export interface ModerationStats {
  pending: number;
  reports: number;
  approvedToday: number;
  rejectedToday: number;
  avgResponseTime: number;
  flaggedItems: number;
}

export interface ModerationHistory {
  id: string;
  action: ModerationAction;
  itemType: ContentType;
  itemId: string;
  itemTitle: string;
  moderator: {
    id: string;
    name: string;
  };
  reason?: string;
  createdAt: string;
}

interface GetPendingParams {
  page?: number;
  pageSize?: number;
  type?: ContentType;
  priority?: string;
  sortBy?: "date" | "priority";
}

interface GetReportsParams {
  page?: number;
  pageSize?: number;
  type?: ReportType;
  status?: string;
  targetType?: ContentType;
}

interface ModerationActionDto {
  action: ModerationAction;
  reason?: string;
  internalNotes?: string;
}

export const moderationService = {
  // Estadísticas
  async getStats(): Promise<ModerationStats> {
    return apiClient.get("/admin/moderation/stats");
  },

  // Items pendientes
  async getPending(
    params: GetPendingParams = {},
  ): Promise<PaginatedResponse<PendingItem>> {
    const searchParams = new URLSearchParams();
    if (params.page) searchParams.set("page", params.page.toString());
    if (params.pageSize)
      searchParams.set("pageSize", params.pageSize.toString());
    if (params.type) searchParams.set("type", params.type);
    if (params.priority) searchParams.set("priority", params.priority);
    if (params.sortBy) searchParams.set("sortBy", params.sortBy);

    return apiClient.get(
      `/admin/moderation/pending?${searchParams.toString()}`,
    );
  },

  // Detalle de item pendiente
  async getPendingDetail(
    type: ContentType,
    id: string,
  ): Promise<PendingItem & { fullContent: unknown }> {
    return apiClient.get(`/admin/moderation/pending/${type}/${id}`);
  },

  // Ejecutar acción de moderación
  async executeAction(
    type: ContentType,
    id: string,
    data: ModerationActionDto,
  ): Promise<void> {
    return apiClient.post(`/admin/moderation/${type}/${id}/action`, data);
  },

  // Reportes
  async getReports(
    params: GetReportsParams = {},
  ): Promise<PaginatedResponse<Report>> {
    const searchParams = new URLSearchParams();
    if (params.page) searchParams.set("page", params.page.toString());
    if (params.pageSize)
      searchParams.set("pageSize", params.pageSize.toString());
    if (params.type) searchParams.set("type", params.type);
    if (params.status) searchParams.set("status", params.status);
    if (params.targetType) searchParams.set("targetType", params.targetType);

    return apiClient.get(
      `/admin/moderation/reports?${searchParams.toString()}`,
    );
  },

  // Detalle de reporte
  async getReportDetail(
    id: string,
  ): Promise<
    Report & { targetContent: unknown; history: ModerationHistory[] }
  > {
    return apiClient.get(`/admin/moderation/reports/${id}`);
  },

  // Resolver reporte
  async resolveReport(
    id: string,
    resolution: "resolved" | "dismissed",
    notes: string,
  ): Promise<void> {
    return apiClient.post(`/admin/moderation/reports/${id}/resolve`, {
      resolution,
      notes,
    });
  },

  // Asignar reporte
  async assignReport(id: string, moderatorId: string): Promise<void> {
    return apiClient.post(`/admin/moderation/reports/${id}/assign`, {
      moderatorId,
    });
  },

  // Historial
  async getHistory(
    page = 1,
    pageSize = 20,
  ): Promise<PaginatedResponse<ModerationHistory>> {
    return apiClient.get(
      `/admin/moderation/history?page=${page}&pageSize=${pageSize}`,
    );
  },

  // Bulk actions
  async bulkApprove(
    items: Array<{ type: ContentType; id: string }>,
  ): Promise<{ success: number; failed: number }> {
    return apiClient.post("/admin/moderation/bulk/approve", { items });
  },

  async bulkReject(
    items: Array<{ type: ContentType; id: string }>,
    reason: string,
  ): Promise<{ success: number; failed: number }> {
    return apiClient.post("/admin/moderation/bulk/reject", { items, reason });
  },
};
```

---

## 🎨 Estados de UI

### Loading State

```typescript
export function ModerationQueueSkeleton() {
  return (
    <div className="space-y-4">
      {[1, 2, 3, 4, 5].map((i) => (
        <div key={i} className="bg-white rounded-xl border p-4">
          <div className="flex gap-4">
            <Skeleton className="w-20 h-20 rounded-lg" />
            <div className="flex-1 space-y-2">
              <Skeleton className="h-5 w-3/4" />
              <Skeleton className="h-4 w-1/2" />
              <Skeleton className="h-4 w-1/4" />
            </div>
            <div className="flex gap-2">
              <Skeleton className="h-9 w-24" />
              <Skeleton className="h-9 w-24" />
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}
```

### Empty State

```typescript
export function ModerationEmptyState() {
  return (
    <div className="text-center py-12 bg-white rounded-xl border">
      <CheckCircle size={48} className="mx-auto text-green-400 mb-4" />
      <h3 className="text-lg font-semibold text-gray-900 mb-2">
        ¡Todo revisado!
      </h3>
      <p className="text-gray-500 max-w-sm mx-auto">
        No hay contenido pendiente de moderación. Buen trabajo.
      </p>
    </div>
  );
}
```

### Error State

```typescript
export function ModerationErrorState({ onRetry }: { onRetry: () => void }) {
  return (
    <div className="text-center py-12 bg-white rounded-xl border">
      <AlertTriangle size={48} className="mx-auto text-amber-400 mb-4" />
      <h3 className="text-lg font-semibold text-gray-900 mb-2">
        Error al cargar cola
      </h3>
      <p className="text-gray-500 mb-4">
        No se pudo obtener el contenido pendiente.
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
// filepath: e2e/admin/moderation.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsAdmin } from "../helpers/auth";

test.describe("Admin Moderation", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page, "moderator");
  });

  test("should display moderation stats", async ({ page }) => {
    await page.goto("/admin/moderacion");

    await expect(page.getByText("Pendientes")).toBeVisible();
    await expect(page.getByText("Reportes")).toBeVisible();
    await expect(page.getByText("Aprobados hoy")).toBeVisible();
  });

  test("should show pending items queue", async ({ page }) => {
    await page.goto("/admin/moderacion");

    await expect(
      page.getByRole("tab", { name: "Pendientes de revisión" }),
    ).toBeVisible();
    await expect(page.getByTestId("pending-queue")).toBeVisible();
  });

  test("should filter by content type", async ({ page }) => {
    await page.goto("/admin/moderacion");

    await page.getByRole("combobox", { name: "Tipo" }).click();
    await page.getByRole("option", { name: "Listings" }).click();

    await expect(page).toHaveURL(/type=listing/);
  });

  test("should approve item from queue", async ({ page }) => {
    await page.goto("/admin/moderacion");

    const firstItem = page.getByTestId("pending-item").first();
    await firstItem.getByRole("button", { name: "Aprobar" }).click();

    await expect(page.getByText("Contenido aprobado")).toBeVisible();
  });

  test("should reject item with reason", async ({ page }) => {
    await page.goto("/admin/moderacion");

    const firstItem = page.getByTestId("pending-item").first();
    await firstItem.getByRole("button", { name: "Revisar" }).click();

    // Modal opens
    await expect(page.getByRole("dialog")).toBeVisible();

    await page.getByRole("button", { name: "Rechazar" }).click();
    await page
      .getByLabel("Razón del rechazo")
      .fill("Contenido no cumple con políticas");
    await page.getByRole("button", { name: "Confirmar rechazo" }).click();

    await expect(page.getByText("Contenido rechazado")).toBeVisible();
  });

  test("should switch to reports tab", async ({ page }) => {
    await page.goto("/admin/moderacion");

    await page.getByRole("tab", { name: "Reportes" }).click();

    await expect(page.getByTestId("reports-queue")).toBeVisible();
  });

  test("should resolve report", async ({ page }) => {
    await page.goto("/admin/moderacion");
    await page.getByRole("tab", { name: "Reportes" }).click();

    const firstReport = page.getByTestId("report-item").first();
    await firstReport.click();

    await page.getByRole("button", { name: "Resolver" }).click();
    await page.getByLabel("Notas de resolución").fill("Revisado y resuelto");
    await page.getByRole("button", { name: "Confirmar resolución" }).click();

    await expect(page.getByText("Reporte resuelto")).toBeVisible();
  });

  test("should view moderation history", async ({ page }) => {
    await page.goto("/admin/moderacion");

    await page.getByRole("tab", { name: "Historial" }).click();

    await expect(page.getByTestId("moderation-history")).toBeVisible();
  });

  test("should perform bulk approve", async ({ page }) => {
    await page.goto("/admin/moderacion");

    // Select multiple items
    await page.getByRole("checkbox").first().click();
    await page.getByRole("checkbox").nth(1).click();
    await page.getByRole("checkbox").nth(2).click();

    await page.getByRole("button", { name: "Aprobar seleccionados" }).click();
    await page.getByRole("button", { name: "Confirmar" }).click();

    await expect(page.getByText(/3 items aprobados/)).toBeVisible();
  });
});
```

---

## 📊 Métricas y Analytics

```typescript
// filepath: src/lib/analytics/moderationEvents.ts
import { analytics } from "@/lib/analytics";

export const moderationEvents = {
  // Visualización
  viewQueue: (tab: string) => {
    analytics.track("admin_moderation_queue_viewed", { tab });
  },

  viewItemDetail: (type: string, id: string) => {
    analytics.track("admin_moderation_item_viewed", { type, id });
  },

  // Acciones
  approveItem: (type: string, id: string, timeSpent: number) => {
    analytics.track("admin_moderation_item_approved", { type, id, timeSpent });
  },

  rejectItem: (type: string, id: string, reason: string, timeSpent: number) => {
    analytics.track("admin_moderation_item_rejected", {
      type,
      id,
      reason,
      timeSpent,
    });
  },

  flagItem: (type: string, id: string) => {
    analytics.track("admin_moderation_item_flagged", { type, id });
  },

  // Reportes
  resolveReport: (id: string, resolution: string) => {
    analytics.track("admin_moderation_report_resolved", { id, resolution });
  },

  dismissReport: (id: string) => {
    analytics.track("admin_moderation_report_dismissed", { id });
  },

  // Bulk
  bulkAction: (action: string, count: number) => {
    analytics.track("admin_moderation_bulk_action", { action, count });
  },

  // Filtros
  filterQueue: (filters: Record<string, string>) => {
    analytics.track("admin_moderation_filtered", filters);
  },
};
```

---

## 🔐 Permisos y Roles

| Acción                      | ADM-SUPER | ADM-ADMIN | ADM-MOD | ADM-SUPPORT |
| --------------------------- | --------- | --------- | ------- | ----------- |
| Ver cola de pendientes      | ✅        | ✅        | ✅      | ❌          |
| Aprobar contenido           | ✅        | ✅        | ✅      | ❌          |
| Rechazar contenido          | ✅        | ✅        | ✅      | ❌          |
| Ver reportes                | ✅        | ✅        | ✅      | ✅          |
| Resolver reportes           | ✅        | ✅        | ✅      | ❌          |
| Descartar reportes          | ✅        | ✅        | ✅      | ❌          |
| Ver historial               | ✅        | ✅        | ✅      | ❌          |
| Bulk actions                | ✅        | ✅        | ❌      | ❌          |
| Configurar reglas auto      | ✅        | ✅        | ❌      | ❌          |
| Ver métricas de moderadores | ✅        | ✅        | ❌      | ❌          |

---

## ✅ Checklist de Implementación

### Backend (ModerationService)

- [ ] Endpoint `GET /api/admin/moderation/stats` estadísticas
- [ ] Endpoint `GET /api/admin/moderation/pending` cola de pendientes
- [ ] Endpoint `GET /api/admin/moderation/pending/{type}/{id}` detalle
- [ ] Endpoint `POST /api/admin/moderation/{type}/{id}/action` ejecutar acción
- [ ] Endpoint `GET /api/admin/moderation/reports` listar reportes
- [ ] Endpoint `GET /api/admin/moderation/reports/{id}` detalle reporte
- [ ] Endpoint `POST /api/admin/moderation/reports/{id}/resolve` resolver
- [ ] Endpoint `POST /api/admin/moderation/reports/{id}/assign` asignar
- [ ] Endpoint `GET /api/admin/moderation/history` historial
- [ ] Endpoint `POST /api/admin/moderation/bulk/approve` bulk approve
- [ ] Endpoint `POST /api/admin/moderation/bulk/reject` bulk reject
- [ ] Notificaciones automáticas a usuarios

### Frontend

- [ ] Página `/admin/moderacion` con tabs
- [ ] Componente `ModerationStats` cards de estadísticas
- [ ] Componente `PendingReviewQueue` cola de items
- [ ] Componente `ReportsQueue` cola de reportes
- [ ] Componente `ReviewDetailModal` modal de revisión
- [ ] Componente `ReportDetailPanel` panel de reporte
- [ ] Componente `ModerationHistory` historial
- [ ] Estados: Loading, Empty, Error
- [ ] Servicio `moderationService`
- [ ] Bulk selection y actions
- [ ] Tests E2E completos
- [ ] Analytics tracking

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/admin-moderation.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsAdmin } from "../helpers/auth";

test.describe("Admin Content Moderation", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("debe mostrar cola de moderación", async ({ page }) => {
    await page.goto("/admin/moderation");

    await expect(page.getByTestId("moderation-queue")).toBeVisible();
  });

  test("debe aprobar listing", async ({ page }) => {
    await page.goto("/admin/moderation");

    await page
      .getByTestId("listing-card")
      .first()
      .getByRole("button", { name: /aprobar/i })
      .click();
    await expect(page.getByText(/listing aprobado/i)).toBeVisible();
  });

  test("debe rechazar listing con razón", async ({ page }) => {
    await page.goto("/admin/moderation");

    await page
      .getByTestId("listing-card")
      .first()
      .getByRole("button", { name: /rechazar/i })
      .click();
    await page.getByRole("combobox", { name: /razón/i }).click();
    await page.getByRole("option", { name: /contenido inapropiado/i }).click();
    await page.getByRole("button", { name: /confirmar/i }).click();

    await expect(page.getByText(/listing rechazado/i)).toBeVisible();
  });

  test("debe realizar acción en bulk", async ({ page }) => {
    await page.goto("/admin/moderation");

    await page.getByTestId("select-all").click();
    await page.getByRole("button", { name: /aprobar seleccionados/i }).click();

    await expect(page.getByText(/listings aprobados/i)).toBeVisible();
  });

  test("debe filtrar por estado", async ({ page }) => {
    await page.goto("/admin/moderation");

    await page.getByRole("tab", { name: /flagged/i }).click();
    await expect(page).toHaveURL(/status=flagged/);
  });
});
```

---

## ➡️ SIGUIENTE PASO

Continuar con: [04-admin-compliance.md](./04-admin-compliance.md)
