# ⚡ Performance Optimization

> **Tiempo estimado:** 45 minutos
> **Prerrequisitos:** Proyecto Next.js configurado
> **Última actualización:** Enero 2026

---

## 📋 OBJETIVO

Optimizar performance del frontend para cumplir:

- **FCP (First Contentful Paint):** < 1.5s
- **LCP (Largest Contentful Paint):** < 2.5s
- **CLS (Cumulative Layout Shift):** < 0.1
- **Bundle Size:** < 500 KB (initial load)
- **TTI (Time to Interactive):** < 3.5s

---

## 🎯 MÉTRICAS TARGET

| Métrica              | Target   | Crítico  |
| -------------------- | -------- | -------- |
| **FCP**              | < 1.5s   | < 2.0s   |
| **LCP**              | < 2.5s   | < 4.0s   |
| **FID**              | < 100ms  | < 300ms  |
| **CLS**              | < 0.1    | < 0.25   |
| **TTFB**             | < 600ms  | < 800ms  |
| **Bundle (initial)** | < 300 KB | < 500 KB |

---

## 🔧 PASO 1: Next.js Config Optimizada

```typescript
// filepath: next.config.mjs
import bundleAnalyzer from "@next/bundle-analyzer";

const withBundleAnalyzer = bundleAnalyzer({
  enabled: process.env.ANALYZE === "true",
});

/** @type {import('next').NextConfig} */
const nextConfig = {
  // =====================================
  // OPTIMIZACIÓN DE IMÁGENES
  // =====================================
  images: {
    // Dominios externos permitidos
    remotePatterns: [
      {
        protocol: "https",
        hostname: "api.okla.com.do",
      },
      {
        protocol: "https",
        hostname: "cdn.okla.com.do",
      },
      {
        protocol: "https",
        hostname: "*.digitaloceanspaces.com",
      },
    ],
    // Formatos modernos
    formats: ["image/avif", "image/webp"],
    // Tamaños de dispositivos
    deviceSizes: [640, 750, 828, 1080, 1200, 1920, 2048],
    // Tamaños de imágenes
    imageSizes: [16, 32, 48, 64, 96, 128, 256, 384],
    // Minimizar memory footprint
    minimumCacheTTL: 60 * 60 * 24 * 30, // 30 días
  },

  // =====================================
  // OPTIMIZACIÓN DE COMPILACIÓN
  // =====================================
  compiler: {
    // Remover console.log en producción
    removeConsole:
      process.env.NODE_ENV === "production"
        ? {
            exclude: ["error", "warn"],
          }
        : false,
  },

  // =====================================
  // EXPERIMENTAL FEATURES
  // =====================================
  experimental: {
    // Optimized package imports
    optimizePackageImports: [
      "lucide-react",
      "@radix-ui/react-icons",
      "date-fns",
      "framer-motion",
    ],
    // Turbopack para desarrollo más rápido
    turbo: {
      rules: {
        "*.svg": {
          loaders: ["@svgr/webpack"],
          as: "*.js",
        },
      },
    },
  },

  // =====================================
  // HEADERS DE CACHÉ
  // =====================================
  async headers() {
    return [
      {
        // Assets estáticos - cache largo
        source: "/static/:path*",
        headers: [
          {
            key: "Cache-Control",
            value: "public, max-age=31536000, immutable",
          },
        ],
      },
      {
        // Imágenes - cache largo
        source: "/_next/image/:path*",
        headers: [
          {
            key: "Cache-Control",
            value: "public, max-age=31536000, immutable",
          },
        ],
      },
      {
        // API responses - cache corto
        source: "/api/:path*",
        headers: [
          {
            key: "Cache-Control",
            value: "public, s-maxage=60, stale-while-revalidate=300",
          },
        ],
      },
    ];
  },

  // =====================================
  // WEBPACK OPTIMIZATIONS
  // =====================================
  webpack: (config, { isServer }) => {
    // Tree shaking más agresivo
    config.optimization = {
      ...config.optimization,
      sideEffects: true,
      usedExports: true,
    };

    // Ignorar source maps en producción
    if (!isServer && process.env.NODE_ENV === "production") {
      config.devtool = false;
    }

    return config;
  },
};

export default withBundleAnalyzer(nextConfig);
```

---

## 🔧 PASO 2: Code Splitting Estratégico

### Dynamic Imports para Componentes Pesados

```typescript
// filepath: src/lib/dynamic-imports.ts
import dynamic from 'next/dynamic';

// Componentes que no se necesitan en el render inicial
export const VehicleGalleryLightbox = dynamic(
  () => import('@/components/vehicles/VehicleGalleryLightbox'),
  {
    loading: () => <div className="aspect-[4/3] bg-gray-100 animate-pulse rounded-lg" />,
    ssr: false, // No necesita SSR
  }
);

export const Vehicle360Viewer = dynamic(
  () => import('@/components/vehicles/Vehicle360Viewer'),
  {
    loading: () => <div className="aspect-video bg-gray-100 animate-pulse rounded-lg" />,
    ssr: false,
  }
);

export const MapComponent = dynamic(
  () => import('@/components/maps/DealerLocationMap'),
  {
    loading: () => <div className="aspect-video bg-gray-100 animate-pulse rounded-lg" />,
    ssr: false,
  }
);

export const ChatWidget = dynamic(
  () => import('@/components/chat/ChatWidget'),
  {
    ssr: false,
  }
);

export const RichTextEditor = dynamic(
  () => import('@/components/forms/RichTextEditor'),
  {
    loading: () => <div className="h-40 bg-gray-100 animate-pulse rounded-lg" />,
    ssr: false,
  }
);

// Charts para dashboards
export const AnalyticsCharts = dynamic(
  () => import('@/components/dealer/AnalyticsCharts'),
  {
    loading: () => <div className="h-80 bg-gray-100 animate-pulse rounded-lg" />,
    ssr: false,
  }
);
```

### Route-based Code Splitting

```typescript
// filepath: src/app/(dealer)/layout.tsx
// El layout de dealer se carga solo cuando se accede a rutas /dealer/*
// Next.js hace esto automáticamente con App Router

export default function DealerLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div className="dealer-layout">
      {/* Lazy load sidebar solo para dealers */}
      <Suspense fallback={<SidebarSkeleton />}>
        <DealerSidebar />
      </Suspense>
      {children}
    </div>
  );
}
```

---

## 🔧 PASO 3: Optimización de Imágenes

### Componente Image Optimizado

```typescript
// filepath: src/components/ui/OptimizedImage.tsx
'use client';

import * as React from 'react';
import Image, { ImageProps } from 'next/image';
import { cn } from '@/lib/utils';

interface OptimizedImageProps extends Omit<ImageProps, 'onLoad'> {
  fallback?: string;
  aspectRatio?: '1/1' | '4/3' | '16/9' | '3/2';
}

export function OptimizedImage({
  src,
  alt,
  fallback = '/images/placeholder.jpg',
  aspectRatio = '4/3',
  className,
  ...props
}: OptimizedImageProps) {
  const [imgSrc, setImgSrc] = React.useState(src);
  const [isLoading, setIsLoading] = React.useState(true);

  const aspectRatioClasses = {
    '1/1': 'aspect-square',
    '4/3': 'aspect-[4/3]',
    '16/9': 'aspect-video',
    '3/2': 'aspect-[3/2]',
  };

  return (
    <div className={cn('relative overflow-hidden', aspectRatioClasses[aspectRatio])}>
      <Image
        src={imgSrc}
        alt={alt}
        fill
        sizes={props.sizes || '(max-width: 768px) 100vw, (max-width: 1200px) 50vw, 33vw'}
        className={cn(
          'object-cover transition-opacity duration-300',
          isLoading ? 'opacity-0' : 'opacity-100',
          className
        )}
        onLoad={() => setIsLoading(false)}
        onError={() => {
          setImgSrc(fallback);
          setIsLoading(false);
        }}
        {...props}
      />
      {isLoading && (
        <div className="absolute inset-0 bg-gray-200 animate-pulse" />
      )}
    </div>
  );
}
```

### Lazy Loading de Imágenes en Listas

```typescript
// filepath: src/components/vehicles/VehicleGrid.tsx
'use client';

import { useInView } from 'react-intersection-observer';
import { VehicleCard } from './VehicleCard';

export function VehicleGrid({ vehicles }) {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
      {vehicles.map((vehicle, index) => (
        <LazyVehicleCard
          key={vehicle.id}
          vehicle={vehicle}
          // Primeros 4 cargan inmediatamente (above the fold)
          priority={index < 4}
        />
      ))}
    </div>
  );
}

function LazyVehicleCard({ vehicle, priority }) {
  const { ref, inView } = useInView({
    triggerOnce: true,
    threshold: 0.1,
    rootMargin: '100px', // Pre-cargar antes de entrar al viewport
  });

  return (
    <div ref={ref}>
      {(inView || priority) ? (
        <VehicleCard vehicle={vehicle} priority={priority} />
      ) : (
        <VehicleCardSkeleton />
      )}
    </div>
  );
}
```

---

## 🔧 PASO 4: Prefetching Inteligente

```typescript
// filepath: src/hooks/usePrefetch.ts
"use client";

import { useRouter } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { useCallback } from "react";
import { vehicleService } from "@/services/vehicleService";

export function usePrefetch() {
  const router = useRouter();
  const queryClient = useQueryClient();

  // Prefetch página de vehículo al hover
  const prefetchVehicle = useCallback(
    (slug: string) => {
      // Prefetch route
      router.prefetch(`/vehiculos/${slug}`);

      // Prefetch data
      queryClient.prefetchQuery({
        queryKey: ["vehicle", slug],
        queryFn: () => vehicleService.getBySlug(slug),
        staleTime: 5 * 60 * 1000, // 5 minutos
      });
    },
    [router, queryClient],
  );

  // Prefetch página de búsqueda con filtros
  const prefetchSearch = useCallback(
    (filters: Record<string, string>) => {
      const params = new URLSearchParams(filters).toString();
      router.prefetch(`/buscar?${params}`);
    },
    [router],
  );

  return { prefetchVehicle, prefetchSearch };
}
```

### Uso en VehicleCard

```typescript
// filepath: src/components/vehicles/VehicleCard.tsx
'use client';

import { usePrefetch } from '@/hooks/usePrefetch';

export function VehicleCard({ vehicle }) {
  const { prefetchVehicle } = usePrefetch();

  return (
    <Link
      href={`/vehiculos/${vehicle.slug}`}
      onMouseEnter={() => prefetchVehicle(vehicle.slug)}
      onTouchStart={() => prefetchVehicle(vehicle.slug)}
    >
      {/* Card content */}
    </Link>
  );
}
```

---

## 🔧 PASO 5: Bundle Analysis

### Script de Análisis

```json
// package.json
{
  "scripts": {
    "analyze": "ANALYZE=true next build",
    "analyze:server": "BUNDLE_ANALYZE=server next build",
    "analyze:browser": "BUNDLE_ANALYZE=browser next build"
  }
}
```

### Componente de Monitoreo de Bundle

```typescript
// filepath: src/components/dev/BundleMonitor.tsx
// Solo en desarrollo
'use client';

import { useEffect, useState } from 'react';

export function BundleMonitor() {
  const [stats, setStats] = useState<{ js: number; css: number } | null>(null);

  useEffect(() => {
    if (process.env.NODE_ENV !== 'development') return;

    // Calcular tamaño aproximado de recursos cargados
    const resources = performance.getEntriesByType('resource') as PerformanceResourceTiming[];

    const jsSize = resources
      .filter((r) => r.name.includes('.js'))
      .reduce((sum, r) => sum + (r.transferSize || 0), 0);

    const cssSize = resources
      .filter((r) => r.name.includes('.css'))
      .reduce((sum, r) => sum + (r.transferSize || 0), 0);

    setStats({
      js: Math.round(jsSize / 1024),
      css: Math.round(cssSize / 1024),
    });
  }, []);

  if (process.env.NODE_ENV !== 'development' || !stats) return null;

  return (
    <div className="fixed bottom-4 left-4 bg-black/80 text-white text-xs p-2 rounded font-mono z-50">
      JS: {stats.js}KB | CSS: {stats.css}KB
    </div>
  );
}
```

---

## 🔧 PASO 6: Font Optimization

```typescript
// filepath: src/app/layout.tsx
import { Inter } from 'next/font/google';

// Optimización de fuentes con next/font
const inter = Inter({
  subsets: ['latin'],
  display: 'swap', // Evitar FOIT (Flash of Invisible Text)
  preload: true,
  fallback: [
    '-apple-system',
    'BlinkMacSystemFont',
    'Segoe UI',
    'Roboto',
    'sans-serif',
  ],
  // Solo cargar los weights necesarios
  weight: ['400', '500', '600', '700'],
  // Variable font para mejor performance
  variable: '--font-inter',
});

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="es-DO" className={inter.variable}>
      <body className={inter.className}>{children}</body>
    </html>
  );
}
```

---

## 🔧 PASO 7: API Response Optimization

### Compression y Caching

```typescript
// filepath: src/middleware.ts
import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";

export function middleware(request: NextRequest) {
  const response = NextResponse.next();

  // Agregar headers de compresión
  response.headers.set("Accept-Encoding", "gzip, deflate, br");

  // Cache control para assets estáticos
  if (request.nextUrl.pathname.startsWith("/_next/static")) {
    response.headers.set(
      "Cache-Control",
      "public, max-age=31536000, immutable",
    );
  }

  // Cache para API pública
  if (
    request.nextUrl.pathname.startsWith("/api/vehicles") &&
    request.method === "GET"
  ) {
    response.headers.set(
      "Cache-Control",
      "public, s-maxage=60, stale-while-revalidate=300",
    );
  }

  return response;
}
```

### Infinite Scroll Optimizado

```typescript
// filepath: src/hooks/useInfiniteVehicles.ts
import { useInfiniteQuery } from "@tanstack/react-query";
import { vehicleService } from "@/services/vehicleService";

export function useInfiniteVehicles(filters: VehicleFilters) {
  return useInfiniteQuery({
    queryKey: ["vehicles", "infinite", filters],
    queryFn: ({ pageParam = 1 }) =>
      vehicleService.search({ ...filters, page: pageParam, pageSize: 20 }),
    getNextPageParam: (lastPage) =>
      lastPage.pagination.hasNextPage
        ? lastPage.pagination.page + 1
        : undefined,
    // Optimizaciones
    staleTime: 2 * 60 * 1000, // 2 minutos
    gcTime: 10 * 60 * 1000, // 10 minutos en cache
    refetchOnWindowFocus: false,
    // Mantener datos previos mientras se cargan nuevos
    placeholderData: (previousData) => previousData,
  });
}
```

---

## 🔧 PASO 8: Core Web Vitals Monitoring

```typescript
// filepath: src/lib/web-vitals.ts
import { onCLS, onFCP, onFID, onLCP, onTTFB, Metric } from "web-vitals";

type ReportHandler = (metric: Metric) => void;

// Reportar a analytics
const reportToAnalytics: ReportHandler = (metric) => {
  // Google Analytics 4
  if (typeof window !== "undefined" && (window as any).gtag) {
    (window as any).gtag("event", metric.name, {
      value: Math.round(
        metric.name === "CLS" ? metric.value * 1000 : metric.value,
      ),
      event_category: "Web Vitals",
      event_label: metric.id,
      non_interaction: true,
    });
  }

  // Log en desarrollo
  if (process.env.NODE_ENV === "development") {
    console.log(`[Web Vitals] ${metric.name}:`, metric.value);
  }
};

export function initWebVitals() {
  onCLS(reportToAnalytics);
  onFCP(reportToAnalytics);
  onFID(reportToAnalytics);
  onLCP(reportToAnalytics);
  onTTFB(reportToAnalytics);
}
```

### Hook useWebVitals

```typescript
// filepath: src/hooks/useWebVitals.ts
"use client";

import { useEffect } from "react";
import { initWebVitals } from "@/lib/web-vitals";

export function useWebVitals() {
  useEffect(() => {
    initWebVitals();
  }, []);
}
```

---

## 🔧 PASO 9: Script de Performance CI

```yaml
# filepath: .github/workflows/lighthouse.yml
name: Lighthouse CI

on:
  pull_request:
    branches: [main, development]

jobs:
  lighthouse:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: "20"
          cache: "npm"

      - name: Install dependencies
        run: npm ci

      - name: Build
        run: npm run build
        env:
          NEXT_PUBLIC_API_URL: ${{ secrets.API_URL }}

      - name: Run Lighthouse CI
        uses: treosh/lighthouse-ci-action@v11
        with:
          configPath: "./lighthouserc.json"
          uploadArtifacts: true
          temporaryPublicStorage: true
```

```json
// filepath: lighthouserc.json
{
  "ci": {
    "collect": {
      "url": [
        "http://localhost:3000/",
        "http://localhost:3000/buscar",
        "http://localhost:3000/vehiculos/toyota-camry-2024"
      ],
      "startServerCommand": "npm run start",
      "numberOfRuns": 3
    },
    "assert": {
      "preset": "lighthouse:recommended",
      "assertions": {
        "first-contentful-paint": ["error", { "maxNumericValue": 1500 }],
        "largest-contentful-paint": ["error", { "maxNumericValue": 2500 }],
        "cumulative-layout-shift": ["error", { "maxNumericValue": 0.1 }],
        "total-blocking-time": ["error", { "maxNumericValue": 200 }],
        "speed-index": ["warn", { "maxNumericValue": 3000 }]
      }
    }
  }
}
```

---

## 🔧 PASO 10: Performance Dashboard Component

```typescript
// filepath: src/components/dev/PerformanceDashboard.tsx
// Solo visible en desarrollo
'use client';

import { useEffect, useState } from 'react';

interface PerformanceMetrics {
  fcp: number | null;
  lcp: number | null;
  cls: number | null;
  ttfb: number | null;
  domContentLoaded: number | null;
  load: number | null;
}

export function PerformanceDashboard() {
  const [metrics, setMetrics] = useState<PerformanceMetrics>({
    fcp: null,
    lcp: null,
    cls: null,
    ttfb: null,
    domContentLoaded: null,
    load: null,
  });
  const [isOpen, setIsOpen] = useState(false);

  useEffect(() => {
    if (process.env.NODE_ENV !== 'development') return;

    // Navigation timing
    const navTiming = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming;

    setMetrics((prev) => ({
      ...prev,
      ttfb: Math.round(navTiming.responseStart - navTiming.requestStart),
      domContentLoaded: Math.round(navTiming.domContentLoadedEventEnd),
      load: Math.round(navTiming.loadEventEnd),
    }));

    // Paint timing
    const paintEntries = performance.getEntriesByType('paint');
    const fcp = paintEntries.find((e) => e.name === 'first-contentful-paint');
    if (fcp) {
      setMetrics((prev) => ({ ...prev, fcp: Math.round(fcp.startTime) }));
    }
  }, []);

  if (process.env.NODE_ENV !== 'development') return null;

  const getStatusColor = (value: number | null, good: number, ok: number) => {
    if (value === null) return 'text-gray-400';
    if (value <= good) return 'text-green-500';
    if (value <= ok) return 'text-yellow-500';
    return 'text-red-500';
  };

  return (
    <div className="fixed bottom-4 right-4 z-50">
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="bg-gray-900 text-white px-3 py-1 rounded-full text-xs font-mono"
      >
        ⚡ Perf
      </button>

      {isOpen && (
        <div className="absolute bottom-10 right-0 bg-gray-900 text-white p-4 rounded-lg shadow-xl min-w-[200px] font-mono text-xs">
          <div className="space-y-2">
            <div className="flex justify-between">
              <span>FCP:</span>
              <span className={getStatusColor(metrics.fcp, 1500, 2500)}>
                {metrics.fcp ?? '...'} ms
              </span>
            </div>
            <div className="flex justify-between">
              <span>TTFB:</span>
              <span className={getStatusColor(metrics.ttfb, 600, 800)}>
                {metrics.ttfb ?? '...'} ms
              </span>
            </div>
            <div className="flex justify-between">
              <span>DOM Ready:</span>
              <span className={getStatusColor(metrics.domContentLoaded, 1500, 3000)}>
                {metrics.domContentLoaded ?? '...'} ms
              </span>
            </div>
            <div className="flex justify-between">
              <span>Load:</span>
              <span className={getStatusColor(metrics.load, 2500, 5000)}>
                {metrics.load ?? '...'} ms
              </span>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
```

---

## ✅ Checklist de Optimización

### Bundle & Code

- [ ] Configurar next.config.mjs optimizado
- [ ] Implementar dynamic imports para componentes pesados
- [ ] Configurar bundle analyzer
- [ ] Eliminar dependencias no usadas

### Imágenes

- [ ] Usar next/image para todas las imágenes
- [ ] Configurar lazy loading para listas
- [ ] Optimizar formatos (WebP, AVIF)
- [ ] Implementar placeholders blur

### Network

- [ ] Configurar headers de caché
- [ ] Implementar prefetching inteligente
- [ ] Optimizar API calls con staleTime

### Fonts

- [ ] Usar next/font para fuentes
- [ ] Limitar weights cargados
- [ ] Configurar display: swap

### Monitoring

- [ ] Configurar Web Vitals reporting
- [ ] Agregar Lighthouse CI
- [ ] Crear dashboard de performance

---

## 🔗 Referencias

- [Next.js Performance](https://nextjs.org/docs/app/building-your-application/optimizing)
- [Web Vitals](https://web.dev/vitals/)
- [Lighthouse CI](https://github.com/GoogleChrome/lighthouse-ci)

---

_La performance es una feature. Usuarios abandonan páginas que tardan más de 3 segundos en cargar._
