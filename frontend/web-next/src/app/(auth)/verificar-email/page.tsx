/**
 * Email Verification Page
 *
 * Features:
 * - Verify email with token from URL
 * - Resend verification email
 * - Success/error states
 */

'use client';

import * as React from 'react';
import { useSearchParams } from 'next/navigation';
import Link from 'next/link';
import { Loader2, Mail, CheckCircle, AlertTriangle, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { authService } from '@/services/auth';

type VerificationState = 'pending' | 'verifying' | 'success' | 'error';

interface VerifyEmailFormProps {
  token: string | null;
  emailParam: string | null;
  redirectTo: string | null;
}

function VerifyEmailForm({ token, emailParam, redirectTo }: VerifyEmailFormProps) {
  const [state, setState] = React.useState<VerificationState>(token ? 'verifying' : 'pending');
  const [error, setError] = React.useState<string | null>(null);
  const [email, setEmail] = React.useState(emailParam || '');
  const [isResending, setIsResending] = React.useState(false);
  const [resendSuccess, setResendSuccess] = React.useState(false);
  const [canResend, setCanResend] = React.useState(true);
  const [resendTimer, setResendTimer] = React.useState(0);
  const loginHref = redirectTo ? `/login?redirect=${encodeURIComponent(redirectTo)}` : '/login';

  // Verify email on mount if token exists
  React.useEffect(() => {
    if (token && state === 'verifying') {
      verifyEmail();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Resend timer countdown
  React.useEffect(() => {
    if (resendTimer > 0) {
      const timer = setTimeout(() => setResendTimer(resendTimer - 1), 1000);
      return () => clearTimeout(timer);
    } else if (resendTimer === 0) {
      setCanResend(true);
    }
  }, [resendTimer]);

  const verifyEmail = async () => {
    if (!token) return;

    setState('verifying');
    setError(null);

    try {
      await authService.verifyEmail(token);
      setState('success');
    } catch (err) {
      const apiError = err as { message?: string };
      setError(
        apiError.message || 'No pudimos verificar tu email. El enlace puede haber expirado.'
      );
      setState('error');
    }
  };

  const handleResend = async () => {
    if (!email) return;

    setIsResending(true);
    setError(null);

    try {
      await authService.resendVerification(email);
      setResendSuccess(true);
      setCanResend(false);
      setResendTimer(60);
    } catch (err) {
      const apiError = err as { message?: string };
      setError(apiError.message || 'Error al reenviar el email de verificación.');
    } finally {
      setIsResending(false);
    }
  };

  // Pending state - waiting for verification
  if (state === 'pending') {
    return (
      <div className="space-y-6 text-center">
        <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-blue-100">
          <Mail className="h-8 w-8 text-blue-600" />
        </div>

        <div className="space-y-2">
          <h1 className="text-2xl font-bold text-foreground">Verifica tu email</h1>
          <p className="text-muted-foreground">
            Te hemos enviado un email con un enlace de verificación. Revisa tu bandeja de entrada y
            haz clic en el enlace para activar tu cuenta.
          </p>
        </div>

        <div className="rounded-lg bg-blue-50 p-4 text-left text-sm text-blue-800">
          <p className="mb-2 font-medium">¿No encuentras el email?</p>
          <ul className="space-y-1 text-blue-700">
            <li>• Revisa tu carpeta de spam o correo no deseado</li>
            <li>• Verifica que el email sea correcto</li>
            <li>• El enlace expira en 24 horas</li>
          </ul>
        </div>

        {/* Resend section */}
        <div className="space-y-3 border-t border-border pt-4">
          <p className="text-sm text-muted-foreground">
            ¿No recibiste el email? Ingresa tu correo para reenviarlo:
          </p>

          {error && (
            <div className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
              {error}
            </div>
          )}

          {resendSuccess && (
            <div className="rounded-lg border border-green-200 bg-green-50 p-3 text-sm text-green-700">
              Email reenviado exitosamente. Revisa tu bandeja de entrada.
            </div>
          )}

          <div className="flex gap-2">
            <Input
              type="email"
              placeholder="tu@email.com"
              value={email}
              onChange={e => setEmail(e.target.value)}
              disabled={isResending}
              className="flex-1"
            />
            <Button
              onClick={handleResend}
              disabled={isResending || !email || !canResend}
              variant="outline"
            >
              {isResending ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : !canResend && resendTimer > 0 ? (
                `${resendTimer}s`
              ) : (
                <RefreshCw className="h-4 w-4" />
              )}
            </Button>
          </div>
        </div>

        <Link href={loginHref}>
          <Button variant="ghost" className="w-full">
            Volver al inicio de sesión
          </Button>
        </Link>
      </div>
    );
  }

  // Verifying state
  if (state === 'verifying') {
    return (
      <div className="space-y-6 text-center">
        <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-blue-100">
          <Loader2 className="h-8 w-8 animate-spin text-blue-600" />
        </div>

        <div className="space-y-2">
          <h1 className="text-2xl font-bold text-foreground">Verificando tu email</h1>
          <p className="text-muted-foreground">
            Por favor espera mientras verificamos tu dirección de email...
          </p>
        </div>
      </div>
    );
  }

  // Success state
  if (state === 'success') {
    return (
      <div className="space-y-6 text-center">
        <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-green-100">
          <CheckCircle className="h-8 w-8 text-green-600" />
        </div>

        <div className="space-y-2">
          <h1 className="text-2xl font-bold text-foreground">¡Email verificado!</h1>
          <p className="text-muted-foreground">
            Tu cuenta ha sido activada exitosamente. Ya puedes iniciar sesión y comenzar a explorar
            OKLA.
          </p>
        </div>

        <Link href={loginHref}>
          <Button className="w-full">Iniciar sesión</Button>
        </Link>
      </div>
    );
  }

  // Error state
  return (
    <div className="space-y-6 text-center">
      <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-red-100">
        <AlertTriangle className="h-8 w-8 text-red-600" />
      </div>

      <div className="space-y-2">
        <h1 className="text-2xl font-bold text-foreground">Error de verificación</h1>
        <p className="text-muted-foreground">
          {error || 'No pudimos verificar tu email. El enlace puede haber expirado.'}
        </p>
      </div>

      {/* Resend section */}
      <div className="space-y-3 border-t border-border pt-4">
        <p className="text-sm text-muted-foreground">
          Ingresa tu email para solicitar un nuevo enlace de verificación:
        </p>

        {resendSuccess && (
          <div className="rounded-lg border border-green-200 bg-green-50 p-3 text-sm text-green-700">
            Email reenviado exitosamente. Revisa tu bandeja de entrada.
          </div>
        )}

        <div className="flex gap-2">
          <Input
            type="email"
            placeholder="tu@email.com"
            value={email}
            onChange={e => setEmail(e.target.value)}
            disabled={isResending}
            className="flex-1"
          />
          <Button onClick={handleResend} disabled={isResending || !email || !canResend}>
            {isResending ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : !canResend && resendTimer > 0 ? (
              `${resendTimer}s`
            ) : (
              'Reenviar'
            )}
          </Button>
        </div>
      </div>

      <Link href={loginHref}>
        <Button variant="ghost" className="w-full">
          Volver al inicio de sesión
        </Button>
      </Link>
    </div>
  );
}

function VerifyEmailPageContent() {
  const searchParams = useSearchParams();
  const token = searchParams.get('token');
  const emailParam = searchParams.get('email');
  const redirectTo = searchParams.get('redirect');
  return <VerifyEmailForm token={token} emailParam={emailParam} redirectTo={redirectTo} />;
}

export default function VerifyEmailPage() {
  return (
    <React.Suspense
      fallback={
        <div className="flex items-center justify-center py-12">
          <Loader2 className="text-primary h-8 w-8 animate-spin" />
        </div>
      }
    >
      <VerifyEmailPageContent />
    </React.Suspense>
  );
}
