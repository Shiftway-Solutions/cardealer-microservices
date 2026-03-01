/**
 * Favorites Page
 *
 * Displays user's saved/favorite vehicles with API integration
 *
 * Route: /cuenta/favoritos
 */

'use client';

import * as React from 'react';
import Link from 'next/link';
import Image from 'next/image';
import {
  Heart,
  Trash2,
  MapPin,
  Calendar,
  Gauge,
  Search,
  Car,
  Loader2,
  Bell,
  ArrowDownAZ,
  RefreshCw,
  AlertCircle,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Switch } from '@/components/ui/switch';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog';
import { cn } from '@/lib/utils';
import { toast } from 'sonner';
import { useFavorites } from '@/hooks/use-favorites';
import type { FavoriteVehicle } from '@/services/favorites';

// =============================================================================
// LOADING STATE
// =============================================================================

function FavoritesLoading() {
  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <Skeleton className="mb-2 h-8 w-32" />
          <Skeleton className="h-4 w-48" />
        </div>
      </div>
      <div className="space-y-4">
        {[1, 2, 3].map(i => (
          <Card key={i} className="overflow-hidden">
            <div className="flex flex-col sm:flex-row">
              <Skeleton className="h-40 w-full flex-shrink-0 sm:h-auto sm:w-56" />
              <div className="flex-1 p-4">
                <Skeleton className="mb-2 h-6 w-2/3" />
                <Skeleton className="mb-4 h-8 w-1/3" />
                <div className="flex gap-4">
                  <Skeleton className="h-4 w-16" />
                  <Skeleton className="h-4 w-20" />
                  <Skeleton className="h-4 w-24" />
                </div>
                <div className="mt-4 flex gap-2">
                  <Skeleton className="h-9 w-28" />
                  <Skeleton className="h-9 w-28" />
                </div>
              </div>
            </div>
          </Card>
        ))}
      </div>
    </div>
  );
}

// =============================================================================
// ERROR STATE
// =============================================================================

function FavoritesError({ error, onRetry }: { error: Error | null; onRetry: () => void }) {
  return (
    <Card className="mx-auto max-w-md">
      <CardContent className="flex flex-col items-center py-12 text-center">
        <AlertCircle className="mb-4 h-12 w-12 text-red-400" />
        <h3 className="mb-2 font-semibold">Error al cargar favoritos</h3>
        <p className="text-muted-foreground mb-6 text-sm">
          {error?.message || 'No se pudieron cargar tus vehículos guardados'}
        </p>
        <Button onClick={onRetry}>
          <RefreshCw className="mr-2 h-4 w-4" />
          Reintentar
        </Button>
      </CardContent>
    </Card>
  );
}

// =============================================================================
// EMPTY STATE
// =============================================================================

function EmptyState() {
  return (
    <div className="py-16 text-center">
      <div className="mx-auto mb-4 flex h-20 w-20 items-center justify-center rounded-full bg-red-50">
        <Heart className="h-10 w-10 text-red-400" />
      </div>
      <h3 className="text-foreground mb-2 text-lg font-medium">No tienes favoritos</h3>
      <p className="text-muted-foreground mx-auto mb-8 max-w-sm">
        Guarda vehículos que te interesen haciendo clic en el corazón para encontrarlos fácilmente
        después.
      </p>
      <Button asChild size="lg">
        <Link href="/vehiculos">
          <Search className="mr-2 h-5 w-5" />
          Explorar vehículos
        </Link>
      </Button>
    </div>
  );
}

// =============================================================================
// FAVORITE CARD
// =============================================================================

function formatFavoritePrice(price: number) {
  return new Intl.NumberFormat('es-DO', {
    style: 'currency',
    currency: 'DOP',
    maximumFractionDigits: 0,
  }).format(price);
}

function formatMileage(km: number) {
  return new Intl.NumberFormat('es-DO').format(km) + ' km';
}

function timeAgo(date: string) {
  const seconds = Math.floor((Date.now() - new Date(date).getTime()) / 1000);
  if (seconds < 60) return 'hace un momento';
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `hace ${minutes} min`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `hace ${hours}h`;
  const days = Math.floor(hours / 24);
  return `hace ${days} días`;
}

interface FavoriteCardProps {
  favorite: FavoriteVehicle;
  onRemove: () => void;
  onToggleNotify: () => void;
  isRemoving: boolean;
}

function FavoriteCard({ favorite, onRemove, onToggleNotify, isRemoving }: FavoriteCardProps) {
  const { vehicle } = favorite;

  const isAvailable = vehicle.status === 'active';

  return (
    <Card className={cn('overflow-hidden transition-opacity', isRemoving && 'opacity-50')}>
      <div className="flex flex-col sm:flex-row">
        {/* Image */}
        <Link
          href={`/vehiculos/${vehicle.slug}`}
          className="bg-muted relative h-40 w-full flex-shrink-0 sm:h-auto sm:w-56"
        >
          {vehicle.imageUrl ? (
            <Image src={vehicle.imageUrl} alt={vehicle.title} fill className="object-cover" />
          ) : (
            <div className="flex h-full w-full items-center justify-center">
              <Car className="text-muted-foreground h-12 w-12" />
            </div>
          )}
          {!isAvailable && (
            <div className="absolute inset-0 flex items-center justify-center bg-black/50">
              <Badge variant="danger" className="text-sm">
                {vehicle.status === 'sold' ? 'Vendido' : 'No disponible'}
              </Badge>
            </div>
          )}
          {vehicle.priceChanged && vehicle.previousPrice && (
            <Badge className="bg-primary/100 absolute top-2 left-2">Precio reducido</Badge>
          )}
        </Link>

        {/* Content */}
        <CardContent className="flex-1 p-4">
          <div className="flex items-start justify-between gap-4">
            <div className="min-w-0 flex-1">
              <Link
                href={`/vehiculos/${vehicle.slug}`}
                className="hover:text-primary block transition-colors"
              >
                <h3 className="text-foreground truncate font-semibold">{vehicle.title}</h3>
              </Link>

              <div className="mt-1 flex items-center gap-2">
                <p className="text-primary text-xl font-bold">
                  {formatFavoritePrice(vehicle.price)}
                </p>
                {vehicle.previousPrice && vehicle.previousPrice > vehicle.price && (
                  <span className="text-muted-foreground text-sm line-through">
                    {formatFavoritePrice(vehicle.previousPrice)}
                  </span>
                )}
              </div>

              {/* Features */}
              <div className="text-muted-foreground mt-3 flex flex-wrap items-center gap-3 text-sm">
                <div className="flex items-center gap-1">
                  <Calendar className="h-4 w-4" />
                  <span>{vehicle.year}</span>
                </div>
                <div className="flex items-center gap-1">
                  <Gauge className="h-4 w-4" />
                  <span>{formatMileage(vehicle.mileage)}</span>
                </div>
                <span>{vehicle.transmission}</span>
                <span>{vehicle.fuelType}</span>
              </div>

              {/* Location */}
              <div className="text-muted-foreground mt-2 flex items-center gap-1 text-sm">
                <MapPin className="h-4 w-4" />
                <span>{vehicle.location}</span>
              </div>

              {/* Saved time & notifications */}
              <div className="mt-2 flex items-center justify-between">
                <p className="text-muted-foreground text-xs">
                  Guardado {timeAgo(favorite.createdAt)}
                </p>
                <div className="flex items-center gap-2">
                  <span className="text-muted-foreground text-xs">
                    {favorite.notifyOnPriceChange ? 'Notificaciones activas' : 'Sin notificaciones'}
                  </span>
                  <Switch
                    checked={favorite.notifyOnPriceChange}
                    onCheckedChange={onToggleNotify}
                    aria-label="Activar notificaciones de precio"
                  />
                </div>
              </div>
            </div>

            {/* Remove button */}
            <AlertDialog>
              <AlertDialogTrigger asChild>
                <Button
                  variant="ghost"
                  size="icon"
                  disabled={isRemoving}
                  className="text-red-500 hover:bg-red-50 hover:text-red-600"
                >
                  {isRemoving ? (
                    <Loader2 className="h-5 w-5 animate-spin" />
                  ) : (
                    <Trash2 className="h-5 w-5" />
                  )}
                </Button>
              </AlertDialogTrigger>
              <AlertDialogContent>
                <AlertDialogHeader>
                  <AlertDialogTitle>¿Eliminar de favoritos?</AlertDialogTitle>
                  <AlertDialogDescription>
                    "{vehicle.title}" será eliminado de tus favoritos. Puedes volver a agregarlo en
                    cualquier momento.
                  </AlertDialogDescription>
                </AlertDialogHeader>
                <AlertDialogFooter>
                  <AlertDialogCancel>Cancelar</AlertDialogCancel>
                  <AlertDialogAction onClick={onRemove} className="bg-red-600 hover:bg-red-700">
                    Eliminar
                  </AlertDialogAction>
                </AlertDialogFooter>
              </AlertDialogContent>
            </AlertDialog>
          </div>

          {/* Actions */}
          <div className="mt-4 flex gap-2">
            <Button asChild size="sm" className="flex-1" disabled={!isAvailable}>
              <Link href={`/vehiculos/${vehicle.slug}`}>Ver detalles</Link>
            </Button>
            <Button asChild variant="outline" size="sm" className="flex-1" disabled={!isAvailable}>
              <Link href={`/vehiculos/${vehicle.slug}#contactar`}>Contactar</Link>
            </Button>
          </div>
        </CardContent>
      </div>
    </Card>
  );
}

// =============================================================================
// MAIN PAGE
// =============================================================================

type SortOption = 'recent' | 'price-asc' | 'price-desc' | 'year-desc';

export default function FavoritesPage() {
  const { favorites, count, isLoading, error, removeFavorite, updateFavorite, isRemoving } =
    useFavorites();

  const [searchQuery, setSearchQuery] = React.useState('');
  const [sortBy, setSortBy] = React.useState<SortOption>('recent');
  const [removingId, setRemovingId] = React.useState<string | null>(null);

  // Handle remove
  const handleRemove = async (vehicleId: string) => {
    setRemovingId(vehicleId);
    try {
      await removeFavorite(vehicleId);
      toast.success('Eliminado de favoritos');
    } catch {
      toast.error('Error al eliminar');
    } finally {
      setRemovingId(null);
    }
  };

  // Handle toggle notifications
  const handleToggleNotify = async (vehicleId: string, currentValue: boolean) => {
    try {
      await updateFavorite(vehicleId, { notifyOnPriceChange: !currentValue });
      toast.success(
        currentValue ? 'Notificaciones desactivadas' : 'Te notificaremos cuando cambie el precio'
      );
    } catch {
      toast.error('Error al actualizar');
    }
  };

  // Filter and sort favorites
  const filteredFavorites = React.useMemo(() => {
    let result = [...favorites];

    // Apply search filter
    if (searchQuery) {
      const search = searchQuery.toLowerCase();
      result = result.filter(f => {
        const v = f.vehicle;
        return (
          v.title.toLowerCase().includes(search) ||
          v.make.toLowerCase().includes(search) ||
          v.model.toLowerCase().includes(search) ||
          v.location.toLowerCase().includes(search)
        );
      });
    }

    // Apply sort
    switch (sortBy) {
      case 'recent':
        result.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
        break;
      case 'price-asc':
        result.sort((a, b) => a.vehicle.price - b.vehicle.price);
        break;
      case 'price-desc':
        result.sort((a, b) => b.vehicle.price - a.vehicle.price);
        break;
      case 'year-desc':
        result.sort((a, b) => b.vehicle.year - a.vehicle.year);
        break;
    }

    return result;
  }, [favorites, searchQuery, sortBy]);

  // Loading state
  if (isLoading) {
    return <FavoritesLoading />;
  }

  // Error state - with manual refetch (need to handle this differently since useFavorites doesn't expose refetch)
  if (error) {
    return <FavoritesError error={error} onRetry={() => window.location.reload()} />;
  }

  // Empty state
  if (favorites.length === 0) {
    return <EmptyState />;
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-foreground text-2xl font-bold">Favoritos</h1>
          <p className="text-muted-foreground">
            {count} {count === 1 ? 'vehículo guardado' : 'vehículos guardados'}
          </p>
        </div>

        {/* Quick Stats */}
        <div className="flex gap-4 text-sm">
          <div className="text-muted-foreground flex items-center gap-1.5">
            <Bell className="h-4 w-4" />
            <span>{favorites.filter(f => f.notifyOnPriceChange).length} con alertas</span>
          </div>
          <div className="text-primary flex items-center gap-1.5">
            <span>{favorites.filter(f => f.vehicle.priceChanged).length} con precio reducido</span>
          </div>
        </div>
      </div>

      {/* Filters & Search */}
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
        <div className="relative flex-1">
          <Search className="text-muted-foreground absolute top-1/2 left-3 h-5 w-5 -translate-y-1/2" />
          <Input
            type="text"
            placeholder="Buscar en favoritos..."
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
            className="pl-10"
          />
        </div>
        <Select value={sortBy} onValueChange={v => setSortBy(v as SortOption)}>
          <SelectTrigger className="w-full sm:w-48">
            <ArrowDownAZ className="mr-2 h-4 w-4" />
            <SelectValue placeholder="Ordenar por" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="recent">Más recientes</SelectItem>
            <SelectItem value="price-asc">Precio: menor a mayor</SelectItem>
            <SelectItem value="price-desc">Precio: mayor a menor</SelectItem>
            <SelectItem value="year-desc">Año: más reciente</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {/* Favorites List */}
      {filteredFavorites.length > 0 ? (
        <div className="space-y-4">
          {filteredFavorites.map(favorite => (
            <FavoriteCard
              key={favorite.id}
              favorite={favorite}
              onRemove={() => handleRemove(favorite.vehicleId)}
              onToggleNotify={() =>
                handleToggleNotify(favorite.vehicleId, favorite.notifyOnPriceChange)
              }
              isRemoving={removingId === favorite.vehicleId || isRemoving}
            />
          ))}
        </div>
      ) : (
        <div className="py-12 text-center">
          <p className="text-muted-foreground">No se encontraron resultados para "{searchQuery}"</p>
          <Button variant="link" onClick={() => setSearchQuery('')} className="mt-2">
            Limpiar búsqueda
          </Button>
        </div>
      )}
    </div>
  );
}
