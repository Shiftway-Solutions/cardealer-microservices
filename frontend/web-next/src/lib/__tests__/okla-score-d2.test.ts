import { describe, it, expect } from 'vitest';
import { calculateOklaScore, type ScoreInput } from '../okla-score-engine';
import type { VinDecodeResult, NhtsaRecall, NhtsaComplaintSummary } from '@/types/okla-score';

// =============================================================================
// D2 — Mechanical Condition Audit Tests
// =============================================================================
// Validates that D2 correctly implements the OKLA Score spec:
//   - Engine scoring (up to +60 pts): displacement, turbo, hybrid, electric
//   - Transmission (up to +30 pts): CVT > DCT > tiptronic > auto > manual
//   - Drivetrain (+25 pts): AWD/4WD bonus
//   - Safety tech (+20 pts): ADAS features (Lane Assist, AEB, Blind Spot)
//   - Recalls (−15 pts each): active/unresolved recalls
//   - NHTSA complaints (−5 pts per 10): complaint volume penalty
// =============================================================================

/** Helper to create a minimal valid ScoreInput */
function makeInput(overrides: Partial<ScoreInput> = {}): ScoreInput {
  const baseVin: VinDecodeResult = {
    vin: '1HGBH41JXMN109186',
    make: 'Honda',
    model: 'Civic',
    year: 2022,
    engineType: 'Gasoline',
    engineCylinders: 4,
    displacementL: 2.0,
    transmission: 'Automatic',
    drivetrain: 'FWD',
    ...overrides.vinDecode,
  };

  return {
    vin: '1HGBH41JXMN109186',
    vinDecode: baseVin,
    listedPriceDOP: 1_200_000,
    marketPriceDOP: 1_200_000,
    declaredMileage: 50_000,
    mileageUnit: 'km' as const,
    sellerType: 'individual' as const,
    ...overrides,
    vinDecode: baseVin,
  };
}

function getD2(input: ScoreInput) {
  const report = calculateOklaScore(input);
  return report.dimensions.find(d => d.dimension === 'D2')!;
}

// ─── Engine Scoring ──────────────────────────────────────────────

describe('D2 — Engine Scoring', () => {
  it('should give +60 pts for electric powertrain', () => {
    const d2 = getD2(
      makeInput({
        vinDecode: {
          vin: 'x',
          make: 'Tesla',
          model: 'Model 3',
          year: 2023,
          engineType: 'Electric',
        },
      })
    );
    const factor = d2.factors.find(f => f.name.includes('Electric'));
    expect(factor).toBeDefined();
    expect(factor!.impact).toBe(60);
  });

  it('should give +50 pts for hybrid powertrain', () => {
    const d2 = getD2(
      makeInput({
        vinDecode: { vin: 'x', make: 'Toyota', model: 'Prius', year: 2023, engineType: 'Hybrid' },
      })
    );
    const factor = d2.factors.find(f => f.name.includes('Hybrid'));
    expect(factor).toBeDefined();
    expect(factor!.impact).toBe(50);
  });

  it('should give +25 pts for turbo engine', () => {
    const d2 = getD2(
      makeInput({
        vinDecode: {
          vin: 'x',
          make: 'VW',
          model: 'Jetta',
          year: 2023,
          engineType: 'Turbocharged Gasoline',
          engineCylinders: 4,
          displacementL: 1.4,
        },
      })
    );
    const factor = d2.factors.find(f => f.name.includes('Turbo'));
    expect(factor).toBeDefined();
    expect(factor!.impact).toBe(25);
  });

  it('should give displacement bonus for 2.0L-3.5L engines', () => {
    const d2_small = getD2(
      makeInput({
        vinDecode: {
          vin: 'x',
          make: 'Honda',
          model: 'Fit',
          year: 2023,
          engineType: 'Gasoline',
          engineCylinders: 4,
          displacementL: 1.5,
        },
      })
    );
    const d2_mid = getD2(
      makeInput({
        vinDecode: {
          vin: 'x',
          make: 'Toyota',
          model: 'Camry',
          year: 2023,
          engineType: 'Gasoline',
          engineCylinders: 4,
          displacementL: 2.5,
        },
      })
    );
    // Mid-range displacement should score higher than small
    expect(d2_mid.rawScore).toBeGreaterThan(d2_small.rawScore);
  });

  it('should cap engine scoring at +60 pts max', () => {
    // Electric already caps at 60, verify it doesn't exceed
    const d2 = getD2(
      makeInput({
        vinDecode: {
          vin: 'x',
          make: 'Tesla',
          model: 'Model S',
          year: 2023,
          engineType: 'Electric',
          engineCylinders: 0,
          displacementL: 0,
        },
      })
    );
    const engineFactor = d2.factors.find(f => f.name.includes('Electric'));
    expect(engineFactor!.impact).toBeLessThanOrEqual(60);
  });
});

// ─── Transmission Scoring ────────────────────────────────────────

describe('D2 — Transmission Scoring', () => {
  it('should rank CVT highest (+30 pts)', () => {
    const d2 = getD2(
      makeInput({
        vinDecode: {
          vin: 'x',
          make: 'Nissan',
          model: 'Sentra',
          year: 2023,
          engineType: 'Gasoline',
          transmission: 'CVT Automatic',
        },
      })
    );
    const factor = d2.factors.find(f => f.name.includes('CVT'));
    expect(factor).toBeDefined();
    expect(factor!.impact).toBe(30);
  });

  it('should rank manual lowest (+10 pts)', () => {
    const d2 = getD2(
      makeInput({
        vinDecode: {
          vin: 'x',
          make: 'Honda',
          model: 'Civic',
          year: 2023,
          engineType: 'Gasoline',
          transmission: 'Manual 6-speed',
        },
      })
    );
    const factor = d2.factors.find(f => f.name.includes('Manual'));
    expect(factor).toBeDefined();
    expect(factor!.impact).toBe(10);
  });

  it('should differentiate CVT > DCT > Tiptronic > Auto > Manual', () => {
    const transmissions = [
      { trans: 'CVT Automatic', expected: 30 },
      { trans: 'DCT Dual Clutch', expected: 28 },
      { trans: 'Tiptronic Sport', expected: 25 },
      { trans: 'Automatic 6-speed', expected: 20 },
      { trans: 'Manual 5-speed', expected: 10 },
    ];

    for (const { trans, expected } of transmissions) {
      const d2 = getD2(
        makeInput({
          vinDecode: {
            vin: 'x',
            make: 'Test',
            model: 'Car',
            year: 2023,
            engineType: 'Gasoline',
            transmission: trans,
          },
        })
      );
      const transFactor = d2.factors.find(
        f =>
          f.name.includes('CVT') ||
          f.name.includes('DCT') ||
          f.name.includes('Tiptronic') ||
          f.name.includes('Automatic') ||
          f.name.includes('Manual')
      );
      expect(transFactor, `Transmission "${trans}" should have factor`).toBeDefined();
      expect(transFactor!.impact, `Transmission "${trans}" should be ${expected}`).toBe(expected);
    }
  });
});

// ─── Drivetrain ──────────────────────────────────────────────────

describe('D2 — Drivetrain Scoring', () => {
  it('should give +25 pts for AWD', () => {
    const d2 = getD2(
      makeInput({
        vinDecode: {
          vin: 'x',
          make: 'Subaru',
          model: 'Outback',
          year: 2023,
          engineType: 'Gasoline',
          drivetrain: 'AWD',
        },
      })
    );
    const factor = d2.factors.find(f => f.name.includes('AWD'));
    expect(factor).toBeDefined();
    expect(factor!.impact).toBe(25);
  });

  it('should give +25 pts for 4WD', () => {
    const d2 = getD2(
      makeInput({
        vinDecode: {
          vin: 'x',
          make: 'Jeep',
          model: 'Wrangler',
          year: 2023,
          engineType: 'Gasoline',
          drivetrain: '4WD',
        },
      })
    );
    const factor = d2.factors.find(f => f.name.includes('AWD'));
    expect(factor).toBeDefined();
    expect(factor!.impact).toBe(25);
  });

  it('should NOT give drivetrain bonus for FWD', () => {
    const d2 = getD2(
      makeInput({
        vinDecode: {
          vin: 'x',
          make: 'Honda',
          model: 'Civic',
          year: 2023,
          engineType: 'Gasoline',
          drivetrain: 'FWD',
        },
      })
    );
    const factor = d2.factors.find(f => f.name.includes('AWD') || f.name.includes('4WD'));
    expect(factor).toBeUndefined();
  });
});

// ─── Safety Tech (ADAS) ─────────────────────────────────────────

describe('D2 — Safety Tech (ADAS) Scoring', () => {
  it('should give points for Lane Assist', () => {
    const d2 = getD2(
      makeInput({
        safetyFeatures: ['Lane Departure Warning', 'Lane Keep Assist'],
      })
    );
    const factor = d2.factors.find(f => f.name.includes('Safety Tech'));
    expect(factor).toBeDefined();
    expect(factor!.impact).toBeGreaterThan(0);
  });

  it('should give points for AEB (Autonomous Emergency Braking)', () => {
    const d2 = getD2(
      makeInput({
        safetyFeatures: ['Autonomous Emergency Braking'],
      })
    );
    const factor = d2.factors.find(f => f.name.includes('Safety Tech'));
    expect(factor).toBeDefined();
  });

  it('should give points for Blind Spot Monitoring', () => {
    const d2 = getD2(
      makeInput({
        safetyFeatures: ['Blind Spot Warning'],
      })
    );
    const factor = d2.factors.find(f => f.name.includes('Safety Tech'));
    expect(factor).toBeDefined();
  });

  it('should cap safety tech at +20 pts even with many features', () => {
    const d2 = getD2(
      makeInput({
        safetyFeatures: [
          'Lane Departure Warning',
          'AEB Forward',
          'Blind Spot Monitor',
          'Adaptive Cruise Control',
          'Forward Collision Warning',
        ],
      })
    );
    const factor = d2.factors.find(f => f.name.includes('Safety Tech'));
    expect(factor).toBeDefined();
    expect(factor!.impact).toBeLessThanOrEqual(20);
  });

  it('should NOT give safety tech points when no features available', () => {
    const d2 = getD2(makeInput({ safetyFeatures: [] }));
    const factor = d2.factors.find(f => f.name.includes('Safety Tech'));
    expect(factor).toBeUndefined();
  });
});

// ─── Active Recalls Penalty ─────────────────────────────────────

describe('D2 — Active Recalls Penalty', () => {
  const makeRecall = (component: string, isResolved = false): NhtsaRecall => ({
    campaignNumber: `${Math.random().toString(36).slice(2, 8)}`,
    component,
    summary: `Recall for ${component}`,
    consequence: 'Safety concern',
    remedy: 'Dealer repair',
    reportReceivedDate: '2024-01-15',
    isResolved,
  });

  it('should penalize −15 pts per active recall', () => {
    const d2_no_recall = getD2(makeInput({ recalls: [] }));
    const d2_one_recall = getD2(makeInput({ recalls: [makeRecall('Airbag')] }));
    const d2_two_recalls = getD2(
      makeInput({ recalls: [makeRecall('Airbag'), makeRecall('Brakes')] })
    );

    // One recall = -15, two = -30
    expect(d2_no_recall.rawScore - d2_one_recall.rawScore).toBe(15);
    expect(d2_no_recall.rawScore - d2_two_recalls.rawScore).toBe(30);
  });

  it('should NOT penalize for resolved recalls', () => {
    const d2_resolved = getD2(makeInput({ recalls: [makeRecall('Airbag', true)] }));
    const d2_no_recall = getD2(makeInput({ recalls: [] }));
    expect(d2_resolved.rawScore).toBe(d2_no_recall.rawScore);
  });

  it('should include recall components in factor description', () => {
    const d2 = getD2(makeInput({ recalls: [makeRecall('Airbag'), makeRecall('Power Steering')] }));
    const factor = d2.factors.find(f => f.name.includes('Recall'));
    expect(factor).toBeDefined();
    expect(factor!.description).toContain('Airbag');
    expect(factor!.description).toContain('Power Steering');
  });
});

// ─── NHTSA Complaints Penalty ───────────────────────────────────

describe('D2 — NHTSA Complaints Penalty', () => {
  it('should penalize −5 pts per 10 complaints', () => {
    const complaints30: NhtsaComplaintSummary = {
      totalComplaints: 30,
      componentBreakdown: { Engine: 15, Transmission: 10, Brakes: 5 },
    };
    const d2_complaints = getD2(makeInput({ complaints: complaints30 }));
    const d2_no_complaints = getD2(makeInput());

    // 30 complaints = 3 groups of 10 = -15 pts
    expect(d2_no_complaints.rawScore - d2_complaints.rawScore).toBe(15);
  });

  it('should NOT penalize for fewer than 10 complaints', () => {
    const complaints5: NhtsaComplaintSummary = {
      totalComplaints: 5,
      componentBreakdown: { Engine: 5 },
    };
    const d2_few = getD2(makeInput({ complaints: complaints5 }));
    const d2_none = getD2(makeInput());
    expect(d2_few.rawScore).toBe(d2_none.rawScore);
  });

  it('should show top complaint components in factor', () => {
    const complaints: NhtsaComplaintSummary = {
      totalComplaints: 50,
      componentBreakdown: { Transmission: 25, Engine: 15, Electrical: 10 },
    };
    const d2 = getD2(makeInput({ complaints }));
    const factor = d2.factors.find(f => f.name.includes('Complaint'));
    expect(factor).toBeDefined();
    expect(factor!.description).toContain('Transmission');
  });
});

// ─── D2 Score Bounds ────────────────────────────────────────────

describe('D2 — Score Bounds', () => {
  it('should never exceed max 200 pts', () => {
    const d2 = getD2(
      makeInput({
        vinDecode: {
          vin: 'x',
          make: 'Tesla',
          model: 'Model S',
          year: 2023,
          engineType: 'Electric',
          transmission: 'CVT Automatic',
          drivetrain: 'AWD',
        },
        safetyFeatures: [
          'Lane Assist',
          'AEB',
          'Blind Spot',
          'Adaptive Cruise',
          'Collision Warning',
        ],
        recalls: [],
      })
    );
    expect(d2.rawScore).toBeLessThanOrEqual(200);
  });

  it('should never go below 0 pts', () => {
    // Vehicle with many recalls and complaints
    const manyRecalls: NhtsaRecall[] = Array.from({ length: 20 }, (_, i) => ({
      campaignNumber: `R${i}`,
      component: `Part${i}`,
      summary: 'Recall',
      consequence: 'Safety',
      remedy: 'Repair',
      reportReceivedDate: '2024-01-01',
      isResolved: false,
    }));
    const manyComplaints: NhtsaComplaintSummary = {
      totalComplaints: 500,
      componentBreakdown: { Engine: 200, Transmission: 200, Brakes: 100 },
    };
    const d2 = getD2(
      makeInput({
        vinDecode: {
          vin: 'x',
          make: 'Bad',
          model: 'Car',
          year: 2023,
          engineType: 'Gasoline',
          transmission: 'Manual',
          drivetrain: 'FWD',
          engineCylinders: 4,
          displacementL: 1.0,
        },
        recalls: manyRecalls,
        complaints: manyComplaints,
      })
    );
    expect(d2.rawScore).toBeGreaterThanOrEqual(0);
  });

  it('should have weight of 20% and max 200 pts', () => {
    const d2 = getD2(makeInput());
    expect(d2.weight).toBe(20);
    expect(d2.maxPoints).toBe(200);
  });
});

// ─── Alerts Integration ─────────────────────────────────────────

describe('D2 — Alerts', () => {
  it('should raise MULTIPLE_ACTIVE_RECALLS alert for 3+ recalls', () => {
    const recalls: NhtsaRecall[] = Array.from({ length: 3 }, (_, i) => ({
      campaignNumber: `RC${i}`,
      component: `Part${i}`,
      summary: 'Recall',
      consequence: 'Safety',
      remedy: 'Fix',
      reportReceivedDate: '2024-01-01',
      isResolved: false,
    }));
    const report = calculateOklaScore(makeInput({ recalls }));
    const alert = report.alerts.find(a => a.code === 'MULTIPLE_ACTIVE_RECALLS');
    expect(alert).toBeDefined();
    expect(alert!.dimension).toBe('D2');
  });

  it('should raise HIGH_NHTSA_COMPLAINTS alert for 50+ complaints', () => {
    const complaints: NhtsaComplaintSummary = {
      totalComplaints: 75,
      componentBreakdown: { Engine: 50, Brakes: 25 },
    };
    const report = calculateOklaScore(makeInput({ complaints }));
    const alert = report.alerts.find(a => a.code === 'HIGH_NHTSA_COMPLAINTS');
    expect(alert).toBeDefined();
    expect(alert!.dimension).toBe('D2');
  });

  it('should NOT raise complaint alert for < 50 complaints', () => {
    const complaints: NhtsaComplaintSummary = {
      totalComplaints: 30,
      componentBreakdown: { Engine: 30 },
    };
    const report = calculateOklaScore(makeInput({ complaints }));
    const alert = report.alerts.find(a => a.code === 'HIGH_NHTSA_COMPLAINTS');
    expect(alert).toBeUndefined();
  });
});
