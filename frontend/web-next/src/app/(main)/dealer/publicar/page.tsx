/**
 * Dealer Vehicle Publishing Page
 *
 * Smart Vehicle Publishing Wizard with VIN decode, CSV import, and batch tools
 * Upgraded to SmartPublishWizard - February 2026
 * KYC-gated: dealer must be verified before publishing
 */

'use client';

import Link from 'next/link';
import { Shield, Clock, AlertCircle } from 'lucide-react';
import { SmartPublishWizard } from '@/components/vehicles/smart-publish';
import { useCurrentDealer } from '@/hooks/use-dealers';
import { useCanSell } from '@/hooks/use-kyc';
import { Button } from '@/components/ui/button';

export default function DealerPublicarPage() {
  const { data: dealer, isLoading: isDealerLoading } = useCurrentDealer();
  const {
    canSell,
    isPending,
    isRejected,
    needsVerification,
    isLoading: isKycLoading,
    rejectionReason,
  } = useCanSell();

  const isLoading = isDealerLoading || isKycLoading;

  // Show loading state while KYC + dealer profile loads
  if (isLoading) {
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
          <Link href="/dealer">
            <Button variant="outline">Volver al dashboard</Button>
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
            Tu solicitud de verificación fue rechazada. Debes volver a verificar para poder publicar
            vehículos.
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
            <Link href="/dealer">
              <Button variant="outline">Volver</Button>
            </Link>
          </div>
        </div>
      </div>
    );
  }

  // Not verified — prompt to start verification
  if (needsVerification || !canSell) {
    return (
      <div className="bg-muted/50 flex min-h-[500px] items-center justify-center">
        <div className="mx-auto max-w-md px-4 text-center">
          <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-blue-100">
            <Shield className="h-8 w-8 text-blue-600" />
          </div>
          <h2 className="mb-2 text-xl font-bold text-gray-900">Verificación requerida</h2>
          <p className="mb-6 text-gray-600">
            Para publicar vehículos como dealer en OKLA debes verificar tu identidad primero. El
            proceso toma menos de 5 minutos y es completamente seguro.
          </p>
          <div className="flex justify-center gap-3">
            <Link href="/cuenta/verificacion">
              <Button className="gap-2 bg-blue-600 hover:bg-blue-700">
                <Shield className="h-4 w-4" />
                Verificar ahora
              </Button>
            </Link>
            <Link href="/dealer">
              <Button variant="outline">Volver</Button>
            </Link>
          </div>
        </div>
      </div>
    );
  }

  // Verified — show publish wizard
  return (
    <div className="bg-muted/50 min-h-screen">
      <div className="mx-auto max-w-4xl px-4 py-8">
        {/* Header */}
        <div className="mb-8 text-center">
          <h1 className="text-foreground mb-2 text-3xl font-bold">Publicar Vehículo</h1>
          <p className="text-muted-foreground">
            {dealer?.businessName
              ? `Publicando como ${dealer.businessName}`
              : 'Publica vehículos con VIN, escaneo o importación CSV'}
          </p>
        </div>

        {/* Smart Publish Wizard - Dealer Mode */}
        <SmartPublishWizard mode="dealer" dealerId={dealer?.id} />
      </div>
    </div>
  );
}
