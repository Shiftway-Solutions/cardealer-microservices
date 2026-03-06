'use client';

import { useState, useEffect, useMemo } from 'react';
import { VehicleCard } from '@/components/ui/vehicle-card';
import { Badge } from '@/components/ui/badge';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Separator } from '@/components/ui/separator';
import { Car, Filter, ArrowRight, Loader2 } from 'lucide-react';
import Link from 'next/link';
import type { VehicleCardData } from '@/types';

interface BrandVehiclesClientProps {
  brand: string;
  brandName: string;
}

interface VehiclesResponse {
  items?: VehicleCardData[];
  data?: VehicleCardData[];
  totalCount?: number;
  total?: number;
}

export function BrandVehiclesClient({ brand, brandName }: BrandVehiclesClientProps) {
  const [vehicles, setVehicles] = useState<VehicleCardData[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [sortBy, setSortBy] = useState<string>('recent');
  const [totalCount, setTotalCount] = useState(0);

  useEffect(() => {
    async function fetchVehicles() {
      try {
        setLoading(true);
        const params = new URLSearchParams({
          make: brand,
          pageSize: '24',
          page: '1',
        });

        // Add sort parameter
        if (sortBy === 'price-asc') params.set('sortBy', 'price');
        else if (sortBy === 'price-desc') params.set('sortBy', 'price_desc');
        else if (sortBy === 'year') params.set('sortBy', 'year_desc');
        else params.set('sortBy', 'newest');

        const res = await fetch(`/api/vehicles?${params.toString()}`);
        if (!res.ok) throw new Error('Error al cargar vehículos');

        const data: VehiclesResponse = await res.json();
        const items = data.items || data.data || [];
        setVehicles(items);
        setTotalCount(data.totalCount || data.total || items.length);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Error desconocido');
      } finally {
        setLoading(false);
      }
    }

    fetchVehicles();
  }, [brand, sortBy]);

  // Extract popular models
  const popularModels = useMemo(() => {
    const modelCounts = new Map<string, number>();
    vehicles.forEach(v => {
      const model = v.model;
      if (model) {
        modelCounts.set(model, (modelCounts.get(model) || 0) + 1);
      }
    });
    return Array.from(modelCounts.entries())
      .sort((a, b) => b[1] - a[1])
      .slice(0, 10);
  }, [vehicles]);

  // Price range
  const priceRange = useMemo(() => {
    if (vehicles.length === 0) return null;
    const prices = vehicles.filter(v => v.price > 0).map(v => v.price);
    if (prices.length === 0) return null;
    return {
      min: Math.min(...prices),
      max: Math.max(...prices),
      avg: prices.reduce((a, b) => a + b, 0) / prices.length,
    };
  }, [vehicles]);

  const formatPrice = (price: number) =>
    new Intl.NumberFormat('es-DO', {
      style: 'currency',
      currency: 'DOP',
      maximumFractionDigits: 0,
    }).format(price);

  if (loading) {
    return (
      <div className="flex flex-col items-center justify-center py-20">
        <Loader2 className="text-primary h-8 w-8 animate-spin" />
        <p className="text-muted-foreground mt-4">Cargando vehículos {brandName}...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="border-destructive/30 bg-destructive/10 rounded-lg border p-8 text-center">
        <p className="text-destructive">{error}</p>
        <button
          onClick={() => window.location.reload()}
          className="bg-primary text-primary-foreground mt-4 rounded-lg px-4 py-2 text-sm"
        >
          Reintentar
        </button>
      </div>
    );
  }

  return (
    <div className="space-y-8">
      {/* Stats Bar */}
      <div className="bg-card flex flex-wrap items-center gap-4 rounded-lg border p-4">
        <div className="flex items-center gap-2">
          <Car className="text-primary h-5 w-5" />
          <span className="font-semibold">
            {totalCount} vehículos {brandName}
          </span>
        </div>
        {priceRange && (
          <>
            <Separator orientation="vertical" className="h-6" />
            <div className="text-muted-foreground text-sm">
              Desde{' '}
              <span className="text-foreground font-medium">{formatPrice(priceRange.min)}</span>{' '}
              hasta{' '}
              <span className="text-foreground font-medium">{formatPrice(priceRange.max)}</span>
            </div>
            <Separator orientation="vertical" className="h-6" />
            <div className="text-muted-foreground text-sm">
              Promedio:{' '}
              <span className="text-foreground font-medium">{formatPrice(priceRange.avg)}</span>
            </div>
          </>
        )}
        <div className="ml-auto flex items-center gap-2">
          <Filter className="text-muted-foreground h-4 w-4" />
          <Select value={sortBy} onValueChange={setSortBy}>
            <SelectTrigger className="w-[180px]">
              <SelectValue placeholder="Ordenar por" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="recent">Más recientes</SelectItem>
              <SelectItem value="price-asc">Menor precio</SelectItem>
              <SelectItem value="price-desc">Mayor precio</SelectItem>
              <SelectItem value="year">Más nuevo</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>

      {/* Popular Models */}
      {popularModels.length > 0 && (
        <div>
          <h2 className="mb-3 text-lg font-semibold">Modelos Populares</h2>
          <div className="flex flex-wrap gap-2">
            {popularModels.map(([model, count]) => (
              <Link
                key={model}
                href={`/vehiculos?make=${brand}&model=${encodeURIComponent(model)}`}
              >
                <Badge
                  variant="secondary"
                  className="hover:bg-primary hover:text-primary-foreground cursor-pointer px-3 py-1.5 transition-colors"
                >
                  {model} <span className="ml-1 text-xs opacity-70">({count})</span>
                </Badge>
              </Link>
            ))}
          </div>
        </div>
      )}

      {/* Vehicle Grid */}
      {vehicles.length > 0 ? (
        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {vehicles.map((vehicle, index) => (
            <VehicleCard key={vehicle.id} vehicle={vehicle} priority={index < 6} />
          ))}
        </div>
      ) : (
        <div className="rounded-lg border p-12 text-center">
          <Car className="text-muted-foreground/40 mx-auto h-12 w-12" />
          <p className="mt-4 text-lg font-medium">
            No hay vehículos {brandName} disponibles en este momento
          </p>
          <p className="text-muted-foreground mt-2 text-sm">
            Vuelve pronto, se agregan nuevos vehículos constantemente.
          </p>
        </div>
      )}

      {/* CTA */}
      {totalCount > 24 && (
        <div className="text-center">
          <Link
            href={`/vehiculos?make=${brand}`}
            className="bg-primary text-primary-foreground hover:bg-primary/90 inline-flex items-center gap-2 rounded-lg px-6 py-3 text-sm font-medium transition-colors"
          >
            Ver todos los {brandName} ({totalCount})
            <ArrowRight className="h-4 w-4" />
          </Link>
        </div>
      )}
    </div>
  );
}
