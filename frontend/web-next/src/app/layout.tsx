import type { Metadata, Viewport } from 'next';
import { Inter } from 'next/font/google';
import { Providers } from './providers';
import { Toaster } from 'sonner';
import { GoogleAnalytics } from '@/components/analytics/google-analytics';
import { SiteJsonLd } from '@/lib/seo';
import { PWAComponents } from '@/components/pwa/pwa-wrapper';
import { CookieConsentBanner, CookieSettingsButton } from '@/components/legal/cookie-consent';
import WebVitals from '@/components/monitoring/web-vitals';
import './globals.css';

const inter = Inter({
  subsets: ['latin'],
  display: 'swap',
  variable: '--font-inter',
});

export const metadata: Metadata = {
  title: {
    default: 'OKLA - Marketplace de Vehículos #1 en República Dominicana',
    template: '%s | OKLA',
  },
  description:
    'Encuentra el vehículo perfecto o vende el tuyo en OKLA. El marketplace de autos más grande de República Dominicana con miles de opciones verificadas.',
  keywords: [
    'carros',
    'vehículos',
    'autos',
    'República Dominicana',
    'comprar carro',
    'vender carro',
    'marketplace',
    'OKLA',
    'dealer',
    'usados',
    'nuevos',
  ],
  authors: [{ name: 'OKLA' }],
  creator: 'OKLA',
  publisher: 'OKLA',
  formatDetection: {
    email: false,
    address: false,
    telephone: false,
  },
  metadataBase: new URL(process.env.NEXT_PUBLIC_SITE_URL || 'https://okla.com.do'),
  alternates: {
    canonical: '/',
  },
  openGraph: {
    type: 'website',
    locale: 'es_DO',
    url: '/',
    siteName: 'OKLA',
    title: 'OKLA - Marketplace de Vehículos #1 en República Dominicana',
    description:
      'Encuentra el vehículo perfecto o vende el tuyo en OKLA. El marketplace de autos más grande de República Dominicana.',
    images: [
      {
        url: '/opengraph-image',
        width: 1200,
        height: 630,
        alt: 'OKLA - Marketplace de Vehículos',
      },
    ],
  },
  twitter: {
    card: 'summary_large_image',
    title: 'OKLA - Marketplace de Vehículos',
    description: 'El marketplace de autos #1 de República Dominicana',
    images: ['/opengraph-image'],
    creator: '@okla',
  },
  robots: {
    index: true,
    follow: true,
    googleBot: {
      index: true,
      follow: true,
      'max-video-preview': -1,
      'max-image-preview': 'large',
      'max-snippet': -1,
    },
  },
  icons: {
    icon: '/favicon.ico',
    shortcut: '/favicon-16x16.png',
    apple: '/apple-touch-icon.png',
  },
  manifest: '/manifest.json',
  // SEO AUDIT FIX: Google Search Console verification
  // Replace with your actual verification code from Google Search Console
  verification: {
    google: process.env.NEXT_PUBLIC_GOOGLE_SITE_VERIFICATION || 'PENDING_VERIFICATION_CODE',
  },
  appleWebApp: {
    capable: true,
    statusBarStyle: 'default',
    title: 'OKLA',
  },
};

export const viewport: Viewport = {
  themeColor: '#00A870',
  width: 'device-width',
  initialScale: 1,
  maximumScale: 5,
};

// Script to apply theme before hydration (prevents flash of wrong theme)
// Default is 'light' - users must explicitly choose dark mode
const themeScript = `
  (function() {
    try {
      const stored = localStorage.getItem('okla-theme');
      // Default to 'light' instead of 'system' - OKLA's brand is light theme
      const theme = stored || 'light';
      const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
      const isDark = theme === 'dark' || (theme === 'system' && prefersDark);
      if (isDark) {
        document.documentElement.classList.add('dark');
      } else {
        document.documentElement.classList.remove('dark');
      }
    } catch (e) {}
  })()
`;

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html
      lang="es-DO"
      className={inter.variable}
      suppressHydrationWarning
      data-scroll-behavior="smooth"
    >
      <head>
        <script dangerouslySetInnerHTML={{ __html: themeScript }} />
        <meta httpEquiv="Permissions-Policy" content="camera=*, microphone=*" />
        {/* Apple PWA splash screens — generated for common iPhone/iPad sizes */}
        <link
          rel="apple-touch-startup-image"
          href="/splash/apple-splash-1170x2532.png"
          media="(device-width: 390px) and (device-height: 844px) and (-webkit-device-pixel-ratio: 3)"
        />
        <link
          rel="apple-touch-startup-image"
          href="/splash/apple-splash-1179x2556.png"
          media="(device-width: 393px) and (device-height: 852px) and (-webkit-device-pixel-ratio: 3)"
        />
        <link
          rel="apple-touch-startup-image"
          href="/splash/apple-splash-1290x2796.png"
          media="(device-width: 430px) and (device-height: 932px) and (-webkit-device-pixel-ratio: 3)"
        />
        <link
          rel="apple-touch-startup-image"
          href="/splash/apple-splash-1125x2436.png"
          media="(device-width: 375px) and (device-height: 812px) and (-webkit-device-pixel-ratio: 3)"
        />
        <link
          rel="apple-touch-startup-image"
          href="/splash/apple-splash-1242x2688.png"
          media="(device-width: 414px) and (device-height: 896px) and (-webkit-device-pixel-ratio: 3)"
        />
        <link
          rel="apple-touch-startup-image"
          href="/splash/apple-splash-828x1792.png"
          media="(device-width: 414px) and (device-height: 896px) and (-webkit-device-pixel-ratio: 2)"
        />
        <link
          rel="apple-touch-startup-image"
          href="/splash/apple-splash-1284x2778.png"
          media="(device-width: 428px) and (device-height: 926px) and (-webkit-device-pixel-ratio: 3)"
        />
        <link
          rel="apple-touch-startup-image"
          href="/splash/apple-splash-750x1334.png"
          media="(device-width: 375px) and (device-height: 667px) and (-webkit-device-pixel-ratio: 2)"
        />
        <link
          rel="apple-touch-startup-image"
          href="/splash/apple-splash-640x1136.png"
          media="(device-width: 320px) and (device-height: 568px) and (-webkit-device-pixel-ratio: 2)"
        />
        {/* iPad */}
        <link
          rel="apple-touch-startup-image"
          href="/splash/apple-splash-2048x2732.png"
          media="(device-width: 1024px) and (device-height: 1366px) and (-webkit-device-pixel-ratio: 2)"
        />
        <link
          rel="apple-touch-startup-image"
          href="/splash/apple-splash-1668x2388.png"
          media="(device-width: 834px) and (device-height: 1194px) and (-webkit-device-pixel-ratio: 2)"
        />
        <link
          rel="apple-touch-startup-image"
          href="/splash/apple-splash-1536x2048.png"
          media="(device-width: 768px) and (device-height: 1024px) and (-webkit-device-pixel-ratio: 2)"
        />
        {/* Preconnect to image CDN for faster LCP */}
        <link rel="preconnect" href="https://okla-images-2026.s3.us-east-2.amazonaws.com" />
        <link rel="dns-prefetch" href="https://okla-images-2026.s3.us-east-2.amazonaws.com" />
        <link rel="preconnect" href="https://cdn.okla.com.do" />
        <link rel="dns-prefetch" href="https://cdn.okla.com.do" />
        {/* CWV P0-1 FIX: Hero preload REMOVED from root layout.
            It was firing on every page (/vehiculos, /cuenta, etc.) wasting bandwidth.
            Moved to homepage-only via src/app/(main)/page.tsx metadata. */}
        <SiteJsonLd />
      </head>
      <body
        className="bg-background text-foreground min-h-screen antialiased"
        suppressHydrationWarning
      >
        <a
          href="#main-content"
          className="focus:bg-primary focus:text-primary-foreground sr-only focus:not-sr-only focus:fixed focus:top-4 focus:left-4 focus:z-[9999] focus:rounded-lg focus:px-4 focus:py-2 focus:text-sm focus:font-medium focus:shadow-lg"
        >
          Ir al contenido principal
        </a>
        <Providers>
          <PWAComponents>
            {children}
            <WebVitals />
            <Toaster
              position="top-right"
              toastOptions={{
                classNames: {
                  toast: 'bg-card text-card-foreground border-border',
                },
              }}
            />
          </PWAComponents>
        </Providers>
        <GoogleAnalytics />
        {/* P1-02 FIX: Facebook Pixel noscript fallback for ad blocker resilience.
            If fbevents.js fails to load, this 1x1 img sends at least a PageView. */}
        {process.env.NEXT_PUBLIC_FB_PIXEL_ID && (
          <noscript>
            {/* eslint-disable-next-line @next/next/no-img-element -- noscript fallback requires native img (next/image needs JS) */}
            <img
              height="1"
              width="1"
              style={{ display: 'none' }}
              src={`https://www.facebook.com/tr?id=${process.env.NEXT_PUBLIC_FB_PIXEL_ID}&ev=PageView&noscript=1`}
              alt=""
            />
          </noscript>
        )}
        <CookieConsentBanner />
        <CookieSettingsButton />
      </body>
    </html>
  );
}
