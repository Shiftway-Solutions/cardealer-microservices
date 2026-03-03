/**
 * Stripe Webhook API Route
 *
 * Handle Stripe payment webhooks
 */

import { NextRequest, NextResponse } from 'next/server';

const _STRIPE_WEBHOOK_SECRET = process.env.STRIPE_WEBHOOK_SECRET;
const BACKEND_URL = process.env.BACKEND_API_URL || 'http://localhost:8080';

export async function POST(request: NextRequest) {
  try {
    const body = await request.text();
    const signature = request.headers.get('stripe-signature');

    if (!signature) {
      return NextResponse.json({ error: 'Missing Stripe signature' }, { status: 400 });
    }

    // In production, verify the signature with Stripe SDK
    // For now, forward to backend for processing
    const response = await fetch(`${BACKEND_URL}/api/webhooks/stripe`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'stripe-signature': signature,
      },
      body,
    });

    if (!response.ok) {
      console.error('Backend webhook processing failed:', response.status);
      return NextResponse.json({ error: 'Webhook processing failed' }, { status: 500 });
    }

    return NextResponse.json({ received: true });
  } catch (error) {
    console.error('Stripe webhook error:', error);
    return NextResponse.json({ error: 'Webhook handler failed' }, { status: 500 });
  }
}
