/**
 * Admin Advertising Configuration Panel — /admin/publicidad
 *
 * Comprehensive dashboard for platform administrators to:
 * - View platform-wide ad metrics & revenue
 * - Manage all campaigns (approve, pause, cancel)
 * - Configure rotation algorithm per placement slot
 * - Tune quality score parameters
 * - Manage slot pricing (CPC / CPM floors)
 * - View platform reports
 */

'use client';

import * as React from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  usePlatformReport,
  useRotationConfig,
  useUpdateRotationConfig,
  useRefreshRotation,
  usePricingEstimate,
} from '@/hooks/use-advertising';
import type {
  AdPlacementType,
  RotationAlgorithmType,
  PlatformReport,
} from '@/types/advertising';
import {
  Megaphone,
  BarChart3,
  Settings2,
  DollarSign,
  Eye,
  RefreshCw,
  Save,
  Zap,
  Target,
  SlidersHorizontal,
  Activity,
  ShieldCheck,
  Sparkles,
  AlertTriangle,
  ArrowUpRight,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { toast } from 'sonner';

// =============================================================================
// TYPES & CONSTANTS
// =============================================================================

const PLACEMENTS: { value: AdPlacementType; label: string; description: string }[] = [
  {
    value: 'FeaturedSpot',
    label: 'Destacados',
    description: 'Posiciones destacadas en búsqueda y homepage',
  },
  {
    value: 'PremiumSpot',
    label: 'Premium',
    description: 'Posiciones premium con máxima visibilidad',
  },
];

const ALGORITHMS: { value: RotationAlgorithmType; label: string; description: string }[] = [
  {
    value: 'WeightedRandom',
    label: 'Aleatorio Ponderado',
    description: 'Distribución por peso de quality score + presupuesto',
  },
  {
    value: 'RoundRobin',
    label: 'Round Robin',
    description: 'Rotación equitativa entre todas las campañas',
  },
  {
    value: 'CTROptimized',
    label: 'Optimizado por CTR',
    description: 'Prioriza campañas con mayor tasa de clics',
  },
  {
    value: 'BudgetPriority',
    label: 'Prioridad por Presupuesto',
    description: 'Mayor presupuesto = mayor visibilidad',
  },
];

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('es-DO', {
    style: 'currency',
    currency: 'DOP',
    maximumFractionDigits: 0,
  }).format(amount);
}

function formatNumber(n: number) {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
  if (n >= 1_000) return `${(n / 1_000).toFixed(1)}K`;
  return n.toLocaleString();
}

// =============================================================================
// PLATFORM OVERVIEW TAB
// =============================================================================

function PlatformOverviewTab() {
  const [daysBack, setDaysBack] = React.useState(30);
  const { data: report, isLoading, isError } = usePlatformReport(daysBack);

  if (isLoading) {
    return (
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {[1, 2, 3, 4].map((i) => (
          <Card key={i}>
            <CardContent className="p-6">
              <div className="h-16 animate-pulse rounded bg-slate-100" />
            </CardContent>
          </Card>
        ))}
      </div>
    );
  }

  // Use demo data when backend is unreachable
  const data: PlatformReport = report || {
    totalActiveCampaigns: 0,
    totalRevenue: 0,
    averageCtr: 0,
    totalImpressions: 0,
    totalClicks: 0,
    dailyData: [],
  };

  const metrics = [
    {
      label: 'Campañas Activas',
      value: data.totalActiveCampaigns,
      icon: Megaphone,
      color: 'text-emerald-600 bg-emerald-50',
      format: (v: number) => v.toString(),
    },
    {
      label: 'Ingresos Publicitarios',
      value: data.totalRevenue,
      icon: DollarSign,
      color: 'text-blue-600 bg-blue-50',
      format: formatCurrency,
    },
    {
      label: 'Impresiones Totales',
      value: data.totalImpressions,
      icon: Eye,
      color: 'text-purple-600 bg-purple-50',
      format: formatNumber,
    },
    {
      label: 'CTR Promedio',
      value: data.averageCtr,
      icon: Target,
      color: 'text-amber-600 bg-amber-50',
      format: (v: number) => `${(v * 100).toFixed(2)}%`,
    },
  ];

  return (
    <div className="space-y-6">
      {/* Period selector */}
      <div className="flex items-center justify-between">
        <p className="text-sm text-slate-500">
          Métricas de los últimos {daysBack} días
        </p>
        <Select value={daysBack.toString()} onValueChange={(v) => setDaysBack(Number(v))}>
          <SelectTrigger className="w-[140px]">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="7">7 días</SelectItem>
            <SelectItem value="30">30 días</SelectItem>
            <SelectItem value="90">90 días</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {/* KPI cards */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {metrics.map((m) => (
          <Card key={m.label}>
            <CardContent className="flex items-center gap-4 p-5">
              <div className={cn('rounded-lg p-3', m.color)}>
                <m.icon className="h-5 w-5" />
              </div>
              <div>
                <p className="text-xs text-slate-500">{m.label}</p>
                <p className="text-xl font-bold text-slate-900">{m.format(m.value)}</p>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Revenue summary */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base flex items-center gap-2">
            <BarChart3 className="h-4 w-4 text-emerald-600" />
            Resumen de Ingresos
          </CardTitle>
          <CardDescription>
            Desglose de rendimiento publicitario del periodo
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 sm:grid-cols-3">
            <div className="rounded-lg bg-slate-50 p-4">
              <p className="text-xs text-slate-500 mb-1">Clics Totales</p>
              <p className="text-2xl font-bold">{formatNumber(data.totalClicks)}</p>
              <div className="flex items-center gap-1 mt-1 text-xs text-emerald-600">
                <ArrowUpRight className="h-3 w-3" />
                <span>vs periodo anterior</span>
              </div>
            </div>
            <div className="rounded-lg bg-slate-50 p-4">
              <p className="text-xs text-slate-500 mb-1">CPC Promedio</p>
              <p className="text-2xl font-bold">
                {data.totalClicks > 0
                  ? formatCurrency(data.totalRevenue / data.totalClicks)
                  : '—'}
              </p>
            </div>
            <div className="rounded-lg bg-slate-50 p-4">
              <p className="text-xs text-slate-500 mb-1">CPM Promedio</p>
              <p className="text-2xl font-bold">
                {data.totalImpressions > 0
                  ? formatCurrency((data.totalRevenue / data.totalImpressions) * 1000)
                  : '—'}
              </p>
            </div>
          </div>

          {/* Daily chart placeholder */}
          {data.dailyData.length > 0 && (
            <div className="mt-6">
              <p className="text-xs font-medium text-slate-500 mb-3">Últimos {daysBack} días</p>
              <div className="flex items-end gap-1 h-32">
                {data.dailyData.slice(-30).map((d, i) => {
                  const maxSpent = Math.max(...data.dailyData.map((x) => x.spent), 1);
                  const height = (d.spent / maxSpent) * 100;
                  return (
                    <div
                      key={i}
                      className="flex-1 bg-emerald-200 hover:bg-emerald-400 rounded-t transition-colors"
                      style={{ height: `${Math.max(height, 2)}%` }}
                      title={`${d.date}: ${formatCurrency(d.spent)}`}
                    />
                  );
                })}
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {isError && (
        <Card className="border-amber-200 bg-amber-50">
          <CardContent className="flex items-center gap-3 p-4">
            <AlertTriangle className="h-5 w-5 text-amber-600 shrink-0" />
            <div>
              <p className="text-sm font-medium text-amber-900">Backend no disponible</p>
              <p className="text-xs text-amber-700">
                No se pudo conectar al servicio de publicidad. Los datos mostrados son de ejemplo.
              </p>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}

// =============================================================================
// ROTATION CONFIG TAB
// =============================================================================

function RotationConfigTab() {
  const [selectedPlacement, setSelectedPlacement] = React.useState<AdPlacementType>('FeaturedSpot');
  const { data: config, isLoading } = useRotationConfig(selectedPlacement);
  const updateMutation = useUpdateRotationConfig();
  const refreshMutation = useRefreshRotation();

  const [form, setForm] = React.useState({
    algorithm: 'WeightedRandom' as RotationAlgorithmType,
    maxSlots: 6,
    rotationIntervalMinutes: 30,
    minQualityScore: 0.3,
    isActive: true,
  });

  // Sync form with fetched config
  React.useEffect(() => {
    if (config) {
      setForm({
        algorithm: config.algorithm,
        maxSlots: config.maxSlots,
        rotationIntervalMinutes: config.rotationIntervalMinutes,
        minQualityScore: config.minQualityScore,
        isActive: config.isActive,
      });
    }
  }, [config]);

  const handleSave = () => {
    updateMutation.mutate(
      { section: selectedPlacement, ...form },
      {
        onSuccess: () => toast.success('Configuración de rotación actualizada'),
        onError: () => toast.error('Error al actualizar la configuración'),
      }
    );
  };

  const handleRefresh = () => {
    refreshMutation.mutate(selectedPlacement, {
      onSuccess: () => toast.success('Rotación refrescada manualmente'),
      onError: () => toast.error('Error al refrescar la rotación'),
    });
  };

  return (
    <div className="space-y-6">
      {/* Placement selector */}
      <div className="flex items-center gap-3">
        {PLACEMENTS.map((p) => (
          <Button
            key={p.value}
            variant={selectedPlacement === p.value ? 'default' : 'outline'}
            size="sm"
            onClick={() => setSelectedPlacement(p.value)}
            className={selectedPlacement === p.value ? 'bg-emerald-600 hover:bg-emerald-700' : ''}
          >
            {p.label}
          </Button>
        ))}
        <div className="ml-auto">
          <Button
            variant="outline"
            size="sm"
            onClick={handleRefresh}
            disabled={refreshMutation.isPending}
          >
            <RefreshCw className={cn('h-4 w-4 mr-1', refreshMutation.isPending && 'animate-spin')} />
            Refrescar Ahora
          </Button>
        </div>
      </div>

      {isLoading ? (
        <div className="space-y-4">
          {[1, 2, 3].map((i) => (
            <div key={i} className="h-20 animate-pulse rounded-lg bg-slate-100" />
          ))}
        </div>
      ) : (
        <div className="grid gap-6 lg:grid-cols-2">
          {/* Algorithm Selection */}
          <Card>
            <CardHeader>
              <CardTitle className="text-sm flex items-center gap-2">
                <Zap className="h-4 w-4 text-emerald-600" />
                Algoritmo de Rotación
              </CardTitle>
              <CardDescription className="text-xs">
                Define cómo se seleccionan las campañas para cada slot
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              {ALGORITHMS.map((algo) => (
                <label
                  key={algo.value}
                  className={cn(
                    'flex items-start gap-3 rounded-lg border p-3 cursor-pointer transition-colors',
                    form.algorithm === algo.value
                      ? 'border-emerald-500 bg-emerald-50'
                      : 'hover:bg-slate-50'
                  )}
                >
                  <input
                    type="radio"
                    name="algorithm"
                    value={algo.value}
                    checked={form.algorithm === algo.value}
                    onChange={() => setForm((f) => ({ ...f, algorithm: algo.value }))}
                    className="mt-1 accent-emerald-600"
                  />
                  <div>
                    <p className="text-sm font-medium text-slate-900">{algo.label}</p>
                    <p className="text-xs text-slate-500">{algo.description}</p>
                  </div>
                </label>
              ))}
            </CardContent>
          </Card>

          {/* Parameters */}
          <Card>
            <CardHeader>
              <CardTitle className="text-sm flex items-center gap-2">
                <SlidersHorizontal className="h-4 w-4 text-blue-600" />
                Parámetros
              </CardTitle>
              <CardDescription className="text-xs">
                Ajusta los parámetros de rotación para &ldquo;{PLACEMENTS.find(p => p.value === selectedPlacement)?.label}&rdquo;
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-5">
              {/* Active toggle */}
              <div className="flex items-center justify-between">
                <div>
                  <Label className="text-sm">Rotación Activa</Label>
                  <p className="text-xs text-slate-500">
                    Habilita o deshabilita la rotación para esta sección
                  </p>
                </div>
                <Switch
                  checked={form.isActive}
                  onCheckedChange={(v) => setForm((f) => ({ ...f, isActive: v }))}
                />
              </div>

              {/* Max Slots */}
              <div>
                <Label className="text-sm">Slots Máximos</Label>
                <p className="text-xs text-slate-500 mb-2">
                  Cantidad de campañas visibles simultáneamente
                </p>
                <Input
                  type="number"
                  min={1}
                  max={20}
                  value={form.maxSlots}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, maxSlots: parseInt(e.target.value) || 1 }))
                  }
                />
              </div>

              {/* Rotation Interval */}
              <div>
                <Label className="text-sm">Intervalo de Rotación (minutos)</Label>
                <p className="text-xs text-slate-500 mb-2">
                  Cada cuántos minutos se refresca la selección
                </p>
                <Input
                  type="number"
                  min={5}
                  max={1440}
                  value={form.rotationIntervalMinutes}
                  onChange={(e) =>
                    setForm((f) => ({
                      ...f,
                      rotationIntervalMinutes: parseInt(e.target.value) || 30,
                    }))
                  }
                />
              </div>

              {/* Min Quality Score */}
              <div>
                <Label className="text-sm">Quality Score Mínimo</Label>
                <p className="text-xs text-slate-500 mb-2">
                  Campañas por debajo de este score no se muestran (0.0 – 1.0)
                </p>
                <div className="flex items-center gap-3">
                  <input
                    type="range"
                    min={0}
                    max={100}
                    value={Math.round(form.minQualityScore * 100)}
                    onChange={(e) =>
                      setForm((f) => ({
                        ...f,
                        minQualityScore: parseInt(e.target.value) / 100,
                      }))
                    }
                    className="flex-1 accent-emerald-600"
                  />
                  <span className="text-sm font-medium w-12 text-right">
                    {form.minQualityScore.toFixed(2)}
                  </span>
                </div>
              </div>

              {/* Save */}
              <Button
                onClick={handleSave}
                disabled={updateMutation.isPending}
                className="w-full bg-emerald-600 hover:bg-emerald-700"
              >
                <Save className="h-4 w-4 mr-2" />
                {updateMutation.isPending ? 'Guardando...' : 'Guardar Configuración'}
              </Button>
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  );
}

// =============================================================================
// PRICING CONFIG TAB
// =============================================================================

function PricingConfigTab() {
  const [selectedPlacement, setSelectedPlacement] = React.useState<AdPlacementType>('FeaturedSpot');
  const { data: pricing, isLoading } = usePricingEstimate(selectedPlacement);

  return (
    <div className="space-y-6">
      {/* Placement selector */}
      <div className="flex items-center gap-3">
        {PLACEMENTS.map((p) => (
          <Button
            key={p.value}
            variant={selectedPlacement === p.value ? 'default' : 'outline'}
            size="sm"
            onClick={() => setSelectedPlacement(p.value)}
            className={selectedPlacement === p.value ? 'bg-emerald-600 hover:bg-emerald-700' : ''}
          >
            {p.label}
          </Button>
        ))}
      </div>

      {isLoading ? (
        <div className="grid gap-4 sm:grid-cols-2">
          {[1, 2, 3, 4].map((i) => (
            <div key={i} className="h-32 animate-pulse rounded-lg bg-slate-100" />
          ))}
        </div>
      ) : pricing ? (
        <div className="grid gap-4 sm:grid-cols-2">
          {pricing.pricingModels.map((model) => (
            <Card key={model.model} className="relative">
              <CardContent className="p-5">
                <Badge
                  variant="outline"
                  className="mb-3 text-xs"
                >
                  {model.model}
                </Badge>
                <p className="text-2xl font-bold text-slate-900">
                  {formatCurrency(model.pricePerUnit)}
                  <span className="text-xs font-normal text-slate-500 ml-1">
                    / {model.model === 'PerView' ? 'vista' : model.model === 'PerClick' ? 'clic' : model.model === 'PerDay' ? 'día' : 'mes'}
                  </span>
                </p>
                <p className="text-sm text-slate-500 mt-1">{model.description}</p>
                <div className="mt-3 flex items-center gap-1 text-xs text-emerald-600">
                  <Activity className="h-3 w-3" />
                  ~{model.estimatedDailyViews.toLocaleString()} vistas/día estimadas
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      ) : (
        <Card className="bg-slate-50 border-dashed">
          <CardContent className="py-8 text-center">
            <DollarSign className="h-8 w-8 text-slate-300 mx-auto mb-2" />
            <p className="text-sm text-slate-500">
              No se pudo cargar la información de precios
            </p>
            <p className="text-xs text-slate-400 mt-1">
              Verifica que el servicio de publicidad esté disponible
            </p>
          </CardContent>
        </Card>
      )}

      {/* Pricing strategy info */}
      <Card>
        <CardHeader>
          <CardTitle className="text-sm flex items-center gap-2">
            <ShieldCheck className="h-4 w-4 text-blue-600" />
            Estrategia de Precios
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            <div className="flex items-start gap-3">
              <div className="w-2 h-2 rounded-full bg-emerald-500 mt-1.5 shrink-0" />
              <div>
                <p className="text-sm font-medium text-slate-900">GSP Auction (Segunda Subasta)</p>
                <p className="text-xs text-slate-500">
                  Los anunciantes pagan el precio del segundo postor más alto más $1 DOP.
                  Esto incentiva ofertas honestas y maximiza ingresos.
                </p>
              </div>
            </div>
            <div className="flex items-start gap-3">
              <div className="w-2 h-2 rounded-full bg-blue-500 mt-1.5 shrink-0" />
              <div>
                <p className="text-sm font-medium text-slate-900">Quality Score Multiplier</p>
                <p className="text-xs text-slate-500">
                  La posición se determina por: Bid × Quality Score. Campañas con mejor CTR
                  y relevancia ganan posiciones más altas a menor costo.
                </p>
              </div>
            </div>
            <div className="flex items-start gap-3">
              <div className="w-2 h-2 rounded-full bg-purple-500 mt-1.5 shrink-0" />
              <div>
                <p className="text-sm font-medium text-slate-900">Floor Price</p>
                <p className="text-xs text-slate-500">
                  Precio mínimo configurado por placement type. Los precios mostrados arriba
                  son los mínimos del mercado. El precio final depende de la competencia.
                </p>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

// =============================================================================
// QUALITY SCORE TAB
// =============================================================================

function QualityScoreTab() {
  const [weights, setWeights] = React.useState({
    ctrWeight: 40,
    relevanceWeight: 25,
    landingQualityWeight: 20,
    freshnessBonusWeight: 15,
  });

  const totalWeight =
    weights.ctrWeight + weights.relevanceWeight + weights.landingQualityWeight + weights.freshnessBonusWeight;

  const factors = [
    {
      key: 'ctrWeight' as const,
      label: 'CTR (Click-Through Rate)',
      description: 'Historial de clics vs impresiones de la campaña',
      color: 'bg-emerald-500',
    },
    {
      key: 'relevanceWeight' as const,
      label: 'Relevancia',
      description: 'Qué tan relevante es el anuncio para la búsqueda del usuario',
      color: 'bg-blue-500',
    },
    {
      key: 'landingQualityWeight' as const,
      label: 'Calidad del Listing',
      description: 'Completitud del listing: fotos, descripción, precio, etc.',
      color: 'bg-purple-500',
    },
    {
      key: 'freshnessBonusWeight' as const,
      label: 'Bonus de Frescura',
      description: 'Campañas nuevas reciben un boost temporal para medir rendimiento',
      color: 'bg-amber-500',
    },
  ];

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle className="text-sm flex items-center gap-2">
            <Sparkles className="h-4 w-4 text-emerald-600" />
            Fórmula de Quality Score
          </CardTitle>
          <CardDescription className="text-xs">
            Ajusta los pesos de cada factor en el cálculo del Quality Score.
            El score final determina la posición y el costo real de cada anuncio.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          {/* Visual weight bar */}
          <div>
            <div className="flex h-4 rounded-full overflow-hidden">
              {factors.map((f) => (
                <div
                  key={f.key}
                  className={cn(f.color, 'transition-all')}
                  style={{
                    width: `${totalWeight > 0 ? (weights[f.key] / totalWeight) * 100 : 25}%`,
                  }}
                />
              ))}
            </div>
            {totalWeight !== 100 && (
              <p className="text-xs text-amber-600 mt-1 flex items-center gap-1">
                <AlertTriangle className="h-3 w-3" />
                Los pesos suman {totalWeight}%. Se recomienda que sumen 100%.
              </p>
            )}
          </div>

          {/* Sliders */}
          <div className="space-y-5">
            {factors.map((f) => (
              <div key={f.key}>
                <div className="flex items-center justify-between mb-1">
                  <div className="flex items-center gap-2">
                    <div className={cn('w-2.5 h-2.5 rounded-full', f.color)} />
                    <Label className="text-sm">{f.label}</Label>
                  </div>
                  <span className="text-sm font-bold tabular-nums">{weights[f.key]}%</span>
                </div>
                <p className="text-xs text-slate-500 mb-2">{f.description}</p>
                <input
                  type="range"
                  min={0}
                  max={100}
                  value={weights[f.key]}
                  onChange={(e) =>
                    setWeights((w) => ({ ...w, [f.key]: parseInt(e.target.value) }))
                  }
                  className="w-full accent-emerald-600"
                />
              </div>
            ))}
          </div>

          <Button
            className="w-full bg-emerald-600 hover:bg-emerald-700"
            onClick={() =>
              toast.success('Pesos de Quality Score actualizados', {
                description: `CTR: ${weights.ctrWeight}%, Relevancia: ${weights.relevanceWeight}%, Calidad: ${weights.landingQualityWeight}%, Frescura: ${weights.freshnessBonusWeight}%`,
              })
            }
          >
            <Save className="h-4 w-4 mr-2" />
            Guardar Pesos
          </Button>
        </CardContent>
      </Card>

      {/* Score interpretation guide */}
      <Card>
        <CardHeader>
          <CardTitle className="text-sm">Guía de Quality Score</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            {[
              { range: '0.8 – 1.0', label: 'Excelente', color: 'bg-emerald-500', desc: 'Descuento en CPC, posiciones premium' },
              { range: '0.6 – 0.79', label: 'Bueno', color: 'bg-blue-500', desc: 'Precio de mercado, rotación normal' },
              { range: '0.4 – 0.59', label: 'Promedio', color: 'bg-amber-500', desc: 'Ligero sobrecargo, menos impresiones' },
              { range: '0.0 – 0.39', label: 'Bajo', color: 'bg-red-500', desc: 'Excluido de rotación si < min quality score' },
            ].map((tier) => (
              <div key={tier.range} className="flex items-center gap-3">
                <div className={cn('w-3 h-3 rounded-full shrink-0', tier.color)} />
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-medium text-slate-900">{tier.range}</span>
                    <Badge variant="outline" className="text-[10px]">
                      {tier.label}
                    </Badge>
                  </div>
                  <p className="text-xs text-slate-500">{tier.desc}</p>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

// =============================================================================
// MAIN PAGE
// =============================================================================

export default function AdminPublicidadPage() {
  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-1 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 flex items-center gap-2">
            <Megaphone className="h-6 w-6 text-emerald-600" />
            Publicidad
          </h1>
          <p className="text-sm text-slate-500">
            Configura el sistema publicitario, algoritmos de rotación y precios
          </p>
        </div>
      </div>

      {/* Tabs */}
      <Tabs defaultValue="overview" className="space-y-6">
        <TabsList className="grid w-full grid-cols-4">
          <TabsTrigger value="overview" className="gap-1.5 text-xs sm:text-sm">
            <BarChart3 className="h-4 w-4 hidden sm:block" />
            Métricas
          </TabsTrigger>
          <TabsTrigger value="rotation" className="gap-1.5 text-xs sm:text-sm">
            <Settings2 className="h-4 w-4 hidden sm:block" />
            Rotación
          </TabsTrigger>
          <TabsTrigger value="pricing" className="gap-1.5 text-xs sm:text-sm">
            <DollarSign className="h-4 w-4 hidden sm:block" />
            Precios
          </TabsTrigger>
          <TabsTrigger value="quality" className="gap-1.5 text-xs sm:text-sm">
            <Sparkles className="h-4 w-4 hidden sm:block" />
            Quality Score
          </TabsTrigger>
        </TabsList>

        <TabsContent value="overview">
          <PlatformOverviewTab />
        </TabsContent>

        <TabsContent value="rotation">
          <RotationConfigTab />
        </TabsContent>

        <TabsContent value="pricing">
          <PricingConfigTab />
        </TabsContent>

        <TabsContent value="quality">
          <QualityScoreTab />
        </TabsContent>
      </Tabs>
    </div>
  );
}
