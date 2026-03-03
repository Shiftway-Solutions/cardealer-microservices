/**
 * Forgot Password Page
 *
 * Features:
 * - Email input for password reset
 * - Success state with instructions
 * - Resend functionality
 */

'use client';

import * as React from 'react';
import Link from 'next/link';
import { ArrowLeft, Loader2, Mail, CheckCircle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { authService } from '@/services/auth';

export default function ForgotPasswordPage() {
  const [isLoading, setIsLoading] = React.useState(false);
  const [isSuccess, setIsSuccess] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const [email, setEmail] = React.useState('');
  const [canResend, setCanResend] = React.useState(false);
  const [resendTimer, setResendTimer] = React.useState(0);

  // Resend timer countdown
  React.useEffect(() => {
    if (resendTimer > 0) {
      const timer = setTimeout(() => setResendTimer(resendTimer - 1), 1000);
      return () => clearTimeout(timer);
    } else if (resendTimer === 0 && isSuccess) {
      setCanResend(true);
    }
  }, [resendTimer, isSuccess]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsLoading(true);

    try {
      await authService.forgotPassword(email);
      setIsSuccess(true);
      setResendTimer(60);
      setCanResend(false);
    } catch (err) {
      const error = err as { message?: string };
      setError(error.message || 'Error al enviar el email. Intenta de nuevo.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleResend = async () => {
    setCanResend(false);
    setResendTimer(60);
    setIsLoading(true);

    try {
      await authService.forgotPassword(email);
    } catch (_err) {
    } finally {
      setIsLoading(false);
    }
  };

  if (isSuccess) {
    return (
      <div className="space-y-6 text-center">
        <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-green-100">
          <CheckCircle className="h-8 w-8 text-green-600" />
        </div>

        <div className="space-y-2">
          <h1 className="text-foreground text-2xl font-bold">Revisa tu email</h1>
          <p className="text-muted-foreground">
            Hemos enviado instrucciones de recuperación a{' '}
            <span className="text-foreground font-medium">{email}</span>
          </p>
        </div>

        <div className="rounded-lg bg-blue-50 p-4 text-sm text-blue-800">
          <p className="mb-1 font-medium">¿No encuentras el email?</p>
          <ul className="space-y-1 text-blue-700">
            <li>• Revisa tu carpeta de spam</li>
            <li>• Verifica que el email sea correcto</li>
            <li>• El link expira en 1 hora</li>
          </ul>
        </div>

        <div className="space-y-3">
          {canResend ? (
            <Button
              variant="outline"
              onClick={handleResend}
              disabled={isLoading}
              className="w-full"
            >
              {isLoading ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Reenviando...
                </>
              ) : (
                'Reenviar email'
              )}
            </Button>
          ) : resendTimer > 0 ? (
            <p className="text-muted-foreground text-sm">
              Puedes reenviar en {resendTimer} segundos
            </p>
          ) : null}

          <Link href="/login">
            <Button variant="ghost" className="w-full gap-2">
              <ArrowLeft className="h-4 w-4" />
              Volver al inicio de sesión
            </Button>
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Back link */}
      <Link
        href="/login"
        className="text-muted-foreground hover:text-foreground inline-flex items-center gap-2 text-sm"
      >
        <ArrowLeft className="h-4 w-4" />
        Volver al inicio de sesión
      </Link>

      {/* Header */}
      <div className="space-y-2">
        <h1 className="text-foreground text-2xl font-bold">¿Olvidaste tu contraseña?</h1>
        <p className="text-muted-foreground">
          No te preocupes, te enviaremos instrucciones para recuperarla.
        </p>
      </div>

      {/* Form */}
      <form onSubmit={handleSubmit} className="space-y-4">
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
              value={email}
              onChange={e => setEmail(e.target.value)}
              required
              autoComplete="email"
              autoFocus
              disabled={isLoading}
              className="pl-10"
            />
          </div>
        </div>

        {/* Submit button */}
        <Button type="submit" className="w-full" disabled={isLoading}>
          {isLoading ? (
            <>
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              Enviando...
            </>
          ) : (
            'Enviar instrucciones'
          )}
        </Button>
      </form>

      {/* Login link */}
      <p className="text-muted-foreground text-center text-sm">
        ¿Recordaste tu contraseña?{' '}
        <Link href="/login" className="text-primary font-medium hover:underline">
          Inicia sesión
        </Link>
      </p>
    </div>
  );
}
