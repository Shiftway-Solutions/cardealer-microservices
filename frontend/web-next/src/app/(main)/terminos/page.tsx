/**
 * Terms of Service Page
 *
 * Legal terms and conditions
 *
 * Route: /terminos
 */

import Link from 'next/link';

// =============================================================================
// METADATA
// =============================================================================

export const metadata = {
  title: 'Términos y Condiciones | OKLA',
  description:
    'Términos y condiciones de uso de la plataforma OKLA, marketplace de vehículos en República Dominicana.',
};

// =============================================================================
// MAIN COMPONENT
// =============================================================================

export default function TerminosPage() {
  return (
    <div className="min-h-screen bg-muted/50 py-12">
      <div className="container mx-auto px-4">
        <div className="mx-auto max-w-4xl">
          {/* Header */}
          <div className="mb-8">
            <h1 className="text-3xl font-bold text-foreground">Términos y Condiciones</h1>
            <p className="mt-2 text-muted-foreground">Última actualización: Enero 2026</p>
          </div>

          {/* Content */}
          <div className="rounded-lg bg-card p-8 shadow-sm">
            <div className="prose prose-gray max-w-none">
              <h2>1. Aceptación de los Términos</h2>
              <p>
                Al acceder y utilizar la plataforma OKLA (en adelante, &ldquo;la Plataforma&rdquo;), usted
                acepta estar sujeto a estos Términos y Condiciones de Uso. Si no está de acuerdo con
                alguna parte de estos términos, no podrá acceder al servicio.
              </p>

              <h2>2. Descripción del Servicio</h2>
              <p>
                OKLA es un marketplace en línea que conecta compradores y vendedores de vehículos en
                República Dominicana. Proporcionamos una plataforma para que los usuarios publiquen
                anuncios de vehículos y se comuniquen con potenciales compradores o vendedores.
              </p>
              <p>
                OKLA no es parte de ninguna transacción entre usuarios, no inspecciona físicamente
                los vehículos, y no garantiza la exactitud de la información proporcionada por los
                vendedores.
              </p>

              <h2>3. Registro de Usuario</h2>
              <p>
                Para utilizar ciertas funciones de la Plataforma, deberá crear una cuenta. Usted es
                responsable de:
              </p>
              <ul>
                <li>Proporcionar información precisa y completa</li>
                <li>Mantener la confidencialidad de su contraseña</li>
                <li>Todas las actividades que ocurran bajo su cuenta</li>
                <li>Notificarnos inmediatamente de cualquier uso no autorizado</li>
              </ul>

              <h2>4. Uso Aceptable</h2>
              <p>Al utilizar la Plataforma, usted acepta no:</p>
              <ul>
                <li>Publicar información falsa o engañosa</li>
                <li>Publicar vehículos que no son de su propiedad o sin autorización</li>
                <li>Utilizar la plataforma para actividades ilegales</li>
                <li>Interferir con el funcionamiento de la Plataforma</li>
                <li>Recopilar información de otros usuarios sin su consentimiento</li>
                <li>Publicar contenido ofensivo, difamatorio o ilegal</li>
              </ul>

              <h2>5. Publicación de Anuncios</h2>
              <p>Al publicar un anuncio de vehículo, usted declara y garantiza que:</p>
              <ul>
                <li>Es el propietario legítimo del vehículo o tiene autorización para venderlo</li>
                <li>La información proporcionada es precisa y completa</li>
                <li>Las fotos son del vehículo real que se está vendiendo</li>
                <li>El vehículo no tiene gravámenes, embargos o restricciones legales ocultas</li>
                <li>Cumple con todas las leyes y regulaciones aplicables</li>
              </ul>

              <h2>6. Tarifas y Pagos</h2>
              <p>
                Algunos servicios de la Plataforma son gratuitos, mientras que otros requieren pago.
                Las tarifas aplicables se mostrarán antes de completar cualquier transacción.
              </p>
              <p>
                Los pagos procesados a través de la Plataforma están sujetos a nuestras políticas de
                reembolso disponibles en nuestro Centro de Ayuda.
              </p>

              <h2>7. Contenido del Usuario</h2>
              <p>
                Usted retiene la propiedad de todo el contenido que publique en la Plataforma. Sin
                embargo, al publicar contenido, nos otorga una licencia mundial, no exclusiva, libre
                de regalías para usar, modificar, reproducir y distribuir dicho contenido en
                conexión con el servicio.
              </p>

              <h2>8. Limitación de Responsabilidad</h2>
              <p>OKLA proporciona la Plataforma &ldquo;tal como está&rdquo; y no garantiza:</p>
              <ul>
                <li>La precisión de la información proporcionada por los usuarios</li>
                <li>La calidad, seguridad o legalidad de los vehículos anunciados</li>
                <li>La capacidad de los compradores o vendedores para completar transacciones</li>
                <li>El funcionamiento ininterrumpido de la Plataforma</li>
              </ul>
              <p>
                En ningún caso OKLA será responsable por daños indirectos, incidentales, especiales
                o consecuentes.
              </p>

              <h2>9. Indemnización</h2>
              <p>
                Usted acepta indemnizar y mantener indemne a OKLA, sus directores, empleados y
                agentes, de cualquier reclamación o demanda, incluyendo honorarios razonables de
                abogados, que surja de su uso de la Plataforma o violación de estos términos.
              </p>

              <h2>10. Propiedad Intelectual</h2>
              <p>
                La Plataforma y su contenido original, características y funcionalidad son propiedad
                de OKLA y están protegidos por leyes de propiedad intelectual nacionales e
                internacionales.
              </p>

              <h2>11. Modificaciones</h2>
              <p>
                Nos reservamos el derecho de modificar estos términos en cualquier momento. Las
                modificaciones entrarán en vigor inmediatamente después de su publicación en la
                Plataforma. Su uso continuado constituye aceptación de los términos modificados.
              </p>

              <h2>12. Terminación</h2>
              <p>
                Podemos terminar o suspender su cuenta y acceso a la Plataforma de inmediato, sin
                previo aviso, por cualquier violación de estos Términos y Condiciones.
              </p>

              <h2>13. Ley Aplicable</h2>
              <p>
                Estos términos se regirán e interpretarán de acuerdo con las leyes de la República
                Dominicana, sin tener en cuenta sus disposiciones sobre conflictos de leyes.
              </p>

              <h2>14. Contacto</h2>
              <p>Si tiene preguntas sobre estos Términos y Condiciones, contáctenos en:</p>
              <ul>
                <li>
                  Email: <a href="mailto:legal@okla.com.do">legal@okla.com.do</a>
                </li>
                <li>Dirección: Av. Winston Churchill #1099, Santo Domingo, RD</li>
              </ul>
            </div>
          </div>

          {/* Footer Links */}
          <div className="mt-8 flex justify-center gap-6 text-sm text-muted-foreground">
            <Link href="/privacidad" className="hover:text-[#00A870]">
              Política de Privacidad
            </Link>
            <Link href="/contacto" className="hover:text-[#00A870]">
              Contacto
            </Link>
            <Link href="/" className="hover:text-[#00A870]">
              Volver al inicio
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
