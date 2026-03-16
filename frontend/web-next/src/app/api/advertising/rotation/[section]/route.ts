import { NextRequest, NextResponse } from 'next/server';

/**
 * GET /api/advertising/rotation/[section]
 *
 * Dynamic BFF route — handles path-based rotation requests from the
 * frontend advertising service (e.g. /api/advertising/rotation/PremiumSpot).
 *
 * The static route at /api/advertising/rotation?section=X was unreachable
 * via path segments because no middleware rewrite was in place.
 * This dynamic route fixes that transparently.
 *
 * Behaviour:
 *  1. Fetch paid campaign items from AdvertisingService for this section.
 *  2. Transform field names (vehicleTitle → title, vehicleImage → imageUrl, …).
 *  3. Fill remaining slots up to targetCount with quality vehicle data so the
 *     section is never sparse.
 *  4. PremiumSpot → targetCount 16, FeaturedSpot → targetCount 9.
 */

const API_URL =
  process.env.INTERNAL_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:18443';

export const runtime = 'edge';

export async function GET(
  _request: NextRequest,
  { params }: { params: Promise<{ section: string }> }
) {
  const { section: sectionParam } = await params;
  const section = sectionParam || 'FeaturedSpot';
  const isPremium = section.toLowerCase().includes('premium');
  const targetCount = isPremium ? 16 : 9;

  // ── 1. Fetch paid campaign items from AdvertisingService ─────────────────
  let campaignItems: Record<string, unknown>[] = [];
  try {
    const res = await fetch(`${API_URL}/api/advertising/rotation/${section}`, {
      headers: { Accept: 'application/json' },
      next: { revalidate: 300 },
    });
    if (res.ok) {
      const data = await res.json();
      const raw: Record<string, unknown>[] = data?.data?.items ?? [];
      campaignItems = raw
        .map(transformRotatedVehicle)
        .filter(
          v =>
            v.title &&
            v.imageUrl &&
            typeof v.imageUrl === 'string' &&
            !v.imageUrl.startsWith('/images/demo/') &&
            v.imageUrl !== '/placeholder-car.jpg'
        );
    }
  } catch {
    // Backend unavailable — campaignItems stays empty, fill below covers it
  }

  // ── 2. Fill remaining slots with quality vehicles ─────────────────────────
  const slotsNeeded = targetCount - campaignItems.length;
  const campaignVehicleIds = new Set(campaignItems.map(v => String(v.vehicleId || '')));

  const fillItems =
    slotsNeeded > 0
      ? await fetchFillVehicles(section, isPremium, slotsNeeded * 3, campaignVehicleIds)
      : [];

  // Campaigns always first, then fill
  const allItems = [...campaignItems, ...fillItems].slice(0, targetCount);

  // ── 3. Return (or demo fallback if completely empty) ──────────────────────
  if (allItems.length === 0) {
    return NextResponse.json({
      success: true,
      data: {
        items: getDemoItems(section, isPremium, targetCount),
        section,
        rotatedAt: new Date().toISOString(),
        source: 'demo',
      },
    });
  }

  return NextResponse.json({
    success: true,
    data: {
      items: allItems,
      section,
      rotatedAt: new Date().toISOString(),
      source: campaignItems.length > 0 ? 'campaigns+fill' : 'fill',
    },
  });
}

// ─────────────────────────────────────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────────────────────────────────────

function transformRotatedVehicle(item: Record<string, unknown>): Record<string, unknown> {
  const placementType = String(item.placementType || '');
  const isPremium = placementType.toLowerCase().includes('premium');
  return {
    ...item,
    title: item.title || item.vehicleTitle || '',
    imageUrl: item.imageUrl || item.vehicleImage || '',
    price: item.price || item.vehiclePrice || 0,
    slug:
      item.slug ||
      `${String(item.vehicleYear || '')}-${String(item.vehicleMake || '').toLowerCase()}-${String(item.vehicleModel || '').toLowerCase()}-${String(
        item.vehicleId || ''
      )
        .replace(/-/g, '')
        .slice(0, 8)}`.replace(/\s+/g, '-'),
    isFeatured: item.isFeatured ?? !isPremium,
    isPremium: item.isPremium ?? isPremium,
  };
}

/** Fetch fresh vehicles to fill remaining rotation slots. */
async function fetchFillVehicles(
  section: string,
  isPremium: boolean,
  fetchCount: number,
  excludeIds: Set<string>
): Promise<Record<string, unknown>[]> {
  try {
    const sortBy = isPremium ? 'price_desc' : 'newest';
    const url = `${API_URL}/api/vehicles?pageSize=${Math.min(fetchCount, 50)}&page=1&sortBy=${sortBy}&status=Active`;
    const res = await fetch(url, {
      headers: { Accept: 'application/json' },
      next: { revalidate: 300 },
    });
    if (!res.ok) return [];

    const data = await res.json();
    const vehicles: Record<string, unknown>[] = data?.vehicles ?? data?.data?.vehicles ?? [];

    // Exclude vehicles already shown via paid campaigns
    const filtered = vehicles.filter(v => !excludeIds.has(String(v.id || '')));

    // Score and rank using WeightedRandom (same algorithm as static route)
    const timeSeed = Math.floor(Date.now() / (5 * 60 * 1000));
    const sectionSeed = section.split('').reduce((acc, c) => acc + c.charCodeAt(0), 0);
    let seed = timeSeed * 31 + sectionSeed;
    const seededRandom = () => {
      seed |= 0;
      seed = (seed + 0x6d2b79f5) | 0;
      let t = Math.imul(seed ^ (seed >>> 15), 1 | seed);
      t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
      return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };

    const scored = filtered.map(v => {
      const images = (v.images as Record<string, unknown>[]) ?? [];
      const photoScore = Math.min(images.length / 10, 1) * 100;
      const year = (v.year as number) || 2020;
      const price = (v.price as number) || 0;
      const recencyScore = Math.min((year - 2018) / 8, 1) * 100;
      const priceScore = price > 0 ? Math.min(price / 5_000_000, 1) * 100 : 50;
      const qualityScore = Math.round(photoScore * 0.35 + recencyScore * 0.4 + priceScore * 0.25);
      return { v, qualityScore, images };
    });

    const selected: typeof scored = [];
    const pool = [...scored];
    while (selected.length < fetchCount && pool.length > 0) {
      const weights = pool.map(item => item.qualityScore + seededRandom() * 30);
      const total = weights.reduce((s, w) => s + w, 0);
      let r = seededRandom() * total;
      let idx = 0;
      for (let i = 0; i < weights.length; i++) {
        r -= weights[i];
        if (r <= 0) {
          idx = i;
          break;
        }
      }
      selected.push(pool[idx]);
      pool.splice(idx, 1);
    }

    return selected.map(({ v, qualityScore, images }, i) => {
      const sorted = [...images].sort((a, b) => {
        const aO = ((a as Record<string, unknown>).sortOrder as number) ?? 99;
        const bO = ((b as Record<string, unknown>).sortOrder as number) ?? 99;
        return aO - bO;
      });
      const imgUrl =
        String((sorted[0] as Record<string, unknown>)?.url ?? '') || '/placeholder-car.jpg';
      const year = v.year || '';
      const make = String(v.make || '').toLowerCase();
      const model = String(v.model || '').toLowerCase();
      const shortId = String(v.id || '')
        .replace(/-/g, '')
        .slice(0, 8);
      return {
        campaignId: `fill-${isPremium ? 'p' : 'f'}${i + 1}`,
        vehicleId: v.id || '',
        title: v.title || `${year} ${v.make} ${v.model}`,
        slug: `${year}-${make}-${model}-${shortId}`.replace(/\s+/g, '-'),
        imageUrl: imgUrl,
        price: v.price || 0,
        currency: v.currency || 'DOP',
        qualityScore,
        location: String(v.city || v.state || 'República Dominicana'),
        isFeatured: !isPremium,
        isPremium,
        placementType: section,
        position: i + 1,
        rotationAlgorithm: 'WeightedRandom',
      };
    });
  } catch {
    return [];
  }
}

/** Minimal demo fallback so the section is never blank in dev/staging. */
function getDemoItems(
  section: string,
  isPremium: boolean,
  count: number
): Record<string, unknown>[] {
  const demos = [
    { make: 'Toyota', model: 'RAV4', year: 2024, price: 2_850_000 },
    { make: 'Honda', model: 'CR-V', year: 2023, price: 1_950_000 },
    { make: 'Hyundai', model: 'Tucson', year: 2024, price: 2_200_000 },
    { make: 'Ford', model: 'Explorer', year: 2023, price: 3_500_000 },
    { make: 'BMW', model: 'X5', year: 2023, price: 6_800_000 },
    { make: 'Mercedes-Benz', model: 'GLE', year: 2024, price: 8_500_000 },
  ];
  return Array.from({ length: count }, (_, i) => {
    const d = demos[i % demos.length];
    return {
      campaignId: `demo-${i + 1}`,
      vehicleId: `demo-${section}-${i + 1}`,
      title: `${d.year} ${d.make} ${d.model}`,
      imageUrl: '/placeholder-car.jpg',
      price: d.price,
      currency: 'DOP',
      location: 'Santo Domingo, DN',
      isFeatured: !isPremium,
      isPremium,
      placementType: section,
      position: i + 1,
    };
  });
}
