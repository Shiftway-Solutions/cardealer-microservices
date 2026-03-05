---
title: "52 - Billing Dashboard"
priority: P1
estimated_time: "2 horas"
dependencies: []
apis: ["BillingService"]
status: complete
last_updated: "2026-01-30"
---

# 💰 52 - Billing Dashboard

**Objetivo:** Dashboard completo de facturación con suscripciones, pagos, facturas y métodos de pago.

**Prioridad:** P0 (CRÍTICO - Gestión de pagos)  
**Complejidad:** 🟡 Alta  
**Dependencias:** BillingService

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura](#-arquitectura)
2. [Páginas](#-páginas)
3. [Hooks](#-hooks)
4. [Tipos TypeScript](#-tipos-typescript)
5. [Flujo de Usuario](#-flujo-de-usuario)

---

## 🏗️ ARQUITECTURA

```
pages/billing/
├── BillingDashboardPage.tsx  # Dashboard principal
├── PaymentMethodsPage.tsx    # Métodos de pago guardados
├── PaymentsPage.tsx          # Historial de pagos
├── PlansPage.tsx             # Gestión de planes/suscripción
└── InvoicesPage.tsx          # Facturas/Comprobantes fiscales
```

### Estructura de Navegación

```
/billing                → Dashboard general (resumen)
/billing/payments       → Historial de transacciones
/billing/methods        → Tarjetas guardadas
/billing/plans          → Plan actual y cambiar plan
/billing/invoices       → Facturas y NCF
```

---

## 📄 PÁGINAS

### 1. BillingDashboardPage.tsx

**Ruta:** `/billing` o `/dealer/billing`

```typescript
// src/pages/billing/BillingDashboardPage.tsx
"use client";

import { Link, useLocation } from "react-router-dom";
import {
  CreditCard,
  Receipt,
  FileText,
  Settings,
  TrendingUp,
  AlertCircle,
  CheckCircle,
  Clock,
  ArrowUpRight,
  Loader2,
} from "lucide-react";
import MainLayout from "@/layouts/MainLayout";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { Progress } from "@/components/ui/Progress";
import { useBillingDashboard } from "@/hooks/useBilling";
import { useAuth } from "@/hooks/useAuth";
import { format } from "date-fns";
import { es } from "date-fns/locale";

export default function BillingDashboardPage() {
  const location = useLocation();
  const { user } = useAuth();

  // Detectar si es dealer
  const isDealer = location.pathname.startsWith("/dealer");
  const routePrefix = isDealer ? "/dealer" : "";

  // Obtener ID del dealer si aplica
  const dealerId = user?.dealerId;

  const { data, isLoading, error } = useBillingDashboard(dealerId);

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat("es-DO", {
      style: "currency",
      currency: "DOP",
      minimumFractionDigits: 0,
    }).format(amount);
  };

  if (isLoading) {
    return (
      <MainLayout>
        <div className="min-h-screen bg-gray-50 flex items-center justify-center">
          <Loader2 className="w-8 h-8 animate-spin text-blue-600" />
        </div>
      </MainLayout>
    );
  }

  if (error || !data) {
    return (
      <MainLayout>
        <div className="min-h-screen bg-gray-50 flex items-center justify-center">
          <div className="text-center">
            <AlertCircle className="w-12 h-12 text-red-500 mx-auto mb-4" />
            <p className="text-gray-600">Error al cargar datos de facturación</p>
          </div>
        </div>
      </MainLayout>
    );
  }

  const { subscription, usage, recentPayments, upcomingInvoice, paymentMethods } = data;

  return (
    <MainLayout>
      <div className="min-h-screen bg-gray-50 py-8">
        <div className="max-w-6xl mx-auto px-4">
          {/* Header */}
          <div className="mb-8">
            <h1 className="text-3xl font-bold text-gray-900 mb-2">
              Facturación
            </h1>
            <p className="text-gray-600">
              Gestiona tu suscripción, pagos y facturas
            </p>
          </div>

          {/* Quick Links */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-8">
            <Link to={`${routePrefix}/billing/payments`}>
              <Card className="hover:shadow-md transition-shadow cursor-pointer">
                <CardContent className="p-4 flex items-center gap-3">
                  <div className="p-2 bg-blue-100 rounded-lg">
                    <Receipt className="w-5 h-5 text-blue-600" />
                  </div>
                  <div>
                    <p className="font-medium">Pagos</p>
                    <p className="text-sm text-gray-500">Ver historial</p>
                  </div>
                </CardContent>
              </Card>
            </Link>

            <Link to={`${routePrefix}/billing/methods`}>
              <Card className="hover:shadow-md transition-shadow cursor-pointer">
                <CardContent className="p-4 flex items-center gap-3">
                  <div className="p-2 bg-green-100 rounded-lg">
                    <CreditCard className="w-5 h-5 text-green-600" />
                  </div>
                  <div>
                    <p className="font-medium">Métodos</p>
                    <p className="text-sm text-gray-500">
                      {paymentMethods.length} guardados
                    </p>
                  </div>
                </CardContent>
              </Card>
            </Link>

            <Link to={`${routePrefix}/billing/invoices`}>
              <Card className="hover:shadow-md transition-shadow cursor-pointer">
                <CardContent className="p-4 flex items-center gap-3">
                  <div className="p-2 bg-purple-100 rounded-lg">
                    <FileText className="w-5 h-5 text-purple-600" />
                  </div>
                  <div>
                    <p className="font-medium">Facturas</p>
                    <p className="text-sm text-gray-500">Descargar NCF</p>
                  </div>
                </CardContent>
              </Card>
            </Link>

            <Link to={`${routePrefix}/billing/plans`}>
              <Card className="hover:shadow-md transition-shadow cursor-pointer">
                <CardContent className="p-4 flex items-center gap-3">
                  <div className="p-2 bg-amber-100 rounded-lg">
                    <Settings className="w-5 h-5 text-amber-600" />
                  </div>
                  <div>
                    <p className="font-medium">Plan</p>
                    <p className="text-sm text-gray-500">Cambiar plan</p>
                  </div>
                </CardContent>
              </Card>
            </Link>
          </div>

          <div className="grid lg:grid-cols-3 gap-8">
            {/* Main Content */}
            <div className="lg:col-span-2 space-y-6">
              {/* Current Subscription */}
              <Card>
                <CardHeader>
                  <div className="flex items-center justify-between">
                    <CardTitle>Plan Actual</CardTitle>
                    <Badge
                      variant={subscription.status === "active" ? "success" : "warning"}
                    >
                      {subscription.status === "active" ? (
                        <>
                          <CheckCircle className="w-3 h-3 mr-1" />
                          Activo
                        </>
                      ) : (
                        <>
                          <Clock className="w-3 h-3 mr-1" />
                          {subscription.status}
                        </>
                      )}
                    </Badge>
                  </div>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="flex items-center justify-between">
                    <div>
                      <h3 className="text-2xl font-bold">{subscription.planName}</h3>
                      <p className="text-gray-500">
                        {formatCurrency(subscription.monthlyPrice)}/mes
                      </p>
                    </div>
                    <Button asChild variant="outline">
                      <Link to={`${routePrefix}/billing/plans`}>
                        Cambiar Plan
                      </Link>
                    </Button>
                  </div>

                  {/* Early Bird Badge */}
                  {subscription.isEarlyBird && (
                    <div className="bg-gradient-to-r from-yellow-50 to-orange-50 border border-yellow-200 rounded-lg p-3">
                      <div className="flex items-center gap-2">
                        <Badge className="bg-yellow-500">🏆 Miembro Fundador</Badge>
                        <span className="text-sm text-yellow-700">
                          20% descuento de por vida aplicado
                        </span>
                      </div>
                    </div>
                  )}

                  {/* Next Billing */}
                  <div className="border-t pt-4">
                    <div className="flex justify-between text-sm">
                      <span className="text-gray-600">Próximo cobro:</span>
                      <span className="font-medium">
                        {format(new Date(subscription.nextBillingDate), "d 'de' MMMM, yyyy", {
                          locale: es,
                        })}
                      </span>
                    </div>
                    <div className="flex justify-between text-sm mt-2">
                      <span className="text-gray-600">Monto:</span>
                      <span className="font-medium">
                        {formatCurrency(subscription.nextBillingAmount)}
                      </span>
                    </div>
                  </div>
                </CardContent>
              </Card>

              {/* Usage */}
              {usage && (
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      <TrendingUp className="w-5 h-5 text-blue-600" />
                      Uso del Plan
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    {/* Vehículos Activos */}
                    <div>
                      <div className="flex justify-between mb-2">
                        <span className="text-sm text-gray-600">
                          Vehículos Activos
                        </span>
                        <span className="text-sm font-medium">
                          {usage.activeListings} / {usage.maxListings === -1 ? "∞" : usage.maxListings}
                        </span>
                      </div>
                      <Progress
                        value={
                          usage.maxListings === -1
                            ? 0
                            : (usage.activeListings / usage.maxListings) * 100
                        }
                        className="h-2"
                      />
                      {usage.maxListings !== -1 && usage.activeListings >= usage.maxListings * 0.9 && (
                        <p className="text-xs text-amber-600 mt-1">
                          ⚠️ Casi alcanzas el límite. Considera actualizar tu plan.
                        </p>
                      )}
                    </div>

                    {/* Vistas Este Mes */}
                    <div className="grid grid-cols-2 gap-4 pt-4 border-t">
                      <div className="text-center p-4 bg-gray-50 rounded-lg">
                        <p className="text-2xl font-bold text-blue-600">
                          {usage.viewsThisMonth.toLocaleString()}
                        </p>
                        <p className="text-sm text-gray-500">Vistas este mes</p>
                      </div>
                      <div className="text-center p-4 bg-gray-50 rounded-lg">
                        <p className="text-2xl font-bold text-green-600">
                          {usage.leadsThisMonth}
                        </p>
                        <p className="text-sm text-gray-500">Leads generados</p>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              )}

              {/* Recent Payments */}
              <Card>
                <CardHeader>
                  <div className="flex items-center justify-between">
                    <CardTitle>Pagos Recientes</CardTitle>
                    <Link
                      to={`${routePrefix}/billing/payments`}
                      className="text-sm text-blue-600 hover:text-blue-700 flex items-center"
                    >
                      Ver todos
                      <ArrowUpRight className="w-4 h-4 ml-1" />
                    </Link>
                  </div>
                </CardHeader>
                <CardContent>
                  {recentPayments.length === 0 ? (
                    <p className="text-gray-500 text-center py-4">
                      No hay pagos recientes
                    </p>
                  ) : (
                    <div className="space-y-3">
                      {recentPayments.slice(0, 5).map((payment) => (
                        <div
                          key={payment.id}
                          className="flex items-center justify-between py-2 border-b last:border-0"
                        >
                          <div>
                            <p className="font-medium">{payment.description}</p>
                            <p className="text-sm text-gray-500">
                              {format(new Date(payment.createdAt), "d MMM yyyy", {
                                locale: es,
                              })}
                            </p>
                          </div>
                          <div className="text-right">
                            <p className="font-medium">
                              {formatCurrency(payment.amount)}
                            </p>
                            <Badge
                              variant={
                                payment.status === "completed"
                                  ? "success"
                                  : payment.status === "pending"
                                  ? "warning"
                                  : "destructive"
                              }
                              className="text-xs"
                            >
                              {payment.status === "completed" && "Pagado"}
                              {payment.status === "pending" && "Pendiente"}
                              {payment.status === "failed" && "Fallido"}
                            </Badge>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </CardContent>
              </Card>
            </div>

            {/* Sidebar */}
            <div className="space-y-6">
              {/* Upcoming Invoice */}
              {upcomingInvoice && (
                <Card className="border-blue-200 bg-blue-50">
                  <CardHeader>
                    <CardTitle className="text-lg">Próxima Factura</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-3">
                      <div className="flex justify-between">
                        <span className="text-gray-600">Plan {subscription.planName}</span>
                        <span>{formatCurrency(upcomingInvoice.subtotal)}</span>
                      </div>
                      {upcomingInvoice.discount > 0 && (
                        <div className="flex justify-between text-green-600">
                          <span>Descuento Early Bird</span>
                          <span>-{formatCurrency(upcomingInvoice.discount)}</span>
                        </div>
                      )}
                      <div className="flex justify-between">
                        <span className="text-gray-600">ITBIS (18%)</span>
                        <span>{formatCurrency(upcomingInvoice.tax)}</span>
                      </div>
                      <div className="border-t pt-3 flex justify-between font-bold">
                        <span>Total</span>
                        <span>{formatCurrency(upcomingInvoice.total)}</span>
                      </div>
                      <p className="text-sm text-gray-500 text-center">
                        Se cobrará el{" "}
                        {format(new Date(upcomingInvoice.dueDate), "d 'de' MMMM", {
                          locale: es,
                        })}
                      </p>
                    </div>
                  </CardContent>
                </Card>
              )}

              {/* Payment Method */}
              <Card>
                <CardHeader>
                  <CardTitle className="text-lg">Método de Pago</CardTitle>
                </CardHeader>
                <CardContent>
                  {paymentMethods.length === 0 ? (
                    <div className="text-center py-4">
                      <CreditCard className="w-12 h-12 text-gray-300 mx-auto mb-3" />
                      <p className="text-gray-500 mb-4">
                        No tienes métodos de pago guardados
                      </p>
                      <Button asChild>
                        <Link to={`${routePrefix}/billing/methods`}>
                          Agregar Método
                        </Link>
                      </Button>
                    </div>
                  ) : (
                    <div className="space-y-3">
                      {paymentMethods
                        .filter((pm) => pm.isDefault)
                        .map((method) => (
                          <div
                            key={method.id}
                            className="flex items-center gap-3 p-3 bg-gray-50 rounded-lg"
                          >
                            <div className="w-12 h-8 bg-white rounded flex items-center justify-center border">
                              <img
                                src={`/img/cards/${method.brand.toLowerCase()}.svg`}
                                alt={method.brand}
                                className="h-5"
                              />
                            </div>
                            <div className="flex-1">
                              <p className="font-medium">
                                •••• •••• •••• {method.last4}
                              </p>
                              <p className="text-xs text-gray-500">
                                Vence {method.expiryMonth}/{method.expiryYear}
                              </p>
                            </div>
                            <Badge variant="outline" className="text-xs">
                              Principal
                            </Badge>
                          </div>
                        ))}
                      <Button asChild variant="ghost" className="w-full">
                        <Link to={`${routePrefix}/billing/methods`}>
                          Gestionar métodos
                        </Link>
                      </Button>
                    </div>
                  )}
                </CardContent>
              </Card>

              {/* Help */}
              <Card>
                <CardContent className="p-4">
                  <div className="flex items-center gap-3">
                    <div className="p-2 bg-gray-100 rounded-lg">
                      <AlertCircle className="w-5 h-5 text-gray-600" />
                    </div>
                    <div className="flex-1">
                      <p className="font-medium text-sm">¿Necesitas ayuda?</p>
                      <p className="text-xs text-gray-500">
                        Contáctanos para cualquier pregunta sobre facturación
                      </p>
                    </div>
                  </div>
                  <Button asChild variant="outline" size="sm" className="w-full mt-3">
                    <Link to="/help/billing">Centro de Ayuda</Link>
                  </Button>
                </CardContent>
              </Card>
            </div>
          </div>
        </div>
      </div>
    </MainLayout>
  );
}
```

---

### 2. PaymentMethodsPage.tsx

**Ruta:** `/billing/methods`

```typescript
// src/pages/billing/PaymentMethodsPage.tsx
"use client";

import { useState } from "react";
import { Link } from "react-router-dom";
import {
  CreditCard,
  Plus,
  Trash2,
  Star,
  ArrowLeft,
  Loader2,
  AlertTriangle,
} from "lucide-react";
import MainLayout from "@/layouts/MainLayout";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/Dialog";
import {
  AlertDialog,
  AlertDialogContent,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogCancel,
  AlertDialogAction,
} from "@/components/ui/AlertDialog";
import { usePaymentMethods } from "@/hooks/useBilling";
import { toast } from "sonner";

interface PaymentMethod {
  id: string;
  brand: "visa" | "mastercard" | "amex";
  last4: string;
  expiryMonth: number;
  expiryYear: number;
  isDefault: boolean;
  holderName: string;
}

export default function PaymentMethodsPage() {
  const [showAddDialog, setShowAddDialog] = useState(false);
  const [deleteMethodId, setDeleteMethodId] = useState<string | null>(null);

  const {
    paymentMethods,
    isLoading,
    setDefaultMethod,
    deleteMethod,
    isDeleting,
  } = usePaymentMethods();

  const handleSetDefault = async (methodId: string) => {
    try {
      await setDefaultMethod(methodId);
      toast.success("Método de pago actualizado");
    } catch {
      toast.error("Error al actualizar método de pago");
    }
  };

  const handleDelete = async () => {
    if (!deleteMethodId) return;
    try {
      await deleteMethod(deleteMethodId);
      toast.success("Método de pago eliminado");
      setDeleteMethodId(null);
    } catch {
      toast.error("Error al eliminar método de pago");
    }
  };

  const getBrandLogo = (brand: string) => {
    return `/img/cards/${brand.toLowerCase()}.svg`;
  };

  return (
    <MainLayout>
      <div className="min-h-screen bg-gray-50 py-8">
        <div className="max-w-2xl mx-auto px-4">
          {/* Header */}
          <div className="mb-8">
            <Link
              to="/billing"
              className="inline-flex items-center text-gray-600 hover:text-gray-900 mb-4"
            >
              <ArrowLeft className="w-4 h-4 mr-2" />
              Volver a Facturación
            </Link>
            <div className="flex items-center justify-between">
              <div>
                <h1 className="text-3xl font-bold text-gray-900 mb-2">
                  Métodos de Pago
                </h1>
                <p className="text-gray-600">
                  Gestiona tus tarjetas y métodos de pago guardados
                </p>
              </div>
              <Button onClick={() => setShowAddDialog(true)}>
                <Plus className="w-4 h-4 mr-2" />
                Agregar
              </Button>
            </div>
          </div>

          {/* Loading */}
          {isLoading && (
            <div className="flex justify-center py-12">
              <Loader2 className="w-8 h-8 animate-spin text-blue-600" />
            </div>
          )}

          {/* Empty State */}
          {!isLoading && paymentMethods.length === 0 && (
            <Card className="py-12 text-center">
              <CreditCard className="w-16 h-16 text-gray-300 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">
                No tienes métodos de pago
              </h3>
              <p className="text-gray-600 mb-6">
                Agrega una tarjeta para realizar pagos automáticos
              </p>
              <Button onClick={() => setShowAddDialog(true)}>
                <Plus className="w-4 h-4 mr-2" />
                Agregar Método de Pago
              </Button>
            </Card>
          )}

          {/* Payment Methods List */}
          <div className="space-y-4">
            {paymentMethods.map((method) => (
              <Card
                key={method.id}
                className={method.isDefault ? "border-blue-300 bg-blue-50" : ""}
              >
                <CardContent className="p-4">
                  <div className="flex items-center gap-4">
                    {/* Card Brand */}
                    <div className="w-16 h-10 bg-white rounded-lg border flex items-center justify-center">
                      <img
                        src={getBrandLogo(method.brand)}
                        alt={method.brand}
                        className="h-6"
                      />
                    </div>

                    {/* Card Info */}
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <p className="font-medium text-gray-900">
                          •••• •••• •••• {method.last4}
                        </p>
                        {method.isDefault && (
                          <Badge variant="outline" className="text-xs">
                            <Star className="w-3 h-3 mr-1 fill-current" />
                            Principal
                          </Badge>
                        )}
                      </div>
                      <p className="text-sm text-gray-500">
                        {method.holderName} • Vence {method.expiryMonth}/
                        {method.expiryYear}
                      </p>
                    </div>

                    {/* Actions */}
                    <div className="flex gap-2">
                      {!method.isDefault && (
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => handleSetDefault(method.id)}
                        >
                          Hacer Principal
                        </Button>
                      )}
                      <Button
                        variant="ghost"
                        size="icon"
                        className="text-red-600 hover:text-red-700 hover:bg-red-50"
                        onClick={() => setDeleteMethodId(method.id)}
                      >
                        <Trash2 className="w-4 h-4" />
                      </Button>
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>

          {/* Accepted Cards Info */}
          <div className="mt-8 p-4 bg-gray-100 rounded-lg">
            <p className="text-sm text-gray-600 text-center mb-3">
              Aceptamos las siguientes tarjetas:
            </p>
            <div className="flex justify-center gap-4">
              <img src="/img/cards/visa.svg" alt="Visa" className="h-8" />
              <img src="/img/cards/mastercard.svg" alt="Mastercard" className="h-8" />
              <img src="/img/cards/amex.svg" alt="American Express" className="h-8" />
            </div>
            <p className="text-xs text-gray-500 text-center mt-3">
              Procesado de forma segura por AZUL, CardNET, PixelPay, Fygaro y PayPal
            </p>
          </div>
        </div>
      </div>

      {/* Delete Confirmation Dialog */}
      <AlertDialog
        open={!!deleteMethodId}
        onOpenChange={() => setDeleteMethodId(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle className="flex items-center gap-2">
              <AlertTriangle className="w-5 h-5 text-red-500" />
              ¿Eliminar método de pago?
            </AlertDialogTitle>
            <AlertDialogDescription>
              Esta acción no se puede deshacer. Si este es tu único método de
              pago, deberás agregar otro para continuar con tu suscripción.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancelar</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDelete}
              className="bg-red-600 hover:bg-red-700"
              disabled={isDeleting}
            >
              {isDeleting ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin mr-2" />
                  Eliminando...
                </>
              ) : (
                "Eliminar"
              )}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Add Payment Method Dialog */}
      <Dialog open={showAddDialog} onOpenChange={setShowAddDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Agregar Método de Pago</DialogTitle>
            <DialogDescription>
              Serás redirigido a una página segura para agregar tu tarjeta.
            </DialogDescription>
          </DialogHeader>
          <div className="py-4 text-center">
            <p className="text-gray-600 mb-4">
              Selecciona tu procesador de pago preferido:
            </p>
            <div className="grid grid-cols-2 gap-4">
              <Button variant="outline" className="h-16 flex-col">
                <img src="/img/azul.svg" alt="AZUL" className="h-6 mb-1" />
                <span className="text-xs">AZUL</span>
              </Button>
              <Button variant="outline" className="h-16 flex-col">
                <img src="/img/cardnet.svg" alt="CardNET" className="h-6 mb-1" />
                <span className="text-xs">CardNET</span>
              </Button>
              <Button variant="outline" className="h-16 flex-col">
                <img src="/img/pixelpay.svg" alt="PixelPay" className="h-6 mb-1" />
                <span className="text-xs">PixelPay</span>
              </Button>
              <Button variant="outline" className="h-16 flex-col">
                <img src="/img/paypal.svg" alt="PayPal" className="h-6 mb-1" />
                <span className="text-xs">PayPal</span>
              </Button>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowAddDialog(false)}>
              Cancelar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </MainLayout>
  );
}
```

---

### 3. PlansPage.tsx

**Ruta:** `/billing/plans`

```typescript
// src/pages/billing/PlansPage.tsx
"use client";

import { useState } from "react";
import { Link } from "react-router-dom";
import {
  Check,
  X,
  ArrowLeft,
  Star,
  ArrowUp,
  ArrowDown,
  Loader2,
  AlertCircle,
} from "lucide-react";
import MainLayout from "@/layouts/MainLayout";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/Dialog";
import { useSubscription, usePlans } from "@/hooks/useBilling";
import { toast } from "sonner";

export default function PlansPage() {
  const [selectedPlan, setSelectedPlan] = useState<string | null>(null);
  const [confirmAction, setConfirmAction] = useState<"upgrade" | "downgrade" | null>(
    null
  );

  const { subscription, isLoading: subLoading } = useSubscription();
  const { plans, isLoading: plansLoading, changePlan, isChanging } = usePlans();

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat("es-DO", {
      style: "currency",
      currency: "DOP",
      minimumFractionDigits: 0,
    }).format(amount);
  };

  const handlePlanChange = async () => {
    if (!selectedPlan) return;

    try {
      await changePlan(selectedPlan);
      toast.success("Plan actualizado exitosamente");
      setConfirmAction(null);
      setSelectedPlan(null);
    } catch {
      toast.error("Error al cambiar de plan");
    }
  };

  const getPlanAction = (planId: string) => {
    if (!subscription) return null;

    const currentIndex = plans.findIndex((p) => p.id === subscription.planId);
    const targetIndex = plans.findIndex((p) => p.id === planId);

    if (currentIndex === targetIndex) return "current";
    if (targetIndex > currentIndex) return "upgrade";
    return "downgrade";
  };

  if (subLoading || plansLoading) {
    return (
      <MainLayout>
        <div className="min-h-screen bg-gray-50 flex items-center justify-center">
          <Loader2 className="w-8 h-8 animate-spin text-blue-600" />
        </div>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <div className="min-h-screen bg-gray-50 py-8">
        <div className="max-w-5xl mx-auto px-4">
          {/* Header */}
          <div className="mb-8">
            <Link
              to="/billing"
              className="inline-flex items-center text-gray-600 hover:text-gray-900 mb-4"
            >
              <ArrowLeft className="w-4 h-4 mr-2" />
              Volver a Facturación
            </Link>
            <h1 className="text-3xl font-bold text-gray-900 mb-2">
              Gestionar Plan
            </h1>
            <p className="text-gray-600">
              Tu plan actual:{" "}
              <span className="font-semibold">{subscription?.planName}</span>
            </p>
          </div>

          {/* Plans Grid */}
          <div className="grid md:grid-cols-3 gap-6">
            {plans.map((plan) => {
              const action = getPlanAction(plan.id);
              const isCurrent = action === "current";

              return (
                <Card
                  key={plan.id}
                  className={`relative ${
                    isCurrent
                      ? "border-2 border-blue-500 shadow-lg"
                      : plan.recommended
                      ? "border-2 border-purple-500"
                      : ""
                  }`}
                >
                  {/* Badges */}
                  {isCurrent && (
                    <div className="absolute -top-3 left-1/2 -translate-x-1/2">
                      <Badge className="bg-blue-600">Plan Actual</Badge>
                    </div>
                  )}
                  {plan.recommended && !isCurrent && (
                    <div className="absolute -top-3 left-1/2 -translate-x-1/2">
                      <Badge className="bg-purple-600">
                        <Star className="w-3 h-3 mr-1" />
                        Recomendado
                      </Badge>
                    </div>
                  )}

                  <CardHeader className="text-center pt-8">
                    <CardTitle>{plan.name}</CardTitle>
                    <p className="text-sm text-gray-500">{plan.description}</p>
                  </CardHeader>

                  <CardContent className="space-y-6">
                    {/* Price */}
                    <div className="text-center">
                      <p className="text-4xl font-bold">
                        {formatCurrency(plan.monthlyPrice)}
                      </p>
                      <p className="text-gray-500">/mes</p>
                    </div>

                    {/* Features */}
                    <ul className="space-y-3">
                      {plan.features.map((feature, idx) => (
                        <li key={idx} className="flex items-start gap-2">
                          {feature.included ? (
                            <Check className="w-5 h-5 text-green-500 flex-shrink-0" />
                          ) : (
                            <X className="w-5 h-5 text-gray-300 flex-shrink-0" />
                          )}
                          <span
                            className={
                              feature.included ? "text-gray-700" : "text-gray-400"
                            }
                          >
                            {feature.name}
                          </span>
                        </li>
                      ))}
                    </ul>

                    {/* Action Button */}
                    <div>
                      {isCurrent ? (
                        <Button disabled className="w-full" variant="outline">
                          Plan Actual
                        </Button>
                      ) : action === "upgrade" ? (
                        <Button
                          className="w-full bg-green-600 hover:bg-green-700"
                          onClick={() => {
                            setSelectedPlan(plan.id);
                            setConfirmAction("upgrade");
                          }}
                        >
                          <ArrowUp className="w-4 h-4 mr-2" />
                          Mejorar Plan
                        </Button>
                      ) : (
                        <Button
                          className="w-full"
                          variant="outline"
                          onClick={() => {
                            setSelectedPlan(plan.id);
                            setConfirmAction("downgrade");
                          }}
                        >
                          <ArrowDown className="w-4 h-4 mr-2" />
                          Cambiar a este Plan
                        </Button>
                      )}
                    </div>
                  </CardContent>
                </Card>
              );
            })}
          </div>

          {/* Cancel Subscription */}
          <div className="mt-12 text-center">
            <p className="text-gray-500 mb-4">
              ¿Necesitas cancelar tu suscripción?
            </p>
            <Button variant="ghost" className="text-red-600 hover:text-red-700">
              Cancelar Suscripción
            </Button>
          </div>
        </div>
      </div>

      {/* Confirmation Dialog */}
      <Dialog
        open={!!confirmAction}
        onOpenChange={() => {
          setConfirmAction(null);
          setSelectedPlan(null);
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {confirmAction === "upgrade" ? "Mejorar Plan" : "Cambiar Plan"}
            </DialogTitle>
            <DialogDescription>
              {confirmAction === "upgrade" ? (
                <>
                  Al mejorar tu plan, tendrás acceso inmediato a las nuevas
                  funcionalidades. Se te cobrará la diferencia prorrateada.
                </>
              ) : (
                <>
                  Al cambiar a un plan menor, perderás acceso a algunas
                  funcionalidades. El cambio se aplicará al inicio del próximo
                  ciclo de facturación.
                </>
              )}
            </DialogDescription>
          </DialogHeader>

          {confirmAction === "downgrade" && (
            <div className="bg-amber-50 border border-amber-200 rounded-lg p-4">
              <div className="flex gap-3">
                <AlertCircle className="w-5 h-5 text-amber-500 flex-shrink-0" />
                <div className="text-sm text-amber-800">
                  <p className="font-medium">Advertencia</p>
                  <p>
                    Si tienes más vehículos activos que el límite del nuevo plan,
                    algunos serán desactivados automáticamente.
                  </p>
                </div>
              </div>
            </div>
          )}

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                setConfirmAction(null);
                setSelectedPlan(null);
              }}
            >
              Cancelar
            </Button>
            <Button
              onClick={handlePlanChange}
              disabled={isChanging}
              className={
                confirmAction === "upgrade"
                  ? "bg-green-600 hover:bg-green-700"
                  : ""
              }
            >
              {isChanging ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin mr-2" />
                  Procesando...
                </>
              ) : confirmAction === "upgrade" ? (
                "Confirmar Mejora"
              ) : (
                "Confirmar Cambio"
              )}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </MainLayout>
  );
}
```

---

### 4. InvoicesPage.tsx

**Ruta:** `/billing/invoices`

```typescript
// src/pages/billing/InvoicesPage.tsx
"use client";

import { useState } from "react";
import { Link } from "react-router-dom";
import {
  FileText,
  Download,
  ArrowLeft,
  Search,
  Filter,
  Loader2,
  Calendar,
  CheckCircle,
  Clock,
  XCircle,
} from "lucide-react";
import MainLayout from "@/layouts/MainLayout";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { Input } from "@/components/ui/Input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/Select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/Table";
import { useInvoices } from "@/hooks/useBilling";
import { format } from "date-fns";
import { es } from "date-fns/locale";

interface Invoice {
  id: string;
  invoiceNumber: string;
  ncf?: string; // Número de Comprobante Fiscal
  description: string;
  amount: number;
  tax: number;
  total: number;
  status: "paid" | "pending" | "overdue" | "cancelled";
  issuedAt: string;
  dueDate: string;
  paidAt?: string;
  downloadUrl: string;
}

export default function InvoicesPage() {
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState<string>("all");
  const [yearFilter, setYearFilter] = useState<string>(
    new Date().getFullYear().toString()
  );

  const { invoices, isLoading, downloadInvoice, isDownloading } = useInvoices({
    search: searchTerm,
    status: statusFilter !== "all" ? statusFilter : undefined,
    year: yearFilter !== "all" ? parseInt(yearFilter) : undefined,
  });

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat("es-DO", {
      style: "currency",
      currency: "DOP",
      minimumFractionDigits: 0,
    }).format(amount);
  };

  const getStatusBadge = (status: Invoice["status"]) => {
    const styles = {
      paid: { variant: "success", icon: CheckCircle, label: "Pagada" },
      pending: { variant: "warning", icon: Clock, label: "Pendiente" },
      overdue: { variant: "destructive", icon: XCircle, label: "Vencida" },
      cancelled: { variant: "secondary", icon: XCircle, label: "Cancelada" },
    };

    const style = styles[status];
    const Icon = style.icon;

    return (
      <Badge variant={style.variant as any} className="flex items-center gap-1">
        <Icon className="w-3 h-3" />
        {style.label}
      </Badge>
    );
  };

  const years = Array.from({ length: 5 }, (_, i) =>
    (new Date().getFullYear() - i).toString()
  );

  return (
    <MainLayout>
      <div className="min-h-screen bg-gray-50 py-8">
        <div className="max-w-5xl mx-auto px-4">
          {/* Header */}
          <div className="mb-8">
            <Link
              to="/billing"
              className="inline-flex items-center text-gray-600 hover:text-gray-900 mb-4"
            >
              <ArrowLeft className="w-4 h-4 mr-2" />
              Volver a Facturación
            </Link>
            <h1 className="text-3xl font-bold text-gray-900 mb-2">
              Facturas
            </h1>
            <p className="text-gray-600">
              Historial de facturas y comprobantes fiscales (NCF)
            </p>
          </div>

          {/* Filters */}
          <Card className="mb-6">
            <CardContent className="p-4">
              <div className="flex flex-col sm:flex-row gap-4">
                <div className="relative flex-1">
                  <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
                  <Input
                    type="text"
                    placeholder="Buscar por número de factura o NCF..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    className="pl-10"
                  />
                </div>
                <Select value={statusFilter} onValueChange={setStatusFilter}>
                  <SelectTrigger className="w-full sm:w-40">
                    <SelectValue placeholder="Estado" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">Todos</SelectItem>
                    <SelectItem value="paid">Pagadas</SelectItem>
                    <SelectItem value="pending">Pendientes</SelectItem>
                    <SelectItem value="overdue">Vencidas</SelectItem>
                  </SelectContent>
                </Select>
                <Select value={yearFilter} onValueChange={setYearFilter}>
                  <SelectTrigger className="w-full sm:w-32">
                    <Calendar className="w-4 h-4 mr-2" />
                    <SelectValue placeholder="Año" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">Todos</SelectItem>
                    {years.map((year) => (
                      <SelectItem key={year} value={year}>
                        {year}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </CardContent>
          </Card>

          {/* Loading */}
          {isLoading && (
            <div className="flex justify-center py-12">
              <Loader2 className="w-8 h-8 animate-spin text-blue-600" />
            </div>
          )}

          {/* Empty State */}
          {!isLoading && invoices.length === 0 && (
            <Card className="py-12 text-center">
              <FileText className="w-16 h-16 text-gray-300 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">
                No hay facturas
              </h3>
              <p className="text-gray-600">
                Las facturas aparecerán aquí después de tu primer pago.
              </p>
            </Card>
          )}

          {/* Invoices Table */}
          {!isLoading && invoices.length > 0 && (
            <Card>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Factura</TableHead>
                    <TableHead>NCF</TableHead>
                    <TableHead>Fecha</TableHead>
                    <TableHead>Total</TableHead>
                    <TableHead>Estado</TableHead>
                    <TableHead className="text-right">Acciones</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {invoices.map((invoice) => (
                    <TableRow key={invoice.id}>
                      <TableCell>
                        <div>
                          <p className="font-medium">{invoice.invoiceNumber}</p>
                          <p className="text-sm text-gray-500">
                            {invoice.description}
                          </p>
                        </div>
                      </TableCell>
                      <TableCell>
                        <code className="text-xs bg-gray-100 px-2 py-1 rounded">
                          {invoice.ncf || "N/A"}
                        </code>
                      </TableCell>
                      <TableCell>
                        <div>
                          <p className="text-sm">
                            {format(new Date(invoice.issuedAt), "d MMM yyyy", {
                              locale: es,
                            })}
                          </p>
                          <p className="text-xs text-gray-500">
                            Vence:{" "}
                            {format(new Date(invoice.dueDate), "d MMM", {
                              locale: es,
                            })}
                          </p>
                        </div>
                      </TableCell>
                      <TableCell>
                        <div>
                          <p className="font-medium">
                            {formatCurrency(invoice.total)}
                          </p>
                          <p className="text-xs text-gray-500">
                            ITBIS: {formatCurrency(invoice.tax)}
                          </p>
                        </div>
                      </TableCell>
                      <TableCell>{getStatusBadge(invoice.status)}</TableCell>
                      <TableCell className="text-right">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => downloadInvoice(invoice.id)}
                          disabled={isDownloading}
                        >
                          <Download className="w-4 h-4 mr-1" />
                          PDF
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </Card>
          )}

          {/* Info Box */}
          <div className="mt-8 bg-blue-50 border border-blue-200 rounded-lg p-4">
            <h4 className="font-medium text-blue-900 mb-2">
              📋 Información sobre Comprobantes Fiscales (NCF)
            </h4>
            <p className="text-sm text-blue-700">
              Todas las facturas incluyen el Número de Comprobante Fiscal (NCF)
              válido para fines tributarios en República Dominicana. Si
              necesitas una factura con datos fiscales diferentes, contacta a
              soporte.
            </p>
          </div>
        </div>
      </div>
    </MainLayout>
  );
}
```

---

## 🪝 HOOKS

### useBilling.ts

```typescript
// src/hooks/useBilling.ts
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { billingService } from "@/services/billingService";

export function useBillingDashboard(dealerId?: string) {
  return useQuery({
    queryKey: ["billing-dashboard", dealerId],
    queryFn: () => billingService.getDashboard(dealerId),
    staleTime: 1000 * 60 * 5, // 5 minutos
  });
}

export function useSubscription() {
  const { data, isLoading, error } = useQuery({
    queryKey: ["subscription"],
    queryFn: () => billingService.getSubscription(),
  });

  return { subscription: data, isLoading, error };
}

export function usePlans() {
  const queryClient = useQueryClient();

  const { data: plans = [], isLoading } = useQuery({
    queryKey: ["plans"],
    queryFn: () => billingService.getPlans(),
  });

  const changeMutation = useMutation({
    mutationFn: (planId: string) => billingService.changePlan(planId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["subscription"] });
      queryClient.invalidateQueries({ queryKey: ["billing-dashboard"] });
    },
  });

  return {
    plans,
    isLoading,
    changePlan: changeMutation.mutateAsync,
    isChanging: changeMutation.isPending,
  };
}

export function usePaymentMethods() {
  const queryClient = useQueryClient();

  const { data: paymentMethods = [], isLoading } = useQuery({
    queryKey: ["payment-methods"],
    queryFn: () => billingService.getPaymentMethods(),
  });

  const setDefaultMutation = useMutation({
    mutationFn: (methodId: string) =>
      billingService.setDefaultPaymentMethod(methodId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["payment-methods"] });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (methodId: string) =>
      billingService.deletePaymentMethod(methodId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["payment-methods"] });
    },
  });

  return {
    paymentMethods,
    isLoading,
    setDefaultMethod: setDefaultMutation.mutateAsync,
    deleteMethod: deleteMutation.mutateAsync,
    isDeleting: deleteMutation.isPending,
  };
}

export function useInvoices(filters?: {
  search?: string;
  status?: string;
  year?: number;
}) {
  const queryClient = useQueryClient();

  const { data: invoices = [], isLoading } = useQuery({
    queryKey: ["invoices", filters],
    queryFn: () => billingService.getInvoices(filters),
  });

  const downloadMutation = useMutation({
    mutationFn: (invoiceId: string) =>
      billingService.downloadInvoice(invoiceId),
  });

  return {
    invoices,
    isLoading,
    downloadInvoice: downloadMutation.mutate,
    isDownloading: downloadMutation.isPending,
  };
}
```

---

## 📝 TIPOS TYPESCRIPT

```typescript
// src/types/billing.ts
export interface Subscription {
  id: string;
  planId: string;
  planName: string;
  monthlyPrice: number;
  status: "active" | "cancelled" | "past_due" | "trialing";
  isEarlyBird: boolean;
  nextBillingDate: string;
  nextBillingAmount: number;
  cancelledAt?: string;
  trialEndsAt?: string;
}

export interface Usage {
  activeListings: number;
  maxListings: number;
  viewsThisMonth: number;
  leadsThisMonth: number;
}

export interface PaymentMethod {
  id: string;
  brand: "visa" | "mastercard" | "amex";
  last4: string;
  expiryMonth: number;
  expiryYear: number;
  isDefault: boolean;
  holderName: string;
}

export interface Payment {
  id: string;
  description: string;
  amount: number;
  status: "completed" | "pending" | "failed" | "refunded";
  createdAt: string;
  paymentMethodLast4?: string;
}

export interface Invoice {
  id: string;
  invoiceNumber: string;
  ncf?: string;
  description: string;
  amount: number;
  tax: number;
  total: number;
  status: "paid" | "pending" | "overdue" | "cancelled";
  issuedAt: string;
  dueDate: string;
  paidAt?: string;
  downloadUrl: string;
}

export interface UpcomingInvoice {
  subtotal: number;
  discount: number;
  tax: number;
  total: number;
  dueDate: string;
}

export interface Plan {
  id: string;
  name: string;
  description: string;
  monthlyPrice: number;
  maxListings: number;
  recommended?: boolean;
  features: { name: string; included: boolean }[];
}

export interface BillingDashboard {
  subscription: Subscription;
  usage?: Usage;
  recentPayments: Payment[];
  upcomingInvoice?: UpcomingInvoice;
  paymentMethods: PaymentMethod[];
}
```

---

## 🛣️ RUTAS

```typescript
// src/App.tsx
import BillingDashboardPage from "./pages/billing/BillingDashboardPage";
import PaymentMethodsPage from "./pages/billing/PaymentMethodsPage";
import PaymentsPage from "./pages/billing/PaymentsPage";
import PlansPage from "./pages/billing/PlansPage";
import InvoicesPage from "./pages/billing/InvoicesPage";

// Rutas de billing (usuario)
<Route
  path="/billing"
  element={
    <ProtectedRoute>
      <BillingDashboardPage />
    </ProtectedRoute>
  }
/>
<Route path="/billing/payments" element={<ProtectedRoute><PaymentsPage /></ProtectedRoute>} />
<Route path="/billing/methods" element={<ProtectedRoute><PaymentMethodsPage /></ProtectedRoute>} />
<Route path="/billing/plans" element={<ProtectedRoute><PlansPage /></ProtectedRoute>} />
<Route path="/billing/invoices" element={<ProtectedRoute><InvoicesPage /></ProtectedRoute>} />

// Rutas de billing (dealer)
<Route path="/dealer/billing" element={<ProtectedRoute><BillingDashboardPage /></ProtectedRoute>} />
<Route path="/dealer/billing/payments" element={<ProtectedRoute><PaymentsPage /></ProtectedRoute>} />
<Route path="/dealer/billing/methods" element={<ProtectedRoute><PaymentMethodsPage /></ProtectedRoute>} />
<Route path="/dealer/billing/plans" element={<ProtectedRoute><PlansPage /></ProtectedRoute>} />
<Route path="/dealer/billing/invoices" element={<ProtectedRoute><InvoicesPage /></ProtectedRoute>} />
```

---

## 🔄 FLUJO DE USUARIO

```
┌─────────────────────────────────────────────────────────────────┐
│                  FLUJO DE BILLING                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  1️⃣ DASHBOARD (/billing)                                        │
│  ├─> Ver resumen: plan actual, próximo cobro, uso              │
│  ├─> Quick links: Pagos, Métodos, Facturas, Planes             │
│  ├─> Pagos recientes (últimos 5)                               │
│  └─> Sidebar: próxima factura, método principal                │
│                                                                 │
│  2️⃣ PAGOS (/billing/payments)                                   │
│  ├─> Historial completo de transacciones                       │
│  ├─> Filtros: fecha, estado, monto                             │
│  └─> Detalles: monto, fecha, estado, método                    │
│                                                                 │
│  3️⃣ MÉTODOS (/billing/methods)                                  │
│  ├─> Lista de tarjetas guardadas                               │
│  ├─> Agregar nuevo método (AZUL, CardNET, etc.)                │
│  ├─> Establecer método principal                               │
│  └─> Eliminar método                                           │
│                                                                 │
│  4️⃣ PLANES (/billing/plans)                                     │
│  ├─> Ver planes disponibles                                    │
│  ├─> Comparar features                                         │
│  ├─> Upgrade → pago inmediato prorrateado                      │
│  └─> Downgrade → aplica próximo ciclo                          │
│                                                                 │
│  5️⃣ FACTURAS (/billing/invoices)                                │
│  ├─> Lista de facturas con NCF                                 │
│  ├─> Filtros: año, estado, búsqueda                            │
│  └─> Descargar PDF con NCF válido                              │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## ✅ VALIDACIÓN

- [ ] Dashboard muestra resumen completo
- [ ] Uso del plan con barra de progreso
- [ ] Early Bird badge si aplica
- [ ] Lista de pagos con estados correctos
- [ ] Métodos de pago: agregar, eliminar, principal
- [ ] Planes: upgrade/downgrade con confirmación
- [ ] Facturas con NCF descargables
- [ ] Responsive en todas las páginas

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/billing-dashboard.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsDealer } from "../helpers/auth";

test.describe("Billing Dashboard", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsDealer(page);
  });

  test("debe mostrar resumen de facturación", async ({ page }) => {
    await page.goto("/dealer/billing");

    await expect(page.getByTestId("billing-summary")).toBeVisible();
  });

  test("debe ver plan actual", async ({ page }) => {
    await page.goto("/dealer/billing");

    await expect(page.getByTestId("current-plan")).toBeVisible();
  });

  test("debe ver historial de facturas", async ({ page }) => {
    await page.goto("/dealer/billing/invoices");

    await expect(page.getByTestId("invoices-list")).toBeVisible();
  });

  test("debe descargar factura con NCF", async ({ page }) => {
    await page.goto("/dealer/billing/invoices");

    const downloadPromise = page.waitForEvent("download");
    await page
      .getByTestId("invoice-row")
      .first()
      .getByRole("button", { name: /descargar/i })
      .click();
    const download = await downloadPromise;

    expect(download.suggestedFilename()).toMatch(/factura.*\.pdf/i);
  });

  test("debe agregar método de pago", async ({ page }) => {
    await page.goto("/dealer/billing/payment-methods");

    await page.getByRole("button", { name: /agregar método/i }).click();
    await expect(page.getByTestId("add-payment-modal")).toBeVisible();
  });

  test("debe ver opciones de upgrade", async ({ page }) => {
    await page.goto("/dealer/billing/plans");

    await expect(page.getByTestId("plans-comparison")).toBeVisible();
  });
});
```

---

_Última actualización: Enero 2026_
