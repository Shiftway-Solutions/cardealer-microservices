/**
 * User Alerts Page
 *
 * Manage price alerts and notifications
 * Connected to AlertService via API Gateway
 */

'use client';

import Link from 'next/link';
import * as React from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Switch } from '@/components/ui/switch';
import { Skeleton } from '@/components/ui/skeleton';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
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
  Search,
} from 'lucide-react';
import {
  usePriceAlerts,
  useTogglePriceAlert,
  useDeletePriceAlert,
  useAlertStats,
  type PriceAlert,
} from '@/hooks/use-alerts';
import { useCreateSavedSearch } from '@/hooks/use-alerts';
import { useMakes, useModelsByMake } from '@/hooks/use-vehicles';
import { formatPrice } from '@/lib/format';

export default function AlertsPage() {
  // Fetch alerts from API
  const { data: alertsData, isLoading, error, refetch } = usePriceAlerts();
  const { data: stats } = useAlertStats();

  // Mutations
  const toggleMutation = useTogglePriceAlert();
  const deleteMutation = useDeletePriceAlert();
  const createSavedSearchMutation = useCreateSavedSearch();

  // Create alert form state
  const [showCreateForm, setShowCreateForm] = React.useState(false);
  const [alertMake, setAlertMake] = React.useState('');
  const [alertModel, setAlertModel] = React.useState('');
  const [alertYearMin, setAlertYearMin] = React.useState('');
  const [alertYearMax, setAlertYearMax] = React.useState('');
  const [alertPriceMax, setAlertPriceMax] = React.useState('');

  // Catalog data for selects
  const { data: makes = [] } = useMakes();
  const { data: models = [] } = useModelsByMake(alertMake);

  // Get alerts array from paginated response
  const alerts = alertsData?.items ?? [];

  const handleCreateAlert = async () => {
    if (!alertMake) {
      toast.error('Selecciona al menos una marca');
      return;
    }
    const name = [alertMake, alertModel, alertYearMin && `${alertYearMin}+`]
      .filter(Boolean)
      .join(' ');
    try {
      await createSavedSearchMutation.mutateAsync({
        name: name || 'Alerta de búsqueda',
        searchParams: {
          make: alertMake || undefined,
          model: alertModel || undefined,
          yearMin: alertYearMin ? parseInt(alertYearMin) : undefined,
          yearMax: alertYearMax ? parseInt(alertYearMax) : undefined,
          priceMax: alertPriceMax ? parseInt(alertPriceMax) : undefined,
        },
        notifyNewListings: true,
        notifyFrequency: 'instant',
      });
      toast.success('Alerta creada exitosamente');
      setShowCreateForm(false);
      setAlertMake('');
      setAlertModel('');
      setAlertYearMin('');
      setAlertYearMax('');
      setAlertPriceMax('');
    } catch {
      toast.error('Error al crear la alerta');
    }
  };

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
        <Button
          onClick={() => setShowCreateForm(v => !v)}
          className="bg-primary hover:bg-primary/90"
        >
          <Plus className="mr-2 h-4 w-4" />
          Nueva Alerta
        </Button>
      </div>

      {/* Create Alert Form */}
      {showCreateForm && (
        <Card className="border-primary/20">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-lg">
              <Search className="h-5 w-5" />
              Crear Alerta por Marca / Modelo / Año
            </CardTitle>
            <CardDescription>
              Te notificaremos cuando aparezca un vehículo que coincida con estos criterios
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-5">
              <div className="space-y-2">
                <Label htmlFor="alert-make">Marca</Label>
                <Select
                  value={alertMake}
                  onValueChange={v => {
                    setAlertMake(v);
                    setAlertModel('');
                  }}
                >
                  <SelectTrigger id="alert-make">
                    <SelectValue placeholder="Seleccionar" />
                  </SelectTrigger>
                  <SelectContent>
                    {makes.map((m: { id: string; name: string }) => (
                      <SelectItem key={m.id} value={m.name}>
                        {m.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="alert-model">Modelo</Label>
                <Select value={alertModel} onValueChange={setAlertModel} disabled={!alertMake}>
                  <SelectTrigger id="alert-model">
                    <SelectValue placeholder="Seleccionar" />
                  </SelectTrigger>
                  <SelectContent>
                    {models.map((m: { id: string; name: string }) => (
                      <SelectItem key={m.id} value={m.name}>
                        {m.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="alert-year-min">Año desde</Label>
                <Input
                  id="alert-year-min"
                  type="number"
                  placeholder="2018"
                  value={alertYearMin}
                  onChange={e => setAlertYearMin(e.target.value)}
                  min={1990}
                  max={2027}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="alert-year-max">Año hasta</Label>
                <Input
                  id="alert-year-max"
                  type="number"
                  placeholder="2026"
                  value={alertYearMax}
                  onChange={e => setAlertYearMax(e.target.value)}
                  min={1990}
                  max={2027}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="alert-price-max">Precio máximo (RD$)</Label>
                <Input
                  id="alert-price-max"
                  type="number"
                  placeholder="2,000,000"
                  value={alertPriceMax}
                  onChange={e => setAlertPriceMax(e.target.value)}
                  min={0}
                />
              </div>
            </div>
            <div className="mt-4 flex items-center gap-3">
              <Button
                onClick={handleCreateAlert}
                disabled={createSavedSearchMutation.isPending || !alertMake}
                className="bg-primary hover:bg-primary/90"
              >
                {createSavedSearchMutation.isPending ? (
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                ) : (
                  <Bell className="mr-2 h-4 w-4" />
                )}
                Crear Alerta
              </Button>
              <Button variant="ghost" onClick={() => setShowCreateForm(false)}>
                Cancelar
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

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
