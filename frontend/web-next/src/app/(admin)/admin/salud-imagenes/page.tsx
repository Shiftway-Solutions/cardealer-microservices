/**
 * Admin Image Health Dashboard
 *
 * Real-time view of image integrity in DO Spaces showing:
 * - Total active images, health percentage (green/yellow/red)
 * - Top 20 listings with most broken images
 * - Re-verify all images button (triggers manual scan)
 * - Current DO Spaces storage cost estimate
 * - Flag listing as "requires dealer attention"
 */

'use client';

import { useState } from 'react';
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
import { Textarea } from '@/components/ui/textarea';
import {
  ImageIcon,
  ShieldCheck,
  ShieldAlert,
  RefreshCw,
  DollarSign,
  HardDrive,
  Clock,
  AlertTriangle,
  Flag,
  Loader2,
  CheckCircle,
} from 'lucide-react';
import { toast } from 'sonner';
import { useImageHealthDashboard, useTriggerScan, useFlagListing } from '@/hooks/use-image-health';
import type { BrokenListing } from '@/services/image-health';

// =============================================================================
// HELPERS
// =============================================================================

function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(2))} ${sizes[i]}`;
}

function formatDate(dateStr: string | null): string {
  if (!dateStr) return 'Nunca';
  const date = new Date(dateStr);
  return date.toLocaleString('es-DO', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function getHealthColor(status: string): string {
  switch (status) {
    case 'green':
      return 'text-green-600 bg-green-50 border-green-200';
    case 'yellow':
      return 'text-yellow-600 bg-yellow-50 border-yellow-200';
    case 'red':
      return 'text-red-600 bg-red-50 border-red-200';
    default:
      return 'text-gray-600 bg-gray-50 border-gray-200';
  }
}

function getHealthBadgeVariant(status: string): 'default' | 'secondary' | 'danger' | 'outline' {
  switch (status) {
    case 'green':
      return 'default';
    case 'yellow':
      return 'secondary';
    case 'red':
      return 'danger';
    default:
      return 'outline';
  }
}

function getHealthLabel(status: string): string {
  switch (status) {
    case 'green':
      return 'Excelente';
    case 'yellow':
      return 'Atención';
    case 'red':
      return 'Crítico';
    default:
      return 'Desconocido';
  }
}

// =============================================================================
// SUB-COMPONENTS
// =============================================================================

function DashboardSkeleton() {
  return (
    <div className="space-y-6">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {Array.from({ length: 4 }).map((_, i) => (
          <Card key={i}>
            <CardHeader className="pb-2">
              <Skeleton className="h-4 w-24" />
            </CardHeader>
            <CardContent>
              <Skeleton className="h-8 w-16" />
              <Skeleton className="mt-2 h-3 w-32" />
            </CardContent>
          </Card>
        ))}
      </div>
      <Card>
        <CardHeader>
          <Skeleton className="h-5 w-48" />
        </CardHeader>
        <CardContent>
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} className="mb-3 h-12 w-full" />
          ))}
        </CardContent>
      </Card>
    </div>
  );
}

function FlagListingDialog({
  listing,
  onFlag,
  isPending,
}: {
  listing: BrokenListing;
  onFlag: (reason: string) => void;
  isPending: boolean;
}) {
  const [reason, setReason] = useState('');

  return (
    <AlertDialog>
      <AlertDialogTrigger asChild>
        <Button variant="outline" size="sm" className="gap-1">
          <Flag className="h-3.5 w-3.5" />
          Notificar
        </Button>
      </AlertDialogTrigger>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Notificar al dealer</AlertDialogTitle>
          <AlertDialogDescription>
            Se notificará al dealer que el listing <strong>{listing.ownerId}</strong> requiere
            atención por imágenes rotas ({listing.brokenCount} de {listing.totalImages}).
          </AlertDialogDescription>
        </AlertDialogHeader>
        <div className="my-4">
          <Textarea
            placeholder="Motivo adicional (opcional)..."
            value={reason}
            onChange={e => setReason(e.target.value)}
            rows={3}
          />
        </div>
        <AlertDialogFooter>
          <AlertDialogCancel>Cancelar</AlertDialogCancel>
          <AlertDialogAction onClick={() => onFlag(reason)} disabled={isPending} className="gap-1">
            {isPending ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <Flag className="h-4 w-4" />
            )}
            Enviar notificación
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}

// =============================================================================
// MAIN PAGE COMPONENT
// =============================================================================

export default function ImageHealthPage() {
  const { data, isLoading, error, refetch } = useImageHealthDashboard();
  const triggerScan = useTriggerScan();
  const flagListing = useFlagListing();

  const handleTriggerScan = () => {
    triggerScan.mutate(undefined, {
      onSuccess: result => {
        toast.success('Escaneo iniciado', {
          description: result.message,
        });
      },
      onError: () => {
        toast.error('Error al iniciar escaneo', {
          description: 'Intenta de nuevo en unos momentos.',
        });
      },
    });
  };

  const handleFlagListing = (listing: BrokenListing, reason: string) => {
    flagListing.mutate(
      {
        ownerId: listing.ownerId,
        dealerId: listing.dealerId,
        reason: reason || 'Imágenes rotas detectadas — requiere atención',
      },
      {
        onSuccess: () => {
          toast.success('Dealer notificado', {
            description: `Listing ${listing.ownerId} marcado como "requiere atención"`,
          });
        },
        onError: () => {
          toast.error('Error al notificar', {
            description: 'No se pudo enviar la notificación. Intenta de nuevo.',
          });
        },
      }
    );
  };

  if (isLoading) {
    return (
      <div className="space-y-6 p-6">
        <h1 className="text-2xl font-bold">Salud de Imágenes</h1>
        <DashboardSkeleton />
      </div>
    );
  }

  if (error || !data) {
    return (
      <div className="space-y-6 p-6">
        <h1 className="text-2xl font-bold">Salud de Imágenes</h1>
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <AlertTriangle className="mb-4 h-12 w-12 text-yellow-500" />
            <p className="text-lg font-medium">No se pudo cargar el dashboard</p>
            <p className="text-muted-foreground text-sm">
              {error instanceof Error ? error.message : 'Error de conexión'}
            </p>
            <Button variant="outline" className="mt-4" onClick={() => refetch()}>
              <RefreshCw className="mr-2 h-4 w-4" />
              Reintentar
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  const { summary, topBrokenListings } = data;

  return (
    <div className="space-y-6 p-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Salud de Imágenes</h1>
          <p className="text-muted-foreground text-sm">
            Monitoreo en tiempo real de la integridad de imágenes en DO Spaces
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" onClick={() => refetch()}>
            <RefreshCw className="mr-2 h-4 w-4" />
            Actualizar
          </Button>
          <Button
            size="sm"
            onClick={handleTriggerScan}
            disabled={triggerScan.isPending}
            className="gap-1"
          >
            {triggerScan.isPending ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <ShieldCheck className="h-4 w-4" />
            )}
            Re-verificar todas las imágenes
          </Button>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-5">
        {/* Health Percentage */}
        <Card className={`border ${getHealthColor(summary.healthStatus)}`}>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Salud General</CardTitle>
            {summary.healthStatus === 'green' ? (
              <ShieldCheck className="h-4 w-4" />
            ) : (
              <ShieldAlert className="h-4 w-4" />
            )}
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{summary.healthPercentage}%</div>
            <Badge variant={getHealthBadgeVariant(summary.healthStatus)} className="mt-1">
              {getHealthLabel(summary.healthStatus)}
            </Badge>
          </CardContent>
        </Card>

        {/* Total Active Images */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Imágenes Activas</CardTitle>
            <ImageIcon className="text-muted-foreground h-4 w-4" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {summary.totalActiveImages.toLocaleString('es-DO')}
            </div>
            <p className="text-muted-foreground text-xs">
              {summary.healthyImages.toLocaleString('es-DO')} accesibles
            </p>
          </CardContent>
        </Card>

        {/* Broken Images */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Imágenes Rotas</CardTitle>
            <AlertTriangle className="text-destructive h-4 w-4" />
          </CardHeader>
          <CardContent>
            <div className="text-destructive text-2xl font-bold">
              {summary.brokenImages.toLocaleString('es-DO')}
            </div>
            <p className="text-muted-foreground text-xs">
              {summary.brokenImages > 0
                ? `${((summary.brokenImages / summary.totalActiveImages) * 100).toFixed(1)}% del total`
                : 'Sin problemas'}
            </p>
          </CardContent>
        </Card>

        {/* Storage */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Almacenamiento</CardTitle>
            <HardDrive className="text-muted-foreground h-4 w-4" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{summary.totalStorageGb} GB</div>
            <p className="text-muted-foreground text-xs">
              {formatBytes(summary.totalStorageBytes)}
            </p>
          </CardContent>
        </Card>

        {/* Cost */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Costo Mensual</CardTitle>
            <DollarSign className="text-muted-foreground h-4 w-4" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">${summary.estimatedMonthlyCostUsd.toFixed(2)}</div>
            <p className="text-muted-foreground text-xs">DO Spaces estimado</p>
          </CardContent>
        </Card>
      </div>

      {/* Last Scan Info */}
      <div className="text-muted-foreground flex items-center gap-2 text-sm">
        <Clock className="h-4 w-4" />
        <span>
          Último escaneo: <strong>{formatDate(summary.lastScanTime)}</strong>
        </span>
        {triggerScan.isSuccess && (
          <Badge variant="outline" className="ml-2 gap-1 text-green-600">
            <CheckCircle className="h-3 w-3" />
            Escaneo manual en progreso
          </Badge>
        )}
      </div>

      {/* Top 20 Broken Listings */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <AlertTriangle className="text-destructive h-5 w-5" />
            Top 20 Listings con Más Imágenes Rotas
          </CardTitle>
        </CardHeader>
        <CardContent>
          {topBrokenListings.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 text-center">
              <CheckCircle className="mb-4 h-12 w-12 text-green-500" />
              <p className="text-lg font-medium">¡Sin imágenes rotas!</p>
              <p className="text-muted-foreground text-sm">
                Todas las imágenes están accesibles en este momento.
              </p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b text-left">
                    <th className="pb-2 font-medium">#</th>
                    <th className="pb-2 font-medium">Listing ID</th>
                    <th className="pb-2 font-medium">Dealer ID</th>
                    <th className="pb-2 text-center font-medium">Total</th>
                    <th className="pb-2 text-center font-medium">Rotas</th>
                    <th className="pb-2 text-center font-medium">% Rotas</th>
                    <th className="pb-2 font-medium">Detectado</th>
                    <th className="pb-2 text-right font-medium">Acción</th>
                  </tr>
                </thead>
                <tbody>
                  {topBrokenListings.map((listing, index) => (
                    <tr key={listing.ownerId} className="border-b last:border-0">
                      <td className="text-muted-foreground py-3">{index + 1}</td>
                      <td className="py-3 font-mono text-xs">{listing.ownerId}</td>
                      <td className="py-3 font-mono text-xs">{listing.dealerId.slice(0, 8)}…</td>
                      <td className="py-3 text-center">{listing.totalImages}</td>
                      <td className="text-destructive py-3 text-center font-medium">
                        {listing.brokenCount}
                      </td>
                      <td className="py-3 text-center">
                        <Badge variant={listing.brokenPercentage >= 50 ? 'danger' : 'secondary'}>
                          {listing.brokenPercentage}%
                        </Badge>
                      </td>
                      <td className="text-muted-foreground py-3 text-xs">
                        {formatDate(listing.lastDetectedAt)}
                      </td>
                      <td className="py-3 text-right">
                        <FlagListingDialog
                          listing={listing}
                          onFlag={reason => handleFlagListing(listing, reason)}
                          isPending={flagListing.isPending}
                        />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
