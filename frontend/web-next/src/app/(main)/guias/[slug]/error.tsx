'use client';

import { useEffect } from 'react';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { AlertTriangle, RefreshCw, ArrowLeft } from 'lucide-react';

export default function GuideError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error('Guide page error:', error);
  }, [error]);

  return (
    <div className="bg-background flex min-h-[60vh] items-center justify-center px-4">
      <div className="max-w-md text-center">
        <div className="mx-auto mb-6 flex h-20 w-20 items-center justify-center rounded-full bg-red-50">
          <AlertTriangle className="h-10 w-10 text-red-500" />
        </div>

        <h1 className="text-foreground mb-2 text-2xl font-bold">No pudimos cargar esta guía</h1>

        <p className="text-muted-foreground mb-6">
          Puede que la guía haya sido removida o haya un problema temporal con nuestro servidor.
          Intenta de nuevo en unos segundos.
        </p>

        {error.digest && (
          <p className="text-muted-foreground/70 mb-6 font-mono text-xs">Ref: {error.digest}</p>
        )}

        <div className="flex flex-col justify-center gap-3 sm:flex-row">
          <Button onClick={reset} className="bg-[#00A870] hover:bg-[#007850]">
            <RefreshCw className="mr-2 h-4 w-4" />
            Intentar de nuevo
          </Button>

          <Button variant="outline" asChild>
            <Link href="/guias">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Volver a guías
            </Link>
          </Button>
        </div>
      </div>
    </div>
  );
}
