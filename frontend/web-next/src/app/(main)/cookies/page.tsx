/**
 * Cookie Policy Page
 *
 * Route: /cookies
 */

import Link from 'next/link';

export const metadata = {
  title: 'Política de Cookies',
  description:
    'Información sobre cómo OKLA utiliza cookies y tecnologías de seguimiento en su plataforma.',
};

export default function CookiesPage() {
  return (
    <div className="bg-muted/50 min-h-screen py-12">
      <div className="container mx-auto px-4">
        <div className="mx-auto max-w-4xl">
          {/* Header */}
          <div className="mb-8">
            <h1 className="text-foreground text-3xl font-bold">Política de Cookies</h1>
            <p className="text-muted-foreground mt-2">Última actualización: Marzo 2026 (v2026.1)</p>
          </div>

          {/* Content */}
          <div className="bg-card rounded-lg p-8 shadow-sm">
            <div className="prose prose-gray text-muted-foreground max-w-none space-y-6">
              <p className="text-base leading-relaxed">
                OKLA utiliza cookies y tecnologías similares para mejorar tu experiencia en nuestra
                plataforma. Esta política explica qué son las cookies, cómo las utilizamos y cómo
                puedes controlarlas.
              </p>

              <section>
                <h2 className="text-foreground text-xl font-semibold">¿Qué son las Cookies?</h2>
                <p className="mt-2">
                  Las cookies son pequeños archivos de texto que se almacenan en tu dispositivo
                  cuando visitas un sitio web. Nos permiten recordar tus preferencias, mantener tu
                  sesión activa y entender cómo utilizas nuestra plataforma.
                </p>
              </section>

              <section>
                <h2 className="text-foreground text-xl font-semibold">
                  Tipos de Cookies que Usamos
                </h2>
                <div className="mt-4 space-y-4">
                  <div className="border-border rounded-lg border p-4">
                    <h3 className="text-foreground font-semibold">🔒 Cookies Esenciales</h3>
                    <p className="mt-1 text-sm">
                      Necesarias para el funcionamiento básico del sitio. Incluyen autenticación de
                      sesión, seguridad y preferencias básicas. No pueden desactivarse.
                    </p>
                  </div>
                  <div className="border-border rounded-lg border p-4">
                    <h3 className="text-foreground font-semibold">📊 Cookies de Análisis</h3>
                    <p className="mt-1 text-sm">
                      Nos ayudan a entender cómo los visitantes interactúan con el sitio, qué
                      páginas son más populares y cómo mejorar la experiencia. Los datos son
                      anónimos y agregados.
                    </p>
                  </div>
                  <div className="border-border rounded-lg border p-4">
                    <h3 className="text-foreground font-semibold">🎯 Cookies de Personalización</h3>
                    <p className="mt-1 text-sm">
                      Recuerdan tus preferencias (idioma, tema, filtros de búsqueda) para
                      personalizar tu experiencia en futuras visitas.
                    </p>
                  </div>
                  <div className="border-border rounded-lg border p-4">
                    <h3 className="text-foreground font-semibold">📣 Cookies de Marketing</h3>
                    <p className="mt-1 text-sm">
                      Utilizadas para mostrarte anuncios relevantes dentro y fuera de OKLA, basados
                      en tus búsquedas e intereses. Puedes optar por no participar.
                    </p>
                  </div>
                </div>
              </section>

              <section>
                <h2 className="text-foreground text-xl font-semibold">
                  Cómo Controlar las Cookies
                </h2>
                <p className="mt-2">
                  Puedes gestionar tus preferencias de cookies en cualquier momento:
                </p>
                <ul className="mt-3 list-disc space-y-1 pl-6">
                  <li>
                    <strong>Configuración del navegador:</strong> La mayoría de los navegadores te
                    permiten bloquear o eliminar cookies en sus opciones de privacidad.
                  </li>
                  <li>
                    <strong>Configuración de OKLA:</strong> Accede a{' '}
                    <Link href="/cuenta/configuracion" className="text-primary hover:underline">
                      Configuración de Cuenta
                    </Link>{' '}
                    para ajustar tus preferencias de cookies.
                  </li>
                  <li>
                    <strong>Opt-out de publicidad:</strong> Visita{' '}
                    <a
                      href="https://optout.networkadvertising.org/"
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-primary hover:underline"
                    >
                      NAI Opt-Out
                    </a>{' '}
                    para desactivar publicidad basada en intereses.
                  </li>
                </ul>
              </section>

              <section>
                <h2 className="text-foreground text-xl font-semibold">Cookies de Terceros</h2>
                <p className="mt-2">
                  Algunos servicios de terceros integrados en OKLA pueden establecer sus propias
                  cookies:
                </p>
                <ul className="mt-3 list-disc space-y-1 pl-6">
                  <li>
                    <strong>Google Analytics:</strong> Análisis de tráfico y comportamiento del
                    usuario
                  </li>
                  <li>
                    <strong>Pasarelas de pago:</strong> Azul, PixelPay (solo durante el proceso de
                    pago)
                  </li>
                  <li>
                    <strong>Redes sociales:</strong> Botones para compartir en Facebook, Instagram,
                    etc.
                  </li>
                </ul>
              </section>

              <section>
                <h2 className="text-foreground text-xl font-semibold">Base Legal (Ley 172-13)</h2>
                <p className="mt-2">
                  El uso de cookies en OKLA se fundamenta en la{' '}
                  <strong>Ley No. 172-13 sobre Protección de Datos de Carácter Personal</strong> de
                  la República Dominicana. Las cookies estrictamente necesarias son procesadas en
                  virtud de nuestro interés legítimo para la prestación del servicio. Las cookies
                  analíticas y de marketing requieren tu consentimiento explícito, el cual puedes
                  otorgar o revocar en cualquier momento desde el panel de preferencias de cookies o
                  escribiéndonos a{' '}
                  <a href="mailto:privacidad@okla.com.do" className="text-primary hover:underline">
                    privacidad@okla.com.do
                  </a>
                  .
                </p>
                <p className="mt-2">
                  Bajo la Ley 172-13, tienes derecho a acceder, rectificar y suprimir tus datos, así
                  como a oponerte al tratamiento. Para ejercer estos derechos, visita nuestra{' '}
                  <a href="/privacidad" className="text-primary hover:underline">
                    Política de Privacidad
                  </a>{' '}
                  o contáctanos directamente.
                </p>
              </section>

              <section>
                <h2 className="text-foreground text-xl font-semibold">
                  Actualizaciones a esta Política
                </h2>
                <p className="mt-2">
                  Podemos actualizar esta política periódicamente. Te notificaremos sobre cambios
                  significativos a través de un aviso en el sitio o por correo electrónico.
                </p>
              </section>

              <section>
                <h2 className="text-foreground text-xl font-semibold">Contáctanos</h2>
                <p className="mt-2">
                  Si tienes preguntas sobre nuestra política de cookies, contáctanos en{' '}
                  <a href="mailto:privacidad@okla.com.do" className="text-primary hover:underline">
                    privacidad@okla.com.do
                  </a>
                  .
                </p>
              </section>
            </div>
          </div>

          {/* Related Links */}
          <div className="mt-6 flex flex-wrap gap-4">
            <Link href="/privacidad" className="text-primary text-sm hover:underline">
              ← Política de Privacidad
            </Link>
            <Link href="/terminos" className="text-primary text-sm hover:underline">
              Términos de Servicio →
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
