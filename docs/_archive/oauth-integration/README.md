# 🔐 OAuth Integration - OKLA Platform

Esta documentación cubre la integración de autenticación OAuth con proveedores externos (Google, Microsoft, Facebook, Apple) en la plataforma OKLA.

## 📂 Estructura de Documentación

| Documento                                              | Descripción                                | Estado |
| ------------------------------------------------------ | ------------------------------------------ | ------ |
| [GOOGLE_OAUTH_SETUP.md](./GOOGLE_OAUTH_SETUP.md)       | Guía completa para configurar Google OAuth | ✅     |
| [MICROSOFT_OAUTH_SETUP.md](./MICROSOFT_OAUTH_SETUP.md) | Guía para configurar Microsoft/Azure AD    | ✅     |
| [FACEBOOK_OAUTH_SETUP.md](./FACEBOOK_OAUTH_SETUP.md)   | Guía para configurar Facebook Login        | ✅     |
| [APPLE_SIGNIN_SETUP.md](./APPLE_SIGNIN_SETUP.md)       | Guía para configurar Apple Sign In         | ✅     |
| [ARCHITECTURE.md](./ARCHITECTURE.md)                   | Arquitectura técnica del sistema OAuth     | ✅     |
| [TROUBLESHOOTING.md](./TROUBLESHOOTING.md)             | Solución de problemas comunes              | ✅     |

## 🎯 Estado de Integración

| Proveedor     | Estado              | Notas                                          |
| ------------- | ------------------- | ---------------------------------------------- |
| **Google**    | ✅ Implementado     | Probado en desarrollo                          |
| **Microsoft** | ⚠️ Pendiente config | Código listo, falta configurar credenciales    |
| **Facebook**  | ⚠️ Pendiente config | Código listo, falta configurar credenciales    |
| **Apple**     | ⚠️ Pendiente config | Código listo, requiere Apple Developer Account |

## 🚀 Quick Start

### Requisitos Previos

1. Docker y Docker Compose instalados
2. Proyecto OKLA clonado y funcionando
3. Cuenta en el proveedor OAuth que deseas configurar

### Configuración Rápida (Google)

```bash
# 1. Configurar variables de entorno en compose.yaml
GOOGLE_CLIENT_ID=tu-client-id.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=tu-client-secret

# 2. Reiniciar servicios
docker-compose up -d --build authservice frontend-web

# 3. Probar en http://localhost:3000/login
```

## 📋 Flujo de Autenticación

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   Usuario    │────▶│   Frontend   │────▶│   Gateway    │────▶│  AuthService │
│              │     │   (React)    │     │   (Ocelot)   │     │   (.NET 8)   │
└──────────────┘     └──────────────┘     └──────────────┘     └──────────────┘
       │                    │                    │                    │
       │  1. Click "Login   │                    │                    │
       │     con Google"    │                    │                    │
       │───────────────────▶│                    │                    │
       │                    │                    │                    │
       │  2. Redirect to    │                    │                    │
       │◀───────────────────│                    │                    │
       │     Google OAuth   │                    │                    │
       │                    │                    │                    │
       │  3. User authenticates with Google      │                    │
       │─────────────────────────────────────────────────────────────▶│
       │                    │                    │                    │ (Google)
       │  4. Redirect back  │                    │                    │
       │◀────────────────────────────────────────────────────────────│
       │     with code      │                    │                    │
       │                    │                    │                    │
       │                    │  5. POST /api/ExternalAuth/callback     │
       │                    │───────────────────▶│───────────────────▶│
       │                    │                    │                    │
       │                    │                    │  6. Exchange code  │
       │                    │                    │     for id_token   │
       │                    │                    │                    │──▶ Google
       │                    │                    │                    │◀──
       │                    │                    │                    │
       │                    │                    │  7. Validate token │
       │                    │                    │     & create user  │
       │                    │                    │                    │
       │                    │  8. Return JWT     │                    │
       │                    │◀───────────────────│◀───────────────────│
       │                    │                    │                    │
       │  9. Logged in!     │                    │                    │
       │◀───────────────────│                    │                    │
       │                    │                    │                    │
```

## 🔧 Componentes del Sistema

### Backend (AuthService)

| Componente                           | Ruta                       | Descripción                          |
| ------------------------------------ | -------------------------- | ------------------------------------ |
| `ExternalAuthController`             | `Controllers/`             | Endpoints REST para OAuth            |
| `ExternalAuthCallbackCommandHandler` | `Application/Features/`    | Lógica de intercambio de código      |
| `ExternalTokenValidator`             | `Infrastructure/Services/` | Validación de tokens con proveedores |
| `ExternalAuthService`                | `Infrastructure/Services/` | Servicio de autenticación externa    |

### Frontend (React)

| Componente              | Ruta          | Descripción                                |
| ----------------------- | ------------- | ------------------------------------------ |
| `authService.ts`        | `services/`   | Métodos para OAuth (loginWithGoogle, etc.) |
| `OAuthCallbackPage.tsx` | `pages/auth/` | Página de callback que procesa el código   |
| `LoginPage.tsx`         | `pages/auth/` | Botones de OAuth en login                  |
| `RegisterPage.tsx`      | `pages/auth/` | Botones de OAuth en registro               |

### Gateway (Ocelot)

```json
{
  "UpstreamPathTemplate": "/api/ExternalAuth/{everything}",
  "DownstreamPathTemplate": "/api/ExternalAuth/{everything}",
  "DownstreamHostAndPorts": [{ "Host": "authservice", "Port": 80 }]
}
```

## 📞 Endpoints API

### POST /api/ExternalAuth/callback

Intercambia el código de autorización por un JWT.

**Request:**

```json
{
  "provider": "google",
  "code": "4/0AX4XfWh...",
  "redirectUri": "http://localhost:3000/auth/callback/google"
}
```

**Response:**

```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbG...",
    "refreshToken": "dGhpcyBpcyBh...",
    "expiresIn": 3600,
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "email": "user@gmail.com",
    "isNewUser": true
  }
}
```

### GET /api/ExternalAuth/linked-accounts

Obtiene las cuentas externas vinculadas al usuario.

**Headers:** `Authorization: Bearer {token}`

**Response:**

```json
{
  "success": true,
  "data": [
    {
      "provider": "google",
      "email": "user@gmail.com",
      "linkedAt": "2026-01-22T21:30:00Z"
    }
  ]
}
```

### POST /api/ExternalAuth/link-account

Vincula una cuenta externa a un usuario existente.

### DELETE /api/ExternalAuth/unlink-account

Desvincula una cuenta externa del usuario.

## 🔒 Seguridad

- Los tokens de acceso tienen expiración de 1 hora
- Los refresh tokens tienen expiración de 7 días
- Los códigos de autorización solo pueden usarse una vez
- HTTPS obligatorio en producción
- CORS configurado solo para dominios permitidos

## 📚 Referencias

- [Google OAuth 2.0 Documentation](https://developers.google.com/identity/protocols/oauth2)
- [Microsoft Identity Platform](https://docs.microsoft.com/en-us/azure/active-directory/develop/)
- [Facebook Login](https://developers.facebook.com/docs/facebook-login/)
- [Sign in with Apple](https://developer.apple.com/sign-in-with-apple/)

---

_Última actualización: Enero 22, 2026_
