# 🚀 Paso 1: Crear Proyecto Next.js

> **Tiempo estimado:** 30 minutos
> **Ejecutor:** Dev 1 o Dev 2
> **Prerrequisitos:** Node.js 20+, pnpm instalado

---

## 📋 PRERREQUISITOS

### Verificar instalaciones

```bash
# Verificar Node.js (debe ser >= 20)
node --version
# Output esperado: v20.x.x o superior

# Verificar pnpm (si no está instalado, instalarlo)
pnpm --version
# Output esperado: 8.x.x o superior

# Si pnpm no está instalado:
npm install -g pnpm
```

### Verificar directorio

```bash
# Navegar al directorio del proyecto
cd /Users/gregorymoreno/Developer/Web/Backend/cardealer-microservices/frontend

# Verificar que estás en el lugar correcto
pwd
# Output esperado: .../cardealer-microservices/frontend
```

---

## 🔧 PASO 1.1: Crear proyecto Next.js

### Comando

```bash
# Crear proyecto con todas las opciones preconfiguradas
pnpm create next-app@latest web-next \
  --typescript \
  --tailwind \
  --eslint \
  --app \
  --src-dir \
  --import-alias "@/*" \
  --use-pnpm
```

### Output esperado

```
Creating a new Next.js app in /path/to/frontend/web-next.

Using pnpm.

Initializing project with template: app-tw

Installing dependencies:
- react
- react-dom
- next

Installing devDependencies:
- typescript
- @types/node
- @types/react
- @types/react-dom
- postcss
- tailwindcss
- eslint
- eslint-config-next

Success! Created web-next at /path/to/frontend/web-next
```

### Validación

```bash
# Entrar al directorio
cd web-next

# Verificar estructura
ls -la
# Debe mostrar: src/, public/, package.json, tsconfig.json, etc.

# Verificar que compila
pnpm build

# Output esperado al final:
# ✓ Compiled successfully
# ✓ Linting and checking validity of types
# ✓ Collecting page data
# ✓ Generating static pages
```

---

## 🔧 PASO 1.2: Instalar dependencias core

### Comando

```bash
# Dependencias de producción
pnpm add \
  @tanstack/react-query \
  zustand \
  react-hook-form \
  @hookform/resolvers \
  zod \
  axios \
  lucide-react \
  framer-motion \
  clsx \
  tailwind-merge \
  date-fns \
  sonner \
  @radix-ui/react-slot
```

### Output esperado

```
Packages: +XX
+++++++++++++++++++++++
Progress: resolved XXX, reused XXX, downloaded X, added XX, done
```

### Validación

```bash
# Verificar que se instalaron
cat package.json | grep -A 20 '"dependencies"'
```

---

## 🔧 PASO 1.3: Instalar dependencias de desarrollo

### Comando

```bash
# Dependencias de desarrollo
pnpm add -D \
  vitest \
  @vitest/coverage-v8 \
  @vitest/ui \
  @testing-library/react \
  @testing-library/jest-dom \
  @testing-library/user-event \
  jsdom \
  @playwright/test \
  msw \
  prettier \
  prettier-plugin-tailwindcss \
  husky \
  lint-staged \
  @commitlint/cli \
  @commitlint/config-conventional
```

### Output esperado

```
Packages: +XX
+++++++++++++++++++++++
Progress: resolved XXX, reused XXX, downloaded X, added XX, done
```

---

## 🔧 PASO 1.4: Configurar scripts en package.json

### Código a modificar

Abrir `package.json` y reemplazar la sección `"scripts"` con:

```json
{
  "name": "web-next",
  "version": "0.1.0",
  "private": true,
  "scripts": {
    "dev": "next dev --turbo",
    "build": "next build",
    "start": "next start",
    "lint": "next lint",
    "lint:fix": "next lint --fix",
    "format": "prettier --write .",
    "format:check": "prettier --check .",
    "test": "vitest",
    "test:ui": "vitest --ui",
    "test:coverage": "vitest run --coverage",
    "test:e2e": "playwright test",
    "test:e2e:ui": "playwright test --ui",
    "test:e2e:headed": "playwright test --headed",
    "prepare": "husky",
    "storybook": "storybook dev -p 6006",
    "build-storybook": "storybook build"
  },
  "dependencies": {
    ...
  }
}
```

### Validación

```bash
# Probar script de desarrollo
pnpm dev

# Debería iniciar en http://localhost:3000
# Presionar Ctrl+C para detener
```

---

## 🔧 PASO 1.5: Crear estructura de carpetas

### Comando

```bash
# Crear estructura completa
mkdir -p src/{app,components,lib,types,styles}
mkdir -p src/app/\(auth\)
mkdir -p src/app/\(public\)
mkdir -p src/app/\(protected\)
mkdir -p src/app/dealer
mkdir -p src/app/admin
mkdir -p src/app/billing
mkdir -p src/app/api
mkdir -p src/components/{ui,layout,vehicles,search,dealers,forms,shared}
mkdir -p src/lib/{api,hooks,store,utils,validations}
mkdir -p src/types/{api,entities,forms}
mkdir -p __tests__/{components,hooks,lib,e2e}
mkdir -p public/{images,icons,fonts}
```

### Validación

```bash
# Ver estructura creada
find src -type d | head -30

# Output esperado:
# src
# src/app
# src/app/(auth)
# src/app/(public)
# src/app/(protected)
# src/app/dealer
# src/app/admin
# src/app/billing
# src/app/api
# src/components
# src/components/ui
# src/components/layout
# ...
```

---

## 🔧 PASO 1.6: Crear archivo de utilidades base

### Código a crear

```typescript
// filepath: src/lib/utils.ts
import { type ClassValue, clsx } from "clsx";
import { twMerge } from "tailwind-merge";

/**
 * Combina clases de Tailwind de forma inteligente
 * Usa clsx para condicionales y twMerge para resolver conflictos
 */
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

/**
 * Formatea un número como moneda dominicana
 */
export function formatCurrency(amount: number): string {
  return new Intl.NumberFormat("es-DO", {
    style: "currency",
    currency: "DOP",
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(amount);
}

/**
 * Formatea un número con separadores de miles
 */
export function formatNumber(num: number): string {
  return new Intl.NumberFormat("es-DO").format(num);
}

/**
 * Formatea una fecha en español
 */
export function formatDate(date: Date | string): string {
  const d = typeof date === "string" ? new Date(date) : date;
  return new Intl.DateTimeFormat("es-DO", {
    year: "numeric",
    month: "long",
    day: "numeric",
  }).format(d);
}

/**
 * Formatea fecha relativa (hace X tiempo)
 */
export function formatRelativeDate(date: Date | string): string {
  const d = typeof date === "string" ? new Date(date) : date;
  const now = new Date();
  const diffInSeconds = Math.floor((now.getTime() - d.getTime()) / 1000);

  if (diffInSeconds < 60) return "hace un momento";
  if (diffInSeconds < 3600)
    return `hace ${Math.floor(diffInSeconds / 60)} minutos`;
  if (diffInSeconds < 86400)
    return `hace ${Math.floor(diffInSeconds / 3600)} horas`;
  if (diffInSeconds < 604800)
    return `hace ${Math.floor(diffInSeconds / 86400)} días`;

  return formatDate(d);
}

/**
 * Genera un slug a partir de un texto
 */
export function slugify(text: string): string {
  return text
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/(^-|-$)/g, "");
}

/**
 * Trunca texto a un máximo de caracteres
 */
export function truncate(text: string, maxLength: number): string {
  if (text.length <= maxLength) return text;
  return text.slice(0, maxLength - 3) + "...";
}

/**
 * Delay para async/await
 */
export function delay(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

/**
 * Debounce function
 */
export function debounce<T extends (...args: unknown[]) => unknown>(
  fn: T,
  ms: number,
): (...args: Parameters<T>) => void {
  let timeoutId: ReturnType<typeof setTimeout>;
  return (...args: Parameters<T>) => {
    clearTimeout(timeoutId);
    timeoutId = setTimeout(() => fn(...args), ms);
  };
}
```

### Validación

```bash
# Verificar que TypeScript compila
pnpm tsc --noEmit

# No debería haber errores
```

---

## 🔧 PASO 1.7: Crear test inicial para utils

### Código a crear

```typescript
// filepath: __tests__/lib/utils.test.ts
import { describe, it, expect } from "vitest";
import {
  cn,
  formatCurrency,
  formatNumber,
  slugify,
  truncate,
} from "@/lib/utils";

describe("cn (classnames)", () => {
  it("combines class names", () => {
    expect(cn("foo", "bar")).toBe("foo bar");
  });

  it("handles conditional classes", () => {
    expect(cn("foo", false && "bar", "baz")).toBe("foo baz");
  });

  it("merges tailwind classes correctly", () => {
    expect(cn("px-2 py-1", "px-4")).toBe("py-1 px-4");
  });
});

describe("formatCurrency", () => {
  it("formats Dominican Pesos correctly", () => {
    expect(formatCurrency(1850000)).toBe("RD$1,850,000");
  });

  it("handles zero", () => {
    expect(formatCurrency(0)).toBe("RD$0");
  });

  it("handles negative numbers", () => {
    expect(formatCurrency(-500)).toBe("-RD$500");
  });
});

describe("formatNumber", () => {
  it("adds thousand separators", () => {
    expect(formatNumber(1234567)).toBe("1,234,567");
  });
});

describe("slugify", () => {
  it("converts text to slug", () => {
    expect(slugify("Toyota Camry 2024")).toBe("toyota-camry-2024");
  });

  it("handles special characters", () => {
    expect(slugify("Año Nuevo 2026!")).toBe("ano-nuevo-2026");
  });

  it("handles accents", () => {
    expect(slugify("Camión Grande")).toBe("camion-grande");
  });
});

describe("truncate", () => {
  it("truncates long text", () => {
    expect(truncate("Hello World", 8)).toBe("Hello...");
  });

  it("does not truncate short text", () => {
    expect(truncate("Hello", 10)).toBe("Hello");
  });
});
```

---

## 🔧 PASO 1.8: Configurar Vitest

### Código a crear

```typescript
// filepath: vitest.config.ts
import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import path from "path";

export default defineConfig({
  plugins: [react()],
  test: {
    globals: true,
    environment: "jsdom",
    setupFiles: ["./__tests__/setup.ts"],
    include: ["**/*.{test,spec}.{ts,tsx}"],
    exclude: ["node_modules", ".next", "e2e"],
    coverage: {
      provider: "v8",
      reporter: ["text", "json", "html", "lcov"],
      exclude: [
        "node_modules/",
        ".next/",
        "__tests__/",
        "**/*.d.ts",
        "**/*.config.*",
        "**/types/",
      ],
      thresholds: {
        global: {
          branches: 70,
          functions: 80,
          lines: 80,
          statements: 80,
        },
      },
    },
  },
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
});
```

### Código adicional

```typescript
// filepath: __tests__/setup.ts
import "@testing-library/jest-dom/vitest";
import { cleanup } from "@testing-library/react";
import { afterEach, vi } from "vitest";

// Cleanup after each test
afterEach(() => {
  cleanup();
});

// Mock next/navigation
vi.mock("next/navigation", () => ({
  useRouter: () => ({
    push: vi.fn(),
    replace: vi.fn(),
    prefetch: vi.fn(),
    back: vi.fn(),
  }),
  usePathname: () => "/",
  useSearchParams: () => new URLSearchParams(),
}));

// Mock next/image
vi.mock("next/image", () => ({
  default: ({ src, alt, ...props }: { src: string; alt: string }) => (
    // eslint-disable-next-line @next/next/no-img-element
    <img src={src} alt={alt} {...props} />
  ),
}));
```

---

## 🔧 PASO 1.9: Ejecutar primer test

### Comando

```bash
# Ejecutar tests
pnpm test

# Output esperado:
# ✓ __tests__/lib/utils.test.ts (5)
#   ✓ cn (classnames) (3)
#   ✓ formatCurrency (3)
#   ✓ formatNumber (1)
#   ✓ slugify (3)
#   ✓ truncate (2)
#
# Test Files  1 passed (1)
# Tests  12 passed (12)
```

### Si hay errores

1. **Error: Cannot find module '@/lib/utils'**
   - Verificar que `vitest.config.ts` tiene el alias configurado
   - Verificar que `tsconfig.json` tiene `"paths": { "@/*": ["./src/*"] }`

2. **Error: @testing-library/jest-dom not found**
   - Ejecutar `pnpm add -D @testing-library/jest-dom`

---

## ✅ VALIDACIÓN FINAL

```bash
# 1. Build debe pasar
pnpm build
# ✓ Expected: Exit code 0

# 2. Tests deben pasar
pnpm test run
# ✓ Expected: All tests pass

# 3. Lint debe pasar
pnpm lint
# ✓ Expected: No errors

# 4. Dev server debe iniciar
pnpm dev
# ✓ Expected: http://localhost:3000 funciona
```

---

## 📊 RESUMEN

| Tarea                   | Estado |
| ----------------------- | ------ |
| Proyecto Next.js creado | ✅     |
| Dependencias instaladas | ✅     |
| Scripts configurados    | ✅     |
| Estructura de carpetas  | ✅     |
| Utilidades base         | ✅     |
| Vitest configurado      | ✅     |
| Primer test pasando     | ✅     |

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/01-SETUP/02-configurar-typescript.md`
