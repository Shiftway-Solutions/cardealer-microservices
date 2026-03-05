import { NextRequest, NextResponse } from 'next/server';

/**
 * GET /api/advertising/rotation
 * BFF route for ad rotation endpoints.
 * Expects query param: ?section=FeaturedSpot (or PremiumSpot, etc.)
 * Also handles direct path: /api/advertising/rotation?section=FeaturedSpot
 *
 * The frontend advertising service calls /api/advertising/rotation/:section
 * but Next.js afterFiles rewrites catch dynamic segments before API routes.
 * The middleware rewrites /api/advertising/rotation/:section to
 * /api/advertising/rotation?section=:section to hit this static route.
 */

const API_URL =
  process.env.INTERNAL_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:18443';

export async function GET(request: NextRequest) {
  const section = request.nextUrl.searchParams.get('section') || 'FeaturedSpot';
  const subpath = request.nextUrl.searchParams.get('subpath') || '';
  const fullPath = subpath ? `${section}/${subpath}` : section;

  try {
    const backendUrl = `${API_URL}/api/advertising/rotation/${fullPath}`;
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

  // Fallback: return demo rotation data
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

export async function POST(request: NextRequest) {
  const section = request.nextUrl.searchParams.get('section') || '';
  const subpath = request.nextUrl.searchParams.get('subpath') || '';
  const fullPath = subpath ? `${section}/${subpath}` : section;

  try {
    // Security Note: Server-to-server call (Next.js BFF → Gateway).
    // CSRF is validated at the browser → Next.js boundary, not here.
    const body = await request.json().catch(() => null);
    const res = await fetch(`${API_URL}/api/advertising/rotation/${fullPath}`, {
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

export async function PUT(request: NextRequest) {
  const section = request.nextUrl.searchParams.get('section') || '';
  const subpath = request.nextUrl.searchParams.get('subpath') || '';
  const fullPath = subpath ? `${section}/${subpath}` : section;

  try {
    const body = await request.json();
    const res = await fetch(`${API_URL}/api/advertising/rotation/${fullPath}`, {
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
  // Real vehicle data from the OKLA database — shown as demo advertising
  // until AdvertisingService is scaled up and campaigns are created.
  // Fields match the RotatedVehicle interface: title, imageUrl, price, etc.
  const featured = [
    {
      campaignId: 'demo-f1',
      vehicleId: '94887983-8bdf-40fb-80fe-bf1465498124',
      title: '2024 Toyota RAV4',
      slug: '2024-toyota-rav4-94887983',
      imageUrl: 'https://images.unsplash.com/photo-1619767886558-efdc259cde1a?w=800&q=75',
      price: 2850000,
      currency: 'DOP',
      qualityScore: 95,
      location: 'Santo Domingo',
      isFeatured: true,
      placementType: section,
      position: 1,
    },
    {
      campaignId: 'demo-f2',
      vehicleId: '3778c87b-b1e3-4be0-a40f-362dbfcee262',
      title: '2023 Toyota Camry',
      slug: '2023-toyota-camry-3778c87b',
      imageUrl: 'https://images.unsplash.com/photo-1549317661-bd32c8ce0db2?w=800&q=75',
      price: 2800000,
      currency: 'DOP',
      qualityScore: 92,
      location: 'Santiago',
      isFeatured: true,
      placementType: section,
      position: 2,
    },
    {
      campaignId: 'demo-f3',
      vehicleId: '839fdf79-3887-4703-89ce-f3db75cde273',
      title: '2023 Honda CR-V',
      slug: '2023-honda-cr-v-839fdf79',
      imageUrl: 'https://images.unsplash.com/photo-1606611013016-969c19ba27c5?w=800&q=75',
      price: 1950000,
      currency: 'DOP',
      qualityScore: 90,
      location: 'Santo Domingo Este',
      isFeatured: true,
      placementType: section,
      position: 3,
    },
  ];

  const premium = [
    {
      campaignId: 'demo-p1',
      vehicleId: '3a600ca0-72a8-4269-a3cb-30d27fc1cede',
      title: '2022 Toyota Corolla',
      slug: '2022-toyota-corolla-3a600ca0',
      imageUrl: 'https://images.unsplash.com/photo-1549317661-bd32c8ce0db2?w=1200&q=80',
      price: 1850000,
      currency: 'DOP',
      qualityScore: 88,
      location: 'Santo Domingo',
      isPremium: true,
      placementType: 'PremiumSpot',
      position: 1,
    },
    {
      campaignId: 'demo-p2',
      vehicleId: '78a42948-04d8-4e40-b168-46d518a89740',
      title: '2021 Toyota Hilux',
      slug: '2021-toyota-hilux-78a42948',
      imageUrl: 'https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=800&q=75',
      price: 2100000,
      currency: 'DOP',
      qualityScore: 85,
      location: 'La Vega',
      isPremium: true,
      placementType: 'PremiumSpot',
      position: 2,
    },
    {
      campaignId: 'demo-p3',
      vehicleId: '42e82cbc-c6c9-4da2-b801-7931362cbb3d',
      title: '2023 Honda CR-V Touring',
      slug: '2023-honda-cr-v-touring-42e82cbc',
      imageUrl: 'https://images.unsplash.com/photo-1606611013016-969c19ba27bb?w=800&q=75',
      price: 2850000,
      currency: 'DOP',
      qualityScore: 93,
      location: 'Punta Cana',
      isPremium: true,
      placementType: 'PremiumSpot',
      position: 3,
    },
  ];

  if (section.includes('Premium')) {
    return premium;
  }
  return featured;
}
