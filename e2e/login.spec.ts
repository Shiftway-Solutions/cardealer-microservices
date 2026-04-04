import { test, expect } from "@playwright/test";

/**
 * QA OKLA - Prueba Login & Dashboard
 * Captura screenshots automáticamente en caso de fallo
 * Ejecutar: npx playwright test e2e/login.spec.ts
 */

test.describe("OKLA Auth Flow", () => {
  test("User Login - Screenshot on Success", async ({ page }) => {
    // Navegar a home
    await page.goto("http://localhost:3000", { waitUntil: "networkidle" });

    // Screenshot inicial
    await page.screenshot({
      path: "screenshots/01-homepage.png",
      fullPage: true,
    });

    // Buscar botón de login
    const loginButton = page.locator('button:has-text("Log in")').first();
    await expect(loginButton).toBeVisible();

    // Click en login
    await loginButton.click();
    await page.waitForURL("**/login", { timeout: 10000 });

    // Screenshot en página de login
    await page.screenshot({
      path: "screenshots/02-login-page.png",
      fullPage: true,
    });

    // Llenar formulario
    await page.fill('input[type="email"]', "buyer002@okla-test.com");
    await page.fill('input[type="password"]', "BuyerTest2026!");

    // Screenshot del formulario relleno
    await page.screenshot({
      path: "screenshots/03-login-filled.png",
      fullPage: true,
    });

    // Submit
    const submitButton = page.locator('button[type="submit"]');
    await submitButton.click();

    // Esperar redirección
    await page.waitForURL("**/dashboard", { timeout: 15000 });

    // Screenshot del dashboard
    await page.screenshot({
      path: "screenshots/04-dashboard.png",
      fullPage: true,
    });

    // Validar que estamos en dashboard
    const userMenu = page.locator('[data-testid="user-menu"]');
    await expect(userMenu).toBeVisible();
  });

  test("Failed Credentials - Show Error", async ({ page }) => {
    await page.goto("http://localhost:3000/login", {
      waitUntil: "networkidle",
    });

    // Llenar con credenciales incorrectas
    await page.fill('input[type="email"]', "invalid@example.com");
    await page.fill('input[type="password"]', "WrongPassword123");

    // Screenshot antes de submit
    await page.screenshot({
      path: "screenshots/05-bad-credentials.png",
      fullPage: true,
    });

    // Submit
    await page.locator('button[type="submit"]').click();

    // Esperar mensaje de error
    const errorMsg = page.locator('[role="alert"]');
    await expect(errorMsg).toBeVisible({ timeout: 5000 });

    // Screenshot del error
    await page.screenshot({
      path: "screenshots/06-login-error.png",
      fullPage: true,
    });
  });
});

test.describe("OKLA Marketplace Search", () => {
  test("Search Vehicles - Screenshot Results", async ({ page }) => {
    await page.goto("http://localhost:3000", { waitUntil: "networkidle" });

    // Screenshot de homepage
    await page.screenshot({
      path: "screenshots/07-homepage-full.png",
      fullPage: true,
    });

    // Buscar input
    const searchInput = page.locator('input[placeholder*="Search"]').first();
    await searchInput.fill("Toyota");

    // Screenshot con búsqueda
    await page.screenshot({
      path: "screenshots/08-search-toyota.png",
      fullPage: false, // Solo viewport
    });

    // Enter para buscar
    await searchInput.press("Enter");

    // Esperar resultados
    await page.waitForSelector('[data-testid="vehicle-card"]', {
      timeout: 10000,
    });

    // Screenshot de resultados
    await page.screenshot({
      path: "screenshots/09-search-results.png",
      fullPage: true,
    });

    // Click en primer resultado
    const firstResult = page.locator('[data-testid="vehicle-card"]').first();
    await firstResult.click();

    // Screenshot del detalle del vehículo
    await page.waitForTimeout(2000);
    await page.screenshot({
      path: "screenshots/10-vehicle-detail.png",
      fullPage: true,
    });
  });
});
