# 📚 Configuración de Storybook

> **Propósito:** Documentar componentes UI de forma interactiva
> **Versión:** Storybook 8.x
> **Última actualización:** Enero 31, 2026

---

## 📋 ÍNDICE

1. [Instalación](#-instalación)
2. [Configuración](#-configuración)
3. [Estructura de Stories](#-estructura-de-stories)
4. [Integración con shadcn/ui](#-integración-con-shadcnui)
5. [Addons Recomendados](#-addons-recomendados)
6. [Documentación de Componentes](#-documentación-de-componentes)
7. [Testing Visual](#-testing-visual)
8. [Deploy](#-deploy)

---

## 🚀 INSTALACIÓN

```bash
# Inicializar Storybook en proyecto Next.js
pnpm dlx storybook@latest init

# Instalar addons adicionales
pnpm add -D @storybook/addon-a11y @storybook/addon-designs @storybook/test
```

### Estructura de Archivos Generada

```
.storybook/
├── main.ts              # Configuración principal
├── preview.ts           # Decoradores globales
└── preview-head.html    # Scripts/styles adicionales

src/
├── components/
│   └── ui/
│       ├── Button.tsx
│       └── Button.stories.tsx   # Story del componente
└── stories/
    └── Introduction.mdx         # Documentación general
```

---

## ⚙️ CONFIGURACIÓN

### `.storybook/main.ts`

```typescript
import type { StorybookConfig } from "@storybook/nextjs";

const config: StorybookConfig = {
  stories: ["../src/**/*.mdx", "../src/**/*.stories.@(js|jsx|mjs|ts|tsx)"],
  addons: [
    "@storybook/addon-onboarding",
    "@storybook/addon-essentials",
    "@chromatic-com/storybook",
    "@storybook/addon-interactions",
    "@storybook/addon-a11y", // Accesibilidad
    "@storybook/addon-designs", // Figma embeds
  ],
  framework: {
    name: "@storybook/nextjs",
    options: {},
  },
  staticDirs: ["../public"],
  docs: {
    autodocs: "tag",
  },
  typescript: {
    reactDocgen: "react-docgen-typescript",
    reactDocgenTypescriptOptions: {
      shouldExtractLiteralValuesFromEnum: true,
      propFilter: (prop) => {
        // Filtrar props de React internos
        if (prop.parent) {
          return !prop.parent.fileName.includes("node_modules");
        }
        return true;
      },
    },
  },
};

export default config;
```

### `.storybook/preview.ts`

```typescript
import type { Preview } from "@storybook/react";
import "../src/app/globals.css"; // Importar Tailwind CSS

const preview: Preview = {
  parameters: {
    controls: {
      matchers: {
        color: /(background|color)$/i,
        date: /Date$/i,
      },
    },
    // Fondos para probar dark/light mode
    backgrounds: {
      default: "light",
      values: [
        { name: "light", value: "#ffffff" },
        { name: "dark", value: "#09090b" },
        { name: "gray", value: "#f4f4f5" },
      ],
    },
    // Viewports para responsive
    viewport: {
      viewports: {
        mobile: { name: "Mobile", styles: { width: "375px", height: "667px" } },
        tablet: { name: "Tablet", styles: { width: "768px", height: "1024px" } },
        desktop: { name: "Desktop", styles: { width: "1440px", height: "900px" } },
      },
    },
    // Accesibilidad
    a11y: {
      config: {
        rules: [
          { id: "color-contrast", enabled: true },
          { id: "label", enabled: true },
        ],
      },
    },
  },
  // Decorador global para providers
  decorators: [
    (Story) => (
      <div className="font-sans antialiased">
        <Story />
      </div>
    ),
  ],
  // Tags globales
  tags: ["autodocs"],
};

export default preview;
```

---

## 📝 ESTRUCTURA DE STORIES

### Patrón Básico

```typescript
// src/components/ui/Button.stories.tsx
import type { Meta, StoryObj } from "@storybook/react";
import { Button } from "./button";

const meta: Meta<typeof Button> = {
  title: "UI/Button",
  component: Button,
  parameters: {
    layout: "centered",
    docs: {
      description: {
        component: "Botón principal de la aplicación OKLA.",
      },
    },
  },
  tags: ["autodocs"],
  argTypes: {
    variant: {
      control: "select",
      options: ["default", "destructive", "outline", "secondary", "ghost", "link"],
      description: "Variante visual del botón",
    },
    size: {
      control: "select",
      options: ["default", "sm", "lg", "icon"],
      description: "Tamaño del botón",
    },
    disabled: {
      control: "boolean",
      description: "Estado deshabilitado",
    },
    asChild: {
      control: "boolean",
      description: "Renderizar como child (para links)",
    },
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

// Story por defecto
export const Default: Story = {
  args: {
    children: "Button",
    variant: "default",
    size: "default",
  },
};

// Todas las variantes
export const AllVariants: Story = {
  render: () => (
    <div className="flex flex-wrap gap-4">
      <Button variant="default">Default</Button>
      <Button variant="destructive">Destructive</Button>
      <Button variant="outline">Outline</Button>
      <Button variant="secondary">Secondary</Button>
      <Button variant="ghost">Ghost</Button>
      <Button variant="link">Link</Button>
    </div>
  ),
};

// Todos los tamaños
export const AllSizes: Story = {
  render: () => (
    <div className="flex items-center gap-4">
      <Button size="sm">Small</Button>
      <Button size="default">Default</Button>
      <Button size="lg">Large</Button>
      <Button size="icon">🚗</Button>
    </div>
  ),
};

// Estado de carga
export const Loading: Story = {
  render: () => (
    <Button disabled>
      <span className="animate-spin mr-2">⏳</span>
      Cargando...
    </Button>
  ),
};

// Con iconos
export const WithIcons: Story = {
  render: () => (
    <div className="flex gap-4">
      <Button>
        <span className="mr-2">🔍</span>
        Buscar
      </Button>
      <Button variant="outline">
        Siguiente
        <span className="ml-2">→</span>
      </Button>
    </div>
  ),
};
```

---

## 🎨 INTEGRACIÓN CON SHADCN/UI

### Documentar Componentes shadcn/ui

```typescript
// src/components/ui/Card.stories.tsx
import type { Meta, StoryObj } from "@storybook/react";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "./card";
import { Button } from "./button";

const meta: Meta<typeof Card> = {
  title: "UI/Card",
  component: Card,
  parameters: {
    layout: "centered",
  },
  tags: ["autodocs"],
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: () => (
    <Card className="w-[350px]">
      <CardHeader>
        <CardTitle>Toyota Camry SE 2024</CardTitle>
        <CardDescription>Santo Domingo, DN</CardDescription>
      </CardHeader>
      <CardContent>
        <p className="text-2xl font-bold text-blue-600">RD$ 1,850,000</p>
        <p className="text-sm text-gray-500">15,000 km • Automático • Gasolina</p>
      </CardContent>
      <CardFooter className="flex justify-between">
        <Button variant="outline">Ver detalles</Button>
        <Button>Contactar</Button>
      </CardFooter>
    </Card>
  ),
};

export const VehicleCard: Story = {
  render: () => (
    <Card className="w-[300px] overflow-hidden">
      <div className="aspect-video bg-gray-200 relative">
        <img
          src="https://placehold.co/300x200"
          alt="Toyota Camry"
          className="object-cover w-full h-full"
        />
        <span className="absolute top-2 left-2 bg-blue-600 text-white text-xs px-2 py-1 rounded">
          Destacado
        </span>
      </div>
      <CardHeader className="pb-2">
        <CardTitle className="text-lg">Toyota Camry SE 2024</CardTitle>
      </CardHeader>
      <CardContent className="pb-2">
        <p className="text-xl font-bold text-blue-600">RD$ 1,850,000</p>
      </CardContent>
      <CardFooter>
        <Button className="w-full">Ver vehículo</Button>
      </CardFooter>
    </Card>
  ),
};
```

---

## 🔌 ADDONS RECOMENDADOS

### Addon de Accesibilidad

```typescript
// En preview.ts
parameters: {
  a11y: {
    element: "#storybook-root",
    config: {
      rules: [
        { id: "color-contrast", enabled: true },
        { id: "label", enabled: true },
        { id: "button-name", enabled: true },
        { id: "image-alt", enabled: true },
      ],
    },
    options: {
      runOnly: {
        type: "tag",
        values: ["wcag2a", "wcag2aa"],
      },
    },
  },
}
```

### Addon de Diseño (Figma)

```typescript
// En una story específica
export const WithFigma: Story = {
  parameters: {
    design: {
      type: "figma",
      url: "https://www.figma.com/file/xxx/OKLA-Design-System?node-id=123",
    },
  },
};
```

### Addon de Interacciones (Testing)

```typescript
import { within, userEvent, expect } from "@storybook/test";

export const ClickInteraction: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    // Encontrar y hacer click en el botón
    const button = canvas.getByRole("button", { name: /buscar/i });
    await userEvent.click(button);

    // Verificar resultado
    await expect(button).toHaveAttribute("aria-pressed", "true");
  },
};
```

---

## 📖 DOCUMENTACIÓN DE COMPONENTES

### MDX para Documentación Rica

```mdx
{/* src/components/ui/Button.mdx */}
import { Meta, Story, Canvas, Controls } from "@storybook/blocks";
import \* as ButtonStories from "./Button.stories";

<Meta of={ButtonStories} />

# Button

El componente `Button` es el elemento de acción principal en OKLA.

## Uso Básico

<Canvas of={ButtonStories.Default} />

## Variantes

Usa la prop `variant` para cambiar el estilo visual:

<Canvas of={ButtonStories.AllVariants} />

## Props

<Controls />

## Guías de Uso

### ✅ Hacer

- Usar texto de acción claro ("Buscar vehículo", no "Click aquí")
- Mantener consistencia en toda la app
- Incluir icono cuando aporte claridad

### ❌ No Hacer

- Usar múltiples botones primarios juntos
- Texto muy largo en botones
- Desactivar sin explicar por qué

## Accesibilidad

- El botón tiene `role="button"` automáticamente
- Soporta navegación por teclado (Enter, Space)
- Estado `:focus-visible` visible
```

---

## 🧪 TESTING VISUAL

### Chromatic Integration

```bash
# Instalar Chromatic
pnpm add -D chromatic

# Ejecutar tests visuales
pnpm chromatic --project-token=<token>
```

### GitHub Action para Visual Testing

```yaml
# .github/workflows/chromatic.yml
name: Chromatic

on:
  push:
    branches: [main, development]
  pull_request:
    branches: [main]

jobs:
  chromatic:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - uses: pnpm/action-setup@v2
        with:
          version: 8

      - uses: actions/setup-node@v4
        with:
          node-version: 20
          cache: pnpm

      - run: pnpm install

      - uses: chromaui/action@latest
        with:
          projectToken: ${{ secrets.CHROMATIC_PROJECT_TOKEN }}
          buildScriptName: build-storybook
```

---

## 🚀 DEPLOY

### Build Estático

```bash
# Build de Storybook
pnpm build-storybook

# Output en storybook-static/
```

### Deploy a Vercel

```json
// vercel.json (para storybook separado)
{
  "buildCommand": "pnpm build-storybook",
  "outputDirectory": "storybook-static"
}
```

### Deploy a GitHub Pages

```yaml
# .github/workflows/storybook.yml
name: Deploy Storybook

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: pnpm/action-setup@v2
        with:
          version: 8

      - uses: actions/setup-node@v4
        with:
          node-version: 20
          cache: pnpm

      - run: pnpm install
      - run: pnpm build-storybook

      - uses: peaceiris/actions-gh-pages@v3
        with:
          github_token: ${{ secrets.GITHUB_TOKEN }}
          publish_dir: ./storybook-static
```

---

## 📁 ORGANIZACIÓN DE STORIES

### Estructura Recomendada

```
src/
├── components/
│   ├── ui/                    # Componentes atómicos (shadcn)
│   │   ├── button.tsx
│   │   ├── button.stories.tsx
│   │   ├── card.tsx
│   │   └── card.stories.tsx
│   │
│   ├── molecules/             # Componentes compuestos
│   │   ├── VehicleCard/
│   │   │   ├── VehicleCard.tsx
│   │   │   ├── VehicleCard.stories.tsx
│   │   │   └── VehicleCard.test.tsx
│   │   └── SearchFilters/
│   │       ├── SearchFilters.tsx
│   │       └── SearchFilters.stories.tsx
│   │
│   └── organisms/             # Componentes complejos
│       ├── Navbar/
│       │   ├── Navbar.tsx
│       │   └── Navbar.stories.tsx
│       └── VehicleGrid/
│           ├── VehicleGrid.tsx
│           └── VehicleGrid.stories.tsx
│
└── stories/
    ├── Introduction.mdx       # Página de inicio
    ├── DesignTokens.mdx       # Documentación de tokens
    └── Guidelines.mdx         # Guías de uso
```

### Naming Convention

```typescript
// Títulos de stories jerárquicos
meta: {
  title: "UI/Button",           // Componentes base
  title: "Molecules/VehicleCard", // Componentes compuestos
  title: "Organisms/Navbar",    // Componentes complejos
  title: "Pages/Home",          // Páginas completas
  title: "Docs/Design Tokens",  // Documentación
}
```

---

## 📚 REFERENCIAS

- [Storybook Documentation](https://storybook.js.org/docs)
- [Storybook for Next.js](https://storybook.js.org/docs/get-started/nextjs)
- [Component Story Format 3.0](https://storybook.js.org/docs/api/csf)
- [Chromatic Visual Testing](https://www.chromatic.com/docs)
