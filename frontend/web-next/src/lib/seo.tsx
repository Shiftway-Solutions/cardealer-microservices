/**
 * SEO Utilities and Components
 *
 * Provides helpers for generating SEO metadata, structured data (JSON-LD),
 * and other search engine optimization features.
 */

import { Metadata } from 'next';

// =============================================================================
// TYPES
// =============================================================================

export interface VehicleSEO {
  id: string;
  slug: string;
  title: string;
  make: string;
  model: string;
  year: number;
  price: number;
  currency?: string;
  mileage?: number;
  fuelType?: string;
  transmission?: string;
  color?: string;
  condition?: 'new' | 'used';
  description?: string;
  images: string[];
  sellerName?: string;
  sellerType?: 'dealer' | 'seller';
  location?: {
    city: string;
    province: string;
    country?: string;
  };
  availability?: 'InStock' | 'OutOfStock' | 'PreOrder';
  createdAt?: string;
  updatedAt?: string;
}

export interface DealerSEO {
  id: string;
  slug: string;
  name: string;
  description?: string;
  logo?: string;
  coverImage?: string;
  address: string;
  city: string;
  province: string;
  phone?: string;
  email?: string;
  website?: string;
  rating?: number;
  reviewCount?: number;
  vehicleCount?: number;
  openingHours?: Array<{
    dayOfWeek: string;
    opens: string;
    closes: string;
  }>;
  socialMedia?: {
    facebook?: string;
    instagram?: string;
    twitter?: string;
  };
}

export interface ArticleSEO {
  title: string;
  slug: string;
  description: string;
  image?: string;
  author?: string;
  publishedAt: string;
  modifiedAt?: string;
  category?: string;
  tags?: string[];
}

// =============================================================================
// SITE CONFIG
// =============================================================================

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || 'https://okla.com.do';
const SITE_NAME = 'OKLA';
const DEFAULT_OG_IMAGE = `${SITE_URL}/og-image.jpg`;

// =============================================================================
// METADATA GENERATORS
// =============================================================================

/**
 * Generate metadata for vehicle detail pages
 */
export function generateVehicleMetadata(vehicle: VehicleSEO): Metadata {
  const title = `${vehicle.year} ${vehicle.make} ${vehicle.model} - ${formatPrice(vehicle.price)}`;
  const description =
    vehicle.description ||
    `Compra ${vehicle.year} ${vehicle.make} ${vehicle.model} por ${formatPrice(vehicle.price)}. ${vehicle.mileage ? `${vehicle.mileage.toLocaleString()} km` : ''} ${vehicle.transmission || ''} ${vehicle.fuelType || ''}. Disponible en ${vehicle.location?.city || 'República Dominicana'}.`;

  const url = `${SITE_URL}/vehiculos/${vehicle.slug}`;
  const images = vehicle.images.length > 0 ? vehicle.images : [DEFAULT_OG_IMAGE];

  return {
    title,
    description: description.slice(0, 160),
    keywords: [
      vehicle.make,
      vehicle.model,
      `${vehicle.year}`,
      'venta',
      'comprar',
      vehicle.location?.city || '',
      vehicle.condition === 'new' ? 'nuevo' : 'usado',
    ].filter(Boolean),
    alternates: {
      canonical: url,
    },
    openGraph: {
      type: 'website',
      url,
      title,
      description: description.slice(0, 200),
      siteName: SITE_NAME,
      locale: 'es_DO',
      images: images.slice(0, 4).map((img, i) => ({
        url: img,
        width: 1200,
        height: 630,
        alt: `${vehicle.make} ${vehicle.model} - Imagen ${i + 1}`,
      })),
    },
    twitter: {
      card: 'summary_large_image',
      title,
      description: description.slice(0, 200),
      images: images[0],
    },
  };
}

/**
 * Generate metadata for dealer pages
 */
export function generateDealerMetadata(dealer: DealerSEO): Metadata {
  const title = `${dealer.name} - Dealer de Vehículos en ${dealer.city}`;
  const description =
    dealer.description ||
    `Visita ${dealer.name} en ${dealer.city}, ${dealer.province}. ${dealer.vehicleCount ? `${dealer.vehicleCount}+ vehículos disponibles.` : ''} ${dealer.rating ? `Calificación: ${dealer.rating}/5` : ''}`;

  const url = `${SITE_URL}/dealers/${dealer.slug}`;
  const image = dealer.coverImage || dealer.logo || DEFAULT_OG_IMAGE;

  return {
    title,
    description: description.slice(0, 160),
    keywords: [dealer.name, 'dealer', 'concesionario', dealer.city, dealer.province, 'vehículos'],
    alternates: {
      canonical: url,
    },
    openGraph: {
      type: 'profile',
      url,
      title,
      description: description.slice(0, 200),
      siteName: SITE_NAME,
      locale: 'es_DO',
      images: [{ url: image, width: 1200, height: 630, alt: dealer.name }],
    },
    twitter: {
      card: 'summary_large_image',
      title,
      description: description.slice(0, 200),
      images: image,
    },
  };
}

/**
 * Generate metadata for search/listing pages
 */
export function generateSearchMetadata(params: {
  make?: string;
  model?: string;
  city?: string;
  type?: string;
  minPrice?: number;
  maxPrice?: number;
}): Metadata {
  const parts: string[] = [];

  if (params.make) parts.push(params.make);
  if (params.model) parts.push(params.model);
  if (params.type) parts.push(params.type);
  if (params.city) parts.push(`en ${params.city}`);
  if (params.minPrice || params.maxPrice) {
    const priceRange =
      params.minPrice && params.maxPrice
        ? `${formatPrice(params.minPrice)} - ${formatPrice(params.maxPrice)}`
        : params.minPrice
          ? `desde ${formatPrice(params.minPrice)}`
          : `hasta ${formatPrice(params.maxPrice!)}`;
    parts.push(priceRange);
  }

  const title = parts.length > 0 ? `${parts.join(' ')} | Vehículos en Venta` : 'Buscar Vehículos';

  const description = `Encuentra ${parts.join(' ')} en OKLA. Miles de vehículos verificados disponibles en República Dominicana.`;

  return {
    title,
    description,
    robots: {
      index: parts.length <= 3, // Don't index overly specific searches
      follow: true,
    },
  };
}

// =============================================================================
// JSON-LD STRUCTURED DATA GENERATORS
// =============================================================================

/**
 * Generate JSON-LD for Organization (site-wide)
 */
export function generateOrganizationJsonLd() {
  return {
    '@context': 'https://schema.org',
    '@type': 'Organization',
    '@id': `${SITE_URL}/#organization`,
    name: SITE_NAME,
    url: SITE_URL,
    logo: {
      '@type': 'ImageObject',
      url: `${SITE_URL}/logo.png`,
      width: 512,
      height: 512,
    },
    sameAs: [
      'https://facebook.com/okla.com.do',
      'https://instagram.com/okla.com.do',
      'https://twitter.com/okla',
    ],
    contactPoint: {
      '@type': 'ContactPoint',
      telephone: '+1-809-555-0123',
      contactType: 'customer service',
      areaServed: 'DO',
      availableLanguage: ['Spanish', 'English'],
    },
  };
}

/**
 * Generate JSON-LD for Website (site-wide)
 */
export function generateWebsiteJsonLd() {
  return {
    '@context': 'https://schema.org',
    '@type': 'WebSite',
    '@id': `${SITE_URL}/#website`,
    url: SITE_URL,
    name: SITE_NAME,
    description: 'Marketplace de vehículos #1 en República Dominicana',
    publisher: {
      '@id': `${SITE_URL}/#organization`,
    },
    potentialAction: {
      '@type': 'SearchAction',
      target: {
        '@type': 'EntryPoint',
        urlTemplate: `${SITE_URL}/vehiculos?q={search_term_string}`,
      },
      'query-input': 'required name=search_term_string',
    },
  };
}

/**
 * Generate JSON-LD for Vehicle (Product schema)
 */
export function generateVehicleJsonLd(vehicle: VehicleSEO) {
  const jsonLd: Record<string, unknown> = {
    '@context': 'https://schema.org',
    '@type': 'Car',
    '@id': `${SITE_URL}/vehiculos/${vehicle.slug}`,
    name: `${vehicle.year} ${vehicle.make} ${vehicle.model}`,
    description: vehicle.description || `${vehicle.year} ${vehicle.make} ${vehicle.model} en venta`,
    url: `${SITE_URL}/vehiculos/${vehicle.slug}`,
    image: vehicle.images,
    brand: {
      '@type': 'Brand',
      name: vehicle.make,
    },
    model: vehicle.model,
    vehicleModelDate: vehicle.year.toString(),
    color: vehicle.color,
    fuelType: mapFuelType(vehicle.fuelType),
    vehicleTransmission: mapTransmission(vehicle.transmission),
    mileageFromOdometer: vehicle.mileage
      ? {
          '@type': 'QuantitativeValue',
          value: vehicle.mileage,
          unitCode: 'KMT',
        }
      : undefined,
    itemCondition:
      vehicle.condition === 'new'
        ? 'https://schema.org/NewCondition'
        : 'https://schema.org/UsedCondition',
    offers: {
      '@type': 'Offer',
      url: `${SITE_URL}/vehiculos/${vehicle.slug}`,
      priceCurrency: vehicle.currency || 'DOP',
      price: vehicle.price,
      availability: `https://schema.org/${vehicle.availability || 'InStock'}`,
      seller: vehicle.sellerName
        ? {
            '@type': vehicle.sellerType === 'dealer' ? 'AutoDealer' : 'Person',
            name: vehicle.sellerName,
          }
        : undefined,
      areaServed: {
        '@type': 'Country',
        name: 'Dominican Republic',
      },
    },
  };

  if (vehicle.location) {
    jsonLd.vehicleLocation = {
      '@type': 'Place',
      address: {
        '@type': 'PostalAddress',
        addressLocality: vehicle.location.city,
        addressRegion: vehicle.location.province,
        addressCountry: vehicle.location.country || 'DO',
      },
    };
  }

  return jsonLd;
}

/**
 * Generate JSON-LD for Dealer (LocalBusiness/AutoDealer schema)
 */
export function generateDealerJsonLd(dealer: DealerSEO) {
  const jsonLd: Record<string, unknown> = {
    '@context': 'https://schema.org',
    '@type': 'AutoDealer',
    '@id': `${SITE_URL}/dealers/${dealer.slug}`,
    name: dealer.name,
    description: dealer.description,
    url: `${SITE_URL}/dealers/${dealer.slug}`,
    logo: dealer.logo,
    image: dealer.coverImage || dealer.logo,
    telephone: dealer.phone,
    email: dealer.email,
    address: {
      '@type': 'PostalAddress',
      streetAddress: dealer.address,
      addressLocality: dealer.city,
      addressRegion: dealer.province,
      addressCountry: 'DO',
    },
  };

  if (dealer.rating && dealer.reviewCount) {
    jsonLd.aggregateRating = {
      '@type': 'AggregateRating',
      ratingValue: dealer.rating,
      reviewCount: dealer.reviewCount,
      bestRating: 5,
      worstRating: 1,
    };
  }

  if (dealer.openingHours && dealer.openingHours.length > 0) {
    jsonLd.openingHoursSpecification = dealer.openingHours.map(hours => ({
      '@type': 'OpeningHoursSpecification',
      dayOfWeek: hours.dayOfWeek,
      opens: hours.opens,
      closes: hours.closes,
    }));
  }

  if (dealer.socialMedia) {
    jsonLd.sameAs = [
      dealer.socialMedia.facebook,
      dealer.socialMedia.instagram,
      dealer.socialMedia.twitter,
    ].filter(Boolean);
  }

  return jsonLd;
}

/**
 * Generate JSON-LD for BreadcrumbList
 */
export function generateBreadcrumbJsonLd(items: Array<{ name: string; url: string }>) {
  return {
    '@context': 'https://schema.org',
    '@type': 'BreadcrumbList',
    itemListElement: items.map((item, index) => ({
      '@type': 'ListItem',
      position: index + 1,
      name: item.name,
      item: item.url.startsWith('http') ? item.url : `${SITE_URL}${item.url}`,
    })),
  };
}

/**
 * Generate JSON-LD for ItemList (vehicle listings, search results)
 */
export function generateItemListJsonLd(
  items: Array<{
    name: string;
    url: string;
    image?: string;
    price?: number;
    currency?: string;
  }>
) {
  return {
    '@context': 'https://schema.org',
    '@type': 'ItemList',
    numberOfItems: items.length,
    itemListElement: items.map((item, index) => ({
      '@type': 'ListItem',
      position: index + 1,
      name: item.name,
      url: item.url.startsWith('http') ? item.url : `${SITE_URL}${item.url}`,
      ...(item.image && { image: item.image }),
    })),
  };
}

/**
 * Generate JSON-LD for FAQ page
 */
export function generateFAQJsonLd(faqs: Array<{ question: string; answer: string }>) {
  return {
    '@context': 'https://schema.org',
    '@type': 'FAQPage',
    mainEntity: faqs.map(faq => ({
      '@type': 'Question',
      name: faq.question,
      acceptedAnswer: {
        '@type': 'Answer',
        text: faq.answer,
      },
    })),
  };
}

/**
 * Generate JSON-LD for Article/Blog post
 */
export function generateArticleJsonLd(article: ArticleSEO) {
  return {
    '@context': 'https://schema.org',
    '@type': 'Article',
    headline: article.title,
    description: article.description,
    image: article.image || DEFAULT_OG_IMAGE,
    author: {
      '@type': 'Organization',
      name: article.author || SITE_NAME,
    },
    publisher: {
      '@id': `${SITE_URL}/#organization`,
    },
    datePublished: article.publishedAt,
    dateModified: article.modifiedAt || article.publishedAt,
    mainEntityOfPage: {
      '@type': 'WebPage',
      '@id': `${SITE_URL}/blog/${article.slug}`,
    },
  };
}

// =============================================================================
// HELPER FUNCTIONS
// =============================================================================

function formatPrice(price: number, currency = 'DOP'): string {
  return new Intl.NumberFormat('es-DO', {
    style: 'currency',
    currency,
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(price);
}

function mapFuelType(fuelType?: string): string | undefined {
  const mapping: Record<string, string> = {
    gasolina: 'https://schema.org/Gasoline',
    diesel: 'https://schema.org/Diesel',
    electrico: 'https://schema.org/Electricity',
    híbrido: 'https://schema.org/HybridElectric',
    gas: 'https://schema.org/NaturalGas',
  };
  return fuelType ? mapping[fuelType.toLowerCase()] : undefined;
}

function mapTransmission(transmission?: string): string | undefined {
  const mapping: Record<string, string> = {
    automatica: 'https://schema.org/AutomaticTransmission',
    automática: 'https://schema.org/AutomaticTransmission',
    manual: 'https://schema.org/ManualTransmission',
  };
  return transmission ? mapping[transmission.toLowerCase()] : undefined;
}

// =============================================================================
// REACT COMPONENTS
// =============================================================================

interface JsonLdProps {
  data: Record<string, unknown>;
}

/**
 * Component to render JSON-LD script tag
 */
export function JsonLd({ data }: JsonLdProps) {
  return (
    <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(data) }} />
  );
}

/**
 * Combined JSON-LD for site-wide schemas
 */
export function SiteJsonLd() {
  return (
    <>
      <JsonLd data={generateOrganizationJsonLd()} />
      <JsonLd data={generateWebsiteJsonLd()} />
    </>
  );
}
