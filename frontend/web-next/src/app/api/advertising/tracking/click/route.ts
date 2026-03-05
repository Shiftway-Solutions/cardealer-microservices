import { NextRequest, NextResponse } from 'next/server';

/**
 * POST /api/advertising/tracking/click
 * BFF route for recording ad clicks.
 * Proxies to backend AdvertisingService, silently fails if unavailable.
 */

const API_URL =
  process.env.INTERNAL_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:18443';

export async function POST(request: NextRequest) {
  try {
    const body = await request.json();
    // Security Note: Server-to-server call (Next.js BFF → Gateway).
    // CSRF is validated at the browser → Next.js boundary, not here.
    // Tracking is fire-and-forget; non-critical.
    const res = await fetch(`${API_URL}/api/advertising/tracking/click`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });

    if (res.ok) {
      const data = await res.json();
      return NextResponse.json(data);
    }
  } catch {
    // Tracking is non-critical — silently accept
  }

  return NextResponse.json({ success: true, tracked: false });
}
