/**
 * Privacy Policy Page
 *
 * Privacy policy and data handling information
 *
 * Route: /privacidad
 */

import Link from 'next/link';

// =============================================================================
// METADATA
// =============================================================================

export const metadata = {
  title: 'Política de Privacidad',
  description:
    'Política de privacidad de OKLA. Conoce cómo recopilamos, usamos y protegemos tu información personal.',
};

// =============================================================================
// MAIN COMPONENT
// =============================================================================

export default function PrivacidadPage() {
  return (
    <div className="bg-muted/50 min-h-screen py-12">
      <div className="container mx-auto px-4">
        <div className="mx-auto max-w-4xl">
          {/* Header */}
          <div className="mb-8">
            <h1 className="text-foreground text-3xl font-bold">Política de Privacidad</h1>
            <p className="text-muted-foreground mt-2">Última actualización: Marzo 2026</p>
          </div>

          {/* Content */}
          <div className="bg-card rounded-lg p-8 shadow-sm">
            <div className="prose prose-gray max-w-none">
              <p className="lead">
                En OKLA, respetamos su privacidad y nos comprometemos a proteger su información
                personal. Esta política explica cómo recopilamos, usamos y protegemos sus datos.
              </p>

              <h2>1. Información que Recopilamos</h2>

              <h3>1.1 Información que usted proporciona</h3>
              <ul>
                <li>
                  <strong>Información de cuenta:</strong> nombre, correo electrónico, teléfono,
                  contraseña
                </li>
                <li>
                  <strong>Información de perfil:</strong> foto de perfil, ubicación, preferencias
                </li>
                <li>
                  <strong>Información de anuncios:</strong> datos de vehículos, fotos, descripciones
                </li>
                <li>
                  <strong>Comunicaciones:</strong> mensajes enviados a través de la plataforma
                </li>
                <li>
                  <strong>Información de pago:</strong> datos de tarjeta (procesados por terceros
                  seguros)
                </li>
              </ul>

              <h3>1.2 Información recopilada automáticamente</h3>
              <ul>
                <li>
                  <strong>Datos de uso:</strong> páginas visitadas, búsquedas realizadas, tiempo en
                  el sitio
                </li>
                <li>
                  <strong>Datos del dispositivo:</strong> tipo de dispositivo, sistema operativo,
                  navegador
                </li>
                <li>
                  <strong>Datos de ubicación:</strong> ubicación aproximada basada en IP
                </li>
                <li>
                  <strong>Cookies:</strong> para mejorar su experiencia y análisis
                </li>
              </ul>

              <h2>2. Cómo Usamos su Información</h2>
              <p>Utilizamos su información para:</p>
              <ul>
                <li>Proporcionar, mantener y mejorar nuestros servicios</li>
                <li>Procesar transacciones y enviar notificaciones relacionadas</li>
                <li>Personalizar su experiencia y mostrar contenido relevante</li>
                <li>Comunicarnos con usted sobre actualizaciones, ofertas y novedades</li>
                <li>Detectar y prevenir fraudes y actividades no autorizadas</li>
                <li>Cumplir con obligaciones legales</li>
                <li>Realizar análisis y mejorar nuestros servicios</li>
              </ul>

              <h2>3. Compartir Información</h2>
              <p>Podemos compartir su información con:</p>
              <ul>
                <li>
                  <strong>Otros usuarios:</strong> información de perfil público y anuncios
                </li>
                <li>
                  <strong>Proveedores de servicios:</strong> empresas que nos ayudan a operar
                  (hosting, pagos, email)
                </li>
                <li>
                  <strong>Socios comerciales:</strong> con su consentimiento, para ofertas
                  relevantes
                </li>
                <li>
                  <strong>Autoridades legales:</strong> cuando sea requerido por ley
                </li>
                <li>
                  <strong>En caso de fusión o adquisición:</strong> como parte de una transferencia
                  de activos
                </li>
              </ul>

              <h2>4. Protección de Datos</h2>
              <p>
                Implementamos medidas de seguridad técnicas y organizativas para proteger su
                información, incluyendo:
              </p>
              <ul>
                <li>Encriptación de datos en tránsito y en reposo</li>
                <li>Acceso restringido a información personal</li>
                <li>Monitoreo continuo de seguridad</li>
                <li>Evaluaciones periódicas de vulnerabilidades</li>
              </ul>

              <h2>5. Sus Derechos</h2>
              <p>
                De conformidad con la <strong>Ley No. 172-13</strong> sobre Protección de Datos de
                Carácter Personal de la República Dominicana, usted tiene derecho a:
              </p>
              <ul>
                <li>
                  <strong>Acceso:</strong> obtener una copia de sus datos personales
                </li>
                <li>
                  <strong>Rectificación:</strong> corregir datos inexactos o incompletos
                </li>
                <li>
                  <strong>Eliminación:</strong> solicitar la eliminación de sus datos
                </li>
                <li>
                  <strong>Portabilidad:</strong> recibir sus datos en formato estructurado
                </li>
                <li>
                  <strong>Oposición:</strong> oponerse al procesamiento de sus datos
                </li>
                <li>
                  <strong>Retirar consentimiento:</strong> en cualquier momento, sin afectar la
                  legalidad del procesamiento previo
                </li>
              </ul>
              <p>
                Para ejercer estos derechos, contáctenos en{' '}
                <a href="mailto:privacidad@okla.com.do">privacidad@okla.com.do</a>. Si considera que
                sus derechos no han sido atendidos, puede presentar una queja ante el{' '}
                <strong>
                  Instituto Nacional de Protección de los Derechos del Consumidor (Pro Consumidor)
                </strong>{' '}
                o ante <strong>INDOTEL</strong> (Instituto Dominicano de las Telecomunicaciones),
                autoridad competente en materia de protección de datos en la República Dominicana.
              </p>

              <h2>6. Cookies y Tecnologías Similares</h2>
              <p>Utilizamos cookies para:</p>
              <ul>
                <li>
                  <strong>Cookies esenciales:</strong> necesarias para el funcionamiento del sitio
                </li>
                <li>
                  <strong>Cookies de preferencias:</strong> recordar sus configuraciones
                </li>
                <li>
                  <strong>Cookies analíticas:</strong> entender cómo usa el sitio
                </li>
                <li>
                  <strong>Cookies de marketing:</strong> mostrar anuncios relevantes
                </li>
              </ul>
              <p>
                Puede gestionar sus preferencias de cookies en la configuración de su navegador.
              </p>

              <h2>7. Inteligencia Artificial y Procesamiento Automatizado</h2>
              <p>
                OKLA utiliza herramientas de inteligencia artificial para mejorar la experiencia del
                usuario. Tiene derecho a solicitar revisión humana de cualquier decisión
                automatizada que le afecte significativamente, contactándonos en{' '}
                <a href="mailto:privacidad@okla.com.do">privacidad@okla.com.do</a>.
              </p>
              <ul>
                <li>
                  <strong>Búsqueda inteligente (SearchAgent):</strong> Sus consultas de búsqueda son
                  procesadas por inteligencia artificial (Anthropic Claude) para interpretar
                  lenguaje natural y extraer filtros de vehículos. Las consultas no se almacenan de
                  forma identificable.
                </li>
                <li>
                  <strong>Chatbot de vendedores (DealerChat):</strong> Las conversaciones con el
                  agente IA son procesadas por Anthropic Claude. Los mensajes pueden ser retenidos
                  temporalmente para el contexto de la conversación (máximo 24 horas).
                </li>
                <li>
                  <strong>Recomendaciones (RecoAgent):</strong> Generamos sugerencias de vehículos
                  basadas en su historial de navegación y preferencias mediante análisis
                  automatizado.
                </li>
                <li>
                  <strong>Estimación de precios (PricingAgent):</strong> Los precios estimados se
                  calculan mediante modelos de IA basados en datos del mercado dominicano de
                  vehículos. No constituyen una valoración oficial.
                </li>
              </ul>
              <p>
                Los datos procesados por nuestros sistemas de IA son tratados por Anthropic, Inc.
                como subencargado, bajo acuerdos de procesamiento de datos que garantizan estándares
                equivalentes de protección conforme a la Ley No. 172-13.
              </p>

              <h2>8. Retención de Datos</h2>
              <p>Conservamos su información según los siguientes plazos:</p>
              <ul>
                <li>
                  <strong>Datos de cuenta activa:</strong> mientras su cuenta permanezca activa
                </li>
                <li>
                  <strong>Datos tras cierre de cuenta:</strong> hasta 5 años para obligaciones
                  legales, contables y fiscales
                </li>
                <li>
                  <strong>Registros de transacciones:</strong> 10 años conforme a legislación
                  tributaria dominicana
                </li>
                <li>
                  <strong>Conversaciones de chatbot:</strong> máximo 24 horas en memoria activa;
                  logs anonimizados hasta 90 días para mejora del servicio
                </li>
                <li>
                  <strong>Historial de búsquedas:</strong> hasta 12 meses en forma anonimizada
                </li>
              </ul>

              <h2>9. Transferencias Internacionales</h2>
              <p>
                Sus datos pueden ser transferidos y procesados en servidores ubicados fuera de
                República Dominicana. Cuando esto ocurra, nos aseguraremos de que existan
                protecciones adecuadas según las leyes aplicables.
              </p>

              <h2>10. Menores de Edad</h2>
              <p>
                Nuestros servicios no están dirigidos a menores de 18 años. No recopilamos
                intencionalmente información de menores. Si tiene conocimiento de que un menor nos
                ha proporcionado datos, contáctenos para eliminarlos.
              </p>

              <h2>11. Cambios a esta Política</h2>
              <p>
                Podemos actualizar esta política periódicamente. Le notificaremos cualquier cambio
                significativo publicando la nueva política en esta página y, si es necesario, por
                correo electrónico.
              </p>

              <h2>12. Contacto</h2>
              <p>Si tiene preguntas sobre esta Política de Privacidad, contáctenos:</p>
              <ul>
                <li>
                  Email: <a href="mailto:privacidad@okla.com.do">privacidad@okla.com.do</a>
                </li>
                <li>Teléfono: +1 (809) 555-1234</li>
                <li>Dirección: Av. Winston Churchill #1099, Santo Domingo, RD</li>
              </ul>
            </div>
          </div>

          {/* Footer Links */}
          <div className="text-muted-foreground mt-8 flex justify-center gap-6 text-sm">
            <Link href="/terminos" className="hover:text-primary">
              Términos y Condiciones
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
