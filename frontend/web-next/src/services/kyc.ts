/**
 * KYC Service - API client for Know Your Customer verification
 * Connects via API Gateway to KYCService
 */

import { apiClient } from '@/lib/api-client';
import { authTokens } from '@/lib/api-client';
import { getApiBaseUrl } from '@/lib/api-url';

// Server Actions — KYC mutations run on the server, invisible to browser DevTools
import {
  serverCreateKYCProfile,
  serverUpdateKYCProfile,
  serverSubmitKYCForReview,
  serverUploadKYCDocument,
  serverDeleteKYCDocument,
  serverProcessIdentityVerification,
  serverApproveKYCProfile,
  serverRejectKYCProfile,
  serverUpsertKYCDraft,
} from '@/actions/kyc';

// =============================================================================
// ENUMS (matching backend KYCService.Domain.Entities)
// =============================================================================

export enum KYCStatus {
  Pending = 1,
  InProgress = 2,
  DocumentsRequired = 3,
  UnderReview = 4,
  Approved = 5,
  Rejected = 6,
  Expired = 7,
  Suspended = 8,
  // Aliases
  NotStarted = 0,
  // Note: PendingReview removed - use UnderReview instead (both = 4)
}

export enum RiskLevel {
  Low = 1,
  Medium = 2,
  High = 3,
  VeryHigh = 4,
  Prohibited = 5,
}

export enum DocumentType {
  // Identity Documents
  Cedula = 1,
  Passport = 2,
  DriverLicense = 3,
  ResidencyCard = 4,
  RNC = 5,
  Other = 99,
  // Proof of Address
  UtilityBill = 10,
  BankStatement = 11,
  LeaseAgreement = 12,
  // Business Documents
  MercantileRegistry = 21,
  BusinessLicense = 22,
  TaxCertificate = 23,
  // Financial
  IncomeProof = 30,
  TaxReturn = 31,
  // Selfie/Verification
  Selfie = 40,
  SelfieWithDocument = 41,
}

export enum EntityType {
  Individual = 1,
  Business = 2,
  NonProfit = 3,
  Government = 4,
}

// =============================================================================
// TYPES
// =============================================================================

export interface KYCProfile {
  id: string;
  userId: string;
  entityType: EntityType;
  status: KYCStatus;
  statusName: string;
  riskLevel: RiskLevel;
  riskLevelName: string;
  riskScore: number;
  riskFactors: string[];

  // Personal info
  fullName: string;
  firstName?: string;
  middleName?: string | null;
  lastName?: string;
  dateOfBirth: string;
  placeOfBirth?: string | null;
  nationality: string;
  gender?: string | null;

  // Document
  primaryDocumentType: DocumentType;
  primaryDocumentNumber?: string | null;
  primaryDocumentExpiry?: string | null;

  // Contact
  email?: string | null;
  phone?: string | null;
  phoneNumber?: string; // Alias

  // Address
  address: string;
  sector?: string | null;
  city: string;
  province: string;
  postalCode?: string | null;
  country?: string;

  // Additional info
  occupation?: string;
  sourceOfFunds?: string;
  expectedMonthlyTransaction?: number;

  // PEP
  isPEP: boolean;
  pepPosition?: string | null;

  // Business (if applicable)
  businessName?: string | null;
  rnc?: string | null;

  // Verification status
  isIdentityVerified: boolean;
  isAddressVerified: boolean;
  identityVerifiedAt?: string | null;
  addressVerifiedAt?: string | null;

  // Timestamps
  createdAt: string;
  approvedAt?: string | null;
  approvedBy?: string | null;
  approvedByName?: string | null;
  expiresAt?: string | null;
  nextReviewAt?: string | null;
  rejectedAt?: string | null;
  rejectionReason?: string | null;
  rejectedBy?: string | null;
  rejectedByName?: string | null;

  // Relations
  documents: KYCDocument[];
  verifications: KYCVerification[];
}

export interface KYCDocument {
  id: string;
  kycProfileId: string;
  type: DocumentType;
  typeName?: string;
  documentName: string;
  fileName: string;
  storageKey?: string;
  fileUrl: string;
  fileType: string;
  fileSize: number;
  side?: 'Front' | 'Back' | null;
  status: number;
  statusName: string;
  rejectionReason?: string | null;
  extractedNumber?: string | null;
  extractedExpiry?: string | null;
  extractedName?: string | null;
  uploadedAt: string;
  verifiedAt?: string | null;
}

export interface DocumentUrlResponse {
  documentId: string;
  url: string;
  expiresAt: string;
}

export interface KYCVerification {
  id: string;
  kycProfileId?: string;
  verificationType: string; // Identity, Address, FaceMatch, Liveness, DocumentOCR
  type?: string; // Alias for backwards compat
  provider: string; // AmazonRekognition, Simulation, Internal
  passed: boolean;
  failureReason?: string | null;
  rawResponse?: string | null;
  confidenceScore: number; // 0-100
  status?: string;
  result?: object;
  verifiedAt?: string | null;
  verifiedBy?: string | null;
  expiresAt?: string | null;
}

/**
 * Biometric verification scores extracted from KYCVerification array
 */
export interface BiometricScores {
  faceMatch: {
    score: number;
    passed: boolean;
    provider: string;
    verifiedAt?: string | null;
  } | null;
  liveness: { score: number; passed: boolean; provider: string; verifiedAt?: string | null } | null;
  documentOcr: {
    score: number;
    passed: boolean;
    provider: string;
    verifiedAt?: string | null;
  } | null;
  overallScore: number | null;
  hasResults: boolean;
}

/**
 * Extract biometric scores from KYCVerification array
 */
export function extractBiometricScores(verifications: KYCVerification[]): BiometricScores {
  const faceMatchV = verifications.find(
    v =>
      (v.verificationType || v.type || '').toLowerCase().includes('face') ||
      (v.verificationType || v.type || '').toLowerCase().includes('match')
  );
  const livenessV = verifications.find(
    v =>
      (v.verificationType || v.type || '').toLowerCase().includes('liveness') ||
      (v.verificationType || v.type || '').toLowerCase().includes('live')
  );
  const ocrV = verifications.find(
    v =>
      (v.verificationType || v.type || '').toLowerCase().includes('ocr') ||
      (v.verificationType || v.type || '').toLowerCase().includes('document')
  );

  const faceMatch = faceMatchV
    ? {
        score: faceMatchV.confidenceScore,
        passed: faceMatchV.passed,
        provider: faceMatchV.provider,
        verifiedAt: faceMatchV.verifiedAt,
      }
    : null;

  const liveness = livenessV
    ? {
        score: livenessV.confidenceScore,
        passed: livenessV.passed,
        provider: livenessV.provider,
        verifiedAt: livenessV.verifiedAt,
      }
    : null;

  const documentOcr = ocrV
    ? {
        score: ocrV.confidenceScore,
        passed: ocrV.passed,
        provider: ocrV.provider,
        verifiedAt: ocrV.verifiedAt,
      }
    : null;

  const scores = [faceMatch?.score, liveness?.score, documentOcr?.score].filter(
    (s): s is number => s != null
  );
  const overallScore =
    scores.length > 0 ? Math.round(scores.reduce((a, b) => a + b, 0) / scores.length) : null;

  return {
    faceMatch,
    liveness,
    documentOcr,
    overallScore,
    hasResults: faceMatch !== null || liveness !== null || documentOcr !== null,
  };
}

export interface CreateKYCProfileRequest {
  userId: string;
  email?: string;
  firstName: string;
  lastName: string;
  documentNumber: string;
  documentType: DocumentType;
  dateOfBirth: string;
  nationality: string;
  address: string;
  city: string;
  province: string;
  phoneNumber: string;
  sourceOfFunds?: string;
  occupation?: string;
  expectedMonthlyTransaction?: number;
}

export interface UpdateKYCProfileRequest extends Partial<CreateKYCProfileRequest> {
  id: string;
}

export interface UploadDocumentRequest {
  profileId: string;
  documentType: DocumentType;
  file: File;
  side?: 'Front' | 'Back';
}

export interface KYCProfileSummary {
  id: string;
  userId: string;
  fullName: string;
  documentNumber?: string;
  entityType?: number;
  status: KYCStatus;
  statusName: string;
  riskLevel: RiskLevel;
  riskLevelName?: string;
  isPEP: boolean;
  createdAt: string;
  expiresAt?: string | null;
  documentsCount: number;
  pendingDocuments: number;
  hasPendingDocuments?: boolean;
}

export interface PaginatedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface LivenessChallenge {
  type: string;
  passed: boolean;
  timestamp: string;
  confidence?: number;
}

export interface LivenessData {
  challenges: LivenessChallenge[];
  videoFrames?: string[];
  deviceGyroscope?: { x: number; y: number; z: number }[];
  selfieImage?: string;
}

export interface DocumentData {
  frontImage: string;
  backImage?: string;
  documentType?: DocumentType;
  extractedData?: {
    documentNumber?: string;
    firstName?: string;
    lastName?: string;
    dateOfBirth?: string;
    expirationDate?: string;
  };
}

// =============================================================================
// API SERVICE
// =============================================================================

const KYC_BASE_URL = '/api/kyc';

/**
 * Get KYC profile by user ID
 * Returns null if profile doesn't exist (404) - this is expected for new users
 */
export async function getKYCProfileByUserId(userId: string): Promise<KYCProfile | null> {
  try {
    // Use fetch directly to avoid axios interceptors logging 404s
    // BFF pattern: getApiBaseUrl() returns internal URL on server, relative URL on client
    const baseUrl = getApiBaseUrl();
    // Security: Use cookie-based auth or retrieve token from authTokens helper instead of raw localStorage
    const { authTokens } = await import('@/lib/api-client');
    const token = typeof window !== 'undefined' ? authTokens.getAccessToken() : null;

    const response = await fetch(`${baseUrl}${KYC_BASE_URL}/kycprofiles/user/${userId}`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
    });

    // Return null for 404 (no profile yet - expected for new users)
    if (response.status === 404) {
      // This is expected for new users - don't log as error
      return null;
    }

    if (!response.ok) {
      throw new Error(`HTTP error ${response.status}`);
    }

    return await response.json();
  } catch (error: unknown) {
    // Only log and throw for unexpected errors (network issues, 500s, etc.)
    console.error('Unexpected error fetching KYC profile:', error);
    throw error;
  }
}

/**
 * Get KYC profile by ID
 */
export async function getKYCProfileById(id: string): Promise<KYCProfile> {
  const response = await apiClient.get<KYCProfile>(`${KYC_BASE_URL}/kycprofiles/${id}`);
  return response.data;
}

/**
 * Create new KYC profile with idempotency key
 */
export async function createKYCProfile(data: CreateKYCProfileRequest): Promise<KYCProfile> {
  // ── Server Action: KYC profile creation server-side, personal data invisible to browser ──
  const result = await serverCreateKYCProfile({
    userId: data.userId,
    email: data.email,
    firstName: data.firstName,
    lastName: data.lastName,
    documentNumber: data.documentNumber,
    documentType: data.documentType,
    dateOfBirth: data.dateOfBirth,
    nationality: data.nationality,
    address: data.address,
    city: data.city,
    province: data.province,
    phoneNumber: data.phoneNumber,
    sourceOfFunds: data.sourceOfFunds,
    occupation: data.occupation,
    expectedMonthlyTransaction: data.expectedMonthlyTransaction,
  });

  if (!result.success || !result.data) {
    throw new Error(result.error || 'Error al crear el perfil KYC');
  }

  return result.data as unknown as KYCProfile;
}

/**
 * Update KYC profile
 */
export async function updateKYCProfile(
  id: string,
  data: Partial<CreateKYCProfileRequest>
): Promise<KYCProfile> {
  // ── Server Action: KYC profile update server-side ──
  const result = await serverUpdateKYCProfile(id, {
    firstName: data.firstName,
    lastName: data.lastName,
    documentNumber: data.documentNumber,
    documentType: data.documentType,
    dateOfBirth: data.dateOfBirth,
    nationality: data.nationality,
    address: data.address,
    city: data.city,
    province: data.province,
    phoneNumber: data.phoneNumber,
    sourceOfFunds: data.sourceOfFunds,
    occupation: data.occupation,
    expectedMonthlyTransaction: data.expectedMonthlyTransaction,
  });

  if (!result.success || !result.data) {
    throw new Error(result.error || 'Error al actualizar el perfil KYC');
  }

  return result.data as unknown as KYCProfile;
}

/**
 * Upload document to KYC profile
 */
export async function uploadKYCDocument(request: UploadDocumentRequest): Promise<KYCDocument> {
  // ── Server Action: document upload server-side, file data & storage keys invisible ──

  // Convert File to base64 for Server Action (Files cannot be passed directly)
  const fileBase64 = await new Promise<string>((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      const result = reader.result as string;
      resolve(result.split(',')[1]); // Remove "data:...;base64," prefix
    };
    reader.onerror = reject;
    reader.readAsDataURL(request.file);
  });

  const result = await serverUploadKYCDocument(
    request.profileId,
    request.documentType,
    fileBase64,
    request.file.name,
    request.file.type,
    request.side
  );

  if (!result.success || !result.data) {
    throw new Error(result.error || 'Error al cargar el documento');
  }

  return result.data as unknown as KYCDocument;
}

/**
 * Get documents for a KYC profile
 */
export async function getKYCDocuments(profileId: string): Promise<KYCDocument[]> {
  const response = await apiClient.get<KYCDocument[]>(
    `${KYC_BASE_URL}/profiles/${profileId}/documents`
  );
  return response.data;
}

/**
 * Delete a KYC document
 */
export async function deleteKYCDocument(documentId: string): Promise<void> {
  // ── Server Action: document deletion server-side ──
  const result = await serverDeleteKYCDocument(documentId);
  if (!result.success) {
    throw new Error(result.error || 'Error al eliminar el documento');
  }
}

/**
 * Submit profile for review
 */
export async function submitKYCForReview(profileId: string): Promise<KYCProfile> {
  // ── Server Action: submission processed server-side ──
  const accessToken = authTokens.getAccessToken();
  const result = await serverSubmitKYCForReview(profileId);

  if (!result.success || !result.data) {
    throw new Error(result.error || 'Error al enviar el perfil para revisión');
  }

  return result.data as unknown as KYCProfile;
}

/**
 * Process identity verification (face comparison)
 */
export async function processIdentityVerification(request: {
  profileId: string;
  selfieFile: File;
  livenessData?: LivenessData;
}): Promise<{ success: boolean; matchScore: number; passed: boolean }> {
  // ── Server Action: biometric verification server-side, scores invisible to browser ──
  const accessToken = authTokens.getAccessToken();

  const formData = new FormData();
  formData.append('profileId', request.profileId);
  formData.append('selfie', request.selfieFile);
  if (request.livenessData) {
    formData.append('livenessData', JSON.stringify(request.livenessData));
  }

  const result = await serverProcessIdentityVerification(formData);

  if (!result.success || !result.data) {
    throw new Error(result.error || 'Error en la verificación de identidad');
  }

  return result.data;
}

/**
 * Get required documents based on user type
 */
export function getRequiredDocuments(userType: 'seller' | 'dealer' = 'seller'): DocumentType[] {
  const baseDocuments = [
    DocumentType.Cedula, // Front
    DocumentType.Selfie,
  ];

  if (userType === 'dealer') {
    return [...baseDocuments, DocumentType.RNC, DocumentType.MercantileRegistry];
  }

  return baseDocuments;
}

/**
 * Get status label in Spanish
 */
export function getStatusLabel(status: KYCStatus): string {
  const labels: Partial<Record<KYCStatus, string>> = {
    [KYCStatus.NotStarted]: 'No iniciado',
    [KYCStatus.Pending]: 'Pendiente',
    [KYCStatus.InProgress]: 'En progreso',
    [KYCStatus.DocumentsRequired]: 'Documentos requeridos',
    [KYCStatus.UnderReview]: 'En revisión',
    [KYCStatus.Approved]: 'Aprobado',
    [KYCStatus.Rejected]: 'Rechazado',
    [KYCStatus.Expired]: 'Expirado',
    [KYCStatus.Suspended]: 'Suspendido',
  };
  return labels[status] || 'Desconocido';
}

/**
 * Get document type label in Spanish
 */
export function getDocumentTypeLabel(type: DocumentType): string {
  const labels: Record<DocumentType, string> = {
    [DocumentType.Cedula]: 'Cédula de Identidad',
    [DocumentType.Passport]: 'Pasaporte',
    [DocumentType.DriverLicense]: 'Licencia de Conducir',
    [DocumentType.ResidencyCard]: 'Tarjeta de Residencia',
    [DocumentType.RNC]: 'RNC',
    [DocumentType.Other]: 'Otro',
    [DocumentType.UtilityBill]: 'Factura de Servicios',
    [DocumentType.BankStatement]: 'Estado de Cuenta',
    [DocumentType.LeaseAgreement]: 'Contrato de Alquiler',
    [DocumentType.MercantileRegistry]: 'Registro Mercantil',
    [DocumentType.BusinessLicense]: 'Licencia Comercial',
    [DocumentType.TaxCertificate]: 'Certificado de Impuestos',
    [DocumentType.IncomeProof]: 'Comprobante de Ingresos',
    [DocumentType.TaxReturn]: 'Declaración de Impuestos',
    [DocumentType.Selfie]: 'Selfie',
    [DocumentType.SelfieWithDocument]: 'Selfie con Documento',
  };
  return labels[type] || 'Documento';
}

/**
 * Check if user is verified for selling
 */
export function isVerifiedForSelling(profile: KYCProfile | null): boolean {
  if (!profile) return false;
  return profile.status === KYCStatus.Approved && profile.isIdentityVerified;
}

// =============================================================================
// ADMIN FUNCTIONS
// =============================================================================

export interface KYCStatistics {
  totalProfiles: number;
  pendingProfiles: number;
  approvedProfiles: number;
  rejectedProfiles: number;
  inProgressProfiles: number;
  inReviewProfiles?: number;
  avgProcessingTimeDays?: number;
  todayApproved?: number;
  todayRejected?: number;
  expiredProfiles?: number;
  highRiskProfiles?: number;
  pepProfiles?: number;
  expiringIn30Days?: number;
  approvalRate?: number;
  highRiskPercentage?: number;
}

export interface AdminKYCProfilesParams {
  page?: number;
  pageSize?: number;
  status?: KYCStatus | null;
  riskLevel?: RiskLevel | null;
  isPEP?: boolean | null;
  search?: string;
}

/**
 * Get all KYC profiles (Admin only)
 */
export async function getAdminKYCProfiles(
  params: AdminKYCProfilesParams = {}
): Promise<PaginatedResult<KYCProfileSummary>> {
  const queryParams = new URLSearchParams();
  if (params.page) queryParams.append('page', params.page.toString());
  if (params.pageSize) queryParams.append('pageSize', params.pageSize.toString());
  if (params.status !== null && params.status !== undefined) {
    queryParams.append('status', params.status.toString());
  }
  if (params.riskLevel !== null && params.riskLevel !== undefined) {
    queryParams.append('riskLevel', params.riskLevel.toString());
  }
  if (params.isPEP !== null && params.isPEP !== undefined) {
    queryParams.append('isPEP', params.isPEP.toString());
  }

  const response = await apiClient.get<PaginatedResult<KYCProfileSummary>>(
    `${KYC_BASE_URL}/kycprofiles?${queryParams.toString()}`
  );
  return response.data;
}

/**
 * Get pending KYC profiles (Admin only)
 */
export async function getPendingKYCProfiles(
  page: number = 1,
  pageSize: number = 20
): Promise<PaginatedResult<KYCProfileSummary>> {
  const response = await apiClient.get<PaginatedResult<KYCProfileSummary>>(
    `${KYC_BASE_URL}/kycprofiles/pending?page=${page}&pageSize=${pageSize}`
  );
  return response.data;
}

/**
 * Get expiring KYC profiles (Admin only)
 */
export async function getExpiringKYCProfiles(
  daysUntilExpiry: number = 30,
  page: number = 1,
  pageSize: number = 20
): Promise<PaginatedResult<KYCProfileSummary>> {
  const response = await apiClient.get<PaginatedResult<KYCProfileSummary>>(
    `${KYC_BASE_URL}/kycprofiles/expiring?daysUntilExpiry=${daysUntilExpiry}&page=${page}&pageSize=${pageSize}`
  );
  return response.data;
}

/**
 * Get KYC statistics (Admin only)
 */
export async function getKYCStatistics(): Promise<KYCStatistics> {
  const response = await apiClient.get<KYCStatistics>(`${KYC_BASE_URL}/kycprofiles/statistics`);
  return response.data;
}

/**
 * Approve KYC profile (Admin only)
 */
export async function approveKYCProfile(
  profileId: string,
  approvedBy: string,
  approvedByName: string,
  notes?: string
): Promise<KYCProfile> {
  // ── Server Action: admin approval processed server-side ──
  const result = await serverApproveKYCProfile(profileId, approvedBy, approvedByName, notes);

  if (!result.success || !result.data) {
    throw new Error(result.error || 'Error al aprobar el perfil KYC');
  }

  return result.data as unknown as KYCProfile;
}

/**
 * Reject KYC profile (Admin only)
 */
export async function rejectKYCProfile(
  profileId: string,
  rejectedBy: string,
  rejectedByName: string,
  rejectionReason: string,
  notes?: string
): Promise<KYCProfile> {
  // ── Server Action: admin rejection processed server-side ──
  const result = await serverRejectKYCProfile(
    profileId,
    rejectedBy,
    rejectedByName,
    rejectionReason,
    notes
  );

  if (!result.success || !result.data) {
    throw new Error(result.error || 'Error al rechazar el perfil KYC');
  }

  return result.data as unknown as KYCProfile;
}

/**
 * Get a fresh pre-signed URL for a KYC document
 * Use this instead of the stored fileUrl which may be expired
 */
export async function getDocumentFreshUrl(documentId: string): Promise<DocumentUrlResponse> {
  const response = await apiClient.get<DocumentUrlResponse>(
    `${KYC_BASE_URL}/documents/${documentId}/url`
  );
  return response.data;
}

// =============================================================================
// KYC PROFILE DRAFTS (wizard autosave)
// =============================================================================

export interface KYCProfileDraft {
  id: string;
  userId: string;
  currentStep: number;
  formData: string; // JSON string
  createdAt: string;
  updatedAt: string;
  expiresAt: string;
}

export interface UpsertDraftRequest {
  currentStep: number;
  formData: string; // JSON string with all wizard form data
}

/**
 * Save or update a KYC draft (upsert). Called onStep + every 30 seconds.
 */
export async function upsertKYCDraft(request: UpsertDraftRequest): Promise<KYCProfileDraft> {
  const result = await serverUpsertKYCDraft(request);

  if (!result.success || !result.data) {
    throw new Error(result.error || 'Error al guardar el borrador KYC');
  }

  return result.data as unknown as KYCProfileDraft;
}

/**
 * Load existing KYC draft for a user. Returns null if no draft.
 */
export async function getKYCDraft(userId: string): Promise<KYCProfileDraft | null> {
  try {
    const response = await apiClient.get<KYCProfileDraft>(
      `${KYC_BASE_URL}/kycprofiles/draft/${userId}`
    );
    return response.data;
  } catch {
    return null;
  }
}

/**
 * Delete the KYC draft for a user (on successful submit or manual discard).
 */
export async function deleteKYCDraft(userId: string): Promise<void> {
  await apiClient.delete(`${KYC_BASE_URL}/kycprofiles/draft/${userId}`);
}

// Export default service object for backwards compatibility
export const kycService = {
  getProfileByUserId: getKYCProfileByUserId,
  getProfileById: getKYCProfileById,
  createProfile: createKYCProfile,
  updateProfile: updateKYCProfile,
  uploadDocument: uploadKYCDocument,
  getDocuments: getKYCDocuments,
  deleteDocument: deleteKYCDocument,
  submitForReview: submitKYCForReview,
  processIdentityVerification,
  getRequiredDocuments,
  getStatusLabel,
  getDocumentTypeLabel,
  isVerifiedForSelling,
  // Document URL
  getDocumentFreshUrl,
  // Admin functions
  getAdminProfiles: getAdminKYCProfiles,
  getPendingProfiles: getPendingKYCProfiles,
  getExpiringProfiles: getExpiringKYCProfiles,
  getStatistics: getKYCStatistics,
  approveProfile: approveKYCProfile,
  rejectProfile: rejectKYCProfile,
  // Draft functions (autosave)
  upsertDraft: upsertKYCDraft,
  getDraft: getKYCDraft,
  deleteDraft: deleteKYCDraft,
};

export default kycService;
