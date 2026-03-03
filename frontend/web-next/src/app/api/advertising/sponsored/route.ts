import { NextRequest, NextResponse } from 'next/server';
import { generateSponsoredVehiclesForSlot } from '@/lib/ad-engine';
import type { AdSlotPosition } from '@/types/ads';

// BFF pattern: server-side route uses INTERNAL_API_URL for Gateway
const API_URL =
  process.env.INTERNAL_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:18443';

/**
 * GET /api/advertising/sponsored
 * Returns sponsored vehicles for a given ad slot position.
 * Proxies to the backend ad server, with client-side ad engine as fallback.
 */
export async function GET(request: NextRequest) {
  const { searchParams } = new URL(request.url);
  const slot = searchParams.get('slot') as AdSlotPosition | null;
  const count = searchParams.get('count');

  if (!slot) {
    return NextResponse.json(
      { success: false, error: 'Missing required parameter: slot' },
      { status: 400 }
    );
  }

  try {
    // Try backend first
    const backendUrl = `${API_URL}/api/advertising/rotation/${slot}`;
    const res = await fetch(backendUrl, {
      headers: { Accept: 'application/json' },
      next: { revalidate: 300 }, // cache 5 min
    });

    if (res.ok) {
      const data = await res.json();
      return NextResponse.json({
        success: true,
        data: data.data?.items || data.data || [],
        meta: {
          slot,
          count: (data.data?.items || data.data || []).length,
          auctionTimestamp: new Date().toISOString(),
          source: 'backend',
        },
      });
    }
  } catch {
    // Backend unavailable — fall through to local engine
  }

  // Fallback: generate demo sponsored vehicles from client-side engine
  try {
    const vehicles = generateSponsoredVehiclesForSlot(
      slot,
      count ? parseInt(count, 10) : undefined
    );

    return NextResponse.json({
      success: true,
      data: vehicles,
      meta: {
        slot,
        count: vehicles.length,
        auctionTimestamp: new Date().toISOString(),
        source: 'demo',
      },
    });
  } catch {
    return NextResponse.json(
      { success: false, error: 'Failed to fetch sponsored vehicles' },
      { status: 500 }
    );
  }
}
