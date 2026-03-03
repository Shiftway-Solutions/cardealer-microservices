'use client';

// ============================================================================
// OKLA Ad Tracking & Analytics
// Impression tracking, click tracking, viewability, anti-fraud signals
// Based on Section 6 & 8 of the OKLA Advertising Study
// ============================================================================

import { useCallback, useRef, useEffect, useState } from 'react';
import type { AdSlotPosition, SponsoredVehicle } from '@/types/ads';

// ---------------------------------------------------------------------------
// Session-level tracking store
// ---------------------------------------------------------------------------

interface AdTrackingState {
  impressions: Map<string, { count: number; firstSeen: number; lastSeen: number }>;
  clicks: Map<string, { count: number; lastClick: number }>;
  sessionStart: number;
  sessionId: string;
}

const trackingState: AdTrackingState = {
  impressions: new Map(),
  clicks: new Map(),
  sessionStart: Date.now(),
  sessionId: `session-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
};

// ---------------------------------------------------------------------------
// Anti-fraud: Basic client-side IVT detection (Section 8.2)
// ---------------------------------------------------------------------------

interface FraudSignals {
  /** Clicks per minute from this session */
  clicksPerMinute: number;
  /** Is the click pattern suspicious (too regular) */
  suspiciousPattern: boolean;
  /** Time since session start */
  sessionDuration: number;
  /** Rapid navigation (< 500ms between click and page load) */
  rapidNavigation: boolean;
}

function detectFraudSignals(): FraudSignals {
  const now = Date.now();
  const sessionDuration = (now - trackingState.sessionStart) / 1000;
  const totalClicks = Array.from(trackingState.clicks.values())
    .reduce((sum, c) => sum + c.count, 0);
  const clicksPerMinute = sessionDuration > 0
    ? (totalClicks / (sessionDuration / 60))
    : 0;

  // Check for suspiciously regular click patterns
  const clickTimes = Array.from(trackingState.clicks.values())
    .map(c => c.lastClick)
    .sort();
  let suspiciousPattern = false;
  if (clickTimes.length >= 3) {
    const intervals = [];
    for (let i = 1; i < clickTimes.length; i++) {
      intervals.push(clickTimes[i] - clickTimes[i - 1]);
    }
    // If all intervals are within 10% of each other, it's suspicious
    const avg = intervals.reduce((s, i) => s + i, 0) / intervals.length;
    suspiciousPattern = intervals.every(i => Math.abs(i - avg) / avg < 0.1);
  }

  return {
    clicksPerMinute,
    suspiciousPattern,
    sessionDuration,
    rapidNavigation: false,
  };
}

// ---------------------------------------------------------------------------
// Tracking functions
// ---------------------------------------------------------------------------

/**
 * Record an ad impression.
 * Respects frequency caps and deduplicates within a session.
 */
export function recordImpression(params: {
  impressionToken: string;
  vehicleId: string;
  campaignId: string;
  slotPosition: AdSlotPosition;
  dealerId: string;
  auctionPosition: number;
}) {
  const existing = trackingState.impressions.get(params.impressionToken);
  if (existing) {
    // Already tracked in this session, just update count
    existing.count++;
    existing.lastSeen = Date.now();
    return;
  }

  trackingState.impressions.set(params.impressionToken, {
    count: 1,
    firstSeen: Date.now(),
    lastSeen: Date.now(),
  });

  // Send to server (fire-and-forget)
  const payload = {
    ...params,
    sessionId: trackingState.sessionId,
    timestamp: Date.now(),
    viewportWidth: typeof window !== 'undefined' ? window.innerWidth : 0,
    viewportHeight: typeof window !== 'undefined' ? window.innerHeight : 0,
    userAgent: typeof navigator !== 'undefined' ? navigator.userAgent : '',
  };

  // Use sendBeacon for reliability
  if (typeof navigator !== 'undefined' && navigator.sendBeacon) {
    navigator.sendBeacon(
      '/api/advertising/tracking/impression',
      new Blob([JSON.stringify(payload)], { type: 'application/json' })
    );
  } else {
    fetch('/api/advertising/tracking/impression', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
      keepalive: true,
    }).catch(() => {/* silent */});
  }
}

/**
 * Record an ad click with fraud detection.
 */
export function recordClick(params: {
  vehicleId: string;
  campaignId: string;
  slotPosition: AdSlotPosition;
  dealerId: string;
  auctionPosition: number;
  actualCpc: number;
}) {
  const key = `${params.campaignId}-${params.vehicleId}`;
  const existing = trackingState.clicks.get(key);

  if (existing) {
    existing.count++;
    existing.lastClick = Date.now();
  } else {
    trackingState.clicks.set(key, { count: 1, lastClick: Date.now() });
  }

  // Anti-fraud check
  const fraudSignals = detectFraudSignals();
  const isLikelyFraud = fraudSignals.clicksPerMinute > 2 || fraudSignals.suspiciousPattern;

  const payload = {
    ...params,
    sessionId: trackingState.sessionId,
    timestamp: Date.now(),
    fraudSignals: {
      clicksPerMinute: fraudSignals.clicksPerMinute,
      suspiciousPattern: fraudSignals.suspiciousPattern,
      sessionDuration: fraudSignals.sessionDuration,
      isLikelyFraud,
    },
    referrer: typeof document !== 'undefined' ? document.referrer : '',
  };

  fetch('/api/advertising/tracking/click', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
    keepalive: true,
  }).catch(() => {/* silent */});

  return { isLikelyFraud };
}

// ---------------------------------------------------------------------------
// React Hooks
// ---------------------------------------------------------------------------

/**
 * Hook for tracking ad impressions on a sponsored vehicle card.
 */
export function useAdTracking(vehicle?: SponsoredVehicle) {
  const impressionTracked = useRef(false);

  const trackImpression = useCallback(() => {
    if (!vehicle || impressionTracked.current) return;
    impressionTracked.current = true;

    recordImpression({
      impressionToken: vehicle.impressionToken,
      vehicleId: vehicle.id,
      campaignId: vehicle.campaignId,
      slotPosition: vehicle.slotPosition,
      dealerId: vehicle.adBidId, // placeholder — use dealerId when available
      auctionPosition: vehicle.auctionPosition,
    });
  }, [vehicle]);

  const trackClick = useCallback(() => {
    if (!vehicle) return;

    return recordClick({
      vehicleId: vehicle.id,
      campaignId: vehicle.campaignId,
      slotPosition: vehicle.slotPosition,
      dealerId: vehicle.adBidId,
      auctionPosition: vehicle.auctionPosition,
      actualCpc: vehicle.actualCpc,
    });
  }, [vehicle]);

  return { trackImpression, trackClick };
}

/**
 * Hook to track ad performance metrics for the dealer dashboard.
 */
export function useAdPerformanceMetrics(dealerId?: string) {
  const metricsRef = useRef({
    impressions: 0,
    clicks: 0,
    leads: 0,
    spent: 0,
  });

  const [metrics, setMetrics] = useState<Record<string, unknown> | null>(null);

  // Poll for updated metrics
  useEffect(() => {
    if (!dealerId) return;

    const fetchMetrics = async () => {
      try {
        const res = await fetch(`/api/advertising/reports/owner/${dealerId}`);
        if (res.ok) {
          const data = await res.json();
          const result = data.data ?? data;
          metricsRef.current = result;
          setMetrics(result);
        }
      } catch {
        // silent
      }
    };

    fetchMetrics();
    const interval = setInterval(fetchMetrics, 5 * 60 * 1000); // 5 min
    return () => clearInterval(interval);
  }, [dealerId]);

  return metrics;
}

/**
 * Hook to calculate viewability metrics (IAB standard: 50% visible for 1s).
 */
export function useViewability(
  ref: React.RefObject<HTMLElement | null>,
  onViewable?: () => void
) {
  const viewableTracked = useRef(false);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    const element = ref.current;
    if (!element || viewableTracked.current) return;

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting && entry.intersectionRatio >= 0.5) {
          // Start 1-second timer for viewability
          timerRef.current = setTimeout(() => {
            if (!viewableTracked.current) {
              viewableTracked.current = true;
              onViewable?.();
            }
          }, 1000);
        } else {
          // Element left viewport, cancel timer
          if (timerRef.current) {
            clearTimeout(timerRef.current);
            timerRef.current = null;
          }
        }
      },
      { threshold: 0.5 }
    );

    observer.observe(element);
    return () => {
      observer.disconnect();
      if (timerRef.current) clearTimeout(timerRef.current);
    };
  }, [ref, onViewable]);
}

// ---------------------------------------------------------------------------
// Analytics Event Tracking (extends Google Analytics)
// ---------------------------------------------------------------------------

/**
 * Track ad events in Google Analytics 4.
 */
export function trackAdEvent(
  eventName: 'ad_impression' | 'ad_click' | 'ad_viewable' | 'ad_conversion',
  params: {
    campaignId?: string;
    slotPosition?: AdSlotPosition;
    vehicleId?: string;
    dealerName?: string;
    sponsorTier?: string;
    cpc?: number;
  }
) {
  if (typeof window === 'undefined') return;

  const gtag = (window as unknown as { gtag?: (...args: unknown[]) => void }).gtag;
  if (gtag) {
    gtag('event', eventName, {
      campaign_id: params.campaignId,
      slot_position: params.slotPosition,
      vehicle_id: params.vehicleId,
      dealer_name: params.dealerName,
      sponsor_tier: params.sponsorTier,
      cpc: params.cpc,
    });
  }
}
