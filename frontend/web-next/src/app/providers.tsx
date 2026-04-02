'use client';

import * as React from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { TooltipProvider } from '@/components/ui/tooltip';
import { AuthProvider } from '@/hooks/use-auth';
import { SiteConfigProvider } from '@/providers/site-config-provider';
import { TrackingProvider } from '@/components/providers/tracking-provider';

// Lazy-load devtools — only in development, excluded from production bundle
const ReactQueryDevtools =
  process.env.NODE_ENV === 'development'
    ? React.lazy(() =>
        import('@tanstack/react-query-devtools').then(mod => ({
          default: mod.ReactQueryDevtools,
        }))
      )
    : () => null;

const DEVTOOLS_HOSTS = new Set(['localhost', '127.0.0.1', 'okla.local']);

function makeQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        // With SSR, we usually want to set some default staleTime
        // above 0 to avoid refetching immediately on the client
        staleTime: 60 * 1000, // 1 minute
        refetchOnWindowFocus: false,
        retry: (failureCount, error) => {
          // Don't retry on 4xx errors
          if (error && 'status' in error) {
            const status = (error as { status: number }).status;
            if (status >= 400 && status < 500) return false;
          }
          return failureCount < 3;
        },
      },
      mutations: {
        retry: false,
      },
    },
  });
}

let browserQueryClient: QueryClient | undefined = undefined;

function getQueryClient() {
  if (typeof window === 'undefined') {
    // Server: always make a new query client
    return makeQueryClient();
  } else {
    // Browser: make a new query client if we don't already have one
    if (!browserQueryClient) browserQueryClient = makeQueryClient();
    return browserQueryClient;
  }
}

interface ProvidersProps {
  children: React.ReactNode;
}

export function Providers({ children }: ProvidersProps) {
  // NOTE: Avoid useState when initializing the query client if you don't
  //       have a suspense boundary between this and the code that may
  //       suspend because React will throw away the client on the initial
  //       render if it suspends and there is no boundary
  const queryClient = getQueryClient();
  const [showDevtools, setShowDevtools] = React.useState(false);

  React.useEffect(() => {
    if (process.env.NODE_ENV !== 'development') return;

    const hostname = window.location.hostname;
    setShowDevtools(DEVTOOLS_HOSTS.has(hostname));
  }, []);

  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <SiteConfigProvider>
          <TrackingProvider>
            <TooltipProvider delayDuration={300}>{children}</TooltipProvider>
          </TrackingProvider>
        </SiteConfigProvider>
      </AuthProvider>
      {showDevtools ? (
        <React.Suspense fallback={null}>
          <ReactQueryDevtools initialIsOpen={false} />
        </React.Suspense>
      ) : null}
    </QueryClientProvider>
  );
}
