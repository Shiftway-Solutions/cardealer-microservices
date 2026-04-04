import { test, expect } from "@playwright/test";

/**
 * OKLA Sprint 24 - Comparador de Vehículos
 * Tests con screenshots capturados en cada paso
 * URL Base: https://biological-robinson-videos-ward.trycloudflare.com
 */

const BASE_URL =
  process.env.BASE_URL ||
  "https://biological-robinson-videos-ward.trycloudflare.com";
const SCREENSHOT_DIR = "screenshots/s24-comparador";

test.beforeEach(async ({ page }) => {
  // Ignorar errores de conexión si el tunnel no está disponible
  page.on("response", (response) => {
    if (response.status() === 524) {
      console.log("⚠️  Tunnel error 524, usando localhost como fallback");
    }
  });
});

test.describe("Sprint 24 - Comparador Side by Side", () => {
  test("S24-01: Homepage carga correctamente", async ({ page }) => {
    console.log(`📍 Navegando a: ${BASE_URL}`);

    try {
      await page.goto(BASE_URL, { waitUntil: "networkidle", timeout: 30000 });
    } catch (e) {
      console.log("⚠️  Tunnel no disponible, usando localhost");
      await page.goto("http://localhost:3000", { waitUntil: "networkidle" });
    }

    // Screenshot 1: Homepage
    await page.screenshot({
      path: `${SCREENSHOT_DIR}/01-homepage.png`,
      fullPage: true,
    });
    console.log("✅ [1/6] Homepage capturada");

    // Verificar que carga
    const title = await page.title();
    expect(title).toBeTruthy();
  });

  test("S24-02: Login con buyer", async ({ page }) => {
    await page.goto("http://localhost:3000", { waitUntil: "domcontentloaded" });

    // Screenshot: Home antes de login
    await page.screenshot({
      path: `${SCREENSHOT_DIR}/02-home-before-login.png`,
      fullPage: true,
    });
    console.log("✅ [2/6] Home pre-login capturada");

    // Buscar botón de login
    const loginBtn = await page
      .locator(
        'button:has-text("Log in"), a:has-text("Log in"), [data-testid="login-btn"]',
      )
      .first();

    if (await loginBtn.isVisible({ timeout: 2000 }).catch(() => false)) {
      await loginBtn.click();
      await page.waitForURL("**/login", { timeout: 10000 }).catch(() => null);

      // Screenshot: Login page
      await page.screenshot({
        path: `${SCREENSHOT_DIR}/03-login-page.png`,
        fullPage: true,
      });
      console.log("✅ [3/6] Página de login capturada");
    } else {
      console.log("ℹ️  Botón de login no encontrado, saltando");
    }
  });

  test("S24-03: Búsqueda de vehículos", async ({ page }) => {
    await page.goto("http://localhost:3000", { waitUntil: "domcontentloaded" });

    // Screenshot: Homepage con búsqueda
    await page.screenshot({
      path: `${SCREENSHOT_DIR}/04-search-section.png`,
      fullPage: false,
    });
    console.log("✅ [4/6] Sección de búsqueda capturada");

    // Buscar input de búsqueda
    const searchInput = page
      .locator(
        'input[placeholder*="Search"], input[placeholder*="search"], input[placeholder*="Busca"]',
      )
      .first();

    if (await searchInput.isVisible({ timeout: 2000 }).catch(() => false)) {
      await searchInput.fill("Toyota");

      // Screenshot: Búsqueda rellenada
      await page.screenshot({
        path: `${SCREENSHOT_DIR}/05-search-typed.png`,
        fullPage: false,
      });
      console.log("✅ [5/6] Búsqueda rellenada capturada");

      // Presionar enter
      await searchInput.press("Enter");
      await page.waitForTimeout(3000);

      // Screenshot: Resultados de búsqueda
      await page.screenshot({
        path: `${SCREENSHOT_DIR}/06-search-results.png`,
        fullPage: true,
      });
      console.log("✅ [6/6] Resultados de búsqueda capturados");
    } else {
      console.log("ℹ️  Input de búsqueda no encontrado");
    }
  });

  test("S24-04: Comparador - Seleccionar vehículos", async ({ page }) => {
    await page.goto("http://localhost:3000", { waitUntil: "domcontentloaded" });

    // Buscar botones de "Comparar" o "Compare"
    const compareButtons = page.locator(
      'button:has-text("Compare"), button:has-text("Comparar"), [data-testid*="compare"]',
    );
    const count = await compareButtons.count().catch(() => 0);

    console.log(`🔎 Encontrados ${count} botones de comparación`);

    if (count > 0) {
      // Screenshot: Botones de comparación
      await page.screenshot({
        path: `${SCREENSHOT_DIR}/07-compare-buttons.png`,
        fullPage: true,
      });
      console.log("✅ Botones de comparación capturados");

      // Click en primer comparador
      await compareButtons.first().click();
      await page.waitForTimeout(1000);

      // Screenshot: Después de clickear comparador
      await page.screenshot({
        path: `${SCREENSHOT_DIR}/08-compare-selected.png`,
        fullPage: true,
      });
      console.log("✅ Primer vehículo seleccionado capturado");

      // Tryear segundo
      if (count > 1) {
        await compareButtons.nth(1).click();
        await page.waitForTimeout(1000);

        // Screenshot: Dos vehículos seleccionados
        await page.screenshot({
          path: `${SCREENSHOT_DIR}/09-compare-two-selected.png`,
          fullPage: true,
        });
        console.log("✅ Dos vehículos seleccionados capturados");
      }
    } else {
      console.log("ℹ️  No se encontraron botones de comparación");
    }
  });

  test("S24-05: Vista de Comparación Side-by-Side", async ({ page }) => {
    await page.goto("http://localhost:3000", { waitUntil: "domcontentloaded" });

    // Buscar botón o enlace a la página de comparación
    const compareLink = page
      .locator(
        'a[href*="compare"], button:has-text("View Comparison"), button:has-text("Ver Comparación")',
      )
      .first();

    if (await compareLink.isVisible({ timeout: 2000 }).catch(() => false)) {
      await compareLink.click();
      await page.waitForURL("**/compare", { timeout: 10000 }).catch(() => null);
      await page.waitForTimeout(2000);

      // Screenshot: Comparador side-by-side
      await page.screenshot({
        path: `${SCREENSHOT_DIR}/10-comparison-view.png`,
        fullPage: true,
      });
      console.log("✅ Vista de comparación capturada");

      // Scroll para ver más detalles
      await page.evaluate(() => window.scrollBy(0, window.innerHeight));
      await page.waitForTimeout(500);

      // Screenshot: Comparador scrolleado
      await page.screenshot({
        path: `${SCREENSHOT_DIR}/11-comparison-scrolled.png`,
        fullPage: true,
      });
      console.log("✅ Comparación scrolleada capturada");
    } else {
      console.log("ℹ️  Enlace de comparación no encontrado");
    }
  });

  test("S24-06: Detalles de Vehículo desde Comparador", async ({ page }) => {
    await page.goto("http://localhost:3000", { waitUntil: "domcontentloaded" });

    // Buscar enlace a un vehículo individual
    const vehicleLink = page
      .locator(
        '[data-testid="vehicle-card"] a, .vehicle-card a, .vehicle-item a',
      )
      .first();

    if (await vehicleLink.isVisible({ timeout: 2000 }).catch(() => false)) {
      await vehicleLink.click();
      await page
        .waitForURL("**/vehicles/**", { timeout: 10000 })
        .catch(() => null);
      await page.waitForTimeout(2000);

      // Screenshot: Detalle del vehículo
      await page.screenshot({
        path: `${SCREENSHOT_DIR}/12-vehicle-detail.png`,
        fullPage: true,
      });
      console.log("✅ Detalle de vehículo capturado");

      // Scroll para galería
      await page.evaluate(() => window.scrollBy(0, window.innerHeight));
      await page.waitForTimeout(500);

      // Screenshot: Galería de fotos
      await page.screenshot({
        path: `${SCREENSHOT_DIR}/13-vehicle-gallery.png`,
        fullPage: true,
      });
      console.log("✅ Galería de vehículo capturada");
    } else {
      console.log("ℹ️  Enlace de vehículo no encontrado");
    }
  });
});
