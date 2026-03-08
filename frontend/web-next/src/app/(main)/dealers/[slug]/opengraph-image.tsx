/**
 * Dynamic OpenGraph Image for Dealer Profile Pages
 *
 * Generates a branded 1200×630 OG image per dealer for social sharing.
 * Fetches the dealer data + banner/logo and renders a branded overlay
 * with OKLA logo, dealer name, location, rating, and verification badge.
 *
 * This file-based convention is auto-discovered by Next.js App Router —
 * no manual <meta property="og:image"> needed in generateMetadata.
 */

import { ImageResponse } from 'next/og';

export const runtime = 'edge';
export const alt = 'Concesionario en OKLA';
export const size = { width: 1200, height: 630 };
export const contentType = 'image/png';

const GATEWAY_URL =
  process.env.NEXT_PUBLIC_API_URL || process.env.GATEWAY_INTERNAL_URL || 'http://gateway:8080';

interface DealerOGData {
  businessName: string;
  logoUrl: string | null;
  bannerUrl: string | null;
  city: string;
  province: string;
  type: string;
  verificationStatus: string;
  rating: number | null;
  reviewCount: number | null;
  currentActiveListings: number;
}

async function fetchDealerForOG(slug: string): Promise<DealerOGData | null> {
  try {
    const res = await fetch(`${GATEWAY_URL}/api/dealers/slug/${slug}`, {
      next: { revalidate: 300 },
    });

    if (!res.ok) return null;

    const data = await res.json();
    const dealer = data.data || data;

    return {
      businessName: dealer.businessName || 'Concesionario',
      logoUrl: dealer.logoUrl || null,
      bannerUrl: dealer.bannerUrl || null,
      city: dealer.city || '',
      province: dealer.province || 'República Dominicana',
      type: dealer.type || 'Independent',
      verificationStatus: dealer.verificationStatus || 'Pending',
      rating: dealer.rating ?? null,
      reviewCount: dealer.reviewCount ?? null,
      currentActiveListings: dealer.currentActiveListings || 0,
    };
  } catch {
    return null;
  }
}

function getDealerTypeLabel(type: string): string {
  const labels: Record<string, string> = {
    Independent: 'Independiente',
    Chain: 'Cadena',
    Franchise: 'Franquicia',
    Boutique: 'Boutique',
  };
  return labels[type] || type;
}

function formatRating(rating: number | null): string {
  if (rating === null || rating === 0) return 'Sin calificación';
  return `★ ${rating.toFixed(1)}`;
}

export default async function Image({ params }: { params: { slug: string } }) {
  const dealer = await fetchDealerForOG(params.slug);

  // Fallback: generic OKLA branded image if dealer not found
  if (!dealer) {
    return new ImageResponse(
      <div
        style={{
          background: 'linear-gradient(135deg, #00A870 0%, #009663 50%, #007a4f 100%)',
          width: '100%',
          height: '100%',
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          fontFamily: 'system-ui, -apple-system, sans-serif',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '20px' }}>
          <div
            style={{
              width: '80px',
              height: '80px',
              background: 'white',
              borderRadius: '20px',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontSize: '48px',
              fontWeight: 'bold',
              color: '#00A870',
            }}
          >
            O
          </div>
          <span style={{ fontSize: '64px', fontWeight: 'bold', color: 'white' }}>OKLA</span>
        </div>
        <div style={{ fontSize: '28px', color: 'rgba(255,255,255,0.9)', marginTop: '20px' }}>
          Concesionario no disponible
        </div>
      </div>,
      { ...size }
    );
  }

  const isVerified = dealer.verificationStatus === 'Verified';

  // Main OG image with dealer data
  return new ImageResponse(
    <div
      style={{
        width: '100%',
        height: '100%',
        display: 'flex',
        position: 'relative',
        fontFamily: 'system-ui, -apple-system, sans-serif',
      }}
    >
      {/* Background: dealer banner or gradient */}
      {dealer.bannerUrl ? (
        <img
          src={dealer.bannerUrl}
          alt=""
          style={{
            position: 'absolute',
            width: '100%',
            height: '100%',
            objectFit: 'cover',
          }}
        />
      ) : (
        <div
          style={{
            position: 'absolute',
            width: '100%',
            height: '100%',
            background: 'linear-gradient(135deg, #1a1a2e 0%, #16213e 50%, #0f3460 100%)',
          }}
        />
      )}

      {/* Dark overlay for readability */}
      <div
        style={{
          position: 'absolute',
          width: '100%',
          height: '100%',
          background:
            'linear-gradient(to top, rgba(0,0,0,0.85) 0%, rgba(0,0,0,0.4) 40%, rgba(0,0,0,0.2) 100%)',
        }}
      />

      {/* Top bar: OKLA logo + dealer type badge */}
      <div
        style={{
          position: 'absolute',
          top: '0',
          left: '0',
          right: '0',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          padding: '28px 40px',
        }}
      >
        {/* Logo */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          <div
            style={{
              width: '44px',
              height: '44px',
              background: '#00A870',
              borderRadius: '12px',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontSize: '26px',
              fontWeight: 'bold',
              color: 'white',
            }}
          >
            O
          </div>
          <span
            style={{
              fontSize: '28px',
              fontWeight: 'bold',
              color: 'white',
              letterSpacing: '-0.5px',
            }}
          >
            OKLA
          </span>
        </div>

        {/* Dealer type badge */}
        <div style={{ display: 'flex', gap: '10px' }}>
          {isVerified && (
            <div
              style={{
                background: '#00A870',
                color: 'white',
                padding: '8px 20px',
                borderRadius: '999px',
                fontSize: '18px',
                fontWeight: '600',
                display: 'flex',
                alignItems: 'center',
                gap: '6px',
              }}
            >
              ✓ Verificado
            </div>
          )}
          <div
            style={{
              background: '#3b82f6',
              color: 'white',
              padding: '8px 20px',
              borderRadius: '999px',
              fontSize: '18px',
              fontWeight: '600',
            }}
          >
            {getDealerTypeLabel(dealer.type)}
          </div>
        </div>
      </div>

      {/* Center: Dealer logo */}
      {dealer.logoUrl && (
        <div
          style={{
            position: 'absolute',
            top: '50%',
            left: '50%',
            transform: 'translate(-50%, -70%)',
            display: 'flex',
          }}
        >
          <img
            src={dealer.logoUrl}
            alt=""
            style={{
              width: '120px',
              height: '120px',
              borderRadius: '24px',
              border: '4px solid rgba(255,255,255,0.3)',
              objectFit: 'cover',
            }}
          />
        </div>
      )}

      {/* Bottom content: name, location, stats */}
      <div
        style={{
          position: 'absolute',
          bottom: '0',
          left: '0',
          right: '0',
          padding: '0 40px 36px 40px',
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        {/* Dealer Name */}
        <div
          style={{
            fontSize: '44px',
            fontWeight: 'bold',
            color: 'white',
            lineHeight: '1.15',
            marginBottom: '12px',
            textShadow: '0 2px 4px rgba(0,0,0,0.5)',
          }}
        >
          {dealer.businessName}
        </div>

        {/* Details row */}
        <div
          style={{
            display: 'flex',
            gap: '24px',
            alignItems: 'center',
          }}
        >
          {/* Location */}
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '6px',
              background: 'rgba(255,255,255,0.15)',
              padding: '8px 16px',
              borderRadius: '999px',
            }}
          >
            <span style={{ fontSize: '16px', color: 'rgba(255,255,255,0.9)' }}>📍</span>
            <span style={{ fontSize: '16px', color: 'rgba(255,255,255,0.9)', fontWeight: '500' }}>
              {dealer.city}
              {dealer.province ? `, ${dealer.province}` : ''}
            </span>
          </div>

          {/* Rating */}
          {dealer.rating !== null && dealer.rating > 0 && (
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '6px',
                background: 'rgba(255,255,255,0.15)',
                padding: '8px 16px',
                borderRadius: '999px',
              }}
            >
              <span style={{ fontSize: '16px', color: '#fbbf24' }}>★</span>
              <span style={{ fontSize: '16px', color: 'rgba(255,255,255,0.9)', fontWeight: '500' }}>
                {dealer.rating.toFixed(1)}
                {dealer.reviewCount ? ` (${dealer.reviewCount})` : ''}
              </span>
            </div>
          )}

          {/* Inventory count */}
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '6px',
              background: 'rgba(255,255,255,0.15)',
              padding: '8px 16px',
              borderRadius: '999px',
            }}
          >
            <span style={{ fontSize: '16px', color: 'rgba(255,255,255,0.9)' }}>🚗</span>
            <span style={{ fontSize: '16px', color: 'rgba(255,255,255,0.9)', fontWeight: '500' }}>
              {dealer.currentActiveListings} vehículos
            </span>
          </div>

          {/* okla.com.do watermark */}
          <div
            style={{
              marginLeft: 'auto',
              fontSize: '16px',
              color: 'rgba(255,255,255,0.5)',
              fontWeight: '400',
            }}
          >
            okla.com.do
          </div>
        </div>
      </div>
    </div>,
    { ...size }
  );
}
