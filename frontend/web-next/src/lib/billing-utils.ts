/**
 * OKLA Billing Utilities — Plan limits, overage calculation, OKLA Coins
 *
 * Extracted from page-level constants for testability.
 * Source of truth for plan billing logic used across components.
 */

// =============================================================================
// ELITE Overage Constants
// =============================================================================

/** ELITE plan soft limit: conversations/month before overage kicks in */
export const ELITE_SOFT_LIMIT = 2000;

/** Cost per conversation above the ELITE soft limit (in RD$) */
export const OVERAGE_COST_RD = 5;

/** Alert tier thresholds (as fractions of the limit) */
export const ALERT_THRESHOLD_YELLOW = 0.8; // 80% of limit
export const ALERT_THRESHOLD_RED = 0.95; // 95% of limit

// =============================================================================
// Overage Calculation
// =============================================================================

export interface OverageResult {
  overageCount: number;
  overageCost: number;
  isOverLimit: boolean;
  alertLevel: 'none' | 'yellow' | 'red' | 'overage';
  usagePercent: number;
}

/**
 * Calculate ELITE plan overage for chatbot conversations.
 *
 * @param sessionCount - Total conversations this billing period
 * @param softLimit - Soft limit before overage (default: ELITE_SOFT_LIMIT)
 * @param costPerConversation - RD$ cost per overage conversation (default: OVERAGE_COST_RD)
 * @returns OverageResult with count, cost, and alert level
 */
export function calculateOverage(
  sessionCount: number,
  softLimit: number = ELITE_SOFT_LIMIT,
  costPerConversation: number = OVERAGE_COST_RD
): OverageResult {
  const isOverLimit = sessionCount > softLimit;
  const overageCount = isOverLimit ? sessionCount - softLimit : 0;
  const overageCost = overageCount * costPerConversation;
  const usagePercent = softLimit > 0 ? sessionCount / softLimit : 0;

  let alertLevel: OverageResult['alertLevel'] = 'none';
  if (isOverLimit) {
    alertLevel = 'overage';
  } else if (usagePercent >= ALERT_THRESHOLD_RED) {
    alertLevel = 'red';
  } else if (usagePercent >= ALERT_THRESHOLD_YELLOW) {
    alertLevel = 'yellow';
  }

  return { overageCount, overageCost, isOverLimit, alertLevel, usagePercent };
}
