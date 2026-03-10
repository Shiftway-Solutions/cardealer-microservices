// =============================================================================
// API Client Configuration
// =============================================================================
// Cliente Axios centralizado con interceptores y manejo de errores

import axios, { AxiosError, AxiosInstance, AxiosRequestConfig, AxiosResponse } from 'axios';
import { getCsrfToken } from '@/lib/security/csrf';
import { getApiBaseUrl } from '@/lib/api-url';

// Environment configuration — BFF pattern:
// Client-side (prod): empty baseURL = same-origin requests → Next.js rewrites → Gateway
// Server-side (prod): INTERNAL_API_URL = http://gateway:8080 (direct internal call)
// Development: http://localhost:18443 (direct to Gateway)
const API_URL = getApiBaseUrl();
const API_TIMEOUT = parseInt(process.env.NEXT_PUBLIC_API_TIMEOUT || '30000', 10);

// Token storage keys (kept for backward cleanup only)
const ACCESS_TOKEN_KEY = 'okla_access_token';
const REFRESH_TOKEN_KEY = 'okla_refresh_token';
const TOKEN_MIGRATED_KEY = 'okla_token_migrated';

// =============================================================================
// Legacy Token Migration (CWE-922)
// =============================================================================
// Clears old localStorage tokens that were used before HttpOnly cookie auth.
// Runs ONCE on first load, then sets a flag to avoid re-running.
function migrateTokensFromLocalStorage(): void {
  if (typeof window === 'undefined') return;
  // Defensive: localStorage may be unavailable in restricted contexts
  // (jsdom 27 without storageQuota, private browsing, some iframe sandboxes).
  if (typeof localStorage === 'undefined' || typeof localStorage.getItem !== 'function') return;

  // Skip if migration already completed
  if (localStorage.getItem(TOKEN_MIGRATED_KEY) === 'true') return;

  const hadAccessToken = localStorage.getItem(ACCESS_TOKEN_KEY);
  const hadRefreshToken = localStorage.getItem(REFRESH_TOKEN_KEY);

  if (hadAccessToken || hadRefreshToken) {
    console.warn(
      '[OKLA Security] Legacy localStorage tokens detected and removed. ' +
        'Auth now uses HttpOnly cookies. This migration runs once.'
    );
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
  }

  // Mark migration as complete so it never runs again
  localStorage.setItem(TOKEN_MIGRATED_KEY, 'true');
}

// Run migration at module initialization (client-side only)
if (typeof window !== 'undefined') {
  migrateTokensFromLocalStorage();
}

// Create axios instance
// Security (CWE-922): withCredentials=true sends HttpOnly cookies automatically
export const apiClient: AxiosInstance = axios.create({
  baseURL: API_URL,
  timeout: API_TIMEOUT,
  withCredentials: true, // Always send HttpOnly cookies with requests
  headers: {
    'Content-Type': 'application/json',
    Accept: 'application/json',
  },
});

// Request interceptor - Add CSRF protection
// NOTE: Auth tokens are now HttpOnly cookies — sent automatically by browser
apiClient.interceptors.request.use(
  config => {
    if (typeof window !== 'undefined') {
      // @deprecated — Legacy localStorage fallback (migration period only).
      // HttpOnly cookies are the primary auth mechanism. This path will be
      // removed in a future release once all clients have migrated.
      const legacyToken = localStorage.getItem(ACCESS_TOKEN_KEY);
      if (legacyToken && config.headers) {
        console.warn(
          '[OKLA Security] Using deprecated localStorage token for auth. ' +
            'Tokens should be HttpOnly cookies. This fallback will be removed soon.'
        );
        config.headers.Authorization = `Bearer ${legacyToken}`;
      }

      // Add CSRF token for mutation requests (POST, PUT, PATCH, DELETE)
      const method = config.method?.toUpperCase();
      if (method && ['POST', 'PUT', 'PATCH', 'DELETE'].includes(method) && config.headers) {
        config.headers['X-CSRF-Token'] = getCsrfToken();
      }
    }
    return config;
  },
  error => {
    return Promise.reject(error);
  }
);

// Response interceptor - Handle errors and token refresh
apiClient.interceptors.response.use(
  (response: AxiosResponse) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as AxiosRequestConfig & {
      _retry?: boolean;
      _silentAuth?: boolean;
    };

    // Handle 401 Unauthorized - Try to refresh token
    // Skip the refresh/logout cascade for requests marked with _silentAuth
    if (error.response?.status === 401 && originalRequest._silentAuth) {
      return Promise.reject(error);
    }

    // Don't attempt token refresh for auth endpoints (login, register, etc.)
    // These return 401 for invalid credentials, not expired tokens
    const requestUrl = originalRequest.url || '';
    const isAuthEndpoint =
      requestUrl.includes('/api/auth/login') ||
      requestUrl.includes('/api/auth/register') ||
      requestUrl.includes('/api/auth/forgot-password') ||
      requestUrl.includes('/api/auth/reset-password') ||
      requestUrl.includes('/api/auth/verify-email');

    if (error.response?.status === 401 && !originalRequest._retry && !isAuthEndpoint) {
      originalRequest._retry = true;

      try {
        // Security (CWE-922): Refresh token is sent automatically via HttpOnly cookie.
        // The backend reads it from cookie, no need to send in body.
        const response = await axios.post(
          `${API_URL}/api/auth/refresh-token`,
          {}, // Empty body — refresh token comes from HttpOnly cookie
          { withCredentials: true }
        );

        const { accessToken: _accessToken } = response.data?.data || response.data || {};

        // Legacy cleanup: if there was a localStorage token, remove it
        if (typeof window !== 'undefined') {
          localStorage.removeItem(ACCESS_TOKEN_KEY);
          localStorage.removeItem(REFRESH_TOKEN_KEY);
        }

        // Retry the original request — new tokens are in HttpOnly cookies
        return apiClient(originalRequest);
      } catch (refreshError) {
        // Refresh failed - Clear any legacy tokens
        if (typeof window !== 'undefined') {
          localStorage.removeItem(ACCESS_TOKEN_KEY);
          localStorage.removeItem(REFRESH_TOKEN_KEY);
          // Clear legacy cookies set by JS
          document.cookie = 'auth-token=; path=/; max-age=0';
          document.cookie = 'refresh-token=; path=/; max-age=0';

          // Dispatch custom event for auth context to handle
          window.dispatchEvent(new CustomEvent('auth:logout'));
        }
        return Promise.reject(refreshError);
      }
    }

    // Transform error for consistent handling
    const apiError = transformError(error);
    return Promise.reject(apiError);
  }
);

// Error transformer
interface ApiErrorResponse {
  code: string;
  message: string;
  errors?: Array<{ field?: string; message: string }>;
  requiresKyc?: boolean;
  redirectUrl?: string;
}

function transformError(error: AxiosError): ApiErrorResponse {
  if (error.response?.data) {
    // Backend may return error details in different fields depending on the middleware
    // .NET ProblemDetails uses "detail", custom ApiResponse uses "message"
    const data = error.response.data as Record<string, unknown>;
    const message =
      (data.message as string) ||
      (data.detail as string) ||
      (data.title as string) ||
      getDefaultErrorMessage(error.response.status);
    const code =
      (data.code as string) || (data.errorCode as string) || `HTTP_${error.response.status}`;
    return {
      code,
      message,
      errors: data.errors as ApiErrorResponse['errors'],
      requiresKyc: data.requiresKyc as boolean | undefined,
      redirectUrl: data.redirectUrl as string | undefined,
    };
  }

  if (error.code === 'ECONNABORTED') {
    return {
      code: 'TIMEOUT',
      message: 'La solicitud tardó demasiado. Por favor, intenta de nuevo.',
    };
  }

  if (!error.response) {
    return {
      code: 'NETWORK_ERROR',
      message: 'Error de conexión. Verifica tu conexión a internet.',
    };
  }

  return {
    code: 'UNKNOWN_ERROR',
    message: 'Ocurrió un error inesperado. Por favor, intenta de nuevo.',
  };
}

function getDefaultErrorMessage(status: number): string {
  const messages: Record<number, string> = {
    400: 'Solicitud inválida. Verifica los datos enviados.',
    401: 'No autorizado. Por favor, inicia sesión.',
    403: 'No tienes permiso para realizar esta acción.',
    404: 'El recurso solicitado no fue encontrado.',
    409: 'Conflicto con el estado actual del recurso.',
    422: 'Los datos enviados no son válidos.',
    429: 'Demasiadas solicitudes. Por favor, espera un momento.',
    500: 'Error interno del servidor. Intenta más tarde.',
    502: 'Error de conexión con el servidor.',
    503: 'Servicio no disponible. Intenta más tarde.',
  };

  return messages[status] || 'Ocurrió un error inesperado.';
}

// Auth helpers
// Security (CWE-922): Tokens are now HttpOnly cookies set by the backend.
// These helpers are kept for backward compatibility during migration,
// but NO LONGER store tokens in localStorage or JS-accessible cookies.
export const authTokens = {
  /**
   * @deprecated Tokens are now set as HttpOnly cookies by the backend.
   * This method only cleans up legacy localStorage tokens.
   */
  setTokens: (_accessToken: string, _refreshToken: string) => {
    if (typeof window !== 'undefined') {
      // Clean up any legacy localStorage tokens (migration)
      localStorage.removeItem(ACCESS_TOKEN_KEY);
      localStorage.removeItem(REFRESH_TOKEN_KEY);
      // Legacy JS cookies cleanup
      document.cookie = 'auth-token=; path=/; max-age=0';
      document.cookie = 'refresh-token=; path=/; max-age=0';
    }
  },

  getAccessToken: (): string | null => {
    // Legacy fallback only — in normal operation, cookies are sent automatically
    if (typeof window !== 'undefined') {
      return localStorage.getItem(ACCESS_TOKEN_KEY);
    }
    return null;
  },

  getRefreshToken: (): string | null => {
    // Legacy fallback only
    if (typeof window !== 'undefined') {
      return localStorage.getItem(REFRESH_TOKEN_KEY);
    }
    return null;
  },

  clearTokens: () => {
    if (typeof window !== 'undefined') {
      // Clear any legacy localStorage tokens
      localStorage.removeItem(ACCESS_TOKEN_KEY);
      localStorage.removeItem(REFRESH_TOKEN_KEY);
      // Clear legacy JS cookies
      document.cookie = 'auth-token=; path=/; max-age=0';
      document.cookie = 'refresh-token=; path=/; max-age=0';
      // Note: HttpOnly cookies (okla_access_token, okla_refresh_token)
      // are cleared server-side by the logout endpoint.
    }
  },

  isAuthenticated: (): boolean => {
    // With HttpOnly cookies, we can't check the token directly.
    // The auth state is managed by the auth context via /api/auth/me.
    // TODO: Eventually remove this entirely — only use auth context/cookie-based auth.
    //
    // If tokens have been migrated (cleared from localStorage), don't check
    // localStorage anymore — it will always be empty post-migration.
    if (typeof window !== 'undefined') {
      if (localStorage.getItem(TOKEN_MIGRATED_KEY) === 'true') {
        // Migration complete — localStorage tokens were cleared.
        // Auth state should come from the auth context (useAuth hook),
        // not from this legacy helper.
        return false;
      }
      return !!localStorage.getItem(ACCESS_TOKEN_KEY);
    }
    return false;
  },
};

export default apiClient;
