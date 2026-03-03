/**
 * Vehicle Stats Page
 *
 * Performance statistics for a vehicle listing
 */

'use client';

import { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  ArrowLeft,
  Eye,
  Phone,
  MessageSquare,
  Heart,
  Share2,
  TrendingUp,
  TrendingDown,
  BarChart3,
  Users,
  MapPin,
  Smartphone,
  Monitor,
  Sparkles,
} from 'lucide-react';
import Link from 'next/link';

const stats = {
  views: { total: 1234, change: 15, trend: 'up' },
  calls: { total: 47, change: 8, trend: 'up' },
  messages: { total: 32, change: -5, trend: 'down' },
  favorites: { total: 89, change: 12, trend: 'up' },
  shares: { total: 24, change: 3, trend: 'up' },
};

const dailyViews = [
  { day: 'Lun', views: 45 },
  { day: 'Mar', views: 62 },
  { day: 'Mie', views: 58 },
  { day: 'Jue', views: 71 },
  { day: 'Vie', views: 89 },
  { day: 'Sab', views: 124 },
  { day: 'Dom', views: 98 },
];

const deviceBreakdown = [
  { device: 'Móvil', percentage: 68, icon: Smartphone },
  { device: 'Desktop', percentage: 28, icon: Monitor },
  { device: 'Tablet', percentage: 4, icon: Monitor },
];

const topLocations = [
  { location: 'Santo Domingo', views: 456 },
  { location: 'Santiago', views: 234 },
  { location: 'La Vega', views: 123 },
  { location: 'San Cristóbal', views: 89 },
  { location: 'Puerto Plata', views: 67 },
];

const recentInteractions = [
  { type: 'call', user: 'Usuario anónimo', time: 'Hace 2 horas', location: 'Santo Domingo' },
  { type: 'message', user: 'Juan Pérez', time: 'Hace 5 horas', location: 'Santiago' },
  { type: 'favorite', user: 'María García', time: 'Hace 8 horas', location: 'La Vega' },
  { type: 'share', user: 'Usuario anónimo', time: 'Hace 1 día', location: 'San Cristóbal' },
  { type: 'call', user: 'Carlos Martínez', time: 'Hace 1 día', location: 'Santo Domingo' },
];

export default function VehicleStatsPage() {
  const [period, setPeriod] = useState('7days');

  const maxViews = Math.max(...dailyViews.map(d => d.views));

  const getInteractionIcon = (type: string) => {
    switch (type) {
      case 'call':
        return <Phone className="h-4 w-4 text-blue-500" />;
      case 'message':
        return <MessageSquare className="h-4 w-4 text-purple-500" />;
      case 'favorite':
        return <Heart className="h-4 w-4 text-red-500" />;
      case 'share':
        return <Share2 className="h-4 w-4 text-primary" />;
      default:
        return <Eye className="h-4 w-4" />;
    }
  };

  return (
    <div className="min-h-screen bg-muted/50">
      <div className="mx-auto max-w-6xl px-4 py-8">
        {/* Header */}
        <div className="mb-8 flex items-center justify-between">
          <div className="flex items-center gap-4">
            <Link href="/mis-vehiculos">
              <Button variant="ghost" size="icon">
                <ArrowLeft className="h-5 w-5" />
              </Button>
            </Link>
            <div>
              <h1 className="text-2xl font-bold text-foreground">Estadísticas del Vehículo</h1>
              <p className="text-muted-foreground">Toyota Corolla XLE 2022</p>
            </div>
          </div>
          <div className="flex items-center gap-4">
            <Select value={period} onValueChange={setPeriod}>
              <SelectTrigger className="w-40">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="7days">Últimos 7 días</SelectItem>
                <SelectItem value="30days">Últimos 30 días</SelectItem>
                <SelectItem value="90days">Últimos 90 días</SelectItem>
                <SelectItem value="all">Todo el tiempo</SelectItem>
              </SelectContent>
            </Select>
            <Link href="/mis-vehiculos/v1/boost">
              <Button className="bg-gradient-to-r from-amber-500 to-orange-500 hover:from-amber-600 hover:to-orange-600">
                <Sparkles className="mr-2 h-4 w-4" />
                Destacar
              </Button>
            </Link>
          </div>
        </div>

        {/* Main Stats */}
        <div className="mb-8 grid grid-cols-2 gap-4 md:grid-cols-5">
          <Card>
            <CardContent className="p-4">
              <div className="mb-2 flex items-center justify-between">
                <Eye className="h-5 w-5 text-blue-500" />
                <div
                  className={`flex items-center text-xs ${
                    stats.views.trend === 'up' ? 'text-primary' : 'text-red-600'
                  }`}
                >
                  {stats.views.trend === 'up' ? (
                    <TrendingUp className="mr-1 h-3 w-3" />
                  ) : (
                    <TrendingDown className="mr-1 h-3 w-3" />
                  )}
                  {stats.views.change}%
                </div>
              </div>
              <p className="text-2xl font-bold">{stats.views.total.toLocaleString()}</p>
              <p className="text-sm text-muted-foreground">Vistas</p>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-4">
              <div className="mb-2 flex items-center justify-between">
                <Phone className="h-5 w-5 text-amber-500" />
                <div
                  className={`flex items-center text-xs ${
                    stats.calls.trend === 'up' ? 'text-primary' : 'text-red-600'
                  }`}
                >
                  {stats.calls.trend === 'up' ? (
                    <TrendingUp className="mr-1 h-3 w-3" />
                  ) : (
                    <TrendingDown className="mr-1 h-3 w-3" />
                  )}
                  {Math.abs(stats.calls.change)}%
                </div>
              </div>
              <p className="text-2xl font-bold">{stats.calls.total}</p>
              <p className="text-sm text-muted-foreground">Llamadas</p>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-4">
              <div className="mb-2 flex items-center justify-between">
                <MessageSquare className="h-5 w-5 text-purple-500" />
                <div
                  className={`flex items-center text-xs ${
                    stats.messages.trend === 'up' ? 'text-primary' : 'text-red-600'
                  }`}
                >
                  {stats.messages.trend === 'up' ? (
                    <TrendingUp className="mr-1 h-3 w-3" />
                  ) : (
                    <TrendingDown className="mr-1 h-3 w-3" />
                  )}
                  {Math.abs(stats.messages.change)}%
                </div>
              </div>
              <p className="text-2xl font-bold">{stats.messages.total}</p>
              <p className="text-sm text-muted-foreground">Mensajes</p>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-4">
              <div className="mb-2 flex items-center justify-between">
                <Heart className="h-5 w-5 text-red-500" />
                <div
                  className={`flex items-center text-xs ${
                    stats.favorites.trend === 'up' ? 'text-primary' : 'text-red-600'
                  }`}
                >
                  <TrendingUp className="mr-1 h-3 w-3" />
                  {stats.favorites.change}%
                </div>
              </div>
              <p className="text-2xl font-bold">{stats.favorites.total}</p>
              <p className="text-sm text-muted-foreground">Favoritos</p>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-4">
              <div className="mb-2 flex items-center justify-between">
                <Share2 className="h-5 w-5 text-primary" />
                <div className="flex items-center text-xs text-primary">
                  <TrendingUp className="mr-1 h-3 w-3" />
                  {stats.shares.change}%
                </div>
              </div>
              <p className="text-2xl font-bold">{stats.shares.total}</p>
              <p className="text-sm text-muted-foreground">Compartidos</p>
            </CardContent>
          </Card>
        </div>

        <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
          {/* Views Chart */}
          <div className="lg:col-span-2">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <BarChart3 className="h-5 w-5" />
                  Vistas Diarias
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="flex h-48 items-end justify-between gap-2">
                  {dailyViews.map(day => (
                    <div key={day.day} className="flex flex-1 flex-col items-center">
                      <span className="mb-1 text-xs text-muted-foreground">{day.views}</span>
                      <div
                        className="w-full rounded-t-lg bg-primary/100 transition-all hover:bg-primary/80"
                        style={{ height: `${(day.views / maxViews) * 100}%` }}
                      />
                      <span className="mt-2 text-xs text-muted-foreground">{day.day}</span>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          </div>

          {/* Device Breakdown */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Monitor className="h-5 w-5" />
                Dispositivos
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {deviceBreakdown.map(item => {
                  const Icon = item.icon;
                  return (
                    <div key={item.device}>
                      <div className="mb-1 flex items-center justify-between">
                        <div className="flex items-center gap-2">
                          <Icon className="h-4 w-4 text-muted-foreground" />
                          <span className="text-sm">{item.device}</span>
                        </div>
                        <span className="text-sm font-medium">{item.percentage}%</span>
                      </div>
                      <div className="h-2 w-full overflow-hidden rounded-full bg-muted">
                        <div
                          className="h-full rounded-full bg-primary/100"
                          style={{ width: `${item.percentage}%` }}
                        />
                      </div>
                    </div>
                  );
                })}
              </div>
            </CardContent>
          </Card>

          {/* Top Locations */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <MapPin className="h-5 w-5" />
                Ubicaciones
              </CardTitle>
              <CardDescription>De dónde ven tu publicación</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                {topLocations.map((loc, i) => (
                  <div key={loc.location} className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <span
                        className={`flex h-5 w-5 items-center justify-center rounded-full text-xs font-medium ${
                          i === 0 ? 'bg-primary/10 text-primary' : 'bg-muted text-muted-foreground'
                        }`}
                      >
                        {i + 1}
                      </span>
                      <span className="text-sm">{loc.location}</span>
                    </div>
                    <span className="text-sm text-muted-foreground">{loc.views}</span>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>

          {/* Recent Interactions */}
          <Card className="lg:col-span-2">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Users className="h-5 w-5" />
                Interacciones Recientes
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                {recentInteractions.map((interaction, i) => (
                  <div
                    key={i}
                    className="flex items-center justify-between rounded-lg bg-muted/50 p-3"
                  >
                    <div className="flex items-center gap-3">
                      <div className="rounded-lg bg-card p-2">
                        {getInteractionIcon(interaction.type)}
                      </div>
                      <div>
                        <p className="text-sm font-medium">{interaction.user}</p>
                        <p className="text-xs text-muted-foreground">{interaction.location}</p>
                      </div>
                    </div>
                    <span className="text-xs text-muted-foreground">{interaction.time}</span>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Tips Card */}
        <Card className="mt-6 border-primary bg-gradient-to-r from-primary/5 to-teal-50">
          <CardContent className="p-6">
            <div className="flex items-start gap-4">
              <div className="rounded-lg bg-primary/10 p-3">
                <TrendingUp className="h-6 w-6 text-primary" />
              </div>
              <div>
                <h3 className="mb-2 font-semibold text-primary">
                  Tips para aumentar tus vistas
                </h3>
                <ul className="space-y-1 text-sm text-primary">
                  <li>• Actualiza el precio regularmente para mantener competitividad</li>
                  <li>• Agrega más fotos de diferentes ángulos</li>
                  <li>• Responde rápidamente a los mensajes para mejorar tu reputación</li>
                  <li>• Considera destacar tu publicación para 10x más visibilidad</li>
                </ul>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
