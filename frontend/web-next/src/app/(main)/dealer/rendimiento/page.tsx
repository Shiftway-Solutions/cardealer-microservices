/**
 * Dealer Performance Page
 *
 * Detailed performance metrics and analytics for dealers
 */

'use client';

import * as React from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import {
  TrendingUp,
  TrendingDown,
  Eye,
  MessageSquare,
  DollarSign,
  Car,
  Clock,
  BarChart3,
  AlertCircle,
  Target,
} from 'lucide-react';
import { useCurrentDealer } from '@/hooks/use-dealers';
import { dealerAnalyticsService } from '@/services/dealer-analytics';
import { PlanGate } from '@/components/plan/plan-gate';

interface PerformanceMetrics {
  overview: {
    totalListings: number;
    activeListings: number;
    soldThisMonth: number;
    totalViews: number;
    totalLeads: number;
    conversionRate: number;
    avgDaysOnMarket: number;
    revenue: number;
  };
  trends: {
    viewsTrend: number;
    leadsTrend: number;
    salesTrend: number;
  };
}

function RendimientoContent() {
  const { data: dealer } = useCurrentDealer();
  const [metrics, setMetrics] = React.useState<PerformanceMetrics | null>(null);
  const [loading, setLoading] = React.useState(true);
  const [error, setError] = React.useState<string | null>(null);
  const [period, setPeriod] = React.useState<'week' | 'month' | 'quarter'>('month');

  React.useEffect(() => {
    if (!dealer?.id) return;

    async function fetchMetrics() {
      try {
        setLoading(true);
        const data = await dealerAnalyticsService.getOverview(dealer!.id);
        setMetrics({
          overview: {
            totalListings: data.kpis?.activeListings ?? 0,
            activeListings: data.kpis?.activeListings ?? 0,
            soldThisMonth: data.kpis?.totalSales ?? 0,
            totalViews: data.kpis?.totalViews ?? 0,
            totalLeads: data.kpis?.totalLeads ?? 0,
            conversionRate: data.kpis?.conversionRate ?? 0,
            avgDaysOnMarket: data.currentSnapshot?.avgDaysOnMarket ?? 0,
            revenue: data.kpis?.totalRevenue ?? 0,
          },
          trends: {
            viewsTrend: data.comparison?.viewsChange ?? 0,
            leadsTrend: data.comparison?.leadsChange ?? 0,
            salesTrend: data.comparison?.salesChange ?? 0,
          },
        });
      } catch (err) {
        console.error('Error fetching performance:', err);
        setError('No se pudieron cargar las métricas de rendimiento.');
      } finally {
        setLoading(false);
      }
    }
    fetchMetrics();
  }, [dealer, period]);

  if (loading) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold">Rendimiento</h1>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {Array.from({ length: 8 }).map((_, i) => (
            <Card key={i}>
              <CardContent className="p-6">
                <Skeleton className="h-20 w-full" />
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex min-h-[300px] items-center justify-center">
        <div className="text-center">
          <AlertCircle className="mx-auto h-12 w-12 text-red-500" />
          <p className="text-muted-foreground mt-4">{error}</p>
          <Button variant="outline" className="mt-4" onClick={() => setError(null)}>
            Reintentar
          </Button>
        </div>
      </div>
    );
  }

  const overview = metrics?.overview;
  const trends = metrics?.trends;

  const TrendBadge = ({ value }: { value: number | undefined }) => {
    if (value === undefined) return null;
    const isPositive = value >= 0;
    return (
      <Badge variant={isPositive ? 'default' : 'danger'} className="text-xs">
        {isPositive ? (
          <TrendingUp className="mr-1 h-3 w-3" />
        ) : (
          <TrendingDown className="mr-1 h-3 w-3" />
        )}
        {isPositive ? '+' : ''}
        {value}%
      </Badge>
    );
  };

  const kpis = [
    {
      label: 'Listados Activos',
      value: overview?.activeListings ?? 0,
      icon: Car,
      color: 'text-blue-600',
      bgColor: 'bg-blue-50',
    },
    {
      label: 'Vistas Totales',
      value: overview?.totalViews?.toLocaleString() ?? '0',
      icon: Eye,
      color: 'text-green-600',
      bgColor: 'bg-green-50',
      trend: trends?.viewsTrend,
    },
    {
      label: 'Leads Generados',
      value: overview?.totalLeads ?? 0,
      icon: MessageSquare,
      color: 'text-purple-600',
      bgColor: 'bg-purple-50',
      trend: trends?.leadsTrend,
    },
    {
      label: 'Vendidos este Mes',
      value: overview?.soldThisMonth ?? 0,
      icon: Target,
      color: 'text-amber-600',
      bgColor: 'bg-amber-50',
      trend: trends?.salesTrend,
    },
    {
      label: 'Tasa de Conversión',
      value: overview?.conversionRate ? `${overview.conversionRate.toFixed(1)}%` : 'N/A',
      icon: BarChart3,
      color: 'text-indigo-600',
      bgColor: 'bg-indigo-50',
    },
    {
      label: 'Prom. Días en Mercado',
      value: overview?.avgDaysOnMarket ?? 'N/A',
      icon: Clock,
      color: 'text-cyan-600',
      bgColor: 'bg-cyan-50',
    },
    {
      label: 'Ingresos del Mes',
      value: overview?.revenue ? `RD$${overview.revenue.toLocaleString()}` : 'N/A',
      icon: DollarSign,
      color: 'text-primary',
      bgColor: 'bg-primary/10',
    },
    {
      label: 'Listados Totales',
      value: overview?.totalListings ?? 0,
      icon: Car,
      color: 'text-slate-600',
      bgColor: 'bg-slate-50',
    },
  ];

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-foreground text-2xl font-bold">Rendimiento</h1>
          <p className="text-muted-foreground">Métricas detalladas del rendimiento de tu negocio</p>
        </div>
        <div className="flex gap-2">
          {(['week', 'month', 'quarter'] as const).map(p => (
            <Button
              key={p}
              variant={period === p ? 'default' : 'outline'}
              size="sm"
              onClick={() => setPeriod(p)}
            >
              {p === 'week' ? 'Semana' : p === 'month' ? 'Mes' : 'Trimestre'}
            </Button>
          ))}
        </div>
      </div>

      {/* KPI Cards */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {kpis.map(kpi => (
          <Card key={kpi.label}>
            <CardContent className="p-6">
              <div className="flex items-start justify-between">
                <div className="flex items-center gap-3">
                  <div className={`rounded-lg p-2.5 ${kpi.bgColor}`}>
                    <kpi.icon className={`h-5 w-5 ${kpi.color}`} />
                  </div>
                  <div>
                    <p className="text-muted-foreground text-xs">{kpi.label}</p>
                    <p className="mt-0.5 text-xl font-bold">{kpi.value}</p>
                  </div>
                </div>
                {'trend' in kpi && kpi.trend !== undefined && <TrendBadge value={kpi.trend} />}
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Tabs */}
      <Tabs defaultValue="engagement" className="space-y-4">
        <TabsList>
          <TabsTrigger value="engagement">Engagement</TabsTrigger>
          <TabsTrigger value="inventory">Inventario</TabsTrigger>
          <TabsTrigger value="leads">Leads</TabsTrigger>
        </TabsList>

        <TabsContent value="engagement">
          <Card>
            <CardHeader>
              <CardTitle>Engagement de Listados</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-muted-foreground flex min-h-[200px] items-center justify-center">
                <div className="text-center">
                  <BarChart3 className="mx-auto h-12 w-12" />
                  <p className="mt-3">Gráfico de engagement próximamente</p>
                  <p className="text-sm">Vistas, favoritos y consultas por vehículo</p>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="inventory">
          <Card>
            <CardHeader>
              <CardTitle>Estado del Inventario</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-muted-foreground flex min-h-[200px] items-center justify-center">
                <div className="text-center">
                  <Car className="mx-auto h-12 w-12" />
                  <p className="mt-3">Análisis de inventario próximamente</p>
                  <p className="text-sm">Distribución por marca, modelo y precio</p>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="leads">
          <Card>
            <CardHeader>
              <CardTitle>Análisis de Leads</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-muted-foreground flex min-h-[200px] items-center justify-center">
                <div className="text-center">
                  <MessageSquare className="mx-auto h-12 w-12" />
                  <p className="mt-3">Métricas de leads próximamente</p>
                  <p className="text-sm">Fuente, conversión y tiempo de respuesta</p>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}

export default function RendimientoPage() {
  return (
    <PlanGate feature="analytics">
      <RendimientoContent />
    </PlanGate>
  );
}
