---
title: "Admin: Moderación de Reviews Avanzada - Documentación Completa"
priority: P2
estimated_time: "2 horas"
dependencies: []
apis: ["UserService", "NotificationService", "MediaService"]
status: complete
last_updated: "2026-01-30"
---

# 🛡️ Admin: Moderación de Reviews Avanzada - Documentación Completa

> **Ruta:** `/admin/reviews/moderation` (Herramienta ESPECIALIZADA)  
> **Scope:** Moderación SOLO de reviews con features avanzadas (bulk actions, sentiment analysis)  
> **Acceso desde:** [14-admin-moderation.md](14-admin-moderation.md) (tab Reviews) o navegación directa  
> **Diferencia:** Dashboard general (14) = Overview | Este doc = Herramienta específica reviews

**Módulo:** 07-REVIEWS-REPUTACION  
**Componente:** Admin Review Moderation Enhanced  
**Última actualización:** Enero 30, 2026  
**Puerto Backend:** 5091 (ReviewService)  
**Estado:** ✅ Backend 100% | 🟡 UI 60% | 📖 Documentación Completa

---

## ✅ INTEGRACIÓN CON REVIEWSERVICE (process-matrix/21-REVIEWS-REPUTACION)

### Servicios Involucrados

| Servicio                | Puerto | Estado  | Responsabilidad       |
| ----------------------- | ------ | ------- | --------------------- |
| **ReviewService**       | 5091   | ✅ 90%  | Moderación de reviews |
| **NotificationService** | 5006   | ✅ 100% | Notificar decisiones  |
| **UserService**         | 5003   | ✅ 100% | Info de reviewers     |
| **AuditService**        | 5089   | ✅ 100% | Trail de moderación   |

### Proceso Relacionado: REVIEW-003 (Moderación de Reviews)

**Fuente:** `process-matrix/21-REVIEWS-REPUTACION/01-dealer-reviews.md`

| Paso | Acción                               | Sistema             | Actor     | Evidencia          |
| ---- | ------------------------------------ | ------------------- | --------- | ------------------ |
| 1    | Review reportado o spam detectado    | ReviewService       | Sistema   | Flag raised        |
| 2    | Status = Flagged                     | ReviewService       | Sistema   | Status changed     |
| 3    | Admin ve lista de reviews pendientes | Admin Panel         | USR-ADMIN | List viewed        |
| 4    | GET /api/reviews/pending             | Gateway             | USR-ADMIN | Request            |
| 5    | Admin revisa review                  | Admin Panel         | USR-ADMIN | Review examined    |
| 6    | Verificar políticas de contenido     | Admin Panel         | USR-ADMIN | Policy check       |
| 7    | **Decisión: Aprobar o Rechazar**     | Admin Panel         | USR-ADMIN | **Decision**       |
| 8    | POST /api/reviews/{id}/moderate      | Gateway             | USR-ADMIN | Request            |
| 9    | **Actualizar status**                | ReviewService       | Sistema   | **Status updated** |
| 10   | Si rechazado: Notificar al autor     | NotificationService | SYS-NOTIF | Notification       |
| 11   | Si aprobado: Actualizar summary      | ReviewService       | Sistema   | Summary updated    |
| 12   | **Audit trail completo**             | AuditService        | Sistema   | Complete audit     |

### Endpoints de Moderación

| Método | Endpoint                        | Descripción                       | Auth     |
| ------ | ------------------------------- | --------------------------------- | -------- |
| `GET`  | `/api/reviews/pending`          | Reviews pendientes de moderación  | ✅ Admin |
| `POST` | `/api/reviews/{id}/moderate`    | Moderar review (aprobar/rechazar) | ✅ Admin |
| `GET`  | `/api/reviews/flagged`          | Reviews reportados                | ✅ Admin |
| `GET`  | `/api/reviews/stats/moderation` | Estadísticas de moderación        | ✅ Admin |

### Rutas UI

| Ruta                             | Estado | Descripción        |
| -------------------------------- | ------ | ------------------ |
| `/admin/reviews/moderation`      | 🟡 60% | Cola de moderación |
| `/admin/reviews/moderation/{id}` | 🟡 60% | Detalle de review  |
| `/admin/reviews/stats`           | 🔴 0%  | Estadísticas       |

---

## 📋 Tabla de Contenidos

1. [Visión General](#-visión-general)
2. [Arquitectura de Moderación](#-arquitectura-de-moderación)
3. [Componentes](#-componentes)
4. [Páginas](#-páginas)
5. [Hooks](#-hooks)
6. [Servicios](#-servicios)
7. [Tipos TypeScript](#-tipos-typescript)
8. [Integración](#-integración)
9. [Testing](#-testing)
10. [Mejoras vs Genérico](#-mejoras-vs-genérico)

---

## 🎯 Visión General

### Propósito

Este documento extiende el sistema de moderación genérico existente (`14-admin-moderation.md`) con funcionalidades **específicas para reviews**, incluyendo:

- **Detección de fraude visual** (TrustScore, IP, User Agent)
- **Moderación en bulk** (aprobar/rechazar múltiples reviews)
- **Filtros avanzados** (TrustScore range, verified purchase, etc.)
- **Moderación de respuestas** de sellers
- **Análisis de sentimiento** visual

**Diferencia con 14-admin-moderation.md:**

- `14-admin-moderation.md`: Moderación genérica (contenido, usuarios, reportes)
- `37-admin-review-moderation-completo.md`: **Especializado en reviews** con fraud detection

---

### Stack Tecnológico

| Categoría          | Tecnología                       |
| ------------------ | -------------------------------- |
| **Framework**      | Next.js 14 App Router            |
| **UI**             | React 19, TailwindCSS, shadcn/ui |
| **Estado**         | React Query (TanStack Query v5)  |
| **Formularios**    | React Hook Form + Zod            |
| **Notificaciones** | react-hot-toast                  |
| **Gráficos**       | recharts (fraud analytics)       |

---

## 🏗️ Arquitectura de Moderación

### Flujo de Moderación con Fraud Detection

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                        FLUJO DE MODERACIÓN AVANZADA                          │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  1️⃣ REVIEW CREADO                                                            │
│  ├─> Usuario envía review (frontend)                                        │
│  ├─> POST /api/reviews                                                      │
│  ├─> Backend calcula TrustScore (0-100)                                     │
│  │   • IP geolocation (República Dominicana +10 pts)                        │
│  │   • User Agent analysis (mobile +5 pts, desktop +3 pts)                  │
│  │   • Verified purchase (+30 pts)                                          │
│  │   • Account age (>6 meses +15 pts)                                       │
│  │   • Previous reviews quality (+10 pts)                                   │
│  ├─> Si TrustScore < 40 → IsFlagged = true (auto-marcado)                  │
│  └─> IsApproved = null (pending moderation)                                 │
│                                                                              │
│  2️⃣ ENCOLAMIENTO                                                             │
│  ├─> Review aparece en Admin Dashboard                                      │
│  ├─> Ordenado por: IsFlagged DESC, TrustScore ASC, CreatedAt DESC           │
│  │   (Flagged primero, menor TrustScore primero, más recientes primero)     │
│  └─> Badge de alerta si TrustScore < 40                                     │
│                                                                              │
│  3️⃣ MODERADOR REVISA                                                         │
│  ├─> Ve FraudIndicators component:                                          │
│  │   • TrustScore con gauge visual (0-100)                                 │
│  │   • IP: 186.149.210.45 (🇩🇴 Santo Domingo) ✅                            │
│  │   • User Agent: Mobile Safari iOS 17.2 ✅                                │
│  │   • Account Age: 8 meses ✅                                              │
│  │   • Verified Purchase: ✅ Sí                                             │
│  ├─> Lee contenido del review                                               │
│  ├─> Ve sentiment analysis: 😊 Positivo (0.85)                              │
│  └─> Toma decisión: Aprobar / Rechazar / Marcar para review manual          │
│                                                                              │
│  4️⃣ ACCIÓN                                                                   │
│  ├─> Opción A: Aprobar                                                      │
│  │   • PATCH /api/reviews/{id}/moderate                                     │
│  │   • Body: { IsApproved: true, ModeratorId, ModeratedAt }                │
│  │   • Review visible para todos                                            │
│  │                                                                           │
│  ├─> Opción B: Rechazar                                                     │
│  │   • PATCH /api/reviews/{id}/moderate                                     │
│  │   • Body: { IsApproved: false, RejectionReason, ModeratorId }           │
│  │   • NotificationService envía email a buyer con razón                    │
│  │   • Review oculto en frontend                                            │
│  │                                                                           │
│  └─> Opción C: Bulk Action (múltiples reviews)                              │
│      • Checkbox select multiple                                             │
│      • "Aprobar Seleccionados" → POST /api/reviews/moderate/bulk            │
│      • Body: { ReviewIds: [...], Action: 'Approve', ModeratorId }           │
│      • Batch update en backend                                              │
│                                                                              │
│  5️⃣ SELLER RESPONDE (OPCIONAL)                                              │
│  ├─> Seller ve review aprobado                                              │
│  ├─> POST /api/reviews/{id}/respond                                         │
│  ├─> Response también entra a moderación                                    │
│  ├─> Moderador ve SellerResponseModeration component                        │
│  └─> Aprueba/rechaza respuesta                                              │
│                                                                              │
│  6️⃣ ANALYTICS                                                                │
│  ├─> Dashboard muestra métricas:                                            │
│  │   • Avg TrustScore: 72.5                                                │
│  │   • % Flagged: 8.5%                                                      │
│  │   • Avg moderation time: 2.3 horas                                       │
│  │   • Approval rate: 92%                                                   │
│  └─> Gráficos: TrustScore distribution, fraud trends, moderation velocity   │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

### Backend API Endpoints (ReviewService)

**Moderación de Reviews:**

| Método  | Endpoint                     | Descripción                         | Auth  |
| ------- | ---------------------------- | ----------------------------------- | ----- |
| `GET`   | `/api/reviews`               | Listar con filtros de moderación    | Admin |
| `GET`   | `/api/reviews/{id}`          | Detalle con fraud data              | Admin |
| `PATCH` | `/api/reviews/{id}/moderate` | Aprobar/rechazar                    | Admin |
| `POST`  | `/api/reviews/moderate/bulk` | Bulk approve/reject                 | Admin |
| `GET`   | `/api/reviews/pending`       | Solo pendientes (IsApproved = null) | Admin |
| `GET`   | `/api/reviews/flagged`       | Solo flagged (IsFlagged = true)     | Admin |

**Moderación de Respuestas:**

| Método  | Endpoint                                            | Descripción                | Auth  |
| ------- | --------------------------------------------------- | -------------------------- | ----- |
| `GET`   | `/api/reviews/{id}/responses`                       | Responses del seller       | Admin |
| `PATCH` | `/api/reviews/{id}/responses/{responseId}/moderate` | Aprobar/rechazar respuesta | Admin |

**Analytics:**

| Método | Endpoint                             | Descripción            | Auth  |
| ------ | ------------------------------------ | ---------------------- | ----- |
| `GET`  | `/api/reviews/admin/stats`           | Estadísticas generales | Admin |
| `GET`  | `/api/reviews/admin/fraud-analytics` | Métricas de fraude     | Admin |

---

### Parámetros de Query para GET /api/reviews

```typescript
interface ReviewModerationFilters {
  // Filtros generales
  page?: number; // default: 1
  pageSize?: number; // default: 20

  // Moderación
  isApproved?: boolean | null; // null = pendiente, true = aprobado, false = rechazado
  isFlagged?: boolean; // Solo reviews marcados con fraude

  // TrustScore
  minTrustScore?: number; // 0-100
  maxTrustScore?: number; // 0-100

  // Verificación
  isVerifiedPurchase?: boolean;

  // Fechas
  startDate?: string; // ISO 8601
  endDate?: string;

  // Búsqueda
  searchTerm?: string; // Buscar en content, title, buyer name

  // Ordenamiento
  sortBy?: "CreatedAt" | "TrustScore" | "HelpfulVotes";
  sortOrder?: "asc" | "desc"; // default: desc
}
```

**Ejemplo de uso:**

```typescript
// Reviews flagged pendientes de moderación
GET /api/reviews?isApproved=null&isFlagged=true&sortBy=TrustScore&sortOrder=asc

// Reviews con TrustScore bajo (< 40)
GET /api/reviews?maxTrustScore=40&isApproved=null

// Reviews verificados rechazados
GET /api/reviews?isVerifiedPurchase=true&isApproved=false
```

---

## 🧩 Componentes

### 1. FraudIndicators

**Propósito:** Visualizar datos de fraud detection para ayudar al moderador.

**Ubicación:** `src/components/admin/reviews/FraudIndicators.tsx`

**Props:**

```typescript
interface FraudIndicatorsProps {
  review: ReviewWithFraudData;
  compact?: boolean; // Vista compacta para tablas
}
```

**Implementación:**

```typescript
// src/components/admin/reviews/FraudIndicators.tsx
import React from 'react';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import {
  Shield,
  MapPin,
  Monitor,
  Calendar,
  CheckCircle,
  AlertTriangle,
  XCircle
} from 'lucide-react';
import { ReviewWithFraudData } from '@/types/reviews';

interface FraudIndicatorsProps {
  review: ReviewWithFraudData;
  compact?: boolean;
}

export const FraudIndicators: React.FC<FraudIndicatorsProps> = ({
  review,
  compact = false
}) => {
  const getTrustScoreColor = (score: number): string => {
    if (score >= 70) return 'text-green-600';
    if (score >= 40) return 'text-yellow-600';
    return 'text-red-600';
  };

  const getTrustScoreLabel = (score: number): string => {
    if (score >= 70) return 'Confiable';
    if (score >= 40) return 'Sospechoso';
    return 'Alto Riesgo';
  };

  const getTrustScoreBgColor = (score: number): string => {
    if (score >= 70) return 'bg-green-100';
    if (score >= 40) return 'bg-yellow-100';
    return 'bg-red-100';
  };

  // Vista compacta para usar en tablas
  if (compact) {
    return (
      <div className="flex items-center gap-2">
        {/* TrustScore badge */}
        <Badge
          variant={review.trustScore >= 70 ? 'default' : 'destructive'}
          className="font-mono"
        >
          {review.trustScore}
        </Badge>

        {/* Verified purchase indicator */}
        {review.isVerifiedPurchase && (
          <CheckCircle className="w-4 h-4 text-green-600" />
        )}

        {/* Flagged indicator */}
        {review.isFlagged && (
          <AlertTriangle className="w-4 h-4 text-red-600" />
        )}
      </div>
    );
  }

  // Vista completa para detail page
  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Shield className="w-5 h-5" />
          Indicadores de Fraude
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-6">
        {/* TrustScore Gauge */}
        <div>
          <div className="flex items-center justify-between mb-2">
            <span className="text-sm font-medium">TrustScore</span>
            <div className="flex items-center gap-2">
              <span className={`text-2xl font-bold ${getTrustScoreColor(review.trustScore)}`}>
                {review.trustScore}
              </span>
              <Badge
                className={getTrustScoreBgColor(review.trustScore)}
              >
                {getTrustScoreLabel(review.trustScore)}
              </Badge>
            </div>
          </div>
          <Progress
            value={review.trustScore}
            className="h-3"
            indicatorClassName={
              review.trustScore >= 70 ? 'bg-green-600' :
              review.trustScore >= 40 ? 'bg-yellow-600' :
              'bg-red-600'
            }
          />
          <p className="text-xs text-muted-foreground mt-1">
            Calculado basado en IP, user agent, cuenta, y compra verificada
          </p>
        </div>

        {/* Flagged Status */}
        {review.isFlagged && (
          <div className="flex items-start gap-3 p-3 bg-red-50 border border-red-200 rounded-lg">
            <AlertTriangle className="w-5 h-5 text-red-600 mt-0.5" />
            <div>
              <p className="font-medium text-red-900">
                Review Marcado Automáticamente
              </p>
              <p className="text-sm text-red-700">
                TrustScore inferior a 40. Revisar manualmente antes de aprobar.
              </p>
            </div>
          </div>
        )}

        {/* IP Address */}
        <div className="flex items-start gap-3">
          <MapPin className="w-5 h-5 text-muted-foreground mt-0.5" />
          <div className="flex-1">
            <p className="text-sm font-medium">Dirección IP</p>
            <p className="text-sm text-muted-foreground font-mono">
              {review.ipAddress || 'No disponible'}
            </p>
            {review.ipLocation && (
              <p className="text-xs text-muted-foreground mt-1">
                📍 {review.ipLocation.city}, {review.ipLocation.country}
              </p>
            )}
          </div>
          {review.ipLocation?.country === 'República Dominicana' && (
            <CheckCircle className="w-4 h-4 text-green-600" />
          )}
        </div>

        {/* User Agent */}
        <div className="flex items-start gap-3">
          <Monitor className="w-5 h-5 text-muted-foreground mt-0.5" />
          <div className="flex-1">
            <p className="text-sm font-medium">Dispositivo</p>
            <p className="text-sm text-muted-foreground">
              {review.userAgentInfo?.device || 'No disponible'}
            </p>
            {review.userAgentInfo && (
              <p className="text-xs text-muted-foreground mt-1">
                {review.userAgentInfo.browser} • {review.userAgentInfo.os}
              </p>
            )}
          </div>
        </div>

        {/* Account Age */}
        <div className="flex items-start gap-3">
          <Calendar className="w-5 h-5 text-muted-foreground mt-0.5" />
          <div className="flex-1">
            <p className="text-sm font-medium">Edad de Cuenta</p>
            <p className="text-sm text-muted-foreground">
              {review.accountAge ? `${review.accountAge} meses` : 'No disponible'}
            </p>
          </div>
          {review.accountAge && review.accountAge >= 6 && (
            <CheckCircle className="w-4 h-4 text-green-600" />
          )}
        </div>

        {/* Verified Purchase */}
        <div className="flex items-start gap-3">
          <CheckCircle className={`w-5 h-5 mt-0.5 ${
            review.isVerifiedPurchase ? 'text-green-600' : 'text-muted-foreground'
          }`} />
          <div className="flex-1">
            <p className="text-sm font-medium">Compra Verificada</p>
            <p className="text-sm text-muted-foreground">
              {review.isVerifiedPurchase ? (
                <span className="text-green-700 font-medium">
                  ✓ Sí (Orden #{review.orderId})
                </span>
              ) : (
                <span className="text-red-700">
                  ✗ No verificada
                </span>
              )}
            </p>
          </div>
        </div>

        {/* Previous Reviews Quality */}
        {review.previousReviewsStats && (
          <div className="p-3 bg-gray-50 rounded-lg">
            <p className="text-sm font-medium mb-2">Reviews Previos del Usuario</p>
            <div className="grid grid-cols-3 gap-2 text-center">
              <div>
                <p className="text-2xl font-bold">{review.previousReviewsStats.total}</p>
                <p className="text-xs text-muted-foreground">Total</p>
              </div>
              <div>
                <p className="text-2xl font-bold text-green-600">
                  {review.previousReviewsStats.approved}
                </p>
                <p className="text-xs text-muted-foreground">Aprobados</p>
              </div>
              <div>
                <p className="text-2xl font-bold text-red-600">
                  {review.previousReviewsStats.rejected}
                </p>
                <p className="text-xs text-muted-foreground">Rechazados</p>
              </div>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
};
```

---

### 2. ReviewModerationQueue

**Propósito:** Tabla mejorada con bulk actions y filtros avanzados específicos para reviews.

**Ubicación:** `src/components/admin/reviews/ReviewModerationQueue.tsx`

**Props:**

```typescript
interface ReviewModerationQueueProps {
  filters?: ReviewModerationFilters;
  onFiltersChange?: (filters: ReviewModerationFilters) => void;
}
```

**Implementación:**

```typescript
// src/components/admin/reviews/ReviewModerationQueue.tsx
import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Badge } from '@/components/ui/badge';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import {
  CheckCircle,
  XCircle,
  Eye,
  ChevronLeft,
  ChevronRight,
  Loader2
} from 'lucide-react';
import { toast } from 'react-hot-toast';
import { reviewModerationService } from '@/services/reviewModerationService';
import { ReviewModerationFilters, ReviewSummary } from '@/types/reviews';
import { FraudIndicators } from './FraudIndicators';
import { ReviewModerationFilters as FiltersComponent } from './ReviewModerationFilters';

interface ReviewModerationQueueProps {
  filters?: ReviewModerationFilters;
  onFiltersChange?: (filters: ReviewModerationFilters) => void;
}

export const ReviewModerationQueue: React.FC<ReviewModerationQueueProps> = ({
  filters: externalFilters,
  onFiltersChange
}) => {
  const queryClient = useQueryClient();
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [internalFilters, setInternalFilters] = useState<ReviewModerationFilters>({
    page: 1,
    pageSize: 20,
    isApproved: null, // Solo pendientes
    sortBy: 'CreatedAt',
    sortOrder: 'desc'
  });

  const filters = externalFilters || internalFilters;

  // Query: Listar reviews pendientes
  const { data, isLoading } = useQuery({
    queryKey: ['reviews', 'moderation', filters],
    queryFn: () => reviewModerationService.getReviews(filters),
  });

  // Mutation: Moderar individual
  const moderateMutation = useMutation({
    mutationFn: ({ id, action, reason }: {
      id: string;
      action: 'approve' | 'reject';
      reason?: string;
    }) => reviewModerationService.moderate(id, action, reason),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['reviews', 'moderation'] });
      toast.success(
        variables.action === 'approve'
          ? 'Review aprobado'
          : 'Review rechazado'
      );
    },
    onError: (error: Error) => {
      toast.error(`Error: ${error.message}`);
    }
  });

  // Mutation: Bulk moderation
  const bulkModerateMutation = useMutation({
    mutationFn: ({ ids, action }: { ids: string[]; action: 'approve' | 'reject' }) =>
      reviewModerationService.moderateBulk(ids, action),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['reviews', 'moderation'] });
      setSelectedIds([]);
      toast.success(`${variables.ids.length} reviews ${
        variables.action === 'approve' ? 'aprobados' : 'rechazados'
      }`);
    },
    onError: (error: Error) => {
      toast.error(`Error en bulk action: ${error.message}`);
    }
  });

  const handleSelectAll = () => {
    if (selectedIds.length === data?.items.length) {
      setSelectedIds([]);
    } else {
      setSelectedIds(data?.items.map(r => r.id) || []);
    }
  };

  const handleSelectOne = (id: string) => {
    if (selectedIds.includes(id)) {
      setSelectedIds(selectedIds.filter(i => i !== id));
    } else {
      setSelectedIds([...selectedIds, id]);
    }
  };

  const handleBulkApprove = () => {
    if (selectedIds.length === 0) return;
    bulkModerateMutation.mutate({ ids: selectedIds, action: 'approve' });
  };

  const handleBulkReject = () => {
    if (selectedIds.length === 0) return;
    // TODO: Implementar modal para ingresar razón de rechazo
    bulkModerateMutation.mutate({ ids: selectedIds, action: 'reject' });
  };

  const handlePageChange = (newPage: number) => {
    const newFilters = { ...filters, page: newPage };
    if (onFiltersChange) {
      onFiltersChange(newFilters);
    } else {
      setInternalFilters(newFilters);
    }
  };

  if (isLoading) {
    return (
      <Card>
        <CardContent className="flex items-center justify-center py-12">
          <Loader2 className="w-8 h-8 animate-spin text-muted-foreground" />
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-4">
      {/* Filters */}
      <FiltersComponent
        filters={filters}
        onChange={onFiltersChange || setInternalFilters}
      />

      {/* Bulk Actions Bar */}
      {selectedIds.length > 0 && (
        <Card className="bg-blue-50 border-blue-200">
          <CardContent className="py-3">
            <div className="flex items-center justify-between">
              <span className="text-sm font-medium">
                {selectedIds.length} review(s) seleccionado(s)
              </span>
              <div className="flex gap-2">
                <Button
                  size="sm"
                  onClick={handleBulkApprove}
                  disabled={bulkModerateMutation.isPending}
                  className="bg-green-600 hover:bg-green-700"
                >
                  <CheckCircle className="w-4 h-4 mr-2" />
                  Aprobar Todos
                </Button>
                <Button
                  size="sm"
                  variant="destructive"
                  onClick={handleBulkReject}
                  disabled={bulkModerateMutation.isPending}
                >
                  <XCircle className="w-4 h-4 mr-2" />
                  Rechazar Todos
                </Button>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Table */}
      <Card>
        <CardHeader>
          <CardTitle>
            Cola de Moderación ({data?.totalCount || 0})
          </CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-12">
                  <Checkbox
                    checked={selectedIds.length === data?.items.length}
                    onCheckedChange={handleSelectAll}
                  />
                </TableHead>
                <TableHead>Review</TableHead>
                <TableHead>Fraud Indicators</TableHead>
                <TableHead>Fecha</TableHead>
                <TableHead className="text-right">Acciones</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data?.items.map((review) => (
                <TableRow key={review.id}>
                  <TableCell>
                    <Checkbox
                      checked={selectedIds.includes(review.id)}
                      onCheckedChange={() => handleSelectOne(review.id)}
                    />
                  </TableCell>

                  <TableCell>
                    <div className="space-y-1">
                      {/* Rating */}
                      <div className="flex items-center gap-2">
                        <div className="flex">
                          {[...Array(5)].map((_, i) => (
                            <span key={i} className={
                              i < review.rating
                                ? 'text-yellow-400'
                                : 'text-gray-300'
                            }>★</span>
                          ))}
                        </div>
                        <span className="text-sm text-muted-foreground">
                          {review.rating}/5
                        </span>
                      </div>

                      {/* Title */}
                      {review.title && (
                        <p className="font-medium">{review.title}</p>
                      )}

                      {/* Content preview */}
                      <p className="text-sm text-muted-foreground line-clamp-2">
                        {review.content}
                      </p>

                      {/* Buyer & Seller */}
                      <div className="flex items-center gap-2 text-xs text-muted-foreground">
                        <span>Por: {review.buyerName}</span>
                        <span>•</span>
                        <span>Dealer: {review.sellerName}</span>
                      </div>
                    </div>
                  </TableCell>

                  <TableCell>
                    <FraudIndicators review={review} compact />
                  </TableCell>

                  <TableCell>
                    <span className="text-sm text-muted-foreground">
                      {new Date(review.createdAt).toLocaleDateString('es-DO')}
                    </span>
                  </TableCell>

                  <TableCell className="text-right">
                    <div className="flex items-center justify-end gap-2">
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => {/* TODO: Open detail modal */}}
                      >
                        <Eye className="w-4 h-4" />
                      </Button>
                      <Button
                        size="sm"
                        onClick={() => moderateMutation.mutate({
                          id: review.id,
                          action: 'approve'
                        })}
                        disabled={moderateMutation.isPending}
                        className="bg-green-600 hover:bg-green-700"
                      >
                        <CheckCircle className="w-4 h-4" />
                      </Button>
                      <Button
                        size="sm"
                        variant="destructive"
                        onClick={() => moderateMutation.mutate({
                          id: review.id,
                          action: 'reject'
                        })}
                        disabled={moderateMutation.isPending}
                      >
                        <XCircle className="w-4 h-4" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>

          {/* Pagination */}
          {data && data.totalPages > 1 && (
            <div className="flex items-center justify-between mt-4">
              <span className="text-sm text-muted-foreground">
                Página {data.currentPage} de {data.totalPages}
              </span>
              <div className="flex gap-2">
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => handlePageChange(filters.page! - 1)}
                  disabled={filters.page === 1}
                >
                  <ChevronLeft className="w-4 h-4" />
                  Anterior
                </Button>
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => handlePageChange(filters.page! + 1)}
                  disabled={filters.page === data.totalPages}
                >
                  Siguiente
                  <ChevronRight className="w-4 h-4" />
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
};
```

---

### 3. ReviewModerationFilters

**Propósito:** Panel de filtros avanzados específicos para reviews.

**Ubicación:** `src/components/admin/reviews/ReviewModerationFilters.tsx`

```typescript
// src/components/admin/reviews/ReviewModerationFilters.tsx
import React from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Slider } from '@/components/ui/slider';
import { Badge } from '@/components/ui/badge';
import { X } from 'lucide-react';
import { ReviewModerationFilters as Filters } from '@/types/reviews';

interface ReviewModerationFiltersProps {
  filters: Filters;
  onChange: (filters: Filters) => void;
}

export const ReviewModerationFilters: React.FC<ReviewModerationFiltersProps> = ({
  filters,
  onChange
}) => {
  const [trustScoreRange, setTrustScoreRange] = React.useState<[number, number]>([
    filters.minTrustScore || 0,
    filters.maxTrustScore || 100
  ]);

  const handleChange = (key: keyof Filters, value: any) => {
    onChange({ ...filters, [key]: value, page: 1 }); // Reset a página 1
  };

  const handleTrustScoreChange = (value: [number, number]) => {
    setTrustScoreRange(value);
    onChange({
      ...filters,
      minTrustScore: value[0],
      maxTrustScore: value[1],
      page: 1
    });
  };

  const handleReset = () => {
    onChange({
      page: 1,
      pageSize: 20,
      sortBy: 'CreatedAt',
      sortOrder: 'desc'
    });
    setTrustScoreRange([0, 100]);
  };

  const activeFiltersCount = [
    filters.isApproved !== undefined,
    filters.isFlagged !== undefined,
    filters.isVerifiedPurchase !== undefined,
    filters.minTrustScore !== undefined || filters.maxTrustScore !== undefined,
    filters.searchTerm
  ].filter(Boolean).length;

  return (
    <Card>
      <CardContent className="pt-6">
        <div className="space-y-4">
          {/* Header con reset */}
          <div className="flex items-center justify-between">
            <h3 className="text-lg font-semibold">Filtros</h3>
            {activeFiltersCount > 0 && (
              <div className="flex items-center gap-2">
                <Badge variant="secondary">
                  {activeFiltersCount} filtro(s) activo(s)
                </Badge>
                <Button size="sm" variant="ghost" onClick={handleReset}>
                  <X className="w-4 h-4 mr-1" />
                  Limpiar
                </Button>
              </div>
            )}
          </div>

          {/* Grid de filtros */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            {/* Estado de moderación */}
            <div className="space-y-2">
              <Label>Estado</Label>
              <Select
                value={
                  filters.isApproved === undefined ? 'all' :
                  filters.isApproved === null ? 'pending' :
                  filters.isApproved ? 'approved' : 'rejected'
                }
                onValueChange={(value) => {
                  handleChange(
                    'isApproved',
                    value === 'all' ? undefined :
                    value === 'pending' ? null :
                    value === 'approved'
                  );
                }}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Todos</SelectItem>
                  <SelectItem value="pending">⏳ Pendiente</SelectItem>
                  <SelectItem value="approved">✅ Aprobado</SelectItem>
                  <SelectItem value="rejected">❌ Rechazado</SelectItem>
                </SelectContent>
              </Select>
            </div>

            {/* Flagged */}
            <div className="space-y-2">
              <Label>Fraude</Label>
              <Select
                value={
                  filters.isFlagged === undefined ? 'all' :
                  filters.isFlagged ? 'flagged' : 'notFlagged'
                }
                onValueChange={(value) => {
                  handleChange(
                    'isFlagged',
                    value === 'all' ? undefined : value === 'flagged'
                  );
                }}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Todos</SelectItem>
                  <SelectItem value="flagged">🚩 Marcados</SelectItem>
                  <SelectItem value="notFlagged">✓ Normales</SelectItem>
                </SelectContent>
              </Select>
            </div>

            {/* Verified Purchase */}
            <div className="space-y-2">
              <Label>Compra Verificada</Label>
              <Select
                value={
                  filters.isVerifiedPurchase === undefined ? 'all' :
                  filters.isVerifiedPurchase ? 'verified' : 'notVerified'
                }
                onValueChange={(value) => {
                  handleChange(
                    'isVerifiedPurchase',
                    value === 'all' ? undefined : value === 'verified'
                  );
                }}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Todos</SelectItem>
                  <SelectItem value="verified">✓ Verificados</SelectItem>
                  <SelectItem value="notVerified">Sin verificar</SelectItem>
                </SelectContent>
              </Select>
            </div>

            {/* Ordenamiento */}
            <div className="space-y-2">
              <Label>Ordenar por</Label>
              <Select
                value={filters.sortBy || 'CreatedAt'}
                onValueChange={(value: any) => handleChange('sortBy', value)}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="CreatedAt">Fecha</SelectItem>
                  <SelectItem value="TrustScore">TrustScore</SelectItem>
                  <SelectItem value="HelpfulVotes">Más útiles</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          {/* TrustScore Range Slider */}
          <div className="space-y-2">
            <Label>
              TrustScore: {trustScoreRange[0]} - {trustScoreRange[1]}
            </Label>
            <Slider
              value={trustScoreRange}
              onValueChange={handleTrustScoreChange}
              min={0}
              max={100}
              step={5}
              className="mt-2"
            />
            <div className="flex justify-between text-xs text-muted-foreground">
              <span>0 (Alto riesgo)</span>
              <span>40 (Sospechoso)</span>
              <span>70 (Confiable)</span>
              <span>100 (Perfecto)</span>
            </div>
          </div>

          {/* Búsqueda */}
          <div className="space-y-2">
            <Label>Buscar</Label>
            <Input
              placeholder="Buscar por contenido, comprador, vendedor..."
              value={filters.searchTerm || ''}
              onChange={(e) => handleChange('searchTerm', e.target.value)}
            />
          </div>
        </div>
      </CardContent>
    </Card>
  );
};
```

---

### 4. SellerResponseModeration

**Propósito:** Moderar respuestas de sellers a reviews (si requieren aprobación).

**Ubicación:** `src/components/admin/reviews/SellerResponseModeration.tsx`

```typescript
// src/components/admin/reviews/SellerResponseModeration.tsx
import React from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { CheckCircle, XCircle, MessageSquare } from 'lucide-react';
import { toast } from 'react-hot-toast';
import { reviewModerationService } from '@/services/reviewModerationService';
import { SellerResponse } from '@/types/reviews';

interface SellerResponseModerationProps {
  reviewId: string;
  response: SellerResponse;
}

export const SellerResponseModeration: React.FC<SellerResponseModerationProps> = ({
  reviewId,
  response
}) => {
  const queryClient = useQueryClient();

  const moderateMutation = useMutation({
    mutationFn: (action: 'approve' | 'reject') =>
      reviewModerationService.moderateResponse(reviewId, response.id, action),
    onSuccess: (_, action) => {
      queryClient.invalidateQueries({ queryKey: ['reviews', reviewId] });
      toast.success(
        action === 'approve'
          ? 'Respuesta aprobada'
          : 'Respuesta rechazada'
      );
    },
    onError: (error: Error) => {
      toast.error(`Error: ${error.message}`);
    }
  });

  if (response.isApproved !== null) {
    // Ya moderada
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <MessageSquare className="w-5 h-5" />
            Respuesta del Vendedor
            <Badge variant={response.isApproved ? 'default' : 'destructive'}>
              {response.isApproved ? 'Aprobada' : 'Rechazada'}
            </Badge>
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm">{response.content}</p>
          <p className="text-xs text-muted-foreground mt-2">
            Por: {response.sellerName} • {new Date(response.createdAt).toLocaleDateString('es-DO')}
          </p>
        </CardContent>
      </Card>
    );
  }

  // Pendiente de moderación
  return (
    <Card className="border-yellow-200 bg-yellow-50">
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <MessageSquare className="w-5 h-5" />
          Respuesta del Vendedor
          <Badge variant="outline" className="bg-yellow-100">
            Pendiente Moderación
          </Badge>
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="p-3 bg-white rounded-lg">
          <p className="text-sm">{response.content}</p>
          <p className="text-xs text-muted-foreground mt-2">
            Por: {response.sellerName} • {new Date(response.createdAt).toLocaleDateString('es-DO')}
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Button
            onClick={() => moderateMutation.mutate('approve')}
            disabled={moderateMutation.isPending}
            className="flex-1 bg-green-600 hover:bg-green-700"
          >
            <CheckCircle className="w-4 h-4 mr-2" />
            Aprobar Respuesta
          </Button>
          <Button
            variant="destructive"
            onClick={() => moderateMutation.mutate('reject')}
            disabled={moderateMutation.isPending}
            className="flex-1"
          >
            <XCircle className="w-4 h-4 mr-2" />
            Rechazar Respuesta
          </Button>
        </div>

        <p className="text-xs text-muted-foreground">
          💡 Las respuestas deben ser profesionales, no ofensivas, y abordar los puntos del review.
        </p>
      </CardContent>
    </Card>
  );
};
```

---

### 5. FraudAnalyticsDashboard

**Propósito:** Dashboard con métricas de fraude para monitorear efectividad del sistema.

**Ubicación:** `src/components/admin/reviews/FraudAnalyticsDashboard.tsx`

```typescript
// src/components/admin/reviews/FraudAnalyticsDashboard.tsx
import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  PieChart,
  Pie,
  Cell,
  ResponsiveContainer
} from 'recharts';
import { Shield, TrendingDown, TrendingUp, AlertTriangle } from 'lucide-react';
import { reviewModerationService } from '@/services/reviewModerationService';

export const FraudAnalyticsDashboard: React.FC = () => {
  const { data: stats } = useQuery({
    queryKey: ['reviews', 'fraud-analytics'],
    queryFn: () => reviewModerationService.getFraudAnalytics(),
    refetchInterval: 60000, // Cada 1 minuto
  });

  if (!stats) return null;

  const COLORS = {
    safe: '#10b981',    // green-500
    suspicious: '#f59e0b', // amber-500
    risky: '#ef4444'    // red-500
  };

  return (
    <div className="space-y-6">
      {/* KPIs Row */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        {/* Avg TrustScore */}
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">TrustScore Promedio</p>
                <p className="text-3xl font-bold text-green-600">
                  {stats.avgTrustScore.toFixed(1)}
                </p>
              </div>
              <Shield className="w-10 h-10 text-green-600 opacity-20" />
            </div>
            <p className="text-xs text-muted-foreground mt-2">
              {stats.trustScoreTrend >= 0 ? '↗' : '↘'}
              {Math.abs(stats.trustScoreTrend).toFixed(1)}% vs mes anterior
            </p>
          </CardContent>
        </Card>

        {/* % Flagged */}
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Reviews Marcados</p>
                <p className="text-3xl font-bold text-yellow-600">
                  {stats.flaggedPercentage.toFixed(1)}%
                </p>
              </div>
              <AlertTriangle className="w-10 h-10 text-yellow-600 opacity-20" />
            </div>
            <p className="text-xs text-muted-foreground mt-2">
              {stats.flaggedCount} de {stats.totalReviews} reviews
            </p>
          </CardContent>
        </Card>

        {/* Approval Rate */}
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Tasa de Aprobación</p>
                <p className="text-3xl font-bold text-blue-600">
                  {stats.approvalRate.toFixed(1)}%
                </p>
              </div>
              <TrendingUp className="w-10 h-10 text-blue-600 opacity-20" />
            </div>
            <p className="text-xs text-muted-foreground mt-2">
              {stats.approvedCount} aprobados
            </p>
          </CardContent>
        </Card>

        {/* Avg Moderation Time */}
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Tiempo de Moderación</p>
                <p className="text-3xl font-bold text-purple-600">
                  {stats.avgModerationTimeHours.toFixed(1)}h
                </p>
              </div>
              <TrendingDown className="w-10 h-10 text-purple-600 opacity-20" />
            </div>
            <p className="text-xs text-muted-foreground mt-2">
              Meta: &lt;3 horas
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Charts Row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* TrustScore Distribution */}
        <Card>
          <CardHeader>
            <CardTitle>Distribución de TrustScore</CardTitle>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={300}>
              <BarChart data={stats.trustScoreDistribution}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="range" />
                <YAxis />
                <Tooltip />
                <Bar dataKey="count" fill="#3b82f6" />
              </BarChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        {/* Fraud by Category */}
        <Card>
          <CardHeader>
            <CardTitle>Reviews por Categoría de Riesgo</CardTitle>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={300}>
              <PieChart>
                <Pie
                  data={[
                    { name: 'Confiable (70-100)', value: stats.safeCount, color: COLORS.safe },
                    { name: 'Sospechoso (40-69)', value: stats.suspiciousCount, color: COLORS.suspicious },
                    { name: 'Alto Riesgo (0-39)', value: stats.riskyCount, color: COLORS.risky },
                  ]}
                  cx="50%"
                  cy="50%"
                  labelLine={false}
                  label={({ name, percent }) => `${name}: ${(percent * 100).toFixed(0)}%`}
                  outerRadius={100}
                  fill="#8884d8"
                  dataKey="value"
                >
                  {[COLORS.safe, COLORS.suspicious, COLORS.risky].map((color, index) => (
                    <Cell key={`cell-${index}`} fill={color} />
                  ))}
                </Pie>
                <Tooltip />
              </PieChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>
      </div>

      {/* Alerts */}
      {stats.alertsCount > 0 && (
        <Card className="border-red-200 bg-red-50">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-red-900">
              <AlertTriangle className="w-5 h-5" />
              Alertas Activas
            </CardTitle>
          </CardHeader>
          <CardContent>
            <ul className="space-y-2">
              {stats.alerts.map((alert, index) => (
                <li key={index} className="text-sm text-red-800">
                  • {alert.message}
                </li>
              ))}
            </ul>
          </CardContent>
        </Card>
      )}
    </div>
  );
};
```

---

## 📄 Páginas

### 1. Admin Review Moderation Page

**Ruta:** `/admin/reviews/moderation`

**Ubicación:** `src/app/admin/reviews/moderation/page.tsx`

```typescript
// src/app/admin/reviews/moderation/page.tsx
import React from 'react';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { ReviewModerationQueue } from '@/components/admin/reviews/ReviewModerationQueue';
import { FraudAnalyticsDashboard } from '@/components/admin/reviews/FraudAnalyticsDashboard';
import { Shield, BarChart3 } from 'lucide-react';

export default function ReviewModerationPage() {
  return (
    <div className="container mx-auto py-8 px-4">
      <div className="mb-6">
        <h1 className="text-3xl font-bold">Moderación de Reviews</h1>
        <p className="text-muted-foreground">
          Gestiona reviews con detección de fraude y moderación en bulk
        </p>
      </div>

      <Tabs defaultValue="queue">
        <TabsList className="grid w-full max-w-md grid-cols-2">
          <TabsTrigger value="queue" className="flex items-center gap-2">
            <Shield className="w-4 h-4" />
            Cola de Moderación
          </TabsTrigger>
          <TabsTrigger value="analytics" className="flex items-center gap-2">
            <BarChart3 className="w-4 h-4" />
            Analytics de Fraude
          </TabsTrigger>
        </TabsList>

        <TabsContent value="queue" className="mt-6">
          <ReviewModerationQueue />
        </TabsContent>

        <TabsContent value="analytics" className="mt-6">
          <FraudAnalyticsDashboard />
        </TabsContent>
      </Tabs>
    </div>
  );
}
```

---

## 🪝 Hooks

### useReviewModeration

**Propósito:** Hook para gestionar estado de moderación con bulk actions.

**Ubicación:** `src/hooks/useReviewModeration.ts`

```typescript
// src/hooks/useReviewModeration.ts
import { useState, useCallback } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "react-hot-toast";
import { reviewModerationService } from "@/services/reviewModerationService";

export const useReviewModeration = () => {
  const queryClient = useQueryClient();
  const [selectedIds, setSelectedIds] = useState<string[]>([]);

  // Mutation: Moderar individual
  const moderateMutation = useMutation({
    mutationFn: ({
      id,
      action,
      reason,
    }: {
      id: string;
      action: "approve" | "reject";
      reason?: string;
    }) => reviewModerationService.moderate(id, action, reason),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ["reviews"] });
      toast.success(
        variables.action === "approve"
          ? "Review aprobado exitosamente"
          : "Review rechazado",
      );
    },
    onError: (error: Error) => {
      toast.error(`Error: ${error.message}`);
    },
  });

  // Mutation: Bulk moderation
  const bulkModerateMutation = useMutation({
    mutationFn: ({
      ids,
      action,
    }: {
      ids: string[];
      action: "approve" | "reject";
    }) => reviewModerationService.moderateBulk(ids, action),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ["reviews"] });
      setSelectedIds([]);
      toast.success(
        `${variables.ids.length} reviews ${
          variables.action === "approve" ? "aprobados" : "rechazados"
        } exitosamente`,
      );
    },
    onError: (error: Error) => {
      toast.error(`Error en bulk action: ${error.message}`);
    },
  });

  const approveOne = useCallback(
    (id: string) => {
      moderateMutation.mutate({ id, action: "approve" });
    },
    [moderateMutation],
  );

  const rejectOne = useCallback(
    (id: string, reason?: string) => {
      moderateMutation.mutate({ id, action: "reject", reason });
    },
    [moderateMutation],
  );

  const approveSelected = useCallback(() => {
    if (selectedIds.length === 0) {
      toast.error("No hay reviews seleccionados");
      return;
    }
    bulkModerateMutation.mutate({ ids: selectedIds, action: "approve" });
  }, [selectedIds, bulkModerateMutation]);

  const rejectSelected = useCallback(() => {
    if (selectedIds.length === 0) {
      toast.error("No hay reviews seleccionados");
      return;
    }
    bulkModerateMutation.mutate({ ids: selectedIds, action: "reject" });
  }, [selectedIds, bulkModerateMutation]);

  const toggleSelection = useCallback((id: string) => {
    setSelectedIds((prev) =>
      prev.includes(id) ? prev.filter((i) => i !== id) : [...prev, id],
    );
  }, []);

  const selectAll = useCallback((ids: string[]) => {
    setSelectedIds(ids);
  }, []);

  const clearSelection = useCallback(() => {
    setSelectedIds([]);
  }, []);

  return {
    // State
    selectedIds,

    // Individual actions
    approveOne,
    rejectOne,

    // Bulk actions
    approveSelected,
    rejectSelected,

    // Selection management
    toggleSelection,
    selectAll,
    clearSelection,

    // Loading states
    isModeratingOne: moderateMutation.isPending,
    isModeratingBulk: bulkModerateMutation.isPending,
  };
};
```

---

## 🔌 Servicios

### reviewModerationService

**Propósito:** Cliente API para endpoints de moderación.

**Ubicación:** `src/services/reviewModerationService.ts`

```typescript
// src/services/reviewModerationService.ts
import axios from "axios";
import {
  ReviewModerationFilters,
  ReviewSummary,
  ReviewWithFraudData,
  PaginatedResponse,
  FraudAnalytics,
  ModerationStats,
} from "@/types/reviews";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5030";

class ReviewModerationService {
  private api = axios.create({
    baseURL: `${API_URL}/api/reviews`,
    headers: {
      "Content-Type": "application/json",
    },
  });

  constructor() {
    // Interceptor para agregar JWT token
    this.api.interceptors.request.use((config) => {
      const token = localStorage.getItem("authToken");
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      return config;
    });
  }

  /**
   * Listar reviews con filtros de moderación
   */
  async getReviews(
    filters: ReviewModerationFilters,
  ): Promise<PaginatedResponse<ReviewSummary>> {
    const response = await this.api.get("/", { params: filters });
    return response.data;
  }

  /**
   * Obtener detalle de review con fraud data
   */
  async getReviewById(id: string): Promise<ReviewWithFraudData> {
    const response = await this.api.get(`/${id}`);
    return response.data;
  }

  /**
   * Moderar review individual
   */
  async moderate(
    id: string,
    action: "approve" | "reject",
    reason?: string,
  ): Promise<void> {
    await this.api.patch(`/${id}/moderate`, {
      isApproved: action === "approve",
      rejectionReason: reason,
    });
  }

  /**
   * Moderar múltiples reviews en bulk
   */
  async moderateBulk(
    ids: string[],
    action: "approve" | "reject",
  ): Promise<void> {
    await this.api.post("/moderate/bulk", {
      reviewIds: ids,
      action,
    });
  }

  /**
   * Moderar respuesta del seller
   */
  async moderateResponse(
    reviewId: string,
    responseId: string,
    action: "approve" | "reject",
  ): Promise<void> {
    await this.api.patch(`/${reviewId}/responses/${responseId}/moderate`, {
      isApproved: action === "approve",
    });
  }

  /**
   * Obtener estadísticas generales de moderación
   */
  async getStats(): Promise<ModerationStats> {
    const response = await this.api.get("/admin/stats");
    return response.data;
  }

  /**
   * Obtener analytics de fraude
   */
  async getFraudAnalytics(): Promise<FraudAnalytics> {
    const response = await this.api.get("/admin/fraud-analytics");
    return response.data;
  }

  /**
   * Obtener solo reviews pendientes
   */
  async getPending(
    page = 1,
    pageSize = 20,
  ): Promise<PaginatedResponse<ReviewSummary>> {
    return this.getReviews({
      page,
      pageSize,
      isApproved: null,
      sortBy: "CreatedAt",
      sortOrder: "desc",
    });
  }

  /**
   * Obtener solo reviews flagged
   */
  async getFlagged(
    page = 1,
    pageSize = 20,
  ): Promise<PaginatedResponse<ReviewSummary>> {
    return this.getReviews({
      page,
      pageSize,
      isFlagged: true,
      isApproved: null,
      sortBy: "TrustScore",
      sortOrder: "asc", // Menor TrustScore primero
    });
  }
}

export const reviewModerationService = new ReviewModerationService();
```

---

## 📘 Tipos TypeScript

### Tipos Completos

**Ubicación:** `src/types/reviews.ts`

```typescript
// src/types/reviews.ts

// === Review Entities ===

export interface ReviewWithFraudData {
  id: string;
  buyerId: string;
  buyerName: string;
  sellerId: string;
  sellerName: string;
  vehicleId: string;
  vehicleTitle: string;
  orderId?: string;

  // Content
  rating: number; // 1-5
  title?: string;
  content: string;

  // Moderation
  isApproved: boolean | null; // null = pending
  isFlagged: boolean;
  rejectionReason?: string;
  moderatorId?: string;
  moderatedAt?: string;

  // Verification
  isVerifiedPurchase: boolean;

  // Voting
  helpfulVotes: number;
  totalVotes: number;

  // Fraud Detection Data
  trustScore: number; // 0-100
  ipAddress?: string;
  ipLocation?: {
    city: string;
    country: string;
    countryCode: string;
  };
  userAgentInfo?: {
    device: string;
    browser: string;
    os: string;
  };
  accountAge?: number; // En meses
  previousReviewsStats?: {
    total: number;
    approved: number;
    rejected: number;
  };

  // Timestamps
  createdAt: string;
  updatedAt: string;
}

export interface ReviewSummary {
  id: string;
  buyerId: string;
  buyerName: string;
  sellerId: string;
  sellerName: string;
  rating: number;
  title?: string;
  content: string;
  isApproved: boolean | null;
  isFlagged: boolean;
  trustScore: number;
  isVerifiedPurchase: boolean;
  createdAt: string;
}

// === Seller Responses ===

export interface SellerResponse {
  id: string;
  reviewId: string;
  sellerId: string;
  sellerName: string;
  content: string;
  isApproved: boolean | null;
  moderatorId?: string;
  moderatedAt?: string;
  createdAt: string;
}

// === Moderation Filters ===

export interface ReviewModerationFilters {
  page?: number;
  pageSize?: number;
  isApproved?: boolean | null;
  isFlagged?: boolean;
  minTrustScore?: number;
  maxTrustScore?: number;
  isVerifiedPurchase?: boolean;
  startDate?: string;
  endDate?: string;
  searchTerm?: string;
  sortBy?: "CreatedAt" | "TrustScore" | "HelpfulVotes";
  sortOrder?: "asc" | "desc";
}

// === Paginated Response ===

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  currentPage: number;
  totalPages: number;
  pageSize: number;
}

// === Stats & Analytics ===

export interface ModerationStats {
  totalReviews: number;
  pendingCount: number;
  approvedCount: number;
  rejectedCount: number;
  flaggedCount: number;
  avgTrustScore: number;
  avgModerationTimeHours: number;
  approvalRate: number; // Percentage
}

export interface FraudAnalytics {
  // Overview
  totalReviews: number;
  avgTrustScore: number;
  trustScoreTrend: number; // % change vs mes anterior

  // Categories
  safeCount: number; // TrustScore >= 70
  suspiciousCount: number; // 40-69
  riskyCount: number; // < 40
  flaggedCount: number;
  flaggedPercentage: number;

  // Moderation
  approvedCount: number;
  rejectedCount: number;
  approvalRate: number;
  avgModerationTimeHours: number;

  // Distribution
  trustScoreDistribution: Array<{
    range: string; // "0-20", "21-40", etc.
    count: number;
  }>;

  // Alerts
  alertsCount: number;
  alerts: Array<{
    type: "high_fraud_rate" | "low_trust_avg" | "slow_moderation";
    message: string;
    severity: "low" | "medium" | "high";
  }>;
}

// === Moderation Actions ===

export interface BulkModerationRequest {
  reviewIds: string[];
  action: "approve" | "reject";
  reason?: string;
}

export interface ModerateReviewRequest {
  isApproved: boolean;
  rejectionReason?: string;
}

export interface ModerateResponseRequest {
  isApproved: boolean;
}
```

---

## 🔗 Integración

### 1. Agregar link en Admin Dashboard

**Archivo:** `src/app/admin/dashboard/page.tsx`

```typescript
import Link from 'next/link';
import { Shield, BarChart3 } from 'lucide-react';

// En el grid de tarjetas del dashboard:
<Link href="/admin/reviews/moderation">
  <Card className="hover:shadow-lg transition-shadow cursor-pointer">
    <CardContent className="pt-6">
      <div className="flex items-center gap-4">
        <div className="p-3 bg-blue-100 rounded-lg">
          <Shield className="w-6 h-6 text-blue-600" />
        </div>
        <div>
          <h3 className="font-semibold">Moderación de Reviews</h3>
          <p className="text-sm text-muted-foreground">
            Cola con fraud detection
          </p>
        </div>
      </div>
    </CardContent>
  </Card>
</Link>
```

### 2. Agregar en Admin Sidebar

**Archivo:** `src/components/admin/AdminSidebar.tsx`

```typescript
import { Shield } from "lucide-react";

const navItems = [
  // ... otros items
  {
    title: "Reviews",
    href: "/admin/reviews/moderation",
    icon: Shield,
  },
];
```

### 3. Integración con NotificationService

**Cuando se rechaza un review, enviar email al buyer:**

```typescript
// Backend: ReviewService
await _notificationService.SendEmailAsync(new SendEmailRequest
{
    To = review.BuyerEmail,
    Subject = "Tu review ha sido rechazado - OKLA",
    Body = $@"
        Hola {review.BuyerName},

        Lamentamos informarte que tu review para {vehicle.Title} ha sido rechazado.

        Razón: {rejectionReason}

        Si crees que esto es un error, por favor contáctanos.

        Saludos,
        Equipo OKLA
    "
});
```

---

## 🧪 Testing

### Tests para useReviewModeration Hook

**Ubicación:** `src/hooks/__tests__/useReviewModeration.test.ts`

```typescript
// src/hooks/__tests__/useReviewModeration.test.ts
import { renderHook, act, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useReviewModeration } from '../useReviewModeration';
import { reviewModerationService } from '@/services/reviewModerationService';

jest.mock('@/services/reviewModerationService');

const createWrapper = () => {
  const queryClient = new QueryClient();
  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      {children}
    </QueryClientProvider>
  );
};

describe('useReviewModeration', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  test('should approve one review', async () => {
    const mockModerate = jest.fn().mockResolvedValue(undefined);
    (reviewModerationService.moderate as jest.Mock) = mockModerate;

    const { result } = renderHook(() => useReviewModeration(), {
      wrapper: createWrapper(),
    });

    act(() => {
      result.current.approveOne('review-123');
    });

    await waitFor(() => {
      expect(mockModerate).toHaveBeenCalledWith('review-123', 'approve', undefined);
    });
  });

  test('should reject one review with reason', async () => {
    const mockModerate = jest.fn().mockResolvedValue(undefined);
    (reviewModerationService.moderate as jest.Mock) = mockModerate;

    const { result } = renderHook(() => useReviewModeration(), {
      wrapper: createWrapper(),
    });

    act(() => {
      result.current.rejectOne('review-456', 'Contenido ofensivo');
    });

    await waitFor(() => {
      expect(mockModerate).toHaveBeenCalledWith('review-456', 'reject', 'Contenido ofensivo');
    });
  });

  test('should approve multiple reviews in bulk', async () => {
    const mockBulkModerate = jest.fn().mockResolvedValue(undefined);
    (reviewModerationService.moderateBulk as jest.Mock) = mockBulkModerate;

    const { result } = renderHook(() => useReviewModeration(), {
      wrapper: createWrapper(),
    });

    // Select 3 reviews
    act(() => {
      result.current.toggleSelection('review-1');
      result.current.toggleSelection('review-2');
      result.current.toggleSelection('review-3');
    });

    // Bulk approve
    act(() => {
      result.current.approveSelected();
    });

    await waitFor(() => {
      expect(mockBulkModerate).toHaveBeenCalledWith(
        ['review-1', 'review-2', 'review-3'],
        'approve'
      );
    });

    // Selection should be cleared
    expect(result.current.selectedIds).toHaveLength(0);
  });

  test('should toggle selection correctly', () => {
    const { result } = renderHook(() => useReviewModeration(), {
      wrapper: createWrapper(),
    });

    // Add
    act(() => {
      result.current.toggleSelection('review-1');
    });
    expect(result.current.selectedIds).toContain('review-1');

    // Remove
    act(() => {
      result.current.toggleSelection('review-1');
    });
    expect(result.current.selectedIds).not.toContain('review-1');
  });

  test('should select all', () => {
    const { result } = renderHook(() => useReviewModeration(), {
      wrapper: createWrapper(),
    });

    act(() => {
      result.current.selectAll(['review-1', 'review-2', 'review-3']);
    });

    expect(result.current.selectedIds).toHaveLength(3);
  });

  test('should clear selection', () => {
    const { result } = renderHook(() => useReviewModeration(), {
      wrapper: createWrapper(),
    });

    act(() => {
      result.current.selectAll(['review-1', 'review-2']);
    });

    act(() => {
      result.current.clearSelection();
    });

    expect(result.current.selectedIds).toHaveLength(0);
  });
});
```

---

## 📊 Mejoras vs Genérico

### Comparación: 14-admin-moderation.md vs 37-admin-review-moderation-completo.md

| Feature                     | Genérico (14)    | Reviews (37)                           | Mejora |
| --------------------------- | ---------------- | -------------------------------------- | ------ |
| **Fraud Detection**         | ❌ No            | ✅ TrustScore, IP, UserAgent           | +100%  |
| **Bulk Actions**            | ❌ No            | ✅ Approve/Reject múltiples            | +100%  |
| **Filtros Avanzados**       | 🟡 Básico        | ✅ TrustScore range, verified purchase | +300%  |
| **Visualización de Fraude** | ❌ No            | ✅ Gauge, badges, indicadores          | +100%  |
| **Analytics**               | 🟡 Stats básicas | ✅ Dashboard completo con gráficos     | +200%  |
| **Seller Responses**        | ❌ No            | ✅ Moderar respuestas                  | +100%  |
| **TrustScore Insights**     | ❌ No            | ✅ Desglose completo de factores       | +100%  |
| **Audit Trail**             | 🟡 Parcial       | ✅ Completo con moderador y razón      | +50%   |

**Key Differentiators:**

1. **Fraud Detection Visual:** El sistema muestra visualmente todos los factores de fraude (IP, UserAgent, edad de cuenta, compra verificada) en un componente dedicado.

2. **Bulk Moderation:** Permite a moderadores aprobar/rechazar múltiples reviews en una acción, mejorando velocidad de moderación en 10x.

3. **Analytics de Fraude:** Dashboard con métricas específicas para monitorear efectividad del sistema anti-fraude.

4. **TrustScore Transparency:** Muestra cómo se calcula el TrustScore para cada review, permitiendo decisiones informadas.

5. **Priorización Inteligente:** Reviews flagged y con bajo TrustScore aparecen primero en la cola.

---

## ✅ Checklist de Implementación

### Backend (ReviewService)

- [ ] Endpoint `GET /api/reviews` con filtros de moderación funcionando
- [ ] Endpoint `PATCH /api/reviews/{id}/moderate` para aprobar/rechazar
- [ ] Endpoint `POST /api/reviews/moderate/bulk` para bulk actions
- [ ] Endpoint `GET /api/reviews/admin/fraud-analytics` para métricas
- [ ] Cálculo de TrustScore implementado (IP, UserAgent, account age, etc.)
- [ ] IsFlagged automático cuando TrustScore < 40
- [ ] Integración con NotificationService para emails de rechazo
- [ ] Tests unitarios para fraud detection (80%+ coverage)

### Frontend

**Componentes:**

- [ ] FraudIndicators con versión completa y compacta
- [ ] ReviewModerationQueue con tabla y bulk actions
- [ ] ReviewModerationFilters con todos los filtros avanzados
- [ ] SellerResponseModeration para aprobar respuestas
- [ ] FraudAnalyticsDashboard con gráficos Recharts

**Páginas:**

- [ ] /admin/reviews/moderation con tabs (Queue + Analytics)

**Hooks:**

- [ ] useReviewModeration con bulk actions y selection management

**Servicios:**

- [ ] reviewModerationService con 9 métodos

**Tipos:**

- [ ] Todos los tipos TypeScript definidos
- [ ] Interfaces para filtros, stats, analytics

**Integración:**

- [ ] Link en Admin Dashboard
- [ ] Item en Admin Sidebar
- [ ] Integración con NotificationService (emails)

**Testing:**

- [ ] Tests para useReviewModeration hook
- [ ] Tests para reviewModerationService
- [ ] Tests E2E para flujo completo de moderación

---

## 🚀 Próximos Pasos (Post-Implementación)

1. **Machine Learning para Fraud Detection:**
   - Modelo de clasificación (RandomForest o XGBoost)
   - Features: TrustScore, IP patterns, content sentiment
   - Auto-aprobar reviews con >90% confianza

2. **Sentiment Analysis:**
   - Integrar NLP para detectar sentimiento (positivo/negativo/neutro)
   - Alertas para reviews con sentimiento extremo

3. **IP Reputation API:**
   - Integrar con MaxMind o IPQualityScore
   - Detectar VPNs, proxies, bots

4. **A/B Testing de Moderation Threshold:**
   - Experimentar con diferentes thresholds de TrustScore
   - Optimizar para balance entre calidad y velocidad

5. **Auto-Moderation:**
   - Reviews con TrustScore > 80 → Auto-aprobar
   - Reviews con TrustScore < 20 → Auto-rechazar (con revisión manual)

---

## 📚 Referencias

- **Backend:** `docs/process-matrix/07-REVIEWS-REPUTACION/01-review-service.md`
- **Audit:** `docs/AUDITORIA-REVIEWS-REPUTACION.md`
- **Generic Moderation:** `docs/frontend-rebuild/04-PAGINAS/14-admin-moderation.md`
- **Reviews UI:** `docs/frontend-rebuild/04-PAGINAS/03-COMPRADOR/06-reviews-reputacion.md`
- **Badges:** `docs/frontend-rebuild/04-PAGINAS/35-badges-display-completo.md`
- **Review Requests:** `docs/frontend-rebuild/04-PAGINAS/98-review-request-response-completo.md`

---

**✅ Documentación Completa - Listo para Implementación**

_Este documento completa el 40% faltante de moderación avanzada específica para reviews con fraud detection, bulk actions, y analytics dedicado._

**Tiempo estimado de implementación:** 12-15 horas  
**Prioridad:** Alta (crítico para calidad y confianza)  
**Dependencias:** ReviewService backend, NotificationService, MediaService (para imágenes futuras)

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/admin-review-moderation.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsAdmin } from "../helpers/auth";

test.describe("Admin Review Moderation", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("debe mostrar cola de reviews pendientes", async ({ page }) => {
    await page.goto("/admin/reviews");

    await expect(page.getByTestId("reviews-queue")).toBeVisible();
  });

  test("debe aprobar review", async ({ page }) => {
    await page.goto("/admin/reviews");

    await page
      .getByTestId("review-card")
      .first()
      .getByRole("button", { name: /aprobar/i })
      .click();
    await expect(page.getByText(/review aprobado/i)).toBeVisible();
  });

  test("debe rechazar review con motivo", async ({ page }) => {
    await page.goto("/admin/reviews");

    await page
      .getByTestId("review-card")
      .first()
      .getByRole("button", { name: /rechazar/i })
      .click();
    await page.getByRole("combobox", { name: /motivo/i }).click();
    await page.getByRole("option", { name: /contenido ofensivo/i }).click();
    await page.getByRole("button", { name: /confirmar/i }).click();

    await expect(page.getByText(/review rechazado/i)).toBeVisible();
  });

  test("debe ver analytics de reviews", async ({ page }) => {
    await page.goto("/admin/reviews/analytics");

    await expect(page.getByTestId("reviews-analytics")).toBeVisible();
  });

  test("debe detectar fraude en reviews", async ({ page }) => {
    await page.goto("/admin/reviews");

    await page.getByRole("tab", { name: /flagged/i }).click();
    await expect(page.getByTestId("fraud-alerts")).toBeVisible();
  });
});
```

---

_Última actualización: Enero 9, 2026_  
_Autor: Gregory Moreno_
