/**
 * Admin Transactions Page
 *
 * View and manage all platform payment transactions
 */

'use client';

import * as React from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
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
  DollarSign,
  Search,
  AlertCircle,
  Download,
  CreditCard,
  CheckCircle2,
  XCircle,
  Clock,
  RotateCcw,
  Receipt,
  TrendingUp,
} from 'lucide-react';
import { format } from 'date-fns';
import { es } from 'date-fns/locale';

interface Transaction {
  id: string;
  reference: string;
  dealerName: string;
  dealerEmail: string;
  amount: number;
  fee: number;
  netAmount: number;
  currency: string;
  gateway: 'stripe' | 'azul';
  type: 'subscription' | 'listing' | 'feature' | 'refund';
  status: 'completed' | 'pending' | 'failed' | 'refunded' | 'disputed';
  description: string;
  createdAt: string;
  cardLast4?: string;
}

const statusConfig: Record<
  string,
  {
    label: string;
    variant: 'default' | 'secondary' | 'danger' | 'outline';
    icon: React.ElementType;
  }
> = {
  completed: { label: 'Completada', variant: 'default', icon: CheckCircle2 },
  pending: { label: 'Pendiente', variant: 'secondary', icon: Clock },
  failed: { label: 'Fallida', variant: 'danger', icon: XCircle },
  refunded: { label: 'Reembolsada', variant: 'outline', icon: RotateCcw },
  disputed: { label: 'Disputada', variant: 'danger', icon: AlertCircle },
};

const typeLabels: Record<string, string> = {
  subscription: 'Suscripción',
  listing: 'Publicación',
  feature: 'Destacado',
  refund: 'Reembolso',
};

const gatewayLabels: Record<string, string> = {
  stripe: 'Stripe',
  azul: 'Azul',
};

export default function TransaccionesPage() {
  const [transactions, setTransactions] = React.useState<Transaction[]>([]);
  const [loading, setLoading] = React.useState(true);
  const [error, setError] = React.useState<string | null>(null);
  const [search, setSearch] = React.useState('');
  const [statusFilter, setStatusFilter] = React.useState<string>('all');
  const [gatewayFilter, setGatewayFilter] = React.useState<string>('all');
  const [typeFilter, setTypeFilter] = React.useState<string>('all');

  React.useEffect(() => {
    async function fetchTransactions() {
      try {
        setLoading(true);
        // TODO: Replace with real API call
        // const data = await adminService.getTransactions({ search, status: statusFilter, gateway: gatewayFilter, type: typeFilter });
        setTransactions([]);
      } catch (err) {
        if (process.env.NODE_ENV === 'development')
          console.error('Error fetching transactions:', err);
        setError('No se pudieron cargar las transacciones.');
      } finally {
        setLoading(false);
      }
    }
    fetchTransactions();
  }, []);

  const filteredTransactions = transactions.filter(t => {
    const matchesSearch =
      !search ||
      t.dealerName.toLowerCase().includes(search.toLowerCase()) ||
      t.reference.toLowerCase().includes(search.toLowerCase());
    const matchesStatus = statusFilter === 'all' || t.status === statusFilter;
    const matchesGateway = gatewayFilter === 'all' || t.gateway === gatewayFilter;
    const matchesType = typeFilter === 'all' || t.type === typeFilter;
    return matchesSearch && matchesStatus && matchesGateway && matchesType;
  });

  const stats = React.useMemo(() => {
    const completed = transactions.filter(t => t.status === 'completed');
    const totalRevenue = completed.reduce((s, t) => s + t.amount, 0);
    const totalFees = completed.reduce((s, t) => s + t.fee, 0);
    const totalNet = completed.reduce((s, t) => s + t.netAmount, 0);
    return {
      count: transactions.length,
      revenue: totalRevenue,
      fees: totalFees,
      net: totalNet,
    };
  }, [transactions]);

  if (loading) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold">Transacciones</h1>
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
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-foreground text-2xl font-bold">Transacciones</h1>
          <p className="text-muted-foreground">
            Historial de pagos y transacciones de la plataforma
          </p>
        </div>
        <Button variant="outline" size="sm">
          <Download className="mr-2 h-4 w-4" />
          Exportar CSV
        </Button>
      </div>

      {/* Stats */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardContent className="flex items-center gap-4 p-6">
            <div className="rounded-lg bg-blue-50 p-3">
              <Receipt className="h-6 w-6 text-blue-600" />
            </div>
            <div>
              <p className="text-muted-foreground text-sm">Transacciones</p>
              <p className="text-2xl font-bold">{stats.count}</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-6">
            <div className="rounded-lg bg-green-50 p-3">
              <DollarSign className="h-6 w-6 text-green-600" />
            </div>
            <div>
              <p className="text-muted-foreground text-sm">Ingresos Brutos</p>
              <p className="text-2xl font-bold">RD${stats.revenue.toLocaleString()}</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-6">
            <div className="rounded-lg bg-amber-50 p-3">
              <CreditCard className="h-6 w-6 text-amber-600" />
            </div>
            <div>
              <p className="text-muted-foreground text-sm">Comisiones</p>
              <p className="text-2xl font-bold">RD${stats.fees.toLocaleString()}</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-6">
            <div className="bg-primary/10 rounded-lg p-3">
              <TrendingUp className="text-primary h-6 w-6" />
            </div>
            <div>
              <p className="text-muted-foreground text-sm">Ingresos Netos</p>
              <p className="text-2xl font-bold">RD${stats.net.toLocaleString()}</p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Filters */}
      <Card>
        <CardContent className="flex flex-col gap-4 p-4 sm:flex-row">
          <div className="relative flex-1">
            <Search className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
            <Input
              placeholder="Buscar por dealer o referencia..."
              className="pl-9"
              value={search}
              onChange={e => setSearch(e.target.value)}
            />
          </div>
          <Select value={statusFilter} onValueChange={setStatusFilter}>
            <SelectTrigger className="w-full sm:w-[150px]">
              <SelectValue placeholder="Estado" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Todos</SelectItem>
              <SelectItem value="completed">Completada</SelectItem>
              <SelectItem value="pending">Pendiente</SelectItem>
              <SelectItem value="failed">Fallida</SelectItem>
              <SelectItem value="refunded">Reembolsada</SelectItem>
            </SelectContent>
          </Select>
          <Select value={gatewayFilter} onValueChange={setGatewayFilter}>
            <SelectTrigger className="w-full sm:w-[130px]">
              <SelectValue placeholder="Pasarela" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Todas</SelectItem>
              <SelectItem value="stripe">Stripe</SelectItem>
              <SelectItem value="azul">Azul</SelectItem>
            </SelectContent>
          </Select>
          <Select value={typeFilter} onValueChange={setTypeFilter}>
            <SelectTrigger className="w-full sm:w-[150px]">
              <SelectValue placeholder="Tipo" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Todos</SelectItem>
              <SelectItem value="subscription">Suscripción</SelectItem>
              <SelectItem value="listing">Publicación</SelectItem>
              <SelectItem value="feature">Destacado</SelectItem>
              <SelectItem value="refund">Reembolso</SelectItem>
            </SelectContent>
          </Select>
        </CardContent>
      </Card>

      {/* Transactions Table */}
      {filteredTransactions.length === 0 ? (
        <div className="flex min-h-[200px] items-center justify-center">
          <div className="text-center">
            <Receipt className="text-muted-foreground mx-auto h-12 w-12" />
            <p className="text-muted-foreground mt-4">
              No hay transacciones con los filtros seleccionados
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
                    <th className="px-4 py-3 text-left font-medium">Fecha</th>
                    <th className="px-4 py-3 text-left font-medium">Referencia</th>
                    <th className="px-4 py-3 text-left font-medium">Dealer</th>
                    <th className="px-4 py-3 text-left font-medium">Tipo</th>
                    <th className="px-4 py-3 text-left font-medium">Pasarela</th>
                    <th className="px-4 py-3 text-right font-medium">Monto</th>
                    <th className="px-4 py-3 text-right font-medium">Comisión</th>
                    <th className="px-4 py-3 text-center font-medium">Estado</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredTransactions.map(tx => {
                    const statusInfo = statusConfig[tx.status];
                    const StatusIcon = statusInfo.icon;
                    return (
                      <tr key={tx.id} className="hover:bg-muted/30 border-b last:border-0">
                        <td className="px-4 py-3 whitespace-nowrap">
                          {format(new Date(tx.createdAt), 'dd MMM yyyy HH:mm', { locale: es })}
                        </td>
                        <td className="px-4 py-3 font-mono text-xs">{tx.reference}</td>
                        <td className="px-4 py-3">
                          <p className="font-medium">{tx.dealerName}</p>
                        </td>
                        <td className="px-4 py-3">{typeLabels[tx.type]}</td>
                        <td className="px-4 py-3">
                          <Badge variant="outline">{gatewayLabels[tx.gateway]}</Badge>
                        </td>
                        <td className="px-4 py-3 text-right font-medium">
                          RD${tx.amount.toLocaleString()}
                        </td>
                        <td className="text-muted-foreground px-4 py-3 text-right">
                          RD${tx.fee.toLocaleString()}
                        </td>
                        <td className="px-4 py-3 text-center">
                          <Badge variant={statusInfo.variant}>
                            <StatusIcon className="mr-1 h-3 w-3" />
                            {statusInfo.label}
                          </Badge>
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
