# ♿ Accesibilidad WCAG 2.1 AA

> **Tiempo estimado:** 30 minutos
> **Prerrequisitos:** Componentes base configurados

---

## 📋 OBJETIVO

Implementar accesibilidad nivel AA:

- Configurar eslint-plugin-jsx-a11y
- Skip links y landmarks
- Focus management
- Screen reader support
- Color contrast
- Keyboard navigation

---

## 🔧 PASO 1: Configurar ESLint A11y

```bash
pnpm add -D eslint-plugin-jsx-a11y
```

```typescript
// filepath: eslint.config.mjs (agregar)
import jsxA11y from "eslint-plugin-jsx-a11y";

export default [
  // ... otras configs
  {
    plugins: {
      "jsx-a11y": jsxA11y,
    },
    rules: {
      ...jsxA11y.configs.recommended.rules,
      "jsx-a11y/anchor-is-valid": "error",
      "jsx-a11y/click-events-have-key-events": "error",
      "jsx-a11y/no-static-element-interactions": "error",
    },
  },
];
```

---

## 🔧 PASO 2: Skip Link

```typescript
// filepath: src/components/a11y/SkipLink.tsx
"use client";

import { cn } from "@/lib/utils";

export function SkipLink() {
  return (
    <a
      href="#main-content"
      className={cn(
        "sr-only focus:not-sr-only",
        "focus:fixed focus:top-4 focus:left-4 focus:z-[100]",
        "focus:px-4 focus:py-2 focus:bg-primary-600 focus:text-white",
        "focus:rounded-lg focus:outline-none focus:ring-2 focus:ring-white"
      )}
    >
      Saltar al contenido principal
    </a>
  );
}
```

```typescript
// filepath: src/app/layout.tsx (agregar)
import { SkipLink } from "@/components/a11y/SkipLink";

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="es">
      <body>
        <SkipLink />
        {children}
      </body>
    </html>
  );
}
```

---

## 🔧 PASO 3: Landmarks ARIA

```typescript
// filepath: src/components/layout/MainLayout.tsx
export function MainLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-screen flex flex-col">
      <header role="banner">
        <Header />
      </header>

      <main id="main-content" role="main" tabIndex={-1} className="flex-1">
        {children}
      </main>

      <footer role="contentinfo">
        <Footer />
      </footer>
    </div>
  );
}
```

---

## 🔧 PASO 4: Focus Trap para Modales

```typescript
// filepath: src/lib/hooks/useFocusTrap.ts
"use client";

import * as React from "react";

export function useFocusTrap(isActive: boolean) {
  const containerRef = React.useRef<HTMLDivElement>(null);

  React.useEffect(() => {
    if (!isActive || !containerRef.current) return;

    const container = containerRef.current;
    const focusableElements = container.querySelectorAll<HTMLElement>(
      'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])',
    );

    const firstElement = focusableElements[0];
    const lastElement = focusableElements[focusableElements.length - 1];

    // Focus first element
    firstElement?.focus();

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key !== "Tab") return;

      if (e.shiftKey) {
        if (document.activeElement === firstElement) {
          e.preventDefault();
          lastElement?.focus();
        }
      } else {
        if (document.activeElement === lastElement) {
          e.preventDefault();
          firstElement?.focus();
        }
      }
    };

    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [isActive]);

  return containerRef;
}
```

---

## 🔧 PASO 5: Announce para Screen Readers

```typescript
// filepath: src/components/a11y/Announcer.tsx
"use client";

import * as React from "react";
import { create } from "zustand";

interface AnnouncerState {
  message: string;
  announce: (message: string) => void;
}

export const useAnnouncer = create<AnnouncerState>((set) => ({
  message: "",
  announce: (message) => {
    set({ message: "" });
    setTimeout(() => set({ message }), 100);
  },
}));

export function Announcer() {
  const message = useAnnouncer((s) => s.message);

  return (
    <div
      role="status"
      aria-live="polite"
      aria-atomic="true"
      className="sr-only"
    >
      {message}
    </div>
  );
}

// Hook helper
export function useAnnounce() {
  return useAnnouncer((s) => s.announce);
}
```

---

## 🔧 PASO 6: Visible Focus Styles

```css
/* filepath: src/app/globals.css (agregar) */
@layer base {
  /* Remove default focus outline, add custom */
  *:focus {
    outline: none;
  }

  /* Visible focus for keyboard users */
  *:focus-visible {
    outline: 2px solid var(--primary-600);
    outline-offset: 2px;
  }

  /* Skip focus styles for mouse users */
  *:focus:not(:focus-visible) {
    outline: none;
  }
}
```

---

## 🔧 PASO 7: Componente VisuallyHidden

```typescript
// filepath: src/components/a11y/VisuallyHidden.tsx
import { cn } from "@/lib/utils";

interface VisuallyHiddenProps {
  children: React.ReactNode;
  as?: "span" | "div" | "label";
  className?: string;
}

export function VisuallyHidden({
  children,
  as: Component = "span",
  className,
}: VisuallyHiddenProps) {
  return (
    <Component className={cn("sr-only", className)}>
      {children}
    </Component>
  );
}
```

---

## ✅ CHECKLIST WCAG 2.1 AA

### Perceptible

- [x] Textos alternativos en imágenes
- [x] Contraste mínimo 4.5:1
- [x] No depender solo del color
- [x] Contenido responsivo

### Operable

- [x] Todo accesible con teclado
- [x] Skip links disponibles
- [x] Focus visible
- [x] Sin trampas de teclado

### Comprensible

- [x] Idioma del documento definido
- [x] Labels en formularios
- [x] Mensajes de error claros
- [x] Navegación consistente

### Robusto

- [x] HTML semántico válido
- [x] ARIA roles correctos
- [x] Compatible con lectores de pantalla

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/03-COMPONENTES/04-dealers.md`
