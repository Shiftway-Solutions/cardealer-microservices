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
  title: 'Términos y Condiciones',
  description:
    'Términos y condiciones de uso de la plataforma OKLA, marketplace de vehículos en República Dominicana.',
};

// =============================================================================
// MAIN COMPONENT
// =============================================================================

export default function TerminosPage() {
  return (
    <div className="bg-muted/50 min-h-screen py-12">
      <div className="container mx-auto px-4">
        <div className="mx-auto max-w-4xl">
          {/* Header */}
          <div className="mb-8">
            <h1 className="text-foreground text-3xl font-bold">Términos y Condiciones</h1>
            <p className="text-muted-foreground mt-2">Última actualización: Marzo 2026 (v2026.1)</p>
          </div>

          {/* Content */}
          <div className="bg-card rounded-lg p-8 shadow-sm">
            <div className="prose prose-gray max-w-none">
              <h2>1. Aceptación de los Términos</h2>
              <p>
                Al acceder y utilizar la plataforma OKLA (en adelante, &ldquo;la Plataforma&rdquo;),
                usted acepta estar sujeto a estos Términos y Condiciones de Uso. Si no está de
                acuerdo con alguna parte de estos términos, no podrá acceder al servicio.
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

              <h2>14. Protección al Consumidor (Ley 358-05)</h2>
              <p>
                En cumplimiento con la Ley No. 358-05 sobre Protección de los Derechos del
                Consumidor o Usuario de la República Dominicana, los usuarios de OKLA tienen derecho
                a:
              </p>
              <ul>
                <li>Recibir información clara, veraz y oportuna sobre los servicios ofrecidos</li>
                <li>
                  Presentar reclamaciones ante el Instituto Nacional de Protección de los Derechos
                  del Consumidor (<strong>Pro Consumidor</strong>) en caso de disputas no resueltas
                  directamente con OKLA
                </li>
                <li>Ser protegidos contra publicidad engañosa o prácticas comerciales desleales</li>
                <li>
                  La limitación de responsabilidad establecida en la Sección 8 de estos Términos no
                  restringe ni sustituye los derechos que la Ley 358-05 otorga a los consumidores
                </li>
              </ul>
              <p>
                Para contactar a Pro Consumidor: <strong>Tel. 809-567-7755</strong> o visitar{' '}
                <a
                  href="https://www.proconsumidor.gob.do"
                  target="_blank"
                  rel="noopener noreferrer"
                >
                  www.proconsumidor.gob.do
                </a>
              </p>

              <h2>15. Cláusula de Exoneración — Responsabilidad del Vendedor/Dealer</h2>
              <p>
                OKLA opera exclusivamente como intermediario tecnológico entre compradores y
                vendedores de vehículos. Al publicar un anuncio a través de la Plataforma, el
                vendedor o dealer reconoce y acepta expresamente que:
              </p>
              <ul>
                <li>
                  <strong>Veracidad de la información:</strong> El vendedor es el único responsable
                  de la exactitud, integridad y veracidad de toda la información proporcionada en el
                  anuncio, incluyendo pero no limitándose a: marca, modelo, año, kilometraje,
                  condición, precio, historial de accidentes y estado mecánico.
                </li>
                <li>
                  <strong>Propiedad legítima:</strong> El vendedor declara ser el propietario
                  legítimo del vehículo o contar con autorización legal escrita del propietario para
                  su venta.
                </li>
                <li>
                  <strong>Ausencia de gravámenes ocultos:</strong> El vendedor garantiza que ha
                  informado de todo gravamen, embargo, prenda o restricción legal que pese sobre el
                  vehículo.
                </li>
                <li>
                  <strong>Exoneración de OKLA:</strong> OKLA no inspecciona, verifica ni certifica
                  los vehículos publicados. En consecuencia, OKLA queda expresamente exonerada de
                  cualquier responsabilidad civil, penal o administrativa derivada de información
                  inexacta, engañosa u omitida por el vendedor.
                </li>
                <li>
                  <strong>Indemnización reforzada:</strong> El vendedor se compromete a indemnizar a
                  OKLA, sus accionistas, directores, empleados y representantes legales, por
                  cualquier daño, pérdida, sanción o gasto (incluyendo honorarios legales) que
                  resulte de reclamaciones de terceros relacionadas con la información publicada o
                  con la transacción del vehículo.
                </li>
                <li>
                  <strong>Confirmación obligatoria:</strong> La publicación de cada anuncio requiere
                  la aceptación explícita de esta cláusula mediante un checkbox obligatorio. La
                  fecha, hora, dirección IP y versión de estos Términos aceptados quedan registrados
                  en el sistema como evidencia legal.
                </li>
              </ul>
              <p>
                Esta cláusula de exoneración complementa, y no reemplaza, las disposiciones
                establecidas en las Secciones 5 (Publicación de Anuncios), 8 (Limitación de
                Responsabilidad) y 9 (Indemnización) de estos Términos.
              </p>

              <h2 id="proteccion-datos">16. Protección de Datos Personales (Ley 172-13)</h2>
              <p>
                En cumplimiento con la Ley No. 172-13 que tiene por objeto la protección integral de
                los datos personales asentados en archivos, registros públicos, bancos de datos u
                otros medios técnicos de tratamiento de datos destinados a dar informes, sean estos
                públicos o privados, OKLA garantiza a sus usuarios:
              </p>
              <ul>
                <li>
                  <strong>Derecho de acceso:</strong> Los usuarios pueden solicitar información
                  sobre qué datos personales suyos son tratados por OKLA.
                </li>
                <li>
                  <strong>Derecho de rectificación:</strong> Los usuarios pueden solicitar la
                  corrección de datos inexactos o incompletos.
                </li>
                <li>
                  <strong>Derecho de supresión:</strong> Los usuarios pueden solicitar la
                  eliminación de sus datos personales, sujeto a obligaciones legales de retención.
                </li>
                <li>
                  <strong>Derecho de portabilidad:</strong> Los usuarios pueden solicitar una copia
                  exportable de sus datos personales en formato estándar.
                </li>
                <li>
                  <strong>Cifrado de datos sensibles:</strong> Los datos personales identificables
                  (nombre, teléfono, cédula) son cifrados en reposo mediante AES-256-GCM.
                </li>
                <li>
                  <strong>Anonimización en logs:</strong> Los registros del sistema no almacenan
                  datos personales en texto plano.
                </li>
              </ul>
              <p>
                Para ejercer estos derechos, contacte a nuestro oficial de protección de datos:{' '}
                <a href="mailto:privacidad@okla.com.do">privacidad@okla.com.do</a>
              </p>

              <h2>17. Contacto</h2>
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
          <div className="text-muted-foreground mt-8 flex justify-center gap-6 text-sm">
            <Link href="/privacidad" className="hover:text-primary">
              Política de Privacidad
            </Link>
            <Link href="/contacto" className="hover:text-primary">
              Contacto
            </Link>
            <Link href="/" className="hover:text-primary">
              Volver al inicio
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
