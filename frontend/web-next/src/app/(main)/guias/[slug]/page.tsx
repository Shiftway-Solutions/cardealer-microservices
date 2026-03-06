/**
 * Individual Guide Page (SSG)
 *
 * Route: /guias/[slug]
 */

import type { Metadata } from 'next';
import Link from 'next/link';
import { notFound } from 'next/navigation';
import { ChevronRight, ArrowLeft, Clock, BookOpen } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { guides, getGuideBySlug, getAllGuideSlugs } from '../guide-data';

// =============================================================================
// SSG
// =============================================================================

export function generateStaticParams() {
  return getAllGuideSlugs().map(slug => ({ slug }));
}

// =============================================================================
// METADATA
// =============================================================================

export async function generateMetadata({
  params,
}: {
  params: Promise<{ slug: string }>;
}): Promise<Metadata> {
  const { slug } = await params;
  const guide = getGuideBySlug(slug);
  if (!guide) return { title: 'Guía no encontrada' };

  const url = `https://okla.com.do/guias/${guide.slug}`;

  return {
    title: `${guide.title} | Guías OKLA`,
    description: guide.description,
    keywords: guide.topics,
    openGraph: {
      title: guide.title,
      description: guide.description,
      url,
      type: 'article',
      locale: 'es_DO',
      siteName: 'OKLA',
    },
    twitter: {
      card: 'summary_large_image',
      title: guide.title,
      description: guide.description,
    },
    alternates: { canonical: url },
  };
}

// =============================================================================
// CONTENT RENDERER
// =============================================================================

function renderContent(content: string) {
  const lines = content.split('\n');
  const elements: React.ReactNode[] = [];
  let inTable = false;
  let tableRows: string[][] = [];
  let tableHeaders: string[] = [];

  const flushTable = () => {
    if (tableHeaders.length > 0) {
      elements.push(
        <div key={`table-${elements.length}`} className="my-6 overflow-x-auto">
          <table className="w-full border-collapse text-sm">
            <thead>
              <tr className="border-border border-b-2">
                {tableHeaders.map((h, i) => (
                  <th key={i} className="text-foreground px-4 py-2 text-left font-semibold">
                    {h.trim()}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {tableRows.map((row, ri) => (
                <tr key={ri} className="border-border border-b">
                  {row.map((cell, ci) => (
                    <td key={ci} className="text-muted-foreground px-4 py-2">
                      {renderInline(cell.trim())}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      );
    }
    tableHeaders = [];
    tableRows = [];
    inTable = false;
  };

  const renderInline = (text: string): React.ReactNode => {
    const parts = text.split(/(\*\*[^*]+\*\*)/g);
    return parts.map((part, i) => {
      if (part.startsWith('**') && part.endsWith('**')) {
        return (
          <strong key={i} className="text-foreground font-semibold">
            {part.slice(2, -2)}
          </strong>
        );
      }
      return part;
    });
  };

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];

    // Table detection
    if (line.includes('|') && line.trim().startsWith('|')) {
      const cells = line.split('|').filter(c => c.trim() !== '');

      if (!inTable) {
        inTable = true;
        tableHeaders = cells;
        continue;
      }

      // Skip separator row
      if (cells.every(c => /^[\s-:]+$/.test(c))) continue;

      tableRows.push(cells);
      continue;
    } else if (inTable) {
      flushTable();
    }

    // Headers
    if (line.startsWith('### ')) {
      elements.push(
        <h3 key={i} className="text-foreground mt-8 mb-3 text-xl font-semibold">
          {line.replace('### ', '')}
        </h3>
      );
    } else if (line.startsWith('## ')) {
      elements.push(
        <h2 key={i} className="text-foreground mt-10 mb-4 text-2xl font-bold">
          {line.replace('## ', '')}
        </h2>
      );
    } else if (line.startsWith('- **')) {
      elements.push(
        <li key={i} className="text-muted-foreground mb-1 ml-4 list-disc">
          {renderInline(line.replace(/^- /, ''))}
        </li>
      );
    } else if (line.startsWith('- ')) {
      elements.push(
        <li key={i} className="text-muted-foreground mb-1 ml-4 list-disc">
          {renderInline(line.replace('- ', ''))}
        </li>
      );
    } else if (/^\d+\.\s/.test(line)) {
      elements.push(
        <li key={i} className="text-muted-foreground mb-1 ml-4 list-decimal">
          {renderInline(line.replace(/^\d+\.\s/, ''))}
        </li>
      );
    } else if (line.startsWith('**') && line.endsWith('**')) {
      elements.push(
        <p key={i} className="text-foreground mt-4 mb-2 font-semibold">
          {line.slice(2, -2)}
        </p>
      );
    } else if (line.trim() === '') {
      // Skip empty lines
    } else {
      elements.push(
        <p key={i} className="text-muted-foreground mb-3 leading-relaxed">
          {renderInline(line)}
        </p>
      );
    }
  }

  if (inTable) flushTable();

  return elements;
}

// =============================================================================
// MAIN COMPONENT
// =============================================================================

export default async function GuiaDetailPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const guide = getGuideBySlug(slug);
  if (!guide) notFound();

  // Related guides (exclude current)
  const relatedGuides = guides.filter(g => g.slug !== guide.slug).slice(0, 3);

  // JSON-LD
  const jsonLd = {
    '@context': 'https://schema.org',
    '@type': 'HowTo',
    name: guide.title,
    description: guide.description,
    totalTime: `PT${parseInt(guide.readTime)}M`,
    dateModified: guide.lastUpdated,
    publisher: {
      '@type': 'Organization',
      name: 'OKLA',
      url: 'https://okla.com.do',
    },
  };

  return (
    <div className="min-h-screen">
      {/* JSON-LD */}
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
      />

      {/* Breadcrumbs */}
      <nav className="border-border bg-muted/30 border-b py-3">
        <div className="text-muted-foreground container mx-auto flex items-center gap-2 px-4 text-sm">
          <Link href="/" className="hover:text-foreground">
            Inicio
          </Link>
          <ChevronRight className="h-3 w-3" />
          <Link href="/guias" className="hover:text-foreground">
            Guías
          </Link>
          <ChevronRight className="h-3 w-3" />
          <span className="text-foreground">{guide.title}</span>
        </div>
      </nav>

      {/* Header */}
      <section className="bg-gradient-to-br from-[#00A870] to-[#007850] py-12 text-white">
        <div className="container mx-auto px-4">
          <div className="mx-auto max-w-3xl">
            <div className="mb-4 flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-full bg-white/20">
                <BookOpen className="h-5 w-5" />
              </div>
              <div className="flex items-center gap-2 text-sm text-white/80">
                <Clock className="h-4 w-4" />
                <span>{guide.readTime} de lectura</span>
              </div>
            </div>
            <h1 className="text-3xl font-bold md:text-4xl">{guide.title}</h1>
            <p className="mt-3 text-lg text-white/90">{guide.description}</p>
            <div className="mt-4 flex flex-wrap gap-2">
              {guide.topics.map((topic, i) => (
                <span
                  key={i}
                  className="rounded-full bg-white/20 px-3 py-1 text-xs font-medium backdrop-blur-sm"
                >
                  {topic}
                </span>
              ))}
            </div>
          </div>
        </div>
      </section>

      {/* Content */}
      <div className="container mx-auto px-4 py-12">
        <div className="mx-auto grid max-w-6xl gap-10 lg:grid-cols-[1fr_300px]">
          {/* Article */}
          <article className="prose-equivalent max-w-none">{renderContent(guide.content)}</article>

          {/* Sidebar */}
          <aside className="space-y-6">
            {/* CTA */}
            <Card className="border-[#00A870]/30 bg-gradient-to-br from-[#00A870]/5 to-[#007850]/5">
              <CardContent className="pt-6 text-center">
                <h3 className="text-foreground font-semibold">¿Listo para buscar?</h3>
                <p className="text-muted-foreground mt-2 text-sm">
                  Encuentra tu próximo vehículo con las herramientas de OKLA.
                </p>
                <Button asChild className="mt-4 w-full bg-[#00A870] hover:bg-[#007850]">
                  <Link href="/vehiculos">Ver vehículos</Link>
                </Button>
              </CardContent>
            </Card>

            {/* Related Guides */}
            <div>
              <h3 className="text-foreground mb-3 font-semibold">Otras guías</h3>
              <div className="space-y-3">
                {relatedGuides.map(g => (
                  <Link
                    key={g.slug}
                    href={`/guias/${g.slug}`}
                    className="group border-border hover:border-primary block rounded-lg border p-3 transition-colors"
                  >
                    <h4 className="text-foreground group-hover:text-primary text-sm font-medium">
                      {g.title}
                    </h4>
                    <p className="text-muted-foreground mt-1 text-xs">{g.readTime} de lectura</p>
                  </Link>
                ))}
              </div>
            </div>

            {/* Tools */}
            <div>
              <h3 className="text-foreground mb-3 font-semibold">Herramientas útiles</h3>
              <div className="space-y-2">
                <Button asChild variant="outline" className="w-full justify-start" size="sm">
                  <Link href="/herramientas/calculadora-financiamiento">
                    💰 Calculadora de financiamiento
                  </Link>
                </Button>
                <Button asChild variant="outline" className="w-full justify-start" size="sm">
                  <Link href="/herramientas/calculadora-importacion">
                    🚢 Calculadora de importación
                  </Link>
                </Button>
                <Button asChild variant="outline" className="w-full justify-start" size="sm">
                  <Link href="/comparar">⚖️ Comparar vehículos</Link>
                </Button>
              </div>
            </div>
          </aside>
        </div>
      </div>

      {/* Back */}
      <div className="border-border border-t py-8">
        <div className="container mx-auto px-4">
          <Button asChild variant="ghost">
            <Link href="/guias" className="gap-2">
              <ArrowLeft className="h-4 w-4" />
              Volver a Guías
            </Link>
          </Button>
        </div>
      </div>
    </div>
  );
}
