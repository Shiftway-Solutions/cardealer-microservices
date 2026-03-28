/**
 * Marcas Index Page — /marcas
 *
 * SEO landing page listing all vehicle brands available on OKLA.
 * Each brand links to its dedicated /marcas/[marca] page.
 *
 * ISR: revalidates every 6 hours.
 */

import type { Metadata } from 'next';
import Link from 'next/link';
import { Car, ChevronRight } from 'lucide-react';
import { generateBreadcrumbJsonLd } from '@/lib/seo';

// ── All brands with SEO data ─────────────────────────────────────────────────
const ALL_BRANDS = [
  { slug: 'toyota', name: 'Toyota', country: 'Japón', popular: true },
  { slug: 'honda', name: 'Honda', country: 'Japón', popular: true },
  { slug: 'hyundai', name: 'Hyundai', country: 'Corea del Sur', popular: true },
  { slug: 'kia', name: 'Kia', country: 'Corea del Sur', popular: true },
  { slug: 'nissan', name: 'Nissan', country: 'Japón', popular: true },
  { slug: 'mitsubishi', name: 'Mitsubishi', country: 'Japón', popular: true },
  { slug: 'suzuki', name: 'Suzuki', country: 'Japón', popular: true },
  { slug: 'chevrolet', name: 'Chevrolet', country: 'EE.UU.', popular: true },
  { slug: 'ford', name: 'Ford', country: 'EE.UU.', popular: true },
  { slug: 'jeep', name: 'Jeep', country: 'EE.UU.', popular: true },
  { slug: 'mazda', name: 'Mazda', country: 'Japón', popular: false },
  { slug: 'bmw', name: 'BMW', country: 'Alemania', popular: false },
  { slug: 'mercedes-benz', name: 'Mercedes-Benz', country: 'Alemania', popular: false },
  { slug: 'audi', name: 'Audi', country: 'Alemania', popular: false },
  { slug: 'volkswagen', name: 'Volkswagen', country: 'Alemania', popular: false },
  { slug: 'lexus', name: 'Lexus', country: 'Japón', popular: false },
  { slug: 'subaru', name: 'Subaru', country: 'Japón', popular: false },
  { slug: 'dodge', name: 'Dodge', country: 'EE.UU.', popular: false },
];

// ── Metadata ─────────────────────────────────────────────────────────────────
export const metadata: Metadata = {
  title: 'Todas las Marcas de Vehículos | Autos en Venta en RD',
  description:
    'Explora todas las marcas de vehículos disponibles en República Dominicana: Toyota, Honda, Hyundai, Kia, Nissan, Chevrolet, Ford, Jeep, BMW, Mercedes-Benz y más. Precios, modelos y ofertas verificadas.',
  keywords: [
    'marcas de carros RD',
    'autos en venta República Dominicana',
    'vehículos usados RD',
    'Toyota RD',
    'Honda RD',
    'Hyundai RD',
    'comprar carro Santo Domingo',
  ],
  openGraph: {
    title: 'Todas las Marcas de Vehículos en República Dominicana | OKLA',
    description:
      'Encuentra vehículos de todas las marcas en RD. Más de 18 marcas con fotos reales, precios transparentes y vendedores verificados.',
    type: 'website',
    locale: 'es_DO',
  },
  alternates: {
    canonical: 'https://okla.com.do/marcas',
  },
};

export const revalidate = 21600; // 6 hours

export default function MarcasIndexPage() {
  const popularBrands = ALL_BRANDS.filter(b => b.popular);
  const otherBrands = ALL_BRANDS.filter(b => !b.popular);

  const breadcrumbJsonLd = generateBreadcrumbJsonLd([
    { name: 'Inicio', url: '/' },
    { name: 'Marcas', url: '/marcas' },
  ]);

  const jsonLd = {
    '@context': 'https://schema.org',
    '@type': 'CollectionPage',
    name: 'Marcas de Vehículos en República Dominicana',
    description: 'Directorio completo de marcas de vehículos disponibles en OKLA.',
    url: 'https://okla.com.do/marcas',
    mainEntity: {
      '@type': 'ItemList',
      itemListElement: ALL_BRANDS.map((brand, i) => ({
        '@type': 'ListItem',
        position: i + 1,
        name: brand.name,
        url: `https://okla.com.do/marcas/${brand.slug}`,
      })),
    },
  };

  return (
    <div className="bg-background min-h-screen">
      {/* Hero Section */}
      <section className="bg-gradient-to-br from-gray-900 to-gray-800 py-16 text-white">
        <div className="container mx-auto px-4">
          <nav className="mb-6 text-sm text-white/60">
            <Link href="/" className="hover:text-white/80">
              Inicio
            </Link>
            <span className="mx-2">›</span>
            <span className="text-white">Marcas</span>
          </nav>

          <h1 className="text-3xl font-bold md:text-5xl">
            Marcas de Vehículos en República Dominicana
          </h1>
          <p className="mt-4 max-w-2xl text-lg text-white/80">
            Explora todas las marcas disponibles en OKLA. Encuentra el vehículo perfecto con fotos
            reales, precios transparentes y vendedores verificados.
          </p>
        </div>
      </section>

      {/* Popular Brands */}
      <section className="container mx-auto px-4 py-12">
        <h2 className="mb-6 text-2xl font-bold">Marcas Más Populares en RD</h2>
        <div className="grid gap-4 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-5">
          {popularBrands.map(brand => (
            <Link
              key={brand.slug}
              href={`/marcas/${brand.slug}`}
              className="group border-border bg-card hover:border-primary/30 flex items-center gap-3 rounded-xl border p-4 shadow-sm transition-all hover:-translate-y-0.5 hover:shadow-lg"
            >
              <div className="bg-primary/10 flex h-12 w-12 shrink-0 items-center justify-center rounded-lg">
                <Car className="text-primary h-6 w-6" />
              </div>
              <div className="min-w-0 flex-1">
                <h3 className="text-foreground group-hover:text-primary truncate font-semibold">
                  {brand.name}
                </h3>
                <p className="text-muted-foreground text-xs">{brand.country}</p>
              </div>
              <ChevronRight className="text-muted-foreground group-hover:text-primary h-4 w-4 shrink-0" />
            </Link>
          ))}
        </div>
      </section>

      {/* Other Brands */}
      <section className="container mx-auto px-4 pb-12">
        <h2 className="mb-6 text-2xl font-bold">Más Marcas Disponibles</h2>
        <div className="grid gap-4 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4">
          {otherBrands.map(brand => (
            <Link
              key={brand.slug}
              href={`/marcas/${brand.slug}`}
              className="group border-border bg-card hover:border-primary/30 flex items-center gap-3 rounded-xl border p-4 shadow-sm transition-all hover:shadow-md"
            >
              <div className="bg-muted flex h-10 w-10 shrink-0 items-center justify-center rounded-lg">
                <Car className="text-muted-foreground h-5 w-5" />
              </div>
              <div className="min-w-0 flex-1">
                <h3 className="text-foreground group-hover:text-primary truncate font-medium">
                  {brand.name}
                </h3>
                <p className="text-muted-foreground text-xs">{brand.country}</p>
              </div>
              <ChevronRight className="text-muted-foreground h-4 w-4 shrink-0" />
            </Link>
          ))}
        </div>
      </section>

      {/* SEO Content */}
      <section className="bg-muted/50 py-14">
        <div className="container mx-auto max-w-3xl px-4">
          <h2 className="mb-4 text-2xl font-bold">
            Compra tu Vehículo en República Dominicana con OKLA
          </h2>
          <div className="text-muted-foreground space-y-3">
            <p>
              OKLA es el marketplace de vehículos más completo de República Dominicana. Aquí
              encontrarás las marcas más vendidas como Toyota, Honda, Hyundai, Kia y Nissan, además
              de marcas premium como BMW, Mercedes-Benz, Audi y Lexus.
            </p>
            <p>
              Cada vehículo publicado en OKLA incluye fotos reales, historial verificado y precios
              transparentes. Utiliza nuestras herramientas gratuitas como la{' '}
              <a href="/herramientas/calculadora-financiamiento" className="text-primary underline">
                Calculadora de Financiamiento
              </a>{' '}
              para conocer tu cuota mensual antes de comprar.
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
