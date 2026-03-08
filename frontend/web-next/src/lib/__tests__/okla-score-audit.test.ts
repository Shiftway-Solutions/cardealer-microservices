import { describe, it, expect } from 'vitest';
import { calculateOklaScore, type ScoreInput } from '../okla-score-engine';
import { DIMENSION_CONFIG } from '@/types/okla-score';

// =============================================================================
// OKLA Score — Full Weighted Sum Audit Tests
// =============================================================================
// Validates:
//   1. Weights sum to exactly 100%
//   2. Max possible score = 1,000
//   3. Min possible score = 0
//   4. Each dimension respects its max points
//   5. Exchange rate is correctly applied
//   6. Score range [0, 1000] enforced
//   7. All 7 dimensions are always present
// =============================================================================

function makeFullInput(overrides: Partial<ScoreInput> = {}): ScoreInput {
  return {
    vin: '1HGBH41JXMN109186',
    vinDecode: {
      vin: '1HGBH41JXMN109186',
      make: 'Toyota',
      model: 'Corolla',
      year: 2023,
      engineType: 'Electric',
      engineCylinders: 0,
      displacementL: 0,
      transmission: 'CVT Automatic',
      drivetrain: 'AWD',
    },
    listedPriceDOP: 1_200_000,
    marketPriceDOP: 1_200_000,
    declaredMileage: 15_000,
    mileageUnit: 'km' as const,
    sellerType: 'dealer' as const,
    sellerScore: 0.95,
    sellerDisputes: 0,
    safetyFeatures: ['Lane Departure Warning', 'AEB', 'Blind Spot Monitor'],
    recalls: [],
    complaints: { totalComplaints: 0, componentBreakdown: {} },
    exchangeRate: 60.5,
    ...overrides,
  };
}

// ─── Dimension Weights ──────────────────────────────────────────

describe('Dimension Configuration', () => {
  it('should have exactly 7 dimensions', () => {
    const dims = Object.keys(DIMENSION_CONFIG);
    expect(dims).toHaveLength(7);
    expect(dims).toEqual(['D1', 'D2', 'D3', 'D4', 'D5', 'D6', 'D7']);
  });

  it('should have weights summing to exactly 100%', () => {
    const totalWeight = Object.values(DIMENSION_CONFIG).reduce((sum, d) => sum + d.weight, 0);
    expect(totalWeight).toBe(100);
  });

  it('should have maxPoints summing to exactly 1000', () => {
    const totalMax = Object.values(DIMENSION_CONFIG).reduce((sum, d) => sum + d.maxPoints, 0);
    expect(totalMax).toBe(1000);
  });

  it('should have correct weight-maxPoints relationship', () => {
    for (const [dim, config] of Object.entries(DIMENSION_CONFIG)) {
      // maxPoints should equal weight × 10
      expect(config.maxPoints, `${dim} maxPoints`).toBe(config.weight * 10);
    }
  });
});

// ─── Score Range ────────────────────────────────────────────────

describe('Score Range [0, 1000]', () => {
  it('should produce score within 0-1000 for optimal vehicle', () => {
    const report = calculateOklaScore(makeFullInput());
    expect(report.score).toBeGreaterThanOrEqual(0);
    expect(report.score).toBeLessThanOrEqual(1000);
  });

  it('should produce score within 0-1000 for worst-case vehicle', () => {
    const report = calculateOklaScore(
      makeFullInput({
        vinDecode: {
          vin: 'BAD',
          make: 'Unknown',
          model: 'Unknown',
          year: 2005,
          engineType: 'Gasoline',
          transmission: 'Manual',
          drivetrain: 'FWD',
          engineCylinders: 4,
          displacementL: 1.0,
        },
        history: {
          vin: 'BAD',
          titleType: 'Salvage',
          totalOwners: 8,
          accidentCount: 5,
          accidentSeverity: 'Severe',
          hasFloodDamage: true,
          hasFrameDamage: true,
          hasHailDamage: true,
          isLemonBuyback: true,
          isRentalFleet: true,
          isStolenOrCloned: false,
          odometerRollback: true,
          odometerReadings: [],
          serviceRecords: 0,
        },
        listedPriceDOP: 5_000_000, // way overpriced
        marketPriceDOP: 500_000,
        declaredMileage: 300_000,
        recalls: Array.from({ length: 10 }, (_, i) => ({
          campaignNumber: `R${i}`,
          component: `Part${i}`,
          summary: 'Recall',
          consequence: 'Safety',
          remedy: 'Fix',
          reportReceivedDate: '2024-01-01',
          isResolved: false,
        })),
        complaints: { totalComplaints: 200, componentBreakdown: { Engine: 100, Brakes: 100 } },
        sellerType: 'individual',
        sellerScore: 0.1,
        sellerDisputes: 10,
      })
    );
    expect(report.score).toBeGreaterThanOrEqual(0);
    expect(report.score).toBeLessThanOrEqual(1000);
  });
});

// ─── All 7 Dimensions Present ───────────────────────────────────

describe('All 7 Dimensions Present', () => {
  it('should always return exactly 7 dimensions in the report', () => {
    const report = calculateOklaScore(makeFullInput());
    expect(report.dimensions).toHaveLength(7);
    const dimNames = report.dimensions.map(d => d.dimension);
    expect(dimNames).toEqual(['D1', 'D2', 'D3', 'D4', 'D5', 'D6', 'D7']);
  });

  it('each dimension should have correct label, weight, and maxPoints', () => {
    const report = calculateOklaScore(makeFullInput());
    for (const dim of report.dimensions) {
      const config = DIMENSION_CONFIG[dim.dimension];
      expect(dim.weight, `${dim.dimension} weight`).toBe(config.weight);
      expect(dim.maxPoints, `${dim.dimension} maxPoints`).toBe(config.maxPoints);
      expect(dim.label, `${dim.dimension} label`).toBe(config.label);
    }
  });

  it('each dimension rawScore should be between 0 and maxPoints', () => {
    const report = calculateOklaScore(makeFullInput());
    for (const dim of report.dimensions) {
      expect(dim.rawScore, `${dim.dimension} rawScore >= 0`).toBeGreaterThanOrEqual(0);
      expect(dim.rawScore, `${dim.dimension} rawScore <= ${dim.maxPoints}`).toBeLessThanOrEqual(
        dim.maxPoints
      );
    }
  });

  it('weightedScore should be correctly computed from rawScore', () => {
    const report = calculateOklaScore(makeFullInput());
    for (const dim of report.dimensions) {
      const expected = Math.round((dim.rawScore / dim.maxPoints) * dim.weight * 10);
      expect(dim.weightedScore, `${dim.dimension} weightedScore`).toBe(expected);
    }
  });

  it('total score should equal sum of weightedScores', () => {
    const report = calculateOklaScore(makeFullInput());
    const sumWeighted = report.dimensions.reduce((sum, d) => sum + d.weightedScore, 0);
    // Allow for rounding
    expect(Math.abs(report.score - sumWeighted)).toBeLessThanOrEqual(1);
  });
});

// ─── Exchange Rate ──────────────────────────────────────────────

describe('Exchange Rate Handling', () => {
  it('should use provided exchange rate for price analysis', () => {
    const report = calculateOklaScore(
      makeFullInput({
        exchangeRate: 61.0,
        listedPriceDOP: 1_830_000, // ~$30,000 USD
        marketPriceDOP: 1_830_000,
      })
    );
    expect(report.priceAnalysis.exchangeRate).toBe(61.0);
  });

  it('should fallback to default rate when no exchangeRate provided', () => {
    const input = makeFullInput();
    delete (input as Partial<ScoreInput>).exchangeRate;
    const report = calculateOklaScore(input);
    // Falls back to DOP_USD_EXCHANGE_RATE constant (60.5)
    expect(report.priceAnalysis.exchangeRate).toBeGreaterThan(40);
    expect(report.priceAnalysis.exchangeRate).toBeLessThan(100);
  });

  it('should calculate fairPriceUSD from DOP using exchange rate', () => {
    const report = calculateOklaScore(
      makeFullInput({
        exchangeRate: 60.5,
        marketPriceDOP: 1_210_000,
      })
    );
    // fairPriceUSD should be marketPriceDOP / rate
    expect(report.priceAnalysis.fairPriceUSD).toBeGreaterThan(0);
    const expectedUSD = Math.round(1_210_000 / 60.5);
    expect(Math.abs(report.priceAnalysis.fairPriceUSD - expectedUSD)).toBeLessThan(5);
  });
});

// ─── Score Level Classification ─────────────────────────────────

describe('Score Level Classification', () => {
  it('should classify high score as excellent', () => {
    const report = calculateOklaScore(makeFullInput());
    // Electric + CVT + AWD + safety features + new car + fair price → should be high
    expect(report.score).toBeGreaterThan(700);
    expect(['excellent', 'good']).toContain(report.level);
  });

  it('should classify low score as critical or deficient', () => {
    const report = calculateOklaScore(
      makeFullInput({
        vinDecode: {
          vin: 'BAD',
          make: 'Unknown',
          model: 'Unknown',
          year: 2005,
          engineType: 'Gasoline',
          transmission: 'Manual',
          drivetrain: 'FWD',
        },
        history: {
          vin: 'BAD',
          titleType: 'Salvage',
          totalOwners: 6,
          accidentCount: 4,
          accidentSeverity: 'Severe',
          hasFloodDamage: true,
          hasFrameDamage: true,
          hasHailDamage: false,
          isLemonBuyback: false,
          isRentalFleet: true,
          isStolenOrCloned: false,
          odometerRollback: true,
          odometerReadings: [],
          serviceRecords: 0,
        },
        listedPriceDOP: 3_000_000,
        marketPriceDOP: 800_000,
        declaredMileage: 250_000,
        recalls: Array.from({ length: 5 }, (_, i) => ({
          campaignNumber: `R${i}`,
          component: `Part${i}`,
          summary: 'Recall',
          consequence: 'Safety',
          remedy: 'Fix',
          reportReceivedDate: '2024-01-01',
          isResolved: false,
        })),
        sellerType: 'individual',
        sellerScore: 0.1,
        sellerDisputes: 5,
      })
    );
    expect(report.score).toBeLessThan(400);
    expect(['critical', 'deficient']).toContain(report.level);
  });
});

// ─── Price Verdicts ─────────────────────────────────────────────

describe('Price Verdict Ranges', () => {
  it('should return fair_price when listed ≈ market', () => {
    const report = calculateOklaScore(
      makeFullInput({
        listedPriceDOP: 1_200_000,
        marketPriceDOP: 1_200_000,
      })
    );
    expect(report.priceAnalysis.priceVerdict).toBe('fair_price');
  });

  it('should return excellent_deal for −20% below market', () => {
    const report = calculateOklaScore(
      makeFullInput({
        listedPriceDOP: 960_000, // 20% below 1.2M
        marketPriceDOP: 1_200_000,
      })
    );
    expect(report.priceAnalysis.priceVerdict).toBe('excellent_deal');
  });

  it('should return abusive_price for +35% above market', () => {
    const report = calculateOklaScore(
      makeFullInput({
        listedPriceDOP: 1_620_000, // 35% above 1.2M
        marketPriceDOP: 1_200_000,
      })
    );
    expect(report.priceAnalysis.priceVerdict).toBe('abusive_price');
  });

  it('should return suspicious_deal for −50% below market', () => {
    const report = calculateOklaScore(
      makeFullInput({
        listedPriceDOP: 600_000, // 50% below 1.2M
        marketPriceDOP: 1_200_000,
      })
    );
    expect(report.priceAnalysis.priceVerdict).toBe('suspicious_deal');
  });
});
