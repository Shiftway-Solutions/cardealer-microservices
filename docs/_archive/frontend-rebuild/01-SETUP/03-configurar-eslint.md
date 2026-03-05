# 🔍 Configurar ESLint y Prettier

> **Tiempo estimado:** 20 minutos
> **Prerrequisitos:** Proyecto Next.js creado

---

## 📋 OBJETIVO

Configurar linting y formateo automático:

- ESLint con reglas estrictas
- Prettier para formateo consistente
- Integración con VS Code
- Husky para pre-commit hooks

---

## 🔧 PASO 1: Instalar Dependencias

```bash
# ESLint plugins adicionales
pnpm add -D \
  eslint-plugin-import \
  eslint-plugin-jsx-a11y \
  eslint-plugin-react-hooks \
  @typescript-eslint/parser \
  @typescript-eslint/eslint-plugin

# Prettier
pnpm add -D \
  prettier \
  eslint-config-prettier \
  eslint-plugin-prettier \
  prettier-plugin-tailwindcss
```

---

## 🔧 PASO 2: Configurar ESLint

```javascript
// filepath: eslint.config.mjs
import { dirname } from "path";
import { fileURLToPath } from "url";
import { FlatCompat } from "@eslint/eslintrc";
import js from "@eslint/js";
import typescriptPlugin from "@typescript-eslint/eslint-plugin";
import typescriptParser from "@typescript-eslint/parser";
import importPlugin from "eslint-plugin-import";
import jsxA11yPlugin from "eslint-plugin-jsx-a11y";
import reactHooksPlugin from "eslint-plugin-react-hooks";
import prettierPlugin from "eslint-plugin-prettier";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const compat = new FlatCompat({
  baseDirectory: __dirname,
});

export default [
  js.configs.recommended,
  ...compat.extends("next/core-web-vitals"),
  {
    files: ["**/*.{ts,tsx}"],
    languageOptions: {
      parser: typescriptParser,
      parserOptions: {
        ecmaVersion: "latest",
        sourceType: "module",
        ecmaFeatures: {
          jsx: true,
        },
        project: "./tsconfig.json",
      },
    },
    plugins: {
      "@typescript-eslint": typescriptPlugin,
      import: importPlugin,
      "jsx-a11y": jsxA11yPlugin,
      "react-hooks": reactHooksPlugin,
      prettier: prettierPlugin,
    },
    rules: {
      // ========================
      // TypeScript
      // ========================
      "@typescript-eslint/no-unused-vars": [
        "error",
        {
          argsIgnorePattern: "^_",
          varsIgnorePattern: "^_",
          caughtErrorsIgnorePattern: "^_",
        },
      ],
      "@typescript-eslint/no-explicit-any": "warn",
      "@typescript-eslint/explicit-function-return-type": "off",
      "@typescript-eslint/explicit-module-boundary-types": "off",
      "@typescript-eslint/no-non-null-assertion": "warn",
      "@typescript-eslint/prefer-nullish-coalescing": "warn",
      "@typescript-eslint/prefer-optional-chain": "error",
      "@typescript-eslint/no-floating-promises": "error",
      "@typescript-eslint/await-thenable": "error",
      "@typescript-eslint/no-misused-promises": [
        "error",
        {
          checksVoidReturn: false,
        },
      ],

      // ========================
      // React
      // ========================
      "react/react-in-jsx-scope": "off",
      "react/prop-types": "off",
      "react/display-name": "off",
      "react/no-unescaped-entities": "off",
      "react-hooks/rules-of-hooks": "error",
      "react-hooks/exhaustive-deps": "warn",

      // ========================
      // Import
      // ========================
      "import/order": [
        "error",
        {
          groups: [
            "builtin",
            "external",
            "internal",
            "parent",
            "sibling",
            "index",
            "type",
          ],
          "newlines-between": "never",
          alphabetize: {
            order: "asc",
            caseInsensitive: true,
          },
          pathGroups: [
            {
              pattern: "react",
              group: "builtin",
              position: "before",
            },
            {
              pattern: "next/**",
              group: "builtin",
              position: "before",
            },
            {
              pattern: "@/**",
              group: "internal",
              position: "before",
            },
          ],
          pathGroupsExcludedImportTypes: ["react", "next"],
        },
      ],
      "import/no-duplicates": "error",
      "import/no-unresolved": "off", // TypeScript handles this
      "import/no-cycle": "warn",
      "import/no-self-import": "error",

      // ========================
      // Accessibility
      // ========================
      "jsx-a11y/alt-text": "error",
      "jsx-a11y/anchor-has-content": "error",
      "jsx-a11y/anchor-is-valid": [
        "error",
        {
          components: ["Link"],
          specialLink: ["hrefLeft", "hrefRight"],
          aspects: ["invalidHref", "preferButton"],
        },
      ],
      "jsx-a11y/aria-props": "error",
      "jsx-a11y/aria-proptypes": "error",
      "jsx-a11y/aria-unsupported-elements": "error",
      "jsx-a11y/click-events-have-key-events": "warn",
      "jsx-a11y/heading-has-content": "error",
      "jsx-a11y/html-has-lang": "error",
      "jsx-a11y/img-redundant-alt": "error",
      "jsx-a11y/interactive-supports-focus": "warn",
      "jsx-a11y/label-has-associated-control": "error",
      "jsx-a11y/no-autofocus": "warn",
      "jsx-a11y/no-noninteractive-element-interactions": "warn",
      "jsx-a11y/no-redundant-roles": "error",
      "jsx-a11y/role-has-required-aria-props": "error",
      "jsx-a11y/role-supports-aria-props": "error",

      // ========================
      // General
      // ========================
      "no-console": ["warn", { allow: ["warn", "error"] }],
      "no-debugger": "error",
      "no-alert": "error",
      "prefer-const": "error",
      "no-var": "error",
      eqeqeq: ["error", "always", { null: "ignore" }],
      curly: ["error", "all"],

      // ========================
      // Prettier
      // ========================
      "prettier/prettier": [
        "error",
        {},
        {
          usePrettierrc: true,
        },
      ],
    },
    settings: {
      react: {
        version: "detect",
      },
      "import/resolver": {
        typescript: {
          alwaysTryTypes: true,
        },
      },
    },
  },
  {
    // Ignore patterns
    ignores: [
      "node_modules/",
      ".next/",
      "out/",
      "build/",
      "dist/",
      "coverage/",
      "*.config.js",
      "*.config.mjs",
      "public/",
    ],
  },
];
```

---

## 🔧 PASO 3: Configurar Prettier

```json
// filepath: .prettierrc
{
  "semi": true,
  "singleQuote": false,
  "tabWidth": 2,
  "useTabs": false,
  "trailingComma": "es5",
  "printWidth": 80,
  "bracketSpacing": true,
  "bracketSameLine": false,
  "arrowParens": "always",
  "endOfLine": "lf",
  "plugins": ["prettier-plugin-tailwindcss"],
  "tailwindConfig": "./tailwind.config.ts",
  "tailwindFunctions": ["cn", "cva", "clsx"]
}
```

```text
// filepath: .prettierignore
# Dependencies
node_modules/

# Build outputs
.next/
out/
build/
dist/

# Coverage
coverage/

# Cache
.turbo/
.eslintcache
.prettiercache

# Generated
*.generated.*
pnpm-lock.yaml

# Config files
*.config.js
*.config.mjs
next-env.d.ts
```

---

## 🔧 PASO 4: Configurar VS Code

```json
// filepath: .vscode/settings.json
{
  // Editor
  "editor.formatOnSave": true,
  "editor.defaultFormatter": "esbenp.prettier-vscode",
  "editor.codeActionsOnSave": {
    "source.fixAll.eslint": "explicit",
    "source.organizeImports": "never"
  },
  "editor.tabSize": 2,
  "editor.insertSpaces": true,
  "editor.detectIndentation": false,

  // Files
  "files.eol": "\n",
  "files.trimTrailingWhitespace": true,
  "files.insertFinalNewline": true,

  // ESLint
  "eslint.validate": [
    "javascript",
    "javascriptreact",
    "typescript",
    "typescriptreact"
  ],
  "eslint.workingDirectories": [{ "mode": "auto" }],
  "eslint.useFlatConfig": true,

  // Prettier
  "[javascript]": {
    "editor.defaultFormatter": "esbenp.prettier-vscode"
  },
  "[javascriptreact]": {
    "editor.defaultFormatter": "esbenp.prettier-vscode"
  },
  "[typescript]": {
    "editor.defaultFormatter": "esbenp.prettier-vscode"
  },
  "[typescriptreact]": {
    "editor.defaultFormatter": "esbenp.prettier-vscode"
  },
  "[json]": {
    "editor.defaultFormatter": "esbenp.prettier-vscode"
  },
  "[jsonc]": {
    "editor.defaultFormatter": "esbenp.prettier-vscode"
  },
  "[markdown]": {
    "editor.defaultFormatter": "esbenp.prettier-vscode"
  },

  // TypeScript
  "typescript.preferences.importModuleSpecifier": "non-relative",
  "typescript.suggest.autoImports": true,
  "typescript.updateImportsOnFileMove.enabled": "always",

  // Tailwind
  "tailwindCSS.experimental.classRegex": [
    ["cva\\(([^)]*)\\)", "[\"'`]([^\"'`]*).*?[\"'`]"],
    ["cn\\(([^)]*)\\)", "[\"'`]([^\"'`]*).*?[\"'`]"],
    ["clsx\\(([^)]*)\\)", "[\"'`]([^\"'`]*).*?[\"'`]"]
  ],
  "tailwindCSS.classAttributes": [
    "class",
    "className",
    "ngClass",
    "containerClassName"
  ],

  // Emmet
  "emmet.includeLanguages": {
    "javascript": "javascriptreact",
    "typescript": "typescriptreact"
  }
}
```

```json
// filepath: .vscode/extensions.json
{
  "recommendations": [
    "dbaeumer.vscode-eslint",
    "esbenp.prettier-vscode",
    "bradlc.vscode-tailwindcss",
    "formulahendry.auto-rename-tag",
    "christian-kohler.path-intellisense",
    "ms-playwright.playwright",
    "vitest.explorer"
  ]
}
```

---

## 🔧 PASO 5: Configurar Husky y lint-staged

```bash
# Instalar Husky
pnpm add -D husky lint-staged

# Inicializar Husky
pnpm exec husky init
```

```json
// filepath: package.json (agregar scripts y lint-staged)
{
  "scripts": {
    "dev": "next dev",
    "build": "next build",
    "start": "next start",
    "lint": "eslint . --max-warnings 0",
    "lint:fix": "eslint . --fix",
    "format": "prettier --write .",
    "format:check": "prettier --check .",
    "type-check": "tsc --noEmit",
    "test": "vitest run",
    "test:watch": "vitest",
    "test:coverage": "vitest run --coverage",
    "test:e2e": "playwright test",
    "validate": "pnpm lint && pnpm type-check && pnpm test",
    "prepare": "husky"
  },
  "lint-staged": {
    "*.{ts,tsx}": ["eslint --fix --max-warnings 0", "prettier --write"],
    "*.{json,md,mdx,css}": ["prettier --write"]
  }
}
```

```bash
# filepath: .husky/pre-commit
pnpm exec lint-staged
```

```bash
# filepath: .husky/pre-push
pnpm run type-check
```

---

## 🔧 PASO 6: EditorConfig

```ini
# filepath: .editorconfig
root = true

[*]
charset = utf-8
end_of_line = lf
indent_size = 2
indent_style = space
insert_final_newline = true
trim_trailing_whitespace = true

[*.md]
trim_trailing_whitespace = false

[*.{yml,yaml}]
indent_size = 2

[Makefile]
indent_style = tab
```

---

## ✅ VALIDACIÓN

### Verificar ESLint

```bash
# Ejecutar lint
pnpm lint

# Output esperado:
# ✔ No ESLint warnings or errors

# Si hay errores, corregir automáticamente:
pnpm lint:fix
```

### Verificar Prettier

```bash
# Verificar formateo
pnpm format:check

# Output esperado:
# Checking formatting...
# All matched files use Prettier code style!

# Formatear archivos:
pnpm format
```

### Verificar tipos

```bash
# Type checking
pnpm type-check

# Output esperado:
# (sin errores)
```

### Verificar Husky

```bash
# Hacer un commit
git add .
git commit -m "test: verify linting"

# Debe ejecutar lint-staged automáticamente
# Si hay errores, el commit se rechaza
```

---

## 📊 RESUMEN

### Archivos creados

| Archivo                   | Función                  |
| ------------------------- | ------------------------ |
| `eslint.config.mjs`       | Configuración ESLint     |
| `.prettierrc`             | Configuración Prettier   |
| `.prettierignore`         | Archivos ignorados       |
| `.vscode/settings.json`   | Config VS Code           |
| `.vscode/extensions.json` | Extensiones recomendadas |
| `.husky/pre-commit`       | Hook pre-commit          |
| `.husky/pre-push`         | Hook pre-push            |
| `.editorconfig`           | Config del editor        |

### Reglas principales

| Categoría  | Reglas                                                |
| ---------- | ----------------------------------------------------- |
| TypeScript | no-unused-vars, no-explicit-any, no-floating-promises |
| React      | hooks rules, exhaustive-deps                          |
| Import     | order, no-cycle, no-duplicates                        |
| A11y       | alt-text, aria-props, labels                          |
| Prettier   | Formateo consistente                                  |

### Comandos disponibles

```bash
pnpm lint          # Verificar errores
pnpm lint:fix      # Corregir errores
pnpm format        # Formatear archivos
pnpm format:check  # Verificar formateo
pnpm type-check    # Verificar tipos
pnpm validate      # Lint + Types + Tests
```

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/01-SETUP/04-instalar-shadcn.md`
