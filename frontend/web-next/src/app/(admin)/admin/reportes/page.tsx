/**
 * Admin Reports Page
 *
 * Report management page for administrators
 * Connected to real APIs - February 2026
 */

'use client';

import { useState } from 'react';
import Link from 'next/link';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import {
  Flag,
  Search,
  ChevronLeft,
  ChevronRight,
  Eye,
  CheckCircle,
  XCircle,
  Clock,
  Loader2,
  AlertTriangle,
  Car,
  User,
  Calendar,
} from 'lucide-react';
import { toast } from 'sonner';
import {
  useAdminReports,
  useReportStats,
  useUpdateReportStatus,
  type ReportFilters,
} from '@/hooks/use-admin';
import { Textarea } from '@/components/ui/textarea';

// =============================================================================
// SKELETON
// =============================================================================

function ReportsSkeleton() {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-10 w-32" />
      </div>
      <div className="grid gap-4 md:grid-cols-4">
        {[1, 2, 3, 4].map(i => (
          <Skeleton key={i} className="h-24" />
        ))}
      </div>
      <Skeleton className="h-96" />
    </div>
  );
}

// =============================================================================
// HELPERS
// =============================================================================

const getStatusBadge = (status: string) => {
  switch (status) {
    case 'resolved':
      return <Badge className="bg-primary/10 text-primary">Resuelto</Badge>;
    case 'pending':
      return <Badge className="bg-amber-100 text-amber-700">Pendiente</Badge>;
    case 'investigating':
      return <Badge className="bg-blue-100 text-blue-700">En revisión</Badge>;
    case 'dismissed':
      return <Badge className="bg-muted text-foreground">Descartado</Badge>;
    default:
      return <Badge variant="secondary">{status}</Badge>;
  }
};

const getTypeBadge = (type: string) => {
  switch (type) {
    case 'fraud':
      return <Badge className="bg-red-100 text-red-700">Fraude</Badge>;
    case 'inappropriate':
      return <Badge className="bg-orange-100 text-orange-700">Inapropiado</Badge>;
    case 'wrong_info':
      return <Badge className="bg-yellow-100 text-yellow-700">Info incorrecta</Badge>;
    case 'duplicate':
      return <Badge className="bg-purple-100 text-purple-700">Duplicado</Badge>;
    case 'spam':
      return <Badge className="bg-pink-100 text-pink-700">Spam</Badge>;
    case 'other':
      return <Badge className="bg-muted text-foreground">Otro</Badge>;
    default:
      return <Badge variant="outline">{type}</Badge>;
  }
};

const getPriorityIndicator = (priority: string) => {
  switch (priority) {
    case 'high':
      return <span className="flex h-2 w-2 rounded-full bg-red-500" />;
    case 'medium':
      return <span className="flex h-2 w-2 rounded-full bg-amber-500" />;
    case 'low':
      return <span className="flex h-2 w-2 rounded-full bg-green-500" />;
    default:
      return <span className="bg-muted-foreground/30 flex h-2 w-2 rounded-full" />;
  }
};

// =============================================================================
// MAIN COMPONENT
// =============================================================================

export default function AdminReportesPage() {
  const [filters, setFilters] = useState<ReportFilters>({
    page: 1,
    pageSize: 10,
  });
  const [searchTerm, setSearchTerm] = useState('');
  const [resolution, setResolution] = useState('');
  const [resolvingId, setResolvingId] = useState<string | null>(null);

  const { data: reportsData, isLoading } = useAdminReports(filters);
  const { data: stats } = useReportStats();
  const updateStatusMutation = useUpdateReportStatus();

  const handleSearch = () => {
    setFilters(prev => ({ ...prev, search: searchTerm, page: 1 }));
  };

  const handleFilterChange = (key: string, value: string) => {
    setFilters(prev => ({
      ...prev,
      [key]: value === 'all' ? undefined : value,
      page: 1,
    }));
  };

  const handleUpdateStatus = async (
    id: string,
    status: 'investigating' | 'resolved' | 'dismissed',
    resolutionText?: string
  ) => {
    try {
      await updateStatusMutation.mutateAsync({ id, status, resolution: resolutionText });
      toast.success(
        `Reporte ${status === 'resolved' ? 'resuelto' : status === 'dismissed' ? 'descartado' : 'actualizado'}`
      );
      setResolvingId(null);
      setResolution('');
    } catch {
      toast.error('Error al actualizar reporte');
    }
  };

  if (isLoading) {
    return <ReportsSkeleton />;
  }

  const reports = reportsData?.items || [];
  const totalPages = reportsData?.totalPages || 1;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-foreground text-3xl font-bold">Reportes</h1>
        <p className="text-muted-foreground">Gestiona los reportes de la comunidad</p>
      </div>

      {/* Stats */}
      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-blue-100 p-2">
                <Flag className="h-5 w-5 text-blue-600" />
              </div>
              <div>
                <p className="text-xl font-bold">{stats?.total || 0}</p>
                <p className="text-muted-foreground text-xs">Total</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-amber-100 p-2">
                <Clock className="h-5 w-5 text-amber-600" />
              </div>
              <div>
                <p className="text-xl font-bold">{stats?.pending || 0}</p>
                <p className="text-muted-foreground text-xs">Pendientes</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-primary/10 p-2">
                <CheckCircle className="h-5 w-5 text-primary" />
              </div>
              <div>
                <p className="text-xl font-bold">{stats?.resolved || 0}</p>
                <p className="text-muted-foreground text-xs">Resueltos</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-red-100 p-2">
                <AlertTriangle className="h-5 w-5 text-red-600" />
              </div>
              <div>
                <p className="text-xl font-bold">{stats?.highPriority || 0}</p>
                <p className="text-muted-foreground text-xs">Alta prioridad</p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Filters */}
      <Card>
        <CardContent className="p-4">
          <div className="flex flex-wrap gap-4">
            <div className="flex flex-1 gap-2">
              <Input
                placeholder="Buscar por vehículo, usuario..."
                value={searchTerm}
                onChange={e => setSearchTerm(e.target.value)}
                onKeyDown={e => e.key === 'Enter' && handleSearch()}
                className="max-w-sm"
              />
              <Button onClick={handleSearch}>
                <Search className="h-4 w-4" />
              </Button>
            </div>
            <Select
              value={filters.status || 'all'}
              onValueChange={v => handleFilterChange('status', v)}
            >
              <SelectTrigger className="w-36">
                <SelectValue placeholder="Estado" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos</SelectItem>
                <SelectItem value="pending">Pendientes</SelectItem>
                <SelectItem value="investigating">En revisión</SelectItem>
                <SelectItem value="resolved">Resueltos</SelectItem>
                <SelectItem value="dismissed">Descartados</SelectItem>
              </SelectContent>
            </Select>
            <Select
              value={filters.type || 'all'}
              onValueChange={v => handleFilterChange('type', v)}
            >
              <SelectTrigger className="w-36">
                <SelectValue placeholder="Tipo" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos</SelectItem>
                <SelectItem value="fraud">Fraude</SelectItem>
                <SelectItem value="inappropriate">Inapropiado</SelectItem>
                <SelectItem value="wrong_info">Info incorrecta</SelectItem>
                <SelectItem value="duplicate">Duplicado</SelectItem>
                <SelectItem value="spam">Spam</SelectItem>
                <SelectItem value="other">Otro</SelectItem>
              </SelectContent>
            </Select>
            <Select
              value={filters.priority || 'all'}
              onValueChange={v => handleFilterChange('priority', v)}
            >
              <SelectTrigger className="w-36">
                <SelectValue placeholder="Prioridad" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todas</SelectItem>
                <SelectItem value="high">Alta</SelectItem>
                <SelectItem value="medium">Media</SelectItem>
                <SelectItem value="low">Baja</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      {/* Reports Table */}
      <Card>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Prioridad</TableHead>
              <TableHead>Vehículo</TableHead>
              <TableHead>Tipo</TableHead>
              <TableHead>Reportado por</TableHead>
              <TableHead>Razón</TableHead>
              <TableHead>Fecha</TableHead>
              <TableHead>Estado</TableHead>
              <TableHead>Acciones</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {reports.map(report => (
              <TableRow key={report.id}>
                <TableCell>
                  <div className="flex items-center gap-2">
                    {getPriorityIndicator(report.priority)}
                    <span className="text-muted-foreground text-xs capitalize">
                      {report.priority}
                    </span>
                  </div>
                </TableCell>
                <TableCell>
                  <Link
                    href={report.type === 'vehicle' ? `/vehiculos/${report.targetId}` : `#`}
                    className="hover:underline"
                  >
                    <div className="flex items-center gap-2">
                      <Car className="text-muted-foreground h-4 w-4" />
                      <span className="font-medium">{report.targetTitle}</span>
                    </div>
                  </Link>
                </TableCell>
                <TableCell>{getTypeBadge(report.type)}</TableCell>
                <TableCell>
                  <div className="flex items-center gap-2">
                    <User className="text-muted-foreground h-4 w-4" />
                    <span>{report.reportedByEmail}</span>
                  </div>
                </TableCell>
                <TableCell>
                  <p
                    className="text-muted-foreground max-w-xs truncate text-sm"
                    title={report.reason}
                  >
                    {report.reason}
                  </p>
                </TableCell>
                <TableCell>
                  <div className="text-muted-foreground flex items-center gap-1 text-sm">
                    <Calendar className="h-3 w-3" />
                    {new Date(report.createdAt).toLocaleDateString('es-DO')}
                  </div>
                </TableCell>
                <TableCell>{getStatusBadge(report.status)}</TableCell>
                <TableCell>
                  <div className="flex gap-1">
                    <Link href={`/admin/reportes/${report.id}`}>
                      <Button variant="ghost" size="sm">
                        <Eye className="h-4 w-4" />
                      </Button>
                    </Link>
                    {report.status === 'pending' && (
                      <>
                        <Button
                          variant="ghost"
                          size="sm"
                          className="text-blue-600"
                          onClick={() => handleUpdateStatus(report.id, 'investigating')}
                          disabled={updateStatusMutation.isPending}
                        >
                          <Clock className="h-4 w-4" />
                        </Button>
                        <AlertDialog
                          open={resolvingId === report.id}
                          onOpenChange={open => !open && setResolvingId(null)}
                        >
                          <AlertDialogTrigger asChild>
                            <Button
                              variant="ghost"
                              size="sm"
                              className="text-primary"
                              onClick={() => setResolvingId(report.id)}
                            >
                              <CheckCircle className="h-4 w-4" />
                            </Button>
                          </AlertDialogTrigger>
                          <AlertDialogContent>
                            <AlertDialogHeader>
                              <AlertDialogTitle>Resolver reporte</AlertDialogTitle>
                              <AlertDialogDescription>
                                Describe la resolución del reporte. Esta información será visible en
                                el historial.
                              </AlertDialogDescription>
                            </AlertDialogHeader>
                            <Textarea
                              placeholder="Resolución del reporte..."
                              value={resolution}
                              onChange={e => setResolution(e.target.value)}
                              rows={3}
                            />
                            <AlertDialogFooter>
                              <AlertDialogCancel onClick={() => setResolution('')}>
                                Cancelar
                              </AlertDialogCancel>
                              <AlertDialogAction
                                onClick={() =>
                                  handleUpdateStatus(report.id, 'resolved', resolution)
                                }
                                disabled={!resolution || updateStatusMutation.isPending}
                                className="bg-primary hover:bg-primary/90"
                              >
                                {updateStatusMutation.isPending && (
                                  <Loader2 className="mr-1 h-4 w-4 animate-spin" />
                                )}
                                Resolver
                              </AlertDialogAction>
                            </AlertDialogFooter>
                          </AlertDialogContent>
                        </AlertDialog>
                        <AlertDialog>
                          <AlertDialogTrigger asChild>
                            <Button variant="ghost" size="sm" className="text-muted-foreground">
                              <XCircle className="h-4 w-4" />
                            </Button>
                          </AlertDialogTrigger>
                          <AlertDialogContent>
                            <AlertDialogHeader>
                              <AlertDialogTitle>¿Descartar reporte?</AlertDialogTitle>
                              <AlertDialogDescription>
                                El reporte será marcado como descartado. Úsalo cuando el reporte no
                                sea válido.
                              </AlertDialogDescription>
                            </AlertDialogHeader>
                            <AlertDialogFooter>
                              <AlertDialogCancel>Cancelar</AlertDialogCancel>
                              <AlertDialogAction
                                onClick={() =>
                                  handleUpdateStatus(
                                    report.id,
                                    'dismissed',
                                    'Descartado por el administrador'
                                  )
                                }
                              >
                                Descartar
                              </AlertDialogAction>
                            </AlertDialogFooter>
                          </AlertDialogContent>
                        </AlertDialog>
                      </>
                    )}
                    {report.status === 'investigating' && (
                      <>
                        <AlertDialog
                          open={resolvingId === report.id}
                          onOpenChange={open => !open && setResolvingId(null)}
                        >
                          <AlertDialogTrigger asChild>
                            <Button
                              variant="ghost"
                              size="sm"
                              className="text-primary"
                              onClick={() => setResolvingId(report.id)}
                            >
                              <CheckCircle className="h-4 w-4" />
                            </Button>
                          </AlertDialogTrigger>
                          <AlertDialogContent>
                            <AlertDialogHeader>
                              <AlertDialogTitle>Resolver reporte</AlertDialogTitle>
                              <AlertDialogDescription>
                                Describe la resolución del reporte.
                              </AlertDialogDescription>
                            </AlertDialogHeader>
                            <Textarea
                              placeholder="Resolución del reporte..."
                              value={resolution}
                              onChange={e => setResolution(e.target.value)}
                              rows={3}
                            />
                            <AlertDialogFooter>
                              <AlertDialogCancel onClick={() => setResolution('')}>
                                Cancelar
                              </AlertDialogCancel>
                              <AlertDialogAction
                                onClick={() =>
                                  handleUpdateStatus(report.id, 'resolved', resolution)
                                }
                                disabled={!resolution || updateStatusMutation.isPending}
                                className="bg-primary hover:bg-primary/90"
                              >
                                Resolver
                              </AlertDialogAction>
                            </AlertDialogFooter>
                          </AlertDialogContent>
                        </AlertDialog>
                      </>
                    )}
                  </div>
                </TableCell>
              </TableRow>
            ))}
            {reports.length === 0 && (
              <TableRow>
                <TableCell colSpan={8} className="text-muted-foreground py-12 text-center">
                  <Flag className="mx-auto mb-2 h-8 w-8" />
                  <p>No se encontraron reportes</p>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </Card>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between">
          <p className="text-muted-foreground text-sm">
            Página {filters.page} de {totalPages}
          </p>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setFilters(prev => ({ ...prev, page: (prev.page || 1) - 1 }))}
              disabled={(filters.page || 1) <= 1}
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setFilters(prev => ({ ...prev, page: (prev.page || 1) + 1 }))}
              disabled={(filters.page || 1) >= totalPages}
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
