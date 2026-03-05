# 🚨 Códigos de Error API por Servicio

> **Tiempo estimado:** 30 minutos de lectura
> **Propósito:** Referencia de todos los códigos de error del backend
> **Última actualización:** Enero 31, 2026

---

## 📋 OBJETIVO

Documentar todos los códigos de error del backend para:

- Manejo correcto en el frontend
- Mensajes de usuario apropiados
- Lógica de retry automático
- Tracking en Sentry

---

## 🎯 ESTRUCTURA DE RESPUESTA DE ERROR

### RFC 7807 Problem Details (Estándar)

```typescript
// filepath: src/types/api-errors.ts
interface ProblemDetails {
  type: string; // URI que identifica el tipo de problema
  title: string; // Resumen corto legible
  status: number; // Código HTTP
  detail?: string; // Explicación específica
  instance?: string; // URI de la ocurrencia
  traceId?: string; // ID para debugging
  errorCode?: string; // Código para manejo programático
  errors?: Record<string, string[]>; // Errores de validación
  extensions?: Record<string, unknown>; // Propiedades adicionales
}
```

### ApiResponse Wrapper (Endpoints Legacy)

```typescript
interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: string;
  metadata?: Record<string, unknown>;
  timestamp: string;
}
```

---

## 📊 CÓDIGOS HTTP ESTÁNDAR

| Código | Nombre              | ErrorCode             | Descripción             | Retry                 |
| ------ | ------------------- | --------------------- | ----------------------- | --------------------- |
| 400    | Bad Request         | `VALIDATION_ERROR`    | Datos inválidos         | ❌ No                 |
| 401    | Unauthorized        | `UNAUTHORIZED`        | Token inválido/expirado | ⚡ Refresh token      |
| 403    | Forbidden           | `FORBIDDEN`           | Sin permisos            | ❌ No                 |
| 404    | Not Found           | `NOT_FOUND`           | Recurso no existe       | ❌ No                 |
| 409    | Conflict            | `CONFLICT`            | Conflicto de datos      | ❌ No                 |
| 422    | Unprocessable       | `VALIDATION_ERROR`    | Error semántico         | ❌ No                 |
| 429    | Too Many Requests   | `RATE_LIMIT_EXCEEDED` | Límite excedido         | ⏱️ Esperar retryAfter |
| 499    | Client Closed       | `REQUEST_CANCELLED`   | Request cancelado       | ⚡ Sí                 |
| 500    | Internal Error      | `INTERNAL_ERROR`      | Error del servidor      | ⚡ Sí (max 3)         |
| 502    | Bad Gateway         | `BAD_GATEWAY`         | Gateway error           | ⚡ Sí (max 3)         |
| 503    | Service Unavailable | `SERVICE_UNAVAILABLE` | Servicio caído          | ⚡ Sí (exponential)   |
| 504    | Gateway Timeout     | `TIMEOUT`             | Timeout                 | ⚡ Sí (max 2)         |

---

## 🔐 AUTH SERVICE - Códigos de Error

### Autenticación

| ErrorCode               | HTTP | Mensaje Usuario (ES)                        | Acción Frontend             |
| ----------------------- | ---- | ------------------------------------------- | --------------------------- |
| `INVALID_CREDENTIALS`   | 401  | "Email o contraseña incorrectos"            | Mostrar error en form       |
| `USER_NOT_FOUND`        | 404  | "No existe una cuenta con este email"       | Link a registro             |
| `USER_LOCKED`           | 403  | "Tu cuenta está bloqueada temporalmente"    | Mostrar tiempo restante     |
| `USER_DISABLED`         | 403  | "Tu cuenta ha sido desactivada"             | Link a soporte              |
| `EMAIL_NOT_VERIFIED`    | 403  | "Por favor verifica tu email"               | Botón reenviar verificación |
| `TOKEN_EXPIRED`         | 401  | -                                           | Refresh automático          |
| `TOKEN_INVALID`         | 401  | "Sesión inválida, inicia sesión nuevamente" | Redirect a login            |
| `REFRESH_TOKEN_EXPIRED` | 401  | "Tu sesión ha expirado"                     | Redirect a login            |

### Registro

| ErrorCode              | HTTP | Mensaje Usuario (ES)                          | Acción Frontend    |
| ---------------------- | ---- | --------------------------------------------- | ------------------ |
| `EMAIL_ALREADY_EXISTS` | 409  | "Ya existe una cuenta con este email"         | Link a login       |
| `PHONE_ALREADY_EXISTS` | 409  | "Este teléfono ya está registrado"            | Link a recuperar   |
| `WEAK_PASSWORD`        | 400  | "La contraseña no cumple los requisitos"      | Mostrar requisitos |
| `INVALID_EMAIL_FORMAT` | 400  | "El formato del email es inválido"            | -                  |
| `INVALID_PHONE_FORMAT` | 400  | "Formato de teléfono inválido (+1809XXXXXXX)" | -                  |

### 2FA

| ErrorCode               | HTTP | Mensaje Usuario (ES)                    | Acción Frontend            |
| ----------------------- | ---- | --------------------------------------- | -------------------------- |
| `2FA_REQUIRED`          | 428  | -                                       | Mostrar form de código 2FA |
| `2FA_INVALID_CODE`      | 400  | "Código incorrecto, intenta nuevamente" | Limpiar input              |
| `2FA_CODE_EXPIRED`      | 400  | "El código ha expirado"                 | Botón reenviar             |
| `2FA_MAX_ATTEMPTS`      | 429  | "Demasiados intentos, espera X minutos" | Mostrar countdown          |
| `2FA_ALREADY_ENABLED`   | 409  | "Ya tienes 2FA activado"                | -                          |
| `RECOVERY_CODE_INVALID` | 400  | "Código de recuperación inválido"       | -                          |
| `RECOVERY_CODE_USED`    | 400  | "Este código ya fue utilizado"          | -                          |
| `NO_RECOVERY_CODES`     | 400  | "No tienes códigos de recuperación"     | Botón generar              |

### OAuth

| ErrorCode              | HTTP | Mensaje Usuario (ES)                     | Acción Frontend           |
| ---------------------- | ---- | ---------------------------------------- | ------------------------- |
| `OAUTH_EMAIL_EXISTS`   | 409  | "Este email ya está registrado"          | Mostrar opciones de login |
| `OAUTH_PROVIDER_ERROR` | 502  | "Error al conectar con {provider}"       | Botón reintentar          |
| `OAUTH_ACCOUNT_LINKED` | 409  | "Esta cuenta ya está vinculada"          | -                         |
| `OAUTH_STATE_MISMATCH` | 400  | "Error de seguridad, intenta nuevamente" | Reiniciar flujo           |

---

## 🚗 VEHICLES SALE SERVICE - Códigos de Error

### Vehículos

| ErrorCode                | HTTP | Mensaje Usuario (ES)                       | Acción Frontend    |
| ------------------------ | ---- | ------------------------------------------ | ------------------ |
| `VEHICLE_NOT_FOUND`      | 404  | "Este vehículo ya no está disponible"      | Link a búsqueda    |
| `VEHICLE_INACTIVE`       | 410  | "Esta publicación ha sido pausada"         | -                  |
| `VEHICLE_SOLD`           | 410  | "Este vehículo ya fue vendido"             | Mostrar similares  |
| `VEHICLE_LIMIT_EXCEEDED` | 403  | "Has alcanzado el límite de publicaciones" | Botón upgrade plan |
| `DUPLICATE_VIN`          | 409  | "Ya existe un vehículo con este VIN"       | -                  |
| `INVALID_VIN`            | 400  | "El VIN ingresado no es válido"            | Mostrar formato    |
| `INVALID_PRICE`          | 400  | "El precio debe estar entre RD$X y RD$X"   | -                  |
| `INVALID_YEAR`           | 400  | "El año debe estar entre X y X"            | -                  |

### Favoritos

| ErrorCode            | HTTP | Mensaje Usuario (ES)              | Acción Frontend   |
| -------------------- | ---- | --------------------------------- | ----------------- |
| `FAVORITE_EXISTS`    | 409  | -                                 | Toggle silencioso |
| `FAVORITE_NOT_FOUND` | 404  | -                                 | Toggle silencioso |
| `FAVORITES_LIMIT`    | 403  | "Máximo 100 favoritos permitidos" | -                 |

### Búsquedas Guardadas

| ErrorCode             | HTTP | Mensaje Usuario (ES)             | Acción Frontend |
| --------------------- | ---- | -------------------------------- | --------------- |
| `SAVED_SEARCH_LIMIT`  | 403  | "Máximo 10 búsquedas guardadas"  | Upgrade plan    |
| `SAVED_SEARCH_EXISTS` | 409  | "Ya tienes una búsqueda similar" | -               |

---

## 💳 BILLING SERVICE - Códigos de Error

### Pagos

| ErrorCode            | HTTP | Mensaje Usuario (ES)            | Acción Frontend    |
| -------------------- | ---- | ------------------------------- | ------------------ |
| `PAYMENT_FAILED`     | 402  | "El pago fue rechazado"         | Botón reintentar   |
| `PAYMENT_DECLINED`   | 402  | "Tu tarjeta fue declinada"      | Cambiar método     |
| `INSUFFICIENT_FUNDS` | 402  | "Fondos insuficientes"          | -                  |
| `CARD_EXPIRED`       | 402  | "Tu tarjeta ha expirado"        | Actualizar tarjeta |
| `CARD_INVALID`       | 400  | "Número de tarjeta inválido"    | -                  |
| `CVV_INVALID`        | 400  | "CVV incorrecto"                | -                  |
| `PAYMENT_PROCESSING` | 202  | "Tu pago está siendo procesado" | Polling status     |
| `PAYMENT_TIMEOUT`    | 504  | "Tiempo de espera agotado"      | Verificar status   |

### Suscripciones

| ErrorCode                | HTTP | Mensaje Usuario (ES)               | Acción Frontend   |
| ------------------------ | ---- | ---------------------------------- | ----------------- |
| `SUBSCRIPTION_ACTIVE`    | 409  | "Ya tienes una suscripción activa" | -                 |
| `SUBSCRIPTION_CANCELLED` | 400  | "Tu suscripción fue cancelada"     | Botón reactivar   |
| `SUBSCRIPTION_EXPIRED`   | 403  | "Tu suscripción ha vencido"        | Botón renovar     |
| `PLAN_NOT_FOUND`         | 404  | "El plan seleccionado no existe"   | -                 |
| `DOWNGRADE_NOT_ALLOWED`  | 403  | "No puedes bajar de plan ahora"    | Contactar soporte |
| `CANCEL_PENDING_INVOICE` | 400  | "Tienes un pago pendiente"         | Pagar primero     |

### Stripe Específicos

| ErrorCode                 | HTTP | Mensaje Usuario (ES)            | Acción Frontend |
| ------------------------- | ---- | ------------------------------- | --------------- |
| `STRIPE_CUSTOMER_ERROR`   | 500  | "Error al procesar tu cuenta"   | Reintentar      |
| `STRIPE_WEBHOOK_ERROR`    | 400  | -                               | Log interno     |
| `STRIPE_RESOURCE_MISSING` | 404  | "Recurso de pago no encontrado" | Crear nuevo     |

### AZUL Específicos (RD)

| ErrorCode              | HTTP | Mensaje Usuario (ES)                  | Acción Frontend |
| ---------------------- | ---- | ------------------------------------- | --------------- |
| `AZUL_DECLINED`        | 402  | "Transacción declinada por el banco"  | Contactar banco |
| `AZUL_AUTH_FAILED`     | 402  | "Error de autenticación"              | -               |
| `AZUL_TIMEOUT`         | 504  | "El banco no respondió a tiempo"      | Reintentar      |
| `AZUL_FRAUD_SUSPECTED` | 403  | "Transacción rechazada por seguridad" | Contactar banco |

---

## 📷 MEDIA SERVICE - Códigos de Error

| ErrorCode           | HTTP | Mensaje Usuario (ES)                        | Acción Frontend   |
| ------------------- | ---- | ------------------------------------------- | ----------------- |
| `FILE_TOO_LARGE`    | 413  | "El archivo excede el límite de 10MB"       | Comprimir         |
| `INVALID_FILE_TYPE` | 415  | "Solo se permiten JPG, PNG, WebP"           | -                 |
| `UPLOAD_FAILED`     | 500  | "Error al subir la imagen"                  | Reintentar        |
| `IMAGE_CORRUPTED`   | 400  | "La imagen está dañada"                     | Seleccionar otra  |
| `STORAGE_LIMIT`     | 403  | "Has alcanzado el límite de almacenamiento" | Eliminar imágenes |
| `PROCESSING_FAILED` | 500  | "Error al procesar la imagen"               | Reintentar        |
| `IMAGE_NOT_FOUND`   | 404  | "Imagen no encontrada"                      | -                 |
| `DELETE_FAILED`     | 500  | "Error al eliminar la imagen"               | Reintentar        |

---

## 📧 NOTIFICATION SERVICE - Códigos de Error

| ErrorCode            | HTTP | Mensaje Usuario (ES)                        | Acción Frontend    |
| -------------------- | ---- | ------------------------------------------- | ------------------ |
| `EMAIL_SEND_FAILED`  | 500  | -                                           | Log interno, retry |
| `SMS_SEND_FAILED`    | 500  | "Error enviando SMS"                        | Botón reenviar     |
| `PUSH_TOKEN_INVALID` | 400  | -                                           | Re-register push   |
| `TEMPLATE_NOT_FOUND` | 404  | -                                           | Log error          |
| `UNSUBSCRIBED`       | 400  | "Te has desuscrito de estas notificaciones" | -                  |
| `RATE_LIMITED`       | 429  | "Demasiados intentos, espera X minutos"     | Countdown          |

---

## 👤 USER SERVICE - Códigos de Error

| ErrorCode              | HTTP | Mensaje Usuario (ES)                               | Acción Frontend    |
| ---------------------- | ---- | -------------------------------------------------- | ------------------ |
| `USER_NOT_FOUND`       | 404  | "Usuario no encontrado"                            | -                  |
| `PROFILE_INCOMPLETE`   | 400  | "Por favor completa tu perfil"                     | Redirect a profile |
| `PHONE_UPDATE_LIMIT`   | 429  | "Solo puedes cambiar tu teléfono cada 30 días"     | -                  |
| `EMAIL_UPDATE_PENDING` | 409  | "Ya tienes una verificación de email pendiente"    | -                  |
| `AVATAR_INVALID`       | 400  | "Formato de imagen inválido"                       | -                  |
| `GDPR_DELETE_PENDING`  | 409  | "Ya tienes una solicitud de eliminación pendiente" | -                  |

---

## 🏢 DEALER SERVICE - Códigos de Error

| ErrorCode                 | HTTP | Mensaje Usuario (ES)                                 | Acción Frontend   |
| ------------------------- | ---- | ---------------------------------------------------- | ----------------- |
| `DEALER_NOT_VERIFIED`     | 403  | "Tu cuenta de dealer está pendiente de verificación" | Status page       |
| `DEALER_SUSPENDED`        | 403  | "Tu cuenta de dealer ha sido suspendida"             | Contactar soporte |
| `DEALER_RNC_INVALID`      | 400  | "El RNC ingresado no es válido"                      | -                 |
| `DEALER_RNC_EXISTS`       | 409  | "Ya existe un dealer con este RNC"                   | -                 |
| `DEALER_DOCUMENT_EXPIRED` | 400  | "El documento ha expirado"                           | Subir nuevo       |
| `DEALER_LOCATION_LIMIT`   | 403  | "Has alcanzado el límite de sucursales"              | Upgrade plan      |
| `INVENTORY_IMPORT_FAILED` | 400  | "Error al importar inventario"                       | Ver detalles      |

---

## 👨‍💼 ADMIN SERVICE - Códigos de Error

| ErrorCode             | HTTP | Mensaje Usuario (ES)                         | Acción Frontend |
| --------------------- | ---- | -------------------------------------------- | --------------- |
| `ADMIN_ACCESS_DENIED` | 403  | "No tienes permisos de administrador"        | -               |
| `ROLE_NOT_FOUND`      | 404  | "Rol no encontrado"                          | -               |
| `PERMISSION_DENIED`   | 403  | "No tienes permiso para esta acción"         | -               |
| `CANNOT_DELETE_SELF`  | 400  | "No puedes eliminarte a ti mismo"            | -               |
| `LAST_ADMIN`          | 400  | "No puedes eliminar el último administrador" | -               |

---

## 🔧 IMPLEMENTACIÓN EN FRONTEND

### Mapper de Errores

```typescript
// filepath: src/lib/api/error-mapper.ts
import { toast } from "sonner";

const ERROR_MESSAGES: Record<string, string> = {
  // Auth
  INVALID_CREDENTIALS: "Email o contraseña incorrectos",
  USER_NOT_FOUND: "No existe una cuenta con este email",
  EMAIL_NOT_VERIFIED: "Por favor verifica tu email",
  TOKEN_EXPIRED: "Tu sesión ha expirado",

  // Vehicles
  VEHICLE_NOT_FOUND: "Este vehículo ya no está disponible",
  VEHICLE_LIMIT_EXCEEDED: "Has alcanzado el límite de publicaciones",

  // Billing
  PAYMENT_FAILED: "El pago fue rechazado",
  INSUFFICIENT_FUNDS: "Fondos insuficientes",
  CARD_EXPIRED: "Tu tarjeta ha expirado",

  // Media
  FILE_TOO_LARGE: "El archivo excede el límite de 10MB",
  INVALID_FILE_TYPE: "Solo se permiten JPG, PNG, WebP",

  // Generic
  VALIDATION_ERROR: "Por favor revisa los datos ingresados",
  UNAUTHORIZED: "Tu sesión ha expirado",
  FORBIDDEN: "No tienes permisos para esta acción",
  NOT_FOUND: "Recurso no encontrado",
  RATE_LIMIT_EXCEEDED: "Demasiadas solicitudes, intenta más tarde",
  INTERNAL_ERROR: "Ocurrió un error, intenta nuevamente",
};

export function getErrorMessage(errorCode: string, fallback?: string): string {
  return ERROR_MESSAGES[errorCode] || fallback || "Ocurrió un error inesperado";
}

export function handleApiError(
  error: ProblemDetails | ApiResponse<unknown>,
): void {
  // Extraer código de error
  const errorCode = "errorCode" in error ? error.errorCode : undefined;

  const message = getErrorMessage(
    errorCode || "",
    ("detail" in error ? error.detail : error.error) || undefined,
  );

  // Mostrar toast
  toast.error(message, {
    action: needsAction(errorCode)
      ? { label: "Ver detalles", onClick: () => {} }
      : undefined,
  });
}

function needsAction(errorCode?: string): boolean {
  const actionCodes = [
    "VEHICLE_LIMIT_EXCEEDED",
    "SUBSCRIPTION_EXPIRED",
    "EMAIL_NOT_VERIFIED",
    "DEALER_NOT_VERIFIED",
  ];
  return errorCode ? actionCodes.includes(errorCode) : false;
}
```

### Hook con Error Handling

```typescript
// filepath: src/hooks/useApiError.ts
import { useCallback } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/providers/AuthProvider";
import { handleApiError, getErrorMessage } from "@/lib/api/error-mapper";

export function useApiError() {
  const router = useRouter();
  const { logout, refreshToken } = useAuth();

  const handleError = useCallback(
    async (error: unknown) => {
      // Type guard
      if (!isApiError(error)) {
        console.error("Unknown error:", error);
        return;
      }

      const status = error.status;
      const errorCode = error.errorCode;

      // 401 - Intentar refresh
      if (status === 401 && errorCode !== "TOKEN_INVALID") {
        const refreshed = await refreshToken();
        if (!refreshed) {
          logout();
          router.push("/login");
        }
        return;
      }

      // 401 con token inválido - Logout directo
      if (status === 401 && errorCode === "TOKEN_INVALID") {
        logout();
        router.push("/login");
        return;
      }

      // 403 - Verificación requerida
      if (errorCode === "EMAIL_NOT_VERIFIED") {
        router.push("/verify-email");
        return;
      }

      if (errorCode === "DEALER_NOT_VERIFIED") {
        router.push("/dealer/verification-pending");
        return;
      }

      // 428 - 2FA requerido
      if (errorCode === "2FA_REQUIRED") {
        router.push("/login/2fa");
        return;
      }

      // Default: mostrar toast
      handleApiError(error);
    },
    [router, logout, refreshToken],
  );

  return { handleError, getErrorMessage };
}

function isApiError(error: unknown): error is ProblemDetails {
  return (
    typeof error === "object" &&
    error !== null &&
    "status" in error &&
    typeof (error as ProblemDetails).status === "number"
  );
}
```

### Retry Logic

```typescript
// filepath: src/lib/api/retry.ts
interface RetryConfig {
  maxRetries: number;
  baseDelay: number;
  maxDelay: number;
  retryableStatuses: number[];
}

const DEFAULT_CONFIG: RetryConfig = {
  maxRetries: 3,
  baseDelay: 1000,
  maxDelay: 10000,
  retryableStatuses: [408, 429, 499, 500, 502, 503, 504],
};

export async function fetchWithRetry<T>(
  fetcher: () => Promise<T>,
  config: Partial<RetryConfig> = {},
): Promise<T> {
  const { maxRetries, baseDelay, maxDelay, retryableStatuses } = {
    ...DEFAULT_CONFIG,
    ...config,
  };

  let lastError: unknown;

  for (let attempt = 0; attempt <= maxRetries; attempt++) {
    try {
      return await fetcher();
    } catch (error) {
      lastError = error;

      // No reintentar si no es retryable
      if (!isRetryable(error, retryableStatuses)) {
        throw error;
      }

      // Última iteración: lanzar error
      if (attempt === maxRetries) {
        throw error;
      }

      // Calcular delay con backoff exponencial
      const delay = Math.min(
        baseDelay * Math.pow(2, attempt) + Math.random() * 1000,
        maxDelay,
      );

      // Si es 429, usar retryAfter del header
      if (isRateLimited(error)) {
        const retryAfter = getRetryAfter(error);
        if (retryAfter) {
          await sleep(retryAfter * 1000);
          continue;
        }
      }

      await sleep(delay);
    }
  }

  throw lastError;
}

function isRetryable(error: unknown, statuses: number[]): boolean {
  if (!isApiError(error)) return false;
  return statuses.includes(error.status);
}

function isRateLimited(error: unknown): boolean {
  return isApiError(error) && error.status === 429;
}

function getRetryAfter(error: unknown): number | null {
  if (!isApiError(error)) return null;
  return error.extensions?.retryAfter as number | null;
}

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}
```

---

## 🧪 TESTS E2E

```typescript
// filepath: e2e/error-handling.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Error Handling", () => {
  test("muestra error de credenciales incorrectas", async ({ page }) => {
    await page.goto("/login");
    await page.fill('[name="email"]', "wrong@email.com");
    await page.fill('[name="password"]', "wrongpassword");
    await page.click('button[type="submit"]');

    await expect(page.locator(".toast-error")).toContainText(
      "Email o contraseña incorrectos",
    );
  });

  test("redirige a login en 401", async ({ page, context }) => {
    // Limpiar token
    await context.clearCookies();

    await page.goto("/dashboard");
    await expect(page).toHaveURL("/login");
  });

  test("muestra error de rate limit", async ({ page }) => {
    // Simular rate limit
    await page.route("**/api/**", (route) => {
      route.fulfill({
        status: 429,
        body: JSON.stringify({
          status: 429,
          errorCode: "RATE_LIMIT_EXCEEDED",
          detail: "Too many requests",
          extensions: { retryAfter: 60 },
        }),
      });
    });

    await page.goto("/vehicles");

    await expect(page.locator(".toast-error")).toContainText(
      "Demasiadas solicitudes",
    );
  });

  test("reintenta en 503 y muestra éxito", async ({ page }) => {
    let attempts = 0;

    await page.route("**/api/vehicles**", (route) => {
      attempts++;
      if (attempts < 3) {
        route.fulfill({ status: 503, body: "Service Unavailable" });
      } else {
        route.fulfill({
          status: 200,
          body: JSON.stringify({ success: true, data: [] }),
        });
      }
    });

    await page.goto("/vehicles");

    // Debería mostrar contenido después de retry
    await expect(page.locator('[data-testid="vehicle-grid"]')).toBeVisible();
  });
});
```

---

## 📋 CHECKLIST DE IMPLEMENTACIÓN

- [ ] Tipo `ProblemDetails` definido en `src/types/api-errors.ts`
- [ ] Mapper de errores con todos los códigos en `src/lib/api/error-mapper.ts`
- [ ] Hook `useApiError` implementado
- [ ] Retry logic con backoff exponencial
- [ ] Interceptor de Axios/Fetch configurado
- [ ] Tests E2E para flujos de error críticos
- [ ] Sentry configurado para capturar errores
- [ ] Traducciones ES/EN para mensajes de error

---

## 🔗 REFERENCIAS

- [RFC 7807 - Problem Details](https://tools.ietf.org/html/rfc7807)
- [HTTP Status Codes](https://developer.mozilla.org/en-US/docs/Web/HTTP/Status)
- [Stripe Error Codes](https://stripe.com/docs/error-codes)

---

_Última actualización: Enero 31, 2026_
