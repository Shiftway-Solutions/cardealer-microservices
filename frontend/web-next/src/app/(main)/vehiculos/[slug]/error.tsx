'use client';

import { useEffect } from 'react';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { AlertTriangle, RefreshCw, ArrowLeft, Search } from 'lucide-react';

export default function VehicleDetailError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    if (process.env.NODE_ENV === 'development') console.error('Vehicle detail error:', error);
  }, [error]);

  return (
    <div className="bg-background flex min-h-[60vh] items-center justify-center px-4">
      <div className="max-w-md text-center">
        <div className="mx-auto mb-6 flex h-20 w-20 items-center justify-center rounded-full bg-red-50">
          <AlertTriangle className="h-10 w-10 text-red-500" />
        </div>

        <h1 className="text-foreground mb-2 text-2xl font-bold">No pudimos cargar este vehículo</h1>

        <p className="text-muted-foreground mb-6">
          Puede que la publicación haya sido removida o haya un problema temporal con nuestro
          servidor. Intenta de nuevo en unos segundos.
        </p>

        {error.digest && (
          <p className="text-muted-foreground/70 mb-6 font-mono text-xs">Ref: {error.digest}</p>
        )}

        <div className="flex flex-col justify-center gap-3 sm:flex-row">
          <Button onClick={reset} className="bg-primary hover:bg-primary/80">
            <RefreshCw className="mr-2 h-4 w-4" />
            Intentar de nuevo
          </Button>

          <Button variant="outline" asChild>
            <Link href="/vehiculos">
              <Search className="mr-2 h-4 w-4" />
              Buscar otros vehículos
            </Link>
          </Button>
        </div>

        <div className="mt-6">
          <Link
            href="/"
            className="text-muted-foreground hover:text-foreground inline-flex items-center gap-1 text-sm"
          >
            <ArrowLeft className="h-3 w-3" />
            Volver al inicio
          </Link>
        </div>
      </div>
    </div>
  );
}
