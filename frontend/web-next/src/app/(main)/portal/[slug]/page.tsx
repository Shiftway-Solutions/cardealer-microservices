/**
 * Public Dealer Portal Page
 *
 * Premium branded page for each dealer showing their full inventory,
 * brand identity, chatbot, and shareable link.
 * Available for all dealers; advanced features gated by subscription plan.
 */

import { Metadata } from 'next';
import { notFound } from 'next/navigation';
import { DealerPortalClient } from './portal-client';

// ============================================================
// METADATA
// ============================================================

interface PortalPageProps {
  params: Promise<{ slug: string }>;
}

export async function generateMetadata({ params }: PortalPageProps): Promise<Metadata> {
  const { slug } = await params;
  const baseUrl = process.env.NEXT_PUBLIC_APP_URL || 'https://okla.com.do';

  // Fetch dealer info server-side for SEO
  let dealerName = slug.replace(/-/g, ' ');
  let description = `Explora el inventario completo de ${dealerName} en OKLA — República Dominicana.`;

  try {
    const gatewayUrl = process.env.GATEWAY_INTERNAL_URL || process.env.NEXT_PUBLIC_API_URL || '';
    const res = await fetch(`${gatewayUrl}/api/dealers/slug/${slug}`, {
      next: { revalidate: 300 },
    });
    if (res.ok) {
      const data = await res.json();
      const dealer = data.data || data;
      dealerName = dealer.businessName || dealerName;
      description =
        dealer.description ||
        `Visita el portal de ${dealerName} en OKLA. Vehículos verificados, financiamiento y más.`;
    }
  } catch {
    // Use defaults
  }

  return {
    title: `${dealerName} | Portal de Dealer — OKLA`,
    description,
    openGraph: {
      title: `${dealerName} — Portal Exclusivo en OKLA`,
      description,
      url: `${baseUrl}/portal/${slug}`,
      siteName: 'OKLA',
      type: 'website',
    },
    twitter: {
      card: 'summary_large_image',
      title: `${dealerName} — OKLA`,
      description,
    },
  };
}

// ============================================================
// PAGE
// ============================================================

export default async function DealerPortalPage({ params }: PortalPageProps) {
  const { slug } = await params;

  // Server-side fetch for ISR
  let dealer = null;
  try {
    const gatewayUrl = process.env.GATEWAY_INTERNAL_URL || process.env.NEXT_PUBLIC_API_URL || '';
    const res = await fetch(`${gatewayUrl}/api/dealers/slug/${slug}`, {
      next: { revalidate: 300 },
    });
    if (res.ok) {
      const data = await res.json();
      dealer = data.data || data;
    }
  } catch {
    // Will show 404
  }

  if (!dealer) {
    notFound();
  }

  return <DealerPortalClient slug={slug} initialDealer={dealer} />;
}
