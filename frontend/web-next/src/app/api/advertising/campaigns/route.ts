import { NextRequest, NextResponse } from 'next/server';

// BFF pattern: server-side route uses INTERNAL_API_URL for Gateway
const API_URL =
  process.env.INTERNAL_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:18443';

/**
 * POST /api/advertising/campaigns
 * Proxies campaign creation to backend AdvertisingService.
 */
export async function POST(request: NextRequest) {
  try {
    const body = await request.json();

    const res = await fetch(`${API_URL}/api/advertising/campaigns`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(request.headers.get('authorization')
          ? { Authorization: request.headers.get('authorization')! }
          : {}),
      },
      body: JSON.stringify(body),
    });

    const data = await res.json();
    return NextResponse.json(data, { status: res.status });
  } catch {
    return NextResponse.json(
      { success: false, error: 'Failed to create campaign' },
      { status: 502 }
    );
  }
}

/**
 * GET /api/advertising/campaigns?ownerId=...&ownerType=...&status=...&page=...&pageSize=...
 * Proxies campaign listing to backend.
 */
export async function GET(request: NextRequest) {
  try {
    const { searchParams } = new URL(request.url);
    const ownerId = searchParams.get('ownerId');

    if (!ownerId) {
      return NextResponse.json(
        { success: false, error: 'Missing ownerId' },
        { status: 400 }
      );
    }

    const ownerType = searchParams.get('ownerType') || 'Individual';
    const status = searchParams.get('status') || '';
    const page = searchParams.get('page') || '1';
    const pageSize = searchParams.get('pageSize') || '20';

    const params = new URLSearchParams({ ownerType, page, pageSize });
    if (status) params.set('status', status);

    const res = await fetch(
      `${API_URL}/api/advertising/campaigns/owner/${ownerId}?${params.toString()}`,
      {
        headers: {
          Accept: 'application/json',
          ...(request.headers.get('authorization')
            ? { Authorization: request.headers.get('authorization')! }
            : {}),
        },
      }
    );

    const data = await res.json();
    return NextResponse.json(data, { status: res.status });
  } catch {
    return NextResponse.json(
      { success: false, error: 'Failed to fetch campaigns' },
      { status: 502 }
    );
  }
}
