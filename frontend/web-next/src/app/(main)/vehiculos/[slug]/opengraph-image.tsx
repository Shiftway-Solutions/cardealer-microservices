/**
 * Dynamic OpenGraph Image for Vehicle Detail Pages
 *
 * Generates a branded 1200×630 OG image per vehicle for social sharing.
 * Fetches the vehicle data + primary photo and renders a branded overlay
 * with OKLA logo, vehicle title, price, year, and location.
 *
 * This file-based convention is auto-discovered by Next.js App Router —
 * no manual <meta property="og:image"> needed in generateMetadata.
 */

import { ImageResponse } from 'next/og';

export const runtime = 'edge';
export const alt = 'Vehículo en OKLA';
export const size = { width: 1200, height: 630 };
export const contentType = 'image/png';

const GATEWAY_URL =
  process.env.NEXT_PUBLIC_API_URL || process.env.GATEWAY_INTERNAL_URL || 'http://gateway:8080';

interface VehicleOGData {
  title: string;
  make: string;
  model: string;
  year: number;
  price: number;
  currency: string;
  imageUrl: string | null;
  condition: string;
  location: string;
  mileage: number;
}

async function fetchVehicleForOG(slug: string): Promise<VehicleOGData | null> {
  try {
    const res = await fetch(`${GATEWAY_URL}/api/vehicles/slug/${slug}`, {
      next: { revalidate: 300 },
    });

    if (!res.ok) return null;

    const data = await res.json();
    const vehicle = data.data || data;

    const primaryImage =
      vehicle.images?.find((img: { isPrimary: boolean }) => img.isPrimary) ?? vehicle.images?.[0];

    return {
      title: vehicle.title || `${vehicle.year} ${vehicle.make} ${vehicle.model}`,
      make: vehicle.make || '',
      model: vehicle.model || '',
      year: vehicle.year || 0,
      price: vehicle.price || 0,
      currency: vehicle.currency || 'DOP',
      imageUrl: primaryImage?.url || null,
      condition: vehicle.condition === 'new' ? 'Nuevo' : 'Usado',
      location: vehicle.location?.city || vehicle.city || 'República Dominicana',
      mileage: vehicle.mileage || 0,
    };
  } catch {
    return null;
  }
}

function formatPrice(price: number, currency: string): string {
  if (currency === 'USD') {
    return `US$ ${price.toLocaleString('en-US')}`;
  }
  return `RD$ ${price.toLocaleString('es-DO')}`;
}

function formatMileage(mileage: number): string {
  if (mileage === 0) return '0 km';
  return `${mileage.toLocaleString('es-DO')} km`;
}

export default async function Image({ params }: { params: { slug: string } }) {
  const vehicle = await fetchVehicleForOG(params.slug);

  // Fallback: generic OKLA branded image if vehicle not found
  if (!vehicle) {
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
          Vehículo no disponible
        </div>
      </div>,
      { ...size }
    );
  }

  // Main OG image with vehicle data
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
      {/* Background: vehicle image or gradient */}
      {vehicle.imageUrl ? (
        <img
          src={vehicle.imageUrl}
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
            background: 'linear-gradient(135deg, #1a1a2e 0%, #16213e 100%)',
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
            'linear-gradient(to top, rgba(0,0,0,0.85) 0%, rgba(0,0,0,0.3) 50%, rgba(0,0,0,0.15) 100%)',
        }}
      />

      {/* Top bar: OKLA logo + condition badge */}
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

        {/* Condition badge */}
        <div
          style={{
            background: vehicle.condition === 'Nuevo' ? '#00A870' : '#3b82f6',
            color: 'white',
            padding: '8px 20px',
            borderRadius: '999px',
            fontSize: '18px',
            fontWeight: '600',
          }}
        >
          {vehicle.condition}
        </div>
      </div>

      {/* Bottom content: title, price, details */}
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
        {/* Vehicle Title */}
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
          {vehicle.year} {vehicle.make} {vehicle.model}
        </div>

        {/* Price */}
        <div
          style={{
            fontSize: '36px',
            fontWeight: 'bold',
            color: '#00A870',
            marginBottom: '16px',
            textShadow: '0 2px 4px rgba(0,0,0,0.5)',
          }}
        >
          {formatPrice(vehicle.price, vehicle.currency)}
        </div>

        {/* Details row */}
        <div
          style={{
            display: 'flex',
            gap: '24px',
            alignItems: 'center',
          }}
        >
          {/* Mileage */}
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
            <span style={{ fontSize: '16px', color: 'rgba(255,255,255,0.9)' }}>📏</span>
            <span style={{ fontSize: '16px', color: 'rgba(255,255,255,0.9)', fontWeight: '500' }}>
              {formatMileage(vehicle.mileage)}
            </span>
          </div>

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
              {vehicle.location}
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
