# 🏗️ Metodología de Desarrollo de Componentes

> **Principio fundamental:** Los estilos se centralizan en componentes. Las páginas solo componen componentes sin estilos adicionales.

---

## 📋 Índice

1. [Principios de Arquitectura](#principios-de-arquitectura)
2. [Estructura de Componentes](#estructura-de-componentes)
3. [Patrones de Diseño](#patrones-de-diseño)
4. [Checklist de Componentes](#checklist-de-componentes)
5. [Ejemplos Prácticos](#ejemplos-prácticos)

---

## 🎯 Principios de Arquitectura

### 1. Separación de Responsabilidades

```
┌─────────────────────────────────────────────────────────────────┐
│                          PÁGINA                                  │
│  - Solo compone componentes                                     │
│  - Maneja datos y estado                                        │
│  - NO tiene estilos directos (excepto layout básico)           │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                       COMPONENTES                                │
│  - Encapsulan TODO el estilo visual                            │
│  - Props para variantes (size, variant, color)                 │
│  - Reutilizables en cualquier página                           │
│  - Exportados desde barrel (index.ts)                          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      PRIMITIVOS UI                               │
│  - Button, Input, Dialog, Select                                │
│  - Basados en Radix UI                                          │
│  - Variantes con CVA (class-variance-authority)                │
└─────────────────────────────────────────────────────────────────┘
```

### 2. Regla de Oro

> **Si escribes `className` directamente en una página, probablemente deberías crear un componente.**

### 3. Flujo de Estilos

```
Tokens (CSS Variables)
    ↓
Tailwind Config
    ↓
Componentes UI (Button, Input)
    ↓
Componentes de Dominio (HeroStatic, CTASection)
    ↓
Páginas (solo composición)
```

---

## 📁 Estructura de Componentes

### Ubicación de Archivos

```
src/
├── components/
│   ├── ui/                    # Primitivos UI (Button, Input, Dialog)
│   │   ├── button.tsx
│   │   ├── input.tsx
│   │   └── index.ts           # Barrel export
│   │
│   ├── homepage/              # Componentes específicos de homepage
│   │   ├── hero-carousel.tsx
│   │   ├── hero-static.tsx
│   │   ├── section-container.tsx
│   │   ├── section-header.tsx
│   │   ├── features-grid.tsx
│   │   ├── cta-section.tsx
│   │   ├── loading-states.tsx
│   │   └── index.ts           # Barrel export
│   │
│   ├── vehicles/              # Componentes de vehículos
│   │   ├── vehicle-card.tsx
│   │   ├── vehicle-filters.tsx
│   │   └── index.ts
│   │
│   └── layout/                # Componentes de layout
│       ├── navbar.tsx
│       ├── footer.tsx
│       └── index.ts
│
└── app/
    ├── page.tsx               # Homepage - SOLO compone componentes
    └── vehiculos/
        └── page.tsx           # Vehículos - SOLO compone componentes
```

### Barrel Exports (index.ts)

Cada carpeta de componentes DEBE tener un `index.ts`:

```typescript
/**
 * Homepage components barrel export
 *
 * All homepage components are centralized here with their own styling.
 * Pages should import and use these without additional styling.
 */

// Hero components
export { default as HeroCarousel } from "./hero-carousel";
export { HeroStatic } from "./hero-static";

// Section components
export { SectionContainer } from "./section-container";
export { SectionHeader } from "./section-header";

// Feature components
export { FeaturesGrid } from "./features-grid";
export type { Feature } from "./features-grid";

// CTA components
export { CTASection } from "./cta-section";

// Loading states
export { LoadingSection, ErrorSection, SkeletonGrid } from "./loading-states";

// Types
export type { FeaturedListingItem } from "./featured-section";
```

---

## 🎨 Patrones de Diseño

### Patrón 1: Componente con Variantes (CVA)

Para componentes con múltiples variantes visuales:

```typescript
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '@/lib/utils';

const buttonVariants = cva(
  // Base styles
  'inline-flex items-center justify-center rounded-lg font-medium transition-all',
  {
    variants: {
      variant: {
        default: 'bg-[#00A870] text-white hover:bg-[#009663]',
        secondary: 'border-2 border-[#00A870] text-[#00A870]',
        outline: 'border border-gray-300 bg-white text-gray-700',
        ghost: 'text-gray-700 hover:bg-gray-100',
      },
      size: {
        sm: 'h-8 px-3 text-sm',
        default: 'h-10 px-4',
        lg: 'h-12 px-6 text-lg',
        xl: 'h-14 px-8 text-xl',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'default',
    },
  }
);

interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean;
}

export function Button({ className, variant, size, ...props }: ButtonProps) {
  return (
    <button className={cn(buttonVariants({ variant, size }), className)} {...props} />
  );
}
```

### Patrón 2: Componente con Props de Configuración

Para componentes que reciben configuración estructurada:

```typescript
interface CTAButton {
  label: string;
  href: string;
  variant?: 'primary' | 'secondary';
}

interface CTASectionProps {
  title: string;
  subtitle?: string;
  primaryButton: CTAButton;
  secondaryButton?: CTAButton;
  className?: string;
}

export function CTASection({
  title,
  subtitle,
  primaryButton,
  secondaryButton,
  className,
}: CTASectionProps) {
  return (
    <section className={cn('bg-[#00A870] py-16', className)}>
      {/* Implementación con estilos encapsulados */}
    </section>
  );
}
```

### Patrón 3: Componente Container

Para wrappers reutilizables:

```typescript
interface SectionContainerProps {
  title?: string;
  subtitle?: string;
  children: React.ReactNode;
  background?: 'white' | 'gray' | 'gradient';
  className?: string;
}

export function SectionContainer({
  title,
  subtitle,
  children,
  background = 'white',
  className,
}: SectionContainerProps) {
  const bgClasses = {
    white: 'bg-white',
    gray: 'bg-gray-50',
    gradient: 'bg-gradient-to-b from-white to-gray-50',
  };

  return (
    <section className={cn('py-6', bgClasses[background], className)}>
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        {title && <SectionHeader title={title} subtitle={subtitle} />}
        {children}
      </div>
    </section>
  );
}
```

### Patrón 4: Componente de Estado (Loading/Error)

```typescript
interface LoadingSectionProps {
  message?: string;
  className?: string;
}

export function LoadingSection({
  message = 'Cargando...',
  className
}: LoadingSectionProps) {
  return (
    <section className={cn('bg-gray-50 py-12', className)}>
      <div className="mx-auto max-w-7xl px-4 text-center">
        <Loader2 className="mx-auto mb-4 h-12 w-12 animate-spin text-[#00A870]" />
        <p className="text-gray-600">{message}</p>
      </div>
    </section>
  );
}
```

---

## ✅ Checklist de Componentes

### Al crear un nuevo componente:

- [ ] **Ubicación correcta** - En la carpeta de dominio apropiada (`homepage/`, `vehicles/`, etc.)
- [ ] **Props tipadas** - Interface con todas las props documentadas
- [ ] **Estilos encapsulados** - TODO el CSS dentro del componente
- [ ] **Variantes con CVA** - Si tiene múltiples estados visuales
- [ ] **className prop** - Para permitir extensión cuando sea necesario
- [ ] **Default values** - Props opcionales tienen valores por defecto
- [ ] **Export en index.ts** - Agregado al barrel export
- [ ] **Documentación JSDoc** - Comentarios explicando uso

### Al crear una página:

- [ ] **Solo imports de componentes** - No usar clases de Tailwind directamente
- [ ] **Datos y lógica** - Solo manejo de estado y transformaciones
- [ ] **Composición limpia** - JSX legible y declarativo
- [ ] **Sin estilos inline** - Excepto layout básico (`<>`, `<div className="flex">`)

---

## 📝 Ejemplos Prácticos

### ❌ INCORRECTO - Página con estilos mezclados

```tsx
// ❌ NO HACER ESTO
export default function HomePage() {
  return (
    <>
      <section className="relative h-[calc(100vh-4rem)] overflow-hidden bg-gradient-to-br from-gray-900 to-gray-800">
        <div className="absolute inset-0 bg-[url('/hero-pattern.svg')] opacity-10" />
        <div className="relative mx-auto flex h-full max-w-7xl items-center px-4">
          <h1 className="text-4xl font-bold text-white">Tu próximo vehículo</h1>
          <Link
            href="/vehiculos"
            className="inline-flex h-14 items-center bg-[#00A870] px-8 text-white"
          >
            Explorar
          </Link>
        </div>
      </section>

      <section className="bg-white py-12">
        <div className="mx-auto max-w-7xl px-4">
          <h2 className="text-3xl font-bold">Vehículos</h2>
          {/* más estilos... */}
        </div>
      </section>
    </>
  );
}
```

### ✅ CORRECTO - Página que compone componentes

```tsx
// ✅ HACER ESTO
import {
  HeroStatic,
  SectionContainer,
  FeaturedListingGrid,
  FeaturesGrid,
  CTASection,
  LoadingSection,
  ErrorSection,
} from "@/components/homepage";

export default function HomePage() {
  const { vehicles, isLoading, error } = useVehicles();

  return (
    <>
      {/* Hero - componente con estilos encapsulados */}
      <HeroStatic />

      {/* Sección - componente container */}
      <SectionContainer
        title="Vehículos Destacados"
        subtitle="Explora nuestra selección premium"
        background="gradient"
      >
        {isLoading ? (
          <LoadingSection />
        ) : error ? (
          <ErrorSection message={error} />
        ) : (
          <FeaturedListingGrid vehicles={vehicles} maxItems={9} />
        )}
      </SectionContainer>

      {/* Features - componente con data prop */}
      <FeaturesGrid features={FEATURES} />

      {/* CTA - componente configurado via props */}
      <CTASection
        title="¿Listo para vender?"
        subtitle="Publica en minutos"
        primaryButton={{ label: "Publicar", href: "/vender" }}
      />
    </>
  );
}
```

---

## 🎨 Colores OKLA

Usar siempre los colores de marca:

```typescript
// En componentes, usar directamente:
"bg-[#00A870]"; // Verde primario
"text-[#00A870]"; // Texto verde
"hover:bg-[#009663]"; // Verde oscuro (hover)
"bg-[#00A870]/10"; // Verde con opacidad

// O usar la variable de Tailwind si está configurada:
"bg-primary";
"text-primary";
```

---

## 📚 Documentos Relacionados

- [02-UX-DESIGN-SYSTEM/01-design-tokens.md](../02-UX-DESIGN-SYSTEM/01-design-tokens.md) - Tokens de diseño
- [03-COMPONENTES/07-homepage-components.md](07-homepage-components.md) - Componentes del homepage
- [01-SETUP/01-crear-proyecto.md](../01-SETUP/01-crear-proyecto.md) - Setup del proyecto

---

**Última actualización:** Enero 2026
