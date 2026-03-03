/**
 * Why Choose OKLA Section
 *
 * Professional value proposition section with icons and animations
 * Highlights key differentiators of the platform
 */

'use client';

import {
  Shield,
  TrendingUp,
  MessageCircle,
  Camera,
  CreditCard,
  Clock,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { useInView } from '@/hooks/use-in-view';
import type { LucideIcon } from 'lucide-react';

// =============================================================================
// TYPES
// =============================================================================

interface ValueProp {
  icon: LucideIcon;
  title: string;
  description: string;
  highlight?: string;
  color: string;
}

interface WhyChooseUsProps {
  className?: string;
  variant?: 'grid' | 'alternating' | 'cards';
}

// =============================================================================
// VALUE PROPOSITIONS
// =============================================================================

const VALUE_PROPS: ValueProp[] = [
  {
    icon: Shield,
    title: 'Vendedores Verificados',
    description:
      'Todos nuestros dealers y vendedores pasan por un proceso de verificación riguroso. Tu seguridad es nuestra prioridad.',
    highlight: '100% Verificados',
    color: 'from-blue-500 to-blue-600',
  },
  {
    icon: Camera,
    title: 'Fotos Profesionales',
    description:
      'Herramientas para mejorar las fotos de tu vehículo y atraer más compradores interesados.',
    highlight: 'Alta Calidad',
    color: 'from-purple-500 to-purple-600',
  },
  {
    icon: TrendingUp,
    title: 'Mejor Precio del Mercado',
    description:
      'Análisis de mercado en tiempo real para que obtengas el mejor precio, ya sea que compres o vendas.',
    highlight: 'Precios Justos',
    color: 'from-primary to-primary',
  },
  {
    icon: MessageCircle,
    title: 'Contacto Directo',
    description:
      'Habla directamente con vendedores sin intermediarios. Sin comisiones ocultas ni sorpresas.',
    highlight: '0% Comisión',
    color: 'from-amber-500 to-orange-600',
  },
  {
    icon: Clock,
    title: 'Vende en 7 Días',
    description:
      'Nuestros usuarios venden sus vehículos en promedio en solo 7 días. El marketplace más activo de RD.',
    highlight: '7 Días Promedio',
    color: 'from-pink-500 to-rose-600',
  },
  {
    icon: CreditCard,
    title: 'Pagos Seguros',
    description:
      'Integración con los principales bancos dominicanos y procesadores de pago internacionales.',
    highlight: 'Banco Popular & Stripe',
    color: 'from-cyan-500 to-blue-600',
  },
];

// =============================================================================
// GRID VARIANT
// =============================================================================

function GridVariant({ className }: { className?: string }) {
  const { ref, inView } = useInView();
  return (
    <div
      ref={ref}
      className={cn('grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3 lg:gap-8', className)}
    >
      {VALUE_PROPS.map((prop, index) => (
        <div
          key={prop.title}
          className={cn(
            'group border-border bg-card relative overflow-hidden rounded-2xl border p-6 shadow-sm transition-all duration-500 hover:border-transparent hover:shadow-xl lg:p-8',
            inView ? 'translate-y-0 opacity-100' : 'translate-y-5 opacity-0'
          )}
          style={{ transitionDelay: `${index * 100}ms` }}
        >
          {/* Background Gradient on Hover */}
          <div
            className={cn(
              'absolute inset-0 bg-gradient-to-br opacity-0 transition-opacity duration-300 group-hover:opacity-5',
              prop.color
            )}
          />

          {/* Icon */}
          <div
            className={cn(
              'mb-5 flex h-14 w-14 items-center justify-center rounded-xl bg-gradient-to-br shadow-lg transition-transform duration-300 group-hover:scale-110',
              prop.color
            )}
          >
            <prop.icon className="h-7 w-7 text-white" />
          </div>

          {/* Highlight Badge */}
          {prop.highlight && (
            <span className="bg-muted text-muted-foreground mb-3 inline-block rounded-full px-3 py-1 text-xs font-semibold tracking-wide">
              {prop.highlight}
            </span>
          )}

          {/* Title */}
          <h3 className="text-foreground group-hover:text-primary mb-3 text-xl leading-snug font-bold tracking-tight transition-colors">
            {prop.title}
          </h3>

          {/* Description */}
          <p className="text-muted-foreground leading-relaxed">{prop.description}</p>

          {/* Decorative Corner */}
          <div
            className={cn(
              'absolute -right-2 -bottom-2 h-20 w-20 rounded-full bg-gradient-to-br opacity-10 blur-xl',
              prop.color
            )}
          />
        </div>
      ))}
    </div>
  );
}

// =============================================================================
// ALTERNATING VARIANT
// =============================================================================

function AlternatingVariant({ className }: { className?: string }) {
  const { ref, inView } = useInView();
  return (
    <div ref={ref} className={cn('space-y-16 lg:space-y-24', className)}>
      {VALUE_PROPS.slice(0, 4).map((prop, index) => (
        <div
          key={prop.title}
          className={cn(
            'flex flex-col items-center gap-8 transition-all duration-600 lg:flex-row lg:gap-16',
            index % 2 === 1 && 'lg:flex-row-reverse',
            inView ? 'translate-y-0 opacity-100' : 'translate-y-8 opacity-0'
          )}
          style={{ transitionDelay: `${index * 150}ms` }}
        >
          {/* Visual */}
          <div className="relative flex-1">
            <div
              className={cn(
                'mx-auto aspect-square w-full max-w-md rounded-3xl bg-gradient-to-br p-1',
                prop.color
              )}
            >
              <div className="bg-background flex h-full w-full items-center justify-center rounded-3xl">
                <prop.icon
                  className={cn(
                    'h-32 w-32 bg-gradient-to-br bg-clip-text',
                    prop.color.replace('from-', 'text-').replace(/to-.+/, '')
                  )}
                />
              </div>
            </div>
            {/* Decorative Dots */}
            <div className="bg-muted absolute top-8 left-8 -z-10 h-full w-full rounded-3xl" />
          </div>

          {/* Content */}
          <div className="flex-1 text-center lg:text-left">
            {prop.highlight && (
              <span
                className={cn(
                  'mb-4 inline-block rounded-full bg-gradient-to-r px-4 py-1.5 text-sm font-semibold text-white',
                  prop.color
                )}
              >
                {prop.highlight}
              </span>
            )}
            <h3 className="text-foreground mb-4 text-3xl font-bold lg:text-4xl">{prop.title}</h3>
            <p className="text-muted-foreground mx-auto max-w-lg text-lg leading-relaxed lg:mx-0">
              {prop.description}
            </p>
          </div>
        </div>
      ))}
    </div>
  );
}

// =============================================================================
// CARDS VARIANT (COMPACT)
// =============================================================================

function CardsVariant({ className }: { className?: string }) {
  const { ref, inView } = useInView();
  return (
    <div
      ref={ref}
      className={cn('grid grid-cols-2 gap-4 md:grid-cols-3 lg:grid-cols-6', className)}
    >
      {VALUE_PROPS.map((prop, index) => (
        <div
          key={prop.title}
          className={cn(
            'group border-border bg-card flex flex-col items-center rounded-2xl border p-4 text-center shadow-sm transition-all duration-500 hover:-translate-y-2 hover:shadow-lg lg:p-6',
            inView ? 'scale-100 opacity-100' : 'scale-90 opacity-0'
          )}
          style={{ transitionDelay: `${index * 50}ms` }}
        >
          {/* Icon */}
          <div
            className={cn(
              'mb-3 flex h-12 w-12 items-center justify-center rounded-xl bg-gradient-to-br transition-transform group-hover:scale-110',
              prop.color
            )}
          >
            <prop.icon className="h-6 w-6 text-white" />
          </div>

          {/* Title */}
          <h4 className="text-foreground group-hover:text-primary mb-1 text-sm leading-snug font-semibold tracking-tight transition-colors lg:text-base">
            {prop.title}
          </h4>

          {/* Highlight */}
          {prop.highlight && (
            <span className="text-muted-foreground text-xs font-medium">{prop.highlight}</span>
          )}
        </div>
      ))}
    </div>
  );
}

// =============================================================================
// MAIN COMPONENT
// =============================================================================

export function WhyChooseUs({ className, variant = 'grid' }: WhyChooseUsProps) {
  const { ref: headerRef, inView: headerInView } = useInView();
  const VariantComponent = {
    grid: GridVariant,
    alternating: AlternatingVariant,
    cards: CardsVariant,
  }[variant];

  return (
    <section id="por-que-okla" className={cn('py-16 lg:py-24', className)}>
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div
          ref={headerRef}
          className={cn(
            'mb-12 text-center transition-all duration-600 lg:mb-16',
            headerInView ? 'translate-y-0 opacity-100' : 'translate-y-5 opacity-0'
          )}
        >
          <span className="bg-primary/10 text-primary mb-4 inline-block rounded-full px-4 py-1.5 text-sm font-semibold tracking-wide">
            ¿Por qué OKLA?
          </span>
          <h2 className="text-foreground mb-4 text-3xl leading-tight font-bold tracking-tight lg:text-5xl">
            La forma más inteligente de
            <br />
            <span className="text-primary">comprar y vender</span> vehículos
          </h2>
          <p className="text-muted-foreground mx-auto max-w-2xl text-lg leading-relaxed">
            Miles de dominicanos confían en OKLA para encontrar su próximo vehículo o vender el suyo
            de forma rápida y segura.
          </p>
        </div>

        {/* Content */}
        <VariantComponent className={className} />
      </div>
    </section>
  );
}

export default WhyChooseUs;
