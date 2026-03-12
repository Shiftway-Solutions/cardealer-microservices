/**
 * Account Layout
 *
 * Layout for authenticated user account pages with sidebar navigation.
 *
 * Security:
 * - Middleware handles server-side route protection (first layer)
 * - AuthGuard provides client-side defense-in-depth (second layer)
 * - Sidebar navigation is dynamically generated based on accountType
 */

'use client';

import Image from 'next/image';
import * as React from 'react';
import Link from 'next/link';
import { useRouter, usePathname } from 'next/navigation';
import { LogOut, User, Lock } from 'lucide-react';
import { Separator } from '@/components/ui/separator';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { useAuth } from '@/hooks/use-auth';
import { AuthGuard } from '@/components/auth/auth-guard';
import { getNavigationForUser, getUserBadge } from '@/config/navigation';
import { usePlanAccess } from '@/hooks/use-plan-access';

interface AccountLayoutProps {
  children: React.ReactNode;
}

export default function AccountLayout({ children }: AccountLayoutProps) {
  return (
    <AuthGuard loginPath="/login">
      <AccountLayoutContent>{children}</AccountLayoutContent>
    </AuthGuard>
  );
}

function AccountLayoutContent({ children }: AccountLayoutProps) {
  const router = useRouter();
  const pathname = usePathname();
  const { user, logout } = useAuth();
  const { currentPlan } = usePlanAccess();

  // Redirect accounts to their own portal:
  // - Dealers → /dealer/dashboard (they have their own portal)
  // - Admins at /cuenta root → /admin (profile/security sub-pages still accessible
  //   via the admin panel header dropdown links like /cuenta/perfil)
  React.useEffect(() => {
    if (!user) return;
    if (user.accountType === 'dealer' || user.accountType === 'dealer_employee') {
      router.replace('/dealer/dashboard');
    }
    if (
      (user.accountType === 'admin' || user.accountType === 'platform_employee') &&
      pathname === '/cuenta'
    ) {
      router.replace('/admin');
    }
  }, [user, router, pathname]);

  // AuthGuard guarantees user is defined at this point
  if (!user) return null;

  const handleLogout = async () => {
    await logout();
    router.push('/login');
  };

  // Obtener badge del usuario
  const userBadge = getUserBadge(user);

  return (
    <div className="bg-muted/50 min-h-screen">
      <div className="container mx-auto px-4 py-6">
        <div className="flex flex-col gap-6 lg:flex-row">
          {/* Sidebar */}
          <aside className="w-full flex-shrink-0 lg:w-72">
            <nav className="border-border bg-card sticky top-20 overflow-hidden rounded-lg border shadow-sm">
              {/* User info header */}
              <div className="border-border border-b p-4">
                <div className="flex items-center gap-3">
                  <div className="bg-primary/10 flex h-12 w-12 flex-shrink-0 items-center justify-center overflow-hidden rounded-full">
                    {user.avatarUrl ? (
                      <Image
                        src={user.avatarUrl}
                        alt=""
                        width={48}
                        height={48}
                        className="h-12 w-12 rounded-full object-cover"
                        onError={e => {
                          e.currentTarget.style.display = 'none';
                        }}
                      />
                    ) : null}
                    <User className="text-primary h-6 w-6" />
                  </div>
                  <div className="min-w-0 flex-1 overflow-hidden">
                    <div className="flex items-center gap-2">
                      <p className="text-foreground truncate font-semibold whitespace-nowrap">
                        {user.firstName?.split(' ')[0] || 'Usuario'}
                      </p>
                      {userBadge && (
                        <span
                          className={`rounded-full px-2 py-0.5 text-xs font-medium ${userBadge.color}`}
                        >
                          {userBadge.label}
                        </span>
                      )}
                    </div>
                    <p className="text-muted-foreground truncate text-sm whitespace-nowrap">
                      {user.email}
                    </p>
                  </div>
                </div>
              </div>

              {/* Navigation - Menú según tipo de usuario */}
              <div className="p-2">
                {getNavigationForUser(user).map((section, index) => (
                  <div key={section.title}>
                    {index > 0 && <Separator className="my-2" />}
                    <p className="text-muted-foreground px-3 py-2 text-xs font-medium tracking-wider uppercase">
                      {section.title}
                    </p>
                    {section.items.map(item => {
                      const isLocked = item.requiredPlan && currentPlan === 'libre';
                      const navLink = (
                        <Link
                          key={item.href}
                          href={item.href}
                          className={`text-foreground hover:bg-muted hover:text-foreground flex items-center gap-3 rounded-lg px-3 py-2 transition-colors ${isLocked ? 'opacity-60' : ''}`}
                        >
                          <item.icon className="h-5 w-5 flex-shrink-0" />
                          <span className="truncate">{item.label}</span>
                          {isLocked && (
                            <Lock className="ml-auto h-3.5 w-3.5 flex-shrink-0 text-amber-500" />
                          )}
                          {!isLocked && item.badge && (
                            <span className="bg-primary/10 text-primary ml-auto rounded-full px-2 py-0.5 text-xs font-medium">
                              {item.badge}
                            </span>
                          )}
                        </Link>
                      );

                      if (isLocked) {
                        return (
                          <TooltipProvider key={item.href}>
                            <Tooltip>
                              <TooltipTrigger asChild>{navLink}</TooltipTrigger>
                              <TooltipContent side="right">
                                <p className="text-xs">Requiere plan {item.requiredPlan}</p>
                              </TooltipContent>
                            </Tooltip>
                          </TooltipProvider>
                        );
                      }

                      return navLink;
                    })}
                  </div>
                ))}

                <Separator className="my-2" />

                {/* Logout */}
                <button
                  type="button"
                  onClick={handleLogout}
                  className="flex w-full items-center gap-3 rounded-lg px-3 py-2 text-red-600 transition-colors hover:bg-red-50"
                >
                  <LogOut className="h-5 w-5" />
                  <span>Cerrar Sesión</span>
                </button>
              </div>
            </nav>
          </aside>

          {/* Main content */}
          <main className="min-w-0 flex-1">{children}</main>
        </div>
      </div>
    </div>
  );
}
