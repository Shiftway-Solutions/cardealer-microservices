/**
 * Dealer Edit Vehicle Page
 *
 * Edit vehicle in dealer inventory - Connected to real APIs
 * February 2026
 */

'use client';

import * as React from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import Image from 'next/image';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Badge } from '@/components/ui/badge';
import { Switch } from '@/components/ui/switch';
import { Skeleton } from '@/components/ui/skeleton';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
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
  ArrowLeft,
  Save,
  Eye,
  Trash2,
  Upload,
  GripVertical,
  Plus,
  TrendingUp,
  BarChart3,
  AlertCircle,
  Loader2,
  RefreshCw,
  Camera,
} from 'lucide-react';
import { toast } from 'sonner';
import { useIsMobile } from '@/hooks/use-is-mobile';
import {
  useVehicle,
  useUpdateVehicle,
  useDeleteVehicle,
  useMakes,
  useFuelTypes,
  useTransmissions,
  useColors,
} from '@/hooks/use-vehicles';
import { useUploadImages } from '@/hooks/use-media';
import type { UpdateVehicleRequest } from '@/services/vehicles';

// =============================================================================
// SKELETON COMPONENTS
// =============================================================================

function EditVehicleSkeleton() {
  return (
    <div className="min-h-screen bg-slate-900 p-6">
      <div className="mb-6 flex flex-col justify-between gap-4 md:flex-row md:items-center">
        <div className="flex items-center gap-4">
          <Skeleton className="h-10 w-10 rounded bg-slate-800" />
          <div>
            <Skeleton className="mb-2 h-7 w-40 bg-slate-800" />
            <Skeleton className="h-4 w-24 bg-slate-800" />
          </div>
        </div>
        <div className="flex items-center gap-3">
          <Skeleton className="h-6 w-16 bg-slate-800" />
          <Skeleton className="h-10 w-32 bg-slate-800" />
          <Skeleton className="h-10 w-36 bg-slate-800" />
        </div>
      </div>
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        <div className="space-y-6 lg:col-span-2">
          <Card className="border-slate-700 bg-slate-800">
            <CardHeader>
              <Skeleton className="h-6 w-48 bg-slate-700" />
            </CardHeader>
            <CardContent className="space-y-4">
              <Skeleton className="h-10 w-full bg-slate-700" />
              <div className="grid grid-cols-3 gap-4">
                <Skeleton className="h-10 w-full bg-slate-700" />
                <Skeleton className="h-10 w-full bg-slate-700" />
                <Skeleton className="h-10 w-full bg-slate-700" />
              </div>
              <Skeleton className="h-32 w-full bg-slate-700" />
            </CardContent>
          </Card>
        </div>
        <div className="space-y-6">
          <Card className="border-slate-700 bg-slate-800">
            <CardHeader>
              <Skeleton className="h-6 w-32 bg-slate-700" />
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-2 gap-4">
                {[1, 2, 3, 4].map(i => (
                  <Skeleton key={i} className="h-20 w-full bg-slate-700" />
                ))}
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}

// =============================================================================
// MAIN COMPONENT
// =============================================================================

export default function DealerEditVehiclePage() {
  const params = useParams();
  const router = useRouter();
  const vehicleId = params.id as string;

  // API Hooks
  const { data: vehicle, isLoading, error, refetch } = useVehicle(vehicleId);
  const updateMutation = useUpdateVehicle();
  const deleteMutation = useDeleteVehicle();
  const uploadMutation = useUploadImages();

  // Catalog hooks
  const { data: makes } = useMakes();
  const { data: fuelTypes } = useFuelTypes();
  const { data: transmissions } = useTransmissions();
  const { data: colors } = useColors();

  // Form state
  const [formData, setFormData] = React.useState({
    make: '',
    model: '',
    year: '',
    price: '',
    mileage: '',
    fuelType: '',
    transmission: '',
    exteriorColor: '',
    vin: '',
    description: '',
    condition: '',
  });
  const [isActive, setIsActive] = React.useState(true);
  const [isFeatured, setIsFeatured] = React.useState(false);
  const [isNegotiable, setIsNegotiable] = React.useState(false);
  const [hasChanges, setHasChanges] = React.useState(false);

  // Image upload
  const fileInputRef = React.useRef<HTMLInputElement>(null);
  const cameraInputRef = React.useRef<HTMLInputElement>(null);
  const isMobile = useIsMobile();
  const [uploadingImages, setUploadingImages] = React.useState(false);

  // Sync form with vehicle data
  React.useEffect(() => {
    if (vehicle) {
      setFormData({
        make: vehicle.make || '',
        model: vehicle.model || '',
        year: vehicle.year?.toString() || '',
        price: vehicle.price?.toString() || '',
        mileage: vehicle.mileage?.toString() || '',
        fuelType: vehicle.fuelType || '',
        transmission: vehicle.transmission || '',
        exteriorColor: vehicle.exteriorColor || '',
        vin: '', // VIN not stored in Vehicle type
        description: vehicle.description || '',
        condition: vehicle.condition || '',
      });
      setIsActive(vehicle.status === 'active');
      setIsFeatured(vehicle.isFeatured || false);
      setIsNegotiable(vehicle.isNegotiable || false);
    }
  }, [vehicle]);

  const handleChange = (field: string, value: string) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    setHasChanges(true);
  };

  const handleSave = async () => {
    if (!vehicle) return;

    const updateData: UpdateVehicleRequest = {
      make: formData.make,
      model: formData.model,
      year: parseInt(formData.year),
      price: parseInt(formData.price),
      mileage: parseInt(formData.mileage),
      fuelType: formData.fuelType,
      transmission: formData.transmission,
      exteriorColor: formData.exteriorColor,
      vin: formData.vin || undefined,
      description: formData.description || undefined,
      condition: formData.condition,
      status: isActive ? 'active' : 'paused',
      isNegotiable,
    };

    try {
      await updateMutation.mutateAsync({ id: vehicleId, data: updateData });
      toast.success('Vehículo actualizado correctamente');
      setHasChanges(false);
    } catch {
      toast.error('Error al actualizar el vehículo');
    }
  };

  const handleDelete = async () => {
    try {
      await deleteMutation.mutateAsync(vehicleId);
      toast.success('Vehículo eliminado');
      router.push('/dealer/inventario');
    } catch {
      toast.error('Error al eliminar el vehículo');
    }
  };

  const handleImageUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (!files || files.length === 0) return;

    setUploadingImages(true);
    try {
      const filesArray = Array.from(files);
      await uploadMutation.mutateAsync({
        files: filesArray,
        folder: `vehicles/${vehicleId}`,
      });
      toast.success(`${files.length} imagen(es) subidas`);
      refetch(); // Refresh vehicle data to get new images
    } catch {
      toast.error('Error al subir las imágenes');
    } finally {
      setUploadingImages(false);
    }
  };

  // Loading state
  if (isLoading) {
    return <EditVehicleSkeleton />;
  }

  // Error state
  if (error || !vehicle) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-900 p-6">
        <Card className="w-full max-w-md border-slate-700 bg-slate-800">
          <CardContent className="p-8 text-center">
            <AlertCircle className="mx-auto mb-4 h-12 w-12 text-red-500" />
            <h2 className="mb-2 text-xl font-semibold text-white">Vehículo no encontrado</h2>
            <p className="mb-4 text-slate-400">
              El vehículo que buscas no existe o no tienes permiso para editarlo.
            </p>
            <div className="flex justify-center gap-3">
              <Button variant="outline" onClick={() => refetch()}>
                <RefreshCw className="mr-2 h-4 w-4" />
                Reintentar
              </Button>
              <Link href="/dealer/inventario">
                <Button>Volver al inventario</Button>
              </Link>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  const vehicleTitle = `${vehicle.year} ${vehicle.make} ${vehicle.model}`;

  return (
    <div className="min-h-screen bg-slate-900 p-6">
      {/* Header */}
      <div className="mb-6 flex flex-col justify-between gap-4 md:flex-row md:items-center">
        <div className="flex items-center gap-4">
          <Link href="/dealer/inventario">
            <Button
              variant="ghost"
              size="icon"
              className="text-slate-400 hover:bg-slate-800 hover:text-white"
            >
              <ArrowLeft className="h-5 w-5" />
            </Button>
          </Link>
          <div>
            <h1 className="text-2xl font-bold text-white">Editar Vehículo</h1>
            <p className="text-slate-400">{vehicleTitle}</p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <Badge
            variant={isActive ? 'default' : 'secondary'}
            className={isActive ? 'bg-primary' : ''}
          >
            {isActive ? 'Activo' : 'Pausado'}
          </Badge>
          <Link href={`/vehiculos/${vehicle.slug}`} target="_blank" rel="noopener noreferrer">
            <Button
              variant="outline"
              className="border-slate-700 text-slate-300 hover:bg-slate-800"
            >
              <Eye className="mr-2 h-4 w-4" />
              Ver publicación
            </Button>
          </Link>
          <Button
            className="bg-primary hover:bg-primary/90"
            onClick={handleSave}
            disabled={updateMutation.isPending || !hasChanges}
          >
            {updateMutation.isPending ? (
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <Save className="mr-2 h-4 w-4" />
            )}
            Guardar Cambios
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        {/* Main Content */}
        <div className="space-y-6 lg:col-span-2">
          <Tabs defaultValue="info" className="w-full">
            <TabsList className="w-full justify-start border-b border-slate-700 bg-slate-800">
              <TabsTrigger value="info" className="data-[state=active]:bg-slate-700">
                Información
              </TabsTrigger>
              <TabsTrigger value="photos" className="data-[state=active]:bg-slate-700">
                Fotos ({vehicle.images?.length || 0})
              </TabsTrigger>
              <TabsTrigger value="pricing" className="data-[state=active]:bg-slate-700">
                Precio
              </TabsTrigger>
              <TabsTrigger value="settings" className="data-[state=active]:bg-slate-700">
                Configuración
              </TabsTrigger>
            </TabsList>

            <TabsContent value="info" className="mt-6 space-y-6">
              <Card className="border-slate-700 bg-slate-800">
                <CardHeader>
                  <CardTitle className="text-white">Información del Vehículo</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="grid grid-cols-2 gap-4 md:grid-cols-3">
                    <div>
                      <label className="mb-2 block text-sm font-medium text-slate-300">Marca</label>
                      <Select value={formData.make} onValueChange={v => handleChange('make', v)}>
                        <SelectTrigger className="border-slate-700 bg-slate-900 text-white">
                          <SelectValue placeholder="Seleccionar marca" />
                        </SelectTrigger>
                        <SelectContent>
                          {makes?.map(make => (
                            <SelectItem key={make.id} value={make.name}>
                              {make.name}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>
                    <div>
                      <label className="mb-2 block text-sm font-medium text-slate-300">
                        Modelo
                      </label>
                      <Input
                        className="border-slate-700 bg-slate-900 text-white"
                        value={formData.model}
                        onChange={e => handleChange('model', e.target.value)}
                        placeholder="Ej: Camry SE"
                      />
                    </div>
                    <div>
                      <label className="mb-2 block text-sm font-medium text-slate-300">Año</label>
                      <Select value={formData.year} onValueChange={v => handleChange('year', v)}>
                        <SelectTrigger className="border-slate-700 bg-slate-900 text-white">
                          <SelectValue placeholder="Seleccionar año" />
                        </SelectTrigger>
                        <SelectContent>
                          {Array.from({ length: 30 }, (_, i) => 2026 - i).map(y => (
                            <SelectItem key={y} value={y.toString()}>
                              {y}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-4 md:grid-cols-3">
                    <div>
                      <label className="mb-2 block text-sm font-medium text-slate-300">
                        Kilometraje
                      </label>
                      <Input
                        className="border-slate-700 bg-slate-900 text-white"
                        type="number"
                        value={formData.mileage}
                        onChange={e => handleChange('mileage', e.target.value)}
                        placeholder="Ej: 25000"
                      />
                    </div>
                    <div>
                      <label className="mb-2 block text-sm font-medium text-slate-300">
                        Combustible
                      </label>
                      <Select
                        value={formData.fuelType}
                        onValueChange={v => handleChange('fuelType', v)}
                      >
                        <SelectTrigger className="border-slate-700 bg-slate-900 text-white">
                          <SelectValue placeholder="Seleccionar" />
                        </SelectTrigger>
                        <SelectContent>
                          {fuelTypes?.map(ft => (
                            <SelectItem key={ft.value} value={ft.value}>
                              {ft.label}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>
                    <div>
                      <label className="mb-2 block text-sm font-medium text-slate-300">
                        Transmisión
                      </label>
                      <Select
                        value={formData.transmission}
                        onValueChange={v => handleChange('transmission', v)}
                      >
                        <SelectTrigger className="border-slate-700 bg-slate-900 text-white">
                          <SelectValue placeholder="Seleccionar" />
                        </SelectTrigger>
                        <SelectContent>
                          {transmissions?.map(t => (
                            <SelectItem key={t.value} value={t.value}>
                              {t.label}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <label className="mb-2 block text-sm font-medium text-slate-300">Color</label>
                      <Select
                        value={formData.exteriorColor}
                        onValueChange={v => handleChange('exteriorColor', v)}
                      >
                        <SelectTrigger className="border-slate-700 bg-slate-900 text-white">
                          <SelectValue placeholder="Seleccionar color" />
                        </SelectTrigger>
                        <SelectContent>
                          {colors?.map(c => (
                            <SelectItem key={c.value} value={c.value}>
                              {c.label}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>
                    <div>
                      <label className="mb-2 block text-sm font-medium text-slate-300">VIN</label>
                      <Input
                        className="border-slate-700 bg-slate-900 text-white"
                        value={formData.vin}
                        onChange={e => handleChange('vin', e.target.value)}
                        placeholder="Número de identificación"
                      />
                    </div>
                  </div>

                  <div>
                    <label className="mb-2 block text-sm font-medium text-slate-300">
                      Condición
                    </label>
                    <Select
                      value={formData.condition}
                      onValueChange={v => handleChange('condition', v)}
                    >
                      <SelectTrigger className="border-slate-700 bg-slate-900 text-white">
                        <SelectValue placeholder="Seleccionar condición" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="new">Nuevo</SelectItem>
                        <SelectItem value="used">Usado - Excelente</SelectItem>
                        <SelectItem value="used-good">Usado - Bueno</SelectItem>
                        <SelectItem value="used-fair">Usado - Regular</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>

                  <div>
                    <label className="mb-2 block text-sm font-medium text-slate-300">
                      Descripción
                    </label>
                    <Textarea
                      className="min-h-[120px] border-slate-700 bg-slate-900 text-white"
                      value={formData.description}
                      onChange={e => handleChange('description', e.target.value)}
                      placeholder="Describe el vehículo, características especiales, historial de mantenimiento..."
                    />
                  </div>
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent value="photos" className="mt-6 space-y-6">
              <Card className="border-slate-700 bg-slate-800">
                <CardHeader>
                  <CardTitle className="text-white">Fotos del Vehículo</CardTitle>
                  <CardDescription className="text-slate-400">
                    La primera foto será la principal. Máximo 20 fotos.
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="mb-6 grid grid-cols-2 gap-4 md:grid-cols-4">
                    {vehicle.images?.map((image, index) => (
                      <div
                        key={image.id}
                        className={`relative aspect-[4/3] overflow-hidden rounded-lg border-2 bg-slate-900 ${
                          index === 0 ? 'border-primary' : 'border-slate-700'
                        }`}
                      >
                        <Image
                          src={image.url}
                          alt={image.alt || `Foto ${index + 1}`}
                          fill
                          className="object-cover"
                        />
                        <div className="absolute top-2 left-2">
                          <GripVertical className="h-4 w-4 cursor-move text-white drop-shadow" />
                        </div>
                        {index === 0 && (
                          <Badge className="bg-primary absolute bottom-2 left-2 text-xs">
                            Principal
                          </Badge>
                        )}
                      </div>
                    ))}

                    {/* Upload placeholder */}
                    {(vehicle.images?.length || 0) < 20 && (
                      <button
                        onClick={() => fileInputRef.current?.click()}
                        disabled={uploadingImages}
                        className="flex aspect-[4/3] flex-col items-center justify-center gap-2 rounded-lg border-2 border-dashed border-slate-600 bg-slate-900 transition-colors hover:border-slate-500 disabled:opacity-50"
                      >
                        {uploadingImages ? (
                          <Loader2 className="h-6 w-6 animate-spin text-slate-500" />
                        ) : (
                          <>
                            <Plus className="h-6 w-6 text-slate-500" />
                            <span className="text-xs text-slate-500">Agregar</span>
                          </>
                        )}
                      </button>
                    )}
                  </div>

                  <input
                    ref={fileInputRef}
                    type="file"
                    accept="image/*"
                    multiple
                    className="hidden"
                    onChange={handleImageUpload}
                  />
                  {/* Camera input for PWA mobile */}
                  {isMobile && (
                    <input
                      ref={cameraInputRef}
                      type="file"
                      accept="image/*"
                      capture="environment"
                      className="hidden"
                      onChange={handleImageUpload}
                    />
                  )}

                  <div className="rounded-lg border-2 border-dashed border-slate-600 p-8 text-center">
                    <Upload className="mx-auto mb-3 h-10 w-10 text-slate-500" />
                    <p className="mb-2 text-slate-400">
                      Arrastra fotos aquí o haz clic para seleccionar
                    </p>
                    <p className="text-xs text-slate-500">PNG, JPG hasta 10MB • Máximo 20 fotos</p>
                    <Button
                      variant="outline"
                      className="mt-4 border-slate-600 text-slate-300 hover:bg-slate-700"
                      onClick={() => fileInputRef.current?.click()}
                      disabled={uploadingImages}
                    >
                      {uploadingImages ? (
                        <>
                          <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                          Subiendo...
                        </>
                      ) : (
                        'Seleccionar Archivos'
                      )}
                    </Button>
                  </div>

                  {/* Camera button for PWA mobile */}
                  {isMobile && (
                    <button
                      type="button"
                      onClick={() => cameraInputRef.current?.click()}
                      className="flex w-full items-center justify-center gap-2 rounded-lg border-2 border-emerald-200 bg-emerald-50 p-4 text-emerald-700 transition-colors hover:bg-emerald-100 active:bg-emerald-200"
                    >
                      <Camera className="h-5 w-5" />
                      <span className="font-medium">Tomar Foto con Cámara</span>
                    </button>
                  )}
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent value="pricing" className="mt-6 space-y-6">
              <Card className="border-slate-700 bg-slate-800">
                <CardHeader>
                  <CardTitle className="text-white">Precio y Opciones</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div>
                    <label className="mb-2 block text-sm font-medium text-slate-300">
                      Precio de Venta (RD$)
                    </label>
                    <Input
                      className="border-slate-700 bg-slate-900 text-2xl font-bold text-white"
                      type="number"
                      value={formData.price}
                      onChange={e => handleChange('price', e.target.value)}
                      placeholder="1500000"
                    />
                    {formData.price && (
                      <p className="mt-1 text-sm text-slate-400">
                        ≈ ${Math.round(parseInt(formData.price) / 58).toLocaleString()} USD
                      </p>
                    )}
                  </div>

                  <div className="flex items-center justify-between rounded-lg bg-slate-900 p-4">
                    <div>
                      <p className="font-medium text-white">Precio negociable</p>
                      <p className="text-sm text-slate-400">
                        Permite que compradores hagan ofertas
                      </p>
                    </div>
                    <Switch
                      checked={isNegotiable}
                      onCheckedChange={v => {
                        setIsNegotiable(v);
                        setHasChanges(true);
                      }}
                    />
                  </div>
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent value="settings" className="mt-6 space-y-6">
              <Card className="border-slate-700 bg-slate-800">
                <CardHeader>
                  <CardTitle className="text-white">Estado de la Publicación</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="flex items-center justify-between rounded-lg bg-slate-900 p-4">
                    <div>
                      <p className="font-medium text-white">Publicación activa</p>
                      <p className="text-sm text-slate-400">El vehículo es visible en búsquedas</p>
                    </div>
                    <Switch
                      checked={isActive}
                      onCheckedChange={v => {
                        setIsActive(v);
                        setHasChanges(true);
                      }}
                    />
                  </div>

                  <div className="flex items-center justify-between rounded-lg bg-slate-900 p-4">
                    <div>
                      <p className="font-medium text-white">Destacado</p>
                      <p className="text-sm text-slate-400">Aparece primero en resultados</p>
                    </div>
                    <Switch
                      checked={isFeatured}
                      onCheckedChange={v => {
                        setIsFeatured(v);
                        setHasChanges(true);
                      }}
                    />
                  </div>
                </CardContent>
              </Card>

              <Card className="border-red-800 bg-red-900/20">
                <CardHeader>
                  <CardTitle className="text-red-400">Zona de Peligro</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="font-medium text-white">Eliminar vehículo</p>
                      <p className="text-sm text-slate-400">Esta acción no se puede deshacer</p>
                    </div>
                    <AlertDialog>
                      <AlertDialogTrigger asChild>
                        <Button variant="destructive" disabled={deleteMutation.isPending}>
                          {deleteMutation.isPending ? (
                            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                          ) : (
                            <Trash2 className="mr-2 h-4 w-4" />
                          )}
                          Eliminar
                        </Button>
                      </AlertDialogTrigger>
                      <AlertDialogContent>
                        <AlertDialogHeader>
                          <AlertDialogTitle>¿Eliminar vehículo?</AlertDialogTitle>
                          <AlertDialogDescription>
                            Estás a punto de eliminar <strong>{vehicleTitle}</strong>. Esta acción
                            no se puede deshacer y perderás todas las estadísticas asociadas.
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
            </TabsContent>
          </Tabs>
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Stats */}
          <Card className="border-slate-700 bg-slate-800">
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-white">
                <BarChart3 className="h-5 w-5" />
                Estadísticas
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-2 gap-4">
                <div className="rounded-lg bg-slate-900 p-3 text-center">
                  <p className="text-2xl font-bold text-white">{vehicle.viewCount || 0}</p>
                  <p className="text-xs text-slate-400">Vistas</p>
                </div>
                <div className="rounded-lg bg-slate-900 p-3 text-center">
                  <p className="text-2xl font-bold text-white">{vehicle.favoriteCount || 0}</p>
                  <p className="text-xs text-slate-400">Favoritos</p>
                </div>
              </div>
              <p className="mt-4 text-center text-xs text-slate-500">
                Publicado {new Date(vehicle.createdAt).toLocaleDateString('es-DO')}
              </p>
            </CardContent>
          </Card>

          {/* Boost */}
          <Card className="border-yellow-700 bg-gradient-to-br from-yellow-900/40 to-orange-900/40">
            <CardContent className="p-6">
              <TrendingUp className="mb-3 h-8 w-8 text-yellow-500" />
              <h3 className="mb-2 text-lg font-semibold text-white">Promocionar Vehículo</h3>
              <p className="mb-4 text-sm text-yellow-200">
                Aumenta la visibilidad de esta publicación y consigue más leads.
              </p>
              <Button className="w-full bg-yellow-600 hover:bg-yellow-700">
                Ver opciones de boost
              </Button>
            </CardContent>
          </Card>

          {/* Tips */}
          <Card className="border-slate-700 bg-slate-800">
            <CardContent className="p-6">
              <div className="flex items-start gap-3">
                <AlertCircle className="h-5 w-5 shrink-0 text-blue-400" />
                <div>
                  <p className="mb-1 text-sm font-medium text-white">Mejora tu publicación</p>
                  <ul className="space-y-1 text-xs text-slate-400">
                    <li>• Agrega más fotos (10+ recomendado)</li>
                    <li>• Incluye video del vehículo</li>
                    <li>• Completa todos los campos</li>
                    <li>• Usa descripción detallada</li>
                  </ul>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
