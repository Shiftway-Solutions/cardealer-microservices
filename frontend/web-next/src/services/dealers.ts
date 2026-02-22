/**
 * Dealers Service - API client for dealer operations
 * Connects via API Gateway to DealerManagementService
 */

import { apiClient } from '@/lib/api-client';
import type {
  Dealer,
  DealerType,
  DealerStatus,
  VerificationStatus,
  DealerPlan,
  PaginatedResponse,
} from '@/types';

// ============================================================
// API TYPES
// ============================================================

export interface DealerDto {
  id: string;
  userId: string;
  businessName: string;
  legalName?: string;
  rnc?: string;
  type: DealerType;
  email: string;
  phone: string;
  mobilePhone?: string;
  website?: string;
  facebookUrl?: string;
  instagramUrl?: string;
  whatsAppNumber?: string;
  address: string;
  city: string;
  province: string;
  logoUrl?: string;
  bannerUrl?: string;
  status: DealerStatus;
  verificationStatus: VerificationStatus;
  currentPlan: DealerPlan;
  isSubscriptionActive: boolean;
  maxActiveListings: number;
  currentActiveListings: number;
  rating?: number;
  reviewCount?: number;
  responseRate?: number;
  avgResponseTimeMinutes?: number;
  createdAt: string;
  verifiedAt?: string;
  description?: string;
  establishedDate?: string;
  employeeCount?: number;
}

export interface DealerLocationDto {
  id: string;
  dealerId: string;
  name: string;
  address: string;
  city: string;
  province: string;
  phone?: string;
  email?: string;
  isPrimary: boolean;
  locationType: 'headquarters' | 'branch' | 'showroom' | 'serviceCenter' | 'warehouse';
  latitude?: number;
  longitude?: number;
  businessHours?: BusinessHours;
}

export interface BusinessHours {
  monday?: DayHours;
  tuesday?: DayHours;
  wednesday?: DayHours;
  thursday?: DayHours;
  friday?: DayHours;
  saturday?: DayHours;
  sunday?: DayHours;
}

export interface DayHours {
  open: string;
  close: string;
  isClosed?: boolean;
}

export interface CreateDealerRequest {
  // Basic info
  businessName: string;
  tradeName?: string; // Trade/display name (was: legalName)
  description?: string;
  dealerType?: string; // 'Independent' | 'Chain' | 'Franchise' (was: type)

  // Contact
  email: string;
  phone: string;
  mobilePhone?: string;
  whatsApp?: string; // WhatsApp number (was: whatsAppNumber)
  website?: string;
  facebookUrl?: string;
  instagramUrl?: string;

  // Location — fields must match UserService backend field names
  address: string;
  city: string;
  state: string; // Province/state (was: province)
  zipCode?: string;
  country?: string;

  // Legal
  businessRegistrationNumber?: string; // RNC (was: rnc)
  taxId?: string;
  dealerLicenseNumber?: string;

  // Branding
  logoUrl?: string;
  bannerUrl?: string;

  // Legacy fields kept for backward compat with GET responses from DealerManagementService
  legalName?: string;
  rnc?: string;
  type?: DealerType;
  province?: string;
  whatsAppNumber?: string;
  establishedDate?: string;
  employeeCount?: number;
}

export interface UpdateDealerRequest {
  businessName?: string;
  legalName?: string;
  phone?: string;
  mobilePhone?: string;
  website?: string;
  facebookUrl?: string;
  instagramUrl?: string;
  whatsAppNumber?: string;
  address?: string;
  city?: string;
  province?: string;
  description?: string;
  logoUrl?: string;
  bannerUrl?: string;
}

export interface DealerSearchParams {
  page?: number;
  pageSize?: number;
  status?: DealerStatus;
  verificationStatus?: VerificationStatus;
  city?: string;
  province?: string;
  searchTerm?: string;
}

export interface DealerStatsDto {
  totalListings: number;
  activeListings: number;
  totalViews: number;
  viewsThisMonth: number;
  viewsChange: number;
  totalInquiries: number;
  inquiriesThisMonth: number;
  inquiriesChange: number;
  pendingInquiries: number;
  responseRate: number;
  avgResponseTimeMinutes: number;
  totalRevenue: number;
  revenueThisMonth: number;
  revenueChange: number;
}

// ============================================================
// TRANSFORM FUNCTIONS
// ============================================================

export function transformDealer(dto: DealerDto): Dealer {
  return {
    id: dto.id,
    userId: dto.userId,
    businessName: dto.businessName,
    legalName: dto.legalName,
    rnc: dto.rnc,
    type: dto.type,
    description: dto.description,
    establishedDate: dto.establishedDate,
    employeeCount: dto.employeeCount,
    email: dto.email,
    phone: dto.phone,
    mobilePhone: dto.mobilePhone,
    website: dto.website,
    facebookUrl: dto.facebookUrl,
    instagramUrl: dto.instagramUrl,
    whatsAppNumber: dto.whatsAppNumber,
    address: dto.address,
    city: dto.city,
    province: dto.province,
    logoUrl: dto.logoUrl,
    bannerUrl: dto.bannerUrl,
    status: dto.status,
    verificationStatus: dto.verificationStatus,
    plan: dto.currentPlan,
    isSubscriptionActive: dto.isSubscriptionActive,
    maxActiveListings: dto.maxActiveListings,
    currentActiveListings: dto.currentActiveListings,
    rating: dto.rating,
    reviewCount: dto.reviewCount,
    responseRate: dto.responseRate,
    avgResponseTimeMinutes: dto.avgResponseTimeMinutes,
    responseTime: dto.avgResponseTimeMinutes
      ? formatResponseTime(dto.avgResponseTimeMinutes)
      : undefined,
    createdAt: dto.createdAt,
    verifiedAt: dto.verifiedAt,
  };
}

function formatResponseTime(minutes: number): string {
  if (minutes < 60) {
    return `${minutes} min`;
  }
  const hours = Math.floor(minutes / 60);
  if (hours < 24) {
    return `${hours}h`;
  }
  const days = Math.floor(hours / 24);
  return `${days}d`;
}

// ============================================================
// API FUNCTIONS
// ============================================================

/**
 * Get list of dealers with pagination and filters
 */
export async function getDealers(
  params: DealerSearchParams = {}
): Promise<PaginatedResponse<Dealer>> {
  const response = await apiClient.get<{
    items: DealerDto[];
    pagination: {
      page: number;
      pageSize: number;
      totalItems: number;
      totalPages: number;
    };
  }>('/api/dealers', { params });

  return {
    items: response.data.items.map(transformDealer),
    pagination: {
      ...response.data.pagination,
      hasNextPage: response.data.pagination.page < response.data.pagination.totalPages,
      hasPreviousPage: response.data.pagination.page > 1,
    },
  };
}

/**
 * Get dealer by ID
 */
export async function getDealerById(id: string): Promise<Dealer> {
  const response = await apiClient.get<DealerDto>(`/api/dealers/${id}`);
  return transformDealer(response.data);
}

/**
 * Get dealer by slug (URL-friendly business name)
 */
export async function getDealerBySlug(slug: string): Promise<Dealer> {
  const response = await apiClient.get<DealerDto>(`/api/dealers/slug/${slug}`);
  return transformDealer(response.data);
}

/**
 * Get dealer by user ID (for logged-in dealer users)
 */
export async function getDealerByUserId(userId: string): Promise<Dealer | null> {
  try {
    const response = await apiClient.get<DealerDto>(`/api/dealers/user/${userId}`);
    return transformDealer(response.data);
  } catch {
    return null;
  }
}

/**
 * Get current dealer (for logged-in dealer users)
 */
export async function getCurrentDealer(): Promise<Dealer | null> {
  try {
    const response = await apiClient.get<DealerDto>('/api/dealers/me');
    return transformDealer(response.data);
  } catch {
    return null;
  }
}

/**
 * Create new dealer
 */
export async function createDealer(data: CreateDealerRequest): Promise<Dealer> {
  const response = await apiClient.post<DealerDto>('/api/dealers', data);
  return transformDealer(response.data);
}

/**
 * Update dealer profile
 */
export async function updateDealer(id: string, data: UpdateDealerRequest): Promise<Dealer> {
  const response = await apiClient.put<DealerDto>(`/api/dealers/${id}`, data);
  return transformDealer(response.data);
}

/**
 * Get dealer statistics
 */
export async function getDealerStats(dealerId: string): Promise<DealerStatsDto> {
  const response = await apiClient.get<DealerStatsDto>(`/api/dealers/${dealerId}/stats`);
  return response.data;
}

/**
 * Get dealer locations
 */
export async function getDealerLocations(dealerId: string): Promise<DealerLocationDto[]> {
  const response = await apiClient.get<DealerLocationDto[]>(`/api/dealers/${dealerId}/locations`);
  return response.data;
}

/**
 * Add dealer location
 */
export async function addDealerLocation(
  dealerId: string,
  data: Omit<DealerLocationDto, 'id' | 'dealerId'>
): Promise<DealerLocationDto> {
  const response = await apiClient.post<DealerLocationDto>(
    `/api/dealers/${dealerId}/locations`,
    data
  );
  return response.data;
}

/**
 * Update dealer location
 */
export async function updateDealerLocation(
  dealerId: string,
  locationId: string,
  data: Partial<DealerLocationDto>
): Promise<DealerLocationDto> {
  const response = await apiClient.put<DealerLocationDto>(
    `/api/dealers/${dealerId}/locations/${locationId}`,
    data
  );
  return response.data;
}

/**
 * Delete dealer location
 */
export async function deleteDealerLocation(dealerId: string, locationId: string): Promise<void> {
  await apiClient.delete(`/api/dealers/${dealerId}/locations/${locationId}`);
}

// ============================================================
// DOCUMENT TYPES & FUNCTIONS
// ============================================================

export type DocumentType =
  | 'RNC'
  | 'BusinessLicense'
  | 'IdentificationCard'
  | 'ProofOfAddress'
  | 'ElectricityBill'
  | 'BankStatement'
  | 'TaxCertificate'
  | 'IncorporationDocs'
  | 'PowerOfAttorney'
  | 'PreviousSalesRecords'
  | 'InsurancePolicy'
  | 'Other';

export type DocumentVerificationStatus =
  | 'Pending'
  | 'UnderReview'
  | 'Approved'
  | 'Rejected'
  | 'Expired';

export interface DealerDocumentDto {
  id: string;
  dealerId: string;
  type: DocumentType;
  fileName: string;
  fileUrl: string;
  fileSizeBytes: number;
  mimeType: string;
  verificationStatus: DocumentVerificationStatus;
  verifiedAt?: string;
  rejectionReason?: string;
  expiryDate?: string;
  isExpired: boolean;
  uploadedAt: string;
}

export interface UploadDocumentRequest {
  type: DocumentType;
  file: File;
  expiryDate?: string;
}

// Document type labels in Spanish
export const DOCUMENT_TYPE_LABELS: Record<DocumentType, string> = {
  RNC: 'Registro Nacional del Contribuyente (RNC)',
  BusinessLicense: 'Licencia Comercial',
  IdentificationCard: 'Cédula del Representante Legal',
  ProofOfAddress: 'Comprobante de Domicilio',
  ElectricityBill: 'Factura de Luz (validación de domicilio)',
  BankStatement: 'Certificación Bancaria',
  TaxCertificate: 'Certificación Tributaria',
  IncorporationDocs: 'Acta Constitutiva',
  PowerOfAttorney: 'Poder Notarial',
  PreviousSalesRecords: 'Récords de Ventas Anteriores',
  InsurancePolicy: 'Póliza de Seguro',
  Other: 'Otro Documento',
};

// Required documents for verification (minimal — only what's legally needed)
export const REQUIRED_DOCUMENT_TYPES: DocumentType[] = ['IdentificationCard'];

// Optional documents (boost dealer trust profile)
export const OPTIONAL_DOCUMENT_TYPES: DocumentType[] = [
  'ElectricityBill',
  'BusinessLicense',
  'IncorporationDocs',
];

/**
 * Get dealer documents
 */
export async function getDealerDocuments(dealerId: string): Promise<DealerDocumentDto[]> {
  const response = await apiClient.get<DealerDocumentDto[]>(`/api/dealers/${dealerId}/documents`);
  return response.data;
}

/**
 * Upload dealer document
 */
export async function uploadDealerDocument(
  dealerId: string,
  request: UploadDocumentRequest
): Promise<DealerDocumentDto> {
  const formData = new FormData();
  formData.append('file', request.file);
  formData.append('type', request.type);
  if (request.expiryDate) {
    formData.append('expiryDate', request.expiryDate);
  }

  const response = await apiClient.post<DealerDocumentDto>(
    `/api/dealers/${dealerId}/documents`,
    formData,
    {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    }
  );
  return response.data;
}

/**
 * Delete dealer document
 */
export async function deleteDealerDocument(dealerId: string, documentId: string): Promise<void> {
  await apiClient.delete(`/api/dealers/${dealerId}/documents/${documentId}`);
}

/**
 * Get document download URL
 */
export async function getDocumentDownloadUrl(
  dealerId: string,
  documentId: string
): Promise<string> {
  const response = await apiClient.get<{ url: string }>(
    `/api/dealers/${dealerId}/documents/${documentId}/download`
  );
  return response.data.url;
}

// ============================================================
// SUBSCRIPTION / PLAN FUNCTIONS
// ============================================================

export interface PlanInfo {
  plan: DealerPlan;
  name: string;
  price: number;
  maxListings: number;
  features: string[];
  isPopular?: boolean;
}

// Default DEALER_PLANS (updated dynamically via updateDealerPlansWithPricing)
export const DEALER_PLANS: PlanInfo[] = [
  {
    plan: 'starter',
    name: 'Starter',
    price: 2899,
    maxListings: 20,
    features: [
      'Hasta 20 vehículos',
      'Dashboard básico',
      'Soporte por email',
      'Badge de verificación',
      'Perfil de dealer',
      'Estadísticas básicas',
    ],
  },
  {
    plan: 'pro',
    name: 'Pro',
    price: 7499,
    maxListings: 75,
    isPopular: true,
    features: [
      'Hasta 75 vehículos',
      'Dashboard avanzado',
      'Soporte prioritario',
      'Badge de verificación',
      'Perfil destacado',
      'Estadísticas avanzadas',
      'CRM integrado',
      'Importación CSV',
    ],
  },
  {
    plan: 'enterprise',
    name: 'Enterprise',
    price: 17499,
    maxListings: -1, // unlimited
    features: [
      'Vehículos ilimitados',
      'Dashboard premium',
      'Soporte 24/7',
      'Badge premium',
      'Perfil destacado',
      'Analytics completo',
      'CRM avanzado',
      'API access',
      'Múltiples ubicaciones',
    ],
  },
];

/**
 * Update DEALER_PLANS with dynamic pricing from ConfigurationService
 */
export function updateDealerPlansWithPricing(pricing: {
  dealerStarter: number;
  dealerPro: number;
  dealerEnterprise: number;
  starterMaxVehicles: number;
  proMaxVehicles: number;
  earlyBirdDiscount: number;
  earlyBirdDeadline: string;
  earlyBirdFreeMonths: number;
}): void {
  // Update Starter
  DEALER_PLANS[0].price = pricing.dealerStarter;
  DEALER_PLANS[0].maxListings = pricing.starterMaxVehicles;
  DEALER_PLANS[0].features[0] = `Hasta ${pricing.starterMaxVehicles} vehículos`;
  // Update Pro
  DEALER_PLANS[1].price = pricing.dealerPro;
  DEALER_PLANS[1].maxListings = pricing.proMaxVehicles;
  DEALER_PLANS[1].features[0] = `Hasta ${pricing.proMaxVehicles} vehículos`;
  // Update Enterprise
  DEALER_PLANS[2].price = pricing.dealerEnterprise;

  // Update early bird config
  _earlyBirdDiscount = pricing.earlyBirdDiscount;
  _earlyBirdDeadline = pricing.earlyBirdDeadline;
  _earlyBirdFreeMonths = pricing.earlyBirdFreeMonths;
}

/**
 * Get plan info
 */
export function getPlanInfo(plan: DealerPlan): PlanInfo | undefined {
  return DEALER_PLANS.find(p => p.plan === plan);
}

// Early bird configuration (updated from ConfigurationService)
let _earlyBirdDiscount = 25;
let _earlyBirdDeadline = '2026-12-31';
let _earlyBirdFreeMonths = 2;

/**
 * Check if early bird offer is active
 */
export function isEarlyBirdActive(): boolean {
  const deadline = new Date(`${_earlyBirdDeadline}T23:59:59`);
  return new Date() < deadline;
}

/**
 * Calculate early bird price (dynamic discount %)
 */
export function getEarlyBirdPrice(regularPrice: number): number {
  return Math.round(regularPrice * (1 - _earlyBirdDiscount / 100));
}

/**
 * Get days remaining for early bird
 */
export function getEarlyBirdDaysRemaining(): number {
  const deadline = new Date(`${_earlyBirdDeadline}T23:59:59`);
  const now = new Date();
  const diff = deadline.getTime() - now.getTime();
  return Math.max(0, Math.ceil(diff / (1000 * 60 * 60 * 24)));
}

/**
 * Get early bird free months
 */
export function getEarlyBirdFreeMonths(): number {
  return _earlyBirdFreeMonths;
}

/**
 * Get early bird discount percentage
 */
export function getEarlyBirdDiscount(): number {
  return _earlyBirdDiscount;
}

// ============================================================
// SERVICE EXPORT
// ============================================================

export const dealerService = {
  getDealers,
  getDealerById,
  getDealerBySlug,
  getDealerByUserId,
  getCurrentDealer,
  createDealer,
  updateDealer,
  getDealerStats,
  getDealerLocations,
  addDealerLocation,
  updateDealerLocation,
  deleteDealerLocation,
  getDealerDocuments,
  uploadDealerDocument,
  deleteDealerDocument,
  getDocumentDownloadUrl,
  getPlanInfo,
  isEarlyBirdActive,
  getEarlyBirdPrice,
  getEarlyBirdDaysRemaining,
  getEarlyBirdFreeMonths,
  getEarlyBirdDiscount,
  updateDealerPlansWithPricing,
  DEALER_PLANS,
  DOCUMENT_TYPE_LABELS,
  REQUIRED_DOCUMENT_TYPES,
  OPTIONAL_DOCUMENT_TYPES,
};

export default dealerService;
