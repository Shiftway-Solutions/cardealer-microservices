/**
 * Edit Vehicle Page
 *
 * Allows sellers to edit their existing vehicle listings.
 * Shows rejection reason if vehicle was rejected, with re-submit option.
 */

'use client';

import * as React from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import {
  ArrowLeft,
  Save,
  Send,
  Loader2,
  AlertTriangle,
  Car,
  DollarSign,
  MapPin,
  FileText,
  ImageIcon,
  Info,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { toast } from 'sonner';
import Image from 'next/image';
import { vehicleService, type CatalogOption } from '@/services/vehicles';
import type { Vehicle } from '@/types';

export default function EditVehiclePage() {
  const params = useParams();
  const router = useRouter();
  const vehicleId = params.id as string;

  const [vehicle, setVehicle] = React.useState<Vehicle | null>(null);
  const [isLoading, setIsLoading] = React.useState(true);
  const [isSaving, setIsSaving] = React.useState(false);
  const [isSubmitting, setIsSubmitting] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);

  // Catalog options
  const [fuelTypes, setFuelTypes] = React.useState<CatalogOption[]>([]);
  const [transmissions, setTransmissions] = React.useState<CatalogOption[]>([]);
  const [bodyTypes, setBodyTypes] = React.useState<CatalogOption[]>([]);

  // Form state
  const [form, setForm] = React.useState({
    price: 0,
    currency: 'DOP' as 'DOP' | 'USD',
    mileage: 0,
    transmission: '',
    fuelType: '',
    bodyType: '',
    exteriorColor: '',
    interiorColor: '',
    condition: '',
    description: '',
    province: '',
    city: '',
    isNegotiable: false,
    features: [] as string[],
  });

  // Load vehicle and catalog data
  React.useEffect(() => {
    async function load() {
      try {
        const [v, fuels, trans, bodies] = await Promise.all([
          vehicleService.getById(vehicleId),
          vehicleService.getFuelTypes(),
          vehicleService.getTransmissions(),
          vehicleService.getBodyTypes(),
        ]);

        setVehicle(v);
        setFuelTypes(fuels);
        setTransmissions(trans);
        setBodyTypes(bodies);

        // Populate form with current vehicle data.
        // NOTE: v.fuelType comes from transformVehicle() which maps backend enum to Spanish
        // display labels (e.g. "Gasolina"). The Select value must use the catalog key (e.g.
        // "gasoline") for the option to be selected and for toEnum() to map it correctly on save.
        const fuelTypeKey =
          fuels.find(f => f.label === v.fuelType)?.value || v.fuelType?.toLowerCase() || '';
        setForm({
          price: v.price,
          currency: v.currency || 'DOP',
          mileage: v.mileage,
          transmission: v.transmission || '',
          fuelType: fuelTypeKey,
          bodyType: v.bodyType || '',
          exteriorColor: v.exteriorColor || '',
          interiorColor: v.interiorColor || '',
          condition: v.condition || '',
          description: v.description || '',
          province: v.location?.province || '',
          city: v.location?.city || '',
          isNegotiable: v.isNegotiable ?? false,
          features: v.features || [],
        });
      } catch {
        setError('No se pudo cargar el vehículo. Verifica que existe y tienes permisos.');
      } finally {
        setIsLoading(false);
      }
    }
    load();
  }, [vehicleId]);

  const handleSave = async () => {
    if (!vehicle) return;
    setIsSaving(true);
    try {
      await vehicleService.update(vehicleId, {
        price: form.price,
        currency: form.currency,
        mileage: form.mileage,
        transmission: form.transmission,
        fuelType: form.fuelType,
        condition: form.condition,
        description: form.description,
        province: form.province,
        city: form.city,
        isNegotiable: form.isNegotiable,
        features: form.features,
      });
      toast.success('Cambios guardados exitosamente');
    } catch {
      toast.error('Error al guardar los cambios');
    } finally {
      setIsSaving(false);
    }
  };

  const handleResubmit = async () => {
    if (!vehicle) return;
    setIsSubmitting(true);
    try {
      // Save changes first
      await vehicleService.update(vehicleId, {
        price: form.price,
        currency: form.currency,
        mileage: form.mileage,
        transmission: form.transmission,
        fuelType: form.fuelType,
        condition: form.condition,
        description: form.description,
        province: form.province,
        city: form.city,
        isNegotiable: form.isNegotiable,
        features: form.features,
      });
      // Then re-submit for review
      await vehicleService.publish(vehicleId);
      toast.success('Vehículo enviado a revisión nuevamente');
      router.push('/cuenta/mis-vehiculos');
    } catch (error: unknown) {
      const err = error as {
        message?: string;
        code?: string;
        requiresKyc?: boolean;
        redirectUrl?: string;
      };
      if (err.requiresKyc || err.code === 'HTTP_403') {
        toast.error(err.message || 'Debes verificar tu identidad antes de publicar.');
        router.push(err.redirectUrl || '/cuenta/verificacion');
        return;
      }
      toast.error('Error al re-enviar el vehículo a revisión');
    } finally {
      setIsSubmitting(false);
    }
  };

  const updateForm = (field: string, value: unknown) => {
    setForm(prev => ({ ...prev, [field]: value }));
  };

  if (isLoading) {
    return (
      <div className="mx-auto max-w-4xl space-y-6 p-6">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-4 w-96" />
        <div className="grid gap-6 md:grid-cols-2">
          <Skeleton className="h-48" />
          <Skeleton className="h-48" />
        </div>
      </div>
    );
  }

  if (error || !vehicle) {
    return (
      <div className="mx-auto max-w-4xl p-6">
        <Card>
          <CardContent className="py-12 text-center">
            <AlertTriangle className="mx-auto mb-4 h-12 w-12 text-red-500" />
            <h2 className="mb-2 text-xl font-semibold">Error al cargar</h2>
            <p className="text-muted-foreground mb-4">{error || 'No se encontró el vehículo'}</p>
            <Button asChild variant="outline">
              <Link href="/cuenta/mis-vehiculos">
                <ArrowLeft className="mr-2 h-4 w-4" />
                Volver a mis vehículos
              </Link>
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  const isRejected = vehicle.status === 'rejected';
  const isPending = vehicle.status === 'pending';

  return (
    <div className="mx-auto max-w-4xl space-y-6 p-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" asChild>
          <Link href="/cuenta/mis-vehiculos">
            <ArrowLeft className="h-5 w-5" />
          </Link>
        </Button>
        <div className="flex-1">
          <h1 className="text-foreground text-2xl font-bold">
            Editar: {vehicle.year} {vehicle.make} {vehicle.model}
          </h1>
          <p className="text-muted-foreground">
            {vehicle.trim && `${vehicle.trim} • `}
            {vehicle.vin && `VIN: ${vehicle.vin}`}
          </p>
        </div>
        <Badge
          variant="outline"
          className={
            isRejected
              ? 'border-red-200 bg-red-50 text-red-700'
              : isPending
                ? 'border-yellow-200 bg-yellow-50 text-yellow-700'
                : 'border-green-200 bg-green-50 text-green-700'
          }
        >
          {isRejected ? 'Rechazado' : isPending ? 'En Revisión' : vehicle.status}
        </Badge>
      </div>

      {/* Rejection reason banner */}
      {isRejected && vehicle.rejectionReason && (
        <Card className="border-red-200 bg-red-50">
          <CardContent className="flex items-start gap-3 p-4">
            <AlertTriangle className="mt-0.5 h-5 w-5 flex-shrink-0 text-red-600" />
            <div>
              <p className="font-medium text-red-800">Motivo del rechazo:</p>
              <p className="text-red-700">{vehicle.rejectionReason}</p>
              <p className="mt-2 text-sm text-red-600">
                Corrige los problemas señalados y re-envía tu vehículo a revisión.
              </p>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Pending review info */}
      {isPending && (
        <Card className="border-yellow-200 bg-yellow-50">
          <CardContent className="flex items-start gap-3 p-4">
            <Info className="mt-0.5 h-5 w-5 flex-shrink-0 text-yellow-600" />
            <div>
              <p className="font-medium text-yellow-800">En revisión</p>
              <p className="text-yellow-700">
                Tu vehículo está siendo revisado por nuestro equipo. Puedes editar los datos pero
                los cambios se aplicarán después de la revisión.
              </p>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Vehicle info (read-only) */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Car className="h-5 w-5" />
            Información del Vehículo
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <div>
              <Label className="text-muted-foreground text-xs">Marca</Label>
              <p className="font-medium">{vehicle.make}</p>
            </div>
            <div>
              <Label className="text-muted-foreground text-xs">Modelo</Label>
              <p className="font-medium">{vehicle.model}</p>
            </div>
            <div>
              <Label className="text-muted-foreground text-xs">Año</Label>
              <p className="font-medium">{vehicle.year}</p>
            </div>
            <div>
              <Label className="text-muted-foreground text-xs">VIN</Label>
              <p className="font-mono font-medium">{vehicle.vin || 'N/A'}</p>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Specs (editable) */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <FileText className="h-5 w-5" />
            Especificaciones
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <div className="space-y-2">
              <Label htmlFor="mileage">Kilometraje</Label>
              <Input
                id="mileage"
                type="number"
                value={form.mileage}
                onChange={e => updateForm('mileage', parseInt(e.target.value) || 0)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="transmission">Transmisión</Label>
              <Select value={form.transmission} onValueChange={v => updateForm('transmission', v)}>
                <SelectTrigger id="transmission">
                  <SelectValue placeholder="Seleccionar" />
                </SelectTrigger>
                <SelectContent>
                  {transmissions.map(t => (
                    <SelectItem key={t.value} value={t.value}>
                      {t.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="fuelType">Combustible</Label>
              <Select value={form.fuelType} onValueChange={v => updateForm('fuelType', v)}>
                <SelectTrigger id="fuelType">
                  <SelectValue placeholder="Seleccionar" />
                </SelectTrigger>
                <SelectContent>
                  {fuelTypes.map(f => (
                    <SelectItem key={f.value} value={f.value}>
                      {f.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="bodyType">Tipo de carrocería</Label>
              <Select value={form.bodyType} onValueChange={v => updateForm('bodyType', v)}>
                <SelectTrigger id="bodyType">
                  <SelectValue placeholder="Seleccionar" />
                </SelectTrigger>
                <SelectContent>
                  {bodyTypes.map(b => (
                    <SelectItem key={b.value} value={b.value}>
                      {b.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="condition">Condición</Label>
              <Select value={form.condition} onValueChange={v => updateForm('condition', v)}>
                <SelectTrigger id="condition">
                  <SelectValue placeholder="Seleccionar" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="new">Nuevo</SelectItem>
                  <SelectItem value="like-new">Casi Nuevo</SelectItem>
                  <SelectItem value="certified">Certificado Pre-Owned</SelectItem>
                  <SelectItem value="excellent">Excelente</SelectItem>
                  <SelectItem value="good">Bueno</SelectItem>
                  <SelectItem value="fair">Aceptable</SelectItem>
                  <SelectItem value="used">Usado</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="exteriorColor">Color Exterior</Label>
              <Input
                id="exteriorColor"
                value={form.exteriorColor}
                onChange={e => updateForm('exteriorColor', e.target.value)}
                placeholder="Ej: Blanco"
              />
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Pricing */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <DollarSign className="h-5 w-5" />
            Precio
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-3">
            <div className="space-y-2">
              <Label htmlFor="price">Precio</Label>
              <Input
                id="price"
                type="number"
                value={form.price}
                onChange={e => updateForm('price', parseInt(e.target.value) || 0)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="currency">Moneda</Label>
              <Select value={form.currency} onValueChange={v => updateForm('currency', v)}>
                <SelectTrigger id="currency">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="DOP">RD$ (DOP)</SelectItem>
                  <SelectItem value="USD">US$ (USD)</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="flex items-end gap-3 pb-1">
              <Switch
                id="negotiable"
                checked={form.isNegotiable}
                onCheckedChange={v => updateForm('isNegotiable', v)}
              />
              <Label htmlFor="negotiable">Negociable</Label>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Location */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <MapPin className="h-5 w-5" />
            Ubicación
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="province">Provincia</Label>
              <Input
                id="province"
                value={form.province}
                onChange={e => updateForm('province', e.target.value)}
                placeholder="Ej: Santo Domingo"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="city">Ciudad / Sector</Label>
              <Input
                id="city"
                value={form.city}
                onChange={e => updateForm('city', e.target.value)}
                placeholder="Ej: Piantini"
              />
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Description */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <FileText className="h-5 w-5" />
            Descripción
          </CardTitle>
        </CardHeader>
        <CardContent>
          <Textarea
            value={form.description}
            onChange={e => updateForm('description', e.target.value)}
            placeholder="Describe tu vehículo: estado, mantenimiento, accesorios adicionales..."
            rows={5}
          />
        </CardContent>
      </Card>

      {/* Images (read-only preview) */}
      {vehicle.images && vehicle.images.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <ImageIcon className="h-5 w-5" />
              Imágenes ({vehicle.images.length})
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-3 gap-3 sm:grid-cols-4 lg:grid-cols-6">
              {vehicle.images.map((img, idx) => (
                <div
                  key={img.id || idx}
                  className="bg-muted relative aspect-square overflow-hidden rounded-lg"
                >
                  <Image
                    src={img.url}
                    alt={img.alt || `Imagen ${idx + 1}`}
                    fill
                    className="object-cover"
                  />
                  {idx === 0 && (
                    <div className="absolute right-0 bottom-0 left-0 bg-black/60 px-2 py-0.5 text-center text-xs text-white">
                      Principal
                    </div>
                  )}
                </div>
              ))}
            </div>
            <p className="text-muted-foreground mt-2 text-sm">
              Para cambiar imágenes, publica un nuevo listado o contacta a soporte.
            </p>
          </CardContent>
        </Card>
      )}

      {/* Action buttons */}
      <div className="flex flex-col gap-3 sm:flex-row sm:justify-end">
        <Button variant="outline" asChild>
          <Link href="/cuenta/mis-vehiculos">Cancelar</Link>
        </Button>
        <Button onClick={handleSave} disabled={isSaving || isSubmitting} variant="outline">
          {isSaving ? (
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
          ) : (
            <Save className="mr-2 h-4 w-4" />
          )}
          Guardar Cambios
        </Button>
        {isRejected && (
          <Button onClick={handleResubmit} disabled={isSaving || isSubmitting}>
            {isSubmitting ? (
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <Send className="mr-2 h-4 w-4" />
            )}
            Guardar y Re-enviar a Revisión
          </Button>
        )}
      </div>
    </div>
  );
}
