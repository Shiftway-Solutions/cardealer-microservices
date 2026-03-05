---
title: "📝 Página de Publicar Vehículo"
priority: P1
estimated_time: "60 minutos"
dependencies: []
apis: []
status: complete
last_updated: "2026-01-30"
---

# 📝 Página de Publicar Vehículo

> **Tiempo estimado:** 60 minutos
> **Prerrequisitos:** Formularios, subida de imágenes, autenticación

---

## 📋 OBJETIVO

Implementar flujo de publicación:

- Formulario multi-paso
- Subida de imágenes
- Preview antes de publicar
- Guardado de borradores

---

## 🎨 WIREFRAME - PUBLICAR VEHÍCULO (Multi-Step)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ HEADER (Navbar)                                                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│                         PUBLICAR VEHÍCULO                                    │
│            Completa los datos de tu vehículo para publicarlo                │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │ PASO 1          PASO 2          PASO 3          PASO 4          PASO 5  ││
│  │ ●──────────────○──────────────○──────────────○──────────────○           ││
│  │ Info Básica    Detalles       Fotos          Precio         Revisar    ││
│  │ ════════                                                                ││
│  └─────────────────────────────────────────────────────────────────────────┘│
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │ INFORMACIÓN BÁSICA                                            (1 de 5)  ││
│  │                                                                         ││
│  │  Marca *                              Modelo *                          ││
│  │  ┌─────────────────────────────┐      ┌─────────────────────────────┐   ││
│  │  │ Seleccionar marca...    ▼  │      │ Seleccionar modelo...    ▼  │   ││
│  │  └─────────────────────────────┘      └─────────────────────────────┘   ││
│  │                                                                         ││
│  │  Año *                                Versión/Trim                      ││
│  │  ┌─────────────────────────────┐      ┌─────────────────────────────┐   ││
│  │  │ 2024                     ▼  │      │ LX, EX, Sport...           │   ││
│  │  └─────────────────────────────┘      └─────────────────────────────┘   ││
│  │                                                                         ││
│  │  Kilometraje *                        VIN (Opcional)                    ││
│  │  ┌─────────────────────────────┐      ┌─────────────────────────────┐   ││
│  │  │ 25,000                   km │      │ 1HGCM66543A123456          │   ││
│  │  └─────────────────────────────┘      └─────────────────────────────┘   ││
│  │                                                                         ││
│  │  Condición *                                                            ││
│  │  ○ Nuevo    ● Usado    ○ Certificado                                   ││
│  │                                                                         ││
│  └─────────────────────────────────────────────────────────────────────────┘│
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │                    [💾 Guardar Borrador]    [Siguiente → Detalles]      ││
│  └─────────────────────────────────────────────────────────────────────────┘│
│                                                                              │
├─────────────────────────────────────────────────────────────────────────────┤
│ FOOTER                                                                       │
└─────────────────────────────────────────────────────────────────────────────┘
```

## 🎨 WIREFRAME - PASO 3: FOTOS

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │ PASO 1          PASO 2          PASO 3          PASO 4          PASO 5  ││
│  │ ✓──────────────✓──────────────●──────────────○──────────────○           ││
│  │ Info Básica    Detalles       Fotos          Precio         Revisar    ││
│  │                               ══════                                    ││
│  └─────────────────────────────────────────────────────────────────────────┘│
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │ FOTOS DEL VEHÍCULO                                            (3 de 5)  ││
│  │                                                                         ││
│  │  ⚠️ Sube mínimo 5 fotos. La primera será la principal.                  ││
│  │                                                                         ││
│  │  ┌─────────────────────────────────────────────────────────────────┐    ││
│  │  │                                                                 │    ││
│  │  │     📷 Arrastra tus fotos aquí o haz clic para seleccionar     │    ││
│  │  │                                                                 │    ││
│  │  │     Formatos: JPG, PNG • Max: 10MB por imagen • Hasta 20 fotos │    ││
│  │  │                                                                 │    ││
│  │  └─────────────────────────────────────────────────────────────────┘    ││
│  │                                                                         ││
│  │  FOTOS SUBIDAS (4/20):                                                  ││
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐                   ││
│  │  │ 📷 Foto 1│ │ 📷 Foto 2│ │ 📷 Foto 3│ │ 📷 Foto 4│                   ││
│  │  │ ⭐ PRINC │ │          │ │          │ │          │                   ││
│  │  │    [✕]  │ │    [✕]  │ │    [✕]  │ │    [✕]  │                   ││
│  │  └──────────┘ └──────────┘ └──────────┘ └──────────┘                   ││
│  │                                                                         ││
│  │  💡 Tip: Incluye exterior, interior, motor y detalles importantes      ││
│  │                                                                         ││
│  └─────────────────────────────────────────────────────────────────────────┘│
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │  [← Detalles]           [💾 Guardar]           [Siguiente → Precio]     ││
│  └─────────────────────────────────────────────────────────────────────────┘│
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔧 PASO 1: Página Principal

```typescript
// filepath: src/app/(main)/publicar/page.tsx
import { Metadata } from "next";
import { redirect } from "next/navigation";
import { auth } from "@/lib/auth";
import { PublishForm } from "@/components/publish/PublishForm";

export const metadata: Metadata = {
  title: "Publicar Vehículo | OKLA",
  description: "Publica tu vehículo y llega a miles de compradores",
};

export default async function PublishPage() {
  const session = await auth();

  if (!session?.user) {
    redirect("/login?callbackUrl=/publicar");
  }

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="container max-w-3xl">
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold text-gray-900">
            Publicar Vehículo
          </h1>
          <p className="text-gray-600 mt-2">
            Completa los datos de tu vehículo para publicarlo
          </p>
        </div>

        <PublishForm />
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 2: Hook de Multi-Step

```typescript
// filepath: src/lib/hooks/useMultiStepForm.ts
"use client";

import * as React from "react";

interface UseMultiStepFormProps {
  totalSteps: number;
  initialStep?: number;
}

export function useMultiStepForm({
  totalSteps,
  initialStep = 0,
}: UseMultiStepFormProps) {
  const [currentStep, setCurrentStep] = React.useState(initialStep);
  const [completedSteps, setCompletedSteps] = React.useState<Set<number>>(
    new Set(),
  );

  const isFirstStep = currentStep === 0;
  const isLastStep = currentStep === totalSteps - 1;
  const progress = ((currentStep + 1) / totalSteps) * 100;

  const goToStep = (step: number) => {
    if (step >= 0 && step < totalSteps) {
      setCurrentStep(step);
    }
  };

  const nextStep = () => {
    if (!isLastStep) {
      setCompletedSteps((prev) => new Set([...prev, currentStep]));
      setCurrentStep((prev) => prev + 1);
    }
  };

  const prevStep = () => {
    if (!isFirstStep) {
      setCurrentStep((prev) => prev - 1);
    }
  };

  const markComplete = (step: number) => {
    setCompletedSteps((prev) => new Set([...prev, step]));
  };

  const isStepComplete = (step: number) => completedSteps.has(step);

  return {
    currentStep,
    totalSteps,
    isFirstStep,
    isLastStep,
    progress,
    goToStep,
    nextStep,
    prevStep,
    markComplete,
    isStepComplete,
  };
}
```

---

## 🔧 PASO 3: PublishForm Container

```typescript
// filepath: src/components/publish/PublishForm.tsx
"use client";

import * as React from "react";
import { useRouter } from "next/navigation";
import { FormProvider, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { PublishSteps } from "./PublishSteps";
import { BasicInfoStep } from "./steps/BasicInfoStep";
import { DetailsStep } from "./steps/DetailsStep";
import { PhotosStep } from "./steps/PhotosStep";
import { Video360Step } from "./steps/Video360Step"; // ← NUEVO
import { PricingStep } from "./steps/PricingStep";
import { PreviewStep } from "./steps/PreviewStep";
import { useMultiStepForm } from "@/lib/hooks/useMultiStepForm";
import { vehicleService } from "@/lib/services/vehicleService";
import { showToast } from "@/lib/toast";

const publishSchema = z.object({
  // Basic Info
  makeId: z.string().min(1, "Selecciona una marca"),
  modelId: z.string().min(1, "Selecciona un modelo"),
  year: z.number().min(1990).max(new Date().getFullYear() + 1),
  trim: z.string().optional(),
  condition: z.enum(["new", "used"]),

  // Details
  mileage: z.number().min(0),
  transmission: z.enum(["automatic", "manual"]),
  fuelType: z.enum(["gasoline", "diesel", "electric", "hybrid"]),
  bodyType: z.string(),
  exteriorColor: z.string(),
  interiorColor: z.string().optional(),
  drivetrain: z.string().optional(),
  engine: z.string().optional(),
  vin: z.string().optional(),
  description: z.string().min(20, "Mínimo 20 caracteres"),
  features: z.array(z.string()),

  // Photos
  images: z.array(z.object({
    id: z.string(),
    url: z.string(),
    isPrimary: z.boolean(),
  })).min(1, "Agrega al menos 1 foto"),

  // Pricing
  price: z.number().min(10000, "Precio mínimo RD$ 10,000"),
  isNegotiable: z.boolean(),
  acceptsTrade: z.boolean().optional(),

  // Location
  city: z.string().min(1, "Selecciona ciudad"),
  province: z.string().min(1, "Selecciona provincia"),
});

export type PublishFormData = z.infer<typeof publishSchema>;

const STEPS = [
  { title: "Información básica", component: BasicInfoStep },
  { title: "Detalles", component: DetailsStep },
  { title: "Fotos", component: PhotosStep },
  { title: "Video 360°", component: Video360Step }, // ← NUEVO: Paso para vista 360°
  { title: "Precio", component: PricingStep },
  { title: "Revisar", component: PreviewStep },
];

export function PublishForm() {
  const router = useRouter();
  const [isSubmitting, setIsSubmitting] = React.useState(false);

  const multiStep = useMultiStepForm({
    totalSteps: STEPS.length,
  });

  const methods = useForm<PublishFormData>({
    resolver: zodResolver(publishSchema),
    defaultValues: {
      condition: "used",
      mileage: 0,
      transmission: "automatic",
      fuelType: "gasoline",
      images: [],
      features: [],
      isNegotiable: true,
      acceptsTrade: false,
    },
    mode: "onChange",
  });

  const CurrentStepComponent = STEPS[multiStep.currentStep].component;

  const handleNext = async () => {
    // Validate current step fields
    const fieldsToValidate = getStepFields(multiStep.currentStep);
    const isValid = await methods.trigger(fieldsToValidate);

    if (isValid) {
      multiStep.nextStep();
      window.scrollTo({ top: 0, behavior: "smooth" });
    }
  };

  const handleSubmit = async (data: PublishFormData) => {
    setIsSubmitting(true);
    try {
      const result = await vehicleService.create(data);
      showToast.success("¡Vehículo publicado!", "Tu anuncio está activo");
      router.push(`/vehiculos/${result.slug}`);
    } catch (error) {
      showToast.error("Error al publicar", "Intenta de nuevo");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <FormProvider {...methods}>
      <form onSubmit={methods.handleSubmit(handleSubmit)}>
        {/* Progress Steps */}
        <PublishSteps
          steps={STEPS.map((s) => s.title)}
          currentStep={multiStep.currentStep}
          onStepClick={multiStep.goToStep}
        />

        {/* Current Step */}
        <div className="bg-white rounded-xl shadow-sm p-6 mt-6">
          <CurrentStepComponent />
        </div>

        {/* Navigation */}
        <div className="flex justify-between mt-6">
          {!multiStep.isFirstStep && (
            <button
              type="button"
              onClick={multiStep.prevStep}
              className="px-6 py-2 text-gray-600 hover:text-gray-900"
            >
              Anterior
            </button>
          )}

          <div className="ml-auto">
            {multiStep.isLastStep ? (
              <button
                type="submit"
                disabled={isSubmitting}
                className="px-8 py-3 bg-primary-600 text-white rounded-lg hover:bg-primary-700 disabled:opacity-50"
              >
                {isSubmitting ? "Publicando..." : "Publicar vehículo"}
              </button>
            ) : (
              <button
                type="button"
                onClick={handleNext}
                className="px-8 py-3 bg-primary-600 text-white rounded-lg hover:bg-primary-700"
              >
                Siguiente
              </button>
            )}
          </div>
        </div>
      </form>
    </FormProvider>
  );
}

function getStepFields(step: number): (keyof PublishFormData)[] {
  switch (step) {
    case 0:
      return ["makeId", "modelId", "year", "condition"];
    case 1:
      return ["mileage", "transmission", "fuelType", "description"];
    case 2:
      return ["images"];
    case 3:
      return ["price", "city", "province"];
    default:
      return [];
  }
}
```

---

## 🔧 PASO 4: PublishSteps

```typescript
// filepath: src/components/publish/PublishSteps.tsx
import { Check } from "lucide-react";
import { cn } from "@/lib/utils";

interface PublishStepsProps {
  steps: string[];
  currentStep: number;
  onStepClick: (step: number) => void;
}

export function PublishSteps({
  steps,
  currentStep,
  onStepClick,
}: PublishStepsProps) {
  return (
    <div className="flex items-center justify-between">
      {steps.map((step, index) => {
        const isComplete = index < currentStep;
        const isCurrent = index === currentStep;

        return (
          <div key={step} className="flex items-center flex-1">
            <button
              type="button"
              onClick={() => isComplete && onStepClick(index)}
              disabled={!isComplete}
              className={cn(
                "flex flex-col items-center flex-1",
                isComplete && "cursor-pointer"
              )}
            >
              {/* Circle */}
              <div
                className={cn(
                  "w-10 h-10 rounded-full flex items-center justify-center text-sm font-medium",
                  isComplete && "bg-primary-600 text-white",
                  isCurrent && "bg-primary-600 text-white ring-4 ring-primary-100",
                  !isComplete && !isCurrent && "bg-gray-200 text-gray-600"
                )}
              >
                {isComplete ? <Check size={18} /> : index + 1}
              </div>

              {/* Label */}
              <span
                className={cn(
                  "mt-2 text-xs font-medium hidden sm:block",
                  isCurrent ? "text-primary-600" : "text-gray-500"
                )}
              >
                {step}
              </span>
            </button>

            {/* Connector */}
            {index < steps.length - 1 && (
              <div
                className={cn(
                  "h-0.5 flex-1 mx-2",
                  index < currentStep ? "bg-primary-600" : "bg-gray-200"
                )}
              />
            )}
          </div>
        );
      })}
    </div>
  );
}
```

---

## 🔧 PASO 5: BasicInfoStep

```typescript
// filepath: src/components/publish/steps/BasicInfoStep.tsx
"use client";

import { useFormContext } from "react-hook-form";
import { FormField } from "@/components/ui/FormField";
import { Select } from "@/components/ui/Select";
import { RadioGroup } from "@/components/ui/RadioGroup";
import { useMakes, useModels } from "@/lib/hooks/useCatalog";
import type { PublishFormData } from "../PublishForm";

export function BasicInfoStep() {
  const { register, watch, formState: { errors } } = useFormContext<PublishFormData>();
  const selectedMakeId = watch("makeId");

  const { data: makes } = useMakes();
  const { data: models } = useModels(selectedMakeId);

  const currentYear = new Date().getFullYear();
  const years = Array.from({ length: 35 }, (_, i) => currentYear + 1 - i);

  return (
    <div className="space-y-6">
      <h2 className="text-xl font-semibold text-gray-900">
        Información básica
      </h2>

      {/* Condition */}
      <FormField label="Condición" error={errors.condition?.message}>
        <RadioGroup
          options={[
            { value: "new", label: "Nuevo" },
            { value: "used", label: "Usado" },
          ]}
          {...register("condition")}
        />
      </FormField>

      {/* Make */}
      <FormField label="Marca" error={errors.makeId?.message}>
        <Select {...register("makeId")}>
          <option value="">Seleccionar marca</option>
          {makes?.map((make) => (
            <option key={make.id} value={make.id}>
              {make.name}
            </option>
          ))}
        </Select>
      </FormField>

      {/* Model */}
      <FormField label="Modelo" error={errors.modelId?.message}>
        <Select {...register("modelId")} disabled={!selectedMakeId}>
          <option value="">Seleccionar modelo</option>
          {models?.map((model) => (
            <option key={model.id} value={model.id}>
              {model.name}
            </option>
          ))}
        </Select>
      </FormField>

      {/* Year */}
      <FormField label="Año" error={errors.year?.message}>
        <Select {...register("year", { valueAsNumber: true })}>
          <option value="">Seleccionar año</option>
          {years.map((year) => (
            <option key={year} value={year}>
              {year}
            </option>
          ))}
        </Select>
      </FormField>
    </div>
  );
}
```

---

## 🔧 PASO 6: PhotosStep

```typescript
// filepath: src/components/publish/steps/PhotosStep.tsx
"use client";

import * as React from "react";
import { useFormContext } from "react-hook-form";
import { Upload, X, Star, Image as ImageIcon } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { ImageUploader } from "@/components/forms/ImageUploader";
import { cn } from "@/lib/utils";
import type { PublishFormData } from "../PublishForm";

export function PhotosStep() {
  const { watch, setValue, formState: { errors } } = useFormContext<PublishFormData>();
  const images = watch("images") || [];

  const handleUpload = (newImages: { id: string; url: string }[]) => {
    const updatedImages = [
      ...images,
      ...newImages.map((img, i) => ({
        ...img,
        isPrimary: images.length === 0 && i === 0,
      })),
    ];
    setValue("images", updatedImages);
  };

  const handleRemove = (id: string) => {
    const filtered = images.filter((img) => img.id !== id);
    // If we removed the primary, make first one primary
    if (filtered.length > 0 && !filtered.some((img) => img.isPrimary)) {
      filtered[0].isPrimary = true;
    }
    setValue("images", filtered);
  };

  const handleSetPrimary = (id: string) => {
    const updated = images.map((img) => ({
      ...img,
      isPrimary: img.id === id,
    }));
    setValue("images", updated);
  };

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold text-gray-900">Fotos</h2>
        <p className="text-gray-600 mt-1">
          Agrega hasta 20 fotos de tu vehículo
        </p>
      </div>

      {/* Upload area */}
      <ImageUploader
        onUpload={handleUpload}
        maxFiles={20 - images.length}
        disabled={images.length >= 20}
      />

      {/* Error */}
      {errors.images && (
        <p className="text-sm text-red-600">{errors.images.message}</p>
      )}

      {/* Preview grid */}
      {images.length > 0 && (
        <div className="grid grid-cols-3 sm:grid-cols-4 gap-4">
          {images.map((image) => (
            <div
              key={image.id}
              className={cn(
                "relative aspect-[4/3] rounded-lg overflow-hidden group",
                image.isPrimary && "ring-2 ring-primary-600"
              )}
            >
              <img
                src={image.url}
                alt="Foto del vehículo"
                className="w-full h-full object-cover"
              />

              {/* Primary badge */}
              {image.isPrimary && (
                <span className="absolute top-2 left-2 px-2 py-1 bg-primary-600 text-white text-xs rounded">
                  Principal
                </span>
              )}

              {/* Actions overlay */}
              <div className="absolute inset-0 bg-black/50 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center gap-2">
                {!image.isPrimary && (
                  <button
                    type="button"
                    onClick={() => handleSetPrimary(image.id)}
                    className="p-2 bg-white rounded-full hover:bg-gray-100"
                    title="Hacer principal"
                  >
                    <Star size={16} />
                  </button>
                )}
                <button
                  type="button"
                  onClick={() => handleRemove(image.id)}
                  className="p-2 bg-red-500 text-white rounded-full hover:bg-red-600"
                  title="Eliminar"
                >
                  <X size={16} />
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Tips */}
      <div className="bg-blue-50 rounded-lg p-4">
        <h4 className="font-medium text-blue-900">Tips para mejores fotos</h4>
        <ul className="mt-2 text-sm text-blue-700 space-y-1">
          <li>• Toma fotos con buena iluminación</li>
          <li>• Incluye exterior, interior y motor</li>
          <li>• Muestra detalles importantes</li>
          <li>• La primera foto será la principal</li>
        </ul>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 7: Video360Step (NUEVO - Vista 360° Opcional)

```typescript
// filepath: src/components/publish/steps/Video360Step.tsx
"use client";

import * as React from "react";
import { useFormContext } from "react-hook-form";
import { motion, AnimatePresence } from "framer-motion";
import {
  Video,
  RotateCcw,
  Upload,
  X,
  CheckCircle,
  AlertCircle,
  Loader2,
  Info,
  Sparkles,
} from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Progress } from "@/components/ui/Progress";
import { Vehicle360Viewer } from "@/components/vehicles/Vehicle360Viewer";
import { useVehicle360 } from "@/lib/hooks/useVehicle360";
import { cn, formatFileSize } from "@/lib/utils";
import type { PublishFormData } from "../PublishForm";

const ACCEPTED_FORMATS = ["video/mp4", "video/quicktime", "video/avi", "video/webm"];
const MAX_FILE_SIZE = 500 * 1024 * 1024; // 500MB
const MAX_DURATION = 60; // 60 segundos

export function Video360Step() {
  const { watch, setValue } = useFormContext<PublishFormData>();
  const vehicleId = watch("id"); // ID temporal del vehículo en borrador

  // Hook para manejo de 360°
  const {
    view,
    job,
    isLoading,
    isProcessing,
    progress,
    error,
    uploadAndProcess,
    cancelProcessing,
    refetch,
  } = useVehicle360(vehicleId, { autoFetch: !!vehicleId });

  const [selectedFile, setSelectedFile] = React.useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = React.useState<string | null>(null);
  const [validationError, setValidationError] = React.useState<string | null>(null);
  const [isDragOver, setIsDragOver] = React.useState(false);
  const [skipStep, setSkipStep] = React.useState(false);

  const inputRef = React.useRef<HTMLInputElement>(null);

  // Cleanup preview URL on unmount
  React.useEffect(() => {
    return () => {
      if (previewUrl) {
        URL.revokeObjectURL(previewUrl);
      }
    };
  }, [previewUrl]);

  const validateFile = async (file: File): Promise<string | null> => {
    if (!ACCEPTED_FORMATS.includes(file.type)) {
      return "Formato no soportado. Usa MP4, MOV, AVI o WebM.";
    }

    if (file.size > MAX_FILE_SIZE) {
      return `El archivo es muy grande. Máximo ${formatFileSize(MAX_FILE_SIZE)}.`;
    }

    // Check duration
    return new Promise((resolve) => {
      const video = document.createElement("video");
      video.preload = "metadata";

      video.onloadedmetadata = () => {
        URL.revokeObjectURL(video.src);
        if (video.duration > MAX_DURATION) {
          resolve(`El video es muy largo. Máximo ${MAX_DURATION} segundos.`);
        } else {
          resolve(null);
        }
      };

      video.onerror = () => {
        URL.revokeObjectURL(video.src);
        resolve("No se pudo leer el video.");
      };

      video.src = URL.createObjectURL(file);
    });
  };

  const handleFileSelect = async (file: File) => {
    setValidationError(null);

    const validationResult = await validateFile(file);
    if (validationResult) {
      setValidationError(validationResult);
      return;
    }

    setSelectedFile(file);
    setPreviewUrl(URL.createObjectURL(file));
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      handleFileSelect(file);
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(false);

    const file = e.dataTransfer.files?.[0];
    if (file) {
      handleFileSelect(file);
    }
  };

  const handleUpload = async () => {
    if (!selectedFile) return;
    await uploadAndProcess(selectedFile);
  };

  const handleClear = () => {
    setSelectedFile(null);
    if (previewUrl) {
      URL.revokeObjectURL(previewUrl);
      setPreviewUrl(null);
    }
    setValidationError(null);
    if (inputRef.current) {
      inputRef.current.value = "";
    }
  };

  const getStatusMessage = (): string => {
    if (!job) return "";

    switch (job.status) {
      case "Pending":
      case "Queued":
        return "En cola de procesamiento...";
      case "Uploading":
        return "Subiendo video...";
      case "ExtractingFrames":
        return "Extrayendo 6 fotos del video...";
      case "RemovingBackgrounds":
        return `Procesando imágenes (${progress}%)...`;
      case "Completed":
        return "¡Vista 360° creada exitosamente!";
      case "Failed":
        return job.errorMessage || "Error en el procesamiento";
      default:
        return "Procesando...";
    }
  };

  // Si el usuario eligió saltar este paso
  if (skipStep && !view?.isReady) {
    return (
      <div className="space-y-6">
        <div>
          <h2 className="text-xl font-semibold text-gray-900 flex items-center gap-2">
            <RotateCcw className="h-5 w-5 text-primary-600" />
            Vista 360° (Opcional)
          </h2>
          <p className="text-gray-600 mt-1">
            Has elegido omitir este paso. Puedes agregar la vista 360° después desde tu dashboard.
          </p>
        </div>

        <Button
          variant="outline"
          onClick={() => setSkipStep(false)}
        >
          Agregar Vista 360°
        </Button>
      </div>
    );
  }

  // Si ya tiene vista 360° disponible
  if (view?.isReady && !isProcessing) {
    return (
      <div className="space-y-6">
        <div>
          <h2 className="text-xl font-semibold text-gray-900 flex items-center gap-2">
            <CheckCircle className="h-5 w-5 text-green-600" />
            Vista 360° Lista
          </h2>
          <p className="text-gray-600 mt-1">
            Tu vehículo tendrá una vista interactiva 360° cuando lo publiques.
          </p>
        </div>

        <Vehicle360Viewer vehicleId={vehicleId} height={300} />

        <div className="flex gap-3">
          <Button
            variant="outline"
            onClick={() => inputRef.current?.click()}
          >
            <RotateCcw className="h-4 w-4 mr-2" />
            Reemplazar Video
          </Button>
        </div>

        <input
          ref={inputRef}
          type="file"
          accept="video/*"
          className="hidden"
          onChange={handleInputChange}
        />
      </div>
    );
  }

  // Si está procesando
  if (isProcessing) {
    return (
      <div className="space-y-6">
        <div>
          <h2 className="text-xl font-semibold text-gray-900 flex items-center gap-2">
            <Loader2 className="h-5 w-5 animate-spin text-primary-600" />
            Procesando Video 360°
          </h2>
        </div>

        <div className="bg-blue-50 border border-blue-200 rounded-xl p-6">
          <Progress value={progress} className="h-2 mb-4" />

          <div className="flex items-center justify-between text-sm">
            <span className="text-blue-700">{getStatusMessage()}</span>
            <span className="text-blue-600 font-medium">{progress}%</span>
          </div>

          <p className="text-xs text-blue-600 mt-4">
            ⏱️ Esto puede tomar 2-5 minutos. Puedes continuar al siguiente paso mientras procesamos.
          </p>

          <Button
            variant="outline"
            size="sm"
            onClick={cancelProcessing}
            className="mt-4"
          >
            <X className="h-4 w-4 mr-2" />
            Cancelar
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold text-gray-900 flex items-center gap-2">
          <RotateCcw className="h-5 w-5 text-primary-600" />
          Vista 360° (Opcional)
          <span className="ml-2 px-2 py-0.5 bg-amber-100 text-amber-700 text-xs font-medium rounded-full flex items-center gap-1">
            <Sparkles className="h-3 w-3" />
            Diferenciador
          </span>
        </h2>
        <p className="text-gray-600 mt-1">
          Los vehículos con vista 360° reciben <strong>3x más consultas</strong> que los que solo tienen fotos.
        </p>
      </div>

      {/* Dropzone */}
      {!selectedFile && (
        <div
          className={cn(
            "relative border-2 border-dashed rounded-xl p-8 text-center transition-colors cursor-pointer",
            isDragOver
              ? "border-primary-500 bg-primary-50"
              : "border-gray-200 hover:border-gray-300",
            validationError && "border-red-300 bg-red-50"
          )}
          onDrop={handleDrop}
          onDragOver={(e) => { e.preventDefault(); setIsDragOver(true); }}
          onDragLeave={() => setIsDragOver(false)}
          onClick={() => inputRef.current?.click()}
        >
          <input
            ref={inputRef}
            type="file"
            accept="video/*"
            className="hidden"
            onChange={handleInputChange}
          />

          <Video className="h-12 w-12 mx-auto text-gray-400 mb-4" />

          <h3 className="font-semibold text-gray-900 mb-1">
            Subir Video 360°
          </h3>

          <p className="text-sm text-gray-500 mb-4">
            Arrastra un video aquí o haz clic para seleccionar
          </p>

          <p className="text-xs text-gray-400">
            MP4, MOV, AVI, WebM • Máx. 500MB • Máx. 60 segundos
          </p>

          {validationError && (
            <div className="mt-4 flex items-center justify-center gap-2 text-red-600">
              <AlertCircle className="h-4 w-4" />
              <span className="text-sm">{validationError}</span>
            </div>
          )}
        </div>
      )}

      {/* Preview del video seleccionado */}
      {selectedFile && previewUrl && !isProcessing && (
        <div className="space-y-4">
          <div className="relative rounded-xl overflow-hidden bg-black">
            <video
              src={previewUrl}
              controls
              className="w-full max-h-[250px] object-contain"
            />

            <button
              type="button"
              className="absolute top-2 right-2 bg-black/60 text-white p-1.5 rounded-full hover:bg-black/80"
              onClick={handleClear}
            >
              <X className="h-4 w-4" />
            </button>
          </div>

          <div className="flex items-center justify-between bg-gray-50 rounded-lg p-3">
            <div className="flex items-center gap-3">
              <Video className="h-5 w-5 text-gray-400" />
              <div>
                <p className="font-medium text-sm text-gray-900">
                  {selectedFile.name}
                </p>
                <p className="text-xs text-gray-500">
                  {formatFileSize(selectedFile.size)}
                </p>
              </div>
            </div>

            <Button size="sm" onClick={handleUpload}>
              <Upload className="h-4 w-4 mr-2" />
              Procesar Video
            </Button>
          </div>

          {/* Explicación de qué pasará */}
          <div className="bg-blue-50 border border-blue-100 rounded-lg p-4">
            <h4 className="font-medium text-blue-900 flex items-center gap-2">
              <Info className="h-4 w-4" />
              ¿Qué sucederá?
            </h4>
            <ul className="mt-2 text-sm text-blue-700 space-y-1">
              <li>✓ Extraeremos 6 fotos del video (cada 60°)</li>
              <li>✓ Removeremos el fondo de cada imagen automáticamente</li>
              <li>✓ Crearemos una vista 360° interactiva profesional</li>
              <li>⏱️ Tiempo estimado: 2-5 minutos</li>
            </ul>
          </div>
        </div>
      )}

      {/* Error */}
      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 flex items-start gap-3">
          <AlertCircle className="h-5 w-5 text-red-500 flex-shrink-0 mt-0.5" />
          <div>
            <p className="font-medium text-red-900">Error en el procesamiento</p>
            <p className="text-sm text-red-700">{error}</p>
            <Button
              variant="outline"
              size="sm"
              onClick={refetch}
              className="mt-2"
            >
              Reintentar
            </Button>
          </div>
        </div>
      )}

      {/* Tips para grabar */}
      <div className="bg-gray-50 rounded-lg p-4">
        <h4 className="font-medium text-gray-900 mb-2">
          💡 Tips para un buen video 360°
        </h4>
        <ul className="text-sm text-gray-600 space-y-1">
          <li>• Graba con el vehículo sobre una plataforma giratoria</li>
          <li>• Usa buena iluminación, preferiblemente exterior</li>
          <li>• Mantén la cámara estable a la altura del vehículo</li>
          <li>• Una vuelta completa y lenta (15-30 segundos)</li>
          <li>• Fondo limpio para mejor remoción automática</li>
        </ul>
      </div>

      {/* Skip option */}
      {!selectedFile && (
        <div className="text-center">
          <button
            type="button"
            onClick={() => setSkipStep(true)}
            className="text-sm text-gray-500 hover:text-gray-700 underline"
          >
            Omitir este paso y continuar sin vista 360°
          </button>
        </div>
      )}
    </div>
  );
}
```

---

## 🔧 PASO 8: PricingStep con IA (P1 - Feature Diferenciadora) ⭐

```typescript
// filepath: src/components/publish/steps/PricingStep.tsx
"use client";

import * as React from "react";
import { useFormContext } from "react-hook-form";
import { motion, AnimatePresence } from "framer-motion";
import {
  DollarSign,
  TrendingUp,
  TrendingDown,
  Sparkles,
  AlertCircle,
  CheckCircle,
  RefreshCw,
  Info,
  Loader2,
  BarChart3,
  Target,
} from "lucide-react";
import { FormField } from "@/components/ui/FormField";
import { Input } from "@/components/ui/Input";
import { Checkbox } from "@/components/ui/Checkbox";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { Skeleton } from "@/components/ui/Skeleton";
import { usePriceSuggestion } from "@/lib/hooks/useVehicleIntelligence";
import { formatPrice, cn } from "@/lib/utils";
import type { PublishFormData } from "../PublishForm";

export function PricingStep() {
  const { register, watch, setValue, formState: { errors } } = useFormContext<PublishFormData>();
  const [showAnalysis, setShowAnalysis] = React.useState(true);
  const [userAdjustedPrice, setUserAdjustedPrice] = React.useState(false);

  // Watch vehicle data for AI analysis
  const makeId = watch("makeId");
  const modelId = watch("modelId");
  const year = watch("year");
  const mileage = watch("mileage");
  const condition = watch("condition");
  const price = watch("price");

  // Get AI price suggestion
  const {
    data: analysis,
    isLoading,
    error,
    refetch,
  } = usePriceSuggestion({
    makeId,
    modelId,
    year,
    mileage,
    condition,
  }, {
    enabled: !!makeId && !!modelId && !!year,
  });

  // Auto-fill suggested price if user hasn't adjusted manually
  React.useEffect(() => {
    if (analysis?.suggestedPrice && !price && !userAdjustedPrice) {
      setValue("price", analysis.suggestedPrice);
    }
  }, [analysis, price, userAdjustedPrice, setValue]);

  const pricePosition = React.useMemo(() => {
    if (!analysis || !price) return null;

    const { marketMin, marketAvg, marketMax } = analysis.marketStats;
    if (price < marketMin) return "low";
    if (price > marketMax) return "high";
    if (Math.abs(price - marketAvg) / marketAvg < 0.05) return "average";
    return price < marketAvg ? "below" : "above";
  }, [analysis, price]);

  const handlePriceChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setUserAdjustedPrice(true);
    // El register maneja el setValue automáticamente
  };

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center gap-3">
          <Loader2 className="h-6 w-6 text-primary-600 animate-spin" />
          <div>
            <h2 className="text-xl font-semibold text-gray-900">
              Analizando mercado...
            </h2>
            <p className="text-sm text-gray-600 mt-1">
              Estamos calculando el precio óptimo para tu vehículo
            </p>
          </div>
        </div>

        <div className="space-y-4">
          <Skeleton className="h-32 w-full" />
          <Skeleton className="h-20 w-full" />
          <Skeleton className="h-20 w-full" />
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold text-gray-900">Precio</h2>
        <p className="text-gray-600 mt-1">
          Establece el precio de venta de tu vehículo
        </p>
      </div>

      {/* AI Price Suggestion Card */}
      {analysis && showAnalysis && (
        <motion.div
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          className="bg-gradient-to-br from-primary-50 to-blue-50 border border-primary-200 rounded-xl p-6"
        >
          <div className="flex items-start justify-between mb-4">
            <div className="flex items-center gap-2">
              <Sparkles className="h-5 w-5 text-primary-600" />
              <h3 className="font-semibold text-gray-900">
                Precio Sugerido con IA
              </h3>
            </div>
            <button
              type="button"
              onClick={() => setShowAnalysis(false)}
              className="text-gray-400 hover:text-gray-600"
            >
              ×
            </button>
          </div>

          {/* Suggested Price */}
          <div className="bg-white rounded-lg p-4 mb-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-gray-600">Precio recomendado</p>
                <p className="text-3xl font-bold text-primary-600">
                  {formatPrice(analysis.suggestedPrice)}
                </p>
              </div>
              <Button
                size="sm"
                variant="outline"
                onClick={() => {
                  setValue("price", analysis.suggestedPrice);
                  setUserAdjustedPrice(false);
                }}
              >
                Usar este precio
              </Button>
            </div>

            {/* Confidence score */}
            <div className="mt-3 flex items-center gap-2">
              <div className="flex-1 bg-gray-200 rounded-full h-2">
                <div
                  className="bg-primary-600 h-2 rounded-full transition-all"
                  style={{ width: `${analysis.confidence}%` }}
                />
              </div>
              <span className="text-xs text-gray-600">
                {analysis.confidence}% confianza
              </span>
            </div>
          </div>

          {/* Market Stats */}
          <div className="grid grid-cols-3 gap-3 mb-4">
            <div className="bg-white rounded-lg p-3">
              <p className="text-xs text-gray-600">Mínimo mercado</p>
              <p className="font-semibold text-gray-900">
                {formatPrice(analysis.marketStats.marketMin)}
              </p>
            </div>
            <div className="bg-white rounded-lg p-3">
              <p className="text-xs text-gray-600">Promedio</p>
              <p className="font-semibold text-gray-900">
                {formatPrice(analysis.marketStats.marketAvg)}
              </p>
            </div>
            <div className="bg-white rounded-lg p-3">
              <p className="text-xs text-gray-600">Máximo mercado</p>
              <p className="font-semibold text-gray-900">
                {formatPrice(analysis.marketStats.marketMax)}
              </p>
            </div>
          </div>

          {/* Insights */}
          <div className="space-y-2">
            <h4 className="text-sm font-medium text-gray-900 flex items-center gap-2">
              <Info className="h-4 w-4 text-primary-600" />
              ¿Cómo calculamos este precio?
            </h4>
            <ul className="space-y-1.5">
              {analysis.insights.map((insight, i) => (
                <li key={i} className="text-sm text-gray-700 flex items-start gap-2">
                  <CheckCircle className="h-4 w-4 text-green-600 flex-shrink-0 mt-0.5" />
                  <span>{insight}</span>
                </li>
              ))}
            </ul>
          </div>

          {/* Expected days to sell */}
          {analysis.daysToSellEstimate && (
            <div className="mt-4 bg-white rounded-lg p-3 flex items-center gap-3">
              <Target className="h-5 w-5 text-primary-600" />
              <div>
                <p className="text-sm font-medium text-gray-900">
                  Estimado de venta
                </p>
                <p className="text-xs text-gray-600">
                  Este vehículo podría venderse en{" "}
                  <span className="font-semibold">
                    {analysis.daysToSellEstimate} días
                  </span>{" "}
                  a este precio
                </p>
              </div>
            </div>
          )}

          {/* Retry button if AI fails */}
          {error && (
            <div className="mt-4">
              <Button
                variant="outline"
                size="sm"
                onClick={() => refetch()}
                className="w-full"
              >
                <RefreshCw className="h-4 w-4 mr-2" />
                Recalcular precio
              </Button>
            </div>
          )}
        </motion.div>
      )}

      {/* Manual Price Input */}
      <FormField
        label="Precio de venta"
        hint="Ingresa el precio en pesos dominicanos (RD$)"
        error={errors.price?.message}
      >
        <div className="relative">
          <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
            <DollarSign className="h-5 w-5 text-gray-400" />
          </div>
          <Input
            type="number"
            placeholder="1,850,000"
            className="pl-10"
            {...register("price", { valueAsNumber: true })}
            onChange={handlePriceChange}
          />
        </div>
      </FormField>

      {/* Price Position Indicator */}
      <AnimatePresence>
        {analysis && price && pricePosition && (
          <motion.div
            initial={{ opacity: 0, height: 0 }}
            animate={{ opacity: 1, height: "auto" }}
            exit={{ opacity: 0, height: 0 }}
            className={cn(
              "rounded-lg p-4 flex items-start gap-3",
              pricePosition === "low" && "bg-red-50 border border-red-200",
              pricePosition === "below" && "bg-yellow-50 border border-yellow-200",
              pricePosition === "average" && "bg-green-50 border border-green-200",
              pricePosition === "above" && "bg-blue-50 border border-blue-200",
              pricePosition === "high" && "bg-purple-50 border border-purple-200"
            )}
          >
            {pricePosition === "low" && (
              <>
                <TrendingDown className="h-5 w-5 text-red-600 flex-shrink-0" />
                <div>
                  <p className="font-medium text-red-900">
                    Precio por debajo del mercado
                  </p>
                  <p className="text-sm text-red-700 mt-1">
                    Tu precio está {Math.abs(((price - analysis.marketStats.marketMin) / analysis.marketStats.marketMin * 100)).toFixed(0)}%
                    por debajo del rango normal. Podrías venderlo más rápido, pero considera si estás perdiendo dinero.
                  </p>
                </div>
              </>
            )}
            {pricePosition === "below" && (
              <>
                <TrendingDown className="h-5 w-5 text-yellow-600 flex-shrink-0" />
                <div>
                  <p className="font-medium text-yellow-900">
                    Precio competitivo
                  </p>
                  <p className="text-sm text-yellow-700 mt-1">
                    Tu precio está ligeramente por debajo del promedio. ¡Buena estrategia para venta rápida!
                  </p>
                </div>
              </>
            )}
            {pricePosition === "average" && (
              <>
                <CheckCircle className="h-5 w-5 text-green-600 flex-shrink-0" />
                <div>
                  <p className="font-medium text-green-900">
                    Precio óptimo
                  </p>
                  <p className="text-sm text-green-700 mt-1">
                    Tu precio está en el promedio del mercado. Balance perfecto entre velocidad y ganancia.
                  </p>
                </div>
              </>
            )}
            {pricePosition === "above" && (
              <>
                <TrendingUp className="h-5 w-5 text-blue-600 flex-shrink-0" />
                <div>
                  <p className="font-medium text-blue-900">
                    Precio por encima del promedio
                  </p>
                  <p className="text-sm text-blue-700 mt-1">
                    Tu precio está ligeramente por encima del promedio. Puede tardar más en vender, pero maximiza ganancia.
                  </p>
                </div>
              </>
            )}
            {pricePosition === "high" && (
              <>
                <AlertCircle className="h-5 w-5 text-purple-600 flex-shrink-0" />
                <div>
                  <p className="font-medium text-purple-900">
                    Precio por encima del mercado
                  </p>
                  <p className="text-sm text-purple-700 mt-1">
                    Tu precio está {Math.abs(((price - analysis.marketStats.marketMax) / analysis.marketStats.marketMax * 100)).toFixed(0)}%
                    por encima del rango normal. Considera bajar el precio para atraer más compradores.
                  </p>
                </div>
              </>
            )}
          </motion.div>
        )}
      </AnimatePresence>

      {/* Options */}
      <div className="space-y-3">
        <label className="flex items-start gap-3">
          <Checkbox {...register("isNegotiable")} />
          <div>
            <p className="font-medium text-gray-900">Precio negociable</p>
            <p className="text-sm text-gray-600">
              Permite que los compradores hagan ofertas
            </p>
          </div>
        </label>

        <label className="flex items-start gap-3">
          <Checkbox {...register("acceptsTrade")} />
          <div>
            <p className="font-medium text-gray-900">Acepto permuta</p>
            <p className="text-sm text-gray-600">
              Estás dispuesto a recibir otro vehículo como parte de pago
            </p>
          </div>
        </label>
      </div>

      {/* Market comparison chart (if analysis available) */}
      {analysis && price && (
        <div className="bg-gray-50 rounded-lg p-4">
          <h4 className="font-medium text-gray-900 mb-3 flex items-center gap-2">
            <BarChart3 className="h-4 w-4" />
            Comparación con el mercado
          </h4>
          <div className="relative h-16">
            {/* Market range bar */}
            <div className="absolute inset-x-0 top-1/2 -translate-y-1/2 h-6 bg-gradient-to-r from-red-200 via-green-200 to-red-200 rounded-full" />

            {/* Markers */}
            <div className="absolute inset-x-0 top-1/2 -translate-y-1/2 flex items-center justify-between px-2">
              <div className="text-xs text-gray-600">
                {formatPrice(analysis.marketStats.marketMin)}
              </div>
              <div className="text-xs font-medium text-gray-900">
                {formatPrice(analysis.marketStats.marketAvg)}
              </div>
              <div className="text-xs text-gray-600">
                {formatPrice(analysis.marketStats.marketMax)}
              </div>
            </div>

            {/* Your price indicator */}
            <div
              className="absolute top-0 -translate-x-1/2 flex flex-col items-center"
              style={{
                left: `${Math.min(100, Math.max(0, ((price - analysis.marketStats.marketMin) / (analysis.marketStats.marketMax - analysis.marketStats.marketMin)) * 100))}%`,
              }}
            >
              <div className="bg-primary-600 text-white text-xs font-medium px-2 py-1 rounded">
                Tu precio
              </div>
              <div className="w-0.5 h-4 bg-primary-600" />
            </div>
          </div>
        </div>
      )}

      {/* Error fallback - If AI service not available */}
      {error && !analysis && (
        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4 flex items-start gap-3">
          <AlertCircle className="h-5 w-5 text-yellow-600 flex-shrink-0" />
          <div>
            <p className="font-medium text-yellow-900">
              Sugerencia de precio no disponible
            </p>
            <p className="text-sm text-yellow-700 mt-1">
              No pudimos calcular un precio sugerido en este momento.
              Puedes investigar precios similares en el marketplace o intentar de nuevo.
            </p>
            <Button
              variant="outline"
              size="sm"
              onClick={() => refetch()}
              className="mt-2"
            >
              <RefreshCw className="h-4 w-4 mr-2" />
              Reintentar
            </Button>
          </div>
        </div>
      )}

      {/* Tips */}
      <div className="bg-blue-50 rounded-lg p-4">
        <h4 className="font-medium text-blue-900">💡 Tips para fijar el precio</h4>
        <ul className="mt-2 text-sm text-blue-700 space-y-1">
          <li>• Investiga precios de vehículos similares en el marketplace</li>
          <li>• Considera el estado real del vehículo (mecánico y estético)</li>
          <li>• Un precio competitivo atrae más compradores</li>
          <li>• Deja margen para negociación si marcas "Precio negociable"</li>
          <li>• Precios justos venden más rápido y generan mejor reputación</li>
        </ul>
      </div>
    </div>
  );
}
```

**Hooks requeridos:**

```typescript
// filepath: src/lib/hooks/useVehicleIntelligence.ts
import { useQuery } from "@tanstack/react-query";
import { vehicleIntelligenceService } from "@/lib/services/vehicleIntelligenceService";

interface UsePriceSuggestionParams {
  makeId?: string;
  modelId?: string;
  year?: number;
  mileage?: number;
  condition?: "new" | "used";
}

export function usePriceSuggestion(
  params: UsePriceSuggestionParams,
  options?: { enabled?: boolean },
) {
  return useQuery({
    queryKey: ["vehicle-price-suggestion", params],
    queryFn: () => vehicleIntelligenceService.analyzePricing(params),
    enabled: options?.enabled ?? true,
    staleTime: 5 * 60 * 1000, // 5 minutos
    retry: 2,
  });
}
```

---

## 📚 REFERENCIAS

### Documentos Process Matrix

- [01-media-service.md](../../process-matrix/10-MEDIA-ARCHIVOS/01-media-service.md) - Upload presigned URLs
- [02-image-processing.md](../../process-matrix/10-MEDIA-ARCHIVOS/02-image-processing.md) - Image processing worker

### Documentos Frontend

- **[100-media-multimedia-completo.md](./05-media-multimedia.md)** - 🆕 Documentación completa de Media & Multimedia
- [03-componentes-base.md](../02-UX-DESIGN-SYSTEM/03-componentes-base.md) - Componentes base
- [03-form-components.md](../03-COMPONENTES/03-form-components.md) - Formularios

---

## ✅ VALIDACIÓN

```bash
pnpm dev
# Verificar en http://localhost:3000/publicar:
# - Requiere autenticación ✅
# - Steps navigation funciona ✅
# - Validación por paso ✅
# - Subida de imágenes ✅
# - **Precio sugerido con IA funciona** ✅ NUEVO
# - **Análisis de mercado en tiempo real** ✅ NUEVO
# - **Comparación visual con el mercado** ✅ NUEVO
# - **Vista 360° opcional** ✅ NUEVO (ver 100-media-multimedia-completo.md)
# - Preview final ✅
```

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/publish-vehicle.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsUser } from "../helpers/auth";

test.describe("Publicar Vehículo", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
    await page.goto("/publicar");
  });

  test("debe requerir autenticación", async ({ page }) => {
    await page.context().clearCookies();
    await page.goto("/publicar");

    await expect(page).toHaveURL(/\/login\?redirect=/);
  });

  test("debe mostrar stepper con todos los pasos", async ({ page }) => {
    await expect(page.getByTestId("publish-stepper")).toBeVisible();
    await expect(page.getByText("Información Básica")).toBeVisible();
    await expect(page.getByText("Fotos")).toBeVisible();
    await expect(page.getByText("Detalles")).toBeVisible();
    await expect(page.getByText("Precio")).toBeVisible();
    await expect(page.getByText("Revisión")).toBeVisible();
  });

  test("debe validar paso 1 - Información Básica", async ({ page }) => {
    // Intentar avanzar sin datos
    await page.getByRole("button", { name: /siguiente/i }).click();

    await expect(page.getByText(/marca es requerida/i)).toBeVisible();
    await expect(page.getByText(/modelo es requerido/i)).toBeVisible();
    await expect(page.getByText(/año es requerido/i)).toBeVisible();

    // Completar campos
    await page.getByRole("combobox", { name: /marca/i }).click();
    await page.getByRole("option", { name: "Toyota" }).click();

    await page.getByRole("combobox", { name: /modelo/i }).click();
    await page.getByRole("option", { name: "Camry" }).click();

    await page.getByRole("combobox", { name: /año/i }).click();
    await page.getByRole("option", { name: "2023" }).click();

    await page.getByRole("button", { name: /siguiente/i }).click();

    // Debe avanzar al paso 2
    await expect(page.getByText("Paso 2")).toBeVisible();
  });

  test("debe subir imágenes en paso 2", async ({ page }) => {
    // Completar paso 1 primero
    await page.getByRole("combobox", { name: /marca/i }).click();
    await page.getByRole("option", { name: "Toyota" }).click();
    await page.getByRole("combobox", { name: /modelo/i }).click();
    await page.getByRole("option", { name: "Camry" }).click();
    await page.getByRole("combobox", { name: /año/i }).click();
    await page.getByRole("option", { name: "2023" }).click();
    await page.getByRole("button", { name: /siguiente/i }).click();

    // Paso 2 - Fotos
    const dropzone = page.getByTestId("image-dropzone");
    await expect(dropzone).toBeVisible();

    // Simular upload de archivo
    const fileInput = page.locator('input[type="file"]');
    await fileInput.setInputFiles("./fixtures/car-photo.jpg");

    await expect(page.getByTestId("uploaded-image")).toBeVisible();
  });

  test("debe mostrar precio sugerido por IA", async ({ page }) => {
    // Navegar hasta paso de precio (4)
    // ... completar pasos anteriores

    await expect(page.getByTestId("ai-price-suggestion")).toBeVisible();
    await expect(page.getByText(/precio sugerido/i)).toBeVisible();
    await expect(page.getByTestId("market-comparison")).toBeVisible();
  });

  test("debe guardar como borrador", async ({ page }) => {
    // Completar algo de info
    await page.getByRole("combobox", { name: /marca/i }).click();
    await page.getByRole("option", { name: "Toyota" }).click();

    await page.getByRole("button", { name: /guardar borrador/i }).click();

    await expect(page.getByText(/borrador guardado/i)).toBeVisible();
  });

  test("debe mostrar preview antes de publicar", async ({ page }) => {
    // Completar todos los pasos y llegar a review
    // ... navigate to final step

    await expect(page.getByTestId("publication-preview")).toBeVisible();
    await expect(page.getByRole("button", { name: /publicar/i })).toBeVisible();
  });

  test("debe navegar entre pasos con stepper", async ({ page }) => {
    // Completar paso 1
    await page.getByRole("combobox", { name: /marca/i }).click();
    await page.getByRole("option", { name: "Toyota" }).click();
    await page.getByRole("combobox", { name: /modelo/i }).click();
    await page.getByRole("option", { name: "Camry" }).click();
    await page.getByRole("combobox", { name: /año/i }).click();
    await page.getByRole("option", { name: "2023" }).click();
    await page.getByRole("button", { name: /siguiente/i }).click();

    // Volver al paso 1 haciendo click en stepper
    await page.getByTestId("step-1").click();
    await expect(page.getByRole("combobox", { name: /marca/i })).toHaveValue(
      "Toyota",
    );
  });
});
```

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/04-PAGINAS/05-dashboard.md`
