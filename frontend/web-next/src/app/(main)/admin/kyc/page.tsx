/**
 * Admin KYC Verification Panel
 *
 * Allows OKLA administrators to:
 *  - View pending identity verification requests
 *  - Inspect submitted documents and biometric scores
 *  - Approve or reject KYC profiles
 *  - Monitor verification statistics
 *
 * Access: Admin / platform_employee only
 * Backend: KYCService via server actions (identity never exposed to browser)
 */

'use client';

import * as React from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import {
  Shield,
  CheckCircle,
  XCircle,
  Clock,
  AlertTriangle,
  Eye,
  Users,
  ChevronLeft,
  ChevronRight,
  Search,
  Filter,
  RefreshCw,
  FileText,
  User,
  Calendar,
  Hash,
  Building2,
  AlertCircle,
} from 'lucide-react';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog';
import { Skeleton } from '@/components/ui/skeleton';

import { useAuth } from '@/hooks/use-auth';
import {
  kycService,
  KYCStatus,
  RiskLevel,
  type KYCProfileSummary,
  type KYCStatistics,
} from '@/services/kyc';

// =============================================================================
// TYPES
// =============================================================================

type FilterStatus = 'pending' | 'all' | 'approved' | 'rejected' | 'inprogress';

interface RejectDialogState {
  open: boolean;
  profileId: string;
  profileName: string;
}

interface DetailDialogState {
  open: boolean;
  profile: KYCProfileSummary | null;
}

// =============================================================================
// HELPERS
// =============================================================================

function statusToKYCStatus(filter: FilterStatus): KYCStatus | null {
  switch (filter) {
    case 'pending':
      return KYCStatus.UnderReview;
    case 'approved':
      return KYCStatus.Approved;
    case 'rejected':
      return KYCStatus.Rejected;
    case 'inprogress':
      return KYCStatus.InProgress;
    default:
      return null;
  }
}

function getRiskBadge(riskLevel: number) {
  switch (riskLevel) {
    case RiskLevel.Low:
      return (
        <Badge className="bg-green-100 text-green-800 hover:bg-green-100">
          Riesgo bajo
        </Badge>
      );
    case RiskLevel.Medium:
      return (
        <Badge className="bg-yellow-100 text-yellow-800 hover:bg-yellow-100">
          Riesgo medio
        </Badge>
      );
    case RiskLevel.High:
      return (
        <Badge className="bg-orange-100 text-orange-800 hover:bg-orange-100">
          Riesgo alto
        </Badge>
      );
    case RiskLevel.VeryHigh:
    case RiskLevel.Prohibited:
      return (
        <Badge className="bg-red-100 text-red-800 hover:bg-red-100">
          Riesgo muy alto
        </Badge>
      );
    default:
      return <Badge variant="secondary">Desconocido</Badge>;
  }
}

function getStatusBadge(status: number) {
  switch (status) {
    case KYCStatus.UnderReview:
      return (
        <Badge className="bg-purple-100 text-purple-800 hover:bg-purple-100">
          <Clock className="mr-1 h-3 w-3" />
          En revisión
        </Badge>
      );
    case KYCStatus.Approved:
      return (
        <Badge className="bg-green-100 text-green-800 hover:bg-green-100">
          <CheckCircle className="mr-1 h-3 w-3" />
          Aprobado
        </Badge>
      );
    case KYCStatus.Rejected:
      return (
        <Badge className="bg-red-100 text-red-800 hover:bg-red-100">
          <XCircle className="mr-1 h-3 w-3" />
          Rechazado
        </Badge>
      );
    case KYCStatus.InProgress:
      return (
        <Badge className="bg-blue-100 text-blue-800 hover:bg-blue-100">
          En progreso
        </Badge>
      );
    case KYCStatus.DocumentsRequired:
      return <Badge variant="outline">Docs. requeridos</Badge>;
    case KYCStatus.Expired:
      return <Badge variant="danger">Expirado</Badge>;
    default:
      return <Badge variant="secondary">Pendiente</Badge>;
  }
}

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString('es-DO', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

// =============================================================================
// STAT CARD
// =============================================================================

function StatCard({
  title,
  value,
  icon: Icon,
  color,
  subtitle,
}: {
  title: string;
  value: number | string;
  icon: React.ElementType;
  color: string;
  subtitle?: string;
}) {
  return (
    <Card>
      <CardContent className="p-6">
        <div className="flex items-start justify-between">
          <div>
            <p className="text-sm font-medium text-muted-foreground">{title}</p>
            <p className={`mt-1 text-2xl font-bold ${color}`}>{value}</p>
            {subtitle && <p className="mt-1 text-xs text-muted-foreground">{subtitle}</p>}
          </div>
          <div className={`rounded-lg p-2 ${color.replace('text-', 'bg-').replace('-600', '-100').replace('-700', '-100')}`}>
            <Icon className={`h-5 w-5 ${color}`} />
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

// =============================================================================
// PROFILE ROW
// =============================================================================

function ProfileRow({
  profile,
  onApprove,
  onReject,
  onViewDetail,
  isApproving,
}: {
  profile: KYCProfileSummary;
  onApprove: (id: string, name: string) => void;
  onReject: (id: string, name: string) => void;
  onViewDetail: (profile: KYCProfileSummary) => void;
  isApproving: boolean;
}) {
  const canAction = profile.status === KYCStatus.UnderReview;

  return (
    <tr className="border-b transition-colors hover:bg-muted/50">
      {/* Name + entity type */}
      <td className="px-4 py-3">
        <div className="flex items-center gap-3">
          <div className="flex h-9 w-9 flex-shrink-0 items-center justify-center rounded-full bg-gray-100">
            {profile.entityType === 2 ? (
              <Building2 className="h-4 w-4 text-gray-600" />
            ) : (
              <User className="h-4 w-4 text-gray-600" />
            )}
          </div>
          <div>
            <p className="font-medium text-gray-900">{profile.fullName}</p>
            <p className="text-xs text-muted-foreground">
              {profile.entityType === 2 ? 'Dealer' : 'Vendedor individual'}
              {profile.isPEP && (
                <span className="ml-2 rounded bg-red-100 px-1.5 py-0.5 text-xs font-medium text-red-700">
                  PEP
                </span>
              )}
            </p>
          </div>
        </div>
      </td>

      {/* Document number */}
      <td className="px-4 py-3">
        <div className="flex items-center gap-1 text-sm text-gray-700">
          <Hash className="h-3 w-3 text-muted-foreground" />
          {profile.documentNumber || '—'}
        </div>
      </td>

      {/* Docs submitted */}
      <td className="px-4 py-3 text-center">
        <span
          className={`inline-flex items-center gap-1 text-sm font-medium ${
            profile.pendingDocuments > 0 ? 'text-amber-600' : 'text-green-600'
          }`}
        >
          <FileText className="h-3 w-3" />
          {profile.documentsCount}
          {profile.pendingDocuments > 0 && (
            <span className="text-xs text-amber-500">({profile.pendingDocuments} pend.)</span>
          )}
        </span>
      </td>

      {/* Status */}
      <td className="px-4 py-3">{getStatusBadge(profile.status)}</td>

      {/* Risk */}
      <td className="px-4 py-3">{getRiskBadge(profile.riskLevel)}</td>

      {/* Date */}
      <td className="px-4 py-3 text-sm text-muted-foreground">
        <div className="flex items-center gap-1">
          <Calendar className="h-3 w-3" />
          {formatDate(profile.createdAt)}
        </div>
      </td>

      {/* Actions */}
      <td className="px-4 py-3">
        <div className="flex items-center gap-2">
          <Button
            variant="ghost"
            size="sm"
            className="gap-1 text-blue-600 hover:bg-blue-50 hover:text-blue-700"
            onClick={() => onViewDetail(profile)}
          >
            <Eye className="h-3 w-3" />
            Ver
          </Button>

          {canAction && (
            <>
              <Button
                variant="ghost"
                size="sm"
                className="gap-1 text-green-600 hover:bg-green-50 hover:text-green-700"
                onClick={() => onApprove(profile.id, profile.fullName)}
                disabled={isApproving}
              >
                <CheckCircle className="h-3 w-3" />
                Aprobar
              </Button>
              <Button
                variant="ghost"
                size="sm"
                className="gap-1 text-red-600 hover:bg-red-50 hover:text-red-700"
                onClick={() => onReject(profile.id, profile.fullName)}
                disabled={isApproving}
              >
                <XCircle className="h-3 w-3" />
                Rechazar
              </Button>
            </>
          )}
        </div>
      </td>
    </tr>
  );
}

// =============================================================================
// MAIN PAGE
// =============================================================================

export default function AdminKYCPage() {
  const { user } = useAuth();
  const queryClient = useQueryClient();

  // Filter / pagination state
  const [statusFilter, setStatusFilter] = React.useState<FilterStatus>('pending');
  const [search, setSearch] = React.useState('');
  const [debouncedSearch, setDebouncedSearch] = React.useState('');
  const [page, setPage] = React.useState(1);
  const PAGE_SIZE = 20;

  // Debounce search input
  React.useEffect(() => {
    const t = setTimeout(() => setDebouncedSearch(search), 400);
    return () => clearTimeout(t);
  }, [search]);

  // Dialog state
  const [rejectDialog, setRejectDialog] = React.useState<RejectDialogState>({
    open: false,
    profileId: '',
    profileName: '',
  });
  const [rejectionReason, setRejectionReason] = React.useState('');
  const [rejectionNotes, setRejectionNotes] = React.useState('');
  const [detailDialog, setDetailDialog] = React.useState<DetailDialogState>({
    open: false,
    profile: null,
  });

  // ── Queries ──────────────────────────────────────────────────────────────

  const statsQuery = useQuery<KYCStatistics>({
    queryKey: ['admin', 'kyc', 'statistics'],
    queryFn: () => kycService.getStatistics(),
    staleTime: 60_000,
    refetchInterval: 90_000,
  });

  const profilesQuery = useQuery({
    queryKey: ['admin', 'kyc', 'profiles', statusFilter, debouncedSearch, page],
    queryFn: () =>
      kycService.getAdminProfiles({
        page,
        pageSize: PAGE_SIZE,
        status: statusToKYCStatus(statusFilter),
        search: debouncedSearch || undefined,
      }),
    staleTime: 30_000,
  });

  const profiles: KYCProfileSummary[] = profilesQuery.data?.items ?? [];
  const totalPages = profilesQuery.data
    ? Math.ceil((profilesQuery.data.totalCount ?? 0) / PAGE_SIZE)
    : 1;

  // ── Mutations ─────────────────────────────────────────────────────────────

  const approveMutation = useMutation({
    mutationFn: ({ profileId }: { profileId: string; profileName: string }) =>
      kycService.approveProfile(profileId, user?.id ?? '', user?.fullName ?? 'Admin'),
    onSuccess: (_, vars) => {
      toast.success(`✅ ${vars.profileName} aprobado correctamente`);
      queryClient.invalidateQueries({ queryKey: ['admin', 'kyc'] });
    },
    onError: (err: Error, vars) => {
      toast.error(`Error al aprobar ${vars.profileName}: ${err.message}`);
    },
  });

  const rejectMutation = useMutation({
    mutationFn: ({
      profileId,
      reason,
      notes,
    }: {
      profileId: string;
      profileName: string;
      reason: string;
      notes?: string;
    }) =>
      kycService.rejectProfile(
        profileId,
        user?.id ?? '',
        user?.fullName ?? 'Admin',
        reason,
        notes
      ),
    onSuccess: (_, vars) => {
      toast.success(`Solicitud de ${vars.profileName} rechazada`);
      setRejectDialog({ open: false, profileId: '', profileName: '' });
      setRejectionReason('');
      setRejectionNotes('');
      queryClient.invalidateQueries({ queryKey: ['admin', 'kyc'] });
    },
    onError: (err: Error) => {
      toast.error(`Error al rechazar: ${err.message}`);
    },
  });

  // ── Handlers ──────────────────────────────────────────────────────────────

  function handleApprove(profileId: string, profileName: string) {
    if (!confirm(`¿Aprobar verificación de ${profileName}?`)) return;
    approveMutation.mutate({ profileId, profileName });
  }

  function handleOpenReject(profileId: string, profileName: string) {
    setRejectDialog({ open: true, profileId, profileName });
  }

  function handleConfirmReject() {
    if (!rejectionReason.trim()) {
      toast.error('Debes indicar el motivo del rechazo');
      return;
    }
    rejectMutation.mutate({
      profileId: rejectDialog.profileId,
      profileName: rejectDialog.profileName,
      reason: rejectionReason.trim(),
      notes: rejectionNotes.trim() || undefined,
    });
  }

  function handleViewDetail(profile: KYCProfileSummary) {
    setDetailDialog({ open: true, profile });
  }

  function handleRefresh() {
    queryClient.invalidateQueries({ queryKey: ['admin', 'kyc'] });
    toast.info('Lista actualizada');
  }

  // Guard — admin only
  if (user && user.accountType !== 'admin' && user.accountType !== 'platform_employee') {
    return (
      <div className="flex min-h-[400px] items-center justify-center">
        <Card className="mx-auto max-w-md">
          <CardContent className="p-8 text-center">
            <AlertCircle className="mx-auto mb-4 h-12 w-12 text-red-500" />
            <h2 className="mb-2 text-xl font-bold">Acceso restringido</h2>
            <p className="text-muted-foreground">Solo los administradores pueden acceder a este panel.</p>
          </CardContent>
        </Card>
      </div>
    );
  }

  // ==========================================================================
  // RENDER
  // ==========================================================================

  const stats = statsQuery.data;

  return (
    <div className="space-y-6">
      {/* Page header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Panel de Verificación KYC</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Revisa, aprueba y rechaza solicitudes de verificación de identidad
          </p>
        </div>
        <Button variant="outline" size="sm" className="gap-2" onClick={handleRefresh}>
          <RefreshCw className="h-4 w-4" />
          Actualizar
        </Button>
      </div>

      {/* Stats row */}
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
        {statsQuery.isLoading ? (
          Array.from({ length: 4 }).map((_, i) => (
            <Card key={i}>
              <CardContent className="p-6">
                <Skeleton className="mb-2 h-4 w-28" />
                <Skeleton className="h-7 w-16" />
              </CardContent>
            </Card>
          ))
        ) : (
          <>
            <StatCard
              title="Total solicitudes"
              value={stats?.totalProfiles ?? 0}
              icon={Users}
              color="text-blue-600"
            />
            <StatCard
              title="En revisión"
              value={stats?.inReviewProfiles ?? stats?.pendingProfiles ?? 0}
              icon={Clock}
              color="text-purple-600"
              subtitle="Esperando acción"
            />
            <StatCard
              title="Aprobados"
              value={stats?.approvedProfiles ?? 0}
              icon={CheckCircle}
              color="text-green-600"
              subtitle={
                stats?.approvalRate != null
                  ? `${Math.round(stats.approvalRate * 100)}% tasa de aprobación`
                  : undefined
              }
            />
            <StatCard
              title="Rechazados"
              value={stats?.rejectedProfiles ?? 0}
              icon={XCircle}
              color="text-red-600"
            />
          </>
        )}
      </div>

      {/* Filters + Table */}
      <Card>
        <CardHeader className="pb-3">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <CardTitle className="text-base font-semibold">Solicitudes de Verificación</CardTitle>
            <div className="flex flex-wrap items-center gap-2">
              {/* Search */}
              <div className="relative">
                <Search className="absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-muted-foreground" />
                <Input
                  className="h-8 w-52 pl-8 text-sm"
                  placeholder="Buscar por nombre…"
                  value={search}
                  onChange={e => {
                    setSearch(e.target.value);
                    setPage(1);
                  }}
                />
              </div>

              {/* Status filter */}
              <Select
                value={statusFilter}
                onValueChange={val => {
                  setStatusFilter(val as FilterStatus);
                  setPage(1);
                }}
              >
                <SelectTrigger className="h-8 w-44 text-sm">
                  <Filter className="mr-1 h-3 w-3" />
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="pending">En revisión</SelectItem>
                  <SelectItem value="inprogress">En progreso</SelectItem>
                  <SelectItem value="approved">Aprobados</SelectItem>
                  <SelectItem value="rejected">Rechazados</SelectItem>
                  <SelectItem value="all">Todos</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardHeader>

        <CardContent className="p-0">
          {profilesQuery.isLoading ? (
            <div className="space-y-3 p-4">
              {Array.from({ length: 6 }).map((_, i) => (
                <Skeleton key={i} className="h-14 w-full rounded-md" />
              ))}
            </div>
          ) : profilesQuery.isError ? (
            <div className="flex flex-col items-center justify-center gap-3 py-16 text-center">
              <AlertTriangle className="h-10 w-10 text-amber-500" />
              <p className="text-sm text-muted-foreground">
                Error al cargar las solicitudes. Intenta actualizar la página.
              </p>
              <Button variant="outline" size="sm" onClick={handleRefresh}>
                Reintentar
              </Button>
            </div>
          ) : profiles.length === 0 ? (
            <div className="flex flex-col items-center justify-center gap-3 py-16 text-center">
              <Shield className="h-10 w-10 text-muted-foreground/40" />
              <p className="text-sm text-muted-foreground">
                {statusFilter === 'pending'
                  ? 'No hay solicitudes pendientes de revisión'
                  : 'No hay solicitudes en este estado'}
              </p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-muted/40 text-xs uppercase tracking-wider text-muted-foreground">
                    <th className="px-4 py-3 text-left">Solicitante</th>
                    <th className="px-4 py-3 text-left">Documento</th>
                    <th className="px-4 py-3 text-center">Docs</th>
                    <th className="px-4 py-3 text-left">Estado</th>
                    <th className="px-4 py-3 text-left">Riesgo</th>
                    <th className="px-4 py-3 text-left">Fecha</th>
                    <th className="px-4 py-3 text-left">Acciones</th>
                  </tr>
                </thead>
                <tbody>
                  {profiles.map(profile => (
                    <ProfileRow
                      key={profile.id}
                      profile={profile}
                      onApprove={handleApprove}
                      onReject={handleOpenReject}
                      onViewDetail={handleViewDetail}
                      isApproving={approveMutation.isPending}
                    />
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between border-t px-4 py-3">
              <p className="text-xs text-muted-foreground">
                Página {page} de {totalPages}
              </p>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  disabled={page <= 1}
                  onClick={() => setPage(p => p - 1)}
                >
                  <ChevronLeft className="h-4 w-4" />
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={page >= totalPages}
                  onClick={() => setPage(p => p + 1)}
                >
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* ── Reject Dialog ─────────────────────────────────────────────────── */}
      <Dialog
        open={rejectDialog.open}
        onOpenChange={open => {
          if (!open) {
            setRejectDialog({ open: false, profileId: '', profileName: '' });
            setRejectionReason('');
            setRejectionNotes('');
          }
        }}
      >
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 text-red-700">
              <XCircle className="h-5 w-5" />
              Rechazar verificación
            </DialogTitle>
          </DialogHeader>

          <div className="space-y-4 py-2">
            <p className="text-sm text-muted-foreground">
              Estás rechazando la solicitud de{' '}
              <span className="font-semibold text-foreground">{rejectDialog.profileName}</span>. El
              usuario recibirá una notificación con el motivo indicado.
            </p>

            <div className="space-y-1.5">
              <Label htmlFor="reason">
                Motivo del rechazo <span className="text-red-500">*</span>
              </Label>
              <Textarea
                id="reason"
                rows={3}
                placeholder="Ej: Documento ilegible, selfie no coincide con cédula, documentos expirados…"
                value={rejectionReason}
                onChange={e => setRejectionReason(e.target.value)}
              />
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="notes">Notas internas (opcional)</Label>
              <Textarea
                id="notes"
                rows={2}
                placeholder="Notas privadas para el equipo (no visibles al usuario)"
                value={rejectionNotes}
                onChange={e => setRejectionNotes(e.target.value)}
              />
            </div>
          </div>

          <DialogFooter className="gap-2">
            <Button
              variant="outline"
              onClick={() => setRejectDialog({ open: false, profileId: '', profileName: '' })}
            >
              Cancelar
            </Button>
            <Button
              variant="destructive"
              onClick={handleConfirmReject}
              disabled={!rejectionReason.trim() || rejectMutation.isPending}
            >
              {rejectMutation.isPending ? 'Rechazando…' : 'Confirmar rechazo'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* ── Detail Dialog ─────────────────────────────────────────────────── */}
      <Dialog
        open={detailDialog.open}
        onOpenChange={open => {
          if (!open) setDetailDialog({ open: false, profile: null });
        }}
      >
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <Shield className="h-5 w-5 text-blue-600" />
              Detalle de Solicitud KYC
            </DialogTitle>
          </DialogHeader>

          {detailDialog.profile && (
            <div className="space-y-4 py-2 text-sm">
              {/* Identity */}
              <div className="rounded-lg border p-4 space-y-2">
                <h3 className="font-semibold text-foreground">Información del solicitante</h3>
                <div className="grid grid-cols-2 gap-2 text-muted-foreground">
                  <div>
                    <span className="font-medium text-foreground">Nombre:</span>{' '}
                    {detailDialog.profile.fullName}
                  </div>
                  <div>
                    <span className="font-medium text-foreground">Documento:</span>{' '}
                    {detailDialog.profile.documentNumber || '—'}
                  </div>
                  <div>
                    <span className="font-medium text-foreground">Tipo:</span>{' '}
                    {detailDialog.profile.entityType === 2 ? 'Dealer' : 'Vendedor individual'}
                  </div>
                  <div>
                    <span className="font-medium text-foreground">PEP:</span>{' '}
                    {detailDialog.profile.isPEP ? (
                      <span className="text-red-600 font-semibold">Sí — Persona políticamente expuesta</span>
                    ) : (
                      'No'
                    )}
                  </div>
                </div>
              </div>

              {/* Status + Risk */}
              <div className="rounded-lg border p-4 space-y-2">
                <h3 className="font-semibold text-foreground">Estado y riesgo</h3>
                <div className="flex flex-wrap gap-3">
                  {getStatusBadge(detailDialog.profile.status)}
                  {getRiskBadge(detailDialog.profile.riskLevel)}
                  {detailDialog.profile.riskLevelName && (
                    <span className="text-xs text-muted-foreground self-center">
                      ({detailDialog.profile.riskLevelName})
                    </span>
                  )}
                </div>
              </div>

              {/* Documents */}
              <div className="rounded-lg border p-4 space-y-2">
                <h3 className="font-semibold text-foreground">Documentos</h3>
                <div className="flex items-center gap-4">
                  <div className="text-center">
                    <p className="text-2xl font-bold text-foreground">
                      {detailDialog.profile.documentsCount}
                    </p>
                    <p className="text-xs text-muted-foreground">Enviados</p>
                  </div>
                  <div className="text-center">
                    <p
                      className={`text-2xl font-bold ${detailDialog.profile.pendingDocuments > 0 ? 'text-amber-600' : 'text-green-600'}`}
                    >
                      {detailDialog.profile.pendingDocuments}
                    </p>
                    <p className="text-xs text-muted-foreground">Pendientes</p>
                  </div>
                </div>
              </div>

              {/* Dates */}
              <div className="rounded-lg border p-4 space-y-2">
                <h3 className="font-semibold text-foreground">Fechas</h3>
                <div className="space-y-1 text-muted-foreground">
                  <p>
                    <span className="font-medium text-foreground">Creado:</span>{' '}
                    {formatDate(detailDialog.profile.createdAt)}
                  </p>
                  {detailDialog.profile.expiresAt && (
                    <p>
                      <span className="font-medium text-foreground">Vence:</span>{' '}
                      {formatDate(detailDialog.profile.expiresAt)}
                    </p>
                  )}
                </div>
              </div>

              {/* Quick actions if pending */}
              {detailDialog.profile.status === KYCStatus.UnderReview && (
                <div className="flex gap-3 pt-1">
                  <Button
                    className="flex-1 gap-2 bg-green-600 hover:bg-green-700"
                    onClick={() => {
                      setDetailDialog({ open: false, profile: null });
                      handleApprove(detailDialog.profile!.id, detailDialog.profile!.fullName);
                    }}
                    disabled={approveMutation.isPending}
                  >
                    <CheckCircle className="h-4 w-4" />
                    Aprobar
                  </Button>
                  <Button
                    variant="destructive"
                    className="flex-1 gap-2"
                    onClick={() => {
                      setDetailDialog({ open: false, profile: null });
                      handleOpenReject(detailDialog.profile!.id, detailDialog.profile!.fullName);
                    }}
                  >
                    <XCircle className="h-4 w-4" />
                    Rechazar
                  </Button>
                </div>
              )}
            </div>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
