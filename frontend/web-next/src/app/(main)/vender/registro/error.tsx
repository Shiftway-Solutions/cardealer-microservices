'use client';

import { AlertCircle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import Link from 'next/link';

export default function Error({
  error: _error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  return (
    <div className="flex min-h-[60vh] items-center justify-center px-4">
      <div className="mx-auto max-w-md text-center">
        <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-red-100">
          <AlertCircle className="h-6 w-6 text-red-600" />
        </div>
        <h2 className="mb-2 text-xl font-semibold text-gray-900">
          Algo salió mal
        </h2>
        <p className="mb-6 text-gray-500">
          Ocurrió un error inesperado. Por favor intenta de nuevo.
        </p>
        <div className="flex flex-col gap-3 sm:flex-row sm:justify-center">
          <Button onClick={reset} className="bg-[#00A870] hover:bg-[#009663]">
            Intentar de nuevo
          </Button>
          <Button asChild variant="outline">
            <Link href="/vender">Volver a vender</Link>
          </Button>
        </div>
      </div>
    </div>
  );
}
