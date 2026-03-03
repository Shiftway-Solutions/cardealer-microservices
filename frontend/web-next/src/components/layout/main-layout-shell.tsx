'use client';

import { usePathname } from 'next/navigation';
import dynamic from 'next/dynamic';
import { Footer } from '@/components/layout/footer';
import { SupportAgentWidget } from '@/components/chat/SupportAgentWidget';

// Lazy-load Navbar — it's a heavy component (1000+ lines, icons, popovers, auth state)
// Skeleton is shown immediately while the full Navbar loads
const Navbar = dynamic(
  () => import('@/components/layout/navbar').then(mod => ({ default: mod.Navbar })),
  {
    ssr: true,
    loading: () => (
      <header className="bg-background sticky top-0 z-50 h-16 w-full border-b lg:h-18">
        <nav className="mx-auto flex h-full max-w-7xl items-center justify-between px-4 sm:px-6 lg:px-8">
          <div className="flex items-center gap-2.5">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-[#00A870] to-[#009663]">
              <span className="text-xl font-bold text-white">O</span>
            </div>
            <span className="text-foreground text-2xl font-bold tracking-tight">OKLA</span>
          </div>
        </nav>
      </header>
    ),
  }
);

/**
 * MainLayoutShell
 *
 * Conditionally renders the global Navbar and Footer.
 * Hides them for routes that have their own layout (e.g., dealer portal).
 */
export function MainLayoutShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();

  // Dealer portal has its own header, sidebar, and layout — skip global Navbar/Footer
  const isDealerPortal = pathname.startsWith('/dealer');

  // Support chatbot should appear on ALL pages (except dealer portal which has its own layout)
  // Vehicle pages also get the support chatbot — it coexists with the vehicle-specific chatbot
  const hideSupportWidget = false;

  if (isDealerPortal) {
    return <>{children}</>;
  }

  return (
    <div className="flex min-h-screen flex-col">
      <a
        href="#main-content"
        className="bg-primary text-primary-foreground fixed top-0 left-1/2 z-[100] -translate-x-1/2 -translate-y-full rounded-b-lg px-4 py-2 text-sm font-medium transition-transform focus:translate-y-0"
      >
        Saltar al contenido principal
      </a>
      <Navbar />
      <main id="main-content" className="flex-1">
        {children}
      </main>
      <Footer />
      {!hideSupportWidget && <SupportAgentWidget />}
    </div>
  );
}
