/**
 * Upgrade Banner Component
 *
 * Prominent banner shown to dealers/sellers on free plans to encourage upgrading.
 * Supports inline (in-page) and floating (fixed bottom) variants.
 * Dismissable with 7-day localStorage persistence.
 */

'use client';

import { useState } from 'react';
import Link from 'next/link';
import { X, Sparkles, Camera, BarChart3, Star, Zap, ArrowRight } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';

// =============================================================================
// TYPES
// =============================================================================

interface UpgradeBannerProps {
  /** Display variant */
  variant?: 'inline' | 'floating';
  /** User type for targeted messaging */
  userType?: 'dealer' | 'seller';
  /** Current plan name (for display) */
  currentPlan?: string;
  /** Override CTA link */
  upgradeUrl?: string;
  /** Additional CSS classes */
  className?: string;
}

// =============================================================================
// CONSTANTS
// =============================================================================

const DISMISS_KEY = 'okla_upgrade_banner_dismissed';
const DISMISS_DURATION_MS = 7 * 24 * 60 * 60 * 1000; // 7 days

interface MissingFeature {
  icon: React.ElementType;
  text: string;
}

const DEALER_MISSING_FEATURES: MissingFeature[] = [
  { icon: Camera, text: 'Hasta 40 fotos por vehículo' },
  { icon: BarChart3, text: 'Analíticas avanzadas y reportes' },
  { icon: Star, text: 'Publicaciones destacadas cada mes' },
  { icon: Zap, text: 'ChatBot IA para atender clientes 24/7' },
];

const SELLER_MISSING_FEATURES: MissingFeature[] = [
  { icon: Camera, text: 'Hasta 12 fotos por publicación' },
  { icon: Star, text: 'Badge de vendedor verificado' },
  { icon: BarChart3, text: 'Estadísticas detalladas de vistas' },
  { icon: Zap, text: 'Publicaciones permanentes sin expiración' },
];

// =============================================================================
// COMPONENT
// =============================================================================

export function UpgradeBanner({
  variant = 'inline',
  userType = 'dealer',
  currentPlan,
  upgradeUrl,
  className,
}: UpgradeBannerProps) {
  const [isDismissed, setIsDismissed] = useState(() => {
    if (typeof window === 'undefined') return true;
    try {
      const dismissed = localStorage.getItem(DISMISS_KEY);
      if (dismissed) {
        const dismissedAt = parseInt(dismissed, 10);
        if (Date.now() - dismissedAt < DISMISS_DURATION_MS) {
          return true;
        }
      }
      return false;
    } catch {
      return false;
    }
  });

  const handleDismiss = () => {
    setIsDismissed(true);
    try {
      localStorage.setItem(DISMISS_KEY, Date.now().toString());
    } catch {
      // Silently fail if localStorage is unavailable
    }
  };

  if (isDismissed) return null;

  const features = userType === 'dealer' ? DEALER_MISSING_FEATURES : SELLER_MISSING_FEATURES;
  const defaultUrl =
    userType === 'dealer'
      ? '/cuenta/upgrade?plan=visible&type=dealer'
      : '/cuenta/upgrade?plan=premium&type=seller';
  const href = upgradeUrl ?? defaultUrl;
  const planLabel = currentPlan ?? (userType === 'dealer' ? 'Libre' : 'Libre');

  if (variant === 'floating') {
    return (
      <div
        className={cn(
          'fixed inset-x-0 bottom-0 z-50 p-4',
          'animate-in slide-in-from-bottom duration-500',
          className
        )}
      >
        <div className="mx-auto max-w-4xl overflow-hidden rounded-2xl bg-gradient-to-r from-purple-600 via-indigo-600 to-blue-600 p-4 shadow-2xl sm:p-6">
          <div className="flex items-start justify-between gap-4">
            <div className="flex items-start gap-3 sm:items-center">
              <div className="shrink-0 rounded-full bg-white/20 p-2.5">
                <Sparkles className="h-5 w-5 animate-pulse text-yellow-300" />
              </div>
              <div className="text-white">
                <p className="text-sm font-bold sm:text-base">
                  ¡Actualiza tu plan para desbloquear más funcionalidades!
                </p>
                <p className="mt-0.5 text-xs text-white/80 sm:text-sm">
                  Estás en el plan <span className="font-semibold">{planLabel}</span>. Mejora hoy y
                  obtén acceso a herramientas premium.
                </p>
              </div>
            </div>
            <div className="flex shrink-0 items-center gap-2">
              <Button
                asChild
                size="sm"
                className="bg-white font-semibold text-indigo-700 shadow-md hover:bg-white/90"
              >
                <Link href={href}>
                  Ver planes
                  <ArrowRight className="ml-1 h-4 w-4" />
                </Link>
              </Button>
              <button
                onClick={handleDismiss}
                className="rounded-full p-1.5 text-white/70 hover:bg-white/20 hover:text-white"
                aria-label="Cerrar banner"
              >
                <X className="h-4 w-4" />
              </button>
            </div>
          </div>
        </div>
      </div>
    );
  }

  // Inline variant
  return (
    <div
      className={cn(
        'relative overflow-hidden rounded-xl bg-gradient-to-r from-purple-600 via-indigo-600 to-blue-600 p-6 shadow-lg',
        className
      )}
    >
      {/* Dismiss button */}
      <button
        onClick={handleDismiss}
        className="absolute top-3 right-3 rounded-full p-1.5 text-white/70 hover:bg-white/20 hover:text-white"
        aria-label="Cerrar banner"
      >
        <X className="h-4 w-4" />
      </button>

      {/* Content */}
      <div className="flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between">
        <div className="space-y-3">
          <div className="flex items-center gap-2">
            <div className="rounded-full bg-white/20 p-2">
              <Sparkles className="h-5 w-5 animate-pulse text-yellow-300" />
            </div>
            <h3 className="text-lg font-bold text-white">¡Desbloquea todo el potencial de OKLA!</h3>
          </div>
          <p className="max-w-lg text-sm text-white/85">
            Con el plan <span className="font-semibold">{planLabel}</span> te estás perdiendo
            funcionalidades que te ayudarán a vender más rápido. Actualiza hoy:
          </p>

          {/* Missing features */}
          <ul className="grid grid-cols-1 gap-2 sm:grid-cols-2">
            {features.map((feature, i) => {
              const Icon = feature.icon;
              return (
                <li key={i} className="flex items-center gap-2 text-sm text-white/90">
                  <Icon className="h-4 w-4 shrink-0 text-yellow-300" />
                  {feature.text}
                </li>
              );
            })}
          </ul>
        </div>

        {/* CTA */}
        <div className="shrink-0">
          <Button
            asChild
            size="lg"
            className="w-full bg-white font-bold text-indigo-700 shadow-lg hover:bg-white/90 sm:w-auto"
          >
            <Link href={href}>
              Actualizar Plan
              <ArrowRight className="ml-2 h-4 w-4" />
            </Link>
          </Button>
        </div>
      </div>
    </div>
  );
}
