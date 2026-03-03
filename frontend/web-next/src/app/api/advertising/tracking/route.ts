import { NextRequest, NextResponse } from 'next/server';

// BFF pattern: server-side route uses INTERNAL_API_URL for Gateway
const API_URL =
  process.env.INTERNAL_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:18443';

/**
 * POST /api/advertising/tracking
 * Proxies impression/click tracking to backend AdvertisingService.
 * Accepts { type: 'impression' | 'click', ...payload }
 */
export async function POST(request: NextRequest) {
  try {
    const body = await request.json();
    const { type, ...payload } = body;

    const endpoint =
      type === 'click'
        ? `${API_URL}/api/advertising/tracking/click`
        : `${API_URL}/api/advertising/tracking/impression`;

    const res = await fetch(endpoint, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    });

    if (res.ok) {
      return NextResponse.json({ success: true });
    }

    return NextResponse.json(
      { success: false, error: 'Tracking failed' },
      { status: res.status }
    );
  } catch {
    // Tracking failures should not break the user experience
    return NextResponse.json({ success: true });
  }
}
