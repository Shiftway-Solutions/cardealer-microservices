/**
 * Dealer Inventory Analytics Page
 *
 * Analytics dashboard for vehicle inventory performance.
 * Data derived from real vehicle inventory via useVehiclesByDealer.
 */

'use client';

import * as React from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import {
  ArrowLeft,
  Car,
  Eye,
  Clock,
  TrendingUp,
  TrendingDown,
  AlertCircle,
  BarChart3,
  PieChart,
  Download,
  Loader2,
} from 'lucide-react';
import Link from 'next/link';
import { useCurrentDealer } from '@/hooks/use-dealers';
import { useVehiclesByDealer } from '@/hooks/use-vehicles';
import { PlanGate } from '@/components/plan/plan-gate';

function InventoryAnalyticsContent() {
  const { data: dealer } = useCurrentDealer();
  const { data: vehiclesData, isLoading } = useVehiclesByDealer(dealer?.id || '');

  // eslint-disable-next-line react-hooks/exhaustive-deps
  const vehicles = vehiclesData?.items || [];

  // Compute inventory stats from real data
  // eslint-disable-next-line react-hooks/exhaustive-deps
  const inventoryStats = React.useMemo(() => {
    const total = vehicles.length;
    const active = vehicles.filter(v => v.status === 'active').length;
    const pending = vehicles.filter(v => v.status === 'pending' || v.status === 'draft').length;
    const sold = vehicles.filter(v => v.status === 'sold').length;
    const totalViews = vehicles.reduce((sum, v) => sum + (v.viewCount || 0), 0);
    const avgViews = total > 0 ? Math.round(totalViews / total) : 0;
    const avgDaysOnMarket =
      total > 0
        ? Math.round(
            vehicles.reduce((sum, v) => {
              const days = v.createdAt
                // eslint-disable-next-line react-hooks/exhaustive-deps
                ? Math.ceil((Date.now() - new Date(v.createdAt).getTime()) / 86400000)
          )
        : 0;

    return { total, active, pending, sold, avgDaysOnMarket, avgViews, totalViews };
  }, [vehicles]);

  // Top performers: sorted by viewCount desc
  // eslint-disable-next-line react-hooks/exhaustive-deps
  const topPerformers = React.useMemo(() => {
    return [...vehicles]
      .sort((a, b) => (b.viewCount || 0) - (a.viewCount || 0))
      .slice(0, 5)
      .map(v => ({
        id: v.id,
        title: `${v.make} ${v.model} ${v.year}`,
        views: v.viewCount || 0,
        leads: 0,
        daysActive: v.createdAt
          // eslint-disable-next-line react-hooks/exhaustive-deps
          ? Math.ceil((Date.now() - new Date(v.createdAt).getTime()) / 86400000)
          : 0,
      }));
  }, [vehicles]);

  // Underperformers: 30+ days with < 30 views
  // eslint-disable-next-line react-hooks/exhaustive-deps
  const underperformers = React.useMemo(() => {
    return vehicles
      .filter(v => {
        const days = v.createdAt
          // eslint-disable-next-line react-hooks/exhaustive-deps
          ? Math.ceil((Date.now() - new Date(v.createdAt).getTime()) / 86400000)
      })
      .sort((a, b) => (a.viewCount || 0) - (b.viewCount || 0))
      .slice(0, 5)
      .map(v => ({
        id: v.id,
        title: `${v.make} ${v.model} ${v.year}`,
        views: v.viewCount || 0,
        leads: 0,
        daysActive: v.createdAt
          // eslint-disable-next-line react-hooks/exhaustive-deps
          ? Math.ceil((Date.now() - new Date(v.createdAt).getTime()) / 86400000)
          : 0,
      }));
  }, [vehicles]);

  // Category distribution by make (bodyType not available in VehicleCardData)
  // eslint-disable-next-line react-hooks/exhaustive-deps
  const categoryDistribution = React.useMemo(() => {
    const counts: Record<string, number> = {};
    vehicles.forEach(v => {
      const type = v.make || 'Otro';
      counts[type] = (counts[type] || 0) + 1;
    });
    const total = vehicles.length || 1;
    return Object.entries(counts)
      .map(([name, count]) => ({
        name,
        count,
        percentage: Math.round((count / total) * 100),
      }))
      .sort((a, b) => b.count - a.count)
      .slice(0, 6);
  }, [vehicles]);

  if (isLoading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <Loader2 className="text-muted-foreground h-8 w-8 animate-spin" />
      </div>
    );
  }
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
            <h1 className="text-2xl font-bold">Analytics de Inventario</h1>
            <p className="text-muted-foreground">Rendimiento de tu inventario de vehículos</p>
          </div>
        </div>
        <Button variant="outline">
          <Download className="mr-2 h-4 w-4" />
          Exportar Reporte
        </Button>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="bg-primary/10 rounded-lg p-2">
                <Car className="text-primary h-5 w-5" />
              </div>
              <div>
                <p className="text-muted-foreground text-sm">Vehículos Activos</p>
                <p className="text-2xl font-bold">{inventoryStats.active}</p>
                <p className="text-muted-foreground text-xs">de {inventoryStats.total} totales</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-blue-100 p-2">
                <Eye className="h-5 w-5 text-blue-600" />
              </div>
              <div>
                <p className="text-muted-foreground text-sm">Vistas Totales</p>
                <p className="text-2xl font-bold">{inventoryStats.totalViews.toLocaleString()}</p>
                <p className="text-muted-foreground text-xs">este mes</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-purple-100 p-2">
                <BarChart3 className="h-5 w-5 text-purple-600" />
              </div>
              <div>
                <p className="text-muted-foreground text-sm">Promedio Vistas</p>
                <p className="text-2xl font-bold">{inventoryStats.avgViews}</p>
                <p className="text-muted-foreground text-xs">por vehículo</p>
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
                <p className="text-muted-foreground text-sm">Días Promedio</p>
                <p className="text-2xl font-bold">{inventoryStats.avgDaysOnMarket}</p>
                <p className="text-muted-foreground text-xs">en el mercado</p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        {/* Top Performers */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <TrendingUp className="text-primary h-5 w-5" />
              Mejor Rendimiento
            </CardTitle>
            <CardDescription>Vehículos con más vistas y leads</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {topPerformers.map((vehicle, index) => (
                <div
                  key={vehicle.id}
                  className="bg-muted/50 flex items-center gap-4 rounded-lg p-3"
                >
                  <div className="bg-primary/10 text-primary flex h-8 w-8 items-center justify-center rounded-full font-bold">
                    {index + 1}
                  </div>
                  <div className="flex-1">
                    <p className="font-medium">{vehicle.title}</p>
                    <div className="text-muted-foreground flex gap-4 text-sm">
                      <span className="flex items-center gap-1">
                        <Eye className="h-3 w-3" /> {vehicle.views}
                      </span>
                      <span>{vehicle.leads} leads</span>
                      <span>{vehicle.daysActive} días</span>
                    </div>
                  </div>
                  <Badge className="bg-primary/10 text-primary">Destacado</Badge>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        {/* Underperformers */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <TrendingDown className="h-5 w-5 text-red-500" />
              Requieren Atención
            </CardTitle>
            <CardDescription>Vehículos con bajo rendimiento</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {underperformers.map(vehicle => (
                <div
                  key={vehicle.id}
                  className="flex items-center gap-4 rounded-lg border border-red-100 bg-red-50 p-3"
                >
                  <AlertCircle className="h-5 w-5 text-red-500" />
                  <div className="flex-1">
                    <p className="font-medium">{vehicle.title}</p>
                    <div className="text-muted-foreground flex gap-4 text-sm">
                      <span className="flex items-center gap-1">
                        <Eye className="h-3 w-3" /> {vehicle.views}
                      </span>
                      <span>{vehicle.leads} leads</span>
                      <span className="text-red-600">{vehicle.daysActive} días</span>
                    </div>
                  </div>
                  <Button size="sm" variant="outline" className="border-red-300 text-red-600">
                    Optimizar
                  </Button>
                </div>
              ))}
            </div>

            <div className="mt-6 rounded-lg border border-yellow-200 bg-yellow-50 p-4">
              <div className="flex items-start gap-3">
                <AlertCircle className="h-5 w-5 flex-shrink-0 text-yellow-600" />
                <div>
                  <p className="font-medium text-yellow-800">Recomendación</p>
                  <p className="text-sm text-yellow-700">
                    Considera reducir el precio o mejorar las fotos de estos vehículos para aumentar
                    su visibilidad y generar más leads.
                  </p>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Category Distribution */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <PieChart className="h-5 w-5" />
            Distribución por Categoría
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {categoryDistribution.map(cat => (
              <div key={cat.name} className="flex items-center gap-4">
                <div className="w-24 text-sm font-medium">{cat.name}</div>
                <div className="flex-1">
                  <Progress value={cat.percentage} className="h-3" />
                </div>
                <div className="w-20 text-right">
                  <span className="text-sm font-medium">{cat.count}</span>
                  <span className="text-muted-foreground ml-1 text-sm">({cat.percentage}%)</span>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Views Chart Placeholder */}
      <Card>
        <CardHeader>
          <CardTitle>Tendencia de Vistas</CardTitle>
          <CardDescription>Vistas por día en los últimos 30 días</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="bg-muted/50 flex h-64 items-center justify-center rounded-lg">
            <div className="text-muted-foreground text-center">
              <BarChart3 className="mx-auto mb-2 h-12 w-12" />
              <p>Gráfico de tendencias</p>
              <p className="text-sm">Integrar Chart.js o Recharts</p>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

export default function InventoryAnalyticsPage() {
  return (
    <PlanGate feature="analytics">
      <InventoryAnalyticsContent />
    </PlanGate>
  );
}
