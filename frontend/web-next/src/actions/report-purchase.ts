'use server';

/**
 * Report Purchase Server Actions
 *
 * Creates Stripe PaymentIntents for OKLA Score™ report purchases.
 * Supports both authenticated users and guest buyers (email-keyed).
 * The browser never sees the Stripe secret key or payment intent creation details.
 *
 * Flow: Browser → Server Action → Gateway → BillingService
 */

import { cookies } from 'next/headers';
import { getInternalApiUrl } from '@/lib/api-url';

// =============================================================================
// TYPES
// =============================================================================

export interface ReportPaymentIntentResult {
  clientSecret: string;
  paymentIntentId: string;
  amount: number;
  currency: string;
}

export interface ReportPurchaseResult {
  success: boolean;
  purchaseId?: string;
  error?: string;
}

export interface ActionResult<T = void> {
  success: boolean;
  data?: T;
  error?: string;
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

  // CSRF protection for mutations
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
// SERVER ACTIONS
// =============================================================================

/**
 * Create a PaymentIntent for an OKLA Score™ report purchase.
 * Supports guest purchases (email-keyed) — no auth required.
 *
 * @param vehicleId - The vehicle ID the report is for
 * @param buyerEmail - Email for receipt and purchase linking
 * @param accessToken - Optional; included when buyer is authenticated
 */
export async function serverCreateReportPaymentIntent(
  vehicleId: string,
  buyerEmail: string,
  accessToken?: string | null
): Promise<ActionResult<ReportPaymentIntentResult>> {
  try {
    const result = await internalFetch<ReportPaymentIntentResult>(
      '/api/billing/reports/payment-intent',
      {
        method: 'POST',
        body: {
          vehicleId,
          buyerEmail,
          productId: 'okla-score-report',
        },
        token: accessToken,
      }
    );

    return { success: true, data: result };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Error al crear el intento de pago',
    };
  }
}

/**
 * Confirm a report purchase after successful Stripe payment.
 * Records the purchase in BillingService and triggers receipt email.
 *
 * @param paymentIntentId - Stripe PaymentIntent ID
 * @param vehicleId - The vehicle the report is for
 * @param buyerEmail - Email for receipt and purchase linking
 * @param accessToken - Optional
 */
export async function serverConfirmReportPurchase(
  paymentIntentId: string,
  vehicleId: string,
  buyerEmail: string,
  accessToken?: string | null
): Promise<ActionResult<ReportPurchaseResult>> {
  try {
    const result = await internalFetch<ReportPurchaseResult>(
      '/api/billing/reports/confirm-purchase',
      {
        method: 'POST',
        body: {
          paymentIntentId,
          vehicleId,
          buyerEmail,
          productId: 'okla-score-report',
        },
        token: accessToken,
      }
    );

    return { success: true, data: result };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Error al confirmar la compra',
    };
  }
}

/**
 * Check if a report has been purchased for a vehicle.
 * Works by email (guest) or by auth token (registered user).
 */
export async function serverCheckReportPurchase(
  vehicleId: string,
  buyerEmail?: string,
  accessToken?: string | null
): Promise<ActionResult<{ purchased: boolean; purchaseId?: string }>> {
  try {
    const params = new URLSearchParams({ vehicleId });
    if (buyerEmail) params.set('email', buyerEmail);

    const result = await internalFetch<{ purchased: boolean; purchaseId?: string }>(
      `/api/billing/reports/check-purchase?${params.toString()}`,
      { token: accessToken }
    );

    return { success: true, data: result };
  } catch {
    // If the endpoint doesn't exist yet, assume not purchased
    return { success: true, data: { purchased: false } };
  }
}
