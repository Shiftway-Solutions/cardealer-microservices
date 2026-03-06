/**
 * FAQ Page — Preguntas Frecuentes
 *
 * Route: /faq
 */

import Link from 'next/link';
import { HelpCircle, ChevronDown, ArrowRight } from 'lucide-react';
import { Button } from '@/components/ui/button';

export const metadata = {
  title: 'Preguntas Frecuentes | OKLA',
  description:
    'Respuestas a las preguntas más comunes sobre OKLA: cómo comprar, vender, publicar anuncios, pagos y seguridad en el marketplace de vehículos de RD.',
  keywords: [
    'preguntas frecuentes OKLA',
    'FAQ vehículos RD',
    'ayuda marketplace autos',
    'comprar vehículo dominicana',
    'vender carro OKLA',
  ],
};

const faqs = [
  {
    category: 'Comprar Vehículos',
    questions: [
      {
        q: '¿Cómo puedo buscar vehículos en OKLA?',
        a: 'Usa el buscador en la página principal o ve a /vehiculos. Puedes filtrar por marca, modelo, año, precio, combustible, transmisión, provincia y más. También puedes guardar búsquedas y recibir alertas cuando hay nuevos vehículos que coincidan.',
      },
      {
        q: '¿Los precios son negociables?',
        a: 'Sí, la mayoría de los precios son negociables. Puedes contactar al vendedor directamente a través del sistema de mensajería de OKLA para iniciar una negociación.',
      },
      {
        q: '¿Cómo verifico que un vendedor es legítimo?',
        a: 'Los vendedores verificados tienen una insignia azul en su perfil, lo que indica que completaron el proceso KYC (verificación de identidad con cédula). Siempre revisa el perfil, historial de anuncios y reseñas del vendedor antes de cerrar un trato.',
      },
      {
        q: '¿Puedo pagar a través de OKLA?',
        a: 'OKLA facilita pagos seguros a través de pasarelas locales (Azul/Banco Popular, PixelPay). Sin embargo, te recomendamos no pagar el total hasta completar el traspaso y la inspección del vehículo.',
      },
    ],
  },
  {
    category: 'Vender Vehículos',
    questions: [
      {
        q: '¿Cuánto cuesta publicar un vehículo?',
        a: 'Los vendedores individuales pagan RD$29 por publicación. Este costo incluye fotos ilimitadas, aparición en los resultados de búsqueda y herramientas de comunicación con compradores.',
      },
      {
        q: '¿Cuánto tiempo está activo mi anuncio?',
        a: 'Los anuncios individuales están activos por 30 días. Puedes renovarlos o desactivarlos en cualquier momento desde tu cuenta en /mis-vehiculos.',
      },
      {
        q: '¿Necesito verificar mi identidad para vender?',
        a: 'Para mayor confianza de los compradores, te recomendamos completar la verificación KYC. Los vendedores verificados reciben más contactos y cierran tratos más rápido.',
      },
      {
        q: '¿Puedo publicar múltiples vehículos?',
        a: 'Los vendedores individuales pueden publicar hasta 5 vehículos a la vez. Si tienes más vehículos, considera registrarte como dealer para acceder a planes con inventario ilimitado.',
      },
    ],
  },
  {
    category: 'Dealers',
    questions: [
      {
        q: '¿Qué beneficios tienen los dealers en OKLA?',
        a: 'Los dealers tienen acceso a inventario ilimitado, panel de analytics, herramientas de CRM, importación masiva de vehículos, posicionamiento destacado y soporte prioritario. Los planes van de RD$49 a RD$299/mes.',
      },
      {
        q: '¿Cómo me registro como dealer?',
        a: 'Ve a /dealer/registro y completa el formulario con los datos de tu agencia. Necesitarás tu RNC, documentos de la empresa y completar la verificación. El proceso toma menos de 48 horas.',
      },
    ],
  },
  {
    category: 'Cuenta y Seguridad',
    questions: [
      {
        q: '¿Cómo cambio mi contraseña?',
        a: 'Ve a /cuenta/seguridad para cambiar tu contraseña. También puedes activar la verificación en dos pasos (2FA) para mayor seguridad.',
      },
      {
        q: '¿Qué hago si olvidé mi contraseña?',
        a: 'Haz clic en "¿Olvidaste tu contraseña?" en la página de login. Te enviaremos un enlace de recuperación a tu correo electrónico registrado.',
      },
      {
        q: '¿Cómo reporto un fraude o anuncio sospechoso?',
        a: 'Puedes reportar directamente desde el anuncio usando el botón "Reportar", o ir a /reportar para casos más graves. Nuestro equipo de seguridad actúa en menos de 24 horas.',
      },
      {
        q: '¿OKLA protege mis datos personales?',
        a: 'Sí. OKLA cumple con la Ley 172-13 de Protección de Datos Personales de República Dominicana. Tus datos están cifrados y nunca se venden a terceros. Ver nuestra Política de Privacidad.',
      },
    ],
  },
];

export default function FaqPage() {
  // Generate FAQPage JSON-LD schema dynamically from the faqs array
  const faqJsonLd = {
    '@context': 'https://schema.org',
    '@type': 'FAQPage',
    mainEntity: faqs.flatMap(section =>
      section.questions.map(item => ({
        '@type': 'Question',
        name: item.q,
        acceptedAnswer: {
          '@type': 'Answer',
          text: item.a,
        },
      }))
    ),
  };

  return (
    <div className="bg-muted/50 min-h-screen">
      {/* FAQPage Schema.org Structured Data */}
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(faqJsonLd) }}
      />

      {/* Hero */}
      <section className="bg-gradient-to-br from-[#00A870] to-[#007850] py-14 text-white">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-2xl text-center">
            <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-full bg-white/20">
              <HelpCircle className="h-7 w-7" />
            </div>
            <h1 className="text-4xl font-bold">Preguntas Frecuentes</h1>
            <p className="mt-3 text-white/90">
              Encuentra respuestas rápidas a las dudas más comunes sobre OKLA.
            </p>
          </div>
        </div>
      </section>

      {/* FAQ Content */}
      <section className="py-12">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-3xl space-y-8">
            {faqs.map((section, sIndex) => (
              <div key={sIndex}>
                <h2 className="text-foreground mb-4 text-xl font-bold">{section.category}</h2>
                <div className="space-y-3">
                  {section.questions.map((item, qIndex) => (
                    <details
                      key={qIndex}
                      className="group border-border bg-card open:border-primary/30 rounded-lg border shadow-sm"
                    >
                      <summary className="text-foreground flex cursor-pointer list-none items-center justify-between gap-4 p-5 font-medium">
                        <span>{item.q}</span>
                        <ChevronDown className="text-muted-foreground h-4 w-4 flex-shrink-0 transition-transform group-open:rotate-180" />
                      </summary>
                      <div className="border-border text-muted-foreground border-t px-5 pt-3 pb-5 text-sm">
                        {item.a}
                      </div>
                    </details>
                  ))}
                </div>
              </div>
            ))}
          </div>

          {/* Still have questions */}
          <div className="bg-primary/5 border-primary/20 mx-auto mt-12 max-w-2xl rounded-2xl border p-8 text-center">
            <h2 className="text-foreground text-xl font-bold">¿No encontraste tu respuesta?</h2>
            <p className="text-muted-foreground mt-2">
              Visita nuestro Centro de Ayuda completo o habla con Ana, nuestra asistente IA
              disponible 24/7.
            </p>
            <div className="mt-5 flex flex-col gap-3 sm:flex-row sm:justify-center">
              <Button asChild className="bg-primary hover:bg-primary/90">
                <Link href="/ayuda">
                  Centro de Ayuda
                  <ArrowRight className="ml-2 h-4 w-4" />
                </Link>
              </Button>
              <Button asChild variant="outline">
                <Link href="/contacto">Contactar Soporte</Link>
              </Button>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
