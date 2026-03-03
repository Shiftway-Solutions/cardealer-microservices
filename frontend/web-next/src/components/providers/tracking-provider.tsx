/**
 * Tracking Provider — Global behavior tracking for OKLA
 *
 * Wraps the app to automatically track page views, session data, and device info.
 * Works for both logged-in and anonymous users.
 * Batches events and sends them every 10 seconds or on page unload.
 */

'use client';

import { createContext, useContext, useCallback, useEffect, useRef, type ReactNode } from 'react';
import { usePathname } from 'next/navigation';
import { useAuth } from '@/hooks/use-auth';
import {
  getDeviceInfo,
  getSessionId,
  getAnonymousId,
  getUtmParams,
} from '@/lib/device-fingerprint';
import type { TrackingEventType, DeviceInfo, TrackEventRequest } from '@/types/analytics';

// =============================================================================
// CONTEXT
// =============================================================================

interface TrackingContextValue {
  /** Track a single event */
  track: (eventType: TrackingEventType, properties?: Record<string, unknown>) => void;
  /** Track a vehicle view with timing */
  trackVehicleView: (vehicleId: string, meta: Record<string, unknown>) => void;
  /** Get current session info */
  sessionId: string;
  anonymousId: string;
  deviceInfo: DeviceInfo;
}

const TrackingContext = createContext<TrackingContextValue | null>(null);

// =============================================================================
// CONSTANTS
// =============================================================================

const BATCH_INTERVAL_MS = 10_000; // Send events every 10 seconds
const MAX_BATCH_SIZE = 50; // Max events per batch
const TRACKING_ENDPOINT = '/api/analytics/track';

// =============================================================================
// PROVIDER
// =============================================================================

export function TrackingProvider({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const { user } = useAuth();

  // Stable refs
  const sessionIdRef = useRef<string>('');
  const anonymousIdRef = useRef<string>('');
  const deviceInfoRef = useRef<DeviceInfo | null>(null);
  const eventQueueRef = useRef<TrackEventRequest[]>([]);
  const lastPageRef = useRef<string>('');
  const pageStartRef = useRef<number>(Date.now());

  // Initialize on mount (client-side only)
  useEffect(() => {
    sessionIdRef.current = getSessionId();
    anonymousIdRef.current = getAnonymousId();
    deviceInfoRef.current = getDeviceInfo();

    // Track session start
    enqueueEvent('session_start', {
      landingPage: window.location.pathname,
      referrer: document.referrer || undefined,
      utm: getUtmParams(),
    });

    return () => {
      // Flush on unmount
      flushEvents();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Track page views on route change
  useEffect(() => {
    if (!pathname || pathname === lastPageRef.current) return;

    // If there was a previous page, track time spent
    if (lastPageRef.current) {
      const timeOnPage = Math.round((Date.now() - pageStartRef.current) / 1000);
      enqueueEvent('page_view', {
        path: pathname,
        previousPage: lastPageRef.current,
        timeOnPreviousPage: timeOnPage,
        title: typeof document !== 'undefined' ? document.title : '',
      });
    } else {
      enqueueEvent('page_view', {
        path: pathname,
        title: typeof document !== 'undefined' ? document.title : '',
      });
    }

    lastPageRef.current = pathname;
    pageStartRef.current = Date.now();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [pathname]);

  // Periodic flush
  useEffect(() => {
    const interval = setInterval(flushEvents, BATCH_INTERVAL_MS);
    return () => clearInterval(interval);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Flush on page hide (mobile-friendly replacement for beforeunload)
  useEffect(() => {
    const handleVisibilityChange = () => {
      if (document.visibilityState === 'hidden') {
        flushEvents();
      }
    };

    const handleBeforeUnload = () => {
      flushEvents();
    };

    document.addEventListener('visibilitychange', handleVisibilityChange);
    window.addEventListener('beforeunload', handleBeforeUnload);

    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange);
      window.removeEventListener('beforeunload', handleBeforeUnload);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Enqueue an event
  function enqueueEvent(eventType: TrackingEventType, properties: Record<string, unknown> = {}) {
    const event: TrackEventRequest = {
      eventType,
      sessionId: sessionIdRef.current,
      anonymousId: anonymousIdRef.current,
      userId: user?.id || undefined,
      deviceFingerprint: deviceInfoRef.current?.fingerprint || '',
      pageUrl: typeof window !== 'undefined' ? window.location.pathname : '',
      properties,
    };

    eventQueueRef.current.push(event);

    // Auto-flush if batch is full
    if (eventQueueRef.current.length >= MAX_BATCH_SIZE) {
      flushEvents();
    }
  }

  // Flush events to server
  function flushEvents() {
    const events = eventQueueRef.current.splice(0);
    if (events.length === 0) return;

    const body = JSON.stringify({
      events,
      device: deviceInfoRef.current,
      session: {
        sessionId: sessionIdRef.current,
        anonymousId: anonymousIdRef.current,
        userId: user?.id || null,
      },
    });

    // Use sendBeacon for reliability (works even on page close)
    if (typeof navigator !== 'undefined' && navigator.sendBeacon) {
      const blob = new Blob([body], { type: 'application/json' });
      const sent = navigator.sendBeacon(TRACKING_ENDPOINT, blob);
      if (!sent) {
        // Fallback to fetch
        fetch(TRACKING_ENDPOINT, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body,
          keepalive: true,
        }).catch(() => {
          /* silent fail for tracking */
        });
      }
    } else {
      fetch(TRACKING_ENDPOINT, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body,
        keepalive: true,
      }).catch(() => {
        /* silent fail for tracking */
      });
    }
  }

  // Public track function
  const track = useCallback(
    (eventType: TrackingEventType, properties?: Record<string, unknown>) => {
      enqueueEvent(eventType, properties || {});
    },
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [user?.id]
  );

  // Track vehicle views with duration
  const trackVehicleView = useCallback(
    (vehicleId: string, meta: Record<string, unknown>) => {
      enqueueEvent('vehicle_viewed', { vehicleId, ...meta });
    },
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [user?.id]
  );

  const value: TrackingContextValue = {
    track,
    trackVehicleView,
    sessionId: sessionIdRef.current,
    anonymousId: anonymousIdRef.current,
    deviceInfo: deviceInfoRef.current || ({} as DeviceInfo),
  };

  return <TrackingContext.Provider value={value}>{children}</TrackingContext.Provider>;
}

// =============================================================================
// HOOK
// =============================================================================

export function useTracking() {
  const ctx = useContext(TrackingContext);
  if (!ctx) {
    // Return a no-op tracker if used outside provider
    return {
      track: () => {},
      trackVehicleView: () => {},
      sessionId: '',
      anonymousId: '',
      deviceInfo: {} as DeviceInfo,
    } as TrackingContextValue;
  }
  return ctx;
}
