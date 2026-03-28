import type { Metadata } from 'next';
import { ImportCalculator } from './import-calculator';

export const metadata: Metadata = {
  title: 'Calculadora de Importación de Vehículos a RD',
  description:
    'Calcula los impuestos y costos de importar un vehículo a República Dominicana. Incluye arancel, ITBIS, selectivo, flete marítimo y más.',
  keywords: [
    'importar vehículo RD',
    'impuestos importación carro',
    'DGA República Dominicana',
    'costo importar auto',
    'arancel vehículo dominicana',
    'ITBIS importación',
    'selectivo al consumo vehículos',
  ],
  openGraph: {
    title: 'Calculadora de Importación de Vehículos a RD | OKLA',
    description:
      'Estima los impuestos y costos totales de importar un vehículo a República Dominicana.',
    type: 'website',
    locale: 'es_DO',
  },
};

export default function CalculadoraImportacionPage() {
  return (
    <div className="bg-background min-h-screen">
      {/* Hero */}
      <section className="bg-gradient-to-br from-[#1e40af] to-[#1e3a5f] py-14 text-white">
        <div className="container mx-auto px-4 text-center">
          <h1 className="mb-3 text-3xl font-bold md:text-4xl">
            Calculadora de Importación de Vehículos
          </h1>
          <p className="mx-auto max-w-2xl text-white/90">
            Estima los impuestos, aranceles y costos totales de traer un vehículo desde Estados
            Unidos a República Dominicana.
          </p>
        </div>
      </section>

      {/* Calculator */}
      <section className="container mx-auto px-4 py-10">
        <ImportCalculator />
      </section>

      {/* Info section */}
      <section className="bg-muted/50 py-14">
        <div className="container mx-auto max-w-3xl px-4">
          <h2 className="mb-6 text-2xl font-bold">Sobre la importación de vehículos a RD</h2>
          <div className="text-muted-foreground space-y-4">
            <p>
              La importación de vehículos a República Dominicana está regulada por la{' '}
              <strong>Dirección General de Aduanas (DGA)</strong> y sujeta a varios impuestos:
              arancel, Impuesto Selectivo al Consumo (ISC) e ITBIS (18%).
            </p>
            <p>
              Los <strong>vehículos eléctricos</strong> gozan de exención total de aranceles e ISC
              según la{' '}
              <strong>
                Ley 103-13 de Incentivo a la Importación de Vehículos de Energía No Convencional
              </strong>
              , lo que los hace significativamente más económicos de importar.
            </p>
            <p>
              Los <strong>vehículos híbridos</strong> reciben un descuento del 50% en el Impuesto
              Selectivo al Consumo, haciéndolos una opción intermedia atractiva.
            </p>
            <p>
              Para vehículos usados, existe un recargo adicional del{' '}
              <strong>20% sobre el arancel</strong> si el vehículo tiene más de 5 años de
              fabricación. Vehículos de más de 5 años pueden tener restricciones adicionales.
            </p>
            <p className="rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
              <strong>⚠️ Disclaimer:</strong> Los cálculos presentados son estimados y de carácter
              referencial. Las tasas, impuestos y costos pueden variar. Para cifras exactas,
              consulte con un agente aduanal certificado o visite{' '}
              <a
                href="https://www.aduanas.gob.do"
                target="_blank"
                rel="noopener noreferrer"
                className="underline"
              >
                aduanas.gob.do
              </a>
              .
            </p>
          </div>
        </div>
      </section>

      {/* JSON-LD structured data */}
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{
          __html: JSON.stringify({
            '@context': 'https://schema.org',
            '@type': 'WebApplication',
            name: 'Calculadora de Importación de Vehículos OKLA',
            description:
              'Calcula los costos e impuestos de importar un vehículo a República Dominicana',
            url: 'https://okla.com.do/herramientas/calculadora-importacion',
            applicationCategory: 'FinanceApplication',
            operatingSystem: 'Web',
            offers: {
              '@type': 'Offer',
              price: '0',
              priceCurrency: 'DOP',
            },
          }),
        }}
      />
    </div>
  );
}
