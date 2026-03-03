import { NextRequest, NextResponse } from 'next/server';
import type { VinDecodeResult, NhtsaRecall, NhtsaSafetyRating } from '@/types/okla-score';
import { calculateOklaScore, type ScoreInput } from '@/lib/okla-score-engine';

// =============================================================================
// BFF: OKLA Score™ Calculate — Orchestrates all APIs + scoring
// =============================================================================

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

    // Step 1: Decode VIN (parallel with nothing — it's the prerequisite)
    const vinDecodeRes = await fetch(`${baseUrl}/api/score/vin-decode?vin=${vin}`);
    const vinDecodeData = await vinDecodeRes.json();

    if (!vinDecodeData.success || !vinDecodeData.data) {
      return NextResponse.json(
        { success: false, error: vinDecodeData.error || 'No se pudo decodificar el VIN' },
        { status: 422 }
      );
    }

    const vinDecode: VinDecodeResult = vinDecodeData.data;

    // Step 2: Parallel calls for recalls, safety, complaints
    const [recallsRes, safetyRes] = await Promise.all([
      fetch(
        `${baseUrl}/api/score/recalls?make=${encodeURIComponent(vinDecode.make)}&model=${encodeURIComponent(vinDecode.model)}&year=${vinDecode.year}`
      ),
      fetch(
        `${baseUrl}/api/score/safety?make=${encodeURIComponent(vinDecode.make)}&model=${encodeURIComponent(vinDecode.model)}&year=${vinDecode.year}`
      ),
    ]);

    let recalls: NhtsaRecall[] = [];
    let safetyRating: NhtsaSafetyRating | undefined;

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

    // Step 3: Calculate OKLA Score
    const input: ScoreInput = {
      vin,
      vinDecode,
      listedPriceDOP: listedPriceDOP || 0,
      declaredMileage: declaredMileage || 0,
      mileageUnit: mileageUnit || 'km',
      recalls,
      safetyRating,
      sellerType: sellerType || 'individual',
      exchangeRate: 58.5, // TODO: fetch live from BCRD or ExchangeRate-API
    };

    const report = calculateOklaScore(input);

    return NextResponse.json({
      success: true,
      data: report,
      cached: false,
    });
  } catch (error) {
    console.error('[OKLA Score Calculate] Error:', error);
    return NextResponse.json(
      { success: false, error: 'Error calculando el OKLA Score' },
      { status: 500 }
    );
  }
}
