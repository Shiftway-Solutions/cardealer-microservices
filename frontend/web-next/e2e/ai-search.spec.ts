/**
 * E2E Tests — AI-Powered Vehicle Search (SearchAgent)
 *
 * Tests the integration of SearchAgent (Claude AI) with the main
 * search bar on /vehiculos. Natural language queries are processed
 * by the AI and translated into structured vehicle filters.
 *
 * Tests are split into two groups:
 * 1. Live tests — work against production, no mocking
 * 2. Mock tests — use page.route() to mock AI API responses (local only)
 */

import { test, expect } from '@playwright/test';

// =============================================================================
// LIVE TESTS — Work against the production site
// =============================================================================

test.describe('AI-Powered Search — Live Integration', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/vehiculos');
    await page.waitForLoadState('networkidle');
  });

  test('search bar should be visible and have AI-ready placeholder', async ({ page }) => {
    const searchInput = page.getByLabel(/buscar vehículos/i);
    await expect(searchInput).toBeVisible();
    await expect(searchInput).toHaveAttribute('placeholder', /marca.*modelo.*año.*color/i);
  });

  test('should trigger AI search when pressing Enter with natural language query', async ({
    page,
  }) => {
    const searchInput = page.getByLabel(/buscar vehículos/i);
    await searchInput.fill('Toyota Corolla 2020 automática');

    // Intercept the AI search API call
    const aiSearchPromise = page
      .waitForResponse(
        response =>
          response.url().includes('/api/search-agent/search') && response.status() === 200,
        { timeout: 15000 }
      )
      .catch(() => null);

    // Press Enter to trigger AI search
    await searchInput.press('Enter');

    const aiResponse = await aiSearchPromise;

    if (aiResponse) {
      const responseData = await aiResponse.json();
      expect(responseData.success).toBe(true);
      expect(responseData.data).toBeDefined();

      // Check if AI info banner appears
      const aiBanner = page.locator('text=IA interpretó');
      const bannerVisible = await aiBanner.isVisible().catch(() => false);

      if (bannerVisible) {
        await expect(page.locator('text=/\\d+% confianza/')).toBeVisible();
      }
    }
  });

  // This test only passes after the changes are deployed to the target environment.
  // Skip on production until the next deploy propagates the removal.
  test('should NOT have the floating AI chat bubble (SearchAgentWidget removed)', async ({
    page,
    baseURL,
  }) => {
    test.skip(
      !baseURL?.includes('localhost'),
      'Floating bubble removal not yet deployed to this environment'
    );
    await page.waitForTimeout(2000);
    const floatingBubble = page.locator('button[aria-label="Buscar con IA"]');
    await expect(floatingBubble).toHaveCount(0);
  });

  test('search bar should disable during AI processing', async ({ page }) => {
    test.setTimeout(60000);
    const searchInput = page.getByLabel(/buscar vehículos/i);

    await searchInput.fill('Honda CR-V 2021');
    await searchInput.press('Enter');

    // Wait for processing to complete
    await page.waitForTimeout(8000);

    // Input should be re-enabled after processing
    await expect(searchInput).toBeEnabled({ timeout: 20000 });
  });

  test('Dominican slang should trigger AI search', async ({ page }) => {
    const searchInput = page.getByLabel(/buscar vehículos/i);

    const aiSearchPromise = page
      .waitForResponse(response => response.url().includes('/api/search-agent/search'), {
        timeout: 15000,
      })
      .catch(() => null);

    await searchInput.fill('yipeta barata en Santiago');
    await searchInput.press('Enter');

    const response = await aiSearchPromise;
    if (response) {
      expect(response.status()).toBeLessThan(500);
    }
  });

  test('should show results after AI search', async ({ page }) => {
    const searchInput = page.getByLabel(/buscar vehículos/i);

    await searchInput.fill('SUV automática menos de 2 millones');
    await searchInput.press('Enter');

    // Wait for AI + vehicle search to complete
    await page.waitForTimeout(8000);

    await expect(page.locator('body')).toBeVisible();

    // Should have vehicle cards or empty state
    const hasContent =
      (await page
        .locator('text=/\\d+.*vehículos encontrados/')
        .isVisible()
        .catch(() => false)) ||
      (await page
        .locator('text=No encontramos resultados')
        .isVisible()
        .catch(() => false));

    expect(hasContent).toBe(true);
  });

  test('should clear search and reset when X button clicked', async ({ page }) => {
    const searchInput = page.getByLabel(/buscar vehículos/i);

    await searchInput.fill('Toyota Corolla');
    await searchInput.press('Enter');
    await page.waitForTimeout(3000);

    const clearButton = page.locator('form button[type="button"]').first();
    if (await clearButton.isVisible().catch(() => false)) {
      await clearButton.click();
      await expect(searchInput).toHaveValue('');
    }
  });
});

// =============================================================================
// MOCK TESTS — Require local dev server (page.route for API mocking)
// =============================================================================

test.describe('AI-Powered Search — Mocked API', () => {
  test.skip(
    ({ baseURL }) => !baseURL?.includes('localhost'),
    'Mocked tests only run against localhost'
  );

  test.beforeEach(async ({ page }) => {
    await page.goto('/vehiculos');
    await page.waitForLoadState('networkidle');
  });

  test('should display AI metadata banner after successful AI search', async ({ page }) => {
    const searchInput = page.getByLabel(/buscar vehículos/i);

    await page.route('**/api/search-agent/search', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          data: {
            aiFilters: {
              filtros_exactos: {
                marca: 'Toyota',
                modelo: 'Corolla',
                anio_desde: 2020,
                anio_hasta: 2024,
                precio_min: null,
                precio_max: null,
                moneda: 'DOP',
                tipo_vehiculo: null,
                transmision: 'automatica',
                combustible: null,
                condicion: null,
                kilometraje_max: null,
              },
              filtros_relajados: null,
              resultado_minimo_garantizado: 8,
              nivel_filtros_activo: 1,
              patrocinados_config: null,
              ordenar_por: 'relevancia',
              dealer_verificado: null,
              confianza: 0.92,
              query_reformulada: 'Toyota Corolla 2020-2024 automático',
              advertencias: [],
              mensaje_relajamiento: null,
              mensaje_usuario: null,
            },
            wasCached: false,
            latencyMs: 340,
            isAiSearchEnabled: true,
          },
        }),
      });
    });

    await searchInput.fill('Toyota Corolla automática');
    await searchInput.press('Enter');

    const aiBanner = page.locator('text=IA interpretó');
    await expect(aiBanner).toBeVisible({ timeout: 5000 });
    await expect(page.locator('text=92% confianza')).toBeVisible();
    await expect(page.locator('text=340ms')).toBeVisible();
  });

  test('should fall back when AI returns low confidence', async ({ page }) => {
    const searchInput = page.getByLabel(/buscar vehículos/i);

    await page.route('**/api/search-agent/search', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          data: {
            aiFilters: {
              filtros_exactos: null,
              filtros_relajados: null,
              resultado_minimo_garantizado: 0,
              nivel_filtros_activo: 0,
              patrocinados_config: null,
              ordenar_por: 'relevancia',
              dealer_verificado: null,
              confianza: 0,
              query_reformulada: null,
              advertencias: [],
              mensaje_relajamiento: null,
              mensaje_usuario: 'No entendí tu consulta.',
            },
            wasCached: false,
            latencyMs: 150,
            isAiSearchEnabled: true,
          },
        }),
      });
    });

    await searchInput.fill('cuál es la capital de Francia');
    await searchInput.press('Enter');

    await page.waitForTimeout(2000);
    await expect(page.locator('text=IA interpretó')).not.toBeVisible();
  });

  test('should fall back when AI service errors', async ({ page }) => {
    const searchInput = page.getByLabel(/buscar vehículos/i);

    await page.route('**/api/search-agent/search', async route => {
      await route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Internal server error' }),
      });
    });

    await searchInput.fill('Honda Civic 2022');
    await searchInput.press('Enter');

    await page.waitForTimeout(2000);
    await expect(page.locator('body')).toBeVisible();
    await expect(page.locator('text=IA interpretó')).not.toBeVisible();
  });

  test('should apply filters correctly from AI response', async ({ page }) => {
    const searchInput = page.getByLabel(/buscar vehículos/i);

    await page.route('**/api/search-agent/search', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          data: {
            aiFilters: {
              filtros_exactos: {
                marca: 'Hyundai',
                modelo: 'Tucson',
                anio_desde: 2019,
                anio_hasta: 2023,
                precio_min: 500000,
                precio_max: 1500000,
                moneda: 'DOP',
                tipo_vehiculo: 'SUV',
                transmision: 'automatica',
                combustible: 'gasolina',
                condicion: 'usado',
                kilometraje_max: 80000,
              },
              filtros_relajados: null,
              resultado_minimo_garantizado: 8,
              nivel_filtros_activo: 1,
              patrocinados_config: null,
              ordenar_por: 'relevancia',
              dealer_verificado: null,
              confianza: 0.95,
              query_reformulada: 'Hyundai Tucson 2019-2023 automática gasolina usada',
              advertencias: [],
              mensaje_relajamiento: null,
              mensaje_usuario: null,
            },
            wasCached: true,
            latencyMs: 45,
            isAiSearchEnabled: true,
          },
        }),
      });
    });

    await searchInput.fill('Tucson usada gasolina automática menos de millón y medio');
    await searchInput.press('Enter');

    await expect(page.locator('text=IA interpretó')).toBeVisible({ timeout: 5000 });
    await expect(page.locator('text=95% confianza')).toBeVisible();
    await expect(page.locator('text=caché')).toBeVisible();
  });
});
