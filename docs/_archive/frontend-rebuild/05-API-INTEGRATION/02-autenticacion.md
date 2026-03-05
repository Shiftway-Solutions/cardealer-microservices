# 🔐 Autenticación con NextAuth.js

> **Tiempo estimado:** 60 minutos
> **Prerrequisitos:** Proyecto Next.js configurado, API de AuthService

---

## 📋 OBJETIVO

Implementar autenticación completa:

- NextAuth.js con Credentials provider
- JWT + Refresh tokens
- Protección de rutas
- Hooks y contexto de usuario
- Páginas de login/registro
- Manejo de sesión

---

## 🔧 PASO 1: Instalar NextAuth

```bash
pnpm add next-auth@beta
pnpm add -D @types/bcryptjs
```

---

## 🔧 PASO 2: Configurar NextAuth

```typescript
// filepath: src/lib/auth/config.ts
import { NextAuthConfig } from "next-auth";
import Credentials from "next-auth/providers/credentials";
import { z } from "zod";

const loginSchema = z.object({
  email: z.string().email(),
  password: z.string().min(6),
});

export const authConfig: NextAuthConfig = {
  pages: {
    signIn: "/login",
    signOut: "/logout",
    error: "/login",
    newUser: "/registro",
  },
  providers: [
    Credentials({
      name: "credentials",
      credentials: {
        email: { label: "Email", type: "email" },
        password: { label: "Password", type: "password" },
      },
      async authorize(credentials) {
        const parsed = loginSchema.safeParse(credentials);

        if (!parsed.success) {
          return null;
        }

        try {
          const response = await fetch(
            `${process.env.API_URL}/api/auth/login`,
            {
              method: "POST",
              headers: { "Content-Type": "application/json" },
              body: JSON.stringify({
                email: parsed.data.email,
                password: parsed.data.password,
              }),
            },
          );

          if (!response.ok) {
            return null;
          }

          const data = await response.json();

          return {
            id: data.user.id,
            email: data.user.email,
            name: data.user.fullName,
            image: data.user.avatar,
            role: data.user.role,
            accessToken: data.accessToken,
            refreshToken: data.refreshToken,
            accessTokenExpires: data.expiresAt,
          };
        } catch {
          return null;
        }
      },
    }),
  ],
  callbacks: {
    authorized({ auth, request: { nextUrl } }) {
      const isLoggedIn = !!auth?.user;
      const isOnDashboard = nextUrl.pathname.startsWith("/dashboard");
      const isOnPublish = nextUrl.pathname.startsWith("/publicar");
      const isOnFavorites = nextUrl.pathname.startsWith("/favoritos");

      // Protected routes
      if (isOnDashboard || isOnPublish || isOnFavorites) {
        if (isLoggedIn) return true;
        return false; // Redirect to login
      }

      return true;
    },
    async jwt({ token, user, trigger, session }) {
      // Initial sign in
      if (user) {
        return {
          ...token,
          id: user.id,
          role: user.role,
          accessToken: user.accessToken,
          refreshToken: user.refreshToken,
          accessTokenExpires: user.accessTokenExpires,
        };
      }

      // Update session
      if (trigger === "update" && session) {
        return { ...token, ...session };
      }

      // Return previous token if not expired
      if (Date.now() < (token.accessTokenExpires as number) - 60000) {
        return token;
      }

      // Refresh token
      return await refreshAccessToken(token);
    },
    async session({ session, token }) {
      if (token) {
        session.user.id = token.id as string;
        session.user.role = token.role as string;
        session.accessToken = token.accessToken as string;
        session.error = token.error as string | undefined;
      }
      return session;
    },
  },
  session: {
    strategy: "jwt",
    maxAge: 30 * 24 * 60 * 60, // 30 days
  },
  secret: process.env.NEXTAUTH_SECRET,
};

async function refreshAccessToken(token: any) {
  try {
    const response = await fetch(`${process.env.API_URL}/api/auth/refresh`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        refreshToken: token.refreshToken,
      }),
    });

    if (!response.ok) {
      throw new Error("Failed to refresh token");
    }

    const data = await response.json();

    return {
      ...token,
      accessToken: data.accessToken,
      refreshToken: data.refreshToken ?? token.refreshToken,
      accessTokenExpires: data.expiresAt,
      error: undefined,
    };
  } catch {
    return {
      ...token,
      error: "RefreshAccessTokenError",
    };
  }
}
```

```typescript
// filepath: src/lib/auth/index.ts
import NextAuth from "next-auth";
import { authConfig } from "./config";

export const {
  handlers: { GET, POST },
  auth,
  signIn,
  signOut,
} = NextAuth(authConfig);
```

```typescript
// filepath: src/app/api/auth/[...nextauth]/route.ts
export { GET, POST } from "@/lib/auth";
```

---

## 🔧 PASO 3: Tipos de Sesión

```typescript
// filepath: src/types/next-auth.d.ts
import "next-auth";
import { DefaultSession } from "next-auth";

declare module "next-auth" {
  interface Session {
    user: {
      id: string;
      role: string;
    } & DefaultSession["user"];
    accessToken: string;
    error?: string;
  }

  interface User {
    id: string;
    role: string;
    accessToken: string;
    refreshToken: string;
    accessTokenExpires: number;
  }
}

declare module "next-auth/jwt" {
  interface JWT {
    id: string;
    role: string;
    accessToken: string;
    refreshToken: string;
    accessTokenExpires: number;
    error?: string;
  }
}
```

---

## 🔧 PASO 4: Middleware de Autenticación

```typescript
// filepath: src/middleware.ts
import { auth } from "@/lib/auth";
import { NextResponse } from "next/server";

export default auth((req) => {
  const { nextUrl } = req;
  const isLoggedIn = !!req.auth;

  // Protected routes
  const protectedPaths = [
    "/dashboard",
    "/publicar",
    "/favoritos",
    "/mensajes",
    "/configuracion",
  ];

  const isProtectedRoute = protectedPaths.some((path) =>
    nextUrl.pathname.startsWith(path),
  );

  // Redirect to login if not authenticated
  if (isProtectedRoute && !isLoggedIn) {
    const loginUrl = new URL("/login", nextUrl.origin);
    loginUrl.searchParams.set("callbackUrl", nextUrl.pathname);
    return NextResponse.redirect(loginUrl);
  }

  // Redirect to dashboard if already logged in on auth pages
  const authPaths = ["/login", "/registro"];
  const isAuthPage = authPaths.includes(nextUrl.pathname);

  if (isAuthPage && isLoggedIn) {
    return NextResponse.redirect(new URL("/dashboard", nextUrl.origin));
  }

  return NextResponse.next();
});

export const config = {
  matcher: [
    /*
     * Match all request paths except:
     * - _next/static (static files)
     * - _next/image (image optimization files)
     * - favicon.ico, robots.txt, sitemap.xml
     * - public folder
     * - api routes (except auth)
     */
    "/((?!_next/static|_next/image|favicon.ico|robots.txt|sitemap.xml|images|api/(?!auth)).*)",
  ],
};
```

---

## 🔧 PASO 5: Provider de Sesión

```typescript
// filepath: src/components/providers/SessionProvider.tsx
"use client";

import { SessionProvider as NextAuthSessionProvider } from "next-auth/react";
import * as React from "react";

interface SessionProviderProps {
  children: React.ReactNode;
}

export function SessionProvider({ children }: SessionProviderProps) {
  return (
    <NextAuthSessionProvider refetchOnWindowFocus={true} refetchInterval={5 * 60}>
      {children}
    </NextAuthSessionProvider>
  );
}
```

```typescript
// filepath: src/components/providers/index.tsx (actualizar)
"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools";
import { ThemeProvider } from "next-themes";
import * as React from "react";
import { SessionProvider } from "./SessionProvider";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 60 * 1000,
      gcTime: 5 * 60 * 1000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

export function Providers({ children }: { children: React.ReactNode }) {
  return (
    <SessionProvider>
      <QueryClientProvider client={queryClient}>
        <ThemeProvider
          attribute="class"
          defaultTheme="light"
          enableSystem={false}
          disableTransitionOnChange
        >
          {children}
        </ThemeProvider>
        <ReactQueryDevtools initialIsOpen={false} />
      </QueryClientProvider>
    </SessionProvider>
  );
}
```

---

## 🔧 PASO 6: Hook de Autenticación

```typescript
// filepath: src/lib/hooks/useAuth.ts
"use client";

import { useSession, signIn, signOut } from "next-auth/react";
import { useRouter } from "next/navigation";
import * as React from "react";

export function useAuth() {
  const { data: session, status, update } = useSession();
  const router = useRouter();

  const isLoading = status === "loading";
  const isAuthenticated = status === "authenticated";
  const user = session?.user;
  const accessToken = session?.accessToken;

  // Check if session has error (e.g., refresh token failed)
  const hasError = session?.error === "RefreshAccessTokenError";

  // Logout on error
  React.useEffect(() => {
    if (hasError) {
      signOut({ callbackUrl: "/login" });
    }
  }, [hasError]);

  const login = React.useCallback(async (email: string, password: string) => {
    const result = await signIn("credentials", {
      email,
      password,
      redirect: false,
    });

    if (result?.error) {
      throw new Error("Credenciales inválidas");
    }

    return result;
  }, []);

  const logout = React.useCallback(async () => {
    await signOut({ callbackUrl: "/" });
  }, []);

  const updateSession = React.useCallback(
    async (data: Partial<typeof session>) => {
      await update(data);
    },
    [update],
  );

  // Role checks
  const isAdmin = user?.role === "admin";
  const isDealer = user?.role === "dealer";
  const isBuyer = user?.role === "buyer" || user?.role === "individual";

  return {
    user,
    session,
    accessToken,
    isLoading,
    isAuthenticated,
    isAdmin,
    isDealer,
    isBuyer,
    login,
    logout,
    updateSession,
  };
}
```

---

## 🔧 PASO 7: Página de Login

```typescript
// filepath: src/app/(auth)/login/page.tsx
import { Metadata } from "next";
import Link from "next/link";
import { LoginForm } from "@/components/auth/LoginForm";

export const metadata: Metadata = {
  title: "Iniciar Sesión | OKLA",
  description: "Inicia sesión en tu cuenta de OKLA",
};

export default function LoginPage() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        {/* Logo */}
        <div className="text-center">
          <Link href="/" className="inline-block">
            <h1 className="text-3xl font-bold text-primary-600">OKLA</h1>
          </Link>
          <h2 className="mt-6 text-2xl font-bold text-gray-900">
            Iniciar Sesión
          </h2>
          <p className="mt-2 text-gray-600">
            ¿No tienes cuenta?{" "}
            <Link
              href="/registro"
              className="text-primary-600 hover:text-primary-700 font-medium"
            >
              Regístrate gratis
            </Link>
          </p>
        </div>

        {/* Form */}
        <LoginForm />

        {/* Forgot password */}
        <div className="text-center">
          <Link
            href="/recuperar-contrasena"
            className="text-sm text-gray-600 hover:text-gray-900"
          >
            ¿Olvidaste tu contraseña?
          </Link>
        </div>
      </div>
    </div>
  );
}
```

```typescript
// filepath: src/components/auth/LoginForm.tsx
"use client";

import * as React from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Eye, EyeOff, Mail, Lock } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { FormField } from "@/components/ui/FormField";
import { Alert, AlertDescription } from "@/components/ui/Alert";
import { useAuth } from "@/lib/hooks/useAuth";

const loginSchema = z.object({
  email: z.string().email("Email inválido"),
  password: z.string().min(6, "Mínimo 6 caracteres"),
});

type LoginFormData = z.infer<typeof loginSchema>;

export function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { login } = useAuth();

  const [showPassword, setShowPassword] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);

  const callbackUrl = searchParams.get("callbackUrl") || "/dashboard";

  const form = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: "",
      password: "",
    },
  });

  const onSubmit = async (data: LoginFormData) => {
    setError(null);

    try {
      await login(data.email, data.password);
      router.push(callbackUrl);
      router.refresh();
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Error al iniciar sesión. Intenta de nuevo."
      );
    }
  };

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      <FormField
        label="Email"
        error={form.formState.errors.email?.message}
      >
        <div className="relative">
          <Mail
            size={18}
            className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
          />
          <Input
            type="email"
            placeholder="tu@email.com"
            className="pl-10"
            {...form.register("email")}
          />
        </div>
      </FormField>

      <FormField
        label="Contraseña"
        error={form.formState.errors.password?.message}
      >
        <div className="relative">
          <Lock
            size={18}
            className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
          />
          <Input
            type={showPassword ? "text" : "password"}
            placeholder="••••••••"
            className="pl-10 pr-10"
            {...form.register("password")}
          />
          <button
            type="button"
            onClick={() => setShowPassword(!showPassword)}
            className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600"
          >
            {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
          </button>
        </div>
      </FormField>

      <Button
        type="submit"
        className="w-full"
        size="lg"
        isLoading={form.formState.isSubmitting}
      >
        Iniciar Sesión
      </Button>
    </form>
  );
}
```

---

## 🔧 PASO 8: Página de Registro

```typescript
// filepath: src/app/(auth)/registro/page.tsx
import { Metadata } from "next";
import Link from "next/link";
import { RegisterForm } from "@/components/auth/RegisterForm";

export const metadata: Metadata = {
  title: "Crear Cuenta | OKLA",
  description: "Crea tu cuenta en OKLA y comienza a buscar vehículos",
};

export default function RegisterPage() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        {/* Logo */}
        <div className="text-center">
          <Link href="/" className="inline-block">
            <h1 className="text-3xl font-bold text-primary-600">OKLA</h1>
          </Link>
          <h2 className="mt-6 text-2xl font-bold text-gray-900">
            Crear Cuenta
          </h2>
          <p className="mt-2 text-gray-600">
            ¿Ya tienes cuenta?{" "}
            <Link
              href="/login"
              className="text-primary-600 hover:text-primary-700 font-medium"
            >
              Iniciar sesión
            </Link>
          </p>
        </div>

        {/* Form */}
        <RegisterForm />

        {/* Terms */}
        <p className="text-xs text-center text-gray-500">
          Al crear una cuenta, aceptas nuestros{" "}
          <Link href="/terminos" className="text-primary-600 hover:underline">
            Términos de Servicio
          </Link>{" "}
          y{" "}
          <Link href="/privacidad" className="text-primary-600 hover:underline">
            Política de Privacidad
          </Link>
        </p>
      </div>
    </div>
  );
}
```

```typescript
// filepath: src/components/auth/RegisterForm.tsx
"use client";

import * as React from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Eye, EyeOff, Mail, Lock, User, Phone } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { FormField } from "@/components/ui/FormField";
import { Alert, AlertDescription } from "@/components/ui/Alert";
import { authService } from "@/lib/services/authService";
import { useAuth } from "@/lib/hooks/useAuth";
import { showToast } from "@/lib/toast";

const registerSchema = z
  .object({
    fullName: z.string().min(2, "Nombre requerido"),
    email: z.string().email("Email inválido"),
    phone: z.string().min(10, "Teléfono inválido"),
    password: z.string().min(6, "Mínimo 6 caracteres"),
    confirmPassword: z.string(),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: "Las contraseñas no coinciden",
    path: ["confirmPassword"],
  });

type RegisterFormData = z.infer<typeof registerSchema>;

export function RegisterForm() {
  const router = useRouter();
  const { login } = useAuth();

  const [showPassword, setShowPassword] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);

  const form = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      fullName: "",
      email: "",
      phone: "",
      password: "",
      confirmPassword: "",
    },
  });

  const onSubmit = async (data: RegisterFormData) => {
    setError(null);

    try {
      // Register user
      await authService.register({
        fullName: data.fullName,
        email: data.email,
        phone: data.phone,
        password: data.password,
      });

      showToast.success(
        "Cuenta creada",
        "Tu cuenta ha sido creada exitosamente"
      );

      // Auto login after registration
      await login(data.email, data.password);
      router.push("/dashboard");
      router.refresh();
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Error al crear cuenta. Intenta de nuevo."
      );
    }
  };

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-5">
      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      <FormField
        label="Nombre completo"
        error={form.formState.errors.fullName?.message}
      >
        <div className="relative">
          <User
            size={18}
            className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
          />
          <Input
            placeholder="Juan Pérez"
            className="pl-10"
            {...form.register("fullName")}
          />
        </div>
      </FormField>

      <FormField
        label="Email"
        error={form.formState.errors.email?.message}
      >
        <div className="relative">
          <Mail
            size={18}
            className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
          />
          <Input
            type="email"
            placeholder="tu@email.com"
            className="pl-10"
            {...form.register("email")}
          />
        </div>
      </FormField>

      <FormField
        label="Teléfono"
        error={form.formState.errors.phone?.message}
      >
        <div className="relative">
          <Phone
            size={18}
            className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
          />
          <Input
            type="tel"
            placeholder="809-555-1234"
            className="pl-10"
            {...form.register("phone")}
          />
        </div>
      </FormField>

      <FormField
        label="Contraseña"
        error={form.formState.errors.password?.message}
      >
        <div className="relative">
          <Lock
            size={18}
            className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
          />
          <Input
            type={showPassword ? "text" : "password"}
            placeholder="••••••••"
            className="pl-10 pr-10"
            {...form.register("password")}
          />
          <button
            type="button"
            onClick={() => setShowPassword(!showPassword)}
            className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600"
          >
            {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
          </button>
        </div>
      </FormField>

      <FormField
        label="Confirmar contraseña"
        error={form.formState.errors.confirmPassword?.message}
      >
        <div className="relative">
          <Lock
            size={18}
            className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
          />
          <Input
            type={showPassword ? "text" : "password"}
            placeholder="••••••••"
            className="pl-10"
            {...form.register("confirmPassword")}
          />
        </div>
      </FormField>

      <Button
        type="submit"
        className="w-full"
        size="lg"
        isLoading={form.formState.isSubmitting}
      >
        Crear Cuenta
      </Button>
    </form>
  );
}
```

---

## 🔧 PASO 9: Auth Service

```typescript
// filepath: src/lib/services/authService.ts
import { apiClient } from "@/lib/api/client";

interface RegisterRequest {
  fullName: string;
  email: string;
  phone: string;
  password: string;
}

interface LoginResponse {
  user: {
    id: string;
    email: string;
    fullName: string;
    avatar?: string;
    role: string;
  };
  accessToken: string;
  refreshToken: string;
  expiresAt: number;
}

export const authService = {
  async register(data: RegisterRequest): Promise<void> {
    await apiClient.post("/auth/register", data);
  },

  async login(email: string, password: string): Promise<LoginResponse> {
    const response = await apiClient.post<LoginResponse>("/auth/login", {
      email,
      password,
    });
    return response.data;
  },

  async refreshToken(refreshToken: string): Promise<LoginResponse> {
    const response = await apiClient.post<LoginResponse>("/auth/refresh", {
      refreshToken,
    });
    return response.data;
  },

  async forgotPassword(email: string): Promise<void> {
    await apiClient.post("/auth/forgot-password", { email });
  },

  async resetPassword(token: string, password: string): Promise<void> {
    await apiClient.post("/auth/reset-password", { token, password });
  },

  async changePassword(
    currentPassword: string,
    newPassword: string,
  ): Promise<void> {
    await apiClient.post("/auth/change-password", {
      currentPassword,
      newPassword,
    });
  },

  async verifyEmail(token: string): Promise<void> {
    await apiClient.post("/auth/verify-email", { token });
  },

  async resendVerification(email: string): Promise<void> {
    await apiClient.post("/auth/resend-verification", { email });
  },
};
```

---

## 🔧 PASO 10: Layout de Auth

```typescript
// filepath: src/app/(auth)/layout.tsx
import { redirect } from "next/navigation";
import { auth } from "@/lib/auth";

export default async function AuthLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await auth();

  // Redirect to dashboard if already logged in
  if (session?.user) {
    redirect("/dashboard");
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-50 to-gray-100">
      {children}
    </div>
  );
}
```

---

## ✅ VALIDACIÓN

### Variables de Entorno

```bash
# filepath: .env.local
NEXTAUTH_SECRET=tu-secreto-super-seguro-aqui-minimo-32-caracteres
NEXTAUTH_URL=http://localhost:3000
API_URL=http://localhost:18443
```

### Test de Login

```typescript
// filepath: __tests__/auth/login.test.tsx
import { describe, it, expect, vi } from "vitest";
import { screen, waitFor } from "@testing-library/react";
import { render } from "@tests/utils/test-utils";
import { LoginForm } from "@/components/auth/LoginForm";

describe("LoginForm", () => {
  it("renders email and password fields", () => {
    render(<LoginForm />);

    expect(screen.getByPlaceholderText(/email/i)).toBeInTheDocument();
    expect(screen.getByPlaceholderText(/••••••••/)).toBeInTheDocument();
  });

  it("shows validation errors", async () => {
    const { user } = render(<LoginForm />);

    await user.click(screen.getByRole("button", { name: /iniciar sesión/i }));

    await waitFor(() => {
      expect(screen.getByText(/email inválido/i)).toBeInTheDocument();
    });
  });
});
```

### Ejecutar

```bash
pnpm test auth

pnpm dev
# Verificar:
# - http://localhost:3000/login
# - http://localhost:3000/registro
# - Rutas protegidas redirigen a login
# - Login funciona y redirige
# - Sesión persiste al refrescar
```

---

## 📊 RESUMEN

| Archivo                            | Función                |
| ---------------------------------- | ---------------------- |
| `lib/auth/config.ts`               | Configuración NextAuth |
| `lib/auth/index.ts`                | Exports de NextAuth    |
| `middleware.ts`                    | Protección de rutas    |
| `hooks/useAuth.ts`                 | Hook de autenticación  |
| `providers/SessionProvider.tsx`    | Provider de sesión     |
| `app/(auth)/login/page.tsx`        | Página de login        |
| `app/(auth)/registro/page.tsx`     | Página de registro     |
| `components/auth/LoginForm.tsx`    | Formulario de login    |
| `components/auth/RegisterForm.tsx` | Formulario de registro |
| `services/authService.ts`          | API de autenticación   |

### Flujo de Autenticación

1. Usuario accede a ruta protegida
2. Middleware detecta no autenticado
3. Redirect a `/login?callbackUrl=...`
4. Usuario ingresa credenciales
5. NextAuth valida con API backend
6. Se genera JWT con access/refresh tokens
7. Usuario redirigido a callbackUrl
8. Token se refresca automáticamente

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/05-API-INTEGRATION/03-formularios.md`
