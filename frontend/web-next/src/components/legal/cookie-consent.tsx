'use client';

import * as React from 'react';
import { useState, useEffect, useCallback } from 'react';
import { Button } from '@/components/ui/button';
import { Switch } from '@/components/ui/switch';
import { Cookie, Settings, Shield } from 'lucide-react';

const CONSENT_KEY = 'okla-cookie-consent';

interface CookiePreferences {
  essential: boolean; // Always true
  preferences: boolean;
  analytics: boolean;
  marketing: boolean;
  consentDate: string;
  version: string;
}

const DEFAULT_PREFERENCES: CookiePreferences = {
  essential: true,
  preferences: false,
  analytics: false,
  marketing: false,
  consentDate: '',
  version: '1.0',
};

function getStoredConsent(): CookiePreferences | null {
  if (typeof window === 'undefined') return null;
  try {
    const stored = localStorage.getItem(CONSENT_KEY);
    if (stored) return JSON.parse(stored);
  } catch {
    // ignore
  }
  return null;
}

function saveConsent(prefs: CookiePreferences) {
  if (typeof window === 'undefined') return;
  const data = { ...prefs, consentDate: new Date().toISOString() };
  localStorage.setItem(CONSENT_KEY, JSON.stringify(data));
}

export function CookieConsentBanner() {
  const [visible, setVisible] = useState(false);
  const [showConfig, setShowConfig] = useState(false);
  const [preferences, setPreferences] = useState<CookiePreferences>(DEFAULT_PREFERENCES);

  useEffect(() => {
    const stored = getStoredConsent();
    if (!stored) {
      setVisible(true);
    }
  }, []);

  const handleAcceptAll = useCallback(() => {
    const allAccepted: CookiePreferences = {
      essential: true,
      preferences: true,
      analytics: true,
      marketing: true,
      consentDate: '',
      version: '1.0',
    };
    saveConsent(allAccepted);
    setVisible(false);
  }, []);

  const handleRejectNonEssential = useCallback(() => {
    const essentialOnly: CookiePreferences = {
      essential: true,
      preferences: false,
      analytics: false,
      marketing: false,
      consentDate: '',
      version: '1.0',
    };
    saveConsent(essentialOnly);
    setVisible(false);
  }, []);

  const handleSavePreferences = useCallback(() => {
    saveConsent(preferences);
    setVisible(false);
    setShowConfig(false);
  }, [preferences]);

  if (!visible) return null;

  return (
    <div
      className="pointer-events-none fixed inset-x-0 bottom-0 z-30 p-3 pb-[max(0.75rem,env(safe-area-inset-bottom))] sm:p-4"
      role="dialog"
      aria-label="Configuración de cookies"
    >
      <div className="border-border bg-card pointer-events-auto mx-auto max-h-[78vh] max-w-4xl overflow-y-auto rounded-xl border shadow-2xl">
        {/* Main banner */}
        <div className="p-5 sm:p-6">
          <div className="flex items-start gap-3">
            <div className="bg-primary/10 rounded-lg p-2">
              <Cookie className="text-primary h-5 w-5" />
            </div>
            <div className="flex-1 space-y-2">
              <h2 className="text-foreground text-base font-semibold">Configuración de Cookies</h2>
              <p className="text-muted-foreground text-sm leading-relaxed">
                Utilizamos cookies para mejorar tu experiencia en OKLA. De acuerdo con la{' '}
                <strong>Ley 172-13</strong> de Protección de Datos Personales de República
                Dominicana, necesitamos tu consentimiento para usar cookies no esenciales. Puedes
                configurar tus preferencias o aceptar/rechazar a continuación.
              </p>
            </div>
          </div>

          {/* Cookie categories (expandable) */}
          {showConfig && (
            <div className="border-border bg-muted/30 mt-5 space-y-3 rounded-lg border p-4">
              {/* Essential */}
              <div className="flex items-center justify-between gap-4">
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <Shield className="text-primary h-4 w-4" />
                    <span className="text-foreground text-sm font-medium">Esenciales</span>
                    <span className="bg-primary/10 text-primary rounded px-1.5 py-0.5 text-[10px] font-medium">
                      Siempre activas
                    </span>
                  </div>
                  <p className="text-muted-foreground mt-1 text-xs">
                    Necesarias para el funcionamiento del sitio. Incluyen autenticación, seguridad y
                    preferencias básicas.
                  </p>
                </div>
                <Switch checked={true} disabled aria-label="Cookies esenciales siempre activas" />
              </div>

              {/* Preferences */}
              <div className="border-border border-t pt-3">
                <div className="flex items-center justify-between gap-4">
                  <div className="flex-1">
                    <span className="text-foreground text-sm font-medium">Preferencias</span>
                    <p className="text-muted-foreground mt-1 text-xs">
                      Permiten recordar tus configuraciones como idioma, región y personalización de
                      la interfaz.
                    </p>
                  </div>
                  <Switch
                    checked={preferences.preferences}
                    onCheckedChange={checked =>
                      setPreferences(prev => ({ ...prev, preferences: !!checked }))
                    }
                    aria-label="Cookies de preferencias"
                  />
                </div>
              </div>

              {/* Analytics */}
              <div className="border-border border-t pt-3">
                <div className="flex items-center justify-between gap-4">
                  <div className="flex-1">
                    <span className="text-foreground text-sm font-medium">Analíticas</span>
                    <p className="text-muted-foreground mt-1 text-xs">
                      Nos ayudan a entender cómo usas OKLA para mejorar el servicio. Incluyen Google
                      Analytics y herramientas similares.
                    </p>
                  </div>
                  <Switch
                    checked={preferences.analytics}
                    onCheckedChange={checked =>
                      setPreferences(prev => ({ ...prev, analytics: !!checked }))
                    }
                    aria-label="Cookies analíticas"
                  />
                </div>
              </div>

              {/* Marketing */}
              <div className="border-border border-t pt-3">
                <div className="flex items-center justify-between gap-4">
                  <div className="flex-1">
                    <span className="text-foreground text-sm font-medium">Marketing</span>
                    <p className="text-muted-foreground mt-1 text-xs">
                      Utilizadas para mostrarte publicidad relevante y medir la efectividad de
                      campañas publicitarias.
                    </p>
                  </div>
                  <Switch
                    checked={preferences.marketing}
                    onCheckedChange={checked =>
                      setPreferences(prev => ({ ...prev, marketing: !!checked }))
                    }
                    aria-label="Cookies de marketing"
                  />
                </div>
              </div>
            </div>
          )}

          {/* Action buttons */}
          <div className="mt-5 flex flex-col gap-2 sm:flex-row sm:justify-end">
            {!showConfig ? (
              <>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setShowConfig(true)}
                  className="gap-1.5 sm:order-1"
                >
                  <Settings className="h-3.5 w-3.5" />
                  Configurar
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleRejectNonEssential}
                  className="sm:order-2"
                >
                  Rechazar no esenciales
                </Button>
                <Button size="sm" onClick={handleAcceptAll} className="sm:order-3">
                  Aceptar todas
                </Button>
              </>
            ) : (
              <>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setShowConfig(false)}
                  className="sm:order-1"
                >
                  Volver
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleRejectNonEssential}
                  className="sm:order-2"
                >
                  Rechazar no esenciales
                </Button>
                <Button size="sm" onClick={handleSavePreferences} className="sm:order-3">
                  Guardar preferencias
                </Button>
              </>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

/**
 * Small floating button that appears in the footer area after the user
 * has already given/denied consent so they can re-open the config panel.
 */
export function CookieSettingsButton() {
  const [hasConsent, setHasConsent] = useState(false);
  const [showBanner, setShowBanner] = useState(false);

  useEffect(() => {
    const stored = getStoredConsent();
    if (stored) setHasConsent(true);
  }, []);

  const handleReopen = () => {
    // Remove stored consent to re-show the full banner
    localStorage.removeItem(CONSENT_KEY);
    setHasConsent(false);
    setShowBanner(true);
  };

  if (showBanner) {
    return (
      <ReopenedBanner
        onClose={() => {
          setShowBanner(false);
          setHasConsent(true);
        }}
      />
    );
  }

  if (!hasConsent) return null;

  return (
    <button
      onClick={handleReopen}
      className="border-border bg-card text-muted-foreground hover:bg-muted hover:text-foreground fixed bottom-[max(0.75rem,env(safe-area-inset-bottom))] left-3 z-20 flex items-center gap-1.5 rounded-full border px-3 py-1.5 text-xs font-medium shadow-lg transition-colors sm:left-4"
      aria-label="Configurar cookies"
    >
      <Cookie className="h-3.5 w-3.5" />
      Configurar cookies
    </button>
  );
}

/**
 * Re-opened cookie consent banner (when user clicks "Configurar cookies" button).
 */
function ReopenedBanner({ onClose }: { onClose: () => void }) {
  const showConfig = true;
  const [preferences, setPreferences] = useState<CookiePreferences>(() => {
    return getStoredConsent() || DEFAULT_PREFERENCES;
  });

  const handleAcceptAll = () => {
    saveConsent({
      essential: true,
      preferences: true,
      analytics: true,
      marketing: true,
      consentDate: '',
      version: '1.0',
    });
    onClose();
  };

  const handleRejectNonEssential = () => {
    saveConsent({
      essential: true,
      preferences: false,
      analytics: false,
      marketing: false,
      consentDate: '',
      version: '1.0',
    });
    onClose();
  };

  const handleSavePreferences = () => {
    saveConsent(preferences);
    onClose();
  };

  return (
    <div
      className="pointer-events-none fixed inset-x-0 bottom-0 z-30 p-3 pb-[max(0.75rem,env(safe-area-inset-bottom))] sm:p-4"
      role="dialog"
      aria-label="Configuración de cookies"
    >
      <div className="border-border bg-card pointer-events-auto mx-auto max-h-[78vh] max-w-4xl overflow-y-auto rounded-xl border shadow-2xl">
        <div className="p-5 sm:p-6">
          <div className="flex items-start gap-3">
            <div className="bg-primary/10 rounded-lg p-2">
              <Cookie className="text-primary h-5 w-5" />
            </div>
            <div className="flex-1 space-y-2">
              <h2 className="text-foreground text-base font-semibold">Configuración de Cookies</h2>
              <p className="text-muted-foreground text-sm leading-relaxed">
                Modifica tus preferencias de cookies. Conforme a la <strong>Ley 172-13</strong>.
              </p>
            </div>
          </div>

          {showConfig && (
            <div className="border-border bg-muted/30 mt-5 space-y-3 rounded-lg border p-4">
              <div className="flex items-center justify-between gap-4">
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <Shield className="text-primary h-4 w-4" />
                    <span className="text-foreground text-sm font-medium">Esenciales</span>
                    <span className="bg-primary/10 text-primary rounded px-1.5 py-0.5 text-[10px] font-medium">
                      Siempre activas
                    </span>
                  </div>
                  <p className="text-muted-foreground mt-1 text-xs">
                    Necesarias para el funcionamiento del sitio.
                  </p>
                </div>
                <Switch checked={true} disabled aria-label="Cookies esenciales siempre activas" />
              </div>

              <div className="border-border border-t pt-3">
                <div className="flex items-center justify-between gap-4">
                  <div className="flex-1">
                    <span className="text-foreground text-sm font-medium">Preferencias</span>
                    <p className="text-muted-foreground mt-1 text-xs">
                      Recordar configuraciones personales.
                    </p>
                  </div>
                  <Switch
                    checked={preferences.preferences}
                    onCheckedChange={checked =>
                      setPreferences(prev => ({ ...prev, preferences: !!checked }))
                    }
                    aria-label="Cookies de preferencias"
                  />
                </div>
              </div>

              <div className="border-border border-t pt-3">
                <div className="flex items-center justify-between gap-4">
                  <div className="flex-1">
                    <span className="text-foreground text-sm font-medium">Analíticas</span>
                    <p className="text-muted-foreground mt-1 text-xs">
                      Ayudan a mejorar el servicio.
                    </p>
                  </div>
                  <Switch
                    checked={preferences.analytics}
                    onCheckedChange={checked =>
                      setPreferences(prev => ({ ...prev, analytics: !!checked }))
                    }
                    aria-label="Cookies analíticas"
                  />
                </div>
              </div>

              <div className="border-border border-t pt-3">
                <div className="flex items-center justify-between gap-4">
                  <div className="flex-1">
                    <span className="text-foreground text-sm font-medium">Marketing</span>
                    <p className="text-muted-foreground mt-1 text-xs">
                      Publicidad y campañas relevantes.
                    </p>
                  </div>
                  <Switch
                    checked={preferences.marketing}
                    onCheckedChange={checked =>
                      setPreferences(prev => ({ ...prev, marketing: !!checked }))
                    }
                    aria-label="Cookies de marketing"
                  />
                </div>
              </div>
            </div>
          )}

          <div className="mt-5 flex flex-col gap-2 sm:flex-row sm:justify-end">
            <Button variant="outline" size="sm" onClick={handleRejectNonEssential}>
              Rechazar no esenciales
            </Button>
            <Button variant="outline" size="sm" onClick={handleSavePreferences}>
              Guardar preferencias
            </Button>
            <Button size="sm" onClick={handleAcceptAll}>
              Aceptar todas
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
