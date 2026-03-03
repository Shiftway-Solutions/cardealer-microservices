import { NextRequest, NextResponse } from 'next/server';

// =============================================================================
// BFF: NHTSA Recalls by Vehicle (FREE, no API key needed)
// =============================================================================
// Docs: https://api.nhtsa.gov/
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
    const res = await fetch(
      `https://api.nhtsa.gov/recalls/recallsByVehicle?make=${encodeURIComponent(make)}&model=${encodeURIComponent(model)}&modelYear=${year}`,
      { next: { revalidate: 3600 } } // cache 1h
    );

    if (!res.ok) {
      return NextResponse.json(
        { success: false, error: `NHTSA Recalls API error: ${res.status}` },
        { status: 502 }
      );
    }

    const data = await res.json();
    const rawRecalls = data.results || [];

    const recalls = rawRecalls.map((r: Record<string, string>) => ({
      campaignNumber: r.NHTSACampaignNumber || '',
      component: r.Component || '',
      summary: r.Summary || '',
      consequence: r.Consequence || '',
      remedy: r.Remedy || '',
      reportReceivedDate: r.ReportReceivedDate || '',
      isResolved: false, // NHTSA doesn't track resolution per VIN
    }));

    return NextResponse.json({
      success: true,
      data: recalls,
      count: recalls.length,
    });
  } catch (error) {
    console.error('[NHTSA Recalls] Error:', error);
    return NextResponse.json({ success: false, error: 'Failed to fetch recalls' }, { status: 500 });
  }
}
