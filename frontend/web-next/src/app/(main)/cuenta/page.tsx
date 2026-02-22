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
} from 'lucide-react';
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

// ============================================================
// MAIN EXPORT — dispatches to the right dashboard by role
// ============================================================

export default function AccountDashboardPage() {
  const { user } = useAuth();

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
            <h1 className="text-foreground text-2xl font-bold">
              {dealer?.businessName ?? 'Portal Dealer'}
            </h1>
          )}
          <p className="text-muted-foreground">Panel de control del concesionario</p>
        </div>
        {!kycLoading && (
          canSell ? (
            <Link href="/dealer/inventario/nuevo">
              <Button className="gap-2">
                <Plus className="h-4 w-4" />
                Nuevo Vehículo
              </Button>
            </Link>
          ) : (
            <Link href="/cuenta/verificacion">
              <Button variant="outline" className="gap-2 border-amber-300 text-amber-700 hover:bg-amber-50">
                <Shield className="h-4 w-4" />
                Verificar para publicar
              </Button>
            </Link>
          )
        )}
      </div>

      {/* Verification banner — shown while unverified */}
      <DealerVerificationBanner />

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
          <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
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
  const [recentVehicles, setRecentVehicles] = React.useState<UserVehicleDto[]>([]);
  const [vehiclesLoading, setVehiclesLoading] = React.useState(true);
  const { canSell, isLoading: kycLoading } = useCanSell();

  React.useEffect(() => {
    userService
      .getUserVehicles({ limit: 3, status: 'all' })
      .then(r => setRecentVehicles(r.vehicles))
      .catch(() => setRecentVehicles([]))
      .finally(() => setVehiclesLoading(false));
  }, []);

  const isLoading = profileLoading || statsLoading;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-foreground text-2xl font-bold">Mi Panel de Vendedor</h1>
          <p className="text-muted-foreground">Gestiona tus publicaciones y consultas</p>
        </div>
        {!kycLoading && (
          canSell ? (
            <Link href="/vender/publicar">
              <Button className="gap-2">
                <Plus className="h-4 w-4" />
                Publicar Vehículo
              </Button>
            </Link>
          ) : (
            <Link href="/cuenta/verificacion">
              <Button variant="outline" className="gap-2 border-amber-300 text-amber-700 hover:bg-amber-50">
                <Shield className="h-4 w-4" />
                Verificar para publicar
              </Button>
            </Link>
          )
        )}
      </div>

      {/* Verification banner — shown while unverified */}
      <VerificationBanner />

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
          <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
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
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

// ============================================================
// BUYER DASHBOARD (original content, preserved)
// ============================================================

function BuyerDashboard() {
  const [stats, setStats] = React.useState<{
    vehiclesPublished: number;
    totalViews: number;
    totalInquiries: number;
    averageRating: number;
    reviewCount: number;
  } | null>(null);
  const [recentVehicles, setRecentVehicles] = React.useState<UserVehicleDto[]>([]);
  const [loading, setLoading] = React.useState(true);
  const [error, setError] = React.useState<string | null>(null);

  React.useEffect(() => {
    async function fetchData() {
      try {
        setLoading(true);
        setError(null);

        // Fetch user stats
        const statsData = await userService.getUserStats().catch(() => null);
        setStats(
          statsData ?? {
            vehiclesPublished: 0,
            totalViews: 0,
            totalInquiries: 0,
            averageRating: 0,
            reviewCount: 0,
          }
        );

        // Fetch recent vehicles
        const vehiclesResult = await userService
          .getUserVehicles({ limit: 3, status: 'all' })
          .catch(() => ({ vehicles: [], total: 0 }));
        setRecentVehicles(vehiclesResult.vehicles);
      } catch (err) {
        console.error('Error fetching dashboard data:', err);
        setError('No se pudieron cargar los datos. Por favor, intenta de nuevo.');
      } finally {
        setLoading(false);
      }
    }

    fetchData();
  }, []);

  if (loading) {
    return (
      <div className="flex min-h-[400px] items-center justify-center">
        <div className="text-center">
          <Loader2 className="mx-auto h-12 w-12 animate-spin text-blue-600" />
          <p className="text-muted-foreground mt-4">Cargando tu dashboard...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex min-h-[400px] items-center justify-center">
        <div className="text-center">
          <AlertCircle className="mx-auto h-12 w-12 text-red-600" />
          <p className="text-muted-foreground mt-4">{error}</p>
          <Button onClick={() => window.location.reload()} className="mt-4">
            Reintentar
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-foreground text-2xl font-bold">Dashboard</h1>
          <p className="text-muted-foreground">Bienvenido a tu panel de control</p>
        </div>
        <Link href="/vender">
          <Button className="gap-2">
            <Plus className="h-4 w-4" />
            Publicar Vehículo
          </Button>
        </Link>
      </div>

      {/* Verification Banner */}
      <VerificationBanner />

      {/* Stats Grid */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <MetricCard
          title="Vehículos Activos"
          value={stats?.vehiclesPublished ?? 0}
          icon={Car}
          color="blue"
        />
        <MetricCard
          title="Vistas Totales"
          value={stats?.totalViews ?? 0}
          icon={Eye}
          color="green"
        />
        <MetricCard
          title="Consultas"
          value={stats?.totalInquiries ?? 0}
          icon={MessageSquare}
          color="purple"
        />
        <MetricCard
          title="Calificación"
          value={stats?.averageRating ? `${stats.averageRating.toFixed(1)}` : '—'}
          icon={Star}
          color="yellow"
          suffix={stats?.averageRating ? `(${stats.reviewCount})` : ''}
        />
      </div>

      {/* Quick Actions */}
      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Acciones Rápidas</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
            <QuickAction href="/vender" icon={Plus} label="Publicar Vehículo" color="green" />
            <QuickAction
              href="/cuenta/mis-vehiculos"
              icon={Car}
              label="Ver Mis Vehículos"
              color="blue"
            />
            <QuickAction
              href="/cuenta/mensajes"
              icon={MessageSquare}
              label="Ver Mensajes"
              color="purple"
            />
            <QuickAction
              href="/cuenta/favoritos"
              icon={Star}
              label="Mis Favoritos"
              color="yellow"
            />
          </div>
        </CardContent>
      </Card>

      {/* Recent Vehicles */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle className="text-lg">Mis Vehículos Recientes</CardTitle>
          <Link href="/cuenta/mis-vehiculos">
            <Button variant="ghost" size="sm" className="gap-1">
              Ver todos
              <ArrowRight className="h-4 w-4" />
            </Button>
          </Link>
        </CardHeader>
        <CardContent>
          {recentVehicles.length > 0 ? (
            <div className="space-y-4">
              {recentVehicles.map(vehicle => (
                <VehicleListItem key={vehicle.id} vehicle={vehicle} />
              ))}
            </div>
          ) : (
            <div className="py-8 text-center">
              <Car className="text-muted-foreground mx-auto mb-4 h-12 w-12" />
              <p className="text-muted-foreground mb-4">No tienes vehículos publicados</p>
              <Link href="/vender">
                <Button>Publicar mi primer vehículo</Button>
              </Link>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Performance Tips */}
      <Card className="border-blue-100 bg-blue-50">
        <CardContent className="pt-6">
          <div className="flex gap-4">
            <div className="flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-full bg-blue-100">
              <TrendingUp className="h-6 w-6 text-blue-600" />
            </div>
            <div>
              <h3 className="text-foreground mb-1 font-semibold">Mejora tus resultados</h3>
              <p className="text-muted-foreground mb-3 text-sm">
                Los vehículos con fotos profesionales y descripciones completas reciben hasta 3x más
                consultas.
              </p>
              <Link href="/ayuda/consejos-vendedor">
                <Button variant="outline" size="sm">
                  Ver consejos
                </Button>
              </Link>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
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
function QuickAction({
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
    (new Date(vehicle.expiresAt).getTime() - Date.now()) / (1000 * 60 * 60 * 24)
  );
  const isExpiringSoon = daysUntilExpiry <= 7 && daysUntilExpiry > 0 && vehicle.status === 'active';

  return (
    <div className="border-border hover:border-border flex gap-4 rounded-lg border p-4 transition-colors">
      {/* Image */}
      <div className="bg-muted h-18 w-24 flex-shrink-0 overflow-hidden rounded-lg">
        <img
          src={vehicle.imageUrl || '/images/vehicle-placeholder.jpg'}
          alt={vehicle.title}
          className="h-full w-full object-cover"
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
              <h3 className="mb-1 font-semibold text-purple-900">Verificación del dealer en proceso</h3>
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
                <Button size="sm" variant="outline" className="border-red-300 text-red-700 hover:bg-red-100">
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
                <Button size="sm" className="gap-2 bg-amber-600 hover:bg-amber-700 text-white">
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