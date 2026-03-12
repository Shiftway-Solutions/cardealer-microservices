/**
 * Dealer Inventory Page
 *
 * Manage dealer vehicle inventory with filters and actions
 * Connected to real APIs - January 2026
 */

'use client';

import * as React from 'react';
import Link from 'next/link';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Car,
  Search,
  Plus,
  Upload,
  MoreHorizontal,
  Eye,
  Edit,
  Trash2,
  TrendingUp,
  Pause,
  Play,
  ChevronLeft,
  ChevronRight,
  RefreshCw,
  AlertCircle,
} from 'lucide-react';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { useCurrentDealer, useDealerStats } from '@/hooks/use-dealers';
import { useVehiclesByDealer, useDeleteVehicle, useUpdateVehicle } from '@/hooks/use-vehicles';
import { VideoHelpButton } from '@/components/dealer/video-help-button';
import { toast } from 'sonner';
import Image from 'next/image';
import type { VehicleStatus } from '@/types';

// Default vehicle image
const DEFAULT_VEHICLE_IMAGE = 'https://images.unsplash.com/photo-1494976388531-d1058494cdd8?w=200';

const getStatusBadge = (status: VehicleStatus | undefined) => {
  switch (status) {
    case 'active':
      return <Badge className="bg-primary/10 text-primary">Activo</Badge>;
    case 'paused':
      return <Badge className="bg-amber-100 text-amber-700">Pausado</Badge>;
    case 'sold':
      return <Badge className="bg-blue-100 text-blue-700">Vendido</Badge>;
    case 'pending':
      return <Badge className="bg-muted text-foreground">Pendiente</Badge>;
    case 'expired':
      return <Badge className="bg-red-100 text-red-700">Expirado</Badge>;
    default:
      return <Badge variant="secondary">{status || 'Desconocido'}</Badge>;
  }
};

// Calculate days since vehicle was listed
function getDaysListed(createdAt: string | undefined): number {
  if (!createdAt) return 0;
  const created = new Date(createdAt);
  const now = new Date();
  const diffTime = Math.abs(now.getTime() - created.getTime());
  return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
}

// Skeleton for loading state
function InventorySkeleton() {
  return (
    <div className="space-y-4">
      {[1, 2, 3, 4, 5].map(i => (
        <div key={i} className="flex items-center gap-4 p-4">
          <Skeleton className="h-4 w-4" />
          <Skeleton className="h-12 w-16 rounded" />
          <div className="flex-1 space-y-2">
            <Skeleton className="h-4 w-48" />
            <Skeleton className="h-3 w-32" />
          </div>
          <Skeleton className="h-6 w-20" />
          <Skeleton className="h-4 w-16" />
        </div>
      ))}
    </div>
  );
}

export default function InventoryPage() {
  const [search, setSearch] = React.useState('');
  const [statusFilter, setStatusFilter] = React.useState<string>('all');
  const [selectedVehicles, setSelectedVehicles] = React.useState<string[]>([]);
  const [page, setPage] = React.useState(1);
  const pageSize = 10;

  // Get current dealer
  const { data: dealer, isLoading: isDealerLoading } = useCurrentDealer();

  // Get dealer stats for inventory count
  const { data: stats } = useDealerStats(dealer?.id);

  // Use dealer.userId (the auth user ID) as sellerId for vehicle queries
  const sellerId = dealer?.userId || '';

  // Get vehicles from API
  const {
    data: vehiclesData,
    isLoading: isVehiclesLoading,
    error: vehiclesError,
    refetch,
  } = useVehiclesByDealer(sellerId, {
    page,
    pageSize,
    status: statusFilter !== 'all' ? (statusFilter as VehicleStatus) : undefined,
  });

  // Mutations
  const deleteVehicle = useDeleteVehicle();
  const updateVehicle = useUpdateVehicle();

  // Filter vehicles by search (client-side for instant feedback)
  const vehicles = vehiclesData?.items || [];
  const filteredVehicles = vehicles.filter(v => {
    const title = `${v.make} ${v.model} ${v.trim || ''} ${v.year}`;
    return title.toLowerCase().includes(search.toLowerCase());
  });

  const totalItems = vehiclesData?.pagination?.totalItems || 0;
  const totalPages = vehiclesData?.pagination?.totalPages || 1;
  const activeCount = stats?.activeListings || 0;
  const maxListings = stats?.totalListings || 50; // Default to 50 if not available

  const toggleSelect = (id: string) => {
    setSelectedVehicles(prev => (prev.includes(id) ? prev.filter(v => v !== id) : [...prev, id]));
  };

  const toggleSelectAll = () => {
    if (selectedVehicles.length === filteredVehicles.length) {
      setSelectedVehicles([]);
    } else {
      setSelectedVehicles(filteredVehicles.map(v => v.id));
    }
  };

  // Handle delete vehicle
  const handleDelete = async (vehicleId: string) => {
    if (!confirm('¿Estás seguro de eliminar este vehículo?')) return;

    try {
      await deleteVehicle.mutateAsync(vehicleId);
      toast.success('Vehículo eliminado correctamente');
      refetch();
    } catch {
      toast.error('No se pudo eliminar el vehículo');
    }
  };

  // Handle pause/activate vehicle
  const handleToggleStatus = async (
    vehicleId: string,
    currentStatus: VehicleStatus | undefined
  ) => {
    const newStatus: VehicleStatus = currentStatus === 'active' ? 'paused' : 'active';

    try {
      await updateVehicle.mutateAsync({
        id: vehicleId,
        data: { status: newStatus },
      });
      toast.success(`Vehículo ${newStatus === 'active' ? 'activado' : 'pausado'}`);
      refetch();
    } catch {
      toast.error('No se pudo actualizar el estado del vehículo');
    }
  };

  // Loading state
  if (isDealerLoading || isVehiclesLoading) {
    return (
      <div className="space-y-6">
        <div className="flex flex-col justify-between gap-4 sm:flex-row">
          <div>
            <h1 className="text-foreground text-2xl font-bold">Inventario</h1>
            <Skeleton className="mt-1 h-4 w-40" />
          </div>
        </div>
        <Card>
          <CardContent className="p-4">
            <InventorySkeleton />
          </CardContent>
        </Card>
      </div>
    );
  }

  // Error state
  if (vehiclesError) {
    return (
      <div className="space-y-6">
        <div className="flex flex-col justify-between gap-4 sm:flex-row">
          <div>
            <h1 className="text-foreground text-2xl font-bold">Inventario</h1>
          </div>
        </div>
        <Card className="border-red-200 bg-red-50">
          <CardContent className="flex flex-col items-center justify-center gap-4 p-8">
            <AlertCircle className="h-12 w-12 text-red-500" />
            <p className="text-center text-red-700">
              Error al cargar el inventario. Por favor intenta de nuevo.
            </p>
            <Button variant="outline" onClick={() => refetch()}>
              <RefreshCw className="mr-2 h-4 w-4" />
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
          <div className="flex items-center gap-2">
            <h1 className="text-foreground text-2xl font-bold">Inventario</h1>
            <VideoHelpButton sectionKey="inventario" variant="icon" />
          </div>
          <p className="text-muted-foreground">
            {activeCount} de {maxListings} vehículos activos
          </p>
        </div>
        <div className="flex gap-3">
          <Button variant="outline" onClick={() => refetch()}>
            <RefreshCw className="mr-2 h-4 w-4" />
            Actualizar
          </Button>
          <Button variant="outline" asChild>
            <Link href="/dealer/inventario/importar">
              <Upload className="mr-2 h-4 w-4" />
              Importar
            </Link>
          </Button>
          <Button className="bg-primary hover:bg-primary/90" asChild>
            <Link href="/publicar">
              <Plus className="mr-2 h-4 w-4" />
              Agregar Vehículo
            </Link>
          </Button>
        </div>
      </div>

      {/* Filters */}
      <Card>
        <CardContent className="p-4">
          <div className="flex flex-col gap-4 sm:flex-row">
            <div className="relative flex-1">
              <Search className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
              <Input
                placeholder="Buscar por marca, modelo..."
                value={search}
                onChange={e => setSearch(e.target.value)}
                className="pl-9"
              />
            </div>
            <Select
              value={statusFilter}
              onValueChange={v => {
                setStatusFilter(v);
                setPage(1);
              }}
            >
              <SelectTrigger className="w-40">
                <SelectValue placeholder="Estado" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos</SelectItem>
                <SelectItem value="active">Activos</SelectItem>
                <SelectItem value="paused">Pausados</SelectItem>
                <SelectItem value="sold">Vendidos</SelectItem>
                <SelectItem value="pending">Pendientes</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      {/* Bulk Actions */}
      {selectedVehicles.length > 0 && (
        <Card className="border-primary bg-primary/10">
          <CardContent className="flex items-center justify-between p-4">
            <span className="text-primary text-sm">
              {selectedVehicles.length} vehículo(s) seleccionado(s)
            </span>
            <div className="flex gap-2">
              <Button size="sm" variant="outline">
                <Pause className="mr-1 h-4 w-4" />
                Pausar
              </Button>
              <Button size="sm" variant="outline">
                <TrendingUp className="mr-1 h-4 w-4" />
                Boost
              </Button>
              <Button
                size="sm"
                variant="outline"
                className="text-red-600 hover:text-red-700"
                onClick={() => {
                  if (confirm(`¿Eliminar ${selectedVehicles.length} vehículo(s)?`)) {
                    // TODO: Implement bulk delete
                    toast.info('La eliminación masiva estará disponible pronto');
                  }
                }}
              >
                <Trash2 className="mr-1 h-4 w-4" />
                Eliminar
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Inventory Table */}
      <Card>
        <CardContent className="p-0">
          {filteredVehicles.length === 0 ? (
            <div className="flex flex-col items-center justify-center gap-4 p-12">
              <Car className="h-16 w-16 text-gray-300" />
              <div className="text-center">
                <h3 className="text-foreground text-lg font-medium">No hay vehículos</h3>
                <p className="text-muted-foreground">
                  {search || statusFilter !== 'all'
                    ? 'No se encontraron vehículos con los filtros aplicados.'
                    : 'Comienza agregando tu primer vehículo al inventario.'}
                </p>
              </div>
              <Button asChild className="bg-primary hover:bg-primary/90">
                <Link href="/publicar">
                  <Plus className="mr-2 h-4 w-4" />
                  Agregar Vehículo
                </Link>
              </Button>
            </div>
          ) : (
            <>
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead className="border-border bg-muted/50 border-b">
                    <tr>
                      <th className="w-10 p-4">
                        <input
                          type="checkbox"
                          checked={
                            selectedVehicles.length === filteredVehicles.length &&
                            filteredVehicles.length > 0
                          }
                          onChange={toggleSelectAll}
                          className="rounded"
                        />
                      </th>
                      <th className="text-muted-foreground p-4 text-left text-sm font-medium">
                        Vehículo
                      </th>
                      <th className="text-muted-foreground p-4 text-left text-sm font-medium">
                        Precio
                      </th>
                      <th className="text-muted-foreground p-4 text-left text-sm font-medium">
                        Estado
                      </th>
                      <th className="text-muted-foreground p-4 text-center text-sm font-medium">
                        Vistas
                      </th>
                      <th className="text-muted-foreground p-4 text-center text-sm font-medium">
                        Días
                      </th>
                      <th className="w-10 p-4"></th>
                    </tr>
                  </thead>
                  <tbody className="divide-y">
                    {filteredVehicles.map(vehicle => {
                      const title = `${vehicle.make} ${vehicle.model} ${vehicle.trim || ''} ${vehicle.year}`;
                      const image = vehicle.imageUrl || DEFAULT_VEHICLE_IMAGE;
                      const daysListed = getDaysListed(vehicle.createdAt);

                      return (
                        <tr key={vehicle.id} className="hover:bg-muted/50">
                          <td className="p-4">
                            <input
                              type="checkbox"
                              checked={selectedVehicles.includes(vehicle.id)}
                              onChange={() => toggleSelect(vehicle.id)}
                              className="rounded"
                            />
                          </td>
                          <td className="p-4">
                            <div className="flex items-center gap-3">
                              <Image
                                src={image}
                                alt={title}
                                width={64}
                                height={48}
                                className="h-12 w-16 rounded object-cover"
                              />
                              <div>
                                <Link
                                  href={`/vehiculos/${vehicle.slug}`}
                                  className="hover:text-primary font-medium transition-colors"
                                >
                                  {title}
                                </Link>
                                <p className="text-muted-foreground text-xs">
                                  {vehicle.mileage?.toLocaleString()} km • {vehicle.transmission}
                                </p>
                              </div>
                            </div>
                          </td>
                          <td className="p-4">
                            <span className="font-semibold">
                              {vehicle.currency === 'USD' ? 'US$' : 'RD$'}{' '}
                              {vehicle.price.toLocaleString()}
                            </span>
                          </td>
                          <td className="p-4">{getStatusBadge(vehicle.status)}</td>
                          <td className="p-4 text-center">
                            <span className="text-muted-foreground">{vehicle.viewCount || 0}</span>
                          </td>
                          <td className="p-4 text-center">
                            <span
                              className={
                                daysListed > 30 ? 'text-amber-600' : 'text-muted-foreground'
                              }
                            >
                              {daysListed}
                            </span>
                          </td>
                          <td className="p-4">
                            <DropdownMenu>
                              <DropdownMenuTrigger asChild>
                                <Button variant="ghost" size="icon">
                                  <MoreHorizontal className="h-4 w-4" />
                                </Button>
                              </DropdownMenuTrigger>
                              <DropdownMenuContent align="end">
                                <DropdownMenuItem asChild>
                                  <Link href={`/vehiculos/${vehicle.slug}`}>
                                    <Eye className="mr-2 h-4 w-4" />
                                    Ver Publicación
                                  </Link>
                                </DropdownMenuItem>
                                <DropdownMenuItem asChild>
                                  <Link href={`/vender/editar/${vehicle.id}`}>
                                    <Edit className="mr-2 h-4 w-4" />
                                    Editar
                                  </Link>
                                </DropdownMenuItem>
                                <DropdownMenuItem>
                                  <TrendingUp className="mr-2 h-4 w-4" />
                                  Boost
                                </DropdownMenuItem>
                                <DropdownMenuItem
                                  onClick={() => handleToggleStatus(vehicle.id, vehicle.status)}
                                >
                                  {vehicle.status === 'active' ? (
                                    <>
                                      <Pause className="mr-2 h-4 w-4" />
                                      Pausar
                                    </>
                                  ) : (
                                    <>
                                      <Play className="mr-2 h-4 w-4" />
                                      Activar
                                    </>
                                  )}
                                </DropdownMenuItem>
                                <DropdownMenuItem
                                  className="text-red-600"
                                  onClick={() => handleDelete(vehicle.id)}
                                >
                                  <Trash2 className="mr-2 h-4 w-4" />
                                  Eliminar
                                </DropdownMenuItem>
                              </DropdownMenuContent>
                            </DropdownMenu>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>

              {/* Pagination */}
              <div className="border-border flex items-center justify-between border-t p-4">
                <p className="text-muted-foreground text-sm">
                  Mostrando {(page - 1) * pageSize + 1}-{Math.min(page * pageSize, totalItems)} de{' '}
                  {totalItems} vehículos
                </p>
                <div className="flex gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={page <= 1}
                    onClick={() => setPage(p => p - 1)}
                  >
                    <ChevronLeft className="h-4 w-4" />
                  </Button>
                  {Array.from({ length: Math.min(totalPages, 5) }, (_, i) => i + 1).map(p => (
                    <Button
                      key={p}
                      variant="outline"
                      size="sm"
                      className={page === p ? 'bg-primary/10' : ''}
                      onClick={() => setPage(p)}
                    >
                      {p}
                    </Button>
                  ))}
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={page >= totalPages}
                    onClick={() => setPage(p => p + 1)}
                  >
                    <ChevronRight className="h-4 w-4" />
                  </Button>
                </div>
              </div>
            </>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
