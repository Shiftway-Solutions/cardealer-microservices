/**
 * Playwright config for running E2E tests against production (https://okla.com.do)
 * No local dev server required.
 */
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  forbidOnly: false,
  retries: 1,
  workers: 1,
  timeout: 60000,
  reporter: [
    ['line'],
    ['html', { outputFolder: 'playwright-report-prod', open: 'never' }],
    ['json', { outputFile: 'test-results/results-prod.json' }],
  ],
  use: {
    baseURL: process.env.PLAYWRIGHT_BASE_URL || 'https://okla.com.do',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    actionTimeout: 15000,
    navigationTimeout: 30000,
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  // No webServer - tests run against live production URL
});
