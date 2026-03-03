// ============================================================================
// ENUMS - Tipos de cuenta y planes
// ============================================================================

/**
 * Tipos de cuenta en la plataforma
 * - GUEST: Usuario no autenticado (búsqueda solo)
 * - BUYER: Comprador registrado (favoritos, mensajes)
 * - SELLER: Vendedor individual (publica vehículos)
 * - DEALER: Propietario de concesionario (acceso total)
 * - DEALER_EMPLOYEE: Empleado de concesionario (acceso limitado)
 * - ADMIN: Administrador de la plataforma (acceso total)
 * - PLATFORM_EMPLOYEE: Empleado de plataforma (acceso limitado)
 */
export enum AccountType {
  GUEST = 'guest',
  BUYER = 'buyer',
  SELLER = 'seller',
  DEALER = 'dealer',
  DEALER_EMPLOYEE = 'dealer_employee',
  ADMIN = 'admin',
  PLATFORM_EMPLOYEE = 'platform_employee',
}

/**
 * Roles específicos para empleados de dealers
 */
export enum DealerRole {
  OWNER = 'owner', // Acceso total al dealer
  MANAGER = 'manager', // Gestión completa excepto billing
  SALES_MANAGER = 'sales_manager', // Gestión de ventas y leads
  INVENTORY_MANAGER = 'inventory_manager', // Gestión de inventario
  SALESPERSON = 'salesperson', // Solo leads asignados
  VIEWER = 'viewer', // Solo lectura
}

/**
 * Roles específicos para administradores de la plataforma.
 * Alineado con AdminRole del backend (AdminService.Domain.Enums.AdminRole).
 */
export enum AdminRole {
  SUPER_ADMIN = 'super_admin', // Acceso total a todo
  PLATFORM_ADMIN = 'platform_admin', // Configuración, mantenimiento, homepage
  MODERATION_ADMIN = 'moderation_admin', // Aprobar/rechazar listings, verificar dealers
  SUPPORT_ADMIN = 'support_admin', // Soporte al cliente, tickets
  ANALYTICS_ADMIN = 'analytics_admin', // Solo lectura de analytics/reportes
}

/**
 * @deprecated Use AdminRole instead. Mantener por compatibilidad.
 * Roles específicos para empleados de plataforma
 */
export enum PlatformRole {
  SUPER_ADMIN = 'super_admin', // Acceso total
  ADMIN = 'admin', // Gestión de usuarios y dealers
  MODERATOR = 'moderator', // Moderación de contenido
  SUPPORT = 'support', // Soporte técnico
  ANALYST = 'analyst', // Solo lectura de analytics
}

/**
 * Planes de suscripción para dealers
 */
export enum DealerPlan {
  LIBRE = 'libre', // Plan gratuito: listados ilimitados, stats básicas
  VISIBLE = 'visible', // Visibilidad mejorada, badge verificado, stats avanzadas
  PRO = 'pro', // ChatAgent, CRM, boosts, analytics avanzados
  ELITE = 'elite', // Manager dedicado, API access, white label
}

/**
 * Permisos granulares para empleados de dealers
 */
export enum DealerPermission {
  // Inventory
  VIEW_INVENTORY = 'dealer:inventory:view',
  CREATE_LISTING = 'dealer:inventory:create',
  EDIT_LISTING = 'dealer:inventory:edit',
  DELETE_LISTING = 'dealer:inventory:delete',
  BULK_UPLOAD = 'dealer:inventory:bulk',

  // Leads
  VIEW_ALL_LEADS = 'dealer:leads:view:all',
  VIEW_ASSIGNED_LEADS = 'dealer:leads:view:assigned',
  ASSIGN_LEADS = 'dealer:leads:assign',
  EDIT_LEADS = 'dealer:leads:edit',

  // Team Management
  INVITE_EMPLOYEES = 'dealer:team:invite',
  MANAGE_EMPLOYEES = 'dealer:team:manage',
  ASSIGN_ROLES = 'dealer:team:assign_roles',

  // Analytics
  VIEW_ANALYTICS = 'dealer:analytics:view',
  EXPORT_REPORTS = 'dealer:analytics:export',

  // Billing
  VIEW_BILLING = 'dealer:billing:view',
  MANAGE_SUBSCRIPTION = 'dealer:billing:manage',

  // Settings
  EDIT_DEALER_PROFILE = 'dealer:settings:edit',
  MANAGE_BRANDING = 'dealer:settings:branding',
}

/**
 * Permisos para empleados de plataforma
 */
export enum PlatformPermission {
  // User Management
  VIEW_USERS = 'platform:users:view',
  EDIT_USERS = 'platform:users:edit',
  DELETE_USERS = 'platform:users:delete',
  SUSPEND_USERS = 'platform:users:suspend',

  // Dealer Management
  VIEW_DEALERS = 'platform:dealers:view',
  APPROVE_DEALERS = 'platform:dealers:approve',
  SUSPEND_DEALERS = 'platform:dealers:suspend',

  // Content Moderation
  MODERATE_LISTINGS = 'platform:content:moderate',
  DELETE_LISTINGS = 'platform:content:delete',

  // System Config
  MANAGE_SETTINGS = 'platform:settings:manage',
  VIEW_ANALYTICS = 'platform:analytics:view',

  // Support
  ACCESS_SUPPORT_TICKETS = 'platform:support:access',
  IMPERSONATE_USER = 'platform:support:impersonate',
}

export enum VehicleStatus {
  AVAILABLE = 'available',
  SOLD = 'sold',
  PENDING = 'pending',
  RESERVED = 'reserved',
}

export enum TransmissionType {
  MANUAL = 'manual',
  AUTOMATIC = 'automatic',
  SEMI_AUTOMATIC = 'semi_automatic',
}

export enum FuelType {
  GASOLINE = 'gasoline',
  DIESEL = 'diesel',
  ELECTRIC = 'electric',
  HYBRID = 'hybrid',
  PLUG_IN_HYBRID = 'plug_in_hybrid',
}

export enum BodyType {
  SEDAN = 'sedan',
  SUV = 'suv',
  HATCHBACK = 'hatchback',
  COUPE = 'coupe',
  CONVERTIBLE = 'convertible',
  WAGON = 'wagon',
  PICKUP = 'pickup',
  VAN = 'van',
}

// ============================================================================
// EMPLOYEE MANAGEMENT TYPES
// ============================================================================

/**
 * Features disponibles por plan de dealer
 */
export interface DealerPlanFeatures {
  maxListings: number; // Máximo de vehículos publicados
  maxImages: number; // Imágenes por vehículo
  analyticsAccess: boolean; // Acceso a analytics dashboard
  marketPriceAnalysis: boolean; // Análisis de precios de mercado
  bulkUpload: boolean; // Carga masiva CSV/Excel
  featuredListings: number; // Publicaciones destacadas permitidas
  leadManagement: boolean; // CRM de leads
  emailAutomation: boolean; // Secuencias de email
  customBranding: boolean; // Logo y colores personalizados
  apiAccess: boolean; // Acceso a API
  prioritySupport: boolean; // Soporte prioritario
  whatsappIntegration: boolean; // Integración WhatsApp Business
}

/**
 * Configuración de límites por plan
 */
export const DEALER_PLAN_LIMITS: Record<DealerPlan, DealerPlanFeatures> = {
  [DealerPlan.LIBRE]: {
    maxListings: 999999, // Ilimitado
    maxImages: 10,
    analyticsAccess: false,
    marketPriceAnalysis: false,
    bulkUpload: false,
    featuredListings: 0,
    leadManagement: false,
    emailAutomation: false,
    customBranding: false,
    apiAccess: false,
    prioritySupport: false,
    whatsappIntegration: false,
  },
  [DealerPlan.VISIBLE]: {
    maxListings: 999999, // Ilimitado
    maxImages: 25,
    analyticsAccess: true,
    marketPriceAnalysis: false,
    bulkUpload: true,
    featuredListings: 3,
    leadManagement: true,
    emailAutomation: false,
    customBranding: false,
    apiAccess: false,
    prioritySupport: false,
    whatsappIntegration: false,
  },
  [DealerPlan.PRO]: {
    maxListings: 999999, // Ilimitado
    maxImages: 40,
    analyticsAccess: true,
    marketPriceAnalysis: true,
    bulkUpload: true,
    featuredListings: 10,
    leadManagement: true,
    emailAutomation: true,
    customBranding: true,
    apiAccess: false,
    prioritySupport: true,
    whatsappIntegration: true,
  },
  [DealerPlan.ELITE]: {
    maxListings: 999999, // Ilimitado
    maxImages: 50,
    analyticsAccess: true,
    marketPriceAnalysis: true,
    bulkUpload: true,
    featuredListings: 50,
    leadManagement: true,
    emailAutomation: true,
    customBranding: true,
    apiAccess: true,
    prioritySupport: true,
    whatsappIntegration: true,
  },
};

/**
 * Información de suscripción de dealer
 */
export interface DealerSubscription {
  plan: DealerPlan;
  status: 'active' | 'canceled' | 'expired' | 'trial';
  startDate: string;
  endDate?: string; // null si es FREE (permanente)
  trialEndDate?: string; // Para trials de planes pagados
  features: DealerPlanFeatures;
  usage: {
    currentListings: number; // Listados actuales
    listingsThisMonth: number; // Listados publicados este mes
    featuredUsed: number; // Featured listings usados
  };
}

/**
 * Empleado de un dealer
 */
export interface DealerEmployee {
  id: string;
  userId: string;
  user?: User;
  dealerId: string;
  dealerRole: DealerRole;
  permissions: DealerPermission[];
  invitedBy: string;
  invitedByUser?: User;
  status: 'pending' | 'active' | 'suspended';
  invitationDate: string;
  activationDate?: string;
  notes?: string;
}

/**
 * Empleado de la plataforma
 */
export interface PlatformEmployee {
  id: string;
  userId: string;
  user?: User;
  platformRole: PlatformRole;
  permissions: PlatformPermission[];
  assignedBy: string;
  assignedByUser?: User;
  status: 'active' | 'suspended';
  hireDate: string;
  department?: string;
  notes?: string;
}

/**
 * Invitación para unirse como empleado de dealer
 */
export interface DealerEmployeeInvitation {
  id: string;
  dealerId: string;
  email: string;
  dealerRole: DealerRole;
  permissions: DealerPermission[];
  invitedBy: string;
  status: 'pending' | 'accepted' | 'expired' | 'revoked';
  invitationDate: string;
  expirationDate: string;
  acceptedDate?: string;
}

/**
 * Invitación para unirse como empleado de plataforma
 */
export interface PlatformEmployeeInvitation {
  id: string;
  email: string;
  platformRole: PlatformRole;
  permissions: PlatformPermission[];
  invitedBy: string;
  status: 'pending' | 'accepted' | 'expired' | 'revoked';
  invitationDate: string;
  expirationDate: string;
  acceptedDate?: string;
}

// ============================================================================
// USER & AUTH TYPES
// ============================================================================

export interface User {
  id: string;
  email: string;
  name: string;
  phone?: string;
  avatar?: string;
  accountType: AccountType;

  // Dealer subscription (solo si accountType = DEALER)
  subscription?: DealerSubscription;

  // Platform-level (si es admin o platform employee)
  platformRole?: PlatformRole;
  platformPermissions?: PlatformPermission[];

  // Dealer-level (si es dealer o dealer employee)
  dealerId?: string;
  dealerRole?: DealerRole;
  dealerPermissions?: DealerPermission[];
  dealer?: DealerInfo;

  // Employee metadata
  employerUserId?: string; // ID del que lo contrató (dealer owner o admin)
  createdBy?: string; // Usuario que creó esta cuenta

  // Legacy fields (mantener compatibilidad)
  roles: string[];
  permissions?: string[];

  emailVerified: boolean;
  phoneVerified: boolean;
  twoFactorEnabled: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface DealerInfo {
  id: string;
  businessName: string;
  tradeName?: string;
  taxId: string;
  logo?: string;
  description?: string;
  address: Address;
  phone: string;
  email: string;
  website?: string;
  socialMedia?: SocialMedia;
  plan: DealerPlan;
  verificationStatus: 'pending' | 'verified' | 'rejected';
  verificationDate?: string;
  rating: number;
  totalReviews: number;
  totalSales: number;
  stats: DealerStats;
}

export interface DealerStats {
  activeListings: number;
  totalListings: number;
  leadsThisMonth: number;
  conversionRate: number;
  averageResponseTime: number; // en minutos
  profileViews: number;
}

export interface Subscription {
  id: string;
  plan: DealerPlan;
  status: 'active' | 'cancelled' | 'expired' | 'past_due';
  startDate: string;
  endDate: string;
  autoRenew: boolean;
  paymentMethod: PaymentMethod;
  limits: SubscriptionLimits;
}

export interface SubscriptionLimits {
  maxListings: number;
  maxPhotosPerListing: number;
  analyticsAccess: boolean;
  bulkUpload: boolean;
  prioritySupport: boolean;
  apiAccess: boolean;
  whiteLabelAccess: boolean;
}

export interface PaymentMethod {
  type: 'credit_card' | 'bank_transfer' | 'paypal';
  last4?: string;
  brand?: string;
  expiryMonth?: number;
  expiryYear?: number;
}

export interface Address {
  street: string;
  city: string;
  state: string;
  zipCode: string;
  country: string;
  latitude?: number;
  longitude?: number;
}

export interface SocialMedia {
  facebook?: string;
  instagram?: string;
  twitter?: string;
  linkedin?: string;
  youtube?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  user: User;
  accessToken: string;
  refreshToken: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  name: string;
  phone?: string;
  accountType: AccountType; // NUEVO: especificar tipo al registrar
}

// ============================================================================
// VEHICLE TYPES
// ============================================================================

export interface Vehicle {
  id: string;
  make: string;
  model: string;
  year: number;
  price: number;
  mileage: number;
  description: string;
  status: VehicleStatus;
  transmission: TransmissionType;
  fuelType: FuelType;
  bodyType: BodyType;
  color: string;
  doors: number;
  seats: number;
  engine: string;
  horsepower: number;
  vin?: string;
  licensePlate?: string;
  images: VehicleImage[];
  features: string[];

  // Dealer-specific fields
  dealerId?: string;
  dealerInfo?: DealerInfo;
  acquisitionCost?: number; // Costo de adquisición (privado)
  daysInInventory?: number; // Días en inventario
  viewCount?: number;
  leadCount?: number;
  isFeatured?: boolean;

  location: Address;
  createdAt: string;
  updatedAt: string;
  publishedAt?: string;
}

export interface VehicleImage {
  id: string;
  url: string;
  thumbnailUrl: string;
  order: number;
  caption?: string;
}

export interface VehicleSearchParams {
  make?: string;
  model?: string;
  minYear?: number;
  maxYear?: number;
  minPrice?: number;
  maxPrice?: number;
  maxMileage?: number;
  transmission?: TransmissionType;
  fuelType?: FuelType;
  bodyType?: BodyType;
  dealerId?: string;
  status?: VehicleStatus;
  page?: number;
  limit?: number;
  sortBy?: 'price' | 'year' | 'mileage' | 'createdAt';
  sortOrder?: 'asc' | 'desc';
}

// ============================================================================
// DEALER-SPECIFIC TYPES
// ============================================================================

export interface InventoryItem extends Vehicle {
  acquisitionCost: number;
  acquisitionDate: string;
  expectedProfit: number;
  profitMargin: number;
  daysInInventory: number;
  reservedBy?: string;
  reservedUntil?: string;
}

export interface Lead {
  id: string;
  vehicleId: string;
  vehicle: Vehicle;
  customerId: string;
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  message: string;
  status: 'new' | 'contacted' | 'qualified' | 'negotiating' | 'won' | 'lost';
  source: 'website' | 'phone' | 'email' | 'chat' | 'walk_in';
  assignedTo?: string;
  notes: LeadNote[];
  scheduledTestDrive?: TestDrive;
  createdAt: string;
  updatedAt: string;
  lastContactedAt?: string;
}

export interface LeadNote {
  id: string;
  content: string;
  createdBy: string;
  createdAt: string;
}

export interface TestDrive {
  id: string;
  scheduledDate: string;
  duration: number; // minutos
  status: 'scheduled' | 'completed' | 'cancelled' | 'no_show';
  notes?: string;
}

export interface Campaign {
  id: string;
  name: string;
  description: string;
  type: 'discount' | 'featured' | 'email' | 'social';
  status: 'draft' | 'active' | 'paused' | 'completed';
  startDate: string;
  endDate: string;
  budget?: number;
  vehicleIds: string[];
  metrics: CampaignMetrics;
  createdAt: string;
  updatedAt: string;
}

export interface CampaignMetrics {
  impressions: number;
  clicks: number;
  leads: number;
  conversions: number;
  cost: number;
  roi: number;
}

export interface DealerAnalytics {
  period: 'day' | 'week' | 'month' | 'year';
  startDate: string;
  endDate: string;
  metrics: {
    totalViews: number;
    totalLeads: number;
    totalSales: number;
    revenue: number;
    averageSalePrice: number;
    conversionRate: number;
    averageDaysToSell: number;
    inventoryTurnover: number;
  };
  topVehicles: Array<{
    vehicleId: string;
    vehicle: Vehicle;
    views: number;
    leads: number;
  }>;
  leadsBySource: Record<string, number>;
  salesByMonth: Array<{
    month: string;
    sales: number;
    revenue: number;
  }>;
}

// ============================================================================
// API RESPONSE TYPES
// ============================================================================

export interface ApiResponse<T = unknown> {
  success: boolean;
  data?: T;
  error?: string;
  message?: string;
}

export interface PaginatedResponse<T> {
  items: T[];
  total: number;
  page: number;
  limit: number;
  totalPages: number;
}

// ============================================================================
// NOTIFICATION TYPES
// ============================================================================

export interface Notification {
  id: string;
  type: 'info' | 'success' | 'warning' | 'error';
  title: string;
  message: string;
  read: boolean;
  actionUrl?: string;
  createdAt: string;
}

// ============================================================================
// MESSAGE TYPES
// ============================================================================

export interface Message {
  id: string;
  senderId: string;
  senderName: string;
  receiverId: string;
  receiverName: string;
  vehicleId?: string;
  subject?: string;
  content: string;
  read: boolean;
  createdAt: string;
}

export interface Conversation {
  id: string;
  participants: User[];
  lastMessage: Message;
  unreadCount: number;
  updatedAt: string;
}
