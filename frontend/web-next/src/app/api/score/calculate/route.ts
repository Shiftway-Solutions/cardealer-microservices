import { NextRequest, NextResponse } from 'next/server';
import type {
  VinDecodeResult,
  VinHistoryReport,
  NhtsaRecall,
  NhtsaSafetyRating,
  NhtsaComplaintSummary,
} from '@/types/okla-score';
import { calculateOklaScore, type ScoreInput } from '@/lib/okla-score-engine';
import { DOP_USD_EXCHANGE_RATE } from '@/lib/constants';

// =============================================================================
// BFF: OKLA Score™ Calculate — Orchestrates all APIs + scoring
// =============================================================================

// Gateway URL for backend vehicle-data API
const GATEWAY_URL =
  process.env.GATEWAY_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

export async function POST(request: NextRequest) {
  try {
    const body = await request.json();
    const { vin, listedPriceDOP, declaredMileage, mileageUnit, sellerType } = body;

    if (!vin || vin.length !== 17) {
      return NextResponse.json(
        { success: false, error: 'VIN debe tener exactamente 17 caracteres' },
        { status: 400 }
      );
    }

    const baseUrl = request.nextUrl.origin;

    // Step 1: Decode VIN (prerequisite for everything else)
    const vinDecodeRes = await fetch(`${baseUrl}/api/score/vin-decode?vin=${vin}`);
    const vinDecodeData = await vinDecodeRes.json();

    if (!vinDecodeData.success || !vinDecodeData.data) {
      return NextResponse.json(
        { success: false, error: vinDecodeData.error || 'No se pudo decodificar el VIN' },
        { status: 422 }
      );
    }

    const vinDecode: VinDecodeResult = vinDecodeData.data;

    // Step 2: Parallel calls for recalls, safety, history, market price, AND complaints
    // RETENTION/D1 FIX: Added vehicle history fetch — D1 (25% of score) was always
    // returning baseline 100 pts because history was never fetched.
    // D4 FIX: Added market price fetch — D4 (17% of score) was ALWAYS returning
    // neutral 85/170 because marketPriceDOP was never populated.
    // D2 FIX: Added complaints fetch — D2 (20% of score) now penalizes NHTSA complaints
    const [recallsRes, safetyRes, historyRes, marketPriceRes, complaintsRes, specsRes] =
      await Promise.all([
        fetch(
          `${baseUrl}/api/score/recalls?make=${encodeURIComponent(vinDecode.make)}&model=${encodeURIComponent(vinDecode.model)}&year=${vinDecode.year}`
        ),
        fetch(
          `${baseUrl}/api/score/safety?make=${encodeURIComponent(vinDecode.make)}&model=${encodeURIComponent(vinDecode.model)}&year=${vinDecode.year}`
        ),
        fetchVehicleHistory(vin),
        fetchMarketPrice(vinDecode.make, vinDecode.model, vinDecode.year),
        fetchComplaints(vinDecode.make, vinDecode.model, vinDecode.year),
        fetchVehicleSpecs(vinDecode.make, vinDecode.model, vinDecode.year),
      ]);

    let recalls: NhtsaRecall[] = [];
    let safetyRating: NhtsaSafetyRating | undefined;
    let vinHistory: VinHistoryReport | undefined;
    let marketPriceDOP: number | undefined;
    let marketPriceUSD: number | undefined;
    let complaints: NhtsaComplaintSummary | undefined;
    let safetyFeatures: string[] | undefined;

    try {
      const recallsData = await recallsRes.json();
      if (recallsData.success && recallsData.data) {
        recalls = recallsData.data;
      }
    } catch {
      // Recalls fetch failed, continue without
    }

    try {
      const safetyData = await safetyRes.json();
      if (safetyData.success && safetyData.data) {
        safetyRating = safetyData.data;
      }
    } catch {
      // Safety fetch failed, continue without
    }

    // D2 FIX: Parse complaints for NHTSA complaint penalty
    if (complaintsRes) {
      complaints = complaintsRes;
    }

    // D2 FIX: Extract safety/ADAS features from vehicle specs
    if (specsRes?.standardFeatures) {
      safetyFeatures = specsRes.standardFeatures.filter((f: string) =>
        /lane|blind spot|collision|aeb|cruise|emergency|braking|assist/i.test(f)
      );
    }

    // Map backend history response to frontend VinHistoryReport
    if (historyRes) {
      vinHistory = mapBackendHistoryToFrontend(historyRes);
    }

    // Fetch live exchange rate (best-effort, fallback to constant)
    const liveExchangeRate = await fetchExchangeRate();
    const effectiveRate = liveExchangeRate || DOP_USD_EXCHANGE_RATE;

    // D4 FIX: Parse market price response from backend VehicleDataController
    if (marketPriceRes) {
      marketPriceDOP = marketPriceRes.averagePrice;
      // Backend returns in DOP — convert to USD for reference
      marketPriceUSD = marketPriceDOP ? Math.round(marketPriceDOP / effectiveRate) : undefined;
    }

    // Step 3: Calculate OKLA Score — now with D1 history + D4 market price + D2 complaints!
    const input: ScoreInput = {
      vin,
      vinDecode,
      history: vinHistory,
      listedPriceDOP: listedPriceDOP || 0,
      marketPriceDOP, // D4 FIX: Now populated from backend VehicleDataController
      marketPriceUSD, // D4 FIX: Derived from DOP using live exchange rate
      declaredMileage: declaredMileage || 0,
      mileageUnit: mileageUnit || 'km',
      recalls,
      safetyRating,
      complaints, // D2 FIX: NHTSA complaint penalty
      safetyFeatures, // D2 FIX: ADAS features from Edmunds/specs
      sellerType: sellerType || 'individual',
      exchangeRate: effectiveRate, // FIX B3: Use live rate from BCRD with fallback
    };

    const report = calculateOklaScore(input);

    return NextResponse.json({
      success: true,
      data: report,
      cached: false,
      historyAvailable: !!vinHistory,
    });
  } catch (error) {
    console.error('[OKLA Score Calculate] Error:', error);
    return NextResponse.json(
      { success: false, error: 'Error calculando el OKLA Score' },
      { status: 500 }
    );
  }
}

// =============================================================================
// MARKET PRICE ADAPTER (D4 FIX)
// Fetches from backend VehicleDataController /market-price endpoint
// =============================================================================

interface BackendMarketPriceResponse {
  make: string;
  model: string;
  year: number;
  trim: string | null;
  averagePrice: number; // DOP
  medianPrice: number; // DOP
  minPrice: number; // DOP
  maxPrice: number; // DOP
  currency: string; // "DOP"
  sampleSize: number;
  priceAboveMarket: number | null;
  marketPosition: string;
  depreciationRate: number | null;
  analyzedAt: string;
  provider: string; // "Mock" | "MarketCheck"
}

async function fetchMarketPrice(
  make: string,
  model: string,
  year: number
): Promise<BackendMarketPriceResponse | null> {
  try {
    const url = `${GATEWAY_URL}/api/vehicle-data/market-price/${encodeURIComponent(make)}/${encodeURIComponent(model)}/${year}`;
    const res = await fetch(url, {
      headers: { 'Content-Type': 'application/json' },
      signal: AbortSignal.timeout(5000), // 5s timeout
    });

    if (!res.ok) {
      console.warn(`[Market Price] Backend returned ${res.status} for ${make} ${model} ${year}`);
      return null;
    }

    return await res.json();
  } catch (error) {
    console.warn('[Market Price] Failed to fetch, D4 will use neutral score:', error);
    return null;
  }
}

// =============================================================================
// LIVE EXCHANGE RATE (FIX B3 + BCRD PRIMARY)
// Fetches USD→DOP rate with priority:
//   1. BCRD (Banco Central de la República Dominicana) — authoritative
//   2. ExchangeRate-API — fallback if BCRD is unavailable
//   3. Hardcoded constant — last resort
// Uses in-memory cache with 4-hour TTL.
// =============================================================================

let cachedRate: { rate: number; fetchedAt: number; source: string } | null = null;
const RATE_CACHE_TTL_MS = 4 * 60 * 60 * 1000; // 4 hours

async function fetchExchangeRate(): Promise<number | null> {
  // Return cached if fresh
  if (cachedRate && Date.now() - cachedRate.fetchedAt < RATE_CACHE_TTL_MS) {
    return cachedRate.rate;
  }

  // Priority 1: BCRD (Banco Central de la República Dominicana)
  try {
    const bcrdRate = await fetchBcrdRate();
    if (bcrdRate) {
      cachedRate = { rate: bcrdRate, fetchedAt: Date.now(), source: 'BCRD' };
      return bcrdRate;
    }
  } catch {
    // BCRD unavailable, try fallback
  }

  // Priority 2: ExchangeRate-API (free tier, 1500 req/mo)
  try {
    const res = await fetch('https://open.er-api.com/v6/latest/USD', {
      signal: AbortSignal.timeout(3000), // 3s timeout
    });

    if (res.ok) {
      const data = await res.json();
      const dopRate = data?.rates?.DOP;

      if (dopRate && typeof dopRate === 'number' && dopRate > 40 && dopRate < 100) {
        cachedRate = { rate: dopRate, fetchedAt: Date.now(), source: 'ExchangeRate-API' };
        return dopRate;
      }
    }
  } catch {
    // Exchange rate API failed too
  }

  return null;
}

/**
 * Fetch USD→DOP rate from Banco Central de la República Dominicana.
 * BCRD publishes daily reference rates via their public API.
 */
async function fetchBcrdRate(): Promise<number | null> {
  try {
    // BCRD public API for exchange rates (no API key required)
    const res = await fetch('https://api.bcrd.gob.do/indicadores/tasadecambio', {
      headers: { Accept: 'application/json' },
      signal: AbortSignal.timeout(3000),
    });

    if (!res.ok) return null;

    const data = await res.json();
    // BCRD returns an array of rates; find the USD sell rate (Venta)
    // The response typically has: { data: [{ moneda: "USD", venta: 60.xx, compra: 59.xx }] }
    const usdRate =
      data?.data?.find?.((r: { moneda: string }) => r.moneda === 'USD') ||
      data?.find?.((r: { moneda: string }) => r.moneda === 'USD');

    const ventaRate = usdRate?.venta;
    if (ventaRate && typeof ventaRate === 'number' && ventaRate > 40 && ventaRate < 100) {
      return ventaRate;
    }

    return null;
  } catch {
    return null;
  }
}

// =============================================================================
// VEHICLE HISTORY ADAPTER
// Fetches from backend VehicleDataController and maps to frontend types
// =============================================================================

interface BackendHistoryReport {
  vin: string;
  provider: string;
  reportDate: string;
  titleInfo: { type: string; state: string; issuedDate: string };
  ownershipHistory: Array<{
    ownerNumber: number;
    purchaseDate: string;
    location: string;
    ownerType: string;
  }>;
  serviceHistory: Array<{
    date: string;
    mileage: number;
    type: string;
    description: string;
    facility: string;
  }>;
  accidentHistory: Array<{
    date: string;
    severity: string;
    description: string;
    damageAreas: string[];
    airbagsDeployed: boolean;
  }>;
  titleHistory: Array<{
    date: string;
    type: string;
    state: string;
    mileage: number;
    event: string;
  }>;
  totalOwners: number;
  lastReportedMileage: number;
  isCleanTitle: boolean;
  hasSalvageHistory: boolean;
  hasFloodDamage: boolean;
  hasFrameDamage: boolean;
  isLemonBuyback: boolean;
  isStolenRecord: boolean;
  odometerRollback: boolean; // FIX A2: Backend DetectOdometerRollback() result
  overallScore: number;
}

async function fetchVehicleHistory(vin: string): Promise<BackendHistoryReport | null> {
  try {
    const res = await fetch(`${GATEWAY_URL}/api/vehicle-data/history/${vin}`, {
      headers: { 'Content-Type': 'application/json' },
      signal: AbortSignal.timeout(5000), // 5s timeout — don't block score calc
    });

    if (!res.ok) {
      console.warn(`[Vehicle History] Backend returned ${res.status} for VIN ${vin}`);
      return null;
    }

    const data = await res.json();
    return data;
  } catch (error) {
    console.warn('[Vehicle History] Failed to fetch from backend, D1 will use baseline:', error);
    return null;
  }
}

// =============================================================================
// NHTSA COMPLAINTS ADAPTER (D2 FIX)
// Fetches complaints summary for make/model/year from backend NHTSA endpoint
// =============================================================================

async function fetchComplaints(
  make: string,
  model: string,
  year: number
): Promise<NhtsaComplaintSummary | null> {
  try {
    const url = `${GATEWAY_URL}/api/vehicle-data/nhtsa/complaints/${encodeURIComponent(make)}/${encodeURIComponent(model)}/${year}`;
    const res = await fetch(url, {
      headers: { 'Content-Type': 'application/json' },
      signal: AbortSignal.timeout(5000),
    });

    if (!res.ok) return null;

    const data = await res.json();
    // Backend returns array of NhtsaComplaint objects — summarize for scoring
    if (Array.isArray(data)) {
      const componentBreakdown: Record<string, number> = {};
      for (const complaint of data) {
        const comp = complaint.components || 'Unknown';
        componentBreakdown[comp] = (componentBreakdown[comp] || 0) + 1;
      }
      return {
        totalComplaints: data.length,
        componentBreakdown,
      };
    }

    return null;
  } catch (error) {
    console.warn('[NHTSA Complaints] Failed to fetch, D2 complaints penalty skipped:', error);
    return null;
  }
}

// =============================================================================
// VEHICLE SPECS ADAPTER (D2 FIX)
// Fetches equipment/features for ADAS scoring from backend specs endpoint
// =============================================================================

interface BackendSpecsResponse {
  make: string;
  model: string;
  year: number;
  standardFeatures?: string[];
  safetyFeatures?: string[];
  [key: string]: unknown;
}

async function fetchVehicleSpecs(
  make: string,
  model: string,
  year: number
): Promise<BackendSpecsResponse | null> {
  try {
    const url = `${GATEWAY_URL}/api/vehicle-data/specs/${encodeURIComponent(make)}/${encodeURIComponent(model)}/${year}`;
    const res = await fetch(url, {
      headers: { 'Content-Type': 'application/json' },
      signal: AbortSignal.timeout(5000),
    });

    if (!res.ok) return null;
    return await res.json();
  } catch (error) {
    console.warn('[Vehicle Specs] Failed to fetch, D2 safety tech scoring skipped:', error);
    return null;
  }
}

function mapBackendHistoryToFrontend(backend: BackendHistoryReport): VinHistoryReport {
  // Map title type
  const titleTypeMap: Record<string, VinHistoryReport['titleType']> = {
    Clean: 'Clean',
    Salvage: 'Salvage',
    Rebuilt: 'Rebuilt',
    Flood: 'Flood',
    Junk: 'Junk',
  };
  const titleType = titleTypeMap[backend.titleInfo?.type] || 'Unknown';

  // Determine accident severity from accident history
  let accidentSeverity: VinHistoryReport['accidentSeverity'] = 'None';
  const accidentCount = backend.accidentHistory?.length || 0;
  if (accidentCount > 0) {
    const severities = backend.accidentHistory.map(a => a.severity?.toLowerCase());
    if (severities.includes('severe') || severities.includes('total')) {
      accidentSeverity = 'Severe';
    } else if (severities.includes('moderate')) {
      accidentSeverity = 'Moderate';
    } else {
      accidentSeverity = 'Minor';
    }
  }

  // Map odometer readings from service history
  const odometerReadings = (backend.serviceHistory || [])
    .filter(s => s.mileage > 0)
    .map(s => ({
      date: s.date,
      mileage: s.mileage,
      source: s.facility || 'Service Record',
    }))
    .sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());

  // Check for rental/fleet from ownership history
  const isRentalFleet = (backend.ownershipHistory || []).some(
    o => o.ownerType?.toLowerCase() === 'fleet' || o.ownerType?.toLowerCase() === 'rental'
  );

  return {
    vin: backend.vin,
    titleType,
    totalOwners: backend.totalOwners || 0,
    accidentCount,
    accidentSeverity,
    hasFloodDamage: backend.hasFloodDamage || false,
    hasFrameDamage: backend.hasFrameDamage || false,
    hasHailDamage: false, // Not in current backend model — extend later
    isLemonBuyback: backend.isLemonBuyback || false,
    isRentalFleet,
    isStolenOrCloned: backend.isStolenRecord || false,
    odometerRollback: backend.odometerRollback || false, // FIX A2: propagate backend rollback detection
    odometerReadings,
    serviceRecords: backend.serviceHistory?.length || 0,
    lastReportedMileage: backend.lastReportedMileage,
    lastReportedDate: backend.serviceHistory?.[0]?.date,
  };
}
