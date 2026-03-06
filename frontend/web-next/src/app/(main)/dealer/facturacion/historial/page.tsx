/**
 * Dealer Billing History Page
 *
 * Complete billing history with invoices
 * Connected to real APIs - P1 Integration
 */

'use client';

import { useState, useMemo } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  ArrowLeft,
  Search,
  Download,
  FileText,
  CreditCard,
  DollarSign,
  Calendar,
  CheckCircle,
  XCircle,
  Clock,
  AlertCircle,
  AlertTriangle,
  RefreshCw,
  Loader2,
} from 'lucide-react';
import Link from 'next/link';
import { useCurrentDealer } from '@/hooks/use-dealers';
import {
  useInvoices,
  useBillingStats,
  useSubscription,
  useDownloadInvoice,
} from '@/hooks/use-dealer-billing';
import {
  formatCurrency,
  getInvoiceStatusLabel,
  getInvoiceStatusColor,
  formatBillingDate,
} from '@/services/dealer-billing';
import type { Invoice } from '@/services/dealer-billing';
import { toast } from 'sonner';

// ============================================================================
// Skeleton Components
// ============================================================================

function StatsSkeleton() {
  return (
    <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
      {[1, 2, 3, 4].map(i => (
        <Card key={i}>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <Skeleton className="h-9 w-9 rounded-lg" />
              <div>
                <Skeleton className="mb-1 h-4 w-24" />
                <Skeleton className="h-7 w-20" />
              </div>
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

function InvoicesListSkeleton() {
  return (
    <div className="space-y-4">
      {[1, 2, 3, 4, 5].map(i => (
        <div key={i} className="flex items-center justify-between rounded-lg border p-4">
          <div className="flex items-center gap-4">
            <Skeleton className="h-10 w-10 rounded-lg" />
            <div>
              <Skeleton className="mb-1 h-4 w-40" />
              <Skeleton className="mb-1 h-3 w-56" />
              <Skeleton className="h-3 w-28" />
            </div>
          </div>
          <div className="flex items-center gap-4">
            <Skeleton className="h-6 w-24" />
            <Skeleton className="h-8 w-16" />
          </div>
        </div>
      ))}
    </div>
  );
}

// ============================================================================
// Helper Components
// ============================================================================

function InvoiceStatusBadge({ status }: { status: Invoice['status'] }) {
  const label = getInvoiceStatusLabel(status);
  const colorClass = getInvoiceStatusColor(status);

  const icons = {
    Paid: <CheckCircle className="mr-1 h-3 w-3" />,
    Pending: <Clock className="mr-1 h-3 w-3" />,
    Overdue: <AlertTriangle className="mr-1 h-3 w-3" />,
    Cancelled: <XCircle className="mr-1 h-3 w-3" />,
  };

  return (
    <Badge className={colorClass}>
      {icons[status]}
      {label}
    </Badge>
  );
}

// ============================================================================
// Main Page Component
// ============================================================================

export default function BillingHistoryPage() {
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [yearFilter, setYearFilter] = useState('all');

  // Get dealer data
  const { data: dealer, isLoading: dealerLoading } = useCurrentDealer();
  const dealerId = dealer?.id || '';

  // Get billing data from API
  const {
    data: invoices,
    isLoading: invoicesLoading,
    error: invoicesError,
    refetch,
  } = useInvoices(dealerId);
  const { data: stats, isLoading: statsLoading } = useBillingStats(dealerId);
  const { data: subscription } = useSubscription(dealerId);

  // Mutations
  const downloadInvoice = useDownloadInvoice();

  const isLoading = dealerLoading || invoicesLoading || statsLoading;

  // Filter invoices
  const filteredInvoices = useMemo(() => {
    if (!invoices) return [];
    return invoices.filter(inv => {
      // Search filter
      if (searchQuery) {
        const query = searchQuery.toLowerCase();
        const matchesSearch =
          inv.invoiceNumber.toLowerCase().includes(query) ||
          inv.description.toLowerCase().includes(query);
        if (!matchesSearch) return false;
      }
      // Status filter
      if (statusFilter !== 'all' && inv.status.toLowerCase() !== statusFilter) return false;
      // Year filter
      if (yearFilter !== 'all') {
        const invYear = new Date(inv.createdAt).getFullYear().toString();
        if (invYear !== yearFilter) return false;
      }
      return true;
    });
  }, [invoices, searchQuery, statusFilter, yearFilter]);

  // Calculate stats from real data
  const totalPaid =
    stats?.totalPaid ||
    invoices?.reduce((sum, inv) => (inv.status === 'Paid' ? sum + inv.amount : sum), 0) ||
    0;
  const invoiceCount = stats?.invoiceCount || invoices?.length || 0;
  const monthlyAmount = subscription?.monthlyPrice || stats?.monthlyAverage || 0;
  const nextBillingDate = stats?.nextBillingDate || subscription?.currentPeriodEnd;

  // Get unique years for filter
  const availableYears = useMemo(() => {
    if (!invoices) return [];
    const years = new Set(invoices.map(inv => new Date(inv.createdAt).getFullYear().toString()));
    return Array.from(years).sort().reverse();
  }, [invoices]);

  // Handle download
  const handleDownload = async (invoiceId: string) => {
    try {
      await downloadInvoice.mutateAsync(invoiceId);
      toast.success('Factura descargada');
    } catch {
      toast.error('Error al descargar la factura');
    }
  };

  // Error state
  if (invoicesError) {
    return (
      <div className="space-y-6">
        <div className="flex items-center gap-4">
          <Link href="/dealer/facturacion">
            <Button variant="ghost" size="icon">
              <ArrowLeft className="h-5 w-5" />
            </Button>
          </Link>
          <h1 className="text-2xl font-bold">Historial de Facturación</h1>
        </div>
        <Card>
          <CardContent className="py-12 text-center">
            <AlertCircle className="mx-auto mb-4 h-12 w-12 text-red-400" />
            <p className="mb-2 font-medium text-red-600">Error al cargar las facturas</p>
            <p className="text-muted-foreground mb-4 text-sm">
              No se pudo obtener el historial de facturación.
            </p>
            <Button variant="outline" onClick={() => refetch()}>
              <RefreshCw className="mr-2 h-4 w-4" />
              Reintentar
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Link href="/dealer/facturacion">
            <Button variant="ghost" size="icon">
              <ArrowLeft className="h-5 w-5" />
            </Button>
          </Link>
          <div>
            <h1 className="text-2xl font-bold">Historial de Facturación</h1>
            <p className="text-muted-foreground">Todas tus facturas y transacciones</p>
          </div>
        </div>
        <Button variant="outline" onClick={() => refetch()}>
          <RefreshCw className="mr-2 h-4 w-4" />
          Actualizar
        </Button>
      </div>

      {/* Stats */}
      {isLoading ? (
        <StatsSkeleton />
      ) : (
        <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center gap-3">
                <div className="bg-primary/10 rounded-lg p-2">
                  <DollarSign className="text-primary h-5 w-5" />
                </div>
                <div>
                  <p className="text-muted-foreground text-sm">Total Pagado</p>
                  <p className="text-xl font-bold">{formatCurrency(totalPaid)}</p>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center gap-3">
                <div className="rounded-lg bg-blue-100 p-2">
                  <FileText className="h-5 w-5 text-blue-600" />
                </div>
                <div>
                  <p className="text-muted-foreground text-sm">Facturas</p>
                  <p className="text-xl font-bold">{invoiceCount}</p>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center gap-3">
                <div className="rounded-lg bg-purple-100 p-2">
                  <CreditCard className="h-5 w-5 text-purple-600" />
                </div>
                <div>
                  <p className="text-muted-foreground text-sm">Mensualidad</p>
                  <p className="text-xl font-bold">{formatCurrency(monthlyAmount)}</p>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="pt-6">
              <div className="flex items-center gap-3">
                <div className="rounded-lg bg-orange-100 p-2">
                  <Calendar className="h-5 w-5 text-orange-600" />
                </div>
                <div>
                  <p className="text-muted-foreground text-sm">Próximo Cobro</p>
                  <p className="text-xl font-bold">
                    {nextBillingDate ? formatBillingDate(nextBillingDate) : 'N/A'}
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Filters */}
      <Card>
        <CardContent className="py-4">
          <div className="flex flex-col gap-4 sm:flex-row">
            <div className="relative flex-1">
              <Search className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
              <Input
                placeholder="Buscar por número de factura..."
                className="pl-10"
                value={searchQuery}
                onChange={e => setSearchQuery(e.target.value)}
              />
            </div>
            <Select value={statusFilter} onValueChange={setStatusFilter}>
              <SelectTrigger className="w-40">
                <SelectValue placeholder="Estado" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos</SelectItem>
                <SelectItem value="paid">Pagadas</SelectItem>
                <SelectItem value="pending">Pendientes</SelectItem>
                <SelectItem value="overdue">Vencidas</SelectItem>
                <SelectItem value="cancelled">Canceladas</SelectItem>
              </SelectContent>
            </Select>
            <Select value={yearFilter} onValueChange={setYearFilter}>
              <SelectTrigger className="w-40">
                <SelectValue placeholder="Período" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todo</SelectItem>
                {availableYears.map(year => (
                  <SelectItem key={year} value={year}>
                    {year}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      {/* Invoices List */}
      <Card>
        <CardHeader>
          <CardTitle>Facturas</CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <InvoicesListSkeleton />
          ) : filteredInvoices.length > 0 ? (
            <div className="space-y-4">
              {filteredInvoices.map(invoice => (
                <div
                  key={invoice.id}
                  className="bg-muted/50 hover:bg-muted flex items-center justify-between rounded-lg p-4 transition-colors"
                >
                  <div className="flex items-center gap-4">
                    <div className="bg-card rounded-lg border p-3">
                      <FileText className="text-muted-foreground h-5 w-5" />
                    </div>
                    <div>
                      <div className="flex items-center gap-2">
                        <p className="font-medium">{invoice.invoiceNumber}</p>
                        <InvoiceStatusBadge status={invoice.status} />
                      </div>
                      <p className="text-muted-foreground text-sm">{invoice.description}</p>
                      <p className="text-muted-foreground text-xs">
                        {new Date(invoice.createdAt).toLocaleDateString('es-DO', {
                          year: 'numeric',
                          month: 'long',
                          day: 'numeric',
                        })}
                      </p>
                    </div>
                  </div>

                  <div className="flex items-center gap-6">
                    <div className="text-right">
                      <p className="text-lg font-bold">
                        {formatCurrency(invoice.amount, { currency: invoice.currency })}
                      </p>
                      {invoice.paidAt && (
                        <p className="text-muted-foreground text-xs">
                          Pagada {formatBillingDate(invoice.paidAt)}
                        </p>
                      )}
                    </div>
                    <div className="flex gap-2">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => handleDownload(invoice.id)}
                        disabled={downloadInvoice.isPending}
                      >
                        {downloadInvoice.isPending ? (
                          <Loader2 className="h-4 w-4 animate-spin" />
                        ) : (
                          <Download className="h-4 w-4" />
                        )}
                      </Button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div className="text-muted-foreground py-12 text-center">
              <FileText className="mx-auto mb-4 h-12 w-12 opacity-50" />
              <p className="font-medium">No se encontraron facturas</p>
              <p className="text-sm">
                {searchQuery || statusFilter !== 'all' || yearFilter !== 'all'
                  ? 'Intenta cambiar los filtros de búsqueda'
                  : 'Aún no tienes facturas registradas'}
              </p>
            </div>
          )}

          {/* Results count */}
          {!isLoading && filteredInvoices.length > 0 && (
            <div className="border-border mt-6 flex items-center justify-between border-t pt-4">
              <p className="text-muted-foreground text-sm">
                Mostrando {filteredInvoices.length} de {invoices?.length || 0} facturas
              </p>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
