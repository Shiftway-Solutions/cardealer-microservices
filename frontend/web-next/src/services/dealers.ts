/**
 * Dealers Service - API client for dealer operations
 * Connects via API Gateway to DealerManagementService
 */

import { apiClient } from '@/lib/api-client';
import {
  dealerAnalyticsService,
  type DealerSnapshot,
  type KpiSummary,
} from '@/services/dealer-analytics';
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
  slug?: string;
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

function mapAnalyticsToDealerStats(
  dealer: Dealer,
  kpis: KpiSummary,
  inventoryStats: DealerSnapshot
): DealerStatsDto {
  return {
    totalListings:
      inventoryStats.totalVehicles ?? dealer.currentActiveListings ?? kpis.activeListings,
    activeListings:
      kpis.activeListings ?? inventoryStats.activeVehicles ?? dealer.currentActiveListings,
    totalViews: kpis.totalViews ?? inventoryStats.totalViews,
    viewsThisMonth: kpis.totalViews ?? inventoryStats.totalViews,
    viewsChange: kpis.viewsChange ?? 0,
    totalInquiries: kpis.totalContacts ?? inventoryStats.totalContacts,
    inquiriesThisMonth: kpis.totalContacts ?? inventoryStats.totalContacts,
    inquiriesChange: kpis.contactsChange ?? 0,
    pendingInquiries: inventoryStats.newLeads ?? 0,
    responseRate: dealer.responseRate ?? kpis.conversionRate ?? 0,
    avgResponseTimeMinutes: dealer.avgResponseTimeMinutes ?? kpis.avgResponseTime ?? 0,
    totalRevenue: kpis.totalRevenue ?? inventoryStats.totalRevenue,
    revenueThisMonth: kpis.totalRevenue ?? inventoryStats.totalRevenue,
    revenueChange: kpis.revenueChange ?? 0,
  };
}

// ============================================================
// TRANSFORM FUNCTIONS
// ============================================================

export function transformDealer(dto: DealerDto): Dealer {
  return {
    id: dto.id,
    userId: dto.userId,
    slug: dto.slug,
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
    const response = await apiClient.get<DealerDto>(`/api/dealers/owner/${userId}`);
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
 * Falls back gracefully when DealerAnalyticsService (--profile ai) is not running.
 */
export async function getDealerStats(dealerId: string): Promise<DealerStatsDto> {
  const dealer = await getDealerById(dealerId);

  // Analytics service may not be running in all environments (requires --profile ai).
  // Resolve KPIs and inventory stats independently so partial data is usable.
  const [kpis, inventoryStats] = await Promise.all([
    dealerAnalyticsService.getKpis(dealerId).catch(() => null),
    dealerAnalyticsService.getInventoryStats(dealerId).catch(() => null),
  ]);

  if (!kpis && !inventoryStats) {
    // Analytics service unavailable — return minimal stats from dealer profile
    return {
      totalListings: dealer.currentActiveListings ?? 0,
      activeListings: dealer.currentActiveListings ?? 0,
      totalViews: 0,
      viewsThisMonth: 0,
      viewsChange: 0,
      totalInquiries: 0,
      inquiriesThisMonth: 0,
      inquiriesChange: 0,
      pendingInquiries: 0,
      responseRate: dealer.responseRate ?? 0,
      avgResponseTimeMinutes: dealer.avgResponseTimeMinutes ?? 0,
      totalRevenue: 0,
      revenueThisMonth: 0,
      revenueChange: 0,
    };
  }

  return mapAnalyticsToDealerStats(
    dealer,
    kpis ?? ({} as KpiSummary),
    inventoryStats ?? ({} as DealerSnapshot)
  );
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
    plan: 'libre',
    name: 'LIBRE',
    price: 0,
    maxListings: -1, // unlimited
    features: [
      'Vehículos ilimitados',
      'Hasta 5 fotos por vehículo',
      'Posición estándar en búsquedas',
      '1 valoración IA gratis',
      'Panel de control básico',
      'Perfil de dealer',
    ],
  },
  {
    plan: 'visible',
    name: 'VISIBLE',
    price: 1699,
    maxListings: -1, // unlimited
    features: [
      'Vehículos ilimitados',
      'Hasta 10 fotos por vehículo',
      'Prioridad media en búsquedas',
      '3 vehículos destacados/mes',
      '$15 en OKLA Coins/mes',
      'Badge Dealer Verificado OKLA',
      'Dashboard Analytics básico',
      '5 valoraciones IA/mes',
      'Soporte email',
    ],
  },
  {
    plan: 'starter',
    name: 'STARTER',
    price: 3499,
    maxListings: -1, // unlimited
    features: [
      'Vehículos ilimitados',
      'Hasta 12 fotos por vehículo',
      'Alta prioridad en búsquedas',
      '5 vehículos destacados/mes',
      '$30 en OKLA Coins/mes',
      'Badge Dealer Verificado+ OKLA',
      'ChatAgent Web 100 conv/mes',
      'ChatAgent WhatsApp 100 conv/mes',
      '10 valoraciones IA/mes',
      'Dashboard Analytics básico+',
      'Soporte email 48h',
    ],
  },
  {
    plan: 'pro',
    name: 'PRO',
    price: 5799,
    maxListings: -1, // unlimited
    isPopular: true,
    features: [
      'Vehículos ilimitados',
      'Hasta 15 fotos por vehículo',
      'Alta prioridad en búsquedas',
      '10 vehículos destacados/mes',
      '$45 en OKLA Coins/mes',
      'Badge Dealer Verificado Dorado',
      'ChatAgent Web 300 conv/mes',
      'ChatAgent WhatsApp 300 conv/mes',
      'Agendamiento de citas automático',
      'Valoración IA ilimitada',
      'Dashboard Analytics avanzado',
      'Soporte email prioritario',
    ],
  },
  {
    plan: 'elite',
    name: 'ÉLITE',
    price: 20299,
    maxListings: -1, // unlimited
    features: [
      'Vehículos ilimitados',
      'Hasta 20 fotos + video tour',
      'Top prioridad en búsquedas',
      '25 vehículos destacados/mes',
      '$120 en OKLA Coins/mes',
      'Badge Dealer Verificado Premium',
      'ChatAgent Web 5,000 conv/mes',
      'ChatAgent WhatsApp 5,000 conv/mes',
      'Agendamiento + recordatorios WA',
      'Valoración IA ilimitada + informe PDF',
      'Dashboard Analytics completo + exportar',
      'Gerente de cuenta dedicado',
    ],
  },
  {
    plan: 'enterprise',
    name: 'ENTERPRISE',
    price: 34999,
    maxListings: -1, // unlimited
    features: [
      'Vehículos ilimitados',
      'Hasta 20 fotos + video tour',
      'Posición #1 garantizada en búsquedas',
      '50 vehículos destacados/mes',
      '$300 en OKLA Coins/mes',
      'Badge Dealer Enterprise 👑',
      'ChatAgent Web SIN LÍMITE',
      'ChatAgent WhatsApp SIN LÍMITE',
      'Agendamiento + CRM + recordatorios WA',
      'Valoración IA ilimitada + PDF + API',
      'Dashboard completo + API + reportes custom',
      'Acceso completo a API OKLA',
      'Manager dedicado + SLA garantizado',
    ],
  },
];

/**
 * Update DEALER_PLANS with dynamic pricing from ConfigurationService
 */
export function updateDealerPlansWithPricing(pricing: {
  dealerLibre: number;
  dealerVisible: number;
  dealerStarter?: number;
  dealerPro: number;
  dealerElite: number;
  dealerEnterprise?: number;
  earlyBirdDiscount: number;
  earlyBirdDeadline: string;
  earlyBirdFreeMonths: number;
}): void {
  // Update LIBRE
  DEALER_PLANS[0].price = pricing.dealerLibre;
  // Update VISIBLE
  DEALER_PLANS[1].price = pricing.dealerVisible;
  // Update STARTER
  DEALER_PLANS[2].price = pricing.dealerStarter ?? 3422;
  // Update PRO
  DEALER_PLANS[3].price = pricing.dealerPro;
  // Update ÉLITE
  DEALER_PLANS[4].price = pricing.dealerElite;
  // Update ENTERPRISE
  DEALER_PLANS[5].price = pricing.dealerEnterprise ?? 34742;

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
