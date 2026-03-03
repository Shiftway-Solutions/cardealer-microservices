'use client';

import { useEffect, useState, useRef } from 'react';
import { useRouter, useSearchParams, useParams } from 'next/navigation';
import { Loader2 } from 'lucide-react';
import { useAuth } from '@/hooks/use-auth';

/**
 * Security (CWE-601): Validates that a redirect URL is a safe relative path.
 * Blocks absolute URLs, protocol-relative URLs, and javascript: schemes.
 */
function isValidRedirect(url: string): boolean {
  if (!url || typeof url !== 'string') return false;
  const trimmed = url.trim();
  // Must start with / (relative path) but NOT // (protocol-relative)
  if (!trimmed.startsWith('/') || trimmed.startsWith('//')) return false;
  // Block javascript:, data:, vbscript: schemes
  const lower = trimmed.toLowerCase();
  if (lower.includes('javascript:') || lower.includes('data:') || lower.includes('vbscript:'))
    return false;
  // Block encoded schemes
  if (lower.includes('%6a%61%76%61') || lower.includes('%00')) return false;
  return true;
}

export default function OAuthCallbackPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const params = useParams();
  const provider = params.provider as string;
  const [error, setError] = useState<string | null>(null);
  const [_isProcessing, setIsProcessing] = useState(true);
  const { refreshUser } = useAuth();

  // Prevent duplicate calls (React StrictMode causes useEffect to run twice)
  const hasProcessed = useRef(false);

  useEffect(() => {
    const handleCallback = async () => {
      // Prevent duplicate processing
      if (hasProcessed.current) {
        console.log('OAuth callback already processed, skipping...');
        return;
      }
      hasProcessed.current = true;

      const code = searchParams.get('code');
      const errorParam = searchParams.get('error');
      const errorDescription = searchParams.get('error_description');

      // Handle OAuth errors from provider
      if (errorParam) {
        setError(errorDescription || `Error de autenticación: ${errorParam}`);
        setIsProcessing(false);
        return;
      }

      if (!code) {
        setError('No se recibió el código de autorización');
        setIsProcessing(false);
        return;
      }

      try {
        // Send the authorization code to our backend to exchange for tokens
        // BFF pattern: empty NEXT_PUBLIC_API_URL in prod = same-origin, proxied by Next.js rewrites
        const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:18443';
        const response = await fetch(`${apiUrl}/api/auth/oauth/${provider}/callback`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          credentials: 'same-origin',
          body: JSON.stringify({
            code: code,
            // Support both old (/auth/callback/) and new (/callback/) paths
            redirectUri: `${window.location.origin}/callback/${provider}`,
          }),
        });

        const data = await response.json();

        // Security: Do NOT log token data
        if (!response.ok) {
          console.error('OAuth authentication failed with status:', response.status);
          // Provide more helpful error messages based on common OAuth errors
          let errorMessage = data.message || data.error || 'Error al autenticar con el proveedor';

          if (data.message?.includes('invalid_grant') || errorMessage.includes('invalid_grant')) {
            errorMessage =
              'El código de autorización ha expirado o ya fue utilizado. Por favor, intenta iniciar sesión nuevamente.';
          } else if (data.message?.includes('redirect_uri')) {
            errorMessage =
              'Error de configuración: La URL de redirección no coincide. Contacta al soporte.';
          }

          console.error('OAuth error:', errorMessage);
          throw new Error(errorMessage);
        }

        // Handle both wrapped ApiResponse and direct response formats
        const tokenData = data.data || data;

        // Security (CWE-922): Tokens are now HttpOnly cookies set by the backend.
        // No localStorage or JS cookie storage needed.
        if (tokenData?.accessToken) {
          // Clean up any legacy localStorage tokens
          localStorage.removeItem('okla_access_token');
          localStorage.removeItem('okla_refresh_token');
          // Clear legacy JS cookies
          document.cookie = 'auth-token=; path=/; max-age=0';
          document.cookie = 'refresh-token=; path=/; max-age=0';

          // Refresh auth state to load the user (HttpOnly cookies are sent automatically)
          await refreshUser();

          // Security (CWE-601): Validate redirect URL is a safe relative path
          const rawRedirect = localStorage.getItem('authRedirectTo') || '/';
          localStorage.removeItem('authRedirectTo');
          const safeRedirect = isValidRedirect(rawRedirect) ? rawRedirect : '/';
          router.push(safeRedirect);
        } else {
          console.error('No tokens received from OAuth callback');
          throw new Error('No se recibieron tokens de autenticación');
        }
      } catch (err) {
        console.error('OAuth callback error:', err);
        setError(err instanceof Error ? err.message : 'Error al completar la autenticación');
        setIsProcessing(false);
      }
    };

    handleCallback();
  }, [provider, searchParams, router, refreshUser]);

  if (error) {
    return (
      <div className="bg-muted/50 flex min-h-screen items-center justify-center">
        <div className="bg-card w-full max-w-md rounded-lg p-8 text-center shadow-lg">
          <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-red-100">
            <svg
              className="h-8 w-8 text-red-600"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M6 18L18 6M6 6l12 12"
              />
            </svg>
          </div>
          <h1 className="text-foreground mb-2 text-xl font-semibold">Error de Autenticación</h1>
          <p className="text-muted-foreground mb-6">{error}</p>
          <button
            onClick={() => router.push('/login')}
            className="rounded-lg bg-blue-600 px-6 py-2 text-white transition-colors hover:bg-blue-700"
          >
            Volver al Login
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-muted/50 flex min-h-screen items-center justify-center">
      <div className="bg-card w-full max-w-md rounded-lg p-8 text-center shadow-lg">
        <Loader2 className="mx-auto mb-4 h-12 w-12 animate-spin text-blue-600" />
        <h1 className="text-foreground mb-2 text-xl font-semibold">Completando autenticación...</h1>
        <p className="text-muted-foreground">
          Conectando con {provider.charAt(0).toUpperCase() + provider.slice(1)}
        </p>
      </div>
    </div>
  );
}
