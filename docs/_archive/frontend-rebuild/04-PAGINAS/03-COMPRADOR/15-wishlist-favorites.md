# ❤️ Wishlist y Favoritos

> **Tiempo estimado:** 20 minutos  
> **Página:** WishlistPage, FavoritesPage

---

## 📋 OBJETIVO

Gestión de vehículos guardados:

- Agregar/quitar favoritos
- Lista de deseos organizada
- Alertas de cambios de precio
- Compartir lista

---

## 🎨 WIREFRAME

```
┌─────────────────────────────────────────────────────────────────┐
│ MIS FAVORITOS (12)                    [Ordenar ▼] [Compartir]   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ [Imagen]  Toyota Camry 2024              ❤️                 │ │
│ │           $1,850,000 • 15,000 km                            │ │
│ │           Santo Domingo                                      │ │
│ │           Guardado hace 3 días                               │ │
│ │           🔔 Alertarme si baja el precio                     │ │
│ │           [Ver detalle] [Contactar] [Quitar]                │ │
│ ├─────────────────────────────────────────────────────────────┤ │
│ │ [Imagen]  Honda CR-V 2023                ❤️ 📉 -$50,000     │ │
│ │           $2,050,000 (antes $2,100,000)                     │ │
│ │           Santiago                                           │ │
│ │           Guardado hace 1 semana                             │ │
│ │           ✅ Alerta de precio activa                         │ │
│ │           [Ver detalle] [Contactar] [Quitar]                │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ Mostrando 2 de 12 • [Cargar más]                                │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 IMPLEMENTACIÓN

```typescript
// filepath: src/app/(user)/favorites/page.tsx
'use client';

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Switch } from '@/components/ui/switch';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { userService } from '@/services/api/userService';
import { formatCurrency, formatRelativeTime } from '@/lib/format';
import { Heart, Bell, BellOff, Share2, Trash2, TrendingDown, ExternalLink, MessageCircle } from 'lucide-react';
import { toast } from 'sonner';
import Link from 'next/link';
import Image from 'next/image';

export default function FavoritesPage() {
  const [sortBy, setSortBy] = useState('recent');
  const queryClient = useQueryClient();

  const { data: favorites, isLoading } = useQuery({
    queryKey: ['user-favorites', sortBy],
    queryFn: () => userService.getFavorites(sortBy),
  });

  const removeFavoriteMutation = useMutation({
    mutationFn: (vehicleId: string) => userService.removeFavorite(vehicleId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['user-favorites'] });
      toast.success('Removido de favoritos');
    },
  });

  const toggleAlertMutation = useMutation({
    mutationFn: ({ vehicleId, enabled }: { vehicleId: string; enabled: boolean }) =>
      userService.togglePriceAlert(vehicleId, enabled),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['user-favorites'] });
      toast.success('Alerta actualizada');
    },
  });

  const handleShare = async () => {
    const url = `${window.location.origin}/shared-list/${favorites?.shareId}`;
    await navigator.clipboard.writeText(url);
    toast.success('Link copiado al portapapeles');
  };

  if (isLoading) {
    return <div className="container py-8 text-center">Cargando favoritos...</div>;
  }

  return (
    <div className="container max-w-3xl mx-auto py-8 px-4">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold flex items-center gap-2">
          <Heart className="w-6 h-6 text-red-500 fill-current" />
          Mis Favoritos ({favorites?.items?.length || 0})
        </h1>
        <div className="flex gap-2">
          <Select value={sortBy} onValueChange={setSortBy}>
            <SelectTrigger className="w-40">
              <SelectValue placeholder="Ordenar" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="recent">Más recientes</SelectItem>
              <SelectItem value="price_asc">Menor precio</SelectItem>
              <SelectItem value="price_desc">Mayor precio</SelectItem>
              <SelectItem value="price_drop">Bajaron de precio</SelectItem>
            </SelectContent>
          </Select>
          <Button variant="outline" onClick={handleShare}>
            <Share2 className="w-4 h-4 mr-2" />
            Compartir
          </Button>
        </div>
      </div>

      {/* Empty State */}
      {favorites?.items?.length === 0 ? (
        <Card>
          <CardContent className="py-12 text-center">
            <Heart className="w-12 h-12 mx-auto text-gray-300 mb-4" />
            <p className="text-gray-600 mb-4">No tienes vehículos guardados</p>
            <Button asChild>
              <Link href="/vehicles">Explorar vehículos</Link>
            </Button>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-4">
          {favorites?.items?.map((fav: any) => (
            <Card key={fav.id} className="overflow-hidden">
              <CardContent className="p-0">
                <div className="flex">
                  {/* Image */}
                  <div className="relative w-40 h-32 flex-shrink-0">
                    <Image
                      src={fav.vehicle.imageUrl || '/placeholder-vehicle.jpg'}
                      alt={fav.vehicle.title}
                      fill
                      className="object-cover"
                    />
                    {fav.priceChange && fav.priceChange < 0 && (
                      <Badge className="absolute top-2 left-2 bg-green-600">
                        <TrendingDown className="w-3 h-3 mr-1" />
                        {formatCurrency(Math.abs(fav.priceChange))}
                      </Badge>
                    )}
                  </div>

                  {/* Content */}
                  <div className="flex-1 p-4">
                    <div className="flex items-start justify-between mb-2">
                      <Link
                        href={`/vehicles/${fav.vehicle.slug}`}
                        className="font-semibold hover:text-primary-600"
                      >
                        {fav.vehicle.title}
                      </Link>
                      <Button
                        size="sm"
                        variant="ghost"
                        className="text-red-500"
                        onClick={() => removeFavoriteMutation.mutate(fav.vehicleId)}
                      >
                        <Trash2 className="w-4 h-4" />
                      </Button>
                    </div>

                    <div className="mb-2">
                      <span className="text-lg font-bold text-primary-600">
                        {formatCurrency(fav.vehicle.price)}
                      </span>
                      {fav.vehicle.previousPrice && (
                        <span className="text-sm text-gray-500 line-through ml-2">
                          {formatCurrency(fav.vehicle.previousPrice)}
                        </span>
                      )}
                    </div>

                    <div className="text-sm text-gray-600 mb-3">
                      {fav.vehicle.mileage?.toLocaleString()} km • {fav.vehicle.location}
                    </div>

                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <Switch
                          checked={fav.priceAlertEnabled}
                          onCheckedChange={(checked) =>
                            toggleAlertMutation.mutate({ vehicleId: fav.vehicleId, enabled: checked })
                          }
                        />
                        <span className="text-sm text-gray-600">
                          {fav.priceAlertEnabled ? (
                            <>
                              <Bell className="w-4 h-4 inline text-primary-600" /> Alerta activa
                            </>
                          ) : (
                            <>
                              <BellOff className="w-4 h-4 inline" /> Alertarme
                            </>
                          )}
                        </span>
                      </div>

                      <div className="flex gap-2">
                        <Button size="sm" variant="outline" asChild>
                          <Link href={`/vehicles/${fav.vehicle.slug}`}>
                            <ExternalLink className="w-4 h-4 mr-1" />
                            Ver
                          </Link>
                        </Button>
                        <Button size="sm">
                          <MessageCircle className="w-4 h-4 mr-1" />
                          Contactar
                        </Button>
                      </div>
                    </div>

                    <p className="text-xs text-gray-500 mt-2">
                      Guardado {formatRelativeTime(fav.createdAt)}
                    </p>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
```

### Botón de Favorito (Componente Reutilizable)

```typescript
// filepath: src/components/vehicles/FavoriteButton.tsx
'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Button } from '@/components/ui/button';
import { userService } from '@/services/api/userService';
import { useAuth } from '@/hooks/useAuth';
import { Heart } from 'lucide-react';
import { toast } from 'sonner';
import { useRouter } from 'next/navigation';
import { cn } from '@/lib/utils';

interface FavoriteButtonProps {
  vehicleId: string;
  variant?: 'icon' | 'button';
  className?: string;
}

export function FavoriteButton({ vehicleId, variant = 'icon', className }: FavoriteButtonProps) {
  const { isAuthenticated } = useAuth();
  const router = useRouter();
  const queryClient = useQueryClient();

  const { data: isFavorite } = useQuery({
    queryKey: ['is-favorite', vehicleId],
    queryFn: () => userService.checkFavorite(vehicleId),
    enabled: isAuthenticated,
  });

  const toggleMutation = useMutation({
    mutationFn: () =>
      isFavorite
        ? userService.removeFavorite(vehicleId)
        : userService.addFavorite(vehicleId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['is-favorite', vehicleId] });
      queryClient.invalidateQueries({ queryKey: ['user-favorites'] });
      toast.success(isFavorite ? 'Removido de favoritos' : 'Agregado a favoritos');
    },
  });

  const handleClick = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();

    if (!isAuthenticated) {
      router.push(`/login?redirect=/vehicles/${vehicleId}`);
      return;
    }

    toggleMutation.mutate();
  };

  if (variant === 'icon') {
    return (
      <button
        onClick={handleClick}
        className={cn(
          'p-2 rounded-full transition-colors',
          isFavorite
            ? 'bg-red-100 text-red-500'
            : 'bg-white/80 text-gray-600 hover:bg-white',
          className
        )}
      >
        <Heart className={cn('w-5 h-5', isFavorite && 'fill-current')} />
      </button>
    );
  }

  return (
    <Button
      variant={isFavorite ? 'destructive' : 'outline'}
      onClick={handleClick}
      className={className}
    >
      <Heart className={cn('w-4 h-4 mr-2', isFavorite && 'fill-current')} />
      {isFavorite ? 'En favoritos' : 'Agregar a favoritos'}
    </Button>
  );
}
```

---

## 📡 ENDPOINTS

| Método   | Endpoint                                 | Descripción              |
| -------- | ---------------------------------------- | ------------------------ |
| `GET`    | `/api/users/favorites?sort=recent`       | Listar favoritos         |
| `POST`   | `/api/users/favorites`                   | Agregar favorito         |
| `DELETE` | `/api/users/favorites/{vehicleId}`       | Quitar favorito          |
| `GET`    | `/api/users/favorites/{vehicleId}/check` | Verificar si es favorito |
| `PUT`    | `/api/users/favorites/{vehicleId}/alert` | Toggle alerta de precio  |
| `GET`    | `/api/shared-list/{shareId}`             | Lista compartida pública |

---

## ✅ CHECKLIST

- [ ] Lista de favoritos con imagen y detalles
- [ ] Ordenamiento (reciente, precio, bajaron)
- [ ] Botón de favorito reutilizable
- [ ] Toggle de alerta de precio
- [ ] Indicador de bajada de precio
- [ ] Compartir lista
- [ ] Estado vacío con CTA

---

_Última actualización: Enero 31, 2026_
