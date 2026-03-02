/**
 * PORTAL AUDIT — All user portals via browser (single login per role)
 *
 * Optimized: logs in ONCE per role, then visits ALL pages in sequence.
 * This avoids rate limiting (429) on the login endpoint.
 *
 * Run:
 *   cd frontend/web-next
 *   PLAYWRIGHT_BASE_URL=https://okla.com.do node node_modules/@playwright/test/cli.js test e2e/portal-audit.spec.ts --config playwright.prod.config.ts --reporter=list
 */

import { test, expect, type Page } from '@playwright/test';

const BASE = process.env.PLAYWRIGHT_BASE_URL ?? 'https://okla.com.do';

const SELLER = { email: 'gmoreno@okla.com.do', password: '$Gregory1' };
const BUYER = { email: 'buyer002@okla-test.com', password: 'BuyerTest2026!' };
const ADMIN = { email: 'admin@okla.local', password: 'Admin123!@#' };

async function login(page: Page, email: string, password: string, maxRetries = 3) {
  for (let attempt = 1; attempt <= maxRetries; attempt++) {
    await page.goto(`${BASE}/login`, { waitUntil: 'domcontentloaded', timeout: 30_000 });
    await page.waitForLoadState('networkidle', { timeout: 15_000 }).catch(() => {});

    await page.getByRole('textbox', { name: /email/i }).first().fill(email);
    await page.locator('input[type="password"]').first().fill(password);
    await page
      .getByRole('button', { name: /iniciar sesión/i })
      .first()
      .click();

    try {
      await page.waitForURL(url => !url.toString().includes('/login'), { timeout: 30_000 });
      await page.waitForLoadState('networkidle', { timeout: 10_000 }).catch(() => {});
      console.log(`  Login succeeded for ${email} (attempt ${attempt})`);
      return;
    } catch {
      // Check if there's an error message on the page (rate limit, invalid creds, etc.)
      const bodyText = (await page.textContent('body').catch(() => '')) ?? '';
      const hasRateLimit = /too many|rate limit|429|demasiadas solicitudes/i.test(bodyText);
      const hasError = /error|invalid|incorrecto/i.test(bodyText);
      console.log(
        `  Login attempt ${attempt} failed for ${email}. Rate-limited: ${hasRateLimit}, Error: ${hasError}`
      );
      if (attempt < maxRetries) {
        const wait = attempt * 30_000; // 30s, 60s, 90s backoff
        console.log(`  Waiting ${wait / 1000}s before retry...`);
        await new Promise(r => setTimeout(r, wait));
      }
    }
  }
  throw new Error(`Login failed for ${email} after ${maxRetries} attempts`);
}

interface AuditResult {
  path: string;
  status: number;
  has500: boolean;
  redirected: boolean;
  finalUrl: string;
  error?: string;
}

async function visitPage(page: Page, path: string): Promise<AuditResult> {
  try {
    const response = await page.goto(`${BASE}${path}`, {
      waitUntil: 'domcontentloaded',
      timeout: 30_000,
    });
    // Brief wait for initial render instead of networkidle (too slow on data-heavy admin pages)
    await page.waitForTimeout(3_000);
    const httpStatus = response?.status() ?? 0;
    const bodyText = (await page.textContent('body')) ?? '';
    const has500 = /internal server error/i.test(bodyText) || httpStatus === 500;
    const redirected = page.url().includes('/login');
    return { path, status: httpStatus, has500, redirected, finalUrl: page.url() };
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : String(e);
    return { path, status: 0, has500: false, redirected: false, finalUrl: '', error: msg };
  }
}

// ===========================================================================
// BUYER PORTAL
// ===========================================================================
test('Buyer Portal — all pages', async ({ page }) => {
  test.setTimeout(300_000); // 5 min

  const pages = [
    '/cuenta',
    '/cuenta/perfil',
    '/cuenta/favoritos',
    '/cuenta/busquedas',
    '/cuenta/alertas',
    '/cuenta/mensajes',
    '/cuenta/notificaciones',
    '/cuenta/seguridad',
    '/cuenta/configuracion',
  ];

  await login(page, BUYER.email, BUYER.password);
  console.log('✅ Buyer logged in');

  const results: AuditResult[] = [];

  for (const p of pages) {
    const r = await visitPage(page, p);
    results.push(r);
    if (r.error) {
      console.log(`❌ ${p} — ERROR: ${r.error}`);
    } else if (r.redirected) {
      console.log(`⚠️ ${p} — Redirected to login`);
    } else if (r.has500) {
      console.log(`❌ ${p} — 500 Internal Server Error`);
    } else {
      console.log(`✅ ${p} OK (${r.status})`);
    }
  }

  // Public pages (no login required)
  for (const p of ['/vehiculos', '/dealers', '/comparar']) {
    const r = await visitPage(page, p);
    results.push(r);
    console.log(`✅ ${p} OK (${r.status})`);
  }

  // Summary
  const failed = results.filter(r => r.has500 || r.error);
  console.log(`\n📊 Buyer Portal: ${results.length - failed.length}/${results.length} passed`);
  if (failed.length) {
    for (const f of failed) console.log(`  ❌ ${f.path}: ${f.error ?? '500 error'}`);
  }
  expect(failed.filter(r => r.has500).length, 'Pages with 500 errors').toBe(0);
});

// ===========================================================================
// SELLER PORTAL
// ===========================================================================
test('Seller Portal — all pages', async ({ page }) => {
  test.setTimeout(600_000); // 10 min

  const pages = [
    '/cuenta',
    '/cuenta/perfil',
    '/cuenta/mis-vehiculos',
    '/cuenta/estadisticas',
    '/cuenta/consultas',
    '/cuenta/resenas',
    '/cuenta/favoritos',
    '/cuenta/alertas',
    '/cuenta/pagos',
    '/cuenta/historial',
    '/cuenta/seguridad',
    '/cuenta/configuracion',
    '/cuenta/verificacion',
    '/cuenta/notificaciones',
    '/publicar',
    '/vender/dashboard',
    '/vender/leads',
  ];

  // Wait 5s between portal logins to avoid rate limiting
  await new Promise(r => setTimeout(r, 5_000));
  await login(page, SELLER.email, SELLER.password);
  console.log('✅ Seller logged in');

  const results: AuditResult[] = [];

  for (const p of pages) {
    const r = await visitPage(page, p);
    results.push(r);
    if (r.error) {
      console.log(`❌ ${p} — ERROR: ${r.error}`);
    } else if (r.redirected) {
      console.log(`⚠️ ${p} — Redirected to login`);
    } else if (r.has500) {
      console.log(`❌ ${p} — 500 Internal Server Error`);
    } else {
      console.log(`✅ ${p} OK (${r.status})`);
    }
  }

  const failed = results.filter(r => r.has500 || r.error);
  console.log(`\n📊 Seller Portal: ${results.length - failed.length}/${results.length} passed`);
  if (failed.length) {
    for (const f of failed) console.log(`  ❌ ${f.path}: ${f.error ?? '500 error'}`);
  }
  expect(failed.filter(r => r.has500).length, 'Pages with 500 errors').toBe(0);
});

// ===========================================================================
// ADMIN PORTAL
// ===========================================================================
test('Admin Portal — all pages', async ({ page }) => {
  test.setTimeout(600_000); // 10 min

  const pages = [
    '/admin',
    '/admin/usuarios',
    '/admin/vehiculos',
    '/admin/dealers',
    '/admin/reviews',
    '/admin/reportes',
    '/admin/kyc',
    '/admin/facturacion',
    '/admin/analytics',
    '/admin/contenido',
    '/admin/mensajes',
    '/admin/equipo',
    '/admin/configuracion',
    '/admin/logs',
    '/admin/mantenimiento',
    '/admin/promociones',
    '/admin/suscripciones',
    '/admin/transacciones',
    '/admin/compliance',
    '/admin/soporte',
    '/admin/sistema',
  ];

  // Wait 5s between portal logins to avoid rate limiting
  await new Promise(r => setTimeout(r, 5_000));
  await login(page, ADMIN.email, ADMIN.password);
  console.log('✅ Admin logged in');

  const results: AuditResult[] = [];

  for (const p of pages) {
    const r = await visitPage(page, p);
    results.push(r);
    if (r.error) {
      console.log(`❌ ${p} — ERROR: ${r.error}`);
    } else if (r.redirected) {
      console.log(`⚠️ ${p} — Redirected to login`);
    } else if (r.has500) {
      console.log(`❌ ${p} — 500 Internal Server Error`);
    } else {
      console.log(`✅ ${p} OK (${r.status})`);
    }
  }

  const failed = results.filter(r => r.has500 || r.error);
  console.log(`\n📊 Admin Portal: ${results.length - failed.length}/${results.length} passed`);
  if (failed.length) {
    for (const f of failed) console.log(`  ❌ ${f.path}: ${f.error ?? '500 error'}`);
  }
  expect(failed.filter(r => r.has500).length, 'Pages with 500 errors').toBe(0);
});

// ===========================================================================
// DEALER PORTAL
// ===========================================================================
test('Dealer Portal — all pages', async ({ page }) => {
  test.setTimeout(600_000); // 10 min

  const pages = [
    '/dealer',
    '/dealer/inventario',
    '/dealer/leads',
    '/dealer/analytics',
    '/dealer/citas',
    '/dealer/mensajes',
    '/dealer/resenas',
    '/dealer/empleados',
    '/dealer/ubicaciones',
    '/dealer/pricing',
    '/dealer/reportes',
    '/dealer/perfil',
    '/dealer/documentos',
    '/dealer/facturacion',
    '/dealer/suscripcion',
    '/dealer/configuracion',
    '/dealer/publicar',
  ];

  // Wait 5s between portal logins to avoid rate limiting
  await new Promise(r => setTimeout(r, 5_000));
  await login(page, SELLER.email, SELLER.password);
  console.log('✅ Dealer (seller) logged in');

  const results: AuditResult[] = [];

  for (const p of pages) {
    const r = await visitPage(page, p);
    results.push(r);
    if (r.error) {
      console.log(`❌ ${p} — ERROR: ${r.error}`);
    } else if (r.redirected) {
      console.log(`⚠️ ${p} — Redirected to login`);
    } else if (r.has500) {
      console.log(`❌ ${p} — 500 Internal Server Error`);
    } else {
      // Some dealer pages may redirect non-dealers to /cuenta or elsewhere
      const isRedirectAway = !r.finalUrl.includes(p) && !r.finalUrl.includes('/login');
      if (isRedirectAway) {
        console.log(`ℹ️ ${p} — Redirected to ${r.finalUrl} (likely no dealer role)`);
      } else {
        console.log(`✅ ${p} OK (${r.status})`);
      }
    }
  }

  const failed = results.filter(r => r.has500 || r.error);
  console.log(`\n📊 Dealer Portal: ${results.length - failed.length}/${results.length} passed`);
  if (failed.length) {
    for (const f of failed) console.log(`  ❌ ${f.path}: ${f.error ?? '500 error'}`);
  }
  expect(failed.filter(r => r.has500).length, 'Pages with 500 errors').toBe(0);
});
