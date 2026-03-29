/**
 * Compare Vehicles Page
 *
 * Allows users to compare up to 3 vehicles side by side
 *
 * Route: /comparar
 */

'use client';

import * as React from 'react';
import { Suspense } from 'react';
import Link from 'next/link';
import { useSearchParams, useRouter } from 'next/navigation';
import { X, Plus, ArrowLeft, Check, Minus, Car, Share2 } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import Image from 'next/image';
import { Card, CardContent } from '@/components/ui/card';
import { cn } from '@/lib/utils';
import { formatFuelType, formatTransmission } from '@/lib/format';
import { useVehicleDtosForComparison } from '@/hooks/use-vehicles';
import { useLocalComparison } from '@/hooks/use-comparisons';
import type { VehicleDto } from '@/services/vehicles';

// =============================================================================
// TYPES
// =============================================================================

type CompareVehicle = VehicleDto;

// Helper: get first image URL from vehicle
function getImageUrl(v: CompareVehicle): string {
  const sorted = [...(v.images || [])].sort((a, b) => (a.order ?? 99) - (b.order ?? 99));
  return sorted[0]?.url || '/placeholder-car.jpg';
}

// Helper: get location string
function getLocation(v: CompareVehicle): string {
  return [v.city, v.province].filter(Boolean).join(', ') || '—';
}

// Helper: get slug — must match backend GenerateSlug format: {year}-{make}-{model}-{shortId}
function getSlug(v: CompareVehicle): string {
  if (v.slug) return v.slug;
  const baseSlug = `${v.year}-${v.make}-${v.model}`
    .toLowerCase()
    .replace(/\s+/g, '-')
    .replace(/--+/g, '-');
  const shortId = v.id.replace(/-/g, '').slice(0, 8);
  return `${baseSlug}-${shortId}`;
}

// Spec comparison rows — reads DIRECTLY from VehicleDto fields
interface SpecRow {
  key: string;
  label: string;
  format: (v: CompareVehicle) => string;
  highlight?: 'min' | 'max';
  numericValue?: (v: CompareVehicle) => number | null;
}

const specRows: SpecRow[] = [
  {
    key: 'price',
    label: 'Precio',
    format: v => `RD$ ${v.price?.toLocaleString() ?? '—'}`,
    highlight: 'min',
    numericValue: v => v.price ?? null,
  },
  {
    key: 'year',
    label: 'Año',
    format: v => v.year?.toString() ?? '—',
    highlight: 'max',
    numericValue: v => v.year ?? null,
  },
  {
    key: 'mileage',
    label: 'Kilometraje',
    format: v => (v.mileage != null ? `${v.mileage.toLocaleString()} km` : '—'),
    highlight: 'min',
    numericValue: v => v.mileage ?? null,
  },
  {
    key: 'transmission',
    label: 'Transmisión',
    format: v => formatTransmission(v.transmission),
  },
  {
    key: 'fuelType',
    label: 'Combustible',
    format: v => formatFuelType(v.fuelType),
  },
  {
    key: 'engineSize',
    label: 'Motor',
    format: v => v.engineSize || '—',
  },
  {
    key: 'horsepower',
    label: 'Potencia',
    format: v => (v.horsepower ? `${v.horsepower} HP` : '—'),
    highlight: 'max',
    numericValue: v => v.horsepower ?? null,
  },
  {
    key: 'drivetrain',
    label: 'Tracción',
    format: v => v.drivetrain || '—',
  },
  {
    key: 'bodyType',
    label: 'Tipo',
    format: v => v.bodyType || '—',
  },
  {
    key: 'condition',
    label: 'Condición',
    format: v => {
      const c = v.condition?.toLowerCase();
      if (c === 'new' || c === '0') return 'Nuevo';
      if (c === 'certified' || c === 'certifiedpreowned') return 'Certificado';
      return 'Usado';
    },
  },
  {
    key: 'doors',
    label: 'Puertas',
    format: v => v.doors?.toString() || '—',
  },
  {
    key: 'seats',
    label: 'Asientos',
    format: v => v.seats?.toString() || '—',
  },
  {
    key: 'exteriorColor',
    label: 'Color Exterior',
    format: v => v.exteriorColor || '—',
  },
  {
    key: 'interiorColor',
    label: 'Color Interior',
    format: v => v.interiorColor || '—',
  },
  {
    key: 'location',
    label: 'Ubicación',
    format: v => getLocation(v),
  },
];

// =============================================================================
// COMPARE CONTENT COMPONENT (uses useSearchParams)
// =============================================================================

function CompareContent() {
  const router = useRouter();
  const searchParams = useSearchParams();

  // Get vehicle IDs from URL params or local storage
  const urlIds = searchParams.get('ids')?.split(',').filter(Boolean) || [];
  const localComparison = useLocalComparison();

  // Combine IDs (URL takes priority)
  const vehicleIds = urlIds.length > 0 ? urlIds : localComparison.vehicleIds;

  // Fetch FULL VehicleDto data (not stripped VehicleCardData)
  const { data: vehiclesData, isLoading, isError } = useVehicleDtosForComparison(vehicleIds);

  // Use the full VehicleDto directly — no stripping of specs
  const vehicles: CompareVehicle[] = React.useMemo(() => {
    return vehiclesData ?? [];
  }, [vehiclesData]);

  // Track comparison analytics when vehicles load
  React.useEffect(() => {
    if (vehicles.length >= 2) {
      import('@/components/analytics/google-analytics')
        .then(({ trackComparison }) => {
          trackComparison(
            vehicles.map(v => ({
              id: v.id,
              make: v.make || '',
              model: v.model || '',
            }))
          );
        })
        .catch(() => {
          /* analytics not critical */
        });
    }
  }, [vehicles]);

  const removeVehicle = (id: string) => {
    localComparison.removeVehicle(id);
    // Update URL if using URL params
    if (urlIds.length > 0) {
      const newIds = urlIds.filter(uid => uid !== id);
      if (newIds.length > 0) {
        router.replace(`/comparar?ids=${newIds.join(',')}`);
      } else {
        router.replace('/comparar');
      }
    }
  };

  const clearAll = () => {
    localComparison.clearAll();
    router.replace('/comparar');
  };

  const shareComparison = async () => {
    const ids = vehicles.map(v => v.id).join(',');
    const url = `${window.location.origin}/comparar?ids=${ids}`;
    try {
      await navigator.clipboard.writeText(url);
      toast.success('Enlace copiado al portapapeles');
    } catch {
      toast.error('No se pudo copiar el enlace');
    }
  };

  // Get unique features across all vehicles
  const featuresInComparison = React.useMemo(() => {
    const allVehicleFeatures = new Set<string>();
    vehicles.forEach(v => v.features?.forEach(f => allVehicleFeatures.add(f)));
    return Array.from(allVehicleFeatures).sort();
  }, [vehicles]);

  // Compute which cell is the "best" for highlighted rows
  const highlightMap = React.useMemo(() => {
    const map: Record<string, string | null> = {};
    for (const row of specRows) {
      if (!row.highlight || !row.numericValue || vehicles.length < 2) {
        map[row.key] = null;
        continue;
      }
      let bestId: string | null = null;
      let bestVal: number | null = null;
      for (const v of vehicles) {
        const val = row.numericValue(v);
        if (val == null) continue;
        if (bestVal == null) {
          bestVal = val;
          bestId = v.id;
        } else if (row.highlight === 'min' && val < bestVal) {
          bestVal = val;
          bestId = v.id;
        } else if (row.highlight === 'max' && val > bestVal) {
          bestVal = val;
          bestId = v.id;
        }
      }
      // Only highlight if there are at least 2 non-null values and they differ
      const nonNull = vehicles.filter(v => row.numericValue!(v) != null);
      const allSame = nonNull.length > 1 && nonNull.every(v => row.numericValue!(v) === bestVal);
      map[row.key] = allSame ? null : bestId;
    }
    return map;
  }, [vehicles]);

  if (isLoading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <div className="text-center">
          <div className="border-primary mx-auto h-8 w-8 animate-spin rounded-full border-4 border-t-transparent" />
          <p className="text-muted-foreground mt-2">Cargando comparación...</p>
        </div>
      </div>
    );
  }

  if (isError) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <div className="text-center">
          <p className="text-destructive mb-2 font-semibold">Error al cargar los vehículos</p>
          <p className="text-muted-foreground text-sm">
            No se pudieron obtener los datos para la comparación.
          </p>
        </div>
      </div>
    );
  }

  // Empty state
  if (vehicles.length === 0) {
    return (
      <div className="bg-muted/50 min-h-screen py-8">
        <div className="container mx-auto px-4">
          <Link
            href="/vehiculos"
            className="text-muted-foreground hover:text-foreground inline-flex items-center gap-1 text-sm"
          >
            <ArrowLeft className="h-4 w-4" />
            Volver a vehículos
          </Link>

          <div className="mt-8 flex min-h-[50vh] flex-col items-center justify-center text-center">
            <div className="bg-muted mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full">
              <Car className="text-muted-foreground h-8 w-8" />
            </div>
            <h1 className="text-foreground text-2xl font-bold">Sin vehículos para comparar</h1>
            <p className="text-muted-foreground mt-2 max-w-md">
              Agrega vehículos a la comparación desde la página de búsqueda o detalle de vehículo.
            </p>
            <Button asChild className="bg-primary hover:bg-primary/90 mt-6 gap-2">
              <Link href="/vehiculos">
                <Car className="h-4 w-4" />
                Explorar vehículos
              </Link>
            </Button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-muted/50 min-h-screen py-8">
      <div className="container mx-auto px-4">
        {/* Header */}
        <div className="mb-6 flex items-center justify-between">
          <div>
            <Link
              href="/vehiculos"
              className="text-muted-foreground hover:text-foreground inline-flex items-center gap-1 text-sm"
            >
              <ArrowLeft className="h-4 w-4" />
              Volver
            </Link>
            <h1 className="text-foreground mt-2 text-2xl font-bold">Comparar vehículos</h1>
            <p className="text-muted-foreground">
              {vehicles.length} de 3 vehículos • Compara especificaciones y características
            </p>
          </div>

          <div className="flex gap-2">
            <Button variant="outline" onClick={shareComparison} className="gap-2">
              <Share2 className="h-4 w-4" />
              Compartir
            </Button>
            <Button
              variant="outline"
              onClick={clearAll}
              className="gap-2 text-red-600 hover:bg-red-50"
            >
              <X className="h-4 w-4" />
              Limpiar
            </Button>
          </div>
        </div>

        {/* Vehicle Cards Header — responsive with horizontal scroll on mobile */}
        <div className="-mx-4 mb-6 overflow-x-auto px-4 pb-2 sm:mx-0 sm:overflow-visible sm:px-0 sm:pb-0">
          <div
            className="grid gap-4"
            style={{
              gridTemplateColumns: `repeat(${Math.min(vehicles.length + 1, 4)}, minmax(220px, 1fr))`,
              minWidth: vehicles.length > 1 ? `${(vehicles.length + 1) * 220}px` : undefined,
            }}
          >
            {/* Empty first column for labels (hidden on mobile) */}
            <div className="hidden lg:block" />

            {/* Vehicle cards */}
            {vehicles.map(vehicle => (
              <Card key={vehicle.id} className="relative overflow-hidden">
                <button
                  onClick={() => removeVehicle(vehicle.id)}
                  className="absolute top-2 right-2 z-10 flex h-6 w-6 items-center justify-center rounded-full bg-black/50 text-white transition-colors hover:bg-black/70"
                >
                  <X className="h-4 w-4" />
                </button>

                <div className="bg-muted relative aspect-[16/10] overflow-hidden">
                  <Image
                    src={getImageUrl(vehicle)}
                    alt={`${vehicle.year} ${vehicle.make} ${vehicle.model}`}
                    fill
                    className="object-cover"
                    sizes="(max-width: 768px) 80vw, 33vw"
                  />
                </div>

                <CardContent className="p-4">
                  <h2 className="text-foreground font-bold">
                    {vehicle.year} {vehicle.make} {vehicle.model}
                  </h2>
                  {vehicle.trim && <p className="text-muted-foreground text-sm">{vehicle.trim}</p>}
                  <p className="text-primary mt-2 text-xl font-bold">
                    RD$ {vehicle.price?.toLocaleString()}
                  </p>

                  <div className="mt-4">
                    <Button asChild variant="outline" size="sm" className="w-full">
                      <Link href={`/vehiculos/${getSlug(vehicle)}`}>Ver detalle</Link>
                    </Button>
                  </div>
                </CardContent>
              </Card>
            ))}

            {/* Add vehicle card */}
            {vehicles.length < 3 && (
              <Card className="hover:border-primary hover:bg-primary/5 flex min-h-[300px] cursor-pointer items-center justify-center border-2 border-dashed transition-colors">
                <Link href="/vehiculos" className="flex flex-col items-center p-6 text-center">
                  <div className="bg-primary/10 flex h-12 w-12 items-center justify-center rounded-full">
                    <Plus className="text-primary h-6 w-6" />
                  </div>
                  <p className="text-foreground mt-2 font-medium">Agregar vehículo</p>
                  <p className="text-muted-foreground text-sm">Hasta 3 vehículos</p>
                </Link>
              </Card>
            )}
          </div>
        </div>

        {/* Specifications Table — mobile scrollable */}
        <Card className="mb-6 overflow-hidden">
          <div className="border-border bg-muted/50 border-b px-4 py-3">
            <h3 className="text-foreground font-semibold">Especificaciones</h3>
          </div>

          <div className="divide-y overflow-x-auto">
            {specRows.map(row => {
              const bestId = highlightMap[row.key];
              return (
                <div
                  key={row.key}
                  className="grid gap-4 p-4"
                  style={{
                    gridTemplateColumns: `minmax(140px, 180px) repeat(${vehicles.length}, minmax(140px, 1fr))`,
                  }}
                >
                  <div className="text-muted-foreground text-sm font-medium">{row.label}</div>
                  {vehicles.map(vehicle => {
                    const isBest = bestId === vehicle.id;
                    return (
                      <div
                        key={vehicle.id}
                        className={cn(
                          'text-foreground text-sm font-medium',
                          isBest &&
                            'rounded-md bg-green-50 px-2 py-0.5 text-green-700 dark:bg-green-950 dark:text-green-300'
                        )}
                      >
                        {row.format(vehicle)}
                      </div>
                    );
                  })}
                </div>
              );
            })}
          </div>
        </Card>

        {/* Features Comparison */}
        {featuresInComparison.length > 0 && (
          <Card className="overflow-hidden">
            <div className="border-border bg-muted/50 border-b px-4 py-3">
              <h3 className="text-foreground font-semibold">Características</h3>
            </div>

            <div className="divide-y overflow-x-auto">
              {featuresInComparison.map(feature => (
                <div
                  key={feature}
                  className="grid gap-4 p-4"
                  style={{
                    gridTemplateColumns: `minmax(140px, 180px) repeat(${vehicles.length}, minmax(140px, 1fr))`,
                  }}
                >
                  <div className="text-muted-foreground text-sm font-medium">{feature}</div>
                  {vehicles.map(vehicle => (
                    <div key={vehicle.id} className="flex items-center">
                      {vehicle.features?.includes(feature) ? (
                        <Check className="h-5 w-5 text-green-500" />
                      ) : (
                        <Minus className="h-5 w-5 text-gray-300" />
                      )}
                    </div>
                  ))}
                </div>
              ))}
            </div>
          </Card>
        )}
      </div>
    </div>
  );
}

// =============================================================================
// LOADING FALLBACK
// =============================================================================

function CompareLoading() {
  return (
    <div className="flex min-h-[60vh] items-center justify-center">
      <div className="text-center">
        <div className="border-primary mx-auto h-8 w-8 animate-spin rounded-full border-4 border-t-transparent" />
        <p className="text-muted-foreground mt-2">Cargando comparación...</p>
      </div>
    </div>
  );
}

// =============================================================================
// MAIN PAGE COMPONENT WITH SUSPENSE
// =============================================================================

export default function CompararPage() {
  return (
    <Suspense fallback={<CompareLoading />}>
      <CompareContent />
    </Suspense>
  );
}
