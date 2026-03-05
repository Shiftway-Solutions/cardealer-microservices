# 🔐 01 - Autenticación y Registro

**Última actualización:** Enero 21, 2026  
**Versión:** 1.0.0  
**Servicio Principal:** AuthService

---

## 📋 Resumen

Este documento describe todos los flujos de autenticación y registro de usuarios en la plataforma OKLA.

---

## 🎭 Tipos de Usuario Aplicables

| AccountType        | Puede Registrarse | Método de Registro        |
| ------------------ | ----------------- | ------------------------- |
| `Guest`            | No (automático)   | -                         |
| `Individual`       | ✅ Sí             | Formulario público        |
| `Dealer`           | ✅ Sí             | Formulario + Verificación |
| `DealerEmployee`   | ❌ No             | Invitación de Dealer      |
| `Admin`            | ❌ No             | Creado por SuperAdmin     |
| `PlatformEmployee` | ❌ No             | Creado por SuperAdmin     |

---

## 📋 Pre-requisitos

- Ninguno para registro público
- Email válido y único
- Contraseña que cumpla requisitos de seguridad

---

## 🔄 Flujos de Autenticación

### 1️⃣ Registro de Usuario Individual

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     FLUJO DE REGISTRO INDIVIDUAL                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1. Usuario llena formulario                                                │
│     ↓                                                                       │
│  2. POST /api/auth/register                                                 │
│     ↓                                                                       │
│  3. Backend valida datos                                                    │
│     ├── Email único                                                         │
│     ├── Password seguro                                                     │
│     └── Campos requeridos                                                   │
│     ↓                                                                       │
│  4. Crear usuario con emailVerified = false                                 │
│     ↓                                                                       │
│  5. Enviar email de verificación                                            │
│     ↓                                                                       │
│  6. Usuario click en link                                                   │
│     ↓                                                                       │
│  7. GET /api/auth/verify-email?token={token}                                │
│     ↓                                                                       │
│  8. emailVerified = true                                                    │
│     ↓                                                                       │
│  9. Usuario puede hacer login                                               │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 2️⃣ Login Standard

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          FLUJO DE LOGIN                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1. Usuario ingresa credenciales                                            │
│     ↓                                                                       │
│  2. POST /api/auth/login                                                    │
│     ↓                                                                       │
│  3. Backend valida:                                                         │
│     ├── Usuario existe                                                      │
│     ├── Password correcto                                                   │
│     ├── Email verificado                                                    │
│     ├── Cuenta activa                                                       │
│     └── No está bloqueada                                                   │
│     ↓                                                                       │
│  4. Generar JWT tokens                                                      │
│     ├── accessToken (15 min)                                                │
│     └── refreshToken (7 días)                                               │
│     ↓                                                                       │
│  5. Retornar tokens + datos de usuario                                      │
│     ↓                                                                       │
│  6. Frontend guarda tokens en localStorage/cookies                          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3️⃣ Refresh Token

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       FLUJO DE REFRESH TOKEN                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1. accessToken expira (o está por expirar)                                 │
│     ↓                                                                       │
│  2. Frontend detecta error 401 o tiempo restante < 1 min                    │
│     ↓                                                                       │
│  3. POST /api/auth/refresh                                                  │
│     Body: { "refreshToken": "..." }                                         │
│     ↓                                                                       │
│  4. Backend valida refreshToken:                                            │
│     ├── Token válido                                                        │
│     ├── No expirado                                                         │
│     ├── No revocado                                                         │
│     └── Usuario activo                                                      │
│     ↓                                                                       │
│  5. Generar nuevos tokens                                                   │
│     ↓                                                                       │
│  6. Invalidar refreshToken anterior                                         │
│     ↓                                                                       │
│  7. Retornar nuevos tokens                                                  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 4️⃣ Forgot/Reset Password

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    FLUJO DE RECUPERACIÓN DE CONTRASEÑA                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1. Usuario click "Olvidé mi contraseña"                                    │
│     ↓                                                                       │
│  2. Ingresa email                                                           │
│     ↓                                                                       │
│  3. POST /api/auth/forgot-password                                          │
│     Body: { "email": "..." }                                                │
│     ↓                                                                       │
│  4. Backend genera token de reset (1 hora validez)                          │
│     ↓                                                                       │
│  5. Envía email con link de reset                                           │
│     https://okla.com.do/reset-password?token={token}                        │
│     ↓                                                                       │
│  6. Usuario click en link, llega a página de reset                          │
│     ↓                                                                       │
│  7. Usuario ingresa nueva contraseña                                        │
│     ↓                                                                       │
│  8. POST /api/auth/reset-password                                           │
│     Body: { "token": "...", "newPassword": "..." }                          │
│     ↓                                                                       │
│  9. Backend valida token y actualiza contraseña                             │
│     ↓                                                                       │
│  10. Invalida todos los refresh tokens activos                              │
│     ↓                                                                       │
│  11. Usuario debe hacer login nuevamente                                    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 📡 API Endpoints

### POST `/api/auth/register`

Registra un nuevo usuario Individual.

**Request:**

```json
{
  "email": "usuario@ejemplo.com",
  "password": "MiPassword123!",
  "confirmPassword": "MiPassword123!",
  "firstName": "Juan",
  "lastName": "Pérez",
  "phoneNumber": "+1-809-555-1234",
  "acceptTerms": true,
  "acceptMarketing": false
}
```

**Response (201 Created):**

```json
{
  "success": true,
  "message": "Cuenta creada exitosamente. Por favor verifica tu email.",
  "data": {
    "userId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "email": "usuario@ejemplo.com",
    "emailVerified": false,
    "accountType": "Individual",
    "createdAt": "2026-01-21T10:30:00Z"
  }
}
```

**Validaciones:**

| Campo             | Regla                                          | Mensaje de Error                         |
| ----------------- | ---------------------------------------------- | ---------------------------------------- |
| `email`           | Formato válido, único                          | "Email inválido" / "Email ya registrado" |
| `password`        | Min 8 chars, 1 mayúscula, 1 número, 1 especial | "Contraseña no cumple requisitos"        |
| `confirmPassword` | Igual a password                               | "Las contraseñas no coinciden"           |
| `firstName`       | 2-50 chars                                     | "Nombre requerido"                       |
| `lastName`        | 2-50 chars                                     | "Apellido requerido"                     |
| `phoneNumber`     | Formato válido                                 | "Teléfono inválido"                      |
| `acceptTerms`     | true                                           | "Debe aceptar términos y condiciones"    |

**Errores:**

| Código | Causa              | Response                                  |
| ------ | ------------------ | ----------------------------------------- |
| 400    | Validación fallida | `{ "errors": [...] }`                     |
| 409    | Email ya existe    | `{ "error": "Email already registered" }` |

---

### POST `/api/auth/login`

Inicia sesión y obtiene tokens JWT.

**Request:**

```json
{
  "email": "usuario@ejemplo.com",
  "password": "MiPassword123!",
  "rememberMe": true
}
```

**Response (200 OK):**

```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2...",
    "expiresIn": 900,
    "refreshExpiresIn": 604800,
    "tokenType": "Bearer",
    "user": {
      "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "email": "usuario@ejemplo.com",
      "firstName": "Juan",
      "lastName": "Pérez",
      "fullName": "Juan Pérez",
      "accountType": "Individual",
      "avatarUrl": null,
      "emailVerified": true,
      "phoneVerified": false,
      "hasActiveSubscription": false,
      "dealerId": null,
      "adminRole": null
    }
  }
}
```

**Errores:**

| Código | Causa                  | Response                                                          |
| ------ | ---------------------- | ----------------------------------------------------------------- |
| 400    | Campos faltantes       | `{ "errors": [...] }`                                             |
| 401    | Credenciales inválidas | `{ "error": "Invalid credentials" }`                              |
| 403    | Email no verificado    | `{ "error": "Email not verified", "requiresVerification": true }` |
| 403    | Cuenta suspendida      | `{ "error": "Account suspended", "reason": "..." }`               |
| 429    | Demasiados intentos    | `{ "error": "Too many attempts", "lockoutMinutes": 30 }`          |

---

### GET `/api/auth/verify-email`

Verifica el email del usuario.

**Query Parameters:**

- `token` (required): Token de verificación del email

**Request:**

```
GET /api/auth/verify-email?token=abc123def456
```

**Response (200 OK):**

```json
{
  "success": true,
  "message": "Email verificado exitosamente. Ahora puedes iniciar sesión."
}
```

**Errores:**

| Código | Causa          | Response                                |
| ------ | -------------- | --------------------------------------- |
| 400    | Token faltante | `{ "error": "Token required" }`         |
| 400    | Token expirado | `{ "error": "Token expired" }`          |
| 400    | Token inválido | `{ "error": "Invalid token" }`          |
| 409    | Ya verificado  | `{ "error": "Email already verified" }` |

---

### POST `/api/auth/refresh`

Renueva los tokens JWT.

**Request:**

```json
{
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2..."
}
```

**Response (200 OK):**

```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "bmV3IHJlZnJlc2ggdG9r...",
    "expiresIn": 900,
    "refreshExpiresIn": 604800,
    "tokenType": "Bearer"
  }
}
```

**Errores:**

| Código | Causa          | Response                                |
| ------ | -------------- | --------------------------------------- |
| 400    | Token faltante | `{ "error": "Refresh token required" }` |
| 401    | Token inválido | `{ "error": "Invalid refresh token" }`  |
| 401    | Token expirado | `{ "error": "Refresh token expired" }`  |
| 401    | Token revocado | `{ "error": "Refresh token revoked" }`  |

---

### POST `/api/auth/forgot-password`

Solicita un email de recuperación de contraseña.

**Request:**

```json
{
  "email": "usuario@ejemplo.com"
}
```

**Response (200 OK):**

```json
{
  "success": true,
  "message": "Si el email existe, recibirás instrucciones para recuperar tu contraseña."
}
```

> ⚠️ **Seguridad:** Siempre retorna 200 OK aunque el email no exista para evitar enumeración de usuarios.

---

### POST `/api/auth/reset-password`

Establece una nueva contraseña usando el token de recuperación.

**Request:**

```json
{
  "token": "abc123def456",
  "newPassword": "NuevaPassword123!",
  "confirmPassword": "NuevaPassword123!"
}
```

**Response (200 OK):**

```json
{
  "success": true,
  "message": "Contraseña actualizada exitosamente. Por favor inicia sesión."
}
```

**Errores:**

| Código | Causa                  | Response                                             |
| ------ | ---------------------- | ---------------------------------------------------- |
| 400    | Token expirado         | `{ "error": "Reset token expired" }`                 |
| 400    | Token inválido         | `{ "error": "Invalid reset token" }`                 |
| 400    | Password débil         | `{ "error": "Password does not meet requirements" }` |
| 400    | Passwords no coinciden | `{ "error": "Passwords do not match" }`              |

---

### POST `/api/auth/logout`

Cierra sesión e invalida tokens.

**Headers:**

```http
Authorization: Bearer {accessToken}
```

**Request:**

```json
{
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2..."
}
```

**Response (200 OK):**

```json
{
  "success": true,
  "message": "Sesión cerrada exitosamente."
}
```

---

### GET `/api/auth/me`

Obtiene los datos del usuario autenticado actual.

**Headers:**

```http
Authorization: Bearer {accessToken}
```

**Response (200 OK):**

```json
{
  "success": true,
  "data": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "email": "usuario@ejemplo.com",
    "firstName": "Juan",
    "lastName": "Pérez",
    "fullName": "Juan Pérez",
    "accountType": "Individual",
    "avatarUrl": "https://cdn.okla.com.do/avatars/abc123.jpg",
    "emailVerified": true,
    "phoneNumber": "+1-809-555-1234",
    "phoneVerified": false,
    "hasActiveSubscription": false,
    "dealerId": null,
    "adminRole": null,
    "permissions": ["marketplace:view", "favorites:manage", "listings:create"],
    "createdAt": "2026-01-15T08:00:00Z",
    "lastLoginAt": "2026-01-21T10:30:00Z"
  }
}
```

---

## 💡 Ejemplos de Código

### Frontend: Registro

```typescript
// services/authService.ts
interface RegisterData {
  email: string;
  password: string;
  confirmPassword: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  acceptTerms: boolean;
  acceptMarketing?: boolean;
}

export async function register(data: RegisterData): Promise<RegisterResponse> {
  const response = await fetch("/api/auth/register", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });

  if (!response.ok) {
    const error = await response.json();
    throw new AuthError(error.error || "Registration failed", response.status);
  }

  return response.json();
}
```

### Frontend: Login con Store

```typescript
// store/authStore.ts
import { create } from "zustand";
import { persist } from "zustand/middleware";

interface AuthState {
  user: User | null;
  accessToken: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshAuth: () => Promise<void>;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      user: null,
      accessToken: null,
      refreshToken: null,
      isAuthenticated: false,

      login: async (email, password) => {
        const response = await fetch("/api/auth/login", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ email, password }),
        });

        if (!response.ok) {
          const error = await response.json();
          throw new Error(error.error);
        }

        const { data } = await response.json();

        set({
          user: data.user,
          accessToken: data.accessToken,
          refreshToken: data.refreshToken,
          isAuthenticated: true,
        });
      },

      logout: async () => {
        const { accessToken, refreshToken } = get();

        try {
          await fetch("/api/auth/logout", {
            method: "POST",
            headers: {
              "Content-Type": "application/json",
              Authorization: `Bearer ${accessToken}`,
            },
            body: JSON.stringify({ refreshToken }),
          });
        } finally {
          set({
            user: null,
            accessToken: null,
            refreshToken: null,
            isAuthenticated: false,
          });
        }
      },

      refreshAuth: async () => {
        const { refreshToken } = get();

        if (!refreshToken) {
          throw new Error("No refresh token available");
        }

        const response = await fetch("/api/auth/refresh", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ refreshToken }),
        });

        if (!response.ok) {
          // Token inválido, forzar logout
          get().logout();
          throw new Error("Session expired");
        }

        const { data } = await response.json();

        set({
          accessToken: data.accessToken,
          refreshToken: data.refreshToken,
        });
      },
    }),
    {
      name: "okla-auth-storage",
      partialize: (state) => ({
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        user: state.user,
      }),
    },
  ),
);
```

### Frontend: Axios Interceptor

```typescript
// lib/axios.ts
import axios from "axios";
import { useAuthStore } from "@/store/authStore";

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
});

// Request interceptor - agregar token
api.interceptors.request.use((config) => {
  const { accessToken } = useAuthStore.getState();

  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }

  return config;
});

// Response interceptor - manejar 401
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        await useAuthStore.getState().refreshAuth();
        const { accessToken } = useAuthStore.getState();
        originalRequest.headers.Authorization = `Bearer ${accessToken}`;
        return api(originalRequest);
      } catch (refreshError) {
        // Refresh falló, redirigir a login
        window.location.href = "/login";
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  },
);

export default api;
```

---

## 🔍 Validaciones de Contraseña

### Requisitos Mínimos

```
✅ Mínimo 8 caracteres
✅ Al menos 1 letra mayúscula (A-Z)
✅ Al menos 1 letra minúscula (a-z)
✅ Al menos 1 número (0-9)
✅ Al menos 1 carácter especial (!@#$%^&*(),.?":{}|<>)
❌ No puede ser igual al email
❌ No puede contener el nombre de usuario
❌ No puede estar en lista de contraseñas comunes
```

### Regex de Validación

```javascript
const passwordRegex =
  /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/;
```

---

## ⚠️ Casos de Error Especiales

### Cuenta Bloqueada por Intentos

Después de 5 intentos fallidos consecutivos:

```json
{
  "error": "Account temporarily locked",
  "lockoutMinutes": 30,
  "remainingLockoutTime": 1800,
  "message": "Tu cuenta está bloqueada temporalmente. Intenta de nuevo en 30 minutos."
}
```

### Email No Verificado

```json
{
  "error": "Email not verified",
  "requiresVerification": true,
  "resendUrl": "/api/auth/resend-verification",
  "message": "Por favor verifica tu email antes de iniciar sesión."
}
```

### Token de Verificación Expirado

```json
{
  "error": "Verification token expired",
  "canResend": true,
  "resendUrl": "/api/auth/resend-verification",
  "message": "El link de verificación ha expirado. Solicita uno nuevo."
}
```

---

## 🔗 Navegación

- **Anterior:** [00_INDICE_MAESTRO.md](00_INDICE_MAESTRO.md)
- **Siguiente:** [02_GESTION_USUARIOS.md](02_GESTION_USUARIOS.md)

---

**Equipo OKLA - Enero 2026**
