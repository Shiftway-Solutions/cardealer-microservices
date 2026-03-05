# 🚀 CI/CD Integration for Testing

> **Tiempo estimado:** 30 minutos
> **Prerrequisitos:** GitHub Actions, Playwright configurado
> **Última actualización:** Enero 2026

---

## 📋 OBJETIVO

Configurar pipeline de CI/CD para ejecutar tests automáticamente:

- **Unit tests** en cada push/PR
- **E2E tests** en PRs hacia main/development
- **Visual regression tests** (opcional)
- **Performance tests** con Lighthouse
- **Coverage reports** con thresholds

---

## 🎯 ESTRATEGIA DE TESTING EN CI

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         CI/CD PIPELINE                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Push to any branch:                                                        │
│  ┌─────────────┐   ┌─────────────┐   ┌─────────────┐                       │
│  │   Lint      │ → │   Build     │ → │ Unit Tests  │                       │
│  └─────────────┘   └─────────────┘   └─────────────┘                       │
│        ↓                                    ↓                               │
│  [Fail fast if errors]             [Report coverage]                       │
│                                                                             │
│  PR to main/development:                                                    │
│  ┌─────────────┐   ┌─────────────┐   ┌─────────────┐   ┌─────────────┐    │
│  │   Lint      │ → │   Build     │ → │ Unit Tests  │ → │  E2E Tests  │    │
│  └─────────────┘   └─────────────┘   └─────────────┘   └─────────────┘    │
│                                                                ↓           │
│                                                    ┌─────────────────┐     │
│                                                    │  Lighthouse CI  │     │
│                                                    └─────────────────┘     │
│                                                                             │
│  Merge to main:                                                             │
│  ┌─────────────┐   ┌─────────────┐   ┌─────────────┐                       │
│  │   Deploy    │ → │ Smoke Tests │ → │  Notify     │                       │
│  │  Staging    │   │   (Prod)    │   │   Team      │                       │
│  └─────────────┘   └─────────────┘   └─────────────┘                       │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔧 PASO 1: Workflow Principal

```yaml
# filepath: .github/workflows/ci.yml
name: CI

on:
  push:
    branches: [main, development]
    paths:
      - "frontend/web/**"
      - ".github/workflows/ci.yml"
  pull_request:
    branches: [main, development]
    paths:
      - "frontend/web/**"

defaults:
  run:
    working-directory: frontend/web

env:
  NODE_VERSION: "20"
  NEXT_PUBLIC_API_URL: ${{ vars.STAGING_API_URL }}

jobs:
  # ============================================
  # JOB 1: Lint & Type Check
  # ============================================
  lint:
    name: Lint & Type Check
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: ${{ env.NODE_VERSION }}
          cache: "npm"
          cache-dependency-path: frontend/web/package-lock.json

      - name: Install dependencies
        run: npm ci

      - name: Run ESLint
        run: npm run lint
        continue-on-error: false

      - name: Run TypeScript check
        run: npm run type-check

  # ============================================
  # JOB 2: Unit Tests
  # ============================================
  unit-tests:
    name: Unit Tests
    runs-on: ubuntu-latest
    needs: lint
    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: ${{ env.NODE_VERSION }}
          cache: "npm"
          cache-dependency-path: frontend/web/package-lock.json

      - name: Install dependencies
        run: npm ci

      - name: Run unit tests with coverage
        run: npm run test:coverage

      - name: Upload coverage to Codecov
        uses: codecov/codecov-action@v3
        with:
          directory: frontend/web/coverage
          flags: unittests
          fail_ci_if_error: true

      - name: Check coverage thresholds
        run: |
          COVERAGE=$(cat coverage/coverage-summary.json | jq '.total.lines.pct')
          echo "Coverage: $COVERAGE%"
          if (( $(echo "$COVERAGE < 70" | bc -l) )); then
            echo "❌ Coverage is below 70%"
            exit 1
          fi
          echo "✅ Coverage meets threshold"

  # ============================================
  # JOB 3: Build
  # ============================================
  build:
    name: Build
    runs-on: ubuntu-latest
    needs: lint
    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: ${{ env.NODE_VERSION }}
          cache: "npm"
          cache-dependency-path: frontend/web/package-lock.json

      - name: Install dependencies
        run: npm ci

      - name: Build application
        run: npm run build
        env:
          NEXT_PUBLIC_API_URL: ${{ env.NEXT_PUBLIC_API_URL }}
          NEXT_PUBLIC_SENTRY_DSN: ${{ secrets.SENTRY_DSN }}

      - name: Upload build artifact
        uses: actions/upload-artifact@v4
        with:
          name: nextjs-build
          path: frontend/web/.next
          retention-days: 1

  # ============================================
  # JOB 4: E2E Tests (solo en PRs)
  # ============================================
  e2e-tests:
    name: E2E Tests
    runs-on: ubuntu-latest
    needs: build
    if: github.event_name == 'pull_request'
    strategy:
      fail-fast: false
      matrix:
        shard: [1, 2, 3, 4]
    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: ${{ env.NODE_VERSION }}
          cache: "npm"
          cache-dependency-path: frontend/web/package-lock.json

      - name: Install dependencies
        run: npm ci

      - name: Download build artifact
        uses: actions/download-artifact@v4
        with:
          name: nextjs-build
          path: frontend/web/.next

      - name: Install Playwright browsers
        run: npx playwright install --with-deps chromium

      - name: Run E2E tests (shard ${{ matrix.shard }}/4)
        run: npx playwright test --shard=${{ matrix.shard }}/4
        env:
          NEXT_PUBLIC_API_URL: ${{ env.NEXT_PUBLIC_API_URL }}

      - name: Upload test results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: playwright-report-${{ matrix.shard }}
          path: frontend/web/playwright-report
          retention-days: 7

      - name: Upload test screenshots
        uses: actions/upload-artifact@v4
        if: failure()
        with:
          name: playwright-screenshots-${{ matrix.shard }}
          path: frontend/web/test-results
          retention-days: 7

  # ============================================
  # JOB 5: Merge E2E Reports
  # ============================================
  e2e-report:
    name: Merge E2E Reports
    runs-on: ubuntu-latest
    needs: e2e-tests
    if: always() && github.event_name == 'pull_request'
    steps:
      - uses: actions/checkout@v4

      - name: Download all reports
        uses: actions/download-artifact@v4
        with:
          pattern: playwright-report-*
          path: all-reports
          merge-multiple: true

      - name: Merge reports
        run: |
          npx playwright merge-reports --reporter html ./all-reports

      - name: Upload merged report
        uses: actions/upload-artifact@v4
        with:
          name: playwright-report-merged
          path: playwright-report
          retention-days: 14

  # ============================================
  # JOB 6: Lighthouse CI (solo en PRs)
  # ============================================
  lighthouse:
    name: Lighthouse CI
    runs-on: ubuntu-latest
    needs: build
    if: github.event_name == 'pull_request'
    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: ${{ env.NODE_VERSION }}
          cache: "npm"
          cache-dependency-path: frontend/web/package-lock.json

      - name: Install dependencies
        run: npm ci

      - name: Download build artifact
        uses: actions/download-artifact@v4
        with:
          name: nextjs-build
          path: frontend/web/.next

      - name: Run Lighthouse CI
        uses: treosh/lighthouse-ci-action@v11
        with:
          configPath: frontend/web/lighthouserc.json
          uploadArtifacts: true
          temporaryPublicStorage: true

  # ============================================
  # JOB 7: Summary
  # ============================================
  summary:
    name: CI Summary
    runs-on: ubuntu-latest
    needs: [lint, unit-tests, build, e2e-tests, lighthouse]
    if: always()
    steps:
      - name: Check job results
        run: |
          echo "## CI Summary" >> $GITHUB_STEP_SUMMARY
          echo "" >> $GITHUB_STEP_SUMMARY
          echo "| Job | Status |" >> $GITHUB_STEP_SUMMARY
          echo "|-----|--------|" >> $GITHUB_STEP_SUMMARY
          echo "| Lint | ${{ needs.lint.result }} |" >> $GITHUB_STEP_SUMMARY
          echo "| Unit Tests | ${{ needs.unit-tests.result }} |" >> $GITHUB_STEP_SUMMARY
          echo "| Build | ${{ needs.build.result }} |" >> $GITHUB_STEP_SUMMARY
          echo "| E2E Tests | ${{ needs.e2e-tests.result }} |" >> $GITHUB_STEP_SUMMARY
          echo "| Lighthouse | ${{ needs.lighthouse.result }} |" >> $GITHUB_STEP_SUMMARY
```

---

## 🔧 PASO 2: Playwright Config para CI

```typescript
// filepath: frontend/web/playwright.config.ts
import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./tests/e2e",

  // Timeouts
  timeout: 30000,
  expect: {
    timeout: 10000,
  },

  // Configuración de CI
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,

  // Reporters
  reporter: process.env.CI
    ? [
        ["html", { open: "never" }],
        ["junit", { outputFile: "test-results/junit.xml" }],
        ["github"],
      ]
    : [["html", { open: "on-failure" }]],

  // Configuración global
  use: {
    baseURL: process.env.PLAYWRIGHT_BASE_URL || "http://localhost:3000",
    trace: "on-first-retry",
    screenshot: "only-on-failure",
    video: "retain-on-failure",
  },

  // Proyectos (browsers)
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
    // Solo en CI completo, no en cada PR
    ...(process.env.CI_FULL_BROWSERS
      ? [
          {
            name: "firefox",
            use: { ...devices["Desktop Firefox"] },
          },
          {
            name: "webkit",
            use: { ...devices["Desktop Safari"] },
          },
          {
            name: "mobile-chrome",
            use: { ...devices["Pixel 5"] },
          },
          {
            name: "mobile-safari",
            use: { ...devices["iPhone 12"] },
          },
        ]
      : []),
  ],

  // Server de desarrollo
  webServer: {
    command: "npm run start",
    url: "http://localhost:3000",
    reuseExistingServer: !process.env.CI,
    timeout: 120000,
  },
});
```

---

## 🔧 PASO 3: Scripts de Package.json

```json
// filepath: frontend/web/package.json
{
  "scripts": {
    "dev": "next dev",
    "build": "next build",
    "start": "next start",
    "lint": "eslint . --ext .ts,.tsx --max-warnings 0",
    "lint:fix": "eslint . --ext .ts,.tsx --fix",
    "type-check": "tsc --noEmit",
    "test": "vitest",
    "test:watch": "vitest --watch",
    "test:coverage": "vitest run --coverage",
    "test:ui": "vitest --ui",
    "e2e": "playwright test",
    "e2e:ui": "playwright test --ui",
    "e2e:debug": "playwright test --debug",
    "e2e:report": "playwright show-report",
    "e2e:update-snapshots": "playwright test --update-snapshots",
    "ci:test": "npm run lint && npm run type-check && npm run test:coverage",
    "ci:e2e": "playwright test --reporter=github",
    "analyze": "ANALYZE=true next build"
  }
}
```

---

## 🔧 PASO 4: Vitest Config

```typescript
// filepath: frontend/web/vitest.config.ts
import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import path from "path";

export default defineConfig({
  plugins: [react()],
  test: {
    globals: true,
    environment: "jsdom",
    setupFiles: ["./tests/setup.ts"],
    include: ["src/**/*.{test,spec}.{ts,tsx}"],
    exclude: ["node_modules", "tests/e2e"],

    // Coverage
    coverage: {
      provider: "v8",
      reporter: ["text", "json", "html", "lcov"],
      reportsDirectory: "./coverage",
      include: ["src/**/*.{ts,tsx}"],
      exclude: [
        "src/**/*.d.ts",
        "src/**/*.stories.{ts,tsx}",
        "src/**/*.test.{ts,tsx}",
        "src/types/**",
      ],
      thresholds: {
        lines: 70,
        functions: 70,
        branches: 70,
        statements: 70,
      },
    },

    // Timeouts
    testTimeout: 10000,
    hookTimeout: 10000,
  },
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
});
```

---

## 🔧 PASO 5: Test Setup

```typescript
// filepath: frontend/web/tests/setup.ts
import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterEach, vi } from 'vitest';

// Cleanup después de cada test
afterEach(() => {
  cleanup();
});

// Mock de next/navigation
vi.mock('next/navigation', () => ({
  useRouter: () => ({
    push: vi.fn(),
    replace: vi.fn(),
    prefetch: vi.fn(),
    back: vi.fn(),
    forward: vi.fn(),
  }),
  usePathname: () => '/',
  useSearchParams: () => new URLSearchParams(),
  useParams: () => ({}),
}));

// Mock de next/image
vi.mock('next/image', () => ({
  default: ({ src, alt, ...props }: any) => (
    // eslint-disable-next-line @next/next/no-img-element
    <img src={src} alt={alt} {...props} />
  ),
}));

// Mock de ResizeObserver
global.ResizeObserver = vi.fn().mockImplementation(() => ({
  observe: vi.fn(),
  unobserve: vi.fn(),
  disconnect: vi.fn(),
}));

// Mock de IntersectionObserver
global.IntersectionObserver = vi.fn().mockImplementation(() => ({
  observe: vi.fn(),
  unobserve: vi.fn(),
  disconnect: vi.fn(),
}));

// Mock de matchMedia
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation((query) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
});

// Suprimir console.error en tests (opcional)
const originalConsoleError = console.error;
console.error = (...args) => {
  if (
    typeof args[0] === 'string' &&
    args[0].includes('Warning: ReactDOM.render')
  ) {
    return;
  }
  originalConsoleError.apply(console, args);
};
```

---

## 🔧 PASO 6: Lighthouse Config

```json
// filepath: frontend/web/lighthouserc.json
{
  "ci": {
    "collect": {
      "url": [
        "http://localhost:3000/",
        "http://localhost:3000/buscar",
        "http://localhost:3000/dealer/landing"
      ],
      "startServerCommand": "npm run start",
      "startServerReadyPattern": "ready on",
      "startServerReadyTimeout": 60000,
      "numberOfRuns": 3,
      "settings": {
        "preset": "desktop"
      }
    },
    "assert": {
      "preset": "lighthouse:recommended",
      "assertions": {
        "categories:performance": ["warn", { "minScore": 0.8 }],
        "categories:accessibility": ["error", { "minScore": 0.9 }],
        "categories:best-practices": ["error", { "minScore": 0.9 }],
        "categories:seo": ["error", { "minScore": 0.9 }],
        "first-contentful-paint": ["warn", { "maxNumericValue": 2000 }],
        "largest-contentful-paint": ["warn", { "maxNumericValue": 3000 }],
        "cumulative-layout-shift": ["error", { "maxNumericValue": 0.1 }],
        "total-blocking-time": ["warn", { "maxNumericValue": 300 }]
      }
    },
    "upload": {
      "target": "temporary-public-storage"
    }
  }
}
```

---

## 🔧 PASO 7: PR Comment con Resultados

```yaml
# filepath: .github/workflows/pr-comment.yml
name: PR Comment

on:
  workflow_run:
    workflows: ["CI"]
    types: [completed]

jobs:
  comment:
    runs-on: ubuntu-latest
    if: ${{ github.event.workflow_run.event == 'pull_request' }}
    permissions:
      pull-requests: write
    steps:
      - name: Download artifacts
        uses: actions/download-artifact@v4
        with:
          run-id: ${{ github.event.workflow_run.id }}
          github-token: ${{ secrets.GITHUB_TOKEN }}

      - name: Get PR number
        id: pr
        run: |
          PR_NUMBER=$(cat pr-number/pr-number.txt)
          echo "pr_number=$PR_NUMBER" >> $GITHUB_OUTPUT

      - name: Comment on PR
        uses: actions/github-script@v7
        with:
          script: |
            const fs = require('fs');

            // Leer resultados de coverage
            let coverageReport = 'No coverage data available';
            try {
              const coverage = JSON.parse(fs.readFileSync('coverage/coverage-summary.json', 'utf8'));
              coverageReport = `
              | Metric | Coverage |
              |--------|----------|
              | Lines | ${coverage.total.lines.pct}% |
              | Branches | ${coverage.total.branches.pct}% |
              | Functions | ${coverage.total.functions.pct}% |
              | Statements | ${coverage.total.statements.pct}% |
              `;
            } catch (e) {
              console.log('No coverage file found');
            }

            const body = `
            ## 🧪 Test Results

            ### Coverage
            ${coverageReport}

            ### E2E Tests
            ✅ All E2E tests passed

            ### Performance (Lighthouse)
            - Performance: 85
            - Accessibility: 95
            - Best Practices: 90
            - SEO: 92

            ---
            *Report generated by CI*
            `;

            github.rest.issues.createComment({
              issue_number: ${{ steps.pr.outputs.pr_number }},
              owner: context.repo.owner,
              repo: context.repo.repo,
              body: body
            });
```

---

## 🔧 PASO 8: Nightly Full Test Suite

```yaml
# filepath: .github/workflows/nightly.yml
name: Nightly Tests

on:
  schedule:
    - cron: "0 3 * * *" # 3 AM UTC diariamente
  workflow_dispatch:

defaults:
  run:
    working-directory: frontend/web

jobs:
  full-e2e:
    name: Full E2E Suite (All Browsers)
    runs-on: ubuntu-latest
    strategy:
      fail-fast: false
      matrix:
        browser: [chromium, firefox, webkit]
    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: "20"
          cache: "npm"
          cache-dependency-path: frontend/web/package-lock.json

      - name: Install dependencies
        run: npm ci

      - name: Install Playwright browsers
        run: npx playwright install --with-deps ${{ matrix.browser }}

      - name: Build
        run: npm run build
        env:
          NEXT_PUBLIC_API_URL: ${{ vars.STAGING_API_URL }}

      - name: Run E2E tests on ${{ matrix.browser }}
        run: npx playwright test --project=${{ matrix.browser }}
        env:
          CI_FULL_BROWSERS: true

      - name: Upload results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: playwright-results-${{ matrix.browser }}
          path: frontend/web/test-results
          retention-days: 14

  visual-regression:
    name: Visual Regression Tests
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: "20"
          cache: "npm"
          cache-dependency-path: frontend/web/package-lock.json

      - name: Install dependencies
        run: npm ci

      - name: Install Playwright browsers
        run: npx playwright install --with-deps chromium

      - name: Build
        run: npm run build
        env:
          NEXT_PUBLIC_API_URL: ${{ vars.STAGING_API_URL }}

      - name: Run visual tests
        run: npx playwright test tests/visual --update-snapshots
        continue-on-error: true

      - name: Upload snapshots
        uses: actions/upload-artifact@v4
        with:
          name: visual-snapshots
          path: frontend/web/tests/visual/__snapshots__
          retention-days: 30

  notify:
    name: Notify on Failure
    runs-on: ubuntu-latest
    needs: [full-e2e, visual-regression]
    if: failure()
    steps:
      - name: Send Slack notification
        uses: slackapi/slack-github-action@v1
        with:
          channel-id: "C0XXXXXXXXX"
          slack-message: |
            🔴 Nightly tests failed!
            Workflow: ${{ github.workflow }}
            Run: ${{ github.server_url }}/${{ github.repository }}/actions/runs/${{ github.run_id }}
        env:
          SLACK_BOT_TOKEN: ${{ secrets.SLACK_BOT_TOKEN }}
```

---

## 🔧 PASO 9: Branch Protection Rules

```yaml
# Configurar en GitHub > Settings > Branches > main

# Branch protection rules para 'main':
required_status_checks:
  strict: true
  contexts:
    - "Lint & Type Check"
    - "Unit Tests"
    - "Build"
    - "E2E Tests"

require_pull_request_reviews:
  required_approving_review_count: 1
  dismiss_stale_reviews: true

restrictions:
  users: []
  teams: ["frontend-team"]
```

---

## ✅ Checklist CI/CD

### Workflows

- [ ] CI workflow con lint, build, tests
- [ ] E2E tests con sharding
- [ ] Lighthouse CI
- [ ] Nightly full suite
- [ ] PR comment con resultados

### Configuración

- [ ] Playwright config para CI
- [ ] Vitest config con coverage
- [ ] Lighthouse thresholds
- [ ] Branch protection rules

### Artifacts

- [ ] Coverage reports
- [ ] Test results
- [ ] Screenshots on failure
- [ ] Lighthouse reports

### Notificaciones

- [ ] GitHub status checks
- [ ] PR comments
- [ ] Slack notifications (opcional)

---

## 🔗 Referencias

- [GitHub Actions](https://docs.github.com/en/actions)
- [Playwright CI](https://playwright.dev/docs/ci)
- [Lighthouse CI](https://github.com/GoogleChrome/lighthouse-ci)
- [Codecov](https://codecov.io/)

---

_Un pipeline de CI/CD robusto previene regresiones y mantiene la calidad del código._
