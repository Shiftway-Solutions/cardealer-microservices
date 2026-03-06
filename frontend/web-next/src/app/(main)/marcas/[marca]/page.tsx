import type { Metadata } from 'next';
import { Suspense } from 'react';
import { BrandVehiclesClient } from './brand-vehicles-client';

// Top 10 marcas en RD
const TOP_BRANDS = [
  'Toyota',
  'Honda',
  'Hyundai',
  'Kia',
  'Nissan',
  'Mitsubishi',
  'Suzuki',
  'Chevrolet',
  'Ford',
  'Jeep',
];

// Brand-specific descriptions for SEO
const BRAND_DESCRIPTIONS: Record<string, string> = {
  toyota:
    'Toyota es la marca más vendida en República Dominicana. Encuentra Corolla, Hilux, RAV4, Land Cruiser y más modelos Toyota usados y nuevos.',
  honda:
    'Honda es sinónimo de confiabilidad en RD. Descubre Civic, Accord, CR-V, HR-V y más modelos Honda en venta.',
  hyundai:
    'Hyundai ofrece excelente relación calidad-precio. Encuentra Tucson, Santa Fe, Elantra, Accent y más modelos Hyundai.',
  kia: 'Kia ha ganado terreno rápidamente en RD. Encuentra Sportage, Seltos, Forte, K5 y más modelos Kia.',
  nissan:
    'Nissan tiene una fuerte presencia en el mercado dominicano. Descubre Kicks, Frontier, Sentra, Pathfinder y más.',
  mitsubishi:
    'Mitsubishi es popular en RD por sus SUV y pickups. Encuentra L200, Outlander, ASX, Montero Sport y más.',
  suzuki:
    'Suzuki ofrece vehículos compactos y económicos ideales para la ciudad. Encuentra Swift, Vitara, Jimny y más.',
  chevrolet:
    'Chevrolet tiene una larga historia en RD. Descubre Tracker, Onix, Captiva, Silverado y más modelos Chevrolet.',
  ford: 'Ford ofrece pickups y SUV robustos. Encuentra Ranger, Explorer, Escape, Bronco Sport y más modelos Ford.',
  jeep: 'Jeep es sinónimo de aventura y off-road. Descubre Wrangler, Grand Cherokee, Compass, Renegade y más modelos Jeep.',
};

interface PageProps {
  params: Promise<{ marca: string }>;
}

function capitalize(str: string): string {
  return str.charAt(0).toUpperCase() + str.slice(1).toLowerCase();
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { marca } = await params;
  const brandName = capitalize(marca);
  const description =
    BRAND_DESCRIPTIONS[marca.toLowerCase()] ||
    `Encuentra los mejores ${brandName} usados y nuevos en República Dominicana. Precios, modelos y ofertas verificadas en OKLA.`;

  return {
    title: `${brandName} en Venta | Vehículos ${brandName} en RD | OKLA`,
    description,
    keywords: [
      `${brandName} en venta`,
      `${brandName} usados RD`,
      `${brandName} República Dominicana`,
      `comprar ${brandName}`,
      `precio ${brandName} RD`,
      `${brandName} Santo Domingo`,
    ],
    openGraph: {
      title: `${brandName} en Venta en República Dominicana | OKLA`,
      description,
      type: 'website',
      locale: 'es_DO',
    },
    alternates: {
      canonical: `https://okla.com.do/marcas/${marca.toLowerCase()}`,
    },
  };
}

export async function generateStaticParams() {
  return TOP_BRANDS.map(brand => ({
    marca: brand.toLowerCase(),
  }));
}

// ISR: revalidate every hour
export const revalidate = 3600;

export default async function BrandPage({ params }: PageProps) {
  const { marca } = await params;
  const brandName = capitalize(marca);
  const description =
    BRAND_DESCRIPTIONS[marca.toLowerCase()] ||
    `Encuentra los mejores ${brandName} usados y nuevos en República Dominicana.`;

  // JSON-LD structured data
  const jsonLd = {
    '@context': 'https://schema.org',
    '@type': 'ItemList',
    name: `Vehículos ${brandName} en Venta`,
    description,
    url: `https://okla.com.do/marcas/${marca.toLowerCase()}`,
    itemListOrder: 'https://schema.org/ItemListUnordered',
    numberOfItems: 0, // Will be updated client-side
  };

  return (
    <div className="bg-background min-h-screen">
      {/* Hero Section */}
      <section className="bg-gradient-to-br from-gray-900 to-gray-800 py-16 text-white">
        <div className="container mx-auto px-4">
          {/* Breadcrumbs */}
          <nav className="mb-6 text-sm text-white/60">
            <a href="/" className="hover:text-white/80">
              Inicio
            </a>
            <span className="mx-2">›</span>
            <a href="/marcas" className="hover:text-white/80">
              Marcas
            </a>
            <span className="mx-2">›</span>
            <span className="text-white">{brandName}</span>
          </nav>

          <h1 className="text-3xl font-bold md:text-5xl">Vehículos {brandName} en Venta</h1>
          <p className="mt-4 max-w-2xl text-lg text-white/80">{description}</p>
        </div>
      </section>

      {/* Main Content */}
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
          <BrandVehiclesClient brand={marca} brandName={brandName} />
        </Suspense>
      </section>

      {/* SEO Content Section */}
      <section className="bg-muted/50 py-14">
        <div className="container mx-auto max-w-3xl px-4">
          <h2 className="mb-4 text-2xl font-bold">¿Por qué comprar {brandName} en OKLA?</h2>
          <div className="text-muted-foreground space-y-3">
            <p>
              En OKLA encontrarás la mayor selección de vehículos {brandName} en República
              Dominicana, con fotos reales, precios transparentes y vendedores verificados.
            </p>
            <p>
              Utiliza nuestras herramientas gratuitas como la{' '}
              <a href="/herramientas/calculadora-financiamiento" className="text-primary underline">
                Calculadora de Financiamiento
              </a>{' '}
              para conocer tu cuota mensual, o la{' '}
              <a href="/herramientas/calculadora-importacion" className="text-primary underline">
                Calculadora de Importación
              </a>{' '}
              si estás pensando en traer un {brandName} desde Estados Unidos.
            </p>
          </div>
        </div>
      </section>

      {/* JSON-LD */}
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
      />
    </div>
  );
}
