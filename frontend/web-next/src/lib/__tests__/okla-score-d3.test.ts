import { describe, it, expect } from 'vitest';
import { calculateOklaScore, type ScoreInput } from '../okla-score-engine';
import type { VinHistoryReport, OdometerReading, VinDecodeResult } from '@/types/okla-score';

// =============================================================================
// D3 — Mileage / Odometer (18%, max 180 pts) Audit Tests
// =============================================================================
// Validates that D3 correctly implements the OKLA Score spec:
//
//   MILE → KM CONVERSION:
//     factor 1.60934: km / 1.60934 = miles (used when mileageUnit = 'km')
//
//   ZERO/MISSING MILEAGE:
//     Returns 50 pts with "No mileage declared" factor (-40 impact)
//
//   AGE-ADJUSTED SCORING (miles per year):
//     ≤ 8,000 mi/yr ... 180 pts (Excellent)
//     ≤ 12,000 mi/yr .. 150 pts (Good — market average)
//     ≤ 15,000 mi/yr .. 120 pts (Above average)
//     ≤ 20,000 mi/yr ..  80 pts (High usage)
//     ≤ 25,000 mi/yr ..  40 pts (Very high)
//     > 25,000 mi/yr ..  10 pts (Extreme / commercial)
//
//   ODOMETER ROLLBACK (backend confirmed via odometerRollback flag):
//     raw = 0 pts, immediate return with factor
//
//   DIRECTIONAL DISCREPANCY:
//     Declared < 85% of lastReportedMileage → FRAUD SUSPECTED → 0 pts
//     Declared > lastReported with >200 mi/day rate → -60 pts (rapid increase)
//
//   SEQUENTIAL ANALYSIS (odometerReadings):
//     If reading[i].mileage < reading[i-1].mileage - 500 → rollback → 0 pts
//
//   Score clamped to [0, 180]
//
// All tests use mocked VinHistoryReport — NO real VinAudit/CARFAX calls.
// =============================================================================

const CURRENT_YEAR = new Date().getFullYear();

/** Build a default VinHistoryReport */
function makeHistory(overrides: Partial<VinHistoryReport> = {}): VinHistoryReport {
  return {
    vin: '1HGBH41JXMN109186',
    titleType: 'Clean',
    totalOwners: 1,
    accidentCount: 0,
    accidentSeverity: 'None',
    hasFloodDamage: false,
    hasFrameDamage: false,
    hasHailDamage: false,
    isLemonBuyback: false,
    isRentalFleet: false,
    isStolenOrCloned: false,
    odometerRollback: false,
    odometerReadings: [],
    serviceRecords: 3,
    ...overrides,
  };
}

/** Build a minimal valid ScoreInput */
function makeInput(overrides: Partial<ScoreInput> = {}): ScoreInput {
  return {
    vin: '1HGBH41JXMN109186',
    vinDecode: {
      vin: '1HGBH41JXMN109186',
      make: 'Toyota',
      model: 'Corolla',
      year: CURRENT_YEAR - 5, // 5-year-old vehicle by default
      engineType: 'Gasoline',
      engineCylinders: 4,
      displacementL: 2.0,
      transmission: 'Automatic',
      drivetrain: 'FWD',
    },
    listedPriceDOP: 1_200_000,
    marketPriceDOP: 1_200_000,
    declaredMileage: 50_000,
    mileageUnit: 'km' as const,
    sellerType: 'individual' as const,
    history: makeHistory(),
    ...overrides,
  };
}

/** Extract D3 from the full score report */
function getD3(input: ScoreInput) {
  const report = calculateOklaScore(input);
  return report.dimensions.find(d => d.dimension === 'D3')!;
}

// ─── Mile ↔ Km Conversion ──────────────────────────────────────

describe('D3 — Mile/Km Conversion (factor 1.60934)', () => {
  it('should convert km to miles internally using /1.60934', () => {
    // 50,000 km ÷ 1.60934 = ~31,069 miles
    // Vehicle: 5 years old → ~6,214 mi/yr → ≤8,000 → 180 pts
    const d3 = getD3(
      makeInput({
        declaredMileage: 50_000,
        mileageUnit: 'km',
        vinDecode: {
          vin: 'x',
          make: 'Toyota',
          model: 'Corolla',
          year: CURRENT_YEAR - 5,
        },
      })
    );
    expect(d3.rawScore).toBe(180); // ~6,214 mi/yr is ≤8,000
  });

  it('should use mileage directly when unit is miles', () => {
    // 50,000 miles / 5 years = 10,000 mi/yr → ≤12,000 → 150 pts
    const d3 = getD3(
      makeInput({
        declaredMileage: 50_000,
        mileageUnit: 'miles',
        vinDecode: {
          vin: 'x',
          make: 'Toyota',
          model: 'Corolla',
          year: CURRENT_YEAR - 5,
        },
      })
    );
    expect(d3.rawScore).toBe(150); // 10,000 mi/yr is ≤12,000
  });

  it('should produce same score for equivalent km and miles values', () => {
    // 80,467 km = 50,000 miles exactly
    const d3_km = getD3(
      makeInput({
        declaredMileage: 80_467,
        mileageUnit: 'km',
        vinDecode: { vin: 'x', make: 'Toyota', model: 'Corolla', year: CURRENT_YEAR - 5 },
      })
    );
    const d3_mi = getD3(
      makeInput({
        declaredMileage: 50_000,
        mileageUnit: 'miles',
        vinDecode: { vin: 'x', make: 'Toyota', model: 'Corolla', year: CURRENT_YEAR - 5 },
      })
    );
    expect(d3_km.rawScore).toBe(d3_mi.rawScore);
  });

  it('should handle atypical range: dealer enters miles value in km field', () => {
    // Dealer enters 50,000 (meant miles) but marks as km
    // Engine interprets as 50,000 km ÷ 1.60934 = ~31,069 mi → 6,214 mi/yr ≤ 8k
    // vs correct 50,000 mi → 10,000 mi/yr ≤ 12k
    // This shows the km value produces a HIGHER score (lower apparent mileage)
    const d3_wrong_unit = getD3(
      makeInput({
        declaredMileage: 50_000,
        mileageUnit: 'km',
        vinDecode: { vin: 'x', make: 'Toyota', model: 'Corolla', year: CURRENT_YEAR - 5 },
      })
    );
    const d3_correct_unit = getD3(
      makeInput({
        declaredMileage: 50_000,
        mileageUnit: 'miles',
        vinDecode: { vin: 'x', make: 'Toyota', model: 'Corolla', year: CURRENT_YEAR - 5 },
      })
    );
    // Wrong unit (km when meant miles) gives a higher score — the system should flag this
    expect(d3_wrong_unit.rawScore).toBeGreaterThanOrEqual(d3_correct_unit.rawScore);
  });
});

// ─── Zero/Missing Mileage ──────────────────────────────────────

describe('D3 — Zero/Missing Mileage', () => {
  it('should return 50 pts for zero mileage', () => {
    const d3 = getD3(makeInput({ declaredMileage: 0 }));
    expect(d3.rawScore).toBe(50);
  });

  it('should return 50 pts for negative mileage', () => {
    const d3 = getD3(makeInput({ declaredMileage: -1000 }));
    expect(d3.rawScore).toBe(50);
  });

  it('should include "No mileage declared" factor', () => {
    const d3 = getD3(makeInput({ declaredMileage: 0 }));
    const factor = d3.factors.find(f => f.name === 'No mileage declared');
    expect(factor).toBeDefined();
    expect(factor!.impact).toBe(-40);
    expect(factor!.source).toBe('OKLA Validation');
  });

  it('should have Spanish translation for no mileage', () => {
    const d3 = getD3(makeInput({ declaredMileage: 0 }));
    const factor = d3.factors.find(f => f.name === 'No mileage declared');
    expect(factor!.nameEs).toBe('Sin kilometraje declarado');
  });
});

// ─── Age-Adjusted Mileage Scoring (mi/yr tiers) ────────────────

describe('D3 — Age-Adjusted Mileage Scoring', () => {
  it('should give 180 pts for ≤8,000 mi/yr (excellent)', () => {
    // 5-year vehicle, 30,000 miles = 6,000 mi/yr
    const d3 = getD3(
      makeInput({
        declaredMileage: 30_000,
        mileageUnit: 'miles',
        vinDecode: { vin: 'x', make: 'Toyota', model: 'Corolla', year: CURRENT_YEAR - 5 },
      })
    );
    expect(d3.rawScore).toBe(180);
  });

  it('should give 150 pts for ≤12,000 mi/yr (good — market average)', () => {
    // 5-year vehicle, 50,000 miles = 10,000 mi/yr
    const d3 = getD3(
      makeInput({
        declaredMileage: 50_000,
        mileageUnit: 'miles',
        vinDecode: { vin: 'x', make: 'Toyota', model: 'Corolla', year: CURRENT_YEAR - 5 },
      })
    );
    expect(d3.rawScore).toBe(150);
  });

  it('should give 120 pts for ≤15,000 mi/yr (above average)', () => {
    // 5-year vehicle, 70,000 miles = 14,000 mi/yr
    const d3 = getD3(
      makeInput({
        declaredMileage: 70_000,
        mileageUnit: 'miles',
        vinDecode: { vin: 'x', make: 'Toyota', model: 'Corolla', year: CURRENT_YEAR - 5 },
      })
    );
    expect(d3.rawScore).toBe(120);
  });

  it('should give 80 pts for ≤20,000 mi/yr (high usage)', () => {
    // 5-year vehicle, 90,000 miles = 18,000 mi/yr
    const d3 = getD3(
      makeInput({
        declaredMileage: 90_000,
        mileageUnit: 'miles',
        vinDecode: { vin: 'x', make: 'Toyota', model: 'Corolla', year: CURRENT_YEAR - 5 },
      })
    );
    expect(d3.rawScore).toBe(80);
  });

  it('should give 40 pts for ≤25,000 mi/yr (very high)', () => {
    // 5-year vehicle, 120,000 miles = 24,000 mi/yr
    const d3 = getD3(
      makeInput({
        declaredMileage: 120_000,
        mileageUnit: 'miles',
        vinDecode: { vin: 'x', make: 'Toyota', model: 'Corolla', year: CURRENT_YEAR - 5 },
      })
    );
    expect(d3.rawScore).toBe(40);
  });

  it('should give 10 pts for >25,000 mi/yr (extreme/commercial)', () => {
    // 5-year vehicle, 150,000 miles = 30,000 mi/yr
    const d3 = getD3(
      makeInput({
        declaredMileage: 150_000,
        mileageUnit: 'miles',
        vinDecode: { vin: 'x', make: 'Toyota', model: 'Corolla', year: CURRENT_YEAR - 5 },
      })
    );
    expect(d3.rawScore).toBe(10);
  });

  it('should treat 1-year-old vehicle minimum age as 1 (prevent div by 0)', () => {
    // Current-year vehicle, 5,000 miles = 5,000 mi/yr → ≤8,000 → 180
    const d3 = getD3(
      makeInput({
        declaredMileage: 5_000,
        mileageUnit: 'miles',
        vinDecode: { vin: 'x', make: 'Toyota', model: 'Corolla', year: CURRENT_YEAR },
      })
    );
    expect(d3.rawScore).toBe(180);
  });

  it('should include mileage factor with mi and mi/yr in description', () => {
    const d3 = getD3(
      makeInput({
        declaredMileage: 50_000,
        mileageUnit: 'miles',
        vinDecode: { vin: 'x', make: 'Toyota', model: 'Corolla', year: CURRENT_YEAR - 5 },
      })
    );
    const factor = d3.factors.find(f => f.name.includes('Mileage'));
    expect(factor).toBeDefined();
    expect(factor!.description).toContain('mi/yr');
    expect(factor!.source).toBe('Seller declaration');
  });

  it('should include Spanish km/año in factor nameEs', () => {
    const d3 = getD3(
      makeInput({
        declaredMileage: 50_000,
        mileageUnit: 'miles',
        vinDecode: { vin: 'x', make: 'Toyota', model: 'Corolla', year: CURRENT_YEAR - 5 },
      })
    );
    const factor = d3.factors.find(f => f.name.includes('Mileage'));
    expect(factor!.nameEs).toContain('km/año');
  });
});

// ─── Odometer Rollback Detection ────────────────────────────────

describe('D3 — Odometer Rollback (Backend Confirmed)', () => {
  it('should return 0 pts when odometerRollback flag is true', () => {
    const d3 = getD3(
      makeInput({
        declaredMileage: 30_000,
        mileageUnit: 'miles',
        history: makeHistory({ odometerRollback: true }),
      })
    );
    expect(d3.rawScore).toBe(0);
  });

  it('should include ODOMETER ROLLBACK DETECTED factor', () => {
    const d3 = getD3(
      makeInput({
        declaredMileage: 30_000,
        mileageUnit: 'miles',
        history: makeHistory({ odometerRollback: true }),
      })
    );
    const factor = d3.factors.find(f => f.name === 'ODOMETER ROLLBACK DETECTED');
    expect(factor).toBeDefined();
    expect(factor!.impact).toBe(-180);
    expect(factor!.source).toBe('VinAudit/NMVTIS');
  });

  it('should have Spanish translation for rollback', () => {
    const d3 = getD3(
      makeInput({
        declaredMileage: 30_000,
        mileageUnit: 'miles',
        history: makeHistory({ odometerRollback: true }),
      })
    );
    const factor = d3.factors.find(f => f.name === 'ODOMETER ROLLBACK DETECTED');
    expect(factor!.nameEs).toBe('RETROCESO DE ODÓMETRO DETECTADO');
  });
});

// ─── Directional Discrepancy (Declared vs Last Reported) ────────

describe('D3 — Directional Discrepancy Check', () => {
  it('should return 0 pts when declared < 85% of lastReportedMileage (fraud suspected)', () => {
    // Last reported: 60,000 mi. Declared: 30,000 mi (50% of last → < 85%)
    const d3 = getD3(
      makeInput({
        declaredMileage: 30_000,
        mileageUnit: 'miles',
        history: makeHistory({
          lastReportedMileage: 60_000,
          lastReportedDate: '2024-01-01',
        }),
      })
    );
    expect(d3.rawScore).toBe(0);
  });

  it('should include ODOMETER FRAUD SUSPECTED factor', () => {
    const d3 = getD3(
      makeInput({
        declaredMileage: 20_000,
        mileageUnit: 'miles',
        history: makeHistory({ lastReportedMileage: 50_000 }),
      })
    );
    const factor = d3.factors.find(f => f.name === 'ODOMETER FRAUD SUSPECTED');
    expect(factor).toBeDefined();
    expect(factor!.impact).toBe(-180);
    expect(factor!.source).toBe('VinAudit/NMVTIS');
  });

  it('should NOT flag when declared >= 85% of lastReported (normal increase)', () => {
    // Last: 50,000 mi. Declared: 55,000 mi (110% → clearly above 85%)
    const d3 = getD3(
      makeInput({
        declaredMileage: 55_000,
        mileageUnit: 'miles',
        vinDecode: { vin: 'x', make: 'Toyota', model: 'Corolla', year: CURRENT_YEAR - 5 },
        history: makeHistory({
          lastReportedMileage: 50_000,
          lastReportedDate: '2024-01-01',
        }),
      })
    );
    expect(d3.rawScore).toBeGreaterThan(0);
    const fraudFactor = d3.factors.find(f => f.name === 'ODOMETER FRAUD SUSPECTED');
    expect(fraudFactor).toBeUndefined();
  });

  it('should penalize unusually rapid mileage increase (>200 mi/day)', () => {
    // Last: 40,000 mi reported 30 days ago. Declared: 50,000 mi = 10k in 30 days = 333 mi/day
    const thirtyDaysAgo = new Date(Date.now() - 30 * 86400000).toISOString().split('T')[0];
    const d3 = getD3(
      makeInput({
        declaredMileage: 50_000,
        mileageUnit: 'miles',
        vinDecode: { vin: 'x', make: 'Toyota', model: 'Corolla', year: CURRENT_YEAR - 5 },
        history: makeHistory({
          lastReportedMileage: 40_000,
          lastReportedDate: thirtyDaysAgo,
        }),
      })
    );
    const rapidFactor = d3.factors.find(f => f.name.includes('rapid mileage'));
    expect(rapidFactor).toBeDefined();
    expect(rapidFactor!.impact).toBe(-60);
  });

  it('should have Spanish translation for fraud factor', () => {
    const d3 = getD3(
      makeInput({
        declaredMileage: 20_000,
        mileageUnit: 'miles',
        history: makeHistory({ lastReportedMileage: 50_000 }),
      })
    );
    const factor = d3.factors.find(f => f.name === 'ODOMETER FRAUD SUSPECTED');
    expect(factor!.nameEs).toBe('SOSPECHA DE FRAUDE DE ODÓMETRO');
  });
});

// ─── Sequential Odometer Analysis (odometerReadings) ────────────

describe('D3 — Sequential Odometer Readings Analysis', () => {
  it('should detect rollback when reading[i] < reading[i-1] - 500', () => {
    const readings: OdometerReading[] = [
      { date: '2021-01-01', mileage: 20_000, source: 'Service' },
      { date: '2022-01-01', mileage: 35_000, source: 'Service' },
      { date: '2023-01-01', mileage: 28_000, source: 'DMV' }, // Decrease >500
    ];
    const d3 = getD3(
      makeInput({
        declaredMileage: 30_000,
        mileageUnit: 'miles',
        history: makeHistory({ odometerReadings: readings }),
      })
    );
    expect(d3.rawScore).toBe(0);
    const factor = d3.factors.find(f => f.name === 'SEQUENTIAL ROLLBACK IN RECORDS');
    expect(factor).toBeDefined();
    expect(factor!.impact).toBe(-180);
    expect(factor!.source).toBe('Service Records');
  });

  it('should NOT flag normal increasing readings', () => {
    const readings: OdometerReading[] = [
      { date: '2021-01-01', mileage: 20_000, source: 'Service' },
      { date: '2022-01-01', mileage: 35_000, source: 'Service' },
      { date: '2023-01-01', mileage: 48_000, source: 'DMV' },
    ];
    const d3 = getD3(
      makeInput({
        declaredMileage: 50_000,
        mileageUnit: 'miles',
        vinDecode: { vin: 'x', make: 'Toyota', model: 'Corolla', year: CURRENT_YEAR - 5 },
        history: makeHistory({ odometerReadings: readings }),
      })
    );
    const rollbackFactor = d3.factors.find(f => f.name === 'SEQUENTIAL ROLLBACK IN RECORDS');
    expect(rollbackFactor).toBeUndefined();
    expect(d3.rawScore).toBeGreaterThan(0);
  });

  it('should tolerate small decrease ≤500 mi (measurement variance)', () => {
    const readings: OdometerReading[] = [
      { date: '2022-06-01', mileage: 40_000, source: 'Service' },
      { date: '2023-01-01', mileage: 39_600, source: 'DMV' }, // Only 400 mi decrease (≤500)
    ];
    const d3 = getD3(
      makeInput({
        declaredMileage: 45_000,
        mileageUnit: 'miles',
        vinDecode: { vin: 'x', make: 'Toyota', model: 'Corolla', year: CURRENT_YEAR - 5 },
        history: makeHistory({ odometerReadings: readings }),
      })
    );
    const rollbackFactor = d3.factors.find(f => f.name === 'SEQUENTIAL ROLLBACK IN RECORDS');
    expect(rollbackFactor).toBeUndefined();
  });

  it('should detect rollback in unsorted readings (sorted by date internally)', () => {
    // Provided out-of-order but dates reveal the real sequence
    const readings: OdometerReading[] = [
      { date: '2023-01-01', mileage: 25_000, source: 'DMV' }, // Latest
      { date: '2021-01-01', mileage: 20_000, source: 'Service' }, // Earliest
      { date: '2022-01-01', mileage: 40_000, source: 'Service' }, // Middle
    ];
    // Sorted by date: 20k → 40k → 25k (25k < 40k - 500) → ROLLBACK
    const d3 = getD3(
      makeInput({
        declaredMileage: 30_000,
        mileageUnit: 'miles',
        history: makeHistory({ odometerReadings: readings }),
      })
    );
    expect(d3.rawScore).toBe(0);
    const factor = d3.factors.find(f => f.name === 'SEQUENTIAL ROLLBACK IN RECORDS');
    expect(factor).toBeDefined();
  });

  it('should require at least 2 readings for sequential analysis', () => {
    // Single reading — cannot compare — no rollback detection
    const readings: OdometerReading[] = [
      { date: '2022-01-01', mileage: 30_000, source: 'Service' },
    ];
    const d3 = getD3(
      makeInput({
        declaredMileage: 50_000,
        mileageUnit: 'miles',
        vinDecode: { vin: 'x', make: 'Toyota', model: 'Corolla', year: CURRENT_YEAR - 5 },
        history: makeHistory({ odometerReadings: readings }),
      })
    );
    const rollbackFactor = d3.factors.find(f => f.name === 'SEQUENTIAL ROLLBACK IN RECORDS');
    expect(rollbackFactor).toBeUndefined();
  });
});

// ─── Km/Year Average vs Expected ────────────────────────────────

describe('D3 — Km/Year Average Calculation', () => {
  it('should compute mi/yr correctly for a 5-year vehicle', () => {
    // 60,000 miles / 5 years = 12,000 mi/yr → ≤12,000 → 150 pts
    const d3 = getD3(
      makeInput({
        declaredMileage: 60_000,
        mileageUnit: 'miles',
        vinDecode: { vin: 'x', make: 'Toyota', model: 'Corolla', year: CURRENT_YEAR - 5 },
      })
    );
    expect(d3.rawScore).toBe(150);
  });

  it('should compute mi/yr correctly for a 10-year vehicle', () => {
    // 100,000 miles / 10 years = 10,000 mi/yr → ≤12,000 → 150 pts
    const d3 = getD3(
      makeInput({
        declaredMileage: 100_000,
        mileageUnit: 'miles',
        vinDecode: { vin: 'x', make: 'Toyota', model: 'Corolla', year: CURRENT_YEAR - 10 },
      })
    );
    expect(d3.rawScore).toBe(150);
  });

  it('should score higher for lower km/year average', () => {
    // Vehicle A: 20,000 mi / 5yr = 4,000 mi/yr → 180
    // Vehicle B: 80,000 mi / 5yr = 16,000 mi/yr → 80
    const d3_low = getD3(
      makeInput({
        declaredMileage: 20_000,
        mileageUnit: 'miles',
        vinDecode: { vin: 'x', make: 'Toyota', model: 'Camry', year: CURRENT_YEAR - 5 },
      })
    );
    const d3_high = getD3(
      makeInput({
        declaredMileage: 80_000,
        mileageUnit: 'miles',
        vinDecode: { vin: 'x', make: 'Toyota', model: 'Camry', year: CURRENT_YEAR - 5 },
      })
    );
    expect(d3_low.rawScore).toBeGreaterThan(d3_high.rawScore);
  });

  it('should handle km input with correct mi/yr calculation', () => {
    // 96,560 km ÷ 1.60934 ≈ 60,000 miles / 5 years = 12,000 mi/yr → 150
    const d3 = getD3(
      makeInput({
        declaredMileage: 96_560,
        mileageUnit: 'km',
        vinDecode: { vin: 'x', make: 'Toyota', model: 'Corolla', year: CURRENT_YEAR - 5 },
      })
    );
    expect(d3.rawScore).toBe(150);
  });

  it('should handle very old vehicle (20 years) with moderate mileage as low mi/yr', () => {
    // 100,000 miles / 20 years = 5,000 mi/yr → ≤8,000 → 180
    const d3 = getD3(
      makeInput({
        declaredMileage: 100_000,
        mileageUnit: 'miles',
        vinDecode: { vin: 'x', make: 'Toyota', model: 'Corolla', year: CURRENT_YEAR - 20 },
      })
    );
    expect(d3.rawScore).toBe(180);
  });
});

// ─── Score Clamping [0, 180] ────────────────────────────────────

describe('D3 — Score Clamping', () => {
  it('should never exceed 180 pts', () => {
    const d3 = getD3(
      makeInput({
        declaredMileage: 5_000,
        mileageUnit: 'miles',
        vinDecode: { vin: 'x', make: 'Toyota', model: 'Corolla', year: CURRENT_YEAR },
      })
    );
    expect(d3.rawScore).toBeLessThanOrEqual(180);
  });

  it('should never go below 0 pts', () => {
    const d3 = getD3(
      makeInput({
        declaredMileage: 30_000,
        mileageUnit: 'miles',
        history: makeHistory({ odometerRollback: true }),
      })
    );
    expect(d3.rawScore).toBeGreaterThanOrEqual(0);
    expect(d3.rawScore).toBe(0);
  });

  it('D3 dimension always present in full score report', () => {
    const report = calculateOklaScore(makeInput({}));
    const d3 = report.dimensions.find(d => d.dimension === 'D3');
    expect(d3).toBeDefined();
    expect(d3!.maxPoints).toBe(180);
  });
});

// ─── Edge Cases & Combinations ──────────────────────────────────

describe('D3 — Edge Cases', () => {
  it('should handle rollback flag AND sequential readings (flag takes priority)', () => {
    // odometerRollback=true causes immediate return before readings analysis
    const readings: OdometerReading[] = [
      { date: '2021-01-01', mileage: 20_000, source: 'Service' },
      { date: '2022-01-01', mileage: 30_000, source: 'Service' },
    ];
    const d3 = getD3(
      makeInput({
        declaredMileage: 30_000,
        mileageUnit: 'miles',
        history: makeHistory({ odometerRollback: true, odometerReadings: readings }),
      })
    );
    expect(d3.rawScore).toBe(0);
    const rollbackFactor = d3.factors.find(f => f.name === 'ODOMETER ROLLBACK DETECTED');
    expect(rollbackFactor).toBeDefined();
  });

  it('should handle no history at all (no rollback detection possible)', () => {
    // Without history, D3 only scores based on declared mileage and age
    const d3 = getD3(
      makeInput({
        declaredMileage: 40_000,
        mileageUnit: 'miles',
        history: undefined,
        vinDecode: { vin: 'x', make: 'Toyota', model: 'Corolla', year: CURRENT_YEAR - 5 },
      })
    );
    // 40k mi / 5yr = 8,000 mi/yr → exactly ≤ 8,000 → 180
    expect(d3.rawScore).toBe(180);
    const rollbackFactor = d3.factors.find(f => f.name === 'ODOMETER ROLLBACK DETECTED');
    expect(rollbackFactor).toBeUndefined();
  });

  it('should combine discrepancy + sequential rollback', () => {
    // Both fraud mechanisms trigger — should still be 0
    const readings: OdometerReading[] = [
      { date: '2021-01-01', mileage: 50_000, source: 'Service' },
      { date: '2022-01-01', mileage: 30_000, source: 'DMV' }, // Sequential rollback
    ];
    const d3 = getD3(
      makeInput({
        declaredMileage: 20_000, // Also < 85% of lastReported
        mileageUnit: 'miles',
        history: makeHistory({
          lastReportedMileage: 60_000,
          odometerReadings: readings,
        }),
      })
    );
    expect(d3.rawScore).toBe(0);
  });
});
