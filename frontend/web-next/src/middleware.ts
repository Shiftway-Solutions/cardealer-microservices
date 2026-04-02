/**
 * Next.js Middleware
 *
 * Handles authentication and route protection
 * Based on the routing-map.md specification
 *
 * SECURITY NOTES:
 * - JWT verification should be done server-side via API
 * - This middleware only does client-side checks for UX
 * - Critical operations must be verified by backend
 */

import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

// =============================================================================
// CONSTANTS
// =============================================================================

const AUTH_COOKIE_NAME = 'okla_access_token';
const REFRESH_COOKIE_NAME = 'okla_refresh_token';
const MAINTENANCE_CACHE_COOKIE = 'maintenance-status';
const MAINTENANCE_CACHE_TTL = 60; // seconds
const LOGIN_PATH = '/login';
const ACCOUNT_PATH = '/cuenta';
const FORBIDDEN_PATH = '/403';
const MAINTENANCE_PATH = '/mantenimiento';
const MAINTENANCE_API_URL =
  (process.env.INTERNAL_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:18443') +
  '/api/maintenance/status';

// =============================================================================
// ROUTE DEFINITIONS
// =============================================================================

// Public routes (no auth required)
const publicRoutes = [
  '/',
  '/vehiculos',
  '/vehiculos/[^/]+$', // /vehiculos/[slug]
  '/vehiculos/[^/]+/360$', // /vehiculos/[slug]/360
  '/buscar',
  '/dealers',
  '/dealers/[^/]+$', // /dealers/[slug]
  '/comparar',
  '/ayuda',
  '/ayuda/[^/]+$', // /ayuda/[category]
  '/ayuda/articulo/[^/]+$', // /ayuda/articulo/[slug]
  '/contacto',
  '/about',
  '/nosotros',
  '/terminos',
  '/privacidad',
  '/cookies',
  '/seguridad',
  '/politica-reembolso',
  '/reclamaciones',
  '/okla-score',
  '/reportar',
  '/empleos',
  '/prensa',
  '/precios',
  '/guias',
  '/guias/[^/]+$', // /guias/[slug]
  '/blog',
  '/blog/[^/]+$', // /blog/[slug]
  '/marcas',
  '/marcas/[^/]+$', // /marcas/[marca]
  '/herramientas',
  '/herramientas/[^/]+$', // /herramientas/calculadora-financiamiento, etc.
  '/faq',
  '/vender', // Landing page is public
  '/auth/callback', // OAuth callback routes (must be public)
  '/mantenimiento', // Maintenance page always accessible
];

// Guest-only routes (redirect if authenticated)
const guestOnlyRoutes = [
  '/login',
  '/registro',
  '/recuperar-contrasena',
  '/restablecer-contrasena',
  '/reset-password', // English alias for restablecer-contrasena (from email links)
  '/forgot-password', // English alias for recuperar-contrasena (from email links)
  '/crear-contrasena',
];

// Routes accessible by both guests AND authenticated users with unverified email
const verificationRoutes = ['/verificar-email', '/verify-email'];

// Role-protected routes (requires specific roles)
const roleProtectedRoutes: Record<string, string[]> = {
  '/dealer': ['dealer', 'dealer_employee', 'admin'],
  '/dealer/inventario': ['dealer', 'dealer_employee', 'admin'],
  '/dealer/analytics': ['dealer', 'dealer_employee', 'admin'],
  '/dealer/leads': ['dealer', 'dealer_employee', 'admin'],
  '/dealer/mensajes': ['dealer', 'dealer_employee', 'admin'],
  '/dealer/perfil': ['dealer', 'dealer_employee', 'admin'],
  '/dealer/facturacion': ['dealer', 'dealer_employee', 'admin'],
  '/dealer/suscripcion': ['dealer', 'dealer_employee', 'admin'],
  '/admin': ['admin', 'platform_employee'],
  '/admin/usuarios': ['admin', 'platform_employee'],
  '/admin/vehiculos': ['admin', 'platform_employee'],
  '/admin/dealers': ['admin', 'platform_employee'],
  '/admin/reportes': ['admin', 'platform_employee'],
  '/publicar': ['user', 'seller', 'dealer', 'dealer_employee', 'admin'],
  '/mis-vehiculos': ['user', 'seller', 'dealer', 'dealer_employee', 'admin'],
  '/cuenta': ['user', 'buyer', 'seller', 'dealer', 'dealer_employee', 'admin', 'platform_employee'],
  '/dashboard': ['user', 'buyer', 'seller', 'dealer', 'dealer_employee'],
  '/checkout': ['user', 'buyer', 'seller', 'dealer', 'dealer_employee', 'admin'],
  '/mensajes': [
    'user',
    'buyer',
    'seller',
    'dealer',
    'dealer_employee',
    'admin',
    'platform_employee',
  ],
};

// Routes that require authentication (any role)
const authenticatedRoutes = [
  '/cuenta',
  '/cuenta/perfil',
  '/cuenta/seguridad',
  '/cuenta/favoritos',
  '/cuenta/alertas',
  '/cuenta/busquedas',
  '/cuenta/mensajes',
  '/cuenta/notificaciones',
  '/cuenta/historial',
  '/publicar',
  '/publicar/fotos',
  '/publicar/preview',
  '/mis-vehiculos',
  '/checkout',
  '/dashboard',
  '/mensajes',
];

// =============================================================================
// TYPES
// =============================================================================

interface TokenPayload {
  sub: string;
  email: string;
  role: string | string[]; // Can be single role or array of roles
  exp: number;
  iat: number;
  dealerId?: string;
  emailVerified?: boolean;
}

// =============================================================================
// HELPERS
// =============================================================================

function isPublicRoute(pathname: string): boolean {
  return publicRoutes.some(route => {
    // Handle dynamic routes with regex
    if (route.includes('[^/]')) {
      const regex = new RegExp(`^${route.replace(/\[slug\]/g, '[^/]+')}$`);
      return regex.test(pathname);
    }
    // Exact match for home route to prevent matching all paths
    if (route === '/') {
      return pathname === '/';
    }
    return pathname === route || pathname.startsWith(`${route}/`);
  });
}

function isGuestOnlyRoute(pathname: string): boolean {
  return guestOnlyRoutes.some(route => pathname === route || pathname.startsWith(`${route}/`));
}

function isAuthenticatedRoute(pathname: string): boolean {
  return authenticatedRoutes.some(route => pathname === route || pathname.startsWith(`${route}/`));
}

function getRequiredRoles(pathname: string): string[] | null {
  for (const [routePrefix, roles] of Object.entries(roleProtectedRoutes)) {
    if (pathname === routePrefix || pathname.startsWith(`${routePrefix}/`)) {
      return roles;
    }
  }
  return null;
}

/**
 * Decode JWT token payload
 * NOTE: Full cryptographic signature verification happens server-side (Gateway/backend).
 * This middleware only decodes the payload for routing decisions (roles, expiry).
 * Edge Runtime does not support Node.js crypto APIs needed for HMAC verification.
 * Security is enforced by: 1) Backend validates signatures on every API call,
 * 2) This middleware checks token structure and expiry for UX routing only.
 */
function decodeToken(token: string): TokenPayload | null {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;

    // Use Edge-compatible base64url decoding
    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );
    const payload = JSON.parse(jsonPayload);
    return {
      sub:
        payload.sub ||
        payload.userId ||
        payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ||
        '',
      email:
        payload.email ||
        payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] ||
        '',
      role:
        payload.role ||
        payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
        'user',
      exp: payload.exp || 0,
      iat: payload.iat || 0,
      dealerId: payload.dealerId,
      emailVerified: (() => {
        const val = payload.emailVerified ?? payload.email_verified;
        if (typeof val === 'boolean') return val;
        if (typeof val === 'string') return val.toLowerCase() === 'true';
        return undefined;
      })(),
    };
  } catch {
    return null;
  }
}

/**
 * Check if token is expired (with 60 second buffer)
 */
function isTokenExpired(payload: TokenPayload): boolean {
  const now = Math.floor(Date.now() / 1000);
  const buffer = 60; // 60 second buffer
  return payload.exp < now + buffer;
}

/**
 * Create redirect response with security headers
 */
function createRedirect(url: URL, request: NextRequest): NextResponse {
  // Use 303 (See Other) for POST/PUT/PATCH/DELETE so the browser follows with GET.
  // Default 307 preserves the HTTP method, which causes 404 when a POST is
  // redirected to a page route that only handles GET (e.g. /dealer/dashboard).
  const status = request.method !== 'GET' && request.method !== 'HEAD' ? 303 : 307;
  const response = NextResponse.redirect(url, status);
  addSecurityHeaders(response);
  return response;
}

/**
 * Add security headers to response
 */
function addSecurityHeaders(response: NextResponse): void {
  // Prevent clickjacking – skip in development so VS Code Simple Browser (iframe) works
  if (process.env.NODE_ENV !== 'development') {
    response.headers.set('X-Frame-Options', 'DENY');
  }

  // Prevent MIME type sniffing
  response.headers.set('X-Content-Type-Options', 'nosniff');

  // Enable XSS filter
  response.headers.set('X-XSS-Protection', '1; mode=block');

  // Referrer policy
  response.headers.set('Referrer-Policy', 'strict-origin-when-cross-origin');

  // HSTS - enforce HTTPS (1 year, include subdomains)
  response.headers.set('Strict-Transport-Security', 'max-age=31536000; includeSubDomains; preload');

  // Permissions policy - allow camera and microphone for KYC verification
  response.headers.set(
    'Permissions-Policy',
    'camera=(self), microphone=(self), geolocation=(self), payment=(self)'
  );
}

/**
 * Clear auth cookies and redirect to login
 */
function clearAuthAndRedirect(request: NextRequest, pathname: string): NextResponse {
  const loginUrl = new URL(LOGIN_PATH, request.url);
  // SEM FIX: Preserve query params (including UTMs) in callbackUrl during auth redirects
  const searchParams = request.nextUrl.searchParams.toString();
  const callbackPath = searchParams ? `${pathname}?${searchParams}` : pathname;
  loginUrl.searchParams.set('callbackUrl', callbackPath);
  loginUrl.searchParams.set('reason', 'session_expired');

  const response = NextResponse.redirect(loginUrl);
  response.cookies.delete(AUTH_COOKIE_NAME);
  response.cookies.delete(REFRESH_COOKIE_NAME);
  addSecurityHeaders(response);

  return response;
}

// =============================================================================
// MIDDLEWARE
// =============================================================================

export async function middleware(request: NextRequest) {
  const { pathname, searchParams } = request.nextUrl;

  // Skip static files and API routes
  if (
    pathname.startsWith('/_next') ||
    pathname.startsWith('/api') ||
    pathname.startsWith('/images') ||
    pathname.startsWith('/fonts') ||
    pathname.includes('.') // static files
  ) {
    return NextResponse.next();
  }

  // Skip Server Action requests (POST with Next-Action header).
  // Server Actions handle their own auth. Middleware redirects break
  // the response format causing "An unexpected response was received from the server."
  if (request.headers.get('next-action')) {
    const response = NextResponse.next();
    addSecurityHeaders(response);
    return response;
  }

  // Handle maintenance page: redirect to home if maintenance is NOT active
  if (pathname === MAINTENANCE_PATH) {
    try {
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), 3000);

      const statusRes = await fetch(MAINTENANCE_API_URL, {
        signal: controller.signal,
        headers: { Accept: 'application/json' },
      });
      clearTimeout(timeoutId);

      if (statusRes.ok) {
        const statusData = await statusRes.json();
        if (statusData.isMaintenanceMode !== true) {
          // Maintenance is OFF — redirect to home
          const homeUrl = new URL('/', request.url);
          const response = NextResponse.redirect(homeUrl);
          addSecurityHeaders(response);
          return response;
        }
      }
    } catch {
      // If service unreachable, redirect to home (fail open)
      const homeUrl = new URL('/', request.url);
      const response = NextResponse.redirect(homeUrl);
      addSecurityHeaders(response);
      return response;
    }

    const response = NextResponse.next();
    addSecurityHeaders(response);
    return response;
  }

  // Get auth token from cookie
  const token = request.cookies.get(AUTH_COOKIE_NAME)?.value;
  const refreshToken = request.cookies.get(REFRESH_COOKIE_NAME)?.value;

  // Decode token and extract payload
  let tokenPayload: TokenPayload | null = null;
  let isAuthenticated = false;
  let isExpired = false;

  if (token) {
    tokenPayload = decodeToken(token);
    if (tokenPayload) {
      isExpired = isTokenExpired(tokenPayload);
      isAuthenticated = !isExpired;

      // Token decoded successfully
    }
  }

  // If token is expired but we have refresh token, allow access to
  // public/guest routes but redirect protected routes to login with callbackUrl
  // so the client-side auth hook can attempt token refresh
  if (isExpired && refreshToken) {
    if (isPublicRoute(pathname) || isGuestOnlyRoute(pathname)) {
      const response = NextResponse.next();
      response.headers.set('X-Token-Status', 'expired');
      addSecurityHeaders(response);
      return response;
    }
    // For protected routes, still allow through but mark as expired
    // Client-side will handle refresh via auth hook interceptor
    const response = NextResponse.next();
    response.headers.set('X-Token-Status', 'expired');
    response.headers.set('X-Token-Needs-Refresh', 'true');
    addSecurityHeaders(response);
    return response;
  }

  // =========================================================================
  // MAINTENANCE MODE CHECK
  // =========================================================================
  // Check if the platform is in maintenance mode.
  // Admins bypass maintenance mode to manage the platform.
  // =========================================================================
  const isAdmin = (() => {
    if (!tokenPayload) return false;
    const userRoles: string[] = Array.isArray(tokenPayload.role)
      ? tokenPayload.role.map(r => r.toLowerCase())
      : [tokenPayload.role.toLowerCase()];
    return userRoles.includes('admin') || userRoles.includes('platform_employee');
  })();

  if (!isAdmin) {
    // Use cached maintenance status from cookie to avoid HTTP call on every navigation
    const cachedMaintenance = request.cookies.get(MAINTENANCE_CACHE_COOKIE)?.value;
    let isMaintenanceMode = false;

    if (cachedMaintenance !== undefined) {
      // Use cached value ("1" = maintenance ON, "0" = maintenance OFF)
      isMaintenanceMode = cachedMaintenance === '1';
    } else {
      // No cache — fetch from API and cache the result
      try {
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 2000);

        const maintenanceRes = await fetch(MAINTENANCE_API_URL, {
          signal: controller.signal,
          headers: { Accept: 'application/json' },
        });
        clearTimeout(timeoutId);

        if (maintenanceRes.ok) {
          const maintenanceData = await maintenanceRes.json();
          isMaintenanceMode = maintenanceData.isMaintenanceMode === true;
        }
      } catch (err) {
        // If MaintenanceService is unreachable, fail open (allow access)
        console.warn(
          '⚠️ MaintenanceService check failed (fail-open):',
          err instanceof Error ? err.message : 'unknown'
        );
      }
    }

    if (isMaintenanceMode) {
      const maintenanceUrl = new URL(MAINTENANCE_PATH, request.url);
      const response = NextResponse.redirect(maintenanceUrl);
      // Cache maintenance=ON for shorter TTL to recover faster
      response.cookies.set(MAINTENANCE_CACHE_COOKIE, '1', {
        maxAge: 15,
        path: '/',
        httpOnly: true,
        sameSite: 'lax',
      });
      addSecurityHeaders(response);
      return response;
    }
  }

  // If token is expired without refresh token, clear and redirect
  if (isExpired && !refreshToken && isAuthenticatedRoute(pathname)) {
    return clearAuthAndRedirect(request, pathname);
  }

  // Check public routes
  if (isPublicRoute(pathname)) {
    const response = NextResponse.next();
    addSecurityHeaders(response);
    return response;
  }

  // Check verification routes (accessible by guests AND authenticated with unverified email)
  if (verificationRoutes.some(r => pathname === r || pathname.startsWith(`${r}/`))) {
    // If authenticated with verified email, redirect to role-based portal (already verified)
    if (isAuthenticated && tokenPayload?.emailVerified !== false) {
      const roles: string[] = Array.isArray(tokenPayload?.role)
        ? (tokenPayload.role as string[]).map(r => r.toLowerCase())
        : [((tokenPayload?.role as string) || '').toLowerCase()];
      let destPath = ACCOUNT_PATH;
      if (roles.includes('admin') || roles.includes('platform_employee')) {
        destPath = '/admin';
      } else if (roles.includes('dealer') || roles.includes('dealer_employee')) {
        destPath = '/dealer/dashboard';
      }
      return createRedirect(new URL(destPath, request.url), request);
    }
    // Allow access for guests and unverified users
    const response = NextResponse.next();
    addSecurityHeaders(response);
    return response;
  }

  // Check guest-only routes
  if (isGuestOnlyRoute(pathname)) {
    if (isAuthenticated && tokenPayload) {
      // Role-aware redirect: admins → /admin, dealers → /dealer/dashboard, others → /cuenta
      const roles: string[] = Array.isArray(tokenPayload.role)
        ? tokenPayload.role.map(r => r.toLowerCase())
        : [tokenPayload.role.toLowerCase()];
      let destPath = ACCOUNT_PATH;
      if (roles.includes('admin') || roles.includes('platform_employee')) {
        destPath = '/admin';
      } else if (roles.includes('dealer') || roles.includes('dealer_employee')) {
        destPath = '/dealer/dashboard';
      }
      return createRedirect(new URL(destPath, request.url), request);
    }
    const response = NextResponse.next();
    addSecurityHeaders(response);
    return response;
  }

  // Unknown routes (not in any known route list) — pass through to Next.js 404 handler.
  // This prevents the "deny-by-default" behavior that would redirect users who type
  // a non-existent URL to the login page instead of showing a 404 page.
  const isKnownProtectedRoute =
    isAuthenticatedRoute(pathname) || getRequiredRoles(pathname) !== null;

  if (!isKnownProtectedRoute) {
    const response = NextResponse.next();
    addSecurityHeaders(response);
    return response;
  }

  // All known protected routes require authentication
  if (!isAuthenticated) {
    const loginUrl = new URL(LOGIN_PATH, request.url);
    loginUrl.searchParams.set('callbackUrl', pathname);

    // Preserve any query params in the callback
    if (searchParams.toString()) {
      loginUrl.searchParams.set('callbackUrl', `${pathname}?${searchParams.toString()}`);
    }

    return createRedirect(loginUrl, request);
  }

  // =========================================================================
  // EMAIL VERIFICATION GATE
  // =========================================================================
  // Users with unverified email can only access public/guest routes and
  // the verification page itself. All other routes redirect to /verificar-email.
  // This applies to users who registered via form (NOT OAuth/Google/Apple).
  // =========================================================================
  if (isAuthenticated && tokenPayload) {
    if (tokenPayload.emailVerified === false) {
      // Allow access to verificar-email page so they can verify
      if (!pathname.startsWith('/verificar-email')) {
        const verifyUrl = new URL('/verificar-email', request.url);
        verifyUrl.searchParams.set('email', tokenPayload.email);
        return createRedirect(verifyUrl, request);
      }
    }
  }

  // Check role-protected routes
  const requiredRoles = getRequiredRoles(pathname);
  if (requiredRoles && tokenPayload) {
    // Handle role as string or array (user can have multiple roles)
    const userRoles: string[] = Array.isArray(tokenPayload.role)
      ? tokenPayload.role.map(r => r.toLowerCase())
      : [tokenPayload.role.toLowerCase()];

    // Check if user has ANY of the required roles
    const hasRequiredRole = userRoles.some(role => requiredRoles.includes(role));
    if (!hasRequiredRole) {
      return createRedirect(new URL(FORBIDDEN_PATH, request.url), request);
    }

    // Do not redirect dealer portal pages based only on dealerId in the JWT.
    // The claim can be stale or absent until the token refreshes after onboarding,
    // and portal pages already render explicit onboarding or empty states.
  }

  // Add user info headers for server components
  const response = NextResponse.next();

  // Cache maintenance=OFF for 60s so subsequent navigations skip the API call
  if (!isAdmin && !request.cookies.get(MAINTENANCE_CACHE_COOKIE)?.value) {
    response.cookies.set(MAINTENANCE_CACHE_COOKIE, '0', {
      maxAge: MAINTENANCE_CACHE_TTL,
      path: '/',
      httpOnly: true,
      sameSite: 'lax',
    });
  }

  if (tokenPayload) {
    response.headers.set('X-User-Id', tokenPayload.sub);
    // Handle role as string or array
    const roleHeader = Array.isArray(tokenPayload.role)
      ? tokenPayload.role.join(',')
      : tokenPayload.role;
    response.headers.set('X-User-Role', roleHeader);
    if (tokenPayload.dealerId) {
      response.headers.set('X-Dealer-Id', tokenPayload.dealerId);
    }
  }
  addSecurityHeaders(response);

  return response;
}

// =============================================================================
// CONFIG
// =============================================================================

export const config = {
  matcher: [
    /*
     * Match all request paths except:
     * - _next/static (static files)
     * - _next/image (image optimization files)
     * - favicon.ico (favicon file)
     * - public folder
     */
    '/((?!_next/static|_next/image|favicon.ico|images|fonts|.*\\..*).*)',
  ],
};
