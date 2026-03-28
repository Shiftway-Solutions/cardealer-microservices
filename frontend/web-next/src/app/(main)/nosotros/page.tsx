/**
 * About Us Page
 *
 * Static page with company information
 *
 * Route: /nosotros
 */

import Link from 'next/link';
import { Users, Target, Award, Car, Mail } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { ABOUT_STATS, PLATFORM_STATS } from '@/lib/platform-stats';

// =============================================================================
// METADATA
// =============================================================================

export const metadata = {
  title: 'Nosotros',
  description:
    'Conoce a OKLA, el marketplace líder de vehículos en República Dominicana. Nuestra misión, valores y el equipo detrás de la plataforma.',
};

// =============================================================================
// MAIN COMPONENT
// =============================================================================

export default function NosotrosPage() {
  return (
    <div className="min-h-screen">
      {/* Hero Section */}
      <section className="from-primary to-primary/80 relative bg-gradient-to-br py-20 text-white">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-3xl text-center">
            <h1 className="text-4xl font-bold md:text-5xl">
              Transformando el mercado de vehículos en RD
            </h1>
            <p className="mt-4 text-lg text-white/90">
              OKLA nació con una misión clara: hacer que comprar y vender vehículos sea
              transparente, seguro y accesible para todos los dominicanos.
            </p>
          </div>
        </div>
        <div className="absolute inset-x-0 bottom-0">
          <svg
            viewBox="0 0 1440 48"
            fill="none"
            xmlns="http://www.w3.org/2000/svg"
            className="w-full"
          >
            <path d="M0 48h1440V0C1440 0 1140 48 720 48S0 0 0 0v48z" className="fill-background" />
          </svg>
        </div>
      </section>

      {/* Stats Section */}
      <section className="py-12">
        <div className="container mx-auto px-4">
          <div className="grid grid-cols-2 gap-6 md:grid-cols-4">
            {ABOUT_STATS.map((stat, index) => (
              <div key={index} className="text-center">
                <div className="text-primary text-3xl font-bold md:text-4xl">{stat.value}</div>
                <div className="text-muted-foreground mt-1 text-sm">{stat.label}</div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Our Story */}
      <section className="bg-muted/50 py-16">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-3xl">
            <h2 className="text-foreground text-center text-3xl font-bold">Nuestra Historia</h2>
            <div className="text-muted-foreground mt-8 space-y-4">
              <p>
                OKLA fue fundada en {PLATFORM_STATS.foundingYear} por un equipo de emprendedores
                dominicanos apasionados por la tecnología y el sector automotriz. Vimos una
                oportunidad de modernizar un mercado que durante años funcionó de manera fragmentada
                y poco transparente.
              </p>
              <p>
                Inspirados en plataformas exitosas como CarGurus en Estados Unidos, desarrollamos
                una solución adaptada a las necesidades específicas del mercado dominicano. Nuestro
                algoritmo de Deal Rating analiza miles de puntos de datos para ayudarte a encontrar
                el mejor precio.
              </p>
              <p>
                Hoy, OKLA es el marketplace de vehículos de más rápido crecimiento en República
                Dominicana, conectando compradores y vendedores de manera eficiente y segura.
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* Values */}
      <section className="py-16">
        <div className="container mx-auto px-4">
          <h2 className="text-foreground text-center text-3xl font-bold">Nuestros Valores</h2>
          <div className="mt-12 grid gap-8 md:grid-cols-3">
            {[
              {
                icon: Target,
                title: 'Transparencia',
                description:
                  'Creemos que la información es poder. Proporcionamos datos de precios históricos, valoraciones de mercado y comparaciones para que tomes decisiones informadas.',
              },
              {
                icon: Users,
                title: 'Comunidad',
                description:
                  'Más que una plataforma, somos una comunidad de entusiastas del mundo automotriz. Conectamos personas con intereses comunes.',
              },
              {
                icon: Award,
                title: 'Excelencia',
                description:
                  'Nos esforzamos por ofrecer la mejor experiencia posible. Cada característica, cada detalle está diseñado pensando en nuestros usuarios.',
              },
            ].map((value, index) => (
              <Card key={index}>
                <CardContent className="pt-6">
                  <div className="bg-primary/10 flex h-12 w-12 items-center justify-center rounded-lg">
                    <value.icon className="text-primary h-6 w-6" />
                  </div>
                  <h3 className="text-foreground mt-4 text-xl font-semibold">{value.title}</h3>
                  <p className="text-muted-foreground mt-2">{value.description}</p>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      </section>

      {/* Team */}
      <section className="bg-muted/50 py-16">
        <div className="container mx-auto px-4">
          <h2 className="text-foreground text-center text-3xl font-bold">Nuestro Equipo</h2>
          <p className="text-muted-foreground mx-auto mt-4 max-w-2xl text-center">
            Un equipo multidisciplinario de profesionales comprometidos con revolucionar el mercado
            automotriz dominicano.
          </p>
          <div className="mt-12 grid gap-8 sm:grid-cols-2 lg:grid-cols-4">
            {[
              {
                name: 'Carlos Rodríguez',
                role: 'CEO & Co-Fundador',
                initials: 'CR',
                color: 'from-primary to-primary/80',
              },
              {
                name: 'María Santos',
                role: 'CTO',
                initials: 'MS',
                color: 'from-blue-500 to-blue-700',
              },
              {
                name: 'José Pérez',
                role: 'Director de Operaciones',
                initials: 'JP',
                color: 'from-amber-500 to-amber-700',
              },
              {
                name: 'Ana García',
                role: 'Directora de Marketing',
                initials: 'AG',
                color: 'from-purple-500 to-purple-700',
              },
            ].map((member, index) => (
              <div key={index} className="text-center">
                <div
                  className={`mx-auto flex h-24 w-24 items-center justify-center rounded-full bg-gradient-to-br ${member.color} text-2xl font-bold text-white shadow-lg`}
                >
                  {member.initials}
                </div>
                <h3 className="text-foreground mt-4 font-semibold">{member.name}</h3>
                <p className="text-muted-foreground text-sm">{member.role}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Contact CTA */}
      <section className="py-16">
        <div className="container mx-auto px-4">
          <div className="from-primary to-primary/80 mx-auto max-w-3xl rounded-2xl bg-gradient-to-br p-8 text-center text-white md:p-12">
            <h2 className="text-2xl font-bold md:text-3xl">¿Tienes preguntas?</h2>
            <p className="mt-4 text-white/90">
              Estamos aquí para ayudarte. Contáctanos y te responderemos a la brevedad.
            </p>
            <div className="mt-8 flex flex-col items-center justify-center gap-4 sm:flex-row">
              <Button asChild size="lg" className="hover:bg-muted text-primary gap-2 bg-white">
                <Link href="/contacto">
                  <Mail className="h-4 w-4" />
                  Contáctanos
                </Link>
              </Button>
              <Button
                asChild
                variant="outline"
                size="lg"
                className="gap-2 border-white text-white hover:bg-white/10"
              >
                <Link href="/vehiculos">
                  <Car className="h-4 w-4" />
                  Ver vehículos
                </Link>
              </Button>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
