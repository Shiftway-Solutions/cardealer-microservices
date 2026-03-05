# 🧩 Componentes Base UI

> **Tiempo estimado:** 45 minutos
> **Prerrequisitos:** Design tokens configurados, shadcn/ui instalado
> **Tema visual:** CarGurus USA - Verde esmeralda (#00A870) como primario

---

## 📋 OBJETIVO

Crear componentes base reutilizables con:

- **Tema CarGurus:** Verde primario, UI limpia, sombras sutiles
- Accesibilidad completa (WCAG 2.1 AA)
- Variants y sizes configurables
- Animaciones sutiles
- TypeScript estricto

> 📖 **Referencia:** Ver [00-TEMA-CARGURUS-AUDITORIA.md](./00-TEMA-CARGURUS-AUDITORIA.md) para guía de diseño

---

## 🔧 PASO 1: Instalar shadcn/ui

### Comando

```bash
# Inicializar shadcn/ui
pnpm dlx shadcn@latest init

# Cuando pregunte, seleccionar:
# - Style: Default
# - Base color: Slate
# - CSS variables: Yes
```

### Instalar componentes necesarios

```bash
# Instalar componentes base
pnpm dlx shadcn@latest add button
pnpm dlx shadcn@latest add input
pnpm dlx shadcn@latest add label
pnpm dlx shadcn@latest add card
pnpm dlx shadcn@latest add badge
pnpm dlx shadcn@latest add skeleton
pnpm dlx shadcn@latest add dialog
pnpm dlx shadcn@latest add dropdown-menu
pnpm dlx shadcn@latest add select
pnpm dlx shadcn@latest add toast
pnpm dlx shadcn@latest add tooltip
pnpm dlx shadcn@latest add avatar
pnpm dlx shadcn@latest add separator
pnpm dlx shadcn@latest add sheet
pnpm dlx shadcn@latest add tabs
pnpm dlx shadcn@latest add accordion
```

---

## 🔧 PASO 2: Extender Button con variants OKLA (Tema CarGurus)

### Código a crear

```tsx
// filepath: src/components/ui/button.tsx
import * as React from "react";
import { Slot } from "@radix-ui/react-slot";
import { cva, type VariantProps } from "class-variance-authority";
import { Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";

/**
 * Button variants - Tema CarGurus
 *
 * Primary: Verde esmeralda (#00A870) - CTAs principales
 * Secondary: Outline gris - Acciones secundarias
 * Ghost: Transparente con hover verde suave
 */
const buttonVariants = cva(
  "inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-lg text-sm font-semibold transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 [&_svg]:pointer-events-none [&_svg]:size-4 [&_svg]:shrink-0",
  {
    variants: {
      variant: {
        // ⭐ Verde CarGurus - CTA principal
        default:
          "bg-primary text-white shadow-button-primary hover:bg-primary-600 active:bg-primary-700 active:scale-[0.98]",
        // Rojo para acciones destructivas
        destructive:
          "bg-danger text-white shadow-sm hover:bg-danger-600 active:scale-[0.98]",
        // Outline gris - Acciones secundarias
        outline:
          "border border-gray-300 bg-white text-secondary-700 shadow-sm hover:bg-gray-50 hover:border-gray-400 active:scale-[0.98]",
        // Secondary sutil
        secondary:
          "bg-gray-100 text-secondary-700 hover:bg-gray-200 active:scale-[0.98]",
        // Ghost - hover verde suave
        ghost: "text-secondary-600 hover:bg-primary-50 hover:text-primary-600",
        // Link verde
        link: "text-primary underline-offset-4 hover:underline",
        // Success verde claro
        success:
          "bg-success text-white shadow-button-success hover:bg-success-600 active:scale-[0.98]",
        // Warning naranja
        warning:
          "bg-warning text-black shadow-sm hover:bg-warning-600 active:scale-[0.98]",
      },
      size: {
        default: "h-10 px-5 py-2",
        sm: "h-8 rounded-md px-3 text-xs",
        lg: "h-12 rounded-lg px-6 text-base",
        xl: "h-14 rounded-xl px-8 text-lg",
        icon: "h-10 w-10",
        "icon-sm": "h-8 w-8",
        "icon-lg": "h-12 w-12",
      },
      fullWidth: {
        true: "w-full",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  },
);

export interface ButtonProps
  extends
    React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean;
  isLoading?: boolean;
  loadingText?: string;
  leftIcon?: React.ReactNode;
  rightIcon?: React.ReactNode;
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  (
    {
      className,
      variant,
      size,
      fullWidth,
      asChild = false,
      isLoading = false,
      loadingText,
      leftIcon,
      rightIcon,
      children,
      disabled,
      ...props
    },
    ref,
  ) => {
    const Comp = asChild ? Slot : "button";

    return (
      <Comp
        className={cn(buttonVariants({ variant, size, fullWidth, className }))}
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
          <>
            {leftIcon}
            {children}
            {rightIcon}
          </>
        )}
      </Comp>
    );
  },
);
Button.displayName = "Button";

export { Button, buttonVariants };
```

---

## 🔧 PASO 3: Crear Input mejorado

### Código a crear

```tsx
// filepath: src/components/ui/input.tsx
import * as React from "react";
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "@/lib/utils";

const inputVariants = cva(
  "flex w-full rounded-lg border bg-background text-sm transition-colors file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50",
  {
    variants: {
      size: {
        sm: "h-8 px-3 text-xs",
        default: "h-10 px-3 py-2",
        lg: "h-12 px-4 py-3 text-base",
      },
      state: {
        default: "border-input",
        error: "border-danger focus-visible:ring-danger",
        success: "border-success focus-visible:ring-success",
      },
    },
    defaultVariants: {
      size: "default",
      state: "default",
    },
  },
);

export interface InputProps
  extends
    Omit<React.InputHTMLAttributes<HTMLInputElement>, "size">,
    VariantProps<typeof inputVariants> {
  leftElement?: React.ReactNode;
  rightElement?: React.ReactNode;
  error?: string;
}

const Input = React.forwardRef<HTMLInputElement, InputProps>(
  (
    {
      className,
      type,
      size,
      state,
      leftElement,
      rightElement,
      error,
      ...props
    },
    ref,
  ) => {
    const inputState = error ? "error" : state;

    if (leftElement || rightElement) {
      return (
        <div className="relative">
          {leftElement && (
            <div className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground">
              {leftElement}
            </div>
          )}
          <input
            type={type}
            className={cn(
              inputVariants({ size, state: inputState }),
              leftElement && "pl-10",
              rightElement && "pr-10",
              className,
            )}
            ref={ref}
            {...props}
          />
          {rightElement && (
            <div className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground">
              {rightElement}
            </div>
          )}
        </div>
      );
    }

    return (
      <input
        type={type}
        className={cn(inputVariants({ size, state: inputState, className }))}
        ref={ref}
        {...props}
      />
    );
  },
);
Input.displayName = "Input";

export { Input, inputVariants };
```

---

## 🔧 PASO 4: Crear FormField con Label y Error

### Código a crear

```tsx
// filepath: src/components/ui/form-field.tsx
import * as React from "react";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";

interface FormFieldProps {
  id: string;
  label: string;
  error?: string;
  hint?: string;
  required?: boolean;
  children: React.ReactNode;
  className?: string;
}

export function FormField({
  id,
  label,
  error,
  hint,
  required,
  children,
  className,
}: FormFieldProps) {
  return (
    <div className={cn("space-y-2", className)}>
      <Label
        htmlFor={id}
        className={cn("text-sm font-medium", error && "text-danger")}
      >
        {label}
        {required && <span className="ml-1 text-danger">*</span>}
      </Label>
      {children}
      {error ? (
        <p className="text-xs text-danger" id={`${id}-error`} role="alert">
          {error}
        </p>
      ) : hint ? (
        <p className="text-xs text-muted-foreground" id={`${id}-hint`}>
          {hint}
        </p>
      ) : null}
    </div>
  );
}
```

---

## 🔧 PASO 5: Crear VehicleCard

### Código a crear

```tsx
// filepath: src/components/vehicles/VehicleCard.tsx
"use client";

import * as React from "react";
import Link from "next/link";
import Image from "next/image";
import { Heart, MapPin, Gauge, Calendar, Fuel } from "lucide-react";
import { cn, formatCurrency, formatNumber } from "@/lib/utils";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import type { VehicleSummary } from "@/types";

interface VehicleCardProps {
  vehicle: VehicleSummary;
  onFavoriteClick?: (vehicleId: string) => void;
  isFavorited?: boolean;
  className?: string;
  priority?: boolean;
}

export function VehicleCard({
  vehicle,
  onFavoriteClick,
  isFavorited = false,
  className,
  priority = false,
}: VehicleCardProps) {
  const {
    id,
    slug,
    title,
    price,
    year,
    make,
    model,
    mileage,
    city,
    condition,
    primaryImage,
    sellerType,
    isVerified,
  } = vehicle;

  return (
    <article
      className={cn(
        "group relative overflow-hidden rounded-xl border bg-card shadow-card transition-all duration-300",
        "hover:-translate-y-1 hover:shadow-card-lg",
        className,
      )}
    >
      {/* Image Container */}
      <Link
        href={`/vehiculos/${slug}`}
        className="relative block aspect-[4/3] overflow-hidden"
      >
        <Image
          src={primaryImage || "/images/placeholder-vehicle.jpg"}
          alt={title}
          fill
          sizes="(max-width: 640px) 100vw, (max-width: 1024px) 50vw, 33vw"
          className="object-cover transition-transform duration-300 group-hover:scale-105"
          priority={priority}
        />

        {/* Condition Badge */}
        <div className="absolute left-3 top-3 flex gap-2">
          {condition === "new" && (
            <Badge className="bg-primary text-white">Nuevo</Badge>
          )}
          {condition === "certified" && (
            <Badge className="bg-success text-white">Certificado</Badge>
          )}
          {sellerType === "dealer" && isVerified && (
            <Badge variant="secondary" className="bg-white/90">
              <span className="text-success">✓</span> Verificado
            </Badge>
          )}
        </div>

        {/* Gradient overlay */}
        <div className="absolute inset-x-0 bottom-0 h-20 bg-gradient-to-t from-black/50 to-transparent" />
      </Link>

      {/* Favorite Button */}
      <Button
        variant="ghost"
        size="icon"
        className={cn(
          "absolute right-3 top-3 z-10 bg-white/80 backdrop-blur-sm",
          "hover:bg-white hover:text-danger",
          isFavorited && "text-danger",
        )}
        onClick={(e) => {
          e.preventDefault();
          onFavoriteClick?.(id);
        }}
        aria-label={isFavorited ? "Quitar de favoritos" : "Agregar a favoritos"}
      >
        <Heart className={cn("h-5 w-5", isFavorited && "fill-current")} />
      </Button>

      {/* Content */}
      <div className="p-4">
        {/* Title */}
        <Link href={`/vehiculos/${slug}`}>
          <h3 className="line-clamp-1 font-semibold text-foreground group-hover:text-primary">
            {year} {make} {model}
          </h3>
        </Link>

        <p className="mt-1 line-clamp-1 text-sm text-muted-foreground">
          {title}
        </p>

        {/* Price */}
        <div className="mt-3">
          <span className="price-display text-price-md text-primary">
            <span className="price-currency">RD$</span>
            {formatNumber(price)}
          </span>
        </div>

        {/* Specs Grid */}
        <div className="mt-4 grid grid-cols-2 gap-2 text-sm text-muted-foreground">
          <div className="flex items-center gap-1.5">
            <Calendar className="h-4 w-4" />
            <span>{year}</span>
          </div>
          <div className="flex items-center gap-1.5">
            <Gauge className="h-4 w-4" />
            <span>{formatNumber(mileage)} km</span>
          </div>
          <div className="col-span-2 flex items-center gap-1.5">
            <MapPin className="h-4 w-4" />
            <span className="truncate">{city}</span>
          </div>
        </div>
      </div>
    </article>
  );
}

// Skeleton version for loading
export function VehicleCardSkeleton() {
  return (
    <div className="overflow-hidden rounded-xl border bg-card shadow-card">
      <div className="aspect-[4/3] animate-pulse bg-muted" />
      <div className="p-4 space-y-3">
        <div className="h-5 w-3/4 animate-pulse rounded bg-muted" />
        <div className="h-4 w-1/2 animate-pulse rounded bg-muted" />
        <div className="h-7 w-2/3 animate-pulse rounded bg-muted" />
        <div className="grid grid-cols-2 gap-2">
          <div className="h-4 animate-pulse rounded bg-muted" />
          <div className="h-4 animate-pulse rounded bg-muted" />
        </div>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 6: Crear PriceDisplay

### Código a crear

```tsx
// filepath: src/components/ui/price-display.tsx
import * as React from "react";
import { cn, formatNumber } from "@/lib/utils";

interface PriceDisplayProps {
  price: number;
  originalPrice?: number;
  size?: "sm" | "md" | "lg" | "xl";
  showDiscount?: boolean;
  className?: string;
}

const sizeClasses = {
  sm: "text-base",
  md: "text-xl",
  lg: "text-2xl",
  xl: "text-3xl",
};

export function PriceDisplay({
  price,
  originalPrice,
  size = "md",
  showDiscount = true,
  className,
}: PriceDisplayProps) {
  const hasDiscount = originalPrice && originalPrice > price;
  const discountPercent = hasDiscount
    ? Math.round(((originalPrice - price) / originalPrice) * 100)
    : 0;

  return (
    <div className={cn("flex flex-wrap items-baseline gap-2", className)}>
      {/* Main Price */}
      <span
        className={cn(
          "price-display font-bold text-primary",
          sizeClasses[size],
        )}
      >
        <span className="text-[0.6em] font-semibold">RD$</span>
        {formatNumber(price)}
      </span>

      {/* Original Price */}
      {hasDiscount && (
        <span className="text-sm text-muted-foreground line-through">
          RD${formatNumber(originalPrice)}
        </span>
      )}

      {/* Discount Badge */}
      {hasDiscount && showDiscount && (
        <span className="rounded-full bg-danger/10 px-2 py-0.5 text-xs font-medium text-danger">
          -{discountPercent}%
        </span>
      )}
    </div>
  );
}

// Compact version for lists
export function PriceDisplayCompact({
  price,
  className,
}: {
  price: number;
  className?: string;
}) {
  return (
    <span
      className={cn(
        "price-display tabular-nums font-semibold text-primary",
        className,
      )}
    >
      RD${formatNumber(price)}
    </span>
  );
}
```

---

## 🔧 PASO 7: Crear EmptyState

### Código a crear

```tsx
// filepath: src/components/ui/empty-state.tsx
import * as React from "react";
import { LucideIcon } from "lucide-react";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";

interface EmptyStateProps {
  icon: LucideIcon;
  title: string;
  description: string;
  action?: {
    label: string;
    onClick: () => void;
  };
  secondaryAction?: {
    label: string;
    onClick: () => void;
  };
  className?: string;
}

export function EmptyState({
  icon: Icon,
  title,
  description,
  action,
  secondaryAction,
  className,
}: EmptyStateProps) {
  return (
    <div
      className={cn(
        "flex flex-col items-center justify-center px-4 py-12 text-center",
        className,
      )}
    >
      <div className="mb-4 rounded-full bg-muted p-4">
        <Icon className="h-8 w-8 text-muted-foreground" />
      </div>

      <h3 className="mb-2 text-lg font-semibold text-foreground">{title}</h3>

      <p className="mb-6 max-w-md text-sm text-muted-foreground">
        {description}
      </p>

      {(action || secondaryAction) && (
        <div className="flex flex-wrap justify-center gap-3">
          {action && <Button onClick={action.onClick}>{action.label}</Button>}
          {secondaryAction && (
            <Button variant="outline" onClick={secondaryAction.onClick}>
              {secondaryAction.label}
            </Button>
          )}
        </div>
      )}
    </div>
  );
}
```

---

## 🔧 PASO 8: Crear LoadingSpinner

### Código a crear

```tsx
// filepath: src/components/ui/loading.tsx
import * as React from "react";
import { Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";

interface LoadingSpinnerProps {
  size?: "sm" | "md" | "lg";
  className?: string;
}

const sizeClasses = {
  sm: "h-4 w-4",
  md: "h-6 w-6",
  lg: "h-8 w-8",
};

export function LoadingSpinner({
  size = "md",
  className,
}: LoadingSpinnerProps) {
  return (
    <Loader2
      className={cn("animate-spin text-primary", sizeClasses[size], className)}
      aria-label="Cargando..."
    />
  );
}

// Full page loader
export function PageLoader() {
  return (
    <div className="flex min-h-[50vh] items-center justify-center">
      <div className="flex flex-col items-center gap-4">
        <LoadingSpinner size="lg" />
        <p className="text-sm text-muted-foreground">Cargando...</p>
      </div>
    </div>
  );
}

// Inline loader
export function InlineLoader({ text = "Cargando..." }: { text?: string }) {
  return (
    <div className="flex items-center gap-2 text-sm text-muted-foreground">
      <LoadingSpinner size="sm" />
      <span>{text}</span>
    </div>
  );
}

// Button loading state
export function ButtonLoader() {
  return <Loader2 className="h-4 w-4 animate-spin" />;
}
```

---

## 🔧 PASO 9: Test para componentes

### Código a crear

```tsx
// filepath: __tests__/components/Button.test.tsx
import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Button } from "@/components/ui/button";
import { Plus } from "lucide-react";

describe("Button", () => {
  it("renders children correctly", () => {
    render(<Button>Click me</Button>);
    expect(
      screen.getByRole("button", { name: /click me/i }),
    ).toBeInTheDocument();
  });

  it("handles click events", async () => {
    const user = userEvent.setup();
    const handleClick = vi.fn();

    render(<Button onClick={handleClick}>Click me</Button>);
    await user.click(screen.getByRole("button"));

    expect(handleClick).toHaveBeenCalledTimes(1);
  });

  it("shows loading state", () => {
    render(<Button isLoading>Submit</Button>);

    expect(screen.getByRole("button")).toBeDisabled();
    // Loader icon should be visible
  });

  it("renders with left icon", () => {
    render(<Button leftIcon={<Plus data-testid="plus-icon" />}>Add</Button>);

    expect(screen.getByTestId("plus-icon")).toBeInTheDocument();
  });

  it("applies variant classes", () => {
    const { rerender } = render(<Button variant="destructive">Delete</Button>);
    expect(screen.getByRole("button")).toHaveClass("bg-danger");

    rerender(<Button variant="success">Confirm</Button>);
    expect(screen.getByRole("button")).toHaveClass("bg-success");
  });

  it("renders as full width when specified", () => {
    render(<Button fullWidth>Full Width</Button>);
    expect(screen.getByRole("button")).toHaveClass("w-full");
  });

  it("is disabled when disabled prop is true", () => {
    render(<Button disabled>Disabled</Button>);
    expect(screen.getByRole("button")).toBeDisabled();
  });
});
```

```tsx
// filepath: __tests__/components/VehicleCard.test.tsx
import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import {
  VehicleCard,
  VehicleCardSkeleton,
} from "@/components/vehicles/VehicleCard";
import type { VehicleSummary } from "@/types";

const mockVehicle: VehicleSummary = {
  id: "1",
  slug: "toyota-camry-2024",
  title: "Toyota Camry SE 2024",
  price: 1850000,
  year: 2024,
  make: "Toyota",
  model: "Camry",
  mileage: 15000,
  city: "Santo Domingo",
  condition: "used",
  primaryImage: "/images/camry.jpg",
  sellerType: "dealer",
  isVerified: true,
};

describe("VehicleCard", () => {
  it("renders vehicle information correctly", () => {
    render(<VehicleCard vehicle={mockVehicle} />);

    expect(screen.getByText(/2024 Toyota Camry/i)).toBeInTheDocument();
    expect(screen.getByText(/1,850,000/)).toBeInTheDocument();
    expect(screen.getByText(/15,000 km/)).toBeInTheDocument();
    expect(screen.getByText(/Santo Domingo/)).toBeInTheDocument();
  });

  it("shows verified badge for verified dealers", () => {
    render(<VehicleCard vehicle={mockVehicle} />);

    expect(screen.getByText(/Verificado/)).toBeInTheDocument();
  });

  it("shows new badge for new vehicles", () => {
    render(<VehicleCard vehicle={{ ...mockVehicle, condition: "new" }} />);

    expect(screen.getByText("Nuevo")).toBeInTheDocument();
  });

  it("handles favorite click", async () => {
    const user = userEvent.setup();
    const handleFavorite = vi.fn();

    render(
      <VehicleCard vehicle={mockVehicle} onFavoriteClick={handleFavorite} />,
    );

    await user.click(
      screen.getByRole("button", { name: /agregar a favoritos/i }),
    );
    expect(handleFavorite).toHaveBeenCalledWith("1");
  });

  it("shows filled heart when favorited", () => {
    render(<VehicleCard vehicle={mockVehicle} isFavorited />);

    expect(
      screen.getByRole("button", { name: /quitar de favoritos/i }),
    ).toBeInTheDocument();
  });

  it("links to vehicle detail page", () => {
    render(<VehicleCard vehicle={mockVehicle} />);

    const links = screen.getAllByRole("link");
    expect(links[0]).toHaveAttribute("href", "/vehiculos/toyota-camry-2024");
  });
});

describe("VehicleCardSkeleton", () => {
  it("renders loading skeleton", () => {
    render(<VehicleCardSkeleton />);

    // Should have animated skeleton elements
    const skeletons = document.querySelectorAll(".animate-pulse");
    expect(skeletons.length).toBeGreaterThan(0);
  });
});
```

---

## ✅ VALIDACIÓN

### Ejecutar tests

```bash
# Ejecutar todos los tests de componentes
pnpm test __tests__/components/

# Output esperado:
# ✓ __tests__/components/Button.test.tsx (7)
# ✓ __tests__/components/VehicleCard.test.tsx (7)
#
# Test Files  2 passed (2)
# Tests  14 passed (14)
```

### Verificar build

```bash
pnpm build
# No debería haber errores de TypeScript
```

---

## 🔧 PASO 8: Crear DealRatingBadge (Distintivo CarGurus)

### Descripción

El **DealRatingBadge** es el componente distintivo que diferencia a CarGurus de otros marketplaces. Muestra una calificación visual del precio del vehículo comparado con el mercado.

### Código a crear

```tsx
// filepath: src/components/vehicles/DealRatingBadge.tsx
"use client";

import * as React from "react";
import { cn } from "@/lib/utils";
import { DEAL_RATING, type DealRatingType } from "@/lib/design-tokens";
import {
  Flame,
  ThumbsUp,
  Minus,
  AlertTriangle,
  XCircle,
  HelpCircle,
} from "lucide-react";

interface DealRatingBadgeProps {
  rating: DealRatingType;
  size?: "sm" | "md" | "lg";
  showIcon?: boolean;
  showLabel?: boolean;
  className?: string;
}

const iconMap = {
  great: Flame,
  good: ThumbsUp,
  fair: Minus,
  high: AlertTriangle,
  overpriced: XCircle,
  none: HelpCircle,
} as const;

const sizeClasses = {
  sm: "text-[10px] px-1.5 py-0.5 gap-0.5",
  md: "text-xs px-2 py-1 gap-1",
  lg: "text-sm px-3 py-1.5 gap-1.5",
} as const;

const iconSizes = {
  sm: "h-3 w-3",
  md: "h-3.5 w-3.5",
  lg: "h-4 w-4",
} as const;

export function DealRatingBadge({
  rating,
  size = "md",
  showIcon = true,
  showLabel = true,
  className,
}: DealRatingBadgeProps) {
  const config = DEAL_RATING[rating];
  const Icon = iconMap[rating];

  return (
    <span
      className={cn(
        "inline-flex items-center font-semibold uppercase rounded shadow-sm",
        sizeClasses[size],
        className,
      )}
      style={{
        backgroundColor: config.bgColor,
        color: config.color,
      }}
      title={config.label}
    >
      {showIcon && <Icon className={iconSizes[size]} />}
      {showLabel && <span>{config.label}</span>}
    </span>
  );
}

// Variante para mostrar ahorro estimado
interface DealRatingWithSavingsProps extends DealRatingBadgeProps {
  savings?: number;
  marketPrice?: number;
}

export function DealRatingWithSavings({
  rating,
  savings,
  marketPrice,
  size = "md",
  className,
}: DealRatingWithSavingsProps) {
  const config = DEAL_RATING[rating];

  if (!savings && !marketPrice) {
    return (
      <DealRatingBadge rating={rating} size={size} className={className} />
    );
  }

  const savingsText = savings
    ? `RD$ ${savings.toLocaleString("es-DO")} bajo mercado`
    : marketPrice
      ? `Mercado: RD$ ${marketPrice.toLocaleString("es-DO")}`
      : null;

  return (
    <div className={cn("flex flex-col gap-0.5", className)}>
      <DealRatingBadge rating={rating} size={size} />
      {savingsText && (
        <span className="text-xs font-medium" style={{ color: config.bgColor }}>
          ↓ {savingsText}
        </span>
      )}
    </div>
  );
}
```

### Uso del componente

```tsx
// Ejemplo de uso en VehicleCard
import { DealRatingBadge, DealRatingWithSavings } from '@/components/vehicles/DealRatingBadge';

// Badge simple
<DealRatingBadge rating="great" />
<DealRatingBadge rating="good" size="lg" />
<DealRatingBadge rating="fair" showIcon={false} />

// Badge con ahorro
<DealRatingWithSavings
  rating="great"
  savings={85000}
  size="md"
/>

// En VehicleCard
<div className="absolute left-3 top-3">
  <DealRatingBadge rating={vehicle.dealRating} />
</div>
```

### Tests E2E para DealRatingBadge

```typescript
// filepath: e2e/components/deal-rating-badge.spec.ts
import { test, expect } from "@playwright/test";

test.describe("DealRatingBadge", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/storybook/deal-rating-badge");
  });

  test("muestra todos los ratings correctamente", async ({ page }) => {
    // Great Deal - Verde
    const greatBadge = page.locator('[data-rating="great"]');
    await expect(greatBadge).toBeVisible();
    await expect(greatBadge).toHaveCSS("background-color", "rgb(0, 168, 112)");

    // Good Deal - Verde lima
    const goodBadge = page.locator('[data-rating="good"]');
    await expect(goodBadge).toHaveCSS("background-color", "rgb(124, 179, 66)");

    // Fair Deal - Naranja
    const fairBadge = page.locator('[data-rating="fair"]');
    await expect(fairBadge).toHaveCSS("background-color", "rgb(255, 167, 38)");

    // High Price - Rojo
    const highBadge = page.locator('[data-rating="high"]');
    await expect(highBadge).toHaveCSS("background-color", "rgb(239, 83, 80)");
  });

  test("muestra iconos según el rating", async ({ page }) => {
    const greatBadge = page.locator('[data-rating="great"]');
    await expect(greatBadge.locator("svg")).toBeVisible();
  });

  test("oculta label cuando showLabel=false", async ({ page }) => {
    const iconOnlyBadge = page.locator('[data-testid="icon-only-badge"]');
    await expect(iconOnlyBadge.locator("svg")).toBeVisible();
    await expect(iconOnlyBadge).not.toContainText("Excelente");
  });
});
```

---

## 🎨 GUÍA VISUAL - Tema CarGurus

### Paleta de colores de referencia

| Elemento    | Color    | Hex       | Uso                                |
| ----------- | -------- | --------- | ---------------------------------- |
| Primary CTA | Verde    | `#00A870` | Botones principales, links activos |
| Headlines   | Navy     | `#1A1A2E` | Títulos, texto importante          |
| Body text   | Gray 700 | `#616161` | Texto de párrafos                  |
| Muted text  | Gray 500 | `#9E9E9E` | Texto secundario, placeholders     |
| Background  | Gray 50  | `#FAFAFA` | Fondo de página                    |
| Card bg     | White    | `#FFFFFF` | Fondo de cards                     |
| Border      | Gray 200 | `#EEEEEE` | Bordes de cards e inputs           |

### Jerarquía de botones

```
┌─────────────────────────────────────────────────────────┐
│  PRIORIDAD ALTA                                        │
│  ┌─────────────────────┐                               │
│  │   Contactar Vendedor │  ← Verde #00A870 (default)   │
│  └─────────────────────┘                               │
│                                                         │
│  PRIORIDAD MEDIA                                       │
│  ┌─────────────────────┐                               │
│  │   Guardar Búsqueda  │  ← Outline gris (outline)     │
│  └─────────────────────┘                               │
│                                                         │
│  PRIORIDAD BAJA                                        │
│  ┌─────────────────────┐                               │
│  │   Ver más detalles  │  ← Ghost verde (ghost)        │
│  └─────────────────────┘                               │
└─────────────────────────────────────────────────────────┘
```

### Cards de vehículos

```
┌─────────────────────────────────────┐
│  ┌───────────────────────────────┐  │
│  │        IMAGEN 4:3             │  │
│  │                               │  │
│  │ ┌─────────────────┐           │  │
│  │ │ EXCELENTE PRECIO│   ← Badge │  │
│  │ └─────────────────┘           │  │
│  └───────────────────────────────┘  │
│                                     │
│  2024 Toyota Camry SE               │  ← Navy #1A1A2E
│  32,450 km · Santo Domingo          │  ← Gray #616161
│                                     │
│  RD$ 1,250,000                      │  ← Verde #00A870 (bold)
│  ↓ RD$ 50,000 bajo mercado          │  ← Verde #00A870 (small)
│                                     │
└─────────────────────────────────────┘
    ↑
    Border: #EEEEEE, Shadow: card-sm
    Hover: shadow-card-hover, translateY(-4px)
```

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/02-UX-DESIGN-SYSTEM/04-patrones-ux.md`

---

## 📊 RESUMEN

| Componente       | Ubicación                  | Propósito                           |
| ---------------- | -------------------------- | ----------------------------------- |
| `Button`         | `ui/button.tsx`            | Botón con variants, loading, iconos |
| `Input`          | `ui/input.tsx`             | Input con estados, iconos           |
| `FormField`      | `ui/form-field.tsx`        | Wrapper con label/error             |
| `VehicleCard`    | `vehicles/VehicleCard.tsx` | Card de vehículo para grids         |
| `PriceDisplay`   | `ui/price-display.tsx`     | Formateo de precios RD$             |
| `EmptyState`     | `ui/empty-state.tsx`       | Estado vacío con acción             |
| `LoadingSpinner` | `ui/loading.tsx`           | Indicadores de carga                |

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/02-UX-DESIGN-SYSTEM/04-patrones-ux.md`
