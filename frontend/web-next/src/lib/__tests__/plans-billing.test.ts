import { describe, it, expect } from 'vitest';
import {
  calculateOverage,
  ELITE_SOFT_LIMIT,
  STARTER_SOFT_LIMIT,
  OVERAGE_COST_RD,
  ALERT_THRESHOLD_YELLOW,
  ALERT_THRESHOLD_RED,
} from '../billing-utils';
import { DealerPlan, DEALER_PLAN_LIMITS } from '@/lib/plan-config';

// =============================================================================
// OKLA Plans & Billing — Unit Tests
// =============================================================================
// Validates:
//   1. Plan feature tiers (LIBRE < VISIBLE < PRO < ÉLITE)
//   2. OKLA Coins credits progression
//   3. ChatAgent limits per plan
//   4. ELITE overage calculation
//   5. Alert thresholds
//   6. Edge cases (zero, negative, boundary)
// =============================================================================

describe('Dealer Plan Features', () => {
  it('should define exactly 6 plans', () => {
    expect(Object.values(DealerPlan)).toHaveLength(6);
    expect(DealerPlan.LIBRE).toBe('libre');
    expect(DealerPlan.VISIBLE).toBe('visible');
    expect(DealerPlan.STARTER).toBe('starter');
    expect(DealerPlan.PRO).toBe('pro');
    expect(DealerPlan.ELITE).toBe('elite');
    expect(DealerPlan.ENTERPRISE).toBe('enterprise');
  });

  it('should have limits defined for every plan', () => {
    for (const plan of Object.values(DealerPlan)) {
      expect(DEALER_PLAN_LIMITS[plan]).toBeDefined();
    }
  });

  describe('LIBRE plan', () => {
    const features = DEALER_PLAN_LIMITS[DealerPlan.LIBRE];

    it('should allow unlimited listings', () => {
      expect(features.maxListings).toBeGreaterThan(0);
    });

    it('should have basic image limit', () => {
      expect(features.maxImages).toBe(5);
    });

    it('should NOT have advanced features', () => {
      expect(features.analyticsAccess).toBe(false);
      expect(features.marketPriceAnalysis).toBe(false);
      expect(features.bulkUpload).toBe(false);
      expect(features.leadManagement).toBe(false);
      expect(features.emailAutomation).toBe(false);
      expect(features.customBranding).toBe(false);
      expect(features.apiAccess).toBe(false);
      expect(features.prioritySupport).toBe(false);
      expect(features.whatsappIntegration).toBe(false);
    });

    it('should have zero featured listings', () => {
      expect(features.featuredListings).toBe(0);
    });

    it('should have zero OKLA Coins credits', () => {
      expect(features.monthlyOklaCoinsCredits).toBe(0);
    });

    it('should have zero ChatAgent conversations', () => {
      expect(features.chatAgentWeb).toBe(0);
      expect(features.chatAgentWhatsApp).toBe(0);
    });

    it('should have standard search priority', () => {
      expect(features.searchPriority).toBe('standard');
    });

    it('should have no badge', () => {
      expect(features.badgeType).toBe('none');
    });
  });

  describe('VISIBLE plan', () => {
    const features = DEALER_PLAN_LIMITS[DealerPlan.VISIBLE];

    it('should allow more images than LIBRE', () => {
      expect(features.maxImages).toBeGreaterThan(DEALER_PLAN_LIMITS[DealerPlan.LIBRE].maxImages);
    });

    it('should enable analytics access', () => {
      expect(features.analyticsAccess).toBe(true);
    });

    it('should enable bulk upload', () => {
      expect(features.bulkUpload).toBe(true);
    });

    it('should enable lead management (CRM)', () => {
      expect(features.leadManagement).toBe(true);
    });

    it('should have featured listings > 0', () => {
      expect(features.featuredListings).toBeGreaterThan(0);
    });

    it('should receive OKLA Coins credits', () => {
      expect(features.monthlyOklaCoinsCredits).toBeGreaterThan(0);
    });

    it('should have verified badge', () => {
      expect(features.badgeType).toBe('verified');
    });
  });

  describe('PRO plan', () => {
    const features = DEALER_PLAN_LIMITS[DealerPlan.PRO];

    it('should allow more images than VISIBLE', () => {
      expect(features.maxImages).toBeGreaterThan(DEALER_PLAN_LIMITS[DealerPlan.VISIBLE].maxImages);
    });

    it('should enable market price analysis', () => {
      expect(features.marketPriceAnalysis).toBe(true);
    });

    it('should enable email automation', () => {
      expect(features.emailAutomation).toBe(true);
    });

    it('should enable custom branding', () => {
      expect(features.customBranding).toBe(true);
    });

    it('should enable WhatsApp integration', () => {
      expect(features.whatsappIntegration).toBe(true);
    });

    it('should have ChatAgent web conversations > 0', () => {
      expect(features.chatAgentWeb).toBeGreaterThan(0);
    });

    it('should receive more OKLA Coins than VISIBLE', () => {
      expect(features.monthlyOklaCoinsCredits).toBeGreaterThan(
        DEALER_PLAN_LIMITS[DealerPlan.VISIBLE].monthlyOklaCoinsCredits
      );
    });

    it('should have gold badge', () => {
      expect(features.badgeType).toBe('verified-gold');
    });

    it('should NOT have API access', () => {
      expect(features.apiAccess).toBe(false);
    });
  });

  describe('STARTER plan', () => {
    const features = DEALER_PLAN_LIMITS[DealerPlan.STARTER];

    it('should have 12 images', () => {
      expect(features.maxImages).toBe(12);
    });

    it('should have 5 featured listings', () => {
      expect(features.featuredListings).toBe(5);
    });

    it('should have 100 ChatAgent conversations/month', () => {
      expect(features.chatAgentWeb).toBe(100);
      expect(features.chatAgentWhatsApp).toBe(100);
    });

    it('should have $30 OKLA Coins/month', () => {
      expect(features.monthlyOklaCoinsCredits).toBe(30);
    });

    it('should have verified-plus badge', () => {
      expect(features.badgeType).toBe('verified-plus');
    });

    it('should NOT have API access', () => {
      expect(features.apiAccess).toBe(false);
    });
  });

  describe('ELITE plan', () => {
    const features = DEALER_PLAN_LIMITS[DealerPlan.ELITE];

    it('should enable ALL features', () => {
      expect(features.analyticsAccess).toBe(true);
      expect(features.marketPriceAnalysis).toBe(true);
      expect(features.bulkUpload).toBe(true);
      expect(features.leadManagement).toBe(true);
      expect(features.emailAutomation).toBe(true);
      expect(features.customBranding).toBe(true);
      expect(features.apiAccess).toBe(true);
      expect(features.prioritySupport).toBe(true);
      expect(features.whatsappIntegration).toBe(true);
    });

    it('should have 2000 ChatAgent conversations/month', () => {
      expect(features.chatAgentWeb).toBe(2000);
      expect(features.chatAgentWhatsApp).toBe(2000);
    });

    it('should have the most OKLA Coins credits', () => {
      expect(features.monthlyOklaCoinsCredits).toBeGreaterThan(
        DEALER_PLAN_LIMITS[DealerPlan.PRO].monthlyOklaCoinsCredits
      );
    });

    it('should have premium badge', () => {
      expect(features.badgeType).toBe('verified-premium');
    });

    it('should have top search priority', () => {
      expect(features.searchPriority).toBe('top');
    });

    it('should have complete dashboard', () => {
      expect(features.dashboardLevel).toBe('complete');
    });

    it('should support export analytics and PDF pricing', () => {
      expect(features.canExportAnalytics).toBe(true);
      expect(features.pricingAgentPdf).toBe(true);
    });
  });

  describe('Feature progression (tier ordering)', () => {
    it('maxImages should increase with plan tier', () => {
      const libre = DEALER_PLAN_LIMITS[DealerPlan.LIBRE].maxImages;
      const visible = DEALER_PLAN_LIMITS[DealerPlan.VISIBLE].maxImages;
      const starter = DEALER_PLAN_LIMITS[DealerPlan.STARTER].maxImages;
      const pro = DEALER_PLAN_LIMITS[DealerPlan.PRO].maxImages;
      const elite = DEALER_PLAN_LIMITS[DealerPlan.ELITE].maxImages;
      expect(libre).toBeLessThan(visible);
      expect(visible).toBeLessThan(starter);
      expect(starter).toBeLessThan(pro);
      expect(pro).toBeLessThanOrEqual(elite);
    });

    it('featuredListings should increase with plan tier', () => {
      const libre = DEALER_PLAN_LIMITS[DealerPlan.LIBRE].featuredListings;
      const visible = DEALER_PLAN_LIMITS[DealerPlan.VISIBLE].featuredListings;
      const starter = DEALER_PLAN_LIMITS[DealerPlan.STARTER].featuredListings;
      const pro = DEALER_PLAN_LIMITS[DealerPlan.PRO].featuredListings;
      const elite = DEALER_PLAN_LIMITS[DealerPlan.ELITE].featuredListings;
      expect(libre).toBeLessThan(visible);
      expect(visible).toBeLessThan(starter);
      expect(starter).toBeLessThan(pro);
      expect(pro).toBeLessThan(elite);
    });

    it('monthlyOklaCoinsCredits should increase with plan tier', () => {
      const libre = DEALER_PLAN_LIMITS[DealerPlan.LIBRE].monthlyOklaCoinsCredits;
      const visible = DEALER_PLAN_LIMITS[DealerPlan.VISIBLE].monthlyOklaCoinsCredits;
      const starter = DEALER_PLAN_LIMITS[DealerPlan.STARTER].monthlyOklaCoinsCredits;
      const pro = DEALER_PLAN_LIMITS[DealerPlan.PRO].monthlyOklaCoinsCredits;
      const elite = DEALER_PLAN_LIMITS[DealerPlan.ELITE].monthlyOklaCoinsCredits;
      expect(libre).toBeLessThan(visible);
      expect(visible).toBeLessThan(starter);
      expect(starter).toBeLessThan(pro);
      expect(pro).toBeLessThan(elite);
    });
  });
});

// =============================================================================
// ELITE Overage Calculation
// =============================================================================

describe('ELITE Overage Calculation', () => {
  describe('Constants', () => {
    it('ELITE soft limit should be 2000 conversations/month', () => {
      expect(ELITE_SOFT_LIMIT).toBe(2000);
    });

    it('STARTER soft limit should be 100 conversations/month', () => {
      expect(STARTER_SOFT_LIMIT).toBe(100);
    });

    it('Overage cost should be RD$5 per conversation', () => {
      expect(OVERAGE_COST_RD).toBe(5);
    });

    it('Yellow alert threshold should be 80%', () => {
      expect(ALERT_THRESHOLD_YELLOW).toBe(0.8);
    });

    it('Red alert threshold should be 95%', () => {
      expect(ALERT_THRESHOLD_RED).toBe(0.95);
    });
  });

  describe('calculateOverage', () => {
    it('should return zero overage when under limit', () => {
      const result = calculateOverage(500);
      expect(result.overageCount).toBe(0);
      expect(result.overageCost).toBe(0);
      expect(result.isOverLimit).toBe(false);
    });

    it('should return zero overage at exactly the limit', () => {
      const result = calculateOverage(ELITE_SOFT_LIMIT);
      expect(result.overageCount).toBe(0);
      expect(result.overageCost).toBe(0);
      expect(result.isOverLimit).toBe(false);
    });

    it('should calculate overage when over limit by 1', () => {
      const result = calculateOverage(ELITE_SOFT_LIMIT + 1);
      expect(result.overageCount).toBe(1);
      expect(result.overageCost).toBe(OVERAGE_COST_RD);
      expect(result.isOverLimit).toBe(true);
    });

    it('should calculate overage for large excess', () => {
      const excess = 500;
      const result = calculateOverage(ELITE_SOFT_LIMIT + excess);
      expect(result.overageCount).toBe(excess);
      expect(result.overageCost).toBe(excess * OVERAGE_COST_RD);
      expect(result.isOverLimit).toBe(true);
    });

    it('should handle zero sessions', () => {
      const result = calculateOverage(0);
      expect(result.overageCount).toBe(0);
      expect(result.overageCost).toBe(0);
      expect(result.isOverLimit).toBe(false);
      expect(result.usagePercent).toBe(0);
    });

    it('should use custom soft limit and cost', () => {
      const result = calculateOverage(150, 100, 10);
      expect(result.overageCount).toBe(50);
      expect(result.overageCost).toBe(500);
      expect(result.isOverLimit).toBe(true);
    });
  });

  describe('Alert levels', () => {
    it('should be "none" for low usage (< 80%)', () => {
      const result = calculateOverage(1000); // 50%
      expect(result.alertLevel).toBe('none');
    });

    it('should be "yellow" at 80% usage', () => {
      const result = calculateOverage(ELITE_SOFT_LIMIT * ALERT_THRESHOLD_YELLOW); // 1600
      expect(result.alertLevel).toBe('yellow');
    });

    it('should be "yellow" between 80% and 95%', () => {
      const result = calculateOverage(ELITE_SOFT_LIMIT * 0.9); // 90%
      expect(result.alertLevel).toBe('yellow');
    });

    it('should be "red" at 95% usage', () => {
      const result = calculateOverage(ELITE_SOFT_LIMIT * ALERT_THRESHOLD_RED); // 1900
      expect(result.alertLevel).toBe('red');
    });

    it('should be "red" between 95% and 100%', () => {
      const result = calculateOverage(ELITE_SOFT_LIMIT * 0.975); // 97.5%
      expect(result.alertLevel).toBe('red');
    });

    it('should be "overage" when over 100%', () => {
      const result = calculateOverage(ELITE_SOFT_LIMIT + 1);
      expect(result.alertLevel).toBe('overage');
    });
  });

  describe('Usage percentage', () => {
    it('should calculate correct usage percent', () => {
      const result = calculateOverage(ELITE_SOFT_LIMIT * 0.5); // 50%
      expect(result.usagePercent).toBe(0.5);
    });

    it('should be 1.0 at exactly the limit', () => {
      const result = calculateOverage(ELITE_SOFT_LIMIT);
      expect(result.usagePercent).toBe(1.0);
    });

    it('should be > 1.0 when over limit', () => {
      const excess = 500;
      const result = calculateOverage(ELITE_SOFT_LIMIT + excess);
      const expected = parseFloat(((ELITE_SOFT_LIMIT + excess) / ELITE_SOFT_LIMIT).toFixed(10));
      expect(result.usagePercent).toBeCloseTo(expected, 5);
    });
  });
});
