import type { NextConfig } from 'next';
import bundleAnalyzer from '@next/bundle-analyzer';

const withBundleAnalyzer = bundleAnalyzer({
  enabled: process.env.ANALYZE === 'true',
});

const nextConfig: NextConfig = {
  // Output standalone for Docker deployment
  output: 'standalone',

  // Security: remove X-Powered-By header
  poweredByHeader: false,

  // Development: enable strict mode for catching bugs
  reactStrictMode: true,

  // Image optimization configuration
  images: {
    remotePatterns: [
      {
        protocol: 'https',
        hostname: 'images.unsplash.com',
        port: '',
        pathname: '/**',
      },
      // Test/seed data placeholder services
      {
        protocol: 'https',
        hostname: 'picsum.photos',
        port: '',
        pathname: '/**',
      },
      {
        protocol: 'https',
        hostname: 'placehold.co',
        port: '',
        pathname: '/**',
      },
      {
        protocol: 'https',
        hostname: 'via.placeholder.com',
        port: '',
        pathname: '/**',
      },
      {
        protocol: 'https',
        hostname: '*.okla.com.do',
        port: '',
        pathname: '/**',
      },
      {
        protocol: 'https',
        hostname: 'okla-media.s3.amazonaws.com',
        port: '',
        pathname: '/**',
      },
      {
        protocol: 'https',
        hostname: 'okla-media.s3.us-east-1.amazonaws.com',
        port: '',
        pathname: '/**',
      },
      {
        protocol: 'https',
        hostname: 'okla-images-2026.s3.us-east-2.amazonaws.com',
        port: '',
        pathname: '/**',
      },
      {
        protocol: 'https',
        hostname: '*.s3.amazonaws.com',
        port: '',
        pathname: '/**',
      },
      {
        protocol: 'https',
        hostname: '*.s3.*.amazonaws.com',
        port: '',
        pathname: '/**',
      },
      // DigitalOcean Spaces CDN — for image storage migration
      {
        protocol: 'https',
        hostname: '*.cdn.digitaloceanspaces.com',
        port: '',
        pathname: '/**',
      },
      {
        protocol: 'https',
        hostname: '*.digitaloceanspaces.com',
        port: '',
        pathname: '/**',
      },
      // Wikimedia Commons — logos de marcas automotrices para DealerPromoSection
      {
        protocol: 'https',
        hostname: 'upload.wikimedia.org',
        port: '',
        pathname: '/**',
      },
    ],
    formats: ['image/avif', 'image/webp'],
    deviceSizes: [640, 750, 828, 1080, 1200, 1920, 2048, 3840],
    imageSizes: [16, 32, 48, 64, 96, 128, 256, 384],
    qualities: [60, 70, 75, 80, 85],
    // Cache optimized images for 30 days — vehicle photos rarely change.
    // Reduces re-processing on the server and speeds up repeat visits on slow DR connections.
    minimumCacheTTL: 2592000,
  },

  // Enable experimental features
  experimental: {
    optimizePackageImports: [
      'lucide-react',
      '@radix-ui/react-icons',
      'date-fns',
      'sonner',
      'recharts',
      '@radix-ui/react-dialog',
      '@radix-ui/react-dropdown-menu',
      '@radix-ui/react-select',
      '@radix-ui/react-tabs',
      'zod',
      'react-hook-form',
    ],
    // KYC documents (cedula front/back/selfie) can be up to ~1MB each after base64 encoding.
    // Default Next.js limit is 1MB; raise to 10MB to handle higher-res camera captures.
    serverActions: {
      bodySizeLimit: '10mb',
    },
  },

  // Compiler options — keep console.error and console.warn in production for debugging
  compiler: {
    removeConsole: process.env.NODE_ENV === 'production' ? { exclude: ['error', 'warn'] } : false,
  },

  // Headers for security and caching
  async headers() {
    const isDev = process.env.NODE_ENV === 'development';
    const disableCSP = process.env.DISABLE_CSP === 'true';

    // Content Security Policy
    const cspDirectives = [
      "default-src 'self'",
      "script-src 'self' 'unsafe-inline' https://www.googletagmanager.com https://*.googletagmanager.com https://www.google-analytics.com https://js.stripe.com https://www.paypal.com https://www.sandbox.paypal.com https://www.paypalobjects.com",
      "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com",
      "img-src 'self' data: blob: https:",
      "font-src 'self' https://fonts.gstatic.com",
      // worker-src must be explicitly set; otherwise browsers fall back to script-src which
      // does not allow blob: URIs. browser-image-compression creates blob: workers for
      // client-side image compression before upload — without this directive uploads stall.
      "worker-src 'self' blob:",
      // BFF pattern: browser calls same-origin (/api/*), Next.js rewrites proxy to internal Gateway
      // No need for https://api.okla.com.do — Gateway is NOT exposed externally
      "connect-src 'self' http://localhost:* https://www.google-analytics.com https://www.googletagmanager.com https://*.googletagmanager.com https://*.google-analytics.com https://api.stripe.com https://*.paypal.com https://api-m.paypal.com https://api-m.sandbox.paypal.com wss: ws:",
      "frame-src 'self' https://js.stripe.com https://www.google.com https://www.paypal.com https://www.sandbox.paypal.com",
      "frame-ancestors 'self'",
      "form-action 'self'",
      "base-uri 'self'",
      "object-src 'none'",
    ];

    // Only apply CSP in production and when not explicitly disabled
    const csp = isDev || disableCSP ? '' : cspDirectives.join('; ');

    return [
      {
        source: '/:path*',
        headers: [
          // DNS Prefetch
          {
            key: 'X-DNS-Prefetch-Control',
            value: 'on',
          },
          // Prevent clickjacking (disabled in dev so VS Code Simple Browser iframe works)
          ...(isDev
            ? []
            : [
                {
                  key: 'X-Frame-Options',
                  value: 'SAMEORIGIN',
                },
              ]),
          // Prevent MIME type sniffing
          {
            key: 'X-Content-Type-Options',
            value: 'nosniff',
          },
          // Referrer policy
          {
            key: 'Referrer-Policy',
            value: 'strict-origin-when-cross-origin',
          },
          // XSS Protection (legacy but still useful)
          {
            key: 'X-XSS-Protection',
            value: '1; mode=block',
          },
          // Permissions Policy (formerly Feature-Policy)
          // In development, allow camera/microphone from any origin for remote access
          // In production, restrict to self only
          {
            key: 'Permissions-Policy',
            value: isDev
              ? 'camera=*, microphone=*, geolocation=(self), interest-cohort=()'
              : 'camera=(self), microphone=(self), geolocation=(self), interest-cohort=()',
          },
          // HSTS - Strict Transport Security (1 year)
          ...(isDev
            ? []
            : [
                {
                  key: 'Strict-Transport-Security',
                  value: 'max-age=31536000; includeSubDomains; preload',
                },
              ]),
          // Content Security Policy
          ...(csp
            ? [
                {
                  key: 'Content-Security-Policy',
                  value: csp,
                },
              ]
            : []),
          // Prevent stale HTML after Next.js deployments.
          // Chunk hashes change on every build — if the browser caches old HTML,
          // it references chunks that no longer exist on the server → 404 cascade.
          // "no-cache" allows caching but forces revalidation before use (ETag/304).
          // /_next/static/:path* (below) overrides this to "immutable" for JS/CSS chunks.
          {
            key: 'Cache-Control',
            value: 'no-cache, must-revalidate',
          },
        ],
      },
      // Cache static assets aggressively
      {
        source: '/icons/:path*',
        headers: [
          {
            key: 'Cache-Control',
            value: 'public, max-age=31536000, immutable',
          },
        ],
      },
      {
        source: '/_next/static/:path*',
        headers: [
          {
            key: 'Cache-Control',
            value: 'public, max-age=31536000, immutable',
          },
        ],
      },
      // API routes - no cache
      {
        source: '/api/:path*',
        headers: [
          {
            key: 'Cache-Control',
            value: 'no-store, no-cache, must-revalidate',
          },
          {
            key: 'X-Content-Type-Options',
            value: 'nosniff',
          },
        ],
      },
      // Cache optimized images (Next.js image optimizer output) — 7 days with 30-day stale-while-revalidate
      // Vehicle photos rarely change; aggressive caching reduces bandwidth for slow DR connections
      {
        source: '/_next/image/:path*',
        headers: [
          {
            key: 'Cache-Control',
            value: 'public, max-age=604800, stale-while-revalidate=2592000',
          },
        ],
      },
    ];
  },

  // Redirects
  async redirects() {
    return [
      // Redirect /autos to /vehiculos for SEO
      {
        source: '/autos',
        destination: '/vehiculos',
        permanent: true,
      },
      {
        source: '/autos/:slug*',
        destination: '/vehiculos/:slug*',
        permanent: true,
      },
      // Redirect English email verification URL to Spanish route
      {
        source: '/verify-email',
        destination: '/verificar-email',
        permanent: false, // Use 307 to preserve query params (token)
      },
      // OAuth callback: old /auth/callback/:provider → new /callback/:provider
      {
        source: '/auth/callback/:provider',
        destination: '/callback/:provider',
        permanent: true,
      },
    ];
  },

  // ==========================================================================
  // BFF Pattern: Reverse-proxy /api/* to internal Gateway
  // ==========================================================================
  // In production, the Gateway is NOT exposed to the internet.
  // All API traffic flows: Browser → okla.com.do/api/* → Next.js → gateway:8080
  //
  // Uses `afterFiles` so Next.js API routes (e.g., /api/kyc-config, /api/pricing)
  // are served first. Only unmatched /api/* paths get proxied to the Gateway.
  async rewrites() {
    // Server-side env var — not NEXT_PUBLIC_ so it's only available at runtime
    const internalApiUrl =
      process.env.INTERNAL_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:18443';

    return {
      // beforeFiles rewrites run BEFORE filesystem routes (API route handlers).
      // We use this to rewrite dynamic advertising paths to static BFF routes
      // that can be handled by our API route handlers (which have fallback data).
      beforeFiles: [
        // /api/advertising/rotation/FeaturedSpot → /api/advertising/rotation?section=FeaturedSpot
        {
          source: '/api/advertising/rotation/:section',
          destination: '/api/advertising/rotation?section=:section',
        },
        // /api/advertising/rotation/config/:section → /api/advertising/rotation?section=:section&subpath=config
        {
          source: '/api/advertising/rotation/config/:section',
          destination: '/api/advertising/rotation?section=:section&subpath=config',
        },
      ],
      afterFiles: [
        {
          source: '/api/:path*',
          destination: `${internalApiUrl}/api/:path*`,
        },
      ],
    };
  },
};

export default withBundleAnalyzer(nextConfig);
