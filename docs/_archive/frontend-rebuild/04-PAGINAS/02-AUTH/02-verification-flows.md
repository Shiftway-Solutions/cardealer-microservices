---
title: "53 - Auth Verification Flows"
priority: P0
estimated_time: ""
dependencies: []
apis: ["AuthService"]
status: complete
last_updated: "2026-01-30"
---

# 🔐 53 - Auth Verification Flows

**Objetivo:** Flujos de verificación de identidad: email, 2FA (TOTP/SMS/Email), recuperación de contraseña.

**Prioridad:** P0 (CRÍTICO - Seguridad)  
**Complejidad:** 🔴 Muy Alta  
**Dependencias:** AuthService

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura](#-arquitectura)
2. [Páginas](#-páginas)
3. [Hooks](#-hooks)
4. [Tipos TypeScript](#-tipos-typescript)
5. [Flujos](#-flujos)

---

## 🏗️ ARQUITECTURA

```
pages/auth/
├── EmailVerificationPendingPage.tsx  # Esperando verificación
├── VerifyEmailPage.tsx               # Procesar token de verificación
├── TwoFactorSetupPage.tsx            # Configurar 2FA
├── TwoFactorVerifyPage.tsx           # Verificar 2FA en login
├── ForgotPasswordPage.tsx            # Solicitar reset
├── ResetPasswordPage.tsx             # Establecer nueva contraseña
└── SetPasswordPage.tsx               # Primera contraseña (OAuth)
```

### Estructura de Navegación

```
/auth/verify-email-pending  → Esperando que usuario verifique email
/auth/verify-email/:token   → Verificar email con token
/auth/2fa/setup             → Configurar TOTP/SMS/Email
/auth/2fa/verify            → Ingresar código 2FA en login
/auth/forgot-password       → Solicitar link de reset
/auth/reset-password/:token → Establecer nueva contraseña
/auth/set-password          → Crear contraseña (usuarios OAuth)
```

---

## 📄 PÁGINAS

### 1. EmailVerificationPendingPage.tsx

**Ruta:** `/auth/verify-email-pending`

```typescript
// src/pages/auth/EmailVerificationPendingPage.tsx
"use client";

import { useState, useEffect } from "react";
import { Link } from "react-router-dom";
import { Mail, RefreshCw, CheckCircle, ArrowLeft, Loader2 } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useAuth } from "@/hooks/useAuth";
import { toast } from "sonner";

export default function EmailVerificationPendingPage() {
  const { user, resendVerificationEmail, isResending } = useAuth();
  const [cooldown, setCooldown] = useState(0);
  const [emailSent, setEmailSent] = useState(false);

  // Cooldown de 60 segundos entre reenvíos
  useEffect(() => {
    if (cooldown > 0) {
      const timer = setTimeout(() => setCooldown(cooldown - 1), 1000);
      return () => clearTimeout(timer);
    }
  }, [cooldown]);

  const handleResend = async () => {
    if (cooldown > 0 || isResending) return;

    try {
      await resendVerificationEmail();
      setCooldown(60);
      setEmailSent(true);
      toast.success("Email de verificación enviado");
    } catch (error) {
      toast.error("Error al enviar email. Intenta de nuevo.");
    }
  };

  const maskedEmail = user?.email
    ? user.email.replace(/(.{2})(.*)(@.*)/, "$1***$3")
    : "tu correo";

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <div className="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <Mail className="w-8 h-8 text-blue-600" />
          </div>
          <CardTitle className="text-2xl">Verifica tu Email</CardTitle>
        </CardHeader>

        <CardContent className="space-y-6">
          <div className="text-center text-gray-600">
            <p>Hemos enviado un link de verificación a:</p>
            <p className="font-medium text-gray-900 mt-1">{maskedEmail}</p>
          </div>

          {emailSent && (
            <div className="bg-green-50 border border-green-200 rounded-lg p-4 flex items-center gap-3">
              <CheckCircle className="w-5 h-5 text-green-600 flex-shrink-0" />
              <p className="text-sm text-green-700">
                ¡Email enviado! Revisa tu bandeja de entrada.
              </p>
            </div>
          )}

          <div className="bg-gray-50 rounded-lg p-4">
            <h4 className="font-medium mb-2">¿No recibes el email?</h4>
            <ul className="text-sm text-gray-600 space-y-1">
              <li>• Revisa tu carpeta de spam o correo no deseado</li>
              <li>• Asegúrate de que tu email está bien escrito</li>
              <li>• Espera unos minutos, puede tardar en llegar</li>
            </ul>
          </div>

          <Button
            onClick={handleResend}
            disabled={cooldown > 0 || isResending}
            className="w-full"
            variant={cooldown > 0 ? "outline" : "default"}
          >
            {isResending ? (
              <>
                <Loader2 className="w-4 h-4 animate-spin mr-2" />
                Enviando...
              </>
            ) : cooldown > 0 ? (
              <>
                <RefreshCw className="w-4 h-4 mr-2" />
                Reenviar en {cooldown}s
              </>
            ) : (
              <>
                <RefreshCw className="w-4 h-4 mr-2" />
                Reenviar Email de Verificación
              </>
            )}
          </Button>

          <div className="text-center">
            <Link
              to="/login"
              className="text-sm text-gray-600 hover:text-gray-900 inline-flex items-center"
            >
              <ArrowLeft className="w-4 h-4 mr-1" />
              Volver al inicio de sesión
            </Link>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
```

---

### 2. VerifyEmailPage.tsx

**Ruta:** `/auth/verify-email/:token`

```typescript
// src/pages/auth/VerifyEmailPage.tsx
"use client";

import { useEffect, useState } from "react";
import { useParams, useNavigate, Link } from "react-router-dom";
import {
  CheckCircle,
  XCircle,
  Loader2,
  ArrowRight,
  RefreshCw,
} from "lucide-react";
import { Card, CardContent } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { authService } from "@/services/authService";

type VerificationState = "loading" | "success" | "error" | "expired";

export default function VerifyEmailPage() {
  const { token } = useParams<{ token: string }>();
  const navigate = useNavigate();
  const [state, setState] = useState<VerificationState>("loading");
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    if (!token) {
      setState("error");
      setErrorMessage("Token de verificación no proporcionado");
      return;
    }

    const verifyEmail = async () => {
      try {
        await authService.verifyEmail(token);
        setState("success");

        // Redirigir después de 3 segundos
        setTimeout(() => {
          navigate("/login?verified=true");
        }, 3000);
      } catch (error: any) {
        if (error.response?.status === 410) {
          setState("expired");
        } else {
          setState("error");
          setErrorMessage(
            error.response?.data?.message || "Error al verificar email"
          );
        }
      }
    };

    verifyEmail();
  }, [token, navigate]);

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center p-4">
      <Card className="w-full max-w-md">
        <CardContent className="py-12 text-center">
          {state === "loading" && (
            <>
              <Loader2 className="w-16 h-16 animate-spin text-blue-600 mx-auto mb-4" />
              <h2 className="text-xl font-semibold mb-2">Verificando...</h2>
              <p className="text-gray-600">
                Estamos verificando tu dirección de email
              </p>
            </>
          )}

          {state === "success" && (
            <>
              <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
                <CheckCircle className="w-10 h-10 text-green-600" />
              </div>
              <h2 className="text-xl font-semibold text-green-700 mb-2">
                ¡Email Verificado!
              </h2>
              <p className="text-gray-600 mb-6">
                Tu cuenta ha sido verificada exitosamente. Serás redirigido al
                login en unos segundos.
              </p>
              <Button asChild>
                <Link to="/login">
                  Ir a Iniciar Sesión
                  <ArrowRight className="w-4 h-4 ml-2" />
                </Link>
              </Button>
            </>
          )}

          {state === "expired" && (
            <>
              <div className="w-16 h-16 bg-amber-100 rounded-full flex items-center justify-center mx-auto mb-4">
                <RefreshCw className="w-10 h-10 text-amber-600" />
              </div>
              <h2 className="text-xl font-semibold text-amber-700 mb-2">
                Link Expirado
              </h2>
              <p className="text-gray-600 mb-6">
                Este link de verificación ha expirado. Por favor solicita uno
                nuevo.
              </p>
              <Button asChild>
                <Link to="/auth/verify-email-pending">
                  Solicitar Nuevo Link
                </Link>
              </Button>
            </>
          )}

          {state === "error" && (
            <>
              <div className="w-16 h-16 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-4">
                <XCircle className="w-10 h-10 text-red-600" />
              </div>
              <h2 className="text-xl font-semibold text-red-700 mb-2">
                Error de Verificación
              </h2>
              <p className="text-gray-600 mb-6">{errorMessage}</p>
              <div className="space-y-2">
                <Button asChild className="w-full">
                  <Link to="/auth/verify-email-pending">
                    Solicitar Nuevo Link
                  </Link>
                </Button>
                <Button asChild variant="outline" className="w-full">
                  <Link to="/login">Volver al Login</Link>
                </Button>
              </div>
            </>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
```

---

### 3. TwoFactorSetupPage.tsx

**Ruta:** `/auth/2fa/setup`

```typescript
// src/pages/auth/TwoFactorSetupPage.tsx
"use client";

import { useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Shield,
  Smartphone,
  Mail,
  MessageSquare,
  Check,
  Copy,
  Loader2,
  ArrowLeft,
  QrCode,
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/Tabs";
import { useTwoFactorSetup } from "@/hooks/useAuth";
import { toast } from "sonner";
import QRCode from "react-qr-code";

type TwoFactorMethod = "totp" | "sms" | "email";

export default function TwoFactorSetupPage() {
  const navigate = useNavigate();
  const [selectedMethod, setSelectedMethod] = useState<TwoFactorMethod>("totp");
  const [step, setStep] = useState<"select" | "configure" | "verify">("select");
  const [verificationCode, setVerificationCode] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");

  const {
    initTOTP,
    initSMS,
    initEmail,
    verifyAndEnable,
    totpSecret,
    qrCodeUri,
    recoveryCodes,
    isLoading,
    isVerifying,
  } = useTwoFactorSetup();

  const handleMethodSelect = async (method: TwoFactorMethod) => {
    setSelectedMethod(method);

    try {
      if (method === "totp") {
        await initTOTP();
      }
      setStep("configure");
    } catch {
      toast.error("Error al iniciar configuración");
    }
  };

  const handleInitSMS = async () => {
    try {
      await initSMS(phoneNumber);
      setStep("verify");
      toast.success("Código enviado por SMS");
    } catch {
      toast.error("Error al enviar SMS");
    }
  };

  const handleInitEmail = async () => {
    try {
      await initEmail();
      setStep("verify");
      toast.success("Código enviado a tu email");
    } catch {
      toast.error("Error al enviar email");
    }
  };

  const handleVerify = async () => {
    if (verificationCode.length !== 6) {
      toast.error("El código debe tener 6 dígitos");
      return;
    }

    try {
      await verifyAndEnable(selectedMethod, verificationCode);
      toast.success("2FA habilitado correctamente");
      // Mostrar recovery codes y luego navegar
    } catch {
      toast.error("Código inválido. Intenta de nuevo.");
    }
  };

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text);
    toast.success("Copiado al portapapeles");
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center p-4">
      <Card className="w-full max-w-lg">
        <CardHeader>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => navigate(-1)}
            className="absolute left-4 top-4"
          >
            <ArrowLeft className="w-4 h-4 mr-1" />
            Volver
          </Button>
          <div className="text-center pt-4">
            <div className="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mx-auto mb-4">
              <Shield className="w-8 h-8 text-blue-600" />
            </div>
            <CardTitle className="text-2xl">
              Configurar Autenticación de 2 Factores
            </CardTitle>
            <p className="text-gray-600 mt-2">
              Añade una capa extra de seguridad a tu cuenta
            </p>
          </div>
        </CardHeader>

        <CardContent>
          {/* Step 1: Select Method */}
          {step === "select" && (
            <div className="space-y-4">
              <button
                onClick={() => handleMethodSelect("totp")}
                className="w-full p-4 border rounded-lg hover:border-blue-500 hover:bg-blue-50 transition-colors text-left flex items-center gap-4"
              >
                <div className="p-3 bg-blue-100 rounded-lg">
                  <Smartphone className="w-6 h-6 text-blue-600" />
                </div>
                <div className="flex-1">
                  <h3 className="font-medium">App Autenticadora</h3>
                  <p className="text-sm text-gray-500">
                    Google Authenticator, Authy, etc.
                  </p>
                </div>
                <span className="px-2 py-1 bg-green-100 text-green-700 text-xs rounded">
                  Recomendado
                </span>
              </button>

              <button
                onClick={() => handleMethodSelect("sms")}
                className="w-full p-4 border rounded-lg hover:border-blue-500 hover:bg-blue-50 transition-colors text-left flex items-center gap-4"
              >
                <div className="p-3 bg-green-100 rounded-lg">
                  <MessageSquare className="w-6 h-6 text-green-600" />
                </div>
                <div className="flex-1">
                  <h3 className="font-medium">SMS</h3>
                  <p className="text-sm text-gray-500">
                    Recibe código por mensaje de texto
                  </p>
                </div>
              </button>

              <button
                onClick={() => handleMethodSelect("email")}
                className="w-full p-4 border rounded-lg hover:border-blue-500 hover:bg-blue-50 transition-colors text-left flex items-center gap-4"
              >
                <div className="p-3 bg-purple-100 rounded-lg">
                  <Mail className="w-6 h-6 text-purple-600" />
                </div>
                <div className="flex-1">
                  <h3 className="font-medium">Email</h3>
                  <p className="text-sm text-gray-500">
                    Recibe código por correo electrónico
                  </p>
                </div>
              </button>
            </div>
          )}

          {/* Step 2: Configure TOTP */}
          {step === "configure" && selectedMethod === "totp" && (
            <div className="space-y-6">
              <div className="text-center">
                <p className="text-sm text-gray-600 mb-4">
                  Escanea este código QR con tu app autenticadora:
                </p>
                <div className="bg-white p-4 rounded-lg inline-block border">
                  {qrCodeUri && <QRCode value={qrCodeUri} size={180} />}
                </div>
              </div>

              <div className="bg-gray-50 rounded-lg p-4">
                <p className="text-sm text-gray-600 mb-2">
                  ¿No puedes escanear? Ingresa este código manualmente:
                </p>
                <div className="flex items-center gap-2">
                  <code className="flex-1 bg-white px-3 py-2 rounded border text-sm font-mono">
                    {totpSecret}
                  </code>
                  <Button
                    variant="outline"
                    size="icon"
                    onClick={() => copyToClipboard(totpSecret || "")}
                  >
                    <Copy className="w-4 h-4" />
                  </Button>
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium mb-2">
                  Ingresa el código de 6 dígitos de tu app:
                </label>
                <Input
                  type="text"
                  inputMode="numeric"
                  maxLength={6}
                  value={verificationCode}
                  onChange={(e) =>
                    setVerificationCode(e.target.value.replace(/\D/g, ""))
                  }
                  placeholder="000000"
                  className="text-center text-2xl tracking-widest"
                />
              </div>

              <Button
                onClick={handleVerify}
                disabled={verificationCode.length !== 6 || isVerifying}
                className="w-full"
              >
                {isVerifying ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin mr-2" />
                    Verificando...
                  </>
                ) : (
                  "Verificar y Habilitar"
                )}
              </Button>
            </div>
          )}

          {/* Step 2: Configure SMS */}
          {step === "configure" && selectedMethod === "sms" && (
            <div className="space-y-6">
              <div>
                <label className="block text-sm font-medium mb-2">
                  Ingresa tu número de teléfono:
                </label>
                <div className="flex gap-2">
                  <select className="border rounded-lg px-3 py-2 bg-white">
                    <option value="+1">🇩🇴 +1</option>
                    <option value="+1">🇺🇸 +1</option>
                  </select>
                  <Input
                    type="tel"
                    value={phoneNumber}
                    onChange={(e) => setPhoneNumber(e.target.value)}
                    placeholder="809-555-1234"
                    className="flex-1"
                  />
                </div>
              </div>

              <Button
                onClick={handleInitSMS}
                disabled={phoneNumber.length < 10 || isLoading}
                className="w-full"
              >
                {isLoading ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin mr-2" />
                    Enviando...
                  </>
                ) : (
                  "Enviar Código SMS"
                )}
              </Button>
            </div>
          )}

          {/* Step 2: Configure Email */}
          {step === "configure" && selectedMethod === "email" && (
            <div className="space-y-6">
              <div className="text-center text-gray-600">
                <p>Enviaremos un código de verificación a tu email:</p>
                <p className="font-medium text-gray-900 mt-1">user***@gmail.com</p>
              </div>

              <Button onClick={handleInitEmail} disabled={isLoading} className="w-full">
                {isLoading ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin mr-2" />
                    Enviando...
                  </>
                ) : (
                  "Enviar Código por Email"
                )}
              </Button>
            </div>
          )}

          {/* Step 3: Verify (SMS/Email) */}
          {step === "verify" && (selectedMethod === "sms" || selectedMethod === "email") && (
            <div className="space-y-6">
              <div className="text-center text-gray-600">
                <p>
                  Ingresa el código de 6 dígitos que recibiste por{" "}
                  {selectedMethod === "sms" ? "SMS" : "email"}:
                </p>
              </div>

              <Input
                type="text"
                inputMode="numeric"
                maxLength={6}
                value={verificationCode}
                onChange={(e) =>
                  setVerificationCode(e.target.value.replace(/\D/g, ""))
                }
                placeholder="000000"
                className="text-center text-2xl tracking-widest"
                autoFocus
              />

              <Button
                onClick={handleVerify}
                disabled={verificationCode.length !== 6 || isVerifying}
                className="w-full"
              >
                {isVerifying ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin mr-2" />
                    Verificando...
                  </>
                ) : (
                  "Verificar y Habilitar"
                )}
              </Button>
            </div>
          )}

          {/* Recovery Codes (shown after verification) */}
          {recoveryCodes && recoveryCodes.length > 0 && (
            <div className="mt-6 space-y-4">
              <div className="bg-amber-50 border border-amber-200 rounded-lg p-4">
                <h4 className="font-medium text-amber-800 mb-2">
                  ⚠️ Códigos de Recuperación
                </h4>
                <p className="text-sm text-amber-700 mb-4">
                  Guarda estos códigos en un lugar seguro. Los necesitarás si
                  pierdes acceso a tu dispositivo de 2FA.
                </p>
                <div className="grid grid-cols-2 gap-2 mb-4">
                  {recoveryCodes.map((code, idx) => (
                    <code
                      key={idx}
                      className="bg-white px-3 py-1 rounded border text-sm font-mono text-center"
                    >
                      {code}
                    </code>
                  ))}
                </div>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => copyToClipboard(recoveryCodes.join("\n"))}
                  className="w-full"
                >
                  <Copy className="w-4 h-4 mr-2" />
                  Copiar Todos
                </Button>
              </div>

              <Button onClick={() => navigate("/settings/security")} className="w-full">
                <Check className="w-4 h-4 mr-2" />
                Listo, Continuar
              </Button>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
```

---

### 4. TwoFactorVerifyPage.tsx

**Ruta:** `/auth/2fa/verify`

```typescript
// src/pages/auth/TwoFactorVerifyPage.tsx
"use client";

import { useState, useEffect, useRef } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import {
  Shield,
  Smartphone,
  Mail,
  MessageSquare,
  Loader2,
  ArrowLeft,
  KeyRound,
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/Tabs";
import { useTwoFactorVerify } from "@/hooks/useAuth";
import { toast } from "sonner";

type VerifyMethod = "totp" | "sms" | "email" | "recovery";

export default function TwoFactorVerifyPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const [code, setCode] = useState("");
  const [method, setMethod] = useState<VerifyMethod>("totp");
  const [cooldown, setCooldown] = useState(0);
  const inputRefs = useRef<(HTMLInputElement | null)[]>([]);

  // Obtener challengeToken del state o query params
  const challengeToken =
    location.state?.challengeToken ||
    new URLSearchParams(location.search).get("token");

  const availableMethods: VerifyMethod[] =
    location.state?.availableMethods || ["totp"];

  const {
    verify,
    sendCode,
    isVerifying,
    isSending,
  } = useTwoFactorVerify(challengeToken);

  // Cooldown para reenvío
  useEffect(() => {
    if (cooldown > 0) {
      const timer = setTimeout(() => setCooldown(cooldown - 1), 1000);
      return () => clearTimeout(timer);
    }
  }, [cooldown]);

  // Redirigir si no hay token
  useEffect(() => {
    if (!challengeToken) {
      navigate("/login");
    }
  }, [challengeToken, navigate]);

  // Auto-submit cuando se completan 6 dígitos
  useEffect(() => {
    if (code.length === 6) {
      handleVerify();
    }
  }, [code]);

  const handleCodeChange = (value: string) => {
    // Solo permitir dígitos
    const cleaned = value.replace(/\D/g, "").slice(0, 6);
    setCode(cleaned);
  };

  const handleSendCode = async () => {
    if (cooldown > 0 || isSending) return;

    try {
      await sendCode(method);
      setCooldown(60);
      toast.success(
        method === "sms"
          ? "Código enviado por SMS"
          : "Código enviado a tu email"
      );
    } catch {
      toast.error("Error al enviar código");
    }
  };

  const handleVerify = async () => {
    if (code.length !== 6 && method !== "recovery") {
      toast.error("Ingresa el código completo");
      return;
    }

    try {
      await verify(method, code);
      // El hook maneja la redirección después de verificar
    } catch (error: any) {
      toast.error(error.message || "Código inválido");
      setCode("");
    }
  };

  const getMethodIcon = (m: VerifyMethod) => {
    switch (m) {
      case "totp":
        return <Smartphone className="w-4 h-4" />;
      case "sms":
        return <MessageSquare className="w-4 h-4" />;
      case "email":
        return <Mail className="w-4 h-4" />;
      case "recovery":
        return <KeyRound className="w-4 h-4" />;
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => navigate("/login")}
            className="absolute left-4 top-4"
          >
            <ArrowLeft className="w-4 h-4 mr-1" />
            Volver
          </Button>
          <div className="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <Shield className="w-8 h-8 text-blue-600" />
          </div>
          <CardTitle className="text-2xl">Verificación de 2 Pasos</CardTitle>
          <p className="text-gray-600 mt-2">
            Ingresa el código de verificación para continuar
          </p>
        </CardHeader>

        <CardContent>
          {/* Method Tabs */}
          {availableMethods.length > 1 && (
            <Tabs
              value={method}
              onValueChange={(v) => {
                setMethod(v as VerifyMethod);
                setCode("");
              }}
              className="mb-6"
            >
              <TabsList className="grid grid-cols-3 w-full">
                {availableMethods.includes("totp") && (
                  <TabsTrigger value="totp" className="flex items-center gap-1">
                    <Smartphone className="w-4 h-4" />
                    <span className="hidden sm:inline">App</span>
                  </TabsTrigger>
                )}
                {availableMethods.includes("sms") && (
                  <TabsTrigger value="sms" className="flex items-center gap-1">
                    <MessageSquare className="w-4 h-4" />
                    <span className="hidden sm:inline">SMS</span>
                  </TabsTrigger>
                )}
                {availableMethods.includes("email") && (
                  <TabsTrigger value="email" className="flex items-center gap-1">
                    <Mail className="w-4 h-4" />
                    <span className="hidden sm:inline">Email</span>
                  </TabsTrigger>
                )}
              </TabsList>
            </Tabs>
          )}

          {/* TOTP Input */}
          {method === "totp" && (
            <div className="space-y-6">
              <p className="text-sm text-gray-600 text-center">
                Abre tu app autenticadora e ingresa el código de 6 dígitos
              </p>

              {/* 6-digit code input */}
              <div className="flex justify-center gap-2">
                {[0, 1, 2, 3, 4, 5].map((idx) => (
                  <Input
                    key={idx}
                    ref={(el) => (inputRefs.current[idx] = el)}
                    type="text"
                    inputMode="numeric"
                    maxLength={1}
                    value={code[idx] || ""}
                    onChange={(e) => {
                      const newCode = code.split("");
                      newCode[idx] = e.target.value;
                      handleCodeChange(newCode.join(""));
                      // Auto-focus next
                      if (e.target.value && idx < 5) {
                        inputRefs.current[idx + 1]?.focus();
                      }
                    }}
                    onKeyDown={(e) => {
                      // Handle backspace
                      if (e.key === "Backspace" && !code[idx] && idx > 0) {
                        inputRefs.current[idx - 1]?.focus();
                      }
                    }}
                    className="w-12 h-14 text-center text-2xl font-bold"
                  />
                ))}
              </div>
            </div>
          )}

          {/* SMS/Email Input */}
          {(method === "sms" || method === "email") && (
            <div className="space-y-6">
              <div className="text-center">
                <p className="text-sm text-gray-600">
                  {method === "sms"
                    ? "Te enviaremos un código a tu número de teléfono registrado"
                    : "Te enviaremos un código a tu email registrado"}
                </p>
              </div>

              {/* Send code button */}
              <Button
                onClick={handleSendCode}
                disabled={cooldown > 0 || isSending}
                variant="outline"
                className="w-full"
              >
                {isSending ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin mr-2" />
                    Enviando...
                  </>
                ) : cooldown > 0 ? (
                  `Reenviar en ${cooldown}s`
                ) : (
                  `Enviar Código por ${method === "sms" ? "SMS" : "Email"}`
                )}
              </Button>

              {/* Code input */}
              <Input
                type="text"
                inputMode="numeric"
                maxLength={6}
                value={code}
                onChange={(e) => handleCodeChange(e.target.value)}
                placeholder="000000"
                className="text-center text-2xl tracking-widest"
              />
            </div>
          )}

          {/* Recovery Code Input */}
          {method === "recovery" && (
            <div className="space-y-6">
              <p className="text-sm text-gray-600 text-center">
                Ingresa uno de tus códigos de recuperación de 10 caracteres
              </p>

              <Input
                type="text"
                value={code}
                onChange={(e) => setCode(e.target.value.toUpperCase())}
                placeholder="XXXX-XXXX-XX"
                className="text-center text-lg tracking-wider font-mono"
                maxLength={12}
              />
            </div>
          )}

          {/* Verify Button */}
          <Button
            onClick={handleVerify}
            disabled={code.length < 6 || isVerifying}
            className="w-full mt-6"
          >
            {isVerifying ? (
              <>
                <Loader2 className="w-4 h-4 animate-spin mr-2" />
                Verificando...
              </>
            ) : (
              "Verificar"
            )}
          </Button>

          {/* Use Recovery Code */}
          {method !== "recovery" && (
            <div className="mt-6 text-center">
              <button
                onClick={() => {
                  setMethod("recovery");
                  setCode("");
                }}
                className="text-sm text-blue-600 hover:text-blue-700"
              >
                ¿No tienes acceso? Usa un código de recuperación
              </button>
            </div>
          )}

          {method === "recovery" && (
            <div className="mt-6 text-center">
              <button
                onClick={() => {
                  setMethod("totp");
                  setCode("");
                }}
                className="text-sm text-gray-600 hover:text-gray-900"
              >
                Volver a verificación normal
              </button>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
```

---

### 5. ForgotPasswordPage.tsx

**Ruta:** `/auth/forgot-password`

```typescript
// src/pages/auth/ForgotPasswordPage.tsx
"use client";

import { useState } from "react";
import { Link } from "react-router-dom";
import { Mail, ArrowLeft, Loader2, CheckCircle } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Label } from "@/components/ui/Label";
import { authService } from "@/services/authService";
import { toast } from "sonner";
import { z } from "zod";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";

const schema = z.object({
  email: z.string().email("Email inválido"),
});

type FormData = z.infer<typeof schema>;

export default function ForgotPasswordPage() {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [submittedEmail, setSubmittedEmail] = useState("");

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
  });

  const onSubmit = async (data: FormData) => {
    setIsSubmitting(true);
    try {
      await authService.requestPasswordReset(data.email);
      setSubmittedEmail(data.email);
      setSubmitted(true);
    } catch (error: any) {
      // No revelar si el email existe o no por seguridad
      setSubmittedEmail(data.email);
      setSubmitted(true);
    } finally {
      setIsSubmitting(false);
    }
  };

  if (submitted) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center p-4">
        <Card className="w-full max-w-md">
          <CardContent className="py-12 text-center">
            <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
              <CheckCircle className="w-10 h-10 text-green-600" />
            </div>
            <h2 className="text-xl font-semibold mb-2">Revisa tu Email</h2>
            <p className="text-gray-600 mb-6">
              Si existe una cuenta con{" "}
              <span className="font-medium">{submittedEmail}</span>, recibirás
              un link para restablecer tu contraseña.
            </p>
            <div className="bg-gray-50 rounded-lg p-4 text-sm text-gray-600 mb-6">
              <p>¿No recibes el email?</p>
              <ul className="mt-2 text-left space-y-1">
                <li>• Revisa tu carpeta de spam</li>
                <li>• Verifica que el email esté correcto</li>
                <li>• Espera unos minutos</li>
              </ul>
            </div>
            <Button asChild variant="outline" className="w-full">
              <Link to="/login">
                <ArrowLeft className="w-4 h-4 mr-2" />
                Volver al Login
              </Link>
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <div className="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <Mail className="w-8 h-8 text-blue-600" />
          </div>
          <CardTitle className="text-2xl">¿Olvidaste tu Contraseña?</CardTitle>
          <p className="text-gray-600 mt-2">
            Ingresa tu email y te enviaremos un link para restablecerla
          </p>
        </CardHeader>

        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
            <div>
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                type="email"
                {...register("email")}
                placeholder="tu@email.com"
                className="mt-1"
              />
              {errors.email && (
                <p className="text-red-500 text-sm mt-1">{errors.email.message}</p>
              )}
            </div>

            <Button type="submit" disabled={isSubmitting} className="w-full">
              {isSubmitting ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin mr-2" />
                  Enviando...
                </>
              ) : (
                "Enviar Link de Recuperación"
              )}
            </Button>
          </form>

          <div className="mt-6 text-center">
            <Link
              to="/login"
              className="text-sm text-gray-600 hover:text-gray-900 inline-flex items-center"
            >
              <ArrowLeft className="w-4 h-4 mr-1" />
              Volver al Login
            </Link>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
```

---

### 6. ResetPasswordPage.tsx

**Ruta:** `/auth/reset-password/:token`

```typescript
// src/pages/auth/ResetPasswordPage.tsx
"use client";

import { useState, useEffect } from "react";
import { useParams, useNavigate, Link } from "react-router-dom";
import {
  Lock,
  Eye,
  EyeOff,
  CheckCircle,
  XCircle,
  Loader2,
  ArrowRight,
  Check,
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Label } from "@/components/ui/Label";
import { authService } from "@/services/authService";
import { toast } from "sonner";
import { z } from "zod";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";

const schema = z
  .object({
    password: z
      .string()
      .min(8, "Mínimo 8 caracteres")
      .regex(/[A-Z]/, "Debe contener al menos una mayúscula")
      .regex(/[a-z]/, "Debe contener al menos una minúscula")
      .regex(/[0-9]/, "Debe contener al menos un número")
      .regex(/[^A-Za-z0-9]/, "Debe contener al menos un carácter especial"),
    confirmPassword: z.string(),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: "Las contraseñas no coinciden",
    path: ["confirmPassword"],
  });

type FormData = z.infer<typeof schema>;

export default function ResetPasswordPage() {
  const { token } = useParams<{ token: string }>();
  const navigate = useNavigate();
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [success, setSuccess] = useState(false);
  const [tokenValid, setTokenValid] = useState<boolean | null>(null);

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
  });

  const password = watch("password", "");

  // Verificar token al cargar
  useEffect(() => {
    if (!token) {
      setTokenValid(false);
      return;
    }

    const verifyToken = async () => {
      try {
        await authService.verifyResetToken(token);
        setTokenValid(true);
      } catch {
        setTokenValid(false);
      }
    };

    verifyToken();
  }, [token]);

  // Password requirements check
  const requirements = [
    { label: "Mínimo 8 caracteres", valid: password.length >= 8 },
    { label: "Una letra mayúscula", valid: /[A-Z]/.test(password) },
    { label: "Una letra minúscula", valid: /[a-z]/.test(password) },
    { label: "Un número", valid: /[0-9]/.test(password) },
    { label: "Un carácter especial", valid: /[^A-Za-z0-9]/.test(password) },
  ];

  const onSubmit = async (data: FormData) => {
    if (!token) return;

    setIsSubmitting(true);
    try {
      await authService.resetPassword(token, data.password);
      setSuccess(true);
      toast.success("Contraseña actualizada correctamente");
    } catch (error: any) {
      toast.error(error.message || "Error al restablecer contraseña");
    } finally {
      setIsSubmitting(false);
    }
  };

  // Token inválido
  if (tokenValid === false) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center p-4">
        <Card className="w-full max-w-md">
          <CardContent className="py-12 text-center">
            <div className="w-16 h-16 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-4">
              <XCircle className="w-10 h-10 text-red-600" />
            </div>
            <h2 className="text-xl font-semibold text-red-700 mb-2">
              Link Inválido o Expirado
            </h2>
            <p className="text-gray-600 mb-6">
              Este link de recuperación no es válido o ha expirado. Por favor
              solicita uno nuevo.
            </p>
            <Button asChild className="w-full">
              <Link to="/auth/forgot-password">Solicitar Nuevo Link</Link>
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  // Verificando token
  if (tokenValid === null) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center p-4">
        <Loader2 className="w-8 h-8 animate-spin text-blue-600" />
      </div>
    );
  }

  // Éxito
  if (success) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center p-4">
        <Card className="w-full max-w-md">
          <CardContent className="py-12 text-center">
            <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
              <CheckCircle className="w-10 h-10 text-green-600" />
            </div>
            <h2 className="text-xl font-semibold text-green-700 mb-2">
              ¡Contraseña Actualizada!
            </h2>
            <p className="text-gray-600 mb-6">
              Tu contraseña ha sido restablecida exitosamente. Ya puedes iniciar
              sesión con tu nueva contraseña.
            </p>
            <Button asChild className="w-full">
              <Link to="/login">
                Ir a Iniciar Sesión
                <ArrowRight className="w-4 h-4 ml-2" />
              </Link>
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <div className="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <Lock className="w-8 h-8 text-blue-600" />
          </div>
          <CardTitle className="text-2xl">Nueva Contraseña</CardTitle>
          <p className="text-gray-600 mt-2">
            Crea una nueva contraseña segura para tu cuenta
          </p>
        </CardHeader>

        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
            {/* Password */}
            <div>
              <Label htmlFor="password">Nueva Contraseña</Label>
              <div className="relative mt-1">
                <Input
                  id="password"
                  type={showPassword ? "text" : "password"}
                  {...register("password")}
                  placeholder="••••••••"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-500"
                >
                  {showPassword ? (
                    <EyeOff className="w-4 h-4" />
                  ) : (
                    <Eye className="w-4 h-4" />
                  )}
                </button>
              </div>
              {errors.password && (
                <p className="text-red-500 text-sm mt-1">
                  {errors.password.message}
                </p>
              )}
            </div>

            {/* Password Requirements */}
            <div className="bg-gray-50 rounded-lg p-4">
              <p className="text-sm font-medium mb-2">
                Requisitos de contraseña:
              </p>
              <ul className="space-y-1">
                {requirements.map((req, idx) => (
                  <li
                    key={idx}
                    className={`flex items-center gap-2 text-sm ${
                      req.valid ? "text-green-600" : "text-gray-500"
                    }`}
                  >
                    {req.valid ? (
                      <Check className="w-4 h-4" />
                    ) : (
                      <div className="w-4 h-4 border rounded-full" />
                    )}
                    {req.label}
                  </li>
                ))}
              </ul>
            </div>

            {/* Confirm Password */}
            <div>
              <Label htmlFor="confirmPassword">Confirmar Contraseña</Label>
              <div className="relative mt-1">
                <Input
                  id="confirmPassword"
                  type={showConfirm ? "text" : "password"}
                  {...register("confirmPassword")}
                  placeholder="••••••••"
                />
                <button
                  type="button"
                  onClick={() => setShowConfirm(!showConfirm)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-500"
                >
                  {showConfirm ? (
                    <EyeOff className="w-4 h-4" />
                  ) : (
                    <Eye className="w-4 h-4" />
                  )}
                </button>
              </div>
              {errors.confirmPassword && (
                <p className="text-red-500 text-sm mt-1">
                  {errors.confirmPassword.message}
                </p>
              )}
            </div>

            <Button type="submit" disabled={isSubmitting} className="w-full">
              {isSubmitting ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin mr-2" />
                  Actualizando...
                </>
              ) : (
                "Restablecer Contraseña"
              )}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
```

---

## 🪝 HOOKS

### useAuth.ts (2FA Extensions)

```typescript
// src/hooks/useAuth.ts (extensiones para 2FA)
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { authService } from "@/services/authService";
import { useNavigate } from "react-router-dom";

export function useTwoFactorSetup() {
  const queryClient = useQueryClient();
  const [totpSecret, setTotpSecret] = useState<string | null>(null);
  const [qrCodeUri, setQrCodeUri] = useState<string | null>(null);
  const [recoveryCodes, setRecoveryCodes] = useState<string[] | null>(null);

  const initTOTPMutation = useMutation({
    mutationFn: () => authService.initTOTP(),
    onSuccess: (data) => {
      setTotpSecret(data.secret);
      setQrCodeUri(data.qrCodeUri);
    },
  });

  const initSMSMutation = useMutation({
    mutationFn: (phone: string) => authService.initSMS2FA(phone),
  });

  const initEmailMutation = useMutation({
    mutationFn: () => authService.initEmail2FA(),
  });

  const verifyMutation = useMutation({
    mutationFn: ({
      method,
      code,
    }: {
      method: "totp" | "sms" | "email";
      code: string;
    }) => authService.verifyAndEnable2FA(method, code),
    onSuccess: (data) => {
      setRecoveryCodes(data.recoveryCodes);
      queryClient.invalidateQueries({ queryKey: ["user"] });
    },
  });

  return {
    initTOTP: initTOTPMutation.mutateAsync,
    initSMS: initSMSMutation.mutateAsync,
    initEmail: initEmailMutation.mutateAsync,
    verifyAndEnable: (method: "totp" | "sms" | "email", code: string) =>
      verifyMutation.mutateAsync({ method, code }),
    totpSecret,
    qrCodeUri,
    recoveryCodes,
    isLoading:
      initTOTPMutation.isPending ||
      initSMSMutation.isPending ||
      initEmailMutation.isPending,
    isVerifying: verifyMutation.isPending,
  };
}

export function useTwoFactorVerify(challengeToken: string) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const verifyMutation = useMutation({
    mutationFn: ({
      method,
      code,
    }: {
      method: "totp" | "sms" | "email" | "recovery";
      code: string;
    }) => authService.verify2FA(challengeToken, method, code),
    onSuccess: (data) => {
      // Guardar tokens
      localStorage.setItem("accessToken", data.accessToken);
      localStorage.setItem("refreshToken", data.refreshToken);

      // Invalidar queries y redirigir
      queryClient.invalidateQueries({ queryKey: ["user"] });
      navigate(data.redirectTo || "/");
    },
  });

  const sendCodeMutation = useMutation({
    mutationFn: (method: "sms" | "email") =>
      authService.send2FACode(challengeToken, method),
  });

  return {
    verify: (method: "totp" | "sms" | "email" | "recovery", code: string) =>
      verifyMutation.mutateAsync({ method, code }),
    sendCode: sendCodeMutation.mutateAsync,
    isVerifying: verifyMutation.isPending,
    isSending: sendCodeMutation.isPending,
  };
}
```

---

## 📝 TIPOS TYPESCRIPT

```typescript
// src/types/auth.ts
export interface TwoFactorSetupResponse {
  secret?: string;
  qrCodeUri?: string;
  recoveryCodes?: string[];
}

export interface TwoFactorChallengeResponse {
  challengeToken: string;
  availableMethods: ("totp" | "sms" | "email")[];
  maskedPhone?: string;
  maskedEmail?: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  user: User;
  requires2FA?: boolean;
  twoFactorChallenge?: TwoFactorChallengeResponse;
}

export interface PasswordResetRequest {
  email: string;
}

export interface PasswordResetConfirm {
  token: string;
  newPassword: string;
}

export interface EmailVerificationStatus {
  isVerified: boolean;
  email: string;
  verifiedAt?: string;
}
```

---

## 🛣️ RUTAS

```typescript
// src/App.tsx
import EmailVerificationPendingPage from "./pages/auth/EmailVerificationPendingPage";
import VerifyEmailPage from "./pages/auth/VerifyEmailPage";
import TwoFactorSetupPage from "./pages/auth/TwoFactorSetupPage";
import TwoFactorVerifyPage from "./pages/auth/TwoFactorVerifyPage";
import ForgotPasswordPage from "./pages/auth/ForgotPasswordPage";
import ResetPasswordPage from "./pages/auth/ResetPasswordPage";
import SetPasswordPage from "./pages/auth/SetPasswordPage";

// Rutas públicas de auth
<Route path="/auth/verify-email-pending" element={<EmailVerificationPendingPage />} />
<Route path="/auth/verify-email/:token" element={<VerifyEmailPage />} />
<Route path="/auth/2fa/verify" element={<TwoFactorVerifyPage />} />
<Route path="/auth/forgot-password" element={<ForgotPasswordPage />} />
<Route path="/auth/reset-password/:token" element={<ResetPasswordPage />} />

// Rutas protegidas
<Route path="/auth/2fa/setup" element={<ProtectedRoute><TwoFactorSetupPage /></ProtectedRoute>} />
<Route path="/auth/set-password" element={<ProtectedRoute><SetPasswordPage /></ProtectedRoute>} />
```

---

## 🔄 FLUJOS

### Flujo de Verificación de Email

```
┌─────────────────────────────────────────────────────────────────┐
│               VERIFICACIÓN DE EMAIL                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  1️⃣ REGISTRO                                                    │
│  ├─> Usuario completa registro                                 │
│  ├─> Backend crea cuenta con emailVerified=false               │
│  ├─> Envía email con link de verificación                      │
│  └─> Redirige a /auth/verify-email-pending                     │
│                                                                 │
│  2️⃣ PENDIENTE                                                   │
│  ├─> Usuario ve página de espera                               │
│  ├─> Puede reenviar email (cooldown 60s)                       │
│  └─> Link en email: /auth/verify-email/:token                  │
│                                                                 │
│  3️⃣ VERIFICACIÓN                                                │
│  ├─> Usuario hace clic en link                                 │
│  ├─> Frontend envía token a backend                            │
│  ├─> Backend valida y actualiza emailVerified=true             │
│  └─> Redirige a /login?verified=true                           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Flujo de 2FA

```
┌─────────────────────────────────────────────────────────────────┐
│              AUTENTICACIÓN DE 2 FACTORES                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  📱 CONFIGURACIÓN (Settings → Security → Enable 2FA)            │
│  ├─> Seleccionar método: TOTP, SMS, Email                      │
│  ├─> TOTP: Escanear QR o copiar secret                         │
│  ├─> SMS: Verificar número de teléfono                         │
│  ├─> Email: Enviar código al email registrado                  │
│  ├─> Ingresar código de 6 dígitos                              │
│  └─> Guardar 10 códigos de recuperación                        │
│                                                                 │
│  🔐 LOGIN CON 2FA                                                │
│  ├─> Usuario ingresa email + contraseña                        │
│  ├─> Backend valida credenciales                               │
│  ├─> Si 2FA habilitado → retorna challengeToken                │
│  ├─> Frontend redirige a /auth/2fa/verify                      │
│  ├─> Usuario ingresa código (TOTP/SMS/Email)                   │
│  ├─> O usa código de recuperación                              │
│  └─> Backend retorna accessToken + refreshToken                │
│                                                                 │
│  🔑 CÓDIGOS DE RECUPERACIÓN                                      │
│  ├─> 10 códigos de un solo uso                                 │
│  ├─> Cada código: XXXX-XXXX-XX (10 chars)                      │
│  ├─> Se invalidan al usar                                      │
│  └─> Regenerar desde Settings si se agotan                     │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Flujo de Recuperación de Contraseña

```
┌─────────────────────────────────────────────────────────────────┐
│            RECUPERACIÓN DE CONTRASEÑA                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  1️⃣ SOLICITUD                                                   │
│  ├─> Usuario hace clic en "¿Olvidaste tu contraseña?"          │
│  ├─> /auth/forgot-password                                     │
│  ├─> Ingresa email                                             │
│  ├─> Backend envía link (válido 24h)                           │
│  └─> Muestra mensaje genérico (seguridad)                      │
│                                                                 │
│  2️⃣ RESET                                                       │
│  ├─> Usuario hace clic en link del email                       │
│  ├─> /auth/reset-password/:token                               │
│  ├─> Frontend valida token con backend                         │
│  ├─> Si válido → muestra formulario                            │
│  ├─> Si inválido/expirado → muestra error                      │
│  └─> Usuario crea nueva contraseña                             │
│                                                                 │
│  3️⃣ CONFIRMACIÓN                                                │
│  ├─> Backend actualiza contraseña                              │
│  ├─> Invalida todas las sesiones anteriores                    │
│  ├─> Envía email de confirmación                               │
│  └─> Redirige a /login                                         │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## ✅ VALIDACIÓN

### Email Verification

- [ ] Página pending muestra email enmascarado
- [ ] Reenvío con cooldown de 60 segundos
- [ ] Token válido → success + redirect
- [ ] Token expirado → mostrar opción de reenvío
- [ ] Token inválido → error message

### 2FA Setup

- [ ] TOTP: QR code renderiza correctamente
- [ ] TOTP: Secret copiable
- [ ] SMS: Validación de número
- [ ] Email: Envío con cooldown
- [ ] Códigos de recuperación mostrados
- [ ] Copiar todos los códigos

### 2FA Verify

- [ ] Input de 6 dígitos con auto-focus
- [ ] Auto-submit al completar
- [ ] Tabs entre métodos disponibles
- [ ] Opción de recovery code
- [ ] Cooldown en reenvío SMS/Email

### Password Reset

- [ ] Validación de email
- [ ] Mensaje genérico (no revelar existencia)
- [ ] Token validation al cargar página
- [ ] Password requirements visual
- [ ] Match validation
- [ ] Success redirect a login

---

## 🧪 TESTS E2E (PLAYWRIGHT)

```typescript
// filepath: e2e/verification-flows.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Verification Flows", () => {
  test.describe("Email Verification", () => {
    test("debe mostrar página de verificación de email", async ({ page }) => {
      await page.goto("/verificar-email?token=test-token");
      await expect(
        page.getByRole("heading", { name: /verificar email/i }),
      ).toBeVisible();
    });

    test("debe aceptar código de 6 dígitos", async ({ page }) => {
      await page.goto("/verificar-email");

      const inputs = page.locator('input[type="text"]');
      await inputs.nth(0).fill("1");
      await inputs.nth(1).fill("2");
      await inputs.nth(2).fill("3");
      await inputs.nth(3).fill("4");
      await inputs.nth(4).fill("5");
      await inputs.nth(5).fill("6");

      await expect(page).toHaveURL(/\/login/);
    });

    test("debe reenviar código con cooldown", async ({ page }) => {
      await page.goto("/verificar-email");

      await page.getByRole("button", { name: /reenviar/i }).click();
      await expect(page.getByText(/código enviado/i)).toBeVisible();
      await expect(
        page.getByRole("button", { name: /reenviar/i }),
      ).toBeDisabled();
    });
  });

  test.describe("Password Reset", () => {
    test("debe solicitar reset de contraseña", async ({ page }) => {
      await page.goto("/recuperar-password");

      await page.fill('input[name="email"]', "test@example.com");
      await page.click('button[type="submit"]');

      await expect(page.getByText(/revisa tu email/i)).toBeVisible();
    });

    test("debe cambiar contraseña con token válido", async ({ page }) => {
      await page.goto("/reset-password?token=valid-token");

      await page.fill('input[name="password"]', "NewSecurePass123!");
      await page.fill('input[name="confirmPassword"]', "NewSecurePass123!");
      await page.click('button[type="submit"]');

      await expect(page).toHaveURL("/login");
      await expect(page.getByText(/contraseña actualizada/i)).toBeVisible();
    });
  });

  test.describe("2FA Verification", () => {
    test("debe verificar con código 2FA", async ({ page }) => {
      await page.goto("/2fa-verify");

      await page.fill('input[name="code"]', "123456");
      await page.click('button[type="submit"]');

      await expect(page).toHaveURL("/dashboard");
    });
  });
});
```

---

_Última actualización: Enero 2026_
