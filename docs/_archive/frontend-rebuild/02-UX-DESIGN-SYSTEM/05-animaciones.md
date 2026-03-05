# ✨ Animaciones con Framer Motion

> **Tiempo estimado:** 45 minutos
> **Prerrequisitos:** Proyecto Next.js configurado

---

## 📋 OBJETIVO

Implementar animaciones consistentes:

- Configurar Framer Motion
- Crear variantes reutilizables
- Animaciones de página
- Micro-interacciones
- Loading states animados
- Accesibilidad (reduced motion)

---

## 🔧 PASO 1: Instalar Framer Motion

```bash
pnpm add framer-motion
```

---

## 🔧 PASO 2: Configuración Base

```typescript
// filepath: src/lib/motion.ts
import { Variants, Transition } from "framer-motion";

/**
 * Transiciones base
 */
export const transitions = {
  // Rápida para micro-interacciones
  fast: {
    duration: 0.15,
    ease: "easeOut",
  } as Transition,

  // Normal para la mayoría de animaciones
  normal: {
    duration: 0.3,
    ease: [0.4, 0, 0.2, 1], // ease-out-cubic
  } as Transition,

  // Suave para entradas/salidas
  smooth: {
    duration: 0.5,
    ease: [0.22, 1, 0.36, 1], // ease-out-expo
  } as Transition,

  // Bounce para elementos divertidos
  bounce: {
    type: "spring",
    stiffness: 400,
    damping: 25,
  } as Transition,

  // Spring suave
  spring: {
    type: "spring",
    stiffness: 300,
    damping: 30,
  } as Transition,
} as const;

/**
 * Variantes de fade
 */
export const fadeVariants: Variants = {
  initial: { opacity: 0 },
  animate: { opacity: 1 },
  exit: { opacity: 0 },
};

/**
 * Variantes de slide desde arriba
 */
export const slideDownVariants: Variants = {
  initial: { opacity: 0, y: -20 },
  animate: { opacity: 1, y: 0 },
  exit: { opacity: 0, y: -20 },
};

/**
 * Variantes de slide desde abajo
 */
export const slideUpVariants: Variants = {
  initial: { opacity: 0, y: 20 },
  animate: { opacity: 1, y: 0 },
  exit: { opacity: 0, y: 20 },
};

/**
 * Variantes de slide desde izquierda
 */
export const slideLeftVariants: Variants = {
  initial: { opacity: 0, x: -20 },
  animate: { opacity: 1, x: 0 },
  exit: { opacity: 0, x: -20 },
};

/**
 * Variantes de slide desde derecha
 */
export const slideRightVariants: Variants = {
  initial: { opacity: 0, x: 20 },
  animate: { opacity: 1, x: 0 },
  exit: { opacity: 0, x: 20 },
};

/**
 * Variantes de scale
 */
export const scaleVariants: Variants = {
  initial: { opacity: 0, scale: 0.9 },
  animate: { opacity: 1, scale: 1 },
  exit: { opacity: 0, scale: 0.9 },
};

/**
 * Variantes de pop (para elementos pequeños)
 */
export const popVariants: Variants = {
  initial: { opacity: 0, scale: 0.5 },
  animate: { opacity: 1, scale: 1 },
  exit: { opacity: 0, scale: 0.5 },
};

/**
 * Variantes de contenedor para stagger children
 */
export const staggerContainerVariants: Variants = {
  initial: {},
  animate: {
    transition: {
      staggerChildren: 0.1,
      delayChildren: 0.05,
    },
  },
  exit: {
    transition: {
      staggerChildren: 0.05,
      staggerDirection: -1,
    },
  },
};

/**
 * Variantes de item para stagger
 */
export const staggerItemVariants: Variants = {
  initial: { opacity: 0, y: 20 },
  animate: {
    opacity: 1,
    y: 0,
    transition: transitions.normal,
  },
  exit: {
    opacity: 0,
    y: 20,
    transition: transitions.fast,
  },
};

/**
 * Variantes para modales/dialogs
 */
export const modalVariants: Variants = {
  initial: { opacity: 0, scale: 0.95, y: 10 },
  animate: {
    opacity: 1,
    scale: 1,
    y: 0,
    transition: transitions.smooth,
  },
  exit: {
    opacity: 0,
    scale: 0.95,
    y: 10,
    transition: transitions.fast,
  },
};

/**
 * Variantes para overlay/backdrop
 */
export const overlayVariants: Variants = {
  initial: { opacity: 0 },
  animate: { opacity: 1, transition: transitions.normal },
  exit: { opacity: 0, transition: transitions.fast },
};

/**
 * Variantes para drawer/sheet desde la derecha
 */
export const drawerRightVariants: Variants = {
  initial: { x: "100%" },
  animate: { x: 0, transition: transitions.smooth },
  exit: { x: "100%", transition: transitions.normal },
};

/**
 * Variantes para drawer/sheet desde abajo
 */
export const drawerBottomVariants: Variants = {
  initial: { y: "100%" },
  animate: { y: 0, transition: transitions.smooth },
  exit: { y: "100%", transition: transitions.normal },
};

/**
 * Variantes para dropdown
 */
export const dropdownVariants: Variants = {
  initial: { opacity: 0, y: -10, scale: 0.95 },
  animate: {
    opacity: 1,
    y: 0,
    scale: 1,
    transition: transitions.fast,
  },
  exit: {
    opacity: 0,
    y: -10,
    scale: 0.95,
    transition: { duration: 0.1 },
  },
};

/**
 * Variantes para tooltips
 */
export const tooltipVariants: Variants = {
  initial: { opacity: 0, scale: 0.9 },
  animate: {
    opacity: 1,
    scale: 1,
    transition: { duration: 0.15 },
  },
  exit: {
    opacity: 0,
    scale: 0.9,
    transition: { duration: 0.1 },
  },
};
```

---

## 🔧 PASO 3: Hook de Reduced Motion

```typescript
// filepath: src/lib/hooks/useReducedMotion.ts
"use client";

import * as React from "react";

export function useReducedMotion(): boolean {
  const [reducedMotion, setReducedMotion] = React.useState(false);

  React.useEffect(() => {
    const mediaQuery = window.matchMedia("(prefers-reduced-motion: reduce)");

    setReducedMotion(mediaQuery.matches);

    const handleChange = (event: MediaQueryListEvent) => {
      setReducedMotion(event.matches);
    };

    mediaQuery.addEventListener("change", handleChange);
    return () => mediaQuery.removeEventListener("change", handleChange);
  }, []);

  return reducedMotion;
}

/**
 * Retorna variantes sin animación si el usuario prefiere reduced motion
 */
export function useAnimationVariants<T extends Record<string, unknown>>(
  variants: T,
): T | Record<string, Record<string, never>> {
  const reducedMotion = useReducedMotion();

  if (reducedMotion) {
    // Retornar variantes vacías
    return Object.keys(variants).reduce(
      (acc, key) => ({
        ...acc,
        [key]: {},
      }),
      {} as Record<string, Record<string, never>>,
    );
  }

  return variants;
}
```

---

## 🔧 PASO 4: Componentes de Animación

### AnimatePresence Wrapper

```typescript
// filepath: src/components/motion/AnimatedPresence.tsx
"use client";

import { AnimatePresence, motion, HTMLMotionProps } from "framer-motion";
import * as React from "react";
import { fadeVariants, transitions } from "@/lib/motion";

interface AnimatedPresenceProps {
  children: React.ReactNode;
  mode?: "wait" | "sync" | "popLayout";
  initial?: boolean;
}

export function AnimatedPresenceWrapper({
  children,
  mode = "wait",
  initial = false,
}: AnimatedPresenceProps) {
  return (
    <AnimatePresence mode={mode} initial={initial}>
      {children}
    </AnimatePresence>
  );
}

// Page transition wrapper
interface PageTransitionProps extends HTMLMotionProps<"div"> {
  children: React.ReactNode;
}

export function PageTransition({ children, ...props }: PageTransitionProps) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: 20 }}
      transition={transitions.smooth}
      {...props}
    >
      {children}
    </motion.div>
  );
}
```

### FadeIn Component

```typescript
// filepath: src/components/motion/FadeIn.tsx
"use client";

import { motion, HTMLMotionProps, Variants } from "framer-motion";
import * as React from "react";
import { useReducedMotion } from "@/lib/hooks/useReducedMotion";
import { transitions } from "@/lib/motion";

interface FadeInProps extends HTMLMotionProps<"div"> {
  children: React.ReactNode;
  delay?: number;
  direction?: "up" | "down" | "left" | "right" | "none";
  duration?: number;
  className?: string;
}

export function FadeIn({
  children,
  delay = 0,
  direction = "up",
  duration,
  className,
  ...props
}: FadeInProps) {
  const reducedMotion = useReducedMotion();

  const directionOffset = {
    up: { y: 20 },
    down: { y: -20 },
    left: { x: 20 },
    right: { x: -20 },
    none: {},
  };

  const variants: Variants = reducedMotion
    ? {
        hidden: {},
        visible: {},
      }
    : {
        hidden: {
          opacity: 0,
          ...directionOffset[direction],
        },
        visible: {
          opacity: 1,
          x: 0,
          y: 0,
          transition: {
            ...transitions.normal,
            delay,
            ...(duration && { duration }),
          },
        },
      };

  return (
    <motion.div
      initial="hidden"
      whileInView="visible"
      viewport={{ once: true, margin: "-50px" }}
      variants={variants}
      className={className}
      {...props}
    >
      {children}
    </motion.div>
  );
}
```

### Stagger Container

```typescript
// filepath: src/components/motion/Stagger.tsx
"use client";

import { motion, HTMLMotionProps } from "framer-motion";
import * as React from "react";
import { useReducedMotion } from "@/lib/hooks/useReducedMotion";
import { staggerContainerVariants, staggerItemVariants } from "@/lib/motion";

interface StaggerContainerProps extends HTMLMotionProps<"div"> {
  children: React.ReactNode;
  staggerDelay?: number;
  className?: string;
}

export function StaggerContainer({
  children,
  staggerDelay = 0.1,
  className,
  ...props
}: StaggerContainerProps) {
  const reducedMotion = useReducedMotion();

  const containerVariants = reducedMotion
    ? {}
    : {
        ...staggerContainerVariants,
        animate: {
          transition: {
            staggerChildren: staggerDelay,
            delayChildren: 0.1,
          },
        },
      };

  return (
    <motion.div
      initial="initial"
      whileInView="animate"
      viewport={{ once: true, margin: "-50px" }}
      variants={containerVariants}
      className={className}
      {...props}
    >
      {children}
    </motion.div>
  );
}

interface StaggerItemProps extends HTMLMotionProps<"div"> {
  children: React.ReactNode;
  className?: string;
}

export function StaggerItem({
  children,
  className,
  ...props
}: StaggerItemProps) {
  const reducedMotion = useReducedMotion();

  return (
    <motion.div
      variants={reducedMotion ? {} : staggerItemVariants}
      className={className}
      {...props}
    >
      {children}
    </motion.div>
  );
}
```

### Scale on Hover

```typescript
// filepath: src/components/motion/ScaleOnHover.tsx
"use client";

import { motion, HTMLMotionProps } from "framer-motion";
import * as React from "react";
import { useReducedMotion } from "@/lib/hooks/useReducedMotion";

interface ScaleOnHoverProps extends HTMLMotionProps<"div"> {
  children: React.ReactNode;
  scale?: number;
  className?: string;
}

export function ScaleOnHover({
  children,
  scale = 1.02,
  className,
  ...props
}: ScaleOnHoverProps) {
  const reducedMotion = useReducedMotion();

  return (
    <motion.div
      whileHover={reducedMotion ? {} : { scale }}
      whileTap={reducedMotion ? {} : { scale: 0.98 }}
      transition={{ duration: 0.2 }}
      className={className}
      {...props}
    >
      {children}
    </motion.div>
  );
}
```

---

## 🔧 PASO 5: Animaciones de Skeleton

```typescript
// filepath: src/components/motion/AnimatedSkeleton.tsx
"use client";

import { motion } from "framer-motion";
import { cn } from "@/lib/utils";

interface AnimatedSkeletonProps {
  className?: string;
  variant?: "text" | "circular" | "rectangular";
  width?: string | number;
  height?: string | number;
}

export function AnimatedSkeleton({
  className,
  variant = "rectangular",
  width,
  height,
}: AnimatedSkeletonProps) {
  const baseClasses = cn(
    "bg-gray-200 overflow-hidden",
    {
      "rounded": variant === "text",
      "rounded-full": variant === "circular",
      "rounded-lg": variant === "rectangular",
    },
    className
  );

  return (
    <div
      className={baseClasses}
      style={{ width, height }}
    >
      <motion.div
        className="h-full w-full bg-gradient-to-r from-transparent via-white/40 to-transparent"
        animate={{
          x: ["-100%", "100%"],
        }}
        transition={{
          duration: 1.5,
          repeat: Infinity,
          ease: "linear",
        }}
      />
    </div>
  );
}
```

---

## 🔧 PASO 6: Animaciones de Loading

```typescript
// filepath: src/components/motion/LoadingSpinner.tsx
"use client";

import { motion } from "framer-motion";
import { cn } from "@/lib/utils";

interface LoadingSpinnerProps {
  size?: "sm" | "md" | "lg";
  className?: string;
}

export function LoadingSpinner({
  size = "md",
  className,
}: LoadingSpinnerProps) {
  const sizeClasses = {
    sm: "w-4 h-4",
    md: "w-8 h-8",
    lg: "w-12 h-12",
  };

  return (
    <motion.div
      className={cn(
        "border-2 border-gray-200 border-t-primary-600 rounded-full",
        sizeClasses[size],
        className
      )}
      animate={{ rotate: 360 }}
      transition={{
        duration: 1,
        repeat: Infinity,
        ease: "linear",
      }}
    />
  );
}

// Dots loading animation
export function LoadingDots({ className }: { className?: string }) {
  return (
    <div className={cn("flex gap-1", className)}>
      {[0, 1, 2].map((i) => (
        <motion.div
          key={i}
          className="w-2 h-2 bg-primary-600 rounded-full"
          animate={{
            scale: [1, 1.2, 1],
            opacity: [0.5, 1, 0.5],
          }}
          transition={{
            duration: 0.8,
            repeat: Infinity,
            delay: i * 0.2,
          }}
        />
      ))}
    </div>
  );
}

// Pulse animation for buttons
export function LoadingPulse({ className }: { className?: string }) {
  return (
    <motion.div
      className={cn("w-4 h-4 bg-current rounded-full", className)}
      animate={{
        scale: [1, 1.2, 1],
        opacity: [1, 0.5, 1],
      }}
      transition={{
        duration: 1,
        repeat: Infinity,
      }}
    />
  );
}
```

---

## 🔧 PASO 7: Animaciones de Contador

```typescript
// filepath: src/components/motion/AnimatedCounter.tsx
"use client";

import { motion, useSpring, useTransform, useInView } from "framer-motion";
import * as React from "react";
import { useReducedMotion } from "@/lib/hooks/useReducedMotion";

interface AnimatedCounterProps {
  value: number;
  duration?: number;
  formatFn?: (value: number) => string;
  className?: string;
}

export function AnimatedCounter({
  value,
  duration = 2,
  formatFn = (v) => Math.round(v).toLocaleString(),
  className,
}: AnimatedCounterProps) {
  const ref = React.useRef<HTMLSpanElement>(null);
  const isInView = useInView(ref, { once: true });
  const reducedMotion = useReducedMotion();

  const spring = useSpring(0, {
    stiffness: 50,
    damping: 30,
    duration: reducedMotion ? 0 : duration,
  });

  const display = useTransform(spring, (v) => formatFn(v));

  React.useEffect(() => {
    if (isInView) {
      spring.set(value);
    }
  }, [isInView, value, spring]);

  return (
    <motion.span ref={ref} className={className}>
      {display}
    </motion.span>
  );
}
```

---

## 🔧 PASO 8: Ejemplos de Uso

### En VehicleGrid

```typescript
// filepath: src/components/vehicles/VehicleGrid.tsx (ejemplo de uso)
"use client";

import { StaggerContainer, StaggerItem } from "@/components/motion/Stagger";
import { VehicleCard } from "./VehicleCard";

export function VehicleGrid({ vehicles }: { vehicles: Vehicle[] }) {
  return (
    <StaggerContainer className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
      {vehicles.map((vehicle) => (
        <StaggerItem key={vehicle.id}>
          <VehicleCard vehicle={vehicle} />
        </StaggerItem>
      ))}
    </StaggerContainer>
  );
}
```

### En HomePage

```typescript
// filepath: src/components/home/HeroSection.tsx (ejemplo de uso)
"use client";

import { FadeIn } from "@/components/motion/FadeIn";
import { AnimatedCounter } from "@/components/motion/AnimatedCounter";

export function HeroSection() {
  return (
    <section>
      <FadeIn delay={0.1}>
        <h1>Encuentra tu vehículo perfecto</h1>
      </FadeIn>

      <FadeIn delay={0.2} direction="up">
        <p>Miles de vehículos disponibles</p>
      </FadeIn>

      <div className="grid grid-cols-3 gap-8">
        <FadeIn delay={0.3}>
          <AnimatedCounter value={15000} />
          <span>Vehículos</span>
        </FadeIn>
        <FadeIn delay={0.4}>
          <AnimatedCounter value={500} />
          <span>Dealers</span>
        </FadeIn>
        <FadeIn delay={0.5}>
          <AnimatedCounter value={50000} />
          <span>Usuarios</span>
        </FadeIn>
      </div>
    </section>
  );
}
```

### En Cards con Hover

```typescript
// filepath: src/components/vehicles/VehicleCard.tsx (ejemplo de uso)
"use client";

import { ScaleOnHover } from "@/components/motion/ScaleOnHover";

export function VehicleCard({ vehicle }: { vehicle: Vehicle }) {
  return (
    <ScaleOnHover scale={1.02}>
      <article className="bg-white rounded-xl shadow-sm">
        {/* ... contenido */}
      </article>
    </ScaleOnHover>
  );
}
```

---

## ✅ VALIDACIÓN

### Test de Reduced Motion

```typescript
// filepath: __tests__/hooks/useReducedMotion.test.ts
import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderHook } from "@testing-library/react";
import { useReducedMotion } from "@/lib/hooks/useReducedMotion";

describe("useReducedMotion", () => {
  beforeEach(() => {
    // Mock matchMedia
    Object.defineProperty(window, "matchMedia", {
      writable: true,
      value: vi.fn().mockImplementation((query) => ({
        matches: query === "(prefers-reduced-motion: reduce)",
        media: query,
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
      })),
    });
  });

  it("detects reduced motion preference", () => {
    const { result } = renderHook(() => useReducedMotion());
    expect(result.current).toBe(true);
  });
});
```

### Ejecutar

```bash
pnpm test hooks/useReducedMotion

pnpm dev
# Verificar en http://localhost:3000:
# - Animaciones de entrada en homepage
# - Hover effects en cards
# - Transiciones de página suaves
# - Counters animados
# - Sin animaciones con prefers-reduced-motion
```

---

## 📊 RESUMEN

### Componentes creados

| Componente       | Archivo                       | Función                |
| ---------------- | ----------------------------- | ---------------------- |
| FadeIn           | `motion/FadeIn.tsx`           | Fade con dirección     |
| StaggerContainer | `motion/Stagger.tsx`          | Container para stagger |
| StaggerItem      | `motion/Stagger.tsx`          | Item animado           |
| ScaleOnHover     | `motion/ScaleOnHover.tsx`     | Escala en hover        |
| AnimatedSkeleton | `motion/AnimatedSkeleton.tsx` | Skeleton con shimmer   |
| LoadingSpinner   | `motion/LoadingSpinner.tsx`   | Spinner de carga       |
| LoadingDots      | `motion/LoadingSpinner.tsx`   | Dots animados          |
| AnimatedCounter  | `motion/AnimatedCounter.tsx`  | Números animados       |
| PageTransition   | `motion/AnimatedPresence.tsx` | Transición de página   |

### Variantes disponibles

| Variante                   | Uso                |
| -------------------------- | ------------------ |
| `fadeVariants`             | Fade in/out simple |
| `slideUpVariants`          | Slide desde abajo  |
| `slideDownVariants`        | Slide desde arriba |
| `scaleVariants`            | Scale in/out       |
| `modalVariants`            | Modales/dialogs    |
| `dropdownVariants`         | Dropdowns/menus    |
| `staggerContainerVariants` | Contenedor stagger |
| `staggerItemVariants`      | Items stagger      |

### Accesibilidad

- ✅ Hook `useReducedMotion` detecta preferencia
- ✅ Variantes se desactivan automáticamente
- ✅ Sin animations con `prefers-reduced-motion: reduce`

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/02-UX-DESIGN-SYSTEM/06-accesibilidad.md`
