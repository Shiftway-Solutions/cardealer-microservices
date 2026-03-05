---
title: "76 - Página de Venta de Vehículo (SellYourCarPage)"
priority: P1
estimated_time: "2 horas"
dependencies: []
apis: ["MediaService"]
status: partial
last_updated: "2026-01-30"
---

# 76 - Página de Venta de Vehículo (SellYourCarPage)

> **Módulo**: SellYourCarPage (Multi-step Wizard)  
> **Ubicación**: `frontend/web/src/pages/vehicles/SellYourCarPage.tsx`  
> **Última actualización**: Enero 2026

---

## 📐 Arquitectura General

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      SELL YOUR CAR - WIZARD                             │
│                   /sell (Multi-step Form)                               │
│                                                                         │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │ STEPPER PROGRESS BAR                                           │    │
│  │                                                                 │    │
│  │  ①───────②───────③───────④───────⑤                            │    │
│  │  Vehicle   Photos  Features Pricing  Review                     │    │
│  │  Info              Options  & Details                           │    │
│  │                                                                 │    │
│  │  ● = Current  ✓ = Complete  ○ = Pending                        │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                         │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │ STEP 1: VehicleInfoStep                                        │    │
│  │ ┌─────────────────────────────────────────────────────────┐   │    │
│  │ │ VIN Input + Decode Button                               │   │    │
│  │ │ [__________________] [🔍 Decode VIN]                    │   │    │
│  │ │                                                         │   │    │
│  │ │ Auto-filled from VIN:                                   │   │    │
│  │ │ • Make, Model, Year, Trim                               │   │    │
│  │ │ • Transmission, Fuel Type, Engine                       │   │    │
│  │ │ • Body Type, Drivetrain                                 │   │    │
│  │ │                                                         │   │    │
│  │ │ Manual fields:                                          │   │    │
│  │ │ • Mileage, Exterior Color, Interior Color               │   │    │
│  │ │ • Condition (New/Used/Certified)                        │   │    │
│  │ │ • Doors, Seats                                          │   │    │
│  │ └─────────────────────────────────────────────────────────┘   │    │
│  │                                                                │    │
│  │ VehiclePreviewCard  |  TrimSelectionCard (if multiple trims)  │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                         │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │ STEP 2: PhotosStep                                             │    │
│  │ ┌─────────────────────────────────────────────────────────┐   │    │
│  │ │ Drag & Drop Zone                                        │   │    │
│  │ │ [📷 Drop images here or click to upload]                │   │    │
│  │ │                                                         │   │    │
│  │ │ Requirements:                                           │   │    │
│  │ │ • Min 1 image, Max 10 images                            │   │    │
│  │ │ • Max 10MB per image (auto-compressed to 1MB)           │   │    │
│  │ │ • Recommended: 1920px max dimension                     │   │    │
│  │ │                                                         │   │    │
│  │ │ Image Grid (drag to reorder):                           │   │    │
│  │ │ [🖼️] [🖼️] [🖼️] [🖼️] [🖼️]                             │   │    │
│  │ │  ⭐   2     3     4     5                               │   │    │
│  │ │ (first = main)                                          │   │    │
│  │ └─────────────────────────────────────────────────────────┘   │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                         │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │ STEP 3: FeaturesStep                                           │    │
│  │ ┌─────────────────────────────────────────────────────────┐   │    │
│  │ │ Feature Categories (checkboxes):                        │   │    │
│  │ │                                                         │   │    │
│  │ │ 🛡️ Safety          │ 🎵 Entertainment                  │   │    │
│  │ │ ☑ ABS Brakes       │ ☑ Bluetooth                       │   │    │
│  │ │ ☑ Airbags          │ ☑ Apple CarPlay                   │   │    │
│  │ │ ☑ Backup Camera    │ ☐ Android Auto                    │   │    │
│  │ │ ☐ Blind Spot       │ ☐ Premium Sound                   │   │    │
│  │ │                                                         │   │    │
│  │ │ ❄️ Comfort         │ 🚗 Performance                    │   │    │
│  │ │ ☑ A/C              │ ☐ Sport Mode                      │   │    │
│  │ │ ☐ Heated Seats     │ ☐ Paddle Shifters                 │   │    │
│  │ │ ☐ Sunroof          │ ☐ Sport Suspension                │   │    │
│  │ │                                                         │   │    │
│  │ │ VIN auto-selects safety features if available           │   │    │
│  │ └─────────────────────────────────────────────────────────┘   │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                         │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │ STEP 4: PricingStep                                            │    │
│  │ ┌─────────────────────────────────────────────────────────┐   │    │
│  │ │ Price Input                                             │   │    │
│  │ │ RD$ [____________]                                      │   │    │
│  │ │                                                         │   │    │
│  │ │ 💡 Price Suggestion (VehicleIntelligenceService):       │   │    │
│  │ │ ┌─────────────────────────────────────────────────────┐ │   │    │
│  │ │ │ Suggested Range: RD$ 1,650,000 - RD$ 1,850,000     │ │   │    │
│  │ │ │ Market Avg: RD$ 1,750,000                          │ │   │    │
│  │ │ │ Your Price: ✅ Competitive                         │ │   │    │
│  │ │ └─────────────────────────────────────────────────────┘ │   │    │
│  │ │                                                         │   │    │
│  │ │ Description (min 50 chars):                             │   │    │
│  │ │ [_______________________________________________]       │   │    │
│  │ │                                                         │   │    │
│  │ │ Location: [Santo Domingo ▼]                             │   │    │
│  │ │                                                         │   │    │
│  │ │ Seller Info:                                            │   │    │
│  │ │ Name: [_______]  Phone: [_______]  Email: [_______]    │   │    │
│  │ └─────────────────────────────────────────────────────────┘   │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                         │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │ STEP 5: ReviewStep                                             │    │
│  │ ┌─────────────────────────────────────────────────────────┐   │    │
│  │ │ Review Summary                                          │   │    │
│  │ │                                                         │   │    │
│  │ │ 📷 [Main Image]  Toyota Camry 2023 XSE                 │   │    │
│  │ │                  RD$ 1,850,000                          │   │    │
│  │ │                  Santo Domingo                          │   │    │
│  │ │                                                         │   │    │
│  │ │ ✅ 10 Photos  ✅ 12 Features  ✅ Description OK         │   │    │
│  │ │                                                         │   │    │
│  │ │ [💾 Save Draft]  [📤 Publish Listing]                   │   │    │
│  │ └─────────────────────────────────────────────────────────┘   │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                         │
│  Draft Resume Modal (on page load if draft exists):                     │
│  "We found a saved draft. Continue where you left off?"                 │
│  [Start Fresh] [Continue Draft]                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 🔧 Tipos TypeScript

```typescript
// VehicleFormData - Estado completo del formulario
export interface VehicleFormData {
  // Step 1: Vehicle Info
  make: string;
  model: string;
  trim?: string; // LE, SE, XLE, Sport (from VIN decode)
  year: number;
  mileage: number;
  vin: string;
  transmission?: string; // Opcional si VIN no tiene datos
  fuelType?: string; // Opcional si VIN no tiene datos
  bodyType?: string; // Opcional - VIN no siempre tiene datos
  drivetrain?: string; // Opcional si VIN no tiene datos
  engine?: string; // Opcional si VIN no tiene datos
  horsepower?: string;
  doors?: number;
  seats?: number;
  exteriorColor: string;
  interiorColor: string;
  condition: string;

  // Step 3: Features (from FeaturesStep)
  features: string[];

  // VIN-decoded data (auto-filled)
  vinBasePrice?: number; // MSRP from VIN (for price suggestion)
  vinSafetyFeatures?: string[]; // Safety features from VIN (auto-select in FeaturesStep)

  // Step 2: Photos
  images: File[];

  // Step 4: Pricing
  price: number;
  description: string;
  location: string;
  sellerName: string;
  sellerPhone: string;
  sellerEmail: string;
}

// Step configuration
interface Step {
  id: number;
  name: string;
  description: string;
}

const steps: Step[] = [
  {
    id: 1,
    name: "Vehicle Info",
    description: "Basic details about your vehicle",
  },
  { id: 2, name: "Photos", description: "Upload images of your vehicle" },
  { id: 3, name: "Features & Options", description: "Select vehicle features" },
  {
    id: 4,
    name: "Pricing & Details",
    description: "Set price and contact info",
  },
  { id: 5, name: "Review", description: "Review and publish your listing" },
];
```

---

## 🧩 Componentes del Wizard

### SellYourCarPage (Parent)

```
frontend/web/src/pages/vehicles/SellYourCarPage.tsx (405 líneas)
│
├── State
│   ├── currentStep (1-5)
│   ├── showDraftModal (boolean)
│   ├── isSubmitting (boolean)
│   └── formData (Partial<VehicleFormData>)
│
├── Persistence
│   ├── localStorage.getItem('sell-vehicle-draft')
│   ├── localStorage.setItem('sell-vehicle-draft', JSON.stringify)
│   └── Auto-save on formData change
│
├── Methods
│   ├── updateFormData(data) - Merge partial data
│   ├── saveDraft() - Manual save
│   ├── clearDraft() - Clear localStorage
│   ├── nextStep() - Increment step + scroll top
│   ├── prevStep() - Decrement step + scroll top
│   └── handleSubmit() - Upload images + create vehicle
│
├── Submit Flow
│   │
│   ├── 1. Validate required fields
│   ├── 2. Upload images to MediaService
│   │   └── uploadVehicleImages(formData.images)
│   ├── 3. Create vehicle with image URLs
│   │   └── createVehicle(vehiclePayload)
│   ├── 4. Clear draft on success
│   └── 5. Redirect to /vehicles/:id
│
└── Render
    ├── Header (title, subtitle)
    ├── Stepper (5 steps with lines)
    ├── Step Content (dynamic by currentStep)
    ├── Progress info ("Step X of 5")
    └── Draft Resume Modal
```

### Step 1: VehicleInfoStep

```
frontend/web/src/components/organisms/sell/VehicleInfoStep.tsx (1136 líneas)
│
├── Validation Schema (Zod)
│   ├── make: required
│   ├── model: required
│   ├── year: 1900 - currentYear+1
│   ├── mileage: min 0
│   ├── vin: required, max 17 chars
│   ├── exteriorColor: required
│   ├── interiorColor: required
│   ├── condition: required
│   └── Optional: transmission, fuelType, bodyType, etc.
│
├── Features
│   ├── VIN decode (NHTSA API via vinDecoderService)
│   ├── Make/Model/Year cascading dropdowns
│   ├── Trim selection with TrimSelectionCard
│   ├── VehiclePreviewCard (live preview)
│   └── Fallback data if backend unavailable
│
├── VIN Decode Flow
│   ├── validateVINFormat(vin) - Check format
│   ├── decodeVIN(vin) - Call API
│   └── Auto-fill: make, model, year, trim, engine, etc.
│
├── Catalog Service
│   ├── getAllMakes()
│   ├── getModelsByMake(makeId)
│   ├── getAvailableYears()
│   └── getTrimsByModelAndYear(modelId, year)
│
└── Props
    ├── data: Partial<VehicleFormData>
    ├── onNext: (data) => void
    └── onBack: () => void
```

### Step 2: PhotosStep

```
frontend/web/src/components/organisms/sell/PhotosStep.tsx (323 líneas)
│
├── State
│   ├── images: File[]
│   ├── previews: string[]
│   ├── isDragging
│   ├── error
│   ├── isCompressing
│   └── compressionProgress
│
├── Validation
│   ├── Only image files (image/*)
│   ├── Max 10MB per image (before compression)
│   └── Max 10 images total
│
├── Image Compression
│   └── browser-image-compression library
│       ├── maxSizeMB: 1
│       ├── maxWidthOrHeight: 1920
│       └── useWebWorker: true
│
├── Features
│   ├── Drag & drop zone
│   ├── Click to upload
│   ├── Image preview grid
│   ├── Drag to reorder (first = main)
│   ├── Remove individual images
│   └── Progress bar during compression
│
└── Props
    ├── data: Partial<VehicleFormData>
    ├── onNext: (data) => void
    └── onBack: () => void
```

### Step 3: FeaturesStep

```
frontend/web/src/components/organisms/sell/FeaturesStep.tsx
│
├── Feature Categories
│   ├── Safety: ABS, Airbags, Backup Camera, Blind Spot, etc.
│   ├── Entertainment: Bluetooth, Apple CarPlay, Android Auto, etc.
│   ├── Comfort: A/C, Heated Seats, Sunroof, Leather, etc.
│   └── Performance: Sport Mode, Paddle Shifters, etc.
│
├── Features
│   ├── Checkbox grid by category
│   ├── Auto-select from VIN safety features
│   ├── Select All / Deselect All per category
│   └── Feature count display
│
└── Props
    ├── data: Partial<VehicleFormData>
    ├── onNext: (data) => void
    └── onBack: () => void
```

### Step 4: PricingStep

```
frontend/web/src/components/organisms/sell/PricingStep.tsx (398 líneas)
│
├── Validation Schema (Zod)
│   ├── price: required, min 1, max 10,000,000
│   ├── description: min 50, max 2000 chars
│   ├── location: required
│   ├── sellerName: required
│   ├── sellerPhone: min 10 chars
│   └── sellerEmail: valid email
│
├── Price Suggestion
│   ├── vehicleIntelligenceService.getPriceAnalysis()
│   ├── Request: make, model, year, mileage, bodyType, price, location
│   └── Response: suggestedMin, suggestedMax, marketAvg, competitiveness
│
├── Features
│   ├── Price input with formatting
│   ├── Price suggestion card (if auth)
│   ├── Character counter for description
│   ├── Location dropdown (provinces RD)
│   └── Seller contact info
│
└── Props
    ├── data: Partial<VehicleFormData>
    ├── onNext: (data) => void
    └── onBack: () => void
```

### Step 5: ReviewStep

```
frontend/web/src/components/organisms/sell/ReviewStep.tsx
│
├── Summary Sections
│   ├── Main image preview
│   ├── Vehicle info (make, model, year, etc.)
│   ├── Price display
│   ├── Features list
│   ├── Photos count
│   └── Seller info
│
├── Validation Checks
│   ├── ✅ All required fields complete
│   ├── ✅ At least 1 photo
│   ├── ✅ Description >= 50 chars
│   └── ⚠️ Warnings if missing optional data
│
├── Actions
│   ├── Save Draft button
│   ├── Publish Listing button
│   └── Edit (go back to specific step)
│
└── Props
    ├── data: VehicleFormData (complete)
    ├── onSubmit: () => Promise<void>
    ├── onBack: () => void
    ├── onSaveDraft: () => void
    └── isSubmitting: boolean
```

---

## 🌐 API Services

### vehicleService.ts

```typescript
// services/vehicleService.ts

interface CreateVehicleDto {
  make: string;
  model: string;
  year: number;
  price: number;
  mileage: number;
  vin: string;
  description: string;
  location: string;
  images: string[]; // URLs from MediaService
  features: string[];
  condition: string;
  exteriorColor: string;
  interiorColor: string;
  transmission?: string;
  fuelType?: string;
  bodyType?: string;
  sellerName: string;
  sellerPhone: string;
  sellerEmail: string;
}

// POST /api/vehicles
export const createVehicle = async (
  vehicle: CreateVehicleDto,
): Promise<Vehicle> => {
  const { data } = await api.post("/vehicles", vehicle);
  return data;
};
```

### mediaService.ts

```typescript
// services/mediaService.ts

interface UploadResult {
  url: string;
  fileName: string;
  size: number;
}

type ProgressCallback = (
  current: number,
  total: number,
  progress: number,
) => void;

// POST /api/media/upload (multiple)
export const uploadVehicleImages = async (
  files: File[],
  onProgress?: ProgressCallback,
): Promise<UploadResult[]> => {
  const results: UploadResult[] = [];

  for (let i = 0; i < files.length; i++) {
    const formData = new FormData();
    formData.append("file", files[i]);
    formData.append("category", "vehicle");

    const { data } = await api.post("/media/upload", formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });

    results.push(data);
    onProgress?.(i + 1, files.length, ((i + 1) / files.length) * 100);
  }

  return results;
};
```

### vinDecoderService.ts

```typescript
// services/vinDecoderService.ts

interface VinDecodeResult {
  make: string;
  model: string;
  year: number;
  trim?: string;
  engine?: string;
  transmission?: string;
  fuelType?: string;
  bodyType?: string;
  drivetrain?: string;
  baseMSRP?: number;
  safetyFeatures?: string[];
}

// Validate VIN format (17 chars, no I, O, Q)
export const validateVINFormat = (vin: string): boolean => {
  const vinRegex = /^[A-HJ-NPR-Z0-9]{17}$/i;
  return vinRegex.test(vin);
};

// GET /api/vin/:vin (or NHTSA API)
export const decodeVIN = async (vin: string): Promise<VinDecodeResult> => {
  const { data } = await api.get(`/vin/${vin}`);
  return data;
};
```

### vehicleCatalogService.ts

```typescript
// services/vehicleCatalogService.ts

export interface VehicleMake {
  id: string;
  name: string;
  logoUrl?: string;
}

export interface VehicleModel {
  id: string;
  name: string;
  makeId: string;
}

export interface VehicleTrim {
  id: string;
  name: string;
  modelId: string;
  baseMSRP?: number;
  features?: string[];
}

// GET /api/catalog/makes
export const getAllMakes = async (): Promise<VehicleMake[]> => {
  const { data } = await api.get("/catalog/makes");
  return data;
};

// GET /api/catalog/makes/:makeId/models
export const getModelsByMake = async (
  makeId: string,
): Promise<VehicleModel[]> => {
  const { data } = await api.get(`/catalog/makes/${makeId}/models`);
  return data;
};

// GET /api/catalog/years
export const getAvailableYears = async (): Promise<number[]> => {
  const { data } = await api.get("/catalog/years");
  return data;
};

// GET /api/catalog/models/:modelId/trims?year=:year
export const getTrimsByModelAndYear = async (
  modelId: string,
  year: number,
): Promise<VehicleTrim[]> => {
  const { data } = await api.get(`/catalog/models/${modelId}/trims`, {
    params: { year },
  });
  return data;
};
```

### vehicleIntelligenceService.ts

```typescript
// services/vehicleIntelligenceService.ts

export interface PriceAnalysisDto {
  suggestedMin: number;
  suggestedMax: number;
  marketAverage: number;
  competitiveness: "low" | "fair" | "competitive" | "high";
  similarListings: number;
  daysOnMarketAvg: number;
}

// POST /api/intelligence/price-analysis
export const getPriceAnalysis = async (request: {
  make: string;
  model: string;
  year: number;
  mileage: number;
  bodyType?: string;
  askingPrice: number;
  location: string;
}): Promise<PriceAnalysisDto> => {
  const { data } = await api.post("/intelligence/price-analysis", request);
  return data;
};
```

---

## 🗄️ Persistencia de Draft

```typescript
// localStorage key
const DRAFT_KEY = "sell-vehicle-draft";

// Load on mount
useEffect(() => {
  const saved = localStorage.getItem(DRAFT_KEY);
  if (saved) {
    const parsed = JSON.parse(saved);
    // Check if has meaningful data
    if (
      parsed.make ||
      parsed.model ||
      parsed.year ||
      parsed.vin ||
      parsed.price
    ) {
      setShowDraftModal(true);
    }
    setFormData(parsed);
  }
}, []);

// Auto-save on change
useEffect(() => {
  localStorage.setItem(DRAFT_KEY, JSON.stringify(formData));
}, [formData]);

// Clear on successful submit
localStorage.removeItem(DRAFT_KEY);
```

---

## 🛣️ Rutas

```typescript
// App.tsx
<Route path="/sell" element={
  <ProtectedRoute>
    <SellYourCarPage />
  </ProtectedRoute>
} />
```

---

## 📦 Dependencias

```json
{
  "react-hook-form": "^7.x",
  "@hookform/resolvers": "^3.x",
  "zod": "^3.x",
  "browser-image-compression": "^2.x",
  "react-icons": "^4.x"
}
```

---

## 🌍 Internacionalización

```json
// locales/es/sell.json
{
  "title": "Vender Tu Vehículo",
  "subtitle": "Publica tu vehículo en pocos pasos y alcanza miles de compradores potenciales",
  "steps": {
    "vehicleInfo": "Información del Vehículo",
    "photos": "Fotos",
    "features": "Características",
    "pricing": "Precio y Detalles",
    "review": "Revisar"
  },
  "vehicleInfo": {
    "vinPlaceholder": "Ingresa el VIN de 17 caracteres",
    "decodeVin": "Decodificar VIN",
    "vinSuccess": "VIN decodificado exitosamente",
    "vinError": "Error al decodificar VIN"
  },
  "photos": {
    "dropzone": "Arrastra imágenes aquí o haz clic para subir",
    "maxImages": "Máximo 10 imágenes",
    "compressing": "Comprimiendo imágenes..."
  },
  "pricing": {
    "suggestedPrice": "Precio Sugerido",
    "marketAverage": "Promedio del Mercado",
    "competitive": "Tu precio es competitivo"
  },
  "review": {
    "saveDraft": "Guardar Borrador",
    "publishListing": "Publicar Listado"
  },
  "draft": {
    "title": "¿Continuar tu borrador?",
    "message": "Encontramos un borrador guardado. ¿Deseas continuar donde lo dejaste?",
    "continue": "Continuar Borrador",
    "startFresh": "Empezar de Nuevo"
  }
}
```

---

## ✅ Checklist de Validación

### General

- [ ] Wizard navega correctamente entre pasos
- [ ] Stepper muestra estado correcto (current, complete, pending)
- [ ] Scroll to top en cambio de paso
- [ ] Draft se guarda automáticamente
- [ ] Modal de draft aparece si existe borrador
- [ ] Clear draft funciona
- [ ] Submit exitoso redirige a vehicle detail

### Step 1: Vehicle Info

- [ ] VIN decode funciona
- [ ] Make/Model/Year cascading dropdowns
- [ ] Trim selection si hay múltiples
- [ ] Preview card se actualiza en tiempo real
- [ ] Validación de campos requeridos
- [ ] Fallback data si backend unavailable

### Step 2: Photos

- [ ] Drag & drop funciona
- [ ] Click to upload funciona
- [ ] Max 10 images enforced
- [ ] Compression funciona
- [ ] Preview grid muestra imágenes
- [ ] Remove individual image
- [ ] Reorder images (first = main)

### Step 3: Features

- [ ] Feature categories display
- [ ] Checkboxes funcionan
- [ ] Auto-select from VIN
- [ ] Feature count displays

### Step 4: Pricing

- [ ] Price input con formato
- [ ] Price suggestion (if auth)
- [ ] Description character counter
- [ ] Location dropdown
- [ ] Seller info validation

### Step 5: Review

- [ ] Summary displays correctly
- [ ] Validation checks visible
- [ ] Save Draft funciona
- [ ] Publish creates vehicle
- [ ] Images upload to MediaService first

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/sell-your-car.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsUser } from "../helpers/auth";

test.describe("Vender tu Auto", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
  });

  test("debe mostrar landing de vender", async ({ page }) => {
    await page.goto("/vender");

    await expect(
      page.getByRole("heading", { name: /vende tu auto/i }),
    ).toBeVisible();
    await expect(page.getByRole("button", { name: /comenzar/i })).toBeVisible();
  });

  test("debe iniciar flujo de publicación", async ({ page }) => {
    await page.goto("/vender");

    await page.getByRole("button", { name: /comenzar/i }).click();
    await expect(page).toHaveURL("/publicar");
  });

  test("debe mostrar beneficios de vender", async ({ page }) => {
    await page.goto("/vender");

    await expect(page.getByTestId("selling-benefits")).toBeVisible();
  });

  test("debe mostrar planes de publicación", async ({ page }) => {
    await page.goto("/vender");

    await expect(page.getByTestId("listing-plans")).toBeVisible();
    await expect(page.getByText(/gratis|básico|destacado/i)).toBeVisible();
  });

  test("debe mostrar FAQ para vendedores", async ({ page }) => {
    await page.goto("/vender");

    await expect(page.getByTestId("seller-faq")).toBeVisible();
  });

  test("debe estimar valor del vehículo (si disponible)", async ({ page }) => {
    await page.goto("/vender");

    const estimator = page.getByTestId("value-estimator");
    if (await estimator.isVisible()) {
      await page.getByRole("combobox", { name: /marca/i }).click();
      await page.getByRole("option", { name: "Toyota" }).click();
      await expect(page.getByTestId("estimated-value")).toBeVisible();
    }
  });
});
```

---

## 📚 Documentación Relacionada

- [74-vehicle-detail-browse-pages.md](../01-PUBLICO/03-detalle-vehiculo.md) - Vehicle detail page
- [57-dealer-inventory-management.md](../05-DEALER/02-dealer-inventario.md) - Dealer add vehicle
- [75-vehicle-media-pages.md](../09-COMPONENTES-COMUNES/04-vehicle-media.md) - Media upload
