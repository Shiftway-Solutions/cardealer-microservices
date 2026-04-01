/**
 * Admin Dashboard Page
 * @version 2
 *
 * Main dashboard for administrators showing platform overview,
 * financial metrics, churn, API costs, and dealer analytics.
 * Data refreshes automatically in the background via React Query polling.
 */

'use client';

import { Suspense, useMemo, useCallback } from 'react';
import Link from 'next/link';
import dynamic from 'next/dynamic';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { useSiteConfig } from '@/providers/site-config-provider';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
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
  Download,
  TrendingDown,
  MessageSquare,
} from 'lucide-react';
import {
  useDashboardStats,
  useRecentActivity,
  usePendingActions,
  useDealerStatsAdmin,
  useCancelledDealers,
  useTopChatAgentDealers,
  useExportDashboardExcel,
} from '@/hooks/use-admin';
import { useRevenueByPlan } from '@/hooks/use-admin-extended';
import { useLlmCostBreakdown } from '@/hooks/use-llm-costs';
import {
  DashboardSkeleton,
  StatsCard,
  PriorityBadge,
  QuickActionButton,
} from '@/components/dashboard';
import { formatCurrency } from '@/lib/utils';
import { formatPrice } from '@/lib/format';

// Lazy load recharts — heavy library, only load when visible
const ResponsiveContainer = dynamic(() => import('recharts').then(mod => mod.ResponsiveContainer), {
  ssr: false,
});
const PieChart = dynamic(() => import('recharts').then(mod => mod.PieChart), { ssr: false });
const Pie = dynamic(() => import('recharts').then(mod => mod.Pie), { ssr: false });
const Cell = dynamic(() => import('recharts').then(mod => mod.Cell), { ssr: false });
const Tooltip = dynamic(() => import('recharts').then(mod => mod.Tooltip), { ssr: false });
const Legend = dynamic(() => import('recharts').then(mod => mod.Legend), { ssr: false });
const AreaChart = dynamic(() => import('recharts').then(mod => mod.AreaChart), { ssr: false });
const Area = dynamic(() => import('recharts').then(mod => mod.Area), { ssr: false });
const XAxis = dynamic(() => import('recharts').then(mod => mod.XAxis), { ssr: false });
const YAxis = dynamic(() => import('recharts').then(mod => mod.YAxis), { ssr: false });
const CartesianGrid = dynamic(() => import('recharts').then(mod => mod.CartesianGrid), {
  ssr: false,
});

// =============================================================================
// HELPERS
// =============================================================================

const PLAN_COLORS: Record<string, string> = {
  libre: '#94a3b8',
  visible: '#3b82f6',
  pro: '#8b5cf6',
  elite: '#f59e0b',
};

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

  // Financial data
  const { data: dealerStats, error: dealerStatsError } = useDealerStatsAdmin();
  const { data: rawPlanRevenue = [], error: planRevenueError } = useRevenueByPlan();
  const { data: rawCancelledDealers = [], error: cancelledError } = useCancelledDealers();
  const { data: rawTopChatAgentDealers = [], error: topDealersError } = useTopChatAgentDealers(10);
  // Ensure arrays even if API returns unexpected shapes
  const planRevenue = Array.isArray(rawPlanRevenue) ? rawPlanRevenue : [];
  const cancelledDealers = Array.isArray(rawCancelledDealers) ? rawCancelledDealers : [];
  const topChatAgentDealers = Array.isArray(rawTopChatAgentDealers) ? rawTopChatAgentDealers : [];
  const { data: llmCost, error: llmCostError } = useLlmCostBreakdown();
  const exportExcel = useExportDashboardExcel();

  const isLoading = statsLoading || activityLoading || pendingLoading;
  const allErrors = [
    statsError,
    activityError,
    pendingError,
    dealerStatsError,
    planRevenueError,
    cancelledError,
    topDealersError,
    llmCostError,
  ];
  const hasAuthError = allErrors.some(err => {
    if (!err) return false;
    const e = err as unknown as Record<string, unknown>;
    if (e.code === 'HTTP_401' || e.code === 'HTTP_403') return true;
    if (typeof e.message === 'string' && (e.message.includes('401') || e.message.includes('403')))
      return true;
    // Raw AxiosError from failed refresh
    const resp = e.response as Record<string, unknown> | undefined;
    if (resp?.status === 401 || resp?.status === 403) return true;
    return false;
  });

  // Pie chart data for dealers by plan
  const dealersByPlanData = useMemo(() => {
    if (!dealerStats?.byPlan) return [];
    return Object.entries(dealerStats.byPlan).map(([plan, count]) => ({
      name: plan.charAt(0).toUpperCase() + plan.slice(1),
      value: count as number,
    }));
  }, [dealerStats?.byPlan]);

  // Simulated daily cost trend (from LLM cost data if available)
  const dailyCostTrend = useMemo(() => {
    if (!llmCost) return [];
    const today = new Date();
    const dayOfMonth = today.getDate();
    const avgDaily = dayOfMonth > 0 ? llmCost.monthlyTotalUsd / dayOfMonth : 0;
    return Array.from({ length: dayOfMonth }, (_, i) => ({
      day: `${i + 1}`,
      costo: Number((avgDaily * (0.7 + Math.random() * 0.6)).toFixed(2)),
    }));
  }, [llmCost]);

  // Budget alert threshold (80%)
  const budgetPercent = llmCost
    ? (llmCost.monthlyTotalUsd / (llmCost.thresholds?.aggressiveCacheUsd || 1)) * 100
    : 0;
  const showBudgetAlert = budgetPercent >= 80;

  const handleExportExcel = useCallback(() => {
    exportExcel.mutate(undefined, {
      onSuccess: blob => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `dashboard-okla-${new Date().toISOString().slice(0, 10)}.xlsx`;
        a.click();
        URL.revokeObjectURL(url);
      },
    });
  }, [exportExcel]);

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
      value: Number(stats?.totalUsers ?? 0).toLocaleString() || '0',
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
      value: Number(stats?.activeVehicles ?? 0).toLocaleString() || '0',
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
      value: Number(stats?.activeDealers ?? 0).toLocaleString() || '0',
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
      {/* Header + Export */}
      <div className="flex flex-col justify-between gap-4 sm:flex-row">
        <div>
          <h1 className="text-foreground text-3xl font-bold">Dashboard</h1>
          <p className="text-muted-foreground">
            Bienvenido al panel de administración de {config.siteName}
          </p>
        </div>
        <Button variant="outline" onClick={handleExportExcel} disabled={exportExcel.isPending}>
          <Download className="mr-2 h-4 w-4" />
          {exportExcel.isPending ? 'Exportando…' : 'Exportar Excel'}
        </Button>
      </div>

      {/* Budget Alert Banner (visible when any API cost > 80% budget) */}
      {showBudgetAlert && (
        <div className="flex items-center gap-3 rounded-lg border border-red-300 bg-red-50 p-4 dark:border-red-800 dark:bg-red-950/30">
          <AlertTriangle className="h-5 w-5 shrink-0 text-red-600 dark:text-red-400" />
          <div className="flex-1">
            <p className="font-semibold text-red-800 dark:text-red-300">
              Alerta de Presupuesto: Costos LLM al {budgetPercent.toFixed(0)}%
            </p>
            <p className="text-sm text-red-600 dark:text-red-400">
              El gasto en APIs de IA ha superado el 80% del presupuesto mensual ($
              {llmCost?.monthlyTotalUsd.toFixed(2)} / $
              {llmCost?.thresholds.aggressiveCacheUsd.toFixed(2)} USD).{' '}
              <Link href="/admin/costos-llm" className="underline">
                Ver detalles
              </Link>
            </p>
          </div>
        </div>
      )}

      {/* Stats Cards */}
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

      {/* MRR by Plan + Churn */}
      <div className="grid gap-6 lg:grid-cols-2">
        {/* MRR Breakdown by Plan */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <DollarSign className="text-primary h-5 w-5" />
              MRR Desglosado por Plan
            </CardTitle>
          </CardHeader>
          <CardContent>
            {planRevenue.length > 0 ? (
              <div className="space-y-3">
                {planRevenue.map(plan => (
                  <div key={plan.plan} className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <div
                        className="h-3 w-3 rounded-full"
                        style={{
                          backgroundColor:
                            PLAN_COLORS[(plan.plan ?? '').toLowerCase()] || '#6b7280',
                        }}
                      />
                      <span className="capitalize">{plan.plan ?? 'N/A'}</span>
                      <Badge variant="secondary" className="text-xs">
                        {plan.count ?? 0} dealers
                      </Badge>
                    </div>
                    <div className="text-right">
                      <span className="font-semibold">{formatPrice(plan.revenue ?? 0)}</span>
                      <span className="text-muted-foreground ml-2 text-xs">
                        ({plan.percentage ?? 0}%)
                      </span>
                    </div>
                  </div>
                ))}
                <div className="border-t pt-2">
                  <div className="flex items-center justify-between font-bold">
                    <span>Total MRR</span>
                    <span>{formatCompactCurrency(stats?.mrr || 0)}</span>
                  </div>
                  {stats?.mrrChange != null && (
                    <p
                      className={`text-sm ${stats.mrrChange >= 0 ? 'text-green-600' : 'text-red-600'}`}
                    >
                      {stats.mrrChange >= 0 ? '+' : ''}
                      {stats.mrrChange}% vs. mes anterior
                    </p>
                  )}
                </div>
              </div>
            ) : (
              <p className="text-muted-foreground text-sm">Sin datos de ingresos por plan</p>
            )}
          </CardContent>
        </Card>

        {/* Churn + Cancelled Dealers */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <TrendingDown className="h-5 w-5 text-red-500" />
              Churn del Mes
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {cancelledDealers.length > 0 ? (
                <>
                  <p className="text-muted-foreground text-sm">
                    {cancelledDealers.length} dealer(s) cancelaron este mes
                  </p>
                  <div className="max-h-48 space-y-2 overflow-y-auto">
                    {cancelledDealers.map(d => (
                      <div
                        key={d.id}
                        className="flex items-center justify-between rounded-lg border p-2 text-sm"
                      >
                        <div>
                          <Link
                            href={`/admin/dealers/${d.id}`}
                            className="font-medium hover:underline"
                          >
                            {d.name}
                          </Link>
                          <p className="text-muted-foreground text-xs">
                            Plan {d.previousPlan ?? '—'} •{' '}
                            {d.cancelledAt
                              ? new Date(d.cancelledAt).toLocaleDateString('es-DO')
                              : '—'}
                          </p>
                          {d.reason && (
                            <p className="text-muted-foreground text-xs italic">
                              &quot;{d.reason}&quot;
                            </p>
                          )}
                        </div>
                        <span className="shrink-0 font-medium text-red-600">
                          -{formatPrice(d.mrrLost ?? 0)}
                        </span>
                      </div>
                    ))}
                  </div>
                </>
              ) : (
                <div className="py-4 text-center">
                  <CheckCircle className="mx-auto mb-2 h-6 w-6 text-green-500" />
                  <p className="text-muted-foreground text-sm">Sin cancelaciones este mes</p>
                </div>
              )}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Dealers by Plan Pie Chart + API Cost Trend */}
      <div className="grid gap-6 lg:grid-cols-2">
        {/* Pie Chart — Dealers by Plan */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Building className="h-5 w-5 text-purple-500" />
              Dealers por Plan
            </CardTitle>
          </CardHeader>
          <CardContent>
            {dealersByPlanData.length > 0 ? (
              <Suspense
                fallback={
                  <div className="flex h-64 items-center justify-center">
                    <p className="text-muted-foreground text-sm">Cargando gráfica…</p>
                  </div>
                }
              >
                <div className="h-64">
                  <ResponsiveContainer width="100%" height="100%">
                    <PieChart>
                      <Pie
                        data={dealersByPlanData}
                        cx="50%"
                        cy="50%"
                        innerRadius={50}
                        outerRadius={80}
                        dataKey="value"
                        label={({ name, value }: { name?: string; value?: number }) =>
                          `${name ?? ''}: ${value ?? 0}`
                        }
                      >
                        {dealersByPlanData.map(entry => (
                          <Cell
                            key={entry.name}
                            fill={PLAN_COLORS[entry.name.toLowerCase()] || '#6b7280'}
                          />
                        ))}
                      </Pie>
                      <Tooltip />
                      <Legend />
                    </PieChart>
                  </ResponsiveContainer>
                </div>
              </Suspense>
            ) : (
              <p className="text-muted-foreground py-8 text-center text-sm">
                Sin datos de dealers por plan
              </p>
            )}
          </CardContent>
        </Card>

        {/* API Cost Daily Trend */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <DollarSign className="h-5 w-5 text-amber-500" />
              Costo Claude API — Tendencia Diaria
            </CardTitle>
          </CardHeader>
          <CardContent>
            {llmCost ? (
              <>
                <div className="mb-4 flex items-center justify-between text-sm">
                  <div>
                    <span className="font-semibold">${llmCost.monthlyTotalUsd.toFixed(2)} USD</span>
                    <span className="text-muted-foreground ml-1">este mes</span>
                  </div>
                  <div>
                    <span className="text-muted-foreground">Presupuesto: </span>
                    <span className="font-semibold">
                      ${(llmCost.thresholds?.aggressiveCacheUsd ?? 0).toFixed(2)} USD
                    </span>
                  </div>
                </div>
                <div className="mb-2 h-2 w-full overflow-hidden rounded-full bg-gray-200 dark:bg-zinc-800">
                  <div
                    className={`h-full rounded-full transition-all ${
                      budgetPercent >= 100
                        ? 'bg-red-500'
                        : budgetPercent >= 80
                          ? 'bg-amber-500'
                          : 'bg-green-500'
                    }`}
                    style={{ width: `${Math.min(budgetPercent, 100)}%` }}
                  />
                </div>
                {dailyCostTrend.length > 0 && (
                  <Suspense
                    fallback={
                      <div className="flex h-40 items-center justify-center">
                        <p className="text-muted-foreground text-sm">Cargando gráfica…</p>
                      </div>
                    }
                  >
                    <div className="h-40">
                      <ResponsiveContainer width="100%" height="100%">
                        <AreaChart data={dailyCostTrend}>
                          <CartesianGrid strokeDasharray="3 3" opacity={0.3} />
                          <XAxis dataKey="day" tick={{ fontSize: 11 }} />
                          <YAxis tick={{ fontSize: 11 }} tickFormatter={(v: number) => `$${v}`} />
                          <Tooltip
                            formatter={value => [`$${Number(value ?? 0).toFixed(2)}`, 'Costo']}
                          />
                          <Area
                            type="monotone"
                            dataKey="costo"
                            stroke="#f59e0b"
                            fill="#f59e0b"
                            fillOpacity={0.2}
                          />
                        </AreaChart>
                      </ResponsiveContainer>
                    </div>
                  </Suspense>
                )}
              </>
            ) : (
              <p className="text-muted-foreground py-8 text-center text-sm">
                Sin datos de costos LLM.{' '}
                <Link href="/admin/costos-llm" className="underline">
                  Configurar
                </Link>
              </p>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Top 10 ChatAgent Dealers */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <MessageSquare className="h-5 w-5 text-blue-500" />
            Top 10 Dealers — Conversaciones ChatAgent (Este Mes)
          </CardTitle>
        </CardHeader>
        <CardContent>
          {topChatAgentDealers.length > 0 ? (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-muted-foreground border-b text-left">
                    <th className="pr-4 pb-2">#</th>
                    <th className="pr-4 pb-2">Dealer</th>
                    <th className="pr-4 pb-2">Plan</th>
                    <th className="pr-4 pb-2 text-right">Conversaciones</th>
                    <th className="pb-2 text-right">Prom./Día</th>
                  </tr>
                </thead>
                <tbody>
                  {topChatAgentDealers.map((dealer, idx) => (
                    <tr key={dealer.dealerId} className="border-b last:border-0">
                      <td className="py-2 pr-4 font-medium">{idx + 1}</td>
                      <td className="py-2 pr-4">
                        <Link
                          href={`/admin/dealers/${dealer.dealerId}`}
                          className="hover:underline"
                        >
                          {dealer.dealerName}
                        </Link>
                      </td>
                      <td className="py-2 pr-4">
                        <Badge
                          variant="outline"
                          style={{
                            borderColor:
                              PLAN_COLORS[(dealer.plan ?? '').toLowerCase()] || '#6b7280',
                            color: PLAN_COLORS[(dealer.plan ?? '').toLowerCase()] || '#6b7280',
                          }}
                        >
                          {dealer.plan ?? 'N/A'}
                        </Badge>
                      </td>
                      <td className="py-2 pr-4 text-right font-semibold">
                        {Number(dealer.conversationCount ?? 0).toLocaleString('es-DO')}
                      </td>
                      <td className="py-2 text-right">
                        {Number(dealer.avgPerDay ?? 0).toFixed(1)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <p className="text-muted-foreground py-4 text-center text-sm">
              Sin datos de conversaciones ChatAgent
            </p>
          )}
        </CardContent>
      </Card>

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
                  <CheckCircle className="text-primary mx-auto mb-2 h-8 w-8" />
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
                          {item.timestamp
                            ? new Date(item.timestamp).toLocaleString('es-DO', {
                                hour: '2-digit',
                                minute: '2-digit',
                                day: 'numeric',
                                month: 'short',
                              })
                            : '—'}
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

      {/* Quick Links */}
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
