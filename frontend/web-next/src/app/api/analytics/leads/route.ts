import { NextRequest, NextResponse } from 'next/server';
import type { PredictedLead, LeadListResponse, DeviceInfo } from '@/types/analytics';

// =============================================================================
// BFF: Lead Prediction & Scoring Endpoint
// =============================================================================
// Analyzes tracked visitor behavior to predict buyer leads.
// Uses the lead-prediction-engine for AI-powered scoring.
// =============================================================================

const _API_URL =
  process.env.INTERNAL_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:18443';

export async function GET(request: NextRequest) {
  const filter = request.nextUrl.searchParams.get('filter') || 'all'; // hot, warm, cold, all
  const sortBy = request.nextUrl.searchParams.get('sort') || 'score'; // score, recent, contacts
  const limit = parseInt(request.nextUrl.searchParams.get('limit') || '50', 10);

  try {
    // First, get all tracked sessions from in-memory store
    const trackRes = await fetch(
      new URL('/api/analytics/track?type=all-sessions', request.nextUrl.origin),
      { cache: 'no-store' }
    );
    const trackData = await trackRes.json();

    if (!trackData.success || !trackData.data?.sessions) {
      // Return demo data
      const demoLeads = generateDemoLeads();
      return NextResponse.json({
        success: true,
        data: buildResponse(demoLeads, filter, sortBy, limit),
      });
    }

    const sessions = trackData.data.sessions;
    const leads: PredictedLead[] = [];

    for (const session of sessions) {
      // Get individual visitor events
      const eventsRes = await fetch(
        new URL(
          `/api/analytics/track?type=visitor&visitorId=${session.anonymousId}`,
          request.nextUrl.origin
        ),
        { cache: 'no-store' }
      );
      const eventsData = await eventsRes.json();
      const events = eventsData.data?.events || [];

      const lead = scoreLead(session, events);
      if (lead.totalScore > 0) {
        leads.push(lead);
      }
    }

    // If no real leads, include demo data
    if (leads.length === 0) {
      const demoLeads = generateDemoLeads();
      return NextResponse.json({
        success: true,
        data: buildResponse(demoLeads, filter, sortBy, limit),
        source: 'demo',
      });
    }

    return NextResponse.json({ success: true, data: buildResponse(leads, filter, sortBy, limit) });
  } catch (error) {
    console.error('[Leads API] Error:', error);
    const demoLeads = generateDemoLeads();
    return NextResponse.json({
      success: true,
      data: buildResponse(demoLeads, filter, sortBy, limit),
      source: 'demo',
    });
  }
}

// =============================================================================
// LEAD SCORING ENGINE (AI-Inspired Rule-Based Model)
// =============================================================================

interface SessionInfo {
  anonymousId: string;
  userId: string | null;
  device: DeviceInfo;
  firstSeen: string;
  lastSeen: string;
  eventCount: number;
  pageViews: number;
  vehicleViews: string[];
  searches: number;
  contactActions: number;
  favoritesAdded: number;
}

interface EventInfo {
  eventType: string;
  timestamp: string;
  pageUrl: string;
  properties: Record<string, unknown>;
  userId: string | null;
}

// Signal weights (total = 100 max)
const WEIGHTS = {
  // ENGAGEMENT (max 25)
  pageViews: 0.5, // per page view, cap at 10pts
  uniqueVehicles: 2, // per unique vehicle, cap at 8pts
  returnVisits: 3, // per return (sessions > 1), cap at 6pts
  sessionDuration: 0.2, // per minute, cap at 4pts (inferred)
  recencyBonus: 5, // if active in last 24h

  // INTENT (max 25)
  searchPerformed: 1, // per search, cap at 5pts
  filterApplied: 2, // filters show specificity, cap at 6pts
  galleryOpened: 2, // deeper exploration, cap at 4pts
  view360: 3, // strong interest
  specsExpanded: 1.5, // comparing details
  priceHistoryViewed: 2, // price-sensitive buyer
  vehicleCompared: 3, // active comparison

  // CONTACT (max 30) — highest weight, strongest signal
  callClicked: 10, // direct contact attempt
  whatsappClicked: 9, // messaging intent
  messageSent: 8, // wrote a message
  contactForm: 7, // filled form
  testDriveRequested: 12, // very strong intent
  negotiationStarted: 10, // price discussion

  // FINANCIAL (max 20)
  financingCalc: 6, // checked financing
  insuranceQuote: 5, // checking insurance
  paymentPageVisited: 8, // near conversion
  favoriteAdded: 1.5, // bookmarking, cap at 6pts
};

function scoreLead(session: SessionInfo, events: EventInfo[]): PredictedLead {
  let engagementScore = 0;
  let intentScore = 0;
  let contactScore = 0;
  let financialReadinessScore = 0;

  const signals: PredictedLead['signals'] = [];
  const vehicleInterest: Map<
    string,
    {
      viewCount: number;
      totalViewTime: number;
      contacted: boolean;
      favorited: boolean;
      lastViewed: string;
      title: string;
      price: number;
      image?: string;
    }
  > = new Map();

  // Count event types
  const eventCounts: Record<string, number> = {};
  for (const ev of events) {
    eventCounts[ev.eventType] = (eventCounts[ev.eventType] || 0) + 1;

    // Track vehicle interest
    if (ev.eventType === 'vehicle_viewed' && ev.properties?.vehicleId) {
      const vid = ev.properties.vehicleId as string;
      const existing = vehicleInterest.get(vid) || {
        viewCount: 0,
        totalViewTime: 0,
        contacted: false,
        favorited: false,
        lastViewed: ev.timestamp,
        title:
          (ev.properties.make || '') +
          ' ' +
          (ev.properties.model || '') +
          ' ' +
          (ev.properties.year || ''),
        price: (ev.properties.price as number) || 0,
        image: ev.properties.image as string | undefined,
      };
      existing.viewCount++;
      existing.totalViewTime += (ev.properties.viewDuration as number) || 30;
      existing.lastViewed = ev.timestamp;
      vehicleInterest.set(vid, existing);
    }

    // Mark contacted vehicles
    if (
      [
        'dealer_call_clicked',
        'dealer_whatsapp_clicked',
        'dealer_message_sent',
        'contact_form_submitted',
      ].includes(ev.eventType) &&
      ev.properties?.vehicleId
    ) {
      const vid = ev.properties.vehicleId as string;
      const existing = vehicleInterest.get(vid);
      if (existing) existing.contacted = true;
    }

    // Mark favorited vehicles
    if (ev.eventType === 'favorite_added' && ev.properties?.vehicleId) {
      const vid = ev.properties.vehicleId as string;
      const existing = vehicleInterest.get(vid);
      if (existing) existing.favorited = true;
    }
  }

  // --- ENGAGEMENT SCORING (0-25) ---
  engagementScore += Math.min(10, session.pageViews * WEIGHTS.pageViews);
  engagementScore += Math.min(8, (session.vehicleViews?.length || 0) * WEIGHTS.uniqueVehicles);

  // Recency bonus: active in last 24h
  const lastSeenHoursAgo = (Date.now() - new Date(session.lastSeen).getTime()) / (1000 * 60 * 60);
  if (lastSeenHoursAgo < 24) engagementScore += WEIGHTS.recencyBonus;
  else if (lastSeenHoursAgo < 72) engagementScore += WEIGHTS.recencyBonus * 0.5;

  engagementScore = Math.min(25, engagementScore);

  // --- INTENT SCORING (0-25) ---
  intentScore += Math.min(5, (eventCounts['search_performed'] || 0) * WEIGHTS.searchPerformed);
  intentScore += Math.min(6, (eventCounts['search_filter_applied'] || 0) * WEIGHTS.filterApplied);
  intentScore += Math.min(4, (eventCounts['vehicle_gallery_opened'] || 0) * WEIGHTS.galleryOpened);
  intentScore += Math.min(3, (eventCounts['vehicle_360_viewed'] || 0) * WEIGHTS.view360);
  intentScore += Math.min(3, (eventCounts['vehicle_specs_expanded'] || 0) * WEIGHTS.specsExpanded);
  intentScore += Math.min(
    2,
    (eventCounts['vehicle_price_history_viewed'] || 0) * WEIGHTS.priceHistoryViewed
  );
  intentScore += Math.min(3, (eventCounts['vehicle_compared'] || 0) * WEIGHTS.vehicleCompared);

  intentScore = Math.min(25, intentScore);

  // --- CONTACT SCORING (0-30) ---
  if (eventCounts['dealer_call_clicked']) {
    contactScore += Math.min(10, eventCounts['dealer_call_clicked'] * WEIGHTS.callClicked);
    addSignal(
      signals,
      'dealer_call_clicked',
      '📞 Llamó al dealer',
      'high',
      eventCounts['dealer_call_clicked'],
      WEIGHTS.callClicked
    );
  }
  if (eventCounts['dealer_whatsapp_clicked']) {
    contactScore += Math.min(9, eventCounts['dealer_whatsapp_clicked'] * WEIGHTS.whatsappClicked);
    addSignal(
      signals,
      'dealer_whatsapp_clicked',
      '💬 WhatsApp al dealer',
      'high',
      eventCounts['dealer_whatsapp_clicked'],
      WEIGHTS.whatsappClicked
    );
  }
  if (eventCounts['dealer_message_sent']) {
    contactScore += Math.min(8, eventCounts['dealer_message_sent'] * WEIGHTS.messageSent);
    addSignal(
      signals,
      'dealer_message_sent',
      '✉️ Mensaje enviado',
      'high',
      eventCounts['dealer_message_sent'],
      WEIGHTS.messageSent
    );
  }
  if (eventCounts['test_drive_requested']) {
    contactScore += Math.min(12, eventCounts['test_drive_requested'] * WEIGHTS.testDriveRequested);
    addSignal(
      signals,
      'test_drive_requested',
      '🚗 Test drive solicitado',
      'high',
      eventCounts['test_drive_requested'],
      WEIGHTS.testDriveRequested
    );
  }
  if (eventCounts['price_negotiation_started']) {
    contactScore += Math.min(
      10,
      eventCounts['price_negotiation_started'] * WEIGHTS.negotiationStarted
    );
    addSignal(
      signals,
      'price_negotiation_started',
      '💰 Negociación iniciada',
      'high',
      eventCounts['price_negotiation_started'],
      WEIGHTS.negotiationStarted
    );
  }

  contactScore = Math.min(30, contactScore);

  // --- FINANCIAL READINESS (0-20) ---
  if (eventCounts['financing_calculator_used']) {
    financialReadinessScore += Math.min(
      6,
      eventCounts['financing_calculator_used'] * WEIGHTS.financingCalc
    );
    addSignal(
      signals,
      'financing_calculator_used',
      '🏦 Calculadora de financiamiento',
      'medium',
      eventCounts['financing_calculator_used'],
      WEIGHTS.financingCalc
    );
  }
  if (eventCounts['insurance_quote_requested']) {
    financialReadinessScore += Math.min(
      5,
      eventCounts['insurance_quote_requested'] * WEIGHTS.insuranceQuote
    );
    addSignal(
      signals,
      'insurance_quote_requested',
      '🛡️ Cotizó seguro',
      'medium',
      eventCounts['insurance_quote_requested'],
      WEIGHTS.insuranceQuote
    );
  }
  if (eventCounts['payment_page_visited']) {
    financialReadinessScore += Math.min(
      8,
      eventCounts['payment_page_visited'] * WEIGHTS.paymentPageVisited
    );
    addSignal(
      signals,
      'payment_page_visited',
      '💳 Visitó página de pago',
      'high',
      eventCounts['payment_page_visited'],
      WEIGHTS.paymentPageVisited
    );
  }
  financialReadinessScore += Math.min(6, (session.favoritesAdded || 0) * WEIGHTS.favoriteAdded);

  financialReadinessScore = Math.min(20, financialReadinessScore);

  // Add engagement/intent signals
  if (session.pageViews > 5)
    addSignal(
      signals,
      'page_view',
      '📄 Múltiples páginas visitadas',
      'low',
      session.pageViews,
      WEIGHTS.pageViews
    );
  if ((session.vehicleViews?.length || 0) > 2)
    addSignal(
      signals,
      'vehicle_viewed',
      '🚘 Múltiples vehículos vistos',
      'medium',
      session.vehicleViews?.length || 0,
      WEIGHTS.uniqueVehicles
    );
  if (session.searches > 2)
    addSignal(
      signals,
      'search_performed',
      '🔍 Búsquedas activas',
      'medium',
      session.searches,
      WEIGHTS.searchPerformed
    );

  // --- TOTAL SCORE ---
  const totalScore = Math.round(
    engagementScore + intentScore + contactScore + financialReadinessScore
  );

  // --- LEVEL ---
  const level =
    totalScore >= 60 ? 'hot' : totalScore >= 35 ? 'warm' : totalScore >= 10 ? 'cold' : 'inactive';

  // --- CONVERSION PROBABILITY (logistic function) ---
  // P = 1 / (1 + e^(-k * (score - midpoint)))
  const k = 0.08;
  const midpoint = 50;
  const conversionProbability = parseFloat(
    (1 / (1 + Math.exp(-k * (totalScore - midpoint)))).toFixed(3)
  );

  // --- ESTIMATED DAYS TO PURCHASE ---
  const estimatedDaysToPurchase =
    totalScore >= 60
      ? Math.max(1, 7 - Math.floor(totalScore / 15))
      : totalScore >= 35
        ? Math.max(7, 30 - Math.floor(totalScore / 3))
        : 60;

  // --- RECOMMENDED ACTION ---
  const recommendedAction =
    totalScore >= 70
      ? 'Contactar inmediatamente — alto interés de compra'
      : totalScore >= 50
        ? 'Enviar oferta personalizada por WhatsApp'
        : totalScore >= 35
          ? 'Enviar información de vehículos similares'
          : totalScore >= 20
            ? 'Incluir en campañas de remarketing'
            : 'Monitorear actividad futura';

  // --- INTERESTED VEHICLES ---
  const interestedVehicles = Array.from(vehicleInterest.entries())
    .map(([vehicleId, data]) => ({
      vehicleId,
      title: data.title.trim() || 'Vehículo',
      image: data.image,
      price: data.price,
      viewCount: data.viewCount,
      totalViewTime: data.totalViewTime,
      contacted: data.contacted,
      favorited: data.favorited,
      lastViewed: data.lastViewed,
      interestScore: Math.min(
        100,
        data.viewCount * 20 +
          (data.contacted ? 30 : 0) +
          (data.favorited ? 15 : 0) +
          Math.min(35, data.totalViewTime / 10)
      ),
    }))
    .sort((a, b) => b.interestScore - a.interestScore)
    .slice(0, 10);

  // --- PREFERRED PROFILE (inferred from viewed vehicles) ---
  const makes: Record<string, number> = {};
  const models: Record<string, number> = {};
  let minPrice = Infinity,
    maxPrice = 0,
    minYear = 9999,
    maxYear = 0;

  for (const ev of events) {
    if (ev.eventType === 'vehicle_viewed' && ev.properties) {
      const make = ev.properties.make as string;
      const model = ev.properties.model as string;
      const price = ev.properties.price as number;
      const year = ev.properties.year as number;

      if (make) makes[make] = (makes[make] || 0) + 1;
      if (model) models[model] = (models[model] || 0) + 1;
      if (price > 0) {
        minPrice = Math.min(minPrice, price);
        maxPrice = Math.max(maxPrice, price);
      }
      if (year > 0) {
        minYear = Math.min(minYear, year);
        maxYear = Math.max(maxYear, year);
      }
    }
  }

  const preferredProfile = {
    preferredMakes: Object.entries(makes)
      .sort((a, b) => b[1] - a[1])
      .slice(0, 3)
      .map(([k]) => k),
    preferredModels: Object.entries(models)
      .sort((a, b) => b[1] - a[1])
      .slice(0, 3)
      .map(([k]) => k),
    yearRange: { min: minYear === 9999 ? 2018 : minYear, max: maxYear === 0 ? 2025 : maxYear },
    priceRange: {
      min: minPrice === Infinity ? 500000 : minPrice,
      max: maxPrice === 0 ? 3000000 : maxPrice,
    },
    preferredCondition: 'both' as const,
  };

  return {
    visitorId: session.userId || session.anonymousId,
    isAnonymous: !session.userId,
    userName: session.userId ? undefined : undefined,
    device: session.device || {
      deviceType: 'unknown' as const,
      os: 'Unknown' as const,
      browser: 'Unknown' as const,
      fingerprint: session.anonymousId,
      screenWidth: 0,
      screenHeight: 0,
      language: 'es',
      timezone: 'America/Santo_Domingo',
      isTouch: false,
    },
    totalScore,
    level,
    breakdown: {
      engagementScore: Math.round(engagementScore),
      intentScore: Math.round(intentScore),
      contactScore: Math.round(contactScore),
      financialReadinessScore: Math.round(financialReadinessScore),
    },
    signals: signals.sort((a, b) => b.pointsContributed - a.pointsContributed),
    interestedVehicles,
    preferredProfile,
    conversionProbability,
    estimatedDaysToPurchase,
    recommendedAction,
    firstSeen: session.firstSeen,
    lastSeen: session.lastSeen,
    totalSessions: 1,
    totalPageViews: session.pageViews,
    totalTimeSpentMinutes: Math.round(session.eventCount * 0.3),
  };
}

function addSignal(
  signals: PredictedLead['signals'],
  type: string,
  label: string,
  importance: 'high' | 'medium' | 'low',
  count: number,
  weight: number
) {
  signals.push({
    type: type as PredictedLead['signals'][0]['type'],
    label,
    importance,
    count,
    lastOccurred: new Date().toISOString(),
    pointsContributed: Math.round(count * weight * 10) / 10,
  });
}

function buildResponse(
  leads: PredictedLead[],
  filter: string,
  sortBy: string,
  limit: number
): LeadListResponse {
  let filtered = leads;

  if (filter !== 'all') {
    filtered = leads.filter(l => l.level === filter);
  }

  switch (sortBy) {
    case 'score':
      filtered.sort((a, b) => b.totalScore - a.totalScore);
      break;
    case 'recent':
      filtered.sort((a, b) => new Date(b.lastSeen).getTime() - new Date(a.lastSeen).getTime());
      break;
    case 'contacts':
      filtered.sort((a, b) => b.breakdown.contactScore - a.breakdown.contactScore);
      break;
  }

  filtered = filtered.slice(0, limit);

  return {
    leads: filtered,
    totalCount: leads.length,
    hotCount: leads.filter(l => l.level === 'hot').length,
    warmCount: leads.filter(l => l.level === 'warm').length,
    coldCount: leads.filter(l => l.level === 'cold').length,
    avgScore:
      leads.length > 0 ? Math.round(leads.reduce((s, l) => s + l.totalScore, 0) / leads.length) : 0,
  };
}

// =============================================================================
// DEMO DATA
// =============================================================================

function generateDemoLeads(): PredictedLead[] {
  const now = new Date();
  const ago = (hours: number) => new Date(now.getTime() - hours * 60 * 60 * 1000).toISOString();

  return [
    {
      visitorId: 'lead-001',
      isAnonymous: false,
      userName: 'María García',
      email: 'maria.garcia@email.com',
      phone: '+1 809-555-0101',
      totalScore: 82,
      level: 'hot',
      breakdown: {
        engagementScore: 22,
        intentScore: 20,
        contactScore: 25,
        financialReadinessScore: 15,
      },
      device: {
        deviceType: 'mobile',
        os: 'iOS',
        browser: 'Safari',
        fingerprint: 'fp-001',
        screenWidth: 390,
        screenHeight: 844,
        language: 'es-DO',
        timezone: 'America/Santo_Domingo',
        isTouch: true,
      },
      signals: [
        {
          type: 'dealer_whatsapp_clicked',
          label: '💬 WhatsApp al dealer',
          importance: 'high',
          count: 3,
          lastOccurred: ago(2),
          pointsContributed: 9,
        },
        {
          type: 'test_drive_requested',
          label: '🚗 Test drive solicitado',
          importance: 'high',
          count: 1,
          lastOccurred: ago(4),
          pointsContributed: 12,
        },
        {
          type: 'financing_calculator_used',
          label: '🏦 Calculadora de financiamiento',
          importance: 'medium',
          count: 2,
          lastOccurred: ago(6),
          pointsContributed: 6,
        },
        {
          type: 'vehicle_viewed',
          label: '🚘 Múltiples vehículos vistos',
          importance: 'medium',
          count: 8,
          lastOccurred: ago(1),
          pointsContributed: 4,
        },
      ],
      interestedVehicles: [
        {
          vehicleId: 'v-101',
          title: 'Toyota RAV4 2023',
          price: 2800000,
          viewCount: 5,
          totalViewTime: 420,
          contacted: true,
          favorited: true,
          lastViewed: ago(2),
          interestScore: 90,
        },
        {
          vehicleId: 'v-102',
          title: 'Honda CR-V 2022',
          price: 2500000,
          viewCount: 3,
          totalViewTime: 180,
          contacted: true,
          favorited: false,
          lastViewed: ago(6),
          interestScore: 65,
        },
      ],
      preferredProfile: {
        preferredMakes: ['Toyota', 'Honda'],
        preferredModels: ['RAV4', 'CR-V'],
        yearRange: { min: 2021, max: 2024 },
        priceRange: { min: 2200000, max: 3000000 },
        preferredCondition: 'both',
      },
      conversionProbability: 0.78,
      estimatedDaysToPurchase: 3,
      recommendedAction: 'Contactar inmediatamente — alto interés de compra',
      firstSeen: ago(72),
      lastSeen: ago(1),
      totalSessions: 6,
      totalPageViews: 34,
      totalTimeSpentMinutes: 45,
    },
    {
      visitorId: 'lead-002',
      isAnonymous: true,
      totalScore: 58,
      level: 'warm',
      breakdown: {
        engagementScore: 18,
        intentScore: 22,
        contactScore: 8,
        financialReadinessScore: 10,
      },
      device: {
        deviceType: 'desktop',
        os: 'Windows',
        browser: 'Chrome',
        fingerprint: 'fp-002',
        screenWidth: 1920,
        screenHeight: 1080,
        language: 'es-DO',
        timezone: 'America/Santo_Domingo',
        isTouch: false,
      },
      signals: [
        {
          type: 'dealer_call_clicked',
          label: '📞 Llamó al dealer',
          importance: 'high',
          count: 1,
          lastOccurred: ago(12),
          pointsContributed: 10,
        },
        {
          type: 'search_performed',
          label: '🔍 Búsquedas activas',
          importance: 'medium',
          count: 7,
          lastOccurred: ago(8),
          pointsContributed: 5,
        },
        {
          type: 'vehicle_compared',
          label: '⚖️ Comparó vehículos',
          importance: 'medium',
          count: 2,
          lastOccurred: ago(10),
          pointsContributed: 6,
        },
        {
          type: 'financing_calculator_used',
          label: '🏦 Calculadora de financiamiento',
          importance: 'medium',
          count: 1,
          lastOccurred: ago(14),
          pointsContributed: 6,
        },
      ],
      interestedVehicles: [
        {
          vehicleId: 'v-201',
          title: 'Hyundai Tucson 2023',
          price: 2200000,
          viewCount: 4,
          totalViewTime: 300,
          contacted: true,
          favorited: true,
          lastViewed: ago(8),
          interestScore: 75,
        },
        {
          vehicleId: 'v-202',
          title: 'Kia Sportage 2022',
          price: 1900000,
          viewCount: 3,
          totalViewTime: 200,
          contacted: false,
          favorited: true,
          lastViewed: ago(12),
          interestScore: 55,
        },
        {
          vehicleId: 'v-203',
          title: 'Nissan Rogue 2023',
          price: 2400000,
          viewCount: 2,
          totalViewTime: 120,
          contacted: false,
          favorited: false,
          lastViewed: ago(18),
          interestScore: 30,
        },
      ],
      preferredProfile: {
        preferredMakes: ['Hyundai', 'Kia', 'Nissan'],
        preferredModels: ['Tucson', 'Sportage', 'Rogue'],
        yearRange: { min: 2021, max: 2024 },
        priceRange: { min: 1800000, max: 2500000 },
        preferredCondition: 'used',
      },
      conversionProbability: 0.55,
      estimatedDaysToPurchase: 12,
      recommendedAction: 'Enviar oferta personalizada por WhatsApp',
      firstSeen: ago(120),
      lastSeen: ago(8),
      totalSessions: 4,
      totalPageViews: 22,
      totalTimeSpentMinutes: 28,
    },
    {
      visitorId: 'lead-003',
      isAnonymous: false,
      userName: 'Carlos Pérez',
      email: 'carlos.perez@gmail.com',
      totalScore: 45,
      level: 'warm',
      breakdown: {
        engagementScore: 15,
        intentScore: 18,
        contactScore: 0,
        financialReadinessScore: 12,
      },
      device: {
        deviceType: 'mobile',
        os: 'Android',
        browser: 'Chrome',
        fingerprint: 'fp-003',
        screenWidth: 412,
        screenHeight: 915,
        language: 'es-DO',
        timezone: 'America/Santo_Domingo',
        isTouch: true,
        connectionType: '4g',
      },
      signals: [
        {
          type: 'financing_calculator_used',
          label: '🏦 Calculadora de financiamiento',
          importance: 'medium',
          count: 3,
          lastOccurred: ago(24),
          pointsContributed: 6,
        },
        {
          type: 'vehicle_viewed',
          label: '🚘 Múltiples vehículos vistos',
          importance: 'medium',
          count: 6,
          lastOccurred: ago(20),
          pointsContributed: 4,
        },
        {
          type: 'search_filter_applied',
          label: '🔧 Filtros aplicados',
          importance: 'medium',
          count: 4,
          lastOccurred: ago(22),
          pointsContributed: 8,
        },
        {
          type: 'favorite_added',
          label: '❤️ Favoritos guardados',
          importance: 'low',
          count: 3,
          lastOccurred: ago(24),
          pointsContributed: 4.5,
        },
      ],
      interestedVehicles: [
        {
          vehicleId: 'v-301',
          title: 'Toyota Corolla 2021',
          price: 1400000,
          viewCount: 4,
          totalViewTime: 240,
          contacted: false,
          favorited: true,
          lastViewed: ago(20),
          interestScore: 55,
        },
        {
          vehicleId: 'v-302',
          title: 'Honda Civic 2020',
          price: 1200000,
          viewCount: 2,
          totalViewTime: 100,
          contacted: false,
          favorited: true,
          lastViewed: ago(28),
          interestScore: 35,
        },
      ],
      preferredProfile: {
        preferredMakes: ['Toyota', 'Honda'],
        preferredModels: ['Corolla', 'Civic'],
        yearRange: { min: 2019, max: 2022 },
        priceRange: { min: 1000000, max: 1600000 },
        preferredCondition: 'used',
      },
      conversionProbability: 0.38,
      estimatedDaysToPurchase: 18,
      recommendedAction: 'Enviar información de vehículos similares',
      firstSeen: ago(168),
      lastSeen: ago(20),
      totalSessions: 3,
      totalPageViews: 16,
      totalTimeSpentMinutes: 18,
    },
    {
      visitorId: 'lead-004',
      isAnonymous: true,
      totalScore: 22,
      level: 'cold',
      breakdown: {
        engagementScore: 10,
        intentScore: 8,
        contactScore: 0,
        financialReadinessScore: 4,
      },
      device: {
        deviceType: 'tablet',
        os: 'iOS',
        browser: 'Safari',
        fingerprint: 'fp-004',
        screenWidth: 820,
        screenHeight: 1180,
        language: 'es',
        timezone: 'America/Santo_Domingo',
        isTouch: true,
      },
      signals: [
        {
          type: 'vehicle_viewed',
          label: '🚘 Vehículos vistos',
          importance: 'low',
          count: 3,
          lastOccurred: ago(48),
          pointsContributed: 2,
        },
        {
          type: 'search_performed',
          label: '🔍 Búsquedas realizadas',
          importance: 'low',
          count: 2,
          lastOccurred: ago(50),
          pointsContributed: 2,
        },
      ],
      interestedVehicles: [
        {
          vehicleId: 'v-401',
          title: 'BMW X3 2022',
          price: 3500000,
          viewCount: 2,
          totalViewTime: 90,
          contacted: false,
          favorited: false,
          lastViewed: ago(48),
          interestScore: 25,
        },
      ],
      preferredProfile: {
        preferredMakes: ['BMW'],
        preferredModels: ['X3'],
        yearRange: { min: 2021, max: 2024 },
        priceRange: { min: 3000000, max: 4000000 },
        preferredCondition: 'both',
      },
      conversionProbability: 0.18,
      estimatedDaysToPurchase: 45,
      recommendedAction: 'Incluir en campañas de remarketing',
      firstSeen: ago(240),
      lastSeen: ago(48),
      totalSessions: 2,
      totalPageViews: 8,
      totalTimeSpentMinutes: 6,
    },
    {
      visitorId: 'lead-005',
      isAnonymous: false,
      userName: 'Ana Rodríguez',
      email: 'ana.rod@hotmail.com',
      phone: '+1 829-555-0202',
      totalScore: 91,
      level: 'hot',
      breakdown: {
        engagementScore: 25,
        intentScore: 24,
        contactScore: 28,
        financialReadinessScore: 14,
      },
      device: {
        deviceType: 'mobile',
        os: 'iOS',
        browser: 'Safari',
        fingerprint: 'fp-005',
        screenWidth: 430,
        screenHeight: 932,
        language: 'es-DO',
        timezone: 'America/Santo_Domingo',
        isTouch: true,
      },
      signals: [
        {
          type: 'test_drive_requested',
          label: '🚗 Test drive solicitado',
          importance: 'high',
          count: 2,
          lastOccurred: ago(3),
          pointsContributed: 12,
        },
        {
          type: 'price_negotiation_started',
          label: '💰 Negociación iniciada',
          importance: 'high',
          count: 1,
          lastOccurred: ago(5),
          pointsContributed: 10,
        },
        {
          type: 'dealer_whatsapp_clicked',
          label: '💬 WhatsApp al dealer',
          importance: 'high',
          count: 4,
          lastOccurred: ago(2),
          pointsContributed: 9,
        },
        {
          type: 'financing_calculator_used',
          label: '🏦 Calculadora de financiamiento',
          importance: 'medium',
          count: 3,
          lastOccurred: ago(8),
          pointsContributed: 6,
        },
        {
          type: 'payment_page_visited',
          label: '💳 Visitó página de pago',
          importance: 'high',
          count: 1,
          lastOccurred: ago(4),
          pointsContributed: 8,
        },
      ],
      interestedVehicles: [
        {
          vehicleId: 'v-501',
          title: 'Mercedes-Benz GLC 2023',
          price: 4200000,
          viewCount: 8,
          totalViewTime: 720,
          contacted: true,
          favorited: true,
          lastViewed: ago(2),
          interestScore: 95,
        },
        {
          vehicleId: 'v-502',
          title: 'BMW X3 2023',
          price: 3800000,
          viewCount: 4,
          totalViewTime: 300,
          contacted: true,
          favorited: true,
          lastViewed: ago(8),
          interestScore: 70,
        },
      ],
      preferredProfile: {
        preferredMakes: ['Mercedes-Benz', 'BMW'],
        preferredModels: ['GLC', 'X3'],
        yearRange: { min: 2022, max: 2024 },
        priceRange: { min: 3500000, max: 5000000 },
        preferredCondition: 'both',
      },
      conversionProbability: 0.88,
      estimatedDaysToPurchase: 2,
      recommendedAction: 'Contactar inmediatamente — alto interés de compra',
      firstSeen: ago(96),
      lastSeen: ago(2),
      totalSessions: 9,
      totalPageViews: 52,
      totalTimeSpentMinutes: 68,
    },
  ];
}
