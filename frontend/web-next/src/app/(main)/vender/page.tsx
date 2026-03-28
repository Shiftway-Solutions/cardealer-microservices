/**
 * Sell Your Car Landing Page (Server Component)
 *
 * Mostly static content for SEO with small client islands
 * for auth-dependent CTAs and dynamic pricing.
 *
 * Route: /vender
 */

import { Suspense } from 'react';
import type { Metadata } from 'next';
import { Card, CardContent } from '@/components/ui/card';
import {
  Camera,
  DollarSign,
  Users,
  Shield,
  CheckCircle,
  TrendingUp,
  Star,
  Zap,
} from 'lucide-react';
import {
  VenderKycBanner,
  HeroCTA,
  FinalCTA,
  VenderPricing,
  VenderSellerRedirect,
} from './vender-cta';
import { SELLER_STATS } from '@/lib/platform-stats';

// =============================================================================
// METADATA (SEO)
// =============================================================================

export const metadata: Metadata = {
  title: 'Vende tu Vehículo',
  description:
    'Vende tu vehículo al mejor precio en República Dominicana. Sin comisiones, sin intermediarios. Conecta con miles de compradores.',
  openGraph: {
    title: 'Vende tu Vehículo | OKLA',
    description: 'La forma más fácil de vender tu carro en RD',
  },
};

// =============================================================================
// STEPS DATA
// =============================================================================

const steps = [
  {
    number: '1',
    title: 'Crea tu anuncio',
    description: 'Ingresa los detalles de tu vehículo y sube fotos de calidad',
    icon: Camera,
  },
  {
    number: '2',
    title: 'Define tu precio',
    description: 'Te ayudamos con sugerencias basadas en el mercado dominicano',
    icon: DollarSign,
  },
  {
    number: '3',
    title: 'Recibe ofertas',
    description: 'Compradores interesados te contactarán directamente',
    icon: Users,
  },
  {
    number: '4',
    title: 'Cierra la venta',
    description: 'Coordina la entrega y recibe tu pago de forma segura',
    icon: Shield,
  },
];

// =============================================================================
// BENEFITS DATA
// =============================================================================

const benefits = [
  {
    title: 'Sin comisiones',
    description: 'Vende tu vehículo sin pagar comisión sobre la venta',
    icon: DollarSign,
  },
  {
    title: 'Rápido y fácil',
    description: 'Publica tu anuncio en menos de 5 minutos',
    icon: Zap,
  },
  {
    title: 'Máxima visibilidad',
    description: 'Tu anuncio llegará a miles de compradores en toda RD',
    icon: TrendingUp,
  },
  {
    title: 'Soporte 24/7',
    description: 'Nuestro equipo te ayuda en todo el proceso',
    icon: Star,
  },
];

// Stats imported from @/lib/platform-stats (single source of truth)
const stats = SELLER_STATS;

// =============================================================================
// MAIN COMPONENT
// =============================================================================

export default function VenderPage() {
  return (
    <div className="bg-background min-h-screen">
      {/* Seller redirect — if already registered, redirect to /cuenta (Client Island) */}
      <Suspense fallback={null}>
        <VenderSellerRedirect />
      </Suspense>

      {/* KYC Verification Banner (Client Island) */}
      <Suspense fallback={null}>
        <VenderKycBanner />
      </Suspense>

      {/* Hero Section */}
      <section className="from-primary via-primary/90 relative overflow-hidden bg-gradient-to-br to-[#007d52]">
        {/* Background pattern */}
        <div className="absolute inset-0 opacity-10">
          <div
            className="absolute inset-0"
            style={{
              backgroundImage: 'radial-gradient(circle at 1px 1px, white 1px, transparent 0)',
              backgroundSize: '40px 40px',
            }}
          />
        </div>

        <div className="relative container mx-auto px-4 py-16 lg:py-24">
          <div className="mx-auto max-w-3xl text-center">
            {/* Badge */}
            <div className="mb-6 inline-flex items-center gap-2 rounded-full bg-white/20 px-4 py-2 text-sm font-medium text-white backdrop-blur-sm">
              <Star className="h-4 w-4 fill-yellow-400 text-yellow-400" />
              La forma más fácil de vender tu carro en RD
            </div>

            {/* Title */}
            <h1 className="mb-6 text-4xl font-bold tracking-tight text-white md:text-5xl lg:text-6xl">
              Vende tu vehículo
              <br />
              <span className="text-yellow-300">al mejor precio</span>
            </h1>

            {/* Subtitle */}
            <p className="mx-auto mb-8 max-w-2xl text-lg text-white/90 md:text-xl">
              Conecta con miles de compradores en República Dominicana. Sin comisiones ocultas, sin
              intermediarios. Tú tienes el control.
            </p>

            {/* CTAs (Client Island - auth-dependent) */}
            <Suspense
              fallback={
                <div className="flex items-center justify-center gap-4">
                  <div className="h-12 w-48 animate-pulse rounded-lg bg-white/20" />
                  <div className="h-12 w-48 animate-pulse rounded-lg bg-white/10" />
                </div>
              }
            >
              <HeroCTA />
            </Suspense>

            {/* Trust indicators */}
            <div className="mt-10 flex flex-wrap items-center justify-center gap-6 text-sm text-white/80">
              <div className="flex items-center gap-2">
                <CheckCircle className="h-5 w-5 text-yellow-300" />
                <span>100% Gratis</span>
              </div>
              <div className="flex items-center gap-2">
                <CheckCircle className="h-5 w-5 text-yellow-300" />
                <span>Sin comisiones</span>
              </div>
              <div className="flex items-center gap-2">
                <CheckCircle className="h-5 w-5 text-yellow-300" />
                <span>Soporte local</span>
              </div>
            </div>
          </div>
        </div>

        {/* Wave divider */}
        <div className="absolute right-0 bottom-0 left-0">
          <svg
            viewBox="0 0 1440 120"
            className="h-16 w-full text-white md:h-24"
            preserveAspectRatio="none"
          >
            <path fill="currentColor" d="M0,64L1440,32L1440,120L0,120Z" />
          </svg>
        </div>
      </section>

      {/* Stats Section */}
      <section className="bg-card border-border border-b py-12">
        <div className="container mx-auto px-4">
          <div className="grid grid-cols-2 gap-8 md:grid-cols-4">
            {stats.map((stat, index) => (
              <div key={index} className="text-center">
                <div className="text-primary text-3xl font-bold md:text-4xl">{stat.value}</div>
                <div className="text-muted-foreground mt-1 text-sm">{stat.label}</div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* How It Works Section */}
      <section id="como-funciona" className="bg-muted/50 py-16 lg:py-24">
        <div className="container mx-auto px-4">
          <div className="mb-12 text-center">
            <h2 className="text-foreground mb-4 text-3xl font-bold md:text-4xl">¿Cómo funciona?</h2>
            <p className="text-muted-foreground mx-auto max-w-2xl text-lg">
              Vender tu vehículo en OKLA es rápido y sencillo. Sigue estos 4 pasos y conecta con
              compradores reales.
            </p>
          </div>

          <div className="grid gap-8 md:grid-cols-2 lg:grid-cols-4">
            {steps.map((step, index) => {
              const Icon = step.icon;
              return (
                <div key={index} className="relative">
                  {/* Connector line (hidden on last item and mobile) */}
                  {index < steps.length - 1 && (
                    <div className="bg-primary/20 absolute top-12 left-1/2 hidden h-0.5 w-full lg:block" />
                  )}

                  <Card className="bg-card relative">
                    <CardContent className="p-6 text-center">
                      {/* Step number badge */}
                      <div className="bg-primary/10 mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full">
                        <Icon className="text-primary h-8 w-8" />
                      </div>

                      {/* Step number */}
                      <div className="text-primary mb-2 text-sm font-semibold">
                        Paso {step.number}
                      </div>

                      {/* Title */}
                      <h3 className="text-foreground mb-2 text-lg font-bold">{step.title}</h3>

                      {/* Description */}
                      <p className="text-muted-foreground text-sm">{step.description}</p>
                    </CardContent>
                  </Card>
                </div>
              );
            })}
          </div>
        </div>
      </section>

      {/* Benefits Section */}
      <section className="bg-card py-16 lg:py-24">
        <div className="container mx-auto px-4">
          <div className="mb-12 text-center">
            <h2 className="text-foreground mb-4 text-3xl font-bold md:text-4xl">
              ¿Por qué vender en OKLA?
            </h2>
            <p className="text-muted-foreground mx-auto max-w-2xl text-lg">
              Somos la plataforma líder para compra y venta de vehículos en República Dominicana.
            </p>
          </div>

          <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4">
            {benefits.map((benefit, index) => {
              const Icon = benefit.icon;
              return (
                <Card key={index} className="bg-muted/50 border-none shadow-none">
                  <CardContent className="p-6">
                    <div className="bg-primary/10 mb-4 flex h-12 w-12 items-center justify-center rounded-lg">
                      <Icon className="text-primary h-6 w-6" />
                    </div>
                    <h3 className="text-foreground mb-2 text-lg font-bold">{benefit.title}</h3>
                    <p className="text-muted-foreground text-sm">{benefit.description}</p>
                  </CardContent>
                </Card>
              );
            })}
          </div>
        </div>
      </section>

      {/* Pricing Section */}
      <section className="bg-muted/50 py-16 lg:py-24">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-3xl">
            <div className="mb-12 text-center">
              <h2 className="text-foreground mb-4 text-3xl font-bold md:text-4xl">
                Planes de publicación
              </h2>
              <p className="text-muted-foreground mx-auto max-w-2xl text-lg">
                Elige el plan que mejor se adapte a tus necesidades.
              </p>
            </div>

            <Suspense
              fallback={
                <div className="grid gap-6 md:grid-cols-2">
                  <div className="h-96 animate-pulse rounded-lg bg-gray-200" />
                  <div className="h-96 animate-pulse rounded-lg bg-gray-200" />
                </div>
              }
            >
              <VenderPricing />
            </Suspense>
          </div>
        </div>
      </section>

      {/* Final CTA Section */}
      <section className="bg-primary py-16 lg:py-20">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-3xl text-center">
            <h2 className="mb-4 text-3xl font-bold text-white md:text-4xl">
              ¿Listo para vender tu vehículo?
            </h2>
            <p className="mx-auto mb-8 max-w-xl text-lg text-white/90">
              Publica tu anuncio hoy y conecta con compradores interesados en toda República
              Dominicana.
            </p>

            {/* CTA based on verification status (Client Island) */}
            <Suspense
              fallback={<div className="mx-auto h-12 w-48 animate-pulse rounded-lg bg-white/20" />}
            >
              <FinalCTA />
            </Suspense>
          </div>
        </div>
      </section>
    </div>
  );
}
