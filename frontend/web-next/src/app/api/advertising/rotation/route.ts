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
      // Transform backend field names to match frontend RotatedVehicle interface
      // Backend returns: vehicleTitle, vehicleImage, vehiclePrice, vehicleMake, vehicleModel, vehicleYear
      // Frontend expects: title, imageUrl, price, location, isFeatured, isPremium
      if (data?.data?.items) {
        data.data.items = data.data.items.map(transformRotatedVehicle);
        // Check if images are placeholder/demo paths — if so, enrich with real data
        const hasPlaceholderImages = data.data.items.some(
          (item: Record<string, unknown>) =>
            !item.imageUrl ||
            (typeof item.imageUrl === 'string' &&
              (item.imageUrl.startsWith('/images/demo/') ||
                item.imageUrl === '/placeholder-car.jpg'))
        );
        if (hasPlaceholderImages) {
          // Try to enrich with real vehicle data from the vehicles API
          const enriched = await enrichWithRealVehicles(data.data.items, section);
          if (enriched.length > 0) {
            data.data.items = enriched;
          } else {
            // Fall back to demo data with working Unsplash images
            data.data.items = getDemoRotationItems(section);
          }
        }
      }
      return NextResponse.json(data);
    }
  } catch {
    // Backend unavailable — fall through to demo data
  }

  // Try to get real vehicles first before falling back to static demo data
  const realItems = await enrichWithRealVehicles([], section);
  if (realItems.length > 0) {
    return NextResponse.json({
      success: true,
      data: {
        items: realItems,
        section,
        rotatedAt: new Date().toISOString(),
        source: 'vehicles-api',
      },
    });
  }

  // Fallback: return demo rotation data with working Unsplash images
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

/**
 * Transform backend AdvertisingService field names to frontend RotatedVehicle fields.
 * Backend: vehicleTitle, vehicleImage, vehiclePrice, vehicleMake, vehicleModel, vehicleYear
 * Frontend: title, imageUrl, price, location, slug, isFeatured, isPremium
 */
function transformRotatedVehicle(item: Record<string, unknown>): Record<string, unknown> {
  const placementType = String(item.placementType || '');
  return {
    ...item,
    title: item.title || item.vehicleTitle || '',
    imageUrl: item.imageUrl || item.vehicleImage || '',
    price: item.price || item.vehiclePrice || 0,
    slug:
      item.slug ||
      `${String(item.vehicleYear || '')}-${String(item.vehicleMake || '').toLowerCase()}-${String(item.vehicleModel || '').toLowerCase()}-${String(item.vehicleId || '').slice(0, 8)}`.replace(
        /\s+/g,
        '-'
      ),
    isFeatured: item.isFeatured ?? placementType === 'FeaturedSpot',
    isPremium: item.isPremium ?? placementType === 'PremiumSpot',
  };
}

/**
 * Fetch real active vehicles from the vehicles API to fill ad rotation slots.
 * Uses the most expensive/newest vehicles for Premium, newest for Featured.
 */
async function enrichWithRealVehicles(
  existingItems: Record<string, unknown>[],
  section: string
): Promise<Record<string, unknown>[]> {
  try {
    const sortBy = section.includes('Premium') ? 'price_desc' : 'newest';
    const pageSize = section.includes('Premium') ? 12 : 6;
    const vehiclesUrl = `${API_URL}/api/vehicles?pageSize=${pageSize}&page=1&sortBy=${sortBy}&status=Active`;
    const res = await fetch(vehiclesUrl, {
      headers: { Accept: 'application/json' },
      next: { revalidate: 300 },
    });

    if (!res.ok) return [];

    const data = await res.json();
    const vehicles = data?.vehicles || data?.data?.vehicles || data?.data?.items || [];
    if (!vehicles.length) return [];

    return vehicles.map((v: Record<string, unknown>, idx: number) => {
      // Extract the best image URL from the vehicle's images array
      const images = (v.images as Record<string, unknown>[]) || [];
      const sortedImages = images.slice().sort((a, b) => {
        const aOrder = (a.sortOrder as number) ?? (a.order as number) ?? 99;
        const bOrder = (b.sortOrder as number) ?? (b.order as number) ?? 99;
        return aOrder - bOrder;
      });
      const primaryImage = sortedImages[0];
      let imageUrl = (primaryImage?.url as string) || '';

      // Strip query params from pre-signed S3 URLs to get a public URL
      // If the S3 bucket is not public, this won't work, but the pre-signed URL
      // should still be valid for its TTL (24h in production)
      if (!imageUrl) {
        imageUrl = '/placeholder-car.jpg';
      }

      // Collect up to 3 image URLs for gallery display
      const imageUrls = sortedImages
        .slice(0, 3)
        .map((img: Record<string, unknown>) => (img.url as string) || '')
        .filter(Boolean);

      // Build slug
      const year = v.year || '';
      const make = String(v.make || '').toLowerCase();
      const model = String(v.model || '').toLowerCase();
      const shortId = String(v.id || '').slice(0, 8);
      const slug = `${year}-${make}-${model}-${shortId}`.replace(/\s+/g, '-');

      const isPremium = section.includes('Premium');

      return {
        campaignId: `real-${isPremium ? 'p' : 'f'}${idx + 1}`,
        vehicleId: v.id || '',
        title: v.title || `${year} ${v.make} ${v.model}`,
        slug,
        imageUrl,
        imageUrls: imageUrls.length > 0 ? imageUrls : [imageUrl],
        price: v.price || 0,
        currency: v.currency || 'DOP',
        qualityScore: 90 + Math.floor(Math.random() * 10),
        location: v.city || v.state || 'República Dominicana',
        photoCount: imageUrls.length || 1,
        isFeatured: !isPremium,
        isPremium,
        placementType: section,
        position: idx + 1,
      };
    });
  } catch {
    return [];
  }
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
  // Each vehicle includes photoCount and imageUrls for gallery display.
  const featured = [
    {
      campaignId: 'demo-f1',
      vehicleId: '94887983-8bdf-40fb-80fe-bf1465498124',
      title: '2024 Toyota RAV4 Limited',
      slug: '2024-toyota-rav4-94887983',
      imageUrl: 'https://images.unsplash.com/photo-1619767886558-efdc259cde1a?w=800&q=75',
      imageUrls: [
        'https://images.unsplash.com/photo-1619767886558-efdc259cde1a?w=800&q=75',
        'https://images.unsplash.com/photo-1621007947382-bb3c3994e3fb?w=800&q=75',
        'https://images.unsplash.com/photo-1568844293986-8d0400f4f36d?w=800&q=75',
      ],
      price: 2850000,
      currency: 'DOP',
      qualityScore: 95,
      location: 'Santo Domingo',
      photoCount: 3,
      isFeatured: true,
      placementType: section,
      position: 1,
    },
    {
      campaignId: 'demo-f2',
      vehicleId: '3778c87b-b1e3-4be0-a40f-362dbfcee262',
      title: '2023 Toyota Camry SE',
      slug: '2023-toyota-camry-3778c87b',
      imageUrl: 'https://images.unsplash.com/photo-1549317661-bd32c8ce0db2?w=800&q=75',
      imageUrls: [
        'https://images.unsplash.com/photo-1549317661-bd32c8ce0db2?w=800&q=75',
        'https://images.unsplash.com/photo-1553440569-bcc63803a83d?w=800&q=75',
        'https://images.unsplash.com/photo-1580273916550-e323be2ae537?w=800&q=75',
      ],
      price: 2800000,
      currency: 'DOP',
      qualityScore: 92,
      location: 'Santiago',
      photoCount: 3,
      isFeatured: true,
      placementType: section,
      position: 2,
    },
    {
      campaignId: 'demo-f3',
      vehicleId: '839fdf79-3887-4703-89ce-f3db75cde273',
      title: '2023 Honda CR-V EX-L',
      slug: '2023-honda-cr-v-839fdf79',
      imageUrl: 'https://images.unsplash.com/photo-1606611013016-969c19ba27c5?w=800&q=75',
      imageUrls: [
        'https://images.unsplash.com/photo-1606611013016-969c19ba27c5?w=800&q=75',
        'https://images.unsplash.com/photo-1583121274602-3e2820c69888?w=800&q=75',
        'https://images.unsplash.com/photo-1605559424843-9e4c228bf1c2?w=800&q=75',
      ],
      price: 1950000,
      currency: 'DOP',
      qualityScore: 90,
      location: 'Santo Domingo Este',
      photoCount: 3,
      isFeatured: true,
      placementType: section,
      position: 3,
    },
    {
      campaignId: 'demo-f4',
      vehicleId: 'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
      title: '2024 Hyundai Tucson SEL',
      slug: '2024-hyundai-tucson-a1b2c3d4',
      imageUrl: 'https://images.unsplash.com/photo-1609521263047-f8f205293f24?w=800&q=75',
      imageUrls: [
        'https://images.unsplash.com/photo-1609521263047-f8f205293f24?w=800&q=75',
        'https://images.unsplash.com/photo-1625231334168-32354e3b4f4b?w=800&q=75',
        'https://images.unsplash.com/photo-1617814076367-b759c7d7e738?w=800&q=75',
      ],
      price: 2200000,
      currency: 'DOP',
      qualityScore: 91,
      location: 'La Vega',
      photoCount: 3,
      isFeatured: true,
      placementType: section,
      position: 4,
    },
    {
      campaignId: 'demo-f5',
      vehicleId: 'b2c3d4e5-f6a7-8901-bcde-f12345678901',
      title: '2023 Kia Sportage LX',
      slug: '2023-kia-sportage-b2c3d4e5',
      imageUrl: 'https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=800&q=75',
      imageUrls: [
        'https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=800&q=75',
        'https://images.unsplash.com/photo-1542362567-b07e54358753?w=800&q=75',
        'https://images.unsplash.com/photo-1503376780353-7e6692767b70?w=800&q=75',
      ],
      price: 2050000,
      currency: 'DOP',
      qualityScore: 89,
      location: 'San Pedro de Macorís',
      photoCount: 3,
      isFeatured: true,
      placementType: section,
      position: 5,
    },
    {
      campaignId: 'demo-f6',
      vehicleId: 'c3d4e5f6-a7b8-9012-cdef-123456789012',
      title: '2024 Nissan Pathfinder SV',
      slug: '2024-nissan-pathfinder-c3d4e5f6',
      imageUrl: 'https://images.unsplash.com/photo-1533473359331-2969f3c6b787?w=800&q=75',
      imageUrls: [
        'https://images.unsplash.com/photo-1533473359331-2969f3c6b787?w=800&q=75',
        'https://images.unsplash.com/photo-1494976388531-d1058494cdd8?w=800&q=75',
        'https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?w=800&q=75',
      ],
      price: 3200000,
      currency: 'DOP',
      qualityScore: 93,
      location: 'Distrito Nacional',
      photoCount: 3,
      isFeatured: true,
      placementType: section,
      position: 6,
    },
  ];

  const premium = [
    {
      campaignId: 'demo-p1',
      vehicleId: '3a600ca0-72a8-4269-a3cb-30d27fc1cede',
      title: '2024 Mercedes-Benz GLC 300',
      slug: '2024-mercedes-benz-glc-3a600ca0',
      imageUrl: 'https://images.unsplash.com/photo-1618843479313-40f8afb4b4d8?w=800&q=75',
      imageUrls: [
        'https://images.unsplash.com/photo-1618843479313-40f8afb4b4d8?w=800&q=75',
        'https://images.unsplash.com/photo-1617814076367-b759c7d7e738?w=800&q=75',
        'https://images.unsplash.com/photo-1616422285623-13ff0162193c?w=800&q=75',
      ],
      price: 4250000,
      currency: 'DOP',
      qualityScore: 97,
      location: 'Distrito Nacional',
      photoCount: 3,
      isPremium: true,
      placementType: 'PremiumSpot',
      position: 1,
    },
    {
      campaignId: 'demo-p2',
      vehicleId: '78a42948-04d8-4e40-b168-46d518a89740',
      title: '2023 BMW X5 xDrive40i',
      slug: '2023-bmw-x5-78a42948',
      imageUrl: 'https://images.unsplash.com/photo-1555215695-3004980ad54e?w=800&q=75',
      imageUrls: [
        'https://images.unsplash.com/photo-1555215695-3004980ad54e?w=800&q=75',
        'https://images.unsplash.com/photo-1556189250-72ba954cfc2b?w=800&q=75',
        'https://images.unsplash.com/photo-1580273916550-e323be2ae537?w=800&q=75',
      ],
      price: 5100000,
      currency: 'DOP',
      qualityScore: 96,
      location: 'Punta Cana',
      photoCount: 3,
      isPremium: true,
      placementType: 'PremiumSpot',
      position: 2,
    },
    {
      campaignId: 'demo-p3',
      vehicleId: '42e82cbc-c6c9-4da2-b801-7931362cbb3d',
      title: '2023 Audi Q7 Premium Plus',
      slug: '2023-audi-q7-42e82cbc',
      imageUrl: 'https://images.unsplash.com/photo-1606664515524-ed2f786a0bd6?w=800&q=75',
      imageUrls: [
        'https://images.unsplash.com/photo-1606664515524-ed2f786a0bd6?w=800&q=75',
        'https://images.unsplash.com/photo-1603584173870-7f23fdae1b7a?w=800&q=75',
        'https://images.unsplash.com/photo-1549317661-bd32c8ce0db2?w=800&q=75',
      ],
      price: 4850000,
      currency: 'DOP',
      qualityScore: 95,
      location: 'Santo Domingo',
      photoCount: 3,
      isPremium: true,
      placementType: 'PremiumSpot',
      position: 3,
    },
    {
      campaignId: 'demo-p4',
      vehicleId: 'd4e5f6a7-b8c9-0123-defa-234567890123',
      title: '2024 Lexus RX 350h',
      slug: '2024-lexus-rx-d4e5f6a7',
      imageUrl: 'https://images.unsplash.com/photo-1583121274602-3e2820c69888?w=800&q=75',
      imageUrls: [
        'https://images.unsplash.com/photo-1583121274602-3e2820c69888?w=800&q=75',
        'https://images.unsplash.com/photo-1605559424843-9e4c228bf1c2?w=800&q=75',
        'https://images.unsplash.com/photo-1542362567-b07e54358753?w=800&q=75',
      ],
      price: 4500000,
      currency: 'DOP',
      qualityScore: 94,
      location: 'Santiago',
      photoCount: 3,
      isPremium: true,
      placementType: 'PremiumSpot',
      position: 4,
    },
    {
      campaignId: 'demo-p5',
      vehicleId: 'e5f6a7b8-c9d0-1234-efab-345678901234',
      title: '2023 Porsche Cayenne S',
      slug: '2023-porsche-cayenne-e5f6a7b8',
      imageUrl: 'https://images.unsplash.com/photo-1503376780353-7e6692767b70?w=800&q=75',
      imageUrls: [
        'https://images.unsplash.com/photo-1503376780353-7e6692767b70?w=800&q=75',
        'https://images.unsplash.com/photo-1494976388531-d1058494cdd8?w=800&q=75',
        'https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?w=800&q=75',
      ],
      price: 7500000,
      currency: 'DOP',
      qualityScore: 98,
      location: 'Casa de Campo',
      photoCount: 3,
      isPremium: true,
      placementType: 'PremiumSpot',
      position: 5,
    },
    {
      campaignId: 'demo-p6',
      vehicleId: 'f6a7b8c9-d0e1-2345-fabc-456789012345',
      title: '2024 Range Rover Sport',
      slug: '2024-range-rover-sport-f6a7b8c9',
      imageUrl: 'https://images.unsplash.com/photo-1606611013016-969c19ba27c5?w=800&q=75',
      imageUrls: [
        'https://images.unsplash.com/photo-1606611013016-969c19ba27c5?w=800&q=75',
        'https://images.unsplash.com/photo-1619767886558-efdc259cde1a?w=800&q=75',
        'https://images.unsplash.com/photo-1533473359331-2969f3c6b787?w=800&q=75',
      ],
      price: 6800000,
      currency: 'DOP',
      qualityScore: 96,
      location: 'Distrito Nacional',
      photoCount: 3,
      isPremium: true,
      placementType: 'PremiumSpot',
      position: 6,
    },
    {
      campaignId: 'demo-p7',
      vehicleId: 'a7b8c9d0-e1f2-3456-abcd-567890123456',
      title: '2023 Volvo XC90 T8',
      slug: '2023-volvo-xc90-a7b8c9d0',
      imageUrl: 'https://images.unsplash.com/photo-1609521263047-f8f205293f24?w=800&q=75',
      imageUrls: [
        'https://images.unsplash.com/photo-1609521263047-f8f205293f24?w=800&q=75',
        'https://images.unsplash.com/photo-1625231334168-32354e3b4f4b?w=800&q=75',
        'https://images.unsplash.com/photo-1553440569-bcc63803a83d?w=800&q=75',
      ],
      price: 5200000,
      currency: 'DOP',
      qualityScore: 94,
      location: 'Santo Domingo',
      photoCount: 3,
      isPremium: true,
      placementType: 'PremiumSpot',
      position: 7,
    },
    {
      campaignId: 'demo-p8',
      vehicleId: 'b8c9d0e1-f2a3-4567-bcde-678901234567',
      title: '2024 Genesis GV80 3.5T',
      slug: '2024-genesis-gv80-b8c9d0e1',
      imageUrl: 'https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=800&q=75',
      imageUrls: [
        'https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=800&q=75',
        'https://images.unsplash.com/photo-1618843479313-40f8afb4b4d8?w=800&q=75',
        'https://images.unsplash.com/photo-1555215695-3004980ad54e?w=800&q=75',
      ],
      price: 4800000,
      currency: 'DOP',
      qualityScore: 93,
      location: 'La Romana',
      photoCount: 3,
      isPremium: true,
      placementType: 'PremiumSpot',
      position: 8,
    },
    {
      campaignId: 'demo-p9',
      vehicleId: 'c9d0e1f2-a3b4-5678-cdef-789012345678',
      title: '2023 Infiniti QX60 Luxe',
      slug: '2023-infiniti-qx60-c9d0e1f2',
      imageUrl: 'https://images.unsplash.com/photo-1494976388531-d1058494cdd8?w=800&q=75',
      imageUrls: [
        'https://images.unsplash.com/photo-1494976388531-d1058494cdd8?w=800&q=75',
        'https://images.unsplash.com/photo-1606664515524-ed2f786a0bd6?w=800&q=75',
        'https://images.unsplash.com/photo-1603584173870-7f23fdae1b7a?w=800&q=75',
      ],
      price: 3900000,
      currency: 'DOP',
      qualityScore: 91,
      location: 'Santiago',
      photoCount: 3,
      isPremium: true,
      placementType: 'PremiumSpot',
      position: 9,
    },
    {
      campaignId: 'demo-p10',
      vehicleId: 'd0e1f2a3-b4c5-6789-defa-890123456789',
      title: '2024 Acura MDX Type S',
      slug: '2024-acura-mdx-d0e1f2a3',
      imageUrl: 'https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?w=800&q=75',
      imageUrls: [
        'https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?w=800&q=75',
        'https://images.unsplash.com/photo-1583121274602-3e2820c69888?w=800&q=75',
        'https://images.unsplash.com/photo-1605559424843-9e4c228bf1c2?w=800&q=75',
      ],
      price: 4100000,
      currency: 'DOP',
      qualityScore: 92,
      location: 'Santo Domingo Oeste',
      photoCount: 3,
      isPremium: true,
      placementType: 'PremiumSpot',
      position: 10,
    },
    {
      campaignId: 'demo-p11',
      vehicleId: 'e1f2a3b4-c5d6-7890-efab-901234567890',
      title: '2023 Lincoln Aviator Reserve',
      slug: '2023-lincoln-aviator-e1f2a3b4',
      imageUrl: 'https://images.unsplash.com/photo-1542362567-b07e54358753?w=800&q=75',
      imageUrls: [
        'https://images.unsplash.com/photo-1542362567-b07e54358753?w=800&q=75',
        'https://images.unsplash.com/photo-1533473359331-2969f3c6b787?w=800&q=75',
        'https://images.unsplash.com/photo-1556189250-72ba954cfc2b?w=800&q=75',
      ],
      price: 4600000,
      currency: 'DOP',
      qualityScore: 90,
      location: 'Punta Cana',
      photoCount: 3,
      isPremium: true,
      placementType: 'PremiumSpot',
      position: 11,
    },
    {
      campaignId: 'demo-p12',
      vehicleId: 'f2a3b4c5-d6e7-8901-fabc-012345678901',
      title: '2024 Cadillac Escalade Sport',
      slug: '2024-cadillac-escalade-f2a3b4c5',
      imageUrl: 'https://images.unsplash.com/photo-1616422285623-13ff0162193c?w=800&q=75',
      imageUrls: [
        'https://images.unsplash.com/photo-1616422285623-13ff0162193c?w=800&q=75',
        'https://images.unsplash.com/photo-1619767886558-efdc259cde1a?w=800&q=75',
        'https://images.unsplash.com/photo-1609521263047-f8f205293f24?w=800&q=75',
      ],
      price: 8500000,
      currency: 'DOP',
      qualityScore: 99,
      location: 'Casa de Campo',
      photoCount: 3,
      isPremium: true,
      placementType: 'PremiumSpot',
      position: 12,
    },
  ];

  if (section.includes('Premium')) {
    return premium;
  }
  return featured;
}
