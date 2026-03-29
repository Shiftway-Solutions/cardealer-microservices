/**
 * Seller Subscription Page
 *
 * Manages subscription plan for individual sellers.
 * Shows current plan, usage, available plans, and upgrade/downgrade options.
 */

'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Crown, Check, Zap, Sparkles, BarChart3, Loader2, X, ArrowRight } from 'lucide-react';
import { toast } from 'sonner';
import { useAuth } from '@/hooks/use-auth';
import { usePlanAccess } from '@/hooks/use-plan-access';
import { usePlatformPricing } from '@/hooks/use-platform-pricing';
import { PlanBadge, PlanUsageBar } from '@/components/plan/plan-gate';
import { formatPrice } from '@/lib/format';

// =============================================================================
// HELPERS
// =============================================================================

// Plan features description — v5.0 — keys MUST match SellerPlan values (libre_seller / estandar / verificado)
function getPlanFeatures() {
  return {
    libre_seller: [
      '1 publicación activa',
      'Hasta 5 fotos por vehículo',
      'Duración: 30 días',
      '⬇ Posición al fondo en búsquedas',
      '⚪ Sin badge de verificación',
      'KYC: solo email',
    ],
    estandar: [
      '1 publicación por pago',
      'Hasta 10 fotos por vehículo',
      'Duración: 60 días',
      '⬆ Posición media (bajo dealers)',
      '🔵 Badge Vendedor OKLA',
      'KYC: email + teléfono verificados',
      'Renovación de listing: RD$579',
      '1 valoración PricingAgent IA por listing',
    ],
    verificado: [
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
  };
}

// =============================================================================
// PAGE COMPONENT
// =============================================================================

export default function SellerSubscriptionPage() {
  useAuth();
  const router = useRouter();
  const { currentPlan, maxListings, maxImages, featuredPerMonth, isLoading } = usePlanAccess();
  const { pricing } = usePlatformPricing();
  const [changingPlan, setChangingPlan] = useState<string | null>(null);

  const plans = getPlanFeatures();

  // Simulated usage data (would come from API in real implementation)
  const usage = {
    currentListings: 0,
    featuredUsed: 0,
  };

  const handleChangePlan = async (newPlan: string) => {
    if (newPlan === currentPlan) return;
    setChangingPlan(newPlan);
    try {
      if (newPlan === 'libre_seller') {
        // Downgrade — inform user the change applies at end of period
        toast.success(
          'Plan cambiado a Libre. Los cambios aplican al final del período de facturación.'
        );
        setChangingPlan(null);
        return;
      }
      // For paid plans redirect to the upgrade checkout
      const planParam = newPlan === 'estandar' ? 'estandar' : 'verificado';
      router.push(`/cuenta/upgrade?plan=${planParam}&type=seller`);
    } catch {
      toast.error('Error al cambiar el plan. Intenta de nuevo.');
      setChangingPlan(null);
    }
  };

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-10 w-64" />
        <div className="grid gap-6 md:grid-cols-3">
          {[1, 2, 3].map(i => (
            <Skeleton key={i} className="h-[450px] rounded-xl" />
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-8">
      {/* ── Header ──────────────────────────────────────────── */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-foreground text-2xl font-bold">Mi Suscripción</h1>
          <p className="text-muted-foreground">
            Gestiona tu plan de vendedor y desbloquea más funciones
          </p>
        </div>
        <PlanBadge size="lg" />
      </div>

      {/* ── Current Plan Summary ─────────────────────────────── */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <BarChart3 className="text-primary h-5 w-5" />
            Resumen de Tu Plan
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-3">
            <PlanUsageBar
              current={usage.currentListings}
              max={maxListings}
              label="Publicaciones activas"
            />
            <PlanUsageBar
              current={0}
              max={maxImages}
              label="Fotos por vehículo (máx)"
              showUnlimited={false}
            />
            {featuredPerMonth > 0 && (
              <PlanUsageBar
                current={usage.featuredUsed}
                max={featuredPerMonth}
                label="Destacadas este mes"
              />
            )}
          </div>
        </CardContent>
      </Card>

      {/* ── Plan Cards ─────────────────────────────────────── */}
      <div className="grid gap-6 md:grid-cols-3">
        {/* LIBRE */}
        <PlanCard
          name="Libre"
          planKey="libre_seller"
          price={0}
          period=""
          description="Para vendedores ocasionales"
          features={plans.libre_seller}
          icon={Zap}
          isCurrent={currentPlan === 'libre_seller'}
          isChanging={changingPlan === 'libre_seller'}
          onSelect={() => handleChangePlan('libre_seller')}
          currentPlan={currentPlan}
        />

        {/* ESTÁNDAR */}
        <PlanCard
          name="Estándar"
          planKey="estandar"
          price={pricing.sellerEstandar}
          period="/publicación"
          description="Vende más rápido"
          features={plans.estandar}
          icon={Sparkles}
          isCurrent={currentPlan === 'estandar'}
          isPopular
          isChanging={changingPlan === 'estandar'}
          onSelect={() => handleChangePlan('estandar')}
          currentPlan={currentPlan}
        />

        {/* VERIFICADO */}
        <PlanCard
          name="Verificado"
          planKey="verificado"
          price={pricing.sellerVerificado}
          period="/mes"
          description="Máxima visibilidad y herramientas"
          features={plans.verificado}
          icon={Crown}
          isCurrent={currentPlan === 'verificado'}
          isChanging={changingPlan === 'verificado'}
          onSelect={() => handleChangePlan('verificado')}
          currentPlan={currentPlan}
        />
      </div>

      {/* ── Feature Comparison Table ──────────────────────── */}
      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Comparación de Planes</CardTitle>
          <CardDescription>Todas las funciones incluidas en cada plan de vendedor</CardDescription>
        </CardHeader>
        <CardContent className="overflow-x-auto">
          <ComparisonTable currentPlan={currentPlan} />
        </CardContent>
      </Card>

      {/* ── Upgrade to Dealer CTA ──────────────────────────── */}
      <Card className="border-primary/20 from-primary/5 bg-gradient-to-r to-emerald-50">
        <CardContent className="flex flex-col items-center gap-4 p-6 sm:flex-row sm:justify-between">
          <div className="flex items-center gap-4">
            <div className="bg-primary/10 flex h-12 w-12 items-center justify-center rounded-xl">
              <Crown className="text-primary h-6 w-6" />
            </div>
            <div>
              <h3 className="font-semibold">¿Eres un negocio o concesionario?</h3>
              <p className="text-muted-foreground text-sm">
                Los planes Dealer ofrecen vehículos ilimitados, CRM, leads, analytics y más.
              </p>
            </div>
          </div>
          <Link href="/dealers/registro">
            <Button variant="outline" className="border-primary text-primary gap-2">
              Ver Planes Dealer
              <ArrowRight className="h-4 w-4" />
            </Button>
          </Link>
        </CardContent>
      </Card>

      {/* ── FAQ ──────────────────────────────────────────── */}
      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Preguntas Frecuentes</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <FaqItem
            q="¿Puedo cambiar de plan en cualquier momento?"
            a="Sí, puedes subir o bajar de plan cuando quieras. Al subir, el cambio aplica inmediatamente. Al bajar, aplica al final del período de facturación actual."
          />
          <FaqItem
            q="¿Qué pasa con mis publicaciones si bajo de plan?"
            a="Tus publicaciones existentes permanecerán activas. Si excedes el límite del nuevo plan, no podrás crear nuevas hasta que estés dentro del límite."
          />
          <FaqItem
            q="¿El badge de verificado se mantiene si cambio de plan?"
            a="El badge de verificado solo está disponible en los planes Estándar y Verificado. Si bajas al plan Libre, el badge se desactiva."
          />
          <FaqItem
            q="¿Cómo funcionan las publicaciones destacadas?"
            a="Cada mes se reinicia tu cuota de publicaciones destacadas según tu plan. Las destacadas aparecen primero en resultados de búsqueda por 7 días."
          />
          <FaqItem
            q="¿Puedo convertirme en Dealer?"
            a="Sí, si tienes un negocio de venta de vehículos puedes registrarte como Dealer para acceder a funciones como CRM, leads, inventario masivo y más."
          />
        </CardContent>
      </Card>
    </div>
  );
}

// =============================================================================
// PLAN CARD COMPONENT
// =============================================================================

interface PlanCardProps {
  name: string;
  planKey: string;
  price: number;
  period: string;
  description: string;
  features: string[];
  icon: typeof Zap;
  isCurrent: boolean;
  isPopular?: boolean;
  isChanging: boolean;
  onSelect: () => void;
  currentPlan: string;
}

function PlanCard({
  name,
  planKey,
  price,
  period,
  description,
  features,
  icon: Icon,
  isCurrent,
  isPopular,
  isChanging,
  onSelect,
  currentPlan,
}: PlanCardProps) {
  const isUpgrade = getPlanOrder(planKey) > getPlanOrder(currentPlan);

  return (
    <Card
      className={`relative flex flex-col ${
        isPopular
          ? 'border-primary shadow-primary/10 border-2 shadow-lg'
          : isCurrent
            ? 'border-2 border-blue-400'
            : 'border-border'
      }`}
    >
      {isPopular && (
        <div className="absolute -top-3 left-1/2 -translate-x-1/2">
          <Badge className="bg-primary text-white">MÁS POPULAR</Badge>
        </div>
      )}
      {isCurrent && (
        <div className="absolute -top-3 right-4">
          <Badge variant="secondary" className="bg-blue-100 text-blue-700">
            Plan Actual
          </Badge>
        </div>
      )}

      <CardContent className="flex flex-1 flex-col p-6">
        <div className="mb-4 flex items-center gap-2">
          <div
            className={`flex h-10 w-10 items-center justify-center rounded-xl ${
              isPopular ? 'bg-primary/10' : 'bg-muted'
            }`}
          >
            <Icon className={`h-5 w-5 ${isPopular ? 'text-primary' : 'text-muted-foreground'}`} />
          </div>
          <div>
            <h3 className="text-lg font-bold">{name}</h3>
            <p className="text-muted-foreground text-xs">{description}</p>
          </div>
        </div>

        <div className="mb-6">
          {price === 0 ? (
            <span className="text-foreground text-3xl font-bold">Gratis</span>
          ) : (
            <>
              <span className="text-foreground text-3xl font-bold">{formatPrice(price)}</span>
              <span className="text-muted-foreground text-sm">{period}</span>
            </>
          )}
        </div>

        <ul className="mb-6 flex-1 space-y-2.5">
          {features.map((feature, i) => (
            <li key={i} className="flex items-start gap-2 text-sm">
              <Check className="text-primary mt-0.5 h-4 w-4 flex-shrink-0" />
              <span className="text-muted-foreground">{feature}</span>
            </li>
          ))}
        </ul>

        {isCurrent ? (
          <Button disabled variant="outline" className="w-full">
            Plan Actual
          </Button>
        ) : (
          <Button
            onClick={onSelect}
            disabled={isChanging}
            className={`w-full gap-2 ${
              isUpgrade ? 'bg-primary hover:bg-primary/90' : 'bg-gray-600 hover:bg-gray-700'
            }`}
          >
            {isChanging ? (
              <>
                <Loader2 className="h-4 w-4 animate-spin" />
                Procesando...
              </>
            ) : isUpgrade ? (
              <>
                <ArrowRight className="h-4 w-4" />
                Mejorar a {name}
              </>
            ) : (
              `Cambiar a ${name}`
            )}
          </Button>
        )}
      </CardContent>
    </Card>
  );
}

function getPlanOrder(plan: string): number {
  switch (plan) {
    case 'libre_seller':
      return 0;
    case 'estandar':
      return 1;
    case 'verificado':
      return 2;
    default:
      return -1;
  }
}

// =============================================================================
// COMPARISON TABLE
// =============================================================================

function ComparisonTable({ currentPlan }: { currentPlan: string }) {
  const features = [
    {
      label: 'Publicaciones activas',
      libre_seller: '1',
      estandar: '1 por pago',
      verificado: '3 simultáneas',
    },
    { label: 'Fotos por vehículo', libre_seller: '5', estandar: '10', verificado: '12' },
    {
      label: 'Duración del listing',
      libre_seller: '30 días',
      estandar: '60 días',
      verificado: '90 días',
    },
    { label: 'Contacto WhatsApp', libre_seller: true, estandar: true, verificado: true },
    {
      label: 'Posición en búsquedas',
      libre_seller: '⬇ Fondo',
      estandar: '⬆ Media',
      verificado: 'Alta',
    },
    {
      label: 'Badge de confianza',
      libre_seller: '⚪ Sin verificar',
      estandar: '🔵 Vendedor OKLA',
      verificado: '✅ Verificado',
    },
    {
      label: 'Verificación KYC',
      libre_seller: 'Solo email',
      estandar: 'Email + teléfono',
      verificado: 'Cédula + selfie + tel.',
    },
    {
      label: 'Renovación listing',
      libre_seller: 'No incluida',
      estandar: 'RD$579',
      verificado: 'Incluida',
    },
    { label: 'PricingAgent IA', libre_seller: '—', estandar: '1 / listing', verificado: '2 / mes' },
    { label: 'Analytics', libre_seller: false, estandar: false, verificado: true },
  ];

  const planColumns = ['libre_seller', 'estandar', 'verificado'] as const;
  const planHeaders = ['Libre', 'Estándar', 'Verificado'];

  return (
    <table className="w-full">
      <thead>
        <tr className="border-b">
          <th className="py-3 text-left text-sm font-medium">Función</th>
          {planHeaders.map((h, i) => (
            <th
              key={h}
              className={`py-3 text-center text-sm font-medium ${
                planColumns[i] === currentPlan ? 'text-primary' : ''
              }`}
            >
              {h}
              {planColumns[i] === currentPlan && (
                <Badge className="bg-primary ml-1 text-[10px]">Actual</Badge>
              )}
            </th>
          ))}
        </tr>
      </thead>
      <tbody>
        {features.map(row => (
          <tr key={row.label} className="border-b last:border-0">
            <td className="text-muted-foreground py-3 text-sm">{row.label}</td>
            {planColumns.map(col => {
              const val = row[col];
              return (
                <td key={col} className="py-3 text-center">
                  {typeof val === 'boolean' ? (
                    val ? (
                      <Check className="text-primary mx-auto h-4 w-4" />
                    ) : (
                      <X className="text-muted-foreground/30 mx-auto h-4 w-4" />
                    )
                  ) : (
                    <span
                      className={`text-sm font-medium ${
                        col === currentPlan ? 'text-primary' : 'text-foreground'
                      }`}
                    >
                      {val}
                    </span>
                  )}
                </td>
              );
            })}
          </tr>
        ))}
      </tbody>
    </table>
  );
}

// =============================================================================
// FAQ ITEM
// =============================================================================

function FaqItem({ q, a }: { q: string; a: string }) {
  return (
    <div className="border-b pb-4 last:border-0 last:pb-0">
      <h4 className="text-foreground mb-1 text-sm font-semibold">{q}</h4>
      <p className="text-muted-foreground text-sm">{a}</p>
    </div>
  );
}
