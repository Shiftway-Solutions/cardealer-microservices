/**
 * Dealer Plans Section (Client Component) — OKLA v3
 *
 * Renders dealer pricing plans with full feature comparison.
 * Shows 6-tier visibility-based plans (Libre/Visible/Starter/Pro/Elite/Enterprise).
 */

'use client';

import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { CheckCircle, X, Crown, Sparkles, Shield, Zap, Star, Building2 } from 'lucide-react';
import { usePlatformPricing } from '@/hooks/use-platform-pricing';

export function DealerPlansSection() {
  const { pricing, formatPrice, isLoading } = usePlatformPricing();

  const plans = [
    {
      name: 'LIBRE',
      price: '$0',
      period: '/mes',
      icon: Shield,
      description: 'Para empezar sin costo',
      color: 'gray',
      features: [
        { text: 'Publicaciones ilimitadas', included: true },
        { text: 'Hasta 5 fotos por vehículo', included: true },
        { text: 'Posición estándar en búsquedas', included: true },
        { text: '1 valoración PricingAgent gratis', included: true },
        { text: 'Destacados/mes', included: false },
        { text: 'OKLA Coins', included: false },
        { text: 'Badge Verificado', included: false },
        { text: 'ChatAgent IA', included: false },
      ],
      cta: 'Comenzar Gratis',
      highlighted: false,
      badge: null,
    },
    {
      name: 'VISIBLE',
      price: formatPrice(pricing.dealerVisible || 1682),
      period: '/mes',
      icon: Zap,
      description: 'Más visibilidad y herramientas',
      color: 'blue',
      features: [
        { text: 'Hasta 10 fotos por vehículo', included: true },
        { text: 'Prioridad media en búsquedas', included: true },
        { text: '3 destacados/mes', included: true },
        { text: '$15 OKLA Coins/mes', included: true },
        { text: '🔵 Badge Verificado', included: true },
        { text: '5 valoraciones PricingAgent/mes', included: true },
        { text: 'Dashboard básico', included: true },
        { text: 'ChatAgent IA', included: false },
      ],
      cta: 'Elegir Plan',
      highlighted: false,
      badge: null,
    },
    {
      name: 'STARTER',
      price: formatPrice(pricing.dealerStarter || 3422),
      period: '/mes',
      icon: Star,
      description: 'Primer paso con ChatAgent',
      color: 'teal',
      features: [
        { text: 'Hasta 12 fotos por vehículo', included: true },
        { text: 'Alta prioridad en búsquedas', included: true },
        { text: '5 destacados/mes', included: true },
        { text: '$30 OKLA Coins/mes', included: true },
        { text: '🔵 Badge Verificado+', included: true },
        { text: 'ChatAgent Web 100 conv/mes', included: true },
        { text: 'ChatAgent WhatsApp 100 conv/mes', included: true },
        { text: 'Overage $0.10/conv adicional', included: true },
      ],
      cta: 'Elegir Plan',
      highlighted: false,
      badge: null,
    },
    {
      name: 'PRO',
      price: formatPrice(pricing.dealerPro || 5742),
      period: '/mes',
      icon: Sparkles,
      description: 'Herramientas avanzadas de venta',
      color: 'purple',
      features: [
        { text: 'Hasta 15 fotos por vehículo', included: true },
        { text: 'Alta prioridad en búsquedas', included: true },
        { text: '10 destacados/mes', included: true },
        { text: '$45 OKLA Coins/mes', included: true },
        { text: '🥇 Badge Verificado Dorado', included: true },
        { text: 'ChatAgent Web 500 conv/mes', included: true },
        { text: 'ChatAgent WhatsApp 500 conv/mes', included: true },
        { text: 'Agendamiento automático', included: true },
        { text: 'PricingAgent ilimitado', included: true },
        { text: 'Dashboard avanzado', included: true },
      ],
      cta: 'Elegir Plan',
      highlighted: true,
      badge: 'MÁS POPULAR',
    },
    {
      name: 'ÉLITE',
      price: formatPrice(pricing.dealerElite || 20242),
      period: '/mes',
      icon: Crown,
      description: 'Para grandes concesionarios',
      color: 'amber',
      features: [
        { text: 'Hasta 20 fotos + video tour', included: true },
        { text: 'Top prioridad en búsquedas', included: true },
        { text: '25 destacados/mes', included: true },
        { text: '$120 OKLA Coins/mes', included: true },
        { text: '💎 Badge Verificado Premium', included: true },
        { text: 'ChatAgent Web 2,000 conv/mes', included: true },
        { text: 'ChatAgent WhatsApp 2,000 conv/mes', included: true },
        { text: 'Citas + recordatorios WhatsApp', included: true },
        { text: 'PricingAgent ilimitado + PDF', included: true },
        { text: 'Dashboard completo + exportar', included: true },
        { text: 'Gerente de cuenta dedicado', included: true },
      ],
      cta: 'Contactar Ventas',
      highlighted: false,
      badge: 'RECOMENDADO',
    },
    {
      name: 'ENTERPRISE',
      price: formatPrice(pricing.dealerEnterprise || 34999),
      period: '/mes',
      icon: Building2,
      description: 'Grupos automotrices y franquicias',
      color: 'slate',
      features: [
        { text: 'Hasta 20 fotos + video tour', included: true },
        { text: 'Máxima visibilidad en búsquedas', included: true },
        { text: '50 destacados/mes', included: true },
        { text: '$300 OKLA Coins/mes', included: true },
        { text: '👑 Badge Enterprise', included: true },
        { text: 'ChatAgent SIN LÍMITE', included: true },
        { text: 'Agendamiento + CRM + recordatorios WA', included: true },
        { text: 'Acceso completo a API OKLA', included: true },
        { text: 'Dashboard + API + reportes custom', included: true },
        { text: 'Manager dedicado + SLA garantizado', included: true },
      ],
      cta: 'Contactar Ventas',
      highlighted: false,
      badge: 'ENTERPRISE',
    },
  ];

  if (isLoading) {
    return (
      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
        {[1, 2, 3, 4, 5, 6].map(i => (
          <Card key={i} className="border-border animate-pulse">
            <CardContent className="p-6">
              <div className="mb-4 h-6 w-24 rounded bg-gray-200" />
              <div className="mb-6 h-10 w-32 rounded bg-gray-200" />
              <div className="space-y-3">
                {[1, 2, 3, 4, 5].map(j => (
                  <div key={j} className="h-4 w-full rounded bg-gray-200" />
                ))}
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    );
  }

  return (
    <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
      {plans.map((plan, index) => {
        const Icon = plan.icon;
        return (
          <Card
            key={index}
            className={`relative flex flex-col ${
              plan.highlighted ? 'border-primary scale-[1.02] border-2 shadow-xl' : 'border-border'
            }`}
          >
            {plan.badge && (
              <div className="absolute -top-3 left-1/2 z-10 -translate-x-1/2">
                <span
                  className={`rounded-full px-3 py-1 text-xs font-semibold text-white ${
                    plan.badge === 'MÁS POPULAR'
                      ? 'bg-primary'
                      : plan.badge === 'RECOMENDADO'
                        ? 'bg-amber-500'
                        : 'bg-slate-700'
                  }`}
                >
                  {plan.badge}
                </span>
              </div>
            )}
            <CardContent className="flex flex-1 flex-col p-6">
              <div className="mb-4">
                <div className="mb-1 flex items-center gap-2">
                  <Icon
                    className={`h-5 w-5 ${plan.highlighted ? 'text-primary' : 'text-muted-foreground'}`}
                  />
                  <h3 className="text-foreground text-lg font-bold">{plan.name}</h3>
                </div>
                <p className="text-muted-foreground text-sm">{plan.description}</p>
              </div>
              <div className="mb-5">
                <span className="text-foreground text-3xl font-bold">{plan.price}</span>
                <span className="text-muted-foreground text-sm">{plan.period}</span>
              </div>
              <ul className="mb-6 flex-1 space-y-2.5">
                {plan.features.map((feature, featureIndex) => (
                  <li
                    key={featureIndex}
                    className={`flex items-start gap-2 text-sm ${
                      feature.included ? 'text-foreground' : 'text-muted-foreground/50 line-through'
                    }`}
                  >
                    {feature.included ? (
                      <CheckCircle className="text-primary mt-0.5 h-4 w-4 shrink-0" />
                    ) : (
                      <X className="text-muted-foreground/30 mt-0.5 h-4 w-4 shrink-0" />
                    )}
                    {feature.text}
                  </li>
                ))}
              </ul>
              <Button
                asChild
                className={`w-full ${
                  plan.highlighted
                    ? 'bg-primary hover:bg-primary/90 text-white'
                    : 'bg-secondary text-secondary-foreground hover:bg-secondary/80'
                }`}
              >
                <Link href="/dealers/registro">{plan.cta}</Link>
              </Button>
            </CardContent>
          </Card>
        );
      })}
    </div>
  );
}
