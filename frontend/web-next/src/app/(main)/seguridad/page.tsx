/**
 * Security Center Page
 *
 * Trust & Safety information page for users.
 * Note: This is the public-facing security page.
 * For account security settings, see /cuenta/seguridad
 *
 * Route: /seguridad
 */

import type { Metadata } from 'next';
import Link from 'next/link';
import { Shield, Lock, Eye, AlertTriangle, CheckCircle, Phone, Mail } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';

export const metadata: Metadata = {
  title: 'Centro de Seguridad',
  description:
    'Cómo OKLA protege tus datos y transacciones. Consejos de seguridad para comprar y vender vehículos de forma segura.',
};

const securityFeatures = [
  {
    icon: Lock,
    title: 'Cifrado SSL/TLS',
    description:
      'Toda la comunicación entre tu dispositivo y OKLA está cifrada con TLS 1.3, el estándar de seguridad más alto disponible.',
  },
  {
    icon: Shield,
    title: 'Verificación KYC',
    description:
      'Verificamos la identidad de vendedores mediante documentos de identidad y prueba de vida para reducir el fraude.',
  },
  {
    icon: Eye,
    title: 'Monitoreo 24/7',
    description:
      'Nuestro equipo monitorea la plataforma continuamente para detectar actividades sospechosas y proteger a los usuarios.',
  },
  {
    icon: CheckCircle,
    title: 'Pagos Seguros',
    description:
      'Procesamos pagos a través de pasarelas certificadas PCI-DSS: Azul (Banco Popular) y PixelPay.',
  },
];

const safetyTips = [
  'Nunca pagues fuera de la plataforma OKLA',
  'Verifica la identidad del vendedor antes de cerrar trato',
  'Realiza inspecciones en persona en lugares públicos',
  'Desconfía de precios extremadamente bajos',
  'No compartas información bancaria por mensajes',
  'Usa el sistema de mensajería interno de OKLA',
  'Verifica el VIN del vehículo antes de comprar',
  'Solicita un historial del vehículo (verificación DGII o inspección mecánica)',
];

export default function SeguridadPage() {
  return (
    <div className="min-h-screen">
      {/* Hero */}
      <section className="from-primary to-primary/80 bg-gradient-to-br py-16 text-white">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-3xl text-center">
            <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-white/20">
              <Shield className="h-8 w-8" />
            </div>
            <h1 className="text-4xl font-bold">Centro de Seguridad</h1>
            <p className="mt-4 text-lg text-white/90">
              Tu seguridad es nuestra prioridad. Conoce cómo protegemos cada transacción en OKLA.
            </p>
          </div>
        </div>
      </section>

      {/* Features */}
      <section className="py-16">
        <div className="container mx-auto px-4">
          <h2 className="text-foreground mb-8 text-center text-2xl font-bold">
            Cómo Protegemos tu Información
          </h2>
          <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4">
            {securityFeatures.map((feature, index) => (
              <Card key={index} className="border-border">
                <CardContent className="pt-6">
                  <div className="bg-primary/10 mb-4 flex h-12 w-12 items-center justify-center rounded-full">
                    <feature.icon className="text-primary h-6 w-6" />
                  </div>
                  <h3 className="text-foreground font-semibold">{feature.title}</h3>
                  <p className="text-muted-foreground mt-2 text-sm">{feature.description}</p>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      </section>

      {/* Safety Tips */}
      <section className="bg-muted/50 py-16">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-3xl">
            <div className="mb-2 flex items-center gap-2">
              <AlertTriangle className="h-5 w-5 text-amber-500" />
              <h2 className="text-foreground text-2xl font-bold">
                Consejos para Comprar y Vender de Forma Segura
              </h2>
            </div>
            <p className="text-muted-foreground mb-6">
              Sigue estas recomendaciones para tener una experiencia segura en OKLA.
            </p>
            <div className="grid gap-3 sm:grid-cols-2">
              {safetyTips.map((tip, index) => (
                <div
                  key={index}
                  className="bg-card flex items-start gap-3 rounded-lg p-3 shadow-sm"
                >
                  <CheckCircle className="text-primary mt-0.5 h-5 w-5 flex-shrink-0" />
                  <span className="text-foreground text-sm">{tip}</span>
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

      {/* Report Section */}
      <section className="py-16">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-2xl text-center">
            <h2 className="text-foreground text-2xl font-bold">
              ¿Detectaste Actividad Sospechosa?
            </h2>
            <p className="text-muted-foreground mt-3">
              Si encuentras un anuncio fraudulento o recibes mensajes sospechosos, repórtalo
              inmediatamente. Actuamos en menos de 24 horas.
            </p>
            <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:justify-center">
              <Button asChild className="bg-primary hover:bg-primary/90">
                <Link href="/reportar">Reportar Fraude</Link>
              </Button>
              <Button asChild variant="outline">
                <Link href="/ayuda">Centro de Ayuda</Link>
              </Button>
            </div>
          </div>
        </div>
      </section>

      {/* Contact */}
      <section className="border-border bg-muted/50 border-t py-12">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-2xl">
            <h2 className="text-foreground mb-6 text-center text-xl font-bold">
              Equipo de Seguridad OKLA
            </h2>
            <div className="grid gap-4 sm:grid-cols-2">
              <a
                href="mailto:seguridad@okla.com.do"
                className="bg-card flex items-center gap-3 rounded-lg p-4 shadow-sm transition-shadow hover:shadow-md"
              >
                <Mail className="text-primary h-5 w-5" />
                <div>
                  <div className="text-foreground font-medium">Email</div>
                  <div className="text-muted-foreground text-sm">seguridad@okla.com.do</div>
                </div>
              </a>
              <a
                href="tel:+18098501234"
                className="bg-card flex items-center gap-3 rounded-lg p-4 shadow-sm transition-shadow hover:shadow-md"
              >
                <Phone className="text-primary h-5 w-5" />
                <div>
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
