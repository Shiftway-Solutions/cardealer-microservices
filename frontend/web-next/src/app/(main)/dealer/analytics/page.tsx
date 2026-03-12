/**
 * Dealer Analytics Page
 *
 * Analytics and reporting dashboard for dealers
 * Connected to real APIs — February 2026
 */

'use client';

import * as React from 'react';
import Link from 'next/link';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  BarChart3,
  TrendingUp,
  Eye,
  Users,
  Car,
  DollarSign,
  Calendar,
  Download,
  ArrowUpRight,
  ArrowDownRight,
  RefreshCw,
  AlertCircle,
} from 'lucide-react';
import { useCurrentDealer, useDealerStats } from '@/hooks/use-dealers';
import { useVehiclesByDealer } from '@/hooks/use-vehicles';
import { useEngagement, useTrends, useExportReport } from '@/hooks/use-dealer-analytics';
import { PlanGate } from '@/components/plan/plan-gate';
import { VideoHelpButton } from '@/components/dealer/video-help-button';

// Skeleton for stats loading
function StatsSkeleton() {
  return (
    <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
      {[1, 2, 3, 4].map(i => (
        <Card key={i}>
          <CardContent className="p-4">
            <Skeleton className="mb-2 h-5 w-5" />
            <Skeleton className="mb-1 h-8 w-24" />
            <Skeleton className="h-3 w-16" />
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

const PERIOD_OPTIONS = [
  { value: '7d', label: 'Últimos 7 días', days: 7 },
  { value: '30d', label: 'Últimos 30 días', days: 30 },
  { value: '90d', label: 'Últimos 90 días', days: 90 },
  { value: '1y', label: 'Este año', days: 365 },
] as const;

function getDateRange(periodValue: string): { fromDate: string; toDate: string } {
  const now = new Date();
  const toDate = now.toISOString();
  const from = new Date(now);
  const opt = PERIOD_OPTIONS.find(p => p.value === periodValue);
  from.setDate(from.getDate() - (opt?.days ?? 30));
  return { fromDate: from.toISOString(), toDate };
}

function AnalyticsPageContent() {
  const [period, setPeriod] = React.useState('30d');
  const dateRange = React.useMemo(() => getDateRange(period), [period]);

  // Get current dealer
  const { data: dealer, isLoading: isDealerLoading } = useCurrentDealer();

  // Get dealer stats from API
  const {
    data: stats,
    isLoading: isStatsLoading,
    error: statsError,
    refetch,
  } = useDealerStats(dealer?.id);

  // Get dealer vehicles for top performers
  const { data: vehiclesData } = useVehiclesByDealer(dealer?.id || '');

  // Get engagement data for traffic sources
  const { data: engagement } = useEngagement(dealer?.id || '', dateRange);

  // Get views trend for chart — filtered by selected period
  const { data: viewsTrend } = useTrends(dealer?.id || '', 'views', dateRange);
  const { data: leadsTrend } = useTrends(dealer?.id || '', 'leads', dateRange);

  // Export mutation
  const exportMutation = useExportReport(dealer?.id || '');

  // Build traffic sources from real engagement data
  const trafficSources = React.useMemo(() => {
    if (!engagement?.topReferrers || engagement.topReferrers.length === 0) {
      return [];
    }
    return engagement.topReferrers.map(r => ({
      source: r.source,
      visits: r.count,
      percentage: r.percentage,
    }));
  }, [engagement]);

  // Build weekly data from real trends
  const weeklyData = React.useMemo(() => {
    const dayNames = ['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb'];
    const viewsPoints = viewsTrend?.slice(-7) || [];
    const leadsPoints = leadsTrend?.slice(-7) || [];

    if (viewsPoints.length === 0) return [];

    return viewsPoints.map((vp, i) => {
      const date = new Date(vp.date);
      return {
        day: dayNames[date.getDay()] || vp.label,
        views: vp.value,
        leads: leadsPoints[i]?.value || 0,
      };
    });
  }, [viewsTrend, leadsTrend]);

  // Build top vehicles from real data sorted by viewCount
  const topVehicles = React.useMemo(() => {
    const items = vehiclesData?.items || [];
    return items
      .filter(v => (v.viewCount || 0) > 0)
      .sort((a, b) => (b.viewCount || 0) - (a.viewCount || 0))
      .slice(0, 5)
      .map(v => ({
        title: `${v.make} ${v.model} ${v.year}`,
        views: v.viewCount || 0,
        leads: 0,
        conversion: 0,
      }));
  }, [vehiclesData]);

  // Format currency with compact notation for dashboard display
  const formatCurrencyCompact = (value: number) => {
    if (value >= 1000000) {
      return `RD$ ${(value / 1000000).toFixed(1)}M`;
    }
    return `RD$ ${value.toLocaleString()}`;
  };

  // Format number with K suffix
  const formatNumber = (value: number) => {
    if (value >= 1000) {
      return `${(value / 1000).toFixed(1)}K`;
    }
    return value.toString();
  };

  // Build overview stats from API data
  const overviewStats = stats
    ? [
        {
          label: 'Vistas Totales',
          value: formatNumber(stats.viewsThisMonth),
          change: stats.viewsChange
            ? `${stats.viewsChange > 0 ? '+' : ''}${stats.viewsChange.toFixed(1)}%`
            : '+0%',
          trend: (stats.viewsChange || 0) >= 0 ? 'up' : 'down',
          icon: Eye,
          period: 'vs mes anterior',
        },
        {
          label: 'Personas interesadas',
          value: stats.inquiriesThisMonth.toString(),
          change: stats.inquiriesChange
            ? `${stats.inquiriesChange > 0 ? '+' : ''}${stats.inquiriesChange.toFixed(1)}%`
            : '+0%',
          trend: (stats.inquiriesChange || 0) >= 0 ? 'up' : 'down',
          icon: Users,
          period: 'vs mes anterior',
        },
        {
          label: 'Tasa de Respuesta',
          value: `${stats.responseRate.toFixed(1)}%`,
          change: stats.responseRate >= 80 ? '+Buena' : '-Mejorar',
          trend: stats.responseRate >= 80 ? 'up' : 'down',
          icon: TrendingUp,
          period: 'respuestas a consultas',
        },
        {
          label: 'Ingresos Mes',
          value: formatCurrencyCompact(stats.revenueThisMonth),
          change: stats.revenueChange
            ? `${stats.revenueChange > 0 ? '+' : ''}${stats.revenueChange.toFixed(1)}%`
            : '+0%',
          trend: (stats.revenueChange || 0) >= 0 ? 'up' : 'down',
          icon: DollarSign,
          period: 'vs mes anterior',
        },
      ]
    : [];

  const isLoading = isDealerLoading || isStatsLoading;

  // Error state
  if (statsError) {
    return (
      <div className="space-y-6">
        <div className="flex flex-col justify-between gap-4 sm:flex-row">
          <div>
            <h1 className="text-foreground text-2xl font-bold">¿Cómo va tu negocio?</h1>
            <p className="text-muted-foreground">
              Aquí ves cómo están funcionando tus publicaciones
            </p>
          </div>
        </div>
        <Card className="border-red-200 bg-red-50">
          <CardContent className="flex flex-col items-center justify-center gap-4 p-8">
            <AlertCircle className="h-12 w-12 text-red-500" />
            <p className="text-center text-red-700">
              Error al cargar las estadísticas. Por favor intenta de nuevo.
            </p>
            <Button variant="outline" onClick={() => refetch()}>
              <RefreshCw className="mr-2 h-4 w-4" />
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
          <div className="flex items-center gap-2">
            <h1 className="text-foreground text-2xl font-bold">¿Cómo va tu negocio?</h1>
            <VideoHelpButton sectionKey="analytics" variant="icon" />
          </div>
          <p className="text-muted-foreground">Aquí ves cómo están funcionando tus publicaciones</p>
        </div>
        <div className="flex gap-3">
          <Button variant="outline" onClick={() => refetch()}>
            <RefreshCw className="mr-2 h-4 w-4" />
            Actualizar
          </Button>
          <Select value={period} onValueChange={setPeriod}>
            <SelectTrigger className="w-44">
              <Calendar className="mr-2 h-4 w-4" />
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {PERIOD_OPTIONS.map(opt => (
                <SelectItem key={opt.value} value={opt.value}>
                  {opt.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Button
            variant="outline"
            onClick={() => exportMutation.mutate('pdf')}
            disabled={exportMutation.isPending}
          >
            <Download className="mr-2 h-4 w-4" />
            {exportMutation.isPending ? 'Exportando...' : 'Exportar'}
          </Button>
        </div>
      </div>

      {/* Overview Stats */}
      {isLoading ? (
        <StatsSkeleton />
      ) : (
        <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
          {overviewStats.map(stat => {
            const Icon = stat.icon;
            const TrendIcon = stat.trend === 'up' ? ArrowUpRight : ArrowDownRight;
            return (
              <Card key={stat.label}>
                <CardContent className="p-4">
                  <div className="mb-2 flex items-center justify-between">
                    <Icon className="text-muted-foreground h-5 w-5" />
                    <div
                      className={`flex items-center text-sm ${
                        stat.trend === 'up' ? 'text-primary' : 'text-red-600'
                      }`}
                    >
                      <TrendIcon className="h-4 w-4" />
                      {stat.change}
                    </div>
                  </div>
                  <p className="text-2xl font-bold">{stat.value}</p>
                  <p className="text-muted-foreground text-xs">{stat.period}</p>
                </CardContent>
              </Card>
            );
          })}
        </div>
      )}

      {/* Tabs */}
      <Tabs defaultValue="overview" className="space-y-4">
        <TabsList>
          <TabsTrigger value="overview">Resumen</TabsTrigger>
          <TabsTrigger value="vehicles">Vehículos</TabsTrigger>
          <TabsTrigger value="traffic">Tráfico</TabsTrigger>
          <TabsTrigger value="leads">Interesados</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-6">
          <div className="grid gap-6 lg:grid-cols-2">
            {/* Weekly Performance Chart */}
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Rendimiento Semanal</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {weeklyData.length > 0 ? (
                    weeklyData.map(day => {
                      const maxViews = Math.max(...weeklyData.map(d => d.views), 1);
                      const maxLeads = Math.max(...weeklyData.map(d => d.leads), 1);
                      return (
                        <div key={day.day} className="flex items-center gap-4">
                          <span className="text-muted-foreground w-8 text-sm font-medium">
                            {day.day}
                          </span>
                          <div className="flex-1">
                            <div className="mb-1 flex items-center gap-2">
                              <div
                                className="bg-primary/100 h-3 rounded-full"
                                style={{ width: `${(day.views / maxViews) * 100}%` }}
                              />
                              <span className="text-muted-foreground text-xs">{day.views}</span>
                            </div>
                            <div className="flex items-center gap-2">
                              <div
                                className="h-2 rounded-full bg-blue-400"
                                style={{ width: `${(day.leads / maxLeads) * 100}%` }}
                              />
                              <span className="text-muted-foreground text-xs">{day.leads}</span>
                            </div>
                          </div>
                        </div>
                      );
                    })
                  ) : (
                    <div className="text-muted-foreground py-6 text-center text-sm">
                      No hay datos de rendimiento semanal disponibles
                    </div>
                  )}
                </div>
                <div className="border-border mt-4 flex items-center justify-center gap-6 border-t pt-4">
                  <div className="flex items-center gap-2">
                    <div className="bg-primary/100 h-3 w-3 rounded-full" />
                    <span className="text-muted-foreground text-sm">Vistas</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <div className="h-2 w-3 rounded-full bg-blue-400" />
                    <span className="text-muted-foreground text-sm">Interesados</span>
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* Traffic Sources */}
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Fuentes de Tráfico</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {trafficSources.length > 0 ? (
                    trafficSources.map(source => (
                      <div key={source.source}>
                        <div className="mb-1 flex items-center justify-between">
                          <span className="text-sm font-medium">{source.source}</span>
                          <span className="text-muted-foreground text-sm">
                            {source.visits.toLocaleString()} ({source.percentage}%)
                          </span>
                        </div>
                        <div className="bg-muted h-2 overflow-hidden rounded-full">
                          <div
                            className="bg-primary/100 h-full rounded-full"
                            style={{ width: `${source.percentage}%` }}
                          />
                        </div>
                      </div>
                    ))
                  ) : (
                    <div className="text-muted-foreground py-6 text-center text-sm">
                      No hay datos de fuentes de tráfico disponibles
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>
          </div>

          {/* Top Vehicles */}
          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle className="text-lg">Top Vehículos</CardTitle>
              <Button variant="ghost" size="sm" asChild>
                <Link href="/dealer/analytics/inventario">Ver todos</Link>
              </Button>
            </CardHeader>
            <CardContent>
              {topVehicles.length > 0 ? (
                <div className="overflow-x-auto">
                  <table className="w-full">
                    <thead>
                      <tr className="border-border border-b">
                        <th className="text-muted-foreground py-3 text-left text-sm font-medium">
                          Vehículo
                        </th>
                        <th className="text-muted-foreground py-3 text-center text-sm font-medium">
                          Vistas
                        </th>
                        <th className="text-muted-foreground py-3 text-center text-sm font-medium">
                          Favoritos
                        </th>
                        <th className="text-muted-foreground py-3 text-center text-sm font-medium">
                          Conversión
                        </th>
                      </tr>
                    </thead>
                    <tbody>
                      {topVehicles.map((vehicle, index) => (
                        <tr key={index} className="border-b last:border-0">
                          <td className="py-3">
                            <div className="flex items-center gap-2">
                              <span className="text-muted-foreground w-5 text-xs">{index + 1}</span>
                              <span className="font-medium">{vehicle.title}</span>
                            </div>
                          </td>
                          <td className="py-3 text-center">{vehicle.views.toLocaleString()}</td>
                          <td className="py-3 text-center">{vehicle.leads}</td>
                          <td className="py-3 text-center">
                            <Badge
                              className={
                                vehicle.conversion >= 3
                                  ? 'bg-primary/10 text-primary'
                                  : vehicle.conversion >= 2
                                    ? 'bg-amber-100 text-amber-700'
                                    : 'bg-muted text-foreground'
                              }
                            >
                              {vehicle.conversion}%
                            </Badge>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                <div className="text-muted-foreground py-8 text-center">
                  <Car className="mx-auto mb-2 h-8 w-8 text-gray-300" />
                  <p className="text-sm">No hay datos de vehículos disponibles</p>
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="vehicles">
          <Card>
            <CardContent className="p-8 text-center">
              <BarChart3 className="mx-auto mb-4 h-12 w-12 text-gray-300" />
              <h3 className="text-foreground mb-2 font-medium">Analytics de Vehículos</h3>
              <p className="text-muted-foreground mb-4">
                Análisis detallado del rendimiento de cada vehículo
              </p>
              <Button variant="outline" asChild>
                <Link href="/dealer/analytics/inventario">Ver Analytics de Inventario</Link>
              </Button>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="traffic">
          <Card>
            <CardContent className="p-8 text-center">
              <Eye className="mx-auto mb-4 h-12 w-12 text-gray-300" />
              <h3 className="text-foreground mb-2 font-medium">Analytics de Tráfico</h3>
              <p className="text-muted-foreground">
                Análisis detallado de fuentes de tráfico y comportamiento
              </p>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="leads">
          <Card>
            <CardContent className="p-8 text-center">
              <Users className="mx-auto mb-4 h-12 w-12 text-gray-300" />
              <h3 className="text-foreground mb-2 font-medium">Personas Interesadas</h3>
              <p className="text-muted-foreground mb-4">
                Análisis de las personas que preguntan por tus vehículos
              </p>
              <Button variant="outline" asChild>
                <Link href="/dealer/analytics/ventas">Ver Analytics de Ventas</Link>
              </Button>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}

export default function AnalyticsPage() {
  return (
    <PlanGate feature="analytics">
      <AnalyticsPageContent />
    </PlanGate>
  );
}
