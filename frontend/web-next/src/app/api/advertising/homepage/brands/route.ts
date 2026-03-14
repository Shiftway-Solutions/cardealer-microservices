import { NextRequest, NextResponse } from 'next/server';

/**
 * GET /api/advertising/homepage/brands
 * BFF route for homepage brand logos/images.
 * Proxies to backend AdvertisingService with demo fallback.
 */

const API_URL =
  process.env.INTERNAL_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:18443';

export async function GET(request: NextRequest) {
  const includeHidden = request.nextUrl.searchParams.get('includeHidden') || 'false';

  try {
    const backendUrl = `${API_URL}/api/advertising/homepage/brands?includeHidden=${includeHidden}`;
    const res = await fetch(backendUrl, {
      headers: { Accept: 'application/json' },
      next: { revalidate: 3600 }, // cache 1 hour
    });

    if (res.ok) {
      const data = await res.json();
      return NextResponse.json(data);
    }
  } catch {
    // Backend unavailable — fall through to demo data
  }

  // Fallback: return demo brand data
  const brands = getDemoBrands(includeHidden === 'true');
  return NextResponse.json({
    success: true,
    data: brands,
  });
}

export async function PUT(request: NextRequest) {
  try {
    const body = await request.json();
    const authHeader = request.headers.get('authorization');
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      Accept: 'application/json',
    };
    if (authHeader) headers['Authorization'] = authHeader;

    const res = await fetch(`${API_URL}/api/advertising/homepage/brands`, {
      method: 'PUT',
      headers,
      body: JSON.stringify(body),
    });

    if (res.ok) {
      const data = await res.json();
      return NextResponse.json(data);
    }
    return NextResponse.json(
      { success: false, error: `Backend returned ${res.status}` },
      { status: res.status }
    );
  } catch {
    return NextResponse.json({ success: false, error: 'Backend unavailable' }, { status: 502 });
  }
}

function getDemoBrands(includeHidden: boolean) {
  // Concesionarios verificados de República Dominicana con fotos reales de sus vehículos
  // subidas al bucket S3 de producción (okla-images-2026, acceso público).
  const S3 = 'https://okla-images-2026.s3.us-east-2.amazonaws.com/vehicles/2026/03/06';

  const allBrands = [
    {
      id: 'dealer-okla-premium',
      brandKey: 'okla-premium-motors',
      displayName: 'OKLA Premium Motors',
      logoUrl: `${S3}/0c0a99b8-21bb-4676-bf2b-793d99193d20.jpg`,
      displayOrder: 1,
      isActive: true,
      vehicleCount: 3,
    },
    {
      id: 'dealer-auto-express',
      brandKey: 'auto-express-sd',
      displayName: 'Auto Express SD',
      logoUrl: `${S3}/b70d9d72-b508-4a78-bb39-ee6f1350db8f.jpg`,
      displayOrder: 2,
      isActive: true,
      vehicleCount: 12,
    },
    {
      id: 'dealer-motores-caribe',
      brandKey: 'motores-del-caribe',
      displayName: 'Motores del Caribe',
      logoUrl: `${S3}/0befd9b6-eb63-4eed-a13b-2fdcefaff6a1.jpg`,
      displayOrder: 3,
      isActive: true,
      vehicleCount: 8,
    },
    {
      id: 'dealer-carcenter-rd',
      brandKey: 'grupo-carcenter-rd',
      displayName: 'Grupo CarCenter RD',
      logoUrl: `${S3}/ec0d97ae-c18f-4d2a-b31e-15ea227f77d6.jpg`,
      displayOrder: 4,
      isActive: true,
      vehicleCount: 15,
    },
    {
      id: 'dealer-premier-motors',
      brandKey: 'premier-motors-srl',
      displayName: 'Premier Motors SRL',
      logoUrl: `${S3}/f9170344-f56a-4a39-8031-5a60b352e894.jpg`,
      displayOrder: 5,
      isActive: true,
      vehicleCount: 10,
    },
    {
      id: 'dealer-caribbean-auto',
      brandKey: 'caribbean-auto-group',
      displayName: 'Caribbean Auto Group',
      logoUrl: `${S3}/892c3605-42b9-4d8d-93f6-07c19a0c8cd2.jpg`,
      displayOrder: 6,
      isActive: true,
      vehicleCount: 7,
    },
    {
      id: 'dealer-mundo-auto',
      brandKey: 'mundo-auto-rd',
      displayName: 'Mundo Auto RD',
      logoUrl: `${S3}/67355928-79cd-49c8-b9f9-9fec0e5fe1f0.jpg`,
      displayOrder: 7,
      isActive: true,
      vehicleCount: 20,
    },
    {
      id: 'dealer-elite-cars',
      brandKey: 'elite-cars-rd',
      displayName: 'Elite Cars RD',
      logoUrl: `${S3}/5148b583-bad8-48cf-8e19-c422b9aac9f5.jpg`,
      displayOrder: 8,
      isActive: true,
      vehicleCount: 6,
    },
    {
      id: 'dealer-autostar',
      brandKey: 'autostar-premium',
      displayName: 'AutoStar Premium',
      logoUrl: `${S3}/3f7439e1-7bf5-4839-9fce-088c25d5f9f9.jpg`,
      displayOrder: 9,
      isActive: true,
      vehicleCount: 9,
    },
    {
      id: 'dealer-concesionaria-nacional',
      brandKey: 'concesionaria-nacional',
      displayName: 'Concesionaria Nacional',
      logoUrl: `${S3}/3e639040-ff03-4ada-a2fd-c9a7ab892c5b.jpg`,
      displayOrder: 10,
      isActive: true,
      vehicleCount: 11,
    },
  ];

  if (includeHidden) {
    return allBrands;
  }
  return allBrands.filter(b => b.isActive);
}
