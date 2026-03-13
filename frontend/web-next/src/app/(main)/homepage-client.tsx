/**
 * Homepage Client Component
 *
 * Client-side interactive parts of the homepage.
 * Receives pre-fetched data from server component and handles interactivity.
 */

'use client';

import { useMemo, useRef, useState, useEffect, type ReactNode } from 'react';
import { HeroCompact, WhyChooseUs, CTASection, TestimonialsCarousel } from '@/components/homepage';
import VehicleTypeSection from '@/components/homepage/vehicle-type-section';
import FeaturedVehicles from '@/components/advertising/featured-vehicles';
import { NativeBannerAd } from '@/components/advertising/native-ads';
import { DealerPromoSection } from '@/components/homepage/dealer-promo-section';
import { VehicleOfTheDay } from '@/components/homepage/vehicle-of-the-day';
import { useBrands } from '@/hooks/use-advertising';
import { useQuery } from '@tanstack/react-query';
import type { BrandConfig } from '@/types/advertising';
import { Bus, Car, Gauge, Leaf, Truck, Wind, Zap } from 'lucide-react';
import { HOMEPAGE_STATS } from '@/lib/platform-stats';

// =============================================
// CWV FIX: LazySection — defers mounting of below-fold sections until they
// enter the viewport (with 200px rootMargin for pre-loading). This reduces
// initial JS work and main-thread contention, improving LCP and INP.
// =============================================

function LazySection({ children, minHeight = 300 }: { children: ReactNode; minHeight?: number }) {
  const ref = useRef<HTMLDivElement>(null);
  const [isVisible, setIsVisible] = useState(false);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) {
          setIsVisible(true);
          observer.disconnect();
        }
      },
      { rootMargin: '200px' }
    );
    observer.observe(el);
    return () => observer.disconnect();
  }, []);

  return (
    <div ref={ref} style={{ minHeight: isVisible ? undefined : minHeight }}>
      {isVisible ? children : null}
    </div>
  );
}

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

  // Fetch vehicles for "Vehicle of the Day"
  const { data: vodVehicles } = useQuery({
    queryKey: ['vehicle-of-the-day'],
    queryFn: async () => {
      try {
        const res = await fetch('/api/vehicles?pageSize=50&sortBy=newest');
        if (!res.ok) return [];
        const data = await res.json();
        return data?.data?.items || data?.items || data?.data || [];
      } catch {
        return [];
      }
    },
    staleTime: 5 * 60 * 1000,
  });

  return (
    <>
      {/* ── 1. HERO — NL search + vehicle photos ─────────────────────────── */}
      <HeroCompact />

      {/* ── 📊 SOCIAL PROOF — Platform stats ────────────────────────────── */}
      <section className="border-border bg-card border-b py-10">
        <div className="container mx-auto px-4">
          <div className="grid grid-cols-2 gap-6 md:grid-cols-4">
            {HOMEPAGE_STATS.map((stat, index) => (
              <div key={index} className="text-center">
                <div className="text-primary text-3xl font-bold md:text-4xl">{stat.value}</div>
                <div className="text-muted-foreground mt-1 text-sm">{stat.label}</div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ── 🏆 VEHÍCULO DEL DÍA ─────────────────────────────────────────── */}
      {/* Only render when vehicles are available — no empty placeholder */}
      {vodVehicles && vodVehicles.length > 0 && <VehicleOfTheDay vehicles={vodVehicles} />}

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
      <LazySection minHeight={400}>
        <VehicleTypeSection
          filterValue="Hatchback"
          title="Hatchbacks"
          subtitle="Agilidad y practicidad en cada viaje"
          icon={<Car className="inline-block h-6 w-6" />}
          viewAllHref="/vehiculos?bodyType=hatchback"
          accentColor="violet"
        />
      </LazySection>

      {/* Dealer CTA banner between type sections */}
      <LazySection minHeight={120}>
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
      </LazySection>

      {/* 8. Camionetas / Pickups */}
      <LazySection minHeight={400}>
        <VehicleTypeSection
          filterValue="Pickup"
          title="Camionetas"
          subtitle="Potencia y versatilidad para trabajo y aventura"
          icon={<Truck className="inline-block h-6 w-6" />}
          viewAllHref="/vehiculos?bodyType=pickup"
          accentColor="amber"
        />
      </LazySection>

      {/* 9. Coupés */}
      <LazySection minHeight={400}>
        <VehicleTypeSection
          filterValue="Coupe"
          title="Coupés"
          subtitle="Diseño elegante y líneas aerodinámicas"
          icon={<Gauge className="inline-block h-6 w-6" />}
          viewAllHref="/vehiculos?bodyType=coupe"
          accentColor="rose"
        />
      </LazySection>

      {/* Deportivos */}
      <LazySection minHeight={400}>
        <VehicleTypeSection
          filterValue="SportsCar"
          title="Deportivos"
          subtitle="Rendimiento y adrenalina en cada curva"
          icon={<Gauge className="inline-block h-6 w-6" />}
          viewAllHref="/vehiculos?bodyType=sport"
          accentColor="orange"
        />
      </LazySection>

      {/* Convertibles */}
      <LazySection minHeight={400}>
        <VehicleTypeSection
          filterValue="Convertible"
          title="Convertibles"
          subtitle="Libertad a cielo abierto en cada trayecto"
          icon={<Wind className="inline-block h-6 w-6" />}
          viewAllHref="/vehiculos?bodyType=convertible"
          accentColor="pink"
        />
      </LazySection>

      {/* Vans */}
      <LazySection minHeight={400}>
        <VehicleTypeSection
          filterValue="Van"
          title="Vans"
          subtitle="Capacidad y espacio para toda la familia"
          icon={<Bus className="inline-block h-6 w-6" />}
          viewAllHref="/vehiculos?bodyType=van"
          accentColor="slate"
        />
      </LazySection>

      {/* Minivans */}
      <LazySection minHeight={400}>
        <VehicleTypeSection
          filterValue="Minivan"
          title="Minivans"
          subtitle="El equilibrio perfecto entre comodidad y espacio"
          icon={<Bus className="inline-block h-6 w-6" />}
          viewAllHref="/vehiculos?bodyType=minivan"
          accentColor="yellow"
        />
      </LazySection>

      {/* 10. Híbridos — filter by fuelType */}
      <LazySection minHeight={400}>
        <VehicleTypeSection
          filterType="fuelType"
          filterValue="Hybrid"
          title="Híbridos"
          subtitle="Mayor eficiencia, menor impacto ambiental"
          icon={<Leaf className="inline-block h-6 w-6" />}
          viewAllHref="/vehiculos?fuelType=hibrido"
          accentColor="teal"
        />
      </LazySection>

      {/* 11. Eléctricos — filter by fuelType */}
      <LazySection minHeight={400}>
        <VehicleTypeSection
          filterType="fuelType"
          filterValue="Electric"
          title="Eléctricos"
          subtitle="El futuro de la movilidad ya está aquí"
          icon={<Zap className="inline-block h-6 w-6" />}
          viewAllHref="/vehiculos?fuelType=electrico"
          accentColor="indigo"
        />
      </LazySection>

      {/* ── FINAL ──────────────────────────────────────────────────────────── */}

      {/* Ley 358-05 / Pro-Consumidor — Aviso de publicidad general del homepage */}
      <div className="mx-auto max-w-7xl px-4 py-4 sm:px-6 lg:px-8 2xl:max-w-[1600px]">
        <p className="text-muted-foreground border-t pt-4 text-center text-[11px] leading-relaxed">
          Todos los espacios de vehículos en esta página constituyen contenido publicitario pagado
          por sus respectivos anunciantes. Los precios son de referencia y no incluyen ITBIS (18%),
          gastos de traspaso ni otros cargos aplicables. La información publicada es responsabilidad
          de cada anunciante. Conforme a la Ley 358-05 de Protección al Consumidor y las
          disposiciones de Pro-Consumidor e INDOTEL sobre publicidad digital en República
          Dominicana.
        </p>
      </div>

      {/* ── ⭐ TESTIMONIOS — Social proof from real users ─────────────────── */}
      <LazySection minHeight={300}>
        <TestimonialsCarousel autoPlay autoPlayInterval={6000} />
      </LazySection>

      {/* Why Choose OKLA */}
      <LazySection minHeight={300}>
        <WhyChooseUs variant="grid" />
      </LazySection>

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
