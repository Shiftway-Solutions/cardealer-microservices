/**
 * Checkout Page
 *
 * Main checkout flow for premium services
 *
 * Route: /checkout?product=boost-basic
 */

'use client';

import * as React from 'react';
import { Suspense } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import Link from 'next/link';
import { useQuery, useMutation } from '@tanstack/react-query';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { Separator } from '@/components/ui/separator';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import {
  CreditCard,
  Shield,
  Lock,
  Check,
  ArrowLeft,
  Info,
  AlertCircle,
  RefreshCw,
  Loader2,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { toast } from 'sonner';
import {
  checkoutService,
  updateProductsWithPricing,
  type CreateCheckoutRequest,
  type AvailableProvider,
} from '@/services/checkout';
import { usePlatformPricing } from '@/hooks/use-platform-pricing';
import { StripePaymentForm } from '@/components/checkout/StripePaymentForm';
import { PayPalPaymentButton } from '@/components/checkout/PayPalPaymentButton';
import {
  serverCreatePaymentIntent,
  serverCreatePayPalOrder,
  serverCapturePayPalOrder,
} from '@/actions/checkout';

// =============================================================================
// LOADING STATE
// =============================================================================

function CheckoutLoading() {
  return (
    <div className="bg-muted/50 min-h-screen py-8">
      <div className="container mx-auto max-w-4xl px-4">
        <Skeleton className="mb-8 h-8 w-32" />
        <div className="grid grid-cols-1 gap-8 lg:grid-cols-3">
          <div className="space-y-6 lg:col-span-2">
            <Skeleton className="h-48 w-full rounded-lg" />
            <Skeleton className="h-64 w-full rounded-lg" />
          </div>
          <Skeleton className="h-96 w-full rounded-lg" />
        </div>
      </div>
    </div>
  );
}

// =============================================================================
// ERROR STATE
// =============================================================================

function CheckoutError({ message, onRetry }: { message: string; onRetry: () => void }) {
  const router = useRouter();

  return (
    <div className="bg-muted/50 min-h-screen">
      <div className="container mx-auto max-w-4xl px-4 py-16">
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-16">
            <AlertCircle className="mb-4 h-16 w-16 text-red-400" />
            <h2 className="mb-2 text-xl font-semibold">Error en el checkout</h2>
            <p className="text-muted-foreground mb-6">{message}</p>
            <div className="flex gap-3">
              <Button variant="outline" onClick={() => router.back()}>
                <ArrowLeft className="mr-2 h-4 w-4" />
                Volver
              </Button>
              <Button onClick={onRetry}>
                <RefreshCw className="mr-2 h-4 w-4" />
                Reintentar
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

// =============================================================================
// GATEWAY DISPLAY HELPERS
// =============================================================================

/** Gateway metadata for rendering in checkout UI */
const GATEWAY_DISPLAY: Record<
  string,
  { label: string; description: string; color: string; abbr: string }
> = {
  azul: {
    label: 'AZUL',
    description: 'Banco Popular',
    color: 'bg-blue-600',
    abbr: 'AZUL',
  },
  cardnet: {
    label: 'CardNET',
    description: 'Visa, Mastercard',
    color: 'bg-red-600',
    abbr: 'CN',
  },
  pixelpay: {
    label: 'PixelPay',
    description: 'Tarjeta de crédito',
    color: 'bg-purple-600',
    abbr: 'PP',
  },
  fygaro: {
    label: 'Fygaro',
    description: 'Pago en línea',
    color: 'bg-teal-600',
    abbr: 'FY',
  },
  paypal: {
    label: 'PayPal',
    description: 'Cuenta PayPal',
    color: 'bg-[#003087]',
    abbr: 'PP',
  },
  stripe: {
    label: 'Stripe',
    description: 'Tarjeta internacional',
    color: 'bg-indigo-600',
    abbr: 'ST',
  },
};

/** Gateways that redirect the user to an external payment page */
const REDIRECT_GATEWAYS = new Set(['azul', 'fygaro']);

/** Gateways that accept card input directly in the form (legacy — non-Stripe) */
const CARD_INPUT_GATEWAYS = new Set(['cardnet', 'pixelpay']);

/** Gateways with dedicated SDK components */
const STRIPE_GATEWAY = 'stripe';
const PAYPAL_GATEWAY = 'paypal';

function getGatewayDisplayInfo(gw: AvailableProvider) {
  const key = gw.gateway.toLowerCase();
  const meta = GATEWAY_DISPLAY[key] ?? {
    label: gw.name,
    description: gw.type,
    color: 'bg-gray-600',
    abbr: gw.gateway.substring(0, 2).toUpperCase(),
  };
  return {
    label: meta.label,
    description: meta.description,
    icon: (
      <div
        className={cn(
          'mb-2 flex h-8 w-8 items-center justify-center rounded text-xs font-bold text-white',
          meta.color
        )}
      >
        {meta.abbr}
      </div>
    ),
  };
}

function isRedirectGateway(method: string): boolean {
  return REDIRECT_GATEWAYS.has(method);
}

function isCardInputGateway(method: string): boolean {
  return CARD_INPUT_GATEWAYS.has(method);
}

function getGatewayLabel(method: string): string {
  return GATEWAY_DISPLAY[method]?.label ?? method.toUpperCase();
}

// =============================================================================
// CHECKOUT CONTENT
// =============================================================================

function CheckoutContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const productId = searchParams.get('product') || 'boost-basic';
  const vehicleId = searchParams.get('vehicleId') || undefined;
  const dealerId = searchParams.get('dealerId') || undefined;

  // Sync dynamic pricing from ConfigurationService
  const { pricing, isLoading: pricingLoading } = usePlatformPricing();

  // Update checkout products when pricing loads
  React.useEffect(() => {
    if (pricing) {
      updateProductsWithPricing(pricing);
    }
  }, [pricing]);

  // Fetch product data (after pricing is loaded to get dynamic prices)
  const {
    data: product,
    isLoading: productLoading,
    error: productError,
    refetch: refetchProduct,
  } = useQuery({
    queryKey: ['checkout-product', productId, pricing],
    queryFn: () => checkoutService.getProduct(productId),
    enabled: !pricingLoading,
  });

  // Fetch available payment gateways (admin-enabled only)
  const { data: availableGateways = [], isLoading: gatewaysLoading } = useQuery({
    queryKey: ['available-gateways'],
    queryFn: () => checkoutService.getAvailableGateways(),
    staleTime: 60_000, // Cache 1 min
  });

  // State
  const [paymentMethod, setPaymentMethod] = React.useState<string>('');
  const [promoCode, setPromoCode] = React.useState('');
  const [promoApplied, setPromoApplied] = React.useState<{
    discountAmount: number;
    discountType: 'percentage' | 'fixed';
    discountValue: number;
  } | null>(null);
  // Stripe Elements state — card data never touches our frontend state
  const [stripeClientSecret, setStripeClientSecret] = React.useState<string | null>(null);
  const [stripeLoading, setStripeLoading] = React.useState(false);
  // Legacy card data for non-SDK gateways (CardNET, PixelPay)
  // ⚠️ PCI SECURITY NOTE: These gateways should migrate to their own hosted SDKs
  const [cardData, setCardData] = React.useState({
    number: '',
    name: '',
    expiry: '',
    cvv: '',
  });

  // Clear sensitive card data on unmount (PCI best practice)
  React.useEffect(() => {
    return () => setCardData({ number: '', name: '', expiry: '', cvv: '' });
  }, []);

  // Auto-select first gateway when loaded
  React.useEffect(() => {
    if (availableGateways.length > 0 && !paymentMethod) {
      setPaymentMethod(availableGateways[0].gateway.toLowerCase());
    }
  }, [availableGateways, paymentMethod]);

  // Calculate prices (must be before Stripe effect that depends on total)
  const subtotal = product?.price ?? 0;
  const discount = promoApplied?.discountAmount ?? 0;
  const subtotalAfterDiscount = subtotal - discount;
  const tax = checkoutService.calculateTax(subtotalAfterDiscount);
  const total = subtotalAfterDiscount + tax;

  // Create Stripe PaymentIntent when Stripe is selected
  React.useEffect(() => {
    if (paymentMethod !== STRIPE_GATEWAY || !product || total <= 0) {
      setStripeClientSecret(null);
      return;
    }
    let cancelled = false;
    async function createIntent() {
      setStripeLoading(true);
      try {
        // First create a checkout session to get session ID
        const request: CreateCheckoutRequest = {
          productId,
          vehicleId,
          dealerId,
          promoCode: promoApplied ? promoCode : undefined,
          paymentMethod: STRIPE_GATEWAY,
          returnUrl: `${window.location.origin}/checkout/exito`,
          cancelUrl: `${window.location.origin}/checkout?product=${productId}`,
        };
        const session = await checkoutService.createCheckoutSession(request);
        if (cancelled) return;

        // Then create PaymentIntent via server action
        const result = await serverCreatePaymentIntent(session.sessionId, '');
        if (cancelled) return;

        if (result.success && result.data?.clientSecret) {
          setStripeClientSecret(result.data.clientSecret);
        } else {
          toast.error(result.error || 'Error al inicializar Stripe');
        }
      } catch {
        if (!cancelled) toast.error('Error al conectar con Stripe');
      } finally {
        if (!cancelled) setStripeLoading(false);
      }
    }
    createIntent();
    return () => {
      cancelled = true;
    };
  }, [paymentMethod, product, total, productId, vehicleId, dealerId, promoApplied, promoCode]);

  // Validate promo code mutation
  const promoMutation = useMutation({
    mutationFn: (code: string) => checkoutService.validatePromoCode(code, productId),
    onSuccess: result => {
      if (result.valid) {
        setPromoApplied({
          discountAmount: result.discountAmount,
          discountType: result.discountType,
          discountValue: result.discountValue,
        });
        toast.success('Código promocional aplicado');
      } else {
        toast.error(result.errorMessage || 'Código no válido');
      }
    },
    onError: () => {
      toast.error('Error al validar el código');
    },
  });

  // Process payment mutation
  const paymentMutation = useMutation({
    mutationFn: async () => {
      // First create checkout session
      const request: CreateCheckoutRequest = {
        productId,
        vehicleId,
        dealerId,
        promoCode: promoApplied ? promoCode : undefined,
        paymentMethod,
        returnUrl: `${window.location.origin}/checkout/exito`,
        cancelUrl: `${window.location.origin}/checkout?product=${productId}`,
      };

      const session = await checkoutService.createCheckoutSession(request);

      // If payment URL provided, redirect (for Azul)
      if (session.paymentUrl) {
        window.location.href = session.paymentUrl;
        return { redirected: true };
      }

      // Process payment directly
      // SECURITY: Card data must be tokenized via payment provider (Stripe Elements / Azul).
      // Raw card data is NEVER sent to our backend — this creates a token via the provider's SDK.
      const result = await checkoutService.processPayment({
        sessionId: session.sessionId,
        paymentGateway: paymentMethod,
        // Card fields are for UI validation only — actual tokenization happens
        // server-side via Stripe/Azul SDK redirect. This is a fallback for direct processing.
        cardholderName: cardData.name,
        last4: cardData.number.slice(-4),
      });

      return result;
    },
    onSuccess: result => {
      if (result && 'redirected' in result && result.redirected) return;
      if (result && 'success' in result && result.success && result.orderId) {
        router.push(`/checkout/exito?orderId=${result.orderId}`);
      } else if (result && 'error' in result) {
        toast.error(result.error || 'Error al procesar el pago');
      }
    },
    onError: () => {
      toast.error('Error al procesar el pago');
    },
  });

  const handleApplyPromo = () => {
    if (promoCode.trim()) {
      promoMutation.mutate(promoCode.trim());
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    // Stripe and PayPal are handled by their own SDK components — skip validation
    if (paymentMethod === STRIPE_GATEWAY || paymentMethod === PAYPAL_GATEWAY) return;

    // Basic validation for legacy card-type gateways
    const selectedGateway = availableGateways.find(g => g.gateway.toLowerCase() === paymentMethod);
    const isCardType = selectedGateway?.type === 'CreditCard';

    if (isCardType && !isRedirectGateway(paymentMethod)) {
      if (!cardData.number || !cardData.name || !cardData.expiry || !cardData.cvv) {
        toast.error('Por favor completa todos los campos de la tarjeta');
        return;
      }
    }

    paymentMutation.mutate();

    // Clear sensitive card data from state immediately after submission
    setCardData({ number: '', name: '', expiry: '', cvv: '' });
  };

  // Loading state
  if (productLoading || gatewaysLoading) {
    return <CheckoutLoading />;
  }

  // Error state
  if (productError || !product) {
    return (
      <CheckoutError
        message="No se pudo cargar la información del producto"
        onRetry={() => refetchProduct()}
      />
    );
  }

  const isProcessing = paymentMutation.isPending;

  return (
    <div className="bg-muted/50 min-h-screen">
      {/* Header */}
      <header className="border-border bg-card border-b py-4">
        <div className="container mx-auto px-4">
          <div className="flex items-center justify-between">
            <Button variant="ghost" size="sm" onClick={() => router.back()}>
              <ArrowLeft className="mr-2 h-4 w-4" />
              Volver
            </Button>
            <div className="text-primary flex items-center gap-2">
              <Shield className="h-5 w-5" />
              <span className="text-sm font-medium">Pago Seguro</span>
            </div>
          </div>
        </div>
      </header>

      <div className="container mx-auto px-4 py-8">
        <div className="mx-auto max-w-4xl">
          <h1 className="mb-8 text-2xl font-bold">Checkout</h1>

          <div className="grid grid-cols-1 gap-8 lg:grid-cols-3">
            {/* Payment Form */}
            <div className="space-y-6 lg:col-span-2">
              {/* Payment Method */}
              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <CreditCard className="h-5 w-5" />
                    Método de Pago
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  {availableGateways.length === 0 ? (
                    <div className="flex items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
                      <AlertCircle className="h-5 w-5 shrink-0" />
                      <p>No hay métodos de pago disponibles en este momento. Intenta más tarde.</p>
                    </div>
                  ) : (
                    <RadioGroup
                      value={paymentMethod}
                      onValueChange={v => setPaymentMethod(v)}
                      className={cn(
                        'grid gap-4',
                        availableGateways.length <= 2 ? 'grid-cols-2' : 'grid-cols-2 sm:grid-cols-3'
                      )}
                    >
                      {availableGateways.map(gw => {
                        const gwKey = gw.gateway.toLowerCase();
                        const info = getGatewayDisplayInfo(gw);
                        return (
                          <div key={gwKey}>
                            <RadioGroupItem value={gwKey} id={gwKey} className="peer sr-only" />
                            <Label
                              htmlFor={gwKey}
                              className={cn(
                                'flex cursor-pointer flex-col items-center justify-center rounded-lg border-2 p-4',
                                'hover:bg-muted/50 transition-colors',
                                paymentMethod === gwKey
                                  ? 'border-primary bg-primary/10'
                                  : 'border-border'
                              )}
                            >
                              {info.icon}
                              <span className="font-medium">{info.label}</span>
                              <span className="text-muted-foreground text-center text-xs">
                                {info.description}
                              </span>
                            </Label>
                          </div>
                        );
                      })}
                    </RadioGroup>
                  )}
                </CardContent>
              </Card>

              {/* Stripe Elements — PCI-compliant card input */}
              {paymentMethod === STRIPE_GATEWAY && (
                <Card>
                  <CardHeader>
                    <CardTitle>Pago con Tarjeta (Stripe)</CardTitle>
                  </CardHeader>
                  <CardContent>
                    {stripeLoading ? (
                      <div className="flex items-center justify-center py-8">
                        <Loader2 className="text-primary h-8 w-8 animate-spin" />
                        <span className="text-muted-foreground ml-3">
                          Inicializando pago seguro...
                        </span>
                      </div>
                    ) : stripeClientSecret ? (
                      <StripePaymentForm
                        clientSecret={stripeClientSecret}
                        amount={checkoutService.formatCurrency(total, product.currency)}
                        onSuccess={paymentIntentId => {
                          router.push(`/checkout/exito?paymentIntentId=${paymentIntentId}`);
                        }}
                        onError={message => {
                          toast.error(message);
                        }}
                        returnUrl={`${typeof window !== 'undefined' ? window.location.origin : ''}/checkout/exito`}
                        disabled={isProcessing}
                      />
                    ) : (
                      <div className="flex items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
                        <AlertCircle className="h-5 w-5 shrink-0" />
                        <p>
                          No se pudo inicializar el pago con Stripe. Intenta seleccionar otro
                          método.
                        </p>
                      </div>
                    )}
                  </CardContent>
                </Card>
              )}

              {/* PayPal Smart Buttons */}
              {paymentMethod === PAYPAL_GATEWAY && (
                <Card>
                  <CardHeader>
                    <CardTitle>Pago con PayPal</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <PayPalPaymentButton
                      clientId={process.env.NEXT_PUBLIC_PAYPAL_CLIENT_ID || ''}
                      amount={total.toFixed(2)}
                      currency={product.currency === 'DOP' ? 'USD' : product.currency}
                      onCreateOrder={async () => {
                        const result = await serverCreatePayPalOrder(
                          total,
                          product.currency === 'DOP' ? 'USD' : product.currency,
                          product.name,
                          '',
                          `${window.location.origin}/checkout/exito`,
                          `${window.location.origin}/checkout?product=${productId}`
                        );
                        if (!result.success || !result.data?.orderId) {
                          throw new Error(result.error || 'Error al crear orden PayPal');
                        }
                        return result.data.orderId;
                      }}
                      onApprove={async orderId => {
                        const result = await serverCapturePayPalOrder(orderId, '');
                        if (result.success && result.data?.status === 'COMPLETED') {
                          router.push(`/checkout/exito?orderId=${orderId}`);
                        } else {
                          toast.error(result.error || 'Error al procesar el pago de PayPal');
                        }
                      }}
                      onError={message => {
                        toast.error(message);
                      }}
                      onCancel={() => {
                        toast.info('Pago cancelado');
                      }}
                      disabled={isProcessing}
                    />
                  </CardContent>
                </Card>
              )}

              {/* Card Details (for legacy card-type gateways like CardNET, PixelPay) */}
              {isCardInputGateway(paymentMethod) && (
                <Card>
                  <CardHeader>
                    <CardTitle>Datos de la Tarjeta</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <form onSubmit={handleSubmit} className="space-y-4">
                      <div>
                        <Label htmlFor="cardNumber">Número de Tarjeta</Label>
                        <Input
                          id="cardNumber"
                          placeholder="1234 5678 9012 3456"
                          value={cardData.number}
                          onChange={e => setCardData({ ...cardData, number: e.target.value })}
                          required
                          autoComplete="cc-number"
                          inputMode="numeric"
                        />
                      </div>

                      <div>
                        <Label htmlFor="cardName">Nombre en la Tarjeta</Label>
                        <Input
                          id="cardName"
                          placeholder="JUAN PEREZ"
                          value={cardData.name}
                          onChange={e => setCardData({ ...cardData, name: e.target.value })}
                          required
                          autoComplete="cc-name"
                        />
                      </div>

                      <div className="grid grid-cols-2 gap-4">
                        <div>
                          <Label htmlFor="expiry">Vencimiento</Label>
                          <Input
                            id="expiry"
                            placeholder="MM/AA"
                            value={cardData.expiry}
                            onChange={e => setCardData({ ...cardData, expiry: e.target.value })}
                            required
                            autoComplete="cc-exp"
                            inputMode="numeric"
                          />
                        </div>
                        <div>
                          <Label htmlFor="cvv">CVV</Label>
                          <Input
                            id="cvv"
                            type="password"
                            placeholder="123"
                            maxLength={4}
                            value={cardData.cvv}
                            onChange={e => setCardData({ ...cardData, cvv: e.target.value })}
                            required
                            autoComplete="cc-csc"
                            inputMode="numeric"
                          />
                        </div>
                      </div>

                      <Button
                        type="submit"
                        className="bg-primary hover:bg-primary/90 h-12 w-full text-lg"
                        disabled={isProcessing}
                      >
                        {isProcessing ? (
                          <>
                            <Loader2 className="mr-2 h-5 w-5 animate-spin" />
                            Procesando...
                          </>
                        ) : (
                          <>
                            <Lock className="mr-2 h-5 w-5" />
                            Pagar {checkoutService.formatCurrency(total, product.currency)}
                          </>
                        )}
                      </Button>
                    </form>
                  </CardContent>
                </Card>
              )}

              {/* Redirect-based gateway (Azul, Fygaro, etc.) */}
              {isRedirectGateway(paymentMethod) && (
                <Card>
                  <CardHeader>
                    <CardTitle>Pago con {getGatewayLabel(paymentMethod)}</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <p className="text-muted-foreground mb-4">
                      Serás redirigido a la página segura de {getGatewayLabel(paymentMethod)} para
                      completar tu pago.
                    </p>
                    <Button
                      onClick={handleSubmit}
                      className="h-12 w-full bg-blue-600 text-lg hover:bg-blue-700"
                      disabled={isProcessing}
                    >
                      {isProcessing ? (
                        <>
                          <Loader2 className="mr-2 h-5 w-5 animate-spin" />
                          Redirigiendo...
                        </>
                      ) : (
                        <>
                          <Lock className="mr-2 h-5 w-5" />
                          Pagar con {getGatewayLabel(paymentMethod)}{' '}
                          {checkoutService.formatCurrency(total, product.currency)}
                        </>
                      )}
                    </Button>
                  </CardContent>
                </Card>
              )}

              {/* Security Notice */}
              <div className="text-muted-foreground flex items-start gap-3 text-sm">
                <Shield className="text-primary h-5 w-5 shrink-0" />
                <p>
                  Tus datos de pago están protegidos con encriptación SSL de 256 bits. Nunca
                  almacenamos los datos completos de tu tarjeta.
                </p>
              </div>
            </div>

            {/* Order Summary */}
            <div>
              <Card className="sticky top-4">
                <CardHeader>
                  <CardTitle>Resumen del Pedido</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  {/* Product */}
                  <div>
                    <h3 className="font-semibold">{product.name}</h3>
                    <p className="text-muted-foreground text-sm">{product.description}</p>
                    {product.originalPrice && (
                      <Badge variant="secondary" className="bg-primary/10 text-primary mt-2">
                        Ahorra{' '}
                        {checkoutService.formatCurrency(
                          product.originalPrice - product.price,
                          product.currency
                        )}
                      </Badge>
                    )}
                  </div>

                  {/* Features */}
                  <ul className="space-y-1.5">
                    {product.features.map(feature => (
                      <li key={feature} className="flex items-start gap-2 text-sm">
                        <Check className="text-primary mt-0.5 h-4 w-4 shrink-0" />
                        {feature}
                      </li>
                    ))}
                  </ul>

                  <Separator />

                  {/* Pricing */}
                  <div className="space-y-2 text-sm">
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">Subtotal</span>
                      <span>
                        {product.originalPrice && (
                          <span className="text-muted-foreground mr-2 line-through">
                            {checkoutService.formatCurrency(
                              product.originalPrice,
                              product.currency
                            )}
                          </span>
                        )}
                        {checkoutService.formatCurrency(subtotal, product.currency)}
                      </span>
                    </div>
                    {promoApplied && (
                      <div className="text-primary flex justify-between">
                        <span>Descuento ({promoCode})</span>
                        <span>-{checkoutService.formatCurrency(discount, product.currency)}</span>
                      </div>
                    )}
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">ITBIS (18%)</span>
                      <span>{checkoutService.formatCurrency(tax, product.currency)}</span>
                    </div>
                    <Separator />
                    <div className="flex justify-between text-lg font-bold">
                      <span>Total</span>
                      <span className="text-primary">
                        {checkoutService.formatCurrency(total, product.currency)}
                      </span>
                    </div>
                  </div>

                  {/* Promo Code */}
                  <div className="flex gap-2">
                    <Input
                      placeholder="Código promocional"
                      className="flex-1"
                      value={promoCode}
                      onChange={e => setPromoCode(e.target.value)}
                      disabled={!!promoApplied}
                    />
                    <Button
                      variant="outline"
                      onClick={handleApplyPromo}
                      disabled={promoMutation.isPending || !!promoApplied}
                    >
                      {promoMutation.isPending ? (
                        <Loader2 className="h-4 w-4 animate-spin" />
                      ) : promoApplied ? (
                        <Check className="h-4 w-4" />
                      ) : (
                        'Aplicar'
                      )}
                    </Button>
                  </div>

                  {/* Info */}
                  <div className="bg-muted/50 text-muted-foreground flex items-start gap-2 rounded-lg p-3 text-xs">
                    <Info className="mt-0.5 h-4 w-4 shrink-0" />
                    <p>
                      Al completar la compra, aceptas nuestros{' '}
                      <Link href="/terminos" className="text-primary hover:underline">
                        términos y condiciones
                      </Link>
                      .
                    </p>
                  </div>
                </CardContent>
              </Card>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

// =============================================================================
// PAGE EXPORT
// =============================================================================

export default function CheckoutPage() {
  return (
    <Suspense fallback={<CheckoutLoading />}>
      <CheckoutContent />
    </Suspense>
  );
}
