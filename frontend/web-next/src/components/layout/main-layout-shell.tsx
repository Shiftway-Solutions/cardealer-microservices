'use client';

import { usePathname } from 'next/navigation';
import dynamic from 'next/dynamic';
import { Footer } from '@/components/layout/footer';
import { Navbar } from '@/components/layout/navbar';

// Lazy-load SupportAgentWidget — non-critical, heavy component (LLM, WebSocket, chat UI)
// Deferring this improves INP since it's not needed immediately
const SupportAgentWidget = dynamic(
  () =>
    import('@/components/chat/SupportAgentWidget').then(mod => ({
      default: mod.SupportAgentWidget,
    })),
  { ssr: false }
);

// Lazy-load ComparisonBar — only rendered when vehicles are selected for comparison
const ComparisonBar = dynamic(
  () =>
    import('@/components/comparison/comparison-bar').then(mod => ({
      default: mod.ComparisonBar,
    })),
  { ssr: false }
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
  // Only exclude /dealer/* (with trailing /) to avoid matching /dealers landing page
  const isDealerPortal = pathname === '/dealer' || pathname.startsWith('/dealer/');

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
      <ComparisonBar />
      {!hideSupportWidget && <SupportAgentWidget />}
    </div>
  );
}
