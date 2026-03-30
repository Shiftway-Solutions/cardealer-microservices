/**
 * Dealer Dashboard Page — /dealer/dashboard
 *
 * Primary landing page for dealers after login.
 * Rendered inside the dealer portal layout (/dealer/layout.tsx) — includes sidebar.
 *
 * This file is the canonical home for the dealer command center.
 * Login, middleware, and /cuenta layout all redirect dealers here.
 */

'use client';

import * as React from 'react';
import Link from 'next/link';
import {
  Car,
  Eye,
  MessageSquare,
  Users,
  TrendingUp,
  Plus,
  Clock,
  AlertCircle,
  Loader2,
  Shield,
  BarChart3,
  Calendar,
  Package,
  Crown,
  Lock,
  Download,
  ChevronRight,
} from 'lucide-react';
import { useCanSell } from '@/hooks/use-kyc';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { useDealerDashboard } from '@/hooks/use-dealers';
import { useRecentLeads } from '@/hooks/use-crm';
import { useUpcomingAppointments } from '@/hooks/use-appointments';
import { formatLeadName } from '@/services/crm';
import { getAppointmentTypeLabel } from '@/services/appointments';
import { PlanBadge, PlanUsageBar } from '@/components/plan/plan-gate';
import { UpgradeBanner } from '@/components/shared/upgrade-banner';
import { MissedOpportunityBanner } from '@/components/dealer/missed-opportunity-banner';
import { BenchmarkComparisonCard } from '@/components/dealer/benchmark-comparison-card';
import { ChatbotUpgradeBanner } from '@/components/dealer/chatbot-upgrade-banner';
import { NoSaleConversionModal } from '@/components/dealer/no-sale-conversion-modal';
import { usePlanAccess } from '@/hooks/use-plan-access';
import { useExportReport } from '@/hooks/use-dealer-analytics';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from '@/components/ui/dialog';

// =============================================================================
// MAIN PAGE
// =============================================================================

export default function DealerDashboardPage() {
  return <DealerDashboardContent />;
}

// =============================================================================
// DASHBOARD CONTENT
// =============================================================================

function DealerDashboardContent() {
  const { dealer, stats, isLoading: dealerLoading } = useDealerDashboard();
  const { data: leads, isLoading: leadsLoading } = useRecentLeads(5, !!dealer);
  const { data: appointments, isLoading: appointmentsLoading } = useUpcomingAppointments(
    dealer?.id ?? '',
    4
  );
  const { canSell, isLoading: kycLoading } = useCanSell();
  const { canAccess, currentPlan, minimumPlanFor } = usePlanAccess();
  const [showListingLimitModal, setShowListingLimitModal] = React.useState(false);
  const exportMutation = useExportReport(dealer?.id ?? '');

  const maxListings = dealer?.maxActiveListings ?? 3;
  const activeListings = dealer?.currentActiveListings ?? 0;
  const isAtListingLimit = maxListings !== -1 && activeListings >= maxListings;

  if (!dealerLoading && !dealer) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-foreground text-2xl font-bold">Portal Dealer</h1>
            <p className="text-muted-foreground">Activa tu perfil para desbloquear el portal</p>
          </div>
        </div>

        <Card className="border-amber-200 bg-amber-50/70">
          <CardContent className="p-6">
            <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
              <div className="flex gap-4">
                <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-amber-100 text-amber-700">
                  <AlertCircle className="h-6 w-6" />
                </div>
                <div className="space-y-2">
                  <div>
                    <h2 className="text-foreground text-xl font-semibold">
                      Completa tu perfil de dealer
                    </h2>
                    <p className="text-muted-foreground max-w-2xl text-sm leading-6">
                      Tu cuenta ya tiene acceso al portal, pero todavia no existe un perfil de
                      concesionario activo. Completa el registro para habilitar inventario,
                      configuracion publica, leads, citas y facturacion.
                    </p>
                  </div>
                  <div className="flex flex-wrap gap-2 text-xs text-amber-800">
                    <Badge variant="secondary">Inventario</Badge>
                    <Badge variant="secondary">Leads</Badge>
                    <Badge variant="secondary">Citas</Badge>
                    <Badge variant="secondary">Perfil publico</Badge>
                  </div>
                </div>
              </div>

              <div className="flex flex-col gap-2 sm:flex-row">
                <Button asChild>
                  <Link href="/dealer/registro">
                    Completar registro
                    <ChevronRight className="ml-2 h-4 w-4" />
                  </Link>
                </Button>
                <Button variant="outline" asChild>
                  <Link href="/cuenta/perfil">Revisar mi cuenta</Link>
                </Button>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          {dealerLoading ? (
            <Skeleton className="mb-1 h-8 w-52" />
          ) : (
            <div className="flex items-center gap-3">
              <h1 className="text-foreground text-2xl font-bold">
                {dealer?.businessName ?? 'Portal Dealer'}
              </h1>
              <PlanBadge showUpgradeLink />
            </div>
          )}
          <p className="text-muted-foreground">Panel de control del concesionario</p>
        </div>
        {!kycLoading && canSell && (
          <div className="flex items-center gap-2">
            {canAccess('analytics') && (
              <Button
                variant="outline"
                size="sm"
                className="gap-2"
                disabled={exportMutation.isPending}
                onClick={() => exportMutation.mutate('csv')}
              >
                {exportMutation.isPending ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <Download className="h-4 w-4" />
                )}
                Exportar datos
              </Button>
            )}
            {currentPlan === 'libre' && (
              <Link href="/cuenta/upgrade?plan=visible&type=dealer">
                <Button
                  variant="outline"
                  className="gap-2 border-purple-300 text-purple-700 hover:bg-purple-50"
                >
                  <Crown className="h-4 w-4 text-purple-500" />
                  Activar plan VISIBLE — desde RD$1,699/mes
                </Button>
              </Link>
            )}
            {isAtListingLimit ? (
              <Button className="gap-2" onClick={() => setShowListingLimitModal(true)}>
                <Plus className="h-4 w-4" />
                Nuevo Vehículo
              </Button>
            ) : (
              <Link href="/dealer/inventario/nuevo">
                <Button className="gap-2">
                  <Plus className="h-4 w-4" />
                  Nuevo Vehículo
                </Button>
              </Link>
            )}
          </div>
        )}
      </div>

      {/* Listing limit upgrade modal */}
      <Dialog open={showListingLimitModal} onOpenChange={setShowListingLimitModal}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <Lock className="h-5 w-5 text-amber-500" />
              Límite de publicaciones alcanzado
            </DialogTitle>
            <DialogDescription>
              Has alcanzado el máximo de {maxListings} vehículos activos en tu plan {currentPlan}.
              Elimina un vehículo existente o actualiza tu plan para publicar más.
            </DialogDescription>
          </DialogHeader>
          <div className="flex flex-col gap-3 pt-2">
            <Link href="/cuenta/upgrade?plan=visible&type=dealer">
              <Button className="w-full gap-2">
                <Crown className="h-4 w-4" />
                Upgrade de plan
              </Button>
            </Link>
            <Button variant="outline" onClick={() => setShowListingLimitModal(false)}>
              Cerrar
            </Button>
          </div>
        </DialogContent>
      </Dialog>

      {/* Verification banner — shown while unverified */}
      <DealerVerificationBanner />

      {/* Upgrade banner — shown to dealers on free plan */}
      <UpgradeBanner
        variant="inline"
        userType="dealer"
        upgradeUrl="/cuenta/upgrade?plan=visible&type=dealer"
      />

      {/* Missed opportunity banner — dynamic LIBRE-plan urgency counter */}
      <MissedOpportunityBanner />

      {/* ChatBot upgrade banner — shown after first inquiry for LIBRE dealers */}
      <ChatbotUpgradeBanner />

      {/* Benchmark comparison — shows LIBRE dealers how VISIBLE dealers perform */}
      <BenchmarkComparisonCard />

      {/* 45-day no-sale modal — triggers for LIBRE dealers with no revenue */}
      <NoSaleConversionModal />

      {/* Listings usage progress bar — active listings vs plan limit */}
      {!dealerLoading && dealer && (
        <Card>
          <CardContent className="pt-6">
            <PlanUsageBar
              current={stats?.activeListings ?? 0}
              max={dealer.maxActiveListings === -1 ? 999999 : dealer.maxActiveListings}
              label="Vehículos Activos"
              showUnlimited
            />
          </CardContent>
        </Card>
      )}

      {/* Metric cards */}
      {dealerLoading ? (
        <div className="grid gap-4 sm:grid-cols-3">
          {[1, 2, 3].map(i => (
            <Skeleton key={i} className="h-24 rounded-xl" />
          ))}
        </div>
      ) : (
        <div className="grid gap-4 sm:grid-cols-3">
          <MetricCard
            title="Vehículos Activos"
            value={stats?.activeListings ?? 0}
            icon={Car}
            color="blue"
          />
          <MetricCard
            title="Vistas (últimos 7 días)"
            value={stats?.viewsThisMonth ?? 0}
            icon={Eye}
            color="green"
          />
          <MetricCard
            title="Consultas del Mes"
            value={stats?.inquiriesThisMonth ?? 0}
            icon={MessageSquare}
            color="purple"
          />
        </div>
      )}

      {/* Limited stats section for LIBRE plan — only 7 days, no history */}
      {!dealerLoading && currentPlan === 'libre' && (
        <Card className="border-dashed border-gray-300">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="flex items-center gap-2 text-base">
              <BarChart3 className="h-4 w-4" />
              Estadísticas (últimos 7 días)
            </CardTitle>
            <TooltipProvider>
              <Tooltip>
                <TooltipTrigger asChild>
                  <div className="flex cursor-help items-center gap-1.5 text-xs text-amber-600">
                    <Lock className="h-3.5 w-3.5" />
                    <span>Historial limitado</span>
                  </div>
                </TooltipTrigger>
                <TooltipContent side="left" className="max-w-[220px]">
                  <p className="text-xs">
                    Desde el plan <b>VISIBLE</b> puedes ver historial de 30, 60 y 90 días con
                    gráficos de tendencia.
                  </p>
                </TooltipContent>
              </Tooltip>
            </TooltipProvider>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-3 gap-4 text-center">
              <div>
                <p className="text-2xl font-bold">
                  {(stats?.viewsThisMonth ?? 0).toLocaleString()}
                </p>
                <p className="text-muted-foreground text-xs">Vistas</p>
              </div>
              <div>
                <p className="text-2xl font-bold">{stats?.inquiriesThisMonth ?? 0}</p>
                <p className="text-muted-foreground text-xs">Consultas</p>
              </div>
              <div>
                <p className="text-2xl font-bold">{stats?.activeListings ?? 0}</p>
                <p className="text-muted-foreground text-xs">Publicaciones</p>
              </div>
            </div>
            <p className="text-muted-foreground mt-3 text-center text-xs">
              Solo se muestran los últimos 7 días. Mejora tu plan para acceder a historial completo
              y tendencias.
            </p>
          </CardContent>
        </Card>
      )}

      <div className="grid gap-6 lg:grid-cols-2">
        {/* Recent Leads */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="flex items-center gap-2 text-base">
              <Users className="h-4 w-4" />
              Leads Recientes
            </CardTitle>
            <Button variant="ghost" size="sm" asChild>
              <Link href="/dealer/leads" className="flex items-center gap-1">
                Ver todos <ChevronRight className="h-4 w-4" />
              </Link>
            </Button>
          </CardHeader>
          <CardContent>
            {leadsLoading ? (
              <div className="space-y-3">
                {[1, 2, 3].map(i => (
                  <Skeleton key={i} className="h-12 rounded-lg" />
                ))}
              </div>
            ) : (leads?.length ?? 0) > 0 ? (
              <div className="space-y-1">
                {leads?.map(lead => (
                  <Link
                    key={lead.id}
                    href={`/dealer/leads/${lead.id}`}
                    className="hover:bg-muted flex items-center justify-between rounded-lg p-3 transition-colors"
                  >
                    <div className="flex items-center gap-3">
                      <div className="bg-muted flex h-9 w-9 items-center justify-center rounded-full">
                        <span className="text-muted-foreground text-sm font-medium">
                          {lead.firstName?.charAt(0) ?? '?'}
                        </span>
                      </div>
                      <div>
                        <p className="text-sm font-medium">{formatLeadName(lead)}</p>
                        <p className="text-muted-foreground text-xs">{lead.email}</p>
                      </div>
                    </div>
                    <LeadStatusBadge status={lead.status} />
                  </Link>
                ))}
              </div>
            ) : (
              <div className="text-muted-foreground py-8 text-center">
                <Users className="mx-auto mb-2 h-8 w-8 text-gray-300" />
                <p className="text-sm">No hay leads recientes</p>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Upcoming Appointments */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="flex items-center gap-2 text-base">
              <Calendar className="h-4 w-4" />
              Próximas Citas
            </CardTitle>
            <Button variant="ghost" size="sm" asChild>
              <Link href="/dealer/citas" className="flex items-center gap-1">
                Ver todas <ChevronRight className="h-4 w-4" />
              </Link>
            </Button>
          </CardHeader>
          <CardContent>
            {appointmentsLoading ? (
              <div className="space-y-3">
                {[1, 2, 3].map(i => (
                  <Skeleton key={i} className="h-12 rounded-lg" />
                ))}
              </div>
            ) : (appointments?.length ?? 0) > 0 ? (
              <div className="space-y-2">
                {appointments?.map(apt => (
                  <Link
                    key={apt.id}
                    href="/dealer/citas/calendario"
                    className="border-border hover:border-primary flex items-center justify-between rounded-lg border p-3 transition-colors"
                  >
                    <div className="flex items-center gap-3">
                      <div className="bg-primary/10 flex h-9 w-9 items-center justify-center rounded-lg">
                        <Calendar className="text-primary h-4 w-4" />
                      </div>
                      <div>
                        <p className="text-sm font-medium">{apt.clientName}</p>
                        <p className="text-muted-foreground text-xs">
                          {getAppointmentTypeLabel(apt.type)}
                        </p>
                      </div>
                    </div>
                    <div className="text-right">
                      <p className="text-sm font-medium">
                        {new Date(apt.scheduledDate).toLocaleDateString('es-DO', {
                          weekday: 'short',
                          day: 'numeric',
                          month: 'short',
                        })}
                      </p>
                      <p className="text-muted-foreground text-xs">{apt.scheduledTime}</p>
                    </div>
                  </Link>
                ))}
              </div>
            ) : (
              <div className="text-muted-foreground py-8 text-center">
                <Calendar className="mx-auto mb-2 h-8 w-8 text-gray-300" />
                <p className="text-sm">No hay citas programadas</p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Quick Actions */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Acciones Rápidas</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 gap-3 md:grid-cols-5">
            <QuickActionTile
              href="/dealer/inventario"
              icon={Package}
              label="Inventario"
              color="blue"
            />
            <QuickActionTile
              href="/dealer/leads"
              icon={Users}
              label="Ver Leads"
              color="purple"
              locked={!canAccess('leadManagement')}
              lockedTooltip={`Requiere plan ${minimumPlanFor('leadManagement')}`}
            />
            <QuickActionTile
              href="/dealer/analytics"
              icon={TrendingUp}
              label="Analytics"
              color="green"
              locked={!canAccess('analytics')}
              lockedTooltip={`Requiere plan ${minimumPlanFor('analytics')}`}
            />
            <QuickActionTile href="/dealer/citas" icon={Calendar} label="Citas" color="yellow" />
            <QuickActionTile
              href="/dealer/suscripcion"
              icon={Crown}
              label="Mi Plan"
              color="green"
            />
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

// =============================================================================
// SUB-COMPONENTS
// =============================================================================

function MetricCard({
  title,
  value,
  icon: Icon,
  color,
}: {
  title: string;
  value: number | string;
  icon: React.ElementType;
  color: 'blue' | 'green' | 'purple' | 'yellow';
}) {
  const colorClasses = {
    blue: 'bg-blue-100 text-blue-600',
    green: 'bg-green-100 text-green-600',
    purple: 'bg-purple-100 text-purple-600',
    yellow: 'bg-yellow-100 text-yellow-600',
  };

  return (
    <Card>
      <CardContent className="pt-6">
        <div className="flex items-center gap-3">
          <div
            className={`flex h-10 w-10 items-center justify-center rounded-lg ${colorClasses[color]}`}
          >
            <Icon className="h-5 w-5" />
          </div>
          <div>
            <p className="text-foreground text-2xl font-bold">
              {typeof value === 'number' ? value.toLocaleString() : value}
            </p>
            <p className="text-muted-foreground text-sm">{title}</p>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

function QuickActionTile({
  href,
  icon: Icon,
  label,
  color,
  locked,
  lockedTooltip,
}: {
  href: string;
  icon: React.ElementType;
  label: string;
  color: 'blue' | 'green' | 'purple' | 'yellow';
  locked?: boolean;
  lockedTooltip?: string;
}) {
  const colorClasses = {
    blue: 'bg-blue-100 text-blue-600',
    green: 'bg-green-100 text-green-600',
    purple: 'bg-purple-100 text-purple-600',
    yellow: 'bg-yellow-100 text-yellow-600',
  };

  const tile = (
    <Link
      href={href}
      className={`border-border hover:border-primary hover:bg-primary/5 relative flex flex-col items-center gap-2 rounded-xl border p-4 transition-all ${
        locked ? 'opacity-60' : ''
      }`}
    >
      <div
        className={`flex h-11 w-11 items-center justify-center rounded-xl ${colorClasses[color]}`}
      >
        <Icon className="h-5 w-5" />
      </div>
      <span className="text-foreground text-center text-sm font-medium">{label}</span>
      {locked && (
        <div className="absolute top-2 right-2">
          <Lock className="h-3.5 w-3.5 text-amber-500" />
        </div>
      )}
    </Link>
  );

  if (locked && lockedTooltip) {
    return (
      <TooltipProvider>
        <Tooltip>
          <TooltipTrigger asChild>{tile}</TooltipTrigger>
          <TooltipContent>
            <p className="text-xs">{lockedTooltip}</p>
          </TooltipContent>
        </Tooltip>
      </TooltipProvider>
    );
  }

  return tile;
}

function LeadStatusBadge({ status }: { status: string }) {
  const config: Record<string, { label: string; className: string }> = {
    new: { label: 'Nuevo', className: 'bg-primary/10 text-primary' },
    contacted: { label: 'Contactado', className: 'bg-blue-100 text-blue-700' },
    qualified: { label: 'Calificado', className: 'bg-purple-100 text-purple-700' },
    closed: { label: 'Cerrado', className: 'bg-green-100 text-green-700' },
    lost: { label: 'Perdido', className: 'bg-muted text-muted-foreground' },
  };
  const c = config[status] ?? { label: status, className: 'bg-muted text-foreground' };
  return <Badge className={c.className}>{c.label}</Badge>;
}

function DealerVerificationBanner() {
  const { canSell, isPending, isRejected, needsVerification, isLoading, rejectionReason } =
    useCanSell();

  if (isLoading || canSell) return null;

  if (isPending) {
    return (
      <Card className="border-purple-200 bg-purple-50">
        <CardContent className="pt-6">
          <div className="flex items-start gap-4">
            <div className="flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-full bg-purple-100">
              <Clock className="h-6 w-6 text-purple-600" />
            </div>
            <div className="flex-1">
              <h3 className="mb-1 font-semibold text-purple-900">
                Verificación del dealer en proceso
              </h3>
              <p className="text-sm text-purple-700">
                Tu documentación está siendo revisada por nuestro equipo. Te notificaremos por
                correo electrónico cuando sea aprobada (24–48 horas hábiles).
              </p>
            </div>
          </div>
        </CardContent>
      </Card>
    );
  }

  if (isRejected) {
    return (
      <Card className="border-red-200 bg-red-50">
        <CardContent className="pt-6">
          <div className="flex items-start gap-4">
            <div className="flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-full bg-red-100">
              <AlertCircle className="h-6 w-6 text-red-600" />
            </div>
            <div className="flex-1">
              <h3 className="mb-1 font-semibold text-red-900">Verificación rechazada</h3>
              <p className="mb-3 text-sm text-red-700">
                La verificación de tu concesionario fue rechazada. Revisa los motivos y vuelve a
                enviar tu documentación.
              </p>
              {rejectionReason && (
                <p className="mb-3 rounded bg-red-100 px-3 py-2 text-sm text-red-700">
                  <strong>Motivo:</strong> {rejectionReason}
                </p>
              )}
              <Link href="/cuenta/verificacion">
                <Button
                  size="sm"
                  variant="outline"
                  className="border-red-300 text-red-700 hover:bg-red-100"
                >
                  Reintentar verificación
                </Button>
              </Link>
            </div>
          </div>
        </CardContent>
      </Card>
    );
  }

  if (needsVerification) {
    return (
      <Card className="border-amber-200 bg-amber-50">
        <CardContent className="pt-6">
          <div className="flex items-start gap-4">
            <div className="flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-full bg-amber-100">
              <Shield className="h-6 w-6 text-amber-600" />
            </div>
            <div className="flex-1">
              <h3 className="mb-1 font-semibold text-amber-900">
                Verifica tu concesionario para publicar vehículos
              </h3>
              <p className="mb-2 text-sm text-amber-700">
                Antes de publicar tu inventario debes verificar tu identidad y los documentos de tu
                negocio. Necesitarás:
              </p>
              <ul className="mb-3 space-y-1 text-sm text-amber-700">
                <li className="flex items-center gap-2">
                  <span className="h-1.5 w-1.5 rounded-full bg-amber-500" />
                  Cédula de identidad del representante legal
                </li>
                <li className="flex items-center gap-2">
                  <span className="h-1.5 w-1.5 rounded-full bg-amber-500" />
                  RNC del negocio
                </li>
                <li className="flex items-center gap-2">
                  <span className="h-1.5 w-1.5 rounded-full bg-amber-500" />
                  Registro Mercantil o licencia comercial
                </li>
                <li className="flex items-center gap-2">
                  <span className="h-1.5 w-1.5 rounded-full bg-amber-500" />
                  Selfie de verificación biométrica
                </li>
              </ul>
              <Link href="/cuenta/verificacion">
                <Button size="sm" className="gap-2 bg-amber-600 text-white hover:bg-amber-700">
                  <Shield className="h-4 w-4" />
                  Iniciar verificación del dealer
                </Button>
              </Link>
            </div>
          </div>
        </CardContent>
      </Card>
    );
  }

  return null;
}
