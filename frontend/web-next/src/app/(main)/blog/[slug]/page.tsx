/**
 * Individual Blog Post Page
 *
 * Route: /blog/[slug]
 * SSG with generateStaticParams for all posts
 */

import Image from 'next/image';
import Link from 'next/link';
import { notFound } from 'next/navigation';
import type { Metadata } from 'next';
import { ArrowLeft, Calendar, Clock, User, ChevronRight } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { blogPosts, getPostBySlug, getRelatedPosts } from '../blog-data';
import { ShareButtons } from './share-buttons';

// =============================================================================
// STATIC GENERATION
// =============================================================================

export function generateStaticParams() {
  return blogPosts.map(post => ({
    slug: post.slug,
  }));
}

export async function generateMetadata({
  params,
}: {
  params: Promise<{ slug: string }>;
}): Promise<Metadata> {
  const { slug } = await params;
  const post = getPostBySlug(slug);
  if (!post) return { title: 'Artículo no encontrado' };

  const url = `https://okla.com.do/blog/${slug}`;

  return {
    title: `${post.title}`,
    description: post.excerpt,
    keywords: [post.category.toLowerCase(), 'OKLA', 'vehículos RD', 'mercado automotriz'],
    openGraph: {
      title: post.title,
      description: post.excerpt,
      url,
      images: [{ url: post.imageUrl, width: 1200, height: 600, alt: post.title }],
      type: 'article',
      publishedTime: post.isoDate,
      authors: [post.author],
      locale: 'es_DO',
      siteName: 'OKLA',
    },
    twitter: {
      card: 'summary_large_image',
      title: post.title,
      description: post.excerpt,
      images: [post.imageUrl],
    },
    alternates: { canonical: url },
  };
}

// =============================================================================
// PAGE COMPONENT
// =============================================================================

export default async function BlogPostPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const post = getPostBySlug(slug);
  if (!post) notFound();

  const relatedPosts = getRelatedPosts(slug, 3);

  // JSON-LD Article schema
  const articleJsonLd = {
    '@context': 'https://schema.org',
    '@type': 'Article',
    headline: post.title,
    description: post.excerpt,
    image: post.imageUrl,
    datePublished: post.isoDate,
    author: {
      '@type': 'Organization',
      name: post.author,
      url: 'https://okla.com.do',
    },
    publisher: {
      '@type': 'Organization',
      name: 'OKLA',
      url: 'https://okla.com.do',
      logo: {
        '@type': 'ImageObject',
        url: 'https://okla.com.do/logo.png',
      },
    },
    mainEntityOfPage: {
      '@type': 'WebPage',
      '@id': `https://okla.com.do/blog/${slug}`,
    },
  };

  // Parse content into paragraphs and headings
  const contentSections = post.content.split('\n\n').filter(Boolean);

  // Safe React-based bold text renderer (replaces dangerouslySetInnerHTML)
  function renderBoldText(text: string): React.ReactNode[] {
    const parts = text.split(/\*\*(.*?)\*\*/g);
    return parts.map((part, i) =>
      i % 2 === 1 ? (
        <strong key={i} className="text-foreground font-semibold">
          {part}
        </strong>
      ) : (
        <span key={i}>{part}</span>
      )
    );
  }

  return (
    <div className="min-h-screen">
      {/* JSON-LD */}
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(articleJsonLd) }}
      />

      {/* Breadcrumbs */}
      <nav className="border-border border-b" aria-label="Breadcrumb">
        <div className="container mx-auto px-4 py-3">
          <ol className="text-muted-foreground flex items-center gap-1 text-sm">
            <li>
              <Link href="/" className="hover:text-primary transition-colors">
                Inicio
              </Link>
            </li>
            <li>
              <ChevronRight className="h-3.5 w-3.5" />
            </li>
            <li>
              <Link href="/blog" className="hover:text-primary transition-colors">
                Blog
              </Link>
            </li>
            <li>
              <ChevronRight className="h-3.5 w-3.5" />
            </li>
            <li className="text-foreground line-clamp-1 font-medium">{post.title}</li>
          </ol>
        </div>
      </nav>

      {/* Hero Image */}
      <div className="relative h-64 w-full overflow-hidden bg-gradient-to-br from-slate-100 to-slate-200 md:h-96 dark:from-slate-800 dark:to-slate-900">
        <Image
          src={post.imageUrl}
          alt={post.title}
          fill
          className="object-cover"
          priority
          sizes="100vw"
        />
        <div className="absolute inset-0 bg-gradient-to-t from-black/60 to-transparent" />
        <div className="absolute right-0 bottom-0 left-0 p-6 md:p-10">
          <div className="container mx-auto">
            <Badge className="bg-primary hover:bg-primary/90 mb-3 text-white">
              {post.category}
            </Badge>
            <h1 className="max-w-3xl text-2xl font-bold text-white md:text-4xl">{post.title}</h1>
          </div>
        </div>
      </div>

      {/* Article Content */}
      <section className="py-10">
        <div className="container mx-auto px-4">
          <div className="flex flex-col gap-10 lg:flex-row">
            {/* Main Content */}
            <article className="min-w-0 flex-1">
              {/* Meta info */}
              <div className="text-muted-foreground mb-8 flex flex-wrap items-center gap-4 text-sm">
                <span className="flex items-center gap-1.5">
                  <User className="h-4 w-4" />
                  {post.author}
                </span>
                <span className="flex items-center gap-1.5">
                  <Calendar className="h-4 w-4" />
                  {post.date}
                </span>
                <span className="flex items-center gap-1.5">
                  <Clock className="h-4 w-4" />
                  {post.readTime} lectura
                </span>
              </div>

              {/* Content — rendered from markdown-like text */}
              <div className="prose prose-slate dark:prose-invert prose-headings:text-foreground prose-p:text-muted-foreground max-w-none">
                {contentSections.map((section, index) => {
                  // H2 headings
                  if (section.startsWith('## ')) {
                    return (
                      <h2 key={index} className="text-foreground mt-8 mb-4 text-2xl font-bold">
                        {section.replace('## ', '')}
                      </h2>
                    );
                  }
                  // H3 headings
                  if (section.startsWith('### ')) {
                    return (
                      <h3 key={index} className="text-foreground mt-6 mb-3 text-xl font-semibold">
                        {section.replace('### ', '')}
                      </h3>
                    );
                  }
                  // Table detection (lines with |)
                  if (section.includes('|') && section.includes('---')) {
                    const rows = section.split('\n').filter(r => r.trim() && !r.includes('---'));
                    const headers = rows[0]
                      ?.split('|')
                      .map(h => h.trim())
                      .filter(Boolean);
                    const dataRows = rows.slice(1).map(r =>
                      r
                        .split('|')
                        .map(c => c.trim())
                        .filter(Boolean)
                    );
                    return (
                      <div key={index} className="my-6 overflow-x-auto">
                        <table className="border-border w-full border-collapse border text-sm">
                          <thead>
                            <tr className="bg-muted">
                              {headers?.map((h, i) => (
                                <th
                                  key={i}
                                  className="text-foreground border-border border px-4 py-2 text-left font-semibold"
                                >
                                  {h}
                                </th>
                              ))}
                            </tr>
                          </thead>
                          <tbody>
                            {dataRows.map((row, ri) => (
                              <tr key={ri} className="even:bg-muted/50">
                                {row.map((cell, ci) => (
                                  <td
                                    key={ci}
                                    className="text-muted-foreground border-border border px-4 py-2"
                                  >
                                    {cell}
                                  </td>
                                ))}
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    );
                  }
                  // List items
                  if (section.startsWith('- ') || section.match(/^\d+\./)) {
                    const items = section.split('\n').filter(Boolean);
                    const isOrdered = section.match(/^\d+\./);
                    const ListTag = isOrdered ? 'ol' : 'ul';
                    return (
                      <ListTag key={index} className="text-muted-foreground my-4 space-y-2 pl-6">
                        {items.map((item, i) => (
                          <li key={i} className={isOrdered ? 'list-decimal' : 'list-disc'}>
                            {renderBoldText(item.replace(/^[-\d]+[.)]\s*/, ''))}
                          </li>
                        ))}
                      </ListTag>
                    );
                  }
                  // Regular paragraphs — handle **bold** text safely (no dangerouslySetInnerHTML)
                  return (
                    <p key={index} className="text-muted-foreground my-4 leading-relaxed">
                      {renderBoldText(section)}
                    </p>
                  );
                })}
              </div>

              {/* Share buttons */}
              <div className="border-border mt-10 border-t pt-6">
                <ShareButtons title={post.title} slug={post.slug} />
              </div>

              {/* CTA */}
              <div className="from-primary to-primary/80 mt-8 rounded-xl bg-gradient-to-r p-6 text-white md:p-8">
                <h3 className="text-xl font-bold">¿Buscas tu próximo vehículo?</h3>
                <p className="mt-2 text-white/90">
                  Explora miles de vehículos verificados en el marketplace más completo de República
                  Dominicana.
                </p>
                <div className="mt-4 flex flex-wrap gap-3">
                  <Button asChild className="text-primary bg-white hover:bg-white/90">
                    <Link href="/vehiculos">Explorar Vehículos</Link>
                  </Button>
                  <Button
                    asChild
                    variant="outline"
                    className="border-white text-white hover:bg-white/10"
                  >
                    <Link href="/herramientas">Ver Herramientas</Link>
                  </Button>
                </div>
              </div>

              {/* Back to blog */}
              <div className="mt-8">
                <Button asChild variant="ghost" className="gap-2">
                  <Link href="/blog">
                    <ArrowLeft className="h-4 w-4" />
                    Volver al Blog
                  </Link>
                </Button>
              </div>
            </article>

            {/* Sidebar */}
            <aside className="w-full shrink-0 lg:w-72">
              {/* Related Posts */}
              <div className="border-border bg-card sticky top-24 rounded-lg border p-5 shadow-sm">
                <h3 className="text-foreground mb-4 font-semibold">Artículos Relacionados</h3>
                <div className="space-y-4">
                  {relatedPosts.map(related => (
                    <Link key={related.slug} href={`/blog/${related.slug}`} className="group block">
                      <div className="flex gap-3">
                        <div className="h-16 w-16 flex-shrink-0 overflow-hidden rounded-md">
                          <Image
                            src={related.imageUrl}
                            alt={related.title}
                            width={64}
                            height={64}
                            sizes="64px"
                            className="h-full w-full object-cover transition-transform group-hover:scale-105"
                          />
                        </div>
                        <div className="min-w-0 flex-1">
                          <Badge variant="secondary" className="mb-1 text-[10px]">
                            {related.category}
                          </Badge>
                          <h4 className="text-foreground group-hover:text-primary line-clamp-2 text-sm font-medium transition-colors">
                            {related.title}
                          </h4>
                          <span className="text-muted-foreground text-xs">{related.date}</span>
                        </div>
                      </div>
                    </Link>
                  ))}
                </div>

                <div className="mt-5 border-t pt-4">
                  <Button asChild variant="outline" className="w-full" size="sm">
                    <Link href="/blog">Ver Todos los Artículos</Link>
                  </Button>
                </div>
              </div>
            </aside>
          </div>
        </div>
      </section>
    </div>
  );
}
