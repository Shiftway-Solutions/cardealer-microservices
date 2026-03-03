/**
 * Dealer Sales Analytics Page
 *
 * Sales performance and revenue analytics — connected to DealerAnalyticsService
 */

'use client';

import { useMemo, useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
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
  ArrowLeft,
  DollarSign,
  TrendingUp,
  TrendingDown,
  Car,
  Target,
  Clock,
  Download,
  LineChart,
  Users,
  AlertCircle,
  Loader2,
} from 'lucide-react';
import Link from 'next/link';
import { toast } from 'sonner';
import { useCurrentDealer } from '@/hooks/use-dealers';
import {
  useAnalyticsOverview,
  useInventoryPerformance,
  useExportReport,
} from '@/hooks/use-dealer-analytics';
import type { VehiclePerformance } from '@/services/dealer-analytics';
import { PlanGate } from '@/components/plan/plan-gate';

// =============================================================================
// HELPERS
// =============================================================================

function formatPrice(value: number): string {
  return new Intl.NumberFormat('es-DO', {
    style: 'currency',
    currency: 'DOP',
    maximumFractionDigits: 0,
  }).format(value);
}

function getDateRange(period: string): { fromDate: string; toDate: string } {
  const now = new Date();
  const toDate = now.toISOString();
  const from = new Date(now);

  switch (period) {
    case 'week':
      from.setDate(from.getDate() - 7);
      break;
    case 'quarter':
      from.setMonth(from.getMonth() - 3);
      break;
    case 'year':
      from.setFullYear(from.getFullYear() - 1);
      break;
    default: // month
      from.setMonth(from.getMonth() - 1);
  }

  return { fromDate: from.toISOString(), toDate };
}

// =============================================================================
// SKELETON
// =============================================================================

function SalesAnalyticsSkeleton() {
  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Skeleton className="h-10 w-10" />
        <div>
          <Skeleton className="mb-2 h-7 w-48" />
          <Skeleton className="h-5 w-64" />
        </div>
      </div>
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        {[1, 2, 3, 4].map(i => (
          <Card key={i}>
            <CardContent className="pt-6">
              <Skeleton className="mb-2 h-10 w-10" />
              <Skeleton className="mb-1 h-8 w-24" />
              <Skeleton className="h-4 w-16" />
            </CardContent>
          </Card>
        ))}
      </div>
      <Card>
        <CardContent className="py-6">
          <Skeleton className="h-8 w-full" />
        </CardContent>
      </Card>
    </div>
  );
}

// =============================================================================
// MAIN PAGE
// =============================================================================

function SalesAnalyticsContent() {
  const [period, setPeriod] = useState('month');
  const { data: dealer, isLoading: isDealerLoading } = useCurrentDealer();
  const dealerId = dealer?.id || '';

  const dateRange = useMemo(() => getDateRange(period), [period]);

  const { data: overview, isLoading: isOverviewLoading } = useAnalyticsOverview(
    dealerId,
    dateRange
  );

  const { data: topPerformers, isLoading: isPerformersLoading } = useInventoryPerformance(
    dealerId,
    { sortBy: 'views', limit: 5 }
  );

  const exportMutation = useExportReport(dealerId);

  const isLoading = isDealerLoading || isOverviewLoading;

  const handleExport = async () => {
    try {
      await exportMutation.mutateAsync('pdf');
      toast.success('Reporte exportado correctamente');
    } catch {
      toast.error('Error al exportar el reporte');
    }
  };

  if (isLoading) return <SalesAnalyticsSkeleton />;

  if (!dealer) {
    return (
      <div className="flex min-h-[400px] items-center justify-center">
        <Card className="w-full max-w-md p-6 text-center">
          <CardContent>
            <AlertCircle className="mx-auto mb-4 h-12 w-12 text-amber-500" />
            <h2 className="mb-2 text-xl font-semibold">No se encontró el dealer</h2>
            <p className="text-muted-foreground">Por favor, inicia sesión como dealer.</p>
          </CardContent>
        </Card>
      </div>
    );
  }

  const kpis = overview?.kpis;
  const snapshot = overview?.currentSnapshot;
  const revenueTrend = overview?.revenueTrend || [];
  const topVehicles = overview?.topPerformers || topPerformers || [];

  // Sales target approximation
  const vehiclesSold = kpis?.totalSales || 0;
  const monthlyTarget = 15;
  const monthlyProgress = Math.min(100, Math.round((vehiclesSold / monthlyTarget) * 100));

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Link href="/dealer/analytics">
            <Button variant="ghost" size="icon">
              <ArrowLeft className="h-5 w-5" />
            </Button>
          </Link>
          <div>
            <h1 className="text-2xl font-bold">Analytics de Ventas</h1>
            <p className="text-muted-foreground">Rendimiento de ventas y métricas clave</p>
          </div>
        </div>
        <div className="flex gap-2">
          <Select value={period} onValueChange={setPeriod}>
            <SelectTrigger className="w-40">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="week">Esta semana</SelectItem>
              <SelectItem value="month">Este mes</SelectItem>
              <SelectItem value="quarter">Este trimestre</SelectItem>
              <SelectItem value="year">Este año</SelectItem>
            </SelectContent>
          </Select>
          <Button variant="outline" onClick={handleExport} disabled={exportMutation.isPending}>
            {exportMutation.isPending ? (
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <Download className="mr-2 h-4 w-4" />
            )}
            Exportar
          </Button>
        </div>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-primary/10 p-2">
                <DollarSign className="h-5 w-5 text-primary" />
              </div>
              <div>
                <p className="text-muted-foreground text-sm">Ingresos</p>
                <p className="text-2xl font-bold">{formatPrice(kpis?.totalRevenue || 0)}</p>
                <div className="flex items-center text-xs">
                  {(kpis?.revenueChange ?? 0) >= 0 ? (
                    <span className="flex items-center text-primary">
                      <TrendingUp className="mr-1 h-3 w-3" />+
                      {(kpis?.revenueChange ?? 0).toFixed(1)}%
                    </span>
                  ) : (
                    <span className="flex items-center text-red-600">
                      <TrendingDown className="mr-1 h-3 w-3" />
                      {(kpis?.revenueChange ?? 0).toFixed(1)}%
                    </span>
                  )}
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-blue-100 p-2">
                <Car className="h-5 w-5 text-blue-600" />
              </div>
              <div>
                <p className="text-muted-foreground text-sm">Vendidos</p>
                <p className="text-2xl font-bold">{vehiclesSold}</p>
                <p className="text-muted-foreground text-xs">Meta: {monthlyTarget}</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-purple-100 p-2">
                <Target className="h-5 w-5 text-purple-600" />
              </div>
              <div>
                <p className="text-muted-foreground text-sm">Conversión</p>
                <p className="text-2xl font-bold">{(kpis?.conversionRate ?? 0).toFixed(1)}%</p>
                <p className="text-muted-foreground text-xs">leads → ventas</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-orange-100 p-2">
                <Clock className="h-5 w-5 text-orange-600" />
              </div>
              <div>
                <p className="text-muted-foreground text-sm">Tiempo Venta</p>
                <p className="text-2xl font-bold">{Math.round(snapshot?.avgDaysOnMarket || 0)}</p>
                <p className="text-muted-foreground text-xs">días promedio</p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Monthly Target Progress */}
      <Card>
        <CardContent className="py-6">
          <div className="mb-4 flex items-center justify-between">
            <div>
              <h3 className="font-semibold">Meta Mensual</h3>
              <p className="text-muted-foreground text-sm">
                {vehiclesSold} de {monthlyTarget} vehículos
              </p>
            </div>
            <Badge
              className={
                monthlyProgress >= 100
                  ? 'bg-primary/10 text-primary'
                  : 'bg-yellow-100 text-yellow-700'
              }
            >
              {monthlyProgress}%
            </Badge>
          </div>
          <div className="bg-muted h-4 w-full rounded-full">
            <div
              className="h-4 rounded-full bg-primary/100 transition-all"
              style={{ width: `${monthlyProgress}%` }}
            />
          </div>
        </CardContent>
      </Card>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        {/* Top Performing Vehicles */}
        <Card>
          <CardHeader>
            <CardTitle>Vehículos Mejor Rendimiento</CardTitle>
            <CardDescription>Top vehículos por vistas y engagement</CardDescription>
          </CardHeader>
          <CardContent>
            {isPerformersLoading ? (
              <div className="space-y-4">
                {[1, 2, 3].map(i => (
                  <Skeleton key={i} className="h-16 w-full" />
                ))}
              </div>
            ) : topVehicles.length > 0 ? (
              <div className="space-y-4">
                {topVehicles.slice(0, 5).map((vehicle: VehiclePerformance, idx: number) => (
                  <div
                    key={vehicle.id || idx}
                    className="bg-muted/50 flex items-center justify-between rounded-lg p-3"
                  >
                    <div className="flex items-center gap-3">
                      <div className="rounded-lg bg-primary/10 p-2">
                        <Car className="h-4 w-4 text-primary" />
                      </div>
                      <div>
                        <p className="text-sm font-medium">
                          {vehicle.vehicleTitle ||
                            `${vehicle.vehicleMake} ${vehicle.vehicleModel} ${vehicle.vehicleYear}`}
                        </p>
                        <p className="text-muted-foreground text-xs">
                          {vehicle.views} vistas • {vehicle.contacts} contactos
                        </p>
                      </div>
                    </div>
                    <div className="text-right">
                      <p className="font-bold text-primary">
                        {formatPrice(vehicle.vehiclePrice || 0)}
                      </p>
                      <p className="text-muted-foreground text-xs">{vehicle.daysOnMarket} días</p>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <p className="text-muted-foreground py-8 text-center">
                No hay datos de rendimiento disponibles
              </p>
            )}
          </CardContent>
        </Card>

        {/* Engagement Metrics */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Users className="h-5 w-5" />
              Métricas de Engagement
            </CardTitle>
            <CardDescription>Métricas de contacto por canal</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {[
                {
                  name: 'Vistas Totales',
                  value: kpis?.totalViews || 0,
                  change: kpis?.viewsChange || 0,
                },
                {
                  name: 'Contactos',
                  value: kpis?.totalContacts || 0,
                  change: kpis?.contactsChange || 0,
                },
                {
                  name: 'Leads',
                  value: kpis?.totalLeads || 0,
                  change: kpis?.leadsChange || 0,
                },
              ].map(source => (
                <div key={source.name} className="bg-muted/50 rounded-lg p-4">
                  <div className="mb-2 flex items-center justify-between">
                    <span className="font-medium">{source.name}</span>
                    <Badge variant="outline">
                      {source.change >= 0 ? '+' : ''}
                      {source.change.toFixed(1)}%
                    </Badge>
                  </div>
                  <p className="text-2xl font-bold">{source.value.toLocaleString('es-DO')}</p>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Inventory Summary from snapshot */}
      {snapshot && (
        <Card>
          <CardHeader>
            <CardTitle>Resumen de Inventario</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 gap-4 md:grid-cols-4">
              <div className="bg-muted/50 rounded-lg p-4 text-center">
                <p className="text-muted-foreground text-sm">Activos</p>
                <p className="mt-1 text-2xl font-bold">{snapshot.activeVehicles}</p>
                <p className="text-sm text-blue-600">vehículos</p>
              </div>
              <div className="bg-muted/50 rounded-lg p-4 text-center">
                <p className="text-muted-foreground text-sm">Vendidos</p>
                <p className="mt-1 text-2xl font-bold">{snapshot.soldVehicles}</p>
                <p className="text-sm text-primary">este período</p>
              </div>
              <div className="bg-muted/50 rounded-lg p-4 text-center">
                <p className="text-muted-foreground text-sm">Valor Inventario</p>
                <p className="mt-1 text-2xl font-bold">
                  {formatPrice(snapshot.totalInventoryValue)}
                </p>
              </div>
              <div className="bg-muted/50 rounded-lg p-4 text-center">
                <p className="text-muted-foreground text-sm">Precio Promedio</p>
                <p className="mt-1 text-2xl font-bold">{formatPrice(snapshot.avgVehiclePrice)}</p>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Revenue Trend Chart */}
      <Card>
        <CardHeader>
          <CardTitle>Tendencia de Ingresos</CardTitle>
          <CardDescription>Evolución de ingresos en el período</CardDescription>
        </CardHeader>
        <CardContent>
          {revenueTrend.length > 0 ? (
            <div className="flex h-64 items-end gap-1">
              {revenueTrend.map(
                (point: { value: number; label?: string; date?: string }, idx: number) => {
                  const maxVal = Math.max(
                    ...revenueTrend.map((p: { value: number }) => p.value),
                    1
                  );
                  return (
                    <div key={idx} className="flex flex-1 flex-col items-center">
                      <div
                        className="w-full rounded-t bg-primary/100"
                        style={{ height: `${(point.value / maxVal) * 200}px` }}
                        title={`${point.label}: ${formatPrice(point.value)}`}
                      />
                      <span className="text-muted-foreground mt-2 truncate text-xs">
                        {point.label ||
                          (point.date
                            ? new Date(point.date).toLocaleDateString('es-DO', {
                                day: '2-digit',
                                month: 'short',
                              })
                            : '')}
                      </span>
                    </div>
                  );
                }
              )}
            </div>
          ) : (
            <div className="bg-muted/50 flex h-64 items-center justify-center rounded-lg">
              <div className="text-muted-foreground text-center">
                <LineChart className="mx-auto mb-2 h-12 w-12" />
                <p>No hay datos de tendencia disponibles</p>
                <p className="text-sm">Los datos aparecerán cuando haya actividad</p>
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

export default function SalesAnalyticsPage() {
  return (
    <PlanGate feature="analytics">
      <SalesAnalyticsContent />
    </PlanGate>
  );
}
