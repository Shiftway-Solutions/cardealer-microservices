'use client';

import * as React from 'react';
import { Suspense } from 'react';
import { VehicleFilters, VehicleSearchResults } from '@/components/search';
import { useVehicleSearch } from '@/hooks/use-vehicle-search';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/components/ui/sheet';
import { Filter, X, MapPin, Sparkles, TrendingUp, Clock } from 'lucide-react';
import { cn } from '@/lib/utils';
import Link from 'next/link';
import AiSearchBar from '@/components/search/ai-search-bar';

// Loading skeleton for search
function SearchLoading() {
  return (
    <div className="container mx-auto px-4 py-6">
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-4">
        <aside className="lg:col-span-1">
          <Skeleton className="h-[600px] w-full rounded-lg" />
        </aside>
        <main className="lg:col-span-3">
          <Skeleton className="mb-4 h-12 w-full rounded-lg" />
          <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
            {Array.from({ length: 6 }).map((_, i) => (
              <Skeleton key={i} className="h-[300px] w-full rounded-lg" />
            ))}
          </div>
        </main>
      </div>
    </div>
  );
}

// Quick filters component
interface QuickFilter {
  id: string;
  label: string;
  icon: React.ElementType;
  filters: Record<string, unknown>;
}

const quickFilters: QuickFilter[] = [
  {
    id: 'best-deals',
    label: 'Mejores Ofertas',
    icon: Sparkles,
    filters: { dealRating: ['great', 'good'] },
  },
  {
    id: 'new-listings',
    label: 'Nuevos',
    icon: Clock,
    filters: { sortBy: 'newest' },
  },
  {
    id: 'low-mileage',
    label: 'Bajo Kilometraje',
    icon: TrendingUp,
    filters: { maxMileage: 50000 },
  },
  {
    id: 'santo-domingo',
    label: 'Santo Domingo',
    icon: MapPin,
    filters: { location: 'santo-domingo' },
  },
  {
    id: 'santiago',
    label: 'Santiago',
    icon: MapPin,
    filters: { location: 'santiago' },
  },
];

// Main search content
function SearchContent() {
  const { filters, setFilters, clearFilters, results, isLoading, isFetching, activeFilterCount } =
    useVehicleSearch();

  const [showMobileFilters, setShowMobileFilters] = React.useState(false);
  const [activeQuickFilters, setActiveQuickFilters] = React.useState<string[]>([]);

  // Toggle quick filter
  const toggleQuickFilter = (filter: QuickFilter) => {
    const isActive = activeQuickFilters.includes(filter.id);

    if (isActive) {
      setActiveQuickFilters(prev => prev.filter(id => id !== filter.id));
      // Only remove the specific keys that this quick filter set
      const keysToRemove = Object.keys(filter.filters) as (keyof typeof filters)[];
      const cleaned = { ...filters };
      for (const key of keysToRemove) {
        delete cleaned[key];
      }
      setFilters({ ...cleaned, page: 1 } as typeof filters);
    } else {
      setActiveQuickFilters(prev => [...prev, filter.id]);
      setFilters({ ...filters, ...filter.filters } as typeof filters);
    }
  };

  return (
    <div className="bg-muted/50 min-h-screen">
      {/* Hero Section */}
      <div className="from-primary to-primary/90 text-primary-foreground bg-gradient-to-r py-12">
        <div className="container mx-auto px-4">
          <h1 className="mb-2 text-3xl font-bold md:text-4xl">Búsqueda de Vehículos</h1>
          <p className="text-primary-foreground/80 mb-6 text-lg">
            Encuentra tu próximo vehículo entre miles de opciones
          </p>

          {/* AI Search Bar */}
          <AiSearchBar
            onFiltersApplied={aiFilters => {
              setFilters({ ...filters, ...aiFilters } as typeof filters);
            }}
            className="[&_input]:text-foreground [&_input]:placeholder:text-muted-foreground [&_.text-muted-foreground]:text-primary-foreground/70 mb-6 [&_.text-amber-600]:text-amber-200 [&_.text-destructive]:text-red-300 [&_input]:bg-white/95"
          />

          {/* Quick Filters */}
          <div className="flex flex-wrap gap-2">
            {quickFilters.map(filter => {
              const Icon = filter.icon;
              const isActive = activeQuickFilters.includes(filter.id);

              return (
                <Button
                  key={filter.id}
                  variant={isActive ? 'secondary' : 'outline'}
                  size="sm"
                  className={cn(
                    'rounded-full transition-all',
                    isActive
                      ? 'hover:bg-muted text-primary bg-white'
                      : 'border-white/30 bg-white/10 text-white hover:bg-white/20'
                  )}
                  onClick={() => toggleQuickFilter(filter)}
                >
                  <Icon className="mr-1.5 h-4 w-4" />
                  {filter.label}
                </Button>
              );
            })}
          </div>
        </div>
      </div>

      {/* Active Filters Bar */}
      {activeFilterCount > 0 && (
        <div className="border-border bg-card border-b py-3">
          <div className="container mx-auto px-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <span className="text-muted-foreground text-sm">Filtros activos:</span>
                <Badge variant="secondary" className="bg-primary/10 text-primary">
                  {activeFilterCount}
                </Badge>

                {/* Show some active filters as badges */}
                {filters.make && (
                  <Badge variant="outline" className="gap-1">
                    Marca: {filters.make}
                  </Badge>
                )}
                {(filters.priceMin || filters.priceMax) && (
                  <Badge variant="outline" className="gap-1">
                    Precio: {filters.priceMin ? `$${filters.priceMin.toLocaleString()}` : '$0'} -{' '}
                    {filters.priceMax ? `$${filters.priceMax.toLocaleString()}` : '∞'}
                  </Badge>
                )}
              </div>

              <Button
                variant="ghost"
                size="sm"
                className="text-muted-foreground hover:text-foreground"
                onClick={() => {
                  clearFilters();
                  setActiveQuickFilters([]);
                }}
              >
                <X className="mr-1 h-4 w-4" />
                Limpiar todo
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Main Content */}
      <div className="container mx-auto px-4 py-6">
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-4">
          {/* Desktop Filters Sidebar */}
          <aside className="hidden lg:col-span-1 lg:block">
            <div className="sticky top-4">
              <VehicleFilters
                filters={filters}
                onChange={setFilters}
                onClear={clearFilters}
                activeCount={activeFilterCount}
              />
            </div>
          </aside>

          {/* Results */}
          <main className="lg:col-span-3">
            {/* Mobile Filter Button + Sheet Drawer */}
            <div className="mb-4 lg:hidden">
              <Button
                variant="outline"
                className="w-full justify-center"
                onClick={() => setShowMobileFilters(true)}
              >
                <Filter className="mr-2 h-4 w-4" />
                Filtros
                {activeFilterCount > 0 && (
                  <Badge variant="secondary" className="bg-primary/10 text-primary ml-2">
                    {activeFilterCount}
                  </Badge>
                )}
              </Button>
            </div>

            {/* Mobile Filters Sheet */}
            <Sheet open={showMobileFilters} onOpenChange={setShowMobileFilters}>
              <SheetContent side="left" className="w-[320px] overflow-y-auto p-4 sm:w-[380px]">
                <SheetHeader>
                  <SheetTitle>Filtros de búsqueda</SheetTitle>
                </SheetHeader>
                <div className="mt-4">
                  <VehicleFilters
                    filters={filters}
                    onChange={newFilters => {
                      setFilters(newFilters);
                      setShowMobileFilters(false);
                    }}
                    onClear={() => {
                      clearFilters();
                      setShowMobileFilters(false);
                    }}
                    activeCount={activeFilterCount}
                  />
                </div>
              </SheetContent>
            </Sheet>

            {/* Results with toolbar */}
            <VehicleSearchResults
              vehicles={results?.vehicles ?? []}
              total={results?.total ?? 0}
              page={results?.page ?? 1}
              totalPages={results?.totalPages ?? 1}
              filters={filters}
              isLoading={isLoading}
              isFetching={isFetching}
              onFilterChange={setFilters}
              onClearFilters={clearFilters}
              activeFilterCount={activeFilterCount}
              onPageChange={page => setFilters({ page })}
            />
          </main>
        </div>
      </div>

      {/* SEO Content */}
      <section className="border-border bg-card border-t py-12">
        <div className="container mx-auto px-4">
          <h2 className="mb-6 text-2xl font-bold">
            Encuentra el vehículo perfecto en República Dominicana
          </h2>

          <div className="grid grid-cols-1 gap-8 md:grid-cols-3">
            <div>
              <h3 className="mb-2 text-lg font-semibold">Miles de opciones</h3>
              <p className="text-muted-foreground">
                Explora la mayor selección de vehículos en RD. Desde económicos hasta lujo, nuevos y
                usados.
              </p>
            </div>

            <div>
              <h3 className="mb-2 text-lg font-semibold">Precios transparentes</h3>
              <p className="text-muted-foreground">
                Nuestro sistema de Deal Rating te ayuda a identificar las mejores ofertas del
                mercado.
              </p>
            </div>

            <div>
              <h3 className="mb-2 text-lg font-semibold">Vendedores verificados</h3>
              <p className="text-muted-foreground">
                Dealers y vendedores verificados para que compres con total confianza y seguridad.
              </p>
            </div>
          </div>

          {/* Popular Searches */}
          <div className="mt-10">
            <h3 className="mb-4 text-lg font-semibold">Búsquedas Populares</h3>
            <div className="flex flex-wrap gap-2">
              {[
                'Toyota Corolla',
                'Honda CR-V',
                'Hyundai Tucson',
                'Kia Sportage',
                'Toyota RAV4',
                'Nissan Kicks',
                'Mazda CX-5',
                'Hyundai Santa Fe',
                'Toyota Hilux',
                'Jeep Wrangler',
              ].map(search => (
                <Link
                  key={search}
                  href={`/buscar?q=${encodeURIComponent(search)}`}
                  className="bg-muted text-foreground hover:bg-muted/80 rounded-full px-3 py-1.5 text-sm transition-colors"
                >
                  {search}
                </Link>
              ))}
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}

// Page export with Suspense
export default function BuscarPage() {
  return (
    <Suspense fallback={<SearchLoading />}>
      <SearchContent />
    </Suspense>
  );
}
