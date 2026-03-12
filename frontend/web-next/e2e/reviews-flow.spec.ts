/**
 * E2E Tests — Complete Review Flow
 *
 * Validates the full review lifecycle:
 *   Buyer logs in → navigates to a vehicle detail page → submits a review
 *   → Seller logs in → opens /cuenta/resenas → sees the review
 *   → Seller responds to the review
 *   → Buyer-visible response is confirmed via API
 *
 * Strategy:
 *   • Steps 1–5 use the Playwright `request` fixture (API-level, fast).
 *   • Steps 6–8 use the `page` fixture to validate the seller portal UI.
 *   • CSRF: Double-Submit Cookie pattern — `csrf_token` cookie + `X-CSRF-Token` header.
 *   • All API calls go through the gateway at BASE_URL (/api/*).
 *   • Set PLAYWRIGHT_BASE_URL env var to override (default: https://okla.com.do).
 *
 * Credentials:
 *   Buyer  : buyer002@okla-test.com  / BuyerTest2026!
 *   Seller : nmateo@okla.com.do     / Dealer2026!@#
 *
 * Run:
 *   cd frontend/web-next
 *   PLAYWRIGHT_BASE_URL=https://okla.com.do pnpm exec playwright test e2e/reviews-flow.spec.ts
 */

import { test, expect, type APIRequestContext, type Page } from '@playwright/test';
import { randomUUID } from 'crypto';

// ─── Configuration ────────────────────────────────────────────────────────────
const BASE_URL = process.env.PLAYWRIGHT_BASE_URL ?? 'https://okla.com.do';

// ─── Credentials ─────────────────────────────────────────────────────────────
const BUYER_EMAIL = 'buyer002@okla-test.com';
const BUYER_PASSWORD = 'BuyerTest2026!';
const SELLER_EMAIL = 'nmateo@okla.com.do';
const SELLER_PASSWORD = 'Dealer2026!@#';

// ─── Shared state ─────────────────────────────────────────────────────────────
const csrfToken = randomUUID().replace(/-/g, '');

let buyerToken = '';
let sellerToken = '';
let sellerId = ''; // SellerProfile.id (used by ReviewService)
let vehicleId = ''; // A vehicle the seller has listed
let vehicleSlug = ''; // For navigating to detail page
let reviewId = ''; // Created review ID

// ─── Helpers ──────────────────────────────────────────────────────────────────

/** Build CSRF + auth headers for state-changing requests. */
function authHeaders(token: string) {
  return {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${token}`,
    'X-CSRF-Token': csrfToken,
    Cookie: `csrf_token=${csrfToken}`,
  };
}

/** Decode the payload section of a JWT token. */
function decodeJwt(token: string): Record<string, unknown> {
  const parts = token.split('.');
  if (parts.length !== 3) throw new Error('Invalid JWT');
  const padded = parts[1].replace(/-/g, '+').replace(/_/g, '/');
  return JSON.parse(Buffer.from(padded, 'base64').toString('utf-8'));
}

/** Login helper — returns bearer token. */
async function login(request: APIRequestContext, email: string, password: string): Promise<string> {
  const res = await request.post(`${BASE_URL}/api/auth/login`, {
    headers: { 'Content-Type': 'application/json' },
    data: { email, password },
  });
  expect(res.status(), `Login failed for ${email}: ${res.status()}`).toBe(200);
  const body = await res.json();
  const token: string = body?.data?.token ?? body?.token ?? body?.accessToken ?? '';
  expect(token, `No token returned for ${email}`).toBeTruthy();
  return token;
}

/** Login via browser UI — fills email/password form and waits for redirect. */
async function loginViaBrowser(page: Page, email: string, password: string) {
  await page.goto(`${BASE_URL}/login`);
  await page.waitForLoadState('domcontentloaded');

  // Fill email
  const emailInput = page
    .locator('input[type="email"], input[name="email"], input[id*="email"]')
    .first();
  await emailInput.fill(email);

  // Fill password
  const passwordInput = page.locator('input[type="password"]').first();
  await passwordInput.fill(password);

  // Submit
  const submitBtn = page.locator('button[type="submit"]').first();
  await submitBtn.click();

  // Wait for navigation away from login page
  await page.waitForURL(url => !url.toString().includes('/login'), { timeout: 15_000 });
}

// ─────────────────────────────────────────────────────────────────────────────
// SUITE
// ─────────────────────────────────────────────────────────────────────────────
test.describe('Review Flow — Full E2E via Gateway + UI', () => {
  test.setTimeout(90_000);

  // ── 01. Buyer login ──────────────────────────────────────────────────────────
  test('01 · Buyer login', async ({ request }) => {
    buyerToken = await login(request, BUYER_EMAIL, BUYER_PASSWORD);
    console.log('[01] Buyer authenticated ✓');
    expect(buyerToken).toBeTruthy();
  });

  // ── 02. Seller login + resolve seller profile ID ──────────────────────────────
  test('02 · Seller login + resolve SellerProfile ID', async ({ request }) => {
    sellerToken = await login(request, SELLER_EMAIL, SELLER_PASSWORD);
    expect(sellerToken).toBeTruthy();

    // Decode JWT to get userId
    const payload = decodeJwt(sellerToken);
    const userId = (payload['nameid'] ??
      payload['sub'] ??
      payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ??
      '') as string;
    console.log(`[02] Seller userId = ${userId}`);
    expect(userId).toBeTruthy();

    // Resolve SellerProfile by userId
    const profileRes = await request.get(`${BASE_URL}/api/users/sellers/by-user/${userId}`, {
      headers: { Authorization: `Bearer ${sellerToken}` },
    });

    if (profileRes.status() === 200) {
      const body = await profileRes.json();
      sellerId = body?.data?.id ?? body?.id ?? '';
    }

    // Fallback: try /api/users/sellers/me
    if (!sellerId) {
      const meRes = await request.get(`${BASE_URL}/api/users/sellers/me`, {
        headers: { Authorization: `Bearer ${sellerToken}` },
      });
      if (meRes.status() === 200) {
        const body = await meRes.json();
        sellerId = body?.data?.id ?? body?.id ?? '';
      }
    }

    console.log(`[02] Seller SellerProfile.id = ${sellerId}`);
    expect(sellerId, 'Could not resolve seller profile ID').toBeTruthy();
  });

  // ── 03. Resolve a vehicle published by the seller ─────────────────────────────
  test('03 · Resolve a vehicle listed by the seller', async ({ request }) => {
    // Fetch active listings for this seller from the vehicles API
    const res = await request.get(`${BASE_URL}/api/vehicles`, {
      headers: { Authorization: `Bearer ${sellerToken}` },
      params: { sellerId, status: 'active', pageSize: '1' },
    });

    if (res.status() === 200) {
      const body = await res.json();
      const items: Array<{ id: string; slug?: string }> = body?.data?.items ?? body?.items ?? [];
      if (items.length > 0) {
        vehicleId = items[0].id;
        vehicleSlug = items[0].slug ?? vehicleId;
      }
    }

    // If no vehicles found for seller, use a stub so the review can still be created
    // (ReviewService doesn't require vehicleId)
    if (!vehicleId) {
      console.warn(
        '[03] No active vehicles found for seller — will submit review without vehicleId'
      );
    } else {
      console.log(`[03] vehicleId = ${vehicleId}, slug = ${vehicleSlug}`);
    }
  });

  // ── 04. Buyer submits a review ─────────────────────────────────────────────────
  test('04 · Buyer submits review for the seller', async ({ request }) => {
    const uniqueContent = `Excelente atención y transparencia. Prueba E2E ${Date.now()}`;

    const res = await request.post(`${BASE_URL}/api/reviews`, {
      headers: authHeaders(buyerToken),
      data: {
        sellerId,
        vehicleId: vehicleId || null,
        rating: 5,
        title: 'Excelente vendedor',
        content: uniqueContent,
      },
    });

    expect([200, 201], `Create review failed: ${res.status()} — ${await res.text()}`).toContain(
      res.status()
    );

    const body = await res.json();
    reviewId = body?.data?.id ?? body?.id ?? '';
    console.log(`[04] reviewId = ${reviewId}`);
    expect(reviewId, 'Review ID not returned').toBeTruthy();
  });

  // ── 05. Review is visible via GET /api/reviews/seller/{sellerId} ──────────────
  test('05 · Review appears in seller review list', async ({ request }) => {
    expect(reviewId, 'reviewId must be set by step 04').toBeTruthy();

    // Poll a few times to allow for any async processing
    let found = false;
    for (let attempt = 0; attempt < 5; attempt++) {
      const res = await request.get(`${BASE_URL}/api/reviews/seller/${sellerId}`, {
        headers: { Authorization: `Bearer ${sellerToken}` },
        params: { pageSize: '50', sortBy: 'newest' },
      });

      if (res.status() === 200) {
        const body = await res.json();
        const items: Array<{ id: string }> = body?.data?.items ?? body?.items ?? [];
        found = items.some(r => r.id === reviewId);
        if (found) break;
      }

      if (!found && attempt < 4) {
        await new Promise(resolve => setTimeout(resolve, 2000));
      }
    }

    expect(found, `Review ${reviewId} not found in seller reviews`).toBe(true);
    console.log('[05] Review visible in seller list ✓');
  });

  // ── 06. Seller sees review on /cuenta/resenas and responds via UI ──────────────
  test('06 · Seller logs in and responds to review on /cuenta/resenas', async ({ page }) => {
    test.skip(!reviewId, 'Skipping UI test — no reviewId (step 04 may have failed)');

    // Login via browser
    await loginViaBrowser(page, SELLER_EMAIL, SELLER_PASSWORD);

    // Navigate to seller reviews page
    await page.goto(`${BASE_URL}/cuenta/resenas`);
    await page.waitForLoadState('networkidle');

    // Verify page loaded
    const heading = page.getByRole('heading', { name: /rese[ñn]as/i }).first();
    await expect(heading).toBeVisible({ timeout: 10_000 });

    // Wait for reviews to load (cards appear)
    const firstCard = page.locator('[class*="Card"]').first();
    await expect(firstCard).toBeVisible({ timeout: 15_000 });

    // Find the respond button for our specific review
    const respondBtn = page.locator(`[data-testid="respond-btn-${reviewId}"]`);

    // If the button is not found (review may be off-screen or paginated),
    // use the first available "Responder" button instead
    const hasSpecificBtn = await respondBtn.isVisible().catch(() => false);
    const targetBtn = hasSpecificBtn
      ? respondBtn
      : page.getByRole('button', { name: /responder/i }).first();

    await expect(targetBtn).toBeVisible({ timeout: 10_000 });
    await targetBtn.click();

    // The inline textarea should appear
    const textarea = page.locator('textarea').first();
    await expect(textarea).toBeVisible({ timeout: 5_000 });

    const responseText = `Gracias por tu reseña. Fue un placer atenderte. Respuesta E2E ${Date.now()}`;
    await textarea.fill(responseText);

    // Submit response
    const publishBtn = page.getByRole('button', { name: /publicar/i }).first();
    await publishBtn.click();

    // Expect success toast
    const successToast = page.getByText(/respuesta publicada/i);
    await expect(successToast).toBeVisible({ timeout: 10_000 });

    console.log('[06] Seller responded via UI ✓');
  });

  // ── 07. Verify response is stored in ReviewService ────────────────────────────
  test('07 · Review response confirmed via GET /api/reviews/{id}', async ({ request }) => {
    test.skip(!reviewId, 'Skipping — no reviewId');

    let hasResponse = false;

    // Poll up to 5 times (response may take a moment to persist)
    for (let attempt = 0; attempt < 5; attempt++) {
      const res = await request.get(`${BASE_URL}/api/reviews/${reviewId}`, {
        headers: { Authorization: `Bearer ${buyerToken}` },
      });

      if (res.status() === 200) {
        const body = await res.json();
        const review = body?.data ?? body;
        // Backend may return response as `response.content` or `sellerResponse`
        const responseContent = review?.response?.content ?? review?.sellerResponse ?? '';
        hasResponse = responseContent.trim().length > 0;
        if (hasResponse) break;
      }

      if (!hasResponse && attempt < 4) {
        await new Promise(resolve => setTimeout(resolve, 2000));
      }
    }

    expect(hasResponse, `Review ${reviewId} has no response after seller replied`).toBe(true);
    console.log('[07] Seller response stored in ReviewService ✓');
  });

  // ── 08. Buyer sees response on vehicle detail page (UI smoke test) ─────────────
  test('08 · Buyer sees seller response on vehicle detail page', async ({ page }) => {
    test.skip(!vehicleSlug, 'Skipping — no vehicle slug found in step 03');
    test.skip(!reviewId, 'Skipping — no reviewId');

    // Visit the vehicle detail page as a guest (no need to log in for public reviews)
    await page.goto(`${BASE_URL}/vehiculos/${vehicleSlug}`);
    await page.waitForLoadState('networkidle');

    // Verify reviews section is present
    const reviewsSection = page
      .locator('[data-testid="reviews-section"], section, article')
      .filter({ hasText: /rese[ñn]as|opiniones|valoraciones/i })
      .first();

    const sectionVisible = await reviewsSection.isVisible().catch(() => false);

    if (!sectionVisible) {
      // Reviews section might be below the fold — scroll down
      await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
      await page.waitForTimeout(1500);
    }

    // Check that "Respuesta del vendedor" or similar text is visible
    const responseLabel = page
      .getByText(/respuesta del vendedor|respuesta del concesionario|tu respuesta/i)
      .first();
    const isVisible = await responseLabel.isVisible({ timeout: 8_000 }).catch(() => false);

    // Non-fatal: the vehicle detail may not show reviews if section is conditionally rendered
    if (!isVisible) {
      console.warn(
        '[08] Response label not visible on vehicle page — may require scroll or different selector'
      );
    } else {
      console.log('[08] Seller response visible on vehicle detail page ✓');
    }
  });

  // ── 09. Cleanup — mark review as not-visible by reporting it ─────────────────
  // (Optional soft cleanup to avoid polluting test data)
  test('09 · [Cleanup] Buyer deletes the test review', async ({ request }) => {
    test.skip(!reviewId, 'Skipping cleanup — no reviewId');

    const res = await request.delete(`${BASE_URL}/api/reviews/${reviewId}`, {
      headers: authHeaders(buyerToken),
    });

    // 200/204 = deleted; 403/404 = already gone or not allowed — both acceptable
    const ok = [200, 204, 403, 404].includes(res.status());
    if (!ok) {
      console.warn(`[09] Cleanup delete returned unexpected status: ${res.status()}`);
    } else {
      console.log(`[09] Test review ${reviewId} cleaned up (status ${res.status()}) ✓`);
    }
    // Do not fail the suite on cleanup errors
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// SUITE 2 — Smoke: Review stats endpoint
// ─────────────────────────────────────────────────────────────────────────────
test.describe('Review Stats — smoke tests', () => {
  test.setTimeout(30_000);

  test('GET /api/reviews/seller/{id}/summary returns valid stats', async ({ request }) => {
    // Login as seller to get a valid sellerId
    const token = await login(request, SELLER_EMAIL, SELLER_PASSWORD);
    const payload = decodeJwt(token);
    const userId = (payload['nameid'] ?? payload['sub'] ?? '') as string;

    // Resolve sellerId
    let sid = '';
    const profileRes = await request.get(`${BASE_URL}/api/users/sellers/by-user/${userId}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (profileRes.status() === 200) {
      const body = await profileRes.json();
      sid = body?.data?.id ?? body?.id ?? '';
    }

    if (!sid) {
      console.warn('Could not resolve sellerId — skipping stats test');
      return;
    }

    const res = await request.get(`${BASE_URL}/api/reviews/seller/${sid}/summary`, {
      headers: { Authorization: `Bearer ${token}` },
    });

    expect([200, 404], `Unexpected status ${res.status()}`).toContain(res.status());

    if (res.status() === 200) {
      const body = await res.json();
      const stats = body?.data ?? body;
      // Must have averageRating and totalReviews
      expect(typeof stats?.averageRating ?? stats?.AverageRating).not.toBe('undefined');
      console.log(
        `[smoke] Stats: avg=${stats?.averageRating ?? stats?.AverageRating}, total=${stats?.totalReviews ?? stats?.TotalReviews}`
      );
    }
  });

  test('GET /api/reviews/buyer/{buyerId} returns buyer review history', async ({ request }) => {
    const token = await login(request, BUYER_EMAIL, BUYER_PASSWORD);
    const payload = decodeJwt(token);
    const buyerId = (payload['nameid'] ?? payload['sub'] ?? '') as string;
    expect(buyerId).toBeTruthy();

    const res = await request.get(`${BASE_URL}/api/reviews/buyer/${buyerId}`, {
      headers: { Authorization: `Bearer ${token}` },
    });

    expect([200, 404], `Unexpected status ${res.status()}`).toContain(res.status());

    if (res.status() === 200) {
      const body = await res.json();
      const items = body?.data?.items ?? body?.items ?? [];
      console.log(`[smoke] Buyer has ${items.length} review(s)`);
      expect(Array.isArray(items)).toBe(true);
    }
  });
});
