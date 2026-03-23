import { NextRequest, NextResponse } from 'next/server';

/**
 * GET  /api/vehicles — BFF proxy → VehiclesSaleService vía Gateway.
 *   Fallback: demo data cubriendo todos los tipos de carrocería y combustible
 *   para que las secciones del homepage siempre muestren vehículos completos.
 *
 *   Parámetros soportados:
 *   bodyStyle   = SUV | Sedan | Hatchback | Pickup | Coupe | Crossover |
 *                 SportsCar | Convertible | Van | Minivan
 *   fuelType    = Gasoline | Hybrid | Electric | Diesel
 *   limit       = número máximo de resultados
 *   pageSize    = alias de limit
 *   sortBy      = newest | featured | price_desc | price_asc
 *   status      = Active (default)
 *
 * POST /api/vehicles — BFF proxy for authenticated vehicle creation.
 *   Forwards the request body and Authorization header to VehiclesSaleService.
 *   Required because Next.js intercepts all /api/vehicles requests (including
 *   POST) before the afterFiles rewrite can proxy them to the Gateway, causing
 *   a 405 Method Not Allowed when only GET is exported here.
 */

export const runtime = 'edge';

const API_URL =
  process.env.INTERNAL_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:18443';

export async function POST(request: NextRequest) {
  const authHeader = request.headers.get('authorization');
  const cookieHeader = request.headers.get('cookie');

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
  };
  if (authHeader) headers['Authorization'] = authHeader;
  if (cookieHeader) headers['Cookie'] = cookieHeader;

  try {
    const body = await request.text();
    const res = await fetch(`${API_URL}/api/vehicles`, {
      method: 'POST',
      headers,
      body,
      signal: AbortSignal.timeout(30000),
    });

    const data = await res.text();
    const contentType = res.headers.get('content-type') ?? 'application/json';

    return new NextResponse(data, {
      status: res.status,
      headers: { 'Content-Type': contentType },
    });
  } catch (error) {
    console.error('[/api/vehicles POST] proxy error:', error);
    return NextResponse.json({ error: 'Error interno del servidor.' }, { status: 500 });
  }
}

export async function GET(request: NextRequest) {
  const { searchParams } = request.nextUrl;
  const bodyStyle = searchParams.get('bodyStyle') ?? '';
  const fuelType = searchParams.get('fuelType') ?? '';
  const limit = parseInt(searchParams.get('limit') ?? searchParams.get('pageSize') ?? '10', 10);
  const sortBy = searchParams.get('sortBy') ?? 'featured';

  // Build backend URL preserving all query params
  const backendParams = new URLSearchParams(searchParams);
  const backendUrl = `${API_URL}/api/vehicles?${backendParams.toString()}`;

  try {
    const res = await fetch(backendUrl, {
      headers: { Accept: 'application/json' },
      next: { revalidate: 300 },
    });

    if (res.ok) {
      const data = await res.json();
      // Backend responded — return as-is
      return NextResponse.json(data);
    }
  } catch {
    // Backend unavailable — fall through to demo data
  }

  // ═══════════════════════════════════════════════════════════════
  // FALLBACK: demo vehicles data (backend indisponible)
  // Cubre todos los tipos de carrocería y combustible del homepage.
  // ═══════════════════════════════════════════════════════════════
  const allDemoVehicles = getDemoVehicles();

  let filtered = allDemoVehicles;

  if (bodyStyle) {
    filtered = filtered.filter(v => v.bodyStyle.toLowerCase() === bodyStyle.toLowerCase());
  }

  if (fuelType) {
    filtered = filtered.filter(v => v.fuelType.toLowerCase() === fuelType.toLowerCase());
  }

  // Sorting
  if (sortBy === 'price_desc') {
    filtered = [...filtered].sort((a, b) => b.price - a.price);
  } else if (sortBy === 'price_asc') {
    filtered = [...filtered].sort((a, b) => a.price - b.price);
  } else if (sortBy === 'newest') {
    filtered = [...filtered].sort((a, b) => b.year - a.year);
  }
  // 'featured' keeps insertion order (featured vehicles first by design)

  const vehicles = filtered.slice(0, limit);

  return NextResponse.json({
    vehicles,
    totalCount: filtered.length,
    page: 1,
    pageSize: limit,
    source: 'demo',
  });
}

// ─────────────────────────────────────────────────────────────────────────────
// Demo Vehicle Catalog — datos realistas del mercado dominicano
// Cubre: SUV, Crossover, Sedan, Hatchback, Pickup, Coupe, SportsCar,
//        Convertible, Van, Minivan, Hybrid, Electric
// ─────────────────────────────────────────────────────────────────────────────
function getDemoVehicles() {
  const base = (
    id: string,
    make: string,
    model: string,
    trim: string,
    year: number,
    price: number,
    bodyStyle: string,
    fuelType: string,
    city: string,
    engine: string,
    hp: number,
    transmission: string,
    mileage: number,
    img1: string,
    img2?: string
  ) => ({
    id,
    title: `${year} ${make} ${model} ${trim}`,
    make,
    model,
    year,
    price,
    currency: 'DOP',
    mileage,
    mileageUnit: 'Km',
    bodyStyle,
    fuelType,
    transmission,
    city,
    state: 'RD',
    isFeatured: false,
    isPremium: false,
    images: [
      { url: img1, sortOrder: 1, isPrimary: true },
      ...(img2 ? [{ url: img2, sortOrder: 2, isPrimary: false }] : []),
    ],
  });

  return [
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // SUVs (4)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    base(
      'demo-suv-001',
      'Toyota',
      'RAV4',
      'Limited',
      2024,
      2_850_000,
      'SUV',
      'Gasoline',
      'Santo Domingo',
      '2.5L I4',
      203,
      'Automatic',
      8_500,
      'https://images.unsplash.com/photo-1619767886558-efdc259cde1a?w=800&q=75',
      'https://images.unsplash.com/photo-1621007947382-bb3c3994e3fb?w=800&q=75'
    ),
    base(
      'demo-suv-002',
      'Honda',
      'CR-V',
      'EX-L',
      2023,
      1_950_000,
      'SUV',
      'Gasoline',
      'Santiago',
      '1.5L Turbo I4',
      192,
      'Automatic',
      15_200,
      'https://images.unsplash.com/photo-1603584173870-7f23fdae1b7a?w=800&q=75'
    ),
    base(
      'demo-suv-003',
      'Hyundai',
      'Tucson',
      'SEL',
      2024,
      2_200_000,
      'SUV',
      'Gasoline',
      'La Vega',
      '2.5L I4',
      187,
      'Automatic',
      5_000,
      'https://images.unsplash.com/photo-1609521263047-f8f205293f24?w=800&q=75'
    ),
    base(
      'demo-suv-004',
      'Nissan',
      'X-Trail',
      'SL',
      2023,
      2_100_000,
      'SUV',
      'Gasoline',
      'Distrito Nacional',
      '2.5L I4',
      182,
      'Automatic',
      18_000,
      'https://images.unsplash.com/photo-1541899481282-d53bffe3c35d?w=800&q=75'
    ),

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Crossovers (4)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    base(
      'demo-cross-001',
      'Kia',
      'Sportage',
      'LX',
      2023,
      2_050_000,
      'Crossover',
      'Gasoline',
      'San Pedro de Macorís',
      '2.5L I4',
      187,
      'Automatic',
      12_000,
      'https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=800&q=75'
    ),
    base(
      'demo-cross-002',
      'Mazda',
      'CX-30',
      'Premium',
      2024,
      1_900_000,
      'Crossover',
      'Gasoline',
      'Distrito Nacional',
      '2.5L I4',
      191,
      'Automatic',
      3_200,
      'https://images.unsplash.com/photo-1580273916550-e323be2ae537?w=800&q=75'
    ),
    base(
      'demo-cross-003',
      'Ford',
      'Escape',
      'SE',
      2023,
      1_750_000,
      'Crossover',
      'Gasoline',
      'Santiago',
      '1.5L Turbo I4',
      181,
      'Automatic',
      22_000,
      'https://images.unsplash.com/photo-1494976388531-d1058494cdd8?w=800&q=75'
    ),
    base(
      'demo-cross-004',
      'Chevrolet',
      'Equinox',
      'RS',
      2024,
      1_850_000,
      'Crossover',
      'Gasoline',
      'Santo Domingo Este',
      '1.5L Turbo I4',
      175,
      'Automatic',
      9_800,
      'https://images.unsplash.com/photo-1556189250-72ba954cfc2b?w=800&q=75'
    ),

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Sedanes (6)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    base(
      'demo-sedan-001',
      'Toyota',
      'Camry',
      'SE',
      2023,
      2_800_000,
      'Sedan',
      'Gasoline',
      'Santiago',
      '2.5L I4',
      203,
      'Automatic',
      9_100,
      'https://images.unsplash.com/photo-1549317661-bd32c8ce0db2?w=800&q=75'
    ),
    base(
      'demo-sedan-002',
      'Honda',
      'Accord',
      'Touring',
      2024,
      3_100_000,
      'Sedan',
      'Gasoline',
      'Distrito Nacional',
      '2.0L Turbo I4',
      252,
      'Automatic',
      4_500,
      'https://images.unsplash.com/photo-1553440569-bcc63803a83d?w=800&q=75'
    ),
    base(
      'demo-sedan-003',
      'Hyundai',
      'Sonata',
      'SEL',
      2023,
      1_980_000,
      'Sedan',
      'Gasoline',
      'San Cristóbal',
      '2.5L I4',
      191,
      'Automatic',
      14_200,
      'https://images.unsplash.com/photo-1625231334168-32354e3b4f4b?w=800&q=75'
    ),
    base(
      'demo-sedan-004',
      'Nissan',
      'Altima',
      'SR',
      2023,
      1_820_000,
      'Sedan',
      'Gasoline',
      'Puerto Plata',
      '2.5L I4',
      182,
      'Automatic',
      11_300,
      'https://images.unsplash.com/photo-1617788138017-80ad40651399?w=800&q=75'
    ),
    base(
      'demo-sedan-005',
      'Kia',
      'K5',
      'GT-Line',
      2024,
      2_100_000,
      'Sedan',
      'Gasoline',
      'Distrito Nacional',
      '1.6L Turbo I4',
      180,
      'Automatic',
      7_800,
      'https://images.unsplash.com/photo-1542362567-b07e54358753?w=800&q=75'
    ),
    base(
      'demo-sedan-006',
      'Mazda',
      '6',
      'Grand Touring',
      2023,
      2_450_000,
      'Sedan',
      'Gasoline',
      'Santiago',
      '2.5L Turbo I4',
      227,
      'Automatic',
      13_500,
      'https://images.unsplash.com/photo-1503376780353-7e6692767b70?w=800&q=75'
    ),

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Hatchbacks (4)  ← usuario pide 2 más
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    base(
      'demo-hatch-001',
      'Honda',
      'Fit',
      'Sport',
      2022,
      980_000,
      'Hatchback',
      'Gasoline',
      'Santo Domingo',
      '1.5L I4',
      128,
      'Automatic',
      28_000,
      'https://images.unsplash.com/photo-1612825173281-9a193378527e?w=800&q=75'
    ),
    base(
      'demo-hatch-002',
      'Toyota',
      'Yaris',
      'SE',
      2023,
      1_050_000,
      'Hatchback',
      'Gasoline',
      'Santiago',
      '1.5L I4',
      106,
      'Automatic',
      12_500,
      'https://images.unsplash.com/photo-1621007947382-bb3c3994e3fb?w=800&q=75'
    ),
    base(
      'demo-hatch-003',
      'Hyundai',
      'i20',
      'Active',
      2023,
      1_150_000,
      'Hatchback',
      'Gasoline',
      'La Romana',
      '1.4L I4',
      100,
      'Automatic',
      8_300,
      'https://images.unsplash.com/photo-1609521263047-f8f205293f24?w=800&q=75'
    ),
    base(
      'demo-hatch-004',
      'Kia',
      'Rio',
      'LX',
      2024,
      1_200_000,
      'Hatchback',
      'Gasoline',
      'Distrito Nacional',
      '1.6L I4',
      120,
      'Automatic',
      5_200,
      'https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=800&q=75'
    ),

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Pickup / Camionetas (3)  ← usuario pide 1 más
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    base(
      'demo-pickup-001',
      'Toyota',
      'Hilux',
      'SRX 4x4',
      2024,
      3_250_000,
      'Pickup',
      'Diesel',
      'Santiago',
      '2.8L Diesel Turbo I4',
      204,
      'Automatic',
      6_800,
      'https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=800&q=75'
    ),
    base(
      'demo-pickup-002',
      'Ford',
      'Ranger',
      'Wildtrak',
      2023,
      2_950_000,
      'Pickup',
      'Diesel',
      'Santo Domingo',
      '2.0L Diesel Turbo I4',
      170,
      'Automatic',
      18_000,
      'https://images.unsplash.com/photo-1494976388531-d1058494cdd8?w=800&q=75'
    ),
    base(
      'demo-pickup-003',
      'Chevrolet',
      'Colorado',
      'LT 4x4',
      2023,
      2_700_000,
      'Pickup',
      'Gasoline',
      'San Pedro de Macorís',
      '3.6L V6',
      308,
      'Automatic',
      21_000,
      'https://images.unsplash.com/photo-1556189250-72ba954cfc2b?w=800&q=75'
    ),

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Coupés (3)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    base(
      'demo-coupe-001',
      'Honda',
      'Civic',
      'Si',
      2023,
      1_890_000,
      'Coupe',
      'Gasoline',
      'Distrito Nacional',
      '1.5L Turbo I4',
      200,
      'Manual',
      9_600,
      'https://images.unsplash.com/photo-1603584173870-7f23fdae1b7a?w=800&q=75'
    ),
    base(
      'demo-coupe-002',
      'Toyota',
      'GR86',
      'Premium',
      2023,
      2_450_000,
      'Coupe',
      'Gasoline',
      'Santo Domingo',
      '2.4L Boxer H4',
      228,
      'Manual',
      5_200,
      'https://images.unsplash.com/photo-1549317661-bd32c8ce0db2?w=800&q=75'
    ),
    base(
      'demo-coupe-003',
      'Hyundai',
      'Elantra',
      'N',
      2024,
      2_100_000,
      'Coupe',
      'Gasoline',
      'Santiago',
      '2.0L Turbo I4',
      276,
      'DCT',
      3_100,
      'https://images.unsplash.com/photo-1609521263047-f8f205293f24?w=800&q=75'
    ),

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Deportivos / SportsCar (3)  ← usuario pide 2 más
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    base(
      'demo-sport-001',
      'Ford',
      'Mustang',
      'GT Premium',
      2023,
      4_200_000,
      'SportsCar',
      'Gasoline',
      'Distrito Nacional',
      '5.0L V8',
      450,
      'Manual',
      8_900,
      'https://images.unsplash.com/photo-1612825173281-9a193378527e?w=800&q=75'
    ),
    base(
      'demo-sport-002',
      'Chevrolet',
      'Camaro',
      'SS',
      2023,
      4_800_000,
      'SportsCar',
      'Gasoline',
      'Punta Cana',
      '6.2L V8',
      455,
      'Automatic',
      5_500,
      'https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=800&q=75'
    ),
    base(
      'demo-sport-003',
      'Nissan',
      '370Z',
      'Nismo',
      2023,
      3_600_000,
      'SportsCar',
      'Gasoline',
      'Casa de Campo',
      '3.7L V6',
      350,
      'Manual',
      11_200,
      'https://images.unsplash.com/photo-1617788138017-80ad40651399?w=800&q=75'
    ),

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Convertibles (3)  ← usuario pide 2 más
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    base(
      'demo-conv-001',
      'Mazda',
      'MX-5 Miata',
      'Grand Touring',
      2023,
      2_950_000,
      'Convertible',
      'Gasoline',
      'Boca Chica',
      '2.0L I4',
      181,
      'Manual',
      7_800,
      'https://images.unsplash.com/photo-1580273916550-e323be2ae537?w=800&q=75'
    ),
    base(
      'demo-conv-002',
      'Ford',
      'Mustang',
      'Convertible EcoBoost',
      2024,
      3_800_000,
      'Convertible',
      'Gasoline',
      'Distrito Nacional',
      '2.3L Turbo I4',
      330,
      'Automatic',
      4_100,
      'https://images.unsplash.com/photo-1503376780353-7e6692767b70?w=800&q=75'
    ),
    base(
      'demo-conv-003',
      'Chevrolet',
      'Camaro',
      'Convertible SS',
      2023,
      4_500_000,
      'Convertible',
      'Gasoline',
      'Punta Cana',
      '6.2L V8',
      455,
      'Automatic',
      6_200,
      'https://images.unsplash.com/photo-1542362567-b07e54358753?w=800&q=75'
    ),

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Vans (3)  ← usuario pide 2 más
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    base(
      'demo-van-001',
      'Toyota',
      'HiAce',
      'Van GL',
      2023,
      2_650_000,
      'Van',
      'Diesel',
      'Santo Domingo Norte',
      '2.8L Diesel I4',
      150,
      'Manual',
      32_000,
      'https://images.unsplash.com/photo-1519641471654-76ce0107ad1b?w=800&q=75'
    ),
    base(
      'demo-van-002',
      'Ford',
      'Transit',
      'Cargo Van',
      2023,
      2_400_000,
      'Van',
      'Diesel',
      'Santiago',
      '2.0L EcoBlue Diesel',
      170,
      'Automatic',
      28_500,
      'https://images.unsplash.com/photo-1494976388531-d1058494cdd8?w=800&q=75'
    ),
    base(
      'demo-van-003',
      'Nissan',
      'NV350',
      'Urvan Premium',
      2024,
      2_850_000,
      'Van',
      'Diesel',
      'Distrito Nacional',
      '2.5L Diesel I4',
      163,
      'Manual',
      14_700,
      'https://images.unsplash.com/photo-1617788138017-80ad40651399?w=800&q=75'
    ),

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Minivans (3)  ← usuario pide 2 más
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    base(
      'demo-minivan-001',
      'Honda',
      'Odyssey',
      'EX-L',
      2023,
      3_200_000,
      'Minivan',
      'Gasoline',
      'Distrito Nacional',
      '3.5L V6',
      280,
      'Automatic',
      16_400,
      'https://images.unsplash.com/photo-1603584173870-7f23fdae1b7a?w=800&q=75'
    ),
    base(
      'demo-minivan-002',
      'Kia',
      'Carnival',
      'EX',
      2024,
      3_100_000,
      'Minivan',
      'Gasoline',
      'Santiago',
      '3.5L V6',
      290,
      'Automatic',
      5_800,
      'https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=800&q=75'
    ),
    base(
      'demo-minivan-003',
      'Toyota',
      'Sienna',
      'XLE',
      2023,
      3_500_000,
      'Minivan',
      'Hybrid',
      'Santo Domingo',
      '2.5L Hybrid I4',
      245,
      'eCVT',
      12_900,
      'https://images.unsplash.com/photo-1619767886558-efdc259cde1a?w=800&q=75'
    ),

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Híbridos (4)  ← usuario pide 1 más
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    base(
      'demo-hybrid-001',
      'Toyota',
      'Camry',
      'Hybrid XLE',
      2023,
      2_950_000,
      'Sedan',
      'Hybrid',
      'Distrito Nacional',
      '2.5L Hybrid I4',
      208,
      'eCVT',
      9_800,
      'https://images.unsplash.com/photo-1549317661-bd32c8ce0db2?w=800&q=75'
    ),
    base(
      'demo-hybrid-002',
      'Toyota',
      'RAV4',
      'Hybrid XSE',
      2024,
      3_200_000,
      'SUV',
      'Hybrid',
      'Santiago',
      '2.5L Hybrid I4',
      219,
      'eCVT',
      7_200,
      'https://images.unsplash.com/photo-1619767886558-efdc259cde1a?w=800&q=75'
    ),
    base(
      'demo-hybrid-003',
      'Honda',
      'CR-V',
      'Hybrid EX-L',
      2023,
      2_800_000,
      'SUV',
      'Hybrid',
      'Santo Domingo',
      '2.0L Hybrid',
      204,
      'eCVT',
      11_000,
      'https://images.unsplash.com/photo-1603584173870-7f23fdae1b7a?w=800&q=75'
    ),
    base(
      'demo-hybrid-004',
      'Hyundai',
      'Tucson',
      'Hybrid SEL',
      2024,
      2_650_000,
      'SUV',
      'Hybrid',
      'La Vega',
      '1.6L Turbo Hybrid',
      227,
      'Automatic',
      5_400,
      'https://images.unsplash.com/photo-1609521263047-f8f205293f24?w=800&q=75'
    ),

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Eléctricos (3)  ← usuario pide 1 más
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    base(
      'demo-elec-001',
      'Tesla',
      'Model 3',
      'Long Range AWD',
      2023,
      4_200_000,
      'Sedan',
      'Electric',
      'Distrito Nacional',
      'Dual Motor Electric',
      358,
      'Automatic',
      18_500,
      'https://images.unsplash.com/photo-1616422285623-13ff0162193c?w=800&q=75'
    ),
    base(
      'demo-elec-002',
      'Tesla',
      'Model Y',
      'Standard Range',
      2024,
      4_650_000,
      'SUV',
      'Electric',
      'Santo Domingo',
      'Single Motor Electric',
      283,
      'Automatic',
      8_200,
      'https://images.unsplash.com/photo-1556189250-72ba954cfc2b?w=800&q=75'
    ),
    base(
      'demo-elec-003',
      'BYD',
      'Seal',
      'Premium AWD',
      2024,
      3_800_000,
      'Sedan',
      'Electric',
      'Santiago',
      'Dual Motor Electric',
      523,
      'Automatic',
      4_100,
      'https://images.unsplash.com/photo-1542362567-b07e54358753?w=800&q=75'
    ),
  ];
}
