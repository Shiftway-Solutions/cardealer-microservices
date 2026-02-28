/**
 * Saved Searches Page
 *
 * Manage saved search queries
 * Connected to AlertService via API Gateway
 */

'use client';

import Link from 'next/link';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Switch } from '@/components/ui/switch';
import { Skeleton } from '@/components/ui/skeleton';
import { toast } from 'sonner';
import {
  Search,
  Plus,
  Trash2,
  Bell,
  Filter,
  Play,
  Clock,
  Car,
  AlertCircle,
  Loader2,
} from 'lucide-react';
import {
  useSavedSearches,
  useToggleSavedSearch,
  useDeleteSavedSearch,
  useAlertStats,
  formatNotifyFrequency,
  buildSearchDescription,
  type SavedSearch,
} from '@/hooks/use-alerts';
import type { VehicleSearchParams } from '@/types';
import { formatPrice } from '@/lib/format';

const getFilterChips = (params: VehicleSearchParams): string[] => {
  const chips: string[] = [];
  if (params.bodyType) chips.push(params.bodyType);
  if (params.make) chips.push(params.make);
  if (params.model) chips.push(params.model);
  if (params.priceMin && params.priceMax) {
    chips.push(`${formatPrice(params.priceMin)} - ${formatPrice(params.priceMax)}`);
  } else if (params.priceMax) {
    chips.push(`Hasta ${formatPrice(params.priceMax)}`);
  } else if (params.priceMin) {
    chips.push(`Desde ${formatPrice(params.priceMin)}`);
  }
  if (params.yearMin) chips.push(`Desde ${params.yearMin}`);
  if (params.mileageMax) chips.push(`< ${Number(params.mileageMax).toLocaleString()} km`);
  if (params.condition === 'nuevo' || params.condition === 'new') chips.push('Nuevo');
  if (params.condition === 'usado' || params.condition === 'used') chips.push('Usado');
  if (params.province) chips.push(params.province);
  if (params.sellerType === 'dealer') chips.push('Dealer');
  if (params.sellerType === 'seller') chips.push('Particular');
  if ((params as Record<string, unknown>).isCertified) chips.push('Con garantía');
  if (params.hasCleanTitle) chips.push('Título limpio');
  return chips;
};

export default function SavedSearchesPage() {
  // Fetch searches from API
  const { data: searchesData, isLoading, error, refetch } = useSavedSearches();
  const { data: stats } = useAlertStats();

  // Mutations
  const toggleMutation = useToggleSavedSearch();
  const deleteMutation = useDeleteSavedSearch();

  // Get searches array from paginated response
  const searches = searchesData?.items ?? [];

  const handleToggleNotifications = async (search: SavedSearch) => {
    try {
      await toggleMutation.mutateAsync({
        id: search.id,
        notifyNewListings: search.notifyNewListings,
        notifyFrequency: search.notifyFrequency,
      });
      toast.success('Notificaciones actualizadas');
    } catch {
      toast.error('Error al actualizar notificaciones');
    }
  };

  const handleDeleteSearch = async (id: string) => {
    try {
      await deleteMutation.mutateAsync(id);
      toast.success('Búsqueda eliminada');
    } catch {
      toast.error('Error al eliminar búsqueda');
    }
  };

  const handleRunSearch = (search: SavedSearch) => {
    // Navigate directly to /vehiculos with saved search params.
    // (Backend has no /run endpoint — the search is executed client-side.)
    const sp = search.searchParams as Record<string, unknown>;
    const urlParams = new URLSearchParams();
    if (sp.q) urlParams.set('q', String(sp.q));
    if (sp.make) urlParams.set('make', String(sp.make));
    if (sp.model) urlParams.set('model', String(sp.model));
    if (sp.yearMin) urlParams.set('year_min', String(sp.yearMin));
    if (sp.yearMax) urlParams.set('year_max', String(sp.yearMax));
    if (sp.priceMin) urlParams.set('price_min', String(sp.priceMin));
    if (sp.priceMax) urlParams.set('price_max', String(sp.priceMax));
    if (sp.mileageMax) urlParams.set('mileage_max', String(sp.mileageMax));
    if (sp.bodyType) urlParams.set('body_type', String(sp.bodyType));
    if (sp.transmission) urlParams.set('transmission', String(sp.transmission));
    if (sp.fuelType) urlParams.set('fuel_type', String(sp.fuelType));
    if (sp.drivetrain) urlParams.set('drivetrain', String(sp.drivetrain));
    if (sp.condition) urlParams.set('condition', String(sp.condition));
    if (sp.province) urlParams.set('province', String(sp.province));
    if (sp.city) urlParams.set('city', String(sp.city));
    if (sp.dealRating) urlParams.set('deal_rating', String(sp.dealRating));
    if (sp.sellerType) urlParams.set('seller_type', String(sp.sellerType));
    if (sp.isCertified) urlParams.set('is_certified', 'true');
    if (sp.hasCleanTitle) urlParams.set('has_clean_title', 'true');
    if (sp.color) urlParams.set('color', String(sp.color));
    window.location.href = `/vehiculos?${urlParams.toString()}`;
  };

  // Show loading skeleton
  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="flex flex-col justify-between gap-4 sm:flex-row">
          <div>
            <h1 className="text-foreground text-2xl font-bold">Búsquedas Guardadas</h1>
            <p className="text-muted-foreground">Accede rápidamente a tus filtros favoritos</p>
          </div>
        </div>
        <div className="grid grid-cols-3 gap-4">
          {[1, 2, 3].map(i => (
            <Card key={i}>
              <CardContent className="p-4">
                <Skeleton className="h-16 w-full" />
              </CardContent>
            </Card>
          ))}
        </div>
        <div className="space-y-4">
          {[1, 2, 3].map(i => (
            <Skeleton key={i} className="h-32 w-full" />
          ))}
        </div>
      </div>
    );
  }

  // Show error state
  if (error) {
    return (
      <div className="space-y-6">
        <div className="flex flex-col justify-between gap-4 sm:flex-row">
          <div>
            <h1 className="text-foreground text-2xl font-bold">Búsquedas Guardadas</h1>
            <p className="text-muted-foreground">Accede rápidamente a tus filtros favoritos</p>
          </div>
        </div>
        <Card className="border-red-200">
          <CardContent className="flex flex-col items-center py-12">
            <AlertCircle className="mb-4 h-12 w-12 text-red-500" />
            <p className="text-muted-foreground mb-4">Error al cargar las búsquedas</p>
            <Button onClick={() => refetch()} variant="outline">
              Reintentar
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col justify-between gap-4 sm:flex-row">
        <div>
          <h1 className="text-foreground text-2xl font-bold">Búsquedas Guardadas</h1>
          <p className="text-muted-foreground">Accede rápidamente a tus filtros favoritos</p>
        </div>
        <Button asChild className="bg-[#00A870] hover:bg-[#008a5c]">
          <Link href="/vehiculos">
            <Plus className="mr-2 h-4 w-4" />
            Nueva Búsqueda
          </Link>
        </Button>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-3 gap-4">
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-[#00A870]/10 p-2">
                <Search className="h-5 w-5 text-[#00A870]" />
              </div>
              <div>
                <p className="text-2xl font-bold">{stats?.totalSavedSearches ?? searches.length}</p>
                <p className="text-muted-foreground text-sm">Guardadas</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-blue-100 p-2">
                <Bell className="h-5 w-5 text-blue-600" />
              </div>
              <div>
                <p className="text-2xl font-bold">
                  {stats?.activeSavedSearches ?? searches.filter(s => s.notifyNewListings).length}
                </p>
                <p className="text-muted-foreground text-sm">Con Alertas</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-amber-100 p-2">
                <Car className="h-5 w-5 text-amber-600" />
              </div>
              <div>
                <p className="text-2xl font-bold">
                  {stats?.newMatchesThisWeek ??
                    searches.reduce((acc, s) => acc + (s.newMatchCount || 0), 0)}
                </p>
                <p className="text-muted-foreground text-sm">Nuevos Resultados</p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Searches List */}
      <div className="space-y-4">
        {searches.map(search => (
          <Card key={search.id} className="overflow-hidden">
            <CardContent className="p-0">
              <div className="flex flex-col md:flex-row">
                {/* Main Content */}
                <div className="flex-1 p-4">
                  <div className="mb-3 flex items-start justify-between">
                    <div>
                      <div className="flex items-center gap-2">
                        <h3 className="text-lg font-semibold">{search.name}</h3>
                        {search.newMatchCount > 0 && (
                          <Badge className="bg-[#00A870]/10 text-[#00A870]">
                            {search.newMatchCount} nuevos
                          </Badge>
                        )}
                      </div>
                      <p className="text-muted-foreground mt-1 flex items-center gap-1 text-sm">
                        <Clock className="h-3 w-3" />
                        Última ejecución:{' '}
                        {search.lastMatchedAt
                          ? new Date(search.lastMatchedAt).toLocaleDateString('es-DO')
                          : 'Nunca'}
                      </p>
                    </div>
                  </div>

                  {/* Filters */}
                  <div className="mb-4 flex flex-wrap gap-2">
                    {getFilterChips(search.searchParams).map((chip, idx) => (
                      <Badge key={idx} variant="outline" className="bg-muted/50">
                        <Filter className="mr-1 h-3 w-3" />
                        {chip}
                      </Badge>
                    ))}
                  </div>

                  {/* Actions Row */}
                  <div className="border-border flex items-center justify-between border-t pt-3">
                    <div className="flex items-center gap-4">
                      <span className="text-muted-foreground text-sm">
                        {search.matchCount} resultados
                      </span>
                      <div className="flex items-center gap-2">
                        <span className="text-muted-foreground text-sm">Notificaciones</span>
                        <Switch
                          checked={search.notifyNewListings}
                          onCheckedChange={() => handleToggleNotifications(search)}
                          disabled={toggleMutation.isPending}
                        />
                      </div>
                    </div>
                    <div className="flex gap-2">
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleDeleteSearch(search.id)}
                        disabled={deleteMutation.isPending}
                      >
                        {deleteMutation.isPending ? (
                          <Loader2 className="h-4 w-4 animate-spin" />
                        ) : (
                          <Trash2 className="h-4 w-4 text-red-500" />
                        )}
                      </Button>
                      <Button
                        className="bg-[#00A870] hover:bg-[#008a5c]"
                        onClick={() => handleRunSearch(search)}
                      >
                        <Play className="mr-1 h-4 w-4" />
                        Ejecutar
                      </Button>
                    </div>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Empty State */}
      {searches.length === 0 && (
        <Card>
          <CardContent className="p-12 text-center">
            <Search className="mx-auto mb-4 h-16 w-16 text-gray-300" />
            <h3 className="mb-2 text-xl font-semibold">No tienes búsquedas guardadas</h3>
            <p className="text-muted-foreground mb-6">
              Guarda tus búsquedas favoritas para acceder a ellas rápidamente
            </p>
            <Button asChild className="bg-[#00A870] hover:bg-[#008a5c]">
              <Link href="/vehiculos">
                <Search className="mr-2 h-4 w-4" />
                Buscar Vehículos
              </Link>
            </Button>
          </CardContent>
        </Card>
      )}

      {/* Tips */}
      <Card className="border-blue-200 bg-blue-50">
        <CardContent className="p-4">
          <h4 className="mb-2 font-medium text-blue-800">💡 Consejo</h4>
          <p className="text-sm text-blue-700">
            Activa las notificaciones en tus búsquedas guardadas para recibir alertas cuando se
            publiquen nuevos vehículos que coincidan con tus criterios.
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
