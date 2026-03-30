/**
 * Reset Password Page
 *
 * Features:
 * - New password input with strength indicator
 * - Confirm password validation
 * - Token validation from URL
 * - Success redirect to login
 */

'use client';

import * as React from 'react';
import { useSearchParams } from 'next/navigation';
import Link from 'next/link';
import { Eye, EyeOff, Loader2, Lock, Check, X, AlertTriangle, CheckCircle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { authService } from '@/services/auth';
import { cn } from '@/lib/utils';

interface ResetPasswordFormProps {
  token: string | null;
}

function ResetPasswordForm({ token }: ResetPasswordFormProps) {
  const [isLoading, setIsLoading] = React.useState(false);
  const [isSuccess, setIsSuccess] = React.useState(false);
  const [showPassword, setShowPassword] = React.useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);

  const [formData, setFormData] = React.useState({
    password: '',
    confirmPassword: '',
  });

  // Password requirements
  const passwordRequirements = [
    { label: 'Mínimo 8 caracteres', test: (p: string) => p.length >= 8 },
    { label: 'Al menos una mayúscula', test: (p: string) => /[A-Z]/.test(p) },
    { label: 'Al menos una minúscula', test: (p: string) => /[a-z]/.test(p) },
    { label: 'Al menos un número', test: (p: string) => /\d/.test(p) },
  ];

  const isPasswordValid = passwordRequirements.every(req => req.test(formData.password));
  const doPasswordsMatch =
    formData.password === formData.confirmPassword && formData.confirmPassword.length > 0;

  // Check if token is missing
  if (!token) {
    return (
      <div className="space-y-6 text-center">
        <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-red-100">
          <AlertTriangle className="h-8 w-8 text-red-600" />
        </div>

        <div className="space-y-2">
          <h1 className="text-foreground text-2xl font-bold">Enlace inválido</h1>
          <p className="text-muted-foreground">
            El enlace de recuperación no es válido o ha expirado. Por favor solicita uno nuevo.
          </p>
        </div>

        <Link href="/recuperar-contrasena">
          <Button className="w-full">Solicitar nuevo enlace</Button>
        </Link>
      </div>
    );
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!isPasswordValid) {
      setError('La contraseña no cumple con los requisitos');
      return;
    }

    if (!doPasswordsMatch) {
      setError('Las contraseñas no coinciden');
      return;
    }

    setIsLoading(true);

    try {
      await authService.resetPassword({
        token,
        password: formData.password,
      });
      setIsSuccess(true);
    } catch (err) {
      const error = err as { message?: string };
      setError(
        error.message || 'Error al restablecer la contraseña. El enlace puede haber expirado.'
      );
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
          <h1 className="text-foreground text-2xl font-bold">¡Contraseña actualizada!</h1>
          <p className="text-muted-foreground">
            Tu contraseña ha sido restablecida exitosamente. Ya puedes iniciar sesión con tu nueva
            contraseña.
          </p>
        </div>

        <Link href="/login">
          <Button className="w-full">Iniciar sesión</Button>
        </Link>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="space-y-2">
        <h1 className="text-foreground text-2xl font-bold">Nueva contraseña</h1>
        <p className="text-muted-foreground">Crea una contraseña segura para tu cuenta.</p>
      </div>

      {/* Form */}
      <form onSubmit={handleSubmit} className="space-y-4">
        {/* Error message */}
        {error && (
          <div className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
            {error}
          </div>
        )}

        {/* Password */}
        <div className="space-y-2">
          <Label htmlFor="password">Nueva contraseña</Label>
          <div className="relative">
            <Lock className="text-muted-foreground absolute top-1/2 left-3 h-5 w-5 -translate-y-1/2" />
            <Input
              id="password"
              type={showPassword ? 'text' : 'password'}
              placeholder="••••••••"
              value={formData.password}
              onChange={e => setFormData({ ...formData, password: e.target.value })}
              required
              autoComplete="new-password"
              autoFocus
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

          {/* Password requirements */}
          {formData.password && (
            <div className="mt-2 space-y-1">
              {passwordRequirements.map((req, index) => (
                <div
                  key={index}
                  className={cn(
                    'flex items-center gap-2 text-xs',
                    req.test(formData.password) ? 'text-green-600' : 'text-muted-foreground'
                  )}
                >
                  {req.test(formData.password) ? (
                    <Check className="h-3.5 w-3.5" />
                  ) : (
                    <X className="h-3.5 w-3.5" />
                  )}
                  {req.label}
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Confirm Password */}
        <div className="space-y-2">
          <Label htmlFor="confirmPassword">Confirmar contraseña</Label>
          <div className="relative">
            <Lock className="text-muted-foreground absolute top-1/2 left-3 h-5 w-5 -translate-y-1/2" />
            <Input
              id="confirmPassword"
              type={showConfirmPassword ? 'text' : 'password'}
              placeholder="••••••••"
              value={formData.confirmPassword}
              onChange={e => setFormData({ ...formData, confirmPassword: e.target.value })}
              required
              autoComplete="new-password"
              disabled={isLoading}
              className={cn(
                'pr-10 pl-10',
                formData.confirmPassword &&
                  (doPasswordsMatch ? 'border-green-500' : 'border-red-500')
              )}
            />
            <button
              type="button"
              onClick={() => setShowConfirmPassword(!showConfirmPassword)}
              className="text-muted-foreground hover:text-muted-foreground absolute top-1/2 right-3 -translate-y-1/2"
              tabIndex={-1}
              aria-label={showConfirmPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'}
            >
              {showConfirmPassword ? <EyeOff className="h-5 w-5" /> : <Eye className="h-5 w-5" />}
            </button>
          </div>
          {formData.confirmPassword && !doPasswordsMatch && (
            <p className="text-xs text-red-500">Las contraseñas no coinciden</p>
          )}
        </div>

        {/* Submit button */}
        <Button
          type="submit"
          className="w-full"
          disabled={isLoading || !isPasswordValid || !doPasswordsMatch}
        >
          {isLoading ? (
            <>
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              Restableciendo...
            </>
          ) : (
            'Restablecer contraseña'
          )}
        </Button>
      </form>
    </div>
  );
}

function ResetPasswordPageContent() {
  const searchParams = useSearchParams();
  const token = searchParams.get('token');
  return <ResetPasswordForm token={token} />;
}

export default function ResetPasswordPage() {
  return (
    <React.Suspense
      fallback={
        <div className="flex items-center justify-center py-12">
          <Loader2 className="text-primary h-8 w-8 animate-spin" />
        </div>
      }
    >
      <ResetPasswordPageContent />
    </React.Suspense>
  );
}
