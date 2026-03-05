# 🏗️ OAuth Integration - Arquitectura Técnica

Este documento describe la arquitectura técnica del sistema de autenticación OAuth en OKLA.

## 📊 Diagrama de Arquitectura

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                                    OKLA PLATFORM                                     │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  ┌────────────────────────────────────────────────────────────────────────────────┐ │
│  │                              FRONTEND (React 19)                                │ │
│  │                                                                                 │ │
│  │  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐             │ │
│  │  │   LoginPage.tsx  │  │ RegisterPage.tsx │  │OAuthCallbackPage │             │ │
│  │  │                  │  │                  │  │      .tsx        │             │ │
│  │  │  [Google] [MS]   │  │  [Google] [MS]   │  │                  │             │ │
│  │  │  [FB] [Apple]    │  │  [FB] [Apple]    │  │ Procesa callback │             │ │
│  │  └────────┬─────────┘  └────────┬─────────┘  └────────┬─────────┘             │ │
│  │           │                     │                     │                        │ │
│  │           └─────────────────────┼─────────────────────┘                        │ │
│  │                                 ▼                                              │ │
│  │  ┌─────────────────────────────────────────────────────────────────────────┐  │ │
│  │  │                        authService.ts                                    │  │ │
│  │  │                                                                          │  │ │
│  │  │  • loginWithGoogle()      → Redirect to Google OAuth                    │  │ │
│  │  │  • loginWithMicrosoft()   → Redirect to Microsoft OAuth                 │  │ │
│  │  │  • loginWithFacebook()    → Redirect to Facebook OAuth                  │  │ │
│  │  │  • loginWithApple()       → Redirect to Apple OAuth                     │  │ │
│  │  │  • handleOAuthCallback()  → POST /api/ExternalAuth/callback             │  │ │
│  │  │  • getLinkedAccounts()    → GET /api/ExternalAuth/linked-accounts       │  │ │
│  │  │  • linkExternalAccount()  → POST /api/ExternalAuth/link-account         │  │ │
│  │  │  • unlinkExternalAccount()→ DELETE /api/ExternalAuth/unlink-account     │  │ │
│  │  └─────────────────────────────────┬───────────────────────────────────────┘  │ │
│  │                                    │                                          │ │
│  └────────────────────────────────────┼──────────────────────────────────────────┘ │
│                                       │                                            │
│                                       ▼                                            │
│  ┌────────────────────────────────────────────────────────────────────────────────┐│
│  │                           GATEWAY (Ocelot)                                      ││
│  │                                                                                 ││
│  │   /api/ExternalAuth/{everything}  ──────────▶  authservice:80                  ││
│  │                                                                                 ││
│  └─────────────────────────────────────────────────────────────────────────────────┘│
│                                       │                                            │
│                                       ▼                                            │
│  ┌────────────────────────────────────────────────────────────────────────────────┐│
│  │                         AUTHSERVICE (.NET 8)                                    ││
│  │                                                                                 ││
│  │  ┌─────────────────────────────────────────────────────────────────────────┐   ││
│  │  │                    API Layer (Controllers)                               │   ││
│  │  │                                                                          │   ││
│  │  │  ExternalAuthController.cs                                               │   ││
│  │  │  ├── POST   /callback        → ExternalAuthCallbackCommand              │   ││
│  │  │  ├── POST   /authenticate    → ExternalAuthCommand                      │   ││
│  │  │  ├── POST   /link-account    → LinkExternalAccountCommand               │   ││
│  │  │  ├── DELETE /unlink-account  → UnlinkExternalAccountCommand             │   ││
│  │  │  └── GET    /linked-accounts → GetLinkedAccountsQuery                   │   ││
│  │  └──────────────────────────────────┬──────────────────────────────────────┘   ││
│  │                                     │                                          ││
│  │                                     ▼                                          ││
│  │  ┌─────────────────────────────────────────────────────────────────────────┐   ││
│  │  │                  Application Layer (CQRS + MediatR)                      │   ││
│  │  │                                                                          │   ││
│  │  │  ExternalAuthCallbackCommandHandler.cs                                   │   ││
│  │  │  ├── 1. Parse provider (google/microsoft/facebook/apple)                │   ││
│  │  │  ├── 2. ExchangeCodeForIdToken() → HTTP call to provider                │   ││
│  │  │  │      ├── ExchangeGoogleCode()                                        │   ││
│  │  │  │      ├── ExchangeMicrosoftCode()                                     │   ││
│  │  │  │      ├── ExchangeFacebookCode()                                      │   ││
│  │  │  │      └── ExchangeAppleCode()                                         │   ││
│  │  │  ├── 3. Validate token with ExternalAuthService                         │   ││
│  │  │  └── 4. Return JWT tokens                                               │   ││
│  │  └──────────────────────────────────┬──────────────────────────────────────┘   ││
│  │                                     │                                          ││
│  │                                     ▼                                          ││
│  │  ┌─────────────────────────────────────────────────────────────────────────┐   ││
│  │  │                   Infrastructure Layer (Services)                        │   ││
│  │  │                                                                          │   ││
│  │  │  ExternalAuthService.cs                                                  │   ││
│  │  │  ├── AuthenticateAsync() → Validate & create/find user                  │   ││
│  │  │  └── Uses IExternalTokenValidator                                        │   ││
│  │  │                                                                          │   ││
│  │  │  ExternalTokenValidator.cs                                               │   ││
│  │  │  ├── ValidateGoogleTokenAsync()                                          │   ││
│  │  │  │   └── GET https://oauth2.googleapis.com/tokeninfo?id_token=xxx       │   ││
│  │  │  └── ValidateMicrosoftTokenAsync()                                       │   ││
│  │  │      └── GET https://graph.microsoft.com/v1.0/me                        │   ││
│  │  └─────────────────────────────────────────────────────────────────────────┘   ││
│  │                                                                                 ││
│  └─────────────────────────────────────────────────────────────────────────────────┘│
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       │ HTTP Calls
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                              EXTERNAL OAUTH PROVIDERS                                │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐    │
│  │     Google     │  │   Microsoft    │  │    Facebook    │  │     Apple      │    │
│  │                │  │                │  │                │  │                │    │
│  │ Token Endpoint │  │ Token Endpoint │  │ Token Endpoint │  │ Token Endpoint │    │
│  │ oauth2.google  │  │ login.micro... │  │ graph.facebook │  │ appleid.apple  │    │
│  │ apis.com       │  │ softonline.com │  │ .com           │  │ .com           │    │
│  └────────────────┘  └────────────────┘  └────────────────┘  └────────────────┘    │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

## 📁 Estructura de Archivos

### Backend (AuthService)

```
backend/AuthService/
├── AuthService.Api/
│   └── Controllers/
│       └── ExternalAuthController.cs          # REST endpoints
│
├── AuthService.Application/
│   ├── DTOs/ExternalAuth/
│   │   ├── ExternalAuthRequest.cs
│   │   ├── ExternalAuthResponse.cs
│   │   ├── ExternalAuthCallbackRequest.cs
│   │   ├── ExternalLoginRequest.cs
│   │   ├── ExternalLoginResponse.cs
│   │   ├── LinkedAccountResponse.cs
│   │   └── UnlinkExternalAccountRequest.cs
│   │
│   └── Features/ExternalAuth/
│       ├── Commands/
│       │   ├── ExternalAuth/
│       │   │   ├── ExternalAuthCommand.cs
│       │   │   └── ExternalAuthCommandHandler.cs
│       │   ├── ExternalAuthCallback/
│       │   │   ├── ExternalAuthCallbackCommand.cs
│       │   │   └── ExternalAuthCallbackCommandHandler.cs  # Token exchange logic
│       │   ├── ExternalLogin/
│       │   │   ├── ExternalLoginCommand.cs
│       │   │   └── ExternalLoginCommandHandler.cs
│       │   ├── LinkExternalAccount/
│       │   │   ├── LinkExternalAccountCommand.cs
│       │   │   └── LinkExternalAccountCommandHandler.cs
│       │   └── UnlinkExternalAccount/
│       │       ├── UnlinkExternalAccountCommand.cs
│       │       └── UnlinkExternalAccountCommandHandler.cs
│       │
│       └── Queries/
│           └── GetLinkedAccounts/
│               ├── GetLinkedAccountsQuery.cs
│               └── GetLinkedAccountsQueryHandler.cs
│
├── AuthService.Domain/
│   ├── Entities/
│   │   └── User.cs                            # ExternalProvider, ExternalId fields
│   ├── Enums/
│   │   └── ExternalAuthProvider.cs            # Google=1, Microsoft=2, Facebook=3, Apple=4
│   └── Interfaces/
│       └── Services/
│           ├── IExternalAuthService.cs
│           └── IExternalTokenValidator.cs
│
└── AuthService.Infrastructure/
    └── Services/ExternalAuth/
        ├── ExternalAuthService.cs             # Main authentication service
        └── ExternalTokenValidator.cs          # Token validation with providers
```

### Frontend (React)

```
frontend/web/src/
├── services/
│   └── authService.ts                         # OAuth methods
│
├── pages/auth/
│   ├── LoginPage.tsx                          # OAuth buttons
│   ├── RegisterPage.tsx                       # OAuth buttons
│   └── OAuthCallbackPage.tsx                  # Callback handler
│
├── pages/user/
│   └── SecuritySettingsPage.tsx               # Linked accounts management
│
└── store/
    └── authStore.ts                           # Auth state management (Zustand)
```

### Gateway Configuration

```
backend/Gateway/Gateway.Api/
└── ocelot.dev.json                            # Route configuration
```

```json
{
  "UpstreamPathTemplate": "/api/ExternalAuth/{everything}",
  "UpstreamHttpMethod": ["OPTIONS", "GET", "POST", "PUT", "DELETE"],
  "DownstreamPathTemplate": "/api/ExternalAuth/{everything}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [{ "Host": "authservice", "Port": 80 }]
}
```

## 🔄 Flujo Detallado de Autenticación

### Fase 1: Inicio del Flujo OAuth

```typescript
// Frontend: authService.ts - loginWithGoogle()

1. Usuario hace clic en "Continuar con Google"
2. Frontend construye URL de autorización:
   https://accounts.google.com/o/oauth2/v2/auth?
     client_id=xxx.apps.googleusercontent.com
     &redirect_uri=http://localhost:3000/auth/callback/google
     &response_type=code
     &scope=openid email profile
     &state=random-uuid

3. window.location.href = authUrl (redirección a Google)
```

### Fase 2: Usuario en Google

```
4. Usuario ve página de Google
5. Inicia sesión / selecciona cuenta
6. Acepta permisos (email, perfil)
7. Google redirige a:
   http://localhost:3000/auth/callback/google?code=4/0AX4XfWh...&state=random-uuid
```

### Fase 3: Callback en Frontend

```typescript
// Frontend: OAuthCallbackPage.tsx

8. Componente extrae 'code' de URL params
9. Verifica que no se procese dos veces (useRef)
10. Llama a handleOAuthCallback(provider, code)
```

### Fase 4: Intercambio de Código

```typescript
// Frontend → Backend

11. POST /api/ExternalAuth/callback
    {
      "provider": "google",
      "code": "4/0AX4XfWh...",
      "redirectUri": "http://localhost:3000/auth/callback/google"
    }
```

```csharp
// Backend: ExternalAuthCallbackCommandHandler.cs

12. ExchangeGoogleCode(code, redirectUri):
    - POST https://oauth2.googleapis.com/token
      {
        "code": "4/0AX4XfWh...",
        "client_id": "xxx.apps.googleusercontent.com",
        "client_secret": "GOCSPX-xxx",
        "redirect_uri": "http://localhost:3000/auth/callback/google",
        "grant_type": "authorization_code"
      }

13. Google responde:
    {
      "access_token": "ya29.xxx",
      "id_token": "eyJhbGciOiJSUzI1...",
      "expires_in": 3600,
      "refresh_token": "1//xxx"
    }

14. Extraer id_token de la respuesta
```

### Fase 5: Validación del Token

```csharp
// Backend: ExternalTokenValidator.cs

15. ValidateGoogleTokenAsync(idToken):
    - GET https://oauth2.googleapis.com/tokeninfo?id_token=eyJhbG...

16. Google responde:
    {
      "email": "user@gmail.com",
      "email_verified": "true",
      "sub": "112233445566778899",
      "name": "John Doe",
      "picture": "https://..."
    }
```

### Fase 6: Crear/Encontrar Usuario

```csharp
// Backend: ExternalAuthService.cs

17. AuthenticateAsync(Google, idToken):
    - Buscar usuario por ExternalProvider=Google, ExternalId=sub
    - Si no existe, crear nuevo usuario
    - Generar JWT access token y refresh token

18. Retornar:
    {
      "accessToken": "eyJhbG...",
      "refreshToken": "base64...",
      "expiresIn": 3600,
      "userId": "guid",
      "email": "user@gmail.com",
      "isNewUser": true/false
    }
```

### Fase 7: Completar Login

```typescript
// Frontend: OAuthCallbackPage.tsx

19. Recibir respuesta del backend
20. storeLogin(response) - Guardar en Zustand + localStorage
21. navigate('/dashboard') - Redirigir a dashboard
```

## 🔐 Modelo de Datos

### Tabla: Users

```sql
CREATE TABLE "Users" (
    "Id" UUID PRIMARY KEY,
    "Email" VARCHAR(255) UNIQUE NOT NULL,
    "PasswordHash" VARCHAR(255),          -- NULL for OAuth-only users
    "FirstName" VARCHAR(100),
    "LastName" VARCHAR(100),
    "ExternalProvider" VARCHAR(50),        -- 'Google', 'Microsoft', 'Facebook', 'Apple'
    "ExternalId" VARCHAR(255),             -- ID del usuario en el proveedor
    "ExternalProviderData" JSONB,          -- Datos adicionales del proveedor
    "EmailVerified" BOOLEAN DEFAULT false,
    "CreatedAt" TIMESTAMP NOT NULL,
    "UpdatedAt" TIMESTAMP
);

CREATE INDEX idx_users_external ON "Users" ("ExternalProvider", "ExternalId");
```

### Enum: ExternalAuthProvider

```csharp
public enum ExternalAuthProvider
{
    Google = 1,
    Microsoft = 2,
    Facebook = 3,
    Apple = 4
}
```

## ⚙️ Configuración por Proveedor

### Google

```yaml
# compose.yaml
Authentication__Google__ClientId: "xxx.apps.googleusercontent.com"
Authentication__Google__ClientSecret: "GOCSPX-xxx"
```

| Endpoint      | URL                                            |
| ------------- | ---------------------------------------------- |
| Authorization | `https://accounts.google.com/o/oauth2/v2/auth` |
| Token         | `https://oauth2.googleapis.com/token`          |
| Token Info    | `https://oauth2.googleapis.com/tokeninfo`      |

### Microsoft

```yaml
Authentication__Microsoft__ClientId: "guid"
Authentication__Microsoft__ClientSecret: "xxx"
```

| Endpoint      | URL                                                              |
| ------------- | ---------------------------------------------------------------- |
| Authorization | `https://login.microsoftonline.com/common/oauth2/v2.0/authorize` |
| Token         | `https://login.microsoftonline.com/common/oauth2/v2.0/token`     |
| User Info     | `https://graph.microsoft.com/v1.0/me`                            |

### Facebook

```yaml
Authentication__Facebook__AppId: "123456789"
Authentication__Facebook__AppSecret: "xxx"
```

| Endpoint      | URL                                                   |
| ------------- | ----------------------------------------------------- |
| Authorization | `https://www.facebook.com/v18.0/dialog/oauth`         |
| Token         | `https://graph.facebook.com/v18.0/oauth/access_token` |
| User Info     | `https://graph.facebook.com/me?fields=id,email,name`  |

### Apple

```yaml
Authentication__Apple__ClientId: "com.okla.app"
Authentication__Apple__ClientSecret: "xxx" # Generated JWT
```

| Endpoint      | URL                                        |
| ------------- | ------------------------------------------ |
| Authorization | `https://appleid.apple.com/auth/authorize` |
| Token         | `https://appleid.apple.com/auth/token`     |

## 🛡️ Consideraciones de Seguridad

### 1. PKCE (Proof Key for Code Exchange)

Para mayor seguridad, se puede implementar PKCE:

```typescript
// Frontend: Generar code_verifier y code_challenge
const codeVerifier = generateRandomString(64);
const codeChallenge = base64url(sha256(codeVerifier));

// Agregar a URL de autorización
authUrl.searchParams.append("code_challenge", codeChallenge);
authUrl.searchParams.append("code_challenge_method", "S256");

// Backend: Enviar code_verifier en token exchange
```

### 2. State Parameter

El parámetro `state` previene ataques CSRF:

```typescript
const state = crypto.randomUUID();
sessionStorage.setItem("oauth_state", state);

// En callback, verificar que coincida
const returnedState = searchParams.get("state");
if (returnedState !== sessionStorage.getItem("oauth_state")) {
  throw new Error("Invalid state parameter");
}
```

### 3. Secrets Management

- ❌ Nunca guardar Client Secret en frontend
- ❌ Nunca subir secrets a Git
- ✅ Usar variables de entorno
- ✅ Usar Kubernetes Secrets en producción
- ✅ Rotar secrets periódicamente

---

_Última actualización: Enero 22, 2026_
