/**
 * Admin Dashboard Page
 *
 * Main dashboard for administrators showing platform overview.
 * Data refreshes automatically in the background via React Query polling.
 */

'use client';

import Link from 'next/link';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { useSiteConfig } from '@/providers/site-config-provider';
import { Button } from '@/components/ui/button';
import {
  Users,
  Car,
  Building,
  DollarSign,
  AlertTriangle,
  CheckCircle,
  Clock,
  Flag,
  ChevronRight,
} from 'lucide-react';
import { useDashboardStats, useRecentActivity, usePendingActions } from '@/hooks/use-admin';
import {
  DashboardSkeleton,
  StatsCard,
  PriorityBadge,
  QuickActionButton,
} from '@/components/dashboard';
import { formatCurrency } from '@/lib/utils';

// =============================================================================
// HELPERS
// =============================================================================

const getActionIcon = (type: string) => {
  switch (type) {
    case 'moderation':
      return Clock;
    case 'verification':
      return Building;
    case 'report':
      return Flag;
    case 'support':
      return AlertTriangle;
    default:
      return Clock;
  }
};

const getActivityIcon = (type: string) => {
  switch (type) {
    case 'dealer':
      return Building;
    case 'vehicle':
      return Car;
    case 'user':
      return Users;
    case 'payment':
      return DollarSign;
    case 'report':
      return Flag;
    default:
      return CheckCircle;
  }
};

/**
 * Format currency with compact notation for dashboard stats
 */
const formatCompactCurrency = (value: number) => {
  if (value >= 1000000) {
    return `RD$ ${(value / 1000000).toFixed(1)}M`;
  }
  if (value >= 1000) {
    return `RD$ ${(value / 1000).toFixed(0)}K`;
  }
  return formatCurrency(value);
};

// =============================================================================
// MAIN COMPONENT
// =============================================================================

export default function AdminDashboardPage() {
  const config = useSiteConfig();
  const { data: stats, isLoading: statsLoading, error: statsError } = useDashboardStats();

  const { data: activity, isLoading: activityLoading, error: activityError } = useRecentActivity(5);

  const {
    data: pendingActions,
    isLoading: pendingLoading,
    error: pendingError,
  } = usePendingActions();

  const isLoading = statsLoading || activityLoading || pendingLoading;
  const hasAuthError = [statsError, activityError, pendingError].some(
    err =>
      (err as { code?: string })?.code === 'HTTP_401' ||
      (err as { message?: string })?.message?.includes('401')
  );

  // Handle authentication errors
  if (hasAuthError) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-gray-50">
        <Card className="mx-4 w-full max-w-md">
          <CardHeader className="text-center">
            <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-red-100">
              <AlertTriangle className="h-6 w-6 text-red-600" />
            </div>
            <CardTitle className="text-xl">Acceso Restringido</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4 text-center">
            <p className="text-gray-600">
              Necesitas iniciar sesión con una cuenta de administrador para acceder al panel.
            </p>
            <div className="flex flex-col gap-2">
              <Link href="/login">
                <Button className="w-full">Iniciar Sesión</Button>
              </Link>
              <Link href="/">
                <Button variant="outline" className="w-full">
                  Volver al Inicio
                </Button>
              </Link>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  if (isLoading) {
    return <DashboardSkeleton />;
  }

  const platformStats = [
    {
      title: 'Usuarios Totales',
      value: stats?.totalUsers.toLocaleString() || '0',
      change: stats?.usersChange
        ? `${stats.usersChange > 0 ? '+' : ''}${stats.usersChange}%`
        : '~0%',
      trend:
        (stats?.usersChange || 0) > 0 ? 'up' : (stats?.usersChange || 0) < 0 ? 'down' : 'neutral',
      icon: Users,
      color: 'blue' as const,
    },
    {
      title: 'Vehículos Activos',
      value: stats?.activeVehicles.toLocaleString() || '0',
      change: stats?.vehiclesChange
        ? `${stats.vehiclesChange > 0 ? '+' : ''}${stats.vehiclesChange}%`
        : '~0%',
      trend:
        (stats?.vehiclesChange || 0) > 0
          ? 'up'
          : (stats?.vehiclesChange || 0) < 0
            ? 'down'
            : 'neutral',
      icon: Car,
      color: 'primary' as const,
    },
    {
      title: 'Dealers Activos',
      value: stats?.activeDealers.toLocaleString() || '0',
      change: stats?.dealersChange
        ? `${stats.dealersChange > 0 ? '+' : ''}${stats.dealersChange}`
        : '~0',
      trend:
        (stats?.dealersChange || 0) > 0
          ? 'up'
          : (stats?.dealersChange || 0) < 0
            ? 'down'
            : 'neutral',
      icon: Building,
      color: 'purple' as const,
    },
    {
      title: 'MRR',
      value: formatCompactCurrency(stats?.mrr || 0),
      change: stats?.mrrChange ? `${stats.mrrChange > 0 ? '+' : ''}${stats.mrrChange}%` : '~0%',
      trend: (stats?.mrrChange || 0) > 0 ? 'up' : (stats?.mrrChange || 0) < 0 ? 'down' : 'neutral',
      icon: DollarSign,
      color: 'amber' as const,
    },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col justify-between gap-4 sm:flex-row">
        <div>
          <h1 className="text-foreground text-3xl font-bold">Dashboard</h1>
          <p className="text-muted-foreground">
            Bienvenido al panel de administración de {config.siteName}
          </p>
        </div>
      </div>

      {/* Stats Cards - Using shared StatsCard component */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {platformStats.map(stat => (
          <StatsCard
            key={stat.title}
            title={stat.title}
            value={stat.value}
            icon={stat.icon}
            color={stat.color}
            change={stat.change}
            trend={stat.trend as 'up' | 'down'}
          />
        ))}
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        {/* Pending Actions */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <AlertTriangle className="h-5 w-5 text-amber-500" />
              Acciones Pendientes
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {pendingActions && pendingActions.length > 0 ? (
                pendingActions.map((action, index) => {
                  const Icon = getActionIcon(action.type);
                  return (
                    <Link
                      key={index}
                      href={action.href}
                      className="hover:bg-muted/50 flex items-center justify-between rounded-lg border p-4 transition-colors"
                    >
                      <div className="flex items-center gap-3">
                        <div className="bg-muted rounded-lg p-2">
                          <Icon className="text-muted-foreground h-5 w-5" />
                        </div>
                        <div>
                          <p className="text-foreground font-medium">{action.title}</p>
                          <p className="text-muted-foreground text-sm">{action.count} pendientes</p>
                        </div>
                      </div>
                      <div className="flex items-center gap-2">
                        <PriorityBadge priority={action.priority} />
                        <ChevronRight className="text-muted-foreground h-5 w-5" />
                      </div>
                    </Link>
                  );
                })
              ) : (
                <div className="text-muted-foreground py-8 text-center">
                  <CheckCircle className="mx-auto mb-2 h-8 w-8 text-primary" />
                  <p className="font-medium">No hay acciones pendientes</p>
                  <p className="mt-1 text-sm">
                    Verificaciones KYC, moderación de contenido y reportes aparecerán aquí
                  </p>
                </div>
              )}
            </div>
          </CardContent>
        </Card>

        {/* Recent Activity */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Clock className="h-5 w-5 text-blue-500" />
              Actividad Reciente
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {activity && activity.length > 0 ? (
                activity.map(item => {
                  const Icon = getActivityIcon(item.subjectType);
                  return (
                    <div key={item.id} className="flex items-start gap-3">
                      <div className="bg-muted rounded-lg p-2">
                        <Icon className="text-muted-foreground h-4 w-4" />
                      </div>
                      <div className="flex-1">
                        <p className="text-foreground text-sm font-medium">{item.action}</p>
                        <p className="text-muted-foreground text-sm">{item.subject}</p>
                        <p className="text-muted-foreground text-xs">
                          {new Date(item.timestamp).toLocaleString('es-DO', {
                            hour: '2-digit',
                            minute: '2-digit',
                            day: 'numeric',
                            month: 'short',
                          })}
                        </p>
                      </div>
                    </div>
                  );
                })
              ) : (
                <div className="text-muted-foreground py-8 text-center">
                  <Clock className="mx-auto mb-2 h-8 w-8 text-slate-400" />
                  <p className="font-medium">No hay actividad reciente</p>
                  <p className="mt-1 text-sm">
                    Registros, actualizaciones y pagos se mostrarán aquí
                  </p>
                </div>
              )}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Quick Links - Using shared QuickActionButton component */}
      <Card>
        <CardHeader>
          <CardTitle>Accesos Rápidos</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <QuickActionButton href="/admin/usuarios" icon={Users} label="Gestionar Usuarios" />
            <QuickActionButton href="/admin/vehiculos" icon={Car} label="Moderar Vehículos" />
            <QuickActionButton href="/admin/dealers" icon={Building} label="Gestionar Dealers" />
            <QuickActionButton href="/admin/reportes" icon={Flag} label="Ver Reportes" />
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
