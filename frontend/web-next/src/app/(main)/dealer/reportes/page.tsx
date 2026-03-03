/**
 * Dealer Reports Page
 *
 * Generate and view dealer performance reports with real API integration
 */

'use client';

import { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import {
  FileText,
  Download,
  Calendar,
  TrendingUp,
  TrendingDown,
  DollarSign,
  Car,
  Users,
  Eye,
  BarChart3,
  PieChart,
  AlertCircle,
  Loader2,
} from 'lucide-react';
import { toast } from 'sonner';
import { useCurrentDealer, useDealerStats } from '@/hooks/use-dealers';
import { useTrends, useExportReport, useMonthlyReport } from '@/hooks/use-dealer-analytics';
import { PlanGate } from '@/components/plan/plan-gate';

// =============================================================================
// HELPERS
// =============================================================================

const formatPrice = (price: number) => {
  return new Intl.NumberFormat('es-DO', {
    style: 'currency',
    currency: 'DOP',
    maximumFractionDigits: 0,
  }).format(price);
};

const formatNumber = (num: number) => {
  return new Intl.NumberFormat('es-DO').format(num);
};

const getChangeIndicator = (change: number) => {
  if (change > 0) {
    return (
      <span className="text-primary flex items-center text-sm">
        <TrendingUp className="mr-1 h-4 w-4" />+{change.toFixed(1)}%
      </span>
    );
  } else if (change < 0) {
    return (
      <span className="flex items-center text-sm text-red-600">
        <TrendingDown className="mr-1 h-4 w-4" />
        {change.toFixed(1)}%
      </span>
    );
  }
  return <span className="text-muted-foreground text-sm">0%</span>;
};

// Get current period name
const getCurrentPeriodName = () => {
  const now = new Date();
  return now.toLocaleDateString('es-DO', { month: 'long', year: 'numeric' });
};

// =============================================================================
// SKELETON COMPONENTS
// =============================================================================

function ReportsSkeleton() {
  return (
    <div className="space-y-6">
      <div className="flex flex-col justify-between gap-4 sm:flex-row">
        <div>
          <Skeleton className="mb-2 h-8 w-48" />
          <Skeleton className="h-5 w-64" />
        </div>
        <div className="flex gap-2">
          <Skeleton className="h-10 w-32" />
          <Skeleton className="h-10 w-36" />
        </div>
      </div>
      <Card className="bg-primary">
        <CardContent className="p-6">
          <Skeleton className="bg-primary/100 h-12 w-64" />
        </CardContent>
      </Card>
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        {[1, 2, 3, 4].map(i => (
          <Card key={i}>
            <CardContent className="p-4">
              <Skeleton className="mb-2 h-5 w-5" />
              <Skeleton className="mb-1 h-8 w-24" />
              <Skeleton className="h-4 w-20" />
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}

// =============================================================================
// MAIN PAGE COMPONENT
// =============================================================================

function DealerReportsContent() {
  const { data: dealer, isLoading: isDealerLoading } = useCurrentDealer();
  const { data: stats, isLoading: isStatsLoading } = useDealerStats(dealer?.id);
  const [selectedPeriod, setSelectedPeriod] = useState('month');
  const dealerId = dealer?.id || '';

  const exportMutation = useExportReport(dealerId);
  const { data: viewsTrend, isError: isTrendsError } = useTrends(dealerId, 'views');
  const { isError: isReportError } = useMonthlyReport(dealerId);

  const isLoading = isDealerLoading || isStatsLoading;
  const hasAnalyticsError = isTrendsError || isReportError;

  const handleExportPDF = async () => {
    try {
      await exportMutation.mutateAsync('pdf');
      toast.success('Reporte exportado correctamente');
    } catch {
      toast.error('Error al exportar el reporte');
    }
  };

  if (isLoading) {
    return <ReportsSkeleton />;
  }

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

  // Calculate derived stats
  const conversionRate =
    stats?.totalViews && stats.totalInquiries ? (stats.totalInquiries / stats.totalViews) * 100 : 0;

  return (
    <div className="space-y-6">
      {/* Analytics Error Banner */}
      {hasAnalyticsError && (
        <Card className="border-red-200 bg-red-50">
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <AlertCircle className="h-6 w-6 text-red-600" />
              <div className="flex-1">
                <p className="font-medium text-red-800">
                  No se pudieron cargar algunos datos de analítica
                </p>
                <p className="text-sm text-red-600">
                  El servicio de analítica podría no estar disponible. Los datos básicos del dealer
                  se muestran a continuación.
                </p>
              </div>
              <Button
                variant="outline"
                className="border-red-300"
                onClick={() => window.location.reload()}
              >
                Reintentar
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Header */}
      <div className="flex flex-col justify-between gap-4 sm:flex-row">
        <div>
          <h1 className="text-foreground text-2xl font-bold">Reportes</h1>
          <p className="text-muted-foreground">Análisis de rendimiento y estadísticas</p>
        </div>
        <div className="flex gap-2">
          <select
            className="rounded-md border px-3 py-2 text-sm"
            value={selectedPeriod}
            onChange={e => setSelectedPeriod(e.target.value)}
          >
            <option value="month">Este Mes</option>
            <option value="quarter">Este Trimestre</option>
            <option value="year">Este Año</option>
            <option value="custom">Personalizado</option>
          </select>
          <Button
            className="bg-primary hover:bg-primary/90"
            onClick={handleExportPDF}
            disabled={exportMutation.isPending}
          >
            {exportMutation.isPending ? (
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <Download className="mr-2 h-4 w-4" />
            )}
            Exportar PDF
          </Button>
        </div>
      </div>

      {/* Period Indicator */}
      <Card className="from-primary to-primary/90 bg-gradient-to-r text-white">
        <CardContent className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-primary-foreground">Reporte del Período</p>
              <h2 className="text-2xl font-bold capitalize">{getCurrentPeriodName()}</h2>
            </div>
            <Calendar className="h-10 w-10 opacity-50" />
          </div>
        </CardContent>
      </Card>

      {/* Summary Stats */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <Card>
          <CardContent className="p-4">
            <div className="mb-2 flex items-center justify-between">
              <Eye className="h-5 w-5 text-blue-600" />
              {getChangeIndicator(stats?.viewsChange || 0)}
            </div>
            <p className="text-2xl font-bold">{formatNumber(stats?.totalViews || 0)}</p>
            <p className="text-muted-foreground text-sm">Vistas Totales</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="mb-2 flex items-center justify-between">
              <Users className="h-5 w-5 text-purple-600" />
              {getChangeIndicator(stats?.inquiriesChange || 0)}
            </div>
            <p className="text-2xl font-bold">{formatNumber(stats?.totalInquiries || 0)}</p>
            <p className="text-muted-foreground text-sm">Leads Generados</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="mb-2 flex items-center justify-between">
              <Car className="h-5 w-5 text-amber-600" />
              <span className="text-muted-foreground text-sm">—</span>
            </div>
            <p className="text-2xl font-bold">{stats?.activeListings || 0}</p>
            <p className="text-muted-foreground text-sm">Vehículos Activos</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="mb-2 flex items-center justify-between">
              <DollarSign className="text-primary h-5 w-5" />
              {getChangeIndicator(stats?.revenueChange || 0)}
            </div>
            <p className="text-2xl font-bold">
              {formatPrice(stats?.totalRevenue || stats?.revenueThisMonth || 0)}
            </p>
            <p className="text-muted-foreground text-sm">Ingresos</p>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Chart Placeholder */}
        <div className="lg:col-span-2">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <BarChart3 className="h-5 w-5" />
                Rendimiento por Semana
              </CardTitle>
            </CardHeader>
            <CardContent>
              {Array.isArray(viewsTrend) && viewsTrend.length > 0 ? (
                <div className="flex h-64 items-end gap-1">
                  {viewsTrend
                    .slice(-12)
                    .map((point: { value: number; label?: string; date?: string }, idx: number) => {
                      const maxVal = Math.max(
                        ...viewsTrend.map((p: { value: number }) => p.value),
                        1
                      );
                      return (
                        <div key={idx} className="flex flex-1 flex-col items-center">
                          <div
                            className="w-full rounded-t bg-blue-500"
                            style={{ height: `${(point.value / maxVal) * 200}px` }}
                            title={`${point.label || ''}: ${point.value}`}
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
                    })}
                </div>
              ) : (
                <div className="bg-muted/50 flex h-64 items-center justify-center rounded-lg">
                  <div className="text-muted-foreground text-center">
                    <BarChart3 className="mx-auto mb-2 h-12 w-12 opacity-50" />
                    <p>No hay datos de tendencia disponibles</p>
                    <p className="text-sm">Los datos aparecerán cuando haya actividad</p>
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        </div>

        {/* Conversion Funnel */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <PieChart className="h-5 w-5" />
              Embudo de Conversión
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-3">
              <div>
                <div className="mb-1 flex justify-between text-sm">
                  <span>Vistas</span>
                  <span className="font-medium">{formatNumber(stats?.totalViews || 0)}</span>
                </div>
                <div className="bg-muted h-3 overflow-hidden rounded-full">
                  <div className="h-full w-full rounded-full bg-blue-500" />
                </div>
              </div>
              <div>
                <div className="mb-1 flex justify-between text-sm">
                  <span>Contactos</span>
                  <span className="font-medium">
                    {formatNumber(stats?.totalInquiries || 0)} ({conversionRate.toFixed(2)}%)
                  </span>
                </div>
                <div className="bg-muted h-3 overflow-hidden rounded-full">
                  <div
                    className="h-full rounded-full bg-purple-500"
                    style={{ width: `${Math.min(100, conversionRate * 10)}%` }}
                  />
                </div>
              </div>
              <div>
                <div className="mb-1 flex justify-between text-sm">
                  <span>Respondidos</span>
                  <span className="font-medium">
                    {stats?.responseRate ? `${stats.responseRate.toFixed(0)}%` : '—'}
                  </span>
                </div>
                <div className="bg-muted h-3 overflow-hidden rounded-full">
                  <div
                    className="h-full rounded-full bg-amber-500"
                    style={{ width: `${stats?.responseRate || 0}%` }}
                  />
                </div>
              </div>
            </div>
            <div className="border-border border-t pt-4">
              <p className="text-muted-foreground text-sm">
                <strong>Tasa de respuesta:</strong>{' '}
                {stats?.responseRate ? `${stats.responseRate.toFixed(1)}%` : 'N/A'}
              </p>
              {stats?.avgResponseTimeMinutes && (
                <p className="text-muted-foreground mt-1 text-sm">
                  <strong>Tiempo de respuesta promedio:</strong>{' '}
                  {stats.avgResponseTimeMinutes < 60
                    ? `${stats.avgResponseTimeMinutes} min`
                    : `${Math.round(stats.avgResponseTimeMinutes / 60)}h`}
                </p>
              )}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Performance Summary */}
      <Card>
        <CardHeader>
          <CardTitle>Resumen de Rendimiento</CardTitle>
          <CardDescription>Métricas clave de tu concesionario</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid gap-6 md:grid-cols-3">
            <div className="rounded-lg bg-blue-50 p-4 text-center">
              <Eye className="mx-auto mb-2 h-8 w-8 text-blue-600" />
              <p className="text-3xl font-bold text-blue-700">
                {formatNumber(stats?.viewsThisMonth || 0)}
              </p>
              <p className="text-sm text-blue-600">Vistas Este Mes</p>
            </div>
            <div className="rounded-lg bg-purple-50 p-4 text-center">
              <Users className="mx-auto mb-2 h-8 w-8 text-purple-600" />
              <p className="text-3xl font-bold text-purple-700">
                {formatNumber(stats?.inquiriesThisMonth || 0)}
              </p>
              <p className="text-sm text-purple-600">Leads Este Mes</p>
            </div>
            <div className="bg-primary/10 rounded-lg p-4 text-center">
              <DollarSign className="text-primary mx-auto mb-2 h-8 w-8" />
              <p className="text-primary text-3xl font-bold">
                {formatPrice(stats?.revenueThisMonth || 0)}
              </p>
              <p className="text-primary text-sm">Ingresos Este Mes</p>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Pending Inquiries Alert */}
      {stats?.pendingInquiries && stats.pendingInquiries > 0 && (
        <Card className="border-amber-200 bg-amber-50">
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <AlertCircle className="h-6 w-6 text-amber-600" />
              <div className="flex-1">
                <p className="font-medium text-amber-800">
                  Tienes {stats.pendingInquiries} consultas sin responder
                </p>
                <p className="text-sm text-amber-600">
                  Responder rápido mejora tu tasa de conversión y ranking
                </p>
              </div>
              <Button variant="outline" className="border-amber-300" asChild>
                <a href="/dealer/mensajes">Ver Mensajes</a>
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Previous Reports */}
      <Card>
        <CardHeader>
          <CardTitle>Reportes Anteriores</CardTitle>
          <CardDescription>Descarga reportes de períodos anteriores</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            {[
              {
                id: '1',
                name: `Reporte ${getMonthName(-1)}`,
                date: getMonthDate(-1),
                type: 'monthly',
              },
              {
                id: '2',
                name: `Reporte ${getMonthName(-2)}`,
                date: getMonthDate(-2),
                type: 'monthly',
              },
              {
                id: '3',
                name: `Reporte ${getMonthName(-3)}`,
                date: getMonthDate(-3),
                type: 'monthly',
              },
            ].map(report => (
              <div
                key={report.id}
                className="hover:bg-muted/50 flex items-center justify-between rounded-lg border p-3"
              >
                <div className="flex items-center gap-3">
                  <div className="bg-muted rounded-lg p-2">
                    <FileText className="text-muted-foreground h-5 w-5" />
                  </div>
                  <div>
                    <p className="font-medium">{report.name}</p>
                    <p className="text-muted-foreground text-sm">
                      Generado: {new Date(report.date).toLocaleDateString('es-DO')}
                    </p>
                  </div>
                </div>
                <div className="flex items-center gap-3">
                  <Badge variant="outline">
                    {report.type === 'monthly' ? 'Mensual' : 'Trimestral'}
                  </Badge>
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={async () => {
                      try {
                        await exportMutation.mutateAsync('pdf');
                        toast.success('Reporte descargado');
                      } catch {
                        toast.error('Error al descargar');
                      }
                    }}
                    disabled={exportMutation.isPending}
                  >
                    {exportMutation.isPending ? (
                      <Loader2 className="h-4 w-4 animate-spin" />
                    ) : (
                      <Download className="h-4 w-4" />
                    )}
                  </Button>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

// Helper functions for report dates
function getMonthName(offset: number): string {
  const date = new Date();
  date.setMonth(date.getMonth() + offset);
  return date.toLocaleDateString('es-DO', { month: 'long', year: 'numeric' });
}

function getMonthDate(offset: number): string {
  const date = new Date();
  date.setMonth(date.getMonth() + offset);
  date.setDate(1);
  return date.toISOString();
}

export default function DealerReportsPage() {
  return (
    <PlanGate feature="detailedStats">
      <DealerReportsContent />
    </PlanGate>
  );
}
