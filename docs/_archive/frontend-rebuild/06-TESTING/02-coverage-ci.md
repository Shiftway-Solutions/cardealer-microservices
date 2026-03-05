# 📊 Coverage y CI/CD

> **Tiempo estimado:** 30 minutos
> **Prerrequisitos:** Vitest configurado, GitHub Actions

---

## 📋 OBJETIVO

Configurar:

- Coverage thresholds (80%+)
- GitHub Actions CI
- PR checks automáticos
- Coverage badges

---

## 🔧 PASO 1: Vitest Coverage Config

```typescript
// filepath: vitest.config.ts
import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import tsconfigPaths from "vite-tsconfig-paths";

export default defineConfig({
  plugins: [react(), tsconfigPaths()],
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: ["./src/test/setup.ts"],
    include: ["src/**/*.{test,spec}.{ts,tsx}"],
    exclude: ["node_modules", "e2e"],
    coverage: {
      provider: "v8",
      reporter: ["text", "json", "html", "lcov"],
      reportsDirectory: "./coverage",
      include: ["src/**/*.{ts,tsx}"],
      exclude: [
        "src/**/*.d.ts",
        "src/**/*.test.{ts,tsx}",
        "src/**/*.spec.{ts,tsx}",
        "src/test/**",
        "src/types/**",
        "src/mocks/**",
      ],
      thresholds: {
        statements: 80,
        branches: 75,
        functions: 80,
        lines: 80,
      },
    },
  },
});
```

---

## 🔧 PASO 2: Package.json Scripts

```json
{
  "scripts": {
    "test": "vitest",
    "test:run": "vitest run",
    "test:ui": "vitest --ui",
    "test:coverage": "vitest run --coverage",
    "test:ci": "vitest run --coverage --reporter=json",
    "e2e": "playwright test",
    "e2e:ui": "playwright test --ui",
    "lint": "eslint . --ext .ts,.tsx --max-warnings 0",
    "lint:fix": "eslint . --ext .ts,.tsx --fix",
    "typecheck": "tsc --noEmit",
    "validate": "pnpm lint && pnpm typecheck && pnpm test:run"
  }
}
```

---

## 🔧 PASO 3: GitHub Actions CI

```yaml
# filepath: .github/workflows/ci.yml
name: CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]

concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

jobs:
  lint:
    name: Lint & Type Check
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: pnpm/action-setup@v2
        with:
          version: 9

      - uses: actions/setup-node@v4
        with:
          node-version: 20
          cache: "pnpm"

      - run: pnpm install --frozen-lockfile

      - name: Lint
        run: pnpm lint

      - name: Type Check
        run: pnpm typecheck

  test:
    name: Unit Tests
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: pnpm/action-setup@v2
        with:
          version: 9

      - uses: actions/setup-node@v4
        with:
          node-version: 20
          cache: "pnpm"

      - run: pnpm install --frozen-lockfile

      - name: Run Tests
        run: pnpm test:coverage

      - name: Upload Coverage
        uses: codecov/codecov-action@v4
        with:
          token: ${{ secrets.CODECOV_TOKEN }}
          files: ./coverage/lcov.info
          fail_ci_if_error: false

  e2e:
    name: E2E Tests
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: pnpm/action-setup@v2
        with:
          version: 9

      - uses: actions/setup-node@v4
        with:
          node-version: 20
          cache: "pnpm"

      - run: pnpm install --frozen-lockfile

      - name: Install Playwright
        run: pnpm exec playwright install --with-deps chromium

      - name: Build
        run: pnpm build

      - name: Run E2E Tests
        run: pnpm e2e

      - uses: actions/upload-artifact@v4
        if: failure()
        with:
          name: playwright-report
          path: playwright-report/
          retention-days: 7

  build:
    name: Build
    runs-on: ubuntu-latest
    needs: [lint, test]
    steps:
      - uses: actions/checkout@v4

      - uses: pnpm/action-setup@v2
        with:
          version: 9

      - uses: actions/setup-node@v4
        with:
          node-version: 20
          cache: "pnpm"

      - run: pnpm install --frozen-lockfile

      - name: Build
        run: pnpm build

      - name: Check Bundle Size
        run: |
          SIZE=$(du -sk .next/static | cut -f1)
          if [ $SIZE -gt 512 ]; then
            echo "Bundle size ($SIZE KB) exceeds limit (512 KB)"
            exit 1
          fi
```

---

## 🔧 PASO 4: PR Template

```markdown
<!-- filepath: .github/pull_request_template.md -->

## Descripción

<!-- Describe los cambios -->

## Tipo de cambio

- [ ] Bug fix
- [ ] Nueva funcionalidad
- [ ] Breaking change
- [ ] Documentación

## Checklist

- [ ] Tests agregados/actualizados
- [ ] Lint pasa sin warnings
- [ ] TypeScript compila sin errores
- [ ] Probado localmente
- [ ] Coverage >= 80%

## Screenshots (si aplica)

<!-- Agrega screenshots -->
```

---

## 🔧 PASO 5: Pre-commit Hooks

```bash
# Instalar husky
pnpm add -D husky lint-staged
pnpm exec husky init
```

```json
// filepath: package.json (agregar)
{
  "lint-staged": {
    "*.{ts,tsx}": ["eslint --fix", "prettier --write"],
    "*.{json,md,yml}": ["prettier --write"]
  }
}
```

```bash
# filepath: .husky/pre-commit
pnpm lint-staged
pnpm typecheck
```

```bash
# filepath: .husky/pre-push
pnpm test:run
```

---

## 🔧 PASO 6: Coverage Badge

```markdown
<!-- filepath: README.md -->

# OKLA Frontend

[![CI](https://github.com/org/okla-frontend/actions/workflows/ci.yml/badge.svg)](https://github.com/org/okla-frontend/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/org/okla-frontend/branch/main/graph/badge.svg)](https://codecov.io/gh/org/okla-frontend)

## Scripts

| Script               | Descripción            |
| -------------------- | ---------------------- |
| `pnpm dev`           | Servidor de desarrollo |
| `pnpm build`         | Build de producción    |
| `pnpm test`          | Tests en modo watch    |
| `pnpm test:coverage` | Tests con coverage     |
| `pnpm e2e`           | Tests E2E              |
| `pnpm validate`      | Lint + Types + Tests   |

## Coverage Goals

- Statements: 80%+
- Branches: 75%+
- Functions: 80%+
- Lines: 80%+
```

---

## ✅ VALIDACIÓN

```bash
# Local
pnpm validate
pnpm test:coverage

# Ver reporte HTML
open coverage/index.html

# Verificar hooks
git commit -m "test commit"
# Debe ejecutar lint-staged y typecheck
```

---

## 📊 RESUMEN FINAL

La documentación está completa:

| Carpeta             | Archivos | Estado |
| ------------------- | -------- | ------ |
| 01-SETUP            | 5        | ✅     |
| 02-UX-DESIGN-SYSTEM | 6        | ✅     |
| 03-COMPONENTES      | 5        | ✅     |
| 04-PAGINAS          | 5        | ✅     |
| 05-API-INTEGRATION  | 4        | ✅     |
| 06-TESTING          | 2        | ✅     |
| 07-BACKEND-SUPPORT  | 1        | ✅     |

**Total: 28 documentos**
