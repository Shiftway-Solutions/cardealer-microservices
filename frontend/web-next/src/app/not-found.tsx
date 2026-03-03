import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Search, Home } from 'lucide-react';

export default function NotFound() {
  return (
    <div className="bg-muted/50 flex min-h-screen items-center justify-center px-4">
      <div className="max-w-lg text-center">
        {/* 404 Number */}
        <div className="relative mb-6">
          <h1 className="text-muted-foreground/20 text-[150px] leading-none font-bold select-none">
            404
          </h1>
          <div className="absolute inset-0 flex items-center justify-center">
            <div className="bg-primary/10 flex h-24 w-24 items-center justify-center rounded-full">
              <Search className="text-primary h-12 w-12" />
            </div>
          </div>
        </div>

        <h2 className="text-foreground mb-3 text-2xl font-bold">Página no encontrada</h2>

        <p className="text-muted-foreground mb-8">
          Lo sentimos, no pudimos encontrar la página que buscas. Es posible que haya sido movida o
          eliminada.
        </p>

        <div className="flex flex-col justify-center gap-3 sm:flex-row">
          <Button variant="default" asChild>
            <Link href="/">
              <Home className="mr-2 h-4 w-4" />
              Ir al inicio
            </Link>
          </Button>

          <Button variant="outline" asChild>
            <Link href="/vehiculos">
              <Search className="mr-2 h-4 w-4" />
              Buscar vehículos
            </Link>
          </Button>
        </div>

        {/* Suggestions */}
        <div className="border-border mt-12 border-t pt-8">
          <p className="text-muted-foreground mb-4 text-sm">¿Buscabas algo de esto?</p>
          <div className="flex flex-wrap justify-center gap-2">
            <Link
              href="/vehiculos"
              className="bg-primary/10 text-primary rounded-full px-3 py-1 text-sm hover:underline"
            >
              Ver vehículos
            </Link>
            <Link
              href="/dealers"
              className="bg-primary/10 text-primary rounded-full px-3 py-1 text-sm hover:underline"
            >
              Dealers
            </Link>
            <Link
              href="/buscar"
              className="bg-primary/10 text-primary rounded-full px-3 py-1 text-sm hover:underline"
            >
              Búsqueda avanzada
            </Link>
            <Link
              href="/ayuda"
              className="bg-primary/10 text-primary rounded-full px-3 py-1 text-sm hover:underline"
            >
              Centro de ayuda
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
