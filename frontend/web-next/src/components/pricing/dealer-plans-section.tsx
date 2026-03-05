/**
 * Dealer Plans Section (Client Component) — OKLA v2
 *
 * Renders dealer pricing plans with full feature comparison.
 * Shows 4-tier visibility-based plans (Libre/Visible/Pro/Elite).
 */

'use client';

import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { CheckCircle, X, Crown, Sparkles, Shield, Zap } from 'lucide-react';
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
      features: [
        { text: 'Publicaciones ilimitadas', included: true },
        { text: 'Hasta 10 fotos por vehículo', included: true },
        { text: 'Posición estándar en búsquedas', included: true },
        { text: '1 valoración IA gratis (PricingAgent)', included: true },
        { text: 'Vehículos destacados', included: false },
        { text: 'OKLA Coins mensuales', included: false },
        { text: 'Badge Verificado', included: false },
        { text: 'ChatAgent IA', included: false },
        { text: 'Dashboard Analytics', included: false },
      ],
      cta: 'Comenzar Gratis',
      highlighted: false,
      badge: null,
    },
    {
      name: 'VISIBLE',
      price: formatPrice(pricing.dealerVisible || 29),
      period: '/mes',
      icon: Zap,
      description: 'Más visibilidad y herramientas',
      features: [
        { text: 'Publicaciones ilimitadas', included: true },
        { text: 'Hasta 20 fotos por vehículo', included: true },
        { text: 'Prioridad media en búsquedas', included: true },
        { text: '3 destacados incluidos/mes', included: true },
        { text: '$15 en OKLA Coins/mes', included: true },
        { text: 'Badge Dealer Verificado', included: true },
        { text: '5 valoraciones PricingAgent/mes', included: true },
        { text: 'Dashboard básico', included: true },
        { text: 'ChatAgent IA', included: false },
      ],
      cta: 'Elegir Plan',
      highlighted: false,
      badge: null,
    },
    {
      name: 'PRO',
      price: formatPrice(pricing.dealerPro || 89),
      period: '/mes',
      icon: Sparkles,
      description: 'Herramientas avanzadas de venta',
      features: [
        { text: 'Publicaciones ilimitadas', included: true },
        { text: 'Hasta 30 fotos por vehículo', included: true },
        { text: 'Alta prioridad en búsquedas', included: true },
        { text: '10 destacados incluidos/mes', included: true },
        { text: '$45 en OKLA Coins/mes', included: true },
        { text: 'Badge Verificado Dorado', included: true },
        { text: 'ChatAgent IA: 500 conv web + WA/mes', included: true },
        { text: 'Agendamiento automático de citas', included: true },
        { text: 'PricingAgent ilimitado + Dashboard avanzado', included: true },
      ],
      cta: 'Elegir Plan',
      highlighted: true,
      badge: 'MÁS POPULAR',
    },
    {
      name: 'ÉLITE',
      price: formatPrice(pricing.dealerElite || 199),
      period: '/mes',
      icon: Crown,
      description: 'Para grandes concesionarios',
      features: [
        { text: 'Publicaciones ilimitadas', included: true },
        { text: 'Hasta 40 fotos + video tour', included: true },
        { text: 'Top prioridad en búsquedas', included: true },
        { text: '25 destacados incluidos/mes', included: true },
        { text: '$120 en OKLA Coins/mes', included: true },
        { text: 'Badge Verificado Premium', included: true },
        { text: 'ChatAgent IA ilimitado (web + WA)', included: true },
        { text: 'Citas auto + recordatorios WhatsApp', included: true },
        { text: 'PricingAgent ilimitado + informe PDF', included: true },
        { text: 'Dashboard completo + exportar + API', included: true },
      ],
      cta: 'Contactar Ventas',
      highlighted: false,
      badge: 'ENTERPRISE',
    },
  ];

  if (isLoading) {
    return (
      <div className="grid gap-8 md:grid-cols-2 lg:grid-cols-4">
        {[1, 2, 3, 4].map(i => (
          <Card key={i} className="border-border animate-pulse">
            <CardContent className="p-8">
              <div className="mb-4 h-6 w-24 rounded bg-gray-200" />
              <div className="mb-6 h-10 w-32 rounded bg-gray-200" />
              <div className="space-y-3">
                {[1, 2, 3, 4].map(j => (
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
    <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4">
      {plans.map((plan, index) => {
        const Icon = plan.icon;
        return (
          <Card
            key={index}
            className={`relative flex flex-col ${
              plan.highlighted ? 'border-2 border-[#00A870] shadow-xl scale-[1.02]' : 'border-border'
            }`}
          >
            {plan.badge && (
              <div className="absolute -top-3 left-1/2 -translate-x-1/2 z-10">
                <span className={`rounded-full px-3 py-1 text-xs font-semibold text-white ${
                  plan.badge === 'MÁS POPULAR' ? 'bg-[#00A870]' : 'bg-purple-600'
                }`}>
                  {plan.badge}
                </span>
              </div>
            )}
            <CardContent className="flex flex-1 flex-col p-6">
              <div className="mb-4">
                <div className="flex items-center gap-2 mb-1">
                  <Icon className={`h-5 w-5 ${plan.highlighted ? 'text-[#00A870]' : 'text-muted-foreground'}`} />
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
                      <CheckCircle className="h-4 w-4 mt-0.5 shrink-0 text-[#00A870]" />
                    ) : (
                      <X className="h-4 w-4 mt-0.5 shrink-0 text-muted-foreground/30" />
                    )}
                    {feature.text}
                  </li>
                ))}
              </ul>
              <Button
                asChild
                className={`w-full ${
                  plan.highlighted
                    ? 'bg-[#00A870] hover:bg-[#009663] text-white'
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
