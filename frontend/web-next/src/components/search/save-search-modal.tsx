'use client';

/**
 * SaveSearchModal
 *
 * Modal that allows authenticated users to save their current search filters.
 * Saved searches are accessible at /cuenta/busquedas and can trigger email
 * notifications when new matching vehicles are published.
 *
 * Based on patterns from CarGurus & AutoTrader save-search feature.
 */

import * as React from 'react';
import { Bell, BellOff, BookmarkPlus, Check, Loader2 } from 'lucide-react';
import { useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { useCreateSavedSearch } from '@/hooks/use-alerts';
import { useAuth } from '@/hooks/use-auth';
import type { VehicleSearchFilters } from '@/hooks/use-vehicle-search';

// =============================================================================
// SCHEMA
// =============================================================================

const saveSearchSchema = z.object({
  name: z
    .string()
    .min(2, 'El nombre debe tener al menos 2 caracteres')
    .max(60, 'Máximo 60 caracteres'),
  notifyNewListings: z.boolean(),
  notifyFrequency: z.enum(['instant', 'daily', 'weekly', 'never']),
});

type SaveSearchForm = z.infer<typeof saveSearchSchema>;

// =============================================================================
// HELPER — build human-readable default name
// =============================================================================

function buildDefaultName(filters: VehicleSearchFilters): string {
  const parts: string[] = [];
  if (filters.condition === 'nuevo') parts.push('Nuevo');
  if (filters.condition === 'usado') parts.push('Usado');
  if (filters.make) parts.push(filters.make);
  if (filters.model) parts.push(filters.model);
  if (filters.yearMin && filters.yearMax) parts.push(`${filters.yearMin}–${filters.yearMax}`);
  else if (filters.yearMin) parts.push(`desde ${filters.yearMin}`);
  if (filters.bodyType) parts.push(filters.bodyType);
  if (filters.province) parts.push(filters.province);
  if (filters.priceMax) parts.push(`hasta ${(filters.priceMax / 1000).toFixed(0)}K`);

  return parts.length > 0 ? parts.join(' ') : 'Mi búsqueda';
}

// =============================================================================
// COMPONENT
// =============================================================================

export interface SaveSearchModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  filters: VehicleSearchFilters;
  totalResults?: number;
}

export function SaveSearchModal({
  open,
  onOpenChange,
  filters,
  totalResults,
}: SaveSearchModalProps) {
  const { isAuthenticated } = useAuth();
  const createSavedSearch = useCreateSavedSearch();
  const [saved, setSaved] = React.useState(false);

  const {
    register,
    handleSubmit,
    control,
    setValue,
    reset,
    formState: { errors },
  } = useForm<SaveSearchForm>({
    resolver: zodResolver(saveSearchSchema),
    defaultValues: {
      name: buildDefaultName(filters),
      notifyNewListings: true,
      notifyFrequency: 'daily',
    },
  });

  // Update default name when filters change
  React.useEffect(() => {
    if (open) {
      reset({
        name: buildDefaultName(filters),
        notifyNewListings: true,
        notifyFrequency: 'daily',
      });
      setSaved(false);
    }
  }, [open, filters, reset]);

  const notifyEnabled = useWatch({ control, name: 'notifyNewListings' });
  const notifyFrequency = useWatch({ control, name: 'notifyFrequency' });

  const onSubmit = async (data: SaveSearchForm) => {
    if (!isAuthenticated) {
      toast.error('Debes iniciar sesión para guardar búsquedas');
      return;
    }

    // Map VehicleSearchFilters → VehicleSearchParams (flatten for API)
    const searchParams = {
      q: filters.query,
      make: filters.make,
      model: filters.model,
      yearMin: filters.yearMin,
      yearMax: filters.yearMax,
      priceMin: filters.priceMin,
      priceMax: filters.priceMax,
      mileageMax: filters.mileageMax,
      bodyType: filters.bodyType as import('@/types').VehicleBodyType | undefined,
      transmission: filters.transmission,
      fuelType: filters.fuelType,
      condition: filters.condition,
      province: filters.province,
      city: filters.city,
      drivetrain: filters.drivetrain,
      sellerType: filters.sellerType as 'dealer' | 'seller' | undefined,
      dealRating: filters.dealRating,
    };

    try {
      await createSavedSearch.mutateAsync({
        name: data.name,
        searchParams,
        notifyNewListings: data.notifyNewListings,
        notifyFrequency: data.notifyFrequency,
      });

      setSaved(true);
      toast.success('¡Búsqueda guardada!', {
        description: 'Accede a tus búsquedas en Mi Cuenta → Búsquedas',
        action: {
          label: 'Ver',
          onClick: () => {
            window.location.href = '/cuenta/busquedas';
          },
        },
      });

      setTimeout(() => {
        onOpenChange(false);
      }, 1500);
    } catch {
      toast.error('Error al guardar la búsqueda. Intenta de nuevo.');
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <BookmarkPlus className="h-5 w-5 text-[#00A870]" />
            Guardar búsqueda
          </DialogTitle>
          <DialogDescription>
            {totalResults !== undefined && totalResults > 0 && (
              <span className="font-medium text-[#00A870]">
                {totalResults.toLocaleString()} vehículos
              </span>
            )}{' '}
            coinciden con tus filtros actuales. Guárdalos para acceder rápidamente.
          </DialogDescription>
        </DialogHeader>

        {saved ? (
          <div className="flex flex-col items-center gap-3 py-6">
            <div className="flex h-14 w-14 items-center justify-center rounded-full bg-green-100">
              <Check className="h-7 w-7 text-green-600" />
            </div>
            <p className="text-foreground text-lg font-semibold">¡Guardado!</p>
            <p className="text-muted-foreground text-center text-sm">
              Puedes verla en <strong>Mi Cuenta → Búsquedas</strong>
            </p>
          </div>
        ) : (
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
            {/* Name */}
            <div className="space-y-1.5">
              <Label htmlFor="search-name">Nombre de la búsqueda</Label>
              <Input
                id="search-name"
                placeholder="Ej: Toyota SUV Santo Domingo"
                {...register('name')}
              />
              {errors.name && <p className="text-xs text-red-500">{errors.name.message}</p>}
            </div>

            {/* Notifications toggle */}
            <div className="border-border bg-muted/30 flex items-center justify-between rounded-lg border p-3">
              <div className="flex items-center gap-2">
                {notifyEnabled ? (
                  <Bell className="h-4 w-4 text-[#00A870]" />
                ) : (
                  <BellOff className="text-muted-foreground h-4 w-4" />
                )}
                <div>
                  <p className="text-sm font-medium">Alertas de nuevos vehículos</p>
                  <p className="text-muted-foreground text-xs">
                    Notifícame cuando publiquen uno nuevo
                  </p>
                </div>
              </div>
              <Switch
                checked={notifyEnabled}
                onCheckedChange={v => setValue('notifyNewListings', v)}
              />
            </div>

            {/* Frequency */}
            {notifyEnabled && (
              <div className="space-y-1.5">
                <Label>Frecuencia de notificación</Label>
                <Select
                  value={notifyFrequency}
                  onValueChange={v =>
                    setValue('notifyFrequency', v as SaveSearchForm['notifyFrequency'])
                  }
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="instant">Inmediatamente</SelectItem>
                    <SelectItem value="daily">Resumen diario</SelectItem>
                    <SelectItem value="weekly">Resumen semanal</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            )}

            <DialogFooter className="gap-2 sm:gap-0">
              <Button type="button" variant="ghost" onClick={() => onOpenChange(false)}>
                Cancelar
              </Button>
              <Button
                type="submit"
                className="bg-[#00A870] hover:bg-[#008a5c]"
                disabled={createSavedSearch.isPending}
              >
                {createSavedSearch.isPending ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    Guardando...
                  </>
                ) : (
                  <>
                    <BookmarkPlus className="mr-2 h-4 w-4" />
                    Guardar búsqueda
                  </>
                )}
              </Button>
            </DialogFooter>
          </form>
        )}
      </DialogContent>
    </Dialog>
  );
}
