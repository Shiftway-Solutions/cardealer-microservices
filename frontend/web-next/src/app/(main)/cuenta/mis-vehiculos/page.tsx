/**
 * My Vehicles Page
 *
 * Displays user's vehicle listings with filters and actions
 */

'use client';

import * as React from 'react';
import Link from 'next/link';
import {
  Plus,
  Search,
  Filter,
  MoreVertical,
  Eye,
  Edit,
  Pause,
  Play,
  Trash2,
  AlertCircle,
  Car,
  Loader2,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent } from '@/components/ui/card';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { cn } from '@/lib/utils';
import { userService, type UserVehicleDto } from '@/services/users';
import { vehicleService } from '@/services/vehicles';
import { toast } from 'sonner';

type VehicleStatus = 'all' | 'active' | 'pending' | 'paused' | 'sold' | 'expired' | 'rejected';

const statusConfig: Record<string, { label: string; color: string; bgColor: string }> = {
  active: { label: 'Activo', color: 'text-green-700', bgColor: 'bg-green-100' },
  pending: { label: 'Pendiente', color: 'text-yellow-700', bgColor: 'bg-yellow-100' },
  paused: { label: 'Pausado', color: 'text-foreground', bgColor: 'bg-muted' },
  sold: { label: 'Vendido', color: 'text-blue-700', bgColor: 'bg-blue-100' },
  expired: { label: 'Expirado', color: 'text-red-700', bgColor: 'bg-red-100' },
  rejected: { label: 'Rechazado', color: 'text-red-700', bgColor: 'bg-red-100' },
};

function StatusBadge({ status }: { status: string }) {
  const config = statusConfig[status] || statusConfig.pending;
  return (
    <span
      className={cn('rounded-full px-2 py-1 text-xs font-medium', config.color, config.bgColor)}
    >
      {config.label}
    </span>
  );
}

function VehicleCard({
  vehicle,
  onPause,
  onActivate,
  onDelete,
}: {
  vehicle: UserVehicleDto;
  onPause: (id: string) => void;
  onActivate: (id: string) => void;
  onDelete: (id: string) => void;
}) {
  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('es-DO', {
      style: 'currency',
      currency: 'DOP',
      maximumFractionDigits: 0,
    }).format(price);
  };

  return (
    <Card className="overflow-hidden">
      <div className="flex flex-col sm:flex-row">
        {/* Image */}
        <div className="bg-muted relative h-40 w-full flex-shrink-0 sm:h-auto sm:w-48">
          {vehicle.imageUrl ? (
            <img
              src={vehicle.imageUrl}
              alt={vehicle.title}
              className="h-full w-full object-cover"
            />
          ) : (
            <div className="flex h-full w-full items-center justify-center">
              <Car className="text-muted-foreground h-12 w-12" />
            </div>
          )}
          {/* Status overlay for paused/expired/pending/rejected */}
          {(vehicle.status === 'paused' || vehicle.status === 'expired') && (
            <div className="absolute inset-0 flex items-center justify-center bg-black/40">
              <span className="rounded bg-black/60 px-3 py-1 font-medium text-white">
                {vehicle.status === 'paused' ? 'Pausado' : 'Expirado'}
              </span>
            </div>
          )}
          {vehicle.status === 'pending' && (
            <div className="absolute inset-0 flex items-center justify-center bg-yellow-900/30">
              <span className="rounded bg-yellow-600/80 px-3 py-1 text-sm font-medium text-white">
                En Revisión
              </span>
            </div>
          )}
          {vehicle.status === 'rejected' && (
            <div className="absolute inset-0 flex items-center justify-center bg-red-900/30">
              <span className="rounded bg-red-600/80 px-3 py-1 text-sm font-medium text-white">
                Rechazado
              </span>
            </div>
          )}
        </div>

        {/* Content */}
        <CardContent className="flex-1 p-4">
          <div className="flex items-start justify-between gap-4">
            <div className="min-w-0 flex-1">
              <div className="mb-1 flex items-center gap-2">
                <StatusBadge status={vehicle.status} />
                {vehicle.isFeatured && (
                  <span className="text-primary bg-primary/10 rounded-full px-2 py-1 text-xs font-medium">
                    Destacado
                  </span>
                )}
              </div>

              <h3 className="text-foreground truncate font-semibold">{vehicle.title}</h3>

              <p className="text-primary mt-1 text-xl font-bold">{formatPrice(vehicle.price)}</p>

              {/* Stats */}
              <div className="text-muted-foreground mt-3 flex items-center gap-4 text-sm">
                <div className="flex items-center gap-1">
                  <Eye className="h-4 w-4" />
                  <span>{vehicle.viewCount} vistas</span>
                </div>
                <div>{vehicle.inquiryCount} consultas</div>
              </div>

              {/* Expiration warning */}
              {vehicle.status === 'active' && vehicle.expiresAt && (
                <div className="mt-2">
                  {new Date(vehicle.expiresAt) < new Date(Date.now() + 7 * 24 * 60 * 60 * 1000) && (
                    <div className="flex items-center gap-1 text-sm text-yellow-700">
                      <AlertCircle className="h-4 w-4" />
                      Expira el {new Date(vehicle.expiresAt).toLocaleDateString('es-DO')}
                    </div>
                  )}
                </div>
              )}
            </div>

            {/* Actions */}
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="icon">
                  <MoreVertical className="h-4 w-4" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem asChild>
                  <Link href={`/vehiculos/${vehicle.slug}`}>
                    <Eye className="mr-2 h-4 w-4" />
                    Ver publicación
                  </Link>
                </DropdownMenuItem>
                <DropdownMenuItem asChild>
                  <Link href={`/vender/editar/${vehicle.id}`}>
                    <Edit className="mr-2 h-4 w-4" />
                    Editar
                  </Link>
                </DropdownMenuItem>
                <DropdownMenuSeparator />
                {vehicle.status === 'active' && (
                  <DropdownMenuItem onClick={() => onPause(vehicle.id)}>
                    <Pause className="mr-2 h-4 w-4" />
                    Pausar
                  </DropdownMenuItem>
                )}
                {(vehicle.status === 'paused' || vehicle.status === 'rejected') && (
                  <DropdownMenuItem onClick={() => onActivate(vehicle.id)}>
                    <Play className="mr-2 h-4 w-4" />
                    {vehicle.status === 'rejected' ? 'Re-enviar a Revisión' : 'Reactivar'}
                  </DropdownMenuItem>
                )}
                <DropdownMenuSeparator />
                <DropdownMenuItem
                  onClick={() => onDelete(vehicle.id)}
                  className="text-red-600 focus:bg-red-50 focus:text-red-700"
                >
                  <Trash2 className="mr-2 h-4 w-4" />
                  Eliminar
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </CardContent>
      </div>
    </Card>
  );
}

function EmptyState({ status }: { status: VehicleStatus }) {
  const messages: Record<VehicleStatus, { title: string; description: string }> = {
    all: {
      title: 'No tienes vehículos publicados',
      description: 'Publica tu primer vehículo y llega a miles de compradores',
    },
    active: {
      title: 'No tienes vehículos activos',
      description: 'Tus vehículos pausados o expirados aparecen en otras pestañas',
    },
    pending: {
      title: 'No hay vehículos pendientes',
      description: 'Los vehículos en revisión aparecerán aquí',
    },
    paused: {
      title: 'No tienes vehículos pausados',
      description: 'Los vehículos que pauses temporalmente aparecerán aquí',
    },
    sold: {
      title: 'No has marcado ningún vehículo como vendido',
      description: 'Cuando vendas un vehículo, márcalo como vendido',
    },
    expired: {
      title: 'No tienes publicaciones expiradas',
      description: 'Las publicaciones que expiren aparecerán aquí',
    },
    rejected: {
      title: 'No tienes vehículos rechazados',
      description: 'Si algún anuncio es rechazado por el equipo de revisión, aparecerá aquí con los motivos',
    },
  };

  const { title, description } = messages[status];

  return (
    <div className="py-12 text-center">
      <div className="bg-muted mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full">
        <Car className="text-muted-foreground h-8 w-8" />
      </div>
      <h3 className="text-foreground mb-1 font-medium">{title}</h3>
      <p className="text-muted-foreground mb-6">{description}</p>
      {status === 'all' && (
        <Button asChild>
          <Link href="/publicar">
            <Plus className="mr-2 h-4 w-4" />
            Publicar vehículo
          </Link>
        </Button>
      )}
    </div>
  );
}

export default function MyVehiclesPage() {
  const [vehicles, setVehicles] = React.useState<UserVehicleDto[]>([]);
  const [isLoading, setIsLoading] = React.useState(true);
  const [selectedStatus, setSelectedStatus] = React.useState<VehicleStatus>('all');
  const [searchQuery, setSearchQuery] = React.useState('');

  // Load vehicles
  React.useEffect(() => {
    async function loadVehicles() {
      try {
        const data = await userService.getUserVehicles();
        setVehicles(data.vehicles);
      } catch {
        // Error already handled in userService - silently use empty array
        setVehicles([]);
      } finally {
        setIsLoading(false);
      }
    }
    loadVehicles();
  }, []);

  // Filter vehicles
  const filteredVehicles = React.useMemo(() => {
    return vehicles.filter(vehicle => {
      // Status filter
      if (selectedStatus !== 'all' && vehicle.status !== selectedStatus) {
        return false;
      }

      // Search filter
      if (searchQuery) {
        const search = searchQuery.toLowerCase();
        return vehicle.title.toLowerCase().includes(search);
      }

      return true;
    });
  }, [vehicles, selectedStatus, searchQuery]);

  // Status counts
  const statusCounts = React.useMemo(() => {
    const counts: Record<string, number> = { all: vehicles.length };
    vehicles.forEach(v => {
      counts[v.status] = (counts[v.status] || 0) + 1;
    });
    return counts;
  }, [vehicles]);

  // Actions
  const handlePause = async (id: string) => {
    try {
      await vehicleService.unpublish(id, 'Pausado por el vendedor');
      setVehicles(prev => prev.map(v => (v.id === id ? { ...v, status: 'paused' } : v)));
      toast.success('Vehículo pausado');
    } catch {
      toast.error('Error al pausar el vehículo');
    }
  };

  const handleActivate = async (id: string) => {
    try {
      await vehicleService.publish(id);
      setVehicles(prev => prev.map(v => (v.id === id ? { ...v, status: 'pending' } : v)));
      toast.success('Vehículo enviado a revisión nuevamente');
    } catch {
      toast.error('Error al reactivar el vehículo. Verifica que cumple los requisitos.');
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('¿Estás seguro de eliminar este vehículo? Esta acción no se puede deshacer.')) {
      return;
    }
    try {
      await vehicleService.delete(id);
      setVehicles(prev => prev.filter(v => v.id !== id));
      toast.success('Vehículo eliminado');
    } catch {
      toast.error('Error al eliminar el vehículo');
    }
  };

  const tabs: { value: VehicleStatus; label: string }[] = [
    { value: 'all', label: 'Todos' },
    { value: 'active', label: 'Activos' },
    { value: 'pending', label: 'En Revisión' },
    { value: 'rejected', label: 'Rechazados' },
    { value: 'paused', label: 'Pausados' },
    { value: 'sold', label: 'Vendidos' },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-foreground text-2xl font-bold">Mis Vehículos</h1>
          <p className="text-muted-foreground">{vehicles.length} publicaciones en total</p>
        </div>
        <Button asChild>
          <Link href="/publicar">
            <Plus className="mr-2 h-4 w-4" />
            Publicar vehículo
          </Link>
        </Button>
      </div>

      {/* Filters */}
      <div className="flex flex-col gap-4 sm:flex-row">
        {/* Search */}
        <div className="relative flex-1">
          <Search className="text-muted-foreground absolute top-1/2 left-3 h-5 w-5 -translate-y-1/2" />
          <Input
            type="text"
            placeholder="Buscar por título..."
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
            className="pl-10"
          />
        </div>
      </div>

      {/* Status Tabs */}
      <div className="border-border border-b">
        <div className="-mb-px flex gap-2 overflow-x-auto">
          {tabs.map(tab => (
            <button
              key={tab.value}
              onClick={() => setSelectedStatus(tab.value)}
              className={cn(
                'border-b-2 px-4 py-2 text-sm font-medium whitespace-nowrap transition-colors',
                selectedStatus === tab.value
                  ? 'border-primary text-primary'
                  : 'text-muted-foreground hover:border-border hover:text-foreground border-transparent'
              )}
            >
              {tab.label}
              {statusCounts[tab.value] > 0 && (
                <span className="text-muted-foreground ml-1.5 text-xs">
                  ({statusCounts[tab.value]})
                </span>
              )}
            </button>
          ))}
        </div>
      </div>

      {/* Content */}
      {isLoading ? (
        <div className="flex items-center justify-center py-12">
          <Loader2 className="text-primary h-8 w-8 animate-spin" />
        </div>
      ) : filteredVehicles.length > 0 ? (
        <div className="space-y-4">
          {filteredVehicles.map(vehicle => (
            <VehicleCard
              key={vehicle.id}
              vehicle={vehicle}
              onPause={handlePause}
              onActivate={handleActivate}
              onDelete={handleDelete}
            />
          ))}
        </div>
      ) : (
        <EmptyState status={selectedStatus} />
      )}
    </div>
  );
}
