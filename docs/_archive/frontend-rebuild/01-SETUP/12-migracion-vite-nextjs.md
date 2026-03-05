# 🔄 Guía de Migración: Vite → Next.js

> **Tiempo estimado:** 2-3 sprints (4-6 semanas)
> **Propósito:** Migrar frontend actual de Vite/React a Next.js 14+ App Router
> **Última actualización:** Enero 31, 2026

---

## 📋 OBJETIVO

Migrar el frontend actual de OKLA de Vite a Next.js para obtener:

1. **Server-Side Rendering (SSR)** - SEO mejorado, mejor FCP
2. **App Router** - Layouts anidados, loading states nativos
3. **API Routes** - BFF (Backend for Frontend) cuando necesario
4. **Optimizaciones automáticas** - Imágenes, fuentes, scripts

---

## 📊 INVENTARIO ACTUAL (Vite)

### Estructura de Carpetas Actual

```
frontend/web/src/
├── App.tsx                    # Router principal (React Router v6)
├── main.tsx                   # Entry point
├── pages/                     # 27 páginas + 7 subcarpetas
│   ├── HomePage.tsx
│   ├── SearchPage.tsx
│   ├── admin/                 # Páginas de admin
│   ├── auth/                  # Login, Register, etc.
│   ├── billing/               # Checkout, invoices
│   ├── dealer/                # Portal dealer
│   ├── kyc/                   # Verificación identidad
│   ├── seller/                # Portal vendedor
│   ├── user/                  # Perfil usuario
│   └── vehicles/              # Detalle, listados
├── components/                # 24 subcarpetas de componentes
│   ├── atoms/
│   ├── molecules/
│   ├── organisms/
│   ├── dealer/
│   ├── marketplace/
│   └── ...
├── layouts/                   # MainLayout, DashboardLayout
├── hooks/                     # Custom hooks
├── services/                  # API services
├── store/                     # Zustand (authStore.ts)
├── lib/                       # Utilidades
├── i18n/                      # Internacionalización
└── types/                     # TypeScript types
```

### Dependencias Actuales a Migrar

| Dependencia Actual      | Equivalente Next.js | Acción     |
| ----------------------- | ------------------- | ---------- |
| `react-router-dom`      | App Router nativo   | Eliminar   |
| `vite`                  | Next.js             | Eliminar   |
| `@tanstack/react-query` | ✅ Mantener         | Compatible |
| `zustand`               | ✅ Mantener         | Compatible |
| `axios`                 | ✅ Mantener         | Compatible |
| `react-hook-form`       | ✅ Mantener         | Compatible |
| `zod`                   | ✅ Mantener         | Compatible |
| `framer-motion`         | ✅ Mantener         | Compatible |
| `tailwindcss`           | ✅ Mantener         | Compatible |
| `shadcn/ui`             | ✅ Mantener         | Compatible |

---

## 🗺️ MAPEO DE RUTAS

### React Router → Next.js App Router

| Ruta Actual (React Router) | Nueva Ruta (App Router)                     | Tipo      |
| -------------------------- | ------------------------------------------- | --------- |
| `/`                        | `app/page.tsx`                              | SSG       |
| `/search`                  | `app/search/page.tsx`                       | SSR       |
| `/vehicles/:slug`          | `app/vehicles/[slug]/page.tsx`              | SSR + ISR |
| `/auth/login`              | `app/(auth)/login/page.tsx`                 | CSR       |
| `/auth/register`           | `app/(auth)/register/page.tsx`              | CSR       |
| `/user/profile`            | `app/(dashboard)/user/profile/page.tsx`     | CSR       |
| `/user/favorites`          | `app/(dashboard)/user/favorites/page.tsx`   | SSR       |
| `/dealer/dashboard`        | `app/(dashboard)/dealer/page.tsx`           | CSR       |
| `/dealer/inventory`        | `app/(dashboard)/dealer/inventory/page.tsx` | SSR       |
| `/admin/*`                 | `app/(admin)/admin/**/page.tsx`             | CSR       |
| `/checkout`                | `app/(checkout)/checkout/page.tsx`          | CSR       |

### Route Groups Recomendados

```
app/
├── (public)/                  # Páginas públicas
│   ├── layout.tsx            # MainLayout
│   ├── page.tsx              # Homepage
│   ├── search/
│   └── vehicles/[slug]/
├── (auth)/                    # Sin layout principal
│   ├── layout.tsx            # AuthLayout (minimal)
│   ├── login/
│   └── register/
├── (dashboard)/               # Requiere autenticación
│   ├── layout.tsx            # DashboardLayout
│   ├── user/
│   └── dealer/
├── (admin)/                   # Solo admins
│   ├── layout.tsx            # AdminLayout
│   └── admin/
└── (checkout)/                # Flujo de pago
    ├── layout.tsx            # CheckoutLayout
    └── checkout/
```

---

## 📦 MIGRACIÓN POR FASE

### Fase 1: Setup Inicial (Sprint 1 - Semana 1-2)

**Objetivo:** Proyecto Next.js funcionando con estructura base

```bash
# 1. Crear proyecto Next.js
npx create-next-app@latest frontend-next --typescript --tailwind --eslint --app --src-dir

# 2. Configurar aliases
# next.config.ts
import type { NextConfig } from 'next'

const nextConfig: NextConfig = {
  experimental: {
    typedRoutes: true,
  },
  images: {
    remotePatterns: [
      { protocol: 'https', hostname: 'cdn.okla.com.do' },
      { protocol: 'https', hostname: 's3.amazonaws.com' },
    ],
  },
}

export default nextConfig
```

**Archivos a migrar primero:**

1. `tailwind.config.js` → `tailwind.config.ts`
2. `src/styles/globals.css` → `app/globals.css`
3. `src/lib/utils.ts` → `lib/utils.ts`
4. Copiar todos los componentes de `shadcn/ui`

### Fase 2: Componentes Base (Sprint 1 - Semana 2)

**Objetivo:** Migrar componentes sin dependencia de routing

#### Migrar en Orden:

1. **atoms/** (sin cambios)

   ```
   components/atoms/ → components/ui/
   ```

2. **molecules/** (sin cambios)

   ```
   components/molecules/ → components/
   ```

3. **organisms/** (algunos cambios)
   ```
   components/organisms/Navbar.tsx → components/layout/navbar.tsx
   components/organisms/Footer.tsx → components/layout/footer.tsx
   ```

#### Cambios Comunes en Componentes:

```tsx
// ANTES (Vite)
import { Link } from 'react-router-dom';
import logo from '@/assets/logo.png';

export function Navbar() {
  return (
    <Link to="/vehicles">Vehículos</Link>
    <img src={logo} alt="OKLA" />
  );
}

// DESPUÉS (Next.js)
import Link from 'next/link';
import Image from 'next/image';

export function Navbar() {
  return (
    <Link href="/vehicles">Vehículos</Link>
    <Image src="/logo.png" alt="OKLA" width={120} height={40} />
  );
}
```

### Fase 3: Layouts (Sprint 2 - Semana 1)

**Migrar layouts a Route Groups:**

```tsx
// app/(public)/layout.tsx
import { Navbar } from "@/components/layout/navbar";
import { Footer } from "@/components/layout/footer";

export default function PublicLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div className="min-h-screen flex flex-col">
      <Navbar />
      <main className="flex-1">{children}</main>
      <Footer />
    </div>
  );
}
```

```tsx
// app/(dashboard)/layout.tsx
import { redirect } from "next/navigation";
import { auth } from "@/lib/auth";
import { DashboardSidebar } from "@/components/layout/dashboard-sidebar";

export default async function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await auth();

  if (!session) {
    redirect("/login");
  }

  return (
    <div className="flex min-h-screen">
      <DashboardSidebar user={session.user} />
      <main className="flex-1 p-6">{children}</main>
    </div>
  );
}
```

### Fase 4: Páginas Públicas (Sprint 2 - Semana 1-2)

#### Homepage (SSG)

```tsx
// app/(public)/page.tsx
import { Suspense } from "react";
import { HeroSection } from "@/components/home/hero-section";
import { FeaturedVehicles } from "@/components/home/featured-vehicles";
import { FeaturedVehiclesSkeleton } from "@/components/skeletons";

export const metadata = {
  title: "OKLA - Marketplace de Vehículos en República Dominicana",
  description: "Encuentra tu próximo vehículo en OKLA...",
};

export default function HomePage() {
  return (
    <>
      <HeroSection />
      <Suspense fallback={<FeaturedVehiclesSkeleton />}>
        <FeaturedVehicles />
      </Suspense>
    </>
  );
}

// components/home/featured-vehicles.tsx
async function FeaturedVehicles() {
  const vehicles = await fetch(
    "https://api.okla.com.do/api/vehicles/featured",
    {
      next: { revalidate: 60 }, // ISR: revalidar cada 60 segundos
    },
  ).then((res) => res.json());

  return <VehicleGrid vehicles={vehicles.data} />;
}
```

#### Búsqueda (SSR con searchParams)

```tsx
// app/(public)/search/page.tsx
import { VehicleGrid } from "@/components/vehicles/vehicle-grid";
import { SearchFilters } from "@/components/search/search-filters";
import { Pagination } from "@/components/ui/pagination";

interface SearchPageProps {
  searchParams: Promise<{
    q?: string;
    make?: string;
    model?: string;
    minPrice?: string;
    maxPrice?: string;
    page?: string;
  }>;
}

export default async function SearchPage({ searchParams }: SearchPageProps) {
  const params = await searchParams;
  const page = parseInt(params.page || "1", 10);

  const response = await fetch(
    `https://api.okla.com.do/api/vehicles/search?${new URLSearchParams(params as Record<string, string>)}`,
    { cache: "no-store" }, // SSR dinámico
  );
  const data = await response.json();

  return (
    <div className="container mx-auto py-8">
      <div className="grid grid-cols-1 lg:grid-cols-4 gap-8">
        <aside className="lg:col-span-1">
          <SearchFilters initialFilters={params} />
        </aside>
        <main className="lg:col-span-3">
          <VehicleGrid vehicles={data.data} />
          <Pagination
            currentPage={page}
            totalPages={data.pagination.totalPages}
          />
        </main>
      </div>
    </div>
  );
}
```

#### Detalle Vehículo (SSR + ISR)

```tsx
// app/(public)/vehicles/[slug]/page.tsx
import { notFound } from "next/navigation";
import { VehicleGallery } from "@/components/vehicles/vehicle-gallery";
import { VehicleInfo } from "@/components/vehicles/vehicle-info";
import { ContactSeller } from "@/components/vehicles/contact-seller";

interface VehiclePageProps {
  params: Promise<{ slug: string }>;
}

// Generar metadata dinámico
export async function generateMetadata({ params }: VehiclePageProps) {
  const { slug } = await params;
  const vehicle = await getVehicle(slug);

  if (!vehicle) return { title: "Vehículo no encontrado" };

  return {
    title: `${vehicle.year} ${vehicle.make} ${vehicle.model} - OKLA`,
    description: vehicle.description,
    openGraph: {
      images: [vehicle.images[0]?.url],
    },
  };
}

// ISR: regenerar cada 5 minutos
export const revalidate = 300;

export default async function VehiclePage({ params }: VehiclePageProps) {
  const { slug } = await params;
  const vehicle = await getVehicle(slug);

  if (!vehicle) {
    notFound();
  }

  return (
    <div className="container mx-auto py-8">
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        <div className="lg:col-span-2">
          <VehicleGallery images={vehicle.images} />
          <VehicleInfo vehicle={vehicle} />
        </div>
        <aside>
          <ContactSeller seller={vehicle.seller} vehicleId={vehicle.id} />
        </aside>
      </div>
    </div>
  );
}

async function getVehicle(slug: string) {
  const res = await fetch(`https://api.okla.com.do/api/vehicles/${slug}`, {
    next: { revalidate: 300 },
  });

  if (!res.ok) return null;
  const data = await res.json();
  return data.data;
}
```

### Fase 5: Autenticación (Sprint 2 - Semana 2)

#### Setup NextAuth.js

```typescript
// lib/auth.ts
import NextAuth from "next-auth";
import CredentialsProvider from "next-auth/providers/credentials";

export const { handlers, auth, signIn, signOut } = NextAuth({
  providers: [
    CredentialsProvider({
      name: "Credentials",
      credentials: {
        email: { label: "Email", type: "email" },
        password: { label: "Password", type: "password" },
      },
      async authorize(credentials) {
        const res = await fetch("https://api.okla.com.do/api/auth/login", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(credentials),
        });

        if (!res.ok) return null;

        const data = await res.json();
        return {
          id: data.user.id,
          email: data.user.email,
          name: data.user.fullName,
          accessToken: data.accessToken,
          refreshToken: data.refreshToken,
        };
      },
    }),
  ],
  callbacks: {
    async jwt({ token, user }) {
      if (user) {
        token.accessToken = user.accessToken;
        token.refreshToken = user.refreshToken;
      }
      return token;
    },
    async session({ session, token }) {
      session.accessToken = token.accessToken;
      return session;
    },
  },
  pages: {
    signIn: "/login",
    error: "/login",
  },
});
```

#### Migrar Login Page

```tsx
// app/(auth)/login/page.tsx
"use client";

import { useState } from "react";
import { signIn } from "next-auth/react";
import { useRouter, useSearchParams } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";

const loginSchema = z.object({
  email: z.string().email("Email inválido"),
  password: z.string().min(8, "Mínimo 8 caracteres"),
});

type LoginForm = z.infer<typeof loginSchema>;

export default function LoginPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const callbackUrl = searchParams.get("callbackUrl") || "/";
  const [error, setError] = useState<string | null>(null);

  const form = useForm<LoginForm>({
    resolver: zodResolver(loginSchema),
  });

  const onSubmit = async (data: LoginForm) => {
    const result = await signIn("credentials", {
      email: data.email,
      password: data.password,
      redirect: false,
    });

    if (result?.error) {
      setError("Credenciales inválidas");
    } else {
      router.push(callbackUrl);
      router.refresh();
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center">
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4 w-96">
        {error && <Alert variant="destructive">{error}</Alert>}
        {/* Form fields */}
      </form>
    </div>
  );
}
```

### Fase 6: Dashboard Pages (Sprint 3)

**Patrón para páginas de dashboard con datos dinámicos:**

```tsx
// app/(dashboard)/dealer/inventory/page.tsx
import { auth } from "@/lib/auth";
import { redirect } from "next/navigation";
import { InventoryClient } from "./inventory-client";

export default async function InventoryPage() {
  const session = await auth();

  if (!session?.user?.dealerId) {
    redirect("/dealer/onboarding");
  }

  // Fetch inicial para hidratación
  const initialData = await fetch(
    `https://api.okla.com.do/api/dealers/${session.user.dealerId}/vehicles`,
    {
      headers: { Authorization: `Bearer ${session.accessToken}` },
      cache: "no-store",
    },
  ).then((res) => res.json());

  return (
    <InventoryClient
      dealerId={session.user.dealerId}
      initialData={initialData}
    />
  );
}

// app/(dashboard)/dealer/inventory/inventory-client.tsx
("use client");

import { useQuery } from "@tanstack/react-query";
import { DataTable } from "@/components/ui/data-table";
import { columns } from "./columns";

interface InventoryClientProps {
  dealerId: string;
  initialData: any;
}

export function InventoryClient({
  dealerId,
  initialData,
}: InventoryClientProps) {
  const { data } = useQuery({
    queryKey: ["dealer-inventory", dealerId],
    queryFn: () => fetchDealerInventory(dealerId),
    initialData,
  });

  return <DataTable columns={columns} data={data.vehicles} />;
}
```

---

## 🔄 HOOKS MIGRATION

### useNavigate → useRouter

```tsx
// ANTES (React Router)
import { useNavigate, useParams, useSearchParams } from "react-router-dom";

function Component() {
  const navigate = useNavigate();
  const { id } = useParams();
  const [searchParams, setSearchParams] = useSearchParams();

  const handleClick = () => {
    navigate("/vehicles");
    navigate(-1); // back
    navigate("/search?q=toyota", { replace: true });
  };
}

// DESPUÉS (Next.js)
("use client");

import { useRouter, useParams, useSearchParams } from "next/navigation";

function Component() {
  const router = useRouter();
  const params = useParams();
  const searchParams = useSearchParams();

  const handleClick = () => {
    router.push("/vehicles");
    router.back();
    router.replace("/search?q=toyota");
  };
}
```

### useLocation → usePathname

```tsx
// ANTES
import { useLocation } from "react-router-dom";
const { pathname, search } = useLocation();

// DESPUÉS
import { usePathname, useSearchParams } from "next/navigation";
const pathname = usePathname();
const searchParams = useSearchParams();
```

---

## 🔧 STATE MANAGEMENT

### Zustand (Sin cambios mayores)

El store de Zustand funciona igual, solo agregar hidratación SSR:

```tsx
// store/auth-store.ts
import { create } from "zustand";
import { persist, createJSONStorage } from "zustand/middleware";

interface AuthState {
  user: User | null;
  setUser: (user: User | null) => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      setUser: (user) => set({ user }),
    }),
    {
      name: "auth-storage",
      storage: createJSONStorage(() => localStorage),
      skipHydration: true, // ← Importante para SSR
    },
  ),
);

// Hidratación en layout
// app/providers.tsx
("use client");

import { useEffect } from "react";
import { useAuthStore } from "@/store/auth-store";

export function Providers({ children }: { children: React.ReactNode }) {
  useEffect(() => {
    useAuthStore.persist.rehydrate();
  }, []);

  return <>{children}</>;
}
```

---

## 📁 MAPEO COMPLETO DE ARCHIVOS

### Páginas Principales

| Archivo Actual                         | Archivo Nuevo                               |
| -------------------------------------- | ------------------------------------------- |
| `pages/HomePage.tsx`                   | `app/(public)/page.tsx`                     |
| `pages/SearchPage.tsx`                 | `app/(public)/search/page.tsx`              |
| `pages/vehicles/VehicleDetailPage.tsx` | `app/(public)/vehicles/[slug]/page.tsx`     |
| `pages/auth/LoginPage.tsx`             | `app/(auth)/login/page.tsx`                 |
| `pages/auth/RegisterPage.tsx`          | `app/(auth)/register/page.tsx`              |
| `pages/user/ProfilePage.tsx`           | `app/(dashboard)/user/profile/page.tsx`     |
| `pages/FavoritesPage.tsx`              | `app/(dashboard)/user/favorites/page.tsx`   |
| `pages/ComparisonPage.tsx`             | `app/(public)/compare/page.tsx`             |
| `pages/DealerDashboard.tsx`            | `app/(dashboard)/dealer/page.tsx`           |
| `pages/InventoryManagementPage.tsx`    | `app/(dashboard)/dealer/inventory/page.tsx` |
| `pages/admin/*`                        | `app/(admin)/admin/**/page.tsx`             |

### Componentes

| Carpeta Actual                    | Carpeta Nueva                           |
| --------------------------------- | --------------------------------------- |
| `components/atoms/`               | `components/ui/`                        |
| `components/molecules/`           | `components/`                           |
| `components/organisms/Navbar.tsx` | `components/layout/navbar.tsx`          |
| `components/organisms/Footer.tsx` | `components/layout/footer.tsx`          |
| `components/dealer/`              | `components/dealer/` (sin cambios)      |
| `components/marketplace/`         | `components/marketplace/` (sin cambios) |

### Servicios y Utilidades

| Archivo Actual               | Archivo Nuevo                     |
| ---------------------------- | --------------------------------- |
| `services/api.ts`            | `lib/api/client.ts`               |
| `services/vehicleService.ts` | `lib/api/vehicles.ts`             |
| `services/authService.ts`    | `lib/auth.ts` + `lib/api/auth.ts` |
| `hooks/useAuth.ts`           | `hooks/use-auth.ts`               |
| `store/authStore.ts`         | `store/auth-store.ts`             |
| `lib/utils.ts`               | `lib/utils.ts` (sin cambios)      |
| `types/*.ts`                 | `types/*.ts` (sin cambios)        |

---

## ✅ CHECKLIST DE MIGRACIÓN

### Setup (Semana 1)

- [ ] Crear proyecto Next.js 14+
- [ ] Configurar TypeScript strict
- [ ] Configurar Tailwind CSS
- [ ] Instalar shadcn/ui
- [ ] Configurar ESLint + Prettier
- [ ] Configurar path aliases

### Componentes Base (Semana 2)

- [ ] Migrar componentes UI (atoms/molecules)
- [ ] Migrar layouts
- [ ] Configurar Image y Link de Next.js
- [ ] Actualizar imports

### Páginas Públicas (Semana 3)

- [ ] Homepage (SSG)
- [ ] Búsqueda (SSR)
- [ ] Detalle vehículo (ISR)
- [ ] Páginas estáticas

### Autenticación (Semana 4)

- [ ] Configurar NextAuth.js
- [ ] Migrar Login/Register
- [ ] Middleware de protección
- [ ] Session management

### Dashboard (Semana 5-6)

- [ ] Dashboard usuario
- [ ] Dashboard dealer
- [ ] Dashboard admin
- [ ] Forms con Server Actions

### Optimización (Semana 6)

- [ ] Image optimization
- [ ] Bundle analysis
- [ ] Core Web Vitals
- [ ] Testing E2E

---

## 🚨 BREAKING CHANGES A CONSIDERAR

1. **`useEffect` para data fetching** → Server Components async
2. **`window`/`document`** → Solo en `'use client'` components
3. **React Router hooks** → Next.js navigation hooks
4. **Static imports de imágenes** → `next/image` con paths públicos
5. **Environment variables** → Prefijo `NEXT_PUBLIC_` para cliente

---

## 🔗 REFERENCIAS

- [Next.js App Router Docs](https://nextjs.org/docs/app)
- [Migration from Vite](https://nextjs.org/docs/app/building-your-application/upgrading/from-vite)
- [NextAuth.js v5](https://authjs.dev/getting-started/migrating-to-v5)
- [TanStack Query with Next.js](https://tanstack.com/query/latest/docs/react/guides/ssr)

---

_Última actualización: Enero 31, 2026_
