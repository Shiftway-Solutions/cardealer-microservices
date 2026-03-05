---
title: "49 - Payment Results (Flujo AZUL)"
priority: P1
estimated_time: "2 horas"
dependencies: []
apis: ["BillingService"]
status: complete
last_updated: "2026-01-30"
---

# 💳 49 - Payment Results (Flujo AZUL)

**Objetivo:** Gestionar las páginas de resultado de pago (aprobado, rechazado, cancelado) para la pasarela AZUL.

**Prioridad:** P0 (CRÍTICO - Flujo de pagos)  
**Complejidad:** 🟡 Baja  
**Dependencias:** BillingService, azulService

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura](#-arquitectura)
2. [Páginas](#-páginas)
3. [Componentes](#-componentes)
4. [Servicios](#-servicios)
5. [Tipos TypeScript](#-tipos-typescript)
6. [Flujo de Usuario](#-flujo-de-usuario)

---

## 🏗️ ARQUITECTURA

```
pages/
├── AzulPaymentPage.tsx      # Página de pago (pre-redirección)
├── AzulApprovedPage.tsx     # Pago exitoso
├── AzulDeclinedPage.tsx     # Pago rechazado
└── AzulCancelledPage.tsx    # Pago cancelado por usuario
```

### Flujo AZUL

```
Usuario → AzulPaymentPage → AZUL (externo) → Callback URL
                                              ├── /payment/azul/approved
                                              ├── /payment/azul/declined
                                              └── /payment/azul/cancelled
```

---

## 📄 PÁGINAS

### 1. AzulPaymentPage.tsx

**Ruta:** `/payment/azul?amount=X&listingId=Y&planType=Z`

```typescript
// src/pages/AzulPaymentPage.tsx
"use client";

import { useState, useEffect } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { Loader2, AlertCircle, CreditCard, ArrowLeft, Shield } from "lucide-react";
import MainLayout from "@/layouts/MainLayout";
import { Button } from "@/components/ui/Button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { azulService } from "@/services/azulService";

export default function AzulPaymentPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Parámetros de URL
  const amount = parseFloat(searchParams.get("amount") || "0");
  const listingId = searchParams.get("listingId");
  const planType = searchParams.get("planType") as
    | "individual"
    | "dealer-basic"
    | "dealer-premium"
    | null;

  // Cálculos de impuestos (ITBIS 18%)
  const itbis = azulService.calculateITBIS(amount);
  const total = amount + itbis;

  useEffect(() => {
    if (!amount || amount <= 0) {
      setError("Monto de pago inválido");
    }
    if (!listingId && !planType) {
      setError("Información de pago incompleta");
    }
  }, [amount, listingId, planType]);

  const handlePayment = async () => {
    setLoading(true);
    setError(null);

    try {
      const orderNumber = `OKLA-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;

      const response = await azulService.initiatePayment({
        amount,
        itbis,
        orderNumber,
        description: listingId
          ? `Publicación de vehículo ${listingId}`
          : `Plan ${planType}`,
      });

      // Guardar contexto en sessionStorage para callback
      sessionStorage.setItem("azul_order_number", orderNumber);
      sessionStorage.setItem(
        "azul_payment_context",
        JSON.stringify({ listingId, planType, amount, itbis, total })
      );

      // Submit form a AZUL
      azulService.submitAzulForm(response);
    } catch (err) {
      console.error("Payment initiation error:", err);
      setError(
        err instanceof Error
          ? err.message
          : "Error al procesar el pago. Por favor intenta nuevamente."
      );
      setLoading(false);
    }
  };

  const getPlanName = () => {
    switch (planType) {
      case "individual":
        return "Publicación Individual";
      case "dealer-basic":
        return "Plan Dealer Básico";
      case "dealer-premium":
        return "Plan Dealer Premium";
      default:
        return "Servicio OKLA";
    }
  };

  return (
    <MainLayout>
      <div className="min-h-screen bg-gray-50 py-12">
        <div className="max-w-2xl mx-auto px-4">
          {/* Botón Volver */}
          <button
            onClick={() => navigate(-1)}
            className="flex items-center text-gray-600 hover:text-gray-900 mb-6"
          >
            <ArrowLeft className="w-4 h-4 mr-2" />
            Volver
          </button>

          <Card>
            <CardHeader className="text-center bg-blue-600 text-white rounded-t-lg">
              <div className="flex justify-center mb-4">
                <CreditCard className="w-16 h-16" />
              </div>
              <CardTitle className="text-2xl">Confirmar Pago</CardTitle>
              <p className="text-blue-100">Procesado de forma segura por AZUL</p>
            </CardHeader>

            <CardContent className="p-6 space-y-6">
              {/* Error */}
              {error && (
                <div className="flex items-center gap-3 p-4 bg-red-50 text-red-700 rounded-lg">
                  <AlertCircle className="w-5 h-5 flex-shrink-0" />
                  <p>{error}</p>
                </div>
              )}

              {/* Resumen */}
              <div className="bg-gray-50 rounded-lg p-6 space-y-4">
                <h3 className="font-semibold text-lg">Resumen del Pago</h3>

                <div className="space-y-3">
                  <div className="flex justify-between">
                    <span className="text-gray-600">Concepto:</span>
                    <span className="font-medium">{getPlanName()}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-600">Subtotal:</span>
                    <span>RD$ {amount.toLocaleString("es-DO")}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-600">ITBIS (18%):</span>
                    <span>RD$ {itbis.toLocaleString("es-DO")}</span>
                  </div>
                  <div className="border-t pt-3">
                    <div className="flex justify-between text-xl font-bold">
                      <span>Total:</span>
                      <span className="text-blue-600">
                        RD$ {total.toLocaleString("es-DO")}
                      </span>
                    </div>
                  </div>
                </div>
              </div>

              {/* Seguridad */}
              <div className="flex items-center gap-3 text-sm text-gray-500">
                <Shield className="w-5 h-5 text-green-500" />
                <span>
                  Transacción segura encriptada con SSL. Serás redirigido a AZUL
                  para completar el pago.
                </span>
              </div>

              {/* Botón Pagar */}
              <Button
                onClick={handlePayment}
                disabled={loading || !!error}
                className="w-full py-4 text-lg"
                size="lg"
              >
                {loading ? (
                  <>
                    <Loader2 className="w-5 h-5 animate-spin mr-2" />
                    Procesando...
                  </>
                ) : (
                  <>
                    <CreditCard className="w-5 h-5 mr-2" />
                    Pagar RD$ {total.toLocaleString("es-DO")}
                  </>
                )}
              </Button>

              {/* Métodos aceptados */}
              <div className="text-center text-sm text-gray-500">
                <p>Aceptamos:</p>
                <div className="flex justify-center gap-4 mt-2">
                  <img src="/img/visa.svg" alt="Visa" className="h-8" />
                  <img src="/img/mastercard.svg" alt="Mastercard" className="h-8" />
                  <img src="/img/azul.svg" alt="AZUL" className="h-8" />
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </MainLayout>
  );
}
```

---

### 2. AzulApprovedPage.tsx

**Ruta:** `/payment/azul/approved?OrderNumber=X&AuthorizationCode=Y&Amount=Z`

```typescript
// src/pages/AzulApprovedPage.tsx
"use client";

import { useEffect, useState } from "react";
import { useNavigate, useSearchParams, Link } from "react-router-dom";
import { CheckCircle, Home, FileText, Loader2, Car, Download } from "lucide-react";
import MainLayout from "@/layouts/MainLayout";
import { Button } from "@/components/ui/Button";
import { Card, CardContent } from "@/components/ui/Card";
import { azulService } from "@/services/azulService";

interface Transaction {
  orderNumber: string;
  authorizationCode: string;
  amount: number;
  itbis: number;
  total: number;
  status: "approved";
  createdAt: string;
}

export default function AzulApprovedPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const [loading, setLoading] = useState(true);
  const [transaction, setTransaction] = useState<Transaction | null>(null);

  // Parámetros de AZUL callback
  const orderNumber = searchParams.get("OrderNumber");
  const authCode = searchParams.get("AuthorizationCode");

  useEffect(() => {
    const fetchTransaction = async () => {
      if (!orderNumber) {
        setLoading(false);
        return;
      }

      try {
        const tx = await azulService.getTransaction(orderNumber);
        setTransaction(tx);
      } catch (error) {
        console.error("Error fetching transaction:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchTransaction();

    // Limpiar sessionStorage
    sessionStorage.removeItem("azul_order_number");
    sessionStorage.removeItem("azul_payment_context");
  }, [orderNumber]);

  if (loading) {
    return (
      <MainLayout>
        <div className="min-h-screen bg-gray-50 flex items-center justify-center">
          <div className="text-center">
            <Loader2 className="w-12 h-12 animate-spin text-blue-600 mx-auto mb-4" />
            <p className="text-gray-600">Verificando transacción...</p>
          </div>
        </div>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <div className="min-h-screen bg-gray-50 py-12">
        <div className="max-w-2xl mx-auto px-4">
          <Card className="overflow-hidden">
            {/* Header de éxito */}
            <div className="bg-gradient-to-r from-green-500 to-green-600 px-6 py-12 text-center">
              <div className="inline-flex items-center justify-center w-20 h-20 bg-white rounded-full mb-4">
                <CheckCircle className="w-12 h-12 text-green-500" />
              </div>
              <h1 className="text-3xl font-bold text-white mb-2">
                ¡Pago Aprobado!
              </h1>
              <p className="text-green-100 text-lg">
                Tu transacción ha sido procesada exitosamente
              </p>
            </div>

            <CardContent className="p-6 space-y-6">
              {/* Detalles */}
              <div className="bg-gray-50 rounded-lg p-6 space-y-4">
                <h2 className="text-lg font-semibold">Detalles de la transacción</h2>

                {transaction ? (
                  <div className="grid grid-cols-2 gap-4 text-sm">
                    <div>
                      <p className="text-gray-600">Orden #</p>
                      <p className="font-mono font-medium">{transaction.orderNumber}</p>
                    </div>
                    <div>
                      <p className="text-gray-600">Código de Autorización</p>
                      <p className="font-mono font-medium">
                        {transaction.authorizationCode || authCode || "N/A"}
                      </p>
                    </div>
                    <div>
                      <p className="text-gray-600">Monto</p>
                      <p className="font-medium">
                        RD$ {transaction.total.toLocaleString("es-DO")}
                      </p>
                    </div>
                    <div>
                      <p className="text-gray-600">Fecha</p>
                      <p className="font-medium">
                        {new Date(transaction.createdAt).toLocaleDateString("es-DO", {
                          year: "numeric",
                          month: "long",
                          day: "numeric",
                          hour: "2-digit",
                          minute: "2-digit",
                        })}
                      </p>
                    </div>
                  </div>
                ) : (
                  <div className="grid grid-cols-2 gap-4 text-sm">
                    <div>
                      <p className="text-gray-600">Orden #</p>
                      <p className="font-mono font-medium">{orderNumber}</p>
                    </div>
                    <div>
                      <p className="text-gray-600">Código de Autorización</p>
                      <p className="font-mono font-medium">{authCode || "N/A"}</p>
                    </div>
                  </div>
                )}
              </div>

              {/* Aviso de confirmación */}
              <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
                <p className="text-sm text-blue-700">
                  📧 Te hemos enviado un recibo a tu correo electrónico. Guarda
                  este número de orden para cualquier consulta.
                </p>
              </div>

              {/* Acciones */}
              <div className="flex flex-col sm:flex-row gap-4">
                <Button asChild className="flex-1">
                  <Link to="/dashboard">
                    <Car className="w-4 h-4 mr-2" />
                    Ver mi Vehículo
                  </Link>
                </Button>
                <Button variant="outline" asChild className="flex-1">
                  <Link to="/">
                    <Home className="w-4 h-4 mr-2" />
                    Ir al Inicio
                  </Link>
                </Button>
              </div>

              {/* Descargar recibo */}
              <div className="text-center">
                <Button variant="ghost" size="sm">
                  <Download className="w-4 h-4 mr-2" />
                  Descargar Recibo PDF
                </Button>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </MainLayout>
  );
}
```

---

### 3. AzulDeclinedPage.tsx

**Ruta:** `/payment/azul/declined?OrderNumber=X&ErrorMessage=Y`

```typescript
// src/pages/AzulDeclinedPage.tsx
"use client";

import { useSearchParams, Link } from "react-router-dom";
import { XCircle, RefreshCw, HelpCircle, ArrowLeft, AlertTriangle } from "lucide-react";
import MainLayout from "@/layouts/MainLayout";
import { Button } from "@/components/ui/Button";
import { Card, CardContent } from "@/components/ui/Card";

export default function AzulDeclinedPage() {
  const [searchParams] = useSearchParams();

  const orderNumber = searchParams.get("OrderNumber");
  const errorMessage = searchParams.get("ErrorMessage") || "Transacción no autorizada";
  const errorCode = searchParams.get("ErrorCode");

  // Mensajes amigables según código de error
  const getErrorDescription = (code: string | null) => {
    const errorMessages: Record<string, string> = {
      "51": "Fondos insuficientes en la tarjeta",
      "54": "Tarjeta expirada",
      "57": "Transacción no permitida para este tipo de tarjeta",
      "62": "Tarjeta restringida",
      "65": "Límite de crédito excedido",
      "91": "Banco emisor no disponible",
      default: "La transacción no pudo ser procesada por el banco",
    };
    return errorMessages[code || "default"] || errorMessages.default;
  };

  return (
    <MainLayout>
      <div className="min-h-screen bg-gray-50 py-12">
        <div className="max-w-2xl mx-auto px-4">
          <Card className="overflow-hidden">
            {/* Header de error */}
            <div className="bg-gradient-to-r from-red-500 to-red-600 px-6 py-12 text-center">
              <div className="inline-flex items-center justify-center w-20 h-20 bg-white rounded-full mb-4">
                <XCircle className="w-12 h-12 text-red-500" />
              </div>
              <h1 className="text-3xl font-bold text-white mb-2">
                Pago Rechazado
              </h1>
              <p className="text-red-100 text-lg">
                No pudimos procesar tu pago
              </p>
            </div>

            <CardContent className="p-6 space-y-6">
              {/* Razón del rechazo */}
              <div className="bg-red-50 border border-red-200 rounded-lg p-4">
                <div className="flex items-start gap-3">
                  <AlertTriangle className="w-5 h-5 text-red-500 flex-shrink-0 mt-0.5" />
                  <div>
                    <p className="font-medium text-red-800">
                      {getErrorDescription(errorCode)}
                    </p>
                    {errorCode && (
                      <p className="text-sm text-red-600 mt-1">
                        Código de error: {errorCode}
                      </p>
                    )}
                  </div>
                </div>
              </div>

              {/* Sugerencias */}
              <div className="space-y-3">
                <h3 className="font-semibold">¿Qué puedes hacer?</h3>
                <ul className="space-y-2 text-gray-600">
                  <li className="flex items-start gap-2">
                    <span className="text-blue-500">•</span>
                    Verifica que la información de tu tarjeta sea correcta
                  </li>
                  <li className="flex items-start gap-2">
                    <span className="text-blue-500">•</span>
                    Asegúrate de tener fondos suficientes
                  </li>
                  <li className="flex items-start gap-2">
                    <span className="text-blue-500">•</span>
                    Intenta con otra tarjeta de crédito o débito
                  </li>
                  <li className="flex items-start gap-2">
                    <span className="text-blue-500">•</span>
                    Contacta a tu banco si el problema persiste
                  </li>
                </ul>
              </div>

              {/* Orden */}
              {orderNumber && (
                <div className="bg-gray-50 rounded-lg p-4">
                  <p className="text-sm text-gray-600">
                    Referencia: <span className="font-mono">{orderNumber}</span>
                  </p>
                </div>
              )}

              {/* Acciones */}
              <div className="flex flex-col sm:flex-row gap-4">
                <Button
                  onClick={() => window.history.back()}
                  className="flex-1"
                >
                  <RefreshCw className="w-4 h-4 mr-2" />
                  Intentar Nuevamente
                </Button>
                <Button variant="outline" asChild className="flex-1">
                  <Link to="/help">
                    <HelpCircle className="w-4 h-4 mr-2" />
                    Contactar Soporte
                  </Link>
                </Button>
              </div>

              {/* Volver */}
              <div className="text-center">
                <Link
                  to="/"
                  className="inline-flex items-center text-gray-600 hover:text-gray-900"
                >
                  <ArrowLeft className="w-4 h-4 mr-2" />
                  Volver al inicio
                </Link>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </MainLayout>
  );
}
```

---

### 4. AzulCancelledPage.tsx

**Ruta:** `/payment/azul/cancelled`

```typescript
// src/pages/AzulCancelledPage.tsx
"use client";

import { Link } from "react-router-dom";
import { XCircle, ArrowLeft, RefreshCw, ShoppingCart } from "lucide-react";
import MainLayout from "@/layouts/MainLayout";
import { Button } from "@/components/ui/Button";
import { Card, CardContent } from "@/components/ui/Card";

export default function AzulCancelledPage() {
  // Recuperar contexto del pago cancelado
  const context = sessionStorage.getItem("azul_payment_context");
  const paymentContext = context ? JSON.parse(context) : null;

  // Limpiar sessionStorage
  sessionStorage.removeItem("azul_order_number");
  sessionStorage.removeItem("azul_payment_context");

  return (
    <MainLayout>
      <div className="min-h-screen bg-gray-50 py-12">
        <div className="max-w-2xl mx-auto px-4">
          <Card className="overflow-hidden">
            {/* Header */}
            <div className="bg-gradient-to-r from-gray-500 to-gray-600 px-6 py-12 text-center">
              <div className="inline-flex items-center justify-center w-20 h-20 bg-white rounded-full mb-4">
                <XCircle className="w-12 h-12 text-gray-500" />
              </div>
              <h1 className="text-3xl font-bold text-white mb-2">
                Pago Cancelado
              </h1>
              <p className="text-gray-200 text-lg">
                Has cancelado el proceso de pago
              </p>
            </div>

            <CardContent className="p-6 space-y-6">
              {/* Mensaje */}
              <div className="text-center py-4">
                <p className="text-gray-600 text-lg">
                  No te preocupes, no se ha realizado ningún cargo a tu tarjeta.
                </p>
              </div>

              {/* Información del pago (si disponible) */}
              {paymentContext && (
                <div className="bg-gray-50 rounded-lg p-4">
                  <h3 className="font-medium mb-3">Tu orden guardada:</h3>
                  <div className="flex justify-between text-sm">
                    <span className="text-gray-600">Monto:</span>
                    <span className="font-medium">
                      RD$ {paymentContext.total?.toLocaleString("es-DO")}
                    </span>
                  </div>
                  <p className="text-xs text-gray-500 mt-3">
                    Puedes completar tu pago cuando estés listo.
                  </p>
                </div>
              )}

              {/* Acciones */}
              <div className="flex flex-col sm:flex-row gap-4">
                <Button
                  onClick={() => window.history.go(-2)}
                  className="flex-1"
                >
                  <RefreshCw className="w-4 h-4 mr-2" />
                  Reintentar Pago
                </Button>
                <Button variant="outline" asChild className="flex-1">
                  <Link to="/vehicles">
                    <ShoppingCart className="w-4 h-4 mr-2" />
                    Seguir Navegando
                  </Link>
                </Button>
              </div>

              {/* Volver */}
              <div className="text-center">
                <Link
                  to="/"
                  className="inline-flex items-center text-gray-600 hover:text-gray-900"
                >
                  <ArrowLeft className="w-4 h-4 mr-2" />
                  Volver al inicio
                </Link>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </MainLayout>
  );
}
```

---

## 🔧 SERVICIOS

### azulService.ts

```typescript
// src/services/azulService.ts
import { apiClient } from "./api-client";

export interface InitiatePaymentRequest {
  amount: number;
  itbis: number;
  orderNumber: string;
  description: string;
}

export interface AzulFormData {
  merchantId: string;
  merchantName: string;
  merchantType: string;
  currencyCode: string;
  orderNumber: string;
  amount: string;
  itbis: string;
  approvedUrl: string;
  declinedUrl: string;
  cancelUrl: string;
  useCustomField1: string;
  customField1Label: string;
  customField1Value: string;
  authHash: string;
}

class AzulService {
  private readonly ITBIS_RATE = 0.18; // 18% en RD

  calculateITBIS(amount: number): number {
    return Math.round(amount * this.ITBIS_RATE * 100) / 100;
  }

  async initiatePayment(
    request: InitiatePaymentRequest,
  ): Promise<AzulFormData> {
    const response = await apiClient.post<AzulFormData>(
      "/api/azul-payment/initiate",
      request,
    );
    return response.data;
  }

  submitAzulForm(formData: AzulFormData): void {
    const form = document.createElement("form");
    form.method = "POST";
    form.action = "https://pagos.azul.com.do/PaymentPage/";

    Object.entries(formData).forEach(([key, value]) => {
      const input = document.createElement("input");
      input.type = "hidden";
      input.name = key;
      input.value = value;
      form.appendChild(input);
    });

    document.body.appendChild(form);
    form.submit();
  }

  async getTransaction(orderNumber: string) {
    const response = await apiClient.get(
      `/api/azul-payment/transaction/${orderNumber}`,
    );
    return response.data;
  }

  async refundTransaction(orderNumber: string, amount?: number) {
    const response = await apiClient.post(`/api/azul-payment/refund`, {
      orderNumber,
      amount,
    });
    return response.data;
  }
}

export const azulService = new AzulService();
```

---

## 📝 TIPOS TYPESCRIPT

```typescript
// src/types/payment.ts
export type PaymentStatus =
  | "pending"
  | "approved"
  | "declined"
  | "cancelled"
  | "refunded";

export interface PaymentTransaction {
  id: string;
  orderNumber: string;
  authorizationCode?: string;
  amount: number;
  itbis: number;
  total: number;
  status: PaymentStatus;
  provider: "azul" | "cardnet" | "pixelpay" | "fygaro" | "paypal";
  listingId?: string;
  planType?: string;
  errorCode?: string;
  errorMessage?: string;
  createdAt: string;
  updatedAt: string;
}

export interface PaymentContext {
  listingId?: string;
  planType?: "individual" | "dealer-basic" | "dealer-premium";
  amount: number;
  itbis: number;
  total: number;
}
```

---

## 🛣️ RUTAS

```typescript
// src/App.tsx - agregar rutas
import AzulPaymentPage from "./pages/AzulPaymentPage";
import AzulApprovedPage from "./pages/AzulApprovedPage";
import AzulDeclinedPage from "./pages/AzulDeclinedPage";
import AzulCancelledPage from "./pages/AzulCancelledPage";

// En el router:
<Route path="/payment/azul" element={<AzulPaymentPage />} />
<Route path="/payment/azul/approved" element={<AzulApprovedPage />} />
<Route path="/payment/azul/declined" element={<AzulDeclinedPage />} />
<Route path="/payment/azul/cancelled" element={<AzulCancelledPage />} />
```

---

## 🔄 FLUJO DE USUARIO

```
┌─────────────────────────────────────────────────────────────────┐
│                     FLUJO DE PAGO AZUL                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  1️⃣ CHECKOUT                                                    │
│  ├─> Usuario selecciona plan o publicación                     │
│  ├─> Click "Pagar con AZUL"                                    │
│  └─> Redirige a /payment/azul?amount=X&planType=Y              │
│                                                                 │
│  2️⃣ CONFIRMACIÓN                                                │
│  ├─> AzulPaymentPage muestra resumen                           │
│  ├─> Usuario revisa: Subtotal + ITBIS = Total                  │
│  └─> Click "Pagar RD$ X,XXX"                                   │
│                                                                 │
│  3️⃣ PROCESAMIENTO                                               │
│  ├─> Backend genera hash y form data                           │
│  ├─> Frontend submit form a AZUL                               │
│  └─> Usuario en página de AZUL (externo)                       │
│                                                                 │
│  4️⃣ RESULTADO                                                   │
│  ├─> AZUL procesa tarjeta                                      │
│  └─> Redirect a callback URL:                                  │
│      ├─> ✅ /payment/azul/approved (éxito)                     │
│      ├─> ❌ /payment/azul/declined (rechazado)                 │
│      └─> ⚪ /payment/azul/cancelled (cancelado)                │
│                                                                 │
│  5️⃣ POST-PAGO                                                   │
│  ├─> Backend actualiza estado del listing/suscripción          │
│  ├─> Envía email de confirmación                               │
│  └─> Usuario puede ver su vehículo publicado                   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## ✅ VALIDACIÓN

- [ ] Monto válido (> 0)
- [ ] Contexto de pago presente (listingId o planType)
- [ ] ITBIS calculado correctamente (18%)
- [ ] SessionStorage limpiado después de callbacks
- [ ] Errores mostrados de forma amigable
- [ ] Botones de reintentar funcionando
- [ ] Links de navegación correctos

---

## 🎉 RESUMEN

| Página              | Ruta                      | Propósito             |
| ------------------- | ------------------------- | --------------------- |
| `AzulPaymentPage`   | `/payment/azul`           | Confirmación pre-pago |
| `AzulApprovedPage`  | `/payment/azul/approved`  | Pago exitoso          |
| `AzulDeclinedPage`  | `/payment/azul/declined`  | Pago rechazado        |
| `AzulCancelledPage` | `/payment/azul/cancelled` | Pago cancelado        |

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/payment-results.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsUser } from "../helpers/auth";

test.describe("Payment Results Pages", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test("debe mostrar página de pago aprobado", async ({ page }) => {
    await page.goto("/payment/azul/approved?txId=TEST123");

    await expect(page.getByTestId("payment-success")).toBeVisible();
    await expect(page.getByText(/pago exitoso/i)).toBeVisible();
  });

  test("debe mostrar confirmación con detalles", async ({ page }) => {
    await page.goto("/payment/azul/approved?txId=TEST123");

    await expect(page.getByTestId("transaction-id")).toBeVisible();
    await expect(page.getByTestId("amount-paid")).toBeVisible();
  });

  test("debe mostrar página de pago rechazado", async ({ page }) => {
    await page.goto("/payment/azul/declined?reason=insufficient_funds");

    await expect(page.getByTestId("payment-declined")).toBeVisible();
    await expect(page.getByText(/pago rechazado/i)).toBeVisible();
  });

  test("debe ofrecer reintentar pago", async ({ page }) => {
    await page.goto("/payment/azul/declined");

    await page.getByRole("button", { name: /reintentar/i }).click();
    await expect(page).toHaveURL(/checkout/);
  });

  test("debe mostrar página de pago cancelado", async ({ page }) => {
    await page.goto("/payment/azul/cancelled");

    await expect(page.getByTestId("payment-cancelled")).toBeVisible();
  });
});
```

---

_Última actualización: Enero 2026_
