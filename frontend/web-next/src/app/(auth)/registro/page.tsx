/**
 * Register Page
 *
 * Features:
 * - Full registration form with backend integration
 * - Input sanitization for security
 * - Phone number with country code
 * - Password strength indicator
 * - Terms acceptance
 * - Social registration
 */

'use client';

import * as React from 'react';
import Link from 'next/link';
import { useRouter, useSearchParams } from 'next/navigation';
import {
  Eye,
  EyeOff,
  Loader2,
  Mail,
  Lock,
  User,
  Phone,
  Check,
  X,
  ShoppingBag,
  Tag,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Separator } from '@/components/ui/separator';
import { authService } from '@/services/auth';
import { sanitizeEmail, sanitizePhone, sanitizeText } from '@/lib/security/sanitize';
import { cn } from '@/lib/utils';

export default function RegisterPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const redirectTo = searchParams.get('redirect') || '';

  const [isLoading, setIsLoading] = React.useState(false);
  const [showPassword, setShowPassword] = React.useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);

  const [formData, setFormData] = React.useState({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    password: '',
    confirmPassword: '',
    acceptTerms: false,
    acceptDataTransfer: false,
    ageVerification: false,
    accountType: 'buyer' as 'buyer' | 'seller',
  });

  // Password requirements
  const passwordRequirements = [
    { label: 'Mínimo 8 caracteres', test: (p: string) => p.length >= 8 },
    { label: 'Al menos una mayúscula', test: (p: string) => /[A-Z]/.test(p) },
    { label: 'Al menos una minúscula', test: (p: string) => /[a-z]/.test(p) },
    { label: 'Al menos un número', test: (p: string) => /\d/.test(p) },
    { label: 'Al menos un carácter especial', test: (p: string) => /[^a-zA-Z0-9]/.test(p) },
  ];

  const passwordStrength = passwordRequirements.filter(req => req.test(formData.password)).length;
  const isPasswordValid = passwordStrength === passwordRequirements.length;
  const doPasswordsMatch =
    formData.password === formData.confirmPassword && formData.confirmPassword.length > 0;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    // ===== INPUT SANITIZATION (Security) =====
    const sanitizedFirstName = sanitizeText(formData.firstName.trim(), { maxLength: 50 });
    const sanitizedLastName = sanitizeText(formData.lastName.trim(), { maxLength: 50 });
    const sanitizedEmail = sanitizeEmail(formData.email);
    const sanitizedPhone = formData.phone ? sanitizePhone(formData.phone) : undefined;

    // Validate sanitized inputs
    if (!sanitizedFirstName || sanitizedFirstName.length < 2) {
      setError('El nombre es inválido o muy corto');
      return;
    }

    if (!sanitizedLastName || sanitizedLastName.length < 2) {
      setError('El apellido es inválido o muy corto');
      return;
    }

    if (!sanitizedEmail) {
      setError('El email es inválido');
      return;
    }

    // Validate password requirements
    if (!isPasswordValid) {
      setError('La contraseña no cumple con todos los requisitos de seguridad');
      return;
    }

    if (!doPasswordsMatch) {
      setError('Las contraseñas no coinciden');
      return;
    }

    if (!formData.acceptTerms) {
      setError('Debes aceptar los términos y condiciones');
      return;
    }

    if (!formData.ageVerification) {
      setError('Debes confirmar que tienes 18 años o más');
      return;
    }

    if (!formData.acceptDataTransfer) {
      setError('Debes aceptar la transferencia internacional de datos para continuar');
      return;
    }

    setIsLoading(true);

    try {
      // Send sanitized data to backend
      await authService.register({
        firstName: sanitizedFirstName,
        lastName: sanitizedLastName,
        email: sanitizedEmail,
        phone: sanitizedPhone,
        password: formData.password, // Password is NOT sanitized (would break it)
        acceptTerms: formData.acceptTerms,
        accountType: formData.accountType,
        userIntent: formData.accountType === 'seller' ? 'sell' : 'browse',
      });

      // Redirect to verification page with email pre-filled for resend
      const redirectSuffix = redirectTo ? `&redirect=${encodeURIComponent(redirectTo)}` : '';
      router.push(`/verificar-email?email=${encodeURIComponent(sanitizedEmail)}${redirectSuffix}`);
    } catch (err) {
      // SECURITY: Handle specific error types
      const error = err as { message?: string; status?: number; code?: string };

      if (error.status === 409 || error.message?.includes('already exists')) {
        setError('Ya existe una cuenta con este email. ¿Quieres iniciar sesión?');
      } else if (error.status === 400) {
        setError('Los datos ingresados no son válidos. Verifica e intenta de nuevo.');
      } else if (error.status === 429) {
        setError('Demasiados intentos. Por favor espera unos minutos.');
      } else {
        setError(error.message || 'Error al crear la cuenta. Intenta de nuevo.');
      }
    } finally {
      setIsLoading(false);
    }
  };

  const handleSocialLogin = (provider: 'google' | 'apple') => {
    authService.loginWithProvider(provider);
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="space-y-2 text-center">
        <h1 className="text-foreground text-2xl font-bold">Crear cuenta</h1>
        <p className="text-muted-foreground">Únete a la comunidad de OKLA</p>
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
          <span className="bg-muted/50 text-muted-foreground px-2">o regístrate con email</span>
        </div>
      </div>

      {/* Registration Form */}
      <form onSubmit={handleSubmit} className="space-y-4">
        {/* Error message */}
        {error && (
          <div
            className="border-destructive/30 bg-destructive/10 text-destructive rounded-lg border p-3 text-sm"
            role="alert"
          >
            {error}
          </div>
        )}

        {/* Account type selector */}
        <div className="space-y-2">
          <Label>¿Qué quieres hacer en OKLA?</Label>
          <div className="grid grid-cols-2 gap-3">
            <button
              type="button"
              onClick={() => setFormData({ ...formData, accountType: 'buyer' })}
              className={cn(
                'flex flex-col items-center gap-2 rounded-lg border-2 p-4 text-sm transition-colors',
                formData.accountType === 'buyer'
                  ? 'border-primary bg-primary/5 text-primary'
                  : 'border-border text-muted-foreground hover:border-primary/50'
              )}
            >
              <ShoppingBag className="h-5 w-5" />
              <span className="font-medium">Comprar</span>
              <span className="text-muted-foreground text-center text-xs">Busco un vehículo</span>
            </button>
            <button
              type="button"
              onClick={() => setFormData({ ...formData, accountType: 'seller' })}
              className={cn(
                'flex flex-col items-center gap-2 rounded-lg border-2 p-4 text-sm transition-colors',
                formData.accountType === 'seller'
                  ? 'border-primary bg-primary/5 text-primary'
                  : 'border-border text-muted-foreground hover:border-primary/50'
              )}
            >
              <Tag className="h-5 w-5" />
              <span className="font-medium">Vender</span>
              <span className="text-muted-foreground text-center text-xs">
                Quiero publicar vehículos
              </span>
            </button>
          </div>
        </div>

        {/* Name fields */}
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 sm:gap-4">
          <div className="space-y-2">
            <Label htmlFor="firstName">Nombre</Label>
            <div className="relative">
              <User className="text-muted-foreground absolute top-1/2 left-3 h-5 w-5 -translate-y-1/2" />
              <Input
                id="firstName"
                type="text"
                placeholder="Juan"
                value={formData.firstName}
                onChange={e => setFormData({ ...formData, firstName: e.target.value })}
                required
                autoComplete="given-name"
                disabled={isLoading}
                className="pl-10"
              />
            </div>
          </div>
          <div className="space-y-2">
            <Label htmlFor="lastName">Apellido</Label>
            <Input
              id="lastName"
              type="text"
              placeholder="Pérez"
              value={formData.lastName}
              onChange={e => setFormData({ ...formData, lastName: e.target.value })}
              required
              autoComplete="family-name"
              disabled={isLoading}
            />
          </div>
        </div>

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

        {/* Phone */}
        <div className="space-y-2">
          <Label htmlFor="phone">Teléfono (opcional)</Label>
          <div className="relative">
            <Phone className="text-muted-foreground absolute top-1/2 left-3 h-5 w-5 -translate-y-1/2" />
            <Input
              id="phone"
              type="tel"
              placeholder="809-555-0123"
              value={formData.phone}
              onChange={e => setFormData({ ...formData, phone: e.target.value })}
              autoComplete="tel"
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
              autoComplete="new-password"
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
                  (doPasswordsMatch ? 'border-primary' : 'border-destructive')
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
            <p className="text-destructive text-xs">Las contraseñas no coinciden</p>
          )}
        </div>

        {/* Terms */}
        <label className="flex cursor-pointer items-start gap-2">
          <input
            type="checkbox"
            checked={formData.acceptTerms}
            onChange={e => setFormData({ ...formData, acceptTerms: e.target.checked })}
            className="text-primary focus:ring-primary border-border mt-0.5 h-4 w-4 rounded"
          />
          <span className="text-muted-foreground text-sm">
            Acepto los{' '}
            <Link href="/terminos" className="text-primary hover:underline">
              Términos y Condiciones
            </Link>{' '}
            y la{' '}
            <Link href="/privacidad" className="text-primary hover:underline">
              Política de Privacidad
            </Link>
          </span>
        </label>

        {/* Age Verification — Ley 172-13 */}
        <label className="flex cursor-pointer items-start gap-2">
          <input
            type="checkbox"
            checked={formData.ageVerification}
            onChange={e => setFormData({ ...formData, ageVerification: e.target.checked })}
            className="text-primary focus:ring-primary border-border mt-0.5 h-4 w-4 rounded"
          />
          <span className="text-muted-foreground text-sm">
            Declaro que tengo 18 años o más de edad
          </span>
        </label>

        {/* International Data Transfer Consent — Ley 172-13, Art. 27 */}
        <label className="flex cursor-pointer items-start gap-2">
          <input
            type="checkbox"
            checked={formData.acceptDataTransfer}
            onChange={e => setFormData({ ...formData, acceptDataTransfer: e.target.checked })}
            className="text-primary focus:ring-primary border-border mt-0.5 h-4 w-4 rounded"
          />
          <span className="text-muted-foreground text-sm">
            Acepto que mis datos personales sean transferidos y procesados en servidores ubicados en
            Estados Unidos, conforme al Art. 27 de la Ley 172-13.{' '}
            <Link href="/privacidad" className="text-primary hover:underline">
              Ver Política de Privacidad
            </Link>
          </span>
        </label>

        {/* Submit button */}
        <Button
          type="submit"
          className="w-full"
          disabled={
            isLoading ||
            !formData.acceptTerms ||
            !formData.ageVerification ||
            !formData.acceptDataTransfer
          }
        >
          {isLoading ? (
            <>
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              Creando cuenta...
            </>
          ) : (
            'Crear cuenta'
          )}
        </Button>
      </form>

      {/* Login link */}
      <p className="text-muted-foreground text-center text-sm">
        ¿Ya tienes cuenta?{' '}
        <Link href="/login" className="text-primary font-medium hover:underline">
          Inicia sesión
        </Link>
      </p>
    </div>
  );
}
