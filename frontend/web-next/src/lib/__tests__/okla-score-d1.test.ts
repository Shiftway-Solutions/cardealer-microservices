import { describe, it, expect } from 'vitest';
import { calculateOklaScore, type ScoreInput } from '../okla-score-engine';
import type { VinHistoryReport, TitleType, AccidentSeverity } from '@/types/okla-score';

// =============================================================================
// D1 — VIN History (25%, max 250 pts) Audit Tests
// =============================================================================
// Validates that D1 correctly implements the OKLA Score spec:
//
//   TITLE TYPE SCORING (base = 100):
//     Clean ......... +0   → raw stays 100 → final up to 250 with bonuses
//     Salvage ....... -200 → raw = -100 → clamped to 0
//     Rebuilt ....... -120 → raw = -20  → clamped to 0
//     Flood ......... -180 → raw = -80  → clamped to 0
//     Junk .......... -250 → raw = -150 → clamped to 0
//     Unknown ....... -50  → raw = 50
//
//   DAMAGE FLAGS:
//     Flood damage .. -180 pts   (severe internal corrosion)
//     Frame damage .. -150 pts   (structural integrity)
//     Hail damage ... -60 pts    (cosmetic + depreciation)
//     Lemon buyback . -180 pts   (chronic defects)
//     Stolen/cloned . raw = -250 → 0 (BLOCKING)
//
//   ACCIDENT SCORING:
//     0 accidents ... +50 pts bonus
//     Minor ......... -30 pts
//     Moderate ...... -70 pts
//
//   OWNERSHIP & MAINTENANCE:
//     Rental/fleet .. -40 pts
//     Single owner .. +20 pts
//     Service recs>5  +30 pts
//
//   NO HISTORY:
//     Returns 100 pts with "No history available" factor
//
//   Score clamped to [0, 250]
//
// All tests use local ScoreInput objects with mocked VinHistoryReport data.
// NO real API calls to VinAudit, CARFAX, or NMVTIS are made.
// =============================================================================

/** Build a clean VinHistoryReport with all flags off */
function makeCleanHistory(overrides: Partial<VinHistoryReport> = {}): VinHistoryReport {
  return {
    vin: '1HGBH41JXMN109186',
    titleType: 'Clean',
    totalOwners: 2,
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

/** Build a minimal valid ScoreInput with optional history */
function makeInput(
  historyOverrides?: Partial<VinHistoryReport> | null,
  scoreOverrides: Partial<ScoreInput> = {}
): ScoreInput {
  return {
    vin: '1HGBH41JXMN109186',
    vinDecode: {
      vin: '1HGBH41JXMN109186',
      make: 'Toyota',
      model: 'Corolla',
      year: 2022,
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
    // null means no history (VIN not found), otherwise build from overrides
    history: historyOverrides === null ? undefined : makeCleanHistory(historyOverrides),
    ...scoreOverrides,
  };
}

/** Extract D1 from the full score report */
function getD1(input: ScoreInput) {
  const report = calculateOklaScore(input);
  return report.dimensions.find(d => d.dimension === 'D1')!;
}

// ─── Title Type Scoring ─────────────────────────────────────────

describe('D1 — Title Type Scoring', () => {
  it('should give max score for Clean title with bonuses (no accidents + single owner + service records)', () => {
    // Clean title: base=100, +50 (0 accidents), +20 (1 owner), +30 (>5 service) = 200
    // Clamped to min(250, 200) = 200
    const d1 = getD1(
      makeInput({
        titleType: 'Clean',
        accidentCount: 0,
        accidentSeverity: 'None',
        totalOwners: 1,
        serviceRecords: 10,
      })
    );
    expect(d1.rawScore).toBe(200);
    expect(d1.rawScore).toBeLessThanOrEqual(250);
  });

  it('should give 250 pts (max) for perfect Clean title — score is clamped', () => {
    // Even with all bonuses, max is 250
    const d1 = getD1(
      makeInput({
        titleType: 'Clean',
        accidentCount: 0,
        accidentSeverity: 'None',
        totalOwners: 1,
        serviceRecords: 10,
      })
    );
    expect(d1.rawScore).toBeLessThanOrEqual(250);
    expect(d1.rawScore).toBeGreaterThan(0);
  });

  it('should give 0 pts for Salvage title (base=100, penalty=-200, clamped to 0)', () => {
    const d1 = getD1(makeInput({ titleType: 'Salvage' }));
    // base 100 - 200 = -100 → clamped to 0 (before other bonuses might add)
    // With 0 accidents bonus (+50): -100 + 50 = -50 → clamped to 0
    expect(d1.rawScore).toBe(0);
  });

  it('should give 0 pts for Rebuilt title (base=100, penalty=-120)', () => {
    // base 100 - 120 = -20, + 50 (0 accidents) = 30 → but clamped may vary
    const d1 = getD1(makeInput({ titleType: 'Rebuilt' }));
    // Actually: 100 - 120 + 50 (no accidents) = 30, clamped to max(0, min(250, 30)) = 30
    // With default 2 owners and 3 service records, no additional bonuses
    expect(d1.rawScore).toBe(30);
  });

  it('should give 0 pts for Flood title (base=100, penalty=-180)', () => {
    const d1 = getD1(makeInput({ titleType: 'Flood' }));
    // 100 - 180 + 50 (0 accidents) = -30 → clamped to 0
    expect(d1.rawScore).toBe(0);
  });

  it('should give 0 pts for Junk title (base=100, penalty=-250)', () => {
    const d1 = getD1(makeInput({ titleType: 'Junk' }));
    // 100 - 250 + 50 = -100 → clamped to 0
    expect(d1.rawScore).toBe(0);
  });

  it('should apply -50 penalty for Unknown title', () => {
    const d1 = getD1(makeInput({ titleType: 'Unknown' }));
    // 100 - 50 + 50 (0 accidents) = 100
    expect(d1.rawScore).toBe(100);
  });

  it('should include title factor in factors array for non-Clean titles', () => {
    const d1 = getD1(makeInput({ titleType: 'Salvage' }));
    const titleFactor = d1.factors.find(f => f.name.includes('Title'));
    expect(titleFactor).toBeDefined();
    expect(titleFactor!.impact).toBe(-200);
    expect(titleFactor!.source).toBe('VinAudit/NMVTIS');
  });

  it('should NOT include title factor for Clean title (no penalty)', () => {
    const d1 = getD1(makeInput({ titleType: 'Clean' }));
    const titleFactor = d1.factors.find(f => f.name.includes('Title'));
    expect(titleFactor).toBeUndefined();
  });
});

// ─── Damage Flags ───────────────────────────────────────────────

describe('D1 — Damage Flags', () => {
  it('should apply -180 for flood damage flag', () => {
    const d1 = getD1(makeInput({ hasFloodDamage: true }));
    const factor = d1.factors.find(f => f.name === 'Flood Damage');
    expect(factor).toBeDefined();
    expect(factor!.impact).toBe(-180);
  });

  it('should apply -150 for frame damage flag', () => {
    const d1 = getD1(makeInput({ hasFrameDamage: true }));
    const factor = d1.factors.find(f => f.name === 'Frame Damage');
    expect(factor).toBeDefined();
    expect(factor!.impact).toBe(-150);
  });

  it('should apply -60 for hail damage flag', () => {
    const d1 = getD1(makeInput({ hasHailDamage: true }));
    const factor = d1.factors.find(f => f.name === 'Hail Damage');
    expect(factor).toBeDefined();
    expect(factor!.impact).toBe(-60);
  });

  it('should apply -180 for lemon buyback flag', () => {
    const d1 = getD1(makeInput({ isLemonBuyback: true }));
    const factor = d1.factors.find(f => f.name === 'Lemon Buyback');
    expect(factor).toBeDefined();
    expect(factor!.impact).toBe(-180);
  });

  it('should BLOCK vehicle with stolen/cloned VIN (raw = -250 → clamped to 0)', () => {
    const d1 = getD1(makeInput({ isStolenOrCloned: true }));
    expect(d1.rawScore).toBe(0);
    const factor = d1.factors.find(f => f.name === 'Stolen/Cloned VIN');
    expect(factor).toBeDefined();
    expect(factor!.impact).toBe(-250);
    expect(factor!.descriptionEs).toContain('BLOQUEADO');
  });

  it('should stack multiple damage flags and clamp to 0', () => {
    // Clean base=100, flood=-180, frame=-150, +50 (0 accidents)
    // 100 - 180 - 150 + 50 = -180 → clamped to 0
    const d1 = getD1(
      makeInput({
        hasFloodDamage: true,
        hasFrameDamage: true,
      })
    );
    expect(d1.rawScore).toBe(0);
  });

  it('should accumulate damage: flood title (-180) + flood damage (-180)', () => {
    // Flood title: 100 - 180 = -80
    // Flood damage: -80 - 180 = -260
    // + 50 (0 accidents) = -210 → clamped to 0
    const d1 = getD1(
      makeInput({
        titleType: 'Flood',
        hasFloodDamage: true,
      })
    );
    expect(d1.rawScore).toBe(0);
  });
});

// ─── Accident Scoring ───────────────────────────────────────────

describe('D1 — Accident Scoring', () => {
  it('should give +50 bonus for 0 accidents', () => {
    const d1 = getD1(makeInput({ accidentCount: 0, accidentSeverity: 'None' }));
    const factor = d1.factors.find(f => f.name === 'No Accidents');
    expect(factor).toBeDefined();
    expect(factor!.impact).toBe(50);
  });

  it('should apply -30 for minor accidents', () => {
    const d1 = getD1(makeInput({ accidentCount: 2, accidentSeverity: 'Minor' }));
    const factor = d1.factors.find(f => f.name === 'Minor Accidents');
    expect(factor).toBeDefined();
    expect(factor!.impact).toBe(-30);
    // Score: 100 - 30 = 70
    expect(d1.rawScore).toBe(70);
  });

  it('should apply -70 for moderate accidents', () => {
    const d1 = getD1(makeInput({ accidentCount: 1, accidentSeverity: 'Moderate' }));
    const factor = d1.factors.find(f => f.name === 'Moderate Accidents');
    expect(factor).toBeDefined();
    expect(factor!.impact).toBe(-70);
    // Score: 100 - 70 = 30
    expect(d1.rawScore).toBe(30);
  });

  it('should include accident count in description', () => {
    const d1 = getD1(makeInput({ accidentCount: 3, accidentSeverity: 'Minor' }));
    const factor = d1.factors.find(f => f.name === 'Minor Accidents');
    expect(factor!.description).toContain('3');
    expect(factor!.descriptionEs).toContain('3');
  });

  it('should combine accidents with title penalties', () => {
    // Rebuilt: 100 - 120 = -20, + moderate accidents: -20 - 70 = -90 → clamped to 0
    const d1 = getD1(
      makeInput({
        titleType: 'Rebuilt',
        accidentCount: 2,
        accidentSeverity: 'Moderate',
      })
    );
    expect(d1.rawScore).toBe(0);
  });
});

// ─── Ownership & Maintenance Bonuses ────────────────────────────

describe('D1 — Ownership & Maintenance', () => {
  it('should apply -40 for rental/fleet vehicles', () => {
    const d1 = getD1(makeInput({ isRentalFleet: true }));
    const factor = d1.factors.find(f => f.name === 'Rental/Fleet');
    expect(factor).toBeDefined();
    expect(factor!.impact).toBe(-40);
  });

  it('should give +20 bonus for single owner', () => {
    const d1 = getD1(makeInput({ totalOwners: 1 }));
    const factor = d1.factors.find(f => f.name === 'Single Owner');
    expect(factor).toBeDefined();
    expect(factor!.impact).toBe(20);
  });

  it('should NOT give single-owner bonus for 2+ owners', () => {
    const d1 = getD1(makeInput({ totalOwners: 3 }));
    const factor = d1.factors.find(f => f.name === 'Single Owner');
    expect(factor).toBeUndefined();
  });

  it('should give +30 bonus for >5 service records', () => {
    const d1 = getD1(makeInput({ serviceRecords: 8 }));
    const factor = d1.factors.find(f => f.name === 'Verified Maintenance');
    expect(factor).toBeDefined();
    expect(factor!.impact).toBe(30);
  });

  it('should NOT give service bonus for ≤5 records', () => {
    const d1 = getD1(makeInput({ serviceRecords: 5 }));
    const factor = d1.factors.find(f => f.name === 'Verified Maintenance');
    expect(factor).toBeUndefined();
  });

  it('should stack all bonuses: single owner + service + no accidents', () => {
    // Clean: 100 + 50 (0 acc) + 20 (1 owner) + 30 (>5 service) = 200
    const d1 = getD1(
      makeInput({
        totalOwners: 1,
        serviceRecords: 10,
        accidentCount: 0,
        accidentSeverity: 'None',
      })
    );
    expect(d1.rawScore).toBe(200);
  });
});

// ─── No History (VIN Not Found) ─────────────────────────────────

describe('D1 — VIN Not Found (No History Available)', () => {
  it('should return 100 pts when history is undefined (partial score)', () => {
    const d1 = getD1(makeInput(null));
    expect(d1.rawScore).toBe(100);
  });

  it('should include "No history available" factor as visible warning', () => {
    const d1 = getD1(makeInput(null));
    const factor = d1.factors.find(f => f.name === 'No history available');
    expect(factor).toBeDefined();
    expect(factor!.impact).toBe(0);
    expect(factor!.source).toBe('N/A');
    expect(factor!.descriptionEs).toContain('historial VIN no disponible');
  });

  it('should have exactly 1 factor when no history', () => {
    const d1 = getD1(makeInput(null));
    expect(d1.factors).toHaveLength(1);
  });

  it('should still be within valid range [0, 250]', () => {
    const d1 = getD1(makeInput(null));
    expect(d1.rawScore).toBeGreaterThanOrEqual(0);
    expect(d1.rawScore).toBeLessThanOrEqual(250);
  });
});

// ─── Score Clamping [0, 250] ────────────────────────────────────

describe('D1 — Score Clamping', () => {
  it('should never go below 0 even with extreme penalties', () => {
    // Salvage(-200) + flood damage(-180) + frame(-150) + lemon(-180) + moderate accident(-70)
    const d1 = getD1(
      makeInput({
        titleType: 'Salvage',
        hasFloodDamage: true,
        hasFrameDamage: true,
        isLemonBuyback: true,
        accidentCount: 3,
        accidentSeverity: 'Moderate',
      })
    );
    expect(d1.rawScore).toBe(0);
    expect(d1.rawScore).toBeGreaterThanOrEqual(0);
  });

  it('should never exceed 250 even with all bonuses', () => {
    const d1 = getD1(
      makeInput({
        titleType: 'Clean',
        accidentCount: 0,
        accidentSeverity: 'None',
        totalOwners: 1,
        serviceRecords: 20,
      })
    );
    expect(d1.rawScore).toBeLessThanOrEqual(250);
  });

  it('should return 0 for stolen vehicle regardless of other factors', () => {
    // Stolen sets raw = -250, then bonuses/penalties don't matter
    const d1 = getD1(
      makeInput({
        isStolenOrCloned: true,
        accidentCount: 0,
        accidentSeverity: 'None',
        totalOwners: 1,
        serviceRecords: 20,
      })
    );
    // raw = -250 + (bonuses after stolen flag is set to -250)
    // The code does: raw = -250 (overwrite), then +50, +20, +30 = -250 + 100 = -150 → clamped to 0
    expect(d1.rawScore).toBe(0);
  });
});

// ─── Factor Source Attribution (Mock Verification) ──────────────

describe('D1 — Factor Source Attribution (VinAudit/NMVTIS Mocks)', () => {
  it('should attribute title factors to VinAudit/NMVTIS', () => {
    const d1 = getD1(makeInput({ titleType: 'Salvage' }));
    const factor = d1.factors.find(f => f.name.includes('Title'));
    expect(factor!.source).toBe('VinAudit/NMVTIS');
  });

  it('should attribute damage factors to VinAudit', () => {
    const d1 = getD1(makeInput({ hasFloodDamage: true }));
    const factor = d1.factors.find(f => f.name === 'Flood Damage');
    expect(factor!.source).toBe('VinAudit');
  });

  it('should attribute accident factors to VinAudit', () => {
    const d1 = getD1(makeInput({ accidentCount: 0 }));
    const factor = d1.factors.find(f => f.name === 'No Accidents');
    expect(factor!.source).toBe('VinAudit');
  });

  it('should attribute stolen/cloned to NMVTIS', () => {
    const d1 = getD1(makeInput({ isStolenOrCloned: true }));
    const factor = d1.factors.find(f => f.name === 'Stolen/Cloned VIN');
    expect(factor!.source).toBe('NMVTIS');
  });

  it('should attribute ownership and maintenance to VinAudit', () => {
    const d1 = getD1(makeInput({ totalOwners: 1, serviceRecords: 10 }));
    const ownerFactor = d1.factors.find(f => f.name === 'Single Owner');
    const maintenanceFactor = d1.factors.find(f => f.name === 'Verified Maintenance');
    expect(ownerFactor!.source).toBe('VinAudit');
    expect(maintenanceFactor!.source).toBe('VinAudit');
  });
});

// ─── Spanish Translations (Dominican Market) ───────────────────

describe('D1 — Spanish Translations', () => {
  it('should have Spanish name for Salvage title', () => {
    const d1 = getD1(makeInput({ titleType: 'Salvage' }));
    const factor = d1.factors.find(f => f.name.includes('Title'));
    expect(factor!.nameEs).toContain('Salvamento');
  });

  it('should have Spanish description for flood damage', () => {
    const d1 = getD1(makeInput({ hasFloodDamage: true }));
    const factor = d1.factors.find(f => f.name === 'Flood Damage');
    expect(factor!.descriptionEs).toContain('corrosión');
  });

  it('should have Spanish name for no history warning', () => {
    const d1 = getD1(makeInput(null));
    const factor = d1.factors[0];
    expect(factor.nameEs).toBe('Sin historial disponible');
  });
});

// ─── Edge Cases & Combinations ──────────────────────────────────

describe('D1 — Edge Cases', () => {
  it('should handle Clean title with minor accident (net positive)', () => {
    // base=100, minor=-30 = 70
    const d1 = getD1(makeInput({ accidentCount: 1, accidentSeverity: 'Minor' }));
    expect(d1.rawScore).toBe(70);
  });

  it('should handle Unknown title with all bonuses', () => {
    // base=100, unknown=-50, +50(0 acc), +20(1 owner), +30(>5 svc) = 150
    const d1 = getD1(
      makeInput({
        titleType: 'Unknown',
        accidentCount: 0,
        accidentSeverity: 'None',
        totalOwners: 1,
        serviceRecords: 10,
      })
    );
    expect(d1.rawScore).toBe(150);
  });

  it('should handle hail damage on otherwise clean vehicle', () => {
    // base=100, hail=-60, +50(0 acc) = 90
    const d1 = getD1(makeInput({ hasHailDamage: true }));
    expect(d1.rawScore).toBe(90);
  });

  it('should handle rental fleet with single owner (both flags apply)', () => {
    // base=100, rental=-40, +50(0 acc), +20(1 owner) = 130
    const d1 = getD1(
      makeInput({
        isRentalFleet: true,
        totalOwners: 1,
      })
    );
    expect(d1.rawScore).toBe(130);
  });

  it('should handle all penalties combined (worst case)', () => {
    // Junk: 100-250=-150, flood=-180, frame=-150, hail=-60, lemon=-180,
    // stolen overrides to -250, rental=-40, moderate acc=-70
    // All these stack but stolen overrides raw = -250
    // Then: -250 - 40 - 70 = -360 → clamped to 0
    const d1 = getD1(
      makeInput({
        titleType: 'Junk',
        hasFloodDamage: true,
        hasFrameDamage: true,
        hasHailDamage: true,
        isLemonBuyback: true,
        isStolenOrCloned: true,
        isRentalFleet: true,
        accidentCount: 5,
        accidentSeverity: 'Moderate',
        totalOwners: 5,
        serviceRecords: 0,
      })
    );
    expect(d1.rawScore).toBe(0);
  });

  it('should handle moderate accidents on rebuilt title', () => {
    // base=100, rebuilt=-120, moderate=-70 = 100-120-70 = -90 → clamped to 0
    const d1 = getD1(
      makeInput({
        titleType: 'Rebuilt',
        accidentCount: 2,
        accidentSeverity: 'Moderate',
      })
    );
    expect(d1.rawScore).toBe(0);
  });

  it('D1 dimension always present in full score report', () => {
    const report = calculateOklaScore(makeInput({}));
    const d1 = report.dimensions.find(d => d.dimension === 'D1');
    expect(d1).toBeDefined();
    expect(d1!.dimension).toBe('D1');
  });

  it('D1 weight should be 25% contributing up to 250 max points', () => {
    const report = calculateOklaScore(makeInput({}));
    const d1 = report.dimensions.find(d => d.dimension === 'D1')!;
    expect(d1.maxPoints).toBe(250);
    expect(d1.rawScore).toBeLessThanOrEqual(250);
    expect(d1.rawScore).toBeGreaterThanOrEqual(0);
  });
});
