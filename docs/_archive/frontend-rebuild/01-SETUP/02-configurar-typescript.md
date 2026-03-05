# 🔧 Paso 2: Configurar TypeScript Estricto

> **Tiempo estimado:** 15 minutos
> **Prerrequisitos:** Paso 01-crear-proyecto completado

---

## 📋 OBJETIVO

Configurar TypeScript con reglas estrictas para:

- Detectar errores en tiempo de compilación
- Mejorar autocompletado del IDE
- Forzar tipos explícitos
- Prevenir bugs comunes

---

## 🔧 PASO 2.1: Actualizar tsconfig.json

### Código completo

Reemplazar todo el contenido de `tsconfig.json`:

```json
{
  "$schema": "https://json.schemastore.org/tsconfig",
  "compilerOptions": {
    /* Base Options */
    "target": "ES2022",
    "lib": ["DOM", "DOM.Iterable", "ES2022"],
    "module": "ESNext",
    "moduleResolution": "bundler",
    "esModuleInterop": true,
    "isolatedModules": true,
    "resolveJsonModule": true,
    "allowJs": true,
    "jsx": "preserve",
    "incremental": true,

    /* Strict Type-Checking */
    "strict": true,
    "noImplicitAny": true,
    "strictNullChecks": true,
    "strictFunctionTypes": true,
    "strictBindCallApply": true,
    "strictPropertyInitialization": true,
    "noImplicitThis": true,
    "useUnknownInCatchVariables": true,
    "alwaysStrict": true,

    /* Additional Checks */
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "exactOptionalPropertyTypes": true,
    "noImplicitReturns": true,
    "noFallthroughCasesInSwitch": true,
    "noUncheckedIndexedAccess": true,
    "noImplicitOverride": true,
    "noPropertyAccessFromIndexSignature": true,
    "forceConsistentCasingInFileNames": true,

    /* Next.js Requirements */
    "skipLibCheck": true,
    "noEmit": true,

    /* Path Aliases */
    "baseUrl": ".",
    "paths": {
      "@/*": ["./src/*"],
      "@/components/*": ["./src/components/*"],
      "@/lib/*": ["./src/lib/*"],
      "@/types/*": ["./src/types/*"],
      "@/styles/*": ["./src/styles/*"],
      "@/hooks/*": ["./src/lib/hooks/*"],
      "@/store/*": ["./src/lib/store/*"],
      "@/api/*": ["./src/lib/api/*"]
    },

    /* Plugins */
    "plugins": [{ "name": "next" }]
  },
  "include": ["next-env.d.ts", "**/*.ts", "**/*.tsx", ".next/types/**/*.ts"],
  "exclude": ["node_modules", ".next", "out"]
}
```

---

## 🔧 PASO 2.2: Crear tipos globales

### Código a crear

```typescript
// filepath: src/types/global.d.ts

/* eslint-disable @typescript-eslint/no-unused-vars */

// Extend Window interface for global variables
declare global {
  interface Window {
    gtag?: (...args: unknown[]) => void;
    fbq?: (...args: unknown[]) => void;
  }
}

// Make TypeScript happy with CSS modules
declare module "*.module.css" {
  const classes: { readonly [key: string]: string };
  export default classes;
}

declare module "*.module.scss" {
  const classes: { readonly [key: string]: string };
  export default classes;
}

// SVG imports
declare module "*.svg" {
  import { FC, SVGProps } from "react";
  const content: FC<SVGProps<SVGElement>>;
  export default content;
}

// Image imports
declare module "*.png" {
  const content: string;
  export default content;
}

declare module "*.jpg" {
  const content: string;
  export default content;
}

declare module "*.webp" {
  const content: string;
  export default content;
}

export {};
```

---

## 🔧 PASO 2.3: Crear tipos de entidades base

### Código a crear

```typescript
// filepath: src/types/entities/index.ts

/**
 * Tipos base para todas las entidades
 */

export interface BaseEntity {
  id: string;
  createdAt: string;
  updatedAt: string;
}

export interface PaginatedResponse<T> {
  data: T[];
  pagination: {
    page: number;
    pageSize: number;
    totalItems: number;
    totalPages: number;
    hasNextPage: boolean;
    hasPreviousPage: boolean;
  };
}

export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  errors?: Record<string, string[]>;
}

export interface ApiError {
  success: false;
  message: string;
  errors?: Record<string, string[]>;
  statusCode: number;
}
```

```typescript
// filepath: src/types/entities/vehicle.ts

import type { BaseEntity } from "./index";

export type VehicleCondition = "new" | "used" | "certified";
export type VehicleStatus =
  | "draft"
  | "active"
  | "pending"
  | "sold"
  | "expired"
  | "deleted";
export type FuelType =
  | "gasoline"
  | "diesel"
  | "electric"
  | "hybrid"
  | "plugin_hybrid";
export type TransmissionType = "automatic" | "manual" | "cvt" | "dct";
export type DrivetrainType = "fwd" | "rwd" | "awd" | "4wd";

export interface VehicleImage {
  id: string;
  url: string;
  thumbnailUrl: string;
  alt: string;
  isPrimary: boolean;
  sortOrder: number;
}

export interface Vehicle extends BaseEntity {
  slug: string;
  title: string;
  description: string;

  // Precio
  price: number;
  originalPrice?: number;
  priceNegotiable: boolean;

  // Identificación
  make: string;
  makeId: string;
  model: string;
  modelId: string;
  trim?: string;
  year: number;
  vin?: string;

  // Condición
  condition: VehicleCondition;
  mileage: number;
  mileageUnit: "km" | "mi";

  // Especificaciones
  fuelType: FuelType;
  transmission: TransmissionType;
  drivetrain: DrivetrainType;
  engineSize?: number;
  horsepower?: number;
  cylinders?: number;

  // Exterior
  exteriorColor: string;
  interiorColor: string;
  bodyType: string;
  doors: number;
  seats: number;

  // Features
  features: string[];
  safetyFeatures: string[];

  // Media
  images: VehicleImage[];
  videoUrl?: string;

  // Location
  city: string;
  province: string;

  // Status
  status: VehicleStatus;
  views: number;
  favorites: number;
  inquiries: number;

  // Seller
  sellerId: string;
  sellerType: "individual" | "dealer";
  dealerId?: string;

  // Dates
  publishedAt?: string;
  expiresAt?: string;
  soldAt?: string;
}

export interface VehicleSearchFilters {
  query?: string;
  makeId?: string;
  modelId?: string;
  yearMin?: number;
  yearMax?: number;
  priceMin?: number;
  priceMax?: number;
  mileageMax?: number;
  condition?: VehicleCondition;
  fuelType?: FuelType;
  transmission?: TransmissionType;
  bodyType?: string;
  province?: string;
  city?: string;
  sortBy?: "price_asc" | "price_desc" | "year_desc" | "mileage_asc" | "newest";
}

export interface VehicleSummary {
  id: string;
  slug: string;
  title: string;
  price: number;
  year: number;
  make: string;
  model: string;
  mileage: number;
  city: string;
  condition: VehicleCondition;
  primaryImage: string;
  sellerType: "individual" | "dealer";
  isVerified: boolean;
}
```

```typescript
// filepath: src/types/entities/user.ts

import type { BaseEntity } from "./index";

export type UserRole = "buyer" | "seller" | "dealer" | "admin";
export type AccountStatus = "active" | "pending" | "suspended" | "banned";

export interface User extends BaseEntity {
  email: string;
  phone?: string;
  firstName: string;
  lastName: string;
  fullName: string;
  avatarUrl?: string;

  role: UserRole;
  accountStatus: AccountStatus;

  isEmailVerified: boolean;
  isPhoneVerified: boolean;

  // For sellers
  dealerId?: string;

  // Preferences
  preferredLanguage: "es" | "en";
  notificationPreferences: {
    email: boolean;
    sms: boolean;
    push: boolean;
  };

  lastLoginAt?: string;
}

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
}

export interface AuthUser {
  user: User;
  tokens: AuthTokens;
}
```

```typescript
// filepath: src/types/entities/dealer.ts

import type { BaseEntity } from "./index";

export type DealerType =
  | "independent"
  | "chain"
  | "multiple_store"
  | "franchise";
export type DealerStatus =
  | "pending"
  | "under_review"
  | "active"
  | "suspended"
  | "rejected"
  | "inactive";
export type VerificationStatus =
  | "not_verified"
  | "documents_uploaded"
  | "under_review"
  | "verified"
  | "rejected";
export type DealerPlan = "none" | "starter" | "pro" | "enterprise";

export interface DealerLocation extends BaseEntity {
  dealerId: string;
  name: string;
  address: string;
  city: string;
  province: string;
  phone: string;
  email: string;
  isPrimary: boolean;
  latitude?: number;
  longitude?: number;
  businessHours: {
    [day: string]: { open: string; close: string } | null;
  };
}

export interface Dealer extends BaseEntity {
  userId: string;

  // Business Info
  businessName: string;
  legalName: string;
  rnc: string;
  dealerType: DealerType;

  // Contact
  email: string;
  phone: string;
  mobilePhone?: string;
  website?: string;

  // Social
  facebookUrl?: string;
  instagramUrl?: string;
  whatsappNumber?: string;

  // Status
  status: DealerStatus;
  verificationStatus: VerificationStatus;

  // Subscription
  currentPlan: DealerPlan;
  maxActiveListings: number;
  activeListingsCount: number;
  isSubscriptionActive: boolean;
  subscriptionExpiresAt?: string;
  isEarlyBird: boolean;

  // Verification
  isVerified: boolean;
  verifiedAt?: string;

  // Stats
  totalViews: number;
  totalInquiries: number;
  totalSales: number;
  averageRating: number;
  reviewCount: number;

  // Locations
  primaryLocation?: DealerLocation;
  locations: DealerLocation[];

  // Branding
  logoUrl?: string;
  bannerUrl?: string;
  description?: string;

  // Metadata
  establishedDate?: string;
  employeeCount?: number;
}

export interface DealerPlanInfo {
  id: DealerPlan;
  name: string;
  price: number;
  earlyBirdPrice: number;
  maxListings: number;
  features: string[];
  isPopular: boolean;
}
```

---

## 🔧 PASO 2.4: Crear tipos de API

### Código a crear

```typescript
// filepath: src/types/api/index.ts

/**
 * Tipos para respuestas de API
 */

export interface ApiConfig {
  baseURL: string;
  timeout: number;
  headers: Record<string, string>;
}

export interface RequestConfig {
  signal?: AbortSignal;
  headers?: Record<string, string>;
}

// HTTP Methods
export type HttpMethod = "GET" | "POST" | "PUT" | "PATCH" | "DELETE";

// Error types
export interface ValidationError {
  field: string;
  message: string;
  code: string;
}

export interface ApiErrorResponse {
  success: false;
  message: string;
  errors?: ValidationError[];
  statusCode: number;
  timestamp: string;
  path: string;
}

// Auth
export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phone?: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  password: string;
  confirmPassword: string;
}

// Vehicles
export interface CreateVehicleRequest {
  title: string;
  description: string;
  price: number;
  makeId: string;
  modelId: string;
  year: number;
  condition: "new" | "used" | "certified";
  mileage: number;
  fuelType: string;
  transmission: string;
  exteriorColor: string;
  interiorColor: string;
  bodyType: string;
  features: string[];
  city: string;
  province: string;
}

export interface UpdateVehicleRequest extends Partial<CreateVehicleRequest> {
  id: string;
}

// Search
export interface SearchRequest {
  query?: string;
  filters?: Record<string, unknown>;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortOrder?: "asc" | "desc";
}

// Dealers
export interface CreateDealerRequest {
  businessName: string;
  legalName: string;
  rnc: string;
  dealerType: string;
  email: string;
  phone: string;
  mobilePhone?: string;
  website?: string;
  address: string;
  city: string;
  province: string;
  description?: string;
  establishedDate?: string;
  employeeCount?: number;
}
```

---

## 🔧 PASO 2.5: Crear tipos para formularios

### Código a crear

```typescript
// filepath: src/types/forms/index.ts

import { z } from "zod";

/**
 * Schemas de validación con Zod
 * Cada schema genera automáticamente el tipo TypeScript
 */

// Login
export const loginSchema = z.object({
  email: z.string().email("Ingrese un email válido"),
  password: z.string().min(8, "La contraseña debe tener al menos 8 caracteres"),
});

export type LoginFormData = z.infer<typeof loginSchema>;

// Register
export const registerSchema = z
  .object({
    firstName: z.string().min(2, "El nombre debe tener al menos 2 caracteres"),
    lastName: z.string().min(2, "El apellido debe tener al menos 2 caracteres"),
    email: z.string().email("Ingrese un email válido"),
    phone: z
      .string()
      .regex(/^[0-9]{10}$/, "Ingrese un teléfono válido de 10 dígitos")
      .optional(),
    password: z
      .string()
      .min(8, "La contraseña debe tener al menos 8 caracteres")
      .regex(
        /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/,
        "La contraseña debe contener mayúsculas, minúsculas y números",
      ),
    confirmPassword: z.string(),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: "Las contraseñas no coinciden",
    path: ["confirmPassword"],
  });

export type RegisterFormData = z.infer<typeof registerSchema>;

// Vehicle Search
export const vehicleSearchSchema = z.object({
  query: z.string().optional(),
  makeId: z.string().optional(),
  modelId: z.string().optional(),
  yearMin: z.coerce.number().min(1990).max(2030).optional(),
  yearMax: z.coerce.number().min(1990).max(2030).optional(),
  priceMin: z.coerce.number().min(0).optional(),
  priceMax: z.coerce.number().min(0).optional(),
  mileageMax: z.coerce.number().min(0).optional(),
  condition: z.enum(["new", "used", "certified"]).optional(),
  fuelType: z.string().optional(),
  transmission: z.string().optional(),
  bodyType: z.string().optional(),
  province: z.string().optional(),
  city: z.string().optional(),
});

export type VehicleSearchFormData = z.infer<typeof vehicleSearchSchema>;

// Create Vehicle
export const createVehicleSchema = z.object({
  title: z.string().min(10, "El título debe tener al menos 10 caracteres"),
  description: z
    .string()
    .min(50, "La descripción debe tener al menos 50 caracteres"),
  price: z.coerce.number().min(1, "Ingrese un precio válido"),
  makeId: z.string().min(1, "Seleccione una marca"),
  modelId: z.string().min(1, "Seleccione un modelo"),
  year: z.coerce.number().min(1990).max(2030, "Ingrese un año válido"),
  condition: z.enum(["new", "used", "certified"], {
    errorMap: () => ({ message: "Seleccione la condición" }),
  }),
  mileage: z.coerce.number().min(0, "Ingrese el kilometraje"),
  fuelType: z.string().min(1, "Seleccione el tipo de combustible"),
  transmission: z.string().min(1, "Seleccione la transmisión"),
  exteriorColor: z.string().min(1, "Seleccione el color exterior"),
  interiorColor: z.string().min(1, "Seleccione el color interior"),
  bodyType: z.string().min(1, "Seleccione el tipo de carrocería"),
  features: z.array(z.string()).default([]),
  city: z.string().min(1, "Seleccione la ciudad"),
  province: z.string().min(1, "Seleccione la provincia"),
});

export type CreateVehicleFormData = z.infer<typeof createVehicleSchema>;

// Dealer Registration
export const dealerRegistrationSchema = z.object({
  businessName: z
    .string()
    .min(3, "El nombre del negocio debe tener al menos 3 caracteres"),
  legalName: z
    .string()
    .min(3, "El nombre legal debe tener al menos 3 caracteres"),
  rnc: z.string().regex(/^[0-9]{9,11}$/, "El RNC debe tener 9-11 dígitos"),
  dealerType: z.enum(["independent", "chain", "multiple_store", "franchise"], {
    errorMap: () => ({ message: "Seleccione el tipo de dealer" }),
  }),
  email: z.string().email("Ingrese un email válido"),
  phone: z
    .string()
    .regex(/^[0-9]{10}$/, "Ingrese un teléfono válido de 10 dígitos"),
  mobilePhone: z
    .string()
    .regex(/^[0-9]{10}$/, "Ingrese un teléfono válido")
    .optional()
    .or(z.literal("")),
  website: z
    .string()
    .url("Ingrese una URL válida")
    .optional()
    .or(z.literal("")),
  address: z.string().min(10, "La dirección debe tener al menos 10 caracteres"),
  city: z.string().min(2, "Seleccione la ciudad"),
  province: z.string().min(2, "Seleccione la provincia"),
  description: z
    .string()
    .max(500, "La descripción no puede exceder 500 caracteres")
    .optional(),
  establishedDate: z.string().optional(),
  employeeCount: z.coerce.number().min(1).max(10000).optional(),
});

export type DealerRegistrationFormData = z.infer<
  typeof dealerRegistrationSchema
>;

// Contact Form
export const contactFormSchema = z.object({
  name: z.string().min(2, "El nombre debe tener al menos 2 caracteres"),
  email: z.string().email("Ingrese un email válido"),
  phone: z
    .string()
    .regex(/^[0-9]{10}$/, "Ingrese un teléfono válido")
    .optional()
    .or(z.literal("")),
  message: z
    .string()
    .min(10, "El mensaje debe tener al menos 10 caracteres")
    .max(1000),
});

export type ContactFormData = z.infer<typeof contactFormSchema>;
```

---

## 🔧 PASO 2.6: Re-exportar todos los tipos

### Código a crear

```typescript
// filepath: src/types/index.ts

// Re-export all types for easy imports
export * from "./entities/index";
export * from "./entities/vehicle";
export * from "./entities/user";
export * from "./entities/dealer";
export * from "./api/index";
export * from "./forms/index";

// Common utility types
export type Nullable<T> = T | null;
export type Optional<T> = T | undefined;
export type Maybe<T> = T | null | undefined;

// ID types for type safety
export type UserId = string & { readonly __brand: "UserId" };
export type VehicleId = string & { readonly __brand: "VehicleId" };
export type DealerId = string & { readonly __brand: "DealerId" };

// Status unions
export type LoadingState = "idle" | "loading" | "success" | "error";

export interface AsyncState<T> {
  data: T | null;
  status: LoadingState;
  error: string | null;
}
```

---

## ✅ VALIDACIÓN

### Comando de verificación

```bash
# Verificar que TypeScript compila sin errores
pnpm tsc --noEmit

# Output esperado: (sin output significa éxito)

# Si hay errores, verás algo como:
# src/types/entities/vehicle.ts:15:3 - error TS2304: Cannot find name 'xxx'
```

### Test de imports

Crear archivo temporal para probar:

```typescript
// filepath: src/test-types.ts (borrar después)
import type {
  Vehicle,
  VehicleSummary,
  User,
  Dealer,
  DealerPlanInfo,
  LoginFormData,
  RegisterFormData,
  PaginatedResponse,
  ApiResponse,
} from "@/types";

// Si este archivo compila, los tipos están correctos
const testVehicle: Vehicle = {} as Vehicle;
const testUser: User = {} as User;
console.log(testVehicle, testUser);
```

```bash
# Verificar
pnpm tsc --noEmit

# Borrar archivo de prueba
rm src/test-types.ts
```

---

## 📊 RESUMEN

| Archivo                         | Propósito                           |
| ------------------------------- | ----------------------------------- |
| `tsconfig.json`                 | Config estricta de TypeScript       |
| `src/types/global.d.ts`         | Tipos globales y módulos            |
| `src/types/entities/index.ts`   | Tipos base (BaseEntity, Pagination) |
| `src/types/entities/vehicle.ts` | Tipos de vehículos                  |
| `src/types/entities/user.ts`    | Tipos de usuarios                   |
| `src/types/entities/dealer.ts`  | Tipos de dealers                    |
| `src/types/api/index.ts`        | Tipos de requests/responses         |
| `src/types/forms/index.ts`      | Schemas Zod + tipos de formularios  |
| `src/types/index.ts`            | Re-exports centralizados            |

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/01-SETUP/03-configurar-eslint.md`
