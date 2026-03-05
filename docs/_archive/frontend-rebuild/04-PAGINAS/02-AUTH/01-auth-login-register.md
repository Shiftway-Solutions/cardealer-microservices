---
title: "Páginas de Autenticación"
priority: P0
estimated_time: "35 minutos"
dependencies: []
apis: []
status: complete
last_updated: "2026-01-30"
---

# 🔐 Páginas de Autenticación

> **Tiempo estimado:** 35 minutos
> **Prerrequisitos:** NextAuth.js configurado

---

## 📋 OBJETIVO

Implementar páginas de auth:

- Login con formulario
- Registro de usuario
- Recuperar contraseña
- Social login (Google)

---

## 🎨 WIREFRAME - LOGIN

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              LOGIN PAGE                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────┐ ┌─────────────────────────────────────────┐ │
│ │                             │ │                                         │ │
│ │                             │ │           [OKLA LOGO]                   │ │
│ │                             │ │                                         │ │
│ │      HERO IMAGE             │ │      Bienvenido de vuelta              │ │
│ │      (Vehículo premium)     │ │      Ingresa a tu cuenta               │ │
│ │                             │ │                                         │ │
│ │                             │ │   ┌─────────────────────────────────┐   │ │
│ │                             │ │   │ Email                           │   │ │
│ │                             │ │   └─────────────────────────────────┘   │ │
│ │                             │ │   ┌─────────────────────────────────┐   │ │
│ │                             │ │   │ Contraseña               [👁️]   │   │ │
│ │                             │ │   └─────────────────────────────────┘   │ │
│ │                             │ │                                         │ │
│ │                             │ │   [ ] Recordarme    ¿Olvidaste tu      │ │
│ │                             │ │                      contraseña?       │ │
│ │                             │ │                                         │ │
│ │                             │ │   ┌─────────────────────────────────┐   │ │
│ │                             │ │   │         INICIAR SESIÓN          │   │ │
│ │                             │ │   └─────────────────────────────────┘   │ │
│ │                             │ │                                         │ │
│ │                             │ │   ─────────── o continúa con ──────────│ │
│ │                             │ │                                         │ │
│ │                             │ │   ┌───────────┐   ┌───────────────────┐ │ │
│ │                             │ │   │  Google   │   │      Apple        │ │ │
│ │                             │ │   └───────────┘   └───────────────────┘ │ │
│ │                             │ │                                         │ │
│ │                             │ │   ¿No tienes cuenta? Regístrate gratis │ │
│ │                             │ │                                         │ │
│ └─────────────────────────────┘ └─────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
```

## 🎨 WIREFRAME - REGISTRO

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                            REGISTRO PAGE                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────┐ ┌─────────────────────────────────────────┐ │
│ │                             │ │                                         │ │
│ │      HERO IMAGE             │ │           [OKLA LOGO]                   │ │
│ │                             │ │                                         │ │
│ │      "Encuentra tu          │ │      Crear cuenta                       │ │
│ │       próximo vehículo"     │ │      ─────────────────                  │ │
│ │                             │ │                                         │ │
│ │                             │ │   ┌────────────────┐ ┌────────────────┐ │ │
│ │                             │ │   │ Nombre         │ │ Apellido       │ │ │
│ │                             │ │   └────────────────┘ └────────────────┘ │ │
│ │                             │ │   ┌─────────────────────────────────┐   │ │
│ │                             │ │   │ Email                           │   │ │
│ │                             │ │   └─────────────────────────────────┘   │ │
│ │                             │ │   ┌─────────────────────────────────┐   │ │
│ │                             │ │   │ Teléfono (+1 809)               │   │ │
│ │                             │ │   └─────────────────────────────────┘   │ │
│ │                             │ │   ┌─────────────────────────────────┐   │ │
│ │                             │ │   │ Contraseña               [👁️]   │   │ │
│ │                             │ │   └─────────────────────────────────┘   │ │
│ │                             │ │   ┌─────────────────────────────────┐   │ │
│ │                             │ │   │ Confirmar Contraseña     [👁️]   │   │ │
│ │                             │ │   └─────────────────────────────────┘   │ │
│ │                             │ │                                         │ │
│ │                             │ │   [ ] Acepto los Términos y Condiciones│ │
│ │                             │ │                                         │ │
│ │                             │ │   ┌─────────────────────────────────┐   │ │
│ │                             │ │   │          CREAR CUENTA           │   │ │
│ │                             │ │   └─────────────────────────────────┘   │ │
│ │                             │ │                                         │ │
│ │                             │ │   ¿Ya tienes cuenta? Inicia sesión     │ │
│ │                             │ │                                         │ │
│ └─────────────────────────────┘ └─────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔧 PASO 1: Página de Login

```typescript
// filepath: src/app/(auth)/login/page.tsx
import { Metadata } from "next";
import Link from "next/link";
import { LoginForm } from "@/components/auth/LoginForm";
import { SocialLogin } from "@/components/auth/SocialLogin";
import { Logo } from "@/components/ui/Logo";

export const metadata: Metadata = {
  title: "Iniciar Sesión | OKLA",
};

export default function LoginPage() {
  return (
    <div className="min-h-screen flex">
      {/* Left side - Form */}
      <div className="flex-1 flex items-center justify-center p-8">
        <div className="w-full max-w-md space-y-8">
          <div className="text-center">
            <Logo className="mx-auto h-12 w-auto" />
            <h1 className="mt-6 text-3xl font-bold text-gray-900">
              Bienvenido de vuelta
            </h1>
            <p className="mt-2 text-gray-600">
              Ingresa a tu cuenta para continuar
            </p>
          </div>

          <LoginForm />

          <div className="relative">
            <div className="absolute inset-0 flex items-center">
              <div className="w-full border-t border-gray-200" />
            </div>
            <div className="relative flex justify-center text-sm">
              <span className="px-2 bg-white text-gray-500">
                o continúa con
              </span>
            </div>
          </div>

          <SocialLogin />

          <p className="text-center text-sm text-gray-600">
            ¿No tienes cuenta?{" "}
            <Link
              href="/registro"
              className="text-primary-600 hover:text-primary-700 font-medium"
            >
              Regístrate gratis
            </Link>
          </p>
        </div>
      </div>

      {/* Right side - Image */}
      <div className="hidden lg:block lg:flex-1 relative">
        <img
          src="/images/auth-bg.jpg"
          alt=""
          className="absolute inset-0 w-full h-full object-cover"
        />
        <div className="absolute inset-0 bg-gradient-to-r from-primary-600/90 to-primary-800/90" />
        <div className="absolute inset-0 flex items-center justify-center p-12">
          <div className="text-white text-center">
            <h2 className="text-4xl font-bold">
              Encuentra tu próximo vehículo
            </h2>
            <p className="mt-4 text-lg text-white/80">
              Miles de vehículos te esperan en OKLA
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 2: LoginForm

```typescript
// filepath: src/components/auth/LoginForm.tsx
"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import Link from "next/link";
import { signIn } from "next-auth/react";
import { FormField } from "@/components/ui/FormField";
import { Input } from "@/components/ui/Input";
import { Button } from "@/components/ui/Button";
import { showToast } from "@/lib/toast";
import { useState } from "react";

const loginSchema = z.object({
  email: z.string().email("Email inválido"),
  password: z.string().min(1, "Contraseña requerida"),
});

type LoginFormData = z.infer<typeof loginSchema>;

export function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const callbackUrl = searchParams.get("callbackUrl") || "/dashboard";
  const [isLoading, setIsLoading] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
  });

  const onSubmit = async (data: LoginFormData) => {
    setIsLoading(true);
    try {
      const result = await signIn("credentials", {
        email: data.email,
        password: data.password,
        redirect: false,
      });

      if (result?.error) {
        showToast.error("Credenciales inválidas");
        return;
      }

      router.push(callbackUrl);
      router.refresh();
    } catch {
      showToast.error("Error al iniciar sesión");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
      <FormField label="Email" error={errors.email?.message}>
        <Input
          {...register("email")}
          type="email"
          placeholder="tu@email.com"
          autoComplete="email"
        />
      </FormField>

      <FormField label="Contraseña" error={errors.password?.message}>
        <Input
          {...register("password")}
          type="password"
          placeholder="••••••••"
          autoComplete="current-password"
        />
      </FormField>

      <div className="flex items-center justify-between">
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" className="rounded border-gray-300" />
          <span className="text-gray-600">Recordarme</span>
        </label>
        <Link
          href="/recuperar"
          className="text-sm text-primary-600 hover:text-primary-700"
        >
          ¿Olvidaste tu contraseña?
        </Link>
      </div>

      <Button type="submit" disabled={isLoading} className="w-full">
        {isLoading ? "Iniciando..." : "Iniciar sesión"}
      </Button>
    </form>
  );
}
```

---

## 🔧 PASO 3: Página de Registro

```typescript
// filepath: src/app/(auth)/registro/page.tsx
import { Metadata } from "next";
import Link from "next/link";
import { RegisterForm } from "@/components/auth/RegisterForm";
import { Logo } from "@/components/ui/Logo";

export const metadata: Metadata = {
  title: "Crear Cuenta | OKLA",
};

export default function RegisterPage() {
  return (
    <div className="min-h-screen flex items-center justify-center p-8 bg-gray-50">
      <div className="w-full max-w-md">
        <div className="bg-white rounded-2xl shadow-lg p-8">
          <div className="text-center mb-8">
            <Logo className="mx-auto h-10" />
            <h1 className="mt-4 text-2xl font-bold text-gray-900">
              Crear cuenta
            </h1>
            <p className="mt-2 text-gray-600">
              Únete a miles de compradores y vendedores
            </p>
          </div>

          <RegisterForm />

          <p className="mt-6 text-center text-sm text-gray-600">
            ¿Ya tienes cuenta?{" "}
            <Link
              href="/login"
              className="text-primary-600 hover:text-primary-700 font-medium"
            >
              Inicia sesión
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 4: RegisterForm

```typescript
// filepath: src/components/auth/RegisterForm.tsx
"use client";

import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useState } from "react";
import { FormField } from "@/components/ui/FormField";
import { Input } from "@/components/ui/Input";
import { Button } from "@/components/ui/Button";
import { authService } from "@/lib/services/authService";
import { showToast } from "@/lib/toast";

const registerSchema = z
  .object({
    name: z.string().min(2, "Nombre muy corto"),
    email: z.string().email("Email inválido"),
    password: z.string().min(8, "Mínimo 8 caracteres"),
    confirmPassword: z.string(),
    acceptTerms: z.boolean().refine((v) => v, "Debes aceptar los términos"),
    // CONSENT-REG-001: Consentimientos opcionales (NO pre-marcados)
    marketingConsent: z.boolean().optional().default(false),
    partnersConsent: z.boolean().optional().default(false),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: "Las contraseñas no coinciden",
    path: ["confirmPassword"],
  });

type RegisterFormData = z.infer<typeof registerSchema>;

export function RegisterForm() {
  const router = useRouter();
  const [isLoading, setIsLoading] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
  });

  const onSubmit = async (data: RegisterFormData) => {
    setIsLoading(true);
    try {
      // CONSENT-REG-002: Registrar consentimientos con metadata
      await authService.register({
        name: data.name,
        email: data.email,
        password: data.password,
        consents: {
          terms: true, // Obligatorio
          privacy: true, // Obligatorio
          marketingEmail: data.marketingConsent || false,
          partnersEmail: data.partnersConsent || false,
          // Metadata para auditoría (Ley 172-13)
          timestamp: new Date().toISOString(),
          ipAddress: await getClientIP(), // Obtener IP del cliente
          userAgent: navigator.userAgent,
          source: 'registration_form',
          version: '1.0' // Versión del texto de consentimiento
        }
      });

      showToast.success("Cuenta creada", "Revisa tu email para verificar");
      router.push("/login");
    } catch (error) {
      showToast.error("Error al crear cuenta");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
      <FormField label="Nombre completo" error={errors.name?.message}>
        <Input {...register("name")} placeholder="Juan Pérez" />
      </FormField>

      <FormField label="Email" error={errors.email?.message}>
        <Input
          {...register("email")}
          type="email"
          placeholder="tu@email.com"
        />
      </FormField>

      <FormField label="Contraseña" error={errors.password?.message}>
        <Input
          {...register("password")}
          type="password"
          placeholder="Mínimo 8 caracteres"
        />
      </FormField>

      <FormField label="Confirmar contraseña" error={errors.confirmPassword?.message}>
        <Input
          {...register("confirmPassword")}
          type="password"
          placeholder="Repite tu contraseña"
        />
      </FormField>

      <FormField error={errors.acceptTerms?.message}>
        <label className="flex items-start gap-2 text-sm">
          <input
            {...register("acceptTerms")}
            type="checkbox"
            className="mt-1 rounded border-gray-300"
          />
          <span className="text-gray-600">
            Acepto los{" "}
            <a href="/terminos" className="text-primary-600 hover:underline">
              términos de servicio
            </a>{" "}
            y la{" "}
            <a href="/privacidad" className="text-primary-600 hover:underline">
              política de privacidad
            </a>
          </span>
        </label>
      </FormField>

      {/* CONSENT-REG-001: Consentimiento de Marketing (Opcional) */}
      <div className="space-y-3 pt-2 border-t border-gray-200">
        <FormField>
          <label className="flex items-start gap-2 text-sm">
            <input
              {...register("marketingConsent")}
              type="checkbox"
              className="mt-1 rounded border-gray-300"
            />
            <span className="text-gray-600">
              Deseo recibir ofertas, promociones y novedades de OKLA por email
            </span>
          </label>
        </FormField>

        <FormField>
          <label className="flex items-start gap-2 text-sm">
            <input
              {...register("partnersConsent")}
              type="checkbox"
              className="mt-1 rounded border-gray-300"
            />
            <span className="text-gray-600">
              Acepto recibir ofertas de partners seleccionados de OKLA
            </span>
          </label>
        </FormField>

        <p className="text-xs text-gray-500">
          📋 <strong>Tu privacidad es importante.</strong> Puedes cambiar estas
          preferencias en cualquier momento desde tu configuración. Ley 172-13 RD.
        </p>
      </div>

      <Button type="submit" disabled={isLoading} className="w-full">
        {isLoading ? "Creando cuenta..." : "Crear cuenta"}
      </Button>
    </form>
  );
}
```

---

## 🔧 PASO 5: SocialLogin

```typescript
// filepath: src/components/auth/SocialLogin.tsx
"use client";

import { signIn } from "next-auth/react";
import { useState } from "react";
import { Button } from "@/components/ui/Button";

export function SocialLogin() {
  const [isLoading, setIsLoading] = useState<string | null>(null);

  const handleSocialLogin = async (provider: string) => {
    setIsLoading(provider);
    await signIn(provider, { callbackUrl: "/dashboard" });
  };

  return (
    <div className="space-y-3">
      <Button
        type="button"
        variant="outline"
        onClick={() => handleSocialLogin("google")}
        disabled={isLoading !== null}
        className="w-full"
      >
        <svg className="w-5 h-5 mr-2" viewBox="0 0 24 24">
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
        {isLoading === "google" ? "Conectando..." : "Continuar con Google"}
      </Button>
    </div>
  );
}
```

---

## 🔧 PASO 6: Email Verification (P2 - Opcional)

```typescript
// filepath: src/app/(auth)/verificar-email/page.tsx
import { Metadata } from "next";
import { VerifyEmailForm } from "@/components/auth/VerifyEmailForm";
import { Mail } from "lucide-react";

export const metadata: Metadata = {
  title: "Verificar Email | OKLA",
};

interface VerifyEmailPageProps {
  searchParams: { token?: string; email?: string };
}

export default function VerifyEmailPage({ searchParams }: VerifyEmailPageProps) {
  return (
    <div className="min-h-screen flex items-center justify-center p-8 bg-gray-50">
      <div className="w-full max-w-md">
        <div className="bg-white rounded-2xl shadow-lg p-8">
          <div className="text-center mb-8">
            <div className="w-16 h-16 bg-primary-100 rounded-full flex items-center justify-center mx-auto mb-4">
              <Mail size={32} className="text-primary-600" />
            </div>
            <h1 className="text-2xl font-bold text-gray-900">
              Verifica tu email
            </h1>
            <p className="mt-2 text-gray-600">
              Te enviamos un código de verificación
            </p>
          </div>

          <VerifyEmailForm
            token={searchParams.token}
            email={searchParams.email}
          />
        </div>
      </div>
    </div>
  );
}
```

```typescript
// filepath: src/components/auth/VerifyEmailForm.tsx
"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Input } from "@/components/ui/Input";
import { Button } from "@/components/ui/Button";
import { authService } from "@/lib/services/authService";
import { showToast } from "@/lib/toast";
import { CheckCircle, Clock } from "lucide-react";

interface VerifyEmailFormProps {
  token?: string;
  email?: string;
}

export function VerifyEmailForm({ token, email }: VerifyEmailFormProps) {
  const router = useRouter();
  const [code, setCode] = useState(token || "");
  const [isVerifying, setIsVerifying] = useState(false);
  const [isResending, setIsResending] = useState(false);
  const [countdown, setCountdown] = useState(0);

  const handleVerify = async () => {
    if (code.length !== 6) return;

    setIsVerifying(true);
    try {
      await authService.verifyEmail(code);
      showToast.success("Email verificado", "Ya puedes iniciar sesión");
      router.push("/login");
    } catch (error) {
      showToast.error("Código inválido o expirado");
    } finally {
      setIsVerifying(false);
    }
  };

  const handleResend = async () => {
    if (!email) {
      showToast.error("Email no proporcionado");
      return;
    }

    setIsResending(true);
    try {
      await authService.resendVerificationEmail(email);
      showToast.success("Código reenviado");
      setCountdown(60);

      // Countdown
      const interval = setInterval(() => {
        setCountdown((prev) => {
          if (prev <= 1) {
            clearInterval(interval);
            return 0;
          }
          return prev - 1;
        });
      }, 1000);
    } catch (error) {
      showToast.error("Error al reenviar código");
    } finally {
      setIsResending(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* Info */}
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <div className="flex items-start gap-3">
          <Clock size={20} className="text-blue-600 flex-shrink-0 mt-0.5" />
          <div>
            <p className="text-sm font-medium text-blue-900">
              Código enviado a {email}
            </p>
            <p className="text-sm text-blue-700 mt-1">
              El código expira en 15 minutos
            </p>
          </div>
        </div>
      </div>

      {/* Code input */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Código de verificación
        </label>
        <Input
          type="text"
          placeholder="000000"
          value={code}
          onChange={(e) => setCode(e.target.value.replace(/\D/g, "").slice(0, 6))}
          maxLength={6}
          className="text-center text-2xl tracking-widest"
          autoFocus
        />
      </div>

      {/* Verify button */}
      <Button
        onClick={handleVerify}
        disabled={code.length !== 6 || isVerifying}
        className="w-full"
      >
        {isVerifying ? "Verificando..." : "Verificar email"}
      </Button>

      {/* Resend */}
      <div className="text-center">
        <button
          onClick={handleResend}
          disabled={isResending || countdown > 0}
          className="text-sm text-primary-600 hover:underline disabled:text-gray-400 disabled:no-underline"
        >
          {isResending
            ? "Enviando..."
            : countdown > 0
            ? `Reenviar en ${countdown}s`
            : "¿No recibiste el código? Reenviar"}
        </button>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 7: Social Login Expansion (P2 - Opcional)

Expande SocialLogin para incluir Facebook y Apple:

```typescript
// filepath: src/components/auth/SocialLogin.tsx
"use client";

import { signIn } from "next-auth/react";
import { useState } from "react";
import { Button } from "@/components/ui/Button";
import { FaGoogle, FaFacebook, FaApple } from "react-icons/fa";

export function SocialLogin() {
  const [isLoading, setIsLoading] = useState<string | null>(null);

  const handleSocialLogin = async (provider: string) => {
    setIsLoading(provider);
    await signIn(provider, { callbackUrl: "/dashboard" });
  };

  const providers = [
    {
      id: "google",
      name: "Google",
      icon: FaGoogle,
      color: "text-red-600",
      bgHover: "hover:bg-gray-50",
    },
    {
      id: "facebook",
      name: "Facebook",
      icon: FaFacebook,
      color: "text-blue-600",
      bgHover: "hover:bg-gray-50",
    },
    {
      id: "apple",
      name: "Apple",
      icon: FaApple,
      color: "text-gray-900",
      bgHover: "hover:bg-gray-50",
    },
  ];

  return (
    <div className="space-y-3">
      {providers.map((provider) => {
        const Icon = provider.icon;
        return (
          <Button
            key={provider.id}
            type="button"
            variant="outline"
            onClick={() => handleSocialLogin(provider.id)}
            disabled={isLoading !== null}
            className={`w-full ${provider.bgHover}`}
          >
            <Icon className={`w-5 h-5 mr-2 ${provider.color}`} />
            {isLoading === provider.id
              ? "Conectando..."
              : `Continuar con ${provider.name}`}
          </Button>
        );
      })}
    </div>
  );
}
```

### Configuración de Providers

**NextAuth Configuration (auth.config.ts):**

```typescript
import NextAuth from "next-auth";
import GoogleProvider from "next-auth/providers/google";
import FacebookProvider from "next-auth/providers/facebook";
import AppleProvider from "next-auth/providers/apple";
import CredentialsProvider from "next-auth/providers/credentials";

export const authConfig = {
  providers: [
    // Google OAuth
    GoogleProvider({
      clientId: process.env.GOOGLE_CLIENT_ID!,
      clientSecret: process.env.GOOGLE_CLIENT_SECRET!,
      authorization: {
        params: {
          prompt: "consent",
          access_type: "offline",
          response_type: "code",
        },
      },
    }),

    // Facebook OAuth
    FacebookProvider({
      clientId: process.env.FACEBOOK_CLIENT_ID!,
      clientSecret: process.env.FACEBOOK_CLIENT_SECRET!,
    }),

    // Apple Sign In
    AppleProvider({
      clientId: process.env.APPLE_CLIENT_ID!,
      clientSecret: process.env.APPLE_CLIENT_SECRET!,
    }),

    // Email/Password
    CredentialsProvider({
      name: "Credentials",
      credentials: {
        email: { label: "Email", type: "email" },
        password: { label: "Password", type: "password" },
      },
      async authorize(credentials) {
        // Tu lógica de autenticación
        return null;
      },
    }),
  ],
  // ... resto de config
};
```

**Environment Variables (.env.local):**

```bash
# Google OAuth
GOOGLE_CLIENT_ID=your_google_client_id.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your_google_client_secret

# Facebook OAuth
FACEBOOK_CLIENT_ID=your_facebook_app_id
FACEBOOK_CLIENT_SECRET=your_facebook_app_secret

# Apple Sign In
APPLE_CLIENT_ID=your_apple_service_id
APPLE_CLIENT_SECRET=your_apple_private_key

# NextAuth
NEXTAUTH_URL=http://localhost:3000
NEXTAUTH_SECRET=your_random_secret_key_here
```

### Setup de Providers

#### 1️⃣ Google OAuth Setup

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create project → Enable Google+ API
3. Credentials → Create OAuth 2.0 Client ID
4. Authorized redirect URIs:
   - `http://localhost:3000/api/auth/callback/google` (dev)
   - `https://okla.com.do/api/auth/callback/google` (prod)

#### 2️⃣ Facebook OAuth Setup

1. Go to [Facebook Developers](https://developers.facebook.com/)
2. Create App → Consumer
3. Add Facebook Login product
4. Settings → Valid OAuth Redirect URIs:
   - `http://localhost:3000/api/auth/callback/facebook`
   - `https://okla.com.do/api/auth/callback/facebook`

#### 3️⃣ Apple Sign In Setup

1. Go to [Apple Developer](https://developer.apple.com/account/)
2. Certificates, Identifiers & Profiles → Identifiers → App IDs
3. Create Service ID
4. Configure Sign in with Apple:
   - Domains: `okla.com.do`
   - Return URLs: `https://okla.com.do/api/auth/callback/apple`
5. Create Private Key for Sign in with Apple
6. Generate Client Secret (JWT) with the private key

**Apple Client Secret Generation:**

```javascript
// generate-apple-secret.js
const jwt = require("jsonwebtoken");
const fs = require("fs");

const privateKey = fs.readFileSync("AuthKey_XXXXX.p8", "utf8");

const token = jwt.sign(
  {
    iss: "YOUR_TEAM_ID",
    iat: Math.floor(Date.now() / 1000),
    exp: Math.floor(Date.now() / 1000) + 86400 * 180, // 6 months
    aud: "https://appleid.apple.com",
    sub: "YOUR_SERVICE_ID",
  },
  privateKey,
  {
    algorithm: "ES256",
    keyid: "YOUR_KEY_ID",
  },
);

console.log(token);
```

---

## ✅ VALIDACIÓN

```bash
pnpm dev

# Verificar Login/Register:
# - /login muestra formulario
# - /registro funciona
# - Validaciones funcionan
# - Redirección correcta

# Verificar Email Verification (P2):
# - /verificar-email muestra formulario
# - Código de 6 dígitos funciona
# - Reenviar código tiene countdown
# - Token expirado muestra error
# - Email verificado redirige a login

# Verificar Social Login Expansion (P2):
# - Botón Google funciona
# - Botón Facebook funciona
# - Botón Apple funciona
# - Loading states funcionan
# - Redirect correcto después de OAuth
```

---

## 🚀 MEJORAS FUTURAS

1. **Magic Link Login**: Email con link de inicio de sesión sin contraseña
2. **Biometric Auth**: Face ID/Touch ID en mobile
3. **Passkeys**: WebAuthn para login sin contraseña
4. **Account Recovery**: Preguntas de seguridad como backup

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/auth.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Auth - Login & Register", () => {
  test.describe("Login Flow", () => {
    test.beforeEach(async ({ page }) => {
      await page.goto("/login");
    });

    test("debe mostrar formulario de login", async ({ page }) => {
      await expect(
        page.getByRole("heading", { name: /iniciar sesión/i }),
      ).toBeVisible();
      await expect(page.getByLabel(/email/i)).toBeVisible();
      await expect(page.getByLabel(/contraseña/i)).toBeVisible();
      await expect(page.getByRole("button", { name: /entrar/i })).toBeVisible();
    });

    test("debe mostrar error con credenciales inválidas", async ({ page }) => {
      await page.fill('input[name="email"]', "invalid@example.com");
      await page.fill('input[name="password"]', "wrongpassword");
      await page.click('button[type="submit"]');

      await expect(page.getByText(/credenciales inválidas/i)).toBeVisible();
    });

    test("debe hacer login exitoso y redirigir", async ({ page }) => {
      await page.fill('input[name="email"]', "test@example.com");
      await page.fill('input[name="password"]', "Password123!");
      await page.click('button[type="submit"]');

      await expect(page).toHaveURL("/dashboard");
      await expect(page.getByTestId("user-menu")).toBeVisible();
    });

    test("debe mostrar validación en campos vacíos", async ({ page }) => {
      await page.click('button[type="submit"]');

      await expect(page.getByText(/email es requerido/i)).toBeVisible();
      await expect(page.getByText(/contraseña es requerida/i)).toBeVisible();
    });

    test("debe redirigir a página original después de login", async ({
      page,
    }) => {
      await page.goto("/publicar"); // Página protegida
      await expect(page).toHaveURL(/\/login\?redirect=/);

      await page.fill('input[name="email"]', "test@example.com");
      await page.fill('input[name="password"]', "Password123!");
      await page.click('button[type="submit"]');

      await expect(page).toHaveURL("/publicar");
    });

    test("debe soportar login con Google", async ({ page }) => {
      await page.getByRole("button", { name: /continuar con google/i }).click();
      // El popup de OAuth debería abrirse
      await expect(page.context().pages()).toHaveLength(2);
    });
  });

  test.describe("Register Flow", () => {
    test.beforeEach(async ({ page }) => {
      await page.goto("/registro");
    });

    test("debe mostrar formulario de registro", async ({ page }) => {
      await expect(
        page.getByRole("heading", { name: /crear cuenta/i }),
      ).toBeVisible();
      await expect(page.getByLabel(/nombre/i)).toBeVisible();
      await expect(page.getByLabel(/email/i)).toBeVisible();
      await expect(page.getByLabel(/teléfono/i)).toBeVisible();
    });

    test("debe validar formato de email", async ({ page }) => {
      await page.fill('input[name="email"]', "invalidemail");
      await page.click('button[type="submit"]');

      await expect(page.getByText(/email inválido/i)).toBeVisible();
    });

    test("debe validar fortaleza de contraseña", async ({ page }) => {
      await page.fill('input[name="password"]', "123");

      await expect(page.getByTestId("password-strength")).toHaveAttribute(
        "data-strength",
        "weak",
      );
    });

    test("debe completar registro exitosamente", async ({ page }) => {
      const uniqueEmail = `test${Date.now()}@example.com`;

      await page.fill('input[name="firstName"]', "Juan");
      await page.fill('input[name="lastName"]', "Pérez");
      await page.fill('input[name="email"]', uniqueEmail);
      await page.fill('input[name="phone"]', "8091234567");
      await page.fill('input[name="password"]', "SecurePass123!");
      await page.fill('input[name="confirmPassword"]', "SecurePass123!");
      await page.check('input[name="acceptTerms"]');

      await page.click('button[type="submit"]');

      await expect(page).toHaveURL(/\/verificar-email/);
    });
  });

  test.describe("Logout", () => {
    test("debe cerrar sesión correctamente", async ({ page }) => {
      // Login primero
      await page.goto("/login");
      await page.fill('input[name="email"]', "test@example.com");
      await page.fill('input[name="password"]', "Password123!");
      await page.click('button[type="submit"]');

      await expect(page).toHaveURL("/dashboard");

      // Logout
      await page.getByTestId("user-menu").click();
      await page.getByRole("menuitem", { name: /cerrar sesión/i }).click();

      await expect(page).toHaveURL("/");
      await expect(
        page.getByRole("link", { name: /iniciar sesión/i }),
      ).toBeVisible();
    });
  });

  test.describe("Password Reset", () => {
    test("debe solicitar reset de contraseña", async ({ page }) => {
      await page.goto("/login");
      await page.getByRole("link", { name: /olvidé mi contraseña/i }).click();

      await expect(page).toHaveURL("/recuperar-password");

      await page.fill('input[name="email"]', "test@example.com");
      await page.click('button[type="submit"]');

      await expect(page.getByText(/revisa tu email/i)).toBeVisible();
    });
  });
});
```

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/04-PAGINAS/08-perfil.md` (Device Fingerprinting + SMS 2FA)
