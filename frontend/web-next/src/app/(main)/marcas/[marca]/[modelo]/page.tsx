/**
 * Model SEO Landing Page — /marcas/[marca]/[modelo]
 *
 * Static SEO page for popular make+model combinations.
 * Example: /marcas/toyota/corolla → "Toyota Corolla usados en República Dominicana"
 *
 * Features:
 * - SSG via generateStaticParams for top 80+ make+model combos
 * - ISR revalidation every 1 hour (inventory freshness)
 * - Unique SEO-optimized descriptions per model
 * - JSON-LD ItemList + BreadcrumbList structured data
 * - Server-renderable metadata for crawlers
 * - Client-side vehicle grid with real-time inventory
 */

import type { Metadata } from 'next';
import Link from 'next/link';
import { Suspense } from 'react';
import { generateBreadcrumbJsonLd } from '@/lib/seo';
import { MODEL_DESCRIPTIONS } from '@/data/model-seo-content';
import { ModelVehiclesClient } from './model-vehicles-client';

// ── Popular make+model combinations for static generation ─────────────────
// These are the most searched combinations in the DR market.
// The list is intentionally comprehensive to cover long-tail SEO.
const POPULAR_COMBINATIONS: Array<{ make: string; model: string }> = [
  // Toyota (14 models — #1 brand in DR)
  { make: 'toyota', model: 'corolla' },
  { make: 'toyota', model: 'camry' },
  { make: 'toyota', model: 'rav4' },
  { make: 'toyota', model: 'hilux' },
  { make: 'toyota', model: 'fortuner' },
  { make: 'toyota', model: 'land-cruiser' },
  { make: 'toyota', model: 'prado' },
  { make: 'toyota', model: 'yaris' },
  { make: 'toyota', model: 'c-hr' },
  { make: 'toyota', model: 'highlander' },
  { make: 'toyota', model: 'tacoma' },
  { make: 'toyota', model: '4runner' },
  { make: 'toyota', model: 'tundra' },
  { make: 'toyota', model: 'sequoia' },
  // Honda (8 models)
  { make: 'honda', model: 'civic' },
  { make: 'honda', model: 'accord' },
  { make: 'honda', model: 'cr-v' },
  { make: 'honda', model: 'hr-v' },
  { make: 'honda', model: 'pilot' },
  { make: 'honda', model: 'odyssey' },
  { make: 'honda', model: 'fit' },
  { make: 'honda', model: 'ridgeline' },
  // Hyundai (8 models)
  { make: 'hyundai', model: 'elantra' },
  { make: 'hyundai', model: 'sonata' },
  { make: 'hyundai', model: 'tucson' },
  { make: 'hyundai', model: 'santa-fe' },
  { make: 'hyundai', model: 'accent' },
  { make: 'hyundai', model: 'kona' },
  { make: 'hyundai', model: 'palisade' },
  { make: 'hyundai', model: 'creta' },
  // Kia (7 models)
  { make: 'kia', model: 'sportage' },
  { make: 'kia', model: 'sorento' },
  { make: 'kia', model: 'forte' },
  { make: 'kia', model: 'seltos' },
  { make: 'kia', model: 'rio' },
  { make: 'kia', model: 'soul' },
  { make: 'kia', model: 'carnival' },
  // Nissan (8 models)
  { make: 'nissan', model: 'sentra' },
  { make: 'nissan', model: 'altima' },
  { make: 'nissan', model: 'pathfinder' },
  { make: 'nissan', model: 'frontier' },
  { make: 'nissan', model: 'kicks' },
  { make: 'nissan', model: 'rogue' },
  { make: 'nissan', model: 'versa' },
  { make: 'nissan', model: 'x-trail' },
  // Ford (6 models)
  { make: 'ford', model: 'explorer' },
  { make: 'ford', model: 'escape' },
  { make: 'ford', model: 'ranger' },
  { make: 'ford', model: 'f-150' },
  { make: 'ford', model: 'bronco' },
  { make: 'ford', model: 'expedition' },
  // Chevrolet (6 models)
  { make: 'chevrolet', model: 'silverado' },
  { make: 'chevrolet', model: 'equinox' },
  { make: 'chevrolet', model: 'tahoe' },
  { make: 'chevrolet', model: 'trailblazer' },
  { make: 'chevrolet', model: 'traverse' },
  { make: 'chevrolet', model: 'colorado' },
  // Jeep (5 models)
  { make: 'jeep', model: 'wrangler' },
  { make: 'jeep', model: 'grand-cherokee' },
  { make: 'jeep', model: 'compass' },
  { make: 'jeep', model: 'renegade' },
  { make: 'jeep', model: 'gladiator' },
  // Mitsubishi (5 models)
  { make: 'mitsubishi', model: 'outlander' },
  { make: 'mitsubishi', model: 'l200' },
  { make: 'mitsubishi', model: 'asx' },
  { make: 'mitsubishi', model: 'montero' },
  { make: 'mitsubishi', model: 'eclipse-cross' },
  // Suzuki (4 models)
  { make: 'suzuki', model: 'vitara' },
  { make: 'suzuki', model: 'swift' },
  { make: 'suzuki', model: 'jimny' },
  { make: 'suzuki', model: 'grand-vitara' },
  // Mazda (4 models)
  { make: 'mazda', model: 'cx-5' },
  { make: 'mazda', model: 'cx-30' },
  { make: 'mazda', model: 'mazda3' },
  { make: 'mazda', model: 'cx-9' },
  // BMW (3 models)
  { make: 'bmw', model: 'x3' },
  { make: 'bmw', model: 'x5' },
  { make: 'bmw', model: 'serie-3' },
  // Mercedes-Benz (3 models)
  { make: 'mercedes-benz', model: 'clase-c' },
  { make: 'mercedes-benz', model: 'gle' },
  { make: 'mercedes-benz', model: 'glc' },
];

// ── Model display name mapping ───────────────────────────────────────────────
// Maps slug → human-readable model name
const MODEL_DISPLAY_NAMES: Record<string, string> = {
  corolla: 'Corolla',
  camry: 'Camry',
  rav4: 'RAV4',
  hilux: 'Hilux',
  fortuner: 'Fortuner',
  'land-cruiser': 'Land Cruiser',
  prado: 'Prado',
  yaris: 'Yaris',
  'c-hr': 'C-HR',
  highlander: 'Highlander',
  tacoma: 'Tacoma',
  '4runner': '4Runner',
  tundra: 'Tundra',
  sequoia: 'Sequoia',
  civic: 'Civic',
  accord: 'Accord',
  'cr-v': 'CR-V',
  'hr-v': 'HR-V',
  pilot: 'Pilot',
  odyssey: 'Odyssey',
  fit: 'Fit',
  ridgeline: 'Ridgeline',
  elantra: 'Elantra',
  sonata: 'Sonata',
  tucson: 'Tucson',
  'santa-fe': 'Santa Fe',
  accent: 'Accent',
  kona: 'Kona',
  palisade: 'Palisade',
  creta: 'Creta',
  sportage: 'Sportage',
  sorento: 'Sorento',
  forte: 'Forte',
  seltos: 'Seltos',
  rio: 'Rio',
  soul: 'Soul',
  carnival: 'Carnival',
  sentra: 'Sentra',
  altima: 'Altima',
  pathfinder: 'Pathfinder',
  frontier: 'Frontier',
  kicks: 'Kicks',
  rogue: 'Rogue',
  versa: 'Versa',
  'x-trail': 'X-Trail',
  explorer: 'Explorer',
  escape: 'Escape',
  ranger: 'Ranger',
  'f-150': 'F-150',
  bronco: 'Bronco',
  expedition: 'Expedition',
  silverado: 'Silverado',
  equinox: 'Equinox',
  tahoe: 'Tahoe',
  trailblazer: 'Trailblazer',
  traverse: 'Traverse',
  colorado: 'Colorado',
  wrangler: 'Wrangler',
  'grand-cherokee': 'Grand Cherokee',
  compass: 'Compass',
  renegade: 'Renegade',
  gladiator: 'Gladiator',
  outlander: 'Outlander',
  l200: 'L200',
  asx: 'ASX',
  montero: 'Montero',
  'eclipse-cross': 'Eclipse Cross',
  vitara: 'Vitara',
  swift: 'Swift',
  jimny: 'Jimny',
  'grand-vitara': 'Grand Vitara',
  'cx-5': 'CX-5',
  'cx-30': 'CX-30',
  mazda3: 'Mazda3',
  'cx-9': 'CX-9',
  x3: 'X3',
  x5: 'X5',
  'serie-3': 'Serie 3',
  'clase-c': 'Clase C',
  gle: 'GLE',
  glc: 'GLC',
};

// ── Brand display names ──────────────────────────────────────────────────────
const BRAND_NAMES: Record<string, string> = {
  toyota: 'Toyota',
  honda: 'Honda',
  hyundai: 'Hyundai',
  kia: 'Kia',
  nissan: 'Nissan',
  mitsubishi: 'Mitsubishi',
  suzuki: 'Suzuki',
  chevrolet: 'Chevrolet',
  ford: 'Ford',
  jeep: 'Jeep',
  mazda: 'Mazda',
  bmw: 'BMW',
  'mercedes-benz': 'Mercedes-Benz',
  audi: 'Audi',
  volkswagen: 'Volkswagen',
  lexus: 'Lexus',
  subaru: 'Subaru',
  dodge: 'Dodge',
};

function capitalize(str: string): string {
  return str.charAt(0).toUpperCase() + str.slice(1).toLowerCase();
}

// ── Types ────────────────────────────────────────────────────────────────────
interface PageProps {
  params: Promise<{ marca: string; modelo: string }>;
}

// ── Static Params ────────────────────────────────────────────────────────────
export async function generateStaticParams() {
  return POPULAR_COMBINATIONS.map(combo => ({
    marca: combo.make,
    modelo: combo.model,
  }));
}

// ISR: revalidate every hour for fresh inventory
export const revalidate = 3600;

// ── Metadata ─────────────────────────────────────────────────────────────────
export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { marca, modelo } = await params;
  const brandName = BRAND_NAMES[marca.toLowerCase()] || capitalize(marca);
  const modelName = MODEL_DISPLAY_NAMES[modelo.toLowerCase()] || capitalize(modelo);
  const combo = `${brandName} ${modelName}`;
  const comboKey = `${marca.toLowerCase()}-${modelo.toLowerCase()}`;

  const description =
    MODEL_DESCRIPTIONS[comboKey] ||
    `Encuentra ${combo} usados y nuevos en venta en República Dominicana. Precios, fotos reales, comparaciones y ofertas verificadas en OKLA.`;

  return {
    title: `${combo} en Venta | ${combo} Usados y Nuevos en RD`,
    description,
    keywords: [
      `${combo} en venta`,
      `${combo} usados`,
      `${combo} precio RD`,
      `${combo} República Dominicana`,
      `comprar ${combo}`,
      `${combo} Santo Domingo`,
      `${combo} Santiago`,
      `${modelName} usados RD`,
      `${brandName} ${modelName} precio`,
    ],
    openGraph: {
      title: `${combo} en Venta en República Dominicana | OKLA`,
      description,
      type: 'website',
      locale: 'es_DO',
    },
    alternates: {
      canonical: `https://okla.com.do/marcas/${marca.toLowerCase()}/${modelo.toLowerCase()}`,
    },
  };
}

// ── Page Component ───────────────────────────────────────────────────────────
export default async function ModelPage({ params }: PageProps) {
  const { marca, modelo } = await params;
  const brandName = BRAND_NAMES[marca.toLowerCase()] || capitalize(marca);
  const modelName = MODEL_DISPLAY_NAMES[modelo.toLowerCase()] || capitalize(modelo);
  const combo = `${brandName} ${modelName}`;
  const comboKey = `${marca.toLowerCase()}-${modelo.toLowerCase()}`;

  const description =
    MODEL_DESCRIPTIONS[comboKey] ||
    `Encuentra ${combo} usados y nuevos en venta en República Dominicana. Precios, fotos reales y vendedores verificados.`;

  // JSON-LD structured data
  const jsonLd = {
    '@context': 'https://schema.org',
    '@type': 'ItemList',
    name: `${combo} en Venta en República Dominicana`,
    description,
    url: `https://okla.com.do/marcas/${marca.toLowerCase()}/${modelo.toLowerCase()}`,
    itemListOrder: 'https://schema.org/ItemListUnordered',
  };

  const breadcrumbJsonLd = generateBreadcrumbJsonLd([
    { name: 'Inicio', url: '/' },
    { name: 'Marcas', url: '/marcas' },
    { name: brandName, url: `/marcas/${marca.toLowerCase()}` },
    { name: modelName, url: `/marcas/${marca.toLowerCase()}/${modelo.toLowerCase()}` },
  ]);

  return (
    <div className="bg-background min-h-screen">
      {/* Hero Section */}
      <section className="bg-gradient-to-br from-gray-900 to-gray-800 py-16 text-white">
        <div className="container mx-auto px-4">
          {/* Breadcrumbs */}
          <nav className="mb-6 text-sm text-white/60">
            <Link href="/" className="hover:text-white/80">
              Inicio
            </Link>
            <span className="mx-2">›</span>
            <Link href="/marcas" className="hover:text-white/80">
              Marcas
            </Link>
            <span className="mx-2">›</span>
            <Link href={`/marcas/${marca.toLowerCase()}`} className="hover:text-white/80">
              {brandName}
            </Link>
            <span className="mx-2">›</span>
            <span className="text-white">{modelName}</span>
          </nav>

          <h1 className="text-3xl font-bold md:text-5xl">{combo} en Venta</h1>
          <p className="mt-4 max-w-2xl text-lg text-white/80">{description}</p>
        </div>
      </section>

      {/* Main Content — Vehicle Grid */}
      <section className="container mx-auto px-4 py-10">
        <Suspense
          fallback={
            <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
              {Array.from({ length: 6 }).map((_, i) => (
                <div key={i} className="bg-muted h-80 animate-pulse rounded-xl" />
              ))}
            </div>
          }
        >
          <ModelVehiclesClient
            brand={marca}
            model={modelo}
            brandName={brandName}
            modelName={modelName}
          />
        </Suspense>
      </section>

      {/* SEO Content Section */}
      <section className="bg-muted/50 py-14">
        <div className="container mx-auto max-w-3xl px-4">
          <h2 className="mb-4 text-2xl font-bold">¿Por qué comprar {combo} en OKLA?</h2>
          <div className="text-muted-foreground space-y-3">
            <p>
              En OKLA encontrarás la mayor selección de {combo} en República Dominicana. Todos los
              vehículos incluyen fotos reales, precios transparentes y vendedores verificados.
            </p>
            <p>
              Compara precios de {combo} usados y nuevos, revisa el historial de cada vehículo y
              contacta directamente al vendedor. Utiliza nuestra{' '}
              <a href="/herramientas/calculadora-financiamiento" className="text-primary underline">
                Calculadora de Financiamiento
              </a>{' '}
              para conocer tu cuota mensual estimada.
            </p>
            <p>
              ¿Buscas otros modelos de {brandName}?{' '}
              <Link href={`/marcas/${marca.toLowerCase()}`} className="text-primary underline">
                Ver todos los {brandName} disponibles
              </Link>
              .
            </p>
          </div>
        </div>
      </section>

      {/* JSON-LD */}
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
      />
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(breadcrumbJsonLd) }}
      />
    </div>
  );
}
