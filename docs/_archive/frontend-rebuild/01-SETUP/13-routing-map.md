# 🗺️ Mapa de Rutas Completo - OKLA Frontend

> **Tiempo estimado:** 10 minutos (referencia)
> **Prerrequisitos:** Entender la estructura de roles y layouts
> **Última actualización:** Enero 31, 2026

---

## 📋 OBJETIVO

Documento de referencia con TODAS las rutas de la aplicación, incluyendo:

- Path de la ruta
- Componente/página asociada
- Requisitos de autenticación
- Roles permitidos
- Layout utilizado
- Middleware aplicado

---

## 🎯 RESUMEN DE RUTAS

| Categoría             | Cantidad | Descripción                   |
| --------------------- | -------- | ----------------------------- |
| **Públicas**          | 15       | Sin autenticación requerida   |
| **Auth (Guest-only)** | 6        | Solo usuarios NO autenticados |
| **Comprador**         | 12       | Usuarios autenticados (buyer) |
| **Vendedor**          | 8        | Vendedores individuales       |
| **Dealer**            | 25       | Portal de dealers             |
| **Admin**             | 20       | Panel administrativo          |
| **API Routes**        | 10       | Endpoints internos Next.js    |
| **Total**             | ~96      | Rutas únicas                  |

---

## 🔧 CONFIGURACIÓN DE MIDDLEWARE

```typescript
// filepath: src/middleware.ts
import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";
import { getToken } from "next-auth/jwt";

// Rutas públicas (no requieren auth)
const publicRoutes = [
  "/",
  "/vehiculos",
  "/vehiculos/(.*)",
  "/buscar",
  "/dealers",
  "/dealers/(.*)",
  "/ayuda",
  "/contacto",
  "/about",
  "/terminos",
  "/privacidad",
];

// Rutas solo para invitados (redirigir si autenticado)
const guestOnlyRoutes = [
  "/login",
  "/registro",
  "/recuperar-password",
  "/verificar-email",
];

// Rutas protegidas por rol
const roleProtectedRoutes = {
  "/dealer": ["dealer", "admin"],
  "/admin": ["admin"],
  "/vender": ["seller", "dealer", "admin"],
};

export async function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const token = await getToken({ req: request });

  // Verificar rutas públicas
  const isPublicRoute = publicRoutes.some((route) =>
    new RegExp(`^${route}$`).test(pathname),
  );
  if (isPublicRoute) return NextResponse.next();

  // Verificar rutas guest-only
  const isGuestOnly = guestOnlyRoutes.some((route) =>
    pathname.startsWith(route),
  );
  if (isGuestOnly && token) {
    return NextResponse.redirect(new URL("/dashboard", request.url));
  }

  // Verificar autenticación
  if (!token) {
    const loginUrl = new URL("/login", request.url);
    loginUrl.searchParams.set("callbackUrl", pathname);
    return NextResponse.redirect(loginUrl);
  }

  // Verificar roles
  for (const [routePrefix, allowedRoles] of Object.entries(
    roleProtectedRoutes,
  )) {
    if (pathname.startsWith(routePrefix)) {
      const userRole = token.role as string;
      if (!allowedRoles.includes(userRole)) {
        return NextResponse.redirect(new URL("/403", request.url));
      }
    }
  }

  return NextResponse.next();
}

export const config = {
  matcher: ["/((?!api|_next/static|_next/image|favicon.ico|images|fonts).*)"],
};
```

---

## 🌐 RUTAS PÚBLICAS (Sin Auth)

| Ruta                     | Página              | Layout             | Descripción                           |
| ------------------------ | ------------------- | ------------------ | ------------------------------------- |
| `/`                      | `HomePage`          | `MainLayout`       | Página principal con hero, destacados |
| `/vehiculos`             | `VehicleListPage`   | `MainLayout`       | Listado de vehículos con filtros      |
| `/vehiculos/[slug]`      | `VehicleDetailPage` | `MainLayout`       | Detalle de vehículo                   |
| `/vehiculos/[slug]/360`  | `Vehicle360Page`    | `FullscreenLayout` | Visor 360°                            |
| `/buscar`                | `SearchPage`        | `MainLayout`       | Búsqueda avanzada                     |
| `/comparar`              | `ComparePage`       | `MainLayout`       | Comparador (hasta 3)                  |
| `/dealers`               | `DealerListPage`    | `MainLayout`       | Directorio de dealers                 |
| `/dealers/[slug]`        | `DealerProfilePage` | `MainLayout`       | Perfil público del dealer             |
| `/ayuda`                 | `HelpCenterPage`    | `MainLayout`       | Centro de ayuda/FAQ                   |
| `/ayuda/[category]`      | `HelpCategoryPage`  | `MainLayout`       | Artículos por categoría               |
| `/ayuda/articulo/[slug]` | `HelpArticlePage`   | `MainLayout`       | Artículo individual                   |
| `/contacto`              | `ContactPage`       | `MainLayout`       | Formulario de contacto                |
| `/about`                 | `AboutPage`         | `MainLayout`       | Acerca de OKLA                        |
| `/terminos`              | `TermsPage`         | `MainLayout`       | Términos y condiciones                |
| `/privacidad`            | `PrivacyPage`       | `MainLayout`       | Política de privacidad                |

### Implementación de Página Pública

```typescript
// filepath: src/app/(public)/vehiculos/page.tsx
import { Metadata } from "next";
import { VehicleList } from "@/components/vehicles/VehicleList";
import { VehicleFilters } from "@/components/vehicles/VehicleFilters";

export const metadata: Metadata = {
  title: "Vehículos en Venta | OKLA",
  description: "Encuentra el vehículo perfecto entre miles de opciones.",
};

export default function VehicleListPage() {
  return (
    <div className="container py-8">
      <h1 className="text-3xl font-bold mb-6">Vehículos en Venta</h1>
      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
        <aside className="lg:col-span-1">
          <VehicleFilters />
        </aside>
        <main className="lg:col-span-3">
          <VehicleList />
        </main>
      </div>
    </div>
  );
}
```

---

## 🔐 RUTAS DE AUTENTICACIÓN (Guest-Only)

| Ruta                  | Página               | Layout       | Descripción        |
| --------------------- | -------------------- | ------------ | ------------------ |
| `/login`              | `LoginPage`          | `AuthLayout` | Iniciar sesión     |
| `/registro`           | `RegisterPage`       | `AuthLayout` | Crear cuenta       |
| `/registro/dealer`    | `DealerRegisterPage` | `AuthLayout` | Registro de dealer |
| `/recuperar-password` | `ForgotPasswordPage` | `AuthLayout` | Solicitar reset    |
| `/reset-password`     | `ResetPasswordPage`  | `AuthLayout` | Cambiar password   |
| `/verificar-email`    | `VerifyEmailPage`    | `AuthLayout` | Verificar email    |

### Guard para Guest-Only

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

  // Redirigir si ya está autenticado
  if (session?.user) {
    redirect("/dashboard");
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="w-full max-w-md p-6">
        {children}
      </div>
    </div>
  );
}
```

---

## 👤 RUTAS DEL COMPRADOR (Auth Required)

| Ruta                         | Página              | Layout            | Roles | Descripción              |
| ---------------------------- | ------------------- | ----------------- | ----- | ------------------------ |
| `/dashboard`                 | `UserDashboard`     | `DashboardLayout` | `*`   | Dashboard principal      |
| `/perfil`                    | `ProfilePage`       | `DashboardLayout` | `*`   | Editar perfil            |
| `/perfil/seguridad`          | `SecurityPage`      | `DashboardLayout` | `*`   | Cambiar password, 2FA    |
| `/favoritos`                 | `FavoritesPage`     | `DashboardLayout` | `*`   | Vehículos guardados      |
| `/alertas`                   | `AlertsPage`        | `DashboardLayout` | `*`   | Alertas de precio        |
| `/busquedas-guardadas`       | `SavedSearchesPage` | `DashboardLayout` | `*`   | Búsquedas guardadas      |
| `/mensajes`                  | `MessagesPage`      | `DashboardLayout` | `*`   | Centro de mensajes       |
| `/mensajes/[conversationId]` | `ConversationPage`  | `DashboardLayout` | `*`   | Chat individual          |
| `/notificaciones`            | `NotificationsPage` | `DashboardLayout` | `*`   | Todas las notificaciones |
| `/mis-consultas`             | `MyInquiriesPage`   | `DashboardLayout` | `*`   | Consultas enviadas       |
| `/historial`                 | `HistoryPage`       | `DashboardLayout` | `*`   | Vehículos vistos         |
| `/configuracion`             | `SettingsPage`      | `DashboardLayout` | `*`   | Preferencias             |

### Dashboard Layout con Sidebar

```typescript
// filepath: src/app/(dashboard)/layout.tsx
import { auth } from "@/lib/auth";
import { redirect } from "next/navigation";
import { DashboardSidebar } from "@/components/dashboard/Sidebar";
import { DashboardHeader } from "@/components/dashboard/Header";

export default async function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await auth();

  if (!session?.user) {
    redirect("/login");
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <DashboardHeader user={session.user} />
      <div className="flex">
        <DashboardSidebar role={session.user.role} />
        <main className="flex-1 p-6">{children}</main>
      </div>
    </div>
  );
}
```

---

## 🚗 RUTAS DEL VENDEDOR (Role: seller, dealer, admin)

| Ruta                        | Página               | Layout            | Roles     | Descripción             |
| --------------------------- | -------------------- | ----------------- | --------- | ----------------------- |
| `/vender`                   | `SellLandingPage`    | `MainLayout`      | `*`       | Landing "Vende tu auto" |
| `/publicar`                 | `CreateListingPage`  | `DashboardLayout` | `seller+` | Formulario publicación  |
| `/publicar/fotos`           | `UploadPhotosPage`   | `DashboardLayout` | `seller+` | Subir fotos/360°        |
| `/publicar/preview`         | `ListingPreviewPage` | `DashboardLayout` | `seller+` | Vista previa            |
| `/mis-vehiculos`            | `MyVehiclesPage`     | `DashboardLayout` | `seller+` | Mis publicaciones       |
| `/mis-vehiculos/[id]`       | `EditVehiclePage`    | `DashboardLayout` | `seller+` | Editar publicación      |
| `/mis-vehiculos/[id]/stats` | `VehicleStatsPage`   | `DashboardLayout` | `seller+` | Estadísticas            |
| `/mis-vehiculos/[id]/boost` | `BoostVehiclePage`   | `DashboardLayout` | `seller+` | Promocionar             |

---

## 🏪 RUTAS DEL DEALER (Role: dealer, admin)

| Ruta                            | Página                    | Layout         | Descripción             |
| ------------------------------- | ------------------------- | -------------- | ----------------------- |
| `/dealer`                       | `DealerDashboard`         | `DealerLayout` | Dashboard principal     |
| `/dealer/inventario`            | `InventoryPage`           | `DealerLayout` | Gestión de inventario   |
| `/dealer/inventario/nuevo`      | `NewVehiclePage`          | `DealerLayout` | Agregar vehículo        |
| `/dealer/inventario/[id]`       | `EditVehiclePage`         | `DealerLayout` | Editar vehículo         |
| `/dealer/inventario/importar`   | `ImportCSVPage`           | `DealerLayout` | Importar CSV/Excel      |
| `/dealer/leads`                 | `LeadsPage`               | `DealerLayout` | CRM de leads            |
| `/dealer/leads/[id]`            | `LeadDetailPage`          | `DealerLayout` | Detalle de lead         |
| `/dealer/analytics`             | `AnalyticsPage`           | `DealerLayout` | Reportes y métricas     |
| `/dealer/analytics/inventario`  | `InventoryAnalyticsPage`  | `DealerLayout` | Analytics de inventario |
| `/dealer/analytics/ventas`      | `SalesAnalyticsPage`      | `DealerLayout` | Analytics de ventas     |
| `/dealer/citas`                 | `AppointmentsPage`        | `DealerLayout` | Gestión de citas        |
| `/dealer/citas/calendario`      | `CalendarPage`            | `DealerLayout` | Vista calendario        |
| `/dealer/mensajes`              | `DealerMessagesPage`      | `DealerLayout` | Centro de mensajes      |
| `/dealer/empleados`             | `EmployeesPage`           | `DealerLayout` | Gestión de staff        |
| `/dealer/empleados/nuevo`       | `NewEmployeePage`         | `DealerLayout` | Agregar empleado        |
| `/dealer/ubicaciones`           | `LocationsPage`           | `DealerLayout` | Sucursales              |
| `/dealer/ubicaciones/nueva`     | `NewLocationPage`         | `DealerLayout` | Nueva sucursal          |
| `/dealer/perfil`                | `DealerProfilePage`       | `DealerLayout` | Editar perfil dealer    |
| `/dealer/documentos`            | `DocumentsPage`           | `DealerLayout` | Documentos/KYC          |
| `/dealer/facturacion`           | `BillingPage`             | `DealerLayout` | Facturación y pagos     |
| `/dealer/facturacion/historial` | `BillingHistoryPage`      | `DealerLayout` | Historial de pagos      |
| `/dealer/suscripcion`           | `SubscriptionPage`        | `DealerLayout` | Plan y upgrade          |
| `/dealer/pricing`               | `PricingIntelligencePage` | `DealerLayout` | IA de pricing           |
| `/dealer/reportes`              | `ReportsPage`             | `DealerLayout` | Reportes descargables   |
| `/dealer/configuracion`         | `DealerSettingsPage`      | `DealerLayout` | Configuración           |

### Dealer Layout con Navegación

```typescript
// filepath: src/app/(dealer)/layout.tsx
import { auth } from "@/lib/auth";
import { redirect } from "next/navigation";
import { DealerSidebar } from "@/components/dealer/Sidebar";
import { DealerHeader } from "@/components/dealer/Header";

export default async function DealerLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await auth();

  if (!session?.user) {
    redirect("/login");
  }

  if (!["dealer", "admin"].includes(session.user.role)) {
    redirect("/403");
  }

  return (
    <div className="min-h-screen bg-gray-100">
      <DealerHeader />
      <div className="flex">
        <DealerSidebar />
        <main className="flex-1 p-6 lg:p-8">{children}</main>
      </div>
    </div>
  );
}
```

---

## 🔧 RUTAS DE ADMIN (Role: admin)

| Ruta                          | Página                  | Layout        | Descripción              |
| ----------------------------- | ----------------------- | ------------- | ------------------------ |
| `/admin`                      | `AdminDashboard`        | `AdminLayout` | Dashboard principal      |
| `/admin/usuarios`             | `UsersManagementPage`   | `AdminLayout` | Gestión de usuarios      |
| `/admin/usuarios/[id]`        | `UserDetailPage`        | `AdminLayout` | Detalle de usuario       |
| `/admin/dealers`              | `DealersManagementPage` | `AdminLayout` | Gestión de dealers       |
| `/admin/dealers/[id]`         | `DealerDetailPage`      | `AdminLayout` | Detalle de dealer        |
| `/admin/vehiculos`            | `VehiclesModeration`    | `AdminLayout` | Moderación vehículos     |
| `/admin/vehiculos/pendientes` | `PendingListingsPage`   | `AdminLayout` | Publicaciones pendientes |
| `/admin/reviews`              | `ReviewsModerationPage` | `AdminLayout` | Moderación de reviews    |
| `/admin/reportes`             | `ReportsPage`           | `AdminLayout` | Contenido reportado      |
| `/admin/kyc`                  | `KYCQueuePage`          | `AdminLayout` | Cola de verificación     |
| `/admin/kyc/[id]`             | `KYCReviewPage`         | `AdminLayout` | Revisar KYC              |
| `/admin/compliance`           | `CompliancePage`        | `AdminLayout` | AML/DGII                 |
| `/admin/soporte`              | `SupportPage`           | `AdminLayout` | Tickets de soporte       |
| `/admin/soporte/[id]`         | `TicketDetailPage`      | `AdminLayout` | Detalle de ticket        |
| `/admin/analytics`            | `PlatformAnalyticsPage` | `AdminLayout` | Analytics plataforma     |
| `/admin/sistema`              | `SystemPage`            | `AdminLayout` | Estado del sistema       |
| `/admin/mantenimiento`        | `MaintenancePage`       | `AdminLayout` | Modo mantenimiento       |
| `/admin/roles`                | `RolesPermissionsPage`  | `AdminLayout` | RBAC                     |
| `/admin/logs`                 | `AuditLogsPage`         | `AdminLayout` | Logs de auditoría        |
| `/admin/configuracion`        | `AdminSettingsPage`     | `AdminLayout` | Configuración global     |

---

## 💳 RUTAS DE PAGOS

| Ruta                  | Página                | Layout          | Auth | Descripción     |
| --------------------- | --------------------- | --------------- | ---- | --------------- |
| `/checkout`           | `CheckoutPage`        | `MinimalLayout` | ✅   | Proceso de pago |
| `/checkout/exito`     | `CheckoutSuccessPage` | `MinimalLayout` | ✅   | Confirmación    |
| `/checkout/cancelado` | `CheckoutCancelPage`  | `MinimalLayout` | ✅   | Pago cancelado  |
| `/checkout/error`     | `CheckoutErrorPage`   | `MinimalLayout` | ✅   | Error en pago   |

---

## 🔗 RUTAS API (Next.js API Routes)

| Ruta                      | Método | Descripción               |
| ------------------------- | ------ | ------------------------- |
| `/api/auth/[...nextauth]` | `*`    | NextAuth endpoints        |
| `/api/upload`             | `POST` | Proxy para subir imágenes |
| `/api/revalidate`         | `POST` | ISR revalidation          |
| `/api/og`                 | `GET`  | Open Graph images         |
| `/api/sitemap`            | `GET`  | Sitemap dinámico          |
| `/api/health`             | `GET`  | Health check              |
| `/api/webhook/stripe`     | `POST` | Webhook Stripe            |
| `/api/webhook/azul`       | `POST` | Webhook AZUL              |

---

## 📱 ESTRUCTURA DE CARPETAS (App Router)

```
src/app/
├── (public)/                    # Rutas públicas
│   ├── page.tsx                 # /
│   ├── vehiculos/
│   │   ├── page.tsx             # /vehiculos
│   │   └── [slug]/
│   │       ├── page.tsx         # /vehiculos/[slug]
│   │       └── 360/page.tsx     # /vehiculos/[slug]/360
│   ├── buscar/page.tsx          # /buscar
│   ├── comparar/page.tsx        # /comparar
│   ├── dealers/
│   │   ├── page.tsx             # /dealers
│   │   └── [slug]/page.tsx      # /dealers/[slug]
│   ├── ayuda/
│   │   ├── page.tsx             # /ayuda
│   │   ├── [category]/page.tsx
│   │   └── articulo/[slug]/page.tsx
│   ├── contacto/page.tsx
│   ├── about/page.tsx
│   ├── terminos/page.tsx
│   └── privacidad/page.tsx
│
├── (auth)/                      # Rutas de autenticación
│   ├── layout.tsx               # AuthLayout (guest-only)
│   ├── login/page.tsx
│   ├── registro/
│   │   ├── page.tsx
│   │   └── dealer/page.tsx
│   ├── recuperar-password/page.tsx
│   ├── reset-password/page.tsx
│   └── verificar-email/page.tsx
│
├── (dashboard)/                 # Rutas de usuario autenticado
│   ├── layout.tsx               # DashboardLayout
│   ├── dashboard/page.tsx
│   ├── perfil/
│   │   ├── page.tsx
│   │   └── seguridad/page.tsx
│   ├── favoritos/page.tsx
│   ├── alertas/page.tsx
│   ├── busquedas-guardadas/page.tsx
│   ├── mensajes/
│   │   ├── page.tsx
│   │   └── [conversationId]/page.tsx
│   ├── notificaciones/page.tsx
│   ├── mis-consultas/page.tsx
│   ├── historial/page.tsx
│   └── configuracion/page.tsx
│
├── (seller)/                    # Rutas de vendedor
│   ├── layout.tsx
│   ├── vender/page.tsx
│   ├── publicar/
│   │   ├── page.tsx
│   │   ├── fotos/page.tsx
│   │   └── preview/page.tsx
│   └── mis-vehiculos/
│       ├── page.tsx
│       └── [id]/
│           ├── page.tsx
│           ├── stats/page.tsx
│           └── boost/page.tsx
│
├── (dealer)/                    # Portal dealer
│   ├── layout.tsx               # DealerLayout
│   └── dealer/
│       ├── page.tsx             # Dashboard
│       ├── inventario/
│       ├── leads/
│       ├── analytics/
│       ├── citas/
│       ├── empleados/
│       ├── ubicaciones/
│       ├── perfil/
│       ├── documentos/
│       ├── facturacion/
│       ├── suscripcion/
│       ├── pricing/
│       ├── reportes/
│       └── configuracion/
│
├── (admin)/                     # Panel admin
│   ├── layout.tsx               # AdminLayout
│   └── admin/
│       ├── page.tsx
│       ├── usuarios/
│       ├── dealers/
│       ├── vehiculos/
│       ├── reviews/
│       ├── reportes/
│       ├── kyc/
│       ├── compliance/
│       ├── soporte/
│       ├── analytics/
│       ├── sistema/
│       ├── mantenimiento/
│       ├── roles/
│       ├── logs/
│       └── configuracion/
│
├── (checkout)/                  # Flujo de pago
│   ├── layout.tsx               # MinimalLayout
│   └── checkout/
│       ├── page.tsx
│       ├── exito/page.tsx
│       ├── cancelado/page.tsx
│       └── error/page.tsx
│
├── api/                         # API Routes
│   ├── auth/[...nextauth]/route.ts
│   ├── upload/route.ts
│   ├── revalidate/route.ts
│   ├── og/route.ts
│   ├── sitemap/route.ts
│   ├── health/route.ts
│   └── webhook/
│       ├── stripe/route.ts
│       └── azul/route.ts
│
├── 403/page.tsx                 # Forbidden
├── 404/page.tsx                 # Not Found
├── 500/page.tsx                 # Server Error
├── error.tsx                    # Error Boundary
├── loading.tsx                  # Loading global
├── not-found.tsx                # Not Found global
├── layout.tsx                   # Root Layout
└── globals.css                  # Estilos globales
```

---

## ✅ CHECKLIST DE IMPLEMENTACIÓN

### Rutas Públicas

- [ ] Homepage con secciones dinámicas
- [ ] Listado de vehículos con filtros
- [ ] Detalle de vehículo con galería
- [ ] Visor 360°
- [ ] Búsqueda avanzada
- [ ] Comparador
- [ ] Directorio de dealers
- [ ] Centro de ayuda
- [ ] Páginas estáticas (about, terms, privacy)

### Rutas de Auth

- [ ] Login con email/password
- [ ] Login con OAuth (Google, Facebook)
- [ ] Registro de usuarios
- [ ] Registro de dealers
- [ ] Recuperación de password
- [ ] Verificación de email

### Rutas de Usuario

- [ ] Dashboard con resumen
- [ ] Perfil editable
- [ ] Configuración de seguridad
- [ ] Favoritos
- [ ] Alertas de precio
- [ ] Búsquedas guardadas
- [ ] Centro de mensajes
- [ ] Notificaciones

### Rutas de Vendedor

- [ ] Landing "Vende tu auto"
- [ ] Formulario de publicación (wizard)
- [ ] Subida de fotos/360°
- [ ] Vista previa
- [ ] Gestión de publicaciones
- [ ] Estadísticas por vehículo
- [ ] Promociones/boost

### Rutas de Dealer

- [ ] Dashboard con KPIs
- [ ] Gestión de inventario
- [ ] Importación CSV
- [ ] CRM de leads
- [ ] Analytics y reportes
- [ ] Gestión de citas
- [ ] Empleados y ubicaciones
- [ ] Facturación y suscripción
- [ ] Pricing intelligence

### Rutas de Admin

- [ ] Dashboard ejecutivo
- [ ] Gestión de usuarios
- [ ] Gestión de dealers
- [ ] Moderación de contenido
- [ ] Cola de KYC
- [ ] Compliance/DGII
- [ ] Soporte/tickets
- [ ] Configuración del sistema

---

## 🔒 NOTAS DE SEGURIDAD

1. **Todas las rutas protegidas** verifican sesión en el layout correspondiente
2. **Middleware** intercepta ANTES de renderizar para redirigir rápido
3. **Roles** se verifican tanto en middleware como en API
4. **Tokens JWT** expiran en 15 minutos, refresh automático
5. **CSRF** protegido por NextAuth
6. **Rate limiting** en API routes críticas

---

## 📚 REFERENCIAS

- [Next.js App Router](https://nextjs.org/docs/app)
- [NextAuth.js](https://authjs.dev/)
- [Middleware](https://nextjs.org/docs/app/building-your-application/routing/middleware)
