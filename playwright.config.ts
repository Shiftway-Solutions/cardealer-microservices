import { defineConfig, devices } from "@playwright/test";

/**
 * OKLA QA Configuration - Playwright
 * Optimizado para Chrome + Screenshots + Testing E2E
 */
export default defineConfig({
  testDir: "./e2e",
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : 1,
  reporter: [
    ["html", { outputFolder: "playwright-report" }],
    ["junit", { outputFile: "test-results/junit.xml" }],
    ["json", { outputFile: "test-results/results.json" }],
    ["list"],
  ],
  use: {
    baseURL: "http://localhost:3000",
    screenshot: "only-on-failure",
    video: "retain-on-failure",
    trace: "on-first-retry",
    navigationTimeout: 30000,
    actionTimeout: 10000,
  },

  projects: [
    {
      name: "chromium",
      use: {
        ...devices["Desktop Chrome"],
        viewport: { width: 1280, height: 720 },
        // Desactiva notificaciones y permisos
        launchArgs: [
          "--disable-notifications",
          "--disable-extensions",
          "--disable-popup-blocking",
        ],
      },
    },

    {
      name: "chromium-mobile",
      use: {
        ...devices["Pixel 5"],
        screenshot: "only-on-failure",
      },
    },
  ],

  webServer: {
    cmd: "npm run dev",
    url: "http://localhost:3000",
    reuseExistingServer: !process.env.CI,
    timeout: 120000,
  },
});
