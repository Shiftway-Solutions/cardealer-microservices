/**
 * Analytics & User Behavior Tracking Types
 *
 * Comprehensive type system for tracking both logged-in and anonymous users,
 * their devices, behavior signals, and predicted lead scores.
 */

// =============================================================================
// DEVICE & SESSION
// =============================================================================

/** Device types detected via User-Agent */
export type DeviceType = 'desktop' | 'tablet' | 'mobile' | 'bot' | 'unknown';

/** Operating system */
export type OSFamily = 'Windows' | 'macOS' | 'iOS' | 'Android' | 'Linux' | 'ChromeOS' | 'Unknown';

/** Browser family */
export type BrowserFamily =
  | 'Chrome'
  | 'Firefox'
  | 'Safari'
  | 'Edge'
  | 'Opera'
  | 'Samsung Internet'
  | 'UC Browser'
  | 'Unknown';

/** Device fingerprint — identifies a unique device/browser combo */
export interface DeviceInfo {
  deviceType: DeviceType;
  os: OSFamily;
  osVersion?: string;
  browser: BrowserFamily;
  browserVersion?: string;
  screenWidth: number;
  screenHeight: number;
  language: string;
  timezone: string;
  /** Canvas + WebGL fingerprint hash */
  fingerprint: string;
  /** Is touch-capable device */
  isTouch: boolean;
  /** Connection type (4g, wifi, etc.) */
  connectionType?: string;
}

/** Visitor session — tracks a browsing session */
export interface VisitorSession {
  sessionId: string;
  /** Anonymous fingerprint if not logged in */
  anonymousId: string;
  /** Logged-in userId or null */
  userId: string | null;
  device: DeviceInfo;
  /** UTM params */
  utm?: {
    source?: string;
    medium?: string;
    campaign?: string;
    term?: string;
    content?: string;
  };
  /** Referrer URL */
  referrer?: string;
  /** First page landed on */
  landingPage: string;
  /** Session start timestamp ISO */
  startedAt: string;
  /** Last activity timestamp */
  lastActivityAt: string;
  /** Total pages viewed in session */
  pageCount: number;
  /** Total time in seconds */
  durationSeconds: number;
  /** Geographic location (from IP) */
  geo?: {
    country?: string;
    region?: string;
    city?: string;
  };
}

// =============================================================================
// BEHAVIOR EVENTS
// =============================================================================

/** All trackable event types */
export type TrackingEventType =
  // Navigation
  | 'page_view'
  | 'session_start'
  | 'session_end'
  // Search
  | 'search_performed'
  | 'search_filter_applied'
  | 'search_results_viewed'
  // Vehicle interactions
  | 'vehicle_viewed'
  | 'vehicle_gallery_opened'
  | 'vehicle_360_viewed'
  | 'vehicle_specs_expanded'
  | 'vehicle_price_history_viewed'
  | 'vehicle_shared'
  | 'vehicle_compared'
  // Engagement
  | 'favorite_added'
  | 'favorite_removed'
  | 'save_search'
  // Contact intent (HIGH value signals)
  | 'dealer_call_clicked'
  | 'dealer_whatsapp_clicked'
  | 'dealer_message_sent'
  | 'contact_form_submitted'
  | 'test_drive_requested'
  | 'price_negotiation_started'
  // Financial
  | 'financing_calculator_used'
  | 'insurance_quote_requested'
  | 'payment_page_visited'
  // OKLA Score
  | 'okla_score_checked'
  | 'vin_lookup_performed'
  // Conversion
  | 'listing_created'
  | 'campaign_created'
  | 'subscription_started'
  // Other
  | 'chatbot_interaction'
  | 'notification_clicked'
  | 'external_link_clicked';

/** Base tracking event */
export interface TrackingEvent {
  eventId: string;
  eventType: TrackingEventType;
  sessionId: string;
  anonymousId: string;
  userId: string | null;
  timestamp: string;
  /** Device fingerprint hash */
  deviceFingerprint: string;
  /** Page URL where event occurred */
  pageUrl: string;
  /** Custom properties per event type */
  properties: Record<string, unknown>;
}

/** Vehicle view event properties */
export interface VehicleViewProperties {
  vehicleId: string;
  make: string;
  model: string;
  year: number;
  price: number;
  dealerId?: string;
  sellerId?: string;
  /** Time spent on vehicle page in seconds */
  viewDuration: number;
  /** Scroll depth percentage */
  scrollDepth: number;
  /** Sections viewed */
  sectionsViewed: string[];
  /** From search or direct */
  source: 'search' | 'featured' | 'recommended' | 'direct' | 'shared' | 'ad';
}

/** Search event properties */
export interface SearchProperties {
  query?: string;
  filters: {
    make?: string;
    model?: string;
    yearMin?: number;
    yearMax?: number;
    priceMin?: number;
    priceMax?: number;
    condition?: string;
    fuelType?: string;
    transmission?: string;
    location?: string;
  };
  resultsCount: number;
  page: number;
}

/** Contact intent event properties */
export interface ContactIntentProperties {
  vehicleId: string;
  dealerId?: string;
  sellerId?: string;
  contactMethod: 'call' | 'whatsapp' | 'message' | 'form' | 'test_drive';
  vehiclePrice?: number;
}

// =============================================================================
// LEAD SCORING
// =============================================================================

/** Lead score level */
export type LeadScoreLevel = 'hot' | 'warm' | 'cold' | 'inactive';

/** Lead score breakdown by behavior category */
export interface LeadScoreBreakdown {
  /** Frequency & recency of visits (0-25) */
  engagementScore: number;
  /** Search specificity & vehicle interest depth (0-25) */
  intentScore: number;
  /** Contact actions: calls, messages, WhatsApp (0-30) */
  contactScore: number;
  /** Financial readiness: financing calc, payment views (0-20) */
  financialReadinessScore: number;
}

/** A scored lead (buyer) */
export interface PredictedLead {
  /** Visitor ID (userId or anonymousId) */
  visitorId: string;
  isAnonymous: boolean;
  /** User name if logged in */
  userName?: string;
  email?: string;
  phone?: string;

  /** Total lead score 0-100 */
  totalScore: number;
  level: LeadScoreLevel;
  breakdown: LeadScoreBreakdown;

  /** Device used most recently */
  device: DeviceInfo;

  /** Behavioral signals */
  signals: LeadSignal[];

  /** Vehicles they're most interested in */
  interestedVehicles: InterestedVehicle[];
  /** Preferred vehicle profile */
  preferredProfile: VehiclePreference;

  /** Predicted conversion probability (0.0 - 1.0) */
  conversionProbability: number;
  /** Estimated time to purchase (days) */
  estimatedDaysToPurchase: number;
  /** Recommended action for the dealer */
  recommendedAction: string;

  /** Timestamps */
  firstSeen: string;
  lastSeen: string;
  totalSessions: number;
  totalPageViews: number;
  totalTimeSpentMinutes: number;
}

/** A specific behavioral signal */
export interface LeadSignal {
  type: TrackingEventType;
  label: string;
  /** Importance: high, medium, low */
  importance: 'high' | 'medium' | 'low';
  count: number;
  lastOccurred: string;
  /** Points contributed to lead score */
  pointsContributed: number;
}

/** A vehicle the lead is interested in */
export interface InterestedVehicle {
  vehicleId: string;
  title: string;
  image?: string;
  price: number;
  /** Number of views */
  viewCount: number;
  /** Total time spent viewing (seconds) */
  totalViewTime: number;
  /** Made contact about this vehicle */
  contacted: boolean;
  /** Added to favorites */
  favorited: boolean;
  lastViewed: string;
  /** Interest score for this specific vehicle */
  interestScore: number;
}

/** The inferred vehicle preference profile */
export interface VehiclePreference {
  preferredMakes: string[];
  preferredModels: string[];
  yearRange: { min: number; max: number };
  priceRange: { min: number; max: number };
  preferredCondition: 'new' | 'used' | 'both';
  preferredFuelType?: string;
  preferredTransmission?: string;
  preferredBodyType?: string;
}

// =============================================================================
// ANALYTICS DASHBOARD
// =============================================================================

/** Summary stats for admin dashboard */
export interface AnalyticsSummary {
  period: string;
  totalVisitors: number;
  uniqueVisitors: number;
  returningVisitors: number;
  avgSessionDuration: number;
  avgPagesPerSession: number;
  bounceRate: number;
  topDevices: { type: DeviceType; count: number; percentage: number }[];
  topBrowsers: { browser: BrowserFamily; count: number }[];
  topOS: { os: OSFamily; count: number }[];
  topPages: { path: string; views: number }[];
  topSearches: { query: string; count: number }[];
  topVehiclesViewed: { vehicleId: string; title: string; views: number }[];
  /** Leads summary */
  hotLeads: number;
  warmLeads: number;
  coldLeads: number;
  conversionRate: number;
}

/** Real-time visitors */
export interface RealTimeMetrics {
  activeVisitors: number;
  activeByDevice: Record<DeviceType, number>;
  recentEvents: TrackingEvent[];
}

// =============================================================================
// API REQUEST/RESPONSE
// =============================================================================

export interface TrackEventRequest {
  eventType: TrackingEventType;
  sessionId: string;
  anonymousId: string;
  userId?: string;
  deviceFingerprint: string;
  pageUrl: string;
  properties: Record<string, unknown>;
}

export interface TrackBatchRequest {
  events: TrackEventRequest[];
  device: DeviceInfo;
  session: Partial<VisitorSession>;
}

export interface LeadListResponse {
  leads: PredictedLead[];
  totalCount: number;
  hotCount: number;
  warmCount: number;
  coldCount: number;
  avgScore: number;
}
