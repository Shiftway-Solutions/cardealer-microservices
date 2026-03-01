/**
 * Authentication Service - API client for auth operations
 * Connects via API Gateway to AuthService
 */

import { apiClient, authTokens } from '@/lib/api-client';
import { getClientApiUrl } from '@/lib/api-url';
import type { User } from '@/types';

// Server Actions — mutations run on the server, invisible to browser DevTools
import {
  serverLogin,
  serverVerify2FA,
  serverRegister,
  serverForgotPassword,
  serverResetPassword,
  serverVerifyEmail,
  serverResendVerification,
  serverChangePassword,
  serverSetPassword,
  serverLogout,
  serverSetup2FA,
  serverEnable2FA,
  serverDisable2FA,
  serverRequestAccountDeletion,
  serverConfirmAccountDeletion,
  type UserProfileResult,
} from '@/actions/auth';

// ============================================================
// API TYPES
// ============================================================

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe?: boolean;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  password: string;
  acceptTerms: boolean;
  /** Account type to register as. Defaults to 'buyer' if not provided. */
  accountType?: 'buyer' | 'seller' | 'dealer';
  /** User intent. Auto-derived from accountType if not set explicitly. */
  userIntent?: 'browse' | 'buy' | 'sell' | 'buy_and_sell';
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  user?: UserDto;
}

export interface UserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  avatarUrl?: string;
  phone?: string;
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
  preferredLocale: string;
  preferredCurrency: 'DOP' | 'USD';
  createdAt: string;
  lastLoginAt?: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  Token: string;
  NewPassword: string;
  ConfirmPassword: string;
}

export interface VerifyEmailRequest {
  token: string;
}

export interface Session {
  id: string;
  deviceName: string;
  deviceType: 'desktop' | 'mobile' | 'tablet' | 'unknown';
  browser: string;
  os: string;
  ipAddress: string;
  location?: string;
  lastActiveAt: string;
  createdAt: string;
  isCurrent: boolean;
}

// Backend SessionDetailDto structure (from GetActiveSessionsResponse)
// Backend returns PascalCase, but axios may transform to camelCase depending on config
interface BackendActiveSession {
  // Support both PascalCase (raw) and camelCase (transformed)
  id?: string;
  Id?: string;
  device?: string;
  Device?: string;
  browser?: string;
  Browser?: string;
  operatingSystem?: string;
  OperatingSystem?: string;
  location?: string;
  Location?: string;
  ipAddress?: string;
  IpAddress?: string;
  lastActive?: string;
  LastActiveAt?: string;
  lastActiveAt?: string;
  createdAt?: string;
  CreatedAt?: string;
  isCurrent?: boolean;
  IsCurrent?: boolean;
  isExpiringSoon?: boolean;
  IsExpiringSoon?: boolean;
  expiresAt?: string;
  ExpiresAt?: string;
}

// Backend LinkedProviderDto structure
interface BackendLinkedProvider {
  provider: string;
  email: string;
  linkedAt: string;
}

// Backend SecuritySettingsDto structure
interface BackendSecuritySettings {
  twoFactorEnabled: boolean;
  twoFactorType?: string;
  lastPasswordChange?: string;
  activeSessions: BackendActiveSession[];
  recentLogins: {
    id: string;
    device: string;
    browser: string;
    location: string;
    ipAddress: string;
    loginTime: string;
    success: boolean;
  }[];
  hasPassword: boolean;
  linkedProviders?: BackendLinkedProvider[];
}

// Backend GetActiveSessionsResponse structure wrapped in ApiResponse
// Backend returns: { success: true, data: { success: true, sessions: [...], totalActiveSessions: N } }
// Or possibly: { success: true, sessions: [...], totalActiveSessions: N }
interface BackendGetActiveSessionsResponse {
  success: boolean;
  data?: {
    success: boolean;
    sessions: BackendActiveSession[];
    totalActiveSessions: number;
  };
  // Also support flat structure
  sessions?: BackendActiveSession[];
  totalSessions?: number;
  totalActiveSessions?: number;
}

export interface TwoFactorSetupResponse {
  qrCodeUrl: string;
  secret: string;
  backupCodes: string[];
}

export interface TwoFactorStatus {
  isEnabled: boolean;
  enabledAt?: string;
  backupCodesRemaining: number;
}

/**
 * Linked OAuth provider (Google, Apple, etc.)
 */
export interface LinkedProvider {
  provider: 'Google' | 'Apple' | string;
  email: string;
  linkedAt: string;
}

/**
 * Complete security settings for the account
 */
export interface SecuritySettings {
  twoFactorEnabled: boolean;
  twoFactorType?: string;
  lastPasswordChange?: string;
  hasPassword: boolean;
  linkedProviders: LinkedProvider[];
}

// ============================================================
// TRANSFORM FUNCTIONS
// ============================================================

/**
 * Normalize backend account-type strings to the frontend canonical format.
 *
 * The backend emits AccountType.ToString().ToLowerInvariant() which produces:
 *   "dealeremployee"    (AccountType.DealerEmployee = 3)
 *   "platformemployee"  (AccountType.PlatformEmployee = 5)
 *
 * The frontend type and all switch/case blocks expect the underscore forms:
 *   "dealer_employee" / "platform_employee"
 *
 * Fixing this here (single source of truth) ensures every consumer of
 * transformUser() — login redirect, middleware role checks, etc. — sees the
 * correct value without per-callsite workarounds.
 */
function normalizeAccountType(type: string): User['accountType'] {
  const map: Record<string, User['accountType']> = {
    dealeremployee: 'dealer_employee',
    platformemployee: 'platform_employee',
  };
  return (map[type] ?? type) as User['accountType'];
}

export const transformUser = (dto: UserDto): User => ({
  id: dto.id,
  email: dto.email,
  firstName: dto.firstName,
  lastName: dto.lastName,
  fullName: dto.fullName,
  avatarUrl: dto.avatarUrl,
  phone: dto.phone,
  accountType: normalizeAccountType(dto.accountType),
  userIntent: dto.userIntent,
  isVerified: dto.isVerified,
  isEmailVerified: dto.isEmailVerified,
  isPhoneVerified: dto.isPhoneVerified,
  preferredLocale: dto.preferredLocale,
  preferredCurrency: dto.preferredCurrency,
  createdAt: dto.createdAt,
  lastLoginAt: dto.lastLoginAt,
});

// ============================================================
// 2FA LOGIN ERROR
// ============================================================

/**
 * Custom error thrown when login requires two-factor authentication.
 * The login page catches this to show the 2FA code input form.
 */
export class TwoFactorRequiredError extends Error {
  tempToken: string;
  twoFactorType: string;

  constructor(tempToken: string, twoFactorType: string) {
    super('Se requiere verificación de dos factores');
    this.name = 'TwoFactorRequiredError';
    this.tempToken = tempToken;
    this.twoFactorType = twoFactorType;
  }
}

// ============================================================
// API FUNCTIONS
// ============================================================

/**
 * Login with email and password.
 * If the account has 2FA enabled, throws TwoFactorRequiredError
 * with the tempToken needed to complete authentication via verifyTwoFactorLogin().
 */
export async function login(data: LoginRequest): Promise<{ user: User }> {
  // ── Server Action: credentials are sent server-side, invisible to browser ──
  const result = await serverLogin(data.email, data.password, data.rememberMe);

  if (!result.success) {
    throw new Error(result.error || 'Error al iniciar sesión');
  }

  // Check if 2FA is required
  if (result.data?.requiresTwoFactor && result.data.tempToken) {
    throw new TwoFactorRequiredError(
      result.data.tempToken,
      result.data.twoFactorType || 'authenticator'
    );
  }

  // Store tokens client-side (Server Actions can't access localStorage)
  authTokens.setTokens(result.data!.accessToken, result.data!.refreshToken);

  // Use the user profile fetched server-side inside serverLogin().
  // This avoids calling getCurrentUser() (which uses apiClient pointing at
  // localhost:18443 in dev — a different origin from localhost:3000, so the
  // auth cookie would not be sent cross-origin even with withCredentials:true).
  const user = result.data?.user
    ? transformUser(result.data.user as UserProfileResult & UserDto)
    : await getCurrentUser();

  if (!user) {
    throw new Error('Failed to load user profile after login');
  }

  return { user };
}

/**
 * Complete login with 2FA code.
 * Called after login() throws TwoFactorRequiredError.
 * Backend endpoint: POST /api/auth/2fa/login (Gateway → /api/TwoFactor/login)
 */
export async function verifyTwoFactorLogin(
  tempToken: string,
  twoFactorCode: string
): Promise<{ user: User }> {
  // ── Server Action: 2FA codes processed server-side ──
  const result = await serverVerify2FA(tempToken, twoFactorCode);

  if (!result.success || !result.data) {
    throw new Error(result.error || 'Código 2FA inválido');
  }

  // Store tokens client-side
  authTokens.setTokens(result.data.accessToken, result.data.refreshToken);

  // Use user profile embedded by serverVerify2FA (same cross-origin fix as login)
  const user = result.data.user
    ? transformUser(result.data.user as UserProfileResult & UserDto)
    : await getCurrentUser();

  if (!user) {
    throw new Error('Failed to load user profile after 2FA verification');
  }

  return { user };
}

/**
 * Register a new user
 * NOTE: Does NOT store tokens or auto-login.
 * The user must verify their email before logging in.
 * Backend enforces this: LoginHandler rejects unverified emails.
 */
export async function register(data: RegisterRequest): Promise<{ email: string }> {
  // ── Server Action: registration data sent server-side ──
  const result = await serverRegister(
    data.firstName,
    data.lastName,
    data.email,
    data.password,
    data.acceptTerms,
    data.phone,
    data.accountType,
    data.userIntent
  );

  if (!result.success) {
    throw new Error(result.error || 'Error al crear la cuenta');
  }

  // Do NOT store tokens — user must verify email first.
  return { email: data.email };
}

/**
 * Logout current user
 * Clears HttpOnly cookies via Server Action and legacy localStorage tokens.
 * Backend logout API is best-effort — cookie clearing always happens.
 */
export async function logout(): Promise<void> {
  try {
    // ── Server Action: logout processed server-side ──
    // Try to send refresh token if available (legacy localStorage fallback)
    const refreshToken = authTokens.getRefreshToken();
    const accessToken = authTokens.getAccessToken();
    // Always call serverLogout to clear HttpOnly cookies server-side,
    // even if we don't have tokens in localStorage
    await serverLogout(refreshToken || '', accessToken || '');
  } finally {
    // Clear legacy localStorage tokens
    authTokens.clearTokens();
  }
}

/**
 * Get current authenticated user
 */
export async function getCurrentUser(): Promise<User | null> {
  try {
    const response = await apiClient.get<{ success: boolean; data: UserDto }>('/api/auth/me');
    // Backend returns ApiResponse<UserDto>, so we need to access .data.data
    const userData = response.data.data || response.data;
    return transformUser(userData as UserDto);
  } catch {
    return null;
  }
}

/**
 * Request password reset email
 */
export async function forgotPassword(email: string): Promise<void> {
  // ── Server Action: email sent server-side ──
  const result = await serverForgotPassword(email);
  if (!result.success) {
    throw new Error(result.error || 'Error al enviar el correo de recuperación');
  }
}

/**
 * Reset password with token
 */
export async function resetPassword(data: { token: string; password: string }): Promise<void> {
  // ── Server Action: password reset server-side ──
  const result = await serverResetPassword(data.token, data.password);
  if (!result.success) {
    throw new Error(result.error || 'Error al restablecer la contraseña');
  }
}

/**
 * Verify email with token
 */
export async function verifyEmail(token: string): Promise<void> {
  // ── Server Action: verification processed server-side ──
  const result = await serverVerifyEmail(token);
  if (!result.success) {
    throw new Error(result.error || 'Error al verificar el correo');
  }
}

/**
 * Resend verification email
 */
export async function resendVerification(email: string): Promise<void> {
  // ── Server Action: resend processed server-side ──
  const result = await serverResendVerification(email);
  if (!result.success) {
    throw new Error(result.error || 'Error al reenviar el correo de verificación');
  }
}

/**
 * Set password for OAuth users
 */
export async function setPassword(password: string): Promise<void> {
  // ── Server Action: password set server-side ──
  const accessToken = authTokens.getAccessToken();
  const result = await serverSetPassword(password, accessToken || '');
  if (!result.success) {
    throw new Error(result.error || 'Error al establecer la contraseña');
  }
}

/**
 * Change password (for authenticated users)
 * Uses POST /api/auth/security/change-password
 * Backend expects: { CurrentPassword, NewPassword, ConfirmPassword }
 */
export async function changePassword(currentPassword: string, newPassword: string): Promise<void> {
  // ── Server Action: password change processed server-side ──
  const accessToken = authTokens.getAccessToken();
  const result = await serverChangePassword(currentPassword, newPassword, accessToken || '');
  if (!result.success) {
    throw new Error(result.error || 'Error al cambiar la contraseña');
  }
}

/**
 * Login with OAuth provider
 */
export async function loginWithProvider(provider: 'google' | 'apple'): Promise<void> {
  // BFF pattern: use same-origin relative URL in production (Next.js rewrites proxy to Gateway)
  const apiUrl = getClientApiUrl();
  window.location.href = `${apiUrl}/api/auth/oauth/${provider}`;
}

// ============================================================
// SESSION MANAGEMENT
// ============================================================

/**
 * Transform backend session to frontend Session type
 * Handles both PascalCase (raw .NET) and camelCase (axios transformed)
 */
function transformBackendSession(session: BackendActiveSession): Session {
  // Get values supporting both casings
  const device = session.device ?? session.Device ?? '';
  const browser = session.browser ?? session.Browser ?? '';
  const operatingSystem = session.operatingSystem ?? session.OperatingSystem ?? '';
  const location = session.location ?? session.Location ?? '';
  const ipAddress = session.ipAddress ?? session.IpAddress ?? '';
  const lastActive = session.lastActive ?? session.lastActiveAt ?? session.LastActiveAt ?? '';
  const createdAt = session.createdAt ?? session.CreatedAt ?? lastActive;
  const isCurrent = session.isCurrent ?? session.IsCurrent ?? false;
  const id = session.id ?? session.Id ?? '';

  // Parse device type from device field ("Desktop", "Mobile", "Tablet")
  const deviceLower = device.toLowerCase();
  let deviceType: Session['deviceType'] = 'unknown';
  if (
    deviceLower.includes('mobile') ||
    deviceLower.includes('iphone') ||
    deviceLower.includes('android')
  ) {
    deviceType = 'mobile';
  } else if (deviceLower.includes('tablet') || deviceLower.includes('ipad')) {
    deviceType = 'tablet';
  } else if (deviceLower.includes('desktop')) {
    deviceType = 'desktop';
  }

  // Resolve human-readable browser name (Spanish fallback for unknowns)
  const browserDisplay =
    browser && browser !== 'Unknown Browser' ? browser : 'Navegador desconocido';

  // Resolve OS — prefer dedicated OperatingSystem field over device string
  const osDisplay =
    operatingSystem && operatingSystem !== 'Unknown OS'
      ? operatingSystem
      : extractOS(device) || 'Sistema desconocido';

  // Build a meaningful device name: "Chrome en Windows 10/11" or "Navegador desconocido (Escritorio)"
  let deviceName: string;
  if (browserDisplay !== 'Navegador desconocido' && osDisplay !== 'Sistema desconocido') {
    deviceName = `${browserDisplay} en ${osDisplay}`;
  } else if (browserDisplay !== 'Navegador desconocido') {
    deviceName = browserDisplay;
  } else {
    const deviceLabel =
      deviceType === 'mobile' ? 'Móvil' : deviceType === 'tablet' ? 'Tablet' : 'Escritorio';
    deviceName = `Dispositivo desconocido (${deviceLabel})`;
  }

  return {
    id,
    deviceName,
    deviceType,
    browser: browserDisplay,
    os: osDisplay,
    ipAddress,
    location: location || undefined,
    lastActiveAt: lastActive,
    createdAt,
    isCurrent,
  };
}

/**
 * Extract OS from device string
 */
function extractOS(device: string): string {
  if (!device) return 'Sistema desconocido';
  const deviceLower = device.toLowerCase();
  if (deviceLower.includes('windows')) return 'Windows';
  if (deviceLower.includes('mac')) return 'macOS';
  if (deviceLower.includes('linux')) return 'Linux';
  if (deviceLower.includes('iphone') || deviceLower.includes('ios')) return 'iOS';
  if (deviceLower.includes('android')) return 'Android';
  return device;
}

/**
 * Get all active sessions for the current user
 * Uses /api/auth/security/sessions endpoint
 * Backend returns ApiResponse<GetActiveSessionsResponse>
 */
export async function getSessions(): Promise<Session[]> {
  try {
    const response = await apiClient.get<BackendGetActiveSessionsResponse>(
      '/api/auth/security/sessions'
    );
    const responseData = response.data;

    // Debug logging for development
    if (process.env.NODE_ENV === 'development') {
      console.log(
        '[getSessions] Raw response:',
        JSON.stringify(responseData, null, 2).slice(0, 500)
      );
    }

    // Handle nested structure: { success: true, data: { sessions: [...] } }
    // Also handle PascalCase: { Success: true, Data: { Sessions: [...] } }
    let sessions: BackendActiveSession[] = [];

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const data = responseData as any;

    // Try different paths to find sessions array
    if (data.data?.sessions && Array.isArray(data.data.sessions)) {
      sessions = data.data.sessions;
    } else if (data.data?.Sessions && Array.isArray(data.data.Sessions)) {
      sessions = data.data.Sessions;
    } else if (data.Data?.sessions && Array.isArray(data.Data.sessions)) {
      sessions = data.Data.sessions;
    } else if (data.Data?.Sessions && Array.isArray(data.Data.Sessions)) {
      sessions = data.Data.Sessions;
    } else if (data.sessions && Array.isArray(data.sessions)) {
      sessions = data.sessions;
    } else if (data.Sessions && Array.isArray(data.Sessions)) {
      sessions = data.Sessions;
    }

    if (process.env.NODE_ENV === 'development') {
      console.log('[getSessions] Found sessions:', sessions.length);
    }

    if (sessions.length > 0) {
      return sessions.map(transformBackendSession);
    }

    return [];
  } catch (error) {
    // SEGURIDAD: Solo mostrar errores en desarrollo
    if (process.env.NODE_ENV === 'development') {
      console.error('Error getting sessions:', error);
    }
    // Return empty array instead of throwing to prevent UI crashes
    return [];
  }
}

/**
 * Revoke a specific session
 * Uses /api/auth/security/sessions/{sessionId} endpoint
 */
export async function revokeSession(sessionId: string): Promise<void> {
  await apiClient.delete(`/api/auth/security/sessions/${sessionId}`);
}

/**
 * Revoke all sessions except current
 * Uses /api/auth/security/sessions/all endpoint
 */
export async function revokeAllSessions(): Promise<void> {
  await apiClient.delete('/api/auth/security/sessions/all');
}

// ============================================================
// TWO-FACTOR AUTHENTICATION
// ============================================================

interface BackendGenerateRecoveryCodesResponse {
  data?: {
    recoveryCodes: string[];
    message?: string;
  };
  recoveryCodes?: string[];
  message?: string;
}

/**
 * Get complete security settings for the account
 * Includes: hasPassword, linkedProviders, 2FA status
 * Uses /api/auth/security endpoint
 */
export async function getSecuritySettings(): Promise<SecuritySettings> {
  try {
    const response = await apiClient.get<BackendSecuritySettings>('/api/auth/security');
    const data = response.data;

    return {
      twoFactorEnabled: data.twoFactorEnabled ?? false,
      twoFactorType: data.twoFactorType,
      lastPasswordChange: data.lastPasswordChange,
      hasPassword: data.hasPassword ?? true, // Default to true for backward compatibility
      linkedProviders:
        data.linkedProviders?.map(p => ({
          provider: p.provider,
          email: p.email,
          linkedAt: p.linkedAt,
        })) ?? [],
    };
  } catch (error) {
    // SEGURIDAD: Solo mostrar errores en desarrollo
    if (process.env.NODE_ENV === 'development') {
      console.error('Error getting security settings:', error);
    }
    // Return safe defaults
    return {
      twoFactorEnabled: false,
      hasPassword: true,
      linkedProviders: [],
    };
  }
}

/**
 * Get 2FA status from security settings
 * Uses /api/auth/security endpoint and extracts 2FA info
 */
export async function get2FAStatus(): Promise<TwoFactorStatus> {
  try {
    const response = await apiClient.get<BackendSecuritySettings>('/api/auth/security');
    const data = response.data;

    return {
      isEnabled: data.twoFactorEnabled ?? false,
      enabledAt: data.lastPasswordChange, // Backend doesn't have enabledAt, use lastPasswordChange as approximation
      backupCodesRemaining: 0, // Backend doesn't track this directly
    };
  } catch (error) {
    // SEGURIDAD: Solo mostrar errores en desarrollo
    if (process.env.NODE_ENV === 'development') {
      console.error('Error getting 2FA status:', error);
    }
    // Return default status instead of throwing
    return {
      isEnabled: false,
      backupCodesRemaining: 0,
    };
  }
}

/**
 * Initialize 2FA setup (get QR code and secret)
 * Uses POST /api/auth/2fa/enable with type Authenticator
 * Backend endpoint: POST /api/TwoFactor/enable (via Gateway /api/auth/2fa/enable)
 */
export async function setup2FA(): Promise<TwoFactorSetupResponse> {
  // ── Server Action: 2FA setup processed server-side, QR code returned ──
  const accessToken = authTokens.getAccessToken();
  const result = await serverSetup2FA(accessToken || '');

  if (!result.success || !result.data) {
    throw new Error(result.error || 'Error al configurar 2FA');
  }

  return {
    secret: result.data.secret,
    qrCodeUrl: result.data.qrCodeUrl,
    backupCodes: result.data.backupCodes,
  };
}

/**
 * Verify 2FA code to complete setup
 * Uses POST /api/auth/2fa/verify
 * Backend endpoint: POST /api/TwoFactor/verify (via Gateway /api/auth/2fa/verify)
 */
export async function enable2FA(code: string): Promise<void> {
  // ── Server Action: 2FA verification server-side ──
  const accessToken = authTokens.getAccessToken();
  const result = await serverEnable2FA(code, accessToken || '');
  if (!result.success) {
    throw new Error(result.error || 'Código 2FA inválido');
  }
}

/**
 * Disable 2FA
 * Uses POST /api/auth/2fa/disable
 * Backend endpoint: POST /api/TwoFactor/disable (via Gateway /api/auth/2fa/disable)
 * Requires password for security
 */
export async function disable2FA(password: string): Promise<void> {
  // ── Server Action: 2FA disable processed server-side ──
  const accessToken = authTokens.getAccessToken();
  const result = await serverDisable2FA(password, accessToken || '');
  if (!result.success) {
    throw new Error(result.error || 'Error al desactivar 2FA');
  }
}

/**
 * Regenerate backup/recovery codes
 * Uses POST /api/auth/2fa/generate-recovery-codes
 * Backend endpoint: POST /api/TwoFactor/generate-recovery-codes
 * Requires password for security
 */
export async function regenerateBackupCodes(password: string): Promise<string[]> {
  const response = await apiClient.post<BackendGenerateRecoveryCodesResponse>(
    '/api/auth/2fa/generate-recovery-codes',
    {
      password, // Backend expects Password field
    }
  );

  // Handle both wrapped {data: {...}} and direct response
  const result = response.data.data ?? response.data;
  return result.recoveryCodes ?? [];
}

// ============================================================
// ACCOUNT MANAGEMENT (Delete Account - ARCO Compliance)
// ============================================================

/**
 * Deletion reason enum - matches backend DeletionReason
 * Backend expects numeric values:
 * 0 = PrivacyConcerns, 1 = NoLongerNeeded, 2 = FoundAlternative,
 * 3 = BadExperience, 4 = TooManyEmails, 5 = Other
 */
export type DeletionReasonString =
  | 'PrivacyConcerns'
  | 'NoLongerNeeded'
  | 'FoundAlternative'
  | 'BadExperience'
  | 'TooManyEmails'
  | 'Other';

export const DeletionReasonMap: Record<DeletionReasonString, number> = {
  PrivacyConcerns: 0,
  NoLongerNeeded: 1,
  FoundAlternative: 2,
  BadExperience: 3,
  TooManyEmails: 4,
  Other: 5,
};

export interface RequestAccountDeletionRequest {
  reason: DeletionReasonString;
  otherReason?: string;
  feedback?: string;
}

export interface AccountDeletionResponse {
  requestId: string;
  status: string;
  message: string;
  gracePeriodEndsAt: string;
  confirmationEmailSentTo: string;
}

export interface ConfirmAccountDeletionRequest {
  confirmationCode: string;
  password: string;
}

export interface AccountDeletionStatus {
  requestId: string;
  status: string;
  requestedAt: string;
  gracePeriodEndsAt: string;
  canCancel: boolean;
  daysRemaining: number;
  reason?: string;
}

/**
 * Request account deletion (Step 1)
 * Sends a confirmation code to user's email
 */
export async function requestAccountDeletion(
  request: RequestAccountDeletionRequest
): Promise<AccountDeletionResponse> {
  // ── Server Action: deletion request processed server-side ──
  const accessToken = authTokens.getAccessToken();
  const result = await serverRequestAccountDeletion(
    DeletionReasonMap[request.reason],
    accessToken || '',
    request.otherReason,
    request.feedback
  );

  if (!result.success || !result.data) {
    throw new Error(result.error || 'Error al solicitar eliminación de cuenta');
  }

  return {
    requestId: result.data.requestId,
    status: 'Pending',
    message: 'Solicitud de eliminación creada',
    gracePeriodEndsAt: result.data.gracePeriodEndsAt,
    confirmationEmailSentTo: '',
  };
}

/**
 * Confirm account deletion (Step 2)
 * Requires the confirmation code sent to email + password
 */
export async function confirmAccountDeletion(
  request: ConfirmAccountDeletionRequest
): Promise<void> {
  // ── Server Action: deletion confirmation processed server-side ──
  const accessToken = authTokens.getAccessToken();
  const result = await serverConfirmAccountDeletion(
    request.confirmationCode,
    request.password,
    accessToken || ''
  );

  if (!result.success) {
    throw new Error(result.error || 'Error al confirmar eliminación de cuenta');
  }

  // Clear tokens client-side after successful deletion
  authTokens.clearTokens();
}

/**
 * Cancel account deletion request (within grace period)
 */
export async function cancelAccountDeletion(): Promise<void> {
  await apiClient.post('/api/privacy/delete-account/cancel');
}

/**
 * Get account deletion status
 */
export async function getAccountDeletionStatus(): Promise<AccountDeletionStatus | null> {
  try {
    const response = await apiClient.get<AccountDeletionStatus>(
      '/api/privacy/delete-account/status'
    );
    return response.data;
  } catch {
    return null;
  }
}

/**
 * @deprecated Use requestAccountDeletion + confirmAccountDeletion instead
 * Legacy function for backwards compatibility
 */
export async function deleteAccount(_password: string): Promise<void> {
  // This is now a 2-step process, this function is kept for compatibility
  // but should be replaced with the new flow
  throw new Error(
    'deleteAccount is deprecated. Use requestAccountDeletion + confirmAccountDeletion instead.'
  );
}

/**
 * Auth service object
 */
export const authService = {
  login,
  register,
  logout,
  getCurrentUser,
  forgotPassword,
  resetPassword,
  verifyEmail,
  resendVerification,
  setPassword,
  changePassword,
  loginWithProvider,
  // Session management
  getSessions,
  revokeSession,
  revokeAllSessions,
  // Security settings
  getSecuritySettings,
  // 2FA
  get2FAStatus,
  setup2FA,
  enable2FA,
  disable2FA,
  verifyTwoFactorLogin,
  regenerateBackupCodes,
  // Account deletion (ARCO compliance - 2 step process)
  requestAccountDeletion,
  confirmAccountDeletion,
  cancelAccountDeletion,
  getAccountDeletionStatus,
  // @deprecated - use requestAccountDeletion + confirmAccountDeletion
  deleteAccount,
};

export default authService;
