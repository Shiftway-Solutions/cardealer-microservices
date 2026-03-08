import { describe, it, expect } from 'vitest';
import {
  calculateDealRating,
  isOvervalued,
  type DealRating,
} from '@/components/ui/deal-rating-badge';

// =============================================================================
// PricingAgent Audit — Frontend deal-rating-badge tests
// Validates 5-tier system + >20% overvaluation helper
// =============================================================================

describe('calculateDealRating — 5-tier classification', () => {
  // ── Great Deal (≤ -15%) ───────────────────────────────────────────────
  it('returns "great" when price is 20% below market', () => {
    expect(calculateDealRating(8000, 10000)).toBe('great');
  });

  it('returns "great" when price is exactly 15% below market', () => {
    expect(calculateDealRating(8500, 10000)).toBe('great');
  });

  it('returns "great" when price is 50% below market', () => {
    expect(calculateDealRating(5000, 10000)).toBe('great');
  });

  // ── Good Deal (-15% to -5%) ───────────────────────────────────────────
  it('returns "good" when price is 10% below market', () => {
    expect(calculateDealRating(9000, 10000)).toBe('good');
  });

  it('returns "good" when price is 14% below market', () => {
    expect(calculateDealRating(8600, 10000)).toBe('good');
  });

  it('returns "good" when price is exactly 5% below market', () => {
    expect(calculateDealRating(9500, 10000)).toBe('good');
  });

  // ── Fair (±5%) ────────────────────────────────────────────────────────
  it('returns "fair" when price equals market', () => {
    expect(calculateDealRating(10000, 10000)).toBe('fair');
  });

  it('returns "fair" when price is 3% above market', () => {
    expect(calculateDealRating(10300, 10000)).toBe('fair');
  });

  it('returns "fair" when price is 4% below market', () => {
    expect(calculateDealRating(9600, 10000)).toBe('fair');
  });

  it('returns "fair" when price is exactly 5% above market', () => {
    expect(calculateDealRating(10500, 10000)).toBe('fair');
  });

  // ── High Price (+5% to +15%) ──────────────────────────────────────────
  it('returns "high" when price is 10% above market', () => {
    expect(calculateDealRating(11000, 10000)).toBe('high');
  });

  it('returns "high" when price is 6% above market', () => {
    expect(calculateDealRating(10600, 10000)).toBe('high');
  });

  it('returns "high" when price is exactly 15% above market', () => {
    expect(calculateDealRating(11500, 10000)).toBe('high');
  });

  // ── Overpriced (>15%) ─────────────────────────────────────────────────
  it('returns "overpriced" when price is 16% above market', () => {
    expect(calculateDealRating(11600, 10000)).toBe('overpriced');
  });

  it('returns "overpriced" when price is 20% above market', () => {
    expect(calculateDealRating(12000, 10000)).toBe('overpriced');
  });

  it('returns "overpriced" when price is 50% above market', () => {
    expect(calculateDealRating(15000, 10000)).toBe('overpriced');
  });

  it('returns "overpriced" when price is double the market', () => {
    expect(calculateDealRating(20000, 10000)).toBe('overpriced');
  });

  // ── Uncertain (no data) ───────────────────────────────────────────────
  it('returns "uncertain" when marketPrice is null', () => {
    expect(calculateDealRating(10000, null)).toBe('uncertain');
  });

  it('returns "uncertain" when marketPrice is 0', () => {
    expect(calculateDealRating(10000, 0)).toBe('uncertain');
  });

  it('returns "uncertain" when marketPrice is negative', () => {
    expect(calculateDealRating(10000, -5000)).toBe('uncertain');
  });
});

describe('calculateDealRating — threshold boundaries', () => {
  // Exact boundary tests with precise decimal math
  it('boundary: -15.01% → great', () => {
    // price = 10000 * (1 - 0.1501) = 8499
    expect(calculateDealRating(8499, 10000)).toBe('great');
  });

  it('boundary: -14.99% → good', () => {
    // price = 10000 * (1 - 0.1499) = 8501
    expect(calculateDealRating(8501, 10000)).toBe('good');
  });

  it('boundary: -5.01% → good', () => {
    expect(calculateDealRating(9499, 10000)).toBe('good');
  });

  it('boundary: -4.99% → fair', () => {
    expect(calculateDealRating(9501, 10000)).toBe('fair');
  });

  it('boundary: +5.01% → high', () => {
    expect(calculateDealRating(10501, 10000)).toBe('high');
  });

  it('boundary: +15.01% → overpriced', () => {
    expect(calculateDealRating(11501, 10000)).toBe('overpriced');
  });
});

describe('isOvervalued — >20% overvaluation detection', () => {
  it('returns true when price is 25% above market', () => {
    expect(isOvervalued(12500, 10000)).toBe(true);
  });

  it('returns true when price is exactly 20.01% above market', () => {
    expect(isOvervalued(12001, 10000)).toBe(true);
  });

  it('returns false when price is exactly 20% above market', () => {
    // 20% is the boundary — exactly 20% is NOT overvalued (>20 not >=20)
    expect(isOvervalued(12000, 10000)).toBe(false);
  });

  it('returns false when price is 15% above market', () => {
    expect(isOvervalued(11500, 10000)).toBe(false);
  });

  it('returns false when price is at market', () => {
    expect(isOvervalued(10000, 10000)).toBe(false);
  });

  it('returns false when price is below market', () => {
    expect(isOvervalued(8000, 10000)).toBe(false);
  });

  it('returns false when marketPrice is null', () => {
    expect(isOvervalued(10000, null)).toBe(false);
  });

  it('returns false when marketPrice is 0', () => {
    expect(isOvervalued(10000, 0)).toBe(false);
  });

  it('returns true when price is double the market (100% above)', () => {
    expect(isOvervalued(20000, 10000)).toBe(true);
  });
});

describe('DealRating type — includes overpriced', () => {
  it('all 6 rating values are valid DealRating types', () => {
    const allRatings: DealRating[] = ['great', 'good', 'fair', 'high', 'overpriced', 'uncertain'];
    expect(allRatings).toHaveLength(6);
  });
});
