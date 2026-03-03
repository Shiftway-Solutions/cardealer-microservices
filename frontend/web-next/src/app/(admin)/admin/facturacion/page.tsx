/**
 * Admin Billing Page
 *
 * Platform billing management, revenue tracking
 */

'use client';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  DollarSign,
  TrendingUp,
  CreditCard,
  Users,
  Download,
  Calendar,
  ArrowUpRight,
  ArrowDownRight,
  RefreshCw,
  AlertCircle,
  Loader2,
} from 'lucide-react';
import {
  useRevenueStats,
  useRecentTransactions,
  usePendingPaymentsAdmin,
  useRevenueByPlan,
} from '@/hooks/use-admin-extended';
import { formatPrice } from '@/lib/format';

export default function AdminBillingPage() {
  const { data: revenue, isLoading: loadingRevenue } = useRevenueStats();
  const { data: transactions = [] } = useRecentTransactions();
  const { data: pendingPayments = [] } = usePendingPaymentsAdmin();
  const { data: planRevenue = [], isLoading: loadingPlans } = useRevenueByPlan();

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col justify-between gap-4 sm:flex-row">
        <div>
          <h1 className="text-2xl font-bold">Facturación</h1>
          <p className="text-muted-foreground">Gestión de ingresos y pagos</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline">
            <Calendar className="mr-2 h-4 w-4" />
            Febrero 2024
          </Button>
          <Button variant="outline">
            <Download className="mr-2 h-4 w-4" />
            Exportar
          </Button>
        </div>
      </div>

      {/* Revenue Cards */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <Card className="from-primary to-primary bg-gradient-to-br text-white">
          <CardContent className="p-4">
            <div className="mb-2 flex items-center justify-between">
              <DollarSign className="h-5 w-5 opacity-80" />
              {revenue?.growth != null && (
                <span className="flex items-center text-sm">
                  <ArrowUpRight className="h-4 w-4" />
                  {revenue.growth}%
                </span>
              )}
            </div>
            <p className="text-2xl font-bold">
              {loadingRevenue ? '—' : formatPrice(revenue?.mrr ?? 0)}
            </p>
            <p className="text-sm opacity-80">MRR (Ingreso Mensual)</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="mb-2 flex items-center justify-between">
              <TrendingUp className="h-5 w-5 text-blue-500" />
            </div>
            <p className="text-2xl font-bold">
              {loadingRevenue ? '—' : formatPrice(revenue?.arr ?? 0)}
            </p>
            <p className="text-muted-foreground text-sm">ARR (Ingreso Anual)</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="mb-2 flex items-center justify-between">
              <Users className="h-5 w-5 text-purple-500" />
            </div>
            <p className="text-2xl font-bold">
              {loadingRevenue ? '—' : (revenue?.activeSubscriptions ?? 0)}
            </p>
            <p className="text-muted-foreground text-sm">Suscripciones Activas</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="mb-2 flex items-center justify-between">
              <RefreshCw className="h-5 w-5 text-red-500" />
              <span className="flex items-center text-sm text-red-500">
                <ArrowDownRight className="h-4 w-4" />
                -0.3%
              </span>
            </div>
            <p className="text-2xl font-bold">
              {loadingRevenue ? '—' : `${revenue?.churnRate ?? 0}%`}
            </p>
            <p className="text-muted-foreground text-sm">Tasa de Cancelación</p>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Recent Transactions */}
        <div className="lg:col-span-2">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle>Transacciones Recientes</CardTitle>
              <Button variant="ghost" size="sm">
                Ver todas
              </Button>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                {transactions.map(tx => (
                  <div
                    key={tx.id}
                    className="flex items-center justify-between rounded-lg border p-3"
                  >
                    <div className="flex items-center gap-3">
                      <div
                        className={`rounded-lg p-2 ${
                          tx.status === 'completed' ? 'bg-primary/10' : 'bg-red-100'
                        }`}
                      >
                        <CreditCard
                          className={`h-4 w-4 ${
                            tx.status === 'completed' ? 'text-primary' : 'text-red-600'
                          }`}
                        />
                      </div>
                      <div>
                        <p className="font-medium">{tx.dealerName}</p>
                        <p className="text-muted-foreground text-sm">
                          Plan {tx.plan} • {new Date(tx.date).toLocaleDateString('es-DO')}
                        </p>
                      </div>
                    </div>
                    <div className="text-right">
                      <p className="font-bold">{formatPrice(tx.amount)}</p>
                      <Badge
                        className={
                          tx.status === 'completed'
                            ? 'bg-primary/10 text-primary'
                            : 'bg-red-100 text-red-700'
                        }
                      >
                        {tx.status === 'completed' ? 'Completado' : 'Fallido'}
                      </Badge>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Pending Payments */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <AlertCircle className="h-5 w-5 text-amber-500" />
                Pagos Pendientes
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {pendingPayments.map(payment => (
                <div
                  key={payment.id}
                  className="rounded-lg border border-amber-200 bg-amber-50 p-3"
                >
                  <div className="mb-1 flex justify-between">
                    <span className="font-medium">{payment.dealerName}</span>
                    <span className="font-bold">{formatPrice(payment.amount)}</span>
                  </div>
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">
                      Vence: {new Date(payment.dueDate).toLocaleDateString('es-DO')}
                    </span>
                    {payment.daysOverdue > 0 && (
                      <span className="text-red-600">{payment.daysOverdue} días vencido</span>
                    )}
                  </div>
                </div>
              ))}
              <Button variant="outline" className="w-full">
                Enviar Recordatorios
              </Button>
            </CardContent>
          </Card>

          {/* Revenue by Plan */}
          <Card>
            <CardHeader>
              <CardTitle>Ingresos por Plan</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {loadingPlans ? (
                <div className="flex justify-center py-4">
                  <Loader2 className="h-5 w-5 animate-spin" />
                </div>
              ) : planRevenue.length > 0 ? (
                planRevenue.map(plan => (
                  <div key={plan.plan} className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <div className="bg-primary/100 h-3 w-3 rounded-full" />
                      <span>{plan.plan}</span>
                    </div>
                    <span className="font-medium">{formatPrice(plan.revenue)}</span>
                  </div>
                ))
              ) : (
                <p className="text-muted-foreground text-sm">Sin datos</p>
              )}
            </CardContent>
          </Card>

          {/* Quick Stats */}
          <Card>
            <CardHeader>
              <CardTitle>Promedio por Cliente</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-3xl font-bold">
                {loadingRevenue ? '—' : formatPrice(revenue?.avgRevenuePerUser ?? 0)}
              </p>
              <p className="text-muted-foreground text-sm">Ingreso promedio por dealer</p>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
