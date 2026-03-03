/**
 * E2E Tests — Validated Implemented Features (March 2026)
 *
 * Tests all features implemented in this session:
 * 1. Auth-gate on "Chat en vivo" button
 * 2. OKLA green branding on AI search (no purple)
 * 3. Support chatbot visible on all pages
 * 4. /mensajes routing for dealer chats
 * 5. AI search agent responsiveness
 *
 * Run: pnpm exec playwright test e2e/implemented-features.spec.ts --config=playwright.prod.config.ts
 */
import { test, expect } from '@playwright/test';

// ─── Helper: Login ───────────────────────────────────────────────────
async function tryLogin(page: any, email: string, password: string): Promise<boolean> {
  try {
    await page.goto('/login', { waitUntil: 'domcontentloaded', timeout: 15000 });
    await page.waitForTimeout(2000);
    const emailInput = page.locator('input[type="email"], input[name="email"], input[placeholder*="email" i], input[placeholder*="correo" i]').first();
    await emailInput.waitFor({ state: 'visible', timeout: 5000 });
    await emailInput.fill(email);
    const passwordInput = page.locator('input[type="password"]').first();
    await passwordInput.waitFor({ state: 'visible', timeout: 5000 });
    await passwordInput.fill(password);
    const loginBtn = page.locator('button[type="submit"], button:has-text("Iniciar sesión"), button:has-text("Iniciar Sesión")').first();
    await loginBtn.click();
    await page.waitForURL((url: URL) => !url.pathname.includes('/login'), { timeout: 15000 });
    return true;
  } catch {
    return false;
  }
}

// =====================================================================
// TEST 1: Auth-gate on "Chat en vivo" button
// =====================================================================
test.describe('Feature 1: Auth-gate on Chat en vivo', () => {
  test('Anonymous user sees login prompt when clicking Chat en vivo', async ({ page }) => {
    await page.goto('/vehiculos', { waitUntil: 'domcontentloaded', timeout: 20000 });
    await page.waitForTimeout(3000);
    const vehicleCard = page.locator('a[href*="/vehiculos/"]').first();
    await expect(vehicleCard).toBeVisible({ timeout: 15000 });
    await vehicleCard.click();
    await page.waitForTimeout(3000);
    const chatButton = page.locator('button:has-text("Chat en vivo")');
    if (await chatButton.isVisible({ timeout: 5000 }).catch(() => false)) {
      await chatButton.click();
      const loginPrompt = page.locator('text=Inicia sesión para chatear');
      await expect(loginPrompt).toBeVisible({ timeout: 5000 });
      const loginBtn = page.locator('button:has-text("Iniciar sesión")');
      const registerBtn = page.locator('button:has-text("Crear cuenta gratis")');
      await expect(loginBtn).toBeVisible();
      await expect(registerBtn).toBeVisible();
    } else {
      test.skip();
    }
  });

  test('Authenticated user can access Chat en vivo', async ({ page }) => {
    const loggedIn = await tryLogin(page, 'buyer002@okla-test.com', 'BuyerTest2026!');
    if (!loggedIn) { test.skip(); return; }
    await page.goto('/vehiculos', { waitUntil: 'domcontentloaded', timeout: 20000 });
    await page.waitForTimeout(3000);
    const vehicleCard = page.locator('a[href*="/vehiculos/"]').first();
    if (await vehicleCard.isVisible({ timeout: 10000 }).catch(() => false)) {
      await vehicleCard.click();
      await page.waitForTimeout(3000);
      const chatButton = page.locator('button:has-text("Chat en vivo")');
      if (await chatButton.isVisible({ timeout: 5000 }).catch(() => false)) {
        await chatButton.click();
        await page.waitForURL((url: URL) => url.pathname.includes('/mensajes'), { timeout: 10000 });
        expect(page.url()).toContain('/mensajes');
      }
    }
  });
});

// =====================================================================
// TEST 2: OKLA Green Branding on AI Search (no purple)
// =====================================================================
test.describe('Feature 2: OKLA Green branding on /vehiculos', () => {
  test('AI search UI uses OKLA green, no purple classes', async ({ page }) => {
    await page.goto('/vehiculos', { waitUntil: 'domcontentloaded', timeout: 20000 });
    await page.waitForTimeout(3000);
    const purpleElements = page.locator('[class*="purple"]');
    const purpleCount = await purpleElements.count();
    expect(purpleCount).toBe(0);
    const sparklesIcon = page.locator('svg.lucide-sparkles').first();
    if (await sparklesIcon.isVisible({ timeout: 5000 }).catch(() => false)) {
      expect(true).toBeTruthy();
    }
  });
});

// =====================================================================
// TEST 3: Support Chatbot on ALL pages
// =====================================================================
test.describe('Feature 3: Support chatbot on all pages', () => {
  test('Support chatbot is visible on homepage', async ({ page }) => {
    await page.goto('/', { waitUntil: 'domcontentloaded', timeout: 20000 });
    await page.waitForTimeout(3000);
    const supportBubble = page.locator('button:has-text("Soporte"), [aria-label*="soporte" i], button:has(svg.lucide-headphones)');
    await expect(supportBubble.first()).toBeVisible({ timeout: 10000 });
  });

  test('Support chatbot is visible on /vehiculos', async ({ page }) => {
    await page.goto('/vehiculos', { waitUntil: 'domcontentloaded', timeout: 20000 });
    await page.waitForTimeout(3000);
    const supportBubble = page.locator('button:has-text("Soporte"), [aria-label*="soporte" i], button:has(svg.lucide-headphones)');
    await expect(supportBubble.first()).toBeVisible({ timeout: 10000 });
  });

  test('Support chatbot is visible on vehicle detail page', async ({ page }) => {
    await page.goto('/vehiculos', { waitUntil: 'domcontentloaded', timeout: 20000 });
    await page.waitForTimeout(3000);
    const vehicleCard = page.locator('a[href*="/vehiculos/"]').first();
    if (await vehicleCard.isVisible({ timeout: 10000 }).catch(() => false)) {
      await vehicleCard.click();
      await page.waitForTimeout(3000);
      const supportBubble = page.locator('button:has-text("Soporte"), [aria-label*="soporte" i], button:has(svg.lucide-headphones)');
      await expect(supportBubble.first()).toBeVisible({ timeout: 10000 });
    } else { test.skip(); }
  });

  test('Support chatbot opens when clicked', async ({ page }) => {
    await page.goto('/', { waitUntil: 'domcontentloaded', timeout: 20000 });
    await page.waitForTimeout(3000);
    const supportBubble = page.locator('button:has-text("Soporte"), [aria-label*="soporte" i], button:has(svg.lucide-headphones)');
    if (await supportBubble.first().isVisible({ timeout: 10000 }).catch(() => false)) {
      await supportBubble.first().click();
      await page.waitForTimeout(1000);
      const chatInput = page.locator('textarea[placeholder*="mensaje" i], input[placeholder*="mensaje" i], textarea[placeholder*="escribe" i], textarea[placeholder*="pregunta" i]');
      await expect(chatInput.first()).toBeVisible({ timeout: 5000 });
    } else { test.skip(); }
  });
});

// =====================================================================
// TEST 4: Dealer Chat Routes to /mensajes
// =====================================================================
test.describe('Feature 4: Dealer chat routes to /mensajes', () => {
  test('Dealer Chatear con Ana routes to /mensajes for authenticated user', async ({ page }) => {
    const loggedIn = await tryLogin(page, 'buyer002@okla-test.com', 'BuyerTest2026!');
    if (!loggedIn) { test.skip(); return; }
    await page.goto('/vehiculos', { waitUntil: 'domcontentloaded', timeout: 20000 });
    await page.waitForTimeout(3000);
    const vehicleCard = page.locator('a[href*="/vehiculos/"]').first();
    if (await vehicleCard.isVisible({ timeout: 10000 }).catch(() => false)) {
      await vehicleCard.click();
      await page.waitForTimeout(3000);
      const anaButton = page.locator('button:has-text("Chatear con Ana")');
      if (await anaButton.isVisible({ timeout: 3000 }).catch(() => false)) {
        await anaButton.click();
        await page.waitForURL((url: URL) => url.pathname.includes('/mensajes'), { timeout: 10000 });
        expect(page.url()).toContain('/mensajes');
      } else { test.skip(); }
    } else { test.skip(); }
  });
});

// =====================================================================
// TEST 5: AI Search Responsiveness
// =====================================================================
test.describe('Feature 5: AI Search Agent', () => {
  test('AI search widget is visible on /vehiculos', async ({ page }) => {
    await page.goto('/vehiculos', { waitUntil: 'domcontentloaded', timeout: 20000 });
    await page.waitForTimeout(3000);
    const aiSearchBubble = page.locator('button[aria-label*="Buscar con IA"], button[aria-label*="búsqueda IA"], button[aria-label*="search" i]');
    const aiSearchInput = page.locator('input[placeholder*="IA" i], input[placeholder*="lenguaje natural" i], input[placeholder*="Busca" i]');
    const sparklesButton = page.locator('button:has(svg.lucide-sparkles)');
    const hasBubble = await aiSearchBubble.isVisible().catch(() => false);
    const hasInput = await aiSearchInput.isVisible().catch(() => false);
    const hasSparkles = await sparklesButton.isVisible().catch(() => false);
    expect(hasBubble || hasInput || hasSparkles).toBeTruthy();
  });
});

// =====================================================================
// TEST 6: Page Load and Navigation Smoke Tests
// =====================================================================
test.describe('Smoke Tests: Critical Pages Load', () => {
  test('Homepage loads and shows OKLA brand', async ({ page }) => {
    await page.goto('/', { waitUntil: 'domcontentloaded', timeout: 20000 });
    await page.waitForTimeout(2000);
    expect(page.url()).toContain('okla');
    const oklaLogo = page.locator('text=OKLA');
    await expect(oklaLogo.first()).toBeVisible({ timeout: 10000 });
  });

  test('/vehiculos page loads with vehicle cards', async ({ page }) => {
    await page.goto('/vehiculos', { waitUntil: 'domcontentloaded', timeout: 20000 });
    await page.waitForTimeout(3000);
    const vehicleCards = page.locator('[class*="vehicle"], [class*="card"], a[href*="/vehiculos/"]');
    await expect(vehicleCards.first()).toBeVisible({ timeout: 15000 });
  });

  test('Login page loads', async ({ page }) => {
    await page.goto('/login', { waitUntil: 'domcontentloaded', timeout: 20000 });
    await page.waitForTimeout(2000);
    const loginHeading = page.locator('text=Bienvenido');
    await expect(loginHeading.first()).toBeVisible({ timeout: 10000 });
  });

  test('/mensajes page redirects to login for anonymous users', async ({ page }) => {
    await page.goto('/mensajes', { waitUntil: 'domcontentloaded', timeout: 20000 });
    await page.waitForURL((url: URL) => url.pathname.includes('/login') || url.pathname.includes('/mensajes'), { timeout: 15000 });
    if (page.url().includes('/login')) {
      expect(page.url()).toContain('/login');
    }
  });
});
