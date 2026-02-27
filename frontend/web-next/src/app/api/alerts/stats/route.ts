/**
 * BFF: GET /api/alerts/stats
 *
 * Aggregates data from AlertService's two separate list endpoints
 * (/api/pricealerts and /api/savedsearches) and returns the AlertStats
 * shape that the useAlertStats hook expects.
 *
 * The AlertService does not expose a dedicated /stats endpoint, so
 * this server-side route computes the aggregation. It intercepts the
 * browser request before the Next.js gateway rewrite fires (afterFiles).
 *
 * Shape returned:
 *   {
 *     totalPriceAlerts: number
 *     activePriceAlerts: number
 *     priceDropsThisMonth: number      ← alerts triggered this calendar month
 *     totalSavedSearches: number
 *     activeSavedSearches: number
 *     newMatchesThisWeek: number       ← searches notified within last 7 days
 *   }
 */

import { NextRequest, NextResponse } from 'next/server';

// BFF pattern: server-to-server via the internal Gateway URL
const INTERNAL_API_URL =
  process.env.INTERNAL_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:18443';

// ── Types matching PriceAlertDto / SavedSearchDto from AlertService ────────────

interface PriceAlertDto {
  id: string;
  vehicleId: string;
  targetPrice: number;
  condition: string;
  isActive: boolean;
  isTriggered: boolean;
  triggeredAt: string | null;
  createdAt: string;
  updatedAt: string;
}

interface SavedSearchDto {
  id: string;
  name: string;
  searchCriteria: string;
  sendEmailNotifications: boolean;
  frequency: string;
  lastNotificationSent: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

// ── Handler ───────────────────────────────────────────────────────────────────

export async function GET(request: NextRequest) {
  // Forward the user's auth token to the AlertService endpoints.
  // Support both Bearer header (legacy localStorage) and HttpOnly cookie auth.
  let authHeader = request.headers.get('authorization');

  // If no explicit Authorization header, extract token from the HttpOnly cookie
  // that the AuthService sets on login (okla_access_token).
  if (!authHeader) {
    const accessTokenCookie = request.cookies.get('okla_access_token');
    if (accessTokenCookie?.value) {
      authHeader = `Bearer ${accessTokenCookie.value}`;
    }
  }

  if (!authHeader) {
    return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
  }

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    Authorization: authHeader,
  };

  // Propagate cookie header too for additional context
  const cookieHeader = request.headers.get('cookie');
  if (cookieHeader) headers['Cookie'] = cookieHeader;

  // Fetch both lists in parallel — gracefully handle individual failures
  const [priceAlertsResult, savedSearchesResult] = await Promise.allSettled([
    fetch(`${INTERNAL_API_URL}/api/pricealerts`, {
      headers,
      signal: AbortSignal.timeout(8_000),
    }),
    fetch(`${INTERNAL_API_URL}/api/savedsearches`, {
      headers,
      signal: AbortSignal.timeout(8_000),
    }),
  ]);

  // ── Parse price alerts ─────────────────────────────────────────────────────
  let priceAlerts: PriceAlertDto[] = [];
  if (priceAlertsResult.status === 'fulfilled' && priceAlertsResult.value.ok) {
    try {
      const raw = await priceAlertsResult.value.json();
      // AlertService returns a plain array; guard against wrapped shapes
      priceAlerts = Array.isArray(raw) ? raw : (raw?.data ?? raw?.items ?? raw?.value ?? []);
    } catch {
      // ignore parse errors
    }
  }

  // ── Parse saved searches ───────────────────────────────────────────────────
  let savedSearches: SavedSearchDto[] = [];
  if (savedSearchesResult.status === 'fulfilled' && savedSearchesResult.value.ok) {
    try {
      const raw = await savedSearchesResult.value.json();
      savedSearches = Array.isArray(raw) ? raw : (raw?.data ?? raw?.items ?? raw?.value ?? []);
    } catch {
      // ignore parse errors
    }
  }

  // ── Compute stats ─────────────────────────────────────────────────────────
  const now = new Date();
  const startOfMonth = new Date(now.getFullYear(), now.getMonth(), 1);
  const oneWeekAgo = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1_000);

  // Alerts triggered (IsTriggered=true) within the current calendar month
  const priceDropsThisMonth = priceAlerts.filter(a => {
    if (!a.isTriggered || !a.triggeredAt) return false;
    return new Date(a.triggeredAt) >= startOfMonth;
  }).length;

  // Saved searches that sent a notification within the last 7 days
  const newMatchesThisWeek = savedSearches.filter(s => {
    if (!s.lastNotificationSent) return false;
    return new Date(s.lastNotificationSent) >= oneWeekAgo;
  }).length;

  const stats = {
    totalPriceAlerts: priceAlerts.length,
    activePriceAlerts: priceAlerts.filter(a => a.isActive).length,
    priceDropsThisMonth,
    totalSavedSearches: savedSearches.length,
    activeSavedSearches: savedSearches.filter(s => s.isActive).length,
    newMatchesThisWeek,
  };

  return NextResponse.json(stats, {
    headers: {
      // Short cache — counts can change after toggle operations
      'Cache-Control': 'private, max-age=30, s-maxage=0',
    },
  });
}
