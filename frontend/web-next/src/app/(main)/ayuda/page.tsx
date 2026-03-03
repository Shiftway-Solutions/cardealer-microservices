/**
 * Help/FAQ Page
 *
 * Frequently asked questions and help center
 *
 * Route: /ayuda
 */

'use client';

import * as React from 'react';
import {
  Search,
  ChevronDown,
  Car,
  CreditCard,
  Shield,
  UserCircle,
  Store,
  MessageCircle,
  Phone,
  Mail,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { cn } from '@/lib/utils';
import { useSiteConfig } from '@/providers/site-config-provider';

// =============================================================================
// TYPES & DATA
// =============================================================================

interface FAQItem {
  question: string;
  answer: string;
}

interface FAQCategory {
  id: string;
  name: string;
  icon: React.ElementType;
  items: FAQItem[];
}

const faqCategories: FAQCategory[] = [
  {
    id: 'comprar',
    name: 'Comprar un vehículo',
    icon: Car,
    items: [
      {
        question: '¿Cómo busco vehículos en OKLA?',
        answer:
          'Puedes usar nuestra barra de búsqueda en la página principal o ir a la sección "Comprar" para usar filtros avanzados como marca, modelo, año, precio, ubicación y más. También puedes ordenar los resultados por precio, fecha o relevancia.',
      },
      {
        question: '¿Qué significa el Deal Rating (valoración del precio)?',
        answer:
          'El Deal Rating es nuestro sistema patentado que analiza miles de listados y datos de mercado para determinar si el precio de un vehículo es justo. Los ratings van desde "Excelente" (verde) hasta "Precio Alto" (rojo), ayudándote a identificar las mejores ofertas.',
      },
      {
        question: '¿Cómo contacto a un vendedor?',
        answer:
          'En cada anuncio encontrarás un botón "Contactar vendedor". Puedes enviar un mensaje a través de nuestra plataforma, llamar directamente o, si está disponible, escribir por WhatsApp. Recomendamos siempre comunicarte primero antes de acordar una cita.',
      },
      {
        question: '¿OKLA verifica los vehículos?',
        answer:
          'Los vehículos con el badge "Verificado" han pasado por nuestra inspección básica de documentos. Sin embargo, siempre recomendamos que realices tu propia inspección mecánica antes de comprar y que verifiques los documentos del vehículo.',
      },
      {
        question: '¿Puedo financiar mi compra?',
        answer:
          'OKLA no ofrece financiamiento directo, pero trabajamos con instituciones financieras asociadas que pueden ayudarte. Busca vehículos con el badge "Financiamiento disponible" o contacta a nuestro equipo para más información.',
      },
    ],
  },
  {
    id: 'vender',
    name: 'Vender un vehículo',
    icon: Store,
    items: [
      {
        question: '¿Cómo publico mi vehículo?',
        answer:
          'Ve a "Vender" y sigue los pasos del asistente de publicación. Necesitarás fotos del vehículo, información básica (marca, modelo, año, kilometraje) y una descripción. Los anuncios básicos son gratuitos.',
      },
      {
        question: '¿Cuánto cuesta publicar un anuncio?',
        answer:
          'Los anuncios básicos son GRATIS. Ofrecemos opciones premium desde RD$499 que incluyen mayor visibilidad, destacado en búsquedas, y badge especial. Los dealers tienen planes mensuales con beneficios adicionales.',
      },
      {
        question: '¿Cuánto tiempo dura mi anuncio?',
        answer:
          'Los anuncios gratuitos duran 30 días y pueden renovarse. Los anuncios premium duran 60 días. Siempre puedes editar o eliminar tu anuncio desde tu panel de control.',
      },
      {
        question: '¿Cómo recibo el pago cuando vendo?',
        answer:
          'OKLA facilita el contacto entre compradores y vendedores, pero la transacción se realiza directamente entre ustedes. Recomendamos usar métodos de pago seguros y realizar la transferencia de propiedad en una Administración de Tránsito.',
      },
      {
        question: '¿Qué fotos debo incluir?',
        answer:
          'Recomendamos al menos 8 fotos: exterior desde todos los ángulos, interior (asientos delanteros y traseros), tablero con kilometraje, motor, y cualquier detalle relevante. Las buenas fotos aumentan significativamente las posibilidades de venta.',
      },
    ],
  },
  {
    id: 'pagos',
    name: 'Pagos y facturación',
    icon: CreditCard,
    items: [
      {
        question: '¿Qué métodos de pago aceptan?',
        answer:
          'Aceptamos tarjetas de crédito/débito (Visa, Mastercard, American Express), transferencias bancarias, y pagos a través de AZUL. Pronto añadiremos más opciones locales.',
      },
      {
        question: '¿Puedo obtener un reembolso?',
        answer:
          'Sí, ofrecemos reembolso completo dentro de las primeras 24 horas si tu anuncio no ha tenido vistas. Después de este período, evaluamos cada caso individualmente. Contacta a soporte para más información.',
      },
      {
        question: '¿Cómo cancelo mi suscripción de dealer?',
        answer:
          'Puedes cancelar tu suscripción en cualquier momento desde tu panel de dealer en "Configuración > Suscripción". Tu acceso continuará hasta el final del período pagado.',
      },
      {
        question: '¿Tienen descuentos para múltiples anuncios?',
        answer:
          'Sí, ofrecemos descuentos por volumen. Si tienes más de 5 vehículos para vender, te recomendamos nuestros planes de dealer que ofrecen mejor valor y herramientas adicionales.',
      },
    ],
  },
  {
    id: 'cuenta',
    name: 'Mi cuenta',
    icon: UserCircle,
    items: [
      {
        question: '¿Cómo creo una cuenta?',
        answer:
          'Haz clic en "Crear cuenta" en la esquina superior derecha. Puedes registrarte con tu correo electrónico o usar tu cuenta de Google o Facebook para un registro más rápido.',
      },
      {
        question: '¿Cómo recupero mi contraseña?',
        answer:
          'En la página de inicio de sesión, haz clic en "¿Olvidaste tu contraseña?". Ingresa tu correo electrónico y te enviaremos un enlace para restablecerla.',
      },
      {
        question: '¿Cómo elimino mi cuenta?',
        answer:
          'Ve a "Configuración > Cuenta > Eliminar cuenta". Ten en cuenta que esta acción es irreversible y se eliminarán todos tus anuncios, favoritos y datos asociados.',
      },
      {
        question: '¿Cómo verifico mi cuenta?',
        answer:
          'Para verificar tu cuenta, ve a "Configuración > Verificación" y sube una foto de tu cédula o pasaporte. El proceso toma entre 24-48 horas. Las cuentas verificadas generan más confianza.',
      },
    ],
  },
  {
    id: 'seguridad',
    name: 'Seguridad y fraude',
    icon: Shield,
    items: [
      {
        question: '¿Cómo identifico un anuncio fraudulento?',
        answer:
          'Desconfía de precios demasiado bajos, vendedores que evitan llamadas telefónicas, solicitudes de pago adelantado, o urgencia injustificada. Los anuncios verificados por OKLA tienen menor riesgo.',
      },
      {
        question: '¿Qué hago si encuentro un anuncio sospechoso?',
        answer:
          'Usa el botón "Reportar" en el anuncio o contáctanos directamente. Nuestro equipo revisa todos los reportes en menos de 24 horas y toma las acciones necesarias.',
      },
      {
        question: '¿Cómo protege OKLA mis datos?',
        answer:
          'Usamos encriptación de grado bancario para proteger tus datos. Nunca compartimos tu información personal con terceros sin tu consentimiento. Lee nuestra Política de Privacidad para más detalles.',
      },
      {
        question: '¿Es seguro reunirme con vendedores/compradores?',
        answer:
          'Siempre reúnete en lugares públicos durante el día, lleva a alguien de confianza, y no compartas información personal sensible. Si algo te parece sospechoso, confía en tu instinto.',
      },
    ],
  },
];

// =============================================================================
// COMPONENTS
// =============================================================================

function FAQAccordion({ item }: { item: FAQItem }) {
  const [isOpen, setIsOpen] = React.useState(false);

  return (
    <div className="border-border border-b">
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="flex w-full items-center justify-between py-4 text-left"
      >
        <span className="text-foreground font-medium">{item.question}</span>
        <ChevronDown
          className={cn(
            'text-muted-foreground h-5 w-5 shrink-0 transition-transform',
            isOpen && 'rotate-180'
          )}
        />
      </button>
      {isOpen && <div className="text-muted-foreground pb-4">{item.answer}</div>}
    </div>
  );
}

// =============================================================================
// MAIN COMPONENT
// =============================================================================

export default function AyudaPage() {
  const config = useSiteConfig();
  const [searchQuery, setSearchQuery] = React.useState('');
  const [activeCategory, setActiveCategory] = React.useState<string | null>(null);

  // Filter FAQs based on search
  const filteredCategories = React.useMemo(() => {
    if (!searchQuery.trim()) return faqCategories;

    const query = searchQuery.toLowerCase();
    return faqCategories
      .map(category => ({
        ...category,
        items: category.items.filter(
          item =>
            item.question.toLowerCase().includes(query) || item.answer.toLowerCase().includes(query)
        ),
      }))
      .filter(category => category.items.length > 0);
  }, [searchQuery]);

  return (
    <div className="min-h-screen">
      {/* Hero */}
      <section className="bg-gradient-to-br from-[#00A870] to-[#007850] py-16 text-white">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-2xl text-center">
            <h1 className="text-3xl font-bold md:text-4xl">¿En qué podemos ayudarte?</h1>
            <p className="mt-4 text-white/90">
              Encuentra respuestas a las preguntas más frecuentes o contáctanos directamente.
            </p>

            {/* Search */}
            <div className="relative mt-8">
              <Search className="text-muted-foreground absolute top-1/2 left-4 h-5 w-5 -translate-y-1/2" />
              <Input
                type="search"
                placeholder="Buscar en ayuda..."
                value={searchQuery}
                onChange={e => setSearchQuery(e.target.value)}
                className="text-foreground h-12 bg-white pl-12"
              />
            </div>
          </div>
        </div>
      </section>

      {/* Quick Links */}
      <section className="border-border border-b py-8">
        <div className="container mx-auto px-4">
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
            {faqCategories.map(category => (
              <button
                key={category.id}
                onClick={() => setActiveCategory(category.id)}
                className={cn(
                  'flex items-center gap-3 rounded-lg border p-4 transition-colors hover:border-[#00A870] hover:bg-[#00A870]/5',
                  activeCategory === category.id && 'border-[#00A870] bg-[#00A870]/5'
                )}
              >
                <category.icon className="h-5 w-5 text-[#00A870]" />
                <span className="text-foreground font-medium">{category.name}</span>
              </button>
            ))}
          </div>
        </div>
      </section>

      {/* FAQ Content */}
      <section className="py-12">
        <div className="container mx-auto px-4">
          <div className="grid gap-8 lg:grid-cols-3">
            {/* Categories */}
            <div className="lg:col-span-2">
              {filteredCategories.length === 0 ? (
                <div className="py-12 text-center">
                  <p className="text-muted-foreground">
                    No encontramos resultados para &ldquo;{searchQuery}&rdquo;.
                  </p>
                  <Button
                    variant="link"
                    onClick={() => setSearchQuery('')}
                    className="mt-2 text-[#00A870]"
                  >
                    Limpiar búsqueda
                  </Button>
                </div>
              ) : (
                <div className="space-y-8">
                  {filteredCategories.map(category => (
                    <Card
                      key={category.id}
                      id={category.id}
                      className={cn(
                        'overflow-hidden transition-shadow',
                        activeCategory === category.id && 'ring-2 ring-[#00A870]'
                      )}
                    >
                      <div className="border-border bg-muted/50 flex items-center gap-3 border-b px-6 py-4">
                        <category.icon className="h-5 w-5 text-[#00A870]" />
                        <h2 className="text-foreground font-semibold">{category.name}</h2>
                      </div>
                      <CardContent className="p-6 pt-0">
                        {category.items.map((item, index) => (
                          <FAQAccordion key={index} item={item} />
                        ))}
                      </CardContent>
                    </Card>
                  ))}
                </div>
              )}
            </div>

            {/* Contact Sidebar */}
            <div className="lg:col-span-1">
              <div className="sticky top-24 space-y-6">
                <Card>
                  <CardContent className="p-6">
                    <h3 className="text-foreground font-semibold">¿No encontraste tu respuesta?</h3>
                    <p className="text-muted-foreground mt-2 text-sm">
                      Nuestro equipo de soporte está listo para ayudarte.
                    </p>

                    <div className="mt-6 space-y-4">
                      <a
                        href={`mailto:${config.supportEmail}`}
                        className="flex items-center gap-3 rounded-lg border p-3 transition-colors hover:border-[#00A870] hover:bg-[#00A870]/5"
                      >
                        <Mail className="h-5 w-5 text-[#00A870]" />
                        <div>
                          <div className="text-foreground font-medium">Email</div>
                          <div className="text-muted-foreground text-sm">{config.supportEmail}</div>
                        </div>
                      </a>

                      <a
                        href={config.phoneHref}
                        className="flex items-center gap-3 rounded-lg border p-3 transition-colors hover:border-[#00A870] hover:bg-[#00A870]/5"
                      >
                        <Phone className="h-5 w-5 text-[#00A870]" />
                        <div>
                          <div className="text-foreground font-medium">Teléfono</div>
                          <div className="text-muted-foreground text-sm">{config.supportPhone}</div>
                        </div>
                      </a>

                      <a
                        href={config.whatsappUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="flex items-center gap-3 rounded-lg border p-3 transition-colors hover:border-[#00A870] hover:bg-[#00A870]/5"
                      >
                        <MessageCircle className="h-5 w-5 text-[#00A870]" />
                        <div>
                          <div className="text-foreground font-medium">WhatsApp</div>
                          <div className="text-muted-foreground text-sm">Chat en vivo</div>
                        </div>
                      </a>
                    </div>
                  </CardContent>
                </Card>

                <Card className="bg-[#00A870] text-white">
                  <CardContent className="p-6">
                    <h3 className="font-semibold">Horario de atención</h3>
                    <div className="mt-4 space-y-2 text-sm text-white/90">
                      {config.businessHours.split('|').map((line, i) => {
                        const parts = line.trim().split(':');
                        const label = parts[0]?.trim() || '';
                        const time = parts.slice(1).join(':').trim() || '';
                        return (
                          <div key={i} className="flex justify-between">
                            <span>{label}</span>
                            <span>{time}</span>
                          </div>
                        );
                      })}
                    </div>
                  </CardContent>
                </Card>
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
