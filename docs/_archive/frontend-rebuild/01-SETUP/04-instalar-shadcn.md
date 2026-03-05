# 📦 Instalar y Configurar shadcn/ui

> **Tiempo estimado:** 25 minutos
> **Prerrequisitos:** Proyecto Next.js, Tailwind CSS configurado

---

## 📋 OBJETIVO

Instalar shadcn/ui y configurar componentes base:

- Inicializar shadcn/ui
- Instalar componentes esenciales
- Configurar variantes personalizadas
- Crear utilidades de estilo

---

## 🔧 PASO 1: Inicializar shadcn/ui

```bash
# Ejecutar comando de inicialización
pnpm dlx shadcn@latest init

# Responder a las preguntas:
# ✔ Which style would you like to use? › New York
# ✔ Which color would you like to use as the base color? › Neutral
# ✔ Would you like to use CSS variables for theming? › yes
```

### Verificar archivos creados

```bash
# Debe existir:
ls -la components.json
ls -la src/lib/utils.ts
ls -la src/app/globals.css
```

---

## 🔧 PASO 2: Configurar components.json

```json
// filepath: components.json
{
  "$schema": "https://ui.shadcn.com/schema.json",
  "style": "new-york",
  "rsc": true,
  "tsx": true,
  "tailwind": {
    "config": "tailwind.config.ts",
    "css": "src/app/globals.css",
    "baseColor": "neutral",
    "cssVariables": true,
    "prefix": ""
  },
  "aliases": {
    "components": "@/components",
    "utils": "@/lib/utils",
    "ui": "@/components/ui",
    "lib": "@/lib",
    "hooks": "@/lib/hooks"
  },
  "iconLibrary": "lucide"
}
```

---

## 🔧 PASO 3: Instalar Componentes Esenciales

```bash
# Componentes base (instalar todos de una vez)
pnpm dlx shadcn@latest add \
  button \
  input \
  label \
  select \
  checkbox \
  radio-group \
  switch \
  textarea \
  dialog \
  dropdown-menu \
  popover \
  tooltip \
  tabs \
  card \
  badge \
  avatar \
  separator \
  skeleton \
  toast \
  sonner \
  form \
  sheet \
  alert \
  alert-dialog \
  accordion \
  scroll-area
```

### Verificar instalación

```bash
# Ver componentes instalados
ls -la src/components/ui/

# Debe mostrar:
# accordion.tsx
# alert-dialog.tsx
# alert.tsx
# avatar.tsx
# badge.tsx
# button.tsx
# card.tsx
# checkbox.tsx
# dialog.tsx
# dropdown-menu.tsx
# form.tsx
# input.tsx
# label.tsx
# popover.tsx
# radio-group.tsx
# scroll-area.tsx
# select.tsx
# separator.tsx
# sheet.tsx
# skeleton.tsx
# sonner.tsx
# switch.tsx
# tabs.tsx
# textarea.tsx
# toast.tsx
# tooltip.tsx
```

---

## 🔧 PASO 4: Configurar Utilidades

```typescript
// filepath: src/lib/utils.ts
import { type ClassValue, clsx } from "clsx";
import { twMerge } from "tailwind-merge";

/**
 * Combina clases de Tailwind de forma segura
 * Resuelve conflictos y merge correctamente
 */
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

/**
 * Formatea un número con separadores de miles
 */
export function formatNumber(num: number): string {
  return new Intl.NumberFormat("es-DO").format(num);
}

/**
 * Formatea un precio en RD$
 */
export function formatPrice(price: number): string {
  return `RD$ ${formatNumber(price)}`;
}

/**
 * Formatea una fecha relativa (hace X días)
 */
export function formatRelativeDate(date: Date | string): string {
  const now = new Date();
  const then = new Date(date);
  const diffMs = now.getTime() - then.getTime();
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

  if (diffDays === 0) {
    return "Hoy";
  }
  if (diffDays === 1) {
    return "Ayer";
  }
  if (diffDays < 7) {
    return `Hace ${diffDays} días`;
  }
  if (diffDays < 30) {
    const weeks = Math.floor(diffDays / 7);
    return `Hace ${weeks} semana${weeks > 1 ? "s" : ""}`;
  }
  if (diffDays < 365) {
    const months = Math.floor(diffDays / 30);
    return `Hace ${months} mes${months > 1 ? "es" : ""}`;
  }
  const years = Math.floor(diffDays / 365);
  return `Hace ${years} año${years > 1 ? "s" : ""}`;
}

/**
 * Formatea un número de teléfono dominicano
 */
export function formatPhoneNumber(phone: string): string {
  const cleaned = phone.replace(/\D/g, "");
  if (cleaned.length === 10) {
    return `${cleaned.slice(0, 3)}-${cleaned.slice(3, 6)}-${cleaned.slice(6)}`;
  }
  return phone;
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
    .replace(/^-+|-+$/g, "");
}

/**
 * Trunca un texto a una longitud máxima
 */
export function truncate(text: string, maxLength: number): string {
  if (text.length <= maxLength) {
    return text;
  }
  return text.slice(0, maxLength).trim() + "...";
}

/**
 * Capitaliza la primera letra
 */
export function capitalize(text: string): string {
  return text.charAt(0).toUpperCase() + text.slice(1).toLowerCase();
}

/**
 * Valida un RNC dominicano (9 dígitos)
 */
export function isValidRNC(rnc: string): boolean {
  const cleaned = rnc.replace(/\D/g, "");
  return cleaned.length === 9;
}

/**
 * Valida una cédula dominicana (11 dígitos)
 */
export function isValidCedula(cedula: string): boolean {
  const cleaned = cedula.replace(/\D/g, "");
  if (cleaned.length !== 11) {
    return false;
  }

  // Luhn algorithm for Dominican cedula
  const weights = [1, 2, 1, 2, 1, 2, 1, 2, 1, 2];
  let sum = 0;

  for (let i = 0; i < 10; i++) {
    let digit = parseInt(cleaned[i]) * weights[i];
    if (digit > 9) {
      digit = Math.floor(digit / 10) + (digit % 10);
    }
    sum += digit;
  }

  const checkDigit = (10 - (sum % 10)) % 10;
  return checkDigit === parseInt(cleaned[10]);
}
```

---

## 🔧 PASO 5: Personalizar Button

```typescript
// filepath: src/components/ui/button.tsx
import * as React from "react";
import { Slot } from "@radix-ui/react-slot";
import { cva, type VariantProps } from "class-variance-authority";
import { Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";

const buttonVariants = cva(
  [
    "inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-lg",
    "text-sm font-medium transition-all duration-200",
    "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2",
    "disabled:pointer-events-none disabled:opacity-50",
    "[&_svg]:pointer-events-none [&_svg]:size-4 [&_svg]:shrink-0",
  ],
  {
    variants: {
      variant: {
        default:
          "bg-primary-600 text-white shadow hover:bg-primary-700 active:bg-primary-800",
        destructive:
          "bg-red-600 text-white shadow-sm hover:bg-red-700 active:bg-red-800",
        outline:
          "border border-gray-300 bg-white text-gray-700 shadow-sm hover:bg-gray-50 hover:border-gray-400 active:bg-gray-100",
        secondary:
          "bg-gray-100 text-gray-900 shadow-sm hover:bg-gray-200 active:bg-gray-300",
        ghost:
          "text-gray-700 hover:bg-gray-100 hover:text-gray-900 active:bg-gray-200",
        link:
          "text-primary-600 underline-offset-4 hover:underline",
        success:
          "bg-green-600 text-white shadow hover:bg-green-700 active:bg-green-800",
      },
      size: {
        default: "h-10 px-4 py-2",
        xs: "h-7 rounded-md px-2 text-xs",
        sm: "h-9 rounded-md px-3",
        lg: "h-12 rounded-lg px-6 text-base",
        xl: "h-14 rounded-xl px-8 text-lg",
        icon: "h-10 w-10",
        "icon-sm": "h-8 w-8",
        "icon-lg": "h-12 w-12",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  }
);

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean;
  isLoading?: boolean;
  loadingText?: string;
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  (
    {
      className,
      variant,
      size,
      asChild = false,
      isLoading = false,
      loadingText,
      disabled,
      children,
      ...props
    },
    ref
  ) => {
    const Comp = asChild ? Slot : "button";

    return (
      <Comp
        className={cn(buttonVariants({ variant, size, className }))}
        ref={ref}
        disabled={disabled || isLoading}
        {...props}
      >
        {isLoading ? (
          <>
            <Loader2 className="animate-spin" />
            {loadingText || children}
          </>
        ) : (
          children
        )}
      </Comp>
    );
  }
);
Button.displayName = "Button";

export { Button, buttonVariants };
```

---

## 🔧 PASO 6: Personalizar Badge

```typescript
// filepath: src/components/ui/badge.tsx
import * as React from "react";
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "@/lib/utils";

const badgeVariants = cva(
  [
    "inline-flex items-center rounded-full border font-medium transition-colors",
    "focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2",
  ],
  {
    variants: {
      variant: {
        default: "border-transparent bg-gray-100 text-gray-800",
        primary: "border-transparent bg-primary-100 text-primary-700",
        secondary: "border-transparent bg-gray-100 text-gray-700",
        success: "border-transparent bg-green-100 text-green-700",
        warning: "border-transparent bg-yellow-100 text-yellow-700",
        error: "border-transparent bg-red-100 text-red-700",
        outline: "border-gray-300 text-gray-700",
        // Específicos para vehículos
        new: "border-transparent bg-green-500 text-white",
        used: "border-transparent bg-blue-100 text-blue-700",
        certified: "border-transparent bg-purple-100 text-purple-700",
        featured: "border-transparent bg-yellow-400 text-yellow-900",
        dealer: "border-transparent bg-primary-500 text-white",
      },
      size: {
        default: "px-2.5 py-0.5 text-xs",
        sm: "px-2 py-px text-[10px]",
        lg: "px-3 py-1 text-sm",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  }
);

export interface BadgeProps
  extends React.HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof badgeVariants> {}

function Badge({ className, variant, size, ...props }: BadgeProps) {
  return (
    <div
      className={cn(badgeVariants({ variant, size }), className)}
      {...props}
    />
  );
}

export { Badge, badgeVariants };
```

---

## 🔧 PASO 7: Configurar Sonner (Toasts)

```typescript
// filepath: src/components/ui/sonner.tsx
"use client";

import { useTheme } from "next-themes";
import { Toaster as Sonner } from "sonner";

type ToasterProps = React.ComponentProps<typeof Sonner>;

const Toaster = ({ ...props }: ToasterProps) => {
  const { theme = "system" } = useTheme();

  return (
    <Sonner
      theme={theme as ToasterProps["theme"]}
      className="toaster group"
      position="bottom-right"
      expand={false}
      richColors
      closeButton
      toastOptions={{
        classNames: {
          toast:
            "group toast group-[.toaster]:bg-white group-[.toaster]:text-gray-950 group-[.toaster]:border-gray-200 group-[.toaster]:shadow-lg",
          description: "group-[.toast]:text-gray-500",
          actionButton:
            "group-[.toast]:bg-primary-600 group-[.toast]:text-white",
          cancelButton:
            "group-[.toast]:bg-gray-100 group-[.toast]:text-gray-500",
          closeButton:
            "group-[.toast]:bg-white group-[.toast]:border-gray-200",
        },
      }}
      {...props}
    />
  );
};

export { Toaster };
```

```typescript
// filepath: src/lib/toast.ts
import { toast } from "sonner";

/**
 * Toast wrappers con estilos consistentes
 */
export const showToast = {
  success: (message: string, description?: string) => {
    toast.success(message, { description });
  },

  error: (message: string, description?: string) => {
    toast.error(message, { description });
  },

  warning: (message: string, description?: string) => {
    toast.warning(message, { description });
  },

  info: (message: string, description?: string) => {
    toast.info(message, { description });
  },

  loading: (message: string) => {
    return toast.loading(message);
  },

  promise: <T>(
    promise: Promise<T>,
    messages: {
      loading: string;
      success: string | ((data: T) => string);
      error: string | ((error: Error) => string);
    },
  ) => {
    return toast.promise(promise, messages);
  },

  dismiss: (toastId?: string | number) => {
    toast.dismiss(toastId);
  },
};
```

---

## 🔧 PASO 8: Agregar al Layout

```typescript
// filepath: src/app/layout.tsx (actualizar)
import { Toaster } from "@/components/ui/sonner";

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="es" suppressHydrationWarning>
      <body className={cn(font.variable, "antialiased")}>
        <Providers>
          {children}
          <Toaster />
        </Providers>
      </body>
    </html>
  );
}
```

---

## ✅ VALIDACIÓN

### Verificar componentes

```bash
# Listar todos los componentes instalados
ls src/components/ui/

# Debe mostrar 25+ componentes
```

### Test de Button

```typescript
// filepath: __tests__/components/ui/Button.test.tsx
import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { Button } from "@/components/ui/button";

describe("Button", () => {
  it("renders correctly", () => {
    render(<Button>Click me</Button>);
    expect(screen.getByRole("button")).toHaveTextContent("Click me");
  });

  it("shows loading state", () => {
    render(<Button isLoading>Submit</Button>);
    expect(screen.getByRole("button")).toBeDisabled();
  });

  it("applies variant classes", () => {
    render(<Button variant="destructive">Delete</Button>);
    expect(screen.getByRole("button")).toHaveClass("bg-red-600");
  });
});
```

### Test de Badge

```typescript
// filepath: __tests__/components/ui/Badge.test.tsx
import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { Badge } from "@/components/ui/badge";

describe("Badge", () => {
  it("renders correctly", () => {
    render(<Badge>New</Badge>);
    expect(screen.getByText("New")).toBeInTheDocument();
  });

  it("applies variant classes", () => {
    render(<Badge variant="success">Active</Badge>);
    expect(screen.getByText("Active")).toHaveClass("bg-green-100");
  });
});
```

### Ejecutar tests

```bash
pnpm test components/ui

# Output esperado:
# ✓ Button renders correctly
# ✓ Button shows loading state
# ✓ Button applies variant classes
# ✓ Badge renders correctly
# ✓ Badge applies variant classes
```

---

## 📊 RESUMEN

### Componentes instalados

| Categoría   | Componentes                                                                 |
| ----------- | --------------------------------------------------------------------------- |
| Formularios | button, input, label, select, checkbox, radio-group, switch, textarea, form |
| Feedback    | toast, sonner, alert, skeleton                                              |
| Overlays    | dialog, dropdown-menu, popover, tooltip, sheet, alert-dialog                |
| Navegación  | tabs, accordion                                                             |
| Display     | card, badge, avatar, separator, scroll-area                                 |

### Utilidades creadas

| Función                | Descripción             |
| ---------------------- | ----------------------- |
| `cn()`                 | Combina clases Tailwind |
| `formatNumber()`       | Formato de números      |
| `formatPrice()`        | Formato RD$             |
| `formatRelativeDate()` | Hace X días             |
| `formatPhoneNumber()`  | Teléfono DR             |
| `slugify()`            | Genera slugs            |
| `isValidRNC()`         | Valida RNC              |
| `isValidCedula()`      | Valida cédula           |

### Personalizaciones

| Componente | Cambios                                             |
| ---------- | --------------------------------------------------- |
| Button     | 7 variantes, 8 tamaños, loading state               |
| Badge      | 12 variantes, 3 tamaños, específicos para vehículos |
| Sonner     | Posición bottom-right, rich colors                  |

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/01-SETUP/05-configurar-playwright.md`
