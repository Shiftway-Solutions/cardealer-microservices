/**
 * Device Fingerprinting & Detection
 *
 * Generates a unique device fingerprint from browser characteristics.
 * Works for both logged-in and anonymous users.
 * GDPR-friendly: no cookies, uses canvas + WebGL + system properties.
 */

import type { DeviceInfo, DeviceType, OSFamily, BrowserFamily } from '@/types/analytics';

// =============================================================================
// DEVICE TYPE DETECTION
// =============================================================================

export function detectDeviceType(): DeviceType {
  if (typeof navigator === 'undefined') return 'unknown';

  const ua = navigator.userAgent.toLowerCase();

  if (/bot|crawler|spider|crawling/i.test(ua)) return 'bot';

  // Check for tablets first (before mobile, since some tablets match mobile patterns)
  if (/ipad/i.test(ua)) return 'tablet';
  if (/android/i.test(ua) && !/mobile/i.test(ua)) return 'tablet';
  if (/tablet/i.test(ua)) return 'tablet';

  // Mobile
  if (/iphone|ipod/i.test(ua)) return 'mobile';
  if (/android.*mobile/i.test(ua)) return 'mobile';
  if (/mobile|phone/i.test(ua)) return 'mobile';

  return 'desktop';
}

// =============================================================================
// OS DETECTION
// =============================================================================

export function detectOS(): { os: OSFamily; version?: string } {
  if (typeof navigator === 'undefined') return { os: 'Unknown' };

  const ua = navigator.userAgent;

  if (/Windows NT (\d+\.\d+)/.test(ua)) {
    const match = ua.match(/Windows NT (\d+\.\d+)/);
    const version = match?.[1];
    const versionMap: Record<string, string> = {
      '10.0': '10/11',
      '6.3': '8.1',
      '6.2': '8',
      '6.1': '7',
    };
    return { os: 'Windows', version: versionMap[version || ''] || version };
  }

  if (/Mac OS X (\d+[._]\d+)/.test(ua)) {
    const match = ua.match(/Mac OS X (\d+[._]\d+[._]?\d*)/);
    return { os: 'macOS', version: match?.[1]?.replace(/_/g, '.') };
  }

  if (/iPhone OS (\d+_\d+)/.test(ua) || /iPad.*OS (\d+_\d+)/.test(ua)) {
    const match = ua.match(/OS (\d+_\d+)/);
    return { os: 'iOS', version: match?.[1]?.replace(/_/g, '.') };
  }

  if (/Android (\d+\.?\d*)/.test(ua)) {
    const match = ua.match(/Android (\d+\.?\d*)/);
    return { os: 'Android', version: match?.[1] };
  }

  if (/CrOS/.test(ua)) return { os: 'ChromeOS' };
  if (/Linux/.test(ua)) return { os: 'Linux' };

  return { os: 'Unknown' };
}

// =============================================================================
// BROWSER DETECTION
// =============================================================================

export function detectBrowser(): { browser: BrowserFamily; version?: string } {
  if (typeof navigator === 'undefined') return { browser: 'Unknown' };

  const ua = navigator.userAgent;

  // Order matters: check more specific browsers first
  if (/SamsungBrowser\/(\d+)/.test(ua)) {
    const match = ua.match(/SamsungBrowser\/(\d+\.?\d*)/);
    return { browser: 'Samsung Internet', version: match?.[1] };
  }

  if (/UCBrowser\/(\d+)/.test(ua)) {
    const match = ua.match(/UCBrowser\/(\d+\.?\d*)/);
    return { browser: 'UC Browser', version: match?.[1] };
  }

  if (/OPR\/(\d+)/.test(ua) || /Opera\/(\d+)/.test(ua)) {
    const match = ua.match(/OPR\/(\d+\.?\d*)/) || ua.match(/Opera\/(\d+\.?\d*)/);
    return { browser: 'Opera', version: match?.[1] };
  }

  if (/Edg\/(\d+)/.test(ua)) {
    const match = ua.match(/Edg\/(\d+\.?\d*)/);
    return { browser: 'Edge', version: match?.[1] };
  }

  // Firefox before Chrome (some Firefox UAs contain "like Gecko")
  if (/Firefox\/(\d+)/.test(ua)) {
    const match = ua.match(/Firefox\/(\d+\.?\d*)/);
    return { browser: 'Firefox', version: match?.[1] };
  }

  // Safari before Chrome (Chrome contains "Safari" in UA)
  if (/Safari\//.test(ua) && !/Chrome\//.test(ua)) {
    const match = ua.match(/Version\/(\d+\.?\d*)/);
    return { browser: 'Safari', version: match?.[1] };
  }

  if (/Chrome\/(\d+)/.test(ua)) {
    const match = ua.match(/Chrome\/(\d+\.?\d*)/);
    return { browser: 'Chrome', version: match?.[1] };
  }

  return { browser: 'Unknown' };
}

// =============================================================================
// CANVAS FINGERPRINT
// =============================================================================

function getCanvasFingerprint(): string {
  if (typeof document === 'undefined') return 'no-canvas';

  try {
    const canvas = document.createElement('canvas');
    canvas.width = 200;
    canvas.height = 50;
    const ctx = canvas.getContext('2d');
    if (!ctx) return 'no-ctx';

    // Draw a specific text with specific styling
    ctx.textBaseline = 'top';
    ctx.font = '14px Arial';
    ctx.fillStyle = '#f60';
    ctx.fillRect(125, 1, 62, 20);
    ctx.fillStyle = '#069';
    ctx.fillText('OKLA.do 🚗', 2, 15);
    ctx.fillStyle = 'rgba(102, 204, 0, 0.7)';
    ctx.fillText('OKLA.do 🚗', 4, 17);

    return canvas.toDataURL();
  } catch {
    return 'canvas-error';
  }
}

// =============================================================================
// WEBGL FINGERPRINT
// =============================================================================

function getWebGLFingerprint(): string {
  if (typeof document === 'undefined') return 'no-webgl';

  try {
    const canvas = document.createElement('canvas');
    const gl = canvas.getContext('webgl') || canvas.getContext('experimental-webgl');
    if (!gl || !(gl instanceof WebGLRenderingContext)) return 'no-webgl';

    const debugInfo = gl.getExtension('WEBGL_debug_renderer_info');
    const vendor = debugInfo ? gl.getParameter(debugInfo.UNMASKED_VENDOR_WEBGL) : 'unknown';
    const renderer = debugInfo ? gl.getParameter(debugInfo.UNMASKED_RENDERER_WEBGL) : 'unknown';

    return `${vendor}~${renderer}`;
  } catch {
    return 'webgl-error';
  }
}

// =============================================================================
// HASH FUNCTION (FNV-1a)
// =============================================================================

function fnv1aHash(str: string): string {
  let hash = 0x811c9dc5; // FNV offset basis
  for (let i = 0; i < str.length; i++) {
    hash ^= str.charCodeAt(i);
    hash = (hash * 0x01000193) >>> 0; // FNV prime, keep as uint32
  }
  return hash.toString(36); // Base-36 for compact representation
}

// =============================================================================
// CONNECTION INFO
// =============================================================================

function getConnectionType(): string | undefined {
  if (typeof navigator === 'undefined') return undefined;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const conn =
    (navigator as any).connection ||
    (navigator as any).mozConnection ||
    (navigator as any).webkitConnection;
  return conn?.effectiveType || conn?.type || undefined;
}

// =============================================================================
// GENERATE FULL DEVICE FINGERPRINT
// =============================================================================

export function generateDeviceFingerprint(): string {
  const components = [
    getCanvasFingerprint(),
    getWebGLFingerprint(),
    typeof screen !== 'undefined'
      ? `${screen.width}x${screen.height}x${screen.colorDepth}`
      : 'no-screen',
    typeof navigator !== 'undefined' ? navigator.language : 'no-lang',
    typeof navigator !== 'undefined' ? String(navigator.hardwareConcurrency || 0) : '0',
    typeof navigator !== 'undefined'
      ? String((navigator as { deviceMemory?: number }).deviceMemory || 0)
      : '0',
    Intl?.DateTimeFormat?.()?.resolvedOptions?.()?.timeZone || 'no-tz',
    typeof navigator !== 'undefined' ? String(navigator.maxTouchPoints || 0) : '0',
  ];

  return fnv1aHash(components.join('|'));
}

// =============================================================================
// BUILD COMPLETE DEVICE INFO
// =============================================================================

export function getDeviceInfo(): DeviceInfo {
  const { os, version: osVersion } = detectOS();
  const { browser, version: browserVersion } = detectBrowser();

  return {
    deviceType: detectDeviceType(),
    os,
    osVersion,
    browser,
    browserVersion,
    screenWidth: typeof screen !== 'undefined' ? screen.width : 0,
    screenHeight: typeof screen !== 'undefined' ? screen.height : 0,
    language: typeof navigator !== 'undefined' ? navigator.language : 'unknown',
    timezone: Intl?.DateTimeFormat?.()?.resolvedOptions?.()?.timeZone || 'unknown',
    fingerprint: generateDeviceFingerprint(),
    isTouch: typeof navigator !== 'undefined' ? navigator.maxTouchPoints > 0 : false,
    connectionType: getConnectionType(),
  };
}

// =============================================================================
// SESSION ID GENERATION
// =============================================================================

const SESSION_KEY = 'okla_session_id';
const ANON_KEY = 'okla_anonymous_id';

/** Generate a random ID */
function generateId(): string {
  if (typeof crypto !== 'undefined' && crypto.randomUUID) {
    return crypto.randomUUID();
  }
  return `${Date.now().toString(36)}-${Math.random().toString(36).substring(2, 10)}`;
}

/** Get or create a session ID (persists for browser session via sessionStorage) */
export function getSessionId(): string {
  if (typeof sessionStorage === 'undefined') return generateId();

  let id = sessionStorage.getItem(SESSION_KEY);
  if (!id) {
    id = generateId();
    sessionStorage.setItem(SESSION_KEY, id);
  }
  return id;
}

/** Get or create an anonymous ID (persists across sessions via localStorage) */
export function getAnonymousId(): string {
  if (typeof localStorage === 'undefined') return generateId();

  let id = localStorage.getItem(ANON_KEY);
  if (!id) {
    id = generateId();
    localStorage.setItem(ANON_KEY, id);
  }
  return id;
}

/** Extract UTM params from current URL */
export function getUtmParams(): Record<string, string> | undefined {
  if (typeof window === 'undefined') return undefined;

  const params = new URLSearchParams(window.location.search);
  const utm: Record<string, string> = {};

  for (const key of ['utm_source', 'utm_medium', 'utm_campaign', 'utm_term', 'utm_content']) {
    const value = params.get(key);
    if (value) utm[key.replace('utm_', '')] = value;
  }

  return Object.keys(utm).length > 0 ? utm : undefined;
}
