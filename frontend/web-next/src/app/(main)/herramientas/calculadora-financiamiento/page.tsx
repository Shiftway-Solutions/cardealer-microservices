import type { Metadata } from 'next';
import { FinancingCalculator } from './financing-calculator';

export const metadata: Metadata = {
  title: 'Calculadora de Financiamiento Vehicular',
  description:
    'Calcula tu cuota mensual para financiar tu vehículo en República Dominicana. Tasas actualizadas del mercado, seguros y tabla de amortización completa.',
  keywords: [
    'financiamiento vehicular',
    'calculadora cuotas',
    'préstamo vehículo RD',
    'financiar carro dominicana',
    'cuota mensual auto',
    'tabla amortización',
  ],
  openGraph: {
    title: 'Calculadora de Financiamiento Vehicular | OKLA',
    description: 'Calcula tu cuota mensual para financiar tu vehículo en República Dominicana.',
    type: 'website',
    locale: 'es_DO',
  },
};

export default function CalculadoraFinanciamientoPage() {
  return (
    <div className="bg-background min-h-screen">
      {/* Hero */}
      <section className="from-primary to-primary/80 bg-gradient-to-br py-14 text-white">
        <div className="container mx-auto px-4 text-center">
          <h1 className="mb-3 text-3xl font-bold md:text-4xl">
            Calculadora de Financiamiento Vehicular
          </h1>
          <p className="mx-auto max-w-2xl text-white/90">
            Calcula tu cuota mensual, total de intereses y genera una tabla de amortización
            completa. Tasas basadas en el mercado financiero dominicano.
          </p>
        </div>
      </section>

      {/* Calculator */}
      <section className="container mx-auto px-4 py-10">
        <FinancingCalculator />
      </section>

      {/* Info section */}
      <section className="bg-muted/50 py-14">
        <div className="container mx-auto max-w-3xl px-4">
          <h2 className="mb-6 text-2xl font-bold">Sobre el financiamiento vehicular en RD</h2>
          <div className="text-muted-foreground space-y-4">
            <p>
              En República Dominicana, las tasas de interés para préstamos vehiculares oscilan entre
              el <strong>8% y el 24% anual</strong>, dependiendo de la entidad financiera, el plazo
              y el perfil del solicitante. La Superintendencia de Bancos (SIB) publica mensualmente
              las tasas de referencia.
            </p>
            <p>
              La mayoría de los bancos y financieras requieren un{' '}
              <strong>inicial mínimo del 20%</strong> del valor del vehículo. Algunos ofrecen
              financiamiento hasta el 90% para vehículos nuevos con condiciones especiales.
            </p>
            <p>
              El seguro vehicular es obligatorio según la <strong>Ley 146-02</strong> y cubre
              responsabilidad civil. Un seguro todo riesgo, generalmente requerido por las
              financieras, representa aproximadamente un 3-5% del valor del vehículo anualmente.
            </p>
            <p className="text-sm italic">
              Nota: Esta calculadora es de carácter informativo. Las cuotas reales pueden variar
              según la entidad financiera, su perfil crediticio y las condiciones vigentes. Consulte
              con su banco o financiera para una cotización precisa.
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
            name: 'Calculadora de Financiamiento Vehicular OKLA',
            description:
              'Calcula tu cuota mensual para financiar un vehículo en República Dominicana',
            url: 'https://okla.com.do/herramientas/calculadora-financiamiento',
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
