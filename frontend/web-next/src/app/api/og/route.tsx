/**
 * Open Graph Image API Route
 *
 * Generate dynamic OG images for social sharing
 */

import { ImageResponse } from 'next/og';
import { NextRequest } from 'next/server';

export const runtime = 'edge';

export async function GET(request: NextRequest) {
  try {
    const { searchParams } = new URL(request.url);

    const title = searchParams.get('title') || 'OKLA';
    const description = searchParams.get('description') || 'Tu marketplace de vehículos';
    const type = searchParams.get('type') || 'default';
    const price = searchParams.get('price');
    const year = searchParams.get('year');
    const _image = searchParams.get('image');

    // Vehicle OG Image
    if (type === 'vehicle') {
      return new ImageResponse(
        <div
          style={{
            height: '100%',
            width: '100%',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            backgroundColor: '#0f172a',
            backgroundImage: 'linear-gradient(135deg, #0f172a 0%, #1e3a5f 100%)',
          }}
        >
          <div
            style={{
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              padding: '40px',
            }}
          >
            {/* Logo */}
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                marginBottom: '20px',
              }}
            >
              <div
                style={{
                  width: '50px',
                  height: '50px',
                  backgroundColor: '#10b981',
                  borderRadius: '10px',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  marginRight: '15px',
                }}
              >
                <svg width="30" height="30" viewBox="0 0 24 24" fill="white">
                  <path d="M19 17h2c.6 0 1-.4 1-1v-3c0-.9-.7-1.7-1.5-1.9L18 10V5c0-.6-.4-1-1-1H7c-.6 0-1 .4-1 1v5l-2.5 1.1c-.8.2-1.5 1-1.5 1.9v3c0 .6.4 1 1 1h2c0 1.7 1.3 3 3 3s3-1.3 3-3h2c0 1.7 1.3 3 3 3s3-1.3 3-3z" />
                </svg>
              </div>
              <span style={{ color: 'white', fontSize: '32px', fontWeight: 'bold' }}>OKLA</span>
            </div>

            {/* Vehicle Title */}
            <h1
              style={{
                color: 'white',
                fontSize: '48px',
                fontWeight: 'bold',
                textAlign: 'center',
                marginBottom: '10px',
              }}
            >
              {title}
            </h1>

            {/* Year & Price */}
            <div
              style={{
                display: 'flex',
                gap: '20px',
                marginTop: '20px',
              }}
            >
              {year && <span style={{ color: '#94a3b8', fontSize: '24px' }}>{year}</span>}
              {price && (
                <span style={{ color: '#10b981', fontSize: '32px', fontWeight: 'bold' }}>
                  RD$ {price}
                </span>
              )}
            </div>
          </div>
        </div>,
        {
          width: 1200,
          height: 630,
        }
      );
    }

    // Default OG Image
    return new ImageResponse(
      <div
        style={{
          height: '100%',
          width: '100%',
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          backgroundColor: '#0f172a',
          backgroundImage: 'linear-gradient(135deg, #10b981 0%, #0d9488 100%)',
        }}
      >
        <div
          style={{
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            padding: '40px',
          }}
        >
          {/* Logo */}
          <div
            style={{
              width: '100px',
              height: '100px',
              backgroundColor: 'white',
              borderRadius: '20px',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              marginBottom: '30px',
            }}
          >
            <svg width="60" height="60" viewBox="0 0 24 24" fill="#10b981">
              <path d="M19 17h2c.6 0 1-.4 1-1v-3c0-.9-.7-1.7-1.5-1.9L18 10V5c0-.6-.4-1-1-1H7c-.6 0-1 .4-1 1v5l-2.5 1.1c-.8.2-1.5 1-1.5 1.9v3c0 .6.4 1 1 1h2c0 1.7 1.3 3 3 3s3-1.3 3-3h2c0 1.7 1.3 3 3 3s3-1.3 3-3z" />
            </svg>
          </div>

          {/* Title */}
          <h1
            style={{
              color: 'white',
              fontSize: '64px',
              fontWeight: 'bold',
              textAlign: 'center',
              marginBottom: '20px',
            }}
          >
            {title}
          </h1>

          {/* Description */}
          <p
            style={{
              color: 'rgba(255, 255, 255, 0.8)',
              fontSize: '24px',
              textAlign: 'center',
              maxWidth: '800px',
            }}
          >
            {description}
          </p>
        </div>
      </div>,
      {
        width: 1200,
        height: 630,
      }
    );
  } catch (error) {
    console.error('OG Image generation error:', error);
    return new Response('Failed to generate image', { status: 500 });
  }
}
