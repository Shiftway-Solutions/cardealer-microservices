import { NextRequest, NextResponse } from 'next/server';
import type { TrackBatchRequest, TrackEventRequest, DeviceInfo } from '@/types/analytics';

// =============================================================================
// BFF: Analytics Tracking Endpoint
// =============================================================================
// Receives batched tracking events from the client-side TrackingProvider.
// Stores events in-memory for lead scoring, and forwards to backend when available.
// =============================================================================

const API_URL =
  process.env.INTERNAL_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:18443';

// In-memory event store (production would use Redis or backend DB)
// Keyed by anonymousId → array of events
const eventStore = new Map<string, StoredEvent[]>();
const sessionStore = new Map<string, SessionData>();

interface StoredEvent {
  eventType: string;
  timestamp: string;
  pageUrl: string;
  properties: Record<string, unknown>;
  userId: string | null;
}

interface SessionData {
  anonymousId: string;
  userId: string | null;
  device: DeviceInfo;
  firstSeen: string;
  lastSeen: string;
  eventCount: number;
  pageViews: number;
  vehicleViews: Set<string>;
  searches: number;
  contactActions: number;
  favoritesAdded: number;
}

// Max events to keep per visitor (prevent memory overflow)
const MAX_EVENTS_PER_VISITOR = 500;
// Max total visitors tracked in-memory
const MAX_VISITORS = 10_000;

export async function POST(request: NextRequest) {
  try {
    let body: TrackBatchRequest;

    // Support both JSON and Blob (from sendBeacon)
    const contentType = request.headers.get('content-type') || '';
    if (contentType.includes('application/json')) {
      body = await request.json();
    } else {
      const text = await request.text();
      body = JSON.parse(text);
    }

    const { events, device, session } = body;

    if (!events || events.length === 0) {
      return NextResponse.json({ success: true, tracked: 0 });
    }

    const anonymousId = session?.anonymousId || events[0]?.anonymousId || 'unknown';
    const userId = session?.userId || events[0]?.userId || null;

    // Store events in memory
    storeEvents(anonymousId, userId, events, device);

    // Try forwarding to backend (fire-and-forget)
    forwardToBackend(events, device, session).catch(() => {
      /* silent fail */
    });

    return NextResponse.json({
      success: true,
      tracked: events.length,
    });
  } catch (error) {
    console.error('[Analytics Track] Error:', error);
    return NextResponse.json({ success: false, error: 'Invalid tracking data' }, { status: 400 });
  }
}

// =============================================================================
// GET: Retrieve stored analytics data (for lead scoring)
// =============================================================================

export async function GET(request: NextRequest) {
  const type = request.nextUrl.searchParams.get('type') || 'summary';
  const visitorId = request.nextUrl.searchParams.get('visitorId');

  if (type === 'visitor' && visitorId) {
    const events = eventStore.get(visitorId) || [];
    const session = sessionStore.get(visitorId);
    return NextResponse.json({
      success: true,
      data: { events, session: session ? serializeSession(session) : null },
    });
  }

  if (type === 'all-sessions') {
    const sessions: Record<string, unknown>[] = [];
    sessionStore.forEach(data => {
      sessions.push(serializeSession(data));
    });
    return NextResponse.json({
      success: true,
      data: {
        totalVisitors: sessionStore.size,
        sessions: sessions.slice(0, 200), // cap response size
      },
    });
  }

  // Default: summary
  let totalEvents = 0;
  eventStore.forEach(events => {
    totalEvents += events.length;
  });

  let contactActions = 0;
  let vehicleViewers = 0;
  let searchCount = 0;
  const deviceBreakdown: Record<string, number> = {};

  sessionStore.forEach(data => {
    contactActions += data.contactActions;
    if (data.vehicleViews.size > 0) vehicleViewers++;
    searchCount += data.searches;
    const dt = data.device?.deviceType || 'unknown';
    deviceBreakdown[dt] = (deviceBreakdown[dt] || 0) + 1;
  });

  return NextResponse.json({
    success: true,
    data: {
      totalVisitors: sessionStore.size,
      totalEvents,
      contactActions,
      vehicleViewers,
      searchCount,
      deviceBreakdown,
    },
  });
}

// =============================================================================
// HELPERS
// =============================================================================

function storeEvents(
  anonymousId: string,
  userId: string | null,
  events: TrackEventRequest[],
  device: DeviceInfo
) {
  // Clean old visitors if at capacity
  if (sessionStore.size >= MAX_VISITORS && !sessionStore.has(anonymousId)) {
    const oldest = findOldestSession();
    if (oldest) {
      sessionStore.delete(oldest);
      eventStore.delete(oldest);
    }
  }

  // Init or update session
  let session = sessionStore.get(anonymousId);
  if (!session) {
    session = {
      anonymousId,
      userId,
      device: device || ({} as DeviceInfo),
      firstSeen: new Date().toISOString(),
      lastSeen: new Date().toISOString(),
      eventCount: 0,
      pageViews: 0,
      vehicleViews: new Set(),
      searches: 0,
      contactActions: 0,
      favoritesAdded: 0,
    };
    sessionStore.set(anonymousId, session);
  }

  // Update userId if now logged in
  if (userId && !session.userId) {
    session.userId = userId;
  }
  if (device) {
    session.device = device;
  }
  session.lastSeen = new Date().toISOString();

  // Get or create event list
  let storedEvents = eventStore.get(anonymousId);
  if (!storedEvents) {
    storedEvents = [];
    eventStore.set(anonymousId, storedEvents);
  }

  // Process each event
  for (const event of events) {
    // Store event (capped)
    if (storedEvents.length < MAX_EVENTS_PER_VISITOR) {
      storedEvents.push({
        eventType: event.eventType,
        timestamp: new Date().toISOString(),
        pageUrl: event.pageUrl,
        properties: event.properties,
        userId: event.userId || null,
      });
    }

    session.eventCount++;

    // Update session counters
    switch (event.eventType) {
      case 'page_view':
        session.pageViews++;
        break;
      case 'vehicle_viewed':
        if (event.properties?.vehicleId) {
          session.vehicleViews.add(event.properties.vehicleId as string);
        }
        break;
      case 'search_performed':
      case 'search_filter_applied':
        session.searches++;
        break;
      case 'dealer_call_clicked':
      case 'dealer_whatsapp_clicked':
      case 'dealer_message_sent':
      case 'contact_form_submitted':
      case 'test_drive_requested':
      case 'price_negotiation_started':
        session.contactActions++;
        break;
      case 'favorite_added':
        session.favoritesAdded++;
        break;
    }
  }
}

function serializeSession(data: SessionData): Record<string, unknown> {
  return {
    ...data,
    vehicleViews: Array.from(data.vehicleViews),
  };
}

function findOldestSession(): string | null {
  let oldest: string | null = null;
  let oldestTime = Infinity;

  sessionStore.forEach((data, key) => {
    const time = new Date(data.lastSeen).getTime();
    if (time < oldestTime) {
      oldestTime = time;
      oldest = key;
    }
  });

  return oldest;
}

async function forwardToBackend(
  events: TrackEventRequest[],
  device: DeviceInfo,
  session: Partial<{ sessionId: string; anonymousId: string; userId: string | null }>
) {
  try {
    await fetch(`${API_URL}/api/analytics/events`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ events, device, session }),
    });
  } catch {
    // Backend may not have analytics endpoint yet — that's OK
  }
}
