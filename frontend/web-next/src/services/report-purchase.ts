/**
 * Report Purchase Service
 *
 * Client-side service for managing OKLA Score™ report purchases.
 * Handles purchase state tracking (localStorage for persistent access),
 * and coordinates with server actions for payment processing.
 */

import {
  serverCreateReportPaymentIntent,
  serverConfirmReportPurchase,
  serverCheckReportPurchase,
} from '@/actions/report-purchase';
import { authTokens } from '@/lib/api-client';
import apiClient from '@/lib/api-client';

// =============================================================================
// TYPES
// =============================================================================

export interface ReportPurchaseState {
  vehicleId: string;
  purchaseId: string;
  buyerEmail: string;
  purchasedAt: string;
}

// =============================================================================
// LOCAL PURCHASE CACHE — Survives page reloads
// =============================================================================

const PURCHASE_KEY_PREFIX = 'okla_report_';

function getPurchaseKey(vehicleId: string): string {
  return `${PURCHASE_KEY_PREFIX}${vehicleId}`;
}

/**
 * Check if a report purchase exists locally (localStorage cache).
 */
export function isReportPurchasedLocally(vehicleId: string): boolean {
  if (typeof window === 'undefined') return false;
  try {
    const stored = localStorage.getItem(getPurchaseKey(vehicleId));
    return stored !== null;
  } catch {
    return false;
  }
}

/**
 * Save a successful purchase to local cache.
 */
export function saveReportPurchaseLocally(
  vehicleId: string,
  purchaseId: string,
  buyerEmail: string
): void {
  if (typeof window === 'undefined') return;
  try {
    const state: ReportPurchaseState = {
      vehicleId,
      purchaseId,
      buyerEmail,
      purchasedAt: new Date().toISOString(),
    };
    localStorage.setItem(getPurchaseKey(vehicleId), JSON.stringify(state));
  } catch {
    // localStorage not available
  }
}

/**
 * Get all locally cached report purchases.
 */
export function getAllLocalPurchases(): ReportPurchaseState[] {
  if (typeof window === 'undefined') return [];
  try {
    const purchases: ReportPurchaseState[] = [];
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (key?.startsWith(PURCHASE_KEY_PREFIX)) {
        const raw = localStorage.getItem(key);
        if (raw) {
          purchases.push(JSON.parse(raw) as ReportPurchaseState);
        }
      }
    }
    return purchases.sort(
      (a, b) => new Date(b.purchasedAt).getTime() - new Date(a.purchasedAt).getTime()
    );
  } catch {
    return [];
  }
}

// =============================================================================
// API-BACKED PURCHASES — Authenticated users
// =============================================================================

export interface ApiReportPurchase {
  purchaseId: string;
  vehicleId: string;
  productId: string;
  purchasedAt: string;
  amountCents: number;
  currency: string;
}

/**
 * Fetch all purchased reports from the API for the authenticated user.
 * Merges with locally cached purchases (deduplication by vehicleId).
 */
export async function getMyReports(): Promise<ReportPurchaseState[]> {
  try {
    const response = await apiClient.get<{ reports: ApiReportPurchase[] }>(
      '/api/billing/reports/my-reports'
    );
    const apiReports = (response.data.reports ?? []).map(r => ({
      vehicleId: r.vehicleId,
      purchaseId: r.purchaseId,
      buyerEmail: '',
      purchasedAt: r.purchasedAt,
    }));

    // Merge with local purchases (local may have guest purchases not yet linked)
    const local = getAllLocalPurchases();
    const seen = new Set(apiReports.map(r => r.vehicleId));
    const merged = [...apiReports, ...local.filter(l => !seen.has(l.vehicleId))];

    return merged.sort(
      (a, b) => new Date(b.purchasedAt).getTime() - new Date(a.purchasedAt).getTime()
    );
  } catch {
    // Fallback to local-only if API fails
    return getAllLocalPurchases();
  }
}

// =============================================================================
// PAYMENT FLOW
// =============================================================================

/**
 * Create a Stripe PaymentIntent for report purchase.
 */
export async function createReportPaymentIntent(vehicleId: string, buyerEmail: string) {
  const token = authTokens.getAccessToken();
  return serverCreateReportPaymentIntent(vehicleId, buyerEmail, token);
}

/**
 * Confirm purchase after successful Stripe payment.
 * Saves to local cache and triggers backend confirmation.
 */
export async function confirmReportPurchase(
  paymentIntentId: string,
  vehicleId: string,
  buyerEmail: string
) {
  const token = authTokens.getAccessToken();
  const result = await serverConfirmReportPurchase(paymentIntentId, vehicleId, buyerEmail, token);

  if (result.success && result.data?.purchaseId) {
    saveReportPurchaseLocally(vehicleId, result.data.purchaseId, buyerEmail);
  }

  return result;
}

/**
 * Check if a report has been purchased (API check).
 */
export async function checkReportPurchase(vehicleId: string, buyerEmail?: string) {
  const token = authTokens.getAccessToken();
  return serverCheckReportPurchase(vehicleId, buyerEmail, token);
}
