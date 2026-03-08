'use client';

import { useState, useCallback, useEffect } from 'react';
import { MethodSelector, type PublishMethod } from './method-selector';
import { VinInput } from './vin-input';
import { VinScanner } from './vin-scanner';
import { VinDecodeResults } from './vin-decode-results';
import { VehicleInfoForm } from './vehicle-info-form';
import { PhotoUploadManager } from '@/components/vehicles/photos/photo-upload-manager';
import type { PhotoItem } from '@/components/vehicles/photos/photo-card';
import { PricingStep } from './pricing-step';
import { ReviewStep } from './review-step';
import { View360Step } from './view360-step';
import { VideoUploadStep } from './video-upload-step';
import { CsvImportWizard } from './csv-import-wizard';
import { useCreateVehicle, usePublishVehicle } from '@/hooks/use-vehicles';
import { useSellerByUserId } from '@/hooks/use-seller';
import { useAuth } from '@/hooks/use-auth';
import { DEALER_PLAN_LIMITS, SELLER_PLAN_LIMITS, DealerPlan, SellerPlan } from '@/lib/plan-config';
import type { SmartVinDecodeResult, CreateVehicleRequest } from '@/services/vehicles';
import type { CsvVehicleRow } from './csv-import-wizard';
import { toast } from 'sonner';
import { useRouter } from 'next/navigation';
import { ChevronLeft, ChevronRight, Save } from 'lucide-react';

// ============================================================
// Types
// ============================================================

export type WizardMode = 'individual' | 'dealer';

export interface VehicleFormData {
  // VIN
  vin: string;
  // Basic info
  make: string;
  makeId: string;
  model: string;
  modelId: string;
  year: number;
  trim: string;
  trimId: string;
  // Specs
  vehicleType: string;
  bodyStyle: string;
  doors: number;
  fuelType: string;
  transmission: string;
  driveType: string;
  engineSize: string;
  cylinders: number;
  horsepower: number;
  // Condition
  mileage: number;
  mileageUnit: 'km' | 'mi';
  condition: string;
  exteriorColor: string;
  interiorColor: string;
  // Features
  features: string[];
  // Photos
  images: UploadedImage[];
  // Videos
  videoUrl: string;
  videoThumbnailUrl: string;
  // Pricing
  price: number;
  currency: 'DOP' | 'USD';
  isNegotiable: boolean;
  acceptsTrades: boolean;
  // Description
  description: string;
  // Location
  province: string;
  city: string;
  // Contact
  sellerName: string;
  sellerPhone: string;
  sellerEmail: string;
  sellerWhatsApp: string;
}

export interface UploadedImage {
  id: string;
  url: string;
  file?: File;
  preview?: string;
  alt?: string;
  order: number;
  isPrimary: boolean;
  isUploading?: boolean;
  progress?: number;
}

type WizardStep =
  | 'method'
  | 'vin-input'
  | 'vin-scan'
  | 'vin-results'
  | 'info'
  | 'photos'
  | 'video'
  | 'view360'
  | 'pricing'
  | 'review'
  | 'csv-import';

const STEP_ORDER: WizardStep[] = [
  'method',
  'info',
  'photos',
  'video',
  'view360',
  'pricing',
  'review',
];

const STEP_LABELS: Record<string, string> = {
  method: 'Método',
  info: 'Información',
  photos: 'Fotos',
  video: 'Video',
  view360: 'Vista 360°',
  pricing: 'Precio',
  review: 'Revisión',
};

const DRAFT_KEY_PREFIX = 'okla_draft_vehicle_';

// ============================================================
// VIN Value Normalizers
// Maps backend VIN decode strings → catalog option keys.
//
// The backend MapBodyStyle / MapFuelType / MapTransmission functions now return
// lowercase catalog-aligned keys (e.g. "suv", "gasoline", "automatic").
// These normalizers handle both the current lowercase output AND the legacy
// PascalCase output ("SUV", "Gasoline", "Automatic") for backwards compatibility,
// as well as raw NHTSA strings in case they ever reach the frontend directly.
// ============================================================

/**
 * Normalize body style → catalog option value.
 * Backend now returns lowercase catalog keys; this handles legacy PascalCase + raw NHTSA.
 */
function normalizeBodyStyle(raw: string): string {
  const s = raw.toLowerCase().trim();
  if (
    s === 'suv' ||
    s.includes('sport utility') ||
    s === 'crossover utility vehicle' ||
    s === 'cuv'
  )
    return 'suv';
  if (
    s === 'pickup' ||
    s === 'truck' ||
    s.includes('pickup') ||
    s.includes('regular cab') ||
    s.includes('crew cab') ||
    s.includes('extended cab')
  )
    return 'pickup';
  if (s === 'minivan' || s.includes('minivan') || s === 'passenger van') return 'minivan';
  if (s === 'van' || s === 'cargo van') return 'van';
  if (s === 'coupe' || s === 'coupé' || s.includes('2dr coupe')) return 'coupe';
  if (s === 'convertible' || s === 'cabriolet' || s === 'roadster') return 'convertible';
  if (s === 'hatchback' || s.includes('hatchback')) return 'hatchback';
  if (s === 'crossover') return 'crossover';
  if (s === 'wagon' || s.includes('wagon') || s === 'estate') return 'wagon';
  if (s === 'sedan' || s === 'saloon' || s.includes('sedan')) return 'sedan';
  return s; // already a catalog key or unknown
}

/**
 * Normalize fuel type → catalog option value.
 * Backend now returns lowercase catalog keys; handles legacy PascalCase + raw NHTSA.
 */
function normalizeFuelType(raw: string): string {
  const s = raw.toLowerCase().trim();
  // Plugin hybrid BEFORE generic hybrid check
  if (
    s === 'plugin_hybrid' ||
    s === 'pluginhybrid' ||
    s === 'plug-in hybrid' ||
    s === 'phev' ||
    s.includes('plug')
  )
    return 'plugin_hybrid';
  if (
    s === 'gasoline' ||
    s.includes('gasoline') ||
    s.includes('unleaded') ||
    s === 'petrol' ||
    s === 'e85'
  )
    return 'gasoline';
  if (s === 'flex_fuel' || s === 'flexfuel' || s.includes('flex fuel') || s.includes('flex-fuel'))
    return 'flex_fuel';
  if (s === 'diesel' || s.includes('diesel')) return 'diesel';
  if (s === 'hybrid' || s.includes('hybrid') || s === 'hev') return 'hybrid';
  if (
    s === 'electric' ||
    s === 'bev' ||
    s === 'ev' ||
    s.includes('battery electric') ||
    s.includes('electric')
  )
    return 'electric';
  if (
    s === 'lpg' ||
    s === 'gas' ||
    s === 'propane' ||
    s === 'cng' ||
    s.includes('natural gas') ||
    s === 'naturalgas' ||
    s === 'hydrogen'
  )
    return 'lpg';
  return s;
}

/**
 * Normalize transmission → catalog option value.
 * Backend now returns lowercase catalog keys; handles legacy PascalCase + raw NHTSA.
 * Catalog keys: automatic | manual | cvt | dct | semi-automatic
 */
function normalizeTransmission(raw: string): string {
  const s = raw.toLowerCase().trim();
  if (s === 'cvt' || s.includes('cvt') || s.includes('continuously variable')) return 'cvt';
  // DCT / Dual Clutch — check BEFORE generic "automatic"
  if (
    s === 'dct' ||
    s === 'dualclutch' ||
    s === 'dual-clutch' ||
    s.includes('dual clutch') ||
    s.includes('dsg') ||
    s.includes('dct')
  )
    return 'dct';
  // Semi-automatic / Automated Manual
  if (
    s === 'semi-automatic' ||
    s === 'semi_automatic' ||
    s === 'automated' ||
    s.includes('automated manual') ||
    s.includes('semi-auto') ||
    s.includes('semi_auto')
  )
    return 'semi-automatic';
  if (s === 'manual' || s.includes('manual') || /^m\d/.test(s)) return 'manual';
  if (s === 'automatic' || s.includes('automatic') || s.includes('auto') || /^a\d/.test(s))
    return 'automatic';
  return s;
}

// ============================================================
// Component
// ============================================================

interface SmartPublishWizardProps {
  mode?: WizardMode;
  dealerId?: string;
  userId?: string;
  dealerPlan?: DealerPlan;
  sellerPlan?: SellerPlan;
}

const initialFormData: VehicleFormData = {
  vin: '',
  make: '',
  makeId: '',
  model: '',
  modelId: '',
  year: new Date().getFullYear(),
  trim: '',
  trimId: '',
  vehicleType: '',
  bodyStyle: '',
  doors: 4,
  fuelType: '',
  transmission: '',
  driveType: '',
  engineSize: '',
  cylinders: 0,
  horsepower: 0,
  mileage: 0,
  mileageUnit: 'km',
  condition: 'Used',
  exteriorColor: '',
  interiorColor: '',
  features: [],
  images: [],
  videoUrl: '',
  videoThumbnailUrl: '',
  price: 0,
  currency: 'DOP',
  isNegotiable: true,
  acceptsTrades: false,
  description: '',
  province: '',
  city: '',
  sellerName: '',
  sellerPhone: '',
  sellerEmail: '',
  sellerWhatsApp: '',
};

// ============================================================
// Draft Serialization
// ============================================================

/**
 * Prepare formData for JSON serialization into localStorage.
 * – Strips non-serializable `File` objects and ephemeral blob/data URLs from images.
 * – Keeps only images that have a real server URL (already uploaded).
 * – Preserves all other scalar/primitive fields unchanged.
 */
function serializeForDraft(data: VehicleFormData, step: WizardStep): Record<string, unknown> {
  const serializableImages = data.images
    .filter(img => img.url && !img.url.startsWith('blob:') && !img.isUploading)
    .map(img => ({
      id: img.id,
      url: img.url,
      order: img.order,
      isPrimary: img.isPrimary,
    }));

  // Spread all fields, override images with the serializable version
  return {
    ...data,
    images: serializableImages,
    _step: step,
    _timestamp: Date.now(),
  };
}

export function SmartPublishWizard({
  mode = 'individual',
  dealerId,
  userId,
  dealerPlan,
  sellerPlan,
}: SmartPublishWizardProps) {
  const router = useRouter();
  const { user } = useAuth();
  const [currentStep, setCurrentStep] = useState<WizardStep>('method');
  const [formData, setFormData] = useState<VehicleFormData>(initialFormData);
  const [vinDecodeResult, setVinDecodeResult] = useState<SmartVinDecodeResult | null>(null);
  const [autoFilledFields, setAutoFilledFields] = useState<Set<string>>(new Set());
  const [showDraftPrompt, setShowDraftPrompt] = useState(false);
  const [profileLoaded, setProfileLoaded] = useState(false);

  const createVehicle = useCreateVehicle();
  const publishVehicle = usePublishVehicle();
  const draftKey = `${DRAFT_KEY_PREFIX}${userId || dealerId || 'anonymous'}`;

  // Fetch seller profile to pre-populate location and contact fields
  const { data: sellerProfile, isLoading: sellerProfileLoading } = useSellerByUserId(userId);

  // Pre-populate form data from seller profile + authenticated user (runs once on profile load)
  // We wait for the seller profile query to complete before populating to avoid partial updates.
  useEffect(() => {
    if (profileLoaded) return;
    if (!user) return;
    // If userId is provided, wait for the seller profile query to finish loading
    if (userId && sellerProfileLoading) return;

    const updates: Partial<VehicleFormData> = {};

    // Location from seller profile
    if (sellerProfile?.state) updates.province = sellerProfile.state;
    if (sellerProfile?.city) updates.city = sellerProfile.city;

    // Contact from seller profile + user
    if (sellerProfile?.fullName) updates.sellerName = sellerProfile.fullName;
    if (user?.phone) updates.sellerPhone = user.phone;
    if (user?.email) updates.sellerEmail = user.email;
    if (sellerProfile?.whatsApp) updates.sellerWhatsApp = sellerProfile.whatsApp;

    if (Object.keys(updates).length > 0) {
      setFormData(prev => ({ ...prev, ...updates }));
    }
    setProfileLoaded(true);
  }, [sellerProfile, sellerProfileLoading, user, userId, profileLoaded]);

  // Check for existing draft on mount
  useEffect(() => {
    let hasDraft = false;
    try {
      const saved = localStorage.getItem(draftKey);
      if (saved) {
        const parsed = JSON.parse(saved);
        if (parsed?.make || parsed?.vin) {
          hasDraft = true;
        }
      }
    } catch {
      /* ignore */
    }
    if (hasDraft) {
      // Use requestAnimationFrame to avoid synchronous setState in effect
      requestAnimationFrame(() => setShowDraftPrompt(true));
    }
  }, [draftKey]);

  // Auto-save to localStorage on step change
  useEffect(() => {
    if (currentStep !== 'method' && (formData.make || formData.vin)) {
      try {
        localStorage.setItem(draftKey, JSON.stringify(serializeForDraft(formData, currentStep)));
      } catch {
        /* ignore */
      }
    }
  }, [currentStep, formData, draftKey]);

  const restoreDraft = useCallback(() => {
    try {
      const saved = localStorage.getItem(draftKey);
      if (saved) {
        const parsed = JSON.parse(saved);
        const { _step, _timestamp: _ts, ...data } = parsed;
        void _ts;

        // Reconstruct UploadedImage objects from serialized draft data
        if (Array.isArray(data.images)) {
          data.images = data.images
            .filter((img: Record<string, unknown>) => img.url && typeof img.url === 'string')
            .map((img: Record<string, unknown>) => ({
              id: (img.id as string) || crypto.randomUUID(),
              url: img.url as string,
              order: (img.order as number) ?? 0,
              isPrimary: (img.isPrimary as boolean) ?? false,
              isUploading: false,
              progress: 100,
            }));
        }

        setFormData(prev => ({ ...prev, ...data }));
        setCurrentStep(_step || 'info');
      }
    } catch {
      /* ignore */
    }
    setShowDraftPrompt(false);
  }, [draftKey]);

  const discardDraft = useCallback(() => {
    localStorage.removeItem(draftKey);
    setShowDraftPrompt(false);
  }, [draftKey]);

  const updateFormData = useCallback((updates: Partial<VehicleFormData>) => {
    setFormData(prev => ({ ...prev, ...updates }));
  }, []);

  // Handle method selection
  const handleMethodSelect = useCallback((method: PublishMethod) => {
    switch (method) {
      case 'vin-keyboard':
        setCurrentStep('vin-input');
        break;
      case 'vin-scan':
        setCurrentStep('vin-scan');
        break;
      case 'manual':
        setCurrentStep('info');
        break;
      case 'csv':
        setCurrentStep('csv-import');
        break;
    }
  }, []);

  // Handle VIN decoded
  const handleVinDecoded = useCallback(
    (result: SmartVinDecodeResult) => {
      setVinDecodeResult(result);

      // Auto-fill form data from decode
      const filled = new Set<string>();
      const updates: Partial<VehicleFormData> = { vin: result.vin };

      if (result.autoFill) {
        const af = result.autoFill;
        if (af.make) {
          updates.make = af.make;
          filled.add('make');
        }
        if (af.model) {
          updates.model = af.model;
          filled.add('model');
        }
        if (af.year) {
          updates.year = af.year;
          filled.add('year');
        }
        if (af.trim) {
          updates.trim = af.trim;
          filled.add('trim');
        }
        if (af.vehicleType) {
          updates.vehicleType = af.vehicleType;
          filled.add('vehicleType');
        }
        if (af.bodyStyle) {
          // Normalize to catalog option value (e.g. "Sport Utility Vehicle" → "suv")
          updates.bodyStyle = normalizeBodyStyle(af.bodyStyle);
          filled.add('bodyStyle');
        }
        if (af.fuelType) {
          // Normalize to catalog option value (e.g. "Regular Unleaded" → "gasoline")
          updates.fuelType = normalizeFuelType(af.fuelType);
          filled.add('fuelType');
        }
        if (af.transmission) {
          // Normalize to catalog option value (e.g. "6-Speed Automatic" → "automatic")
          updates.transmission = normalizeTransmission(af.transmission);
          filled.add('transmission');
        }
        if (af.driveType) {
          updates.driveType = af.driveType;
          filled.add('driveType');
        }
        if (af.engineSize) {
          updates.engineSize = af.engineSize;
          filled.add('engineSize');
        }
        if (af.horsepower) {
          updates.horsepower = af.horsepower;
          filled.add('horsepower');
        }
        if (af.cylinders) {
          updates.cylinders = af.cylinders;
          filled.add('cylinders');
        }
      }
      // Catalog IDs and extra fields from top-level result
      // These IDs are used by VehicleInfoForm to resolve canonical make/model names from catalog
      if (result.catalogMakeId) updates.makeId = result.catalogMakeId;
      if (result.catalogModelId) updates.modelId = result.catalogModelId;
      if (result.catalogTrimId) updates.trimId = result.catalogTrimId;
      if (result.suggestedDescription) {
        updates.description = result.suggestedDescription;
        filled.add('description');
      }
      if (result.doors) {
        updates.doors = result.doors;
        filled.add('doors');
      }

      setAutoFilledFields(filled);
      updateFormData(updates);
      setCurrentStep('vin-results');
    },
    [updateFormData]
  );

  // Navigation
  const mainStepIndex = STEP_ORDER.indexOf(currentStep);
  const isMainStep = mainStepIndex >= 0;

  const goToNext = useCallback(() => {
    if (isMainStep && mainStepIndex < STEP_ORDER.length - 1) {
      setCurrentStep(STEP_ORDER[mainStepIndex + 1]);
    }
  }, [isMainStep, mainStepIndex]);

  const goToPrev = useCallback(() => {
    if (isMainStep && mainStepIndex > 0) {
      setCurrentStep(STEP_ORDER[mainStepIndex - 1]);
    } else if (!isMainStep) {
      setCurrentStep('method');
    }
  }, [isMainStep, mainStepIndex]);

  // Submit
  const handlePublish = useCallback(async () => {
    const request: CreateVehicleRequest = {
      make: formData.make,
      model: formData.model,
      year: formData.year,
      trim: formData.trim || undefined,
      mileage: formData.mileage,
      mileageUnit: formData.mileageUnit, // FIX B1: Send mileage unit to backend
      vin: formData.vin || undefined,
      transmission: formData.transmission,
      fuelType: formData.fuelType,
      bodyType: formData.bodyStyle,
      exteriorColor: formData.exteriorColor || undefined,
      interiorColor: formData.interiorColor || undefined,
      condition: formData.condition,
      price: formData.price,
      currency: formData.currency,
      description: formData.description || undefined,
      features: formData.features,
      images: formData.images
        .filter(img => img.url && !img.url.startsWith('blob:') && !img.isUploading)
        .map(img => ({
          url: img.url,
          order: img.order,
          isPrimary: img.isPrimary,
        })),
      city: formData.city,
      province: formData.province,
      sellerName: formData.sellerName || undefined,
      sellerPhone: formData.sellerPhone || undefined,
      sellerEmail: formData.sellerEmail || undefined,
      sellerWhatsApp: formData.sellerWhatsApp || undefined,
      isNegotiable: formData.isNegotiable,
      // Link vehicle to the authenticated user's account
      sellerId: userId || user?.id || undefined,
    };

    try {
      // Step 1: Create vehicle as Draft
      const created = await createVehicle.mutateAsync(request);

      // Step 2: Submit for review (Draft → PendingReview)
      await publishVehicle.mutateAsync(created.id);

      localStorage.removeItem(draftKey);
      toast.success(
        '¡Tu vehículo ha sido enviado a revisión! Te notificaremos cuando sea aprobado.',
        {
          duration: 6000,
        }
      );
      // Redirect to seller's vehicles dashboard where they can see the status
      router.push('/cuenta/mis-vehiculos');
    } catch (error: unknown) {
      const err = error as {
        message?: string;
        code?: string;
        requiresKyc?: boolean;
        redirectUrl?: string;
      };
      // If dealer KYC is required, redirect to verification page
      if (err.requiresKyc || err.code === 'HTTP_403') {
        toast.error(err.message || 'Debes verificar tu identidad antes de publicar.');
        router.push(err.redirectUrl || '/cuenta/verificacion');
        return;
      }
      toast.error(err.message || 'Error al publicar el vehículo');
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [formData, createVehicle, publishVehicle, draftKey, router]);

  const handleSaveDraft = useCallback(() => {
    try {
      const draft = serializeForDraft(formData, currentStep);
      localStorage.setItem(draftKey, JSON.stringify(draft));
      const imgCount = (draft.images as unknown[]).length;
      toast.success(
        imgCount > 0
          ? `Borrador guardado (${imgCount} foto${imgCount !== 1 ? 's' : ''})`
          : 'Borrador guardado'
      );
    } catch {
      toast.error('Error al guardar borrador');
    }
  }, [formData, currentStep, draftKey]);

  // ========================================
  // Render
  // ========================================

  return (
    <div className="mx-auto max-w-4xl px-4 py-6">
      {/* Draft prompt */}
      {showDraftPrompt && (
        <div className="mb-6 rounded-lg border border-amber-200 bg-amber-50 p-4">
          <p className="text-sm font-medium text-amber-800">
            Tienes un borrador guardado. ¿Deseas continuar donde lo dejaste?
          </p>
          <div className="mt-3 flex gap-3">
            <button
              onClick={restoreDraft}
              className="rounded-md bg-amber-600 px-4 py-2 text-sm font-medium text-white hover:bg-amber-700"
            >
              Continuar borrador
            </button>
            <button
              onClick={discardDraft}
              className="rounded-md border border-amber-300 px-4 py-2 text-sm font-medium text-amber-700 hover:bg-amber-100"
            >
              Empezar de nuevo
            </button>
          </div>
        </div>
      )}

      {/* Progress bar (main steps only) */}
      {isMainStep && mainStepIndex > 0 && (
        <div className="mb-8">
          <div className="mb-2 flex items-center justify-between">
            {STEP_ORDER.filter(s => s !== 'method').map((step, idx) => {
              const stepIdx = idx + 1;
              const active = STEP_ORDER.indexOf(currentStep) >= stepIdx;
              const isCurrent = currentStep === step;
              return (
                <div key={step} className="flex flex-1 items-center">
                  <div
                    className={`flex h-8 w-8 items-center justify-center rounded-full text-sm font-medium transition-colors ${
                      isCurrent
                        ? 'bg-emerald-600 text-white'
                        : active
                          ? 'bg-emerald-100 text-emerald-700'
                          : 'bg-gray-100 text-gray-400'
                    }`}
                  >
                    {idx + 1}
                  </div>
                  <span
                    className={`ml-2 hidden text-sm sm:inline ${isCurrent ? 'font-medium text-gray-900' : 'text-gray-500'}`}
                  >
                    {STEP_LABELS[step]}
                  </span>
                  {idx < STEP_ORDER.length - 2 && (
                    <div
                      className={`mx-3 h-0.5 flex-1 ${active ? 'bg-emerald-200' : 'bg-gray-200'}`}
                    />
                  )}
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* Step content */}
      <div className="min-h-[400px]">
        {currentStep === 'method' && <MethodSelector mode={mode} onSelect={handleMethodSelect} />}

        {currentStep === 'vin-input' && (
          <div className="space-y-6">
            <button
              onClick={() => setCurrentStep('method')}
              className="flex items-center text-sm text-gray-500 hover:text-gray-700"
            >
              <ChevronLeft className="mr-1 h-4 w-4" /> Volver
            </button>
            <VinInput
              value={formData.vin}
              onChange={(vin: string) => updateFormData({ vin })}
              onDecoded={handleVinDecoded}
            />
          </div>
        )}

        {currentStep === 'vin-scan' && (
          <div className="space-y-6">
            <button
              onClick={() => setCurrentStep('method')}
              className="flex items-center text-sm text-gray-500 hover:text-gray-700"
            >
              <ChevronLeft className="mr-1 h-4 w-4" /> Volver
            </button>
            <VinScanner onDecoded={handleVinDecoded} onCancel={() => setCurrentStep('method')} />
          </div>
        )}

        {currentStep === 'vin-results' && vinDecodeResult && (
          <div className="space-y-6">
            <button
              onClick={() => setCurrentStep('vin-input')}
              className="flex items-center text-sm text-gray-500 hover:text-gray-700"
            >
              <ChevronLeft className="mr-1 h-4 w-4" /> Volver
            </button>
            <VinDecodeResults
              result={vinDecodeResult}
              onContinue={() => setCurrentStep('info')}
              onEditManual={() => setCurrentStep('info')}
            />
          </div>
        )}

        {currentStep === 'info' && (
          <VehicleInfoForm
            data={formData}
            onChange={updateFormData}
            autoFilledFields={autoFilledFields}
          />
        )}

        {currentStep === 'photos' && (
          <PhotoUploadManager
            initialPhotos={formData.images.map(img => ({
              id: img.id,
              file: img.file ?? new File([], img.url),
              url: img.preview || img.url,
              order: img.order,
              isPrimary: img.isPrimary,
              status: img.url ? ('uploaded' as const) : ('pending' as const),
              progress: img.progress ?? (img.url ? 100 : 0),
              mediaId: img.id,
            }))}
            onPhotosChange={(photos: PhotoItem[]) => {
              const images: UploadedImage[] = photos.map(p => ({
                id: p.mediaId || p.id,
                url: p.thumbnailUrl || p.url,
                file: p.file,
                preview: p.url,
                order: p.order,
                isPrimary: p.isPrimary,
                isUploading: p.status === 'uploading' || p.status === 'compressing',
                progress: p.progress,
              }));
              updateFormData({ images });
            }}
            accountType={mode === 'dealer' ? 'dealer' : 'individual'}
            dealerPlan={dealerPlan}
            sellerPlan={sellerPlan}
            show360Tab={mode === 'dealer'}
            showBgRemoval={mode === 'dealer'}
          />
        )}

        {currentStep === 'video' && (
          <VideoUploadStep
            accountType={mode === 'dealer' ? 'dealer' : 'individual'}
            videoUrl={formData.videoUrl}
            onVideoUploaded={(url, thumbnailUrl) => {
              setFormData(prev => ({
                ...prev,
                videoUrl: url,
                videoThumbnailUrl: thumbnailUrl || '',
              }));
            }}
            onSkip={() => goToNext()}
            onComplete={() => goToNext()}
          />
        )}

        {currentStep === 'view360' && (
          <View360Step
            vehicleId={formData.vin || undefined}
            accountType={mode === 'dealer' ? 'dealer' : 'individual'}
            dealerPlan={dealerPlan}
            sellerPlan={sellerPlan}
            onSkip={() => setCurrentStep('pricing')}
            onComplete={() => setCurrentStep('pricing')}
          />
        )}

        {currentStep === 'pricing' && <PricingStep data={formData} onChange={updateFormData} />}

        {currentStep === 'review' && (
          <ReviewStep
            data={formData}
            onPublish={handlePublish}
            onSaveDraft={handleSaveDraft}
            onBack={() => setCurrentStep('pricing')}
            isPublishing={createVehicle.isPending}
          />
        )}

        {currentStep === 'csv-import' &&
          (() => {
            // Plan-based gating for bulk CSV import
            const isBulkUploadAllowed = (() => {
              if (mode === 'dealer') {
                const plan = dealerPlan || DealerPlan.LIBRE;
                return DEALER_PLAN_LIMITS[plan]?.bulkUpload ?? false;
              }
              // Individual sellers don't have bulkUpload in their plan features
              return false;
            })();

            if (!isBulkUploadAllowed) {
              return (
                <div className="space-y-6">
                  <button
                    onClick={() => setCurrentStep('method')}
                    className="flex items-center text-sm text-gray-500 hover:text-gray-700"
                  >
                    <ChevronLeft className="mr-1 h-4 w-4" /> Volver
                  </button>
                  <div className="mx-auto max-w-md rounded-xl border border-amber-200 bg-gradient-to-br from-amber-50 to-yellow-50 p-8 text-center">
                    <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-amber-100">
                      <svg
                        xmlns="http://www.w3.org/2000/svg"
                        className="h-8 w-8 text-amber-500"
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="currentColor"
                        strokeWidth="2"
                        strokeLinecap="round"
                        strokeLinejoin="round"
                      >
                        <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
                        <path d="M7 11V7a5 5 0 0 1 10 0v4" />
                      </svg>
                    </div>
                    <h3 className="mb-2 text-xl font-bold text-gray-900">
                      Importación Masiva — Función Premium
                    </h3>
                    <p className="mb-6 text-sm text-gray-600">
                      La importación masiva por CSV está disponible en los planes Visible, Pro y
                      Elite. Actualiza tu plan para publicar hasta 50 vehículos a la vez.
                    </p>
                    <button className="inline-flex items-center gap-2 rounded-lg bg-gradient-to-r from-amber-500 to-yellow-500 px-6 py-3 text-sm font-medium text-white shadow-sm hover:from-amber-600 hover:to-yellow-600">
                      Actualizar Plan
                    </button>
                  </div>
                </div>
              );
            }

            return (
              <div className="space-y-6">
                <button
                  onClick={() => setCurrentStep('method')}
                  className="flex items-center text-sm text-gray-500 hover:text-gray-700"
                >
                  <ChevronLeft className="mr-1 h-4 w-4" /> Volver
                </button>
                <CsvImportWizard
                  onImportComplete={(rows: CsvVehicleRow[]) => {
                    toast.success(`${rows.length} vehículos importados`);
                    setCurrentStep('method');
                  }}
                  onCancel={() => setCurrentStep('method')}
                />
              </div>
            );
          })()}
      </div>

      {/* Navigation buttons (main steps only — hidden on review since ReviewStep has its own actions) */}
      {isMainStep && mainStepIndex > 0 && currentStep !== 'review' && (
        <div className="mt-8 flex items-center justify-between border-t pt-6">
          <button
            onClick={goToPrev}
            className="flex items-center rounded-lg border border-gray-300 px-5 py-2.5 text-sm font-medium text-gray-700 hover:bg-gray-50"
          >
            <ChevronLeft className="mr-1 h-4 w-4" />
            Anterior
          </button>

          <div className="flex gap-3">
            <button
              onClick={handleSaveDraft}
              className="flex items-center rounded-lg border border-gray-300 px-5 py-2.5 text-sm font-medium text-gray-700 hover:bg-gray-50"
            >
              <Save className="mr-1 h-4 w-4" />
              Guardar borrador
            </button>

            <button
              onClick={goToNext}
              className="flex items-center rounded-lg bg-emerald-600 px-5 py-2.5 text-sm font-medium text-white hover:bg-emerald-700"
            >
              Siguiente
              <ChevronRight className="ml-1 h-4 w-4" />
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
