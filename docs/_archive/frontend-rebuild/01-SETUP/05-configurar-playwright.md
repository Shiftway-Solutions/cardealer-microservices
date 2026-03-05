# 🎭 Configurar Playwright para E2E

> **Tiempo estimado:** 20 minutos
> **Prerrequisitos:** Proyecto Next.js configurado

---

## 📋 OBJETIVO

Configurar Playwright para pruebas E2E:

- Instalación y configuración
- Estructura de tests
- Helpers y fixtures
- CI/CD integration

---

## 🔧 PASO 1: Instalar Playwright

```bash
# Instalar Playwright con browsers
pnpm create playwright

# Responder a las preguntas:
# ✔ Where to put your end-to-end tests? › e2e
# ✔ Add a GitHub Actions workflow? › true
# ✔ Install Playwright browsers? › true
```

---

## 🔧 PASO 2: Configurar Playwright

```typescript
// filepath: playwright.config.ts
import { defineConfig, devices } from "@playwright/test";

/**
 * See https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  // Test directory
  testDir: "./e2e",

  // Run tests in parallel
  fullyParallel: true,

  // Fail the build on CI if you accidentally left test.only in the source code
  forbidOnly: !!process.env.CI,

  // Retry on CI only
  retries: process.env.CI ? 2 : 0,

  // Opt out of parallel tests on CI
  workers: process.env.CI ? 1 : undefined,

  // Reporter to use
  reporter: [
    ["html", { open: "never" }],
    ["list"],
    ...(process.env.CI ? [["github"] as const] : []),
  ],

  // Shared settings for all projects
  use: {
    // Base URL for navigation
    baseURL: process.env.PLAYWRIGHT_BASE_URL || "http://localhost:3000",

    // Collect trace when retrying the failed test
    trace: "on-first-retry",

    // Capture screenshot on failure
    screenshot: "only-on-failure",

    // Capture video on failure
    video: "on-first-retry",

    // Locale and timezone for consistent tests
    locale: "es-DO",
    timezoneId: "America/Santo_Domingo",
  },

  // Configure projects for major browsers
  projects: [
    // Desktop browsers
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
    {
      name: "firefox",
      use: { ...devices["Desktop Firefox"] },
    },
    {
      name: "webkit",
      use: { ...devices["Desktop Safari"] },
    },

    // Mobile browsers
    {
      name: "mobile-chrome",
      use: { ...devices["Pixel 5"] },
    },
    {
      name: "mobile-safari",
      use: { ...devices["iPhone 12"] },
    },
  ],

  // Web server to use during tests
  webServer: {
    command: "pnpm dev",
    url: "http://localhost:3000",
    reuseExistingServer: !process.env.CI,
    timeout: 120 * 1000,
  },

  // Global timeout for each test
  timeout: 30 * 1000,

  // Timeout for each assertion
  expect: {
    timeout: 10 * 1000,
  },

  // Output directory for test artifacts
  outputDir: "test-results",
});
```

---

## 🔧 PASO 3: Configurar Fixtures

```typescript
// filepath: e2e/fixtures/index.ts
import { test as base, expect, Page } from "@playwright/test";

// Extend base test with custom fixtures
export const test = base.extend<{
  homePage: Page;
  searchPage: Page;
  vehiclePage: Page;
  authenticatedPage: Page;
}>({
  // Page already on homepage
  homePage: async ({ page }, use) => {
    await page.goto("/");
    await page.waitForLoadState("networkidle");
    await use(page);
  },

  // Page on search with filters
  searchPage: async ({ page }, use) => {
    await page.goto("/vehiculos");
    await page.waitForLoadState("networkidle");
    await use(page);
  },

  // Page on vehicle detail
  vehiclePage: async ({ page }, use) => {
    await page.goto("/vehiculos");
    await page.waitForLoadState("networkidle");
    // Click first vehicle
    await page.locator('[data-testid="vehicle-card"]').first().click();
    await page.waitForLoadState("networkidle");
    await use(page);
  },

  // Authenticated page
  authenticatedPage: async ({ page, context }, use) => {
    // Set auth cookies/tokens
    await context.addCookies([
      {
        name: "next-auth.session-token",
        value: process.env.TEST_SESSION_TOKEN || "test-token",
        domain: "localhost",
        path: "/",
      },
    ]);

    await page.goto("/dashboard");
    await page.waitForLoadState("networkidle");
    await use(page);
  },
});

export { expect };
```

---

## 🔧 PASO 4: Crear Page Objects

```typescript
// filepath: e2e/pages/HomePage.ts
import { Page, Locator, expect } from "@playwright/test";

export class HomePage {
  readonly page: Page;
  readonly heroTitle: Locator;
  readonly searchButton: Locator;
  readonly sellButton: Locator;
  readonly quickSearchMake: Locator;
  readonly quickSearchModel: Locator;
  readonly quickSearchButton: Locator;
  readonly featuredVehicles: Locator;
  readonly categoryCards: Locator;
  readonly makeLogos: Locator;

  constructor(page: Page) {
    this.page = page;
    this.heroTitle = page.getByRole("heading", {
      name: /encuentra tu vehículo/i,
    });
    this.searchButton = page.getByRole("link", { name: /buscar vehículos/i });
    this.sellButton = page.getByRole("link", { name: /vender mi vehículo/i });
    this.quickSearchMake = page.getByLabel("Marca");
    this.quickSearchModel = page.getByLabel("Modelo");
    this.quickSearchButton = page
      .locator("section")
      .filter({ hasText: "Marca" })
      .getByRole("button", { name: /buscar/i });
    this.featuredVehicles = page.locator('[data-testid="vehicle-card"]');
    this.categoryCards = page.locator('[data-testid="category-card"]');
    this.makeLogos = page.locator('[data-testid="make-logo"]');
  }

  async goto() {
    await this.page.goto("/");
  }

  async expectToBeLoaded() {
    await expect(this.heroTitle).toBeVisible();
    await expect(this.featuredVehicles.first()).toBeVisible({ timeout: 10000 });
  }

  async searchByMake(make: string) {
    await this.quickSearchMake.click();
    await this.page.getByRole("option", { name: make }).click();
  }

  async submitQuickSearch() {
    await this.quickSearchButton.click();
    await this.page.waitForURL(/\/vehiculos/);
  }

  async clickFirstVehicle() {
    await this.featuredVehicles.first().click();
    await this.page.waitForURL(/\/vehiculos\/.+/);
  }

  async clickCategory(category: string) {
    await this.page
      .locator('[data-testid="category-card"]')
      .filter({ hasText: category })
      .click();
    await this.page.waitForURL(/\/vehiculos/);
  }
}
```

```typescript
// filepath: e2e/pages/SearchPage.ts
import { Page, Locator, expect } from "@playwright/test";

export class SearchPage {
  readonly page: Page;
  readonly searchInput: Locator;
  readonly filterMake: Locator;
  readonly filterModel: Locator;
  readonly filterYearMin: Locator;
  readonly filterYearMax: Locator;
  readonly filterPriceMin: Locator;
  readonly filterPriceMax: Locator;
  readonly filterCondition: Locator;
  readonly vehicleCards: Locator;
  readonly resultsCount: Locator;
  readonly sortSelect: Locator;
  readonly pagination: Locator;
  readonly noResults: Locator;
  readonly loadingState: Locator;

  constructor(page: Page) {
    this.page = page;
    this.searchInput = page.getByPlaceholder(/buscar/i);
    this.filterMake = page.getByLabel("Marca");
    this.filterModel = page.getByLabel("Modelo");
    this.filterYearMin = page.getByLabel("Año desde");
    this.filterYearMax = page.getByLabel("Año hasta");
    this.filterPriceMin = page.getByLabel("Precio mínimo");
    this.filterPriceMax = page.getByLabel("Precio máximo");
    this.filterCondition = page.getByLabel("Condición");
    this.vehicleCards = page.locator('[data-testid="vehicle-card"]');
    this.resultsCount = page.getByTestId("results-count");
    this.sortSelect = page.getByLabel("Ordenar por");
    this.pagination = page.getByRole("navigation", { name: /paginación/i });
    this.noResults = page.getByTestId("empty-state");
    this.loadingState = page.locator('[data-testid="loading-skeleton"]');
  }

  async goto(params?: string) {
    await this.page.goto(`/vehiculos${params ? `?${params}` : ""}`);
  }

  async expectToBeLoaded() {
    await expect(this.vehicleCards.first()).toBeVisible({ timeout: 10000 });
  }

  async filterByMake(make: string) {
    await this.filterMake.click();
    await this.page.getByRole("option", { name: make }).click();
    await this.waitForResults();
  }

  async filterByYear(min: number, max: number) {
    await this.filterYearMin.fill(min.toString());
    await this.filterYearMax.fill(max.toString());
    await this.waitForResults();
  }

  async filterByPrice(min: number, max: number) {
    await this.filterPriceMin.fill(min.toString());
    await this.filterPriceMax.fill(max.toString());
    await this.waitForResults();
  }

  async sortBy(option: string) {
    await this.sortSelect.click();
    await this.page.getByRole("option", { name: option }).click();
    await this.waitForResults();
  }

  async waitForResults() {
    await this.page.waitForResponse(
      (response) =>
        response.url().includes("/api/vehicles") && response.status() === 200,
    );
    await expect(this.loadingState).not.toBeVisible();
  }

  async getResultsCount(): Promise<number> {
    const text = await this.resultsCount.textContent();
    const match = text?.match(/(\d+)/);
    return match ? parseInt(match[1]) : 0;
  }

  async clickVehicle(index: number) {
    await this.vehicleCards.nth(index).click();
    await this.page.waitForURL(/\/vehiculos\/.+/);
  }

  async goToPage(pageNumber: number) {
    await this.pagination
      .getByRole("button", { name: `${pageNumber}` })
      .click();
    await this.waitForResults();
  }
}
```

```typescript
// filepath: e2e/pages/VehicleDetailPage.ts
import { Page, Locator, expect } from "@playwright/test";

export class VehicleDetailPage {
  readonly page: Page;
  readonly title: Locator;
  readonly price: Locator;
  readonly gallery: Locator;
  readonly mainImage: Locator;
  readonly thumbnails: Locator;
  readonly specsSection: Locator;
  readonly featuresSection: Locator;
  readonly sellerInfo: Locator;
  readonly contactButton: Locator;
  readonly favoriteButton: Locator;
  readonly shareButton: Locator;
  readonly similarVehicles: Locator;

  constructor(page: Page) {
    this.page = page;
    this.title = page.getByRole("heading", { level: 1 });
    this.price = page.getByTestId("vehicle-price");
    this.gallery = page.getByTestId("vehicle-gallery");
    this.mainImage = page.locator('[data-testid="gallery-main-image"]');
    this.thumbnails = page.locator('[data-testid="gallery-thumbnail"]');
    this.specsSection = page.getByTestId("vehicle-specs");
    this.featuresSection = page.getByTestId("vehicle-features");
    this.sellerInfo = page.getByTestId("seller-info");
    this.contactButton = page.getByRole("button", { name: /contactar/i });
    this.favoriteButton = page.getByRole("button", { name: /favorito/i });
    this.shareButton = page.getByRole("button", { name: /compartir/i });
    this.similarVehicles = page.getByTestId("similar-vehicles");
  }

  async goto(slug: string) {
    await this.page.goto(`/vehiculos/${slug}`);
  }

  async expectToBeLoaded() {
    await expect(this.title).toBeVisible();
    await expect(this.price).toBeVisible();
    await expect(this.mainImage).toBeVisible();
  }

  async clickThumbnail(index: number) {
    await this.thumbnails.nth(index).click();
  }

  async openLightbox() {
    await this.mainImage.click();
    await expect(this.page.locator('[role="dialog"]')).toBeVisible();
  }

  async closeLightbox() {
    await this.page.keyboard.press("Escape");
    await expect(this.page.locator('[role="dialog"]')).not.toBeVisible();
  }

  async toggleFavorite() {
    await this.favoriteButton.click();
  }

  async openContactModal() {
    await this.contactButton.click();
    await expect(this.page.locator('[role="dialog"]')).toBeVisible();
  }

  async shareVehicle() {
    await this.shareButton.click();
  }
}
```

---

## 🔧 PASO 5: Crear Tests E2E

```typescript
// filepath: e2e/home.spec.ts
import { test, expect } from "./fixtures";
import { HomePage } from "./pages/HomePage";

test.describe("Homepage", () => {
  test("should load and display hero section", async ({ page }) => {
    const homePage = new HomePage(page);
    await homePage.goto();
    await homePage.expectToBeLoaded();

    await expect(homePage.searchButton).toBeVisible();
    await expect(homePage.sellButton).toBeVisible();
  });

  test("should display featured vehicles", async ({ page }) => {
    const homePage = new HomePage(page);
    await homePage.goto();
    await homePage.expectToBeLoaded();

    const count = await homePage.featuredVehicles.count();
    expect(count).toBeGreaterThan(0);
  });

  test("should navigate to search via quick search", async ({ page }) => {
    const homePage = new HomePage(page);
    await homePage.goto();
    await homePage.expectToBeLoaded();

    await homePage.searchByMake("Toyota");
    await homePage.submitQuickSearch();

    await expect(page).toHaveURL(/\/vehiculos\?.*makeId=/);
  });

  test("should navigate to vehicle detail on click", async ({ page }) => {
    const homePage = new HomePage(page);
    await homePage.goto();
    await homePage.expectToBeLoaded();

    await homePage.clickFirstVehicle();
    await expect(page).toHaveURL(/\/vehiculos\/.+/);
  });

  test("should navigate to category on click", async ({ page }) => {
    const homePage = new HomePage(page);
    await homePage.goto();
    await homePage.expectToBeLoaded();

    await homePage.clickCategory("SUVs");
    await expect(page).toHaveURL(/\/vehiculos\?.*bodyType=suv/);
  });
});
```

```typescript
// filepath: e2e/search.spec.ts
import { test, expect } from "./fixtures";
import { SearchPage } from "./pages/SearchPage";

test.describe("Search Page", () => {
  test("should load and display vehicles", async ({ page }) => {
    const searchPage = new SearchPage(page);
    await searchPage.goto();
    await searchPage.expectToBeLoaded();

    const count = await searchPage.vehicleCards.count();
    expect(count).toBeGreaterThan(0);
  });

  test("should filter by make", async ({ page }) => {
    const searchPage = new SearchPage(page);
    await searchPage.goto();
    await searchPage.expectToBeLoaded();

    await searchPage.filterByMake("Toyota");

    // All visible vehicles should be Toyota
    const vehicles = await searchPage.vehicleCards.allTextContents();
    for (const vehicle of vehicles) {
      expect(vehicle).toContain("Toyota");
    }
  });

  test("should filter by price range", async ({ page }) => {
    const searchPage = new SearchPage(page);
    await searchPage.goto();
    await searchPage.expectToBeLoaded();

    await searchPage.filterByPrice(500000, 1000000);

    // Results should be filtered
    const count = await searchPage.getResultsCount();
    expect(count).toBeGreaterThan(0);
  });

  test("should sort results", async ({ page }) => {
    const searchPage = new SearchPage(page);
    await searchPage.goto();
    await searchPage.expectToBeLoaded();

    await searchPage.sortBy("Precio: menor a mayor");

    // First vehicle should have lowest price
    const firstCard = searchPage.vehicleCards.first();
    await expect(firstCard).toBeVisible();
  });

  test("should navigate to vehicle detail", async ({ page }) => {
    const searchPage = new SearchPage(page);
    await searchPage.goto();
    await searchPage.expectToBeLoaded();

    await searchPage.clickVehicle(0);
    await expect(page).toHaveURL(/\/vehiculos\/.+/);
  });

  test("should paginate results", async ({ page }) => {
    const searchPage = new SearchPage(page);
    await searchPage.goto();
    await searchPage.expectToBeLoaded();

    // Only if there's pagination
    if (await searchPage.pagination.isVisible()) {
      await searchPage.goToPage(2);
      await expect(page).toHaveURL(/page=2/);
    }
  });

  test("should show empty state when no results", async ({ page }) => {
    const searchPage = new SearchPage(page);
    // Search with impossible criteria
    await searchPage.goto("makeId=nonexistent");

    await expect(searchPage.noResults).toBeVisible();
    await expect(searchPage.vehicleCards).toHaveCount(0);
  });
});
```

```typescript
// filepath: e2e/vehicle-detail.spec.ts
import { test, expect } from "./fixtures";
import { VehicleDetailPage } from "./pages/VehicleDetailPage";
import { SearchPage } from "./pages/SearchPage";

test.describe("Vehicle Detail Page", () => {
  test("should load vehicle details", async ({ page }) => {
    // First get a vehicle from search
    const searchPage = new SearchPage(page);
    await searchPage.goto();
    await searchPage.expectToBeLoaded();
    await searchPage.clickVehicle(0);

    const detailPage = new VehicleDetailPage(page);
    await detailPage.expectToBeLoaded();

    await expect(detailPage.specsSection).toBeVisible();
  });

  test("should display gallery with thumbnails", async ({ page }) => {
    const searchPage = new SearchPage(page);
    await searchPage.goto();
    await searchPage.expectToBeLoaded();
    await searchPage.clickVehicle(0);

    const detailPage = new VehicleDetailPage(page);
    await detailPage.expectToBeLoaded();

    await expect(detailPage.mainImage).toBeVisible();
    const thumbnailCount = await detailPage.thumbnails.count();
    expect(thumbnailCount).toBeGreaterThan(0);
  });

  test("should open and close lightbox", async ({ page }) => {
    const searchPage = new SearchPage(page);
    await searchPage.goto();
    await searchPage.expectToBeLoaded();
    await searchPage.clickVehicle(0);

    const detailPage = new VehicleDetailPage(page);
    await detailPage.expectToBeLoaded();

    await detailPage.openLightbox();
    await detailPage.closeLightbox();
  });

  test("should show contact button", async ({ page }) => {
    const searchPage = new SearchPage(page);
    await searchPage.goto();
    await searchPage.expectToBeLoaded();
    await searchPage.clickVehicle(0);

    const detailPage = new VehicleDetailPage(page);
    await detailPage.expectToBeLoaded();

    await expect(detailPage.contactButton).toBeVisible();
    await expect(detailPage.contactButton).toBeEnabled();
  });

  test("should display similar vehicles", async ({ page }) => {
    const searchPage = new SearchPage(page);
    await searchPage.goto();
    await searchPage.expectToBeLoaded();
    await searchPage.clickVehicle(0);

    const detailPage = new VehicleDetailPage(page);
    await detailPage.expectToBeLoaded();

    // Scroll to similar vehicles
    await detailPage.similarVehicles.scrollIntoViewIfNeeded();
    await expect(detailPage.similarVehicles).toBeVisible();
  });
});
```

---

## 🔧 PASO 6: Scripts en package.json

```json
// filepath: package.json (agregar scripts)
{
  "scripts": {
    "test:e2e": "playwright test",
    "test:e2e:ui": "playwright test --ui",
    "test:e2e:headed": "playwright test --headed",
    "test:e2e:debug": "playwright test --debug",
    "test:e2e:chromium": "playwright test --project=chromium",
    "test:e2e:mobile": "playwright test --project=mobile-chrome --project=mobile-safari",
    "test:e2e:report": "playwright show-report"
  }
}
```

---

## 🔧 PASO 7: GitHub Actions Workflow

```yaml
# filepath: .github/workflows/e2e.yml
name: E2E Tests

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]

jobs:
  e2e:
    timeout-minutes: 20
    runs-on: ubuntu-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: "20"

      - name: Install pnpm
        uses: pnpm/action-setup@v3
        with:
          version: 9

      - name: Get pnpm store directory
        shell: bash
        run: |
          echo "STORE_PATH=$(pnpm store path --silent)" >> $GITHUB_ENV

      - name: Setup pnpm cache
        uses: actions/cache@v4
        with:
          path: ${{ env.STORE_PATH }}
          key: ${{ runner.os }}-pnpm-store-${{ hashFiles('**/pnpm-lock.yaml') }}
          restore-keys: |
            ${{ runner.os }}-pnpm-store-

      - name: Install dependencies
        run: pnpm install

      - name: Install Playwright Browsers
        run: pnpm exec playwright install --with-deps

      - name: Run Playwright tests
        run: pnpm test:e2e
        env:
          PLAYWRIGHT_BASE_URL: http://localhost:3000

      - name: Upload Playwright Report
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: playwright-report
          path: playwright-report/
          retention-days: 30

      - name: Upload Test Results
        uses: actions/upload-artifact@v4
        if: failure()
        with:
          name: test-results
          path: test-results/
          retention-days: 7
```

---

## ✅ VALIDACIÓN

### Ejecutar tests

```bash
# Todos los tests
pnpm test:e2e

# Con UI interactiva
pnpm test:e2e:ui

# Solo Chromium
pnpm test:e2e:chromium

# Ver reporte
pnpm test:e2e:report
```

### Output esperado

```
Running 15 tests using 4 workers

  ✓ home.spec.ts:5:5 › Homepage › should load and display hero section (2.3s)
  ✓ home.spec.ts:12:5 › Homepage › should display featured vehicles (1.8s)
  ✓ home.spec.ts:20:5 › Homepage › should navigate to search via quick search (2.1s)
  ✓ search.spec.ts:5:5 › Search Page › should load and display vehicles (2.0s)
  ✓ search.spec.ts:12:5 › Search Page › should filter by make (2.5s)
  ...

  15 passed (45.2s)
```

---

## 📊 RESUMEN

### Estructura de archivos

```
e2e/
├── fixtures/
│   └── index.ts           # Custom fixtures
├── pages/
│   ├── HomePage.ts        # Page object
│   ├── SearchPage.ts      # Page object
│   └── VehicleDetailPage.ts # Page object
├── home.spec.ts           # Tests homepage
├── search.spec.ts         # Tests búsqueda
└── vehicle-detail.spec.ts # Tests detalle
```

### Comandos

| Comando                | Función        |
| ---------------------- | -------------- |
| `pnpm test:e2e`        | Ejecutar todos |
| `pnpm test:e2e:ui`     | UI interactiva |
| `pnpm test:e2e:headed` | Ver browser    |
| `pnpm test:e2e:debug`  | Modo debug     |
| `pnpm test:e2e:report` | Ver reporte    |

### Configuración

| Opción      | Valor                             |
| ----------- | --------------------------------- |
| Browsers    | Chromium, Firefox, Safari, Mobile |
| Timeout     | 30s por test                      |
| Retries     | 2 en CI                           |
| Traces      | On first retry                    |
| Screenshots | On failure                        |

---

## ➡️ SIGUIENTE PASO

Con la configuración de Playwright completada, el setup del proyecto está listo.

Continuar con: `docs/frontend-rebuild/02-UX-DESIGN-SYSTEM/05-animaciones.md`
