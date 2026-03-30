import type { Metadata } from 'next';
import { ShieldCheck } from 'lucide-react';
import { JsonLd } from '@/lib/seo';
import OklaScoreClient from './okla-score-client';

// =============================================================================
// OKLA Score™ — VIN Lookup Page (Server Component for SSR SEO)
// =============================================================================
// Public page where buyers enter a VIN + optional price to get a full score.
// Server Component exports metadata for Google indexing + JSON-LD structured data.
// =============================================================================

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || 'https://okla.com.do';

export const metadata: Metadata = {
  title: 'OKLA Score™ — Cómo Saber si un Carro Usado es Bueno | República Dominicana',
  description:
    'Evalúa cualquier vehículo usado en República Dominicana con el OKLA Score™. ' +
    'Analiza historial VIN, recalls, precio justo, kilometraje y más. ' +
    'La primera herramienta científica para saber si un carro usado es bueno en RD. 100% gratis.',
  keywords: [
    'cómo saber si un carro usado es bueno',
    'República Dominicana',
    'verificar vehículo usado RD',
    'OKLA Score',
    'evaluación vehículo VIN',
    'revisar carro antes de comprar',
    'historial vehicular RD',
    'recalls NHTSA',
    'precio justo carro usado',
    'detector fraude vehicular',
    'comprar carro usado seguro',
  ],
  alternates: {
    canonical: `${SITE_URL}/okla-score`,
  },
  openGraph: {
    type: 'website',
    url: `${SITE_URL}/okla-score`,
    title: 'OKLA Score™ — Evalúa Cualquier Vehículo Usado en RD',
    description:
      'La primera evaluación científica de vehículos usados en República Dominicana. ' +
      'Analiza historial VIN, recalls, kilometraje y precio justo. 100% gratis.',
    siteName: 'OKLA',
    locale: 'es_DO',
    images: [
      {
        url: `${SITE_URL}/og-okla-score.jpg`,
        width: 1200,
        height: 630,
        alt: 'OKLA Score — Evaluación de Vehículos Usados en República Dominicana',
      },
    ],
  },
  twitter: {
    card: 'summary_large_image',
    title: 'OKLA Score™ — Evalúa Cualquier Vehículo Usado en RD',
    description:
      'Analiza historial VIN, recalls, precio justo y más. La herramienta #1 para comprar carros usados en RD.',
    images: `${SITE_URL}/og-okla-score.jpg`,
  },
};

/** Structured data: SoftwareApplication + FAQPage for rich snippets */
function getOklaScoreJsonLd() {
  return {
    '@context': 'https://schema.org',
    '@type': 'SoftwareApplication',
    name: 'OKLA Score™',
    applicationCategory: 'UtilitiesApplication',
    operatingSystem: 'Web',
    url: `${SITE_URL}/okla-score`,
    description:
      'Herramienta gratuita para evaluar vehículos usados en República Dominicana. ' +
      'Analiza historial VIN, recalls NHTSA, precio de mercado, kilometraje y más con un score de 0 a 1,000.',
    offers: {
      '@type': 'Offer',
      price: '0',
      priceCurrency: 'DOP',
    },
    author: {
      '@type': 'Organization',
      name: 'OKLA',
      url: SITE_URL,
    },
  };
}

function getOklaScoreFaqJsonLd() {
  return {
    '@context': 'https://schema.org',
    '@type': 'FAQPage',
    mainEntity: [
      {
        '@type': 'Question',
        name: '¿Cómo saber si un carro usado es bueno en República Dominicana?',
        acceptedAnswer: {
          '@type': 'Answer',
          text: 'Usa el OKLA Score™ para evaluar cualquier vehículo usado con su número VIN. El sistema analiza 7 dimensiones: historial del título, mecánica, kilometraje, precio justo, seguridad NHTSA, depreciación y reputación del vendedor. Un score de 700+ indica un vehículo en buenas condiciones.',
        },
      },
      {
        '@type': 'Question',
        name: '¿Qué es el OKLA Score™?',
        acceptedAnswer: {
          '@type': 'Answer',
          text: 'El OKLA Score™ es la primera evaluación científica de vehículos usados en República Dominicana. Genera un puntaje de 0 a 1,000 basado en datos verificables: historial VIN, recalls activos (NHTSA), comparación de precios con el mercado de EE.UU. y RD, y análisis de kilometraje.',
        },
      },
      {
        '@type': 'Question',
        name: '¿Cuánto cuesta usar el OKLA Score™?',
        acceptedAnswer: {
          '@type': 'Answer',
          text: 'El OKLA Score™ es 100% gratuito. Los datos provienen de fuentes públicas como NHTSA (National Highway Traffic Safety Administration) y el análisis de mercado de OKLA.',
        },
      },
      {
        '@type': 'Question',
        name: '¿Qué necesito para evaluar un vehículo?',
        acceptedAnswer: {
          '@type': 'Answer',
          text: 'Solo necesitas el número VIN (Vehicle Identification Number) de 17 caracteres. Lo encuentras en la esquina inferior izquierda del parabrisas, en la puerta del conductor, o en los documentos del vehículo. Opcionalmente puedes agregar el precio listado y kilometraje para un análisis más completo.',
        },
      },
      {
        '@type': 'Question',
        name: '¿Qué significan los niveles del OKLA Score™?',
        acceptedAnswer: {
          '@type': 'Answer',
          text: 'Excelente (850-1,000): Vehículo en condiciones óptimas. Bueno (700-849): Buen estado general. Regular (550-699): Algunas preocupaciones menores. Deficiente (400-549): Problemas significativos. Crítico (0-399): Riesgos graves detectados.',
        },
      },
    ],
  };
}

function getOklaScoreBreadcrumbJsonLd() {
  return {
    '@context': 'https://schema.org',
    '@type': 'BreadcrumbList',
    itemListElement: [
      {
        '@type': 'ListItem',
        position: 1,
        name: 'Inicio',
        item: SITE_URL,
      },
      {
        '@type': 'ListItem',
        position: 2,
        name: 'OKLA Score™',
        item: `${SITE_URL}/okla-score`,
      },
    ],
  };
}

export default function OklaScorePage() {
  return (
    <div className="min-h-screen bg-gradient-to-b from-emerald-50/50 to-white dark:from-gray-950 dark:to-gray-900">
      {/* JSON-LD Structured Data for Google Rich Results */}
      <JsonLd data={getOklaScoreJsonLd()} />
      <JsonLd data={getOklaScoreFaqJsonLd()} />
      <JsonLd data={getOklaScoreBreadcrumbJsonLd()} />

      {/* Hero Section (SSR for SEO) */}
      <section className="relative overflow-hidden border-b bg-gradient-to-br from-emerald-600 via-emerald-700 to-teal-800 text-white">
        <div className="absolute inset-0 bg-[url('/grid.svg')] opacity-10" />
        <div className="relative container mx-auto px-4 py-16 md:py-24">
          <div className="mx-auto max-w-3xl text-center">
            <div className="mb-4 inline-flex items-center gap-2 rounded-full bg-white/10 px-4 py-2 text-sm font-medium backdrop-blur-sm">
              <ShieldCheck className="h-4 w-4" />
              Powered by NHTSA · 100% Gratis
            </div>
            <h1 className="text-4xl font-extrabold tracking-tight md:text-5xl lg:text-6xl">
              OKLA Score™
            </h1>
            <p className="mt-4 text-xl text-emerald-100 md:text-2xl">
              Cómo saber si un carro usado es bueno en República Dominicana
            </p>
            <p className="mx-auto mt-2 max-w-xl text-emerald-200">
              Analiza cualquier vehículo con su número VIN. Detecta fraudes, recalls, daños ocultos
              y compara precios con el mercado de EE.UU. y RD.
            </p>
          </div>
        </div>
      </section>

      {/* Client-side interactive form + results */}
      <OklaScoreClient />
    </div>
  );
}
