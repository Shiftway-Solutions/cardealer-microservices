'use client';

import * as React from 'react';
import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/hooks/use-auth';
import {
  Menu,
  X,
  Heart,
  User,
  Car,
  Bell,
  MessageSquare,
  LogOut,
  Plus,
  Building2,
  Home,
  Eye,
  DollarSign,
  CheckCircle,
  Clock,
  ChevronRight,
  ShieldCheck,
  Package,
  BarChart3,
  Settings,
  Store,
  TrendingUp,
  Users,
  CreditCard,
  Sparkles,
  UserPlus,
  Flag,
  AlertTriangle,
} from 'lucide-react';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';

// =============================================================================
// TYPES
// =============================================================================

interface NavLink {
  href: string;
  label: string;
  icon?: React.ReactNode;
}

interface UserType {
  email: string;
  firstName?: string;
  lastName?: string;
  avatarUrl?: string;
  accountType?:
    | 'buyer'
    | 'seller'
    | 'dealer'
    | 'dealer_employee'
    | 'admin'
    | 'platform_employee'
    | 'guest';
  userIntent?: 'browse' | 'buy' | 'sell' | 'buy_and_sell';
  listingsCount?: number;
}

// =============================================================================
// NAVIGATION DATA POR TIPO DE USUARIO
// =============================================================================

/**
 * Links principales del navbar - Para CONSUMIDORES (compradores, vendedores, dealers)
 */
const consumerNavLinks: NavLink[] = [
  { href: '/', label: 'Inicio', icon: <Home className="h-4 w-4" /> },
  { href: '/vehiculos', label: 'Comprar', icon: <Car className="h-4 w-4" /> },
  { href: '/vender', label: 'Vender', icon: <Plus className="h-4 w-4" /> },
  { href: '/dealers', label: 'Dealers', icon: <Building2 className="h-4 w-4" /> },
];

/**
 * Link adicional para VISITANTES - ¿Por qué OKLA?
 */
const visitorNavLinks: NavLink[] = [
  { href: '/#por-que-okla', label: '¿Por qué OKLA?', icon: <Sparkles className="h-4 w-4" /> },
];

/**
 * Links adicionales para DEALERS en el navbar principal
 */
const dealerNavLinks: NavLink[] = [
  { href: '/dealer', label: 'Mi Portal', icon: <Store className="h-4 w-4" /> },
];

/**
 * Links para ADMIN - NO incluyen links de consumidor
 * Admin administra la plataforma, no consume
 */
const adminNavLinks: NavLink[] = [
  { href: '/', label: 'Inicio', icon: <Home className="h-4 w-4" /> },
  { href: '/admin', label: 'Panel Admin', icon: <ShieldCheck className="h-4 w-4" /> },
];

/**
 * Retorna los links del navbar según el tipo de usuario
 */
function getNavLinks(user: UserType | null): NavLink[] {
  // VISITANTE NO LOGUEADO - Mostrar link "¿Por qué OKLA?"
  if (!user) return [...consumerNavLinks, ...visitorNavLinks];

  // ADMIN / PLATFORM_EMPLOYEE - Solo links de administración (no consume la plataforma)
  if (user.accountType === 'admin' || user.accountType === 'platform_employee') {
    return adminNavLinks;
  }

  // DEALER / DEALER_EMPLOYEE - Links de consumidor + Mi Portal
  if (user.accountType === 'dealer' || user.accountType === 'dealer_employee') {
    return [...consumerNavLinks, ...dealerNavLinks];
  }

  // Compradores y vendedores individuales
  return consumerNavLinks;
}

/**
 * Retorna el badge del tipo de usuario para mostrar en el navbar
 * NOTA: Para admin y dealer, NO mostramos badge en el botón porque
 * ya tienen un link visible en la navegación principal (evita redundancia)
 */
function getUserBadgeInfo(user: UserType | null): { label: string; color: string } | null {
  if (!user) return null;

  // Admin y Platform Employee ya tienen link visible en navbar - no necesitan badge
  if (user.accountType === 'admin' || user.accountType === 'platform_employee') {
    return null; // Ya tiene link "Admin" en navegación
  }
  // Dealer y Dealer Employee ya tienen link visible en navbar
  if (user.accountType === 'dealer' || user.accountType === 'dealer_employee') {
    return null; // Ya tiene link "Mi Portal" en navegación
  }
  // Seller accountType - siempre muestra badge Vendedor
  if (user.accountType === 'seller') {
    return { label: 'Vendedor', color: 'bg-green-500 text-white' };
  }
  // Buyer con intent de venta o listings activos
  if (
    user.userIntent === 'sell' ||
    user.userIntent === 'buy_and_sell' ||
    (user.listingsCount ?? 0) > 0
  ) {
    return { label: 'Vendedor', color: 'bg-green-500 text-white' };
  }
  if (user.userIntent === 'buy') {
    return { label: 'Comprador', color: 'bg-purple-500 text-white' };
  }
  return null;
}
/**
 * Badge para mostrar DENTRO del dropdown header (donde sí tiene sentido el contexto)
 */
function getDropdownBadgeInfo(user: UserType | null): { label: string; color: string } | null {
  if (!user) return null;

  if (user.accountType === 'admin') {
    return { label: 'Admin', color: 'bg-red-500 text-white' };
  }
  if (user.accountType === 'platform_employee') {
    return { label: 'Plataforma', color: 'bg-red-500 text-white' };
  }
  if (user.accountType === 'dealer') {
    return { label: 'Dealer', color: 'bg-blue-500 text-white' };
  }
  if (user.accountType === 'dealer_employee') {
    return { label: 'Empleado Dealer', color: 'bg-blue-500 text-white' };
  }
  if (user.accountType === 'seller') {
    return { label: 'Vendedor', color: 'bg-green-500 text-white' };
  }
  if (
    user.userIntent === 'sell' ||
    user.userIntent === 'buy_and_sell' ||
    (user.listingsCount ?? 0) > 0
  ) {
    return { label: 'Vendedor', color: 'bg-green-500 text-white' };
  }
  if (user.userIntent === 'buy') {
    return { label: 'Comprador', color: 'bg-purple-500 text-white' };
  }
  return null;
}
/**
 * Retorna las opciones del dropdown según el tipo de usuario
 * NOTA: Admin NO tiene opciones de consumidor (favoritos, cuenta personal, etc.)
 */
function getDropdownItems(
  user: UserType | null
): { href: string; label: string; icon: React.ElementType }[] {
  if (!user) return [];

  // Items base para CONSUMIDORES (compradores, vendedores, dealers)
  const consumerBaseItems = [
    { href: '/cuenta', label: 'Mi Cuenta', icon: User },
    { href: '/cuenta/favoritos', label: 'Favoritos', icon: Heart },
  ];

  // ADMIN / PLATFORM_EMPLOYEE - Solo opciones de administración (NO consume la plataforma)
  if (user.accountType === 'admin' || user.accountType === 'platform_employee') {
    return [
      { href: '/admin', label: 'Dashboard Admin', icon: ShieldCheck },
      { href: '/admin/usuarios', label: 'Usuarios', icon: Users },
      { href: '/admin/vehiculos', label: 'Vehículos', icon: Settings },
      { href: '/admin/analytics', label: 'Métricas', icon: BarChart3 },
      { href: '/cuenta/perfil', label: 'Mi Perfil', icon: User },
      { href: '/cuenta/seguridad', label: 'Seguridad', icon: Settings },
    ];
  }

  // DEALER / DEALER_EMPLOYEE - Accesos a portal + opciones de consumidor
  if (user.accountType === 'dealer' || user.accountType === 'dealer_employee') {
    return [
      { href: '/dealer/inventario', label: 'Mi Inventario', icon: Package },
      { href: '/dealer/leads', label: 'Consultas', icon: MessageSquare },
      { href: '/dealer/analytics', label: 'Estadísticas', icon: TrendingUp },
      ...consumerBaseItems,
      { href: '/dealer/facturacion', label: 'Facturación', icon: CreditCard },
    ];
  }

  // VENDEDOR - accountType seller O buyer con listings/intent de venta
  if (
    user.accountType === 'seller' ||
    (user.listingsCount ?? 0) > 0 ||
    user.userIntent === 'sell' ||
    user.userIntent === 'buy_and_sell'
  ) {
    return [
      { href: '/cuenta/mis-vehiculos', label: 'Mis Vehículos', icon: Car },
      { href: '/dashboard', label: 'Estadísticas', icon: BarChart3 },
      ...consumerBaseItems,
      { href: '/cuenta/pagos', label: 'Pagos', icon: CreditCard },
    ];
  }

  // COMPRADOR PURO
  return [
    ...consumerBaseItems,
    { href: '/cuenta/alertas', label: 'Alertas de Precio', icon: Bell },
  ];
}

// =============================================================================
// NOTIFICATIONS DATA
// =============================================================================

// Mock notifications para CONSUMIDORES (compradores, vendedores, dealers)
const consumerNotifications = [
  {
    id: '1',
    type: 'message',
    title: 'Nuevo mensaje',
    description: 'Carlos te envió un mensaje sobre tu Toyota Corolla',
    time: 'Hace 5 min',
    read: false,
    href: '/mensajes',
  },
  {
    id: '2',
    type: 'view',
    title: 'Tu vehículo fue visto',
    description: 'Honda Civic 2022 tiene 15 nuevas vistas',
    time: 'Hace 1 hora',
    read: false,
    href: '/cuenta/mis-vehiculos',
  },
  {
    id: '3',
    type: 'price',
    title: 'Bajó de precio',
    description: 'Hyundai Tucson que guardaste bajó RD$50,000',
    time: 'Hace 2 horas',
    read: true,
    href: '/cuenta/favoritos',
  },
  {
    id: '4',
    type: 'sold',
    title: '¡Felicidades!',
    description: 'Tu Toyota Camry fue marcado como vendido',
    time: 'Ayer',
    read: true,
    href: '/cuenta/mis-vehiculos',
  },
];

// Mock notifications para ADMINISTRADORES
const adminNotifications = [
  {
    id: '1',
    type: 'kyc',
    title: 'Verificaciones pendientes',
    description: '5 usuarios esperan aprobación de KYC',
    time: 'Hace 10 min',
    read: false,
    href: '/admin/kyc',
  },
  {
    id: '2',
    type: 'report',
    title: 'Nuevos reportes',
    description: '3 publicaciones reportadas por contenido',
    time: 'Hace 30 min',
    read: false,
    href: '/admin/vehiculos',
  },
  {
    id: '3',
    type: 'dealer',
    title: 'Solicitud de dealer',
    description: 'AutoMax RD solicita cuenta dealer',
    time: 'Hace 2 horas',
    read: true,
    href: '/admin/dealers',
  },
  {
    id: '4',
    type: 'alert',
    title: 'Alerta de sistema',
    description: 'Alto volumen de registros en últimas 24h',
    time: 'Ayer',
    read: true,
    href: '/admin/metricas',
  },
];

/**
 * Retorna las notificaciones según el tipo de usuario
 */
function getNotifications(user: UserType | null) {
  if (user?.accountType === 'admin' || user?.accountType === 'platform_employee') {
    return adminNotifications;
  }
  return consumerNotifications;
}

const notificationIcons: Record<string, React.ElementType> = {
  // Consumer types
  message: MessageSquare,
  view: Eye,
  price: DollarSign,
  sold: CheckCircle,
  // Admin types
  kyc: ShieldCheck,
  report: Flag,
  dealer: Building2,
  alert: AlertTriangle,
  // Fallback
  default: Bell,
};

const notificationColors: Record<string, string> = {
  // Consumer types
  message: 'bg-blue-100 text-blue-600 dark:bg-blue-900/30 dark:text-blue-400',
  view: 'bg-purple-100 text-purple-600 dark:bg-purple-900/30 dark:text-purple-400',
  price: 'bg-green-100 text-green-600 dark:bg-green-900/30 dark:text-green-400',
  sold: 'bg-primary/10 text-primary dark:bg-primary/95/30 dark:text-primary/80',
  // Admin types
  kyc: 'bg-amber-100 text-amber-600 dark:bg-amber-900/30 dark:text-amber-400',
  report: 'bg-red-100 text-red-600 dark:bg-red-900/30 dark:text-red-400',
  dealer: 'bg-indigo-100 text-indigo-600 dark:bg-indigo-900/30 dark:text-indigo-400',
  alert: 'bg-orange-100 text-orange-600 dark:bg-orange-900/30 dark:text-orange-400',
  // Fallback
  default: 'bg-muted text-muted-foreground',
};

// =============================================================================
// LOGO COMPONENT
// =============================================================================

function Logo() {
  return (
    <Link href="/" className="group flex items-center gap-2.5">
      <div className="relative">
        <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-[#00A870] to-[#009663] shadow-lg shadow-[#00A870]/20 transition-all group-hover:shadow-xl group-hover:shadow-[#00A870]/30">
          <span className="text-xl font-bold text-white">O</span>
        </div>
        {/* Subtle glow effect */}
        <div className="absolute inset-0 rounded-xl bg-[#00A870] opacity-0 blur-lg transition-opacity group-hover:opacity-30" />
      </div>
      <span className="text-foreground text-2xl font-bold tracking-tight">OKLA</span>
    </Link>
  );
}

// =============================================================================
// DESKTOP NAVIGATION
// =============================================================================

interface DesktopNavProps {
  pathname: string;
  isAuthenticated: boolean;
  user: UserType | null;
}

function DesktopNav({ pathname, isAuthenticated, user }: DesktopNavProps) {
  const navLinks = getNavLinks(user);
  const notifications = getNotifications(user);

  return (
    <div className="hidden min-w-0 items-center gap-1 lg:flex">
      {/* Main Links - dinámicos según tipo de usuario */}
      {navLinks.map(link => (
        <Link
          key={link.href}
          href={link.href}
          className={cn(
            'relative flex shrink-0 items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium tracking-wide transition-colors',
            pathname === link.href || (link.href !== '/' && pathname.startsWith(link.href))
              ? 'text-primary'
              : 'text-foreground/80 hover:bg-muted hover:text-foreground'
          )}
        >
          {link.icon}
          {link.label}
          {(pathname === link.href || (link.href !== '/' && pathname.startsWith(link.href))) && (
            <span className="absolute right-2 bottom-0 left-2 h-0.5 rounded-full bg-[#00A870] transition-all duration-300" />
          )}
        </Link>
      ))}

      {/* User Links when authenticated - with tooltips and badges */}
      {isAuthenticated && (
        <>
          <div className="via-border mx-3 h-8 w-px bg-gradient-to-b from-transparent to-transparent" />
          <TooltipProvider delayDuration={200}>
            {/* Favorites Link - SOLO para consumidores, NO para admin/platform_employee */}
            {user?.accountType !== 'admin' && user?.accountType !== 'platform_employee' && (
              <Tooltip>
                <TooltipTrigger asChild>
                  <Link
                    href="/cuenta/favoritos"
                    className={cn(
                      'relative flex items-center justify-center rounded-xl p-2.5 transition-all',
                      pathname === '/cuenta/favoritos'
                        ? 'bg-primary/10 text-primary shadow-sm'
                        : 'text-muted-foreground hover:bg-muted hover:text-foreground'
                    )}
                  >
                    <Heart className="h-4 w-4" />
                  </Link>
                </TooltipTrigger>
                <TooltipContent side="bottom" className="font-medium">
                  Favoritos
                </TooltipContent>
              </Tooltip>
            )}

            {/* Notifications Dropdown */}
            <Popover>
              <Tooltip>
                <TooltipTrigger asChild>
                  <PopoverTrigger asChild>
                    <button
                      className={cn(
                        'relative flex items-center justify-center rounded-xl p-2.5 transition-all',
                        pathname === '/cuenta/notificaciones'
                          ? 'bg-primary/10 text-primary shadow-sm'
                          : 'text-muted-foreground hover:bg-muted hover:text-foreground'
                      )}
                    >
                      <Bell className="h-4 w-4" />
                      {/* Unread count badge */}
                      {notifications.filter(n => !n.read).length > 0 && (
                        <span className="absolute -top-0.5 -right-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-[#00A870] px-1 text-[10px] font-bold text-white shadow-sm">
                          {notifications.filter(n => !n.read).length}
                        </span>
                      )}
                    </button>
                  </PopoverTrigger>
                </TooltipTrigger>
                <TooltipContent side="bottom" className="font-medium">
                  Notificaciones
                </TooltipContent>
              </Tooltip>
              <PopoverContent align="end" className="w-80 p-0 shadow-xl" sideOffset={8}>
                {/* Header */}
                <div className="border-border flex items-center justify-between border-b px-4 py-3">
                  <h3 className="text-foreground font-semibold">Notificaciones</h3>
                  <button className="text-primary text-xs font-medium hover:underline">
                    Marcar todo leído
                  </button>
                </div>

                {/* Notifications List */}
                <div className="max-h-80 overflow-y-auto">
                  {notifications.length > 0 ? (
                    notifications.map(notification => {
                      const Icon =
                        notificationIcons[notification.type] || notificationIcons.default;
                      const colorClass =
                        notificationColors[notification.type] || notificationColors.default;

                      return (
                        <Link
                          key={notification.id}
                          href={notification.href}
                          className={cn(
                            'hover:bg-muted flex gap-3 px-4 py-3 transition-colors',
                            !notification.read && 'bg-primary/5'
                          )}
                        >
                          <div
                            className={cn(
                              'flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-full',
                              colorClass
                            )}
                          >
                            <Icon className="h-5 w-5" />
                          </div>
                          <div className="min-w-0 flex-1">
                            <p
                              className={cn(
                                'text-sm',
                                !notification.read
                                  ? 'text-foreground font-semibold'
                                  : 'text-muted-foreground'
                              )}
                            >
                              {notification.title}
                            </p>
                            <p className="text-muted-foreground truncate text-xs">
                              {notification.description}
                            </p>
                            <p className="text-muted-foreground/70 mt-1 flex items-center gap-1 text-xs">
                              <Clock className="h-3 w-3" />
                              {notification.time}
                            </p>
                          </div>
                          {!notification.read && (
                            <div className="flex-shrink-0 self-center">
                              <span className="h-2 w-2 rounded-full bg-[#00A870]" />
                            </div>
                          )}
                        </Link>
                      );
                    })
                  ) : (
                    <div className="px-4 py-8 text-center">
                      <Bell className="text-muted-foreground/30 mx-auto h-10 w-10" />
                      <p className="text-muted-foreground mt-2 text-sm">No tienes notificaciones</p>
                    </div>
                  )}
                </div>

                {/* Footer */}
                <div className="border-border border-t">
                  <Link
                    href="/cuenta/notificaciones"
                    className="text-primary hover:bg-muted flex items-center justify-center gap-2 px-4 py-3 text-sm font-medium transition-colors"
                  >
                    Ver todas las notificaciones
                    <ChevronRight className="h-4 w-4" />
                  </Link>
                </div>
              </PopoverContent>
            </Popover>
          </TooltipProvider>
        </>
      )}
    </div>
  );
}

// =============================================================================
// RIGHT SIDE ACTIONS
// =============================================================================

interface RightActionsProps {
  isAuthenticated: boolean;
  user: {
    email: string;
    firstName?: string;
    lastName?: string;
    avatarUrl?: string;
    accountType?:
      | 'buyer'
      | 'seller'
      | 'dealer'
      | 'dealer_employee'
      | 'admin'
      | 'platform_employee'
      | 'guest';
    userIntent?: 'browse' | 'buy' | 'sell' | 'buy_and_sell';
    listingsCount?: number;
  } | null;
  onLogout: () => void;
  onMobileMenuToggle: () => void;
  isMobileMenuOpen: boolean;
}

function RightActions({
  isAuthenticated,
  user,
  onLogout,
  onMobileMenuToggle,
  isMobileMenuOpen,
}: RightActionsProps) {
  const [isUserMenuOpen, setIsUserMenuOpen] = React.useState(false);
  const userMenuRef = React.useRef<HTMLDivElement>(null);

  // Close dropdown when clicking outside
  React.useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (userMenuRef.current && !userMenuRef.current.contains(event.target as Node)) {
        setIsUserMenuOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  return (
    <div className="flex flex-shrink-0 items-center gap-2 sm:gap-3">
      {/* Auth Buttons - CTA llamativo para visitantes */}
      {!isAuthenticated && (
        <div className="hidden items-center gap-2 sm:flex">
          {/* Botón Ingresar - Secundario */}
          <Button
            asChild
            variant="ghost"
            size="sm"
            className="text-foreground hover:bg-muted rounded-full px-4 py-2 text-sm font-medium"
          >
            <Link href="/login">
              <span>Ingresar</span>
            </Link>
          </Button>

          {/* Botón Registrarse - Principal con gradiente */}
          <Button
            asChild
            size="sm"
            className="group to-primary hover:shadow-primary/25 relative gap-1.5 overflow-hidden rounded-full bg-gradient-to-r from-[#00A870] px-5 py-2.5 text-sm font-semibold text-white shadow-lg transition-all hover:shadow-xl"
          >
            <Link href="/registro">
              <UserPlus className="h-4 w-4 transition-transform group-hover:scale-110" />
              <span>Registrarse</span>
            </Link>
          </Button>
        </div>
      )}

      {/* User Menu with Dropdown - Desktop (only when authenticated) */}
      {isAuthenticated && user && (
        <div className="relative hidden lg:block" ref={userMenuRef}>
          <button
            onClick={() => setIsUserMenuOpen(!isUserMenuOpen)}
            className={cn(
              'flex items-center gap-2.5 rounded-full border-2 py-1 pr-3 pl-1 shadow-sm transition-all',
              isUserMenuOpen
                ? 'border-primary bg-primary/5 ring-primary/20 ring-2'
                : 'border-border bg-card hover:border-muted-foreground/30 hover:shadow-md'
            )}
            aria-label="Menú de usuario"
            aria-expanded={isUserMenuOpen}
          >
            {/* Avatar */}
            <div
              className={cn(
                'flex h-8 w-8 items-center justify-center rounded-full',
                isUserMenuOpen
                  ? 'from-primary to-primary bg-gradient-to-br'
                  : 'from-muted to-muted/80 bg-gradient-to-br'
              )}
            >
              {user?.firstName ? (
                <span
                  className={cn(
                    'text-sm font-bold',
                    isUserMenuOpen ? 'text-primary-foreground' : 'text-muted-foreground'
                  )}
                >
                  {user.firstName.charAt(0).toUpperCase()}
                </span>
              ) : (
                <User
                  className={cn(
                    'h-4 w-4',
                    isUserMenuOpen ? 'text-primary-foreground' : 'text-muted-foreground'
                  )}
                />
              )}
            </div>
            {/* User Name - Show only first word of firstName for compact display */}
            <span className="text-foreground max-w-20 truncate text-sm font-medium">
              {user?.firstName?.split(' ')[0] || 'Mi Cuenta'}
            </span>
            {/* Badge de tipo de usuario */}
            {(() => {
              const badge = getUserBadgeInfo(user);
              return badge ? (
                <span
                  className={cn('rounded-full px-1.5 py-0.5 text-[10px] font-bold', badge.color)}
                >
                  {badge.label}
                </span>
              ) : null;
            })()}
            {/* Chevron */}
            <ChevronRight
              className={cn(
                'text-muted-foreground h-4 w-4 transition-transform',
                isUserMenuOpen && 'rotate-90'
              )}
            />
          </button>

          {/* Dropdown Menu - Dinámico según tipo de usuario */}
          {isUserMenuOpen && (
            <div className="border-border bg-card animate-fade-in absolute top-full right-0 z-50 mt-2 w-64 origin-top-right rounded-xl border p-2 shadow-xl">
              {/* User Header con Badge */}
              <div className="border-border mb-2 border-b px-3 pb-3">
                <div className="flex items-center gap-2">
                  <p
                    className="text-foreground truncate text-sm font-semibold"
                    title={
                      user?.firstName && user?.lastName
                        ? `${user.firstName} ${user.lastName}`
                        : 'Mi Cuenta'
                    }
                  >
                    {user?.firstName && user?.lastName
                      ? `${user.firstName.split(' ')[0]} ${user.lastName.split(' ')[0]}`
                      : 'Mi Cuenta'}
                  </p>
                  {(() => {
                    const badge = getDropdownBadgeInfo(user);
                    return badge ? (
                      <span
                        className={cn(
                          'rounded-full px-2 py-0.5 text-[10px] font-bold',
                          badge.color
                        )}
                      >
                        {badge.label}
                      </span>
                    ) : null;
                  })()}
                </div>
                <p className="text-muted-foreground truncate text-xs">{user?.email || ''}</p>
              </div>

              {/* Links dinámicos según tipo de usuario */}
              {getDropdownItems(user).map(item => (
                <Link
                  key={item.href}
                  href={item.href}
                  className="text-foreground hover:bg-muted flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors"
                  onClick={() => setIsUserMenuOpen(false)}
                >
                  <item.icon className="text-muted-foreground h-4 w-4" />
                  {item.label}
                </Link>
              ))}

              <div className="border-border my-2 border-t" />
              <button
                className="text-destructive hover:bg-destructive/10 flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors"
                onClick={() => {
                  setIsUserMenuOpen(false);
                  onLogout();
                }}
              >
                <LogOut className="h-4 w-4" />
                Cerrar Sesión
              </button>
            </div>
          )}
        </div>
      )}

      {/* Mobile Menu Button */}
      <button
        className="text-muted-foreground hover:bg-muted flex h-10 w-10 items-center justify-center rounded-xl transition-colors lg:hidden"
        onClick={onMobileMenuToggle}
        aria-label={isMobileMenuOpen ? 'Cerrar menú' : 'Abrir menú'}
        aria-expanded={isMobileMenuOpen}
      >
        <span className="transition-transform duration-200">
          {isMobileMenuOpen ? <X className="h-6 w-6" /> : <Menu className="h-6 w-6" />}
        </span>
      </button>
    </div>
  );
}

// =============================================================================
// MOBILE MENU
// =============================================================================

interface MobileMenuProps {
  isOpen: boolean;
  pathname: string;
  isAuthenticated: boolean;
  user: {
    email: string;
    firstName?: string;
    lastName?: string;
    accountType?:
      | 'buyer'
      | 'seller'
      | 'dealer'
      | 'dealer_employee'
      | 'admin'
      | 'platform_employee'
      | 'guest';
    userIntent?: 'browse' | 'buy' | 'sell' | 'buy_and_sell';
    listingsCount?: number;
  } | null;
  onLogout: () => void;
}

function MobileMenu({ isOpen, pathname, isAuthenticated, user, onLogout }: MobileMenuProps) {
  const navLinks = getNavLinks(user);
  const dropdownItems = user ? getDropdownItems(user) : [];
  const userBadge = getDropdownBadgeInfo(user); // Badge solo en el header del perfil
  const notifications = getNotifications(user);

  return isOpen ? (
    <div className="border-border bg-card animate-slide-down overflow-hidden border-t lg:hidden">
      <div className="space-y-2 px-4 py-4">
        {/* User Info Header - Show when authenticated */}
        {isAuthenticated && user && (
          <div className="from-primary/10 to-primary/80/10 mb-3 flex items-center gap-3 rounded-xl bg-gradient-to-r px-4 py-3">
            <div className="from-primary to-primary flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-full bg-gradient-to-br shadow-md">
              <span className="text-lg font-bold text-white">
                {user.firstName?.charAt(0).toUpperCase() || 'U'}
              </span>
            </div>
            <div className="min-w-0 flex-1">
              <div className="flex items-center gap-2">
                <p
                  className="text-foreground truncate font-semibold"
                  title={
                    user.firstName && user.lastName
                      ? `${user.firstName} ${user.lastName}`
                      : user.firstName || 'Usuario'
                  }
                >
                  {user.firstName && user.lastName
                    ? `${user.firstName.split(' ')[0]} ${user.lastName.split(' ')[0]}`
                    : user.firstName?.split(' ')[0] || 'Usuario'}
                </p>
                {userBadge && (
                  <span
                    className={cn(
                      'rounded-full px-2 py-0.5 text-[10px] font-bold',
                      userBadge.color
                    )}
                  >
                    {userBadge.label}
                  </span>
                )}
              </div>
              <p className="text-muted-foreground truncate text-xs">{user.email}</p>
            </div>
          </div>
        )}

        {/* Main Navigation - Links dinámicos según tipo de usuario */}
        {navLinks.map(link => (
          <Link
            key={link.href}
            href={link.href}
            className={cn(
              'flex items-center gap-3 rounded-xl px-4 py-3 text-base font-medium transition-colors',
              pathname === link.href || (link.href !== '/' && pathname.startsWith(link.href))
                ? 'bg-primary/10 text-primary'
                : 'text-foreground hover:bg-muted'
            )}
          >
            {link.icon}
            {link.label}
          </Link>
        ))}

        {/* User Links - Dinámicos según tipo de usuario */}
        {isAuthenticated && user && (
          <>
            <div className="border-border my-3 border-t" />
            <p className="text-muted-foreground px-4 text-xs font-medium tracking-wider uppercase">
              Mi Cuenta
            </p>
            {dropdownItems.map(item => (
              <Link
                key={item.href}
                href={item.href}
                className={cn(
                  'flex items-center gap-3 rounded-xl px-4 py-3 text-base font-medium transition-colors',
                  pathname === item.href
                    ? 'bg-primary/10 text-primary'
                    : 'text-foreground hover:bg-muted'
                )}
              >
                <item.icon className="h-4 w-4" />
                {item.label}
              </Link>
            ))}
            <Link
              href="/mensajes"
              className={cn(
                'flex items-center gap-3 rounded-xl px-4 py-3 text-base font-medium transition-colors',
                pathname === '/mensajes'
                  ? 'bg-primary/10 text-primary'
                  : 'text-foreground hover:bg-muted'
              )}
            >
              <MessageSquare className="h-4 w-4" />
              Mensajes
            </Link>
            <Link
              href="/cuenta/notificaciones"
              className={cn(
                'flex items-center gap-3 rounded-xl px-4 py-3 text-base font-medium transition-colors',
                pathname === '/cuenta/notificaciones'
                  ? 'bg-primary/10 text-primary'
                  : 'text-foreground hover:bg-muted'
              )}
            >
              <Bell className="h-4 w-4" />
              Notificaciones
              {notifications.filter(n => !n.read).length > 0 && (
                <span className="ml-auto flex h-5 min-w-5 items-center justify-center rounded-full bg-[#00A870] px-1.5 text-xs font-bold text-white">
                  {notifications.filter(n => !n.read).length}
                </span>
              )}
            </Link>
          </>
        )}

        {/* Divider */}
        <div className="border-border my-3 border-t" />

        {/* Auth Section */}
        {!isAuthenticated && (
          <div className="grid grid-cols-2 gap-3 pt-2">
            <Link
              href="/login"
              className="border-border text-foreground hover:bg-muted flex items-center justify-center rounded-xl border px-4 py-3 font-medium"
            >
              Iniciar Sesión
            </Link>
            <Link
              href="/registro"
              className="bg-foreground text-background hover:bg-foreground/90 flex items-center justify-center rounded-xl px-4 py-3 font-medium"
            >
              Registrarse
            </Link>
          </div>
        )}

        {isAuthenticated && (
          <button
            onClick={onLogout}
            className="text-destructive hover:bg-destructive/10 flex w-full items-center justify-center gap-2 rounded-xl px-4 py-3 font-medium transition-colors"
          >
            <LogOut className="h-5 w-5" />
            Cerrar Sesión
          </button>
        )}
      </div>
    </div>
  ) : null;
}

// =============================================================================
// MAIN NAVBAR COMPONENT
// =============================================================================

export function Navbar() {
  const pathname = usePathname();
  const router = useRouter();
  const [isMobileMenuOpen, setIsMobileMenuOpen] = React.useState(false);
  const [isScrolled, setIsScrolled] = React.useState(false);

  // Get auth state from context
  const { isAuthenticated, user, logout } = useAuth();

  // Handle logout
  const handleLogout = async () => {
    await logout();
    router.push('/');
  };

  // Handle scroll effect
  React.useEffect(() => {
    const handleScroll = () => {
      setIsScrolled(window.scrollY > 10);
    };

    window.addEventListener('scroll', handleScroll, { passive: true });
    return () => window.removeEventListener('scroll', handleScroll);
  }, []);

  // Close mobile menu on route change
  React.useEffect(() => {
    setIsMobileMenuOpen(false);
  }, [pathname]);

  // Prevent body scroll when mobile menu is open
  React.useEffect(() => {
    if (isMobileMenuOpen) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = '';
    }
    return () => {
      document.body.style.overflow = '';
    };
  }, [isMobileMenuOpen]);

  return (
    <header
      className={cn(
        'sticky top-0 z-50 w-full transition-all duration-300',
        isScrolled
          ? 'border-border bg-background/95 border-b shadow-sm backdrop-blur-md'
          : 'bg-background'
      )}
    >
      <nav className="mx-auto flex h-16 max-w-7xl items-center justify-between gap-4 px-4 sm:px-6 lg:h-18 lg:px-8">
        {/* Left - Logo + Navigation together */}
        <div className="flex min-w-0 items-center gap-8">
          <Logo />
          <DesktopNav pathname={pathname} isAuthenticated={isAuthenticated} user={user} />
        </div>

        {/* Right - Actions */}
        <RightActions
          isAuthenticated={isAuthenticated}
          user={user}
          onLogout={handleLogout}
          onMobileMenuToggle={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
          isMobileMenuOpen={isMobileMenuOpen}
        />
      </nav>

      <MobileMenu
        isOpen={isMobileMenuOpen}
        pathname={pathname}
        isAuthenticated={isAuthenticated}
        user={user}
        onLogout={handleLogout}
      />
    </header>
  );
}
