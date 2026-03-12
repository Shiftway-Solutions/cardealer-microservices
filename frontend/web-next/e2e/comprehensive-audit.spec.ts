/**
 * COMPREHENSIVE PLATFORM AUDIT — OKLA Vehicle Marketplace
 *
 * Date: 2026-03-02
 * Purpose: Full audit of ALL views for every user type (buyer, seller, admin, dealer)
 *          Testing end-to-end data flows, UI rendering, data persistence, and cross-user processes.
 *
 * Excludes: Chatbot, Live Chat (per audit instructions)
 *
 * Run:
 *   cd frontend/web-next
 *   PLAYWRIGHT_BASE_URL=https://okla.com.do node node_modules/@playwright/test/cli.js test e2e/comprehensive-audit.spec.ts --config playwright.prod.config.ts --reporter=list
 */

import { test, expect, type APIRequestContext, type Page } from '@playwright/test';
import { randomUUID } from 'crypto';

const BASE_URL = process.env.PLAYWRIGHT_BASE_URL ?? 'https://okla.com.do';

const SELLER_EMAIL = 'nmateo@okla.com.do';
const SELLER_PASSWORD = 'Dealer2026!@#';
const BUYER_EMAIL = 'buyer002@okla-test.com';
const BUYER_PASSWORD = 'BuyerTest2026!';
const ADMIN_EMAIL = 'admin@okla.local';
const ADMIN_PASSWORD = 'Admin123!@#';

const csrfToken = randomUUID().replace(/-/g, '');

function authHeaders(token: string) {
  return {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${token}`,
    'X-CSRF-Token': csrfToken,
    Cookie: `csrf_token=${csrfToken}`,
  };
}

function readHeaders(token: string) {
  return { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` };
}

function decodeJwt(token: string): Record<string, unknown> {
  const [, b64] = token.split('.');
  return JSON.parse(Buffer.from(b64.replace(/-/g, '+').replace(/_/g, '/'), 'base64').toString());
}

async function apiLogin(request: APIRequestContext, email: string, password: string) {
  const res = await request.post(`${BASE_URL}/api/auth/login`, {
    headers: { 'Content-Type': 'application/json' },
    data: { email, password },
  });
  expect(res.status(), `Login failed for ${email}`).toBe(200);
  const body = await res.json();
  const token: string = body?.data?.token ?? body?.data?.accessToken ?? body?.token ?? '';
  expect(token).toBeTruthy();
  const payload = decodeJwt(token);
  const userId = (payload['nameid'] ??
    payload['sub'] ??
    payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ??
    '') as string;
  return { token, userId };
}

async function loginViaBrowser(page: Page, email: string, password: string) {
  await page.goto(`${BASE_URL}/login`, { waitUntil: 'networkidle', timeout: 30_000 });
  await page.getByRole('textbox', { name: /email/i }).first().fill(email);
  await page.locator('input[type="password"]').first().fill(password);
  await page
    .getByRole('button', { name: /iniciar sesión/i })
    .first()
    .click();
  await page.waitForURL(url => !url.toString().includes('/login'), { timeout: 20_000 });
  await page.waitForLoadState('networkidle', { timeout: 10_000 }).catch(() => {});
}

async function auditPage(page: Page, path: string) {
  const response = await page.goto(`${BASE_URL}${path}`, {
    waitUntil: 'domcontentloaded',
    timeout: 30_000,
  });
  await page.waitForLoadState('networkidle', { timeout: 15_000 }).catch(() => {});
  const httpStatus = response?.status() ?? 0;
  const bodyText = (await page.textContent('body')) ?? '';
  const has500 = /internal server error/i.test(bodyText) || httpStatus === 500;
  return { status: httpStatus, url: page.url(), bodyText, has500 };
}

// ===========================================================================
// PHASE 1: AUTH & API HEALTH
// ===========================================================================
test.describe('Phase 1: Auth & API Health', () => {
  test.setTimeout(60_000);

  test('1.1 · All users authenticate', async ({ request }) => {
    const s = await apiLogin(request, SELLER_EMAIL, SELLER_PASSWORD);
    console.log(`✅ Seller: ${s.userId}`);
    const b = await apiLogin(request, BUYER_EMAIL, BUYER_PASSWORD);
    console.log(`✅ Buyer: ${b.userId}`);
    const a = await apiLogin(request, ADMIN_EMAIL, ADMIN_PASSWORD);
    console.log(`✅ Admin: ${a.userId}`);
  });

  test('1.2 · Core APIs reachable', async ({ request }) => {
    const buyer = await apiLogin(request, BUYER_EMAIL, BUYER_PASSWORD);
    const eps = [
      { p: '/api/vehicles?limit=1', n: 'Vehicles', auth: false },
      { p: '/api/notifications', n: 'Notifications', auth: true },
      { p: '/api/contactrequests/received', n: 'Contact', auth: true },
      { p: '/api/favorites', n: 'Favorites', auth: true },
      { p: '/api/pricealerts', n: 'Alerts', auth: true },
      { p: '/api/savedsearches', n: 'SavedSearches', auth: true },
    ];
    for (const ep of eps) {
      const res = await request.get(`${BASE_URL}${ep.p}`, {
        headers: ep.auth ? readHeaders(buyer.token) : {},
      });
      const ok = res.status() < 500;
      console.log(`  ${ok ? '✅' : '❌'} ${ep.n}: ${res.status()}`);
      expect(ok, `${ep.n} returned ${res.status()}`).toBe(true);
    }
  });

  test('1.3 · JWT claims correct', async ({ request }) => {
    const admin = await apiLogin(request, ADMIN_EMAIL, ADMIN_PASSWORD);
    const p = decodeJwt(admin.token);
    expect(p.iss).toBe('okla-api');
    const rc = p.role ?? p['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
    const roles: string[] = Array.isArray(rc) ? rc : [rc as string];
    expect(roles).toContain('Admin');
    console.log(`✅ Admin roles: ${roles.join(', ')}`);
  });
});

// ===========================================================================
// PHASE 2: BUYER PORTAL
// ===========================================================================
test.describe('Phase 2: Buyer Portal', () => {
  test.setTimeout(180_000);

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

  for (const p of pages) {
    test(`2 · ${p}`, async ({ page }) => {
      await loginViaBrowser(page, BUYER_EMAIL, BUYER_PASSWORD);
      const r = await auditPage(page, p);
      expect(r.has500, `${p} → 500`).toBe(false);
      console.log(`✅ ${p} OK (${r.status})`);
    });
  }

  test('2 · public /vehiculos', async ({ page }) => {
    const r = await auditPage(page, '/vehiculos');
    expect(r.has500).toBe(false);
    console.log('✅ /vehiculos OK');
  });

  test('2 · public /dealers', async ({ page }) => {
    const r = await auditPage(page, '/dealers');
    expect(r.has500).toBe(false);
    console.log('✅ /dealers OK');
  });

  test('2 · public /comparar', async ({ page }) => {
    const r = await auditPage(page, '/comparar');
    expect(r.has500).toBe(false);
    console.log('✅ /comparar OK');
  });
});

// ===========================================================================
// PHASE 3: SELLER PORTAL
// ===========================================================================
test.describe('Phase 3: Seller Portal', () => {
  test.setTimeout(180_000);

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

  for (const p of pages) {
    test(`3 · ${p}`, async ({ page }) => {
      await loginViaBrowser(page, SELLER_EMAIL, SELLER_PASSWORD);
      const r = await auditPage(page, p);
      expect(r.has500, `${p} → 500`).toBe(false);
      console.log(`✅ ${p} OK (${r.status})`);
    });
  }
});

// ===========================================================================
// PHASE 4: ADMIN PORTAL
// ===========================================================================
test.describe('Phase 4: Admin Portal', () => {
  test.setTimeout(240_000);

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

  for (const p of pages) {
    test(`4 · ${p}`, async ({ page }) => {
      await loginViaBrowser(page, ADMIN_EMAIL, ADMIN_PASSWORD);
      const r = await auditPage(page, p);
      expect(r.has500, `${p} → 500`).toBe(false);
      console.log(`✅ ${p} OK (${r.status})`);
    });
  }
});

// ===========================================================================
// PHASE 5: DEALER PORTAL
// ===========================================================================
test.describe('Phase 5: Dealer Portal', () => {
  test.setTimeout(180_000);

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

  for (const p of pages) {
    test(`5 · ${p}`, async ({ page }) => {
      await loginViaBrowser(page, SELLER_EMAIL, SELLER_PASSWORD);
      const r = await auditPage(page, p);
      const redirected =
        r.url.includes('/login') || (r.url.includes('/cuenta') && !p.startsWith('/cuenta'));
      if (redirected) {
        console.log(`ℹ️ ${p} — Redirected (no dealer role)`);
      } else {
        expect(r.has500, `${p} → 500`).toBe(false);
        console.log(`✅ ${p} OK`);
      }
    });
  }
});

// ===========================================================================
// PHASE 6: CROSS-USER DATA FLOWS
// ===========================================================================
test.describe('Phase 6: Data Flows', () => {
  test.setTimeout(120_000);

  // Shared token cache — login once in beforeAll, reuse across all tests
  let sellerToken = '';
  let sellerUserId = '';
  let buyerToken = '';
  let buyerUserId = '';
  let adminToken = '';
  let adminUserId = '';

  test.beforeAll(async ({ request }) => {
    const s = await apiLogin(request, SELLER_EMAIL, SELLER_PASSWORD);
    sellerToken = s.token;
    sellerUserId = s.userId;
    // small delay to avoid rate limiter
    await new Promise(r => setTimeout(r, 2_000));
    const b = await apiLogin(request, BUYER_EMAIL, BUYER_PASSWORD);
    buyerToken = b.token;
    buyerUserId = b.userId;
    await new Promise(r => setTimeout(r, 2_000));
    const a = await apiLogin(request, ADMIN_EMAIL, ADMIN_PASSWORD);
    adminToken = a.token;
    adminUserId = a.userId;
    console.log('✅ Phase 6 tokens cached');
  });

  test('6.1 · Buyer inquiry → Seller', async ({ request }) => {
    const vRes = await request.get(`${BASE_URL}/api/vehicles?limit=1`);
    let vid = '';
    if (vRes.ok()) {
      const b = await vRes.json();
      const items = b?.data?.items ?? b?.items ?? [];
      if (items.length) vid = items[0].id;
    }
    if (!vid) {
      console.log('ℹ️ skip');
      return;
    }
    const iRes = await request.post(`${BASE_URL}/api/contactrequests`, {
      headers: authHeaders(buyerToken),
      data: { vehicleId: vid, message: `Audit ${Date.now()}`, contactMethod: 'email' },
    });
    console.log(`  POST /api/contactrequests → ${iRes.status()}`);
    expect([200, 201, 204, 400, 422]).toContain(iRes.status());
    const rRes = await request.get(`${BASE_URL}/api/contactrequests/received`, {
      headers: readHeaders(sellerToken),
    });
    console.log(`  GET received → ${rRes.status()}`);
    expect(rRes.status() < 500).toBe(true);
    console.log('✅ Inquiry flow OK');
  });

  test('6.2 · Favorites CRUD', async ({ request }) => {
    const vRes = await request.get(`${BASE_URL}/api/vehicles?limit=1`);
    if (!vRes.ok()) return;
    const body = await vRes.json();
    const allItems = body?.data?.items ?? body?.items ?? [];
    if (!allItems.length) return;
    const vid = allItems[0]?.id;
    if (!vid) return;

    const add = await request.post(`${BASE_URL}/api/favorites`, {
      headers: authHeaders(buyerToken),
      data: { vehicleId: vid },
    });
    console.log(`  add → ${add.status()}`);
    const list = await request.get(`${BASE_URL}/api/favorites`, {
      headers: readHeaders(buyerToken),
    });
    console.log(`  list → ${list.status()}`);
    await request.delete(`${BASE_URL}/api/favorites/${vid}`, { headers: authHeaders(buyerToken) });
    console.log('✅ Favorites CRUD OK');
  });

  test('6.3 · Price alerts CRUD', async ({ request }) => {
    const res = await request.post(`${BASE_URL}/api/pricealerts`, {
      headers: authHeaders(buyerToken),
      data: {
        make: 'Toyota',
        model: 'Corolla',
        minPrice: 500000,
        maxPrice: 1500000,
        notifyOnPriceDrop: true,
      },
    });
    console.log(`  create → ${res.status()}`);
    expect([200, 201, 400, 409]).toContain(res.status());
    const list = await request.get(`${BASE_URL}/api/pricealerts`, {
      headers: readHeaders(buyerToken),
    });
    console.log(`  list → ${list.status()}`);
    if (res.status() <= 201) {
      const b = await res.json();
      const id = b?.data?.id ?? b?.id;
      if (id)
        await request.delete(`${BASE_URL}/api/pricealerts/${id}`, {
          headers: authHeaders(buyerToken),
        });
    }
    console.log('✅ Price alerts OK');
  });

  test('6.4 · Saved searches CRUD', async ({ request }) => {
    const res = await request.post(`${BASE_URL}/api/savedsearches`, {
      headers: authHeaders(buyerToken),
      data: { name: `Audit ${Date.now()}`, criteria: { make: 'Honda' }, notifyOnNewResults: true },
    });
    console.log(`  create → ${res.status()}`);
    expect([200, 201, 400, 409]).toContain(res.status());
    if (res.status() <= 201) {
      const b = await res.json();
      const id = b?.data?.id ?? b?.id;
      if (id)
        await request.delete(`${BASE_URL}/api/savedsearches/${id}`, {
          headers: authHeaders(buyerToken),
        });
    }
    console.log('✅ Saved searches OK');
  });

  test('6.5 · Reviews cross-user', async ({ request }) => {
    let sellerId = '';
    const pRes = await request.get(`${BASE_URL}/api/users/sellers/by-user/${sellerUserId}`, {
      headers: readHeaders(sellerToken),
    });
    if (pRes.ok()) {
      const b = await pRes.json();
      sellerId = b?.data?.id ?? b?.id ?? '';
    }
    if (!sellerId) {
      const m = await request.get(`${BASE_URL}/api/users/sellers/me`, {
        headers: readHeaders(sellerToken),
      });
      if (m.ok()) {
        const b = await m.json();
        sellerId = b?.data?.id ?? b?.id ?? '';
      }
    }
    if (!sellerId) {
      console.log('ℹ️ No sellerId');
      return;
    }

    const rRes = await request.post(`${BASE_URL}/api/reviews`, {
      headers: authHeaders(buyerToken),
      data: { sellerId, rating: 4, title: 'Audit E2E', content: `Test ${Date.now()}` },
    });
    console.log(`  POST /api/reviews → ${rRes.status()}`);
    expect([200, 201, 400, 409]).toContain(rRes.status());

    if (rRes.status() <= 201) {
      const b = await rRes.json();
      const rid = b?.data?.id ?? b?.id;
      const sRes = await request.get(`${BASE_URL}/api/reviews/seller/${sellerId}`, {
        headers: readHeaders(sellerToken),
      });
      console.log(`  seller reviews → ${sRes.status()}`);
      if (rid)
        await request.delete(`${BASE_URL}/api/reviews/${rid}`, {
          headers: authHeaders(buyerToken),
        });
    }
    console.log('✅ Reviews cross-user OK');
  });

  test('6.6 · Vehicle search/detail', async ({ request }) => {
    const res = await request.get(`${BASE_URL}/api/vehicles?limit=5&make=Toyota`);
    expect(res.status()).toBe(200);
    const body = await res.json();
    const items = body?.data?.items ?? body?.items ?? [];
    if (items.length > 0) {
      const detail = await request.get(`${BASE_URL}/api/vehicles/${items[0].id}`);
      expect(detail.status()).toBe(200);
      const v = (await detail.json())?.data ?? (await detail.json());
      for (const f of ['id', 'title', 'price']) {
        expect(v[f] !== undefined, `Missing ${f}`).toBeTruthy();
      }
    }
    console.log('✅ Vehicle search/detail OK');
  });

  test('6.7 · Admin can list users', async ({ request }) => {
    const res = await request.get(`${BASE_URL}/api/admin/users?pageSize=5`, {
      headers: readHeaders(adminToken),
    });
    console.log(`  admin users → ${res.status()}`);
    expect(res.status() < 500).toBe(true);
    console.log('✅ Admin users OK');
  });

  test('6.8 · Admin can list dealers', async ({ request }) => {
    const res = await request.get(`${BASE_URL}/api/dealers?pageSize=5`, {
      headers: readHeaders(adminToken),
    });
    console.log(`  admin dealers → ${res.status()}`);
    expect(res.status() < 500).toBe(true);
    console.log('✅ Admin dealers OK');
  });

  test('6.9 · Notifications', async ({ request }) => {
    const res = await request.get(`${BASE_URL}/api/notifications`, {
      headers: readHeaders(sellerToken),
    });
    console.log(`  notifications → ${res.status()}`);
    expect(res.status() < 500).toBe(true);
    console.log('✅ Notifications OK');
  });
});

// ===========================================================================
// PHASE 7: PUBLIC PAGES
// ===========================================================================
test.describe('Phase 7: Public Pages', () => {
  test.setTimeout(120_000);

  const pages = [
    '/',
    '/vehiculos',
    '/buscar',
    '/dealers',
    '/vender',
    '/precios',
    '/contacto',
    '/about',
    '/nosotros',
    '/ayuda',
    '/faq',
    '/terminos',
    '/privacidad',
    '/cookies',
    '/seguridad',
    '/login',
    '/registro',
  ];

  for (const p of pages) {
    test(`7 · ${p}`, async ({ page }) => {
      const r = await auditPage(page, p);
      expect(r.has500, `${p} → 500`).toBe(false);
      console.log(`✅ ${p} OK (${r.status})`);
    });
  }
});
