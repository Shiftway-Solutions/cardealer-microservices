/**
 * Admin Analytics Page
 *
 * Platform analytics and insights
 */

'use client';

import { useState, useMemo } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import {
  BarChart3,
  Users,
  Car,
  Eye,
  DollarSign,
  Download,
  ArrowUpRight,
  ArrowDownRight,
  Loader2,
} from 'lucide-react';
import {
  useAnalyticsOverview,
  useWeeklyData,
  useTopVehicleSearches,
  useTrafficSources,
  useDeviceBreakdown,
  useConversionRates,
  useRevenueByChannel,
  useExportAnalyticsReport,
} from '@/hooks/use-admin-extended';

const iconMap: Record<string, React.ComponentType<{ className?: string }>> = {
  visits: Eye,
  users: Users,
  vehicles: Car,
  mrr: DollarSign,
};

export default function AdminAnalyticsPage() {
  const [dateRange, setDateRange] = useState('7d');

  const { data: overviewStats = [], isLoading: loadingOverview } = useAnalyticsOverview(dateRange);
  const { data: weeklyData = [], isLoading: loadingWeekly } = useWeeklyData(dateRange);
  const { data: topVehicles = [] } = useTopVehicleSearches();
  const { data: trafficSources = [] } = useTrafficSources(dateRange);
  const { data: deviceBreakdown = [] } = useDeviceBreakdown(dateRange);
  const { data: conversions } = useConversionRates(dateRange);
  const { data: revenueByChannel = [] } = useRevenueByChannel(dateRange);
  const exportMutation = useExportAnalyticsReport();

  const maxVisits = useMemo(
    () => Math.max(...(weeklyData.length > 0 ? weeklyData.map(d => d.visits) : [1])),
    [weeklyData]
  );

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col justify-between gap-4 sm:flex-row">
        <div>
          <h1 className="text-foreground text-2xl font-bold">Analytics</h1>
          <p className="text-muted-foreground">Métricas y análisis de la plataforma</p>
        </div>
        <div className="flex gap-3">
          <div className="flex rounded-lg border">
            {['7d', '30d', '90d', '1y'].map(range => (
              <button
                key={range}
                onClick={() => setDateRange(range)}
                className={`px-3 py-1.5 text-sm ${
                  dateRange === range
                    ? 'bg-slate-900 text-white'
                    : 'text-muted-foreground hover:bg-muted'
                } ${range === '7d' ? 'rounded-l-lg' : ''} ${range === '1y' ? 'rounded-r-lg' : ''}`}
              >
                {range}
              </button>
            ))}
          </div>
          <Button
            variant="outline"
            disabled={exportMutation.isPending}
            onClick={() => exportMutation.mutate({ period: dateRange, format: 'pdf' })}
          >
            {exportMutation.isPending ? (
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <Download className="mr-2 h-4 w-4" />
            )}
            Exportar
          </Button>
        </div>
      </div>

      {/* Overview Stats */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        {loadingOverview ? (
          <div className="col-span-4 flex justify-center py-8">
            <Loader2 className="text-muted-foreground h-6 w-6 animate-spin" />
          </div>
        ) : overviewStats.length > 0 ? (
          overviewStats.map(stat => {
            const Icon = iconMap[stat.metric] || Eye;
            const TrendIcon = stat.trend === 'up' ? ArrowUpRight : ArrowDownRight;
            return (
              <Card key={stat.label}>
                <CardContent className="p-4">
                  <div className="mb-3 flex items-center justify-between">
                    <div className="rounded-lg bg-slate-100 p-2">
                      <Icon className="h-5 w-5 text-slate-600" />
                    </div>
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
                  <p className="text-muted-foreground text-xs">{stat.label}</p>
                </CardContent>
              </Card>
            );
          })
        ) : (
          <div className="text-muted-foreground col-span-4 py-8 text-center">
            No hay datos de analytics disponibles
          </div>
        )}
      </div>

      {/* Weekly Chart */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <BarChart3 className="h-5 w-5" />
            Visitas Semanales
          </CardTitle>
        </CardHeader>
        <CardContent>
          {loadingWeekly ? (
            <div className="flex h-64 items-center justify-center">
              <Loader2 className="text-muted-foreground h-6 w-6 animate-spin" />
            </div>
          ) : weeklyData.length > 0 ? (
            <>
              <div className="h-64">
                <div className="flex h-full items-end gap-2">
                  {weeklyData.map(data => (
                    <div key={data.day} className="flex flex-1 flex-col items-center gap-2">
                      <div
                        className="w-full rounded-t-lg bg-gradient-to-t from-primary to-primary/60 transition-all hover:opacity-80"
                        style={{ height: `${(data.visits / maxVisits) * 100}%` }}
                      />
                      <span className="text-muted-foreground text-xs">{data.day}</span>
                    </div>
                  ))}
                </div>
              </div>
              <div className="mt-4 flex justify-center gap-8 text-sm">
                <div className="flex items-center gap-2">
                  <div className="h-3 w-3 rounded-full bg-primary/100" />
                  <span className="text-muted-foreground">Visitas</span>
                </div>
              </div>
            </>
          ) : (
            <div className="text-muted-foreground flex h-64 items-center justify-center">
              No hay datos semanales disponibles
            </div>
          )}
        </CardContent>
      </Card>

      <div className="grid gap-6 lg:grid-cols-2">
        {/* Top Vehicles Searched */}
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Vehículos Más Buscados</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {topVehicles.map((vehicle, index) => (
                <div
                  key={index}
                  className="hover:bg-muted/50 flex items-center justify-between rounded-lg p-3"
                >
                  <div className="flex items-center gap-3">
                    <span className="flex h-6 w-6 items-center justify-center rounded-full bg-slate-900 text-xs text-white">
                      {index + 1}
                    </span>
                    <div>
                      <p className="font-medium">
                        {vehicle.make} {vehicle.model}
                      </p>
                      <p className="text-muted-foreground text-xs">
                        {vehicle.searches.toLocaleString()} búsquedas
                      </p>
                    </div>
                  </div>
                  <div className="text-right">
                    <p className="text-sm font-medium">{vehicle.views.toLocaleString()} vistas</p>
                    <p className="text-xs text-primary">
                      {vehicle.leads.toLocaleString()} leads
                    </p>
                  </div>
                </div>
              ))}
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
              {trafficSources.map(source => (
                <div key={source.source}>
                  <div className="mb-1 flex justify-between text-sm">
                    <span className="font-medium">{source.source}</span>
                    <span className="text-muted-foreground">{source.visits}</span>
                  </div>
                  <div className="bg-muted h-2 overflow-hidden rounded-full">
                    <div
                      className="h-full rounded-full bg-gradient-to-r from-primary to-primary/60"
                      style={{ width: `${source.percentage}%` }}
                    />
                  </div>
                  <p className="text-muted-foreground mt-1 text-xs">{source.percentage}%</p>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Device Breakdown & Conversions */}
      <div className="grid gap-6 lg:grid-cols-3">
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Dispositivos</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {deviceBreakdown.map(device => (
                <div key={device.device} className="flex items-center gap-4">
                  <div className="flex-1">
                    <div className="mb-1 flex justify-between text-sm">
                      <span>{device.device}</span>
                      <span className="font-medium">{device.percentage}%</span>
                    </div>
                    <div className="bg-muted h-2 overflow-hidden rounded-full">
                      <div
                        className="h-full rounded-full bg-slate-900"
                        style={{ width: `${device.percentage}%` }}
                      />
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Conversiones</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              <div className="bg-muted/50 rounded-lg p-4">
                <p className="text-2xl font-bold">
                  {conversions?.visitToSignup?.toFixed(1) ?? '—'}%
                </p>
                <p className="text-muted-foreground text-sm">Visita → Registro</p>
              </div>
              <div className="bg-muted/50 rounded-lg p-4">
                <p className="text-2xl font-bold">
                  {conversions?.signupToListing?.toFixed(1) ?? '—'}%
                </p>
                <p className="text-muted-foreground text-sm">Registro → Publicación</p>
              </div>
              <div className="bg-muted/50 rounded-lg p-4">
                <p className="text-2xl font-bold">{conversions?.viewToLead?.toFixed(1) ?? '—'}%</p>
                <p className="text-muted-foreground text-sm">Vista → Lead</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Ingresos por Canal</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {revenueByChannel.length > 0 ? (
                revenueByChannel.map(ch => (
                  <div
                    key={ch.channel}
                    className="flex items-center justify-between rounded-lg p-3"
                    style={{ backgroundColor: `${ch.color}15` }}
                  >
                    <span className="font-medium" style={{ color: ch.color }}>
                      {ch.channel}
                    </span>
                    <span className="font-bold" style={{ color: ch.color }}>
                      RD$ {(ch.amount / 1000).toFixed(0)}K
                    </span>
                  </div>
                ))
              ) : (
                <p className="text-muted-foreground text-center text-sm">Sin datos</p>
              )}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
