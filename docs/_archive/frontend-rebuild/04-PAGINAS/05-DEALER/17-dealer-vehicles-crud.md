# 🚗 CRUD de Vehículos del Dealer

> **Tiempo estimado:** 40 minutos  
> **Páginas:** DealerVehiclesPage, CreateVehiclePage, EditVehiclePage

---

## 📋 OBJETIVO

Gestión completa de inventario de vehículos:

- Lista con filtros y búsqueda
- Crear/editar vehículos
- Acciones en lote
- Estados de publicación

---

## 🎨 WIREFRAME

```
┌─────────────────────────────────────────────────────────────────┐
│ INVENTARIO                    [Importar CSV] [+ Nuevo Vehículo] │
├─────────────────────────────────────────────────────────────────┤
│ [Buscar...            ] [Estado ▼] [Ordenar ▼]                  │
│                                                                 │
│ ☑ Seleccionar todo    [Pausar] [Destacar] [Eliminar]           │
│                                                                 │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ ☐ 🚗 Toyota Camry 2024        $1,850,000   ✓ Activo         │ │
│ │      VIN: 1HGBH41...          15 vistas    [Editar] [...]   │ │
│ └─────────────────────────────────────────────────────────────┘ │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ ☐ 🚗 Honda CR-V 2023          $2,100,000   ⏸ Pausado        │ │
│ │      VIN: 5J6RW2...           8 vistas     [Editar] [...]   │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ Mostrando 1-10 de 45 vehículos     [◀] 1 2 3 4 5 [▶]           │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 IMPLEMENTACIÓN

### Lista de Vehículos del Dealer

```typescript
// filepath: src/app/(dealer)/dealer/vehicles/page.tsx
'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Checkbox } from '@/components/ui/checkbox';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { dealerService } from '@/services/api/dealerService';
import { useToast } from '@/hooks/use-toast';
import { formatCurrency } from '@/lib/format';
import {
  Plus, Upload, Search, MoreHorizontal, Edit, Trash2,
  Eye, Pause, Play, Star, ExternalLink
} from 'lucide-react';

const statusOptions = [
  { value: 'all', label: 'Todos' },
  { value: 'active', label: 'Activos' },
  { value: 'paused', label: 'Pausados' },
  { value: 'draft', label: 'Borradores' },
  { value: 'sold', label: 'Vendidos' },
];

export default function DealerVehiclesPage() {
  const queryClient = useQueryClient();
  const { toast } = useToast();

  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('all');
  const [selected, setSelected] = useState<string[]>([]);
  const [page, setPage] = useState(1);

  const { data, isLoading } = useQuery({
    queryKey: ['dealer-vehicles', { search, status, page }],
    queryFn: () => dealerService.getVehicles({ search, status, page, limit: 10 }),
  });

  const updateStatusMutation = useMutation({
    mutationFn: ({ ids, status }: { ids: string[]; status: string }) =>
      dealerService.bulkUpdateVehicleStatus(ids, status),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dealer-vehicles'] });
      setSelected([]);
      toast({ title: 'Vehículos actualizados' });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (ids: string[]) => dealerService.bulkDeleteVehicles(ids),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dealer-vehicles'] });
      setSelected([]);
      toast({ title: 'Vehículos eliminados' });
    },
  });

  const toggleSelect = (id: string) => {
    setSelected(prev =>
      prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id]
    );
  };

  const toggleSelectAll = () => {
    if (selected.length === data?.items?.length) {
      setSelected([]);
    } else {
      setSelected(data?.items?.map((v: any) => v.id) || []);
    }
  };

  return (
    <div className="container max-w-6xl mx-auto py-8 px-4">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-6">
        <div>
          <h1 className="text-2xl font-bold">Inventario</h1>
          <p className="text-gray-600">
            {data?.total || 0} vehículos | {data?.activeCount || 0} activos
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" asChild>
            <Link href="/dealer/vehicles/import">
              <Upload className="w-4 h-4 mr-2" />
              Importar CSV
            </Link>
          </Button>
          <Button asChild>
            <Link href="/dealer/vehicles/new">
              <Plus className="w-4 h-4 mr-2" />
              Nuevo Vehículo
            </Link>
          </Button>
        </div>
      </div>

      {/* Filters */}
      <div className="flex flex-col md:flex-row gap-4 mb-6">
        <div className="relative flex-grow">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
          <Input
            placeholder="Buscar por marca, modelo, VIN..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-10"
          />
        </div>
        <Select value={status} onValueChange={setStatus}>
          <SelectTrigger className="w-40">
            <SelectValue placeholder="Estado" />
          </SelectTrigger>
          <SelectContent>
            {statusOptions.map(opt => (
              <SelectItem key={opt.value} value={opt.value}>
                {opt.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {/* Bulk Actions */}
      {selected.length > 0 && (
        <div className="bg-primary-50 border border-primary-200 rounded-lg p-3 mb-4 flex items-center gap-4">
          <span className="text-sm font-medium">
            {selected.length} seleccionados
          </span>
          <div className="flex gap-2">
            <Button
              size="sm"
              variant="outline"
              onClick={() => updateStatusMutation.mutate({ ids: selected, status: 'paused' })}
            >
              <Pause className="w-3 h-3 mr-1" /> Pausar
            </Button>
            <Button
              size="sm"
              variant="outline"
              onClick={() => updateStatusMutation.mutate({ ids: selected, status: 'active' })}
            >
              <Play className="w-3 h-3 mr-1" /> Activar
            </Button>
            <Button
              size="sm"
              variant="destructive"
              onClick={() => deleteMutation.mutate(selected)}
            >
              <Trash2 className="w-3 h-3 mr-1" /> Eliminar
            </Button>
          </div>
        </div>
      )}

      {/* Vehicles List */}
      <Card>
        <CardContent className="p-0">
          {/* Header Row */}
          <div className="flex items-center gap-4 p-4 border-b bg-gray-50">
            <Checkbox
              checked={selected.length === data?.items?.length && data?.items?.length > 0}
              onCheckedChange={toggleSelectAll}
            />
            <span className="text-sm font-medium text-gray-600">Seleccionar todo</span>
          </div>

          {/* Vehicle Rows */}
          <div className="divide-y">
            {data?.items?.map((vehicle: any) => (
              <div key={vehicle.id} className="flex items-center gap-4 p-4 hover:bg-gray-50">
                <Checkbox
                  checked={selected.includes(vehicle.id)}
                  onCheckedChange={() => toggleSelect(vehicle.id)}
                />

                {/* Image */}
                <div className="w-20 h-14 bg-gray-100 rounded overflow-hidden flex-shrink-0">
                  {vehicle.mainImage ? (
                    <img src={vehicle.mainImage} alt="" className="w-full h-full object-cover" />
                  ) : (
                    <div className="w-full h-full flex items-center justify-center text-gray-400">
                      🚗
                    </div>
                  )}
                </div>

                {/* Info */}
                <div className="flex-grow min-w-0">
                  <div className="flex items-center gap-2">
                    <Link
                      href={`/dealer/vehicles/${vehicle.id}/edit`}
                      className="font-medium hover:text-primary truncate"
                    >
                      {vehicle.year} {vehicle.make} {vehicle.model}
                    </Link>
                    {vehicle.isFeatured && (
                      <Star className="w-4 h-4 text-yellow-500 fill-yellow-500" />
                    )}
                  </div>
                  <p className="text-sm text-gray-600">VIN: {vehicle.vin}</p>
                </div>

                {/* Price */}
                <div className="text-right">
                  <div className="font-semibold">{formatCurrency(vehicle.price)}</div>
                  <div className="text-sm text-gray-600">{vehicle.views} vistas</div>
                </div>

                {/* Status */}
                <Badge
                  variant={
                    vehicle.status === 'active' ? 'default' :
                    vehicle.status === 'paused' ? 'secondary' :
                    vehicle.status === 'sold' ? 'outline' : 'destructive'
                  }
                >
                  {vehicle.status === 'active' ? 'Activo' :
                   vehicle.status === 'paused' ? 'Pausado' :
                   vehicle.status === 'sold' ? 'Vendido' : 'Borrador'}
                </Badge>

                {/* Actions */}
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="ghost" size="icon">
                      <MoreHorizontal className="w-4 h-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    <DropdownMenuItem asChild>
                      <Link href={`/dealer/vehicles/${vehicle.id}/edit`}>
                        <Edit className="w-4 h-4 mr-2" /> Editar
                      </Link>
                    </DropdownMenuItem>
                    <DropdownMenuItem asChild>
                      <Link href={`/vehicles/${vehicle.slug}`} target="_blank">
                        <ExternalLink className="w-4 h-4 mr-2" /> Ver publicación
                      </Link>
                    </DropdownMenuItem>
                    <DropdownMenuItem
                      onClick={() => updateStatusMutation.mutate({
                        ids: [vehicle.id],
                        status: vehicle.status === 'active' ? 'paused' : 'active'
                      })}
                    >
                      {vehicle.status === 'active' ? (
                        <><Pause className="w-4 h-4 mr-2" /> Pausar</>
                      ) : (
                        <><Play className="w-4 h-4 mr-2" /> Activar</>
                      )}
                    </DropdownMenuItem>
                    <DropdownMenuItem
                      className="text-red-600"
                      onClick={() => deleteMutation.mutate([vehicle.id])}
                    >
                      <Trash2 className="w-4 h-4 mr-2" /> Eliminar
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </div>
            ))}
          </div>

          {/* Empty State */}
          {!isLoading && (!data?.items || data.items.length === 0) && (
            <div className="py-12 text-center">
              <p className="text-gray-600 mb-4">No hay vehículos</p>
              <Button asChild>
                <Link href="/dealer/vehicles/new">Agregar primer vehículo</Link>
              </Button>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Pagination */}
      {data?.totalPages > 1 && (
        <div className="flex justify-center gap-2 mt-6">
          <Button
            variant="outline"
            size="sm"
            disabled={page === 1}
            onClick={() => setPage(p => p - 1)}
          >
            Anterior
          </Button>
          <span className="flex items-center px-4 text-sm">
            Página {page} de {data.totalPages}
          </span>
          <Button
            variant="outline"
            size="sm"
            disabled={page === data.totalPages}
            onClick={() => setPage(p => p + 1)}
          >
            Siguiente
          </Button>
        </div>
      )}
    </div>
  );
}
```

---

## 📡 ENDPOINTS

| Método   | Endpoint                            | Descripción                |
| -------- | ----------------------------------- | -------------------------- |
| `GET`    | `/api/dealers/vehicles`             | Lista vehículos del dealer |
| `POST`   | `/api/vehicles`                     | Crear vehículo             |
| `PUT`    | `/api/vehicles/{id}`                | Actualizar vehículo        |
| `DELETE` | `/api/vehicles/{id}`                | Eliminar vehículo          |
| `POST`   | `/api/dealers/vehicles/bulk-status` | Cambiar estado en lote     |
| `DELETE` | `/api/dealers/vehicles/bulk`        | Eliminar en lote           |

---

## ✅ CHECKLIST

- [ ] Lista con paginación
- [ ] Búsqueda y filtros
- [ ] Selección múltiple
- [ ] Acciones en lote
- [ ] Estados de vehículo
- [ ] Navegación a edición

---

_Última actualización: Enero 30, 2026_
