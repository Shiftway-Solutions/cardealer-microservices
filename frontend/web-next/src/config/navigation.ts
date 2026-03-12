/**
 * Navigation Configuration
 *
 * Centralized navigation items for all user types.
 * Used by cuenta/layout, admin/layout, and dealer layouts.
 *
 * This eliminates duplication of navigation menus across layouts.
 */

'use client';

import {
  User,
  Car,
  Heart,
  Bell,
  MessageSquare,
  Settings,
  Shield,
  CreditCard,
  Home,
  Building2,
  BarChart3,
  FileText,
  Upload,
  Users,
  UsersRound,
  ShieldCheck,
  Megaphone,
  DollarSign,
  Clock,
  Star,
  Search,
  TrendingUp,
  Package,
  Store,
  Flag,
  LayoutDashboard,
  LayoutGrid,
  ScrollText,
  Wrench,
  Brain,
  type LucideIcon,
} from 'lucide-react';

// =============================================================================
// TYPES
// =============================================================================

export interface NavItem {
  href: string;
  label: string;
  icon: LucideIcon;
  badge?: string | number;
  /** External link - opens in new tab */
  external?: boolean;
  /** Requires specific permission */
  permission?: string;
  /** Minimum plan required to access this feature (shown as lock+tooltip for lower plans) */
  requiredPlan?: string;
}

export interface NavSection {
  title: string;
  items: NavItem[];
}

/** @deprecated Use accountType from use-auth hook instead */
export type AccountType =
  | 'buyer'
  | 'seller'
  | 'dealer'
  | 'dealer_employee'
  | 'admin'
  | 'platform_employee'
  | 'guest';

// =============================================================================
// BUYER NAVIGATION
// Usuario individual sin vehículos publicados
// =============================================================================

export const BUYER_NAVIGATION: NavSection[] = [
  {
    title: 'Mi Cuenta',
    items: [
      { href: '/cuenta', label: 'Dashboard', icon: Home },
      { href: '/cuenta/perfil', label: 'Mi Perfil', icon: User },
    ],
  },
  {
    title: 'Búsqueda',
    items: [
      { href: '/cuenta/favoritos', label: 'Favoritos', icon: Heart },
      { href: '/cuenta/busquedas', label: 'Búsquedas Guardadas', icon: Search },
      { href: '/cuenta/alertas', label: 'Alertas de Precio', icon: Bell },
    ],
  },
  {
    title: 'Comunicación',
    items: [
      { href: '/mensajes', label: 'Mensajes', icon: MessageSquare },
      { href: '/cuenta/notificaciones', label: 'Notificaciones', icon: Bell },
    ],
  },
  {
    title: 'Configuración',
    items: [
      { href: '/cuenta/seguridad', label: 'Seguridad', icon: Shield },
      { href: '/cuenta/configuracion', label: 'Preferencias', icon: Settings },
    ],
  },
];

// =============================================================================
// SELLER NAVIGATION
// Usuario con vehículos publicados
// =============================================================================

export const SELLER_NAVIGATION: NavSection[] = [
  {
    title: 'Mi Cuenta',
    items: [
      { href: '/cuenta', label: 'Dashboard', icon: Home },
      { href: '/cuenta/perfil', label: 'Mi Perfil', icon: User },
    ],
  },
  {
    title: 'Mi Garage',
    items: [
      { href: '/cuenta/mis-vehiculos', label: 'Mi Garage', icon: Car },
      { href: '/cuenta/estadisticas', label: 'Estadísticas', icon: BarChart3 },
      { href: '/cuenta/consultas', label: 'Consultas Recibidas', icon: MessageSquare },
      { href: '/cuenta/resenas', label: 'Reseñas', icon: Star },
    ],
  },
  {
    title: 'Búsqueda',
    items: [
      { href: '/cuenta/favoritos', label: 'Favoritos', icon: Heart },
      { href: '/cuenta/alertas', label: 'Alertas de Precio', icon: Bell },
    ],
  },
  {
    title: 'Facturación',
    items: [
      { href: '/cuenta/pagos', label: 'Pagos', icon: CreditCard },
      { href: '/cuenta/historial', label: 'Historial', icon: FileText },
    ],
  },
  {
    title: 'Configuración',
    items: [
      { href: '/cuenta/seguridad', label: 'Seguridad', icon: Shield },
      { href: '/cuenta/notificaciones', label: 'Notificaciones', icon: Bell },
      { href: '/cuenta/configuracion', label: 'Preferencias', icon: Settings },
    ],
  },
];

// =============================================================================
// DEALER NAVIGATION
// Cuenta comercial con acceso al portal dealer
// =============================================================================

export const DEALER_NAVIGATION: NavSection[] = [
  {
    title: 'Portal Dealer',
    items: [
      { href: '/cuenta', label: 'Dashboard', icon: Home },
      { href: '/dealer/inventario', label: 'Inventario', icon: Package },
      { href: '/dealer/publicar', label: 'Publicar Vehículo', icon: Upload },
      {
        href: '/dealer/importar',
        label: 'Importar Masivo',
        icon: FileText,
        requiredPlan: 'VISIBLE',
      },
    ],
  },
  {
    title: 'Ventas',
    items: [
      {
        href: '/dealer/leads',
        label: 'Leads / Consultas',
        icon: MessageSquare,
        requiredPlan: 'VISIBLE',
      },
      { href: '/dealer/analytics', label: 'Analíticas', icon: TrendingUp, requiredPlan: 'VISIBLE' },
      {
        href: '/dealer/rendimiento',
        label: 'Rendimiento',
        icon: BarChart3,
        requiredPlan: 'VISIBLE',
      },
    ],
  },
  {
    title: 'Mi Negocio',
    items: [
      { href: '/dealer/perfil', label: 'Perfil del Negocio', icon: Store },
      { href: '/dealer/ubicaciones', label: 'Sucursales', icon: Building2 },
      { href: '/dealer/empleados', label: 'Mi Equipo', icon: Users },
    ],
  },
  {
    title: 'Facturación',
    items: [
      { href: '/dealer/suscripcion', label: 'Mi Suscripción', icon: Star },
      { href: '/dealer/facturacion', label: 'Facturación', icon: CreditCard },
      { href: '/dealer/historial-pagos', label: 'Historial de Pagos', icon: FileText },
    ],
  },
  {
    title: 'Cuenta Personal',
    items: [
      { href: '/cuenta/perfil', label: 'Mi Perfil', icon: User },
      { href: '/cuenta/seguridad', label: 'Seguridad', icon: Shield },
      { href: '/cuenta/configuracion', label: 'Configuración', icon: Settings },
    ],
  },
];

// =============================================================================
// ADMIN NAVIGATION
// Panel de administración de la plataforma
// =============================================================================

export const ADMIN_MAIN_NAVIGATION: NavItem[] = [
  { href: '/admin', label: 'Dashboard', icon: LayoutDashboard },
  { href: '/admin/usuarios', label: 'Usuarios', icon: Users },
  { href: '/admin/vehiculos', label: 'Vehículos', icon: Car },
  { href: '/admin/dealers', label: 'Dealers', icon: Building2 },
  { href: '/admin/reviews', label: 'Reseñas', icon: Star },
  { href: '/admin/reportes', label: 'Reportes', icon: Flag },
  { href: '/admin/kyc', label: 'Verificaciones KYC', icon: ShieldCheck },
];

export const ADMIN_MANAGEMENT_NAVIGATION: NavItem[] = [
  { href: '/admin/facturacion', label: 'Facturación', icon: CreditCard },
  { href: '/admin/analytics', label: 'Analytics', icon: BarChart3 },
  { href: '/admin/secciones', label: 'Secciones Homepage', icon: LayoutGrid },
  { href: '/admin/contenido', label: 'Contenido', icon: FileText },
  { href: '/admin/mensajes', label: 'Mensajes', icon: MessageSquare },
  { href: '/admin/planes', label: 'Planes de Suscripción', icon: Package },
  { href: '/admin/espacios-publicitarios', label: 'Espacios Publicitarios', icon: Megaphone },
];

export const ADMIN_SYSTEM_NAVIGATION: NavItem[] = [
  { href: '/admin/equipo', label: 'Equipo', icon: UsersRound },
  { href: '/admin/roles', label: 'Roles y Permisos', icon: Shield },
  { href: '/admin/configuracion', label: 'Configuración', icon: Settings },
  { href: '/admin/search-agent', label: 'SearchAgent IA', icon: Brain },
  { href: '/admin/logs', label: 'Logs del Sistema', icon: ScrollText },
  { href: '/admin/mantenimiento', label: 'Mantenimiento', icon: Wrench },
];

/**
 * Admin navigation in section format (for cuenta/layout compatibility)
 */
export const ADMIN_NAVIGATION: NavSection[] = [
  {
    title: 'Administración',
    items: [
      { href: '/admin', label: 'Dashboard Admin', icon: ShieldCheck },
      { href: '/admin/analytics', label: 'Métricas Plataforma', icon: TrendingUp },
      { href: '/admin/reportes', label: 'Reportes', icon: BarChart3 },
    ],
  },
  {
    title: 'Gestión de Usuarios',
    items: [
      { href: '/admin/usuarios', label: 'Usuarios', icon: Users },
      { href: '/admin/dealers', label: 'Dealers', icon: Building2 },
      { href: '/admin/reviews', label: 'Reseñas', icon: Star },
      { href: '/admin/kyc', label: 'Verificaciones KYC', icon: ShieldCheck },
    ],
  },
  {
    title: 'Contenido',
    items: [
      { href: '/admin/vehiculos', label: 'Vehículos', icon: Car },
      { href: '/admin/contenido', label: 'Gestión de Contenido', icon: FileText },
    ],
  },
  {
    title: 'Marketing',
    items: [
      { href: '/admin/promociones', label: 'Promociones', icon: Megaphone },
      { href: '/admin/banners', label: 'Banners', icon: FileText },
      { href: '/admin/early-bird', label: 'Early Bird', icon: Clock },
    ],
  },
  {
    title: 'Finanzas',
    items: [
      { href: '/admin/facturacion', label: 'Facturación', icon: DollarSign },
      { href: '/admin/suscripciones', label: 'Suscripciones', icon: Star },
      { href: '/admin/transacciones', label: 'Transacciones', icon: CreditCard },
      { href: '/admin/costos-llm', label: 'Costos LLM / IA', icon: Brain },
    ],
  },
  {
    title: 'Sistema',
    items: [
      { href: '/admin/mantenimiento', label: 'Mantenimiento', icon: Settings },
      { href: '/admin/logs', label: 'Logs', icon: FileText },
      { href: '/admin/configuracion', label: 'Configuración', icon: Settings },
      { href: '/admin/search-agent', label: 'SearchAgent IA', icon: Brain },
    ],
  },
  {
    title: 'Mi Cuenta',
    items: [
      { href: '/cuenta/perfil', label: 'Mi Perfil', icon: User },
      { href: '/cuenta/seguridad', label: 'Seguridad', icon: Shield },
    ],
  },
];

// =============================================================================
// HELPER FUNCTIONS
// =============================================================================

export interface UserForNavigation {
  accountType?:
    | 'admin'
    | 'dealer'
    | 'dealer_employee'
    | 'buyer'
    | 'seller'
    | 'platform_employee'
    | 'guest'
    | string;
  userIntent?: 'browse' | 'buy' | 'sell' | 'buy_and_sell';
  listingsCount?: number;
}

/**
 * Get navigation sections based on user type
 */
export function getNavigationForUser(user: UserForNavigation): NavSection[] {
  // Admin - Full admin navigation
  if (user.accountType === 'admin') {
    return ADMIN_NAVIGATION;
  }

  // Platform Employee - Same as admin navigation (staff)
  if (user.accountType === 'platform_employee') {
    return ADMIN_NAVIGATION;
  }

  // Dealer - Dealer portal navigation
  if (user.accountType === 'dealer') {
    return DEALER_NAVIGATION;
  }

  // Dealer Employee - Same as dealer navigation (employee of a dealer)
  if (user.accountType === 'dealer_employee') {
    return DEALER_NAVIGATION;
  }

  // Seller accountType - Seller navigation
  if (user.accountType === 'seller') {
    return SELLER_NAVIGATION;
  }

  // Buyer with sell intent: use userIntent to determine navigation
  if (user.userIntent === 'sell' || user.userIntent === 'buy_and_sell') {
    return SELLER_NAVIGATION;
  }

  // Fallback: check if user has listings
  if ((user.listingsCount ?? 0) > 0) {
    return SELLER_NAVIGATION;
  }

  // Default: Buyer navigation (browse, buy intent, or no intent set)
  return BUYER_NAVIGATION;
}

/**
 * Get user badge based on account type
 */
export function getUserBadge(user: UserForNavigation): { label: string; color: string } | null {
  if (user.accountType === 'admin') {
    return { label: 'Admin', color: 'bg-red-100 text-red-700' };
  }
  if (user.accountType === 'platform_employee') {
    return { label: 'Plataforma', color: 'bg-red-100 text-red-700' };
  }
  if (user.accountType === 'dealer') {
    return { label: 'Dealer', color: 'bg-blue-100 text-blue-700' };
  }
  if (user.accountType === 'dealer_employee') {
    return { label: 'Empleado', color: 'bg-blue-100 text-blue-700' };
  }
  if (user.accountType === 'seller') {
    return { label: 'Vendedor', color: 'bg-green-100 text-green-700' };
  }
  if (
    user.userIntent === 'sell' ||
    user.userIntent === 'buy_and_sell' ||
    (user.listingsCount ?? 0) > 0
  ) {
    return { label: 'Vendedor', color: 'bg-green-100 text-green-700' };
  }
  if (user.userIntent === 'buy') {
    return { label: 'Comprador', color: 'bg-purple-100 text-purple-700' };
  }
  return null; // Browse intent doesn't need a badge
}

/**
 * Check if a path is active (exact or starts with)
 */
export function isPathActive(currentPath: string, itemPath: string): boolean {
  if (itemPath === '/cuenta' || itemPath === '/admin' || itemPath === '/dealer') {
    return currentPath === itemPath;
  }
  return currentPath.startsWith(itemPath);
}
