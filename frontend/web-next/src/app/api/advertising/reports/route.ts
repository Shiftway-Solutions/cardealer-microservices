import { NextRequest, NextResponse } from 'next/server';

// BFF pattern: server-side route uses INTERNAL_API_URL for Gateway
const API_URL =
  process.env.INTERNAL_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:18443';

/**
 * GET /api/advertising/reports?type=platform|campaign|owner&id=...&daysBack=30
 * Proxies report requests to backend AdvertisingService.
 */
export async function GET(request: NextRequest) {
  try {
    const { searchParams } = new URL(request.url);
    const type = searchParams.get('type') || 'platform';
    const id = searchParams.get('id') || '';
    const ownerType = searchParams.get('ownerType') || 'Individual';
    const daysBack = searchParams.get('daysBack') || '30';

    let endpoint: string;

    switch (type) {
      case 'campaign':
        if (!id) {
          return NextResponse.json(
            { success: false, error: 'Missing campaign id' },
            { status: 400 }
          );
        }
        endpoint = `${API_URL}/api/advertising/reports/campaign/${id}?daysBack=${daysBack}`;
        break;
      case 'owner':
        if (!id) {
          return NextResponse.json({ success: false, error: 'Missing owner id' }, { status: 400 });
        }
        endpoint = `${API_URL}/api/advertising/reports/owner/${id}?ownerType=${ownerType}&daysBack=${daysBack}`;
        break;
      case 'platform':
      default:
        endpoint = `${API_URL}/api/advertising/reports/platform?daysBack=${daysBack}`;
        break;
    }

    const res = await fetch(endpoint, {
      headers: {
        Accept: 'application/json',
        ...(request.headers.get('authorization')
          ? { Authorization: request.headers.get('authorization')! }
          : {}),
      },
      next: { revalidate: 300 }, // cache 5 min
    });

    const data = await res.json();
    return NextResponse.json(data, { status: res.status });
  } catch {
    return NextResponse.json({ success: false, error: 'Failed to fetch reports' }, { status: 502 });
  }
}
