/**
 * Auth Layout
 * Shared layout for authentication pages (login, register, forgot password)
 */

'use client';

import Image from 'next/image';
import Link from 'next/link';
import { useSiteConfig } from '@/providers/site-config-provider';
import { PLATFORM_STATS } from '@/lib/platform-stats';

export default function AuthLayout({ children }: { children: React.ReactNode }) {
  const config = useSiteConfig();

  return (
    <div className="flex min-h-screen">
      {/* Left side - Hero image (hidden on mobile) */}
      <div className="bg-primary relative hidden lg:flex lg:w-1/2">
        {/* Background Image */}
        <Image
          src="https://images.unsplash.com/photo-1606664515524-ed2f786a0bd6?w=1920&q=80"
          alt="SUV moderno premium"
          fill
          priority
          className="object-cover opacity-80"
          sizes="50vw"
        />

        {/* Overlay */}
        <div className="from-primary/90 to-primary/70 absolute inset-0 bg-gradient-to-br" />

        {/* Content */}
        <div className="relative z-10 flex flex-col justify-between p-12 text-white">
          {/* Logo */}
          <Link href="/" className="flex items-center gap-2">
            <div className="bg-card flex h-10 w-10 items-center justify-center rounded-lg">
              <span className="text-primary text-xl font-bold">{config.siteName.charAt(0)}</span>
            </div>
            <span className="text-2xl font-bold">{config.siteName}</span>
          </Link>

          {/* Main content */}
          <div className="space-y-6">
            <h1 className="text-4xl leading-tight font-bold">{config.siteDescription}</h1>
            <p className="text-lg text-white/90">
              Miles de vehículos te esperan. Encuentra tu próximo carro, jeepeta o camioneta.
            </p>

            {/* Stats */}
            <div className="flex gap-8 pt-4">
              <div>
                <div className="text-3xl font-bold">{PLATFORM_STATS.vehiclesPublished}</div>
                <div className="text-sm text-white/80">Vehículos</div>
              </div>
              <div>
                <div className="text-3xl font-bold">{PLATFORM_STATS.registeredDealers}</div>
                <div className="text-sm text-white/80">Dealers</div>
              </div>
              <div>
                <div className="text-3xl font-bold">{PLATFORM_STATS.activeUsers}</div>
                <div className="text-sm text-white/80">Usuarios</div>
              </div>
            </div>
            <p className="text-xs text-white/50">*Cifras proyectadas basadas en el mercado automotriz dominicano.</p>
          </div>

          {/* Footer */}
          <div className="text-sm text-white/70">
            © {new Date().getFullYear()} {config.siteName}. Todos los derechos reservados.
          </div>
        </div>
      </div>

      {/* Right side - Auth form */}
      <div className="bg-muted/50 flex flex-1 items-center justify-center px-4 py-8 sm:p-12">
        <div className="w-full max-w-md">
          {/* Mobile logo */}
          <div className="mb-8 text-center lg:hidden">
            <Link href="/" className="inline-flex items-center gap-2">
              <div className="bg-primary flex h-10 w-10 items-center justify-center rounded-lg">
                <span className="text-xl font-bold text-white">{config.siteName.charAt(0)}</span>
              </div>
              <span className="text-foreground text-2xl font-bold">{config.siteName}</span>
            </Link>
          </div>

          <main id="main-content">{children}</main>
        </div>
      </div>
    </div>
  );
}
