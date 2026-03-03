'use client';

import { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import { useAdvertiserReport } from '@/hooks/use-advertising';
import type { CampaignReportDetail } from '@/types/advertising';
import {
  BarChart3,
  Eye,
  MousePointerClick,
  DollarSign,
  Target,
  Zap,
  Download,
  Calendar,
  ArrowUpRight,
  ArrowDownRight,
  Minus,
  Activity,
  PieChart,
  FileText,
} from 'lucide-react';

// =============================================================================
// Dealer: Advertising Statistics & Reports Page
// =============================================================================
// Shows comprehensive advertising stats for the dealer/seller including:
// - Summary KPIs with period comparison
// - Daily impression/click/spend trend chart
// - Per-campaign performance breakdown
// - Export options (CSV)
// =============================================================================

export default function DealerAdStatsPage() {
  const [period, setPeriod] = useState<'7d' | '30d' | '90d'>('7d');
  // TODO: replace with real ownerId from auth
  const ownerId = 'current-user-id';

  const { data: report, isLoading, error } = useAdvertiserReport(ownerId, period);

  const formatDOP = (n: number) =>
    new Intl.NumberFormat('es-DO', {
      style: 'currency',
      currency: 'DOP',
      maximumFractionDigits: 0,
    }).format(n);
  const formatNum = (n: number) => new Intl.NumberFormat('es-DO').format(Math.round(n));
  const formatPct = (n: number) => `${n.toFixed(1)}%`;

  // CSV export
  const handleExport = () => {
    if (!report) return;
    const rows: string[] = [
      'Campaña,Vehículo,Estado,Placement,Impresiones,Clicks,CTR,Gastado,Presupuesto,CPC,CPM,Quality Score',
    ];
    for (const c of report.campaigns) {
      rows.push(
        [
          c.campaignId,
          `"${c.vehicleTitle}"`,
          c.status,
          c.placementType,
          c.impressions,
          c.clicks,
          c.ctr.toFixed(2),
          c.spent,
          c.totalBudget,
          c.costPerClick.toFixed(2),
          c.costPerMil.toFixed(2),
          c.qualityScore,
        ].join(',')
      );
    }
    const blob = new Blob([rows.join('\n')], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `okla-ad-report-${period}-${new Date().toISOString().split('T')[0]}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  };

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="h-8 w-64 animate-pulse rounded bg-gray-200" />
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {[...Array(4)].map((_, i) => (
            <Card key={i}>
              <CardContent className="p-6">
                <div className="h-16 animate-pulse rounded bg-gray-100" />
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    );
  }

  if (error || !report) {
    return (
      <Card>
        <CardContent className="p-8 text-center">
          <p className="text-muted-foreground">No se pudo cargar el reporte de publicidad.</p>
        </CardContent>
      </Card>
    );
  }

  const s = report.summary;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="flex items-center gap-2 text-2xl font-bold">
            <BarChart3 className="h-6 w-6 text-emerald-600" />
            Estadísticas de Publicidad
          </h1>
          <p className="text-muted-foreground mt-1 text-sm">
            Reporte de rendimiento para {report.ownerName}
          </p>
        </div>
        <div className="flex items-center gap-2">
          {/* Period selector */}
          <div className="flex rounded-lg border">
            {(['7d', '30d', '90d'] as const).map(p => (
              <button
                key={p}
                onClick={() => setPeriod(p)}
                className={`px-4 py-2 text-sm font-medium transition-colors ${
                  period === p
                    ? 'bg-emerald-600 text-white'
                    : 'text-muted-foreground hover:bg-gray-100'
                } ${p === '7d' ? 'rounded-l-lg' : p === '90d' ? 'rounded-r-lg' : ''}`}
              >
                {p === '7d' ? '7 días' : p === '30d' ? '30 días' : '90 días'}
              </button>
            ))}
          </div>
          <Button variant="outline" size="sm" onClick={handleExport}>
            <Download className="mr-1 h-4 w-4" />
            CSV
          </Button>
        </div>
      </div>

      {/* Period info */}
      <div className="text-muted-foreground flex items-center gap-2 text-xs">
        <Calendar className="h-3.5 w-3.5" />
        <span>
          {report.period.label}: {new Date(report.period.from).toLocaleDateString('es-DO')} —{' '}
          {new Date(report.period.to).toLocaleDateString('es-DO')}
        </span>
      </div>

      {/* KPI Cards */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <KpiCard
          title="Impresiones"
          value={formatNum(s.totalImpressions)}
          change={s.impressionsChange}
          icon={Eye}
          color="text-blue-600"
          bgColor="bg-blue-50"
        />
        <KpiCard
          title="Clicks"
          value={formatNum(s.totalClicks)}
          change={s.clicksChange}
          icon={MousePointerClick}
          color="text-emerald-600"
          bgColor="bg-emerald-50"
        />
        <KpiCard
          title="CTR"
          value={formatPct(s.overallCtr)}
          change={s.ctrChange}
          icon={Target}
          color="text-purple-600"
          bgColor="bg-purple-50"
        />
        <KpiCard
          title="Gastado"
          value={formatDOP(s.totalSpent)}
          change={s.spendChange}
          icon={DollarSign}
          color="text-amber-600"
          bgColor="bg-amber-50"
        />
      </div>

      {/* Secondary KPIs */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <MiniKpi
          label="CPC (Costo por Click)"
          value={formatDOP(s.costPerClick)}
          icon={MousePointerClick}
        />
        <MiniKpi label="CPM (Costo por 1K Imp.)" value={formatDOP(s.costPerMil)} icon={Eye} />
        <MiniKpi label="Leads Estimados" value={formatNum(s.estimatedLeads)} icon={Zap} />
        <MiniKpi label="Costo por Lead" value={formatDOP(s.costPerLead)} icon={Target} />
      </div>

      {/* Budget Utilization */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <PieChart className="h-4 w-4" />
            Utilización de Presupuesto
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="mb-2 flex items-center justify-between">
            <span className="text-muted-foreground text-sm">
              {formatDOP(s.totalSpent)} de {formatDOP(s.totalBudget)}
            </span>
            <span className="text-sm font-semibold">{formatPct(s.budgetUtilization)}</span>
          </div>
          <Progress value={Math.min(100, s.budgetUtilization)} className="h-3" />
          <div className="text-muted-foreground mt-2 flex justify-between text-xs">
            <span>
              {s.activeCampaigns} activas · {s.completedCampaigns} completadas
            </span>
            <span>Restante: {formatDOP(s.totalBudget - s.totalSpent)}</span>
          </div>
        </CardContent>
      </Card>

      {/* Daily Trend Chart (simplified bar chart) */}
      {s.dailyTrend.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-base">
              <Activity className="h-4 w-4" />
              Tendencia Diaria
            </CardTitle>
            <CardDescription>Impresiones y clicks por día</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="flex h-40 items-end gap-1">
              {s.dailyTrend.slice(-30).map((day, i) => {
                const maxViews = Math.max(...s.dailyTrend.slice(-30).map(d => d.views), 1);
                const heightPct = (day.views / maxViews) * 100;
                return (
                  <div key={i} className="group relative flex flex-1 flex-col items-center gap-0.5">
                    {/* Tooltip */}
                    <div className="absolute -top-16 left-1/2 z-10 hidden -translate-x-1/2 rounded bg-gray-900 px-2 py-1 text-[10px] whitespace-nowrap text-white group-hover:block">
                      <p className="font-semibold">
                        {new Date(day.date).toLocaleDateString('es-DO', {
                          month: 'short',
                          day: 'numeric',
                        })}
                      </p>
                      <p>
                        👁 {day.views} | 👆 {day.clicks}
                      </p>
                    </div>
                    {/* Bar */}
                    <div
                      className="w-full rounded-t bg-blue-200 transition-colors hover:bg-blue-400"
                      style={{ height: `${Math.max(2, heightPct)}%` }}
                    />
                    {/* Click overlay */}
                    <div
                      className="absolute bottom-0 w-full rounded-t bg-emerald-400"
                      style={{ height: `${Math.max(1, (day.clicks / maxViews) * 100)}%` }}
                    />
                  </div>
                );
              })}
            </div>
            <div className="text-muted-foreground mt-3 flex items-center justify-center gap-4 text-xs">
              <span className="flex items-center gap-1">
                <div className="h-2 w-2 rounded bg-blue-300" /> Impresiones
              </span>
              <span className="flex items-center gap-1">
                <div className="h-2 w-2 rounded bg-emerald-400" /> Clicks
              </span>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Per-Campaign Breakdown */}
      <div>
        <h2 className="mb-4 flex items-center gap-2 text-lg font-bold">
          <FileText className="h-5 w-5" />
          Rendimiento por Campaña
        </h2>
        <div className="space-y-4">
          {report.campaigns.map(campaign => (
            <CampaignCard
              key={campaign.campaignId}
              campaign={campaign}
              formatDOP={formatDOP}
              formatNum={formatNum}
            />
          ))}
        </div>
      </div>

      {/* Report footer */}
      <div className="text-muted-foreground flex items-center justify-between border-t pt-4 text-xs">
        <span>Generado: {new Date(report.generatedAt).toLocaleString('es-DO')}</span>
        <span>OKLA Marketplace · Reporte de Publicidad</span>
      </div>
    </div>
  );
}

// =============================================================================
// SUB-COMPONENTS
// =============================================================================

function KpiCard({
  title,
  value,
  change,
  icon: Icon,
  color,
  bgColor,
}: {
  title: string;
  value: string;
  change: number;
  icon: React.ElementType;
  color: string;
  bgColor: string;
}) {
  const ChangeIcon = change > 0 ? ArrowUpRight : change < 0 ? ArrowDownRight : Minus;
  const changeColor =
    change > 0 ? 'text-emerald-600' : change < 0 ? 'text-red-600' : 'text-gray-400';

  return (
    <Card>
      <CardContent className="p-5">
        <div className="mb-3 flex items-center justify-between">
          <div className={`flex h-10 w-10 items-center justify-center rounded-lg ${bgColor}`}>
            <Icon className={`h-5 w-5 ${color}`} />
          </div>
          {change !== 0 && (
            <div className={`flex items-center gap-0.5 text-xs font-medium ${changeColor}`}>
              <ChangeIcon className="h-3 w-3" />
              {Math.abs(change).toFixed(1)}%
            </div>
          )}
        </div>
        <p className="text-2xl font-bold tabular-nums">{value}</p>
        <p className="text-muted-foreground mt-1 text-xs">{title}</p>
      </CardContent>
    </Card>
  );
}

function MiniKpi({
  label,
  value,
  icon: Icon,
}: {
  label: string;
  value: string;
  icon: React.ElementType;
}) {
  return (
    <div className="bg-card flex items-center gap-3 rounded-lg border p-4">
      <Icon className="text-muted-foreground h-4 w-4" />
      <div>
        <p className="text-muted-foreground text-xs">{label}</p>
        <p className="text-sm font-bold tabular-nums">{value}</p>
      </div>
    </div>
  );
}

function CampaignCard({
  campaign: c,
  formatDOP,
  formatNum,
}: {
  campaign: CampaignReportDetail;
  formatDOP: (n: number) => string;
  formatNum: (n: number) => string;
}) {
  const statusColors: Record<string, string> = {
    Active: 'bg-emerald-100 text-emerald-800',
    Paused: 'bg-amber-100 text-amber-800',
    Completed: 'bg-blue-100 text-blue-800',
    Cancelled: 'bg-red-100 text-red-800',
    Expired: 'bg-gray-100 text-gray-800',
    PendingPayment: 'bg-yellow-100 text-yellow-800',
  };

  const budgetPct = c.totalBudget > 0 ? (c.spent / c.totalBudget) * 100 : 0;

  return (
    <Card>
      <CardContent className="p-5">
        <div className="mb-4 flex items-start justify-between">
          <div>
            <h3 className="font-semibold">{c.vehicleTitle}</h3>
            <div className="mt-1 flex items-center gap-2">
              <Badge className={statusColors[c.status] || 'bg-gray-100'} variant="secondary">
                {c.status}
              </Badge>
              <Badge variant="outline" className="text-xs">
                {c.placementType}
              </Badge>
              {c.qualityScore > 0 && (
                <span className="text-muted-foreground text-xs">QS: {c.qualityScore}</span>
              )}
            </div>
          </div>
          <div className="text-right">
            <p className="text-muted-foreground text-xs">Presupuesto</p>
            <p className="font-semibold">{formatDOP(c.totalBudget)}</p>
          </div>
        </div>

        {/* Campaign metrics grid */}
        <div className="mt-4 grid grid-cols-2 gap-4 sm:grid-cols-4">
          <div>
            <p className="text-muted-foreground text-xs">Impresiones</p>
            <p className="text-lg font-bold tabular-nums">{formatNum(c.impressions)}</p>
            <p className="text-muted-foreground text-[10px]">~{c.avgDailyImpressions}/día</p>
          </div>
          <div>
            <p className="text-muted-foreground text-xs">Clicks</p>
            <p className="text-lg font-bold tabular-nums">{formatNum(c.clicks)}</p>
            <p className="text-muted-foreground text-[10px]">~{c.avgDailyClicks}/día</p>
          </div>
          <div>
            <p className="text-muted-foreground text-xs">CTR</p>
            <p className="text-lg font-bold tabular-nums">{c.ctr.toFixed(2)}%</p>
          </div>
          <div>
            <p className="text-muted-foreground text-xs">CPC</p>
            <p className="text-lg font-bold tabular-nums">{formatDOP(c.costPerClick)}</p>
          </div>
        </div>

        {/* Budget bar */}
        <div className="mt-4">
          <div className="text-muted-foreground mb-1 flex justify-between text-xs">
            <span>Gastado: {formatDOP(c.spent)}</span>
            <span>Restante: {formatDOP(c.remainingBudget)}</span>
          </div>
          <Progress value={Math.min(100, budgetPct)} className="h-2" />
        </div>

        {/* Dates */}
        <div className="text-muted-foreground mt-3 flex items-center gap-2 text-xs">
          <Calendar className="h-3 w-3" />
          <span>
            {new Date(c.startDate).toLocaleDateString('es-DO')} —{' '}
            {new Date(c.endDate).toLocaleDateString('es-DO')}
          </span>
        </div>
      </CardContent>
    </Card>
  );
}
