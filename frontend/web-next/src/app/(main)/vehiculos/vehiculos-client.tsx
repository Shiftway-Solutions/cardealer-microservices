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
  Car,
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
import { VehicleFilters } from '@/components/search/vehicle-filters';
import { SaveSearchModal } from '@/components/search/save-search-modal';
import { aiSearch, aiFiltersToSearchParams } from '@/services/search-agent';
import { useVehicleSearch } from '@/hooks/use-vehicle-search';
import { useFavorites } from '@/hooks/use-favorites';
import { useMakes, useModelsByMake } from '@/hooks/use-vehicles';
import { useAuth } from '@/hooks/use-auth';
import { useSponsoredSearch } from '@/hooks/use-ads';
import { SponsoredVehicleCard, SidebarAdUnit } from '@/components/advertising/native-ads';
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
// AD SLOT COMPONENTS
// =============================================================================

/**
 * PromoLeaderboard — Branded "Sell your car" conversion banner.
 * Replaces the generic AdSense placeholder until ads are configured.
 */
function AdSlotLeaderboard() {
  return (
    <div
      className="col-span-full my-3 overflow-hidden rounded-2xl bg-gradient-to-r from-[#00A870] to-[#00c882] shadow-md"
      aria-label="Banner promocional"
    >
      <div className="flex flex-col items-center justify-between gap-4 px-6 py-5 sm:flex-row">
        <div className="flex items-center gap-4">
          <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-white/20">
            <Car className="h-6 w-6 text-white" />
          </div>
          <div>
            <p className="text-lg font-bold text-white">¿Tienes un vehículo para vender?</p>
            <p className="text-sm text-white/80">
              Publica en minutos y llega a miles de compradores en RD
            </p>
          </div>
        </div>
        <Link
          href="/vender"
          className="flex shrink-0 items-center gap-2 rounded-xl bg-white px-5 py-2.5 text-sm font-bold text-[#00A870] shadow-sm transition-all hover:scale-105 hover:shadow-md"
        >
          <Tag className="h-4 w-4" />
          Publicar ahora
        </Link>
      </div>
    </div>
  );
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
        <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-[#00A870]/20">
          <Bell className="h-6 w-6 text-[#00A870]" />
        </div>
        <div>
          <p className="font-bold text-white">Crea una alerta</p>
          <p className="mt-1 text-xs text-slate-400">
            Recibe notificaciones cuando aparezca un vehículo que te interese
          </p>
        </div>
        <Link
          href="/cuenta/alertas"
          className="mt-1 flex w-full items-center justify-center gap-2 rounded-xl bg-[#00A870] py-2.5 text-sm font-semibold text-white transition-all hover:bg-[#008a5c]"
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
        className="hover:bg-muted ml-0.5 flex-shrink-0 rounded-full p-0.5 transition-colors"
        aria-label={`Eliminar filtro ${label}`}
      >
        <X className="h-2.5 w-2.5" />
      </button>
    </Badge>
  );
}

// =============================================================================
// MAIN CLIENT COMPONENT
// =============================================================================

export default function VehiculosClient() {
  const { isAuthenticated } = useAuth();
  const [viewMode, setViewMode] = React.useState<'grid' | 'list'>('grid');
  const [mobileFiltersOpen, setMobileFiltersOpen] = React.useState(false);
  const [saveModalOpen, setSaveModalOpen] = React.useState(false);
  const [searchInput, setSearchInput] = React.useState('');
  const [isAiSearching, setIsAiSearching] = React.useState(false);
  const [aiSearchInfo, setAiSearchInfo] = React.useState<{
    query: string;
    confidence: number;
    latencyMs: number;
    wasCached: boolean;
  } | null>(null);

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
    return chips;
  }, [filters]);

  const removeChip = (key: string) => {
    if (key === 'year') {
      setFilters({ yearMin: undefined, yearMax: undefined });
    } else if (key === 'price') {
      setFilters({ priceMin: undefined, priceMax: undefined });
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
          // AI disabled — fall back to text search
          setFilters({ query: query.trim(), page: 1 });
          return;
        }

        if (ai.confianza === 0) {
          // Low confidence — fall back to text search
          setFilters({ query: query.trim(), page: 1 });
          return;
        }

        // Apply AI-generated filters
        if (ai.filtros_exactos) {
          const mapped = aiFiltersToSearchParams(ai.filtros_exactos);
          setFilters({ ...mapped, page: 1 } as Parameters<typeof setFilters>[0]);
        }

        setAiSearchInfo({
          query: ai.query_reformulada ?? query.trim(),
          confidence: ai.confianza,
          latencyMs: result.latencyMs,
          wasCached: result.wasCached,
        });
      } catch {
        // On AI error, fall back to regular text search
        setFilters({ query: query.trim(), page: 1 });
      } finally {
        setIsAiSearching(false);
      }
    },
    [isAiSearching, setFilters]
  );

  // Sync searchInput with filters.query from URL
  React.useEffect(() => {
    if (!isAiSearching) {
      setSearchInput(filters.query ?? '');
    }
  }, [filters.query, isAiSearching]);

  // Render accumulated vehicles with ad slots every 6 items
  const renderResults = () => {
    if (!allVehicles.length) return null;

    if (viewMode === 'list') {
      return (
        <>
          {/* Top sponsored results in list view */}
          {topSponsored.length > 0 && currentPage === 1 && allVehicles.length > 0 && (
            <>
              {topSponsored.slice(0, 2).map((sv: SponsoredVehicle) => (
                <SponsoredVehicleCard key={sv.id} vehicle={sv} variant="horizontal" />
              ))}
            </>
          )}
          {allVehicles.map((vehicle: VehicleCardData, i: number) => (
            <React.Fragment key={vehicle.id}>
              <VehicleCard
                vehicle={vehicle}
                variant="horizontal"
                isFavorite={isFavorite(vehicle.id)}
                onFavoriteClick={handleFavoriteToggle}
                priority={i < 3}
              />
              {(i + 1) % 6 === 0 && <AdSlotLeaderboard />}
              {/* Inline sponsored every 10 items */}
              {(i + 1) % 10 === 0 &&
                inlineSponsored[Math.floor(i / 10) % inlineSponsored.length] && (
                  <SponsoredVehicleCard
                    vehicle={inlineSponsored[Math.floor(i / 10) % inlineSponsored.length]}
                    variant="horizontal"
                  />
                )}
            </React.Fragment>
          ))}
        </>
      );
    }

    // Grid: insert sponsored results + leaderboard
    const items: React.ReactNode[] = [];

    // Top sponsored results (positions 1-3, only on first page)
    if (topSponsored.length > 0 && currentPage === 1) {
      topSponsored.slice(0, 3).forEach((sv: SponsoredVehicle) => {
        items.push(<SponsoredVehicleCard key={`sp-top-${sv.id}`} vehicle={sv} priority />);
      });
    }

    let inlineSponsoredIdx = 0;
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
      if ((i + 1) % 6 === 0) {
        items.push(<AdSlotLeaderboard key={`ad-${i}`} />);
      }
      // Insert inline sponsored every 8 organic results
      if ((i + 1) % 8 === 0 && inlineSponsoredIdx < inlineSponsored.length) {
        items.push(
          <SponsoredVehicleCard
            key={`sp-inline-${inlineSponsored[inlineSponsoredIdx].id}`}
            vehicle={inlineSponsored[inlineSponsoredIdx]}
          />
        );
        inlineSponsoredIdx++;
      }
    });
    return items;
  };

  return (
    <div className="min-h-screen bg-slate-50 dark:bg-slate-950">
      {/* ═══════════════════════════════════════════════════════
          STICKY HEADER: Search bar + Body type quick filters
      ═══════════════════════════════════════════════════════ */}
      <div className="border-border sticky top-0 z-30 border-b bg-white shadow-sm dark:bg-slate-900">
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
                <Sparkles className="pointer-events-none absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2 animate-pulse text-[#00A870]" />
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
                className={cn(
                  'h-10 pr-10 pl-9 text-sm',
                  isAiSearching && 'ring-2 ring-[#00A870]/40'
                )}
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
                    ? 'bg-card text-[#00A870] shadow-sm'
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
                    ? 'bg-card text-[#00A870] shadow-sm'
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
                <span className="absolute -top-1.5 -right-1.5 flex h-4 w-4 items-center justify-center rounded-full bg-[#00A870] text-[10px] font-bold text-white">
                  {activeFilterCount}
                </span>
              )}
            </Button>
          </div>

          {/* Row 2: Trust micro-bar (social proof — compact, always visible) */}
          <div className="scrollbar-none mt-1.5 flex items-center gap-4 overflow-x-auto pb-0.5">
            <div className="text-muted-foreground flex shrink-0 items-center gap-1 text-[11px]">
              <ShieldCheck className="h-3 w-3 text-[#00A870]" />
              <span>Vendedores verificados</span>
            </div>
            <span className="text-border shrink-0">·</span>
            <div className="text-muted-foreground flex shrink-0 items-center gap-1 text-[11px]">
              <Star className="h-3 w-3 text-amber-500" />
              <span>+2,400 vehículos activos</span>
            </div>
            <span className="text-border shrink-0">·</span>
            <div className="text-muted-foreground flex shrink-0 items-center gap-1 text-[11px]">
              <Phone className="h-3 w-3 text-[#00A870]" />
              <span>Contacto directo</span>
            </div>
            <span className="text-border hidden shrink-0 sm:inline">·</span>
            <div className="text-muted-foreground hidden shrink-0 items-center gap-1 text-[11px] sm:flex">
              <Bell className="h-3 w-3 text-[#00A870]" />
              <span>Alertas gratis</span>
            </div>
          </div>

          {/* AI search info banner */}
          {aiSearchInfo && (
            <div className="mt-1.5 flex items-center gap-2 rounded-lg bg-[#00A870]/5 px-3 py-1.5 dark:bg-[#00A870]/10">
              <Sparkles className="h-3.5 w-3.5 shrink-0 text-[#00A870]" />
              <span className="truncate text-xs text-[#005236] dark:text-[#4fd4a4]">
                IA interpretó: <strong>&ldquo;{aiSearchInfo.query}&rdquo;</strong>
              </span>
              <div className="ml-auto flex shrink-0 items-center gap-2">
                <span className="rounded-full bg-[#00A870]/10 px-2 py-0.5 text-[10px] font-medium text-[#00A870] dark:bg-[#00A870]/20 dark:text-[#4fd4a4]">
                  {Math.round(aiSearchInfo.confidence * 100)}% confianza
                </span>
                <span className="text-[10px] text-[#00A870]/60">{aiSearchInfo.latencyMs}ms</span>
                {aiSearchInfo.wasCached && (
                  <span className="rounded-full bg-[#00A870]/10 px-1.5 py-0.5 text-[10px] text-[#00A870]">
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
                  className="ml-1 text-[#00A870]/50 hover:text-[#00A870]"
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
            <div className="border-border sticky top-[165px] rounded-2xl border bg-white shadow-sm dark:bg-slate-900">
              {/* Sidebar header */}
              <div className="border-border flex items-center justify-between border-b px-4 py-3">
                <div className="flex items-center gap-2">
                  <SlidersHorizontal className="h-4 w-4 text-[#00A870]" />
                  <span className="text-sm font-bold">Filtros</span>
                  {activeFilterCount > 0 && (
                    <span className="flex h-5 min-w-[20px] items-center justify-center rounded-full bg-[#00A870] px-1 text-[10px] font-bold text-white">
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

                {/* Sidebar sponsored vehicles */}
                {inlineSponsored.length > 0 && (
                  <SidebarAdUnit vehicles={inlineSponsored.slice(0, 2)} className="mt-4" />
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
                        ? 'border-[#00A870] bg-[#00A870] text-white shadow-sm'
                        : 'border-border text-muted-foreground hover:text-foreground bg-white hover:border-[#00A870]/60 dark:bg-slate-900'
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
                      className="h-8 gap-1.5 border-[#00A870] text-xs text-[#00A870] hover:bg-[#00A870]/10"
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
                  className="mt-5 rounded-xl bg-[#00A870] px-6 py-2.5 text-sm font-semibold text-white shadow-md transition-all hover:bg-[#008a5c] hover:shadow-lg"
                >
                  Limpiar filtros y ver todos
                </button>
                {isAuthenticated && (
                  <button
                    type="button"
                    onClick={() => setSaveModalOpen(true)}
                    className="mt-3 flex items-center gap-2 text-sm text-[#00A870] underline-offset-2 hover:underline"
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
                  <RefreshCcw className="h-4 w-4 animate-spin text-[#00A870]" />
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
              className="w-full bg-[#00A870] hover:bg-[#008a5c]"
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
      <div className="fixed bottom-6 left-4 z-50 flex flex-col items-start gap-2 lg:hidden">
        <button
          type="button"
          onClick={() =>
            isAuthenticated
              ? setSaveModalOpen(true)
              : (window.location.href = '/login?redirect=/vehiculos')
          }
          className="flex items-center gap-2 rounded-full bg-[#00A870] px-4 py-3 text-sm font-semibold text-white shadow-xl ring-2 ring-white/30 transition-all hover:bg-[#008a5c] hover:shadow-2xl active:scale-95"
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
