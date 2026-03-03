/**
 * Dealer Publicidad Page — /dealer/publicidad
 *
 * Dashboard for dealers to view and manage their advertising campaigns,
 * view metrics, spend, and create new boosted listings.
 */

'use client';

import { useState } from 'react';
import Link from 'next/link';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useAuth } from '@/hooks/use-auth';
import {
  useCampaignsByOwner,
  usePauseCampaign,
  useResumeCampaign,
  useCancelCampaign,
  useCampaignReport,
} from '@/hooks/use-advertising';
import type { AdCampaignSummary, CampaignStatus } from '@/types/advertising';
import { PlanGate } from '@/components/plan/plan-gate';

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

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString('es-DO', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  });
}

function statusBadge(status: CampaignStatus) {
  const variants: Record<
    CampaignStatus,
    'default' | 'secondary' | 'success' | 'warning' | 'danger'
  > = {
    Active: 'success',
    Paused: 'warning',
    PendingPayment: 'secondary',
    Completed: 'default',
    Cancelled: 'danger',
    Expired: 'secondary',
  };
  return <Badge variant={variants[status] ?? 'default'}>{status}</Badge>;
}

// =============================================================================
// CAMPAIGN CARD
// =============================================================================

function CampaignCard({ campaign }: { campaign: AdCampaignSummary }) {
  const pauseMutation = usePauseCampaign();
  const resumeMutation = useResumeCampaign();
  const cancelMutation = useCancelCampaign();
  const [showReport, setShowReport] = useState(false);

  const budgetUsedPct =
    campaign.totalBudget > 0
      ? ((campaign.totalBudget - campaign.remainingBudget) / campaign.totalBudget) * 100
      : 0;

  return (
    <Card>
      <CardContent className="pt-6">
        <div className="flex items-start justify-between">
          <div>
            <div className="flex items-center gap-2">
              <span className="text-sm font-medium">
                {campaign.placementType === 'FeaturedSpot' ? '⭐' : '💎'} {campaign.placementType}
              </span>
              {statusBadge(campaign.status)}
            </div>
            <p className="text-muted-foreground mt-1 text-xs">
              {formatDate(campaign.startDate)} — {formatDate(campaign.endDate)}
            </p>
          </div>
          <div className="text-right">
            <p className="text-lg font-bold">{formatCurrency(campaign.totalBudget)}</p>
            <p className="text-muted-foreground text-xs">
              Restante: {formatCurrency(campaign.remainingBudget)}
            </p>
          </div>
        </div>

        {/* Budget progress bar */}
        <div className="mt-3">
          <div className="bg-muted h-2 rounded-full">
            <div
              className="bg-primary h-2 rounded-full transition-all"
              style={{ width: `${Math.min(budgetUsedPct, 100)}%` }}
            />
          </div>
          <p className="text-muted-foreground mt-1 text-xs">{budgetUsedPct.toFixed(1)}% usado</p>
        </div>

        {/* Metrics */}
        <div className="mt-4 grid grid-cols-3 gap-4 text-center text-sm">
          <div>
            <p className="text-muted-foreground">Vistas</p>
            <p className="font-bold">{campaign.totalViews.toLocaleString()}</p>
          </div>
          <div>
            <p className="text-muted-foreground">Clics</p>
            <p className="font-bold">{campaign.totalClicks.toLocaleString()}</p>
          </div>
          <div>
            <p className="text-muted-foreground">CTR</p>
            <p className="font-bold">{(campaign.ctr * 100).toFixed(2)}%</p>
          </div>
        </div>

        {/* Actions */}
        <div className="mt-4 flex gap-2">
          {campaign.status === 'Active' && (
            <Button
              size="sm"
              variant="outline"
              onClick={() => pauseMutation.mutate(campaign.id)}
              disabled={pauseMutation.isPending}
            >
              ⏸️ Pausar
            </Button>
          )}
          {campaign.status === 'Paused' && (
            <Button
              size="sm"
              variant="outline"
              onClick={() => resumeMutation.mutate(campaign.id)}
              disabled={resumeMutation.isPending}
            >
              ▶️ Reanudar
            </Button>
          )}
          {(campaign.status === 'Active' || campaign.status === 'Paused') && (
            <Button
              size="sm"
              variant="outline"
              onClick={() => cancelMutation.mutate(campaign.id)}
              disabled={cancelMutation.isPending}
            >
              ✕ Cancelar
            </Button>
          )}
          <Button size="sm" variant="outline" onClick={() => setShowReport(!showReport)}>
            📊 Reporte
          </Button>
        </div>

        {showReport && <CampaignReportPanel campaignId={campaign.id} />}
      </CardContent>
    </Card>
  );
}

function CampaignReportPanel({ campaignId }: { campaignId: string }) {
  const { data: report, isLoading } = useCampaignReport(campaignId, 30);

  if (isLoading) return <div className="bg-muted mt-4 h-20 animate-pulse rounded" />;
  if (!report) return <p className="text-muted-foreground mt-4 text-sm">Sin datos de reporte.</p>;

  return (
    <div className="mt-4 rounded border p-3">
      <p className="mb-2 text-sm font-semibold">Reporte últimos 30 días</p>
      <div className="grid grid-cols-2 gap-2 text-sm">
        <div>
          <span className="text-muted-foreground">Vistas: </span>
          <span className="font-medium">{report.totalViews.toLocaleString()}</span>
        </div>
        <div>
          <span className="text-muted-foreground">Clics: </span>
          <span className="font-medium">{report.totalClicks.toLocaleString()}</span>
        </div>
        <div>
          <span className="text-muted-foreground">CTR: </span>
          <span className="font-medium">{(report.ctr * 100).toFixed(2)}%</span>
        </div>
        <div>
          <span className="text-muted-foreground">Gastado: </span>
          <span className="font-medium">{formatCurrency(report.totalSpent)}</span>
        </div>
      </div>
    </div>
  );
}

// =============================================================================
// MAIN PAGE
// =============================================================================

function DealerPublicidadContent() {
  const { user } = useAuth();
  const [statusFilter, setStatusFilter] = useState<CampaignStatus | undefined>(undefined);
  const [page, setPage] = useState(1);

  const userId = user?.id ?? '';
  const { data: campaignsData, isLoading } = useCampaignsByOwner(
    userId,
    'Dealer',
    statusFilter,
    page,
    10
  );

  const campaigns = campaignsData?.items ?? [];
  const pagination = campaignsData?.pagination;

  return (
    <div className="mx-auto max-w-7xl px-4 py-8">
      {/* Header */}
      <div className="mb-8 flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">📣 Publicidad</h1>
          <p className="text-muted-foreground">
            Gestiona tus campañas publicitarias y potencia la visibilidad de tu inventario.
          </p>
        </div>
        <Link href="/impulsar">
          <Button>🚀 Crear Nueva Campaña</Button>
        </Link>
      </div>

      <Tabs defaultValue="campaigns">
        <TabsList>
          <TabsTrigger value="campaigns">Mis Campañas</TabsTrigger>
          <TabsTrigger value="overview">Resumen</TabsTrigger>
        </TabsList>

        <TabsContent value="campaigns" className="mt-6">
          {/* Filter buttons */}
          <div className="mb-4 flex flex-wrap gap-2">
            {[undefined, 'Active', 'Paused', 'PendingPayment', 'Completed', 'Cancelled'].map(
              status => (
                <Button
                  key={status ?? 'all'}
                  size="sm"
                  variant={statusFilter === status ? 'default' : 'outline'}
                  onClick={() => {
                    setStatusFilter(status as CampaignStatus | undefined);
                    setPage(1);
                  }}
                >
                  {status ?? 'Todas'}
                </Button>
              )
            )}
          </div>

          {/* Campaign list */}
          {isLoading ? (
            <div className="grid gap-4 md:grid-cols-2">
              {[1, 2, 3, 4].map(i => (
                <Card key={i} className="animate-pulse">
                  <CardContent className="h-48" />
                </Card>
              ))}
            </div>
          ) : campaigns.length === 0 ? (
            <Card>
              <CardContent className="py-12 text-center">
                <p className="text-muted-foreground mb-4">
                  No tienes campañas
                  {statusFilter ? ` con estado "${statusFilter}"` : ''} aún.
                </p>
                <Link href="/impulsar">
                  <Button>🚀 Crear tu primera campaña</Button>
                </Link>
              </CardContent>
            </Card>
          ) : (
            <div className="grid gap-4 md:grid-cols-2">
              {campaigns.map(campaign => (
                <CampaignCard key={campaign.id} campaign={campaign} />
              ))}
            </div>
          )}

          {/* Pagination */}
          {pagination && pagination.totalPages > 1 && (
            <div className="mt-6 flex items-center justify-center gap-2">
              <Button
                size="sm"
                variant="outline"
                disabled={!pagination.hasPreviousPage}
                onClick={() => setPage(p => p - 1)}
              >
                ← Anterior
              </Button>
              <span className="text-muted-foreground text-sm">
                Página {pagination.page} de {pagination.totalPages}
              </span>
              <Button
                size="sm"
                variant="outline"
                disabled={!pagination.hasNextPage}
                onClick={() => setPage(p => p + 1)}
              >
                Siguiente →
              </Button>
            </div>
          )}
        </TabsContent>

        <TabsContent value="overview" className="mt-6">
          <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
            <Card>
              <CardContent className="pt-6">
                <p className="text-muted-foreground text-sm">Campañas Activas</p>
                <p className="text-3xl font-bold">
                  {campaigns.filter(c => c.status === 'Active').length}
                </p>
              </CardContent>
            </Card>
            <Card>
              <CardContent className="pt-6">
                <p className="text-muted-foreground text-sm">Vistas Totales</p>
                <p className="text-3xl font-bold">
                  {campaigns.reduce((sum, c) => sum + c.totalViews, 0).toLocaleString()}
                </p>
              </CardContent>
            </Card>
            <Card>
              <CardContent className="pt-6">
                <p className="text-muted-foreground text-sm">Clics Totales</p>
                <p className="text-3xl font-bold">
                  {campaigns.reduce((sum, c) => sum + c.totalClicks, 0).toLocaleString()}
                </p>
              </CardContent>
            </Card>
            <Card>
              <CardContent className="pt-6">
                <p className="text-muted-foreground text-sm">Inversión Total</p>
                <p className="text-primary text-3xl font-bold">
                  {formatCurrency(
                    campaigns.reduce((sum, c) => sum + (c.totalBudget - c.remainingBudget), 0)
                  )}
                </p>
              </CardContent>
            </Card>
          </div>
        </TabsContent>
      </Tabs>
    </div>
  );
}

export default function DealerPublicidadPage() {
  return (
    <PlanGate feature="featuredListings">
      <DealerPublicidadContent />
    </PlanGate>
  );
}
