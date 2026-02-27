/**
 * Homepage Client Component
 *
 * Client-side interactive parts of the homepage.
 * Receives pre-fetched data from server component and handles interactivity.
 */

'use client';

import { useMemo } from 'react';
import {
  HeroCompact,
  FeaturedListingGrid,
  FeaturedSection,
  SectionContainer,
  CategoryCards,
  BrandSlider,
  WhyChooseUs,
  CTASection,
  SkeletonGrid,
  type FeaturedListingItem,
} from '@/components/homepage';
import FeaturedVehicles from '@/components/advertising/featured-vehicles';
import { useBrands, useCategories } from '@/hooks/use-advertising';
import type { BrandConfig, CategoryImageConfig } from '@/types/advertising';
import {
  transformHomepageVehicleToVehicle,
  type HomepageSection,
  type HomepageVehicle,
  type Vehicle,
} from '@/services/homepage-sections';

// =============================================
// TRANSFORM HELPERS
// =============================================

/**
 * Transform a raw vehicle DTO (from /api/vehicles/featured) to the Vehicle
 * format expected by homepage components.
 */
const transformFeaturedDtoToVehicle = (dto: Record<string, unknown>): Vehicle => {
  const images = dto.images as Array<Record<string, unknown>> | undefined;
  const firstImage = images?.sort((a, b) => {
    const aOrder = (a.sortOrder as number) ?? (a.order as number) ?? 99;
    const bOrder = (b.sortOrder as number) ?? (b.order as number) ?? 99;
    return aOrder - bOrder;
  })[0];
  const imageUrl = (firstImage?.url as string) || '/placeholder-car.jpg';

  return {
    id: dto.id as string,
    make: dto.make as string,
    model: dto.model as string,
    year: dto.year as number,
    price: dto.price as number,
    mileage: dto.mileage as number,
    fuelType: String(dto.fuelType ?? ''),
    transmission: String(dto.transmission ?? ''),
    exteriorColor: String(dto.exteriorColor ?? ''),
    bodyStyle: String(dto.bodyType ?? dto.bodyStyle ?? ''),
    images: images?.map(i => i.url as string).filter(Boolean) || [imageUrl],
    condition: 'Used',
    location: [dto.city, dto.province || dto.state].filter(Boolean).join(', ') || 'Santo Domingo',
    tier: (dto.isFeatured ? 'featured' : 'basic') as Vehicle['tier'],
    featuredBadge: dto.isFeatured ? 'destacado' : undefined,
  };
};

const transformToFeaturedListing = (
  v: HomepageVehicle,
  categoryName: string
): FeaturedListingItem => ({
  id: v.id,
  title: `${v.year} ${v.make} ${v.model}`,
  price: v.price,
  mileage: v.mileage,
  location: 'Santo Domingo, RD',
  imageUrl: v.imageUrl || (v.imageUrls.length > 0 ? v.imageUrls[0] : '/placeholder-car.jpg'),
  category: categoryName,
  year: v.year,
  make: v.make,
  model: v.model,
  fuelType: v.fuelType,
  transmission: v.transmission,
});

const transformSectionVehicles = (section: HomepageSection | undefined): FeaturedListingItem[] => {
  if (!section || section.vehicles.length === 0) return [];
  return section.vehicles.map(v => transformToFeaturedListing(v, section.name));
};

// =============================================
// PROPS
// =============================================

interface HomepageClientProps {
  sections: HomepageSection[];
  fallbackVehicles?: Record<string, unknown>[];
}

// =============================================
// COMPONENT
// =============================================

export default function HomepageClient({ sections, fallbackVehicles = [] }: HomepageClientProps) {
  const getSection = (slug: string) => sections.find(s => s.slug === slug);

  const carousel = getSection('carousel');
  const destacados = getSection('destacados');
  const sedanes = getSection('sedanes');
  const suvs = getSection('suvs');
  const camionetas = getSection('camionetas');
  const deportivos = getSection('deportivos');
  const lujo = getSection('lujo');

  // Dynamic brands & categories from AdvertisingService
  const { data: apiBrands } = useBrands();
  const { data: apiCategories } = useCategories();

  const dynamicBrands = useMemo(() => {
    if (!apiBrands || apiBrands.length === 0) return undefined;
    return apiBrands
      .filter((b: BrandConfig) => b.isActive)
      .sort((a: BrandConfig, b: BrandConfig) => a.displayOrder - b.displayOrder)
      .map((b: BrandConfig) => ({
        id: b.id,
        name: b.displayName,
        slug: b.brandKey.toLowerCase(),
        logoUrl: b.logoUrl || undefined,
        vehicleCount: b.vehicleCount,
      }));
  }, [apiBrands]);

  const dynamicCategories = useMemo(() => {
    if (!apiCategories || apiCategories.length === 0) return undefined;
    return apiCategories
      .filter((c: CategoryImageConfig) => c.isActive)
      .sort((a: CategoryImageConfig, b: CategoryImageConfig) => a.displayOrder - b.displayOrder)
      .map((c: CategoryImageConfig) => ({
        id: c.id,
        name: c.displayName,
        slug: c.categoryKey.toLowerCase(),
        description: c.description,
        vehicleCount: 0,
        imageUrl: c.imageUrl,
        gradient: c.accentColor || 'from-blue-600 to-blue-800',
        trending: c.isTrending,
      }));
  }, [apiCategories]);

  const heroVehicles: Vehicle[] = useMemo(() => {
    const source = carousel?.vehicles.length ? carousel : destacados;
    if (source && source.vehicles.length > 0) {
      return source.vehicles.map(transformHomepageVehicleToVehicle);
    }
    // Fallback: use featured vehicles from the vehicles API
    if (fallbackVehicles.length > 0) {
      return fallbackVehicles.slice(0, 5).map(transformFeaturedDtoToVehicle);
    }
    return [];
  }, [carousel, destacados, fallbackVehicles]);

  const gridVehicles: Vehicle[] = useMemo(() => {
    const source = destacados?.vehicles.length ? destacados : carousel;
    if (source && source.vehicles.length > 0) {
      return source.vehicles.map(transformHomepageVehicleToVehicle);
    }
    // Fallback: use featured vehicles from the vehicles API
    if (fallbackVehicles.length > 0) {
      return fallbackVehicles.map(transformFeaturedDtoToVehicle);
    }
    return [];
  }, [destacados, carousel, fallbackVehicles]);

  const categorySections = useMemo(() => {
    return [sedanes, suvs, camionetas, deportivos, destacados, lujo].filter(
      (section): section is HomepageSection => !!section && section.vehicles.length > 0
    );
  }, [sedanes, suvs, camionetas, deportivos, destacados, lujo]);

  return (
    <>
      {/* Hero Section - Compact with vehicles visible immediately */}
      <HeroCompact vehicles={heroVehicles} isLoading={false} />

      {/* Featured Vehicles Grid - IMMEDIATE after hero */}
      <SectionContainer
        title="Más Vehículos"
        subtitle="Explora nuestra selección premium de vehículos cuidadosamente verificados"
        background="gradient"
      >
        {gridVehicles.length > 0 ? (
          <FeaturedListingGrid vehicles={gridVehicles} maxItems={9} />
        ) : (
          <SkeletonGrid count={6} columns={3} />
        )}
      </SectionContainer>

      {/* Trusted Brands Slider */}
      <section className="bg-card py-8">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <h2 className="text-muted-foreground mb-4 text-center text-xs font-semibold tracking-widest uppercase">
            Las marcas más buscadas en República Dominicana
          </h2>
          <BrandSlider brands={dynamicBrands} autoScroll scrollSpeed={40} />
        </div>
      </section>

      {/* Featured Sponsored Vehicles */}
      <FeaturedVehicles title="⭐ Vehículos Destacados" placementType="FeaturedSpot" maxItems={8} />

      {/* Premium Sponsored Vehicles */}
      <FeaturedVehicles title="💎 Vehículos Premium" placementType="PremiumSpot" maxItems={4} />

      {/* Browse by Category */}
      <section className="bg-muted/50 dark:bg-muted/20 py-12 lg:py-16">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="mb-8 text-center">
            <span className="bg-primary/10 text-primary mb-3 inline-block rounded-full px-4 py-1.5 text-sm font-semibold tracking-wide">
              Explora por Categoría
            </span>
            <h2 className="text-foreground mb-3 text-2xl leading-tight font-bold tracking-tight lg:text-3xl">
              Encuentra el tipo de vehículo perfecto
            </h2>
            <p className="text-muted-foreground mx-auto max-w-xl text-base leading-relaxed">
              Desde SUVs familiares hasta deportivos de alto rendimiento.
            </p>
          </div>
          <CategoryCards categories={dynamicCategories} />
        </div>
      </section>

      {/* Dynamic Category Sections */}
      {categorySections.slice(0, 4).map(section => (
        <FeaturedSection
          key={section.slug}
          title={section.name}
          subtitle={section.subtitle}
          listings={transformSectionVehicles(section)}
          viewAllHref={section.viewAllHref}
          accentColor={section.accentColor}
        />
      ))}

      {/* Why Choose OKLA */}
      <WhyChooseUs variant="grid" />

      {/* CTA */}
      <CTASection
        title="¿Listo para vender tu vehículo?"
        subtitle="Publica tu vehículo en minutos y llega a compradores interesados."
        primaryButton={{ label: 'Publicar Gratis', href: '/vender' }}
        secondaryButton={{ label: 'Para Dealers', href: '/dealers' }}
      />
    </>
  );
}
