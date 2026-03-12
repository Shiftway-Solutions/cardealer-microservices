/**
 * ReportPurchaseModal — Embedded Stripe payment for OKLA Score™ reports
 *
 * This modal provides the complete purchase flow:
 * 1. Email input (pre-filled if authenticated)
 * 2. Stripe PaymentElement (PCI-compliant, no raw card data)
 * 3. Inline success state — unlocks the report without page reload
 * 4. Receipt email triggered on backend after confirmation
 *
 * Supports both authenticated and guest (unauthenticated) buyers.
 */

'use client';

import * as React from 'react';
import {
  CheckCircle,
  FileText,
  Lock,
  Loader2,
  Mail,
  Shield,
  X,
  AlertTriangle,
} from 'lucide-react';
import { Elements, PaymentElement, useStripe, useElements } from '@stripe/react-stripe-js';
import { getStripe } from '@/lib/stripe';
import { useAuth } from '@/hooks/use-auth';
import { createReportPaymentIntent, confirmReportPurchase } from '@/services/report-purchase';
import { DOP_USD_EXCHANGE_RATE } from '@/lib/constants';

// =============================================================================
// TYPES
// =============================================================================

interface ReportPurchaseModalProps {
  open: boolean;
  onClose: () => void;
  vehicleId: string;
  vehicleTitle: string;
  /** Called after successful payment to unlock the report inline */
  onPurchaseComplete: (purchaseId: string, buyerEmail: string) => void;
}

type ModalStep = 'email' | 'payment' | 'processing' | 'success' | 'error';

const REPORT_PRICE_DOP = 420;
const REPORT_PRICE_USD = Math.round((REPORT_PRICE_DOP / DOP_USD_EXCHANGE_RATE) * 100) / 100;

// =============================================================================
// MAIN MODAL
// =============================================================================

export function ReportPurchaseModal({
  open,
  onClose,
  vehicleId,
  vehicleTitle,
  onPurchaseComplete,
}: ReportPurchaseModalProps) {
  const { user, isAuthenticated } = useAuth();
  const [step, setStep] = React.useState<ModalStep>('email');
  const [email, setEmail] = React.useState('');
  const [clientSecret, setClientSecret] = React.useState<string | null>(null);
  const [paymentIntentId, setPaymentIntentId] = React.useState<string | null>(null);
  const [error, setError] = React.useState<string | null>(null);
  const [isCreatingIntent, setIsCreatingIntent] = React.useState(false);

  // Pre-fill email for authenticated users
  React.useEffect(() => {
    if (isAuthenticated && user?.email) {
      setEmail(user.email);
    }
  }, [isAuthenticated, user?.email]);

  // Reset state when modal opens
  React.useEffect(() => {
    if (open) {
      setStep(isAuthenticated && user?.email ? 'email' : 'email');
      setClientSecret(null);
      setPaymentIntentId(null);
      setError(null);
      setIsCreatingIntent(false);
    }
  }, [open, isAuthenticated, user?.email]);

  // Handle email submit → create PaymentIntent
  const handleEmailSubmit = React.useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();
      if (!email || !email.includes('@')) return;

      setIsCreatingIntent(true);
      setError(null);

      const result = await createReportPaymentIntent(vehicleId, email);

      if (result.success && result.data) {
        setClientSecret(result.data.clientSecret);
        setPaymentIntentId(result.data.paymentIntentId);
        setStep('payment');
      } else {
        setError(result.error || 'No se pudo iniciar el proceso de pago');
        setStep('error');
      }
      setIsCreatingIntent(false);
    },
    [email, vehicleId]
  );

  // Handle successful payment
  const handlePaymentSuccess = React.useCallback(async () => {
    if (!paymentIntentId) return;

    setStep('processing');

    const result = await confirmReportPurchase(paymentIntentId, vehicleId, email);

    if (result.success) {
      setStep('success');
      // Notify parent to unlock the report inline
      onPurchaseComplete(result.data?.purchaseId || paymentIntentId, email);
    } else {
      // Payment went through Stripe but backend confirmation failed.
      // The Stripe webhook will still process it — show success anyway.
      setStep('success');
      onPurchaseComplete(paymentIntentId, email);
    }
  }, [paymentIntentId, vehicleId, email, onPurchaseComplete]);

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onClose} />

      {/* Modal */}
      <div className="relative w-full max-w-md rounded-2xl bg-white shadow-2xl dark:bg-slate-900">
        {/* Close button */}
        <button
          onClick={onClose}
          className="absolute right-3 top-3 rounded-full p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-600 dark:hover:bg-slate-800"
          aria-label="Cerrar"
        >
          <X className="h-5 w-5" />
        </button>

        {/* Header */}
        <div className="border-b border-slate-100 px-6 pt-6 pb-4 dark:border-slate-800">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-full bg-emerald-100 dark:bg-emerald-900/50">
              <FileText className="h-5 w-5 text-emerald-600 dark:text-emerald-400" />
            </div>
            <div>
              <h2 className="text-lg font-bold text-slate-900 dark:text-white">
                Informe OKLA Score™
              </h2>
              <p className="text-sm text-slate-500 dark:text-slate-400">
                {vehicleTitle}
              </p>
            </div>
          </div>
        </div>

        {/* Body — Step content */}
        <div className="px-6 py-5">
          {step === 'email' && (
            <EmailStep
              email={email}
              setEmail={setEmail}
              isAuthenticated={isAuthenticated}
              isLoading={isCreatingIntent}
              onSubmit={handleEmailSubmit}
            />
          )}

          {step === 'payment' && clientSecret && (
            <Elements
              stripe={getStripe()}
              options={{
                clientSecret,
                appearance: {
                  theme: 'stripe',
                  variables: {
                    colorPrimary: '#059669',
                    borderRadius: '8px',
                  },
                },
              }}
            >
              <PaymentStep
                onSuccess={handlePaymentSuccess}
                onError={(msg) => {
                  setError(msg);
                  setStep('error');
                }}
              />
            </Elements>
          )}

          {step === 'processing' && <ProcessingStep />}

          {step === 'success' && (
            <SuccessStep email={email} onClose={onClose} />
          )}

          {step === 'error' && (
            <ErrorStep
              error={error}
              onRetry={() => {
                setError(null);
                setStep('email');
              }}
            />
          )}
        </div>

        {/* Footer — Security badge */}
        {(step === 'email' || step === 'payment') && (
          <div className="flex items-center justify-center gap-2 border-t border-slate-100 px-6 py-3 dark:border-slate-800">
            <Shield className="h-3.5 w-3.5 text-slate-400" />
            <span className="text-xs text-slate-400">
              Pago seguro vía Stripe · PCI DSS Level 1
            </span>
          </div>
        )}
      </div>
    </div>
  );
}

// =============================================================================
// STEP COMPONENTS
// =============================================================================

function EmailStep({
  email,
  setEmail,
  isAuthenticated,
  isLoading,
  onSubmit,
}: {
  email: string;
  setEmail: (v: string) => void;
  isAuthenticated: boolean;
  isLoading: boolean;
  onSubmit: (e: React.FormEvent) => void;
}) {
  return (
    <div className="space-y-5">
      {/* Product summary */}
      <div className="rounded-xl bg-emerald-50 p-4 dark:bg-emerald-950/30">
        <div className="flex items-center justify-between">
          <div>
            <p className="font-semibold text-emerald-800 dark:text-emerald-300">
              Informe completo
            </p>
            <p className="mt-0.5 text-xs text-emerald-600 dark:text-emerald-400">
              Análisis de precio + 7 dimensiones
            </p>
          </div>
          <div className="text-right">
            <p className="text-lg font-bold text-emerald-800 dark:text-emerald-300">
              RD${REPORT_PRICE_DOP}
            </p>
            <p className="text-xs text-emerald-600 dark:text-emerald-400">
              ≈ US${REPORT_PRICE_USD}
            </p>
          </div>
        </div>
      </div>

      {/* Features list */}
      <ul className="space-y-1.5 text-sm text-slate-600 dark:text-slate-400">
        <li className="flex items-center gap-2">
          <CheckCircle className="h-3.5 w-3.5 text-emerald-500" />
          Análisis de precio vs. mercado
        </li>
        <li className="flex items-center gap-2">
          <CheckCircle className="h-3.5 w-3.5 text-emerald-500" />
          Desglose de las 7 dimensiones
        </li>
        <li className="flex items-center gap-2">
          <CheckCircle className="h-3.5 w-3.5 text-emerald-500" />
          Historial NHTSA y recalls activos
        </li>
        <li className="flex items-center gap-2">
          <CheckCircle className="h-3.5 w-3.5 text-emerald-500" />
          Acceso permanente · Pago único
        </li>
      </ul>

      {/* Email form */}
      <form onSubmit={onSubmit} className="space-y-3">
        <div>
          <label htmlFor="buyer-email" className="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">
            <Mail className="mr-1 inline h-3.5 w-3.5" />
            {isAuthenticated ? 'Tu email (para el recibo)' : 'Email para el recibo'}
          </label>
          <input
            id="buyer-email"
            type="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="tu@email.com"
            disabled={isLoading}
            className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm shadow-sm transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20 disabled:opacity-60 dark:border-slate-700 dark:bg-slate-800 dark:text-white"
          />
          {!isAuthenticated && (
            <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">
              Si te registras después con este email, encontrarás el informe en tu historial.
            </p>
          )}
        </div>

        <button
          type="submit"
          disabled={isLoading || !email.includes('@')}
          className="flex w-full items-center justify-center gap-2 rounded-lg bg-emerald-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {isLoading ? (
            <>
              <Loader2 className="h-4 w-4 animate-spin" />
              Preparando pago...
            </>
          ) : (
            <>
              <Lock className="h-4 w-4" />
              Continuar al pago — RD${REPORT_PRICE_DOP}
            </>
          )}
        </button>
      </form>
    </div>
  );
}

function PaymentStep({
  onSuccess,
  onError,
}: {
  onSuccess: () => void;
  onError: (msg: string) => void;
}) {
  const stripe = useStripe();
  const elements = useElements();
  const [isProcessing, setIsProcessing] = React.useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!stripe || !elements) return;

    setIsProcessing(true);

    const { error } = await stripe.confirmPayment({
      elements,
      redirect: 'if_required',
      confirmParams: {
        return_url: `${window.location.origin}${window.location.pathname}`,
      },
    });

    if (error) {
      if (error.type === 'card_error' || error.type === 'validation_error') {
        onError(error.message || 'Error en la tarjeta');
      } else {
        onError('Error inesperado al procesar el pago');
      }
      setIsProcessing(false);
    } else {
      // Payment succeeded (no redirect needed)
      onSuccess();
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="rounded-lg border border-slate-200 p-3 dark:border-slate-700">
        <PaymentElement
          options={{
            layout: 'tabs',
          }}
        />
      </div>

      <button
        type="submit"
        disabled={!stripe || !elements || isProcessing}
        className="flex w-full items-center justify-center gap-2 rounded-lg bg-emerald-600 px-4 py-3 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-60"
      >
        {isProcessing ? (
          <>
            <Loader2 className="h-4 w-4 animate-spin" />
            Procesando pago...
          </>
        ) : (
          <>
            Pagar RD${REPORT_PRICE_DOP}
          </>
        )}
      </button>
    </form>
  );
}

function ProcessingStep() {
  return (
    <div className="space-y-4 py-6 text-center">
      <Loader2 className="mx-auto h-10 w-10 animate-spin text-emerald-500" />
      <div>
        <p className="font-semibold text-slate-900 dark:text-white">
          Confirmando tu compra...
        </p>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
          No cierres esta ventana
        </p>
      </div>
    </div>
  );
}

function SuccessStep({ email, onClose }: { email: string; onClose: () => void }) {
  return (
    <div className="space-y-5 py-4 text-center">
      <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-emerald-100 dark:bg-emerald-900/40">
        <CheckCircle className="h-8 w-8 text-emerald-600 dark:text-emerald-400" />
      </div>

      <div>
        <h3 className="text-lg font-bold text-slate-900 dark:text-white">
          ¡Informe desbloqueado!
        </h3>
        <p className="mt-1.5 text-sm text-slate-500 dark:text-slate-400">
          El análisis completo ya está visible abajo. Recibirás un recibo en{' '}
          <strong className="text-slate-700 dark:text-slate-300">{email}</strong>.
        </p>
      </div>

      <button
        onClick={onClose}
        className="w-full rounded-lg bg-emerald-600 px-4 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-emerald-700"
      >
        Ver informe completo
      </button>
    </div>
  );
}

function ErrorStep({ error, onRetry }: { error: string | null; onRetry: () => void }) {
  return (
    <div className="space-y-5 py-4 text-center">
      <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-red-100 dark:bg-red-900/40">
        <AlertTriangle className="h-7 w-7 text-red-600 dark:text-red-400" />
      </div>

      <div>
        <h3 className="text-lg font-bold text-slate-900 dark:text-white">
          Error en el pago
        </h3>
        <p className="mt-1.5 text-sm text-slate-500 dark:text-slate-400">
          {error || 'Hubo un problema al procesar tu pago. Por favor intenta de nuevo.'}
        </p>
      </div>

      <button
        onClick={onRetry}
        className="w-full rounded-lg border border-slate-300 bg-white px-4 py-2.5 text-sm font-semibold text-slate-700 transition-colors hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-800 dark:text-white dark:hover:bg-slate-700"
      >
        Intentar de nuevo
      </button>
    </div>
  );
}
