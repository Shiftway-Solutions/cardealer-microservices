/**
 * New Vehicle Page
 *
 * Form to add a new vehicle to dealer inventory
 * Connected to real APIs - February 2026
 */

'use client';

import * as React from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import Image from 'next/image';
import { Card, CardContent, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Switch } from '@/components/ui/switch';
import {
  ArrowLeft,
  ArrowRight,
  Upload,
  X,
  Car,
  Info,
  DollarSign,
  Camera,
  CheckCircle,
  Loader2,
  AlertCircle,
  Shield,
  Clock,
} from 'lucide-react';
import { useCanSell } from '@/hooks/use-kyc';
import { toast } from 'sonner';
import { useCurrentDealer } from '@/hooks/use-dealers';
import {
  useCreateVehicle,
  useMakes,
  useModelsByMake,
  useBodyTypes,
  useFuelTypes,
  useTransmissions,
  useColors,
  useProvinces,
} from '@/hooks/use-vehicles';
import { useUploadImages } from '@/hooks/use-media';
import type { CreateVehicleRequest, CreateVehicleImage } from '@/services/vehicles';
import {
  sanitizeText,
  sanitizeVIN,
  sanitizePrice,
  sanitizeMileage,
  sanitizeYear,
} from '@/lib/security/sanitize';

// =============================================================================
// TYPES
// =============================================================================

interface FormData {
  make: string;
  model: string;
  year: string;
  bodyType: string;
  mileage: string;
  condition: string;
  transmission: string;
  fuelType: string;
  exteriorColor: string;
  interiorColor: string;
  vin: string;
  description: string;
  price: string;
  currency: 'DOP' | 'USD';
  isNegotiable: boolean;
  city: string;
  province: string;
}

const steps = [
  { id: 1, title: 'Información Básica', icon: Car },
  { id: 2, title: 'Detalles', icon: Info },
  { id: 3, title: 'Precio', icon: DollarSign },
  { id: 4, title: 'Fotos', icon: Camera },
];

// =============================================================================
// SKELETON
// =============================================================================

function NewVehicleSkeleton() {
  return (
    <div className="mx-auto max-w-4xl space-y-6">
      <div className="flex items-center gap-4">
        <Skeleton className="h-10 w-10" />
        <div>
          <Skeleton className="mb-2 h-7 w-48" />
          <Skeleton className="h-5 w-64" />
        </div>
      </div>
      <div className="flex items-center justify-between">
        {[1, 2, 3, 4].map(i => (
          <React.Fragment key={i}>
            <div className="flex flex-col items-center">
              <Skeleton className="h-10 w-10 rounded-full" />
              <Skeleton className="mt-1 h-3 w-16" />
            </div>
            {i < 4 && <Skeleton className="mx-2 h-0.5 w-16 sm:w-24" />}
          </React.Fragment>
        ))}
      </div>
      <Card>
        <CardContent className="space-y-4 p-6">
          <Skeleton className="h-6 w-40" />
          <div className="grid gap-4 sm:grid-cols-2">
            {[1, 2, 3, 4, 5, 6].map(i => (
              <div key={i} className="space-y-2">
                <Skeleton className="h-4 w-20" />
                <Skeleton className="h-10 w-full" />
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

// =============================================================================
// MAIN COMPONENT
// =============================================================================

export default function NewVehiclePage() {
  const router = useRouter();

  // KYC verification check
  const {
    canSell,
    isPending,
    isRejected,
    needsVerification,
    isLoading: kycLoading,
    rejectionReason,
  } = useCanSell();

  // API hooks
  const { data: dealer, isLoading: dealerLoading } = useCurrentDealer();
  const createVehicleMutation = useCreateVehicle();
  const uploadMutation = useUploadImages();

  // Catalog hooks
  const { data: makes, isLoading: makesLoading } = useMakes();
  const { data: bodyTypes } = useBodyTypes();
  const { data: fuelTypes } = useFuelTypes();
  const { data: transmissions } = useTransmissions();
  const { data: colors } = useColors();
  const { data: provinces } = useProvinces();

  // Form state
  const [currentStep, setCurrentStep] = React.useState(1);
  const [formData, setFormData] = React.useState<FormData>({
    make: '',
    model: '',
    year: '',
    bodyType: '',
    mileage: '',
    condition: '',
    transmission: '',
    fuelType: '',
    exteriorColor: '',
    interiorColor: '',
    vin: '',
    description: '',
    price: '',
    currency: 'DOP',
    isNegotiable: true,
    city: '',
    province: '',
  });

  // Image state
  const [images, setImages] = React.useState<File[]>([]);
  const [imagePreviews, setImagePreviews] = React.useState<string[]>([]);
  const [uploading, setUploading] = React.useState(false);
  const fileInputRef = React.useRef<HTMLInputElement>(null);

  // Get models for selected make
  const { data: models } = useModelsByMake(formData.make);

  // Handlers
  const handleChange = (field: keyof FormData, value: string | boolean) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    // Reset model when make changes
    if (field === 'make') {
      setFormData(prev => ({ ...prev, model: '' }));
    }
  };

  const handleImageUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (files) {
      const newFiles = Array.from(files);
      const remaining = 20 - images.length;
      const filesToAdd = newFiles.slice(0, remaining);

      setImages(prev => [...prev, ...filesToAdd]);

      // Create previews
      filesToAdd.forEach(file => {
        const reader = new FileReader();
        reader.onloadend = () => {
          setImagePreviews(prev => [...prev, reader.result as string]);
        };
        reader.readAsDataURL(file);
      });
    }
  };

  const removeImage = (index: number) => {
    setImages(prev => prev.filter((_, i) => i !== index));
    setImagePreviews(prev => prev.filter((_, i) => i !== index));
  };

  const handleSubmit = async () => {
    // Validate required fields
    if (!formData.make || !formData.model || !formData.year || !formData.price) {
      toast.error('Por favor completa todos los campos requeridos');
      return;
    }

    if (images.length === 0) {
      toast.error('Por favor agrega al menos una foto');
      return;
    }

    setUploading(true);

    try {
      // Step 1: Upload images first
      const uploadResults = await uploadMutation.mutateAsync({
        files: images,
        folder: 'vehicles/temp',
      });

      // Step 2: Map upload results to CreateVehicleImage format
      const uploadedImages: CreateVehicleImage[] = uploadResults.map((result, i) => ({
        url: result.url,
        order: i,
        isPrimary: i === 0,
      }));

      // Step 3: Create vehicle with uploaded image URLs
      // Sanitize all user inputs before sending to API
      const vehicleData: CreateVehicleRequest = {
        make: sanitizeText(formData.make, { maxLength: 50 }),
        model: sanitizeText(formData.model, { maxLength: 50 }),
        year: sanitizeYear(parseInt(formData.year)),
        bodyType: sanitizeText(formData.bodyType || 'sedan', { maxLength: 30 }),
        mileage: sanitizeMileage(parseInt(formData.mileage) || 0),
        mileageUnit: (formData as Record<string, string>).mileageUnit === 'mi' ? 'mi' : 'km', // FIX B1: Send mileage unit
        condition: sanitizeText(formData.condition || 'used', { maxLength: 20 }),
        transmission: sanitizeText(formData.transmission || 'automatic', { maxLength: 30 }),
        fuelType: sanitizeText(formData.fuelType || 'gasoline', { maxLength: 30 }),
        exteriorColor: formData.exteriorColor
          ? sanitizeText(formData.exteriorColor, { maxLength: 30 })
          : undefined,
        interiorColor: formData.interiorColor
          ? sanitizeText(formData.interiorColor, { maxLength: 30 })
          : undefined,
        vin: formData.vin ? sanitizeVIN(formData.vin) : undefined,
        description: formData.description
          ? sanitizeText(formData.description, { maxLength: 5000 })
          : undefined,
        price: sanitizePrice(parseInt(formData.price)),
        currency: (formData.currency === 'USD' ? 'USD' : 'DOP') as 'DOP' | 'USD',
        isNegotiable: formData.isNegotiable,
        city: sanitizeText(formData.city || 'Santo Domingo', { maxLength: 100 }),
        province: sanitizeText(formData.province || 'Distrito Nacional', { maxLength: 100 }),
        features: [],
        images: uploadedImages,
      };

      const result = await createVehicleMutation.mutateAsync(vehicleData);
      toast.success('¡Vehículo publicado exitosamente!');
      router.push(`/dealer/inventario/${result.id}`);
    } catch (error) {
      console.error('Error creating vehicle:', error);
      toast.error('Error al publicar el vehículo');
    } finally {
      setUploading(false);
    }
  };

  const nextStep = () => setCurrentStep(prev => Math.min(prev + 1, 4));
  const prevStep = () => setCurrentStep(prev => Math.max(prev - 1, 1));

  // Validation for next button
  const canProceed = () => {
    switch (currentStep) {
      case 1:
        return formData.make && formData.model && formData.year && formData.condition;
      case 2:
        return formData.transmission && formData.fuelType;
      case 3:
        return formData.price;
      case 4:
        return images.length > 0;
      default:
        return true;
    }
  };

  // Loading state (dealer data or KYC check)
  if (dealerLoading || makesLoading || kycLoading) {
    return <NewVehicleSkeleton />;
  }

  // KYC: Verification pending / under review
  if (isPending) {
    return (
      <div className="flex min-h-[500px] items-center justify-center">
        <Card className="mx-auto max-w-md">
          <CardContent className="p-8 text-center">
            <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-purple-100">
              <Clock className="h-8 w-8 text-purple-600" />
            </div>
            <h2 className="mb-2 text-xl font-bold">Verificación del dealer en proceso</h2>
            <p className="text-muted-foreground mb-6">
              La documentación del dealer está siendo revisada por el equipo de OKLA. Recibirás una
              notificación cuando sea aprobada (24–48 horas hábiles).
            </p>
            <Link href="/dealer">
              <Button variant="outline">Volver al Dashboard</Button>
            </Link>
          </CardContent>
        </Card>
      </div>
    );
  }

  // KYC: Verification rejected
  if (isRejected) {
    return (
      <div className="flex min-h-[500px] items-center justify-center">
        <Card className="mx-auto max-w-md border-red-200">
          <CardContent className="p-8 text-center">
            <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-red-100">
              <AlertCircle className="h-8 w-8 text-red-600" />
            </div>
            <h2 className="mb-2 text-xl font-bold text-red-700">Verificación rechazada</h2>
            <p className="text-muted-foreground mb-3">
              La verificación del dealer fue rechazada. Debes volver a verificar la documentación.
            </p>
            {rejectionReason && (
              <p className="mb-4 rounded-lg bg-red-50 px-4 py-2 text-sm text-red-700">
                <strong>Motivo:</strong> {rejectionReason}
              </p>
            )}
            <div className="flex justify-center gap-3">
              <Link href="/cuenta/verificacion">
                <Button className="gap-2">
                  <Shield className="h-4 w-4" />
                  Verificar de nuevo
                </Button>
              </Link>
              <Link href="/dealer">
                <Button variant="outline">Volver</Button>
              </Link>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  // KYC: Not verified yet
  if (needsVerification || !canSell) {
    return (
      <div className="flex min-h-[500px] items-center justify-center">
        <Card className="mx-auto max-w-md border-amber-200">
          <CardContent className="p-8 text-center">
            <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-amber-100">
              <Shield className="h-8 w-8 text-amber-600" />
            </div>
            <h2 className="mb-2 text-xl font-bold text-amber-800">
              Verifica tu dealer antes de publicar
            </h2>
            <p className="text-muted-foreground mb-4">
              Para agregar vehículos al inventario, el dealer debe estar verificado con:
            </p>
            <ul className="mb-6 space-y-1 text-left text-sm text-gray-600">
              <li className="flex items-center gap-2">
                <span className="text-amber-500">•</span> Cédula del representante legal
              </li>
              <li className="flex items-center gap-2">
                <span className="text-amber-500">•</span> RNC del negocio
              </li>
              <li className="flex items-center gap-2">
                <span className="text-amber-500">•</span> Registro Mercantil o licencia comercial
              </li>
              <li className="flex items-center gap-2">
                <span className="text-amber-500">•</span> Selfie de verificación biométrica
              </li>
            </ul>
            <div className="flex justify-center gap-3">
              <Link href="/cuenta/verificacion">
                <Button className="gap-2 bg-amber-600 hover:bg-amber-700">
                  <Shield className="h-4 w-4" />
                  Iniciar verificación del dealer
                </Button>
              </Link>
              <Link href="/dealer">
                <Button variant="outline">Volver</Button>
              </Link>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  // No dealer access
  if (!dealer) {
    return (
      <div className="mx-auto flex max-w-md items-center justify-center p-8">
        <Card>
          <CardContent className="p-8 text-center">
            <AlertCircle className="mx-auto mb-4 h-12 w-12 text-amber-500" />
            <h2 className="mb-2 text-xl font-semibold">Acceso Restringido</h2>
            <p className="text-muted-foreground mb-4">
              Solo los dealers pueden agregar vehículos al inventario.
            </p>
            <Link href="/dealer">
              <Button>Ir al Dashboard</Button>
            </Link>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" asChild>
          <Link href="/dealer/inventario">
            <ArrowLeft className="h-5 w-5" />
          </Link>
        </Button>
        <div>
          <h1 className="text-foreground text-2xl font-bold">Agregar Vehículo</h1>
          <p className="text-muted-foreground">Completa la información del vehículo</p>
        </div>
      </div>

      {/* Progress Steps */}
      <div className="flex items-center justify-between">
        {steps.map((step, index) => {
          const Icon = step.icon;
          const isCompleted = currentStep > step.id;
          const isCurrent = currentStep === step.id;
          return (
            <div key={step.id} className="flex items-center">
              <div className="flex flex-col items-center">
                <div
                  className={`flex h-10 w-10 items-center justify-center rounded-full ${
                    isCompleted
                      ? 'bg-primary text-white'
                      : isCurrent
                        ? 'border-primary bg-primary/10 text-primary border-2'
                        : 'bg-muted text-muted-foreground'
                  }`}
                >
                  {isCompleted ? <CheckCircle className="h-5 w-5" /> : <Icon className="h-5 w-5" />}
                </div>
                <span
                  className={`mt-1 text-xs ${
                    isCurrent ? 'text-primary font-medium' : 'text-muted-foreground'
                  }`}
                >
                  {step.title}
                </span>
              </div>
              {index < steps.length - 1 && (
                <div
                  className={`mx-2 h-0.5 w-16 sm:w-24 ${isCompleted ? 'bg-primary' : 'bg-muted'}`}
                />
              )}
            </div>
          );
        })}
      </div>

      {/* Form Steps */}
      <Card>
        <CardContent className="p-6">
          {currentStep === 1 && (
            <div className="space-y-6">
              <CardTitle>Información Básica</CardTitle>
              <div className="grid gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="make">Marca *</Label>
                  <Select value={formData.make} onValueChange={v => handleChange('make', v)}>
                    <SelectTrigger>
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
                <div className="space-y-2">
                  <Label htmlFor="model">Modelo *</Label>
                  {models && models.length > 0 ? (
                    <Select value={formData.model} onValueChange={v => handleChange('model', v)}>
                      <SelectTrigger>
                        <SelectValue placeholder="Seleccionar modelo" />
                      </SelectTrigger>
                      <SelectContent>
                        {models.map(model => (
                          <SelectItem key={model.id} value={model.name}>
                            {model.name}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  ) : (
                    <Input
                      id="model"
                      placeholder="Ej: Camry SE"
                      value={formData.model}
                      onChange={e => handleChange('model', e.target.value)}
                    />
                  )}
                </div>
                <div className="space-y-2">
                  <Label htmlFor="year">Año *</Label>
                  <Select value={formData.year} onValueChange={v => handleChange('year', v)}>
                    <SelectTrigger>
                      <SelectValue placeholder="Seleccionar año" />
                    </SelectTrigger>
                    <SelectContent>
                      {Array.from({ length: 30 }, (_, i) => 2026 - i).map(year => (
                        <SelectItem key={year} value={year.toString()}>
                          {year}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-2">
                  <Label htmlFor="bodyType">Tipo de Carrocería</Label>
                  <Select
                    value={formData.bodyType}
                    onValueChange={v => handleChange('bodyType', v)}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Seleccionar tipo" />
                    </SelectTrigger>
                    <SelectContent>
                      {bodyTypes?.map(type => (
                        <SelectItem key={type.value} value={type.value}>
                          {type.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-2">
                  <Label htmlFor="mileage">Kilometraje</Label>
                  <Input
                    id="mileage"
                    type="number"
                    placeholder="Ej: 25000"
                    value={formData.mileage}
                    onChange={e => handleChange('mileage', e.target.value)}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="condition">Condición *</Label>
                  <Select
                    value={formData.condition}
                    onValueChange={v => handleChange('condition', v)}
                  >
                    <SelectTrigger>
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
              </div>
            </div>
          )}

          {currentStep === 2 && (
            <div className="space-y-6">
              <CardTitle>Detalles del Vehículo</CardTitle>
              <div className="grid gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="transmission">Transmisión *</Label>
                  <Select
                    value={formData.transmission}
                    onValueChange={v => handleChange('transmission', v)}
                  >
                    <SelectTrigger>
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
                <div className="space-y-2">
                  <Label htmlFor="fuel">Combustible *</Label>
                  <Select
                    value={formData.fuelType}
                    onValueChange={v => handleChange('fuelType', v)}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Seleccionar" />
                    </SelectTrigger>
                    <SelectContent>
                      {fuelTypes?.map(f => (
                        <SelectItem key={f.value} value={f.value}>
                          {f.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-2">
                  <Label htmlFor="exteriorColor">Color Exterior</Label>
                  <Select
                    value={formData.exteriorColor}
                    onValueChange={v => handleChange('exteriorColor', v)}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Seleccionar" />
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
                <div className="space-y-2">
                  <Label htmlFor="interiorColor">Color Interior</Label>
                  <Input
                    id="interiorColor"
                    placeholder="Ej: Negro"
                    value={formData.interiorColor}
                    onChange={e => handleChange('interiorColor', e.target.value)}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="vin">VIN (opcional)</Label>
                  <Input
                    id="vin"
                    placeholder="Número de identificación"
                    value={formData.vin}
                    onChange={e => handleChange('vin', e.target.value)}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="province">Provincia</Label>
                  <Select
                    value={formData.province}
                    onValueChange={v => handleChange('province', v)}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Seleccionar" />
                    </SelectTrigger>
                    <SelectContent>
                      {provinces?.map(p => (
                        <SelectItem key={p.value} value={p.value}>
                          {p.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>
              <div className="space-y-2">
                <Label htmlFor="description">Descripción</Label>
                <Textarea
                  id="description"
                  placeholder="Describe el vehículo, características especiales, historial..."
                  rows={5}
                  value={formData.description}
                  onChange={e => handleChange('description', e.target.value)}
                />
              </div>
            </div>
          )}

          {currentStep === 3 && (
            <div className="space-y-6">
              <CardTitle>Precio</CardTitle>
              <div className="grid gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="price">Precio *</Label>
                  <Input
                    id="price"
                    type="number"
                    placeholder="Ej: 1500000"
                    value={formData.price}
                    onChange={e => handleChange('price', e.target.value)}
                  />
                  {formData.price && formData.currency === 'DOP' && (
                    <p className="text-muted-foreground text-sm">
                      ≈ ${Math.round(parseInt(formData.price) / 58).toLocaleString()} USD
                    </p>
                  )}
                </div>
                <div className="space-y-2">
                  <Label htmlFor="currency">Moneda</Label>
                  <Select
                    value={formData.currency}
                    onValueChange={v => handleChange('currency', v)}
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="DOP">RD$ (Pesos)</SelectItem>
                      <SelectItem value="USD">US$ (Dólares)</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </div>

              <div className="flex items-center justify-between rounded-lg border p-4">
                <div>
                  <p className="font-medium">Precio negociable</p>
                  <p className="text-muted-foreground text-sm">
                    Permite que compradores hagan ofertas
                  </p>
                </div>
                <Switch
                  checked={formData.isNegotiable}
                  onCheckedChange={v => handleChange('isNegotiable', v)}
                />
              </div>

              <div className="flex items-start gap-2 rounded-lg bg-blue-50 p-4">
                <Info className="mt-0.5 h-5 w-5 text-blue-600" />
                <div className="text-sm text-blue-800">
                  <p className="font-medium">Consejo</p>
                  <p className="text-blue-600">
                    Investiga precios de vehículos similares para establecer un precio competitivo.
                  </p>
                </div>
              </div>
            </div>
          )}

          {currentStep === 4 && (
            <div className="space-y-6">
              <CardTitle>Fotos del Vehículo</CardTitle>
              <p className="text-muted-foreground text-sm">
                Agrega fotos de alta calidad. La primera foto será la imagen principal.
              </p>

              {/* Image preview grid */}
              {imagePreviews.length > 0 && (
                <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
                  {imagePreviews.map((preview, index) => (
                    <div
                      key={index}
                      className={`relative aspect-[4/3] overflow-hidden rounded-lg border-2 ${
                        index === 0 ? 'border-primary' : 'border-border'
                      }`}
                    >
                      <Image
                        src={preview}
                        alt={`Preview ${index + 1}`}
                        fill
                        className="object-cover"
                      />
                      <button
                        type="button"
                        onClick={() => removeImage(index)}
                        className="absolute top-2 right-2 rounded-full bg-red-600 p-1 text-white hover:bg-red-700"
                      >
                        <X className="h-4 w-4" />
                      </button>
                      {index === 0 && (
                        <span className="bg-primary absolute bottom-2 left-2 rounded px-2 py-0.5 text-xs text-white">
                          Principal
                        </span>
                      )}
                    </div>
                  ))}
                </div>
              )}

              {/* Upload area */}
              <div
                className="border-border hover:border-primary cursor-pointer rounded-lg border-2 border-dashed p-8 text-center transition-colors"
                onClick={() => fileInputRef.current?.click()}
              >
                <Upload className="text-muted-foreground mx-auto mb-3 h-10 w-10" />
                <p className="text-muted-foreground mb-1">Haz clic o arrastra fotos aquí</p>
                <p className="text-muted-foreground text-xs">
                  PNG, JPG hasta 10MB • Máximo 20 fotos • {20 - images.length} restantes
                </p>
              </div>

              <input
                ref={fileInputRef}
                type="file"
                accept="image/*"
                multiple
                className="hidden"
                onChange={handleImageUpload}
              />

              <div className="flex items-start gap-2 rounded-lg bg-amber-50 p-4">
                <AlertCircle className="mt-0.5 h-5 w-5 text-amber-600" />
                <div className="text-sm text-amber-800">
                  <p className="font-medium">Consejos para mejores fotos</p>
                  <ul className="mt-1 list-inside list-disc text-amber-700">
                    <li>Usa buena iluminación natural</li>
                    <li>Incluye fotos del exterior, interior y motor</li>
                    <li>Muestra detalles importantes</li>
                    <li>Mínimo 5 fotos recomendadas</li>
                  </ul>
                </div>
              </div>
            </div>
          )}

          {/* Navigation buttons */}
          <div className="mt-8 flex justify-between">
            <Button variant="outline" onClick={prevStep} disabled={currentStep === 1}>
              <ArrowLeft className="mr-2 h-4 w-4" />
              Anterior
            </Button>

            {currentStep < 4 ? (
              <Button onClick={nextStep} disabled={!canProceed()}>
                Siguiente
                <ArrowRight className="ml-2 h-4 w-4" />
              </Button>
            ) : (
              <Button
                onClick={handleSubmit}
                disabled={uploading || !canProceed()}
                className="bg-primary hover:bg-primary/90"
              >
                {uploading ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    Publicando...
                  </>
                ) : (
                  <>
                    <CheckCircle className="mr-2 h-4 w-4" />
                    Publicar Vehículo
                  </>
                )}
              </Button>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Summary sidebar for larger screens */}
      {currentStep === 4 && formData.make && formData.model && (
        <Card className="border-primary bg-primary/10">
          <CardContent className="p-6">
            <h3 className="text-primary mb-4 font-semibold">Resumen</h3>
            <div className="space-y-2 text-sm">
              <p>
                <span className="text-primary">Vehículo:</span> {formData.year} {formData.make}{' '}
                {formData.model}
              </p>
              <p>
                <span className="text-primary">Precio:</span>{' '}
                {formData.currency === 'DOP' ? 'RD$' : 'US$'}{' '}
                {parseInt(formData.price || '0').toLocaleString()}
              </p>
              <p>
                <span className="text-primary">Fotos:</span> {images.length}
              </p>
              {formData.mileage && (
                <p>
                  <span className="text-primary">Kilometraje:</span>{' '}
                  {parseInt(formData.mileage).toLocaleString()} km
                </p>
              )}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
