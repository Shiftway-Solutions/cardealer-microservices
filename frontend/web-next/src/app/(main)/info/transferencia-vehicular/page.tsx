import type { Metadata } from 'next';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  AlertTriangle,
  CheckCircle2,
  ClipboardList,
  DollarSign,
  ExternalLink,
  FileCheck,
  Phone,
  Shield,
} from 'lucide-react';

export const metadata: Metadata = {
  title: 'Guía de Transferencia Vehicular en RD',
  description:
    'Guía completa para realizar la transferencia legal de vehículos en República Dominicana. Requisitos, documentos, impuestos y pasos ante la DGTT/INTRANT.',
};

const steps = [
  {
    number: 1,
    title: 'Verificación del vehículo',
    description:
      'Antes de concretar la compra, verifica el historial del vehículo en la DGII y la Policía Nacional para asegurar que no tiene deudas, gravámenes o reportes de robo.',
  },
  {
    number: 2,
    title: 'Acuerdo de compra-venta',
    description:
      'Formaliza un contrato de compra-venta entre ambas partes. Es recomendable legalizarlo ante un notario público para mayor seguridad jurídica.',
  },
  {
    number: 3,
    title: 'Pago del impuesto de transferencia',
    description:
      'Paga el 2% del valor del vehículo como impuesto de transferencia ante la Dirección General de Impuestos Internos (DGII). Este pago es obligatorio.',
  },
  {
    number: 4,
    title: 'Inspección técnica vehicular',
    description:
      'Realiza la inspección técnica del vehículo en un centro autorizado por INTRANT. El vehículo debe estar en condiciones mecánicas y de seguridad adecuadas.',
  },
  {
    number: 5,
    title: 'Seguro vigente',
    description:
      'Asegúrate de que el vehículo cuente con un seguro de responsabilidad civil vigente emitido por una aseguradora autorizada por la Superintendencia de Seguros.',
  },
  {
    number: 6,
    title: 'Traspaso ante la DGTT',
    description:
      'Acude a la Dirección General de Tránsito Terrestre (DGTT) o una oficina de INTRANT con todos los documentos para realizar el traspaso de la matrícula a nombre del nuevo propietario.',
  },
  {
    number: 7,
    title: 'Obtención de nueva matrícula',
    description:
      'Una vez completado el traspaso, recibirás la nueva matrícula del vehículo a nombre del comprador. Verifica que todos los datos estén correctos.',
  },
];

const requiredDocuments = [
  'Matrícula original del vehículo (certificado de registro)',
  'Cédula de identidad del vendedor y del comprador',
  'Contrato de compra-venta (preferiblemente notarizado)',
  'Recibo de pago del impuesto de transferencia (2% - DGII)',
  'Certificado de inspección técnica vigente',
  'Seguro de responsabilidad civil vigente',
  'Recibo de pago de marchamo/placa al día',
  'Poder notarial (si actúa un representante)',
];

export default function TransferenciaVehicularPage() {
  return (
    <div className="mx-auto max-w-4xl px-4 py-12">
      {/* Header */}
      <div className="mb-10 space-y-3">
        <h1 className="text-foreground text-3xl font-bold">
          Guía de Transferencia Vehicular en República Dominicana
        </h1>
        <p className="text-muted-foreground text-lg">
          Todo lo que necesitas saber para realizar la transferencia legal de un vehículo conforme a
          las regulaciones de la DGTT/INTRANT.
        </p>
      </div>

      {/* Disclaimer */}
      <Card className="mb-8 border-amber-200 bg-amber-50/50 dark:border-amber-900/50 dark:bg-amber-950/20">
        <CardContent className="flex items-start gap-3 p-4">
          <AlertTriangle className="mt-0.5 h-5 w-5 shrink-0 text-amber-600 dark:text-amber-400" />
          <div>
            <h3 className="text-foreground text-sm font-semibold">Aviso Importante</h3>
            <p className="text-muted-foreground mt-1 text-sm">
              OKLA es una plataforma de marketplace que conecta compradores y vendedores.{' '}
              <strong>
                OKLA no verifica la propiedad de los vehículos publicados ni participa en el proceso
                de transferencia legal.
              </strong>{' '}
              Es responsabilidad del comprador y vendedor completar todos los trámites legales
              correspondientes.
            </p>
          </div>
        </CardContent>
      </Card>

      {/* Steps */}
      <section className="mb-10">
        <h2 className="text-foreground mb-6 flex items-center gap-2 text-2xl font-bold">
          <ClipboardList className="text-primary h-6 w-6" />
          Pasos para la Transferencia
        </h2>
        <div className="space-y-4">
          {steps.map(step => (
            <div key={step.number} className="flex gap-4">
              <div className="bg-primary text-primary-foreground flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-sm font-bold">
                {step.number}
              </div>
              <div className="border-border flex-1 border-b pb-4 last:border-b-0">
                <h3 className="text-foreground font-semibold">{step.title}</h3>
                <p className="text-muted-foreground mt-1 text-sm">{step.description}</p>
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* Required Documents */}
      <section className="mb-10">
        <h2 className="text-foreground mb-6 flex items-center gap-2 text-2xl font-bold">
          <FileCheck className="text-primary h-6 w-6" />
          Documentos Requeridos
        </h2>
        <Card>
          <CardContent className="p-5">
            <ul className="space-y-3">
              {requiredDocuments.map((doc, index) => (
                <li key={index} className="text-muted-foreground flex items-start gap-2 text-sm">
                  <CheckCircle2 className="text-primary mt-0.5 h-4 w-4 shrink-0" />
                  {doc}
                </li>
              ))}
            </ul>
          </CardContent>
        </Card>
      </section>

      {/* Tax Information */}
      <section className="mb-10">
        <h2 className="text-foreground mb-6 flex items-center gap-2 text-2xl font-bold">
          <DollarSign className="text-primary h-6 w-6" />
          Información Tributaria
        </h2>
        <Card>
          <CardContent className="space-y-4 p-5">
            <div>
              <h3 className="text-foreground font-semibold">Impuesto de Transferencia (2%)</h3>
              <p className="text-muted-foreground mt-1 text-sm">
                La DGII cobra un impuesto del <strong>2% sobre el valor del vehículo</strong> al
                momento de la transferencia. Este impuesto se calcula sobre el valor de mercado
                estimado por la DGII o el precio de venta, el que sea mayor.
              </p>
            </div>
            <div>
              <h3 className="text-foreground font-semibold">ITBIS</h3>
              <p className="text-muted-foreground mt-1 text-sm">
                Las ventas de vehículos por parte de dealers registrados están sujetas al ITBIS
                (18%). Los vehículos usados vendidos entre particulares generalmente no generan
                ITBIS adicional.
              </p>
            </div>
            <div>
              <h3 className="text-foreground font-semibold">Otros costos</h3>
              <p className="text-muted-foreground mt-1 text-sm">
                Considere también los costos de: nueva placa/marchamo, inspección técnica, seguro
                obligatorio y honorarios notariales si aplica.
              </p>
            </div>
          </CardContent>
        </Card>
      </section>

      {/* Contact info */}
      <section>
        <h2 className="text-foreground mb-6 flex items-center gap-2 text-2xl font-bold">
          <Shield className="text-primary h-6 w-6" />
          Contactos Oficiales
        </h2>
        <div className="grid gap-4 sm:grid-cols-2">
          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-base">INTRANT</CardTitle>
            </CardHeader>
            <CardContent className="text-muted-foreground space-y-2 text-sm">
              <p>Instituto Nacional de Tránsito y Transporte Terrestre</p>
              <p className="flex items-center gap-1.5">
                <Phone className="h-3.5 w-3.5" />
                Tel: 809-688-1741
              </p>
              <a
                href="https://www.intrant.gob.do"
                target="_blank"
                rel="noopener noreferrer"
                className="text-primary flex items-center gap-1.5 hover:underline"
              >
                <ExternalLink className="h-3.5 w-3.5" />
                www.intrant.gob.do
              </a>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-base">DGII</CardTitle>
            </CardHeader>
            <CardContent className="text-muted-foreground space-y-2 text-sm">
              <p>Dirección General de Impuestos Internos</p>
              <p className="flex items-center gap-1.5">
                <Phone className="h-3.5 w-3.5" />
                Tel: 809-689-2181
              </p>
              <a
                href="https://www.dgii.gov.do"
                target="_blank"
                rel="noopener noreferrer"
                className="text-primary flex items-center gap-1.5 hover:underline"
              >
                <ExternalLink className="h-3.5 w-3.5" />
                www.dgii.gov.do
              </a>
            </CardContent>
          </Card>
        </div>
      </section>
    </div>
  );
}
