# 🏷️ Tipos TypeScript Globales - OKLA Frontend

> **Tiempo estimado:** 15 minutos (referencia)
> **Prerrequisitos:** TypeScript configurado
> **Última actualización:** Enero 31, 2026

---

## 📋 OBJETIVO

Documento de referencia con TODOS los tipos TypeScript centralizados para:

- Consistencia en todo el proyecto
- Autocompletado en IDE
- Type-safety end-to-end
- Documentación como código

---

## 📁 ESTRUCTURA DE ARCHIVOS DE TIPOS

```
src/types/
├── index.ts              # Re-export de todos los tipos
├── api.ts                # Tipos de respuestas API
├── auth.ts               # Usuario, sesión, tokens
├── vehicles.ts           # Vehículos y catálogo
├── dealers.ts            # Dealers y ubicaciones
├── billing.ts            # Pagos y suscripciones
├── notifications.ts      # Notificaciones
├── messages.ts           # Mensajes y conversaciones
├── reviews.ts            # Reviews y ratings
├── alerts.ts             # Alertas y búsquedas guardadas
├── admin.ts              # Tipos de administración
└── ui.ts                 # Tipos de UI (modals, toasts, etc.)
```

---

## 🔧 TIPOS BASE DE API

```typescript
// filepath: src/types/api.ts

// ============================================================================
// RESPUESTAS GENÉRICAS
// ============================================================================

/** Respuesta paginada genérica */
export interface PaginatedResponse<T> {
  items: T[];
  totalItems: number;
  totalPages: number;
  currentPage: number;
  pageSize: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

/** Respuesta de API exitosa */
export interface ApiResponse<T> {
  success: true;
  data: T;
  message?: string;
}

/** Error de API */
export interface ApiError {
  success: false;
  message: string;
  statusCode: number;
  errors?: Record<string, string[]>;
  errorCode?: string;
  traceId?: string;
}

/** Respuesta de API (unión) */
export type ApiResult<T> = ApiResponse<T> | ApiError;

/** Opciones de paginación para requests */
export interface PaginationParams {
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortOrder?: "asc" | "desc";
}

/** Filtros de fecha */
export interface DateRangeFilter {
  from?: string; // ISO date
  to?: string; // ISO date
}
```

---

## 👤 TIPOS DE AUTENTICACIÓN

```typescript
// filepath: src/types/auth.ts

// ============================================================================
// USUARIOS
// ============================================================================

/** Roles de usuario */
export type UserRole = "buyer" | "seller" | "dealer" | "admin";

/** Estado de verificación */
export type VerificationStatus =
  | "pending"
  | "verified"
  | "rejected"
  | "expired";

/** Usuario base */
export interface User {
  id: string;
  email: string;
  fullName: string;
  firstName: string;
  lastName: string;
  phone?: string;
  avatar?: string;
  role: UserRole;
  isEmailVerified: boolean;
  isPhoneVerified: boolean;
  isKycVerified: boolean;
  kycStatus: VerificationStatus;
  createdAt: string;
  updatedAt: string;
  lastLoginAt?: string;
}

/** Usuario con datos de dealer (si aplica) */
export interface UserWithDealer extends User {
  dealerId?: string;
  dealer?: DealerSummary;
}

/** Perfil público de usuario */
export interface UserProfile {
  id: string;
  fullName: string;
  avatar?: string;
  memberSince: string;
  isVerified: boolean;
  rating?: number;
  totalReviews?: number;
  totalListings?: number;
  responseTime?: string; // "< 1 hora", "1-2 horas", etc.
}

// ============================================================================
// SESIÓN Y TOKENS
// ============================================================================

/** Sesión de NextAuth extendida */
export interface Session {
  user: User;
  accessToken: string;
  refreshToken: string;
  accessTokenExpires: number;
  error?: "RefreshAccessTokenError";
}

/** Respuesta de login */
export interface LoginResponse {
  user: User;
  accessToken: string;
  refreshToken: string;
  expiresAt: number;
}

/** Datos de registro */
export interface RegisterData {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phone?: string;
  acceptTerms: boolean;
  acceptMarketing?: boolean;
}

/** Datos de login */
export interface LoginData {
  email: string;
  password: string;
  rememberMe?: boolean;
}
```

---

## 🚗 TIPOS DE VEHÍCULOS

```typescript
// filepath: src/types/vehicles.ts

// ============================================================================
// ENUMS Y CONSTANTES
// ============================================================================

/** Condición del vehículo */
export type VehicleCondition = "new" | "used" | "certified";

/** Estado de la publicación */
export type ListingStatus =
  | "draft"
  | "pending"
  | "active"
  | "paused"
  | "sold"
  | "expired"
  | "rejected";

/** Tipo de combustible */
export type FuelType =
  | "gasoline"
  | "diesel"
  | "electric"
  | "hybrid"
  | "plugin_hybrid"
  | "gas";

/** Tipo de transmisión */
export type Transmission = "automatic" | "manual" | "cvt" | "semi_automatic";

/** Tipo de tracción */
export type DriveType = "fwd" | "rwd" | "awd" | "4wd";

/** Tipo de carrocería */
export type BodyType =
  | "sedan"
  | "suv"
  | "pickup"
  | "hatchback"
  | "coupe"
  | "convertible"
  | "van"
  | "wagon"
  | "truck";

/** Tipo de vendedor */
export type SellerType = "individual" | "dealer";

// ============================================================================
// CATÁLOGO
// ============================================================================

/** Marca de vehículo */
export interface Make {
  id: string;
  name: string;
  slug: string;
  logoUrl?: string;
  vehicleCount: number;
  isPopular: boolean;
}

/** Modelo de vehículo */
export interface Model {
  id: string;
  makeId: string;
  name: string;
  slug: string;
  vehicleCount: number;
  years: number[]; // Años disponibles
}

/** Variante/Trim de modelo */
export interface Trim {
  id: string;
  modelId: string;
  name: string;
  year: number;
}

/** Color */
export interface Color {
  id: string;
  name: string;
  hexCode: string;
  type: "exterior" | "interior";
}

/** Feature/Característica */
export interface Feature {
  id: string;
  name: string;
  category: string; // 'safety', 'comfort', 'technology', 'exterior'
  icon?: string;
}

// ============================================================================
// VEHÍCULO
// ============================================================================

/** Vehículo resumido (para listas) */
export interface VehicleSummary {
  id: string;
  slug: string;
  title: string;
  year: number;
  make: string;
  makeSlug: string;
  model: string;
  modelSlug: string;
  trim?: string;
  price: number;
  originalPrice?: number; // Para mostrar descuento
  mileage: number;
  condition: VehicleCondition;
  fuelType: FuelType;
  transmission: Transmission;
  primaryImage: string;
  imagesCount: number;
  has360View: boolean;
  city: string;
  province: string;
  sellerType: SellerType;
  sellerId: string;
  sellerName: string;
  sellerAvatar?: string;
  isVerified: boolean;
  isFeatured: boolean;
  isBoosted: boolean;
  viewCount: number;
  favoriteCount: number;
  status: ListingStatus;
  createdAt: string;
  updatedAt: string;
}

/** Vehículo completo (para detalle) */
export interface Vehicle extends VehicleSummary {
  description: string;
  vin?: string;
  licensePlate?: string;

  // Especificaciones
  engine?: string;
  horsepower?: number;
  cylinders?: number;
  displacement?: number; // en litros
  driveType?: DriveType;
  bodyType: BodyType;
  doors: number;
  seats: number;
  exteriorColor: string;
  interiorColor?: string;

  // Galería
  images: VehicleImage[];
  view360Url?: string;
  videoUrl?: string;

  // Features
  features: string[];
  featureCategories: FeatureCategory[];

  // Ubicación
  address?: string;
  coordinates?: {
    lat: number;
    lng: number;
  };

  // Vendedor
  seller: SellerInfo;
  dealerId?: string;
  dealer?: DealerSummary;

  // Historial
  previousOwners?: number;
  serviceHistory?: boolean;
  accidentFree?: boolean;

  // Financiamiento
  acceptsFinancing: boolean;
  acceptsTradeIn: boolean;
  monthlyPaymentEstimate?: number;

  // SEO
  metaTitle?: string;
  metaDescription?: string;
}

/** Imagen de vehículo */
export interface VehicleImage {
  id: string;
  url: string;
  thumbnailUrl: string;
  alt: string;
  order: number;
  isPrimary: boolean;
}

/** Categoría de features */
export interface FeatureCategory {
  name: string;
  features: string[];
}

/** Información del vendedor */
export interface SellerInfo {
  id: string;
  name: string;
  type: SellerType;
  avatar?: string;
  phone?: string;
  whatsapp?: string;
  email?: string;
  isVerified: boolean;
  rating?: number;
  totalReviews?: number;
  totalListings: number;
  memberSince: string;
  responseTime?: string;
  responseRate?: number;
}

// ============================================================================
// FILTROS DE BÚSQUEDA
// ============================================================================

/** Filtros de búsqueda de vehículos */
export interface VehicleFilters {
  query?: string;
  makeId?: string;
  makeSlugs?: string[];
  modelId?: string;
  modelSlugs?: string[];
  yearFrom?: number;
  yearTo?: number;
  priceFrom?: number;
  priceTo?: number;
  mileageFrom?: number;
  mileageTo?: number;
  condition?: VehicleCondition[];
  fuelType?: FuelType[];
  transmission?: Transmission[];
  driveType?: DriveType[];
  bodyType?: BodyType[];
  exteriorColor?: string[];
  sellerType?: SellerType[];
  province?: string[];
  city?: string[];
  features?: string[];
  hasPhotos?: boolean;
  has360View?: boolean;
  hasVideo?: boolean;
  isVerified?: boolean;
  isFeatured?: boolean;
  acceptsFinancing?: boolean;
  acceptsTradeIn?: boolean;
}

/** Opciones de ordenamiento */
export type VehicleSortOption =
  | "relevance"
  | "price_asc"
  | "price_desc"
  | "year_desc"
  | "year_asc"
  | "mileage_asc"
  | "mileage_desc"
  | "newest"
  | "oldest"
  | "views";

// ============================================================================
// COMPARACIÓN
// ============================================================================

/** Vehículos en comparación */
export interface VehicleComparison {
  id: string;
  userId: string;
  vehicles: VehicleSummary[];
  createdAt: string;
  updatedAt: string;
  shareUrl?: string;
}

// ============================================================================
// FAVORITOS
// ============================================================================

/** Vehículo favorito */
export interface FavoriteVehicle {
  id: string;
  vehicleId: string;
  vehicle: VehicleSummary;
  userId: string;
  notes?: string;
  priceWhenSaved: number;
  currentPrice: number;
  priceChange: number; // diferencia
  notifyOnPriceChange: boolean;
  createdAt: string;
}
```

---

## 🏪 TIPOS DE DEALERS

```typescript
// filepath: src/types/dealers.ts

// ============================================================================
// ENUMS
// ============================================================================

/** Tipo de dealer */
export type DealerType =
  | "independent"
  | "chain"
  | "multiple_store"
  | "franchise";

/** Estado del dealer */
export type DealerStatus =
  | "pending"
  | "under_review"
  | "active"
  | "suspended"
  | "rejected"
  | "inactive";

/** Plan de suscripción */
export type DealerPlan = "starter" | "pro" | "enterprise";

// ============================================================================
// DEALER
// ============================================================================

/** Dealer resumido */
export interface DealerSummary {
  id: string;
  slug: string;
  businessName: string;
  logoUrl?: string;
  coverImageUrl?: string;
  city: string;
  province: string;
  isVerified: boolean;
  isPremium: boolean;
  rating?: number;
  totalReviews: number;
  totalListings: number;
  memberSince: string;
}

/** Dealer completo */
export interface Dealer extends DealerSummary {
  legalName: string;
  rnc: string;
  type: DealerType;
  status: DealerStatus;
  plan: DealerPlan;

  // Contacto
  email: string;
  phone: string;
  whatsapp?: string;
  website?: string;

  // Ubicación principal
  address: string;
  coordinates?: {
    lat: number;
    lng: number;
  };

  // Horarios
  businessHours: BusinessHours[];

  // Descripción
  description?: string;
  specialties?: string[]; // "SUVs", "Vehículos de lujo", etc.
  brands?: string[]; // Marcas que vende

  // Verificación
  verificationStatus: VerificationStatus;
  documents: DealerDocument[];

  // Suscripción
  subscription: DealerSubscription;

  // Estadísticas
  stats: DealerStats;

  // Ubicaciones adicionales
  locations: DealerLocation[];

  // Staff
  employeeCount: number;

  // Fechas
  establishedDate?: string;
  createdAt: string;
  updatedAt: string;
}

/** Horario de atención */
export interface BusinessHours {
  day:
    | "monday"
    | "tuesday"
    | "wednesday"
    | "thursday"
    | "friday"
    | "saturday"
    | "sunday";
  isOpen: boolean;
  openTime?: string; // "09:00"
  closeTime?: string; // "18:00"
}

/** Documento del dealer */
export interface DealerDocument {
  id: string;
  type: "rnc" | "business_license" | "id_card" | "proof_of_address" | "other";
  name: string;
  url: string;
  status: "pending" | "approved" | "rejected";
  rejectionReason?: string;
  uploadedAt: string;
  reviewedAt?: string;
}

/** Suscripción del dealer */
export interface DealerSubscription {
  plan: DealerPlan;
  status: "active" | "past_due" | "canceled" | "trialing";
  currentPeriodStart: string;
  currentPeriodEnd: string;
  maxActiveListings: number;
  usedListings: number;
  features: string[];
  price: number;
  currency: "DOP" | "USD";
  billingInterval: "monthly" | "yearly";
  cancelAtPeriodEnd: boolean;
}

/** Estadísticas del dealer */
export interface DealerStats {
  totalViews: number;
  totalInquiries: number;
  totalSales: number;
  avgResponseTime: number; // en minutos
  responseRate: number; // porcentaje
  conversionRate: number;
  inventoryValue: number;
  avgDaysOnMarket: number;
}

/** Ubicación/Sucursal */
export interface DealerLocation {
  id: string;
  name: string;
  type: "headquarters" | "branch" | "showroom" | "service_center";
  address: string;
  city: string;
  province: string;
  phone?: string;
  coordinates?: {
    lat: number;
    lng: number;
  };
  businessHours: BusinessHours[];
  isPrimary: boolean;
}
```

---

## 💳 TIPOS DE PAGOS

```typescript
// filepath: src/types/billing.ts

// ============================================================================
// PAGOS
// ============================================================================

/** Método de pago */
export type PaymentMethod = "card" | "bank_transfer" | "cash";

/** Proveedor de pago */
export type PaymentProvider = "stripe" | "azul" | "cardnet" | "manual";

/** Estado del pago */
export type PaymentStatus =
  | "pending"
  | "processing"
  | "completed"
  | "failed"
  | "refunded"
  | "canceled";

/** Tipo de producto/servicio */
export type ProductType =
  | "listing_fee" // Publicación individual
  | "boost" // Promoción
  | "featured" // Destacado
  | "subscription" // Suscripción dealer
  | "upgrade"; // Upgrade de plan

/** Pago */
export interface Payment {
  id: string;
  userId: string;
  amount: number;
  currency: "DOP" | "USD";
  status: PaymentStatus;
  provider: PaymentProvider;
  productType: ProductType;
  productId?: string; // ID del vehículo, suscripción, etc.
  description: string;
  metadata?: Record<string, unknown>;
  createdAt: string;
  completedAt?: string;
  failureReason?: string;
}

/** Tarjeta guardada */
export interface SavedCard {
  id: string;
  last4: string;
  brand: "visa" | "mastercard" | "amex" | "discover";
  expiryMonth: number;
  expiryYear: number;
  isDefault: boolean;
  cardholderName: string;
}

/** Factura */
export interface Invoice {
  id: string;
  number: string;
  userId: string;
  dealerId?: string;
  amount: number;
  tax: number;
  total: number;
  currency: "DOP" | "USD";
  status: "draft" | "sent" | "paid" | "void";
  dueDate: string;
  paidAt?: string;
  pdfUrl?: string;
  items: InvoiceItem[];
  createdAt: string;
}

/** Item de factura */
export interface InvoiceItem {
  description: string;
  quantity: number;
  unitPrice: number;
  amount: number;
}

// ============================================================================
// PRECIOS DE PRODUCTOS
// ============================================================================

/** Precios de planes de dealer */
export const DEALER_PLANS = {
  starter: {
    name: "Starter",
    price: 49,
    priceYearly: 470, // ~20% descuento
    maxListings: 15,
    features: [
      "Hasta 15 vehículos",
      "Perfil de dealer",
      "Estadísticas básicas",
      "Soporte por email",
    ],
  },
  pro: {
    name: "Pro",
    price: 129,
    priceYearly: 1238,
    maxListings: 50,
    features: [
      "Hasta 50 vehículos",
      "Badge verificado",
      "Estadísticas avanzadas",
      "CRM de leads",
      "Importación CSV",
      "Soporte prioritario",
    ],
  },
  enterprise: {
    name: "Enterprise",
    price: 299,
    priceYearly: 2870,
    maxListings: -1, // ilimitado
    features: [
      "Vehículos ilimitados",
      "Múltiples ubicaciones",
      "API de integración",
      "Pricing intelligence",
      "Account manager",
      "Soporte 24/7",
    ],
  },
} as const;

/** Precios de boost/promociones */
export const BOOST_PRICES = {
  "7_days": { days: 7, price: 500 },
  "14_days": { days: 14, price: 900 },
  "30_days": { days: 30, price: 1500 },
} as const;

/** Precio de publicación individual */
export const LISTING_FEE = 29; // USD para vendedor individual
```

---

## 🔔 TIPOS DE NOTIFICACIONES

```typescript
// filepath: src/types/notifications.ts

/** Tipo de notificación */
export type NotificationType =
  | "message"
  | "inquiry"
  | "price_alert"
  | "favorite_sold"
  | "listing_approved"
  | "listing_rejected"
  | "listing_expired"
  | "payment_success"
  | "payment_failed"
  | "review_received"
  | "system";

/** Notificación */
export interface Notification {
  id: string;
  userId: string;
  type: NotificationType;
  title: string;
  message: string;
  imageUrl?: string;
  actionUrl?: string;
  actionLabel?: string;
  isRead: boolean;
  metadata?: Record<string, unknown>;
  createdAt: string;
}

/** Preferencias de notificación */
export interface NotificationPreferences {
  email: {
    messages: boolean;
    inquiries: boolean;
    priceAlerts: boolean;
    marketing: boolean;
    systemUpdates: boolean;
  };
  push: {
    messages: boolean;
    inquiries: boolean;
    priceAlerts: boolean;
  };
  sms: {
    inquiries: boolean;
    securityAlerts: boolean;
  };
}
```

---

## 💬 TIPOS DE MENSAJES

```typescript
// filepath: src/types/messages.ts

/** Estado de la conversación */
export type ConversationStatus = "active" | "archived" | "blocked";

/** Conversación */
export interface Conversation {
  id: string;
  participants: Participant[];
  vehicleId?: string;
  vehicle?: VehicleSummary;
  lastMessage?: Message;
  unreadCount: number;
  status: ConversationStatus;
  createdAt: string;
  updatedAt: string;
}

/** Participante */
export interface Participant {
  userId: string;
  name: string;
  avatar?: string;
  role: "buyer" | "seller" | "dealer";
  isOnline?: boolean;
  lastSeenAt?: string;
}

/** Mensaje */
export interface Message {
  id: string;
  conversationId: string;
  senderId: string;
  content: string;
  type: "text" | "image" | "offer" | "system";
  attachments?: Attachment[];
  offer?: Offer;
  isRead: boolean;
  readAt?: string;
  createdAt: string;
}

/** Adjunto */
export interface Attachment {
  id: string;
  type: "image" | "document";
  url: string;
  name: string;
  size: number;
}

/** Oferta en mensaje */
export interface Offer {
  amount: number;
  status: "pending" | "accepted" | "rejected" | "expired";
  expiresAt: string;
}
```

---

## ⭐ TIPOS DE REVIEWS

```typescript
// filepath: src/types/reviews.ts

/** Review */
export interface Review {
  id: string;
  reviewerId: string;
  reviewer: UserProfile;
  targetId: string; // userId o dealerId
  targetType: "user" | "dealer";
  rating: number; // 1-5
  title?: string;
  comment: string;
  pros?: string[];
  cons?: string[];
  vehicleId?: string;
  vehicle?: VehicleSummary;
  isVerifiedPurchase: boolean;
  helpfulCount: number;
  response?: ReviewResponse;
  status: "pending" | "published" | "hidden";
  createdAt: string;
  updatedAt: string;
}

/** Respuesta a review */
export interface ReviewResponse {
  content: string;
  createdAt: string;
}

/** Resumen de ratings */
export interface RatingSummary {
  averageRating: number;
  totalReviews: number;
  distribution: {
    5: number;
    4: number;
    3: number;
    2: number;
    1: number;
  };
}
```

---

## 🔔 TIPOS DE ALERTAS

```typescript
// filepath: src/types/alerts.ts

/** Alerta de precio */
export interface PriceAlert {
  id: string;
  userId: string;
  vehicleId: string;
  vehicle: VehicleSummary;
  targetPrice: number;
  currentPrice: number;
  isActive: boolean;
  isTriggered: boolean;
  triggeredAt?: string;
  createdAt: string;
}

/** Búsqueda guardada */
export interface SavedSearch {
  id: string;
  userId: string;
  name: string;
  filters: VehicleFilters;
  notifyOnNewResults: boolean;
  notifyOnPriceDrops: boolean;
  frequency: "instant" | "daily" | "weekly";
  lastNotifiedAt?: string;
  newResultsCount: number;
  createdAt: string;
  updatedAt: string;
}
```

---

## 🔧 TIPOS DE UI

```typescript
// filepath: src/types/ui.ts

/** Estado de modal */
export interface ModalState {
  isOpen: boolean;
  type?: string;
  data?: unknown;
}

/** Toast */
export interface Toast {
  id: string;
  type: "success" | "error" | "warning" | "info";
  title: string;
  message?: string;
  duration?: number;
  action?: {
    label: string;
    onClick: () => void;
  };
}

/** Breadcrumb */
export interface Breadcrumb {
  label: string;
  href?: string;
}

/** Tab */
export interface Tab {
  id: string;
  label: string;
  count?: number;
  icon?: React.ComponentType;
}

/** Opción de select */
export interface SelectOption<T = string> {
  value: T;
  label: string;
  disabled?: boolean;
  icon?: React.ComponentType;
}

/** Estado de formulario */
export interface FormState<T> {
  data: T;
  errors: Partial<Record<keyof T, string>>;
  isSubmitting: boolean;
  isValid: boolean;
}
```

---

## 📦 ARCHIVO INDEX (Re-export)

```typescript
// filepath: src/types/index.ts

// API
export * from "./api";

// Auth
export * from "./auth";

// Vehicles
export * from "./vehicles";

// Dealers
export * from "./dealers";

// Billing
export * from "./billing";

// Notifications
export * from "./notifications";

// Messages
export * from "./messages";

// Reviews
export * from "./reviews";

// Alerts
export * from "./alerts";

// UI
export * from "./ui";

// ============================================================================
// UTILITY TYPES
// ============================================================================

/** Hacer todas las propiedades opcionales recursivamente */
export type DeepPartial<T> = {
  [P in keyof T]?: T[P] extends object ? DeepPartial<T[P]> : T[P];
};

/** Extraer el tipo de un array */
export type ArrayElement<T> = T extends readonly (infer U)[] ? U : never;

/** Hacer algunas propiedades requeridas */
export type RequireFields<T, K extends keyof T> = T & Required<Pick<T, K>>;

/** Omitir propiedades de un tipo */
export type StrictOmit<T, K extends keyof T> = Pick<T, Exclude<keyof T, K>>;
```

---

## ✅ CHECKLIST DE USO

### Importación

```typescript
// Importar tipos específicos
import type { Vehicle, VehicleSummary, VehicleFilters } from "@/types";

// Importar todo
import type * as Types from "@/types";
```

### Validación con Zod

```typescript
import { z } from "zod";
import type { RegisterData } from "@/types";

export const registerSchema = z.object({
  email: z.string().email(),
  password: z.string().min(8),
  firstName: z.string().min(2),
  lastName: z.string().min(2),
  phone: z.string().optional(),
  acceptTerms: z.literal(true),
  acceptMarketing: z.boolean().optional(),
}) satisfies z.ZodType<RegisterData>;
```

### Con React Query

```typescript
import { useQuery } from "@tanstack/react-query";
import type { Vehicle, PaginatedResponse } from "@/types";

export function useVehicles(filters: VehicleFilters) {
  return useQuery<PaginatedResponse<Vehicle>>({
    queryKey: ["vehicles", filters],
    queryFn: () => vehiclesApi.getVehicles(filters),
  });
}
```

---

## 📚 REFERENCIAS

- [TypeScript Handbook](https://www.typescriptlang.org/docs/handbook/)
- [Zod Documentation](https://zod.dev/)
- [TanStack Query Types](https://tanstack.com/query/latest/docs/react/typescript)
