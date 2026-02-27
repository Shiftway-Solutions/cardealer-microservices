'use client';

import * as React from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { Search, SlidersHorizontal, Grid3X3, List, X, AlertCircle, RefreshCcw } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { VehicleCard, VehicleCardSkeleton } from '@/components/ui/vehicle-card';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';
import {
  useVehicleSearch,
  useMakes,
  useBodyTypes,
  useFuelTypes,
  useTransmissions,
} from '@/hooks/use-vehicles';
import { useFavorites } from '@/hooks/use-favorites';
import type { VehicleSearchParams, VehicleCardData } from '@/types';

// =============================================================================
// TYPES
// =============================================================================

interface ActiveFilter {
  key: string;
  value: string;
  label: string;
}

// =============================================================================
// HELPERS
// =============================================================================

/**
 * Build search params from active filters and search query
 */
function buildSearchParams(
  searchQuery: string,
  activeFilters: ActiveFilter[],
  sortBy: string,
  page: number
): VehicleSearchParams {
  const params: VehicleSearchParams = {
    page,
    pageSize: 12,
  };

  if (searchQuery) {
    params.q = searchQuery;
  }

  // Map sortBy to API format
  switch (sortBy) {
    case 'price-asc':
      params.sortBy = 'price';
      params.sortOrder = 'asc';
      break;
    case 'price-desc':
      params.sortBy = 'price';
      params.sortOrder = 'desc';
      break;
    case 'year-desc':
      params.sortBy = 'year';
      params.sortOrder = 'desc';
      break;
    case 'mileage-asc':
      params.sortBy = 'mileage';
      params.sortOrder = 'asc';
      break;
    default:
      // relevance - no sorting param needed
      break;
  }

  // Apply filters
  activeFilters.forEach(filter => {
    switch (filter.key) {
      case 'make':
        params.make = filter.value;
        break;
      case 'bodyType':
        params.bodyType = filter.value as VehicleSearchParams['bodyType'];
        break;
      case 'transmission':
        params.transmission = filter.value;
        break;
      case 'fuelType':
        params.fuelType = filter.value;
        break;
    }
  });

  return params;
}

// =============================================================================
// SKELETON COMPONENTS
// =============================================================================

function VehiclesPageSkeleton({ viewMode }: { viewMode: 'grid' | 'list' }) {
  return (
    <div
      className={
        viewMode === 'grid' ? 'grid gap-6 sm:grid-cols-2 lg:grid-cols-3' : 'flex flex-col gap-4'
      }
    >
      {Array.from({ length: 12 }).map((_, i) => (
        <VehicleCardSkeleton key={i} variant={viewMode === 'list' ? 'horizontal' : 'default'} />
      ))}
    </div>
  );
}

// =============================================================================
// MAIN PAGE COMPONENT
// =============================================================================

export default function VehiculosClient() {
  const router = useRouter();
  const searchParams = useSearchParams();

  // View and UI state
  const [viewMode, setViewMode] = React.useState<'grid' | 'list'>('grid');
  const [showMobileFilters, setShowMobileFilters] = React.useState(false);

  // Search state
  const [searchQuery, setSearchQuery] = React.useState(searchParams.get('q') || '');
  const [sortBy, setSortBy] = React.useState(searchParams.get('sort') || 'relevance');
  const [currentPage, setCurrentPage] = React.useState(
    parseInt(searchParams.get('page') || '1', 10)
  );
  const [activeFilters, setActiveFilters] = React.useState<ActiveFilter[]>(() => {
    const filters: ActiveFilter[] = [];
    const make = searchParams.get('make');
    const bodyType = searchParams.get('bodyType');
    const transmission = searchParams.get('transmission');
    const fuelType = searchParams.get('fuelType');

    if (make) filters.push({ key: 'make', value: make, label: `Marca: ${make}` });
    if (bodyType) filters.push({ key: 'bodyType', value: bodyType, label: `Tipo: ${bodyType}` });
    if (transmission)
      filters.push({ key: 'transmission', value: transmission, label: `Trans: ${transmission}` });
    if (fuelType)
      filters.push({ key: 'fuelType', value: fuelType, label: `Combustible: ${fuelType}` });

    return filters;
  });

  // Selected filter values for controlled selects
  const [selectedMake, setSelectedMake] = React.useState(searchParams.get('make') || '');
  const [selectedBodyType, setSelectedBodyType] = React.useState(
    searchParams.get('bodyType') || ''
  );
  const [selectedTransmission, setSelectedTransmission] = React.useState(
    searchParams.get('transmission') || ''
  );
  const [selectedFuelType, setSelectedFuelType] = React.useState(
    searchParams.get('fuelType') || ''
  );

  // Favorites — uses useFavorites hook (authenticated: API, unauthenticated: localStorage)
  const { isFavorite, toggleFavorite } = useFavorites();

  // Build search params for API
  const apiSearchParams = React.useMemo(
    () => buildSearchParams(searchQuery, activeFilters, sortBy, currentPage),
    [searchQuery, activeFilters, sortBy, currentPage]
  );

  // Fetch vehicles from API
  const {
    data: vehicleData,
    isLoading,
    isError,
    error,
    refetch,
  } = useVehicleSearch(apiSearchParams);

  // Fetch catalog data for filters
  const { data: makes = [] } = useMakes();
  const { data: bodyTypes = [] } = useBodyTypes();
  const { data: fuelTypes = [] } = useFuelTypes();
  const { data: transmissions = [] } = useTransmissions();

  // Derived data
  const vehicles = vehicleData?.items || [];
  const pagination = vehicleData?.pagination;
  const totalCount = pagination?.totalItems || 0;
  const totalPages = pagination?.totalPages || 1;

  // Update URL when filters change
  React.useEffect(() => {
    const params = new URLSearchParams();

    if (searchQuery) params.set('q', searchQuery);
    if (sortBy !== 'relevance') params.set('sort', sortBy);
    if (currentPage > 1) params.set('page', currentPage.toString());

    activeFilters.forEach(filter => {
      params.set(filter.key, filter.value);
    });

    const queryString = params.toString();
    const newUrl = queryString ? `/vehiculos?${queryString}` : '/vehiculos';

    // Only update if different
    if (window.location.pathname + window.location.search !== newUrl) {
      router.replace(newUrl, { scroll: false });
    }
  }, [searchQuery, sortBy, currentPage, activeFilters, router]);

  // Handlers
  const handleFavoriteToggle = (vehicleId: string) => {
    toggleFavorite(vehicleId).catch(() => {
      // Error handled by useFavorites hook
    });
  };

  const addFilter = (key: string, value: string, label: string) => {
    if (!activeFilters.find(f => f.key === key)) {
      setActiveFilters(prev => [...prev.filter(f => f.key !== key), { key, value, label }]);
      setCurrentPage(1); // Reset to first page
    }
  };

  const removeFilter = (key: string) => {
    setActiveFilters(prev => prev.filter(f => f.key !== key));
    // Reset the corresponding select
    if (key === 'make') setSelectedMake('');
    if (key === 'bodyType') setSelectedBodyType('');
    if (key === 'transmission') setSelectedTransmission('');
    if (key === 'fuelType') setSelectedFuelType('');
    setCurrentPage(1);
  };

  const clearAllFilters = () => {
    setActiveFilters([]);
    setSelectedMake('');
    setSelectedBodyType('');
    setSelectedTransmission('');
    setSelectedFuelType('');
    setSearchQuery('');
    setCurrentPage(1);
  };

  const handlePageChange = (page: number) => {
    setCurrentPage(page);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  // Debounced search
  const [debouncedSearch, setDebouncedSearch] = React.useState(searchQuery);

  React.useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(searchQuery);
      setCurrentPage(1);
    }, 300);
    return () => clearTimeout(timer);
  }, [searchQuery]);

  // Use debounced value for API
  const finalSearchParams = React.useMemo(
    () => buildSearchParams(debouncedSearch, activeFilters, sortBy, currentPage),
    [debouncedSearch, activeFilters, sortBy, currentPage]
  );

  // Refetch with debounced params
  const { data: debouncedData, isLoading: isDebouncedLoading } =
    useVehicleSearch(finalSearchParams);

  const displayVehicles = debouncedData?.items || vehicles;
  const displayPagination = debouncedData?.pagination || pagination;
  const displayTotal = displayPagination?.totalItems || 0;
  const displayTotalPages = displayPagination?.totalPages || 1;
  const showLoading = isLoading || isDebouncedLoading;

  return (
    <div className="bg-muted/50 min-h-screen">
      {/* Search Header */}
      <div className="border-border bg-card border-b">
        <div className="mx-auto max-w-7xl px-4 py-4 sm:px-6 lg:px-8">
          <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
            {/* Search Input */}
            <div className="relative max-w-xl flex-1">
              <Search className="text-muted-foreground absolute top-1/2 left-3 h-5 w-5 -translate-y-1/2" />
              <Input
                type="search"
                placeholder="Buscar por marca, modelo, año..."
                value={searchQuery}
                onChange={e => setSearchQuery(e.target.value)}
                className="pl-10"
              />
            </div>

            {/* Actions */}
            <div className="flex items-center gap-2">
              {/* Mobile Filter Toggle */}
              <Button
                variant="outline"
                size="sm"
                onClick={() => setShowMobileFilters(!showMobileFilters)}
                className="md:hidden"
              >
                <SlidersHorizontal className="mr-2 h-4 w-4" />
                Filtros
              </Button>

              {/* Sort */}
              <Select
                value={sortBy}
                onValueChange={value => {
                  setSortBy(value);
                  setCurrentPage(1);
                }}
              >
                <SelectTrigger className="w-[180px]">
                  <SelectValue placeholder="Ordenar por" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="relevance">Relevancia</SelectItem>
                  <SelectItem value="price-asc">Precio: Menor a Mayor</SelectItem>
                  <SelectItem value="price-desc">Precio: Mayor a Menor</SelectItem>
                  <SelectItem value="year-desc">Más Recientes</SelectItem>
                  <SelectItem value="mileage-asc">Menor Kilometraje</SelectItem>
                </SelectContent>
              </Select>

              {/* View Mode Toggle */}
              <div className="border-border bg-muted/50 hidden items-center gap-1 rounded-lg border p-1 md:flex">
                <button
                  onClick={() => setViewMode('grid')}
                  className={`rounded-md p-2 transition-colors ${
                    viewMode === 'grid'
                      ? 'bg-white text-[#00A870] shadow-sm'
                      : 'text-muted-foreground hover:text-foreground'
                  }`}
                  aria-label="Vista de cuadrícula"
                >
                  <Grid3X3 className="h-4 w-4" />
                </button>
                <button
                  onClick={() => setViewMode('list')}
                  className={`rounded-md p-2 transition-colors ${
                    viewMode === 'list'
                      ? 'bg-white text-[#00A870] shadow-sm'
                      : 'text-muted-foreground hover:text-foreground'
                  }`}
                  aria-label="Vista de lista"
                >
                  <List className="h-4 w-4" />
                </button>
              </div>
            </div>
          </div>

          {/* Desktop Filters Row */}
          <div className="mt-4 hidden gap-3 md:flex">
            <Select
              value={selectedMake}
              onValueChange={value => {
                setSelectedMake(value);
                addFilter('make', value, `Marca: ${value}`);
              }}
            >
              <SelectTrigger className="w-[150px]">
                <SelectValue placeholder="Marca" />
              </SelectTrigger>
              <SelectContent>
                {makes.map(make => (
                  <SelectItem key={make.id} value={make.name}>
                    {make.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>

            <Select
              value={selectedBodyType}
              onValueChange={value => {
                setSelectedBodyType(value);
                addFilter('bodyType', value, `Tipo: ${value}`);
              }}
            >
              <SelectTrigger className="w-[150px]">
                <SelectValue placeholder="Tipo" />
              </SelectTrigger>
              <SelectContent>
                {bodyTypes.map(type => (
                  <SelectItem key={type.value} value={type.value}>
                    {type.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>

            <Select
              value={selectedTransmission}
              onValueChange={value => {
                setSelectedTransmission(value);
                addFilter('transmission', value, `Trans: ${value}`);
              }}
            >
              <SelectTrigger className="w-[150px]">
                <SelectValue placeholder="Transmisión" />
              </SelectTrigger>
              <SelectContent>
                {transmissions.map(trans => (
                  <SelectItem key={trans.value} value={trans.value}>
                    {trans.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>

            <Select
              value={selectedFuelType}
              onValueChange={value => {
                setSelectedFuelType(value);
                addFilter('fuelType', value, `Combustible: ${value}`);
              }}
            >
              <SelectTrigger className="w-[150px]">
                <SelectValue placeholder="Combustible" />
              </SelectTrigger>
              <SelectContent>
                {fuelTypes.map(fuel => (
                  <SelectItem key={fuel.value} value={fuel.value}>
                    {fuel.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>

            <Button variant="ghost" size="sm" className="text-[#00A870]">
              <SlidersHorizontal className="mr-2 h-4 w-4" />
              Más filtros
            </Button>
          </div>

          {/* Mobile Filters */}
          {showMobileFilters && (
            <div className="mt-4 grid grid-cols-2 gap-3 md:hidden">
              <Select
                value={selectedMake}
                onValueChange={value => {
                  setSelectedMake(value);
                  addFilter('make', value, `Marca: ${value}`);
                }}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Marca" />
                </SelectTrigger>
                <SelectContent>
                  {makes.map(make => (
                    <SelectItem key={make.id} value={make.name}>
                      {make.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>

              <Select
                value={selectedBodyType}
                onValueChange={value => {
                  setSelectedBodyType(value);
                  addFilter('bodyType', value, `Tipo: ${value}`);
                }}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Tipo" />
                </SelectTrigger>
                <SelectContent>
                  {bodyTypes.map(type => (
                    <SelectItem key={type.value} value={type.value}>
                      {type.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>

              <Select
                value={selectedTransmission}
                onValueChange={value => {
                  setSelectedTransmission(value);
                  addFilter('transmission', value, `Trans: ${value}`);
                }}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Transmisión" />
                </SelectTrigger>
                <SelectContent>
                  {transmissions.map(trans => (
                    <SelectItem key={trans.value} value={trans.value}>
                      {trans.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>

              <Select
                value={selectedFuelType}
                onValueChange={value => {
                  setSelectedFuelType(value);
                  addFilter('fuelType', value, `Combustible: ${value}`);
                }}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Combustible" />
                </SelectTrigger>
                <SelectContent>
                  {fuelTypes.map(fuel => (
                    <SelectItem key={fuel.value} value={fuel.value}>
                      {fuel.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          )}

          {/* Active Filters */}
          {activeFilters.length > 0 && (
            <div className="mt-4 flex flex-wrap items-center gap-2">
              <span className="text-muted-foreground text-sm">Filtros activos:</span>
              {activeFilters.map(filter => (
                <Badge
                  key={`${filter.key}-${filter.value}`}
                  variant="secondary"
                  className="flex items-center gap-1 pl-3"
                >
                  {filter.label}
                  <button
                    onClick={() => removeFilter(filter.key)}
                    className="hover:bg-muted ml-1 rounded-full p-0.5"
                  >
                    <X className="h-3 w-3" />
                  </button>
                </Badge>
              ))}
              <Button
                variant="ghost"
                size="sm"
                onClick={clearAllFilters}
                className="text-red-600 hover:text-red-700"
              >
                Limpiar todo
              </Button>
            </div>
          )}
        </div>
      </div>

      {/* Results Section */}
      <div className="mx-auto max-w-7xl px-4 py-6 sm:px-6 lg:px-8">
        {/* Error State */}
        {isError && (
          <div className="flex flex-col items-center justify-center py-16 text-center">
            <div className="mb-4 rounded-full bg-red-100 p-4">
              <AlertCircle className="h-8 w-8 text-red-500" />
            </div>
            <h3 className="text-foreground text-lg font-semibold">Error al cargar vehículos</h3>
            <p className="text-muted-foreground mt-1">
              {error instanceof Error ? error.message : 'Ocurrió un error inesperado'}
            </p>
            <Button variant="outline" className="mt-4" onClick={() => refetch()}>
              <RefreshCcw className="mr-2 h-4 w-4" />
              Reintentar
            </Button>
          </div>
        )}

        {/* Results Count */}
        {!isError && (
          <div className="mb-4 flex items-center justify-between">
            <p className="text-muted-foreground text-sm">
              <span className="text-foreground font-semibold">{displayTotal.toLocaleString()}</span>{' '}
              vehículos encontrados
            </p>
          </div>
        )}

        {/* Vehicle Grid/List */}
        {!isError && showLoading ? (
          <VehiclesPageSkeleton viewMode={viewMode} />
        ) : !isError && displayVehicles.length > 0 ? (
          <div
            className={
              viewMode === 'grid'
                ? 'grid gap-6 sm:grid-cols-2 lg:grid-cols-3'
                : 'flex flex-col gap-4'
            }
          >
            {displayVehicles.map((vehicle: VehicleCardData, index: number) => (
              <VehicleCard
                key={vehicle.id}
                vehicle={vehicle}
                variant={viewMode === 'list' ? 'horizontal' : 'default'}
                isFavorite={isFavorite(vehicle.id)}
                onFavoriteClick={handleFavoriteToggle}
                priority={index < 3}
              />
            ))}
          </div>
        ) : !isError && !showLoading ? (
          <div className="flex flex-col items-center justify-center py-16 text-center">
            <div className="bg-muted mb-4 rounded-full p-4">
              <Search className="text-muted-foreground h-8 w-8" />
            </div>
            <h3 className="text-foreground text-lg font-semibold">No encontramos resultados</h3>
            <p className="text-muted-foreground mt-1">
              Intenta ajustar los filtros o buscar con otros términos
            </p>
            <Button variant="outline" className="mt-4" onClick={clearAllFilters}>
              Limpiar filtros
            </Button>
          </div>
        ) : null}

        {/* Pagination */}
        {!isError && displayVehicles.length > 0 && displayTotalPages > 1 && (
          <div className="mt-8 flex items-center justify-center gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={currentPage === 1}
              onClick={() => handlePageChange(currentPage - 1)}
            >
              Anterior
            </Button>
            <div className="flex gap-1">
              {/* Show page numbers */}
              {Array.from({ length: Math.min(5, displayTotalPages) }, (_, i) => {
                let pageNum: number;
                if (displayTotalPages <= 5) {
                  pageNum = i + 1;
                } else if (currentPage <= 3) {
                  pageNum = i + 1;
                } else if (currentPage >= displayTotalPages - 2) {
                  pageNum = displayTotalPages - 4 + i;
                } else {
                  pageNum = currentPage - 2 + i;
                }
                return (
                  <Button
                    key={pageNum}
                    size="sm"
                    variant={currentPage === pageNum ? 'default' : 'ghost'}
                    className={
                      currentPage === pageNum ? 'bg-[#00A870] text-white hover:bg-[#008a5c]' : ''
                    }
                    onClick={() => handlePageChange(pageNum)}
                  >
                    {pageNum}
                  </Button>
                );
              })}
            </div>
            <Button
              variant="outline"
              size="sm"
              disabled={currentPage === displayTotalPages}
              onClick={() => handlePageChange(currentPage + 1)}
            >
              Siguiente
            </Button>
          </div>
        )}
      </div>
    </div>
  );
}
