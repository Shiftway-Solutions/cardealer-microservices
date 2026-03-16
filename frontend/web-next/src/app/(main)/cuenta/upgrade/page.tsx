/**
 * Upgrade Checkout Page
 *
 * Allows users to review and confirm a plan upgrade.
 * Shows current plan vs target plan comparison, billing options,
 * and a mock payment form.
 *
 * URL: /cuenta/upgrade?plan=PRO&type=dealer
 */

'use client';

import { useState, useMemo, Suspense } from 'react';
import Link from 'next/link';
import { formatPrice } from '@/lib/format';
import { useSearchParams, useRouter } from 'next/navigation';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
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
  CreditCard,
  Shield,
  ShieldCheck,
  Sparkles,
  ChevronLeft,
  PartyPopper,
  Lock,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { DealerPlan, SellerPlan, DEALER_PLAN_LIMITS, SELLER_PLAN_LIMITS } from '@/lib/plan-config';

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

const DEALER_PLANS: Record<string, PlanInfo> = {
  libre: {
    key: 'libre',
    name: 'Libre',
    monthlyPrice: 0,
    annualPrice: 0,
    features: [
      'Publicaciones ilimitadas',
      '5 fotos por vehículo',
      'Portal público básico',
      'Prioridad de búsqueda estándar',
      '1 valoración IA gratis',
    ],
  },
  visible: {
    key: 'visible',
    name: 'Visible',
    monthlyPrice: 1699,
    annualPrice: 16990,
    features: [
      'Publicaciones ilimitadas',
      '10 fotos por vehículo',
      '3 publicaciones destacadas/mes',
      'Dashboard Analytics básico',
      'Badge Dealer Verificado OKLA',
      '$15 OKLA Coins/mes',
      '5 valoraciones IA/mes',
      'Soporte email',
      '✅ Garantía: si no recibes 10 consultas en 30 días, el mes 2 es gratis',
    ],
  },
  pro: {
    key: 'pro',
    name: 'Pro',
    monthlyPrice: 5799,
    annualPrice: 57990,
    features: [
      'Publicaciones ilimitadas',
      '15 fotos por vehículo',
      '10 publicaciones destacadas/mes',
      'Dashboard Analytics avanzado',
      'Badge Verificado Dorado',
      '$45 OKLA Coins/mes',
      'ChatBot IA (300 conv/mes)',
      'ChatBot WhatsApp (300 conv/mes)',
      'Agendamiento automático',
      'Valoración IA ilimitada',
      'Soporte email prioritario',
    ],
  },
  elite: {
    key: 'elite',
    name: 'Élite',
    monthlyPrice: 14599,
    annualPrice: 145990,
    features: [
      'Publicaciones ilimitadas',
      '20 fotos + video tour',
      '25 publicaciones destacadas/mes',
      'Dashboard Analytics completo + exportar',
      'Badge Verificado Premium',
      '$120 OKLA Coins/mes',
      'ChatBot IA (2,000 conv/mes)',
      'ChatBot WhatsApp (2,000 conv/mes)',
      'Agendamiento + recordatorios WA',
      'Valoración IA ilimitada + PDF',
      'Gerente de cuenta dedicado',
    ],
  },
};

const SELLER_PLANS: Record<string, PlanInfo> = {
  gratis: {
    key: 'gratis',
    name: 'Gratis',
    monthlyPrice: 0,
    annualPrice: 0,
    features: [
      '1 publicación activa',
      '10 fotos por vehículo',
      '30 días de duración',
      'Contacto por WhatsApp',
    ],
  },
  premium: {
    key: 'premium',
    name: 'Premium',
    monthlyPrice: 499,
    annualPrice: 4990,
    features: [
      '5 publicaciones activas',
      '30 fotos por vehículo',
      'Publicaciones permanentes',
      'Badge verificado',
      '2 destacadas/mes',
      'Estadísticas detalladas',
      'Boosts disponibles',
      'Vista 360°',
    ],
  },
  pro: {
    key: 'pro',
    name: 'Pro',
    monthlyPrice: 999,
    annualPrice: 9990,
    features: [
      '15 publicaciones activas',
      '50 fotos por vehículo',
      'Publicaciones permanentes',
      'Badge verificado',
      '5 destacadas/mes',
      'Estadísticas detalladas',
      'Boosts + alertas de precio',
      'Vista 360° + 3 videos',
      'ChatBot IA',
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

  return [
    {
      label: 'Publicaciones activas',
      current: String(current.maxListings),
      target: String(target.maxListings),
    },
    {
      label: 'Fotos por vehículo',
      current: String(current.maxImages),
      target: String(target.maxImages),
    },
    {
      label: 'Duración publicación',
      current: current.listingDuration === 0 ? 'Permanente' : `${current.listingDuration} días`,
      target: target.listingDuration === 0 ? 'Permanente' : `${target.listingDuration} días`,
    },
    {
      label: 'Publicaciones destacadas',
      current: String(current.featuredListings),
      target: String(target.featuredListings),
    },
    { label: 'Badge verificado', current: current.verifiedBadge, target: target.verifiedBadge },
    {
      label: 'Prioridad en búsquedas',
      current: current.searchPriority,
      target: target.searchPriority,
    },
    {
      label: 'Estadísticas detalladas',
      current: current.detailedStats,
      target: target.detailedStats,
    },
    { label: 'Boosts disponibles', current: current.boostAvailable, target: target.boostAvailable },
    { label: 'Vista 360°', current: current.view360Available, target: target.view360Available },
    {
      label: 'Alertas de precio',
      current: current.priceDropAlerts,
      target: target.priceDropAlerts,
    },
    { label: 'Videos', current: String(current.maxVideos), target: String(target.maxVideos) },
  ];
}

// =============================================================================
// INNER COMPONENT (uses searchParams)
// =============================================================================

function UpgradeCheckoutInner() {
  const searchParams = useSearchParams();
  const router = useRouter();

  const targetPlanKey = searchParams.get('plan')?.toLowerCase() ?? 'pro';
  const userType = (searchParams.get('type') as UserType) ?? 'dealer';

  const plans = userType === 'dealer' ? DEALER_PLANS : SELLER_PLANS;
  const planKeys = Object.keys(plans);

  // Current plan = lowest tier (mock: user is on free plan)
  const currentPlanKey = planKeys[0];
  const currentPlan = plans[currentPlanKey];
  const targetPlan = plans[targetPlanKey] ?? plans[planKeys[planKeys.length - 1]];

  const [billingPeriod, setBillingPeriod] = useState<BillingPeriod>('monthly');
  const [acceptTerms, setAcceptTerms] = useState(false);
  const [showSuccess, setShowSuccess] = useState(false);
  const [isProcessing, setIsProcessing] = useState(false);

  // Card form state (mock — no real processing)
  const [cardNumber, setCardNumber] = useState('');
  const [cardName, setCardName] = useState('');
  const [cardExpiry, setCardExpiry] = useState('');
  const [cardCvv, setCardCvv] = useState('');

  const comparison = useMemo(() => {
    if (userType === 'dealer') {
      return buildDealerComparison(currentPlanKey, targetPlan.key);
    }
    return buildSellerComparison(currentPlanKey, targetPlan.key);
  }, [userType, currentPlanKey, targetPlan.key]);

  const price = billingPeriod === 'annual' ? targetPlan.annualPrice : targetPlan.monthlyPrice;
  const monthlyCost =
    billingPeriod === 'annual' ? Math.round(targetPlan.annualPrice / 12) : targetPlan.monthlyPrice;
  const savings =
    billingPeriod === 'annual' ? targetPlan.monthlyPrice * 12 - targetPlan.annualPrice : 0;

  const handleConfirmUpgrade = () => {
    setIsProcessing(true);
    // Simulated processing delay
    setTimeout(() => {
      setIsProcessing(false);
      setShowSuccess(true);
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
          Compara tu plan actual con {targetPlan.name} y confirma tu upgrade
        </p>
      </div>

      {/* Plan Comparison */}
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
                : `${formatPrice(currentPlan.monthlyPrice)}/mes`}
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
              {formatPrice(monthlyCost)}/mes
              {billingPeriod === 'annual' && (
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

      {/* Feature-by-feature comparison table */}
      {comparison.length > 0 && (
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
      )}

      {/* Billing Period Selection */}

      {/* Guarantee Banner — shown only for Plan VISIBLE */}
      {targetPlan.key === 'visible' && (
        <Card className="border-2 border-emerald-200 bg-emerald-50">
          <CardContent className="flex items-start gap-4 pt-6">
            <div className="rounded-full bg-emerald-100 p-3">
              <ShieldCheck className="h-6 w-6 text-emerald-600" />
            </div>
            <div className="space-y-1">
              <h3 className="text-lg font-bold text-emerald-800">🛡️ Garantía de Resultados OKLA</h3>
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
              <p className="text-2xl font-bold">{formatPrice(targetPlan.monthlyPrice)}</p>
              <p className="text-muted-foreground text-sm">por mes</p>
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
              <p className="text-2xl font-bold">{formatPrice(targetPlan.annualPrice)}</p>
              <p className="text-muted-foreground text-sm">
                {formatPrice(Math.round(targetPlan.annualPrice / 12))}/mes
              </p>
            </button>
          </div>
        </CardContent>
      </Card>

      {/* Mock Payment Form */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-lg">
            <CreditCard className="h-5 w-5" />
            Información de pago
          </CardTitle>
          <CardDescription>
            <span className="flex items-center gap-1 text-xs text-emerald-600">
              <Lock className="h-3 w-3" />
              Conexión segura con cifrado SSL
            </span>
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div>
            <Label htmlFor="card-name">Nombre en la tarjeta</Label>
            <Input
              id="card-name"
              placeholder="Juan Pérez"
              value={cardName}
              onChange={e => setCardName(e.target.value)}
            />
          </div>
          <div>
            <Label htmlFor="card-number">Número de tarjeta</Label>
            <Input
              id="card-number"
              placeholder="4242 4242 4242 4242"
              value={cardNumber}
              onChange={e => {
                const val = e.target.value.replace(/\D/g, '').slice(0, 16);
                setCardNumber(val.replace(/(.{4})/g, '$1 ').trim());
              }}
            />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label htmlFor="card-expiry">Fecha de expiración</Label>
              <Input
                id="card-expiry"
                placeholder="MM/AA"
                value={cardExpiry}
                onChange={e => {
                  let val = e.target.value.replace(/\D/g, '').slice(0, 4);
                  if (val.length >= 3) val = val.slice(0, 2) + '/' + val.slice(2);
                  setCardExpiry(val);
                }}
              />
            </div>
            <div>
              <Label htmlFor="card-cvv">CVV</Label>
              <Input
                id="card-cvv"
                placeholder="123"
                type="password"
                maxLength={4}
                value={cardCvv}
                onChange={e => setCardCvv(e.target.value.replace(/\D/g, '').slice(0, 4))}
              />
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Terms & Confirm */}
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
              . Entiendo que se me cobrará <strong>{formatPrice(price)}</strong>{' '}
              {billingPeriod === 'annual' ? 'anualmente' : 'mensualmente'} hasta que cancele mi
              suscripción.
            </span>
          </label>

          <Separator />

          <div className="flex flex-col items-center gap-4 sm:flex-row sm:justify-between">
            <div>
              <p className="text-lg font-bold">
                Total: {formatPrice(price)}
                <span className="text-muted-foreground text-sm font-normal">
                  /{billingPeriod === 'annual' ? 'año' : 'mes'}
                </span>
              </p>
            </div>
            <Button
              size="lg"
              disabled={!acceptTerms || isProcessing}
              onClick={handleConfirmUpgrade}
              className="w-full gap-2 sm:w-auto"
            >
              {isProcessing ? (
                <>
                  <span className="h-4 w-4 animate-spin rounded-full border-2 border-white border-t-transparent" />
                  Procesando...
                </>
              ) : (
                <>
                  <Shield className="h-4 w-4" />
                  Confirmar Upgrade
                </>
              )}
            </Button>
          </div>
        </CardContent>
      </Card>

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
