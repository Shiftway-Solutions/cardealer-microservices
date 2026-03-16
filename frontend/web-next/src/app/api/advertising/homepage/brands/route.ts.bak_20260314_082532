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
  const allBrands = [
    {
      id: 'brand-toyota',
      brandKey: 'toyota',
      displayName: 'Toyota',
      logoUrl: 'https://images.unsplash.com/photo-1621007947382-bb3c3994e3fb?w=400&q=80',
      displayOrder: 1,
      isActive: true,
      vehicleCount: 25,
    },
    {
      id: 'brand-honda',
      brandKey: 'honda',
      displayName: 'Honda',
      logoUrl: 'https://images.unsplash.com/photo-1603584173870-7f23fdae1b7a?w=400&q=80',
      displayOrder: 2,
      isActive: true,
      vehicleCount: 20,
    },
    {
      id: 'brand-hyundai',
      brandKey: 'hyundai',
      displayName: 'Hyundai',
      logoUrl: 'https://images.unsplash.com/photo-1609521263047-f8f205293f24?w=400&q=80',
      displayOrder: 3,
      isActive: true,
      vehicleCount: 18,
    },
    {
      id: 'brand-kia',
      brandKey: 'kia',
      displayName: 'Kia',
      logoUrl: 'https://images.unsplash.com/photo-1619767886558-efdc259cde1a?w=400&q=80',
      displayOrder: 4,
      isActive: true,
      vehicleCount: 15,
    },
    {
      id: 'brand-nissan',
      brandKey: 'nissan',
      displayName: 'Nissan',
      logoUrl: 'https://images.unsplash.com/photo-1617788138017-80ad40651399?w=400&q=80',
      displayOrder: 5,
      isActive: true,
      vehicleCount: 14,
    },
    {
      id: 'brand-ford',
      brandKey: 'ford',
      displayName: 'Ford',
      logoUrl: 'https://images.unsplash.com/photo-1612825173281-9a193378527e?w=400&q=80',
      displayOrder: 6,
      isActive: true,
      vehicleCount: 12,
    },
    {
      id: 'brand-chevrolet',
      brandKey: 'chevrolet',
      displayName: 'Chevrolet',
      logoUrl: 'https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=400&q=80',
      displayOrder: 7,
      isActive: true,
      vehicleCount: 10,
    },
    {
      id: 'brand-mazda',
      brandKey: 'mazda',
      displayName: 'Mazda',
      logoUrl: 'https://images.unsplash.com/photo-1580273916550-e323be2ae537?w=400&q=80',
      displayOrder: 8,
      isActive: true,
      vehicleCount: 8,
    },
    {
      id: 'brand-bmw',
      brandKey: 'bmw',
      displayName: 'BMW',
      logoUrl: 'https://images.unsplash.com/photo-1555215695-3004980ad54e?w=400&q=80',
      displayOrder: 9,
      isActive: true,
      vehicleCount: 6,
    },
    {
      id: 'brand-mercedes-benz',
      brandKey: 'mercedes-benz',
      displayName: 'Mercedes-Benz',
      logoUrl: 'https://images.unsplash.com/photo-1618843479313-40f8afb4b4d8?w=400&q=80',
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
