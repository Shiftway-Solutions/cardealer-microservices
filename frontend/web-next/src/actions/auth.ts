'use server';

/**
 * Auth Server Actions — Critical authentication operations
 *
 * These run exclusively on the Next.js server. The browser only sees
 * an opaque POST to the same origin — no API endpoints, payloads,
 * or responses are visible in DevTools Network tab.
 *
 * Flow: Browser → Server Action (Next.js) → Gateway (internal) → AuthService
 */

import { getInternalApiUrl } from '@/lib/api-url';
import { cookies } from 'next/headers';

// =============================================================================
// COOKIE CONSTANTS (must match backend AuthController.SetAuthCookies)
// =============================================================================

const AUTH_COOKIE_NAME = 'okla_access_token';
const REFRESH_COOKIE_NAME = 'okla_refresh_token';

// =============================================================================
// TYPES (duplicated here because Server Actions can't import client-side types)
// =============================================================================

export interface ActionResult<T = void> {
  success: boolean;
  data?: T;
  error?: string;
  code?: string;
}

/**
 * User profile data returned alongside tokens after a successful login.
 * Matches UserResponse / UserDto from AuthService and UserService.
 * Exported so services/auth.ts can use it with transformUser().
 */
export interface UserProfileResult {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  avatarUrl?: string;
  phone?: string;
  accountType: string;
  userIntent?: string;
  isVerified: boolean;
  isEmailVerified: boolean;
  isPhoneVerified: boolean;
  preferredLocale?: string;
  preferredCurrency?: string;
  createdAt?: string;
  lastLoginAt?: string;
}

interface LoginResult {
  accessToken: string;
  refreshToken: string;
  requiresTwoFactor?: boolean;
  tempToken?: string;
  twoFactorType?: string;
  /** User profile fetched server-side — avoids cross-origin cookie issues in dev */
  user?: UserProfileResult;
}

interface RegisterResult {
  email: string;
}

// =============================================================================
// INTERNAL HELPERS
// =============================================================================

const API_URL = () => getInternalApiUrl();

/**
 * Make an internal API call to the Gateway
 * All calls go through the internal network (http://gateway:8080)
 */
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
  if (method && ['POST', 'PUT', 'PATCH', 'DELETE'].includes(method.toUpperCase())) {
    const cookieStore = await cookies();
    const csrfToken = cookieStore.get('csrf_token')?.value;
    if (csrfToken) {
      headers['X-CSRF-Token'] = csrfToken;
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

  // Some endpoints return no content (204)
  if (response.status === 204) {
    return {} as T;
  }

  return response.json();
}

// =============================================================================
// COOKIE HELPERS
// =============================================================================

/**
 * Set auth cookies from Server Action response.
 * Since Server Actions run server-side, we use `cookies()` from next/headers
 * to set HttpOnly cookies on the response back to the browser.
 *
 * The backend also sets these via Set-Cookie headers, but those are consumed
 * by the Node.js server-side fetch and never reach the browser.
 */
async function setAuthCookiesFromServerAction(accessToken: string, refreshToken: string) {
  const cookieStore = await cookies();

  const isProduction = process.env.NODE_ENV === 'production';

  cookieStore.set(AUTH_COOKIE_NAME, accessToken, {
    httpOnly: true,
    secure: isProduction,
    sameSite: 'lax',
    path: '/',
    maxAge: 24 * 60 * 60, // 24 hours (matches backend JWT expiration)
  });

  cookieStore.set(REFRESH_COOKIE_NAME, refreshToken, {
    httpOnly: true,
    secure: isProduction,
    sameSite: 'lax',
    path: '/',
    maxAge: 30 * 24 * 60 * 60, // 30 days (matches backend refresh token expiration)
  });
}

/**
 * Clear auth cookies on logout.
 */
async function clearAuthCookiesFromServerAction() {
  const cookieStore = await cookies();
  cookieStore.delete(AUTH_COOKIE_NAME);
  cookieStore.delete(REFRESH_COOKIE_NAME);
}

// =============================================================================
// AUTH ACTIONS
// =============================================================================

/**
 * Login — processes credentials on server, returns tokens
 * Browser sees: POST to /_next/... (opaque Server Action call)
 * Browser NEVER sees: /api/auth/login endpoint, credentials payload, token response
 */
export async function serverLogin(
  email: string,
  password: string,
  rememberMe?: boolean
): Promise<ActionResult<LoginResult>> {
  try {
    const response = await internalFetch<{
      success: boolean;
      data: {
        userId: string;
        email: string;
        accessToken: string;
        refreshToken: string;
        expiresAt: string;
        requiresTwoFactor?: boolean;
        tempToken?: string;
        twoFactorType?: string;
      };
    }>('/api/auth/login', {
      method: 'POST',
      body: { email, password, rememberMe },
    });

    const loginData = response.data || response;

    // 2FA required — return temp token (no cookies yet)
    if (loginData.requiresTwoFactor && loginData.tempToken) {
      return {
        success: true,
        data: {
          accessToken: '',
          refreshToken: '',
          requiresTwoFactor: true,
          tempToken: loginData.tempToken,
          twoFactorType: loginData.twoFactorType || 'authenticator',
        },
      };
    }

    // Set HttpOnly cookies so the browser sends them on subsequent requests
    if (loginData.accessToken && loginData.refreshToken) {
      await setAuthCookiesFromServerAction(loginData.accessToken, loginData.refreshToken);
    }

    // Fetch user profile server-side using the access token.
    // This avoids the cross-origin cookie problem in development where the
    // auth cookie (set for localhost:3000) can't be read by apiClient calls
    // directed at localhost:18443 (different origin).
    let userProfile: UserProfileResult | undefined;
    try {
      const meResponse = await internalFetch<{ success: boolean; data: UserProfileResult }>(
        '/api/auth/me',
        { token: loginData.accessToken }
      );
      userProfile = meResponse.data ?? (meResponse as unknown as UserProfileResult);
    } catch {
      // Non-critical — login already succeeded and cookies are set.
      // The auth context will retry getCurrentUser() on next page load.
    }

    return {
      success: true,
      data: {
        accessToken: loginData.accessToken,
        refreshToken: loginData.refreshToken,
        user: userProfile,
      },
    };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Error al iniciar sesión',
      code: 'LOGIN_FAILED',
    };
  }
}

/**
 * Verify 2FA code to complete login
 */
export async function serverVerify2FA(
  tempToken: string,
  twoFactorCode: string
): Promise<ActionResult<LoginResult>> {
  try {
    const response = await internalFetch<{
      success: boolean;
      data: {
        accessToken: string;
        refreshToken: string;
      };
    }>('/api/auth/2fa/login', {
      method: 'POST',
      body: { tempToken, twoFactorCode },
    });

    const result = response.data || response;

    // Set HttpOnly cookies so the browser sends them on subsequent requests
    if (result.accessToken && result.refreshToken) {
      await setAuthCookiesFromServerAction(result.accessToken, result.refreshToken);
    }

    // Fetch user profile server-side (same cross-origin fix as serverLogin)
    let userProfile: UserProfileResult | undefined;
    try {
      const meResponse = await internalFetch<{ success: boolean; data: UserProfileResult }>(
        '/api/auth/me',
        { token: result.accessToken }
      );
      userProfile = meResponse.data ?? (meResponse as unknown as UserProfileResult);
    } catch {
      // Non-critical
    }

    return {
      success: true,
      data: {
        accessToken: result.accessToken,
        refreshToken: result.refreshToken,
        user: userProfile,
      },
    };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Código 2FA inválido',
      code: '2FA_FAILED',
    };
  }
}

/**
 * Register — creates account on server
 * Browser NEVER sees: /api/auth/register endpoint, password in payload
 */
export async function serverRegister(
  firstName: string,
  lastName: string,
  email: string,
  password: string,
  acceptTerms: boolean,
  phone?: string,
  accountType?: string,
  userIntent?: string
): Promise<ActionResult<RegisterResult>> {
  try {
    await internalFetch('/api/auth/register', {
      method: 'POST',
      body: {
        firstName,
        lastName,
        email,
        phone,
        password,
        acceptTerms,
        ...(accountType && { accountType }),
        ...(userIntent && { userIntent }),
      },
    });

    return {
      success: true,
      data: { email },
    };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Error al crear la cuenta',
      code: 'REGISTER_FAILED',
    };
  }
}

/**
 * Forgot password — server-side email sending
 */
export async function serverForgotPassword(email: string): Promise<ActionResult> {
  try {
    await internalFetch('/api/auth/forgot-password', {
      method: 'POST',
      body: { email },
    });

    return { success: true };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Error al enviar el correo',
      code: 'FORGOT_PASSWORD_FAILED',
    };
  }
}

/**
 * Reset password with token
 */
export async function serverResetPassword(
  token: string,
  newPassword: string
): Promise<ActionResult> {
  try {
    await internalFetch('/api/auth/reset-password', {
      method: 'POST',
      body: { token, newPassword, confirmPassword: newPassword },
    });

    return { success: true };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Error al restablecer la contraseña',
      code: 'RESET_PASSWORD_FAILED',
    };
  }
}

/**
 * Verify email with token
 */
export async function serverVerifyEmail(token: string): Promise<ActionResult> {
  try {
    await internalFetch('/api/auth/verify-email', {
      method: 'POST',
      body: { token },
    });

    return { success: true };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Error al verificar el correo',
      code: 'VERIFY_EMAIL_FAILED',
    };
  }
}

/**
 * Resend verification email
 */
export async function serverResendVerification(email: string): Promise<ActionResult> {
  try {
    await internalFetch('/api/auth/resend-verification', {
      method: 'POST',
      body: { email },
    });

    return { success: true };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Error al reenviar verificación',
      code: 'RESEND_VERIFICATION_FAILED',
    };
  }
}

/**
 * Change password (authenticated user)
 */
export async function serverChangePassword(
  currentPassword: string,
  newPassword: string,
  accessToken: string
): Promise<ActionResult> {
  try {
    await internalFetch('/api/auth/security/change-password', {
      method: 'POST',
      body: {
        currentPassword,
        newPassword,
        confirmPassword: newPassword,
      },
      token: accessToken,
    });

    return { success: true };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Error al cambiar la contraseña',
      code: 'CHANGE_PASSWORD_FAILED',
    };
  }
}

/**
 * Set password for OAuth users
 */
export async function serverSetPassword(
  password: string,
  accessToken: string
): Promise<ActionResult> {
  try {
    await internalFetch('/api/auth/set-password', {
      method: 'POST',
      body: { password },
      token: accessToken,
    });

    return { success: true };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Error al establecer la contraseña',
      code: 'SET_PASSWORD_FAILED',
    };
  }
}

/**
 * Logout — invalidates refresh token on server
 */
export async function serverLogout(
  refreshToken: string,
  accessToken: string
): Promise<ActionResult> {
  try {
    await internalFetch('/api/auth/logout', {
      method: 'POST',
      body: { refreshToken },
      token: accessToken,
    });
  } catch {
    // Ignore API errors — cookies should be cleared regardless
  }

  // Always clear HttpOnly cookies server-side
  await clearAuthCookiesFromServerAction();

  return { success: true };
}

// =============================================================================
// 2FA ACTIONS
// =============================================================================

/**
 * Initialize 2FA setup — returns QR code and secret
 */
export async function serverSetup2FA(
  accessToken: string
): Promise<ActionResult<{ qrCodeUrl: string; secret: string; backupCodes: string[] }>> {
  try {
    const response = await internalFetch<{
      data?: { secret: string; qrCodeUri: string; recoveryCodes: string[] };
      secret?: string;
      qrCodeUri?: string;
      recoveryCodes?: string[];
    }>('/api/auth/2fa/enable', {
      method: 'POST',
      body: { type: 1 },
      token: accessToken,
    });

    const result = response.data ?? response;

    return {
      success: true,
      data: {
        secret: result.secret ?? '',
        qrCodeUrl: result.qrCodeUri ?? '',
        backupCodes: result.recoveryCodes ?? [],
      },
    };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Error al configurar 2FA',
      code: '2FA_SETUP_FAILED',
    };
  }
}

/**
 * Verify 2FA code to complete setup
 */
export async function serverEnable2FA(code: string, accessToken: string): Promise<ActionResult> {
  try {
    await internalFetch('/api/auth/2fa/verify', {
      method: 'POST',
      body: { code, type: 1 },
      token: accessToken,
    });

    return { success: true };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Código 2FA inválido',
      code: '2FA_VERIFY_FAILED',
    };
  }
}

/**
 * Disable 2FA
 */
export async function serverDisable2FA(
  password: string,
  accessToken: string
): Promise<ActionResult> {
  try {
    await internalFetch('/api/auth/2fa/disable', {
      method: 'POST',
      body: { password },
      token: accessToken,
    });

    return { success: true };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Error al desactivar 2FA',
      code: '2FA_DISABLE_FAILED',
    };
  }
}

// =============================================================================
// ACCOUNT DELETION (ARCO Compliance)
// =============================================================================

/**
 * Request account deletion — Step 1
 * Reads auth token from HttpOnly cookie — no token parameter needed.
 */
export async function serverRequestAccountDeletion(
  reason: number,
  _accessToken: string, // kept for API compat but cookie is used instead
  otherReason?: string,
  feedback?: string
): Promise<ActionResult<{ requestId: string; gracePeriodEndsAt: string }>> {
  try {
    const cookieStore = await cookies();
    const token = cookieStore.get(AUTH_COOKIE_NAME)?.value || _accessToken;

    const response = await internalFetch<{
      requestId: string;
      gracePeriodEndsAt: string;
    }>('/api/privacy/delete-account/request', {
      method: 'POST',
      body: { reason, otherReason, feedback },
      token,
    });

    return {
      success: true,
      data: {
        requestId: response.requestId,
        gracePeriodEndsAt: response.gracePeriodEndsAt,
      },
    };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Error al solicitar eliminación',
      code: 'DELETE_REQUEST_FAILED',
    };
  }
}

/**
 * Confirm account deletion — Step 2
 * Reads auth token from HttpOnly cookie — no token parameter needed.
 */
export async function serverConfirmAccountDeletion(
  confirmationCode: string,
  password: string,
  _accessToken: string // kept for API compat but cookie is used instead
): Promise<ActionResult> {
  try {
    const cookieStore = await cookies();
    const token = cookieStore.get(AUTH_COOKIE_NAME)?.value || _accessToken;

    await internalFetch('/api/privacy/delete-account/confirm', {
      method: 'POST',
      body: { confirmationCode, password },
      token,
    });

    // Clear auth cookies — account is now queued for deletion
    await clearAuthCookiesFromServerAction();

    return { success: true };
  } catch (error: unknown) {
    const err = error as Error;
    return {
      success: false,
      error: err.message || 'Error al confirmar eliminación',
      code: 'DELETE_CONFIRM_FAILED',
    };
  }
}
