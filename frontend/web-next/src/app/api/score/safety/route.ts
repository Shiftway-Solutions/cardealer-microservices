import { NextRequest, NextResponse } from 'next/server';

// =============================================================================
// BFF: NHTSA Safety Ratings (FREE, no API key needed)
// =============================================================================
// Docs: https://api.nhtsa.gov/SafetyRatings
// =============================================================================

export async function GET(request: NextRequest) {
  const make = request.nextUrl.searchParams.get('make');
  const model = request.nextUrl.searchParams.get('model');
  const year = request.nextUrl.searchParams.get('year');

  if (!make || !model || !year) {
    return NextResponse.json(
      { success: false, error: 'make, model, and year are required' },
      { status: 400 }
    );
  }

  try {
    // First, get the vehicle ID
    const searchRes = await fetch(
      `https://api.nhtsa.gov/SafetyRatings/modelyear/${year}/make/${encodeURIComponent(make)}/model/${encodeURIComponent(model)}?format=json`,
      { next: { revalidate: 86400 } } // cache 24h
    );

    if (!searchRes.ok) {
      return NextResponse.json({ success: true, data: null }); // No ratings found
    }

    const searchData = await searchRes.json();
    const vehicles = searchData.Results || [];

    if (vehicles.length === 0) {
      return NextResponse.json({ success: true, data: null });
    }

    // Get the first vehicle's detailed ratings
    const vehicleId = vehicles[0].VehicleId;
    const ratingRes = await fetch(
      `https://api.nhtsa.gov/SafetyRatings/VehicleId/${vehicleId}?format=json`,
      { next: { revalidate: 86400 } }
    );

    if (!ratingRes.ok) {
      return NextResponse.json({ success: true, data: null });
    }

    const ratingData = await ratingRes.json();
    const r = ratingData.Results?.[0];

    if (!r) {
      return NextResponse.json({ success: true, data: null });
    }

    const parseRating = (val: string | number | undefined): number | undefined => {
      if (!val || val === 'Not Rated') return undefined;
      const n = typeof val === 'string' ? parseInt(val, 10) : val;
      return isNaN(n) ? undefined : n;
    };

    const safetyRating = {
      overallRating: parseRating(r.OverallRating) || 0,
      frontalCrashRating: parseRating(r.OverallFrontCrashRating),
      sideCrashRating: parseRating(r.OverallSideCrashRating),
      rolloverRating: parseRating(r.RolloverRating),
    };

    return NextResponse.json({ success: true, data: safetyRating });
  } catch (error) {
    console.error('[NHTSA Safety] Error:', error);
    return NextResponse.json(
      { success: false, error: 'Failed to fetch safety ratings' },
      { status: 500 }
    );
  }
}
