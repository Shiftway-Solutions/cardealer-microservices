/**
 * useReportPurchase — Hook for OKLA Score™ report purchase state
 *
 * Manages whether a report has been purchased for a given vehicle,
 * combining local cache (localStorage) with API verification fallback.
 */

'use client';

import { useState, useEffect, useCallback } from 'react';
import {
  isReportPurchasedLocally,
  saveReportPurchaseLocally,
  checkReportPurchase,
} from '@/services/report-purchase';

interface UseReportPurchaseReturn {
  /** Whether the report for this vehicle has been purchased */
  isPurchased: boolean;
  /** Mark a report as purchased (called after successful payment) */
  markPurchased: (purchaseId: string, buyerEmail: string) => void;
}

/**
 * Check and track purchase state for a vehicle's OKLA Score report.
 * First checks localStorage, then falls back to API check.
 *
 * @param vehicleId - The vehicle ID to check purchase state for
 */
export function useReportPurchase(vehicleId: string | undefined): UseReportPurchaseReturn {
  const [isPurchased, setIsPurchased] = useState(() => {
    if (!vehicleId) return false;
    return isReportPurchasedLocally(vehicleId);
  });

  // Re-check when vehicleId changes: first local, then API fallback
  useEffect(() => {
    if (!vehicleId) return;

    const localResult = isReportPurchasedLocally(vehicleId);
    setIsPurchased(localResult);

    // If not found locally, check API in case purchase was made on another device
    if (!localResult) {
      checkReportPurchase(vehicleId)
        .then(result => {
          if (result.success && result.data?.purchased && result.data.purchaseId) {
            // Sync API result to localStorage
            saveReportPurchaseLocally(vehicleId, result.data.purchaseId, '');
            setIsPurchased(true);
          }
        })
        .catch(() => {
          // Silent fail — local cache is the primary source
        });
    }
  }, [vehicleId]);

  const markPurchased = useCallback(
    (purchaseId: string, buyerEmail: string) => {
      if (!vehicleId) return;
      saveReportPurchaseLocally(vehicleId, purchaseId, buyerEmail);
      setIsPurchased(true);
    },
    [vehicleId]
  );

  return { isPurchased, markPurchased };
}
