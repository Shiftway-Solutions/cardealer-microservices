/**
 * Vender CTA - Client island for auth-dependent elements
 *
 * Only the KYC banner and CTAs need client-side rendering.
 * The rest of the /vender page is fully static (server component).
 */

'use client';

import * as React from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { Button } from '@/components/ui/button';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Card, CardContent } from '@/components/ui/card';
import { ArrowRight, CheckCircle, Clock, AlertTriangle, BadgeCheck, Loader2 } from 'lucide-react';
import { usePlatformPricing } from '@/hooks/use-platform-pricing';
import { useCanSell } from '@/hooks/use-kyc';
import { useAuth } from '@/hooks/use-auth';

// =============================================================================
// KYC VERIFICATION BANNER
// =============================================================================

export function VenderKycBanner() {
  const { isAuthenticated } = useAuth();
  const { canSell, isPending, isRejected, needsVerification, isLoading, rejectionReason } =
    useCanSell();

  const showVerificationBanner = isAuthenticated && !canSell && !isLoading;

  if (!showVerificationBanner) return null;

  return (
    <div className="border-b border-amber-200 bg-amber-50">
      <div className="container mx-auto px-4 py-4">
        {isPending ? (
          <Alert className="border-blue-200 bg-blue-50">
            <Loader2 className="h-4 w-4 animate-spin text-blue-600" />
            <AlertTitle className="text-blue-800">Verificación en proceso</AlertTitle>
            <AlertDescription className="text-blue-700">
              Tu identidad está siendo verificada. Te notificaremos cuando esté lista. Esto puede
              tomar hasta 24 horas.
            </AlertDescription>
          </Alert>
        ) : isRejected ? (
          <Alert variant="destructive">
            <AlertTriangle className="h-4 w-4" />
            <AlertTitle>Verificación rechazada</AlertTitle>
            <AlertDescription className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div className="space-y-1">
                <span>Tu verificación fue rechazada. Por favor, intenta nuevamente.</span>
                {rejectionReason && (
                  <p className="text-sm opacity-90">
                    <strong>Motivo:</strong> {rejectionReason}
                  </p>
                )}
              </div>
              <Button asChild size="sm" variant="outline" className="ml-4 shrink-0">
                <Link href="/cuenta/verificacion">Reintentar verificación</Link>
              </Button>
            </AlertDescription>
          </Alert>
        ) : needsVerification ? (
          <Alert className="border-amber-300 bg-amber-50">
            <BadgeCheck className="h-4 w-4 text-amber-600" />
            <AlertTitle className="text-amber-800">Verifica tu identidad para vender</AlertTitle>
            <AlertDescription className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <span className="text-amber-700">
                Para publicar vehículos necesitas verificar tu identidad con tu cédula y una selfie.
                Es rápido y seguro.
              </span>
              <Button asChild size="sm" className="shrink-0 bg-amber-600 hover:bg-amber-700">
                <Link href="/cuenta/verificacion">
                  <BadgeCheck className="mr-2 h-4 w-4" />
                  Verificar ahora
                </Link>
              </Button>
            </AlertDescription>
          </Alert>
        ) : null}
      </div>
    </div>
  );
}

// =============================================================================
// HERO CTA BUTTONS
// =============================================================================

export function HeroCTA() {
  const { isAuthenticated } = useAuth();
  const { canSell, isPending } = useCanSell();

  return (
    <div className="flex flex-col items-center justify-center gap-4 sm:flex-row">
      {!isAuthenticated ? (
        <>
          <Button asChild size="lg" className="hover:bg-muted gap-2 bg-white px-8 text-[#00A870]">
            <Link href="/vender/registro">
              Crear cuenta y vender
              <ArrowRight className="h-5 w-5" />
            </Link>
          </Button>
          <Button
            asChild
            variant="outline"
            size="lg"
            className="gap-2 border-white/30 bg-white/10 text-white hover:bg-white/20"
          >
            <Link href="/login">Ya tengo cuenta</Link>
          </Button>
        </>
      ) : canSell ? (
        <Button asChild size="lg" className="hover:bg-muted gap-2 bg-white px-8 text-[#00A870]">
          <Link href="/publicar">
            <BadgeCheck className="h-5 w-5" />
            Publicar mi vehículo
            <ArrowRight className="h-5 w-5" />
          </Link>
        </Button>
      ) : isPending ? (
        <Button
          size="lg"
          disabled
          className="cursor-not-allowed gap-2 bg-white/50 px-8 text-[#00A870]"
        >
          <Loader2 className="h-5 w-5 animate-spin" />
          Verificación en proceso...
        </Button>
      ) : (
        <Button
          asChild
          size="lg"
          className="gap-2 bg-yellow-400 px-8 text-gray-900 hover:bg-yellow-300"
        >
          <Link href="/cuenta/verificacion">
            <BadgeCheck className="h-5 w-5" />
            Verificar identidad primero
          </Link>
        </Button>
      )}
      <Button
        asChild
        variant="outline"
        size="lg"
        className="gap-2 border-white/30 bg-white/10 text-white hover:bg-white/20"
      >
        <Link href="#como-funciona">Ver cómo funciona</Link>
      </Button>
    </div>
  );
}

// =============================================================================
// FINAL CTA
// =============================================================================

export function FinalCTA() {
  const { isAuthenticated } = useAuth();
  const { canSell, isPending } = useCanSell();

  return (
    <>
      {!isAuthenticated ? (
        <Button asChild size="lg" className="hover:bg-muted gap-2 bg-white px-8 text-[#00A870]">
          <Link href="/vender/registro">
            Crear cuenta y vender
            <ArrowRight className="h-5 w-5" />
          </Link>
        </Button>
      ) : canSell ? (
        <Button asChild size="lg" className="hover:bg-muted gap-2 bg-white px-8 text-[#00A870]">
          <Link href="/publicar">
            Publicar ahora
            <ArrowRight className="h-5 w-5" />
          </Link>
        </Button>
      ) : isPending ? (
        <Button
          size="lg"
          disabled
          className="cursor-not-allowed gap-2 bg-white/50 px-8 text-[#00A870]"
        >
          <Loader2 className="h-5 w-5 animate-spin" />
          Verificación en proceso...
        </Button>
      ) : (
        <Button
          asChild
          size="lg"
          className="gap-2 bg-yellow-400 px-8 text-gray-900 hover:bg-yellow-300"
        >
          <Link href="/cuenta/verificacion">
            <BadgeCheck className="h-5 w-5" />
            Verificar identidad para publicar
          </Link>
        </Button>
      )}

      <p className="mt-6 text-sm text-white/70">
        <Clock className="mr-1 inline h-4 w-4" />
        {canSell ? 'Publica en menos de 5 minutos' : 'Verificación rápida con tu cédula'}
      </p>
    </>
  );
}

// =============================================================================
// PRICING SECTION (client-side for dynamic pricing data)
// =============================================================================

export function VenderPricing() {
  const { pricing, formatPrice } = usePlatformPricing();

  return (
    <div className="grid gap-6 md:grid-cols-2">
      {/* Free Plan */}
      <Card className="border-border border-2">
        <CardContent className="p-6">
          <div className="mb-4">
            <h3 className="text-foreground text-xl font-bold">Gratuito</h3>
            <p className="text-muted-foreground text-sm">Para vendedores ocasionales</p>
          </div>
          <div className="mb-6">
            <span className="text-foreground text-4xl font-bold">
              {formatPrice(pricing.basicListing)}
            </span>
          </div>
          <ul className="mb-6 space-y-3">
            <li className="text-muted-foreground flex items-center gap-2 text-sm">
              <CheckCircle className="h-4 w-4 text-[#00A870]" />1 publicación activa
            </li>
            <li className="text-muted-foreground flex items-center gap-2 text-sm">
              <CheckCircle className="h-4 w-4 text-[#00A870]" />
              Hasta {pricing.freeMaxPhotos} fotos
            </li>
            <li className="text-muted-foreground flex items-center gap-2 text-sm">
              <CheckCircle className="h-4 w-4 text-[#00A870]" />
              Duración: {pricing.basicListingDays} días
            </li>
            <li className="text-muted-foreground flex items-center gap-2 text-sm">
              <CheckCircle className="h-4 w-4 text-[#00A870]" />
              Contacto por WhatsApp
            </li>
          </ul>
          <Button asChild variant="outline" className="w-full">
            <Link href="/publicar">Comenzar gratis</Link>
          </Button>
        </CardContent>
      </Card>

      {/* Premium Plan */}
      <Card className="relative border-2 border-[#00A870]">
        {/* Popular badge */}
        <div className="absolute -top-3 left-1/2 -translate-x-1/2">
          <span className="rounded-full bg-[#00A870] px-3 py-1 text-xs font-semibold text-white">
            MÁS POPULAR
          </span>
        </div>

        <CardContent className="p-6">
          <div className="mb-4">
            <h3 className="text-foreground text-xl font-bold">Premium</h3>
            <p className="text-muted-foreground text-sm">Vende más rápido</p>
          </div>
          <div className="mb-6">
            <span className="text-foreground text-4xl font-bold">
              {formatPrice(pricing.sellerPremiumPrice)}
            </span>
            <span className="text-muted-foreground text-sm">/mes</span>
          </div>
          <ul className="mb-6 space-y-3">
            <li className="text-muted-foreground flex items-center gap-2 text-sm">
              <CheckCircle className="h-4 w-4 text-[#00A870]" />
              Hasta 5 publicaciones activas
            </li>
            <li className="text-muted-foreground flex items-center gap-2 text-sm">
              <CheckCircle className="h-4 w-4 text-[#00A870]" />
              Fotos ilimitadas
            </li>
            <li className="text-muted-foreground flex items-center gap-2 text-sm">
              <CheckCircle className="h-4 w-4 text-[#00A870]" />
              Prioridad en búsquedas
            </li>
            <li className="text-muted-foreground flex items-center gap-2 text-sm">
              <CheckCircle className="h-4 w-4 text-[#00A870]" />
              Badge de vendedor verificado
            </li>
            <li className="text-muted-foreground flex items-center gap-2 text-sm">
              <CheckCircle className="h-4 w-4 text-[#00A870]" />
              Estadísticas detalladas
            </li>
          </ul>
          <Button asChild className="w-full bg-[#00A870] hover:bg-[#009663]">
            <Link href="/publicar?plan=premium">Elegir Premium</Link>
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}

// =============================================================================
// SELLER REDIRECT — redirects already-registered sellers away from /vender
// =============================================================================

export function VenderSellerRedirect() {
  const { isAuthenticated, user, isLoading } = useAuth();
  const router = useRouter();

  React.useEffect(() => {
    if (!isLoading && isAuthenticated && user?.accountType === 'seller') {
      router.replace('/cuenta');
    }
  }, [isLoading, isAuthenticated, user, router]);

  return null;
}
