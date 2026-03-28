/**
 * 403 Forbidden Page (Server Component)
 *
 * Shown when user doesn't have permission to access a resource.
 * Server component for minimal JS bundle on error pages.
 * Only the "Go Back" button is a client island (requires router.back()).
 */

import Link from 'next/link';
import type { Metadata } from 'next';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { ShieldX, Home, LogIn } from 'lucide-react';
import { GoBackButton } from './go-back-button';

export const metadata: Metadata = {
  title: 'Acceso Denegado',
  robots: 'noindex',
};

export default function ForbiddenPage() {
  return (
    <div className="bg-muted/50 flex min-h-screen items-center justify-center p-4">
      <Card className="w-full max-w-md text-center">
        <CardContent className="pt-10 pb-8">
          {/* Icon */}
          <div className="mx-auto mb-6 flex h-20 w-20 items-center justify-center rounded-full bg-red-100">
            <ShieldX className="h-10 w-10 text-red-600" />
          </div>

          {/* Title */}
          <h1 className="text-foreground mb-2 text-2xl font-bold">Acceso Denegado</h1>

          {/* Description */}
          <p className="text-muted-foreground mb-8">
            No tienes permiso para acceder a esta página. Si crees que esto es un error, por favor
            contacta a soporte.
          </p>

          {/* Error Code */}
          <p className="mb-8 text-6xl font-bold text-gray-200">403</p>

          {/* Actions */}
          <div className="flex flex-col justify-center gap-3 sm:flex-row">
            <GoBackButton />
            <Button className="bg-primary hover:bg-primary/90" asChild>
              <Link href="/">
                <Home className="mr-2 h-4 w-4" />
                Ir al Inicio
              </Link>
            </Button>
          </div>

          {/* Additional Help */}
          <div className="border-border mt-8 border-t pt-6">
            <p className="text-muted-foreground mb-4 text-sm">¿Necesitas acceso a esta función?</p>
            <div className="flex justify-center gap-4 text-sm">
              <Link href="/login" className="text-primary flex items-center gap-1 hover:underline">
                <LogIn className="h-3 w-3" />
                Iniciar Sesión
              </Link>
              <Link href="/contacto" className="text-primary hover:underline">
                Contactar Soporte
              </Link>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
