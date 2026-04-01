/**
 * Press / Media Room Page
 *
 * Route: /prensa
 */

import { Newspaper, Download, Mail, Phone } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { PRESS_STATS } from '@/lib/platform-stats';

export const metadata = {
  title: 'Sala de Prensa',
  description:
    'Sala de prensa de OKLA. Encuentra comunicados de prensa, estadísticas de la plataforma, kit de marca y contacto con nuestro equipo de relaciones públicas.',
};

const pressReleases = [
  {
    date: 'Febrero 2026',
    title: 'OKLA supera los 10,000 vehículos publicados en su plataforma',
    category: 'Hito',
  },
  {
    date: 'Enero 2026',
    title: 'OKLA lanza verificación KYC para vendedores y dealers',
    category: 'Producto',
  },
  {
    date: 'Diciembre 2025',
    title: 'OKLA integra cinco pasarelas de pago locales e internacionales',
    category: 'Producto',
  },
  {
    date: 'Noviembre 2025',
    title: 'OKLA llega a 50,000 usuarios registrados en República Dominicana',
    category: 'Hito',
  },
];

const stats = PRESS_STATS;

export default function PrensaPage() {
  return (
    <div className="min-h-screen">
      {/* Hero */}
      <section className="bg-gradient-to-br from-slate-800 to-slate-900 py-16 text-white">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-3xl">
            <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-white/15">
              <Newspaper className="h-6 w-6" />
            </div>
            <h1 className="text-4xl font-bold md:text-5xl">Sala de Prensa</h1>
            <p className="mt-4 text-lg text-white/80">
              Recursos para periodistas, bloggers y medios de comunicación que cubren el mercado
              automotriz dominicano.
            </p>
          </div>
        </div>
      </section>

      {/* Stats */}
      <section className="border-border border-b py-10">
        <div className="container mx-auto px-4">
          <div className="grid grid-cols-2 gap-6 md:grid-cols-4">
            {stats.map((stat, index) => (
              <div key={index} className="text-center">
                <div className="text-primary text-3xl font-bold md:text-4xl">{stat.value}</div>
                <div className="text-muted-foreground mt-1 text-sm">{stat.label}</div>
              </div>
            ))}
          </div>
          <p className="text-muted-foreground mt-6 text-center text-xs">
            *Cifras proyectadas basadas en el mercado automotriz dominicano.
          </p>
        </div>
      </section>

      {/* Press Releases */}
      <section className="py-14">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-3xl">
            <h2 className="text-foreground mb-6 text-2xl font-bold">Comunicados de Prensa</h2>
            <div className="space-y-3">
              {pressReleases.map((release, index) => (
                <Card key={index} className="border-border">
                  <CardContent className="flex items-center justify-between p-5">
                    <div>
                      <div className="flex items-center gap-2">
                        <Badge variant="secondary">{release.category}</Badge>
                        <span className="text-muted-foreground text-sm">{release.date}</span>
                      </div>
                      <h3 className="text-foreground mt-1 font-medium">{release.title}</h3>
                    </div>
                    <Button asChild variant="ghost" size="sm" className="flex-shrink-0">
                      <a href="mailto:prensa@okla.com.do" title="Solicitar comunicado por email">
                        <Download className="h-4 w-4" />
                      </a>
                    </Button>
                  </CardContent>
                </Card>
              ))}
            </div>
          </div>
        </div>
      </section>

      {/* Brand Kit */}
      <section className="bg-muted/50 py-14">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-3xl">
            <h2 className="text-foreground mb-2 text-2xl font-bold">Kit de Marca</h2>
            <p className="text-muted-foreground mb-6">
              Logotipos, colores, tipografía y guías de uso de la marca OKLA.
            </p>
            <div className="grid gap-4 sm:grid-cols-3">
              {['Logotipos PNG/SVG', 'Paleta de Colores', 'Guía de Marca'].map((item, index) => (
                <div
                  key={index}
                  className="bg-card border-border flex items-center justify-between rounded-lg border p-4 shadow-sm"
                >
                  <span className="text-foreground text-sm font-medium">{item}</span>
                  <Button variant="ghost" size="sm">
                    <Download className="h-4 w-4" />
                  </Button>
                </div>
              ))}
            </div>
            <p className="text-muted-foreground mt-4 text-sm">
              Para descargar los recursos, contacta a{' '}
              <a href="mailto:prensa@okla.com.do" className="text-primary hover:underline">
                prensa@okla.com.do
              </a>
            </p>
          </div>
        </div>
      </section>

      {/* Press Contact */}
      <section className="py-14">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-2xl text-center">
            <h2 className="text-foreground mb-4 text-2xl font-bold">Contacto de Prensa</h2>
            <p className="text-muted-foreground mb-8">
              Para entrevistas, solicitudes de datos o información adicional, contacta a nuestro
              equipo de comunicaciones.
            </p>
            <div className="grid gap-4 sm:grid-cols-2">
              <a
                href="mailto:prensa@okla.com.do"
                className="bg-card border-border hover:border-primary flex items-center gap-3 rounded-lg border p-4 transition-colors"
              >
                <Mail className="text-primary h-5 w-5" />
                <div className="text-left">
                  <div className="text-foreground font-medium">Email</div>
                  <div className="text-muted-foreground text-sm">prensa@okla.com.do</div>
                </div>
              </a>
              <a
                href="tel:+18098501234"
                className="bg-card border-border hover:border-primary flex items-center gap-3 rounded-lg border p-4 transition-colors"
              >
                <Phone className="text-primary h-5 w-5" />
                <div className="text-left">
                  <div className="text-foreground font-medium">Teléfono</div>
                  <div className="text-muted-foreground text-sm">+1 (809) 850-1234</div>
                </div>
              </a>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
