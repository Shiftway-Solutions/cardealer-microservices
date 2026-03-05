---
title: "28. OAuth Account Management (Unlink & Security)"
priority: P0
estimated_time: "2 horas"
dependencies: []
apis: ["AuthService"]
status: complete
last_updated: "2026-01-30"
---

# 28. OAuth Account Management (Unlink & Security)

**Objetivo:** Sistema seguro para gestionar proveedores OAuth (Google, Facebook, Apple) conectados a la cuenta, con prevención de lockout al desvincular proveedores activos y flujo obligatorio de creación de contraseña.

**Prioridad:** P0 (CRÍTICO - Previene lockout permanente de cuentas)  
**Complejidad:** 🔴 Alta (Account Safety, Email Verification, Password Setup)  
**Dependencias:** AuthService (✅ YA IMPLEMENTADO - 57 tests pasando)

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura](#arquitectura)
2. [Backend API](#backend-api)
3. [Componentes](#componentes)
4. [Páginas](#páginas)
5. [Hooks y Servicios](#hooks-y-servicios)
6. [Tipos TypeScript](#tipos-typescript)
7. [Validación](#validación)

---

## 🏗️ ARQUITECTURA

### Problema que Resuelve

**Escenario de Lockout:**

```
Usuario se registró con Google (OAuth)
    ↓
NO tiene contraseña configurada
    ↓
Usuario intenta desvincular Google
    ↓
❌ SIN PREVENCIÓN: Cuenta queda sin método de login
    ↓
Usuario NO PUEDE acceder nunca más
```

**Nuestra Solución:**

```
Usuario intenta desvincular Google
    ↓
Sistema detecta: "Es tu único método de login"
    ↓
Modal: "Debes crear contraseña primero"
    ↓
Flujo de SetPasswordPage
    ↓
Email con código de 6 dígitos
    ↓
Usuario crea contraseña
    ↓
✅ AHORA SÍ: Puede desvincular Google
    ↓
Tiene 2 métodos: Email+Password + Google
```

---

### Flujo Completo: Unlink Active Provider

```
┌────────────────────────────────────────────────────────────────────────────┐
│                   OAUTH UNLINK SECURITY FLOW                               │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│  1️⃣ USUARIO ACCEDE A SETTINGS                                              │
│  Route: /dashboard/perfil/seguridad/oauth                                 │
│  ├─ Ve lista de providers conectados                                      │
│  ├─ Google: ✓ Conectado (badge verde)                                     │
│  ├─ Facebook: Conectar (botón gris)                                       │
│  └─ Apple: Conectar (botón gris)                                          │
│                                                                            │
│  2️⃣ CLICK "DESVINCULAR GOOGLE"                                            │
│  GET /api/auth/external/can-unlink/google                                 │
│  ├─ Backend verifica:                                                     │
│  │   • ¿Tiene contraseña configurada? NO ❌                               │
│  │   • ¿Cuántos providers activos? 1 (solo Google)                       │
│  │   • Result: canUnlink = false                                         │
│  └─ Response: { canUnlink: false, reason: "NoPasswordSet" }              │
│                                                                            │
│  3️⃣ MODAL DE ADVERTENCIA                                                  │
│  UnlinkActiveProviderModal abre                                           │
│  ├─ Mensaje: "⚠️ No puedes desvincular Google"                           │
│  ├─ Razón: "Es tu único método de login"                                 │
│  ├─ Acción: "Debes crear una contraseña primero"                         │
│  └─ Botón: "Crear Contraseña" → /set-password                            │
│                                                                            │
│  4️⃣ SET PASSWORD PAGE                                                     │
│  Route: /set-password?provider=google                                     │
│  ├─ Usuario ingresa email (pre-filled)                                   │
│  ├─ POST /api/auth/set-password/request                                  │
│  │   └─ Backend envía email con código 6 dígitos                         │
│  ├─ Usuario ingresa código de verificación                               │
│  ├─ Usuario crea nueva contraseña                                        │
│  │   • Min 8 caracteres                                                  │
│  │   • 1 mayúscula, 1 minúscula, 1 número, 1 especial                   │
│  └─ POST /api/auth/set-password/confirm                                  │
│      └─ Backend: Hash password + update User.PasswordHash               │
│                                                                            │
│  5️⃣ CONFIRMACIÓN                                                           │
│  Toast: "✅ Contraseña creada exitosamente"                               │
│  Redirect: /dashboard/perfil/seguridad/oauth                             │
│  ├─ Ya tiene contraseña ✓                                                │
│  └─ Puede ver lista actualizada                                          │
│                                                                            │
│  6️⃣ SEGUNDO INTENTO: DESVINCULAR GOOGLE                                   │
│  Click "Desvincular" nuevamente                                          │
│  GET /api/auth/external/can-unlink/google                                │
│  ├─ Backend verifica:                                                     │
│  │   • ¿Tiene contraseña? SÍ ✅                                           │
│  │   • canUnlink = true                                                  │
│  └─ Modal de confirmación final:                                         │
│      "¿Estás seguro de desvincular Google?"                              │
│                                                                            │
│  7️⃣ CONFIRMACIÓN FINAL CON EMAIL CODE                                     │
│  POST /api/auth/external/unlink-request                                  │
│  ├─ Backend envía email con código de 6 dígitos                          │
│  ├─ Usuario ingresa código en modal                                      │
│  └─ POST /api/auth/external/unlink-confirm                               │
│      ├─ Código válido: Proceder                                          │
│      └─ Código inválido: "Código incorrecto"                             │
│                                                                            │
│  8️⃣ PROVIDER DESVINCULADO                                                 │
│  ├─ ExternalLogin.IsActive = false (DB)                                  │
│  ├─ Audit log creado                                                     │
│  ├─ Email de notificación enviado                                        │
│  └─ UI actualizada: Google = "Conectar" (gris)                           │
│                                                                            │
│  9️⃣ ESTADO FINAL                                                           │
│  Usuario ahora tiene:                                                     │
│  ├─ Email + Password (método principal) ✓                                │
│  ├─ Google: Desconectado                                                 │
│  ├─ Facebook: Disponible para conectar                                   │
│  └─ Apple: Disponible para conectar                                      │
│                                                                            │
│  🔐 SEGURIDAD GARANTIZADA                                                  │
│  ├─ NO HAY FORMA de quedar sin método de login                           │
│  ├─ Doble verificación (modal + email code)                              │
│  └─ Audit trail completo en DB                                           │
│                                                                            │
└────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔌 BACKEND API

### Endpoints (Ya Implementados ✅)

```typescript
// Check if provider can be unlinked
GET / api / auth / external / can - unlink / { provider };
// Response: { canUnlink: boolean, reason?: string }

// Request password creation (email code)
POST / api / auth / set - password / request;
// Body: { email: string }

// Confirm password creation
POST / api / auth / set - password / confirm;
// Body: { email: string, code: string, password: string }

// Request provider unlink (email code)
POST / api / auth / external / unlink - request;
// Body: { provider: "Google" | "Facebook" | "Apple" }

// Confirm provider unlink
POST / api / auth / external / unlink - confirm;
// Body: { provider: string, code: string }

// Get connected providers
GET / api / auth / external / providers;
// Response: [ { provider: "Google", isConnected: true, connectedAt: "..." } ]

// Connect new provider
POST / api / auth / external / connect / { provider };
// Initiates OAuth flow
```

---

## 🎨 COMPONENTES

### PASO 1: ConnectedProvidersCard - Lista de Providers

```typescript
// filepath: src/components/profile/oauth/ConnectedProvidersCard.tsx
"use client";

import { useState } from "react";
import { Shield, Link as LinkIcon, Unlink } from "lucide-react";
import { FaGoogle, FaFacebook, FaApple } from "react-icons/fa";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { useConnectedProviders, useCanUnlink } from "@/lib/hooks/useOAuth";
import { UnlinkProviderModal } from "./UnlinkProviderModal";

const providerConfig = {
  Google: {
    icon: FaGoogle,
    color: "text-red-500",
    bg: "bg-red-50",
  },
  Facebook: {
    icon: FaFacebook,
    color: "text-blue-600",
    bg: "bg-blue-50",
  },
  Apple: {
    icon: FaApple,
    color: "text-gray-900",
    bg: "bg-gray-50",
  },
};

export function ConnectedProvidersCard() {
  const { data: providers, isLoading } = useConnectedProviders();
  const [unlinkingProvider, setUnlinkingProvider] = useState<string | null>(null);

  if (isLoading) {
    return <div>Cargando...</div>;
  }

  return (
    <div className="bg-white rounded-lg border p-6">
      <div className="flex items-start justify-between mb-6">
        <div>
          <h2 className="text-lg font-semibold text-gray-900">
            Cuentas Conectadas
          </h2>
          <p className="text-sm text-gray-600 mt-1">
            Gestiona los métodos de inicio de sesión vinculados a tu cuenta
          </p>
        </div>
        <Shield size={24} className="text-gray-400" />
      </div>

      <div className="space-y-4">
        {Object.entries(providerConfig).map(([provider, config]) => {
          const Icon = config.icon;
          const isConnected = providers?.some(
            (p) => p.provider === provider && p.isConnected
          );

          return (
            <div
              key={provider}
              className={`flex items-center justify-between p-4 rounded-lg border ${
                isConnected ? "bg-green-50 border-green-200" : "bg-gray-50"
              }`}
            >
              <div className="flex items-center gap-3">
                <div className={`w-10 h-10 ${config.bg} rounded-full flex items-center justify-center`}>
                  <Icon size={20} className={config.color} />
                </div>
                <div>
                  <p className="font-medium text-gray-900">{provider}</p>
                  {isConnected && (
                    <p className="text-xs text-gray-600">
                      Conectado el{" "}
                      {new Date(
                        providers.find((p) => p.provider === provider)?.connectedAt!
                      ).toLocaleDateString("es-DO")}
                    </p>
                  )}
                </div>
              </div>

              <div className="flex items-center gap-3">
                {isConnected ? (
                  <>
                    <Badge variant="success">Conectado</Badge>
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => setUnlinkingProvider(provider)}
                    >
                      <Unlink size={14} className="mr-1" />
                      Desvincular
                    </Button>
                  </>
                ) : (
                  <Button
                    size="sm"
                    onClick={() => {
                      window.location.href = `/api/auth/external/connect/${provider.toLowerCase()}`;
                    }}
                  >
                    <LinkIcon size={14} className="mr-1" />
                    Conectar
                  </Button>
                )}
              </div>
            </div>
          );
        })}
      </div>

      {/* Security notice */}
      <div className="mt-6 bg-blue-50 border border-blue-200 rounded-lg p-4">
        <p className="text-sm text-blue-800">
          <strong>🔐 Seguridad:</strong> Siempre debes tener al menos un método de
          inicio de sesión activo. Si desvincula un proveedor, asegúrate de tener
          una contraseña configurada.
        </p>
      </div>

      {/* Unlink modal */}
      {unlinkingProvider && (
        <UnlinkProviderModal
          provider={unlinkingProvider}
          onClose={() => setUnlinkingProvider(null)}
          onSuccess={() => {
            setUnlinkingProvider(null);
          }}
        />
      )}
    </div>
  );
}
```

---

### PASO 2: UnlinkProviderModal - Modal de Desvinculación

```typescript
// filepath: src/components/profile/oauth/UnlinkProviderModal.tsx
"use client";

import { useState, useEffect } from "react";
import { AlertTriangle, Unlink, Lock } from "lucide-react";
import { Modal } from "@/components/ui/Modal";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { useCanUnlink, useRequestUnlink, useConfirmUnlink } from "@/lib/hooks/useOAuth";
import { useRouter } from "next/navigation";

interface UnlinkProviderModalProps {
  provider: string;
  onClose: () => void;
  onSuccess: () => void;
}

export function UnlinkProviderModal({
  provider,
  onClose,
  onSuccess,
}: UnlinkProviderModalProps) {
  const router = useRouter();
  const [step, setStep] = useState<"check" | "warning" | "code">("check");
  const [code, setCode] = useState("");

  const { data: canUnlinkData, isLoading } = useCanUnlink(provider);
  const { mutate: requestUnlink, isPending: isRequesting } = useRequestUnlink();
  const { mutate: confirmUnlink, isPending: isConfirming } = useConfirmUnlink();

  useEffect(() => {
    if (canUnlinkData) {
      if (!canUnlinkData.canUnlink) {
        setStep("warning");
      } else {
        setStep("code");
        // Request code immediately
        requestUnlink(provider);
      }
    }
  }, [canUnlinkData, provider]);

  const handleConfirm = () => {
    confirmUnlink(
      { provider, code },
      {
        onSuccess: () => {
          onSuccess();
        },
      }
    );
  };

  // STEP: Checking if can unlink
  if (step === "check" || isLoading) {
    return (
      <Modal isOpen onClose={onClose} title="Verificando...">
        <div className="text-center py-8">
          <p className="text-gray-600">Verificando si puedes desvincular {provider}...</p>
        </div>
      </Modal>
    );
  }

  // STEP: Warning - Cannot unlink (must set password first)
  if (step === "warning") {
    return (
      <Modal
        isOpen
        onClose={onClose}
        title={`No puedes desvincular ${provider}`}
      >
        <div className="space-y-6">
          {/* Warning icon */}
          <div className="flex justify-center">
            <div className="w-16 h-16 bg-yellow-100 rounded-full flex items-center justify-center">
              <AlertTriangle size={32} className="text-yellow-600" />
            </div>
          </div>

          {/* Message */}
          <div className="text-center">
            <h3 className="text-lg font-semibold text-gray-900 mb-2">
              {provider} es tu único método de inicio de sesión
            </h3>
            <p className="text-gray-600">
              Para desvincular {provider}, primero debes crear una contraseña.
              Esto garantiza que siempre tengas una forma de acceder a tu cuenta.
            </p>
          </div>

          {/* Reason detail */}
          {canUnlinkData?.reason === "NoPasswordSet" && (
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
              <div className="flex items-start gap-3">
                <Lock size={20} className="text-blue-600 flex-shrink-0 mt-0.5" />
                <div>
                  <p className="text-sm font-medium text-blue-900">
                    No tienes contraseña configurada
                  </p>
                  <p className="text-sm text-blue-700 mt-1">
                    Registraste tu cuenta usando {provider}. Crea una contraseña
                    antes de desvincular este proveedor.
                  </p>
                </div>
              </div>
            </div>
          )}

          {/* Actions */}
          <div className="flex gap-3">
            <Button variant="outline" onClick={onClose} className="flex-1">
              Cancelar
            </Button>
            <Button
              onClick={() => {
                router.push(`/set-password?provider=${provider.toLowerCase()}`);
              }}
              className="flex-1"
            >
              Crear Contraseña
            </Button>
          </div>
        </div>
      </Modal>
    );
  }

  // STEP: Confirm with email code
  return (
    <Modal
      isOpen
      onClose={onClose}
      title={`Desvincular ${provider}`}
    >
      <div className="space-y-6">
        {/* Info */}
        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
          <div className="flex items-start gap-3">
            <AlertTriangle size={20} className="text-yellow-600 flex-shrink-0" />
            <div>
              <p className="text-sm font-medium text-yellow-900">
                ¿Estás seguro de desvincular {provider}?
              </p>
              <p className="text-sm text-yellow-700 mt-1">
                Ya no podrás iniciar sesión con {provider}. Deberás usar tu email
                y contraseña.
              </p>
            </div>
          </div>
        </div>

        {/* Email code input */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Código de verificación
          </label>
          <p className="text-sm text-gray-600 mb-3">
            Hemos enviado un código de 6 dígitos a tu email
          </p>
          <Input
            type="text"
            placeholder="000000"
            value={code}
            onChange={(e) => setCode(e.target.value.replace(/\D/g, "").slice(0, 6))}
            maxLength={6}
            className="text-center text-2xl tracking-widest"
          />
        </div>

        {/* Actions */}
        <div className="flex gap-3">
          <Button variant="outline" onClick={onClose} className="flex-1">
            Cancelar
          </Button>
          <Button
            onClick={handleConfirm}
            disabled={code.length !== 6 || isConfirming}
            className="flex-1 bg-red-600 hover:bg-red-700"
          >
            {isConfirming ? "Desvinculando..." : "Desvincular"}
          </Button>
        </div>

        {/* Resend code */}
        <button
          onClick={() => requestUnlink(provider)}
          disabled={isRequesting}
          className="text-sm text-primary-600 hover:underline w-full text-center"
        >
          {isRequesting ? "Enviando..." : "Reenviar código"}
        </button>
      </div>
    </Modal>
  );
}
```

---

### PASO 3: SetPasswordForm - Crear Contraseña

```typescript
// filepath: src/components/auth/SetPasswordForm.tsx
"use client";

import { useState } from "react";
import { Eye, EyeOff, CheckCircle, XCircle } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { useRequestSetPassword, useConfirmSetPassword } from "@/lib/hooks/useOAuth";
import { useRouter } from "next/navigation";

interface SetPasswordFormProps {
  email: string;
  provider?: string;
}

export function SetPasswordForm({ email, provider }: SetPasswordFormProps) {
  const router = useRouter();
  const [step, setStep] = useState<"request" | "confirm">("request");
  const [code, setCode] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);

  const { mutate: requestCode, isPending: isRequesting } = useRequestSetPassword();
  const { mutate: confirmPassword: confirmPwd, isPending: isConfirming } = useConfirmSetPassword();

  const passwordRequirements = [
    { label: "Mínimo 8 caracteres", met: password.length >= 8 },
    { label: "Una mayúscula", met: /[A-Z]/.test(password) },
    { label: "Una minúscula", met: /[a-z]/.test(password) },
    { label: "Un número", met: /\d/.test(password) },
    { label: "Un carácter especial", met: /[!@#$%^&*(),.?":{}|<>]/.test(password) },
  ];

  const isPasswordValid = passwordRequirements.every((req) => req.met);
  const passwordsMatch = password === confirmPassword && password.length > 0;

  const handleRequestCode = () => {
    requestCode(
      { email },
      {
        onSuccess: () => {
          setStep("confirm");
        },
      }
    );
  };

  const handleConfirm = () => {
    if (!isPasswordValid || !passwordsMatch) return;

    confirmPwd(
      { email, code, password },
      {
        onSuccess: () => {
          router.push("/dashboard/perfil/seguridad/oauth?success=password-set");
        },
      }
    );
  };

  // STEP 1: Request email code
  if (step === "request") {
    return (
      <div className="space-y-6">
        <div>
          <h2 className="text-2xl font-bold text-gray-900">Crear Contraseña</h2>
          <p className="text-gray-600 mt-2">
            {provider
              ? `Para desvincular ${provider}, primero crea una contraseña`
              : "Crea una contraseña para tu cuenta"}
          </p>
        </div>

        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
          <p className="text-sm text-blue-800">
            Enviaremos un código de verificación a <strong>{email}</strong>
          </p>
        </div>

        <Button onClick={handleRequestCode} disabled={isRequesting} className="w-full">
          {isRequesting ? "Enviando..." : "Enviar código"}
        </Button>
      </div>
    );
  }

  // STEP 2: Enter code + password
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-gray-900">Verificar y Crear Contraseña</h2>
        <p className="text-gray-600 mt-2">
          Ingresa el código que enviamos a tu email
        </p>
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
        />
      </div>

      {/* Password input */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Nueva contraseña
        </label>
        <div className="relative">
          <Input
            type={showPassword ? "text" : "password"}
            placeholder="••••••••"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
          <button
            type="button"
            onClick={() => setShowPassword(!showPassword)}
            className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600"
          >
            {showPassword ? <EyeOff size={20} /> : <Eye size={20} />}
          </button>
        </div>
      </div>

      {/* Confirm password */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Confirmar contraseña
        </label>
        <Input
          type={showPassword ? "text" : "password"}
          placeholder="••••••••"
          value={confirmPassword}
          onChange={(e) => setConfirmPassword(e.target.value)}
        />
        {confirmPassword.length > 0 && (
          <p className={`text-sm mt-1 ${passwordsMatch ? "text-green-600" : "text-red-600"}`}>
            {passwordsMatch ? "✓ Las contraseñas coinciden" : "✗ Las contraseñas no coinciden"}
          </p>
        )}
      </div>

      {/* Password requirements */}
      <div className="bg-gray-50 rounded-lg p-4">
        <p className="text-sm font-medium text-gray-700 mb-3">
          Requisitos de la contraseña:
        </p>
        <div className="space-y-2">
          {passwordRequirements.map((req) => (
            <div key={req.label} className="flex items-center gap-2">
              {req.met ? (
                <CheckCircle size={16} className="text-green-600" />
              ) : (
                <XCircle size={16} className="text-gray-400" />
              )}
              <span
                className={`text-sm ${
                  req.met ? "text-green-700" : "text-gray-600"
                }`}
              >
                {req.label}
              </span>
            </div>
          ))}
        </div>
      </div>

      {/* Submit */}
      <Button
        onClick={handleConfirm}
        disabled={
          code.length !== 6 ||
          !isPasswordValid ||
          !passwordsMatch ||
          isConfirming
        }
        className="w-full"
      >
        {isConfirming ? "Creando contraseña..." : "Crear contraseña"}
      </Button>

      {/* Resend code */}
      <button
        onClick={() => requestCode({ email })}
        disabled={isRequesting}
        className="text-sm text-primary-600 hover:underline w-full text-center"
      >
        {isRequesting ? "Reenviando..." : "Reenviar código"}
      </button>
    </div>
  );
}
```

---

## 📄 PÁGINAS

### PASO 4: OAuth Management Page

```typescript
// filepath: src/app/(main)/dashboard/perfil/seguridad/oauth/page.tsx
import { Metadata } from "next";
import { redirect } from "next/navigation";
import { Shield } from "lucide-react";
import { auth } from "@/lib/auth";
import { ConnectedProvidersCard } from "@/components/profile/oauth/ConnectedProvidersCard";

export const metadata: Metadata = {
  title: "Cuentas Conectadas | OKLA",
};

export default async function OAuthManagementPage() {
  const session = await auth();

  if (!session?.user) {
    redirect("/login?callbackUrl=/dashboard/perfil/seguridad/oauth");
  }

  return (
    <div className="max-w-3xl mx-auto px-4 py-8">
      <div className="flex items-center gap-3 mb-8">
        <Shield size={32} className="text-primary-600" />
        <div>
          <h1 className="text-2xl font-bold text-gray-900">
            Cuentas Conectadas
          </h1>
          <p className="text-gray-600">
            Gestiona los proveedores de inicio de sesión
          </p>
        </div>
      </div>

      <ConnectedProvidersCard />
    </div>
  );
}
```

---

### PASO 5: Set Password Page

```typescript
// filepath: src/app/(auth)/set-password/page.tsx
import { Metadata } from "next";
import { redirect } from "next/navigation";
import { Lock } from "lucide-react";
import { auth } from "@/lib/auth";
import { SetPasswordForm } from "@/components/auth/SetPasswordForm";

export const metadata: Metadata = {
  title: "Crear Contraseña | OKLA",
};

interface SetPasswordPageProps {
  searchParams: { provider?: string };
}

export default async function SetPasswordPage({
  searchParams,
}: SetPasswordPageProps) {
  const session = await auth();

  if (!session?.user) {
    redirect("/login");
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <div className="max-w-md w-full bg-white rounded-lg shadow-lg p-8">
        <div className="text-center mb-8">
          <div className="w-16 h-16 bg-primary-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <Lock size={32} className="text-primary-600" />
          </div>
        </div>

        <SetPasswordForm
          email={session.user.email!}
          provider={searchParams.provider}
        />
      </div>
    </div>
  );
}
```

---

## 🪝 HOOKS Y SERVICIOS

### PASO 6: OAuth Hooks

```typescript
// filepath: src/lib/hooks/useOAuth.ts
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { oauthService } from "@/lib/services/oauthService";
import { toast } from "sonner";

export function useConnectedProviders() {
  return useQuery({
    queryKey: ["connectedProviders"],
    queryFn: () => oauthService.getConnectedProviders(),
  });
}

export function useCanUnlink(provider: string) {
  return useQuery({
    queryKey: ["canUnlink", provider],
    queryFn: () => oauthService.canUnlink(provider),
    enabled: !!provider,
  });
}

export function useRequestUnlink() {
  return useMutation({
    mutationFn: (provider: string) => oauthService.requestUnlink(provider),
    onSuccess: () => {
      toast.success("Código enviado a tu email");
    },
  });
}

export function useConfirmUnlink() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ provider, code }: { provider: string; code: string }) =>
      oauthService.confirmUnlink(provider, code),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["connectedProviders"] });
      toast.success("Proveedor desvinculado exitosamente");
    },
  });
}

export function useRequestSetPassword() {
  return useMutation({
    mutationFn: ({ email }: { email: string }) =>
      oauthService.requestSetPassword(email),
    onSuccess: () => {
      toast.success("Código enviado a tu email");
    },
  });
}

export function useConfirmSetPassword() {
  return useMutation({
    mutationFn: ({
      email,
      code,
      password,
    }: {
      email: string;
      code: string;
      password: string;
    }) => oauthService.confirmSetPassword(email, code, password),
    onSuccess: () => {
      toast.success("Contraseña creada exitosamente");
    },
  });
}
```

---

## 📦 TIPOS TYPESCRIPT

```typescript
// filepath: src/types/oauth.ts
export type OAuthProvider = "Google" | "Facebook" | "Apple";

export interface ConnectedProvider {
  provider: OAuthProvider;
  isConnected: boolean;
  connectedAt?: string;
  email?: string;
}

export interface CanUnlinkResponse {
  canUnlink: boolean;
  reason?: "NoPasswordSet" | "OnlyProvider" | "Other";
  message?: string;
}

export interface SetPasswordRequest {
  email: string;
}

export interface SetPasswordConfirm {
  email: string;
  code: string;
  password: string;
}

export interface UnlinkRequest {
  provider: OAuthProvider;
}

export interface UnlinkConfirm {
  provider: OAuthProvider;
  code: string;
}
```

---

## ✅ VALIDACIÓN

```bash
pnpm dev

# Verificar Flujo Completo:
# 1. Usuario con solo Google (no password)
# - /dashboard/perfil/seguridad/oauth
# - Click "Desvincular Google"
# - Modal: "Debes crear contraseña primero"
# - Click "Crear Contraseña"
# - Redirect a /set-password?provider=google

# 2. Set Password Page
# - Enviar código funciona
# - Validación de contraseña funciona (8 chars, mayúscula, etc)
# - Confirmar contraseña valida match
# - Submit crea password exitosamente
# - Redirect a /dashboard/perfil/seguridad/oauth

# 3. Segundo Intento de Unlink
# - Click "Desvincular Google" nuevamente
# - Modal de confirmación (no warning)
# - Código enviado a email
# - Ingresar código correcto funciona
# - Google se desmarca como conectado

# 4. Estado Final
# - Usuario puede login con email + password
# - Google ya NO aparece como conectado
# - Puede reconectar Google si lo desea
```

---

## 🚀 MEJORAS FUTURAS

1. **2FA Obligatorio**: Requerir 2FA antes de desvincular providers
2. **Session Revocation**: Cerrar todas las sesiones al desvincular
3. **Biometric Auth**: Face ID/Touch ID como método adicional
4. **Account Recovery**: Recover account con provider si olvida password

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/oauth-management.spec.ts
import { test, expect } from "@playwright/test";
import { loginAsUser } from "../helpers/auth";

test.describe("OAuth Management", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsUser(page);
    await page.goto("/settings/security");
  });

  test("debe mostrar proveedores conectados", async ({ page }) => {
    await expect(page.getByTestId("connected-providers")).toBeVisible();
    await expect(page.getByText(/google|facebook|apple/i)).toBeVisible();
  });

  test("debe conectar nuevo proveedor OAuth", async ({ page }) => {
    await page.getByRole("button", { name: /conectar google/i }).click();

    // Popup de OAuth debería abrirse
    const popupPromise = page.waitForEvent("popup");
    const popup = await popupPromise;
    expect(popup.url()).toContain("accounts.google.com");
  });

  test("debe desvincular proveedor con confirmación", async ({ page }) => {
    await page.getByTestId("disconnect-google").click();

    await expect(page.getByRole("dialog")).toBeVisible();
    await expect(page.getByText(/confirmar desvinculación/i)).toBeVisible();

    await page.getByRole("button", { name: /confirmar/i }).click();
    await expect(page.getByText(/google desvinculado/i)).toBeVisible();
  });

  test("debe mostrar estado de cada proveedor", async ({ page }) => {
    const googleProvider = page.getByTestId("provider-google");
    await expect(
      googleProvider.getByText(/conectado|no conectado/i),
    ).toBeVisible();
  });
});
```

---

**Siguiente documento:** Expandir 2FA en `08-perfil.md`
