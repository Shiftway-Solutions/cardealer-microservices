import type { Metadata } from 'next';
import { Card, CardContent } from '@/components/ui/card';
import {
  AlertTriangle,
  ArrowRight,
  CheckCircle2,
  Clock,
  DollarSign,
  Mail,
  Shield,
} from 'lucide-react';

export const metadata: Metadata = {
  title: 'Política de Reembolso',
  description:
    'Política de reembolso de OKLA conforme a la Ley 126-02. Información sobre el derecho de retracto, condiciones y proceso de solicitud.',
};

export default function PoliticaReembolsoPage() {
  return (
    <div className="mx-auto max-w-4xl px-4 py-12">
      {/* Header */}
      <div className="mb-10 space-y-3">
        <h1 className="text-foreground text-3xl font-bold">Política de Reembolso</h1>
        <p className="text-muted-foreground text-lg">
          En cumplimiento de la <strong>Ley 126-02</strong> de Comercio Electrónico y la{' '}
          <strong>Ley 358-05</strong> de Protección al Consumidor de República Dominicana.
        </p>
        <p className="text-muted-foreground text-sm">Última actualización: 1 de marzo de 2026</p>
      </div>

      {/* Derecho de retracto */}
      <section className="mb-10">
        <div className="mb-4 flex items-center gap-2">
          <Shield className="text-primary h-6 w-6" />
          <h2 className="text-foreground text-2xl font-bold">Derecho de Retracto</h2>
        </div>
        <Card>
          <CardContent className="space-y-4 p-5">
            <p className="text-muted-foreground text-sm">
              De conformidad con la <strong>Ley 126-02</strong> de Comercio Electrónico, Documentos
              y Firmas Digitales, los usuarios de OKLA tienen el derecho de retracto dentro de los{' '}
              <strong>siete (7) días calendario</strong> siguientes a la adquisición de cualquier
              servicio pagado en la plataforma.
            </p>
            <Card className="border-primary/20 bg-primary/5">
              <CardContent className="flex items-start gap-3 p-4">
                <Clock className="text-primary mt-0.5 h-5 w-5 shrink-0" />
                <div>
                  <p className="text-foreground text-sm font-semibold">
                    7 días calendario para solicitar reembolso
                  </p>
                  <p className="text-muted-foreground mt-1 text-xs">
                    A partir de la fecha de compra o contratación del servicio.
                  </p>
                </div>
              </CardContent>
            </Card>
          </CardContent>
        </Card>
      </section>

      {/* Servicios aplicables */}
      <section className="mb-10">
        <div className="mb-4 flex items-center gap-2">
          <DollarSign className="text-primary h-6 w-6" />
          <h2 className="text-foreground text-2xl font-bold">Servicios Aplicables</h2>
        </div>
        <Card>
          <CardContent className="space-y-4 p-5">
            <div>
              <h3 className="text-foreground font-semibold">
                Publicación de vehículos ($29/listado)
              </h3>
              <ul className="text-muted-foreground mt-2 space-y-2 text-sm">
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0 text-green-500" />
                  Reembolso completo si la publicación no ha sido activada o no ha recibido
                  visualizaciones.
                </li>
                <li className="flex items-start gap-2">
                  <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0 text-amber-500" />
                  No aplica reembolso si el listado ya fue publicado y recibió interacciones
                  (visualizaciones, contactos o favoritos).
                </li>
              </ul>
            </div>

            <div className="border-border border-t pt-4">
              <h3 className="text-foreground font-semibold">
                Suscripciones de dealer ($49–$299/mes)
              </h3>
              <ul className="text-muted-foreground mt-2 space-y-2 text-sm">
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0 text-green-500" />
                  Reembolso prorrateado por los días no utilizados si se solicita dentro del período
                  de retracto de 7 días.
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0 text-green-500" />
                  La suscripción se cancelará inmediatamente al procesar el reembolso.
                </li>
                <li className="flex items-start gap-2">
                  <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0 text-amber-500" />
                  Después del período de 7 días, la cancelación surtirá efecto al final del ciclo de
                  facturación actual.
                </li>
              </ul>
            </div>

            <div className="border-border border-t pt-4">
              <h3 className="text-foreground font-semibold">Servicios de impulso/promoción</h3>
              <ul className="text-muted-foreground mt-2 space-y-2 text-sm">
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0 text-green-500" />
                  Reembolso completo si el servicio de impulso no ha sido ejecutado.
                </li>
                <li className="flex items-start gap-2">
                  <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0 text-amber-500" />
                  Reembolso parcial prorrateado si el período de impulso está en curso.
                </li>
              </ul>
            </div>
          </CardContent>
        </Card>
      </section>

      {/* Cómo solicitar */}
      <section className="mb-10">
        <div className="mb-4 flex items-center gap-2">
          <ArrowRight className="text-primary h-6 w-6" />
          <h2 className="text-foreground text-2xl font-bold">Cómo Solicitar un Reembolso</h2>
        </div>
        <Card>
          <CardContent className="p-5">
            <div className="space-y-4">
              <div className="flex gap-4">
                <div className="bg-primary text-primary-foreground flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-sm font-bold">
                  1
                </div>
                <div>
                  <h3 className="text-foreground font-semibold">Envía tu solicitud</h3>
                  <p className="text-muted-foreground mt-1 text-sm">
                    Escribe a{' '}
                    <a
                      href="mailto:reembolsos@okla.com.do"
                      className="text-primary hover:underline"
                    >
                      reembolsos@okla.com.do
                    </a>{' '}
                    con el asunto &ldquo;Solicitud de Reembolso&rdquo; e incluye: tu nombre
                    completo, email de la cuenta, número de transacción/recibo y motivo de la
                    solicitud.
                  </p>
                </div>
              </div>

              <div className="flex gap-4">
                <div className="bg-primary text-primary-foreground flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-sm font-bold">
                  2
                </div>
                <div>
                  <h3 className="text-foreground font-semibold">Confirmación</h3>
                  <p className="text-muted-foreground mt-1 text-sm">
                    Recibirás una confirmación de recepción dentro de 2 días hábiles con un número
                    de caso para seguimiento.
                  </p>
                </div>
              </div>

              <div className="flex gap-4">
                <div className="bg-primary text-primary-foreground flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-sm font-bold">
                  3
                </div>
                <div>
                  <h3 className="text-foreground font-semibold">Evaluación</h3>
                  <p className="text-muted-foreground mt-1 text-sm">
                    Nuestro equipo evaluará tu solicitud y verificará que cumple con las condiciones
                    aplicables.
                  </p>
                </div>
              </div>

              <div className="flex gap-4">
                <div className="bg-primary text-primary-foreground flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-sm font-bold">
                  4
                </div>
                <div>
                  <h3 className="text-foreground font-semibold">Procesamiento</h3>
                  <p className="text-muted-foreground mt-1 text-sm">
                    Si procede, el reembolso se realizará al mismo método de pago original dentro de{' '}
                    <strong>15 días hábiles</strong>. Recibirás una notificación cuando se procese.
                  </p>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      </section>

      {/* Timeline */}
      <section className="mb-10">
        <Card className="border-primary/20 bg-primary/5">
          <CardContent className="flex items-start gap-3 p-5">
            <Clock className="text-primary mt-0.5 h-5 w-5 shrink-0" />
            <div>
              <h3 className="text-foreground font-semibold">Plazo de procesamiento</h3>
              <p className="text-muted-foreground mt-1 text-sm">
                Los reembolsos aprobados serán procesados dentro de un plazo máximo de{' '}
                <strong>15 días hábiles</strong>. El tiempo de reflejo en tu cuenta puede variar
                según tu banco o proveedor de pago (generalmente 3-5 días hábiles adicionales).
              </p>
            </div>
          </CardContent>
        </Card>
      </section>

      {/* Contact */}
      <section>
        <Card>
          <CardContent className="p-5">
            <h3 className="text-foreground mb-3 font-semibold">Contacto para Reembolsos</h3>
            <div className="text-muted-foreground space-y-2 text-sm">
              <p className="flex items-center gap-2">
                <Mail className="text-primary h-4 w-4" />
                Email:{' '}
                <a href="mailto:reembolsos@okla.com.do" className="text-primary hover:underline">
                  reembolsos@okla.com.do
                </a>
              </p>
              <p className="mt-3 text-xs">
                Si no estás satisfecho con la resolución, puedes presentar una reclamación a través
                de nuestro{' '}
                <a href="/reclamaciones" className="text-primary hover:underline">
                  Sistema de Reclamaciones
                </a>{' '}
                o contactar a ProConsumidor (Tel. 809-567-7755).
              </p>
            </div>
          </CardContent>
        </Card>
      </section>
    </div>
  );
}
