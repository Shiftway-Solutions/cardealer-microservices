'use server';

/**
 * KYC Server Actions — Critical identity verification operations
 *
 * These run exclusively on the Next.js server. The browser only sees
 * an opaque POST — no KYC endpoints, personal data, document info,
 * or verification scores are visible in DevTools Network tab.
 *
 * Flow: Browser → Server Action (Next.js) → Gateway (internal) → KYCService
 */

import { cookies } from 'next/headers';
import { getInternalApiUrl } from '@/lib/api-url';

// =============================================================================
// COOKIE CONSTANTS (must match backend AuthController.SetAuthCookies)
// =============================================================================

const AUTH_COOKIE_NAME = 'okla_access_token';

// =============================================================================
// TYPES
// =============================================================================

export interface ActionResult<T = void> {
  success: boolean;
  data?: T;
  error?: string;
  code?: string;
}

interface KYCProfileResult {
  id: string;
  userId: string;
  status: number;
  statusName: string;
  riskLevel: number;
  fullName: string;
  createdAt: string;
  [key: string]: unknown;
}

interface KYCDocumentResult {
  id: string;
  kycProfileId: string;
  type: number;
  documentName: string;
  fileUrl: string;
  status: number;
  statusName: string;
  uploadedAt: string;
  [key: string]: unknown;
}

interface IdentityVerificationResult {
  success: boolean;
  matchScore: number;
  passed: boolean;
}

interface KYCProfileDraft {
  id: string;
  userId: string;
  currentStep: number;
  formData: string;
  createdAt: string;
  updatedAt: string;
  [key: string]: unknown;
}

// =============================================================================
// INTERNAL HELPERS
// =============================================================================

const API_URL = () => getInternalApiUrl();
const KYC_BASE = '/api/kyc';

function generateIdempotencyKey(): string {
  const timestamp = Date.now().toString(36);
  const randomPart = Math.random().toString(36).substring(2, 15);
  return `kyc_${timestamp}_${randomPart}`;
}

async function internalFetch<T>(
  path: string,
  options: {
    method?: string;
    body?: unknown;
    token?: string | null;
    headers?: Record<string, string>;
  } = {}
): Promise<T> {
  const { method = 'GET', body, token, headers: extraHeaders } = options;
  const url = `${API_URL()}${path}`;

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...extraHeaders,
  };

  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  // SECURITY: Add CSRF token from cookie for mutations (POST, PUT, PATCH, DELETE)
  // Gateway's CsrfValidationMiddleware uses Double Submit Cookie pattern:
  // it checks BOTH X-CSRF-Token header AND csrf_token cookie in the same request.
  // In server-to-server calls (Server Action → Gateway), the browser cookie is NOT
  // forwarded automatically, so we must explicitly set the Cookie header as well.
  if (method && ['POST', 'PUT', 'PATCH', 'DELETE'].includes(method.toUpperCase())) {
    const cookieStore = await cookies();
    const csrfToken = cookieStore.get('csrf_token')?.value;
    if (csrfToken) {
      headers['X-CSRF-Token'] = csrfToken;
      // Forward the csrf_token cookie so the gateway can validate both sides of the double-submit
      headers['Cookie'] = `csrf_token=${csrfToken}`;
    }
  }

  const response = await fetch(url, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
    cache: 'no-store',
  });

  if (!response.ok) {
    const errorBody = await response.json().catch(() => ({}));
    const message =
      errorBody.message || errorBody.error || errorBody.title || `Error ${response.status}`;
    throw new Error(message);
  }

  if (response.status === 204) {
    return {} as T;
  }

  return response.json();
}

async function internalFetchFormData<T>(
  path: string,
  formData: FormData,
  options: {
    token?: string | null;
    headers?: Record<string, string>;
  } = {}
): Promise<T> {
  const { token, headers: extraHeaders } = options;
  const url = `${API_URL()}${path}`;

  const headers: Record<string, string> = {
    ...extraHeaders,
  };
  // Do NOT set Content-Type for FormData — browser/fetch sets multipart boundary

  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  // SECURITY: Add CSRF token from cookie for FormData uploads
  // Same double-submit pattern: forward both header and cookie for gateway validation
  const cookieStore = await cookies();
  const csrfToken = cookieStore.get('csrf_token')?.value;
  if (csrfToken) {
    headers['X-CSRF-Token'] = csrfToken;
    headers['Cookie'] = `csrf_token=${csrfToken}`;
  }

  const response = await fetch(url, {
    method: 'POST',
    headers,
    body: formData,
    cache: 'no-store',
  });

  if (!response.ok) {
    const errorBody = await response.json().catch(() => ({}));
    const message =
      errorBody.message || errorBody.error || errorBody.title || `Error ${response.status}`;
    throw new Error(message);
  }

  if (response.status === 204) {
    return {} as T;
  }

  return response.json();
}

// =============================================================================
// PROFILE ACTIONS
// =============================================================================

/**
 * Create KYC profile — server-side
 * Browser NEVER sees: /api/kyc/kycprofiles, personal data, document numbers
 */
export async function serverCreateKYCProfile(data: {
  userId: string;
  firstName: string;
  lastName: string;
  documentNumber: string;
  documentType: number;
  dateOfBirth: string;
  nationality: string;
  address: string;
  city: string;
  province: string;
  phoneNumber: string;
  sourceOfFunds?: string;
  occupation?: string;
  expectedMonthlyTransaction?: number;
}): Promise<ActionResult<KYCProfileResult>> {
  try {
    // SECURITY: Read access token from HttpOnly cookie (set by AuthController on login)
    const cookieStore = await cookies();
    const accessToken = cookieStore.get(AUTH_COOKIE_NAME)?.value;

    if (!accessToken) {
      return {
        success: false,
        error: 'Authentication required — please login again',
        code: 'KYC_AUTH_REQUIRED',
      };
    }

    const mappedData = {
      userId: data.userId,
      fullName: `${data.firstName} ${data.lastName}`.trim(),
      lastName: data.lastName,
      primaryDocumentNumber: data.documentNumber,
      primaryDocumentType: data.documentType,
      dateOfBirth: data.dateOfBirth,
      nationality: data.nationality,
      address: data.address,
      city: data.city,
      province: data.province,
      country: 'DO',
      phone: data.phoneNumber,
      sourceOfFunds: data.sourceOfFunds,
      occupation: data.occupation,
      expectedTransactionVolume: data.expectedMonthlyTransaction?.toString(),
      entityType: 1, // Individual
    };

    const idempotencyKey = generateIdempotencyKey();

    const response = await internalFetch<KYCProfileResult>(`${KYC_BASE}/kycprofiles`, {
      method: 'POST',
      body: mappedData,
      token: accessToken,
      headers: { 'X-Idempotency-Key': idempotencyKey },
    });

    return { success: true, data: response };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Error al crear el perfil KYC',
      code: 'KYC_CREATE_FAILED',
    };
  }
}

/**
 * Update KYC profile — server-side
 * Browser NEVER sees: personal data updates, document numbers
 */
export async function serverUpdateKYCProfile(
  profileId: string,
  data: {
    firstName?: string;
    lastName?: string;
    documentNumber?: string;
    documentType?: number;
    dateOfBirth?: string;
    nationality?: string;
    address?: string;
    city?: string;
    province?: string;
    phoneNumber?: string;
    sourceOfFunds?: string;
    occupation?: string;
    expectedMonthlyTransaction?: number;
  }
): Promise<ActionResult<KYCProfileResult>> {
  try {
    const cookieStore = await cookies();
    const accessToken = cookieStore.get(AUTH_COOKIE_NAME)?.value;

    if (!accessToken) {
      return {
        success: false,
        error: 'Authentication required — please login again',
        code: 'KYC_AUTH_REQUIRED',
      };
    }

    const mappedData: Record<string, unknown> = { id: profileId };
    if (data.firstName || data.lastName) {
      mappedData.fullName = `${data.firstName || ''} ${data.lastName || ''}`.trim();
    }
    if (data.lastName) mappedData.lastName = data.lastName;
    if (data.documentNumber) mappedData.primaryDocumentNumber = data.documentNumber;
    if (data.documentType) mappedData.primaryDocumentType = data.documentType;
    if (data.dateOfBirth) mappedData.dateOfBirth = data.dateOfBirth;
    if (data.nationality) mappedData.nationality = data.nationality;
    if (data.address) mappedData.address = data.address;
    if (data.city) mappedData.city = data.city;
    if (data.province) mappedData.province = data.province;
    if (data.phoneNumber) mappedData.phone = data.phoneNumber;
    if (data.sourceOfFunds) mappedData.sourceOfFunds = data.sourceOfFunds;
    if (data.occupation) mappedData.occupation = data.occupation;
    if (data.expectedMonthlyTransaction) {
      mappedData.expectedTransactionVolume = data.expectedMonthlyTransaction.toString();
    }

    const response = await internalFetch<KYCProfileResult>(`${KYC_BASE}/kycprofiles/${profileId}`, {
      method: 'PUT',
      body: mappedData,
      token: accessToken,
    });

    return { success: true, data: response };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Error al actualizar el perfil KYC',
      code: 'KYC_UPDATE_FAILED',
    };
  }
}

/**
 * Upsert (save or update) KYC profile draft — server-side
 * Called on each step change and auto-save (every 30s).
 * Browser NEVER sees: /api/kyc/kycprofiles/draft, form data
 */
export async function serverUpsertKYCDraft(data: {
  currentStep: number;
  formData: string; // JSON string with all wizard form data
}): Promise<ActionResult<KYCProfileDraft>> {
  try {
    const cookieStore = await cookies();
    const accessToken = cookieStore.get(AUTH_COOKIE_NAME)?.value;

    if (!accessToken) {
      return {
        success: false,
        error: 'Authentication required — please login again',
        code: 'KYC_AUTH_REQUIRED',
      };
    }

    const idempotencyKey = generateIdempotencyKey();

    const response = await internalFetch<KYCProfileDraft>(`${KYC_BASE}/kycprofiles/draft`, {
      method: 'POST',
      body: data,
      token: accessToken,
      headers: { 'X-Idempotency-Key': idempotencyKey },
    });

    return { success: true, data: response };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Error al guardar el borrador KYC',
      code: 'KYC_DRAFT_SAVE_FAILED',
    };
  }
}

/**
 * Submit KYC profile for review — server-side
 * Browser NEVER sees: /api/kyc/.../submit
 */
export async function serverSubmitKYCForReview(
  profileId: string
): Promise<ActionResult<KYCProfileResult>> {
  try {
    const cookieStore = await cookies();
    const accessToken = cookieStore.get(AUTH_COOKIE_NAME)?.value;

    if (!accessToken) {
      return {
        success: false,
        error: 'Authentication required — please login again',
        code: 'KYC_AUTH_REQUIRED',
      };
    }
    const idempotencyKey = generateIdempotencyKey();

    const response = await internalFetch<KYCProfileResult>(
      `${KYC_BASE}/kycprofiles/${profileId}/submit`,
      {
        method: 'POST',
        token: accessToken,
        headers: { 'X-Idempotency-Key': idempotencyKey },
      }
    );

    return { success: true, data: response };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Error al enviar el perfil para revisión',
      code: 'KYC_SUBMIT_FAILED',
    };
  }
}

// =============================================================================
// DOCUMENT ACTIONS
// =============================================================================

/**
 * Upload KYC document — server-side
 * Step 1: Upload file to MediaService (internal)
 * Step 2: Register document in KYC service (internal)
 * Browser NEVER sees: media upload URLs, storage keys, S3 paths
 */
export async function serverUploadKYCDocument(
  profileId: string,
  documentType: number,
  fileBase64: string,
  fileName: string,
  fileType: string,
  side?: string
): Promise<ActionResult<KYCDocumentResult>> {
  try {
    const cookieStore = await cookies();
    const accessToken = cookieStore.get(AUTH_COOKIE_NAME)?.value;

    if (!accessToken) {
      return {
        success: false,
        error: 'Authentication required — please login again',
        code: 'KYC_AUTH_REQUIRED',
      };
    }

    if (!profileId || !fileBase64 || isNaN(documentType)) {
      return {
        success: false,
        error: 'Datos incompletos para la carga del documento',
        code: 'KYC_UPLOAD_INVALID',
      };
    }

    // Step 1: Upload file to MediaService via internal network
    const mediaFormData = new FormData();
    // Convert base64 back to Blob
    const byteString = Buffer.from(fileBase64, 'base64').toString('binary');
    const bytes = new Uint8Array(byteString.length);
    for (let i = 0; i < byteString.length; i++) {
      bytes[i] = byteString.charCodeAt(i);
    }
    const blob = new Blob([bytes], { type: fileType });

    mediaFormData.append('file', blob, fileName);
    mediaFormData.append('folder', 'kyc-documents');

    const uploadResult = await internalFetchFormData<{
      url: string;
      publicId: string;
      storageKey: string;
    }>('/api/media/upload', mediaFormData, {
      token: accessToken,
    });

    const fileUrl = uploadResult.url;
    const storageKey = uploadResult.storageKey || uploadResult.publicId;

    // Step 2: Register document in KYC service
    const idempotencyKey = generateIdempotencyKey();

    const docPayload = {
      kycProfileId: profileId,
      type: documentType,
      documentName: fileName,
      fileName: storageKey,
      storageKey: storageKey,
      fileUrl: fileUrl,
      fileType: fileType,
      fileSize: Buffer.from(fileBase64, 'base64').length,
      uploadedBy: profileId,
      side: side || undefined,
    };

    const response = await internalFetch<KYCDocumentResult>(
      `${KYC_BASE}/profiles/${profileId}/documents`,
      {
        method: 'POST',
        body: docPayload,
        token: accessToken,
        headers: { 'X-Idempotency-Key': idempotencyKey },
      }
    );

    return { success: true, data: response };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Error al cargar el documento',
      code: 'KYC_UPLOAD_FAILED',
    };
  }
}

/**
 * Delete KYC document — server-side
 */
export async function serverDeleteKYCDocument(documentId: string): Promise<ActionResult<void>> {
  try {
    const cookieStore = await cookies();
    const accessToken = cookieStore.get(AUTH_COOKIE_NAME)?.value;

    if (!accessToken) {
      return {
        success: false,
        error: 'Authentication required — please login again',
        code: 'KYC_AUTH_REQUIRED',
      };
    }
    await internalFetch(`${KYC_BASE}/documents/${documentId}`, {
      method: 'DELETE',
      token: accessToken,
    });

    return { success: true };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Error al eliminar el documento',
      code: 'KYC_DELETE_FAILED',
    };
  }
}

// =============================================================================
// IDENTITY VERIFICATION ACTIONS
// =============================================================================

/**
 * Process identity verification (biometric) — server-side
 * Browser NEVER sees: selfie data, biometric scores, liveness data, verification results
 */
export async function serverProcessIdentityVerification(
  formData: FormData
): Promise<ActionResult<IdentityVerificationResult>> {
  try {
    const cookieStore = await cookies();
    const accessToken = cookieStore.get(AUTH_COOKIE_NAME)?.value;

    if (!accessToken) {
      return {
        success: false,
        error: 'Authentication required — please login again',
        code: 'KYC_AUTH_REQUIRED',
      };
    }
    const profileId = formData.get('profileId') as string;
    const selfie = formData.get('selfie') as File;
    const livenessDataStr = formData.get('livenessData') as string | null;

    if (!profileId || !selfie) {
      return {
        success: false,
        error: 'Datos incompletos para la verificación',
        code: 'KYC_VERIFY_INVALID',
      };
    }

    const verifyFormData = new FormData();
    verifyFormData.append('profileId', profileId);
    verifyFormData.append('selfie', selfie);
    if (livenessDataStr) {
      verifyFormData.append('livenessData', livenessDataStr);
    }

    const idempotencyKey = generateIdempotencyKey();

    const response = await internalFetchFormData<IdentityVerificationResult>(
      `${KYC_BASE}/identity-verification/verify`,
      verifyFormData,
      {
        token: accessToken,
        headers: { 'X-Idempotency-Key': idempotencyKey },
      }
    );

    return { success: true, data: response };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Error en la verificación de identidad',
      code: 'KYC_VERIFY_FAILED',
    };
  }
}

// =============================================================================
// ADMIN ACTIONS
// =============================================================================

/**
 * Approve KYC profile (Admin) — server-side
 * Browser NEVER sees: /api/kyc/.../approve, admin identity
 */
export async function serverApproveKYCProfile(
  profileId: string,
  approvedBy: string,
  approvedByName: string,
  notes?: string
): Promise<ActionResult<KYCProfileResult>> {
  try {
    const cookieStore = await cookies();
    const accessToken = cookieStore.get(AUTH_COOKIE_NAME)?.value;

    if (!accessToken) {
      return {
        success: false,
        error: 'Authentication required — please login again',
        code: 'KYC_AUTH_REQUIRED',
      };
    }

    const idempotencyKey = generateIdempotencyKey();

    const response = await internalFetch<KYCProfileResult>(
      `${KYC_BASE}/kycprofiles/${profileId}/approve`,
      {
        method: 'POST',
        body: {
          id: profileId,
          approvedBy,
          approvedByName,
          notes,
        },
        token: accessToken,
        headers: { 'X-Idempotency-Key': idempotencyKey },
      }
    );

    return { success: true, data: response };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Error al aprobar el perfil KYC',
      code: 'KYC_APPROVE_FAILED',
    };
  }
}

/**
 * Reject KYC profile (Admin) — server-side
 * Browser NEVER sees: /api/kyc/.../reject, rejection reason
 */
export async function serverRejectKYCProfile(
  profileId: string,
  rejectedBy: string,
  rejectedByName: string,
  rejectionReason: string,
  notes?: string
): Promise<ActionResult<KYCProfileResult>> {
  try {
    const cookieStore = await cookies();
    const accessToken = cookieStore.get(AUTH_COOKIE_NAME)?.value;

    if (!accessToken) {
      return {
        success: false,
        error: 'Authentication required — please login again',
        code: 'KYC_AUTH_REQUIRED',
      };
    }

    const idempotencyKey = generateIdempotencyKey();

    const response = await internalFetch<KYCProfileResult>(
      `${KYC_BASE}/kycprofiles/${profileId}/reject`,
      {
        method: 'POST',
        body: {
          id: profileId,
          rejectedBy,
          rejectedByName,
          rejectionReason,
          notes,
        },
        token: accessToken,
        headers: { 'X-Idempotency-Key': idempotencyKey },
      }
    );

    return { success: true, data: response };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Error al rechazar el perfil KYC',
      code: 'KYC_REJECT_FAILED',
    };
  }
}
