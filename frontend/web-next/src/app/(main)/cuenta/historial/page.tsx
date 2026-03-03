/**
 * User History Page
 *
 * Recently viewed vehicles with API integration
 *
 * Route: /cuenta/historial
 */

'use client';

import * as React from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
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
import {
  History,
  Trash2,
  Heart,
  Eye,
  Calendar,
  MapPin,
  Gauge,
  Clock,
  Car,
  Loader2,
  AlertCircle,
  RefreshCw,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { toast } from 'sonner';
import { historyService, type ViewedVehicle } from '@/services/history';
import { useFavorites } from '@/hooks/use-favorites';

// =============================================================================
// LOADING STATE
// =============================================================================

function HistoryLoading() {
  return (
    <div className="space-y-6">
      <div className="flex justify-between">
        <div>
          <Skeleton className="mb-2 h-8 w-32" />
          <Skeleton className="h-4 w-48" />
        </div>
        <Skeleton className="h-10 w-40" />
      </div>
      <div className="grid grid-cols-3 gap-4">
        {[1, 2, 3].map(i => (
          <Skeleton key={i} className="h-24 rounded-lg" />
        ))}
      </div>
      <div className="space-y-3">
        {[1, 2, 3].map(i => (
          <Skeleton key={i} className="h-32 rounded-lg" />
        ))}
      </div>
    </div>
  );
}

// =============================================================================
// ERROR STATE
// =============================================================================

function HistoryError({ onRetry }: { onRetry: () => void }) {
  return (
    <Card>
      <CardContent className="flex flex-col items-center py-16 text-center">
        <AlertCircle className="mb-4 h-12 w-12 text-red-400" />
        <h3 className="mb-2 font-semibold">Error al cargar historial</h3>
        <p className="text-muted-foreground mb-4 text-sm">
          No se pudo cargar tu historial de vistas
        </p>
        <Button variant="outline" onClick={onRetry}>
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
    <Card>
      <CardContent className="py-16 text-center">
        <History className="mx-auto mb-4 h-16 w-16 text-gray-300" />
        <h3 className="mb-2 text-xl font-semibold">Historial vacío</h3>
        <p className="text-muted-foreground mb-6">Los vehículos que visites aparecerán aquí</p>
        <Button asChild className="bg-primary hover:bg-primary/90">
          <Link href="/vehiculos">Explorar Vehículos</Link>
        </Button>
      </CardContent>
    </Card>
  );
}

// =============================================================================
// HISTORY ITEM
// =============================================================================

interface HistoryItemProps {
  item: ViewedVehicle;
  onRemove: (vehicleId: string) => void;
  isRemoving: boolean;
  onToggleFavorite: (vehicleId: string, isFavorite: boolean) => void;
}

function HistoryItem({ item, onRemove, isRemoving, onToggleFavorite }: HistoryItemProps) {
  const { vehicle, isFavorite, viewedAt } = item;

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('es-DO', {
      style: 'currency',
      currency: 'DOP',
      maximumFractionDigits: 0,
    }).format(price);
  };

  const isAvailable = vehicle.status === 'active';

  return (
    <Card
      className={cn(
        'overflow-hidden transition-all hover:shadow-md',
        isRemoving && 'opacity-50',
        !isAvailable && 'opacity-75'
      )}
    >
      <CardContent className="p-4">
        <div className="flex gap-4">
          {/* Image */}
          <Link
            href={`/vehiculos/${vehicle.slug}`}
            className="bg-muted relative flex h-20 w-32 flex-shrink-0 items-center justify-center overflow-hidden rounded-lg"
          >
            {vehicle.imageUrl ? (
              <Image src={vehicle.imageUrl} alt={vehicle.title} fill className="object-cover" />
            ) : (
              <Car className="text-muted-foreground h-8 w-8" />
            )}
            {!isAvailable && (
              <div className="absolute inset-0 flex items-center justify-center bg-black/50">
                <Badge variant="danger" className="text-xs">
                  {vehicle.status === 'sold' ? 'Vendido' : 'No disponible'}
                </Badge>
              </div>
            )}
          </Link>

          {/* Content */}
          <div className="min-w-0 flex-1">
            <div className="flex items-start justify-between">
              <div className="min-w-0">
                <Link
                  href={`/vehiculos/${vehicle.slug}`}
                  className="hover:text-primary flex items-center gap-2 transition-colors"
                >
                  <h3 className="truncate font-semibold">{vehicle.title}</h3>
                  {isFavorite && <Heart className="h-4 w-4 shrink-0 fill-red-500 text-red-500" />}
                </Link>
                <p className="text-muted-foreground text-sm">{vehicle.dealerName}</p>
              </div>
              <p className="text-primary shrink-0 text-xl font-bold">
                {formatPrice(vehicle.price)}
              </p>
            </div>

            <div className="text-muted-foreground mt-2 flex flex-wrap gap-3 text-sm">
              <span className="flex items-center gap-1">
                <Calendar className="h-3 w-3" />
                {vehicle.year}
              </span>
              <span className="flex items-center gap-1">
                <Gauge className="h-3 w-3" />
                {vehicle.mileage.toLocaleString()} km
              </span>
              <span className="flex items-center gap-1">
                <MapPin className="h-3 w-3" />
                {vehicle.location}
              </span>
              <span className="text-muted-foreground flex items-center gap-1">
                <Clock className="h-3 w-3" />
                {historyService.formatTimeAgo(viewedAt)}
              </span>
            </div>
          </div>

          {/* Actions */}
          <div className="flex flex-col gap-2">
            <Button
              variant="ghost"
              size="icon"
              onClick={() => onRemove(vehicle.id)}
              disabled={isRemoving}
              className="text-muted-foreground hover:text-red-600"
            >
              {isRemoving ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <Trash2 className="h-4 w-4" />
              )}
            </Button>
            <Button
              variant="ghost"
              size="icon"
              onClick={() => onToggleFavorite(vehicle.id, isFavorite)}
              className={cn(
                isFavorite
                  ? 'text-red-500 hover:text-red-600'
                  : 'text-muted-foreground hover:text-red-500'
              )}
            >
              <Heart className={cn('h-4 w-4', isFavorite && 'fill-current')} />
            </Button>
            <Button
              asChild
              size="sm"
              className="bg-primary hover:bg-primary/90"
              disabled={!isAvailable}
            >
              <Link href={`/vehiculos/${vehicle.slug}`}>Ver</Link>
            </Button>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

// =============================================================================
// MAIN PAGE
// =============================================================================

export default function HistoryPage() {
  const queryClient = useQueryClient();
  const [removingId, setRemovingId] = React.useState<string | null>(null);

  // Get favorites hook for toggle functionality
  const { toggleFavorite } = useFavorites();

  // Fetch history
  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['viewing-history'],
    queryFn: () => historyService.getHistory({ days: 30 }),
    staleTime: 1000 * 60, // 1 minute
  });

  // Remove single item mutation
  const removeMutation = useMutation({
    mutationFn: async (vehicleId: string) => {
      setRemovingId(vehicleId);
      return historyService.removeFromHistory(vehicleId);
    },
    onSuccess: () => {
      toast.success('Eliminado del historial');
      queryClient.invalidateQueries({ queryKey: ['viewing-history'] });
    },
    onError: () => {
      toast.error('Error al eliminar');
    },
    onSettled: () => {
      setRemovingId(null);
    },
  });

  // Clear all history mutation
  const clearMutation = useMutation({
    mutationFn: historyService.clearHistory,
    onSuccess: () => {
      toast.success('Historial eliminado');
      queryClient.invalidateQueries({ queryKey: ['viewing-history'] });
    },
    onError: () => {
      toast.error('Error al limpiar historial');
    },
  });

  // Handle toggle favorite
  const handleToggleFavorite = async (vehicleId: string, currentlyFavorite: boolean) => {
    try {
      await toggleFavorite(vehicleId);
      toast.success(currentlyFavorite ? 'Eliminado de favoritos' : 'Agregado a favoritos');
      queryClient.invalidateQueries({ queryKey: ['viewing-history'] });
    } catch {
      toast.error('Error al actualizar favoritos');
    }
  };

  const items = data?.items || [];
  const totalFavorites = data?.totalFavorites || 0;
  const daysInHistory = historyService.calculateDaysInHistory(items);
  const groupedHistory = historyService.groupByDate(items);

  // Loading state
  if (isLoading) {
    return <HistoryLoading />;
  }

  // Error state
  if (error) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-foreground text-2xl font-bold">Historial</h1>
          <p className="text-muted-foreground">Vehículos que has visto recientemente</p>
        </div>
        <HistoryError onRetry={refetch} />
      </div>
    );
  }

  // Empty state
  if (items.length === 0) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-foreground text-2xl font-bold">Historial</h1>
          <p className="text-muted-foreground">Vehículos que has visto recientemente</p>
        </div>
        <EmptyState />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col justify-between gap-4 sm:flex-row">
        <div>
          <h1 className="text-foreground text-2xl font-bold">Historial</h1>
          <p className="text-muted-foreground">Vehículos que has visto recientemente</p>
        </div>
        <AlertDialog>
          <AlertDialogTrigger asChild>
            <Button variant="outline" disabled={clearMutation.isPending}>
              {clearMutation.isPending ? (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              ) : (
                <Trash2 className="mr-2 h-4 w-4" />
              )}
              Limpiar Historial
            </Button>
          </AlertDialogTrigger>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>¿Limpiar todo el historial?</AlertDialogTitle>
              <AlertDialogDescription>
                Esta acción eliminará todos los vehículos de tu historial de vistas. No se puede
                deshacer.
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel>Cancelar</AlertDialogCancel>
              <AlertDialogAction
                onClick={() => clearMutation.mutate()}
                className="bg-red-600 hover:bg-red-700"
              >
                Limpiar todo
              </AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-3 gap-4">
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="bg-primary/10 rounded-lg p-2">
                <Eye className="text-primary h-5 w-5" />
              </div>
              <div>
                <p className="text-2xl font-bold">{items.length}</p>
                <p className="text-muted-foreground text-sm">Vistos</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-red-100 p-2">
                <Heart className="h-5 w-5 text-red-600" />
              </div>
              <div>
                <p className="text-2xl font-bold">{totalFavorites}</p>
                <p className="text-muted-foreground text-sm">Favoritos</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-blue-100 p-2">
                <Clock className="h-5 w-5 text-blue-600" />
              </div>
              <div>
                <p className="text-2xl font-bold">{daysInHistory}</p>
                <p className="text-muted-foreground text-sm">
                  {daysInHistory === 1 ? 'Día' : 'Días'}
                </p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* History Grouped by Date */}
      {Object.entries(groupedHistory).map(([date, dateItems]) => (
        <div key={date}>
          <h2 className="mb-3 flex items-center gap-2 text-lg font-semibold">
            <Calendar className="text-muted-foreground h-5 w-5" />
            {date}
          </h2>
          <div className="space-y-3">
            {dateItems.map(item => (
              <HistoryItem
                key={item.id}
                item={item}
                onRemove={vehicleId => removeMutation.mutate(vehicleId)}
                isRemoving={removingId === item.vehicle.id}
                onToggleFavorite={handleToggleFavorite}
              />
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}
