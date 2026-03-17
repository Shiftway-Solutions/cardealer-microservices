/**
 * GET /api/exchange-rate?from=USD&to=DOP
 *
 * Server-side exchange rate proxy — keeps external API calls off the browser,
 * avoids CSP issues, and allows server-side caching.
 *
 * Uses open.er-api.com (free tier, 1,500 req/month, no API key needed).
 * Override with EXCHANGE_RATE_API_KEY env var to use the authenticated tier.
 */

import { NextResponse } from 'next/server';

// Cache the rate for 1 hour (Next.js route handler caching)
export const revalidate = 3600;

const FALLBACK_USD_DOP = 62.5; // Approximate fallback if API is unavailable

export async function GET(request: Request) {
  const { searchParams } = new URL(request.url);
  const from = (searchParams.get('from') ?? 'USD').toUpperCase();
  const to = (searchParams.get('to') ?? 'DOP').toUpperCase();

  try {
    const apiKey = process.env.EXCHANGE_RATE_API_KEY;
    const url = apiKey
      ? `https://v6.exchangerate-api.com/v6/${apiKey}/pair/${from}/${to}`
      : `https://open.er-api.com/v6/latest/${from}`;

    const res = await fetch(url, {
      next: { revalidate: 3600 },
      headers: { Accept: 'application/json' },
    });

    if (!res.ok) {
      throw new Error(`Exchange rate API error: ${res.status}`);
    }

    const data = (await res.json()) as {
      result?: string;
      rates?: Record<string, number>;
      conversion_rate?: number;
    };

    let rate: number;

    if (apiKey && data.conversion_rate != null) {
      // Authenticated v6 pair response
      rate = data.conversion_rate;
    } else if (data.rates?.[to] != null) {
      // Free open.er-api response
      rate = data.rates[to]!;
    } else {
      throw new Error('Rate not found in response');
    }

    return NextResponse.json(
      { from, to, rate, source: 'live', updatedAt: new Date().toISOString() },
      {
        headers: {
          'Cache-Control': 'public, s-maxage=3600, stale-while-revalidate=300',
        },
      }
    );
  } catch (err) {
    console.error('[exchange-rate] Failed to fetch rate, using fallback:', err);

    return NextResponse.json(
      {
        from,
        to,
        rate: from === 'USD' && to === 'DOP' ? FALLBACK_USD_DOP : 1,
        source: 'fallback',
        updatedAt: new Date().toISOString(),
      },
      {
        headers: {
          'Cache-Control': 'public, s-maxage=300, stale-while-revalidate=60',
        },
      }
    );
  }
}
