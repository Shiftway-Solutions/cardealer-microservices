/**
 * Payment Transaction History Page
 *
 * Displays all payment transactions for the current user.
 * Route: /cuenta/historial
 */

'use client';

import * as React from 'react';
import Link from 'next/link';
import { useQuery } from '@tanstack/react-query';
import {
  Receipt,
  CreditCard,
  CheckCircle,
  XCircle,
  AlertCircle,
  Clock,
  Loader2,
  RefreshCw,
  Filter,
  DollarSign,
  TrendingUp,
  ChevronRight,
  ChevronLeft,
} from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import {
  userBillingService,
  type UserTransaction,
  type TransactionStatus,
  type UserBillingSummary,
} from '@/services/user-billing';
import { cn } from '@/lib/utils';

// =============================================================================
// LOADING STATE
// =============================================================================

function HistorialLoading() {
  return (
    <div className="space-y-6">
      <div>
        <Skeleton className="mb-2 h-8 w-48" />
        <Skeleton className="h-4 w-64" />
      </div>
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {[1, 2, 3, 4].map(i => (
          <Card key={i}>
            <CardContent className="pt-6">
              <Skeleton className="mb-2 h-4 w-24" />
              <Skeleton className="h-8 w-32" />
            </CardContent>
          </Card>
        ))}
      </div>
      <Card>
        <CardContent className="pt-6">
          <Skeleton className="h-64 w-full" />
        </CardContent>
      </Card>
    </div>
  );
}

// =============================================================================
// ERROR STATE
// =============================================================================

function HistorialError({ onRetry }: { onRetry: () => void }) {
  return (
    <Card>
      <CardContent className="flex flex-col items-center py-16 text-center">
        <AlertCircle className="mb-4 h-12 w-12 text-red-400" />
        <h3 className="mb-2 font-semibold">Error al cargar el historial</h3>
        <p className="text-muted-foreground mb-4 text-sm">
          No se pudo cargar tu historial de transacciones
        </p>
        <Button variant="outline" onClick={onRetry}>
          <RefreshCw className="mr-2 h-4 w-4" />
          Reintentar
        </Button>
      </CardContent>
    </Card>
  );
}

// =============================================================================
// EMPTY STATE
// =============================================================================

function EmptyState({ filtered }: { filtered?: boolean }) {
  return (
    <Card>
      <CardContent className="py-16 text-center">
        <Receipt className="mx-auto mb-4 h-16 w-16 text-gray-300" />
        <h3 className="mb-2 text-xl font-semibold">
          {filtered ? 'Sin transacciones para este filtro' : 'Sin historial de pagos'}
        </h3>
        <p className="text-muted-foreground mb-6">
          {filtered
            ? 'Prueba con otro filtro de estado'
            : 'Cuando realices tu primer pago, las transacciones aparecerán aquí'}
        </p>
        {!filtered && (
          <Button asChild>
            <Link href="/cuenta/pagos">
              <CreditCard className="mr-2 h-4 w-4" />
              Administrar métodos de pago
            </Link>
          </Button>
        )}
      </CardContent>
    </Card>
  );
}

// =============================================================================
// STAT CARD
// =============================================================================

function StatCard({
  title,
  value,
  description,
  icon: Icon,
  iconClass,
  bgClass,
}: {
  title: string;
  value: string | number;
  description?: string;
  icon: React.ElementType;
  iconClass?: string;
  bgClass?: string;
}) {
  return (
    <Card>
      <CardContent className="pt-6">
        <div className="flex items-start justify-between">
          <div>
            <p className="text-muted-foreground text-sm font-medium">{title}</p>
            <p className="mt-1 text-2xl font-semibold">{value}</p>
            {description && <p className="text-muted-foreground mt-1 text-sm">{description}</p>}
          </div>
          <div className={cn('rounded-lg p-3', bgClass ?? 'bg-blue-50')}>
            <Icon className={cn('h-5 w-5', iconClass ?? 'text-blue-600')} />
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

// =============================================================================
// TRANSACTION ROW
// =============================================================================

function TransactionRow({ transaction }: { transaction: UserTransaction }) {
  const statusConfig: Record<
    TransactionStatus,
    { icon: React.ElementType; label: string; colorClass: string }
  > = {
    Approved: {
      icon: CheckCircle,
      label: 'Aprobada',
      colorClass: 'text-green-600 bg-green-50 border-green-200',
    },
    Declined: {
      icon: XCircle,
      label: 'Declinada',
      colorClass: 'text-red-600 bg-red-50 border-red-200',
    },
    Cancelled: {
      icon: AlertCircle,
      label: 'Cancelada',
      colorClass: 'text-amber-600 bg-amber-50 border-amber-200',
    },
    Error: {
      icon: XCircle,
      label: 'Error',
      colorClass: 'text-red-600 bg-red-50 border-red-200',
    },
  };

  const config = statusConfig[transaction.status] ?? statusConfig.Error;
  const StatusIcon = config.icon;

  return (
    <TableRow>
      <TableCell className="font-medium">
        <div className="flex items-center gap-2">
          <Receipt className="text-muted-foreground h-4 w-4 flex-shrink-0" />
          <span className="font-mono text-sm">{transaction.orderNumber}</span>
        </div>
      </TableCell>
      <TableCell>
        <p className="text-muted-foreground line-clamp-1 max-w-[180px] text-sm">
          {transaction.description || '—'}
        </p>
      </TableCell>
      <TableCell>
        <div className="flex flex-col">
          <span className="font-semibold">
            {userBillingService.formatCurrency(transaction.total, transaction.currency)}
          </span>
          {transaction.itbis > 0 && (
            <span className="text-muted-foreground text-xs">
              ITBIS: {userBillingService.formatCurrency(transaction.itbis, transaction.currency)}
            </span>
          )}
        </div>
      </TableCell>
      <TableCell>
        <Badge variant="outline" className={cn('gap-1 border', config.colorClass)}>
          <StatusIcon className="h-3 w-3" />
          {config.label}
        </Badge>
      </TableCell>
      <TableCell>
        <div className="text-muted-foreground flex items-center gap-1.5 text-sm">
          <Clock className="h-3.5 w-3.5 flex-shrink-0" />
          {userBillingService.formatDateTime(transaction.transactionDate)}
        </div>
      </TableCell>
      <TableCell>
        {transaction.cardBrand ? (
          <div className="flex items-center gap-1.5 text-sm">
            <CreditCard className="text-muted-foreground h-4 w-4 flex-shrink-0" />
            <span className="font-medium">{transaction.cardBrand}</span>
            {transaction.cardLast4 && (
              <span className="text-muted-foreground">•••• {transaction.cardLast4}</span>
            )}
          </div>
        ) : (
          <span className="text-muted-foreground text-sm">—</span>
        )}
      </TableCell>
    </TableRow>
  );
}

// =============================================================================
// MAIN PAGE
// =============================================================================

const STATUS_OPTIONS = [
  { label: 'Todos los estados', value: 'all' },
  { label: 'Aprobadas', value: 'Approved' },
  { label: 'Declinadas', value: 'Declined' },
  { label: 'Canceladas', value: 'Cancelled' },
] as const;

const PAGE_SIZE = 10;

export default function HistorialPage() {
  const [currentPage, setCurrentPage] = React.useState(1);
  const [statusFilter, setStatusFilter] = React.useState<string>('all');

  const statusParam = statusFilter !== 'all' ? (statusFilter as TransactionStatus) : undefined;

  // Fetch billing summary for stats
  const { data: summary, isLoading: isLoadingSummary } = useQuery<UserBillingSummary>({
    queryKey: ['user-billing-summary'],
    queryFn: () => userBillingService.getBillingSummary(),
  });

  // Fetch paginated transactions
  const {
    data: transactions,
    isLoading: isLoadingTransactions,
    error: transactionsError,
    refetch,
  } = useQuery<UserTransaction[]>({
    queryKey: ['user-transactions', currentPage, statusFilter],
    queryFn: () =>
      userBillingService.getTransactions({
        page: currentPage,
        pageSize: PAGE_SIZE,
        status: statusParam,
      }),
  });

  const hasTransactions = transactions && transactions.length > 0;
  const hasPrevPage = currentPage > 1;
  const hasNextPage = transactions && transactions.length === PAGE_SIZE;

  const handleStatusChange = (value: string) => {
    setStatusFilter(value);
    setCurrentPage(1);
  };

  // Initial loading
  if (isLoadingSummary && !summary) {
    return <HistorialLoading />;
  }

  // Error state
  if (transactionsError && !isLoadingTransactions) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold">Historial de Pagos</h1>
          <p className="text-muted-foreground">Todas tus transacciones y compras</p>
        </div>
        <HistorialError onRetry={refetch} />
      </div>
    );
  }

  const declinedCount = Math.max(
    0,
    (summary?.totalTransactions ?? 0) - (summary?.totalApproved ?? 0)
  );

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
        <div>
          <h1 className="text-2xl font-bold">Historial de Pagos</h1>
          <p className="text-muted-foreground">Todas tus transacciones y compras</p>
        </div>
        <Button variant="outline" size="sm" asChild>
          <Link href="/cuenta/pagos">
            <CreditCard className="mr-2 h-4 w-4" />
            Métodos de pago
          </Link>
        </Button>
      </div>

      {/* Stats */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard
          title="Total pagado"
          value={
            summary
              ? userBillingService.formatCurrency(summary.totalAmount, summary.currency)
              : 'RD$ 0.00'
          }
          description="Monto acumulado"
          icon={DollarSign}
          bgClass="bg-blue-50"
          iconClass="text-blue-600"
        />
        <StatCard
          title="Transacciones"
          value={summary?.totalTransactions ?? 0}
          description="Total histórico"
          icon={Receipt}
          bgClass="bg-purple-50"
          iconClass="text-purple-600"
        />
        <StatCard
          title="Aprobadas"
          value={summary?.totalApproved ?? 0}
          description="Pagos exitosos"
          icon={TrendingUp}
          bgClass="bg-green-50"
          iconClass="text-green-600"
        />
        <StatCard
          title="Declinadas / Error"
          value={declinedCount}
          description="Pagos fallidos"
          icon={XCircle}
          bgClass="bg-red-50"
          iconClass="text-red-600"
        />
      </div>

      {/* Transactions Table */}
      <Card>
        <CardHeader>
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <CardTitle className="flex items-center gap-2">
                <Receipt className="h-5 w-5" />
                Transacciones
              </CardTitle>
              <CardDescription>
                {summary?.totalTransactions
                  ? `${summary.totalTransactions} transacciones en total`
                  : 'Tu historial de pagos'}
              </CardDescription>
            </div>

            {/* Status Filter */}
            <div className="flex items-center gap-2">
              <Filter className="text-muted-foreground h-4 w-4" />
              <Select value={statusFilter} onValueChange={handleStatusChange}>
                <SelectTrigger className="w-[200px]">
                  <SelectValue placeholder="Filtrar por estado" />
                </SelectTrigger>
                <SelectContent>
                  {STATUS_OPTIONS.map(opt => (
                    <SelectItem key={opt.value} value={opt.value}>
                      {opt.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardHeader>

        <CardContent>
          {isLoadingTransactions ? (
            <div className="flex items-center justify-center py-12">
              <Loader2 className="h-8 w-8 animate-spin text-blue-600" />
            </div>
          ) : !hasTransactions ? (
            <EmptyState filtered={statusFilter !== 'all'} />
          ) : (
            <>
              <div className="rounded-lg border">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Orden</TableHead>
                      <TableHead>Descripción</TableHead>
                      <TableHead>Monto</TableHead>
                      <TableHead>Estado</TableHead>
                      <TableHead>Fecha</TableHead>
                      <TableHead>Método</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {transactions.map(tx => (
                      <TransactionRow key={tx.id} transaction={tx} />
                    ))}
                  </TableBody>
                </Table>
              </div>

              {/* Pagination */}
              <div className="mt-4 flex items-center justify-between">
                <p className="text-muted-foreground text-sm">
                  Página {currentPage} · {transactions.length} resultados
                </p>
                <div className="flex gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={!hasPrevPage}
                    onClick={() => setCurrentPage(p => p - 1)}
                  >
                    <ChevronLeft className="mr-1 h-4 w-4" />
                    Anterior
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={!hasNextPage}
                    onClick={() => setCurrentPage(p => p + 1)}
                  >
                    Siguiente
                    <ChevronRight className="ml-1 h-4 w-4" />
                  </Button>
                </div>
              </div>
            </>
          )}
        </CardContent>
      </Card>

      {/* Help */}
      <Card className="border-blue-100 bg-blue-50/50">
        <CardContent className="flex items-center justify-between py-4">
          <div className="flex items-center gap-3">
            <AlertCircle className="h-5 w-5 text-blue-600" />
            <div>
              <p className="font-medium text-blue-900">¿Tienes dudas sobre un pago?</p>
              <p className="text-sm text-blue-700">
                Nuestro equipo de soporte está disponible para ayudarte.
              </p>
            </div>
          </div>
          <Button variant="outline" size="sm" className="border-blue-300 text-blue-700">
            Contactar soporte
            <ChevronRight className="ml-1 h-4 w-4" />
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}
