/**
 * Report Fraud Page
 *
 * Route: /reportar
 */

import Link from 'next/link';
import { AlertTriangle, Shield, Phone, Mail, MessageSquare, Flag } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';

export const metadata = {
  title: 'Reportar Fraude',
  description:
    'Reporta actividad fraudulenta o sospechosa en OKLA. Te ayudamos a protegerte de estafas al comprar o vender vehículos.',
};

const reportTypes = [
  {
    icon: Flag,
    title: 'Anuncio Fraudulento',
    description: 'Vehículo que no existe, precio engañoso o fotos falsas.',
    action: '/ayuda',
    label: 'Reportar Anuncio',
  },
  {
    icon: MessageSquare,
    title: 'Mensaje Sospechoso',
    description: 'Alguien intentó pedirte dinero o datos personales.',
    action: '/ayuda',
    label: 'Reportar Mensaje',
  },
  {
    icon: Shield,
    title: 'Robo de Cuenta',
    description: 'Crees que alguien accedió a tu cuenta sin autorización.',
    action: '/cuenta/seguridad',
    label: 'Asegurar Cuenta',
  },
  {
    icon: AlertTriangle,
    title: 'Estafa de Pago',
    description: 'Pagaste por un vehículo y no lo recibiste o fue diferente al anunciado.',
    action: '/contacto',
    label: 'Contactar Soporte',
  },
];

const fraudSigns = [
  'El vendedor pide pago fuera de la plataforma (transferencias, Zelle, etc.)',
  'El precio es significativamente más bajo que el mercado sin justificación',
  'El vendedor no permite una inspección física del vehículo',
  'Las fotos del vehículo aparecen en otros sitios (imágenes de stock)',
  'El vendedor presiona para cerrar el trato rápidamente',
  'La documentación del vehículo parece alterada o incompleta',
  'El vendedor dice estar en el exterior y no puede mostrar el vehículo',
  'Te piden depositar un "seguro" antes de ver el vehículo',
];

export default function ReportarPage() {
  return (
    <div className="min-h-screen">
      {/* Hero */}
      <section className="bg-gradient-to-br from-red-600 to-red-800 py-14 text-white">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-2xl text-center">
            <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-full bg-white/20">
              <AlertTriangle className="h-7 w-7" />
            </div>
            <h1 className="text-3xl font-bold md:text-4xl">Reportar Fraude</h1>
            <p className="mt-3 text-white/90">
              Ayúdanos a mantener OKLA seguro. Reporta cualquier actividad sospechosa y nuestro
              equipo actuará en menos de 24 horas.
            </p>
          </div>
        </div>
      </section>

      {/* Report Types */}
      <section className="py-14">
        <div className="container mx-auto px-4">
          <h2 className="text-foreground mb-8 text-center text-2xl font-bold">
            ¿Qué quieres reportar?
          </h2>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {reportTypes.map((type, index) => (
              <Card key={index} className="border-border hover:border-primary transition-colors">
                <CardContent className="pt-6">
                  <div className="mb-3 flex h-11 w-11 items-center justify-center rounded-full bg-red-100">
                    <type.icon className="h-5 w-5 text-red-600" />
                  </div>
                  <h3 className="text-foreground font-semibold">{type.title}</h3>
                  <p className="text-muted-foreground mt-1 text-sm">{type.description}</p>
                  <Button asChild className="mt-4 w-full" variant="outline" size="sm">
                    <Link href={type.action}>{type.label}</Link>
                  </Button>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      </section>

      {/* Contact */}
      <section className="bg-muted/50 py-14">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-2xl">
            <h2 className="text-foreground mb-2 text-2xl font-bold">Contacto Directo</h2>
            <p className="text-muted-foreground mb-6">
              Para reportes urgentes de fraude, comunícate directamente con nuestro equipo de
              seguridad.
            </p>
            <div className="grid gap-4 sm:grid-cols-2">
              <a
                href="mailto:seguridad@okla.com.do"
                className="bg-card flex items-center gap-3 rounded-lg p-4 shadow-sm transition-shadow hover:shadow-md"
              >
                <Mail className="text-primary h-5 w-5" />
                <div>
                  <div className="text-foreground font-medium">Correo de Seguridad</div>
                  <div className="text-muted-foreground text-sm">seguridad@okla.com.do</div>
                </div>
              </a>
              <a
                href="tel:+18095551234"
                className="bg-card flex items-center gap-3 rounded-lg p-4 shadow-sm transition-shadow hover:shadow-md"
              >
                <Phone className="text-primary h-5 w-5" />
                <div>
                  <div className="text-foreground font-medium">Línea de Emergencia</div>
                  <div className="text-muted-foreground text-sm">+1 (809) 555-1234</div>
                </div>
              </a>
            </div>
          </div>
        </div>
      </section>

      {/* Warning Signs */}
      <section className="py-14">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-3xl">
            <h2 className="text-foreground mb-6 text-2xl font-bold">Señales de Alerta de Fraude</h2>
            <div className="grid gap-2 sm:grid-cols-2">
              {fraudSigns.map((sign, index) => (
                <div
                  key={index}
                  className="flex items-start gap-3 rounded-lg border border-amber-200 bg-amber-50 p-3"
                >
                  <AlertTriangle className="mt-0.5 h-4 w-4 flex-shrink-0 text-amber-600" />
                  <span className="text-sm text-amber-900">{sign}</span>
                </div>
              ))}
            </div>
            <div className="bg-primary/5 border-primary/20 mt-8 rounded-lg border p-5">
              <p className="text-foreground text-sm">
                <strong>Recuerda:</strong> OKLA nunca te pedirá tu contraseña, datos de tarjeta de
                crédito o transferencias fuera de la plataforma. Si alguien lo hace afirmando ser
                OKLA,{' '}
                <Link href="/contacto" className="text-primary font-medium hover:underline">
                  repórtalo inmediatamente
                </Link>
                .
              </p>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
