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
import { CsvImportWizard } from './csv-import-wizard';
import { useCreateVehicle, usePublishVehicle } from '@/hooks/use-vehicles';
import { useSellerByUserId } from '@/hooks/use-seller';
import { useAuth } from '@/hooks/use-auth';
import type { SmartVinDecodeResult, CreateVehicleRequest } from '@/services/vehicles';
import type { CsvVehicleRow } from './csv-import-wizard';
import { toast } from 'sonner';
import { useRouter } from 'next/navigation';
import { ChevronLeft, ChevronRight, Save, Send, Loader2 } from 'lucide-react';

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
  | 'pricing'
  | 'review'
  | 'csv-import';

const STEP_ORDER: WizardStep[] = ['method', 'info', 'photos', 'pricing', 'review'];

const STEP_LABELS: Record<string, string> = {
  method: 'Método',
  info: 'Información',
  photos: 'Fotos',
  pricing: 'Precio',
  review: 'Revisión',
};

const DRAFT_KEY_PREFIX = 'okla_draft_vehicle_';

// ============================================================
// VIN Value Normalizers
// Maps raw NHTSA / backend strings → catalog option keys
// ============================================================

/**
 * Normalize body style from VIN decode to catalog option values.
 * NHTSA returns verbose strings like "Sport Utility Vehicle (SUV)".
 */
function normalizeBodyStyle(raw: string): string {
  const s = raw.toLowerCase().trim();
  if (
    s.includes('sport utility') ||
    s === 'suv' ||
    s === 'crossover utility vehicle' ||
    s === 'cuv'
  )
    return 'suv';
  if (s === 'sedan' || s === 'saloon' || s.includes('4dr sedan') || s.includes('sedan/saloon'))
    return 'sedan';
  if (
    s.includes('pickup') ||
    s === 'truck' ||
    s.includes('regular cab') ||
    s.includes('crew cab') ||
    s.includes('extended cab')
  )
    return 'pickup';
  if (s === 'hatchback' || s.includes('3dr hatchback') || s.includes('5dr hatchback'))
    return 'hatchback';
  if (s === 'coupe' || s === 'coupé' || s.includes('2dr coupe')) return 'coupe';
  if (s.includes('minivan') || s === 'passenger van') return 'minivan';
  if (s === 'van' || s === 'cargo van') return 'minivan';
  if (s === 'crossover') return 'crossover';
  if (s.includes('wagon') || s === 'estate') return 'wagon';
  if (s === 'convertible' || s === 'cabriolet' || s === 'roadster') return 'convertible';
  return s; // lowercase fallback
}

/**
 * Normalize fuel type from VIN decode to catalog option values.
 * NHTSA returns values like "Gasoline", "Regular Unleaded", "Flex Fuel".
 */
function normalizeFuelType(raw: string): string {
  const s = raw.toLowerCase().trim();
  if (
    s.includes('gasoline') ||
    s === 'gas' ||
    s.includes('unleaded') ||
    s === 'petrol' ||
    s.includes('flex fuel') ||
    s === 'e85'
  )
    return 'gasoline';
  if (s.includes('diesel')) return 'diesel';
  if (
    s.includes('plug-in hybrid') ||
    s === 'phev' ||
    s.includes('hybrid') ||
    s === 'hev'
  )
    return 'hybrid';
  if (s === 'electric' || s === 'bev' || s === 'ev' || s.includes('battery electric'))
    return 'electric';
  if (s === 'lpg' || s === 'propane' || s === 'cng' || s.includes('natural gas')) return 'lpg';
  return s;
}

/**
 * Normalize transmission from VIN decode to catalog option values.
 * NHTSA returns values like "6-Speed Automatic", "Manual", "CVT".
 */
function normalizeTransmission(raw: string): string {
  const s = raw.toLowerCase().trim();
  if (s.includes('cvt') || s.includes('continuously variable')) return 'cvt';
  if (
    s.includes('dual clutch') ||
    s.includes('dct') ||
    s.includes('dsg') ||
    s.includes('automated manual') ||
    s.includes('semi-auto')
  )
    return 'semi-automatic';
  // Must check CVT & dual-clutch BEFORE generic "automatic"
  if (s.includes('automatic') || s.includes('auto') || /^a\d/.test(s)) return 'automatic';
  if (s.includes('manual') || /^m\d/.test(s)) return 'manual';
  return s;
}

// ============================================================
// Component
// ============================================================

interface SmartPublishWizardProps {
  mode?: WizardMode;
  dealerId?: string;
  userId?: string;
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
  condition: 'used',
  exteriorColor: '',
  interiorColor: '',
  features: [],
  images: [],
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

export function SmartPublishWizard({
  mode = 'individual',
  dealerId,
  userId,
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
        localStorage.setItem(
          draftKey,
          JSON.stringify({ ...formData, _step: currentStep, _timestamp: Date.now() })
        );
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
        .filter(img => img.url)
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
    };

    try {
      // Step 1: Create vehicle as Draft
      const created = await createVehicle.mutateAsync(request);

      // Step 2: Publish it (Draft → Active) so it appears in listings
      await publishVehicle.mutateAsync(created.id);

      localStorage.removeItem(draftKey);
      toast.success('¡Vehículo publicado exitosamente!');
      router.push(`/vehiculos/${created.slug}`);
    } catch (error: unknown) {
      const err = error as { message?: string };
      toast.error(err.message || 'Error al publicar el vehículo');
    }
  }, [formData, createVehicle, publishVehicle, draftKey, router]);

  const handleSaveDraft = useCallback(() => {
    try {
      localStorage.setItem(
        draftKey,
        JSON.stringify({ ...formData, _step: currentStep, _timestamp: Date.now() })
      );
      toast.success('Borrador guardado');
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
            show360Tab={mode === 'dealer'}
            showBgRemoval={mode === 'dealer'}
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

        {currentStep === 'csv-import' && (
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
        )}
      </div>

      {/* Navigation buttons (main steps only) */}
      {isMainStep && mainStepIndex > 0 && (
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

            {currentStep !== 'review' ? (
              <button
                onClick={goToNext}
                className="flex items-center rounded-lg bg-emerald-600 px-5 py-2.5 text-sm font-medium text-white hover:bg-emerald-700"
              >
                Siguiente
                <ChevronRight className="ml-1 h-4 w-4" />
              </button>
            ) : (
              <button
                onClick={handlePublish}
                disabled={createVehicle.isPending}
                className="flex items-center rounded-lg bg-emerald-600 px-6 py-2.5 text-sm font-medium text-white hover:bg-emerald-700 disabled:opacity-50"
              >
                {createVehicle.isPending ? (
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                ) : (
                  <Send className="mr-2 h-4 w-4" />
                )}
                {mode === 'dealer' ? 'Publicar' : 'Publicar (RD$29)'}
              </button>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
