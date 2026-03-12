/**
 * Stripe Client — Singleton loader for @stripe/stripe-js
 *
 * Uses the public key from env to lazily load Stripe.js.
 * Import getStripe() wherever you need the Stripe instance.
 */

import { loadStripe, type Stripe } from '@stripe/stripe-js';

let stripePromise: Promise<Stripe | null> | null = null;

export function getStripe(): Promise<Stripe | null> {
  if (!stripePromise) {
    const key = process.env.NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY;
    if (!key) {
      console.warn('[Stripe] NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY not set');
      return Promise.resolve(null);
    }
    stripePromise = loadStripe(key);
  }
  return stripePromise;
}
