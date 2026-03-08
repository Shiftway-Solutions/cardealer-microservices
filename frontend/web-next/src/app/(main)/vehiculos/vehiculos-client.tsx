'use client';

/**
 * VehiculosClient — Full Marketplace Vehicle Listing Page
 *
 * Architecture based on CarGurus / AutoTrader / Cars.com research:
 *
 * Layout (Desktop):
 *   ┌─────────────────────────────────────────────────┐
 *   │  STICKY SEARCH BAR + BODY TYPE PILLS            │
 *   ├──────────────┬──────────────────────────────────┤
 *   │  FILTERS     │  TOOLBAR (count + sort + save)   │
 *   │  SIDEBAR     │  ┌─────┬─────┬──────┬──────────┐ │
 *   │  (280px)     │  │CARD │CARD │CARD  │ AD SLOT  │ │
 *   │              │  ├─────┼─────┼──────┤ (native) │ │
 *   │              │  │CARD │CARD │CARD  ├──────────┤ │
 *   │  AD SLOT     │  │     │     │      │          │ │
 *   │  (sidebar)   │  └─────┴─────┴──────┘          │ │
 *   │              │  PAGINATION                      │
 *   └──────────────┴──────────────────────────────────┘
 *
 * Ad revenue slots:
 * - Leaderboard ad between row 1 and row 2 (after card 3)
 * - Rectangle ad in right column every ~9 results
 * - Sidebar rectangle below filters
 *
 * Key UX improvements over old version:
 * - Left sidebar filters (23% higher conversion than top-bar per NNG)
 * - Visual body type selector (31% higher engagement per Cars.com)
 * - "Save Search" CTA → /cuenta/busquedas
 * - Sticky header so search is always accessible
 * - Active filter chips with individual X buttons
 * - Monthly payment estimate on cards (AutoTrader pattern)
 * - Deal rating badges (CarGurus pattern)
 */

import * as React from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { useSearchParams } from 'next/navigation';
import { useQuery } from '@tanstack/react-query';
import {
  Search,
  SlidersHorizontal,
  Grid3X3,
  List,
  X,
  AlertCircle,
  RefreshCcw,
  BookmarkPlus,
  TrendingUp,
  Sparkles,
  Clock,
  MapPin,
  Bell,
  LogIn,
  ShieldCheck,
  Tag,
  Phone,
  Star,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { VehicleCard, VehicleCardSkeleton } from '@/components/ui/vehicle-card';
import { Badge } from '@/components/ui/badge';
import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/components/ui/sheet';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { cn } from '@/lib/utils';
import { csrfFetch } from '@/lib/security/csrf';
import { VehicleFilters } from '@/components/search/vehicle-filters';
import { SaveSearchModal } from '@/components/search/save-search-modal';
import { toast } from 'sonner';
import { aiSearch, aiFiltersToSearchParams } from '@/services/search-agent';
import { useVehicleSearch } from '@/hooks/use-vehicle-search';
import { useFavorites } from '@/hooks/use-favorites';
import { useMakes, useModelsByMake } from '@/hooks/use-vehicles';
import { useAuth } from '@/hooks/use-auth';
import { useSponsoredSearch } from '@/hooks/use-ads';
import {
  SponsoredVehicleCard,
  SidebarAdUnit,
  SponsoredBadge,
} from '@/components/advertising/native-ads';
import type { VehicleCardData } from '@/types';
import type { SponsoredVehicle } from '@/types/ads';

// =============================================================================
// CONSTANTS
// =============================================================================

const SORT_OPTIONS = [
  { value: 'relevance', label: 'Más relevantes' },
  { value: 'newest', label: 'Publicados recientemente' },
  { value: 'price_asc', label: 'Precio: Menor a mayor' },
  { value: 'price_desc', label: 'Precio: Mayor a menor' },
  { value: 'year_desc', label: 'Año: Más reciente' },
  { value: 'year_asc', label: 'Año: Más antiguo' },
  { value: 'mileage_asc', label: 'Menor kilometraje' },
];

const QUICK_FILTERS = [
  { id: 'deals', label: 'Ofertas', icon: Sparkles, filter: { dealRating: 'great' as const } },
  { id: 'new', label: 'Nuevos', icon: TrendingUp, filter: { condition: 'nuevo' as const } },
  { id: 'recent', label: 'Recientes', icon: Clock, filter: { sortBy: 'newest' as const } },
  { id: 'sd', label: 'Sto. Domingo', icon: MapPin, filter: { province: 'Santo Domingo' } },
  { id: 'santiago', label: 'Santiago', icon: MapPin, filter: { province: 'Santiago' } },
  { id: 'clean_title', label: 'Título limpio', icon: ShieldCheck, filter: { hasCleanTitle: true } },
];

// =============================================================================
// BANNER TYPES + HOOK
// =============================================================================

interface ConfigurableBanner {
  id: string;
  title: string;
  subtitle?: string | null;
  image: string;
  link: string;
  ctaText?: string | null;
  placement: string;
  status: string;
}

function useSearchLeaderboardBanners() {
  return useQuery<ConfigurableBanner[]>({
    queryKey: ['banners', 'search_leaderboard'],
    queryFn: async () => {
      const res = await fetch('/api/banners?placement=search_leaderboard');
      if (!res.ok) return [];
      const data = await res.json();
      return Array.isArray(data) ? data : [];
    },
    staleTime: 60_000, // 1 minute
    refetchOnWindowFocus: false,
  });
}

// =============================================================================
// AD SLOT COMPONENTS
// =============================================================================

/**
 * ConfigurableBannerCard — renders a single admin-managed banner with legal disclosure.
 * Falls back to OKLA self-promo when no configurable banners are available.
 */
function ConfigurableBannerCard({ banner }: { banner: ConfigurableBanner }) {
  const handleClick = React.useCallback(() => {
    // Fire-and-forget analytics
    csrfFetch(`/api/banners/${banner.id}/click`, { method: 'POST' }).catch(() => {});
  }, [banner.id]);

  // Track view on mount
  React.useEffect(() => {
    csrfFetch(`/api/banners/${banner.id}/view`, { method: 'POST' }).catch(() => {});
  }, [banner.id]);

  return (
    <Link
      href={banner.link ?? '/'}
      onClick={handleClick}
      className="col-span-full my-3 block overflow-hidden rounded-2xl bg-gradient-to-r from-slate-800 to-slate-700 shadow-md transition-all hover:shadow-lg hover:brightness-105"
      aria-label={`Publicidad: ${banner.title}`}
      target={banner.link?.startsWith('http') ? '_blank' : undefined}
      rel={banner.link?.startsWith('http') ? 'noopener noreferrer sponsored' : undefined}
    >
      {/* Legal disclosure — Ley 358-05 */}
      <div className="flex justify-end px-4 pt-2">
        <span className="text-[10px] font-medium text-white/60">Publicidad</span>
      </div>
      <div className="flex flex-col items-center justify-between gap-4 px-6 pb-5 sm:flex-row">
        <div className="flex items-center gap-4">
          {banner.image ? (
            <Image
              src={banner.image}
              alt=""
              aria-hidden="true"
              width={48}
              height={48}
              className="h-12 w-12 shrink-0 rounded-xl object-cover"
              unoptimized
            />
          ) : (
            <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-white/10">
              <Tag className="h-6 w-6 text-white/70" />
            </div>
          )}
          <div>
            <p className="text-lg font-bold text-white">{banner.title}</p>
            {banner.subtitle && <p className="text-sm text-white/80">{banner.subtitle}</p>}
          </div>
        </div>
        {banner.ctaText && (
          <span className="flex shrink-0 items-center gap-2 rounded-xl bg-white px-5 py-2.5 text-sm font-bold text-slate-800 shadow-sm">
            {banner.ctaText}
          </span>
        )}
      </div>
    </Link>
  );
}

/**
 * AdSlotLeaderboard — fetches configurable banners from admin portal.
 * Returns null when no active banners (removed seller self-promo: this page is for buyers).
 */
function AdSlotLeaderboard() {
  const { data: banners, isLoading } = useSearchLeaderboardBanners();

  // While loading, render placeholder to avoid layout shift
  if (isLoading) {
    return (
      <div
        className="bg-muted col-span-full my-3 h-[88px] animate-pulse rounded-2xl"
        aria-hidden="true"
      />
    );
  }

  const activeBanner = banners?.[0]; // Show first active banner (rotate in future)

  // No fallback — this page is for buyers, not sellers.
  // Reserve min-height to prevent CLS when banner loads/disappears.
  return activeBanner ? (
    <div className="col-span-full min-h-[88px]">
      <ConfigurableBannerCard banner={activeBanner} />
    </div>
  ) : null;
}

function AdSlotRectangle({ className }: { className?: string }) {
  return (
    <div
      className={cn(
        'overflow-hidden rounded-2xl bg-gradient-to-br from-slate-900 to-slate-800 shadow-md',
        className
      )}
      aria-label="Panel de alertas"
    >
      <div className="flex flex-col items-center gap-3 p-5 text-center">
        <div className="bg-primary/20 flex h-12 w-12 items-center justify-center rounded-xl">
          <Bell className="text-primary h-6 w-6" />
        </div>
        <div>
          <p className="font-bold text-white">Crea una alerta</p>
          <p className="mt-1 text-xs text-slate-400">
            Recibe notificaciones cuando aparezca un vehículo que te interese
          </p>
        </div>
        <Link
          href="/cuenta/alertas"
          className="bg-primary hover:bg-primary/80 mt-1 flex w-full items-center justify-center gap-2 rounded-xl py-2.5 text-sm font-semibold text-white transition-all"
        >
          <Bell className="h-4 w-4" />
          Activar alertas
        </Link>
        <div className="flex items-center gap-1.5 text-xs text-slate-500">
          <ShieldCheck className="h-3.5 w-3.5" />
          Sin spam · Cancela cuando quieras
        </div>
      </div>
    </div>
  );
}

// =============================================================================
// SKELETON
// =============================================================================

function VehiclesGridSkeleton({ viewMode }: { viewMode: 'grid' | 'list' }) {
  return (
    <div
      className={
        viewMode === 'grid'
          ? 'grid gap-3 sm:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-4'
          : 'flex flex-col gap-3'
      }
    >
      {Array.from({ length: 9 }).map((_, i) => (
        <VehicleCardSkeleton key={i} variant={viewMode === 'list' ? 'horizontal' : 'default'} />
      ))}
    </div>
  );
}

// =============================================================================
// SPONSORED ROW — Green-bordered banner inserted every 2 grid rows (6 organic items)
// =============================================================================

function SponsoredRowGrid({ vehicles }: { vehicles: SponsoredVehicle[] }) {
  if (!vehicles.length) return null;
  const show = vehicles.slice(0, 3);
  return (
    <div className="col-span-full my-3">
      {/* Green banner container — visually separates sponsored from organic */}
      <div className="border-primary bg-primary/5 dark:bg-primary/10 rounded-xl border-2 px-4 py-4">
        <div className="mb-3 flex items-center gap-2">
          <span className="text-primary text-[11px] font-semibold tracking-wider uppercase">
            Vehículos Patrocinados
          </span>
          <SponsoredBadge tier="sponsored" />
        </div>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {show.map(v => (
            <SponsoredVehicleCard key={v.id} vehicle={v} />
          ))}
        </div>
      </div>
    </div>
  );
}

// =============================================================================
// ACTIVE FILTER CHIP
// =============================================================================

function FilterChip({ label, onRemove }: { label: string; onRemove: () => void }) {
  return (
    <Badge
      variant="secondary"
      className="flex max-w-[160px] items-center gap-1 pr-1 pl-2.5 text-xs"
    >
      <span className="truncate">{label}</span>
      <button
        type="button"
        onClick={onRemove}
        className="hover:bg-muted ml-0.5 flex min-h-[28px] min-w-[28px] flex-shrink-0 items-center justify-center rounded-full p-1.5 transition-colors"
        aria-label={`Eliminar filtro ${label}`}
      >
        <X className="h-3.5 w-3.5" />
      </button>
    </Badge>
  );
}

// =============================================================================
// MAIN CLIENT COMPONENT
// =============================================================================

export default function VehiculosClient() {
  const { isAuthenticated } = useAuth();
  const urlSearchParams = useSearchParams();
  const [viewMode, setViewMode] = React.useState<'grid' | 'list'>('grid');
  const [mobileFiltersOpen, setMobileFiltersOpen] = React.useState(false);
  const [saveModalOpen, setSaveModalOpen] = React.useState(false);
  // Initialise search bar text from ai_query param (homepage NL redirect) or empty
  const [searchInput, setSearchInput] = React.useState(() => urlSearchParams.get('ai_query') ?? '');
  const [isAiSearching, setIsAiSearching] = React.useState(false);
  const [aiSearchInfo, setAiSearchInfo] = React.useState<{
    query: string;
    confidence: number;
    latencyMs: number;
    wasCached: boolean;
  } | null>(() => {
    const aiQuery = urlSearchParams.get('ai_query');
    if (!aiQuery) return null;
    // Restore AI search info banner when landing from homepage NL search
    return { query: aiQuery, confidence: 1.0, latencyMs: 0, wasCached: false };
  });

  // Core search hook — handles URL sync, debounce, React Query
  const {
    filters,
    setFilters,
    setFilter,
    clearFilters,
    clearFilter,
    results,
    isLoading,
    isFetching,
    error,
    refetch,
    activeFilterCount,
  } = useVehicleSearch({ syncUrl: true });

  // Catalog data
  const { data: makes = [] } = useMakes();
  const { data: models = [] } = useModelsByMake(filters.make ?? '');

  // Favorites
  const { isFavorite, toggleFavorite } = useFavorites();

  // Sponsored search results (native ads)
  const { topSponsored, inlineSponsored } = useSponsoredSearch(filters.query);

  // Derived
  const vehicles = React.useMemo(() => results?.vehicles ?? [], [results]);
  const totalResults = results?.total ?? 0;
  const currentPage = filters.page ?? 1;
  const totalPages = results?.totalPages ?? 1;

  // ── Infinite scroll ──────────────────────────────────────────────────────
  const [allVehicles, setAllVehicles] = React.useState<VehicleCardData[]>([]);
  const sentinelRef = React.useRef<HTMLDivElement>(null);
  const prevFiltersKeyRef = React.useRef<string | null>(null);

  // Key that ignores page number — changes only when real search params change
  const filtersKey = React.useMemo(() => {
    const { page: _, ...rest } = filters;
    return JSON.stringify(rest);
  }, [filters]);

  // When real search params change: reset accumulated list and scroll to top
  React.useEffect(() => {
    if (prevFiltersKeyRef.current === null) {
      prevFiltersKeyRef.current = filtersKey;
      return;
    }
    if (prevFiltersKeyRef.current !== filtersKey) {
      prevFiltersKeyRef.current = filtersKey;
      setAllVehicles([]);
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }, [filtersKey]);

  // Accumulate pages as they load
  React.useEffect(() => {
    if (!isLoading && vehicles.length > 0) {
      if ((filters.page ?? 1) === 1) {
        setAllVehicles(vehicles);
      } else {
        setAllVehicles(prev => {
          const ids = new Set(prev.map((v: VehicleCardData) => v.id));
          return [...prev, ...vehicles.filter((v: VehicleCardData) => !ids.has(v.id))];
        });
      }
    }
  }, [vehicles, isLoading, filters.page]);

  // Intersection observer: trigger next page when sentinel enters viewport
  React.useEffect(() => {
    const sentinel = sentinelRef.current;
    if (!sentinel) return;
    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting && !isLoading && !isFetching && currentPage < totalPages) {
          setFilter('page', currentPage + 1);
        }
      },
      { rootMargin: '600px' }
    );
    observer.observe(sentinel);
    return () => observer.disconnect();
  }, [isLoading, isFetching, currentPage, totalPages, setFilter]);
  // ─────────────────────────────────────────────────────────────────────────

  // Build active filter chips for display
  const activeChips = React.useMemo(() => {
    const chips: { key: string; label: string }[] = [];
    if (filters.query) chips.push({ key: 'query', label: `"${filters.query}"` });
    if (filters.condition)
      chips.push({
        key: 'condition',
        label: filters.condition === 'nuevo' ? 'Nuevo' : 'Usado',
      });
    if (filters.make) chips.push({ key: 'make', label: filters.make });
    if (filters.model) chips.push({ key: 'model', label: filters.model });
    if (filters.yearMin && filters.yearMax)
      chips.push({ key: 'year', label: `${filters.yearMin}–${filters.yearMax}` });
    else if (filters.yearMin) chips.push({ key: 'year', label: `Desde ${filters.yearMin}` });
    else if (filters.yearMax) chips.push({ key: 'year', label: `Hasta ${filters.yearMax}` });
    if (filters.priceMin || filters.priceMax) {
      const fmtPrice = (v: number) =>
        v >= 1_000_000
          ? `RD$${(v / 1_000_000).toFixed(v % 1_000_000 === 0 ? 0 : 1)}M`
          : `RD$${(v / 1_000).toFixed(0)}K`;
      const min = filters.priceMin ? fmtPrice(filters.priceMin) : '';
      const max = filters.priceMax ? fmtPrice(filters.priceMax) : '';
      chips.push({ key: 'price', label: min && max ? `${min}–${max}` : min || `Hasta ${max}` });
    }
    if (filters.bodyType) chips.push({ key: 'bodyType', label: filters.bodyType });
    if (filters.province) chips.push({ key: 'province', label: filters.province });
    if (filters.mileageMax)
      chips.push({ key: 'mileageMax', label: `< ${filters.mileageMax.toLocaleString()} km` });
    if (filters.fuelType) chips.push({ key: 'fuelType', label: filters.fuelType });
    if (filters.transmission) chips.push({ key: 'transmission', label: filters.transmission });
    if (filters.drivetrain)
      chips.push({ key: 'drivetrain', label: filters.drivetrain.toUpperCase() });
    if (filters.color) chips.push({ key: 'color', label: filters.color });
    if (filters.sellerType)
      chips.push({
        key: 'sellerType',
        label: filters.sellerType === 'dealer' ? 'Dealers' : 'Particulares',
      });
    if (filters.isCertified) chips.push({ key: 'isCertified', label: 'Con garantía' });
    if (filters.hasCleanTitle) chips.push({ key: 'hasCleanTitle', label: 'Título limpio' });
    if (filters.dealRating)
      chips.push({
        key: 'dealRating',
        label:
          filters.dealRating === 'great'
            ? 'Excelente precio'
            : filters.dealRating === 'good'
              ? 'Buen precio'
              : 'Precio justo',
      });
    // Extended DR-market filters
    if (filters.seats) chips.push({ key: 'seats', label: `${filters.seats}+ pasajeros` });
    if (filters.cylinders) chips.push({ key: 'cylinders', label: `${filters.cylinders} cil.` });
    if (filters.interiorColor)
      chips.push({ key: 'interiorColor', label: `Interior: ${filters.interiorColor}` });
    if (filters.features?.length)
      filters.features.forEach(f =>
        chips.push({ key: `feature-${f}`, label: f.replace(/_/g, ' ') })
      );
    return chips;
  }, [filters]);

  const removeChip = (key: string) => {
    if (key === 'year') {
      setFilters({ yearMin: undefined, yearMax: undefined });
    } else if (key === 'price') {
      setFilters({ priceMin: undefined, priceMax: undefined });
    } else if (key.startsWith('feature-')) {
      const featureToRemove = key.replace('feature-', '');
      const updated = (filters.features ?? []).filter(f => f !== featureToRemove);
      setFilters({ features: updated.length ? updated : undefined });
    } else {
      clearFilter(key as keyof typeof filters);
    }
  };

  const handleFavoriteToggle = (vehicleId: string) => {
    toggleFavorite(vehicleId).catch(() => {});
  };

  // AI-powered natural language search handler
  const handleAiSearch = React.useCallback(
    async (query: string) => {
      if (!query.trim() || isAiSearching) return;

      setIsAiSearching(true);
      setAiSearchInfo(null);

      try {
        const result = await aiSearch({ query: query.trim() });
        const ai = result.aiFilters;

        if (!result.isAiSearchEnabled) {
          // AI disabled — clear filters and show all vehicles (no raw NL chip)
          clearFilters();
          setSearchInput(query.trim());
          toast.info('La búsqueda inteligente no está disponible en este momento.');
          return;
        }

        if (ai.confianza === 0) {
          // Claude temporarily overloaded (529) → mensaje_usuario is set → don't pollute filters
          // Genuine low-confidence → also don't use raw NL text as a filter chip
          if (ai.mensaje_usuario) {
            toast.warning(ai.mensaje_usuario);
          }
          clearFilters();
          setSearchInput(query.trim());
          return;
        }

        // Apply AI-generated filters.
        // Explicitly set query:undefined to clear any previous text filter so the
        // raw NL text does NOT appear as a filter chip alongside structured filters.
        if (ai.filtros_exactos) {
          const mapped = aiFiltersToSearchParams(ai.filtros_exactos);
          setFilters({ query: undefined, ...mapped, page: 1 } as Parameters<typeof setFilters>[0]);
          // Show the reformulated query in the search bar for context
          if (ai.query_reformulada) {
            setSearchInput(ai.query_reformulada);
          }
        }

        setAiSearchInfo({
          query: ai.query_reformulada ?? query.trim(),
          confidence: ai.confianza,
          latencyMs: result.latencyMs,
          wasCached: result.wasCached,
        });
      } catch {
        // On AI error, don't pollute filters with raw NL text — show a toast instead
        clearFilters();
        setSearchInput(query.trim());
        toast.error('Error al procesar la búsqueda inteligente. Intenta de nuevo.');
      } finally {
        setIsAiSearching(false);
      }
    },
    [isAiSearching, setFilters, clearFilters]
  );

  // Auto-trigger AI search when landing from homepage NLP search.
  // The homepage now navigates instantly with just ?ai_query=<raw> (no pre-parsed
  // filters), so the vehicles page is responsible for running the AI in the background.
  const hasAutoTriggeredAiRef = React.useRef(false);
  React.useEffect(() => {
    const aiQuery = urlSearchParams.get('ai_query');
    if (!aiQuery || hasAutoTriggeredAiRef.current) return;

    // Skip if the URL already contains structured filters (old-style redirect)
    const hasMake = urlSearchParams.get('make');
    const hasModel = urlSearchParams.get('model');
    if (hasMake || hasModel) return;

    hasAutoTriggeredAiRef.current = true;
    // Run AI parsing in the background — user already sees the vehicles page
    handleAiSearch(aiQuery);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []); // Run once on mount only

  // Sync searchInput with filters.query from URL.
  // Skip when AI search is active or AI info is shown (searchInput holds the reformulated query).
  React.useEffect(() => {
    if (!isAiSearching && !aiSearchInfo) {
      setSearchInput(filters.query ?? '');
    }
  }, [filters.query, isAiSearching, aiSearchInfo]);

  // Render accumulated vehicles with ad slots every 6 items
  const renderResults = () => {
    if (!allVehicles.length) return null;

    if (viewMode === 'list') {
      return (
        <>
          {allVehicles.map((vehicle: VehicleCardData, i: number) => (
            <React.Fragment key={vehicle.id}>
              <VehicleCard
                vehicle={vehicle}
                variant="horizontal"
                isFavorite={isFavorite(vehicle.id)}
                onFavoriteClick={handleFavoriteToggle}
                priority={i < 3}
              />
              {/* Every 6 list items: configurable banner (no seller promo fallback) */}
              {(i + 1) % 6 === 0 && <AdSlotLeaderboard />}
            </React.Fragment>
          ))}
        </>
      );
    }

    // Grid: organic results first — sponsored row appears AFTER 2 rows (6 organic items),
    // then continues cycling every 6 items thereafter.
    const items: React.ReactNode[] = [];

    // Combine topSponsored + inlineSponsored into a single rotating pool
    // so the first sponsored row appears after the first 6 organic vehicles (2 rows)
    const sponsoredPool = [...topSponsored, ...inlineSponsored];
    let sponsoredOffset = 0;

    allVehicles.forEach((vehicle: VehicleCardData, i: number) => {
      items.push(
        <VehicleCard
          key={vehicle.id}
          vehicle={vehicle}
          isFavorite={isFavorite(vehicle.id)}
          onFavoriteClick={handleFavoriteToggle}
          priority={i < 3}
        />
      );
      // Every 6 organic items (≈ 2 rows in 3-col grid): insert sponsored row
      if ((i + 1) % 6 === 0) {
        // Cycle through sponsored pool so rows don't repeat if pool < total rows
        if (sponsoredOffset >= sponsoredPool.length && sponsoredPool.length > 0) {
          sponsoredOffset = 0; // reset to start of pool
        }
        const finalRow = sponsoredPool.slice(sponsoredOffset, sponsoredOffset + 3);
        if (finalRow.length > 0) {
          items.push(<SponsoredRowGrid key={`sp-row-${i}`} vehicles={finalRow} />);
          sponsoredOffset += 3;
        }
        // Also show configurable admin banner if available
        items.push(<AdSlotLeaderboard key={`ad-${i}`} />);
      }
    });
    return items;
  };

  return (
    <div className="min-h-screen bg-slate-50 dark:bg-slate-950">
      {/* ═══════════════════════════════════════════════════════
          STICKY HEADER: Search bar + Body type quick filters
      ═══════════════════════════════════════════════════════ */}
      <div className="border-border sticky top-16 z-30 border-b bg-white shadow-sm lg:top-[72px] dark:bg-slate-900">
        <div className="mx-auto max-w-screen-xl px-4 py-3 sm:px-6">
          {/* Row 1: Search bar + controls */}
          <div className="flex items-center gap-3">
            {/* Search — AI-powered natural language search */}
            <form
              className="relative min-w-0 flex-1"
              onSubmit={e => {
                e.preventDefault();
                if (searchInput.trim()) {
                  handleAiSearch(searchInput);
                }
              }}
            >
              {isAiSearching ? (
                <Sparkles className="text-primary pointer-events-none absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2 animate-pulse" />
              ) : (
                <Search className="text-muted-foreground pointer-events-none absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
              )}
              <Input
                type="search"
                placeholder="Busca por marca, modelo, año, color..."
                value={searchInput}
                onChange={e => {
                  setSearchInput(e.target.value);
                  // Clear AI info when user edits
                  if (aiSearchInfo) setAiSearchInfo(null);
                }}
                className={cn('h-10 pr-10 pl-9 text-sm', isAiSearching && 'ring-primary/40 ring-2')}
                aria-label="Buscar vehículos con IA"
                disabled={isAiSearching}
              />
              {searchInput && (
                <button
                  type="button"
                  onClick={() => {
                    setSearchInput('');
                    setAiSearchInfo(null);
                    clearFilters();
                  }}
                  className="text-muted-foreground hover:text-foreground absolute top-1/2 right-3 -translate-y-1/2"
                >
                  <X className="h-3.5 w-3.5" />
                </button>
              )}
            </form>

            {/* Sort */}
            <Select
              value={filters.sortBy ?? 'relevance'}
              onValueChange={v => setFilters({ sortBy: v as typeof filters.sortBy, page: 1 })}
            >
              <SelectTrigger className="hidden h-10 w-[190px] sm:flex">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {SORT_OPTIONS.map(opt => (
                  <SelectItem key={opt.value} value={opt.value}>
                    {opt.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>

            {/* View mode */}
            <div className="border-border bg-muted/50 hidden items-center gap-0.5 rounded-lg border p-0.5 md:flex">
              <button
                type="button"
                onClick={() => setViewMode('grid')}
                className={cn(
                  'rounded-md p-1.5 transition-colors',
                  viewMode === 'grid'
                    ? 'bg-card text-primary shadow-sm'
                    : 'text-muted-foreground hover:text-foreground'
                )}
                aria-label="Vista cuadrícula"
                aria-pressed={viewMode === 'grid'}
              >
                <Grid3X3 className="h-4 w-4" />
              </button>
              <button
                type="button"
                onClick={() => setViewMode('list')}
                className={cn(
                  'rounded-md p-1.5 transition-colors',
                  viewMode === 'list'
                    ? 'bg-card text-primary shadow-sm'
                    : 'text-muted-foreground hover:text-foreground'
                )}
                aria-label="Vista lista"
                aria-pressed={viewMode === 'list'}
              >
                <List className="h-4 w-4" />
              </button>
            </div>

            {/* Mobile filter button */}
            <Button
              variant="outline"
              size="sm"
              onClick={() => setMobileFiltersOpen(true)}
              className="relative h-10 gap-2 lg:hidden"
            >
              <SlidersHorizontal className="h-4 w-4" />
              <span className="hidden sm:inline">Filtros</span>
              {activeFilterCount > 0 && (
                <span className="bg-primary absolute -top-1.5 -right-1.5 flex h-4 w-4 items-center justify-center rounded-full text-[10px] font-bold text-white">
                  {activeFilterCount}
                </span>
              )}
            </Button>
          </div>

          {/* Row 2: Trust micro-bar (social proof — compact, always visible) */}
          <div className="scrollbar-none mt-1.5 flex items-center gap-4 overflow-x-auto pb-0.5">
            <div className="text-muted-foreground flex shrink-0 items-center gap-1 text-[11px]">
              <ShieldCheck className="text-primary h-3 w-3" />
              <span>Vendedores verificados</span>
            </div>
            <span className="text-border shrink-0">·</span>
            <div className="text-muted-foreground flex shrink-0 items-center gap-1 text-[11px]">
              <Star className="h-3 w-3 text-amber-500" />
              <span>+2,400 vehículos activos</span>
            </div>
            <span className="text-border shrink-0">·</span>
            <div className="text-muted-foreground flex shrink-0 items-center gap-1 text-[11px]">
              <Phone className="text-primary h-3 w-3" />
              <span>Contacto directo</span>
            </div>
            <span className="text-border hidden shrink-0 sm:inline">·</span>
            <div className="text-muted-foreground hidden shrink-0 items-center gap-1 text-[11px] sm:flex">
              <Bell className="text-primary h-3 w-3" />
              <span>Alertas gratis</span>
            </div>
          </div>

          {/* AI search info banner */}
          {aiSearchInfo && (
            <div className="bg-primary/5 dark:bg-primary/10 mt-1.5 flex items-center gap-2 rounded-lg px-3 py-1.5">
              <Sparkles className="text-primary h-3.5 w-3.5 shrink-0" />
              <span className="truncate text-xs text-[#005236] dark:text-[#4fd4a4]">
                IA interpretó: <strong>&ldquo;{aiSearchInfo.query}&rdquo;</strong>
              </span>
              <div className="ml-auto flex shrink-0 items-center gap-2">
                <span className="bg-primary/10 text-primary dark:bg-primary/20 rounded-full px-2 py-0.5 text-[10px] font-medium dark:text-[#4fd4a4]">
                  {Math.round(aiSearchInfo.confidence * 100)}% confianza
                </span>
                <span className="text-primary/60 text-[10px]">{aiSearchInfo.latencyMs}ms</span>
                {aiSearchInfo.wasCached && (
                  <span className="bg-primary/10 text-primary rounded-full px-1.5 py-0.5 text-[10px]">
                    caché
                  </span>
                )}
                <button
                  type="button"
                  onClick={() => {
                    setAiSearchInfo(null);
                    clearFilters();
                    setSearchInput('');
                  }}
                  className="text-primary/50 hover:text-primary ml-1"
                >
                  <X className="h-3 w-3" />
                </button>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* ═══════════════════════════════════════════════════════
          MAIN CONTENT: Sidebar + Results
      ═══════════════════════════════════════════════════════ */}
      <div className="mx-auto max-w-screen-xl px-4 py-4 sm:px-6">
        <div className="flex gap-5">
          {/* ─── LEFT SIDEBAR ─────────────────────────────────── */}
          <aside className="hidden w-[272px] flex-shrink-0 lg:block">
            <div className="border-border sticky top-[232px] rounded-2xl border bg-white shadow-sm dark:bg-slate-900">
              {/* Sidebar header */}
              <div className="border-border flex items-center justify-between border-b px-4 py-3">
                <div className="flex items-center gap-2">
                  <SlidersHorizontal className="text-primary h-4 w-4" />
                  <span className="text-sm font-bold">Filtros</span>
                  {activeFilterCount > 0 && (
                    <span className="bg-primary flex h-5 min-w-[20px] items-center justify-center rounded-full px-1 text-[10px] font-bold text-white">
                      {activeFilterCount}
                    </span>
                  )}
                </div>
                {activeFilterCount > 0 && (
                  <button
                    type="button"
                    onClick={clearFilters}
                    className="text-muted-foreground flex items-center gap-1 text-xs transition-colors hover:text-red-500"
                  >
                    <X className="h-3 w-3" />
                    Limpiar
                  </button>
                )}
              </div>
              <div className="space-y-4 p-4">
                <VehicleFilters
                  filters={filters}
                  onChange={changes => setFilters({ ...changes, page: 1 })}
                  onClear={clearFilters}
                  activeCount={activeFilterCount}
                  makeCatalog={makes.map(m => ({ id: m.id, name: m.name }))}
                  modelCatalog={models.map(m => ({ id: m.id, name: m.name, make: filters.make }))}
                />

                {/* Sidebar ad slot */}
                <AdSlotRectangle className="mt-4" />

                {/* Sidebar sponsored vehicles — show ALL with Patrocinado label */}
                {inlineSponsored.length > 0 && (
                  <SidebarAdUnit vehicles={inlineSponsored} className="mt-4" />
                )}
              </div>
            </div>
          </aside>

          {/* ─── RESULTS AREA ────────────────────────────────── */}
          <main className="min-w-0 flex-1">
            {/* Quick filter pills — aligned with results column */}
            <div className="scrollbar-none mb-3 flex items-center gap-2 overflow-x-auto">
              {QUICK_FILTERS.map(qf => {
                const isActive = Object.entries(qf.filter).every(
                  ([k, v]) => (filters as Record<string, unknown>)[k] === v
                );
                return (
                  <button
                    key={qf.id}
                    type="button"
                    onClick={() =>
                      isActive
                        ? clearFilters()
                        : setFilters({ ...qf.filter, page: 1 } as Parameters<typeof setFilters>[0])
                    }
                    className={cn(
                      'flex flex-shrink-0 items-center gap-1.5 rounded-full border px-3 py-1.5 text-xs font-semibold transition-all',
                      isActive
                        ? 'border-primary bg-primary text-white shadow-sm'
                        : 'border-border text-muted-foreground hover:text-foreground hover:border-primary/60 bg-white dark:bg-slate-900'
                    )}
                  >
                    <qf.icon className="h-3 w-3" />
                    {qf.label}
                  </button>
                );
              })}
            </div>

            {/* Toolbar: count + active filters + save search */}
            <div className="border-border mb-3 space-y-2 border-b pb-3">
              <div className="flex flex-wrap items-center justify-between gap-3">
                {/* Results count */}
                <div className="text-muted-foreground text-sm">
                  {isLoading ? (
                    <span className="bg-muted inline-block h-4 w-32 animate-pulse rounded" />
                  ) : (
                    <>
                      <span className="text-foreground font-bold">
                        {totalResults.toLocaleString()}
                      </span>{' '}
                      vehículos encontrados
                      {isFetching && !isLoading && (
                        <span className="text-muted-foreground ml-2 animate-pulse text-xs">
                          actualizando…
                        </span>
                      )}
                    </>
                  )}
                </div>

                <div className="flex items-center gap-2">
                  {/* Mobile sort */}
                  <Select
                    value={filters.sortBy ?? 'relevance'}
                    onValueChange={v => setFilters({ sortBy: v as typeof filters.sortBy, page: 1 })}
                  >
                    <SelectTrigger className="h-8 w-[160px] text-xs sm:hidden">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {SORT_OPTIONS.map(opt => (
                        <SelectItem key={opt.value} value={opt.value}>
                          {opt.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>

                  {/* Save Search CTA */}
                  {isAuthenticated ? (
                    <Button
                      variant="outline"
                      size="sm"
                      className="border-primary text-primary hover:bg-primary/10 h-8 gap-1.5 text-xs"
                      onClick={() => setSaveModalOpen(true)}
                    >
                      <BookmarkPlus className="h-3.5 w-3.5" />
                      <span className="hidden sm:inline">Guardar búsqueda</span>
                      <span className="sm:hidden">Guardar</span>
                    </Button>
                  ) : (
                    <Button variant="outline" size="sm" className="h-8 gap-1.5 text-xs" asChild>
                      <Link href="/login?redirect=/vehiculos">
                        <LogIn className="h-3.5 w-3.5" />
                        <span className="hidden sm:inline">Guardar búsqueda</span>
                      </Link>
                    </Button>
                  )}
                </div>
              </div>

              {/* Active filter chips */}
              {activeChips.length > 0 && (
                <div className="flex flex-wrap items-center gap-1.5">
                  {activeChips.map(chip => (
                    <FilterChip
                      key={chip.key}
                      label={chip.label}
                      onRemove={() => removeChip(chip.key)}
                    />
                  ))}
                  {activeChips.length > 1 && (
                    <button
                      type="button"
                      onClick={clearFilters}
                      className="text-muted-foreground ml-1 text-xs transition-colors hover:text-red-600"
                    >
                      Limpiar todo
                    </button>
                  )}
                </div>
              )}
            </div>

            {/* Error state */}
            {error && (
              <div className="flex flex-col items-center justify-center rounded-xl border border-red-200 bg-red-50 py-16 text-center">
                <AlertCircle className="mb-3 h-10 w-10 text-red-500" />
                <h3 className="text-foreground text-lg font-semibold">Error al cargar</h3>
                <p className="text-muted-foreground mt-1 text-sm">
                  {error instanceof Error ? error.message : 'Ocurrió un error inesperado'}
                </p>
                <Button variant="outline" className="mt-4" onClick={() => refetch()}>
                  <RefreshCcw className="mr-2 h-4 w-4" />
                  Reintentar
                </Button>
              </div>
            )}

            {/* Loading skeleton — only on initial load */}
            {!error && isLoading && allVehicles.length === 0 && (
              <VehiclesGridSkeleton viewMode={viewMode} />
            )}

            {/* Results grid with ad slots */}
            {!error && allVehicles.length > 0 && (
              <div
                className={cn(
                  viewMode === 'grid'
                    ? 'grid gap-3 sm:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-4'
                    : 'flex flex-col gap-3'
                )}
              >
                {renderResults()}
              </div>
            )}

            {/* Empty state */}
            {!error && !isLoading && !isFetching && allVehicles.length === 0 && (
              <div className="flex flex-col items-center justify-center rounded-2xl border border-dashed border-slate-200 bg-white py-20 text-center shadow-sm dark:bg-slate-900">
                <div className="mb-5 flex h-20 w-20 items-center justify-center rounded-2xl bg-slate-100 dark:bg-slate-800">
                  <span className="text-5xl">🔍</span>
                </div>
                <h3 className="text-foreground text-xl font-bold">No encontramos resultados</h3>
                <p className="text-muted-foreground mt-2 max-w-sm text-sm leading-relaxed">
                  Prueba ajustando los filtros o buscando con otros términos.
                </p>
                <button
                  type="button"
                  onClick={clearFilters}
                  className="bg-primary hover:bg-primary/80 mt-5 rounded-xl px-6 py-2.5 text-sm font-semibold text-white shadow-md transition-all hover:shadow-lg"
                >
                  Limpiar filtros y ver todos
                </button>
                {isAuthenticated && (
                  <button
                    type="button"
                    onClick={() => setSaveModalOpen(true)}
                    className="text-primary mt-3 flex items-center gap-2 text-sm underline-offset-2 hover:underline"
                  >
                    <Bell className="h-3.5 w-3.5" />
                    Recibir alerta cuando haya resultados
                  </button>
                )}
              </div>
            )}

            {/* Infinite scroll sentinel — triggers next page load */}
            <div ref={sentinelRef} className="h-px" />

            {/* Loading more indicator */}
            {(isLoading || isFetching) && allVehicles.length > 0 && (
              <div className="mt-2 flex justify-center py-8">
                <div className="text-muted-foreground flex items-center gap-2 text-sm">
                  <RefreshCcw className="text-primary h-4 w-4 animate-spin" />
                  Cargando más vehículos…
                </div>
              </div>
            )}

            {/* End of results */}
            {!isLoading && !isFetching && allVehicles.length > 0 && currentPage >= totalPages && (
              <p className="text-muted-foreground mt-8 pb-6 text-center text-xs">
                Has visto los <span className="font-semibold">{totalResults.toLocaleString()}</span>{' '}
                vehículos
              </p>
            )}
          </main>
        </div>
      </div>

      {/* ═══════════════════════════════════════════════════════
          MOBILE FILTERS SHEET (bottom drawer)
      ═══════════════════════════════════════════════════════ */}
      <Sheet open={mobileFiltersOpen} onOpenChange={setMobileFiltersOpen}>
        <SheetContent side="left" className="w-[90vw] max-w-[340px] overflow-y-auto p-4">
          <SheetHeader className="mb-4">
            <SheetTitle>Filtros</SheetTitle>
          </SheetHeader>
          <VehicleFilters
            filters={filters}
            onChange={changes => {
              setFilters({ ...changes, page: 1 });
            }}
            onClear={() => {
              clearFilters();
              setMobileFiltersOpen(false);
            }}
            activeCount={activeFilterCount}
            makeCatalog={makes.map(m => ({ id: m.id, name: m.name }))}
            modelCatalog={models.map(m => ({ id: m.id, name: m.name, make: filters.make }))}
          />
          <div className="bg-card sticky bottom-0 mt-4 pt-4 pb-2">
            <Button
              className="bg-primary hover:bg-primary/80 w-full"
              onClick={() => setMobileFiltersOpen(false)}
            >
              Ver {totalResults > 0 ? `${totalResults.toLocaleString()} ` : ''}resultados
            </Button>
          </div>
        </SheetContent>
      </Sheet>

      {/* ═══════════════════════════════════════════════════════
          FLOATING MOBILE BOTTOM CTA
      ═══════════════════════════════════════════════════════ */}
      <div className="pb-safe fixed bottom-6 left-4 z-40 flex flex-col items-start gap-2 lg:hidden">
        <button
          type="button"
          onClick={() =>
            isAuthenticated
              ? setSaveModalOpen(true)
              : (window.location.href = '/login?redirect=/vehiculos')
          }
          className="bg-primary hover:bg-primary/80 flex items-center gap-2 rounded-full px-4 py-3 text-sm font-semibold text-white shadow-xl ring-2 ring-white/30 transition-all hover:shadow-2xl active:scale-95"
          aria-label="Guardar búsqueda y crear alerta"
        >
          <Bell className="h-4 w-4" />
          <span>Guardar búsqueda</span>
        </button>
      </div>

      {/* ═══════════════════════════════════════════════════════
          SAVE SEARCH MODAL
      ═══════════════════════════════════════════════════════ */}
      <SaveSearchModal
        open={saveModalOpen}
        onOpenChange={setSaveModalOpen}
        filters={filters}
        totalResults={totalResults}
      />
    </div>
  );
}
