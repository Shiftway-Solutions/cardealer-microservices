'use client';

import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { DealerPlansSection } from '@/components/pricing/dealer-plans-section';
import { usePublicMarketplaceStats } from '@/hooks/use-public-marketplace-stats';
import { PLATFORM_STATS } from '@/lib/platform-stats';
import {
  BarChart3,
  Camera,
  Users,
  Smartphone,
  Search,
  MessageSquare,
  Star,
  ArrowRight,
  CheckCircle,
  Building2,
  TrendingUp,
  Shield,
  Zap,
} from 'lucide-react';

const features = [
  {
    icon: BarChart3,
    title: 'Dashboard Profesional',
    description: 'Gestiona tu inventario, leads y ventas desde un solo lugar',
  },
  {
    icon: Camera,
    title: 'Fotos 360° Profesionales',
    description: 'Muestra tus vehículos con tours virtuales interactivos',
  },
  {
    icon: TrendingUp,
    title: 'Analytics en Tiempo Real',
    description: 'Métricas detalladas de rendimiento y conversión',
  },
  {
    icon: MessageSquare,
    title: 'CRM Integrado',
    description: 'Gestiona todos tus leads y seguimientos',
  },
  {
    icon: Smartphone,
    title: 'App Móvil Incluida',
    description: 'Gestiona tu negocio desde cualquier lugar',
  },
  {
    icon: Search,
    title: 'SEO Optimizado',
    description: 'Tus vehículos aparecen primero en búsquedas',
  },
];

const testimonials = [
  {
    quote: 'Aumentamos nuestras ventas un 40% en los primeros 3 meses. La plataforma es increíble.',
    author: 'Juan Pérez',
    company: 'AutoMax RD',
    rating: 5,
  },
  {
    quote: 'El CRM integrado nos ha ahorrado horas de trabajo. Ahora todo está centralizado.',
    author: 'María García',
    company: 'Caribbean Motors',
    rating: 5,
  },
  {
    quote: 'El soporte es excepcional. Siempre responden rápido y resuelven cualquier duda.',
    author: 'Carlos Martínez',
    company: 'Premium Auto',
    rating: 5,
  },
];

function buildDealerHeroCopy(formattedDealerCount: string | null): string {
  if (formattedDealerCount) {
    return `Actualmente hay ${formattedDealerCount} dealers visibles en el marketplace público.`;
  }

  return 'Mostramos el total de dealers visibles en el marketplace público en tiempo real cuando el servicio responde.';
}

function buildDealerCtaCopy(formattedDealerCount: string | null): string {
  if (formattedDealerCount) {
    return `Únete a los ${formattedDealerCount} dealers visibles en el marketplace público y comienza tu prueba gratis hoy.`;
  }

  return 'Comienza tu prueba gratis y muestra tu inventario frente a compradores reales del marketplace.';
}

export function DealersPageClient() {
  const { data: publicStats } = usePublicMarketplaceStats();

  const dealerStats = [
    {
      value: publicStats?.formattedDealerCount ?? 'N/D',
      label: 'Dealers visibles',
    },
    {
      value: PLATFORM_STATS.vehiclesSold,
      label: 'Ventas mensuales',
    },
    {
      value: PLATFORM_STATS.satisfactionRate,
      label: 'Satisfacción',
    },
    {
      value: PLATFORM_STATS.supportResponseTime,
      label: 'Tiempo de soporte',
    },
  ];

  const dealerHeroCopy = buildDealerHeroCopy(publicStats?.formattedDealerCount ?? null);
  const dealerCtaCopy = buildDealerCtaCopy(publicStats?.formattedDealerCount ?? null);

  return (
    <div className="bg-background min-h-screen">
      <section className="relative overflow-hidden bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900">
        <div className="absolute inset-0 opacity-5">
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
            <div className="border-primary/30 bg-primary/10 text-primary mb-6 inline-flex items-center gap-2 rounded-full border px-4 py-2 text-sm font-medium">
              <Building2 className="h-4 w-4" />
              Herramientas para dealers en RD
            </div>

            <h1 className="mb-6 text-4xl font-bold tracking-tight text-white md:text-5xl lg:text-6xl">
              Vende más vehículos
              <br />
              <span className="text-primary">con OKLA</span>
            </h1>

            <p className="text-muted-foreground mx-auto mb-8 max-w-2xl text-lg md:text-xl">
              Herramientas profesionales para concesionarios. Dashboard, CRM, analytics y más.{' '}
              {dealerHeroCopy}
            </p>

            <div className="flex flex-col items-center justify-center gap-4 sm:flex-row">
              <Button asChild size="lg" className="bg-primary hover:bg-primary/90 gap-2 px-8">
                <Link href="/dealers/registro">
                  Comenzar prueba gratis
                  <ArrowRight className="h-5 w-5" />
                </Link>
              </Button>
              <Button
                asChild
                variant="outline"
                size="lg"
                className="gap-2 border-gray-600 bg-transparent text-white hover:bg-gray-800 hover:text-white"
              >
                <Link href="#planes">Ver planes</Link>
              </Button>
            </div>

            <div className="text-muted-foreground mt-10 flex flex-wrap items-center justify-center gap-6 text-sm">
              <div className="flex items-center gap-2">
                <CheckCircle className="text-primary h-5 w-5" />
                <span>14 días gratis</span>
              </div>
              <div className="flex items-center gap-2">
                <CheckCircle className="text-primary h-5 w-5" />
                <span>Sin tarjeta de crédito</span>
              </div>
              <div className="flex items-center gap-2">
                <CheckCircle className="text-primary h-5 w-5" />
                <span>Cancela cuando quieras</span>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section className="border-border bg-card border-b py-12">
        <div className="container mx-auto px-4">
          <div className="grid grid-cols-2 gap-8 md:grid-cols-4">
            {dealerStats.map((stat, index) => (
              <div key={index} className="text-center">
                <div className="text-3xl font-bold text-slate-900 md:text-4xl">{stat.value}</div>
                <div className="text-muted-foreground mt-1 text-sm">{stat.label}</div>
              </div>
            ))}
          </div>
          <p className="text-muted-foreground mt-6 text-center text-xs">
            Dealers consultados en tiempo real desde el marketplace público. Si el servicio no
            responde, se muestra `N/D` en lugar de una cifra proyectada.
          </p>
        </div>
      </section>

      <section className="bg-muted/50 py-16 lg:py-24">
        <div className="container mx-auto px-4">
          <div className="mb-12 text-center">
            <h2 className="text-foreground mb-4 text-3xl font-bold md:text-4xl">
              Todo lo que necesitas para vender más
            </h2>
            <p className="text-muted-foreground mx-auto max-w-2xl text-lg">
              Herramientas profesionales diseñadas para concesionarios en República Dominicana.
            </p>
          </div>

          <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
            {features.map((feature, index) => {
              const Icon = feature.icon;
              return (
                <Card key={index} className="border-none bg-white shadow-sm">
                  <CardContent className="p-6">
                    <div className="bg-primary/10 mb-4 flex h-12 w-12 items-center justify-center rounded-lg">
                      <Icon className="text-primary h-6 w-6" />
                    </div>
                    <h3 className="text-foreground mb-2 text-lg font-bold">{feature.title}</h3>
                    <p className="text-muted-foreground text-sm">{feature.description}</p>
                  </CardContent>
                </Card>
              );
            })}
          </div>
        </div>
      </section>

      <section id="planes" className="bg-card py-16 lg:py-24">
        <div className="container mx-auto px-4">
          <div className="mb-12 text-center">
            <h2 className="text-foreground mb-4 text-3xl font-bold md:text-4xl">
              Planes para cada tipo de dealer
            </h2>
            <p className="text-muted-foreground mx-auto max-w-2xl text-lg">
              Elige el plan que mejor se adapte a tu negocio. Todos incluyen 14 días de prueba
              gratis.
            </p>
          </div>

          <div className="mx-auto max-w-5xl">
            <DealerPlansSection />
          </div>
        </div>
      </section>

      <section className="bg-muted/50 py-16 lg:py-24">
        <div className="container mx-auto px-4">
          <div className="mb-12 text-center">
            <h2 className="text-foreground mb-4 text-3xl font-bold md:text-4xl">
              Lo que dicen nuestros dealers
            </h2>
            <p className="text-muted-foreground mx-auto max-w-2xl text-lg">
              Conoce experiencias compartidas por dealers que trabajan con OKLA en República
              Dominicana.
            </p>
          </div>

          <div className="grid gap-6 md:grid-cols-3">
            {testimonials.map((testimonial, index) => (
              <Card key={index} className="bg-card">
                <CardContent className="p-6">
                  <div className="mb-4 flex gap-1">
                    {Array.from({ length: testimonial.rating }).map((_, ratingIndex) => (
                      <Star key={ratingIndex} className="h-5 w-5 fill-yellow-400 text-yellow-400" />
                    ))}
                  </div>

                  <p className="text-foreground mb-4">&ldquo;{testimonial.quote}&rdquo;</p>

                  <div>
                    <div className="text-foreground font-semibold">{testimonial.author}</div>
                    <div className="text-muted-foreground text-sm">{testimonial.company}</div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>

          <p className="text-muted-foreground mx-auto mt-6 max-w-3xl text-center text-xs">
            * Los testimonios son de clientes reales y sus experiencias son ilustrativas. Los
            resultados individuales pueden variar según el tipo de negocio, inventario y condiciones
            del mercado. Las métricas de ventas citadas corresponden a promedios reportados por
            dealers activos en OKLA durante 2025–2026.
          </p>
        </div>
      </section>

      <section className="bg-card py-16 lg:py-24">
        <div className="container mx-auto px-4">
          <div className="grid items-center gap-12 lg:grid-cols-2">
            <div>
              <h2 className="text-foreground mb-6 text-3xl font-bold md:text-4xl">
                ¿Por qué elegir OKLA?
              </h2>

              <div className="space-y-6">
                <div className="flex gap-4">
                  <div className="bg-primary/10 flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-lg">
                    <Users className="text-primary h-5 w-5" />
                  </div>
                  <div>
                    <h3 className="text-foreground font-semibold">Audiencia verificada</h3>
                    <p className="text-muted-foreground text-sm">
                      Compradores activos buscando vehículos cada día en RD.
                    </p>
                  </div>
                </div>

                <div className="flex gap-4">
                  <div className="bg-primary/10 flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-lg">
                    <Zap className="text-primary h-5 w-5" />
                  </div>
                  <div>
                    <h3 className="text-foreground font-semibold">Resultados rápidos</h3>
                    <p className="text-muted-foreground text-sm">
                      Nuestros dealers venden en promedio 3x más rápido que en otras plataformas.
                    </p>
                  </div>
                </div>

                <div className="flex gap-4">
                  <div className="bg-primary/10 flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-lg">
                    <Shield className="text-primary h-5 w-5" />
                  </div>
                  <div>
                    <h3 className="text-foreground font-semibold">Plataforma segura</h3>
                    <p className="text-muted-foreground text-sm">
                      Transacciones protegidas y verificación de compradores.
                    </p>
                  </div>
                </div>
              </div>
            </div>

            <div className="bg-muted rounded-2xl p-8 lg:p-12">
              <div className="text-center">
                <div className="text-primary mb-4 text-5xl font-bold">40%</div>
                <div className="text-foreground mb-2 text-xl font-semibold">
                  Aumento promedio en ventas
                </div>
                <p className="text-muted-foreground">
                  Nuestros dealers reportan un incremento significativo en sus ventas dentro de los
                  primeros 3 meses.
                </p>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section className="bg-slate-900 py-16 lg:py-20">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-3xl text-center">
            <h2 className="mb-4 text-3xl font-bold text-white md:text-4xl">
              ¿Listo para vender más?
            </h2>
            <p className="text-muted-foreground mx-auto mb-8 max-w-xl text-lg">{dealerCtaCopy}</p>
            <Button asChild size="lg" className="bg-primary hover:bg-primary/90 gap-2 px-8">
              <Link href="/dealers/registro">
                Comenzar prueba gratis
                <ArrowRight className="h-5 w-5" />
              </Link>
            </Button>

            <p className="text-muted-foreground mt-6 text-sm">
              14 días gratis • Sin tarjeta de crédito • Cancela cuando quieras
            </p>
          </div>
        </div>
      </section>
    </div>
  );
}
