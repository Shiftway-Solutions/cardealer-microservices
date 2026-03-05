/**
 * Homepage Client Component
 *
 * Client-side interactive parts of the homepage.
 * Receives pre-fetched data from server component and handles interactivity.
 */

'use client';

import { useMemo } from 'react';
import { HeroCompact, WhyChooseUs, CTASection } from '@/components/homepage';
import VehicleTypeSection from '@/components/homepage/vehicle-type-section';
import FeaturedVehicles from '@/components/advertising/featured-vehicles';
import { NativeBannerAd } from '@/components/advertising/native-ads';
import { DealerPromoSection } from '@/components/homepage/dealer-promo-section';
import { useBrands } from '@/hooks/use-advertising';
import type { BrandConfig } from '@/types/advertising';
import { Bus, Car, Gauge, Leaf, Truck, Wind, Zap } from 'lucide-react';

// =============================================
// COMPONENT
// =============================================

export default function HomepageClient() {
  // Dynamic brands from AdvertisingService
  const { data: apiBrands } = useBrands();

  // Sponsored dealers for brand slider
  const dealerSponsors = useMemo(() => {
    if (!apiBrands || apiBrands.length === 0) return undefined;
    return apiBrands
      .filter(
        (b: BrandConfig) =>
          b.isActive &&
          b.logoUrl &&
          'dealerId' in b &&
          (b as unknown as Record<string, unknown>).dealerId
      )
      .sort((a: BrandConfig, b: BrandConfig) => a.displayOrder - b.displayOrder)
      .slice(0, 10)
      .map((b: BrandConfig) => ({
        id: b.id,
        name: b.displayName,
        slug: b.brandKey.toLowerCase(),
        logoUrl: b.logoUrl || undefined,
        vehicleCount: b.vehicleCount,
        isDealer: true,
        portalSlug: b.brandKey.toLowerCase().replace(/\s+/g, '-'),
      }));
  }, [apiBrands]);

  // Stable impression token for the dealer banner
  const bannerImpressionToken = 'banner-dealer-cta-homepage';

  return (
    <>
      {/* ── 1. HERO — NL search + vehicle photos ─────────────────────────── */}
      <HeroCompact />

      {/* ── SECCIONES PAGADAS CON FOTOS GRANDES (primeras) ───────────────── */}

      {/* 2. ⭐ Vehículos Destacados — espacio pagado FeaturedSpot */}
      <FeaturedVehicles title="⭐ Vehículos Destacados" placementType="FeaturedSpot" maxItems={6} />

      {/* 3. 💎 Vehículos Premium — espacio pagado PremiumSpot (sección más grande = precio más alto) */}
      <FeaturedVehicles
        title="💎 Vehículos Premium"
        placementType="PremiumSpot"
        maxItems={12}
        columns={4}
      />

      {/* 4. Dealers Patrocinados — espacios pagados por dealers */}
      <DealerPromoSection dealers={dealerSponsors} totalSlots={8} />

      {/* ── SECCIONES POR TIPO DE VEHÍCULO ──────────────────────────────── */}
      {/* Same card size as ⭐ Destacados — 4-column large-photo grid.      */}
      {/* Each section fetches live data from the vehicles API by type.     */}

      {/* 6. SUVs */}
      <VehicleTypeSection
        filterValue="SUV"
        title="SUVs"
        subtitle="Los más solicitados en República Dominicana"
        icon={<Truck className="inline-block h-6 w-6" />}
        viewAllHref="/vehiculos?bodyType=suv"
        accentColor="blue"
      />

      {/* Crossovers */}
      <VehicleTypeSection
        filterValue="Crossover"
        title="Crossovers"
        subtitle="Versatilidad urbana con actitud todo terreno"
        icon={<Car className="inline-block h-6 w-6" />}
        viewAllHref="/vehiculos?bodyType=crossover"
        accentColor="sky"
      />

      {/* 7. Sedanes */}
      <VehicleTypeSection
        filterValue="Sedan"
        title="Sedanes"
        subtitle="Comodidad y eficiencia para el día a día"
        icon={<Car className="inline-block h-6 w-6" />}
        viewAllHref="/vehiculos?bodyType=sedan"
        accentColor="emerald"
      />

      {/* Hatchbacks */}
      <VehicleTypeSection
        filterValue="Hatchback"
        title="Hatchbacks"
        subtitle="Agilidad y practicidad en cada viaje"
        icon={<Car className="inline-block h-6 w-6" />}
        viewAllHref="/vehiculos?bodyType=hatchback"
        accentColor="violet"
      />

      {/* Dealer CTA banner between type sections */}
      <div className="mx-auto max-w-7xl px-4 py-6 sm:px-6 lg:px-8 2xl:max-w-[1600px]">
        <NativeBannerAd
          title="¿Eres dealer? Llega a más compradores"
          subtitle="Destaca tu inventario con publicidad inteligente y paga solo por resultados reales."
          ctaText="Conocer más"
          ctaUrl="/dealers"
          backgroundGradient="from-emerald-600 to-teal-700"
          impressionToken={bannerImpressionToken}
        />
      </div>

      {/* 8. Camionetas / Pickups */}
      <VehicleTypeSection
        filterValue="Pickup"
        title="Camionetas"
        subtitle="Potencia y versatilidad para trabajo y aventura"
        icon={<Truck className="inline-block h-6 w-6" />}
        viewAllHref="/vehiculos?bodyType=pickup"
        accentColor="amber"
      />

      {/* 9. Coupés */}
      <VehicleTypeSection
        filterValue="Coupe"
        title="Coupés"
        subtitle="Diseño elegante y líneas aerodinámicas"
        icon={<Gauge className="inline-block h-6 w-6" />}
        viewAllHref="/vehiculos?bodyType=coupe"
        accentColor="rose"
      />

      {/* Deportivos */}
      <VehicleTypeSection
        filterValue="Sport"
        title="Deportivos"
        subtitle="Rendimiento y adrenalina en cada curva"
        icon={<Gauge className="inline-block h-6 w-6" />}
        viewAllHref="/vehiculos?bodyType=sport"
        accentColor="orange"
      />

      {/* Convertibles */}
      <VehicleTypeSection
        filterValue="Convertible"
        title="Convertibles"
        subtitle="Libertad a cielo abierto en cada trayecto"
        icon={<Wind className="inline-block h-6 w-6" />}
        viewAllHref="/vehiculos?bodyType=convertible"
        accentColor="pink"
      />

      {/* Vans */}
      <VehicleTypeSection
        filterValue="Van"
        title="Vans"
        subtitle="Capacidad y espacio para toda la familia"
        icon={<Bus className="inline-block h-6 w-6" />}
        viewAllHref="/vehiculos?bodyType=van"
        accentColor="slate"
      />

      {/* Minivans */}
      <VehicleTypeSection
        filterValue="Minivan"
        title="Minivans"
        subtitle="El equilibrio perfecto entre comodidad y espacio"
        icon={<Bus className="inline-block h-6 w-6" />}
        viewAllHref="/vehiculos?bodyType=minivan"
        accentColor="yellow"
      />

      {/* 10. Híbridos — filter by fuelType */}
      <VehicleTypeSection
        filterType="fuelType"
        filterValue="Hybrid"
        title="Híbridos"
        subtitle="Mayor eficiencia, menor impacto ambiental"
        icon={<Leaf className="inline-block h-6 w-6" />}
        viewAllHref="/vehiculos?fuelType=hibrido"
        accentColor="teal"
      />

      {/* 11. Eléctricos — filter by fuelType */}
      <VehicleTypeSection
        filterType="fuelType"
        filterValue="Electric"
        title="Eléctricos"
        subtitle="El futuro de la movilidad ya está aquí"
        icon={<Zap className="inline-block h-6 w-6" />}
        viewAllHref="/vehiculos?fuelType=electrico"
        accentColor="indigo"
      />

      {/* ── FINAL ──────────────────────────────────────────────────────────── */}

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
