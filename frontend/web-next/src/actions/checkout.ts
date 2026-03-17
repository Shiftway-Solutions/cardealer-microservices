'use server';

/**
 * Payment/Checkout Server Actions — Critical payment operations
 *
 * These run exclusively on the Next.js server. The browser only sees
 * an opaque POST — no payment endpoints, amounts, card tokens,
 * or session IDs are visible in DevTools Network tab.
 *
 * Flow: Browser → Server Action (Next.js) → Gateway (internal) → BillingService/PaymentService
 */

import { cookies } from 'next/headers';
import { getInternalApiUrl } from '@/lib/api-url';

// =============================================================================
// TYPES
// =============================================================================

export interface ActionResult<T = void> {
  success: boolean;
  data?: T;
  error?: string;
  code?: string;
}

interface CheckoutSessionResult {
  sessionId: string;
  productId: string;
  subtotal: number;
  tax: number;
  total: number;
  currency: 'DOP' | 'USD';
  status: string;
  paymentUrl?: string;
}

interface PaymentIntentResult {
  id: string;
  clientSecret: string;
  amount: number;
  currency: 'DOP' | 'USD';
  status: string;
}

interface ProcessPaymentResult {
  orderId?: string;
  transactionId?: string;
  receiptUrl?: string;
}

interface PromoCodeResult {
  valid: boolean;
  discountType: 'percentage' | 'fixed';
  discountValue: number;
  discountAmount: number;
  newTotal: number;
  errorMessage?: string;
}

// =============================================================================
// INTERNAL HELPERS
// =============================================================================

const API_URL = () => getInternalApiUrl();

async function internalFetch<T>(
  path: string,
  options: {
    method?: string;
    body?: unknown;
    token?: string | null;
  } = {}
): Promise<T> {
  const { method = 'GET', body, token } = options;
  const url = `${API_URL()}${path}`;

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
  };

  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  // SECURITY: Add CSRF token from cookie for mutations (POST, PUT, PATCH, DELETE)
  // Gateway's CsrfValidationMiddleware uses Double Submit Cookie pattern:
  // it checks BOTH X-CSRF-Token header AND csrf_token cookie in the same request.
  if (method && ['POST', 'PUT', 'PATCH', 'DELETE'].includes(method.toUpperCase())) {
    const cookieStore = await cookies();
    const csrfToken = cookieStore.get('csrf_token')?.value;
    if (csrfToken) {
      headers['X-CSRF-Token'] = csrfToken;
      headers['Cookie'] = `csrf_token=${csrfToken}`;
    }
  }

  const response = await fetch(url, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
    cache: 'no-store',
  });

  if (!response.ok) {
    const errorBody = await response.json().catch(() => ({}));
    const message =
      errorBody.message || errorBody.error || errorBody.title || `Error ${response.status}`;
    throw new Error(message);
  }

  if (response.status === 204) {
    return {} as T;
  }

  return response.json();
}

// =============================================================================
// CHECKOUT ACTIONS
// =============================================================================

/**
 * Create checkout session — server-side
 * Browser NEVER sees: /api/checkout/sessions, product details, pricing
 */
export async function serverCreateCheckoutSession(
  productId: string,
  paymentMethod: string,
  accessToken: string,
  vehicleId?: string,
  dealerId?: string,
  promoCode?: string,
  returnUrl?: string,
  cancelUrl?: string
): Promise<ActionResult<CheckoutSessionResult>> {
  try {
    const response = await internalFetch<CheckoutSessionResult>('/api/checkout/sessions', {
      method: 'POST',
      body: {
        productId,
        paymentMethod,
        vehicleId,
        dealerId,
        promoCode,
        returnUrl,
        cancelUrl,
      },
      token: accessToken,
    });

    return { success: true, data: response };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Error al crear la sesión de pago',
      code: 'CHECKOUT_CREATE_FAILED',
    };
  }
}

/**
 * Create payment intent (Stripe) — server-side
 * Browser NEVER sees: /api/checkout/.../payment-intent, client secret
 */
export async function serverCreatePaymentIntent(
  sessionId: string,
  accessToken: string
): Promise<ActionResult<PaymentIntentResult>> {
  try {
    const response = await internalFetch<PaymentIntentResult>(
      `/api/checkout/sessions/${sessionId}/payment-intent`,
      {
        method: 'POST',
        token: accessToken,
      }
    );

    return { success: true, data: response };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Error al crear el intento de pago',
      code: 'PAYMENT_INTENT_FAILED',
    };
  }
}

/**
 * Process payment — server-side
 * Browser NEVER sees: /api/checkout/process-payment, card tokens, transaction IDs
 */
export async function serverProcessPayment(
  sessionId: string,
  accessToken: string,
  paymentMethodId?: string,
  cardToken?: string
): Promise<ActionResult<ProcessPaymentResult>> {
  try {
    const response = await internalFetch<{
      success: boolean;
      orderId?: string;
      transactionId?: string;
      receiptUrl?: string;
      error?: string;
    }>('/api/checkout/process-payment', {
      method: 'POST',
      body: { sessionId, paymentMethodId, cardToken },
      token: accessToken,
    });

    if (!response.success) {
      return {
        success: false,
        error: response.error || 'Error al procesar el pago',
        code: 'PAYMENT_PROCESSING_FAILED',
      };
    }

    return {
      success: true,
      data: {
        orderId: response.orderId,
        transactionId: response.transactionId,
        receiptUrl: response.receiptUrl,
      },
    };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Error al procesar el pago',
      code: 'PAYMENT_PROCESSING_FAILED',
    };
  }
}

/**
 * Validate promo code — server-side
 * Browser NEVER sees: /api/checkout/validate-promo, discount details
 */
export async function serverValidatePromoCode(
  code: string,
  productId: string,
  accessToken: string
): Promise<ActionResult<PromoCodeResult>> {
  try {
    const response = await internalFetch<PromoCodeResult>('/api/checkout/validate-promo', {
      method: 'POST',
      body: { code, productId },
      token: accessToken,
    });

    return { success: true, data: response };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      data: {
        valid: false,
        discountType: 'percentage',
        discountValue: 0,
        discountAmount: 0,
        newTotal: 0,
        errorMessage: err.message || 'Código promocional no válido',
      },
    };
  }
}

// =============================================================================
// PAYPAL SERVER ACTIONS
// =============================================================================

interface PayPalOrderResult {
  orderId: string;
  status: string;
  approvalUrl?: string;
}

interface PayPalCaptureResult {
  orderId: string;
  captureId: string;
  status: string;
  amount: number;
  currency: string;
}

/**
 * Create PayPal order — server-side
 * Browser NEVER sees: /api/payments/paypal/create-order, PayPal credentials
 */
export async function serverCreatePayPalOrder(
  amount: number,
  currency: string,
  description: string,
  accessToken: string,
  returnUrl?: string,
  cancelUrl?: string
): Promise<ActionResult<PayPalOrderResult>> {
  try {
    const response = await internalFetch<PayPalOrderResult>('/api/payments/paypal/create-order', {
      method: 'POST',
      body: {
        amount,
        currency,
        description,
        returnUrl,
        cancelUrl,
      },
      token: accessToken,
    });

    return { success: true, data: response };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Error al crear la orden de PayPal',
      code: 'PAYPAL_CREATE_ORDER_FAILED',
    };
  }
}

/**
 * Capture PayPal order — server-side
 * Browser NEVER sees: /api/payments/paypal/capture, capture details
 */
export async function serverCapturePayPalOrder(
  orderId: string,
  accessToken: string
): Promise<ActionResult<PayPalCaptureResult>> {
  try {
    const response = await internalFetch<PayPalCaptureResult>('/api/payments/paypal/capture', {
      method: 'POST',
      body: { orderId },
      token: accessToken,
    });

    return { success: true, data: response };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Error al capturar el pago de PayPal',
      code: 'PAYPAL_CAPTURE_FAILED',
    };
  }
}
