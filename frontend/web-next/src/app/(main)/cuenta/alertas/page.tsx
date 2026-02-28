/**
 * User Alerts Page
 *
 * Manage price alerts and notifications
 * Connected to AlertService via API Gateway
 */

'use client';

import Link from 'next/link';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Switch } from '@/components/ui/switch';
import { Skeleton } from '@/components/ui/skeleton';
import { toast } from 'sonner';
import {
  Bell,
  Plus,
  Trash2,
  TrendingDown,
  DollarSign,
  Car,
  Check,
  AlertCircle,
  Loader2,
} from 'lucide-react';
import {
  usePriceAlerts,
  useTogglePriceAlert,
  useDeletePriceAlert,
  useAlertStats,
  type PriceAlert,
} from '@/hooks/use-alerts';

const formatPrice = (price: number) => {
  return new Intl.NumberFormat('es-DO', {
    style: 'currency',
    currency: 'DOP',
    maximumFractionDigits: 0,
  }).format(price);
};

export default function AlertsPage() {
  // Fetch alerts from API
  const { data: alertsData, isLoading, error, refetch } = usePriceAlerts();
  const { data: stats } = useAlertStats();

  // Mutations
  const toggleMutation = useTogglePriceAlert();
  const deleteMutation = useDeletePriceAlert();

  // Get alerts array from paginated response
  const alerts = alertsData?.items ?? [];

  const handleToggleAlert = async (alert: PriceAlert) => {
    try {
      await toggleMutation.mutateAsync({ id: alert.id, isActive: alert.isActive });
      toast.success(alert.isActive ? 'Alerta desactivada' : 'Alerta activada');
    } catch {
      toast.error('Error al actualizar la alerta');
    }
  };

  const handleDeleteAlert = async (id: string) => {
    try {
      await deleteMutation.mutateAsync(id);
      toast.success('Alerta eliminada');
    } catch {
      toast.error('Error al eliminar la alerta');
    }
  };

  const activeAlerts = alerts.filter(a => a.isActive && !a.isTriggered);
  const triggeredAlerts = alerts.filter(a => a.isTriggered);

  // Show loading skeleton
  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="flex flex-col justify-between gap-4 sm:flex-row">
          <div>
            <h1 className="text-foreground text-2xl font-bold">Alertas de Precio</h1>
            <p className="text-muted-foreground">Recibe notificaciones cuando baje el precio</p>
          </div>
        </div>
        <div className="grid grid-cols-3 gap-4">
          {[1, 2, 3].map(i => (
            <Card key={i}>
              <CardContent className="p-4">
                <Skeleton className="h-16 w-full" />
              </CardContent>
            </Card>
          ))}
        </div>
        <Card>
          <CardHeader>
            <Skeleton className="h-6 w-32" />
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {[1, 2, 3].map(i => (
                <Skeleton key={i} className="h-20 w-full" />
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  // Show error state
  if (error) {
    return (
      <div className="space-y-6">
        <div className="flex flex-col justify-between gap-4 sm:flex-row">
          <div>
            <h1 className="text-foreground text-2xl font-bold">Alertas de Precio</h1>
            <p className="text-muted-foreground">Recibe notificaciones cuando baje el precio</p>
          </div>
        </div>
        <Card className="border-red-200">
          <CardContent className="flex flex-col items-center py-12">
            <AlertCircle className="mb-4 h-12 w-12 text-red-500" />
            <p className="text-muted-foreground mb-4">Error al cargar las alertas</p>
            <Button onClick={() => refetch()} variant="outline">
              Reintentar
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col justify-between gap-4 sm:flex-row">
        <div>
          <h1 className="text-foreground text-2xl font-bold">Alertas de Precio</h1>
          <p className="text-muted-foreground">Recibe notificaciones cuando baje el precio</p>
        </div>
        <Button asChild className="bg-primary hover:bg-primary/90">
          <Link href="/buscar">
            <Plus className="mr-2 h-4 w-4" />
            Nueva Alerta
          </Link>
        </Button>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-3 gap-4">
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="bg-primary/10 rounded-lg p-2">
                <Bell className="text-primary h-5 w-5" />
              </div>
              <div>
                <p className="text-2xl font-bold">
                  {stats?.activePriceAlerts ?? activeAlerts.length}
                </p>
                <p className="text-muted-foreground text-sm">Activas</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-blue-100 p-2">
                <Check className="h-5 w-5 text-blue-600" />
              </div>
              <div>
                <p className="text-2xl font-bold">
                  {stats?.priceDropsThisMonth ?? triggeredAlerts.length}
                </p>
                <p className="text-muted-foreground text-sm">Activadas</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="bg-muted rounded-lg p-2">
                <TrendingDown className="text-muted-foreground h-5 w-5" />
              </div>
              <div>
                <p className="text-2xl font-bold">{stats?.totalPriceAlerts ?? alerts.length}</p>
                <p className="text-muted-foreground text-sm">Total</p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Active Alerts */}
      <Card>
        <CardHeader>
          <CardTitle>Alertas Activas</CardTitle>
          <CardDescription>Te notificaremos cuando el precio llegue a tu objetivo</CardDescription>
        </CardHeader>
        <CardContent>
          {activeAlerts.length > 0 ? (
            <div className="space-y-4">
              {activeAlerts.map(alert => (
                <div
                  key={alert.id}
                  className="flex flex-col justify-between gap-4 rounded-lg border p-4 sm:flex-row sm:items-center"
                >
                  <div className="flex items-center gap-4">
                    <div className="bg-muted flex h-12 w-16 items-center justify-center rounded">
                      <Car className="text-muted-foreground h-6 w-6" />
                    </div>
                    <div>
                      <Link
                        href={`/vehiculos/${alert.vehicleSlug ?? alert.vehicleId}`}
                        className="hover:text-primary font-medium"
                      >
                        {alert.vehicleTitle}
                      </Link>
                      <div className="flex items-center gap-4 text-sm">
                        <span className="text-muted-foreground">
                          Actual: {formatPrice(alert.currentPrice)}
                        </span>
                        <span className="text-primary font-medium">
                          Objetivo: {formatPrice(alert.targetPrice)}
                        </span>
                      </div>
                    </div>
                  </div>
                  <div className="flex items-center gap-4">
                    <div className="flex items-center gap-2">
                      <span className="text-muted-foreground text-sm">Activa</span>
                      <Switch
                        checked={alert.isActive}
                        onCheckedChange={() => handleToggleAlert(alert)}
                        disabled={toggleMutation.isPending}
                      />
                    </div>
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={() => handleDeleteAlert(alert.id)}
                      disabled={deleteMutation.isPending}
                    >
                      {deleteMutation.isPending ? (
                        <Loader2 className="h-4 w-4 animate-spin" />
                      ) : (
                        <Trash2 className="h-4 w-4 text-red-500" />
                      )}
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div className="text-muted-foreground py-8 text-center">
              <Bell className="mx-auto mb-3 h-12 w-12 opacity-50" />
              <p>No tienes alertas activas</p>
              <p className="mt-2 text-sm">
                Busca vehículos y crea alertas de precio para recibir notificaciones
              </p>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Triggered Alerts */}
      {triggeredAlerts.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Check className="text-primary h-5 w-5" />
              Alertas Activadas
            </CardTitle>
            <CardDescription>Estos vehículos alcanzaron tu precio objetivo</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {triggeredAlerts.map(alert => (
                <div
                  key={alert.id}
                  className="border-primary bg-primary/10 flex flex-col justify-between gap-4 rounded-lg border p-4 sm:flex-row sm:items-center"
                >
                  <div className="flex items-center gap-4">
                    <div className="bg-card flex h-12 w-16 items-center justify-center rounded">
                      <Car className="text-primary h-6 w-6" />
                    </div>
                    <div>
                      <h4 className="font-medium">{alert.vehicleTitle}</h4>
                      <div className="flex items-center gap-2 text-sm">
                        <Badge className="bg-primary/10 text-primary">
                          <TrendingDown className="mr-1 h-3 w-3" />
                          Precio bajó a {formatPrice(alert.currentPrice)}
                        </Badge>
                      </div>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <Button asChild className="bg-primary hover:bg-primary/90">
                      <Link href={`/vehiculos/${alert.vehicleSlug ?? alert.vehicleId}`}>
                        Ver Vehículo
                      </Link>
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={() => handleDeleteAlert(alert.id)}
                      disabled={deleteMutation.isPending}
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Tips */}
      <Card className="border-blue-200 bg-blue-50">
        <CardContent className="p-4">
          <div className="flex items-start gap-3">
            <DollarSign className="mt-0.5 h-5 w-5 flex-shrink-0 text-blue-600" />
            <div>
              <h4 className="font-medium text-blue-800">¿Cómo funcionan las alertas?</h4>
              <p className="mt-1 text-sm text-blue-700">
                Cuando el precio de un vehículo baje al valor que configuraste, te enviaremos una
                notificación por email y/o push según tus preferencias. También puedes ver las
                alertas activadas en esta página.
              </p>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
