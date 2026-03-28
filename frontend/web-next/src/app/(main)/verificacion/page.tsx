/**
 * Verificación de Dealers — Página pública
 *
 * Explica los 4 criterios que un dealer debe cumplir para obtener
 * el badge "Dealer Verificado OKLA" (✓ azul).
 *
 * Route: /verificacion → okla.do/verificacion
 */

import Link from 'next/link';
import {
  ShieldCheck,
  BadgeCheck,
  Phone,
  Image,
  AlertTriangle,
  Building2,
  CheckCircle2,
  ArrowRight,
} from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';

export const metadata = {
  title: 'Verificación de Dealers',
  description:
    'Conoce los 4 criterios que un dealer debe cumplir para obtener el badge de Dealer Verificado en OKLA: RNC en DGII, WhatsApp verificado, listings moderados y sin reportes de fraude.',
  openGraph: {
    title: 'Verificación de Dealers | OKLA',
    description: 'Transparencia total: así verificamos a los dealers en OKLA República Dominicana.',
    url: 'https://okla.do/verificacion',
    type: 'website',
  },
};

const verificationCriteria = [
  {
    id: 1,
    icon: Building2,
    title: 'RNC activo en DGII',
    description:
      'El Registro Nacional del Contribuyente (RNC) del dealer debe estar registrado y activo en la Dirección General de Impuestos Internos (DGII). Esto confirma que el dealer opera como una entidad comercial legítima en República Dominicana.',
    details: [
      'Verificamos el RNC directamente con la DGII',
      'El contribuyente debe tener estado "Activo"',
      'Se valida que el nombre comercial corresponda al dealer',
    ],
    color: 'text-blue-600',
    bgColor: 'bg-blue-50',
    borderColor: 'border-blue-200',
  },
  {
    id: 2,
    icon: Phone,
    title: 'WhatsApp verificado por OTP',
    description:
      'El número de WhatsApp Business del dealer debe ser verificado mediante un código OTP (One-Time Password) enviado al número registrado. Esto garantiza que el dealer tiene control real del número de contacto.',
    details: [
      'Enviamos un código de 6 dígitos al WhatsApp del dealer',
      'El código expira en 10 minutos',
      'Solo números de República Dominicana (+1-809, +1-829, +1-849)',
    ],
    color: 'text-green-600',
    bgColor: 'bg-green-50',
    borderColor: 'border-green-200',
  },
  {
    id: 3,
    icon: Image,
    title: 'Mínimo 10 listings con fotos reales',
    description:
      'El dealer debe tener al menos 10 vehículos publicados activos con fotografías que hayan pasado nuestro proceso de moderación. Esto demuestra actividad comercial real y compromiso con la calidad.',
    details: [
      'Las fotos son revisadas por nuestro sistema de moderación',
      'Se verifican fotos originales (no de stock ni de internet)',
      'Los vehículos deben estar en estado "Activo" (publicados)',
    ],
    color: 'text-purple-600',
    bgColor: 'bg-purple-50',
    borderColor: 'border-purple-200',
  },
  {
    id: 4,
    icon: AlertTriangle,
    title: 'Sin reportes de fraude (90 días)',
    description:
      'El dealer no debe tener reportes activos de fraude o estafa en los últimos 90 días. Si un dealer acumula reportes verificados, el badge se revoca automáticamente hasta que se resuelvan.',
    details: [
      'Monitoreamos reportes de compradores en tiempo real',
      'Investigamos cada reporte de fraude o estafa',
      '3 reportes activos = suspensión automática del listing',
    ],
    color: 'text-orange-600',
    bgColor: 'bg-orange-50',
    borderColor: 'border-orange-200',
  },
];

const faqItems = [
  {
    question: '¿Cuánto cuesta la verificación?',
    answer:
      'La verificación es completamente gratuita. Está disponible para todos los dealers registrados en OKLA, sin importar su plan (Libre, Visible, Pro o Elite).',
  },
  {
    question: '¿Cuánto tarda el proceso?',
    answer:
      'La verificación de WhatsApp es instantánea. La verificación del RNC toma hasta 24 horas hábiles. Los criterios de listings y reportes se evalúan automáticamente en tiempo real.',
  },
  {
    question: '¿Qué pasa si pierdo el badge?',
    answer:
      'El badge se evalúa periódicamente. Si dejas de cumplir algún criterio (por ejemplo, si se reporta fraude o reduces tus listings activos), el badge se revoca hasta que vuelvas a cumplir los 4 criterios.',
  },
  {
    question: '¿El badge aparece en mi perfil público?',
    answer:
      'Sí. Los compradores ven un ícono ✓ azul junto al nombre de tu dealer en los resultados de búsqueda, en tu perfil público y en cada listing de vehículo.',
  },
  {
    question: '¿Puedo apelar la revocación del badge?',
    answer:
      'Sí. Si consideras que la revocación fue incorrecta, puedes contactar a nuestro equipo de soporte en verificacion@okla.do con tu caso. Revisamos cada apelación en un plazo de 48 horas.',
  },
];

export default function VerificacionPage() {
  return (
    <div className="min-h-screen bg-gray-50">
      {/* Hero */}
      <section className="bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-800 py-16 text-white md:py-24">
        <div className="container mx-auto max-w-4xl px-4 text-center">
          <div className="mb-6 inline-flex items-center gap-2 rounded-full bg-white/10 px-4 py-2 backdrop-blur-sm">
            <ShieldCheck className="h-5 w-5" />
            <span className="text-sm font-medium">Programa de Verificación</span>
          </div>
          <h1 className="mb-4 text-3xl leading-tight font-bold md:text-5xl">
            Dealer Verificado OKLA{' '}
            <BadgeCheck className="inline h-8 w-8 text-blue-200 md:h-10 md:w-10" />
          </h1>
          <p className="mx-auto max-w-2xl text-lg leading-relaxed text-blue-100 md:text-xl">
            Transparencia total. Estos son los 4 criterios que un dealer debe cumplir para obtener
            el badge de verificación y generar confianza con los compradores.
          </p>
        </div>
      </section>

      {/* Criteria Cards */}
      <section className="relative z-10 container mx-auto -mt-8 max-w-4xl px-4">
        <div className="grid gap-6">
          {verificationCriteria.map(criterion => {
            const Icon = criterion.icon;
            return (
              <Card
                key={criterion.id}
                className={`border-l-4 ${criterion.borderColor} shadow-sm transition-shadow hover:shadow-md`}
              >
                <CardContent className="p-6">
                  <div className="flex items-start gap-4">
                    <div
                      className={`${criterion.bgColor} ${criterion.color} shrink-0 rounded-xl p-3`}
                    >
                      <Icon className="h-6 w-6" />
                    </div>
                    <div className="flex-1">
                      <div className="mb-2 flex items-center gap-2">
                        <span className="text-xs font-bold tracking-wider text-gray-400 uppercase">
                          Criterio {criterion.id}
                        </span>
                      </div>
                      <h3 className="mb-2 text-lg font-semibold text-gray-900">
                        {criterion.title}
                      </h3>
                      <p className="mb-4 text-gray-600">{criterion.description}</p>
                      <ul className="space-y-2">
                        {criterion.details.map((detail, i) => (
                          <li key={i} className="flex items-start gap-2 text-sm text-gray-500">
                            <CheckCircle2
                              className={`h-4 w-4 ${criterion.color} mt-0.5 shrink-0`}
                            />
                            <span>{detail}</span>
                          </li>
                        ))}
                      </ul>
                    </div>
                  </div>
                </CardContent>
              </Card>
            );
          })}
        </div>
      </section>

      {/* How it works */}
      <section className="container mx-auto max-w-4xl px-4 py-16">
        <h2 className="mb-8 text-center text-2xl font-bold text-gray-900">¿Cómo funciona?</h2>
        <div className="grid gap-6 md:grid-cols-3">
          <div className="p-6 text-center">
            <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-blue-100 text-lg font-bold text-blue-700">
              1
            </div>
            <h3 className="mb-2 font-semibold text-gray-900">Regístrate como dealer</h3>
            <p className="text-sm text-gray-500">
              Crea tu cuenta de dealer en OKLA con tu RNC y datos comerciales.
            </p>
          </div>
          <div className="p-6 text-center">
            <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-blue-100 text-lg font-bold text-blue-700">
              2
            </div>
            <h3 className="mb-2 font-semibold text-gray-900">Cumple los 4 criterios</h3>
            <p className="text-sm text-gray-500">
              Verifica tu RNC, confirma tu WhatsApp, publica 10+ vehículos y mantén un historial
              limpio.
            </p>
          </div>
          <div className="p-6 text-center">
            <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-blue-100 text-lg font-bold text-blue-700">
              3
            </div>
            <h3 className="mb-2 font-semibold text-gray-900">Obtén tu badge</h3>
            <p className="text-sm text-gray-500">
              El badge aparece automáticamente en tu perfil y listings cuando cumples los 4
              criterios.
            </p>
          </div>
        </div>
      </section>

      {/* FAQ */}
      <section className="bg-white py-16">
        <div className="container mx-auto max-w-3xl px-4">
          <h2 className="mb-8 text-center text-2xl font-bold text-gray-900">
            Preguntas frecuentes
          </h2>
          <div className="space-y-4">
            {faqItems.map((item, i) => (
              <div key={i} className="rounded-lg border border-gray-200 p-5">
                <h3 className="mb-2 font-semibold text-gray-900">{item.question}</h3>
                <p className="text-sm leading-relaxed text-gray-600">{item.answer}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="bg-gray-50 py-12">
        <div className="container mx-auto max-w-2xl px-4 text-center">
          <h2 className="mb-3 text-xl font-bold text-gray-900">¿Eres dealer?</h2>
          <p className="mb-6 text-gray-600">
            Regístrate en OKLA y comienza el proceso de verificación hoy mismo.
          </p>
          <div className="flex flex-col justify-center gap-3 sm:flex-row">
            <Link href="/dealer">
              <Button size="lg" className="gap-2">
                Registrarme como dealer
                <ArrowRight className="h-4 w-4" />
              </Button>
            </Link>
            <Link href="/contacto">
              <Button size="lg" variant="outline" className="gap-2">
                Contactar soporte
              </Button>
            </Link>
          </div>
          <p className="mt-6 text-xs text-gray-400">
            ¿Tienes preguntas sobre la verificación? Escríbenos a{' '}
            <a href="mailto:verificacion@okla.do" className="text-blue-600 hover:underline">
              verificacion@okla.do
            </a>
          </p>
        </div>
      </section>
    </div>
  );
}
