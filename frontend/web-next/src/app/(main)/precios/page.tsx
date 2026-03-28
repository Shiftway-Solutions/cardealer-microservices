/**
 * Pricing Guide Page
 *
 * General vehicle pricing guide for buyers and sellers.
 * Note: Dealer subscription pricing is at /dealer/pricing
 *
 * Route: /precios
 */

import Link from 'next/link';
import { TrendingUp, Search, Calculator, Car, Info, ArrowRight } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';

export const metadata = {
  title: 'Guía de Precios de Vehículos',
  description:
    'Referencia de precios del mercado automotriz en República Dominicana. Entiende el valor justo de un vehículo antes de comprar o vender.',
};

const priceFactors = [
  {
    icon: Car,
    title: 'Marca y Modelo',
    description:
      'Las marcas japonesas (Toyota, Honda, Nissan) mantienen mejor su valor en el mercado dominicano. Los modelos más populares tienen mayor liquidez.',
  },
  {
    icon: TrendingUp,
    title: 'Año y Kilometraje',
    description:
      'Un vehículo pierde entre un 15-20% de su valor en el primer año. El kilometraje ideal para un vehículo usado es menos de 20,000 km por año.',
  },
  {
    icon: Info,
    title: 'Condición General',
    description:
      'El estado del motor, la carrocería y el interior afectan directamente el precio. Un historial de mantenimiento documentado agrega valor.',
  },
  {
    icon: Calculator,
    title: 'Aranceles e Impuestos',
    description:
      'En RD, los vehículos importados tienen aranceles del 0-40% + ITBIS (18%). Esto impacta el precio final vs. el mercado internacional.',
  },
];

const priceRanges = [
  {
    category: 'Económicos / Hatchback',
    examples: 'Toyota Yaris, Kia Rio, Hyundai i10',
    range: 'RD$400,000 - RD$900,000',
    condition: 'Usados 3-7 años',
  },
  {
    category: 'Sedanes Medianos',
    examples: 'Toyota Corolla, Honda Civic, Nissan Sentra',
    range: 'RD$700,000 - RD$1,500,000',
    condition: 'Usados 2-6 años',
  },
  {
    category: 'SUVs / Yipetas',
    examples: 'Toyota RAV4, Honda CR-V, Hyundai Tucson',
    range: 'RD$1,200,000 - RD$2,800,000',
    condition: 'Usados 2-6 años',
  },
  {
    category: 'Pickups',
    examples: 'Toyota Hilux, Ford Ranger, Mitsubishi L200',
    range: 'RD$1,500,000 - RD$3,500,000',
    condition: 'Usados 2-7 años',
  },
  {
    category: 'Vehículos de Lujo',
    examples: 'Mercedes-Benz, BMW, Lexus',
    range: 'RD$2,500,000 en adelante',
    condition: 'Varios años',
  },
];

export default function PreciosPage() {
  return (
    <div className="min-h-screen">
      {/* Hero */}
      <section className="from-primary to-primary/80 bg-gradient-to-br py-14 text-white">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-3xl text-center">
            <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-full bg-white/20">
              <TrendingUp className="h-7 w-7" />
            </div>
            <h1 className="text-4xl font-bold">Guía de Precios</h1>
            <p className="mt-3 text-lg text-white/90">
              Referencia del mercado automotriz dominicano. Compra o vende con conocimiento del
              valor real de los vehículos.
            </p>
            <Button
              asChild
              className="text-primary mt-6 bg-white font-semibold hover:bg-white/90"
              size="lg"
            >
              <Link href="/buscar">
                <Search className="mr-2 h-4 w-4" />
                Buscar Vehículos
              </Link>
            </Button>
          </div>
        </div>
      </section>

      {/* Price Ranges */}
      <section className="py-14">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-4xl">
            <h2 className="text-foreground mb-2 text-2xl font-bold">
              Rangos de Precio por Categoría
            </h2>
            <p className="text-muted-foreground mb-6">
              Precios de referencia del mercado dominicano (Febrero 2026). Los precios varían según
              condición, año y negociación.
            </p>
            <div className="border-border bg-card overflow-hidden rounded-lg border shadow-sm">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-border bg-muted/50 border-b">
                    <th className="text-foreground px-4 py-3 text-left font-semibold">Categoría</th>
                    <th className="text-foreground hidden px-4 py-3 text-left font-semibold sm:table-cell">
                      Ejemplos
                    </th>
                    <th className="text-foreground px-4 py-3 text-left font-semibold">
                      Rango de Precio
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {priceRanges.map((row, index) => (
                    <tr
                      key={index}
                      className="border-border hover:bg-muted/30 border-b last:border-0"
                    >
                      <td className="px-4 py-3">
                        <div className="text-foreground font-medium">{row.category}</div>
                        <div className="text-muted-foreground text-xs">{row.condition}</div>
                      </td>
                      <td className="text-muted-foreground hidden px-4 py-3 sm:table-cell">
                        {row.examples}
                      </td>
                      <td className="text-primary px-4 py-3 font-semibold">{row.range}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <p className="text-muted-foreground mt-3 text-xs">
              * Precios aproximados. Los valores reales dependen del año exacto, kilometraje,
              condición y negociación entre partes.
            </p>
          </div>
        </div>
      </section>

      {/* Price Factors */}
      <section className="bg-muted/50 py-14">
        <div className="container mx-auto px-4">
          <h2 className="text-foreground mb-8 text-center text-2xl font-bold">
            Factores que Afectan el Precio
          </h2>
          <div className="grid gap-5 md:grid-cols-2 lg:grid-cols-4">
            {priceFactors.map((factor, index) => (
              <Card key={index} className="border-border">
                <CardContent className="pt-5">
                  <div className="bg-primary/10 mb-3 flex h-10 w-10 items-center justify-center rounded-full">
                    <factor.icon className="text-primary h-5 w-5" />
                  </div>
                  <h3 className="text-foreground font-semibold">{factor.title}</h3>
                  <p className="text-muted-foreground mt-2 text-sm">{factor.description}</p>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="py-14">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-2xl text-center">
            <h2 className="text-foreground text-2xl font-bold">¿Listo para vender tu vehículo?</h2>
            <p className="text-muted-foreground mt-3">
              Publica gratis o con plan premium y llega a miles de compradores potenciales en RD.
            </p>
            <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:justify-center">
              <Button asChild size="lg" className="bg-primary hover:bg-primary/90">
                <Link href="/publicar">
                  Publicar Vehículo
                  <ArrowRight className="ml-2 h-4 w-4" />
                </Link>
              </Button>
              <Button asChild size="lg" variant="outline">
                <Link href="/guias">Ver Guías de Compra</Link>
              </Button>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
