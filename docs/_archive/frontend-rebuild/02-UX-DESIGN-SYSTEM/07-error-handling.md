# 🚨 Error Handling & Error Boundaries

> **Tiempo estimado:** 45 minutos
> **Prerrequisitos:** React 18+, Next.js 14+
> **Última actualización:** Enero 2026

---

## 📋 OBJETIVO

Implementar sistema robusto de manejo de errores:

- Error Boundaries por sección
- Fallback UI components
- Retry logic
- Error tracking (Sentry)
- Graceful degradation

---

## 🎯 ESTRATEGIA DE ERRORES

```
┌─────────────────────────────────────────────────────────────────┐
│                    JERARQUÍA DE ERROR BOUNDARIES                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────── App Error Boundary ───────────────────┐   │
│  │  Captura errores globales críticos                        │   │
│  │  Muestra: "Algo salió mal, por favor recarga la página"  │   │
│  │                                                            │   │
│  │  ┌─────────────── Layout Error Boundary ──────────────┐   │   │
│  │  │  Captura errores de navegación/layout               │   │   │
│  │  │  Mantiene: Header, Footer funcionales               │   │   │
│  │  │                                                      │   │   │
│  │  │  ┌─────────── Section Error Boundary ───────────┐   │   │   │
│  │  │  │  Captura errores de secciones específicas     │   │   │   │
│  │  │  │  Ejemplo: Solo galería falla, resto funciona  │   │   │   │
│  │  │  │                                                │   │   │   │
│  │  │  │  ┌───── Component Error Boundary ──────────┐  │   │   │   │
│  │  │  │  │  Captura errores de componentes         │  │   │   │   │
│  │  │  │  │  Ejemplo: Widget de chat falla          │  │   │   │   │
│  │  │  │  └─────────────────────────────────────────┘  │   │   │   │
│  │  │  └────────────────────────────────────────────────┘   │   │   │
│  │  └────────────────────────────────────────────────────────┘   │
│  └────────────────────────────────────────────────────────────────┘
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔧 PASO 1: Error Boundary Base (Class Component)

```typescript
// filepath: src/components/error/ErrorBoundary.tsx
'use client';

import * as React from 'react';
import * as Sentry from '@sentry/nextjs';

interface ErrorBoundaryProps {
  children: React.ReactNode;
  fallback?: React.ReactNode;
  onError?: (error: Error, errorInfo: React.ErrorInfo) => void;
  onReset?: () => void;
  level?: 'app' | 'layout' | 'section' | 'component';
}

interface ErrorBoundaryState {
  hasError: boolean;
  error: Error | null;
  errorInfo: React.ErrorInfo | null;
}

export class ErrorBoundary extends React.Component<
  ErrorBoundaryProps,
  ErrorBoundaryState
> {
  constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = {
      hasError: false,
      error: null,
      errorInfo: null,
    };
  }

  static getDerivedStateFromError(error: Error): Partial<ErrorBoundaryState> {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    // Log a Sentry
    Sentry.withScope((scope) => {
      scope.setTag('errorBoundary', this.props.level || 'unknown');
      scope.setExtra('componentStack', errorInfo.componentStack);
      Sentry.captureException(error);
    });

    // Callback personalizado
    this.props.onError?.(error, errorInfo);

    // Actualizar estado
    this.setState({ errorInfo });

    // Log en desarrollo
    if (process.env.NODE_ENV === 'development') {
      console.error('ErrorBoundary caught:', error, errorInfo);
    }
  }

  handleReset = () => {
    this.setState({
      hasError: false,
      error: null,
      errorInfo: null,
    });
    this.props.onReset?.();
  };

  render() {
    if (this.state.hasError) {
      if (this.props.fallback) {
        return this.props.fallback;
      }

      return (
        <ErrorFallback
          error={this.state.error}
          errorInfo={this.state.errorInfo}
          level={this.props.level || 'section'}
          onReset={this.handleReset}
        />
      );
    }

    return this.props.children;
  }
}
```

---

## 🔧 PASO 2: Fallback Components

```typescript
// filepath: src/components/error/ErrorFallback.tsx
'use client';

import * as React from 'react';
import { AlertTriangle, RefreshCw, Home, Bug, ChevronDown } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { cn } from '@/lib/utils';

interface ErrorFallbackProps {
  error: Error | null;
  errorInfo?: React.ErrorInfo | null;
  level: 'app' | 'layout' | 'section' | 'component';
  onReset?: () => void;
  className?: string;
}

export function ErrorFallback({
  error,
  errorInfo,
  level,
  onReset,
  className,
}: ErrorFallbackProps) {
  const [showDetails, setShowDetails] = React.useState(false);
  const isDev = process.env.NODE_ENV === 'development';

  const config = getErrorConfig(level);

  return (
    <div
      className={cn(
        'flex flex-col items-center justify-center text-center',
        config.containerClass,
        className
      )}
      role="alert"
    >
      {/* Icon */}
      <div className={cn('rounded-full p-4 mb-4', config.iconBgClass)}>
        <AlertTriangle className={cn('h-8 w-8', config.iconClass)} />
      </div>

      {/* Title */}
      <h2 className="text-xl font-semibold text-gray-900 mb-2">
        {config.title}
      </h2>

      {/* Description */}
      <p className="text-gray-600 mb-6 max-w-md">{config.description}</p>

      {/* Actions */}
      <div className="flex flex-wrap gap-3 justify-center">
        {onReset && (
          <Button onClick={onReset} variant="primary" className="gap-2">
            <RefreshCw className="h-4 w-4" />
            Intentar de nuevo
          </Button>
        )}

        {level === 'app' && (
          <Button
            variant="outline"
            onClick={() => (window.location.href = '/')}
            className="gap-2"
          >
            <Home className="h-4 w-4" />
            Ir al inicio
          </Button>
        )}

        <Button
          variant="ghost"
          onClick={() => window.location.reload()}
          className="gap-2"
        >
          <RefreshCw className="h-4 w-4" />
          Recargar página
        </Button>
      </div>

      {/* Error Details (Dev only) */}
      {isDev && error && (
        <div className="mt-6 w-full max-w-2xl">
          <button
            onClick={() => setShowDetails(!showDetails)}
            className="flex items-center gap-2 text-sm text-gray-500 hover:text-gray-700 mx-auto"
          >
            <Bug className="h-4 w-4" />
            Detalles del error
            <ChevronDown
              className={cn(
                'h-4 w-4 transition-transform',
                showDetails && 'rotate-180'
              )}
            />
          </button>

          {showDetails && (
            <div className="mt-4 text-left">
              <div className="bg-red-50 border border-red-200 rounded-lg p-4 overflow-auto">
                <p className="font-mono text-sm text-red-800 font-semibold">
                  {error.name}: {error.message}
                </p>
                {error.stack && (
                  <pre className="mt-2 text-xs text-red-600 whitespace-pre-wrap">
                    {error.stack}
                  </pre>
                )}
                {errorInfo?.componentStack && (
                  <pre className="mt-4 text-xs text-gray-600 whitespace-pre-wrap border-t pt-4">
                    Component Stack:
                    {errorInfo.componentStack}
                  </pre>
                )}
              </div>
            </div>
          )}
        </div>
      )}

      {/* Report Button */}
      <p className="mt-6 text-sm text-gray-500">
        ¿El problema persiste?{' '}
        <a
          href="/ayuda/reportar-problema"
          className="text-primary-600 hover:underline"
        >
          Reportar problema
        </a>
      </p>
    </div>
  );
}

function getErrorConfig(level: 'app' | 'layout' | 'section' | 'component') {
  const configs = {
    app: {
      title: 'Algo salió mal',
      description:
        'Ocurrió un error inesperado. Por favor, recarga la página o vuelve al inicio.',
      containerClass: 'min-h-screen p-8 bg-gray-50',
      iconBgClass: 'bg-red-100',
      iconClass: 'text-red-600',
    },
    layout: {
      title: 'Error al cargar la página',
      description:
        'No pudimos cargar esta sección. Intenta recargar la página.',
      containerClass: 'min-h-[400px] p-8',
      iconBgClass: 'bg-orange-100',
      iconClass: 'text-orange-600',
    },
    section: {
      title: 'Error al cargar esta sección',
      description: 'Esta sección no está disponible temporalmente.',
      containerClass: 'min-h-[200px] p-6 bg-gray-50 rounded-lg',
      iconBgClass: 'bg-yellow-100',
      iconClass: 'text-yellow-600',
    },
    component: {
      title: 'Error al cargar',
      description: 'Este contenido no está disponible.',
      containerClass: 'p-4 bg-gray-50 rounded-lg',
      iconBgClass: 'bg-gray-100',
      iconClass: 'text-gray-600',
    },
  };

  return configs[level];
}
```

---

## 🔧 PASO 3: Error Fallbacks Específicos

### Fallback para Galería de Imágenes

```typescript
// filepath: src/components/error/GalleryErrorFallback.tsx
'use client';

import { ImageOff, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/Button';

interface GalleryErrorFallbackProps {
  onRetry?: () => void;
}

export function GalleryErrorFallback({ onRetry }: GalleryErrorFallbackProps) {
  return (
    <div className="aspect-[4/3] bg-gray-100 rounded-lg flex flex-col items-center justify-center text-gray-500">
      <ImageOff className="h-12 w-12 mb-3" />
      <p className="text-sm mb-3">No se pudieron cargar las imágenes</p>
      {onRetry && (
        <Button variant="outline" size="sm" onClick={onRetry} className="gap-2">
          <RefreshCw className="h-4 w-4" />
          Reintentar
        </Button>
      )}
    </div>
  );
}
```

### Fallback para Mapa

```typescript
// filepath: src/components/error/MapErrorFallback.tsx
'use client';

import { MapPin, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/Button';

interface MapErrorFallbackProps {
  address?: string;
  onRetry?: () => void;
}

export function MapErrorFallback({ address, onRetry }: MapErrorFallbackProps) {
  return (
    <div className="aspect-video bg-gray-100 rounded-lg flex flex-col items-center justify-center text-gray-500 p-4">
      <MapPin className="h-10 w-10 mb-3" />
      <p className="text-sm text-center mb-2">No se pudo cargar el mapa</p>
      {address && (
        <p className="text-xs text-gray-400 text-center mb-3">{address}</p>
      )}
      {onRetry && (
        <Button variant="outline" size="sm" onClick={onRetry} className="gap-2">
          <RefreshCw className="h-4 w-4" />
          Reintentar
        </Button>
      )}
    </div>
  );
}
```

### Fallback para Componentes de Datos

```typescript
// filepath: src/components/error/DataErrorFallback.tsx
'use client';

import { Database, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/Button';

interface DataErrorFallbackProps {
  message?: string;
  onRetry?: () => void;
}

export function DataErrorFallback({
  message = 'No se pudieron cargar los datos',
  onRetry,
}: DataErrorFallbackProps) {
  return (
    <div className="p-6 bg-red-50 border border-red-100 rounded-lg flex flex-col items-center text-center">
      <Database className="h-8 w-8 text-red-400 mb-3" />
      <p className="text-sm text-red-600 mb-3">{message}</p>
      {onRetry && (
        <Button
          variant="outline"
          size="sm"
          onClick={onRetry}
          className="gap-2 border-red-200 text-red-600 hover:bg-red-100"
        >
          <RefreshCw className="h-4 w-4" />
          Reintentar
        </Button>
      )}
    </div>
  );
}
```

---

## 🔧 PASO 4: Next.js Error Files

### App-level error.tsx

```typescript
// filepath: src/app/error.tsx
'use client';

import * as React from 'react';
import { ErrorFallback } from '@/components/error/ErrorFallback';

interface ErrorPageProps {
  error: Error & { digest?: string };
  reset: () => void;
}

export default function Error({ error, reset }: ErrorPageProps) {
  React.useEffect(() => {
    // Log a servicio de errores
    console.error('App Error:', error);
  }, [error]);

  return (
    <ErrorFallback
      error={error}
      level="app"
      onReset={reset}
    />
  );
}
```

### Global-error.tsx (para errores en root layout)

```typescript
// filepath: src/app/global-error.tsx
'use client';

import * as React from 'react';

interface GlobalErrorProps {
  error: Error & { digest?: string };
  reset: () => void;
}

export default function GlobalError({ error, reset }: GlobalErrorProps) {
  return (
    <html lang="es-DO">
      <body>
        <div className="min-h-screen flex flex-col items-center justify-center bg-gray-50 p-8">
          <div className="text-center max-w-md">
            <div className="text-6xl mb-4">😵</div>
            <h1 className="text-2xl font-bold text-gray-900 mb-2">
              Error crítico
            </h1>
            <p className="text-gray-600 mb-6">
              Algo salió muy mal. Por favor, recarga la página.
            </p>
            <div className="space-x-4">
              <button
                onClick={reset}
                className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
              >
                Intentar de nuevo
              </button>
              <button
                onClick={() => (window.location.href = '/')}
                className="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-100"
              >
                Ir al inicio
              </button>
            </div>
            {process.env.NODE_ENV === 'development' && (
              <pre className="mt-6 text-left text-xs bg-red-50 p-4 rounded overflow-auto max-h-40">
                {error.message}
              </pre>
            )}
          </div>
        </div>
      </body>
    </html>
  );
}
```

### Not Found Page

```typescript
// filepath: src/app/not-found.tsx
import Link from 'next/link';
import { Home, Search, ArrowLeft } from 'lucide-react';
import { Button } from '@/components/ui/Button';

export default function NotFound() {
  return (
    <div className="min-h-screen flex flex-col items-center justify-center bg-gray-50 p-8">
      <div className="text-center max-w-md">
        {/* Ilustración */}
        <div className="text-8xl mb-6">🚗💨</div>

        <h1 className="text-3xl font-bold text-gray-900 mb-3">
          Página no encontrada
        </h1>

        <p className="text-gray-600 mb-8">
          Parece que el vehículo que buscas se fue a otra parte.
          La página que intentas visitar no existe o fue movida.
        </p>

        <div className="flex flex-col sm:flex-row gap-3 justify-center">
          <Button asChild variant="primary" className="gap-2">
            <Link href="/">
              <Home className="h-4 w-4" />
              Ir al inicio
            </Link>
          </Button>

          <Button asChild variant="outline" className="gap-2">
            <Link href="/buscar">
              <Search className="h-4 w-4" />
              Buscar vehículos
            </Link>
          </Button>
        </div>

        <div className="mt-8">
          <button
            onClick={() => window.history.back()}
            className="text-sm text-gray-500 hover:text-gray-700 inline-flex items-center gap-1"
          >
            <ArrowLeft className="h-4 w-4" />
            Volver a la página anterior
          </button>
        </div>
      </div>
    </div>
  );
}
```

---

## 🔧 PASO 5: Hook useError

```typescript
// filepath: src/hooks/useError.ts
"use client";

import * as React from "react";
import * as Sentry from "@sentry/nextjs";
import { useUIStore } from "@/stores/useUIStore";

interface UseErrorOptions {
  showToast?: boolean;
  logToSentry?: boolean;
  context?: Record<string, unknown>;
}

export function useError(options: UseErrorOptions = {}) {
  const { showToast = true, logToSentry = true, context = {} } = options;
  const { addToast } = useUIStore();

  const handleError = React.useCallback(
    (error: Error | unknown, customMessage?: string) => {
      const errorObj =
        error instanceof Error ? error : new Error(String(error));

      // Log a consola en desarrollo
      if (process.env.NODE_ENV === "development") {
        console.error("Error handled:", errorObj, context);
      }

      // Log a Sentry
      if (logToSentry) {
        Sentry.withScope((scope) => {
          Object.entries(context).forEach(([key, value]) => {
            scope.setExtra(key, value);
          });
          Sentry.captureException(errorObj);
        });
      }

      // Mostrar toast
      if (showToast) {
        addToast({
          type: "error",
          title: "Error",
          message: customMessage || getErrorMessage(errorObj),
          duration: 5000,
        });
      }

      return errorObj;
    },
    [addToast, logToSentry, showToast, context],
  );

  const handleAsyncError = React.useCallback(
    async <T>(
      promise: Promise<T>,
      customMessage?: string,
    ): Promise<T | null> => {
      try {
        return await promise;
      } catch (error) {
        handleError(error, customMessage);
        return null;
      }
    },
    [handleError],
  );

  return { handleError, handleAsyncError };
}

// Extraer mensaje legible del error
function getErrorMessage(error: Error): string {
  // Mensajes de API conocidos
  if (error.message.includes("401")) {
    return "Tu sesión ha expirado. Por favor, inicia sesión nuevamente.";
  }
  if (error.message.includes("403")) {
    return "No tienes permiso para realizar esta acción.";
  }
  if (error.message.includes("404")) {
    return "El recurso solicitado no fue encontrado.";
  }
  if (error.message.includes("500")) {
    return "Error del servidor. Por favor, intenta más tarde.";
  }
  if (error.message.includes("network") || error.message.includes("fetch")) {
    return "Error de conexión. Verifica tu internet.";
  }

  // Mensaje genérico
  return "Ocurrió un error inesperado. Por favor, intenta de nuevo.";
}
```

---

## 🔧 PASO 6: Wrapper de Sección con Error Boundary

```typescript
// filepath: src/components/error/SectionWrapper.tsx
'use client';

import * as React from 'react';
import { ErrorBoundary } from './ErrorBoundary';
import { ErrorFallback } from './ErrorFallback';
import { Skeleton } from '@/components/ui/Skeleton';

interface SectionWrapperProps {
  children: React.ReactNode;
  fallback?: React.ReactNode;
  loadingFallback?: React.ReactNode;
  isLoading?: boolean;
  sectionName?: string;
  onError?: (error: Error) => void;
}

export function SectionWrapper({
  children,
  fallback,
  loadingFallback,
  isLoading = false,
  sectionName,
  onError,
}: SectionWrapperProps) {
  if (isLoading) {
    return (
      loadingFallback || (
        <div className="space-y-4">
          <Skeleton className="h-8 w-1/3" />
          <Skeleton className="h-40 w-full" />
        </div>
      )
    );
  }

  return (
    <ErrorBoundary
      level="section"
      fallback={
        fallback || (
          <ErrorFallback
            error={null}
            level="section"
            onReset={() => window.location.reload()}
          />
        )
      }
      onError={(error, errorInfo) => {
        console.error(`Error in section ${sectionName}:`, error);
        onError?.(error);
      }}
    >
      {children}
    </ErrorBoundary>
  );
}
```

### Uso en Páginas

```typescript
// filepath: src/app/vehiculos/[slug]/page.tsx
import { SectionWrapper } from '@/components/error/SectionWrapper';
import { VehicleGallery } from '@/components/vehicles/VehicleGallery';
import { VehicleSpecs } from '@/components/vehicles/VehicleSpecs';
import { SimilarVehicles } from '@/components/vehicles/SimilarVehicles';
import { GalleryErrorFallback } from '@/components/error/GalleryErrorFallback';

export default function VehicleDetailPage({ vehicle }) {
  return (
    <div className="container py-8">
      {/* Galería con fallback específico */}
      <SectionWrapper
        sectionName="vehicle-gallery"
        fallback={<GalleryErrorFallback />}
      >
        <VehicleGallery images={vehicle.images} />
      </SectionWrapper>

      {/* Specs con fallback genérico */}
      <SectionWrapper sectionName="vehicle-specs">
        <VehicleSpecs specs={vehicle.specifications} />
      </SectionWrapper>

      {/* Vehículos similares (no crítico) */}
      <SectionWrapper
        sectionName="similar-vehicles"
        fallback={null} // No mostrar nada si falla
      >
        <SimilarVehicles vehicleId={vehicle.id} />
      </SectionWrapper>
    </div>
  );
}
```

---

## 🔧 PASO 7: Configuración de Sentry

```typescript
// filepath: sentry.client.config.ts
import * as Sentry from "@sentry/nextjs";

Sentry.init({
  dsn: process.env.NEXT_PUBLIC_SENTRY_DSN,

  // Ajustar sample rate para producción
  tracesSampleRate: process.env.NODE_ENV === "production" ? 0.1 : 1.0,

  // Replay de sesiones para debugging
  replaysSessionSampleRate: 0.1,
  replaysOnErrorSampleRate: 1.0,

  // Filtrar errores conocidos
  beforeSend(event, hint) {
    const error = hint.originalException as Error;

    // Ignorar errores de red esperados
    if (error?.message?.includes("Failed to fetch")) {
      return null;
    }

    // Ignorar errores de extensiones del navegador
    if (error?.stack?.includes("chrome-extension://")) {
      return null;
    }

    return event;
  },

  // Información adicional
  environment: process.env.NODE_ENV,
  release: process.env.NEXT_PUBLIC_APP_VERSION,
});
```

```typescript
// filepath: sentry.server.config.ts
import * as Sentry from "@sentry/nextjs";

Sentry.init({
  dsn: process.env.SENTRY_DSN,
  tracesSampleRate: process.env.NODE_ENV === "production" ? 0.1 : 1.0,
  environment: process.env.NODE_ENV,
});
```

---

## 🔧 PASO 8: API Error Handling

```typescript
// filepath: src/lib/api/error-handler.ts
import { AxiosError } from "axios";
import * as Sentry from "@sentry/nextjs";

export interface ApiError {
  message: string;
  code: string;
  statusCode: number;
  details?: Record<string, string[]>;
}

export function handleApiError(error: unknown): ApiError {
  // Error de Axios
  if (error instanceof AxiosError) {
    const status = error.response?.status || 500;
    const data = error.response?.data;

    // Log a Sentry para errores 5xx
    if (status >= 500) {
      Sentry.captureException(error, {
        extra: {
          url: error.config?.url,
          method: error.config?.method,
          status,
          data,
        },
      });
    }

    return {
      message: data?.message || getDefaultMessage(status),
      code: data?.code || `HTTP_${status}`,
      statusCode: status,
      details: data?.errors,
    };
  }

  // Error genérico
  if (error instanceof Error) {
    Sentry.captureException(error);
    return {
      message: error.message,
      code: "UNKNOWN_ERROR",
      statusCode: 500,
    };
  }

  // Fallback
  return {
    message: "Error desconocido",
    code: "UNKNOWN_ERROR",
    statusCode: 500,
  };
}

function getDefaultMessage(status: number): string {
  const messages: Record<number, string> = {
    400: "Solicitud inválida",
    401: "No autorizado",
    403: "Acceso denegado",
    404: "No encontrado",
    409: "Conflicto de datos",
    422: "Datos inválidos",
    429: "Demasiadas solicitudes",
    500: "Error del servidor",
    502: "Servicio no disponible",
    503: "Servicio en mantenimiento",
  };
  return messages[status] || "Error desconocido";
}
```

---

## 🧪 Testing

### Vitest

```typescript
// __tests__/components/ErrorBoundary.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ErrorBoundary } from '@/components/error/ErrorBoundary';

// Componente que siempre falla
function BrokenComponent() {
  throw new Error('Test error');
}

describe('ErrorBoundary', () => {
  // Silenciar errores de React en consola durante tests
  beforeEach(() => {
    vi.spyOn(console, 'error').mockImplementation(() => {});
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should render children when no error', () => {
    render(
      <ErrorBoundary>
        <div>Working content</div>
      </ErrorBoundary>
    );
    expect(screen.getByText('Working content')).toBeInTheDocument();
  });

  it('should render fallback when error occurs', () => {
    render(
      <ErrorBoundary>
        <BrokenComponent />
      </ErrorBoundary>
    );
    expect(screen.getByText(/algo salió mal/i)).toBeInTheDocument();
  });

  it('should call onError callback', () => {
    const onError = vi.fn();
    render(
      <ErrorBoundary onError={onError}>
        <BrokenComponent />
      </ErrorBoundary>
    );
    expect(onError).toHaveBeenCalled();
  });

  it('should reset when retry button clicked', async () => {
    const user = userEvent.setup();
    let shouldFail = true;

    function ConditionalComponent() {
      if (shouldFail) throw new Error('Test');
      return <div>Recovered</div>;
    }

    render(
      <ErrorBoundary>
        <ConditionalComponent />
      </ErrorBoundary>
    );

    // Verify error state
    expect(screen.getByText(/intentar de nuevo/i)).toBeInTheDocument();

    // Fix the error
    shouldFail = false;

    // Click retry
    await user.click(screen.getByText(/intentar de nuevo/i));

    // Should recover
    expect(screen.getByText('Recovered')).toBeInTheDocument();
  });
});
```

### Playwright E2E

```typescript
// e2e/error-handling.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Error Handling", () => {
  test("should show 404 page for non-existent routes", async ({ page }) => {
    await page.goto("/ruta-que-no-existe");
    await expect(page.getByText("Página no encontrada")).toBeVisible();
    await expect(
      page.getByRole("link", { name: /ir al inicio/i }),
    ).toBeVisible();
  });

  test("should recover from section errors", async ({ page }) => {
    // Simular error en una sección
    await page.route("**/api/vehicles/similar/*", (route) => {
      route.abort("failed");
    });

    await page.goto("/vehiculos/toyota-camry-2024");

    // La página principal debe cargar
    await expect(page.getByRole("heading", { level: 1 })).toBeVisible();

    // La sección de similares debe mostrar fallback o nada
    // (no debe romper toda la página)
  });

  test("should show error toast for API errors", async ({ page }) => {
    // Simular error 500
    await page.route("**/api/favorites", (route) => {
      route.fulfill({
        status: 500,
        body: JSON.stringify({ message: "Error" }),
      });
    });

    await page.goto("/favoritos");

    // Debe mostrar toast de error
    await expect(page.getByRole("alert")).toContainText(/error/i);
  });
});
```

---

## ✅ Checklist de Implementación

- [ ] Crear ErrorBoundary base
- [ ] Crear ErrorFallback components
- [ ] Crear fallbacks específicos (Gallery, Map, Data)
- [ ] Configurar error.tsx y global-error.tsx
- [ ] Crear not-found.tsx
- [ ] Implementar useError hook
- [ ] Crear SectionWrapper
- [ ] Configurar Sentry
- [ ] Implementar API error handler
- [ ] Escribir tests

---

## 🔗 Referencias

- [React Error Boundaries](https://react.dev/reference/react/Component#catching-rendering-errors-with-an-error-boundary)
- [Next.js Error Handling](https://nextjs.org/docs/app/building-your-application/routing/error-handling)
- [Sentry for Next.js](https://docs.sentry.io/platforms/javascript/guides/nextjs/)

---

_Un buen manejo de errores es invisible para el usuario - las cosas simplemente funcionan o se recuperan graciosamente._
