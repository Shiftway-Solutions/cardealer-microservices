import type { Metadata } from 'next';
import Link from 'next/link';
import { Calculator, Ship, BarChart3, GitCompareArrows, ScanLine, Wrench } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';

export const metadata: Metadata = {
  title: 'Herramientas para Vehículos en RD',
  description:
    'Herramientas gratuitas para comprar, vender o importar vehículos en República Dominicana. Calculadora de financiamiento, importación, comparador y más.',
  keywords: [
    'herramientas vehiculares RD',
    'calculadora financiamiento vehículo',
    'calculadora importación carro',
    'comparar vehículos dominicana',
  ],
};

const herramientas = [
  {
    icon: Calculator,
    title: 'Calculadora de Financiamiento',
    description:
      'Calcula tu cuota mensual, total de intereses y tabla de amortización para financiar tu vehículo. Tasas del mercado dominicano actualizadas.',
    href: '/herramientas/calculadora-financiamiento',
    cta: 'Calcular cuota',
    color: 'text-emerald-600',
    bgColor: 'bg-emerald-50',
  },
  {
    icon: Ship,
    title: 'Calculadora de Importación',
    description:
      'Estima los impuestos y costos totales de importar un vehículo a República Dominicana: arancel, ITBIS, selectivo, flete y más.',
    href: '/herramientas/calculadora-importacion',
    cta: 'Calcular costos',
    color: 'text-blue-600',
    bgColor: 'bg-blue-50',
  },
  {
    icon: BarChart3,
    title: 'Guía de Precios',
    description:
      'Consulta los rangos de precios por categoría de vehículo en el mercado dominicano. Económicos, sedanes, SUVs, pickups y más.',
    href: '/precios',
    cta: 'Ver precios',
    color: 'text-amber-600',
    bgColor: 'bg-amber-50',
  },
  {
    icon: GitCompareArrows,
    title: 'Comparador de Vehículos',
    description:
      'Compara hasta 3 vehículos lado a lado: precio, especificaciones, equipamiento y valoración. Toma la mejor decisión.',
    href: '/comparar',
    cta: 'Comparar ahora',
    color: 'text-purple-600',
    bgColor: 'bg-purple-50',
  },
  {
    icon: ScanLine,
    title: 'Verificación VIN',
    description:
      'Escanea o ingresa el VIN de un vehículo para obtener su historial, especificaciones de fábrica y verificar su autenticidad.',
    href: '/okla-score',
    cta: 'Verificar VIN',
    color: 'text-rose-600',
    bgColor: 'bg-rose-50',
  },
];

export default function HerramientasPage() {
  return (
    <div className="bg-background min-h-screen">
      {/* Hero */}
      <section className="from-primary to-primary/80 bg-gradient-to-br py-16 text-white">
        <div className="container mx-auto px-4 text-center">
          <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-white/20">
            <Wrench className="h-8 w-8" />
          </div>
          <h1 className="mb-4 text-4xl font-bold">Herramientas para Vehículos</h1>
          <p className="mx-auto max-w-2xl text-lg text-white/90">
            Todo lo que necesitas para tomar la mejor decisión al comprar, vender o importar un
            vehículo en República Dominicana. 100% gratuito.
          </p>
        </div>
      </section>

      {/* Grid de herramientas */}
      <section className="container mx-auto px-4 py-14">
        <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
          {herramientas.map(tool => (
            <Card key={tool.href} className="group transition-shadow hover:shadow-lg">
              <CardHeader>
                <div
                  className={`mb-3 flex h-12 w-12 items-center justify-center rounded-lg ${tool.bgColor}`}
                >
                  <tool.icon className={`h-6 w-6 ${tool.color}`} />
                </div>
                <CardTitle className="text-xl">{tool.title}</CardTitle>
                <CardDescription className="text-sm leading-relaxed">
                  {tool.description}
                </CardDescription>
              </CardHeader>
              <CardContent>
                <Link href={tool.href}>
                  <Button
                    variant="outline"
                    className="group-hover:bg-primary w-full group-hover:text-white"
                  >
                    {tool.cta}
                  </Button>
                </Link>
              </CardContent>
            </Card>
          ))}
        </div>
      </section>

      {/* CTA final */}
      <section className="bg-muted/50 py-14">
        <div className="container mx-auto px-4 text-center">
          <h2 className="mb-4 text-2xl font-bold">¿Listo para encontrar tu vehículo ideal?</h2>
          <p className="text-muted-foreground mb-6">
            Explora miles de vehículos verificados en OKLA
          </p>
          <div className="flex flex-col items-center justify-center gap-4 sm:flex-row">
            <Link href="/vehiculos">
              <Button size="lg">Buscar vehículos</Button>
            </Link>
            <Link href="/vender">
              <Button size="lg" variant="outline">
                Publicar gratis
              </Button>
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
}
