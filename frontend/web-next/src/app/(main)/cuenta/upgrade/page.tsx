/**
 * Upgrade Checkout Page
 *
 * Allows users to review and confirm a plan upgrade.
 * Shows current plan vs target plan comparison, billing options,
 * and real payment integration (PayPal, Fygaro, Azul).
 *
 * URL: /cuenta/upgrade?plan=PRO&type=dealer
 *      /cuenta/upgrade?plan=premium&type=seller
 */

'use client';

import { useState, useMemo, Suspense } from 'react';
import Link from 'next/link';
import { formatPrice } from '@/lib/format';
import { useSearchParams, useRouter } from 'next/navigation';
import { usePlatformPricing } from '@/hooks/use-platform-pricing';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog';
import {
  Check,
  X,
  ArrowRight,
  Shield,
  ShieldCheck,
  Sparkles,
  ChevronLeft,
  PartyPopper,
  Lock,
  CreditCard,
  AlertCircle,
  Loader2,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { DealerPlan, SellerPlan, DEALER_PLAN_LIMITS, SELLER_PLAN_LIMITS } from '@/lib/plan-config';
import { PayPalPaymentButton } from '@/components/checkout/PayPalPaymentButton';
import { serverCreatePayPalOrder, serverCapturePayPalOrder } from '@/actions/checkout';
import { toast } from 'sonner';

// =============================================================================
// TYPES
// =============================================================================

type UserType = 'dealer' | 'seller';
type BillingPeriod = 'monthly' | 'annual';

interface PlanInfo {
  key: string;
  name: string;
  monthlyPrice: number;
  annualPrice: number;
  features: string[];
}

// =============================================================================
// PLAN DATA
// =============================================================================

// v5.0 — prices in DOP (USD × 58): LIBRE $0 | VISIBLE $29 | STARTER $59 | PRO $99 | ELITE $349 | ENTERPRISE $599
const DEALER_PLANS: Record<string, PlanInfo> = {
  libre: {
    key: 'libre',
    name: 'Libre',
    monthlyPrice: 0,
    annualPrice: 0,
    features: [
      'Publicaciones ilimitadas',
      'Hasta 5 fotos por vehículo',
      'Posición estándar en búsquedas',
      '1 valoración PricingAgent IA gratis',
    ],
  },
  visible: {
    key: 'visible',
    name: 'Visible',
    monthlyPrice: 1682, // $29 USD × 58
    annualPrice: 16820, // 2 meses gratis
    features: [
      'Publicaciones ilimitadas',
      'Hasta 10 fotos por vehículo',
      'Prioridad media en búsquedas',
      '3 publicaciones destacadas/mes',
      '🔵 Badge Dealer Verificado OKLA',
      '$15 OKLA Coins/mes',
      '5 valoraciones PricingAgent IA/mes',
      'Dashboard Analytics básico',
      'Soporte email',
      '✅ Garantía: 10 consultas en 30 días o mes 2 gratis',
    ],
  },
  starter: {
    key: 'starter',
    name: 'Starter',
    monthlyPrice: 3422, // $59 USD × 58
    annualPrice: 34220,
    features: [
      'Publicaciones ilimitadas',
      'Hasta 12 fotos por vehículo',
      'Alta prioridad en búsquedas',
      '5 publicaciones destacadas/mes',
      '🔵 Badge Verificado+',
      '$30 OKLA Coins/mes',
      'ChatAgent Web 100 conv/mes',
      'ChatAgent WhatsApp 100 conv/mes',
      'Overage $0.10/conv adicional',
      '10 valoraciones PricingAgent IA/mes',
      'Soporte email — respuesta 48h',
    ],
  },
  pro: {
    key: 'pro',
    name: 'Pro',
    monthlyPrice: 5742, // $99 USD × 58
    annualPrice: 57420,
    features: [
      'Publicaciones ilimitadas',
      'Hasta 15 fotos por vehículo',
      'Alta prioridad en búsquedas',
      '10 publicaciones destacadas/mes',
      '🥇 Badge Verificado Dorado',
      '$45 OKLA Coins/mes',
      'ChatAgent Web 300 conv/mes',
      'ChatAgent WhatsApp 300 conv/mes',
      'Overage $0.08/conv adicional',
      'Agendamiento automático',
      'PricingAgent IA ilimitado',
      'Dashboard Analytics avanzado',
      'Soporte email — 24h respuesta',
    ],
  },
  elite: {
    key: 'elite',
    name: 'Élite',
    monthlyPrice: 20242, // $349 USD × 58
    annualPrice: 202420,
    features: [
      'Publicaciones ilimitadas',
      'Hasta 20 fotos + video tour',
      'Top prioridad en búsquedas',
      '25 publicaciones destacadas/mes',
      '💎 Badge Verificado Premium',
      '$120 OKLA Coins/mes',
      'ChatAgent Web 5,000 conv/mes',
      'ChatAgent WhatsApp 5,000 conv/mes',
      'Overage $0.06/conv adicional',
      'Agendamiento + recordatorios WhatsApp',
      'PricingAgent IA ilimitado + PDF',
      'Dashboard Analytics completo + exportar',
      'Gerente de cuenta dedicado',
    ],
  },
  enterprise: {
    key: 'enterprise',
    name: 'Enterprise',
    monthlyPrice: 34742, // $599 USD × 58
    annualPrice: 347420,
    features: [
      'Publicaciones ilimitadas',
      'Hasta 20 fotos + video tour',
      '#1 garantizado en posición de búsqueda',
      '50 publicaciones destacadas/mes',
      '👑 Badge Enterprise',
      '$300 OKLA Coins/mes',
      'ChatAgent IA SIN LÍMITE',
      'Agendamiento + CRM + recordatorios WhatsApp',
      'PricingAgent IA ilimitado + PDF + API',
      'Acceso completo a API OKLA',
      'Dashboard + API + reportes personalizados',
      'Empleados ilimitados',
      'SLA garantizado + Soporte 24/7',
    ],
  },
};

// Alias map: URL ?plan= values → internal SELLER_PLANS keys
const SELLER_PLAN_ALIASES: Record<string, string> = {
  premium: 'verificado',
  standard: 'estandar',
  estandar_seller: 'estandar',
};

// v5.0 Seller Plans — prices shown in USD (API stores in DOP, upgrade page divides by 58)
const SELLER_PLANS: Record<string, PlanInfo> = {
  libre_seller: {
    key: 'libre_seller',
    name: 'Libre',
    monthlyPrice: 0,
    annualPrice: 0,
    features: [
      '1 publicación activa',
      'Hasta 5 fotos por vehículo',
      'Duración: 30 días',
      '⬇ Posición al fondo en búsquedas',
      '⚪ Sin badge de verificación',
      'KYC: solo email',
    ],
  },
  estandar: {
    key: 'estandar',
    name: 'Estándar',
    monthlyPrice: 579, // DOP — fallback estático; precio real via usePlatformPricing
    annualPrice: 579, // DOP por listing (pago único)
    features: [
      '1 publicación por pago',
      'Hasta 10 fotos por vehículo',
      'Duración: 60 días',
      '⬆ Posición media (bajo dealers)',
      '🔵 Badge Vendedor OKLA',
      'KYC: email + teléfono verificados',
      'Renovación de listing: $6.99',
      '1 valoración PricingAgent IA por listing',
    ],
  },
  verificado: {
    key: 'verificado',
    name: 'Verificado',
    monthlyPrice: 2029, // DOP — fallback estático; precio real via usePlatformPricing
    annualPrice: 2029,
    features: [
      '3 publicaciones simultáneas',
      'Hasta 12 fotos por vehículo',
      'Duración: 90 días',
      '📈 Alta posición (bajo dealers VISIBLE)',
      '✅ Badge Vendedor Verificado',
      'KYC completo: cédula + selfie + teléfono',
      'Renovación de listing incluida',
      '2 valoraciones PricingAgent IA/mes',
      'Analytics básico de tus publicaciones',
    ],
  },
};

// =============================================================================
// FEATURE COMPARISON
// =============================================================================

interface ComparisonRow {
  label: string;
  current: string | boolean;
  target: string | boolean;
}

function buildDealerComparison(currentKey: string, targetKey: string): ComparisonRow[] {
  const current = DEALER_PLAN_LIMITS[currentKey as DealerPlan];
  const target = DEALER_PLAN_LIMITS[targetKey as DealerPlan];
  if (!current || !target) return [];

  return [
    {
      label: 'Fotos por vehículo',
      current: String(current.maxImages),
      target: String(target.maxImages),
    },
    {
      label: 'Publicaciones destacadas',
      current: String(current.featuredListings),
      target: String(target.featuredListings),
    },
    { label: 'Analíticas', current: current.analyticsAccess, target: target.analyticsAccess },
    {
      label: 'Análisis de mercado',
      current: current.marketPriceAnalysis,
      target: target.marketPriceAnalysis,
    },
    { label: 'Carga masiva (CSV)', current: current.bulkUpload, target: target.bulkUpload },
    {
      label: 'Integración WhatsApp',
      current: current.whatsappIntegration,
      target: target.whatsappIntegration,
    },
    { label: 'Vista 360°', current: current.view360Available, target: target.view360Available },
    {
      label: 'Branding personalizado',
      current: current.customBranding,
      target: target.customBranding,
    },
    {
      label: 'Soporte prioritario',
      current: current.prioritySupport,
      target: target.prioritySupport,
    },
    { label: 'Acceso API', current: current.apiAccess, target: target.apiAccess },
    {
      label: 'OKLA Coins/mes',
      current: String(current.monthlyOklaCoinsCredits),
      target: String(target.monthlyOklaCoinsCredits),
    },
    {
      label: 'ChatBot IA (web)',
      current: current.chatAgentWeb === -1 ? 'Ilimitado' : String(current.chatAgentWeb),
      target: target.chatAgentWeb === -1 ? 'Ilimitado' : String(target.chatAgentWeb),
    },
  ];
}

function buildSellerComparison(currentKey: string, targetKey: string): ComparisonRow[] {
  const current = SELLER_PLAN_LIMITS[currentKey as SellerPlan];
  const target = SELLER_PLAN_LIMITS[targetKey as SellerPlan];
  if (!current || !target) return [];

  // v5.0 seller-specific display maps
  const positionMap: Record<string, string> = {
    libre_seller: '⬇ Fondo absoluto',
    estandar: '⬆ Media (bajo dealers)',
    verificado: '📈 Alta (bajo dealers VISIBLE)',
  };
  const badgeMap: Record<string, string> = {
    libre_seller: '⚪ Sin verificar',
    estandar: '🔵 Vendedor OKLA',
    verificado: '✅ Vendedor Verificado',
  };
  const kycMap: Record<string, string> = {
    libre_seller: 'Solo email',
    estandar: 'Email + teléfono',
    verificado: 'Cédula + selfie + tel.',
  };
  const renewalMap: Record<string, string> = {
    libre_seller: '—',
    estandar: '$6.99',
    verificado: 'Incluida',
  };
  const pricingAgentMap: Record<string, string> = {
    libre_seller: '—',
    estandar: '1/listing',
    verificado: '2/mes',
  };

  return [
    {
      label: 'Publicaciones activas',
      current: currentKey === 'estandar' ? '1 por pago' : String(current.maxListings),
      target: targetKey === 'estandar' ? '1 por pago' : String(target.maxListings),
    },
    {
      label: 'Duración del listing',
      current: `${current.listingDuration} días`,
      target: `${target.listingDuration} días`,
    },
    {
      label: 'Fotos por vehículo',
      current: `Hasta ${current.maxImages}`,
      target: `Hasta ${target.maxImages}`,
    },
    {
      label: 'Posición búsqueda',
      current: positionMap[currentKey] ?? '—',
      target: positionMap[targetKey] ?? '—',
    },
    {
      label: 'Badge de confianza',
      current: badgeMap[currentKey] ?? '—',
      target: badgeMap[targetKey] ?? '—',
    },
    {
      label: 'Verificación KYC',
      current: kycMap[currentKey] ?? '—',
      target: kycMap[targetKey] ?? '—',
    },
    {
      label: 'Renovación listing',
      current: renewalMap[currentKey] ?? '—',
      target: renewalMap[targetKey] ?? '—',
    },
    {
      label: 'PricingAgent IA',
      current: pricingAgentMap[currentKey] ?? '—',
      target: pricingAgentMap[targetKey] ?? '—',
    },
    {
      label: 'Analytics',
      current: current.analyticsAccess,
      target: target.analyticsAccess,
    },
  ];
}

// =============================================================================
// SELLER 3-PLAN COMPARISON
// =============================================================================

interface ComparisonRow3 {
  label: string;
  libre: string | boolean;
  estandar: string | boolean;
  verificado: string | boolean;
}

function buildSellerComparisonAll(): ComparisonRow3[] {
  const libre = SELLER_PLAN_LIMITS[SellerPlan.LIBRE];
  const estandar = SELLER_PLAN_LIMITS[SellerPlan.ESTANDAR];
  const verificado = SELLER_PLAN_LIMITS[SellerPlan.VERIFICADO];

  return [
    {
      label: 'Publicaciones activas',
      libre: String(libre.maxListings),
      estandar: '1 por pago',
      verificado: String(verificado.maxListings),
    },
    {
      label: 'Duración del listing',
      libre: `${libre.listingDuration} días`,
      estandar: `${estandar.listingDuration} días`,
      verificado: `${verificado.listingDuration} días`,
    },
    {
      label: 'Fotos por vehículo',
      libre: `Hasta ${libre.maxImages}`,
      estandar: `Hasta ${estandar.maxImages}`,
      verificado: `Hasta ${verificado.maxImages}`,
    },
    {
      label: 'Posición búsqueda',
      libre: '⬇ Fondo absoluto',
      estandar: '⬆ Media (bajo dealers)',
      verificado: '📈 Alta (bajo dealers VISIBLE)',
    },
    {
      label: 'Badge de confianza',
      libre: '⚪ Sin verificar',
      estandar: '🔵 Vendedor OKLA',
      verificado: '✅ Vendedor Verificado',
    },
    {
      label: 'Verificación KYC',
      libre: 'Solo email',
      estandar: 'Email + teléfono',
      verificado: 'Cédula + selfie + tel.',
    },
    {
      label: 'Renovación listing',
      libre: '—',
      estandar: 'RD$406/listing',
      verificado: 'Incluida',
    },
    {
      label: 'PricingAgent IA',
      libre: '—',
      estandar: '1/listing',
      verificado: '2/mes',
    },
    {
      label: 'Analytics',
      libre: libre.analyticsAccess,
      estandar: estandar.analyticsAccess,
      verificado: verificado.analyticsAccess,
    },
  ];
}

// =============================================================================
// PAYMENT METHOD TYPES
// =============================================================================

type PaymentMethod = 'paypal' | 'fygaro' | 'azul';

const PAYMENT_METHODS: {
  id: PaymentMethod;
  name: string;
  description: string;
  icon: string;
  color: string;
  recommended?: boolean;
}[] = [
  {
    id: 'paypal',
    name: 'PayPal',
    description: 'Paga con tu cuenta PayPal o tarjeta',
    icon: 'PP',
    color: 'bg-[#003087]',
    recommended: true,
  },
  {
    id: 'fygaro',
    name: 'Fygaro',
    description: 'Pago recurrente automático',
    icon: 'FY',
    color: 'bg-teal-600',
  },
  {
    id: 'azul',
    name: 'Azul',
    description: 'Tarjeta de crédito/débito (RD)',
    icon: 'AZ',
    color: 'bg-blue-600',
  },
];

// =============================================================================
// INNER COMPONENT (uses searchParams)
// =============================================================================

function UpgradeCheckoutInner() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const { pricing } = usePlatformPricing();

  const rawPlanKey = searchParams.get('plan')?.toLowerCase() ?? 'pro';
  const userType = (searchParams.get('type') as UserType) ?? 'dealer';
  // Resolve URL aliases → internal plan keys (e.g. ?plan=premium&type=seller → verificado)
  const targetPlanKey =
    userType === 'seller' ? (SELLER_PLAN_ALIASES[rawPlanKey] ?? rawPlanKey) : rawPlanKey;

  // Build dealer plans with LIVE pricing from admin configuration
  const dealerPlansLive = useMemo<Record<string, PlanInfo>>(
    () => ({
      libre: { ...DEALER_PLANS.libre, monthlyPrice: 0, annualPrice: 0 },
      visible: {
        ...DEALER_PLANS.visible,
        monthlyPrice: pricing.dealerVisible,
        annualPrice: Math.round(pricing.dealerVisible * 10),
      },
      starter: {
        ...DEALER_PLANS.starter,
        monthlyPrice: pricing.dealerStarter,
        annualPrice: Math.round(pricing.dealerStarter * 10),
      },
      pro: {
        ...DEALER_PLANS.pro,
        monthlyPrice: pricing.dealerPro,
        annualPrice: Math.round(pricing.dealerPro * 10),
      },
      elite: {
        ...DEALER_PLANS.elite,
        monthlyPrice: pricing.dealerElite,
        annualPrice: Math.round(pricing.dealerElite * 10),
      },
      enterprise: {
        ...DEALER_PLANS.enterprise,
        monthlyPrice: pricing.dealerEnterprise,
        annualPrice: Math.round(pricing.dealerEnterprise * 10),
      },
    }),
    [pricing]
  );

  // Build seller plans with LIVE pricing from admin configuration (prices in DOP)
  const sellerPlansLive = useMemo<Record<string, PlanInfo>>(
    () => ({
      libre_seller: { ...SELLER_PLANS.libre_seller, monthlyPrice: 0, annualPrice: 0 },
      estandar: {
        ...SELLER_PLANS.estandar,
        monthlyPrice: pricing.sellerEstandar,
        annualPrice: pricing.sellerEstandar,
      },
      verificado: {
        ...SELLER_PLANS.verificado,
        monthlyPrice: pricing.sellerVerificado,
        annualPrice: pricing.sellerVerificado,
      },
    }),
    [pricing]
  );

  const plans = userType === 'dealer' ? dealerPlansLive : sellerPlansLive;
  const planKeys = Object.keys(plans);

  // Current plan = lowest tier (mock: user is on free plan)
  const currentPlanKey = planKeys[0];
  const currentPlan = plans[currentPlanKey];
  // For sellers: allow switching target plan by clicking cards; initialize from URL param
  const [selectedTargetPlanKey, setSelectedTargetPlanKey] = useState<string>(targetPlanKey);
  const targetPlan =
    userType === 'seller'
      ? (sellerPlansLive[selectedTargetPlanKey] ?? sellerPlansLive['verificado'])
      : (plans[targetPlanKey] ?? plans[planKeys[planKeys.length - 1]]);

  const [billingPeriod, setBillingPeriod] = useState<BillingPeriod>('monthly');
  const [acceptTerms, setAcceptTerms] = useState(false);
  const [showSuccess, setShowSuccess] = useState(false);
  const [isProcessing, setIsProcessing] = useState(false);
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>('paypal');

  const comparison = useMemo(() => {
    if (userType === 'dealer') {
      return buildDealerComparison(currentPlanKey, targetPlan.key);
    }
    return buildSellerComparison(currentPlanKey, targetPlan.key);
  }, [userType, currentPlanKey, targetPlan.key]);

  const priceCurrency = 'DOP';
  const price = billingPeriod === 'annual' ? targetPlan.annualPrice : targetPlan.monthlyPrice;
  const monthlyCost =
    billingPeriod === 'annual' ? Math.round(targetPlan.annualPrice / 12) : targetPlan.monthlyPrice;
  const savings =
    billingPeriod === 'annual' ? targetPlan.monthlyPrice * 12 - targetPlan.annualPrice : 0;

  // For PayPal: convert DOP to USD equivalent for payment processing
  const paypalAmount = priceCurrency === 'DOP' ? (price / 58).toFixed(2) : price.toFixed(2);
  const paypalCurrency = 'USD';

  const handleFygaroPayment = () => {
    setIsProcessing(true);
    // Fygaro redirects to external payment page
    toast.info('Redirigiendo a Fygaro...');
    setTimeout(() => {
      setIsProcessing(false);
      toast.error('Fygaro no está configurado todavía. Usa PayPal.');
    }, 2000);
  };

  const handleAzulPayment = () => {
    setIsProcessing(true);
    // Azul redirects to external payment page
    toast.info('Redirigiendo a AZUL...');
    setTimeout(() => {
      setIsProcessing(false);
      toast.error('AZUL no está configurado todavía. Usa PayPal.');
    }, 2000);
  };

  const renderCellValue = (value: string | boolean) => {
    if (typeof value === 'boolean') {
      return value ? (
        <Check className="mx-auto h-5 w-5 text-emerald-500" />
      ) : (
        <X className="mx-auto h-5 w-5 text-gray-300" />
      );
    }
    return <span className={cn('font-semibold', value === '0' && 'text-gray-400')}>{value}</span>;
  };

  return (
    <div className="mx-auto max-w-5xl space-y-8 px-4 py-8 sm:px-6 lg:px-8">
      {/* Back link */}
      <Link
        href={userType === 'dealer' ? '/planes' : '/cuenta/suscripcion'}
        className="text-muted-foreground hover:text-foreground inline-flex items-center gap-1 text-sm"
      >
        <ChevronLeft className="h-4 w-4" />
        Volver a planes
      </Link>

      {/* Header */}
      <div className="text-center">
        <div className="bg-primary/10 mx-auto mb-4 inline-flex rounded-full p-3">
          <Sparkles className="text-primary h-8 w-8" />
        </div>
        <h1 className="text-3xl font-bold">Actualiza tu plan</h1>
        <p className="text-muted-foreground mt-2">
          {userType === 'seller'
            ? 'Elige el plan que mejor se adapte a ti y confirma tu upgrade'
            : `Compara tu plan actual con ${targetPlan.name} y confirma tu upgrade`}
        </p>
      </div>

      {/* Plan Comparison */}
      {userType === 'seller' ? (
        /* Seller: all 3 plans as selectable cards */
        <div className="grid gap-4 sm:grid-cols-3">
          {Object.values(sellerPlansLive).map(plan => {
            const isCurrent = plan.key === currentPlanKey;
            const isSelected = plan.key === selectedTargetPlanKey && !isCurrent;
            return (
              <Card
                key={plan.key}
                onClick={() => !isCurrent && setSelectedTargetPlanKey(plan.key)}
                className={cn(
                  'relative border-2 transition-all',
                  isCurrent
                    ? 'border-gray-200'
                    : isSelected
                      ? 'border-primary cursor-pointer shadow-md'
                      : 'cursor-pointer border-gray-200 hover:border-gray-400'
                )}
              >
                {isCurrent && (
                  <div className="absolute -top-3 left-1/2 -translate-x-1/2 rounded-full bg-gray-500 px-3 py-1 text-xs font-bold whitespace-nowrap text-white">
                    TU PLAN
                  </div>
                )}
                {isSelected && (
                  <div className="bg-primary absolute -top-3 left-1/2 -translate-x-1/2 rounded-full px-3 py-1 text-xs font-bold whitespace-nowrap text-white">
                    SELECCIONADO
                  </div>
                )}
                <CardHeader className="pt-6 text-center">
                  <CardTitle className="text-lg">{plan.name}</CardTitle>
                  <CardDescription className="text-base font-semibold">
                    {plan.monthlyPrice === 0
                      ? 'Gratis'
                      : `${formatPrice(plan.monthlyPrice, 'DOP')}${plan.key === 'estandar' ? '/listing' : '/mes'}`}
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <ul className="space-y-1.5">
                    {plan.features.map((f, i) => (
                      <li key={i} className="flex items-start gap-2 text-sm">
                        <Check
                          className={cn(
                            'mt-0.5 h-4 w-4 shrink-0',
                            isCurrent ? 'text-gray-400' : 'text-emerald-500'
                          )}
                        />
                        <span className={isCurrent ? 'text-muted-foreground' : 'font-medium'}>
                          {f}
                        </span>
                      </li>
                    ))}
                  </ul>
                </CardContent>
              </Card>
            );
          })}
        </div>
      ) : (
        /* Dealer: current plan vs target plan */
        <div className="grid gap-6 md:grid-cols-2">
          {/* Current Plan */}
          <Card className="border-2 border-gray-200">
            <CardHeader className="text-center">
              <Badge variant="outline" className="mx-auto mb-2 w-fit">
                Plan Actual
              </Badge>
              <CardTitle className="text-xl">{currentPlan.name}</CardTitle>
              <CardDescription>
                {currentPlan.monthlyPrice === 0
                  ? 'Gratis'
                  : `${formatPrice(currentPlan.monthlyPrice, priceCurrency)}/mes`}
              </CardDescription>
            </CardHeader>
            <CardContent>
              <ul className="space-y-2">
                {currentPlan.features.map((f, i) => (
                  <li key={i} className="flex items-start gap-2 text-sm">
                    <Check className="mt-0.5 h-4 w-4 shrink-0 text-gray-400" />
                    <span className="text-muted-foreground">{f}</span>
                  </li>
                ))}
              </ul>
            </CardContent>
          </Card>

          {/* Target Plan */}
          <Card className="border-primary relative border-2">
            <div className="bg-primary absolute -top-3 left-1/2 -translate-x-1/2 rounded-full px-4 py-1 text-xs font-bold text-white">
              RECOMENDADO
            </div>
            <CardHeader className="text-center">
              <Badge className="bg-primary mx-auto mb-2 w-fit text-white">Nuevo Plan</Badge>
              <CardTitle className="text-xl">{targetPlan.name}</CardTitle>
              <CardDescription>
                {formatPrice(monthlyCost, priceCurrency)}/mes
                {billingPeriod === 'annual' && savings > 0 && (
                  <span className="ml-1 text-emerald-600">(facturación anual)</span>
                )}
              </CardDescription>
            </CardHeader>
            <CardContent>
              <ul className="space-y-2">
                {targetPlan.features.map((f, i) => (
                  <li key={i} className="flex items-start gap-2 text-sm">
                    <Check className="mt-0.5 h-4 w-4 shrink-0 text-emerald-500" />
                    <span className="font-medium">{f}</span>
                  </li>
                ))}
              </ul>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Feature-by-feature comparison table */}
      {userType === 'seller' ? (
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Comparación detallada</CardTitle>
          </CardHeader>
          <CardContent className="p-0">
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-muted/50 border-b">
                    <th className="px-4 py-3 text-left font-medium">Característica</th>
                    <th className="px-4 py-3 text-center font-medium">Libre</th>
                    <th className="px-4 py-3 text-center font-medium">Estándar</th>
                    <th className="bg-primary/5 px-4 py-3 text-center font-medium">Verificado</th>
                  </tr>
                </thead>
                <tbody>
                  {buildSellerComparisonAll().map((row, i) => (
                    <tr key={i} className="border-b last:border-0">
                      <td className="px-4 py-3">{row.label}</td>
                      <td className="px-4 py-3 text-center">{renderCellValue(row.libre)}</td>
                      <td className="px-4 py-3 text-center">{renderCellValue(row.estandar)}</td>
                      <td className="bg-primary/5 px-4 py-3 text-center">
                        {renderCellValue(row.verificado)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </CardContent>
        </Card>
      ) : (
        comparison.length > 0 && (
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Comparación detallada</CardTitle>
            </CardHeader>
            <CardContent className="p-0">
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="bg-muted/50 border-b">
                      <th className="px-4 py-3 text-left font-medium">Característica</th>
                      <th className="px-4 py-3 text-center font-medium">{currentPlan.name}</th>
                      <th className="px-4 py-3 text-center font-medium">{targetPlan.name}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {comparison.map((row, i) => (
                      <tr key={i} className="border-b last:border-0">
                        <td className="px-4 py-3">{row.label}</td>
                        <td className="px-4 py-3 text-center">{renderCellValue(row.current)}</td>
                        <td className="bg-primary/5 px-4 py-3 text-center">
                          {renderCellValue(row.target)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </CardContent>
          </Card>
        )
      )}

      {/* Guarantee Banner — shown only for Plan VISIBLE */}
      {targetPlan.key === 'visible' && (
        <Card className="border-2 border-emerald-200 bg-emerald-50">
          <CardContent className="flex items-start gap-4 pt-6">
            <div className="rounded-full bg-emerald-100 p-3">
              <ShieldCheck className="h-6 w-6 text-emerald-600" />
            </div>
            <div className="space-y-1">
              <h3 className="text-lg font-bold text-emerald-800">Garantía de Resultados OKLA</h3>
              <p className="text-sm text-emerald-700">
                Si no recibes <strong>al menos 10 consultas</strong> de compradores en tus primeros{' '}
                <strong>30 días</strong> con el plan Visible,{' '}
                <strong>tu segundo mes es completamente gratis</strong>. Sin letra pequeña, sin
                complicaciones.
              </p>
              <Link
                href="/terminos#garantia-visible"
                className="text-primary inline-flex items-center gap-1 text-sm font-medium underline"
              >
                Ver términos de la garantía
                <ArrowRight className="h-3 w-3" />
              </Link>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Billing Period Selection */}
      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Período de facturación</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 sm:grid-cols-2">
            <button
              onClick={() => setBillingPeriod('monthly')}
              className={cn(
                'rounded-xl border-2 p-4 text-left transition-all',
                billingPeriod === 'monthly'
                  ? 'border-primary bg-primary/5'
                  : 'border-gray-200 hover:border-gray-300'
              )}
            >
              <p className="font-bold">Mensual</p>
              <p className="text-2xl font-bold">
                {formatPrice(targetPlan.monthlyPrice, priceCurrency)}
              </p>
              <p className="text-muted-foreground text-sm">
                {targetPlan.key === 'estandar' ? 'por listing' : 'por mes'}
              </p>
            </button>
            <button
              onClick={() => setBillingPeriod('annual')}
              className={cn(
                'relative rounded-xl border-2 p-4 text-left transition-all',
                billingPeriod === 'annual'
                  ? 'border-primary bg-primary/5'
                  : 'border-gray-200 hover:border-gray-300'
              )}
            >
              {savings > 0 && (
                <Badge className="absolute -top-2.5 right-3 bg-emerald-500">
                  Ahorra {formatPrice(savings)}
                </Badge>
              )}
              <p className="font-bold">Anual</p>
              <p className="text-2xl font-bold">
                {formatPrice(targetPlan.annualPrice, priceCurrency)}
              </p>
              <p className="text-muted-foreground text-sm">
                {formatPrice(Math.round(targetPlan.annualPrice / 12), priceCurrency)}/mes
              </p>
            </button>
          </div>
        </CardContent>
      </Card>

      {/* Payment Method Selection */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-lg">
            <CreditCard className="h-5 w-5" />
            Método de pago
          </CardTitle>
          <CardDescription>
            <span className="flex items-center gap-1 text-xs text-emerald-600">
              <Lock className="h-3 w-3" />
              Conexión segura con cifrado SSL de 256 bits
            </span>
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-3 sm:grid-cols-3">
            {PAYMENT_METHODS.map(method => (
              <button
                key={method.id}
                onClick={() => setPaymentMethod(method.id)}
                disabled={isProcessing}
                className={cn(
                  'relative flex flex-col items-center rounded-xl border-2 p-4 transition-all',
                  paymentMethod === method.id
                    ? 'border-primary bg-primary/5 shadow-sm'
                    : 'border-gray-200 hover:border-gray-300',
                  isProcessing && 'cursor-not-allowed opacity-50'
                )}
              >
                {method.recommended && (
                  <Badge className="absolute -top-2 right-2 bg-emerald-500 text-[10px]">
                    Recomendado
                  </Badge>
                )}
                <div
                  className={cn(
                    'mb-2 flex h-10 w-10 items-center justify-center rounded-lg text-sm font-bold text-white',
                    method.color
                  )}
                >
                  {method.icon}
                </div>
                <span className="text-sm font-semibold">{method.name}</span>
                <span className="text-muted-foreground mt-0.5 text-center text-[11px] leading-tight">
                  {method.description}
                </span>
              </button>
            ))}
          </div>

          <Separator />

          {/* Security Trust Indicators */}
          <div className="flex flex-wrap items-center justify-center gap-4 rounded-lg bg-gray-50 p-3">
            <div className="flex items-center gap-1.5 text-xs text-gray-600">
              <Shield className="h-4 w-4 text-emerald-500" />
              <span>Pago 100% seguro</span>
            </div>
            <div className="flex items-center gap-1.5 text-xs text-gray-600">
              <Lock className="h-4 w-4 text-emerald-500" />
              <span>Datos encriptados</span>
            </div>
            <div className="flex items-center gap-1.5 text-xs text-gray-600">
              <ShieldCheck className="h-4 w-4 text-emerald-500" />
              <span>Protección al comprador</span>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Terms & Payment Action */}
      <Card>
        <CardContent className="space-y-4 pt-6">
          <label className="flex cursor-pointer items-start gap-3">
            <input
              type="checkbox"
              checked={acceptTerms}
              onChange={e => setAcceptTerms(e.target.checked)}
              className="mt-1 h-4 w-4 rounded border-gray-300"
            />
            <span className="text-sm">
              Acepto los{' '}
              <Link href="/terminos" className="text-primary underline">
                términos y condiciones
              </Link>{' '}
              y la{' '}
              <Link href="/privacidad" className="text-primary underline">
                política de privacidad
              </Link>
              . Entiendo que se me cobrará <strong>{formatPrice(price, priceCurrency)}</strong>{' '}
              {billingPeriod === 'annual' ? 'anualmente' : 'mensualmente'} hasta que cancele mi
              suscripción.
            </span>
          </label>

          <Separator />

          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-lg font-bold">
                  Total: {formatPrice(price, priceCurrency)}
                  <span className="text-muted-foreground text-sm font-normal">
                    /
                    {targetPlan.key === 'estandar'
                      ? 'listing'
                      : billingPeriod === 'annual'
                        ? 'año'
                        : 'mes'}
                  </span>
                </p>
                {priceCurrency === 'DOP' && (
                  <p className="text-muted-foreground text-xs">
                    ≈ ${paypalAmount} USD (tasa de cambio aproximada)
                  </p>
                )}
              </div>
            </div>

            {/* PayPal Payment */}
            {paymentMethod === 'paypal' && acceptTerms && (
              <div className="space-y-3">
                <PayPalPaymentButton
                  clientId={process.env.NEXT_PUBLIC_PAYPAL_CLIENT_ID || ''}
                  amount={paypalAmount}
                  currency={paypalCurrency}
                  onCreateOrder={async () => {
                    const description = `OKLA Plan ${targetPlan.name} (${billingPeriod === 'annual' ? 'Anual' : 'Mensual'})`;
                    const result = await serverCreatePayPalOrder(
                      parseFloat(paypalAmount),
                      paypalCurrency,
                      description,
                      '',
                      `${window.location.origin}/checkout/exito`,
                      `${window.location.origin}/cuenta/upgrade?plan=${targetPlanKey}&type=${userType}`
                    );
                    if (!result.success || !result.data?.orderId) {
                      throw new Error(result.error || 'Error al crear orden PayPal');
                    }
                    return result.data.orderId;
                  }}
                  onApprove={async orderId => {
                    setIsProcessing(true);
                    const result = await serverCapturePayPalOrder(orderId, '');
                    if (result.success && result.data?.status === 'COMPLETED') {
                      setIsProcessing(false);
                      setShowSuccess(true);
                    } else {
                      setIsProcessing(false);
                      toast.error(result.error || 'Error al procesar el pago de PayPal');
                    }
                  }}
                  onError={message => {
                    setIsProcessing(false);
                    toast.error(message);
                  }}
                  onCancel={() => {
                    toast.info('Pago cancelado');
                  }}
                  disabled={isProcessing}
                />
              </div>
            )}

            {/* Fygaro Payment */}
            {paymentMethod === 'fygaro' && (
              <Button
                size="lg"
                disabled={!acceptTerms || isProcessing}
                onClick={handleFygaroPayment}
                className="w-full gap-2"
              >
                {isProcessing ? (
                  <>
                    <Loader2 className="h-4 w-4 animate-spin" />
                    Redirigiendo a Fygaro...
                  </>
                ) : (
                  <>
                    <Shield className="h-4 w-4" />
                    Pagar con Fygaro — {formatPrice(price, priceCurrency)}
                  </>
                )}
              </Button>
            )}

            {/* Azul Payment */}
            {paymentMethod === 'azul' && (
              <Button
                size="lg"
                disabled={!acceptTerms || isProcessing}
                onClick={handleAzulPayment}
                className="w-full gap-2 bg-blue-600 hover:bg-blue-700"
              >
                {isProcessing ? (
                  <>
                    <Loader2 className="h-4 w-4 animate-spin" />
                    Redirigiendo a AZUL...
                  </>
                ) : (
                  <>
                    <CreditCard className="h-4 w-4" />
                    Pagar con AZUL — {formatPrice(price, priceCurrency)}
                  </>
                )}
              </Button>
            )}

            {/* Message when terms not accepted and PayPal is selected */}
            {paymentMethod === 'paypal' && !acceptTerms && (
              <div className="flex items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800">
                <AlertCircle className="h-4 w-4 shrink-0" />
                <span>Acepta los términos y condiciones para continuar con el pago.</span>
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Trust Footer */}
      <div className="flex flex-col items-center gap-2 text-center">
        <div className="flex items-center gap-3">
          <img
            src="https://www.paypalobjects.com/webstatic/mktg/Logo/pp-logo-100px.png"
            alt="PayPal"
            className="h-5 opacity-50"
          />
          <span className="text-xs text-gray-300">|</span>
          <span className="text-xs font-medium text-gray-400">FYGARO</span>
          <span className="text-xs text-gray-300">|</span>
          <span className="text-xs font-medium text-blue-400">AZUL</span>
        </div>
        <p className="text-muted-foreground max-w-md text-xs">
          Tus datos financieros nunca son almacenados en nuestros servidores. Todos los pagos son
          procesados de forma segura por proveedores certificados PCI DSS.
        </p>
      </div>

      {/* Success Modal */}
      <Dialog open={showSuccess} onOpenChange={setShowSuccess}>
        <DialogContent className="text-center sm:max-w-md">
          <DialogHeader>
            <div className="mx-auto mb-4 rounded-full bg-emerald-100 p-4">
              <PartyPopper className="h-10 w-10 text-emerald-600" />
            </div>
            <DialogTitle className="text-2xl">¡Upgrade exitoso!</DialogTitle>
          </DialogHeader>
          <div className="space-y-3">
            <p className="text-muted-foreground">
              Tu plan ha sido actualizado a <strong>{targetPlan.name}</strong>. Ya puedes disfrutar
              de todas las funcionalidades premium.
            </p>
            <div className="rounded-lg bg-emerald-50 p-4">
              <p className="text-sm font-semibold text-emerald-800">
                Se ha enviado un recibo de confirmación a tu correo electrónico.
              </p>
            </div>
          </div>
          <DialogFooter className="mt-4 sm:justify-center">
            <Button
              onClick={() => {
                setShowSuccess(false);
                router.push('/cuenta');
              }}
              className="gap-2"
            >
              Ir a mi cuenta
              <ArrowRight className="h-4 w-4" />
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

// =============================================================================
// PAGE COMPONENT (with Suspense boundary for useSearchParams)
// =============================================================================

export default function UpgradeCheckoutPage() {
  return (
    <Suspense
      fallback={
        <div className="flex min-h-[400px] items-center justify-center">
          <div className="border-t-primary h-8 w-8 animate-spin rounded-full border-4 border-gray-300" />
        </div>
      }
    >
      <UpgradeCheckoutInner />
    </Suspense>
  );
}
