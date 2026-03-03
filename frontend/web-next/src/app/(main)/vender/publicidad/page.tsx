/**
 * Seller Advertising — /vender/publicidad
 *
 * Advertising hub for individual sellers (non-dealers).
 * Shows boost options, active campaigns, and quick actions.
 * Simpler than the dealer version — focused on per-vehicle boosts.
 */

'use client';

import { useState } from 'react';
import Link from 'next/link';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { useAuth } from '@/hooks/use-auth';
import { useCampaignsByOwner, usePauseCampaign, useResumeCampaign } from '@/hooks/use-advertising';
import type { CampaignStatus } from '@/types/advertising';
import {
  Sparkles,
  Zap,
  Eye,
  MousePointerClick,
  TrendingUp,
  ArrowRight,
  Pause,
  Play,
  BarChart3,
  Rocket,
  Star,
  Check,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { toast } from 'sonner';

// =============================================================================
// DATA
// =============================================================================

const BOOST_OPTIONS = [
  {
    id: 'featured',
    name: 'Destacar vehículo',
    description: 'Tu vehículo aparece con badge "Destacado" y más arriba en búsquedas',
    price: 990,
    duration: '7 días',
    icon: Star,
    color: 'text-blue-600 bg-blue-50',
  },
  {
    id: 'premium',
    name: 'Vehículo Premium',
    description: 'Máxima visibilidad: homepage, búsquedas prioritarias y badge premium',
    price: 2490,
    duration: '15 días',
    icon: Zap,
    color: 'text-emerald-600 bg-emerald-50',
    popular: true,
  },
];

// =============================================================================
// HELPERS
// =============================================================================

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('es-DO', {
    style: 'currency',
    currency: 'DOP',
    maximumFractionDigits: 0,
  }).format(amount);
}

function statusBadge(status: CampaignStatus) {
  const map: Record<string, string> = {
    Active: 'bg-emerald-100 text-emerald-700',
    Paused: 'bg-amber-100 text-amber-700',
    Completed: 'bg-slate-100 text-slate-600',
    Cancelled: 'bg-red-100 text-red-700',
    PendingPayment: 'bg-blue-100 text-blue-700',
    Expired: 'bg-slate-100 text-slate-500',
  };
  return (
    <span
      className={cn('rounded-full px-2 py-0.5 text-xs font-medium', map[status] || 'bg-slate-100')}
    >
      {status === 'Active'
        ? 'Activa'
        : status === 'Paused'
          ? 'Pausada'
          : status === 'Completed'
            ? 'Completada'
            : status}
    </span>
  );
}

// =============================================================================
// MAIN
// =============================================================================

export default function SellerAdvertisingPage() {
  const { user } = useAuth();
  const { data: campaignsData, isLoading } = useCampaignsByOwner(user?.id || '');
  const pauseMutation = usePauseCampaign();
  const resumeMutation = useResumeCampaign();
  const [expandedId, setExpandedId] = useState<string | null>(null);

  const campaigns = campaignsData?.items || [];
  const activeCampaigns = campaigns.filter(c => c.status === 'Active');
  const totalImpressions = campaigns.reduce((sum, c) => sum + (c.totalViews || 0), 0);
  const totalClicks = campaigns.reduce((sum, c) => sum + (c.totalClicks || 0), 0);

  return (
    <div className="min-h-screen bg-gradient-to-b from-slate-50 to-white">
      {/* Header */}
      <div className="border-b bg-white">
        <div className="mx-auto max-w-3xl px-4 py-6 sm:px-6">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-emerald-100">
              <Sparkles className="h-5 w-5 text-emerald-600" />
            </div>
            <div>
              <h1 className="text-xl font-bold text-slate-900">Publicidad</h1>
              <p className="text-sm text-slate-500">Destaca tus vehículos y vende más rápido</p>
            </div>
          </div>
        </div>
      </div>

      <div className="mx-auto max-w-3xl space-y-6 px-4 py-6 sm:px-6">
        {/* Overview stats */}
        <div className="grid grid-cols-3 gap-3">
          <Card className="text-center">
            <CardContent className="pt-4 pb-3">
              <Eye className="mx-auto mb-1 h-5 w-5 text-blue-500" />
              <p className="text-lg font-bold text-slate-900">
                {totalImpressions.toLocaleString()}
              </p>
              <p className="text-xs text-slate-500">Impresiones</p>
            </CardContent>
          </Card>
          <Card className="text-center">
            <CardContent className="pt-4 pb-3">
              <MousePointerClick className="mx-auto mb-1 h-5 w-5 text-emerald-500" />
              <p className="text-lg font-bold text-slate-900">{totalClicks.toLocaleString()}</p>
              <p className="text-xs text-slate-500">Clics</p>
            </CardContent>
          </Card>
          <Card className="text-center">
            <CardContent className="pt-4 pb-3">
              <TrendingUp className="mx-auto mb-1 h-5 w-5 text-purple-500" />
              <p className="text-lg font-bold text-slate-900">{activeCampaigns.length}</p>
              <p className="text-xs text-slate-500">Activas</p>
            </CardContent>
          </Card>
        </div>

        {/* Quick Boost Options */}
        <div>
          <h2 className="mb-3 flex items-center gap-2 text-sm font-semibold text-slate-900">
            <Rocket className="h-4 w-4 text-emerald-600" />
            Opciones de Boost
          </h2>
          <div className="grid gap-3 sm:grid-cols-2">
            {BOOST_OPTIONS.map(opt => (
              <Card
                key={opt.id}
                className={cn('relative', opt.popular && 'ring-2 ring-emerald-500')}
              >
                {opt.popular && (
                  <Badge className="absolute top-2 right-2 bg-emerald-600 text-[10px] text-white">
                    Popular
                  </Badge>
                )}
                <CardContent className="pt-5">
                  <div
                    className={cn(
                      'mb-3 flex h-9 w-9 items-center justify-center rounded-lg',
                      opt.color
                    )}
                  >
                    <opt.icon className="h-4.5 w-4.5" />
                  </div>
                  <p className="font-semibold text-slate-900">{opt.name}</p>
                  <p className="mt-1 text-xs text-slate-500">{opt.description}</p>
                  <div className="mt-3 flex items-baseline gap-2">
                    <span className="text-xl font-bold text-slate-900">
                      {formatCurrency(opt.price)}
                    </span>
                    <span className="text-xs text-slate-400">/ {opt.duration}</span>
                  </div>
                  <Button
                    asChild
                    size="sm"
                    className={cn(
                      'mt-3 w-full',
                      opt.popular ? 'bg-emerald-600 text-white hover:bg-emerald-700' : ''
                    )}
                    variant={opt.popular ? 'default' : 'outline'}
                  >
                    <Link href="/cuenta/mis-vehiculos">
                      Seleccionar vehículo
                      <ArrowRight className="ml-1 h-3.5 w-3.5" />
                    </Link>
                  </Button>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>

        {/* Active Campaigns */}
        <div>
          <h2 className="mb-3 flex items-center gap-2 text-sm font-semibold text-slate-900">
            <BarChart3 className="h-4 w-4 text-blue-600" />
            Mis Campañas
          </h2>

          {isLoading ? (
            <div className="space-y-3">
              {[1, 2].map(i => (
                <div key={i} className="h-20 animate-pulse rounded-lg bg-slate-100" />
              ))}
            </div>
          ) : !campaigns.length ? (
            <Card className="border-dashed bg-slate-50">
              <CardContent className="py-8 text-center">
                <Sparkles className="mx-auto mb-2 h-8 w-8 text-slate-300" />
                <p className="text-sm text-slate-500">No tienes campañas activas</p>
                <p className="mt-1 text-xs text-slate-400">
                  Destaca tus vehículos para vender más rápido
                </p>
              </CardContent>
            </Card>
          ) : (
            <div className="space-y-3">
              {campaigns.map(campaign => (
                <Card key={campaign.id} className="overflow-hidden">
                  <CardContent className="p-4">
                    <div className="flex items-center justify-between">
                      <div className="min-w-0 flex-1">
                        <div className="flex items-center gap-2">
                          <p className="truncate text-sm font-semibold text-slate-900">
                            {campaign.placementType === 'PremiumSpot' ? 'Premium' : 'Destacado'} —{' '}
                            {campaign.vehicleId?.slice(0, 8) || 'Paquete'}
                          </p>
                          {statusBadge(campaign.status)}
                        </div>
                        <div className="mt-1 flex items-center gap-4 text-xs text-slate-500">
                          <span className="flex items-center gap-1">
                            <Eye className="h-3 w-3" />
                            {(campaign.totalViews || 0).toLocaleString()}
                          </span>
                          <span className="flex items-center gap-1">
                            <MousePointerClick className="h-3 w-3" />
                            {(campaign.totalClicks || 0).toLocaleString()}
                          </span>
                          <span>
                            {formatCurrency(campaign.totalBudget - (campaign.remainingBudget || 0))}{' '}
                            / {formatCurrency(campaign.totalBudget)}
                          </span>
                        </div>
                      </div>
                      <div className="ml-2 flex items-center gap-1">
                        {campaign.status === 'Active' && (
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => {
                              pauseMutation.mutate(campaign.id);
                              toast.info('Campaña pausada');
                            }}
                          >
                            <Pause className="h-4 w-4" />
                          </Button>
                        )}
                        {campaign.status === 'Paused' && (
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => {
                              resumeMutation.mutate(campaign.id);
                              toast.success('Campaña reanudada');
                            }}
                          >
                            <Play className="h-4 w-4" />
                          </Button>
                        )}
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() =>
                            setExpandedId(expandedId === campaign.id ? null : campaign.id)
                          }
                        >
                          <BarChart3 className="h-4 w-4" />
                        </Button>
                      </div>
                    </div>

                    {/* Budget bar */}
                    <div className="mt-2">
                      <div className="h-1.5 overflow-hidden rounded-full bg-slate-100">
                        <div
                          className="h-full rounded-full bg-emerald-500 transition-all"
                          style={{
                            width: `${Math.min(100, ((campaign.totalBudget - (campaign.remainingBudget || 0)) / campaign.totalBudget) * 100)}%`,
                          }}
                        />
                      </div>
                    </div>

                    {expandedId === campaign.id && (
                      <div className="mt-3 grid grid-cols-2 gap-2 border-t pt-3 text-xs">
                        <div className="rounded bg-slate-50 p-2">
                          <p className="text-slate-400">CTR</p>
                          <p className="font-semibold text-slate-900">
                            {campaign.totalViews
                              ? (((campaign.totalClicks || 0) / campaign.totalViews) * 100).toFixed(
                                  1
                                )
                              : '0.0'}
                            %
                          </p>
                        </div>
                        <div className="rounded bg-slate-50 p-2">
                          <p className="text-slate-400">CPC</p>
                          <p className="font-semibold text-slate-900">
                            {campaign.totalClicks
                              ? formatCurrency(
                                  (campaign.totalBudget - (campaign.remainingBudget || 0)) /
                                    campaign.totalClicks
                                )
                              : '—'}
                          </p>
                        </div>
                      </div>
                    )}
                  </CardContent>
                </Card>
              ))}
            </div>
          )}
        </div>

        {/* Benefits section */}
        <Card className="border-emerald-200 bg-gradient-to-br from-emerald-50 to-emerald-100/30">
          <CardHeader className="pb-2">
            <CardTitle className="text-sm text-emerald-900">¿Por qué anunciarte en OKLA?</CardTitle>
          </CardHeader>
          <CardContent>
            <ul className="space-y-2">
              {[
                'Tus vehículos se muestran a compradores activos',
                'Solo pagas cuando alguien ve o hace clic en tu anuncio',
                'Resultados medibles en tiempo real',
                'Cancela o pausa cuando quieras',
              ].map(item => (
                <li key={item} className="flex items-start gap-2 text-sm text-emerald-800">
                  <Check className="mt-0.5 h-4 w-4 shrink-0 text-emerald-600" />
                  {item}
                </li>
              ))}
            </ul>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
