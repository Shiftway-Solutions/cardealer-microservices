/**
 * SiteConfigProvider
 *
 * Loads platform-wide configuration from ConfigurationService
 * and makes it available to all components via React Context.
 *
 * Falls back to sensible defaults when the API is unreachable
 * or the user is not authenticated (public pages).
 */

'use client';

import * as React from 'react';
import { createContext, useContext, useState, useEffect, useCallback, useMemo } from 'react';
import { apiClient } from '@/lib/api-client';

// =============================================================================
// TYPES
// =============================================================================

export interface SiteConfig {
  // Identity
  siteName: string;
  siteUrl: string;
  siteDescription: string;
  // Emails
  contactEmail: string;
  supportEmail: string;
  noreplyEmail: string;
  legalEmail: string;
  privacyEmail: string;
  // Phone & WhatsApp
  supportPhone: string;
  whatsappNumber: string;
  whatsappUrl: string;
  phoneHref: string;
  // Location
  address: string;
  businessHours: string;
  // Social
  socialFacebook: string;
  socialInstagram: string;
  socialTwitter: string;
  socialYoutube: string;
}

interface SiteConfigContextValue {
  config: SiteConfig;
  isLoaded: boolean;
  isLoading: boolean;
  error: string | null;
  refresh: () => Promise<void>;
}

// =============================================================================
// DEFAULTS — match the actual hardcoded values across the platform
// =============================================================================

const DEFAULT_CONFIG: SiteConfig = {
  siteName: 'OKLA',
  siteUrl: 'https://okla.com.do',
  siteDescription:
    'El marketplace de vehículos #1 de República Dominicana. Compra y vende con confianza.',
  contactEmail: 'info@okla.com.do',
  supportEmail: 'soporte@okla.com.do',
  noreplyEmail: 'noreply@okla.com.do',
  legalEmail: 'legal@okla.com.do',
  privacyEmail: 'privacidad@okla.com.do',
  supportPhone: '+1 (809) 200-6552',
  whatsappNumber: '18092006552',
  whatsappUrl: 'https://wa.me/18092006552',
  phoneHref: 'tel:+18092006552',
  address: 'Av. Winston Churchill #1099, Torre Acrópolis, Piso 10, Santo Domingo, RD',
  businessHours: 'Lunes a Viernes: 8:00 AM - 6:00 PM | Sábados: 9:00 AM - 1:00 PM',
  socialFacebook: 'https://facebook.com/okla',
  socialInstagram: 'https://instagram.com/okla',
  socialTwitter: 'https://twitter.com/okla',
  socialYoutube: 'https://youtube.com/okla',
};

// =============================================================================
// CONTEXT
// =============================================================================

const SiteConfigContext = createContext<SiteConfigContextValue>({
  config: DEFAULT_CONFIG,
  isLoaded: false,
  isLoading: false,
  error: null,
  refresh: async () => {},
});

// =============================================================================
// HELPER: Build SiteConfig from raw API config map
// =============================================================================

function buildConfig(raw: Record<string, string>): SiteConfig {
  const get = (key: string, fallback: string) => raw[key] || fallback;
  const whatsapp = get('general.whatsapp_number', DEFAULT_CONFIG.whatsappNumber);
  const phone = get('general.support_phone', DEFAULT_CONFIG.supportPhone);
  // Extract digits for href
  const phoneDigits = phone.replace(/[^0-9+]/g, '');

  return {
    siteName: get('general.site_name', DEFAULT_CONFIG.siteName),
    siteUrl: get('general.site_url', DEFAULT_CONFIG.siteUrl),
    siteDescription: get('general.site_description', DEFAULT_CONFIG.siteDescription),
    contactEmail: get('general.contact_email', DEFAULT_CONFIG.contactEmail),
    supportEmail: get('general.support_email', DEFAULT_CONFIG.supportEmail),
    noreplyEmail: get('general.noreply_email', DEFAULT_CONFIG.noreplyEmail),
    legalEmail: get('general.legal_email', DEFAULT_CONFIG.legalEmail),
    privacyEmail: get('general.privacy_email', DEFAULT_CONFIG.privacyEmail),
    supportPhone: phone,
    whatsappNumber: whatsapp,
    whatsappUrl: `https://wa.me/${whatsapp}`,
    phoneHref: `tel:${phoneDigits}`,
    address: get('general.address', DEFAULT_CONFIG.address),
    businessHours: get('general.business_hours', DEFAULT_CONFIG.businessHours),
    socialFacebook: get('general.social_facebook', DEFAULT_CONFIG.socialFacebook),
    socialInstagram: get('general.social_instagram', DEFAULT_CONFIG.socialInstagram),
    socialTwitter: get('general.social_twitter', DEFAULT_CONFIG.socialTwitter),
    socialYoutube: get('general.social_youtube', DEFAULT_CONFIG.socialYoutube),
  };
}

// =============================================================================
// CACHE — persist in sessionStorage to avoid re-fetching on navigation
// =============================================================================

const CACHE_KEY = 'okla_site_config';
const CACHE_TTL_MS = 5 * 60 * 1000; // 5 minutes

function getCached(): SiteConfig | null {
  if (typeof window === 'undefined') return null;
  try {
    const raw = sessionStorage.getItem(CACHE_KEY);
    if (!raw) return null;
    const { config, timestamp } = JSON.parse(raw);
    if (Date.now() - timestamp > CACHE_TTL_MS) {
      sessionStorage.removeItem(CACHE_KEY);
      return null;
    }
    return config as SiteConfig;
  } catch {
    return null;
  }
}

function setCache(config: SiteConfig) {
  if (typeof window === 'undefined') return;
  try {
    sessionStorage.setItem(CACHE_KEY, JSON.stringify({ config, timestamp: Date.now() }));
  } catch {
    // sessionStorage full or unavailable — ignore
  }
}

// =============================================================================
// PROVIDER
// =============================================================================

export function SiteConfigProvider({ children }: { children: React.ReactNode }) {
  const [config, setConfig] = useState<SiteConfig>(DEFAULT_CONFIG);
  const [isLoaded, setIsLoaded] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchConfig = useCallback(async () => {
    // Try cache first
    const cached = getCached();
    if (cached) {
      setConfig(cached);
      setIsLoaded(true);
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const environment =
        process.env.NEXT_PUBLIC_APP_ENV ??
        (process.env.NODE_ENV === 'production' ? 'Production' : 'Development');

      // Use _silentAuth so a 401 (unauthenticated pages) does NOT trigger
      // the global refresh-token interceptor — avoids 401→429 rate-limit cascade.
      // Config is cast through `unknown` to attach the custom flag without
      // breaking TypeScript's generic resolution on `apiClient.get<T>()`.
      const silentConfig = { params: { environment }, _silentAuth: true } as unknown as Parameters<
        typeof apiClient.get
      >[1];
      const response = await apiClient.get<Array<{ key: string; value: string }>>(
        '/api/configurations/category/general',
        silentConfig
      );

      const raw: Record<string, string> = {};
      response.data.forEach(item => {
        raw[item.key] = item.value;
      });

      const built = buildConfig(raw);
      setConfig(built);
      setCache(built);
      setIsLoaded(true);
    } catch {
      // API unreachable, 401, or 404 (ConfigurationService not deployed) —
      // use defaults silently; public pages work fine without dynamic config
      setConfig(DEFAULT_CONFIG);
      setIsLoaded(true);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchConfig();
  }, [fetchConfig]);

  const value = useMemo(
    () => ({ config, isLoaded, isLoading, error, refresh: fetchConfig }),
    [config, isLoaded, isLoading, error, fetchConfig]
  );

  return <SiteConfigContext.Provider value={value}>{children}</SiteConfigContext.Provider>;
}

// =============================================================================
// HOOK
// =============================================================================

export function useSiteConfig(): SiteConfig {
  const { config } = useContext(SiteConfigContext);
  return config;
}

export function useSiteConfigContext(): SiteConfigContextValue {
  return useContext(SiteConfigContext);
}
