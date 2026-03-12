/**
 * Admin Orphan Image Cleanup Page
 *
 * Shows the state of the monthly orphan cleanup job:
 * - Pending report waiting for admin approval (with sample preview)
 * - Approve / dismiss actions
 * - Last cleanup result summary
 * - Idle state when no report is pending
 */

'use client';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
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
  Trash2,
  HardDrive,
  Clock,
  AlertTriangle,
  CheckCircle,
  XCircle,
  FileSearch,
  Database,
  Cloud,
} from 'lucide-react';
import { useOrphanCleanupStatus, useApproveOrphanCleanup, useDismissOrphanReport } from '@/hooks/use-orphan-cleanup';

// =============================================================================
// HELPERS
// =============================================================================

function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 B';
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
  const i = Math.floor(Math.log(bytes) / Math.log(1024));
  return `${(bytes / Math.pow(1024, i)).toFixed(2)} ${sizes[i]}`;
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleString('es-DO', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  });
}

// =============================================================================
// MAIN PAGE
// =============================================================================

export default function OrphanCleanupPage() {
  const { data: status, isLoading, error } = useOrphanCleanupStatus();
  const approveMutation = useApproveOrphanCleanup();
  const dismissMutation = useDismissOrphanReport();

  if (isLoading) {
    return (
      <div className="space-y-6 p-6">
        <h1 className="text-2xl font-bold">Limpieza de Imágenes Huérfanas</h1>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {[1, 2, 3].map((i) => (
            <Card key={i}>
              <CardHeader className="pb-2"><Skeleton className="h-4 w-24" /></CardHeader>
              <CardContent><Skeleton className="h-8 w-32" /></CardContent>
            </Card>
          ))}
        </div>
      </div>
    );
  }

  if (error || !status) {
    return (
      <div className="space-y-6 p-6">
        <h1 className="text-2xl font-bold">Limpieza de Imágenes Huérfanas</h1>
        <Card className="border-red-200 bg-red-50">
          <CardContent className="pt-6">
            <p className="text-red-700 flex items-center gap-2">
              <AlertTriangle className="h-5 w-5" />
              Error al cargar el estado del cleanup:
              {error instanceof Error ? ` ${error.message}` : ' Error desconocido'}
            </p>
          </CardContent>
        </Card>
      </div>
    );
  }

  const { hasPendingReport, pendingReport, lastResult } = status;

  return (
    <div className="space-y-6 p-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold flex items-center gap-2">
            <Trash2 className="h-6 w-6 text-amber-500" />
            Limpieza de Imágenes Huérfanas
          </h1>
          <p className="text-muted-foreground mt-1">
            Escaneo mensual de DO Spaces — detecta archivos en S3 sin registro en la base de datos
          </p>
        </div>
        <Badge variant={hasPendingReport ? 'danger' : 'secondary'}>
          {hasPendingReport ? 'Reporte pendiente' : 'Sin reportes'}
        </Badge>
      </div>

      {/* ── PENDING REPORT ── */}
      {hasPendingReport && pendingReport && (
        <>
          {/* Summary Cards */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            <Card className="border-amber-200">
              <CardHeader className="flex flex-row items-center justify-between pb-2">
                <CardTitle className="text-sm font-medium text-muted-foreground">
                  Huérfanos detectados
                </CardTitle>
                <FileSearch className="h-4 w-4 text-amber-500" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold text-amber-600">
                  {pendingReport.orphanCount.toLocaleString()}
                </div>
                <p className="text-xs text-muted-foreground mt-1">archivos sin registro en DB</p>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="flex flex-row items-center justify-between pb-2">
                <CardTitle className="text-sm font-medium text-muted-foreground">
                  Espacio a liberar
                </CardTitle>
                <HardDrive className="h-4 w-4 text-blue-500" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">
                  {pendingReport.totalSizeGb.toFixed(2)} GB
                </div>
                <p className="text-xs text-muted-foreground mt-1">
                  {formatBytes(pendingReport.totalSizeBytes)}
                </p>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="flex flex-row items-center justify-between pb-2">
                <CardTitle className="text-sm font-medium text-muted-foreground">
                  Objetos en S3
                </CardTitle>
                <Cloud className="h-4 w-4 text-purple-500" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">
                  {pendingReport.totalS3Objects.toLocaleString()}
                </div>
                <p className="text-xs text-muted-foreground mt-1">
                  Registros DB: {pendingReport.totalDbKeys.toLocaleString()}
                </p>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="flex flex-row items-center justify-between pb-2">
                <CardTitle className="text-sm font-medium text-muted-foreground">
                  Escaneado
                </CardTitle>
                <Clock className="h-4 w-4 text-gray-500" />
              </CardHeader>
              <CardContent>
                <div className="text-lg font-semibold">
                  {formatDate(pendingReport.generatedAt)}
                </div>
                <p className="text-xs text-muted-foreground mt-1">
                  Duración: {pendingReport.scanDurationSeconds.toFixed(1)}s
                </p>
              </CardContent>
            </Card>
          </div>

          {/* Sample Orphans Table */}
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">
                Muestra de archivos huérfanos (primeros 20)
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="rounded-md border overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b bg-muted/50">
                      <th className="py-2 px-3 text-left font-medium">Archivo</th>
                      <th className="py-2 px-3 text-left font-medium">Listing ID</th>
                      <th className="py-2 px-3 text-right font-medium">Tamaño</th>
                      <th className="py-2 px-3 text-left font-medium">Última modificación</th>
                    </tr>
                  </thead>
                  <tbody>
                    {pendingReport.sampleOrphans.map((orphan, idx) => (
                      <tr key={idx} className="border-b last:border-b-0 hover:bg-muted/30">
                        <td className="py-2 px-3 font-mono text-xs max-w-[300px] truncate" title={orphan.storageKey}>
                          {orphan.storageKey}
                        </td>
                        <td className="py-2 px-3">
                          <Badge variant="outline">{orphan.listingId}</Badge>
                        </td>
                        <td className="py-2 px-3 text-right tabular-nums">
                          {formatBytes(orphan.sizeBytes)}
                        </td>
                        <td className="py-2 px-3 text-muted-foreground">
                          {formatDate(orphan.lastModified)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </CardContent>
          </Card>

          {/* Action Buttons */}
          <div className="flex gap-3">
            {/* APPROVE */}
            <AlertDialog>
              <AlertDialogTrigger asChild>
                <Button variant="destructive" className="gap-2" disabled={approveMutation.isPending}>
                  <Trash2 className="h-4 w-4" />
                  Aprobar eliminación ({pendingReport.orphanCount.toLocaleString()} archivos)
                </Button>
              </AlertDialogTrigger>
              <AlertDialogContent>
                <AlertDialogHeader>
                  <AlertDialogTitle className="flex items-center gap-2">
                    <AlertTriangle className="h-5 w-5 text-red-500" />
                    ¿Confirmar eliminación de archivos huérfanos?
                  </AlertDialogTitle>
                  <AlertDialogDescription>
                    Esta acción eliminará <strong>{pendingReport.orphanCount.toLocaleString()}</strong> archivos
                    ({pendingReport.totalSizeGb.toFixed(2)} GB) de DO Spaces.
                    <br /><br />
                    Los archivos son imágenes que existen en S3 pero <strong>no tienen registro en la base de datos</strong>.
                    La eliminación es irreversible y se registra en el log de auditoría.
                    <br /><br />
                    El proceso inicia dentro de 30 segundos después de aprobar.
                  </AlertDialogDescription>
                </AlertDialogHeader>
                <AlertDialogFooter>
                  <AlertDialogCancel>Cancelar</AlertDialogCancel>
                  <AlertDialogAction
                    onClick={() => approveMutation.mutate()}
                    className="bg-red-600 hover:bg-red-700"
                  >
                    Sí, eliminar {pendingReport.orphanCount.toLocaleString()} archivos
                  </AlertDialogAction>
                </AlertDialogFooter>
              </AlertDialogContent>
            </AlertDialog>

            {/* DISMISS */}
            <AlertDialog>
              <AlertDialogTrigger asChild>
                <Button variant="outline" className="gap-2" disabled={dismissMutation.isPending}>
                  <XCircle className="h-4 w-4" />
                  Descartar reporte
                </Button>
              </AlertDialogTrigger>
              <AlertDialogContent>
                <AlertDialogHeader>
                  <AlertDialogTitle>¿Descartar reporte de huérfanos?</AlertDialogTitle>
                  <AlertDialogDescription>
                    El reporte será descartado y no se eliminará ningún archivo.
                    El próximo escaneo se ejecutará en el intervalo programado (mensual).
                  </AlertDialogDescription>
                </AlertDialogHeader>
                <AlertDialogFooter>
                  <AlertDialogCancel>Cancelar</AlertDialogCancel>
                  <AlertDialogAction onClick={() => dismissMutation.mutate()}>
                    Descartar
                  </AlertDialogAction>
                </AlertDialogFooter>
              </AlertDialogContent>
            </AlertDialog>
          </div>

          {/* Mutation feedback */}
          {approveMutation.isSuccess && (
            <Card className="border-green-200 bg-green-50">
              <CardContent className="pt-4">
                <p className="text-green-700 flex items-center gap-2">
                  <CheckCircle className="h-5 w-5" />
                  Eliminación aprobada. El proceso iniciará en los próximos 30 segundos.
                </p>
              </CardContent>
            </Card>
          )}
          {approveMutation.isError && (
            <Card className="border-red-200 bg-red-50">
              <CardContent className="pt-4">
                <p className="text-red-700 flex items-center gap-2">
                  <AlertTriangle className="h-5 w-5" />
                  Error al aprobar: {approveMutation.error instanceof Error ? approveMutation.error.message : 'Error desconocido'}
                </p>
              </CardContent>
            </Card>
          )}
          {dismissMutation.isSuccess && (
            <Card className="border-blue-200 bg-blue-50">
              <CardContent className="pt-4">
                <p className="text-blue-700 flex items-center gap-2">
                  <CheckCircle className="h-5 w-5" />
                  Reporte descartado. No se eliminará ningún archivo.
                </p>
              </CardContent>
            </Card>
          )}
        </>
      )}

      {/* ── LAST RESULT ── */}
      {lastResult && (
        <Card className="border-green-200">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <CheckCircle className="h-5 w-5 text-green-500" />
              Última limpieza ejecutada
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <p className="text-sm text-muted-foreground">Fecha</p>
                <p className="font-semibold">{formatDate(lastResult.completedAt)}</p>
              </div>
              <div>
                <p className="text-sm text-muted-foreground">Archivos eliminados</p>
                <p className="font-semibold text-green-600">
                  {lastResult.deletedCount.toLocaleString()}
                  {lastResult.failedCount > 0 && (
                    <span className="text-red-500 ml-2">
                      ({lastResult.failedCount} fallidos)
                    </span>
                  )}
                </p>
              </div>
              <div>
                <p className="text-sm text-muted-foreground">Espacio liberado</p>
                <p className="font-semibold">{lastResult.freedGb.toFixed(2)} GB</p>
              </div>
            </div>
            <p className="text-xs text-muted-foreground mt-3">
              Duración: {lastResult.durationSeconds.toFixed(1)}s
            </p>
          </CardContent>
        </Card>
      )}

      {/* ── IDLE STATE ── */}
      {!hasPendingReport && !lastResult && (
        <Card className="border-dashed">
          <CardContent className="pt-6 text-center">
            <Database className="h-12 w-12 mx-auto text-muted-foreground mb-3" />
            <h3 className="text-lg font-semibold">Sin reportes pendientes</h3>
            <p className="text-muted-foreground mt-1">
              El escaneo de imágenes huérfanas se ejecuta mensualmente.
              Cuando se detecten archivos huérfanos, aparecerá un reporte aquí para tu revisión.
            </p>
          </CardContent>
        </Card>
      )}

      {/* Info footer */}
      <Card className="bg-muted/30">
        <CardContent className="pt-4 text-sm text-muted-foreground">
          <p className="flex items-center gap-2 mb-2">
            <Clock className="h-4 w-4" />
            <strong>Frecuencia:</strong> El escaneo se ejecuta cada 30 días automáticamente.
          </p>
          <p className="flex items-center gap-2 mb-2">
            <FileSearch className="h-4 w-4" />
            <strong>Proceso:</strong> Se comparan todos los objetos en DO Spaces con los registros de la base de datos.
            Los archivos sin registro se marcan como huérfanos.
          </p>
          <p className="flex items-center gap-2">
            <AlertTriangle className="h-4 w-4" />
            <strong>Seguridad:</strong> La eliminación NUNCA es automática. Requiere aprobación manual de un administrador.
            Cada archivo eliminado se registra en el log de auditoría.
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
