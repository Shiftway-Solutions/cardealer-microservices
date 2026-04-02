/**
 * Login Page
 *
 * Features:
 * - Email/password login
 * - Remember me
 * - Forgot password link
 * - Social login (Google, Apple)
 * - Link to register
 */

'use client';

import * as React from 'react';
import Link from 'next/link';
import { useRouter, useSearchParams } from 'next/navigation';
import { Eye, EyeOff, Loader2, Mail, Lock, ShieldCheck } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Separator } from '@/components/ui/separator';
import { authService, TwoFactorRequiredError } from '@/services/auth';
import { createDealer } from '@/services/dealers';
import { useAuth } from '@/hooks/use-auth';
import { sanitizeEmail } from '@/lib/security/sanitize';
import type { User } from '@/types';

/**
 * Landing/marketing pages that should never be used as post-login destinations.
 * Authenticated users landing on these get redirected to their role-based portal.
 */
const MARKETING_PAGES = new Set([
  '/vender', // Seller marketing landing
  '/dealer', // Dealer marketing landing
  '/dealer/landing', // Dealer landing variant
  '/dealer/pricing', // Dealer pricing page
]);

/**
 * Guards against a callbackUrl that the user doesn't have permission to visit.
 *
 * Example: a seller navigates to /admin → middleware redirects to
 * /login?callbackUrl=/admin → after login the seller would be sent to /admin,
 * which the middleware then rejects with a 403. We skip the callbackUrl for
 * paths the user cannot access so they land on their own portal instead.
 */
function isRedirectAllowedForUser(user: User | null, redirectUrl: string): boolean {
  if (!user) return false;
  const type = user.accountType;

  // Admin-only section: only admin / platform_employee
  if (redirectUrl.startsWith('/admin')) {
    return type === 'admin' || type === 'platform_employee';
  }

  // Dealer section (data-heavy sub-pages): only dealer roles + admins
  if (redirectUrl.startsWith('/dealer')) {
    return (
      type === 'dealer' ||
      type === 'dealer_employee' ||
      type === 'admin' ||
      type === 'platform_employee'
    );
  }

  return true;
}

/**
 * Returns the correct post-login destination based on the user's accountType.
 * Respects the explicit redirectUrl only when the user has permission to access it,
 * and it is not the root '/' nor a marketing/landing page.
 */
function getPostLoginRedirect(user: User | null, redirectUrl: string): string {
  // If the user was heading somewhere specific — respect it, but validate role first.
  if (
    redirectUrl &&
    redirectUrl !== '/' &&
    !MARKETING_PAGES.has(redirectUrl) &&
    isRedirectAllowedForUser(user, redirectUrl)
  ) {
    return redirectUrl;
  }

  // Role-based default portal
  switch (user?.accountType) {
    case 'seller':
      return '/cuenta';
    case 'dealer':
    case 'dealer_employee':
      return '/dealer/dashboard'; // Direct to dealer portal, no double-hop
    case 'admin':
    case 'platform_employee':
      return '/admin';
    default:
      // buyer, guest → home
      return '/';
  }
}

function LoginForm({ redirectUrl, sessionExpired }: { redirectUrl: string; sessionExpired?: boolean }) {
  const router = useRouter();
  const auth = useAuth();
  const { login, verifyTwoFactorLogin } = auth;

  const [isLoading, setIsLoading] = React.useState(false);
  const [showPassword, setShowPassword] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);

  // 2FA state
  const [twoFactorState, setTwoFactorState] = React.useState<{
    required: boolean;
    tempToken: string;
    twoFactorType: string;
  } | null>(null);
  const [twoFactorCode, setTwoFactorCode] = React.useState('');

  const [formData, setFormData] = React.useState({
    email: '',
    password: '',
    rememberMe: false,
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsLoading(true);

    try {
      // Use the auth context login to update global state
      const { user: loggedUser } = await login({
        email: sanitizeEmail(formData.email),
        password: formData.password, // Password NOT sanitized per security policy
        rememberMe: formData.rememberMe,
      });

      // Redirect after successful login — use the user returned by login() directly
      // to avoid the React state update race condition (auth.user still stale here)
      const finalRedirect = getPostLoginRedirect(loggedUser, redirectUrl);

      // If this is a dealer who registered with pending profile data, create it now
      if (loggedUser?.accountType === 'dealer' && typeof window !== 'undefined') {
        const pendingDealer = localStorage.getItem('pending-dealer-registration');
        if (pendingDealer) {
          try {
            const dealerData = JSON.parse(pendingDealer);
            await createDealer(dealerData);
          } catch {
            // Dealer profile creation failed — user can set it up from their dashboard
          } finally {
            localStorage.removeItem('pending-dealer-registration');
          }
        }
      }

      router.push(finalRedirect);
      router.refresh();
    } catch (err) {
      // If 2FA is required, show the 2FA code form
      if (err instanceof TwoFactorRequiredError) {
        setTwoFactorState({
          required: true,
          tempToken: err.tempToken,
          twoFactorType: err.twoFactorType,
        });
        setIsLoading(false);
        return;
      }
      const error = err as { message?: string };
      setError(error.message || 'Error al iniciar sesión. Verifica tus credenciales.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleTwoFactorSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsLoading(true);

    try {
      const { user: loggedUser2fa } = await verifyTwoFactorLogin(
        twoFactorState!.tempToken,
        twoFactorCode
      );

      // Redirect after successful 2FA verification
      const finalRedirect2 = getPostLoginRedirect(loggedUser2fa, redirectUrl);
      router.push(finalRedirect2);
      router.refresh();
    } catch (err) {
      const error = err as { message?: string };
      setError(error.message || 'Código de verificación inválido. Intenta de nuevo.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleBackToLogin = () => {
    setTwoFactorState(null);
    setTwoFactorCode('');
    setError(null);
  };

  const handleSocialLogin = (provider: 'google' | 'apple') => {
    authService.loginWithProvider(provider);
  };

  // ── 2FA Code Form ──────────────────────────────────────────
  if (twoFactorState?.required) {
    return (
      <div className="space-y-6">
        <div className="space-y-2 text-center">
          <div className="bg-primary/10 mx-auto flex h-12 w-12 items-center justify-center rounded-full">
            <ShieldCheck className="text-primary h-6 w-6" />
          </div>
          <h1 className="text-foreground text-2xl font-bold">Verificación en dos pasos</h1>
          <p className="text-muted-foreground">
            Ingresa el código de 6 dígitos de tu aplicación autenticadora
          </p>
        </div>

        <form onSubmit={handleTwoFactorSubmit} className="space-y-4">
          {error && (
            <div className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
              {error}
            </div>
          )}

          <div className="space-y-2">
            <Label htmlFor="twoFactorCode">Código de verificación</Label>
            <Input
              id="twoFactorCode"
              type="text"
              inputMode="numeric"
              pattern="[0-9]*"
              maxLength={6}
              placeholder="000000"
              value={twoFactorCode}
              onChange={e => {
                const value = e.target.value.replace(/\D/g, '');
                setTwoFactorCode(value);
              }}
              required
              autoFocus
              autoComplete="one-time-code"
              disabled={isLoading}
              className="text-center text-2xl tracking-[0.5em]"
            />
          </div>

          <Button
            type="submit"
            className="w-full"
            disabled={isLoading || twoFactorCode.length !== 6}
          >
            {isLoading ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Verificando...
              </>
            ) : (
              'Verificar'
            )}
          </Button>

          <button
            type="button"
            onClick={handleBackToLogin}
            className="text-muted-foreground hover:text-foreground w-full text-center text-sm"
          >
            ← Volver al inicio de sesión
          </button>
        </form>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="space-y-2 text-center">
        <h1 className="text-foreground text-2xl font-bold">Bienvenido de vuelta</h1>
        <p className="text-muted-foreground">Ingresa a tu cuenta para continuar</p>
      </div>

      {/* Social Login */}
      <div className="grid grid-cols-2 gap-3">
        <Button
          variant="outline"
          onClick={() => handleSocialLogin('google')}
          disabled={isLoading}
          className="gap-2"
        >
          <svg className="h-5 w-5" viewBox="0 0 24 24">
            <path
              fill="currentColor"
              d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
            />
            <path
              fill="currentColor"
              d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
            />
            <path
              fill="currentColor"
              d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
            />
            <path
              fill="currentColor"
              d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
            />
          </svg>
          Google
        </Button>
        <Button
          variant="outline"
          onClick={() => handleSocialLogin('apple')}
          disabled={isLoading}
          className="gap-2"
        >
          <svg className="h-5 w-5" viewBox="0 0 24 24" fill="currentColor">
            <path d="M17.05 20.28c-.98.95-2.05.88-3.08.41-1.09-.47-2.09-.48-3.24 0-1.44.62-2.2.44-3.06-.41C2.79 15.25 3.51 7.59 9.05 7.31c1.35.07 2.29.74 3.08.8 1.18-.24 2.31-.93 3.57-.84 1.51.12 2.65.72 3.4 1.8-3.12 1.87-2.38 5.98.48 7.13-.57 1.5-1.31 2.99-2.54 4.09l.01-.01zM12.03 7.25c-.15-2.23 1.66-4.07 3.74-4.25.29 2.58-2.34 4.5-3.74 4.25z" />
          </svg>
          Apple
        </Button>
      </div>

      {/* Divider */}
      <div className="relative">
        <div className="absolute inset-0 flex items-center">
          <Separator className="w-full" />
        </div>
        <div className="relative flex justify-center text-xs uppercase">
          <span className="bg-muted/50 text-muted-foreground px-2">o continúa con email</span>
        </div>
      </div>

      {/* Login Form */}
      <form onSubmit={handleSubmit} className="space-y-4">
        {/* Session expired banner */}
        {sessionExpired && (
          <div className="rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-700">
            Tu sesión ha expirado. Por favor inicia sesión nuevamente.
          </div>
        )}

        {/* Error message */}
        {error && (
          <div className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
            {error}
          </div>
        )}

        {/* Email */}
        <div className="space-y-2">
          <Label htmlFor="email">Email</Label>
          <div className="relative">
            <Mail className="text-muted-foreground absolute top-1/2 left-3 h-5 w-5 -translate-y-1/2" />
            <Input
              id="email"
              type="email"
              placeholder="tu@email.com"
              value={formData.email}
              onChange={e => setFormData({ ...formData, email: e.target.value })}
              required
              autoComplete="email"
              disabled={isLoading}
              className="pl-10"
            />
          </div>
        </div>

        {/* Password */}
        <div className="space-y-2">
          <Label htmlFor="password">Contraseña</Label>
          <div className="relative">
            <Lock className="text-muted-foreground absolute top-1/2 left-3 h-5 w-5 -translate-y-1/2" />
            <Input
              id="password"
              type={showPassword ? 'text' : 'password'}
              placeholder="••••••••"
              value={formData.password}
              onChange={e => setFormData({ ...formData, password: e.target.value })}
              required
              autoComplete="current-password"
              disabled={isLoading}
              className="pr-10 pl-10"
            />
            <button
              type="button"
              onClick={() => setShowPassword(!showPassword)}
              className="text-muted-foreground hover:text-muted-foreground absolute top-1/2 right-3 -translate-y-1/2"
              tabIndex={-1}
              aria-label={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'}
            >
              {showPassword ? <EyeOff className="h-5 w-5" /> : <Eye className="h-5 w-5" />}
            </button>
          </div>
        </div>

        {/* Remember me & Forgot password */}
        <div className="flex items-center justify-between">
          <label className="flex cursor-pointer items-center gap-2">
            <input
              type="checkbox"
              checked={formData.rememberMe}
              onChange={e => setFormData({ ...formData, rememberMe: e.target.checked })}
              className="text-primary focus:ring-primary border-border h-4 w-4 rounded"
            />
            <span className="text-muted-foreground text-sm">Recordarme</span>
          </label>
          <Link href="/recuperar-contrasena" className="text-primary text-sm hover:underline">
            ¿Olvidaste tu contraseña?
          </Link>
        </div>

        {/* Submit button */}
        <Button type="submit" className="w-full" disabled={isLoading}>
          {isLoading ? (
            <>
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              Iniciando sesión...
            </>
          ) : (
            'Iniciar sesión'
          )}
        </Button>
      </form>

      {/* Register link */}
      <p className="text-muted-foreground text-center text-sm">
        ¿No tienes cuenta?{' '}
        <Link href="/registro" className="text-primary font-medium hover:underline">
          Regístrate gratis
        </Link>
      </p>
    </div>
  );
}

function LoginPageContent() {
  const searchParams = useSearchParams();
  // Support both 'redirect' (from AdminAuthGuard) and 'callbackUrl' (from middleware)
  const rawRedirect = searchParams.get('redirect') || searchParams.get('callbackUrl') || '/';

  // Security: Prevent open redirect (CWE-601) — only allow relative paths
  const redirectUrl =
    rawRedirect.startsWith('/') && !rawRedirect.startsWith('//') ? rawRedirect : '/';

  const reason = searchParams.get('reason');

  return <LoginForm redirectUrl={redirectUrl} sessionExpired={reason === 'session_expired'} />;
}

export default function LoginPage() {
  return (
    <React.Suspense
      fallback={
        <div className="flex items-center justify-center py-12">
          <Loader2 className="text-primary h-8 w-8 animate-spin" />
        </div>
      }
    >
      <LoginPageContent />
    </React.Suspense>
  );
}
