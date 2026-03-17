/**
 * PayPalPaymentButton — PayPal Smart Buttons for checkout
 *
 * Uses the official @paypal/react-paypal-js SDK.
 * Flow:
 *   1. Backend creates a PayPal order via POST /api/payments/paypal/create-order
 *   2. User approves in PayPal popup
 *   3. Backend captures the order via POST /api/payments/paypal/capture
 *   4. Frontend receives confirmation and redirects to success page
 */

'use client';

import * as React from 'react';
import { PayPalScriptProvider, PayPalButtons, FUNDING } from '@paypal/react-paypal-js';
import { Shield, AlertCircle } from 'lucide-react';

// =============================================================================
// TYPES
// =============================================================================

interface PayPalPaymentButtonProps {
  /** PayPal client ID (public key) */
  clientId: string;
  /** Total amount in the checkout currency */
  amount: string;
  /** Currency code (e.g., 'USD', 'DOP') */
  currency: string;
  /** Called to create the order on the backend — should return the PayPal order ID */
  onCreateOrder: () => Promise<string>;
  /** Called after PayPal approves the payment — receives the order ID */
  onApprove: (orderId: string) => Promise<void>;
  /** Called on error */
  onError: (message: string) => void;
  /** Called if user cancels the PayPal popup */
  onCancel?: () => void;
  /** Whether the button is disabled */
  disabled?: boolean;
}

// =============================================================================
// PAYPAL BUTTONS INNER (must be inside PayPalScriptProvider)
// =============================================================================

function PayPalButtonsInner({
  onCreateOrder,
  onApprove,
  onError,
  onCancel,
  disabled,
}: Pick<
  PayPalPaymentButtonProps,
  'onCreateOrder' | 'onApprove' | 'onError' | 'onCancel' | 'disabled'
>) {
  return (
    <div className="space-y-4">
      <PayPalButtons
        style={{
          layout: 'vertical',
          color: 'blue',
          shape: 'rect',
          label: 'pay',
          height: 48,
        }}
        fundingSource={FUNDING.PAYPAL}
        disabled={disabled}
        createOrder={async () => {
          try {
            const orderId = await onCreateOrder();
            return orderId;
          } catch {
            onError('Error al crear la orden de PayPal');
            throw new Error('Failed to create order');
          }
        }}
        onApprove={async data => {
          try {
            await onApprove(data.orderID);
          } catch {
            onError('Error al capturar el pago de PayPal');
          }
        }}
        onError={err => {
          const message = err instanceof Error ? err.message : 'Error inesperado con PayPal';
          onError(message);
        }}
        onCancel={() => {
          onCancel?.();
        }}
      />

      {/* Also show debit/credit card option through PayPal */}
      <PayPalButtons
        style={{
          layout: 'vertical',
          color: 'black',
          shape: 'rect',
          label: 'pay',
          height: 48,
        }}
        fundingSource={FUNDING.CARD}
        disabled={disabled}
        createOrder={async () => {
          try {
            const orderId = await onCreateOrder();
            return orderId;
          } catch {
            onError('Error al crear la orden');
            throw new Error('Failed to create order');
          }
        }}
        onApprove={async data => {
          try {
            await onApprove(data.orderID);
          } catch {
            onError('Error al capturar el pago');
          }
        }}
        onError={err => {
          const message =
            err instanceof Error ? err.message : 'Error inesperado al procesar el pago';
          onError(message);
        }}
        onCancel={() => {
          onCancel?.();
        }}
      />

      <div className="text-muted-foreground flex items-center gap-2 text-xs">
        <Shield className="text-primary h-4 w-4 shrink-0" />
        <span>Pago procesado de forma segura por PayPal. Protección al comprador incluida.</span>
      </div>
    </div>
  );
}

// =============================================================================
// WRAPPER WITH PAYPAL SCRIPT PROVIDER
// =============================================================================

export function PayPalPaymentButton({
  clientId,
  amount: _amount,
  currency,
  onCreateOrder,
  onApprove,
  onError,
  onCancel,
  disabled,
}: PayPalPaymentButtonProps) {
  if (!clientId) {
    return (
      <div className="flex items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
        <AlertCircle className="h-5 w-5 shrink-0" />
        <p>PayPal no está configurado. Contacta al administrador.</p>
      </div>
    );
  }

  return (
    <PayPalScriptProvider
      options={{
        clientId,
        currency: currency === 'DOP' ? 'USD' : currency,
        intent: 'capture',
        locale: 'es_DO',
      }}
    >
      <PayPalButtonsInner
        onCreateOrder={onCreateOrder}
        onApprove={onApprove}
        onError={onError}
        onCancel={onCancel}
        disabled={disabled}
      />
    </PayPalScriptProvider>
  );
}
