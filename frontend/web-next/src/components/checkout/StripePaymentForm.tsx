/**
 * StripePaymentForm — PCI-compliant card input using Stripe Elements
 *
 * Replaces raw card <Input> fields in the checkout page.
 * Card data never touches our frontend state — it goes directly
 * to Stripe's servers via their secure iframe-based Elements.
 *
 * Flow:
 *   1. Create PaymentIntent via server action → get clientSecret
 *   2. Render <PaymentElement> inside <Elements> provider
 *   3. User fills card in Stripe's iframe
 *   4. stripe.confirmPayment() → Stripe handles 3D Secure, etc.
 *   5. Redirect to /checkout/exito on success
 */

'use client';

import * as React from 'react';
import { Elements, PaymentElement, useStripe, useElements } from '@stripe/react-stripe-js';
import { getStripe } from '@/lib/stripe';
import { Button } from '@/components/ui/button';
import { Lock, Loader2, Shield, AlertCircle } from 'lucide-react';

// =============================================================================
// TYPES
// =============================================================================

interface StripePaymentFormProps {
  /** Client secret from a Stripe PaymentIntent */
  clientSecret: string;
  /** Amount to display on the pay button */
  amount: string;
  /** Called when payment succeeds */
  onSuccess: (paymentIntentId: string) => void;
  /** Called on error */
  onError: (message: string) => void;
  /** Return URL after successful payment */
  returnUrl: string;
  /** Whether the form is in a processing state externally */
  disabled?: boolean;
}

// =============================================================================
// INNER FORM (must be inside <Elements> provider)
// =============================================================================

function StripePaymentFormInner({
  amount,
  onSuccess,
  onError,
  returnUrl,
  disabled,
}: Omit<StripePaymentFormProps, 'clientSecret'>) {
  const stripe = useStripe();
  const elements = useElements();
  const [isProcessing, setIsProcessing] = React.useState(false);
  const [isReady, setIsReady] = React.useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!stripe || !elements) return;

    setIsProcessing(true);

    const { error, paymentIntent } = await stripe.confirmPayment({
      elements,
      confirmParams: {
        return_url: returnUrl,
      },
      redirect: 'if_required',
    });

    if (error) {
      const message =
        error.type === 'card_error' || error.type === 'validation_error'
          ? (error.message ?? 'Error de tarjeta')
          : 'Error inesperado al procesar el pago';
      onError(message);
      setIsProcessing(false);
    } else if (paymentIntent) {
      if (paymentIntent.status === 'succeeded') {
        onSuccess(paymentIntent.id);
      } else if (paymentIntent.status === 'requires_action') {
        // 3D Secure or other action needed — Stripe handles this automatically
        // via the confirmPayment call with redirect: 'if_required'
        onError('Se requiere autenticación adicional. Intenta de nuevo.');
        setIsProcessing(false);
      } else {
        onError(`Estado del pago: ${paymentIntent.status}`);
        setIsProcessing(false);
      }
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <PaymentElement
        onReady={() => setIsReady(true)}
        options={{
          layout: 'tabs',
        }}
      />

      <Button
        type="submit"
        className="bg-primary hover:bg-primary/90 h-12 w-full text-lg"
        disabled={!stripe || !elements || !isReady || isProcessing || disabled}
      >
        {isProcessing ? (
          <>
            <Loader2 className="mr-2 h-5 w-5 animate-spin" />
            Procesando...
          </>
        ) : (
          <>
            <Lock className="mr-2 h-5 w-5" />
            Pagar {amount}
          </>
        )}
      </Button>

      <div className="text-muted-foreground flex items-center gap-2 text-xs">
        <Shield className="text-primary h-4 w-4 shrink-0" />
        <span>
          Pago procesado de forma segura por Stripe. Tus datos no pasan por nuestros servidores.
        </span>
      </div>
    </form>
  );
}

// =============================================================================
// WRAPPER WITH ELEMENTS PROVIDER
// =============================================================================

export function StripePaymentForm({
  clientSecret,
  amount,
  onSuccess,
  onError,
  returnUrl,
  disabled,
}: StripePaymentFormProps) {
  const [stripePromise] = React.useState(() => getStripe());

  if (!clientSecret) {
    return (
      <div className="flex items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
        <AlertCircle className="h-5 w-5 shrink-0" />
        <p>Inicializando pago seguro...</p>
      </div>
    );
  }

  return (
    <Elements
      stripe={stripePromise}
      options={{
        clientSecret,
        appearance: {
          theme: 'stripe',
          variables: {
            colorPrimary: '#0F172A',
            borderRadius: '8px',
          },
        },
        locale: 'es',
      }}
    >
      <StripePaymentFormInner
        amount={amount}
        onSuccess={onSuccess}
        onError={onError}
        returnUrl={returnUrl}
        disabled={disabled}
      />
    </Elements>
  );
}
