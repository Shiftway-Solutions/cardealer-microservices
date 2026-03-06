/**
 * Account Dashboard Page — Role-Aware
 *
 * Unified entry point for ALL authenticated users.
 * Renders a tailored dashboard based on account type:
 *   - admin / platform_employee  → Platform admin gateway
 *   - dealer / dealer_employee   → Dealer command center
 *   - seller                     → Seller performance hub
 *   - buyer (default)            → Buyer browsing dashboard
 *
 * /dealer and /vender/dashboard now redirect here.
 * Sub-pages at /dealer/** retain their own layout for deep work.
 */

'use client';

import * as React from 'react';
import Link from 'next/link';
import {
  Car,
  Eye,
  MessageSquare,
  Star,
  TrendingUp,
  ArrowRight,
  Plus,
  Clock,
  AlertCircle,
  Loader2,
  Shield,
  Users,
  Building2,
  BarChart3,
  Calendar,
  Package,
  LayoutDashboard,
  CreditCard,
  ShieldCheck,
  ScrollText,
  ChevronRight,
  Heart,
  DollarSign,
  Search,
  Bell,
  History,
  BookOpen,
  Sparkles,
  MapPin,
  Crown,
} from 'lucide-react';
import Image from 'next/image';
import { useQuery } from '@tanstack/react-query';
import { useAuth } from '@/hooks/use-auth';
import { useCanSell } from '@/hooks/use-kyc';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { userService, type UserVehicleDto } from '@/services/users';
import { useDealerDashboard } from '@/hooks/use-dealers';
import { useRecentLeads } from '@/hooks/use-crm';
import { useUpcomingAppointments } from '@/hooks/use-appointments';
import { formatLeadName } from '@/services/crm';
import { getAppointmentTypeLabel } from '@/services/appointments';
import { useSellerByUserId, useSellerStats } from '@/hooks/use-seller';
import { useFavorites } from '@/hooks/use-favorites';
import { useAlertStats } from '@/hooks/use-alerts';
import { PlanBadge } from '@/components/plan/plan-gate';
import { UpgradeBanner } from '@/components/shared/upgrade-banner';

// ============================================================
// MAIN EXPORT — dispatches to the right dashboard by role
// ============================================================

function DashboardLoadingSkeleton() {
  return (
    <div className="animate-pulse space-y-6">
      {/* Welcome header skeleton */}
      <div className="bg-muted h-32 rounded-2xl" />
      {/* Metric cards skeleton */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        {[1, 2, 3, 4].map(i => (
          <div key={i} className="bg-muted h-24 rounded-xl" />
        ))}
      </div>
      {/* List section skeleton */}
      <div className="bg-card space-y-3 rounded-xl border p-4">
        <div className="bg-muted h-5 w-40 rounded" />
        {[1, 2, 3].map(i => (
          <div key={i} className="bg-muted h-12 rounded-lg" />
        ))}
      </div>
      {/* Quick actions skeleton */}
      <div className="bg-card rounded-xl border p-4">
        <div className="bg-muted mb-3 h-5 w-32 rounded" />
        <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
          {[1, 2, 3, 4].map(i => (
            <div key={i} className="bg-muted h-14 rounded-xl" />
          ))}
        </div>
      </div>
    </div>
  );
}

export default function AccountDashboardPage() {
  const { user, isLoading } = useAuth();

  if (isLoading) {
    return <DashboardLoadingSkeleton />;
  }

  if (!user) {
    return (
      <div className="flex min-h-[400px] items-center justify-center">
        <Loader2 className="text-primary h-8 w-8 animate-spin" />
      </div>
    );
  }

  switch (user.accountType) {
    case 'admin':
    case 'platform_employee':
      return <AdminGatewayDashboard />;
    case 'dealer':
    case 'dealer_employee':
      return <DealerDashboard />;
    case 'seller':
      return <SellerDashboard />;
    default:
      return <BuyerDashboard />;
  }
}

// ============================================================
// ADMIN GATEWAY DASHBOARD
// ============================================================

const ADMIN_QUICK_LINKS = [
  {
    href: '/admin',
    label: 'Dashboard Admin',
    icon: LayoutDashboard,
    bg: 'bg-red-100',
    color: 'text-red-600',
  },
  {
    href: '/admin/usuarios',
    label: 'Usuarios',
    icon: Users,
    bg: 'bg-blue-100',
    color: 'text-blue-600',
  },
  {
    href: '/admin/vehiculos',
    label: 'Vehículos',
    icon: Car,
    bg: 'bg-green-100',
    color: 'text-green-600',
  },
  {
    href: '/admin/dealers',
    label: 'Dealers',
    icon: Building2,
    bg: 'bg-purple-100',
    color: 'text-purple-600',
  },
  {
    href: '/admin/kyc',
    label: 'Verificaciones KYC',
    icon: ShieldCheck,
    bg: 'bg-yellow-100',
    color: 'text-yellow-600',
  },
  {
    href: '/admin/reportes',
    label: 'Reportes',
    icon: BarChart3,
    bg: 'bg-indigo-100',
    color: 'text-indigo-600',
  },
  {
    href: '/admin/facturacion',
    label: 'Facturación',
    icon: CreditCard,
    bg: 'bg-emerald-100',
    color: 'text-emerald-600',
  },
  {
    href: '/admin/logs',
    label: 'Logs del Sistema',
    icon: ScrollText,
    bg: 'bg-gray-100',
    color: 'text-gray-600',
  },
];

function AdminGatewayDashboard() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-foreground text-2xl font-bold">Panel de Administración</h1>
        <p className="text-muted-foreground">Gestión y operaciones de la plataforma OKLA</p>
      </div>

      <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
        {ADMIN_QUICK_LINKS.map(link => (
          <Link
            key={link.href}
            href={link.href}
            className="border-border hover:border-primary hover:bg-primary/5 group flex flex-col items-center gap-3 rounded-xl border p-4 transition-all"
          >
            <div className={`flex h-12 w-12 items-center justify-center rounded-xl ${link.bg}`}>
              <link.icon className={`h-6 w-6 ${link.color}`} />
            </div>
            <span className="text-foreground text-center text-sm font-medium">{link.label}</span>
          </Link>
        ))}
      </div>

      <Card className="border-red-100 bg-red-50">
        <CardContent className="flex flex-col items-start justify-between gap-4 pt-6 sm:flex-row sm:items-center">
          <div className="flex items-center gap-3">
            <ShieldCheck className="h-6 w-6 flex-shrink-0 text-red-600" />
            <div>
              <p className="font-semibold text-red-900">Acceso Completo al Panel Admin</p>
              <p className="text-sm text-red-700">
                Dashboard, KYC, moderación, reportes y configuración
              </p>
            </div>
          </div>
          <Link href="/admin">
            <Button className="gap-2 bg-red-600 text-white hover:bg-red-700">
              Ir al Panel
              <ArrowRight className="h-4 w-4" />
            </Button>
          </Link>
        </CardContent>
      </Card>
    </div>
  );
}

// ============================================================
// DEALER DASHBOARD
// ============================================================

function DealerDashboard() {
  const { dealer, stats, isLoading: dealerLoading } = useDealerDashboard();
  const { data: leads, isLoading: leadsLoading } = useRecentLeads(5);
  const { data: appointments, isLoading: appointmentsLoading } = useUpcomingAppointments(
    dealer?.id ?? '',
    4
  );
  const { canSell, isLoading: kycLoading } = useCanSell();

  return (
    <div className="space-y-6">
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
          <Link href="/dealer/inventario/nuevo">
            <Button className="gap-2">
              <Plus className="h-4 w-4" />
              Nuevo Vehículo
            </Button>
          </Link>
        )}
      </div>

      {/* Verification banner — shown while unverified */}
      <DealerVerificationBanner />

      {/* Upgrade banner — shown to dealers on free plan */}
      <UpgradeBanner
        variant="inline"
        userType="dealer"
        upgradeUrl="/cuenta/upgrade?plan=visible&type=dealer"
      />

      {dealerLoading ? (
        <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
          {[1, 2, 3, 4].map(i => (
            <Skeleton key={i} className="h-24 rounded-xl" />
          ))}
        </div>
      ) : (
        <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
          <MetricCard
            title="Vehículos Activos"
            value={stats?.activeListings ?? 0}
            icon={Car}
            color="blue"
          />
          <MetricCard
            title="Vistas del Mes"
            value={stats?.viewsThisMonth ?? 0}
            icon={Eye}
            color="green"
          />
          <MetricCard
            title="Consultas Activas"
            value={stats?.pendingInquiries ?? 0}
            icon={MessageSquare}
            color="purple"
          />
          <MetricCard
            title="Ingresos del Mes"
            value={
              stats?.revenueThisMonth ? `RD$${(stats.revenueThisMonth / 1000).toFixed(0)}K` : '—'
            }
            icon={DollarSign}
            color="yellow"
          />
        </div>
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
            <QuickActionTile href="/dealer/leads" icon={Users} label="Ver Leads" color="purple" />
            <QuickActionTile
              href="/dealer/analytics"
              icon={TrendingUp}
              label="Analytics"
              color="green"
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

// ============================================================
// SELLER DASHBOARD
// ============================================================

function SellerDashboard() {
  const { user } = useAuth();
  const { data: sellerProfile, isLoading: profileLoading } = useSellerByUserId(user?.id);
  const { data: sellerStats, isLoading: statsLoading } = useSellerStats(sellerProfile?.id);
  const { canSell, isLoading: kycLoading } = useCanSell();
  const { data: vehiclesData, isLoading: vehiclesLoading } = useQuery({
    queryKey: ['user-vehicles-recent'],
    queryFn: () => userService.getUserVehicles({ limit: 3, status: 'all' }),
    staleTime: 60_000, // 1 minute — avoids re-fetch on every dashboard visit
  });
  const recentVehicles = vehiclesData?.vehicles ?? [];

  const isLoading = profileLoading || statsLoading;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <div className="flex items-center gap-3">
            <h1 className="text-foreground text-2xl font-bold">Mi Panel de Vendedor</h1>
            <PlanBadge showUpgradeLink />
          </div>
          <p className="text-muted-foreground">Gestiona tus publicaciones y consultas</p>
        </div>
        {!kycLoading && canSell && (
          <Link href="/vender/publicar">
            <Button className="gap-2">
              <Plus className="h-4 w-4" />
              Publicar Vehículo
            </Button>
          </Link>
        )}
      </div>

      {/* Verification banner — shown while unverified with "Verificar ahora" button inside */}
      <VerificationBanner />

      {/* Seller profile setup banner — shown after KYC approval when profile not yet configured */}
      <SellerProfileBanner />

      {/* Upgrade banner — shown to sellers on free plan */}
      <UpgradeBanner
        variant="inline"
        userType="seller"
        upgradeUrl="/cuenta/upgrade?plan=premium&type=seller"
      />

      {isLoading ? (
        <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
          {[1, 2, 3, 4].map(i => (
            <Skeleton key={i} className="h-24 rounded-xl" />
          ))}
        </div>
      ) : (
        <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
          <MetricCard
            title="Vehículos Activos"
            value={sellerStats?.activeListings ?? 0}
            icon={Car}
            color="blue"
          />
          <MetricCard
            title="Ventas Completadas"
            value={sellerStats?.totalSales ?? 0}
            icon={TrendingUp}
            color="green"
          />
          <MetricCard
            title="Calificación"
            value={sellerStats?.averageRating ? `${sellerStats.averageRating.toFixed(1)}★` : '—'}
            icon={Star}
            color="yellow"
          />
          <MetricCard
            title="Tasa de Respuesta"
            value={sellerStats?.responseRate ? `${Math.round(sellerStats.responseRate)}%` : '—'}
            icon={MessageSquare}
            color="purple"
          />
        </div>
      )}

      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle className="text-base">Mis Vehículos Recientes</CardTitle>
          <Button variant="ghost" size="sm" asChild>
            <Link href="/cuenta/mis-vehiculos" className="flex items-center gap-1">
              Ver todos <ArrowRight className="h-4 w-4" />
            </Link>
          </Button>
        </CardHeader>
        <CardContent>
          {vehiclesLoading ? (
            <div className="space-y-3">
              {[1, 2, 3].map(i => (
                <Skeleton key={i} className="h-16 rounded-lg" />
              ))}
            </div>
          ) : recentVehicles.length > 0 ? (
            <div className="space-y-3">
              {recentVehicles.map(v => (
                <VehicleListItem key={v.id} vehicle={v} />
              ))}
            </div>
          ) : (
            <div className="py-8 text-center">
              <Car className="text-muted-foreground mx-auto mb-4 h-12 w-12" />
              <p className="text-muted-foreground mb-4">No tienes vehículos publicados</p>
              <Link href="/vender/publicar">
                <Button>Publicar mi primer vehículo</Button>
              </Link>
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Acciones Rápidas</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 gap-3 md:grid-cols-5">
            <QuickActionTile
              href="/cuenta/mis-vehiculos"
              icon={Car}
              label="Mis Vehículos"
              color="blue"
            />
            <QuickActionTile
              href="/cuenta/consultas"
              icon={MessageSquare}
              label="Consultas"
              color="purple"
            />
            <QuickActionTile
              href="/cuenta/estadisticas"
              icon={BarChart3}
              label="Estadísticas"
              color="green"
            />
            <QuickActionTile href="/cuenta/pagos" icon={CreditCard} label="Pagos" color="yellow" />
            <QuickActionTile
              href="/cuenta/suscripcion"
              icon={Sparkles}
              label="Mi Plan"
              color="green"
            />
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

// ============================================================
// BUYER DASHBOARD — Focused on searching, saving & buying
// ============================================================

function BuyerDashboard() {
  const { user } = useAuth();
  const { favorites, count: favCount, isLoading: favLoading } = useFavorites();
  const { data: alertStats, isLoading: alertsLoading } = useAlertStats();

  const firstName = user?.firstName?.split(' ')[0] || 'Bienvenido';

  // 3 most recent favorites for preview section
  const recentFavorites = favorites.slice(0, 3);

  return (
    <div className="space-y-6">
      {/* ── Welcome Header ─────────────────────────────────── */}
      <div className="from-primary/5 to-primary/10 rounded-2xl bg-gradient-to-br p-6">
        <div className="flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-center">
          <div>
            <h1 className="text-foreground text-2xl font-bold">¡Hola, {firstName}! 👋</h1>
            <p className="text-muted-foreground mt-1">Encuentra tu próximo vehículo en OKLA</p>
          </div>
          <Link href="/vehiculos">
            <Button className="gap-2" size="lg">
              <Search className="h-5 w-5" />
              Buscar Vehículos
            </Button>
          </Link>
        </div>
      </div>

      {/* ── Activity Summary ───────────────────────────────── */}
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-3">
        <Link
          href="/cuenta/favoritos"
          className="border-border bg-card group rounded-xl border p-4 text-center transition-all hover:border-rose-300 hover:bg-rose-50"
          data-testid="summary-favorites"
        >
          <div className="mx-auto mb-2 flex h-11 w-11 items-center justify-center rounded-xl bg-rose-100 transition-colors group-hover:bg-rose-200">
            <Heart className="h-5 w-5 text-rose-600" />
          </div>
          {favLoading ? (
            <div className="mx-auto mb-1 h-7 w-8 animate-pulse rounded bg-rose-100" />
          ) : (
            <p className="text-foreground text-2xl font-bold">{favCount}</p>
          )}
          <p className="text-muted-foreground text-xs">Favoritos</p>
        </Link>

        <Link
          href="/cuenta/busquedas"
          className="border-border bg-card group rounded-xl border p-4 text-center transition-all hover:border-blue-300 hover:bg-blue-50"
          data-testid="summary-saved-searches"
        >
          <div className="mx-auto mb-2 flex h-11 w-11 items-center justify-center rounded-xl bg-blue-100 transition-colors group-hover:bg-blue-200">
            <BookOpen className="h-5 w-5 text-blue-600" />
          </div>
          {alertsLoading ? (
            <div className="mx-auto mb-1 h-7 w-8 animate-pulse rounded bg-blue-100" />
          ) : (
            <p className="text-foreground text-2xl font-bold">
              {alertStats?.activeSavedSearches ?? 0}
            </p>
          )}
          <p className="text-muted-foreground text-xs">Búsquedas</p>
        </Link>

        <Link
          href="/cuenta/alertas"
          className="border-border bg-card group rounded-xl border p-4 text-center transition-all hover:border-amber-300 hover:bg-amber-50"
          data-testid="summary-alerts"
        >
          <div className="mx-auto mb-2 flex h-11 w-11 items-center justify-center rounded-xl bg-amber-100 transition-colors group-hover:bg-amber-200">
            <Bell className="h-5 w-5 text-amber-600" />
          </div>
          {alertsLoading ? (
            <div className="mx-auto mb-1 h-7 w-8 animate-pulse rounded bg-amber-100" />
          ) : (
            <p className="text-foreground text-2xl font-bold">
              {alertStats?.activePriceAlerts ?? 0}
            </p>
          )}
          <p className="text-muted-foreground text-xs">Alertas de Precio</p>
        </Link>
      </div>

      {/* ── Price Drop Alert ── shown only when there are drops ─ */}
      {!alertsLoading && (alertStats?.priceDropsThisMonth ?? 0) > 0 && (
        <Card className="border-green-200 bg-green-50">
          <CardContent className="flex items-center justify-between gap-4 pt-4 pb-4">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-full bg-green-100">
                <TrendingUp className="h-5 w-5 text-green-600" />
              </div>
              <div>
                <p className="font-semibold text-green-900">
                  {alertStats!.priceDropsThisMonth} bajada
                  {alertStats!.priceDropsThisMonth > 1 ? 's' : ''} de precio este mes
                </p>
                <p className="text-sm text-green-700">Revisa tus alertas para ver los detalles</p>
              </div>
            </div>
            <Link href="/cuenta/alertas">
              <Button
                size="sm"
                variant="outline"
                className="gap-1.5 border-green-400 text-green-700 hover:bg-green-100"
              >
                Ver alertas <ArrowRight className="h-3.5 w-3.5" />
              </Button>
            </Link>
          </CardContent>
        </Card>
      )}

      {/* ── Quick Actions ─────────────────────────────────── */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Acciones Rápidas</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-6">
            <BuyerQuickAction href="/vehiculos" icon={Search} label="Buscar" color="blue" />
            <BuyerQuickAction
              href="/cuenta/favoritos"
              icon={Heart}
              label="Favoritos"
              color="rose"
            />
            <BuyerQuickAction
              href="/cuenta/busquedas"
              icon={BookOpen}
              label="Búsquedas"
              color="purple"
            />
            <BuyerQuickAction href="/cuenta/alertas" icon={Bell} label="Alertas" color="amber" />
            <BuyerQuickAction
              href="/mensajes"
              icon={MessageSquare}
              label="Mensajes"
              color="green"
            />
            <BuyerQuickAction
              href="/cuenta/historial"
              icon={History}
              label="Historial"
              color="gray"
            />
          </div>
        </CardContent>
      </Card>

      {/* ── Recent Favorites ─────────────────────────────── */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between pb-2">
          <CardTitle className="flex items-center gap-2 text-base">
            <Heart className="h-4 w-4 text-rose-500" />
            Mis Favoritos Recientes
          </CardTitle>
          <Button variant="ghost" size="sm" asChild>
            <Link href="/cuenta/favoritos" className="flex items-center gap-1">
              Ver todos <ArrowRight className="h-4 w-4" />
            </Link>
          </Button>
        </CardHeader>
        <CardContent>
          {favLoading ? (
            <div className="space-y-3">
              {[1, 2, 3].map(i => (
                <div key={i} className="flex animate-pulse gap-3">
                  <div className="bg-muted h-16 w-24 rounded-lg" />
                  <div className="flex-1 space-y-2">
                    <div className="bg-muted h-4 w-3/4 rounded" />
                    <div className="bg-muted h-4 w-1/2 rounded" />
                  </div>
                </div>
              ))}
            </div>
          ) : recentFavorites.length > 0 ? (
            <div className="space-y-3">
              {recentFavorites.map(fav => (
                <FavoriteMiniCard key={fav.id} favorite={fav} />
              ))}
            </div>
          ) : (
            <div className="py-8 text-center">
              <Heart className="text-muted-foreground mx-auto mb-3 h-10 w-10" />
              <p className="text-foreground font-medium">Aún no tienes favoritos</p>
              <p className="text-muted-foreground mt-1 mb-4 text-sm">
                Guarda los vehículos que te interesen para compararlos luego
              </p>
              <Link href="/vehiculos">
                <Button variant="outline" className="gap-2">
                  <Search className="h-4 w-4" />
                  Explorar vehículos
                </Button>
              </Link>
            </div>
          )}
        </CardContent>
      </Card>

      {/* ── New Matches from Saved Searches ────────────────── */}
      {!alertsLoading && (alertStats?.newMatchesThisWeek ?? 0) > 0 && (
        <Card className="border-blue-100 bg-blue-50">
          <CardContent className="flex items-center justify-between gap-4 pt-4 pb-4">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-full bg-blue-100">
                <Sparkles className="h-5 w-5 text-blue-600" />
              </div>
              <div>
                <p className="font-semibold text-blue-900">
                  {alertStats!.newMatchesThisWeek} vehículo
                  {alertStats!.newMatchesThisWeek > 1 ? 's nuevos' : ' nuevo'} esta semana
                </p>
                <p className="text-sm text-blue-700">Coinciden con tus búsquedas guardadas</p>
              </div>
            </div>
            <Link href="/cuenta/busquedas">
              <Button
                size="sm"
                variant="outline"
                className="gap-1.5 border-blue-400 text-blue-700 hover:bg-blue-100"
              >
                Ver resultados <ArrowRight className="h-3.5 w-3.5" />
              </Button>
            </Link>
          </CardContent>
        </Card>
      )}
    </div>
  );
}

// ── Buyer-specific helpers ────────────────────────────────────────────────────

type BuyerActionColor = 'blue' | 'rose' | 'purple' | 'amber' | 'green' | 'gray';

function BuyerQuickAction({
  href,
  icon: Icon,
  label,
  color,
}: {
  href: string;
  icon: React.ElementType;
  label: string;
  color: BuyerActionColor;
}) {
  const colorMap: Record<BuyerActionColor, string> = {
    blue: 'bg-blue-100 text-blue-600 group-hover:bg-blue-200',
    rose: 'bg-rose-100 text-rose-600 group-hover:bg-rose-200',
    purple: 'bg-purple-100 text-purple-600 group-hover:bg-purple-200',
    amber: 'bg-amber-100 text-amber-600 group-hover:bg-amber-200',
    green: 'bg-green-100 text-green-600 group-hover:bg-green-200',
    gray: 'bg-gray-100 text-gray-600 group-hover:bg-gray-200',
  };
  return (
    <Link
      href={href}
      className="border-border hover:border-primary/20 group flex flex-col items-center gap-2 rounded-xl border p-3 transition-all"
    >
      <div
        className={`flex h-10 w-10 items-center justify-center rounded-xl transition-colors ${colorMap[color]}`}
      >
        <Icon className="h-5 w-5" />
      </div>
      <span className="text-foreground text-center text-xs font-medium">{label}</span>
    </Link>
  );
}

function FavoriteMiniCard({
  favorite,
}: {
  favorite: import('@/services/favorites').FavoriteVehicle;
}) {
  const v = favorite.vehicle;
  const title = v.title || `${v.year} ${v.make} ${v.model}`;
  const href = v.slug ? `/vehiculos/${v.slug}` : '/vehiculos';

  return (
    <Link
      href={href}
      className="border-border hover:border-primary/30 hover:bg-muted/40 flex items-center gap-3 rounded-lg border p-3 transition-all"
    >
      {/* Image */}
      <div className="bg-muted relative h-14 w-20 flex-shrink-0 overflow-hidden rounded-lg">
        {v.imageUrl ? (
          <Image src={v.imageUrl} alt={title} fill className="object-cover" sizes="80px" />
        ) : (
          <div className="flex h-full w-full items-center justify-center text-2xl">🚗</div>
        )}
      </div>
      {/* Info */}
      <div className="min-w-0 flex-1">
        <p className="text-foreground line-clamp-1 text-sm font-medium">{title}</p>
        <p className="text-primary text-sm font-bold">
          {v.price ? `RD$${v.price.toLocaleString()}` : '—'}
        </p>
        {v.location && (
          <p className="text-muted-foreground flex items-center gap-1 text-xs">
            <MapPin className="h-3 w-3" />
            {v.location}
          </p>
        )}
      </div>
      <ArrowRight className="text-muted-foreground h-4 w-4 flex-shrink-0" />
    </Link>
  );
}

// ============================================================
// SHARED UI COMPONENTS
// ============================================================

function MetricCard({
  title,
  value,
  icon: Icon,
  color,
  suffix,
}: {
  title: string;
  value: number | string;
  icon: React.ElementType;
  color: 'blue' | 'green' | 'purple' | 'yellow';
  suffix?: string;
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
              {suffix && (
                <span className="text-muted-foreground ml-1 text-sm font-normal">{suffix}</span>
              )}
            </p>
            <p className="text-muted-foreground text-sm">{title}</p>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

/** QuickActionTile: compact tile (Dealer/Seller dashboards) */
function QuickActionTile({
  href,
  icon: Icon,
  label,
  color,
}: {
  href: string;
  icon: React.ElementType;
  label: string;
  color: 'blue' | 'green' | 'purple' | 'yellow';
}) {
  const colorClasses = {
    blue: 'bg-blue-100 text-blue-600',
    green: 'bg-green-100 text-green-600',
    purple: 'bg-purple-100 text-purple-600',
    yellow: 'bg-yellow-100 text-yellow-600',
  };
  return (
    <Link
      href={href}
      className="border-border hover:border-primary hover:bg-primary/5 flex flex-col items-center gap-2 rounded-xl border p-4 transition-all"
    >
      <div
        className={`flex h-11 w-11 items-center justify-center rounded-xl ${colorClasses[color]}`}
      >
        <Icon className="h-5 w-5" />
      </div>
      <span className="text-foreground text-center text-sm font-medium">{label}</span>
    </Link>
  );
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

/** QuickAction: large tile (BuyerDashboard) */
function _QuickAction({
  href,
  icon: Icon,
  label,
  color,
}: {
  href: string;
  icon: React.ElementType;
  label: string;
  color: 'blue' | 'green' | 'purple' | 'yellow';
}) {
  const colorClasses = {
    blue: 'bg-blue-100 text-blue-600 group-hover:bg-blue-200',
    green: 'bg-green-100 text-green-600 group-hover:bg-green-200',
    purple: 'bg-purple-100 text-purple-600 group-hover:bg-purple-200',
    yellow: 'bg-yellow-100 text-yellow-600 group-hover:bg-yellow-200',
  };

  return (
    <Link
      href={href}
      className="group border-border hover:border-border flex flex-col items-center gap-3 rounded-lg border p-4 transition-all hover:shadow-sm"
    >
      <div
        className={`flex h-12 w-12 items-center justify-center rounded-lg transition-colors ${colorClasses[color]}`}
      >
        <Icon className="h-6 w-6" />
      </div>
      <span className="text-foreground text-center text-sm font-medium">{label}</span>
    </Link>
  );
}

// Vehicle List Item Component
function VehicleListItem({ vehicle }: { vehicle: UserVehicleDto }) {
  const statusConfig: Record<string, { label: string; className: string }> = {
    active: { label: 'Activo', className: 'bg-green-100 text-green-700' },
    pending: { label: 'Pendiente', className: 'bg-yellow-100 text-yellow-700' },
    paused: { label: 'Pausado', className: 'bg-muted text-foreground' },
    sold: { label: 'Vendido', className: 'bg-blue-100 text-blue-700' },
    expired: { label: 'Expirado', className: 'bg-red-100 text-red-700' },
    rejected: { label: 'Rechazado', className: 'bg-red-100 text-red-700' },
  };

  const config = statusConfig[vehicle.status] || statusConfig.pending;
  const daysUntilExpiry = Math.ceil(
    (new Date(vehicle.expiresAt).getTime() - Date.now()) / (1000 * 60 * 60 * 24) // eslint-disable-line react-hooks/purity
  );
  const isExpiringSoon = daysUntilExpiry <= 7 && daysUntilExpiry > 0 && vehicle.status === 'active';

  return (
    <div className="border-border hover:border-border flex gap-4 rounded-lg border p-4 transition-colors">
      {/* Image */}
      <div className="bg-muted relative h-18 w-24 flex-shrink-0 overflow-hidden rounded-lg">
        <Image
          src={vehicle.imageUrl || '/images/vehicle-placeholder.jpg'}
          alt={vehicle.title}
          fill
          className="object-cover"
          sizes="96px"
        />
      </div>

      {/* Content */}
      <div className="min-w-0 flex-1">
        <div className="flex items-start justify-between gap-2">
          <div>
            <Link
              href={`/vehiculos/${vehicle.slug}`}
              className="hover:text-primary text-foreground line-clamp-1 font-medium"
            >
              {vehicle.title}
            </Link>
            <p className="text-primary text-lg font-bold">
              {vehicle.currency === 'USD' ? 'US$' : 'RD$'}
              {vehicle.price.toLocaleString()}
            </p>
          </div>
          <Badge className={config.className}>{config.label}</Badge>
        </div>

        <div className="text-muted-foreground mt-2 flex items-center gap-4 text-sm">
          <span className="flex items-center gap-1">
            <Eye className="h-4 w-4" />
            {vehicle.viewCount}
          </span>
          <span className="flex items-center gap-1">
            <MessageSquare className="h-4 w-4" />
            {vehicle.inquiryCount}
          </span>
          {isExpiringSoon && (
            <span className="flex items-center gap-1 text-amber-600">
              <Clock className="h-4 w-4" />
              Expira en {daysUntilExpiry} días
            </span>
          )}
        </div>
      </div>

      {/* Actions */}
      <div className="flex items-center">
        <Link href={`/cuenta/mis-vehiculos/${vehicle.id}/editar`}>
          <Button variant="outline" size="sm">
            Editar
          </Button>
        </Link>
      </div>
    </div>
  );
}

// Verification Banner Component
function VerificationBanner() {
  const { canSell, isPending, isRejected, needsVerification, isLoading, rejectionReason } =
    useCanSell();

  // Don't show anything while loading or if verified
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
              <h3 className="mb-1 font-semibold text-purple-900">Verificación en proceso</h3>
              <p className="text-sm text-purple-700">
                Tu solicitud de verificación está siendo revisada. Te notificaremos cuando esté
                lista.
              </p>
            </div>
          </div>
        </CardContent>
      </Card>
    );
  }

  // Verification rejected
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
                Tu solicitud de verificación fue rechazada. Por favor, inténtalo de nuevo.
              </p>
              {rejectionReason && (
                <p className="mb-3 text-sm text-red-600">
                  <strong>Motivo:</strong> {rejectionReason}
                </p>
              )}
              <Link href="/cuenta/verificacion">
                <Button
                  size="sm"
                  variant="outline"
                  className="border-red-300 text-red-700 hover:bg-red-100"
                >
                  Intentar de nuevo
                </Button>
              </Link>
            </div>
          </div>
        </CardContent>
      </Card>
    );
  }

  // Needs verification
  if (needsVerification) {
    return (
      <Card className="border-blue-200 bg-blue-50">
        <CardContent className="pt-6">
          <div className="flex items-start gap-4">
            <div className="flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-full bg-blue-100">
              <Shield className="h-6 w-6 text-blue-600" />
            </div>
            <div className="flex-1">
              <h3 className="mb-1 font-semibold text-blue-900">
                Verifica tu identidad para vender
              </h3>
              <p className="mb-3 text-sm text-blue-700">
                Para publicar vehículos necesitas verificar tu identidad con tu cédula. Es rápido y
                seguro.
              </p>
              <Link href="/cuenta/verificacion">
                <Button size="sm" className="gap-2 bg-blue-600 hover:bg-blue-700">
                  <Shield className="h-4 w-4" />
                  Verificar ahora
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

// Seller Profile Setup Banner — shown when KYC is approved but seller profile not yet configured
function SellerProfileBanner() {
  const { user } = useAuth();
  const { canSell, isLoading: kycLoading } = useCanSell();
  const sellerQuery = useSellerByUserId(canSell ? user?.id : undefined);

  // Only show when verified AND seller profile doesn't exist yet
  if (kycLoading || !canSell || sellerQuery.isLoading) return null;
  if (sellerQuery.data) return null; // Profile already configured

  return (
    <Card className="border-amber-200 bg-amber-50">
      <CardContent className="pt-6">
        <div className="flex items-start gap-4">
          <div className="flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-full bg-amber-100">
            <ShieldCheck className="h-6 w-6 text-amber-600" />
          </div>
          <div className="flex-1">
            <h3 className="mb-1 font-semibold text-amber-900">
              Perfil de vendedor pendiente de configuración
            </h3>
            <p className="mb-3 text-sm text-amber-700">
              Tu identidad fue verificada con éxito. Ahora configura tu perfil de vendedor para
              comenzar a publicar vehículos.
            </p>
            <Link href="/vender/registro">
              <Button size="sm" className="gap-2 bg-amber-600 hover:bg-amber-700">
                <ArrowRight className="h-4 w-4" />
                Completar ahora
              </Button>
            </Link>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

// Dealer-specific Verification Banner (requires RNC + business docs)
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
