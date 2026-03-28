/**
 * Jobs / Careers Page
 *
 * Route: /empleos
 */

import { Briefcase, MapPin, Users, Heart, Zap, Globe, Mail } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';

export const metadata = {
  title: 'Empleos - Únete al Equipo',
  description:
    'Trabaja con nosotros en OKLA. Conoce las oportunidades disponibles y únete al equipo que está transformando el mercado automotriz de República Dominicana.',
};

const openPositions = [
  {
    title: 'Desarrollador Full Stack (.NET / Next.js)',
    department: 'Tecnología',
    type: 'Tiempo Completo',
    location: 'Santo Domingo, RD / Remoto',
    level: 'Senior',
  },
  {
    title: 'Diseñador UX/UI',
    department: 'Producto',
    type: 'Tiempo Completo',
    location: 'Santo Domingo, RD',
    level: 'Semi-Senior',
  },
  {
    title: 'Ejecutivo de Ventas — Dealers',
    department: 'Ventas',
    type: 'Tiempo Completo',
    location: 'Santo Domingo / Santiago',
    level: 'Todos los niveles',
  },
  {
    title: 'Especialista en Marketing Digital',
    department: 'Marketing',
    type: 'Tiempo Completo',
    location: 'Santo Domingo, RD',
    level: 'Semi-Senior',
  },
  {
    title: 'Agente de Soporte al Cliente',
    department: 'Soporte',
    type: 'Tiempo Completo',
    location: 'Santo Domingo, RD',
    level: 'Junior',
  },
];

const benefits = [
  { icon: Heart, title: 'Seguro Médico', desc: 'Cobertura médica para ti y tu familia' },
  { icon: Zap, title: 'Crecimiento Rápido', desc: 'Startup en expansión con oportunidades reales' },
  { icon: Globe, title: 'Trabajo Flexible', desc: 'Modalidad híbrida y remota para muchos roles' },
  { icon: Users, title: 'Cultura Colaborativa', desc: 'Equipo joven, diverso y apasionado' },
];

export default function EmpleosPage() {
  return (
    <div className="min-h-screen">
      {/* Hero */}
      <section className="from-primary to-primary/80 bg-gradient-to-br py-16 text-white">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-3xl text-center">
            <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-full bg-white/20">
              <Briefcase className="h-7 w-7" />
            </div>
            <h1 className="text-4xl font-bold md:text-5xl">Únete al Equipo OKLA</h1>
            <p className="mt-4 text-lg text-white/90">
              Estamos construyendo el marketplace de vehículos más importante de República
              Dominicana. ¿Quieres ser parte del viaje?
            </p>
          </div>
        </div>
      </section>

      {/* Benefits */}
      <section className="py-14">
        <div className="container mx-auto px-4">
          <h2 className="text-foreground mb-8 text-center text-2xl font-bold">
            ¿Por qué trabajar en OKLA?
          </h2>
          <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
            {benefits.map((benefit, index) => (
              <div key={index} className="border-border bg-card rounded-lg border p-5 text-center">
                <div className="bg-primary/10 mx-auto mb-3 flex h-11 w-11 items-center justify-center rounded-full">
                  <benefit.icon className="text-primary h-5 w-5" />
                </div>
                <h3 className="text-foreground font-semibold">{benefit.title}</h3>
                <p className="text-muted-foreground mt-1 text-sm">{benefit.desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Open Positions */}
      <section className="bg-muted/50 py-14">
        <div className="container mx-auto px-4">
          <h2 className="text-foreground mb-8 text-2xl font-bold">Posiciones Abiertas</h2>
          <div className="space-y-3">
            {openPositions.map((position, index) => (
              <Card key={index} className="border-border hover:border-primary transition-colors">
                <CardContent className="p-5">
                  <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                    <div>
                      <h3 className="text-foreground font-semibold">{position.title}</h3>
                      <div className="mt-1 flex flex-wrap items-center gap-2">
                        <Badge variant="secondary">{position.department}</Badge>
                        <span className="text-muted-foreground flex items-center gap-1 text-sm">
                          <MapPin className="h-3 w-3" />
                          {position.location}
                        </span>
                        <span className="text-muted-foreground text-sm">· {position.level}</span>
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      <Badge
                        variant="outline"
                        className="text-primary border-primary/30 bg-primary/5"
                      >
                        {position.type}
                      </Badge>
                      <Button size="sm" asChild>
                        <a
                          href={`mailto:empleos@okla.com.do?subject=Aplicación: ${position.title}`}
                        >
                          Aplicar
                        </a>
                      </Button>
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="py-14">
        <div className="container mx-auto px-4">
          <div className="bg-primary mx-auto max-w-2xl rounded-2xl p-10 text-center text-white">
            <Mail className="mx-auto mb-4 h-10 w-10 opacity-80" />
            <h2 className="text-2xl font-bold">¿No ves tu posición ideal?</h2>
            <p className="mt-3 text-white/85">
              Envíanos tu CV. Siempre estamos buscando personas talentosas para sumarse a OKLA.
            </p>
            <Button
              asChild
              className="text-primary mt-6 bg-white font-semibold hover:bg-white/90"
              size="lg"
            >
              <a href="mailto:empleos@okla.com.do">Enviar CV Espontáneo</a>
            </Button>
          </div>
        </div>
      </section>
    </div>
  );
}
