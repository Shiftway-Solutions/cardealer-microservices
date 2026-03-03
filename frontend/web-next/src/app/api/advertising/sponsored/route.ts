import { NextRequest, NextResponse } from 'next/server';
import { generateSponsoredVehiclesForSlot } from '@/lib/ad-engine';
import type { AdSlotPosition } from '@/types/ads';

/**
 * GET /api/advertising/sponsored
 * Returns sponsored vehicles for a given ad slot position.
 * In production, this proxies to the ad server backend.
 * Currently serves demo data from the local ad engine.
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
    // In production: proxy to backend ad server
    // const backendUrl = `${process.env.GATEWAY_URL}/api/advertising/sponsored?${searchParams.toString()}`;
    // const res = await fetch(backendUrl);
    // const data = await res.json();
    // return NextResponse.json(data);

    // For now: generate demo sponsored vehicles
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
      },
    });
  } catch {
    return NextResponse.json(
      { success: false, error: 'Failed to fetch sponsored vehicles' },
      { status: 500 }
    );
  }
}
