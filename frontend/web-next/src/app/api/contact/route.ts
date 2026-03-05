/**
 * Contact Form BFF Route
 *
 * Forwards contact form submissions to the ContactService via the internal gateway.
 * This is a public endpoint (no auth required) since visitors can contact without an account.
 *
 * @route POST /api/contact
 */

import { NextRequest, NextResponse } from 'next/server';

const INTERNAL_API_URL = process.env.INTERNAL_API_URL || 'http://gateway:8080';

interface ContactPayload {
  name: string;
  email: string;
  phone?: string;
  subject: string;
  message: string;
}

export async function POST(request: NextRequest) {
  try {
    const body = (await request.json()) as ContactPayload;

    // Basic server-side validation
    if (!body.name || !body.email || !body.subject || !body.message) {
      return NextResponse.json(
        { success: false, error: 'Todos los campos requeridos deben ser completados.' },
        { status: 400 }
      );
    }

    if (body.message.length > 5000) {
      return NextResponse.json(
        { success: false, error: 'El mensaje no puede exceder 5000 caracteres.' },
        { status: 400 }
      );
    }

    // Forward to ContactService via gateway
    // Security Note: This is a server-to-server call (Next.js BFF → Gateway).
    // CSRF protection is handled at the browser → Next.js boundary by the
    // CSRF middleware. Internal gateway calls don't need CSRF tokens.
    const response = await fetch(`${INTERNAL_API_URL}/api/contactrequests`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        // Forward auth header if present (logged-in users)
        ...(request.headers.get('authorization')
          ? { Authorization: request.headers.get('authorization')! }
          : {}),
      },
      body: JSON.stringify({
        buyerName: body.name,
        buyerEmail: body.email,
        buyerPhone: body.phone || null,
        subject: body.subject,
        message: body.message,
        // Public contact form — no specific vehicle/seller context
        vehicleId: '00000000-0000-0000-0000-000000000000',
        sellerId: '00000000-0000-0000-0000-000000000000',
      }),
      signal: AbortSignal.timeout(15000),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => null);
      const errorMessage =
        errorData?.error || errorData?.title || 'Error al enviar el mensaje. Intenta de nuevo.';
      return NextResponse.json(
        { success: false, error: errorMessage },
        { status: response.status }
      );
    }

    const data = await response.json();
    return NextResponse.json({
      success: true,
      data,
      message: 'Mensaje enviado exitosamente.',
    });
  } catch (error) {
    console.error('Contact API error:', error);
    return NextResponse.json(
      { success: false, error: 'Error interno del servidor. Intenta de nuevo más tarde.' },
      { status: 500 }
    );
  }
}
