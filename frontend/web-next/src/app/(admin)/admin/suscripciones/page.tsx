/**
 * Admin Subscriptions Management Page
 *
 * Manage dealer subscription plans and subscriptions
 */

'use client';

import * as React from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Input } from '@/components/ui/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  CreditCard,
  Users,
  DollarSign,
  TrendingUp,
  AlertCircle,
  Search,
  Crown,
  Star,
  Zap,
  Eye,
} from 'lucide-react';

interface Subscription {
  id: string;
  dealerName: string;
  dealerEmail: string;
  plan: 'libre' | 'visible' | 'pro' | 'elite';
  status: 'active' | 'trial' | 'past_due' | 'cancelled' | 'expired';
  amount: number;
  billingCycle: 'monthly' | 'annual';
  startDate: string;
  nextBillingDate: string;
  vehicleLimit: number;
  vehiclesUsed: number;
}

const planConfig = {
  libre: { label: 'LIBRE', icon: Zap, color: 'text-gray-600', bg: 'bg-gray-50', price: 0 },
  visible: {
    label: 'VISIBLE',
    icon: Eye,
    color: 'text-blue-600',
    bg: 'bg-blue-50',
    price: 1699,
  },
  pro: {
    label: 'PRO',
    icon: Star,
    color: 'text-purple-600',
    bg: 'bg-purple-50',
    price: 5199,
  },
  elite: {
    label: 'ÉLITE',
    icon: Crown,
    color: 'text-amber-600',
    bg: 'bg-amber-50',
    price: 11599,
  },
};

const statusLabels: Record<string, string> = {
  active: 'Activa',
  trial: 'Prueba',
  past_due: 'Vencida',
  cancelled: 'Cancelada',
  expired: 'Expirada',
};

const statusVariants: Record<string, 'default' | 'secondary' | 'danger' | 'outline'> = {
  active: 'default',
  trial: 'secondary',
  past_due: 'danger',
  cancelled: 'outline',
  expired: 'outline',
};

export default function SuscripcionesPage() {
  const [subscriptions, setSubscriptions] = React.useState<Subscription[]>([]);
  const [loading, setLoading] = React.useState(true);
  const [error, setError] = React.useState<string | null>(null);
  const [search, setSearch] = React.useState('');
  const [planFilter, setPlanFilter] = React.useState<string>('all');
  const [statusFilter, setStatusFilter] = React.useState<string>('all');

  React.useEffect(() => {
    async function fetchSubscriptions() {
      try {
        setLoading(true);
        // TODO: Replace with real API call
        // const data = await adminService.getSubscriptions({ search, plan: planFilter, status: statusFilter });
        setSubscriptions([]);
      } catch (err) {
        console.error('Error fetching subscriptions:', err);
        setError('No se pudieron cargar las suscripciones.');
      } finally {
        setLoading(false);
      }
    }
    fetchSubscriptions();
  }, []);

  const filteredSubscriptions = subscriptions.filter(s => {
    const matchesSearch =
      !search ||
      s.dealerName.toLowerCase().includes(search.toLowerCase()) ||
      s.dealerEmail.toLowerCase().includes(search.toLowerCase());
    const matchesPlan = planFilter === 'all' || s.plan === planFilter;
    const matchesStatus = statusFilter === 'all' || s.status === statusFilter;
    return matchesSearch && matchesPlan && matchesStatus;
  });

  const stats = React.useMemo(() => {
    const active = subscriptions.filter(s => s.status === 'active' || s.status === 'trial');
    const mrr = active.reduce(
      (sum, s) => sum + (s.billingCycle === 'annual' ? s.amount / 12 : s.amount),
      0
    );
    const byPlan: Record<string, number> = {};
    active.forEach(s => {
      byPlan[s.plan] = (byPlan[s.plan] || 0) + 1;
    });
    return { total: subscriptions.length, active: active.length, mrr, byPlan };
  }, [subscriptions]);

  if (loading) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold">Suscripciones</h1>
        <div className="grid gap-4 sm:grid-cols-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <Card key={i}>
              <CardContent className="p-6">
                <Skeleton className="h-20 w-full" />
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex min-h-[300px] items-center justify-center">
        <div className="text-center">
          <AlertCircle className="mx-auto h-12 w-12 text-red-500" />
          <p className="text-muted-foreground mt-4">{error}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-foreground text-2xl font-bold">Suscripciones</h1>
        <p className="text-muted-foreground">Gestiona las suscripciones de dealers</p>
      </div>

      {/* Overview Stats */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardContent className="flex items-center gap-4 p-6">
            <div className="rounded-lg bg-blue-50 p-3">
              <Users className="h-6 w-6 text-blue-600" />
            </div>
            <div>
              <p className="text-muted-foreground text-sm">Total</p>
              <p className="text-2xl font-bold">{stats.total}</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-6">
            <div className="rounded-lg bg-green-50 p-3">
              <CreditCard className="h-6 w-6 text-green-600" />
            </div>
            <div>
              <p className="text-muted-foreground text-sm">Activas</p>
              <p className="text-2xl font-bold">{stats.active}</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-6">
            <div className="bg-primary/10 rounded-lg p-3">
              <DollarSign className="text-primary h-6 w-6" />
            </div>
            <div>
              <p className="text-muted-foreground text-sm">MRR</p>
              <p className="text-2xl font-bold">RD${stats.mrr.toLocaleString()}</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-6">
            <div className="rounded-lg bg-purple-50 p-3">
              <TrendingUp className="h-6 w-6 text-purple-600" />
            </div>
            <div>
              <p className="text-muted-foreground text-sm">Tasa Conversión</p>
              <p className="text-2xl font-bold">N/A</p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Plan Distribution */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {Object.entries(planConfig).map(([key, plan]) => {
          const PlanIcon = plan.icon;
          const count = stats.byPlan[key] ?? 0;
          return (
            <Card key={key}>
              <CardContent className="flex items-center gap-4 p-6">
                <div className={`rounded-lg ${plan.bg} p-3`}>
                  <PlanIcon className={`h-6 w-6 ${plan.color}`} />
                </div>
                <div className="flex-1">
                  <p className="text-muted-foreground text-sm">{plan.label}</p>
                  <p className="text-xl font-bold">{count} suscriptores</p>
                  <p className="text-muted-foreground text-xs">RD${plan.price}/mes</p>
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>

      {/* Filters */}
      <Card>
        <CardContent className="flex flex-col gap-4 p-4 sm:flex-row">
          <div className="relative flex-1">
            <Search className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
            <Input
              placeholder="Buscar dealer..."
              className="pl-9"
              value={search}
              onChange={e => setSearch(e.target.value)}
            />
          </div>
          <Select value={planFilter} onValueChange={setPlanFilter}>
            <SelectTrigger className="w-full sm:w-[160px]">
              <SelectValue placeholder="Plan" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Todos los Planes</SelectItem>
              <SelectItem value="libre">LIBRE</SelectItem>
              <SelectItem value="visible">VISIBLE</SelectItem>
              <SelectItem value="pro">PRO</SelectItem>
              <SelectItem value="elite">ÉLITE</SelectItem>
            </SelectContent>
          </Select>
          <Select value={statusFilter} onValueChange={setStatusFilter}>
            <SelectTrigger className="w-full sm:w-[160px]">
              <SelectValue placeholder="Estado" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Todos</SelectItem>
              <SelectItem value="active">Activa</SelectItem>
              <SelectItem value="trial">Prueba</SelectItem>
              <SelectItem value="past_due">Vencida</SelectItem>
              <SelectItem value="cancelled">Cancelada</SelectItem>
            </SelectContent>
          </Select>
        </CardContent>
      </Card>

      {/* Subscriptions Table */}
      {filteredSubscriptions.length === 0 ? (
        <div className="flex min-h-[200px] items-center justify-center">
          <div className="text-center">
            <CreditCard className="text-muted-foreground mx-auto h-12 w-12" />
            <p className="text-muted-foreground mt-4">
              No hay suscripciones con los filtros seleccionados
            </p>
          </div>
        </div>
      ) : (
        <Card>
          <CardContent className="p-0">
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-muted/50 border-b">
                    <th className="px-4 py-3 text-left font-medium">Dealer</th>
                    <th className="px-4 py-3 text-left font-medium">Plan</th>
                    <th className="px-4 py-3 text-left font-medium">Ciclo</th>
                    <th className="px-4 py-3 text-right font-medium">Monto</th>
                    <th className="px-4 py-3 text-center font-medium">Vehículos</th>
                    <th className="px-4 py-3 text-center font-medium">Estado</th>
                    <th className="px-4 py-3 text-left font-medium">Próximo Cobro</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredSubscriptions.map(sub => {
                    const plan = planConfig[sub.plan];
                    const PlanIcon = plan.icon;
                    return (
                      <tr key={sub.id} className="hover:bg-muted/30 border-b last:border-0">
                        <td className="px-4 py-3">
                          <p className="font-medium">{sub.dealerName}</p>
                          <p className="text-muted-foreground text-xs">{sub.dealerEmail}</p>
                        </td>
                        <td className="px-4 py-3">
                          <span className="flex items-center gap-1.5">
                            <PlanIcon className={`h-4 w-4 ${plan.color}`} />
                            {plan.label}
                          </span>
                        </td>
                        <td className="px-4 py-3">
                          {sub.billingCycle === 'monthly' ? 'Mensual' : 'Anual'}
                        </td>
                        <td className="px-4 py-3 text-right font-medium">
                          RD${sub.amount.toLocaleString()}
                        </td>
                        <td className="px-4 py-3 text-center">
                          {sub.vehiclesUsed}/{sub.vehicleLimit}
                        </td>
                        <td className="px-4 py-3 text-center">
                          <Badge variant={statusVariants[sub.status]}>
                            {statusLabels[sub.status]}
                          </Badge>
                        </td>
                        <td className="px-4 py-3">
                          {new Date(sub.nextBillingDate).toLocaleDateString('es-DO')}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
