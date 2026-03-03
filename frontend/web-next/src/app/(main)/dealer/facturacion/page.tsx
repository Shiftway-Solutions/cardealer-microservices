/**
 * Dealer Billing Page
 *
 * Manage invoices, payments, and billing history
 */

'use client';

import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import {
  CreditCard,
  Download,
  FileText,
  DollarSign,
  Calendar,
  ChevronRight,
  Check,
  AlertCircle,
  Clock,
  AlertTriangle,
  Loader2,
} from 'lucide-react';
import { useCurrentDealer } from '@/hooks/use-dealers';
import {
  useInvoices,
  usePaymentMethods,
  useSubscription,
  useBillingStats,
  useDownloadInvoice,
} from '@/hooks/use-dealer-billing';
import {
  formatCurrency,
  getInvoiceStatusLabel,
  getInvoiceStatusColor,
  getPaymentMethodBrand,
  formatBillingDate,
} from '@/services/dealer-billing';
import type { Invoice } from '@/services/dealer-billing';
import { toast } from 'sonner';

// ============================================================================
// Loading Skeletons
// ============================================================================

function StatsSkeleton() {
  return (
    <div className="grid gap-4 md:grid-cols-3">
      {[1, 2, 3].map(i => (
        <Card key={i}>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <Skeleton className="h-9 w-9 rounded-lg" />
              <div>
                <Skeleton className="mb-1 h-4 w-24" />
                <Skeleton className="h-8 w-20" />
              </div>
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

function InvoicesSkeleton() {
  return (
    <div className="space-y-3">
      {[1, 2, 3, 4].map(i => (
        <div key={i} className="flex items-center justify-between rounded-lg border p-4">
          <div className="flex items-center gap-4">
            <Skeleton className="h-9 w-9 rounded-lg" />
            <div>
              <Skeleton className="mb-1 h-4 w-48" />
              <Skeleton className="h-3 w-32" />
            </div>
          </div>
          <div className="flex items-center gap-4">
            <div className="text-right">
              <Skeleton className="mb-1 h-5 w-20" />
              <Skeleton className="h-5 w-16" />
            </div>
            <Skeleton className="h-8 w-8" />
          </div>
        </div>
      ))}
    </div>
  );
}

function PaymentMethodsSkeleton() {
  return (
    <div className="space-y-3">
      {[1, 2].map(i => (
        <div key={i} className="flex items-center justify-between rounded-lg border p-3">
          <div className="flex items-center gap-3">
            <Skeleton className="h-9 w-9 rounded" />
            <div>
              <Skeleton className="mb-1 h-4 w-28" />
              <Skeleton className="h-3 w-16" />
            </div>
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
    Paid: <Check className="mr-1 h-3 w-3" />,
    Pending: <Clock className="mr-1 h-3 w-3" />,
    Overdue: <AlertTriangle className="mr-1 h-3 w-3" />,
    Cancelled: <AlertCircle className="mr-1 h-3 w-3" />,
    Refunded: <AlertCircle className="mr-1 h-3 w-3" />,
  };

  return (
    <Badge className={colorClass}>
      {icons[status]}
      {label}
    </Badge>
  );
}

export default function DealerBillingPage() {
  // Get current dealer
  const { data: dealer, isLoading: dealerLoading } = useCurrentDealer();
  const dealerId = dealer?.id || '';

  // Get billing data
  const { data: invoices, isLoading: invoicesLoading } = useInvoices(dealerId);
  const { data: paymentMethods, isLoading: methodsLoading } = usePaymentMethods(dealerId);
  const { data: subscription, isLoading: subLoading } = useSubscription(dealerId);
  const { data: stats, isLoading: statsLoading } = useBillingStats(dealerId);

  // Mutations
  const downloadInvoice = useDownloadInvoice();

  const isLoading =
    dealerLoading || invoicesLoading || methodsLoading || subLoading || statsLoading;

  // Handle invoice download
  const handleDownload = async (invoiceId: string) => {
    try {
      await downloadInvoice.mutateAsync(invoiceId);
      toast.success('Factura descargada');
    } catch {
      toast.error('Error al descargar la factura');
    }
  };

  // Calculate stats from data
  const totalPaidThisYear =
    stats?.totalPaid ||
    invoices?.reduce((sum, inv) => (inv.status === 'Paid' ? sum + inv.amount : sum), 0) ||
    0;
  const monthlyAmount = subscription?.monthlyPrice || 0;
  const nextPaymentDate = stats?.nextBillingDate
    ? formatBillingDate(stats.nextBillingDate)
    : subscription?.currentPeriodEnd
      ? formatBillingDate(subscription.currentPeriodEnd)
      : 'N/A';
  const defaultMethod = paymentMethods?.find(m => m.isDefault);

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="flex flex-col justify-between gap-4 sm:flex-row">
          <div>
            <Skeleton className="mb-2 h-8 w-40" />
            <Skeleton className="h-4 w-60" />
          </div>
          <Skeleton className="h-10 w-40" />
        </div>
        <StatsSkeleton />
        <div className="grid gap-6 lg:grid-cols-3">
          <div className="lg:col-span-2">
            <Card>
              <CardHeader>
                <Skeleton className="h-6 w-40" />
              </CardHeader>
              <CardContent>
                <InvoicesSkeleton />
              </CardContent>
            </Card>
          </div>
          <div>
            <Card>
              <CardHeader>
                <Skeleton className="h-6 w-32" />
              </CardHeader>
              <CardContent>
                <PaymentMethodsSkeleton />
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    );
  }
  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col justify-between gap-4 sm:flex-row">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Facturación</h1>
          <p className="text-muted-foreground">Historial de pagos y métodos de pago</p>
        </div>
        <Button variant="outline">
          <Download className="mr-2 h-4 w-4" />
          Exportar Historial
        </Button>
      </div>

      {/* Summary Cards */}
      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-primary/10 p-2">
                <DollarSign className="h-5 w-5 text-primary" />
              </div>
              <div>
                <p className="text-sm text-muted-foreground">Total Pagado (2024)</p>
                <p className="text-2xl font-bold">{formatCurrency(totalPaidThisYear)}</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-blue-100 p-2">
                <Calendar className="h-5 w-5 text-blue-600" />
              </div>
              <div>
                <p className="text-sm text-muted-foreground">Próximo Pago</p>
                <p className="text-2xl font-bold">{nextPaymentDate}</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-amber-100 p-2">
                <CreditCard className="h-5 w-5 text-amber-600" />
              </div>
              <div>
                <p className="text-sm text-muted-foreground">Monto Mensual</p>
                <p className="text-2xl font-bold">{formatCurrency(monthlyAmount)}</p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Invoices */}
        <div className="lg:col-span-2">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle>Historial de Facturas</CardTitle>
              <Button variant="ghost" size="sm">
                Ver todo <ChevronRight className="ml-1 h-4 w-4" />
              </Button>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                {invoices && invoices.length > 0 ? (
                  invoices.map(invoice => (
                    <div
                      key={invoice.id}
                      className="flex items-center justify-between rounded-lg border p-4 hover:bg-muted/50"
                    >
                      <div className="flex items-center gap-4">
                        <div className="rounded-lg bg-muted p-2">
                          <FileText className="h-5 w-5 text-muted-foreground" />
                        </div>
                        <div>
                          <p className="font-medium">{invoice.description}</p>
                          <p className="text-sm text-muted-foreground">
                            {invoice.invoiceNumber} • {formatBillingDate(invoice.createdAt)}
                          </p>
                        </div>
                      </div>
                      <div className="flex items-center gap-4">
                        <div className="text-right">
                          <p className="font-bold">{formatCurrency(invoice.amount)}</p>
                          <InvoiceStatusBadge status={invoice.status} />
                        </div>
                        <Button
                          variant="ghost"
                          size="icon"
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
                  ))
                ) : (
                  <div className="py-8 text-center text-muted-foreground">
                    <FileText className="mx-auto mb-2 h-12 w-12 text-gray-300" />
                    <p>No hay facturas disponibles</p>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Payment Methods */}
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Métodos de Pago</CardTitle>
              <CardDescription>Administra tus tarjetas</CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              {paymentMethods && paymentMethods.length > 0 ? (
                paymentMethods.map(method => (
                  <div
                    key={method.id}
                    className={`flex items-center justify-between rounded-lg border p-3 ${
                      method.isDefault ? 'border-primary bg-primary/10' : ''
                    }`}
                  >
                    <div className="flex items-center gap-3">
                      <div className="rounded bg-card p-2 shadow-sm">
                        <CreditCard className="h-5 w-5" />
                      </div>
                      <div>
                        <p className="font-medium">
                          {getPaymentMethodBrand(method.brand)} •••• {method.last4}
                        </p>
                        <p className="text-xs text-muted-foreground">
                          Exp: {method.expiryMonth}/{method.expiryYear}
                        </p>
                      </div>
                    </div>
                    {method.isDefault && (
                      <Badge variant="outline" className="border-primary text-primary">
                        Principal
                      </Badge>
                    )}
                  </div>
                ))
              ) : (
                <div className="py-4 text-center text-muted-foreground">
                  <CreditCard className="mx-auto mb-2 h-8 w-8 text-gray-300" />
                  <p className="text-sm">No hay métodos de pago</p>
                </div>
              )}
              <Button variant="outline" className="mt-3 w-full">
                <CreditCard className="mr-2 h-4 w-4" />
                Agregar Tarjeta
              </Button>
            </CardContent>
          </Card>

          {/* Billing Info */}
          <Card>
            <CardHeader>
              <CardTitle>Información de Facturación</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2 text-sm">
              <div className="flex justify-between">
                <span className="text-muted-foreground">Razón Social</span>
                <span className="font-medium">
                  {dealer?.legalName || dealer?.businessName || 'N/A'}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">RNC</span>
                <span className="font-medium">{dealer?.rnc || 'N/A'}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Email Facturas</span>
                <span className="font-medium">{dealer?.email || 'N/A'}</span>
              </div>
              <Button variant="link" className="h-auto p-0 text-primary">
                Editar información
              </Button>
            </CardContent>
          </Card>

          {/* Next Payment Alert */}
          {subscription && subscription.status === 'Active' && defaultMethod && (
            <Card className="border-blue-200 bg-blue-50">
              <CardContent className="p-4">
                <div className="flex items-start gap-3">
                  <AlertCircle className="mt-0.5 h-5 w-5 flex-shrink-0 text-blue-600" />
                  <div>
                    <p className="font-medium text-blue-800">Próximo cobro</p>
                    <p className="text-sm text-blue-600">
                      Se cargará {formatCurrency(subscription.monthlyPrice)} a tu tarjeta{' '}
                      {getPaymentMethodBrand(defaultMethod.brand)} ****{defaultMethod.last4} el{' '}
                      {formatBillingDate(subscription.currentPeriodEnd)}.
                    </p>
                  </div>
                </div>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}
