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

// =============================================================================
// METADATA
// =============================================================================

export const metadata = {
  title: 'Nosotros | OKLA - Marketplace de Vehículos en RD',
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
      <section className="relative bg-gradient-to-br from-[#00A870] to-[#007850] py-20 text-white">
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
            <path d="M0 48h1440V0C1440 0 1140 48 720 48S0 0 0 0v48z" fill="white" />
          </svg>
        </div>
      </section>

      {/* Stats Section */}
      <section className="py-12">
        <div className="container mx-auto px-4">
          <div className="grid grid-cols-2 gap-6 md:grid-cols-4">
            {[
              { value: '10,000+', label: 'Vehículos publicados' },
              { value: '50,000+', label: 'Usuarios activos' },
              { value: '500+', label: 'Dealers registrados' },
              { value: '95%', label: 'Satisfacción del cliente' },
            ].map((stat, index) => (
              <div key={index} className="text-center">
                <div className="text-3xl font-bold text-[#00A870] md:text-4xl">{stat.value}</div>
                <div className="mt-1 text-sm text-muted-foreground">{stat.label}</div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Our Story */}
      <section className="bg-muted/50 py-16">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-3xl">
            <h2 className="text-center text-3xl font-bold text-foreground">Nuestra Historia</h2>
            <div className="mt-8 space-y-4 text-muted-foreground">
              <p>
                OKLA fue fundada en 2024 por un equipo de emprendedores dominicanos apasionados por
                la tecnología y el sector automotriz. Vimos una oportunidad de modernizar un mercado
                que durante años funcionó de manera fragmentada y poco transparente.
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
          <h2 className="text-center text-3xl font-bold text-foreground">Nuestros Valores</h2>
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
                  <div className="flex h-12 w-12 items-center justify-center rounded-lg bg-[#00A870]/10">
                    <value.icon className="h-6 w-6 text-[#00A870]" />
                  </div>
                  <h3 className="mt-4 text-xl font-semibold text-foreground">{value.title}</h3>
                  <p className="mt-2 text-muted-foreground">{value.description}</p>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      </section>

      {/* Team */}
      <section className="bg-muted/50 py-16">
        <div className="container mx-auto px-4">
          <h2 className="text-center text-3xl font-bold text-foreground">Nuestro Equipo</h2>
          <p className="mx-auto mt-4 max-w-2xl text-center text-muted-foreground">
            Un equipo multidisciplinario de profesionales comprometidos con revolucionar el mercado
            automotriz dominicano.
          </p>
          <div className="mt-12 grid gap-8 sm:grid-cols-2 lg:grid-cols-4">
            {[
              { name: 'Carlos Rodríguez', role: 'CEO & Co-Fundador', image: '👨‍💼' },
              { name: 'María Santos', role: 'CTO', image: '👩‍💻' },
              { name: 'José Pérez', role: 'Director de Operaciones', image: '👨‍💼' },
              { name: 'Ana García', role: 'Directora de Marketing', image: '👩‍💼' },
            ].map((member, index) => (
              <div key={index} className="text-center">
                <div className="mx-auto flex h-24 w-24 items-center justify-center rounded-full bg-muted text-4xl">
                  {member.image}
                </div>
                <h3 className="mt-4 font-semibold text-foreground">{member.name}</h3>
                <p className="text-sm text-muted-foreground">{member.role}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Contact CTA */}
      <section className="py-16">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-3xl rounded-2xl bg-gradient-to-br from-[#00A870] to-[#007850] p-8 text-center text-white md:p-12">
            <h2 className="text-2xl font-bold md:text-3xl">¿Tienes preguntas?</h2>
            <p className="mt-4 text-white/90">
              Estamos aquí para ayudarte. Contáctanos y te responderemos a la brevedad.
            </p>
            <div className="mt-8 flex flex-col items-center justify-center gap-4 sm:flex-row">
              <Button asChild size="lg" className="gap-2 bg-white text-[#00A870] hover:bg-muted">
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
