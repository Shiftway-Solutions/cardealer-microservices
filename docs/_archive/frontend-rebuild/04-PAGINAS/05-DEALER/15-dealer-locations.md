# 📍 Ubicaciones del Dealer

> **Tiempo estimado:** 25 minutos  
> **Páginas:** DealerLocationsPage

---

## 📋 OBJETIVO

Gestión de sucursales/ubicaciones del dealer:

- CRUD de ubicaciones
- Mapa interactivo
- Horarios por ubicación

---

## 🎨 WIREFRAME

```
┌─────────────────────────────────────────────────────────────────┐
│ UBICACIONES                                   [+ Agregar]       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ ┌──────────────────────────┐ ┌────────────────────────────────┐ │
│ │                          │ │ Sucursal Principal    ★        │ │
│ │      [MAPA GOOGLE]       │ │ Av. Winston Churchill #45      │ │
│ │                          │ │ Santo Domingo                  │ │
│ │         📍 📍             │ │ Tel: 809-555-1234              │ │
│ │                          │ │ [Editar] [Eliminar]            │ │
│ │                          │ ├────────────────────────────────┤ │
│ │                          │ │ Sucursal Santiago              │ │
│ │                          │ │ Av. Juan Pablo Duarte #120     │ │
│ │                          │ │ Santiago                       │ │
│ │                          │ │ Tel: 809-555-5678              │ │
│ │                          │ │ [Editar] [Eliminar]            │ │
│ └──────────────────────────┘ └────────────────────────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 IMPLEMENTACIÓN

```typescript
// filepath: src/app/(dealer)/dealer/locations/page.tsx
'use client';

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { dealerService, type DealerLocation } from '@/services/api/dealerService';
import { GoogleMap, Marker } from '@/components/maps/GoogleMap';
import { useToast } from '@/hooks/use-toast';
import { MapPin, Plus, Star, Phone, Clock, Trash2, Edit } from 'lucide-react';

const locationSchema = z.object({
  name: z.string().min(2, 'Nombre requerido'),
  address: z.string().min(5, 'Dirección requerida'),
  city: z.string().min(2, 'Ciudad requerida'),
  phone: z.string().optional(),
  isPrimary: z.boolean().default(false),
  latitude: z.number().optional(),
  longitude: z.number().optional(),
});

type LocationFormData = z.infer<typeof locationSchema>;

export default function DealerLocationsPage() {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [editingLocation, setEditingLocation] = useState<DealerLocation | null>(null);

  const form = useForm<LocationFormData>({
    resolver: zodResolver(locationSchema),
    defaultValues: { name: '', address: '', city: '', phone: '', isPrimary: false },
  });

  const { data: locations } = useQuery({
    queryKey: ['dealer-locations'],
    queryFn: () => dealerService.getLocations(),
  });

  const createMutation = useMutation({
    mutationFn: (data: LocationFormData) => dealerService.createLocation(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dealer-locations'] });
      setIsDialogOpen(false);
      form.reset();
      toast({ title: 'Ubicación creada' });
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: LocationFormData }) =>
      dealerService.updateLocation(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dealer-locations'] });
      setIsDialogOpen(false);
      setEditingLocation(null);
      form.reset();
      toast({ title: 'Ubicación actualizada' });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => dealerService.deleteLocation(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dealer-locations'] });
      toast({ title: 'Ubicación eliminada' });
    },
  });

  const handleEdit = (location: DealerLocation) => {
    setEditingLocation(location);
    form.reset({
      name: location.name,
      address: location.address,
      city: location.city,
      phone: location.phone || '',
      isPrimary: location.isPrimary,
      latitude: location.latitude,
      longitude: location.longitude,
    });
    setIsDialogOpen(true);
  };

  const handleSubmit = (data: LocationFormData) => {
    if (editingLocation) {
      updateMutation.mutate({ id: editingLocation.id, data });
    } else {
      createMutation.mutate(data);
    }
  };

  const markers = locations?.map((loc: DealerLocation) => ({
    lat: loc.latitude,
    lng: loc.longitude,
    title: loc.name,
  })) || [];

  return (
    <div className="container max-w-6xl mx-auto py-8 px-4">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold">Ubicaciones</h1>
          <p className="text-gray-600">Gestiona las sucursales de tu dealer</p>
        </div>
        <Button onClick={() => { form.reset(); setEditingLocation(null); setIsDialogOpen(true); }}>
          <Plus className="w-4 h-4 mr-2" />
          Agregar Ubicación
        </Button>
      </div>

      <div className="grid lg:grid-cols-2 gap-6">
        {/* Map */}
        <Card className="h-[500px]">
          <CardContent className="p-0 h-full">
            <GoogleMap
              markers={markers}
              center={markers[0] || { lat: 18.4861, lng: -69.9312 }}
              zoom={10}
            />
          </CardContent>
        </Card>

        {/* Locations List */}
        <div className="space-y-4">
          {locations?.map((location: DealerLocation) => (
            <Card key={location.id}>
              <CardContent className="p-4">
                <div className="flex items-start justify-between">
                  <div>
                    <div className="flex items-center gap-2">
                      <h3 className="font-semibold">{location.name}</h3>
                      {location.isPrimary && (
                        <Badge variant="secondary">
                          <Star className="w-3 h-3 mr-1" />
                          Principal
                        </Badge>
                      )}
                    </div>

                    <div className="mt-2 space-y-1 text-sm text-gray-600">
                      <p className="flex items-center gap-2">
                        <MapPin className="w-4 h-4" />
                        {location.address}, {location.city}
                      </p>
                      {location.phone && (
                        <p className="flex items-center gap-2">
                          <Phone className="w-4 h-4" />
                          {location.phone}
                        </p>
                      )}
                    </div>
                  </div>

                  <div className="flex gap-2">
                    <Button variant="ghost" size="icon" onClick={() => handleEdit(location)}>
                      <Edit className="w-4 h-4" />
                    </Button>
                    {!location.isPrimary && (
                      <Button
                        variant="ghost"
                        size="icon"
                        className="text-red-600"
                        onClick={() => deleteMutation.mutate(location.id)}
                      >
                        <Trash2 className="w-4 h-4" />
                      </Button>
                    )}
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}

          {!locations?.length && (
            <Card>
              <CardContent className="py-12 text-center">
                <MapPin className="w-12 h-12 mx-auto text-gray-400 mb-4" />
                <p className="text-gray-600">No hay ubicaciones registradas</p>
              </CardContent>
            </Card>
          )}
        </div>
      </div>

      {/* Add/Edit Dialog */}
      <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {editingLocation ? 'Editar Ubicación' : 'Nueva Ubicación'}
            </DialogTitle>
          </DialogHeader>

          <Form {...form}>
            <form onSubmit={form.handleSubmit(handleSubmit)} className="space-y-4">
              <FormField
                control={form.control}
                name="name"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Nombre</FormLabel>
                    <FormControl>
                      <Input placeholder="Sucursal Principal" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="address"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Dirección</FormLabel>
                    <FormControl>
                      <Input placeholder="Av. Winston Churchill #45" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="city"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Ciudad</FormLabel>
                    <FormControl>
                      <Input placeholder="Santo Domingo" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="phone"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Teléfono</FormLabel>
                    <FormControl>
                      <Input placeholder="809-555-1234" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <div className="flex justify-end gap-2 pt-4">
                <Button type="button" variant="outline" onClick={() => setIsDialogOpen(false)}>
                  Cancelar
                </Button>
                <Button type="submit">
                  {editingLocation ? 'Guardar' : 'Crear'}
                </Button>
              </div>
            </form>
          </Form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
```

---

## 📡 ENDPOINTS

| Método   | Endpoint                      | Descripción          |
| -------- | ----------------------------- | -------------------- |
| `GET`    | `/api/dealers/locations`      | Lista ubicaciones    |
| `POST`   | `/api/dealers/locations`      | Crear ubicación      |
| `PUT`    | `/api/dealers/locations/{id}` | Actualizar ubicación |
| `DELETE` | `/api/dealers/locations/{id}` | Eliminar ubicación   |

---

## ✅ CHECKLIST

- [ ] Lista de ubicaciones
- [ ] Mapa con marcadores
- [ ] CRUD completo
- [ ] Marcar como principal
- [ ] Geocodificación de direcciones

---

_Última actualización: Enero 30, 2026_
