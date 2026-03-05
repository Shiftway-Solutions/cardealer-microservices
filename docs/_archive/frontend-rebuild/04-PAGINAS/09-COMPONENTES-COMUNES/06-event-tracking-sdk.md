---
title: "39. Event Tracking SDK - Integración Frontend"
priority: P0
estimated_time: "3 horas"
dependencies: []
apis: []
status: complete
last_updated: "2026-01-30"
---

# 39. Event Tracking SDK - Integración Frontend

> **Objetivo:** Implementar y configurar el SDK de EventTrackingService para captura automática de eventos de usuario  
> **Tiempo estimado:** 3 horas  
> **Prioridad:** P1 (Crítico - Base para analytics, recomendaciones y lead scoring)  
> **Complejidad:** 🟡 Media (Instalación SDK, configuración, eventos custom)  
> **Dependencias:** EventTrackingService (Puerto 5050), AnalyticsService, RecommendationService

---

## ✅ INTEGRACIÓN CON EVENTTRACKINGSERVICE

Este documento complementa:

- [12-admin-dashboard.md](../06-ADMIN/01-admin-dashboard.md) - Dashboard con analytics
- [28-dealer-analytics-completo.md](../05-DEALER/04-dealer-analytics.md) - Analytics de dealers
- [process-matrix/09-REPORTES-ANALYTICS/03-event-tracking.md](../../process-matrix/09-REPORTES-ANALYTICS/03-event-tracking.md) - **Procesos detallados** ⭐

**Estado:** ✅ Backend 100% | 🔴 SDK no instalado en frontend

---

## 📋 TABLA DE CONTENIDOS

1. [Arquitectura del SDK](#arquitectura-del-sdk)
2. [Instalación](#instalación)
3. [Configuración](#configuración)
4. [Eventos Automáticos](#eventos-automáticos)
5. [Eventos Custom](#eventos-custom)
6. [Identificación de Usuarios](#identificación-de-usuarios)
7. [Best Practices](#best-practices)
8. [Debugging](#debugging)

---

## 🏗️ ARQUITECTURA DEL SDK

### Flujo de Captura de Eventos

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    Event Tracking SDK Flow                               │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│   Browser/App                                                           │
│   ┌─────────────────────────────────────────────────────────────┐      │
│   │                                                               │      │
│   │   User Action → okla.track()                                 │      │
│   │                       │                                       │      │
│   │                       ▼                                       │      │
│   │   ┌──────────────────────────────────────────────┐          │      │
│   │   │          OKLA SDK (JavaScript)               │          │      │
│   │   │                                              │          │      │
│   │   │  • Event Queue (LocalStorage)                │          │      │
│   │   │  • Batch Sender (every 5s or 10 events)      │          │      │
│   │   │  • Retry Logic (exponential backoff)         │          │      │
│   │   │  • Auto-tracking (clicks, page views)        │          │      │
│   │   └──────────────────────┬───────────────────────┘          │      │
│   │                          │                                   │      │
│   │                          │ HTTP POST /api/events/batch       │      │
│   │                          ▼                                   │      │
│   └─────────────────────────────────────────────────────────────┘      │
│                              │                                           │
│                              ▼                                           │
│   ┌─────────────────────────────────────────────────────────────┐      │
│   │       EventTrackingService (Port 5050)                      │      │
│   │                                                               │      │
│   │   EventsController.BatchCreate()                            │      │
│   │         │                                                    │      │
│   │         ├─> Validate events                                 │      │
│   │         ├─> Enrich with IP, UserAgent, Timestamp            │      │
│   │         ├─> Publish to Kafka topic: "raw-events"            │      │
│   │         └─> Return 202 Accepted                             │      │
│   └─────────────────────────────────────────────────────────────┘      │
│                              │                                           │
│              ┌───────────────┼───────────────┐                          │
│              ▼               ▼               ▼                          │
│   ┌────────────────┐  ┌────────────┐  ┌────────────┐                  │
│   │     Kafka      │  │   Redis    │  │  RabbitMQ  │                  │
│   │  (Raw Events)  │  │ (Real-time)│  │ (Consumers)│                  │
│   └────────────────┘  └────────────┘  └────────────┘                  │
│              │               │               │                          │
│              └───────────────┼───────────────┘                          │
│                              │                                           │
│              ┌───────────────┼───────────────┐                          │
│              ▼               ▼               ▼                          │
│   ┌────────────────┐  ┌────────────┐  ┌────────────┐                  │
│   │  ClickHouse    │  │ Analytics  │  │    Lead    │                  │
│   │   (Storage)    │  │  Service   │  │  Scoring   │                  │
│   └────────────────┘  └────────────┘  └────────────┘                  │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 📦 INSTALACIÓN

### PASO 1: Instalar vía Script Tag (Recomendado)

```html
<!-- filepath: frontend/web/index.html -->
<!DOCTYPE html>
<html lang="es">
  <head>
    <!-- ... otros meta tags ... -->

    <!-- OKLA Event Tracking SDK -->
    <script>
      !(function () {
        var o = (window.okla = window.okla || []);
        if (!o.initialize) {
          o.invoked = !0;
          o.methods = ["identify", "track", "page", "reset"];
          o.factory = function (t) {
            return function () {
              var e = Array.prototype.slice.call(arguments);
              e.unshift(t);
              o.push(e);
              return o;
            };
          };
          for (var t = 0; t < o.methods.length; t++) {
            var e = o.methods[t];
            o[e] = o.factory(e);
          }
          o.load = function (t, e) {
            var n = document.createElement("script");
            n.type = "text/javascript";
            n.async = !0;
            n.src =
              (e || "https://cdn.okla.com.do") + "/sdk/v1/okla.min.js";
            var r = document.getElementsByTagName("script")[0];
            r.parentNode.insertBefore(n, r);
            o.WRITE_KEY = t;
          };
          o.load(import.meta.env.VITE_OKLA_WRITE_KEY);
        }
      })();
    </script>
  </head>
  <body>
    <div id="root"></div>
    <script type="module" src="/src/main.tsx"></script>
  </body>
</html>
```

### PASO 2: Variables de Entorno

```bash
# filepath: frontend/web/.env.local
VITE_OKLA_WRITE_KEY=pk_live_1234567890abcdef
VITE_OKLA_API_URL=https://api.okla.com.do
VITE_OKLA_DEBUG=false
```

```bash
# filepath: frontend/web/.env.development
VITE_OKLA_WRITE_KEY=pk_test_dev1234567890
VITE_OKLA_API_URL=http://localhost:5050
VITE_OKLA_DEBUG=true
```

---

## ⚙️ CONFIGURACIÓN

### PASO 3: Wrapper TypeScript

```typescript
// filepath: src/lib/analytics/okla.ts
interface OklaSDK {
  identify: (userId: string, traits?: Record<string, any>) => void;
  track: (
    event: string,
    properties?: Record<string, any>,
    callback?: () => void,
  ) => void;
  page: (name?: string, properties?: Record<string, any>) => void;
  reset: () => void;
  ready: (callback: () => void) => void;
  debug: (enabled?: boolean) => void;
}

declare global {
  interface Window {
    okla: OklaSDK;
  }
}

export const analytics = {
  /**
   * Identificar usuario (al hacer login)
   */
  identify(userId: string, traits?: Record<string, any>) {
    if (typeof window !== "undefined" && window.okla) {
      window.okla.identify(userId, {
        ...traits,
        timestamp: new Date().toISOString(),
      });
    }
  },

  /**
   * Track evento custom
   */
  track(
    event: string,
    properties?: Record<string, any>,
    callback?: () => void,
  ) {
    if (typeof window !== "undefined" && window.okla) {
      window.okla.track(
        event,
        {
          ...properties,
          timestamp: new Date().toISOString(),
          url: window.location.href,
          referrer: document.referrer,
        },
        callback,
      );
    }
  },

  /**
   * Track page view (automático en navegación)
   */
  page(name?: string, properties?: Record<string, any>) {
    if (typeof window !== "undefined" && window.okla) {
      window.okla.page(name, {
        ...properties,
        timestamp: new Date().toISOString(),
        url: window.location.href,
        path: window.location.pathname,
        title: document.title,
      });
    }
  },

  /**
   * Reset al hacer logout
   */
  reset() {
    if (typeof window !== "undefined" && window.okla) {
      window.okla.reset();
    }
  },

  /**
   * Callback cuando SDK está listo
   */
  ready(callback: () => void) {
    if (typeof window !== "undefined" && window.okla) {
      window.okla.ready(callback);
    }
  },

  /**
   * Habilitar debug mode
   */
  debug(enabled = true) {
    if (typeof window !== "undefined" && window.okla) {
      window.okla.debug(enabled);
    }
  },
};

// Habilitar debug en desarrollo
if (import.meta.env.DEV) {
  analytics.debug(true);
}

export default analytics;
```

---

## 🤖 EVENTOS AUTOMÁTICOS

### PASO 4: Tracking Automático en App.tsx

```typescript
// filepath: src/App.tsx
import { useEffect } from "react";
import { useLocation } from "react-router-dom";
import { useAuth } from "@/lib/hooks/useAuth";
import analytics from "@/lib/analytics/okla";

export default function App() {
  const location = useLocation();
  const { user } = useAuth();

  // Track page views automáticamente
  useEffect(() => {
    analytics.page();
  }, [location.pathname]);

  // Identificar usuario al hacer login
  useEffect(() => {
    if (user) {
      analytics.identify(user.id, {
        email: user.email,
        name: user.name,
        role: user.role,
        createdAt: user.createdAt,
        accountType: user.accountType,
      });
    } else {
      analytics.reset();
    }
  }, [user]);

  return (
    <Routes>
      {/* ... rutas ... */}
    </Routes>
  );
}
```

### PASO 5: Tracking de Scroll y Tiempo en Página

```typescript
// filepath: src/components/tracking/PageTracker.tsx
import { useEffect, useRef } from "react";
import analytics from "@/lib/analytics/okla";

export function PageTracker() {
  const startTimeRef = useRef(Date.now());
  const maxScrollRef = useRef(0);

  useEffect(() => {
    const handleScroll = () => {
      const scrollPercentage =
        (window.scrollY / (document.body.scrollHeight - window.innerHeight)) *
        100;
      maxScrollRef.current = Math.max(maxScrollRef.current, scrollPercentage);
    };

    const handleBeforeUnload = () => {
      const timeOnPage = Date.now() - startTimeRef.current;
      analytics.track("page_leave", {
        duration: timeOnPage,
        scrollDepth: Math.round(maxScrollRef.current),
      });
    };

    window.addEventListener("scroll", handleScroll);
    window.addEventListener("beforeunload", handleBeforeUnload);

    return () => {
      window.removeEventListener("scroll", handleScroll);
      window.removeEventListener("beforeunload", handleBeforeUnload);
    };
  }, []);

  return null;
}
```

---

## 🎯 EVENTOS CUSTOM

### Eventos de Vehículos

```typescript
// filepath: src/components/vehicles/VehicleCard.tsx
import analytics from "@/lib/analytics/okla";

export function VehicleCard({ vehicle, position }: VehicleCardProps) {
  const handleClick = () => {
    analytics.track("vehicle_click", {
      vehicleId: vehicle.id,
      vehicleTitle: vehicle.title,
      price: vehicle.price,
      position: position,
      source: "search_results",
    });
  };

  const handleFavorite = () => {
    analytics.track("vehicle_favorite", {
      vehicleId: vehicle.id,
      vehicleTitle: vehicle.title,
    });
  };

  return (
    <div>
      <Link to={`/vehicles/${vehicle.slug}`} onClick={handleClick}>
        {/* ... */}
      </Link>
      <button onClick={handleFavorite}>❤️</button>
    </div>
  );
}
```

### Eventos de Búsqueda

```typescript
// filepath: src/pages/SearchPage.tsx
import { useEffect } from "react";
import analytics from "@/lib/analytics/okla";

export function SearchPage() {
  const { filters, results } = useSearch();

  useEffect(() => {
    if (results) {
      analytics.track("search", {
        query: filters.query,
        filters: {
          make: filters.make,
          model: filters.model,
          yearFrom: filters.yearFrom,
          yearTo: filters.yearTo,
          priceFrom: filters.priceFrom,
          priceTo: filters.priceTo,
        },
        resultsCount: results.totalCount,
      });
    }
  }, [filters, results]);

  const handleFilterApply = (filterType: string, value: any) => {
    analytics.track("search_filter_apply", {
      filterType,
      value,
    });
  };

  return <div>{/* ... */}</div>;
}
```

### Eventos de Lead/Contacto

```typescript
// filepath: src/components/vehicles/ContactDealerForm.tsx
import { useState } from "react";
import analytics from "@/lib/analytics/okla";

export function ContactDealerForm({ vehicle, dealer }: ContactFormProps) {
  const [formStarted, setFormStarted] = useState(false);

  const handleFormStart = () => {
    if (!formStarted) {
      setFormStarted(true);
      analytics.track("lead_form_start", {
        vehicleId: vehicle.id,
        dealerId: dealer.id,
      });
    }
  };

  const handleFieldChange = (fieldName: string) => {
    analytics.track("lead_form_field", {
      vehicleId: vehicle.id,
      fieldName,
    });
  };

  const handleSubmit = async (data: ContactFormData) => {
    analytics.track("lead_form_submit", {
      vehicleId: vehicle.id,
      dealerId: dealer.id,
      leadType: data.inquiryType,
    });

    // ... submit logic
  };

  const handleAbandon = () => {
    analytics.track("lead_form_abandon", {
      vehicleId: vehicle.id,
      lastField: currentField,
    });
  };

  useEffect(() => {
    return () => {
      if (formStarted && !submitted) {
        handleAbandon();
      }
    };
  }, [formStarted, submitted]);

  return (
    <form onSubmit={handleSubmit}>
      <input onFocus={handleFormStart} onChange={() => handleFieldChange("name")} />
      {/* ... */}
    </form>
  );
}
```

### Eventos de Usuario

```typescript
// filepath: src/pages/auth/RegisterPage.tsx
import analytics from "@/lib/analytics/okla";

export function RegisterPage() {
  const handleRegisterStart = () => {
    analytics.track("signup_start", {
      source: "register_page",
    });
  };

  const handleRegisterComplete = (userId: string, method: string) => {
    analytics.track("signup_complete", {
      userId,
      method, // "email", "google", "facebook"
    });

    // Identificar usuario inmediatamente
    analytics.identify(userId, {
      createdAt: new Date().toISOString(),
      signupMethod: method,
    });
  };

  return <div>{/* ... */}</div>;
}
```

### Eventos de Dealer

```typescript
// filepath: src/pages/dealer/CreateVehiclePage.tsx
import analytics from "@/lib/analytics/okla";

export function CreateVehiclePage() {
  const handleVehicleCreate = (vehicleId: string) => {
    analytics.track("vehicle_create", {
      vehicleId,
    });
  };

  const handleVehiclePublish = (vehicleId: string) => {
    analytics.track("vehicle_publish", {
      vehicleId,
    });
  };

  return <div>{/* ... */}</div>;
}
```

---

## 👤 IDENTIFICACIÓN DE USUARIOS

### PASO 6: Identificación en Login

```typescript
// filepath: src/lib/hooks/useAuth.ts
import { useEffect } from "react";
import analytics from "@/lib/analytics/okla";

export function useAuth() {
  const { user, login, logout } = useAuthContext();

  const handleLogin = async (credentials: LoginCredentials) => {
    const user = await login(credentials);

    // Identificar usuario
    analytics.identify(user.id, {
      email: user.email,
      name: user.name,
      role: user.role,
      accountType: user.accountType,
      createdAt: user.createdAt,
    });

    analytics.track("login", {
      userId: user.id,
      method: "email",
    });

    return user;
  };

  const handleLogout = () => {
    analytics.track("logout", {
      userId: user?.id,
    });

    // Reset tracking
    analytics.reset();

    logout();
  };

  return { user, handleLogin, handleLogout };
}
```

### PASO 7: Identificación de Usuarios Anónimos

```typescript
// filepath: src/lib/analytics/anonymousId.ts
import { v4 as uuidv4 } from "uuid";

const ANONYMOUS_ID_KEY = "okla_anonymous_id";

export function getAnonymousId(): string {
  if (typeof window === "undefined") return "";

  let anonymousId = localStorage.getItem(ANONYMOUS_ID_KEY);

  if (!anonymousId) {
    anonymousId = uuidv4();
    localStorage.setItem(ANONYMOUS_ID_KEY, anonymousId);
  }

  return anonymousId;
}

export function clearAnonymousId() {
  if (typeof window !== "undefined") {
    localStorage.removeItem(ANONYMOUS_ID_KEY);
  }
}

// Identificar usuario anónimo al cargar la app
export function identifyAnonymousUser() {
  const anonymousId = getAnonymousId();
  analytics.identify(anonymousId, {
    isAnonymous: true,
    createdAt: new Date().toISOString(),
  });
}
```

---

## ✅ BEST PRACTICES

### 1. Nomenclatura de Eventos

```typescript
// ✅ BUENO: snake_case, descriptivo
analytics.track("vehicle_view", { vehicleId: "123" });
analytics.track("lead_form_submit", { vehicleId: "123" });
analytics.track("search_filter_apply", { filterType: "make" });

// ❌ MALO: camelCase, poco claro
analytics.track("vehicleView", { id: "123" });
analytics.track("submitForm", { v: "123" });
analytics.track("filter", { type: "make" });
```

### 2. Propiedades Consistentes

```typescript
// ✅ BUENO: Siempre incluir IDs relevantes
analytics.track("vehicle_view", {
  vehicleId: vehicle.id,
  vehicleTitle: vehicle.title,
  price: vehicle.price,
  dealerId: vehicle.dealerId,
  source: "search_results",
  position: 3,
});

// ❌ MALO: Propiedades inconsistentes
analytics.track("vehicle_view", {
  id: vehicle.id, // ¿qué ID?
});
```

### 3. Timing de Eventos

```typescript
// ✅ BUENO: Track al completar acción
const handleSubmit = async () => {
  const result = await submitForm(data);
  if (result.success) {
    analytics.track("lead_form_submit", { vehicleId });
  }
};

// ❌ MALO: Track antes de confirmar
const handleSubmit = async () => {
  analytics.track("lead_form_submit", { vehicleId }); // ¿Y si falla?
  await submitForm(data);
};
```

### 4. Evitar PII (Personal Identifiable Information)

```typescript
// ✅ BUENO: Solo IDs y datos no sensibles
analytics.track("profile_update", {
  userId: user.id,
  fieldsUpdated: ["name", "phone"],
});

// ❌ MALO: Datos personales
analytics.track("profile_update", {
  email: "juan@email.com", // ❌
  phone: "+1-809-555-1234", // ❌
  ssn: "123-45-6789", // ❌ ¡NUNCA!
});
```

### 5. Batching y Performance

```typescript
// El SDK automáticamente hace batching cada 5 segundos o 10 eventos
// No es necesario hacer batching manual

// ✅ BUENO: Track eventos individualmente
vehicles.forEach((vehicle, index) => {
  analytics.track("vehicle_impression", {
    vehicleId: vehicle.id,
    position: index,
  });
});
```

---

## 🐛 DEBUGGING

### PASO 8: Debug Mode

```typescript
// Habilitar debug en consola
if (import.meta.env.DEV) {
  analytics.debug(true);
}

// Console output:
// [OKLA] identify: user-123 { email: "...", name: "..." }
// [OKLA] track: vehicle_view { vehicleId: "abc-123", ... }
// [OKLA] batch sent: 5 events
```

### PASO 9: Inspector de Eventos (DevTools)

```typescript
// filepath: src/components/debug/EventInspector.tsx
import { useEffect, useState } from "react";

export function EventInspector() {
  const [events, setEvents] = useState<any[]>([]);

  useEffect(() => {
    if (import.meta.env.DEV) {
      // Intercept okla.track calls
      const originalTrack = window.okla.track;
      window.okla.track = function (event, properties, callback) {
        setEvents((prev) => [
          ...prev,
          { event, properties, timestamp: new Date() },
        ]);
        return originalTrack.call(this, event, properties, callback);
      };
    }
  }, []);

  if (!import.meta.env.DEV) return null;

  return (
    <div className="fixed bottom-4 right-4 bg-black text-white p-4 rounded-lg max-w-md max-h-96 overflow-auto">
      <h3 className="font-bold mb-2">Event Inspector</h3>
      {events.map((evt, i) => (
        <div key={i} className="mb-2 text-xs">
          <strong>{evt.event}</strong>
          <pre>{JSON.stringify(evt.properties, null, 2)}</pre>
        </div>
      ))}
    </div>
  );
}
```

### PASO 10: Network Inspector

```bash
# Verificar requests en Network tab
POST https://api.okla.com.do/api/events/batch

# Request payload:
{
  "events": [
    {
      "event": "vehicle_view",
      "properties": {
        "vehicleId": "abc-123",
        "source": "search_results"
      },
      "userId": "user-123",
      "anonymousId": "anon-456",
      "timestamp": "2026-01-29T10:30:00Z"
    }
  ]
}

# Response:
{
  "accepted": 1,
  "rejected": 0
}
```

---

## 📊 MÉTRICAS Y MONITOREO

### Eventos Críticos a Monitorear

| Evento             | KPI                   | Meta           |
| ------------------ | --------------------- | -------------- |
| `vehicle_view`     | Views por vehículo    | 100+ por mes   |
| `lead_form_submit` | Tasa de conversión    | > 2%           |
| `search`           | Búsquedas por usuario | 3-5 por sesión |
| `vehicle_favorite` | Engagement            | > 10%          |
| `signup_complete`  | Nuevos usuarios       | 100+ por mes   |
| `vehicle_publish`  | Nuevos listings       | 50+ por mes    |
| `phone_click`      | Leads de contacto     | > 5%           |

---

## 🔗 REFERENCIAS

### Backend

- [EventTrackingService](../../../backend/EventTrackingService/)
- [EventsController](../../../backend/EventTrackingService/EventTrackingService.Api/Controllers/EventsController.cs)

### Documentación Process Matrix

- [03-event-tracking.md](../../process-matrix/09-REPORTES-ANALYTICS/03-event-tracking.md) - **Procesos detallados** ⭐

### Servicios Consumidores

- [AnalyticsService](../../process-matrix/09-REPORTES-ANALYTICS/02-analytics-service.md)
- [RecommendationService](../../process-matrix/10-ML-RECOMENDACIONES/)
- [LeadScoringService](../../process-matrix/11-LEADS-CRM/)

---

## ✅ CHECKLIST DE IMPLEMENTACIÓN

### Instalación

- [ ] Agregar snippet de SDK en index.html
- [ ] Configurar variables de entorno
- [ ] Crear wrapper TypeScript

### Eventos Automáticos

- [ ] Track page views en navegación
- [ ] Identificar usuarios en login
- [ ] Reset en logout
- [ ] Track scroll depth
- [ ] Track tiempo en página

### Eventos Custom

- [ ] Vehicle views
- [ ] Vehicle clicks
- [ ] Vehicle favorites
- [ ] Búsquedas
- [ ] Aplicación de filtros
- [ ] Lead form interactions
- [ ] User registration
- [ ] Dealer actions

### Testing

- [ ] Verificar eventos en Network tab
- [ ] Verificar batch sending
- [ ] Verificar retry logic
- [ ] Testing en staging
- [ ] Testing en producción

### Monitoreo

- [ ] Dashboard de eventos en ClickHouse
- [ ] Alertas de eventos faltantes
- [ ] Métricas de performance
- [ ] Validación de datos

---

## 🧪 TESTS E2E (Playwright)

```typescript
import { test, expect } from "@playwright/test";
import { loginAsUser } from "../helpers/auth";

test.describe("Event Tracking SDK", () => {
  test("debe enviar evento de page_view automáticamente", async ({ page }) => {
    const events: any[] = [];
    await page.route("**/api/events/track", async (route) => {
      const postData = route.request().postDataJSON();
      events.push(postData);
      await route.fulfill({ status: 200 });
    });

    await page.goto("/vehicles");
    await page.waitForTimeout(1000);
    expect(events.some((e) => e.event_type === "page_view")).toBeTruthy();
  });

  test("debe enviar evento de vehicle_view al ver detalle", async ({
    page,
  }) => {
    const events: any[] = [];
    await page.route("**/api/events/track", async (route) => {
      const postData = route.request().postDataJSON();
      events.push(postData);
      await route.fulfill({ status: 200 });
    });

    await page.goto("/vehicles/toyota-camry-2024");
    await page.waitForTimeout(1000);
    expect(events.some((e) => e.event_type === "vehicle_view")).toBeTruthy();
  });

  test("debe enviar evento de search al buscar", async ({ page }) => {
    const events: any[] = [];
    await page.route("**/api/events/track", async (route) => {
      const postData = route.request().postDataJSON();
      events.push(postData);
      await route.fulfill({ status: 200 });
    });

    await page.goto("/search");
    await page.getByTestId("search-input").fill("Toyota");
    await page.getByTestId("search-submit").click();
    await page.waitForTimeout(1000);
    expect(events.some((e) => e.event_type === "search")).toBeTruthy();
  });

  test("debe identificar usuario después de login", async ({ page }) => {
    const events: any[] = [];
    await page.route("**/api/events/track", async (route) => {
      const postData = route.request().postDataJSON();
      events.push(postData);
      await route.fulfill({ status: 200 });
    });

    await loginAsUser(page);
    await page.goto("/");
    await page.waitForTimeout(1000);
    expect(events.some((e) => e.user_id !== null)).toBeTruthy();
  });

  test("debe agrupar eventos en batch antes de enviar", async ({ page }) => {
    let requestCount = 0;
    await page.route("**/api/events/batch", async (route) => {
      requestCount++;
      await route.fulfill({ status: 200 });
    });

    await page.goto("/");
    await page.goto("/vehicles");
    await page.goto("/search");
    await page.waitForTimeout(3000);
    expect(requestCount).toBeLessThanOrEqual(2);
  });
});
```

---

**Última actualización:** Enero 29, 2026  
**Versión:** 1.0.0  
**Próxima revisión:** Febrero 15, 2026
