/**
 * Blog Page
 *
 * Route: /blog
 */

import Link from 'next/link';
import { ArrowRight, Rss } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { blogPosts, blogCategories } from './blog-data';

export const metadata = {
  title: 'Blog | OKLA - Noticias del Mercado Automotriz RD',
  description:
    'Noticias, consejos y tendencias del mercado automotriz de República Dominicana. El blog oficial de OKLA.',
};

export default function BlogPage() {
  return (
    <div className="min-h-screen">
      {/* Hero */}
      <section className="border-border border-b bg-gradient-to-br from-slate-50 to-white py-12 dark:from-slate-900 dark:to-slate-800">
        <div className="container mx-auto px-4">
          <div className="flex items-center justify-between">
            <div>
              <div className="mb-2 flex items-center gap-2">
                <Rss className="text-primary h-5 w-5" />
                <span className="text-primary text-sm font-medium">Blog OKLA</span>
              </div>
              <h1 className="text-foreground text-3xl font-bold md:text-4xl">
                Noticias y Consejos Automotrices
              </h1>
              <p className="text-muted-foreground mt-2">
                El mejor contenido sobre el mercado automotriz dominicano
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* Main Content */}
      <section className="py-12">
        <div className="container mx-auto px-4">
          <div className="flex flex-col gap-8 lg:flex-row">
            {/* Posts */}
            <div className="flex-1">
              <div className="grid gap-5 sm:grid-cols-2">
                {blogPosts.map(post => (
                  <Link key={post.slug} href={`/blog/${post.slug}`}>
                    <Card className="border-border hover:border-primary group h-full cursor-pointer transition-colors">
                      <CardContent className="pt-5">
                        <div className="mb-3 flex items-center justify-between">
                          <Badge variant="secondary">{post.category}</Badge>
                          <span className="text-muted-foreground text-xs">
                            {post.readTime} lectura
                          </span>
                        </div>
                        <h2 className="text-foreground group-hover:text-primary line-clamp-2 font-semibold transition-colors">
                          {post.title}
                        </h2>
                        <p className="text-muted-foreground mt-2 line-clamp-2 text-sm">
                          {post.excerpt}
                        </p>
                        <div className="mt-4 flex items-center justify-between">
                          <span className="text-muted-foreground text-xs">{post.date}</span>
                          <span className="text-primary flex items-center gap-1 text-xs font-medium">
                            Leer más
                            <ArrowRight className="h-3 w-3" />
                          </span>
                        </div>
                      </CardContent>
                    </Card>
                  </Link>
                ))}
              </div>
            </div>

            {/* Sidebar */}
            <aside className="w-full lg:w-64">
              {/* Categories */}
              <div className="border-border bg-card rounded-lg border p-5 shadow-sm">
                <h3 className="text-foreground mb-4 font-semibold">Categorías</h3>
                <div className="space-y-1">
                  {blogCategories.map((cat, index) => (
                    <button
                      key={index}
                      className="hover:bg-muted flex w-full items-center justify-between rounded-md px-3 py-2 text-sm transition-colors"
                    >
                      <span className="text-foreground">{cat.name}</span>
                      <span className="text-muted-foreground bg-muted rounded-full px-2 py-0.5 text-xs">
                        {cat.count}
                      </span>
                    </button>
                  ))}
                </div>
              </div>

              {/* Newsletter */}
              <div className="border-primary/20 bg-primary/5 mt-4 rounded-lg border p-5">
                <h3 className="text-foreground font-semibold">Suscríbete</h3>
                <p className="text-muted-foreground mt-1 text-sm">
                  Recibe los mejores artículos directamente en tu correo.
                </p>
                <Button asChild className="bg-primary hover:bg-primary/90 mt-3 w-full" size="sm">
                  <Link href="/contacto">Suscribirse</Link>
                </Button>
              </div>
            </aside>
          </div>
        </div>
      </section>
    </div>
  );
}
