'use client';

import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import type { PredictedLead, LeadListResponse, LeadScoreLevel } from '@/types/analytics';
import {
  Users,
  Flame,
  ThermometerSun,
  Snowflake,
  Target,
  Car,
  Search,
  Smartphone,
  Monitor,
  Tablet,
  Clock,
  Eye,
  Filter,
  BarChart3,
  Brain,
  Mail,
  Zap,
  ChevronDown,
  ChevronUp,
} from 'lucide-react';

// =============================================================================
// Admin Leads Dashboard — AI-Powered Buyer Prediction
// =============================================================================

export default function AdminLeadsPage() {
  const [filter, setFilter] = useState<string>('all');
  const [sortBy, setSortBy] = useState<string>('score');
  const [expandedLead, setExpandedLead] = useState<string | null>(null);

  const { data, isLoading, error } = useQuery<LeadListResponse>({
    queryKey: ['admin-leads', filter, sortBy],
    queryFn: async () => {
      const res = await fetch(`/api/analytics/leads?filter=${filter}&sort=${sortBy}&limit=100`);
      const json = await res.json();
      if (!json.success) throw new Error(json.error);
      return json.data;
    },
    staleTime: 30_000,
  });

  const formatDOP = (n: number) =>
    new Intl.NumberFormat('es-DO', {
      style: 'currency',
      currency: 'DOP',
      maximumFractionDigits: 0,
    }).format(n);

  if (isLoading) {
    return (
      <div className="mx-auto max-w-7xl space-y-6 px-4 py-8">
        <div className="h-8 w-64 animate-pulse rounded bg-gray-200" />
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {[...Array(4)].map((_, i) => (
            <Card key={i}>
              <CardContent className="h-24 animate-pulse" />
            </Card>
          ))}
        </div>
      </div>
    );
  }

  if (error || !data) {
    return (
      <div className="mx-auto max-w-3xl px-4 py-8">
        <Card>
          <CardContent className="p-8 text-center">
            <p className="text-muted-foreground">No se pudieron cargar los leads predictivos.</p>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-7xl space-y-6 px-4 py-8">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="flex items-center gap-2 text-2xl font-bold">
            <Brain className="h-6 w-6 text-purple-600" />
            Leads Predictivos (IA)
          </h1>
          <p className="text-muted-foreground mt-1 text-sm">
            Análisis de comportamiento de compradores potenciales con predicción de conversión
          </p>
        </div>
        <Badge variant="outline" className="text-xs">
          <Zap className="mr-1 h-3 w-3" />
          AI-Powered
        </Badge>
      </div>

      {/* Overview Cards */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
        <OverviewCard
          title="Total Leads"
          value={data.totalCount}
          icon={Users}
          color="text-gray-700"
          bgColor="bg-gray-100"
        />
        <OverviewCard
          title="Leads Calientes"
          value={data.hotCount}
          icon={Flame}
          color="text-red-600"
          bgColor="bg-red-50"
          onClick={() => setFilter(filter === 'hot' ? 'all' : 'hot')}
          active={filter === 'hot'}
        />
        <OverviewCard
          title="Leads Tibios"
          value={data.warmCount}
          icon={ThermometerSun}
          color="text-amber-600"
          bgColor="bg-amber-50"
          onClick={() => setFilter(filter === 'warm' ? 'all' : 'warm')}
          active={filter === 'warm'}
        />
        <OverviewCard
          title="Leads Fríos"
          value={data.coldCount}
          icon={Snowflake}
          color="text-blue-600"
          bgColor="bg-blue-50"
          onClick={() => setFilter(filter === 'cold' ? 'all' : 'cold')}
          active={filter === 'cold'}
        />
        <OverviewCard
          title="Score Promedio"
          value={data.avgScore}
          icon={Target}
          color="text-purple-600"
          bgColor="bg-purple-50"
          suffix="/100"
        />
      </div>

      {/* Filters & Sort */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Filter className="text-muted-foreground h-4 w-4" />
          <span className="text-muted-foreground text-sm">Ordenar por:</span>
          <div className="flex rounded-lg border">
            {[
              { key: 'score', label: 'Puntuación' },
              { key: 'recent', label: 'Más recientes' },
              { key: 'contacts', label: 'Más contactos' },
            ].map(({ key, label }) => (
              <button
                key={key}
                onClick={() => setSortBy(key)}
                className={`px-3 py-1.5 text-xs font-medium transition-colors ${
                  sortBy === key
                    ? 'bg-purple-600 text-white'
                    : 'text-muted-foreground hover:bg-gray-100'
                } ${key === 'score' ? 'rounded-l-lg' : key === 'contacts' ? 'rounded-r-lg' : ''}`}
              >
                {label}
              </button>
            ))}
          </div>
        </div>
        <span className="text-muted-foreground text-sm">{data.leads.length} leads mostrados</span>
      </div>

      {/* Lead Cards */}
      <div className="space-y-3">
        {data.leads.map(lead => (
          <LeadCard
            key={lead.visitorId}
            lead={lead}
            formatDOP={formatDOP}
            expanded={expandedLead === lead.visitorId}
            onToggle={() =>
              setExpandedLead(expandedLead === lead.visitorId ? null : lead.visitorId)
            }
          />
        ))}
      </div>

      {data.leads.length === 0 && (
        <Card>
          <CardContent className="p-8 text-center">
            <Users className="text-muted-foreground mx-auto mb-4 h-12 w-12" />
            <p className="text-muted-foreground">No hay leads con el filtro seleccionado.</p>
          </CardContent>
        </Card>
      )}

      {/* How it works */}
      <Card className="border-purple-200 bg-gradient-to-br from-purple-50 to-indigo-50">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <Brain className="h-5 w-5 text-purple-600" />
            ¿Cómo funciona el Lead Scoring?
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 text-sm sm:grid-cols-2 lg:grid-cols-4">
            <div className="space-y-1">
              <p className="font-semibold text-purple-700">🎯 Engagement (0-25)</p>
              <p className="text-muted-foreground">
                Páginas vistas, vehículos explorados, recencia de visitas, duración de sesión
              </p>
            </div>
            <div className="space-y-1">
              <p className="font-semibold text-purple-700">🔍 Intención (0-25)</p>
              <p className="text-muted-foreground">
                Búsquedas realizadas, filtros aplicados, galería, 360°, comparaciones
              </p>
            </div>
            <div className="space-y-1">
              <p className="font-semibold text-purple-700">📞 Contacto (0-30)</p>
              <p className="text-muted-foreground">
                Llamadas, WhatsApp, mensajes, test drive, negociación de precio
              </p>
            </div>
            <div className="space-y-1">
              <p className="font-semibold text-purple-700">💰 Financiero (0-20)</p>
              <p className="text-muted-foreground">
                Calculadora financiera, cotización seguro, página de pago, favoritos
              </p>
            </div>
          </div>
          <p className="text-muted-foreground mt-4 text-xs">
            La probabilidad de conversión se calcula mediante una función logística: P = 1 / (1 +
            e^(-0.08 × (score - 50)))
          </p>
        </CardContent>
      </Card>
    </div>
  );
}

// =============================================================================
// SUB-COMPONENTS
// =============================================================================

function OverviewCard({
  title,
  value,
  icon: Icon,
  color,
  bgColor,
  suffix,
  onClick,
  active,
}: {
  title: string;
  value: number;
  icon: React.ElementType;
  color: string;
  bgColor: string;
  suffix?: string;
  onClick?: () => void;
  active?: boolean;
}) {
  return (
    <Card
      className={`cursor-pointer transition-all ${active ? 'ring-2 ring-purple-400' : ''} ${onClick ? 'hover:shadow-md' : ''}`}
      onClick={onClick}
    >
      <CardContent className="flex items-center gap-3 p-4">
        <div className={`flex h-10 w-10 items-center justify-center rounded-lg ${bgColor}`}>
          <Icon className={`h-5 w-5 ${color}`} />
        </div>
        <div>
          <p className="text-2xl font-bold tabular-nums">
            {value}
            {suffix && <span className="text-muted-foreground text-sm font-normal">{suffix}</span>}
          </p>
          <p className="text-muted-foreground text-xs">{title}</p>
        </div>
      </CardContent>
    </Card>
  );
}

function LeadCard({
  lead,
  formatDOP,
  expanded,
  onToggle,
}: {
  lead: PredictedLead;
  formatDOP: (n: number) => string;
  expanded: boolean;
  onToggle: () => void;
}) {
  const levelConfig: Record<
    LeadScoreLevel,
    { color: string; bgColor: string; label: string; icon: React.ElementType }
  > = {
    hot: { color: 'text-red-700', bgColor: 'bg-red-100', label: '🔥 Caliente', icon: Flame },
    warm: {
      color: 'text-amber-700',
      bgColor: 'bg-amber-100',
      label: '☀️ Tibio',
      icon: ThermometerSun,
    },
    cold: { color: 'text-blue-700', bgColor: 'bg-blue-100', label: '❄️ Frío', icon: Snowflake },
    inactive: { color: 'text-gray-500', bgColor: 'bg-gray-100', label: '⚪ Inactivo', icon: Users },
  };

  const cfg = levelConfig[lead.level];
  const DeviceIcon =
    lead.device?.deviceType === 'mobile'
      ? Smartphone
      : lead.device?.deviceType === 'tablet'
        ? Tablet
        : Monitor;

  const timeAgo = getTimeAgo(lead.lastSeen);

  return (
    <Card className="transition-shadow hover:shadow-sm">
      <CardContent className="p-4">
        {/* Main row */}
        <div className="flex items-center gap-4">
          {/* Score circle */}
          <div
            className={`flex h-14 w-14 flex-shrink-0 items-center justify-center rounded-full ${cfg.bgColor}`}
          >
            <span className={`text-lg font-bold ${cfg.color}`}>{lead.totalScore}</span>
          </div>

          {/* Lead info */}
          <div className="min-w-0 flex-1">
            <div className="flex items-center gap-2">
              <span className="truncate font-semibold">
                {lead.isAnonymous ? `Visitante Anónimo` : lead.userName || lead.visitorId}
              </span>
              <Badge className={`${cfg.bgColor} ${cfg.color} text-[10px]`} variant="secondary">
                {cfg.label}
              </Badge>
              {lead.isAnonymous && (
                <Badge variant="outline" className="text-[10px]">
                  Anónimo
                </Badge>
              )}
            </div>
            <div className="text-muted-foreground mt-1 flex items-center gap-3 text-xs">
              <span className="flex items-center gap-1">
                <DeviceIcon className="h-3 w-3" />
                {lead.device?.os} · {lead.device?.browser}
              </span>
              <span className="flex items-center gap-1">
                <Clock className="h-3 w-3" />
                {timeAgo}
              </span>
              <span className="flex items-center gap-1">
                <Eye className="h-3 w-3" />
                {lead.totalPageViews} pág.
              </span>
              {lead.email && (
                <span className="flex items-center gap-1">
                  <Mail className="h-3 w-3" />
                  {lead.email}
                </span>
              )}
            </div>
          </div>

          {/* Conversion probability */}
          <div className="hidden flex-shrink-0 text-right sm:block">
            <p className="text-sm font-bold tabular-nums">
              {(lead.conversionProbability * 100).toFixed(0)}%
            </p>
            <p className="text-muted-foreground text-[10px]">prob. conversión</p>
          </div>

          {/* Days to purchase */}
          <div className="hidden flex-shrink-0 text-right sm:block">
            <p className="text-sm font-bold tabular-nums">{lead.estimatedDaysToPurchase}d</p>
            <p className="text-muted-foreground text-[10px]">est. compra</p>
          </div>

          {/* Expand toggle */}
          <Button variant="ghost" size="sm" onClick={onToggle}>
            {expanded ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
          </Button>
        </div>

        {/* Score breakdown bar */}
        <div className="mt-3 flex h-2 gap-0.5 overflow-hidden rounded-full">
          <div
            className="bg-red-400"
            style={{ width: `${lead.breakdown.contactScore}%` }}
            title={`Contacto: ${lead.breakdown.contactScore}`}
          />
          <div
            className="bg-purple-400"
            style={{ width: `${lead.breakdown.engagementScore}%` }}
            title={`Engagement: ${lead.breakdown.engagementScore}`}
          />
          <div
            className="bg-blue-400"
            style={{ width: `${lead.breakdown.intentScore}%` }}
            title={`Intención: ${lead.breakdown.intentScore}`}
          />
          <div
            className="bg-emerald-400"
            style={{ width: `${lead.breakdown.financialReadinessScore}%` }}
            title={`Financiero: ${lead.breakdown.financialReadinessScore}`}
          />
          <div className="flex-1 bg-gray-200" />
        </div>
        <div className="text-muted-foreground mt-1 flex gap-4 text-[10px]">
          <span className="flex items-center gap-1">
            <div className="h-1.5 w-1.5 rounded bg-red-400" /> Contacto{' '}
            {lead.breakdown.contactScore}
          </span>
          <span className="flex items-center gap-1">
            <div className="h-1.5 w-1.5 rounded bg-purple-400" /> Engagement{' '}
            {lead.breakdown.engagementScore}
          </span>
          <span className="flex items-center gap-1">
            <div className="h-1.5 w-1.5 rounded bg-blue-400" /> Intención{' '}
            {lead.breakdown.intentScore}
          </span>
          <span className="flex items-center gap-1">
            <div className="h-1.5 w-1.5 rounded bg-emerald-400" /> Financiero{' '}
            {lead.breakdown.financialReadinessScore}
          </span>
        </div>

        {/* Recommended action */}
        <div className="mt-3 flex items-center gap-2 rounded-lg border border-purple-100 bg-purple-50 p-2">
          <Zap className="h-4 w-4 flex-shrink-0 text-purple-600" />
          <span className="text-sm text-purple-800">{lead.recommendedAction}</span>
        </div>

        {/* Expanded details */}
        {expanded && (
          <div className="mt-4 space-y-4 border-t pt-4">
            {/* Signals */}
            <div>
              <h4 className="mb-2 flex items-center gap-1 text-sm font-semibold">
                <BarChart3 className="h-4 w-4" /> Señales de Comportamiento
              </h4>
              <div className="grid gap-2 sm:grid-cols-2">
                {lead.signals.map((signal, idx) => (
                  <div
                    key={idx}
                    className={`flex items-center justify-between rounded border p-2 text-sm ${
                      signal.importance === 'high'
                        ? 'border-red-100 bg-red-50'
                        : signal.importance === 'medium'
                          ? 'border-amber-100 bg-amber-50'
                          : 'border-gray-100 bg-gray-50'
                    }`}
                  >
                    <span>{signal.label}</span>
                    <div className="flex items-center gap-2">
                      <Badge variant="secondary" className="text-[10px]">
                        ×{signal.count}
                      </Badge>
                      <span className="text-xs font-semibold tabular-nums">
                        +{signal.pointsContributed}pts
                      </span>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            {/* Interested Vehicles */}
            {lead.interestedVehicles.length > 0 && (
              <div>
                <h4 className="mb-2 flex items-center gap-1 text-sm font-semibold">
                  <Car className="h-4 w-4" /> Vehículos de Interés
                </h4>
                <div className="grid gap-2 sm:grid-cols-2">
                  {lead.interestedVehicles.map(v => (
                    <div key={v.vehicleId} className="flex items-center gap-3 rounded border p-2">
                      <div className="min-w-0 flex-1">
                        <p className="truncate text-sm font-medium">{v.title}</p>
                        <p className="text-muted-foreground text-xs">
                          {formatDOP(v.price)} · {v.viewCount} vistas ·{' '}
                          {Math.round(v.totalViewTime / 60)}min
                        </p>
                      </div>
                      <div className="flex gap-1">
                        {v.contacted && (
                          <Badge className="bg-emerald-100 text-[10px] text-emerald-700">
                            Contactado
                          </Badge>
                        )}
                        {v.favorited && (
                          <Badge className="bg-pink-100 text-[10px] text-pink-700">♥</Badge>
                        )}
                      </div>
                      <div className="text-right">
                        <p className="text-xs font-bold">{v.interestScore}</p>
                        <p className="text-muted-foreground text-[10px]">interés</p>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Preferred Profile */}
            <div>
              <h4 className="mb-2 flex items-center gap-1 text-sm font-semibold">
                <Search className="h-4 w-4" /> Perfil de Preferencia Inferido
              </h4>
              <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
                <div className="rounded border bg-gray-50 p-2">
                  <p className="text-muted-foreground text-[10px]">Marcas preferidas</p>
                  <p className="text-sm font-medium">
                    {lead.preferredProfile.preferredMakes.join(', ') || '—'}
                  </p>
                </div>
                <div className="rounded border bg-gray-50 p-2">
                  <p className="text-muted-foreground text-[10px]">Modelos preferidos</p>
                  <p className="text-sm font-medium">
                    {lead.preferredProfile.preferredModels.join(', ') || '—'}
                  </p>
                </div>
                <div className="rounded border bg-gray-50 p-2">
                  <p className="text-muted-foreground text-[10px]">Rango de precio</p>
                  <p className="text-sm font-medium">
                    {formatDOP(lead.preferredProfile.priceRange.min)} —{' '}
                    {formatDOP(lead.preferredProfile.priceRange.max)}
                  </p>
                </div>
                <div className="rounded border bg-gray-50 p-2">
                  <p className="text-muted-foreground text-[10px]">Años</p>
                  <p className="text-sm font-medium">
                    {lead.preferredProfile.yearRange.min} — {lead.preferredProfile.yearRange.max}
                  </p>
                </div>
              </div>
            </div>

            {/* Device Info */}
            <div className="text-muted-foreground flex items-center gap-4 text-xs">
              <span className="flex items-center gap-1">
                <DeviceIcon className="h-3 w-3" /> {lead.device?.deviceType}
              </span>
              <span>
                {lead.device?.os} {lead.device?.osVersion}
              </span>
              <span>
                {lead.device?.browser} {lead.device?.browserVersion}
              </span>
              <span>
                {lead.device?.screenWidth}×{lead.device?.screenHeight}
              </span>
              {lead.device?.connectionType && <span>📶 {lead.device.connectionType}</span>}
              <span>{lead.device?.language}</span>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

function getTimeAgo(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const minutes = Math.floor(diff / 60000);
  if (minutes < 1) return 'ahora';
  if (minutes < 60) return `hace ${minutes}min`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `hace ${hours}h`;
  const days = Math.floor(hours / 24);
  return `hace ${days}d`;
}
