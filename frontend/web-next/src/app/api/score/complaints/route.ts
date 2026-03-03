import { NextRequest, NextResponse } from 'next/server';

// =============================================================================
// BFF: NHTSA Complaints Summary (FREE, no API key needed)
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
      `https://api.nhtsa.gov/complaints/complaintsByVehicle?make=${encodeURIComponent(make)}&model=${encodeURIComponent(model)}&modelYear=${year}`,
      { next: { revalidate: 3600 } }
    );

    if (!res.ok) {
      return NextResponse.json({
        success: true,
        data: { totalComplaints: 0, componentBreakdown: {} },
      });
    }

    const data = await res.json();
    const complaints = data.results || [];

    // Build component breakdown
    const componentBreakdown: Record<string, number> = {};
    for (const c of complaints) {
      const component = c.components || 'Unknown';
      componentBreakdown[component] = (componentBreakdown[component] || 0) + 1;
    }

    return NextResponse.json({
      success: true,
      data: {
        totalComplaints: complaints.length,
        componentBreakdown,
      },
    });
  } catch (error) {
    console.error('[NHTSA Complaints] Error:', error);
    return NextResponse.json({
      success: true,
      data: { totalComplaints: 0, componentBreakdown: {} },
    });
  }
}
