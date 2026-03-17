/**
 * PayPal Webhook API Route
 *
 * Receives PayPal IPN/webhook notifications and forwards to the backend
 * for full signature verification via PayPal's verification API.
 */

import { NextRequest, NextResponse } from 'next/server';

const BACKEND_URL = process.env.BACKEND_API_URL || 'http://localhost:8080';

export async function POST(request: NextRequest) {
  try {
    const body = await request.text();

    // Extract PayPal verification headers
    const paypalHeaders: Record<string, string> = {};
    for (const headerName of [
      'PAYPAL-AUTH-ALGO',
      'PAYPAL-CERT-URL',
      'PAYPAL-TRANSMISSION-ID',
      'PAYPAL-TRANSMISSION-SIG',
      'PAYPAL-TRANSMISSION-TIME',
    ]) {
      const value = request.headers.get(headerName);
      if (value) {
        paypalHeaders[headerName] = value;
      }
    }

    // Require at least the transmission ID header
    if (!paypalHeaders['PAYPAL-TRANSMISSION-ID']) {
      return NextResponse.json({ error: 'Missing PayPal headers' }, { status: 400 });
    }

    // Forward to backend with raw body + PayPal headers for verification
    const response = await fetch(`${BACKEND_URL}/api/webhooks/paypal`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...paypalHeaders,
      },
      body,
    });

    if (!response.ok) {
      console.error('[PayPal Webhook] Backend processing failed:', response.status);
      return NextResponse.json({ error: 'Webhook processing failed' }, { status: 500 });
    }

    return NextResponse.json({ received: true });
  } catch (error) {
    console.error('[PayPal Webhook] Error:', error instanceof Error ? error.message : error);
    return NextResponse.json({ error: 'Webhook handler failed' }, { status: 500 });
  }
}
