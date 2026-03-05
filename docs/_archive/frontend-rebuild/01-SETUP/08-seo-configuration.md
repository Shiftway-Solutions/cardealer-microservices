# 🔍 SEO Configuration

> **Tiempo estimado:** 45 minutos
> **Prerrequisitos:** Next.js 14+ App Router
> **Última actualización:** Enero 2026

---

## 📋 OBJETIVO

Configurar SEO completo para OKLA marketplace:

- Meta tags dinámicos por página
- Open Graph y Twitter Cards
- Schema.org / JSON-LD para vehículos
- Sitemap dinámico
- Robots.txt
- Canonical URLs

---

## 🎯 REQUISITOS SEO PARA MARKETPLACE

| Elemento             | Importancia | Notas                   |
| -------------------- | ----------- | ----------------------- |
| **Title tags**       | 🔴 Crítico  | Único por vehículo      |
| **Meta description** | 🔴 Crítico  | 150-160 caracteres      |
| **Open Graph**       | 🟠 Alto     | Para compartir en redes |
| **JSON-LD Vehicle**  | 🟠 Alto     | Rich snippets en Google |
| **Sitemap**          | 🟠 Alto     | Indexación eficiente    |
| **Canonical URLs**   | 🟡 Medio    | Evitar duplicados       |
| **Hreflang**         | 🟡 Medio    | Para i18n               |

---

## 🔧 PASO 1: Metadata Base

```typescript
// filepath: src/app/layout.tsx
import type { Metadata, Viewport } from "next";

export const viewport: Viewport = {
  width: "device-width",
  initialScale: 1,
  maximumScale: 5,
  themeColor: [
    { media: "(prefers-color-scheme: light)", color: "#3b82f6" },
    { media: "(prefers-color-scheme: dark)", color: "#1e40af" },
  ],
};

export const metadata: Metadata = {
  // Título base
  title: {
    default: "OKLA - Compra y Vende Vehículos en República Dominicana",
    template: "%s | OKLA",
  },

  // Descripción base
  description:
    "El marketplace #1 de vehículos en República Dominicana. Encuentra autos nuevos y usados, SUVs, camionetas y más. Compra seguro con vendedores verificados.",

  // Keywords (menos importante hoy, pero útil)
  keywords: [
    "carros en venta RD",
    "vehículos República Dominicana",
    "comprar auto Santo Domingo",
    "vender carro RD",
    "autos usados RD",
    "concesionarios República Dominicana",
  ],

  // Autor y publisher
  authors: [{ name: "OKLA", url: "https://okla.com.do" }],
  creator: "OKLA",
  publisher: "OKLA",

  // Robots base
  robots: {
    index: true,
    follow: true,
    googleBot: {
      index: true,
      follow: true,
      "max-video-preview": -1,
      "max-image-preview": "large",
      "max-snippet": -1,
    },
  },

  // URLs alternativas
  alternates: {
    canonical: "https://okla.com.do",
    languages: {
      "es-DO": "https://okla.com.do",
      "en-US": "https://okla.com.do/en-US",
    },
  },

  // Open Graph base
  openGraph: {
    type: "website",
    locale: "es_DO",
    alternateLocale: "en_US",
    url: "https://okla.com.do",
    siteName: "OKLA",
    title: "OKLA - Compra y Vende Vehículos en República Dominicana",
    description:
      "El marketplace #1 de vehículos en RD. Autos nuevos y usados con vendedores verificados.",
    images: [
      {
        url: "https://okla.com.do/og-image.jpg",
        width: 1200,
        height: 630,
        alt: "OKLA - Marketplace de Vehículos",
      },
    ],
  },

  // Twitter Card
  twitter: {
    card: "summary_large_image",
    site: "@okla_rd",
    creator: "@okla_rd",
    title: "OKLA - Compra y Vende Vehículos en RD",
    description: "El marketplace #1 de vehículos en República Dominicana.",
    images: ["https://okla.com.do/twitter-image.jpg"],
  },

  // Íconos
  icons: {
    icon: [
      { url: "/favicon-16x16.png", sizes: "16x16", type: "image/png" },
      { url: "/favicon-32x32.png", sizes: "32x32", type: "image/png" },
    ],
    apple: [{ url: "/apple-touch-icon.png", sizes: "180x180" }],
    other: [
      { rel: "mask-icon", url: "/safari-pinned-tab.svg", color: "#3b82f6" },
    ],
  },

  // Manifest PWA
  manifest: "/manifest.json",

  // Verificación de propiedad
  verification: {
    google: "google-site-verification-code",
    yandex: "yandex-verification-code",
  },

  // Categoría
  category: "automotive",
};
```

---

## 🔧 PASO 2: Metadata Dinámico para Vehículos

```typescript
// filepath: src/app/vehiculos/[slug]/page.tsx
import type { Metadata, ResolvingMetadata } from "next";
import { vehicleService } from "@/services/vehicleService";
import { formatCurrency } from "@/lib/formatters/currency";

interface PageProps {
  params: { slug: string };
}

// Generar metadata dinámico
export async function generateMetadata(
  { params }: PageProps,
  parent: ResolvingMetadata,
): Promise<Metadata> {
  const vehicle = await vehicleService.getBySlug(params.slug);

  if (!vehicle) {
    return {
      title: "Vehículo no encontrado",
      description: "Este vehículo ya no está disponible.",
    };
  }

  const title = `${vehicle.year} ${vehicle.make} ${vehicle.model} - ${formatCurrency(vehicle.price, { locale: "es-DO", currency: "DOP", compact: true })}`;

  const description = `${vehicle.condition === "new" ? "Nuevo" : "Usado"} ${vehicle.year} ${vehicle.make} ${vehicle.model} ${vehicle.trim || ""} en ${vehicle.city}. ${vehicle.mileage.toLocaleString()} km, ${vehicle.transmission}, ${vehicle.fuelType}. ${vehicle.description?.slice(0, 100) || ""}`;

  const images =
    vehicle.images?.map((img) => ({
      url: img.url,
      width: 1200,
      height: 800,
      alt: `${vehicle.year} ${vehicle.make} ${vehicle.model}`,
    })) || [];

  return {
    title,
    description,

    // Open Graph para vehículo
    openGraph: {
      type: "website", // og:type product requiere más campos
      title,
      description,
      url: `https://okla.com.do/vehiculos/${params.slug}`,
      images,
      locale: "es_DO",
      siteName: "OKLA",
    },

    // Twitter
    twitter: {
      card: "summary_large_image",
      title,
      description,
      images: images[0]?.url ? [images[0].url] : undefined,
    },

    // Canonical
    alternates: {
      canonical: `https://okla.com.do/vehiculos/${params.slug}`,
    },

    // Robots (no indexar si está vendido o inactivo)
    robots:
      vehicle.status === "Active"
        ? {
            index: true,
            follow: true,
          }
        : {
            index: false,
            follow: false,
          },
  };
}

export default function VehicleDetailPage({ params }: PageProps) {
  // ... page component
}
```

---

## 🔧 PASO 3: JSON-LD Schema para Vehículos

```typescript
// filepath: src/components/seo/VehicleJsonLd.tsx
import Script from 'next/script';
import type { Vehicle } from '@/types';

interface VehicleJsonLdProps {
  vehicle: Vehicle;
}

export function VehicleJsonLd({ vehicle }: VehicleJsonLdProps) {
  const schema = {
    '@context': 'https://schema.org',
    '@type': 'Vehicle',

    // Identificadores
    name: `${vehicle.year} ${vehicle.make} ${vehicle.model}`,
    vehicleIdentificationNumber: vehicle.vin,

    // Marca y modelo
    brand: {
      '@type': 'Brand',
      name: vehicle.make,
    },
    model: vehicle.model,
    vehicleModelDate: vehicle.year.toString(),

    // Características
    mileageFromOdometer: {
      '@type': 'QuantitativeValue',
      value: vehicle.mileage,
      unitCode: 'KMT',
    },
    vehicleTransmission: vehicle.transmission,
    fuelType: mapFuelType(vehicle.fuelType),
    color: vehicle.exteriorColor,
    vehicleInteriorColor: vehicle.interiorColor,
    numberOfDoors: vehicle.doors,
    vehicleSeatingCapacity: vehicle.seats,
    vehicleEngine: vehicle.engineSize ? {
      '@type': 'EngineSpecification',
      engineDisplacement: {
        '@type': 'QuantitativeValue',
        value: vehicle.engineSize,
        unitCode: 'LTR',
      },
    } : undefined,

    // Condición
    itemCondition: vehicle.condition === 'new'
      ? 'https://schema.org/NewCondition'
      : 'https://schema.org/UsedCondition',

    // Precio
    offers: {
      '@type': 'Offer',
      price: vehicle.price,
      priceCurrency: 'DOP',
      availability: vehicle.status === 'Active'
        ? 'https://schema.org/InStock'
        : 'https://schema.org/SoldOut',
      seller: {
        '@type': vehicle.sellerType === 'dealer' ? 'AutoDealer' : 'Person',
        name: vehicle.sellerName,
        ...(vehicle.sellerType === 'dealer' && {
          address: {
            '@type': 'PostalAddress',
            addressLocality: vehicle.city,
            addressCountry: 'DO',
          },
        }),
      },
    },

    // Imágenes
    image: vehicle.images?.map((img) => img.url) || [],

    // Ubicación
    vehicleConfiguration: vehicle.bodyType,

    // URL
    url: `https://okla.com.do/vehiculos/${vehicle.slug}`,
  };

  return (
    <Script
      id="vehicle-jsonld"
      type="application/ld+json"
      dangerouslySetInnerHTML={{ __html: JSON.stringify(schema) }}
    />
  );
}

function mapFuelType(fuel: string): string {
  const mapping: Record<string, string> = {
    gasoline: 'https://schema.org/Gasoline',
    diesel: 'https://schema.org/Diesel',
    electric: 'https://schema.org/Electricity',
    hybrid: 'https://schema.org/HybridElectric',
  };
  return mapping[fuel.toLowerCase()] || fuel;
}
```

### JSON-LD para Organización

```typescript
// filepath: src/components/seo/OrganizationJsonLd.tsx
import Script from 'next/script';

export function OrganizationJsonLd() {
  const schema = {
    '@context': 'https://schema.org',
    '@type': 'Organization',
    name: 'OKLA',
    url: 'https://okla.com.do',
    logo: 'https://okla.com.do/logo.png',
    description: 'El marketplace #1 de vehículos en República Dominicana',

    // Redes sociales
    sameAs: [
      'https://facebook.com/oklard',
      'https://instagram.com/okla_rd',
      'https://twitter.com/okla_rd',
      'https://linkedin.com/company/okla',
    ],

    // Contacto
    contactPoint: {
      '@type': 'ContactPoint',
      telephone: '+1-809-555-0100',
      contactType: 'customer service',
      availableLanguage: ['Spanish', 'English'],
      areaServed: 'DO',
    },

    // Dirección
    address: {
      '@type': 'PostalAddress',
      streetAddress: 'Av. Winston Churchill',
      addressLocality: 'Santo Domingo',
      addressCountry: 'DO',
    },
  };

  return (
    <Script
      id="organization-jsonld"
      type="application/ld+json"
      dangerouslySetInnerHTML={{ __html: JSON.stringify(schema) }}
    />
  );
}
```

### JSON-LD para Búsquedas (SearchAction)

```typescript
// filepath: src/components/seo/WebsiteJsonLd.tsx
import Script from 'next/script';

export function WebsiteJsonLd() {
  const schema = {
    '@context': 'https://schema.org',
    '@type': 'WebSite',
    name: 'OKLA',
    url: 'https://okla.com.do',

    // Búsqueda en el sitio
    potentialAction: {
      '@type': 'SearchAction',
      target: {
        '@type': 'EntryPoint',
        urlTemplate: 'https://okla.com.do/buscar?q={search_term_string}',
      },
      'query-input': 'required name=search_term_string',
    },
  };

  return (
    <Script
      id="website-jsonld"
      type="application/ld+json"
      dangerouslySetInnerHTML={{ __html: JSON.stringify(schema) }}
    />
  );
}
```

---

## 🔧 PASO 4: Sitemap Dinámico

```typescript
// filepath: src/app/sitemap.ts
import { MetadataRoute } from "next";
import { vehicleService } from "@/services/vehicleService";
import { dealerService } from "@/services/dealerService";

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const baseUrl = "https://okla.com.do";

  // Páginas estáticas
  const staticPages: MetadataRoute.Sitemap = [
    {
      url: baseUrl,
      lastModified: new Date(),
      changeFrequency: "daily",
      priority: 1.0,
    },
    {
      url: `${baseUrl}/buscar`,
      lastModified: new Date(),
      changeFrequency: "hourly",
      priority: 0.9,
    },
    {
      url: `${baseUrl}/vender`,
      lastModified: new Date(),
      changeFrequency: "weekly",
      priority: 0.8,
    },
    {
      url: `${baseUrl}/dealer/registro`,
      lastModified: new Date(),
      changeFrequency: "monthly",
      priority: 0.7,
    },
    {
      url: `${baseUrl}/ayuda`,
      lastModified: new Date(),
      changeFrequency: "weekly",
      priority: 0.5,
    },
    {
      url: `${baseUrl}/sobre-nosotros`,
      lastModified: new Date(),
      changeFrequency: "monthly",
      priority: 0.4,
    },
    {
      url: `${baseUrl}/contacto`,
      lastModified: new Date(),
      changeFrequency: "monthly",
      priority: 0.4,
    },
    {
      url: `${baseUrl}/terminos`,
      lastModified: new Date(),
      changeFrequency: "yearly",
      priority: 0.2,
    },
    {
      url: `${baseUrl}/privacidad`,
      lastModified: new Date(),
      changeFrequency: "yearly",
      priority: 0.2,
    },
  ];

  // Páginas de vehículos (dinámicas)
  const vehicles = await vehicleService.getAllForSitemap();
  const vehiclePages: MetadataRoute.Sitemap = vehicles.map((vehicle) => ({
    url: `${baseUrl}/vehiculos/${vehicle.slug}`,
    lastModified: new Date(vehicle.updatedAt),
    changeFrequency: "daily" as const,
    priority: 0.8,
  }));

  // Páginas de dealers
  const dealers = await dealerService.getAllForSitemap();
  const dealerPages: MetadataRoute.Sitemap = dealers.map((dealer) => ({
    url: `${baseUrl}/dealer/${dealer.slug}`,
    lastModified: new Date(dealer.updatedAt),
    changeFrequency: "weekly" as const,
    priority: 0.6,
  }));

  // Páginas de categorías
  const categories = [
    "sedanes",
    "suvs",
    "camionetas",
    "deportivos",
    "electricos",
    "lujo",
  ];
  const categoryPages: MetadataRoute.Sitemap = categories.map((category) => ({
    url: `${baseUrl}/vehiculos/categoria/${category}`,
    lastModified: new Date(),
    changeFrequency: "daily" as const,
    priority: 0.7,
  }));

  // Páginas de marcas
  const makes = await vehicleService.getAllMakes();
  const makePages: MetadataRoute.Sitemap = makes.map((make) => ({
    url: `${baseUrl}/vehiculos/marca/${make.slug}`,
    lastModified: new Date(),
    changeFrequency: "daily" as const,
    priority: 0.7,
  }));

  return [
    ...staticPages,
    ...vehiclePages,
    ...dealerPages,
    ...categoryPages,
    ...makePages,
  ];
}
```

### Sitemap Index para Sitios Grandes

```typescript
// filepath: src/app/sitemap/[id]/route.ts
// Para sitios con muchas páginas (>50,000), dividir en múltiples sitemaps

import { NextRequest } from "next/server";
import { vehicleService } from "@/services/vehicleService";

export async function GET(
  request: NextRequest,
  { params }: { params: { id: string } },
) {
  const page = parseInt(params.id);
  const pageSize = 10000; // 10,000 URLs por sitemap

  const vehicles = await vehicleService.getForSitemap({
    page,
    pageSize,
  });

  const xml = generateSitemapXml(vehicles);

  return new Response(xml, {
    headers: {
      "Content-Type": "application/xml",
      "Cache-Control": "public, max-age=3600, s-maxage=3600",
    },
  });
}

function generateSitemapXml(
  vehicles: Array<{ slug: string; updatedAt: string }>,
) {
  const baseUrl = "https://okla.com.do";

  return `<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
${vehicles
  .map(
    (v) => `
  <url>
    <loc>${baseUrl}/vehiculos/${v.slug}</loc>
    <lastmod>${new Date(v.updatedAt).toISOString()}</lastmod>
    <changefreq>daily</changefreq>
    <priority>0.8</priority>
  </url>
`,
  )
  .join("")}
</urlset>`;
}
```

---

## 🔧 PASO 5: Robots.txt

```typescript
// filepath: src/app/robots.ts
import { MetadataRoute } from "next";

export default function robots(): MetadataRoute.Robots {
  const baseUrl = "https://okla.com.do";

  return {
    rules: [
      {
        userAgent: "*",
        allow: "/",
        disallow: [
          "/api/",
          "/dashboard/",
          "/dealer/dashboard/",
          "/admin/",
          "/settings/",
          "/mensajes/",
          "/checkout/",
          "/_next/",
          "/static/",
          "/*.json$",
        ],
      },
      {
        userAgent: "Googlebot",
        allow: "/",
        disallow: ["/api/", "/dashboard/", "/admin/", "/checkout/"],
      },
      {
        // Bloquear bots malos conocidos
        userAgent: ["AhrefsBot", "SemrushBot", "MJ12bot"],
        disallow: "/",
      },
    ],
    sitemap: `${baseUrl}/sitemap.xml`,
  };
}
```

---

## 🔧 PASO 6: Canonical URLs y Hreflang

```typescript
// filepath: src/lib/seo/canonical.ts
import { headers } from "next/headers";

export function getCanonicalUrl(path: string = ""): string {
  const baseUrl = "https://okla.com.do";

  // Limpiar path
  const cleanPath = path
    .replace(/\/+$/, "") // Remove trailing slashes
    .replace(/^\/+/, "/"); // Ensure single leading slash

  return `${baseUrl}${cleanPath}`;
}

export function getAlternateUrls(path: string = "") {
  const baseUrl = "https://okla.com.do";

  return {
    canonical: `${baseUrl}${path}`,
    "es-DO": `${baseUrl}${path}`,
    "en-US": `${baseUrl}/en-US${path}`,
    "x-default": `${baseUrl}${path}`,
  };
}
```

### Componente de Hreflang

```typescript
// filepath: src/components/seo/HreflangTags.tsx
import { getAlternateUrls } from '@/lib/seo/canonical';

interface HreflangTagsProps {
  path: string;
}

export function HreflangTags({ path }: HreflangTagsProps) {
  const urls = getAlternateUrls(path);

  return (
    <>
      <link rel="canonical" href={urls.canonical} />
      <link rel="alternate" hrefLang="es-DO" href={urls['es-DO']} />
      <link rel="alternate" hrefLang="en-US" href={urls['en-US']} />
      <link rel="alternate" hrefLang="x-default" href={urls['x-default']} />
    </>
  );
}
```

---

## 🔧 PASO 7: Breadcrumbs con Schema

```typescript
// filepath: src/components/seo/BreadcrumbJsonLd.tsx
import Script from 'next/script';

interface BreadcrumbItem {
  name: string;
  href: string;
}

interface BreadcrumbJsonLdProps {
  items: BreadcrumbItem[];
}

export function BreadcrumbJsonLd({ items }: BreadcrumbJsonLdProps) {
  const schema = {
    '@context': 'https://schema.org',
    '@type': 'BreadcrumbList',
    itemListElement: items.map((item, index) => ({
      '@type': 'ListItem',
      position: index + 1,
      name: item.name,
      item: `https://okla.com.do${item.href}`,
    })),
  };

  return (
    <Script
      id="breadcrumb-jsonld"
      type="application/ld+json"
      dangerouslySetInnerHTML={{ __html: JSON.stringify(schema) }}
    />
  );
}
```

### Uso en Página de Vehículo

```typescript
// filepath: src/app/vehiculos/[slug]/page.tsx
import { BreadcrumbJsonLd } from '@/components/seo/BreadcrumbJsonLd';
import { VehicleJsonLd } from '@/components/seo/VehicleJsonLd';

export default function VehicleDetailPage({ vehicle }) {
  const breadcrumbs = [
    { name: 'Inicio', href: '/' },
    { name: 'Vehículos', href: '/buscar' },
    { name: vehicle.make, href: `/vehiculos/marca/${vehicle.makeSlug}` },
    { name: `${vehicle.year} ${vehicle.model}`, href: `/vehiculos/${vehicle.slug}` },
  ];

  return (
    <>
      <BreadcrumbJsonLd items={breadcrumbs} />
      <VehicleJsonLd vehicle={vehicle} />

      {/* Breadcrumbs visual */}
      <nav aria-label="Breadcrumb" className="mb-4">
        <ol className="flex items-center gap-2 text-sm text-gray-600">
          {breadcrumbs.map((item, index) => (
            <li key={item.href} className="flex items-center gap-2">
              {index > 0 && <span>/</span>}
              {index === breadcrumbs.length - 1 ? (
                <span className="text-gray-900 font-medium">{item.name}</span>
              ) : (
                <Link href={item.href} className="hover:text-primary-600">
                  {item.name}
                </Link>
              )}
            </li>
          ))}
        </ol>
      </nav>

      {/* Resto de la página */}
    </>
  );
}
```

---

## 🔧 PASO 8: Meta Tags para Páginas de Categoría

```typescript
// filepath: src/app/vehiculos/categoria/[category]/page.tsx
import type { Metadata } from "next";

const categoryMeta: Record<string, { title: string; description: string }> = {
  sedanes: {
    title: "Sedanes en Venta en RD",
    description:
      "Encuentra sedanes nuevos y usados en República Dominicana. Toyota Camry, Honda Accord, Hyundai Elantra y más.",
  },
  suvs: {
    title: "SUVs en Venta en RD",
    description:
      "Los mejores SUVs del mercado dominicano. Toyota RAV4, Honda CR-V, Hyundai Tucson y más opciones.",
  },
  camionetas: {
    title: "Camionetas en Venta en RD",
    description:
      "Camionetas pickup para trabajo y aventura. Toyota Hilux, Ford Ranger, Nissan Frontier disponibles.",
  },
  deportivos: {
    title: "Autos Deportivos en RD",
    description:
      "Vehículos deportivos y de alto rendimiento en República Dominicana. BMW, Mercedes, Audi y más.",
  },
  electricos: {
    title: "Vehículos Eléctricos en RD",
    description:
      "Carros eléctricos e híbridos en República Dominicana. Tesla, Toyota Prius, Hyundai Ioniq.",
  },
  lujo: {
    title: "Autos de Lujo en RD",
    description:
      "Vehículos premium y de lujo en República Dominicana. Mercedes-Benz, BMW, Lexus, Audi.",
  },
};

export async function generateMetadata({
  params,
}: {
  params: { category: string };
}): Promise<Metadata> {
  const meta = categoryMeta[params.category];

  if (!meta) {
    return {
      title: "Categoría no encontrada",
    };
  }

  return {
    title: meta.title,
    description: meta.description,
    openGraph: {
      title: meta.title,
      description: meta.description,
      url: `https://okla.com.do/vehiculos/categoria/${params.category}`,
    },
  };
}
```

---

## 🧪 Testing SEO

```typescript
// e2e/seo.spec.ts
import { test, expect } from "@playwright/test";

test.describe("SEO", () => {
  test("homepage has correct meta tags", async ({ page }) => {
    await page.goto("/");

    // Title
    await expect(page).toHaveTitle(/OKLA/);

    // Meta description
    const description = await page
      .locator('meta[name="description"]')
      .getAttribute("content");
    expect(description).toContain("vehículos");
    expect(description?.length).toBeLessThanOrEqual(160);

    // Open Graph
    const ogTitle = await page
      .locator('meta[property="og:title"]')
      .getAttribute("content");
    expect(ogTitle).toBeTruthy();

    const ogImage = await page
      .locator('meta[property="og:image"]')
      .getAttribute("content");
    expect(ogImage).toContain("https://");

    // Canonical
    const canonical = await page
      .locator('link[rel="canonical"]')
      .getAttribute("href");
    expect(canonical).toBe("https://okla.com.do");
  });

  test("vehicle page has JSON-LD", async ({ page }) => {
    await page.goto("/vehiculos/toyota-camry-2024");

    // Verificar JSON-LD
    const jsonLd = await page
      .locator('script[type="application/ld+json"]')
      .allTextContents();
    expect(jsonLd.length).toBeGreaterThan(0);

    const vehicleSchema = JSON.parse(
      jsonLd.find((s) => s.includes('"@type":"Vehicle"')) || "{}",
    );
    expect(vehicleSchema["@type"]).toBe("Vehicle");
    expect(vehicleSchema.brand).toBeDefined();
    expect(vehicleSchema.offers).toBeDefined();
  });

  test("sitemap is accessible", async ({ request }) => {
    const response = await request.get("/sitemap.xml");
    expect(response.status()).toBe(200);
    expect(response.headers()["content-type"]).toContain("xml");
  });

  test("robots.txt is accessible", async ({ request }) => {
    const response = await request.get("/robots.txt");
    expect(response.status()).toBe(200);
    const text = await response.text();
    expect(text).toContain("Sitemap:");
    expect(text).toContain("User-agent:");
  });
});
```

---

## ✅ Checklist SEO

### Técnico

- [ ] Configurar metadata base en layout
- [ ] Implementar generateMetadata para páginas dinámicas
- [ ] Crear JSON-LD schemas (Vehicle, Organization, Website)
- [ ] Configurar sitemap dinámico
- [ ] Configurar robots.txt
- [ ] Implementar canonical URLs
- [ ] Agregar hreflang tags

### Contenido

- [ ] Títulos únicos por página (< 60 chars)
- [ ] Descripciones únicas (150-160 chars)
- [ ] Alt text en todas las imágenes
- [ ] Breadcrumbs con schema

### Verificación

- [ ] Validar con Google Search Console
- [ ] Test con Google Rich Results Test
- [ ] Verificar Open Graph con Facebook Debugger
- [ ] Test de Twitter Cards

---

## 🔗 Referencias

- [Next.js Metadata API](https://nextjs.org/docs/app/api-reference/functions/generate-metadata)
- [Schema.org Vehicle](https://schema.org/Vehicle)
- [Google Rich Results Test](https://search.google.com/test/rich-results)
- [Open Graph Protocol](https://ogp.me/)

---

_El SEO bien implementado puede aumentar el tráfico orgánico en un 200%+. Para un marketplace, es fundamental._
