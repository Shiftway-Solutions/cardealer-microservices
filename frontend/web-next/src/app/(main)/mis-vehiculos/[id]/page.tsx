/**
 * Edit Vehicle Page
 *
 * Edit an existing vehicle listing owned by the user
 *
 * Route: /mis-vehiculos/[id]
 */

'use client';

import { useState, useEffect } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { useQueryClient } from '@tanstack/react-query';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
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
  Car,
  Camera,
  DollarSign,
  Settings,
  ArrowLeft,
  Save,
  Trash2,
  Eye,
  Pause,
  Play,
  AlertCircle,
  CheckCircle,
  RefreshCw,
  Loader2,
  RotateCw,
} from 'lucide-react';
import { toast } from 'sonner';
import Image from 'next/image';
import {
  useVehicle,
  useUpdateVehicle,
  useDeleteVehicle,
  useMakes,
  useFuelTypes,
  useTransmissions,
  useColors,
  useProvinces,
} from '@/hooks/use-vehicles';
import type { UpdateVehicleRequest } from '@/services/vehicles';
import { Vehicle360UploadWizard } from '@/components/vehicles/Vehicle360UploadWizard';

// =============================================================================
// TYPES
// =============================================================================

interface VehicleFormData {
  make: string;
  model: string;
  year: string;
  trim: string;
  price: string;
  mileage: string;
  fuelType: string;
  transmission: string;
  exteriorColor: string;
  condition: string;
  doors: string;
  province: string;
  city: string;
  description: string;
  isNegotiable: boolean;
  status: string;
}

// =============================================================================
// LOADING SKELETON
// =============================================================================

function EditVehicleSkeleton() {
  return (
    <div className="bg-muted/50 min-h-screen">
      <div className="mx-auto max-w-5xl px-4 py-8">
        {/* Header skeleton */}
        <div className="mb-6 flex items-center justify-between">
          <div className="flex items-center gap-4">
            <Skeleton className="h-10 w-10" />
            <div>
              <Skeleton className="mb-2 h-8 w-48" />
              <Skeleton className="h-4 w-32" />
            </div>
          </div>
          <div className="flex items-center gap-2">
            <Skeleton className="h-6 w-16" />
            <Skeleton className="h-10 w-32" />
          </div>
        </div>

        {/* Tabs skeleton */}
        <Skeleton className="mb-6 h-10 w-full max-w-md" />

        {/* Card skeleton */}
        <Card>
          <CardHeader>
            <Skeleton className="mb-2 h-6 w-48" />
            <Skeleton className="h-4 w-64" />
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
              {Array.from({ length: 6 }).map((_, i) => (
                <div key={i}>
                  <Skeleton className="mb-2 h-4 w-20" />
                  <Skeleton className="h-10 w-full" />
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

// =============================================================================
// ERROR STATE
// =============================================================================

function EditVehicleError({ message, onRetry }: { message: string; onRetry: () => void }) {
  return (
    <div className="bg-muted/50 min-h-screen">
      <div className="mx-auto max-w-5xl px-4 py-8">
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-16">
            <AlertCircle className="mb-4 h-16 w-16 text-red-400" />
            <h2 className="mb-2 text-xl font-semibold">Error al cargar el vehículo</h2>
            <p className="text-muted-foreground mb-6">{message}</p>
            <div className="flex gap-3">
              <Link href="/mis-vehiculos">
                <Button variant="outline">
                  <ArrowLeft className="mr-2 h-4 w-4" />
                  Volver
                </Button>
              </Link>
              <Button onClick={onRetry}>
                <RefreshCw className="mr-2 h-4 w-4" />
                Reintentar
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

// =============================================================================
// CONDITIONS OPTIONS
// =============================================================================

const conditions = [
  { value: 'New', label: 'Nuevo' },
  { value: 'CertifiedPreOwned', label: 'Certificado Pre-Owned' },
  { value: 'Used', label: 'Usado' },
  { value: 'Salvage', label: 'Salvamento' },
  { value: 'Rebuilt', label: 'Reconstruido' },
];

// =============================================================================
// MAIN COMPONENT
// =============================================================================

export default function EditVehiclePage() {
  const params = useParams();
  const router = useRouter();
  const _queryClient = useQueryClient();
  const vehicleId = params.id as string;

  // Fetch vehicle data
  const { data: vehicle, isLoading, error, refetch } = useVehicle(vehicleId);

  // Fetch catalog data
  const { data: makes = [] } = useMakes();
  const { data: fuelTypes = [] } = useFuelTypes();
  const { data: transmissions = [] } = useTransmissions();
  const { data: colors = [] } = useColors();
  const { data: provinces = [] } = useProvinces();

  // Mutations
  const updateMutation = useUpdateVehicle();
  const deleteMutation = useDeleteVehicle();

  // Form state
  const [formData, setFormData] = useState<VehicleFormData>({
    make: '',
    model: '',
    year: '',
    trim: '',
    price: '',
    mileage: '',
    fuelType: '',
    transmission: '',
    exteriorColor: '',
    condition: '',
    doors: '4',
    province: '',
    city: '',
    description: '',
    isNegotiable: false,
    status: 'active',
  });

  // Initialize form data when vehicle loads
  useEffect(() => {
    if (vehicle) {
      // eslint-disable-next-line react-hooks/exhaustive-deps
      setFormData({
        make: vehicle.make || '',
        model: vehicle.model || '',
        year: String(vehicle.year) || '',
        trim: vehicle.trim || '',
        price: String(vehicle.price) || '',
        mileage: String(vehicle.mileage) || '',
        fuelType: vehicle.fuelType || '',
        transmission: vehicle.transmission || '',
        exteriorColor: vehicle.exteriorColor || '',
        condition: vehicle.condition || '',
        doors: String(vehicle.doors || 4),
        province: vehicle.location?.province || '',
        city: vehicle.location?.city || '',
        description: vehicle.description || '',
        isNegotiable: vehicle.isNegotiable ?? false,
        status: vehicle.status || 'active',
      });
    }
  }, [vehicle]);

  // Handle form changes
  const handleChange = (field: keyof VehicleFormData, value: string | boolean) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  // Handle save
  const handleSave = async () => {
    const updateData: UpdateVehicleRequest = {
      make: formData.make,
      model: formData.model,
      year: parseInt(formData.year),
      trim: formData.trim || undefined,
      price: parseInt(formData.price),
      mileage: parseInt(formData.mileage),
      fuelType: formData.fuelType,
      transmission: formData.transmission,
      exteriorColor: formData.exteriorColor || undefined,
      condition: formData.condition,
      description: formData.description || undefined,
      city: formData.city,
      province: formData.province,
      isNegotiable: formData.isNegotiable,
    };

    try {
      await updateMutation.mutateAsync({ id: vehicleId, data: updateData });
      toast.success('Vehículo actualizado correctamente');
    } catch {
      toast.error('Error al actualizar el vehículo');
    }
  };

  // Handle status change
  const handleStatusChange = async (newStatus: 'active' | 'paused') => {
    try {
      await updateMutation.mutateAsync({
        id: vehicleId,
        data: { status: newStatus },
      });
      setFormData(prev => ({ ...prev, status: newStatus }));
      toast.success(newStatus === 'active' ? 'Publicación activada' : 'Publicación pausada');
    } catch {
      toast.error('Error al cambiar el estado');
    }
  };

  // Handle delete
  const handleDelete = async () => {
    try {
      await deleteMutation.mutateAsync(vehicleId);
      toast.success('Vehículo eliminado correctamente');
      router.push('/mis-vehiculos');
    } catch {
      toast.error('Error al eliminar el vehículo');
    }
  };

  // Loading state
  if (isLoading) {
    return <EditVehicleSkeleton />;
  }

  // Error state
  if (error) {
    return (
      <EditVehicleError
        message="No se pudo cargar la información del vehículo"
        onRetry={() => refetch()}
      />
    );
  }

  // Not found
  if (!vehicle) {
    return <EditVehicleError message="Vehículo no encontrado" onRetry={() => refetch()} />;
  }

  const isSaving = updateMutation.isPending;
  const isDeleting = deleteMutation.isPending;

  return (
    <div className="bg-muted/50 min-h-screen">
      <div className="mx-auto max-w-5xl px-4 py-8">
        {/* Header */}
        <div className="mb-6 flex items-center justify-between">
          <div className="flex items-center gap-4">
            <Link href="/mis-vehiculos">
              <Button variant="ghost" size="icon">
                <ArrowLeft className="h-5 w-5" />
              </Button>
            </Link>
            <div>
              <h1 className="text-foreground text-2xl font-bold">Editar Vehículo</h1>
              <p className="text-muted-foreground">
                {vehicle.make} {vehicle.model} {vehicle.year}
              </p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <Badge
              className={
                formData.status === 'active'
                  ? 'bg-primary/100'
                  : formData.status === 'paused'
                    ? 'bg-yellow-500'
                    : 'bg-muted/500'
              }
            >
              {formData.status === 'active'
                ? 'Activo'
                : formData.status === 'paused'
                  ? 'Pausado'
                  : formData.status}
            </Badge>
            <Link href={`/vehiculos/${vehicle.slug}`}>
              <Button variant="outline">
                <Eye className="mr-2 h-4 w-4" />
                Ver Publicación
              </Button>
            </Link>
          </div>
        </div>

        {/* Tabs */}
        <Tabs defaultValue="info" className="space-y-6">
          <TabsList className="bg-card border">
            <TabsTrigger value="info" className="data-[state=active]:bg-primary/10">
              <Car className="mr-2 h-4 w-4" />
              Información
            </TabsTrigger>
            <TabsTrigger value="photos" className="data-[state=active]:bg-primary/10">
              <Camera className="mr-2 h-4 w-4" />
              Fotos ({vehicle.images?.length ?? 0})
            </TabsTrigger>
            <TabsTrigger value="price" className="data-[state=active]:bg-primary/10">
              <DollarSign className="mr-2 h-4 w-4" />
              Precio
            </TabsTrigger>
            <TabsTrigger value="settings" className="data-[state=active]:bg-primary/10">
              <Settings className="mr-2 h-4 w-4" />
              Configuración
            </TabsTrigger>
            <TabsTrigger value="view360" className="data-[state=active]:bg-primary/10">
              <RotateCw className="mr-2 h-4 w-4" />
              Vista 360°
            </TabsTrigger>
          </TabsList>

          {/* Info Tab */}
          <TabsContent value="info">
            <Card>
              <CardHeader>
                <CardTitle>Información del Vehículo</CardTitle>
                <CardDescription>Datos principales de tu vehículo</CardDescription>
              </CardHeader>
              <CardContent className="space-y-6">
                <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                  <div>
                    <label className="mb-2 block text-sm font-medium">Marca</label>
                    <Select value={formData.make} onValueChange={v => handleChange('make', v)}>
                      <SelectTrigger>
                        <SelectValue placeholder="Seleccionar marca" />
                      </SelectTrigger>
                      <SelectContent>
                        {makes.map(make => (
                          <SelectItem key={make.id} value={make.name}>
                            {make.name}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  <div>
                    <label className="mb-2 block text-sm font-medium">Modelo</label>
                    <Input
                      value={formData.model}
                      onChange={e => handleChange('model', e.target.value)}
                    />
                  </div>
                  <div>
                    <label className="mb-2 block text-sm font-medium">Año</label>
                    <Input
                      type="number"
                      value={formData.year}
                      onChange={e => handleChange('year', e.target.value)}
                    />
                  </div>
                  <div>
                    <label className="mb-2 block text-sm font-medium">Versión/Trim</label>
                    <Input
                      value={formData.trim}
                      onChange={e => handleChange('trim', e.target.value)}
                      placeholder="Ej: XLE, Limited, Sport"
                    />
                  </div>
                  <div>
                    <label className="mb-2 block text-sm font-medium">Kilometraje</label>
                    <Input
                      type="number"
                      value={formData.mileage}
                      onChange={e => handleChange('mileage', e.target.value)}
                    />
                  </div>
                  <div>
                    <label className="mb-2 block text-sm font-medium">Condición</label>
                    <Select
                      value={formData.condition}
                      onValueChange={v => handleChange('condition', v)}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Seleccionar condición" />
                      </SelectTrigger>
                      <SelectContent>
                        {conditions.map(c => (
                          <SelectItem key={c.value} value={c.value}>
                            {c.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  <div>
                    <label className="mb-2 block text-sm font-medium">Combustible</label>
                    <Select
                      value={formData.fuelType}
                      onValueChange={v => handleChange('fuelType', v)}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Seleccionar combustible" />
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
                  <div>
                    <label className="mb-2 block text-sm font-medium">Transmisión</label>
                    <Select
                      value={formData.transmission}
                      onValueChange={v => handleChange('transmission', v)}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Seleccionar transmisión" />
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
                  <div>
                    <label className="mb-2 block text-sm font-medium">Color Exterior</label>
                    <Select
                      value={formData.exteriorColor}
                      onValueChange={v => handleChange('exteriorColor', v)}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Seleccionar color" />
                      </SelectTrigger>
                      <SelectContent>
                        {colors.map(c => (
                          <SelectItem key={c.value} value={c.value}>
                            {c.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  <div>
                    <label className="mb-2 block text-sm font-medium">Puertas</label>
                    <Select value={formData.doors} onValueChange={v => handleChange('doors', v)}>
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="2">2 puertas</SelectItem>
                        <SelectItem value="4">4 puertas</SelectItem>
                        <SelectItem value="5">5 puertas</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                </div>

                <div>
                  <label className="mb-2 block text-sm font-medium">Descripción</label>
                  <Textarea
                    rows={4}
                    value={formData.description}
                    onChange={e => handleChange('description', e.target.value)}
                    placeholder="Describe las características destacadas de tu vehículo..."
                  />
                </div>

                <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                  <div>
                    <label className="mb-2 block text-sm font-medium">Provincia</label>
                    <Select
                      value={formData.province}
                      onValueChange={v => handleChange('province', v)}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Seleccionar provincia" />
                      </SelectTrigger>
                      <SelectContent>
                        {provinces.map(p => (
                          <SelectItem key={p.value} value={p.label}>
                            {p.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  <div>
                    <label className="mb-2 block text-sm font-medium">Ciudad/Sector</label>
                    <Input
                      value={formData.city}
                      onChange={e => handleChange('city', e.target.value)}
                      placeholder="Ej: Piantini, Naco, Los Prados"
                    />
                  </div>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          {/* Photos Tab */}
          <TabsContent value="photos">
            <Card>
              <CardHeader>
                <CardTitle>Fotos del Vehículo</CardTitle>
                <CardDescription>Administra las fotos de tu publicación</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
                  {vehicle.images?.map((image, i) => (
                    <div
                      key={image.id}
                      className="group bg-muted relative aspect-square overflow-hidden rounded-lg"
                    >
                      <Image
                        src={image.url}
                        alt={`${vehicle.make} ${vehicle.model} - Foto ${i + 1}`}
                        fill
                        className="object-cover"
                      />
                      <div className="absolute inset-0 flex items-center justify-center rounded-lg bg-black/50 opacity-0 transition-opacity group-hover:opacity-100">
                        <Button
                          size="icon"
                          variant="ghost"
                          className="text-white hover:bg-white/20"
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                      {image.isPrimary && (
                        <Badge className="bg-primary/100 absolute top-2 left-2 text-xs">
                          Principal
                        </Badge>
                      )}
                    </div>
                  ))}
                  <button className="border-border bg-muted/50 text-muted-foreground hover:border-primary hover:bg-primary/10 hover:text-primary flex aspect-square flex-col items-center justify-center rounded-lg border-2 border-dashed transition-colors">
                    <Camera className="mb-2 h-8 w-8" />
                    <span className="text-sm">Agregar</span>
                  </button>
                </div>
                <p className="text-muted-foreground mt-4 text-sm">
                  Puedes agregar hasta 20 fotos. La primera foto será la principal.
                </p>
              </CardContent>
            </Card>
          </TabsContent>

          {/* Price Tab */}
          <TabsContent value="price">
            <Card>
              <CardHeader>
                <CardTitle>Precio y Condiciones</CardTitle>
                <CardDescription>Configura el precio de tu vehículo</CardDescription>
              </CardHeader>
              <CardContent className="space-y-6">
                <div>
                  <label className="mb-2 block text-sm font-medium">Precio (RD$)</label>
                  <div className="relative">
                    <span className="text-muted-foreground absolute top-1/2 left-3 -translate-y-1/2">
                      RD$
                    </span>
                    <Input
                      type="number"
                      className="h-14 pl-12 text-2xl"
                      value={formData.price}
                      onChange={e => handleChange('price', e.target.value)}
                    />
                  </div>
                  <p className="text-muted-foreground mt-2 text-sm">
                    Precio sugerido basado en el mercado: RD${' '}
                    {(parseInt(formData.price || '0') * 0.95).toLocaleString()} - RD${' '}
                    {(parseInt(formData.price || '0') * 1.05).toLocaleString()}
                  </p>
                </div>

                <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                  <label className="bg-muted/50 hover:bg-muted flex cursor-pointer items-center gap-3 rounded-lg p-4">
                    <input
                      type="checkbox"
                      className="border-border h-5 w-5 rounded"
                      checked={formData.isNegotiable}
                      onChange={e => handleChange('isNegotiable', e.target.checked)}
                    />
                    <div>
                      <span className="font-medium">Precio Negociable</span>
                      <p className="text-muted-foreground text-sm">
                        El comprador puede hacer ofertas
                      </p>
                    </div>
                  </label>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          {/* Settings Tab */}
          <TabsContent value="settings">
            <div className="space-y-6">
              <Card>
                <CardHeader>
                  <CardTitle>Estado de la Publicación</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="bg-muted/50 flex items-center justify-between rounded-lg p-4">
                    <div className="flex items-center gap-3">
                      {formData.status === 'active' ? (
                        <CheckCircle className="text-primary h-5 w-5" />
                      ) : (
                        <Pause className="h-5 w-5 text-yellow-500" />
                      )}
                      <div>
                        <p className="font-medium">
                          {formData.status === 'active'
                            ? 'Publicación Activa'
                            : 'Publicación Pausada'}
                        </p>
                        <p className="text-muted-foreground text-sm">
                          {formData.status === 'active'
                            ? 'Tu vehículo es visible para compradores'
                            : 'Tu vehículo no aparece en búsquedas'}
                        </p>
                      </div>
                    </div>
                    <Button
                      variant="outline"
                      onClick={() =>
                        handleStatusChange(formData.status === 'active' ? 'paused' : 'active')
                      }
                      disabled={updateMutation.isPending}
                    >
                      {updateMutation.isPending ? (
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      ) : formData.status === 'active' ? (
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
                    </Button>
                  </div>
                </CardContent>
              </Card>

              <Card className="border-red-200">
                <CardHeader>
                  <CardTitle className="text-red-600">Zona de Peligro</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="flex items-center justify-between rounded-lg bg-red-50 p-4">
                    <div>
                      <p className="font-medium text-red-900">Eliminar Publicación</p>
                      <p className="text-sm text-red-700">Esta acción no se puede deshacer</p>
                    </div>
                    <AlertDialog>
                      <AlertDialogTrigger asChild>
                        <Button
                          variant="outline"
                          className="border-red-300 text-red-600 hover:bg-red-50"
                          disabled={isDeleting}
                        >
                          {isDeleting ? (
                            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                          ) : (
                            <Trash2 className="mr-2 h-4 w-4" />
                          )}
                          Eliminar
                        </Button>
                      </AlertDialogTrigger>
                      <AlertDialogContent>
                        <AlertDialogHeader>
                          <AlertDialogTitle>¿Estás seguro?</AlertDialogTitle>
                          <AlertDialogDescription>
                            Esta acción eliminará permanentemente tu publicación de{' '}
                            <strong>
                              {vehicle.make} {vehicle.model} {vehicle.year}
                            </strong>
                            . No podrás recuperarla.
                          </AlertDialogDescription>
                        </AlertDialogHeader>
                        <AlertDialogFooter>
                          <AlertDialogCancel>Cancelar</AlertDialogCancel>
                          <AlertDialogAction
                            onClick={handleDelete}
                            className="bg-red-600 hover:bg-red-700"
                          >
                            Sí, eliminar
                          </AlertDialogAction>
                        </AlertDialogFooter>
                      </AlertDialogContent>
                    </AlertDialog>
                  </div>
                </CardContent>
              </Card>
            </div>
          </TabsContent>

          {/* Vista 360° Tab */}
          <TabsContent value="view360">
            <Vehicle360UploadWizard
              vehicleId={vehicle.id}
              onComplete={() => {
                toast.success('Vista 360° procesada correctamente.');
              }}
            />
          </TabsContent>
        </Tabs>

        {/* Save Button */}
        <div className="mt-8 flex justify-end gap-4">
          <Link href="/mis-vehiculos">
            <Button variant="outline">Cancelar</Button>
          </Link>
          <Button
            className="bg-primary hover:bg-primary/90"
            onClick={handleSave}
            disabled={isSaving}
          >
            {isSaving ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Guardando...
              </>
            ) : (
              <>
                <Save className="mr-2 h-4 w-4" />
                Guardar Cambios
              </>
            )}
          </Button>
        </div>
      </div>
    </div>
  );
}
