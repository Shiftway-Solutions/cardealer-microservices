# 🔄 Patrones UX - Estados de Carga, Error y Vacío

> **Tiempo estimado:** 30 minutos
> **Prerrequisitos:** Componentes base creados

---

## 📋 OBJETIVO

Implementar patrones consistentes para:

- Estados de carga (skeletons, spinners)
- Manejo de errores (inline, toast, full page)
- Estados vacíos (con acciones)
- Feedback de acciones (success, error)

---

## 🔧 PASO 1: Sistema de Skeletons

### Skeleton base

```tsx
// filepath: src/components/ui/skeleton.tsx
import * as React from "react";
import { cn } from "@/lib/utils";

interface SkeletonProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: "default" | "circular" | "text" | "rectangular";
  animation?: "pulse" | "shimmer" | "none";
}

function Skeleton({
  className,
  variant = "default",
  animation = "pulse",
  ...props
}: SkeletonProps) {
  const baseClasses = "bg-muted";

  const variantClasses = {
    default: "rounded-md",
    circular: "rounded-full",
    text: "rounded h-4",
    rectangular: "rounded-none",
  };

  const animationClasses = {
    pulse: "animate-pulse",
    shimmer: "skeleton",
    none: "",
  };

  return (
    <div
      className={cn(
        baseClasses,
        variantClasses[variant],
        animationClasses[animation],
        className,
      )}
      {...props}
    />
  );
}

export { Skeleton };
```

### Skeleton para diferentes contextos

```tsx
// filepath: src/components/skeletons/index.tsx
import * as React from "react";
import { Skeleton } from "@/components/ui/skeleton";

// Skeleton para lista de vehículos
export function VehicleGridSkeleton({ count = 6 }: { count?: number }) {
  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {Array.from({ length: count }).map((_, i) => (
        <VehicleCardSkeleton key={i} />
      ))}
    </div>
  );
}

export function VehicleCardSkeleton() {
  return (
    <div className="overflow-hidden rounded-xl border bg-card">
      <Skeleton className="aspect-[4/3] w-full" />
      <div className="space-y-3 p-4">
        <Skeleton className="h-5 w-3/4" />
        <Skeleton className="h-4 w-1/2" />
        <Skeleton className="h-7 w-2/3" />
        <div className="grid grid-cols-2 gap-2">
          <Skeleton className="h-4" />
          <Skeleton className="h-4" />
        </div>
      </div>
    </div>
  );
}

// Skeleton para detalle de vehículo
export function VehicleDetailSkeleton() {
  return (
    <div className="container-page py-8">
      <div className="grid gap-8 lg:grid-cols-2">
        {/* Gallery */}
        <div className="space-y-4">
          <Skeleton className="aspect-[16/9] w-full rounded-xl" />
          <div className="flex gap-2">
            {Array.from({ length: 5 }).map((_, i) => (
              <Skeleton key={i} className="h-16 w-16 rounded-lg" />
            ))}
          </div>
        </div>

        {/* Info */}
        <div className="space-y-6">
          <Skeleton className="h-10 w-3/4" />
          <Skeleton className="h-8 w-1/2" />
          <div className="space-y-3">
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-2/3" />
          </div>
          <Skeleton className="h-12 w-full rounded-lg" />
        </div>
      </div>
    </div>
  );
}

// Skeleton para formularios
export function FormSkeleton({ fields = 4 }: { fields?: number }) {
  return (
    <div className="space-y-6">
      {Array.from({ length: fields }).map((_, i) => (
        <div key={i} className="space-y-2">
          <Skeleton className="h-4 w-24" />
          <Skeleton className="h-10 w-full" />
        </div>
      ))}
      <Skeleton className="h-10 w-32" />
    </div>
  );
}

// Skeleton para tabla
export function TableSkeleton({
  rows = 5,
  cols = 4,
}: {
  rows?: number;
  cols?: number;
}) {
  return (
    <div className="space-y-3">
      {/* Header */}
      <div className="flex gap-4 border-b pb-3">
        {Array.from({ length: cols }).map((_, i) => (
          <Skeleton key={i} className="h-4 flex-1" />
        ))}
      </div>
      {/* Rows */}
      {Array.from({ length: rows }).map((_, i) => (
        <div key={i} className="flex gap-4 py-3">
          {Array.from({ length: cols }).map((_, j) => (
            <Skeleton key={j} className="h-4 flex-1" />
          ))}
        </div>
      ))}
    </div>
  );
}

// Skeleton para stats cards
export function StatsGridSkeleton({ count = 4 }: { count?: number }) {
  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
      {Array.from({ length: count }).map((_, i) => (
        <div key={i} className="rounded-xl border bg-card p-6">
          <Skeleton className="mb-2 h-4 w-24" />
          <Skeleton className="h-8 w-16" />
        </div>
      ))}
    </div>
  );
}

// Skeleton para perfil
export function ProfileSkeleton() {
  return (
    <div className="flex items-center gap-4">
      <Skeleton className="h-16 w-16 rounded-full" />
      <div className="space-y-2">
        <Skeleton className="h-5 w-32" />
        <Skeleton className="h-4 w-24" />
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 2: Sistema de Toast/Notificaciones

### Configurar Sonner

```tsx
// filepath: src/components/ui/toaster.tsx
"use client";

import { Toaster as Sonner } from "sonner";
import { CheckCircle, XCircle, AlertTriangle, Info } from "lucide-react";

type ToasterProps = React.ComponentProps<typeof Sonner>;

const Toaster = ({ ...props }: ToasterProps) => {
  return (
    <Sonner
      className="toaster group"
      toastOptions={{
        classNames: {
          toast:
            "group toast group-[.toaster]:bg-background group-[.toaster]:text-foreground group-[.toaster]:border-border group-[.toaster]:shadow-lg group-[.toaster]:rounded-lg",
          description: "group-[.toast]:text-muted-foreground",
          actionButton:
            "group-[.toast]:bg-primary group-[.toast]:text-primary-foreground",
          cancelButton:
            "group-[.toast]:bg-muted group-[.toast]:text-muted-foreground",
          success:
            "group-[.toaster]:bg-success-50 group-[.toaster]:text-success-900 group-[.toaster]:border-success-200",
          error:
            "group-[.toaster]:bg-danger-50 group-[.toaster]:text-danger-900 group-[.toaster]:border-danger-200",
          warning:
            "group-[.toaster]:bg-warning-50 group-[.toaster]:text-warning-900 group-[.toaster]:border-warning-200",
          info: "group-[.toaster]:bg-info-50 group-[.toaster]:text-info-900 group-[.toaster]:border-info-200",
        },
      }}
      icons={{
        success: <CheckCircle className="h-5 w-5 text-success" />,
        error: <XCircle className="h-5 w-5 text-danger" />,
        warning: <AlertTriangle className="h-5 w-5 text-warning" />,
        info: <Info className="h-5 w-5 text-info" />,
      }}
      position="top-right"
      expand
      richColors
      {...props}
    />
  );
};

export { Toaster };
```

### Hook de toast personalizado

```tsx
// filepath: src/lib/hooks/use-toast.ts
import { toast as sonnerToast } from "sonner";

interface ToastOptions {
  title: string;
  description?: string;
  duration?: number;
  action?: {
    label: string;
    onClick: () => void;
  };
}

export function useToast() {
  const success = ({
    title,
    description,
    duration = 4000,
    action,
  }: ToastOptions) => {
    sonnerToast.success(title, {
      description,
      duration,
      action: action
        ? {
            label: action.label,
            onClick: action.onClick,
          }
        : undefined,
    });
  };

  const error = ({
    title,
    description,
    duration = 5000,
    action,
  }: ToastOptions) => {
    sonnerToast.error(title, {
      description,
      duration,
      action: action
        ? {
            label: action.label,
            onClick: action.onClick,
          }
        : undefined,
    });
  };

  const warning = ({ title, description, duration = 4000 }: ToastOptions) => {
    sonnerToast.warning(title, {
      description,
      duration,
    });
  };

  const info = ({ title, description, duration = 4000 }: ToastOptions) => {
    sonnerToast.info(title, {
      description,
      duration,
    });
  };

  const loading = (title: string) => {
    return sonnerToast.loading(title);
  };

  const dismiss = (id?: string | number) => {
    sonnerToast.dismiss(id);
  };

  const promise = <T,>(
    promise: Promise<T>,
    {
      loading,
      success,
      error,
    }: {
      loading: string;
      success: string | ((data: T) => string);
      error: string | ((err: unknown) => string);
    },
  ) => {
    return sonnerToast.promise(promise, {
      loading,
      success,
      error,
    });
  };

  return {
    success,
    error,
    warning,
    info,
    loading,
    dismiss,
    promise,
  };
}

// Ejemplos de uso:
// const toast = useToast();
//
// toast.success({
//   title: "¡Vehículo guardado!",
//   description: "Se ha agregado a tus favoritos"
// });
//
// toast.error({
//   title: "Error al guardar",
//   description: "Intenta nuevamente",
//   action: {
//     label: "Reintentar",
//     onClick: () => retry()
//   }
// });
//
// toast.promise(saveVehicle(), {
//   loading: "Guardando...",
//   success: "¡Guardado!",
//   error: "Error al guardar"
// });
```

---

## 🔧 PASO 3: Componente Error Boundary

### Error Boundary

```tsx
// filepath: src/components/error-boundary.tsx
"use client";

import * as React from "react";
import { AlertTriangle, RefreshCw, Home } from "lucide-react";
import { Button } from "@/components/ui/button";

interface ErrorBoundaryProps {
  children: React.ReactNode;
  fallback?: React.ReactNode;
}

interface ErrorBoundaryState {
  hasError: boolean;
  error?: Error;
}

export class ErrorBoundary extends React.Component<
  ErrorBoundaryProps,
  ErrorBoundaryState
> {
  constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    // Log to error reporting service
    console.error("Error caught by boundary:", error, errorInfo);
  }

  render() {
    if (this.state.hasError) {
      if (this.props.fallback) {
        return this.props.fallback;
      }

      return (
        <ErrorFallback
          error={this.state.error}
          resetErrorBoundary={() => this.setState({ hasError: false })}
        />
      );
    }

    return this.props.children;
  }
}

// Error fallback component
interface ErrorFallbackProps {
  error?: Error;
  resetErrorBoundary?: () => void;
}

export function ErrorFallback({
  error,
  resetErrorBoundary,
}: ErrorFallbackProps) {
  return (
    <div className="flex min-h-[400px] flex-col items-center justify-center px-4 text-center">
      <div className="mb-4 rounded-full bg-danger-100 p-4">
        <AlertTriangle className="h-8 w-8 text-danger" />
      </div>

      <h2 className="mb-2 text-xl font-semibold text-foreground">
        Algo salió mal
      </h2>

      <p className="mb-6 max-w-md text-sm text-muted-foreground">
        {error?.message ||
          "Ha ocurrido un error inesperado. Por favor, intenta nuevamente."}
      </p>

      <div className="flex gap-3">
        <Button
          onClick={resetErrorBoundary}
          leftIcon={<RefreshCw className="h-4 w-4" />}
        >
          Reintentar
        </Button>
        <Button
          variant="outline"
          onClick={() => (window.location.href = "/")}
          leftIcon={<Home className="h-4 w-4" />}
        >
          Ir al inicio
        </Button>
      </div>
    </div>
  );
}

// Full page error for Next.js error.tsx
export function FullPageError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  React.useEffect(() => {
    // Log to error reporting service
    console.error("Page error:", error);
  }, [error]);

  return (
    <div className="flex min-h-screen flex-col items-center justify-center px-4 text-center">
      <div className="mb-6 rounded-full bg-danger-100 p-6">
        <AlertTriangle className="h-12 w-12 text-danger" />
      </div>

      <h1 className="mb-3 text-3xl font-bold text-foreground">
        ¡Ups! Algo salió mal
      </h1>

      <p className="mb-8 max-w-md text-muted-foreground">
        Lo sentimos, ha ocurrido un error inesperado. Nuestro equipo ha sido
        notificado y estamos trabajando para solucionarlo.
      </p>

      <div className="flex flex-wrap justify-center gap-4">
        <Button
          onClick={reset}
          size="lg"
          leftIcon={<RefreshCw className="h-5 w-5" />}
        >
          Intentar de nuevo
        </Button>
        <Button
          variant="outline"
          size="lg"
          onClick={() => (window.location.href = "/")}
          leftIcon={<Home className="h-5 w-5" />}
        >
          Volver al inicio
        </Button>
      </div>

      {process.env.NODE_ENV === "development" && (
        <details className="mt-8 max-w-xl text-left">
          <summary className="cursor-pointer text-sm text-muted-foreground">
            Detalles del error (solo en desarrollo)
          </summary>
          <pre className="mt-2 overflow-auto rounded-lg bg-muted p-4 text-xs">
            {error.message}
            {error.digest && `\nDigest: ${error.digest}`}
          </pre>
        </details>
      )}
    </div>
  );
}
```

---

## 🔧 PASO 4: Estados vacíos específicos

### Biblioteca de estados vacíos

```tsx
// filepath: src/components/empty-states/index.tsx
import * as React from "react";
import {
  Search,
  Heart,
  Car,
  Bell,
  MessageSquare,
  Filter,
  Plus,
  FileText,
} from "lucide-react";
import { EmptyState } from "@/components/ui/empty-state";
import { useRouter } from "next/navigation";

// Sin resultados de búsqueda
export function NoSearchResults({
  query,
  onClearFilters,
}: {
  query?: string;
  onClearFilters?: () => void;
}) {
  return (
    <EmptyState
      icon={Search}
      title="Sin resultados"
      description={
        query
          ? `No encontramos vehículos que coincidan con "${query}". Intenta con otros términos o ajusta los filtros.`
          : "No encontramos vehículos con los filtros seleccionados."
      }
      action={
        onClearFilters
          ? {
              label: "Limpiar filtros",
              onClick: onClearFilters,
            }
          : undefined
      }
    />
  );
}

// Sin favoritos
export function NoFavorites() {
  const router = useRouter();

  return (
    <EmptyState
      icon={Heart}
      title="Sin favoritos"
      description="Aún no has guardado ningún vehículo. Explora nuestro catálogo y guarda los que te interesen."
      action={{
        label: "Explorar vehículos",
        onClick: () => router.push("/vehiculos"),
      }}
    />
  );
}

// Sin vehículos publicados
export function NoVehiclesPublished() {
  const router = useRouter();

  return (
    <EmptyState
      icon={Car}
      title="Sin vehículos publicados"
      description="No tienes vehículos publicados actualmente. ¡Publica tu primer vehículo y alcanza miles de compradores!"
      action={{
        label: "Publicar vehículo",
        onClick: () => router.push("/vender"),
      }}
    />
  );
}

// Sin alertas
export function NoAlerts() {
  const router = useRouter();

  return (
    <EmptyState
      icon={Bell}
      title="Sin alertas"
      description="No tienes alertas configuradas. Crea alertas para recibir notificaciones cuando haya vehículos que te interesen."
      action={{
        label: "Crear alerta",
        onClick: () => router.push("/alertas/nueva"),
      }}
    />
  );
}

// Sin mensajes
export function NoMessages() {
  const router = useRouter();

  return (
    <EmptyState
      icon={MessageSquare}
      title="Sin mensajes"
      description="No tienes mensajes en tu bandeja. Los mensajes de vendedores y compradores aparecerán aquí."
      action={{
        label: "Explorar vehículos",
        onClick: () => router.push("/vehiculos"),
      }}
    />
  );
}

// Sin datos en tabla
export function NoTableData({
  entityName = "registros",
  onAdd,
  addLabel = "Agregar",
}: {
  entityName?: string;
  onAdd?: () => void;
  addLabel?: string;
}) {
  return (
    <EmptyState
      icon={FileText}
      title={`Sin ${entityName}`}
      description={`No hay ${entityName} para mostrar. Puedes agregar uno nuevo.`}
      action={
        onAdd
          ? {
              label: addLabel,
              onClick: onAdd,
            }
          : undefined
      }
    />
  );
}

// Filtros sin resultados
export function NoFilterResults({
  onClearFilters,
}: {
  onClearFilters: () => void;
}) {
  return (
    <EmptyState
      icon={Filter}
      title="Filtros muy restrictivos"
      description="Los filtros aplicados no devuelven resultados. Intenta ampliar tu búsqueda."
      action={{
        label: "Quitar filtros",
        onClick: onClearFilters,
      }}
    />
  );
}
```

---

## 🔧 PASO 5: Inline Error States

### Componente de error inline

```tsx
// filepath: src/components/ui/error-message.tsx
import * as React from "react";
import { AlertCircle, RefreshCw, XCircle } from "lucide-react";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";

interface ErrorMessageProps {
  message: string;
  variant?: "inline" | "banner" | "card";
  onRetry?: () => void;
  onDismiss?: () => void;
  className?: string;
}

export function ErrorMessage({
  message,
  variant = "inline",
  onRetry,
  onDismiss,
  className,
}: ErrorMessageProps) {
  if (variant === "inline") {
    return (
      <div
        className={cn("flex items-center gap-2 text-sm text-danger", className)}
        role="alert"
      >
        <AlertCircle className="h-4 w-4 shrink-0" />
        <span>{message}</span>
      </div>
    );
  }

  if (variant === "banner") {
    return (
      <div
        className={cn(
          "flex items-center justify-between gap-4 rounded-lg bg-danger-50 px-4 py-3",
          className,
        )}
        role="alert"
      >
        <div className="flex items-center gap-3">
          <AlertCircle className="h-5 w-5 text-danger" />
          <span className="text-sm text-danger-900">{message}</span>
        </div>
        <div className="flex items-center gap-2">
          {onRetry && (
            <Button
              variant="ghost"
              size="sm"
              onClick={onRetry}
              className="text-danger-700 hover:text-danger-900"
            >
              <RefreshCw className="mr-1 h-4 w-4" />
              Reintentar
            </Button>
          )}
          {onDismiss && (
            <Button
              variant="ghost"
              size="icon-sm"
              onClick={onDismiss}
              className="text-danger-700 hover:text-danger-900"
              aria-label="Cerrar"
            >
              <XCircle className="h-4 w-4" />
            </Button>
          )}
        </div>
      </div>
    );
  }

  // Card variant
  return (
    <div
      className={cn(
        "rounded-xl border border-danger-200 bg-danger-50 p-6 text-center",
        className,
      )}
      role="alert"
    >
      <AlertCircle className="mx-auto mb-3 h-8 w-8 text-danger" />
      <p className="mb-4 text-sm text-danger-900">{message}</p>
      {onRetry && (
        <Button
          variant="outline"
          size="sm"
          onClick={onRetry}
          className="border-danger-300 text-danger-700 hover:bg-danger-100"
        >
          <RefreshCw className="mr-2 h-4 w-4" />
          Reintentar
        </Button>
      )}
    </div>
  );
}

// For form field errors
export function FieldError({ message }: { message?: string }) {
  if (!message) return null;

  return (
    <p className="mt-1 text-xs text-danger" role="alert">
      {message}
    </p>
  );
}

// For API errors with multiple messages
export function ApiErrorList({ errors }: { errors: string[] }) {
  if (!errors.length) return null;

  return (
    <div className="rounded-lg bg-danger-50 p-4" role="alert">
      <div className="flex gap-2">
        <AlertCircle className="h-5 w-5 shrink-0 text-danger" />
        <div>
          <p className="font-medium text-danger-900">
            Por favor corrige los siguientes errores:
          </p>
          <ul className="mt-2 list-inside list-disc text-sm text-danger-800">
            {errors.map((error, index) => (
              <li key={index}>{error}</li>
            ))}
          </ul>
        </div>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 6: Confirmar acciones

### Componente de confirmación

```tsx
// filepath: src/components/ui/confirm-dialog.tsx
"use client";

import * as React from "react";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";
import { AlertTriangle, Trash2 } from "lucide-react";

interface ConfirmDialogProps {
  trigger: React.ReactNode;
  title: string;
  description: string;
  confirmLabel?: string;
  cancelLabel?: string;
  variant?: "danger" | "warning" | "default";
  onConfirm: () => void | Promise<void>;
  isLoading?: boolean;
}

export function ConfirmDialog({
  trigger,
  title,
  description,
  confirmLabel = "Confirmar",
  cancelLabel = "Cancelar",
  variant = "default",
  onConfirm,
  isLoading = false,
}: ConfirmDialogProps) {
  const [open, setOpen] = React.useState(false);

  const handleConfirm = async () => {
    await onConfirm();
    setOpen(false);
  };

  const iconColors = {
    danger: "text-danger",
    warning: "text-warning",
    default: "text-primary",
  };

  const confirmButtonVariant = {
    danger: "destructive" as const,
    warning: "warning" as const,
    default: "default" as const,
  };

  return (
    <AlertDialog open={open} onOpenChange={setOpen}>
      <AlertDialogTrigger asChild>{trigger}</AlertDialogTrigger>
      <AlertDialogContent>
        <AlertDialogHeader>
          <div className="flex items-center gap-3">
            <div className={`rounded-full bg-muted p-2 ${iconColors[variant]}`}>
              <AlertTriangle className="h-5 w-5" />
            </div>
            <AlertDialogTitle>{title}</AlertDialogTitle>
          </div>
          <AlertDialogDescription>{description}</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isLoading}>
            {cancelLabel}
          </AlertDialogCancel>
          <AlertDialogAction
            onClick={handleConfirm}
            disabled={isLoading}
            className={
              variant === "danger"
                ? "bg-danger text-white hover:bg-danger-600"
                : undefined
            }
          >
            {isLoading ? "Procesando..." : confirmLabel}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}

// Pre-built delete confirmation
export function DeleteConfirmDialog({
  entityName = "elemento",
  onConfirm,
  isLoading,
}: {
  entityName?: string;
  onConfirm: () => void | Promise<void>;
  isLoading?: boolean;
}) {
  return (
    <ConfirmDialog
      trigger={
        <Button variant="destructive" size="sm">
          <Trash2 className="mr-2 h-4 w-4" />
          Eliminar
        </Button>
      }
      title={`¿Eliminar ${entityName}?`}
      description={`Esta acción no se puede deshacer. ¿Estás seguro de que deseas eliminar este ${entityName}?`}
      confirmLabel="Sí, eliminar"
      variant="danger"
      onConfirm={onConfirm}
      isLoading={isLoading}
    />
  );
}
```

---

## ✅ VALIDACIÓN

### Crear página de prueba

```tsx
// filepath: src/app/ux-test/page.tsx (borrar después)
"use client";

import {
  VehicleGridSkeleton,
  ProfileSkeleton,
  StatsGridSkeleton,
} from "@/components/skeletons";
import { NoSearchResults, NoFavorites } from "@/components/empty-states";
import { ErrorMessage, ApiErrorList } from "@/components/ui/error-message";
import {
  ConfirmDialog,
  DeleteConfirmDialog,
} from "@/components/ui/confirm-dialog";
import { Button } from "@/components/ui/button";
import { useToast } from "@/lib/hooks/use-toast";

export default function UXTestPage() {
  const toast = useToast();

  return (
    <div className="container-page space-y-12 py-8">
      <h1 className="text-display-md">Patrones UX - Test</h1>

      {/* Skeletons */}
      <section>
        <h2 className="mb-4 text-xl font-semibold">Skeletons</h2>
        <VehicleGridSkeleton count={3} />
      </section>

      {/* Empty States */}
      <section>
        <h2 className="mb-4 text-xl font-semibold">Estados Vacíos</h2>
        <div className="grid gap-8 lg:grid-cols-2">
          <NoSearchResults query="Ferrari" onClearFilters={() => {}} />
          <NoFavorites />
        </div>
      </section>

      {/* Error States */}
      <section>
        <h2 className="mb-4 text-xl font-semibold">Errores</h2>
        <div className="space-y-4">
          <ErrorMessage message="Error inline simple" variant="inline" />
          <ErrorMessage
            message="Error con retry"
            variant="banner"
            onRetry={() => {}}
            onDismiss={() => {}}
          />
          <ApiErrorList
            errors={["El email ya existe", "La contraseña es muy corta"]}
          />
        </div>
      </section>

      {/* Toasts */}
      <section>
        <h2 className="mb-4 text-xl font-semibold">Toasts</h2>
        <div className="flex flex-wrap gap-2">
          <Button
            onClick={() =>
              toast.success({
                title: "¡Guardado!",
                description: "El vehículo se guardó correctamente",
              })
            }
          >
            Success Toast
          </Button>
          <Button
            onClick={() =>
              toast.error({ title: "Error", description: "No se pudo guardar" })
            }
          >
            Error Toast
          </Button>
          <Button
            onClick={() =>
              toast.warning({
                title: "Advertencia",
                description: "Verifica los datos",
              })
            }
          >
            Warning Toast
          </Button>
        </div>
      </section>

      {/* Confirm Dialogs */}
      <section>
        <h2 className="mb-4 text-xl font-semibold">Diálogos de Confirmación</h2>
        <div className="flex gap-4">
          <DeleteConfirmDialog
            entityName="vehículo"
            onConfirm={async () => {
              await new Promise((r) => setTimeout(r, 1000));
              toast.success({ title: "Eliminado" });
            }}
          />
        </div>
      </section>
    </div>
  );
}
```

### Verificar

```bash
pnpm dev
# Visitar http://localhost:3000/ux-test
# Verificar que todos los patrones funcionan

# Limpiar
rm src/app/ux-test/page.tsx
```

---

## 📊 RESUMEN

| Patrón       | Componentes   | Uso                   |
| ------------ | ------------- | --------------------- |
| Skeletons    | 6 variantes   | Carga de contenido    |
| Empty States | 7 variantes   | Sin datos             |
| Toasts       | useToast hook | Feedback de acciones  |
| Errors       | 4 variantes   | Manejo de errores     |
| Confirmación | ConfirmDialog | Acciones destructivas |

---

## ➡️ SIGUIENTE PASO

Continuar con: `docs/frontend-rebuild/02-UX-DESIGN-SYSTEM/05-animaciones.md`
