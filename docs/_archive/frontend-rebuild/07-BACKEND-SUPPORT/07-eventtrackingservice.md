# 📊 EventTrackingService - Documentación Frontend

> **Servicio:** EventTrackingService  
> **Puerto:** 5099 (dev) / 8080 (k8s)  
> **Estado:** ✅ Implementado  
> **SDK:** Incluido para frontend  
> **Última actualización:** Enero 2026

---

## 📋 Descripción

Servicio de tracking de eventos para analytics y data science. Captura todas las interacciones del usuario (clics, vistas, búsquedas, conversiones) para alimentar dashboards, recomendaciones y machine learning.

---

## 🎯 Casos de Uso Frontend

### 1. Tracking de Vista de Vehículo

```typescript
// Cuando el usuario ve un vehículo
useEffect(() => {
  trackEvent({
    eventType: "vehicle_view",
    entityType: "Vehicle",
    entityId: vehicleId,
    properties: {
      source: searchParams.get("source") || "direct",
      position: searchParams.get("position") || undefined,
    },
  });
}, [vehicleId]);
```

### 2. Tracking de Búsqueda

```typescript
// Cuando el usuario realiza una búsqueda
const handleSearch = async (filters: SearchFilters) => {
  const results = await vehicleService.search(filters);

  trackEvent({
    eventType: "search",
    properties: {
      filters,
      resultsCount: results.totalCount,
      page: filters.page,
    },
  });

  return results;
};
```

### 3. Tracking de Conversión

```typescript
// Cuando el usuario contacta a un vendedor
const handleContactSeller = async (vehicleId: string, sellerId: string) => {
  trackEvent({
    eventType: "lead_created",
    entityType: "Vehicle",
    entityId: vehicleId,
    properties: {
      sellerId,
      contactMethod: "phone",
    },
  });

  // Abrir WhatsApp o llamada
};
```

---

## 📡 API Endpoints

### Event Ingestion

| Método | Endpoint                  | Descripción                |
| ------ | ------------------------- | -------------------------- |
| `POST` | `/api/events/track`       | Trackear evento individual |
| `POST` | `/api/events/track/batch` | Trackear múltiples eventos |

### Event Queries

| Método | Endpoint                         | Descripción          |
| ------ | -------------------------------- | -------------------- |
| `GET`  | `/api/events/type/{eventType}`   | Eventos por tipo     |
| `GET`  | `/api/events/user/{userId}`      | Eventos de usuario   |
| `GET`  | `/api/events/entity/{type}/{id}` | Eventos de entidad   |
| `GET`  | `/api/events/analytics/summary`  | Resumen de analytics |

---

## 🔧 SDK de Tracking (TypeScript)

```typescript
// lib/tracking.ts

import { apiClient } from "../services/apiClient";

// Tipos de eventos
type EventType =
  | "page_view"
  | "vehicle_view"
  | "vehicle_click"
  | "search"
  | "filter_applied"
  | "favorite_added"
  | "favorite_removed"
  | "compare_added"
  | "compare_removed"
  | "lead_created"
  | "contact_seller"
  | "share"
  | "login"
  | "signup"
  | "listing_created"
  | "listing_published"
  | "checkout_started"
  | "payment_completed";

interface TrackEvent {
  eventType: EventType;
  entityType?: "Vehicle" | "User" | "Dealer" | "Listing";
  entityId?: string;
  properties?: Record<string, any>;
}

interface TrackedEvent extends TrackEvent {
  eventId: string;
  userId?: string;
  sessionId: string;
  timestamp: string;
  deviceInfo: DeviceInfo;
  pageUrl: string;
  referrer?: string;
}

interface DeviceInfo {
  userAgent: string;
  platform: string;
  language: string;
  screenWidth: number;
  screenHeight: number;
  isMobile: boolean;
}

// Generador de Session ID
const getSessionId = (): string => {
  let sessionId = sessionStorage.getItem("okla_session_id");
  if (!sessionId) {
    sessionId = `sess_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    sessionStorage.setItem("okla_session_id", sessionId);
  }
  return sessionId;
};

// Obtener info del dispositivo
const getDeviceInfo = (): DeviceInfo => ({
  userAgent: navigator.userAgent,
  platform: navigator.platform,
  language: navigator.language,
  screenWidth: window.screen.width,
  screenHeight: window.screen.height,
  isMobile: /Mobile|Android|iPhone/i.test(navigator.userAgent),
});

// Buffer para batch
let eventBuffer: TrackEvent[] = [];
let flushTimeout: NodeJS.Timeout | null = null;

const BATCH_SIZE = 10;
const FLUSH_INTERVAL = 5000; // 5 segundos

// Flush del buffer
const flushEvents = async () => {
  if (eventBuffer.length === 0) return;

  const eventsToSend = [...eventBuffer];
  eventBuffer = [];

  try {
    await apiClient.post("/api/events/track/batch", {
      events: eventsToSend.map((event) => ({
        ...event,
        sessionId: getSessionId(),
        timestamp: new Date().toISOString(),
        deviceInfo: getDeviceInfo(),
        pageUrl: window.location.href,
        referrer: document.referrer || undefined,
      })),
    });
  } catch (error) {
    console.error("Failed to flush events:", error);
    // Reintentar agregando de nuevo al buffer
    eventBuffer = [...eventsToSend, ...eventBuffer];
  }
};

// Programar flush
const scheduleFlush = () => {
  if (flushTimeout) clearTimeout(flushTimeout);
  flushTimeout = setTimeout(flushEvents, FLUSH_INTERVAL);
};

/**
 * Trackear un evento
 */
export const trackEvent = (event: TrackEvent): void => {
  eventBuffer.push(event);

  // Flush inmediato si alcanzamos batch size
  if (eventBuffer.length >= BATCH_SIZE) {
    flushEvents();
  } else {
    scheduleFlush();
  }
};

/**
 * Trackear page view automáticamente
 */
export const trackPageView = (pageName?: string): void => {
  trackEvent({
    eventType: "page_view",
    properties: {
      pageName: pageName || document.title,
      path: window.location.pathname,
    },
  });
};

/**
 * Flush eventos antes de cerrar página
 */
if (typeof window !== "undefined") {
  window.addEventListener("beforeunload", () => {
    // Usar sendBeacon para flush síncrono
    if (eventBuffer.length > 0) {
      const payload = JSON.stringify({
        events: eventBuffer.map((event) => ({
          ...event,
          sessionId: getSessionId(),
          timestamp: new Date().toISOString(),
          deviceInfo: getDeviceInfo(),
          pageUrl: window.location.href,
        })),
      });

      navigator.sendBeacon("/api/events/track/batch", payload);
    }
  });
}

// === HELPERS PARA EVENTOS ESPECÍFICOS ===

export const trackVehicleView = (vehicleId: string, source?: string) => {
  trackEvent({
    eventType: "vehicle_view",
    entityType: "Vehicle",
    entityId: vehicleId,
    properties: { source },
  });
};

export const trackVehicleClick = (
  vehicleId: string,
  position: number,
  listType: string,
) => {
  trackEvent({
    eventType: "vehicle_click",
    entityType: "Vehicle",
    entityId: vehicleId,
    properties: { position, listType },
  });
};

export const trackSearch = (
  query: string,
  filters: Record<string, any>,
  resultsCount: number,
) => {
  trackEvent({
    eventType: "search",
    properties: { query, filters, resultsCount },
  });
};

export const trackFavorite = (
  vehicleId: string,
  action: "added" | "removed",
) => {
  trackEvent({
    eventType: action === "added" ? "favorite_added" : "favorite_removed",
    entityType: "Vehicle",
    entityId: vehicleId,
  });
};

export const trackContactSeller = (
  vehicleId: string,
  sellerId: string,
  method: "phone" | "whatsapp" | "message",
) => {
  trackEvent({
    eventType: "lead_created",
    entityType: "Vehicle",
    entityId: vehicleId,
    properties: { sellerId, contactMethod: method },
  });
};

export const trackShare = (
  vehicleId: string,
  platform: "whatsapp" | "facebook" | "twitter" | "copy",
) => {
  trackEvent({
    eventType: "share",
    entityType: "Vehicle",
    entityId: vehicleId,
    properties: { platform },
  });
};
```

---

## 🪝 Hook de React

```typescript
// hooks/useTracking.ts

import { useEffect, useCallback } from "react";
import { useLocation } from "react-router-dom";
import { trackEvent, trackPageView, trackVehicleView } from "../lib/tracking";
import { useAuth } from "./useAuth";

/**
 * Hook para auto-tracking de page views
 */
export function usePageTracking() {
  const location = useLocation();

  useEffect(() => {
    trackPageView();
  }, [location.pathname]);
}

/**
 * Hook para tracking de vehículo
 */
export function useVehicleTracking(
  vehicleId: string | undefined,
  source?: string,
) {
  useEffect(() => {
    if (vehicleId) {
      trackVehicleView(vehicleId, source);
    }
  }, [vehicleId, source]);
}

/**
 * Hook para exponer funciones de tracking
 */
export function useTracking() {
  const { user } = useAuth();

  const track = useCallback(
    (eventType: string, data?: Record<string, any>) => {
      trackEvent({
        eventType: eventType as any,
        properties: {
          ...data,
          userId: user?.id,
        },
      });
    },
    [user?.id],
  );

  return {
    track,
    trackEvent,
    trackVehicleView,
    trackSearch,
    trackFavorite,
    trackContactSeller,
    trackShare,
  };
}
```

---

## 🧩 Componente Provider

```tsx
// components/TrackingProvider.tsx

import { createContext, useContext, useEffect, ReactNode } from "react";
import { useLocation } from "react-router-dom";
import { trackPageView } from "../lib/tracking";

const TrackingContext = createContext({});

export function TrackingProvider({ children }: { children: ReactNode }) {
  const location = useLocation();

  // Auto-track page views
  useEffect(() => {
    trackPageView();
  }, [location.pathname]);

  return (
    <TrackingContext.Provider value={{}}>{children}</TrackingContext.Provider>
  );
}

// Uso en App.tsx:
// <TrackingProvider>
//   <App />
// </TrackingProvider>
```

---

## 📊 Eventos Estándar

### Eventos de Navegación

| Evento      | Descripción     | Properties         |
| ----------- | --------------- | ------------------ |
| `page_view` | Vista de página | `pageName`, `path` |

### Eventos de Vehículos

| Evento           | Descripción                 | Properties                         |
| ---------------- | --------------------------- | ---------------------------------- |
| `vehicle_view`   | Vista detallada de vehículo | `source`                           |
| `vehicle_click`  | Click en card de vehículo   | `position`, `listType`             |
| `search`         | Búsqueda realizada          | `query`, `filters`, `resultsCount` |
| `filter_applied` | Filtro aplicado             | `filterType`, `value`              |

### Eventos de Engagement

| Evento             | Descripción             | Properties |
| ------------------ | ----------------------- | ---------- |
| `favorite_added`   | Agregado a favoritos    | -          |
| `favorite_removed` | Removido de favoritos   | -          |
| `compare_added`    | Agregado a comparación  | -          |
| `compare_removed`  | Removido de comparación | -          |
| `share`            | Compartido              | `platform` |

### Eventos de Conversión

| Evento              | Descripción         | Properties                  |
| ------------------- | ------------------- | --------------------------- |
| `lead_created`      | Lead generado       | `sellerId`, `contactMethod` |
| `contact_seller`    | Contacto a vendedor | `method`                    |
| `checkout_started`  | Checkout iniciado   | `amount`, `plan`            |
| `payment_completed` | Pago completado     | `amount`, `transactionId`   |

### Eventos de Usuario

| Evento              | Descripción       | Properties    |
| ------------------- | ----------------- | ------------- |
| `login`             | Inicio de sesión  | `method`      |
| `signup`            | Registro          | `method`      |
| `listing_created`   | Listing creado    | `vehicleType` |
| `listing_published` | Listing publicado | -             |

---

## 🧪 Testing

### Vitest Mocks

```typescript
// __mocks__/tracking.ts
export const trackEvent = vi.fn();
export const trackPageView = vi.fn();
export const trackVehicleView = vi.fn();
export const trackSearch = vi.fn();
export const trackFavorite = vi.fn();
export const trackContactSeller = vi.fn();
export const trackShare = vi.fn();
```

### Test de Integración

```typescript
// tests/tracking.test.ts
import { trackEvent, trackVehicleView } from "../lib/tracking";

describe("Tracking SDK", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("should track vehicle view", () => {
    trackVehicleView("vehicle-123", "search");

    // Verificar que se agregó al buffer
    // (en un test real verificaríamos el API call)
  });

  it("should include session id", () => {
    trackEvent({ eventType: "page_view" });

    const sessionId = sessionStorage.getItem("okla_session_id");
    expect(sessionId).toBeTruthy();
  });
});
```

### E2E Test (Playwright)

```typescript
// e2e/tracking.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Event Tracking", () => {
  test("should track vehicle view", async ({ page }) => {
    // Interceptar requests de tracking
    const trackingRequests: any[] = [];
    await page.route("/api/events/track/**", (route) => {
      trackingRequests.push(route.request().postDataJSON());
      route.fulfill({ status: 200 });
    });

    await page.goto("/vehicles/toyota-camry-2024");
    await page.waitForTimeout(6000); // Esperar flush

    expect(trackingRequests.length).toBeGreaterThan(0);
    const hasVehicleView = trackingRequests.some((r) =>
      r.events?.some((e: any) => e.eventType === "vehicle_view"),
    );
    expect(hasVehicleView).toBe(true);
  });
});
```

---

## ⚙️ Configuración

### Variables de Entorno

```env
# Tracking settings
VITE_TRACKING_ENABLED=true
VITE_TRACKING_BATCH_SIZE=10
VITE_TRACKING_FLUSH_INTERVAL=5000
```

### Deshabilitar en Desarrollo

```typescript
// lib/tracking.ts
const isTrackingEnabled = () => {
  if (import.meta.env.DEV) return false; // Deshabilitar en dev
  return import.meta.env.VITE_TRACKING_ENABLED !== "false";
};

export const trackEvent = (event: TrackEvent): void => {
  if (!isTrackingEnabled()) {
    console.debug("[Tracking]", event);
    return;
  }
  // ... resto del código
};
```

---

## 📈 Métricas Disponibles

El servicio alimenta dashboards con:

- **Page Views**: Páginas más visitadas
- **Vehicle Views**: Vehículos más vistos
- **Search Patterns**: Términos y filtros más usados
- **Conversion Funnel**: View → Click → Lead → Sale
- **User Journey**: Path de navegación típico
- **Device Distribution**: Desktop vs Mobile

---

## 🔗 Referencias

- [Data ML Strategy](../../docs/DATA_ML_MICROSERVICES_STRATEGY.md)
- [RecommendationService](./07-recommendationservice.md)
- [Analytics Dashboard](../04-PAGINAS/06-ADMIN/)

---

_El tracking es fundamental para ML y personalización. Implementar en todas las páginas._
