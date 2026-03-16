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
  // Logos de marcas: SVGs oficiales alojados en Wikimedia Commons (dominio público / CC).
  // Estos son logotipos de marcas reales, no fotos de vehículos, para un aspecto profesional.
  const allBrands = [
    {
      id: 'brand-toyota',
      brandKey: 'toyota',
      displayName: 'Toyota',
      logoUrl:
        'https://upload.wikimedia.org/wikipedia/commons/thumb/e/ee/Toyota_logo_%28Red%29.svg/320px-Toyota_logo_%28Red%29.svg.png',
      displayOrder: 1,
      isActive: true,
      vehicleCount: 25,
    },
    {
      id: 'brand-honda',
      brandKey: 'honda',
      displayName: 'Honda',
      logoUrl:
        'https://upload.wikimedia.org/wikipedia/commons/thumb/3/38/Honda.svg/320px-Honda.svg.png',
      displayOrder: 2,
      isActive: true,
      vehicleCount: 20,
    },
    {
      id: 'brand-hyundai',
      brandKey: 'hyundai',
      displayName: 'Hyundai',
      logoUrl:
        'https://upload.wikimedia.org/wikipedia/commons/thumb/a/ad/Hyundai_Motor_Company_logo.svg/320px-Hyundai_Motor_Company_logo.svg.png',
      displayOrder: 3,
      isActive: true,
      vehicleCount: 18,
    },
    {
      id: 'brand-kia',
      brandKey: 'kia',
      displayName: 'Kia',
      logoUrl:
        'https://upload.wikimedia.org/wikipedia/commons/thumb/1/13/Kia-logo.svg/320px-Kia-logo.svg.png',
      displayOrder: 4,
      isActive: true,
      vehicleCount: 15,
    },
    {
      id: 'brand-nissan',
      brandKey: 'nissan',
      displayName: 'Nissan',
      logoUrl:
        'https://upload.wikimedia.org/wikipedia/commons/thumb/8/8c/Nissan_2020_logo.svg/320px-Nissan_2020_logo.svg.png',
      displayOrder: 5,
      isActive: true,
      vehicleCount: 14,
    },
    {
      id: 'brand-ford',
      brandKey: 'ford',
      displayName: 'Ford',
      logoUrl:
        'https://upload.wikimedia.org/wikipedia/commons/thumb/3/3e/Ford_logo_flat.svg/320px-Ford_logo_flat.svg.png',
      displayOrder: 6,
      isActive: true,
      vehicleCount: 12,
    },
    {
      id: 'brand-chevrolet',
      brandKey: 'chevrolet',
      displayName: 'Chevrolet',
      logoUrl:
        'https://upload.wikimedia.org/wikipedia/commons/thumb/1/19/Chevrolet_logo.svg/320px-Chevrolet_logo.svg.png',
      displayOrder: 7,
      isActive: true,
      vehicleCount: 10,
    },
    {
      id: 'brand-mazda',
      brandKey: 'mazda',
      displayName: 'Mazda',
      logoUrl:
        'https://upload.wikimedia.org/wikipedia/commons/thumb/1/10/Mazda_logo.svg/320px-Mazda_logo.svg.png',
      displayOrder: 8,
      isActive: true,
      vehicleCount: 8,
    },
    {
      id: 'brand-bmw',
      brandKey: 'bmw',
      displayName: 'BMW',
      logoUrl:
        'https://upload.wikimedia.org/wikipedia/commons/thumb/4/44/BMW.svg/220px-BMW.svg.png',
      displayOrder: 9,
      isActive: true,
      vehicleCount: 6,
    },
    {
      id: 'brand-mercedes-benz',
      brandKey: 'mercedes-benz',
      displayName: 'Mercedes-Benz',
      logoUrl:
        'https://upload.wikimedia.org/wikipedia/commons/thumb/9/90/Mercedes-Logo.svg/220px-Mercedes-Logo.svg.png',
      displayOrder: 10,
      isActive: true,
      vehicleCount: 5,
    },
  ];

  if (includeHidden) {
    return allBrands;
  }
  return allBrands.filter(b => b.isActive);
}
