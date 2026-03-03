/**
 * Help Category Page
 *
 * Shows help articles by category
 */

import Link from 'next/link';
import { Card, CardContent } from '@/components/ui/card';
import { ChevronLeft, FileText, Clock, ChevronRight } from 'lucide-react';

// Mock data
const categories: Record<string, { title: string; description: string }> = {
  compradores: {
    title: 'Guía para Compradores',
    description: 'Todo lo que necesitas saber para comprar tu próximo vehículo',
  },
  vendedores: {
    title: 'Guía para Vendedores',
    description: 'Aprende a vender tu vehículo de forma efectiva',
  },
  dealers: {
    title: 'Portal de Dealers',
    description: 'Recursos para concesionarios y dealers profesionales',
  },
  pagos: {
    title: 'Pagos y Facturación',
    description: 'Información sobre métodos de pago, reembolsos y facturación',
  },
  seguridad: {
    title: 'Seguridad',
    description: 'Protege tu cuenta y aprende a identificar fraudes',
  },
  cuenta: {
    title: 'Mi Cuenta',
    description: 'Gestiona tu perfil, preferencias y configuración',
  },
};

const mockArticles = [
  {
    id: '1',
    slug: 'como-buscar-vehiculos',
    title: '¿Cómo buscar vehículos?',
    excerpt: 'Aprende a usar los filtros avanzados para encontrar el vehículo perfecto.',
    readTime: 3,
  },
  {
    id: '2',
    slug: 'contactar-vendedor',
    title: '¿Cómo contactar a un vendedor?',
    excerpt: 'Envía mensajes y agenda citas fácilmente desde la plataforma.',
    readTime: 2,
  },
  {
    id: '3',
    slug: 'favoritos-alertas',
    title: 'Guardar favoritos y crear alertas',
    excerpt: 'No te pierdas ninguna oportunidad con las alertas de precio.',
    readTime: 4,
  },
  {
    id: '4',
    slug: 'verificar-vehiculo',
    title: '¿Cómo verificar un vehículo?',
    excerpt: 'Consejos para inspeccionar y verificar el estado del vehículo.',
    readTime: 5,
  },
  {
    id: '5',
    slug: 'negociar-precio',
    title: 'Tips para negociar el precio',
    excerpt: 'Estrategias efectivas para obtener el mejor precio.',
    readTime: 6,
  },
];

export default async function HelpCategoryPage({
  params,
}: {
  params: Promise<{ category: string }>;
}) {
  const { category } = await params;
  const categoryInfo = categories[category] || {
    title: category.charAt(0).toUpperCase() + category.slice(1),
    description: 'Artículos de ayuda',
  };

  return (
    <div className="min-h-screen bg-muted/50 py-8">
      <div className="container max-w-4xl">
        {/* Breadcrumb */}
        <div className="mb-6">
          <Link href="/ayuda" className="flex items-center text-primary hover:text-primary">
            <ChevronLeft className="mr-1 h-4 w-4" />
            Volver al Centro de Ayuda
          </Link>
        </div>

        {/* Header */}
        <div className="mb-8">
          <h1 className="mb-2 text-3xl font-bold">{categoryInfo.title}</h1>
          <p className="text-muted-foreground">{categoryInfo.description}</p>
        </div>

        {/* Articles */}
        <div className="space-y-4">
          {mockArticles.map(article => (
            <Link key={article.id} href={`/ayuda/articulo/${article.slug}`}>
              <Card className="cursor-pointer transition-shadow hover:shadow-md">
                <CardContent className="p-6">
                  <div className="flex items-start justify-between">
                    <div className="flex gap-4">
                      <div className="rounded-lg bg-primary/10 p-2">
                        <FileText className="h-5 w-5 text-primary" />
                      </div>
                      <div>
                        <h3 className="mb-1 font-semibold text-foreground">{article.title}</h3>
                        <p className="mb-2 text-sm text-muted-foreground">{article.excerpt}</p>
                        <div className="flex items-center text-xs text-muted-foreground">
                          <Clock className="mr-1 h-3 w-3" />
                          {article.readTime} min de lectura
                        </div>
                      </div>
                    </div>
                    <ChevronRight className="h-5 w-5 text-muted-foreground" />
                  </div>
                </CardContent>
              </Card>
            </Link>
          ))}
        </div>

        {/* Related Categories */}
        <div className="mt-12">
          <h2 className="mb-4 text-lg font-semibold">Otras categorías</h2>
          <div className="grid grid-cols-2 gap-4 md:grid-cols-3">
            {Object.entries(categories)
              .filter(([key]) => key !== category)
              .slice(0, 3)
              .map(([key, value]) => (
                <Link key={key} href={`/ayuda/${key}`}>
                  <Card className="h-full cursor-pointer transition-shadow hover:shadow-md">
                    <CardContent className="p-4">
                      <h3 className="text-sm font-medium">{value.title}</h3>
                    </CardContent>
                  </Card>
                </Link>
              ))}
          </div>
        </div>
      </div>
    </div>
  );
}
