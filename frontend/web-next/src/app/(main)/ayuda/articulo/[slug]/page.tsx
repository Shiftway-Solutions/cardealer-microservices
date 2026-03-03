/**
 * Help Article Page
 *
 * Individual help article with content
 */

import Link from 'next/link';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import {
  ChevronLeft,
  Clock,
  ThumbsUp,
  ThumbsDown,
  Share2,
  Printer,
  MessageCircle,
} from 'lucide-react';

// Mock article data
const mockArticle = {
  slug: 'como-buscar-vehiculos',
  title: '¿Cómo buscar vehículos en OKLA?',
  category: 'compradores',
  categoryName: 'Guía para Compradores',
  readTime: 5,
  lastUpdated: '2024-02-10',
  content: `
## Introducción

OKLA te ofrece herramientas poderosas para encontrar el vehículo perfecto. En esta guía aprenderás a utilizar todas las funcionalidades de búsqueda.

## Búsqueda básica

1. **Desde la página principal**: Usa la barra de búsqueda en el hero para escribir marca, modelo o cualquier característica.

2. **Filtros rápidos**: Selecciona tipo de vehículo (Sedán, SUV, Pickup, etc.) para filtrar resultados.

## Filtros avanzados

En la página de búsqueda encontrarás filtros para:

- **Precio**: Establece un rango mínimo y máximo
- **Año**: Filtra por año de fabricación
- **Kilometraje**: Encuentra vehículos con bajo millaje
- **Ubicación**: Busca en tu provincia o cerca de ti
- **Transmisión**: Automático, manual o CVT
- **Combustible**: Gasolina, diésel, híbrido, eléctrico

## Ordenar resultados

Puedes ordenar los resultados por:
- Más recientes
- Menor precio
- Mayor precio
- Menor kilometraje

## Guardar búsquedas

¿No encontraste lo que buscabas? Guarda tu búsqueda y recibe alertas cuando se publiquen nuevos vehículos que coincidan con tus criterios.

## Consejos adicionales

- Usa el **comparador** para evaluar hasta 3 vehículos lado a lado
- Guarda tus favoritos con el ícono de corazón
- Configura **alertas de precio** para recibir notificaciones de bajadas

¿Tienes más preguntas? Contacta a nuestro equipo de soporte.
  `,
  relatedArticles: [
    { slug: 'contactar-vendedor', title: '¿Cómo contactar a un vendedor?' },
    { slug: 'favoritos-alertas', title: 'Guardar favoritos y crear alertas' },
    { slug: 'verificar-vehiculo', title: '¿Cómo verificar un vehículo?' },
  ],
};

export default async function HelpArticlePage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug: _slug } = await params;
  // In real app, fetch article by slug
  const article = mockArticle;

  return (
    <div className="bg-muted/50 min-h-screen py-8">
      <div className="container max-w-4xl">
        {/* Breadcrumb */}
        <div className="mb-6 flex items-center gap-2 text-sm">
          <Link href="/ayuda" className="text-primary hover:text-primary">
            Centro de Ayuda
          </Link>
          <span className="text-muted-foreground">/</span>
          <Link href={`/ayuda/${article.category}`} className="text-primary hover:text-primary">
            {article.categoryName}
          </Link>
          <span className="text-muted-foreground">/</span>
          <span className="text-muted-foreground truncate">{article.title}</span>
        </div>

        <div className="grid grid-cols-1 gap-8 lg:grid-cols-4">
          {/* Main Content */}
          <div className="lg:col-span-3">
            <Card>
              <CardContent className="p-8">
                {/* Header */}
                <div className="mb-6">
                  <h1 className="mb-3 text-2xl font-bold">{article.title}</h1>
                  <div className="text-muted-foreground flex items-center gap-4 text-sm">
                    <span className="flex items-center gap-1">
                      <Clock className="h-4 w-4" />
                      {article.readTime} min de lectura
                    </span>
                    <span>
                      Actualizado: {new Date(article.lastUpdated).toLocaleDateString('es-DO')}
                    </span>
                  </div>
                </div>

                {/* Article Content */}
                <div className="prose prose-green max-w-none">
                  {article.content.split('\n').map((line, i) => {
                    if (line.startsWith('## ')) {
                      return (
                        <h2 key={i} className="mt-6 mb-3 text-xl font-semibold">
                          {line.replace('## ', '')}
                        </h2>
                      );
                    }
                    if (line.startsWith('- ')) {
                      return (
                        <li key={i} className="ml-4">
                          {line.replace('- ', '')}
                        </li>
                      );
                    }
                    if (line.match(/^\d\./)) {
                      return (
                        <p key={i} className="mb-2 ml-4">
                          {line}
                        </p>
                      );
                    }
                    if (line.trim()) {
                      return (
                        <p key={i} className="text-foreground mb-3">
                          {line}
                        </p>
                      );
                    }
                    return null;
                  })}
                </div>

                {/* Actions */}
                <div className="border-border mt-8 flex items-center justify-between border-t pt-6">
                  <div className="flex items-center gap-4">
                    <span className="text-muted-foreground text-sm">
                      ¿Te fue útil este artículo?
                    </span>
                    <Button variant="outline" size="sm">
                      <ThumbsUp className="mr-1 h-4 w-4" />
                      Sí
                    </Button>
                    <Button variant="outline" size="sm">
                      <ThumbsDown className="mr-1 h-4 w-4" />
                      No
                    </Button>
                  </div>
                  <div className="flex items-center gap-2">
                    <Button variant="ghost" size="sm">
                      <Share2 className="h-4 w-4" />
                    </Button>
                    <Button variant="ghost" size="sm">
                      <Printer className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* Related Articles */}
            <div className="mt-8">
              <h2 className="mb-4 text-lg font-semibold">Artículos relacionados</h2>
              <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
                {article.relatedArticles.map(related => (
                  <Link key={related.slug} href={`/ayuda/articulo/${related.slug}`}>
                    <Card className="h-full cursor-pointer transition-shadow hover:shadow-md">
                      <CardContent className="p-4">
                        <p className="text-sm font-medium">{related.title}</p>
                      </CardContent>
                    </Card>
                  </Link>
                ))}
              </div>
            </div>
          </div>

          {/* Sidebar */}
          <div className="lg:col-span-1">
            <Card className="sticky top-4">
              <CardContent className="p-6">
                <h3 className="mb-4 font-semibold">¿Necesitas más ayuda?</h3>
                <p className="text-muted-foreground mb-4 text-sm">
                  Si no encontraste lo que buscabas, nuestro equipo está disponible para ayudarte.
                </p>
                <Button className="bg-primary hover:bg-primary/90 w-full">
                  <MessageCircle className="mr-2 h-4 w-4" />
                  Contactar Soporte
                </Button>
              </CardContent>
            </Card>
          </div>
        </div>

        {/* Back Button */}
        <div className="mt-8">
          <Link href="/ayuda">
            <Button variant="outline">
              <ChevronLeft className="mr-1 h-4 w-4" />
              Volver al Centro de Ayuda
            </Button>
          </Link>
        </div>
      </div>
    </div>
  );
}
