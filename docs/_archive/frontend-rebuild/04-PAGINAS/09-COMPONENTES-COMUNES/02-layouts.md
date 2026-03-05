---
title: "68 - Layouts"
priority: P0
estimated_time: ""
dependencies: []
apis: []
status: complete
last_updated: "2026-01-30"
---

# 📋 68 - Layouts

**Objetivo:** Layouts principales de la aplicación: MainLayout, AuthLayout, AdminLayout, DealerLayout, OklaLayout.

**Prioridad:** P0 (Crítica)  
**Complejidad:** 🟡 Media  
**Dependencias:** React Router, Navbar, Footer, Sidebar components

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura](#-arquitectura)
2. [MainLayout](#-mainlayout)
3. [AuthLayout](#-authlayout)
4. [AdminLayout](#-adminlayout)
5. [DealerLayout](#-dealerlayout)
6. [DealerPortalLayout](#-dealerportallayout)
7. [OklaLayout](#-oklalayout)
8. [Validación](#-validación)

---

## 🏗️ ARQUITECTURA

```
layouts/
├── MainLayout.tsx              # Layout público con Navbar + Footer (25 líneas)
├── AuthLayout.tsx              # Layout de login/registro (65 líneas)
├── AdminLayout.tsx             # Layout admin con sidebar RBAC (~250 líneas)
├── DealerLayout.tsx            # Layout dealer básico (154 líneas)
├── DealerPortalLayout.tsx      # Layout dealer avanzado (558 líneas)
└── OklaLayout.tsx              # Layout premium OKLA (101 líneas)
```

---

## 🌐 MAINLAYOUT

**Uso:** Páginas públicas (Home, Search, Vehicle Details, etc.)

```typescript
// src/layouts/MainLayout.tsx
import type { ReactNode } from 'react';
import Navbar from '@/components/organisms/Navbar';
import Footer from '@/components/organisms/Footer';
import { MaintenanceBanner } from '@/components/marketplace/MaintenanceBanner';
import { EarlyBirdBanner } from '@/components/marketplace/EarlyBirdBanner';

interface MainLayoutProps {
  children: ReactNode;
}

/**
 * MainLayout - Layout principal para páginas públicas
 * Incluye Navbar + banners + contenido + Footer
 */
export default function MainLayout({ children }: MainLayoutProps) {
  return (
    <div className="min-h-screen flex flex-col">
      <MaintenanceBanner />
      <EarlyBirdBanner />
      <Navbar />
      <main className="flex-1">{children}</main>
      <Footer />
    </div>
  );
}
```

### Estructura Visual

```
┌──────────────────────────────────────────┐
│ MaintenanceBanner (condicional)          │
├──────────────────────────────────────────┤
│ EarlyBirdBanner (condicional)            │
├──────────────────────────────────────────┤
│ Navbar                                   │
├──────────────────────────────────────────┤
│                                          │
│              {children}                  │
│              (flex-1)                    │
│                                          │
├──────────────────────────────────────────┤
│ Footer                                   │
└──────────────────────────────────────────┘
```

---

## 🔐 AUTHLAYOUT

**Uso:** Login, Register, Forgot Password, Reset Password

```typescript
// src/layouts/AuthLayout.tsx
import type { ReactNode } from 'react';

interface AuthLayoutProps {
  children: ReactNode;
}

/**
 * AuthLayout - Layout minimalista para páginas de autenticación
 * Sin navbar/footer, centrado, con imagen lateral en desktop
 */
export default function AuthLayout({ children }: AuthLayoutProps) {
  return (
    <div className="min-h-screen flex">
      {/* Left side - Form */}
      <div className="flex-1 flex items-center justify-center px-4 sm:px-6 lg:px-8 bg-gray-50">
        <div className="w-full max-w-md">
          {children}
        </div>
      </div>

      {/* Right side - Hero/Branding (hidden on mobile) */}
      <div className="hidden lg:flex lg:flex-1 bg-gradient-to-br from-primary to-secondary relative overflow-hidden">
        <div className="absolute inset-0 bg-black opacity-20" />
        <div className="relative z-10 flex flex-col items-center justify-center text-white p-12">
          <h1 className="text-5xl font-bold font-heading mb-4">OKLA</h1>
          <p className="text-xl text-center mb-8">
            Encuentra tu vehículo ideal o vende con confianza
          </p>

          <div className="space-y-4 text-center">
            {/* Feature: Verified Listings */}
            <div className="flex items-center gap-3">
              <div className="w-12 h-12 bg-white/20 rounded-full flex items-center justify-center">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                </svg>
              </div>
              <span className="text-lg">Miles de listings verificados</span>
            </div>

            {/* Feature: Secure */}
            <div className="flex items-center gap-3">
              <div className="w-12 h-12 bg-white/20 rounded-full flex items-center justify-center">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                </svg>
              </div>
              <span className="text-lg">Transacciones seguras</span>
            </div>

            {/* Feature: Easy */}
            <div className="flex items-center gap-3">
              <div className="w-12 h-12 bg-white/20 rounded-full flex items-center justify-center">
                <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
                </svg>
              </div>
              <span className="text-lg">Proceso rápido y fácil</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
```

### Estructura Visual

```
┌───────────────────────────────────────────────────────────────────────┐
│                                                                       │
│  ┌─────────────────────────────┐   ┌─────────────────────────────┐   │
│  │                             │   │                             │   │
│  │                             │   │      BRANDING HERO          │   │
│  │       {children}            │   │      (hidden on mobile)     │   │
│  │       (Login Form)          │   │      - Logo                 │   │
│  │       max-w-md              │   │      - Tagline              │   │
│  │                             │   │      - Features list        │   │
│  │                             │   │                             │   │
│  └─────────────────────────────┘   └─────────────────────────────┘   │
│                                                                       │
└───────────────────────────────────────────────────────────────────────┘
```

---

## 🛠️ ADMINLAYOUT

**Uso:** Panel de administración (documentado en 12-admin-dashboard.md)

```typescript
// src/layouts/AdminLayout.tsx
import { useState, useEffect } from 'react';
import { useNavigate, Outlet } from 'react-router-dom';
import { useAuthStore } from '@/store/authStore';
import AdminSidebar from '@/components/admin/AdminSidebar';
import AdminTopbar from '@/components/admin/AdminTopbar';

interface AdminLayoutProps {
  children?: React.ReactNode;
}

export default function AdminLayout({ children }: AdminLayoutProps) {
  const navigate = useNavigate();
  const { user, isAuthenticated, hasAdminAccess } = useAuthStore();
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [mobileSidebarOpen, setMobileSidebarOpen] = useState(false);

  // Redirect if not admin
  useEffect(() => {
    if (!isAuthenticated || !hasAdminAccess()) {
      navigate('/login?redirect=/admin');
    }
  }, [isAuthenticated, hasAdminAccess, navigate]);

  const toggleSidebar = () => setSidebarOpen(!sidebarOpen);
  const toggleMobileSidebar = () => setMobileSidebarOpen(!mobileSidebarOpen);

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Sidebar (desktop) */}
      <AdminSidebar
        isOpen={sidebarOpen}
        onClose={() => setMobileSidebarOpen(false)}
        isMobile={false}
      />

      {/* Mobile Sidebar Overlay */}
      {mobileSidebarOpen && (
        <>
          <div
            className="fixed inset-0 bg-black/50 z-40 lg:hidden"
            onClick={() => setMobileSidebarOpen(false)}
          />
          <AdminSidebar
            isOpen={true}
            onClose={() => setMobileSidebarOpen(false)}
            isMobile={true}
          />
        </>
      )}

      {/* Main Content */}
      <div className={`transition-all duration-300 ${sidebarOpen ? 'lg:ml-64' : 'lg:ml-20'}`}>
        <AdminTopbar
          onMenuClick={toggleMobileSidebar}
          onSidebarToggle={toggleSidebar}
          sidebarOpen={sidebarOpen}
        />

        <main className="p-6">
          {children || <Outlet />}
        </main>
      </div>
    </div>
  );
}
```

### Estructura Visual

```
┌────────────────────────────────────────────────────────────────────────┐
│                                                                        │
│  ┌──────────────┐   ┌─────────────────────────────────────────────┐   │
│  │              │   │ AdminTopbar                                 │   │
│  │              │   ├─────────────────────────────────────────────┤   │
│  │  AdminSidebar│   │                                             │   │
│  │  (collapsible)   │                                             │   │
│  │  - Dashboard │   │           {children}                        │   │
│  │  - Users     │   │           (main content)                    │   │
│  │  - Listings  │   │           p-6                               │   │
│  │  - Reports   │   │                                             │   │
│  │  - Settings  │   │                                             │   │
│  │              │   │                                             │   │
│  │              │   │                                             │   │
│  └──────────────┘   └─────────────────────────────────────────────┘   │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 🏢 DEALERLAYOUT

**Uso:** Panel básico de dealers

```typescript
// src/layouts/DealerLayout.tsx
import type { ReactNode } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { LogOut, Crown, TrendingUp } from 'lucide-react';
import { usePermissions } from '@/hooks/usePermissions';
import { useAuthStore } from '@/store/authStore';
import { DealerSidebar } from '@/components/navigation';

interface DealerLayoutProps {
  children: ReactNode;
}

const DealerLayout = ({ children }: DealerLayoutProps) => {
  const navigate = useNavigate();
  const logout = useAuthStore((state) => state.logout);
  const {
    user,
    dealerPlan,
    usage,
    limits,
    getUsagePercentage,
    hasReachedLimit
  } = usePermissions();

  const handleLogout = () => {
    logout();
    navigate('/');
  };

  const getPlanBadgeColor = () => {
    switch (dealerPlan) {
      case 'enterprise': return 'bg-purple-100 text-purple-700';
      case 'pro': return 'bg-blue-100 text-blue-700';
      case 'basic': return 'bg-green-100 text-green-700';
      default: return 'bg-gray-100 text-gray-700';
    }
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Top Bar */}
      <div className="bg-white border-b border-gray-200">
        <div className="flex items-center justify-between px-6 py-4">
          <div className="flex items-center gap-4">
            <Link to="/" className="flex items-center gap-2">
              <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center">
                <span className="text-white font-bold text-xl">O</span>
              </div>
              <span className="text-xl font-bold text-gray-900">OKLA</span>
            </Link>
            <div className="h-6 w-px bg-gray-300" />
            <span className="text-sm text-gray-600 font-medium">Panel de Dealer</span>
          </div>

          <div className="flex items-center gap-4">
            {/* Plan Badge */}
            <div className={`flex items-center gap-1.5 px-3 py-1.5 rounded-full ${getPlanBadgeColor()}`}>
              <Crown className="h-4 w-4" />
              <span className="text-sm font-medium capitalize">{dealerPlan || 'Free'}</span>
            </div>

            <span className="text-sm text-gray-600">{user?.name}</span>

            <button
              onClick={handleLogout}
              className="flex items-center gap-2 px-3 py-2 text-sm text-gray-700 hover:bg-gray-100 rounded-lg transition-colors"
            >
              <LogOut className="w-4 h-4" />
              Salir
            </button>
          </div>
        </div>
      </div>

      <div className="flex">
        {/* Sidebar */}
        <aside className="w-72 bg-white border-r border-gray-200 min-h-[calc(100vh-73px)] flex flex-col">
          <div className="p-4 flex-1 overflow-hidden">
            <DealerSidebar />
          </div>

          {/* Usage Stats */}
          <div className="p-4 border-t border-gray-100">
            <div className="space-y-3">
              {/* Listings Usage */}
              <div>
                <div className="flex justify-between text-xs text-gray-600 mb-1">
                  <span>Publicaciones</span>
                  <span>{usage.listings}/{limits.maxListings === Infinity ? '∞' : limits.maxListings}</span>
                </div>
                <div className="w-full h-1.5 bg-gray-100 rounded-full overflow-hidden">
                  <div
                    className={`h-full rounded-full ${
                      hasReachedLimit('listings') ? 'bg-red-500' :
                      getUsagePercentage('listings') >= 80 ? 'bg-amber-500' : 'bg-blue-500'
                    }`}
                    style={{
                      width: limits.maxListings === Infinity
                        ? '20%'
                        : `${getUsagePercentage('listings')}%`
                    }}
                  />
                </div>
              </div>

              {/* Featured Usage */}
              <div>
                <div className="flex justify-between text-xs text-gray-600 mb-1">
                  <span>Destacados</span>
                  <span>{usage.featuredListings}/{limits.maxFeaturedListings}</span>
                </div>
                <div className="w-full h-1.5 bg-gray-100 rounded-full overflow-hidden">
                  <div
                    className={`h-full rounded-full ${
                      hasReachedLimit('featured') ? 'bg-red-500' :
                      getUsagePercentage('featured') >= 80 ? 'bg-amber-500' : 'bg-blue-500'
                    }`}
                    style={{ width: `${getUsagePercentage('featured')}%` }}
                  />
                </div>
              </div>
            </div>
          </div>

          {/* Upgrade CTA */}
          {(dealerPlan === 'free' || dealerPlan === 'basic') && (
            <div className="p-4">
              <Link
                to="/dealer/plans"
                className="block bg-gradient-to-r from-blue-600 to-purple-600 text-white rounded-lg p-4 text-center hover:opacity-90 transition-opacity"
              >
                <TrendingUp className="h-5 w-5 mx-auto mb-2" />
                <p className="text-sm font-semibold">Actualiza tu Plan</p>
                <p className="text-xs text-blue-100 mt-1">Desbloquea más funciones</p>
              </Link>
            </div>
          )}
        </aside>

        {/* Main Content */}
        <main className="flex-1 overflow-y-auto">
          {children}
        </main>
      </div>
    </div>
  );
};

export default DealerLayout;
```

### usePermissions Hook

```typescript
// src/hooks/usePermissions.ts
import { useAuthStore } from "@/store/authStore";

type DealerPlan = "free" | "basic" | "pro" | "enterprise";

interface PlanLimits {
  maxListings: number;
  maxFeaturedListings: number;
  maxPhotosPerListing: number;
  hasAnalytics: boolean;
  hasCRM: boolean;
  hasMarketAnalysis: boolean;
}

interface Usage {
  listings: number;
  featuredListings: number;
  photos: number;
}

const planLimits: Record<DealerPlan, PlanLimits> = {
  free: {
    maxListings: 5,
    maxFeaturedListings: 0,
    maxPhotosPerListing: 5,
    hasAnalytics: false,
    hasCRM: false,
    hasMarketAnalysis: false,
  },
  basic: {
    maxListings: 15,
    maxFeaturedListings: 3,
    maxPhotosPerListing: 10,
    hasAnalytics: true,
    hasCRM: false,
    hasMarketAnalysis: false,
  },
  pro: {
    maxListings: 50,
    maxFeaturedListings: 10,
    maxPhotosPerListing: 20,
    hasAnalytics: true,
    hasCRM: true,
    hasMarketAnalysis: false,
  },
  enterprise: {
    maxListings: Infinity,
    maxFeaturedListings: 50,
    maxPhotosPerListing: 30,
    hasAnalytics: true,
    hasCRM: true,
    hasMarketAnalysis: true,
  },
};

export function usePermissions() {
  const { user } = useAuthStore();

  const dealerPlan = (user?.dealerPlan || "free") as DealerPlan;
  const limits = planLimits[dealerPlan];

  // Mock usage - would come from API
  const usage: Usage = {
    listings: user?.usage?.listings || 0,
    featuredListings: user?.usage?.featuredListings || 0,
    photos: user?.usage?.photos || 0,
  };

  const getUsagePercentage = (
    type: "listings" | "featured" | "photos",
  ): number => {
    const max =
      type === "listings"
        ? limits.maxListings
        : type === "featured"
          ? limits.maxFeaturedListings
          : limits.maxPhotosPerListing;

    if (max === Infinity) return 20;

    const current =
      type === "listings"
        ? usage.listings
        : type === "featured"
          ? usage.featuredListings
          : usage.photos;

    return Math.min(100, (current / max) * 100);
  };

  const hasReachedLimit = (
    type: "listings" | "featured" | "photos",
  ): boolean => {
    return getUsagePercentage(type) >= 100;
  };

  const canAccessFeature = (feature: keyof PlanLimits): boolean => {
    return !!limits[feature];
  };

  return {
    user,
    dealerPlan,
    limits,
    usage,
    getUsagePercentage,
    hasReachedLimit,
    canAccessFeature,
  };
}
```

---

## 🏢 DEALERPORTALLAYOUT

**Uso:** Portal avanzado de dealers con menú expandible

```typescript
// src/layouts/DealerPortalLayout.tsx
import type { ReactNode } from 'react';
import { useState, useEffect } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '@/hooks/useAuth';
import { usePermissions } from '@/hooks/usePermissions';
import { Crown, Building2 } from 'lucide-react';
import {
  FiMenu, FiX, FiHome, FiGrid, FiTruck, FiUsers,
  FiTarget, FiBarChart2, FiSettings, FiDollarSign,
  FiMessageSquare, FiFileText, FiBell, FiLogOut,
  FiChevronDown, FiChevronRight, FiPlusCircle,
  FiPackage, FiTrendingUp, FiCalendar, FiStar,
  FiSearch, FiHelpCircle, FiExternalLink,
} from 'react-icons/fi';
import { FaCar } from 'react-icons/fa';
import NotificationDropdown from '@/components/organisms/NotificationDropdown';

interface MenuItem {
  id: string;
  label: string;
  icon: React.ElementType;
  href?: string;
  badge?: string | number;
  badgeColor?: string;
  children?: MenuItem[];
  requiredPlan?: string[];
}

const DealerPortalLayout = ({ children }: { children: ReactNode }) => {
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const { dealerPlan, usage, limits, getUsagePercentage, hasReachedLimit } = usePermissions();

  const [isSidebarOpen, setIsSidebarOpen] = useState(true);
  const [isMobileSidebarOpen, setIsMobileSidebarOpen] = useState(false);
  const [expandedSections, setExpandedSections] = useState<string[]>(['dashboard']);
  const [isUserMenuOpen, setIsUserMenuOpen] = useState(false);

  // Menú principal del dealer
  const menuItems: MenuItem[] = [
    {
      id: 'dashboard',
      label: 'Dashboard',
      icon: FiGrid,
      children: [
        { id: 'overview', label: 'Resumen General', icon: FiHome, href: '/dealer/dashboard' },
        { id: 'sales', label: 'Ventas del Mes', icon: FiTrendingUp, href: '/dealer/sales' },
        { id: 'alerts', label: 'Alertas', icon: FiBell, href: '/dealer/alerts', badge: 3, badgeColor: 'bg-red-500' },
      ],
    },
    {
      id: 'inventory',
      label: 'Inventario',
      icon: FaCar,
      children: [
        { id: 'vehicles', label: 'Mis Vehículos', icon: FaCar, href: '/dealer/inventory' },
        { id: 'add-vehicle', label: 'Agregar Vehículo', icon: FiPlusCircle, href: '/dealer/inventory/new' },
        { id: 'listings', label: 'Publicaciones', icon: FiPackage, href: '/dealer/listings' },
      ],
    },
    {
      id: 'leads',
      label: 'Leads & CRM',
      icon: FiTarget,
      children: [
        { id: 'inquiries', label: 'Consultas', icon: FiMessageSquare, href: '/dealer/inquiries' },
        { id: 'crm', label: 'Pipeline de Ventas', icon: FiTrendingUp, href: '/dealer/crm', requiredPlan: ['pro', 'enterprise'] },
      ],
    },
    {
      id: 'analytics',
      label: 'Analytics',
      icon: FiBarChart2,
      children: [
        { id: 'basic-analytics', label: 'Estadísticas', icon: FiBarChart2, href: '/dealer/analytics' },
        { id: 'advanced', label: 'Avanzado', icon: FiTrendingUp, href: '/dealer/analytics/advanced', requiredPlan: ['pro', 'enterprise'] },
      ],
    },
    {
      id: 'billing',
      label: 'Facturación',
      icon: FiDollarSign,
      children: [
        { id: 'plan', label: 'Mi Plan', icon: Crown, href: '/dealer/billing/plan' },
        { id: 'invoices', label: 'Facturas', icon: FiFileText, href: '/dealer/billing/invoices' },
      ],
    },
    {
      id: 'settings',
      label: 'Configuración',
      icon: FiSettings,
      children: [
        { id: 'profile', label: 'Perfil del Dealer', icon: Building2, href: '/dealer/settings/profile' },
        { id: 'team', label: 'Equipo', icon: FiUsers, href: '/dealer/settings/team', requiredPlan: ['pro', 'enterprise'] },
      ],
    },
  ];

  const toggleSection = (sectionId: string) => {
    setExpandedSections(prev =>
      prev.includes(sectionId)
        ? prev.filter(id => id !== sectionId)
        : [...prev, sectionId]
    );
  };

  const isItemAccessible = (item: MenuItem): boolean => {
    if (!item.requiredPlan || item.requiredPlan.length === 0) return true;
    return item.requiredPlan.includes(dealerPlan);
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Top Navbar */}
      <nav className="bg-white border-b border-gray-200 sticky top-0 z-50">
        <div className="flex items-center justify-between h-16 px-4">
          {/* Left: Logo + Toggle */}
          <div className="flex items-center gap-4">
            <button onClick={() => setIsMobileSidebarOpen(true)} className="lg:hidden p-2">
              <FiMenu size={24} />
            </button>
            <Link to="/" className="flex items-center gap-2">
              <div className="w-10 h-10 bg-blue-600 rounded-lg flex items-center justify-center">
                <span className="text-white font-bold text-xl">O</span>
              </div>
              <span className="text-xl font-bold hidden sm:block">OKLA</span>
              <span className="text-sm text-gray-500 hidden sm:block">| Portal Dealer</span>
            </Link>
          </div>

          {/* Right: Search + Notifications + User */}
          <div className="flex items-center gap-4">
            <NotificationDropdown />

            <div className={`px-3 py-1.5 rounded-full flex items-center gap-1.5 text-sm font-medium ${
              dealerPlan === 'enterprise' ? 'bg-purple-100 text-purple-700' :
              dealerPlan === 'pro' ? 'bg-blue-100 text-blue-700' :
              'bg-gray-100 text-gray-700'
            }`}>
              <Crown size={14} />
              <span className="capitalize">{dealerPlan}</span>
            </div>

            {/* User Menu */}
            <div className="relative">
              <button onClick={() => setIsUserMenuOpen(!isUserMenuOpen)} className="flex items-center gap-2">
                <div className="w-8 h-8 bg-blue-600 rounded-full flex items-center justify-center text-white font-semibold">
                  {user?.name?.charAt(0) || 'D'}
                </div>
                <FiChevronDown size={16} className="text-gray-500" />
              </button>

              {isUserMenuOpen && (
                <div className="absolute right-0 mt-2 w-56 bg-white rounded-lg shadow-lg border py-2">
                  <div className="px-4 py-2 border-b">
                    <p className="font-semibold">{user?.name}</p>
                    <p className="text-sm text-gray-500">{user?.email}</p>
                  </div>
                  <Link to="/dealer/settings/profile" className="block px-4 py-2 hover:bg-gray-50">Configuración</Link>
                  <Link to="/help" className="block px-4 py-2 hover:bg-gray-50">Centro de Ayuda</Link>
                  <hr className="my-2" />
                  <button onClick={() => { logout(); navigate('/'); }} className="w-full text-left px-4 py-2 text-red-600 hover:bg-red-50">
                    <FiLogOut className="inline mr-2" /> Cerrar Sesión
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>
      </nav>

      <div className="flex">
        {/* Desktop Sidebar */}
        <aside className={`hidden lg:flex flex-col bg-white border-r border-gray-200 transition-all duration-300 ${
          isSidebarOpen ? 'w-72' : 'w-20'
        }`}>
          <div className="flex-1 overflow-y-auto py-4">
            {menuItems.map(section => (
              <div key={section.id} className="mb-2">
                <button
                  onClick={() => toggleSection(section.id)}
                  className="w-full flex items-center justify-between px-4 py-2 text-gray-700 hover:bg-gray-50"
                >
                  <div className="flex items-center gap-3">
                    <section.icon size={18} />
                    {isSidebarOpen && <span className="font-medium">{section.label}</span>}
                  </div>
                  {isSidebarOpen && (
                    expandedSections.includes(section.id) ? <FiChevronDown size={16} /> : <FiChevronRight size={16} />
                  )}
                </button>

                {isSidebarOpen && expandedSections.includes(section.id) && section.children && (
                  <div className="ml-4 border-l border-gray-200 pl-4 mt-1 space-y-1">
                    {section.children.map(item => (
                      <Link
                        key={item.id}
                        to={isItemAccessible(item) ? (item.href || '#') : '#'}
                        className={`flex items-center justify-between px-3 py-2 rounded-lg text-sm ${
                          location.pathname === item.href
                            ? 'bg-blue-50 text-blue-700'
                            : !isItemAccessible(item)
                            ? 'text-gray-400 cursor-not-allowed'
                            : 'text-gray-600 hover:bg-gray-50'
                        }`}
                      >
                        <div className="flex items-center gap-2">
                          <item.icon size={16} />
                          <span>{item.label}</span>
                        </div>
                        {item.badge && (
                          <span className={`px-1.5 py-0.5 text-xs rounded-full text-white ${item.badgeColor || 'bg-blue-500'}`}>
                            {item.badge}
                          </span>
                        )}
                        {!isItemAccessible(item) && <Crown size={12} className="text-amber-500" />}
                      </Link>
                    ))}
                  </div>
                )}
              </div>
            ))}
          </div>

          {/* Sidebar Footer: Usage Stats */}
          {isSidebarOpen && (
            <div className="p-4 border-t">
              <div className="space-y-2">
                <div>
                  <div className="flex justify-between text-xs text-gray-600 mb-1">
                    <span>Publicaciones</span>
                    <span>{usage.listings}/{limits.maxListings === Infinity ? '∞' : limits.maxListings}</span>
                  </div>
                  <div className="w-full h-1.5 bg-gray-100 rounded-full">
                    <div className={`h-full rounded-full ${hasReachedLimit('listings') ? 'bg-red-500' : 'bg-blue-500'}`}
                      style={{ width: `${Math.min(100, getUsagePercentage('listings'))}%` }}
                    />
                  </div>
                </div>
              </div>
            </div>
          )}
        </aside>

        {/* Mobile Sidebar Overlay */}
        {isMobileSidebarOpen && (
          <>
            <div className="fixed inset-0 bg-black/50 z-40 lg:hidden" onClick={() => setIsMobileSidebarOpen(false)} />
            <aside className="fixed left-0 top-0 h-full w-72 bg-white z-50 lg:hidden overflow-y-auto">
              <div className="flex items-center justify-between p-4 border-b">
                <span className="font-bold text-lg">Menú</span>
                <button onClick={() => setIsMobileSidebarOpen(false)}>
                  <FiX size={24} />
                </button>
              </div>
              {/* Same menu items rendering */}
            </aside>
          </>
        )}

        {/* Main Content */}
        <main className="flex-1 overflow-y-auto">
          {children}
        </main>
      </div>
    </div>
  );
};

export default DealerPortalLayout;
```

---

## ✨ OKLALAYOUT

**Uso:** Layout premium con soporte de dark mode

```typescript
// src/layouts/OklaLayout.tsx
import React, { useState, useEffect } from 'react';
import { Outlet } from 'react-router-dom';
import { clsx } from 'clsx';
import { OklaHeader } from '@/components/navigation/OklaHeader';
import { OklaFooter } from '@/components/organisms/OklaFooter';

interface OklaLayoutProps {
  children?: React.ReactNode;
}

export const OklaLayout: React.FC<OklaLayoutProps> = ({ children }) => {
  const [darkMode, setDarkMode] = useState(false);
  const [isLoggedIn, setIsLoggedIn] = useState(false);

  // Check for dark mode preference
  useEffect(() => {
    const isDark = localStorage.getItem('okla-dark-mode') === 'true' ||
      (!localStorage.getItem('okla-dark-mode') &&
       window.matchMedia('(prefers-color-scheme: dark)').matches);

    setDarkMode(isDark);
    if (isDark) {
      document.documentElement.setAttribute('data-theme', 'dark');
      document.documentElement.classList.add('dark');
    }
  }, []);

  const toggleDarkMode = () => {
    const newValue = !darkMode;
    setDarkMode(newValue);
    localStorage.setItem('okla-dark-mode', String(newValue));

    if (newValue) {
      document.documentElement.setAttribute('data-theme', 'dark');
      document.documentElement.classList.add('dark');
    } else {
      document.documentElement.removeAttribute('data-theme');
      document.documentElement.classList.remove('dark');
    }
  };

  const handleLogin = () => {
    window.location.href = '/login';
  };

  const handleLogout = () => {
    setIsLoggedIn(false);
    // Clear auth state
  };

  const handleSearch = (query: string) => {
    window.location.href = `/browse?q=${encodeURIComponent(query)}`;
  };

  const mockUser = isLoggedIn ? {
    name: 'Usuario Demo',
    email: 'demo@okla.com',
  } : undefined;

  return (
    <div
      className={clsx(
        'okla-theme',
        'min-h-screen flex flex-col',
        'bg-[var(--okla-bg)] dark:bg-[var(--okla-bg)]',
        'text-[var(--okla-text)] dark:text-[var(--okla-text)]',
        'transition-colors duration-300'
      )}
    >
      <OklaHeader
        isLoggedIn={isLoggedIn}
        user={mockUser}
        onLogin={handleLogin}
        onLogout={handleLogout}
        onSearch={handleSearch}
        notificationCount={3}
        favoriteCount={5}
        darkMode={darkMode}
        onToggleDarkMode={toggleDarkMode}
      />

      <main className="flex-1">
        {children || <Outlet />}
      </main>

      <OklaFooter />
    </div>
  );
};

export default OklaLayout;
```

---

## ✅ VALIDACIÓN

### MainLayout

- [ ] Navbar visible en todas las páginas
- [ ] Footer visible en todas las páginas
- [ ] MaintenanceBanner condicional (si hay mantenimiento)
- [ ] EarlyBirdBanner condicional (si aplica)
- [ ] Contenido ocupa espacio disponible (flex-1)

### AuthLayout

- [ ] Sin navbar ni footer
- [ ] Form centrado en pantalla
- [ ] Hero branding visible solo en desktop (lg:flex)
- [ ] Responsive: full width en mobile

### AdminLayout

- [ ] Sidebar colapsable en desktop
- [ ] Sidebar overlay en mobile
- [ ] RBAC: redirige si no es admin
- [ ] Topbar con toggle de sidebar
- [ ] Contenido ajusta margin según sidebar

### DealerLayout

- [ ] Badge de plan visible (Free, Basic, Pro, Enterprise)
- [ ] Usage stats en sidebar (publicaciones, destacados)
- [ ] Progress bars con colores semánticos
- [ ] CTA "Actualiza tu Plan" para free/basic
- [ ] Logout funciona correctamente

### DealerPortalLayout

- [ ] Menú expandible por secciones
- [ ] Items bloqueados por plan (requiredPlan)
- [ ] Crown icon en items premium
- [ ] Badges de notificación
- [ ] Mobile sidebar con overlay
- [ ] User dropdown menu

### OklaLayout

- [ ] Dark mode toggle funciona
- [ ] Persiste preferencia en localStorage
- [ ] Respeta prefers-color-scheme del sistema
- [ ] CSS variables para tema
- [ ] OklaHeader y OklaFooter renderizan

---

## 🔗 USO EN RUTAS

```typescript
// src/App.tsx
import { Routes, Route } from 'react-router-dom';
import MainLayout from '@/layouts/MainLayout';
import AuthLayout from '@/layouts/AuthLayout';
import AdminLayout from '@/layouts/AdminLayout';
import DealerLayout from '@/layouts/DealerLayout';
import DealerPortalLayout from '@/layouts/DealerPortalLayout';
import OklaLayout from '@/layouts/OklaLayout';

// Páginas públicas
<Route element={<MainLayout><Outlet /></MainLayout>}>
  <Route path="/" element={<HomePage />} />
  <Route path="/search" element={<SearchPage />} />
  <Route path="/vehicles/:slug" element={<VehicleDetailPage />} />
</Route>

// Auth
<Route element={<AuthLayout><Outlet /></AuthLayout>}>
  <Route path="/login" element={<LoginPage />} />
  <Route path="/register" element={<RegisterPage />} />
</Route>

// Admin
<Route path="/admin" element={<ProtectedRoute><AdminLayout /></ProtectedRoute>}>
  <Route index element={<AdminDashboardPage />} />
  <Route path="users" element={<UsersManagementPage />} />
</Route>

// Dealer
<Route path="/dealer" element={<ProtectedRoute><DealerPortalLayout /></ProtectedRoute>}>
  <Route index element={<DealerDashboard />} />
  <Route path="inventory" element={<DealerInventoryPage />} />
</Route>
```

---

## 🧪 TESTS E2E (Playwright)

```typescript
import { test, expect } from "@playwright/test";
import { loginAsUser, loginAsAdmin, loginAsDealer } from "../helpers/auth";

test.describe("Layouts", () => {
  test("MainLayout debe mostrar navbar y footer en páginas públicas", async ({
    page,
  }) => {
    await page.goto("/");
    await expect(page.getByTestId("navbar")).toBeVisible();
    await expect(page.getByTestId("footer")).toBeVisible();
    await expect(page.getByTestId("main-content")).toBeVisible();
  });

  test("AuthLayout debe mostrar logo y formulario centrado", async ({
    page,
  }) => {
    await page.goto("/login");
    await expect(page.getByTestId("auth-layout")).toBeVisible();
    await expect(page.getByTestId("auth-logo")).toBeVisible();
    await expect(page.getByTestId("login-form")).toBeVisible();
  });

  test("AdminLayout debe mostrar sidebar con navegación RBAC", async ({
    page,
  }) => {
    await loginAsAdmin(page);
    await page.goto("/admin");
    await expect(page.getByTestId("admin-layout")).toBeVisible();
    await expect(page.getByTestId("admin-sidebar")).toBeVisible();
    await expect(page.getByTestId("sidebar-nav-users")).toBeVisible();
  });

  test("DealerPortalLayout debe mostrar dashboard y métricas", async ({
    page,
  }) => {
    await loginAsDealer(page);
    await page.goto("/dealer");
    await expect(page.getByTestId("dealer-portal-layout")).toBeVisible();
    await expect(page.getByTestId("dealer-sidebar")).toBeVisible();
    await expect(page.getByTestId("dealer-metrics")).toBeVisible();
  });

  test("OklaLayout debe mostrar branding premium", async ({ page }) => {
    await page.goto("/okla-premium");
    await expect(page.getByTestId("okla-layout")).toBeVisible();
    await expect(page.getByTestId("okla-branding")).toBeVisible();
  });

  test("ProtectedRoute debe redirigir a login si no autenticado", async ({
    page,
  }) => {
    await page.goto("/admin");
    await expect(page).toHaveURL(/\/login/);
  });
});
```

---

_Última actualización: Enero 2026_
