/**
 * Dynamic Sitemap Generation
 *
 * Generates XML sitemap including:
 * - Static pages (home, about, etc.)
 * - Vehicle listings
 * - Dealer pages
 * - Category pages
 */

import { MetadataRoute } from 'next';

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || 'https://okla.com.do';

// Types for dynamic data fetching
interface VehicleSitemapEntry {
  slug: string;
  updatedAt?: string;
}

interface DealerSitemapEntry {
  slug: string;
  updatedAt?: string;
}

/**
 * Fetch vehicles for sitemap
 * In production, this would call the API
 */
async function getVehiclesForSitemap(): Promise<VehicleSitemapEntry[]> {
  try {
    // BFF pattern: server-side uses INTERNAL_API_URL for direct internal calls
    const apiUrl =
      process.env.INTERNAL_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:18443';
    const response = await fetch(`${apiUrl}/api/vehicles/sitemap`, {
      next: { revalidate: 3600 }, // Cache for 1 hour
    });

    if (!response.ok) {
      console.error('Failed to fetch vehicles for sitemap');
      return [];
    }

    return response.json();
  } catch (error) {
    console.error('Error fetching vehicles for sitemap:', error);
    // Return mock data in development
    return [
      { slug: 'toyota-corolla-2024-abc123', updatedAt: new Date().toISOString() },
      { slug: 'honda-civic-2023-def456', updatedAt: new Date().toISOString() },
      { slug: 'hyundai-tucson-2024-ghi789', updatedAt: new Date().toISOString() },
    ];
  }
}

/**
 * Fetch dealers for sitemap
 */
async function getDealersForSitemap(): Promise<DealerSitemapEntry[]> {
  try {
    // BFF pattern: server-side uses INTERNAL_API_URL for direct internal calls
    const apiUrl =
      process.env.INTERNAL_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:18443';
    const response = await fetch(`${apiUrl}/api/dealers/sitemap`, {
      next: { revalidate: 3600 }, // Cache for 1 hour
    });

    if (!response.ok) {
      console.error('Failed to fetch dealers for sitemap');
      return [];
    }

    return response.json();
  } catch (error) {
    console.error('Error fetching dealers for sitemap:', error);
    // Return mock data in development
    return [
      { slug: 'auto-plaza-santo-domingo', updatedAt: new Date().toISOString() },
      { slug: 'premium-motors-santiago', updatedAt: new Date().toISOString() },
    ];
  }
}

/**
 * Main sitemap generator function
 */
export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const now = new Date().toISOString();

  // Static pages
  const staticPages: MetadataRoute.Sitemap = [
    {
      url: SITE_URL,
      lastModified: now,
      changeFrequency: 'daily',
      priority: 1.0,
    },
    {
      url: `${SITE_URL}/vehiculos`,
      lastModified: now,
      changeFrequency: 'hourly',
      priority: 0.9,
    },
    {
      url: `${SITE_URL}/dealers`,
      lastModified: now,
      changeFrequency: 'daily',
      priority: 0.8,
    },
    {
      url: `${SITE_URL}/vender`,
      lastModified: now,
      changeFrequency: 'weekly',
      priority: 0.8,
    },
    {
      url: `${SITE_URL}/publicar`,
      lastModified: now,
      changeFrequency: 'weekly',
      priority: 0.7,
    },
    {
      url: `${SITE_URL}/nosotros`,
      lastModified: now,
      changeFrequency: 'monthly',
      priority: 0.5,
    },
    {
      url: `${SITE_URL}/contacto`,
      lastModified: now,
      changeFrequency: 'monthly',
      priority: 0.5,
    },
    {
      url: `${SITE_URL}/terminos`,
      lastModified: now,
      changeFrequency: 'yearly',
      priority: 0.3,
    },
    {
      url: `${SITE_URL}/privacidad`,
      lastModified: now,
      changeFrequency: 'yearly',
      priority: 0.3,
    },
    {
      url: `${SITE_URL}/precios`,
      lastModified: now,
      changeFrequency: 'monthly',
      priority: 0.7,
    },
    {
      url: `${SITE_URL}/ayuda`,
      lastModified: now,
      changeFrequency: 'monthly',
      priority: 0.5,
    },
    {
      url: `${SITE_URL}/blog`,
      lastModified: now,
      changeFrequency: 'weekly',
      priority: 0.6,
    },
    {
      url: `${SITE_URL}/guias`,
      lastModified: now,
      changeFrequency: 'monthly',
      priority: 0.5,
    },
    {
      url: `${SITE_URL}/seguridad`,
      lastModified: now,
      changeFrequency: 'monthly',
      priority: 0.4,
    },
    {
      url: `${SITE_URL}/reportar`,
      lastModified: now,
      changeFrequency: 'monthly',
      priority: 0.3,
    },
    {
      url: `${SITE_URL}/comparar`,
      lastModified: now,
      changeFrequency: 'daily',
      priority: 0.6,
    },
  ];

  // Category pages (makes)
  const popularMakes = [
    'toyota',
    'honda',
    'hyundai',
    'kia',
    'nissan',
    'ford',
    'chevrolet',
    'mazda',
    'bmw',
    'mercedes-benz',
    'audi',
    'volkswagen',
    'jeep',
    'mitsubishi',
    'suzuki',
  ];

  const makePages: MetadataRoute.Sitemap = popularMakes.map(make => ({
    url: `${SITE_URL}/vehiculos?make=${make}`,
    lastModified: now,
    changeFrequency: 'daily' as const,
    priority: 0.7,
  }));

  // Brand landing pages (SEO-optimized dedicated pages)
  const brandLandingPages: MetadataRoute.Sitemap = popularMakes.map(make => ({
    url: `${SITE_URL}/marcas/${make}`,
    lastModified: now,
    changeFrequency: 'daily' as const,
    priority: 0.8,
  }));

  // Body type pages
  const bodyTypes = ['sedan', 'suv', 'camioneta', 'pickup', 'coupe', 'hatchback', 'van'];

  const bodyTypePages: MetadataRoute.Sitemap = bodyTypes.map(type => ({
    url: `${SITE_URL}/vehiculos?bodyType=${type}`,
    lastModified: now,
    changeFrequency: 'daily' as const,
    priority: 0.6,
  }));

  // Province pages
  const provinces = [
    'santo-domingo',
    'santiago',
    'la-vega',
    'puerto-plata',
    'san-cristobal',
    'la-romana',
    'san-pedro-de-macoris',
    'punta-cana',
  ];

  const provincePages: MetadataRoute.Sitemap = provinces.map(province => ({
    url: `${SITE_URL}/vehiculos?province=${province}`,
    lastModified: now,
    changeFrequency: 'daily' as const,
    priority: 0.6,
  }));

  // Dynamic vehicle pages
  const vehicles = await getVehiclesForSitemap();
  const vehiclePages: MetadataRoute.Sitemap = vehicles.map(vehicle => ({
    url: `${SITE_URL}/vehiculos/${vehicle.slug}`,
    lastModified: vehicle.updatedAt || now,
    changeFrequency: 'weekly' as const,
    priority: 0.8,
  }));

  // Dynamic dealer pages
  const dealers = await getDealersForSitemap();
  const dealerPages: MetadataRoute.Sitemap = dealers.map(dealer => ({
    url: `${SITE_URL}/dealers/${dealer.slug}`,
    lastModified: dealer.updatedAt || now,
    changeFrequency: 'weekly' as const,
    priority: 0.7,
  }));

  // Combine all pages
  return [
    ...staticPages,
    ...makePages,
    ...brandLandingPages,
    ...bodyTypePages,
    ...provincePages,
    ...vehiclePages,
    ...dealerPages,
  ];
}
