# 🏪 Landing Page y Pricing de Dealers

> **Tiempo estimado:** 60 minutos  
> **Prerrequisitos:** Sistema de autenticación, componentes base  
> **Páginas cubiertas:** DealerLandingPage, DealerPricingPage

---

## 📋 OBJETIVO

Implementar las páginas de captación de dealers:

- Landing page con propuesta de valor
- Página de planes y precios
- Comparación de features por plan
- CTAs claros para registro

---

## 🎨 ESTRUCTURA VISUAL

### Landing Page para Dealers

```
┌─────────────────────────────────────────────────────────────────┐
│ NAVBAR (con CTA "Soy Dealer")                                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  HERO SECTION                                                   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                                                          │   │
│  │    Vende más vehículos con OKLA                         │   │
│  │    La plataforma #1 de República Dominicana             │   │
│  │                                                          │   │
│  │    [Ver Planes]  [Registrarme Gratis]                   │   │
│  │                                                          │   │
│  │    ✓ +10,000 visitantes mensuales                       │   │
│  │    ✓ Herramientas profesionales                         │   │
│  │    ✓ Soporte dedicado                                   │   │
│  │                                                          │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  ESTADÍSTICAS                                                   │
│  ┌───────────┬───────────┬───────────┬───────────┐             │
│  │  +500     │  +10K     │  95%      │  24h      │             │
│  │  Dealers  │  Ventas   │  Satisf.  │  Soporte  │             │
│  └───────────┴───────────┴───────────┴───────────┘             │
│                                                                 │
│  FEATURES                                                       │
│  ┌───────────────┐ ┌───────────────┐ ┌───────────────┐         │
│  │ 📊 Dashboard   │ │ 📸 Fotos 360° │ │ 📈 Analytics  │         │
│  │ Profesional   │ │ Profesionales │ │ en tiempo    │         │
│  │               │ │               │ │ real         │         │
│  └───────────────┘ └───────────────┘ └───────────────┘         │
│                                                                 │
│  ┌───────────────┐ ┌───────────────┐ ┌───────────────┐         │
│  │ 💬 CRM        │ │ 📱 App Móvil  │ │ 🔍 SEO        │         │
│  │ Integrado     │ │ Incluida      │ │ Optimizado   │         │
│  └───────────────┘ └───────────────┘ └───────────────┘         │
│                                                                 │
│  TESTIMONIALES                                                  │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ "Aumentamos nuestras ventas un 40% en 3 meses"          │   │
│  │ - Juan Pérez, AutoMax RD                                │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  CTA FINAL                                                      │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │           Empieza tu prueba gratis de 14 días           │   │
│  │                  [Comenzar Ahora]                        │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│ FOOTER                                                          │
└─────────────────────────────────────────────────────────────────┘
```

### Pricing Page

```
┌─────────────────────────────────────────────────────────────────┐
│ HEADER                                                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│              Planes diseñados para tu negocio                   │
│       Elige el plan que mejor se adapte a tus necesidades       │
│                                                                 │
│  [Mensual]  [Anual - Ahorra 20%]                               │
│                                                                 │
│  ┌─────────────┐  ┌─────────────────┐  ┌─────────────┐         │
│  │   STARTER   │  │  PROFESIONAL    │  │  ENTERPRISE │         │
│  │             │  │   ⭐ POPULAR     │  │             │         │
│  │   $49/mes   │  │    $99/mes      │  │   $249/mes  │         │
│  │             │  │                 │  │             │         │
│  │ 15 listings │  │  50 listings    │  │  Ilimitado  │         │
│  │ 1 ubicación │  │  3 ubicaciones  │  │  Ilimitado  │         │
│  │ Analytics   │  │  Analytics Pro  │  │  Analytics  │         │
│  │ básico      │  │  + CRM          │  │  + API      │         │
│  │             │  │  + Boost        │  │  + Manager  │         │
│  │             │  │                 │  │  dedicado   │         │
│  │  [Elegir]   │  │   [Elegir]      │  │  [Contactar]│         │
│  └─────────────┘  └─────────────────┘  └─────────────┘         │
│                                                                 │
│  COMPARACIÓN DE FEATURES                                        │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ Feature          │ Starter │ Pro    │ Enterprise        │   │
│  │──────────────────┼─────────┼────────┼──────────────────│   │
│  │ Listings         │ 15      │ 50     │ Ilimitado         │   │
│  │ Ubicaciones      │ 1       │ 3      │ Ilimitado         │   │
│  │ Fotos por auto   │ 20      │ 50     │ Ilimitado         │   │
│  │ Video 360°       │ ❌      │ ✅     │ ✅                │   │
│  │ CRM integrado    │ ❌      │ ✅     │ ✅                │   │
│  │ API access       │ ❌      │ ❌     │ ✅                │   │
│  │ Manager dedicado │ ❌      │ ❌     │ ✅                │   │
│  │ White label      │ ❌      │ ❌     │ ✅                │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  FAQ                                                            │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ ▼ ¿Puedo cambiar de plan?                               │   │
│  │ ▼ ¿Hay período de prueba?                               │   │
│  │ ▼ ¿Cómo funciona la facturación?                        │   │
│  │ ▼ ¿Qué métodos de pago aceptan?                         │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 IMPLEMENTACIÓN

### 1. Landing Page para Dealers

```typescript
// filepath: src/app/(marketing)/dealers/page.tsx
import { Metadata } from 'next';
import Image from 'next/image';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import {
  BarChart3, Camera, Users, Smartphone, Search,
  MessageSquare, Star, ArrowRight, CheckCircle, Play
} from 'lucide-react';

export const metadata: Metadata = {
  title: 'Vende tus vehículos en OKLA | Para Dealers',
  description: 'Únete a la plataforma líder de venta de vehículos en República Dominicana. +500 dealers confían en nosotros.',
  openGraph: {
    title: 'OKLA para Dealers - Vende más vehículos',
    description: 'Herramientas profesionales para concesionarios',
    images: ['/images/dealers/og-image.jpg'],
  },
};

const stats = [
  { value: '500+', label: 'Dealers activos' },
  { value: '10K+', label: 'Ventas mensuales' },
  { value: '95%', label: 'Satisfacción' },
  { value: '24h', label: 'Soporte' },
];

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
    icon: BarChart3,
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
    image: '/images/testimonials/juan.jpg',
    rating: 5,
  },
  {
    quote: 'El CRM integrado nos ayudó a nunca perder un lead. Totalmente recomendado.',
    author: 'María García',
    company: 'Autos Premium SD',
    image: '/images/testimonials/maria.jpg',
    rating: 5,
  },
  {
    quote: 'La mejor inversión que hemos hecho para nuestro negocio de vehículos.',
    author: 'Carlos Rodríguez',
    company: 'Rodríguez Motors',
    image: '/images/testimonials/carlos.jpg',
    rating: 5,
  },
];

export default function DealerLandingPage() {
  return (
    <div className="min-h-screen">
      {/* Hero Section */}
      <section className="relative bg-gradient-to-br from-primary-600 to-primary-900 text-white py-20 lg:py-32">
        <div className="container mx-auto px-4">
          <div className="grid lg:grid-cols-2 gap-12 items-center">
            <div>
              <Badge className="mb-4 bg-white/20 text-white border-white/30">
                Nuevo: IA para pricing automático
              </Badge>
              <h1 className="text-4xl lg:text-6xl font-bold mb-6">
                Vende más vehículos con OKLA
              </h1>
              <p className="text-xl text-white/90 mb-8">
                La plataforma #1 de compra-venta de vehículos en República Dominicana.
                Únete a más de 500 dealers que confían en nosotros.
              </p>

              <div className="flex flex-col sm:flex-row gap-4 mb-8">
                <Button size="lg" variant="secondary" asChild>
                  <Link href="/dealers/pricing">
                    Ver Planes
                    <ArrowRight className="ml-2 h-5 w-5" />
                  </Link>
                </Button>
                <Button size="lg" variant="outline" className="border-white text-white hover:bg-white/10" asChild>
                  <Link href="/dealers/register">
                    Registrarme Gratis
                  </Link>
                </Button>
              </div>

              <div className="flex flex-wrap gap-4">
                {['10,000+ visitantes mensuales', 'Herramientas profesionales', 'Soporte dedicado'].map((item) => (
                  <div key={item} className="flex items-center gap-2 text-white/90">
                    <CheckCircle className="h-5 w-5 text-green-400" />
                    <span>{item}</span>
                  </div>
                ))}
              </div>
            </div>

            <div className="relative">
              <div className="relative rounded-lg overflow-hidden shadow-2xl">
                <Image
                  src="/images/dealers/dashboard-preview.png"
                  alt="Dashboard de OKLA para Dealers"
                  width={600}
                  height={400}
                  className="w-full"
                  priority
                />
                <div className="absolute inset-0 flex items-center justify-center bg-black/30">
                  <Button size="lg" variant="secondary" className="rounded-full w-16 h-16">
                    <Play className="h-8 w-8 ml-1" />
                  </Button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Stats Section */}
      <section className="py-12 bg-white border-b">
        <div className="container mx-auto px-4">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-8">
            {stats.map((stat) => (
              <div key={stat.label} className="text-center">
                <div className="text-4xl font-bold text-primary-600 mb-2">
                  {stat.value}
                </div>
                <div className="text-gray-600">{stat.label}</div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section className="py-20 bg-gray-50">
        <div className="container mx-auto px-4">
          <div className="text-center mb-12">
            <h2 className="text-3xl font-bold mb-4">
              Todo lo que necesitas para vender más
            </h2>
            <p className="text-xl text-gray-600 max-w-2xl mx-auto">
              Herramientas profesionales diseñadas para concesionarios de vehículos
            </p>
          </div>

          <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-8">
            {features.map((feature) => (
              <Card key={feature.title} className="hover:shadow-lg transition-shadow">
                <CardContent className="p-6">
                  <div className="w-12 h-12 rounded-lg bg-primary-100 flex items-center justify-center mb-4">
                    <feature.icon className="h-6 w-6 text-primary-600" />
                  </div>
                  <h3 className="text-xl font-semibold mb-2">{feature.title}</h3>
                  <p className="text-gray-600">{feature.description}</p>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      </section>

      {/* Testimonials Section */}
      <section className="py-20 bg-white">
        <div className="container mx-auto px-4">
          <div className="text-center mb-12">
            <h2 className="text-3xl font-bold mb-4">
              Lo que dicen nuestros dealers
            </h2>
          </div>

          <div className="grid md:grid-cols-3 gap-8">
            {testimonials.map((testimonial) => (
              <Card key={testimonial.author} className="bg-gray-50">
                <CardContent className="p-6">
                  <div className="flex gap-1 mb-4">
                    {[...Array(testimonial.rating)].map((_, i) => (
                      <Star key={i} className="h-5 w-5 fill-yellow-400 text-yellow-400" />
                    ))}
                  </div>
                  <p className="text-gray-700 mb-4 italic">"{testimonial.quote}"</p>
                  <div className="flex items-center gap-3">
                    <div className="w-12 h-12 rounded-full bg-gray-300" />
                    <div>
                      <div className="font-semibold">{testimonial.author}</div>
                      <div className="text-sm text-gray-600">{testimonial.company}</div>
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="py-20 bg-primary-600 text-white">
        <div className="container mx-auto px-4 text-center">
          <h2 className="text-3xl font-bold mb-4">
            Empieza tu prueba gratis de 14 días
          </h2>
          <p className="text-xl text-white/90 mb-8 max-w-2xl mx-auto">
            Sin tarjeta de crédito requerida. Cancela cuando quieras.
          </p>
          <Button size="lg" variant="secondary" asChild>
            <Link href="/dealers/register">
              Comenzar Ahora
              <ArrowRight className="ml-2 h-5 w-5" />
            </Link>
          </Button>
        </div>
      </section>
    </div>
  );
}
```

### 2. Página de Precios

```typescript
// filepath: src/app/(marketing)/dealers/pricing/page.tsx
'use client';

import { useState } from 'react';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Switch } from '@/components/ui/switch';
import { Check, X, HelpCircle, Zap } from 'lucide-react';
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from '@/components/ui/accordion';
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip';

const plans = [
  {
    id: 'starter',
    name: 'Starter',
    description: 'Para dealers que están empezando',
    monthlyPrice: 49,
    yearlyPrice: 39,
    features: [
      { name: 'Listings activos', value: '15', included: true },
      { name: 'Ubicaciones', value: '1', included: true },
      { name: 'Fotos por vehículo', value: '20', included: true },
      { name: 'Analytics básico', value: true, included: true },
      { name: 'Soporte por email', value: true, included: true },
      { name: 'Video 360°', value: false, included: false },
      { name: 'CRM integrado', value: false, included: false },
      { name: 'Boost de listings', value: false, included: false },
      { name: 'API access', value: false, included: false },
      { name: 'Manager dedicado', value: false, included: false },
    ],
    cta: 'Comenzar',
    popular: false,
  },
  {
    id: 'professional',
    name: 'Profesional',
    description: 'Para dealers en crecimiento',
    monthlyPrice: 99,
    yearlyPrice: 79,
    features: [
      { name: 'Listings activos', value: '50', included: true },
      { name: 'Ubicaciones', value: '3', included: true },
      { name: 'Fotos por vehículo', value: '50', included: true },
      { name: 'Analytics avanzado', value: true, included: true },
      { name: 'Soporte prioritario', value: true, included: true },
      { name: 'Video 360°', value: true, included: true },
      { name: 'CRM integrado', value: true, included: true },
      { name: 'Boost de listings', value: '5/mes', included: true },
      { name: 'API access', value: false, included: false },
      { name: 'Manager dedicado', value: false, included: false },
    ],
    cta: 'Comenzar',
    popular: true,
  },
  {
    id: 'enterprise',
    name: 'Enterprise',
    description: 'Para grandes concesionarios',
    monthlyPrice: 249,
    yearlyPrice: 199,
    features: [
      { name: 'Listings activos', value: 'Ilimitado', included: true },
      { name: 'Ubicaciones', value: 'Ilimitado', included: true },
      { name: 'Fotos por vehículo', value: 'Ilimitado', included: true },
      { name: 'Analytics enterprise', value: true, included: true },
      { name: 'Soporte 24/7', value: true, included: true },
      { name: 'Video 360°', value: true, included: true },
      { name: 'CRM integrado', value: true, included: true },
      { name: 'Boost de listings', value: 'Ilimitado', included: true },
      { name: 'API access', value: true, included: true },
      { name: 'Manager dedicado', value: true, included: true },
    ],
    cta: 'Contactar Ventas',
    popular: false,
  },
];

const faqs = [
  {
    question: '¿Puedo cambiar de plan en cualquier momento?',
    answer: 'Sí, puedes actualizar o degradar tu plan en cualquier momento. Los cambios se aplicarán al inicio del siguiente ciclo de facturación.',
  },
  {
    question: '¿Hay un período de prueba?',
    answer: 'Sí, ofrecemos 14 días de prueba gratis en todos los planes. No necesitas tarjeta de crédito para empezar.',
  },
  {
    question: '¿Cómo funciona la facturación?',
    answer: 'La facturación es mensual o anual según el plan que elijas. Emitimos facturas con NCF para tu declaración de impuestos.',
  },
  {
    question: '¿Qué métodos de pago aceptan?',
    answer: 'Aceptamos todas las tarjetas de crédito/débito (Visa, Mastercard, AMEX), transferencias bancarias y pagos con AZUL.',
  },
  {
    question: '¿Qué pasa si excedo mi límite de listings?',
    answer: 'Te notificaremos cuando estés cerca del límite. Puedes actualizar tu plan o archivar listings antiguos para hacer espacio.',
  },
];

export default function DealerPricingPage() {
  const [isYearly, setIsYearly] = useState(false);

  return (
    <div className="min-h-screen bg-gray-50 py-20">
      <div className="container mx-auto px-4">
        {/* Header */}
        <div className="text-center mb-12">
          <Badge className="mb-4">Planes para Dealers</Badge>
          <h1 className="text-4xl font-bold mb-4">
            Planes diseñados para tu negocio
          </h1>
          <p className="text-xl text-gray-600 max-w-2xl mx-auto mb-8">
            Elige el plan que mejor se adapte al tamaño de tu inventario
          </p>

          {/* Billing Toggle */}
          <div className="flex items-center justify-center gap-4">
            <span className={`text-sm ${!isYearly ? 'font-semibold' : 'text-gray-500'}`}>
              Mensual
            </span>
            <Switch
              checked={isYearly}
              onCheckedChange={setIsYearly}
            />
            <span className={`text-sm ${isYearly ? 'font-semibold' : 'text-gray-500'}`}>
              Anual
            </span>
            {isYearly && (
              <Badge variant="secondary" className="bg-green-100 text-green-700">
                Ahorra 20%
              </Badge>
            )}
          </div>
        </div>

        {/* Pricing Cards */}
        <div className="grid md:grid-cols-3 gap-8 max-w-6xl mx-auto mb-20">
          {plans.map((plan) => (
            <Card
              key={plan.id}
              className={`relative ${plan.popular ? 'border-primary-500 border-2 shadow-lg' : ''}`}
            >
              {plan.popular && (
                <div className="absolute -top-3 left-1/2 -translate-x-1/2">
                  <Badge className="bg-primary-500 text-white">
                    <Zap className="w-3 h-3 mr-1" />
                    Más popular
                  </Badge>
                </div>
              )}

              <CardHeader className="text-center pb-2">
                <CardTitle className="text-2xl">{plan.name}</CardTitle>
                <p className="text-gray-600 text-sm">{plan.description}</p>
              </CardHeader>

              <CardContent className="text-center">
                <div className="mb-6">
                  <span className="text-5xl font-bold">
                    ${isYearly ? plan.yearlyPrice : plan.monthlyPrice}
                  </span>
                  <span className="text-gray-600">/mes</span>
                  {isYearly && (
                    <p className="text-sm text-gray-500 mt-1">
                      Facturado anualmente (${plan.yearlyPrice * 12}/año)
                    </p>
                  )}
                </div>

                <ul className="space-y-3 text-left">
                  {plan.features.map((feature) => (
                    <li key={feature.name} className="flex items-center gap-2">
                      {feature.included ? (
                        <Check className="w-5 h-5 text-green-500 flex-shrink-0" />
                      ) : (
                        <X className="w-5 h-5 text-gray-300 flex-shrink-0" />
                      )}
                      <span className={feature.included ? '' : 'text-gray-400'}>
                        {feature.name}
                        {typeof feature.value === 'string' && feature.value !== 'true' && (
                          <span className="font-semibold ml-1">({feature.value})</span>
                        )}
                      </span>
                    </li>
                  ))}
                </ul>
              </CardContent>

              <CardFooter>
                <Button
                  className="w-full"
                  variant={plan.popular ? 'default' : 'outline'}
                  size="lg"
                  asChild
                >
                  <Link href={plan.id === 'enterprise' ? '/contact/sales' : `/dealers/register?plan=${plan.id}`}>
                    {plan.cta}
                  </Link>
                </Button>
              </CardFooter>
            </Card>
          ))}
        </div>

        {/* Feature Comparison Table */}
        <div className="max-w-4xl mx-auto mb-20">
          <h2 className="text-2xl font-bold text-center mb-8">
            Comparación detallada de features
          </h2>
          <div className="overflow-x-auto">
            <table className="w-full border-collapse bg-white rounded-lg shadow">
              <thead>
                <tr className="border-b">
                  <th className="text-left p-4">Feature</th>
                  <th className="text-center p-4">Starter</th>
                  <th className="text-center p-4 bg-primary-50">Profesional</th>
                  <th className="text-center p-4">Enterprise</th>
                </tr>
              </thead>
              <tbody>
                {[
                  { feature: 'Listings activos', starter: '15', pro: '50', enterprise: 'Ilimitado' },
                  { feature: 'Ubicaciones/sucursales', starter: '1', pro: '3', enterprise: 'Ilimitado' },
                  { feature: 'Fotos por vehículo', starter: '20', pro: '50', enterprise: 'Ilimitado' },
                  { feature: 'Video 360°', starter: false, pro: true, enterprise: true },
                  { feature: 'CRM integrado', starter: false, pro: true, enterprise: true },
                  { feature: 'Analytics', starter: 'Básico', pro: 'Avanzado', enterprise: 'Enterprise' },
                  { feature: 'Boost de listings', starter: false, pro: '5/mes', enterprise: 'Ilimitado' },
                  { feature: 'Importación CSV', starter: false, pro: true, enterprise: true },
                  { feature: 'API access', starter: false, pro: false, enterprise: true },
                  { feature: 'White label', starter: false, pro: false, enterprise: true },
                  { feature: 'Manager dedicado', starter: false, pro: false, enterprise: true },
                  { feature: 'SLA garantizado', starter: false, pro: false, enterprise: '99.9%' },
                ].map((row) => (
                  <tr key={row.feature} className="border-b hover:bg-gray-50">
                    <td className="p-4 font-medium">{row.feature}</td>
                    <td className="p-4 text-center">
                      {typeof row.starter === 'boolean' ? (
                        row.starter ? <Check className="w-5 h-5 text-green-500 mx-auto" /> : <X className="w-5 h-5 text-gray-300 mx-auto" />
                      ) : (
                        row.starter
                      )}
                    </td>
                    <td className="p-4 text-center bg-primary-50">
                      {typeof row.pro === 'boolean' ? (
                        row.pro ? <Check className="w-5 h-5 text-green-500 mx-auto" /> : <X className="w-5 h-5 text-gray-300 mx-auto" />
                      ) : (
                        row.pro
                      )}
                    </td>
                    <td className="p-4 text-center">
                      {typeof row.enterprise === 'boolean' ? (
                        row.enterprise ? <Check className="w-5 h-5 text-green-500 mx-auto" /> : <X className="w-5 h-5 text-gray-300 mx-auto" />
                      ) : (
                        row.enterprise
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        {/* FAQ Section */}
        <div className="max-w-3xl mx-auto">
          <h2 className="text-2xl font-bold text-center mb-8">
            Preguntas frecuentes
          </h2>
          <Accordion type="single" collapsible className="bg-white rounded-lg shadow">
            {faqs.map((faq, index) => (
              <AccordionItem key={index} value={`item-${index}`}>
                <AccordionTrigger className="px-6 hover:no-underline">
                  {faq.question}
                </AccordionTrigger>
                <AccordionContent className="px-6 pb-4 text-gray-600">
                  {faq.answer}
                </AccordionContent>
              </AccordionItem>
            ))}
          </Accordion>
        </div>

        {/* CTA */}
        <div className="text-center mt-16">
          <p className="text-gray-600 mb-4">
            ¿Tienes preguntas? Nuestro equipo está listo para ayudarte.
          </p>
          <Button variant="outline" asChild>
            <Link href="/contact/sales">
              Hablar con un experto
            </Link>
          </Button>
        </div>
      </div>
    </div>
  );
}
```

---

## 📡 ENDPOINTS DE API

| Método | Endpoint                  | Descripción                | Servicio                |
| ------ | ------------------------- | -------------------------- | ----------------------- |
| `GET`  | `/api/dealers/plans`      | Obtener planes disponibles | DealerManagementService |
| `GET`  | `/api/dealers/plans/{id}` | Detalles de un plan        | DealerManagementService |
| `POST` | `/api/dealers/trial`      | Iniciar prueba gratis      | DealerManagementService |

---

## 🧪 TESTS E2E

```typescript
// filepath: e2e/dealer-landing.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Dealer Landing & Pricing", () => {
  test("landing page loads with all sections", async ({ page }) => {
    await page.goto("/dealers");

    await expect(
      page.getByRole("heading", { name: /vende más vehículos/i }),
    ).toBeVisible();
    await expect(page.getByText("500+")).toBeVisible(); // Stats
    await expect(
      page.getByRole("button", { name: /ver planes/i }),
    ).toBeEnabled();
  });

  test("pricing page shows all plans", async ({ page }) => {
    await page.goto("/dealers/pricing");

    await expect(page.getByText("Starter")).toBeVisible();
    await expect(page.getByText("Profesional")).toBeVisible();
    await expect(page.getByText("Enterprise")).toBeVisible();
  });

  test("yearly toggle updates prices", async ({ page }) => {
    await page.goto("/dealers/pricing");

    // Get initial monthly price
    const monthlyPrice = await page.getByText("$99").first();
    await expect(monthlyPrice).toBeVisible();

    // Toggle to yearly
    await page.getByRole("switch").click();

    // Check discounted price
    await expect(page.getByText("$79")).toBeVisible();
    await expect(page.getByText("Ahorra 20%")).toBeVisible();
  });

  test("CTA navigates to registration", async ({ page }) => {
    await page.goto("/dealers/pricing");

    await page
      .getByRole("button", { name: /comenzar/i })
      .first()
      .click();

    await expect(page).toHaveURL(/\/dealers\/register/);
  });
});
```

---

## ✅ CHECKLIST DE IMPLEMENTACIÓN

- [ ] Landing page con hero, stats, features
- [ ] Sección de testimoniales
- [ ] Página de pricing con 3 planes
- [ ] Toggle mensual/anual con descuento
- [ ] Tabla comparativa de features
- [ ] FAQ acordeón
- [ ] CTAs con tracking
- [ ] SEO optimizado (meta tags, OG)
- [ ] Responsive en todos los breakpoints
- [ ] Tests E2E

---

## 🔗 DOCUMENTOS RELACIONADOS

- [05-dealer-onboarding.md](05-dealer-onboarding.md) - Flujo de registro
- [04-dealer-dashboard.md](04-dealer-dashboard.md) - Dashboard post-registro
- [12-dealer-management-api.md](../../05-API-INTEGRATION/12-dealer-management-api.md) - API

---

_Última actualización: Enero 30, 2026_
