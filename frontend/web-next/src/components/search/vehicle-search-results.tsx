/**
 * Vehicle Search Results Component
 *
 * Displays search results with sorting and view options
 */

'use client';

import * as React from 'react';
import { Grid, List, SlidersHorizontal, ChevronDown } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { VehicleCard } from '@/components/ui/vehicle-card';
import { Skeleton } from '@/components/ui/skeleton';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetTrigger } from '@/components/ui/sheet';
import { cn } from '@/lib/utils';
import { VehicleFilters } from './vehicle-filters';
import type { VehicleSearchFilters, VehicleSearchResult } from '@/hooks/use-vehicle-search';
import type { VehicleCardData } from '@/types';

// =============================================================================
// TYPES
// =============================================================================

export interface VehicleSearchResultsProps {
  vehicles: VehicleSearchResult[];
  total: number;
  page: number;
  totalPages: number;
  filters: VehicleSearchFilters;
  isLoading: boolean;
  isFetching: boolean;
  onFilterChange: (filters: Partial<VehicleSearchFilters>) => void;
  onClearFilters: () => void;
  activeFilterCount: number;
  onPageChange: (page: number) => void;
  facets?: {
    makes: { value: string; count: number }[];
    bodyTypes: { value: string; count: number }[];
    provinces: { value: string; count: number }[];
    fuelTypes: { value: string; count: number }[];
    transmissions: { value: string; count: number }[];
  };
  className?: string;
}

// =============================================================================
// SORT OPTIONS
// =============================================================================

const sortOptions = [
  { value: 'relevance', label: 'Más relevantes' },
  { value: 'price_asc', label: 'Precio: Menor a mayor' },
  { value: 'price_desc', label: 'Precio: Mayor a menor' },
  { value: 'year_desc', label: 'Año: Más reciente' },
  { value: 'year_asc', label: 'Año: Más antiguo' },
  { value: 'mileage_asc', label: 'Kilometraje: Menor a mayor' },
  { value: 'newest', label: 'Publicación: Más reciente' },
];

// =============================================================================
// MAIN COMPONENT
// =============================================================================

export function VehicleSearchResults({
  vehicles,
  total,
  page,
  totalPages,
  filters,
  isLoading,
  isFetching,
  onFilterChange,
  onClearFilters,
  activeFilterCount,
  onPageChange,
  facets,
  className,
}: VehicleSearchResultsProps) {
  const [viewMode, setViewMode] = React.useState<'grid' | 'list'>('grid');
  const [mobileFiltersOpen, setMobileFiltersOpen] = React.useState(false);

  const currentSort = sortOptions.find(opt => opt.value === filters.sortBy) || sortOptions[0];

  // Convert VehicleSearchResult to VehicleCardData
  const convertToCardData = (vehicle: VehicleSearchResult): VehicleCardData => ({
    id: vehicle.id,
    slug: vehicle.slug,
    make: vehicle.make,
    model: vehicle.model,
    year: vehicle.year,
    price: vehicle.price,
    currency: vehicle.currency,
    mileage: vehicle.mileage,
    transmission: vehicle.transmission,
    fuelType: vehicle.fuelType,
    imageUrl: vehicle.imageUrl,
    location: vehicle.location,
    dealRating: vehicle.dealRating,
    isFeatured: vehicle.isFeatured,
    isVerified: vehicle.isVerified,
    dealerName: vehicle.dealerName,
    dealerRating: vehicle.dealerRating,
    trim: vehicle.trim,
    photoCount: vehicle.photoCount,
    isNew: vehicle.isNew,
    isCertified: vehicle.isCertified,
    monthlyPayment: vehicle.monthlyPayment,
    createdAt: vehicle.createdAt,
  });

  return (
    <div className={cn('space-y-4', className)}>
      {/* Toolbar */}
      <div className="flex flex-wrap items-center justify-between gap-4">
        {/* Results count */}
        <div className="text-muted-foreground text-sm">
          {isLoading ? (
            <Skeleton className="h-5 w-32" />
          ) : (
            <>
              <span className="text-foreground font-medium">{total.toLocaleString()}</span>{' '}
              vehículos encontrados
            </>
          )}
        </div>

        {/* Controls */}
        <div className="flex items-center gap-2">
          {/* Mobile filter button */}
          <Sheet open={mobileFiltersOpen} onOpenChange={setMobileFiltersOpen}>
            <SheetTrigger asChild>
              <Button variant="outline" size="sm" className="gap-2 lg:hidden">
                <SlidersHorizontal className="h-4 w-4" />
                Filtros
                {activeFilterCount > 0 && (
                  <span className="bg-primary flex h-5 w-5 items-center justify-center rounded-full text-xs text-white">
                    {activeFilterCount}
                  </span>
                )}
              </Button>
            </SheetTrigger>
            <SheetContent side="left" className="w-[300px] overflow-y-auto">
              <SheetHeader>
                <SheetTitle>Filtros</SheetTitle>
              </SheetHeader>
              <VehicleFilters
                filters={filters}
                onChange={onFilterChange}
                onClear={onClearFilters}
                activeCount={activeFilterCount}
                facets={facets}
                className="mt-4"
              />
            </SheetContent>
          </Sheet>

          {/* Sort dropdown */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="outline" size="sm" className="gap-2">
                {currentSort.label}
                <ChevronDown className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              {sortOptions.map(option => (
                <DropdownMenuItem
                  key={option.value}
                  onClick={() =>
                    onFilterChange({ sortBy: option.value as VehicleSearchFilters['sortBy'] })
                  }
                  className={cn(filters.sortBy === option.value && 'bg-muted')}
                >
                  {option.label}
                </DropdownMenuItem>
              ))}
            </DropdownMenuContent>
          </DropdownMenu>

          {/* View mode toggle */}
          <div className="hidden items-center rounded-md border sm:flex">
            <Button
              variant={viewMode === 'grid' ? 'secondary' : 'ghost'}
              size="sm"
              onClick={() => setViewMode('grid')}
              className="rounded-r-none"
            >
              <Grid className="h-4 w-4" />
            </Button>
            <Button
              variant={viewMode === 'list' ? 'secondary' : 'ghost'}
              size="sm"
              onClick={() => setViewMode('list')}
              className="rounded-l-none"
            >
              <List className="h-4 w-4" />
            </Button>
          </div>
        </div>
      </div>

      {/* Loading overlay */}
      <div className={cn('relative', isFetching && !isLoading && 'opacity-60')}>
        {/* Results grid/list */}
        {isLoading ? (
          <div
            className={cn(
              'grid gap-4',
              viewMode === 'grid'
                ? 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4'
                : 'grid-cols-1'
            )}
          >
            {Array.from({ length: 9 }).map((_, i) => (
              <VehicleCardSkeleton
                key={i}
                variant={viewMode === 'list' ? 'horizontal' : 'default'}
              />
            ))}
          </div>
        ) : vehicles.length === 0 ? (
          <EmptyState onClearFilters={onClearFilters} hasFilters={activeFilterCount > 0} />
        ) : (
          <div
            className={cn(
              'grid gap-4',
              viewMode === 'grid'
                ? 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4'
                : 'grid-cols-1'
            )}
          >
            {vehicles.map(vehicle => (
              <VehicleCard
                key={vehicle.id}
                vehicle={convertToCardData(vehicle)}
                variant={viewMode === 'list' ? 'horizontal' : 'default'}
                showDealRating
                showFavoriteButton
              />
            ))}
          </div>
        )}
      </div>

      {/* Pagination */}
      {totalPages > 1 && !isLoading && (
        <Pagination currentPage={page} totalPages={totalPages} onPageChange={onPageChange} />
      )}
    </div>
  );
}

// =============================================================================
// SUB-COMPONENTS
// =============================================================================

function VehicleCardSkeleton({ variant = 'default' }: { variant?: 'default' | 'horizontal' }) {
  if (variant === 'horizontal') {
    return (
      <div className="flex gap-4 rounded-xl border p-4">
        <Skeleton className="h-32 w-48 rounded-lg" />
        <div className="flex-1 space-y-2">
          <Skeleton className="h-5 w-3/4" />
          <Skeleton className="h-4 w-1/2" />
          <Skeleton className="h-6 w-1/4" />
        </div>
      </div>
    );
  }

  return (
    <div className="overflow-hidden rounded-xl border">
      <Skeleton className="aspect-[16/10] w-full" />
      <div className="space-y-2 p-4">
        <Skeleton className="h-5 w-3/4" />
        <Skeleton className="h-4 w-1/2" />
        <Skeleton className="h-6 w-1/3" />
      </div>
    </div>
  );
}

function EmptyState({
  onClearFilters,
  hasFilters,
}: {
  onClearFilters: () => void;
  hasFilters: boolean;
}) {
  return (
    <div className="flex min-h-[400px] flex-col items-center justify-center text-center">
      <div className="bg-muted flex h-16 w-16 items-center justify-center rounded-full">
        <span className="text-4xl">🔍</span>
      </div>
      <h3 className="text-foreground mt-4 text-lg font-medium">No se encontraron vehículos</h3>
      <p className="text-muted-foreground mt-2 max-w-sm">
        {hasFilters
          ? 'Intenta ajustar los filtros para encontrar más resultados.'
          : 'No hay vehículos disponibles en este momento.'}
      </p>
      {hasFilters && (
        <Button onClick={onClearFilters} className="bg-primary hover:bg-primary/90 mt-4 gap-2">
          Limpiar filtros
        </Button>
      )}
    </div>
  );
}

function Pagination({
  currentPage,
  totalPages,
  onPageChange,
}: {
  currentPage: number;
  totalPages: number;
  onPageChange: (page: number) => void;
}) {
  const getVisiblePages = () => {
    const delta = 2;
    const range = [];
    const rangeWithDots = [];

    for (
      let i = Math.max(2, currentPage - delta);
      i <= Math.min(totalPages - 1, currentPage + delta);
      i++
    ) {
      range.push(i);
    }

    if (currentPage - delta > 2) {
      rangeWithDots.push(1, 'dots-1');
    } else {
      rangeWithDots.push(1);
    }

    rangeWithDots.push(...range);

    if (currentPage + delta < totalPages - 1) {
      rangeWithDots.push('dots-2', totalPages);
    } else if (totalPages > 1) {
      rangeWithDots.push(totalPages);
    }

    return rangeWithDots;
  };

  const pages = getVisiblePages();

  return (
    <nav className="flex items-center justify-center gap-1">
      <Button
        variant="outline"
        size="sm"
        disabled={currentPage === 1}
        onClick={() => onPageChange(currentPage - 1)}
      >
        Anterior
      </Button>

      {pages.map((pageItem, _idx) => {
        if (typeof pageItem === 'string') {
          return (
            <span key={pageItem} className="text-muted-foreground px-2">
              ...
            </span>
          );
        }

        return (
          <Button
            key={pageItem}
            variant={currentPage === pageItem ? 'default' : 'outline'}
            size="sm"
            onClick={() => onPageChange(pageItem)}
            className={cn(
              'min-w-[36px]',
              currentPage === pageItem && 'bg-primary hover:bg-primary/90'
            )}
          >
            {pageItem}
          </Button>
        );
      })}

      <Button
        variant="outline"
        size="sm"
        disabled={currentPage === totalPages}
        onClick={() => onPageChange(currentPage + 1)}
      >
        Siguiente
      </Button>
    </nav>
  );
}

export default VehicleSearchResults;
