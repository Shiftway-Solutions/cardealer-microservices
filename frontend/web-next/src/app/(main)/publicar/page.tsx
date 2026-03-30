/**
 * Publicar Vehículo Page
 *
 * Smart Vehicle Publishing Wizard with VIN decode support
 * Upgraded to SmartPublishWizard - February 2026
 * KYC-gated: user must be verified to publish
 */

'use client';

import Link from 'next/link';
import { Shield, Clock, AlertCircle, User } from 'lucide-react';
import { SmartPublishWizard } from '@/components/vehicles/smart-publish';
import { useCanSell } from '@/hooks/use-kyc';
import { useSellerByUserId } from '@/hooks/use-seller';
import { useAuth } from '@/hooks/use-auth';
import { Button } from '@/components/ui/button';
import { Suspense } from 'react';
import { OnboardingBanner } from '@/components/onboarding/onboarding-banner';

export default function PublicarPage() {
  const { user } = useAuth();
  const { canSell, isPending, isRejected, needsVerification, isLoading, rejectionReason } =
    useCanSell();

  // For seller account type, also check if seller profile is configured
  const isSeller = user?.accountType === 'seller';
  const sellerQuery = useSellerByUserId(isSeller ? user?.id : undefined);
  // In development, skip seller profile check when UserService may be unavailable
  const sellerProfileMissing =
    process.env.NODE_ENV !== 'development' &&
    isSeller &&
    !sellerQuery.isLoading &&
    canSell &&
    (!sellerQuery.data || !sellerQuery.data.fullName);

  // Show loading state while KYC + seller profile data loads
  if (isLoading || (isSeller && sellerQuery.isLoading)) {
    return (
      <div className="flex min-h-[400px] items-center justify-center">
        <div className="h-8 w-8 animate-spin rounded-full border-2 border-emerald-600 border-t-transparent" />
      </div>
    );
  }

  // Verification pending / under review
  if (isPending) {
    return (
      <div className="bg-muted/50 flex min-h-[500px] items-center justify-center">
        <div className="mx-auto max-w-md px-4 text-center">
          <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-purple-100">
            <Clock className="h-8 w-8 text-purple-600" />
          </div>
          <h2 className="mb-2 text-xl font-bold text-gray-900">Verificación en proceso</h2>
          <p className="mb-6 text-gray-600">
            Tu solicitud de verificación está siendo revisada por nuestro equipo. Te notificaremos
            cuando sea aprobada.
          </p>
          <Link href="/cuenta">
            <Button variant="outline">Volver a mi portal</Button>
          </Link>
        </div>
      </div>
    );
  }

  // Verification rejected — prompt to retry
  if (isRejected) {
    return (
      <div className="bg-muted/50 flex min-h-[500px] items-center justify-center">
        <div className="mx-auto max-w-md px-4 text-center">
          <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-red-100">
            <AlertCircle className="h-8 w-8 text-red-600" />
          </div>
          <h2 className="mb-2 text-xl font-bold text-gray-900">Verificación rechazada</h2>
          <p className="mb-3 text-gray-600">
            Tu solicitud de verificación fue rechazada. Debes volver a verificar tu identidad para
            poder publicar vehículos.
          </p>
          {rejectionReason && (
            <p className="mb-4 rounded-lg bg-red-50 px-4 py-2 text-sm text-red-700">
              <strong>Motivo:</strong> {rejectionReason}
            </p>
          )}
          <div className="flex justify-center gap-3">
            <Link href="/cuenta/verificacion">
              <Button className="gap-2">
                <Shield className="h-4 w-4" />
                Verificar de nuevo
              </Button>
            </Link>
            <Link href="/cuenta">
              <Button variant="outline">Volver</Button>
            </Link>
          </div>
        </div>
      </div>
    );
  }

  // Not verified — prompt to verify first
  if (needsVerification) {
    return (
      <div className="bg-muted/50 flex min-h-[500px] items-center justify-center">
        <div className="mx-auto max-w-md px-4 text-center">
          <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-blue-100">
            <Shield className="h-8 w-8 text-blue-600" />
          </div>
          <h2 className="mb-2 text-xl font-bold text-gray-900">Verificación requerida</h2>
          <p className="mb-6 text-gray-600">
            Para publicar vehículos en OKLA necesitas verificar tu identidad primero. El proceso
            toma menos de 5 minutos y es completamente seguro.
          </p>
          <div className="flex justify-center gap-3">
            <Link href="/cuenta/verificacion">
              <Button className="gap-2 bg-blue-600 hover:bg-blue-700">
                <Shield className="h-4 w-4" />
                Verificar ahora
              </Button>
            </Link>
            <Link href="/cuenta">
              <Button variant="outline">Volver</Button>
            </Link>
          </div>
        </div>
      </div>
    );
  }

  // Seller profile not configured — prompt to complete it before publishing
  if (sellerProfileMissing) {
    return (
      <div className="bg-muted/50 flex min-h-[500px] items-center justify-center">
        <div className="mx-auto max-w-md px-4 text-center">
          <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-amber-100">
            <User className="h-8 w-8 text-amber-600" />
          </div>
          <h2 className="mb-2 text-xl font-bold text-gray-900">Perfil de vendedor requerido</h2>
          <p className="mb-6 text-gray-600">
            Para publicar vehículos debes completar tu perfil de vendedor primero. Tu información
            aparecerá públicamente en tus anuncios.
          </p>
          <div className="flex justify-center gap-3">
            <Link href="/cuenta/perfil">
              <Button className="gap-2 bg-amber-600 hover:bg-amber-700">
                <User className="h-4 w-4" />
                Completar perfil
              </Button>
            </Link>
            <Link href="/cuenta">
              <Button variant="outline">Volver</Button>
            </Link>
          </div>
        </div>
      </div>
    );
  }

  // Verified + seller profile complete — show publish wizard
  return (
    <div className="bg-muted/50 min-h-screen">
      <div className="mx-auto max-w-4xl px-4 py-8">
        {/* Onboarding banner for first-time dealers */}
        <Suspense fallback={null}>
          <OnboardingBanner />
        </Suspense>

        {/* Header */}
        <div className="mb-8 text-center">
          <h1 className="text-foreground mb-2 text-3xl font-bold">Publicar Vehículo</h1>
          <p className="text-muted-foreground">
            Publica tu vehículo de forma rápida con detección automática por VIN
          </p>
        </div>

        {/* Smart Publish Wizard */}
        <SmartPublishWizard mode="individual" userId={user?.id} />
      </div>
    </div>
  );
}
