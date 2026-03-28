/**
 * Buying Guides Page
 *
 * Route: /guias
 */

import Link from 'next/link';
import { BookOpen, Car, FileCheck, DollarSign, Search, Shield, ChevronRight } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { guides } from './guide-data';

export const metadata = {
  title: 'Guías de Compra de Vehículos',
  description:
    'Guías completas para comprar y vender vehículos en República Dominicana. Aprende a detectar fraudes, verificar documentos y negociar el mejor precio.',
};

const iconMap: Record<string, typeof Search> = {
  Search,
  FileCheck,
  Car,
  DollarSign,
  Shield,
  BookOpen,
};

const guideCards = guides.map(g => ({
  icon: iconMap[g.icon] || BookOpen,
  title: g.title,
  description: g.description,
  topics: g.topics,
  href: `/guias/${g.slug}`,
  readTime: g.readTime,
}));

const quickTips = [
  'Siempre inspecciona el vehículo en persona antes de pagar',
  'Verifica que el VIN del vehículo coincida con los documentos',
  'Consulta el valor de mercado en OKLA antes de negociar',
  'Nunca pagues el total sin haber completado el traspaso',
  'Lleva a un mecánico de confianza para la revisión técnica',
];

export default function GuiasPage() {
  return (
    <div className="min-h-screen">
      {/* Hero */}
      <section className="from-primary to-primary/80 bg-gradient-to-br py-14 text-white">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-3xl text-center">
            <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-full bg-white/20">
              <BookOpen className="h-7 w-7" />
            </div>
            <h1 className="text-4xl font-bold">Guías de Compra</h1>
            <p className="mt-3 text-lg text-white/90">
              Todo lo que necesitas saber para comprar o vender un vehículo en República Dominicana
              de forma segura e inteligente.
            </p>
          </div>
        </div>
      </section>

      {/* Guides Grid */}
      <section className="py-14">
        <div className="container mx-auto px-4">
          <h2 className="text-foreground mb-8 text-2xl font-bold">Guías Disponibles</h2>
          <div className="grid gap-5 md:grid-cols-2 lg:grid-cols-3">
            {guideCards.map((guide, index) => (
              <Card
                key={index}
                className="border-border hover:border-primary group transition-colors"
              >
                <CardContent className="pt-6">
                  <div className="bg-primary/10 mb-3 flex h-11 w-11 items-center justify-center rounded-full">
                    <guide.icon className="text-primary h-5 w-5" />
                  </div>
                  <div className="flex items-start justify-between">
                    <h3 className="text-foreground group-hover:text-primary font-semibold transition-colors">
                      {guide.title}
                    </h3>
                    <span className="text-muted-foreground bg-muted ml-2 flex-shrink-0 rounded-full px-2 py-0.5 text-xs">
                      {guide.readTime}
                    </span>
                  </div>
                  <p className="text-muted-foreground mt-2 text-sm">{guide.description}</p>
                  <ul className="mt-3 space-y-1">
                    {guide.topics.map((topic, i) => (
                      <li key={i} className="text-muted-foreground flex items-center gap-2 text-xs">
                        <ChevronRight className="text-primary h-3 w-3" />
                        {topic}
                      </li>
                    ))}
                  </ul>
                  <Button asChild className="mt-4 w-full" variant="outline" size="sm">
                    <Link href={guide.href}>Leer Guía</Link>
                  </Button>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      </section>

      {/* Quick Tips */}
      <section className="bg-muted/50 py-14">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-2xl">
            <h2 className="text-foreground mb-6 text-2xl font-bold">
              5 Consejos Esenciales al Comprar
            </h2>
            <div className="space-y-3">
              {quickTips.map((tip, index) => (
                <div
                  key={index}
                  className="bg-card border-border flex items-start gap-3 rounded-lg border p-4 shadow-sm"
                >
                  <span className="bg-primary flex h-6 w-6 flex-shrink-0 items-center justify-center rounded-full text-xs font-bold text-white">
                    {index + 1}
                  </span>
                  <span className="text-foreground text-sm">{tip}</span>
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="py-14">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-2xl text-center">
            <h2 className="text-foreground text-2xl font-bold">¿Tienes más preguntas?</h2>
            <p className="text-muted-foreground mt-3">
              Visita nuestro centro de ayuda o pregunta a Ana, nuestra asistente de IA automotriz.
            </p>
            <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:justify-center">
              <Button asChild size="lg" className="bg-primary hover:bg-primary/90">
                <Link href="/ayuda">Centro de Ayuda</Link>
              </Button>
              <Button asChild size="lg" variant="outline">
                <Link href="/faq">Preguntas Frecuentes</Link>
              </Button>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
