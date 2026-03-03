/**
 * Plan Gate Components
 *
 * Reusable components for feature gating based on subscription plan.
 * - PlanGate: Wraps content, shows UpgradePrompt if feature not accessible
 * - UpgradePrompt: Standalone upgrade CTA card/banner
 * - PlanBadge: Shows current plan badge
 */

'use client';

import Link from 'next/link';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Lock, Crown, Sparkles, ArrowRight, Zap, Star, Eye } from 'lucide-react';
import { usePlanAccess, type PlanFeatureKey } from '@/hooks/use-plan-access';

// =============================================================================
// PLAN GATE — Conditionally renders children or upgrade prompt
// =============================================================================

interface PlanGateProps {
  /** Feature key to check access for */
  feature: PlanFeatureKey;
  /** Content to show when feature is accessible */
  children: React.ReactNode;
  /** Optional custom title for the upgrade prompt */
  title?: string;
  /** Optional custom description */
  description?: string;
  /** Visual variant: 'card' (default) | 'inline' | 'overlay' */
  variant?: 'card' | 'inline' | 'overlay';
}

export function PlanGate({
  feature,
  children,
  title,
  description,
  variant = 'card',
}: PlanGateProps) {
  const { canAccess, isLoading } = usePlanAccess();

  // While loading, show a skeleton placeholder
  if (isLoading) {
    return <div className="animate-pulse rounded-xl bg-gray-100 p-6">{children}</div>;
  }

  // Feature accessible — render children normally
  if (canAccess(feature)) {
    return <>{children}</>;
  }

  // Feature locked — show upgrade prompt
  return (
    <UpgradePrompt feature={feature} title={title} description={description} variant={variant} />
  );
}

// =============================================================================
// UPGRADE PROMPT — Standalone upgrade CTA
// =============================================================================

interface UpgradePromptProps {
  /** Feature being gated */
  feature: PlanFeatureKey;
  /** Custom title */
  title?: string;
  /** Custom description */
  description?: string;
  /** Visual variant */
  variant?: 'card' | 'inline' | 'overlay';
}

const FEATURE_LABELS: Record<PlanFeatureKey, { title: string; description: string }> = {
  analytics: {
    title: 'Analytics Avanzados',
    description:
      'Accede a métricas detalladas de rendimiento, vistas y engagement de tus publicaciones.',
  },
  marketPriceAnalysis: {
    title: 'Análisis de Precios de Mercado',
    description: 'IA analiza el mercado para sugerirte el precio óptimo y maximizar tus ventas.',
  },
  bulkUpload: {
    title: 'Carga Masiva',
    description: 'Importa múltiples vehículos desde CSV/Excel para ahorrar tiempo.',
  },
  featuredListings: {
    title: 'Publicaciones Destacadas',
    description: 'Destaca tus vehículos en posiciones premium para mayor visibilidad.',
  },
  leadManagement: {
    title: 'CRM de Leads',
    description:
      'Gestiona contactos, seguimiento y conversión de prospectos con herramientas profesionales.',
  },
  emailAutomation: {
    title: 'Automatización de Email',
    description: 'Envía secuencias de seguimiento automáticas a tus prospectos interesados.',
  },
  customBranding: {
    title: 'Marca Personalizada',
    description: 'Personaliza tu perfil con tu logo, colores y branding corporativo.',
  },
  apiAccess: {
    title: 'Acceso API',
    description: 'Integra OKLA con tus sistemas usando nuestra API RESTful.',
  },
  prioritySupport: {
    title: 'Soporte Prioritario',
    description: 'Accede a soporte técnico con tiempos de respuesta garantizados.',
  },
  whatsappIntegration: {
    title: 'WhatsApp Business',
    description: 'Recibe consultas directamente en tu WhatsApp Business con botón de contacto.',
  },
  searchPriority: {
    title: 'Prioridad en Búsquedas',
    description: 'Tus publicaciones aparecen primero en los resultados de búsqueda.',
  },
  verifiedBadge: {
    title: 'Badge de Verificación',
    description:
      'Muestra un badge de vendedor verificado para generar más confianza en compradores.',
  },
  detailedStats: {
    title: 'Estadísticas Detalladas',
    description: 'Vistas, clics, contactos y métricas de rendimiento por publicación.',
  },
  boostAvailable: {
    title: 'Boosts de Visibilidad',
    description: 'Impulsa tus publicaciones temporalmente para más exposición y visitas.',
  },
  socialSharing: {
    title: 'Compartir en Redes',
    description: 'Comparte tus publicaciones en redes sociales con preview optimizado.',
  },
  priceDropAlerts: {
    title: 'Alertas de Baja de Precio',
    description:
      'Notifica automáticamente a los interesados cuando bajas el precio de un vehículo.',
  },
};

export function UpgradePrompt({
  feature,
  title,
  description,
  variant = 'card',
}: UpgradePromptProps) {
  const { minimumPlanFor, upgradeUrl, planLabel, isDealer } = usePlanAccess();
  const featureInfo = FEATURE_LABELS[feature] ?? {
    title: 'Función Premium',
    description: 'Esta función requiere un plan superior.',
  };

  const displayTitle = title ?? featureInfo.title;
  const displayDesc = description ?? featureInfo.description;
  const requiredPlan = minimumPlanFor(feature);

  if (variant === 'inline') {
    return (
      <div className="flex items-center gap-3 rounded-lg border border-amber-200 bg-amber-50 p-3">
        <Lock className="h-4 w-4 flex-shrink-0 text-amber-600" />
        <div className="flex-1">
          <p className="text-sm font-medium text-amber-900">{displayTitle}</p>
          <p className="text-xs text-amber-700">Disponible desde el plan {requiredPlan}</p>
        </div>
        <Link href={upgradeUrl}>
          <Button size="sm" variant="outline" className="border-amber-300 text-amber-700">
            Upgrade
          </Button>
        </Link>
      </div>
    );
  }

  if (variant === 'overlay') {
    return (
      <div className="relative">
        <div className="pointer-events-none opacity-30 blur-[2px] select-none">
          <div className="h-48 rounded-xl bg-gray-100" />
        </div>
        <div className="absolute inset-0 flex items-center justify-center">
          <Card className="w-80 border-amber-200 shadow-lg">
            <CardContent className="p-5 text-center">
              <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-full bg-amber-100">
                <Lock className="h-6 w-6 text-amber-600" />
              </div>
              <h4 className="mb-1 font-semibold">{displayTitle}</h4>
              <p className="text-muted-foreground mb-4 text-sm">{displayDesc}</p>
              <Link href={upgradeUrl}>
                <Button className="w-full gap-2 bg-[#00A870] hover:bg-[#009663]">
                  <Crown className="h-4 w-4" />
                  Upgrade a {requiredPlan}
                </Button>
              </Link>
            </CardContent>
          </Card>
        </div>
      </div>
    );
  }

  // Default: 'card' variant
  return (
    <Card className="border-dashed border-amber-300 bg-gradient-to-br from-amber-50 to-orange-50">
      <CardContent className="flex flex-col items-center p-8 text-center">
        <div className="mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-amber-100">
          <Lock className="h-8 w-8 text-amber-600" />
        </div>
        <h3 className="text-foreground mb-2 text-lg font-bold">{displayTitle}</h3>
        <p className="text-muted-foreground mb-6 max-w-sm text-sm">{displayDesc}</p>
        <div className="mb-4 flex items-center gap-2">
          <Badge variant="secondary" className="bg-amber-100 text-amber-800">
            Plan actual: {planLabel}
          </Badge>
          <ArrowRight className="h-4 w-4 text-amber-500" />
          <Badge className="bg-[#00A870] text-white">Requiere: {requiredPlan}</Badge>
        </div>
        <Link href={upgradeUrl}>
          <Button size="lg" className="gap-2 bg-[#00A870] hover:bg-[#009663]">
            <Crown className="h-5 w-5" />
            {isDealer ? 'Mejorar Plan Dealer' : 'Mejorar Mi Plan'}
          </Button>
        </Link>
      </CardContent>
    </Card>
  );
}

// =============================================================================
// PLAN BADGE — Shows current plan with icon
// =============================================================================

interface PlanBadgeProps {
  /** Override plan to display (defaults to user's current plan) */
  plan?: string;
  /** Size variant */
  size?: 'sm' | 'md' | 'lg';
  /** Whether to show upgrade link */
  showUpgradeLink?: boolean;
}

const PLAN_STYLES: Record<string, { bg: string; text: string; icon: typeof Zap }> = {
  // Dealer plans
  libre: { bg: 'bg-gray-100', text: 'text-gray-700', icon: Zap },
  visible: { bg: 'bg-blue-100', text: 'text-blue-700', icon: Eye },
  pro: { bg: 'bg-purple-100', text: 'text-purple-700', icon: Star },
  elite: { bg: 'bg-amber-100', text: 'text-amber-700', icon: Crown },
  // Seller plans
  gratis: { bg: 'bg-gray-100', text: 'text-gray-700', icon: Zap },
  premium: { bg: 'bg-blue-100', text: 'text-blue-700', icon: Sparkles },
};

const ALL_PLAN_LABELS: Record<string, string> = {
  libre: 'Libre',
  visible: 'Visible',
  pro: 'PRO',
  elite: 'Élite',
  gratis: 'Gratis',
  premium: 'Premium',
  none: 'Sin Plan',
};

export function PlanBadge({ plan: overridePlan, size = 'md', showUpgradeLink }: PlanBadgeProps) {
  const { currentPlan, planLabel, upgradeUrl } = usePlanAccess();
  const plan = overridePlan ?? currentPlan;
  const label = ALL_PLAN_LABELS[plan] ?? planLabel;
  const style = PLAN_STYLES[plan] ?? PLAN_STYLES.libre;
  const Icon = style.icon;

  const sizeClasses = {
    sm: 'px-2 py-0.5 text-xs gap-1',
    md: 'px-3 py-1 text-sm gap-1.5',
    lg: 'px-4 py-1.5 text-base gap-2',
  };

  const iconSizes = { sm: 'h-3 w-3', md: 'h-4 w-4', lg: 'h-5 w-5' };

  const badge = (
    <span
      className={`inline-flex items-center rounded-full font-medium ${style.bg} ${style.text} ${sizeClasses[size]}`}
    >
      <Icon className={iconSizes[size]} />
      {label}
    </span>
  );

  if (showUpgradeLink) {
    return (
      <Link href={upgradeUrl} className="group inline-flex items-center gap-2">
        {badge}
        <span className="text-muted-foreground text-xs opacity-0 transition-opacity group-hover:opacity-100">
          Mejorar →
        </span>
      </Link>
    );
  }

  return badge;
}

// =============================================================================
// PLAN USAGE BAR — Shows usage vs limit
// =============================================================================

interface PlanUsageBarProps {
  current: number;
  max: number;
  label: string;
  /** Whether to show "Ilimitado" when max is very large */
  showUnlimited?: boolean;
}

export function PlanUsageBar({ current, max, label, showUnlimited = true }: PlanUsageBarProps) {
  const isUnlimited = max >= 99999;
  const percentage = isUnlimited
    ? Math.min((current / 100) * 100, 100)
    : Math.min((current / max) * 100, 100);
  const isNearLimit = !isUnlimited && percentage >= 80;
  const isAtLimit = !isUnlimited && current >= max;

  return (
    <div className="space-y-1.5">
      <div className="flex items-center justify-between text-sm">
        <span className="text-muted-foreground">{label}</span>
        <span
          className={`font-medium ${isAtLimit ? 'text-red-600' : isNearLimit ? 'text-amber-600' : 'text-foreground'}`}
        >
          {current} / {isUnlimited && showUnlimited ? '∞' : max}
        </span>
      </div>
      <div className="h-2 overflow-hidden rounded-full bg-gray-100">
        <div
          className={`h-full rounded-full transition-all ${
            isAtLimit ? 'bg-red-500' : isNearLimit ? 'bg-amber-500' : 'bg-[#00A870]'
          }`}
          style={{ width: `${percentage}%` }}
        />
      </div>
    </div>
  );
}
