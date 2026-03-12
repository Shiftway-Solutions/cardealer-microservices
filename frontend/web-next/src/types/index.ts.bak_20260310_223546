// =============================================================================
// OKLA - Global TypeScript Types
// =============================================================================
// Tipos centralizados para toda la aplicación

// -----------------------------------------------------------------------------
// Vehicle Types
// -----------------------------------------------------------------------------

export interface Vehicle {
  id: string;
  slug: string;

  // Basic Info
  make: string;
  model: string;
  year: number;
  trim?: string;
  bodyType: VehicleBodyType;
  description?: string;

  // Pricing
  price: number;
  originalPrice?: number;
  marketPrice?: number;
  currency: 'DOP' | 'USD';
  dealRating?: DealRating;
  isNegotiable?: boolean;

  // Technical Specs
  mileage: number;
  transmission: 'automatic' | 'manual' | 'cvt';
  fuelType: 'gasoline' | 'diesel' | 'hybrid' | 'electric';
  drivetrain?: '2wd' | '4wd' | 'awd';
  engineSize?: string;
  horsepower?: number;

  // Features
  exteriorColor?: string;
  interiorColor?: string;
  doors?: number;
  seats?: number;
  features?: string[];

  // Media
  images: VehicleImage[];
  has360View?: boolean;
  hasVideo?: boolean;

  // Status
  status: VehicleStatus;
  condition: 'new' | 'used' | 'certified';
  isFeatured?: boolean;
  viewCount?: number;
  favoriteCount?: number;
  rejectionReason?: string;
  vin?: string;
  oklaScore?: number;

  // Odometer verification (VinAudit/CARFAX cross-reference)
  odometerRollbackDetected?: boolean;
  historicalMileage?: number;
  odometerVerifiedAt?: string;

  // Seller
  sellerId: string;
  sellerType: 'seller' | 'dealer';

  // Location
  location: VehicleLocation;

  // Timestamps
  createdAt: string;
  updatedAt: string;
  publishedAt?: string;
}

export type VehicleBodyType =
  | 'sedan'
  | 'suv'
  | 'pickup'
  | 'hatchback'
  | 'coupe'
  | 'convertible'
  | 'minivan'
  | 'wagon'
  | 'crossover'
  | 'sports';

export type VehicleStatus =
  | 'draft'
  | 'pending'
  | 'active'
  | 'paused'
  | 'sold'
  | 'reserved'
  | 'expired'
  | 'rejected';

export type DealRating = 'great' | 'good' | 'fair' | 'high' | 'uncertain';

export interface VehicleImage {
  id: string;
  url: string;
  thumbnailUrl?: string;
  alt?: string;
  order: number;
  isPrimary?: boolean;
}

export interface VehicleLocation {
  city: string;
  province: string;
  country: string;
  coordinates?: {
    latitude: number;
    longitude: number;
  };
}

// Vehicle Card Props (simplified for lists)
export interface VehicleCardData {
  id: string;
  slug: string;
  make: string;
  model: string;
  year: number;
  price: number;
  currency?: 'DOP' | 'USD';
  mileage: number;
  transmission: string;
  fuelType: string;
  imageUrl: string;
  dealRating?: DealRating;
  location: string;
  isFavorite?: boolean;
  // Optional enhanced fields
  trim?: string;
  photoCount?: number;
  isNew?: boolean;
  isCertified?: boolean;
  isFeatured?: boolean;
  isVerified?: boolean;
  monthlyPayment?: number;
  dealerName?: string;
  dealerRating?: number;
  oklaScore?: number;
  // Status and metadata (for dealer inventory)
  status?: VehicleStatus;
  viewCount?: number;
  createdAt?: string;
}

// -----------------------------------------------------------------------------
// User Types
// -----------------------------------------------------------------------------

export interface User {
  id: string;
  email: string;

  // Profile
  firstName: string;
  lastName: string;
  fullName: string;
  avatarUrl?: string;
  phone?: string;

  // Account
  accountType:
    | 'buyer'
    | 'seller'
    | 'dealer'
    | 'dealer_employee'
    | 'admin'
    | 'platform_employee'
    | 'guest';
  userIntent?: 'browse' | 'buy' | 'sell' | 'buy_and_sell';
  isVerified: boolean;
  isEmailVerified: boolean;
  isPhoneVerified: boolean;

  // Preferences
  preferredLocale: string;
  preferredCurrency: 'DOP' | 'USD';
  notificationPreferences?: NotificationPreferences;

  // Stats
  listingsCount?: number;
  favoritesCount?: number;

  // Timestamps
  createdAt: string;
  lastLoginAt?: string;
}

export interface NotificationPreferences {
  email: boolean;
  sms: boolean;
  push: boolean;
  priceAlerts: boolean;
  newListings: boolean;
  messages: boolean;
  promotions: boolean;
}

// -----------------------------------------------------------------------------
// Dealer Types
// -----------------------------------------------------------------------------

export interface Dealer {
  id: string;
  userId: string;

  // Business Info
  businessName: string;
  legalName?: string;
  rnc?: string;
  type: DealerType;
  description?: string;
  establishedDate?: string;
  employeeCount?: number;

  // Contact
  email: string;
  phone: string;
  mobilePhone?: string;
  website?: string;
  facebookUrl?: string;
  instagramUrl?: string;
  whatsAppNumber?: string;

  // Address
  address: string;
  city: string;
  province: string;

  // Media
  logoUrl?: string;
  bannerUrl?: string;

  // Verification
  status: DealerStatus;
  verificationStatus: VerificationStatus;

  // Subscription
  plan: DealerPlan;
  isSubscriptionActive: boolean;
  maxActiveListings: number;
  currentActiveListings: number;

  // Stats
  rating?: number;
  reviewCount?: number;
  responseRate?: number;
  responseTime?: string;
  avgResponseTimeMinutes?: number;

  // Timestamps
  createdAt: string;
  verifiedAt?: string;
}

export type DealerType = 'independent' | 'chain' | 'multipleStore' | 'franchise';

export type DealerStatus =
  | 'pending'
  | 'underReview'
  | 'active'
  | 'suspended'
  | 'rejected'
  | 'inactive';

export type VerificationStatus =
  | 'notVerified'
  | 'documentsUploaded'
  | 'underReview'
  | 'verified'
  | 'rejected';

export type DealerPlan = 'none' | 'libre' | 'visible' | 'pro' | 'elite';

export type SellerPlan = 'gratis' | 'premium' | 'pro';

// -----------------------------------------------------------------------------
// API Types
// -----------------------------------------------------------------------------

export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
  errors?: ApiError[];
}

export interface ApiError {
  code: string;
  message: string;
  field?: string;
}

export interface PaginatedResponse<T> {
  items: T[];
  pagination: PaginationInfo;
}

export interface PaginationInfo {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface SearchParams {
  q?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}

export interface VehicleSearchParams extends SearchParams {
  make?: string;
  model?: string;
  yearMin?: number;
  yearMax?: number;
  priceMin?: number;
  priceMax?: number;
  mileageMax?: number;
  bodyType?: VehicleBodyType;
  transmission?: string;
  fuelType?: string;
  condition?: string;
  province?: string;
  city?: string;
  drivetrain?: string;
  color?: string;
  isCertified?: boolean;
  hasCleanTitle?: boolean;
  sellerType?: 'dealer' | 'seller';
  features?: string[];
  dealRating?: DealRating;
  // Extended DR-market filters
  seats?: number; // min seats
  cylinders?: number; // engine cylinders
  interiorColor?: string;
}

// -----------------------------------------------------------------------------
// Form Types
// -----------------------------------------------------------------------------

export interface LoginForm {
  email: string;
  password: string;
  rememberMe?: boolean;
}

export interface RegisterForm {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  confirmPassword: string;
  phone?: string;
  acceptTerms: boolean;
}

export interface ContactForm {
  name: string;
  email: string;
  phone?: string;
  message: string;
  vehicleId?: string;
}

// -----------------------------------------------------------------------------
// UI Types
// -----------------------------------------------------------------------------

export interface SelectOption<T = string> {
  value: T;
  label: string;
  disabled?: boolean;
}

export interface BreadcrumbItem {
  label: string;
  href?: string;
}

export interface MenuItem {
  id: string;
  label: string;
  href?: string;
  icon?: React.ReactNode;
  onClick?: () => void;
  children?: MenuItem[];
  badge?: string | number;
}

export interface Toast {
  id: string;
  type: 'success' | 'error' | 'warning' | 'info';
  title: string;
  message?: string;
  duration?: number;
}

// -----------------------------------------------------------------------------
// Homepage Types
// -----------------------------------------------------------------------------

export interface HomepageSection {
  id: string;
  name: string;
  slug: string;
  subtitle?: string;
  displayOrder: number;
  maxItems: number;
  isActive: boolean;
  accentColor?: string;
  viewAllHref?: string;
  vehicles: VehicleCardData[];
}

// -----------------------------------------------------------------------------
// Utility Types
// -----------------------------------------------------------------------------

export type WithRequired<T, K extends keyof T> = T & { [P in K]-?: T[P] };

export type PartialBy<T, K extends keyof T> = Omit<T, K> & Partial<Pick<T, K>>;

export type Nullable<T> = T | null;

export type AsyncState<T> = {
  data: T | null;
  isLoading: boolean;
  error: Error | null;
};
