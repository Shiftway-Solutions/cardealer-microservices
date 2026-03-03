import { NextRequest, NextResponse } from 'next/server';

/**
 * GET /api/advertising/rotation/[...path]
 * BFF catch-all route for ad rotation endpoints (FeaturedSpot, PremiumSpot, etc.)
 * Proxies to backend AdvertisingService with demo data fallback.
 *
 * Handles:
 *   /api/advertising/rotation/FeaturedSpot
 *   /api/advertising/rotation/PremiumSpot
 *   /api/advertising/rotation/config/:section
 *   /api/advertising/rotation/refresh
 */

const API_URL =
  process.env.INTERNAL_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:18443';

export async function GET(
  request: NextRequest,
  { params }: { params: Promise<{ path: string[] }> }
) {
  const { path } = await params;
  const section = path.join('/');

  try {
    const backendUrl = `${API_URL}/api/advertising/rotation/${section}${request.nextUrl.search}`;
    const res = await fetch(backendUrl, {
      headers: { Accept: 'application/json' },
      next: { revalidate: 300 },
    });

    if (res.ok) {
      const data = await res.json();
      return NextResponse.json(data);
    }
  } catch {
    // Backend unavailable — fall through to demo data
  }

  // Fallback: return demo rotation data for known sections
  const demoItems = getDemoRotationItems(section);
  return NextResponse.json({
    success: true,
    data: {
      items: demoItems,
      section,
      rotatedAt: new Date().toISOString(),
      source: 'demo',
    },
  });
}

export async function POST(
  request: NextRequest,
  { params }: { params: Promise<{ path: string[] }> }
) {
  const { path } = await params;
  const section = path.join('/');

  try {
    const body = await request.json().catch(() => null);
    const backendUrl = `${API_URL}/api/advertising/rotation/${section}${request.nextUrl.search}`;
    const res = await fetch(backendUrl, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: body ? JSON.stringify(body) : undefined,
    });

    if (res.ok) {
      const data = await res.json();
      return NextResponse.json(data);
    }
  } catch {
    // Backend unavailable
  }

  return NextResponse.json({ success: true, refreshed: false, source: 'demo' });
}

export async function PUT(
  request: NextRequest,
  { params }: { params: Promise<{ path: string[] }> }
) {
  const { path } = await params;
  const section = path.join('/');

  try {
    const body = await request.json();
    const backendUrl = `${API_URL}/api/advertising/rotation/${section}${request.nextUrl.search}`;
    const res = await fetch(backendUrl, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });

    if (res.ok) {
      const data = await res.json();
      return NextResponse.json(data);
    }
  } catch {
    // Backend unavailable
  }

  return NextResponse.json(
    { success: false, error: 'AdvertisingService unavailable' },
    { status: 503 }
  );
}

function getDemoRotationItems(section: string) {
  const featured = [
    {
      campaignId: 'demo-f1',
      vehicleId: 'demo-v1',
      vehicleTitle: '2023 Toyota RAV4 Limited AWD',
      vehicleImage: '/images/demo/rav4.jpg',
      vehiclePrice: 2850000,
      vehicleMake: 'Toyota',
      vehicleModel: 'RAV4',
      vehicleYear: 2023,
      dealerId: 'dealer-001',
      dealerName: 'AutoMax RD',
      placementType: section,
      position: 1,
    },
    {
      campaignId: 'demo-f2',
      vehicleId: 'demo-v2',
      vehicleTitle: '2022 Honda CR-V Touring',
      vehicleImage: '/images/demo/crv.jpg',
      vehiclePrice: 2450000,
      vehicleMake: 'Honda',
      vehicleModel: 'CR-V',
      vehicleYear: 2022,
      dealerId: 'dealer-002',
      dealerName: 'Honda Plaza',
      placementType: section,
      position: 2,
    },
    {
      campaignId: 'demo-f3',
      vehicleId: 'demo-v3',
      vehicleTitle: '2023 Hyundai Tucson SEL',
      vehicleImage: '/images/demo/tucson.jpg',
      vehiclePrice: 2150000,
      vehicleMake: 'Hyundai',
      vehicleModel: 'Tucson',
      vehicleYear: 2023,
      dealerId: 'dealer-003',
      dealerName: 'Hyundai Motor RD',
      placementType: section,
      position: 3,
    },
  ];

  const premium = [
    {
      campaignId: 'demo-p1',
      vehicleId: 'demo-v4',
      vehicleTitle: '2024 Mercedes-Benz GLC 300',
      vehicleImage: '/images/demo/glc.jpg',
      vehiclePrice: 4200000,
      vehicleMake: 'Mercedes-Benz',
      vehicleModel: 'GLC',
      vehicleYear: 2024,
      dealerId: 'dealer-004',
      dealerName: 'Star Motors RD',
      placementType: 'PremiumSpot',
      position: 1,
    },
    {
      campaignId: 'demo-p2',
      vehicleId: 'demo-v5',
      vehicleTitle: '2024 BMW X3 xDrive30i',
      vehicleImage: '/images/demo/x3.jpg',
      vehiclePrice: 3800000,
      vehicleMake: 'BMW',
      vehicleModel: 'X3',
      vehicleYear: 2024,
      dealerId: 'dealer-005',
      dealerName: 'Bavaria Motors',
      placementType: 'PremiumSpot',
      position: 2,
    },
    {
      campaignId: 'demo-p3',
      vehicleId: 'demo-v6',
      vehicleTitle: '2023 Audi Q5 Premium Plus',
      vehicleImage: '/images/demo/q5.jpg',
      vehiclePrice: 3500000,
      vehicleMake: 'Audi',
      vehicleModel: 'Q5',
      vehicleYear: 2023,
      dealerId: 'dealer-006',
      dealerName: 'Audi Center RD',
      placementType: 'PremiumSpot',
      position: 3,
    },
  ];

  if (section.includes('Premium')) {
    return premium;
  }
  return featured;
}
