/**
 * Seller Subscription Page
 *
 * Manages subscription plan for individual sellers.
 * Shows current plan, usage, available plans, and upgrade/downgrade options.
 */

'use client';

import { useState } from 'react';
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

// =============================================================================
// HELPERS
// =============================================================================

const formatPrice = (price: number) => {
  return new Intl.NumberFormat('es-DO', {
    style: 'currency',
    currency: 'DOP',
    maximumFractionDigits: 0,
  }).format(price);
};

// Plan features description
function getPlanFeatures() {
  return {
    gratis: [
      '1 publicación activa',
      'Hasta 10 fotos por vehículo',
      'Duración: 30 días',
      'Contacto por WhatsApp',
      'Panel de vendedor básico',
    ],
    premium: [
      'Hasta 5 publicaciones activas',
      'Hasta 30 fotos por vehículo',
      'Publicaciones permanentes',
      'Prioridad en búsquedas',
      'Badge de vendedor verificado',
      '2 publicaciones destacadas/mes',
      'Estadísticas detalladas',
      'Boosts disponibles',
      'Compartir en redes con preview',
    ],
    pro: [
      'Hasta 15 publicaciones activas',
      'Hasta 50 fotos por vehículo',
      'Publicaciones permanentes',
      'Máxima prioridad en búsquedas',
      'Badge de vendedor verificado',
      '5 publicaciones destacadas/mes',
      'Analytics avanzados',
      'Boosts disponibles',
      'Compartir en redes con preview',
      'Alertas automáticas de baja de precio',
      'Soporte prioritario',
    ],
  };
}

// =============================================================================
// PAGE COMPONENT
// =============================================================================

export default function SellerSubscriptionPage() {
  useAuth();
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
      // In a real implementation, this would call the subscription API
      toast.success(
        newPlan === 'gratis'
          ? 'Plan cambiado a Gratis. Los cambios aplican al final del período.'
          : `¡Bienvenido al plan ${newPlan === 'premium' ? 'Premium' : 'PRO'}! Serás redirigido al checkout.`
      );
    } catch {
      toast.error('Error al cambiar el plan. Intenta de nuevo.');
    } finally {
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
            <BarChart3 className="h-5 w-5 text-[#00A870]" />
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
            <PlanUsageBar
              current={usage.featuredUsed}
              max={featuredPerMonth}
              label="Destacadas este mes"
            />
          </div>
        </CardContent>
      </Card>

      {/* ── Plan Cards ─────────────────────────────────────── */}
      <div className="grid gap-6 md:grid-cols-3">
        {/* GRATIS */}
        <PlanCard
          name="Gratis"
          planKey="gratis"
          price={0}
          period=""
          description="Para vendedores ocasionales"
          features={plans.gratis}
          icon={Zap}
          isCurrent={currentPlan === 'gratis'}
          isChanging={changingPlan === 'gratis'}
          onSelect={() => handleChangePlan('gratis')}
          currentPlan={currentPlan}
        />

        {/* PREMIUM */}
        <PlanCard
          name="Premium"
          planKey="premium"
          price={pricing.sellerPremium}
          period="/mes"
          description="Vende más rápido"
          features={plans.premium}
          icon={Sparkles}
          isCurrent={currentPlan === 'premium'}
          isPopular
          isChanging={changingPlan === 'premium'}
          onSelect={() => handleChangePlan('premium')}
          currentPlan={currentPlan}
        />

        {/* PRO */}
        <PlanCard
          name="PRO"
          planKey="pro"
          price={pricing.sellerProPlan}
          period="/mes"
          description="Máxima visibilidad y herramientas"
          features={plans.pro}
          icon={Crown}
          isCurrent={currentPlan === 'pro'}
          isChanging={changingPlan === 'pro'}
          onSelect={() => handleChangePlan('pro')}
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
      <Card className="border-[#00A870]/20 bg-gradient-to-r from-[#00A870]/5 to-emerald-50">
        <CardContent className="flex flex-col items-center gap-4 p-6 sm:flex-row sm:justify-between">
          <div className="flex items-center gap-4">
            <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-[#00A870]/10">
              <Crown className="h-6 w-6 text-[#00A870]" />
            </div>
            <div>
              <h3 className="font-semibold">¿Eres un negocio o concesionario?</h3>
              <p className="text-muted-foreground text-sm">
                Los planes Dealer ofrecen vehículos ilimitados, CRM, leads, analytics y más.
              </p>
            </div>
          </div>
          <Link href="/dealers/registro">
            <Button variant="outline" className="gap-2 border-[#00A870] text-[#00A870]">
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
            a="El badge de verificado solo está disponible en planes Premium y PRO. Si bajas al plan Gratis, el badge se desactiva."
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
          ? 'border-2 border-[#00A870] shadow-lg shadow-[#00A870]/10'
          : isCurrent
            ? 'border-2 border-blue-400'
            : 'border-border'
      }`}
    >
      {isPopular && (
        <div className="absolute -top-3 left-1/2 -translate-x-1/2">
          <Badge className="bg-[#00A870] text-white">MÁS POPULAR</Badge>
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
              isPopular ? 'bg-[#00A870]/10' : 'bg-muted'
            }`}
          >
            <Icon className={`h-5 w-5 ${isPopular ? 'text-[#00A870]' : 'text-muted-foreground'}`} />
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
              <Check className="mt-0.5 h-4 w-4 flex-shrink-0 text-[#00A870]" />
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
              isUpgrade ? 'bg-[#00A870] hover:bg-[#009663]' : 'bg-gray-600 hover:bg-gray-700'
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
    case 'gratis':
      return 0;
    case 'premium':
      return 1;
    case 'pro':
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
    { label: 'Publicaciones activas', gratis: '1', premium: '5', pro: '15' },
    { label: 'Fotos por vehículo', gratis: '10', premium: '30', pro: '50' },
    { label: 'Duración publicación', gratis: '30 días', premium: 'Permanente', pro: 'Permanente' },
    { label: 'Contacto WhatsApp', gratis: true, premium: true, pro: true },
    { label: 'Prioridad en búsquedas', gratis: false, premium: true, pro: true },
    { label: 'Badge verificado', gratis: false, premium: true, pro: true },
    { label: 'Destacadas/mes', gratis: '0', premium: '2', pro: '5' },
    { label: 'Estadísticas detalladas', gratis: false, premium: true, pro: true },
    { label: 'Boosts disponibles', gratis: false, premium: true, pro: true },
    { label: 'Compartir en redes', gratis: false, premium: true, pro: true },
    { label: 'Alertas de baja de precio', gratis: false, premium: false, pro: true },
    { label: 'Soporte prioritario', gratis: false, premium: false, pro: true },
  ];

  const planColumns = ['gratis', 'premium', 'pro'] as const;
  const planHeaders = ['Gratis', 'Premium', 'PRO'];

  return (
    <table className="w-full">
      <thead>
        <tr className="border-b">
          <th className="py-3 text-left text-sm font-medium">Función</th>
          {planHeaders.map((h, i) => (
            <th
              key={h}
              className={`py-3 text-center text-sm font-medium ${
                planColumns[i] === currentPlan ? 'text-[#00A870]' : ''
              }`}
            >
              {h}
              {planColumns[i] === currentPlan && (
                <Badge className="ml-1 bg-[#00A870] text-[10px]">Actual</Badge>
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
                      <Check className="mx-auto h-4 w-4 text-[#00A870]" />
                    ) : (
                      <X className="text-muted-foreground/30 mx-auto h-4 w-4" />
                    )
                  ) : (
                    <span
                      className={`text-sm font-medium ${
                        col === currentPlan ? 'text-[#00A870]' : 'text-foreground'
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
