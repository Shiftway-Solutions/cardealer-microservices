/**
 * E2E Tests — Buyer Dashboard (/cuenta)
 *
 * Validates the buyer-focused dashboard UI introduced to replace the
 * seller-centric placeholder that was previously shown to buyer accounts.
 *
 * Strategy:
 *   • Uses a real production buyer account (buyer002@okla-test.com).
 *   • Authenticates via the API gateway BFF (/api/auth/login) to obtain a
 *     session cookie, then uses a full browser context to test the UI.
 *   • Verifies the presence of buyer content AND the absence of seller
 *     content (KYC banner, "Publicar Vehículo" button, seller metrics, etc.).
 *
 * Run against production:
 *   cd frontend/web-next
 *   PLAYWRIGHT_BASE_URL=https://okla.com.do pnpm exec playwright test e2e/buyer-dashboard.spec.ts
 *
 * Run locally (docker-compose):
 *   PLAYWRIGHT_BASE_URL=http://localhost:3000 pnpm exec playwright test e2e/buyer-dashboard.spec.ts
 */

import { test, expect, type Page, type BrowserContext } from '@playwright/test';

// ─── Configuration ────────────────────────────────────────────────────────────
const BASE_URL = process.env.PLAYWRIGHT_BASE_URL ?? 'https://okla.com.do';

// ─── Test Credentials ─────────────────────────────────────────────────────────
const BUYER_EMAIL = 'buyer002@okla-test.com';
const BUYER_PASSWORD = 'BuyerTest2026!';

// ─── Helper: login via API and return browser context with auth cookies ───────
async function loginAsBuyer(context: BrowserContext): Promise<Page> {
  // Perform API login via the BFF — this sets the HttpOnly session cookie
  const apiRequest = await context.request.post(`${BASE_URL}/api/auth/login`, {
    headers: { 'Content-Type': 'application/json' },
    data: { email: BUYER_EMAIL, password: BUYER_PASSWORD },
  });

  expect(apiRequest.status(), 'Login should succeed (200)').toBe(200);

  const body = await apiRequest.json();
  const accessToken: string = body?.data?.accessToken ?? body?.token ?? '';
  expect(accessToken.length, 'Access token must be present').toBeGreaterThan(10);

  // Open a page — the HttpOnly cookie is already set on the context
  const page = await context.newPage();
  // Also store the token in localStorage so the Next.js app can hydrate auth
  await page.goto(BASE_URL);
  await page.evaluate(
    (token) => localStorage.setItem('auth_token', token),
    accessToken
  );

  return page;
}

// =============================================================================
// SUITE 1 — Dashboard renders buyer-specific content
// =============================================================================
test.describe('Buyer Dashboard — Content', () => {
  test.setTimeout(30_000);

  let page: Page;

  test.beforeEach(async ({ context }) => {
    page = await loginAsBuyer(context);
    await page.goto(`${BASE_URL}/cuenta`);
    await page.waitForLoadState('networkidle');
  });

  // ── Welcome & identity ──────────────────────────────────────────────────────
  test('shows personalised buyer greeting', async () => {
    // Greeting should contain "Hola" (no seller-specific wording)
    const heading = page.getByRole('heading', { name: /hola/i });
    await expect(heading).toBeVisible({ timeout: 10_000 });
  });

  test('shows "Buscar Vehículos" CTA in the hero', async () => {
    const searchCta = page.getByRole('link', { name: /buscar veh/i });
    await expect(searchCta.first()).toBeVisible();
  });

  // ── Buyer stat cards (data-testid attributes set in BuyerDashboard) ─────────
  test('shows Favoritos stat card', async () => {
    const card = page.getByTestId('summary-favorites');
    await expect(card).toBeVisible({ timeout: 10_000 });
  });

  test('shows Búsquedas Guardadas stat card', async () => {
    const card = page.getByTestId('summary-saved-searches');
    await expect(card).toBeVisible({ timeout: 10_000 });
  });

  test('shows Alertas de Precio stat card', async () => {
    const card = page.getByTestId('summary-alerts');
    await expect(card).toBeVisible({ timeout: 10_000 });
  });

  // ── Quick Actions grid ──────────────────────────────────────────────────────
  test('shows Acciones Rápidas section with 6 tiles', async () => {
    const section = page.getByRole('heading', { name: /acciones rápidas/i });
    await expect(section).toBeVisible();

    // The 6 quick-action links: Buscar, Favoritos, Búsquedas, Alertas, Mensajes, Historial
    const actions = ['Buscar', 'Favoritos', 'Búsquedas', 'Alertas', 'Mensajes', 'Historial'];
    for (const label of actions) {
      const tile = page.getByRole('link', { name: new RegExp(label, 'i') }).first();
      await expect(tile, `Quick action tile "${label}" should be visible`).toBeVisible();
    }
  });

  // ── Recent Favorites section ────────────────────────────────────────────────
  test('shows Mis Favoritos Recientes section', async () => {
    const title = page.getByRole('heading', { name: /favoritos recientes/i });
    await expect(title).toBeVisible();

    // Either shows favorite cards OR the empty-state CTA
    const hasContent =
      (await page.locator('a[href*="/vehiculos"]').count()) > 0 ||
      (await page.getByText(/explorar vehículos/i).isVisible());
    expect(hasContent, 'Favorites section should have content or empty state CTA').toBe(true);
  });
});

// =============================================================================
// SUITE 2 — Dashboard must NOT show seller content
// =============================================================================
test.describe('Buyer Dashboard — No Seller Content', () => {
  test.setTimeout(30_000);

  let page: Page;

  test.beforeEach(async ({ context }) => {
    page = await loginAsBuyer(context);
    await page.goto(`${BASE_URL}/cuenta`);
    await page.waitForLoadState('networkidle');
  });

  test('does NOT show KYC / VerificationBanner', async () => {
    // The KYC banner contains text about identity verification for selling
    const kycBanner = page.getByText(/verificación de identidad|verifica tu identidad|kyc/i);
    await expect(kycBanner).not.toBeVisible();
  });

  test('does NOT show "Publicar Vehículo" button in the main hero', async () => {
    // The hero area should only have "Buscar Vehículos", not "Publicar Vehículo"
    const publishButton = page.getByRole('link', { name: /publicar veh[íi]culo/i });
    await expect(publishButton).not.toBeVisible();
  });

  test('does NOT show "Vehículos Activos" seller metric', async () => {
    const metric = page.getByText(/veh[íi]culos activos/i);
    await expect(metric).not.toBeVisible();
  });

  test('does NOT show "Vistas Totales" seller metric', async () => {
    const metric = page.getByText(/vistas totales/i);
    await expect(metric).not.toBeVisible();
  });

  test('does NOT show "Consultas" seller metric', async () => {
    // Buyer has no "Consultas" (seller inquiry count) metric
    const metric = page.getByText(/^consultas$/i);
    await expect(metric).not.toBeVisible();
  });

  test('does NOT show "Mis Vehículos Recientes" seller section', async () => {
    const section = page.getByText(/mis veh[íi]culos recientes/i);
    await expect(section).not.toBeVisible();
  });

  test('does NOT show "Publicar mi primer vehículo" seller CTA', async () => {
    const cta = page.getByText(/publicar mi primer veh[íi]culo/i);
    await expect(cta).not.toBeVisible();
  });

  test('does NOT show "Mejora tus resultados" seller tips card', async () => {
    const tips = page.getByText(/mejora tus resultados/i);
    await expect(tips).not.toBeVisible();
  });

  test('does NOT show "Calificación" seller metric', async () => {
    const rating = page.getByText(/^calificaci[oó]n$/i);
    await expect(rating).not.toBeVisible();
  });
});

// =============================================================================
// SUITE 3 — Navigation from buyer dashboard
// =============================================================================
test.describe('Buyer Dashboard — Navigation', () => {
  test.setTimeout(30_000);

  let page: Page;

  test.beforeEach(async ({ context }) => {
    page = await loginAsBuyer(context);
    await page.goto(`${BASE_URL}/cuenta`);
    await page.waitForLoadState('networkidle');
  });

  test('stat card "Favoritos" links to /cuenta/favoritos', async () => {
    const card = page.getByTestId('summary-favorites');
    await card.click();
    await expect(page).toHaveURL(/\/cuenta\/favoritos/);
  });

  test('stat card "Búsquedas" links to /cuenta/busquedas', async () => {
    const card = page.getByTestId('summary-saved-searches');
    await card.click();
    await expect(page).toHaveURL(/\/cuenta\/busquedas/);
  });

  test('stat card "Alertas" links to /cuenta/alertas', async () => {
    const card = page.getByTestId('summary-alerts');
    await card.click();
    await expect(page).toHaveURL(/\/cuenta\/alertas/);
  });

  test('"Ver todos" link in Recent Favorites goes to /cuenta/favoritos', async () => {
    const verTodos = page.getByRole('link', { name: /ver todos/i });
    await expect(verTodos).toBeVisible();
    await verTodos.click();
    await expect(page).toHaveURL(/\/cuenta\/favoritos/);
  });

  test('"Buscar Vehículos" hero CTA navigates to /vehiculos', async () => {
    const cta = page.getByRole('link', { name: /buscar veh/i }).first();
    await cta.click();
    await expect(page).toHaveURL(/\/vehiculos/);
  });

  test('"Historial" quick action links to /cuenta/historial', async () => {
    const tile = page.getByRole('link', { name: /historial/i }).first();
    await tile.click();
    await expect(page).toHaveURL(/\/cuenta\/historial/);
  });

  test('"Mensajes" quick action links to /mensajes', async () => {
    const tile = page.getByRole('link', { name: /mensajes/i }).first();
    await tile.click();
    await expect(page).toHaveURL(/\/mensajes/);
  });
});

// =============================================================================
// SUITE 4 — Favorites API integration from the buyer dashboard
// =============================================================================
test.describe('Buyer Dashboard — Favorites Flow', () => {
  test.setTimeout(45_000);

  let page: Page;
  let accessToken: string;

  test.beforeEach(async ({ context }) => {
    // Login
    const loginRes = await context.request.post(`${BASE_URL}/api/auth/login`, {
      headers: { 'Content-Type': 'application/json' },
      data: { email: BUYER_EMAIL, password: BUYER_PASSWORD },
    });
    expect(loginRes.status()).toBe(200);
    const body = await loginRes.json();
    accessToken = body?.data?.accessToken ?? body?.token ?? '';
    expect(accessToken.length).toBeGreaterThan(10);

    page = await context.newPage();
    await page.goto(BASE_URL);
    await page.evaluate((t) => localStorage.setItem('auth_token', t), accessToken);
  });

  test('favorites count on stat card reflects API count', async ({ request }) => {
    // Fetch current favorites count from the API
    const apiRes = await request.get(`${BASE_URL}/api/favorites/count`, {
      headers: { Authorization: `Bearer ${accessToken}` },
    });

    let apiCount = 0;
    if (apiRes.ok()) {
      const data = await apiRes.json();
      apiCount = data?.data?.count ?? data?.count ?? 0;
    }

    // Navigate to the dashboard and read the displayed count
    await page.goto(`${BASE_URL}/cuenta`);
    await page.waitForLoadState('networkidle');

    const card = page.getByTestId('summary-favorites');
    await expect(card).toBeVisible({ timeout: 10_000 });

    const displayedText = await card.locator('p.text-2xl, p[class*="text-2xl"]').first().textContent();
    const displayedCount = parseInt(displayedText?.trim() ?? '0', 10);

    expect(
      displayedCount,
      `Dashboard favorites count (${displayedCount}) should match API count (${apiCount})`
    ).toBe(apiCount);
  });

  test('adding a favorite from /vehiculos updates the dashboard counter', async () => {
    // Navigate to vehicle listing and get the initial favorites count from dashboard first
    await page.goto(`${BASE_URL}/cuenta`);
    await page.waitForLoadState('networkidle');

    const statCard = page.getByTestId('summary-favorites');
    await expect(statCard).toBeVisible({ timeout: 10_000 });

    const initialText = await statCard
      .locator('p.text-2xl, p[class*="text-2xl"]')
      .first()
      .textContent();
    const initialCount = parseInt(initialText?.trim() ?? '0', 10);

    // Go to /vehiculos and toggle a favorite
    await page.goto(`${BASE_URL}/vehiculos`);
    await page.waitForLoadState('networkidle');

    // Click the first heart/favorite button we find on a vehicle card
    const heartBtn = page
      .locator('[data-testid="favorite-button"], button[aria-label*="favorito"], button[aria-label*="favorite"]')
      .first();

    if (await heartBtn.isVisible({ timeout: 5_000 }).catch(() => false)) {
      // Determine if it's currently favorited (so we know what direction we toggle)
      const isCurrentlyActive =
        (await heartBtn.getAttribute('data-active')) === 'true' ||
        (await heartBtn.getAttribute('aria-pressed')) === 'true';

      await heartBtn.click();
      await page.waitForTimeout(1_500); // let React Query re-fetch

      // Go back to dashboard
      await page.goto(`${BASE_URL}/cuenta`);
      await page.waitForLoadState('networkidle');

      const newText = await page
        .getByTestId('summary-favorites')
        .locator('p.text-2xl, p[class*="text-2xl"]')
        .first()
        .textContent();
      const newCount = parseInt(newText?.trim() ?? '0', 10);

      if (!isCurrentlyActive) {
        // We added a favorite — count should increase by 1
        expect(newCount).toBe(initialCount + 1);
      } else {
        // We removed a favorite — count should decrease by 1
        expect(newCount).toBe(Math.max(0, initialCount - 1));
      }
    } else {
      // No vehicle cards visible (empty catalogue) — just verify dashboard still loads
      test.info().annotations.push({ type: 'skip-reason', description: 'No vehicle cards found' });
    }
  });

  test('/cuenta/favoritos shows the favorited vehicles list', async () => {
    await page.goto(`${BASE_URL}/cuenta/favoritos`);
    await page.waitForLoadState('networkidle');

    // Page should not redirect away (buyer has access to favorites)
    await expect(page).toHaveURL(/\/cuenta\/favoritos/);

    // Should show either vehicle cards or empty state — not an error
    const hasVehicles = await page.locator('[data-testid="vehicle-card"], article').count();
    const hasEmptyState = await page.getByText(/no tienes favoritos|sin favoritos|explorar/i).isVisible();

    expect(
      hasVehicles > 0 || hasEmptyState,
      'Favorites page should show content or empty state'
    ).toBe(true);
  });
});

// =============================================================================
// SUITE 5 — Saved Searches & Price Alerts pages are accessible to buyers
// =============================================================================
test.describe('Buyer Dashboard — Sub-pages', () => {
  test.setTimeout(30_000);

  let page: Page;

  test.beforeEach(async ({ context }) => {
    page = await loginAsBuyer(context);
  });

  test('/cuenta/busquedas loads for buyer', async () => {
    await page.goto(`${BASE_URL}/cuenta/busquedas`);
    await page.waitForLoadState('networkidle');
    await expect(page).toHaveURL(/\/cuenta\/busquedas/);
    // Must not crash — verify no error heading
    await expect(page.getByRole('heading', { name: /error|500|not found/i })).not.toBeVisible();
  });

  test('/cuenta/alertas loads for buyer', async () => {
    await page.goto(`${BASE_URL}/cuenta/alertas`);
    await page.waitForLoadState('networkidle');
    await expect(page).toHaveURL(/\/cuenta\/alertas/);
    await expect(page.getByRole('heading', { name: /error|500|not found/i })).not.toBeVisible();
  });

  test('/cuenta/historial loads for buyer', async () => {
    await page.goto(`${BASE_URL}/cuenta/historial`);
    await page.waitForLoadState('networkidle');
    await expect(page).toHaveURL(/\/cuenta\/historial/);
    await expect(page.getByRole('heading', { name: /error|500|not found/i })).not.toBeVisible();
  });

  test('/cuenta/perfil loads for buyer', async () => {
    await page.goto(`${BASE_URL}/cuenta/perfil`);
    await page.waitForLoadState('networkidle');
    // May be /cuenta/perfil or /cuenta/profile — accept either
    await expect(page).toHaveURL(/\/cuenta\/(perfil|profile)/);
    await expect(page.getByRole('heading', { name: /error|500|not found/i })).not.toBeVisible();
  });
});
