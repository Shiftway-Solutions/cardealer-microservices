/**
 * Dealer Layout
 *
 * Layout for dealer portal with sidebar navigation.
 * Protected by AuthGuard - requires dealer or admin account type.
 *
 * Security:
 * - Middleware handles server-side route protection (first layer)
 * - AuthGuard provides client-side defense-in-depth (second layer)
 * - Only dealer and admin account types can access
 */

'use client';

import * as React from 'react';
import Link from 'next/link';
import { useRouter, usePathname } from 'next/navigation';
import {
  LayoutDashboard,
  Car,
  Users,
  BarChart3,
  Calendar,
  MessageSquare,
  MapPin,
  FileText,
  CreditCard,
  Settings,
  LogOut,
  Building,
  TrendingUp,
  Bell,
  HelpCircle,
  Menu,
  Star,
  Megaphone,
  Crown,
  PlusCircle,
  Bot,
  Upload,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { useAuth } from '@/hooks/use-auth';
import { AuthGuard } from '@/components/auth/auth-guard';
import { usePlanAccess } from '@/hooks/use-plan-access';
import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/components/ui/sheet';

// =============================================================================
// NAVIGATION LINKS
// =============================================================================

const mainLinks = [
  { href: '/dealer', label: 'Dashboard', icon: LayoutDashboard },
  { href: '/dealer/inventario', label: 'Inventario', icon: Car },
  { href: '/dealer/inventario/importar', label: 'Importar Masivo', icon: Upload },
  { href: '/dealer/leads', label: 'Leads', icon: Users },
  { href: '/dealer/publicidad', label: 'Publicidad', icon: Megaphone },
  { href: '/dealer/analytics', label: 'Analytics', icon: BarChart3 },
  { href: '/dealer/citas', label: 'Citas', icon: Calendar },
  { href: '/dealer/mensajes', label: 'Mensajes', icon: MessageSquare },
  { href: '/dealer/resenas', label: 'Reseñas', icon: Star },
  { href: '/dealer/chatbot', label: 'ChatAgent', icon: Bot },
];

const managementLinks = [
  { href: '/dealer/empleados', label: 'Empleados', icon: Users },
  { href: '/dealer/ubicaciones', label: 'Ubicaciones', icon: MapPin },
  { href: '/dealer/pricing', label: 'Pricing IA', icon: TrendingUp },
  { href: '/dealer/reportes', label: 'Reportes', icon: FileText },
];

const settingsLinks = [
  { href: '/dealer/perfil', label: 'Perfil Dealer', icon: Building },
  { href: '/dealer/documentos', label: 'Documentos', icon: FileText },
  { href: '/dealer/facturacion', label: 'Facturación', icon: CreditCard },
  { href: '/dealer/suscripcion', label: 'Suscripción', icon: CreditCard },
  { href: '/dealer/configuracion', label: 'Configuración', icon: Settings },
];

// =============================================================================
// MAIN LAYOUT (with AuthGuard wrapper)
// =============================================================================

export default function DealerLayout({ children }: { children: React.ReactNode }) {
  return (
    <AuthGuard accountTypes={['dealer', 'dealer_employee', 'admin']} forbiddenRedirect="/403">
      <DealerLayoutContent>{children}</DealerLayoutContent>
    </AuthGuard>
  );
}

// =============================================================================
// LAYOUT CONTENT
// =============================================================================

function DealerLayoutContent({ children }: { children: React.ReactNode }) {
  const { user, logout } = useAuth();
  const { currentPlan } = usePlanAccess();
  const router = useRouter();
  const pathname = usePathname();
  const [isMobileMenuOpen, setIsMobileMenuOpen] = React.useState(false);

  const handleLogout = async () => {
    await logout();
    router.push('/login');
  };

  const isActivePath = (href: string) => {
    if (href === '/dealer') return pathname === '/dealer';
    return pathname.startsWith(href);
  };

  // Mobile bottom nav items (most important sections)
  const mobileNavItems = [
    { href: '/dealer', label: 'Dashboard', icon: LayoutDashboard },
    { href: '/dealer/inventario', label: 'Inventario', icon: Car },
    { href: '/publicar', label: 'Publicar', icon: PlusCircle, highlight: true },
    { href: '/dealer/leads', label: 'Leads', icon: Users },
    { href: '/dealer/analytics', label: 'Analytics', icon: BarChart3 },
  ];

  return (
    <div className="bg-muted min-h-screen pb-16 lg:pb-0">
      {/* Header */}
      <header className="bg-card border-border sticky top-0 z-40 border-b">
        <div className="flex h-16 items-center justify-between px-4 lg:px-6">
          <div className="flex items-center gap-4">
            {/* Mobile hamburger menu */}
            <button
              type="button"
              onClick={() => setIsMobileMenuOpen(true)}
              className="hover:bg-muted flex h-11 w-11 items-center justify-center rounded-lg lg:hidden"
              aria-label="Abrir menú de navegación"
            >
              <Menu className="h-5 w-5" />
            </button>
            <Link href="/dealer" className="flex items-center gap-2">
              <div className="bg-primary flex h-8 w-8 items-center justify-center rounded-lg">
                <span className="text-primary-foreground text-lg font-bold">O</span>
              </div>
              <div className="hidden sm:block">
                <span className="text-foreground text-xl font-bold">OKLA</span>
                <span className="text-muted-foreground ml-2 text-sm">Dealer Portal</span>
              </div>
            </Link>
          </div>

          <div className="flex items-center gap-2">
            <Link
              href="/dealer/mensajes"
              className="text-muted-foreground hover:text-foreground hover:bg-muted relative flex h-11 w-11 items-center justify-center rounded-lg"
              aria-label="Mensajes"
            >
              <MessageSquare className="h-5 w-5" />
            </Link>
            <Link
              href="/dealer/notificaciones"
              className="text-muted-foreground hover:text-foreground hover:bg-muted relative flex h-11 w-11 items-center justify-center rounded-lg"
              aria-label="Notificaciones"
            >
              <Bell className="h-5 w-5" />
            </Link>
            <Link
              href="/ayuda"
              className="text-muted-foreground hover:text-foreground hover:bg-muted hidden h-11 w-11 items-center justify-center rounded-lg sm:flex"
              aria-label="Ayuda"
            >
              <HelpCircle className="h-5 w-5" />
            </Link>
            <div className="flex items-center gap-2 border-l pl-3">
              <div className="bg-primary/10 flex h-8 w-8 items-center justify-center rounded-full">
                <Building className="text-primary h-4 w-4" />
              </div>
              <div className="hidden sm:block">
                <p className="text-sm font-medium">{user?.firstName || 'Dealer'}</p>
                <p className="text-muted-foreground text-xs">{user?.email}</p>
              </div>
            </div>
          </div>
        </div>
      </header>

      <div className="flex">
        {/* Desktop Sidebar */}
        <aside className="bg-card sticky top-16 hidden h-[calc(100vh-4rem)] w-64 flex-col border-r lg:flex">
          <nav
            className="flex-1 space-y-1 overflow-y-auto p-4"
            aria-label="Navegación del portal dealer"
          >
            {/* Upgrade CTA for LIBRE dealers */}
            {currentPlan === 'libre' && (
              <Link
                href="/cuenta/upgrade?plan=visible&type=dealer"
                className="mb-3 flex items-center gap-3 rounded-lg bg-gradient-to-r from-purple-600 to-indigo-600 px-3 py-2.5 text-sm font-medium text-white shadow-sm transition-all hover:shadow-md hover:brightness-110"
              >
                <Crown className="h-4 w-4 text-yellow-300" />
                Mejorar Plan
              </Link>
            )}
            {mainLinks.map(link => {
              const Icon = link.icon;
              return (
                <Link
                  key={link.href}
                  href={link.href}
                  className={cn(
                    'flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm transition-colors',
                    isActivePath(link.href)
                      ? 'bg-primary/10 text-primary font-medium'
                      : 'text-foreground hover:bg-primary/5 hover:text-primary'
                  )}
                >
                  <Icon className="h-4 w-4" />
                  {link.label}
                </Link>
              );
            })}

            <div className="border-border mt-4 border-t pt-4">
              <p className="text-muted-foreground mb-2 px-3 text-xs font-semibold tracking-wider uppercase">
                Gestión
              </p>
              {managementLinks.map(link => {
                const Icon = link.icon;
                return (
                  <Link
                    key={link.href}
                    href={link.href}
                    className={cn(
                      'flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm transition-colors',
                      isActivePath(link.href)
                        ? 'bg-primary/10 text-primary font-medium'
                        : 'text-foreground hover:bg-primary/5 hover:text-primary'
                    )}
                  >
                    <Icon className="h-4 w-4" />
                    {link.label}
                  </Link>
                );
              })}
            </div>

            <div className="border-border mt-4 border-t pt-4">
              <p className="text-muted-foreground mb-2 px-3 text-xs font-semibold tracking-wider uppercase">
                Configuración
              </p>
              {settingsLinks.map(link => {
                const Icon = link.icon;
                return (
                  <Link
                    key={link.href}
                    href={link.href}
                    className={cn(
                      'flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm transition-colors',
                      isActivePath(link.href)
                        ? 'bg-primary/10 text-primary font-medium'
                        : 'text-foreground hover:bg-primary/5 hover:text-primary'
                    )}
                  >
                    <Icon className="h-4 w-4" />
                    {link.label}
                  </Link>
                );
              })}
            </div>
          </nav>

          <div className="border-border border-t p-4">
            <button
              type="button"
              onClick={handleLogout}
              className="text-destructive hover:bg-destructive/10 flex w-full items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors"
            >
              <LogOut className="h-4 w-4" />
              Cerrar Sesión
            </button>
          </div>
        </aside>

        {/* Mobile Sidebar Sheet */}
        <Sheet open={isMobileMenuOpen} onOpenChange={setIsMobileMenuOpen}>
          <SheetContent side="left" className="w-[280px] p-0">
            <SheetHeader className="border-b p-4">
              <SheetTitle className="flex items-center gap-2">
                <div className="bg-primary flex h-8 w-8 items-center justify-center rounded-lg">
                  <span className="text-primary-foreground text-lg font-bold">O</span>
                </div>
                OKLA Dealer
              </SheetTitle>
            </SheetHeader>
            <nav
              className="flex-1 space-y-1 overflow-y-auto p-4"
              aria-label="Menú de navegación móvil"
            >
              {/* Upgrade CTA for LIBRE dealers (mobile) */}
              {currentPlan === 'libre' && (
                <Link
                  href="/cuenta/upgrade?plan=visible&type=dealer"
                  onClick={() => setIsMobileMenuOpen(false)}
                  className="mb-3 flex items-center gap-3 rounded-lg bg-gradient-to-r from-purple-600 to-indigo-600 px-3 py-3 text-sm font-medium text-white shadow-sm transition-all hover:shadow-md hover:brightness-110"
                >
                  <Crown className="h-4 w-4 text-yellow-300" />
                  Mejorar Plan
                </Link>
              )}
              {mainLinks.map(link => {
                const Icon = link.icon;
                return (
                  <Link
                    key={link.href}
                    href={link.href}
                    onClick={() => setIsMobileMenuOpen(false)}
                    className={cn(
                      'flex items-center gap-3 rounded-lg px-3 py-3 text-sm transition-colors',
                      isActivePath(link.href)
                        ? 'bg-primary/10 text-primary font-medium'
                        : 'text-foreground hover:bg-primary/5'
                    )}
                  >
                    <Icon className="h-4 w-4" />
                    {link.label}
                  </Link>
                );
              })}

              <div className="border-border mt-4 border-t pt-4">
                <p className="text-muted-foreground mb-2 px-3 text-xs font-semibold tracking-wider uppercase">
                  Gestión
                </p>
                {managementLinks.map(link => {
                  const Icon = link.icon;
                  return (
                    <Link
                      key={link.href}
                      href={link.href}
                      onClick={() => setIsMobileMenuOpen(false)}
                      className={cn(
                        'flex items-center gap-3 rounded-lg px-3 py-3 text-sm transition-colors',
                        isActivePath(link.href)
                          ? 'bg-primary/10 text-primary font-medium'
                          : 'text-foreground hover:bg-primary/5'
                      )}
                    >
                      <Icon className="h-4 w-4" />
                      {link.label}
                    </Link>
                  );
                })}
              </div>

              <div className="border-border mt-4 border-t pt-4">
                <p className="text-muted-foreground mb-2 px-3 text-xs font-semibold tracking-wider uppercase">
                  Configuración
                </p>
                {settingsLinks.map(link => {
                  const Icon = link.icon;
                  return (
                    <Link
                      key={link.href}
                      href={link.href}
                      onClick={() => setIsMobileMenuOpen(false)}
                      className={cn(
                        'flex items-center gap-3 rounded-lg px-3 py-3 text-sm transition-colors',
                        isActivePath(link.href)
                          ? 'bg-primary/10 text-primary font-medium'
                          : 'text-foreground hover:bg-primary/5'
                      )}
                    >
                      <Icon className="h-4 w-4" />
                      {link.label}
                    </Link>
                  );
                })}
              </div>

              <div className="border-border mt-4 border-t pt-4">
                <button
                  type="button"
                  onClick={handleLogout}
                  className="text-destructive hover:bg-destructive/10 flex w-full items-center gap-3 rounded-lg px-3 py-3 text-sm transition-colors"
                >
                  <LogOut className="h-4 w-4" />
                  Cerrar Sesión
                </button>
              </div>
            </nav>
          </SheetContent>
        </Sheet>

        {/* Main Content */}
        <main className="flex-1 overflow-y-auto p-4 lg:p-6">{children}</main>
      </div>

      {/* Mobile Bottom Navigation */}
      <nav
        className="bg-card border-border fixed right-0 bottom-0 left-0 z-40 border-t lg:hidden"
        aria-label="Navegación rápida"
      >
        <div className="flex justify-around">
          {mobileNavItems.map(item => {
            const Icon = item.icon;
            const active = isActivePath(item.href);
            const isHighlight = 'highlight' in item && item.highlight;
            return (
              <Link
                key={item.href}
                href={item.href}
                className={cn(
                  'flex min-h-[56px] flex-1 flex-col items-center justify-center gap-1 text-xs transition-colors',
                  isHighlight
                    ? 'text-primary font-semibold'
                    : active
                      ? 'text-primary'
                      : 'text-muted-foreground hover:text-foreground'
                )}
                aria-current={active ? 'page' : undefined}
              >
                <Icon
                  className={cn(
                    'h-5 w-5',
                    isHighlight && 'bg-primary text-primary-foreground h-8 w-8 rounded-full p-1.5'
                  )}
                />
                <span>{item.label}</span>
              </Link>
            );
          })}
          <button
            type="button"
            onClick={() => setIsMobileMenuOpen(true)}
            className="text-muted-foreground hover:text-foreground flex min-h-[56px] flex-1 flex-col items-center justify-center gap-1 text-xs"
            aria-label="Más opciones"
          >
            <Menu className="h-5 w-5" />
            <span>Más</span>
          </button>
        </div>
      </nav>
    </div>
  );
}
