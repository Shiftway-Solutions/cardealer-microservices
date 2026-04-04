/**
 * Dealer Vehicle Stats Page
 *
 * Analytics for a specific vehicle in dealer inventory — connected to DealerAnalyticsService
 */

'use client';

import { useMemo } from 'react';
import { useParams } from 'next/navigation';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import {
  ArrowLeft,
  Eye,
  Phone,
  MessageSquare,
  Heart,
  TrendingUp,
  Smartphone,
  Monitor,
  Tablet,
  MapPin,
  Zap,
  AlertCircle,
} from 'lucide-react';
import Link from 'next/link';
import { useCurrentDealer } from '@/hooks/use-dealers';
import { useInventoryPerformance, useEngagement, useTrends } from '@/hooks/use-dealer-analytics';
import type { VehiclePerformance } from '@/services/dealer-analytics';
import { PlanGate } from '@/components/plan/plan-gate';

// =============================================================================
// SKELETON
// =============================================================================

function VehicleStatsSkeleton() {
  return (
    <div className="min-h-screen bg-slate-900 p-6">
      <div className="mb-6 flex items-center gap-4">
        <Skeleton className="h-10 w-10 bg-slate-700" />
        <div>
          <Skeleton className="mb-2 h-7 w-64 bg-slate-700" />
          <Skeleton className="h-5 w-32 bg-slate-700" />
        </div>
      </div>
      <div className="mb-6 grid grid-cols-2 gap-4 md:grid-cols-4">
        {[1, 2, 3, 4].map(i => (
          <Card key={i} className="border-slate-700 bg-slate-800">
            <CardContent className="p-4">
              <Skeleton className="mb-2 h-5 w-5 bg-slate-700" />
              <Skeleton className="mb-1 h-10 w-20 bg-slate-700" />
              <Skeleton className="h-4 w-16 bg-slate-700" />
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}

// =============================================================================
// MAIN PAGE
// =============================================================================

export default function DealerVehicleStatsPage() {
  const params = useParams();
  const vehicleId = params.id as string;

  const { data: dealer, isLoading: isDealerLoading } = useCurrentDealer();
  const dealerId = dealer?.id || '';

  const { data: allPerformance, isLoading: isPerfLoading } = useInventoryPerformance(dealerId, {
    sortBy: 'views',
    limit: 100,
  });

  const { data: engagement, isLoading: isEngagementLoading } = useEngagement(dealerId);

  const { data: viewsTrend } = useTrends(dealerId, 'views');

  // Find the specific vehicle from performance data
  const vehiclePerf = useMemo(() => {
    if (!allPerformance || !Array.isArray(allPerformance)) return null;
    return (
      allPerformance.find(
        (v: VehiclePerformance) => v.vehicleId === vehicleId || v.id === vehicleId
      ) || null
    );
  }, [allPerformance, vehicleId]);

  const isLoading = isDealerLoading || isPerfLoading;

  if (isLoading) return <VehicleStatsSkeleton />;

  if (!dealer) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-900">
        <Card className="w-full max-w-md border-slate-700 bg-slate-800 p-6 text-center">
          <CardContent>
            <AlertCircle className="mx-auto mb-4 h-12 w-12 text-amber-500" />
            <h2 className="mb-2 text-xl font-semibold text-white">No se encontró el dealer</h2>
            <p className="text-slate-400">Por favor, inicia sesión como dealer.</p>
          </CardContent>
        </Card>
      </div>
    );
  }

  // Build stats from real data or defaults
  const views = vehiclePerf?.views || 0;
  const contacts = vehiclePerf?.contacts || 0;
  const favorites = vehiclePerf?.favorites || 0;
  const engagementScore = vehiclePerf?.engagementScore || 0;
  const daysOnMarket = vehiclePerf?.daysOnMarket || 0;
  const vehicleTitle =
    vehiclePerf?.vehicleTitle ||
    `${vehiclePerf?.vehicleMake || ''} ${vehiclePerf?.vehicleModel || ''} ${vehiclePerf?.vehicleYear || ''}`.trim() ||
    'Vehículo';

  // Engagement device data (dealer-level) — deviceBreakdown is an array
  const deviceBreakdown = engagement?.deviceBreakdown || [];
  const getDevicePercentage = (type: string) =>
    deviceBreakdown.find(d => d.deviceType.toLowerCase() === type)?.percentage || 0;
  const deviceMobile = getDevicePercentage('mobile') || 65;
  const deviceDesktop = getDevicePercentage('desktop') || 30;
  const deviceTablet = getDevicePercentage('tablet') || 5;

  // Top cities (dealer-level)
  const topCities = engagement?.topCities || [];

  // Daily views from trends
  const dailyViews: Array<{ label: string; value: number }> = Array.isArray(viewsTrend)
    ? viewsTrend.slice(-7)
    : [];
  const maxDailyViews = Math.max(...dailyViews.map(d => d.value), 1);

  return (
    <PlanGate feature="analytics">
      <div className="min-h-screen bg-slate-900 p-6">
        {/* Header */}
        <div className="mb-6 flex flex-col justify-between gap-4 md:flex-row md:items-center">
          <div className="flex items-center gap-4">
            <Link href={`/dealer/inventario/${vehicleId}`}>
              <Button
                variant="ghost"
                size="icon"
                className="text-slate-400 hover:bg-slate-800 hover:text-white"
              >
                <ArrowLeft className="h-5 w-5" />
              </Button>
            </Link>
            <div>
              <h1 className="text-2xl font-bold text-white">Estadísticas del Vehículo</h1>
              <p className="text-slate-400">{vehicleTitle}</p>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <Badge className="border-slate-600 bg-slate-700 text-slate-300">
              {daysOnMarket} días en el mercado
            </Badge>
            <Link href={`/dealer/inventario/${vehicleId}/boost`}>
              <Button className="bg-yellow-600 hover:bg-yellow-700">
                <Zap className="mr-2 h-4 w-4" />
                Promocionar
              </Button>
            </Link>
          </div>
        </div>

        {/* Overview Stats */}
        <div className="mb-6 grid grid-cols-2 gap-4 md:grid-cols-4">
          <Card className="border-slate-700 bg-slate-800">
            <CardContent className="p-4">
              <div className="mb-2 flex items-center justify-between">
                <Eye className="h-5 w-5 text-blue-400" />
              </div>
              <p className="text-3xl font-bold text-white">{views.toLocaleString()}</p>
              <p className="text-sm text-slate-400">Vistas totales</p>
            </CardContent>
          </Card>

          <Card className="border-slate-700 bg-slate-800">
            <CardContent className="p-4">
              <div className="mb-2 flex items-center justify-between">
                <Phone className="text-primary/80 h-5 w-5" />
              </div>
              <p className="text-3xl font-bold text-white">{contacts}</p>
              <p className="text-sm text-slate-400">Contactos</p>
            </CardContent>
          </Card>

          <Card className="border-slate-700 bg-slate-800">
            <CardContent className="p-4">
              <div className="mb-2 flex items-center justify-between">
                <Heart className="h-5 w-5 text-red-400" />
              </div>
              <p className="text-3xl font-bold text-white">{favorites}</p>
              <p className="text-sm text-slate-400">Favoritos</p>
            </CardContent>
          </Card>

          <Card className="border-slate-700 bg-slate-800">
            <CardContent className="p-4">
              <div className="mb-2 flex items-center justify-between">
                <MessageSquare className="h-5 w-5 text-purple-400" />
              </div>
              <p className="text-3xl font-bold text-white">{engagementScore.toFixed(0)}</p>
              <p className="text-sm text-slate-400">Nivel de interés</p>
            </CardContent>
          </Card>
        </div>

        <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
          {/* Charts Column */}
          <div className="space-y-6 lg:col-span-2">
            {/* Daily Views Chart */}
            <Card className="border-slate-700 bg-slate-800">
              <CardHeader>
                <CardTitle className="text-white">Tendencia de Vistas</CardTitle>
                <CardDescription className="text-slate-400">
                  Vistas recientes del dealer
                </CardDescription>
              </CardHeader>
              <CardContent>
                {dailyViews.length > 0 ? (
                  <div className="flex h-48 items-end gap-2">
                    {dailyViews.map((day, i) => (
                      <div key={i} className="flex flex-1 flex-col items-center">
                        <div
                          className="bg-primary w-full rounded-t"
                          style={{ height: `${(day.value / maxDailyViews) * 150}px` }}
                        />
                        <span className="mt-2 truncate text-xs text-slate-400">{day.label}</span>
                        <span className="text-xs text-white">{day.value}</span>
                      </div>
                    ))}
                  </div>
                ) : (
                  <div className="flex h-48 items-center justify-center">
                    <p className="text-slate-500">No hay datos de tendencia disponibles</p>
                  </div>
                )}
              </CardContent>
            </Card>

            {/* Performance Summary */}
            <Card className="border-slate-700 bg-slate-800">
              <CardContent className="p-6">
                <div className="flex items-start gap-4">
                  <div className="bg-primary/20 rounded-lg p-3">
                    <TrendingUp className="text-primary/80 h-6 w-6" />
                  </div>
                  <div>
                    <h3 className="mb-2 text-lg font-semibold text-white">
                      {views > 100
                        ? 'Rendimiento: Bueno'
                        : views > 30
                          ? 'Rendimiento: Regular'
                          : 'Rendimiento: Bajo'}
                    </h3>
                    <p className="text-slate-400">
                      {views > 100
                        ? 'Esta publicación está funcionando bien. Considera promocionarla para maximizar leads.'
                        : 'Considera agregar más fotos o mejorar la descripción para atraer más visitas.'}
                    </p>
                    <div className="mt-4 flex gap-3">
                      <Link href={`/dealer/inventario/${vehicleId}/boost`}>
                        <Button size="sm" className="bg-yellow-600 hover:bg-yellow-700">
                          <Zap className="mr-2 h-4 w-4" />
                          Promocionar
                        </Button>
                      </Link>
                      <Link href={`/dealer/inventario/${vehicleId}`}>
                        <Button
                          size="sm"
                          variant="outline"
                          className="border-slate-700 text-slate-300 hover:bg-slate-700"
                        >
                          Editar publicación
                        </Button>
                      </Link>
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>

          {/* Sidebar */}
          <div className="space-y-6">
            {/* Device Breakdown */}
            <Card className="border-slate-700 bg-slate-800">
              <CardHeader>
                <CardTitle className="text-white">Dispositivos</CardTitle>
                <CardDescription className="text-slate-400">
                  Distribución del dealer
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                {isEngagementLoading ? (
                  <div className="space-y-4">
                    {[1, 2, 3].map(i => (
                      <Skeleton key={i} className="h-6 w-full bg-slate-700" />
                    ))}
                  </div>
                ) : (
                  <>
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-3">
                        <Smartphone className="h-5 w-5 text-slate-400" />
                        <span className="text-slate-300">Móvil</span>
                      </div>
                      <div className="flex items-center gap-2">
                        <div className="h-2 w-24 overflow-hidden rounded-full bg-slate-700">
                          <div
                            className="bg-primary/100 h-full"
                            style={{ width: `${deviceMobile}%` }}
                          />
                        </div>
                        <span className="font-medium text-white">{deviceMobile}%</span>
                      </div>
                    </div>
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-3">
                        <Monitor className="h-5 w-5 text-slate-400" />
                        <span className="text-slate-300">Escritorio</span>
                      </div>
                      <div className="flex items-center gap-2">
                        <div className="h-2 w-24 overflow-hidden rounded-full bg-slate-700">
                          <div
                            className="h-full bg-blue-500"
                            style={{ width: `${deviceDesktop}%` }}
                          />
                        </div>
                        <span className="font-medium text-white">{deviceDesktop}%</span>
                      </div>
                    </div>
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-3">
                        <Tablet className="h-5 w-5 text-slate-400" />
                        <span className="text-slate-300">Tablet</span>
                      </div>
                      <div className="flex items-center gap-2">
                        <div className="h-2 w-24 overflow-hidden rounded-full bg-slate-700">
                          <div
                            className="h-full bg-purple-500"
                            style={{ width: `${deviceTablet}%` }}
                          />
                        </div>
                        <span className="font-medium text-white">{deviceTablet}%</span>
                      </div>
                    </div>
                  </>
                )}
              </CardContent>
            </Card>

            {/* Top Cities */}
            <Card className="border-slate-700 bg-slate-800">
              <CardHeader>
                <CardTitle className="flex items-center gap-2 text-white">
                  <MapPin className="h-5 w-5" />
                  Ciudades Principales
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                {topCities.length > 0 ? (
                  topCities.map((city, i) => (
                    <div key={i} className="flex items-center justify-between">
                      <span className="text-slate-300">{city.city}</span>
                      <div className="flex items-center gap-2">
                        <div className="h-2 w-20 overflow-hidden rounded-full bg-slate-700">
                          <div
                            className="bg-primary/100 h-full"
                            style={{ width: `${city.percentage}%` }}
                          />
                        </div>
                        <span className="text-sm font-medium text-white">{city.percentage}%</span>
                      </div>
                    </div>
                  ))
                ) : (
                  <p className="text-sm text-slate-500">Datos de ubicación no disponibles aún</p>
                )}
              </CardContent>
            </Card>

            {/* Quick Actions */}
            <Card className="border-slate-700 bg-slate-800">
              <CardHeader>
                <CardTitle className="text-white">Acciones Rápidas</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <Link href={`/dealer/inventario/${vehicleId}/boost`}>
                  <Button className="w-full bg-yellow-600 hover:bg-yellow-700">
                    <Zap className="mr-2 h-4 w-4" />
                    Promocionar vehículo
                  </Button>
                </Link>
                <Link href={`/dealer/inventario/${vehicleId}`}>
                  <Button
                    variant="outline"
                    className="w-full border-slate-700 text-slate-300 hover:bg-slate-700"
                  >
                    Editar publicación
                  </Button>
                </Link>
                <Link href="/dealer/leads">
                  <Button
                    variant="outline"
                    className="w-full border-slate-700 text-slate-300 hover:bg-slate-700"
                  >
                    Ver todos los interesados
                  </Button>
                </Link>
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    </PlanGate>
  );
}
